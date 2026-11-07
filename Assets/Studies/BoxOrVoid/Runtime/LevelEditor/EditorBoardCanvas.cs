using System;
using System.Collections.Generic;
using System.Linq;
using BoxOrVoid.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BoxOrVoid.Study.LevelEditing
{
    /// <summary>Pooled uGUI rendering accepts unfinished documents without constructing a playable puzzle.</summary>
    public sealed class EditorBoardCanvas:MonoBehaviour
    {
        private readonly List<Image> images=new List<Image>();
        private int used;
        public float CellSize {get;private set;}
        public LevelData Data {get;private set;}
        public RectTransform Rect=>(RectTransform)transform;
        private static readonly string[] Masks={"None","Up","Right","UpRight","Down","UpDown","RightDown","UpRightDown","Left","UpLeft","RightLeft","UpRightLeft","DownLeft","UpDownLeft","RightDownLeft","Uprightdownleft"};
        private static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        public static Sprite Icon(StudyAssets assets,int theme,ObjectKind kind,Tone tone,Tone view,bool active=false)
        {
            string p=tone==Tone.White?"white":tone==Tone.Black?"black":"gray";bool on=active||tone==view||tone==Tone.Gray;
            string key=kind==ObjectKind.Wall?p+(on?"WallActiveVariantSet.Uprightdownleft":"WallInactiveSprite"):kind==ObjectKind.Player?p+"Player"+(on?"ActiveFacingRightSprite":"InactiveSprite"):kind==ObjectKind.Box?p+"Box"+(on?"ActiveSprite":"InactiveSprite"):p+(kind==ObjectKind.BoxGoal?"Box":"Player")+"GoalSprite";
            return assets.ThemeSprite(theme,key);
        }
        public void Render(StudyAssets assets,LevelData data,int theme,Tone view,float cellSize,float opacity=1)
        {
            Data=data;CellSize=cellSize;Rect.sizeDelta=new Vector2(data.width,data.height)*cellSize;used=0;
            // All shadows sit behind the entire board; interleaving them with tiles
            // lets a later row paint its offset shadow over an already drawn wall.
            for(int i=0;i<data.grid.Length;i++)
                if(data.grid[i].Objects.Any(o=>o.ObjectType==ObjectKind.Wall&&o.Color==Tone.Gray))
                    Draw(assets.ThemeSprite(theme,"grayWallInactiveSprite"),Position(i)+new Vector2(cellSize*.09f,-cellSize*.12f),cellSize,new Color(0,0,0,.35f*opacity));
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++)
            {
                int index=y*data.width+x;Vector2 pos=Position(index);var objects=data.grid[index].Objects;var wall=objects.FirstOrDefault(o=>o.ObjectType==ObjectKind.Wall);
                if(wall!=null)
                {
                    bool active=wall.Color==Tone.Gray||wall.Color==view;string p=wall.Color==Tone.White?"white":wall.Color==Tone.Black?"black":"gray";
                    bool Connected(int xx,int yy)=>xx>=0&&yy>=0&&xx<data.width&&yy<data.height&&data.grid[yy*data.width+xx].Objects.Any(o=>o.ObjectType==ObjectKind.Wall&&o.Color==wall.Color)&&!data.grid[yy*data.width+xx].Objects.Any(o=>o.ObjectType==ObjectKind.Box||o.ObjectType==ObjectKind.Player);
                    int mask=0;for(int n=0;n<4;n++)if(!Connected(x+Directions[n].x,y+Directions[n].y))mask|=1<<n;
                    Sprite sprite=assets.ThemeSprite(theme,p+(active?"WallActiveVariantSet."+Masks[mask]:"WallInactiveSprite"));
                    Draw(sprite,pos,cellSize,new Color(1,1,1,opacity));
                    string[] corners={"UpRight","DownRight","DownLeft","UpLeft"};
                    for(int c=0;c<4;c++){var a=Directions[c];var b=Directions[(c+1)%4];if(active&&Connected(x+a.x,y+a.y)&&Connected(x+b.x,y+b.y)&&!Connected(x+a.x+b.x,y+a.y+b.y))Draw(assets.ThemeSprite(theme,p+"WallCornerOverlays."+corners[c]),pos,cellSize,new Color(1,1,1,opacity));}
                }
                var pieces=objects.Where(o=>o.ObjectType==ObjectKind.Box||o.ObjectType==ObjectKind.Player).ToArray();
                if(pieces.Length>0){var o=pieces[pieces.Length-1];Draw(Icon(assets,theme,o.ObjectType,o.Color,view),pos,cellSize,new Color(1,1,1,opacity));}
                foreach(var o in objects.Where(o=>o.ObjectType==ObjectKind.BoxGoal||o.ObjectType==ObjectKind.PlayerGoal))
                {
                    var sprite=Icon(assets,theme,o.ObjectType,o.Color,view);
                    if(pieces.Length>0){string p=o.Color==Tone.White?"white":"black";sprite=assets.ThemeSprite(theme,p+(o.ObjectType==ObjectKind.BoxGoal?"Box":"Player")+"GoalUnderSprite");}
                    Draw(sprite,pos,cellSize,new Color(1,1,1,opacity));
                }
                if(pieces.Length>1){var o=pieces[pieces.Length-2];Draw(assets.ThemeSprite(theme,(o.Color==Tone.White?"white":"black")+(o.ObjectType==ObjectKind.Player?"Player":"Box")+"UnderSprite"),pos,cellSize,new Color(1,1,1,opacity));}
            }
            for(int i=used;i<images.Count;i++)images[i].gameObject.SetActive(false);
        }
        public Vector2 Position(int index)=>new Vector2(index%Data.width-(Data.width-1)*.5f,index/Data.width-(Data.height-1)*.5f)*CellSize;
        public int CellAt(Vector2 local)
        {if(Data==null)return -1;int x=Mathf.FloorToInt(local.x/CellSize+Data.width*.5f),y=Mathf.FloorToInt(local.y/CellSize+Data.height*.5f);return x<0||y<0||x>=Data.width||y>=Data.height?-1:y*Data.width+x;}
        private void Draw(Sprite sprite,Vector2 position,float size,Color color)
        {
            if(sprite==null)return;
            if(used>=images.Count)images.Add(EditorUi.Image("Tile "+used,transform,null,Vector2.zero,Vector2.one));
            var image=images[used++];image.gameObject.SetActive(true);image.sprite=sprite;image.color=color;image.preserveAspect=true;image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=Vector2.one*size;
        }
    }
}
