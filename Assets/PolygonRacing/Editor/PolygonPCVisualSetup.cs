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
        const string BackupFolder = "PolygonRacingBackups";

        // Per-circuit PC look. Riviera values are the user-accepted October 26 profile; do not retune
        // them when adding other circuits.
        sealed class Profile
        {
            public PolygonCircuitLayout Layout;
            public string DisplayName, ScenePath, Folder, FolderName, VolumeAssetName, SkyName, VolumeObjectName, BackupPrefix;
            public Color SunColor; public float SunIntensity, ShadowStrength;
            public Color Zenith, Horizon, BelowHorizon;
            public Color AmbientSky, AmbientEquator, AmbientGround;
            public Color FogColor; public float FogStart, FogEnd;
            public Color MaterialSky, MaterialGround; public float MaterialStrength, MaterialWrap;
            public float Exposure, Contrast, Saturation, BloomThreshold, BloomIntensity, BloomScatter;
            public float WhiteBalanceTemperature, Vignette; public bool ExtraGrading, CityGlass;
            public string VolumePath => Folder + "/" + VolumeAssetName + ".asset";
            public string SkyPath => Folder + "/" + SkyName + ".mat";
        }

        static readonly Profile Riviera = new Profile
        {
            Layout = PolygonCircuitLayout.Riviera, DisplayName = "Riviera Harbour",
            ScenePath = "Assets/PolygonRacing/Scenes/RivieraHarbour.unity",
            Folder = "Assets/PolygonRacing/Generated/PCVisuals/Riviera", FolderName = "Riviera",
            VolumeAssetName = "Riviera PC Volume Profile", SkyName = "Riviera PC Sky",
            VolumeObjectName = "PC Visual Profile - Riviera Harbour", BackupPrefix = "RivieraHarbour",
            SunColor = new Color(1f, 0.94f, 0.82f), SunIntensity = 1.3f, ShadowStrength = 0.62f,
            Zenith = new Color(0.09f, 0.32f, 0.68f), Horizon = new Color(0.66f, 0.82f, 0.88f),
            BelowHorizon = new Color(0.31f, 0.53f, 0.68f),
            AmbientSky = new Color(0.62f, 0.75f, 0.86f), AmbientEquator = new Color(0.54f, 0.62f, 0.68f),
            AmbientGround = new Color(0.48f, 0.42f, 0.35f),
            FogColor = new Color(0.66f, 0.82f, 0.88f), FogStart = 380, FogEnd = 1250,
            MaterialSky = new Color(0.68f, 0.78f, 0.90f), MaterialGround = new Color(0.54f, 0.48f, 0.40f),
            MaterialStrength = 0.95f, MaterialWrap = 0.12f,
            Exposure = 0.1f, Contrast = 2f, Saturation = 8f, BloomThreshold = 1.2f, BloomIntensity = 0.16f, BloomScatter = 0.55f
        };

        // Downtown PC v01: late golden hour. Long tower shadows across the boulevards, warm haze,
        // reflective curtain-wall glass and a stronger (still restrained) bloom for lit windows.
        static readonly Profile Downtown = new Profile
        {
            Layout = PolygonCircuitLayout.Downtown, DisplayName = "Downtown Streets",
            ScenePath = "Assets/PolygonRacing/Scenes/DowntownCircuit.unity",
            Folder = "Assets/PolygonRacing/Generated/PCVisuals/Downtown", FolderName = "Downtown",
            VolumeAssetName = "Downtown PC Volume Profile", SkyName = "Downtown PC Sky",
            VolumeObjectName = "PC Visual Profile - Downtown Streets", BackupPrefix = "DowntownCircuit",
            SunColor = new Color(1f, 0.84f, 0.64f), SunIntensity = 1.42f, ShadowStrength = 0.7f,
            Zenith = new Color(0.13f, 0.30f, 0.64f), Horizon = new Color(0.96f, 0.76f, 0.58f),
            BelowHorizon = new Color(0.58f, 0.47f, 0.43f),
            AmbientSky = new Color(0.56f, 0.64f, 0.80f), AmbientEquator = new Color(0.70f, 0.60f, 0.52f),
            AmbientGround = new Color(0.42f, 0.36f, 0.32f),
            FogColor = new Color(0.94f, 0.77f, 0.61f), FogStart = 360, FogEnd = 1300,
            MaterialSky = new Color(0.60f, 0.68f, 0.86f), MaterialGround = new Color(0.62f, 0.50f, 0.40f),
            MaterialStrength = 1.0f, MaterialWrap = 0.14f,
            Exposure = 0.05f, Contrast = 8f, Saturation = 10f, BloomThreshold = 1.0f, BloomIntensity = 0.3f, BloomScatter = 0.62f,
            WhiteBalanceTemperature = 6f, Vignette = 0.14f, ExtraGrading = true, CityGlass = true
        };

        static Profile active = Riviera;

        [MenuItem("Polygon Racing/PC Visuals/Apply PC Visual Profile - Riviera Harbour")]
        static void ApplyRivieraProfile()
        {
            ApplyProfile(Riviera, false);
        }

        [MenuItem("Polygon Racing/PC Visuals/Refresh PC Materials - Riviera Harbour")]
        static void RefreshRivieraMaterials()
        {
            ApplyProfile(Riviera, true);
        }

        [MenuItem("Polygon Racing/PC Visuals/Apply PC Visual Profile - Downtown Streets")]
        static void ApplyDowntownProfile()
        {
            ApplyProfile(Downtown, false);
        }

        [MenuItem("Polygon Racing/PC Visuals/Refresh PC Materials - Downtown Streets")]
        static void RefreshDowntownMaterials()
        {
            ApplyProfile(Downtown, true);
        }

        static void ApplyProfile(Profile profile, bool materialsOnly)
        {
            active = profile;
            string scenePath = profile.ScenePath;
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
            if (!File.Exists(scenePath))
            {
                Debug.LogError(profile.DisplayName + " scene was not found: " + scenePath);
                return;
            }

            string backupPath = Path.Combine(BackupFolder,
                profile.BackupPrefix + (materialsOnly ? "_before_pc_materials_" : "_before_pc_visuals_") +
                System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
            try
            {
                Directory.CreateDirectory(BackupFolder);
                File.Copy(scenePath, backupPath, false);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError(profile.DisplayName + " could not be backed up. The scene was not changed.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            try
            {
                EnsureFolder();
                if (materialsOnly)
                {
                    ConvertSceneMaterials(false);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                else if (!ApplyToOpenScene(profile)) return;
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                {
                    Debug.LogError("Could not save " + profile.DisplayName + ". Backup: " + backupPath);
                    return;
                }
                Debug.Log(profile.DisplayName + " PC " + (materialsOnly ? "materials refreshed" : "visual profile applied") +
                    ". Scene backup: " + backupPath);
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }
        }

        public static bool ApplyToOpenRivieraScene() => ApplyToOpenScene(Riviera);
        public static bool ApplyToOpenDowntownScene() => ApplyToOpenScene(Downtown);

        static bool ApplyToOpenScene(Profile profile)
        {
            active = profile;
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying the " + profile.DisplayName + " PC visual profile.");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().path != profile.ScenePath)
            {
                Debug.LogError("The active scene is not " + profile.DisplayName + "; its PC visual profile was not applied.");
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
                Debug.LogError(profile.DisplayName + " player or cockpit camera was not found.");
                return false;
            }

            EnsureFolder();
            var preset = PolygonLook.For(profile.Layout);
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
            string path = active.Folder + "/" + safeName + " PC.mat";
            if (!string.IsNullOrEmpty(sourceGuid)) path = active.Folder + "/" + sourceGuid + " " + safeName + " PC.mat";
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
            float reflection = 0.28f;
            if (active.CityGlass && ContainsAny(name, "window lit", "crown light", "aviation light", "lamp glow"))
            {
                // Emissive city lights stay matte so the glow reads cleanly.
                metallic = 0;
                smoothness = 0.1f;
                reflection = 0.04f;
            }
            else if (active.CityGlass && ContainsAny(name, "glass"))
            {
                // Downtown curtain walls: mirror-like glass that picks up the golden sky.
                metallic = 0.25f;
                smoothness = 0.78f;
                reflection = 0.6f;
            }
            else if (ContainsAny(name, "metal", "gilded", "copper", "steel", "chrome", "alloy"))
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
            if (!source.HasProperty("_ReflectionStrength")) material.SetFloat("_ReflectionStrength", reflection);
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
                Debug.LogError(active.DisplayName + " has no directional sun; lighting was not fully applied.");
                return false;
            }

            sun.transform.rotation = Quaternion.Euler(preset.SunEuler);
            sun.color = active.SunColor;
            sun.intensity = active.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = active.ShadowStrength;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.35f;
            sun.shadowNearPlane = 0.2f;
            EditorUtility.SetDirty(sun);

            var skyShader = Resources.Load<Shader>(PolygonLook.SkyShaderName);
            if (skyShader)
            {
                var sky = AssetDatabase.LoadAssetAtPath<Material>(active.SkyPath);
                if (!sky)
                {
                    sky = new Material(skyShader) { name = active.SkyName };
                    AssetDatabase.CreateAsset(sky, active.SkyPath);
                }
                sky.shader = skyShader;
                sky.SetColor("_ZenithColor", active.Zenith);
                sky.SetColor("_HorizonColor", active.Horizon);
                sky.SetColor("_BelowHorizonColor", active.BelowHorizon);
                sky.SetColor("_SunColor", sun.color);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = active.AmbientSky;
            RenderSettings.ambientEquatorColor = active.AmbientEquator;
            RenderSettings.ambientGroundColor = active.AmbientGround;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = active.FogColor;
            RenderSettings.fogStartDistance = active.FogStart;
            RenderSettings.fogEndDistance = active.FogEnd;
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
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(active.VolumePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = active.VolumeAssetName;
                AssetDatabase.CreateAsset(profile, active.VolumePath);
            }
            var tonemapping = GetOrAdd<Tonemapping>(profile);
            var color = GetOrAdd<ColorAdjustments>(profile);
            var bloom = GetOrAdd<Bloom>(profile);
            if (!tonemapping || !color || !bloom)
            {
                Debug.LogError(active.DisplayName + " PC volume overrides could not be created as profile sub-assets.");
                return false;
            }

            tonemapping.mode.Override(TonemappingMode.Neutral);
            color.postExposure.Override(active.Exposure);
            color.contrast.Override(active.Contrast);
            color.saturation.Override(active.Saturation);
            bloom.threshold.Override(active.BloomThreshold);
            bloom.intensity.Override(active.BloomIntensity);
            bloom.scatter.Override(active.BloomScatter);
            bloom.highQualityFiltering.Override(false);
            bloom.maxIterations.Override(4);
            bloom.dirtIntensity.Override(0f);
            if (active.ExtraGrading)
            {
                var balance = GetOrAdd<WhiteBalance>(profile);
                var vignette = GetOrAdd<Vignette>(profile);
                if (!balance || !vignette) return false;
                balance.temperature.Override(active.WhiteBalanceTemperature);
                balance.tint.Override(0f);
                vignette.intensity.Override(active.Vignette);
                vignette.smoothness.Override(0.45f);
                vignette.rounded.Override(false);
            }

            var volumeObject = GameObject.Find(active.VolumeObjectName);
            if (!volumeObject) volumeObject = new GameObject(active.VolumeObjectName);
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
                    Debug.LogError("Could not persist " + typeof(T).Name + " as a " + active.DisplayName + " volume-profile sub-asset.");
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
            material.SetColor("_SkyAmbient", active.MaterialSky);
            material.SetColor("_GroundAmbient", active.MaterialGround);
            material.SetFloat("_AmbientStrength", active.MaterialStrength);
            material.SetFloat("_SunWrap", active.MaterialWrap);
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
            if (!AssetDatabase.IsValidFolder(active.Folder))
                AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated/PCVisuals", active.FolderName);
        }
    }
}
#endif
