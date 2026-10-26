using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // "POLYGON RIVIERA": a Monaco-inspired harbour street circuit with real elevation. Original
    // layout and designs; only the classic sequence of sections is borrowed. Editor-time generation
    // only, baked per material into spatial chunks like the Downtown circuit.
    //
    // Cross-section (metres from the racing line, both sides; heights follow the road elevation):
    //   0-7.5 road (edge line 7.1-7.5) | 7.5-8.7 kerb at corners, otherwise gutter |
    //   8.7-9.0 steel guardrail (driver barrier limit is 7.5 + 1.3 = 8.8) | 9-13 pavement | 15+ buildings.
    public static class PolygonRivieraArt
    {
        public const string SceneryName="Circuit - editable scenery";
        const float Road=7.5f,Rail0=8.7f,Rail1=9.0f,Walk=13f,BuildingClear=16f,ChunkSize=300,SeaLevel=-.6f;

        // Harbour basin, sea limits and the channel under the south quay.
        static readonly Rect Harbour=Rect.MinMaxRect(-150,-232,420,-30);
        static readonly Rect Channel=Rect.MinMaxRect(300,-345,352,-232);
        const float SeaSouth=-330,SeaWest=-240,SeaEast=600;

        static Material asphalt,tunnelAsphalt,line,kerbRed,gutter,checkDark,railSteel,railBlue,fence,pavement,quay,
            grass,garden,rock,sand,stucco1,stucco2,stucco3,stucco4,stucco5,roofTile,shutterGreen,glassDark,cream,teal,coral,navy,ochre,
            verdigris,gold,palmBark,palmLeaf,palmLeaf2,cypress,lampGlow,tunnelTile,water,poolTile,poolBlue,hullWhite,hullNavy,deckWood,
            skinLight,skinTan,skinDeep,hair,mountain,mountainSnow;
        static Material[] teamColors,shirts,skins,stuccos;
        static readonly List<Rect> reserved=new List<Rect>();
        static float tunnelStart,tunnelEnd;

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit)){kit=new PolygonSceneryKit("Riviera "+key.x+"_"+key.y);kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform root)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Riviera block "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(root,false);pair.Value.Emit(group);
                }
                kits.Clear();
            }
        }

        static Material Mat(string name,Color color)=>PolygonWorld.Paint("RV "+name,color);
        static void CreatePalette()
        {
            asphalt=Mat("Asphalt",new Color(.17f,.18f,.21f));
            tunnelAsphalt=Mat("Tunnel asphalt",new Color(.10f,.10f,.12f));
            line=Mat("Road paint",new Color(.94f,.94f,.90f));
            kerbRed=Mat("Kerb red",new Color(.85f,.10f,.10f));
            gutter=Mat("Gutter",new Color(.36f,.36f,.38f));
            checkDark=Mat("Start checks dark",new Color(.06f,.07f,.09f));
            railSteel=Mat("Guardrail steel",new Color(.74f,.76f,.78f));
            railBlue=Mat("Guardrail blue",new Color(.12f,.30f,.62f));
            fence=Mat("Catch fence",new Color(.20f,.22f,.25f));
            pavement=Mat("Pavement",new Color(.80f,.74f,.64f));
            quay=Mat("Quay stone",new Color(.70f,.64f,.55f));
            grass=Mat("Hillside green",new Color(.36f,.52f,.30f));
            garden=Mat("Garden lawn",new Color(.40f,.62f,.30f));
            rock=Mat("Limestone rock",new Color(.72f,.62f,.48f));
            sand=Mat("Seabed sand",new Color(.70f,.66f,.52f));
            stucco1=Mat("Stucco ochre",new Color(.94f,.72f,.42f));
            stucco2=Mat("Stucco salmon",new Color(.94f,.58f,.48f));
            stucco3=Mat("Stucco cream",new Color(.97f,.90f,.74f));
            stucco4=Mat("Stucco lemon",new Color(.98f,.86f,.52f));
            stucco5=Mat("Stucco rose",new Color(.90f,.66f,.62f));
            roofTile=Mat("Terracotta roof",new Color(.74f,.34f,.22f));
            shutterGreen=Mat("Shutter green",new Color(.20f,.42f,.32f));
            glassDark=Mat("Glass dark",new Color(.08f,.12f,.17f));
            cream=Mat("Ivory",new Color(1,.95f,.84f));
            teal=Mat("Teal",new Color(.03f,.45f,.50f));
            coral=Mat("Coral",new Color(.97f,.36f,.32f));
            navy=Mat("Navy",new Color(.08f,.16f,.30f));
            ochre=Mat("Ochre",new Color(.95f,.66f,.30f));
            verdigris=Mat("Copper roof",new Color(.36f,.62f,.54f));
            gold=Mat("Gilded trim",new Color(.88f,.70f,.30f));
            palmBark=Mat("Palm bark",new Color(.58f,.40f,.24f));
            palmLeaf=Mat("Palm leaves",new Color(.14f,.48f,.26f));
            palmLeaf2=Mat("Palm leaves light",new Color(.32f,.62f,.28f));
            cypress=Mat("Cypress",new Color(.12f,.30f,.20f));
            lampGlow=Mat("Lamp glow",new Color(1,.92f,.70f));lampGlow.SetColor("_EmissionColor",new Color(.8f,.65f,.4f));
            tunnelTile=Mat("Tunnel tile",new Color(.30f,.31f,.33f));
            water=Mat("Harbour water",new Color(.04f,.40f,.58f));
            poolTile=Mat("Pool tile",new Color(.92f,.94f,.95f));
            poolBlue=Mat("Pool basin blue",new Color(.25f,.70f,.86f));
            hullWhite=Mat("Yacht white",new Color(.97f,.97f,.96f));
            hullNavy=Mat("Yacht navy",new Color(.10f,.14f,.24f));
            deckWood=Mat("Teak deck",new Color(.66f,.48f,.30f));
            skinLight=Mat("Crowd skin light",new Color(.96f,.78f,.62f));
            skinTan=Mat("Crowd skin tan",new Color(.78f,.55f,.38f));
            skinDeep=Mat("Crowd skin deep",new Color(.45f,.30f,.20f));
            hair=Mat("Crowd hair",new Color(.16f,.11f,.08f));
            mountain=Mat("Mountain",new Color(.42f,.48f,.40f));
            mountainSnow=Mat("Mountain rock",new Color(.62f,.60f,.56f));
            teamColors=new[]{coral,teal,ochre,navy,Mat("Plum",new Color(.48f,.24f,.52f)),Mat("Lime",new Color(.55f,.80f,.25f))};
            shirts=new[]{coral,teal,ochre,navy,cream,teamColors[4],teamColors[5],stucco4};
            skins=new[]{skinLight,skinTan,skinDeep};
            stuccos=new[]{stucco1,stucco2,stucco3,stucco4,stucco5,stucco3};
        }

        // ------------------------------------------------------------ route helpers
        static Vector3 P(float d)=>PolygonCircuit.AtDistance(d);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float E(float d)=>PolygonCircuit.Elevation(d);
        static Vector3 At(float d,float lateral,float y)=>P(d)+S(d)*lateral+Vector3.up*(E(d)+y);
        static float DistanceAt(Vector3 p)=>PolygonCircuit.ProjectLap(p,out _)*PolygonCircuit.Length;
        static float Offset(Vector3 p){PolygonCircuit.ProjectLap(p,out float o);return o;}
        static float Hash(float x,float z,int salt=0)=>Mathf.Abs(Mathf.Sin(x*12.9898f+z*78.233f+salt*37.719f)*43758.5453f)%1f;
        static bool InTunnel(float d)=>d>tunnelStart&&d<tunnelEnd;

        static void Ribbon(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float y)
        {
            var a=At(d0,l0,y);var b=At(d0,l1,y);var c=At(d1,l1,y);var e=At(d1,l0,y);
            kit.Quad(m,a,b,c,e,(a+b+c+e)*.25f+Vector3.down);
        }
        static void Band(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,Material m,float y=.025f)
        {if(side>0)Ribbon(kit,m,d0,d1,a,b,y);else Ribbon(kit,m,d0,d1,-b,-a,y);}
        static void Slab(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,float y0,float y1,Material m)
        {
            float l0=side>0?a:-b,l1=side>0?b:-a;
            kit.Prism(m,new[]{At(d0,l0,y0),At(d0,l1,y0),At(d1,l1,y0),At(d1,l0,y0)},
                        new[]{At(d0,l0,y1),At(d0,l1,y1),At(d1,l1,y1),At(d1,l0,y1)});
        }
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
            var pts=new[]{frame.TransformPoint(new Vector3(x0,0,z0)),frame.TransformPoint(new Vector3(x1,0,z1)),
                frame.TransformPoint(new Vector3(x0,0,z1)),frame.TransformPoint(new Vector3(x1,0,z0))};
            float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
            foreach(var p in pts){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
            reserved.Add(Rect.MinMaxRect(minX,minZ,maxX,maxZ));
        }
        static void ReserveWorld(float x0,float x1,float z0,float z1){reserved.Add(Rect.MinMaxRect(x0,z0,x1,z1));}

        // ------------------------------------------------------------ terrain
        static Vector3[] trackSamples;static float[] trackHeights;
        static bool IsSea(float x,float z)
        {
            if(x<SeaWest||x>SeaEast||z<SeaSouth)return true;
            if(Harbour.Contains(new Vector2(x,z))||Channel.Contains(new Vector2(x,z)))return true;
            return false;
        }
        // Ground height: road level beside the road, a smooth blend of nearby road heights between
        // roads, rising into hills and mountains to the north; seabed in the sea and harbour.
        public static float TerrainHeight(float x,float z)
        {
            if(IsSea(x,z))return -8;
            var p=new Vector3(x,0,z);
            float lap=PolygonCircuit.ProjectLap(p,out float offset);
            float near=E(lap*PolygonCircuit.Length)-.35f;
            float sum=0,weight=0;
            for(int i=0;i<trackSamples.Length;i++)
            {
                float w=1/((trackSamples[i]-p).sqrMagnitude+900);
                sum+=trackHeights[i]*w;weight+=w;
            }
            float blend=sum/weight;
            // Hills: the old town climbs north of the circuit, mountains beyond.
            float hill=Mathf.SmoothStep(0,1,Mathf.InverseLerp(380,900,z))*70+Mathf.SmoothStep(0,1,Mathf.InverseLerp(800,1350,z))*260;
            hill+=(Mathf.Sin(x*.006f)*.5f+.5f)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(700,1200,z))*120;
            float far=Mathf.SmoothStep(0,1,Mathf.InverseLerp(14,45,offset));
            float h=Mathf.Lerp(near,blend,far)+hill*Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,160,offset));
            // Land slopes down to a rocky shore beside the open sea.
            float shore=Mathf.Min(Mathf.Min(x-SeaWest,SeaEast-x),z-SeaSouth);
            if(shore<14)h=Mathf.Lerp(-3,h,Mathf.Clamp01(shore/14));
            return h;
        }

        static void PrepareTerrainSamples()
        {
            const int n=256;trackSamples=new Vector3[n];trackHeights=new float[n];
            for(int i=0;i<n;i++){float d=i*PolygonCircuit.Length/n;var p=P(d);p.y=0;trackSamples[i]=p;trackHeights[i]=E(d);}
        }

        static void BuildTerrain(Chunks chunks)
        {
            // Fine grid over the circuit and old town; coarse grid out to the mountains.
            const float fx0=-320,fx1=720,fz0=-400,fz1=640,fine=8;
            GridTerrain(chunks,fx0,fx1,fz0,fz1,fine,null,true);
            GridTerrain(chunks,-1400,1800,-1200,1600,40,Rect.MinMaxRect(fx0,fz0,fx1,fz1),false);
        }

        static void GridTerrain(Chunks chunks,float x0,float x1,float z0,float z1,float step,Rect? hole,bool skirt)
        {
            int nx=Mathf.RoundToInt((x1-x0)/step),nz=Mathf.RoundToInt((z1-z0)/step);
            var h=new float[nx+1,nz+1];
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)
            {
                float x=x0+i*step,z=z0+j*step;
                if(hole.HasValue&&hole.Value.Contains(new Vector2(x,z))&&x>hole.Value.xMin&&z>hole.Value.yMin){h[i,j]=0;continue;}
                h[i,j]=TerrainHeight(x,z);
            }
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
            {
                float x=x0+i*step,z=z0+j*step;
                if(hole.HasValue)
                {
                    var r=hole.Value;
                    if(x>=r.xMin&&x+step<=r.xMax&&z>=r.yMin&&z+step<=r.yMax)continue;
                }
                var a=new Vector3(x,h[i,j],z);var b=new Vector3(x+step,h[i+1,j],z);
                var c=new Vector3(x+step,h[i+1,j+1],z+step);var d=new Vector3(x,h[i,j+1],z+step);
                var kit=chunks.At((a+c)*.5f);
                TerrainTri(kit,a,b,c);TerrainTri(kit,a,c,d);
            }
            if(skirt)
            {
                // Drop a skirt round the fine grid so its edge never shows a crack against the coarse grid.
                for(int i=0;i<nx;i++)foreach(int j in new[]{0,nz})
                {
                    var a=new Vector3(x0+i*step,h[i,j],z0+j*step);var b=new Vector3(x0+(i+1)*step,h[i+1,j],z0+j*step);
                    chunks.At(a).Sheet(rock,a,b,b+Vector3.down*30);chunks.At(a).Sheet(rock,a,b+Vector3.down*30,a+Vector3.down*30);
                }
                for(int j=0;j<nz;j++)foreach(int i in new[]{0,nx})
                {
                    var a=new Vector3(x0+i*step,h[i,j],z0+j*step);var b=new Vector3(x0+i*step,h[i,j+1],z0+(j+1)*step);
                    chunks.At(a).Sheet(rock,a,b,b+Vector3.down*30);chunks.At(a).Sheet(rock,a,b+Vector3.down*30,a+Vector3.down*30);
                }
            }
        }

        static void TerrainTri(PolygonSceneryKit kit,Vector3 a,Vector3 b,Vector3 c)
        {
            var n=Vector3.Cross(b-a,c-a).normalized;if(n.y<0)n=-n;
            float minY=Mathf.Min(a.y,Mathf.Min(b.y,c.y)),maxY=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
            var centre=(a+b+c)/3;
            Material m;
            if(maxY<-1.5f)m=sand;
            else if(n.y<.72f||minY<-1.2f)m=rock;
            else if(centre.y>140)m=centre.y>300?mountainSnow:mountain;
            else
            {
                float o=Offset(centre);
                m=o<34?pavement:(Hash(Mathf.Floor(centre.x/24),Mathf.Floor(centre.z/24))<.35f?garden:grass);
            }
            kit.Tri(m,a,b,c,centre+Vector3.down);
        }

        static void BuildWater(Transform world)
        {
            var kit=new PolygonSceneryKit("Riviera sea");
            var a=new Vector3(-2600,SeaLevel,-3200);var b=new Vector3(3000,SeaLevel,-3200);
            var c=new Vector3(3000,SeaLevel,1200);var d=new Vector3(-2600,SeaLevel,1200);
            kit.Quad(water,a,b,c,d,(a+c)*.5f+Vector3.down);
            var group=new GameObject("Harbour water").transform;group.SetParent(world,false);
            kit.Emit(group);
            foreach(Transform child in group)child.name="Harbour water surface";   // plain name: the look pass makes it water
        }

        static void BuildQuays(Chunks chunks)
        {
            // Crisp stone quay walls around the harbour and the channel.
            void Wall(Vector3 from,Vector3 to)
            {
                var mid=(from+to)*.5f;var len=Vector3.Distance(from,to);
                var rot=Quaternion.LookRotation((to-from).normalized);
                float top=TerrainHeight(mid.x+(mid.x>Harbour.center.x?6:-6),mid.z+(mid.z>Harbour.center.y?6:-6));
                top=Mathf.Max(top,.6f);
                chunks.At(mid).Box(quay,mid+Vector3.up*((top-4)*.5f),new Vector3(2.4f,top+4,len+2.4f),rot);
                chunks.At(mid).Box(cream,mid+Vector3.up*(top+.15f),new Vector3(2.8f,.3f,len+2.8f),rot);
            }
            var r=Harbour;
            for(float x=r.xMin;x<r.xMax;x+=40)Wall(new Vector3(x,0,r.yMax),new Vector3(Mathf.Min(x+40,r.xMax),0,r.yMax));
            for(float x=r.xMin;x<r.xMax;x+=40)
            {
                float xe=Mathf.Min(x+40,r.xMax);
                if(x+40<=Channel.xMin||x>=Channel.xMax)Wall(new Vector3(x,0,r.yMin),new Vector3(xe,0,r.yMin));
                else
                {
                    if(x<Channel.xMin)Wall(new Vector3(x,0,r.yMin),new Vector3(Channel.xMin,0,r.yMin));
                    if(xe>Channel.xMax)Wall(new Vector3(Channel.xMax,0,r.yMin),new Vector3(xe,0,r.yMin));
                }
            }
            for(float z=r.yMin;z<r.yMax;z+=40){Wall(new Vector3(r.xMin,0,z),new Vector3(r.xMin,0,Mathf.Min(z+40,r.yMax)));Wall(new Vector3(r.xMax,0,z),new Vector3(r.xMax,0,Mathf.Min(z+40,r.yMax)));}
            foreach(float x in new[]{Channel.xMin,Channel.xMax})
                for(float z=Channel.yMin;z<Channel.yMax;z+=40)Wall(new Vector3(x,0,z),new Vector3(x,0,Mathf.Min(z+40,Channel.yMax)));
            // Breakwater lights at the channel mouth.
            var tiers0=PolygonSceneryMeshes.LighthouseTiers(0);var tiers1=PolygonSceneryMeshes.LighthouseTiers(1);
            var lantern=PolygonSceneryMeshes.LighthouseLantern();var gallery=PolygonSceneryMeshes.LighthouseGallery();
            foreach(float x in new[]{Channel.xMin-6,Channel.xMax+6})
            {
                var basePos=new Vector3(x,0,Channel.yMin+6);
                var lh=new GameObject("Harbour mouth light").transform;lh.SetParent(chunksRoot,false);
                lh.localPosition=basePos;lh.localScale=Vector3.one*.42f;
                Part(lh,tiers0,cream);Part(lh,tiers1,x<Channel.center.x?coral:teal);Part(lh,gallery,navy);Part(lh,lantern,glassDark);
            }
        }
        static Transform chunksRoot;
        static void Part(Transform parent,Mesh mesh,Material m)
        {
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        }

        // ------------------------------------------------------------ road, rails, pavements
        static void BuildRoad(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;const float checkerEnd=1.6f;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));
                bool tunnel=InTunnel(dm);
                bool corner=Mathf.Abs(PolygonCircuit.Curvature(dm))>1/80f||Mathf.Abs(PolygonCircuit.Curvature(dm-12))>1/80f||Mathf.Abs(PolygonCircuit.Curvature(dm+12))>1/80f;
                var surface=tunnel?tunnelAsphalt:asphalt;
                foreach(float side in new[]{-1f,1f})
                {
                    Band(kit,side,d0,d1,Road-.4f,Road,line);
                    Band(kit,side,d0,d1,Road,Rail0,corner&&!tunnel?((i/2)%2==0?kerbRed:line):gutter);
                }
                if(d0<checkerEnd)
                {
                    const int rows=2,cols=16;
                    for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
                    {
                        float a0=d0+checkerEnd*r/rows,a1=d0+checkerEnd*(r+1)/rows,l0=-(Road-.4f)+c*(Road-.4f)*2/cols,l1=l0+(Road-.4f)*2/cols;
                        Ribbon(kit,(r+c)%2==0?line:checkDark,a0,a1,l0,l1,.025f);
                    }
                    d0=checkerEnd;
                }
                bool dash=(i/3)%2==0&&!corner;
                Ribbon(kit,surface,d0,d1,-(Road-.4f),-.1f,.025f);
                Ribbon(kit,dash?line:surface,d0,d1,-.1f,.1f,.025f);
                Ribbon(kit,surface,d0,d1,.1f,Road-.4f,.025f);
                // A skirt under the road so steep terrain changes never show a gap beneath it.
                foreach(float side in new[]{-1f,1f})
                {
                    float l=side*Rail0;
                    kit.Sheet(gutter,At(d0,l,.02f),At(d1,l,.02f),At(d1,l,-2.5f));kit.Sheet(gutter,At(d0,l,.02f),At(d1,l,-2.5f),At(d0,l,-2.5f));
                }
            }
        }

        static void BuildRailsAndPavements(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount/2;float step=PolygonCircuit.Length/n;
            var ads=new[]{cream,teal,coral,navy,ochre,cream,teamColors[4]};
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));bool tunnel=InTunnel(dm);
                foreach(float side in new[]{-1f,1f})
                {
                    float mid=side*(Rail0+Rail1)*.5f;
                    // Double steel guardrail, posts every other segment, sponsor-coloured lower beam.
                    var lower=ads[((i/5)+(side>0?2:0))%ads.Length];
                    kit.Beam(lower,At(d0,mid,.55f),At(d1,mid,.55f),.08f,.32f);
                    kit.Beam(railSteel,At(d0,mid,.95f),At(d1,mid,.95f),.08f,.32f);
                    if(i%2==0)
                    {
                        kit.Beam(railBlue,At(d0,mid+side*.12f,0),At(d0,mid+side*.12f,1.15f),.12f,.12f);
                        if(d0<180)
                        {
                            kit.Box(railSteel,At(d0,mid+side*.12f,.08f),new Vector3(.46f,.16f,.48f),Quaternion.LookRotation(T(d0)));
                            kit.Beam(railBlue,At(d0,mid+side*.45f,.18f),At(d0,mid+side*.12f,.88f),.08f,.08f);
                        }
                    }
                    if(tunnel)continue;
                    // Pavement with a stone kerb face and a deep skirt down to the ground.
                    Slab(kit,side,d0,d1,Rail1,Walk,-2.5f,.15f,pavement);
                    // Catch fence on the faster sections (not in the hairpin or casino square).
                    bool slow=Mathf.Abs(PolygonCircuit.Curvature(dm))>1/45f;
                    if(!slow)
                    {
                        kit.Rail(fence,At(d0,mid+side*.35f,3.4f),At(d1,mid+side*.35f,3.4f),.06f);
                        if(i%2==0)
                        {
                            kit.Beam(fence,At(d0,mid+side*.35f,.2f),At(d0,mid+side*.35f,3.45f),.08f,.08f);
                            if(d0<180)
                                kit.Box(railSteel,At(d0,mid+side*.35f,.08f),new Vector3(.42f,.16f,.48f),Quaternion.LookRotation(T(d0)));
                        }
                        if(d0<180)
                        {
                            kit.Rail(fence,At(d0,mid+side*.35f,1.15f),At(d1,mid+side*.35f,1.15f),.045f);
                            kit.Rail(fence,At(d0,mid+side*.35f,2.3f),At(d1,mid+side*.35f,2.3f),.045f);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------ tunnel under the seafront hotel
        static void BuildTunnel(Transform world,Chunks chunks)
        {
            int n=Mathf.CeilToInt((tunnelEnd-tunnelStart)/6);float step=(tunnelEnd-tunnelStart)/n;
            const float wallIn=9.3f,wallOut=10.3f,ceiling=6.6f,roof=7.6f;
            for(int i=0;i<n;i++)
            {
                float d0=tunnelStart+i*step,d1=d0+step,dm=(d0+d1)*.5f;var kit=chunks.At(P(dm));
                // Land side wall solid; sea side has open arches every other bay (light and sea views).
                Slab(kit,1,d0,d1,wallIn,wallOut,-1,ceiling,tunnelTile);
                if(i%3==0)Slab(kit,-1,d0,d1,wallIn,wallOut,-1,ceiling,tunnelTile);
                else Slab(kit,-1,d0,d1,wallIn,wallOut,-1,1.0f,tunnelTile);
                Slab(kit,-1,d0,d1,wallIn,wallOut,ceiling-1.3f,ceiling,tunnelTile);
                // Ceiling and roof deck.
                kit.Prism(tunnelTile,new[]{At(d0,-wallOut,ceiling),At(d0,wallOut,ceiling),At(d1,wallOut,ceiling),At(d1,-wallOut,ceiling)},
                                     new[]{At(d0,-wallOut-1,roof),At(d0,wallOut+1,roof),At(d1,wallOut+1,roof),At(d1,-wallOut-1,roof)});
                // Light strips down the centre of the ceiling.
                if(i%2==0)kit.Box(lampGlow,At(dm,0,ceiling-.12f),new Vector3(.6f,.16f,step*.7f),Quaternion.LookRotation(T(dm)));
                // Pavement-level walkway inside, behind the rail.
                Slab(kit,1,d0,d1,Rail1,wallIn,-1,.15f,gutter);Slab(kit,-1,d0,d1,Rail1,wallIn,-1,.15f,gutter);
            }
            // Portals.
            foreach(float d in new[]{tunnelStart,tunnelEnd})
            {
                var t=Frame(world,"Tunnel portal",d,0);var kit=new PolygonSceneryKit("Tunnel portal");
                kit.Box(stucco3,new Vector3(0,roof+1.2f,0),new Vector3(wallOut*2+3,2.4f,1.6f));
                kit.Box(gold,new Vector3(0,roof+.05f,0),new Vector3(wallOut*2+3.2f,.25f,1.8f));
                foreach(int s in new[]{-1,1})kit.Box(stucco3,new Vector3(s*(wallOut+.6f),roof*.5f,0),new Vector3(1.6f,roof+.4f,1.8f));
                kit.Emit(t);
            }
            // The seafront hotel stands on the tunnel roof: stepped terraces toward the sea.
            float span=tunnelEnd-tunnelStart;int blocks=Mathf.Max(3,Mathf.RoundToInt(span/34));
            for(int b=0;b<blocks;b++)
            {
                float d=tunnelStart+span*(b+.5f)/blocks;
                var t=Frame(world,"Seafront hotel wing "+(b+1),d,0,roof);var kit=new PolygonSceneryKit("Seafront hotel");
                float len=span/blocks+.6f;
                for(int tier=0;tier<4;tier++)
                {
                    float x0=-wallOut-1+tier*6,x1=46,y0=tier*4.2f,y1=y0+4.2f;
                    var c=new Vector3((x0+x1)*.5f,0,0);
                    kit.Box(stucco3,c+Vector3.up*(y0+.5f),new Vector3(x1-x0,1,len));
                    kit.Sleeve(glassDark,c+Vector3.up*(y0+2.4f),new Vector3(x1-x0-.8f,2.8f,len-.8f));
                    kit.Box(stucco3,c+Vector3.up*(y1-.4f),new Vector3(x1-x0,.8f,len));
                    kit.Box(cream,new Vector3(x0-.1f,y1+.5f,0),new Vector3(.12f,1,len));    // terrace rail
                    if(tier==0)for(int k=0;k<3;k++)kit.Box(coral,new Vector3(x0+1.4f,y1+1.6f,-len*.33f+k*len*.33f),new Vector3(2.2f,.12f,2.2f));
                }
                kit.Box(roofTile,new Vector3((-wallOut+17+46)*.5f,4*4.2f+.4f,0),new Vector3(46-(-wallOut+17)+.6f,.8f,len+.6f));
                // Inland the hotel stands on its own ground floors (arcade), down to the hillside.
                kit.Box(stucco3,new Vector3((wallOut+1+46)*.5f,-(roof+6)*.5f,0),new Vector3(46-wallOut-1,roof+6,len));
                kit.Emit(t);
                Reserve(t,-wallOut-2,48,-len*.5f,len*.5f);
            }
        }

        // ------------------------------------------------------------ start gantry, garages, stands
        static void BuildStartArea(Transform world)
        {
            var gate=Frame(world,"POLYGON RIVIERA - start gantry",32,0);
            var kit=new PolygonSceneryKit("Riviera gantry");
            const float span=11.5f,beamY=7.6f;
            foreach(int side in new[]{-1,1})
            {
                var foot=new Vector3(side*span,0,0);
                kit.Chamfered(quay,foot,1.1f,1.2f,1.0f,1.1f,-1,.6f,.3f);
                kit.Chamfered(cream,foot,.55f,.65f,.4f,.5f,.6f,beamY+1.9f,.2f);
                kit.Chamfered(gold,foot,.62f,.72f,.6f,.7f,.9f,1.5f,.2f);
            }
            kit.Box(navy,new Vector3(0,beamY+.9f,0),new Vector3(span*2+1.4f,1.8f,.9f));
            kit.Box(gold,new Vector3(0,beamY+1.9f,0),new Vector3(span*2+1.8f,.2f,1.1f));
            foreach(int side in new[]{-1,1})for(int row=0;row<2;row++)for(int col=0;col<2;col++)
                foreach(float z in new[]{-.55f,.55f})
                    kit.Box((row+col)%2==0?cream:checkDark,new Vector3(side*(9.6f+col*.8f),beamY+.5f+row*.8f,z),new Vector3(.8f,.8f,.2f));
            var front=PolygonWorld.Label("Polygon Riviera race lettering",gate,"POLYGON RIVIERA",new Vector3(0,beamY+.9f,-.6f),.15f,cream.color);
            var rear=PolygonWorld.Label("Polygon Riviera reverse lettering",gate,"POLYGON RIVIERA",new Vector3(0,beamY+.9f,.6f),.15f,cream.color);
            rear.transform.localRotation=Quaternion.Euler(0,180,0);
            var letteringShader=Resources.Load<Shader>("PolygonGantryLettering");
            if(letteringShader)
            {
                var lettering=new Material(letteringShader){name="Polygon Riviera depth tested lettering"};
                lettering.mainTexture=front.font.material.mainTexture;
                front.GetComponent<MeshRenderer>().sharedMaterial=lettering;rear.GetComponent<MeshRenderer>().sharedMaterial=lettering;
                var swap=gate.gameObject.AddComponent<PolygonGantryLettering>();swap.FrontText=front;swap.RearText=rear;swap.DepthMaterial=lettering;
            }
            var dark=Mat("Starting lens dark",new Color(.065f,.018f,.025f));
            var lit=Mat("Starting lens red",new Color(1,.025f,.012f));lit.SetColor("_EmissionColor",new Color(1,.02f,.008f));
            kit.Box(checkDark,new Vector3(0,beamY-.8f,-.1f),new Vector3(9.3f,1.4f,1.1f));
            for(int i=0;i<5;i++)kit.Box(checkDark,new Vector3((i-2)*1.7f,beamY-.2f,-.9f),new Vector3(1.15f,.06f,.5f),Quaternion.Euler(-8,0,0));
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
            Reserve(gate,-14,14,-3,3);

            // Team garages on the town side (left) of the start straight, with rooftop hospitality.
            var g=Frame(world,"Riviera team garages",130,0);var gk=new PolygonSceneryKit("Riviera garages");
            const float x0=-27,x1=-15,len=150,h=6;
            gk.Box(stucco3,new Vector3((x0+x1)*.5f,h*.5f-1,0),new Vector3(x1-x0,h+2,len));
            gk.Box(cream,new Vector3((x0+x1)*.5f,h+.2f,0),new Vector3(x1-x0+.8f,.4f,len+.8f));
            gk.Sleeve(glassDark,new Vector3((x0+x1)*.5f,h+1.9f,0),new Vector3(x1-x0-1,3,len-1));
            gk.Box(cream,new Vector3((x0+x1)*.5f,h+3.6f,0),new Vector3(x1-x0+1.6f,.4f,len+1.6f));
            var slots=new GameObject("Paddock parked car slots").transform;slots.SetParent(world,false);
            const int bays=10;float bay=len/bays;
            for(int i=0;i<bays;i++)
            {
                float z=-len*.5f+bay*(i+.5f);var color=teamColors[i%teamColors.Length];
                gk.Box(glassDark,new Vector3(x1+.1f,1.8f,z),new Vector3(.3f,4.2f,bay-2.6f));
                gk.Box(color,new Vector3(x1+.2f,4.4f,z),new Vector3(.4f,.8f,bay-.5f));
                gk.Beam(railSteel,new Vector3(x0+1,h+3.8f,z),new Vector3(x0+1,h+11,z),.1f,.1f);
                gk.SheetQuad(color,new Vector3(x0+1,h+10.8f,z),new Vector3(x0+1,h+10.8f,z+2.6f),new Vector3(x0+1,h+8.8f,z+2.6f),new Vector3(x0+1,h+8.6f,z));
                if(i%5==2)
                {
                    var slot=new GameObject("Parked team car slot "+i).transform;slot.SetParent(slots,false);
                    slot.position=g.TransformPoint(new Vector3(x1+3.2f,.15f,z));slot.rotation=g.rotation*Quaternion.Euler(0,90,0);
                }
            }
            gk.Emit(g);Reserve(g,x0-4,x1+5,-len*.5f-3,len*.5f+3);

            // Harbour-side grandstand along the start straight, and stands at the pool and the final hairpin.
            Grandstand(world,"Harbour grandstand",50,120,1,9,0);
            Grandstand(world,"Pool grandstand",DistanceAt(new Vector3(120,0,-296))-40,80,-1,7,1);
            Grandstand(world,"Final hairpin grandstand",DistanceAt(new Vector3(-176,0,-140))-30,60,-1,8,2);
            Grandstand(world,"Tabac grandstand",DistanceAt(new Vector3(400,0,-291))-25,50,-1,6,3);
        }

        static void Grandstand(Transform world,string name,float startD,float length,int sign,int rows,int theme)
        {
            float mid=startD+length*.5f;var t=Frame(world,name,mid,0);var kit=new PolygonSceneryKit("Riviera stand "+theme);
            const float rowDepth=1.15f,rise=.72f,front=Walk+.5f;float back=front+rows*rowDepth;var facing=new Vector3(-sign,0,0);
            int sections=Mathf.Max(1,Mathf.RoundToInt(length/12));float sectionLength=length/sections;
            for(int r=0;r<rows;r++)
            {
                float x0=front+r*rowDepth,top=1.2f+r*rise;
                kit.Box(r%2==0?stucco3:quay,new Vector3(sign*(x0+rowDepth*.5f),(top-2)*.5f,0),new Vector3(rowDepth,top+2,length));
                for(int sct=0;sct<sections;sct++)
                {
                    var seat=teamColors[(sct+theme+(r/3))%teamColors.Length];float z0=-length*.5f+sct*sectionLength;
                    kit.Box(seat,new Vector3(sign*(x0+.45f),top+.22f,z0+sectionLength*.5f),new Vector3(.5f,.44f,sectionLength-.9f));
                    int seats=Mathf.FloorToInt((sectionLength-1.2f)/1.05f);
                    for(int s=0;s<seats;s++)
                    {
                        float z=z0+.9f+s*1.05f,hsh=Hash(z+theme*17,r,2);if(hsh<.24f)continue;
                        var shirt=shirts[Mathf.FloorToInt(Hash(z,r,3)*shirts.Length)%shirts.Length];
                        kit.PersonLite(new Vector3(sign*(x0+.45f),top+.18f,z),facing,.95f+hsh*.12f,shirt,skins[Mathf.FloorToInt(Hash(z,r,5)*3)%3],hsh>.85f?cream:hair,hsh>.93f);
                    }
                }
            }
            for(int sct=0;sct<sections;sct++)
            {
                float z0=-length*.5f+sct*sectionLength;
                kit.Box(teamColors[(sct+theme+2)%teamColors.Length],new Vector3(sign*(front-.1f),.6f,z0+sectionLength*.5f),new Vector3(.2f,1.2f,sectionLength-.2f));
            }
            float wallTop=1.2f+rows*rise+2.6f;
            kit.Box(navy,new Vector3(sign*(back+.25f),(wallTop-2)*.5f,0),new Vector3(.5f,wallTop+2,length+.6f));
            // Striped awning roof in the Riviera style, on slim gilded posts.
            int modules=Mathf.Max(2,Mathf.RoundToInt(length/6));float moduleLength=length/modules;
            for(int k=0;k<modules;k++)
            {
                float z0=-length*.5f+k*moduleLength,z1=z0+moduleLength;var m=k%2==0?cream:theme%2==0?teal:coral;
                kit.SheetQuad(m,new Vector3(sign*(front-1.2f),wallTop+.6f,z0),new Vector3(sign*(back+.4f),wallTop+2.4f,z0),
                    new Vector3(sign*(back+.4f),wallTop+2.4f,z1),new Vector3(sign*(front-1.2f),wallTop+.6f,z1));
                kit.Sheet(m,new Vector3(sign*(front-1.2f),wallTop+.6f,z0),new Vector3(sign*(front-1.2f),wallTop+.6f,z1),new Vector3(sign*(front-1.2f),wallTop,(z0+z1)*.5f));
                kit.Beam(gold,new Vector3(sign*(front-1),0,z0),new Vector3(sign*(front-1),wallTop+.65f,z0),.14f,.14f);
            }
            kit.Emit(t);
            Reserve(t,sign>0?front-2:-(back+3),sign>0?back+3:-(front-2),-length*.5f-3,length*.5f+3);
        }

        // ------------------------------------------------------------ casino square
        static void BuildCasinoSquare(Transform world,Chunks chunks)
        {
            float d=DistanceAt(new Vector3(410,0,340));float ground=E(d);
            // Palace facing the square, north of the road.
            var t=new GameObject("Grand Polygon Casino").transform;t.SetParent(world,false);
            t.localPosition=new Vector3(412,ground,410);
            var kit=new PolygonSceneryKit("Casino");
            // Gardens and fountain between road and palace.
            kit.Box(garden,new Vector3(0,-.6f,-34),new Vector3(56,1.5f,26));
            kit.Box(pavement,new Vector3(0,-.55f,-34),new Vector3(64,1.4f,34));
            kit.Column(cream,new Vector3(0,.15f,-34),5,5,.7f,12);kit.Column(poolBlue,new Vector3(0,.2f,-34),4.4f,4.4f,.7f,12);
            kit.Column(cream,new Vector3(0,.9f,-34),.6f,.4f,3.4f,8);kit.Column(gold,new Vector3(0,4.3f,-34),1.2f,.2f,1,8);
            // Main hall with recessed arched windows.
            kit.Box(stucco3,new Vector3(0,-2+1.5f,0),new Vector3(56,5,30));                       // plinth
            kit.Box(stucco3,new Vector3(0,6.5f,0),new Vector3(50,9,26));
            for(int k=-5;k<=5;k++)
            {
                kit.Box(glassDark,new Vector3(k*4.2f,6.6f,-13.05f),new Vector3(2,5,.4f));
                kit.Box(gold,new Vector3(k*4.2f,9.4f,-13.2f),new Vector3(2.4f,.5f,.5f));
                kit.Box(cream,new Vector3(k*4.2f+2.1f,7,-13.3f),new Vector3(.7f,8,.7f));          // pilasters
            }
            kit.Box(cream,new Vector3(0,11.3f,0),new Vector3(52,.8f,28));                         // cornice
            kit.Box(stucco3,new Vector3(0,12.8f,0),new Vector3(48,2.2f,24));                      // attic
            // Central dome and lantern.
            kit.Column(stucco3,new Vector3(0,13.9f,0),9,9,3,12);
            kit.Column(verdigris,new Vector3(0,16.9f,0),9,4,5,12);
            kit.Column(verdigris,new Vector3(0,21.9f,0),4,1,3,12);
            kit.Column(gold,new Vector3(0,24.9f,0),.8f,.1f,3,8);
            // Twin corner towers with copper caps.
            foreach(int s in new[]{-1,1})
            {
                var b=new Vector3(s*22,0,-8);
                kit.Box(stucco3,b+Vector3.up*9,new Vector3(8,22,8));
                kit.Box(cream,b+Vector3.up*20.3f,new Vector3(8.8f,.7f,8.8f));
                kit.Box(glassDark,b+new Vector3(0,15,-4.05f),new Vector3(2.4f,6,.3f));
                kit.Chamfered(verdigris,b+Vector3.up*20.6f,4.4f,4.4f,.5f,.5f,0,7,1.2f);
                kit.Column(gold,b+Vector3.up*27.6f,.3f,.05f,2.4f,6);
            }
            // Entrance canopy and steps.
            kit.Box(gold,new Vector3(0,5.2f,-15.5f),new Vector3(12,.4f,4));
            kit.Box(cream,new Vector3(0,.2f,-15.5f),new Vector3(16,.9f,6));
            kit.Emit(t);
            ReserveWorld(370,456,366,430);
            // Palms round the gardens.
            foreach(var p in new[]{new Vector3(386,ground,376),new Vector3(438,ground,376),new Vector3(380,ground,390),new Vector3(444,ground,390)})
                Palm(chunks,p,1);
            // Grand hotel on the west side of the square and a cafe terrace to the east.
            var hotel=new GameObject("Hotel on the square").transform;hotel.SetParent(world,false);
            hotel.localPosition=new Vector3(340,TerrainHeight(340,392),392);
            var hk=new PolygonSceneryKit("Square hotel");
            Facade(hk,Vector3.zero,22,14,-3,24,3.6f,.45f,stucco3,glassDark);
            hk.Box(cream,new Vector3(0,24.5f,0),new Vector3(45,1,29));
            hk.Box(roofTile,new Vector3(0,26.2f,0),new Vector3(42,2.4f,26));
            for(int k=-4;k<=4;k++)hk.Box(coral,new Vector3(k*4.6f,3.4f,-15.2f),new Vector3(3.6f,.15f,2.4f),Quaternion.Euler(-14,0,0));
            hk.Emit(hotel);ReserveWorld(314,366,374,410);
            var cafe=new GameObject("Square cafe").transform;cafe.SetParent(world,false);
            cafe.localPosition=new Vector3(486,TerrainHeight(486,388),388);
            var ck=new PolygonSceneryKit("Square cafe");
            ck.Box(stucco4,new Vector3(0,1,0),new Vector3(26,8,14));ck.Box(cream,new Vector3(0,5.2f,0),new Vector3(27,.5f,15));
            for(int k=0;k<6;k++)
            {
                var u=new Vector3(-10+k*4,0,-11);
                ck.Column(cream,u,.05f,.05f,2.4f,6);ck.Column(k%2==0?coral:teal,u+Vector3.up*2.2f,1.6f,.08f,.8f,8);
            }
            ck.Emit(cafe);ReserveWorld(470,502,372,402);
        }

        // ------------------------------------------------------------ the rock with the old palace
        static void BuildRock(Transform world)
        {
            var t=new GameObject("Old palace on the rock").transform;t.SetParent(world,false);
            t.localPosition=new Vector3(-470,SeaLevel-2,-470);
            var go=new GameObject("Palace rock",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(t,false);
            go.transform.localScale=new Vector3(90,58,70);
            go.GetComponent<MeshFilter>().sharedMesh=PolygonSceneryMeshes.Mesa(17);go.GetComponent<MeshRenderer>().sharedMaterial=rock;
            var kit=new PolygonSceneryKit("Old palace");
            const float top=57;
            // Crenellated palace, corner towers, a domed cathedral and a few old-town houses.
            kit.Box(stucco1,new Vector3(0,top+5,-4),new Vector3(40,10,22));
            kit.Sleeve(glassDark,new Vector3(0,top+6,-4),new Vector3(39.2f,2.4f,21.2f));
            for(int k=-4;k<=4;k++)kit.Box(stucco1,new Vector3(k*4.4f,top+10.6f,-15),new Vector3(2,1.2f,1.2f));
            foreach(int s in new[]{-1,1})
            {
                kit.Box(stucco3,new Vector3(s*20,top+8,-15),new Vector3(6,16,6));
                for(int k=-1;k<=1;k++)kit.Box(stucco3,new Vector3(s*20+k*2.1f,top+16.6f,-18),new Vector3(1.1f,1.2f,1.1f));
            }
            kit.Box(stucco3,new Vector3(16,top+6,12),new Vector3(12,12,18));
            kit.Column(stucco3,new Vector3(16,top+12,8),3,3,6,8);kit.Column(verdigris,new Vector3(16,top+18,8),3.2f,.2f,6,8);
            for(int i=0;i<6;i++)
            {
                var c=new Vector3(-20+(i%3)*10,top,12+(i/3)*9);
                kit.Box(stuccos[i%stuccos.Length],c+Vector3.up*3.5f,new Vector3(8,7,7));
                kit.Chamfered(roofTile,c+Vector3.up*7,4.3f,3.8f,.4f,.4f,0,2.6f,.5f);
            }
            kit.Emit(t);
        }

        // ------------------------------------------------------------ harbour boats
        static void BuildHarbourBoats(Transform world,Chunks chunks)
        {
            // Pontoons reaching into the harbour with yachts moored alongside.
            var r=Harbour;
            for(int p=0;p<6;p++)
            {
                float x=r.xMin+40+p*((r.width-80)/5);
                bool fromNorth=p%2==0;float z0=fromNorth?r.yMax:r.yMin,z1=fromNorth?r.yMax-80:r.yMin+80;
                var kit=chunks.At(new Vector3(x,0,(z0+z1)*.5f));
                kit.Box(deckWood,new Vector3(x,SeaLevel+.6f,(z0+z1)*.5f),new Vector3(3,.4f,Mathf.Abs(z1-z0)));
                for(float z=Mathf.Min(z0,z1)+4;z<Mathf.Max(z0,z1);z+=8)kit.Column(quay,new Vector3(x,SeaLevel-3,z),.25f,.25f,3.8f,6);
                int count=0;
                for(float z=Mathf.Min(z0,z1)+8;z<Mathf.Max(z0,z1)-4&&count<6;z+=13,count++)
                    foreach(int s in new[]{-1,1})
                    {
                        float length=Hash(x+s,z)<.3f?32:18+Hash(x,z+s)*10;
                        // Moored stern-to along the pontoon, bows pointing out across the harbour.
                        var c=new Vector3(x+s*(1.5f+length*.5f+1),SeaLevel,z);
                        Yacht(chunks.At(c),c,Quaternion.Euler(0,s*90,0),length,Hash(c.x,c.z,4));
                    }
            }
            // Superyachts anchored out in the bay.
            foreach(var (pos,yaw,len) in new[]{(new Vector3(360,SeaLevel,-390),90f,58f),(new Vector3(150,SeaLevel,-420),80f,48f),
                (new Vector3(-60,SeaLevel,-395),100f,52f),(new Vector3(520,SeaLevel,-430),30f,64f),(new Vector3(-330,SeaLevel,-200),-20f,44f),
                (new Vector3(700,SeaLevel,-140),10f,55f),(new Vector3(680,SeaLevel,120),-15f,40f)})
                Yacht(chunks.At(pos),pos,Quaternion.Euler(0,yaw,0),len,Hash(pos.x,pos.z,9));
            // Sailboats out in the bay.
            var hull=PolygonSceneryMeshes.BoatHull();var cabin=PolygonSceneryMeshes.BoatCabin();
            var rig=PolygonSceneryMeshes.BoatRigging();var sails=PolygonSceneryMeshes.BoatSails();
            for(int i=0;i<10;i++)
            {
                var pos=new Vector3(-400+i*130+Hash(i,1)*60,SeaLevel-.1f,-520-Hash(i,2)*260);
                var b=new GameObject("Bay sailboat").transform;b.SetParent(world,false);
                b.localPosition=pos;b.localRotation=Quaternion.Euler(0,Hash(i,3)*360,0);b.localScale=Vector3.one*1.3f;
                Part(b,hull,hullWhite);Part(b,cabin,teal);Part(b,rig,navy);Part(b,sails,i%3==0?coral:cream);
            }
        }

        // Motor yacht: pointed hull, two or three deck levels with dark glazing, radar arch.
        static void Yacht(PolygonSceneryKit kit,Vector3 c,Quaternion rot,float length,float pick)
        {
            float w=length*.19f,hw=w*.5f;var hull=pick<.3f?hullNavy:hullWhite;
            Vector3 L(float x,float y,float z)=>c+rot*new Vector3(x,y,z);
            var outline=new[]{new Vector2(-hw,-length*.5f),new Vector2(hw,-length*.5f),new Vector2(hw,length*.18f),new Vector2(hw*.55f,length*.4f),
                new Vector2(0,length*.5f),new Vector2(-hw*.55f,length*.4f),new Vector2(-hw,length*.18f)};
            var bottom=new Vector3[outline.Length];var deck=new Vector3[outline.Length];
            for(int i=0;i<outline.Length;i++){bottom[i]=L(outline[i].x*.75f,-.6f,outline[i].y*.96f);deck[i]=L(outline[i].x,1.9f,outline[i].y);}
            kit.Prism(hull,bottom,deck);
            var deckTop=new Vector3[outline.Length];for(int i=0;i<outline.Length;i++)deckTop[i]=L(outline[i].x*.98f,2.0f,outline[i].y*.98f);
            kit.Prism(deckWood,deck,deckTop);
            int levels=length>40?3:2;float y=2;
            for(int lv=0;lv<levels;lv++)
            {
                float lw=hw*(.78f-lv*.12f),l0=-length*(.38f-lv*.04f),l1=length*(.16f-lv*.07f);float hgt=2.2f;
                kit.Box(hullWhite,L(0,y+.35f,(l0+l1)*.5f),new Vector3(lw*2,.7f,l1-l0),rot);
                kit.Sleeve(glassDark,L(0,y+1.2f,(l0+l1)*.5f),new Vector3(lw*2-.5f,1.0f,l1-l0-.5f),rot);
                kit.Box(hullWhite,L(0,y+1.95f,(l0+l1)*.5f),new Vector3(lw*2+.4f,.5f,l1-l0+.6f),rot);
                y+=hgt;
            }
            kit.Box(hullWhite,L(0,y+.9f,-length*.1f),new Vector3(hw*1.2f,.3f,1.2f),rot);
            foreach(int s in new[]{-1,1})kit.Box(hullWhite,L(s*hw*.55f,y+.4f,-length*.1f),new Vector3(.3f,1,1),rot);
        }

        // ------------------------------------------------------------ old town and hillside
        static void BuildTown(Chunks chunks)
        {
            const float lot=30;
            for(float x=-300;x<=680;x+=lot)
                for(float z=-320;z<=880;z+=lot)
                {
                    var c=new Vector3(x+(Hash(x,z,1)-.5f)*6,0,z+(Hash(x,z,2)-.5f)*6);
                    if(IsSea(c.x,c.z))continue;
                    float hx=6+Hash(x,z,3)*7,hz=6+Hash(x,z,4)*7;
                    if(IsSea(c.x-hx-4,c.z)||IsSea(c.x+hx+4,c.z)||IsSea(c.x,c.z-hz-4)||IsSea(c.x,c.z+hz+4))continue;
                    if(Reserved(new Vector2(c.x,c.z),Mathf.Max(hx,hz)+2))continue;
                    float near=MinOffset(c,hx+1.5f,hz+1.5f);
                    if(near<BuildingClear){hx*=.6f;hz*=.6f;near=MinOffset(c,hx+1.5f,hz+1.5f);if(near<BuildingClear||hx<4)continue;}
                    if(z>560&&Hash(x,z,5)<.45f)continue;   // thinning out up the hill
                    float g=Mathf.Min(Mathf.Min(TerrainHeight(c.x-hx,c.z-hz),TerrainHeight(c.x+hx,c.z-hz)),Mathf.Min(TerrainHeight(c.x-hx,c.z+hz),TerrainHeight(c.x+hx,c.z+hz)));
                    float gTop=Mathf.Max(Mathf.Max(TerrainHeight(c.x-hx,c.z-hz),TerrainHeight(c.x+hx,c.z-hz)),Mathf.Max(TerrainHeight(c.x-hx,c.z+hz),TerrainHeight(c.x+hx,c.z+hz)));
                    if(gTop-g>18)continue;   // too steep a site
                    float storeys=Mathf.Round(3+Hash(x,z,6)*(near<40?6:4)+(c.z<120?2:0));
                    float h=gTop-g+storeys*3.3f;
                    var kit=chunks.At(c);
                    var basePos=new Vector3(c.x,g-1.5f,c.z);
                    if(Hash(x,z,7)<.12f&&near>24){Cypresses(kit,c);continue;}
                    Villa(kit,basePos,hx,hz,h+1.5f,Hash(x,z,8),near<60?2:near<180?1:0);
                }
            // Cypress and palm planting along the hill roads.
            for(float d=20;d<PolygonCircuit.Length;d+=42)
            {
                if(InTunnel(d))continue;
                foreach(float side in new[]{-1f,1f})
                {
                    var p=At(d,side*11.6f,0);
                    if(IsSea(p.x,p.z)||Reserved(new Vector2(p.x,p.z),2))continue;
                    if(Mathf.Abs(PolygonCircuit.Curvature(d))>1/60f)continue;
                    if(E(d)<3)Palm(chunks,p,.85f+Hash(p.x,p.z)*.3f);else CypressAt(chunks.At(p),p,.9f+Hash(p.x,p.z)*.4f);
                }
            }
        }

        static float MinOffset(Vector3 c,float hx,float hz)
        {
            float best=float.MaxValue;
            for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)best=Mathf.Min(best,Offset(c+new Vector3(i*hx,0,j*hz)));
            return best;
        }

        // Individually named modules keep the start-straight frontage easy to revise or move.
        static void BuildStartingStraightDetail(Transform world)
        {
            var root=new GameObject("Riviera starting straight detail").transform;
            root.SetParent(world,false);
            var buildings=new GameObject("Facade modules - reposition individually").transform;
            buildings.SetParent(root,false);

            float[] distances={16,50,84,118,152};
            for(int i=0;i<distances.Length;i++)
                foreach(float side in new[]{-1f,1f})
                    StartingStraightBuilding(buildings,distances[i],side,i+(side>0?1:0));

            var furniture=new GameObject("Track furniture - grouped low-poly meshes").transform;
            furniture.SetParent(root,false);
            var kit=new PolygonSceneryKit("Riviera start straight furniture");
            BuildStartStraightGratesAndEnds(kit);
            BuildStartStraightPitWall(kit);
            BuildStartStraightSignsAndPosts(furniture,kit);
            kit.Emit(furniture);
        }

        static void StartingStraightBuilding(Transform parent,float d,float side,int variant)
        {
            float lateral=side*23f;
            var point=At(d,lateral,0);
            float ground=TerrainHeight(point.x,point.z);
            var frame=Frame(parent,$"Riviera frontage module {d:000}m - {(side>0?"harbour":"town")}",d,lateral,ground-E(d));
            float hx=4.8f+(variant%2)*.5f,hz=9.2f+(variant%3==0?.8f:0);
            float height=12.5f+(variant%3)*2.2f;
            Reserve(frame,-hx-1,hx+1,-hz-1,hz+1);

            var kit=new PolygonSceneryKit("Riviera frontage");
            var wall=stuccos[variant%stuccos.Length];
            var awning=variant%2==0?coral:teal;
            float front=-side;
            float face=front*(hx-.05f);
            // Set the main volume back from the street-facing plane to leave depth for reveals.
            kit.Box(wall,new Vector3(-side*.55f,height*.5f,0),new Vector3(hx*2-1.1f,height,hz*2));
            kit.Box(cream,new Vector3(-side*.15f,.34f,0),new Vector3(hx*2,.68f,hz*2+.25f));

            const float windowHeight=1.65f;
            int rows=Mathf.Clamp(Mathf.FloorToInt((height-4.2f)/3.2f),2,4);
            float[] bays={-5.7f,0,5.7f};
            for(int row=0;row<rows;row++)
            {
                float y=4.05f+row*3.0f;
                foreach(float z in bays)
                {
                    float paneX=front*(hx+.07f);
                    kit.Box(glassDark,new Vector3(paneX,y,z),new Vector3(.12f,windowHeight,1.55f));
                    kit.Box(wall,new Vector3(face,y,z-1.02f),new Vector3(.18f,windowHeight+.16f,.22f));
                    kit.Box(wall,new Vector3(face,y,z+1.02f),new Vector3(.18f,windowHeight+.16f,.22f));
                    kit.Box(wall,new Vector3(face,y-windowHeight*.5f-.12f,z),new Vector3(.18f,.22f,2.26f));
                    kit.Box(wall,new Vector3(face,y+windowHeight*.5f+.12f,z),new Vector3(.18f,.22f,2.26f));
                    kit.Box(cream,new Vector3(face+front*.08f,y-windowHeight*.5f-.08f,z),new Vector3(.28f,.12f,2.35f));
                }
                foreach(float z in new[]{-8.25f,-2.85f,2.85f,8.25f})
                    kit.Box(wall,new Vector3(face,y,z),new Vector3(.18f,windowHeight+.22f,z== -8.25f||z==8.25f?3.5f:3.8f));
                kit.Box(wall,new Vector3(face,y+1.5f,0),new Vector3(.18f,.89f,hz*2));

                if(row%2==0)
                {
                    float balconyY=y-windowHeight*.5f-.22f;
                    float balconyX=front*(hx+.48f);
                    kit.Box(cream,new Vector3(balconyX,balconyY,0),new Vector3(1.12f,.2f,4.1f));
                    kit.Beam(railSteel,new Vector3(front*(hx+1.02f),balconyY+.72f,-2.02f),
                        new Vector3(front*(hx+1.02f),balconyY+.72f,2.02f),.07f,.08f);
                    foreach(float z in new[]{-1.9f,-.95f,0f,.95f,1.9f})
                        kit.Beam(railSteel,new Vector3(front*(hx+1.02f),balconyY+.12f,z),
                            new Vector3(front*(hx+1.02f),balconyY+.72f,z),.055f,.055f);
                }
            }

            // Deep entry, a framed door and a broad striped awning are readable at speed.
            float doorX=front*(hx+.07f);
            foreach(float z in new[]{-5.7f,5.7f})
            {
                kit.Box(glassDark,new Vector3(doorX,1.65f,z),new Vector3(.12f,1.8f,3.55f));
                kit.Box(wall,new Vector3(face,1.65f,z-1.92f),new Vector3(.18f,2.1f,.22f));
                kit.Box(wall,new Vector3(face,1.65f,z+1.92f),new Vector3(.18f,2.1f,.22f));
                kit.Box(wall,new Vector3(face,.54f,z),new Vector3(.18f,.24f,4.05f));
                kit.Box(wall,new Vector3(face,2.76f,z),new Vector3(.18f,.24f,4.05f));
            }
            kit.Box(glassDark,new Vector3(doorX,1.52f,0),new Vector3(.14f,2.75f,1.35f));
            kit.Box(cream,new Vector3(face,1.55f,-.82f),new Vector3(.24f,3.05f,.18f));
            kit.Box(cream,new Vector3(face,1.55f,.82f),new Vector3(.24f,3.05f,.18f));
            kit.Box(cream,new Vector3(face,3.12f,0),new Vector3(.24f,.18f,1.82f));
            kit.Box(wall,new Vector3(face,3.35f,0),new Vector3(.25f,.3f,2.05f));
            float canopyX=front*(hx+.75f);
            kit.Box(awning,new Vector3(canopyX,3.45f,0),new Vector3(1.55f,.2f,5.4f),
                Quaternion.Euler(0,0,-front*7f));
            kit.Box(cream,new Vector3(canopyX+front*.02f,3.34f,-1.38f),new Vector3(1.42f,.12f,2.58f));
            foreach(float z in new[]{-2.45f,2.45f})
                kit.Beam(navy,new Vector3(front*(hx+.15f),2.55f,z),
                    new Vector3(front*(hx+1.38f),3.36f,z),.09f,.09f);

            // Contrasting cornice and one of three distinct rooftop silhouettes.
            kit.Box(cream,new Vector3(0,height+.16f,0),new Vector3(hx*2+.45f,.32f,hz*2+.45f));
            if(variant%3==0)
                kit.Chamfered(roofTile,new Vector3(0,height+.25f,0),hx+.3f,hz+.3f,hx*.35f,hz*.35f,0,2.6f,.35f);
            else if(variant%3==1)
            {
                kit.Box(verdigris,new Vector3(0,height+.55f,0),new Vector3(hx*1.45f,.35f,hz*1.35f));
                foreach(float x in new[]{-hx*.45f,hx*.45f})
                    foreach(float z in new[]{-hz*.4f,hz*.4f})
                        kit.Column(cream,new Vector3(x,height+.75f,z),.12f,.1f,2f,6);
                kit.Box(roofTile,new Vector3(0,height+2.78f,0),new Vector3(hx*1.65f,.25f,hz*1.55f));
            }
            else
            {
                kit.Box(roofTile,new Vector3(side*1.4f,height+1.05f,-hz*.25f),new Vector3(2.1f,2.1f,3.2f));
                kit.Chamfered(cream,new Vector3(side*1.4f,height+2.1f,-hz*.25f),1.25f,1.8f,.95f,1.4f,0,.65f,.2f);
                kit.Box(verdigris,new Vector3(-side*1.8f,height+1.1f,hz*.28f),new Vector3(2.4f,2.2f,2.6f));
            }
            kit.Emit(frame);
        }

        static void BuildStartStraightGratesAndEnds(PolygonSceneryKit kit)
        {
            for(float d=8;d<180;d+=34)
                foreach(float side in new[]{-1f,1f})
                {
                    var centre=At(d,side*8.12f,.12f);
                    kit.Box(railSteel,centre,new Vector3(.68f,.08f,1.05f),Quaternion.LookRotation(T(d)));
                    for(int i=-3;i<=3;i++)
                    {
                        float lateral=side*8.12f+i*.085f;
                        kit.Beam(fence,At(d,lateral,.18f),At(d,lateral,.18f)+T(d)*.92f,.035f,.035f);
                    }
                }

            foreach(float d in new[]{0f,176f})
                foreach(float side in new[]{-1f,1f})
                {
                    var end=At(d,side*9.15f,.78f);
                    kit.Chamfered(coral,end,.28f,.38f,.23f,.30f,-.7f,.7f,.1f);
                    kit.Box(cream,end+Vector3.up*.34f,new Vector3(.08f,.16f,.5f),Quaternion.LookRotation(T(d)));
                }
        }

        static void BuildStartStraightPitWall(PolygonSceneryKit kit)
        {
            const float side=-1f;
            for(float d=6;d<174;d+=12)
            {
                float mid=d+6;
                var baseCentre=At(mid,side*10.65f,.65f);
                var rotation=Quaternion.LookRotation(T(mid));
                kit.Box(navy,baseCentre,new Vector3(.62f,1.3f,12.08f),rotation);
                kit.Box(cream,At(mid,side*10.65f,1.36f),new Vector3(.88f,.16f,12.14f),rotation);
                foreach(float z in new[]{-5.1f,5.1f})
                    kit.Box(gold,At(mid,side*10.65f,1.49f)+T(mid)*z,new Vector3(.9f,.08f,.18f),rotation);
            }
        }

        static void BuildStartStraightSignsAndPosts(Transform parent,PolygonSceneryKit kit)
        {
            foreach(var (d,side,text,color) in new[]{
                (42f,-1f,"AZUR MARINA",teal),(94f,1f,"RIVIERA GRAND PRIX",coral),(150f,-1f,"POLYGON RACING",navy)})
            {
                float lateral=side*14.2f;
                var point=At(d,lateral,0);
                var sign=Frame(parent,"Original sponsor board - "+text,d,lateral,TerrainHeight(point.x,point.z)-E(d));
                var rotation=Quaternion.LookRotation(T(d));
                kit.Box(navy,At(d,lateral,2.8f),new Vector3(.28f,1.65f,8.6f),rotation);
                kit.Box(color,At(d,lateral-side*.17f,3.55f),new Vector3(.08f,.16f,8.55f),rotation);
                foreach(float z in new[]{-3.25f,3.25f})
                {
                    var post=At(d,lateral,1.35f)+T(d)*z;
                    kit.Box(railSteel,post,new Vector3(.22f,2.7f,.24f),rotation);
                    kit.Box(navy,At(d,lateral,.12f)+T(d)*z,new Vector3(.72f,.24f,.62f),rotation);
                }
                var label=PolygonWorld.Label("Sponsor lettering",sign,text,new Vector3(-side*.17f,2.85f,0),.11f,cream.color);
                label.transform.localRotation=Quaternion.Euler(0,side>0?-90:90,0);
            }

            foreach(var (d,side) in new[]{(22f,-1f),(82f,1f),(142f,-1f)})
            {
                var point=At(d,side*14.15f,0);
                var frame=Frame(parent,"Marshal post "+Mathf.RoundToInt(d),d,side*14.15f,TerrainHeight(point.x,point.z)-E(d));
                var marshal=new PolygonSceneryKit("Riviera marshal post");
                marshal.Column(navy,new Vector3(0,0,0),.3f,.22f,2.1f,6);
                marshal.Box(ochre,new Vector3(0,2.85f,0),new Vector3(2.7f,1.5f,2.8f));
                marshal.Box(glassDark,new Vector3(-side*1.38f,2.95f,0),new Vector3(.08f,.72f,1.85f));
                marshal.Box(coral,new Vector3(0,3.68f,0),new Vector3(3.05f,.22f,3.1f));
                marshal.Box(cream,new Vector3(-side*1.55f,2.1f,0),new Vector3(.22f,.24f,2.4f));
                marshal.Emit(frame);
            }
        }

        // Mediterranean apartment: stucco walls, stacked shuttered windows, terracotta hip roof.
        static void Villa(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,float pick,int detail)
        {
            var wall=stuccos[Mathf.FloorToInt(pick*stuccos.Length)%stuccos.Length];
            if(detail==0)
            {
                kit.Box(wall,c+Vector3.up*(h*.5f),new Vector3(hx*2,h,hz*2));
            }
            else
            {
                float baseH=Mathf.Max(2.2f,h-Mathf.Floor((h-2.2f)/3.3f)*3.3f);
                kit.Box(wall,c+Vector3.up*(baseH*.5f),new Vector3(hx*2,baseH,hz*2));
                Facade(kit,c,hx,hz,baseH,h,detail==2?3.3f:6.6f,.4f,wall,glassDark);
                if(detail==2)
                {
                    // Green shutters beside some windows on the two long faces, standing well proud.
                    for(float y=baseH+1.6f;y<h-.6f;y+=3.3f)
                        for(float u=-hx+2;u<=hx-2;u+=3.4f)
                            foreach(int f in new[]{-1,1})
                                if(Hash(c.x+u,y,f)<.55f)kit.Box(shutterGreen,c+new Vector3(u+.9f,y,f*(hz+.32f)),new Vector3(.6f,1.5f,.14f));
                    // Balcony slabs on the long faces.
                    for(float y=baseH+3.3f;y<h-.6f;y+=6.6f)
                        foreach(int f in new[]{-1,1})
                            kit.Box(cream,c+new Vector3(0,y,f*(hz+.7f)),new Vector3(hx*1.2f,.22f,1.4f));
                }
            }
            // Hip roof (or a flat roof terrace with a pergola on some).
            if(pick<.78f)kit.Chamfered(roofTile,c+Vector3.up*h,hx+.6f,hz+.6f,Mathf.Max(.4f,hx*.25f),Mathf.Max(.4f,hz*.25f),0,Mathf.Min(hx,hz)*.55f,.3f);
            else
            {
                kit.Box(cream,c+Vector3.up*(h+.45f),new Vector3(hx*2+.4f,.9f,hz*2+.4f));
                kit.Box(garden,c+Vector3.up*(h+1.0f),new Vector3(hx*1.2f,.3f,hz*1.2f));
                if(detail>0)kit.Box(coral,c+new Vector3(hx*.3f,h+3,0),new Vector3(hx*.8f,.12f,hz*1.2f));
            }
        }

        // Alternating wall slabs and recessed glass (no overlapping surfaces; see Downtown notes).
        static void Facade(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float y0,float y1,float step,float glassShare,Material wall,Material glass)
        {
            float y=y0;
            while(y+step<=y1+.01f)
            {
                float g=step*glassShare;
                kit.Box(wall,c+Vector3.up*(y+(step-g)*.5f),new Vector3(hx*2,step-g,hz*2));
                kit.Sleeve(glass,c+Vector3.up*(y+step-g*.5f),new Vector3(hx*2-.7f,g,hz*2-.7f));
                y+=step;
            }
            if(y1-y>.05f)kit.Box(wall,c+Vector3.up*((y+y1)*.5f),new Vector3(hx*2,y1-y,hz*2));
        }

        static void Cypresses(PolygonSceneryKit kit,Vector3 c)
        {
            for(int i=0;i<5;i++)
            {
                var p=c+new Vector3((Hash(c.x,i)-.5f)*16,0,(Hash(c.z,i)-.5f)*16);p.y=TerrainHeight(p.x,p.z);
                CypressAt(kit,p,.8f+Hash(p.x,p.z)*.6f);
            }
        }
        static void CypressAt(PolygonSceneryKit kit,Vector3 p,float s)
        {
            kit.Column(palmBark,p+Vector3.down*.3f,.2f*s,.15f*s,1.4f*s,5);
            kit.Column(cypress,p+Vector3.up*1*s,1.1f*s,.9f*s,4*s,6);
            kit.Column(cypress,p+Vector3.up*5*s,.9f*s,.05f*s,6*s,6);
        }

        static Mesh[] palmTrunks,palmA,palmB;
        static void Palm(Chunks chunks,Vector3 p,float scale)
        {
            if(palmTrunks==null)
            {
                int n=PolygonSceneryMeshes.PalmVariantCount;palmTrunks=new Mesh[n];palmA=new Mesh[n];palmB=new Mesh[n];
                for(int v=0;v<n;v++){palmTrunks[v]=PolygonSceneryMeshes.PalmTrunk(v);palmA[v]=PolygonSceneryMeshes.PalmCrown(v,0);palmB[v]=PolygonSceneryMeshes.PalmCrown(v,1);}
            }
            int variant=Mathf.Abs(Mathf.RoundToInt(p.x*7+p.z*13))%palmTrunks.Length;
            var t=new GameObject("Riviera palm").transform;t.SetParent(chunksRoot,false);
            t.localPosition=new Vector3(p.x,p.y-.2f,p.z);t.localRotation=Quaternion.Euler(0,Hash(p.x,p.z,3)*360,0);t.localScale=Vector3.one*scale*.8f;
            Part(t,palmTrunks[variant],palmBark);Part(t,palmA[variant],palmLeaf);Part(t,palmB[variant],palmLeaf2);
        }

        // ------------------------------------------------------------ hairpin hotel, pool, lamps, boards
        static void BuildHairpinHotel(Transform world)
        {
            // Big modern hotel on the outside of the hairpin, curved by stepping its wings.
            var c=new Vector3(318,0,214);float g=TerrainHeight(c.x,c.z)-3;
            var t=new GameObject("Hairpin grand hotel").transform;t.SetParent(world,false);t.localPosition=new Vector3(c.x,g,c.z);
            var kit=new PolygonSceneryKit("Hairpin hotel");
            for(int w=0;w<3;w++)
            {
                var wc=new Vector3(-6+w*4,0,-34+w*34);
                Facade(kit,wc,18,15,0,36,3.4f,.45f,stucco3,glassDark);
                for(float y=3.4f;y<36;y+=3.4f)kit.Box(cream,wc+new Vector3(18.9f,y-.2f,0),new Vector3(1.8f,.25f,28));   // balconies
                kit.Box(teal,wc+Vector3.up*36.5f,new Vector3(37,1,31));
            }
            kit.Emit(t);ReserveWorld(290,350,160,268);
        }

        static void BuildPool(Chunks chunks)
        {
            var c=new Vector3(90,E(DistanceAt(new Vector3(90,0,-296)))-.2f,-258);
            var kit=chunks.At(c);
            kit.Box(poolTile,c+Vector3.up*-.5f,new Vector3(64,1.4f,26));
            kit.Box(poolBlue,c+Vector3.up*.08f,new Vector3(50,.2f,16));
            for(int k=0;k<6;k++)kit.Box(cream,c+new Vector3(-22.5f+k*9,.24f,0),new Vector3(.25f,.08f,16));
            kit.Box(coral,c+new Vector3(-26,4,0),new Vector3(.6f,.4f,18));
            ReserveWorld(54,126,-274,-242);
        }

        static void BuildLampsAndBoards(Transform world,Chunks chunks)
        {
            int index=0;
            for(float d=10;d<PolygonCircuit.Length;d+=30,index++)
            {
                if(InTunnel(d))continue;
                foreach(float side in new[]{-1f,1f})
                {
                    if((index+(side>0?1:0))%2!=0)continue;
                    var p=At(d,side*12.4f,.15f);
                    if(IsSea(p.x,p.z)||Reserved(new Vector2(p.x,p.z),1.5f))continue;
                    var kit=chunks.At(p);
                    kit.Column(navy,p,.16f,.09f,5.2f,6);
                    kit.Box(gold,p+Vector3.up*5.25f,new Vector3(.36f,.18f,.36f));
                    kit.Chamfered(glassDark,p+Vector3.up*5.35f,.32f,.32f,.42f,.42f,0,.6f,.12f);
                    kit.Box(lampGlow,p+Vector3.up*5.65f,new Vector3(.5f,.3f,.5f));
                    kit.Chamfered(navy,p+Vector3.up*5.95f,.5f,.5f,.05f,.05f,0,.45f,.15f);
                }
            }
            // Distance boards before the slow corners.
            var ink=Mat("Board ink",new Color(.98f,.97f,.93f));float last=-999;
            for(float d=0;d<PolygonCircuit.Length;d+=3)
            {
                if(Mathf.Abs(PolygonCircuit.Curvature(d))<1/40f||d-last<150)continue;
                last=d;float turn=Mathf.Sign(PolygonCircuit.Curvature(d+6));
                foreach(int m in new[]{100,50})
                {
                    float bd=d-m;if(bd<40&&bd>-40||InTunnel(bd))continue;
                    var b=Frame(world,"Riviera corner board "+m,bd,-turn*11.2f,.15f);
                    PolygonWorld.Box("Board post",b,new Vector3(0,1.2f,0),new Vector3(.16f,2.4f,.16f),navy);
                    PolygonWorld.Box("Board panel",b,new Vector3(0,2.7f,0),new Vector3(1.8f,1.1f,.16f),coral);
                    PolygonWorld.Label("Board lettering",b,m.ToString(),new Vector3(0,2.7f,-.13f),.07f,ink.color);
                }
            }
        }

        // ------------------------------------------------------------ entry point
        public static Transform Build(Transform circuitRoot)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.Riviera)
                throw new System.InvalidOperationException("Select the Riviera layout before building its scenery.");
            CreatePalette();reserved.Clear();palmTrunks=null;
            tunnelStart=PolygonCircuit.DistanceAtKnot(28.7f);tunnelEnd=PolygonCircuit.DistanceAtKnot(32.6f);
            PrepareTerrainSamples();
            var world=new GameObject(SceneryName).transform;world.SetParent(circuitRoot,false);
            chunksRoot=new GameObject("Riviera props").transform;chunksRoot.SetParent(world,false);
            var chunks=new Chunks();
            BuildWater(world);
            BuildRoad(chunks);
            BuildRailsAndPavements(chunks);
            BuildTunnel(world,chunks);
            BuildStartArea(world);
            BuildStartingStraightDetail(world);
            BuildCasinoSquare(world,chunks);
            BuildHairpinHotel(world);
            BuildPool(chunks);
            BuildQuays(chunks);
            BuildHarbourBoats(world,chunks);
            BuildRock(world);
            BuildLampsAndBoards(world,chunks);
            BuildTown(chunks);
            BuildTerrain(chunks);
            chunks.Emit(world);
            reserved.Clear();trackSamples=null;trackHeights=null;
            return world;
        }
    }
}
