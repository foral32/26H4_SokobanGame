using System;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameUI
    {
        private void MapPager(int page,int count,int perPage,Action<int> jump)
        {
            int last=(count-1)/perPage;
            if(last==0)return;
            if(page>0)MenuAction("map-prev","上一页",new Vector2(-95,-295),138,()=>jump(page-1),GameIcon.Back);
            if(page<last)MenuAction("map-next","下一页",new Vector2(215,-295),138,()=>jump(page+1),GameIcon.Arrow);
            Text("Map page",(page+1)+" / "+(last+1),new Vector2(65,-295),new Vector2(98,38),18,Quiet);
        }
    }
}
