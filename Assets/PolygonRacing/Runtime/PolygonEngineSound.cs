using System;
using UnityEngine;

namespace PolygonRacing
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PolygonEngineSound : MonoBehaviour
    {
        volatile float targetFrequency=PolygonDrivetrain.IdleRpm*(2f/60f), targetGain;
        double phase;
        float frequency=PolygonDrivetrain.IdleRpm*(2f/60f), gain;
        float pitchRise, pitchFall, gainRise, gainFall;
        int sampleRate;
        AudioClip carrier;
        public void SetDriving(float rpm,float throttle,bool silent)
        {
            // Four-cylinder firing frequency: pitch and load use the authoritative drivetrain.
            targetFrequency=Mathf.Max(PolygonDrivetrain.IdleRpm,rpm)*(2f/60f);
            targetGain=silent?0:.025f+Mathf.Clamp01(throttle)*.045f;
        }
        void Awake()
        {
            sampleRate=AudioSettings.outputSampleRate;
            // Per-sample exponential smoothing uses seconds, independent of output rate.
            pitchRise=Coefficient(.08f);pitchFall=Coefficient(.12f);
            gainRise=Coefficient(.07f);gainFall=Coefficient(.10f);
            var source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=.65f;
            carrier=AudioClip.Create("Engine carrier",1024,1,sampleRate,false);source.clip=carrier;source.Play();
        }
        float Coefficient(float seconds) => sampleRate>0 ? 1f-(float)Math.Exp(-1.0/(sampleRate*seconds)) : 0;
        void OnAudioFilterRead(float[] data,int channels)
        {
            if(sampleRate<=0)return;
            float frequencyTarget=targetFrequency, gainTarget=targetGain;
            for(int i=0;i<data.Length;i+=channels)
            {
                frequency+=(frequencyTarget-frequency)*(frequencyTarget>frequency?pitchRise:pitchFall);
                gain+=(gainTarget-gain)*(gainTarget>gain?gainRise:gainFall);
                phase+=frequency/sampleRate;if(phase>=1)phase-=1;
                float v=(float)(Math.Sin(phase*Math.PI*2)*.6+Math.Sin(phase*Math.PI*4)*.27+Math.Sin(phase*Math.PI*8)*.13)*gain;
                for(int c=0;c<channels;c++)data[i+c]=v;
            }
        }
        void OnDestroy(){if(carrier)Destroy(carrier);}
    }
}
