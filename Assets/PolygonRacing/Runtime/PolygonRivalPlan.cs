using UnityEngine;

namespace PolygonRacing
{
    // Shared opponent route plan for the active circuit: a racing line (lateral offset from the
    // centre line) and corner-speed envelopes. Built once per layout and reused by all rivals.
    public sealed class PolygonRivalPlan
    {
        // Mirror PolygonDriver: lock = steeringAngle/(1+speed*LockFade), yaw = speed/Wheelbase*tan(lock).
        // Using the player's own steering limit keeps rivals on the same cornering rules as the player.
        const float Wheelbase=2.85f,LockFade=.065f;
        const float StationSpacing=2f;
        const float EdgeMargin=2.1f;
        const int LineIterations=700;
        public const float LowLockUse=.85f,HighLockUse=1f;

        public float Length { get; private set; }
        public float Spacing { get; private set; }
        public float LaneLimit { get; private set; }
        public int Count { get; private set; }
        float[] line,lineCurvature,centreCurvature,envelopeLow,envelopeHigh;

        static PolygonRivalPlan cached;
        static string cachedKey;

        public static PolygonRivalPlan For(float steeringAngle,float brakingDeceleration,float cornerGrip)
        {
            string key=PolygonCircuit.LayoutId+"|"+PolygonCircuit.Length.ToString("F2")+"|"+PolygonCircuit.RoadHalfWidth+"|"+steeringAngle+"|"+brakingDeceleration+"|"+cornerGrip;
            if(cached==null||cachedKey!=key){cached=new PolygonRivalPlan(steeringAngle,brakingDeceleration,cornerGrip);cachedKey=key;}
            return cached;
        }

        PolygonRivalPlan(float steeringAngle,float brakingDeceleration,float cornerGrip)
        {
            Length=PolygonCircuit.Length;
            int n=Mathf.Max(16,Mathf.CeilToInt(Length/StationSpacing));
            Count=n;Spacing=Length/n;
            LaneLimit=Mathf.Max(1,PolygonCircuit.RoadHalfWidth-EdgeMargin);
            var centre=new Vector2[n];var side=new Vector2[n];
            centreCurvature=new float[n];
            for(int i=0;i<n;i++)
            {
                float s=i*Spacing;
                var c=PolygonCircuit.AtDistance(s);centre[i]=new Vector2(c.x,c.z);
                var t=PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(s));
                side[i]=new Vector2(t.z,-t.x).normalized; // Cross(up,tangent), same as PolygonWorld.Side
                centreCurvature[i]=PolygonCircuit.Curvature(s);
            }
            // Racing line: an elastic band pulled taut inside the road edges (shortest smooth path).
            // Each pass moves every point toward the midpoint of its neighbours, measured sideways.
            line=new float[n];var next=new float[n];var p=new Vector2[n];
            for(int it=0;it<LineIterations;it++)
            {
                for(int i=0;i<n;i++)p[i]=centre[i]+side[i]*line[i];
                for(int i=0;i<n;i++)
                {
                    var mid=(p[(i+n-1)%n]+p[(i+1)%n])*.5f;
                    next[i]=Mathf.Clamp(line[i]+.6f*Vector2.Dot(mid-p[i],side[i]),-LaneLimit,LaneLimit);
                }
                var swap=line;line=next;next=swap;
            }
            for(int pass=0;pass<4;pass++)
            {
                for(int i=0;i<n;i++)next[i]=Mathf.Clamp((line[(i+n-1)%n]+2*line[i]+line[(i+1)%n])*.25f,-LaneLimit,LaneLimit);
                var swap=line;line=next;next=swap;
            }
            for(int i=0;i<n;i++)p[i]=centre[i]+side[i]*line[i];
            lineCurvature=new float[n];
            for(int i=0;i<n;i++)
            {
                var a=p[i]-p[(i+n-2)%n];var b=p[(i+2)%n]-p[i];
                float cross=a.y*b.x-a.x*b.y; // matches Vector3.SignedAngle(a,b,up): positive turns right
                float angle=Mathf.Atan2(cross,Vector2.Dot(a,b));
                lineCurvature[i]=angle/Mathf.Max(.001f,(a.magnitude+b.magnitude)*.5f);
            }
            envelopeLow=Envelope(LowLockUse,steeringAngle,brakingDeceleration,cornerGrip);
            envelopeHigh=Envelope(HighLockUse,steeringAngle,brakingDeceleration,cornerGrip);
        }

        // Highest speed at each station that the player's steering can hold on the racing line,
        // with braking zones worked backwards from each corner.
        float[] Envelope(float lockUse,float steeringAngle,float deceleration,float cornerGrip)
        {
            int n=Count;var v=new float[n];
            for(int i=0;i<n;i++)v[i]=CornerSpeed(lineCurvature[i],lockUse,steeringAngle,cornerGrip);
            for(int pass=0;pass<2;pass++)
                for(int i=n-1;i>=0;i--)
                    v[i]=Mathf.Min(v[i],Mathf.Sqrt(v[(i+1)%n]*v[(i+1)%n]+2*deceleration*Spacing));
            return v;
        }

        public static float MaxCurvature(float speed,float steeringAngle)
            =>Mathf.Tan(steeringAngle/(1+Mathf.Max(0,speed)*LockFade)*Mathf.Deg2Rad)/Wheelbase;

        static float CornerSpeed(float curvature,float lockUse,float steeringAngle,float cornerGrip)
        {
            const float cap=90;curvature=Mathf.Abs(curvature);
            if(AvailableCurvature(cap,steeringAngle,lockUse,cornerGrip)>=curvature)return cap;
            float lo=0,hi=cap;
            for(int i=0;i<24;i++){float m=(lo+hi)*.5f;if(AvailableCurvature(m,steeringAngle,lockUse,cornerGrip)>=curvature)lo=m;else hi=m;}
            return lo;
        }

        static float AvailableCurvature(float speed,float steeringAngle,float lockUse,float cornerGrip)
            =>Mathf.Min(MaxCurvature(speed,steeringAngle),Mathf.Max(1f,cornerGrip)/Mathf.Max(.01f,speed*speed))*lockUse;

        float Sample(float[] values,float meters)
        {
            float x=Mathf.Repeat(meters,Length)/Spacing;int i=Mathf.FloorToInt(x);float f=x-i;
            return Mathf.Lerp(values[i%Count],values[(i+1)%Count],f);
        }
        public float Line(float meters)=>Sample(line,meters);
        public float LineCurvature(float meters)=>Sample(lineCurvature,meters);
        public float CentreCurvature(float meters)=>Sample(centreCurvature,meters);
        // skill 0 = cautious (85% of steering lock), 1 = at the player's full steering limit
        public float CornerLimit(float meters,float skill)=>Mathf.Lerp(Sample(envelopeLow,meters),Sample(envelopeHigh,meters),Mathf.Clamp01(skill));
    }
}
