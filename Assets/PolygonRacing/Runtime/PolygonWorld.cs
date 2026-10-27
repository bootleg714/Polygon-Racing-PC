using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing
{
    // Builds editable GameObjects in the editor. All dimensions are metres.
    public static class PolygonWorld
    {
        public const float RadiusX = 150f, RadiusZ = 100f;
        public static float RoadHalfWidth=>PolygonCircuit.RoadHalfWidth;
        public static Vector3 Point(float t) => PolygonCircuit.Evaluate(t);
        public static Vector3 Tangent(float t) => PolygonCircuit.Tangent(t);
        public static Vector3 Side(float t) => Vector3.Cross(Vector3.up, Tangent(t));
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        public static Material Paint(string name, Color color)
        {
            if (palette.TryGetValue(name, out var old) && old) return old;
            var shader = PolygonLook.LitShader();
            var mat = new Material(shader) { name = "PR_" + name, color = color };
            mat.SetFloat("_Smoothness", 0.08f);
            palette[name] = mat;
            return mat;
        }
        public static GameObject Box(string name, Transform parent, Vector3 p, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        public static GameObject Surface(string name, Transform parent, List<Vector3> v, List<int> triangles, Material mat)
        {
            var mesh = new Mesh { name = name + "Mesh" };
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(parent, false);
            g.GetComponent<MeshFilter>().sharedMesh = mesh; g.GetComponent<Renderer>().sharedMaterial = mat; return g;
        }
        static void Quad(List<Vector3> v, List<int> tri, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = v.Count; v.AddRange(new[] { a,b,c,d });
            if (Vector3.Cross(b-a,c-a).y >= 0) tri.AddRange(new[] { n,n+1,n+2,n,n+2,n+3 });
            else tri.AddRange(new[] { n,n+2,n+1,n,n+3,n+2 });
        }
        static void Strip(Transform parent, string name, float lo, float hi, float height, Material mat,int segments=256)
        {
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments, b=(i+1)*Mathf.PI*2/segments;
                Quad(v,tri,Point(a)+Side(a)*lo+Vector3.up*height,Point(a)+Side(a)*hi+Vector3.up*height,
                    Point(b)+Side(b)*hi+Vector3.up*height,Point(b)+Side(b)*lo+Vector3.up*height);
            }
            Surface(name,parent,v,tri,mat);
        }
        static Transform TrackFrame(Transform parent,string name,float distance)
        {
            float angle=PolygonCircuit.AngleAtDistance(distance);
            var frame=new GameObject(name).transform;frame.SetParent(parent,false);
            frame.position=Point(angle);frame.rotation=Quaternion.LookRotation(Tangent(angle));return frame;
        }
        static void CoastalSign(Transform parent,string text,float distance,float lateral,Material panel,Material ink)
        {
            var sign=TrackFrame(parent,"Expressway sign - "+text,distance);
            sign.position+=Side(PolygonCircuit.AngleAtDistance(distance))*lateral;
            Box("Sign post",sign,new Vector3(0,1.25f,0),new Vector3(.18f,2.5f,.18f),ink);
            Box("Sign panel",sign,new Vector3(0,3.2f,0),new Vector3(4.8f,1.5f,.18f),panel);
            Label("Sign lettering",sign,text,new Vector3(0,3.2f,-.105f),.075f,ink.color);
        }
        static Mesh CoastalPalmTrunkMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int sides=7;
            float[] heights={0,3.8f,7.4f};
            float[] radii={.34f,.27f,.18f};
            for(int ring=0;ring<heights.Length;ring++)
                for(int side=0;side<sides;side++)
                {
                    float angle=side*Mathf.PI*2/sides;
                    float bend=ring*.12f;
                    vertices.Add(new Vector3(Mathf.Cos(angle)*radii[ring]+bend,heights[ring],Mathf.Sin(angle)*radii[ring]));
                }
            for(int ring=0;ring<heights.Length-1;ring++)
                for(int side=0;side<sides;side++)
                {
                    int a=ring*sides+side,b=ring*sides+(side+1)%sides;
                    int c=(ring+1)*sides+(side+1)%sides,d=(ring+1)*sides+side;
                    triangles.AddRange(new[]{a,c,b,a,d,c});
                }
            var mesh=new Mesh{name="Low-poly palm trunk mesh"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }
        static Mesh CoastalPalmCrownMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int fronds=9;
            for(int i=0;i<fronds;i++)
            {
                float angle=i*Mathf.PI*2/fronds;
                var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var across=new Vector3(-direction.z,0,direction.x);
                Vector3 start=Vector3.zero;
                Vector3 middle=direction*2.05f+Vector3.up*.42f;
                Vector3 tip=direction*4.2f+Vector3.down*.58f;
                int n=vertices.Count;
                vertices.Add(start-across*.12f);vertices.Add(start+across*.12f);
                vertices.Add(middle+across*.34f);vertices.Add(middle-across*.34f);
                vertices.Add(tip);
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+3,n+2,n+4});
                triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2,n+3,n+4,n+2});
            }
            var mesh=new Mesh{name="Low-poly palm crown mesh"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }
        static Mesh CoastalRockMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int sides=7;
            float[] radii={.72f,1,.57f};
            float[] heights={-.48f,-.08f,.4f};
            for(int ring=0;ring<radii.Length;ring++)
                for(int side=0;side<sides;side++)
                {
                    float angle=side*Mathf.PI*2/sides+(ring%2)*.12f;
                    float variation=1+Mathf.Sin(side*12.71f+ring*5.3f)*.13f;
                    vertices.Add(new Vector3(Mathf.Cos(angle)*radii[ring]*variation,heights[ring],Mathf.Sin(angle)*radii[ring]*variation));
                }
            int bottomCenter=vertices.Count;vertices.Add(new Vector3(0,heights[0],0));
            int topCenter=vertices.Count;vertices.Add(new Vector3(0,heights[2],0));
            for(int ring=0;ring<radii.Length-1;ring++)
                for(int side=0;side<sides;side++)
                {
                    int a=ring*sides+side,b=ring*sides+(side+1)%sides;
                    int c=(ring+1)*sides+(side+1)%sides,d=(ring+1)*sides+side;
                    triangles.AddRange(new[]{a,c,b,a,d,c});
                }
            for(int side=0;side<sides;side++)
            {
                triangles.AddRange(new[]{bottomCenter,side,(side+1)%sides});
                triangles.AddRange(new[]{topCenter,sides*2+(side+1)%sides,sides*2+side});
            }
            var mesh=new Mesh{name="Low-poly coastal rock mesh"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }
        static void CoastalMeshObject(string name,Transform parent,Vector3 position,Vector3 scale,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void CoastalPalm(Transform parent,Vector3 position,float scale,Mesh trunkMesh,Mesh crownMesh,Material trunk,Material leaves)
        {
            var palm=new GameObject("Low-poly coastal palm").transform;
            palm.SetParent(parent,false);palm.localPosition=position;palm.localScale=Vector3.one*scale;
            CoastalMeshObject("Palm trunk",palm,Vector3.zero,Vector3.one,trunkMesh,trunk);
            CoastalMeshObject("Palm fronds",palm,new Vector3(.3f,7.25f,0),Vector3.one,crownMesh,leaves);
        }
        static void CoastalRock(Transform parent,Vector3 position,Vector3 scale,Mesh mesh,Material material)
        {
            CoastalMeshObject("Low-poly coastal rock",parent,position,scale,mesh,material);
        }
        public static Transform BuildCoastalScenery(Transform parent)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)
                throw new System.InvalidOperationException("Select Coastal Expressway before building its scenery.");

            palette.Clear();
            var world=new GameObject("Circuit - editable scenery");world.transform.SetParent(parent,false);
            var land=Paint("Coastal grass",new Color(.32f,.49f,.34f));
            var asphalt=Paint("Expressway asphalt",new Color(.16f,.2f,.24f));
            var shoulder=Paint("Coastal runoff",new Color(.68f,.61f,.45f));
            var edge=Paint("Expressway white",new Color(.9f,.93f,.88f));
            var barrier=Paint("Coastal barrier",new Color(.79f,.82f,.8f));
            var concrete=Paint("Bridge concrete",new Color(.56f,.6f,.62f));
            var ocean=Paint("Coastal ocean",new Color(.025f,.31f,.52f));
            var beach=Paint("Coastal sand",new Color(.79f,.7f,.49f));
            var wetSand=Paint("Wet coastal sand",new Color(.58f,.57f,.45f));
            var palmTrunk=Paint("Palm bark",new Color(.37f,.23f,.13f));
            var palmLeaves=Paint("Palm leaves",new Color(.12f,.38f,.24f));
            var palmLeavesLight=Paint("Palm leaves light",new Color(.24f,.53f,.3f));
            var rock=Paint("Coastal rock",new Color(.39f,.38f,.34f));
            var island=Paint("Distant island",new Color(.22f,.39f,.32f));
            var standSeat=Paint("Spectator stand",new Color(.1f,.3f,.46f));
            var standAccent=Paint("Spectator stand accent",new Color(.91f,.32f,.12f));
            var sign=Paint("Expressway sign",new Color(.055f,.27f,.25f));
            var amber=Paint("Expressway marker",new Color(1,.56f,.08f));

            // Leave a water cutout beneath the bridge and bring the shoreline close to
            // the long coastal straight so the sea reads from the driver's seat.
            const float landMinX=-920,landMaxX=1580,landMinZ=-570,landMaxZ=710;
            var bridgeCenter=Point(PolygonCircuit.AngleAtDistance(PolygonCircuit.DistanceAtKnot(2)));
            float channelHalf=105;
            float leftWidth=bridgeCenter.x-channelHalf-landMinX;
            float rightWidth=landMaxX-(bridgeCenter.x+channelHalf);
            Box("Coastal land west",world.transform,
                new Vector3((landMinX+bridgeCenter.x-channelHalf)*.5f,-.58f,(landMinZ+landMaxZ)*.5f),
                new Vector3(leftWidth,1.1f,landMaxZ-landMinZ),land);
            Box("Coastal land east",world.transform,
                new Vector3((bridgeCenter.x+channelHalf+landMaxX)*.5f,-.58f,(landMinZ+landMaxZ)*.5f),
                new Vector3(rightWidth,1.1f,landMaxZ-landMinZ),land);
            Box("Coastal land north of bridge",world.transform,
                new Vector3(bridgeCenter.x,-.58f,(channelHalf+landMaxZ)*.5f),
                new Vector3(channelHalf*2,1.1f,landMaxZ-channelHalf),land);
            Box("Coastal land south of bridge",world.transform,
                new Vector3(bridgeCenter.x,-.58f,(landMinZ-channelHalf)*.5f),
                new Vector3(channelHalf*2,1.1f,-channelHalf-landMinZ),land);
            Box("Open Atlantic",world.transform,new Vector3(300,-.26f,3800),new Vector3(6000,.18f,6104),ocean);
            Box("Wide sandy shoreline",world.transform,new Vector3(330,-.055f,724),new Vector3(2500,.07f,56),beach);
            Box("Wet sand at waterline",world.transform,new Vector3(330,-.17f,748),new Vector3(2500,.025f,18),wetSand);

            float road=RoadHalfWidth;
            Strip(world.transform,"High-speed road",-(road),road,.025f,asphalt,PolygonCircuit.SampleCount);
            Strip(world.transform,"Left road edge",-road+.3f,-road+.58f,.04f,edge,PolygonCircuit.SampleCount);
            Strip(world.transform,"Right road edge",road-.58f,road-.3f,.04f,edge,PolygonCircuit.SampleCount);
            Strip(world.transform,"Left paved shoulder",-road-5,-road-.65f,.012f,shoulder,PolygonCircuit.SampleCount);
            Strip(world.transform,"Right paved shoulder",road+.65f,road+5,.012f,shoulder,PolygonCircuit.SampleCount);

            for(float distance=18;distance<PolygonCircuit.Length;distance+=38)
            {
                var frame=TrackFrame(world.transform,"Highway lane marker",distance);
                Box("Center lane dash",frame,new Vector3(0,.052f,0),new Vector3(.14f,.025f,10),edge);
            }

            float bridgeDistance=PolygonCircuit.DistanceAtKnot(2);
            var channelCenter=Point(PolygonCircuit.AngleAtDistance(bridgeDistance));
            var channel=TrackFrame(world.transform,"Blue water beneath and beside bridge",bridgeDistance);
            Box("Bridge channel water",channel,new Vector3(0,-.36f,0),new Vector3(155,.12f,190),ocean);
            var bridge=TrackFrame(world.transform,"Wide flat estuary bridge",bridgeDistance);
            Box("Bridge deck",bridge,new Vector3(0,-.23f,0),new Vector3(37,.43f,136),concrete);
            foreach(int side in new[]{-1,1})
            {
                Box("Bridge guardrail",bridge,new Vector3(side*18.1f,.72f,0),new Vector3(.3f,1.35f,140),barrier);
                for(int segment=-2;segment<=2;segment++)
                    Box("Bridge pier",bridge,new Vector3(side*16.3f,-2.3f,segment*25),new Vector3(1.3f,4.5f,1.3f),concrete);
            }

            float tunnelDistance=PolygonCircuit.DistanceAtKnot(7);
            var tunnel=TrackFrame(world.transform,"Short expressway tunnel",tunnelDistance);
            Box("Tunnel left wall",tunnel,new Vector3(-14.2f,2.7f,0),new Vector3(1.2f,5.4f,68),concrete);
            Box("Tunnel right wall",tunnel,new Vector3(14.2f,2.7f,0),new Vector3(1.2f,5.4f,68),concrete);
            Box("Tunnel roof",tunnel,new Vector3(0,5.5f,0),new Vector3(29,.8f,68),concrete);
            foreach(int end in new[]{-1,1})
                Box("Tunnel portal marker",tunnel,new Vector3(0,3.1f,end*34),new Vector3(29,.3f,.4f),amber);

            var trunkMesh=CoastalPalmTrunkMesh();
            var crownMesh=CoastalPalmCrownMesh();
            var palmLocations=new[]{
                new Vector3(-650,0,695),new Vector3(-400,0,705),new Vector3(-150,0,710),
                new Vector3(120,0,710),new Vector3(390,0,710),new Vector3(650,0,710),
                new Vector3(920,0,710),new Vector3(1200,0,700),
                new Vector3(-520,0,360),new Vector3(1320,0,440),new Vector3(-560,0,-260),
                new Vector3(1380,0,-250)
            };
            var palmRandom=new System.Random(431);
            for(int i=0;i<palmLocations.Length;i++)
            {
                var position=palmLocations[i];
                PolygonCircuit.ProjectLap(position,out float clearance);
                if(clearance<43)continue;
                CoastalPalm(world.transform,position,.88f+(float)palmRandom.NextDouble()*.27f,
                    trunkMesh,crownMesh,palmTrunk,i%2==0?palmLeaves:palmLeavesLight);
            }

            for(int i=0;i<4;i++)
            {
                float x=-520+i*560;
                float z=1450+(i%2)*330;
                var silhouette=new GameObject("Distant low-poly island").transform;
                silhouette.SetParent(world.transform,false);silhouette.localPosition=new Vector3(x,-.15f,z);
                float height=78+(i%3)*18;
                Faceted("Island silhouette",silhouette,Vector3.zero,new[]{-95f,0f,95f},
                    new[]{55f,175f,70f},new[]{0f,0f,0f},
                    new[]{height*.62f,height,height*.7f},island);
            }

            var rockMesh=CoastalRockMesh();
            var rockLocations=new[]{
                new Vector3(-760,0,680),new Vector3(-570,0,700),new Vector3(-330,0,720),
                new Vector3(-65,0,700),new Vector3(225,0,720),new Vector3(490,0,700),
                new Vector3(760,0,720),new Vector3(1040,0,700),new Vector3(1320,0,680),
                new Vector3(-590,0,500),new Vector3(1370,0,180),new Vector3(-560,0,-390)
            };
            var rockRandom=new System.Random(271);
            foreach(var position in rockLocations)
            {
                PolygonCircuit.ProjectLap(position,out float clearance);
                if(clearance<39)continue;
                float width=2.5f+(float)rockRandom.NextDouble()*3.5f;
                CoastalRock(world.transform,position+Vector3.up*width*.35f,
                    new Vector3(width,width*.8f,width*.82f),rockMesh,rock);
            }

            var grandstandMaterial=standSeat;
            foreach(int side in new[]{-1,1})
            {
                var stand=TrackFrame(world.transform,"Start finish spectator stand",0);
                stand.position+=Side(0)*(side*40);
                Box("Stand foundation",stand,new Vector3(0,.45f,0),new Vector3(13,.9f,54),concrete);
                for(int tier=0;tier<3;tier++)
                    Box("Blue spectator seating",stand,
                        new Vector3(-side*(4-tier*4),1.25f+tier*1.15f,0),
                        new Vector3(4.2f,.7f,50),tier==1?standAccent:grandstandMaterial);
                foreach(int end in new[]{-1,1})
                {
                    Box("Stand support",stand,new Vector3(-side*5,3.4f,end*24),new Vector3(.7f,5.4f,.7f),concrete);
                    Box("Stand support",stand,new Vector3(side*5,3.4f,end*24),new Vector3(.7f,5.4f,.7f),concrete);
                }
                Box("Stand roof",stand,new Vector3(0,6.3f,0),new Vector3(14,.5f,56),concrete);
            }

            foreach(float corner in new[]{PolygonCircuit.DistanceAtKnot(19),PolygonCircuit.DistanceAtKnot(22)})
                foreach(int meters in new[]{150,100,50})
                    CoastalSign(world.transform,meters.ToString(),corner-meters,-14,sign,edge);
            CoastalSign(world.transform,"COAST",PolygonCircuit.DistanceAtKnot(12),17,sign,edge);
            CoastalSign(world.transform,"TECHNICAL\nSECTION",PolygonCircuit.DistanceAtKnot(20),-15,sign,edge);

            for(float distance=0;distance<PolygonCircuit.Length;distance+=44)
            {
                float angle=PolygonCircuit.AngleAtDistance(distance);
                var tangent=Tangent(angle);var side=Side(angle);
                foreach(int direction in new[]{-1,1})
                {
                    var post=Box("Roadside delineator",world.transform,
                        Point(angle)+side*(direction*(road+6.6f))+Vector3.up*.58f,
                        new Vector3(.22f,1.16f,.24f),distance%88<44?edge:amber);
                    post.transform.rotation=Quaternion.LookRotation(tangent);
                    var rail=Box("Low roadside barrier",world.transform,
                        Point(angle)+side*(direction*18.4f)+Vector3.up*.48f,
                        new Vector3(.32f,.96f,Mathf.Min(42,PolygonCircuit.Length-distance)),barrier);
                    rail.transform.rotation=Quaternion.LookRotation(tangent);
                }
            }

            var start=TrackFrame(world.transform,"Expressway start gantry",0);
            Box("Gantry left upright",start,new Vector3(-13,3,0),new Vector3(.6f,6,.6f),barrier);
            Box("Gantry right upright",start,new Vector3(13,3,0),new Vector3(.6f,6,.6f),barrier);
            Box("Gantry beam",start,new Vector3(0,6,0),new Vector3(26,1.2f,.6f),sign);
            Label("Expressway race title",start,"COASTAL  EXPRESSWAY",new Vector3(0,6,-.34f),.14f,Color.white);
            PolygonCoastalArt.Build(world.transform);
            return world.transform;
        }
        public static TextMesh Label(string name, Transform parent, string value, Vector3 p, float size, Color color)
        {
            var g=new GameObject(name,typeof(TextMesh)); g.transform.SetParent(parent,false);g.transform.localPosition=p;
            var t=g.GetComponent<TextMesh>();t.text=value;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
            t.GetComponent<Renderer>().sharedMaterial=t.font.material;
            return t;
        }
        static GameObject Faceted(string name, Transform parent, Vector3 position, float[] z, float[] widths, float[] bottoms, float[] tops, Material mat)
        {
            var v=new List<Vector3>();var tr=new List<int>();
            // Independent vertices on each face retain hard edges.
            System.Action<Vector3,Vector3,Vector3,Vector3> face=(a,b,c,d)=>{
                int n=v.Count;v.AddRange(new[]{a,b,c,d});tr.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
            for(int i=0;i<z.Length-1;i++)
            {
                var a=new Vector3(-widths[i],bottoms[i],z[i]);var b=new Vector3(widths[i],bottoms[i],z[i]);
                var c=new Vector3(widths[i],tops[i],z[i]);var d=new Vector3(-widths[i],tops[i],z[i]);
                var e=new Vector3(-widths[i+1],bottoms[i+1],z[i+1]);var f=new Vector3(widths[i+1],bottoms[i+1],z[i+1]);
                var h=new Vector3(-widths[i+1],tops[i+1],z[i+1]);var k=new Vector3(widths[i+1],tops[i+1],z[i+1]);
                face(d,h,k,c);face(a,b,f,e);face(a,e,h,d);face(b,c,k,f);
                if(i==0)face(a,d,c,b);if(i==z.Length-2)face(e,f,k,h);
            }
            var g=Surface(name,parent,v,tr,mat);g.transform.localPosition=position;return g;
        }
        static GameObject Cylinder(string name, Transform parent, Vector3 p, float radius, float depth, Material mat)
        {
            var v=new List<Vector3>();var tr=new List<int>();
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;int n=v.Count;
                v.AddRange(new[]{new Vector3(-depth/2,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),new Vector3(depth/2,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),new Vector3(depth/2,Mathf.Cos(b)*radius,Mathf.Sin(b)*radius),new Vector3(-depth/2,Mathf.Cos(b)*radius,Mathf.Sin(b)*radius)});
                tr.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                for(int side=-1;side<=1;side+=2){n=v.Count;v.AddRange(new[]{new Vector3(side*depth/2,0,0),new Vector3(side*depth/2,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),new Vector3(side*depth/2,Mathf.Cos(b)*radius,Mathf.Sin(b)*radius)});if(side==1)tr.AddRange(new[]{n,n+1,n+2});else tr.AddRange(new[]{n,n+2,n+1});}
            }
            var g=Surface(name,parent,v,tr,mat);g.transform.localPosition=p;return g;
        }
        public static GameObject Build()
        {
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Forest);
            palette.Clear();
            var root=new GameObject("Polygon Racing VR - Prototype 01");
            root.AddComponent<PolygonCircuitSelector>();
            var red=Paint("Vermilion",new Color(.9f,.055f,.045f));var white=Paint("Ivory",new Color(.94f,.95f,.9f));
            var dark=Paint("Charcoal",new Color(.055f,.07f,.09f));var metal=Paint("Metal",new Color(.3f,.35f,.4f));
            var yellow=Paint("Yellow",new Color(1,.78f,.05f));var green=Paint("Grass",new Color(.27f,.48f,.3f));
            var asphalt=Paint("Asphalt",new Color(.17f,.21f,.25f));var gravel=Paint("Runoff",new Color(.65f,.56f,.39f));
            var world=new GameObject("Circuit - editable scenery").transform;world.SetParent(root.transform,false);
            Box("Ground",world,new Vector3(0,-.3f,0),new Vector3(1800,.5f,1800),green);
            Strip(world,"Gravel shoulders",-15,15,0,gravel);Strip(world,"Road",-RoadHalfWidth,RoadHalfWidth,.025f,asphalt);
            Strip(world,"Left edge line",-8.7f,-8.45f,.032f,white);Strip(world,"Right edge line",8.45f,8.7f,.032f,white);
            for(int i=0;i<128;i++)
            {
                float t=(i+.5f)*Mathf.PI*2/128;var dir=Tangent(t);
                float sectionLength=Vector3.Distance(Point(i*Mathf.PI*2/128),Point((i+1)*Mathf.PI*2/128));
                foreach(int side in new[]{-1,1})
                {
                    var curb=Box("Alternating curb",world,Point(t)+Side(t)*(9.35f*side)+Vector3.up*.045f,new Vector3(.7f,.09f,sectionLength*1.03f),i%2==0?red:white);
                    curb.transform.rotation=Quaternion.LookRotation(dir);
                    if(i%2==0){var wall=Box("Guardrail",world,Point(t)+Side(t)*(16.5f*side)+Vector3.up*.6f,new Vector3(.35f,1.2f,sectionLength*2.06f),white);wall.transform.rotation=Quaternion.LookRotation(dir);}
                }
            }
            for(int i=0;i<18;i++)for(int j=0;j<3;j++)
            {
                var p=Point(0)+Side(0)*(-8.5f+i)+Tangent(0)*(j*.8f);
                var b=Box("Start finish checks",world,p+Vector3.up*.04f,new Vector3(1,.018f,.8f),(i+j)%2==0?white:dark);b.transform.rotation=Quaternion.LookRotation(Tangent(0));
            }
            var banner=new GameObject("Start gantry").transform;banner.SetParent(world,false);banner.position=Point(0);banner.rotation=Quaternion.LookRotation(Tangent(0));
            Box("Left post",banner,new Vector3(-11,3,0),new Vector3(.6f,6,.6f),metal);Box("Right post",banner,new Vector3(11,3,0),new Vector3(.6f,6,.6f),metal);
            Box("Header",banner,new Vector3(0,6,0),new Vector3(22,1.5f,.5f),dark);
            Label("Race title",banner,"POLYGON  RACING  VR",new Vector3(0,6,-.3f),.18f,Color.white);
            var rng=new System.Random(714);
            var leaf1=Paint("Pine",new Color(.14f,.34f,.22f));var leaf2=Paint("Pine light",new Color(.25f,.43f,.25f));var trunk=Paint("Bark",new Color(.29f,.2f,.12f));
            for(int i=0;i<160;i++)
            {
                float t=(float)rng.NextDouble()*Mathf.PI*2;float side=i%2==0?1:-1;float offset=side*(24+(float)rng.NextDouble()*80);
                var location=Point(t)+Side(t)*offset;PolygonCircuit.ProjectLap(location,out float trackClearance);
                if(trackClearance<23)continue;
                var tree=new GameObject("Polygon pine").transform;tree.SetParent(world,false);tree.position=location;
                float h=5+(float)rng.NextDouble()*8; Box("Trunk",tree,new Vector3(0,h*.15f,0),new Vector3(.55f,h*.3f,.55f),trunk);
                var v=new List<Vector3>();var tr=new List<int>();int count=5;
                for(int k=0;k<count;k++){float a=k*Mathf.PI*2/count,b=(k+1)*Mathf.PI*2/count;int n=v.Count;v.AddRange(new[]{new Vector3(Mathf.Sin(a)*h*.28f,h*.22f,Mathf.Cos(a)*h*.28f),Vector3.up*h,new Vector3(Mathf.Sin(b)*h*.28f,h*.22f,Mathf.Cos(b)*h*.28f)});tr.AddRange(new[]{n,n+2,n+1});}
                Surface("Crown",tree,v,tr,i%2==0?leaf1:leaf2);
            }
            for(int i=0;i<15;i++)
            {
                float t=i*Mathf.PI*2/15;var hill=new GameObject("Distant faceted hill").transform;hill.SetParent(world,false);hill.position=new Vector3(Mathf.Sin(t)*740,-3,Mathf.Cos(t)*740);
                var v=new List<Vector3>();var tr=new List<int>();for(int k=0;k<6;k++){float a=k*Mathf.PI/3,b=(k+1)*Mathf.PI/3;int n=v.Count;v.AddRange(new[]{new Vector3(Mathf.Sin(a)*90,0,Mathf.Cos(a)*90),new Vector3(12,60+i%4*18,0),new Vector3(Mathf.Sin(b)*90,0,Mathf.Cos(b)*90)});tr.AddRange(new[]{n,n+2,n+1});}Surface("Hill mesh",hill,v,tr,leaf2);
            }
            // Pit buildings stay well clear of the racing surface.
            for(int i=0;i<6;i++){var p=new Vector3(-60+i*20,0,155);Box("Pit garage",world,p+Vector3.up*3,new Vector3(17,6,12),white);Box("Garage door",world,p+new Vector3(0,2,-6.1f),new Vector3(12,4,.1f),dark);Box("Garage trim",world,p+new Vector3(0,5,-6.2f),new Vector3(17,.8f,.15f),i%2==0?red:yellow);}
            var car=new GameObject("Player car - editable polygon model");car.transform.SetParent(root.transform,false);car.transform.position=Point(0);car.transform.rotation=Quaternion.LookRotation(Tangent(0));
            var drive=car.AddComponent<PolygonDriver>();
            Faceted("Wedge nose",car.transform,Vector3.zero,new[]{.35f,1.35f,2.6f},new[]{.4f,.31f,.18f},new[]{.2f,.2f,.21f},new[]{.7f,.48f,.3f},red);
            Box("Nose stripe",car.transform,new Vector3(0,.49f,1.35f),new Vector3(.13f,.025f,1.3f),white).transform.localRotation=Quaternion.Euler(10,0,0);
            Box("Chassis",car.transform,new Vector3(0,.24f,-.4f),new Vector3(1.1f,.22f,3.1f),dark);
            foreach(int side in new[]{-1,1})
            {
                Faceted("Side pod",car.transform,new Vector3(side*.73f,0,0),new[]{-1.75f,-.6f,.7f},new[]{.28f,.32f,.23f},new[]{.25f,.22f,.23f},new[]{.67f,.62f,.43f},red);
                Box("White side stripe",car.transform,new Vector3(side*.73f,.64f,-.65f),new Vector3(.18f,.035f,1.65f),white);
                Box("Cockpit rim",car.transform,new Vector3(side*.4f,.69f,-.48f),new Vector3(.12f,.13f,1.65f),dark);
            }
            Box("Seat back",car.transform,new Vector3(0,.65f,-1.13f),new Vector3(.65f,.75f,.16f),dark);
            Box("Seat cushion",car.transform,new Vector3(0,.39f,-.65f),new Vector3(.65f,.12f,.65f),dark);
            Box("Engine cover",car.transform,new Vector3(0,.62f,-1.65f),new Vector3(.78f,.58f,.75f),red);
            Box("Front wing",car.transform,new Vector3(0,.24f,2.3f),new Vector3(2.8f,.12f,.45f),white);
            Box("Rear wing",car.transform,new Vector3(0,1.03f,-2.06f),new Vector3(2.5f,.12f,.55f),red);
            foreach(int side in new[]{-1,1}){Box("Rear wing support",car.transform,new Vector3(side*.45f,.65f,-2),new Vector3(.07f,.7f,.12f),metal);Box("Wing endplate",car.transform,new Vector3(side*1.24f,1.08f,-2.06f),new Vector3(.07f,.36f,.7f),white);}
            drive.frontSteering=new Transform[2];drive.wheels=new Transform[4];int wi=0;
            foreach(float z in new[]{1.4f,-1.45f})foreach(int side in new[]{-1,1})
            {
                var pivot=new GameObject(z>0?"Front steering pivot":"Rear wheel pivot").transform;pivot.SetParent(car.transform,false);pivot.localPosition=new Vector3(side*1.13f,.39f,z);
                drive.wheels[wi]=Cylinder("12-sided tire",pivot,Vector3.zero,.39f,.4f,dark).transform;
                Cylinder("Wheel hub",drive.wheels[wi],new Vector3(side*.205f,0,0),.21f,.018f,metal);
                if(z>0)drive.frontSteering[wi]=pivot;
                Box("Suspension arm",car.transform,new Vector3(side*.73f,.35f,z),new Vector3(.8f,.055f,.075f),metal);wi++;
            }
            var dash=Box("Dashboard",car.transform,new Vector3(0,.8f,.43f),new Vector3(.78f,.34f,.1f),dark);
            drive.dashboard=Label("Live instruments",car.transform,"0 MPH\nLAP 1   00:00.0",new Vector3(0,.81f,.365f),.031f,new Color(.4f,1,.7f));
            var wheel=new GameObject("Steering wheel").transform;wheel.SetParent(car.transform,false);wheel.localPosition=new Vector3(0,.68f,.03f);drive.steeringWheel=wheel;
            Box("Wheel top",wheel,new Vector3(0,.13f,0),new Vector3(.43f,.055f,.065f),dark);Box("Wheel lower",wheel,new Vector3(0,-.13f,0),new Vector3(.43f,.055f,.065f),dark);
            foreach(int side in new[]{-1,1})Box("Wheel grip",wheel,new Vector3(side*.21f,0,0),new Vector3(.065f,.3f,.075f),dark);
            Box("Wheel hub",wheel,Vector3.zero,new Vector3(.16f,.11f,.09f),metal);Box("Wheel center stripe",wheel,new Vector3(0,.13f,-.04f),new Vector3(.045f,.06f,.01f),yellow);
            var tracking=new GameObject("Seated tracking space").transform;tracking.SetParent(car.transform,false);tracking.localPosition=PolygonDriver.Seat;
            var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cam.tag="MainCamera";cam.transform.SetParent(tracking,false);
            var camera=cam.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=850;camera.fieldOfView=78;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.46f,.7f,.84f);
            drive.view=camera;drive.trackingSpace=tracking;
            var light=new GameObject("Sun",typeof(Light));light.transform.SetParent(root.transform,false);light.transform.rotation=Quaternion.Euler(48,-35,0);var sun=light.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.None;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.68f,.73f,.79f);RenderSettings.fog=true;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=230;RenderSettings.fogEndDistance=780;
            return root;
        }
    }
}
