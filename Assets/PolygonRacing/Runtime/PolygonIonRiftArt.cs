using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Editor-time canyon construction. Road, tunnels and bridges follow the exact race spline.
    // Rock silhouettes and a few large industrial landmarks carry detail without a dense city.
    public static class PolygonIonRiftArt
    {
        public const string SceneryName="Circuit - editable scenery";
        public const string AnimatedName="Ion Rift - animated machinery";
        public const float Barrier=12f,DeckEdge=13.2f;
        const float ChunkSize=220,Floor=-58;
        static Material road,deck,steel,black,paint,basalt,basaltLight,basaltDark,dust,
            cyan,amber,violet,warmWindow,glass,energy,data,plum;
        static Vector3[] samples;
        static float[] sampleHeights;
        sealed class Pad {public Vector3 Center;public Quaternion Inverse;public float HalfX,HalfZ,Height;}
        static readonly List<Pad> pads=new List<Pad>();
        public static int StructureCount {get;private set;}
        public static int RockCount {get;private set;}

        sealed class Chunks
        {
            readonly Dictionary<Vector2Int,PolygonSceneryKit> kits=new Dictionary<Vector2Int,PolygonSceneryKit>();
            public PolygonSceneryKit At(Vector3 p)
            {
                var key=new Vector2Int(Mathf.FloorToInt(p.x/ChunkSize),Mathf.FloorToInt(p.z/ChunkSize));
                if(!kits.TryGetValue(key,out var kit))
                {kit=new PolygonSceneryKit("Ion canyon "+key.x+","+key.y){MaxVerticesPerMesh=60000};kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform parent)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Canyon sector "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(parent,false);pair.Value.Emit(group);
                }
            }
        }
        static Material Mat(string name,Color color,float emission=0,float smoothness=.16f)
        {
            var shader=Resources.Load<Shader>("PolygonFlatPC");if(!shader)shader=PolygonLook.LitShader();
            var m=new Material(shader){name="IR "+name,color=color,enableInstancing=true};
            m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",0);m.SetFloat("_ReflectionStrength",.18f);
            m.SetColor("_SkyAmbient",new Color(.38f,.37f,.61f));m.SetColor("_GroundAmbient",new Color(.19f,.14f,.22f));
            m.SetFloat("_AmbientStrength",1);m.SetFloat("_SunWrap",.2f);
            if(emission>0)m.SetColor("_EmissionColor",color*emission);
            return m;
        }
        static void Palette()
        {
            road=Mat("Asphalt",new Color(.12f,.13f,.18f),0,.25f);
            deck=Mat("Bridge graphite",new Color(.20f,.24f,.31f));
            steel=Mat("Pale alloy",new Color(.52f,.60f,.68f),0,.42f);
            black=Mat("Black recess",new Color(.025f,.035f,.055f));
            paint=Mat("Road paint",new Color(.79f,.85f,.88f),.12f);
            basalt=Mat("Basalt",new Color(.28f,.23f,.30f));
            basaltLight=Mat("Basalt exposed strata",new Color(.37f,.30f,.36f));
            basaltDark=Mat("Basalt shadow strata",new Color(.18f,.17f,.24f));
            dust=Mat("Rim dust",new Color(.34f,.27f,.32f));
            cyan=Mat("Cyan conduit",new Color(.035f,.85f,1),3.0f);
            amber=Mat("Amber beacon",new Color(1,.39f,.055f),2.4f);
            violet=Mat("Violet edge",new Color(.50f,.15f,1),2.4f);
            warmWindow=Mat("Warm control room",new Color(1,.65f,.27f),1.3f);
            glass=Mat("Control room glass",new Color(.06f,.11f,.17f),0,.5f);
            plum=Mat("Reactor cladding",new Color(.24f,.17f,.29f));
            var shader=Resources.Load<Shader>("PolygonIonEnergy");
            if(!shader)throw new InvalidOperationException("PolygonIonEnergy.shader must finish importing before generating Ion Rift.");
            energy=new Material(shader){name="IR Flowing energy channel",enableInstancing=true};
            energy.SetColor("_BaseColor",new Color(.008f,.055f,.085f));energy.SetColor("_EmissionColor",new Color(.07f,1.4f,1.9f));
            var panel=Resources.Load<Shader>("PolygonNeonPanel");
            if(!panel)throw new InvalidOperationException("PolygonNeonPanel.shader is missing.");
            data=new Material(panel){name="IR Cascading conduit",enableInstancing=true};
            data.SetColor("_BaseColor",new Color(.018f,.055f,.09f));data.SetColor("_EmissionColor",new Color(.04f,1.6f,2.4f));
            data.SetFloat("_ScrollSpeed",.24f);data.SetFloat("_PatternScale",.32f);
        }
        public static Vector3 RoadPoint(float d,float lateral=0,float up=0)=>PolygonBanking.SurfacePoint(d,lateral,up);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(d));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float D(float k)=>PolygonCircuit.DistanceAtKnot(k);
        static float Hash(float x,float z,int seed=0)=>Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f+seed*37.719f)*43758.5453f,1);
        // This gently winding gorge crosses the descending viaduct and the lower return.
        public static float RiverX(float z)=>-238+151*Mathf.Sin(z*.003704f)+70*Mathf.Sin(z*.008333f+1);
        static float RiverDistance(float x,float z)
        {
            float derivative=(RiverX(z+1)-RiverX(z-1))*.5f;
            return Mathf.Abs(x-RiverX(z))/Mathf.Sqrt(1+derivative*derivative);
        }
        static void PrepareTerrain()
        {
            const int n=512;samples=new Vector3[n];sampleHeights=new float[n];
            for(int i=0;i<n;i++)
            {float d=PolygonCircuit.Length*i/n;samples[i]=PolygonCircuit.AtDistance(d);sampleHeights[i]=PolygonCircuit.Elevation(d);}
        }
        static int Outside(float d)
        {
            var p=RoadPoint(d);var side=S(d);
            return RiverDistance((p+side*50).x,(p+side*50).z)>RiverDistance((p-side*50).x,(p-side*50).z)?1:-1;
        }
        static void AddPad(float d,float lateral,float width,float depth,float up=0)
        {
            var p=RoadPoint(d,lateral,up);
            pads.Add(new Pad{Center=p,Inverse=Quaternion.Inverse(Quaternion.LookRotation(T(d))),HalfX=width*.5f,HalfZ=depth*.5f,Height=p.y-1});
        }
        static void PreparePads()
        {
            pads.Clear();AddPad(D(1.5f),-39,34,150);AddPad(D(24.5f),-79,76,88,-8);
            for(float k=4.1f;k<10;k+=1.3f){float d=D(k);AddPad(d,Outside(d)*38,27,25);}
            foreach(float k in new[]{14.8f,18.3f,26.5f,32.1f})AddPad(D(k),-44,25,24);
            foreach(float k in new[]{5.7f,12.7f,18.9f,26.8f,32.4f})
            {float d=D(k);AddPad(d,Outside(d)*54,29,29);}
        }
        public static float GroundHeight(float x,float z)
        {
            var p=new Vector3(x,0,z);float nearest=float.MaxValue,height=60;int closest=0;
            for(int i=0;i<samples.Length;i++)
            {
                float sq=(p-samples[i]).sqrMagnitude;
                if(sq<nearest){nearest=sq;height=sampleHeights[i];closest=i;}
            }
            float river=RiverDistance(x,z);
            float outer=height+52+Mathf.Sin(x*.009f+z*.004f)*15+Mathf.Cos(z*.013f-x*.003f)*12;
            // A deep floor, steep terraced sides and a broad upper rim. Coarse faceting is
            // intentional; road-adjacent samples cut a clear shelf in the rock below it.
            float cut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(34,138,river));
            float result=Mathf.Lerp(Floor-5,outer,cut);
            float roadToRiver=samples[closest].x-RiverX(samples[closest].z);
            if(Mathf.Abs(roadToRiver)>40&&(x-samples[closest].x)*roadToRiver<0)
                result=Mathf.Min(result,height-8-Mathf.Sqrt(nearest)*.16f);
            for(int i=0;i<samples.Length;i++)
            {
                float dist=Vector3.Distance(samples[i],p);
                if(dist<128)result=Mathf.Min(result,sampleHeights[i]-5+Mathf.Max(0,dist-25)*.55f);
            }
            // Open a low canyon-facing shelf beside the two long straights, so the
            // distant cliffs and reactor can be seen without changing the road itself.
            float knot=PolygonCircuit.AngleAtDistance(PolygonCircuit.Length*closest/samples.Length)/(Mathf.PI*2)*PolygonCircuit.KnotCount;
            if((knot<4.2f||knot>44f&&knot<52f)&&nearest<180*180)
            {
                var forward=T(PolygonCircuit.Length*closest/samples.Length);
                var right=Vector3.Cross(Vector3.up,forward);
                float towardRiver=Vector3.Dot(new Vector3(RiverX(samples[closest].z)-samples[closest].x,0,0),right);
                float side=Vector3.Dot(p-samples[closest],right);
                if(side*towardRiver>0&&Mathf.Abs(side)>30)
                    result=Mathf.Min(result,height-16+Mathf.Abs(side)*.05f);
            }
            foreach(var pad in pads)
            {
                var local=pad.Inverse*(p-pad.Center);
                float outside=Mathf.Max(Mathf.Abs(local.x)-pad.HalfX,Mathf.Abs(local.z)-pad.HalfZ);
                if(outside<20)result=Mathf.Min(result,pad.Height+Mathf.Max(0,outside)*1.1f);
            }
            return result;
        }
        static void Ribbon(PolygonSceneryKit kit,Material m,float a,float b,float lo,float hi,float up)
        {
            var p=RoadPoint(a,lo,up);var q=RoadPoint(a,hi,up);var r=RoadPoint(b,hi,up);var s=RoadPoint(b,lo,up);
            kit.Quad(m,p,q,r,s,(p+q+r+s)*.25f+Vector3.down);
        }
        static void Slab(PolygonSceneryKit kit,Material m,float a,float b,float lo,float hi,float bottom,float top)
        {
            kit.Prism(m,new[]{RoadPoint(a,lo,bottom),RoadPoint(a,hi,bottom),RoadPoint(b,hi,bottom),RoadPoint(b,lo,bottom)},
                new[]{RoadPoint(a,lo,top),RoadPoint(a,hi,top),RoadPoint(b,hi,top),RoadPoint(b,lo,top)});
        }
        static void BuildRoad(Chunks chunks)
        {
            float length=PolygonCircuit.Length;
            for(float d=0;d<length;d+=4)
            {
                float end=Mathf.Min(d+4,length);var kit=chunks.At(RoadPoint(d));
                Slab(kit,deck,d,end,-DeckEdge,DeckEdge,-2.2f,-.035f);Ribbon(kit,road,d,end,-10,10,0);
                foreach(int side in new[]{-1,1})
                {
                    Ribbon(kit,deck,d,end,side*10,side*Barrier,.04f);
                    Ribbon(kit,paint,d,end,side*9.6f,side*9.79f,.025f);
                    Slab(kit,black,d,end,side*Barrier,side*(Barrier+.5f),0,1.08f);
                    kit.Beam(cyan,RoadPoint(d,side*(Barrier-.03f),1.06f),RoadPoint(end,side*(Barrier-.03f),1.06f),.11f,.12f);
                    if(((int)d/4)%6==0)
                    {
                        kit.Beam(steel,RoadPoint(d,side*(Barrier+.2f),.08f),RoadPoint(d,side*(Barrier+.2f),1.3f),.13f,.13f);
                        kit.Box(amber,RoadPoint(d,side*(Barrier+.2f),1.4f),new Vector3(.24f,.25f,.24f));
                    }
                }
                if(d%24<8)foreach(float lateral in new[]{-3.33f,3.33f})Ribbon(kit,paint,d,end,lateral-.055f,lateral+.055f,.027f);
            }
            // A footing beneath each deck section also supports the shelves when they pass
            // over local ravines. The two main viaducts receive more expressive superstructures.
            for(float d=30;d<length;d+=58)
            {
                var p=RoadPoint(d);float floor=GroundHeight(p.x,p.z)-.5f;
                float h=p.y-2.2f-floor;if(h<1)continue;var kit=chunks.At(p);var rotation=Quaternion.LookRotation(T(d));
                foreach(int side in new[]{-1,1})
                {
                    var q=p+S(d)*side*8.7f;q.y=floor;
                    kit.Box(deck,q+Vector3.up*h*.5f,new Vector3(2.8f,h,4.6f),rotation);
                    kit.Box(steel,q+Vector3.up*.8f,new Vector3(5,1.6f,7),rotation);
                }
            }
            var start=chunks.At(RoadPoint(0));
            for(int row=0;row<2;row++)for(int col=0;col<20;col++)
                Ribbon(start,(row+col)%2==0?paint:black,row*.6f,(row+1)*.6f,-10+col,-9+col,.035f);
            for(int slot=0;slot<4;slot++)foreach(int side in new[]{-1,1})
            {
                float d=length-9-slot*8;Ribbon(chunks.At(RoadPoint(d)),paint,d,d+.14f,side*3-1,side*3+1,.035f);
            }
        }
        static void BuildTerrain(Chunks chunks)
        {
            const int step=26;
            for(int x=-1120;x<1170;x+=step)for(int z=-620;z<1450;z+=step)
            {
                var a=new Vector3(x,GroundHeight(x,z),z);var b=new Vector3(x+step,GroundHeight(x+step,z),z);
                var c=new Vector3(x+step,GroundHeight(x+step,z+step),z+step);var d=new Vector3(x,GroundHeight(x,z+step),z+step);
                var material=((x/step+z/step)&7)==0?basaltLight:basalt;
                chunks.At(a).Quad(material,a,b,c,d,(a+b+c+d)*.25f+Vector3.down);
            }
            // Ground well outside the circuit fades into matching distant rock, rather than a void.
            var far=chunks.At(new Vector3(0,Floor-15,400));far.Box(basaltDark,new Vector3(0,Floor-16,400),new Vector3(6500,1,6500));
            for(float z=-700;z<1510;z+=12)
            {
                float next=z+12;var a=new Vector3(RiverX(z),Floor-.5f,z);var b=new Vector3(RiverX(next),Floor-.5f,next);
                var side=Vector3.Cross(Vector3.up,(b-a).normalized);var kit=chunks.At(a);
                kit.Quad(energy,a-side*21,a+side*21,b+side*21,b-side*21,(a+b)*.5f+Vector3.down);
                foreach(int sign in new[]{-1,1})
                    kit.Beam(cyan,a+side*sign*20,b+side*sign*20,.22f,.16f);
            }
        }
        static bool ClearRock(Vector3 p,float radius)
        {
            foreach(var pad in pads)
            {
                var local=pad.Inverse*(p-pad.Center);
                float dx=Mathf.Max(0,Mathf.Abs(local.x)-pad.HalfX),dz=Mathf.Max(0,Mathf.Abs(local.z)-pad.HalfZ);
                if(dx*dx+dz*dz<(radius+3)*(radius+3))return false;
            }
            PolygonCircuit.ProjectLapFlat(p,out float offset);if(offset<radius+DeckEdge+8)return false;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f;var q=p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                PolygonCircuit.ProjectLapFlat(q,out offset);if(offset<DeckEdge+7)return false;
            }
            return true;
        }
        static void Mesa(PolygonSceneryKit kit,Vector3 p,float radiusX,float radiusZ,float top,int seed)
        {
            const int sides=9;float bottom=Floor-8;
            float[] y={bottom,Mathf.Lerp(bottom,top,.30f),Mathf.Lerp(bottom,top,.58f),Mathf.Lerp(bottom,top,.81f),top};
            float[] scale={1.18f,1.10f,.94f,.78f,.56f};var rings=new Vector3[y.Length][];
            for(int r=0;r<y.Length;r++)
            {
                rings[r]=new Vector3[sides];
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides+.08f*Mathf.Sin(seed*2.31f);
                    float wobble=.88f+Hash(seed,i,23)*.22f;
                    rings[r][i]=new Vector3(p.x+Mathf.Cos(a)*radiusX*scale[r]*wobble+(r-2)*Mathf.Sin(seed)*1.3f,
                        y[r]+(r==0?0:(Hash(i,seed,7)-.5f)*4),p.z+Mathf.Sin(a)*radiusZ*scale[r]*wobble);
                }
            }
            var center=new Vector3(p.x,(bottom+top)*.5f,p.z);
            for(int r=0;r<rings.Length-1;r++)for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;Material m=(i+seed)%4==0?basaltDark:r==2?basaltLight:basalt;
                kit.Quad(m,rings[r][i],rings[r][j],rings[r+1][j],rings[r+1][i],center);
            }
            var cap=new Vector3(p.x,top-.2f,p.z);
            for(int i=0;i<sides;i++)kit.Tri(dust,cap,rings[rings.Length-1][i],rings[rings.Length-1][(i+1)%sides],center);
            RockCount++;
        }
        static void BuildCliffs(Chunks chunks)
        {
            int seed=0;
            for(float d=20;d<PolygonCircuit.Length;d+=69)
            {
                var center=RoadPoint(d);var side=S(d);
                int outside=RiverDistance((center+side*60).x,(center+side*60).z)>RiverDistance((center-side*60).x,(center-side*60).z)?1:-1;
                for(int row=0;row<2;row++)
                {
                    float r=24+Hash(d,row,41)*16;float offset=64+row*88+Hash(d,row,12)*25;
                    var p=RoadPoint(d,outside*offset);int id=seed++;
                    float rz=r*(.9f+Hash(d,row,6)*.5f);
                    if(!ClearRock(p,Mathf.Max(r,rz)*1.34f+4))continue;
                    float top=center.y+42+row*45+Hash(d,row,67)*52;
                    Mesa(chunks.At(p),p,r,rz,top,id);
                }
            }
            // Widely spaced, pointed monoliths on the opposite rim establish the gorge's scale.
            for(float z=-420;z<1300;z+=135)
            {
                foreach(int side in new[]{-1,1})
                {
                    float x=RiverX(z)+side*(110+Hash(z,side,13)*75);var p=new Vector3(x,0,z);
                    float r=16+Hash(x,z,3)*12;if(!ClearRock(p,r*1.34f+4))continue;
                    float height=GroundHeight(x,z)+65+Hash(x,z,91)*110;
                    Mesa(chunks.At(p),p,r,r*.85f,height,seed++);
                }
            }
            // Distant ranges close the horizon but remain very cheap faceted geometry.
            for(int i=0;i<19;i++)
            {
                float a=i*Mathf.PI*2/19;var p=new Vector3(1500*Mathf.Cos(a),0,450+1450*Mathf.Sin(a));
                Mesa(chunks.At(p),p,180,140,190+Hash(i,0,54)*170,seed++);
            }
        }
        static Transform Frame(Transform parent,string name,float d,float lateral=0,float up=0)
        {
            var f=new GameObject(name).transform;f.SetParent(parent,false);
            f.localPosition=RoadPoint(d,lateral,up);f.localRotation=Quaternion.LookRotation(T(d));return f;
        }
        static void Label(Transform parent,string name,string text,Vector3 position,float size,Color color)
        {
            var label=PolygonWorld.Label(name,parent,text,position,size,color);
            var shader=Resources.Load<Shader>("PolygonGantryLettering");
            if(shader)
            {
                var renderer=label.GetComponent<MeshRenderer>();
                renderer.sharedMaterial=new Material(shader){name="IR Depth tested lettering",mainTexture=renderer.sharedMaterial.mainTexture};
            }
        }
        static void BuildStart(Transform parent)
        {
            var frame=Frame(parent,"Rim Station - start gantry",5);var kit=new PolygonSceneryKit("Ion start gantry");
            foreach(int side in new[]{-1,1})
            {
                kit.Beam(steel,new Vector3(side*16,0,0),new Vector3(side*14,10.5f,0),1.6f,1.6f);
                kit.Beam(amber,new Vector3(side*15.1f,.5f,-.9f),new Vector3(side*13.1f,10,-.9f),.17f,.17f);
            }
            kit.Box(black,new Vector3(0,11.9f,0),new Vector3(29,3.5f,1.7f));
            kit.Box(cyan,new Vector3(0,13.75f,-.9f),new Vector3(29,.17f,.11f));kit.Emit(frame);
            Label(frame,"Ion Rift lettering","ION RIFT",new Vector3(0,12.25f,-.91f),.21f,new Color(.3f,.95f,1));
            var signal=frame.gameObject.AddComponent<PolygonStartLights>();signal.DarkMaterial=black;
            signal.RedMaterial=Mat("Countdown red",new Color(1,.02f,.025f),4);signal.Lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)signal.Lenses[i]=PolygonWorld.Box("Countdown signal "+i,frame,new Vector3((i-2)*1.8f,10.85f,-.97f),new Vector3(1.2f,.55f,.14f),black).GetComponent<MeshRenderer>();
            var station=Frame(parent,"Rim Station - team bays",D(1.5f),-39);var building=new PolygonSceneryKit("Rim Station");
            building.Box(deck,new Vector3(0,-1,0),new Vector3(30,2,144));
            building.Box(plum,new Vector3(-4,5,0),new Vector3(21,10,140));
            for(int bay=0;bay<7;bay++)
            {
                float z=-57+bay*19;
                building.Box(glass,new Vector3(6.6f,3,z),new Vector3(.15f,5,14));
                building.Box(amber,new Vector3(6.8f,5.7f,z),new Vector3(.2f,.22f,14));
                building.Box(warmWindow,new Vector3(6.65f,8,z),new Vector3(.15f,1.1f,13));
                building.Beam(steel,new Vector3(8,0,z-8.2f),new Vector3(14,7,z-8.2f),.35f,.35f);
            }
            building.Box(steel,new Vector3(1,10.5f,0),new Vector3(32,1,145));
            building.Box(cyan,new Vector3(17.1f,10.5f,0),new Vector3(.15f,.18f,145));building.Emit(station);StructureCount++;
        }
        static void IndustrialModule(PolygonSceneryKit kit,Vector3 p,Quaternion rotation,int seed,float scale=1)
        {
            Vector3 V(float x,float y,float z)=>p+rotation*(new Vector3(x,y,z)*scale);
            kit.Box(deck,V(0,0,0),new Vector3(24,2,21)*scale,rotation);
            kit.Box(plum,V(0,5,0),new Vector3(20,9,16)*scale,rotation);
            kit.Box(glass,V(0,6,-8.12f),new Vector3(16,3.1f,.15f)*scale,rotation);
            kit.Box(warmWindow,V(0,6,-8.23f),new Vector3(13,.5f,.08f)*scale,rotation);
            kit.Box(steel,V(0,10,0),new Vector3(22,1.2f,18)*scale,rotation);
            kit.Box(cyan,V(0,10.7f,-9.1f),new Vector3(22,.15f,.1f)*scale,rotation);
            for(int i=-1;i<=1;i++)kit.Box(black,V(i*5,11.4f,2),new Vector3(3.7f,1.4f,6)*scale,rotation);
            if(seed%2==0)
            {
                kit.Column(steel,V(-8,10.5f,4),1.2f*scale,.8f*scale,11*scale,8);
                kit.Column(amber,V(-8,21.5f,4),.9f*scale,.9f*scale,.7f*scale,8);
            }
            StructureCount++;
        }
        static void BuildIndustry(Chunks chunks)
        {
            int id=0;
            for(float k=4.1f;k<10;k+=1.3f)
            {
                float d=D(k);var p=RoadPoint(d);var side=S(d);
                int outside=RiverDistance((p+side*50).x,(p+side*50).z)>RiverDistance((p-side*50).x,(p-side*50).z)?1:-1;
                var site=RoadPoint(d,outside*38);var rotation=Quaternion.LookRotation(T(d))*Quaternion.Euler(0,outside<0?180:0,0);
                IndustrialModule(chunks.At(site),site,rotation,id++,.9f);
            }
            // Climbing power conduits sit outside the barriers and conform to the grade.
            for(float d=D(4);d<D(10);d+=6)
            {
                float end=Mathf.Min(d+6,D(10));var p=RoadPoint(d);var side=S(d);
                int outside=RiverDistance((p+side*50).x,(p+side*50).z)>RiverDistance((p-side*50).x,(p-side*50).z)?1:-1;
                var kit=chunks.At(p);
                foreach(float up in new[]{2.7f,4.8f})
                {
                    kit.Beam(steel,RoadPoint(d,outside*17,up),RoadPoint(end,outside*17,up),1.0f,1.0f);
                    kit.Beam(cyan,RoadPoint(d,outside*16.44f,up),RoadPoint(end,outside*16.44f,up),.13f,.13f);
                }
                if(((int)(d-D(4))/6)%5==0)kit.Beam(deck,RoadPoint(d,outside*18,0),RoadPoint(d,outside*18,5.7f),.4f,.4f);
            }
            foreach(float k in new[]{14.8f,18.3f,26.5f,32.1f})
            {
                float d=D(k);var p=RoadPoint(d,-44);var rotation=Quaternion.LookRotation(T(d));
                IndustrialModule(chunks.At(p),p,rotation,id++,.8f);
            }
        }
        static void BuildSignalTowers(Transform parent,Chunks chunks)
        {
            int index=0;
            foreach(float k in new[]{5.7f,12.7f,18.9f,26.8f,32.4f})
            {
                float d=D(k);int side=Outside(d);var frame=Frame(parent,"Canyon power tower "+(++index),d,side*54);
                var kit=new PolygonSceneryKit("Canyon power tower");
                kit.Box(deck,new Vector3(0,0,0),new Vector3(28,2,28));
                kit.Chamfered(plum,Vector3.zero,11,11,8,8,1,62,2.5f);
                kit.Box(steel,new Vector3(0,63,0),new Vector3(19,2,19));
                // A bright animated display faces an approaching car; all geometry is opaque.
                kit.Box(black,new Vector3(0,30,-10.3f),new Vector3(11.5f,37,.7f));
                kit.Box(data,new Vector3(0,30,-10.7f),new Vector3(10,35,.1f));
                foreach(int edge in new[]{-1,1})
                    kit.Beam(cyan,new Vector3(edge*6,11,-10.8f),new Vector3(edge*6,49,-10.8f),.22f,.22f);
                kit.Beam(amber,new Vector3(-8,64,-9.8f),new Vector3(8,64,-9.8f),.3f,.3f);
                kit.Column(steel,new Vector3(0,64,0),2.3f,1.2f,12,8);
                kit.Column(amber,new Vector3(0,76,0),1.4f,1.4f,1,8);
                kit.Emit(frame);StructureCount++;
            }
            // Directional signal fins are set well outside the barrier so the racing line
            // stays clear. Their illuminated faces add depth on the long canyon approaches.
            for(float d=110;d<PolygonCircuit.Length;d+=96)
            {
                float knot=PolygonCircuit.AngleAtDistance(d)/(Mathf.PI*2)*PolygonCircuit.KnotCount;
                if(knot>22.1f&&knot<24.9f)continue;
                var p=RoadPoint(d);var kit=chunks.At(p);var rotation=Quaternion.LookRotation(T(d));
                foreach(int side in new[]{-1,1})
                {
                    var basePoint=RoadPoint(d,side*17.2f);
                    kit.Box(steel,basePoint+Vector3.up*5.5f,new Vector3(.35f,11,.45f),rotation);
                    kit.Box(black,basePoint+Vector3.up*7.3f,new Vector3(2.5f,4.4f,.26f),rotation);
                    kit.Box((side+(int)(d/96))%3==0?violet:cyan,basePoint+Vector3.up*7.3f-T(d)*.16f,new Vector3(1.7f,3.6f,.08f),rotation);
                    kit.Box(amber,basePoint+Vector3.up*11.1f,new Vector3(.55f,.35f,.55f),rotation);
                }
            }
            foreach(float k in new[]{5.2f,8.8f,14.4f,20.5f,27.1f,32.5f})
                Portal(parent,D(k),"Ion signal arch "+k.ToString("0.0"));
        }
        static void BuildViaduct(Chunks chunks,float first,float last,bool high)
        {
            float a=D(first),b=D(last);float crown=high?48:29;
            foreach(float d in new[]{a,b})
            {
                var kit=chunks.At(RoadPoint(d));
                foreach(int side in new[]{-1,1})
                {
                    var top=RoadPoint(d,side*18,crown);var basePoint=top; basePoint.y=GroundHeight(top.x,top.z)-1;
                    kit.Beam(steel,basePoint,top,2.8f,3.8f);
                    kit.Beam(cyan,RoadPoint(d,side*16.5f,4),RoadPoint(d,side*16.5f,crown-1),.16f,.16f);
                    kit.Box(amber,top+Vector3.up*.6f,new Vector3(1,.7f,1));
                }
                kit.Beam(steel,RoadPoint(d,-18,crown-4),RoadPoint(d,18,crown-4),2.4f,2.4f);
            }
            for(float d=a;d<b;d+=8)
            {
                float end=Mathf.Min(d+8,b);var kit=chunks.At(RoadPoint(d));
                float u=(d-a)/(b-a),v=(end-a)/(b-a);
                float y=11+(crown-11)*Mathf.Pow(u*2-1,2),nextY=11+(crown-11)*Mathf.Pow(v*2-1,2);
                foreach(int side in new[]{-1,1})
                {
                    kit.Beam(steel,RoadPoint(d,side*17,y),RoadPoint(end,side*17,nextY),.30f,.30f);
                    if(((int)((d-a)/8))%2==0)
                        kit.Beam(steel,RoadPoint(d,side*15,1.5f),RoadPoint(d,side*17,y),.11f,.11f);
                    kit.Beam(violet,RoadPoint(d,side*DeckEdge,-1.3f),RoadPoint(end,side*DeckEdge,-1.3f),.14f,.14f);
                }
            }
            StructureCount++;
        }
        static void ConduitFalls(Chunks chunks,float z,int side,float top)
        {
            float x=RiverX(z)+side*99;var p=new Vector3(x,0,z);var kit=chunks.At(p);
            // The falls are framed power channels rather than transparent particles.
            float bottom=Floor+2,height=top-bottom;
            for(int strip=0;strip<3;strip++)
            {
                var center=new Vector3(x,(top+bottom)*.5f,z+(strip-1)*7);
                kit.Box(black,center,new Vector3(1.0f,height+3,6));
                kit.Box(data,center+Vector3.right*(-side*.6f),new Vector3(.16f,height,3.3f));
                kit.Beam(steel,new Vector3(x-side*.8f,bottom,z+(strip-1)*7-2.1f),new Vector3(x-side*.8f,top,z+(strip-1)*7-2.1f),.32f,.32f);
            }
            kit.Box(deck,new Vector3(x,top+1,z),new Vector3(12,4,28));
            kit.Box(amber,new Vector3(x,top+3.2f,z),new Vector3(13,.15f,29));StructureCount++;
        }
        static void Arch(PolygonSceneryKit kit,float z,Material material,float width=18,float height=28)
        {
            foreach(int side in new[]{-1,1})
            {
                kit.Beam(material,new Vector3(side*width,0,z),new Vector3(side*width,height-8,z),2.7f,2.7f);
                kit.Beam(material,new Vector3(side*width,height-8,z),new Vector3(side*(width-7),height,z),2.7f,2.7f);
            }
            kit.Beam(material,new Vector3(-width+7,height,z),new Vector3(width-7,height,z),2.7f,2.7f);
        }
        static void Portal(Transform parent,float d,string name)
        {
            var frame=Frame(parent,name,d);var kit=new PolygonSceneryKit(name);
            Arch(kit,0,steel);Arch(kit,3,deck);
            foreach(int side in new[]{-1,1})
            {
                kit.Beam(cyan,new Vector3(side*16.5f,1,-1.5f),new Vector3(side*16.5f,19,-1.5f),.22f,.22f);
                kit.Beam(cyan,new Vector3(side*16.5f,19,-1.5f),new Vector3(side*10,26.5f,-1.5f),.22f,.22f);
            }
            kit.Beam(cyan,new Vector3(-10,26.5f,-1.5f),new Vector3(10,26.5f,-1.5f),.22f,.22f);kit.Emit(frame);StructureCount++;
        }
        static void BuildTunnel(Transform parent,Chunks chunks,float first,float last,string name)
        {
            float a=D(first),b=D(last);
            for(float d=a;d<b;d+=4)
            {
                float end=Mathf.Min(d+4,b);var kit=chunks.At(RoadPoint(d));
                Slab(kit,basaltDark,d,end,-24,-14,0,35);Slab(kit,basaltDark,d,end,14,24,0,35);
                Slab(kit,basalt,d,end,-24,24,24,38);
                foreach(int side in new[]{-1,1})
                {
                    kit.Beam(amber,RoadPoint(d,side*13.8f,11),RoadPoint(end,side*13.8f,11),.19f,.24f);
                    if(((int)(d-a)/4)%7==0)
                        kit.Beam(steel,RoadPoint(d,side*13.8f,1.5f),RoadPoint(d,side*13.8f,23),.38f,.38f);
                }
            }
            Portal(parent,a,name+" - entrance");Portal(parent,b,name+" - exit");
        }
        static void Ring(PolygonSceneryKit kit,Vector3 center,float radius,Material material,float thickness,int sections=48)
        {
            for(int i=0;i<sections;i++)
            {
                float a=i*Mathf.PI*2/sections,b=(i+1)*Mathf.PI*2/sections;
                kit.Beam(material,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,
                    center+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius,thickness,thickness);
            }
        }
        static void Rotor(Transform parent,string name,float radius,float speed)
        {
            var rotor=new GameObject(name).transform;rotor.SetParent(parent,false);var kit=new PolygonSceneryKit(name);
            for(int blade=0;blade<7;blade++)
            {
                var q=Quaternion.Euler(0,0,blade*360f/7);
                kit.Box(steel,q*new Vector3(0,radius*.54f,0),new Vector3(radius*.22f,radius*.80f,.25f),q);
                kit.Box(cyan,q*new Vector3(0,radius*.75f,-.16f),new Vector3(radius*.09f,radius*.36f,.08f),q);
            }
            kit.Box(black,Vector3.zero,new Vector3(radius*.34f,radius*.34f,.5f));kit.Emit(rotor);
            var motion=rotor.gameObject.AddComponent<PolygonIonRiftMotion>();motion.DegreesPerSecond=speed;
        }
        static bool ClearLandmarkFootprint(Vector3 center,Quaternion rotation,float halfWidth,float halfDepth)
        {
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                var p=center+rotation*new Vector3(x*halfWidth,0,z*halfDepth);
                PolygonCircuit.ProjectLapFlat(p,out float offset);
                if(offset<25)return false;
            }
            return true;
        }
        static void BuildGorgeEntrance(Transform parent)
        {
            float d=D(22.25f);var frame=Frame(parent,"Reactor Gorge - rock gateway",d);
            var kit=new PolygonSceneryKit("Reactor Gorge - rock gateway");
            foreach(int side in new[]{-1,1})
            {
                var local=new Vector3(side*37,0,0);
                if(!ClearLandmarkFootprint(frame.TransformPoint(local),frame.rotation,7,6))continue;
                kit.Chamfered(basaltDark,local,7,6,5,4,0,34,2);
                kit.Chamfered(basalt,local+new Vector3(side*3,0,2),4,4,2,3,0,42,1.5f);
                kit.Beam(amber,local+new Vector3(-side*4,5,-6.1f),local+new Vector3(-side*3,25,-4.2f),.22f,.22f);
            }
            kit.Beam(basaltDark,new Vector3(-24,29,0),new Vector3(24,29,0),7,7);
            kit.Beam(cyan,new Vector3(-18,25,-3.6f),new Vector3(18,25,-3.6f),.24f,.24f);
            kit.Emit(frame);StructureCount++;
        }
        static void BuildRiftConduits(Chunks chunks)
        {
            // Elevated cross-canyon connections: reuse opaque materials and keep
            // their supports clear of all track segments, including nearby returns.
            foreach(float knot in new[]{10.3f,31.5f,47.5f})
            {
                float d=D(knot);var center=RoadPoint(d);var right=S(d);
                var kit=chunks.At(center);
                var a=center-right*100+Vector3.up*53;var b=center+right*100+Vector3.up*53;
                for(int pipe=0;pipe<3;pipe++)
                {
                    var shift=T(d)*(pipe-1)*4;
                    kit.Beam(steel,a+shift,b+shift,1.7f,1.7f);
                    kit.Beam(pipe==1?violet:cyan,a+shift+Vector3.up*.95f,b+shift+Vector3.up*.95f,.22f,.22f);
                }
                foreach(int side in new[]{-1,1})
                {
                    var foot=center+right*(side*100);
                    if(!ClearLandmarkFootprint(foot,Quaternion.identity,4,4))continue;
                    float floor=GroundHeight(foot.x,foot.z);
                    var top=foot+Vector3.up*53;foot.y=floor;
                    if(top.y>foot.y)kit.Beam(deck,foot,top,3,3);
                    kit.Box(amber,top,new Vector3(3,.8f,3));
                }
            }
        }
        static void BuildReactor(Transform parent,Chunks chunks)
        {
            BuildTunnel(parent,chunks,22.25f,24.7f,"Reactor Gorge");
            foreach(float k in new[]{22.65f,23.4f,24.15f})
            {
                var frame=Frame(parent,"Reactor tunnel - turbine",D(k),13.85f,9);
                frame.localRotation*=Quaternion.Euler(0,90,0);var kit=new PolygonSceneryKit("Reactor turbine housing");
                kit.Box(black,Vector3.zero,new Vector3(11.8f,11.8f,.25f));Ring(kit,Vector3.back*.25f,5.2f,steel,.45f);
                Ring(kit,Vector3.back*.52f,4.85f,violet,.12f);kit.Emit(frame);
                var spin=new GameObject("Turbine rotor mount").transform;spin.SetParent(frame,false);spin.localPosition=Vector3.back*.75f;
                Rotor(spin,"Reactor turbine",4.4f,25);
            }
            var complex=Frame(parent,"Ion extraction reactor",D(24.5f),-79,-8);var body=new PolygonSceneryKit("Ion extraction reactor");
            body.Box(deck,new Vector3(0,-8,0),new Vector3(64,16,74));
            body.Chamfered(plum,Vector3.zero,27,30,21,24,0,46,7);
            body.Box(steel,new Vector3(0,47,0),new Vector3(48,3,54));
            foreach(int side in new[]{-1,1})
            {
                body.Beam(steel,new Vector3(side*25,0,-31),new Vector3(side*17,44,-25),2.4f,2.4f);
                body.Beam(cyan,new Vector3(side*22,1,-32.5f),new Vector3(side*15,43,-26.5f),.4f,.4f);
                body.Column(deck,new Vector3(side*25,44,16),3.5f,2,39,8);
                body.Column(amber,new Vector3(side*25,83,16),2,2,.9f,8);
            }
            // A distinctive high-energy crown remains visible beyond the immediate tunnel.
            body.Column(deck,new Vector3(0,47,0),7,5,45,10);
            body.Column(steel,new Vector3(0,92,0),5,2,20,10);
            body.Column(cyan,new Vector3(0,112,0),2.2f,2.2f,2,8);
            foreach(int side in new[]{-1,1})
            {
                body.Beam(steel,new Vector3(side*25,48,-9),new Vector3(side*7,94,-9),2,2);
                body.Beam(violet,new Vector3(side*24,50,-10.1f),new Vector3(side*6,93,-10.1f),.34f,.34f);
            }
            Ring(body,new Vector3(0,89,-9),9.5f,cyan,.7f,32);
            Ring(body,new Vector3(0,25,-30.6f),15.5f,steel,2,64);
            Ring(body,new Vector3(0,25,-31.7f),13.5f,cyan,.7f,64);
            body.Box(data,new Vector3(0,25,-30.8f),new Vector3(16,16,.25f));body.Emit(complex);
            var mount=new GameObject("Reactor core rotor mount").transform;mount.SetParent(complex,false);mount.localPosition=new Vector3(0,25,-32.1f);
            Rotor(mount,"Slow reactor core",12.2f,8);StructureCount++;
        }
        static void BuildBeacons(Transform parent,Chunks chunks)
        {
            for(float d=28;d<PolygonCircuit.Length;d+=42)
            {
                var kit=chunks.At(RoadPoint(d));float curve=PolygonCircuit.Curvature(d+16);
                foreach(int side in new[]{-1,1})
                {
                    kit.Beam(steel,RoadPoint(d,side*14.5f,0),RoadPoint(d,side*14.5f,4.2f),.16f,.16f);
                    kit.Box(amber,RoadPoint(d,side*14.5f,4.25f),new Vector3(.45f,.45f,.45f));
                    if(Mathf.Abs(curve)>.0043f&&side==(curve>0?-1:1))
                    {
                        var center=RoadPoint(d,side*(Barrier+.1f),2.2f);var forward=T(d);var right=S(d);float turn=curve>0?1:-1;
                        kit.Box(black,center,new Vector3(3.6f,1.6f,.15f),Quaternion.LookRotation(forward));
                        kit.Beam(amber,center-right*turn*.6f+Vector3.up*.5f-forward*.1f,center+right*turn*.3f-forward*.1f,.14f,.14f);
                        kit.Beam(amber,center+right*turn*.3f-forward*.1f,center-right*turn*.6f-Vector3.up*.5f-forward*.1f,.14f,.14f);
                    }
                }
            }
            Sign(parent,"CLIFF ASCENT",D(4),amber);Sign(parent,"RIFT VIADUCT",D(14.4f),cyan);
            Sign(parent,"REACTOR GORGE",D(21.7f),amber);Sign(parent,"RIM STATION",D(32.2f),cyan);
        }
        static void Sign(Transform parent,string text,float d,Material accent)
        {
            var frame=Frame(parent,text+" route sign",d);var kit=new PolygonSceneryKit("Ion route sign");
            kit.Box(black,new Vector3(0,10.3f,0),new Vector3(19,2.1f,.4f));
            foreach(int side in new[]{-1,1})kit.Beam(steel,new Vector3(side*14.5f,0,0),new Vector3(side*14.5f,11,0),.5f,.5f);
            kit.Beam(steel,new Vector3(-14.5f,11,0),new Vector3(14.5f,11,0),.5f,.5f);
            kit.Box(accent,new Vector3(0,11.4f,-.25f),new Vector3(19,.14f,.09f));kit.Emit(frame);
            Label(frame,text+" lettering",text,new Vector3(0,10.3f,-.24f),.12f,accent.color);
        }
        // Second polish pass (October 29, 2026): shared neon trackside dressing in the canyon's
        // cyan/amber palette. Tunnels and the start are skipped; viaducts keep their own
        // superstructure, so no posts or overhead pieces are added there.
        static void BuildTrackside(Chunks chunks)
        {
            float length=PolygonCircuit.Length;
            float[] overhead={D(4),D(14.4f),D(21.7f),D(32.2f),D(5.2f),D(8.8f),D(20.5f),D(27.1f),D(32.5f),
                              D(6.1f),D(6.7f),D(22.25f),D(24.7f)};
            var style=new PolygonNeonTracksideStyle
            {
                Prefix="Ion Rift",KitAt=p=>chunks.At(p),Dark=black,Steel=steel,
                KerbA=Mat("Kerb amber glow",new Color(1,.42f,.06f),1.6f,.3f),
                KerbB=Mat("Kerb pale",new Color(.80f,.82f,.88f),.2f,.3f),
                BoardA=PolygonNeonTrackside.Panel("IR LED board cyan",new Color(.04f,1.4f,2.3f),.5f,1.5f),
                BoardB=PolygonNeonTrackside.Panel("IR LED board amber",new Color(2.2f,.72f,.07f),-.35f,1.2f),
                Accents=new[]{cyan,amber,violet},
                SkipAt=d=>d<45||d>length-40||(d>D(6.1f)-15&&d<D(6.7f)+15)||(d>D(22.25f)-15&&d<D(24.7f)+15),
                PostBlocked=(d,side)=>
                {
                    if((d>D(15.6f)-12&&d<D(18.3f)+12)||(d>D(28.6f)-12&&d<D(30.7f)+12))return true;   // viaducts
                    foreach(float o in overhead)if(Mathf.Abs(d-o)<10)return true;                      // portals and signs
                    float fin=Mathf.Repeat(d-110,96);if(fin<6||fin>90)return true;                       // signal fins at 17.2 m
                    return false;
                }
            };
            PolygonNeonTrackside.Build(style);
        }

        public static Transform Build(Transform root)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.IonRift)throw new InvalidOperationException("Select Ion Rift before building the canyon.");
            Palette();PrepareTerrain();PreparePads();StructureCount=RockCount=0;
            var parent=new GameObject(SceneryName).transform;parent.SetParent(root,false);var chunks=new Chunks();
            BuildRoad(chunks);BuildTerrain(chunks);BuildCliffs(chunks);BuildStart(parent);BuildIndustry(chunks);BuildSignalTowers(parent,chunks);
            BuildViaduct(chunks,15.6f,18.3f,true);BuildViaduct(chunks,28.6f,30.7f,false);
            BuildTunnel(parent,chunks,6.1f,6.7f,"Conduit rock passage");
            BuildReactor(parent,chunks);BuildGorgeEntrance(parent);BuildRiftConduits(chunks);
            ConduitFalls(chunks,715,1,91);ConduitFalls(chunks,430,-1,75);ConduitFalls(chunks,70,1,47);
            BuildBeacons(parent,chunks);BuildTrackside(chunks);chunks.Emit(parent);
            Debug.Log($"Ion Rift canyon: {RockCount} faceted rock formations, {StructureCount} industrial structures. Opaque scenery, shared materials, no extra local real-time lights.");
            return parent;
        }
    }
}
