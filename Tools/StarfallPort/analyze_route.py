"""Independent spline and existing-route preservation checks."""
from pathlib import Path
import json,math,re,sys,hashlib
import numpy as np
root=Path(__file__).resolve().parents[2];sys.dont_write_bytecode=True
sys.path.insert(0,str(root/'Tools/RivalSim'));import track
track.HALF_WIDTH['starfall']=10;route=track.Track('starfall')
source=(root/'Assets/PolygonRacing/Runtime/PolygonCircuit.cs').read_text(encoding='utf-8')
baseline=json.loads((root/'Tools/StarfallPort/prior-route-hashes.json').read_text(encoding='utf-8'))
for name,expected in baseline.items():
    pattern=re.escape(name)+r'\s*=\s*\{.*?\};'
    assert hashlib.sha256(re.search(pattern,source,re.S)[0].encode()).hexdigest()==expected,name+' changed'
h=np.array([float(x) for x in re.findall(r'-?\d+(?:\.\d+)?',re.search(r'starfallHeights=\{(.*?)\}',source,re.S)[1])]);assert len(h)==len(route.knots)
def elevation(d):
    u=route.angle_at(d)/(2*math.pi)*len(h);i=int(u);t=u-i;n=len(h);a,b,c,e=[h[j%n] for j in (i-1,i,i+1,i+2)]
    return (a*(1-3*t+3*t*t-t**3)+b*(3*t**3-6*t*t+4)+c*(-3*t**3+3*t*t+3*t+1)+e*t**3)/6
d=np.arange(0,route.length,2);heights=np.array([elevation(x) for x in d]);grades=np.array([(elevation(x+1)-elevation(x-1))/2 for x in d]);curvatures=np.array([route.curvature(x) for x in d]);nearest=float('inf')
for i,p in enumerate(route.points[:-1]):
    delta=np.abs(route.dist[:-1]-route.dist[i]);delta=np.minimum(delta,route.length-delta)
    nearest=min(nearest,float(np.linalg.norm(route.points[:-1]-p,axis=1)[delta>75].min()))
assert abs(elevation(.01)-elevation(route.length-.01))<.01 and max(abs(grades))<.2 and nearest>30
result={'length_m':round(route.length,2),'minimum_height_m':round(float(min(heights)),2),'maximum_height_m':round(float(max(heights)),2),'maximum_grade_percent':round(float(max(abs(grades)))*100,2),'minimum_corner_radius_m':round(float(1/max(abs(curvatures))),2),'minimum_nonlocal_centerline_gap_m':round(nearest,2),'other_route_arrays_preserved':True}
(Path(__file__).parent/'route-analysis.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result,indent=2))
