#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolygonRacing.Editor
{
    // Creates (first run, copied from the Forest scene) or refreshes the Nordwald Ring forest circuit scene.
    public static class PolygonNordwaldSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        public const string NordwaldPath="Assets/PolygonRacing/Scenes/NordwaldRing.unity";
        const string DowntownPath=PolygonDowntownSetup.DowntownPath;
        const string RivieraPath=PolygonRivieraSetup.RivieraPath;
        const string SakuraPath=PolygonSakuraSetup.SakuraPath;
        const string GeneratedPath="Assets/PolygonRacing/Generated/Nordwald";

        [MenuItem("Polygon Racing/15 - Create or Refresh Nordwald Ring")]
        public static void CreateOrRefresh()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before building the Nordwald circuit.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Nordwald circuit","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            bool created=false;
            if(!File.Exists(NordwaldPath))
            {
                if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Nordwald circuit","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
                if(!AssetDatabase.CopyAsset(ForestPath,NordwaldPath)){Debug.LogError("Could not copy the Forest Circuit scene for Nordwald.");return;}
                AssetDatabase.Refresh();created=true;
            }
            else
            {
                string backup="PolygonRacingBackups/NordwaldRing_before_refresh_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(NordwaldPath,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Nordwald scene. Nothing was rebuilt.");return;}
            }

            var scene=EditorSceneManager.OpenScene(NordwaldPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent){Debug.LogError("The Nordwald scene has no player car. Nothing was rebuilt.");return;}
            var root=player.transform.parent;
            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=root.gameObject.AddComponent<PolygonCircuitSelector>();
            selector.Layout=PolygonCircuitLayout.Nordwald;EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Nordwald);

            try
            {
                var oldScenery=root.Find(PolygonNordwaldArt.SceneryName);
                if(oldScenery)oldScenery.name="Old scenery (being replaced)";
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var scenery=PolygonNordwaldArt.Build(root);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                if(oldScenery)Object.DestroyImmediate(oldScenery.gameObject);

                // Start the car on the grid and give the camera the forest's view distance.
                player.transform.position=PolygonWorld.Point(0)+Vector3.up*PolygonCircuit.Elevation(0);
                player.transform.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(0));
                if(player.view){player.view.farClipPlane=1350;player.view.nearClipPlane=.1f;EditorUtility.SetDirty(player.view);}
                EditorUtility.SetDirty(player.transform);

                PersistGeneratedAssets(scenery);
                if(!PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.Nordwald))
                    throw new System.InvalidOperationException("Nordwald base look could not be reapplied.");
                // PC v01: shadows, cool summer-afternoon sky/haze and post-processing.
                if(!PolygonPCVisualSetup.ApplyToOpenNordwaldScene())
                    throw new System.InvalidOperationException("Nordwald PC visual profile could not be applied.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,NordwaldPath))throw new IOException("Unity could not save the Nordwald scene.");
                AddBuildScenes();
                AssetDatabase.SaveAssets();
                Report(scenery,timer.Elapsed.TotalSeconds,created);
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Nordwald circuit build failed. Other circuit scenes are unchanged.");
            }
        }

        // Debug helper: steps the Scene view through close views of the Nordwald landmarks.
        static int landmark;
        [MenuItem("Polygon Racing/Debug - Nordwald Landmark Views")]
        static void NextLandmark()
        {
            var view=SceneView.lastActiveSceneView;
            if(!view){Debug.LogWarning("Open a Scene view first.");return;}
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Nordwald);
            Vector3 Road(float knot,float lateral,float up)
            {
                float d=PolygonCircuit.DistanceAtKnot(knot);var a=PolygonCircuit.AngleAtDistance(d);
                return PolygonCircuit.Evaluate(a)+Vector3.Cross(Vector3.up,PolygonCircuit.Tangent(a))*lateral+Vector3.up*(PolygonCircuit.Elevation(d)+up);
            }
            var views=new (string label,Vector3 eye,Vector3 target)[]{
                ("Banked hairpin from above",Road(50.5f,-40,30),Road(51,0,0)),
                ("Banked hairpin, driver's eye",Road(48.4f,0,1.3f),Road(50,0,1)),
                ("Bridge over the long straight",Road(75.4f,0,1.3f),Road(76.5f,0,4)),
                ("Valley bridge and village",Road(32.2f,0,1.3f),Road(33.3f,0,4)),
                ("Castle on the hill",new Vector3(110,140,380),new Vector3(110,90,560)),
                ("Village from the hillside",new Vector3(330,60,560),new Vector3(330,10,690)),
                ("Camping meadow",new Vector3(-640,60,700),new Vector3(-735,30,800)),
                ("Crest",Road(10.6f,0,1.3f),Road(12.4f,0,1.3f)),
                ("Start from above",Road(78,40,40),Road(2,0,0))};
            var v=views[landmark++%views.Length];
            var temp=new GameObject("Viewpoint (temporary)");temp.hideFlags=HideFlags.HideAndDontSave;
            temp.transform.SetPositionAndRotation(v.eye,Quaternion.LookRotation(v.target-v.eye));
            view.AlignViewToObject(temp.transform);Object.DestroyImmediate(temp);view.Repaint();
            Debug.Log("Nordwald landmark view: "+v.label);
        }

        static void Report(Transform scenery,double seconds,bool created)
        {
            long triangles=0;int renderers=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;renderers++;
                for(int i=0;i<filter.sharedMesh.subMeshCount;i++)triangles+=filter.sharedMesh.GetIndexCount(i)/3;
            }
            Debug.Log($"Nordwald circuit {(created?"created":"refreshed")}: {PolygonCircuit.Length:0} m lap, {renderers} scenery renderers, " +
                      $"{triangles:N0} scenery triangles, built in {seconds:0.0} s. Saved {NordwaldPath} and added it to the build scenes.");
        }

        // Materials keep their asset (and GUID) between builds so the PC clones, keyed by source GUID,
        // are reused; meshes are deleted and recreated (copying into old mesh assets left stale data).
        static void PersistGeneratedAssets(Transform scenery)
        {
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            if(!AssetDatabase.IsValidFolder(GeneratedPath))AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Nordwald");
            var oldMeshes=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{GeneratedPath}))oldMeshes.Add(AssetDatabase.GUIDToAssetPath(guid));
            if(oldMeshes.Count>0)
            {
                var failed=new List<string>();
                AssetDatabase.DeleteAssets(oldMeshes.ToArray(),failed);
                if(failed.Count>0)Debug.LogWarning($"{failed.Count} old Nordwald meshes could not be deleted.");
            }
            var savedMeshes=new HashSet<Mesh>();int index=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!savedMeshes.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPath}/Mesh_{index++}.asset"));
            }

            var materials=new Dictionary<Material,Material>();
            foreach(var renderer in scenery.GetComponentsInChildren<Renderer>(true))
            {
                var shared=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<shared.Length;i++)
                {
                    var material=shared[i];
                    if(!material)continue;
                    if(materials.TryGetValue(material,out var savedMaterial))
                    {
                        if(savedMaterial!=material){shared[i]=savedMaterial;changed=true;}
                        continue;
                    }
                    if(AssetDatabase.Contains(material)){materials.Add(material,material);continue;}
                    string path=$"{GeneratedPath}/{SafeAssetName(material.name)}.mat";
                    var asset=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(asset)
                    {
                        EditorUtility.CopySerialized(material,asset);asset.name=material.name;EditorUtility.SetDirty(asset);
                        shared[i]=asset;materials.Add(material,asset);changed=true;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(path));
                        materials.Add(material,material);
                    }
                }
                if(changed)renderer.sharedMaterials=shared;
            }
        }

        static string SafeAssetName(string name)
        {
            foreach(char c in System.IO.Path.GetInvalidFileNameChars())name=name.Replace(c,'_');
            return name;
        }

        static void AddBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var path in new[]{ForestPath,CoastalPath,DowntownPath,RivieraPath,SakuraPath,NordwaldPath})
            {
                if(!File.Exists(path))continue;
                bool found=false;
                foreach(var s in scenes)if(s.path==path){s.enabled=true;found=true;}
                if(!found)scenes.Add(new EditorBuildSettingsScene(path,true));
            }
            EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
#endif
