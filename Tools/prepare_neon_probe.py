"""Prepare an isolated Unity copy for native track generation and checks.

Keeps asset GUIDs and copies only the Forest template's dependency closure, sources,
resources and settings. Packages resolve from the existing local Unity package cache.
"""
from pathlib import Path
import json, re, shutil

root=Path(__file__).resolve().parents[1]
probe=root/'Temp'/'NeonMeridianProbe'
probe.mkdir(parents=True,exist_ok=True)
guid_pattern=re.compile(r'guid:\s*([0-9a-f]{32})')
guid_paths={}
for meta in (root/'Assets').rglob('*.meta'):
    match=guid_pattern.search(meta.read_text(encoding='utf-8',errors='ignore'))
    if match: guid_paths[match.group(1)]=Path(str(meta)[:-5])

queue=[root/'Assets/PolygonRacing/Scenes/ForestCircuit.unity']
for folder in ['Runtime','Editor','Resources']:
    queue.extend(p for p in (root/'Assets/PolygonRacing'/folder).rglob('*') if p.is_file() and p.suffix!='.meta')
for p in (root/'ProjectSettings').glob('*'):
    if p.is_file(): queue.append(p)
copied=set()
while queue:
    source=queue.pop()
    if source in copied or not source.is_file(): continue
    copied.add(source)
    target=probe/source.relative_to(root)
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target)
    meta=Path(str(source)+'.meta')
    if meta.is_file(): shutil.copy2(meta,Path(str(target)+'.meta'))
    if source.suffix.lower() not in {'.png','.jpg','.wav','.dll','.fbx'}:
        data=source.read_text(encoding='utf-8',errors='ignore')
        queue.extend(guid_paths[g] for g in guid_pattern.findall(data) if g in guid_paths)

# Parent folder GUIDs are preserved too, avoiding inconsistencies when results return.
for target in (probe/'Assets').rglob('*'):
    if target.is_dir():
        original_meta=Path(str(root/target.relative_to(probe))+'.meta')
        if original_meta.is_file(): shutil.copy2(original_meta,Path(str(target)+'.meta'))

packages={}
excluded=('com.unity.ide.','com.unity.collab-','com.unity.visualscripting','com.unity.ai.navigation',
          'com.unity.timeline','com.unity.test-framework.performance','com.unity.graph-authoring')
for package in (root/'Library/PackageCache').iterdir():
    manifest=package/'package.json'
    if not manifest.is_file(): continue
    info=json.loads(manifest.read_text(encoding='utf-8-sig'))
    name=info['name']
    if name.startswith('com.unity.modules.'):
        packages[name]='1.0.0'
    elif not name.startswith(excluded):
        packages[name]='file:'+package.as_posix()
(probe/'Packages').mkdir(exist_ok=True)
(probe/'Packages/manifest.json').write_text(json.dumps({'dependencies':packages},indent=2),encoding='utf-8')
print(f'Prepared {probe}: {len(copied)} source/dependency files, {len(packages)} local packages')
