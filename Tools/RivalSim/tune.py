import sys, statistics as st, json
from multiprocessing import Pool
sys.path.insert(0,'.')
from track import Track
from sim import Plan, World
PROFILES={"casual":dict(pace=.955,line=.4,lapse_rate=.03),"average":dict(pace=.975,line=.6,lapse_rate=.02),
          "good":dict(pace=.99,line=.85,lapse_rate=.01),"expert":dict(pace=1.005,line=1.0)}
_cache={}
def plan(n):
    if n not in _cache: t=Track(n); _cache[n]=(t,Plan(t))
    return _cache[n]
def one(args):
    ai,cfg,n,prof,seed=args; t,p=plan(n)
    r=World(t,p,ai,PROFILES[prof],cfg,seed=seed).run()
    near=min(abs(g) for g in r['gaps'])
    return prof,r['place'],near,r['player_changes'],r['rival_changes']
def evaluate(ai,cfg,tracks=("forest","riviera","coastal","nordwald"),seeds=3):
    jobs=[(ai,cfg,n,pr,s) for n in tracks for pr in PROFILES for s in range(seeds)]
    with Pool(2) as pool: out=pool.map(one,jobs)
    res={}
    for pr in PROFILES:
        rows=[o for o in out if o[0]==pr]
        res[pr]=dict(place=st.mean(o[1] for o in rows),places=sorted(o[1] for o in rows),near=st.median(o[2] for o in rows),
                     ppass=st.mean(o[3] for o in rows),rpass=st.mean(o[4] for o in rows))
    return res
def show(name,res):
    print("--",name)
    for pr,r in res.items():
        print(f"  {pr:8s} avg place {r['place']:.2f} {r['places']}  nearest rival at finish {r['near']:4.1f}s  passes w/ player {r['ppass']:4.1f}  rival-rival {r['rpass']:4.1f}")
if __name__=="__main__":
    # Defaults mirror PolygonRivalPaceSettings / the saved PolygonRivalPace.asset
    base=dict(top=1.01,accel=.98,neutral_s=.8,full_s=3.5,catch=.04,ease=.07,response=4)
    configs={"old":("old",dict(neutral_m=40,full_m=220,catch=.08,ease=.04))}
    for spec in sys.argv[1:]:
        c=dict(base); c.update(json.loads(spec)); configs[spec]=("new",c)
    for k,(ai,cfg) in configs.items(): show(k,evaluate(ai,cfg))
