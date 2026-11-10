using System;
using DraculasBox.Core;

namespace DraculasBox.Runtime
{
    // The Unity Editor assembly supplies the production-content writer. Players have no provider.
    public interface ICampaignWorkshop
    {
        void Show(bool returnToMap);
        void Back();
        bool HandleKeys(IStudyKeyboard keyboard);
        bool EditingBoard { get; }
        void StageBoard(LevelData data);
        void RecoverBoard(LevelData data);
        void CloseBoard(bool discard);
        void StartTrial(LevelData data);
        void ObserveTrial(ActionResult step);
    }

    public sealed partial class DraculaApp
    {
        public static Func<DraculaApp, ICampaignWorkshop> CreateCampaignWorkshop { get; set; }
        public ICampaignWorkshop Campaign { get; private set; }
        public bool EditingOfficial => Campaign?.EditingBoard == true;

        public void OpenCampaign()=>OpenCampaign(false);
        private void OpenCampaign(bool returnToMap)
        {
            if (CreateCampaignWorkshop == null) return;
            Navigate(() =>
            {
                try
                {
                    Campaign ??= CreateCampaignWorkshop(this);
                    Page = GamePage.Campaign;
                    Session = null;
                    Board.Clear();
                    Campaign.Show(returnToMap);
                }
                catch (Exception e) { OperationError("无法打开正式关卡", e, ShowTitle); }
            });
        }

        public void OpenCampaignBoard(LevelData data, LevelData baseline, bool trial=false)
        {
            Navigate(() =>
            {
                Page = GamePage.Editing;
                creator.Open(data, baseline);
                if(trial){creatorPlay=true;Campaign.StartTrial(data);Enter(data);}
            });
        }

        private void CloseBoard(bool discard)
        {
            try
            {
                if (EditingOfficial)
                {
                    Campaign.CloseBoard(discard);
                    OpenCampaign(true);
                    return;
                }
                if (discard) Library.DiscardRecovery(creator.Document.Data.id);
                OpenLibrary();
            }
            catch (Exception e) { OperationError("无法离开关卡", e, CancelLeave); }
        }

        public void RecoverBoard(LevelData data)
        {
            if (EditingOfficial) Campaign.RecoverBoard(data);
            else Library.Recover(data);
        }
    }
}
