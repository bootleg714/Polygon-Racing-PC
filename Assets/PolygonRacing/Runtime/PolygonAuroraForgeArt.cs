using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Editor-time canyon construction. Road, tunnels and bridges follow the exact race spline.
    // Rock silhouettes and a few large industrial landmarks carry detail without a dense city.
    public static class PolygonAuroraForgeArt
    {
        public const string SceneryName="Circuit - editable scenery";
        public const string AnimatedName="Aurora Forge - animated machinery";
        public const float Barrier=12f,DeckEdge=13.2f;
        const float ChunkSize=220,Floor=-35;
        const int TerrainStep=26,TerrainMinX=-1120,TerrainMinZ=-620,TerrainCellsX=89,TerrainCellsZ=80;
        static float[,] terrainGrid;
        static Material road,deck,steel,black,paint,basalt,basaltLight,basaltDark,dust,
            cyan,amber,violet,warmWindow,glass,energy,data,plum,forgeHeat,barrierGlow;
        static Vector3[] samples;
        static float[] sampleHeights;
        static Vector3 lakeCenter;
        static Quaternion lakeInverse;
        static float lakeLevel;
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
                {kit=new PolygonSceneryKit("Aurora ice "+key.x+","+key.y){MaxVerticesPerMesh=60000};kits.Add(key,kit);}
                return kit;
            }
            public void Emit(Transform parent)
            {
                foreach(var pair in kits)
                {
                    var group=new GameObject("Glacier sector "+pair.Key.x+","+pair.Key.y).transform;
                    group.SetParent(parent,false);pair.Value.Emit(group);
                }
            }
        }
        static Material Mat(string name,Color color,float emission=0,float smoothness=.16f)
        {
            var shader=Resources.Load<Shader>("PolygonFlatPC");if(!shader)shader=PolygonLook.LitShader();
            var m=new Material(shader){name="AF "+name,color=color,enableInstancing=true};
            m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",0);m.SetFloat("_ReflectionStrength",.18f);
            m.SetColor("_SkyAmbient",new Color(.38f,.37f,.61f));m.SetColor("_GroundAmbient",new Color(.19f,.14f,.22f));
            m.SetFloat("_AmbientStrength",1);m.SetFloat("_SunWrap",.2f);
            if(emission>0)m.SetColor("_EmissionColor",color*emission);
            return m;
        }
        static void Palette()
        {
            // Lighting pass (Oct 29): wet / icy asphalt that mirrors the neon via the baked probes
            // (PolygonNeonReflections); no moon highlight stripe.
            road=Mat("Asphalt",new Color(.09f,.11f,.15f),0,.86f);road.SetFloat("_ReflectionStrength",.45f);road.SetFloat("_DirectSpecular",0);
            // Same look as the cyan conduit, but named so the probe bake leaves the barrier light out.
            barrierGlow=Mat("Barrier glow",new Color(.035f,.85f,1),3.0f);
            deck=Mat("Bridge graphite",new Color(.20f,.24f,.31f));
            steel=Mat("Pale alloy",new Color(.52f,.60f,.68f),0,.42f);
            black=Mat("Black recess",new Color(.035f,.065f,.09f));
            paint=Mat("Road paint",new Color(.79f,.85f,.88f),.12f);
            basalt=Mat("Basalt",new Color(.64f,.79f,.88f));
            basaltLight=Mat("Basalt exposed strata",new Color(.82f,.91f,.95f));
            basaltDark=Mat("Basalt shadow strata",new Color(.30f,.48f,.60f));
            dust=Mat("Rim dust",new Color(.86f,.92f,.94f));
            cyan=Mat("Cyan conduit",new Color(.035f,.85f,1),3.0f);
            amber=Mat("Amber beacon",new Color(1,.39f,.055f),2.4f);
            violet=Mat("Violet edge",new Color(.20f,.94f,.62f),2.4f);
            warmWindow=Mat("Warm control room",new Color(1,.65f,.27f),1.3f);
            glass=Mat("Control room glass",new Color(.06f,.11f,.17f),0,.5f);
            plum=Mat("Reactor cladding",new Color(.22f,.34f,.43f));
            energy=Mat("Frozen glacial river",new Color(.30f,.67f,.78f),.25f,.5f);
            forgeHeat=Mat("Forge warm glow",new Color(1f,.26f,.045f),2f);
            var panel=Resources.Load<Shader>("PolygonNeonPanel");
            if(!panel)throw new InvalidOperationException("PolygonNeonPanel.shader is missing.");
            data=new Material(panel){name="AF Cascading conduit",enableInstancing=true};
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
            terrainGrid=null;
            const int n=512;samples=new Vector3[n];sampleHeights=new float[n];
            for(int i=0;i<n;i++)
            {float d=PolygonCircuit.Length*i/n;samples[i]=PolygonCircuit.AtDistance(d);sampleHeights[i]=PolygonCircuit.Elevation(d);}
            float lakeD=D(13f);lakeCenter=RoadPoint(lakeD,200);
            lakeInverse=Quaternion.Inverse(Quaternion.LookRotation(T(lakeD)));
            lakeLevel=lakeCenter.y-12;
        }
        static float LakeWeight(Vector3 point)
        {
            var local=lakeInverse*(point-lakeCenter);
            float r=Mathf.Sqrt(local.x*local.x/(120f*120f)+local.z*local.z/(270f*270f));
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1f,r));
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
            if(terrainGrid!=null)
            {
                float gx=(x-TerrainMinX)/TerrainStep,gz=(z-TerrainMinZ)/TerrainStep;
                if(gx>=0&&gx<=TerrainCellsX&&gz>=0&&gz<=TerrainCellsZ)
                {
                    int ix=Mathf.Min(Mathf.FloorToInt(gx),TerrainCellsX-1);
                    int iz=Mathf.Min(Mathf.FloorToInt(gz),TerrainCellsZ-1);
                    float a=Mathf.Lerp(terrainGrid[ix,iz],terrainGrid[ix+1,iz],gx-ix);
                    float b=Mathf.Lerp(terrainGrid[ix,iz+1],terrainGrid[ix+1,iz+1],gx-ix);
                    return Mathf.Lerp(a,b,gz-iz);
                }
            }
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
            // Shape one broad frozen basin, keeping every road segment's shelf clear.
            float lake=LakeWeight(p)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(45,75,Mathf.Sqrt(nearest)));
            if(lake>0)result=Mathf.Lerp(result,lakeLevel,lake);
            // Soften the raised shelf between the opening straight and nearby return.
            // A hard switch between their nearest samples produced a sheer triangle.
            float opening=D(.85f);var shelfCenter=RoadPoint(opening,-100);
            var localShelf=Quaternion.Inverse(Quaternion.LookRotation(T(opening)))*(p-shelfCenter);
            float shelfRadius=Mathf.Sqrt(localShelf.x*localShelf.x/(100f*100f)+localShelf.z*localShelf.z/(300f*300f));
            float shelfFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1f,shelfRadius));
            if(shelfFade>0)result=Mathf.Lerp(result,Mathf.Min(result,RoadPoint(opening).y-7),shelfFade);
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
                    kit.Beam(barrierGlow,RoadPoint(d,side*(Barrier-.03f),1.06f),RoadPoint(end,side*(Barrier-.03f),1.06f),.11f,.12f);
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
        static void PrepareTerrainGrid()
        {
            // Lower only anomalous spikes, with a bounded rise per terrain cell.
            // Two directional sweeps propagate the gentler shelf over the whole grid.
            var grid=new float[TerrainCellsX+1,TerrainCellsZ+1];
            for(int ix=0;ix<=TerrainCellsX;ix++)for(int iz=0;iz<=TerrainCellsZ;iz++)
                grid[ix,iz]=GroundHeight(TerrainMinX+ix*TerrainStep,TerrainMinZ+iz*TerrainStep);
            float maxStep=TerrainStep*.8f;
            for(int ix=0;ix<=TerrainCellsX;ix++)for(int iz=0;iz<=TerrainCellsZ;iz++)
            {
                if(ix>0)grid[ix,iz]=Mathf.Min(grid[ix,iz],grid[ix-1,iz]+maxStep);
                if(iz>0)grid[ix,iz]=Mathf.Min(grid[ix,iz],grid[ix,iz-1]+maxStep);
            }
            for(int ix=TerrainCellsX;ix>=0;ix--)for(int iz=TerrainCellsZ;iz>=0;iz--)
            {
                if(ix<TerrainCellsX)grid[ix,iz]=Mathf.Min(grid[ix,iz],grid[ix+1,iz]+maxStep);
                if(iz<TerrainCellsZ)grid[ix,iz]=Mathf.Min(grid[ix,iz],grid[ix,iz+1]+maxStep);
            }
            float steepest=0;
            for(int ix=0;ix<=TerrainCellsX;ix++)for(int iz=0;iz<=TerrainCellsZ;iz++)
            {
                if(ix>0)steepest=Mathf.Max(steepest,Mathf.Abs(grid[ix,iz]-grid[ix-1,iz]));
                if(iz>0)steepest=Mathf.Max(steepest,Mathf.Abs(grid[ix,iz]-grid[ix,iz-1]));
            }
            if(steepest>maxStep+.01f)throw new InvalidOperationException("Aurora terrain still has an abrupt height step: "+steepest);
            terrainGrid=grid;
            Debug.Log($"AURORA_TERRAIN_SLOPE_OK: max neighbouring rise {steepest:0.0} m per {TerrainStep} m");
        }
        static void BuildTerrain(Chunks chunks)
        {
            for(int ix=0;ix<TerrainCellsX;ix++)for(int iz=0;iz<TerrainCellsZ;iz++)
            {
                int x=TerrainMinX+ix*TerrainStep,z=TerrainMinZ+iz*TerrainStep;
                var a=new Vector3(x,terrainGrid[ix,iz],z);var b=new Vector3(x+TerrainStep,terrainGrid[ix+1,iz],z);
                var c=new Vector3(x+TerrainStep,terrainGrid[ix+1,iz+1],z+TerrainStep);var d=new Vector3(x,terrainGrid[ix,iz+1],z+TerrainStep);
                var mid=(a+b+c+d)*.25f;
                var material=LakeWeight(mid)>.72f?energy:((x/TerrainStep+z/TerrainStep)&7)==0?basaltLight:basalt;
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
                renderer.sharedMaterial=new Material(shader){name="AF Depth tested lettering",mainTexture=renderer.sharedMaterial.mainTexture};
            }
        }
        static void BuildStart(Transform parent)
        {
            var frame=Frame(parent,"Aurora Base - start gantry",5);var kit=new PolygonSceneryKit("Aurora start gantry");
            foreach(int side in new[]{-1,1})
            {
                kit.Beam(steel,new Vector3(side*16,0,0),new Vector3(side*14,10.5f,0),1.6f,1.6f);
                kit.Beam(amber,new Vector3(side*15.1f,.5f,-.9f),new Vector3(side*13.1f,10,-.9f),.17f,.17f);
            }
            kit.Box(black,new Vector3(0,11.9f,0),new Vector3(29,3.5f,1.7f));
            kit.Box(cyan,new Vector3(0,13.75f,-.9f),new Vector3(29,.17f,.11f));kit.Emit(frame);
            Label(frame,"Aurora Forge lettering","AURORA FORGE",new Vector3(0,12.25f,-.91f),.21f,new Color(.3f,.95f,1));
            var signal=frame.gameObject.AddComponent<PolygonStartLights>();signal.DarkMaterial=black;
            signal.RedMaterial=Mat("Countdown red",new Color(1,.02f,.025f),4);signal.Lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)signal.Lenses[i]=PolygonWorld.Box("Countdown signal "+i,frame,new Vector3((i-2)*1.8f,10.85f,-.97f),new Vector3(1.2f,.55f,.14f),black).GetComponent<MeshRenderer>();
            var station=Frame(parent,"Aurora Base - team bays",D(1.5f),-39);var building=new PolygonSceneryKit("Aurora Base");
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
                float d=D(k);int side=Outside(d);var frame=Frame(parent,"Aurora power tower "+(++index),d,side*54);
                var kit=new PolygonSceneryKit("Aurora power tower");
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
                    if(name=="Forge Cavern"&&((int)((d-a)/4))%6==0)
                        kit.Beam(forgeHeat,RoadPoint(d,side*13.75f,4),RoadPoint(d,side*13.75f,8),.4f,.4f);
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
            var motion=rotor.gameObject.AddComponent<PolygonAuroraForgeMotion>();motion.DegreesPerSecond=speed;
        }
        static void BuildReactor(Transform parent,Chunks chunks)
        {
            BuildTunnel(parent,chunks,22.25f,24.7f,"Forge Cavern");
            foreach(float k in new[]{22.65f,23.4f,24.15f})
            {
                var frame=Frame(parent,"Reactor tunnel - turbine",D(k),13.85f,9);
                frame.localRotation*=Quaternion.Euler(0,90,0);var kit=new PolygonSceneryKit("Reactor turbine housing");
                kit.Box(black,Vector3.zero,new Vector3(11.8f,11.8f,.25f));Ring(kit,Vector3.back*.25f,5.2f,steel,.45f);
                Ring(kit,Vector3.back*.52f,4.85f,violet,.12f);kit.Emit(frame);
                var spin=new GameObject("Turbine rotor mount").transform;spin.SetParent(frame,false);spin.localPosition=Vector3.back*.75f;
                Rotor(spin,"Reactor turbine",4.4f,25);
            }
            var complex=Frame(parent,"Aurora Forge complex",D(24.5f),-79,-8);var body=new PolygonSceneryKit("Aurora Forge complex");
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
            Ring(body,new Vector3(0,25,-30.6f),15.5f,steel,2,64);
            Ring(body,new Vector3(0,25,-31.7f),13.5f,cyan,.7f,64);
            body.Box(forgeHeat,new Vector3(0,25,-30.8f),new Vector3(16,16,.25f));
            foreach(int side in new[]{-1,1})
            {
                body.Box(black,new Vector3(side*15,16,-30.6f),new Vector3(7,15,.3f));
                body.Box(forgeHeat,new Vector3(side*15,16,-30.82f),new Vector3(5.5f,13,.08f));
                body.Beam(amber,new Vector3(side*22,3,-32.1f),new Vector3(side*22,41,-32.1f),.32f,.32f);
            }
            body.Emit(complex);
            var mount=new GameObject("Reactor core rotor mount").transform;mount.SetParent(complex,false);mount.localPosition=new Vector3(0,25,-32.1f);
            Rotor(mount,"Slow reactor core",12.2f,8);StructureCount++;
        }
        static void BuildIceLandmarks(Chunks chunks)
        {
            foreach(float knot in new[]{10.8f,17.8f,31.8f})
            {
                float d=D(knot);int side=Outside(d);var site=RoadPoint(d,side*105);
                if(!ClearRock(site,22))continue;
                site.y=GroundHeight(site.x,site.z);var kit=chunks.At(site);
                for(int i=0;i<3;i++)
                {
                    var p=site+T(d)*(i-1)*9+S(d)*side*(i==1?5:0);
                    float h=28+i*9;
                    kit.Chamfered(i==1?basaltLight:basalt,p,5,4,1.2f,1.2f,0,h,1);
                    kit.Beam(cyan,p+Vector3.up*2,p+Vector3.up*(h-1),.16f,.16f);
                }
            }
        }
        static void BuildMiningRig(Chunks chunks)
        {
            // A static silhouette: no particles, realtime lights or moving machinery.
            foreach(float knot in new[]{17.8f,19.2f,27.4f,30.8f})
            {
                float d=D(knot);int side=Outside(d);var p=RoadPoint(d,side*125);
                if(!ClearRock(p,36))continue;
                p.y=GroundHeight(p.x,p.z);var rot=Quaternion.LookRotation(T(d));
                Vector3 V(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
                var kit=chunks.At(p);
                foreach(int s in new[]{-1,1})kit.Box(black,V(s*14,2,0),new Vector3(8,4,35),rot);
                kit.Box(deck,V(0,6,0),new Vector3(32,5,34),rot);
                kit.Box(plum,V(-3,14,-3),new Vector3(24,12,22),rot);
                kit.Box(warmWindow,V(-3,17,-14.1f),new Vector3(15,2,.15f),rot);
                kit.Box(steel,V(-3,21,-3),new Vector3(26,2,24),rot);
                kit.Beam(steel,V(5,18,0),V(5,39,28),3,3);
                kit.Beam(amber,V(5,20,-.4f),V(5,40,27.6f),.24f,.24f);
                FrameForMiningWheel(kit,V(5,39,28),rot);
                StructureCount++;return;
            }
        }
        static void FrameForMiningWheel(PolygonSceneryKit kit,Vector3 center,Quaternion rotation)
        {
            const int segments=16;float radius=8;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Vector3 A(float t)=>center+rotation*new Vector3(0,Mathf.Cos(t)*radius,Mathf.Sin(t)*radius);
                kit.Beam(steel,A(a),A(b),1.4f,1.4f);
                if(i%2==0)kit.Beam(steel,center,A(a),.7f,.7f);
            }
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
            Sign(parent,"ICE ASCENT",D(4),amber);Sign(parent,"GLACIER BRIDGE",D(14.4f),cyan);
            Sign(parent,"FORGE CAVERN",D(21.7f),amber);Sign(parent,"AURORA BASE",D(32.2f),cyan);
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
        static void BuildAurora(Transform parent)
        {
            var shader=Resources.Load<Shader>("PolygonAuroraRibbon");
            if(!shader)throw new InvalidOperationException("PolygonAuroraRibbon.shader must finish importing.");
            var material=new Material(shader){name="AF Northern lights",enableInstancing=true};
            var kit=new PolygonSceneryKit("Aurora curtains");
            // Four distant ribbons surround the circuit, with two-sided transparent faces.
            // Their shader animates broad vertical bands without runtime objects or textures.
            for(int side=0;side<4;side++)for(int i=0;i<36;i++)
            {
                float a=-2200+i*4400f/36,b=-2200+(i+1)*4400f/36;
                Vector3 P(float x,float y)
                {
                    float wave=Mathf.Sin(x*.004f+side*1.7f)*120;
                    if(side==0)return new Vector3(x,y,1650+wave);
                    if(side==1)return new Vector3(x,y,-1200-wave);
                    if(side==2)return new Vector3(1800+wave,y,x);
                    return new Vector3(-1800-wave,y,x);
                }
                var p=P(a,280);var q=P(b,280);var r=P(b,850);var s=P(a,850);
                kit.Quad(material,p,q,r,s,(p+q+r+s)*.25f+Vector3.up);
            }
            var sky=new GameObject("Northern lights").transform;sky.SetParent(parent,false);kit.Emit(sky);
        }
        // Second polish pass (October 29, 2026): shared neon trackside dressing in the Arctic
        // palette (ice cyan, aurora green, forge amber). Tunnels and the start are skipped;
        // viaducts keep their own superstructure, so no posts or overhead pieces go there.
        static void BuildTrackside(Chunks chunks)
        {
            float length=PolygonCircuit.Length;
            float[] overhead={D(4),D(14.4f),D(21.7f),D(32.2f),D(5.2f),D(8.8f),D(20.5f),D(27.1f),D(32.5f),
                              D(6.1f),D(6.7f),D(22.25f),D(24.7f)};
            var style=new PolygonNeonTracksideStyle
            {
                Prefix="Aurora Forge",KitAt=p=>chunks.At(p),Dark=black,Steel=steel,
                KerbA=Mat("Kerb ice glow",new Color(.08f,.78f,1),1.5f,.35f),
                KerbB=Mat("Kerb snow white",new Color(.90f,.95f,.98f),.25f,.35f),
                BoardA=PolygonNeonTrackside.Panel("AF LED board ice",new Color(.05f,1.3f,2.3f),.45f,1.5f),
                BoardB=PolygonNeonTrackside.Panel("AF LED board aurora",new Color(.12f,2.0f,1.0f),-.3f,1.2f),
                Accents=new[]{cyan,violet,amber},
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

        // Skyline pass (October 29, 2026): distant ice megaspires, a forge sky beam and slow cargo lanes.
        static void BuildSkyline(Transform parent,Chunks chunks)
        {
            PolygonNeonSkyline.Build(new PolygonNeonSkylineStyle
            {
                Prefix="Aurora Forge",Parent=parent,KitAt=p=>chunks.At(p),
                Body=basalt,Steel=steel,Glass=glass,Panel=data,Window=cyan,
                SkyBeam=Mat("Sky beam",new Color(1,.42f,.08f),2.6f),
                TailLight=Mat("Craft tail light",new Color(1,.08f,.12f),3.2f),
                HeadLight=Mat("Craft head light",new Color(1,.92f,.78f),2.6f),
                Accents=new[]{cyan,violet,amber},
                Megastructures=7,MegaMinHeight=340,MegaMaxHeight=620,MegaRingMin=320,MegaRingMax=680,
                SkyBeams=2,Lanes=3,CraftPerLane=10,LaneLift=220
            });
        }

        public static Transform Build(Transform root)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.AuroraForge)throw new InvalidOperationException("Select Aurora Forge before building the canyon.");
            Palette();PrepareTerrain();PreparePads();PrepareTerrainGrid();StructureCount=RockCount=0;
            var parent=new GameObject(SceneryName).transform;parent.SetParent(root,false);var chunks=new Chunks();
            BuildRoad(chunks);BuildTerrain(chunks);BuildCliffs(chunks);BuildStart(parent);BuildIndustry(chunks);BuildSignalTowers(parent,chunks);
            BuildViaduct(chunks,15.6f,18.3f,true);BuildViaduct(chunks,28.6f,30.7f,false);
            BuildTunnel(parent,chunks,6.1f,6.7f,"Conduit rock passage");
            BuildReactor(parent,chunks);BuildIceLandmarks(chunks);BuildMiningRig(chunks);
            ConduitFalls(chunks,715,1,91);ConduitFalls(chunks,430,-1,75);ConduitFalls(chunks,70,1,47);
            BuildBeacons(parent,chunks);BuildTrackside(chunks);BuildSkyline(parent,chunks);chunks.Emit(parent);BuildAurora(parent);
            Debug.Log($"Aurora Forge canyon: {RockCount} faceted rock formations, {StructureCount} industrial structures. Opaque scenery, shared materials, no extra local real-time lights.");
            return parent;
        }
    }
}
