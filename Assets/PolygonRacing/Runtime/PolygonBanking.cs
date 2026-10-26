using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // One mild coastal test section. The route and race distance remain in X/Z.
    public static class PolygonBanking
    {
        public const float MaximumDegrees=4;
        public static float Center=>PolygonCircuit.DistanceAtKnot(9.4f);
        public static float Start=>Center-150;
        public static float End=>Center+150;
        public static float Degrees(float distance)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)return 0;
            float d=Mathf.Repeat(distance,PolygonCircuit.Length);
            if(d<=Start||d>=End)return 0;
            float weight=Mathf.Min(Mathf.SmoothStep(0,1,(d-Start)/100),Mathf.SmoothStep(0,1,(End-d)/100));
            float outside=PolygonCircuit.Curvature(Center)>0?-1:1;
            return outside*MaximumDegrees*weight;
        }
        public static float Height(float distance,float lateral)
        {
            if(PolygonCircuit.HasElevation)return PolygonCircuit.Elevation(distance)+lateral*PolygonCircuit.BankSlope(distance);
            float slope=Mathf.Tan(Degrees(distance)*Mathf.Deg2Rad);
            if(Mathf.Abs(lateral)<=20)return Mathf.Max(0,20*Mathf.Abs(slope)+lateral*slope);
            float edge=20*Mathf.Abs(slope)+Mathf.Sign(lateral)*20*slope;
            return Mathf.Max(0,edge)*(1-Mathf.InverseLerp(20,32,Mathf.Abs(lateral)));
        }
        public static Vector3 SurfacePoint(float distance,float lateral,float clearance=0)
        {
            float angle=PolygonCircuit.AngleAtDistance(distance);
            return PolygonWorld.Point(angle)+PolygonWorld.Side(angle)*lateral+Vector3.up*(Height(distance,lateral)+clearance);
        }
        public static Vector3 Normal(float distance,float lateral)
        {
            if(Mathf.Abs(Degrees(distance))<.00001f&&!PolygonCircuit.HasElevation)return Vector3.up;
            var along=SurfacePoint(distance+.5f,lateral)-SurfacePoint(distance-.5f,lateral);
            var across=SurfacePoint(distance,lateral+.1f)-SurfacePoint(distance,lateral-.1f);
            var normal=Vector3.Cross(along,across).normalized;
            return normal.y<0?-normal:normal;
        }
        public static Vector3 ForwardOnSurface(Vector3 forward,Vector3 normal)
        {
            // Preserve steering heading in X/Z exactly; solve only the vertical component.
            forward.y=-(normal.x*forward.x+normal.z*forward.z)/Mathf.Max(.1f,normal.y);
            return forward.normalized;
        }
        public static void AlignPlayer(Transform car)
        {
            float lap=PolygonCircuit.ProjectLap(car.position,out _),d=lap*PolygonCircuit.Length;
            float angle=PolygonCircuit.AngleAtDistance(d);
            float lateral=Vector3.Dot(car.position-PolygonWorld.Point(angle),PolygonWorld.Side(angle));
            var p=car.position;p.y=Height(d,lateral);car.position=p;
            var normal=Normal(d,lateral);var forward=car.forward;forward.y=0;
            forward=ForwardOnSurface(forward,normal);
            if(forward.sqrMagnitude>.001f)car.rotation=Quaternion.LookRotation(forward,normal);
        }
        public static void BuildVergeAndAlignScenery(Transform world)
        {
            if(PolygonCircuit.Layout!=PolygonCircuitLayout.CoastalExpressway)return;
            var v=new List<Vector3>();var tr=new List<int>();
            var cuts=new SortedSet<float>{Start,Start+100,End-100,End};
            for(float d=4;d<PolygonCircuit.Length;d+=4)if(d>Start&&d<End)cuts.Add(d);
            for(float d=18;d<PolygonCircuit.Length;d+=38)
            {if(d>Start&&d<End)cuts.Add(d);if(d+10>Start&&d+10<End)cuts.Add(d+10);}
            var sections=new List<float>(cuts);
            for(int i=0;i<sections.Count-1;i++)
            {
                float d=sections[i],end=sections[i+1];
                foreach(int side in new[]{-1,1})foreach(int band in new[]{0,1})
                {
                    float outside=Degrees(Center)>0?1:-1;
                    if(band==1&&side!=outside)continue;
                    float a=side*(band==0?16:20),b=side*(band==0?20:32);int n=v.Count;
                    var p0=SurfacePoint(d,a,.045f);var p1=SurfacePoint(d,b,.045f);var p2=SurfacePoint(end,b,.045f);var p3=SurfacePoint(end,a,.045f);
                    v.AddRange(new[]{p0,p1,p2,p3});
                    if(Vector3.Cross(p1-p0,p2-p0).y>0)tr.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                    else tr.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                }
            }
            var mat=PolygonWorld.Paint("Coral Sweep banked verge",new Color(.53f,.62f,.40f));
            var meshObject=PolygonWorld.Surface("Coral Sweep banked verge",world,v,tr,mat);
            var renderer=meshObject.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(!child)continue;string name=child.name;
                bool rail=name=="Low roadside barrier";
                bool move=rail||name=="Roadside delineator"||name=="Sculpted palm cluster tree"||name.StartsWith("Ivory chevrons - ")
                    ||name=="Coral Sweep corner name"||name=="Coral Sweep festival pennant"||name.StartsWith("Expressway sign - ");
                if(!move)continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _),d=lap*PolygonCircuit.Length;
                if(d<=Start||d>=End)continue;
                float angle=PolygonCircuit.AngleAtDistance(d),lateral=Vector3.Dot(child.position-PolygonWorld.Point(angle),PolygonWorld.Side(angle));
                float height=Height(d,lateral);if(height<=0)continue;
                child.position+=Vector3.up*height;
                if(rail)
                {
                    var normal=Normal(d,lateral);var forward=ForwardOnSurface(child.forward,normal);
                    child.rotation=Quaternion.LookRotation(forward,normal);
                }
            }
        }
    }
}
