using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace PolygonRacing
{
    // Three visual-only rivals share the working v12 route and player controls.
    public sealed class PolygonRival : MonoBehaviour
    {
        public bool RaceManaged { get; set; }
        [Header("Live pace diagnostics (runtime only)")]
        [SerializeField] float playerGapMeters;
        [SerializeField] float paceMultiplier=1;
        PolygonRivalPaceSettings paceSettings;
        bool ownsPaceSettings;
        float paceAdjustment;
        public bool Participating { get; private set; }=true;
        public float RaceDistance { get; private set; }
        public void SetParticipating(bool active)
        {
            Participating=active;if(model)model.gameObject.SetActive(active);
            if(!active){ResetSlipstream();SetRaceLock(true);}
        }
        public bool RaceLocked { get; private set; }
        public void SetRaceLock(bool locked){RaceLocked=locked;if(locked){speed=0;ResetPace();SetSlipstreamStrength(0);}}
        public void RestartRace(){ResetRival();paused=false;}
        const int Samples=1024;
        public int GridSlot { get; private set; }
        static readonly List<PolygonRival> traffic=new List<PolygonRival>();
        static readonly string[] names={"blue", "yellow", "green"};
        static readonly Color[] colors={new Color(.035f,.32f,.95f),new Color(1,.72f,.025f),new Color(.08f,.75f,.26f)};
        float NominalSpeed=>player.topSpeed*Mathf.Clamp(paceSettings.topSpeedFactor,.85f,1.15f);
        float Acceleration=>player.acceleration*Mathf.Clamp(paceSettings.accelerationFactor,.6f,1.2f);
        float HomeLane=>GridSlot==1?3.5f:-3.5f;
        float StartDistance=>GridSlot==0?12f:24f;
        PolygonDriver player;
        PolygonRace race;
        Transform model;
        Transform[] wheels,frontPivots;
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly Dictionary<Transform,Transform> copied=new Dictionary<Transform,Transform>();
        readonly Dictionary<Material,Material> recolored=new Dictionary<Material,Material>();
        readonly float[] arc=new float[Samples+1];
        Mesh helmetMesh;
        float distance,speed,spin,lane=-3.5f,laneGoal=-3.5f,totalLength;
        float slipstreamStrength;
        readonly PolygonSlipstream.Charge slipstream=new PolygonSlipstream.Charge();
        bool started,paused,wasReset,wasPause;
        UnityEngine.XR.InputDevice left,right;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            traffic.Clear();
            SceneManager.sceneLoaded-=OnSceneLoaded;
            SceneManager.sceneLoaded+=OnSceneLoaded;
        }
        static void OnSceneLoaded(Scene scene,LoadSceneMode mode)
        {
            var driver=Object.FindFirstObjectByType<PolygonDriver>();
            if(!driver||Object.FindFirstObjectByType<PolygonRival>())return;
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Rival "+(i+2)+" - "+names[i]+" car");
                SceneManager.MoveGameObjectToScene(go,driver.gameObject.scene);
                go.AddComponent<PolygonRival>().GridSlot=i;
            }
        }
        void Start()
        {
            player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player){enabled=false;return;}
            race=Object.FindFirstObjectByType<PolygonRace>();
            paceSettings=Resources.Load<PolygonRivalPaceSettings>("PolygonRivalPace");
            if(!paceSettings){paceSettings=ScriptableObject.CreateInstance<PolygonRivalPaceSettings>();ownsPaceSettings=true;}
            var visual=new GameObject("Rival body - copied visual meshes only");
            model=visual.transform;model.SetParent(transform,false);
            foreach(Transform child in player.transform)CopyBranch(child,model);
            wheels=new Transform[player.wheels.Length];
            for(int i=0;i<wheels.Length;i++)if(player.wheels[i]&&copied.TryGetValue(player.wheels[i],out var w))wheels[i]=w;
            frontPivots=new Transform[player.frontSteering.Length];
            for(int i=0;i<frontPivots.Length;i++)if(player.frontSteering[i]&&copied.TryGetValue(player.frontSteering[i],out var w))frontPivots[i]=w;
            AddDriver();traffic.Add(this);
            var last=PolygonWorld.Point(0);
            for(int i=1;i<=Samples;i++){var p=PolygonWorld.Point(i*Mathf.PI*2/Samples);arc[i]=arc[i-1]+Vector3.Distance(last,p);last=p;}
            totalLength=arc[Samples];ResetRival();
        }
        void CopyBranch(Transform original,Transform parent)
        {
            if(original.name==PolygonCarInstruments.RuntimeRootName||original==player.trackingSpace||original.GetComponent<Camera>()||original.GetComponent<TextMesh>()
                ||original.name=="Dashboard"||original.name=="Countdown and results - cockpit panel")return;
            var node=new GameObject(original.name);var t=node.transform;t.SetParent(parent,false);
            t.localPosition=original.localPosition;t.localRotation=original.localRotation;t.localScale=original.localScale;
            copied[original]=t;
            var filter=original.GetComponent<MeshFilter>();var renderer=original.GetComponent<MeshRenderer>();
            if(filter&&renderer)
            {
                node.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var r=node.AddComponent<MeshRenderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var mat=materials[i];if(!mat||!mat.name.StartsWith("PR_Vermilion"))continue;
                    if(!recolored.TryGetValue(mat,out var blue)){blue=new Material(mat){name="Rival "+names[GridSlot],color=colors[GridSlot]};recolored[mat]=blue;ownedMaterials.Add(blue);}
                    materials[i]=blue;
                }
                r.sharedMaterials=materials;r.enabled=renderer.enabled;
            }
            foreach(Transform child in original)CopyBranch(child,t);
        }
        void AddDriver()
        {
            var shader=PolygonLook.LitShader();
            var suit=new Material(shader){name="Rival driver suit",color=new Color(.06f,.09f,.15f)};
            var helmet=new Material(shader){name="Rival gold helmet",color=new Color(1,.74f,.08f)};
            ownedMaterials.Add(suit);ownedMaterials.Add(helmet);
            // PolygonWorld.Box removes colliders immediately and is editor-oriented; construct these runtime visuals directly.
            var torso=GameObject.CreatePrimitive(PrimitiveType.Cube);torso.name="Rival seated driver";torso.transform.SetParent(model,false);
            torso.transform.localPosition=new Vector3(0,.66f,-.67f);torso.transform.localScale=new Vector3(.43f,.43f,.27f);
            var collider=torso.GetComponent<Collider>();collider.enabled=false;Destroy(collider);torso.GetComponent<Renderer>().sharedMaterial=suit;
            var v=new[]{new Vector3(0,.24f,0),new Vector3(.18f,0,0),new Vector3(0,0,.2f),new Vector3(-.18f,0,0),new Vector3(0,0,-.2f),new Vector3(0,-.2f,0)};
            var points=new List<Vector3>();var indices=new List<int>();
            int[] faces={0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
            for(int i=0;i<faces.Length;i++){points.Add(v[faces[i]]);indices.Add(i);}
            helmetMesh=new Mesh{name="Rival faceted helmet"};helmetMesh.SetVertices(points);helmetMesh.SetTriangles(indices,0);helmetMesh.RecalculateNormals();helmetMesh.RecalculateBounds();
            var head=new GameObject("Gold polygon helmet",typeof(MeshFilter),typeof(MeshRenderer));head.transform.SetParent(model,false);head.transform.localPosition=new Vector3(0,1.02f,-.66f);
            head.GetComponent<MeshFilter>().sharedMesh=helmetMesh;head.GetComponent<Renderer>().sharedMaterial=helmet;
        }
        static bool Button(UnityEngine.XR.InputDevice d,InputFeatureUsage<bool> key)=>d.TryGetFeatureValue(key,out var v)&&v;
        void Update()
        {
            if(!player||!model)return;
            if(!left.isValid)left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if(!right.isValid)right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var k=Keyboard.current;
            bool reset=Button(right,CommonUsages.primaryButton)||(k!=null&&k.rKey.isPressed);
            bool pause=Button(left,CommonUsages.primaryButton)||(k!=null&&k.pKey.isPressed);
            if(reset&&!wasReset)ResetRival();if(pause&&!wasPause)paused=!paused;
            wasReset=reset;wasPause=pause;
            if(paused||RaceLocked||!Participating||player.IsPaused)
            {
                SetSlipstreamStrength(0);
                return;
            }
            if(!started&&(RaceManaged||player.SpeedKmh>3))started=true;
            if(!started)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);
            UpdatePace(dt);
            float draft=0;
            if(RaceManaged&&race&&race.SlipstreamAllowed)
            {
                draft=MeasureDraft(player.transform.position,player.transform.forward,race.PlayerRaceDistance,draft);
                foreach(var other in traffic)
                {
                    if(!other||other==this||!other.Participating||other.RaceLocked)continue;
                    draft=MeasureDraft(other.transform.position,other.transform.forward,other.RaceDistance,draft);
                }
            }
            SetSlipstreamStrength(slipstream.Advance(draft,dt));
            // Current arcade handling has no corner traction limit for either racer.
            // Keep traffic spacing below, but do not brake just because the route bends.
            float boostedNominalSpeed=NominalSpeed*paceMultiplier*(1+PolygonSlipstream.SpeedBonus*slipstreamStrength);
            float targetSpeed=boostedNominalSpeed;
            float angle=AngleAt(distance);
            var center=PolygonWorld.Point(angle);var side=PolygonWorld.Side(angle);
            var tangent=PolygonWorld.Tangent(angle);
            float preferred=laneGoal;
            float gap=ForwardGap(center,tangent,side,lane);
            if(gap<18&&Mathf.Abs(lane-laneGoal)<.2f)
            {
                float alternate=lane>=0?-3.5f:3.5f;
                preferred=LaneClear(center,tangent,side,alternate)?alternate:lane;
            }
            else if(gap>=18&&LaneClear(center,tangent,side,HomeLane))preferred=HomeLane;
            laneGoal=preferred;
            // Hold back while a lane is occupied, including during a lane change.
            if(gap<11)targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(0,(gap-4)*2.5f));
            float boostedAcceleration=Acceleration*(1+paceAdjustment*.5f)*(1+PolygonSlipstream.AccelerationBonus*slipstreamStrength);
            speed=Mathf.MoveTowards(speed,targetSpeed,(speed>targetSpeed?18f:boostedAcceleration)*dt);
            RaceDistance+=speed*dt;
            distance=Mathf.Repeat(RaceDistance,totalLength);
            angle=AngleAt(distance);center=PolygonWorld.Point(angle);side=PolygonWorld.Side(angle);
            lane=Mathf.MoveTowards(lane,preferred,dt*1.3f);
            var position=center+side*lane;position.y=PolygonBanking.Height(distance,lane);
            var forward=PolygonWorld.Point(angle+.008f)+PolygonWorld.Side(angle+.008f)*lane-position;forward.y=0;
            var normal=PolygonBanking.Normal(distance,lane);forward=PolygonBanking.ForwardOnSurface(forward,normal);
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(forward,normal));
            spin=(spin+speed/.39f*Mathf.Rad2Deg*dt)%360;
            foreach(var w in wheels)if(w)w.localRotation=Quaternion.Euler(spin,0,0);
            // Shared circuit curvature animates the front wheels through both left and right turns.
            float curvature=PolygonCircuit.Curvature(distance);
            foreach(var w in frontPivots)if(w)w.localRotation=Quaternion.Euler(0,Mathf.Atan(2.85f*curvature)*Mathf.Rad2Deg,0);
        }
        void ResetPace(){paceAdjustment=playerGapMeters=0;paceMultiplier=1;}
        void UpdatePace(float dt)
        {
            if(!RaceManaged||!race||!race.SlipstreamAllowed){ResetPace();return;}
            // Deliberately unwrapped: start-line seams, lapped cars and nearby
            // parallel sections cannot reverse which racer is actually ahead.
            playerGapMeters=race.PlayerRaceDistance-RaceDistance;
            float target=paceSettings.TargetAdjustment(playerGapMeters);
            float blend=1-Mathf.Exp(-dt/Mathf.Max(1,paceSettings.responseSeconds));
            paceAdjustment=Mathf.Lerp(paceAdjustment,target,blend);
            paceMultiplier=1+paceAdjustment;
        }
        float MeasureDraft(Vector3 targetPosition,Vector3 targetForward,float targetDistance,float current)
        {
            return Mathf.Max(current,PolygonSlipstream.Measure(transform.position,transform.forward,RaceDistance,
                targetPosition,targetForward,targetDistance));
        }
        public void SetSlipstreamStrength(float strength){slipstreamStrength=Mathf.Clamp01(strength);}
        public void ResetSlipstream(){slipstream.Reset();SetSlipstreamStrength(0);}
        float ForwardGap(Vector3 center,Vector3 forward,Vector3 side,float targetLane)
        {
            float gap=1000;
            float y=transform.position.y;
            CheckGap(player.transform.position,center,forward,side,targetLane,y,ref gap);
            foreach(var other in traffic)if(other&&other!=this&&other.Participating)
                CheckGap(other.transform.position,center,forward,side,targetLane,y,ref gap);
            return gap;
        }
        // Cars more than 4 m above or below (the other road at a crossover bridge) are ignored.
        static void CheckGap(Vector3 p,Vector3 center,Vector3 forward,Vector3 side,float targetLane,float y,ref float gap)
        {
            if(Mathf.Abs(p.y-y)>4)return;
            var delta=p-center;float ahead=Vector3.Dot(delta,forward);
            if(ahead>0&&Mathf.Abs(Vector3.Dot(delta,side)-targetLane)<3.2f)gap=Mathf.Min(gap,ahead);
        }
        bool LaneClear(Vector3 center,Vector3 forward,Vector3 side,float candidate)
        {
            float y=transform.position.y;
            if(BlocksLane(player.transform.position,center,forward,side,candidate,y))return false;
            foreach(var other in traffic)if(other&&other!=this&&other.Participating&&BlocksLane(other.transform.position,center,forward,side,candidate,y))return false;
            return true;
        }
        static bool BlocksLane(Vector3 p,Vector3 center,Vector3 forward,Vector3 side,float candidate,float y)
        {
            if(Mathf.Abs(p.y-y)>4)return false;
            var delta=p-center;
            return Mathf.Abs(Vector3.Dot(delta,forward))<16&&Mathf.Abs(Vector3.Dot(delta,side)-candidate)<3.2f;
        }
        float AngleAt(float meters)
        {
            int lo=0,hi=Samples;
            while(hi-lo>1){int mid=(lo+hi)/2;if(arc[mid]<=meters)lo=mid;else hi=mid;}
            return (lo+Mathf.InverseLerp(arc[lo],arc[hi],meters))*Mathf.PI*2/Samples;
        }
        void ResetRival()
        {
            speed=spin=slipstreamStrength=0;ResetPace();slipstream.Reset();started=false;lane=laneGoal=HomeLane;distance=RaceDistance=StartDistance;
            float a=AngleAt(distance);transform.SetPositionAndRotation(PolygonWorld.Point(a)+PolygonWorld.Side(a)*lane,Quaternion.LookRotation(PolygonWorld.Tangent(a)));
        }
        void OnApplicationFocus(bool focus){if(!focus)paused=true;}
        void OnDestroy(){if(ownsPaceSettings&&paceSettings)Destroy(paceSettings);traffic.Remove(this);foreach(var m in ownedMaterials)if(m)Destroy(m);if(helmetMesh)Destroy(helmetMesh);}
    }
}
