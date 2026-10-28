"""Compile current game/editor sources using Unity's existing compiler references."""
from pathlib import Path
import subprocess,re,uuid,shutil
root=Path(__file__).resolve().parents[1]
out=root/'Temp/NeonChecks';out.mkdir(parents=True,exist_ok=True)
new_assets=['Runtime/PolygonNeonArt.cs','Runtime/PolygonNeonMotion.cs','Editor/PolygonNeonSetup.cs','Resources/PolygonNeonPanel.shader']
for relative in new_assets:
    path=root/'Assets/PolygonRacing'/relative
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        data='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
        if path.suffix=='.shader': data+='ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta.write_text(data,encoding='utf-8')
    dest=root/'Temp/NeonMeridianProbe/Assets/PolygonRacing'/relative
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest);shutil.copy2(meta,Path(str(dest)+'.meta'))
for relative in ['Runtime/PolygonCircuit.cs','Runtime/PolygonRace.cs','Runtime/PolygonLook.cs','Editor/PolygonPCVisualSetup.cs']:
    shutil.copy2(root/'Assets/PolygonRacing'/relative,root/'Temp/NeonMeridianProbe/Assets/PolygonRacing'/relative)

unity=Path('C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data')
dotnet=unity/'DotNetSdk/dotnet.exe';csc=unity/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
for name,editor in [('Assembly-CSharp',False),('Assembly-CSharp-Editor',True)]:
    source=root/f'Library/Bee/artifacts/1900b0aE.dag/{name}.rsp'
    lines=source.read_text(encoding='utf-8-sig').splitlines()
    result=[]
    for line in lines:
        if re.match(r'^"?Assets/.*\.cs"?$',line):continue
        if line.startswith('-out:'):line=f'-out:"{(out/(name+".dll")).as_posix()}"'
        if line.startswith('-refout:'):line=f'-refout:"{(out/(name+".ref.dll")).as_posix()}"'
        if editor and 'Assembly-CSharp' in line and line.startswith('-r:'):
            line=f'-r:"{(out/"Assembly-CSharp.dll").as_posix()}"'
        result.append(line)
    for path in (root/'Assets').rglob('*.cs'):
        if ('Editor' in path.parts)!=editor:continue
        result.append('"'+path.relative_to(root).as_posix()+'"')
    response=out/(name+'.rsp');response.write_text('\n'.join(result),encoding='utf-8')
    completed=subprocess.run([str(dotnet),str(csc),'@'+str(response),'/nowarn:0618,0672'],cwd=root,text=True,capture_output=True)
    print(completed.stdout);print(completed.stderr)
    if completed.returncode:raise SystemExit(completed.returncode)
    print(name+' compiled successfully')
