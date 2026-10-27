using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Downtown street circuit scenery ("POLYGON CITY"). Editor-time generation only: no update
    // loops, colliders or extra lights. Geometry is baked per material into spatial chunks, so
    // frustum culling and static batching keep the draw-call count manageable on Quest.
    //
    // Cross-section (metres from the racing line, both sides):
    //   0-10 road (edge line 9.6-10) | 10-11.2 kerb at corners, otherwise run-off |
    //   11.2-16.8 run-off | 16.8-17.4 concrete wall + debris fence | 17.4-26 sidewalk |
    //   27.5+ buildings.  The driver's barrier limit is RoadHalfWidth + 6.5 = 16.5 m.
    public static class PolygonDowntownArt
    {
        public const string SceneryName="Circuit - editable scenery";
        // Ground sits 0.25 m below the road paint so distant road never fights the ground for depth.
        const float Base=-.25f;
        const float WallIn=16.8f,WallOut=17.4f,WallTop=1.1f,WalkOut=26f,BuildingClear=27.5f,ChunkSize=400;

        static Material asphalt,runoff,line,kerbRed,checkDark,wall,fence,walk,ground,stone,stoneDark,
            glassBlue,glassTeal,glassGold,glassDark,cream,teal,coral,navy,ochre,plum,lime,brick,steel,leaf,bark,
            distant,distantDark,harbor,skinLight,skinTan,skinDeep,hair,lamp,
            windowWarm,windowCool,asphaltPatch,asphaltSeal,crownTeal,crownAmber,aviation,billboardBack,hoarding,craneYellow;
        static Material[] teamColors,shirts,skins,facades;

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit))
                {
                    kit=new PolygonSceneryKit("Downtown "+key.x+"_"+key.y){MaxVerticesPerMesh=60000};
                    kits.Add(key,kit);
                }
                return kit;
            }
            public void Emit(Transform root)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Downtown block "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(root,false);
                    pair.Value.Emit(group);
                }
                kits.Clear();
            }
        }

        static Material Mat(string name,Color color)=>PolygonWorld.Paint("DT "+name,color);
        static Material Glow(string name,Color color,Color emission)
        {
            var m=Mat(name,color);m.SetColor("_EmissionColor",emission);return m;
        }

        static void CreatePalette()
        {
            asphalt=Mat("Street asphalt",new Color(.15f,.17f,.20f));
            runoff=Mat("Run-off asphalt",new Color(.22f,.24f,.27f));
            line=Mat("Road paint",new Color(.93f,.94f,.90f));
            kerbRed=Mat("Kerb red",new Color(.86f,.12f,.12f));
            checkDark=Mat("Start checks dark",new Color(.06f,.07f,.09f));
            wall=Mat("Concrete wall",new Color(.83f,.82f,.78f));
            fence=Mat("Debris fence",new Color(.22f,.24f,.27f));
            walk=Mat("Sidewalk",new Color(.66f,.63f,.59f));
            ground=Mat("City ground",new Color(.52f,.50f,.48f));
            stone=Mat("Warm stone",new Color(.86f,.79f,.68f));
            stoneDark=Mat("Dark stone",new Color(.45f,.42f,.40f));
            glassBlue=Mat("Glass blue",new Color(.22f,.40f,.55f));
            glassTeal=Mat("Glass teal",new Color(.16f,.46f,.50f));
            glassGold=Mat("Glass amber",new Color(.62f,.48f,.30f));
            glassDark=Mat("Glass dark",new Color(.08f,.12f,.17f));
            cream=Mat("Ivory",new Color(1,.94f,.80f));
            teal=Mat("Teal",new Color(.03f,.45f,.50f));
            coral=Mat("Coral",new Color(.97f,.36f,.32f));
            navy=Mat("Navy",new Color(.08f,.16f,.28f));
            ochre=Mat("Ochre",new Color(.95f,.66f,.30f));
            plum=Mat("Plum",new Color(.48f,.24f,.52f));
            lime=Mat("Lime",new Color(.55f,.80f,.25f));
            brick=Mat("Terracotta brick",new Color(.72f,.36f,.26f));
            steel=Mat("Steel",new Color(.55f,.58f,.62f));
            leaf=Mat("Street tree leaves",new Color(.20f,.50f,.28f));
            bark=Mat("Street tree bark",new Color(.38f,.26f,.18f));
            distant=Mat("Distant skyline",new Color(.50f,.52f,.62f));
            distantDark=Mat("Distant skyline shade",new Color(.38f,.40f,.50f));
            harbor=Mat("Harbor water",new Color(.07f,.38f,.55f));
            skinLight=Mat("Crowd skin light",new Color(.96f,.78f,.62f));
            skinTan=Mat("Crowd skin tan",new Color(.78f,.55f,.38f));
            skinDeep=Mat("Crowd skin deep",new Color(.45f,.30f,.20f));
            hair=Mat("Crowd hair",new Color(.16f,.11f,.08f));
            lamp=Mat("Lamp glow",new Color(1,.93f,.75f));
            // PC detail v01: emissive city lights (bloom picks these up on PC), patched street asphalt.
            lamp.SetColor("_EmissionColor",new Color(.85f,.62f,.35f));
            windowWarm=Glow("Window lit warm",new Color(1,.84f,.60f),new Color(.95f,.62f,.30f));
            windowCool=Glow("Window lit office",new Color(.80f,.90f,1),new Color(.42f,.56f,.72f));
            crownTeal=Glow("Crown light teal",new Color(.35f,.95f,.95f),new Color(.12f,.80f,.86f));
            crownAmber=Glow("Crown light amber",new Color(1,.78f,.40f),new Color(1,.56f,.18f));
            aviation=Glow("Aviation light red",new Color(1,.12f,.08f),new Color(1.6f,.08f,.05f));
            asphaltPatch=Mat("Street asphalt patch",new Color(.19f,.205f,.235f));
            asphaltSeal=Mat("Street asphalt seal",new Color(.115f,.13f,.155f));
            billboardBack=Mat("Billboard frame",new Color(.18f,.19f,.22f));
            hoarding=Mat("Site hoarding",new Color(.20f,.36f,.30f));
            craneYellow=Mat("Crane yellow",new Color(1,.78f,.10f));
            teamColors=new[]{coral,teal,ochre,navy,plum,lime};
            shirts=new[]{coral,teal,ochre,navy,plum,lime,cream,glassBlue};
            skins=new[]{skinLight,skinTan,skinDeep};
            facades=new[]{cream,ochre,coral,teal,brick,stone};
        }

        // ------------------------------------------------------------ route helpers
        static Vector3 P(float d)=>PolygonCircuit.AtDistance(d);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static Vector3 At(float d,float lateral,float y)=>P(d)+S(d)*lateral+Vector3.up*y;
        static float DistanceAt(Vector3 p)=>PolygonCircuit.ProjectLap(p,out _)*PolygonCircuit.Length;
        static float Offset(Vector3 p){PolygonCircuit.ProjectLap(p,out float offset);return offset;}
        static float Hash(float x,float z,int salt=0)=>Mathf.Abs(Mathf.Sin(x*12.9898f+z*78.233f+salt*37.719f)*43758.5453f)%1f;

        static void Ribbon(PolygonSceneryKit kit,Material m,float d0,float d1,float l0,float l1,float y)
        {
            var a=At(d0,l0,y);var b=At(d0,l1,y);var c=At(d1,l1,y);var e=At(d1,l0,y);
            kit.Quad(m,a,b,c,e,(a+b+c+e)*.25f+Vector3.down);
        }

        static Transform Frame(Transform parent,string name,float d,float lateral)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);
            t.localPosition=At(d,lateral,Base);t.localRotation=Quaternion.LookRotation(T(d));
            return t;
        }

        // Reserved areas (world XZ rectangles) kept free of generated buildings.
        static readonly List<Rect> reserved=new List<Rect>();
        static bool Reserved(Vector2 p,float margin)
        {
            foreach(var r in reserved)
                if(p.x>r.xMin-margin&&p.x<r.xMax+margin&&p.y>r.yMin-margin&&p.y<r.yMax+margin)return true;
            return false;
        }
        static void Reserve(Transform frame,float x0,float x1,float z0,float z1)
        {
            var a=frame.TransformPoint(new Vector3(x0,0,z0));var b=frame.TransformPoint(new Vector3(x1,0,z1));
            var c=frame.TransformPoint(new Vector3(x0,0,z1));var d=frame.TransformPoint(new Vector3(x1,0,z0));
            float minX=Mathf.Min(Mathf.Min(a.x,b.x),Mathf.Min(c.x,d.x)),maxX=Mathf.Max(Mathf.Max(a.x,b.x),Mathf.Max(c.x,d.x));
            float minZ=Mathf.Min(Mathf.Min(a.z,b.z),Mathf.Min(c.z,d.z)),maxZ=Mathf.Max(Mathf.Max(a.z,b.z),Mathf.Max(c.z,d.z));
            reserved.Add(Rect.MinMaxRect(minX,minZ,maxX,maxZ));
        }

        // ------------------------------------------------------------ entry point
        public static Transform Build(Transform circuitRoot)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.Downtown)
                throw new System.InvalidOperationException("Select the Downtown layout before building its scenery.");
            CreatePalette();reserved.Clear();PrepareTrackSamples();cranes=0;billboards=0;footbridges=0;
            var world=new GameObject(SceneryName).transform;world.SetParent(circuitRoot,false);
            var chunks=new Chunks();
            BuildGroundAndHarbor(chunks);
            BuildRoad(chunks);
            BuildWallsAndSidewalks(chunks);
            BuildStartGantry(world);
            BuildTimingPylon(world);
            BuildGrandstands(world);
            BuildTeamGarages(world);
            BuildMonorail(world,chunks);
            BuildFootbridges(world);
            BuildStreetFurniture(chunks);
            BuildCornerBoards(world);
            BuildCity(chunks);
            BuildSkyline(chunks);
            chunks.Emit(world);
            AttachHarbor(world);
            Debug.Log($"Downtown PC detail v01: {footbridges} footbridges, {cranes} tower cranes, {billboards} rooftop billboards.");
            reserved.Clear();trackSamples=null;
            return world;
        }

        // ------------------------------------------------------------ ground, harbor
        const float SeaWallZ=990;
        static void BuildGroundAndHarbor(Chunks chunks)
        {
            // Ground tiles (per chunk, so each piece can be culled) stop at the harbor wall.
            for(float x=-1600;x<2000;x+=ChunkSize)
                for(float z=-1200;z<SeaWallZ;z+=ChunkSize)
                {
                    float z1=Mathf.Min(z+ChunkSize,SeaWallZ);
                    var a=new Vector3(x,Base,z);var b=new Vector3(x+ChunkSize,Base,z);
                    var c=new Vector3(x+ChunkSize,Base,z1);var d=new Vector3(x,Base,z1);
                    chunks.At((a+c)*.5f).Quad(ground,a,b,c,d,(a+c)*.5f+Vector3.down);
                }
            // Harbor promenade, sea wall and water to the north of the circuit.
            for(float x=-1600;x<2000;x+=ChunkSize)
            {
                var kit=chunks.At(new Vector3(x+200,0,SeaWallZ));
                kit.Box(stone,new Vector3(x+ChunkSize*.5f,-.6f,SeaWallZ+2),new Vector3(ChunkSize,1.2f,4));
                kit.Box(stoneDark,new Vector3(x+ChunkSize*.5f,.5f,SeaWallZ+3.6f),new Vector3(ChunkSize,1.0f,.6f));
            }
            // The water is one separate object so the look pass can give it the water shader.
            var water=new PolygonSceneryKit("Downtown harbor water");
            var w0=new Vector3(-2400,-1.2f,SeaWallZ+4);var w1=new Vector3(2800,-1.2f,SeaWallZ+4);
            var w2=new Vector3(2800,-1.2f,3400);var w3=new Vector3(-2400,-1.2f,3400);
            water.Quad(harbor,w0,w1,w2,w3,(w0+w2)*.5f+Vector3.down);
            pendingWater=water;
            // A few moored sailboats along the promenade.
            var hull=PolygonSceneryMeshes.BoatHull();var cabin=PolygonSceneryMeshes.BoatCabin();
            var rig=PolygonSceneryMeshes.BoatRigging();var sails=PolygonSceneryMeshes.BoatSails();
            pendingBoats.Clear();
            for(int i=0;i<9;i++)
            {
                float x=-500+i*170+Hash(i,3)*40;
                pendingBoats.Add((new Vector3(x,-1.25f,SeaWallZ+28+Hash(i,9)*60),Hash(i,5)*360,i%3));
            }
            boatMeshes=new[]{hull,cabin,rig,sails};
        }
        static PolygonSceneryKit pendingWater;
        static readonly List<(Vector3 pos,float yaw,int tint)> pendingBoats=new List<(Vector3,float,int)>();
        static Mesh[] boatMeshes;

        // ------------------------------------------------------------ road surface
        static void BuildRoad(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float length=PolygonCircuit.Length,step=length/n;
            const float checkerEnd=1.6f;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));
                bool corner=Mathf.Abs(PolygonCircuit.Curvature(dm))>1/110f||Mathf.Abs(PolygonCircuit.Curvature(dm-18))>1/110f
                    ||Mathf.Abs(PolygonCircuit.Curvature(dm+18))>1/110f;
                bool kerbStripe=(i/2)%2==0;
                foreach(float side in new[]{-1f,1f})
                {
                    Band(kit,side,d0,d1,9.6f,10,line);
                    Band(kit,side,d0,d1,10,11.2f,corner?(kerbStripe?kerbRed:line):runoff);
                    Band(kit,side,d0,d1,11.2f,WallIn,runoff);
                }
                if(d0<checkerEnd)
                {
                    // Start/finish checks replace the asphalt in this short stretch (no overlay).
                    const int rows=2,cols=24;
                    for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
                    {
                        float a0=d0+checkerEnd*r/rows,a1=d0+checkerEnd*(r+1)/rows;
                        float l0=-9.6f+c*19.2f/cols,l1=l0+19.2f/cols;
                        Ribbon(kit,(r+c)%2==0?line:checkDark,a0,a1,l0,l1,.025f);
                    }
                    d0=checkerEnd;
                }
                bool dash=(i/3)%2==0&&!corner;
                // Street-circuit patchwork: lane-wide repairs and sealed strips replace the plain
                // asphalt in place (separate ribbons, never an overlay).
                Ribbon(kit,StreetSurface(i,0),d0,d1,-9.6f,-4.8f,.025f);
                Ribbon(kit,StreetSurface(i,1),d0,d1,-4.8f,-.12f,.025f);
                Ribbon(kit,dash?line:asphalt,d0,d1,-.12f,.12f,.025f);
                Ribbon(kit,StreetSurface(i,2),d0,d1,.12f,4.8f,.025f);
                Ribbon(kit,StreetSurface(i,3),d0,d1,4.8f,9.6f,.025f);
            }
        }
        static Material StreetSurface(int segment,int lane)
        {
            if(Hash(segment/4,lane,11)<.10f)return asphaltPatch;
            if(Hash(segment/7,lane,12)<.08f)return asphaltSeal;
            return asphalt;
        }
        static void Band(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,Material m)
        {
            if(side>0)Ribbon(kit,m,d0,d1,a,b,.025f);else Ribbon(kit,m,d0,d1,-b,-a,.025f);
        }

        // ------------------------------------------------------------ walls, fence, sidewalks
        static void BuildWallsAndSidewalks(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount/2;float step=PolygonCircuit.Length/n;
            var boards=new[]{cream,teal,coral,navy,ochre,cream,plum};
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=(i+1)*step,dm=(d0+d1)*.5f;
                var kit=chunks.At(P(dm));
                foreach(float side in new[]{-1f,1f})
                {
                    // Livery boards change every four segments (about 25 m).
                    var board=boards[((i/4)+(side>0?3:0))%boards.Length];
                    WallSegment(kit,side,d0,d1,WallIn,WallOut,Base,WallTop,board);
                    WallSegment(kit,side,d0,d1,WallOut,WalkOut,Base,.15f,walk);
                    // Debris fence: two rails, posts every other segment.
                    float rail=side*(WallIn+WallOut)*.5f;
                    kit.Rail(fence,At(d0,rail,2.4f),At(d1,rail,2.4f),.06f);
                    kit.Rail(fence,At(d0,rail,4.0f),At(d1,rail,4.0f),.06f);
                    if(i%2==0)kit.Beam(fence,At(d0,rail,WallTop),At(d0,rail,4.05f),.1f,.1f);
                }
            }
        }
        static void WallSegment(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,float y0,float y1,Material m)
        {
            float l0=side>0?a:-b,l1=side>0?b:-a;
            kit.Prism(m,new[]{At(d0,l0,y0),At(d0,l1,y0),At(d1,l1,y0),At(d1,l0,y0)},
                        new[]{At(d0,l0,y1),At(d0,l1,y1),At(d1,l1,y1),At(d1,l0,y1)});
        }

        // ------------------------------------------------------------ start gantry
        static void BuildStartGantry(Transform world)
        {
            var gate=Frame(world,"POLYGON CITY - start gantry",32,0);
            var kit=new PolygonSceneryKit("City gantry");
            const float span=19.5f,beamY=9.5f;
            foreach(int side in new[]{-1,1})
            {
                var foot=new Vector3(side*span,0,0);
                kit.Chamfered(stoneDark,foot,1.4f,1.6f,1.3f,1.5f,0,.8f,.35f);
                kit.Chamfered(navy,foot,.8f,.95f,.6f,.75f,.8f,beamY+2.2f,.25f);
                kit.Chamfered(coral,foot,.9f,1.05f,.86f,1.0f,1.1f,1.9f,.3f);
                kit.Box(teal,new Vector3(side*(span-.85f),5.5f,0),new Vector3(.3f,8,.3f));
                // Tall vertical banners on the outer face of each pylon.
                kit.SheetQuad(side>0?teal:coral,new Vector3(side*(span+.9f),beamY+1,-.6f),new Vector3(side*(span+.9f),beamY+1,.6f),
                    new Vector3(side*(span+.9f),3,.6f),new Vector3(side*(span+.9f),2.4f,-.6f));
            }
            // Box truss: top and bottom chords with diagonal lacing.
            foreach(float z in new[]{-.7f,.7f})
            {
                kit.Beam(navy,new Vector3(-span,beamY,z),new Vector3(span,beamY,z),.35f,.35f);
                kit.Beam(navy,new Vector3(-span,beamY+2.2f,z),new Vector3(span,beamY+2.2f,z),.35f,.35f);
                for(int k=0;k<12;k++)
                {
                    float x0=-span+k*span*2/12,x1=x0+span*2/12;
                    kit.Beam(steel,new Vector3(x0,k%2==0?beamY:beamY+2.2f,z),new Vector3(x1,k%2==0?beamY+2.2f:beamY,z),.12f,.12f);
                }
            }
            // Title panels front and back.
            kit.Box(navy,new Vector3(0,beamY+1.1f,-.85f),new Vector3(26,1.9f,.2f));
            kit.Box(navy,new Vector3(0,beamY+1.1f,.85f),new Vector3(26,1.9f,.2f));
            foreach(int side in new[]{-1,1})for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            {
                var m=(row+col)%2==0?cream:checkDark;
                kit.Box(m,new Vector3(side*(14.4f+col*.9f),beamY+.65f+row*.9f,-.95f),new Vector3(.9f,.9f,.2f));
                kit.Box(m,new Vector3(side*(14.4f+col*.9f),beamY+.65f+row*.9f,.95f),new Vector3(.9f,.9f,.2f));
            }
            var front=PolygonWorld.Label("Polygon City race lettering",gate,"POLYGON CITY",new Vector3(0,beamY+1.1f,-1.0f),.2f,cream.color);
            var rear=PolygonWorld.Label("Polygon City reverse lettering",gate,"POLYGON CITY",new Vector3(0,beamY+1.1f,1.0f),.2f,cream.color);
            rear.transform.localRotation=Quaternion.Euler(0,180,0);
            var letteringShader=Resources.Load<Shader>("PolygonGantryLettering");
            if(letteringShader)
            {
                var lettering=new Material(letteringShader){name="Polygon City depth tested lettering"};
                lettering.mainTexture=front.font.material.mainTexture;
                front.GetComponent<MeshRenderer>().sharedMaterial=lettering;rear.GetComponent<MeshRenderer>().sharedMaterial=lettering;
                var swap=gate.gameObject.AddComponent<PolygonGantryLettering>();
                swap.FrontText=front;swap.RearText=rear;swap.DepthMaterial=lettering;
            }
            // Five-light start signal under the truss (same controller as the coast).
            var dark=Mat("Starting lens dark",new Color(.065f,.018f,.025f));
            var lit=Mat("Starting lens red",new Color(1,.025f,.012f));
            lit.SetColor("_EmissionColor",new Color(1,.02f,.008f));
            kit.Box(checkDark,new Vector3(0,beamY-.9f,-.1f),new Vector3(9.3f,1.6f,1.2f));
            for(int k=0;k<2;k++)kit.Box(steel,new Vector3(k==0?-4:4,beamY-.05f,0),new Vector3(.14f,.3f,.18f));
            for(int i=0;i<5;i++)
                kit.Box(checkDark,new Vector3((i-2)*1.7f,beamY-.34f,-.98f),new Vector3(1.15f,.06f,.56f),Quaternion.Euler(-8,0,0));
            kit.Emit(gate);
            var lens=PolygonSceneryMeshesExtra.SignalLens();var lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)
            {
                var go=new GameObject("Starting red lens "+(i+1),typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(gate,false);go.transform.localPosition=new Vector3((i-2)*1.7f,beamY-.9f,-.71f);
                go.GetComponent<MeshFilter>().sharedMesh=lens;lenses[i]=go.GetComponent<MeshRenderer>();lenses[i].sharedMaterial=lit;
            }
            // The dark material must also sit on a renderer so the editor saves it as an asset.
            var keep=new GameObject("Starting lens dark material holder",typeof(MeshFilter),typeof(MeshRenderer));
            keep.transform.SetParent(gate,false);keep.transform.localPosition=new Vector3(0,beamY-.9f,.55f);keep.transform.localScale=Vector3.one*.4f;
            keep.GetComponent<MeshFilter>().sharedMesh=lens;keep.GetComponent<MeshRenderer>().sharedMaterial=dark;
            var lights=gate.gameObject.AddComponent<PolygonStartLights>();
            lights.Lenses=lenses;lights.DarkMaterial=dark;lights.RedMaterial=lit;
        }

        // ------------------------------------------------------------ timing pylon
        static void BuildTimingPylon(Transform world)
        {
            var t=Frame(world,"City timing pylon",-40,-31);
            var kit=new PolygonSceneryKit("Timing pylon");
            kit.Chamfered(stoneDark,Vector3.zero,3,3,2.6f,2.6f,0,1,.6f);
            kit.Box(navy,new Vector3(0,14,0),new Vector3(4,26,1.6f));
            kit.Box(coral,new Vector3(0,27.5f,0),new Vector3(4.4f,1.2f,2));
            for(int i=0;i<10;i++)
            {
                float y=24.5f-i*2.2f;
                kit.Box(teamColors[i%teamColors.Length],new Vector3(-1.3f,y,-.85f),new Vector3(.9f,1.5f,.12f));
                kit.Box(cream,new Vector3(.45f,y,-.85f),new Vector3(2.4f,1.5f,.12f));
                kit.Box(teamColors[(i+3)%teamColors.Length],new Vector3(-1.3f,y,.85f),new Vector3(.9f,1.5f,.12f));
                kit.Box(cream,new Vector3(.45f,y,.85f),new Vector3(2.4f,1.5f,.12f));
            }
            kit.Emit(t);
            Reserve(t,-8,8,-8,8);
        }

        // ------------------------------------------------------------ grandstands
        static void BuildGrandstands(Transform world)
        {
            // Inside of the start straight (left, +lateral is right): two big stands.
            Grandstand(world,"Main grandstand A",70,82,-1,12,0);
            Grandstand(world,"Main grandstand B",162,82,-1,12,1);
            Grandstand(world,"Boulevard grandstand",270,70,-1,9,2);
            // Outside of the straight, past the team garages.
            Grandstand(world,"Riverside grandstand",255,90,1,10,3);
            // Two smaller stands on the outside of the north and east boulevards.
            Grandstand(world,"North boulevard stand",DistanceAt(new Vector3(180,0,700))-30,60,1,8,4);
            Grandstand(world,"East boulevard stand",DistanceAt(new Vector3(700,0,170))-30,60,1,8,5);
        }

        static void Grandstand(Transform world,string name,float startD,float length,int sign,int rows,int theme)
        {
            float mid=startD+length*.5f;
            var t=Frame(world,name,mid,0);
            var kit=new PolygonSceneryKit("Grandstand "+theme);
            const float rowDepth=1.2f,rise=.75f,front=WalkOut+.5f;
            float back=front+rows*rowDepth;
            var facing=new Vector3(-sign,0,0);
            int sections=Mathf.Max(1,Mathf.RoundToInt(length/12));
            float sectionLength=length/sections;
            for(int r=0;r<rows;r++)
            {
                float x0=front+r*rowDepth,top=1.3f+r*rise;
                kit.Box(r%2==0?stone:wall,new Vector3(sign*(x0+rowDepth*.5f),top*.5f,0),new Vector3(rowDepth,top,length));
                for(int sct=0;sct<sections;sct++)
                {
                    var seatColor=teamColors[(sct+theme+(r/4))%teamColors.Length];
                    float z0=-length*.5f+sct*sectionLength;
                    kit.Box(seatColor,new Vector3(sign*(x0+.45f),top+.22f,z0+sectionLength*.5f),new Vector3(.5f,.44f,sectionLength-.9f));
                    int seats=Mathf.FloorToInt((sectionLength-1.2f)/1.05f);
                    for(int s=0;s<seats;s++)
                    {
                        float z=z0+.9f+s*1.05f;
                        float h=Hash(z+theme*31,r);
                        if(h<.24f)continue;
                        var shirt=shirts[Mathf.FloorToInt(Hash(z,r,3)*shirts.Length)%shirts.Length];
                        if(Hash(z,r,9)<.35f)shirt=teamColors[(sct+theme)%teamColors.Length];
                        kit.PersonLite(new Vector3(sign*(x0+.45f),top+.18f,z),facing,.95f+h*.12f,shirt,
                            skins[Mathf.FloorToInt(Hash(z,r,5)*3)%3],h>.85f?cream:hair,h>.92f);
                    }
                }
            }
            // Front fascia in team colours and a rail.
            for(int sct=0;sct<sections;sct++)
            {
                float z0=-length*.5f+sct*sectionLength;
                kit.Box(teamColors[(sct+theme+2)%teamColors.Length],new Vector3(sign*(front-.1f),.75f,z0+sectionLength*.5f),new Vector3(.2f,1.5f,sectionLength-.2f));
            }
            kit.Box(steel,new Vector3(sign*(front-.1f),2.05f,0),new Vector3(.08f,.08f,length));
            // Back wall with tall colour panels, and stair towers at the ends.
            float wallTop=1.3f+rows*rise+3;
            kit.Box(navy,new Vector3(sign*(back+.25f),wallTop*.5f,0),new Vector3(.5f,wallTop,length+.6f));
            for(int sct=0;sct<sections;sct++)
            {
                float z0=-length*.5f+sct*sectionLength;
                kit.Box(teamColors[(sct+theme+1)%teamColors.Length],new Vector3(sign*(back+.6f),wallTop*.55f,z0+sectionLength*.5f),
                    new Vector3(.2f,wallTop*.7f,sectionLength*.7f));
            }
            foreach(int end in new[]{-1,1})
                kit.Box(stone,new Vector3(sign*(front+back)*.5f,wallTop*.5f,end*(length*.5f+1.6f)),new Vector3(back-front+1,wallTop,3.2f));
            // Repeated peaked canopy modules on slim masts: the stand's signature roof.
            int modules=Mathf.Max(2,Mathf.RoundToInt(length/10));
            float moduleLength=length/modules,roofLow=wallTop+1.2f,roofHigh=wallTop+3.4f;
            for(int k=0;k<modules;k++)
            {
                float z0=-length*.5f+k*moduleLength,z1=z0+moduleLength,zm=(z0+z1)*.5f;
                var m=k%3==0?cream:k%3==1?teal:coral;
                var a=new Vector3(sign*(front-1.5f),roofLow,z0+.15f);var b=new Vector3(sign*(back+.5f),roofHigh,z0+.15f);
                var c=new Vector3(sign*(back+.5f),roofHigh,z1-.15f);var d=new Vector3(sign*(front-1.5f),roofLow,z1-.15f);
                var peak=new Vector3(sign*((front+back)*.5f),roofHigh+1.6f,zm);
                kit.Sheet(m,a,b,peak);kit.Sheet(m,b,c,peak);kit.Sheet(m,c,d,peak);kit.Sheet(m,d,a,peak);
                kit.Beam(steel,new Vector3(sign*(back+.4f),0,z0),new Vector3(sign*(back+.4f),roofHigh+.1f,z0),.3f,.3f);
                kit.Beam(steel,new Vector3(sign*(back+.4f),roofHigh,z0),new Vector3(sign*(front-1.4f),roofLow,z0),.18f,.24f);
            }
            kit.Beam(steel,new Vector3(sign*(back+.4f),0,length*.5f),new Vector3(sign*(back+.4f),roofHigh+.1f,length*.5f),.3f,.3f);
            kit.Beam(steel,new Vector3(sign*(back+.4f),roofHigh,length*.5f),new Vector3(sign*(front-1.4f),roofLow,length*.5f),.18f,.24f);
            kit.Emit(t);
            Reserve(t,sign>0?front-2:-(back+6),sign>0?back+6:-(front-2),-length*.5f-4,length*.5f+4);
        }

        // ------------------------------------------------------------ team garages and parked cars
        static void BuildTeamGarages(Transform world)
        {
            var t=Frame(world,"City team garages",130,0);
            var kit=new PolygonSceneryKit("Team garages");
            const float x0=36,x1=50,length=120,h=7.5f;
            // Right side of the start straight (positive lateral).
            kit.Box(stone,new Vector3((x0+x1)*.5f,h*.5f,0),new Vector3(x1-x0,h,length));
            kit.Box(glassDark,new Vector3((x0+x1)*.5f,h+2.1f,0),new Vector3(x1-x0-1,4.2f,length-1));
            kit.Box(cream,new Vector3((x0+x1)*.5f,h+4.45f,0),new Vector3(x1-x0+1.2f,.5f,length+1.2f));
            kit.Box(stone,new Vector3((x0+x1)*.5f,h+.2f,0),new Vector3(x1-x0+.8f,.4f,length+.8f));
            const int bays=8;float bay=length/bays;
            var slots=new GameObject("Paddock parked car slots").transform;slots.SetParent(world,false);
            for(int i=0;i<bays;i++)
            {
                float z=-length*.5f+bay*(i+.5f);var color=teamColors[i%teamColors.Length];
                kit.Box(glassDark,new Vector3(x0-.08f,2.6f,z),new Vector3(.3f,5.2f,bay-3));
                kit.Box(color,new Vector3(x0-.2f,5.8f,z),new Vector3(.4f,.9f,bay-.6f));
                kit.Box(color,new Vector3(x0-.15f,h+.95f,z),new Vector3(.3f,1.5f,bay*.6f));
                // Rooftop team flag.
                kit.Beam(steel,new Vector3(x1-1,h+4.7f,z-bay*.3f),new Vector3(x1-1,h+12,z-bay*.3f),.12f,.12f);
                kit.SheetQuad(color,new Vector3(x1-1,h+11.8f,z-bay*.3f),new Vector3(x1-1,h+11.8f,z-bay*.3f+3),
                    new Vector3(x1-1,h+9.6f,z-bay*.3f+3),new Vector3(x1-1,h+9.4f,z-bay*.3f));
                if(i%4==1||i%4==2)
                {
                    var slot=new GameObject("Parked team car slot "+i).transform;slot.SetParent(slots,false);
                    slot.position=t.TransformPoint(new Vector3(x0-4.5f,.15f,z));
                    slot.rotation=t.rotation*Quaternion.Euler(0,-90,0);
                }
            }
            kit.Emit(t);
            Reserve(t,x0-6,x1+4,-length*.5f-4,length*.5f+4);
        }

        // ------------------------------------------------------------ elevated monorail
        static void BuildMonorail(Transform world,Chunks chunks)
        {
            // Runs north-south along x = 100, crossing the north boulevard (z = 700) at 10 m.
            const float x=100,deckY=10,z0=430,z1=SeaWallZ-10;
            var kit=new PolygonSceneryKit("Monorail");
            kit.Box(cream,new Vector3(x,deckY,(z0+z1)*.5f),new Vector3(5.5f,1.2f,z1-z0));
            kit.Box(teal,new Vector3(x,deckY+.85f,(z0+z1)*.5f),new Vector3(1.4f,.5f,z1-z0));
            foreach(int side in new[]{-1,1})kit.Box(steel,new Vector3(x+side*2.6f,deckY+1.0f,(z0+z1)*.5f),new Vector3(.12f,.8f,z1-z0));
            for(float z=z0+12;z<z1;z+=34)
            {
                var p=new Vector3(x,Base,z);
                if(Offset(p)<20)continue;
                kit.Chamfered(stone,p,1.1f,1.1f,.8f,.8f,0,deckY-.6f,.3f);
                kit.Box(stone,new Vector3(x,deckY-.8f,z),new Vector3(5,1,2.2f));
                ReserveWorld(x-3,x+3,z-3,z+3);
            }
            ReserveWorld(x-4,x+4,z0,z1);
            // A three-car train parked over the boulevard.
            float tz=640;
            for(int car=0;car<3;car++)
            {
                float cz=tz+car*15.5f;
                kit.Chamfered(cream,new Vector3(x,deckY+1.1f,cz),1.5f,7.2f,1.5f,7.2f,0,2.4f,.5f);
                kit.Box(glassDark,new Vector3(x,deckY+2.4f,cz),new Vector3(3.15f,.8f,13.4f));
                kit.Chamfered(teal,new Vector3(x,deckY+3.0f,cz),1.4f,7.0f,1.0f,6.6f,0,.5f,.5f);
                kit.Box(coral,new Vector3(x,deckY+1.5f,cz),new Vector3(3.1f,.25f,14.2f));
            }
            kit.Chamfered(glassDark,new Vector3(x,deckY+1.4f,tz-7.6f),1.3f,.6f,.7f,.3f,0,2,.3f);
            kit.Emit(world);
        }
        static void ReserveWorld(float x0,float x1,float z0,float z1){reserved.Add(Rect.MinMaxRect(x0,z0,x1,z1));}

        // ------------------------------------------------------------ lamps, trees, flags
        static void BuildStreetFurniture(Chunks chunks)
        {
            float length=PolygonCircuit.Length;int index=0;
            for(float d=8;d<length;d+=36,index++)
            {
                foreach(float side in new[]{-1f,1f})
                {
                    var kit=chunks.At(P(d));
                    // Street lamp on the outer sidewalk, arm over the fence.
                    var foot=At(d,side*24.5f,0);
                    if(!Reserved(new Vector2(foot.x,foot.z),2))
                    {
                        kit.Column(steel,foot,.14f,.1f,8.5f,6);
                        var arm=At(d,side*20.5f,8.5f);
                        kit.Beam(steel,foot+Vector3.up*8.3f,arm,.12f,.12f);
                        kit.Box(navy,arm+Vector3.down*.15f,new Vector3(1.0f,.25f,.5f),Quaternion.LookRotation(T(d)));
                        kit.Box(lamp,arm+Vector3.down*.32f,new Vector3(.8f,.1f,.35f),Quaternion.LookRotation(T(d)));
                    }
                    // Street tree in a planter, offset half a step.
                    float dt=d+18;var tree=At(dt,side*22.5f,0);
                    if(!Reserved(new Vector2(tree.x,tree.z),3)&&Mathf.Abs(PolygonCircuit.Curvature(dt))<1/200f)
                        StreetTree(chunks.At(tree),tree,.8f+Hash(tree.x,tree.z)*.5f);
                    // Team flags every other lamp, alternating colours around the lap.
                    if(index%2==0)
                    {
                        var pole=At(d+9,side*20.2f,0);
                        if(!Reserved(new Vector2(pole.x,pole.z),2))
                        {
                            var color=teamColors[(index/2+(side>0?2:0))%teamColors.Length];
                            kit.Column(steel,pole+Vector3.up*.15f,.08f,.06f,11,5);
                            var t=T(d+9);var top=pole+Vector3.up*10.8f;
                            kit.Sheet(color,top,top+t*2.4f,top+Vector3.down*4.2f);
                            kit.Sheet(color,top+t*2.4f,top+t*2.4f+Vector3.down*4.6f,top+Vector3.down*4.2f);
                            kit.Sheet(color,top+Vector3.down*4.2f,top+t*2.4f+Vector3.down*4.6f,top+t*1.2f+Vector3.down*3.6f);
                        }
                    }
                }
            }
        }

        static void StreetTree(PolygonSceneryKit kit,Vector3 p,float scale)
        {
            kit.Box(stone,p+Vector3.up*.35f,new Vector3(2.2f,.7f,2.2f));
            kit.Column(bark,p+Vector3.up*.7f,.18f*scale,.12f*scale,3.2f*scale,5);
            var c=p+Vector3.up*(.7f+3.6f*scale);
            kit.Chamfered(leaf,c+Vector3.down*1.2f*scale,1.2f*scale,1.2f*scale,2.0f*scale,2.0f*scale,0,1.4f*scale,.9f*scale);
            kit.Chamfered(leaf,c+Vector3.up*.2f*scale,2.0f*scale,2.0f*scale,.9f*scale,.9f*scale,0,1.8f*scale,.9f*scale);
        }

        // ------------------------------------------------------------ corner distance boards
        static void BuildCornerBoards(Transform world)
        {
            float length=PolygonCircuit.Length;
            var corners=new List<float>();float last=-999;
            for(float d=0;d<length;d+=4)
            {
                float c=PolygonCircuit.Curvature(d);
                if(Mathf.Abs(c)>1/60f&&d-last>120){corners.Add(d);last=d;}
            }
            var ink=Mat("Board ink",new Color(.98f,.97f,.93f));
            foreach(float corner in corners)
            {
                float turn=Mathf.Sign(PolygonCircuit.Curvature(corner+10));
                foreach(int metres in new[]{150,100,50})
                {
                    float d=corner-metres-20;if(d<40&&d>-40)continue;
                    var board=Frame(world,"City corner board "+metres,d,-turn*21.5f);
                    PolygonWorld.Box("Board post",board,new Vector3(0,1.6f,0),new Vector3(.2f,3.2f,.2f),fence);
                    PolygonWorld.Box("Board panel",board,new Vector3(0,3.6f,0),new Vector3(2.6f,1.6f,.2f),navy);
                    PolygonWorld.Label("Board lettering",board,metres.ToString(),new Vector3(0,3.6f,-.16f),.09f,ink.color);
                }
            }
        }

        // ------------------------------------------------------------ city blocks
        static readonly Vector2 Core=new Vector2(190,400);
        static void BuildCity(Chunks chunks)
        {
            const float lot=52;
            for(float x=-900;x<=1340;x+=lot)
                for(float z=-560;z<=SeaWallZ-30;z+=lot)
                {
                    float jx=(Hash(x,z,1)-.5f)*8,jz=(Hash(x,z,2)-.5f)*8;
                    var c=new Vector3(x+jx,Base,z+jz);
                    float hx=10+Hash(x,z,3)*11,hz=10+Hash(x,z,4)*11;
                    if(c.z+hz>SeaWallZ-14)continue;
                    if(Reserved(new Vector2(c.x,c.z),Mathf.Max(hx,hz)+2))continue;
                    // Podiums and awnings reach about 2.5 m past the footprint.
                    float near=MinOffset(c,hx+2.5f,hz+2.5f);
                    if(near<BuildingClear)
                    {
                        // Try a slimmer building before giving up on this lot.
                        hx*=.6f;hz*=.6f;near=MinOffset(c,hx+2.5f,hz+2.5f);
                        if(near<BuildingClear||hx<6)continue;
                    }
                    float coreDistance=Vector2.Distance(new Vector2(c.x,c.z),Core);
                    float h=Mathf.Lerp(230,26,Mathf.Clamp01(coreDistance/620f))*(.55f+Hash(x,z,5)*.7f);
                    if(near<60)h=Mathf.Max(h,24+Hash(x,z,6)*40);
                    h=Mathf.Clamp(h,12,260);
                    int detail=near<150?2:near<380?1:0;
                    if(detail==0&&Hash(x,z,7)<.25f)continue;
                    if(h<34&&Hash(x,z,8)<.12f&&detail>0){Plaza(chunks.At(c),c,hx,hz);continue;}
                    if(cranes<MaxCranes&&near>40&&near<220&&Hash(x,z,10)<.08f&&ConstructionSite(chunks.At(c),c,hx,hz,h,Hash(x,z,9)))
                        continue;
                    Building(chunks.At(c),c,hx,hz,h,detail,Hash(x,z,9));
                }
        }

        static float MinOffset(Vector3 c,float hx,float hz)
        {
            float best=float.MaxValue;
            for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)
                best=Mathf.Min(best,Offset(c+new Vector3(i*hx,0,j*hz)));
            return best;
        }

        static void Plaza(PolygonSceneryKit kit,Vector3 c,float hx,float hz)
        {
            kit.Box(walk,c+Vector3.up*.2f,new Vector3(hx*2,.4f,hz*2));
            kit.Box(lime,c+Vector3.up*.45f,new Vector3(hx*1.4f,.15f,hz*1.4f));
            for(int i=0;i<4;i++)
            {
                var p=c+new Vector3((i%2==0?-1:1)*hx*.55f,0,(i<2?-1:1)*hz*.55f);
                StreetTree(kit,p,1+Hash(p.x,p.z)*.4f);
            }
            kit.Column(cream,c,1.2f,.6f,5,8);
        }

        static void Building(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,int detail,float pick)
        {
            if(detail==0){SimpleBlock(kit,c,hx,hz,h,pick);return;}
            if(h>110)
            {
                if(pick<.4f)GlassTower(kit,c,hx,hz,h,detail,pick);
                else if(pick<.7f)SteppedTower(kit,c,hx,hz,h,detail,pick);
                else RoundTower(kit,c,Mathf.Min(hx,hz),h,detail,pick);
            }
            else if(h>45)
            {
                if(pick<.5f)GlassTower(kit,c,hx,hz,h,detail,pick);
                else if(pick<.8f)SteppedTower(kit,c,hx,hz,h,detail,pick);
                else Apartment(kit,c,hx,hz,h,detail,pick);
            }
            else Apartment(kit,c,hx,hz,h,detail,pick);
        }

        static Material GlassFor(float pick)=>pick<.33f?glassBlue:pick<.66f?glassTeal:glassGold;

        // Windows never overlap walls: facades are stacked as solid wall slabs and recessed glass
        // bands (or deep proud fins), so no two surfaces sit within a few centimetres of each other.
        // Shallow overlaps flickered in the headset at distance (depth-buffer precision).
        const float Recess=.35f,Proud=.6f;

        // Alternating wall slabs and recessed glass from y0 to y1 (relative to c).
        static void Facade(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float y0,float y1,float step,float glassShare,Material wall,Material glass)
        {
            float y=y0;
            while(y+step<=y1+.01f)
            {
                float g=step*glassShare;
                kit.Box(wall,c+Vector3.up*(y+(step-g)*.5f),new Vector3(hx*2,step-g,hz*2));
                kit.Sleeve(glass,c+Vector3.up*(y+step-g*.5f),new Vector3(hx*2-Recess*2,g,hz*2-Recess*2));
                y+=step;
            }
            if(y1-y>.05f)kit.Box(wall,c+Vector3.up*((y+y1)*.5f),new Vector3(hx*2,y1-y,hz*2));
        }

        static void GlassTower(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,int detail,float pick)
        {
            var glass=GlassFor(pick*3%1);var frame=pick<.5f?cream:steel;
            // Podium: stone base, recessed lobby glass, stone cap.
            kit.Box(stoneDark,c+Vector3.up*1.7f,new Vector3(hx*2+3,3.4f,hz*2+3));
            kit.Sleeve(glassDark,c+Vector3.up*4.5f,new Vector3(hx*2+3-Recess*2,2.2f,hz*2+3-Recess*2));
            kit.Box(stoneDark,c+Vector3.up*5.8f,new Vector3(hx*2+3,.4f,hz*2+3));
            if(detail==2)CurtainWall(kit,c,hx,hz,h,glass,frame);
            else
            {
                // Curtain wall: glass shaft with proud floor bands.
                kit.Box(glass,c+Vector3.up*(6+(h-6)*.5f),new Vector3(hx*2-1.2f,h-6,hz*2-1.2f));
                for(float y=6+12.6f;y<h-2;y+=12.6f)
                    kit.Box(frame,c+Vector3.up*y,new Vector3(hx*2,.55f,hz*2));
            }
            // Corner fins, well proud of the glass.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                kit.Box(frame,c+new Vector3(sx*(hx-.2f),6+(h-6)*.5f,sz*(hz-.2f)),new Vector3(1.0f,h-6,1.0f));
            // Crown: setback penthouse, crown ring and a mast on the tallest.
            kit.Box(glassDark,c+Vector3.up*(h+3),new Vector3(hx*1.3f,6,hz*1.3f));
            kit.Box(frame,c+Vector3.up*(h+6.4f),new Vector3(hx*1.5f,.8f,hz*1.5f));
            if(h>140)kit.Column(steel,c+Vector3.up*(h+6.8f),.6f,.15f,h*.18f,6);
            if(detail==2)
            {
                // Glowing crown band under the cornice ring, and red aviation lights.
                kit.Box(pick<.5f?crownTeal:crownAmber,c+Vector3.up*(h+5.35f),new Vector3(hx*1.3f+.5f,.5f,hz*1.3f+.5f));
                if(h>140)kit.Box(aviation,c+Vector3.up*(h+6.8f+h*.18f+.2f),Vector3.one*.6f);
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                    kit.Box(aviation,c+new Vector3(sx*hx*.68f,h+7.02f,sz*hz*.68f),Vector3.one*.45f);
            }
        }

        static void SteppedTower(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,int detail,float pick)
        {
            var body=pick<.5f?stone:pick<.75f?ochre:cream;var trim=pick<.5f?navy:teal;
            float[] tops={h*.5f,h*.78f,h};float[] scale={1,.76f,.52f};float y0=0;
            for(int tier=0;tier<3;tier++)
            {
                float sx=hx*scale[tier],sz=hz*scale[tier],y1=tops[tier];
                // Recessed dark glass core with deep stone piers standing proud of it.
                kit.Box(body,c+Vector3.up*(y0+1.5f),new Vector3(sx*2,3,sz*2));
                kit.Box(glassDark,c+Vector3.up*((y0+3+y1)*.5f),new Vector3(sx*2-Recess*4,y1-y0-3,sz*2-Recess*4));
                float step=detail==2?3.4f:7;
                for(float u=-sx;u<=sx+.01f;u+=Mathf.Max(step,(sx*2)/Mathf.Max(1,Mathf.Round(sx*2/step))))
                    foreach(int f in new[]{-1,1})
                        kit.Box(body,c+new Vector3(Mathf.Clamp(u,-sx+.5f,sx-.5f),(y0+3+y1)*.5f,f*(sz-.5f)),new Vector3(1.0f,y1-y0-3,1.0f+Proud));
                for(float u=-sz;u<=sz+.01f;u+=Mathf.Max(step,(sz*2)/Mathf.Max(1,Mathf.Round(sz*2/step))))
                    foreach(int f in new[]{-1,1})
                        kit.Box(body,c+new Vector3(f*(sx-.5f),(y0+3+y1)*.5f,Mathf.Clamp(u,-sz+.5f,sz-.5f)),new Vector3(1.0f+Proud,y1-y0-3,1.0f));
                kit.Box(trim,c+Vector3.up*(y1+.35f),new Vector3(sx*2+1.2f,.7f,sz*2+1.2f));
                y0=y1+.7f;
            }
            kit.Chamfered(trim,c+Vector3.up*(h+.7f),hx*.35f,hz*.35f,.3f,.3f,0,h*.12f,hx*.1f);
            if(detail==2)kit.Box(aviation,c+Vector3.up*(h+.7f+h*.12f+.25f),Vector3.one*.5f);
        }

        static void RoundTower(PolygonSceneryKit kit,Vector3 c,float r,float h,int detail,float pick)
        {
            var glass=GlassFor(pick);
            kit.Column(stoneDark,c,r+2,r+2,6,8);
            kit.Column(glass,c+Vector3.up*6,r,r*.92f,h-6,8);
            float spacing=detail==2?6:15;
            for(float y=10;y<h-3;y+=spacing)
            {
                float t=(y-6)/(h-6),rr=Mathf.Lerp(r,r*.92f,t);
                kit.Column(cream,c+Vector3.up*y,rr+Proud,rr+Proud,.5f,8);
            }
            kit.Column(cream,c+Vector3.up*h,r*.92f+.8f,r*.5f,4,8);
            kit.Column(steel,c+Vector3.up*(h+4),1,.12f,h*.15f,6);
            if(detail==2)
            {
                kit.Column(pick<.85f?crownAmber:crownTeal,c+Vector3.up*(h-2.2f),r*.92f+.35f,r*.92f+.35f,1.2f,8);
                kit.Box(aviation,c+Vector3.up*(h+4+h*.15f+.2f),Vector3.one*.5f);
            }
        }

        static void Apartment(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,int detail,float pick)
        {
            var body=facades[Mathf.FloorToInt(pick*facades.Length)%facades.Length];
            var band=body==cream||body==stone?navy:cream;
            // Recessed shop glass at street level, then stacked floors of wall and window.
            kit.Sleeve(glassDark,c+Vector3.up*1.5f,new Vector3(hx*2-Recess*2,3,hz*2-Recess*2));
            if(detail==2)WindowBayFacade(kit,c,hx,hz,3,h,3.6f,.46f,body,glassDark);
            else Facade(kit,c,hx,hz,3,h,7.2f,.42f,body,glassDark);
            kit.Box(band,c+Vector3.up*(h+.4f),new Vector3(hx*2+.6f,.8f,hz*2+.6f));
            bool billboard=detail==2&&h<80&&Hash(c.x,c.z,31)<.3f&&RooftopBillboard(kit,c,hx,hz,h,pick);
            if(detail==2)
            {
                var toTrack=ToTrack(c);var awning=teamColors[Mathf.FloorToInt(pick*97)%teamColors.Length];
                if(Mathf.Abs(toTrack.x)>Mathf.Abs(toTrack.z))
                {
                    float sx=Mathf.Sign(toTrack.x);
                    kit.Box(awning,c+new Vector3(sx*(hx+1.1f),3.3f,0),new Vector3(2.2f,.15f,hz*1.6f),Quaternion.Euler(0,0,-sx*12));
                }
                else
                {
                    float sz=Mathf.Sign(toTrack.z);
                    kit.Box(awning,c+new Vector3(0,3.3f,sz*(hz+1.1f)),new Vector3(hx*1.6f,.15f,2.2f),Quaternion.Euler(sz*12,0,0));
                }
                // Roof clutter: water tank and plant room (the tank gives way to a billboard).
                if(!billboard)
                {
                    kit.Column(brick,c+new Vector3(hx*.4f,h+.8f,hz*.3f),1.4f,1.4f,2.6f,6);
                    kit.Chamfered(brick,c+new Vector3(hx*.4f,h+3.4f,hz*.3f),1.5f,1.5f,.2f,.2f,0,1.2f,.5f);
                    kit.Box(steel,c+new Vector3(-hx*.35f,h+1.6f,-hz*.3f),new Vector3(hx*.6f,2.4f,hz*.5f));
                }
            }
        }

        static void SimpleBlock(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,float pick)
        {
            var body=pick<.4f?distant:pick<.7f?stone:distantDark;
            float g0=h*.55f,g1=h*.63f+1;
            kit.Box(body,c+Vector3.up*(g0*.5f),new Vector3(hx*2,g0,hz*2));
            kit.Sleeve(glassDark,c+Vector3.up*((g0+g1)*.5f),new Vector3(hx*2-1.2f,g1-g0,hz*2-1.2f));
            kit.Box(body,c+Vector3.up*((g1+h)*.5f),new Vector3(hx*2,h-g1,hz*2));
        }

        static Vector3 ToTrack(Vector3 c)
        {
            float lap=PolygonCircuit.ProjectLap(c,out _);
            var p=PolygonCircuit.AtDistance(lap*PolygonCircuit.Length);var d=p-c;d.y=0;return d;
        }

        // ------------------------------------------------------------ PC detail v01
        // Everything below is editor-time geometry baked into the existing chunks: no runtime
        // updates, colliders or lights. Lit windows and crowns are emissive materials (bloom on PC).
        static Vector3[] trackSamples;
        static int cranes,billboards,footbridges;
        const int MaxCranes=5;
        const float PierHalf=.28f,CornerPier=.9f;

        static void PrepareTrackSamples()
        {
            int n=Mathf.CeilToInt(PolygonCircuit.Length/15);trackSamples=new Vector3[n];
            for(int i=0;i<n;i++)trackSamples[i]=P(i*PolygonCircuit.Length/n);
        }

        // True when some part of the circuit within range lies in front of this face.
        static bool FaceSeen(Vector3 faceCentre,Vector3 outward,float range)
        {
            float r2=range*range;
            foreach(var p in trackSamples)
            {
                var to=p-faceCentre;to.y=0;
                if(to.sqrMagnitude<r2&&Vector3.Dot(outward,to)>6)return true;
            }
            return false;
        }

        static readonly Vector3[] FaceDirections={Vector3.forward,Vector3.back,Vector3.right,Vector3.left};

        // Face-local point: u along the face (r), y up, d along the outward normal (n).
        static Vector3 FV(Vector3 o,Vector3 r,Vector3 n,float u,float y,float d)=>o+r*u+Vector3.up*y+n*d;

        // Single-sided quad in a face plane at depth d, facing outward.
        static void FaceQuad(PolygonSceneryKit kit,Material m,Vector3 o,Vector3 r,Vector3 n,float u0,float u1,float y0,float y1,float d)
        {
            if(u1-u0<.001f||y1-y0<.001f)return;
            kit.Quad(m,FV(o,r,n,u0,y0,d),FV(o,r,n,u1,y0,d),FV(o,r,n,u1,y1,d),FV(o,r,n,u0,y1,d),FV(o,r,n,(u0+u1)*.5f,(y0+y1)*.5f,d-1));
        }

        // Front plus two side faces of a vertical strip standing from depth -depth to 0. Top,
        // bottom and back are always covered by neighbouring geometry, so they are omitted.
        static void Pier(PolygonSceneryKit kit,Material m,Vector3 o,Vector3 r,Vector3 n,float u0,float u1,float y0,float y1,float depth)
        {
            FaceQuad(kit,m,o,r,n,u0,u1,y0,y1,0);
            float ym=(y0+y1)*.5f;
            kit.Quad(m,FV(o,r,n,u0,y0,0),FV(o,r,n,u0,y1,0),FV(o,r,n,u0,y1,-depth),FV(o,r,n,u0,y0,-depth),FV(o,r,n,u0+1,ym,-depth*.5f));
            kit.Quad(m,FV(o,r,n,u1,y0,0),FV(o,r,n,u1,y1,0),FV(o,r,n,u1,y1,-depth),FV(o,r,n,u1,y0,-depth),FV(o,r,n,u1-1,ym,-depth*.5f));
        }

        // Inward-facing reveal closing the end of a recessed band at u (side = +1 faces -r).
        static void Reveal(PolygonSceneryKit kit,Material m,Vector3 o,Vector3 r,Vector3 n,float u,float side,float y0,float y1,float depth)
        {
            float ym=(y0+y1)*.5f;
            kit.Quad(m,FV(o,r,n,u,y0,0),FV(o,r,n,u,y1,0),FV(o,r,n,u,y1,-depth),FV(o,r,n,u,y0,-depth),FV(o,r,n,u+side,ym,-depth*.5f));
        }

        // Near-track glass towers: the plain shaft becomes a curtain wall of individual panels
        // between proud mullions and thin floor spandrels, with scattered lit offices. Faces the
        // circuit never shows keep one panel per face.
        static void CurtainWall(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,Material glass,Material frame)
        {
            const float floorH=4.2f,column=3.0f,mullionDepth=.35f,mullionHalf=.12f;
            float px=hx-.6f,pz=hz-.6f,top=h-.5f;
            var alt=glass==glassDark?glassBlue:glassDark;
            kit.Box(stoneDark,c+Vector3.up*(h-.25f),new Vector3(px*2,.5f,pz*2));
            int rows=Mathf.Max(1,Mathf.CeilToInt((top-6)/floorH));
            for(int f=0;f<4;f++)
            {
                var n=FaceDirections[f];bool alongZ=Mathf.Abs(n.z)>.5f;
                float half=alongZ?px:pz,plane=alongZ?pz:px;
                var o=c+n*plane;var r=Vector3.Cross(Vector3.up,n);
                if(!FaceSeen(o+Vector3.up*30,n,450)){FaceQuad(kit,glass,o,r,n,-half,half,6,top,0);continue;}
                int cols=Mathf.Max(3,Mathf.RoundToInt(half*2/column));float pitch=half*2/cols;
                for(int row=0;row<rows;row++)
                {
                    float y0=6+row*floorH,y1=Mathf.Min(y0+floorH,top);
                    if(y1-y0<.2f)break;
                    bool litFloor=Hash(c.x+row*3.1f,c.z+f,21)<.07f;
                    for(int col=0;col<cols;col++)
                    {
                        float u0=-half+col*pitch;
                        float v=Hash(o.x+u0*1.7f+row,o.z+y0,17+f);
                        var m=litFloor?(v<.75f?windowCool:glass):v<.08f?windowCool:v<.10f?windowWarm:v<.22f?alt:glass;
                        FaceQuad(kit,m,o,r,n,u0,u0+pitch,y0,y1,0);
                    }
                }
                // Mullions stand proud of the glass; the corner fins already cover the corners.
                for(int col=1;col<cols;col++)
                {
                    float u=-half+col*pitch;
                    Pier(kit,frame,o+n*mullionDepth,r,n,u-mullionHalf,u+mullionHalf,6,top,mullionDepth);
                }
            }
            // Thin spandrel ledge at every floor (proud of the glass, inside the mullion depth).
            for(int row=1;row<rows;row++)
                kit.Box(frame,c+Vector3.up*(6+row*floorH),new Vector3(px*2+.24f,.3f,pz*2+.24f));
        }

        // Near-track apartments: the continuous recessed glass bands are split into individual
        // windows by wall piers, so every floor reads as a row of real windows; some are lit.
        // Wall slabs and recess depth are the accepted anti-flicker construction (no coplanar faces).
        static void WindowBayFacade(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float y0,float y1,float step,float glassShare,Material wall,Material glass)
        {
            var seen=new bool[4];
            for(int f=0;f<4;f++)
            {
                var n=FaceDirections[f];float plane=Mathf.Abs(n.z)>.5f?hz:hx;
                seen[f]=FaceSeen(c+n*plane+Vector3.up*15,n,320);
            }
            bool Seen(Vector3 dir){for(int f=0;f<4;f++)if(Vector3.Dot(FaceDirections[f],dir)>.5f)return seen[f];return false;}
            float y=y0;int floor=0;
            while(y+step<=y1+.01f)
            {
                float g=step*glassShare,b0=y+step-g,b1=y+step;
                kit.Box(wall,c+Vector3.up*(y+(step-g)*.5f),new Vector3(hx*2,step-g,hz*2));
                for(int f=0;f<4;f++)
                {
                    var n=FaceDirections[f];bool alongZ=Mathf.Abs(n.z)>.5f;
                    float half=alongZ?hx:hz,plane=alongZ?hz:hx,edge=half-Recess;
                    var o=c+n*plane;var r=Vector3.Cross(Vector3.up,n);
                    if(!alongZ)
                    {
                        // Close the band ends where the neighbouring face has a solid corner pier.
                        if(Seen(r))Reveal(kit,wall,o,r,n,edge,1,b0,b1,Recess);
                        if(Seen(-r))Reveal(kit,wall,o,r,n,-edge,-1,b0,b1,Recess);
                    }
                    if(!seen[f]){FaceQuad(kit,glass,o,r,n,-edge,edge,b0,b1,-Recess);continue;}
                    float inner=-edge,outer=edge;
                    if(alongZ)
                    {
                        Pier(kit,wall,o,r,n,-half,-half+CornerPier,b0,b1,Recess);
                        Pier(kit,wall,o,r,n,half-CornerPier,half,b0,b1,Recess);
                        inner=-half+CornerPier;outer=half-CornerPier;
                    }
                    int bays=Mathf.Max(2,Mathf.RoundToInt((outer-inner)/3.2f));float pitch=(outer-inner)/bays;
                    for(int k=0;k<bays;k++)
                    {
                        float u0=inner+k*pitch,u1=u0+pitch;
                        if(k>0)Pier(kit,wall,o,r,n,u0-PierHalf,u0+PierHalf,b0,b1,Recess);
                        float p0=k==0?u0:u0+PierHalf,p1=k==bays-1?u1:u1-PierHalf;
                        float v=Hash(o.x+u0*1.3f,o.z+b0+floor,23+f);
                        FaceQuad(kit,v<.13f?windowWarm:v<.17f?windowCool:glass,o,r,n,p0,p1,b0,b1,-Recess);
                    }
                }
                y+=step;floor++;
            }
            if(y1-y>.05f)kit.Box(wall,c+Vector3.up*((y+y1)*.5f),new Vector3(hx*2,y1-y,hz*2));
        }

        // Rooftop advertising hoarding facing the circuit: original abstract graphics only.
        static bool RooftopBillboard(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,float pick)
        {
            var to=ToTrack(c);to.y=0;
            if(to.sqrMagnitude<1)return false;
            var f=to.normalized;var r=Vector3.Cross(Vector3.up,f);var rot=Quaternion.LookRotation(f);
            float w=Mathf.Clamp(Mathf.Min(hx,hz)*1.5f,8,15),bh=w*.42f,roof=h+.8f,legs=2.2f;
            var p=c+f*(Mathf.Min(hx,hz)*.3f);p.y=c.y;
            Vector3 L(float u,float y,float d)=>p+r*u+Vector3.up*y+f*d;
            foreach(float u in new[]{-w*.38f,0,w*.38f})
            {
                kit.Beam(steel,L(u,roof,-.1f),L(u,roof+legs+bh,-.1f),.25f,.25f);
                kit.Beam(steel,L(u,roof,-2.6f),L(u,roof+legs+bh*.75f,-.25f),.16f,.16f);
            }
            float cy=roof+legs+bh*.5f;
            kit.Box(billboardBack,L(0,cy,.2f),new Vector3(w+.5f,bh+.5f,.35f),rot);
            var a=teamColors[Mathf.FloorToInt(pick*61)%teamColors.Length];
            var b=teamColors[(Mathf.FloorToInt(pick*61)+2)%teamColors.Length];
            kit.Box(a,L(0,cy,.45f),new Vector3(w,bh,.15f),rot);
            float front=.62f;
            int design=Mathf.FloorToInt(pick*97)%3;
            if(design==0)
            {
                // Bold chevron with a light band.
                kit.Sheet(b,L(-w*.45f,cy+bh*.42f,front),L(-w*.05f,cy,front),L(-w*.45f,cy-bh*.42f,front));
                kit.Sheet(b,L(-w*.25f,cy+bh*.42f,front),L(w*.15f,cy,front),L(-w*.25f,cy-bh*.42f,front));
                kit.Box(cream,L(w*.28f,cy,.6f),new Vector3(w*.36f,bh*.22f,.12f),rot);
            }
            else if(design==1)
            {
                // Faceted polygon emblem and two stripes.
                for(int i=0;i<6;i++)
                {
                    float t0=i*Mathf.PI/3,t1=(i+1)*Mathf.PI/3,rad=bh*.36f;
                    kit.Sheet(i%2==0?cream:b,L(-w*.28f,cy,front),L(-w*.28f+Mathf.Cos(t0)*rad,cy+Mathf.Sin(t0)*rad,front),
                        L(-w*.28f+Mathf.Cos(t1)*rad,cy+Mathf.Sin(t1)*rad,front));
                }
                kit.Box(cream,L(w*.18f,cy+bh*.16f,.6f),new Vector3(w*.44f,bh*.14f,.12f),rot);
                kit.Box(b,L(w*.12f,cy-bh*.16f,.6f),new Vector3(w*.32f,bh*.14f,.12f),rot);
            }
            else
            {
                // Diagonal split with a checkered race strip.
                kit.Sheet(b,L(w*.5f,cy+bh*.5f,front),L(w*.5f,cy-bh*.5f,front),L(-w*.1f,cy-bh*.5f,front));
                for(int k=0;k<8;k++)
                    kit.Box(k%2==0?cream:checkDark,L(-w*.45f+k*w*.075f,cy+bh*.28f,.6f),new Vector3(w*.075f,bh*.16f,.12f),rot);
            }
            // Lamp bar along the top edge lighting the face.
            kit.Box(steel,L(0,cy+bh*.5f+.35f,.9f),new Vector3(w*.9f,.18f,.8f),rot);
            kit.Box(lamp,L(0,cy+bh*.5f+.22f,1.15f),new Vector3(w*.85f,.1f,.25f),rot);
            billboards++;
            return true;
        }

        // Half-built tower with concrete core, slabs, columns, hoarding and a tower crane.
        static bool ConstructionSite(PolygonSceneryKit kit,Vector3 c,float hx,float hz,float h,float pick)
        {
            float built=Mathf.Clamp(h*.5f,20,80),floorH=4f;
            int floors=Mathf.FloorToInt(built/floorH);built=floors*floorH;
            // The crane stands beside the building, away from the circuit.
            var away=-ToTrack(c);away.y=0;away=away.sqrMagnitude>1?away.normalized:Vector3.forward;
            var side=Mathf.Abs(away.x)>Mathf.Abs(away.z)?new Vector3(Mathf.Sign(away.x),0,0):new Vector3(0,0,Mathf.Sign(away.z));
            var foot=c+side*((Mathf.Abs(side.x)>.5f?hx:hz)+4.5f);
            float craneH=built+24+pick*14,jib=42;
            // The mast stays well off the street; the jib points as far from the circuit as it can
            // (at 60 m+ it may still swing over a road, as real city cranes do).
            if(Offset(foot)<32)return false;
            float yaw=0,best=-1;
            for(int k=0;k<8;k++)
            {
                float a=k*45+pick*20;var dir=Quaternion.Euler(0,a,0)*Vector3.forward;
                float clear=Mathf.Min(Offset(foot+dir*jib),Offset(foot+dir*jib*.5f));
                if(clear>best){best=clear;yaw=a;}
            }
            cranes++;
            kit.Box(stone,c+Vector3.up*((built+6)*.5f),new Vector3(Mathf.Min(hx,hz)*.8f,built+6,Mathf.Min(hx,hz)*.6f));
            for(int i=1;i<=floors;i++)
                kit.Box(i==floors?stoneDark:wall,c+Vector3.up*(i*floorH),new Vector3(hx*2,.35f,hz*2));
            int nx=Mathf.Max(2,Mathf.RoundToInt(hx*2/7)),nz=Mathf.Max(2,Mathf.RoundToInt(hz*2/7));
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)
            {
                if(i>0&&i<nx&&j>0&&j<nz)continue;
                kit.Box(stone,c+new Vector3(-hx+.5f+i*(hx*2-1)/nx,built*.5f,-hz+.5f+j*(hz*2-1)/nz),new Vector3(.7f,built,.7f));
            }
            // Cladding on the lower third, safety netting band at the working floor.
            int clad=Mathf.Max(1,floors/3);
            kit.Box(stoneDark,c+Vector3.up*.25f,new Vector3(hx*2-.6f,.5f,hz*2-.6f));
            for(int i=0;i<clad;i++)
                kit.Sleeve(i%2==0?glassTeal:glassDark,c+Vector3.up*(i*floorH+floorH*.5f+.09f),new Vector3(hx*2-.8f,floorH-.18f,hz*2-.8f));
            kit.Sleeve(hoarding,c+Vector3.up*(built-1.4f),new Vector3(hx*2+.8f,2.4f,hz*2+.8f));
            // Site hoarding at street level.
            foreach(int s in new[]{-1,1})
            {
                kit.Box(hoarding,c+new Vector3(s*(hx+1.8f),1.1f,0),new Vector3(.2f,2.2f,hz*2+3.6f));
                kit.Box(hoarding,c+new Vector3(0,1.1f,s*(hz+1.8f)),new Vector3(hx*2+3.6f,2.2f,.2f));
            }
            TowerCrane(kit,foot,craneH,yaw,jib);
            return true;
        }

        static void TowerCrane(PolygonSceneryKit kit,Vector3 foot,float height,float yaw,float jib)
        {
            // Members are sized to read at a few hundred metres (thin lattices vanished).
            var paint=craneYellow;const float s=1.3f,seg=3f;
            kit.Box(stoneDark,foot+Vector3.up*.6f,new Vector3(5,1.2f,5));
            var corners=new[]{new Vector3(-s,0,-s),new Vector3(s,0,-s),new Vector3(s,0,s),new Vector3(-s,0,s)};
            foreach(var k in corners)kit.Beam(paint,foot+k+Vector3.up*1.2f,foot+k+Vector3.up*height,.42f,.42f);
            int levels=Mathf.FloorToInt((height-1.2f)/seg);
            for(int l=0;l<levels;l++)
            {
                float y0=1.2f+l*seg,y1=y0+seg;
                for(int i=0;i<4;i++)
                {
                    var a=corners[i];var b=corners[(i+1)%4];
                    kit.Beam(paint,foot+a+Vector3.up*y1,foot+b+Vector3.up*y1,.22f,.22f);
                    if((l+i)%2==0)kit.Beam(paint,foot+a+Vector3.up*y0,foot+b+Vector3.up*y1,.17f,.17f);
                    else kit.Beam(paint,foot+b+Vector3.up*y0,foot+a+Vector3.up*y1,.17f,.17f);
                }
            }
            var rot=Quaternion.Euler(0,yaw,0);var dir=rot*Vector3.forward;var across=rot*Vector3.right;
            float top=height;
            kit.Box(steel,foot+Vector3.up*(top+.6f),new Vector3(2.6f,1.2f,2.6f),rot);
            kit.Box(cream,foot+dir*1.6f+across*1.6f+Vector3.up*(top-.4f),new Vector3(1.6f,1.8f,2.0f),rot);
            kit.Box(glassDark,foot+dir*2.45f+across*1.6f+Vector3.up*(top-.2f),new Vector3(1.3f,1.0f,.3f),rot);
            // Jib: triangular truss.
            float jb=top+1.2f,jt=top+3.0f;
            var l0=foot-across*.8f+Vector3.up*jb;var r0=foot+across*.8f+Vector3.up*jb;var t0=foot+Vector3.up*jt;
            kit.Beam(paint,l0,l0+dir*jib,.32f,.32f);kit.Beam(paint,r0,r0+dir*jib,.32f,.32f);kit.Beam(paint,t0,t0+dir*(jib-1),.32f,.32f);
            for(float d=0;d<jib-1;d+=3)
            {
                kit.Beam(paint,l0+dir*d,t0+dir*(d+1.5f),.15f,.15f);kit.Beam(paint,r0+dir*d,t0+dir*(d+1.5f),.15f,.15f);
                kit.Beam(paint,l0+dir*d,r0+dir*(d+3),.12f,.12f);
            }
            // Counter-jib, counterweights, A-frame and pendant ties.
            kit.Box(paint,foot-dir*7+Vector3.up*(jb+.25f),new Vector3(1.8f,.5f,14),rot);
            kit.Box(stone,foot-dir*12.5f+Vector3.up*(jb-1.0f),new Vector3(2.4f,2.4f,2.6f),rot);
            var apex=foot+Vector3.up*(top+9);
            foreach(var k in new[]{-1f,1f})kit.Beam(paint,foot+across*k*.9f+Vector3.up*(top+1.2f),apex,.32f,.32f);
            kit.Beam(steel,apex,t0+dir*(jib*.7f),.1f,.1f);
            kit.Beam(steel,apex,foot-dir*13+Vector3.up*(jb+.5f),.1f,.1f);
            kit.Box(aviation,apex+Vector3.up*.3f,Vector3.one*.5f);
            kit.Box(aviation,t0+dir*(jib-1)+Vector3.up*.3f,Vector3.one*.4f);
            // Trolley, hoist line and a load of steel.
            var trolley=foot+dir*(jib*.55f)+Vector3.up*jb;
            kit.Box(steel,trolley+Vector3.down*.3f,new Vector3(1.4f,.5f,1.4f),rot);
            float drop=Mathf.Min(height*.45f,24);
            kit.Beam(steel,trolley+Vector3.down*.55f,trolley+Vector3.down*drop,.08f,.08f);
            kit.Box(coral,trolley+Vector3.down*(drop+.4f),new Vector3(.5f,.8f,.5f),rot);
            kit.Box(navy,trolley+Vector3.down*(drop+1.3f),new Vector3(.6f,.6f,7),rot);
        }

        // Footbridges over the circuit on long straights: classic street-race framing overhead.
        static void BuildFootbridges(Transform world)
        {
            float length=PolygonCircuit.Length;var picks=new List<float>();
            for(float d=240;d<length-260&&picks.Count<3;d+=10)
            {
                if(picks.Count>0&&d-picks[picks.Count-1]<length*.15f)continue;
                bool straight=true;
                for(float e=-60;e<=200&&straight;e+=10)if(Mathf.Abs(PolygonCircuit.Curvature(d+e))>1/400f)straight=false;
                if(!straight)continue;
                var a=At(d,-23,0);var b=At(d,23,0);var mid=P(d);
                if(Mathf.Abs(mid.x-100)<70&&mid.z>400)continue; // keep clear of the monorail crossing
                if(Reserved(new Vector2(a.x,a.z),9)||Reserved(new Vector2(b.x,b.z),9)||Reserved(new Vector2(mid.x,mid.z),9))continue;
                picks.Add(d);
            }
            for(int i=0;i<picks.Count;i++)Footbridge(world,picks[i],i);
            footbridges=picks.Count;
        }

        static void Footbridge(Transform world,float d,int index)
        {
            var t=Frame(world,"City footbridge "+(index+1),d,0);
            var kit=new PolygonSceneryKit("Footbridge "+(index+1));
            const float tower=22.2f,deckY=7.0f,span=44.4f;
            // Stair/lift towers on the sidewalks, outside the wall and fence.
            foreach(int s in new[]{-1,1})
            {
                kit.Box(stoneDark,new Vector3(s*tower,.4f,0),new Vector3(5.0f,.8f,6.4f));
                kit.Box(teal,new Vector3(s*tower,(deckY+3.65f+.8f)*.5f,0),new Vector3(4.4f,deckY+3.65f-.8f,5.6f));
                kit.Box(glassTeal,new Vector3(s*tower,5.0f,0),new Vector3(3.4f,6.6f,6.0f));
                kit.Box(cream,new Vector3(s*tower,deckY+3.9f,0),new Vector3(5.2f,.5f,6.6f));
            }
            // Deck, glazed side screens, roof and an under-deck truss.
            kit.Box(cream,new Vector3(0,deckY,0),new Vector3(span-4.4f,.8f,4.4f));
            foreach(int z in new[]{-1,1})
            {
                kit.Box(glassBlue,new Vector3(0,deckY+1.75f,z*2.0f),new Vector3(span-4.6f,2.7f,.16f));
                kit.Box(steel,new Vector3(0,deckY+3.2f,z*2.05f),new Vector3(span-4.4f,.22f,.3f));
                for(int k=0;k<=12;k++)
                {
                    float x=-(span-4.4f)*.5f+k*(span-4.4f)/12;
                    kit.Box(steel,new Vector3(x,deckY+1.75f,z*2.12f),new Vector3(.14f,2.7f,.12f));
                    if(k<12)kit.Beam(steel,new Vector3(x,deckY-.4f,z*2.0f),new Vector3(x+(span-4.4f)/12,deckY-1.5f,z*2.0f),.14f,.14f);
                }
                kit.Beam(steel,new Vector3(-(span-4.4f)*.5f,deckY-1.5f,z*2.0f),new Vector3((span-4.4f)*.5f,deckY-1.5f,z*2.0f),.2f,.2f);
            }
            // Bright roof and a glowing strip under the deck so the bridge reads from far down the straight.
            kit.Box(coral,new Vector3(0,deckY+3.5f,0),new Vector3(span+1,.35f,5.4f));
            kit.Box(cream,new Vector3(0,deckY+3.8f,0),new Vector3(span+1.4f,.25f,5.8f));
            foreach(int z in new[]{-1,1})kit.Box(lamp,new Vector3(0,deckY-.47f,z*1.4f),new Vector3(span-6,.12f,.35f));
            // Event banners on both faces, with checkered ends and a team-colour stripe.
            var color=teamColors[(index*2+1)%teamColors.Length];var stripe=teamColors[(index*2+4)%teamColors.Length];
            if(color==navy)color=coral;
            foreach(int z in new[]{-1,1})
            {
                kit.Box(color,new Vector3(0,deckY-.15f,z*2.5f),new Vector3(30,1.7f,.2f));
                kit.Box(stripe,new Vector3(0,deckY-.65f,z*2.66f),new Vector3(28,.35f,.12f));
                kit.Box(cream,new Vector3(0,deckY+.25f,z*2.66f),new Vector3(14,.5f,.12f));
                foreach(int s in new[]{-1,1})for(int row=0;row<2;row++)for(int col=0;col<3;col++)
                    kit.Box((row+col)%2==0?cream:checkDark,new Vector3(s*(15.6f+col*.8f),deckY-.55f+row*.8f,z*2.5f),new Vector3(.8f,.8f,.2f));
            }
            kit.Emit(t);
            Reserve(t,-27,27,-5,5);
        }

        // ------------------------------------------------------------ distant skyline
        static void BuildSkyline(Chunks chunks)
        {
            // A ring of tall, simple silhouettes that fades into the haze, plus a far shore.
            for(int i=0;i<70;i++)
            {
                float a=i*Mathf.PI*2/70+Hash(i,1)*.05f,r=980+Hash(i,2)*260;
                var c=new Vector3(Core.x+Mathf.Cos(a)*r*1.25f,Base,Core.y+Mathf.Sin(a)*r);
                if(c.z>SeaWallZ-20)c.z=1450+Hash(i,3)*250;
                float h=70+Hash(i,4)*190,w=16+Hash(i,5)*22;
                var kit=chunks.At(c);
                kit.Box(Hash(i,6)<.5f?distant:distantDark,c+Vector3.up*(h*.5f),new Vector3(w,h,w*(.7f+Hash(i,7)*.6f)));
                if(Hash(i,8)<.3f)kit.Box(distantDark,c+Vector3.up*(h+h*.08f),new Vector3(w*.6f,h*.16f,w*.5f));
            }
        }

        // ------------------------------------------------------------ late pieces (water, boats)
        public static void AttachHarbor(Transform scenery)
        {
            if(pendingWater!=null)
            {
                var group=new GameObject("Harbor water").transform;group.SetParent(scenery,false);
                pendingWater.Emit(group);pendingWater=null;
                // Plain name (no " - "), so the look pass recognises it as water.
                foreach(Transform child in group)child.name="Harbor water surface";
            }
            if(boatMeshes==null)return;
            var hullMat=cream;var sailMats=new[]{coral,cream,teal};
            foreach(var (pos,yaw,tint) in pendingBoats)
            {
                var boat=new GameObject("Harbor sailboat").transform;boat.SetParent(scenery,false);
                boat.localPosition=pos;boat.localRotation=Quaternion.Euler(0,yaw,0);boat.localScale=Vector3.one*1.2f;
                Part(boat,boatMeshes[0],hullMat);Part(boat,boatMeshes[1],teal);Part(boat,boatMeshes[2],navy);Part(boat,boatMeshes[3],sailMats[tint]);
            }
            pendingBoats.Clear();
        }
        static void Part(Transform parent,Mesh mesh,Material m)
        {
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        }
    }

    // Small shared mesh helpers that do not belong to a particular venue.
    public static class PolygonSceneryMeshesExtra
    {
        public static Mesh SignalLens()
        {
            var v=new List<Vector3>();var t=new List<int>();
            var back=new Vector3(0,0,.24f);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12,b=(i+1)*Mathf.PI*2/12;
                var p=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,0);var q=new Vector3(Mathf.Cos(b)*.5f,Mathf.Sin(b)*.5f,0);
                int n=v.Count;v.AddRange(new[]{Vector3.zero,q,p});t.AddRange(new[]{n,n+1,n+2});
                n=v.Count;v.AddRange(new[]{back,p+back,q+back});t.AddRange(new[]{n,n+1,n+2});
                n=v.Count;v.AddRange(new[]{p,q,q+back,p+back});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=new Mesh{name="City starting signal lens"};mesh.SetVertices(v);mesh.SetTriangles(t,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
