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
        AudioSource idleLoop, highLoop;
        AudioSource[] rotary;
        static readonly string[] RotaryClips={"int_idle","int_low_on","int_low_off","int_med_on","int_med_off","int_high_on","int_high_off"};
        static readonly float[] RotaryRpm={1100,2500,4800,7600};
        static readonly float[] RotaryBoundaries={1800,3650,6200};
        const float RotaryCrossfadeWidth=300f;
        // Switch during Play Mode in the Inspector to compare with the original synth.
        public bool useRecordedEngine=true;
        // When disabled, use the earlier two recorded samples for comparison.
        public bool useRotaryEngine=true;
        public void SetDriving(float rpm,float throttle,bool silent)
        {
            // Four-cylinder firing frequency: pitch and load use the authoritative drivetrain.
            targetFrequency=Mathf.Max(PolygonDrivetrain.IdleRpm,rpm)*(2f/60f);
            bool useRotary=useRecordedEngine&&useRotaryEngine&&rotary!=null;
            bool usePrevious=useRecordedEngine&&!useRotary&&idleLoop&&highLoop;
            bool recorded=useRotary||usePrevious;
            float load=Mathf.Clamp01(throttle);
            targetGain=silent||recorded?0:.025f+load*.045f;
            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1800,3800,rpm));
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            float mix=1f-Mathf.Exp(-dt/.10f);
            float idleVolume=silent||!usePrevious?0:(1-blend)*(.14f+load*.025f);
            float highVolume=silent||!usePrevious?0:blend*(.16f+load*.07f);
            float pitchMix=1f-Mathf.Exp(-dt/.10f);
            if(idleLoop&&highLoop)
            {
                idleLoop.volume=Mathf.Lerp(idleLoop.volume,idleVolume,mix);
                highLoop.volume=Mathf.Lerp(highLoop.volume,highVolume,mix);
                idleLoop.pitch=Mathf.Lerp(idleLoop.pitch,Mathf.Clamp(rpm/1100f,1f,1.35f),pitchMix);
                highLoop.pitch=Mathf.Lerp(highLoop.pitch,Mathf.Clamp(.65f+(rpm-1800f)*(.65f/7200f),.65f,1.3f),pitchMix);
            }
            if(rotary==null)return;
            // Only blend near the band boundaries: sustained overlapping recordings can
            // sound like two engines. A short fade still avoids switching clicks.
            for(int band=0;band<RotaryRpm.Length;band++)
            {
                float lower=band==0?1:BandTransition(rpm,RotaryBoundaries[band-1]);
                float upper=band==RotaryRpm.Length-1?1:1-BandTransition(rpm,RotaryBoundaries[band]);
                float weight=lower*upper, pitch=Mathf.Clamp(rpm/RotaryRpm[band],.8f,1.2f);
                int first=band==0?0:band*2-1, count=band==0?1:2;
                for(int variant=0;variant<count;variant++)
                {
                    var layer=rotary[first+variant];
                    float onWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.6f,load));
                    float loadWeight=band==0?1:variant==0?onWeight:1-onWeight;
                    float volume=silent||!useRotary?0:weight*loadWeight*(.072f+load*.018f);
                    layer.volume=Mathf.Lerp(layer.volume,volume,mix);
                    layer.pitch=Mathf.Lerp(layer.pitch,pitch,pitchMix);
                }
            }
        }
        static float BandTransition(float rpm,float boundary)
            => Mathf.SmoothStep(0,1,Mathf.InverseLerp(boundary-RotaryCrossfadeWidth*.5f,boundary+RotaryCrossfadeWidth*.5f,rpm));
        void Awake()
        {
            sampleRate=AudioSettings.outputSampleRate;
            // Per-sample exponential smoothing uses seconds, independent of output rate.
            pitchRise=Coefficient(.08f);pitchFall=Coefficient(.12f);
            gainRise=Coefficient(.07f);gainFall=Coefficient(.10f);
            var source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=.65f;
            carrier=AudioClip.Create("Engine carrier",1024,1,sampleRate,false);source.clip=carrier;source.Play();
            var idle=Resources.Load<AudioClip>("PolygonEngineIdle");
            var high=Resources.Load<AudioClip>("PolygonEngineHigh");
            if(idle&&high){idleLoop=MakeLoop("Recorded engine idle",idle);highLoop=MakeLoop("Recorded engine high RPM",high);}
            var clips=new AudioClip[RotaryClips.Length];
            bool available=true;
            for(int i=0;i<clips.Length;i++){clips[i]=Resources.Load<AudioClip>("Rotary_"+RotaryClips[i]);if(!clips[i])available=false;}
            if(available)
            {
                rotary=new AudioSource[clips.Length];
                for(int i=0;i<clips.Length;i++)rotary[i]=MakeLoop("Rotary "+RotaryClips[i],clips[i]);
            }
        }
        AudioSource MakeLoop(string label,AudioClip clip)
        {
            var child=new GameObject(label);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.clip=clip;source.playOnAwake=false;
            source.loop=true;source.spatialBlend=0;source.volume=0;source.Play();return source;
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
        void OnDestroy()
        {
            if(carrier)Destroy(carrier);
            if(idleLoop)Destroy(idleLoop.gameObject);
            if(highLoop)Destroy(highLoop.gameObject);
            if(rotary!=null)foreach(var layer in rotary)if(layer)Destroy(layer.gameObject);
        }
    }
}
