"""Initial source scaffolding for the Aurora Forge circuit."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
runtime=root/'Assets/PolygonRacing/Runtime'
editor=root/'Assets/PolygonRacing/Editor'
def modify(path,pairs):
    data=path.read_bytes()
    for old,new in pairs:
        a=old.encode();b=new.encode()
        if data.count(a)!=1:raise RuntimeError(f'{path}: expected one occurrence of {old!r}, got {data.count(a)}')
        data=data.replace(a,b,1)
    path.write_bytes(data)
modify(runtime/'PolygonCircuit.cs',[
    ('LayoutName=>activeLayout==PolygonCircuitLayout.IonRift?', 'LayoutName=>activeLayout==PolygonCircuitLayout.AuroraForge?"Aurora Forge":activeLayout==PolygonCircuitLayout.IonRift?'),
    ('LayoutId=>activeLayout==PolygonCircuitLayout.IonRift?', 'LayoutId=>activeLayout==PolygonCircuitLayout.AuroraForge?"AuroraForgeV01":activeLayout==PolygonCircuitLayout.IonRift?'),
    ('BarrierMargin=>activeLayout==PolygonCircuitLayout.IonRift?', 'BarrierMargin=>activeLayout==PolygonCircuitLayout.AuroraForge?2f:activeLayout==PolygonCircuitLayout.IonRift?'),
    ('knots=layout==PolygonCircuitLayout.IonRift?', 'knots=layout==PolygonCircuitLayout.AuroraForge?auroraKnots:layout==PolygonCircuitLayout.IonRift?'),
    ('heights=layout==PolygonCircuitLayout.IonRift?', 'heights=layout==PolygonCircuitLayout.AuroraForge?auroraHeights:layout==PolygonCircuitLayout.IonRift?'),
    ('banks=layout==PolygonCircuitLayout.IonRift?', 'banks=layout==PolygonCircuitLayout.AuroraForge?auroraBanks:layout==PolygonCircuitLayout.IonRift?'),
    ('RoadHalfWidth=layout==PolygonCircuitLayout.IonRift?', 'RoadHalfWidth=layout==PolygonCircuitLayout.AuroraForge?10f:layout==PolygonCircuitLayout.IonRift?')])
modify(runtime/'PolygonRace.cs',[
    ('+(selection==8?"> ":"  ")+"BACK\\nSTICK', '+(selection==8?"> ":"  ")+"AURORA FORGE\\n"+(selection==9?"> ":"  ")+"BACK\\nSTICK'),
    ('int options=selectingRivieraLighting?3:selectingCircuit?9:3;', 'int options=selectingRivieraLighting?3:selectingCircuit?10:3;'),
    ('if(selection==8){selectingCircuit=false;', 'if(selection==9){selectingCircuit=false;'),
    ('string sceneName=layout==PolygonCircuitLayout.IonRift?', 'string sceneName=layout==PolygonCircuitLayout.AuroraForge?"AuroraForge":layout==PolygonCircuitLayout.IonRift?'),
    ('int count=selectingRivieraLighting?3:selectingCircuit?9:3;', 'int count=selectingRivieraLighting?3:selectingCircuit?10:3;'),
    ('if(ButtonAt(8,"BACK"))', 'if(ButtonAt(8,"AURORA FORGE"))LoadCircuit(PolygonCircuitLayout.AuroraForge);\n                if(ButtonAt(9,"BACK"))'),
    ('string restart=mode==Mode.TimeTrial?', 'if(boardText)boardText.characterSize=selectingCircuit?.0082f:.009f;\n            string restart=mode==Mode.TimeTrial?')])
modify(runtime/'PolygonLook.cs',[(
    'if(layout==PolygonCircuitLayout.IonRift)',
    'if(layout==PolygonCircuitLayout.AuroraForge)\n                return new Preset{SunEuler=new Vector3(20,-55,0),SunColor=new Color(.65f,.8f,1),SunIntensity=.7f,Zenith=new Color(.012f,.035f,.10f),Horizon=new Color(.18f,.30f,.40f),BelowHorizon=new Color(.08f,.14f,.21f),WaterZenith=new Color(.04f,.16f,.25f),FogStart=500,FogEnd=2000,CloudRadius=1800,CloudSeed=131};\n            if(layout==PolygonCircuitLayout.IonRift)')])
pc=editor/'PolygonPCVisualSetup.cs'
modify(pc,[(
    '        static bool ApplyToOpenScene(Profile profile)',
    '''        static readonly Profile AuroraForge = new Profile
        {
            Layout = PolygonCircuitLayout.AuroraForge, DisplayName = "Aurora Forge",
            ScenePath = "Assets/PolygonRacing/Scenes/AuroraForge.unity",
            Folder = "Assets/PolygonRacing/Generated/PCVisuals/AuroraForge", FolderName = "AuroraForge",
            VolumeAssetName = "Aurora Forge PC Volume Profile", SkyName = "Aurora Forge PC Sky",
            VolumeObjectName = "PC Visual Profile - Aurora Forge", BackupPrefix = "AuroraForge",
            SunColor = new Color(.65f,.8f,1), SunIntensity = .7f, ShadowStrength = .45f,
            Zenith = new Color(.012f,.035f,.10f), Horizon = new Color(.18f,.30f,.40f),
            BelowHorizon = new Color(.08f,.14f,.21f),
            AmbientSky = new Color(.50f,.65f,.80f), AmbientEquator = new Color(.36f,.48f,.57f),
            AmbientGround = new Color(.25f,.32f,.40f),
            FogColor = new Color(.18f,.30f,.40f), FogStart = 500, FogEnd = 2000,
            MaterialSky = new Color(.68f,.78f,.90f), MaterialGround = new Color(.38f,.48f,.58f),
            MaterialStrength = 1f, MaterialWrap = .18f,
            Exposure = .1f, Contrast = 5f, Saturation = 8f,
            BloomThreshold = .9f, BloomIntensity = .5f, BloomScatter = .6f,
            WhiteBalanceTemperature = -4f, Vignette = .06f, ExtraGrading = true
        };
        public static bool ApplyToOpenAuroraForgeScene() => ApplyToOpenScene(AuroraForge);

        static bool ApplyToOpenScene(Profile profile)''')])
src=(runtime/'PolygonIonRiftArt.cs').read_text(encoding='utf-8')
for old,new in [('PolygonIonRiftArt','PolygonAuroraForgeArt'),('PolygonIonRiftMotion','PolygonAuroraForgeMotion'),('PolygonCircuitLayout.IonRift','PolygonCircuitLayout.AuroraForge'),('Ion Rift','Aurora Forge'),('Ion canyon','Aurora ice'),('IR ','AF '),('Ion start gantry','Aurora start gantry'),('ION RIFT','AURORA FORGE'),('Rim Station','Aurora Base'),('Reactor Gorge','Forge Cavern'),('Ion extraction reactor','Aurora Forge complex'),('Canyon power tower','Aurora power tower'),('Canyon sector','Glacier sector'),('Canyon power','Aurora power'),('RIFT VIADUCT','GLACIER BRIDGE'),('CLIFF ASCENT','ICE ASCENT'),('REACTOR GORGE','FORGE CAVERN'),('RIM STATION','AURORA BASE')]:src=src.replace(old,new)
colors={
'new Color(.28f,.23f,.30f)':'new Color(.64f,.79f,.88f)',
'new Color(.37f,.30f,.36f)':'new Color(.82f,.91f,.95f)',
'new Color(.18f,.17f,.24f)':'new Color(.30f,.48f,.60f)',
'new Color(.34f,.27f,.32f)':'new Color(.86f,.92f,.94f)',
'new Color(.24f,.17f,.29f)':'new Color(.22f,.34f,.43f)',
'new Color(.50f,.15f,1)':'new Color(.20f,.94f,.62f)',
'new Color(.025f,.035f,.055f)':'new Color(.035f,.065f,.09f)'}
for old,new in colors.items():src=src.replace(old,new)
(runtime/'PolygonAuroraForgeArt.cs').write_text(src,encoding='utf-8',newline='\n')
motion=(runtime/'PolygonIonRiftMotion.cs').read_text(encoding='utf-8').replace('PolygonIonRiftMotion','PolygonAuroraForgeMotion').replace('PolygonIonRiftArt','PolygonAuroraForgeArt')
(runtime/'PolygonAuroraForgeMotion.cs').write_text(motion,encoding='utf-8',newline='\n')
setup=(editor/'PolygonIonRiftSetup.cs').read_text(encoding='utf-8')
for old,new in [('PolygonIonRiftSetup','PolygonAuroraForgeSetup'),('PolygonIonRiftArt','PolygonAuroraForgeArt'),('PolygonCircuitLayout.IonRift','PolygonCircuitLayout.AuroraForge'),('ApplyToOpenIonRiftScene','ApplyToOpenAuroraForgeScene'),('IonRift','AuroraForge'),('Ion Rift','Aurora Forge'),('17 - Create','18 - Create'),('ION_BUILD_OK','AURORA_BUILD_OK')]:setup=setup.replace(old,new)
(editor/'PolygonAuroraForgeSetup.cs').write_text(setup,encoding='utf-8',newline='\n')
print('Aurora source scaffolding created')
