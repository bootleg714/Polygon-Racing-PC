// Copy into the isolated probe project's Assets/Editor directory. Never runs in the main game.
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

public static class IonRiftProbe
{
    const string Output="IonRiftInspection";
    static int checks;
    sealed class CollisionSource
    {public Vector3[] Vertices;public int[] Indices;public Transform Transform;}
    static void Check(bool condition,string message)
    {checks++;if(!condition)throw new Exception(message);}

    public static void Run()
    {
        try
        {
            PolygonIonRiftSetup.Build();Verify();Capture();
            Debug.Log("ION_PROBE_OK: "+checks+" native checks passed");
        }
        catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
    }
    public static void InspectExisting()
    {
        try
        {
            EditorSceneManager.OpenScene(PolygonIonRiftSetup.ScenePath);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.IonRift);
            Verify();Capture();Debug.Log("ION_PROBE_OK: "+checks+" native checks passed");
        }
        catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
    }

    static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    static void Verify()
    {
        checks=0;
        var player=Object.FindFirstObjectByType<PolygonDriver>();
        Check(player&&player.view,"Player/cockpit camera missing");
        Check(Mathf.Abs(player.topSpeed-67.056f)<.001f,"Accepted top speed changed");
        Check(Mathf.Abs(player.cornerGrip-60)<.001f&&Mathf.Abs(player.maxSlipAngle-5)<.001f,"Accepted grip/slip tuning changed");
        Check(PolygonCircuit.LayoutId=="IonRiftV02"&&PolygonCircuit.LayoutName=="Ion Rift","New circuit timing/name mismatch");
        Check((int)PolygonCircuitLayout.Forest==0&&(int)PolygonCircuitLayout.Nordwald==5&&
              (int)PolygonCircuitLayout.NeonMeridian==6&&(int)PolygonCircuitLayout.IonRift==7,"Existing serialized layout indices changed");
        Check(Mathf.Abs(PolygonCircuit.RoadHalfWidth-10)<.001f,"Expected twenty-metre road width");
        float length=PolygonCircuit.Length,min=float.MaxValue,max=float.MinValue,grade=0,maxBank=0;
        for(float d=0;d<length;d+=2)
        {
            float height=PolygonCircuit.Elevation(d);min=Mathf.Min(min,height);max=Mathf.Max(max,height);
            float slope=Mathf.Abs(PolygonCircuit.Elevation(d+1)-PolygonCircuit.Elevation(d-1))*.5f;grade=Mathf.Max(grade,slope);
            maxBank=Mathf.Max(maxBank,Mathf.Abs(PolygonCircuit.BankDegrees(d)));
            Check(Finite(PolygonIonRiftArt.RoadPoint(d)),"Nonfinite road position at "+d);
            Check(slope<.20f,"Excessive sampled road grade at "+d+": "+slope);
            Check(PolygonBanking.Normal(d,0).y>.8f,"Invalid road normal at "+d);
            if(((int)d)%16!=0)continue;
            foreach(float side in new[]{-7f,0,7f})
            {
                var p=PolygonIonRiftArt.RoadPoint(d,side);
                float projected=PolygonCircuit.ProjectLap(p,out float offset)*length;
                float error=Mathf.Abs(Mathf.DeltaAngle(d/length*360,projected/length*360))/360*length;
                Check(error<1,"Ambiguous road projection at "+d+", lateral "+side);
                Check(offset<7.1f,"Unexpected projection offset at "+d);
            }
        }
        Check(Mathf.Abs(PolygonCircuit.Elevation(.01f)-PolygonCircuit.Elevation(length-.01f))<.01f,"Elevation seam is discontinuous");
        Check(maxBank<=6.1f,"Banking exceeds planned mild banking");
        var scenery=player.transform.parent.Find(PolygonIonRiftArt.SceneryName);Check(scenery,"Ion scenery root missing");
        long triangles=0;int meshes=0;var colliders=new List<MeshCollider>();
        var collisionSources=new Dictionary<Collider,CollisionSource>();
        bool oldBackfaces=Physics.queriesHitBackfaces;
        try
        {
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;Check(mesh,"Missing mesh: "+filter.name);meshes++;
                Check(AssetDatabase.Contains(mesh),"Transient mesh: "+filter.name);
                var vertices=mesh.vertices;var indices=mesh.triangles;triangles+=indices.Length/3;
                Check(indices.Length%3==0,"Incomplete triangle list in "+filter.name);
                foreach(var vertex in vertices)Check(Finite(vertex),"Nonfinite mesh vertex: "+filter.name);
                for(int i=0;i<indices.Length;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    Check(a>=0&&a<vertices.Length&&b>=0&&b<vertices.Length&&c>=0&&c<vertices.Length,"Invalid triangle index: "+filter.name);
                    Check(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude>1e-15f,"Degenerate triangle: "+filter.name);
                }
                var renderer=filter.GetComponent<MeshRenderer>();
                // Very low sea/floor backdrops cannot affect the road corridor; omit their large
                // triangles from temporary physics meshes. Flat road tops are deliberately kept.
                if(renderer&&renderer.bounds.max.y>=min-.1f&&mesh.vertexCount>3&&!filter.GetComponent<Collider>())
                {
                    var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;colliders.Add(collider);
                    collisionSources.Add(collider,new CollisionSource{Vertices=vertices,Indices=indices,Transform=filter.transform});
                }
            }
            foreach(var renderer in player.transform.parent.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)
                {
                    Check(material&&material.shader,"Missing material/shader on "+renderer.name);
                    Check(AssetDatabase.Contains(material),"Transient material on "+renderer.name);
                    Check(!ShaderUtil.ShaderHasError(material.shader),"Shader error: "+material.shader.name);
                }
            foreach(var signals in scenery.GetComponentsInChildren<PolygonStartLights>(true))
                Check(AssetDatabase.Contains(signals.RedMaterial)&&AssetDatabase.Contains(signals.DarkMaterial),"Start-signal materials not persistent");
            Physics.queriesHitBackfaces=true;Physics.SyncTransforms();int blocked=0,unsupported=0,buried=0;
            for(float d=0;d<length;d+=6)foreach(float lateral in new[]{-8f,0,8f})
            {
                var p=PolygonIonRiftArt.RoadPoint(d,lateral,.22f);
                if(Physics.Raycast(p,Vector3.up,out var hit,4.2f))
                {blocked++;Debug.LogError($"ION_BLOCKED_ROAD d={d:0.0} lateral={lateral} hit={hit.collider.name} height={hit.distance:0.00}");}
                if(!Physics.Raycast(p,Vector3.down,out _,.6f))
                {unsupported++;Debug.LogError($"ION_MISSING_ROAD d={d:0.0} lateral={lateral}");}
                // Starting a ray inside a thick rock can otherwise miss an obstruction whose
                // top lies far above the car. For every collider above the lane, the first hit
                // must be an underside/front face (a legal bridge/tunnel overhead), not an
                // outward upward face indicating that this road point is buried in a solid.
                foreach(var above in Physics.RaycastAll(p,Vector3.up,1000))
                {
                    if(!collisionSources.TryGetValue(above.collider,out var source)||above.triangleIndex<0)continue;
                    int first=above.triangleIndex*3;if(first+2>=source.Indices.Length)continue;
                    var a=source.Transform.TransformPoint(source.Vertices[source.Indices[first]]);
                    var b=source.Transform.TransformPoint(source.Vertices[source.Indices[first+1]]);
                    var c=source.Transform.TransformPoint(source.Vertices[source.Indices[first+2]]);
                    if(Vector3.Cross(b-a,c-a).normalized.y<=.05f)continue;
                    buried++;Debug.LogError($"ION_BURIED_ROAD d={d:0.0} lateral={lateral} mesh={above.collider.name} exitHeight={above.distance:0.00}");break;
                }
            }
            Check(blocked==0,"Sampled road corridor has "+blocked+" obstructions");
            Check(unsupported==0,"Sampled road surface has "+unsupported+" holes");
            Check(buried==0,"Sampled road points buried in geometry: "+buried);
            Check(triangles<1200000,"Scenery triangle budget exceeded");Check(meshes<1800,"Scenery renderer budget exceeded");
            int missingScripts=0;
            foreach(var t in player.transform.root.GetComponentsInChildren<Transform>(true))
                missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            Check(missingScripts==0,"Missing script components in scene");
            Debug.Log($"ION_GEOMETRY: {length:0.0}m, altitude {min:0.0}-{max:0.0}m, maximum grade {grade*100:0.0}%, max bank {maxBank:0.0} degrees, {meshes} meshes, {triangles} triangles");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"/report.txt",$"Native Unity geometry verification\nLength: {length:0.0} m\nAltitude: {min:0.0}-{max:0.0} m\nMaximum grade: {grade*100:0.0}%\nMaximum bank: {maxBank:0.0} degrees\nScenery meshes: {meshes}\nScenery triangles: {triangles}\nRoad corridor obstructions: {blocked}\nRoad surface holes: {unsupported}\nBuried road points: {buried}\nChecks: {checks}\nCorridor sampling: every 6 m, at lateral -8/0/+8 m, 4.2 m upward clearance above a 0.22 m start height, downward surface queries and backface-aware solid-exit checks.\n");
        }
        finally {Physics.queriesHitBackfaces=oldBackfaces;foreach(var collider in colliders)if(collider)Object.DestroyImmediate(collider);}
    }

    static void Capture()
    {
        QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var go=new GameObject("Ion Rift inspection camera");var camera=go.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=3000;camera.nearClipPlane=.1f;camera.fieldOfView=72;
        camera.allowHDR=true;camera.allowMSAA=true;camera.useOcclusionCulling=false;
        var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
        data.volumeLayerMask=-1;data.volumeTrigger=camera.transform;
        var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};target.Create();
        camera.targetTexture=target;Directory.CreateDirectory(Output);
        try
        {
            float[] knots={.45f,7.2f,12.4f,16.2f,23.1f,29.6f};
            string[] names={"01_Rim_Station","02_Cliff_Ascent","03_High_Overlook","04_Canyon_Plunge","05_Reactor_Gorge","06_Return_Bridge"};
            for(int i=0;i<knots.Length;i++)
            {
                float d=PolygonCircuit.DistanceAtKnot(knots[i]);
                var eye=PolygonIonRiftArt.RoadPoint(d,0,2.3f);var targetPoint=PolygonIonRiftArt.RoadPoint(d+115,0,4.3f);
                camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(targetPoint-eye));CaptureView(camera,target,names[i]);
            }
            var bounds=new Bounds(PolygonCircuit.AtDistance(0),Vector3.zero);
            for(float d=0;d<PolygonCircuit.Length;d+=25)bounds.Encapsulate(PolygonIonRiftArt.RoadPoint(d));
            float span=Mathf.Max(bounds.size.x,bounds.size.z);
            var overviewTarget=bounds.center+Vector3.up*10;
            var overviewEye=overviewTarget+new Vector3(-.64f,.82f,-.80f)*span;
            camera.fieldOfView=58;camera.transform.SetPositionAndRotation(overviewEye,Quaternion.LookRotation(overviewTarget-overviewEye));
            CaptureView(camera,target,"00_Canyon_Overview");
        }
        finally
        {
            RenderTexture.active=null;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);
        }
    }
    static void CaptureView(Camera camera,RenderTexture target,string name)
    {
        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);Debug.Log("ION_CAPTURE: "+name);
    }
}
