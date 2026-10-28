"""Independent route/elevation sanity checks using the existing simulation's spline port."""
from pathlib import Path
import json,re,sys,subprocess
import numpy as np

root=Path(__file__).resolve().parents[2]
sys.dont_write_bytecode=True
sys.path.insert(0,str(root/'Tools/RivalSim'))
import track
track.HALF_WIDTH['neon']=10
route=track.Track('neon')
source=(root/'Assets/PolygonRacing/Runtime/PolygonCircuit.cs').read_text()
heights=np.array([float(x) for x in re.findall(r'-?\d+(?:\.\d+)?',re.search(r'neonHeights=\{(.*?)\}',source,re.S)[1])])
assert len(heights)==len(route.knots)
def elevation(d):
    u=route.angle_at(d)/(2*np.pi)*len(heights);i=int(u);t=u-i;n=len(heights)
    a,b,c,e=[heights[k%n] for k in (i-1,i,i+1,i+2)]
    return (a*(1-3*t+3*t*t-t**3)+b*(3*t**3-6*t*t+4)+c*(-3*t**3+3*t*t+3*t+1)+e*t**3)/6

distance=np.arange(0,route.length,2)
altitude=np.array([elevation(d) for d in distance])
grade=np.array([(elevation(d+1)-elevation(d-1))/2 for d in distance])
curvature=np.array([route.curvature(d) for d in distance])
assert np.max(np.abs(grade))<.2
assert abs(elevation(.01)-elevation(route.length-.01))<.01

# Check all nonlocal centerline samples for overlaps, allowing 70 m along the track.
points=route.points[:-1];closest=float('inf')
for i,p in enumerate(points):
    arc=np.abs(route.dist[:-1]-route.dist[i]);arc=np.minimum(arc,route.length-arc)
    candidates=np.linalg.norm(points-p,axis=1)[arc>70]
    closest=min(closest,float(candidates.min()))
assert closest>2*route.half_width+10,closest

# Existing route control points and height/bank arrays must remain byte-for-byte identical.
original=subprocess.check_output(['git','show','HEAD:Assets/PolygonRacing/Runtime/PolygonCircuit.cs'],cwd=root,text=True)
for name in ['forestKnots','coastalKnots','downtownKnots','rivieraKnots','rivieraHeights','sakuraKnots','sakuraHeights','nordwaldKnots','nordwaldHeights','nordwaldBanks']:
    pattern=re.escape(name)+r'\s*=\s*\{.*?\};'
    assert re.search(pattern,original,re.S)[0]==re.search(pattern,source,re.S)[0],name+' changed'
result={'length_m':round(route.length,2),'min_height_m':round(float(altitude.min()),2),'max_height_m':round(float(altitude.max()),2),
        'max_grade_percent':round(float(np.max(np.abs(grade)))*100,2),'minimum_corner_radius_m':round(float(1/np.max(np.abs(curvature))),2),
        'minimum_nonlocal_centerline_gap_m':round(closest,2),'existing_layout_arrays_preserved':True}
print(json.dumps(result,indent=2))
(Path(__file__).parent/'route-analysis.json').write_text(json.dumps(result,indent=2)+'\n')
