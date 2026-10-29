#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace PolygonRacing.Editor
{
    // Lighting & atmosphere pass (October 29, 2026): "wet road" reflections for the neon stages.
    // A row of small baked reflection probes follows the circuit a few metres above the road.
    // Each one captures the surrounding neon (opaque emissive geometry, HDR) into a 128 px cubemap;
    // the PC Flat Lit shader already samples reflection probes through GlossyEnvironmentReflection,
    // so the glossier asphalt picks up coloured city/canyon light without any real-time lights,
    // screen-space reflections or per-frame cost beyond one probe-blended cubemap lookup.
    public static class PolygonNeonReflections
    {
        public const string RootName="Wet road reflection probes";

        // Bakes probes for the open neon scene. Car and other non-scenery renderers are hidden
        // during capture so the start grid does not reflect a giant car.
        public static int Bake(Transform circuitRoot,Transform scenery,string folder,string label,float spacing=80,float height=4.5f,int resolution=128)
        {
            if(EditorApplication.isPlaying){Debug.LogError("Stop Play Mode before baking reflection probes.");return 0;}
            var old=circuitRoot.Find(RootName);if(old)Object.DestroyImmediate(old.gameObject);
            EnsureFolder(folder);
            var stale=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Texture",new[]{folder}))stale.Add(AssetDatabase.GUIDToAssetPath(guid));
            if(stale.Count>0){var failed=new List<string>();AssetDatabase.DeleteAssets(stale.ToArray(),failed);}

            // Hide everything outside the generated scenery (car, rivals, cockpit) while capturing.
            var hidden=new List<Renderer>();
            var scene=circuitRoot.gameObject.scene;
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(r.gameObject.scene==scene&&r.enabled&&(!(scenery&&r.transform.IsChildOf(scenery))||LowGlow(r))){r.enabled=false;hidden.Add(r);}

            // The GPU Resident Drawer does not draw into editor probe captures (the probes came out
            // sky-only), so switch it off for the bake and restore it afterwards.
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var residentMode=pipeline?pipeline.gpuResidentDrawerMode:GPUResidentDrawerMode.Disabled;
            if(pipeline&&residentMode!=GPUResidentDrawerMode.Disabled)pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;

            // Capture without fog and against a black sky. The hazy fog horizon reflected at grazing
            // angles as a light band across the road (Thomas, 11:42); the neon itself is what should
            // show in the wet asphalt, with the night sky staying dark.
            bool fog=RenderSettings.fog;RenderSettings.fog=false;

            var root=new GameObject(RootName).transform;root.SetParent(circuitRoot,false);
            int count=Mathf.Max(4,Mathf.RoundToInt(PolygonCircuit.Length/spacing)),baked=0;
            try
            {
                for(int i=0;i<count;i++)
                {
                    float d=PolygonCircuit.Length*i/count;
                    var go=new GameObject($"Wet road probe {i:000}");go.transform.SetParent(root,false);
                    go.transform.localPosition=PolygonBanking.SurfacePoint(d,0,height);
                    var probe=go.AddComponent<ReflectionProbe>();
                    probe.mode=ReflectionProbeMode.Custom;
                    probe.resolution=resolution;probe.hdr=true;
                    // Box projection (parallax correction): reflections slide past at the speed of the
                    // world instead of riding along with the camera until the next probe takes over
                    // (Thomas: "some reflections move too slowly ... they linger"). Probes are closer
                    // together and their boxes tighter so the corrected reflection stays near the truth.
                    probe.size=new Vector3(spacing*2.1f,70,spacing*2.1f);probe.center=new Vector3(0,8,0);
                    probe.boxProjection=true;probe.importance=1;probe.blendDistance=spacing*.25f;
                    probe.nearClipPlane=.5f;probe.farClipPlane=spacing*1.8f;   // distant city glow converging at the horizon read as a light band
                    probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.004f,.005f,.012f);probe.cullingMask=~0;
                    probe.shadowDistance=0;
                    // The scenery is not lightmap-static, so a baked/custom capture must include "dynamic" objects.
                    probe.renderDynamicObjects=true;
                    string path=$"{folder}/{label} wet road {i:000}.exr";
                    if(!Lightmapping.BakeReflectionProbe(probe,path)){Debug.LogWarning($"{label}: probe {i} could not be baked.");continue;}
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    if(AssetImporter.GetAtPath(path) is TextureImporter importer&&importer.textureShape!=TextureImporterShape.TextureCube)
                    {
                        importer.textureShape=TextureImporterShape.TextureCube;
                        importer.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;
                        importer.mipmapEnabled=true;
                        importer.SaveAndReimport();
                    }
                    probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if(probe.customBakedTexture)baked++;
                    EditorUtility.SetDirty(probe);
                }
            }
            finally
            {
                RenderSettings.fog=fog;
                foreach(var r in hidden)if(r)r.enabled=true;
                if(pipeline&&pipeline.gpuResidentDrawerMode!=residentMode)pipeline.gpuResidentDrawerMode=residentMode;
            }
            // Wherever the probe weights add up to less than one, Forward+ fills in with the sky
            // reflection; on the glossy road that showed as a light grey band at a fixed distance.
            // The night sky contributes almost nothing now, so only the captured neon shows.
            RenderSettings.reflectionIntensity=.15f;
            Debug.Log($"{label} wet-road reflections: {baked}/{count} probes baked ({resolution} px HDR, {spacing:0} m apart).");
            return baked;
        }

        // Trackside glows right beside the road (barrier light, edge spill, LED boards, kerbs) are
        // left out of the capture: reflected at grazing angles they smeared into a light band
        // across the street (Thomas, Oct 29 11:42). Buildings, arches and billboards still reflect.
        // The road surface itself is left out too: seen at a grazing angle from the probe it was light
        // grey and came back as a light ring across the street at a fixed distance (Oct 29 11:56).
        static readonly string[] lowGlowNames={"Barrier glow","Edge spill","LED board","Kerb",
            "Asphalt","Road markings","Road paint","Graphite deck","Bridge graphite"};
        static bool LowGlow(Renderer r)
        {
            var m=r.sharedMaterial;if(!m)return false;
            foreach(var n in lowGlowNames)if(m.name.Contains(n))return true;
            return false;
        }

        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }

        // Re-bakes the open neon stage's probes without regenerating its scenery (after art tweaks).
        [MenuItem("Polygon Racing/PC Visuals/Bake Wet-Road Reflections - Open Neon Stage")]
        static void BakeOpenScene()
        {
            var selector=Object.FindFirstObjectByType<PolygonCircuitSelector>();
            if(!selector){Debug.LogError("Open a neon stage first.");return;}
            string folder,label;
            switch(selector.Layout)
            {
                case PolygonCircuitLayout.NeonMeridian:folder="Assets/PolygonRacing/Generated/PCVisuals/Neon/Reflections";label="Neon Meridian";break;
                case PolygonCircuitLayout.IonRift:folder="Assets/PolygonRacing/Generated/PCVisuals/IonRift/Reflections";label="Ion Rift";break;
                case PolygonCircuitLayout.AuroraForge:folder="Assets/PolygonRacing/Generated/PCVisuals/AuroraForge/Reflections";label="Aurora Forge";break;
                case PolygonCircuitLayout.StarfallPort:folder="Assets/PolygonRacing/Generated/PCVisuals/StarfallPort/Reflections";label="Starfall Port";break;
                default:Debug.LogError("Wet-road reflections are only for the neon stages.");return;
            }
            PolygonCircuit.SetLayout(selector.Layout);
            var root=selector.transform;var scenery=root.Find("Circuit - editable scenery");
            if(Bake(root,scenery,folder,label)>0)
            {
                var scene=EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
