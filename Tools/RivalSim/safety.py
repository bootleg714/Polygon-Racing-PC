import sys; sys.path.insert(0,'.')
from multiprocessing import Pool
from track import Track, LAYOUTS
from sim import Plan, World, DT, NewRival
import math
CFG=dict(top=1.01,accel=.98,neutral_s=.8,full_s=3.5,catch=.04,ease=.07,response=4)
def check(n):
    t=Track(n); p=Plan(t); worst_lat=-99; overlap=0; frames=0; minv=99; nan=False
    for seed in range(2):
        w=World(t,p,"new",dict(pace=.98,line=.6,lapse_rate=.02),CFG,seed=seed)
        # run manually to inspect each frame
        while w.time<600 and w.player.finish is None:
            for c in w.cars:
                c.draft=0
            for c in w.cars:
                if c.finish is None: c.step(w,DT)
            w.time+=DT
            for c in w.cars:
                if c.finish is None and c.s>=3*t.length: c.finish=w.time
            frames+=1
            rs=[r for r in w.rivals if r.finish is None]
            for r in rs:
                worst_lat=max(worst_lat,abs(r.lat)-t.half_width)
                if any(map(math.isnan,(r.s,r.lat,r.v))): nan=True
                if w.time>10: minv=min(minv,r.v)
            for i in range(len(rs)):
                for j in range(i+1,len(rs)):
                    if abs(rs[i].s-rs[j].s)<4.2 and abs(rs[i].lat-rs[j].lat)<1.9: overlap+=1
    return f"{n:9s} rival edge margin {(-worst_lat):5.2f} m inside road | rival-rival overlap frames {overlap}/{frames} | min speed after start {minv*3.6:5.0f} km/h | nan {nan}"
if __name__=="__main__":  # guard needed for multiprocessing on Windows
    with Pool(2) as pl:
        for line in pl.map(check,LAYOUTS): print(line)
