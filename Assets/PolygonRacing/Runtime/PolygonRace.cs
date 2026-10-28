using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using CommonUsages=UnityEngine.XR.CommonUsages;

namespace PolygonRacing
{
    [DefaultExecutionOrder(100)]
    public sealed class PolygonRace : MonoBehaviour
    {
        enum Phase { Menu,Countdown,Driving,Finished }
        enum Mode { Race,TimeTrial }
        const int RaceLaps=3;
        PolygonDriver player;
        PolygonStartLights startLights;
        PolygonDashboardFit dashboardFit;
        PolygonRival[] rivals;
        Phase phase;
        Mode mode;
        Mode menuMode;
        float countdown,elapsed,lapStarted,bestLap,lastLap,goUntil,trialBest;
        float[] finishTimes,previousRivalLaps;
        int resetSerial,place,finalPlace,selection;
        bool selectingCircuit,selectingRivieraLighting,wasConfirm,wasMenu,navLatched;
        PolygonRivieraTimeOfDay rivieraLighting=PolygonRivieraTimeOfDay.Daytime;
        UnityEngine.XR.InputDevice left,right;
        readonly Progress progress=new Progress();
        readonly PolygonSlipstream.Charge playerSlipstream=new PolygonSlipstream.Charge();
        static bool pendingLaunch;
        static PolygonCircuitLayout pendingLayout;
        static Mode pendingMode;
        static PolygonRivieraTimeOfDay pendingRivieraLighting=PolygonRivieraTimeOfDay.Daytime;
        string BestKey=>"PolygonRacing."+PolygonCircuit.LayoutId+".TimeTrialBest";
        int FieldSize=>rivals.Length+1;
        GameObject board;
        TextMesh boardText;
        Material boardMaterial;
        string boardValue;
        public bool SlipstreamAllowed=>phase==Phase.Driving&&mode==Mode.Race&&player&&!player.IsPaused;
        public float PlayerRaceDistance=>progress.LapsTravelled*PolygonCircuit.Length;
        // Same three-second countdown: reds at 0/.5/1/1.5/2 s, then out at 3 s.
        public int StartLightCount=>phase==Phase.Countdown?Mathf.Clamp(1+Mathf.FloorToInt((3-countdown)/.5f),1,5):0;

        // Unwrapped player route distance, independent of steering/heading.
        sealed class Progress
        {
            float previous;
            public float LapsTravelled {get;private set;}
            public int Laps=>Mathf.Max(0,Mathf.FloorToInt(LapsTravelled));
            public void Reset(Vector3 p){previous=PolygonCircuit.ProjectLap(p,out _);LapsTravelled=0;}
            public void Tick(Vector3 p)
            {
                float current=PolygonMarinaPit.ProjectRaceLap(p,out float offset);
                float delta=Mathf.Repeat(current-previous+.5f,1)-.5f;
                if(Mathf.Abs(delta)<.04f&&offset<PolygonWorld.RoadHalfWidth+8)LapsTravelled+=delta;
                previous=current;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
        // Fast Enter Play Mode may reuse the open scene without a sceneLoaded event.
        // This idempotent check also leaves normal scene switching on the existing callback.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInitialScene(){SceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
        static void SceneLoaded(Scene scene,LoadSceneMode loadMode)
        {
            var driver=Object.FindFirstObjectByType<PolygonDriver>();
            if(!driver||Object.FindFirstObjectByType<PolygonRace>())return;
            var go=new GameObject("Race menu and timing");SceneManager.MoveGameObjectToScene(go,driver.gameObject.scene);go.AddComponent<PolygonRace>();
        }
        void Start()
        {
            player=Object.FindFirstObjectByType<PolygonDriver>();
            startLights=Object.FindFirstObjectByType<PolygonStartLights>();
            rivals=Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None);
            System.Array.Sort(rivals,(a,b)=>a.GridSlot.CompareTo(b.GridSlot));
            if(!player){enabled=false;return;}
            if(player.dashboard)
            {
                var fitter=GetComponent<PolygonDashboardFit>();
                if(!fitter)fitter=gameObject.AddComponent<PolygonDashboardFit>();
                fitter.Configure(player.dashboard,player.transform.Find("Dashboard"));
                dashboardFit=fitter;
            }
            finishTimes=new float[rivals.Length];previousRivalLaps=new float[rivals.Length];
            CreateBoard();ShowMenu();
            if(pendingLaunch)
            {
                pendingLaunch=false;
                rivieraLighting=pendingRivieraLighting;
                if(PolygonCircuit.Layout==pendingLayout)Begin(pendingMode);
                else Debug.LogError("The requested circuit scene loaded with the wrong circuit layout.");
            }
        }
        void CreateBoard()
        {
            board=new GameObject("Countdown and results - cockpit panel");board.transform.SetParent(player.transform,false);board.transform.localPosition=new Vector3(0,1.32f,1.6f);
            var background=GameObject.CreatePrimitive(PrimitiveType.Cube);background.name="Panel background";background.transform.SetParent(board.transform,false);background.transform.localScale=new Vector3(1.5f,.84f,.018f);
            var collider=background.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var shader=PolygonLook.LitShader();
            boardMaterial=new Material(shader){name="Race panel charcoal",color=new Color(.025f,.045f,.065f)};
            background.GetComponent<Renderer>().sharedMaterial=boardMaterial;
            boardText=PolygonWorld.Label("Race display",board.transform,"",new Vector3(0,0,-.025f),.009f,new Color(.5f,1,.78f));
        }
        void ShowMenu()
        {
            ClearSlipstream();
            phase=Phase.Menu;player.RaceManaged=true;player.RestartRace();player.SetRaceLock(true);resetSerial=player.ResetSerial;
            foreach(var car in rivals){car.RaceManaged=true;car.SetRaceLock(true);}
            selectingCircuit=false;selectingRivieraLighting=false;selection=0;navLatched=false;DrawMenu();
        }
        void DrawMenu()
        {
            if(boardText)boardText.characterSize=selectingCircuit?.0082f:.009f;
            string restart=mode==Mode.TimeTrial?"RESTART TIME TRIAL":"RESTART RACE";
            if(selectingRivieraLighting)
            {
                SetBoard("RIVIERA LIGHTING\n"+(selection==0?"> ":"  ")+"DAYTIME\n"+(selection==1?"> ":"  ")+"SUNSET\n"+(selection==2?"> ":"  ")+"BACK\nSTICK / D-PAD / ARROWS: MOVE\nA / ENTER: SELECT  Y / M: MENU",true);
                return;
            }
            if(selectingCircuit)
            {
                SetBoard("SELECT CIRCUIT\n"+(selection==0?"> ":"  ")+"FOREST CIRCUIT\n"+(selection==1?"> ":"  ")+"COASTAL EXPRESSWAY\n"+(selection==2?"> ":"  ")+"DOWNTOWN STREETS\n"+(selection==3?"> ":"  ")+"RIVIERA HARBOUR\n"+(selection==4?"> ":"  ")+"SAKURA HILLS\n"+(selection==5?"> ":"  ")+"NORDWALD RING\n"+(selection==6?"> ":"  ")+"NEON MERIDIAN\n"+(selection==7?"> ":"  ")+"ION RIFT\n"+(selection==8?"> ":"  ")+"AURORA FORGE\n"+(selection==9?"> ":"  ")+"BACK\nSTICK / D-PAD / ARROWS: MOVE\nA / ENTER: SELECT  Y / M: MENU",true);
                return;
            }
            SetBoard("POLYGON RACING\n"+(selection==0?"> ":"  ")+"START RACE\n"+(selection==1?"> ":"  ")+"TIME TRIAL\n"+(selection==2?"> ":"  ")+restart+"\nSTICK / ARROWS: MOVE\nA / ENTER: SELECT  Y / M: MENU",true);
        }
        void Begin(Mode selected)
        {
            ClearSlipstream();
            mode=selected;
            if(PolygonCircuit.Layout==PolygonCircuitLayout.Riviera)
            {
                var lighting=player.GetComponent<PolygonRivieraLighting>();
                if(!lighting&&rivieraLighting==PolygonRivieraTimeOfDay.Sunset)lighting=player.gameObject.AddComponent<PolygonRivieraLighting>();
                if(lighting)lighting.Apply(rivieraLighting);
            }
            player.RestartRace();player.RaceManaged=true;player.SetRaceLock(true);resetSerial=player.ResetSerial;
            trialBest=PlayerPrefs.GetFloat(BestKey,0);
            foreach(var car in rivals){car.RestartRace();car.RaceManaged=true;car.SetParticipating(mode==Mode.Race);car.SetRaceLock(true);}
            for(int i=0;i<finishTimes.Length;i++){finishTimes[i]=-1;previousRivalLaps[i]=rivals[i].RaceDistance/PolygonCircuit.Length;}
            progress.Reset(player.transform.position);
            countdown=3;elapsed=lapStarted=bestLap=lastLap=goUntil=0;place=FieldSize;finalPlace=0;
            phase=Phase.Countdown;SetBoard("3\nGET READY",!startLights);
        }
        static bool Button(UnityEngine.XR.InputDevice d,InputFeatureUsage<bool> key)=>d.TryGetFeatureValue(key,out var value)&&value;
        void Update()
        {
            if(!player||rivals==null)return;
            if(!left.isValid)left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if(!right.isValid)right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var keyboard=Keyboard.current;
            var gamepad=Gamepad.current;
            bool confirm=Button(right,CommonUsages.primaryButton)||(keyboard!=null&&keyboard.enterKey.isPressed)||(gamepad!=null&&gamepad.buttonSouth.isPressed);
            bool menu=Button(left,CommonUsages.secondaryButton)||(keyboard!=null&&keyboard.mKey.isPressed)||(gamepad!=null&&gamepad.buttonNorth.isPressed);
            bool confirmPressed=confirm&&!wasConfirm,menuPressed=menu&&!wasMenu;
            wasConfirm=confirm;wasMenu=menu;
            if(menuPressed){ShowMenu();return;}
            if(phase==Phase.Menu)
            {
                // The driver's A/R reset is harmless in the menu; absorb its serial change.
                resetSerial=player.ResetSerial;player.SetRaceLock(true);
                float y=0;if(left.TryGetFeatureValue(CommonUsages.primary2DAxis,out var stick))y=stick.y;
                if(gamepad!=null)
                {
                    y=gamepad.leftStick.y.ReadValue();
                    if(Mathf.Abs(gamepad.dpad.y.ReadValue())>.6f)y=gamepad.dpad.y.ReadValue();
                }
                if(keyboard!=null){if(keyboard.upArrowKey.isPressed)y=1;if(keyboard.downArrowKey.isPressed)y=-1;}
                bool held=Mathf.Abs(y)>.6f;
                int options=selectingRivieraLighting?3:selectingCircuit?10:3;
                if(held&&!navLatched){selection=(selection+(y>0?options-1:1))%options;DrawMenu();}
                navLatched=held;
                if(confirmPressed)
                {
                    if(selectingRivieraLighting)
                    {
                        if(selection==2)BackToCircuits();
                        else SelectRivieraLighting(selection==0?PolygonRivieraTimeOfDay.Daytime:PolygonRivieraTimeOfDay.Sunset);
                    }
                    else if(selectingCircuit)
                    {
                        if(selection==9){selectingCircuit=false;selection=menuMode==Mode.Race?0:1;DrawMenu();}
                        else LoadCircuit((PolygonCircuitLayout)selection);
                    }
                    else if(selection<2)
                    {
                        menuMode=selection==0?Mode.Race:Mode.TimeTrial;
                        selectingCircuit=true;selection=0;DrawMenu();
                    }
                    else Begin(mode);
                }
                return;
            }
            if(player.ResetSerial!=resetSerial){Begin(mode);return;}
            if(player.IsPaused)
            {
                player.SetSlipstreamStrength(0);
                foreach(var car in rivals)car.SetSlipstreamStrength(0);
                return;
            }
            if(phase==Phase.Countdown)
            {
                countdown-=Mathf.Min(Time.deltaTime,.05f);
                if(countdown<=0)
                {
                    phase=Phase.Driving;player.SetRaceLock(false);
                    if(mode==Mode.Race)foreach(var car in rivals)car.SetRaceLock(false);
                    goUntil=1;SetBoard("GO!",!startLights);
                }
                return;
            }
            if(phase!=Phase.Driving)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;
            float previousPlayerLaps=progress.LapsTravelled;
            int before=progress.Laps;progress.Tick(player.transform.position);
            UpdatePlayerSlipstream(dt);
            // Rivals report their actual accumulated route distance, rather than estimating
            // their lap count from a sideways offset on the visible road.
            place=1;
            if(mode==Mode.Race)for(int i=0;i<rivals.Length;i++)
            {
                float laps=rivals[i].RaceDistance/PolygonCircuit.Length;
                if(laps>=RaceLaps&&finishTimes[i]<0)
                {finishTimes[i]=elapsed-dt+dt*Mathf.InverseLerp(previousRivalLaps[i],laps,RaceLaps);rivals[i].SetRaceLock(true);}
                previousRivalLaps[i]=laps;
                if(finishTimes[i]>=0||laps>progress.LapsTravelled+.0001f)place++;
            }
            if(progress.Laps>before)
            {
                lastLap=elapsed-lapStarted;lapStarted=elapsed;
                if(bestLap==0||lastLap<bestLap)bestLap=lastLap;
                if(mode==Mode.TimeTrial&&(trialBest==0||lastLap<trialBest))
                {trialBest=lastLap;PlayerPrefs.SetFloat(BestKey,trialBest);PlayerPrefs.Save();}
            }
            if(mode==Mode.Race&&progress.Laps>=RaceLaps)
            {
                float playerFinish=elapsed-dt+dt*Mathf.InverseLerp(previousPlayerLaps,progress.LapsTravelled,RaceLaps);
                finalPlace=1;foreach(float time in finishTimes)if(time>=0&&time<=playerFinish)finalPlace++;
                place=finalPlace;phase=Phase.Finished;player.SetRaceLock(true);foreach(var car in rivals)car.SetRaceLock(true);
                ClearSlipstream();
                SetBoard($"FINISH - {Ordinal(finalPlace)} PLACE\nTOTAL {PolygonDriver.FormatTime(elapsed)}\nBEST LAP {PolygonDriver.FormatTime(bestLap)}\nA / R: RACE AGAIN\nY / M: MENU",true);
            }
            else if(elapsed>=goUntil)SetBoard("",false);
        }
        void LateUpdate()
        {
            if(!player||!player.dashboard||rivals==null)return;
            if(player.IsPaused){Dashboard("PAUSED","","","P / START");return;}
            if(phase==Phase.Menu){DrawMenu();Dashboard("SELECT MODE","","","");return;}
            if(phase==Phase.Countdown)
            {
                SetBoard(Mathf.Max(1,Mathf.CeilToInt(countdown))+"\nGET READY",!startLights);
                Dashboard("GET READY","","",mode==Mode.Race?"3 LAP RACE":"TIME TRIAL");return;
            }
            if(phase==Phase.Finished){Dashboard("FINISH",finalPlace+"/"+FieldSize,"","R / A: RESTART");return;}
            string speed=$"{player.SpeedMph:000} MPH";
            if(mode==Mode.TimeTrial)
                Dashboard(speed,"L "+DashboardTime(elapsed-lapStarted),"B "+(trialBest>0?DashboardTime(trialBest):"--:--.-"),player.IsOnPitLane?"PIT 50 MPH":"TIME TRIAL");
            else
            {
                float leader=progress.LapsTravelled;int finished=0;
                for(int i=0;i<rivals.Length;i++){leader=Mathf.Max(leader,rivals[i].RaceDistance/PolygonCircuit.Length);if(finishTimes[i]>=0)finished++;}
                int lapGap=Mathf.FloorToInt(Mathf.Max(0,leader-progress.LapsTravelled));
                string gap=finished>0?" "+finished+" FIN":lapGap>0?" +"+lapGap+"L":"";
                Dashboard(speed,"P"+place+"/"+FieldSize+gap,"L"+Mathf.Min(RaceLaps,progress.Laps+1)+"/"+RaceLaps+" "+DashboardTime(elapsed-lapStarted),DraftIndicator());
            }
        }
        void Dashboard(string main,string left,string right,string status)
        {
            if(dashboardFit&&dashboardFit.Ready)dashboardFit.SetRows(main,left,right,status);
            else player.dashboard.text=main+"\n"+left+" "+right+"\n"+status;
        }
        // Keep long sessions bounded on the small physical screen; the desktop HUD and
        // saved lap records retain their original full-precision timing and formatting.
        static string DashboardTime(float seconds)
        {
            if(seconds<6000)return PolygonDriver.FormatTime(seconds);
            if(seconds>=360000)return ">99H";
            int totalMinutes=Mathf.FloorToInt(seconds/60);
            return $"{totalMinutes/60:00}H{totalMinutes%60:00}";
        }
        void UpdatePlayerSlipstream(float deltaTime)
        {
            float alignment=0;
            if(mode==Mode.Race)
            {
                foreach(var car in rivals)
                {
                    if(!car||!car.Participating||car.RaceLocked)continue;
                    alignment=Mathf.Max(alignment,PolygonSlipstream.Measure(player.transform.position,
                        player.transform.forward,PlayerRaceDistance,car.transform.position,car.transform.forward,car.RaceDistance));
                }
            }
            player.SetSlipstreamStrength(playerSlipstream.Advance(alignment,deltaTime));
        }
        string DraftIndicator()
        {
            if(player.IsOnPitLane)return "PIT 50 MPH";
            int bars=Mathf.Clamp(Mathf.CeilToInt(playerSlipstream.Buildup*6),0,6);
            string gauge=new string('|',bars)+new string('.',6-bars);
            return playerSlipstream.Strength>=.65f?$"BOOST [{gauge}]":$"DRAFT [{gauge}]";
        }
        void ClearSlipstream()
        {
            playerSlipstream.Reset();
            if(player)player.SetSlipstreamStrength(0);
            if(rivals!=null)foreach(var car in rivals)if(car)car.ResetSlipstream();
        }
        static string Ordinal(int n)=>n==1?"1ST":n==2?"2ND":n==3?"3RD":"4TH";
        void SetBoard(string value,bool visible)
        {
            if(!board)return;if(board.activeSelf!=visible)board.SetActive(visible);
            if(boardValue!=value){boardValue=value;boardText.text=value;}
        }
        void LoadCircuit(PolygonCircuitLayout layout)
        {
            if(layout==PolygonCircuitLayout.Riviera)
            {
                selectingCircuit=false;selectingRivieraLighting=true;selection=0;DrawMenu();return;
            }
            StartCircuit(layout);
        }
        void SelectRivieraLighting(PolygonRivieraTimeOfDay preset)
        {
            rivieraLighting=preset;
            if(StartCircuit(PolygonCircuitLayout.Riviera))selectingRivieraLighting=false;
        }
        void BackToCircuits()
        {
            selectingRivieraLighting=false;selectingCircuit=true;selection=3;DrawMenu();
        }
        bool StartCircuit(PolygonCircuitLayout layout)
        {
            string sceneName=layout==PolygonCircuitLayout.AuroraForge?"AuroraForge":layout==PolygonCircuitLayout.IonRift?"IonRift":layout==PolygonCircuitLayout.NeonMeridian?"NeonMeridian":layout==PolygonCircuitLayout.Forest?"ForestCircuit":layout==PolygonCircuitLayout.Downtown?"DowntownCircuit":layout==PolygonCircuitLayout.Riviera?"RivieraHarbour":layout==PolygonCircuitLayout.Sakura?"SakuraHills":layout==PolygonCircuitLayout.Nordwald?"NordwaldRing":"CoastalExpressway";
            if(PolygonCircuit.Layout==layout){Begin(menuMode);return true;}
            if(!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot load {sceneName}. Add its scene to the active build profile before selecting this circuit.");
                return false;
            }
            pendingLaunch=true;pendingLayout=layout;pendingMode=menuMode;
            pendingRivieraLighting=layout==PolygonCircuitLayout.Riviera?rivieraLighting:PolygonRivieraTimeOfDay.Daytime;
            SceneManager.LoadScene(sceneName);return true;
        }
        void OnGUI()
        {
            if(!player||rivals==null||!player.view||player.view.stereoEnabled)return;
            var safe=PolygonDesktopHUD.SafeArea;float scale=PolygonDesktopHUD.Scale(safe);
            if(phase==Phase.Menu)
            {
                DrawDesktopMenu(safe,scale);
                return;
            }
            var race=PolygonDesktopHUD.RaceRect(safe,scale);
            var help=PolygonDesktopHUD.HelpRect(safe,scale,player.IsPaused);
            string heading=mode==Mode.TimeTrial?"TIME TRIAL":"POSITION  "+place+" / "+FieldSize;
            string lap=mode==Mode.TimeTrial?"LAP  "+Mathf.Max(1,progress.Laps+1):"LAP  "+Mathf.Min(RaceLaps,progress.Laps+1)+" / "+RaceLaps;
            float current=phase==Phase.Finished?lastLap:Mathf.Max(0,elapsed-lapStarted);
            float best=mode==Mode.TimeTrial?trialBest:bestLap;
            PolygonDesktopHUD.Race(race,scale,heading,lap,PolygonDriver.FormatTime(current),best>0?PolygonDriver.FormatTime(best):"--:--.-");
            if(player.IsPaused)
            {
                PolygonDesktopHUD.Help(help,scale,player.UsingGamepad,true,true);
                float y=help.y+Mathf.Min(104*scale,Mathf.Max(0,help.height-32*scale)),w=(help.width-40*scale)/3;
                if(GUI.Button(new Rect(help.x+10*scale,y,w,27*scale),"RESUME"))player.ResumeFromPause();
                if(GUI.Button(new Rect(help.x+20*scale+w,y,w,27*scale),"RESTART"))Begin(mode);
                if(GUI.Button(new Rect(help.x+30*scale+2*w,y,w,27*scale),"MENU"))ShowMenu();
            }
            else if(phase==Phase.Driving&&elapsed<8)
                PolygonDesktopHUD.Help(help,scale,player.UsingGamepad,false,true,Mathf.Clamp01((8-elapsed)/2));
            if(player.ShowDesktopDebug)PolygonDesktopHUD.Debug(safe,race,help,scale,player.HudFps,player);
        }
        void DrawDesktopMenu(Rect safe,float scale)
        {
            int count=selectingRivieraLighting?3:selectingCircuit?10:3;
            float width=Mathf.Min(240*scale,safe.width-24*scale);
            float step=Mathf.Min(42*scale,(safe.height-55*scale)/count);
            float x=safe.x+12*scale,y=safe.y+12*scale;
            PolygonDesktopHUD.Panel(new Rect(x,y,width,Mathf.Min(safe.height-24*scale,35*scale+step*count)));
            PolygonDesktopHUD.Label(new Rect(x+10*scale,y+5*scale,width-20*scale,23*scale),
                selectingRivieraLighting?"RIVIERA LIGHTING":selectingCircuit?"SELECT CIRCUIT":"SELECT MODE",Mathf.Max(12,Mathf.RoundToInt(15*scale)),TextAnchor.MiddleLeft);
            var style=new GUIStyle(GUI.skin.button){fontSize=Mathf.Max(10,Mathf.RoundToInt(13*scale))};
            bool ButtonAt(int i,string label)=>GUI.Button(new Rect(x+8*scale,y+31*scale+i*step,width-16*scale,Mathf.Max(1,step-5*scale)),label,style);
            if(selectingRivieraLighting)
            {
                if(ButtonAt(0,"DAYTIME"))SelectRivieraLighting(PolygonRivieraTimeOfDay.Daytime);
                if(ButtonAt(1,"SUNSET"))SelectRivieraLighting(PolygonRivieraTimeOfDay.Sunset);
                if(ButtonAt(2,"BACK"))BackToCircuits();
            }
            else if(selectingCircuit)
            {
                if(ButtonAt(0,"FOREST CIRCUIT"))LoadCircuit(PolygonCircuitLayout.Forest);
                if(ButtonAt(1,"COASTAL EXPRESSWAY"))LoadCircuit(PolygonCircuitLayout.CoastalExpressway);
                if(ButtonAt(2,"DOWNTOWN STREETS"))LoadCircuit(PolygonCircuitLayout.Downtown);
                if(ButtonAt(3,"RIVIERA HARBOUR"))LoadCircuit(PolygonCircuitLayout.Riviera);
                if(ButtonAt(4,"SAKURA HILLS"))LoadCircuit(PolygonCircuitLayout.Sakura);
                if(ButtonAt(5,"NORDWALD RING"))LoadCircuit(PolygonCircuitLayout.Nordwald);
                if(ButtonAt(6,"NEON MERIDIAN"))LoadCircuit(PolygonCircuitLayout.NeonMeridian);
                if(ButtonAt(7,"ION RIFT"))LoadCircuit(PolygonCircuitLayout.IonRift);
                if(ButtonAt(8,"AURORA FORGE"))LoadCircuit(PolygonCircuitLayout.AuroraForge);
                if(ButtonAt(9,"BACK")){selectingCircuit=false;selection=menuMode==Mode.Race?0:1;DrawMenu();}
            }
            else
            {
                if(ButtonAt(0,"START RACE")){menuMode=Mode.Race;selectingCircuit=true;selection=0;DrawMenu();}
                if(ButtonAt(1,"TIME TRIAL")){menuMode=Mode.TimeTrial;selectingCircuit=true;selection=0;DrawMenu();}
                if(ButtonAt(2,"RESTART CURRENT MODE"))Begin(mode);
            }
        }
        void OnDestroy()
        {
            ClearSlipstream();
            if(player){player.RaceManaged=false;player.SetRaceLock(false);}
            if(rivals!=null)foreach(var car in rivals)if(car){car.RaceManaged=false;car.SetParticipating(true);car.SetRaceLock(false);}
            if(board)Destroy(board);if(boardMaterial)Destroy(boardMaterial);
        }
    }
}
