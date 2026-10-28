// Native Play-mode smoke test. This runs only in the isolated probe project.
using System;
using System.IO;
using System.Reflection;
using PolygonRacing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Management;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class NeonPlayProbe
{
    const string Key="Neon.PlayProbe.Active";
    static double started;
    static int stage,errors,checks;
    static Vector3 initialTrain;
    static Quaternion initialFan;
    static PolygonNeonMotion train,fan;
    static PolygonRace race;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static NeonPlayProbe()
    {
        if(!SessionState.GetBool(Key,false))return;
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=Log;
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/PolygonRacing/Scenes/NeonMeridian.unity");
        if(XRGeneralSettings.Instance)XRGeneralSettings.Instance.InitManagerOnStart=false;
        EditorSettings.enterPlayModeOptionsEnabled=false;
        SessionState.SetBool(Key,true);
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        EditorApplication.EnterPlaymode();
    }
    static void Log(string text,string trace,LogType type)
    {
        // Unity's headless Search index bootstrap can throw before any game behaviour runs.
        // Record actual game/engine errors; this unrelated Editor-only startup bug is excluded.
        if((type==LogType.Exception||type==LogType.Error)&&!trace.Contains("UnityEditor.Search."))errors++;
    }
    static void Check(bool value,string message)
    {checks++;if(!value)throw new Exception(message);}
    static void Begin(string mode)
    {
        var method=typeof(PolygonRace).GetMethod("Begin",Private);
        method.Invoke(race,new[]{Enum.Parse(method.GetParameters()[0].ParameterType,mode)});
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(started==0)started=EditorApplication.timeSinceStartup;
        double elapsed=EditorApplication.timeSinceStartup-started;
        try
        {
            if(elapsed>90)throw new Exception("Play-mode check timed out");
            if(stage==0&&elapsed>2)
            {
                race=Object.FindFirstObjectByType<PolygonRace>();Check(race,"Race system did not start");
                var player=Object.FindFirstObjectByType<PolygonDriver>();Check(player&&player.view,"Player/camera missing");
                Check(PolygonCircuit.Layout==PolygonCircuitLayout.NeonMeridian,"Wrong active layout");
                Check(Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None).Length==3,"Three opponents not created");
                typeof(PolygonRace).GetField("selectingCircuit",Private).SetValue(race,true);
                typeof(PolygonRace).GetMethod("DrawMenu",Private).Invoke(race,null);
                var text=(TextMesh)typeof(PolygonRace).GetField("boardText",Private).GetValue(race);
                Check(text.text.Contains("NEON MERIDIAN")&&text.text.Contains("BACK"),"Circuit menu missing Neon/back");
                foreach(var motion in Object.FindObjectsByType<PolygonNeonMotion>(FindObjectsSortMode.None))
                {
                    Check(motion.transform.parent.name==PolygonNeonArt.AnimatedName,"Motion was not detached from static scenery");
                    foreach(var renderer in motion.GetComponentsInChildren<MeshRenderer>())Check(!renderer.isPartOfStaticBatch,"Animated mesh was frozen by static batching");
                    if(motion.Mode==PolygonNeonMotion.Motion.Shuttle)train=motion;else fan=motion;
                }
                Check(train&&fan,"Train/fans missing");initialTrain=train.transform.position;initialFan=fan.transform.rotation;
                Begin("TimeTrial");
                foreach(var rival in Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None))Check(!rival.Participating,"Time trial did not hide opponents");
                stage=1;
            }
            else if(stage==1&&elapsed>6)
            {
                Check(Vector3.Distance(initialTrain,train.transform.position)>5,"Train did not move");
                Check(Quaternion.Angle(initialFan,fan.transform.rotation)>10,"Fan did not rotate");
                Begin("Race");
                foreach(var rival in Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None))Check(rival.Participating,"Race did not restore opponents");
                stage=2;
            }
            else if(stage==2&&elapsed>17)
            {
                foreach(var rival in Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None))
                {
                    Check(!rival.RaceLocked,"Opponent remained locked after countdown");
                    Check(rival.RaceDistance>30,"Opponent failed to progress along the new circuit");
                    Check(float.IsFinite(rival.transform.position.y),"Invalid rival elevation");
                }
                Check(errors==0,"Play mode logged "+errors+" errors");
                Directory.CreateDirectory("Inspection");File.WriteAllText("Inspection/play-mode-report.txt",$"Native Play mode: {checks} checks passed. Menu, time trial, countdown, three moving rivals, train, fans and static-batching isolation verified. No target-hardware FPS claim.\n");
                Debug.Log("NEON_PLAY_OK: "+checks+" checks; menu, time trial, race countdown/opponents and animations passed.");
                Finish(0);
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Finish(int code)
    {
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
        EditorApplication.Exit(code);
    }
}
