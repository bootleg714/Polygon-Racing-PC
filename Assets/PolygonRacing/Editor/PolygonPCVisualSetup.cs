#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PolygonRacing.Editor
{
    public static class PolygonPCVisualSetup
    {
        const string RivieraPath = "Assets/PolygonRacing/Scenes/RivieraHarbour.unity";
        const string ProfileFolder = "Assets/PolygonRacing/Generated/PCVisuals/Riviera";
        const string VolumeProfilePath = ProfileFolder + "/Riviera PC Volume Profile.asset";
        const string SkyProfilePath = ProfileFolder + "/Riviera PC Sky.mat";
        const string BackupFolder = "PolygonRacingBackups";
        const string VolumeObjectName = "PC Visual Profile - Riviera Harbour";

        [MenuItem("Polygon Racing/PC Visuals/Apply PC Visual Profile - Riviera Harbour")]
        static void ApplyRivieraProfile()
        {
            ApplyRivieraProfile(false);
        }

        [MenuItem("Polygon Racing/PC Visuals/Refresh PC Materials - Riviera Harbour")]
        static void RefreshRivieraMaterials()
        {
            ApplyRivieraProfile(true);
        }

        static void ApplyRivieraProfile(bool materialsOnly)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before applying the PC visual profile.");
                return;
            }
            if (!Resources.Load<Shader>("PolygonFlatPC") ||
                (!materialsOnly && !Resources.Load<Shader>(PolygonLook.SkyShaderName)))
            {
                EditorUtility.DisplayDialog("PC visual profile",
                    "PolygonFlatPC.shader or PolygonSky.shader is missing or has not finished importing. Nothing was changed.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(RivieraPath))
            {
                Debug.LogError("Riviera Harbour scene was not found: " + RivieraPath);
                return;
            }

            string backupPath = Path.Combine(BackupFolder,
                (materialsOnly ? "RivieraHarbour_before_pc_materials_" : "RivieraHarbour_before_pc_visuals_") +
                System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
            try
            {
                Directory.CreateDirectory(BackupFolder);
                File.Copy(RivieraPath, backupPath, false);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Riviera Harbour could not be backed up. The scene was not changed.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(RivieraPath, OpenSceneMode.Single);
            try
            {
                EnsureFolder();
                if (materialsOnly)
                {
                    ConvertSceneMaterials(false);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                else if (!ApplyToOpenRivieraScene()) return;
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                {
                    Debug.LogError("Could not save Riviera Harbour. Backup: " + backupPath);
                    return;
                }
                Debug.Log("Riviera Harbour PC " + (materialsOnly ? "materials refreshed" : "visual profile applied") +
                    ". Scene backup: " + backupPath);
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }
        }

        public static bool ApplyToOpenRivieraScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying the Riviera PC visual profile.");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().path != RivieraPath)
            {
                Debug.LogError("The active scene is not Riviera Harbour; its PC visual profile was not applied.");
                return false;
            }
            if (!Resources.Load<Shader>("PolygonFlatPC") || !Resources.Load<Shader>(PolygonLook.SkyShaderName))
            {
                Debug.LogError("PolygonFlatPC.shader or PolygonSky.shader is missing or has not finished importing.");
                return false;
            }
            var driver = Object.FindFirstObjectByType<PolygonDriver>();
            if (!driver || !driver.view)
            {
                Debug.LogError("Riviera Harbour player or cockpit camera was not found.");
                return false;
            }

            EnsureFolder();
            var preset = PolygonLook.For(PolygonCircuitLayout.Riviera);
            if (!ConfigurePcPipeline()) return false;
            ConvertSceneMaterials();
            if (!ConfigureEnvironment(driver, preset) || !ConfigurePostProcessing(driver)) return false;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return true;
        }

        static bool ConfigurePcPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/PC_RPAsset.asset");
            if (!pipeline)
            {
                Debug.LogError("Assets/Settings/PC_RPAsset.asset is missing; PC renderer settings were not changed.");
                return false;
            }
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 120;
            pipeline.shadowCascadeCount = 4;
            EditorUtility.SetDirty(pipeline);
            return true;
        }

        static void ConvertSceneMaterials(bool configureRendererShadows = true)
        {
            var shader = Resources.Load<Shader>("PolygonFlatPC");
            var carbonShader = Resources.Load<Shader>("PolygonCarbonPC");
            var clones = new Dictionary<Material, Material>();
            var refreshed = new HashSet<Material>();
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene || renderer is ParticleSystemRenderer ||
                    renderer.GetComponent<TextMesh>()) continue;
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source || !source.shader) continue;
                    if (source.shader == shader || (carbonShader && source.shader == carbonShader))
                    {
                        // Already converted: refresh profile fill without resetting any surface tuning.
                        if (refreshed.Add(source))
                        {
                            SetAmbient(source);
                            EditorUtility.SetDirty(source);
                        }
                        continue;
                    }
                    if (source.shader.name != "Polygon Racing/Flat Lit") continue;
                    if (!clones.TryGetValue(source, out var material))
                    {
                        material = CreateOrUpdateClone(source, shader);
                        clones[source] = material;
                        refreshed.Add(material);
                    }
                    materials[i] = material;
                    changed = true;
                }
                if (!changed) continue;
                renderer.sharedMaterials = materials;
                if (configureRendererShadows)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
                EditorUtility.SetDirty(renderer);
            }
        }

        static Material CreateOrUpdateClone(Material source, Shader shader)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string safeName = Sanitize(source.name);
            string path = ProfileFolder + "/" + safeName + " PC.mat";
            if (!string.IsNullOrEmpty(sourceGuid)) path = ProfileFolder + "/" + sourceGuid + " " + safeName + " PC.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(source) { name = source.name + " PC" };
                material.shader = shader;
                InitializeSurfaceResponse(material, source);
                AssetDatabase.CreateAsset(material, path);
            }

            // Reuse the clone's identity and artist edits when scenery is regenerated.
            material.shader = shader;
            SetAmbient(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void InitializeSurfaceResponse(Material material, Material source)
        {
            string name = source.name.ToLowerInvariant();
            float metallic = 0.02f;
            float smoothness = 0.18f;
            if (ContainsAny(name, "metal", "gilded", "copper", "steel", "chrome", "alloy"))
            {
                metallic = 0.68f;
                smoothness = 0.48f;
            }
            else if (ContainsAny(name, "vermilion car body", "car livery", "car body", "paint"))
            {
                metallic = 0.12f;
                smoothness = 0.44f;
            }
            else if (ContainsAny(name, "car mirror"))
            {
                metallic = 0.4f;
                smoothness = 0.62f;
            }
            else if (ContainsAny(name, "glass"))
            {
                metallic = 0.12f;
                smoothness = 0.5f;
            }
            else if (ContainsAny(name, "rubber", "tyre", "tire", "asphalt", "road", "gutter", "car carbon"))
            {
                metallic = 0;
                smoothness = 0.08f;
            }
            // Supply first-conversion defaults only for properties the source shader lacks.
            if (!source.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (!source.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (!source.HasProperty("_ReflectionStrength")) material.SetFloat("_ReflectionStrength", 0.28f);
        }

        static bool ConfigureEnvironment(PolygonDriver driver, PolygonLook.Preset preset)
        {
            var sun = RenderSettings.sun;
            if (!sun)
            {
                foreach (var light in Object.FindObjectsByType<Light>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (!sun)
            {
                Debug.LogError("Riviera Harbour has no directional sun; lighting was not fully applied.");
                return false;
            }

            sun.transform.rotation = Quaternion.Euler(preset.SunEuler);
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.35f;
            sun.shadowNearPlane = 0.2f;
            EditorUtility.SetDirty(sun);

            var skyShader = Resources.Load<Shader>(PolygonLook.SkyShaderName);
            if (skyShader)
            {
                var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyProfilePath);
                if (!sky)
                {
                    sky = new Material(skyShader) { name = "Riviera PC Sky" };
                    AssetDatabase.CreateAsset(sky, SkyProfilePath);
                }
                sky.shader = skyShader;
                sky.SetColor("_ZenithColor", new Color(0.09f, 0.32f, 0.68f));
                sky.SetColor("_HorizonColor", new Color(0.66f, 0.82f, 0.88f));
                sky.SetColor("_BelowHorizonColor", new Color(0.31f, 0.53f, 0.68f));
                sky.SetColor("_SunColor", sun.color);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.75f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.54f, 0.62f, 0.68f);
            RenderSettings.ambientGroundColor = new Color(0.48f, 0.42f, 0.35f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.88f);
            RenderSettings.fogStartDistance = 380;
            RenderSettings.fogEndDistance = 1250;
            DynamicGI.UpdateEnvironment();

            driver.view.clearFlags = CameraClearFlags.Skybox;
            driver.view.backgroundColor = RenderSettings.fogColor;
            driver.view.allowMSAA = true;
            var cameraData = driver.view.GetComponent<UniversalAdditionalCameraData>();
            if (!cameraData) cameraData = driver.view.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.None;
            EditorUtility.SetDirty(driver.view);
            EditorUtility.SetDirty(cameraData);
            EditorUtility.SetDirty(RenderSettings.sun);
            return true;
        }

        static bool ConfigurePostProcessing(PolygonDriver driver)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Riviera PC Volume Profile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            var tonemapping = GetOrAdd<Tonemapping>(profile);
            var color = GetOrAdd<ColorAdjustments>(profile);
            var bloom = GetOrAdd<Bloom>(profile);
            if (!tonemapping || !color || !bloom)
            {
                Debug.LogError("Riviera PC volume overrides could not be created as profile sub-assets.");
                return false;
            }

            tonemapping.mode.Override(TonemappingMode.Neutral);
            color.postExposure.Override(0.1f);
            color.contrast.Override(2f);
            color.saturation.Override(8f);
            bloom.threshold.Override(1.2f);
            bloom.intensity.Override(0.16f);
            bloom.scatter.Override(0.55f);
            bloom.highQualityFiltering.Override(false);
            bloom.maxIterations.Override(4);
            bloom.dirtIntensity.Override(0f);

            var volumeObject = GameObject.Find(VolumeObjectName);
            if (!volumeObject) volumeObject = new GameObject(VolumeObjectName);
            var volume = volumeObject.GetComponent<Volume>();
            if (!volume) volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = profile;
            profile.Reset();
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(driver.view);
            return true;
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component)) component = profile.Add<T>(true);
            if (!component) return null;
            if (!AssetDatabase.Contains(component))
            {
                AssetDatabase.AddObjectToAsset(component, profile);
                if (!AssetDatabase.Contains(component))
                {
                    Debug.LogError("Could not persist " + typeof(T).Name + " as a Riviera volume-profile sub-asset.");
                    return null;
                }
            }
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        static void SetAmbient(Material material)
        {
            // These four values belong to the PC profile. All other material properties are artist-owned.
            material.SetColor("_SkyAmbient", new Color(0.68f, 0.78f, 0.90f));
            material.SetColor("_GroundAmbient", new Color(0.54f, 0.48f, 0.40f));
            material.SetFloat("_AmbientStrength", 0.95f);
            material.SetFloat("_SunWrap", 0.12f);
        }

        static bool ContainsAny(string value, params string[] terms)
        {
            foreach (var term in terms) if (value.Contains(term)) return true;
            return false;
        }

        static string Sanitize(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            return name;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated/PCVisuals"))
                AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated", "PCVisuals");
            if (!AssetDatabase.IsValidFolder(ProfileFolder))
                AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated/PCVisuals", "Riviera");
        }
    }
}
#endif
