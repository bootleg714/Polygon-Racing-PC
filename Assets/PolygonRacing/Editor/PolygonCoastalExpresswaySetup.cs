#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolygonRacing.Editor
{
    public static class PolygonCoastalExpresswaySetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        const string GeneratedPath="Assets/PolygonRacing/Generated/CoastalExpresswayArtV02";

        [MenuItem("Polygon Racing/6 - Create Coastal Expressway Layout")]
        static void CreateCoastalExpressway()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before creating the circuit.");return;}
            if(!File.Exists(ForestPath))
            {
                EditorUtility.DisplayDialog("Coastal Expressway","Create the Forest Circuit first. Nothing was changed.","OK");
                return;
            }
            if(File.Exists(CoastalPath))
            {
                EditorUtility.DisplayDialog("Coastal Expressway",
                    "CoastalExpressway.unity already exists. Use Polygon Racing > 7 - Refresh Coastal Expressway Scenery to update its scenery without replacing the circuit.",
                    "OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(EditorSceneManager.GetActiveScene().path!=ForestPath)
                EditorSceneManager.OpenScene(ForestPath,OpenSceneMode.Single);

            var forestPlayer=Object.FindFirstObjectByType<PolygonDriver>();
            if(!forestPlayer||!forestPlayer.transform.parent)
            {
                EditorUtility.DisplayDialog("Coastal Expressway","The saved Forest Circuit player could not be found. Nothing was changed.","OK");
                return;
            }

            var forestRoot=forestPlayer.transform.parent;
            var forestSelector=forestRoot.GetComponent<PolygonCircuitSelector>();
            if(!forestSelector)forestSelector=Undo.AddComponent<PolygonCircuitSelector>(forestRoot.gameObject);
            forestSelector.Layout=PolygonCircuitLayout.Forest;
            EditorUtility.SetDirty(forestSelector);
            if(!EditorSceneManager.SaveScene(forestPlayer.gameObject.scene))
            {
                Debug.LogError("Could not save Forest Circuit before creating Coastal Expressway.");
                return;
            }

            if(!AssetDatabase.CopyAsset(ForestPath,CoastalPath))
            {
                Debug.LogError("Could not copy Forest Circuit to a new Coastal Expressway scene.");
                return;
            }
            AssetDatabase.Refresh();
            var coastalScene=EditorSceneManager.OpenScene(CoastalPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent)
            {
                Debug.LogError("The copied scene has no player car. Forest Circuit is unchanged.");
                return;
            }

            var selector=player.transform.parent.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=Undo.AddComponent<PolygonCircuitSelector>(player.transform.parent.gameObject);
            selector.Layout=PolygonCircuitLayout.CoastalExpressway;
            EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.CoastalExpressway);

            var oldScenery=player.transform.parent.Find("Circuit - editable scenery");
            if(!oldScenery)
            {
                PolygonCircuit.SetLayout(PolygonCircuitLayout.Forest);
                Debug.LogError("The copied Forest Circuit scenery could not be found. Forest Circuit is unchanged.");
                return;
            }

            try
            {
                Directory.CreateDirectory(GeneratedPath);
                AssetDatabase.Refresh();
                var scenery=PolygonWorld.BuildCoastalScenery(player.transform.parent);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                PersistGeneratedAssets(scenery);
                ConfigureCoastalView(player);
                PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.CoastalExpressway);
                Object.DestroyImmediate(oldScenery.gameObject);
                if(!PolygonPCVisualSetup.ApplyToOpenCoastalScene())
                    throw new System.InvalidOperationException("Coastal PC visual profile could not be applied.");
                EditorSceneManager.MarkSceneDirty(coastalScene);
                if(!EditorSceneManager.SaveScene(coastalScene,CoastalPath))
                    throw new IOException("Unity could not save the Coastal Expressway scene.");
                AddBuildScenes();
                // Retain prior generated assets so the scene backup remains restorable.
                AssetDatabase.SaveAssets();
                Selection.activeGameObject=player.gameObject;
                if(SceneView.lastActiveSceneView)
                    SceneView.lastActiveSceneView.LookAt(player.transform.position+Vector3.up,Quaternion.Euler(25,145,0),24);
                Debug.Log($"Coastal Expressway created at {PolygonCircuit.Length:0} metres. Forest Circuit is preserved. The player cockpit and handling are unchanged. Both scenes are enabled in the shared build scene list.");
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Coastal Expressway generation failed. The original ForestCircuit scene remains intact.");
            }
        }

        [MenuItem("Polygon Racing/7 - Refresh Coastal Expressway Scenery")]
        static void RefreshCoastalExpresswayScenery()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before refreshing the circuit scenery.");return;}
            if(!File.Exists(CoastalPath))
            {
                EditorUtility.DisplayDialog("Coastal Expressway",
                    "Create the Coastal Expressway first with Polygon Racing > 6 - Create Coastal Expressway Layout.",
                    "OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;

            var scene=EditorSceneManager.OpenScene(CoastalPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent)
            {
                Debug.LogError("The Coastal Expressway player car could not be found. No scenery was changed.");
                return;
            }
            string backup="PolygonRacingBackups/CoastalExpressway_before_art_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
            try
            {
                Directory.CreateDirectory("PolygonRacingBackups");
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the current scene.");
                File.Copy(CoastalPath,backup,false);
            }
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not create scenery backup. Nothing was rebuilt.");return;}
            var root=player.transform.parent;
            var oldScenery=root.Find("Circuit - editable scenery");
            if(!oldScenery)
            {
                Debug.LogError("The existing Coastal Expressway scenery could not be found. No scenery was changed.");
                return;
            }

            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=Undo.AddComponent<PolygonCircuitSelector>(root.gameObject);
            selector.Layout=PolygonCircuitLayout.CoastalExpressway;
            EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.CoastalExpressway);

            try
            {
                Directory.CreateDirectory(GeneratedPath);
                AssetDatabase.Refresh();
                var scenery=PolygonWorld.BuildCoastalScenery(root);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                PersistGeneratedAssets(scenery);
                ConfigureCoastalView(player);
                PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.CoastalExpressway);
                Object.DestroyImmediate(oldScenery.gameObject);
                // PC v01: shadows, sky, haze and post-processing (after the old scenery is gone,
                // so only the new scenery's materials are converted).
                if(!PolygonPCVisualSetup.ApplyToOpenCoastalScene())
                    throw new System.InvalidOperationException("Coastal PC visual profile could not be applied.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,CoastalPath))
                    throw new IOException("Unity could not save the refreshed Coastal Expressway scene.");
                AddBuildScenes();
                // Retain prior generated assets so the scene backup remains restorable.
                AssetDatabase.SaveAssets();
                Debug.Log($"Coastal Expressway scenery refreshed. The tested route, player car, and handling are unchanged. Both circuits are enabled in the shared build scene list.");
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Coastal Expressway scenery refresh failed. The saved scene file was not intentionally replaced.");
            }
        }

        static void ConfigureCoastalView(PolygonDriver player)
        {
            if(player.view)
            {
                player.view.farClipPlane=1400;
                player.view.backgroundColor=new Color(.43f,.75f,.87f);
                EditorUtility.SetDirty(player.view);
            }
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.78f,.80f,.78f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.52f,.78f,.84f);
            RenderSettings.fogStartDistance=500;RenderSettings.fogEndDistance=1300;
        }

        static void PersistGeneratedAssets(Transform scenery)
        {
            var saved=new HashSet<Object>();
            int index=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!saved.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,
                    AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPath}/Mesh_{index++}.asset"));
            }
            // PC v01: materials are refreshed in place by name (stable GUIDs, so the PC material
            // clones are reused); meshes keep the original unique-path behaviour so older scene
            // backups stay restorable.
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
                    string safe=material.name;foreach(char c in Path.GetInvalidFileNameChars())safe=safe.Replace(c,'_');
                    string path=$"{GeneratedPath}/{safe}.mat";
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

        static void AddBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddBuildScene(scenes,ForestPath);
            AddBuildScene(scenes,CoastalPath);
            EditorBuildSettings.scenes=scenes.ToArray();
        }

        static void PruneUnusedGeneratedAssets(string scenePath)
        {
            var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(scenePath,true));
            foreach(var guid in AssetDatabase.FindAssets("",new[]{GeneratedPath}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(!dependencies.Contains(path)&&!AssetDatabase.DeleteAsset(path))
                    Debug.LogWarning($"Could not remove unused Coastal Expressway asset: {path}");
            }
        }

        static void AddBuildScene(List<EditorBuildSettingsScene> scenes,string path)
        {
            foreach(var scene in scenes)
                if(scene.path==path){scene.enabled=true;return;}
            scenes.Add(new EditorBuildSettingsScene(path,true));
        }
    }
}
#endif
