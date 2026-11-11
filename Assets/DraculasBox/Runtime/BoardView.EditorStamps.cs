using System.Collections.Generic;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class BoardView
    {
        public readonly struct StampCell
        {
            public readonly Cell cell;public readonly Brush brush;
            public StampCell(Cell cell,Brush brush){this.cell=cell;this.brush=brush;}
        }
        private struct StampRequest {public Brush brush;public double at;}
        private struct StampVisual {public SpriteRenderer renderer;public Vector3 position,scale,anchor;public double at;public float weight;}
        private const float StampSeconds=.22f;
        private readonly Dictionary<Cell,StampRequest> stampRequests=new Dictionary<Cell,StampRequest>();
        private readonly List<StampVisual> stampVisuals=new List<StampVisual>();
        private readonly List<Cell> expiredStamps=new List<Cell>();
        private int[] terrainVisualStarts,terrainVisualCounts,plateVisualStarts,plateVisualCounts,doorVisualStarts,doorVisualCounts;
        private void RestoreStampTransforms()
        {
            foreach(var item in stampVisuals)if(item.renderer!=null){item.renderer.transform.localPosition=item.position;item.renderer.transform.localScale=item.scale;}
        }
        public void ClearEditorStamps(){RestoreStampTransforms();stampVisuals.Clear();stampRequests.Clear();}
        public void StampCells(IReadOnlyList<StampCell> cells)
        {
            if(!editing||state==null||cells.Count==0)return;
            RestoreStampTransforms();double now=Time.realtimeSinceStartupAsDouble;expiredStamps.Clear();
            foreach(var item in stampRequests)if(now-item.Value.at>=StampSeconds)expiredStamps.Add(item.Key);
            foreach(var cell in expiredStamps)stampRequests.Remove(cell);
            foreach(var item in cells)if(state.Level.Inside(item.cell)&&item.brush!=Brush.Erase)stampRequests[item.cell]=new StampRequest{brush=item.brush,at=now};
            BindStampVisuals();
        }
        private void AddStamp(SpriteRenderer renderer,Cell cell,StampRequest request,float weight=1)
        {
            if(renderer==null)return;
            stampVisuals.Add(new StampVisual{renderer=renderer,position=renderer.transform.localPosition,scale=renderer.transform.localScale,anchor=Position(cell)+Vector3.down*.5f,at=request.at,weight=weight});
        }
        private void AddStampRange(int first,int count,Cell cell,StampRequest request,float weight=1)
        {for(int i=first;i<first+count;i++)AddStamp(renderers[i],cell,request,weight);}
        private void BindStampVisuals()
        {
            stampVisuals.Clear();if(!editing||state==null)return;
            foreach(var item in stampRequests)
            {
                var cell=item.Key;var request=item.Value;var brush=request.brush;var level=state.Level;
                if(!level.Inside(cell))continue;
                if(brush==Brush.Floor||brush==Brush.Wall||brush==Brush.Void)
                {
                    char terrain=brush==Brush.Floor?'.':brush==Brush.Wall?'#':' ';
                    if(level.Terrain(cell)==terrain){int i=cell.y*level.Width+cell.x;AddStampRange(terrainVisualStarts[i],terrainVisualCounts[i],cell,request,.36f);}
                }
                else if(brush==Brush.Player&&state.Player==cell){AddStamp(player,cell,request);AddStamp(cape,cell,request);AddStamp(fangs,cell,request);}
                else if(brush==Brush.Coffin&&level.Coffin==cell){AddStamp(coffin,cell,request);AddStamp(coffinExposure,cell,request);}
                else if(brush==Brush.Plate){for(int i=0;i<level.PlateCount;i++)if(level.PlateCell(i)==cell)AddStampRange(plateVisualStarts[i],plateVisualCounts[i],cell,request);}
                else if(brush==Brush.Door){for(int i=0;i<level.DoorCount;i++)if(level.DoorCell(i)==cell)AddStampRange(doorVisualStarts[i],doorVisualCounts[i],cell,request);}
                else
                {
                    int i=state.At(cell);if(i<0)continue;
                    var kind=brush==Brush.Box?PieceKind.Box:brush==Brush.Lamp?PieceKind.Lamp:brush==Brush.MirrorSlash?PieceKind.MirrorSlash:PieceKind.MirrorBackslash;
                    if(level.Kind(i)!=kind)continue;
                    AddStamp(pieces[i],cell,request);AddStamp(pieceShadows[i],cell,request);AddStamp(lampGlows[i],cell,request);AddStamp(mirrorRays[i],cell,request);
                }
            }
        }
        private void AnimateEditorStamps()
        {
            if(!editing||stampVisuals.Count==0)return;double now=Time.realtimeSinceStartupAsDouble;bool active=false;
            foreach(var item in stampVisuals)
            {
                float q=(float)(now-item.at)/StampSeconds;if(q>=1||item.renderer==null)continue;active=true;
                float squash=q<.30f?Mathf.Lerp(0,.14f,Mathf.SmoothStep(0,1,q/.30f)):q<.64f?Mathf.Lerp(.14f,-.055f,Mathf.SmoothStep(0,1,(q-.30f)/.34f)):Mathf.Lerp(-.055f,0,Mathf.SmoothStep(0,1,(q-.64f)/.36f));
                var scale=new Vector3(1+squash*.45f*item.weight,1-squash*item.weight,1);
                item.renderer.transform.localScale=Vector3.Scale(item.scale,scale);
                item.renderer.transform.localPosition=item.anchor+Vector3.Scale(item.position-item.anchor,scale);
            }
            if(!active){stampVisuals.Clear();stampRequests.Clear();}
        }
    }
}
