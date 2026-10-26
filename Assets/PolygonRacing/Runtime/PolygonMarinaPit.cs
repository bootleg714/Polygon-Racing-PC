using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Optional branch of the coastal straight. Race distance maps to the main route.
    public static class PolygonMarinaPit
    {
        public const float HalfWidth=5.5f,SpeedLimit=80f/3.6f;
        const int Samples=192;
        static readonly Vector3[] points=new Vector3[Samples+1];
        static readonly float[] distances=new float[Samples+1];
        static bool ready;
        static float cachedLength;
        public static float Entry=>PolygonCircuit.DistanceAtKnot(11.2f);
        public static float Exit=>PolygonCircuit.DistanceAtKnot(14.9f);
        public static float Offset(float d)
        {
            if(d<=Entry||d>=Exit)return 0;
            return 42*Mathf.Min(Mathf.SmoothStep(0,1,(d-Entry)/100),Mathf.SmoothStep(0,1,(Exit-d)/100));
        }
        public static Vector3 Point(float d)
        {float a=PolygonCircuit.AngleAtDistance(d);return PolygonWorld.Point(a)+PolygonWorld.Side(a)*Offset(d);}
        static void Prepare()
        {
            if(ready&&Mathf.Abs(cachedLength-PolygonCircuit.Length)<.01f)return;
            for(int i=0;i<=Samples;i++){distances[i]=Mathf.Lerp(Entry,Exit,i/(float)Samples);points[i]=Point(distances[i]);}
            cachedLength=PolygonCircuit.Length;ready=true;
        }
        public static bool TrySample(Vector3 p,float mainOffset,out Vector3 center,out float offset,out float lap)
        {
            center=Vector3.zero;offset=0;lap=0;
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)return false;
            Prepare();p.y=0;float best=float.PositiveInfinity,d=0;
            for(int i=0;i<Samples;i++)
            {
                var ab=points[i+1]-points[i];float t=Mathf.Clamp01(Vector3.Dot(p-points[i],ab)/Mathf.Max(ab.sqrMagnitude,.00001f));
                var q=points[i]+ab*t;float sq=(p-q).sqrMagnitude;
                if(sq<best){best=sq;center=q;d=Mathf.Lerp(distances[i],distances[i+1],t);}
            }
            offset=Mathf.Sqrt(best);lap=d/PolygonCircuit.Length;
            return offset-HalfWidth<mainOffset-PolygonWorld.RoadHalfWidth;
        }
        public static float ProjectRaceLap(Vector3 p,out float offset)
        {
            float lap=PolygonCircuit.ProjectLap(p,out offset);
            if(TrySample(p,offset,out _,out float branchOffset,out float branchLap))
            {offset=branchOffset+PolygonWorld.RoadHalfWidth-HalfWidth;return branchLap;}
            return lap;
        }

        sealed class Batch
        {
            public Material material;
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<int> triangles=new List<int>();
        }
        static readonly Dictionary<Material,Batch> batches=new Dictionary<Material,Batch>();
        static void Band(float a,float b,float loA,float hiA,float loB,float hiB,Material mat)
        {
            if(hiA<=loA&&hiB<=loB)return;
            if(!batches.TryGetValue(mat,out var batch)){batch=new Batch{material=mat};batches.Add(mat,batch);}
            float aa=PolygonCircuit.AngleAtDistance(a),bb=PolygonCircuit.AngleAtDistance(b);
            var pa=PolygonWorld.Point(aa)+Vector3.up*.045f;var pb=PolygonWorld.Point(bb)+Vector3.up*.045f;
            var sa=PolygonWorld.Side(aa);var sb=PolygonWorld.Side(bb);int n=batch.vertices.Count;
            var v0=pa+sa*loA+Vector3.up*PolygonBanking.Height(a,loA);
            var v1=pa+sa*hiA+Vector3.up*PolygonBanking.Height(a,hiA);
            var v2=pb+sb*hiB+Vector3.up*PolygonBanking.Height(b,hiB);
            var v3=pb+sb*loB+Vector3.up*PolygonBanking.Height(b,loB);
            batch.vertices.AddRange(new[]{v0,v1,v2,v3});
            if(Vector3.Cross(v1-v0,v2-v0).y>0)batch.triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            else batch.triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        public static void BuildScenery(Transform world)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)return;
            batches.Clear();
            // Remove the layered road and paint, including the prior curb overlays.
            string[] names={"High-speed road","Left road edge","Right road edge","Left paved shoulder","Right paved shoulder","Highway lane marker",
                "Coral outside curb stripes","Ivory curb stripes","Teal inside curb stripes"};
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(!child||child==world)continue;
                foreach(string name in names)if(child.name==name){Object.DestroyImmediate(child.gameObject);break;}
            }
            // Clear old scenery from the new lane and its garage apron.
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(!child)continue;
                if(child.name!="Sculpted palm cluster tree"&&child.name!="Beach sandstone outcrop")continue;
                PolygonCircuit.ProjectLap(child.position,out float mainOffset);
                if(TrySample(child.position,mainOffset,out _,out float off,out _)&&off<26)Object.DestroyImmediate(child.gameObject);
            }
            var road=PolygonWorld.Paint("Marina road",new Color(.14f,.19f,.23f));
            var pit=PolygonWorld.Paint("Marina lane",new Color(.24f,.32f,.34f));
            var white=PolygonWorld.Paint("CE Art Warm ivory",new Color(1,.94f,.78f));
            var coral=PolygonWorld.Paint("CE Art Festival coral",new Color(.98f,.32f,.32f));
            var teal=PolygonWorld.Paint("CE Art Festival teal",new Color(.025f,.43f,.48f));
            var shoulder=PolygonWorld.Paint("Marina pale shoulder",new Color(.68f,.61f,.45f));
            var boundaries=new SortedSet<float>{0,PolygonCircuit.Length,Entry,Exit,Entry+100,Exit-100,PolygonBanking.Start,PolygonBanking.Start+100,PolygonBanking.End-100,PolygonBanking.End};
            for(float d=4;d<PolygonCircuit.Length;d+=4)boundaries.Add(d);
            for(float d=18;d<PolygonCircuit.Length;d+=38){boundaries.Add(d);boundaries.Add(Mathf.Min(d+10,PolygonCircuit.Length));}
            // Insert exactly where the branch reaches the outer edge of the main shoulder.
            for(int i=1;i<100;i++)
            {
                float a=Entry+i-1,b=Entry+i;
                if(Offset(a)+HalfWidth<16&&Offset(b)+HalfWidth>=16){float d=Mathf.Lerp(a,b,Mathf.InverseLerp(Offset(a),Offset(b),16-HalfWidth));boundaries.Add(d);boundaries.Add(Exit-(d-Entry));break;}
            }
            var ds=new List<float>(boundaries);float w=PolygonWorld.RoadHalfWidth;
            for(int i=0;i<ds.Count-1;i++)
            {
                float a=ds[i],b=ds[i+1],mid=(a+b)*.5f;
                Band(a,b,-w,-w+.3f,-w,-w+.3f,road);
                Band(a,b,-w+.3f,-w+.58f,-w+.3f,-w+.58f,white);
                Band(a,b,-w+.58f,-.07f,-w+.58f,-.07f,road);
                bool dash=mid>=18&&Mathf.Repeat(mid-18,38)<10;
                Band(a,b,-.07f,.07f,-.07f,.07f,dash?white:road);
                Band(a,b,.07f,w-.58f,.07f,w-.58f,road);
                Band(a,b,w-.58f,w-.3f,w-.58f,w-.3f,white);
                Band(a,b,w-.3f,w,w-.3f,w,road);
                float curve=PolygonCircuit.Curvature(mid);bool curved=Mathf.Abs(curve)>.0028f;
                foreach(int side in new[]{-1,1})
                {
                    Material curb=shoulder;
                    if(curved)curb=Mathf.FloorToInt(mid/4)%2==1?white:side==(curve>0?-1:1)?coral:teal;
                    float lo=side<0?-16:w,hi=side<0?-w:16;
                    Band(a,b,lo,side<0?-w-1.45f:w+.25f,lo,side<0?-w-1.45f:w+.25f,shoulder);
                    Band(a,b,side<0?-w-1.45f:w+.25f,side<0?-w-.25f:w+1.45f,side<0?-w-1.45f:w+.25f,side<0?-w-.25f:w+1.45f,curb);
                    Band(a,b,side<0?-w-.25f:w+1.45f,hi,side<0?-w-.25f:w+1.45f,hi,shoulder);
                }
                if(mid>Entry&&mid<Exit)
                {
                    float ca=Offset(a),cb=Offset(b);
                    float la=Mathf.Max(16,ca-HalfWidth),lb=Mathf.Max(16,cb-HalfWidth);
                    float ha=Mathf.Max(16,ca+HalfWidth),hb=Mathf.Max(16,cb+HalfWidth);
                    float ea=Mathf.Min(.22f,Mathf.Max(0,ha-la)*.4f),eb=Mathf.Min(.22f,Mathf.Max(0,hb-lb)*.4f);
                    Band(a,b,la,la+ea,lb,lb+eb,white);
                    Band(a,b,la+ea,ha-ea,lb+eb,hb-eb,pit);
                    Band(a,b,ha-ea,ha,hb-eb,hb,white);
                }
            }
            var root=new GameObject("Marina pit lane and adjoining road surfaces").transform;root.SetParent(world,false);
            foreach(var batch in batches.Values)PolygonWorld.Surface("Non-overlapping pavement - "+batch.material.name,root,batch.vertices,batch.triangles,batch.material);
            batches.Clear();BuildBuildings(root,white,coral,teal,pit);
            // Open the roadside barrier at the entry and merge; the branch is visibly separate.
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(!child||(child.name!="Low roadside barrier"&&child.name!="Roadside delineator"))continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _);float d=lap*PolygonCircuit.Length;
                float angle=PolygonCircuit.AngleAtDistance(d);
                if(d>Entry-40&&d<Exit+40&&Vector3.Dot(child.position-PolygonWorld.Point(angle),PolygonWorld.Side(angle))>0)
                    Object.DestroyImmediate(child.gameObject);
            }
        }
        static Transform Frame(Transform root,string name,float d,float lateral)
        {
            float angle=PolygonCircuit.AngleAtDistance(d);var t=new GameObject(name).transform;t.SetParent(root,false);
            t.position=PolygonWorld.Point(angle)+PolygonWorld.Side(angle)*lateral;t.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(angle));return t;
        }
        static void BuildLaunch(Transform parent,Vector3 p,Material hull,Material white,Material window)
        {
            var v=new List<Vector3>();var t=new List<int>();
            System.Action<Vector3,Vector3,Vector3,Vector3> face=(a,b,c,d)=>
            {int n=v.Count;v.AddRange(new[]{a+p,b+p,c+p,d+p});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
            float[] z={-6,2,6},w={1.8f,2.1f,.15f};
            for(int i=0;i<2;i++)
            {
                var a=new Vector3(-w[i],-.2f,z[i]);var b=new Vector3(w[i],-.2f,z[i]);
                var c=new Vector3(w[i],.8f,z[i]);var d=new Vector3(-w[i],.8f,z[i]);
                var e=new Vector3(-w[i+1],-.2f,z[i+1]);var f=new Vector3(w[i+1],-.2f,z[i+1]);
                var g=new Vector3(w[i+1],.8f,z[i+1]);var h=new Vector3(-w[i+1],.8f,z[i+1]);
                face(d,h,g,c);face(a,b,f,e);face(a,e,h,d);face(b,c,g,f);
                if(i==0)face(a,d,c,b);if(i==1)face(e,f,g,h);
            }
            PolygonWorld.Surface("Moored faceted launch hull",parent,v,t,hull);
            PolygonWorld.Box("Launch cabin",parent,p+new Vector3(0,1.35f,-1),new Vector3(3,1.2f,4.6f),white);
            PolygonWorld.Box("Launch blue windshield",parent,p+new Vector3(0,1.4f,1.34f),new Vector3(2.7f,.7f,.1f),window);
            PolygonWorld.Box("Launch canopy",parent,p+new Vector3(0,2.05f,-1),new Vector3(3.4f,.18f,5),white);
        }
        static void BuildBuildings(Transform root,Material white,Material coral,Material teal,Material pit)
        {
            for(int i=0;i<6;i++)
            {
                float d=Entry+145+i*48;var garage=Frame(root,"Marina pastel pit garage",d,62);
                PolygonWorld.Box("Pier apron",garage,new Vector3(-3,-.22f,0),new Vector3(22,.4f,34),white);
                PolygonWorld.Box("Pastel garage",garage,new Vector3(0,3,0),new Vector3(13,6,29),i%2==0?white:teal);
                PolygonWorld.Box("Garage opening",garage,new Vector3(-6.56f,2.3f,0),new Vector3(.12f,4.4f,21),pit);
                PolygonWorld.Box("Coral garage header",garage,new Vector3(-6.7f,5,0),new Vector3(.2f,1,29),coral);
                for(int stripe=0;stripe<7;stripe++)PolygonWorld.Box("Striped marina awning",garage,new Vector3(-10,5.4f,-12+stripe*4),new Vector3(8,.28f,4),stripe%2==0?coral:white);
                foreach(int end in new[]{-1,1})PolygonWorld.Box("Awning upright",garage,new Vector3(-13.5f,2.7f,end*11),new Vector3(.15f,5.4f,.15f),white);
                var number=PolygonWorld.Label("Marina garage number",garage,"MARINA "+(i+1).ToString("00"),new Vector3(-6.95f,5,0),.065f,white.color);
                number.transform.localRotation=Quaternion.Euler(0,90,0);
            }
            var booth=Frame(root,"Marina timing booth",Entry+115,63);
            PolygonWorld.Box("Timing booth",booth,new Vector3(0,3,0),new Vector3(10,6,12),white);
            PolygonWorld.Box("Teal timing windows",booth,new Vector3(-5.08f,4,0),new Vector3(.15f,2,10),teal);
            foreach(float d in new[]{Entry-45,Entry+100,Exit-85})
            {
                bool exit=d>Exit-100;var sign=Frame(root,exit?"Pit merge sign":"Marina pit entry sign",d,exit?56:24);
                PolygonWorld.Box("Pit sign",sign,new Vector3(0,3,0),new Vector3(6,2.6f,.22f),teal);
                PolygonWorld.Box("Pit sign post",sign,new Vector3(0,1.3f,0),new Vector3(.2f,2.6f,.2f),coral);
                PolygonWorld.Label("Pit sign text",sign,exit?"MERGE\nLEFT":"MARINA PIT >\n80 KM/H",new Vector3(0,3,-.26f),.065f,white.color);
            }
            for(int i=0;i<9;i++)
            {
                var marker=Frame(root,"Marina pit edge marker",Entry+110+i*43,49);
                PolygonWorld.Box("Coral pit bollard",marker,new Vector3(0,.55f,0),new Vector3(.25f,1.1f,.25f),i%2==0?coral:white);
            }
            // Finger piers and moored boats extend into the sea behind the garages.
            for(int i=0;i<4;i++)
            {
                var dock=Frame(root,"Marina finger pier",Entry+175+i*85,89);
                PolygonWorld.Box("Ivory pier",dock,new Vector3(0,-.1f,0),new Vector3(39,.3f,3),white);
                BuildLaunch(dock,new Vector3(10,-.35f,7),i%2==0?coral:teal,white,pit);
                foreach(int end in new[]{-1,1})PolygonWorld.Box("Teal mooring post",dock,new Vector3(end*17,.55f,0),new Vector3(.35f,1.4f,.35f),teal);
            }
        }
    }
}
