#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing.Editor
{
    // Sky, light and water pass. Converts each scene's opaque materials to the flat-lit
    // polygon shader, water to the faceted water shader, and adds a gradient sky,
    // camera-anchored clouds, warmer sun and matching horizon fog. Geometry, the route,
    // the cockpit/tracking setup and gameplay are untouched.
    public static class PolygonLookSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        const string LookPath="Assets/PolygonRacing/Generated/Look";
        const string CloudObjectName="Sky - camera anchored clouds";

        static readonly Color SkyAmbient=new Color(.60f,.74f,.86f),GroundAmbient=new Color(.50f,.42f,.33f);
        const float AmbientStrength=.75f;
        static readonly HashSet<string> Convertible=new HashSet<string>{
            "Universal Render Pipeline/Simple Lit","Universal Render Pipeline/Lit","Universal Render Pipeline/Baked Lit",
            "Standard","Polygon Racing/Flat Lit","Polygon Racing/Faceted Water"};

        [MenuItem("Polygon Racing/9 - Apply Sky, Light and Water Look")]
        static void ApplyLookToBothCircuits()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before applying the look.");return;}
            if(!ShadersPresent(out string missing))
            {
                EditorUtility.DisplayDialog("Polygon Racing look",
                    "Missing "+missing+" in Assets/PolygonRacing/Resources. Let Unity finish importing, then try again. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            int done=0;
            foreach(var (path,layout) in new[]{(ForestPath,PolygonCircuitLayout.Forest),(CoastalPath,PolygonCircuitLayout.CoastalExpressway),(PolygonDowntownSetup.DowntownPath,PolygonCircuitLayout.Downtown),(PolygonRivieraSetup.RivieraPath,PolygonCircuitLayout.Riviera),(PolygonSakuraSetup.SakuraPath,PolygonCircuitLayout.Sakura),(PolygonNordwaldSetup.NordwaldPath,PolygonCircuitLayout.Nordwald)})
            {
                if(!File.Exists(path)){Debug.LogWarning("Skipped missing scene "+path);continue;}
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                string backup="PolygonRacingBackups/"+Path.GetFileNameWithoutExtension(path)+"_before_look_"+
                    System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(path,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up "+path+". It was not changed.");continue;}
                if(!ApplyToOpenScene(layout))continue;
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)){Debug.LogError("Unity could not save "+path+".");continue;}
                done++;
                Debug.Log($"Sky, light and water look applied to {path}. Backup: {backup}");
            }
            AssetDatabase.SaveAssets();
            if(done>0)Debug.Log("Polygon Racing look applied to "+done+" circuit(s). Save is done; use Build And Run to try it in the headset.");
        }

        static bool ShadersPresent(out string missing)
        {
            missing=null;
            foreach(var name in new[]{PolygonLook.FlatShaderName,PolygonLook.WaterShaderName,PolygonLook.SkyShaderName,PolygonLook.CloudShaderName})
                if(!Resources.Load<Shader>(name)){missing=name+".shader";return false;}
            return true;
        }

        // Applies the look to the scene that is currently open. Also called by the coastal
        // scenery refresh so rebuilt scenery keeps the look. Returns false if nothing changed.
        public static bool ApplyToOpenScene(PolygonCircuitLayout layout)
        {
            if(!ShadersPresent(out string missing))
            {
                Debug.LogWarning("Polygon Racing look not applied: "+missing+" is missing from Assets/PolygonRacing/Resources.");
                return false;
            }
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent)
            {
                Debug.LogError("Polygon Racing look: the player car could not be found in this scene. Nothing was changed.");
                return false;
            }
            EnsureFolder();
            var preset=PolygonLook.For(layout);
            ConvertMaterials(preset);
            ApplySignLettering(player);
            var sun=ConfigureSun(player.transform.parent,preset);
            var sky=SkyMaterial(layout,preset);
            ConfigureEnvironment(player,sun,sky,preset);
            BuildClouds(player.transform.parent,layout,preset);
            AssetDatabase.SaveAssets();
            return true;
        }

        // ------------------------------------------------------------ materials
        static void ConvertMaterials(PolygonLook.Preset preset)
        {
            var flat=Resources.Load<Shader>(PolygonLook.FlatShaderName);
            var water=Resources.Load<Shader>(PolygonLook.WaterShaderName);
            var waterUses=new HashSet<Material>();
            var otherUses=new Dictionary<Material,List<(Renderer renderer,int slot)>>();
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(renderer is ParticleSystemRenderer||renderer.GetComponent<TextMesh>())continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var m=materials[i];
                    if(!m||!m.shader||!Convertible.Contains(m.shader.name))continue;
                    if(IsWaterMaterial(m)&&IsWaterObject(renderer.gameObject.name))waterUses.Add(m);
                    else
                    {
                        if(!otherUses.TryGetValue(m,out var list))otherUses[m]=list=new List<(Renderer,int)>();
                        list.Add((renderer,i));
                    }
                }
            }
            foreach(var m in waterUses)
            {
                var color=BaseColor(m);float cull=m.HasProperty("_Cull")?m.GetFloat("_Cull"):2;
                m.shader=water;
                m.SetColor("_BaseColor",color);m.SetFloat("_Cull",cull);
                m.SetColor("_ReflectionZenith",preset.WaterZenith);
                // Look v02 water strength (v01 was too subtle in the headset).
                m.SetFloat("_CellSize",8);m.SetFloat("_WaveHeight",1.5f);m.SetFloat("_WaveSpeed",1.2f);
                m.SetFloat("_FacetFadeStart",90);m.SetFloat("_FacetFadeEnd",420);m.SetFloat("_Fresnel",.35f);
                m.SetFloat("_Specular",2);m.SetFloat("_SpecularPower",60);m.SetFloat("_FacetTint",.22f);
                SetAmbient(m);
                EditorUtility.SetDirty(m);
            }
            foreach(var pair in otherUses)
            {
                var m=pair.Key;
                if(waterUses.Contains(m))
                {
                    // Same colour used for both water and a building, e.g. turquoise walls:
                    // the building gets its own flat-lit twin so it isn't drawn as water.
                    var twin=FlatTwin(m,flat);
                    foreach(var (renderer,slot) in pair.Value)
                    {
                        var materials=renderer.sharedMaterials;materials[slot]=twin;renderer.sharedMaterials=materials;
                        EditorUtility.SetDirty(renderer);
                    }
                    continue;
                }
                ToFlat(m,flat);
            }
        }

        static void ToFlat(Material m,Shader flat)
        {
            var color=BaseColor(m);
            var emission=Color.black;
            if(m.HasProperty("_EmissionColor")&&(m.shader.name.StartsWith("Polygon Racing")||m.IsKeywordEnabled("_EMISSION")))
                emission=m.GetColor("_EmissionColor");
            float cull=m.HasProperty("_Cull")?m.GetFloat("_Cull"):2;
            m.shader=flat;
            m.SetColor("_BaseColor",color);m.SetColor("_EmissionColor",emission);m.SetFloat("_Cull",cull);
            SetAmbient(m);
            EditorUtility.SetDirty(m);
        }

        static Material FlatTwin(Material source,Shader flat)
        {
            string path=$"{LookPath}/{Sanitize(source.name)} flat twin.mat";
            var twin=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!twin){twin=new Material(flat){name=source.name+" flat twin"};AssetDatabase.CreateAsset(twin,path);}
            twin.shader=flat;
            twin.SetColor("_BaseColor",BaseColor(source));twin.SetColor("_EmissionColor",Color.black);twin.SetFloat("_Cull",2);
            SetAmbient(twin);
            EditorUtility.SetDirty(twin);
            return twin;
        }

        static void SetAmbient(Material m)
        {
            m.SetColor("_SkyAmbient",SkyAmbient);m.SetColor("_GroundAmbient",GroundAmbient);m.SetFloat("_AmbientStrength",AmbientStrength);
        }

        static Color BaseColor(Material m)
        {
            if(m.HasProperty("_BaseColor"))return m.GetColor("_BaseColor");
            return m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;
        }

        static bool IsWaterMaterial(Material m)
        {
            string n=m.name.ToLowerInvariant();
            if(n.Contains("foam")||n.Contains("streak")||n.Contains("surf"))return false;
            return n.Contains("turquoise")||n.Contains("ocean")||n.Contains("lagoon")||n.Contains("water");
        }

        static bool IsWaterObject(string name)
        {
            string n=name.ToLowerInvariant();
            // Merged landmark meshes are named "<landmark> - <material>" and are buildings.
            if(n.Contains(" - ")||n.Contains("streak")||n.Contains("surf"))return false;
            return n.Contains("water")||n.Contains("estuary")||n.Contains("ocean")||n.Contains("atlantic")||n.EndsWith(" sea");
        }

        static string Sanitize(string name)
        {
            foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');
            return name;
        }

        // Roadside sign text used Unity's default font material, which draws through rocks and
        // cliffs. Point it at the depth-tested lettering shader (shared with the gantry title).
        static void ApplySignLettering(PolygonDriver player)
        {
            var shader=Resources.Load<Shader>("PolygonGantryLettering");
            if(!shader){Debug.LogWarning("PolygonGantryLettering.shader not found; sign text left unchanged.");return;}
            string path=$"{LookPath}/Sign lettering depth tested.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(shader){name="Sign lettering depth tested"};AssetDatabase.CreateAsset(mat,path);}
            mat.shader=shader;
            Font font=null;int count=0;
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!text||!text.font||text.transform.IsChildOf(player.transform))continue;
                var renderer=text.GetComponent<MeshRenderer>();if(!renderer)continue;
                if(renderer.sharedMaterial&&renderer.sharedMaterial.shader==shader&&renderer.sharedMaterial!=mat)continue;   // gantry title
                font=text.font;renderer.sharedMaterial=mat;EditorUtility.SetDirty(renderer);count++;
            }
            if(!font)return;
            mat.mainTexture=font.material.mainTexture;EditorUtility.SetDirty(mat);
            var root=player.transform.parent;
            var binder=root.GetComponent<PolygonSignLettering>();
            if(!binder)binder=root.gameObject.AddComponent<PolygonSignLettering>();
            binder.Font=font;binder.DepthMaterial=mat;EditorUtility.SetDirty(binder);
            Debug.Log($"Depth-tested lettering applied to {count} sign texts.");
        }

        // ------------------------------------------------------------ sun, sky, fog
        static Light ConfigureSun(Transform root,PolygonLook.Preset preset)
        {
            Light sun=null;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(light.type==LightType.Directional){sun=light;break;}
            if(!sun)
            {
                var go=new GameObject("Sun",typeof(Light));go.transform.SetParent(root,false);
                sun=go.GetComponent<Light>();sun.type=LightType.Directional;
            }
            sun.transform.rotation=Quaternion.Euler(preset.SunEuler);
            sun.color=preset.SunColor;sun.intensity=preset.SunIntensity;sun.shadows=LightShadows.None;
            EditorUtility.SetDirty(sun);EditorUtility.SetDirty(sun.transform);
            return sun;
        }

        static Material SkyMaterial(PolygonCircuitLayout layout,PolygonLook.Preset preset)
        {
            var shader=Resources.Load<Shader>(PolygonLook.SkyShaderName);
            string path=$"{LookPath}/Sky {layout}.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!sky){sky=new Material(shader){name="Sky "+layout};AssetDatabase.CreateAsset(sky,path);}
            sky.shader=shader;
            sky.SetColor("_ZenithColor",preset.Zenith);sky.SetColor("_HorizonColor",preset.Horizon);
            sky.SetColor("_BelowHorizonColor",preset.BelowHorizon);sky.SetColor("_SunColor",preset.SunColor);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        static void ConfigureEnvironment(PolygonDriver player,Light sun,Material sky,PolygonLook.Preset preset)
        {
            RenderSettings.skybox=sky;RenderSettings.sun=sun;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=preset.Horizon;
            RenderSettings.fogStartDistance=preset.FogStart;RenderSettings.fogEndDistance=preset.FogEnd;
            // Trilight ambient matches the flat shader's hemisphere for anything still on URP shaders.
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=SkyAmbient*AmbientStrength;
            RenderSettings.ambientEquatorColor=Color.Lerp(SkyAmbient,GroundAmbient,.5f)*AmbientStrength;
            RenderSettings.ambientGroundColor=GroundAmbient*AmbientStrength;
            DynamicGI.UpdateEnvironment();
            if(player.view)
            {
                player.view.clearFlags=CameraClearFlags.Skybox;
                player.view.backgroundColor=preset.Horizon;
                EditorUtility.SetDirty(player.view);
            }
        }

        // ------------------------------------------------------------ clouds
        static void BuildClouds(Transform root,PolygonCircuitLayout layout,PolygonLook.Preset preset)
        {
            var old=root.Find(CloudObjectName);
            if(old)Object.DestroyImmediate(old.gameObject);

            string meshPath=$"{LookPath}/Clouds {layout}.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(!mesh){mesh=new Mesh{name="Clouds "+layout};AssetDatabase.CreateAsset(mesh,meshPath);}
            FillCloudMesh(mesh,preset.CloudRadius,preset.CloudSeed);
            EditorUtility.SetDirty(mesh);

            var shader=Resources.Load<Shader>(PolygonLook.CloudShaderName);
            string matPath=$"{LookPath}/Clouds {layout}.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat){mat=new Material(shader){name="Clouds "+layout};AssetDatabase.CreateAsset(mat,matPath);}
            mat.shader=shader;
            mat.SetColor("_HorizonColor",preset.Horizon);mat.SetColor("_ShadowColor",SkyAmbient);
            EditorUtility.SetDirty(mat);

            var go=new GameObject(CloudObjectName,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(root,false);
            go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;go.transform.localScale=Vector3.one;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }

        // Faceted half-dome puffs in a ring around the viewer, 7-17 degrees above the horizon.
        static void FillCloudMesh(Mesh mesh,float radius,int seed)
        {
            var rng=new System.Random(seed);
            float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int clouds=16,segments=6;
            var ringShape=new[]{new Vector2(1,0),new Vector2(.8f,.6f),new Vector2(.35f,1)};
            for(int k=0;k<clouds;k++)
            {
                float azimuth=k*Mathf.PI*2/clouds+R(-.15f,.15f);
                float elevation=R(7,17)*Mathf.Deg2Rad;
                float width=radius*R(.10f,.19f),height=width*R(.18f,.28f);
                var center=new Vector3(Mathf.Sin(azimuth)*Mathf.Cos(elevation),Mathf.Sin(elevation),Mathf.Cos(azimuth)*Mathf.Cos(elevation))*radius;
                var tangent=new Vector3(Mathf.Cos(azimuth),0,-Mathf.Sin(azimuth));
                var inward=-center.normalized;
                int blobs=rng.Next(3,6);
                for(int b=0;b<blobs;b++)
                {
                    var offset=tangent*R(-.5f,.5f)*width+Vector3.up*R(0,.3f)*height;
                    float blobWidth=width*R(.35f,.6f),blobHeight=height*R(.7f,1.3f);
                    var origin=center+offset;
                    int start=vertices.Count;
                    foreach(var ring in ringShape)
                        for(int s=0;s<=segments;s++)
                        {
                            float a=s*Mathf.PI/segments;
                            vertices.Add(origin+tangent*(Mathf.Cos(a)*blobWidth*ring.x)
                                +inward*(Mathf.Sin(a)*blobWidth*.35f*ring.x)+Vector3.up*(ring.y*blobHeight));
                        }
                    for(int r=0;r<ringShape.Length-1;r++)
                        for(int s=0;s<segments;s++)
                        {
                            int a=start+r*(segments+1)+s,c=start+(r+1)*(segments+1)+s;
                            triangles.AddRange(new[]{a,a+1,c+1,a,c+1,c});
                        }
                }
            }
            mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            // The shader re-centres the clouds on the camera, so never frustum-cull them.
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*100000);
        }

        static void EnsureFolder()
        {
            if(AssetDatabase.IsValidFolder(LookPath))return;
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Look");
        }
    }
}
#endif
