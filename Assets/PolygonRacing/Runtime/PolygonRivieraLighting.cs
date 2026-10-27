using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolygonRacing
{
    public enum PolygonRivieraTimeOfDay { Daytime, Sunset }

    // Runtime-only look: originals and saved Daytime assets are never edited.
    [DisallowMultipleComponent]
    public sealed class PolygonRivieraLighting : MonoBehaviour
    {
        struct RendererState { public Renderer renderer; public Material[] materials; }
        readonly List<RendererState> originals=new List<RendererState>();
        readonly Dictionary<Material,Material> sunsetMaterials=new Dictionary<Material,Material>();
        Material originalSky,sunsetSky;
        Light sun;
        Quaternion sunRotation;
        Color sunColor,fogColor,skyAmbient,equatorAmbient,groundAmbient;
        float sunIntensity;
        bool active;
        PolygonPCMirrors mirrors;

        public void Apply(PolygonRivieraTimeOfDay preset)
        {
            if(preset==PolygonRivieraTimeOfDay.Daytime){Restore();return;}
            if(active)return;
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.Riviera)return;
            sun=RenderSettings.sun;originalSky=RenderSettings.skybox;
            if(!sun||!originalSky)
            {Debug.LogError("Riviera Sunset needs the existing daytime sun and sky; daytime was retained.");return;}
            sunRotation=sun.transform.rotation;sunColor=sun.color;sunIntensity=sun.intensity;
            fogColor=RenderSettings.fogColor;skyAmbient=RenderSettings.ambientSkyColor;
            equatorAmbient=RenderSettings.ambientEquatorColor;groundAmbient=RenderSettings.ambientGroundColor;
            try
            {
                // Mark active before mutation so a failed apply can restore every setting.
                active=true;
                sunsetSky=new Material(originalSky){name="Riviera Sunset sky (runtime)"};
                sunsetSky.SetColor("_ZenithColor",new Color(.10f,.27f,.55f));
                sunsetSky.SetColor("_HorizonColor",new Color(.82f,.65f,.59f));
                sunsetSky.SetColor("_BelowHorizonColor",new Color(.39f,.39f,.45f));
                sunsetSky.SetColor("_SunColor",new Color(1,.77f,.58f));
                RenderSettings.skybox=sunsetSky;
                RenderSettings.fogColor=new Color(.82f,.65f,.59f);
                RenderSettings.ambientSkyColor=new Color(.62f,.69f,.79f);
                RenderSettings.ambientEquatorColor=new Color(.61f,.53f,.49f);
                RenderSettings.ambientGroundColor=new Color(.51f,.43f,.40f);
                sun.transform.rotation=Quaternion.Euler(14,-120,0);
                sun.color=new Color(1,.79f,.62f);sun.intensity=1.18f;
                ApplySceneMaterials();
                mirrors=GetComponent<PolygonPCMirrors>();if(mirrors)mirrors.SetSunsetLighting(true);
                DynamicGI.UpdateEnvironment();
            }
            catch(System.Exception ex){Debug.LogException(ex);Restore();}
        }
        void ApplySceneMaterials()
        {
            var scene=gameObject.scene;
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=scene||renderer is ParticleSystemRenderer||renderer.GetComponent<TextMesh>())continue;
                var original=renderer.sharedMaterials;Material[] changed=null;
                for(int i=0;i<original.Length;i++)
                {
                    var source=original[i];if(!source||!source.shader)continue;
                    string shader=source.shader.name;
                    bool water=shader=="Polygon Racing/Faceted Water";
                    if(!water&&shader!="Polygon Racing/PC Flat Lit"&&shader!="Polygon Racing/PC Carbon Weave"&&shader!="Polygon Racing/Flat Lit")continue;
                    if(!sunsetMaterials.TryGetValue(source,out var material))
                    {
                        material=new Material(source){name=source.name+" (Sunset runtime)"};
                        if(material.HasProperty("_SkyAmbient"))material.SetColor("_SkyAmbient",new Color(.69f,.73f,.82f));
                        if(material.HasProperty("_GroundAmbient"))material.SetColor("_GroundAmbient",new Color(.59f,.47f,.42f));
                        if(shader=="Polygon Racing/PC Carbon Weave"&&material.HasProperty("_SunsetSheen"))
                            material.SetFloat("_SunsetSheen",.11f);
                        if(water&&material.HasProperty("_ReflectionZenith"))
                            material.SetColor("_ReflectionZenith",Color.Lerp(source.GetColor("_ReflectionZenith"),new Color(.88f,.58f,.42f),.16f));
                        sunsetMaterials.Add(source,material);
                    }
                    if(changed==null)changed=(Material[])original.Clone();
                    changed[i]=material;
                }
                if(changed==null)continue;
                originals.Add(new RendererState{renderer=renderer,materials=original});
                renderer.sharedMaterials=changed;
            }
        }
        void Restore()
        {
            if(!active)return;
            foreach(var state in originals)if(state.renderer)state.renderer.sharedMaterials=state.materials;
            originals.Clear();
            if(mirrors)mirrors.SetSunsetLighting(false);
            // Do not write old scene settings into another circuit during a scene transition.
            if(gameObject.scene.IsValid()&&SceneManager.GetActiveScene()==gameObject.scene)
            {
                if(sun){sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=sunIntensity;}
                RenderSettings.skybox=originalSky;RenderSettings.fogColor=fogColor;
                RenderSettings.ambientSkyColor=skyAmbient;RenderSettings.ambientEquatorColor=equatorAmbient;
                RenderSettings.ambientGroundColor=groundAmbient;
                DynamicGI.UpdateEnvironment();
            }
            if(sunsetSky){Destroy(sunsetSky);sunsetSky=null;}
            foreach(var pair in sunsetMaterials)if(pair.Value)Destroy(pair.Value);
            sunsetMaterials.Clear();active=false;
        }
        void OnDisable(){Restore();}
    }
}
