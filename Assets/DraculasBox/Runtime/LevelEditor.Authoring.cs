using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DraculasBox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DraculasBox.Runtime
{
    public sealed partial class LevelEditor
    {
        private enum AuthoringTool { None, Solutions, Generator, Playback, Notes }
        private AuthoringTool tool;
        private CancellationTokenSource toolWork;
        private TMP_Text toolMessage;
        private string toolNote;
        private LevelData proofBoard, candidate;
        private int proofPushes;
        private bool shortest, extendSearch, generatorFromLibrary;
        private DraftMechanic allowed=DraftMechanic.Sun;
        private DraftComplexity complexity=DraftComplexity.Intro;
        private LevelThumbnail draftPreview;
        private Button playSolution;
        private Session playback;
        private string playbackRoute;
        private bool autoPlay;
        private float nextStep;
        private static readonly DraftMechanic[] Mechanisms={DraftMechanic.Sun,DraftMechanic.Lamp,DraftMechanic.Circuit,DraftMechanic.Mirror};
        private bool ToolOpen=>tool!=AuthoringTool.None;

        private void CancelWork()
        {
            var previous=toolWork;toolWork=null;previous?.Cancel();
        }
        internal void ResetAuthoringTool()
        {
            app?.CancelCircuitReplies();
            CancelWork();tool=AuthoringTool.None;generatorFromLibrary=false;
            candidate=null;playback=null;playbackRoute=null;toolMessage=null;playSolution=null;draftPreview=null;
        }
        public bool CloseAuthoringTool()
        {
            if(!ToolOpen)return false;
            bool library=generatorFromLibrary;
            ResetAuthoringTool();
            if(library)app.OpenLibrary();else Show();
            return true;
        }
        private void BeginTool(AuthoringTool value)
        {
            if(app.Page==GamePage.Editing){CommitPending();ClosePanels();board.BrushPreview(null,Selected,true);}
            ResetAuthoringTool();tool=value;toolNote="";
        }
        private bool CurrentJob(CancellationTokenSource request)
        {
            return this!=null&&isActiveAndEnabled&&toolWork==request&&app!=null&&
                app.Page==(generatorFromLibrary?GamePage.Library:GamePage.Editing);
        }
        private async void RunToolJob<T>(Func<CancellationToken,T> work,Action<T> accept,string busy)
        {
            CancelWork();
            var request=new CancellationTokenSource();toolWork=request;
            var token=request.Token;toolNote=busy;RefreshTool();
            try
            {
                var result=await Task.Run(()=>work(token),token);
                if(CurrentJob(request))accept(result);
            }
            catch(OperationCanceledException) when(token.IsCancellationRequested){}
            catch(Exception e){if(CurrentJob(request))toolNote=e.Message;}
            finally
            {
                if(this!=null&&toolWork==request){toolWork=null;if(ToolOpen)RefreshTool();}
                request.Dispose();
            }
        }
        private TMP_Text ToolText(Transform root,string name,string text,int x,int y,int width,int height,int size,bool muted=false)
        {
            var label=ui.Text(name,text,new Vector2(x,y),new Vector2(width,height),size,
                muted?GameUI.Muted:GameUI.WorkshopInk,TextAlignmentOptions.MidlineLeft);
            label.transform.SetParent(root,false);return label;
        }
        private RectTransform ToolSheet(string title,int width,int height)
        {
            var sheet=ui.WorkshopOverlay(title,Vector2.zero,new Vector2(width,height),()=>CloseAuthoringTool());
            ToolText(sheet,"Heading",title,-22,height/2-35,width-100,42,26);
            ui.EditorButton("close-authoring","×",new Vector2(width/2-38,height/2-35),new Vector2(38,36),()=>CloseAuthoringTool(),false,sheet,true);
            return sheet;
        }
        public void OpenSolutions()
        {
            BeginTool(AuthoringTool.Solutions);
            var sheet=ToolSheet("关卡解",480,204);
            toolMessage=ToolText(sheet,"Solution status","",0,4,420,64,20,true);
            ui.EditorButton("solve-level","计算关卡解",new Vector2(-111,-60),new Vector2(198,42),Calculate,false,sheet,true);
            playSolution=ui.EditorButton("play-solution","播放关卡解",new Vector2(111,-60),new Vector2(198,42),StartPlayback,true,sheet,true);
            var data=Document.Data;
            bool cached=proofBoard!=null&&data.LayoutEquals(proofBoard)&&data.solution==proofBoard.solution;
            if(!cached){proofBoard=null;extendSearch=shortest=false;}
            toolNote=Document.Validation();
            if(toolNote==null&&!cached&&!string.IsNullOrEmpty(data.solution))
            {
                var snapshot=data.Copy();
                RunToolJob(token=>LevelSolver.Proves(snapshot,snapshot.solution,out int count,token)?count:-1,count=>
                {
                    if(!Document.Data.ContentEquals(snapshot)){toolNote="棋盘已更改，请重新计算。";return;}
                    if(count<0){Document.DiscardSolution();toolNote="原解已失效";return;}
                    proofBoard=snapshot;proofPushes=count;toolNote="";
                },"正在校验已存解…");
            }
            else RefreshTool();
            ui.FocusButton("solve-level");
        }
        private void Calculate()
        {
            if(toolWork!=null){CancelWork();toolNote="已取消";RefreshTool();return;}
            var snapshot=Document.Data.Copy();
            int states=extendSearch?180000:65000;
            int ms=extendSearch?18000:6000;
            RunToolJob(token=>LevelSolver.Solve(snapshot,token,states,ms),result=>
            {
                if(!Document.Data.ContentEquals(snapshot)){toolNote="棋盘已更改，请重新计算。";return;}
                extendSearch=result.Outcome==SearchOutcome.Limit;
                if(result.Outcome==SearchOutcome.Solved)
                {
                    Document.ApplySearchResult(result);
                    proofBoard=Document.Data.Copy();proofPushes=result.Pushes;shortest=true;toolNote="";
                }
                else if(extendSearch)toolNote=proofBoard==null?"达到搜索上限，尚未判定":"已有关卡解，最短步数待定";
                else toolNote=result.Outcome==SearchOutcome.Unsolvable?"此关无解":result.Error;
            },"正在计算…");
        }
        public void OpenGenerator(bool fromLibrary=false)
        {
            BeginTool(AuthoringTool.Generator);generatorFromLibrary=fromLibrary;
            var sheet=ToolSheet("草稿预设",752,438);
            ToolText(sheet,"Mechanics","关卡机制",-198,103,296,32,20);
            string[] names={"阳光","四向灯","压力板与门","斜镜"};
            for(int i=0;i<Mechanisms.Length;i++)
            {
                var mechanism=Mechanisms[i];
                ui.EditorButton("mechanic-"+i,names[i],new Vector2(-277+i%2*146,60-i/2*48),
                    new Vector2(134,38),()=>ToggleMechanic(mechanism),false,sheet,true);
            }
            ToolText(sheet,"Difficulty","难度",-198,-52,296,30,20);
            string[] grades={"Ⅰ","Ⅱ","Ⅲ"};
            for(int i=0;i<grades.Length;i++)
            {
                var grade=(DraftComplexity)i;
                ui.EditorButton("complexity-"+i,grades[i],new Vector2(-302+i*97,-95),new Vector2(88,38),
                    ()=>{if(complexity!=grade){complexity=grade;GenerateDraft();}},false,sheet,true);
            }
            ui.WorkshopPanel("Preview surface",new Vector2(162,22),new Vector2(365,254),sheet).color=BoardView.SafeColor;
            draftPreview=ui.Thumbnail("Draft preview",null,new Vector2(162,22),new Vector2(354,246),sheet);
            toolMessage=ToolText(sheet,"Draft status","",162,-129,365,32,18,true);
            ui.EditorButton("reroll","换一稿",new Vector2(83,-178),new Vector2(120,42),GenerateDraft,false,sheet,true);
            ui.EditorButton("adopt-draft",fromLibrary?"采用草稿":"替换当前棋盘",new Vector2(251,-178),new Vector2(186,42),AdoptDraft,true,sheet,true);
            ui.FocusButton("reroll");GenerateDraft();
        }
        private void ToggleMechanic(DraftMechanic mechanism)
        {
            allowed^=mechanism;
            if(mechanism==DraftMechanic.Mirror&&allowed.HasFlag(mechanism))allowed|=DraftMechanic.Lamp;
            if(mechanism==DraftMechanic.Lamp&&!allowed.HasFlag(mechanism))allowed&=~DraftMechanic.Mirror;
            GenerateDraft();
        }
        private void GenerateDraft()
        {
            var options=allowed;
            CancelWork();
            var grade=complexity;int seed=Guid.NewGuid().GetHashCode();
            string previous=candidate?.title??(generatorFromLibrary?null:Document?.Data.title);
            candidate=null;draftPreview.Set(null);
            if(DraftGenerator.Choices(options,grade).Length==0)
            {toolNote="暂无匹配草稿";RefreshTool();return;}
            RunToolJob(token=>DraftGenerator.Generate(options,grade,seed,token,previous),data=>
            {
                candidate=data;draftPreview.Set(data);
                var preset=DraftPresets.All.First(p=>p.Title==data.title);
                toolNote=preset.Title+"  ·  "+preset.Difficulty;
            },"正在载入…");
        }
        private void AdoptDraft()
        {
            if(candidate==null||toolWork!=null)return;
            var draft=candidate;
            if(generatorFromLibrary){ResetAuthoringTool();app.Edit(draft);return;}
            Document.ApplyGenerated(draft);proofBoard=null;
            board.ResetEditorView();CloseAuthoringTool();
        }
        private void RefreshTool()
        {
            if(toolMessage==null)return;
            bool busy=toolWork!=null;
            if(tool==AuthoringTool.Solutions)
            {
                bool ready=proofBoard!=null&&!string.IsNullOrEmpty(Document.Data.solution);
                string summary=ready?(shortest?"最短 ":"")+Document.Data.solution.Length+" 步 · 推动 "+proofPushes+" 次":"尚未计算";
                toolMessage.text=string.IsNullOrEmpty(toolNote)?summary:toolNote;
                playSolution.gameObject.SetActive(ready);
                ui.SetEnabled("play-solution",ready&&!busy);
                ui.SetLabel("solve-level",busy?"取消计算":extendSearch?"扩大搜索":"计算关卡解");
                ui.SetEnabled("solve-level",busy||Document.Validation()==null);
            }
            else if(tool==AuthoringTool.Generator)
            {
                toolMessage.text=toolNote;
                for(int i=0;i<Mechanisms.Length;i++)ui.WorkshopChoice("mechanic-"+i,allowed.HasFlag(Mechanisms[i]));
                for(int i=0;i<3;i++)
                {
                    ui.SetEnabled("complexity-"+i,true);
                    ui.WorkshopChoice("complexity-"+i,(int)complexity==i);
                }
                bool variety=DraftGenerator.Choices(allowed,complexity).Length>1;
                ui.SetLabel("reroll",candidate==null||variety?"换一稿":"仅此一稿");
                ui.SetEnabled("reroll",!busy&&variety);ui.SetEnabled("adopt-draft",!busy&&candidate!=null);
            }
        }
        private void StartPlayback()
        {
            if(toolWork!=null||proofBoard==null||!Document.Data.LayoutEquals(proofBoard)||Document.Data.solution!=proofBoard.solution)return;
            tool=AuthoringTool.Playback;playbackRoute=Document.Data.solution;
            playback=new Session(new Definition(Document.Data));board.Load(playback.State);
            ui.Clear();ui.WorkshopTheme();
            ui.EditorButton("stop-playback","返回编辑",new Vector2(-532,307),new Vector2(150,42),()=>CloseAuthoringTool(),false,null,true);
            ui.Text("Playback heading","关卡解",new Vector2(0,307),new Vector2(360,46),27,GameUI.WorkshopInk);
            ui.EditorButton("previous-step","上一步",new Vector2(-174,-305),new Vector2(118,42),()=>StepPlayback(true),false,null,true);
            ui.EditorButton("play-pause","暂停",new Vector2(0,-305),new Vector2(180,42),TogglePlayback,true,null,true);
            ui.EditorButton("next-step","下一步",new Vector2(174,-305),new Vector2(118,42),()=>StepPlayback(false),false,null,true);
            toolMessage=ui.Text("Playback steps","",new Vector2(0,-250),new Vector2(500,36),20,GameUI.WorkshopInk);
            autoPlay=true;nextStep=Time.unscaledTime+.45f;RefreshPlayback();ui.FocusButton("play-pause");
        }
        private void TogglePlayback()
        {
            app.CancelCircuitReplies();
            if(playback.State.Won){playback=new Session(playback.State.Level);board.Load(playback.State);autoPlay=true;}
            else autoPlay=!autoPlay;
            nextStep=Time.unscaledTime+.32f;RefreshPlayback();
        }
        private void StepPlayback(bool previous)
        {
            autoPlay=false;
            if(previous&&playback.UndoCount>0){var undo=playback.Apply(Command.Undo);board.Present(undo);app.CueCircuits(undo);}
            else if(!previous&&!playback.State.Won)AdvancePlayback();
            RefreshPlayback();
        }
        private void AdvancePlayback()
        {
            var command=(Command)LevelSolver.Directions.IndexOf(playbackRoute[playback.Moves]);
            var move=playback.Apply(command);board.Present(move);
            app.CueCircuits(move);
            app.Cue(playback.State.Won?"goal":move.PushedCount>0?"impactWood_heavy_001":"footstep_concrete_003");
            nextStep=Time.unscaledTime+.32f;
            if(playback.State.Won)autoPlay=false;
            RefreshPlayback();
        }
        private void RefreshPlayback()
        {
            toolMessage.text=playback.Moves+" / "+playbackRoute.Length+" 步";
            ui.SetLabel("play-pause",playback.State.Won?"重新播放":autoPlay?"暂停":"继续");
            ui.SetEnabled("previous-step",playback.UndoCount>0);ui.SetEnabled("next-step",!playback.State.Won);
        }
        private void UpdateAuthoringTool()
        {
            if(tool==AuthoringTool.Playback&&autoPlay&&Time.unscaledTime>=nextStep)AdvancePlayback();
            if(app.Automated||!Application.isFocused)return;
            if(Input.GetKeyDown(KeyCode.Escape)){CloseAuthoringTool();return;}
            if(ui.HasTextFocus)return;
            if(tool==AuthoringTool.Playback)
            {
                if(Input.GetKeyDown(KeyCode.Space))TogglePlayback();
                else if(Input.GetKeyDown(KeyCode.LeftArrow))StepPlayback(true);
                else if(Input.GetKeyDown(KeyCode.RightArrow))StepPlayback(false);
                return;
            }
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))ui.Handle(StudyAction.Confirm);
            else if(Input.GetKeyDown(KeyCode.LeftArrow))ui.Handle(StudyAction.Left);
            else if(Input.GetKeyDown(KeyCode.RightArrow))ui.Handle(StudyAction.Right);
            else if(Input.GetKeyDown(KeyCode.UpArrow))ui.Handle(StudyAction.Up);
            else if(Input.GetKeyDown(KeyCode.DownArrow))ui.Handle(StudyAction.Down);
        }
    }
}
