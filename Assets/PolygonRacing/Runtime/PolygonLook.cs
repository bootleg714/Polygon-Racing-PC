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
            if(layout==PolygonCircuitLayout.AuroraForge)
                return new Preset{SunEuler=new Vector3(20,-55,0),SunColor=new Color(.65f,.8f,1),SunIntensity=.7f,Zenith=new Color(.012f,.035f,.10f),Horizon=new Color(.18f,.30f,.40f),BelowHorizon=new Color(.08f,.14f,.21f),WaterZenith=new Color(.04f,.16f,.25f),FogStart=500,FogEnd=2000,CloudRadius=1800,CloudSeed=131};
            if(layout==PolygonCircuitLayout.IonRift)
                return new Preset{
                    SunEuler=new Vector3(18,-65,0),SunColor=new Color(.95f,.67f,.56f),SunIntensity=.65f,
                    Zenith=new Color(.025f,.035f,.10f),Horizon=new Color(.24f,.14f,.30f),
                    BelowHorizon=new Color(.07f,.045f,.11f),WaterZenith=new Color(.025f,.12f,.18f),
                    FogStart=650,FogEnd=2300,CloudRadius=2200,CloudSeed=131};
            if(layout==PolygonCircuitLayout.NeonMeridian)
                return new Preset{
                    SunEuler=new Vector3(24,-48,0),SunColor=new Color(.45f,.58f,1),SunIntensity=.35f,
                    Zenith=new Color(.012f,.018f,.065f),Horizon=new Color(.12f,.075f,.23f),
                    BelowHorizon=new Color(.025f,.035f,.075f),WaterZenith=new Color(.02f,.06f,.14f),
                    FogStart=500,FogEnd=1900,CloudRadius=1800,CloudSeed=107};
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
