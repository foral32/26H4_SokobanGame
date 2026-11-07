#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BoxOrVoid.Study
{
    /// <summary>Opt-in capture of the actual camera and listener; no desktop or system audio capture.</summary>
    public sealed class StudyFilm:MonoBehaviour
    {
        private readonly List<float> audio=new List<float>();
        private readonly List<string> frames=new List<string>();
        private readonly List<double> times=new List<double>();
        private readonly object audioLock=new object();
        private Camera camera;
        private string folder;
        private RenderTexture target;
        private Texture2D image;
        private volatile bool recording;
        private int channels=2,sampleRate;
        private double started,nextFrame;

        public void Begin(Camera source,string output)
        {
            camera=source;folder=Path.GetFullPath(output);Directory.CreateDirectory(folder);
            target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            sampleRate=AudioSettings.outputSampleRate;started=Time.realtimeSinceStartupAsDouble;nextFrame=started;recording=true;
            StartCoroutine(CaptureFrames());
        }
        private IEnumerator CaptureFrames()
        {
            while(recording)
            {
                yield return null;
                double now=Time.realtimeSinceStartupAsDouble;if(now<nextFrame) continue;nextFrame=now+1.0/30;
                var prior=camera.targetTexture;var active=RenderTexture.active;
                try
                {
                    camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    string filename="frame_"+frames.Count.ToString("D5")+".png";
                    File.WriteAllBytes(Path.Combine(folder,filename),image.EncodeToPNG());frames.Add(filename);times.Add(now-started);
                }
                finally {camera.targetTexture=prior;RenderTexture.active=active;}
            }
        }
        private void OnAudioFilterRead(float[] samples,int channelCount)
        {
            if(!recording) return;
            lock(audioLock) {channels=channelCount;if(audio.Count<sampleRate*channelCount*60) audio.AddRange(samples);}
        }
        public void Finish()
        {
            recording=false;StopAllCoroutines();double end=Time.realtimeSinceStartupAsDouble-started;
            using(var writer=new StreamWriter(Path.Combine(folder,"frames.ffconcat")))
            {
                writer.WriteLine("ffconcat version 1.0");
                for(int i=0;i<frames.Count;i++)
                {writer.WriteLine("file '"+frames[i]+"'");writer.WriteLine("duration "+((i+1<times.Count?times[i+1]:end)-times[i]).ToString("F6",CultureInfo.InvariantCulture));}
                if(frames.Count>0) writer.WriteLine("file '"+frames[frames.Count-1]+"'");
            }
            lock(audioLock)
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,"audio.wav"))))
            {
                int bytes=audio.Count*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(bytes+36);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(sampleRate);writer.Write(sampleRate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);foreach(float sample in audio) writer.Write((short)(Mathf.Clamp(sample,-1,1)*32767));
            }
            File.WriteAllText(Path.Combine(folder,"capture.json"),JsonUtility.ToJson(new Record{frames=frames.Count,seconds=end,audioSamples=audio.Count,sampleRate=sampleRate,channels=channels},true));
            target.Release();Destroy(target);Destroy(image);
        }
        [Serializable] private sealed class Record {public int frames,audioSamples,sampleRate,channels;public double seconds;}
    }
}
#endif
