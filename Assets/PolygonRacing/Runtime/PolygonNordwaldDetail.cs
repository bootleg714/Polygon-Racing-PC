using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Nordwald PC detail pass v01 (October 2026). Adds sponsor boards on the straights, banners on the
    // spectator catch fences, marshal posts before the big corners, festival marquees and flag lines
    // behind the crowds, camping clearings in the woods, spinning wind turbines (beside the road and
    // on the hilltops), hunting high seats and hay bales on the meadows and timber piles at the forest
    // edge. Runs before the forest is planted, so every reserved spot keeps the trees back.
    // Everything except the turbine rotors is baked into the same chunks as the rest of the scenery.
    public static partial class PolygonNordwaldArt
    {
        static Material turbineWhite,marshalOrange,straw,baleWrap,logEnds;
        static Material[] sponsorColors;
        static Transform rotorRoot;
        static int clearingStart,clearingEnd;
        static int dBoards,dBanners,dMarshals,dMarquees,dCampers,dSeats,dBales,dTurbines,dLogPiles;
        static readonly List<Vector3> turbineSpots=new List<Vector3>();

        static void BuildDetail(Transform world,Chunks chunks)
        {
            turbineWhite=Mat("Turbine white",new Color(.92f,.93f,.94f));
            marshalOrange=Mat("Marshal orange",new Color(1f,.48f,.10f));
            straw=Mat("Straw bale",new Color(.86f,.74f,.42f));
            baleWrap=Mat("Bale wrap",new Color(.92f,.93f,.90f));
            logEnds=Mat("Log ends",new Color(.80f,.66f,.46f));
            sponsorColors=new[]{red,navy,yellow,green,teal,coral,charcoal,white};
            dBoards=dBanners=dMarshals=dMarquees=dCampers=dSeats=dBales=dTurbines=dLogPiles=0;
            turbineSpots.Clear();
            rotorRoot=new GameObject("Nordwald turbine rotors").transform;rotorRoot.SetParent(world,false);

            BuildSponsorBoards(chunks);
            BuildFenceBanners(chunks);
            BuildMarshalPosts(chunks);
            BuildFestival(chunks);
            BuildCampClearings(chunks);
            BuildTurbines(chunks);
            BuildHighSeats(chunks);
            BuildLogPiles(chunks);
            BuildBales(chunks);
            turbineSpots.Clear();
            Debug.Log($"Nordwald PC detail v01: {dBoards} sponsor board runs, {dBanners} fence banners, {dMarshals} marshal posts, "+
                      $"{dMarquees} festival marquees/stalls, {dCampers} extra campers and tents, {dTurbines} wind turbines, "+
                      $"{dSeats} high seats, {dLogPiles} timber piles, {dBales} hay bales.");
        }

        // ------------------------------------------------------------ helpers
        static bool Near(float d,float at,float r)
        {
            float x=Mathf.Repeat(d-at,PolygonCircuit.Length);
            return x<r||x>PolygonCircuit.Length-r;
        }
        static bool NearBridge(float d,float r)=>Near(d,DistanceAtKnot(76.5f),r)||Near(d,DistanceAtKnot(33.3f),r);
        static bool NearStart(float d)=>Near(d,(PitStart+PitEnd)*.5f,(PitEnd-PitStart)*.5f+35);
        static float Ground(Vector3 p)=>TerrainHeight(p.x,p.z);
        static bool Level(Vector3 p,float r,float tolerance)
        {
            float h=Ground(p);
            return Mathf.Abs(TerrainHeight(p.x+r,p.z)-h)<tolerance&&Mathf.Abs(TerrainHeight(p.x-r,p.z)-h)<tolerance&&
                   Mathf.Abs(TerrainHeight(p.x,p.z+r)-h)<tolerance&&Mathf.Abs(TerrainHeight(p.x,p.z-r)-h)<tolerance;
        }
        // Like Reserved, but ignores the open clearings behind the spectator fences (those are free
        // ground for marquees and flags; grandstands, buildings and bridges still block).
        static bool Solid(Vector2 p,float margin)
        {
            for(int i=0;i<reserved.Count;i++)
            {
                if(i>=clearingStart&&i<clearingEnd)continue;
                var r=reserved[i];
                if(p.x>r.xMin-margin&&p.x<r.xMax+margin&&p.y>r.yMin-margin&&p.y<r.yMax+margin)return true;
            }
            return false;
        }
        static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
        static void ReserveBox(Vector3 p,float r){ReserveWorld(p.x-r,p.x+r,p.z-r,p.z+r);}

        static void Figure(PolygonSceneryKit kit,Vector3 p,Vector3 facing,Material shirt,int seed)
        {
            float h=Hash(p.x+seed,p.z-seed,9);
            var r=Vector3.Cross(Vector3.up,facing.normalized)*.11f;
            kit.Beam(jeans,p+r,p+r+Vector3.up*.86f,.14f,.14f);kit.Beam(jeans,p-r,p-r+Vector3.up*.86f,.14f,.14f);
            kit.PersonLite(p+Vector3.up*.78f,facing,.95f+h*.1f,shirt,skins[Mathf.FloorToInt(Hash(p.z,p.x,10)*3)%3],hair,false);
        }

        // ------------------------------------------------------------ sponsor boards on the straights
        static void BuildSponsorBoards(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;int lastRun=-1;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=d0+step,dm=(d0+d1)*.5f;
                if(NearStart(dm)||NearBridge(dm,40)||Banked(dm))continue;
                if(Mathf.Abs(PolygonCircuit.Curvature(dm))>1/220f)continue;
                bool kilometre=false;for(int km=1;km*1000<PolygonCircuit.Length;km++)if(Near(dm,km*1000,3))kilometre=true;
                if(kilometre)continue;
                int run=Mathf.FloorToInt(dm/72);if(run%3==2)continue;   // runs of boards with breaks between
                var kit=chunks.At(P(dm));
                foreach(float side in new[]{-1f,1f})
                {
                    if(Fenced(dm,side))continue;
                    if(((run+(side>0?1:0))&1)==1)continue;           // alternate sides run by run
                    int block=Mathf.FloorToInt(dm/18)+(side>0?3:0);var board=sponsorColors[block%sponsorColors.Length];
                    Slab(kit,side,d0,d1,10.32f,10.46f,-.6f,1.0f,board);
                    if(block%3!=1)Slab(kit,side,d0,d1,10.28f,10.32f,.34f,.66f,board==white||board==yellow?navy:white);
                    if(run!=lastRun){dBoards++;lastRun=run;}
                }
            }
        }

        // ------------------------------------------------------------ banners on the catch fences
        static void BuildFenceBanners(Chunks chunks)
        {
            int n=PolygonCircuit.SampleCount;float step=PolygonCircuit.Length/n;int last=-1;
            for(int i=0;i<n;i++)
            {
                float d0=i*step,d1=d0+step,dm=(d0+d1)*.5f;
                int run=Mathf.FloorToInt(dm/24);if(run%2==1||NearBridge(dm,30))continue;
                var kit=chunks.At(P(dm));
                foreach(float side in new[]{-1f,1f})
                {
                    if(!Fenced(dm,side))continue;
                    var m=sponsorColors[(run/2+(side>0?2:0))%sponsorColors.Length];
                    Slab(kit,side,d0,d1,10.28f,10.33f,1.05f,1.85f,m);
                    if(run!=last){dBanners++;last=run;}
                }
            }
        }

        // ------------------------------------------------------------ marshal posts
        static void BuildMarshalPosts(Chunks chunks)
        {
            float last=-999,length=PolygonCircuit.Length;
            for(float d=320;d<length-120;d+=5)
            {
                if(d-last<480||!Corner(d))continue;
                float pd=d-38,side=OutsideSign(d);
                if(NearStart(pd)||NearBridge(pd,45)||Fenced(pd,side)||Banked(pd))continue;
                var p=At(pd,side*(Barrier1+3.4f),0);
                if(Reserved(XZ(p),3)||Offset(p)<Barrier1+2.4f)continue;
                p.y=Mathf.Max(Ground(p),H(pd,side*Edge));
                var f=T(pd);f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f);var away=Vector3.Cross(Vector3.up,f)*side;
                var kit=chunks.At(p);
                kit.Box(concrete,p+Vector3.down*.6f,new Vector3(5.4f,1.4f,4.4f),rot);
                var c=p+away*1.1f+Vector3.up*.1f;
                kit.Box(white,c+Vector3.up*1.1f,new Vector3(2.2f,2.2f,2.6f),rot);
                kit.Box(red,c+Vector3.up*2.32f,new Vector3(2.6f,.24f,3.0f),rot);
                kit.Box(glassDark,c-away*1.11f+Vector3.up*1.45f,new Vector3(.06f,.7f,1.8f),rot);
                kit.Box(marshalOrange,c-away*1.12f+Vector3.up*.55f,new Vector3(.06f,.3f,2.2f),rot);
                // Flag stand and fire extinguisher at the front of the pad.
                var fp=p-away*1.2f+f*1.5f+Vector3.up*.1f;
                kit.Beam(steel,fp,fp+Vector3.up*2.3f,.08f,.08f);
                kit.SheetQuad(yellow,fp+Vector3.up*2.25f,fp+Vector3.up*2.25f-f*1.1f,fp+Vector3.up*1.55f-f*1.1f,fp+Vector3.up*1.55f);
                kit.Column(red,p-away*1.4f-f*1.6f+Vector3.up*.1f,.14f,.14f,.62f,6);
                // Two marshals in orange overalls watching the road.
                Figure(kit,p-away*1.5f+Vector3.up*.1f,-away,marshalOrange,(int)pd);
                Figure(kit,p-away*1.2f-f*.9f+Vector3.up*.1f,-away+f*.3f,marshalOrange,(int)pd+7);
                ReserveBox(p,4.6f);
                last=d;dMarshals++;
            }
        }

        // ------------------------------------------------------------ festival behind the crowds
        static void BuildFestival(Chunks chunks)
        {
            int item=0;
            foreach(var z in spectatorZones)
            {
                float a=DistanceAtKnot(z.x),b=DistanceAtKnot(z.y),side=Mathf.Sign(z.z);
                for(float d=a+14;d<b-14;d+=27)
                {
                    float lateral=side*(Barrier1+11.5f+Hash(d,z.x,61)*2.5f);
                    var p=At(d,lateral,0);
                    if(Solid(XZ(p),6)||Offset(p)<Barrier1+9)continue;
                    p.y=Ground(p);
                    if(!Level(p,4,1.6f))continue;
                    var f=T(d);f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f);var toRoad=-Vector3.Cross(Vector3.up,f)*side;
                    var kit=chunks.At(p);int kind=item%3;
                    if(kind==0)Marquee(kit,p,rot,toRoad,item);
                    else if(kind==1)FlagLine(kit,p,f,item);
                    else FoodStall(kit,p,rot,toRoad,item);
                    if(kind!=1)dMarquees++;
                    item++;
                }
            }
        }
        static void Marquee(PolygonSceneryKit kit,Vector3 p,Quaternion rot,Vector3 toRoad,int seed)
        {
            var stripe=tentColors[seed%tentColors.Length];
            Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
            kit.Box(white,L(0,.85f,0),new Vector3(5.0f,2.3f,8.0f),rot);
            kit.Box(stripe,L(0,1.86f,0),new Vector3(5.12f,.3f,8.12f),rot);
            kit.Prism(white,new[]{L(-2.8f,2.0f,-4.3f),L(2.8f,2.0f,-4.3f),L(0,3.7f,-4.3f)},new[]{L(-2.8f,2.0f,4.3f),L(2.8f,2.0f,4.3f),L(0,3.7f,4.3f)});
            kit.Beam(steel,L(0,3.7f,-4.3f),L(0,5.2f,-4.3f),.06f,.06f);
            kit.SheetQuad(stripe,L(0,5.15f,-4.3f),L(0,5.15f,-3.3f),L(0,4.55f,-3.3f),L(0,4.55f,-4.3f));
            // Beer benches in front, with a few people.
            for(int k=-1;k<=1;k++)
            {
                var bc=p+toRoad*4.2f+rot*new Vector3(0,0,k*2.6f);
                kit.Box(timber,bc+Vector3.up*.72f,new Vector3(.7f,.06f,2.2f),rot);
                kit.Box(timber,bc+Vector3.up*.36f,new Vector3(.5f,.72f,.1f),rot);
                if(Hash(seed,k,62)<.7f)Figure(kit,bc+toRoad*.9f,-toRoad,shirts[(seed+k+5)%shirts.Length],seed*3+k);
            }
        }
        static void FlagLine(PolygonSceneryKit kit,Vector3 p,Vector3 f,int seed)
        {
            for(int k=-2;k<=2;k++)
            {
                var fp=p+f*(k*3.2f);fp.y=Ground(fp)-.2f;
                kit.Beam(steel,fp,fp+Vector3.up*7.2f,.1f,.1f);
                var col=tentColors[(seed+k+8)%tentColors.Length];
                kit.SheetQuad(col,fp+Vector3.up*7.1f,fp+Vector3.up*7.1f+f*1.9f,fp+Vector3.up*5.9f+f*1.9f,fp+Vector3.up*5.9f);
            }
        }
        static void FoodStall(PolygonSceneryKit kit,Vector3 p,Quaternion rot,Vector3 toRoad,int seed)
        {
            var body=vanStripes[seed%vanStripes.Length];
            Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
            kit.Box(body,L(0,1.55f,0),new Vector3(2.4f,2.7f,5.0f),rot);
            kit.Box(white,L(0,2.96f,0),new Vector3(2.5f,.14f,5.1f),rot);
            kit.Box(charcoal,p+toRoad*1.21f+Vector3.up*1.75f,new Vector3(.06f,.8f,3.2f),rot);
            kit.SheetQuad(white,p+toRoad*1.22f+Vector3.up*2.65f+rot*new Vector3(0,0,-2.2f),p+toRoad*1.22f+Vector3.up*2.65f+rot*new Vector3(0,0,2.2f),
                                p+toRoad*2.6f+Vector3.up*2.3f+rot*new Vector3(0,0,2.2f),p+toRoad*2.6f+Vector3.up*2.3f+rot*new Vector3(0,0,-2.2f));
            foreach(int s in new[]{-1,1})foreach(int k in new[]{-1,1})
                kit.Box(charcoal,L(s*1.1f,.3f,k*1.6f),new Vector3(.25f,.6f,.6f),rot);
            for(int k=0;k<3;k++)
                if(Hash(seed,k,63)<.75f)Figure(kit,p+toRoad*(2.4f+k*.7f)+rot*new Vector3(0,0,(k-1)*1.1f),-toRoad,shirts[(seed+k)%shirts.Length],seed*5+k);
        }

        // ------------------------------------------------------------ camping clearings in the woods
        static void BuildCampClearings(Chunks chunks)
        {
            int zone=0;
            foreach(var z in spectatorZones)
            {
                float a=DistanceAtKnot(z.x),b=DistanceAtKnot(z.y),side=Mathf.Sign(z.z);
                float mid=(a+b)*.5f;
                foreach(float along in new[]{-.25f,.25f})
                {
                    float d=mid+(b-a)*along;
                    var c=Flat(d,side*50);
                    if(Solid(XZ(c),24)||Offset(c)<44)continue;
                    var f=T(d);f.y=0;f.Normalize();
                    int placed=0;
                    for(int i=0;i<16;i++)
                    {
                        int seed=zone*40+i+(along>0?20:0);
                        var p=c+f*((Hash(seed,1,71)-.5f)*34)+Vector3.Cross(Vector3.up,f)*side*((Hash(seed,2,72)-.5f)*22);
                        if(Offset(p)<36||Solid(XZ(p),3))continue;
                        p.y=Ground(p);if(!Level(p,3,.9f))continue;
                        var rot=Quaternion.Euler(0,Hash(seed,3,73)*360,0);var kit=chunks.At(p);
                        if(i%3==0)Camper(kit,p,rot,seed);else Tent(kit,p,rot,seed);
                        if(i%5==2)Figure(kit,p+rot*new Vector3(2.6f,0,1.2f),rot*Vector3.back,shirts[seed%shirts.Length],seed);
                        placed++;
                    }
                    if(placed==0)continue;
                    dCampers+=placed;
                    // Campfire ring in the middle.
                    var fire=c;fire.y=Ground(fire);var fk=chunks.At(fire);
                    for(int k=0;k<8;k++){float ang=k*Mathf.PI/4;fk.Box(rock,fire+new Vector3(Mathf.Cos(ang)*.9f,.1f,Mathf.Sin(ang)*.9f),new Vector3(.35f,.3f,.35f));}
                    fk.Column(bark,fire,.5f,.05f,.7f,5);
                    ReserveWorld(c.x-22,c.x+22,c.z-22,c.z+22);
                }
                zone++;
            }
        }
        static void Camper(PolygonSceneryKit kit,Vector3 p,Quaternion rot,int i)
        {
            kit.Box(white,p+Vector3.up*1.7f,new Vector3(2.3f,2.4f,6.2f),rot);
            kit.Box(vanStripes[i%vanStripes.Length],p+Vector3.up*1.4f,new Vector3(2.34f,.45f,6.24f),rot);
            kit.Box(glassDark,p+Vector3.up*2.1f+rot*new Vector3(0,0,3.05f),new Vector3(2.0f,.8f,.14f),rot);
            foreach(int s in new[]{-1,1})foreach(int f in new[]{-1,1})kit.Box(charcoal,p+Vector3.up*.38f+rot*new Vector3(s*1.05f,0,f*2.1f),new Vector3(.32f,.76f,.76f),rot);
            kit.SheetQuad(vanStripes[(i+2)%vanStripes.Length],p+rot*new Vector3(1.15f,2.5f,-2),p+rot*new Vector3(1.15f,2.5f,2),p+rot*new Vector3(3.6f,2.1f,2),p+rot*new Vector3(3.6f,2.1f,-2));
            kit.Box(red,p+rot*new Vector3(2.6f,.45f,0),new Vector3(.6f,.1f,.6f),rot);
        }
        static void Tent(PolygonSceneryKit kit,Vector3 p,Quaternion rot,int i)
        {
            var col=tentColors[i%tentColors.Length];
            var l=rot*Vector3.right;var f=rot*Vector3.forward;float w=1.4f+Hash(i,4,74)*.6f,len=2.2f+Hash(i,5,75);
            var b=p+Vector3.down*.1f;
            kit.Prism(col,new[]{b-l*w-f*len,b+l*w-f*len,b+Vector3.up*1.4f-f*len},new[]{b-l*w+f*len,b+l*w+f*len,b+Vector3.up*1.4f+f*len});
        }

        // ------------------------------------------------------------ wind turbines
        static void BuildTurbines(Chunks chunks)
        {
            float length=PolygonCircuit.Length;var laps=new List<float>();
            // Beside the road, where the forest opens: spaced round the lap so one is always in view.
            for(float d=180;d<length-120&&laps.Count<7;d+=20)
            {
                bool spaced=true;foreach(var l in laps)if(Near(d,l,420))spaced=false;
                if(!spaced||NearStart(d))continue;
                foreach(float side in new[]{-1f,1f})
                {
                    var p=Flat(d,side*(52+Hash(d,side,81)*22));
                    if(Offset(p)<46||Reserved(XZ(p),14)||!Level(p,7,2.6f))continue;
                    p.y=Ground(p);
                    var facing=Flat(d,0)-p;facing.y=0;facing.Normalize();
                    Turbine(chunks,p,54,22,facing,laps.Count+30);
                    laps.Add(d);break;
                }
            }
            // A wind farm along the highest ground inside the detailed area, all facing the wind.
            var candidates=new List<Vector3>();
            for(float x=-880;x<=1180;x+=40)
                for(float z=-240;z<=1400;z+=40)
                {
                    var p=new Vector3(x,0,z);float o=Offset(p);
                    if(o<170||o>460||(IsMeadow(x,z)&&Hash(x,z,82)<.5f))continue;
                    p.y=Ground(p);candidates.Add(p);
                }
            candidates.Sort((u,v)=>v.y.CompareTo(u.y));
            var wind=new Vector3(-.55f,0,-.83f).normalized;int farm=0;
            foreach(var p in candidates)
            {
                if(farm>=7)break;
                bool spaced=true;foreach(var q in turbineSpots)if((new Vector2(q.x-p.x,q.z-p.z)).sqrMagnitude<150*150)spaced=false;
                if(!spaced||Reserved(XZ(p),18)||!Level(p,7,3f))continue;
                Turbine(chunks,p,64,26,wind,farm+50);farm++;
            }
        }
        static void Turbine(Chunks chunks,Vector3 p,float hub,float blade,Vector3 facing,int seed)
        {
            var kit=chunks.At(p);var look=Quaternion.LookRotation(facing);float s=hub/62;
            kit.Column(concrete,p+Vector3.down*1.6f,4.4f*s,4.1f*s,1.9f,10);
            kit.Column(turbineWhite,p+Vector3.down*.2f,1.7f*s,.95f*s,hub,10);
            kit.Box(turbineWhite,p+Vector3.up*(hub+.6f*s)-facing*.6f*s,new Vector3(2.2f,2.4f,5.2f)*s,look);
            kit.Box(red,p+Vector3.up*(hub+1.95f*s)-facing*2.4f*s,new Vector3(.35f,.3f,.35f),look);
            var rotor=new GameObject("Wind turbine rotor").transform;rotor.SetParent(rotorRoot,false);
            rotor.position=p+Vector3.up*(hub+.6f*s)+facing*2.4f*s;rotor.rotation=look;
            var rk=new PolygonSceneryKit("Nordwald turbine rotor");
            rk.Box(turbineWhite,Vector3.zero,new Vector3(1.5f,1.5f,1.6f)*s);
            rk.Box(turbineWhite,new Vector3(0,0,.95f*s),new Vector3(.9f,.9f,.5f)*s);
            for(int b=0;b<3;b++)
            {
                float a=b*120*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                rk.Box(turbineWhite,dir*(blade*.5f+.7f*s)+Vector3.forward*.2f,new Vector3(1.1f*s,blade,.32f*s),Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90));
                rk.Box(red,dir*(blade+.7f*s-.9f)+Vector3.forward*.2f,new Vector3(1.14f*s,1.6f,.36f*s),Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90));
            }
            rk.Emit(rotor);
            rotor.Rotate(0,0,Hash(seed,p.x,83)*120,Space.Self);
            var spinner=rotor.gameObject.AddComponent<PolygonWindTurbine>();
            spinner.DegreesPerSecond=14+Hash(seed,p.z,84)*10;
            ReserveBox(p,12);turbineSpots.Add(p);dTurbines++;
        }

        // ------------------------------------------------------------ hunting high seats on the meadows
        static void BuildHighSeats(Chunks chunks)
        {
            float last=-999;
            for(float d=60;d<PolygonCircuit.Length&&dSeats<12;d+=15)
            {
                if(d-last<240||NearStart(d))continue;
                foreach(float side in new[]{-1f,1f})
                {
                    var p=Flat(d,side*(28+Hash(d,side,91)*30));
                    if(!IsMeadow(p.x,p.z)||Offset(p)<24||Reserved(XZ(p),4))continue;
                    p.y=Ground(p);
                    var toRoad=Flat(d,0)-p;toRoad.y=0;toRoad.Normalize();
                    HighSeat(chunks.At(p),p,toRoad);ReserveBox(p,3);
                    last=d;dSeats++;break;
                }
            }
        }
        static void HighSeat(PolygonSceneryKit kit,Vector3 p,Vector3 toRoad)
        {
            var rot=Quaternion.LookRotation(toRoad);
            Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
            const float top=4.4f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                kit.Beam(timber,L(sx*1.25f,-.3f,sz*1.25f),L(sx*.8f,top,sz*.8f),.16f,.16f);
            kit.Beam(timber,L(-1.1f,1.6f,-1.1f),L(1.1f,1.6f,1.1f),.08f,.08f);
            kit.Beam(timber,L(1.1f,1.6f,-1.1f),L(-1.1f,1.6f,1.1f),.08f,.08f);
            kit.Box(timber,L(0,top,0),new Vector3(2.0f,.14f,2.0f),rot);
            kit.Box(bark,L(0,top+.85f,0),new Vector3(1.7f,1.6f,1.7f),rot);
            kit.Box(charcoal,L(0,top+1.2f,.86f),new Vector3(1.1f,.4f,.04f),rot);
            kit.Box(spruceDark,L(0,top+1.78f,-.05f),new Vector3(2.1f,.12f,2.2f),rot*Quaternion.Euler(-12,0,0));
            // Ladder up the back.
            var l0=L(0,-.2f,-2.3f);var l1=L(0,top,-.95f);var across=rot*Vector3.right*.28f;
            kit.Beam(timber,l0-across,l1-across,.08f,.08f);kit.Beam(timber,l0+across,l1+across,.08f,.08f);
            for(int k=1;k<10;k++){var q=Vector3.Lerp(l0,l1,k/10f);kit.Beam(timber,q-across,q+across,.06f,.06f);}
        }

        // ------------------------------------------------------------ timber piles at the forest edge
        static void BuildLogPiles(Chunks chunks)
        {
            float last=-999;
            for(float d=120;d<PolygonCircuit.Length&&dLogPiles<16;d+=10)
            {
                if(d-last<300||NearStart(d)||NearBridge(d,40)||Banked(d))continue;
                float side=Hash(d,3,95)<.5f?-1:1;
                if(Fenced(d,side))side=-side;
                if(Fenced(d,side))continue;
                var p=Flat(d,side*15.5f);
                if(IsMeadow(p.x,p.z)||Reserved(XZ(p),4)||Offset(p)<14)continue;
                p.y=Ground(p);if(!Level(p,3,1.1f))continue;
                var f=T(d);f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);var kit=chunks.At(p);
                int[] rows={4,3,2};
                for(int row=0;row<rows.Length;row++)
                    for(int k=0;k<rows[row];k++)
                    {
                        float x=(k-(rows[row]-1)*.5f)*.5f;
                        var c=p+r*x+Vector3.up*(.22f+row*.42f);
                        var a=c-f*2.6f;var b=c+f*2.6f;
                        kit.Beam(bark,a,b,.44f,.44f);
                        kit.Beam(logEnds,a-f*.03f,a+f*.02f,.46f,.46f);
                        kit.Beam(logEnds,b-f*.02f,b+f*.03f,.46f,.46f);
                    }
                ReserveBox(p,4);last=d;dLogPiles++;
            }
        }

        // ------------------------------------------------------------ hay bales on the meadows
        static void BuildBales(Chunks chunks)
        {
            for(float x=-900;x<=1200;x+=12)
                for(float z=-260;z<=1420;z+=12)
                {
                    if(!IsMeadow(x,z)||Hash(x,z,101)>.075f)continue;
                    var c=new Vector3(x+(Hash(x,z,102)-.5f)*8,0,z+(Hash(x,z,103)-.5f)*8);
                    float o=Offset(c);if(o<22||o>170||Reserved(XZ(c),3))continue;
                    int count=1+Mathf.FloorToInt(Hash(x,z,104)*3);
                    var m=Hash(x,z,105)<.7f?straw:baleWrap;
                    float yaw=Hash(x,z,106)*Mathf.PI;var axis=new Vector3(Mathf.Cos(yaw),0,Mathf.Sin(yaw));var across=Vector3.Cross(Vector3.up,axis);
                    for(int k=0;k<count;k++)
                    {
                        var p=c+across*(k*1.9f);p.y=Ground(p);
                        Bale(chunks.At(p),p,axis,m);dBales++;
                    }
                }
        }
        static void Bale(PolygonSceneryKit kit,Vector3 p,Vector3 axis,Material m)
        {
            const float r=.75f,half=.62f;var up=Vector3.up;var side=Vector3.Cross(up,axis);
            var a=new Vector3[8];var b=new Vector3[8];var centre=p+up*(r-.06f);
            for(int i=0;i<8;i++)
            {
                float t=(i+.5f)*Mathf.PI/4;var o=(side*Mathf.Cos(t)+up*Mathf.Sin(t))*r;
                a[i]=centre+o-axis*half;b[i]=centre+o+axis*half;
            }
            kit.Prism(m,a,b);
        }
    }
}
