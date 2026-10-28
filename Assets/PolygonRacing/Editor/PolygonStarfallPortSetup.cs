#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace PolygonRacing.Editor
{
    public static class PolygonStarfallPortSetup
    {
        const string Template="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        public const string ScenePath="Assets/PolygonRacing/Scenes/StarfallPort.unity";
        public const string GeneratedPath="Assets/PolygonRacing/Generated/StarfallPort";

        [MenuItem("Polygon Racing/19 - Create or Refresh Starfall Port")]
        public static void CreateOrRefresh()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before building Starfall Port.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try {Build();}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("Starfall Port build failed. See the Console and the before-refresh backup.");}
        }

        // Callable from an isolated batch-mode Unity project as well as the editor menu.
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building Starfall Port.");
            foreach(string shader in new[]{"PolygonFlatPC","PolygonSky","PolygonNeonPanel","PolygonIonEnergy"})
                if(!Resources.Load<Shader>(shader))throw new InvalidOperationException(shader+".shader is missing or still importing.");
            var timer=System.Diagnostics.Stopwatch.StartNew();
            bool created=!File.Exists(ScenePath);
            if(created)
            {
                if(!File.Exists(Template))throw new FileNotFoundException("The Forest template scene was not found.",Template);
                if(!AssetDatabase.CopyAsset(Template,ScenePath))throw new IOException("Could not create the Starfall Port scene.");
                AssetDatabase.Refresh();
            }
            else
            {
                Directory.CreateDirectory("PolygonRacingBackups");
                File.Copy(ScenePath,"PolygonRacingBackups/StarfallPort_before_refresh_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity",false);
            }
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent)throw new InvalidOperationException("The copied scene has no player/root.");
            var root=player.transform.parent;
            PolygonCircuit.SetLayout(PolygonCircuitLayout.StarfallPort);
            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=root.gameObject.AddComponent<PolygonCircuitSelector>();
            selector.Layout=PolygonCircuitLayout.StarfallPort;EditorUtility.SetDirty(selector);
            var oldScenery=root.Find(PolygonStarfallPortArt.SceneryName);
            if(oldScenery)oldScenery.name="Starfall Port scenery being replaced";
            foreach(string name in new[]{PolygonStarfallPortArt.AnimatedName,"Sky - camera anchored clouds"})
            {var child=root.Find(name);if(child)Object.DestroyImmediate(child.gameObject);}
            // A template can acquire a profile later; keep only this circuit's global profile.
            foreach(var volume in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(volume.gameObject.scene==scene&&volume.gameObject.name!="PC Visual Profile - Starfall Port")Object.DestroyImmediate(volume.gameObject);
            var scenery=PolygonStarfallPortArt.Build(root);
            if(oldScenery)Object.DestroyImmediate(oldScenery.gameObject);
            player.transform.position=PolygonStarfallPortArt.RoadPoint(0);
            player.transform.rotation=Quaternion.LookRotation(PolygonCircuit.Tangent(0));
            PolygonBanking.AlignPlayer(player.transform);
            if(player.view)
            {
                player.view.farClipPlane=2600;player.view.nearClipPlane=.1f;
                EditorUtility.SetDirty(player.view);
            }
            EditorUtility.SetDirty(player.transform);
            EnsureFolder(GeneratedPath);EnsureFolder(GeneratedPath+"/Meshes");EnsureFolder(GeneratedPath+"/Materials");
            PersistMeshes(scenery);
            // Clone all shared template/car materials before applying the night profile. This
            // prevents a night ambient update from altering car materials used by other circuits.
            PersistMaterials(root);
            if(!PolygonPCVisualSetup.ApplyToOpenStarfallPortScene())throw new InvalidOperationException("Starfall Port night profile failed.");
            var sky=RenderSettings.skybox;
            if(sky){sky.SetFloat("_SunGlow",0);sky.SetFloat("_SunHalo",0);sky.SetFloat("_SunDisc",0);EditorUtility.SetDirty(sky);}
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save Starfall Port.");
            var builds=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);bool found=false;
            foreach(var s in builds)if(s.path==ScenePath){s.enabled=true;found=true;}
            if(!found)builds.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
            long triangles=0;int count=0;
            foreach(var f in scenery.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f.sharedMesh)continue;count++;
                for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)triangles+=f.sharedMesh.GetIndexCount(sub)/3;
            }
            Debug.Log($"STARFALL_BUILD_OK: {PolygonCircuit.Length:0} m, {PolygonStarfallPortArt.RockCount} rock formations, {PolygonStarfallPortArt.StructureCount} structures, {count} scenery renderers, {triangles:N0} triangles, {timer.Elapsed.TotalSeconds:0.0} s. Saved {ScenePath}.");
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static string Safe(string name)
        {foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');return name;}
        static void PersistMeshes(Transform scenery)
        {
            var paths=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{GeneratedPath+"/Meshes"}))paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            if(paths.Count>0)
            {
                var failed=new List<string>();AssetDatabase.DeleteAssets(paths.ToArray(),failed);
                if(failed.Count>0)throw new IOException("Could not replace "+failed.Count+" previous Starfall Port meshes.");
            }
            int index=0;var saved=new HashSet<Mesh>();
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!saved.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,$"{GeneratedPath}/Meshes/{index++:0000} {Safe(mesh.name)}.asset");
            }
        }
        static void PersistMaterials(Transform root)
        {
            var saved=new Dictionary<Material,Material>();
            Material Persist(Material source)
            {
                if(!source)return null;if(saved.TryGetValue(source,out var known))return known;
                string original=AssetDatabase.GetAssetPath(source);
                if(original.StartsWith(GeneratedPath+"/",StringComparison.Ordinal)||original.StartsWith("Assets/PolygonRacing/Generated/PCVisuals/StarfallPort/",StringComparison.Ordinal))
                {saved.Add(source,source);return source;}
                string guid=AssetDatabase.AssetPathToGUID(original);
                string path=GeneratedPath+"/Materials/"+(string.IsNullOrEmpty(guid)?"":guid+" ")+Safe(source.name)+".mat";
                var asset=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!asset)
                {
                    asset=new Material(source){name=source.name};AssetDatabase.CreateAsset(asset,path);
                }
                else if(string.IsNullOrEmpty(original))
                {EditorUtility.CopySerialized(source,asset);EditorUtility.SetDirty(asset);}
                saved.Add(source,asset);return asset;
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=Persist(materials[i]);
                renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);
            }
            // Start lights hold materials separately from their lens renderers.
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(!behaviour)continue;var serialized=new SerializedObject(behaviour);var property=serialized.GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue is Material material)
                        property.objectReferenceValue=Persist(material);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static int landmark;
        [MenuItem("Polygon Racing/Debug - Starfall Port Landmark Views")]
        static void NextView()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath){Debug.LogWarning("Open Starfall Port first.");return;}
            var view=SceneView.lastActiveSceneView;if(!view)return;
            PolygonCircuit.SetLayout(PolygonCircuitLayout.StarfallPort);
            float[] knots={.45f,7.2f,12.4f,16.2f,23.1f,29.6f};float d=PolygonCircuit.DistanceAtKnot(knots[landmark++%knots.Length]);
            var eye=PolygonStarfallPortArt.RoadPoint(d,0,3);var target=PolygonStarfallPortArt.RoadPoint(d+130,0,5);
            var helper=new GameObject("Starfall Port viewpoint"){hideFlags=HideFlags.HideAndDontSave};
            helper.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
            view.AlignViewToObject(helper.transform);Object.DestroyImmediate(helper);view.Repaint();
        }
    }
}
#endif
