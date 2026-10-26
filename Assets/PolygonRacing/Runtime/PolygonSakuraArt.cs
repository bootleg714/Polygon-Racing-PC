using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // "POLYGON SAKURA": a figure-eight circuit in rolling spring countryside, inspired by the classic
    // Japanese figure-eight (the downhill run passes under the back straight on a bridge). Original
    // layout, buildings and landmarks: amusement park with a turning Ferris wheel behind the main
    // grandstand, cherry trees, a lake with a gate standing in the water, a pagoda on the hill,
    // terraced paddies, pine forests and mountains. Editor-time generation, baked per material into
    // spatial chunks like the other circuits.
    //
    // Cross-section (metres from the racing line, both sides; heights follow the road elevation):
    //   0-9 road (edge line 8.6-9) | 9-10.4 kerb at corners | 10.4-15.5 run-off (gravel on the outside
    //   of corners) | 15.5-15.8 steel barrier with tyre stack (driver limit 9 + 6.5 = 15.5) |
    //   16.3-16.9 retaining wall where the road stands above the ground | terrain beyond.
    public static class PolygonSakuraArt
    {
        public const string SceneryName="Circuit - editable scenery";
        public const string AnimatedName="Sakura animated rides";
        const float Road=9f,Kerb=10.4f,Barrier0=15.5f,Barrier1=15.8f,Wall0=16.3f,Wall1=16.9f,ChunkSize=300;
        const float BridgeHalfSpan=30f,AbutmentOffset=19.5f,DeckHalfWidth=17.2f;

        static readonly Vector2 LakeCentre=new Vector2(-418,330);
        const float LakeRx=62,LakeRz=56,LakeLevel=7.2f;
        static readonly Vector2 FerrisSite=new Vector2(565,-52);
        static readonly Vector2 ParkCentre=new Vector2(585,-95),ParkHalf=new Vector2(150,72);
        static bool InPark(float x,float z)=>Mathf.Abs(x-ParkCentre.x)<ParkHalf.x&&Mathf.Abs(z-ParkCentre.y)<ParkHalf.y;
        static readonly Vector2 PagodaSite=new Vector2(-40,660);

        static Material asphalt,line,kerbRed,gravel,runoff,grass,grassDark,meadow,rock,dirt,concrete,wallStone,armco,tyreBlack,tyreRed,
            fence,cream,white,charcoal,vermilion,gold,teal,navy,coral,ochre,sakura,sakuraPale,sakuraDeep,pine,pineDark,bark,lakeWater,
            paddy,paddyFlooded,glassDark,steel,lampGlow,skinLight,skinTan,skinDeep,hair,mountain,mountainSnow,roofGrey,plaster,timber,
            lilac,lime,sky,plaza,plazaWarm;
        static Material[] teamColors,shirts,skins,gondolaColors;
        static readonly List<Rect> reserved=new List<Rect>();
        static Transform propsRoot;

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit)){kit=new PolygonSceneryKit("Sakura "+key.x+"_"+key.y);kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform root)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Sakura block "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(root,false);pair.Value.Emit(group);
                }
                kits.Clear();
            }
        }

        static Material Mat(string name,Color color)=>PolygonWorld.Paint("SK "+name,color);
        static void CreatePalette()
        {
            asphalt=Mat("Asphalt",new Color(.20f,.21f,.24f));
            line=Mat("Road paint",new Color(.95f,.95f,.92f));
            kerbRed=Mat("Kerb red",new Color(.86f,.12f,.12f));
            gravel=Mat("Gravel trap",new Color(.80f,.72f,.56f));
            runoff=Mat("Run-off grass",new Color(.38f,.62f,.30f));
            grass=Mat("Meadow green",new Color(.44f,.62f,.32f));
            grassDark=Mat("Hill green",new Color(.32f,.50f,.28f));
            meadow=Mat("Spring meadow",new Color(.56f,.70f,.36f));
            rock=Mat("Grey rock",new Color(.55f,.54f,.50f));
            dirt=Mat("Earth bank",new Color(.55f,.44f,.32f));
            concrete=Mat("Concrete",new Color(.78f,.78f,.76f));
            wallStone=Mat("Retaining stone",new Color(.62f,.60f,.56f));
            armco=Mat("Barrier steel",new Color(.76f,.78f,.80f));
            tyreBlack=Mat("Tyre wall",new Color(.08f,.08f,.09f));
            tyreRed=Mat("Tyre band red",new Color(.85f,.14f,.12f));
            fence=Mat("Catch fence",new Color(.20f,.22f,.25f));
            cream=Mat("Ivory",new Color(1,.95f,.86f));
            white=Mat("White",new Color(.96f,.96f,.95f));
            charcoal=Mat("Charcoal",new Color(.13f,.14f,.16f));
            vermilion=Mat("Vermilion",new Color(.88f,.24f,.14f));
            gold=Mat("Gold",new Color(.90f,.72f,.30f));
            teal=Mat("Teal",new Color(.04f,.47f,.52f));
            navy=Mat("Navy",new Color(.09f,.17f,.32f));
            coral=Mat("Coral",new Color(.97f,.38f,.34f));
            ochre=Mat("Ochre",new Color(.95f,.68f,.30f));
            sakura=Mat("Cherry blossom",new Color(.98f,.72f,.80f));
            sakuraPale=Mat("Cherry blossom pale",new Color(1,.86f,.90f));
            sakuraDeep=Mat("Cherry blossom deep",new Color(.93f,.55f,.68f));
            pine=Mat("Pine green",new Color(.16f,.38f,.24f));
            pineDark=Mat("Pine dark",new Color(.10f,.28f,.20f));
            bark=Mat("Bark",new Color(.36f,.25f,.18f));
            lakeWater=Mat("Lake water",new Color(.12f,.42f,.52f));
            paddy=Mat("Paddy green",new Color(.52f,.74f,.30f));
            paddyFlooded=Mat("Paddy flooded",new Color(.50f,.66f,.70f));
            glassDark=Mat("Glass dark",new Color(.08f,.12f,.17f));
            steel=Mat("Steel grey",new Color(.58f,.60f,.63f));
            lampGlow=Mat("Lamp glow",new Color(1,.92f,.70f));lampGlow.SetColor("_EmissionColor",new Color(.8f,.65f,.4f));
            skinLight=Mat("Crowd skin light",new Color(.96f,.80f,.66f));
            skinTan=Mat("Crowd skin tan",new Color(.80f,.58f,.42f));
            skinDeep=Mat("Crowd skin deep",new Color(.46f,.31f,.21f));
            hair=Mat("Crowd hair",new Color(.10f,.08f,.07f));
            mountain=Mat("Mountain",new Color(.36f,.46f,.40f));
            mountainSnow=Mat("Mountain snow",new Color(.92f,.93f,.95f));
            roofGrey=Mat("Roof tile grey",new Color(.27f,.29f,.33f));
            plaster=Mat("White plaster",new Color(.93f,.91f,.86f));
            timber=Mat("Dark timber",new Color(.30f,.21f,.15f));
            lilac=Mat("Lilac",new Color(.62f,.48f,.82f));
            lime=Mat("Lime",new Color(.60f,.82f,.26f));
            sky=Mat("Sky blue",new Color(.36f,.66f,.92f));
            plaza=Mat("Park paving",new Color(.82f,.78f,.70f));
            plazaWarm=Mat("Park paving warm",new Color(.86f,.74f,.62f));
            teamColors=new[]{coral,teal,ochre,navy,lilac,lime};
            shirts=new[]{coral,teal,ochre,navy,white,lilac,lime,sakura,sky};
            skins=new[]{skinLight,skinTan,skinDeep};
            gondolaColors=new[]{coral,teal,ochre,sky,lilac,lime};
        }

        // ------------------------------------------------------------ route helpers
        static Vector3 P(float d)=>PolygonCircuit.AtDistance(d);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float E(float d)=>PolygonCircuit.Elevation(d);
        static Vector3 At(float d,float lateral,float y)=>P(d)+S(d)*lateral+Vector3.up*(E(d)+y);
        static float Offset(Vector3 p){PolygonCircuit.ProjectLapFlat(p,out float o);return o;}
        static float Hash(float x,float z,int salt=0)=>Mathf.Abs(Mathf.Sin(x*12.9898f+z*78.233f+salt*37.719f)*43758.5453f)%1f;
        static float Wrap(float a,float b){float d=Mathf.Repeat(a-b,PolygonCircuit.Length);return d>PolygonCircuit.Length*.5f?d-PolygonCircuit.Length:d;}

        static void Ribbon(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float y)
        {
            var a=At(d0,l0,y);var b=At(d0,l1,y);var c=At(d1,l1,y);var e=At(d1,l0,y);
            kit.Quad(m,a,b,c,e,(a+b+c+e)*.25f+Vector3.down);
        }
        static void Band(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,Material m,float y=.025f)
        {if(side>0)Ribbon(kit,m,d0,d1,a,b,y);else Ribbon(kit,m,d0,d1,-b,-a,y);}
        // Solid strip along the road between laterals a..b, heights y0..y1 relative to the road at each end.
        static void Slab(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,float y0a,float y0b,float y1,Material m)
        {
            float l0=side>0?a:-b,l1=side>0?b:-a;
            kit.Prism(m,new[]{At(d0,l0,y0a),At(d0,l1,y0a),At(d1,l1,y0b),At(d1,l0,y0b)},
                        new[]{At(d0,l0,y1),At(d0,l1,y1),At(d1,l1,y1),At(d1,l0,y1)});
        }
        static void Slab(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,float y0,float y1,Material m)=>Slab(kit,side,d0,d1,a,b,y0,y0,y1,m);
        static Transform Frame(Transform parent,string name,float d,float lateral,float y=0)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);
            t.localPosition=At(d,lateral,y);t.localRotation=Quaternion.LookRotation(T(d));
            return t;
        }
        static bool Reserved(Vector2 p,float margin)
        {
            foreach(var r in reserved)
                if(p.x>r.xMin-margin&&p.x<r.xMax+margin&&p.y>r.yMin-margin&&p.y<r.yMax+margin)return true;
            return false;
        }
        static void Reserve(Transform frame,float x0,float x1,float z0,float z1)
        {
            float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
            foreach(var local in new[]{new Vector3(x0,0,z0),new Vector3(x1,0,z1),new Vector3(x0,0,z1),new Vector3(x1,0,z0)})
            {var p=frame.TransformPoint(local);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
            reserved.Add(Rect.MinMaxRect(minX,minZ,maxX,maxZ));
        }
        static void ReserveWorld(float x0,float x1,float z0,float z1){reserved.Add(Rect.MinMaxRect(x0,z0,x1,z1));}
        static void Part(Transform parent,Mesh mesh,Material m)
        {
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        }

        // ------------------------------------------------------------ the crossover
        // Lower = the downhill run passing underneath; upper = the back straight on the bridge.
        static float crossLower,crossUpper;static Vector3 crossPoint;
        static void FindCrossover()
        {
            const int n=1024;float step=PolygonCircuit.Length/n;float best=float.MaxValue;
            var pts=new Vector3[n];for(int i=0;i<n;i++){pts[i]=P(i*step);pts[i].y=0;}
            for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)
            {
                if(Mathf.Abs(Wrap(i*step,j*step))<250)continue;
                float dd=(pts[i]-pts[j]).sqrMagnitude;
                if(dd<best){best=dd;float a=i*step,b=j*step;if(E(a)<E(b)){crossLower=a;crossUpper=b;}else{crossLower=b;crossUpper=a;}}
            }
            // Refine both distances to the true intersection.
            for(int k=0;k<4;k++)
            {
                crossLower=Refine(crossLower,P(crossUpper));crossUpper=Refine(crossUpper,P(crossLower));
            }
            crossPoint=P(crossLower);crossPoint.y=0;lowerSide=S(crossLower);
        }
        static Vector3 lowerSide;
        // Sideways distance from the road passing under the bridge (straight-line approximation).
        static float LowerLateral(Vector3 p){p.y=0;return Vector3.Dot(p-crossPoint,lowerSide);}
        static float Refine(float around,Vector3 target)
        {
            float best=float.MaxValue,bestD=around;target.y=0;
            for(float d=around-12;d<=around+12;d+=.1f){var p=P(d);p.y=0;float dd=(p-target).sqrMagnitude;if(dd<best){best=dd;bestD=d;}}
            return Mathf.Repeat(bestD,PolygonCircuit.Length);
        }
        static float UpperDelta(float d)=>Wrap(d,crossUpper);
        static bool OnBridge(float d)=>Mathf.Abs(UpperDelta(d))<BridgeHalfSpan;

        // ------------------------------------------------------------ terrain
        const int TerrainSamples=512;
        static Vector3[] tPts;static float[] tH,tD;static bool[] tZone;
        static void PrepareTerrainSamples()
        {
            tPts=new Vector3[TerrainSamples+1];tH=new float[TerrainSamples+1];tD=new float[TerrainSamples+1];tZone=new bool[TerrainSamples+1];
            for(int i=0;i<=TerrainSamples;i++)
            {
                float d=i*PolygonCircuit.Length/TerrainSamples;var p=P(d);p.y=0;
                tPts[i]=p;tH[i]=E(d);tD[i]=d;tZone[i]=Mathf.Abs(UpperDelta(d))<180;
            }
        }
        // Nearest point on the sampled route in plan view, either outside or inside the bridge zone.
        static void Nearest(Vector3 p,bool zone,out float offset,out float distance)
        {
            float best=float.MaxValue;distance=0;
            for(int i=0;i<TerrainSamples;i++)
            {
                if(tZone[i]!=zone&&tZone[i+1]!=zone)continue;
                var ab=tPts[i+1]-tPts[i];float t=Mathf.Clamp01(Vector3.Dot(p-tPts[i],ab)/Mathf.Max(ab.sqrMagnitude,.0001f));
                float dd=(p-(tPts[i]+ab*t)).sqrMagnitude;
                if(dd<best){best=dd;distance=Mathf.Lerp(tD[i],tD[i+1],t);}
            }
            offset=Mathf.Sqrt(best);
        }
        static float Hills(float x,float z)
        {
            float h=(Mathf.Sin(x*.011f+1.3f)*Mathf.Sin(z*.009f+.4f)*.5f+.5f)*20+(Mathf.Sin(x*.023f+z*.017f)*.5f+.5f)*7;
            // Mountains ringing the valley.
            float r=new Vector2(x-150,z-260).magnitude;
            h+=Mathf.SmoothStep(0,1,Mathf.InverseLerp(820,1500,r))*(120+90*(Mathf.Sin(Mathf.Atan2(z-260,x-150)*5+1)*.5f+.5f));
            // The pagoda hill north of the circuit.
            float pagoda=new Vector2(x-PagodaSite.x,z-PagodaSite.y).magnitude;
            h+=Mathf.SmoothStep(1,0,Mathf.InverseLerp(30,150,pagoda))*26;
            return h;
        }
        static float LakeDistance(float x,float z)=>new Vector2((x-LakeCentre.x)/LakeRx,(z-LakeCentre.y)/LakeRz).magnitude;
        public static float TerrainHeight(float x,float z)
        {
            var p=new Vector3(x,0,z);
            Nearest(p,false,out float o1,out float d1);
            Nearest(p,true,out float o2,out float d2);
            float sum=0,weight=0;
            for(int i=0;i<TerrainSamples;i+=2){float w=1/((tPts[i]-p).sqrMagnitude+900);sum+=tH[i]*w;weight+=w;}
            float blend=sum/weight,hills=Hills(x,z);
            float Base(float o,float d)=>Mathf.Lerp(E(d)-.35f,blend,Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,45,o)))+hills*Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,170,o));
            float h=Base(o1,d1);
            if(o2<o1)
            {
                // Close to the bridge the ground follows the road underneath; the back straight rides
                // above it on walled embankments, joining the ground again 100-180 m from the bridge.
                float du=Mathf.Abs(UpperDelta(d2));
                h=Mathf.Lerp(h,Base(o2,d2),Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,180,du)));
            }
            if(o2<60)
            {
                float du=Mathf.Abs(UpperDelta(d2));
                h=Mathf.Min(h,E(d2)-.8f+Mathf.Max(0,o2-17)*.6f+Mathf.Max(0,du-150));
            }
            h=ApplySites(x,z,h);
            // The lake: a shallow basin with a sandy shelf.
            float lake=LakeDistance(x,z);
            if(lake<1.25f)h=Mathf.Lerp(LakeLevel-3.2f,Mathf.Max(h,LakeLevel+.6f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9f,1.25f,lake)));
            return h;
        }

        static void BuildTerrain(Chunks chunks)
        {
            const float fx0=-660,fx1=960,fz0=-260,fz1=780,fine=10;
            GridTerrain(chunks,fx0,fx1,fz0,fz1,fine,null,true);
            GridTerrain(chunks,-2200,2600,-1800,2400,40,Rect.MinMaxRect(fx0,fz0,fx1,fz1),false);
        }
        static void GridTerrain(Chunks chunks,float x0,float x1,float z0,float z1,float step,Rect? hole,bool skirt)
        {
            int nx=Mathf.RoundToInt((x1-x0)/step),nz=Mathf.RoundToInt((z1-z0)/step);
            var h=new float[nx+1,nz+1];
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)
            {
                float x=x0+i*step,z=z0+j*step;
                if(hole.HasValue&&x>hole.Value.xMin&&x<hole.Value.xMax&&z>hole.Value.yMin&&z<hole.Value.yMax){h[i,j]=0;continue;}
                h[i,j]=TerrainHeight(x,z);
            }
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
            {
                float x=x0+i*step,z=z0+j*step;
                if(hole.HasValue){var r=hole.Value;if(x>=r.xMin&&x+step<=r.xMax&&z>=r.yMin&&z+step<=r.yMax)continue;}
                var a=new Vector3(x,h[i,j],z);var b=new Vector3(x+step,h[i+1,j],z);
                var c=new Vector3(x+step,h[i+1,j+1],z+step);var d=new Vector3(x,h[i,j+1],z+step);
                var kit=chunks.At((a+c)*.5f);
                // Split along the shorter diagonal so ridges stay crisp.
                if(Mathf.Abs(a.y-c.y)<=Mathf.Abs(b.y-d.y)){TerrainTri(kit,a,b,c,hole.HasValue);TerrainTri(kit,a,c,d,hole.HasValue);}
                else{TerrainTri(kit,a,b,d,hole.HasValue);TerrainTri(kit,b,c,d,hole.HasValue);}
            }
            if(skirt)
            {
                for(int i=0;i<nx;i++)foreach(int j in new[]{0,nz})
                {
                    var a=new Vector3(x0+i*step,h[i,j],z0+j*step);var b=new Vector3(x0+(i+1)*step,h[i+1,j],z0+j*step);
                    chunks.At(a).Sheet(rock,a,b,b+Vector3.down*40);chunks.At(a).Sheet(rock,a,b+Vector3.down*40,a+Vector3.down*40);
                }
                for(int j=0;j<nz;j++)foreach(int i in new[]{0,nx})
                {
                    var a=new Vector3(x0+i*step,h[i,j],z0+j*step);var b=new Vector3(x0+i*step,h[i,j+1],z0+(j+1)*step);
                    chunks.At(a).Sheet(rock,a,b,b+Vector3.down*40);chunks.At(a).Sheet(rock,a,b+Vector3.down*40,a+Vector3.down*40);
                }
            }
        }
        static void TerrainTri(PolygonSceneryKit kit,Vector3 a,Vector3 b,Vector3 c,bool far)
        {
            var n=Vector3.Cross(b-a,c-a).normalized;if(n.y<0)n=-n;
            var centre=(a+b+c)/3;Material m;
            if(centre.y>175)m=mountainSnow;
            else if(n.y<.62f)m=rock;
            else if(centre.y>70)m=n.y<.8f?rock:mountain;
            else if(!far&&LakeDistance(centre.x,centre.z)<1.02f)m=dirt;
            else if(!far&&InPark(centre.x,centre.z))m=Hash(Mathf.Floor(centre.x/20),Mathf.Floor(centre.z/20),9)<.5f?plaza:plazaWarm;
            else
            {
                float o=far?999:Offset(centre);
                float patch=Hash(Mathf.Floor(centre.x/30),Mathf.Floor(centre.z/30));
                m=o<17.5f?runoff:n.y<.82f?dirt:centre.y>28?grassDark:patch<.3f?meadow:grass;
            }
            kit.Tri(m,a,b,c,centre+Vector3.down);
        }

        // ------------------------------------------------------------ road, run-off, barriers
        static float[] groundLeft,groundRight;   // ground height relative to the road, just outside the wall line
        static void PrepareGround()
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;
            groundLeft=new float[n+1];groundRight=new float[n+1];
            for(int i=0;i<=n;i++)
            {
                float d=i*step;
                var l=At(d,-(Wall1+.6f),0);var r=At(d,Wall1+.6f,0);
                groundLeft[i]=TerrainHeight(l.x,l.z)-E(d);groundRight[i]=TerrainHeight(r.x,r.z)-E(d);
            }
        }
        static bool Corner(float d)=>Mathf.Abs(PolygonCircuit.Curvature(d))>1/90f||Mathf.Abs(PolygonCircuit.Curvature(d-14))>1/90f||Mathf.Abs(PolygonCircuit.Curvature(d+14))>1/90f;
        static float OutsideSign(float d)
        {
            float c=PolygonCircuit.Curvature(d)+PolygonCircuit.Curvature(d-14)+PolygonCircuit.Curvature(d+14);
            return c>0?-1:1;   // turning right (positive) puts the outside on the left
        }

        static void BuildRoad(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;const float checkerEnd=1.6f;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));
                bool corner=Corner(dm);float outside=OutsideSign(dm);bool bridge=OnBridge(dm);
                foreach(float side in new[]{-1f,1f})
                {
                    Band(kit,side,d0,d1,Road-.4f,Road,line);
                    Band(kit,side,d0,d1,Road,Kerb,corner?((i/2)%2==0?kerbRed:line):runoff,.03f);
                    Band(kit,side,d0,d1,Kerb,Wall1,corner&&side==outside&&!bridge?gravel:bridge?concrete:runoff,.02f);
                    float g0=side<0?groundLeft[i]:groundRight[i],g1=side<0?groundLeft[i+1]:groundRight[i+1];
                    if(side<0&&dm>PitStart&&dm<PitEnd)continue;   // the pit apron slab closes this side
                    if(bridge)
                    {
                        // Embankment side walls continue under the deck until the road below.
                        var w=At(dm,side*Wall1,0);
                        if(Mathf.Abs(LowerLateral(w))>AbutmentOffset+1.6f)Slab(kit,side,d0,d1,Wall0,Wall1,g0-1.2f,g1-1.2f,-1.95f,wallStone);
                        continue;
                    }
                    if(Mathf.Min(g0,g1)<-.6f)
                    {
                        // Embankment: stone retaining wall from the ground up to a low parapet.
                        Slab(kit,side,d0,d1,Wall0,Wall1,g0-1.2f,g1-1.2f,1.0f,wallStone);
                    }
                    else
                    {
                        // A skirt under the road edge so terrain steps never show a gap.
                        float l=side*Wall1;
                        kit.Sheet(dirt,At(d0,l,.02f),At(d1,l,.02f),At(d1,l,Mathf.Min(-1.5f,g1-1)));
                        kit.Sheet(dirt,At(d0,l,.02f),At(d1,l,Mathf.Min(-1.5f,g1-1)),At(d0,l,Mathf.Min(-1.5f,g0-1)));
                    }
                }
                if(d0<checkerEnd)
                {
                    const int rows=2,cols=18;
                    for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
                    {
                        float a0=d0+checkerEnd*r/rows,a1=d0+checkerEnd*(r+1)/rows,l0=-(Road-.4f)+c*(Road-.4f)*2/cols,l1=l0+(Road-.4f)*2/cols;
                        Ribbon(kit,(r+c)%2==0?line:charcoal,a0,a1,l0,l1,.025f);
                    }
                    d0=checkerEnd;
                }
                Ribbon(kit,asphalt,d0,d1,-(Road-.4f),Road-.4f,.025f);
            }
            // Grid boxes for the starting field.
            var gk=chunks.At(P(10));
            foreach(var (gd,lane) in new[]{(-4f,0f),(8f,-3.5f),(20f,3.5f),(20f,-3.5f)})
            {
                Ribbon(gk,line,gd-.15f,gd+.15f,lane-1.6f,lane+1.6f,.03f);
                Ribbon(gk,line,gd-4,gd+.15f,lane-1.6f,lane-1.3f,.03f);Ribbon(gk,line,gd-4,gd+.15f,lane+1.3f,lane+1.6f,.03f);
            }
        }

        static void BuildBarriers(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount/2;float step=PolygonCircuit.Length/n;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;var kit=chunks.At(P(dm));
                bool corner=Corner(dm);float outside=OutsideSign(dm);
                foreach(float side in new[]{-1f,1f})
                {
                    float mid=side*(Barrier0+Barrier1)*.5f;
                    kit.Beam(armco,At(d0,mid,.5f),At(d1,mid,.5f),.1f,.3f);
                    kit.Beam(armco,At(d0,mid,.88f),At(d1,mid,.88f),.1f,.3f);
                    if(i%2==0)kit.Beam(steel,At(d0,mid+side*.15f,0),At(d0,mid+side*.15f,1.1f),.12f,.12f);
                    if(corner&&side==outside&&!OnBridge(dm))
                    {
                        // Tyre wall behind the barrier with red bands, and a catch fence.
                        Slab(kit,side,d0,d1,Barrier1+.05f,Wall0-.05f,0,.45f,tyreBlack);
                        Slab(kit,side,d0,d1,Barrier1+.05f,Wall0-.05f,.45f,.75f,(i/2)%2==0?tyreRed:white);
                        kit.Rail(fence,At(d0,side*(Wall0-.2f),4.2f),At(d1,side*(Wall0-.2f),4.2f),.06f);
                        kit.Rail(fence,At(d0,side*(Wall0-.2f),2.4f),At(d1,side*(Wall0-.2f),2.4f),.04f);
                        if(i%2==0)kit.Beam(fence,At(d0,side*(Wall0-.2f),.6f),At(d0,side*(Wall0-.2f),4.25f),.1f,.1f);
                    }
                }
            }
        }

        // ------------------------------------------------------------ the crossover bridge
        static void BuildBridge(Transform world,Chunks chunks)
        {
            int n=Mathf.CeilToInt(BridgeHalfSpan*2/3);float step=BridgeHalfSpan*2/n;
            for(int i=0;i<n;i++)
            {
                float d0=crossUpper-BridgeHalfSpan+i*step,d1=d0+step,dm=(d0+d1)*.5f;var kit=chunks.At(P(dm));
                // Deck slab, edge beams and parapets.
                kit.Prism(concrete,new[]{At(d0,-DeckHalfWidth,-1.9f),At(d0,DeckHalfWidth,-1.9f),At(d1,DeckHalfWidth,-1.9f),At(d1,-DeckHalfWidth,-1.9f)},
                                   new[]{At(d0,-DeckHalfWidth,-.05f),At(d0,DeckHalfWidth,-.05f),At(d1,DeckHalfWidth,-.05f),At(d1,-DeckHalfWidth,-.05f)});
                foreach(float side in new[]{-1f,1f})
                {
                    Slab(kit,side,d0,d1,DeckHalfWidth-.6f,DeckHalfWidth+.25f,-2.4f,1.15f,white);
                    Slab(kit,side,d0,d1,DeckHalfWidth+.25f,DeckHalfWidth+.35f,-.6f,.2f,vermilion);   // painted stripe band
                }
                for(int g=-2;g<=2;g++)Slab(kit,1,d0,d1,g*6.5f-.45f,g*6.5f+.45f,-3.3f,-1.95f,steel);
                if(i%2==0)kit.Box(lampGlow,At(dm,0,-1.95f),new Vector3(.5f,.1f,1.2f),Quaternion.LookRotation(T(dm)));
            }
            // Name boards on both faces of the bridge, read by drivers on the road below.
            foreach(float side in new[]{-1f,1f})
            {
                var board=Frame(world,"Bridge name board",crossUpper,side*(DeckHalfWidth+.6f),-.3f);
                board.localRotation=Quaternion.LookRotation(-side*S(crossUpper));
                PolygonWorld.Box("Bridge board panel",board,new Vector3(0,0,.12f),new Vector3(24,2.2f,.2f),navy);
                PolygonWorld.Label("Bridge lettering",board,"POLYGON SAKURA",new Vector3(0,0,-.05f),.18f,cream.color);
            }
            // Abutments: walls parallel to the lower road closing the embankment ends.
            var lp=P(crossLower);lp.y=0;var lt=T(crossLower);var ls=S(crossLower);
            var up=P(crossUpper);up.y=0;var ut=T(crossUpper);var us=S(crossUpper);
            foreach(float s in new[]{-1f,1f})
            {
                var basePoint=lp+ls*s*AbutmentOffset;
                // Solve for the points on this line at upper laterals -/+ (deck edge + parapet).
                float Solve(float upperLateral)
                {
                    float denom=Vector3.Dot(lt,us);
                    return (upperLateral-Vector3.Dot(basePoint-up,us))/(Mathf.Abs(denom)<.01f?.01f:denom);
                }
                float ta=Solve(-(DeckHalfWidth+.4f)),tb=Solve(DeckHalfWidth+.4f);
                var a=basePoint+lt*ta;var b=basePoint+lt*tb;
                float ua=crossUpper+Vector3.Dot(a-up,ut),ub=crossUpper+Vector3.Dot(b-up,ut);
                float bottom=E(crossLower)-2;
                var outward=ls*s*1.4f;
                var kit=chunks.At(a);
                kit.Prism(wallStone,new[]{a+Vector3.up*bottom,b+Vector3.up*bottom,b+outward+Vector3.up*bottom,a+outward+Vector3.up*bottom},
                                    new[]{a+Vector3.up*(E(ua)-1.9f),b+Vector3.up*(E(ub)-1.9f),b+outward+Vector3.up*(E(ub)-1.9f),a+outward+Vector3.up*(E(ua)-1.9f)});
                // Bearing shelf in front of the wall under the deck.
                kit.Prism(concrete,new[]{a+Vector3.up*(E(ua)-2.6f),b+Vector3.up*(E(ub)-2.6f),b-outward*.6f+Vector3.up*(E(ub)-2.6f),a-outward*.6f+Vector3.up*(E(ua)-2.6f)},
                                   new[]{a+Vector3.up*(E(ua)-1.95f),b+Vector3.up*(E(ub)-1.95f),b-outward*.6f+Vector3.up*(E(ub)-1.95f),a-outward*.6f+Vector3.up*(E(ua)-1.95f)});
            }
        }

        // ------------------------------------------------------------ levelled sites
        // Ground inside these rectangles is cut down to a fixed height (soft 20 m edge), so pits,
        // stands and the amusement park never have terrain poking through their floors.
        struct Site{public Vector3 Centre,AxisX,AxisZ;public float HalfX,HalfZ,Height,Edge;}
        static readonly List<Site> sites=new List<Site>();
        static void AddSite(Vector3 centre,Vector3 forward,float halfX,float halfZ,float height,float edge=20)
        {
            forward.y=0;forward.Normalize();
            sites.Add(new Site{Centre=new Vector3(centre.x,0,centre.z),AxisZ=forward,AxisX=Vector3.Cross(Vector3.up,forward),HalfX=halfX,HalfZ=halfZ,Height=height,Edge=edge});
        }
        static float ApplySites(float x,float z,float h)
        {
            foreach(var s in sites)
            {
                var d=new Vector3(x,0,z)-s.Centre;
                float ox=Mathf.Max(0,Mathf.Abs(Vector3.Dot(d,s.AxisX))-s.HalfX),oz=Mathf.Max(0,Mathf.Abs(Vector3.Dot(d,s.AxisZ))-s.HalfZ);
                float outside=Mathf.Sqrt(ox*ox+oz*oz);if(outside>s.Edge)continue;
                h=Mathf.Min(h,Mathf.Lerp(s.Height,h,Mathf.SmoothStep(0,1,outside/s.Edge)));
            }
            return h;
        }
        static float DistanceAt(Vector3 p)=>PolygonCircuit.ProjectLapFlat(p,out _)*PolygonCircuit.Length;

        // ------------------------------------------------------------ start gantry
        static void BuildGantry(Transform world)
        {
            var gate=Frame(world,"POLYGON SAKURA - start gantry",32,0);
            var kit=new PolygonSceneryKit("Sakura gantry");
            const float span=17.8f,beamY=7.8f;
            foreach(int side in new[]{-1,1})
            {
                var foot=new Vector3(side*span,0,0);
                kit.Chamfered(concrete,foot,1.1f,1.2f,1.0f,1.1f,-2,.6f,.3f);
                kit.Chamfered(white,foot,.6f,.7f,.42f,.52f,.6f,beamY+2f,.2f);
                kit.Chamfered(vermilion,foot,.68f,.78f,.66f,.76f,.9f,1.6f,.2f);
            }
            // Torii-style crown: a dark lintel with upswept vermilion ends over the signal housing.
            kit.Box(navy,new Vector3(0,beamY+.9f,0),new Vector3(span*2+1.4f,1.8f,.9f));
            kit.Box(vermilion,new Vector3(0,beamY+2.05f,0),new Vector3(span*2+3.4f,.5f,1.3f));
            foreach(int s in new[]{-1,1})kit.Box(vermilion,new Vector3(s*(span+1.9f),beamY+2.35f,0),new Vector3(1.2f,.5f,1.3f),Quaternion.Euler(0,0,s*-12));
            kit.Box(charcoal,new Vector3(0,beamY+2.45f,0),new Vector3(span*2+2.6f,.3f,1.1f));
            var front=PolygonWorld.Label("Polygon Sakura race lettering",gate,"POLYGON SAKURA",new Vector3(0,beamY+.9f,-.6f),.15f,cream.color);
            var rear=PolygonWorld.Label("Polygon Sakura reverse lettering",gate,"POLYGON SAKURA",new Vector3(0,beamY+.9f,.6f),.15f,cream.color);
            rear.transform.localRotation=Quaternion.Euler(0,180,0);
            var letteringShader=Resources.Load<Shader>("PolygonGantryLettering");
            if(letteringShader)
            {
                var lettering=new Material(letteringShader){name="Polygon Sakura depth tested lettering"};
                lettering.mainTexture=front.font.material.mainTexture;
                front.GetComponent<MeshRenderer>().sharedMaterial=lettering;rear.GetComponent<MeshRenderer>().sharedMaterial=lettering;
                var swap=gate.gameObject.AddComponent<PolygonGantryLettering>();swap.FrontText=front;swap.RearText=rear;swap.DepthMaterial=lettering;
            }
            var dark=Mat("Starting lens dark",new Color(.065f,.018f,.025f));
            var lit=Mat("Starting lens red",new Color(1,.025f,.012f));lit.SetColor("_EmissionColor",new Color(1,.02f,.008f));
            kit.Box(charcoal,new Vector3(0,beamY-.8f,-.1f),new Vector3(9.3f,1.4f,1.1f));
            for(int i=0;i<5;i++)kit.Box(charcoal,new Vector3((i-2)*1.7f,beamY-.2f,-.9f),new Vector3(1.15f,.06f,.5f),Quaternion.Euler(-8,0,0));
            kit.Emit(gate);
            var lens=PolygonSceneryMeshesExtra.SignalLens();var lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)
            {
                var go=new GameObject("Starting red lens "+(i+1),typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(gate,false);go.transform.localPosition=new Vector3((i-2)*1.7f,beamY-.8f,-.66f);
                go.GetComponent<MeshFilter>().sharedMesh=lens;lenses[i]=go.GetComponent<MeshRenderer>();lenses[i].sharedMaterial=lit;
            }
            var keep=new GameObject("Starting lens dark material holder",typeof(MeshFilter),typeof(MeshRenderer));
            keep.transform.SetParent(gate,false);keep.transform.localPosition=new Vector3(0,beamY-.8f,.5f);keep.transform.localScale=Vector3.one*.4f;
            keep.GetComponent<MeshFilter>().sharedMesh=lens;keep.GetComponent<MeshRenderer>().sharedMaterial=dark;
            var lights=gate.gameObject.AddComponent<PolygonStartLights>();lights.Lenses=lenses;lights.DarkMaterial=dark;lights.RedMaterial=lit;
        }

        // ------------------------------------------------------------ pits, paddock and tower
        const float PitStart=-12,PitEnd=210;
        static void BuildPits(Transform world,Chunks chunks)
        {
            int n=Mathf.CeilToInt((PitEnd-PitStart)/4);float step=(PitEnd-PitStart)/n;
            for(int i=0;i<n;i++)
            {
                float d0=PitStart+i*step,d1=d0+step,dm=(d0+d1)*.5f;var kit=chunks.At(P(dm));
                // Pit wall with a fence, then the pit apron up to the garages.
                Slab(kit,-1,d0,d1,Barrier1+.05f,Barrier1+.75f,-.6f,1.2f,concrete);
                Slab(kit,-1,d0,d1,Barrier1+.75f,32,-2.5f,.06f,concrete);
                Ribbon(kit,line,d0,d1,-24.2f,-24f,.07f);
                kit.Rail(fence,At(d0,-(Barrier1+.4f),4.2f),At(d1,-(Barrier1+.4f),4.2f),.06f);
                if(i%2==0)kit.Beam(fence,At(d0,-(Barrier1+.4f),1.2f),At(d0,-(Barrier1+.4f),4.25f),.1f,.1f);
            }
            // Garage block: two storeys, team-coloured bay fascias, hospitality glazing above.
            float mid=(PitStart+PitEnd)*.5f,len=PitEnd-PitStart-8;
            var g=Frame(world,"Sakura pit garages",mid,0);var gk=new PolygonSceneryKit("Sakura garages");
            const float x0=-46,x1=-32,h=6.5f;
            gk.Box(white,new Vector3((x0+x1)*.5f,(h-4)*.5f,0),new Vector3(x1-x0,h+4,len));
            gk.Box(charcoal,new Vector3((x0+x1)*.5f,h+.25f,0),new Vector3(x1-x0+.8f,.5f,len+.8f));
            gk.Sleeve(glassDark,new Vector3((x0+x1)*.5f,h+2.1f,0),new Vector3(x1-x0-1.2f,3.2f,len-1.2f));
            gk.Box(white,new Vector3((x0+x1)*.5f,h+4.0f,0),new Vector3(x1-x0+2.4f,.5f,len+2.4f));
            gk.Box(vermilion,new Vector3(x1+1.25f,h+4.0f,0),new Vector3(.3f,.6f,len+2.4f));
            var slots=new GameObject("Paddock parked car slots").transform;slots.SetParent(world,false);
            const int bays=14;float bay=len/bays;
            for(int i=0;i<bays;i++)
            {
                float z=-len*.5f+bay*(i+.5f);var color=teamColors[i%teamColors.Length];
                gk.Box(charcoal,new Vector3(x1+.15f,2.0f,z),new Vector3(.3f,4.0f,bay-2.4f));
                gk.Box(color,new Vector3(x1+.25f,4.7f,z),new Vector3(.5f,1.0f,bay-.4f));
                gk.Box(white,new Vector3(x1+.2f,2.6f,z+bay*.5f),new Vector3(.4f,5.2f,.6f));
                if(i%4==1)
                {
                    var slot=new GameObject("Parked team car slot "+i).transform;slot.SetParent(slots,false);
                    slot.position=g.TransformPoint(new Vector3(x1+3.4f,.18f,z));slot.rotation=g.rotation*Quaternion.Euler(0,-90,0);
                }
            }
            gk.Emit(g);Reserve(g,x0-30,x1+6,-len*.5f-6,len*.5f+6);
            // Paddock behind: team trucks and hospitality tents.
            var pk=new PolygonSceneryKit("Sakura paddock");
            for(int i=0;i<10;i++)
            {
                float z=-len*.5f+12+i*(len-24)/9;var color=teamColors[i%teamColors.Length];
                pk.Box(white,new Vector3(x0-9,2.0f,z),new Vector3(3,3.4f,14));
                pk.Box(color,new Vector3(x0-9,2.3f,z),new Vector3(3.05f,.9f,14.05f));
                pk.Box(charcoal,new Vector3(x0-9,.45f,z-7.6f),new Vector3(2.6f,1,1.6f));
                if(i%3==0)
                {
                    pk.Box(white,new Vector3(x0-22,1.6f,z),new Vector3(10,3.2f,10));
                    pk.Chamfered(i%2==0?white:color,new Vector3(x0-22,3.2f,z),5.6f,5.6f,.4f,.4f,0,2.4f,1);
                }
            }
            pk.Emit(g);
            // Race control tower at the pit exit.
            var tower=Frame(world,"Sakura race control tower",PitEnd+22,-40);var tk=new PolygonSceneryKit("Sakura tower");
            tk.Chamfered(white,Vector3.zero,7,7,6,6,-3,18,1.5f);
            for(int f=0;f<3;f++)
            {
                float y=20+f*4.2f;
                tk.Chamfered(white,Vector3.zero,9-f*.6f,9-f*.6f,9-f*.6f,9-f*.6f,y-2,y-1.2f,2);
                tk.Chamfered(glassDark,Vector3.zero,8.6f-f*.6f,8.6f-f*.6f,8.8f-f*.6f,8.8f-f*.6f,y-1.2f,y+1.6f,2);
                tk.Chamfered(white,Vector3.zero,9.2f-f*.6f,9.2f-f*.6f,9.2f-f*.6f,9.2f-f*.6f,y+1.6f,y+2.2f,2);
            }
            tk.Chamfered(vermilion,Vector3.zero,7.6f,7.6f,1,1,32.2f,36,1.5f);
            tk.Column(steel,new Vector3(0,36,0),.25f,.08f,7,6);
            tk.Emit(tower);Reserve(tower,-12,12,-12,12);
        }

        // ------------------------------------------------------------ grandstands
        static void Grandstand(Transform world,string name,float startD,float length,int sign,int rows,int theme,float front=17.6f)
        {
            float mid=startD+length*.5f;var t=Frame(world,name,mid,0);var kit=new PolygonSceneryKit("Sakura stand "+theme);
            const float rowDepth=1.15f,rise=.72f;float back=front+rows*rowDepth;var facing=new Vector3(-sign,0,0);
            int sections=Mathf.Max(1,Mathf.RoundToInt(length/12));float sectionLength=length/sections;
            for(int r=0;r<rows;r++)
            {
                float x0=front+r*rowDepth,top=1.2f+r*rise;
                kit.Box(r%2==0?concrete:white,new Vector3(sign*(x0+rowDepth*.5f),(top-6)*.5f,0),new Vector3(rowDepth,top+6,length));
                for(int sct=0;sct<sections;sct++)
                {
                    var seat=teamColors[(sct+theme+(r/4))%teamColors.Length];float z0=-length*.5f+sct*sectionLength;
                    kit.Box(seat,new Vector3(sign*(x0+.45f),top+.22f,z0+sectionLength*.5f),new Vector3(.5f,.44f,sectionLength-.9f));
                    int seats=Mathf.FloorToInt((sectionLength-1.2f)/1.05f);
                    for(int s=0;s<seats;s++)
                    {
                        float z=z0+.9f+s*1.05f,hsh=Hash(z+theme*17,r,2);if(hsh<.24f)continue;
                        var shirt=shirts[Mathf.FloorToInt(Hash(z,r,3)*shirts.Length)%shirts.Length];
                        kit.PersonLite(new Vector3(sign*(x0+.45f),top+.18f,z),facing,.95f+hsh*.12f,shirt,skins[Mathf.FloorToInt(Hash(z,r,5)*3)%3],hsh>.86f?white:hair,hsh>.93f);
                    }
                }
            }
            for(int sct=0;sct<sections;sct++)
            {
                float z0=-length*.5f+sct*sectionLength;
                kit.Box(teamColors[(sct+theme+2)%teamColors.Length],new Vector3(sign*(front-.1f),.6f,z0+sectionLength*.5f),new Vector3(.2f,1.2f,sectionLength-.2f));
            }
            float wallTop=1.2f+rows*rise+3f;
            kit.Box(navy,new Vector3(sign*(back+.25f),(wallTop-6)*.5f,0),new Vector3(.5f,wallTop+6,length+.6f));
            // Cantilevered white roof with a vermilion fascia, on raked columns at the back.
            var a=new Vector3(sign*(front-1.5f),wallTop+.8f,0);var b=new Vector3(sign*(back+.6f),wallTop+2.6f,0);
            kit.Box(white,(a+b)*.5f,new Vector3(Mathf.Abs(b.x-a.x),.35f,length+1),Quaternion.Euler(0,0,sign*Mathf.Atan2(b.y-a.y,Mathf.Abs(b.x-a.x))*Mathf.Rad2Deg));
            kit.Box(vermilion,a+new Vector3(sign*-.1f,-.15f,0),new Vector3(.3f,.6f,length+1.1f));
            int bays=Mathf.Max(2,Mathf.RoundToInt(length/10));
            for(int k=0;k<=bays;k++)
            {
                float z=-length*.5f+k*length/bays;
                kit.Beam(steel,new Vector3(sign*(back+.5f),-1,z),new Vector3(sign*(back+.5f),wallTop+2.5f,z),.3f,.3f);
                kit.Beam(steel,new Vector3(sign*(back+.5f),wallTop+2.4f,z),new Vector3(sign*(front-1.2f),wallTop+.9f,z),.18f,.25f);
            }
            kit.Emit(t);
            Reserve(t,sign>0?front-2:-(back+4),sign>0?back+4:-(front-2),-length*.5f-4,length*.5f+4);
        }

        static void PlanSites()
        {
            // Pits and main grandstand either side of the start straight.
            float mid=(PitStart+PitEnd)*.5f;
            AddSite(P(mid)-S(mid)*36,T(mid),30,(PitEnd-PitStart)*.5f+8,E(PitEnd)-.5f);
            AddSite(P(mid)+S(mid)*26,T(mid),12,(PitEnd-PitStart)*.5f+10,E(PitEnd)-.5f);
            AddSite(P(PitEnd+22)-S(PitEnd+22)*40,T(PitEnd+22),14,14,E(PitEnd+22)-.5f);
            // Amusement park.
            AddSite(new Vector3(ParkCentre.x,0,ParkCentre.y),Vector3.forward,ParkHalf.x,ParkHalf.y,1.6f,55);
            // A gentle hollow round the lake.
            AddSite(new Vector3(LakeCentre.x,0,LakeCentre.y),Vector3.forward,LakeRx*1.45f,LakeRz*1.45f,LakeLevel+1.4f);
        }

        static void BuildStands(Transform world)
        {
            Grandstand(world,"Main grandstand",PitStart,PitEnd-PitStart,1,13,0);
            Grandstand(world,"Turn one grandstand",DistanceAt(new Vector3(770,0,100))-20,80,1,10,1);
            Grandstand(world,"Esses grandstand",DistanceAt(new Vector3(785,0,300))-55,110,-1,9,2,36f);
            Grandstand(world,"Hairpin grandstand",DistanceAt(new Vector3(-430,0,83))-75,70,-1,10,3);
            Grandstand(world,"Spoon grandstand",DistanceAt(new Vector3(-288,0,330))-45,80,-1,9,4,22f);
            Grandstand(world,"Chicane grandstand",DistanceAt(new Vector3(330,0,157))-35,55,-1,8,5,22f);
        }

        // ------------------------------------------------------------ amusement park
        static void BuildAmusementPark(Transform world,Transform animatedRoot,Chunks chunks)
        {
            float ground=TerrainHeight(FerrisSite.x,FerrisSite.y);
            const float hubY=40,radius=31,halfWidth=1.7f;const int spokes=16,gondolas=24;
            // Static A-frame legs and the boarding platform.
            var site=new Vector3(FerrisSite.x,ground,FerrisSite.y);
            var kit=chunks.At(site);
            foreach(int s in new[]{-1,1})foreach(int f in new[]{-1,1})
                kit.Beam(white,site+new Vector3(f*15,0,s*(halfWidth+4)),site+new Vector3(0,hubY,s*(halfWidth+.6f)),.9f,.9f);
            foreach(int s in new[]{-1,1})kit.Beam(white,site+new Vector3(-15*.45f,hubY*.45f,s*(halfWidth+2.4f)),site+new Vector3(15*.45f,hubY*.45f,s*(halfWidth+2.4f)),.6f,.6f);
            kit.Box(concrete,site+new Vector3(0,.4f,-6),new Vector3(14,1.6f,7));
            kit.Box(coral,site+new Vector3(0,3.4f,-6),new Vector3(14.6f,.4f,7.6f));
            foreach(int f in new[]{-1,1})kit.Beam(white,site+new Vector3(f*6.8f,1.2f,-8.6f),site+new Vector3(f*6.8f,3.2f,-8.6f),.25f,.25f);
            ReserveWorld(FerrisSite.x-22,FerrisSite.x+22,FerrisSite.y-14,FerrisSite.y+10);

            // Turning wheel: its own transform under the animated root (not static batched).
            var root=new GameObject("Sakura Ferris wheel").transform;root.SetParent(animatedRoot,false);
            root.localPosition=site+Vector3.up*hubY;root.localRotation=Quaternion.identity;
            var wheel=new GameObject("Wheel").transform;wheel.SetParent(root,false);
            var wk=new PolygonSceneryKit("Ferris wheel");
            const int rimSegments=48;
            Vector3 Rim(int k,float r,float z){float a=k*Mathf.PI*2/rimSegments;return new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z);}
            foreach(float z in new[]{-halfWidth,halfWidth})
            {
                for(int k=0;k<rimSegments;k++)
                {
                    wk.Beam(white,Rim(k,radius,z),Rim(k+1,radius,z),.5f,.5f);
                    wk.Beam(steel,Rim(k,radius-3.2f,z),Rim(k+1,radius-3.2f,z),.3f,.3f);
                    if(k%2==0)wk.Beam(white,Rim(k,radius,z),Rim(k+1,radius-3.2f,z),.18f,.18f);
                }
                for(int k=0;k<spokes;k++)wk.Beam(steel,new Vector3(0,0,z),Rim(k*rimSegments/spokes,radius-3.2f,z),.22f,.22f);
            }
            for(int k=0;k<rimSegments;k+=2)wk.Beam(white,Rim(k,radius,-halfWidth),Rim(k,radius,halfWidth),.25f,.25f);
            wk.Column(coral,new Vector3(0,0,-halfWidth-1.2f),2.6f,2.6f,.01f,12);
            wk.Box(coral,Vector3.zero,new Vector3(3.6f,3.6f,halfWidth*2+3));
            for(int k=0;k<rimSegments;k+=3)wk.Box(lampGlow,Rim(k,radius+.35f,-halfWidth-.1f),new Vector3(.45f,.45f,.3f));
            wk.Emit(wheel);
            // Gondolas hang from pivots on the rim; one template, instantiated round the wheel.
            var pivots=new Transform[gondolas];
            for(int i=0;i<gondolas;i++)
            {
                var pivot=new GameObject("Gondola "+(i+1)).transform;pivot.SetParent(wheel,false);
                float a=i*Mathf.PI*2/gondolas;pivot.localPosition=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                pivots[i]=pivot;
            }
            for(int c=0;c<gondolaColors.Length;c++)
            {
                var gk=new PolygonSceneryKit("Gondola "+gondolaColors[c].name);
                gk.Beam(steel,new Vector3(0,0,-halfWidth+.2f),new Vector3(0,0,halfWidth-.2f),.25f,.25f);
                gk.Beam(steel,new Vector3(0,0,0),new Vector3(0,-1.3f,0),.2f,.2f);
                gk.Chamfered(gondolaColors[c],new Vector3(0,-4.6f,0),1.2f,1.2f,1.3f,1.3f,0,1.2f,.35f);
                gk.Chamfered(glassDark,new Vector3(0,-4.6f,0),1.22f,1.22f,1.22f,1.22f,1.2f,2.4f,.35f);
                gk.Chamfered(white,new Vector3(0,-4.6f,0),1.35f,1.35f,.3f,.3f,2.4f,3.3f,.35f);
                gk.Emit(pivots[c]);
                // The other gondolas of this colour share the same meshes.
                var template=new List<Transform>();foreach(Transform child in pivots[c])template.Add(child);
                for(int i=c+gondolaColors.Length;i<gondolas;i+=gondolaColors.Length)
                    foreach(var child in template)Object.Instantiate(child.gameObject,pivots[i],false).name=child.name;
            }
            var spinner=root.gameObject.AddComponent<PolygonFerrisWheel>();spinner.Wheel=wheel;spinner.Gondolas=pivots;

            // Roller coaster loop: twin rails on white trestles round an oval.
            var coaster=new Vector3(655,0,-125);const int pts=72;var path=new Vector3[pts];
            for(int i=0;i<pts;i++)
            {
                float a=i*Mathf.PI*2/pts;var p=coaster+new Vector3(Mathf.Cos(a)*72,0,Mathf.Sin(a)*30);
                float lift=6+18*Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*2+.6f)),2)+6*Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*5)),3);
                p.y=TerrainHeight(p.x,p.z)+lift;path[i]=p;
            }
            var ck=chunks.At(coaster);
            for(int i=0;i<pts;i++)
            {
                var a=path[i];var b=path[(i+1)%pts];var side=Vector3.Cross(Vector3.up,(b-a).normalized)*.8f;
                ck.Beam(teal,a+side,b+side,.35f,.35f);ck.Beam(teal,a-side,b-side,.35f,.35f);
                ck.Beam(coral,a+Vector3.down*.7f,b+Vector3.down*.7f,.7f,.7f);
                if(i%3==0)
                {
                    float g=TerrainHeight(a.x,a.z);
                    ck.Beam(white,new Vector3(a.x,g-.5f,a.z)+side*1.8f,a+Vector3.down*.8f+side*.4f,.45f,.45f);
                    ck.Beam(white,new Vector3(a.x,g-.5f,a.z)-side*1.8f,a+Vector3.down*.8f-side*.4f,.45f,.45f);
                }
            }
            // Train of cars parked on the lift hill.
            for(int k=0;k<5;k++)
            {
                var a=path[8+k];var b=path[9+k];var c=(a+b)*.5f+Vector3.up*.6f;
                ck.Box(k==0?coral:ochre,c,new Vector3(1.9f,1.0f,Vector3.Distance(a,b)*.8f),Quaternion.LookRotation(b-a));
            }
            ReserveWorld(578,732,-160,-90);
            // Carousel, drop tower, stalls and the park gate.
            var car=new Vector3(500,TerrainHeight(500,-110),-110);var mk=chunks.At(car);
            mk.Column(white,car,9,9,.8f,12);
            for(int k=0;k<12;k++){float a=k*Mathf.PI*2/12;mk.Column(gold,car+new Vector3(Mathf.Cos(a)*7.6f,.8f,Mathf.Sin(a)*7.6f),.18f,.18f,4.4f,6);}
            mk.Column(coral,car+Vector3.up*.8f,1.6f,1.6f,4.6f,10);
            mk.Column(white,car+Vector3.up*5.2f,9.6f,9.6f,.6f,12);
            for(int k=0;k<12;k++)
            {
                float a0=k*Mathf.PI*2/12,a1=(k+1)*Mathf.PI*2/12;
                mk.Tri(k%2==0?coral:white,car+new Vector3(Mathf.Cos(a0)*9.6f,5.8f,Mathf.Sin(a0)*9.6f),car+new Vector3(Mathf.Cos(a1)*9.6f,5.8f,Mathf.Sin(a1)*9.6f),car+Vector3.up*10.5f,car+Vector3.up*6);
            }
            mk.Column(gold,car+Vector3.up*10.4f,.3f,.05f,1.6f,6);
            ReserveWorld(488,512,-122,-98);
            var drop=new Vector3(710,TerrainHeight(710,-52),-52);var dk=chunks.At(drop);
            dk.Column(white,drop,2.4f,1.8f,58,8);dk.Column(teal,drop+Vector3.up*18,4.2f,4.2f,2.2f,10);
            dk.Column(coral,drop+Vector3.up*58,2.6f,.3f,4,8);
            ReserveWorld(702,718,-60,-44);
            for(int k=0;k<8;k++)
            {
                var p=new Vector3(470+k*26,0,-30-Hash(k,1)*12);p.y=TerrainHeight(p.x,p.z);var sk=chunks.At(p);
                sk.Box(white,p+Vector3.up*1.4f,new Vector3(6,2.8f,4));
                var awn=gondolaColors[k%gondolaColors.Length];
                for(int st=0;st<4;st++)
                    sk.SheetQuad(st%2==0?awn:white,p+new Vector3(-3+st*1.5f,3.4f,-2.2f),p+new Vector3(-1.5f+st*1.5f,3.4f,-2.2f),p+new Vector3(-1.5f+st*1.5f,2.6f,-3.4f),p+new Vector3(-3+st*1.5f,2.6f,-3.4f));
                ReserveWorld(p.x-4,p.x+4,p.z-4,p.z+3);
            }
            // Striped marquee tents and planters round the plaza; no forest inside the park.
            for(int k=0;k<9;k++)
            {
                var p=new Vector3(460+k*30+Hash(k,7)*8,0,-150+Hash(k,8)*20);if(k%3==1)p.z=-75+Hash(k,9)*10;
                p.y=TerrainHeight(p.x,p.z);var tk=chunks.At(p);if(Reserved(new Vector2(p.x,p.z),3))continue;
                var c=gondolaColors[(k+2)%gondolaColors.Length];
                tk.Column(white,p,4.6f,4.6f,2.6f,8);
                for(int i=0;i<8;i++)
                {
                    float a0=i*Mathf.PI*2/8,a1=(i+1)*Mathf.PI*2/8;
                    tk.Tri(i%2==0?c:white,p+new Vector3(Mathf.Cos(a0)*5.4f,2.6f,Mathf.Sin(a0)*5.4f),p+new Vector3(Mathf.Cos(a1)*5.4f,2.6f,Mathf.Sin(a1)*5.4f),p+Vector3.up*6.2f,p+Vector3.up*3);
                }
                tk.Column(gold,p+Vector3.up*6.1f,.15f,.05f,1.4f,6);
            }
            for(int k=0;k<14;k++)
            {
                var p=new Vector3(455+k*20,0,-28);p.y=TerrainHeight(p.x,p.z);
                CherryTree(chunks.At(p),p,.85f);
            }
            ReserveWorld(ParkCentre.x-ParkHalf.x,ParkCentre.x+ParkHalf.x,ParkCentre.y-ParkHalf.y,ParkCentre.y+ParkHalf.y);
        }

        // ------------------------------------------------------------ lake, gates, pagoda
        static void Torii(PolygonSceneryKit kit,Vector3 basePos,float yaw,float scale,float footDepth=1)
        {
            var rot=Quaternion.Euler(0,yaw,0);
            Vector3 L(float x,float y,float z)=>basePos+rot*new Vector3(x,y,z)*scale;
            float span=6f,height=10.5f;
            foreach(int s in new[]{-1,1})
            {
                kit.Column(vermilion,L(s*span*.5f,-footDepth/scale,0),.62f*scale,.5f*scale,(height+footDepth/scale)*scale,10);
                kit.Column(charcoal,L(s*span*.5f,-footDepth/scale,0),.78f*scale,.74f*scale,(footDepth/scale+.9f)*scale,10);
            }
            kit.Box(vermilion,L(0,height*.74f,0),new Vector3(span+2.2f,.7f,.5f)*scale,rot);                 // tie beam
            kit.Box(vermilion,L(0,height-.15f,0),new Vector3(span+3.4f,.75f,.8f)*scale,rot);                // under-lintel
            kit.Box(charcoal,L(0,height+.55f,0),new Vector3(span+4.4f,.6f,1.1f)*scale,rot);                 // top lintel
            foreach(int s in new[]{-1,1})
                kit.Box(charcoal,L(s*(span*.5f+2.5f),height+.75f,0),new Vector3(1.4f,.55f,1.1f)*scale,rot*Quaternion.Euler(0,0,s*-14));
            kit.Box(vermilion,L(0,height*.87f,0),new Vector3(.7f,height*.2f,.4f)*scale,rot);                // centre strut
            kit.Box(charcoal,L(0,height*.87f,-.25f),new Vector3(1.4f,1.5f,.12f)*scale,rot);                 // plaque
        }

        static void StoneLantern(PolygonSceneryKit kit,Vector3 p,float s=1)
        {
            kit.Column(rock,p,.55f*s,.45f*s,.35f*s,6);
            kit.Column(rock,p+Vector3.up*.35f*s,.18f*s,.18f*s,1.0f*s,6);
            kit.Chamfered(rock,p+Vector3.up*1.35f*s,.4f*s,.4f*s,.4f*s,.4f*s,0,.25f*s,.1f*s);
            kit.Box(lampGlow,p+Vector3.up*1.85f*s,new Vector3(.5f,.5f,.5f)*s);
            kit.Chamfered(rock,p+Vector3.up*1.6f*s,.4f*s,.4f*s,.4f*s,.4f*s,0,.1f*s,.1f*s);
            kit.Chamfered(rock,p+Vector3.up*2.1f*s,.75f*s,.75f*s,.08f*s,.08f*s,0,.6f*s,.2f*s);
        }

        static Vector3 teaHouse;
        static void BuildLake(Transform world,Chunks chunks)
        {
            // Water surface: an oval fan slightly larger than the basin, so the banks hide its rim.
            var wk=new PolygonSceneryKit("Sakura lake");const int seg=40;
            var c=new Vector3(LakeCentre.x,LakeLevel,LakeCentre.y);
            for(int i=0;i<seg;i++)
            {
                float a0=i*Mathf.PI*2/seg,a1=(i+1)*Mathf.PI*2/seg;
                var p0=c+new Vector3(Mathf.Cos(a0)*LakeRx*1.2f,0,Mathf.Sin(a0)*LakeRz*1.2f);
                var p1=c+new Vector3(Mathf.Cos(a1)*LakeRx*1.2f,0,Mathf.Sin(a1)*LakeRz*1.2f);
                wk.Tri(lakeWater,c,p0,p1,c+Vector3.down);
            }
            var group=new GameObject("Sakura lake water").transform;group.SetParent(world,false);
            wk.Emit(group);foreach(Transform child in group)child.name="Sakura lake water surface";
            ReserveWorld(LakeCentre.x-LakeRx*1.3f,LakeCentre.x+LakeRx*1.3f,LakeCentre.y-LakeRz*1.3f,LakeCentre.y+LakeRz*1.3f);
            // A great gate standing in the water, facing the back straight.
            var kit=chunks.At(c);
            Torii(kit,new Vector3(LakeCentre.x+10,LakeLevel,LakeCentre.y+16),-35,1.45f,3.5f);
            // Lanterns round the shore and a tea house on the north bank with a jetty.
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10+.2f;var p=new Vector3(LakeCentre.x+Mathf.Cos(a)*LakeRx*1.32f,0,LakeCentre.y+Mathf.Sin(a)*LakeRz*1.32f);
                p.y=TerrainHeight(p.x,p.z);StoneLantern(chunks.At(p),p,1.2f);
            }
            var th=new Vector3(LakeCentre.x-8,0,LakeCentre.y+LakeRz*1.36f);th.y=TerrainHeight(th.x,th.z);var tk=chunks.At(th);teaHouse=new Vector3(th.x,0,th.z);
            tk.Box(timber,th+Vector3.up*.2f,new Vector3(14,1.4f,10));
            for(int k=-1;k<=1;k+=2)for(int j=-1;j<=1;j+=2)tk.Column(timber,th+new Vector3(k*6.4f,.9f,j*4.4f),.22f,.22f,3,6);
            tk.Sleeve(plaster,th+new Vector3(0,2.4f,1.5f),new Vector3(10,3,5));
            tk.Chamfered(roofGrey,th+Vector3.up*3.9f,9.4f,7.4f,4.5f,1.2f,0,3.6f,.6f);
            tk.Box(timber,new Vector3(th.x,LakeLevel+.5f,th.z-14),new Vector3(3,.3f,18));
            ReserveWorld(th.x-12,th.x+12,th.z-26,th.z+9);
        }

        static void BuildShrines(Transform world,Chunks chunks)
        {
            // Five-storey pagoda on the hill north of the circuit, with a shrine hall and gate.
            var site=new Vector3(PagodaSite.x,0,PagodaSite.y);site.y=TerrainHeight(site.x,site.z);
            var kit=chunks.At(site);
            kit.Chamfered(rock,site+Vector3.down*3,13,13,12,12,0,4.2f,1.5f);
            float y=1.2f;
            for(int k=0;k<5;k++)
            {
                float w=6.2f-k*.75f,h=k==0?4.4f:3.4f;
                kit.Box(vermilion,site+Vector3.up*(y+h*.5f),new Vector3(w*2,h,w*2));
                kit.Sleeve(white,site+Vector3.up*(y+h*.62f),new Vector3(w*2+.3f,h*.22f,w*2+.3f));
                kit.Chamfered(charcoal,site+Vector3.up*(y+h),w+3.4f,w+3.4f,w+.8f,w+.8f,-.2f,1.3f,1.2f);
                kit.Chamfered(gold,site+Vector3.up*(y+h),w+3.5f,w+3.5f,w+3.4f,w+3.4f,-.45f,-.2f,1.2f);
                y+=h+1.1f;
            }
            kit.Column(gold,site+Vector3.up*y,.45f,.12f,9,8);
            for(int r=0;r<6;r++)kit.Column(gold,site+Vector3.up*(y+1.2f+r*1.15f),.75f-r*.07f,.75f-r*.07f,.22f,8);
            ReserveWorld(site.x-16,site.x+16,site.z-16,site.z+16);
            var hall=site+new Vector3(34,0,6);hall.y=TerrainHeight(hall.x,hall.z);
            kit.Box(rock,hall+Vector3.up*-1,new Vector3(22,3,16));
            kit.Box(plaster,hall+Vector3.up*2.6f,new Vector3(18,4.4f,12));
            for(int k=-3;k<=3;k++)kit.Column(vermilion,hall+new Vector3(k*2.9f,.5f,-6.3f),.25f,.25f,4.6f,6);
            kit.Chamfered(roofGrey,hall+Vector3.up*4.8f,12.4f,8.6f,9,1.2f,0,4.6f,.5f);
            kit.Box(gold,hall+Vector3.up*9.5f,new Vector3(18.2f,.3f,.5f));
            ReserveWorld(hall.x-14,hall.x+14,hall.z-12,hall.z+10);
            for(int k=0;k<3;k++)
            {
                var g=site+new Vector3(14,0,-30-k*16);g.y=TerrainHeight(g.x,g.z);Torii(kit,g,0,.62f);
                StoneLantern(kit,g+new Vector3(-6,0,4),1);StoneLantern(kit,g+new Vector3(6,0,4),1);
            }
            ReserveWorld(site.x+6,site.x+22,site.z-70,site.z-14);
            // A big gate on the hill outside the hairpin.
            var hp=new Vector3(-585,0,120);hp.y=TerrainHeight(hp.x,hp.z);
            var hk=chunks.At(hp);Torii(hk,hp,90,1.7f,2);StoneLantern(hk,hp+new Vector3(10,0,-12),1.4f);StoneLantern(hk,hp+new Vector3(10,0,12),1.4f);
            ReserveWorld(hp.x-14,hp.x+16,hp.z-18,hp.z+18);
        }

        // ------------------------------------------------------------ countryside
        static void FarmHouse(PolygonSceneryKit kit,Vector3 c,float yaw,float s)
        {
            var rot=Quaternion.Euler(0,yaw,0);
            Vector3 L(float x,float y,float z)=>c+rot*new Vector3(x,y,z)*s;
            kit.Box(plaster,L(0,1.2f,0),new Vector3(12,4.4f,8)*s,rot);
            kit.Box(timber,L(0,3.5f,0),new Vector3(12.2f,.35f,8.2f)*s,rot);
            kit.Box(timber,L(0,.1f,0),new Vector3(12.3f,.6f,8.3f)*s,rot);
            // Gabled roof with deep eaves.
            kit.Prism(roofGrey,new[]{L(-7.4f,3.6f,-5.4f),L(-7.4f,3.6f,5.4f),L(-7.4f,7.2f,0)},new[]{L(7.4f,3.6f,-5.4f),L(7.4f,3.6f,5.4f),L(7.4f,7.2f,0)});
            kit.Box(charcoal,L(0,7.25f,0),new Vector3(15.2f,.35f,.6f)*s,rot);
            kit.Box(glassDark,L(-3,1.8f,-4.06f),new Vector3(3,1.2f,.1f)*s,rot);
            kit.Box(timber,L(2.5f,1.5f,-4.06f),new Vector3(2.2f,2.6f,.1f)*s,rot);
        }

        static void BuildPaddies(Chunks chunks)
        {
            // Terraced rice fields south-west of the circuit, with farmhouses between them.
            const float cx=34,cz=22;
            for(float x=-580;x<-60;x+=cx)
                for(float z=-236;z<-12;z+=cz)
                {
                    var c=new Vector3(x+cx*.5f,0,z+cz*.5f);
                    if(Offset(c)<60||Reserved(new Vector2(c.x,c.z),2))continue;
                    var kit=chunks.At(c);
                    if(Hash(x,z,11)<.07f)
                    {
                        c.y=TerrainHeight(c.x,c.z);FarmHouse(kit,c+Vector3.down*.6f,Hash(x,z,12)<.5f?0:90,1);
                        CherryTree(kit,c+new Vector3(9,0,7),.9f);continue;
                    }
                    float hx=cx*.5f-.6f,hz=cz*.5f-.6f;
                    float lo=float.MaxValue,hi=float.MinValue;
                    foreach(var (u,v) in new[]{(-hx,-hz),(hx,-hz),(hx,hz),(-hx,hz),(0f,0f)}){float g=TerrainHeight(c.x+u,c.z+v);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
                    if(hi-lo>5)continue;
                    var m=Hash(x,z,13)<.35f?paddyFlooded:paddy;
                    kit.Box(dirt,new Vector3(c.x,(lo-1.2f+hi+.35f)*.5f,c.z),new Vector3(cx-.2f,hi+.35f-(lo-1.2f),cz-.2f));
                    kit.Box(m,new Vector3(c.x,hi+.45f,c.z),new Vector3(cx-1.6f,.2f,cz-1.6f));
                }
            ReserveWorld(-580,-60,-236,-12);
            // Scattered farmhouses on the far side of the valley.
            foreach(var (p,yaw) in new[]{(new Vector3(880,0,330),20f),(new Vector3(900,0,470),0f),(new Vector3(260,0,-160),90f),
                (new Vector3(-180,0,600),15f),(new Vector3(140,0,640),-10f),(new Vector3(-640,0,420),80f)})
            {
                var c=p;c.y=TerrainHeight(c.x,c.z);if(Offset(c)<40)continue;
                FarmHouse(chunks.At(c),c+Vector3.down*.6f,yaw,1.1f);ReserveWorld(c.x-12,c.x+12,c.z-12,c.z+12);
            }
        }

        static void CherryTree(PolygonSceneryKit kit,Vector3 p,float s)
        {
            var blossom=Hash(p.x,p.z,21)<.33f?sakuraPale:Hash(p.x,p.z,22)<.5f?sakura:sakuraDeep;
            kit.Column(bark,p+Vector3.down*.3f,.32f*s,.22f*s,2.6f*s,5);
            for(int b=0;b<3;b++)
            {
                float a=b*2.09f+Hash(p.x,p.z,b)*1.2f;
                var c=p+new Vector3(Mathf.Cos(a)*1.6f*s,3.1f*s+b*.5f*s,Mathf.Sin(a)*1.6f*s);
                float r=(2.0f+Hash(p.z,p.x,b)*.7f)*s;
                kit.Beam(bark,p+Vector3.up*2.1f*s,c,.2f*s,.2f*s);
                kit.Column(blossom,c+Vector3.down*r*.35f,r*.55f,r,r*.45f,6);
                kit.Column(blossom,c+Vector3.up*r*.1f,r,r*.35f,r*.55f,6);
            }
        }
        static void PineTree(PolygonSceneryKit kit,Vector3 p,float h,bool simple)
        {
            var m=Hash(p.x,p.z,31)<.5f?pine:pineDark;
            if(simple){kit.Column(m,p+Vector3.down*.5f,h*.26f,.1f,h,6);return;}
            kit.Column(bark,p+Vector3.down*.4f,.3f,.18f,h*.3f,5);
            for(int t=0;t<3;t++)
            {
                float y=h*(.22f+t*.23f),r=h*(.27f-t*.06f);
                kit.Column(m,p+Vector3.up*y,r,.12f,h*(.42f-t*.06f),6);
            }
        }

        static void BuildTrees(Chunks chunks)
        {
            // Cherry trees lining the circuit (not at the bridge or among the stands).
            for(float d=8;d<PolygonCircuit.Length;d+=26)
            {
                if(Mathf.Abs(UpperDelta(d))<200||Mathf.Abs(Wrap(d,crossLower))<60)continue;
                foreach(float side in new[]{-1f,1f})
                {
                    float lateral=side*(21.5f+Hash(d,side)*5);var p=At(d,lateral,0);
                    if(Reserved(new Vector2(p.x,p.z),3)||Offset(p)<20||LakeDistance(p.x,p.z)<1.4f)continue;
                    p.y=TerrainHeight(p.x,p.z);CherryTree(chunks.At(p),p,.95f+Hash(p.x,p.z,5)*.35f);
                }
            }
            // Cherry groves round the lake and on the pagoda hill.
            for(int i=0;i<28;i++)
            {
                float a=i*Mathf.PI*2/28+Hash(i,3)*.15f,r=1.45f+Hash(i,4)*.5f;
                var p=new Vector3(LakeCentre.x+Mathf.Cos(a)*LakeRx*r,0,LakeCentre.y+Mathf.Sin(a)*LakeRz*r);
                if(Offset(p)<22||LakeDistance(p.x,p.z)<1.38f||(p-teaHouse).sqrMagnitude<16*16)continue;
                p.y=TerrainHeight(p.x,p.z);CherryTree(chunks.At(p),p,1.1f);
            }
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24,r=26+Hash(i,6)*40;
                var p=new Vector3(PagodaSite.x+Mathf.Cos(a)*r,0,PagodaSite.y+Mathf.Sin(a)*r);
                if(Reserved(new Vector2(p.x,p.z),2))continue;
                p.y=TerrainHeight(p.x,p.z);CherryTree(chunks.At(p),p,1.0f+Hash(i,7)*.3f);
            }
            // Pine woods on the hills, thinning near the track.
            int count=0;
            for(float x=-1300;x<=1600;x+=22)
                for(float z=-900;z<=1400;z+=22)
                {
                    bool near=x>-660&&x<960&&z>-260&&z<780;
                    if(!near&&(Hash(x,z,40)<.55f||((int)(x/22)+(int)(z/22))%2!=0))continue;
                    var p=new Vector3(x+(Hash(x,z,41)-.5f)*14,0,z+(Hash(x,z,42)-.5f)*14);
                    float cluster=Mathf.Sin(p.x*.013f+1)*Mathf.Sin(p.z*.011f+2);
                    if(cluster<-.1f)continue;
                    if(near)
                    {
                        float o=Offset(p);if(o<42)continue;
                        if(Reserved(new Vector2(p.x,p.z),4)||LakeDistance(p.x,p.z)<1.5f)continue;
                        if(Hash(x,z,43)>.25f+Mathf.InverseLerp(40,220,o)*.5f)continue;
                    }
                    p.y=TerrainHeight(p.x,p.z);
                    if(p.y>150)continue;   // tree line
                    PineTree(chunks.At(p),p,9+Hash(x,z,44)*8,!near);count++;
                }
        }

        // ------------------------------------------------------------ boards and marshal posts
        static void BuildBoardsAndPosts(Transform world,Chunks chunks)
        {
            var ink=Mat("Board ink",new Color(.98f,.97f,.93f));float last=-999;
            for(float d=0;d<PolygonCircuit.Length;d+=3)
            {
                if(Mathf.Abs(PolygonCircuit.Curvature(d))<1/32f||d-last<150)continue;
                last=d;float outside=OutsideSign(d+6);
                foreach(int m in new[]{150,100,50})
                {
                    float bd=d-m;if(OnBridge(bd))continue;
                    var b=Frame(world,"Sakura corner board "+m,bd,outside*14.6f,0);
                    PolygonWorld.Box("Board post",b,new Vector3(0,1.2f,0),new Vector3(.16f,2.4f,.16f),navy);
                    PolygonWorld.Box("Board panel",b,new Vector3(0,2.7f,0),new Vector3(1.8f,1.1f,.16f),vermilion);
                    PolygonWorld.Label("Board lettering",b,m.ToString(),new Vector3(0,2.7f,-.13f),.07f,ink.color);
                }
            }
            for(float d=140;d<PolygonCircuit.Length-60;d+=270)
            {
                if(Mathf.Abs(UpperDelta(d))<60||Mathf.Abs(Wrap(d,crossLower))<40||(d>PitStart-20&&d<PitEnd+40))continue;
                float side=OutsideSign(d);var p=At(d,side*(Wall1+3),0);p.y=Mathf.Min(p.y,TerrainHeight(p.x,p.z));
                if(Reserved(new Vector2(p.x,p.z),2))continue;
                var kit=chunks.At(p);var rot=Quaternion.LookRotation(T(d));
                kit.Box(white,p+Vector3.up*1.2f,new Vector3(2.2f,2.4f,2.2f),rot);
                kit.Box(ochre,p+Vector3.up*2.55f,new Vector3(2.8f,.3f,2.8f),rot);
                kit.Beam(steel,p+Vector3.up*2.7f,p+Vector3.up*5.2f,.08f,.08f);
                kit.SheetQuad(lime,p+Vector3.up*5.1f,p+Vector3.up*5.1f+rot*Vector3.forward*1.4f,p+Vector3.up*4.2f+rot*Vector3.forward*1.4f,p+Vector3.up*4.2f);
            }
        }

        // ------------------------------------------------------------ entry point
        public static Transform Build(Transform circuitRoot)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.Sakura)
                throw new System.InvalidOperationException("Select the Sakura layout before building its scenery.");
            CreatePalette();reserved.Clear();sites.Clear();
            FindCrossover();
            PrepareTerrainSamples();
            PlanSites();
            PrepareGround();
            var world=new GameObject(SceneryName).transform;world.SetParent(circuitRoot,false);
            propsRoot=new GameObject("Sakura props").transform;propsRoot.SetParent(world,false);
            var animated=new GameObject(AnimatedName).transform;animated.SetParent(circuitRoot,false);
            var chunks=new Chunks();
            BuildRoad(chunks);
            BuildBarriers(chunks);
            BuildBridge(world,chunks);
            BuildGantry(world);
            BuildPits(world,chunks);
            BuildStands(world);
            BuildAmusementPark(world,animated,chunks);
            BuildLake(world,chunks);
            BuildShrines(world,chunks);
            BuildBoardsAndPosts(world,chunks);
            BuildPaddies(chunks);
            BuildTrees(chunks);
            BuildTerrain(chunks);
            chunks.Emit(world);
            reserved.Clear();sites.Clear();tPts=null;tH=null;tD=null;tZone=null;groundLeft=groundRight=null;
            Debug.Log($"Sakura crossover: lower road at {crossLower:0} m ({E(crossLower):0.0} m high), bridge at {crossUpper:0} m ({E(crossUpper):0.0} m high).");
            return world;
        }
    }
}
