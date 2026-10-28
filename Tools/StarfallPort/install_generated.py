"""Install the natively verified Starfall Port scene and generated assets."""
from pathlib import Path
import hashlib,json,re,shutil
root=Path(__file__).resolve().parents[2];probe=root/'Temp/NeonMeridianProbe';inspection=probe/'StarfallPortInspection'
report=(inspection/'report.txt').read_text(encoding='utf-8');play=(inspection/'play-mode-report.txt').read_text(encoding='utf-8')
for expected in ['Road corridor obstructions: 0','Road surface holes: 0','Buried road points: 0']:
    if expected not in report:raise RuntimeError('Missing passing geometry result: '+expected)
if 'Game/engine errors: 0' not in play:raise RuntimeError('Play-mode checks have not passed')
folders=['Assets/PolygonRacing/Generated/StarfallPort','Assets/PolygonRacing/Generated/PCVisuals/StarfallPort'];scene='Assets/PolygonRacing/Scenes/StarfallPort.unity'
for path in folders+[scene]:
    if (root/path).exists():raise RuntimeError('Refusing to overwrite an existing target: '+path)
    if not (probe/path).exists():raise RuntimeError('Missing generated output: '+path)
settings=root/'ProjectSettings/EditorBuildSettings.asset'
if 'StarfallPort.unity' in settings.read_text(encoding='utf-8-sig'):raise RuntimeError('Scene already listed')
protected=[root/'Assets/PolygonRacing/Scenes'/name for name in ['ForestCircuit.unity','CoastalExpressway.unity','DowntownCircuit.unity','RivieraHarbour.unity','SakuraHills.unity','NordwaldRing.unity','NeonMeridian.unity','IonRift.unity','AuroraForge.unity']]
protected.extend([root/'Assets/Resources/PolygonRivalPace.asset',root/'Assets/Settings/PC_RPAsset.asset'])
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
before={p.relative_to(root).as_posix():digest(p) for p in protected if p.exists()}
for relative in folders:
    source=probe/relative;target=root/relative;shutil.copytree(source,target);shutil.copy2(Path(str(source)+'.meta'),Path(str(target)+'.meta'))
source=probe/scene;target=root/scene;shutil.copy2(source,target);shutil.copy2(Path(str(source)+'.meta'),Path(str(target)+'.meta'))
guid=re.search(r'guid:\s*([0-9a-f]{32})',Path(str(target)+'.meta').read_text())[1];current=settings.read_text(encoding='utf-8-sig')
if 'StarfallPort.unity' in current or '  m_configObjects:' not in current:raise RuntimeError('Scene settings changed during installation')
current=current.replace('  m_configObjects:',f'  - enabled: 1\n    path: {scene}\n    guid: {guid}\n  m_configObjects:',1);settings.write_text(current,encoding='utf-8',newline='\n')
previews=root/'Tools/StarfallPort/Previews';previews.mkdir(exist_ok=True)
names={'00_Canyon_Overview.png':'00_Port_Overview.png','01_Rim_Station.png':'01_Port_Station.png','02_Cliff_Ascent.png':'02_Dock_Approach.png','03_High_Overlook.png':'03_Launch_Overlook.png','04_Canyon_Plunge.png':'04_Waterfront_Descent.png','05_Reactor_Gorge.png':'05_Launch_Tunnel.png','06_Return_Bridge.png':'06_Return_Bridge.png'}
for item in inspection.iterdir():
    if item.is_file():shutil.copy2(item,previews/names.get(item.name,item.name))
after={p.relative_to(root).as_posix():digest(p) for p in protected if p.exists()};(root/'Tools/StarfallPort/protected-sha256.json').write_text(json.dumps({'before':before,'after':after},indent=2)+'\n')
if before!=after:print('NOTE: A protected file changed during installation; review recorded hashes.')
else:print('Existing nine scenes, rival tuning and PC renderer hashes are unchanged.')
print('Installed Starfall Port and enabled it as the tenth build scene.')
