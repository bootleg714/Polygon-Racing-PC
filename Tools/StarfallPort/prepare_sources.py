"""One-time Starfall Port source scaffold and shared circuit integration."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]; runtime=root/'Assets/PolygonRacing/Runtime'; editor=root/'Assets/PolygonRacing/Editor'
def modify(path,pairs):
    data=path.read_bytes()
    for old,new in pairs:
        a=old.encode();b=new.encode()
        if data.count(a)!=1:raise RuntimeError(f'{path}: expected one {old!r}, got {data.count(a)}')
        data=data.replace(a,b,1)
    path.write_bytes(data)
modify(runtime/'PolygonCircuit.cs',[
    ('LayoutName=>activeLayout==PolygonCircuitLayout.AuroraForge?', 'LayoutName=>activeLayout==PolygonCircuitLayout.StarfallPort?"Starfall Port":activeLayout==PolygonCircuitLayout.AuroraForge?'),
    ('LayoutId=>activeLayout==PolygonCircuitLayout.AuroraForge?', 'LayoutId=>activeLayout==PolygonCircuitLayout.StarfallPort?"StarfallPortV01":activeLayout==PolygonCircuitLayout.AuroraForge?'),
    ('BarrierMargin=>activeLayout==PolygonCircuitLayout.AuroraForge?', 'BarrierMargin=>activeLayout==PolygonCircuitLayout.StarfallPort?2f:activeLayout==PolygonCircuitLayout.AuroraForge?'),
    ('knots=layout==PolygonCircuitLayout.AuroraForge?', 'knots=layout==PolygonCircuitLayout.StarfallPort?starfallKnots:layout==PolygonCircuitLayout.AuroraForge?'),
    ('heights=layout==PolygonCircuitLayout.AuroraForge?', 'heights=layout==PolygonCircuitLayout.StarfallPort?starfallHeights:layout==PolygonCircuitLayout.AuroraForge?'),
    ('banks=layout==PolygonCircuitLayout.AuroraForge?', 'banks=layout==PolygonCircuitLayout.StarfallPort?starfallBanks:layout==PolygonCircuitLayout.AuroraForge?'),
    ('RoadHalfWidth=layout==PolygonCircuitLayout.AuroraForge?', 'RoadHalfWidth=layout==PolygonCircuitLayout.StarfallPort?10f:layout==PolygonCircuitLayout.AuroraForge?')])
modify(runtime/'PolygonRace.cs',[
    ('+(selection==9?"> ":"  ")+"BACK\\nSTICK', '+(selection==9?"> ":"  ")+"STARFALL PORT\\n"+(selection==10?"> ":"  ")+"BACK\\nSTICK'),
    ('int options=selectingRivieraLighting?3:selectingCircuit?10:3;', 'int options=selectingRivieraLighting?3:selectingCircuit?11:3;'),
    ('if(selection==9){selectingCircuit=false;', 'if(selection==10){selectingCircuit=false;'),
    ('string sceneName=layout==PolygonCircuitLayout.AuroraForge?', 'string sceneName=layout==PolygonCircuitLayout.StarfallPort?"StarfallPort":layout==PolygonCircuitLayout.AuroraForge?'),
    ('int count=selectingRivieraLighting?3:selectingCircuit?10:3;', 'int count=selectingRivieraLighting?3:selectingCircuit?11:3;'),
    ('if(ButtonAt(9,"BACK"))', 'if(ButtonAt(9,"STARFALL PORT"))LoadCircuit(PolygonCircuitLayout.StarfallPort);\n                if(ButtonAt(10,"BACK"))'),
    ('boardText.characterSize=selectingCircuit?.0082f:.009f', 'boardText.characterSize=selectingCircuit?.0077f:.009f')])
modify(runtime/'PolygonLook.cs',[(
    'if(layout==PolygonCircuitLayout.AuroraForge)',
    'if(layout==PolygonCircuitLayout.StarfallPort)\n                return new Preset{SunEuler=new Vector3(18,-70,0),SunColor=new Color(1,.72f,.63f),SunIntensity=.65f,Zenith=new Color(.035f,.025f,.12f),Horizon=new Color(.32f,.16f,.30f),BelowHorizon=new Color(.08f,.06f,.16f),WaterZenith=new Color(.04f,.09f,.20f),FogStart=550,FogEnd=2200,CloudRadius=1800,CloudSeed=151};\n            if(layout==PolygonCircuitLayout.AuroraForge)')])
modify(editor/'PolygonPCVisualSetup.cs',[(
    '        static bool ApplyToOpenScene(Profile profile)',
    '''        static readonly Profile StarfallPort = new Profile
        {
            Layout = PolygonCircuitLayout.StarfallPort, DisplayName = "Starfall Port",
            ScenePath = "Assets/PolygonRacing/Scenes/StarfallPort.unity",
            Folder = "Assets/PolygonRacing/Generated/PCVisuals/StarfallPort", FolderName = "StarfallPort",
            VolumeAssetName = "Starfall Port PC Volume Profile", SkyName = "Starfall Port PC Sky",
            VolumeObjectName = "PC Visual Profile - Starfall Port", BackupPrefix = "StarfallPort",
            SunColor = new Color(1,.72f,.63f), SunIntensity = .65f, ShadowStrength = .45f,
            Zenith = new Color(.035f,.025f,.12f), Horizon = new Color(.32f,.16f,.30f),
            BelowHorizon = new Color(.08f,.06f,.16f),
            AmbientSky = new Color(.43f,.42f,.58f), AmbientEquator = new Color(.37f,.29f,.39f),
            AmbientGround = new Color(.19f,.17f,.23f),
            FogColor = new Color(.32f,.16f,.30f), FogStart = 550, FogEnd = 2200,
            MaterialSky = new Color(.53f,.50f,.68f), MaterialGround = new Color(.27f,.22f,.30f),
            MaterialStrength = 1f, MaterialWrap = .18f,
            Exposure = .1f, Contrast = 6f, Saturation = 8f,
            BloomThreshold = .9f, BloomIntensity = .55f, BloomScatter = .6f,
            WhiteBalanceTemperature = 2f, Vignette = .06f, ExtraGrading = true
        };
        public static bool ApplyToOpenStarfallPortScene() => ApplyToOpenScene(StarfallPort);

        static bool ApplyToOpenScene(Profile profile)''')])
src=(runtime/'PolygonIonRiftArt.cs').read_text(encoding='utf-8')
for old,new in [('PolygonIonRiftArt','PolygonStarfallPortArt'),('PolygonIonRiftMotion','PolygonStarfallPortMotion'),('PolygonCircuitLayout.IonRift','PolygonCircuitLayout.StarfallPort'),('Ion Rift','Starfall Port'),('Ion canyon','Starfall sector'),('IR ','SP '),('ION RIFT','STARFALL PORT'),('Rim Station','Port Station'),('Reactor Gorge','Launch Tunnel'),('Ion extraction reactor','Starfall launch control'),('Canyon power tower','Spaceport control tower'),('Canyon sector','Spaceport sector'),('Canyon power','Spaceport power'),('RIFT VIADUCT','WATERFRONT'),('CLIFF ASCENT','DOCK APPROACH'),('REACTOR GORGE','LAUNCH TUNNEL'),('RIM STATION','PORT STATION')]:src=src.replace(old,new)
colors={'new Color(.28f,.23f,.30f)':'new Color(.20f,.24f,.33f)','new Color(.37f,.30f,.36f)':'new Color(.32f,.34f,.43f)','new Color(.18f,.17f,.24f)':'new Color(.12f,.15f,.23f)','new Color(.34f,.27f,.32f)':'new Color(.30f,.32f,.39f)','new Color(.24f,.17f,.29f)':'new Color(.25f,.21f,.34f)','new Color(.50f,.15f,1)':'new Color(1,.08f,.45f)'}
for old,new in colors.items():src=src.replace(old,new)
src=src.replace('BuildRoad(chunks);BuildTerrain(chunks);BuildCliffs(chunks);BuildStart(parent);BuildIndustry(chunks);BuildSignalTowers(parent,chunks);','BuildRoad(chunks);BuildTerrain(chunks);BuildStart(parent);BuildIndustry(chunks);BuildSignalTowers(parent,chunks);BuildSpaceport(parent,chunks);')
(runtime/'PolygonStarfallPortArt.cs').write_text(src,encoding='utf-8',newline='\n')
motion=(runtime/'PolygonIonRiftMotion.cs').read_text(encoding='utf-8').replace('PolygonIonRiftMotion','PolygonStarfallPortMotion').replace('PolygonIonRiftArt','PolygonStarfallPortArt')
(runtime/'PolygonStarfallPortMotion.cs').write_text(motion,encoding='utf-8',newline='\n')
setup=(editor/'PolygonIonRiftSetup.cs').read_text(encoding='utf-8')
for old,new in [('PolygonIonRiftSetup','PolygonStarfallPortSetup'),('PolygonIonRiftArt','PolygonStarfallPortArt'),('PolygonCircuitLayout.IonRift','PolygonCircuitLayout.StarfallPort'),('ApplyToOpenIonRiftScene','ApplyToOpenStarfallPortScene'),('IonRift','StarfallPort'),('Ion Rift','Starfall Port'),('17 - Create','19 - Create'),('ION_BUILD_OK','STARFALL_BUILD_OK')]:setup=setup.replace(old,new)
(editor/'PolygonStarfallPortSetup.cs').write_text(setup,encoding='utf-8',newline='\n')
print('Starfall source scaffold complete')
