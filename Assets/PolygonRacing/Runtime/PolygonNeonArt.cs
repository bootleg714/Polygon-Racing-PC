using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing
{
    // Neon Meridian: editor-baked city, built from the same route used by the driver and AI.
    // Opaque geometry, a small shared palette and 180 m chunks keep the night city affordable.
    public static class PolygonNeonArt
    {
        public const string SceneryName="Circuit - editable scenery";
        public const string AnimatedName="Neon Meridian - moving transit";
        public const float Barrier=12f, DeckEdge=13.2f;
        const float ChunkSize=180;
        static Material road,concrete,wall,wallAlt,steel,glass,ground,rock,water,paint,
            cyan,magenta,violet,amber,coolWindow,warmWindow,black,green,dataCyan,dataPink;
        static Vector3[] route;
        static float[] elevations;
        static readonly List<Bounds> occupied=new List<Bounds>();
        public static int BuildingCount {get;private set;}

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit))
                {kit=new PolygonSceneryKit("Neon sector "+key.x+","+key.y){MaxVerticesPerMesh=60000};kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform parent)
            {
                foreach(var pair in kits)
                {
                    var child=new GameObject("City sector "+pair.Key.x+","+pair.Key.y).transform;
                    child.SetParent(parent,false);pair.Value.Emit(child);
                }
            }
        }

        static Material Mat(string name,Color color,float glow=0,float shine=.15f)
        {
            var shader=Resources.Load<Shader>("PolygonFlatPC");
            if(!shader)shader=PolygonLook.LitShader();
            var m=new Material(shader){name="NM "+name,color=color,enableInstancing=true};
            m.SetFloat("_Smoothness",shine);m.SetFloat("_Metallic",0);
            m.SetColor("_SkyAmbient",new Color(.30f,.38f,.62f));
            m.SetColor("_GroundAmbient",new Color(.12f,.14f,.24f));
            m.SetFloat("_AmbientStrength",1);m.SetFloat("_SunWrap",.2f);
            m.SetFloat("_ReflectionStrength",.18f);
            if(glow>0)m.SetColor("_EmissionColor",color*glow);
            return m;
        }
        static void Palette()
        {
            road=Mat("Asphalt",new Color(.105f,.12f,.17f),0,.26f);
            concrete=Mat("Graphite deck",new Color(.18f,.22f,.30f));
            wall=Mat("Midnight facade",new Color(.15f,.20f,.31f));
            wallAlt=Mat("Plum facade",new Color(.25f,.18f,.32f));
            steel=Mat("Titanium frame",new Color(.35f,.42f,.53f),0,.4f);
            glass=Mat("Obsidian glass",new Color(.045f,.095f,.16f),0,.6f);
            ground=Mat("Terraced city ground",new Color(.085f,.10f,.16f));
            rock=Mat("Basalt escarpment",new Color(.13f,.12f,.20f));
            water=Mat("Night harbour",new Color(.025f,.065f,.13f),.08f,.65f);
            paint=Mat("Road markings",new Color(.72f,.83f,.91f),.12f);
            cyan=Mat("Cyan light",new Color(.035f,.8f,1),3.2f);
            magenta=Mat("Magenta light",new Color(1,.035f,.42f),2.5f);
            violet=Mat("Violet light",new Color(.42f,.15f,1),2.6f);
            amber=Mat("Amber guidance",new Color(1,.43f,.075f),2.3f);
            coolWindow=Mat("Cool window",new Color(.20f,.57f,.85f),1.15f);
            warmWindow=Mat("Warm window",new Color(1,.65f,.29f),1.0f);
            black=Mat("Black recess",new Color(.025f,.035f,.06f));
            green=Mat("Terrace planting",new Color(.055f,.20f,.17f));
            var panel=Resources.Load<Shader>("PolygonNeonPanel");
            if(!panel)throw new InvalidOperationException("PolygonNeonPanel.shader must finish importing before building Neon Meridian.");
            dataCyan=new Material(panel){name="NM Cyan data cascade",enableInstancing=true};
            dataCyan.SetColor("_BaseColor",new Color(.012f,.045f,.09f));dataCyan.SetColor("_EmissionColor",new Color(.05f,1.5f,2.5f));
            dataCyan.SetFloat("_ScrollSpeed",.22f);dataCyan.SetFloat("_PatternScale",.42f);
            dataPink=new Material(dataCyan){name="NM Magenta signal panels"};
            dataPink.SetColor("_EmissionColor",new Color(2.2f,.03f,.65f));dataPink.SetFloat("_ScrollSpeed",-.08f);
        }

        public static Vector3 RoadPoint(float d,float lateral=0,float up=0)=>PolygonBanking.SurfacePoint(d,lateral,up);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float D(float k)=>PolygonCircuit.DistanceAtKnot(k);
        static float Hash(float x,float z,int seed=0)=>Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f+seed*37.719f)*43758.5453f,1);
        static Material Accent(int i)=>i%3==0?cyan:i%3==1?magenta:violet;
        static void PrepareTerrain()
        {
            const int n=256;route=new Vector3[n];elevations=new float[n];
            for(int i=0;i<n;i++){float d=PolygonCircuit.Length*i/n;route[i]=PolygonCircuit.AtDistance(d);elevations[i]=PolygonCircuit.Elevation(d);}
        }
        public static float GroundHeight(float x,float z)
        {
            float best=float.MaxValue,height=0;
            var p=new Vector3(x,0,z);
            for(int i=0;i<route.Length;i++)
            {float sq=(route[i]-p).sqrMagnitude;if(sq<best){best=sq;height=elevations[i];}}
            // The city slopes away below its highway. Nearby lower sections cut the escarpment
            // back too, preventing a high terrace from burying a lower part of the circuit.
            float result=height-10-Mathf.Min(28,Mathf.Sqrt(best)*.075f);
            for(int i=0;i<route.Length;i++)
            {
                float dist=Vector3.Distance(route[i],p);
                if(dist<125)result=Mathf.Min(result,elevations[i]-7+Mathf.Max(0,dist-28)*.16f);
            }
            return Mathf.Max(-24,result);
        }
        static void Ribbon(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float up)
        {
            var a=RoadPoint(d0,l0,up);var b=RoadPoint(d0,l1,up);var c=RoadPoint(d1,l1,up);var d=RoadPoint(d1,l0,up);
            kit.Quad(m,a,b,c,d,(a+b+c+d)*.25f+Vector3.down);
        }
        static void RoadSlab(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float y0,float y1)
        {
            kit.Prism(m,new[]{RoadPoint(d0,l0,y0),RoadPoint(d0,l1,y0),RoadPoint(d1,l1,y0),RoadPoint(d1,l0,y0)},
                new[]{RoadPoint(d0,l0,y1),RoadPoint(d0,l1,y1),RoadPoint(d1,l1,y1),RoadPoint(d1,l0,y1)});
        }
        static void BuildRoad(Chunks chunks)
        {
            float length=PolygonCircuit.Length;
            for(float d=0;d<length;d+=4)
            {
                float end=Mathf.Min(d+4,length);var kit=chunks.At(RoadPoint(d));
                RoadSlab(kit,concrete,d,end,-DeckEdge,DeckEdge,-1.9f,-.025f);
                Ribbon(kit,road,d,end,-10,10,0);
                foreach(float side in new[]{-1f,1f})
                {
                    float lo=side>0?10:-Barrier,hi=side>0?Barrier:-10;
                    Ribbon(kit,concrete,d,end,lo,hi,.035f);
                    Ribbon(kit,paint,d,end,side*9.6f,side*9.78f,.022f);
                    RoadSlab(kit,black,d,end,side*Barrier,side*(Barrier+.55f),0,1.1f);
                    kit.Beam(cyan,RoadPoint(d,side*(Barrier-.025f),1.05f),RoadPoint(end,side*(Barrier-.025f),1.05f),.10f,.12f);
                    // Broad, dim edge fill suggests neon spill without a local light per segment.
                    Ribbon(kit,coolWindow,d,end,side*10.1f,side*10.22f,.05f);
                    if(((int)d/4)%4==0)kit.Beam(steel,RoadPoint(d,side*(Barrier+.2f),.1f),RoadPoint(d,side*(Barrier+.2f),1.4f),.10f,.10f);
                }
                if(d%24<8)
                    foreach(float lateral in new[]{-3.33f,3.33f})Ribbon(kit,paint,d,end,lateral-.055f,lateral+.055f,.025f);
            }
            // Deck piers follow the actual underside, rather than floating above the hillside.
            for(float d=24;d<length;d+=52)
            {
                var p=RoadPoint(d);float floor=GroundHeight(p.x,p.z)-1;
                float h=p.y-1.9f-floor;if(h<1)continue;var kit=chunks.At(p);
                foreach(float side in new[]{-1f,1f})
                {
                    var q=p+S(d)*side*8.5f;q.y=floor;
                    kit.Box(concrete,q+Vector3.up*h*.5f,new Vector3(2.3f,h,3.4f),Quaternion.LookRotation(T(d)));
                    kit.Beam(violet,q+Vector3.up*.5f,q+Vector3.up*(h-.2f),.08f,.08f);
                }
            }
            // Start/finish and staggered four-car grid, all conformed to grade.
            var startKit=chunks.At(RoadPoint(0));
            for(int row=0;row<2;row++)for(int col=0;col<20;col++)
                Ribbon(startKit,(row+col)%2==0?paint:black,row*.6f,(row+1)*.6f,-10+col,-9+col,.035f);
            for(int row=0;row<4;row++)
            {
                float d=PolygonCircuit.Length-9-row*8;
                foreach(float side in new[]{-1f,1f})Ribbon(chunks.At(RoadPoint(d)),paint,d,d+.13f,side*3-1,side*3+1,.035f);
            }
        }
        static void BuildTerrain(Chunks chunks)
        {
            const int step=44;
            for(int x=-880;x<1060;x+=step)for(int z=-490;z<1240;z+=step)
            {
                var a=new Vector3(x,GroundHeight(x,z),z);
                var b=new Vector3(x+step,GroundHeight(x+step,z),z);
                var c=new Vector3(x+step,GroundHeight(x+step,z+step),z+step);
                var d=new Vector3(x,GroundHeight(x,z+step),z+step);
                chunks.At(a).Quad(((x/step+z/step)&3)==0?rock:ground,a,b,c,d,(a+b+c+d)*.25f+Vector3.down);
            }
            var sea=new PolygonSceneryKit("Neon harbour");
            sea.Box(water,new Vector3(0,-27.5f,400),new Vector3(7000,1,7000));
            var parent=terrainParent;sea.Emit(parent);
        }
        static Transform terrainParent;

        static bool CanPlace(Vector3 p,float width,float depth,Quaternion rotation,float margin=6)
        {
            // All corners and edge midpoints must clear every part of the circuit.
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                var v=p+rotation*new Vector3(x*width*.5f,0,z*depth*.5f);
                PolygonCircuit.ProjectLapFlat(v,out float offset);
                if(offset<DeckEdge+margin)return false;
            }
            // Conservative axis-aligned footprints also keep adjacent lots from overlapping.
            var ex=rotation*new Vector3(width*.5f+2,0,0);var ez=rotation*new Vector3(0,0,depth*.5f+2);
            var bounds=new Bounds(new Vector3(p.x,0,p.z),new Vector3((Mathf.Abs(ex.x)+Mathf.Abs(ez.x))*2,2,(Mathf.Abs(ex.z)+Mathf.Abs(ez.z))*2));
            foreach(var b in occupied)if(b.Intersects(bounds))return false;
            occupied.Add(bounds);return true;
        }
        static void Tower(PolygonSceneryKit kit,Vector3 p,float width,float depth,float height,Quaternion rotation,int seed,bool detailed)
        {
            Vector3 V(float x,float y,float z)=>p+rotation*new Vector3(x,y,z);
            Material body=seed%3==0?wallAlt:wall,accent=Accent(seed);
            float podium=Mathf.Min(10,height*.23f);
            kit.Box(concrete,V(0,1,0),new Vector3(width+2,2,depth+2),rotation);
            kit.Box(body,V(0,podium*.5f+2,0),new Vector3(width,podium,depth),rotation);
            kit.Box(glass,V(0,podium+(height-podium)*.5f,0),new Vector3(width*.88f,height-podium,depth*.88f),rotation);
            // Staggered occupied floors: opaque bands in front of the glass, with gaps for recesses.
            float spacing=detailed?5.2f:10;
            for(float y=podium+3;y<height-2;y+=spacing)
            {
                kit.Box(body,V(0,y,0),new Vector3(width*.91f,.8f,depth*.91f),rotation);
                if(detailed)
                {
                    for(int face=0;face<4;face++)
                    {
                        float f=(seed+face+(int)y)%5;
                        if(f==0)continue;
                        var q=rotation*Quaternion.Euler(0,face*90,0);
                        float span=face%2==0?width:depth,front=face%2==0?depth:width;
                        var local=Quaternion.Euler(0,face*90,0)*new Vector3(0,0,front*.446f+.04f);
                        kit.Box((seed+face+(int)y)%3==0?warmWindow:coolWindow,p+rotation*(local+Vector3.up*(y+1.6f)),new Vector3(span*.56f,.65f,.08f),q);
                    }
                }
            }
            // One lit architectural edge and a second, shorter edge keep the silhouette legible.
            foreach(int side in new[]{-1,1})
            {
                float top=side<0?height:height*.72f;
                kit.Beam(accent,V(side*width*.448f,podium,-depth*.448f),V(side*width*.448f,top,-depth*.448f),.13f,.13f);
            }
            kit.Box(body,V(0,height+1.2f,0),new Vector3(width*.96f,2.4f,depth*.96f),rotation);
            kit.Beam(accent,V(-width*.49f,height+2.5f,-depth*.48f),V(width*.49f,height+2.5f,-depth*.48f),.22f,.22f);
            if(seed%4==0)
            {
                kit.Box(body,V(width*.10f,height+7,0),new Vector3(width*.52f,12,depth*.60f),rotation);
                kit.Beam(accent,V(width*.10f,height+13,0),V(width*.10f,height+22,0),.25f,.25f);
            }
            else if(seed%4==1)
            {
                // Sloping illuminated crowns break up the otherwise rectilinear skyline.
                kit.Prism(body,new[]{V(-width*.46f,height+2,-depth*.46f),V(width*.46f,height+2,-depth*.46f),V(width*.46f,height+2,depth*.46f),V(-width*.46f,height+2,depth*.46f)},
                    new[]{V(-width*.10f,height+21,-depth*.30f),V(width*.36f,height+21,-depth*.30f),V(width*.36f,height+21,depth*.30f),V(-width*.10f,height+21,depth*.30f)});
                kit.Beam(accent,V(-width*.47f,height+2,-depth*.47f),V(-width*.11f,height+21,-depth*.31f),.28f,.28f);
                kit.Beam(accent,V(-width*.11f,height+21,-depth*.31f),V(width*.37f,height+21,-depth*.31f),.28f,.28f);
            }
            else if(seed%4==2)
            {
                for(int tier=0;tier<3;tier++)
                {
                    float scale=.83f-tier*.20f,y=height+3+tier*5;
                    kit.Box(body,V(0,y,0),new Vector3(width*scale,5,depth*scale),rotation);
                    kit.Box(accent,V(0,y+2.6f,0),new Vector3(width*scale+.15f,.10f,depth*scale+.15f),rotation);
                }
            }
            if(detailed&&height>38)
            {
                // Animated luminous facade with a solid frame; geometry stays outside the wall.
                float signH=Mathf.Min(26,height*.40f),signW=width*.38f;
                var sign=V(width*.13f,podium+signH*.7f,-depth*.46f-.18f);
                kit.Box(black,sign,new Vector3(signW+.8f,signH+.8f,.34f),rotation);
                kit.Box(seed%2==0?dataCyan:dataPink,sign+rotation*Vector3.back*.20f,new Vector3(signW,signH,.08f),rotation);
            }
            BuildingCount++;
        }

        static void BuildCity(Chunks chunks)
        {
            // Two roadside bands, with wider gaps through the Crown and open descent.
            int seed=0;
            for(float d=65;d<PolygonCircuit.Length-60;d+=64)
            {
                float knot=PolygonCircuit.AngleAtDistance(d)/(Mathf.PI*2)*PolygonCircuit.KnotCount;
                bool crownVista=knot>10.0f&&knot<15.2f;
                bool overlook=knot>15.2f&&knot<19.5f;
                foreach(int side in new[]{-1,1})for(int band=0;band<2;band++)
                {
                    int id=seed++;if(crownVista||(overlook&&side>0))continue;
                    if(knot>27.4f&&knot<30.8f)continue; // Tunnel has its own service architecture.
                    float w=20+Hash(d,side+band,11)*20,depth=20+Hash(d,band,32)*18;
                    float lateral=side*(34+w*.25f+band*64);
                    var p=RoadPoint(d,lateral);p.y=GroundHeight(p.x,p.z);
                    var rotation=Quaternion.LookRotation(T(d))*Quaternion.Euler(0,side<0?180:0,0);
                    if(!CanPlace(p,w+4,depth+4,rotation))continue;
                    float h=28+Hash(d,side,82)*78+(band>0?32:0);
                    Tower(chunks.At(p),p,w,depth,h,rotation,id,true);
                    if(band==0&&id%3==0)TerraceGarden(chunks.At(p),p+rotation*new Vector3(-w*.25f,2.2f,-depth*.5f-3),rotation);
                }
            }
            // Infield and outer skyline. Footprint rejection reserves sightlines and track space.
            for(int x=-780;x<=980;x+=77)for(int z=-360;z<=1150;z+=77)
            {
                var p=new Vector3(x+(Hash(x,z,1)-.5f)*28,0,z+(Hash(x,z,2)-.5f)*28);
                PolygonCircuit.ProjectLapFlat(p,out float offset);
                if(offset<75||offset>450)continue;
                float w=24+Hash(x,z,4)*25,depth=24+Hash(x,z,5)*28;
                var rotation=Quaternion.Euler(0,Hash(x,z,8)>.6f?22:0,0);
                if(!CanPlace(p,w,depth,rotation,14))continue;
                p.y=GroundHeight(p.x,p.z);
                float h=32+Mathf.Pow(Hash(x,z,6),2)*150;
                // Reserve a clear shoulder at the Crown: the landmark and lower skyline
                // should be visible from the driver's seat, not only from an aerial camera.
                float nearest=PolygonCircuit.AngleAtDistance(PolygonCircuit.ProjectLapFlat(p,out _)*PolygonCircuit.Length)/(Mathf.PI*2)*PolygonCircuit.KnotCount;
                if(nearest>10&&nearest<15.5f&&offset<110)continue;
                Tower(chunks.At(p),p,w,depth,h,rotation,seed++,offset<180);
            }
        }
        static void TerraceGarden(PolygonSceneryKit kit,Vector3 p,Quaternion rotation)
        {
            kit.Box(concrete,p,new Vector3(9,1,3),rotation);
            kit.Box(green,p+Vector3.up*.9f,new Vector3(8,.8f,2.4f),rotation);
            for(int i=-1;i<=1;i++)
            {
                var q=p+rotation*new Vector3(i*3,.7f,0);
                kit.Column(black,q,.18f,.12f,3.3f,6);
                kit.Column(green,q+Vector3.up*2.7f,1.2f,.35f,2.8f,6);
            }
        }

        static void Ring(PolygonSceneryKit kit,Vector3 center,Vector3 across,float radius,Material m,float thickness,int segments=96)
        {
            across.Normalize();
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                kit.Beam(m,center+(across*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a))*radius,
                    center+(across*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b))*radius,thickness,thickness);
            }
        }
        static void BuildCrown(Chunks chunks)
        {
            float d=D(13.6f);var p=RoadPoint(d,-165);p.y=GroundHeight(p.x,p.z);
            var across=new Vector3(.72f,0,-.69f).normalized;var center=p+Vector3.up*105;
            var kit=chunks.At(p);
            // The landmark lot is reserved before the surrounding city is populated.
            occupied.Add(new Bounds(new Vector3(p.x,0,p.z),new Vector3(300,2,240)));
            Ring(kit,center,across,72,steel,5);Ring(kit,center+Vector3.back*.12f,across,73.8f,cyan,1.05f);
            Ring(kit,center+Vector3.back*.18f,across,68.7f,violet,.55f);
            foreach(int side in new[]{-1,1})
            {
                var tower=p+across*side*81;
                Tower(kit,tower,25,31,168,Quaternion.identity,side<0?0:2,true);
                kit.Beam(steel,tower+Vector3.up*135,center+across*side*68,3.5f,3.5f);
            }
            // Small luminous ticks reinforce scale without filling the ring's open center.
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;var direction=across*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);
                kit.Beam(i%3==0?amber:coolWindow,center+direction*75,center+direction*78,.5f,.5f);
            }
        }
        static void BuildCascade(Chunks chunks)
        {
            foreach(float k in new[]{15.3f,17.1f})
            {
                float d=D(k);var p=RoadPoint(d,-62);p.y=GroundHeight(p.x,p.z);
                var rotation=Quaternion.LookRotation(T(d));var kit=chunks.At(p);
                occupied.Add(new Bounds(new Vector3(p.x,0,p.z),new Vector3(75,2,75)));
                kit.Box(wall,p+Vector3.up*47,new Vector3(34,94,30),rotation);
                for(int strip=0;strip<5;strip++)
                {
                    var q=p+rotation*new Vector3(-12+strip*6,48,-15.3f);
                    kit.Box(dataCyan,q,new Vector3(2.2f,82,.18f),rotation);
                    kit.Box(steel,q+rotation*new Vector3(1.4f,0,0),new Vector3(.3f,84,.35f),rotation);
                }
                kit.Box(violet,p+Vector3.up*95,new Vector3(36,.3f,32),rotation);
            }
        }
        static Transform Frame(Transform parent,string name,float d,float lateral=0,float up=0)
        {
            var frame=new GameObject(name).transform;frame.SetParent(parent,false);
            frame.localPosition=RoadPoint(d,lateral,up);frame.localRotation=Quaternion.LookRotation(T(d));return frame;
        }
        static void Label(Transform parent,string name,string text,Vector3 position,float size,Color color)
        {
            var label=PolygonWorld.Label(name,parent,text,position,size,color);
            var shader=Resources.Load<Shader>("PolygonGantryLettering");
            if(shader)
            {
                var r=label.GetComponent<MeshRenderer>();var original=r.sharedMaterial;
                var m=new Material(shader){name="NM Lettering",mainTexture=original.mainTexture,color=Color.white};r.sharedMaterial=m;
            }
        }
        static void BuildStart(Transform parent)
        {
            var gate=Frame(parent,"Prism Boulevard - start gantry",5);var kit=new PolygonSceneryKit("Neon start gantry");
            foreach(int side in new[]{-1,1})
            {
                kit.Box(steel,new Vector3(side*14,6,0),new Vector3(1.4f,12,1.4f));
                kit.Box(cyan,new Vector3(side*13.25f,6,-.74f),new Vector3(.18f,10,.08f));
            }
            kit.Box(black,new Vector3(0,11.5f,0),new Vector3(29,3.3f,1.4f));
            kit.Box(magenta,new Vector3(0,13.2f,-.75f),new Vector3(29,.14f,.08f));
            kit.Emit(gate);
            Label(gate,"Neon Meridian race lettering","NEON MERIDIAN",new Vector3(0,11.9f,-.76f),.16f,new Color(.3f,.9f,1));
            var lights=gate.gameObject.AddComponent<PolygonStartLights>();lights.DarkMaterial=black;
            lights.RedMaterial=Mat("Start red",new Color(1,.02f,.025f),4);
            lights.Lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)
            {
                var lens=PolygonWorld.Box("Start signal "+(i+1),gate,new Vector3((i-2)*1.9f,10.55f,-.8f),new Vector3(1.2f,.6f,.14f),black);
                lights.Lenses[i]=lens.GetComponent<MeshRenderer>();
            }
            // Trackside service/pit frontage; this is scenery, not a new pit-lane branch.
            var pits=Frame(parent,"Prism Boulevard - team garages",D(1.4f),-37);var pk=new PolygonSceneryKit("Neon team garages");
            pk.Box(wall,new Vector3(0,6,0),new Vector3(22,12,128));
            for(int i=0;i<8;i++)
            {
                float z=-55+i*16;pk.Box(glass,new Vector3(11.08f,3.2f,z),new Vector3(.12f,5.2f,12));
                pk.Box(Accent(i),new Vector3(11.2f,6.4f,z),new Vector3(.16f,.22f,12));
                pk.Box(warmWindow,new Vector3(11.12f,9,z),new Vector3(.14f,1.1f,12));
            }
            pk.Box(cyan,new Vector3(0,12.15f,0),new Vector3(23,.20f,130));pk.Emit(pits);
        }

        static void BuildMarket(Transform parent,Chunks chunks)
        {
            for(float k=4.5f;k<9.5f;k+=.85f)
            {
                float d=D(k);var p=RoadPoint(d);var kit=chunks.At(p);
                // High walkways frame the climb. Their bottoms stay at least 11 m above
                // BOTH edges of the road, including cross-fall and grade.
                float ceiling=Mathf.Max(RoadPoint(d,-Barrier).y,RoadPoint(d,Barrier).y)+13;
                var cross=p;cross.y=ceiling;
                var rotation=Quaternion.LookRotation(T(d));
                kit.Box(wall,cross,new Vector3(67,2.2f,6.2f),rotation);
                foreach(int side in new[]{-1,1})
                {
                    var support=p+S(d)*side*29;float g=GroundHeight(support.x,support.z);
                    float h=ceiling-g;support.y=g+h*.5f;
                    kit.Box(steel,support,new Vector3(1.2f,h,3.5f),rotation);
                }
                kit.Beam(magenta,cross-S(d)*33+T(d)*3.2f,cross+S(d)*33+T(d)*3.2f,.18f,.18f);
                kit.Beam(cyan,cross-S(d)*33-T(d)*3.2f,cross+S(d)*33-T(d)*3.2f,.18f,.18f);
                foreach(int side in new[]{-1,1})
                {
                    var stall=RoadPoint(d+23,side*19,.4f);var rot=Quaternion.LookRotation(T(d+23));
                    kit.Box(black,stall+Vector3.up*1.0f,new Vector3(4,2,6),rot);
                    kit.Box(side<0?magenta:violet,stall+Vector3.up*3.7f,new Vector3(5,.3f,7),rot);
                    kit.Box(warmWindow,stall+Vector3.up*2.5f,new Vector3(3.6f,.25f,5.5f),rot);
                }
            }
        }
        static void BuildTunnel(Transform parent,Chunks chunks)
        {
            float start=D(27.5f),finish=D(30.5f);
            for(float d=start;d<finish;d+=4)
            {
                float end=Mathf.Min(d+4,finish);var kit=chunks.At(RoadPoint(d));
                // Geometry hugs the spline rather than placing straight boxes across bends.
                RoadSlab(kit,wall,d,end,-16,-14,0,13.5f);RoadSlab(kit,wall,d,end,14,16,0,13.5f);
                RoadSlab(kit,concrete,d,end,-16,16,12.8f,15.8f);
                foreach(int side in new[]{-1,1})
                {
                    kit.Beam(amber,RoadPoint(d,side*13.8f,10),RoadPoint(end,side*13.8f,10),.18f,.25f);
                    if(((int)(d-start)/4)%6==0)
                        kit.Beam(steel,RoadPoint(d,side*13.8f,1.3f),RoadPoint(d,side*13.8f,12.3f),.45f,.45f);
                }
            }
            for(float d=start+36;d<finish-20;d+=54)
            {
                var frame=Frame(parent,"Undercity - ventilation machinery",d,13.9f,6.7f);
                frame.localRotation*=Quaternion.Euler(0,90,0);
                var kit=new PolygonSceneryKit("Undercity fan housing");
                kit.Box(black,Vector3.zero,new Vector3(6,6,.25f));
                Ring(kit,Vector3.back*.25f,Vector3.right,2.4f,steel,.25f,24);kit.Emit(frame);
                var rotor=new GameObject("Ventilation rotor").transform;rotor.SetParent(frame,false);rotor.localPosition=Vector3.back*.5f;
                var rk=new PolygonSceneryKit("Neon fan blades");
                for(int blade=0;blade<5;blade++)
                {
                    var q=Quaternion.Euler(0,0,blade*72);
                    rk.Box(steel,q*new Vector3(0,1.2f,0),new Vector3(.65f,2.2f,.12f),q);
                }
                rk.Emit(rotor);var motion=rotor.gameObject.AddComponent<PolygonNeonMotion>();
                motion.Mode=PolygonNeonMotion.Motion.Spin;motion.AngularSpeed=38;
            }
            DistrictSign(parent,"Undercity","UNDERCITY",start-35,amber);
            DistrictSign(parent,"Skyline exit","SKYLINE / PRISM",finish+24,cyan);
        }
        static void DistrictSign(Transform parent,string name,string text,float d,Material accent)
        {
            var f=Frame(parent,name+" directional sign",d);var kit=new PolygonSceneryKit(name+" sign");
            kit.Box(black,new Vector3(0,9.8f,0),new Vector3(16,2,.45f));
            kit.Box(accent,new Vector3(0,10.85f,-.3f),new Vector3(16,.13f,.1f));
            foreach(int side in new[]{-1,1})kit.Box(steel,new Vector3(side*14,5,0),new Vector3(.45f,10,.45f));
            kit.Beam(steel,new Vector3(-14,10,0),new Vector3(14,10,0),.5f,.5f);kit.Emit(f);
            Label(f,name+" lettering",text,new Vector3(0,9.8f,-.26f),.12f,accent.color);
        }
        static void BuildFurniture(Transform parent,Chunks chunks)
        {
            for(float d=24;d<PolygonCircuit.Length;d+=38)
            {
                var kit=chunks.At(RoadPoint(d));float curve=PolygonCircuit.Curvature(d+20);
                foreach(int side in new[]{-1,1})
                {
                    var p=RoadPoint(d,side*14.5f);var q=Quaternion.LookRotation(T(d));
                    kit.Box(steel,p+Vector3.up*3.5f,new Vector3(.16f,7,.16f),q);
                    kit.Beam(steel,p+Vector3.up*7,p-S(d)*side*2+Vector3.up*7,.16f,.16f);
                    kit.Box(coolWindow,p-S(d)*side*1.7f+Vector3.up*6.85f,new Vector3(1.6f,.12f,.45f),q);
                    if(Mathf.Abs(curve)>.005f&&side==(curve>0?-1:1))
                    {
                        var f=Quaternion.LookRotation(T(d));var center=RoadPoint(d,side*(Barrier+.1f),2.1f);
                        kit.Box(black,center,new Vector3(3.6f,1.6f,.15f),f);
                        var right=f*Vector3.right;var up=Vector3.up;float turn=curve>0?1:-1;
                        kit.Beam(amber,center-right*turn*.6f+up*.5f-T(d)*.1f,center+right*turn*.3f-T(d)*.1f,.13f,.13f);
                        kit.Beam(amber,center+right*turn*.3f-T(d)*.1f,center-right*turn*.6f-up*.5f-T(d)*.1f,.13f,.13f);
                    }
                }
            }
            DistrictSign(parent,"Lantern Rise","LANTERN RISE",D(4),magenta);
            DistrictSign(parent,"The Crown","THE CROWN",D(10.4f),cyan);
            DistrictSign(parent,"Cascade Descent","CASCADE",D(15.2f),violet);
        }
        static void BuildTransit(Transform parent,Chunks chunks)
        {
            float d=D(7.4f);var p=RoadPoint(d);var axis=(S(d)+T(d)*.25f).normalized;
            float height=p.y+29;var a=p-axis*200;var b=p+axis*200;a.y=b.y=height;
            var kit=chunks.At(p);
            kit.Beam(concrete,a,b,5,1.3f);kit.Beam(cyan,a+Vector3.up*.85f,b+Vector3.up*.85f,.2f,.2f);
            for(int i=0;i<7;i++)
            {
                var support=Vector3.Lerp(a,b,i/6f);PolygonCircuit.ProjectLapFlat(support,out float offset);
                if(offset<19)continue;float floor=GroundHeight(support.x,support.z);
                kit.Box(steel,new Vector3(support.x,(floor+height)*.5f,support.z),new Vector3(2,height-floor,2));
            }
            var train=new GameObject("Lantern Rise - elevated express train").transform;train.SetParent(parent,false);
            var rotation=Quaternion.LookRotation(axis);train.localPosition=a+Vector3.up*2.5f;train.localRotation=rotation;
            var tk=new PolygonSceneryKit("Neon express train");
            for(int carriage=0;carriage<3;carriage++)
            {
                float z=-carriage*13;tk.Box(wall,new Vector3(0,1,z),new Vector3(3.2f,2.7f,12));
                tk.Box(cyan,new Vector3(0,2.4f,z),new Vector3(3.3f,.14f,12));
                foreach(int side in new[]{-1,1})tk.Box(warmWindow,new Vector3(side*1.64f,1.3f,z),new Vector3(.10f,.9f,10.5f));
            }
            tk.Emit(train);var motion=train.gameObject.AddComponent<PolygonNeonMotion>();
            motion.Mode=PolygonNeonMotion.Motion.Shuttle;motion.StartPoint=a+Vector3.up*2.5f;motion.EndPoint=b+Vector3.up*2.5f;
            motion.Speed=19;motion.Phase=.28f;
        }

        public static Transform Build(Transform root)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.NeonMeridian)
                throw new InvalidOperationException("Select Neon Meridian before generating its city.");
            Palette();PrepareTerrain();occupied.Clear();BuildingCount=0;
            var parent=new GameObject(SceneryName).transform;parent.SetParent(root,false);terrainParent=parent;
            var chunks=new Chunks();
            // Reserve the garage block and skyline landmark sites before populating ordinary lots.
            var pits=RoadPoint(D(1.4f),-37);occupied.Add(new Bounds(new Vector3(pits.x,0,pits.z),new Vector3(145,2,70)));
            BuildRoad(chunks);BuildTerrain(chunks);BuildCrown(chunks);BuildCascade(chunks);
            BuildCity(chunks);BuildStart(parent);BuildMarket(parent,chunks);BuildTunnel(parent,chunks);
            BuildFurniture(parent,chunks);BuildTransit(parent,chunks);chunks.Emit(parent);
            Debug.Log($"Neon Meridian city: {BuildingCount} buildings. Opaque, chunked scenery; no extra real-time local lights.");
            occupied.Clear();return parent;
        }
    }
}
