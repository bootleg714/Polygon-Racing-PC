using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Coastal Expressway PC detail v01: fills the empty infield, inland country, beach and sea
    // around the accepted coastal art. Editor-time geometry only (no colliders, lights or update
    // loops), baked per material into 300 m chunks. Runs after PolygonCoastalArt and banking, and
    // reserves the footprint of every existing object, so nothing lands on stands, paddock,
    // cliffs, palms, the marina, bridge or tunnel.
    public static class PolygonCoastalDetail
    {
        const float ChunkSize=300,SeaLevel=-.95f;
        static Material white,cream,terracotta,roofTile,pastelBlue,pastelYellow,pastelPink,pastelMint,windowDark,doorWood,paving,
            scrub,scrubDark,dryGrass,duneSand,palmBark,palmLeaf,palmLeafLight,cypress,coral,teal,navy,ochre,lime,lilac,sky,red,steel,
            skinLight,skinTan,skinDeep,hair,turbine,hullNavy,containerRed,containerBlue,containerGreen,towelA,towelB,flagYellow,
            bougainvillea,oleander,vine,stoneWall,poolBlue;
        static Material[] shirts,skins,pastels,umbrellaPairs,adColors,boardBold;
        static readonly List<Rect> reserved=new List<Rect>();
        static int banks,villages,houses,palms,boats,turbines,umbrellas,villas,fields;
        static Transform animatedRoot;

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit)){kit=new PolygonSceneryKit("Coast detail "+key.x+"_"+key.y){MaxVerticesPerMesh=60000};kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform root)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Coast detail block "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(root,false);pair.Value.Emit(group);
                }
                kits.Clear();
            }
        }

        static Material Mat(string name,Color color)=>PolygonWorld.Paint("CE PC "+name,color);
        static void CreatePalette()
        {
            white=Mat("Whitewash",new Color(.96f,.95f,.92f));
            cream=Mat("Village ivory",new Color(1,.93f,.80f));
            terracotta=Mat("Terracotta roof",new Color(.80f,.38f,.24f));
            roofTile=Mat("Roof tile dark",new Color(.62f,.28f,.20f));
            pastelBlue=Mat("Pastel blue",new Color(.62f,.80f,.92f));
            pastelYellow=Mat("Pastel yellow",new Color(.98f,.86f,.52f));
            pastelPink=Mat("Pastel pink",new Color(.96f,.70f,.66f));
            pastelMint=Mat("Pastel mint",new Color(.66f,.88f,.76f));
            windowDark=Mat("Window dark",new Color(.10f,.14f,.18f));
            doorWood=Mat("Door wood",new Color(.42f,.26f,.16f));
            paving=Mat("Village paving",new Color(.84f,.78f,.66f));
            scrub=Mat("Coastal scrub",new Color(.30f,.50f,.30f));
            scrubDark=Mat("Coastal scrub dark",new Color(.22f,.40f,.26f));
            dryGrass=Mat("Dry grass",new Color(.66f,.68f,.42f));
            duneSand=Mat("Dune sand",new Color(.93f,.80f,.56f));
            palmBark=Mat("Palm trunk",new Color(.55f,.38f,.22f));
            palmLeaf=Mat("Palm frond",new Color(.10f,.48f,.28f));
            palmLeafLight=Mat("Palm frond light",new Color(.30f,.64f,.30f));
            cypress=Mat("Cypress",new Color(.14f,.34f,.22f));
            coral=Mat("Coral",new Color(.98f,.38f,.32f));
            teal=Mat("Teal",new Color(.03f,.48f,.52f));
            navy=Mat("Navy",new Color(.08f,.17f,.30f));
            ochre=Mat("Ochre",new Color(.96f,.68f,.30f));
            lime=Mat("Lime",new Color(.58f,.82f,.26f));
            lilac=Mat("Lilac",new Color(.62f,.50f,.82f));
            sky=Mat("Sky blue",new Color(.36f,.66f,.92f));
            red=Mat("Signal red",new Color(.88f,.14f,.12f));
            steel=Mat("Steel",new Color(.60f,.62f,.65f));
            skinLight=Mat("Skin light",new Color(.96f,.80f,.66f));
            skinTan=Mat("Skin tan",new Color(.80f,.58f,.42f));
            skinDeep=Mat("Skin deep",new Color(.46f,.31f,.21f));
            hair=Mat("Hair",new Color(.12f,.09f,.07f));
            turbine=Mat("Turbine white",new Color(.94f,.95f,.96f));
            hullNavy=Mat("Ship hull",new Color(.12f,.20f,.32f));
            containerRed=Mat("Container red",new Color(.72f,.20f,.16f));
            containerBlue=Mat("Container blue",new Color(.16f,.34f,.62f));
            containerGreen=Mat("Container green",new Color(.20f,.50f,.34f));
            towelA=Mat("Towel stripe",new Color(.98f,.86f,.30f));
            towelB=Mat("Towel blue",new Color(.30f,.56f,.90f));
            flagYellow=Mat("Flag yellow",new Color(1,.82f,.18f));
            bougainvillea=Mat("Bougainvillea",new Color(.90f,.34f,.60f));
            oleander=Mat("Oleander white",new Color(.98f,.94f,.96f));
            vine=Mat("Vine rows",new Color(.38f,.56f,.26f));
            stoneWall=Mat("Field stone wall",new Color(.78f,.72f,.62f));
            poolBlue=Mat("Pool tiles",new Color(.30f,.78f,.90f));
            shirts=new[]{coral,teal,ochre,navy,white,lilac,lime,sky,flagYellow};
            skins=new[]{skinLight,skinTan,skinDeep};
            pastels=new[]{white,white,cream,pastelBlue,pastelYellow,pastelPink,pastelMint,white};
            umbrellaPairs=new[]{coral,white,teal,white,flagYellow,sky,lilac,white,red,white};
            adColors=new[]{white,coral,navy,teal,white,ochre,sky,lilac};
            boardBold=new[]{teal,coral,navy,flagYellow};
        }

        // ------------------------------------------------------------ route and land helpers
        static float A(float d)=>PolygonCircuit.AngleAtDistance(d);
        static Vector3 At(float d,float lateral,float y){float a=A(d);return PolygonWorld.Point(a)+PolygonWorld.Side(a)*lateral+Vector3.up*y;}
        static Vector3 Tan(float d)=>PolygonWorld.Tangent(A(d));
        static float Offset(Vector3 p){PolygonCircuit.ProjectLap(p,out float o);return o;}
        static float Lap(Vector3 p)=>PolygonCircuit.ProjectLap(p,out _)*PolygonCircuit.Length;
        static float Hash(float x,float z,int salt=0)=>Mathf.Abs(Mathf.Sin(x*12.9898f+z*78.233f+salt*37.719f)*43758.5453f)%1f;
        static float Shore(float x)=>708+Mathf.Sin(x*.009f)*7+Mathf.Sin(x*.021f)*3;
        static bool Near(float d,float centre,float range)
        {
            float g=Mathf.Abs(Mathf.Repeat(d-centre,PolygonCircuit.Length));g=Mathf.Min(g,PolygonCircuit.Length-g);return g<range;
        }
        static bool IsLand(float x,float z)=>x>-710&&x<1350&&z>-520&&z<668&&!(x>222&&x<450&&z<114);
        // Sections of the road that already carry special structures close to it.
        static bool TrackBusy(float d,float offset)
        {
            if(offset<42&&Near(d,PolygonCircuit.DistanceAtKnot(2),120))return true;     // estuary bridge
            if(offset<40&&Near(d,PolygonCircuit.DistanceAtKnot(7),70))return true;      // tunnel
            if(offset<40&&d>PolygonBanking.Start-20&&d<PolygonBanking.End+40)return true; // banked sweep, Amber Point
            if(offset<64&&d>PolygonMarinaPit.Entry-80&&d<PolygonMarinaPit.Exit+80)return true; // marina lane
            if(offset<60&&(d<130||d>PolygonCircuit.Length-70))return true;              // start stands, gantry, paddock
            return false;
        }
        static bool Reserved(Vector2 p,float margin)
        {
            foreach(var r in reserved)
                if(p.x>r.xMin-margin&&p.x<r.xMax+margin&&p.y>r.yMin-margin&&p.y<r.yMax+margin)return true;
            return false;
        }
        static void ReserveWorld(float x0,float x1,float z0,float z1){reserved.Add(Rect.MinMaxRect(x0,z0,x1,z1));}
        static bool FreeLand(Vector3 p,float margin,float minOffset)
        {
            if(!IsLand(p.x,p.z))return false;
            float o=Offset(p);if(o<minOffset)return false;
            if(TrackBusy(Lap(p),o))return false;
            return !Reserved(new Vector2(p.x,p.z),margin);
        }
        static void ReserveExisting(Transform world)
        {
            reserved.Clear();
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                var b=renderer.bounds;
                if(Mathf.Max(b.size.x,b.size.z)>220)continue;   // land, sea, road and verge sheets
                reserved.Add(Rect.MinMaxRect(b.min.x-2,b.min.z-2,b.max.x+2,b.max.z+2));
            }
        }

        // ------------------------------------------------------------ entry point
        public static void Build(Transform world)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)return;
            CreatePalette();ReserveExisting(world);
            banks=villages=houses=palms=boats=turbines=umbrellas=villas=fields=0;
            var root=new GameObject("Coastal PC detail").transform;root.SetParent(world,false);
            // Turbine rotors are separate moving objects (not baked into the static chunks).
            animatedRoot=new GameObject("Coastal turbine rotors").transform;animatedRoot.SetParent(world,false);
            var chunks=new Chunks();
            BuildAdBoards(chunks);
            BuildSpectatorBanks(chunks);
            BuildRoadsideTurbines(chunks);
            BuildVillages(chunks);
            BuildBeachLife(chunks);
            BuildWindFarm(chunks);
            BuildFields(chunks);
            BuildVegetation(chunks);
            BuildSeaTraffic(chunks);
            chunks.Emit(root);
            reserved.Clear();
            Debug.Log($"Coastal PC detail v01: {banks} spectator banks, {villages} villages ({houses} houses), {umbrellas} beach umbrellas, "+
                      $"{palms} palms, {villas} villas, {fields} vineyards, {boats} boats and ships, {turbines} wind turbines.");
        }

        // ------------------------------------------------------------ advertising boards
        static void BuildAdBoards(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=d0+step,dm=(d0+d1)*.5f;
                if(TrackBusy(dm,20))continue;
                if(Mathf.Abs(PolygonCircuit.Curvature(dm))>1/260f)continue;
                var kit=chunks.At(At(dm,0,0));
                foreach(float side in new[]{-1f,1f})
                {
                    int block=i/5+(side>0?3:0);var board=adColors[block%adColors.Length];
                    Slab(kit,side,d0,d1,18.75f,18.9f,0,1.05f,board);
                    if(block%3!=1)Slab(kit,side,d0,d1,18.7f,18.75f,.38f,.72f,board==white?boardBold[block%boardBold.Length]:white);
                }
            }
        }
        static void Slab(PolygonSceneryKit kit,float side,float d0,float d1,float a,float b,float y0,float y1,Material m)
        {
            float l0=side>0?a:-b,l1=side>0?b:-a;
            kit.Prism(m,new[]{At(d0,l0,y0),At(d0,l1,y0),At(d1,l1,y0),At(d1,l0,y0)},
                        new[]{At(d0,l0,y1),At(d0,l1,y1),At(d1,l1,y1),At(d1,l0,y1)});
        }

        // ------------------------------------------------------------ spectator banks with bunting
        static void BuildSpectatorBanks(Chunks chunks)
        {
            float length=PolygonCircuit.Length;var picks=new List<(float d,float side)>();
            for(float d=140;d<length-120&&picks.Count<6;d+=10)
            {
                bool spaced=true;foreach(var pk in picks)if(Near(d,pk.d,320))spaced=false;
                if(!spaced)continue;
                bool straight=true;for(float e=-50;e<=50;e+=10)if(Mathf.Abs(PolygonCircuit.Curvature(d+e))>1/260f)straight=false;
                if(!straight)continue;
                foreach(float side in new[]{1f,-1f})
                {
                    bool ok=true;
                    for(float e=-45;e<=45&&ok;e+=9)
                        foreach(float l in new[]{21.5f,26f,30.5f})
                        {
                            var p=At(d+e,side*l,0);
                            if(!IsLand(p.x,p.z)||Reserved(new Vector2(p.x,p.z),1.5f)||TrackBusy(Lap(p),20)||Offset(p)<20){ok=false;break;}
                        }
                    if(!ok)continue;
                    picks.Add((d,side));break;
                }
            }
            int theme=0;
            foreach(var (d,side) in picks)SpectatorBank(chunks,d-45,d+45,side,theme++);
            banks=picks.Count;
        }

        static void SpectatorBank(Chunks chunks,float d0,float d1,float side,int theme)
        {
            const int rows=6;const float first=21.4f,depth=1.3f,rise=.45f,seg=4.5f;float back=first+rows*depth;
            for(float d=d0;d<d1-.01f;d+=seg)
            {
                float e=Mathf.Min(d+seg,d1);var kit=chunks.At(At((d+e)*.5f,side*25,0));
                for(int r=0;r<rows;r++)
                {
                    float a=first+r*depth;
                    Slab(kit,side,d,e,a,a+depth,-.05f,.2f+r*rise,r%2==0?cream:white);
                    Slab(kit,side,d,e,a+.05f,a+.45f,.2f+r*rise,.62f+r*rise,teal);     // bench
                }
                Slab(kit,side,d,e,back,back+.3f,-.05f,.2f+rows*rise+1.0f,navy);       // back screen
            }
            for(float d=d0+.8f;d<d1-.8f;d+=.95f)
                for(int r=0;r<rows;r++)
                {
                    float h=Hash(d*1.37f,r+theme*13,51);if(h<.25f)continue;
                    float dd=d+(Hash(d,r,53)-.5f)*.4f;
                    var p=At(dd,side*(first+r*depth+.85f),.2f+r*rise);
                    var kit=chunks.At(p);var facing=-PolygonWorld.Side(A(dd))*side;
                    var shirt=Hash(d,r,54)<.3f?boardBold[theme%boardBold.Length]:shirts[Mathf.FloorToInt(Hash(d,r,55)*shirts.Length)%shirts.Length];
                    kit.PersonLite(p+Vector3.up*.42f,facing,.95f+h*.1f,shirt,skins[Mathf.FloorToInt(Hash(d,r,57)*3)%3],h>.86f?white:hair,h>.94f);
                    if(Hash(d*.7f,r,58)<.05f)Umbrella(kit,p+Vector3.up*.45f,2.6f,1.3f,Mathf.FloorToInt(Hash(d,r,59)*5));
                }
            // Bunting: poles along the back with strings of pennants.
            Vector3? prev=null;int k=0;
            for(float d=d0;d<=d1+.01f;d+=10,k++)
            {
                var foot=At(d,side*(back+.9f),0);var kit=chunks.At(foot);
                kit.Column(white,foot+Vector3.down*.1f,.1f,.07f,6.2f,6);
                if(k%3==0)
                {
                    var top=foot+Vector3.up*6.1f;var t=Tan(d);
                    kit.SheetQuad(boardBold[(k/3+theme)%boardBold.Length],top,top+t*2.2f,top+Vector3.down*1.3f+t*2.2f,top+Vector3.down*1.3f);
                }
                if(prev.HasValue)
                {
                    var a=prev.Value+Vector3.up*5.4f;var b=foot+Vector3.up*5.4f;
                    kit.Beam(navy,a,b,.03f,.03f);
                    int count=Mathf.Max(3,Mathf.RoundToInt(Vector3.Distance(a,b)/.75f));var dir=(b-a).normalized;
                    for(int i=0;i<count;i++)
                    {
                        float u=(i+.5f)/count;var q=Vector3.Lerp(a,b,u)+Vector3.down*Mathf.Sin(u*Mathf.PI)*.5f;
                        var c=umbrellaPairs[(i+k)%umbrellaPairs.Length];
                        kit.Sheet(c,q-dir*.28f,q+dir*.28f,q+Vector3.down*.6f);
                    }
                }
                prev=foot;
            }
            var c0=At(d0,side*first,0);var c1=At(d1,side*(back+2),0);var c2=At((d0+d1)*.5f,side*(back+2),0);var c3=At((d0+d1)*.5f,side*first,0);
            ReserveWorld(Mathf.Min(Mathf.Min(c0.x,c1.x),Mathf.Min(c2.x,c3.x))-2,Mathf.Max(Mathf.Max(c0.x,c1.x),Mathf.Max(c2.x,c3.x))+2,
                         Mathf.Min(Mathf.Min(c0.z,c1.z),Mathf.Min(c2.z,c3.z))-2,Mathf.Max(Mathf.Max(c0.z,c1.z),Mathf.Max(c2.z,c3.z))+2);
        }

        // Striped beach umbrella: pole plus an eight-panel canopy in two colours.
        static void Umbrella(PolygonSceneryKit kit,Vector3 foot,float height,float radius,int pair)
        {
            var a=umbrellaPairs[(pair*2)%umbrellaPairs.Length];var b=umbrellaPairs[(pair*2+1)%umbrellaPairs.Length];
            var top=foot+Vector3.up*height;
            kit.Beam(steel,foot,top+Vector3.up*.1f,.06f,.06f);
            for(int i=0;i<8;i++)
            {
                float a0=i*Mathf.PI/4,a1=(i+1)*Mathf.PI/4;
                var p0=top+new Vector3(Mathf.Cos(a0)*radius,-.42f,Mathf.Sin(a0)*radius);
                var p1=top+new Vector3(Mathf.Cos(a1)*radius,-.42f,Mathf.Sin(a1)*radius);
                kit.Sheet(i%2==0?a:b,top+Vector3.up*.12f,p0,p1);
            }
        }

        // ------------------------------------------------------------ whitewashed villages
        static void BuildVillages(Chunks chunks)
        {
            var centres=new List<Vector3>();
            for(float x=-640;x<=1300&&centres.Count<5;x+=40)
                for(float z=-480;z<=600&&centres.Count<5;z+=40)
                {
                    if(Hash(x,z,61)>.2f)continue;
                    var c=new Vector3(x,0,z);float o=Offset(c);
                    if(o<80||o>320||!FreeLand(c,52,0))continue;
                    bool spaced=true;foreach(var v in centres)if((v-c).sqrMagnitude<330*330)spaced=false;
                    if(!spaced)continue;
                    // The whole village footprint must be land, clear of the circuit.
                    bool ok=true;
                    for(int i=0;i<8&&ok;i++){float a=i*Mathf.PI/4;var q=c+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*58;if(!IsLand(q.x,q.z)||Offset(q)<45)ok=false;}
                    if(ok)centres.Add(c);
                }
            foreach(var c in centres)Village(chunks,c);
            villages=centres.Count;
        }

        static void Village(Chunks chunks,Vector3 c)
        {
            var kit=chunks.At(c);
            // Central square with a church and bell tower.
            kit.Box(paving,c+Vector3.up*.05f,new Vector3(26,.3f,26));
            var church=c+new Vector3(-6,0,7);
            kit.Box(white,church+new Vector3(0,4,0),new Vector3(9,8,15));
            kit.Prism(terracotta,new[]{church+new Vector3(-5,8,-8),church+new Vector3(5,8,-8),church+new Vector3(0,11,-8)},
                                 new[]{church+new Vector3(-5,8,8),church+new Vector3(5,8,8),church+new Vector3(0,11,8)});
            var tower=church+new Vector3(6.5f,0,4.5f);
            kit.Box(white,tower+new Vector3(0,8,0),new Vector3(4,16,4));
            foreach(var n in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
                kit.Box(windowDark,tower+Vector3.up*13.6f+n*2.04f,new Vector3(Mathf.Abs(n.x)>.5f?.12f:1.2f,1.8f,Mathf.Abs(n.z)>.5f?.12f:1.2f));
            kit.Chamfered(terracotta,tower+Vector3.up*16,2.4f,2.4f,.1f,.1f,0,3.6f,.2f);
            kit.Box(doorWood,church+new Vector3(0,1.5f,-7.56f),new Vector3(2.2f,3,.12f));
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI/2+.4f;var p=c+new Vector3(Mathf.Cos(a)*10,0,Mathf.Sin(a)*10);
                Palm(kit,p,1.0f+Hash(i,c.x,62)*.3f,i);
            }
            int count=0;
            for(float gx=-52;gx<=52;gx+=13)
                for(float gz=-52;gz<=52;gz+=13)
                {
                    if(Mathf.Abs(gx)<16&&Mathf.Abs(gz)<16)continue;          // the square
                    if(gx*gx+gz*gz>58*58)continue;
                    float h=Hash(c.x+gx,c.z+gz,63);if(h<.18f)continue;
                    var p=c+new Vector3(gx+(Hash(gx,c.z,64)-.5f)*3,0,gz+(Hash(gz,c.x,65)-.5f)*3);
                    if(Offset(p)<40||!IsLand(p.x,p.z)||Reserved(new Vector2(p.x,p.z),3))continue;
                    var k2=chunks.At(p);
                    if(h<.26f){Cypress(k2,p,7+h*20);Cypress(k2,p+new Vector3(2.5f,0,1.5f),6+h*14);continue;}
                    float w=6+Hash(gx,gz,66)*3,dp=5+Hash(gz,gx,67)*3,ht=h>.75f?7.6f:4.6f;
                    House(k2,p,w,dp,ht,pastels[Mathf.FloorToInt(Hash(gx,gz,68)*pastels.Length)%pastels.Length],Hash(gx,gz,69)<.55f);
                    count++;
                }
            houses+=count;
            ReserveWorld(c.x-62,c.x+62,c.z-62,c.z+62);
        }

        static void House(PolygonSceneryKit kit,Vector3 c,float w,float d,float h,Material wall,bool tiledRoof)
        {
            kit.Box(wall,c+Vector3.up*(h*.5f-.05f),new Vector3(w,h+.1f,d));
            int floors=h>6?2:1;
            foreach(var n in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
            {
                bool alongX=Mathf.Abs(n.z)>.5f;float len=alongX?w:d,half=alongX?d*.5f:w*.5f;
                var along=alongX?Vector3.right:Vector3.forward;
                int cols=Mathf.Max(1,Mathf.FloorToInt((len-1.2f)/2.4f));
                for(int f=0;f<floors;f++)
                    for(int k=0;k<cols;k++)
                    {
                        float u=-len*.5f+(k+.5f)*len/cols;float y=1.7f+f*3;
                        bool door=f==0&&k==cols/2&&n==Vector3.back;
                        var centre=c+along*u+n*(half+.05f)+Vector3.up*(door?1.1f:y);
                        var size=alongX?new Vector3(door?1.1f:.9f,door?2.2f:1.1f,.14f):new Vector3(.14f,1.1f,.9f);
                        kit.Box(door?doorWood:windowDark,centre,size);
                    }
            }
            if(tiledRoof)kit.Chamfered(terracotta,c+Vector3.up*(h-.05f),w*.5f+.45f,d*.5f+.45f,w*.18f,d*.12f,0,2.0f,.1f);
            else
            {
                kit.Box(white,c+Vector3.up*(h+.25f),new Vector3(w+.2f,.5f,d+.2f));
                if(Hash(c.x,c.z,70)<.4f)
                {
                    // Roof terrace pergola with a striped awning.
                    kit.Box(roofTile,c+new Vector3(-w*.2f,h+1.5f,0),new Vector3(w*.4f,2.0f,d*.5f));
                    kit.Box(Hash(c.z,c.x,71)<.5f?teal:coral,c+new Vector3(w*.2f,h+2.6f,0),new Vector3(w*.45f,.12f,d*.6f));
                }
            }
        }

        static void Cypress(PolygonSceneryKit kit,Vector3 p,float h)
        {
            kit.Column(palmBark,p+Vector3.down*.1f,.14f,.1f,h*.2f,5);
            kit.Chamfered(cypress,p+Vector3.up*h*.15f,.75f,.75f,.12f,.12f,0,h*.85f,.25f);
        }

        // Low-poly palm built in the kit: curved trunk, drooping fronds, coconuts.
        static void Palm(PolygonSceneryKit kit,Vector3 p,float s,int seed)
        {
            float lean=Hash(seed,p.x,72)*Mathf.PI*2;var dir=new Vector3(Mathf.Cos(lean),0,Mathf.Sin(lean));
            var prev=p+Vector3.down*.2f;const int segs=5;float height=8.5f*s;
            for(int i=1;i<=segs;i++)
            {
                float t=i/(float)segs;var q=p+Vector3.up*height*t+dir*(t*t*1.8f*s);
                kit.Beam(palmBark,prev,q,(.42f-.12f*t)*s,(.42f-.12f*t)*s);prev=q;
            }
            var top=prev;
            for(int f=0;f<8;f++)
            {
                float a=f*Mathf.PI/4+Hash(seed,f,73)*.3f;var fd=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,fd);
                var mid=top+fd*2.4f*s+Vector3.up*.5f*s;var tip=top+fd*4.4f*s+Vector3.down*1.1f*s;
                var m=f%2==0?palmLeaf:palmLeafLight;
                kit.Sheet(m,top,mid+side*.7f*s,mid-side*.7f*s);
                kit.Sheet(m,mid+side*.7f*s,tip,mid-side*.7f*s);
            }
            kit.Box(doorWood,top+Vector3.down*.4f*s,new Vector3(.7f,.5f,.7f)*s);
            palms++;
        }

        // ------------------------------------------------------------ beach life
        static float BeachY(float x,float z)=>Mathf.Lerp(-.045f,-.95f,Mathf.InverseLerp(674,Shore(x),z));
        static void BuildBeachLife(Chunks chunks)
        {
            int index=0;
            for(float x=-640;x<=1330;x+=6.5f,index++)
            {
                float shore=Shore(x);
                float z=679+Hash(x,1,80)*Mathf.Max(0,shore-692);
                var p=new Vector3(x,0,z);p.y=BeachY(x,z);
                if(Offset(p)<23||Reserved(new Vector2(p.x,p.z),1.5f)||TrackBusy(Lap(p),30))continue;
                var kit=chunks.At(p);float h=Hash(x,2,81);
                if(index%36==0)
                {
                    // Lifeguard tower near the water's edge.
                    var t=new Vector3(x,0,shore-7);t.y=BeachY(x,t.z);
                    foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                        kit.Beam(white,t+new Vector3(sx*1.1f,-.3f,sz*1.1f),t+new Vector3(sx*.8f,3.0f,sz*.8f),.16f,.16f);
                    kit.Box(red,t+Vector3.up*3.9f,new Vector3(2.4f,1.8f,2.4f));
                    kit.Box(windowDark,t+new Vector3(0,4.1f,1.22f),new Vector3(1.8f,.7f,.1f));
                    kit.Chamfered(white,t+Vector3.up*4.8f,1.7f,1.7f,.2f,.2f,0,.8f,.1f);
                    kit.Beam(steel,t+new Vector3(1.3f,4.8f,0),t+new Vector3(1.3f,7.6f,0),.06f,.06f);
                    kit.SheetQuad(flagYellow,t+new Vector3(1.3f,7.5f,0),t+new Vector3(2.8f,7.5f,0),t+new Vector3(2.8f,6.6f,0),t+new Vector3(1.3f,6.6f,0));
                    continue;
                }
                if(index%36>=10&&index%36<14)
                {
                    // A short row of beach huts at the back of the sand.
                    var b=new Vector3(x,0,678.5f);b.y=BeachY(x,b.z);
                    if(Offset(b)<23||Reserved(new Vector2(b.x,b.z),1))continue;
                    var m=pastels[(index+3)%pastels.Length];
                    kit.Box(m,b+Vector3.up*1.2f,new Vector3(3,2.6f,2.6f));
                    kit.Prism(index%2==0?teal:coral,new[]{b+new Vector3(-1.75f,2.5f,-1.55f),b+new Vector3(1.75f,2.5f,-1.55f),b+new Vector3(0,3.6f,-1.55f)},
                                                   new[]{b+new Vector3(-1.75f,2.5f,1.55f),b+new Vector3(1.75f,2.5f,1.55f),b+new Vector3(0,3.6f,1.55f)});
                    kit.Box(doorWood,b+new Vector3(0,1.0f,1.32f),new Vector3(1.0f,1.8f,.1f));
                    continue;
                }
                if(h<.45f)
                {
                    Umbrella(kit,p,2.4f,1.4f,Mathf.FloorToInt(Hash(x,3,82)*5));umbrellas++;
                    var towel=p+new Vector3(1.6f,.03f,.4f);
                    kit.Box(Hash(x,4,83)<.5f?towelA:towelB,towel,new Vector3(.9f,.04f,1.9f));
                    if(Hash(x,5,84)<.6f)
                    {
                        // Sunbather lying on the towel.
                        var skin=skins[Mathf.FloorToInt(Hash(x,6,85)*3)%3];
                        kit.Box(shirts[Mathf.FloorToInt(Hash(x,7,86)*shirts.Length)%shirts.Length],towel+new Vector3(0,.12f,-.1f),new Vector3(.42f,.22f,.8f));
                        kit.Box(skin,towel+new Vector3(0,.1f,.6f),new Vector3(.3f,.18f,.7f));
                        kit.Box(skin,towel+new Vector3(0,.16f,-.68f),new Vector3(.24f,.24f,.26f));
                    }
                }
                else if(h<.55f)
                {
                    // People standing at the water's edge.
                    var q=new Vector3(x,0,shore-3);q.y=BeachY(x,q.z);
                    var kq=chunks.At(q);var facing=Vector3.forward;
                    kq.Box(navy,q+Vector3.up*.35f,new Vector3(.3f,.7f,.2f));
                    kq.PersonLite(q+Vector3.up*.6f,facing,.95f,shirts[Mathf.FloorToInt(Hash(x,8,87)*shirts.Length)%shirts.Length],skins[Mathf.FloorToInt(Hash(x,9,88)*3)%3],hair,false);
                }
            }
        }

        // ------------------------------------------------------------ wind farm on the southern plain
        static void BuildWindFarm(Chunks chunks)
        {
            for(int i=0;i<9;i++)
            {
                var p=new Vector3(-620+i*230+Hash(i,1,90)*40,0,-455-Hash(i,2,91)*45);
                if(!FreeLand(p,12,90))continue;
                Turbine(chunks,p,62,24,Vector3.forward,i);
            }
        }

        // Turbines close to the circuit (48-68 m from the racing line), rotors facing the road.
        static void BuildRoadsideTurbines(Chunks chunks)
        {
            float length=PolygonCircuit.Length;var laps=new List<float>();
            for(float d=150;d<length-100&&laps.Count<7;d+=15)
            {
                bool spaced=true;foreach(var l in laps)if(Near(d,l,300))spaced=false;
                if(!spaced)continue;
                foreach(float side in new[]{-1f,1f})
                {
                    var p=At(d,side*(48+Hash(d,side,140)*20),0);
                    if(!FreeLand(p,10,42))continue;
                    bool ok=true;
                    foreach(var q in new[]{p+Vector3.right*9,p-Vector3.right*9,p+Vector3.forward*9,p-Vector3.forward*9})
                        if(!FreeLand(q,3,30))ok=false;
                    if(!ok)continue;
                    var facing=At(d,0,0)-p;facing.y=0;facing.Normalize();
                    Turbine(chunks,p,46,19,facing,laps.Count+20);
                    ReserveWorld(p.x-11,p.x+11,p.z-11,p.z+11);
                    laps.Add(d);break;
                }
            }
        }

        // Static tower and nacelle in the chunks; the rotor is its own object with a spinner.
        static void Turbine(Chunks chunks,Vector3 p,float hub,float blade,Vector3 facing,int seed)
        {
            var kit=chunks.At(p);var look=Quaternion.LookRotation(facing);float s=hub/62;
            kit.Column(turbine,p+Vector3.down*.2f,1.7f*s,.95f*s,hub,10);
            kit.Box(turbine,p+Vector3.up*(hub+.6f*s)-facing*.6f*s,new Vector3(2.2f,2.4f,5.2f)*s,look);
            kit.Box(red,p+Vector3.up*(hub+1.95f*s)-facing*2.4f*s,new Vector3(.35f,.3f,.35f),look);
            var rotor=new GameObject("Wind turbine rotor").transform;rotor.SetParent(animatedRoot,false);
            rotor.position=p+Vector3.up*(hub+.6f*s)+facing*2.4f*s;rotor.rotation=look;
            var rk=new PolygonSceneryKit("Turbine rotor");
            rk.Box(turbine,Vector3.zero,new Vector3(1.5f,1.5f,1.6f)*s);
            rk.Box(turbine,new Vector3(0,0,.95f*s),new Vector3(.9f,.9f,.5f)*s);
            for(int b=0;b<3;b++)
            {
                float a=b*120*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                rk.Box(turbine,dir*(blade*.5f+.7f*s)+Vector3.forward*.2f,new Vector3(1.1f*s,blade,.32f*s),Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90));
            }
            rk.Emit(rotor);
            rotor.Rotate(0,0,Hash(seed,p.x,141)*120,Space.Self);
            var spinner=rotor.gameObject.AddComponent<PolygonWindTurbine>();
            spinner.DegreesPerSecond=16+Hash(seed,p.z,142)*12;
            turbines++;
        }

        // ------------------------------------------------------------ scrub, dunes and palm groves
        static void BuildVegetation(Chunks chunks)
        {
            for(float x=-700;x<=1340;x+=15)
                for(float z=-510;z<=660;z+=15)
                {
                    var p=new Vector3(x+(Hash(x,z,100)-.5f)*11,0,z+(Hash(x,z,101)-.5f)*11);
                    float h=Hash(x,z,102);
                    float o=Offset(p);
                    // Denser close to the circuit, where it is seen.
                    float keep=Mathf.Lerp(.55f,.30f,Mathf.InverseLerp(30,260,o));
                    if(h>keep)continue;
                    if(!FreeLand(p,1.5f,25))continue;
                    var kit=chunks.At(p);
                    if(h<.016f)
                    {
                        // Rounded grassy or sandy mound.
                        float r=4+Hash(x,z,103)*6,ht=1.2f+Hash(z,x,104)*2.2f;
                        if(o<25+r)continue;
                        var m=p.z>560||Hash(x,z,105)<.3f?duneSand:dryGrass;
                        kit.Chamfered(m,p+Vector3.down*.3f,r,r*.85f,r*.55f,r*.5f,0,ht*.6f,r*.35f);
                        kit.Chamfered(m,p+Vector3.up*(ht*.6f-.3f),r*.55f,r*.5f,r*.2f,r*.18f,0,ht*.4f,r*.15f);
                        continue;
                    }
                    if(h<.06f)
                    {
                        int n=2+Mathf.FloorToInt(Hash(z,x,106)*4);
                        for(int i=0;i<n;i++)
                        {
                            var q=p+new Vector3((Hash(i,x,107)-.5f)*8,0,(Hash(i,z,108)-.5f)*8);
                            if(FreeLand(q,1,25))Palm(chunks.At(q),q,.85f+Hash(i,x,109)*.45f,i+(int)x);
                        }
                        continue;
                    }
                    if(h<.068f&&o>55&&!Reserved(new Vector2(p.x,p.z),14))
                    {
                        Villa(kit,p,Hash(x,z,111));
                        ReserveWorld(p.x-14,p.x+14,p.z-14,p.z+14);
                        continue;
                    }
                    float s=1.0f+Hash(x,z,110)*1.4f;
                    var bush=h<.10f?bougainvillea:h<.12f?oleander:h<.24f?scrub:scrubDark;
                    kit.Chamfered(bush,p+Vector3.down*.1f,.95f*s,.85f*s,.4f*s,.35f*s,0,1.25f*s,.3f*s);
                    if(h<.2f)kit.Chamfered(bush,p+new Vector3(.9f*s,-.1f,.5f*s),.65f*s,.6f*s,.25f*s,.22f*s,0,.9f*s,.2f*s);
                }
        }

        // A whitewashed villa with a garden wall, pool and palms.
        static void Villa(PolygonSceneryKit kit,Vector3 c,float pick)
        {
            var wall=pastels[Mathf.FloorToInt(pick*pastels.Length)%pastels.Length];
            House(kit,c,9+pick*3,7+pick*2,pick>.5f?7.6f:4.6f,wall,pick<.6f);
            var pool=c+new Vector3(0,0,-8.5f);
            kit.Box(white,pool+Vector3.up*.12f,new Vector3(8,.3f,4.6f));
            kit.Box(poolBlue,pool+Vector3.up*.28f,new Vector3(6.8f,.06f,3.4f));
            foreach(int sx in new[]{-1,1})kit.Box(stoneWall,c+new Vector3(sx*10.5f,.55f,-1.5f),new Vector3(.5f,1.1f,22));
            kit.Box(stoneWall,c+new Vector3(0,.55f,9.5f),new Vector3(21.5f,1.1f,.5f));
            Palm(kit,c+new Vector3(-7,0,-11),1.0f,(int)(pick*100));
            Palm(kit,c+new Vector3(7.5f,0,-10),.9f,(int)(pick*100)+3);
            kit.Chamfered(bougainvillea,c+new Vector3(5,0,6.5f),1.4f,1.2f,.6f,.5f,0,1.8f,.4f);
            villas++;
        }

        // Vineyards in walled plots on the southern plain and in the infield.
        static void BuildFields(Chunks chunks)
        {
            var centres=new List<Vector3>();
            for(float x=-620;x<=1280&&centres.Count<8;x+=38)
                for(float z=-470;z<=560&&centres.Count<8;z+=38)
                {
                    if(Hash(x,z,130)>.12f)continue;
                    var c=new Vector3(x,0,z);float o=Offset(c);
                    if(o<60||o>360||!FreeLand(c,36,0))continue;
                    bool spaced=true;foreach(var v in centres)if((v-c).sqrMagnitude<200*200)spaced=false;
                    if(!spaced)continue;
                    bool ok=true;
                    foreach(var q in new[]{c+new Vector3(34,0,26),c+new Vector3(-34,0,26),c+new Vector3(34,0,-26),c+new Vector3(-34,0,-26)})
                        if(!IsLand(q.x,q.z)||Offset(q)<40)ok=false;
                    if(ok)centres.Add(c);
                }
            foreach(var c in centres)
            {
                bool alongX=Hash(c.x,c.z,131)<.5f;
                for(int row=-11;row<=11;row++)
                    for(float t=-28;t<28;t+=4.2f)
                    {
                        var p=c+(alongX?new Vector3(t+2.1f,0,row*2.3f):new Vector3(row*2.3f,0,t+2.1f));
                        if(Offset(p)<40)continue;
                        chunks.At(p).Box(vine,p+Vector3.up*.45f,alongX?new Vector3(4.1f,1.0f,.9f):new Vector3(.9f,1.0f,4.1f));
                    }
                var kit=chunks.At(c);
                foreach(int s in new[]{-1,1})
                {
                    kit.Box(stoneWall,c+new Vector3(s*31,.5f,0),new Vector3(.6f,1.0f,57));
                    kit.Box(stoneWall,c+new Vector3(0,.5f,s*28),new Vector3(62.6f,1.0f,.6f));
                }
                Cypress(kit,c+new Vector3(-33,0,-30),9);Cypress(kit,c+new Vector3(-30,0,-30),7.5f);
                ReserveWorld(c.x-34,c.x+34,c.z-31,c.z+31);
                fields++;
            }
        }

        // ------------------------------------------------------------ boats, windsurfers and a ship
        static void BuildSeaTraffic(Chunks chunks)
        {
            for(int i=0;i<26;i++)
            {
                var p=new Vector3(-700+Hash(i,1,120)*2100,SeaLevel,780+Hash(i,2,121)*520);
                if(Reserved(new Vector2(p.x,p.z),12))continue;
                float yaw=Hash(i,3,122)*360;var kit=chunks.At(p);
                if(i%4==3)MotorBoat(kit,p,yaw);else Sailboat(kit,p,yaw,.9f+Hash(i,4,123)*.6f,umbrellaPairs[(i*3)%umbrellaPairs.Length]);
                boats++;
            }
            for(int i=0;i<10;i++)
            {
                float x=-560+Hash(i,5,124)*1800;var p=new Vector3(x,SeaLevel,Shore(x)+18+Hash(i,6,125)*50);
                if(Reserved(new Vector2(p.x,p.z),6))continue;
                var kit=chunks.At(p);float yaw=Hash(i,7,126)*360;var rot=Quaternion.Euler(0,yaw,0);
                kit.Box(white,p+Vector3.up*.08f,new Vector3(.6f,.12f,2.6f),rot);
                var mast=p+rot*new Vector3(0,.1f,.2f);
                kit.Sheet(umbrellaPairs[(i*2)%umbrellaPairs.Length],mast,mast+Vector3.up*4.2f,mast+rot*new Vector3(0,.4f,-1.9f));
                kit.Box(navy,p+rot*new Vector3(.45f,1.0f,-.2f),new Vector3(.3f,1.6f,.25f),rot);
                boats++;
            }
            // Container ship heading along the horizon.
            var ship=new Vector3(1080,SeaLevel,1240);
            if(!Reserved(new Vector2(ship.x,ship.z),60))
            {
                var kit=chunks.At(ship);var rot=Quaternion.Euler(0,82,0);
                Vector3 L(float x,float y,float z)=>ship+rot*new Vector3(x,y,z);
                kit.Box(hullNavy,L(0,1.5f,0),new Vector3(20,6,130),rot);
                kit.Prism(hullNavy,new[]{L(-10,-1.5f,65),L(10,-1.5f,65),L(10,4.5f,65),L(-10,4.5f,65)},new[]{L(-2,-1.5f,78),L(2,-1.5f,78),L(2,4.5f,80),L(-2,4.5f,80)});
                kit.Box(red,L(0,-.9f,0),new Vector3(20.2f,.8f,130.2f),rot);
                kit.Box(white,L(0,10,-52),new Vector3(16,13,12),rot);
                kit.Box(windowDark,L(0,15,-45.9f),new Vector3(14,1.4f,.2f),rot);
                kit.Column(coral,L(4,16.5f,-56),1.4f,1.2f,6,8);
                var stacks=new[]{containerRed,containerBlue,containerGreen,ochre,white};
                for(int bay=0;bay<8;bay++)for(int tier=0;tier<3;tier++)for(int col=-1;col<=1;col++)
                    kit.Box(stacks[(bay*3+tier*2+col+5)%stacks.Length],L(col*6.2f,6.2f+tier*2.6f,-36+bay*11.5f),new Vector3(6,2.5f,11.2f),rot);
                boats++;
            }
        }

        static void Sailboat(PolygonSceneryKit kit,Vector3 p,float yaw,float s,Material sail)
        {
            var rot=Quaternion.Euler(0,yaw,0);Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z)*s;
            kit.Prism(white,new[]{L(-1.3f,-.3f,-4),L(1.3f,-.3f,-4),L(1.3f,-.3f,2.5f),L(0,-.3f,5)},new[]{L(-1.5f,.9f,-4.2f),L(1.5f,.9f,-4.2f),L(1.5f,.9f,2.6f),L(0,.9f,5.6f)});
            kit.Box(navy,L(0,1.3f,-1.6f),new Vector3(1.6f,.8f,2.4f)*s,rot);
            kit.Beam(steel,L(0,.9f,.6f),L(0,10.5f,.6f),.12f*s,.12f*s);
            kit.Sheet(sail,L(0,10.2f,.55f),L(0,1.5f,.55f),L(0,1.5f,-3.6f));
            kit.Sheet(white,L(0,9.4f,.7f),L(0,1.3f,.7f),L(0,1.3f,4.6f));
        }
        static void MotorBoat(PolygonSceneryKit kit,Vector3 p,float yaw)
        {
            var rot=Quaternion.Euler(0,yaw,0);Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
            kit.Prism(white,new[]{L(-1.6f,-.2f,-5),L(1.6f,-.2f,-5),L(1.6f,-.2f,3),L(0,-.2f,6)},new[]{L(-1.9f,1.1f,-5.2f),L(1.9f,1.1f,-5.2f),L(1.9f,1.1f,3.2f),L(0,1.1f,6.6f)});
            kit.Box(navy,L(0,1.5f,-.8f),new Vector3(3,.8f,4),rot);
            kit.Box(windowDark,L(0,2.1f,.2f),new Vector3(2.8f,.6f,2.2f),rot);
            kit.Box(teal,L(0,.75f,0),new Vector3(3.9f,.25f,10.2f),rot);
        }
    }
}
