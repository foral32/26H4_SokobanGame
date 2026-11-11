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
        public void BeginCharacterJuice(DraculaApp host)
        {
            app=host;app.SetTestKeyboard(this);report.runtime="Unity Editor character feedback";report.width=Screen.width;report.height=Screen.height;
            folder=Path.GetFullPath("DraculasBox/Reviews/CharacterJuice-20261111/Checks");Directory.CreateDirectory(folder);
            Application.logMessageReceived+=Log;StartCoroutine(Guard(CharacterJuice()));
        }
        private SpriteRenderer HeroForCheck()=>app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Vampire"&&r.gameObject.activeSelf);
        private SpriteRenderer BoxForCheck(Cell cell)
        {int i=app.Board.State.At(cell);return app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Piece "+i&&r.gameObject.activeSelf);}
        private int EyeCountForCheck(Sprite sprite)
        {
            var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height,0,RenderTextureFormat.ARGB32);
            var copy=new Texture2D((int)sprite.textureRect.width,(int)sprite.textureRect.height,TextureFormat.RGBA32,false);
            try
            {
                Graphics.Blit(sprite.texture,rt);RenderTexture.active=rt;copy.ReadPixels(sprite.textureRect,0,0);var pixels=copy.GetPixels();int runs=0;bool prior=false;
                for(int x=0;x<copy.width;x++)
                {
                    bool dark=false;for(int y=copy.height/5;y<copy.height*4/5;y++){var p=pixels[y*copy.width+x];if(p.a>.9f&&p.grayscale<.3f){dark=true;break;}}
                    if(dark&&!prior)runs++;prior=dark;
                }
                return runs;
            }
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Destroy(copy);}
        }
        private IEnumerator CharacterJuice()
        {
            string profile=Directory.GetParent(app.Library.Folder).FullName;
            Require(app.Automated&&profile.StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),"character checks use isolated storage");report.progressIsolated=true;
            yield return Ready();
            var eyes=app.UI.AuthoredTitle.GetComponentInChildren<TitleEyesGraphic>();
            Require(eyes!=null&&!eyes.raycastTarget,"approved logo has nonblocking animated eyes");
            yield return new WaitForSecondsRealtime(.2f);float firstLook=eyes.Gaze.y;
            yield return Tap(KeyCode.DownArrow);
            Require(app.UI.SelectedId=="creator"&&eyes.Gaze.y<firstLook-.2f,"keyboard selection moves the logo gaze toward its menu option");
            Capture("title-look");
            app.UI.HoverForCheck("campaign");yield return new WaitForSecondsRealtime(.2f);
            Require(app.UI.SelectedId=="campaign"&&eyes.Gaze.x>1,"pointer focus moves the gaze toward the editor entry");
            Capture("title-look-right");
            eyes.SetPointerForCheck(new Vector2(Screen.width*.18f,Screen.height*.85f));yield return new WaitForSecondsRealtime(.25f);
            Require(eyes.Gaze.x<-4&&eyes.Gaze.y>3,"eyes follow the pointer over empty upper-left space, outside all menu buttons");Capture("title-mouse-left");
            eyes.SetPointerForCheck(new Vector2(Screen.width*.82f,Screen.height*.85f));yield return new WaitForSecondsRealtime(.25f);
            Require(eyes.Gaze.x>4&&eyes.Gaze.y>3,"eyes follow the pointer across empty upper-right space");Capture("title-mouse-right");
            yield return Tap(KeyCode.UpArrow);Require(eyes.Gaze.y<0,"keyboard selection takes over from a stationary pointer");eyes.SetPointerForCheck(null);
            float until=Time.realtimeSinceStartup+6;bool blink=false;
            while(Time.realtimeSinceStartup<until)
            {if(eyes.Openness<.22f){blink=true;Capture("title-blink");break;}yield return null;}
            Require(blink,"logo blinks naturally without an input event");
            yield return new WaitForSecondsRealtime(.2f);Require(eyes.Openness>.9f,"eyes reopen after the blink");
            foreach(var theme in GameThemes.All)
            {
                GameThemes.Apply(theme.Id);app.ShowTitle();yield return Ready();
                var art=app.UI.AuthoredTitle.GetComponentInChildren<TitleArtworkGraphic>();eyes=app.UI.AuthoredTitle.GetComponentInChildren<TitleEyesGraphic>();
                Require(art.EyeBounds(out var left,out var right)&&left.width>5&&right.x>left.x&&eyes!=null,"eyes remain aligned with approved artwork in "+theme.Id);
                Require(app.UI.FindButton("campaign")!=null,"animated logo preserves formal editor entry in "+theme.Id);
                Capture("title-"+theme.Id);if(theme.Id=="wine")Capture("title-wine-4x3",1024,768);
            }
            GameThemes.Apply(GameThemes.DefaultThemeId);app.EditNew();yield return Ready();
            var editor=app.Creator;var doc=editor.Document;var initial=doc.Data.Copy();var point=new Cell(4,2);
            var screen=app.Board.Screen(point);editor.Select(Brush.Box);editor.PointerSample(screen,true,true,false);editor.PointerSample(screen,false,false,true);editor.FlushPreview();
            yield return new WaitForSecondsRealtime(.05f);
            Require(BoxForCheck(point).transform.localScale.y<.89f,"point placement visibly compresses the new box");
            Require(doc.UndoCount==1&&doc.Data.pieces.Length==initial.pieces.Length+1,"point feedback adds no document transaction");Capture("stamp-point");
            yield return new WaitForSecondsRealtime(.28f);Require(Mathf.Abs(BoxForCheck(point).transform.localScale.y-.9f)<.001f,"point stamp settles to the exact original size");
            var once=doc.Data.Copy();int history=doc.UndoCount;screen=app.Board.Screen(doc.Data.player);
            Require(!editor.PointerSample(screen,true,true,false),"occupied placement is rejected");editor.PointerSample(screen,false,false,true);editor.FlushPreview();
            Require(doc.Data.ContentEquals(once)&&doc.UndoCount==history&&Mathf.Abs(BoxForCheck(point).transform.localScale.y-.9f)<.001f,"failed placement changes neither history nor existing box scale");
            editor.SelectShape(PaintShape.Rectangle);var a=app.Board.Screen(new Cell(4,2));var b=app.Board.Screen(new Cell(6,4));
            editor.PointerSample(a,true,true,false);editor.PointerSample(b,false,true,false);editor.PointerSample(b,false,false,true);
            yield return new WaitForSecondsRealtime(.05f);
            Require(doc.UndoCount==history+1&&doc.Data.pieces.Length==9,"rectangle is still one complete edit");
            Require(BoxForCheck(new Cell(6,4)).transform.localScale.y<.89f,"rectangle placements share a visible stamp beat");
            Require(Mathf.Abs(BoxForCheck(new Cell(5,3)).transform.localScale.y-.9f)<.001f&&Mathf.Abs(BoxForCheck(point).transform.localScale.y-.9f)<.001f,"skipped preexisting boxes do not play placement feedback");
            Require(app.Board.CellAt(app.Board.Screen(new Cell(6,4)),out var picked)&&picked==new Cell(6,4),"stamp deformation does not change cell picking");Capture("stamp-rectangle");
            editor.Undo();yield return null;Require(doc.Data.ContentEquals(once)&&Mathf.Abs(BoxForCheck(point).transform.localScale.y-.9f)<.001f,"undo clears the stamp and restores the complete previous draft");
            editor.SelectShape(PaintShape.Brush);var start=new Cell(2,1);var end=new Cell(3,1);var beforeStroke=doc.Data.Copy();history=doc.UndoCount;
            editor.PointerSample(app.Board.Screen(start),true,true,false);editor.FlushPreview();yield return new WaitForSecondsRealtime(.04f);
            editor.PointerSample(app.Board.Screen(end),false,true,false);editor.FlushPreview();yield return null;
            Require(BoxForCheck(start).transform.localScale.y<.899f,"the previous stamp survives the next preview rebuild");editor.PointerSample(app.Board.Screen(end),false,false,true);
            Require(doc.UndoCount==history+1,"continuous stamped painting remains one stroke");editor.Undo();yield return null;Require(doc.Data.ContentEquals(beforeStroke),"one undo restores a continuously stamped stroke");
            Require(editor.Save()&&!doc.Dirty,"saving a stamped draft still succeeds");yield return new WaitForSecondsRealtime(1);
            Capture("editor-rest-4x3",1024,768);
            for(int face=0;face<4;face++)
            {
                var level=PushPoseLevel(face,false);app.StartEditorPlay(level);yield return Ready();string before=app.Session.State.Key();
                Press(Key("URDL"[face]));yield return null;yield return null;key=KeyCode.None;
                Require(!HeroForCheck().sprite.name.Contains("effort")&&!HeroForCheck().sprite.name.Contains("squeezed"),"pushing retains the original face in direction "+face);
                Require(EyeCountForCheck(HeroForCheck().sprite)==2,"pushing preserves two separate readable eyes in direction "+face);
                Require(app.Session.Moves==1&&app.Session.State.Player==level.player+LightField.Directions[face],"effort leaves movement rules unchanged in direction "+face);
                Capture("push-"+face);yield return new WaitForSecondsRealtime(.25f);yield return Ready();
                Require(!HeroForCheck().sprite.name.Contains("effort"),"effort relaxes after pushing in direction "+face);
                yield return Tap(KeyCode.Z);Require(app.Session.State.Key()==before,"undo restores the exact pushing state in direction "+face);
                level=PushPoseLevel(face,true);app.StartEditorPlay(level);yield return Ready();before=app.Session.State.Key();
                Press(Key("URDL"[face]));yield return null;yield return null;key=KeyCode.None;
                Require(!HeroForCheck().sprite.name.Contains("squeezed"),"blocked crate retains the original face in direction "+face);
                Require(EyeCountForCheck(HeroForCheck().sprite)==2,"blocked expression preserves two separate eyes in direction "+face);
                var fangs=app.Board.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Fangs"&&r.gameObject.activeSelf);
                Require(fangs.enabled==(face!=0),"blocked push retains the original fang visibility in direction "+face);
                Require(app.Session.State.Key()==before&&app.Session.Moves==0,"blocked expression changes no model state in direction "+face);Capture("blocked-"+face);
                yield return new WaitForSecondsRealtime(.27f);Require(!HeroForCheck().sprite.name.Contains("squeezed"),"blocked expression settles in direction "+face);
                yield return Tap(KeyCode.R);Require(!HeroForCheck().sprite.name.Contains("squeezed"),"reset leaves no expression residue in direction "+face);
            }
            yield return DeathMotionSmoke();
            app.StartEditorPlay(app.assets.Level("c1_01"));yield return Ready();foreach(char c in "RRRUU")yield return Tap(Key(c));
            yield return Page(GamePage.Editing);Require(!HeroForCheck().sprite.name.Contains("effort")&&!HeroForCheck().sprite.name.Contains("squeezed"),"coffin completion returns to an editor with a neutral face");
            report.editorRoundtrip=true;
        }
        private static LevelData PushPoseLevel(int face,bool blocked)
        {
            var data=LevelData.Blank(7,7);data.player=new Cell(3,3);data.coffin=new Cell(5,5);var direction=LightField.Directions[face];
            data.pieces=new[]{new EntityData{id="crate",kind=PieceKind.Box,cell=data.player+direction}};
            if(blocked){var stop=data.player+direction+direction;var row=data.rows[stop.y].ToCharArray();row[stop.x]='#';data.rows[stop.y]=new string(row);}
            return data;
        }
    }
}
