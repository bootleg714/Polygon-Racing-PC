using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace PolygonRacing
{
    // Flat-track arcade handling; intentionally no suspension bounce or camera shake.
    public sealed class PolygonDriver : MonoBehaviour
    {
        public static readonly Vector3 Seat = new Vector3(0,1.03f,-.53f);
        public Camera view;
        public Transform trackingSpace, steeringWheel;
        public Transform[] frontSteering, wheels;
        public TextMesh dashboard;
        [Range(10,80)] public float topSpeed = 67.056f;
        [Range(5,25)] public float acceleration = 13f;
        [Range(10,40)] public float braking = 24f;
        [Range(10,40)] public float steeringAngle = 28f;
        [Header("Experimental corner grip")]
        [Tooltip("Lateral acceleration available to turn the direction of travel, in metres per second squared. Higher values give more grip.")]
        [Range(1,60)] public float cornerGrip = 60f;
        [Tooltip("Maximum difference in degrees between the car's heading and its direction of travel.")]
        [Range(0,12)] public float maxSlipAngle = 5f;
        public float SpeedKmh => speed * 3.6f;
        public float SpeedMph => speed * 2.23693629f;
        public float CornerSlip { get; private set; }
        public bool IsPaused => paused;
        public bool UsingGamepad { get; private set; }
        public bool ShowDesktopDebug { get; private set; }
        public float HudSessionTime { get; private set; }
        public float HudFps { get; private set; }
        public PolygonDrivetrain Drivetrain { get; } = new PolygonDrivetrain();
        public bool IsOnPitLane { get; private set; }
        public int ResetSerial { get; private set; }
        public bool RaceManaged { get; set; }
        public bool RaceLocked { get; private set; }
        public void SetRaceLock(bool locked){if(locked&&!RaceLocked)Drivetrain.Reset();RaceLocked=locked;if(locked){speed=0;CornerSlip=0;SetSlipstreamStrength(0);}}
        public void SetSlipstreamStrength(float strength){slipstreamStrength=Mathf.Clamp01(strength);}
        public void RestartRace(){ResetCar();paused=false;}
        float speed, steering, wheelSpin, lapTime, bestTime, lastLap, slipstreamStrength;
        float travelHeading;
        bool travelHeadingValid;
        int lap=1, nextCheckpoint=1;
        bool paused, centered, wasReset, wasRecenter, wasPause, xrActive;
        float nextStatus;
        string status="READY - Right trigger to drive";
        Vector3 startPosition;
        Quaternion startRotation;
        readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        InputDevice head, left, right;
        PolygonEngineSound engine;

        void Start()
        {
            startPosition=PolygonWorld.Point(0)+Vector3.up*PolygonCircuit.Elevation(0);startRotation=Quaternion.LookRotation(PolygonWorld.Tangent(0));
            Application.targetFrameRate=72;
            var scenery=transform.parent.Find("Circuit - editable scenery");
            if(scenery)StaticBatchingUtility.Combine(scenery.gameObject);
            engine=GetComponent<PolygonEngineSound>();
            if(!engine)engine=gameObject.AddComponent<PolygonEngineSound>();
        }
        void OnEnable() { Application.onBeforeRender+=TrackHead; }
        void OnDisable() { Application.onBeforeRender-=TrackHead; }
        void RefreshDevices()
        {
            if(!head.isValid)head=InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if(!left.isValid)left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if(!right.isValid)right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            subsystems.Clear();SubsystemManager.GetSubsystems(subsystems);
            bool running=false;
            foreach(var s in subsystems)if(s.running){running=true;if(!xrActive)s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);}
            if(running!=xrActive){xrActive=running;centered=false;}
        }
        void TrackHead()
        {
            if(!xrActive || !head.isValid || !view)return;
            if(head.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked) && !tracked)return;
            if(!head.TryGetFeatureValue(CommonUsages.devicePosition,out var p) || !head.TryGetFeatureValue(CommonUsages.deviceRotation,out var q))return;
            if(!centered)
            {
                var yaw=Quaternion.Euler(0,-q.eulerAngles.y,0);
                trackingSpace.localRotation=yaw;trackingSpace.localPosition=Seat-yaw*p;centered=true;
            }
            view.transform.localPosition=p;view.transform.localRotation=q;
        }
        static bool Button(InputDevice d, InputFeatureUsage<bool> key) => d.TryGetFeatureValue(key,out var v) && v;
        static float Axis(InputDevice d, InputFeatureUsage<float> key) => d.TryGetFeatureValue(key,out var v)?v:0;
        void Update()
        {
            RefreshDevices();TrackHead();
            float throttle=Axis(right,CommonUsages.trigger),brake=Axis(left,CommonUsages.trigger),turn=0;
            if(left.TryGetFeatureValue(CommonUsages.primary2DAxis,out var stick))turn=stick.x;
            bool reset=Button(right,CommonUsages.primaryButton),recenter=Button(right,CommonUsages.secondaryButton),pause=Button(left,CommonUsages.primaryButton);
            var gamepad=Gamepad.current;
            if(gamepad!=null)
            {
                throttle=Mathf.Max(throttle,gamepad.rightTrigger.ReadValue());
                brake=Mathf.Max(brake,gamepad.leftTrigger.ReadValue());
                turn+=gamepad.leftStick.x.ReadValue();
                reset|=gamepad.buttonSouth.isPressed;
                recenter|=gamepad.buttonEast.isPressed;
                pause|=gamepad.startButton.isPressed;
            }
            if(gamepad!=null&&gamepad.wasUpdatedThisFrame&&(gamepad.buttonSouth.wasPressedThisFrame||gamepad.buttonNorth.wasPressedThisFrame||
                gamepad.buttonEast.wasPressedThisFrame||gamepad.startButton.wasPressedThisFrame||
                gamepad.leftStick.ReadValue().sqrMagnitude>.04f||gamepad.dpad.ReadValue().sqrMagnitude>.04f||
                gamepad.rightTrigger.ReadValue()>.1f||gamepad.leftTrigger.ReadValue()>.1f))UsingGamepad=true;
            var k=Keyboard.current;
            if(k!=null)
            {
                if(k.anyKey.wasPressedThisFrame)UsingGamepad=false;
                if(k.f3Key.wasPressedThisFrame)ShowDesktopDebug=!ShowDesktopDebug;
                if(k.wKey.isPressed||k.upArrowKey.isPressed)throttle=1;
                if(k.sKey.isPressed||k.downArrowKey.isPressed||k.spaceKey.isPressed)brake=1;
                turn+= (k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
                reset|=k.rKey.isPressed;recenter|=k.cKey.isPressed;pause|=k.pKey.isPressed;
            }
            if(reset&&!wasReset)ResetCar();if(recenter&&!wasRecenter)centered=false;
            if(pause&&!wasPause){paused=!paused;status=paused?"PAUSED":"DRIVE";nextStatus=Time.time+3;}
            wasReset=reset;wasRecenter=recenter;wasPause=pause;
            if(!xrActive)
            {
                trackingSpace.localPosition=Seat;trackingSpace.localRotation=Quaternion.identity;
                view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;
            }
            if(Time.unscaledDeltaTime>0)HudFps=Mathf.Lerp(HudFps,1f/Time.unscaledDeltaTime,.05f);
            if(!paused && !RaceLocked)
            {
                HudSessionTime+=Time.deltaTime;
                float dt=Mathf.Min(Time.deltaTime,.05f);
                steering=Mathf.MoveTowards(steering,Mathf.Clamp(turn,-1,1),dt*3.8f);
                int nearest=NearestPoint(transform.position,out float distance);
                bool pit=PolygonMarinaPit.TrySample(transform.position,distance,out _,out float pitOffset,out _);
                IsOnPitLane=pit&&distance>PolygonWorld.RoadHalfWidth+1;
                float surfaceHalfWidth=pit?PolygonMarinaPit.HalfWidth:PolygonWorld.RoadHalfWidth;
                if(pit)distance=pitOffset;
                bool offroad=distance>surfaceHalfWidth;
                float boostedTopSpeed=topSpeed*(1+PolygonSlipstream.SpeedBonus*slipstreamStrength);
                float boostedAcceleration=acceleration*(1+PolygonSlipstream.AccelerationBonus*slipstreamStrength);
                float cap=offroad?14:IsOnPitLane?Mathf.Min(PolygonMarinaPit.SpeedLimit,boostedTopSpeed):boostedTopSpeed;
                speed=Mathf.Clamp(speed+(throttle*boostedAcceleration-brake*braking-1.3f-speed*.025f-(offroad?4:0))*dt,0,Mathf.Max(cap,speed));
                if(speed>cap)speed=Mathf.MoveTowards(speed,cap,dt*12);
                // Decrease steering lock at speed to keep cornering predictable.
                float lockDegrees=steeringAngle/(1+speed*.065f);
                float yaw=speed/2.85f*Mathf.Tan(steering*lockDegrees*Mathf.Deg2Rad)*Mathf.Rad2Deg;
                // Steering stays in the horizontal route plane; bank roll is applied below.
                var flatForward=transform.forward;flatForward.y=0;flatForward.Normalize();
                float heading=Mathf.Atan2(flatForward.x,flatForward.z)*Mathf.Rad2Deg+yaw*dt;
                if(!travelHeadingValid || speed<1f){travelHeading=heading;travelHeadingValid=true;}
                float maxTravelTurn=Mathf.Max(1f,cornerGrip)/Mathf.Max(speed,1f)*Mathf.Rad2Deg*dt;
                travelHeading=Mathf.MoveTowardsAngle(travelHeading,heading,maxTravelTurn);
                // Limit body-to-travel slip by restraining the body heading, rather than
                // pulling travel into the turn beyond the available grip.
                float slip=Mathf.Clamp(Mathf.DeltaAngle(heading,travelHeading),-Mathf.Max(0,maxSlipAngle),Mathf.Max(0,maxSlipAngle));
                CornerSlip=maxSlipAngle>0.01f?Mathf.Clamp01(Mathf.Abs(slip)/maxSlipAngle):0;
                heading=travelHeading-slip;
                transform.rotation=Quaternion.Euler(0,heading,0);
                transform.position+=Quaternion.Euler(0,travelHeading,0)*Vector3.forward*speed*dt;
                NearestPoint(transform.position,out float after);
                bool afterPit=PolygonMarinaPit.TrySample(transform.position,after,out var pitCenter,out float pitAfter,out _);
                if(afterPit)after=pitAfter;
                float barrierLimit=(afterPit?PolygonMarinaPit.HalfWidth:PolygonWorld.RoadHalfWidth)+PolygonCircuit.BarrierMargin;
                if(after>barrierLimit)
                {
                    var center=afterPit?pitCenter:PolygonCircuit.AtDistance(PolygonCircuit.ProjectLap(transform.position,out _)*PolygonCircuit.Length);
                    var offset=transform.position-center;offset.y=0;
                    transform.position=center+offset.normalized*(barrierLimit-.1f);speed=Mathf.Min(speed,7);
                    status="BARRIER - A / R to restart";nextStatus=Time.time+2;
                }
                PolygonBanking.AlignPlayer(transform);
                if(speed>.1f||lapTime>0)lapTime+=Time.deltaTime;
                var checkpoint=PolygonWorld.Point((nextCheckpoint%16)*Mathf.PI*2/16);
                var flat=transform.position;flat.y=0;
                // Height check: on a figure-eight a checkpoint can sit on the road above or below.
                float checkpointHeight=PolygonCircuit.Elevation(PolygonCircuit.DistanceAtKnot((nextCheckpoint%16)*PolygonCircuit.KnotCount/16f));
                if(Vector3.Distance(flat,checkpoint)<22 && Mathf.Abs(transform.position.y-checkpointHeight)<5 && Vector3.Dot(transform.forward,PolygonWorld.Tangent(nextCheckpoint*Mathf.PI*2/16))>.2f)
                {
                    nextCheckpoint++;
                    if(nextCheckpoint>16){lastLap=lapTime;if(bestTime==0||lapTime<bestTime)bestTime=lapTime;lap++;lapTime=0;nextCheckpoint=1;status="LAP COMPLETE";nextStatus=Time.time+5;}
                }
                wheelSpin=(wheelSpin+speed/.39f*Mathf.Rad2Deg*dt)%360;
                if(frontSteering!=null)foreach(var w in frontSteering)if(w)w.localRotation=Quaternion.Euler(0,steering*lockDegrees,0);
                if(wheels!=null)foreach(var w in wheels)if(w)w.localRotation=Quaternion.Euler(wheelSpin,0,0);
                if(steeringWheel)steeringWheel.localRotation=Quaternion.Euler(0,0,-steering*85);
            }
            if(!paused&&!RaceLocked)Drivetrain.Step(speed,throttle,Mathf.Min(Time.deltaTime,.05f));
            if(engine)engine.SetDriving(Drivetrain.Rpm,Drivetrain.Throttle,paused||RaceLocked);
            if(dashboard)dashboard.text=$"{SpeedMph:000} MPH\n{(paused?"PAUSED":"LAP "+lap)}  {FormatTime(lapTime)}";
        }
        public void ResumeFromPause(){paused=false;status="DRIVE";nextStatus=Time.time+3;}
        public static string FormatTime(float seconds) => $"{(int)(seconds/60):00}:{seconds%60:00.0}";
        static int NearestPoint(Vector3 p,out float distance)
        {
            // Road classification remains lateral, independent of bank height; a crossover layout
            // keeps the real height so the road above or below is never picked.
            if(!PolygonCircuit.HasCrossover)p.y=0;
            // Exact projection onto the route (the old 256-point lookup overstated the lateral
            // offset by up to ~5 m between samples, which matters on narrow street circuits).
            float lap=PolygonCircuit.ProjectLap(p,out distance);
            return Mathf.RoundToInt(PolygonCircuit.AngleAtDistance(lap*PolygonCircuit.Length)/(Mathf.PI*2)*256)%256;
        }
        void ResetCar()
        {
            transform.SetPositionAndRotation(startPosition,startRotation);travelHeadingValid=false;CornerSlip=0;Drivetrain.Reset();HudSessionTime=0;speed=steering=slipstreamStrength=0;IsOnPitLane=false;lapTime=0;nextCheckpoint=1;lap=1;lastLap=0;ResetSerial++;
            status="RESTARTED";nextStatus=Time.time+3;
        }
        void OnApplicationFocus(bool focus){        if(!focus){paused=true;status="PAUSED - START / X / P to resume";nextStatus=float.PositiveInfinity;}}
        void OnGUI()
        {
            // Fallback when no race manager is active; the cockpit display remains unchanged.
            if(xrActive||RaceManaged)return;
            var safe=PolygonDesktopHUD.SafeArea;float scale=PolygonDesktopHUD.Scale(safe);
            var race=PolygonDesktopHUD.RaceRect(safe,scale);var help=PolygonDesktopHUD.HelpRect(safe,scale,paused);
            PolygonDesktopHUD.Race(race,scale,"SESSION","LAP "+lap,FormatTime(lapTime),bestTime>0?FormatTime(bestTime):"--:--.-");
            if(paused)
            {
                PolygonDesktopHUD.Help(help,scale,UsingGamepad,true,false);
                float y=help.y+Mathf.Min(104*scale,Mathf.Max(0,help.height-32*scale)),w=(help.width-30*scale)*.5f;
                if(GUI.Button(new Rect(help.x+10*scale,y,w,27*scale),"RESUME"))ResumeFromPause();
                if(GUI.Button(new Rect(help.x+20*scale+w,y,w,27*scale),"RESTART"))RestartRace();
            }
            else if(HudSessionTime<8)
                PolygonDesktopHUD.Help(help,scale,UsingGamepad,false,false,Mathf.Clamp01((8-HudSessionTime)/2));
            if(ShowDesktopDebug)PolygonDesktopHUD.Debug(safe,race,help,scale,HudFps,this);
        }

    }
}
