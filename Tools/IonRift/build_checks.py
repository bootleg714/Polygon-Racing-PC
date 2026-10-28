"""Compile project sources and refresh Ion Rift's isolated native-check inputs."""
from pathlib import Path
import runpy,shutil,uuid

root=Path(__file__).resolve().parents[2]
probe=root/'Temp/NeonMeridianProbe'
if not (probe/'ProjectSettings/ProjectVersion.txt').exists():
    raise SystemExit('Run Tools/prepare_neon_probe.py first to prepare the isolated Unity project.')
for relative in ['Runtime/PolygonIonRiftArt.cs','Runtime/PolygonIonRiftMotion.cs','Editor/PolygonIonRiftSetup.cs','Resources/PolygonIonEnergy.shader']:
    source=root/'Assets/PolygonRacing'/relative;meta=Path(str(source)+'.meta')
    if not meta.exists():
        data='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
        if source.suffix=='.shader':
            data+='ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta.write_text(data,encoding='utf-8',newline='\n')
    target=probe/'Assets/PolygonRacing'/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target);shutil.copy2(meta,Path(str(target)+'.meta'))
for name in ['IonRiftProbe.cs','IonRiftPlayProbe.cs']:
    shutil.copy2(root/'Tools/IonRift'/name,probe/'Assets/Editor'/name)
runpy.run_path(str(root/'Tools/check_neon_source.py'),run_name='__main__')
print('Ion Rift sources and native-check runners copied to the isolated Unity project.')
