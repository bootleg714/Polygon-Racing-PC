using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace PolygonRacing
{
    // Additive audio and gentle controller feedback. Never moves the car or headset.
    [DefaultExecutionOrder(200)]
    public sealed class PolygonRacingFeedback : MonoBehaviour
    {
        sealed class RivalVoice
        {
            public PolygonRival car;
            public AudioSource source;
            public Vector3 previous;
        }
        PolygonDriver player;
        AudioSource tires,gravel;
        bool recordedSkid,skidActive;
        float skidOnsetTime;
        const float SkidStart=.50f,SkidRelease=.30f,SkidOnsetDelay=.08f;
        readonly List<RivalVoice> voices=new List<RivalVoice>();
        readonly List<GameObject> emitters=new List<GameObject>();
        readonly List<AudioClip> clips=new List<AudioClip>();
        Vector3 previousForward;
        int resetSerial;
        float nextPulse;
        bool focused=true;
        UnityEngine.XR.InputDevice left,right;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;
        }
        // Fast Enter Play Mode may reuse the open scene without a sceneLoaded event.
        // This idempotent check also leaves normal scene switching on the existing callback.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInitialScene(){SceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
        static void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            var driver=UnityEngine.Object.FindFirstObjectByType<PolygonDriver>();
            if(!driver||UnityEngine.Object.FindFirstObjectByType<PolygonRacingFeedback>())return;
            var go=new GameObject("Racing audio and road feedback");
            SceneManager.MoveGameObjectToScene(go,driver.gameObject.scene);
            go.AddComponent<PolygonRacingFeedback>();
        }
        void Start()
        {
            player=UnityEngine.Object.FindFirstObjectByType<PolygonDriver>();
            if(!player){enabled=false;return;}
            var tireClip=Resources.Load<AudioClip>("PolygonSkid");
            recordedSkid=tireClip;
            if(!tireClip)tireClip=MakeClip("Procedural tire scrub",0);
            var gravelClip=MakeClip("Procedural gravel rumble",1);
            var engineClip=MakeClip("Procedural rival engine",2);
            tires=MakeSource("Tire scrub",player.transform,tireClip,false,!recordedSkid);
            if(recordedSkid)tires.loop=true;
            gravel=MakeSource("Off-road rumble",player.transform,gravelClip,false);
            var rivals=UnityEngine.Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None);
            foreach(var car in rivals)
            {
                var source=MakeSource("Passing engine",car.transform,engineClip,true);
                voices.Add(new RivalVoice{car=car,source=source,previous=car.transform.position});
            }
            previousForward=player.transform.forward;resetSerial=player.ResetSerial;
        }
        AudioSource MakeSource(string name,Transform parent,AudioClip clip,bool spatial,bool loop=true)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);emitters.Add(go);
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
            source.clip=clip;source.volume=0;source.spatialBlend=spatial?1:0;
            source.dopplerLevel=spatial ? .65f : 0;source.minDistance=4;source.maxDistance=65;
            source.rolloffMode=AudioRolloffMode.Linear;source.priority=spatial?160:100;
            if(loop)source.Play();return source;
        }
        void LateUpdate()
        {
            if(!player||!tires||!gravel)return;
            float dt=Time.deltaTime;
            bool reset=resetSerial!=player.ResetSerial;
            bool silent=!focused||player.IsPaused||player.RaceLocked||reset||dt<=0||dt>.15f;
            resetSerial=player.ResetSerial;
            float speed=player.SpeedKmh/3.6f;
            float yaw=dt>0?Mathf.Abs(Vector3.SignedAngle(previousForward,player.transform.forward,Vector3.up))*Mathf.Deg2Rad/dt:0;
            previousForward=player.transform.forward;
            PolygonCircuit.ProjectLap(player.transform.position,out float offset);
            float surfaceWidth=PolygonWorld.RoadHalfWidth;
            if(PolygonMarinaPit.TrySample(player.transform.position,offset,out _,out float pitOffset,out _)){offset=pitOffset;surfaceWidth=PolygonMarinaPit.HalfWidth;}
            float roughness=Mathf.SmoothStep(0,1,Mathf.InverseLerp(surfaceWidth-.1f,surfaceWidth+1,offset));
            float scrub=Mathf.InverseLerp(3.5f,10,speed*yaw)*Mathf.InverseLerp(7,16,speed)*(1-roughness);
            if(recordedSkid)UpdateRecordedSkid(scrub,dt,silent);
            float tireTarget=silent||(recordedSkid&&!skidActive)?0:
                recordedSkid?RecordedSkidVolume(speed,yaw,roughness,player.steeringAngle):scrub*.16f;
            float gravelTarget=silent?0:roughness*Mathf.InverseLerp(1,14,speed)*.2f;
            tires.volume=silent?0:Mathf.MoveTowards(tires.volume,tireTarget,dt*.45f);
            if(recordedSkid&&!skidActive&&tires.volume<=.0001f)tires.Stop();
            gravel.volume=silent?0:Mathf.MoveTowards(gravel.volume,gravelTarget,dt*.65f);
            tires.pitch=Mathf.Lerp(.85f,1.12f,Mathf.Clamp01(speed/42));
            gravel.pitch=Mathf.Lerp(.65f,1.1f,Mathf.Clamp01(speed/14));
            foreach(var voice in voices)
            {
                if(!voice.car||!voice.source)continue;
                float meters=Vector3.Distance(voice.previous,voice.car.transform.position);
                voice.previous=voice.car.transform.position;
                float rivalSpeed=dt>0?meters/dt:0;
                bool quiet=silent||voice.car.RaceLocked||meters>8;
                float drive=Mathf.Clamp01(rivalSpeed/37);
                voice.source.pitch=Mathf.Lerp(.8f,2.4f,drive)*(1+voice.car.GridSlot*.035f);
                voice.source.volume=quiet?0:Mathf.MoveTowards(voice.source.volume,.12f+drive*.1f,dt*.5f);
            }
            if(silent){StopHaptics();return;}
            // Short, modest pulses only while moving off-road. Unsupported devices are skipped.
            if(roughness>.25f&&speed>2&&Time.unscaledTime>=nextPulse)
            {
                nextPulse=Time.unscaledTime+.12f;
                if(!left.isValid)left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                if(!right.isValid)right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                float strength=.12f*roughness*Mathf.InverseLerp(2,14,speed);
                Pulse(left,strength);Pulse(right,strength*.85f);
            }
        }
        static float RecordedSkidVolume(float speed,float yaw,float roughness,float steeringAngle)
        {
            // Read the driver's existing steering envelope without changing it. A fixed
            // demand cap saturates early at racing speed, even with modest steering.
            float lockRadians=steeringAngle/(1+speed*.065f)*Mathf.Deg2Rad;
            float fullLockDemand=speed*speed/2.85f*Mathf.Tan(lockRadians);
            float intensity=Mathf.InverseLerp(3.5f,Mathf.Max(16.5f,fullLockDemand),speed*yaw);
            float response=intensity*(.20f+.80f*intensity);
            return .08f*response*Mathf.InverseLerp(7,16,speed)*(1-roughness);
        }
        void UpdateRecordedSkid(float scrub,float dt,bool silent)
        {
            if(silent){tires.Stop();skidActive=false;skidOnsetTime=0;return;}
            if(skidActive)
            {
                if(scrub<SkidRelease)skidActive=false;
                return;
            }
            // Reject small steering corrections and brief cornering spikes. Release
            // below a separate threshold so a sustained corner does not chatter.
            skidOnsetTime=scrub>=SkidStart?skidOnsetTime+dt:0;
            if(skidOnsetTime>=SkidOnsetDelay)
            {
                skidActive=true;skidOnsetTime=0;
                if(!tires.isPlaying)tires.Play();
            }
        }
        static void Pulse(UnityEngine.XR.InputDevice device,float amplitude)
        {
            if(device.isValid&&device.TryGetHapticCapabilities(out var caps)&&caps.supportsImpulse&&caps.numChannels>0)
                device.SendHapticImpulse(0,amplitude,.035f);
        }
        void StopHaptics(){if(left.isValid)left.StopHaptics();if(right.isValid)right.StopHaptics();}
        void OnApplicationFocus(bool focus){focused=focus;if(!focus)Mute();}
        void OnDisable(){Mute();}
        void Mute()
        {
            if(tires)tires.volume=0;if(gravel)gravel.volume=0;
            if(recordedSkid&&tires){tires.Stop();skidActive=false;skidOnsetTime=0;}
            foreach(var voice in voices)if(voice.source)voice.source.volume=0;
            StopHaptics();
        }
        AudioClip MakeClip(string name,int kind)
        {
            const int rate=22050,count=rate*2;
            var samples=new float[count];uint random=714;float low=0;
            for(int i=0;i<count;i++)
            {
                double t=(double)i/rate;
                random=unchecked(random*1664525u+1013904223u);
                float noise=(random>>8)/8388607.5f-1;
                low+=(noise-low)*(kind==1 ? .045f : .16f);
                float v;
                if(kind==0)
                    v=(float)(Math.Sin(t*Math.PI*2*780)*.12+Math.Sin(t*Math.PI*2*1170)*.06)+(noise-low)*.22f;
                else if(kind==1)
                    v=low*.9f+(float)(Math.Sin(t*Math.PI*2*43)*.09+Math.Sin(t*Math.PI*2*81)*.045);
                else
                    v=(float)(Math.Sin(t*Math.PI*2*55)*.29+Math.Sin(t*Math.PI*2*110)*.13+Math.Sin(t*Math.PI*2*220)*.065)+low*.045f;
                samples[i]=v;
            }
            // Ease the last 10 ms into the first sample so the loop has no amplitude step.
            const int fade=220;
            for(int i=0;i<fade;i++)
            {
                float blend=Mathf.SmoothStep(0,1,(i+1f)/fade);
                samples[count-fade+i]=Mathf.Lerp(samples[count-fade+i],samples[0],blend);
            }
            var clip=AudioClip.Create(name,count,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
        void OnDestroy()
        {
            Mute();foreach(var emitter in emitters)if(emitter)Destroy(emitter);
            foreach(var clip in clips)if(clip)Destroy(clip);
        }
    }
}
