using System;
using System.Collections;
using System.IO;
using System.Linq;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class RuntimeChecks
    {
        public void BeginShadowReview(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor merged shadows, settings and sound";report.width=Screen.width;report.height=Screen.height;
            folder=Path.GetFullPath("DraculasBox/Reviews/ShadowSeams-20261111/Play");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;StartCoroutine(Guard(ShadowReviewChecks()));
        }
        private static LevelData ShadowRoom()=>new LevelData
        {
            id="shadow-seam-review",title="交叠投影",sunEnabled=true,sunFrom=SunSide.Left,
            rows=new[]{"#########","##....###","##.....##","##......#","##......#","###...###","#########"},
            player=new Cell(3,3),coffin=new Cell(4,5),pieces=new[]{new EntityData{id="box-a",cell=new Cell(2,2)},new EntityData{id="box-b",cell=new Cell(3,4)},new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(3,5)}}
        };
        private IEnumerator ShadowReviewChecks()
        {
            yield return Ready();yield return LightTheme("wine");app.StartEditorPlay(ShadowRoom());yield return Ready();
            var cells=new[]{new Cell(3,2),new Cell(3,2)};var offsets=new[]{new Vector2(.20f,-.47f),new Vector2(.20f,-.53f)};
            var colors=LightPixels(cells,offsets);float jump=Mathf.Abs(colors[0].grayscale-colors[1].grayscale);
            Require(jump<.12f,"merged solar shadows soften the reproduced internal step");Capture("merged-shadows");
            var raw=(RenderTexture)LightMember("shadowRaw");int id=raw.GetInstanceID(),updates=(int)LightMember("shadowFieldUpdates");
            for(int i=0;i<18;i++)yield return null;
            Require((int)LightMember("shadowFieldUpdates")==updates,"idle boards do not rerender or blur their shadow field");
            raw.Release();((RenderTexture)LightMember("shadowSoft")).Release();
            typeof(BoardView).GetMethod("Refresh",LightPrivate).Invoke(app.Board,new object[]{1f});
            Require(raw.IsCreated()&&((RenderTexture)LightMember("shadowSoft")).IsCreated()&&(int)LightMember("shadowFieldUpdates")>updates,"a stationary board restores and repaints lost GPU shadow textures");
            var recovered=LightPixels(cells,offsets);Require(Mathf.Abs(recovered[0].grayscale-colors[0].grayscale)<.015f,"restored GPU contents retain the original shadow pixels");
            var saved=app.Session.State.Key();app.Act(Command.Left);yield return Ready();app.Act(Command.Up);yield return Ready();
            Require(app.Session.State.Position(0)==new Cell(2,1)&&((RenderTexture)LightMember("shadowRaw")).GetInstanceID()==id,"moving a caster updates the union without allocating another render texture");
            app.Act(Command.Undo);yield return Ready();app.Act(Command.Undo);yield return Ready();
            var restored=LightPixels(cells,offsets);
            Require(app.Session.State.Key()==saved&&Mathf.Abs(restored[0].grayscale-colors[0].grayscale)<.015f&&Mathf.Abs(restored[1].grayscale-colors[1].grayscale)<.015f,"undo restores the original merged projection pixels");
            var doorSun=new LevelData
            {
                id="door-solar-fade",title="日光门影",sunEnabled=true,sunFrom=SunSide.Left,
                rows=new[]{".........",".........",".#.......",".........",".........",".#.......",".#.......","........."},
                player=new Cell(2,6),coffin=new Cell(8,7),pieces=new[]{new EntityData{id="weight",cell=new Cell(2,5)}},
                plates=new[]{new DeviceData{cell=new Cell(2,4)}},doors=new[]{new DeviceData{cell=new Cell(1,3)}}
            };
            app.StartEditorPlay(doorSun);yield return Ready();app.Act(Command.Up);Require(app.Session.State.DoorOpen(0)&&!app.Session.State.Dead,"sunlit door fixture opens while keeping the player safe");
            typeof(BoardView).GetMethod("Refresh",LightPrivate).Invoke(app.Board,new object[]{.90f});
            var last=LightPixels(new[]{new Cell(2,2)},new[]{new Vector2(.20f,-.475f)})[0];
            typeof(BoardView).GetMethod("Refresh",LightPrivate).Invoke(app.Board,new object[]{1f});
            var end=LightPixels(new[]{new Cell(2,2)},new[]{new Vector2(.20f,-.475f)})[0];
            Require(Mathf.Abs(last.grayscale-end.grayscale)<.035f,"an opening door does not pop the adjacent fixed wall contour on its final frame");Capture("sun-door-end");yield return Ready();
            var joined=ShadowRoom();joined.pieces=Array.Empty<EntityData>();joined.player=new Cell(2,3);app.StartEditorPlay(joined);yield return Ready();
            colors=LightPixels(new[]{new Cell(2,2),new Cell(2,2),new Cell(2,2),new Cell(4,2)},new[]{new Vector2(.2f,-.40f),new Vector2(.2f,-.50f),new Vector2(.2f,-.60f),Vector2.zero});
            Require(colors[1].grayscale<=Mathf.Max(colors[0].grayscale,colors[2].grayscale)+.045f,"touching wall projections have no artificial bright seam");
            Require(colors[3].grayscale>.60f,"filtering never darkens the sunny cell beyond the two-cell projection");
            foreach(SunSide side in Enum.GetValues(typeof(SunSide)))
            {
                var direction=LightField.SunTravel[(int)side];var box=new Cell(3,3);
                var data=new LevelData{id="shadow-direction",title="投影方向",rows=Enumerable.Repeat(".......",7).ToArray(),sunEnabled=true,sunFrom=side,player=box+direction,coffin=new Cell(0,0),pieces=new[]{new EntityData{id="box",cell=box}}};
                app.StartEditorPlay(data);yield return Ready();var layers=app.Board.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Vampire"||s.name=="Cape"||s.name=="Fangs").ToArray();var visible=layers.Select(s=>s.enabled).ToArray();
                try
                {
                    foreach(var layer in layers)layer.enabled=false;
                    colors=LightPixels(new[]{box+direction,box+direction+direction,box+direction+direction+direction});
                }
                finally{for(int i=0;i<layers.Length;i++)layers[i].enabled=visible[i];}
                Require(colors[1].grayscale>colors[0].grayscale+.06f&&colors[2].grayscale>colors[1].grayscale+.06f,"GPU mask has the correct orientation, gradient and two-cell limit "+side);Capture("merged-"+side);
            }
            app.ShowTitle();yield return Ready();app.Settings();yield return Ready();CheckSettingsOrder();Capture("settings-order");
            yield return Click("music-credits");Require(app.Page==GamePage.Confirming,"music attribution still opens after reordering");yield return Click("music-credits-back");CheckSettingsOrder();yield return Click("back");Require(app.Page==GamePage.Title,"bottom settings action returns to the title");
            app.Progress.UnlockCampaign();app.LoadLevel("c1_01");yield return Ready();app.Pause();yield return Ready();yield return Click("settings");CheckSettingsOrder();Capture("pause-settings-order");yield return Click("back");Require(app.Page==GamePage.Paused,"pause settings preserve their return destination");
            yield return DeathPolishChecks();
        }
        private void CheckSettingsOrder()
        {
            var credits=(RectTransform)app.UI.FindButton("music-credits").transform;var back=(RectTransform)app.UI.FindButton("back").transform;
            Require(credits.anchoredPosition.y>back.anchoredPosition.y,"music attribution is above the final back action");
        }
    }
}
