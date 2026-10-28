"""Independent numerical route check and preservation audit for existing circuits."""
from pathlib import Path
import json,math,re,sys,hashlib
import numpy as np
root=Path(__file__).resolve().parents[2]
sys.dont_write_bytecode=True
sys.path.insert(0,str(root/'Tools/RivalSim'))
import track
track.HALF_WIDTH['aurora']=10
route=track.Track('aurora')
source=(root/'Assets/PolygonRacing/Runtime/PolygonCircuit.cs').read_text(encoding='utf-8')
baseline=json.loads((root/'Tools/AuroraForge/prior-route-hashes.json').read_text(encoding='utf-8'))
for name,expected in baseline.items():
    pattern=re.escape(name)+r'\s*=\s*\{.*?\};'
    assert hashlib.sha256(re.search(pattern,source,re.S)[0].encode()).hexdigest()==expected,name+' changed'
h=np.array([float(x) for x in re.findall(r'-?\d+(?:\.\d+)?',re.search(r'auroraHeights=\{(.*?)\}',source,re.S)[1])])
assert len(h)==len(route.knots)
def elevation(d):
    u=route.angle_at(d)/(2*math.pi)*len(h);i=int(u);t=u-i;n=len(h)
    a,b,c,e=[h[j%n] for j in (i-1,i,i+1,i+2)]
    return (a*(1-3*t+3*t*t-t**3)+b*(3*t**3-6*t*t+4)+c*(-3*t**3+3*t*t+3*t+1)+e*t**3)/6
distances=np.arange(0,route.length,2)
altitudes=np.array([elevation(d) for d in distances]);grades=np.array([(elevation(d+1)-elevation(d-1))/2 for d in distances])
curvatures=np.array([route.curvature(d) for d in distances]);nearest=float('inf')
for i,p in enumerate(route.points[:-1]):
    delta=np.abs(route.dist[:-1]-route.dist[i]);delta=np.minimum(delta,route.length-delta)
    nearest=min(nearest,float(np.linalg.norm(route.points[:-1]-p,axis=1)[delta>75].min()))
assert abs(elevation(.01)-elevation(route.length-.01))<.01
assert max(abs(grades))<.2 and nearest>30
result={'length_m':round(route.length,2),'minimum_height_m':round(float(min(altitudes)),2),'maximum_height_m':round(float(max(altitudes)),2),
        'maximum_grade_percent':round(float(max(abs(grades)))*100,2),'minimum_corner_radius_m':round(float(1/max(abs(curvatures))),2),
        'minimum_nonlocal_centerline_gap_m':round(nearest,2),'other_route_arrays_preserved':True}
(Path(__file__).parent/'route-analysis.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
