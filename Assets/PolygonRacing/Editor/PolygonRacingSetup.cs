using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace PolygonRacing.Editor
{
    public static class PolygonRacingSetup
    {
        const string ScenePath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalScenePath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        [MenuItem("Polygon Racing/1 - Create Forest Circuit")]
        public static void CreateCircuit()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Exit Play Mode first.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory("Assets/PolygonRacing/Scenes");
            Directory.CreateDirectory("Assets/PolygonRacing/Generated");AssetDatabase.Refresh();
            var root=PolygonWorld.Build();
            // Persist generated meshes/materials so the scene and prefab survive reloads and APK builds.
            var assets=new HashSet<Object>();int id=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;if(!mesh||AssetDatabase.Contains(mesh)||!assets.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath($"Assets/PolygonRacing/Generated/Mesh_{id++}.asset"));
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)
            {
                if(!mat||AssetDatabase.Contains(mat)||!assets.Add(mat))continue;
                AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/PolygonRacing/Generated/"+mat.name+".mat"));
            }
            var car=root.GetComponentInChildren<PolygonDriver>().gameObject;
            PrefabUtility.SaveAsPrefabAsset(car,"Assets/PolygonRacing/Generated/PlayerCar.prefab");
            EditorSceneManager.SaveScene(scene,ScenePath);
            var buildScenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            EnsureBuildScene(buildScenes,ScenePath);
            if(File.Exists(CoastalScenePath))EnsureBuildScene(buildScenes,CoastalScenePath);
            EditorBuildSettings.scenes=buildScenes.ToArray();
            AssetDatabase.SaveAssets();Selection.activeGameObject=car;
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(car.transform.position+Vector3.up,Quaternion.Euler(20,140,0),8);
            Debug.Log("Forest Circuit created. Press Play and click Game view. W accelerates, A/D steer, Space brakes. Quest setup: Polygon Racing > 2 - Configure Quest.");
        }
        static void EnsureBuildScene(List<EditorBuildSettingsScene> scenes,string path)
        {
            foreach(var scene in scenes)if(scene.path==path){scene.enabled=true;return;}
            scenes.Add(new EditorBuildSettingsScene(path,true));
        }
        [MenuItem("Polygon Racing/2 - Configure Quest")]
        public static void ConfigureQuest()
        {
            PlayerSettings.companyName="Independent";PlayerSettings.productName="Polygon Racing VR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.bootleg714.polygonracingvr");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground=true;
            EditorUserBuildSettings.buildAppBundle=false;
            var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if(mobile){mobile.msaaSampleCount=4;mobile.renderScale=1f;mobile.supportsHDR=false;mobile.shadowDistance=0;GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.renderPipeline=mobile;EditorUtility.SetDirty(mobile);}
            Directory.CreateDirectory("Assets/XR/Settings");AssetDatabase.Refresh();
            if(!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey,out var store)||!store)
            {
                store=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(store,AssetDatabase.GenerateUniqueAssetPath("Assets/XR/Settings/XRGeneralSettings.asset"));
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,store,true);
            }
            var existing=store.SettingsForBuildTarget(BuildTargetGroup.Android);
            if(!existing||!existing.Manager)store.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var settings=store.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart=true;
            settings.Manager.automaticLoading=true;settings.Manager.automaticRunning=true;
            bool assigned=XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android);
            if(!assigned){Debug.LogError("Could not assign OpenXR. Enable OpenXR under Project Settings > XR Plug-in Management > Android, then run Configure Quest again.");return;}
            var oxr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if(oxr)
            {
                oxr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
                var features=new List<OpenXRFeature>();oxr.GetFeatures(features);
                foreach(var feature in features)
                {
                    string type=feature.GetType().Name;
                    if(type=="OculusQuestFeature"){feature.enabled=false;EditorUtility.SetDirty(feature);}
                    if(type=="OculusTouchControllerProfile"||type=="MetaQuestFeature"){feature.enabled=true;EditorUtility.SetDirty(feature);}
                }
                EditorUtility.SetDirty(oxr);
            }
            EditorUtility.SetDirty(store);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(settings.Manager);AssetDatabase.SaveAssets();
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
            Debug.Log("Quest Android defaults applied. Check Android OpenXR: Oculus Touch Controller Profile and Meta Quest Support must both be enabled. Run OpenXR Project Validation and fix required errors before building. See README_FIRST.md.");
        }
        [MenuItem("Polygon Racing/3 - Check Quest Setup")]
        public static void CheckSetup()
        {
            var issues=new List<string>();
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            bool loader=false;if(general&&general.Manager)foreach(var l in general.Manager.activeLoaders)if(l&&l.GetType().FullName=="UnityEngine.XR.OpenXR.OpenXRLoader")loader=true;
            if(!loader||!general||!general.InitManagerOnStart)issues.Add("Android OpenXR loader / Initialize XR on Startup is missing.");
            var oxr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            bool touch=false,quest=false;
            if(oxr){var list=new List<OpenXRFeature>();oxr.GetFeatures(list);foreach(var f in list){if(!f.enabled)continue;string n=f.GetType().Name;touch|=n=="OculusTouchControllerProfile";quest|=n=="MetaQuestFeature"||n=="OculusQuestFeature";}}
            if(!touch)issues.Add("Enable Oculus Touch Controller Profile under Android OpenXR Interaction Profiles.");
            if(!quest)issues.Add("Enable Meta Quest Support under Android OpenXR Features.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)issues.Add("Switch the active Build Profile to Android.");
            if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)!=ScriptingImplementation.IL2CPP||PlayerSettings.Android.targetArchitectures!=AndroidArchitecture.ARM64)issues.Add("Run Configure Quest to select IL2CPP and ARM64.");
            if(!File.Exists(ScenePath))issues.Add("Run Create Forest Circuit first.");
            string message=issues.Count==0?"Local setup checks passed. Also run OpenXR Project Validation (fix required errors), then Build And Run with the Quest connected.":string.Join("\n\n",issues);
            EditorUtility.DisplayDialog("Polygon Racing - Quest Setup",message,"OK");
            if(issues.Count>0)Debug.LogWarning(message);else Debug.Log(message);
        }
    }
}
