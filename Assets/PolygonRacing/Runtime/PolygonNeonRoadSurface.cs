using System;
using UnityEngine;

namespace PolygonRacing
{
    // Road-surface pass (October 29, 2026), shared by the four neon circuits (road +-10 m).
    //   - Expansion-joint seams across the asphalt every 16 m.
    //   - Glowing chevron rows painted in each braking zone before the tight corners.
    //   - A faint dashed racing line that drifts to the inside of each corner.
    //   - Glowing cat's-eye studs just inside both edge lines.
    // All of it is flat opaque geometry a few millimetres above the road, baked into the stage
    // chunks. Material names start with "Road paint" so the wet-road probe bake leaves them out.
    // Heights: road 0 | seams .012 | lane paint .022-.035 (existing) | racing line .03 | chevrons .042.
    public sealed class PolygonNeonRoadSurfaceStyle
    {
        public string Prefix="Neon";
        public Func<Vector3,PolygonSceneryKit> KitAt;
        public Material Seam,Chevron,RacingLine,StudLeft,StudRight;
        public Func<float,bool> SkipAt;
        public float SeamSpacing=16,StudSpacing=12;
    }

    public static class PolygonNeonRoadSurface
    {
        static float L=>PolygonCircuit.Length;
        static float Wrap(float d)=>Mathf.Repeat(d,PolygonCircuit.Length);
        static Vector3 P(float d,float lateral,float up)=>PolygonBanking.SurfacePoint(Wrap(d),lateral,up);
        static float Curve(float d)=>PolygonCircuit.Curvature(Wrap(d));
        static float Smoothed(float d,float half)
        {
            float sum=0;int n=0;for(float x=-half;x<=half;x+=8){sum+=Curve(d+x);n++;}return sum/n;
        }
        static float MaxCurve(float d,float span)
        {
            float m=0;for(float x=-span;x<=span;x+=4)m=Mathf.Max(m,Mathf.Abs(Curve(d+x)));return m;
        }
        // Flat quad on the road surface from four (distance, lateral) corners.
        static void Patch(PolygonSceneryKit kit,Material m,float y,float d0,float l0,float d1,float l1,float d2,float l2,float d3,float l3)
        {
            var a=P(d0,l0,y);var b=P(d1,l1,y);var c=P(d2,l2,y);var e=P(d3,l3,y);
            kit.Quad(m,a,b,c,e,(a+b+c+e)*.25f+Vector3.down);
        }

        public static void Build(PolygonNeonRoadSurfaceStyle s)
        {
            bool Skip(float d)=>s.SkipAt!=null&&s.SkipAt(Wrap(d));
            int seams=0,chevrons=0,dashes=0,studs=0;

            // Expansion joints across the full road width.
            for(float d=s.SeamSpacing*.5f;d<L;d+=s.SeamSpacing)
            {
                if(Skip(d))continue;
                Patch(s.KitAt(P(d,0,0)),s.Seam,.012f,d-.07f,-9.55f,d-.07f,9.55f,d+.07f,9.55f,d+.07f,-9.55f);seams++;
            }

            // Cat's-eye studs just inside the edge lines (left amber / right white by default).
            for(float d=4;d<L;d+=s.StudSpacing)
            {
                if(Skip(d))continue;
                var kit=s.KitAt(P(d,0,0));var rot=Quaternion.LookRotation(PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(Wrap(d))));
                kit.Box(s.StudLeft,P(d,-9.3f,.03f),new Vector3(.28f,.06f,.2f),rot);
                kit.Box(s.StudRight,P(d,9.3f,.03f),new Vector3(.28f,.06f,.2f),rot);
                studs+=2;
            }

            // Braking-zone chevrons: rows of ^ shapes in the 120 m before each tight corner.
            var zone=new bool[Mathf.CeilToInt(L/2)+1];
            float lastCorner=-999;
            for(float d=150;d<L+150;d+=2)
            {
                float c=Curve(d);
                if(Mathf.Abs(c)<1/95f||d-lastCorner<260)continue;
                if(MaxCurve(d-95,52)>1/170f)continue;
                lastCorner=d;
                for(int row=0;row<6;row++)
                {
                    float rd=d-25-row*18;if(Skip(rd))continue;
                    var kit=s.KitAt(P(rd,0,0));
                    foreach(float lane in new[]{-5f,5f})
                    {
                        // Left and right arms meeting at the front (pointing in the direction of travel).
                        Patch(kit,s.Chevron,.042f,rd-1.6f,lane-3.1f,rd-1.6f,lane-2.5f,rd+1.6f,lane+.3f,rd+1.6f,lane-.3f);
                        Patch(kit,s.Chevron,.042f,rd-1.6f,lane+2.5f,rd-1.6f,lane+3.1f,rd+1.6f,lane+.3f,rd+1.6f,lane-.3f);
                    }
                    chevrons++;
                    for(float z=rd-3;z<=rd+3;z+=2){int i=Mathf.FloorToInt(Wrap(z)/2);if(i>=0&&i<zone.Length)zone[i]=true;}
                }
            }

            // Racing line: short faint dashes that lean toward the inside of each corner.
            for(float d=20;d<L;d+=7)
            {
                if(Skip(d)||zone[Mathf.Clamp(Mathf.FloorToInt(Wrap(d)/2),0,zone.Length-1)])continue;
                float lat=Mathf.Clamp(Smoothed(d,48)*420,-6.5f,6.5f);
                float lat2=Mathf.Clamp(Smoothed(d+2.4f,48)*420,-6.5f,6.5f);
                // Keep clear of the dashed lane lines at +-3.33 m.
                if(Mathf.Abs(Mathf.Abs(lat)-3.33f)<.35f)continue;
                Patch(s.KitAt(P(d,0,0)),s.RacingLine,.03f,d,lat-.14f,d,lat+.14f,d+2.4f,lat2+.14f,d+2.4f,lat2-.14f);dashes++;
            }
            Debug.Log($"{s.Prefix} road surface v01: {seams} seams, {studs} cat's-eye studs, {chevrons} chevron rows, {dashes} racing-line dashes.");
        }
    }
}
