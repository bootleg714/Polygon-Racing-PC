"""Copy the validated new circuit from the isolated Unity project into this workspace."""
from pathlib import Path
import datetime,hashlib,re,shutil,subprocess,json

root=Path(__file__).resolve().parents[2]
probe=root/'Temp/NeonMeridianProbe'
report=(probe/'Inspection/report.txt').read_text()
play_report=(probe/'Inspection/play-mode-report.txt').read_text()
assert 'Road corridor obstructions: 0' in report
assert '38 checks passed' in play_report

protected=[root/'Assets/PolygonRacing/Scenes'/name for name in ['ForestCircuit.unity','CoastalExpressway.unity','DowntownCircuit.unity','RivieraHarbour.unity','SakuraHills.unity','NordwaldRing.unity']]
protected.extend([root/'Assets/Resources/PolygonRivalPace.asset',root/'Assets/Settings/PC_RPAsset.asset'])
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
before={str(p.relative_to(root)):digest(p) for p in protected if p.exists()}

backup=root/'PolygonRacingBackups'/('NeonMeridian_before_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S'))
backup.mkdir(parents=True,exist_ok=False)
for path in ['ProjectSettings/EditorBuildSettings.asset','POLYGON_RACING_HANDOFF3.md']:
    target=backup/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/path,target)
for path in ['Assets/PolygonRacing/Runtime/PolygonCircuit.cs','Assets/PolygonRacing/Runtime/PolygonRace.cs','Assets/PolygonRacing/Runtime/PolygonLook.cs','Assets/PolygonRacing/Editor/PolygonPCVisualSetup.cs']:
    target=backup/path;target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(subprocess.check_output(['git','show','HEAD:'+path],cwd=root))

folders=['Assets/PolygonRacing/Generated/Neon','Assets/PolygonRacing/Generated/PCVisuals/Neon']
for relative in folders:
    source=probe/relative;target=root/relative
    if target.exists():raise RuntimeError('Refusing to overwrite an already installed generated folder: '+str(target))
    shutil.copytree(source,target)
    shutil.copy2(Path(str(source)+'.meta'),Path(str(target)+'.meta'))
relative='Assets/PolygonRacing/Scenes/NeonMeridian.unity'
target=root/relative
if target.exists():raise RuntimeError('Refusing to overwrite an already installed Neon scene')
shutil.copy2(probe/relative,target);shutil.copy2(Path(str(probe/relative)+'.meta'),Path(str(target)+'.meta'))
guid=re.search(r'guid:\s*([0-9a-f]{32})',Path(str(target)+'.meta').read_text())[1]
settings=root/'ProjectSettings/EditorBuildSettings.asset'
text=settings.read_text(encoding='utf-8-sig')
assert 'NeonMeridian.unity' not in text
text=text.replace('  m_configObjects:',f'  - enabled: 1\n    path: {relative}\n    guid: {guid}\n  m_configObjects:',1)
settings.write_text(text,encoding='utf-8',newline='\n')
previews=root/'Tools/NeonMeridian/Previews';previews.mkdir(exist_ok=True)
for path in (probe/'Inspection').glob('*'):
    if path.is_file():shutil.copy2(path,previews/path.name)
after={str(p.relative_to(root)):digest(p) for p in protected if p.exists()}
assert before==after,'Existing circuits, rival tuning or renderer settings changed'
(backup/'protected-sha256.json').write_text(json.dumps(before,indent=2))
print('Installed new scene, meshes, night materials and previews.')
print('Enabled Neon Meridian as the seventh build scene.')
print('Existing six scenes, rival tuning and PC renderer hashes unchanged.')
print('Backup:',backup.relative_to(root))
