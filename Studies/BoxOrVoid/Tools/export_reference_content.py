"""Extract the authorized two-chapter study data and presentation references."""
from pathlib import Path
import hashlib
import json
import shutil
import re
import math
from PIL import Image
import inspect_reference_assets as source
from UnityPy.classes import PPtr

ROOT = source.ROOT
ASSETS = ROOT.parents[1] / "Assets/Studies/BoxOrVoid"
OUT = ROOT / "Research"
FILES = {o.assets_file.name:o.assets_file for o in source.environment.objects}
origins = []
exported = {}
sprite_imports = []


def deref(pointer, file):
    if not pointer.get("m_PathID"):
        return None
    return PPtr(m_FileID=pointer["m_FileID"],m_PathID=pointer["m_PathID"],assetsfile=FILES[file]).deref()


def safe_name(value):
    return re.sub(r'[<>:"/\\|?*]', '_', value)


def export(obj):
    if obj is None:
        return ""
    key = (obj.assets_file.name,obj.path_id)
    if key in exported:
        return exported[key]
    data = obj.read()
    name = safe_name(data.m_Name)
    if obj.type.name == "Sprite":
        output = ASSETS / "Art/Sprites" / (name+".png")
        output.parent.mkdir(parents=True,exist_ok=True)
        # UnityPy returns the trimmed render rectangle. Restore the logical
        # sprite canvas, otherwise corner bevels and badges become full tiles.
        cropped = data.image.convert('RGBA')
        size = (math.ceil(data.m_Rect.width), math.ceil(data.m_Rect.height))
        canvas = Image.new('RGBA', size)
        offset = data.m_RD.textureRectOffset
        canvas.paste(cropped, (round(offset.x), size[1] - round(offset.y) - cropped.height))
        canvas.save(output)
        sprite_imports.append({'name':name,'pixelsPerUnit':data.m_PixelsToUnits,'pivotX':data.m_Pivot.x,'pivotY':data.m_Pivot.y})
    elif obj.type.name == "Font":
        output = ASSETS / "Art/Fonts" / (name+".ttf")
        output.parent.mkdir(parents=True,exist_ok=True)
        output.write_bytes(bytes(data.m_FontData))
    elif obj.type.name == "AudioClip":
        samples = data.samples
        if not samples:
            raise RuntimeError("No decoded samples for "+name)
        sample_name,sample = next(iter(samples.items()))
        output = ASSETS / "Audio" / (name+Path(sample_name).suffix)
        output.parent.mkdir(parents=True,exist_ok=True)
        output.write_bytes(sample)
    else:
        raise RuntimeError("Unexpected export type "+obj.type.name)
    origins.append({"output":str(output.relative_to(ASSETS)),"sourceFile":obj.assets_file.name,"pathId":obj.path_id,"type":obj.type.name,"name":data.m_Name,"sha256":hashlib.sha256(output.read_bytes()).hexdigest()})
    exported[key] = name
    return name


def flatten_sprites(tree,file,prefix=""):
    entries=[]
    for key,value in tree.items():
        if key.startswith("m_") or any(t in key.lower() for t in ["snake","sticky","flip"]):
            continue
        path=prefix+key
        if isinstance(value,dict):
            if "m_PathID" in value:
                obj=deref(value,file)
                if obj and obj.type.name=="Sprite":
                    entries.append({"key":path,"sprite":export(obj)})
            else:
                entries += flatten_sprites(value,file,path+".")
    return entries


themes=[]
for name in ["Tut Sprite Data","Box Sprite Data"]:
    item=next(x for x in source.behaviours if x['class']=='ObjectSpriteData' and x['name']==name)
    themes.append({"chapter":item['tree']['worldIndex'],"sprites":flatten_sprites(item['tree'],item['file'])})
for item in source.behaviours:
    if item['class']=='BackgroundData' and item['tree'].get('worldIndex',99)<2:
        themes[item['tree']['worldIndex']]['backgrounds']=flatten_sprites(item['tree'],item['file'])

# Only two-chapter/UI presentation assets. No later-chapter rules or binaries.
extras={"title_upscaled_withChineseName","Cursor","Goal_complete","keyboard_space_icon_outline","keyboard_z_outline","keyboard_r_outline","keyboard_escape_outline","Play_active","Settings_active","Return_active","Home Icon","Return Icon","Resume Button","Settings Icon","Tut World Background","Main Menu Background","Main Menu Pointer","Particle_Box_Uncolored","Dim Overlay","vignette","Main Menu Button Panel"}
for obj in source.environment.objects:
    if obj.type.name=='Font' and obj.peek_name()=='NotoSansSC-Black':
        export(obj)
    if obj.type.name=='Sprite':
        name=obj.peek_name() or ''
        if name in extras or name.removesuffix('_0') in extras or name.startswith(('Level_tile_tut_','Level_tile_box_')) or name in ('Level_tile_locked','Level_tile_locked_hard','Tut World Tile-sc','Box World Tile','Box World Locked','Box World Background'):
            export(obj)

audio=[]
for item in source.behaviours:
    if item['class'] in ('SfxLibrary','UiSfxLibrary','MusicLibrary'):
        for key,value in item['tree'].items():
            if item['class']=='MusicLibrary' and key in ('theme','boxWorld'):
                value=value['clip']
            if key.startswith('m_') or not isinstance(value,dict) or 'm_PathID' not in value:
                continue
            if item['class']=='MusicLibrary' and key not in ('theme','boxWorld'):
                continue
            obj=deref(value,item['file'])
            if obj and obj.type.name=='AudioClip':
                audio.append({"key":key,"clip":export(obj)})

db=next(x['tree'] for x in source.behaviours if x['class']=='LevelDatabase')
levels=[]
for chapter,folder in enumerate(['Tut','Box']):
    for level in db['worlds'][chapter]['levels']:
        path=source.SOURCE/'StreamingAssets/Levels'/folder/level['levelFileName']
        target=ASSETS/'Content/Levels'/path.name
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(path,target)
        data=json.loads(path.read_text(encoding='utf-8'))
        levels.append({"id":path.stem,"chapter":chapter,"displayId":level['levelDisplayId'],"defaultUnlocked":level['isUnlockedByDefault'],"hintGroups":level['levelSceneUIGroupIds'],"width":data['width'],"height":data['height'],"source":str(path),"sha256":hashlib.sha256(path.read_bytes()).hexdigest()})

layout=[]
for item in source.behaviours:
    if item['class']!='LevelTile' or item['file'] not in ('level2','level3') or not item['tree']['levelFile']['m_PathID']:
        continue
    tree=item['tree']; file=item['file']
    level_asset=deref(tree['levelFile'],file)
    obj=deref(tree['m_GameObject'],file).read()
    transform=next(c.component.deref().read() for c in obj.m_Component if c.component.deref().type.name=='Transform')
    unlock=[]
    for pointer in tree['tilesToUnlockOnCompletion']:
        target=next(x for x in source.behaviours if x['file']==file and x['id']==pointer['m_PathID'])
        unlock.append(deref(target['tree']['levelFile'],file).peek_name())
    layout.append({"id":level_asset.peek_name(),"x":transform.m_LocalPosition.x,"y":transform.m_LocalPosition.y,"unlocks":unlock,"lockedSprite":export(deref(tree['lockedSprite'],file)),"unlockedSprite":export(deref(tree['unlockedSprite'],file)),"completedSprite":export(deref(tree['completedSprite'],file))})

for level in levels:
    entry=next((x for x in layout if x['id']==level['id']),None)
    if entry:
        level.update(entry)

navigation=[]
for chapter,file in enumerate(['level2','level3']):
    links=[x for x in source.behaviours if x['file']==file and x['class']=='SelectionTileLink']
    link_names={x['id']:'node_'+str(chapter)+'_'+str(x['id']) for x in links}
    for link in links:
        tree=link['tree'];go=deref(tree['m_GameObject'],file).read()
        transform=next(c.component.deref().read() for c in go.m_Component if c.component.deref().type.name=='Transform')
        tile=next((x for x in source.behaviours if x['file']==file and x['class'] in ('LevelTile','WorldTile') and x['tree']['m_GameObject']==tree['m_GameObject']),None)
        level_id='';sprite='';is_return=bool(tile and tile['class']=='WorldTile')
        if tile:
            t=tile['tree']
            if t.get('levelFile',{}).get('m_PathID'): level_id=deref(t['levelFile'],file).peek_name()
            sprite=export(deref(t.get('unlockedSprite',{}),file))
        navigation.append({'id':link_names[link['id']],'chapter':chapter,'x':transform.m_LocalPosition.x,'y':transform.m_LocalPosition.y,'level':level_id,'isReturn':is_return,'sprite':sprite,'neighbors':[link_names.get(tree.get(k,{}).get('m_PathID'),'') for k in ['up','right','down','left']]})

manifest={"sourceGame":"Box or Void Demo","referenceEngine":"6000.0.47f1","themes":themes,"audio":audio,"levels":levels,"navigation":navigation}
(ASSETS/'Content/Catalog.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(ASSETS/'Content/SpriteImport.json').write_text(json.dumps({'sprites':sprite_imports},indent=2),encoding='utf-8')
(OUT/'Levels.json').write_text(json.dumps(levels,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'AssetSources.json').write_text(json.dumps(origins,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({"levels":len(levels),"themes":len(themes),"exported":len(exported),"layout":len(layout),"audio":len(audio)},ensure_ascii=False))
