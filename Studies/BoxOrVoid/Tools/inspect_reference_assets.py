"""Read-only inventory of the locally supplied reference Unity build."""
from pathlib import Path
import collections
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools/python"))
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.Tpk import get_typetree_node
from UnityPy.enums import ClassIDType

SOURCE = Path(r"D:\Steam\steamapps\common\Box or Void Demo\Box or Void_Data")
OUT = ROOT / "Research/Extracted"
OUT.mkdir(parents=True, exist_ok=True)
files = list(SOURCE.glob("*.assets")) + [p for p in SOURCE.glob("level*") if p.is_file()]
files += list(SOURCE.glob("*.resS")) + list(SOURCE.glob("*.resource"))
files += list((SOURCE / "StreamingAssets/aa").rglob("*.bundle"))
environment = UnityPy.load(*(str(p) for p in files))
generator = TypeTreeGenerator("6000.0.47f1", generator="AssetStudio")
generator.load_local_dll_folder(str(SOURCE / "Managed"))
environment.typetree_generator = generator
inventory, behaviours, failures = [], [], []
for obj in environment.objects:
    kind = obj.type.name
    if kind not in ("Sprite", "Texture2D", "AudioClip", "Font", "MonoBehaviour", "TextAsset"):
        continue
    try:
        data = obj.parse_monobehaviour_head() if kind == "MonoBehaviour" else obj.read()
        entry = {"file": obj.assets_file.name, "id": obj.path_id, "type": kind, "name": getattr(data, "m_Name", "")}
        if kind == "MonoBehaviour":
            script = data.m_Script.read()
            entry["class"] = script.m_ClassName
            if entry["class"] in ("LevelDatabase", "WorldData", "ObjectSpriteData", "BackgroundData", "SfxPlayer", "SfxLibrary", "PlayerController", "GameManager", "GameInput", "InputActionAsset", "ObjectSpriteManager", "LevelLoader", "WorldSelectionController", "LevelSelectionController", "LevelTile", "WorldTile", "LevelIdDisplay", "CameraController", "CameraFit", "WorldLayoutData", "LevelSelectionLayout", "WorldLayout", "MusicLibrary", "UiSfxLibrary", "LevelSceneUIGroup", "TextMeshProUGUI", "CameraController2D", "BackgroundEntityManager", "SelectionTileLink", "Paths", "LevelSelectionCursor"):
                fullname = (script.m_Namespace + "." if script.m_Namespace else "") + script.m_ClassName
                node = generator.get_nodes_up(script.m_AssemblyName, fullname)
                # AssetStudio's generated root omits the enabled-byte alignment.
                # Use UnityPy's versioned built-in header before custom fields.
                header = get_typetree_node(ClassIDType.MonoBehaviour, obj.version)
                headers = {n.m_Name:n for n in header.m_Children}
                node.m_Children = [headers.get(n.m_Name,n) for n in node.m_Children]
                tree = obj.read_typetree(nodes=node)
                behaviours.append(dict(entry, tree=tree))
        inventory.append(entry)
    except Exception as exc:
        failures.append({"file":obj.assets_file.name,"id":obj.path_id,"type":kind,"error":str(exc)[:140]})
(OUT / "inventory.json").write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding="utf-8")
(OUT / "serialized.json").write_text(json.dumps(behaviours,ensure_ascii=False,indent=2),encoding="utf-8")
(OUT / "failures.json").write_text(json.dumps(failures,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps({"files":len(files),"types":dict(collections.Counter(x['type'] for x in inventory)),"behaviours":len(behaviours),"failures":len(failures)},ensure_ascii=False))
print("Relevant classes:",dict(collections.Counter(x['class'] for x in behaviours)))
