"""Compile and copy Aurora Forge into the isolated Unity test project."""
from pathlib import Path
import runpy,shutil,uuid
root=Path(__file__).resolve().parents[2]
probe=root/'Temp/NeonMeridianProbe'
for relative in ['Runtime/PolygonAuroraForgeArt.cs','Runtime/PolygonAuroraForgeMotion.cs','Editor/PolygonAuroraForgeSetup.cs','Resources/PolygonAuroraRibbon.shader']:
    source=root/'Assets/PolygonRacing'/relative;meta=Path(str(source)+'.meta')
    if not meta.exists():
        data='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
        if source.suffix=='.shader':data+='ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta.write_text(data,encoding='utf-8',newline='\n')
    target=probe/'Assets/PolygonRacing'/relative;target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target);shutil.copy2(meta,Path(str(target)+'.meta'))
for name in ['Probe','PlayProbe']:
    src=(root/f'Tools/IonRift/IonRift{name}.cs').read_text(encoding='utf-8')
    for old,new in [('IonRift','AuroraForge'),('Ion Rift','Aurora Forge'),('ION RIFT','AURORA FORGE'),('ION_','AURORA_'),('Twelve-line','Thirteen-line')]:src=src.replace(old,new)
    if name=='Probe':src=src.replace('(int)PolygonCircuitLayout.AuroraForge==7','(int)PolygonCircuitLayout.IonRift==7&&(int)PolygonCircuitLayout.AuroraForge==8')
    target=probe/f'Assets/Editor/AuroraForge{name}.cs';target.write_text(src,encoding='utf-8',newline='\n')
    (root/f'Tools/AuroraForge/AuroraForge{name}.cs').write_text(src,encoding='utf-8',newline='\n')
runpy.run_path(str(root/'Tools/check_neon_source.py'),run_name='__main__')
