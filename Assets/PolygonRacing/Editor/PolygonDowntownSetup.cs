#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolygonRacing.Editor
{
    // Creates (first run) or refreshes the Downtown street circuit scene. The first run copies the
    // Forest Circuit scene, so the player car, cockpit, tracking, dashboard and race systems are
    // exactly the tested ones; only the scenery and the circuit layout change.
    public static class PolygonDowntownSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        public const string DowntownPath="Assets/PolygonRacing/Scenes/DowntownCircuit.unity";
        const string GeneratedPath="Assets/PolygonRacing/Generated/Downtown";

        [MenuItem("Polygon Racing/12 - Create or Refresh Downtown Circuit")]
        public static void CreateOrRefresh()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before building the Downtown circuit.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Downtown circuit","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            bool created=false;
            if(!File.Exists(DowntownPath))
            {
                if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Downtown circuit","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
                if(!AssetDatabase.CopyAsset(ForestPath,DowntownPath)){Debug.LogError("Could not copy the Forest Circuit scene for Downtown.");return;}
                AssetDatabase.Refresh();created=true;
            }
            else
            {
                string backup="PolygonRacingBackups/DowntownCircuit_before_refresh_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(DowntownPath,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Downtown scene. Nothing was rebuilt.");return;}
            }

            var scene=EditorSceneManager.OpenScene(DowntownPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent){Debug.LogError("The Downtown scene has no player car. Nothing was rebuilt.");return;}
            var root=player.transform.parent;
            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=root.gameObject.AddComponent<PolygonCircuitSelector>();
            selector.Layout=PolygonCircuitLayout.Downtown;EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Downtown);

            try
            {
                var oldScenery=root.Find(PolygonDowntownArt.SceneryName);
                if(oldScenery)oldScenery.name="Old scenery (being replaced)";
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var scenery=PolygonDowntownArt.Build(root);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                if(oldScenery)Object.DestroyImmediate(oldScenery.gameObject);

                // Start the car on the new grid and give the camera the city's view distance.
                player.transform.position=PolygonWorld.Point(0);
                player.transform.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(0));
                if(player.view){player.view.farClipPlane=1400;player.view.nearClipPlane=.1f;EditorUtility.SetDirty(player.view);}
                EditorUtility.SetDirty(player.transform);

                PersistGeneratedAssets(scenery);
                PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.Downtown);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,DowntownPath))throw new IOException("Unity could not save the Downtown scene.");
                AddBuildScenes();
                AssetDatabase.SaveAssets();
                Report(scenery,timer.Elapsed.TotalSeconds,created);
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Downtown circuit build failed. Forest and Coastal scenes are unchanged.");
            }
        }

        // Debug helper: steps the Scene view through overview, driver-height and helicopter views
        // of the current circuit, for checking scenery without driving a lap.
        static int viewpoint;
        [MenuItem("Polygon Racing/Debug - Next Circuit Viewpoint %#v")]
        static void NextViewpoint()
        {
            var view=SceneView.lastActiveSceneView;
            if(!view){Debug.LogWarning("Open a Scene view first.");return;}
            var selector=Object.FindFirstObjectByType<PolygonCircuitSelector>();
            if(selector)PolygonCircuit.SetLayout(selector.Layout);
            const int laps=16;
            int count=2+laps*2;int k=viewpoint++%count;
            Vector3 position;Quaternion rotation;string label;
            var bounds=new Bounds(PolygonCircuit.AtDistance(0),Vector3.zero);
            for(int b=1;b<=64;b++)bounds.Encapsulate(PolygonCircuit.AtDistance(PolygonCircuit.Length*b/64));
            float span=Mathf.Max(bounds.size.x,bounds.size.z);
            if(k==0){position=bounds.center+new Vector3(0,span*.7f,-span*.85f);rotation=Quaternion.Euler(40,0,0);label="Overview";}
            else if(k==1){position=bounds.center+new Vector3(span*.8f,span*.35f,span*.8f);rotation=Quaternion.Euler(22,-135,0);label="Opposite overview";}
            else
            {
                int i=(k-2)/2;bool high=(k-2)%2==1;
                float d=i*PolygonCircuit.Length/laps;
                var forward=PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
                if(high)
                {
                    position=PolygonCircuit.AtDistance(d)-forward*60+Vector3.up*(45+PolygonCircuit.Elevation(d));
                    rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(25,0,0);label=$"Helicopter {d:0} m";
                }
                else
                {
                    position=PolygonCircuit.AtDistance(d)+Vector3.up*(1.3f+PolygonCircuit.Elevation(d));
                    rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(3,0,0);label=$"Driver {d:0} m";
                }
            }
            var temp=new GameObject("Viewpoint (temporary)");temp.hideFlags=HideFlags.HideAndDontSave;
            temp.transform.SetPositionAndRotation(position,rotation);
            view.AlignViewToObject(temp.transform);Object.DestroyImmediate(temp);
            view.Repaint();
            Debug.Log($"Viewpoint {k+1}/{count}: {label}");
        }

        static void Report(Transform scenery,double seconds,bool created)
        {
            long triangles=0;int renderers=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;renderers++;
                for(int i=0;i<filter.sharedMesh.subMeshCount;i++)triangles+=filter.sharedMesh.GetIndexCount(i)/3;
            }
            Debug.Log($"Downtown circuit {(created?"created":"refreshed")}: {PolygonCircuit.Length:0} m lap, {renderers} scenery renderers, " +
                      $"{triangles:N0} scenery triangles, built in {seconds:0.0} s. Saved {DowntownPath} and added it to the build scenes.");
        }

        static void PersistGeneratedAssets(Transform scenery)
        {
            // The folder is replaced on every build (older Downtown backups lose their meshes; the
            // live scene is always complete).
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            if(AssetDatabase.IsValidFolder(GeneratedPath))AssetDatabase.DeleteAsset(GeneratedPath);
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Downtown");
            var saved=new HashSet<Object>();int index=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!saved.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,$"{GeneratedPath}/Mesh_{index++}.asset");
            }
            foreach(var renderer in scenery.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||AssetDatabase.Contains(material)||!saved.Add(material))continue;
                    AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPath}/{material.name}.mat"));
                }
        }

        static void AddBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var path in new[]{ForestPath,CoastalPath,DowntownPath})
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
