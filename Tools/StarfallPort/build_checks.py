"""Compile and copy Starfall Port into the isolated Unity test project."""
from pathlib import Path
import runpy,shutil,uuid
root=Path(__file__).resolve().parents[2];probe=root/'Temp/NeonMeridianProbe'
for relative in ['Runtime/PolygonStarfallPortArt.cs','Runtime/PolygonStarfallPortMotion.cs','Runtime/PolygonStarfallShuttle.cs','Editor/PolygonStarfallPortSetup.cs']:
    source=root/'Assets/PolygonRacing'/relative;meta=Path(str(source)+'.meta')
    if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8',newline='\n')
    target=probe/'Assets/PolygonRacing'/relative;target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target);shutil.copy2(meta,Path(str(target)+'.meta'))
for name in ['Probe','PlayProbe']:
    src=(root/f'Tools/IonRift/IonRift{name}.cs').read_text(encoding='utf-8')
    for old,new in [('IonRift','StarfallPort'),('Ion Rift','Starfall Port'),('ION RIFT','STARFALL PORT'),('ION_','STARFALL_'),('Twelve-line','Fourteen-line')]:src=src.replace(old,new)
    if name=='Probe':src=src.replace('(int)PolygonCircuitLayout.StarfallPort==7','(int)PolygonCircuitLayout.IonRift==7&&(int)PolygonCircuitLayout.AuroraForge==8&&(int)PolygonCircuitLayout.StarfallPort==9')
    if name=='PlayProbe':
        src=src.replace('static PolygonRace race;','static PolygonRace race;\n    static readonly Dictionary<Transform,Vector3> shuttleStart=new Dictionary<Transform,Vector3>();')
        src=src.replace('InspectRotors();Next(2);','InspectRotors();foreach(var shuttle in Object.FindObjectsByType<PolygonStarfallShuttle>()){Check(shuttle.transform.parent.name==PolygonStarfallPortArt.AnimatedName,"Shuttle is still in static scenery");shuttleStart.Add(shuttle.transform,shuttle.transform.position);}Check(shuttleStart.Count==2,"Expected two cargo shuttles");Next(2);')
        src=src.replace('var player=Object.FindFirstObjectByType<PolygonDriver>();\n                typeof(PolygonDriver).GetField("paused",Private).SetValue(player,true);','foreach(var shuttle in shuttleStart)Check(Vector3.Distance(shuttle.Key.position,shuttle.Value)>5,"Cargo shuttle did not move");\n                var player=Object.FindFirstObjectByType<PolygonDriver>();\n                typeof(PolygonDriver).GetField("paused",Private).SetValue(player,true);')
    target=probe/f'Assets/Editor/StarfallPort{name}.cs';target.write_text(src,encoding='utf-8',newline='\n')
    (root/f'Tools/StarfallPort/StarfallPort{name}.cs').write_text(src,encoding='utf-8',newline='\n')
runpy.run_path(str(root/'Tools/check_neon_source.py'),run_name='__main__')
