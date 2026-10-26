#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolygonRacing.Editor
{
    // Creates (first run, copied from the Forest scene) or refreshes the Sakura Hills figure-eight scene.
    public static class PolygonSakuraSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        public const string SakuraPath="Assets/PolygonRacing/Scenes/SakuraHills.unity";
        const string DowntownPath=PolygonDowntownSetup.DowntownPath;
        const string RivieraPath=PolygonRivieraSetup.RivieraPath;
        const string GeneratedPath="Assets/PolygonRacing/Generated/Sakura";

        [MenuItem("Polygon Racing/14 - Create or Refresh Sakura Hills")]
        public static void CreateOrRefresh()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before building the Sakura circuit.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Sakura circuit","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            bool created=false;
            if(!File.Exists(SakuraPath))
            {
                if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Sakura circuit","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
                if(!AssetDatabase.CopyAsset(ForestPath,SakuraPath)){Debug.LogError("Could not copy the Forest Circuit scene for Sakura.");return;}
                AssetDatabase.Refresh();created=true;
            }
            else
            {
                string backup="PolygonRacingBackups/SakuraHills_before_refresh_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(SakuraPath,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Sakura scene. Nothing was rebuilt.");return;}
            }

            var scene=EditorSceneManager.OpenScene(SakuraPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent){Debug.LogError("The Sakura scene has no player car. Nothing was rebuilt.");return;}
            var root=player.transform.parent;
            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=root.gameObject.AddComponent<PolygonCircuitSelector>();
            selector.Layout=PolygonCircuitLayout.Sakura;EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Sakura);

            try
            {
                var oldScenery=root.Find(PolygonSakuraArt.SceneryName);
                if(oldScenery)oldScenery.name="Old scenery (being replaced)";
                var oldAnimated=root.Find(PolygonSakuraArt.AnimatedName);
                if(oldAnimated)Object.DestroyImmediate(oldAnimated.gameObject);
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var scenery=PolygonSakuraArt.Build(root);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                if(oldScenery)Object.DestroyImmediate(oldScenery.gameObject);

                // Start the car on the grid and give the camera the valley's view distance.
                player.transform.position=PolygonWorld.Point(0)+Vector3.up*PolygonCircuit.Elevation(0);
                player.transform.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(0));
                if(player.view){player.view.farClipPlane=1600;player.view.nearClipPlane=.1f;EditorUtility.SetDirty(player.view);}
                EditorUtility.SetDirty(player.transform);

                PersistGeneratedAssets(scenery,root.Find(PolygonSakuraArt.AnimatedName));
                PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.Sakura);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,SakuraPath))throw new IOException("Unity could not save the Sakura scene.");
                AddBuildScenes();
                AssetDatabase.SaveAssets();
                Report(scenery,timer.Elapsed.TotalSeconds,created);
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Sakura circuit build failed. Forest and Coastal scenes are unchanged.");
            }
        }

        // Debug helper: steps the Scene view through close views of the Sakura landmarks.
        static int landmark;
        [MenuItem("Polygon Racing/Debug - Sakura Landmark Views")]
        static void NextLandmark()
        {
            var view=SceneView.lastActiveSceneView;
            if(!view){Debug.LogWarning("Open a Scene view first.");return;}
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Sakura);
            float lower=PolygonCircuit.ProjectLap(new Vector3(180,3,266),out _)*PolygonCircuit.Length;
            var cross=PolygonCircuit.AtDistance(lower);var t=PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(lower));
            var side=Vector3.Cross(Vector3.up,t);
            var views=new (string label,Vector3 eye,Vector3 target)[]{
                ("Under the bridge (lower road)",cross-t*55+Vector3.up*(PolygonCircuit.Elevation(lower-55)+1.3f),cross+Vector3.up*(PolygonCircuit.Elevation(lower)+3)),
                ("Bridge from the field",cross+side*70+t*25+Vector3.up*16,cross+Vector3.up*8),
                ("Bridge from the field, other side",cross-side*70-t*25+Vector3.up*16,cross+Vector3.up*8),
                ("Ferris wheel from the main grandstand",new Vector3(600,26,48),new Vector3(565,38,-52)),
                ("Amusement park",new Vector3(520,55,40),new Vector3(610,0,-110)),
                ("Lake and water gate",new Vector3(-335,20,250),new Vector3(-408,12,346)),
                ("Pagoda hill",new Vector3(-10,45,560),new Vector3(-40,40,660)),
                ("Hairpin gate",new Vector3(-470,12,92),new Vector3(-585,16,120)),
                ("Esses from above",new Vector3(860,70,200),new Vector3(780,10,330))};
            var v=views[landmark++%views.Length];
            var temp=new GameObject("Viewpoint (temporary)");temp.hideFlags=HideFlags.HideAndDontSave;
            temp.transform.SetPositionAndRotation(v.eye,Quaternion.LookRotation(v.target-v.eye));
            view.AlignViewToObject(temp.transform);Object.DestroyImmediate(temp);view.Repaint();
            Debug.Log("Sakura landmark view: "+v.label);
        }

        static void Report(Transform scenery,double seconds,bool created)
        {
            long triangles=0;int renderers=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;renderers++;
                for(int i=0;i<filter.sharedMesh.subMeshCount;i++)triangles+=filter.sharedMesh.GetIndexCount(i)/3;
            }
            Debug.Log($"Sakura circuit {(created?"created":"refreshed")}: {PolygonCircuit.Length:0} m lap, {renderers} scenery renderers, " +
                      $"{triangles:N0} scenery triangles, built in {seconds:0.0} s. Saved {SakuraPath} and added it to the build scenes.");
        }

        static void PersistGeneratedAssets(Transform scenery,Transform animated)
        {
            // The folder is replaced on every build (older Sakura backups lose their meshes; the
            // live scene is always complete).
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            if(AssetDatabase.IsValidFolder(GeneratedPath))AssetDatabase.DeleteAsset(GeneratedPath);
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Sakura");
            var saved=new HashSet<Object>();int index=0;
            var filters=new List<MeshFilter>(scenery.GetComponentsInChildren<MeshFilter>(true));
            var renderers=new List<Renderer>(scenery.GetComponentsInChildren<Renderer>(true));
            if(animated){filters.AddRange(animated.GetComponentsInChildren<MeshFilter>(true));renderers.AddRange(animated.GetComponentsInChildren<Renderer>(true));}
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!saved.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,$"{GeneratedPath}/Mesh_{index++}.asset");
            }
            foreach(var renderer in renderers)
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||AssetDatabase.Contains(material)||!saved.Add(material))continue;
                    AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPath}/{material.name}.mat"));
                }
        }

        static void AddBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var path in new[]{ForestPath,CoastalPath,DowntownPath,RivieraPath,SakuraPath})
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
