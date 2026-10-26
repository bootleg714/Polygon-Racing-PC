using UnityEngine;

namespace PolygonRacing
{
    // Shared look settings for the sky, light and water pass. Shaders live in
    // Assets/PolygonRacing/Resources so they are always included in Quest builds.
    public static class PolygonLook
    {
        public const string FlatShaderName="PolygonFlat",WaterShaderName="PolygonWater",
            SkyShaderName="PolygonSky",CloudShaderName="PolygonClouds";

        // Opaque scenery/car shader: the flat-lit polygon shader when present, else URP Simple Lit.
        public static Shader LitShader()
        {
            var shader=Resources.Load<Shader>(FlatShaderName);
            if(!shader)shader=Shader.Find("Universal Render Pipeline/Simple Lit");
            if(!shader)shader=Shader.Find("Standard");
            return shader;
        }

        public struct Preset
        {
            public Vector3 SunEuler;public Color SunColor;public float SunIntensity;
            public Color Zenith,Horizon,BelowHorizon,WaterZenith;
            public float FogStart,FogEnd,CloudRadius;public int CloudSeed;
        }

        public static Preset For(PolygonCircuitLayout layout)
        {
            if(layout==PolygonCircuitLayout.CoastalExpressway)
                return new Preset{
                    SunEuler=new Vector3(38,-35,0),SunColor=new Color(1,.94f,.82f),SunIntensity=1.3f,
                    Zenith=new Color(.16f,.46f,.82f),Horizon=new Color(.66f,.86f,.93f),
                    BelowHorizon=new Color(.40f,.70f,.80f),WaterZenith=new Color(.20f,.52f,.80f),
                    FogStart=380,FogEnd=1300,CloudRadius=1100,CloudSeed=11};
            if(layout==PolygonCircuitLayout.Riviera)
                // Bright Mediterranean afternoon: high sun, deep blue sky, pale aqua haze over the sea.
                return new Preset{
                    SunEuler=new Vector3(46,-120,0),SunColor=new Color(1,.95f,.84f),SunIntensity=1.3f,
                    Zenith=new Color(.10f,.36f,.78f),Horizon=new Color(.72f,.86f,.92f),
                    BelowHorizon=new Color(.30f,.55f,.72f),WaterZenith=new Color(.16f,.44f,.80f),
                    FogStart=420,FogEnd=1350,CloudRadius=1100,CloudSeed=53};
            if(layout==PolygonCircuitLayout.Nordwald)
                // Cool summer afternoon in the hills: soft high sun, pale grey-blue haze among the trees.
                return new Preset{
                    SunEuler=new Vector3(42,-150,0),SunColor=new Color(1,.95f,.88f),SunIntensity=1.22f,
                    Zenith=new Color(.28f,.47f,.74f),Horizon=new Color(.78f,.84f,.86f),
                    BelowHorizon=new Color(.46f,.56f,.52f),WaterZenith=new Color(.24f,.46f,.70f),
                    FogStart=300,FogEnd=1150,CloudRadius=1150,CloudSeed=89};
            if(layout==PolygonCircuitLayout.Sakura)
                // Clear spring morning: soft warm sun from the east, pale lilac-blue haze in the valley.
                return new Preset{
                    SunEuler=new Vector3(34,115,0),SunColor=new Color(1,.93f,.86f),SunIntensity=1.28f,
                    Zenith=new Color(.22f,.48f,.84f),Horizon=new Color(.84f,.86f,.93f),
                    BelowHorizon=new Color(.52f,.62f,.62f),WaterZenith=new Color(.24f,.52f,.80f),
                    FogStart=420,FogEnd=1500,CloudRadius=1250,CloudSeed=71};
            if(layout==PolygonCircuitLayout.Downtown)
                // Late-afternoon golden hour: low warm sun, peach haze between the towers.
                return new Preset{
                    SunEuler=new Vector3(26,-62,0),SunColor=new Color(1,.86f,.68f),SunIntensity=1.35f,
                    Zenith=new Color(.17f,.38f,.74f),Horizon=new Color(.93f,.80f,.68f),
                    BelowHorizon=new Color(.62f,.55f,.52f),WaterZenith=new Color(.25f,.45f,.75f),
                    FogStart=320,FogEnd=1150,CloudRadius=1100,CloudSeed=37};
            return new Preset{
                SunEuler=new Vector3(42,-35,0),SunColor=new Color(1,.94f,.84f),SunIntensity=1.25f,
                Zenith=new Color(.20f,.44f,.76f),Horizon=new Color(.70f,.84f,.88f),
                BelowHorizon=new Color(.50f,.66f,.62f),WaterZenith=new Color(.20f,.50f,.80f),
                FogStart=220,FogEnd=800,CloudRadius=650,CloudSeed=23};
        }
    }
}
