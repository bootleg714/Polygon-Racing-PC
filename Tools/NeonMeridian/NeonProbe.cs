// Native Unity verification runner. Copied into the isolated probe's Assets/Editor folder.
using System;
using System.Collections.Generic;
using System.IO;
using PolygonRacing;
using PolygonRacing.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class NeonProbe
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            PolygonNeonSetup.Build();
            Verify();
            Capture();
            Debug.Log("NEON_PROBE_OK: "+checks+" native checks passed");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void InspectExisting()
    {
        try
        {
            EditorSceneManager.OpenScene(PolygonNeonSetup.ScenePath);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.NeonMeridian);
            Verify();Capture();Debug.Log("NEON_PROBE_OK: "+checks+" native checks passed");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Verify()
    {
        var player=Object.FindFirstObjectByType<PolygonDriver>();Check(player&&player.view,"Player/camera missing");
        Check(Mathf.Abs(player.topSpeed-67.056f)<.001f,"Tuned top speed changed");
        Check(Mathf.Abs(player.cornerGrip-60)<.001f,"Tuned grip changed");
        Check(PolygonCircuit.LayoutId=="NeonMeridianV01","Neon timing ID missing");
        Check((int)PolygonCircuitLayout.Nordwald==5&&(int)PolygonCircuitLayout.NeonMeridian==6,"Existing serialized layout indices changed");
        float length=PolygonCircuit.Length,min=float.MaxValue,max=float.MinValue,grade=0;
        for(float d=0;d<length;d+=2)
        {
            float y=PolygonCircuit.Elevation(d);min=Mathf.Min(min,y);max=Mathf.Max(max,y);
            float slope=Mathf.Abs(PolygonCircuit.Elevation(d+1)-PolygonCircuit.Elevation(d-1))*.5f;grade=Mathf.Max(grade,slope);
            var p=PolygonNeonArt.RoadPoint(d);Check(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),"Nonfinite road");
            Check(slope<.20f,"Excessive grade at "+d+": "+slope);
            if(((int)d)%16==0)foreach(float side in new[]{-7f,0,7f})
            {
                var sample=PolygonNeonArt.RoadPoint(d,side);float projected=PolygonCircuit.ProjectLap(sample,out float offset)*length;
                float error=Mathf.Abs(Mathf.DeltaAngle(d/length*360,projected/length*360))/360*length;
                Check(error<1,"Ambiguous road projection at "+d);Check(offset<7.1f,"Bad projection offset");
            }
        }
        Check(Mathf.Abs(PolygonCircuit.Elevation(.01f)-PolygonCircuit.Elevation(length-.01f))<.01f,"Elevation seam");
        var scenery=player.transform.parent.Find(PolygonNeonArt.SceneryName);Check(scenery,"Scenery missing");
        long triangles=0;int meshes=0;var colliders=new List<MeshCollider>();
        foreach(var f in scenery.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=f.sharedMesh;Check(mesh,"Missing mesh: "+f.name);meshes++;
            Check(AssetDatabase.Contains(mesh),"Transient mesh: "+f.name);
            var vertices=mesh.vertices;var indices=mesh.triangles;triangles+=indices.Length/3;
            foreach(var v in vertices)Check(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z),"Nonfinite mesh vertex");
            for(int i=0;i<indices.Length;i+=3)
                Check(Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).sqrMagnitude>1e-15f,"Degenerate triangle in "+f.name);
            if(mesh.vertexCount>3&&f.GetComponent<MeshRenderer>()&&mesh.bounds.size.y>.2f&&f.name!="Neon harbour - NM Night harbour")
            {var collider=f.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;colliders.Add(collider);}
        }
        foreach(var renderer in player.transform.parent.GetComponentsInChildren<Renderer>(true))
            foreach(var m in renderer.sharedMaterials)
            {
                Check(m&&m.shader,"Missing material/shader on "+renderer.name);
                Check(AssetDatabase.Contains(m),"Unpersisted material on "+renderer.name);
                Check(!ShaderUtil.ShaderHasError(m.shader),"Shader error in "+m.shader.name);
            }
        foreach(var light in scenery.GetComponentsInChildren<PolygonStartLights>(true))
            Check(AssetDatabase.Contains(light.RedMaterial)&&AssetDatabase.Contains(light.DarkMaterial),"Start signal materials not saved");
        Physics.SyncTransforms();int blocked=0;
        for(float d=0;d<length;d+=6)foreach(float lateral in new[]{-8f,0,8f})
        {
            var p=PolygonNeonArt.RoadPoint(d,lateral,.22f);
            if(Physics.Raycast(p,Vector3.up,out var hit,4.2f))
            {blocked++;Debug.LogError($"BLOCKED_ROAD d={d:0.0} lateral={lateral} hit={hit.collider.name} height={hit.distance:0.00}");}
        }
        foreach(var collider in colliders)Object.DestroyImmediate(collider);
        Check(blocked==0,"Road corridor has "+blocked+" obstructions");
        Check(triangles<1200000,"Scenery triangle budget exceeded");
        Check(meshes<1600,"Scenery renderer budget exceeded");
        int scriptMissing=0;
        foreach(var t in player.transform.root.GetComponentsInChildren<Transform>(true))scriptMissing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        Check(scriptMissing==0,"Missing scripts in scene");
        Debug.Log($"NEON_GEOMETRY: {length:0.0}m, altitude {min:0.0}-{max:0.0}m, maximum grade {grade*100:0.0}%, {meshes} meshes, {triangles} triangles");
        Directory.CreateDirectory("Inspection");
        File.WriteAllText("Inspection/report.txt",$"Native Unity geometry verification\nLength: {length:0.0} m\nAltitude: {min:0.0}-{max:0.0} m\nMaximum grade: {grade*100:0.0}%\nScenery meshes: {meshes}\nScenery triangles: {triangles}\nRoad corridor obstructions: {blocked}\nChecks: {checks}\n");
    }
    static void Capture()
    {
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        QualitySettings.renderPipeline=pipeline;
        var go=new GameObject("Inspection camera");var camera=go.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=2200;camera.nearClipPlane=.1f;camera.fieldOfView=72;
        camera.allowHDR=true;camera.allowMSAA=true;
        var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
        data.volumeLayerMask=-1;data.volumeTrigger=camera.transform;
        var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.antiAliasing=4;target.Create();
        camera.targetTexture=target;
        float[] knots={.45f,6.5f,11.6f,16.2f,28.8f,31.2f};
        string[] names={"01_Prism_Boulevard","02_Lantern_Rise","03_The_Crown","04_Cascade_Descent","05_Undercity","06_Skyline_Return"};
        for(int i=0;i<knots.Length;i++)
        {
            float d=PolygonCircuit.DistanceAtKnot(knots[i]);
            var eye=PolygonNeonArt.RoadPoint(d,0,2.3f);var ahead=PolygonNeonArt.RoadPoint(d+110,0,4.3f);
            camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(ahead-eye));
            // StandardRequest runs URP's per-frame constants, sky and volume update too.
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes("Inspection/"+names[i]+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            Debug.Log("NEON_CAPTURE: "+names[i]);
        }
        RenderTexture.active=null;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);
    }
}
