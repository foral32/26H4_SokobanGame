using System;
using DraculasBox.Core;
using UnityEngine;

namespace DraculasBox.Runtime
{
    public sealed partial class GameAudio
    {
        public const float CircuitReplySeconds=.075f;
        private readonly AudioClip[,] circuitSounds=new AudioClip[4,2];
        private readonly double[] circuitDue={-1,-1,-1,-1};private readonly bool[] circuitOpening=new bool[4];
        public event Action<int,bool,bool> CircuitPlayed;
        private void BuildCircuitSounds()
        {
            double[] notes={392,440,523.251,587.330};const int sampleRate=22050;
            for(int channel=0;channel<4;channel++)for(int type=0;type<2;type++)
            {
                if(circuitSounds[channel,type]!=null)continue;
                float seconds=type==0?.115f:.15f;int count=(int)(seconds*sampleRate);var samples=new float[count];
                for(int i=0;i<count;i++)
                {
                    double t=i/(double)sampleRate,phase=t*notes[channel]*Math.PI*2;
                    double envelope=Math.Min(1,t/.004)*Math.Exp(-t/(type==0?.029:.043))*Math.Min(1,(seconds-t)/.012);
                    samples[i]=(float)((Math.Sin(phase)+Math.Sin(phase*2)*(type==0?.28:.14)+Math.Sin(phase*3)*.05)*envelope*.36);
                }
                var clip=AudioClip.Create((type==0?"Plate":"Door")+" channel "+(channel+1),count,1,sampleRate,false);
                clip.hideFlags=HideFlags.DontSave;clip.SetData(samples,0);circuitSounds[channel,type]=clip;
            }
        }
        public AudioClip CircuitSound(int channel,bool door)=>channel>=0&&channel<4?circuitSounds[channel,door?1:0]:null;
        public void CancelCircuitReplies(){for(int i=0;i<circuitDue.Length;i++)circuitDue[i]=-1;}
        public void Circuits(ActionResult result)
        {
            if(result==null||result.Kind==ActionKind.NoChange||result.Kind==ActionKind.Blocked)return;
            // History has its own cue. A pending reply must not outlive the state it described.
            CancelCircuitReplies();if(result.Kind==ActionKind.Undone||result.Kind==ActionKind.Reset)return;
            var level=result.After.Level;int plates=0,pressed=0,doors=0,opened=0;
            for(int i=0;i<level.PlateCount;i++)if(result.Before.PlatePressed(i)!=result.After.PlatePressed(i))
            {int bit=1<<level.PlateChannel(i);plates|=bit;if(result.After.PlatePressed(i))pressed|=bit;}
            for(int i=0;i<level.DoorCount;i++)if(result.Before.DoorOpen(i)!=result.After.DoorOpen(i))
            {int bit=1<<level.DoorChannel(i);doors|=bit;if(result.After.DoorOpen(i))opened|=bit;}
            for(int channel=0;channel<4;channel++)
            {
                int bit=1<<channel;bool plate=(plates&bit)!=0,door=(doors&bit)!=0;
                if(plate)PlayCircuit(channel,false,(pressed&bit)!=0);
                if(!door)continue;
                if(plate){circuitDue[channel]=AudioSettings.dspTime+CircuitReplySeconds;circuitOpening[channel]=(opened&bit)!=0;}
                else PlayCircuit(channel,true,(opened&bit)!=0);
            }
        }
        private void UpdateCircuitReplies()
        {
            double now=AudioSettings.dspTime;
            for(int channel=0;channel<4;channel++)if(circuitDue[channel]>=0&&now>=circuitDue[channel])
            {circuitDue[channel]=-1;PlayCircuit(channel,true,circuitOpening[channel]);}
        }
        private void PlayCircuit(int channel,bool door,bool active)
        {
            var clip=circuitSounds[channel,door?1:0];if(effects==null||clip==null)return;
            effects.PlayOneShot(clip,active?.65f:.43f);CircuitPlayed?.Invoke(channel,door,active);
        }
        private void OnDestroy()
        {
            CancelCircuitReplies();CircuitPlayed=null;
            foreach(var clip in circuitSounds)if(clip!=null)Destroy(clip);
        }
    }
}
