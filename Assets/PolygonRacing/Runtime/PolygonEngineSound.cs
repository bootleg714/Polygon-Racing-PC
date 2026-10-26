using System;
using UnityEngine;

namespace PolygonRacing
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PolygonEngineSound : MonoBehaviour
    {
        volatile float targetFrequency=48, targetGain=.025f;
        double phase;
        float frequency=48, gain;
        int sampleRate;
        AudioClip carrier;
        public void SetDriving(float speed,float throttle){targetFrequency=48+speed*150+throttle*18;targetGain=.025f+throttle*.045f+speed*.025f;}
        void Awake()
        {
            sampleRate=AudioSettings.outputSampleRate;
            var source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=.65f;
            carrier=AudioClip.Create("Engine carrier",1024,1,sampleRate,false);source.clip=carrier;source.Play();
        }
        void OnAudioFilterRead(float[] data,int channels)
        {
            if(sampleRate<=0)return;
            for(int i=0;i<data.Length;i+=channels)
            {
                frequency+=(targetFrequency-frequency)*.0005f;gain+=(targetGain-gain)*.0005f;
                phase+=frequency/sampleRate;if(phase>=1)phase-=1;
                float v=(float)(Math.Sin(phase*Math.PI*2)*.6+Math.Sin(phase*Math.PI*4)*.27+Math.Sin(phase*Math.PI*8)*.13)*gain;
                for(int c=0;c<channels;c++)data[i+c]=v;
            }
        }
        void OnDestroy(){if(carrier)Destroy(carrier);}
    }
}
