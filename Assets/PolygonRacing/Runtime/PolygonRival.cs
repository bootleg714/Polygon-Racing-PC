using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace PolygonRacing
{
    // Three visual-only rivals share the working v12 route and player controls.
    // Racecraft v01: each rival follows a shared racing line with a corner-speed plan based on the
    // player's own steering limit, has a small personality, passes and defends instead of queueing,
    // and gets catch-up / leader easing from the time gap to the player.
    public sealed class PolygonRival : MonoBehaviour
    {
        public bool RaceManaged { get; set; }
        [Header("Live pace diagnostics (runtime only)")]
        [SerializeField] float playerGapMeters;
        [SerializeField] float playerGapSeconds;
        [SerializeField] float paceMultiplier=1;
        [SerializeField] string behaviour="line";
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
        float distance,speed,spin,lane=-3.5f,totalLength;
        float slipstreamStrength;
        readonly PolygonSlipstream.Charge slipstream=new PolygonSlipstream.Charge();
        bool started,paused,wasReset,wasPause;
        UnityEngine.XR.InputDevice left,right;

        // Small differences so the pack stays close but each car races differently.
        // lockUse: share of the player's steering limit used in tight corners (.85 cautious .. 1 full).
        struct Personality{public float top,lockUse,aggression,defend,consistency,mistake;}
        static readonly Personality[] personalities={
            new Personality{top=.995f,lockUse=.93f,aggression=.45f,defend=.35f,consistency=.90f,mistake=.05f},  // blue: smooth, tidy
            new Personality{top=1.003f,lockUse=.97f,aggression=.90f,defend=.70f,consistency=.75f,mistake=.09f}, // yellow: late, eager, scrappy
            new Personality{top=1.01f,lockUse=.95f,aggression=.65f,defend=.60f,consistency=.95f,mistake=.03f}}; // green: fastest, consistent
        Personality Me=>personalities[Mathf.Clamp(GridSlot,0,personalities.Length-1)];
        const float SameLane=2.4f,PassOffset=3.1f,SideBySide=3f;
        float BrakeDeceleration=>player.braking*.8f+1.3f;
        struct Other{public Component car;public float rel,lat,speed;}
        readonly List<Other> others=new List<Other>();
        PolygonRivalPlan plan;
        System.Random rng=new System.Random();
        Component passCar;
        float laneVelocity,passSide,raceTime,wanderPhase,nextPassCheck,nextBendCheck;
        float defendOffset,defendUntil=-1,mistakeUntil=-1,lastHeading,steerCurvature;
        int mistakeKind;
        bool headingValid;
        static int playerSampleFrame=-1;
        static float playerRouteDistance,playerLateral;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            traffic.Clear();playerSampleFrame=-1;
            SceneManager.sceneLoaded-=OnSceneLoaded;
            SceneManager.sceneLoaded+=OnSceneLoaded;
        }
        // Fast Enter Play Mode may reuse the open scene without a sceneLoaded event.
        // This idempotent check also leaves normal scene switching on the existing callback.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInitialScene(){OnSceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
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
            totalLength=arc[Samples];plan=PolygonRivalPlan.For(player.steeringAngle,BrakeDeceleration);ResetRival();
        }
        void CopyBranch(Transform original,Transform parent)
        {
            if(original.name==PolygonPCMirrors.RuntimeRootName||original.name==PolygonCarInstruments.RuntimeRootName||original==player.trackingSpace||original.GetComponent<Camera>()||original.GetComponent<TextMesh>()
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
            if(plan==null||!Mathf.Approximately(plan.Length,PolygonCircuit.Length))plan=PolygonRivalPlan.For(player.steeringAngle,BrakeDeceleration);
            raceTime+=dt;
            var me=Me;
            float lim=plan.LaneLimit;
            float aggression=Mathf.Clamp01(me.aggression*paceSettings.aggression);
            // Slow pace wander: consistent drivers wander less. Gives natural gap changes and passes.
            float wander=(1-me.consistency)*.05f*(Mathf.Sin(raceTime*.21f+wanderPhase)*.6f+Mathf.Sin(raceTime*.057f+wanderPhase*1.7f)*.4f);
            // Catch-up also raises cornering skill (closer to the steering limit); leader easing lowers it.
            float skill=Mathf.Clamp01((me.lockUse-PolygonRivalPlan.LowLockUse)/(PolygonRivalPlan.HighLockUse-PolygonRivalPlan.LowLockUse)+paceAdjustment*3);
            float boostedNominalSpeed=NominalSpeed*me.top*(1+paceAdjustment+wander)*(1+PolygonSlipstream.SpeedBonus*slipstreamStrength);
            float targetSpeed=Mathf.Min(boostedNominalSpeed,plan.CornerLimit(distance+speed*.25f,skill));

            // Mistakes: rolled once per noticeable bend; more likely when comfortably ahead of the player.
            float bend=Mathf.Max(Mathf.Abs(plan.LineCurvature(distance+20)),Mathf.Max(Mathf.Abs(plan.LineCurvature(distance+40)),Mathf.Abs(plan.LineCurvature(distance+60))));
            if(bend>1/160f&&raceTime>=nextBendCheck)
            {
                nextBendCheck=raceTime+6;
                float chance=me.mistake*paceSettings.mistakeFrequency*(1+4*Mathf.Max(0,-paceAdjustment)/Mathf.Max(.0001f,paceSettings.leaderEase));
                if(Random01()<chance){mistakeKind=Random01()<.5f?1:2;mistakeUntil=raceTime+Mathf.Lerp(1.2f,2.2f,Random01());}
            }
            bool mistake=raceTime<mistakeUntil;
            if(mistake&&mistakeKind==1)targetSpeed*=.9f;                 // lifted / braked too early
            float lineLane=plan.Line(distance),desired=lineLane;
            if(mistake&&mistakeKind==2)desired=lineLane-Mathf.Sign(plan.LineCurvature(distance+30))*2f; // ran wide
            behaviour=mistake?"mistake":"line";

            GatherTraffic();
            // Car ahead in our lane, within a speed-dependent look-ahead.
            float look=10+speed*1.1f,aheadRel=float.MaxValue;int ahead=-1;
            for(int i=0;i<others.Count;i++)
            {
                var o=others[i];
                if(o.rel>0&&o.rel<look&&Mathf.Abs(o.lat-lane)<SameLane&&o.rel<aheadRel){ahead=i;aheadRel=o.rel;}
            }
            int passing=-1;
            if(passCar)
            {
                for(int i=0;i<others.Count;i++)if(others[i].car==passCar)passing=i;
                if(passing<0||others[passing].rel<-6||others[passing].rel>look+15){passCar=null;passing=-1;nextPassCheck=raceTime+2.5f;}
            }
            if(ahead>=0&&passing<0&&raceTime>=nextPassCheck)
            {
                // Decide twice a second; commit only with a real speed advantage (draft or pace).
                nextPassCheck=raceTime+.5f;
                var o=others[ahead];
                bool closing=speed>o.speed+1-.8f*aggression&&aheadRel<12+10*aggression;
                if(closing&&Random01()<.3f+.7f*aggression&&ChoosePassSide(o,aheadRel,lim,out float side))
                {passCar=o.car;passSide=side;passing=ahead;}
            }
            if(passing>=0){desired=Mathf.Clamp(others[passing].lat+passSide,-lim,lim);behaviour="passing";}
            else if(ahead>=0)
            {
                // Follow at a sensible distance (slipstream will offer the next chance to pass).
                float follow=7+.15f*speed;
                targetSpeed=Mathf.Min(targetSpeed,others[ahead].speed+Mathf.Clamp((aheadRel-follow)*.6f,-6,4));
                behaviour="following";
            }

            // Defend: a car closing from behind before a bend makes us take the inside once.
            // Never start the move while anyone is alongside.
            if(passing<0&&raceTime>defendUntil)
            {
                bool alongside=false;int chaser=-1;
                for(int i=0;i<others.Count;i++)
                {
                    var o=others[i];
                    if(Mathf.Abs(o.rel)<8)alongside=true;
                    else if(o.rel>-22&&o.rel<-6&&o.speed>speed-1&&chaser<0)chaser=i;
                }
                float bendAhead=plan.LineCurvature(distance+90);
                if(!alongside&&chaser>=0&&Mathf.Abs(bendAhead)>1/250f&&Random01()<me.defend*Mathf.Clamp01(paceSettings.aggression)*dt*2)
                {
                    float inside=Mathf.Sign(bendAhead)*Mathf.Min(lim,Mathf.Abs(lineLane)+2.5f);
                    defendOffset=inside-lineLane;defendUntil=raceTime+3.5f;
                }
            }
            if(raceTime<defendUntil&&passing<0){desired=Mathf.Clamp(lineLane+defendOffset,-lim,lim);behaviour="defending";}

            // Side-by-side spacing: never squeeze into a car that overlaps us, never rear-end one.
            for(int i=0;i<others.Count;i++)
            {
                var o=others[i];
                if(Mathf.Abs(o.rel)<7.5f&&Mathf.Abs(desired-o.lat)<SideBySide)
                {
                    float away=lane-o.lat;if(away==0)away=GridSlot%2==1?1:-1;
                    float candidate=o.lat+Mathf.Sign(away)*SideBySide;
                    if(Mathf.Abs(candidate)>lim)candidate=o.lat-Mathf.Sign(away)*SideBySide;
                    desired=candidate;
                }
                if(o.rel>0&&o.rel<9&&Mathf.Abs(lane-o.lat)<2.2f)targetSpeed=Mathf.Min(targetSpeed,o.speed-(9-o.rel)*.8f);
            }
            desired=Mathf.Clamp(desired,-lim,lim);

            // Lateral: smooth spring with limited sideways speed; stop sliding toward a car beside us.
            const float omega=1.4f;
            float lateralAcceleration=Mathf.Clamp(omega*omega*(desired-lane)-2*omega*laneVelocity,-6,6);
            laneVelocity=Mathf.Clamp(laneVelocity+lateralAcceleration*dt,-4,4);
            for(int i=0;i<others.Count;i++)
            {
                var o=others[i];
                if(Mathf.Abs(o.rel)<6&&Mathf.Abs(o.lat-lane)<SideBySide&&(o.lat-lane)*laneVelocity>0)laneVelocity=0;
            }
            lane+=laneVelocity*dt;
            if(Mathf.Abs(lane)>lim+.5f){lane=Mathf.Sign(lane)*(lim+.5f);laneVelocity=0;}

            // Speed: same drag terms as the player's car; brake firmly only when well above target.
            targetSpeed=Mathf.Max(0,targetSpeed);
            float boostedAcceleration=Acceleration*(1+Mathf.Max(0,paceAdjustment)*.5f)*(1+PolygonSlipstream.AccelerationBonus*slipstreamStrength);
            if(speed<targetSpeed)speed=Mathf.Min(targetSpeed,speed+(boostedAcceleration-1.3f-speed*.025f)*dt);
            else speed=Mathf.Max(targetSpeed,speed-(speed-targetSpeed<1.5f?1.3f+speed*.025f:BrakeDeceleration)*dt);

            // Advance along the centre line: inside of a bend covers more route per metre driven,
            // and sideways motion costs forward progress.
            float forwardSpeed=Mathf.Sqrt(Mathf.Max(0,speed*speed-laneVelocity*laneVelocity));
            RaceDistance+=forwardSpeed*dt/Mathf.Max(.3f,1-plan.CentreCurvature(distance)*lane);
            distance=Mathf.Repeat(RaceDistance,totalLength);
            float angle=AngleAt(distance);var center=PolygonWorld.Point(angle);var sideVector=PolygonWorld.Side(angle);
            var position=center+sideVector*lane;position.y=PolygonBanking.Height(distance,lane);
            float lookMeters=Mathf.Max(1.5f,speed*.06f);
            float aheadAngle=AngleAt(Mathf.Repeat(distance+lookMeters,totalLength));
            var forward=PolygonWorld.Point(aheadAngle)+PolygonWorld.Side(aheadAngle)*(lane+laneVelocity*lookMeters/Mathf.Max(speed,1))-position;forward.y=0;
            if(forward.sqrMagnitude<.0001f)forward=PolygonWorld.Tangent(angle);
            var normal=PolygonBanking.Normal(distance,lane);var surfaceForward=PolygonBanking.ForwardOnSurface(forward,normal);
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(surfaceForward,normal));
            spin=(spin+speed/.39f*Mathf.Rad2Deg*dt)%360;
            foreach(var w in wheels)if(w)w.localRotation=Quaternion.Euler(spin,0,0);
            // Front wheels follow the car's actual path curvature (racing line, passes and corners).
            float heading=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
            float travelled=speed*dt;
            float pathCurvature=headingValid&&travelled>.01f?Mathf.DeltaAngle(lastHeading,heading)*Mathf.Deg2Rad/travelled:PolygonCircuit.Curvature(distance);
            lastHeading=heading;headingValid=true;
            steerCurvature=Mathf.Lerp(steerCurvature,pathCurvature,1-Mathf.Exp(-dt*8));
            foreach(var w in frontPivots)if(w)w.localRotation=Quaternion.Euler(0,Mathf.Atan(2.85f*steerCurvature)*Mathf.Rad2Deg,0);
        }
        float Random01()=>(float)rng.NextDouble();
        float Wrap(float meters)=>Mathf.Repeat(meters-RaceDistance+totalLength*.5f,totalLength)-totalLength*.5f;
        // Player route distance and lateral offset, sampled once per frame for all rivals.
        void SamplePlayer()
        {
            if(playerSampleFrame==Time.frameCount)return;
            playerSampleFrame=Time.frameCount;
            var p=player.transform.position;
            playerRouteDistance=RaceManaged&&race?race.PlayerRaceDistance:PolygonCircuit.ProjectLap(p,out _)*PolygonCircuit.Length;
            float wrapped=Mathf.Repeat(playerRouteDistance,PolygonCircuit.Length);
            var c=PolygonCircuit.AtDistance(wrapped);var t=PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(wrapped));
            var d=p-c;d.y=0;playerLateral=Vector3.Dot(d,Vector3.Cross(Vector3.up,t));
        }
        // Cars more than 4 m above or below (the other road at a crossover bridge) are ignored.
        void GatherTraffic()
        {
            others.Clear();
            float y=transform.position.y;
            if(!player.IsOnPitLane&&Mathf.Abs(player.transform.position.y-y)<=4)
            {
                SamplePlayer();
                if(Mathf.Abs(playerLateral)<=PolygonWorld.RoadHalfWidth+1)
                    others.Add(new Other{car=player,rel=Wrap(playerRouteDistance),lat=playerLateral,speed=player.SpeedKmh/3.6f});
            }
            foreach(var other in traffic)
            {
                if(!other||other==this||!other.Participating||Mathf.Abs(other.transform.position.y-y)>4)continue;
                others.Add(new Other{car=other,rel=Wrap(other.RaceDistance),lat=other.lane,speed=other.speed});
            }
        }
        // Pull alongside on the free side; prefer the inside of the coming bend and a short move.
        bool ChoosePassSide(Other target,float rel,float lim,out float offset)
        {
            float bendSign=0;for(int x=40;x<=160;x+=40)bendSign+=plan.LineCurvature(distance+x);
            float best=float.NegativeInfinity;offset=0;bool found=false;
            for(int s=-1;s<=1;s+=2)
            {
                float candidate=target.lat+s*PassOffset;
                if(Mathf.Abs(candidate)>lim)continue;
                bool free=true;
                foreach(var o in others)
                    if(o.car!=target.car&&o.rel>-8&&o.rel<rel+14&&Mathf.Abs(o.lat-candidate)<SameLane){free=false;break;}
                if(!free)continue;
                float score=-Mathf.Abs(candidate-lane)*.3f+(s*bendSign>0?2:0);
                if(score>best){best=score;offset=s*PassOffset;found=true;}
            }
            return found;
        }
        void ResetPace(){paceAdjustment=playerGapMeters=playerGapSeconds=0;paceMultiplier=1;}
        void UpdatePace(float dt)
        {
            if(!RaceManaged||!race||!race.SlipstreamAllowed){ResetPace();return;}
            // Deliberately unwrapped: start-line seams, lapped cars and nearby
            // parallel sections cannot reverse which racer is actually ahead.
            playerGapMeters=race.PlayerRaceDistance-RaceDistance;
            // Seconds of gap at this car's speed (floored so a slow launch does not exaggerate it).
            playerGapSeconds=playerGapMeters/Mathf.Max(20,speed);
            float target=paceSettings.TargetAdjustment(playerGapSeconds);
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
        float AngleAt(float meters)
        {
            int lo=0,hi=Samples;
            while(hi-lo>1){int mid=(lo+hi)/2;if(arc[mid]<=meters)lo=mid;else hi=mid;}
            return (lo+Mathf.InverseLerp(arc[lo],arc[hi],meters))*Mathf.PI*2/Samples;
        }
        void ResetRival()
        {
            speed=spin=slipstreamStrength=laneVelocity=steerCurvature=0;ResetPace();slipstream.Reset();started=false;lane=HomeLane;distance=RaceDistance=StartDistance;
            rng=new System.Random(GridSlot*7919+System.Environment.TickCount);
            wanderPhase=(float)rng.NextDouble()*100;raceTime=nextPassCheck=nextBendCheck=0;
            passCar=null;defendUntil=mistakeUntil=-1;headingValid=false;behaviour="line";
            float a=AngleAt(distance);transform.SetPositionAndRotation(PolygonWorld.Point(a)+PolygonWorld.Side(a)*lane,Quaternion.LookRotation(PolygonWorld.Tangent(a)));
        }
        void OnApplicationFocus(bool focus){if(!focus)paused=true;}
        void OnDestroy(){if(ownsPaceSettings&&paceSettings)Destroy(paceSettings);traffic.Remove(this);foreach(var m in ownedMaterials)if(m)Destroy(m);if(helmetMesh)Destroy(helmetMesh);}
    }
}
