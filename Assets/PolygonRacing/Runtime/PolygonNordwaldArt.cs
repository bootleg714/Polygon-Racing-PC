using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // "NORDWALD RING": a long, narrow forest circuit inspired by the classic German mountain course
    // (relentless corners in dense woods, big climbs and plunges, a banked concrete hairpin at the top
    // of the hill, spectators camping beside the track, a long straight home). Original layout,
    // buildings and landmarks: castle on the hill inside the loop, a village with a church spire in
    // the valley, country road bridges over the track, camping meadows. Editor-time generation baked
    // per material into spatial chunks, like the other circuits.
    //
    // Cross-section (metres from the racing line, both sides; heights follow road elevation and bank):
    //   0-7 road (edge line 6.7-7) | 7-8.2 kerb at corners | 8.2-9.5 grass verge |
    //   9.5-9.8 double steel barrier (driver limit 7 + 2.5 = 9.5) | 10.4+ forest floor.
    public static class PolygonNordwaldArt
    {
        public const string SceneryName="Circuit - editable scenery";
        const float Road=7f,Kerb=8.2f,Barrier0=9.5f,Barrier1=9.8f,Edge=10.4f,ChunkSize=300;

        static readonly Vector2 CastleSite=new Vector2(110,560);
        static readonly Vector2 VillageSite=new Vector2(330,690);
        static readonly Vector2 CampSite=new Vector2(-735,800);

        static Material asphalt,patch,concrete,concreteDark,line,kerbRed,kerbYellow,verge,forestFloor,meadow,grassDark,rock,dirt,
            armco,armcoBlue,fence,white,cream,charcoal,navy,coral,teal,ochre,yellow,green,red,lilac,stone,stoneDark,slate,plaster,timber,
            glassDark,steel,lampGlow,spruce,spruceDark,pine,beech,beechLight,bark,water,skinLight,skinTan,skinDeep,hair,jeans,canvas,ochrePlaster;
        static Material[] teamColors,shirts,skins,tentColors,vanStripes;
        static readonly List<Rect> reserved=new List<Rect>();

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit)){kit=new PolygonSceneryKit("Nordwald "+key.x+"_"+key.y);kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform root)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Nordwald block "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(root,false);pair.Value.Emit(group);
                }
                kits.Clear();
            }
        }

        static Material Mat(string name,Color color)=>PolygonWorld.Paint("NW "+name,color);
        static void CreatePalette()
        {
            asphalt=Mat("Asphalt",new Color(.21f,.22f,.24f));
            patch=Mat("Asphalt patch",new Color(.17f,.18f,.20f));
            concrete=Mat("Banked concrete",new Color(.66f,.66f,.64f));
            concreteDark=Mat("Concrete joints",new Color(.42f,.42f,.42f));
            line=Mat("Road paint",new Color(.95f,.95f,.92f));
            kerbRed=Mat("Kerb red",new Color(.84f,.12f,.12f));
            kerbYellow=Mat("Kerb yellow",new Color(.96f,.80f,.16f));
            verge=Mat("Verge grass",new Color(.36f,.56f,.28f));
            forestFloor=Mat("Forest floor",new Color(.26f,.36f,.20f));
            meadow=Mat("Meadow",new Color(.48f,.64f,.32f));
            grassDark=Mat("Hill green",new Color(.30f,.46f,.26f));
            rock=Mat("Grey rock",new Color(.50f,.49f,.46f));
            dirt=Mat("Earth bank",new Color(.46f,.36f,.26f));
            armco=Mat("Barrier steel",new Color(.76f,.78f,.80f));
            armcoBlue=Mat("Barrier blue",new Color(.14f,.30f,.62f));
            fence=Mat("Catch fence",new Color(.20f,.22f,.25f));
            white=Mat("White",new Color(.96f,.96f,.95f));
            cream=Mat("Ivory",new Color(1,.95f,.86f));
            charcoal=Mat("Charcoal",new Color(.13f,.14f,.16f));
            navy=Mat("Navy",new Color(.09f,.17f,.32f));
            coral=Mat("Coral",new Color(.97f,.38f,.34f));
            teal=Mat("Teal",new Color(.04f,.47f,.52f));
            ochre=Mat("Ochre",new Color(.95f,.68f,.30f));
            yellow=Mat("Signal yellow",new Color(.98f,.84f,.20f));
            green=Mat("Racing green",new Color(.10f,.40f,.24f));
            red=Mat("Signal red",new Color(.82f,.14f,.14f));
            lilac=Mat("Lilac",new Color(.58f,.46f,.80f));
            stone=Mat("Castle stone",new Color(.62f,.58f,.52f));
            stoneDark=Mat("Castle stone dark",new Color(.48f,.45f,.41f));
            slate=Mat("Slate roof",new Color(.22f,.24f,.28f));
            plaster=Mat("White plaster",new Color(.94f,.92f,.86f));
            timber=Mat("Dark timber",new Color(.28f,.19f,.13f));
            glassDark=Mat("Glass dark",new Color(.08f,.12f,.17f));
            steel=Mat("Steel grey",new Color(.58f,.60f,.63f));
            lampGlow=Mat("Lamp glow",new Color(1,.92f,.70f));lampGlow.SetColor("_EmissionColor",new Color(.8f,.65f,.4f));
            spruce=Mat("Spruce",new Color(.12f,.30f,.20f));
            spruceDark=Mat("Spruce dark",new Color(.08f,.22f,.16f));
            pine=Mat("Pine",new Color(.18f,.36f,.22f));
            beech=Mat("Beech",new Color(.26f,.46f,.20f));
            beechLight=Mat("Beech light",new Color(.40f,.58f,.24f));
            bark=Mat("Bark",new Color(.33f,.24f,.17f));
            water=Mat("Stream water",new Color(.14f,.36f,.42f));
            skinLight=Mat("Crowd skin light",new Color(.96f,.80f,.66f));
            skinTan=Mat("Crowd skin tan",new Color(.80f,.58f,.42f));
            skinDeep=Mat("Crowd skin deep",new Color(.46f,.31f,.21f));
            hair=Mat("Crowd hair",new Color(.14f,.10f,.08f));
            jeans=Mat("Jeans",new Color(.18f,.24f,.38f));
            canvas=Mat("Canvas",new Color(.86f,.82f,.70f));
            ochrePlaster=Mat("Pale ochre plaster",new Color(.92f,.82f,.62f));
            teamColors=new[]{coral,teal,ochre,navy,lilac,green};
            shirts=new[]{coral,teal,ochre,navy,white,lilac,green,yellow,red,charcoal};
            skins=new[]{skinLight,skinTan,skinDeep};
            tentColors=new[]{coral,teal,ochre,green,yellow,lilac,navy,red};
            vanStripes=new[]{coral,teal,navy,ochre,green};
        }

        // ------------------------------------------------------------ route helpers
        static Vector3 P(float d)=>PolygonCircuit.AtDistance(d);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float E(float d)=>PolygonCircuit.Elevation(d);
        // Road surface height across the section, including the bank.
        static float H(float d,float lateral)=>E(d)+lateral*PolygonCircuit.BankSlope(d);
        static Vector3 At(float d,float lateral,float y)=>P(d)+S(d)*lateral+Vector3.up*(H(d,lateral)+y);
        static Vector3 Flat(float d,float lateral){var p=P(d)+S(d)*lateral;p.y=0;return p;}
        static float Offset(Vector3 p){PolygonCircuit.ProjectLapFlat(p,out float o);return o;}
        static float DistanceAt(Vector3 p)=>PolygonCircuit.ProjectLapFlat(p,out _)*PolygonCircuit.Length;
        static float Hash(float x,float z,int salt=0)=>Mathf.Abs(Mathf.Sin(x*12.9898f+z*78.233f+salt*37.719f)*43758.5453f)%1f;
        static float DistanceAtKnot(float k)=>PolygonCircuit.DistanceAtKnot(k);

        static void Ribbon(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float y)
        {
            var a=At(d0,l0,y);var b=At(d0,l1,y);var c=At(d1,l1,y);var e=At(d1,l0,y);
            kit.Quad(m,a,b,c,e,(a+b+c+e)*.25f+Vector3.down);
        }
        static void Band(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,Material m,float y=.025f)
        {if(side>0)Ribbon(kit,m,d0,d1,a,b,y);else Ribbon(kit,m,d0,d1,-b,-a,y);}
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
        static void ReserveAround(Vector2 c,float r){reserved.Add(Rect.MinMaxRect(c.x-r,c.y-r,c.x+r,c.y+r));}

        // ------------------------------------------------------------ levelled sites
        struct Site{public Vector3 Centre,AxisX,AxisZ;public float HalfX,HalfZ,Height,Edge;}
        static readonly List<Site> sites=new List<Site>();
        static void AddSite(Vector3 centre,Vector3 forward,float halfX,float halfZ,float height,float edge=25)
        {
            forward.y=0;forward.Normalize();
            sites.Add(new Site{Centre=new Vector3(centre.x,0,centre.z),AxisZ=forward,AxisX=Vector3.Cross(Vector3.up,forward),HalfX=halfX,HalfZ=halfZ,Height=height,Edge=edge});
        }
        // Sites pull the ground toward their height (up or down) with a soft edge.
        static float ApplySites(float x,float z,float h)
        {
            foreach(var s in sites)
            {
                var d=new Vector3(x,0,z)-s.Centre;
                float ox=Mathf.Max(0,Mathf.Abs(Vector3.Dot(d,s.AxisX))-s.HalfX),oz=Mathf.Max(0,Mathf.Abs(Vector3.Dot(d,s.AxisZ))-s.HalfZ);
                float outside=Mathf.Sqrt(ox*ox+oz*oz);if(outside>s.Edge)continue;
                h=Mathf.Lerp(s.Height,h,Mathf.SmoothStep(0,1,outside/s.Edge));
            }
            return h;
        }

        // ------------------------------------------------------------ terrain
        const int TerrainSamples=768;
        static Vector3[] tPts;static float[] tH,tD,tLow;
        static void PrepareTerrainSamples()
        {
            tPts=new Vector3[TerrainSamples+1];tH=new float[TerrainSamples+1];tD=new float[TerrainSamples+1];tLow=new float[TerrainSamples+1];
            for(int i=0;i<=TerrainSamples;i++)
            {
                float d=i*PolygonCircuit.Length/TerrainSamples;var p=P(d);p.y=0;
                tPts[i]=p;tH[i]=E(d);tD[i]=d;
                // Lowest point of the banked cross-section, so the ground never covers the low edge.
                tLow[i]=E(d)-Mathf.Abs(PolygonCircuit.BankSlope(d))*Edge;
            }
        }
        static void Nearest(Vector3 p,out float offset,out float distance,out float low)
        {
            float best=float.MaxValue;distance=0;low=0;
            for(int i=0;i<TerrainSamples;i++)
            {
                var ab=tPts[i+1]-tPts[i];float t=Mathf.Clamp01(Vector3.Dot(p-tPts[i],ab)/Mathf.Max(ab.sqrMagnitude,.0001f));
                float dd=(p-(tPts[i]+ab*t)).sqrMagnitude;
                if(dd<best){best=dd;distance=Mathf.Lerp(tD[i],tD[i+1],t);low=Mathf.Lerp(tLow[i],tLow[i+1],t);}
            }
            offset=Mathf.Sqrt(best);
        }
        static float Hills(float x,float z)
        {
            // Rolling wooded hills, a few old volcanic cones, and the castle hill inside the loop.
            float h=(Mathf.Sin(x*.0075f+.6f)*Mathf.Sin(z*.0068f+1.9f)*.5f+.5f)*34+(Mathf.Sin(x*.019f+z*.013f)*.5f+.5f)*9;
            foreach(var (cx,cz,r,ht) in new[]{(110f,560f,240f,62f),(-1150f,1500f,420f,170f),(1500f,1300f,380f,140f),(-1300f,-500f,360f,120f),(1400f,-600f,420f,150f),(300f,1900f,500f,180f)})
            {
                float dist=new Vector2(x-cx,z-cz).magnitude;
                h+=Mathf.SmoothStep(1,0,Mathf.Clamp01(dist/r))*ht;
            }
            return h;
        }
        public static float TerrainHeight(float x,float z)
        {
            var p=new Vector3(x,0,z);
            Nearest(p,out float o,out float d,out float low);
            float sum=0,weight=0;
            for(int i=0;i<TerrainSamples;i+=3){float w=1/((tPts[i]-p).sqrMagnitude+1600);sum+=tH[i]*w;weight+=w;}
            float blend=sum/weight;
            float h=Mathf.Lerp(low-.4f,blend,Mathf.SmoothStep(0,1,Mathf.InverseLerp(11,48,o)))+Hills(x,z)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,170,o));
            // Levelled sites may only lower the ground right beside the road (the pit apron);
            // raising it there would bury the track, so their pull upward fades out within 60 m.
            float site=ApplySites(x,z,h);
            return site<h?site:Mathf.Lerp(h,site,Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,60,o)));
        }

        static void BuildTerrain(Chunks chunks)
        {
            const float fx0=-900,fx1=1200,fz0=-260,fz1=1420,fine=12;
            GridTerrain(chunks,fx0,fx1,fz0,fz1,fine,null,true);
            GridTerrain(chunks,-2600,2800,-2200,3000,48,Rect.MinMaxRect(fx0,fz0,fx1,fz1),false);
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
            if(n.y<.5f)m=rock;
            else if(n.y<.66f)m=dirt;
            else if(far)m=centre.y>150?grassDark:forestFloor;
            else
            {
                float o=Offset(centre);
                bool clearing=Reserved(new Vector2(centre.x,centre.z),0)||IsMeadow(centre.x,centre.z);
                m=o<11?verge:clearing?meadow:forestFloor;
            }
            kit.Tri(m,a,b,c,centre+Vector3.down);
        }
        // Open meadows break up the forest here and there (no trees, lighter grass).
        static bool IsMeadow(float x,float z)=>Mathf.Sin(x*.0105f+2.1f)*Mathf.Sin(z*.0093f+.7f)>.62f;

        // ------------------------------------------------------------ road, verge, embankments
        static float[] groundLeft,groundRight;
        static void PrepareGround()
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;
            groundLeft=new float[n+1];groundRight=new float[n+1];
            for(int i=0;i<=n;i++)
            {
                float d=i*step;
                var l=At(d,-Edge,0);var r=At(d,Edge,0);
                groundLeft[i]=TerrainHeight(l.x,l.z)-H(d,-Edge);groundRight[i]=TerrainHeight(r.x,r.z)-H(d,Edge);
            }
        }
        static bool Corner(float d)=>Mathf.Abs(PolygonCircuit.Curvature(d))>1/75f||Mathf.Abs(PolygonCircuit.Curvature(d-10))>1/75f||Mathf.Abs(PolygonCircuit.Curvature(d+10))>1/75f;
        static float OutsideSign(float d)
        {
            float c=PolygonCircuit.Curvature(d)+PolygonCircuit.Curvature(d-10)+PolygonCircuit.Curvature(d+10);
            return c>0?-1:1;
        }
        static bool Banked(float d)=>Mathf.Abs(PolygonCircuit.BankDegrees(d))>4;
        const float PitStart=-20,PitEnd=250;
        static bool InPits(float d)=>d>PitStart&&d<PitEnd;

        static void BuildRoad(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;const float checkerEnd=1.6f;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));
                bool corner=Corner(dm),bank=Banked(dm);
                foreach(float side in new[]{-1f,1f})
                {
                    Band(kit,side,d0,d1,Road-.3f,Road,line);
                    Band(kit,side,d0,d1,Road,Kerb,corner&&!bank?((i/2)%2==0?kerbRed:line):bank?concreteDark:verge,.03f);
                    Band(kit,side,d0,d1,Kerb,Edge,verge,.02f);
                    if(side<0&&InPits(dm))continue;   // the pit apron closes this side
                    float g0=side<0?groundLeft[i]:groundRight[i],g1=side<0?groundLeft[i+1]:groundRight[i+1];
                    float l=side*Edge;
                    if(Mathf.Min(g0,g1)<-.8f)
                    {
                        // Earth embankment sloping down to the ground (1:1.4), grassed.
                        float run0=Mathf.Max(0,-g0)*1.4f+.5f,run1=Mathf.Max(0,-g1)*1.4f+.5f;
                        var a=At(d0,l,.02f);var b=At(d1,l,.02f);
                        var c=At(d1,side*(Edge+run1),0);c.y=b.y+g1-.6f;
                        var e=At(d0,side*(Edge+run0),0);e.y=a.y+g0-.6f;
                        kit.Quad(verge,a,b,c,e,(a+b)*.5f+Vector3.down*6);
                        kit.Sheet(dirt,a,b,At(d1,l,Mathf.Min(-1.5f,g1-1)));kit.Sheet(dirt,a,At(d1,l,Mathf.Min(-1.5f,g1-1)),At(d0,l,Mathf.Min(-1.5f,g0-1)));
                    }
                    else
                    {
                        kit.Sheet(dirt,At(d0,l,.02f),At(d1,l,.02f),At(d1,l,Mathf.Min(-1.5f,g1-1)));
                        kit.Sheet(dirt,At(d0,l,.02f),At(d1,l,Mathf.Min(-1.5f,g1-1)),At(d0,l,Mathf.Min(-1.5f,g0-1)));
                    }
                }
                if(d0<checkerEnd)
                {
                    const int rows=2,cols=14;
                    for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
                    {
                        float a0=d0+checkerEnd*r/rows,a1=d0+checkerEnd*(r+1)/rows,l0=-(Road-.3f)+c*(Road-.3f)*2/cols,l1=l0+(Road-.3f)*2/cols;
                        Ribbon(kit,(r+c)%2==0?line:charcoal,a0,a1,l0,l1,.025f);
                    }
                    d0=checkerEnd;
                }
                if(bank)
                {
                    // Concrete slabs with dark joints across the banked hairpin.
                    const int lanes=6;float w=(Road-.3f)*2/lanes;
                    for(int k=0;k<lanes;k++)
                    {
                        float l0=-(Road-.3f)+k*w;
                        Ribbon(kit,concrete,d0,d1,l0+.08f,l0+w-.08f,.025f);
                        Ribbon(kit,concreteDark,d0,d1,l0+w-.08f,l0+w+.08f,.025f);
                    }
                    Ribbon(kit,concrete,d0,d1,-(Road-.3f),-(Road-.3f)+.08f,.025f);
                }
                else
                {
                    // Patched asphalt: the odd darker repair across the old surface.
                    var surf=Hash(Mathf.Floor(dm/23),1,3)<.18f?patch:asphalt;
                    Ribbon(kit,surf,d0,d1,-(Road-.3f),Road-.3f,.025f);
                }
            }
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
                foreach(float side in new[]{-1f,1f})
                {
                    if(side<0&&InPits(dm))continue;
                    float mid=side*(Barrier0+Barrier1)*.5f;
                    var lower=((i/6)+(side>0?1:0))%3==0?armcoBlue:armco;
                    kit.Beam(lower,At(d0,mid,.45f),At(d1,mid,.45f),.1f,.3f);
                    kit.Beam(armco,At(d0,mid,.82f),At(d1,mid,.82f),.1f,.3f);
                    if(i%2==0)kit.Beam(steel,At(d0,mid+side*.15f,-.2f),At(d0,mid+side*.15f,1.0f),.12f,.12f);
                    if(Fenced(dm,side))
                    {
                        kit.Rail(fence,At(d0,side*(Barrier1+.6f),3.6f),At(d1,side*(Barrier1+.6f),3.6f),.06f);
                        kit.Rail(fence,At(d0,side*(Barrier1+.6f),2.0f),At(d1,side*(Barrier1+.6f),2.0f),.04f);
                        if(i%2==0)kit.Beam(fence,At(d0,side*(Barrier1+.6f),-.2f),At(d0,side*(Barrier1+.6f),3.65f),.1f,.1f);
                    }
                }
            }
        }

        // Spectator areas: (first knot, last knot, side). Catch fences, crowds and campers go here.
        static readonly Vector3[] spectatorZones={new Vector3(10.5f,13.5f,1),new Vector3(48,54,1),new Vector3(57,61.5f,1),new Vector3(62,64.5f,1),new Vector3(31,34,-1)};
        static bool Fenced(float d,float side)
        {
            foreach(var z in spectatorZones)
            {
                float a=DistanceAtKnot(z.x),b=DistanceAtKnot(z.y);
                if(d>a&&d<b&&Mathf.Sign(z.z)==Mathf.Sign(side))return true;
            }
            return InPits(d)&&side>0;
        }

        // ------------------------------------------------------------ country road bridges over the track
        static void BuildRoadBridge(Transform world,Chunks chunks,float d,string name)
        {
            var t=Frame(world,name,d,0);
            var kit=new PolygonSceneryKit(name);
            const float deck=7.4f,half=4.8f,span=13.2f,rampLength=80;
            // Deck and parapets (local x crosses the track, z runs along it).
            kit.Box(stone,new Vector3(0,deck-.7f,0),new Vector3(span*2+4,1.4f,half*2));
            kit.Box(charcoal,new Vector3(0,deck+.02f,0),new Vector3(span*2+4,.1f,half*2-1.4f));
            foreach(int s in new[]{-1,1})
            {
                kit.Box(stone,new Vector3(0,deck+.55f,s*(half-.35f)),new Vector3(span*2+4,1.1f,.7f));
                kit.Box(stoneDark,new Vector3(0,deck-1.55f,s*(half-.4f)),new Vector3(span*2+4.2f,.3f,.8f));
            }
            // Arched stone abutments beyond the barriers.
            foreach(int s in new[]{-1,1})
            {
                kit.Box(stone,new Vector3(s*(span+1),deck*.5f-2,0),new Vector3(2.2f,deck+2.6f,half*2+1));
                kit.Box(stoneDark,new Vector3(s*(span+.1f),deck-1.8f,0),new Vector3(.4f,.5f,half*2+1.2f));
            }
            // Ramps: earth embankments carrying the road down to the hillside on both sides.
            foreach(int s in new[]{-1,1})
            {
                Vector3 prevTop=Vector3.zero;float prevH=0;
                for(int k=0;k<=8;k++)
                {
                    float x=s*(span+2+rampLength*k/8f);
                    var wp=t.TransformPoint(new Vector3(x,0,0));
                    float ground=TerrainHeight(wp.x,wp.z)-t.position.y;
                    float hgt=Mathf.Lerp(deck,Mathf.Max(ground,deck-rampLength*.11f),k/8f);
                    if(k==8)hgt=Mathf.Max(ground+.3f,hgt);
                    var top=new Vector3(x,hgt,0);
                    if(k>0)
                    {
                        float w0=half+Mathf.Max(0,prevH-ground)*1.4f,w1=half+Mathf.Max(0,hgt-ground)*1.4f;
                        float b0=Mathf.Min(prevH,ground)-2,b1=Mathf.Min(hgt,ground)-2;
                        kit.Prism(verge,new[]{new Vector3(prevTop.x,b0,-w0-2),new Vector3(prevTop.x,b0,w0+2),new Vector3(x,b1,w1+2),new Vector3(x,b1,-w1-2)},
                                       new[]{new Vector3(prevTop.x,prevH,-half),new Vector3(prevTop.x,prevH,half),new Vector3(x,hgt,half),new Vector3(x,hgt,-half)});
                        kit.SheetQuad(charcoal,new Vector3(prevTop.x,prevH+.05f,-half+.7f),new Vector3(prevTop.x,prevH+.05f,half-.7f),new Vector3(x,hgt+.05f,half-.7f),new Vector3(x,hgt+.05f,-half+.7f));
                    }
                    prevTop=top;prevH=hgt;
                }
            }
            // A few people leaning on the parapet watching the cars below.
            for(int k=0;k<7;k++)
            {
                float x=-span+2+k*3.6f+Hash(d,k)*1.2f;int s=k%2==0?-1:1;
                Standing(kit,new Vector3(x,deck+.05f,s*(half-1.2f)),new Vector3(0,0,s),k);
            }
            kit.Emit(t);
            Reserve(t,-span-rampLength-6,span+rampLength+6,-half-14,half+14);
        }

        static void Standing(PolygonSceneryKit kit,Vector3 p,Vector3 facing,int seed)
        {
            float h=Hash(p.x+seed,p.z-seed,7);
            var shirt=shirts[Mathf.FloorToInt(h*shirts.Length)%shirts.Length];
            var r=Vector3.Cross(Vector3.up,facing.normalized)*.11f;
            kit.Beam(jeans,p+r,p+r+Vector3.up*.86f,.14f,.14f);kit.Beam(jeans,p-r,p-r+Vector3.up*.86f,.14f,.14f);
            kit.PersonLite(p+Vector3.up*.78f,facing,.95f+h*.1f,shirt,skins[Mathf.FloorToInt(Hash(p.z,p.x,8)*3)%3],h>.8f?white:hair,h>.9f);
        }

        // ------------------------------------------------------------ start gantry, pits, grandstand
        static void BuildGantry(Transform world)
        {
            var gate=Frame(world,"NORDWALD RING - start gantry",32,0);
            var kit=new PolygonSceneryKit("Nordwald gantry");
            const float span=11.6f,beamY=7.6f;
            foreach(int side in new[]{-1,1})
            {
                var foot=new Vector3(side*span,0,0);
                kit.Chamfered(concreteDark,foot,.9f,1.0f,.8f,.9f,-2,.5f,.25f);
                // Lattice tower: four corner posts with cross braces.
                foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})
                    kit.Beam(steel,foot+new Vector3(a*.45f,.5f,b*.45f),foot+new Vector3(a*.45f,beamY+1.9f,b*.45f),.14f,.14f);
                for(float y=1;y<beamY;y+=1.6f)foreach(int b in new[]{-1,1})
                    kit.Beam(steel,foot+new Vector3(-.45f,y,b*.45f),foot+new Vector3(.45f,y+1.4f,b*.45f),.08f,.08f);
            }
            kit.Box(green,new Vector3(0,beamY+.9f,0),new Vector3(span*2+1.4f,1.8f,.9f));
            kit.Box(yellow,new Vector3(0,beamY+1.9f,0),new Vector3(span*2+1.8f,.2f,1.1f));
            kit.Box(yellow,new Vector3(0,beamY-.05f,0),new Vector3(span*2+1.8f,.15f,1.1f));
            var front=PolygonWorld.Label("Nordwald race lettering",gate,"NORDWALD RING",new Vector3(0,beamY+.9f,-.6f),.15f,cream.color);
            var rear=PolygonWorld.Label("Nordwald reverse lettering",gate,"NORDWALD RING",new Vector3(0,beamY+.9f,.6f),.15f,cream.color);
            rear.transform.localRotation=Quaternion.Euler(0,180,0);
            var letteringShader=Resources.Load<Shader>("PolygonGantryLettering");
            if(letteringShader)
            {
                var lettering=new Material(letteringShader){name="Nordwald depth tested lettering"};
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

        static void BuildPits(Transform world,Chunks chunks)
        {
            int n=Mathf.CeilToInt((PitEnd-PitStart)/4);float step=(PitEnd-PitStart)/n;
            for(int i=0;i<n;i++)
            {
                float d0=PitStart+i*step,d1=d0+step,dm=(d0+d1)*.5f;var kit=chunks.At(P(dm));
                Slab(kit,-1,d0,d1,Barrier0,Barrier0+.7f,-.6f,1.15f,white);
                Slab(kit,-1,d0,d1,Barrier0+.7f,24,-2.5f,.06f,concreteDark);
                Ribbon(kit,line,d0,d1,-16.2f,-16f,.07f);
                kit.Rail(fence,At(d0,-(Barrier0+.35f),4f),At(d1,-(Barrier0+.35f),4f),.06f);
                if(i%2==0)kit.Beam(fence,At(d0,-(Barrier0+.35f),1.1f),At(d0,-(Barrier0+.35f),4.05f),.1f,.1f);
            }
            float mid=(PitStart+PitEnd)*.5f,len=PitEnd-PitStart-8;
            var g=Frame(world,"Nordwald pit building",mid,0);var gk=new PolygonSceneryKit("Nordwald pits");
            const float x0=-38,x1=-24,h=6.5f;
            gk.Box(white,new Vector3((x0+x1)*.5f,(h-4)*.5f,0),new Vector3(x1-x0,h+4,len));
            gk.Box(charcoal,new Vector3((x0+x1)*.5f,h+.25f,0),new Vector3(x1-x0+.8f,.5f,len+.8f));
            gk.Sleeve(glassDark,new Vector3((x0+x1)*.5f,h+2.6f,0),new Vector3(x1-x0-1.2f,4.2f,len-1.2f));
            gk.Box(white,new Vector3((x0+x1)*.5f,h+5.0f,0),new Vector3(x1-x0+3,.5f,len+3));
            gk.Box(green,new Vector3(x1+1.55f,h+5.0f,0),new Vector3(.3f,.7f,len+3));
            var slots=new GameObject("Paddock parked car slots").transform;slots.SetParent(world,false);
            const int bays=16;float bay=len/bays;
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
            // Race control tower at the end of the building.
            gk.Box(white,new Vector3((x0+x1)*.5f,10,len*.5f+8),new Vector3(12,24,12));
            gk.Sleeve(glassDark,new Vector3((x0+x1)*.5f,20,len*.5f+8),new Vector3(12.8f,3.4f,12.8f));
            gk.Box(white,new Vector3((x0+x1)*.5f,22.2f,len*.5f+8),new Vector3(14,1,14));
            gk.Box(green,new Vector3((x0+x1)*.5f,23,len*.5f+8),new Vector3(10,.6f,10));
            gk.Emit(g);Reserve(g,x0-30,x1+6,-len*.5f-8,len*.5f+20);
        }

        static void Grandstand(Transform world,string name,float startD,float length,int sign,int rows,int theme,float front=11.2f,bool roof=true)
        {
            float mid=startD+length*.5f;var t=Frame(world,name,mid,0);var kit=new PolygonSceneryKit("Nordwald stand "+theme);
            const float rowDepth=1.15f,rise=.72f;float back=front+rows*rowDepth;var facing=new Vector3(-sign,0,0);
            int sections=Mathf.Max(1,Mathf.RoundToInt(length/12));float sectionLength=length/sections;
            for(int r=0;r<rows;r++)
            {
                float x0=front+r*rowDepth,top=1.2f+r*rise;
                kit.Box(r%2==0?concreteDark:steel,new Vector3(sign*(x0+rowDepth*.5f),(top-6)*.5f,0),new Vector3(rowDepth,top+6,length));
                for(int sct=0;sct<sections;sct++)
                {
                    var seat=teamColors[(sct+theme+(r/4))%teamColors.Length];float z0=-length*.5f+sct*sectionLength;
                    kit.Box(seat,new Vector3(sign*(x0+.45f),top+.22f,z0+sectionLength*.5f),new Vector3(.5f,.44f,sectionLength-.9f));
                    int seats=Mathf.FloorToInt((sectionLength-1.2f)/1.05f);
                    for(int s=0;s<seats;s++)
                    {
                        float z=z0+.9f+s*1.05f,hsh=Hash(z+theme*17,r,2);if(hsh<.28f)continue;
                        var shirt=shirts[Mathf.FloorToInt(Hash(z,r,3)*shirts.Length)%shirts.Length];
                        kit.PersonLite(new Vector3(sign*(x0+.45f),top+.18f,z),facing,.95f+hsh*.12f,shirt,skins[Mathf.FloorToInt(Hash(z,r,5)*3)%3],hsh>.86f?white:hair,hsh>.93f);
                    }
                }
            }
            float wallTop=1.2f+rows*rise+3f;
            kit.Box(green,new Vector3(sign*(back+.25f),(wallTop-6)*.5f,0),new Vector3(.5f,wallTop+6,length+.6f));
            if(roof)
            {
                var a=new Vector3(sign*(front-1.5f),wallTop+.8f,0);var b=new Vector3(sign*(back+.6f),wallTop+2.6f,0);
                kit.Box(white,(a+b)*.5f,new Vector3(Mathf.Abs(b.x-a.x),.35f,length+1),Quaternion.Euler(0,0,sign*Mathf.Atan2(b.y-a.y,Mathf.Abs(b.x-a.x))*Mathf.Rad2Deg));
                kit.Box(yellow,a+new Vector3(sign*-.1f,-.15f,0),new Vector3(.3f,.6f,length+1.1f));
                int bays=Mathf.Max(2,Mathf.RoundToInt(length/10));
                for(int k=0;k<=bays;k++)
                {
                    float z=-length*.5f+k*length/bays;
                    kit.Beam(steel,new Vector3(sign*(back+.5f),-1,z),new Vector3(sign*(back+.5f),wallTop+2.5f,z),.3f,.3f);
                    kit.Beam(steel,new Vector3(sign*(back+.5f),wallTop+2.4f,z),new Vector3(sign*(front-1.2f),wallTop+.9f,z),.18f,.25f);
                }
            }
            kit.Emit(t);
            Reserve(t,sign>0?front-2:-(back+4),sign>0?back+4:-(front-2),-length*.5f-4,length*.5f+4);
        }

        // ------------------------------------------------------------ spectators and campers
        static void BuildSpectators(Transform world,Chunks chunks)
        {
            // Grandstands: crest, banked hairpin (outside), valley, spectator corners.
            Grandstand(world,"Crest grandstand",DistanceAtKnot(11.2f)-30,60,1,7,1,12.5f);
            Grandstand(world,"Banked hairpin grandstand",DistanceAtKnot(46.6f)-20,50,1,8,2,13f,false);
            Grandstand(world,"Valley grandstand",DistanceAtKnot(31.6f)-25,50,-1,7,3,12.5f);
            Grandstand(world,"Spectator corner grandstand",DistanceAtKnot(59.4f)-30,60,1,8,4,18f);
            // Standing crowds behind the fences in every spectator zone.
            var clearings=new List<Rect>();
            foreach(var z in spectatorZones)
            {
                float a=DistanceAtKnot(z.x),b=DistanceAtKnot(z.y);float side=Mathf.Sign(z.z);
                for(float d=a+3;d<b-3;d+=1.4f)
                {
                    if(Hash(d,z.x,1)<.35f)continue;
                    float lateral=side*(Barrier1+1.6f+Hash(d,z.y,2)*4.5f);
                    var p=At(d,lateral,0);
                    if(Reserved(new Vector2(p.x,p.z),.6f)||Offset(p)<Barrier1+1.2f)continue;
                    p.y=Mathf.Max(TerrainHeight(p.x,p.z),H(d,side*Edge))+.02f;
                    Standing(chunks.At(p),p,-S(d)*side,(int)d);
                }
                // Keep the forest back from the spectator banks.
                for(float d=a;d<b;d+=20){var p=Flat(d,side*20);clearings.Add(Rect.MinMaxRect(p.x-14,p.z-14,p.x+14,p.z+14));}
            }
            reserved.AddRange(clearings);

            // Camping meadow outside the spectator corners: camper vans, tents, flags and barbecues.
            var c=new Vector3(CampSite.x,0,CampSite.y);
            for(int i=0;i<46;i++)
            {
                float ang=Hash(i,1)*Mathf.PI*2,r=Mathf.Sqrt(Hash(i,2))*78;
                var p=c+new Vector3(Mathf.Cos(ang)*r,0,Mathf.Sin(ang)*r*1.5f);
                if(Offset(p)<22)continue;
                p.y=TerrainHeight(p.x,p.z);var kit=chunks.At(p);float yaw=Hash(i,3)*360;var rot=Quaternion.Euler(0,yaw,0);
                if(i%3==0)
                {
                    // Camper van.
                    kit.Box(white,p+Vector3.up*1.7f,new Vector3(2.3f,2.4f,6.2f),rot);
                    kit.Box(vanStripes[i%vanStripes.Length],p+Vector3.up*1.4f,new Vector3(2.34f,.45f,6.24f),rot);
                    kit.Box(glassDark,p+Vector3.up*2.1f+rot*new Vector3(0,0,3.05f),new Vector3(2.0f,.8f,.14f),rot);
                    foreach(int s in new[]{-1,1})foreach(int f in new[]{-1,1})kit.Box(charcoal,p+Vector3.up*.38f+rot*new Vector3(s*1.05f,0,f*2.1f),new Vector3(.32f,.76f,.76f),rot);
                    // Awning with chairs.
                    kit.SheetQuad(vanStripes[(i+2)%vanStripes.Length],p+rot*new Vector3(1.15f,2.5f,-2),p+rot*new Vector3(1.15f,2.5f,2),p+rot*new Vector3(3.6f,2.1f,2),p+rot*new Vector3(3.6f,2.1f,-2));
                    kit.Box(red,p+rot*new Vector3(2.6f,.45f,0),new Vector3(.6f,.1f,.6f),rot);
                }
                else
                {
                    // Ridge tent.
                    var col=tentColors[i%tentColors.Length];
                    var l=rot*Vector3.right;var f=rot*Vector3.forward;float w=1.4f+Hash(i,4)*.6f,len=2.2f+Hash(i,5);
                    kit.Prism(col,new[]{p-l*w-f*len,p+l*w-f*len,p+Vector3.up*1.3f-f*len},new[]{p-l*w+f*len,p+l*w+f*len,p+Vector3.up*1.3f+f*len});
                }
                if(i%4==1)
                {
                    // Flag on a pole.
                    var fp=p+rot*new Vector3(-3,0,0);
                    kit.Beam(steel,fp,fp+Vector3.up*5.5f,.08f,.08f);
                    var fc=tentColors[(i+3)%tentColors.Length];
                    kit.SheetQuad(fc,fp+Vector3.up*5.4f,fp+Vector3.up*5.4f+rot*Vector3.right*1.8f,fp+Vector3.up*4.3f+rot*Vector3.right*1.8f,fp+Vector3.up*4.3f);
                }
                if(i%2==0)Standing(kit,p+rot*new Vector3(2.4f,0,1.5f),rot*Vector3.back,i);
            }
            ReserveWorld(c.x-90,c.x+90,c.z-125,c.z+125);
        }

        // ------------------------------------------------------------ castle on the hill
        static void BuildCastle(Transform world,Chunks chunks)
        {
            var c=new Vector3(CastleSite.x,0,CastleSite.y);c.y=TerrainHeight(c.x,c.z);
            var kit=chunks.At(c);
            // Rocky crown under the walls.
            kit.Chamfered(stoneDark,c+Vector3.down*6,40,34,34,28,0,7,8);
            // Curtain wall ring with crenellations and round corner towers.
            const int sides=10;float rx=32,rz=26;
            for(int i=0;i<sides;i++)
            {
                float a0=i*Mathf.PI*2/sides,a1=(i+1)*Mathf.PI*2/sides;
                var p0=c+new Vector3(Mathf.Cos(a0)*rx,0,Mathf.Sin(a0)*rz);var p1=c+new Vector3(Mathf.Cos(a1)*rx,0,Mathf.Sin(a1)*rz);
                var mid=(p0+p1)*.5f;var dir=(p1-p0).normalized;float len=Vector3.Distance(p0,p1);var rot=Quaternion.LookRotation(dir);
                kit.Box(stone,mid+Vector3.up*4.5f,new Vector3(2.2f,10,len+.6f),rot);
                for(float u=-len*.5f+1;u<len*.5f-.5f;u+=2.2f)kit.Box(stone,mid+rot*new Vector3(0,10.3f,u),new Vector3(2.2f,1.2f,1.1f),rot);
                if(i%2==0)
                {
                    kit.Column(stone,p0+Vector3.down*1,4,3.6f,15,10);
                    kit.Column(slate,p0+Vector3.up*14,4.4f,.3f,6,10);
                }
            }
            // Gatehouse.
            var gate=c+new Vector3(0,0,-rz);kit.Box(stone,gate+Vector3.up*6,new Vector3(10,14,7));
            kit.Box(charcoal,gate+new Vector3(0,3,-3.6f),new Vector3(4,6,.4f));
            kit.Prism(slate,new[]{gate+new Vector3(-5.6f,13,-4),gate+new Vector3(5.6f,13,-4),gate+new Vector3(0,18,-4)},new[]{gate+new Vector3(-5.6f,13,4),gate+new Vector3(5.6f,13,4),gate+new Vector3(0,18,4)});
            // Great round keep with a crenellated top and a flag.
            var keep=c+new Vector3(-6,0,6);
            kit.Column(stone,keep+Vector3.down*1,9,8,32,14);
            kit.Column(stone,keep+Vector3.up*31,9.2f,9.2f,1.2f,14);
            for(int k=0;k<14;k++){float a=k*Mathf.PI*2/14;kit.Box(stone,keep+new Vector3(Mathf.Cos(a)*8.6f,33.2f,Mathf.Sin(a)*8.6f),new Vector3(1.6f,1.6f,1.6f),Quaternion.Euler(0,-a*Mathf.Rad2Deg,0));}
            for(int k=0;k<4;k++){float a=k*Mathf.PI/2+.4f;kit.Box(glassDark,keep+new Vector3(Mathf.Cos(a)*8.3f,22,Mathf.Sin(a)*8.3f),new Vector3(1.2f,2.2f,1.2f),Quaternion.Euler(0,-a*Mathf.Rad2Deg,0));}
            kit.Beam(steel,keep+Vector3.up*32,keep+Vector3.up*40,.15f,.15f);
            kit.SheetQuad(red,keep+Vector3.up*39.8f,keep+new Vector3(3.2f,39.8f,0),keep+new Vector3(3.2f,37.8f,0),keep+Vector3.up*37.8f);
            kit.SheetQuad(yellow,keep+Vector3.up*37.8f,keep+new Vector3(3.2f,37.8f,0),keep+new Vector3(3.2f,36.8f,0),keep+Vector3.up*36.8f);
            // Hall with a steep slate roof.
            var hall=c+new Vector3(14,0,-4);
            kit.Box(plaster,hall+Vector3.up*5,new Vector3(14,12,22));
            kit.Prism(slate,new[]{hall+new Vector3(-7.8f,11,-11.6f),hall+new Vector3(7.8f,11,-11.6f),hall+new Vector3(0,19,-11.6f)},new[]{hall+new Vector3(-7.8f,11,11.6f),hall+new Vector3(7.8f,11,11.6f),hall+new Vector3(0,19,11.6f)});
            for(int k=-3;k<=3;k++)kit.Box(glassDark,hall+new Vector3(-7.06f,7,k*2.8f),new Vector3(.14f,2.2f,1.1f));
            ReserveAround(CastleSite,48);
        }

        // ------------------------------------------------------------ village in the valley
        static void HalfTimbered(PolygonSceneryKit kit,Vector3 c,float yaw,float w,float l,int floors,float pick)
        {
            var rot=Quaternion.Euler(0,yaw,0);float h=floors*3f;
            Vector3 L(float x,float y,float z)=>c+rot*new Vector3(x,y,z);
            var wall=pick<.6f?plaster:pick<.8f?cream:ochrePlaster;
            kit.Box(stoneDark,L(0,-.6f,0),new Vector3(w+.4f,2,l+.4f),rot);
            kit.Box(wall,L(0,h*.5f+.4f,0),new Vector3(w,h,l),rot);
            // Timber frame standing proud of the plaster: posts, floor beams and braces.
            foreach(int s in new[]{-1,1})
            {
                for(float u=-l*.5f;u<=l*.5f+.01f;u+=l/3)kit.Box(timber,L(s*(w*.5f+.12f),h*.5f+.4f,u),new Vector3(.24f,h,.26f),rot);
                for(int f=0;f<=floors;f++)kit.Box(timber,L(s*(w*.5f+.12f),.4f+f*3,0),new Vector3(.24f,.26f,l+.2f),rot);
                for(int f=0;f<floors;f++)
                {
                    kit.Box(glassDark,L(s*(w*.5f+.05f),1.9f+f*3,-l*.17f),new Vector3(.1f,1.1f,.9f),rot);
                    kit.Box(glassDark,L(s*(w*.5f+.05f),1.9f+f*3,l*.17f),new Vector3(.1f,1.1f,.9f),rot);
                }
            }
            foreach(int s in new[]{-1,1})for(float u=-w*.5f;u<=w*.5f+.01f;u+=w/2)kit.Box(timber,L(u,h*.5f+.4f,s*(l*.5f+.12f)),new Vector3(.26f,h,.24f),rot);
            float ridge=h+.4f+w*.75f;
            kit.Prism(slate,new[]{L(-w*.5f-.7f,h+.3f,-l*.5f-.6f),L(w*.5f+.7f,h+.3f,-l*.5f-.6f),L(0,ridge,-l*.5f-.6f)},new[]{L(-w*.5f-.7f,h+.3f,l*.5f+.6f),L(w*.5f+.7f,h+.3f,l*.5f+.6f),L(0,ridge,l*.5f+.6f)});
            kit.Box(stoneDark,L(w*.25f,ridge-.4f,l*.3f),new Vector3(.8f,2.2f,.8f),rot);
        }
        static void BuildVillage(Chunks chunks)
        {
            var c=new Vector3(VillageSite.x,0,VillageSite.y);c.y=TerrainHeight(c.x,c.z);
            // Church with a tall slate spire at the heart of the village.
            var kit=chunks.At(c);var ch=c+new Vector3(-10,0,-8);
            kit.Box(plaster,ch+Vector3.up*5,new Vector3(11,12,26));
            kit.Prism(slate,new[]{ch+new Vector3(-6.2f,11,-13.6f),ch+new Vector3(6.2f,11,-13.6f),ch+new Vector3(0,18,-13.6f)},new[]{ch+new Vector3(-6.2f,11,13.6f),ch+new Vector3(6.2f,11,13.6f),ch+new Vector3(0,18,13.6f)});
            var tower=ch+new Vector3(0,0,-17);
            kit.Box(plaster,tower+Vector3.up*10,new Vector3(7,22,7));
            kit.Box(glassDark,tower+new Vector3(0,17,-3.56f),new Vector3(1.6f,3,.14f));
            kit.Box(timber,tower+Vector3.up*21.2f,new Vector3(7.4f,.4f,7.4f));
            kit.Chamfered(slate,tower+Vector3.up*21.4f,3.9f,3.9f,.15f,.15f,0,20,1.2f);
            kit.Column(yellow,tower+Vector3.up*41.2f,.12f,.12f,1.6f,6);
            for(int i=0;i<4;i++)kit.Box(glassDark,ch+new Vector3(-5.56f,6,-9+i*6),new Vector3(.14f,4,1.6f));
            ReserveWorld(ch.x-9,ch.x+9,ch.z-24,ch.z+16);
            // Houses round the church and along the lanes.
            int placed=0;
            for(int i=0;i<60&&placed<34;i++)
            {
                float ang=Hash(i,11)*Mathf.PI*2,r=24+Mathf.Sqrt(Hash(i,12))*95;
                var p=c+new Vector3(Mathf.Cos(ang)*r,0,Mathf.Sin(ang)*r*.8f);
                if(Offset(p)<26||Reserved(new Vector2(p.x,p.z),8))continue;
                p.y=TerrainHeight(p.x,p.z);
                float w=7+Hash(i,13)*3,l=10+Hash(i,14)*5;int floors=Hash(i,15)<.45f?2:3;
                HalfTimbered(chunks.At(p),p,Mathf.Round(Hash(i,16)*4)*90+(Hash(i,17)-.5f)*20,w,l,floors,Hash(i,18));
                ReserveWorld(p.x-l*.6f,p.x+l*.6f,p.z-l*.6f,p.z+l*.6f);placed++;
            }
            ReserveAround(VillageSite,60);
        }

        // ------------------------------------------------------------ forest
        static void Spruce(PolygonSceneryKit kit,Vector3 p,float h,bool simple)
        {
            var m=Hash(p.x,p.z,31)<.5f?spruce:spruceDark;
            if(simple){kit.Column(m,p+Vector3.down*.5f,h*.22f,.1f,h,6);return;}
            kit.Column(bark,p+Vector3.down*.4f,.28f,.16f,h*.25f,5);
            kit.Column(m,p+Vector3.up*h*.18f,h*.24f,.1f,h*.55f,6);
            kit.Column(m,p+Vector3.up*h*.48f,h*.17f,.06f,h*.52f,6);
        }
        static void Broadleaf(PolygonSceneryKit kit,Vector3 p,float h,bool simple)
        {
            var m=Hash(p.x,p.z,32)<.55f?beech:beechLight;float r=h*.3f;
            if(simple){kit.Chamfered(m,p+Vector3.up*h*.3f,r,r,r*.4f,r*.4f,0,h*.7f,r*.4f);return;}
            kit.Column(bark,p+Vector3.down*.4f,.32f,.2f,h*.45f,5);
            kit.Chamfered(m,p+Vector3.up*h*.38f,r*.75f,r*.75f,r,r,0,h*.25f,r*.35f);
            kit.Chamfered(m,p+Vector3.up*h*.63f,r,r,r*.35f,r*.35f,0,h*.37f,r*.35f);
        }
        static void BuildForest(Chunks chunks)
        {
            int count=0;
            // Dense woods close along the track: the circuit runs through a green tunnel.
            for(float d=4;d<PolygonCircuit.Length;d+=9f)
                foreach(float side in new[]{-1f,1f})
                    for(int k=0;k<3;k++)
                    {
                        float lateral=side*(12.5f+k*13+Hash(d,side+k,1)*10);
                        var p=Flat(d,lateral);
                        if(Hash(d,k+side,2)<.28f)continue;
                        if(Offset(p)<12||Reserved(new Vector2(p.x,p.z),3)||IsMeadow(p.x,p.z))continue;
                        p.y=TerrainHeight(p.x,p.z);
                        float h=12+Hash(p.x,p.z,3)*10;
                        bool simple=k==2;   // the back row reads as a silhouette
                        if(Hash(p.x,p.z,4)<.72f)Spruce(chunks.At(p),p,h,simple);else Broadleaf(chunks.At(p),p,h*.8f,simple);
                        count++;
                    }
            // Wider woods over the hills inside the detailed area (simpler trees further away).
            for(float x=-900;x<=1200;x+=28)
                for(float z=-260;z<=1420;z+=28)
                {
                    var p=new Vector3(x+(Hash(x,z,41)-.5f)*16,0,z+(Hash(x,z,42)-.5f)*16);
                    if(Hash(x,z,43)<.3f||IsMeadow(p.x,p.z)||Reserved(new Vector2(p.x,p.z),4))continue;
                    float o=Offset(p);if(o<46)continue;
                    p.y=TerrainHeight(p.x,p.z);
                    float h=12+Hash(x,z,44)*9;bool simple=o>75;
                    if(Hash(x,z,45)<.7f)Spruce(chunks.At(p),p,h,simple);else Broadleaf(chunks.At(p),p,h*.8f,simple);
                    count++;
                }
            // Distant hills: sparse simple trees on the coarse ground.
            for(float x=-2500;x<=2700;x+=72)
                for(float z=-2100;z<=2900;z+=72)
                {
                    if(x>-900&&x<1200&&z>-260&&z<1420)continue;
                    var p=new Vector3(x+(Hash(x,z,46)-.5f)*40,0,z+(Hash(x,z,47)-.5f)*40);
                    if(Hash(x,z,48)<.45f||IsMeadow(p.x,p.z))continue;
                    p.y=TerrainHeight(p.x,p.z);
                    Spruce(chunks.At(p),p,14+Hash(x,z,49)*8,true);count++;
                }
            Debug.Log("Nordwald forest: "+count+" trees.");
        }

        // ------------------------------------------------------------ boards and kilometre posts
        static void BuildBoards(Transform world)
        {
            var ink=Mat("Board ink",new Color(.98f,.97f,.93f));float last=-999;
            for(float d=0;d<PolygonCircuit.Length;d+=3)
            {
                if(Mathf.Abs(PolygonCircuit.Curvature(d))<1/30f||d-last<150)continue;
                last=d;float outside=OutsideSign(d+6);
                foreach(int m in new[]{150,100,50})
                {
                    float bd=d-m;
                    var b=Frame(world,"Nordwald corner board "+m,bd,outside*(Barrier1+.8f),0);
                    PolygonWorld.Box("Board post",b,new Vector3(0,1.2f,0),new Vector3(.16f,2.4f,.16f),navy);
                    PolygonWorld.Box("Board panel",b,new Vector3(0,2.7f,0),new Vector3(1.8f,1.1f,.16f),green);
                    PolygonWorld.Label("Board lettering",b,m.ToString(),new Vector3(0,2.7f,-.13f),.07f,ink.color);
                }
            }
            for(int km=1;km*1000<PolygonCircuit.Length;km++)
            {
                var b=Frame(world,"Kilometre post "+km,km*1000,Barrier1+.9f,0);
                PolygonWorld.Box("Kilometre post",b,new Vector3(0,1.0f,0),new Vector3(.5f,2f,.25f),white);
                PolygonWorld.Box("Kilometre cap",b,new Vector3(0,2.1f,0),new Vector3(.56f,.2f,.3f),red);
                PolygonWorld.Label("Kilometre number",b,km.ToString(),new Vector3(0,1.4f,-.14f),.06f,charcoal.color);
            }
        }

        // ------------------------------------------------------------ entry point
        public static Transform Build(Transform circuitRoot)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.Nordwald)
                throw new System.InvalidOperationException("Select the Nordwald layout before building its scenery.");
            CreatePalette();reserved.Clear();sites.Clear();
            PrepareTerrainSamples();
            // Level ground for the start complex, the village and the camping meadow.
            float mid=(PitStart+PitEnd)*.5f;
            float pitGround=Mathf.Min(Mathf.Min(E(PitStart),E(mid)),Mathf.Min(E(PitEnd),E(PitEnd+40)))-.5f;
            AddSite(P(mid),T(mid),48,(PitEnd-PitStart)*.5f+14,pitGround,30);
            AddSite(new Vector3(VillageSite.x,0,VillageSite.y),Vector3.forward,70,55,TerrainHeight(VillageSite.x,VillageSite.y),70);
            AddSite(new Vector3(CampSite.x,0,CampSite.y),Vector3.forward,45,95,TerrainHeight(CampSite.x,CampSite.y),80);
            PrepareGround();
            var world=new GameObject(SceneryName).transform;world.SetParent(circuitRoot,false);
            var chunks=new Chunks();
            BuildRoad(chunks);
            BuildBarriers(chunks);
            BuildGantry(world);
            BuildPits(world,chunks);
            Grandstand(world,"Main grandstand",PitStart+10,PitEnd-PitStart-20,1,12,0);
            BuildRoadBridge(world,chunks,DistanceAtKnot(76.5f),"Country road bridge over the long straight");
            BuildRoadBridge(world,chunks,DistanceAtKnot(33.3f),"Village road bridge over the valley");
            BuildSpectators(world,chunks);
            BuildCastle(world,chunks);
            BuildVillage(chunks);
            BuildBoards(world);
            BuildForest(chunks);
            BuildTerrain(chunks);
            chunks.Emit(world);
            reserved.Clear();sites.Clear();tPts=null;tH=null;tD=null;tLow=null;groundLeft=groundRight=null;
            return world;
        }
    }
}
