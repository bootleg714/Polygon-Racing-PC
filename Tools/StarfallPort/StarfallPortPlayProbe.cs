// Isolated native Play-mode regression: cross-scene loading, both menu paths and rotor lifecycle.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PolygonRacing;
using PolygonRacing.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class StarfallPortPlayProbe
{
    const string SessionKey="StarfallPort.PlayProbe.Active";
    const string Forest="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
    const string Output="StarfallPortInspection";
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Dictionary<Transform,Quaternion> rotationAtStart=new Dictionary<Transform,Quaternion>();
    static readonly Dictionary<Transform,Quaternion> rotationAtPause=new Dictionary<Transform,Quaternion>();
    static readonly List<InputDevice> disabledInput=new List<InputDevice>();
    static double started,stageStarted;
    static int stage,checks,errors;
    static PolygonRace race;
    static readonly Dictionary<Transform,Vector3> shuttleStart=new Dictionary<Transform,Vector3>();
    static Keyboard testKeyboard;
    static PolygonStarfallLaunch launch;
    static Vector3 launchStart,launchPaused;

    static StarfallPortPlayProbe()
    {if(SessionState.GetBool(SessionKey,false))Attach();}
    static void Attach()
    {
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
    }
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene(Forest);
            if(XRGeneralSettings.Instance)XRGeneralSettings.Instance.InitManagerOnStart=false;
            // Match the main project's fast Play settings. Explicitly attach our callback since
            // the static constructor will not run again with domain reload disabled.
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
            SessionState.SetBool(SessionKey,true);stage=checks=errors=0;started=stageStarted=0;
            Attach();EditorApplication.EnterPlaymode();
        }
        catch(Exception exception){Debug.LogException(exception);Finish(1);}
    }
    static void Log(string text,string trace,LogType type)
    {
        // Known isolated headless Editor Search bootstrap issue; game/engine errors still fail.
        if((type==LogType.Exception||type==LogType.Error)&&!(trace??"").Contains("UnityEditor.Search."))errors++;
    }
    static void Check(bool condition,string message)
    {checks++;if(!condition)throw new Exception(message);}
    static object Get(PolygonRace owner,string field)=>typeof(PolygonRace).GetField(field,Private).GetValue(owner);
    static void Set(PolygonRace owner,string field,object value)=>typeof(PolygonRace).GetField(field,Private).SetValue(owner,value);
    static void SetMode(PolygonRace owner,string field,string value)
    {var member=typeof(PolygonRace).GetField(field,Private);member.SetValue(owner,Enum.Parse(member.FieldType,value));}
    static void Invoke(PolygonRace owner,string method,params object[] args)=>typeof(PolygonRace).GetMethod(method,Private).Invoke(owner,args);
    static void Next(int nextStage)
    {stage=nextStage;stageStarted=EditorApplication.timeSinceStartup;Debug.Log("STARFALL_PLAY_STAGE: "+stage);}
    static void AcquireRace(string path,PolygonCircuitLayout layout,string mode)
    {
        Check(SceneManager.GetActiveScene().path==path,"Failed to load "+path);
        Check(PolygonCircuit.Layout==layout,"Circuit selector did not activate "+layout);
        race=Object.FindFirstObjectByType<PolygonRace>();Check(race,"New scene race controller missing");
        Check(Get(race,"mode").ToString()==mode,"Pending menu mode was lost when loading "+path);
        var player=Object.FindFirstObjectByType<PolygonDriver>();Check(player&&player.view,"New scene player/camera missing");
        Check(player.RaceManaged,"Player did not join race control after scene load");
        var rivals=Object.FindObjectsByType<PolygonRival>();Check(rivals.Length==3,"New scene did not create three opponents");
        foreach(var rival in rivals)Check(rival.Participating==(mode=="Race"),"Wrong opponent participation in "+mode);
        string phase=Get(race,"phase").ToString();Check(phase=="Countdown"||phase=="Driving","Loaded scene did not start a countdown");
    }
    static void InspectRotors()
    {
        var rotors=Object.FindObjectsByType<PolygonStarfallPortMotion>();Check(rotors.Length>0,"No rotating Ion machinery found");
        rotationAtStart.Clear();
        foreach(var rotor in rotors)
        {
            Check(rotor.transform.parent&&rotor.transform.parent.name==PolygonStarfallPortArt.AnimatedName,"Rotor did not leave static scenery before Start");
            Check(rotor.enabled,"Rotor component disabled");
            foreach(var renderer in rotor.GetComponentsInChildren<MeshRenderer>())Check(!renderer.isPartOfStaticBatch,"Rotor frozen inside a static scenery batch");
            rotationAtStart.Add(rotor.transform,rotor.transform.rotation);
        }
    }
    static void IsolateInput()
    {
        foreach(var device in InputSystem.devices)
            if(device.enabled&&(device is Keyboard||device is Gamepad))
            {disabledInput.Add(device);InputSystem.DisableDevice(device);}
        testKeyboard=InputSystem.AddDevice<Keyboard>("Starfall Port native test keyboard");testKeyboard.MakeCurrent();
    }
    static void LoadThroughSharedHandler(PolygonCircuitLayout destination,string mode)
    {
        SetMode(race,"menuMode",mode);
        Invoke(race,"LoadCircuit",destination);
    }
    static void PrepareCenterMenu()
    {
        Invoke(race,"ShowMenu");SetMode(race,"menuMode","TimeTrial");
        Set(race,"selectingCircuit",true);Set(race,"selection",(int)PolygonCircuitLayout.StarfallPort);
        Invoke(race,"DrawMenu");
        var label=(TextMesh)Get(race,"boardText");
        Check(label.text.Contains("> STARFALL PORT")&&label.text.Contains("BACK"),"Center menu selection does not point to Starfall Port");
    }
    static void CheckCenterMenuFit()
    {
        var text=(TextMesh)Get(race,"boardText");var board=(GameObject)Get(race,"board");
        var background=board.transform.Find("Panel background");Check(background,"Center menu backing panel missing");
        var bounds=text.GetComponent<Renderer>().localBounds;
        float minY=text.transform.localPosition.y+bounds.min.y*text.transform.localScale.y;
        float maxY=text.transform.localPosition.y+bounds.max.y*text.transform.localScale.y;
        float minX=text.transform.localPosition.x+bounds.min.x*text.transform.localScale.x;
        float maxX=text.transform.localPosition.x+bounds.max.x*text.transform.localScale.x;
        Check(minY>=-background.localScale.y*.5f-.01f&&maxY<=background.localScale.y*.5f+.01f,"Fourteen-line center menu overflows its backing panel vertically");
        Check(minX>=-background.localScale.x*.5f-.01f&&maxX<=background.localScale.x*.5f+.01f,"Center menu overflows its backing panel horizontally");
        Debug.Log($"STARFALL_MENU_BOUNDS: text {bounds.size.x:0.000} x {bounds.size.y:0.000} m, panel {background.localScale.x:0.000} x {background.localScale.y:0.000} m");
    }
    static void PressCenterConfirm()
    {
        testKeyboard.MakeCurrent();
        InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.Enter));InputSystem.Update();
        Invoke(race,"Update");
        InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());InputSystem.Update();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        double now=EditorApplication.timeSinceStartup;if(started==0)started=stageStarted=now;
        double elapsed=now-stageStarted;
        try
        {
            if(now-started>120)throw new Exception("Ion Play transition test timed out in stage "+stage);
            if(stage==0&&elapsed>2)
            {
                Check(EditorSettings.enterPlayModeOptionsEnabled&&EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload),"Fast Play configuration was not used");
                Check(SceneManager.GetActiveScene().path==Forest&&PolygonCircuit.Layout==PolygonCircuitLayout.Forest,"Test did not start on Forest");
                Check(Application.CanStreamedLevelBeLoaded("StarfallPort"),"Starfall Port unavailable in the live runtime scene list");
                race=Object.FindFirstObjectByType<PolygonRace>();Check(race,"Forest race controller missing");
                IsolateInput();
                // Desktop menu buttons use this same private handler. This exercises a real
                // scene transition, rather than the same-scene Begin shortcut used previously.
                LoadThroughSharedHandler(PolygonCircuitLayout.StarfallPort,"Race");Next(1);
            }
            else if(stage==1&&elapsed>1)
            {
                AcquireRace(PolygonStarfallPortSetup.ScenePath,PolygonCircuitLayout.StarfallPort,"Race");InspectRotors();foreach(var shuttle in Object.FindObjectsByType<PolygonStarfallShuttle>()){Check(shuttle.transform.parent.name==PolygonStarfallPortArt.AnimatedName,"Shuttle is still in static scenery");shuttleStart.Add(shuttle.transform,shuttle.transform.position);}Check(shuttleStart.Count==2,"Expected two cargo shuttles");launch=Object.FindFirstObjectByType<PolygonStarfallLaunch>();Check(launch&&launch.transform.parent.name==PolygonStarfallPortArt.AnimatedName,"Launch ship missing or statically batched");foreach(var renderer in launch.GetComponentsInChildren<MeshRenderer>())Check(!renderer.isPartOfStaticBatch,"Launch ship frozen inside a static batch");launchStart=launch.transform.position;Next(2);
            }
            else if(stage==2&&Get(race,"phase").ToString()=="Driving")Next(3);
            else if(stage==3&&elapsed>3.5)
            {
                foreach(var rival in Object.FindObjectsByType<PolygonRival>())
                {
                    Check(!rival.RaceLocked,"Opponent remained locked after countdown");
                    Check(rival.RaceDistance>30,"Opponent did not progress on Starfall Port");
                    Check(float.IsFinite(rival.transform.position.y),"Invalid opponent elevation");
                }
                foreach(var pair in rotationAtStart)
                {
                    Check(pair.Key,"Rotor destroyed unexpectedly");
                    Check(Quaternion.Angle(pair.Value,pair.Key.rotation)>10,"Rotor failed to rotate after entering Play");
                    rotationAtPause.Add(pair.Key,pair.Key.rotation);
                }
                foreach(var shuttle in shuttleStart)Check(Vector3.Distance(shuttle.Key.position,shuttle.Value)>5,"Cargo shuttle did not move");
                Check(Vector3.Distance(launch.transform.position,launchStart)>5,"Launch ship did not move");
                var player=Object.FindFirstObjectByType<PolygonDriver>();
                typeof(PolygonDriver).GetField("paused",Private).SetValue(player,true);launchPaused=launch.transform.position;Next(4);
            }
            else if(stage==4&&elapsed>.45)
            {
                foreach(var pair in rotationAtPause)Check(Quaternion.Angle(pair.Value,pair.Key.rotation)<.1f,"Machinery did not honor pause");
                Check(Vector3.Distance(launch.transform.position,launchPaused)<.1f,"Launch ship did not honor pause");
                Object.FindFirstObjectByType<PolygonDriver>().ResumeFromPause();
                LoadThroughSharedHandler(PolygonCircuitLayout.Forest,"TimeTrial");Next(5);
            }
            else if(stage==5&&elapsed>1)
            {
                AcquireRace(Forest,PolygonCircuitLayout.Forest,"TimeTrial");
                Check(Application.CanStreamedLevelBeLoaded("StarfallPort"),"Ion scene registration lost after returning to Forest");
                PrepareCenterMenu();Next(6);
            }
            else if(stage==6&&elapsed>.3)
            {CheckCenterMenuFit();PressCenterConfirm();Next(7);}
            else if(stage==7&&elapsed>1)
            {AcquireRace(PolygonStarfallPortSetup.ScenePath,PolygonCircuitLayout.StarfallPort,"TimeTrial");Next(8);}
            else if(stage==8&&Get(race,"phase").ToString()=="Driving")Next(9);
            else if(stage==9&&elapsed>1)
            {
                foreach(var rival in Object.FindObjectsByType<PolygonRival>())Check(!rival.Participating&&rival.RaceLocked,"Time-trial opponents became active");
                var previous=race;var sceneHandle=SceneManager.GetActiveScene().handle;
                LoadThroughSharedHandler(PolygonCircuitLayout.StarfallPort,"TimeTrial");
                Check(race==previous&&SceneManager.GetActiveScene().handle==sceneHandle,"Same-track restart unnecessarily reloaded the scene");
                Check(Get(race,"phase").ToString()=="Countdown"&&(float)Get(race,"elapsed")==0,"Same-track restart did not reset timing/countdown");
                Check(errors==0,"Play mode logged "+errors+" game/engine errors");
                Directory.CreateDirectory(Output);
                File.WriteAllText(Output+"/play-mode-report.txt",$"Native Play mode: {checks} checks passed.\nFast Enter Play: domain and scene reload disabled, matching the main project.\nForest -> Starfall Port Race via shared desktop circuit handler: passed.\nCountdown, three progressing opponents and valid elevation: passed.\nMachinery and launch ship detachment, movement and pause behavior: passed.\nStarfall Port -> Forest TimeTrial -> Starfall Port TimeTrial via center-menu Enter: passed.\nFourteen-line center menu fits its backing panel: passed.\nSame-track restart, mode persistence and runtime scene availability: passed.\nGame/engine errors: {errors}.\nNo sustained target-hardware FPS or complete-lap driving claim.\n");
                Debug.Log("STARFALL_PLAY_OK: "+checks+" cross-scene/menu/race/animation checks passed");Finish(0);
            }
        }
        catch(Exception exception){Debug.LogException(exception);Finish(1);}
    }
    static void Finish(int exitCode)
    {
        SessionState.SetBool(SessionKey,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        if(testKeyboard!=null&&testKeyboard.added)InputSystem.RemoveDevice(testKeyboard);
        foreach(var device in disabledInput)if(device!=null&&device.added)InputSystem.EnableDevice(device);
        EditorApplication.Exit(exitCode);
    }
}
