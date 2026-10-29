using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Second-pass trackside dressing shared by the four neon circuits (October 29, 2026).
    // All four use the same cross-section: road +-10 m, deck strip 10-12 m, a 1.1 m barrier from
    // 12 m and a deck edge at 13.2 m. Everything here is opaque, editor-baked into the caller's
    // chunks, and stays outside the 20 m road; overhead pieces clear the road by at least 9.5 m.
    //   - Animated LED boards on the road-facing side of the barrier along the straights.
    //   - Glowing two-tone kerbs on the inside and outside of every real corner.
    //   - Light arches ("gates") over the longest straights.
    //   - Cantilevered holo billboards above the verge, angled toward oncoming cars.
    //   - Brake-marker pylons (3/2/1 bands) 150/100/50 m before the tight corners.
    public sealed class PolygonNeonTracksideStyle
    {
        public string Prefix="Neon";
        public Func<Vector3,PolygonSceneryKit> KitAt;
        public Material Dark,Steel,KerbA,KerbB,BoardA,BoardB;
        public Material[] Accents;
        // True where nothing should be added (tunnels, start complex, landmark crossings...).
        public Func<float,bool> SkipAt;
        // True where nothing may cross above the road (walkways, overhead signs, bridges...).
        public Func<float,bool> OverheadSkip;
        // True where a post at (distance, side) would hit an existing trackside object.
        public Func<float,int,bool> PostBlocked;
        // True where a world-space point (with a radius) is inside an existing building/structure.
        public Func<Vector3,float,bool> Blocked;
        public int MaxArchStraights=3,ArchesPerStraight=8;
        public float ArchSpacing=24,BillboardSpacing=140;
    }

    public static class PolygonNeonTrackside
    {
        const float Barrier=12f,PostLateral=12.9f;
        public struct Stats{public int BoardRuns,KerbCorners,Arches,Billboards,Markers;}

        static Vector3 P(float d,float lateral,float up=0)=>PolygonBanking.SurfacePoint(Wrap(d),lateral,up);
        static Vector3 T(float d)=>PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(Wrap(d)));
        static Vector3 S(float d)=>Vector3.Cross(Vector3.up,T(d));
        static float Wrap(float d)=>Mathf.Repeat(d,PolygonCircuit.Length);
        static float Curve(float d)=>PolygonCircuit.Curvature(Wrap(d));
        static float MaxCurve(float d,float span)
        {
            float m=0;for(float x=-span;x<=span;x+=4)m=Mathf.Max(m,Mathf.Abs(Curve(d+x)));return m;
        }

        public static Material Panel(string name,Color emission,float speed,float scale)
        {
            var shader=Resources.Load<Shader>("PolygonNeonPanel");
            if(!shader)throw new InvalidOperationException("PolygonNeonPanel.shader must finish importing before building neon scenery.");
            var m=new Material(shader){name=name,enableInstancing=true};
            m.SetColor("_BaseColor",new Color(.01f,.02f,.04f));m.SetColor("_EmissionColor",emission);
            m.SetFloat("_ScrollSpeed",speed);m.SetFloat("_PatternScale",scale);
            return m;
        }

        public static Stats Build(PolygonNeonTracksideStyle style)
        {
            var stats=new Stats();
            float length=PolygonCircuit.Length;
            bool Skip(float d)=>style.SkipAt!=null&&style.SkipAt(Wrap(d));
            bool PostBad(float d,int side)=>Skip(d)||(style.PostBlocked!=null&&style.PostBlocked(Wrap(d),side));
            bool Hit(Vector3 p,float r)=>style.Blocked!=null&&style.Blocked(p,r);
            Material Accent(int i)=>style.Accents[((i%style.Accents.Length)+style.Accents.Length)%style.Accents.Length];

            // ---------------------------------------------------------------- straights
            var straights=new List<Vector2>();
            {
                float start=-1;
                for(float d=0;d<=length;d+=4)
                {
                    bool straight=d<length&&Mathf.Abs(Curve(d))<1/320f&&!Skip(d);
                    if(straight&&start<0)start=d;
                    if(!straight&&start>=0){if(d-start>=100)straights.Add(new Vector2(start,d));start=-1;}
                }
                straights.Sort((a,b)=>(b.y-b.x).CompareTo(a.y-a.x));
            }

            // ---------------------------------------------------------------- LED barrier boards
            {
                int lastRun=int.MinValue;
                for(float d=0;d<length;d+=4)
                {
                    if(Skip(d)||Mathf.Abs(Curve(d))>1/300f)continue;
                    int run=Mathf.FloorToInt(d/36);
                    var kit=style.KitAt(P(d,0));
                    foreach(int side in new[]{-1,1})
                    {
                        if(((run+(side>0?1:0))%3)==2)continue;           // gaps, offset between sides
                        var m=((run+(side>0?1:0))&1)==0?style.BoardA:style.BoardB;
                        Slab(kit,m,d,Mathf.Min(d+4,length),side*(Barrier-.07f),side*Barrier,.22f,.9f);
                    }
                    if(run!=lastRun){stats.BoardRuns++;lastRun=run;}
                }
            }

            // ---------------------------------------------------------------- corner kerbs
            {
                bool inCorner=false;
                for(float d=0;d<length;d+=2)
                {
                    float c=Curve(d+1);
                    bool corner=Mathf.Abs(c)>1/150f;
                    if(corner&&!inCorner)stats.KerbCorners++;
                    inCorner=corner;
                    if(!corner||Skip(d))continue;
                    var kit=style.KitAt(P(d,0));int block=Mathf.FloorToInt(d/2);
                    foreach(int side in new[]{-1,1})
                        Slab(kit,(block&1)==0?style.KerbA:style.KerbB,d,Mathf.Min(d+2,length),side*9.85f,side*11.3f,0,.085f);
                }
            }

            // ---------------------------------------------------------------- light arches
            {
                int used=0;
                foreach(var s in straights)
                {
                    if(used>=style.MaxArchStraights)break;
                    if(s.y-s.x<180)break;
                    int built=0;
                    for(float d=s.x+30;d<s.y-40&&built<style.ArchesPerStraight;d+=style.ArchSpacing)
                    {
                        if(PostBad(d,-1)||PostBad(d,1)||(style.OverheadSkip!=null&&style.OverheadSkip(Wrap(d))))continue;
                        var l=P(d,-PostLateral);var r=P(d,PostLateral);
                        float top=Mathf.Max(P(d,-10).y,P(d,10).y)+10.2f;
                        if(Hit(new Vector3(l.x,top,l.z),1)||Hit(new Vector3(r.x,top,r.z),1))continue;
                        var kit=style.KitAt(P(d,0));var f=T(d);var accent=Accent(built+used);
                        var lt=new Vector3(l.x,top,l.z);var rt=new Vector3(r.x,top,r.z);
                        kit.Beam(style.Steel,l+Vector3.down*.2f,lt+Vector3.up*.4f,.55f,.55f);
                        kit.Beam(style.Steel,r+Vector3.down*.2f,rt+Vector3.up*.4f,.55f,.55f);
                        kit.Beam(style.Steel,lt,rt,.6f,.8f);
                        kit.Beam(accent,lt+Vector3.down*.47f,rt+Vector3.down*.47f,.16f,.12f);
                        var inward=(r-l);inward.y=0;inward.Normalize();
                        kit.Beam(accent,l+inward*.31f+Vector3.up*1.3f,lt+inward*.31f+Vector3.down*.5f,.12f,.16f);
                        kit.Beam(accent,r-inward*.31f+Vector3.up*1.3f,rt-inward*.31f+Vector3.down*.5f,.12f,.16f);
                        var rot=Quaternion.LookRotation(f);var mid=(lt+rt)*.5f;
                        kit.Box(style.Dark,mid-f*.33f,new Vector3(9,.7f,.06f),rot);
                        kit.Box(built%2==0?style.BoardA:style.BoardB,mid-f*.38f,new Vector3(8.4f,.5f,.05f),rot);
                        built++;stats.Arches++;
                    }
                    if(built>0)used++;
                }
            }

            // ---------------------------------------------------------------- billboards
            {
                int n=0;
                foreach(var s in straights)
                    for(float d=s.x+45;d<s.y-15;d+=style.BillboardSpacing)
                    {
                        int side=(n&1)==0?1:-1;
                        if(PostBad(d,side)){side=-side;if(PostBad(d,side))continue;}
                        var f=T(d);var outward=S(d)*side;
                        var face=(-f-outward*.45f).normalized;face.y=0;face.Normalize();   // toward oncoming cars, angled to the road
                        var rot=Quaternion.LookRotation(face);var right=rot*Vector3.right;
                        float spanSign=Vector3.Dot(right,outward)>0?1:-1;               // board extends away from the road
                        var foot=P(d,side*PostLateral);
                        var centre=foot+Vector3.up*7.6f+right*spanSign*2.8f+face*.32f;
                        if(Hit(centre,4)||Hit(centre+right*spanSign*3,1.5f))continue;
                        var kit=style.KitAt(foot);
                        kit.Beam(style.Steel,foot+Vector3.down*.2f,foot+Vector3.up*9.6f,.36f,.36f);
                        kit.Beam(style.Steel,foot+Vector3.up*6.2f,centre-face*.2f+right*spanSign*1.6f+Vector3.down*1.4f,.18f,.18f);
                        kit.Box(style.Dark,centre,new Vector3(6.1f,3.5f,.3f),rot);
                        kit.Box((n&1)==0?style.BoardA:style.BoardB,centre+face*.18f,new Vector3(5.6f,3.0f,.06f),rot);
                        kit.Box(Accent(n),centre+face*.17f+Vector3.up*1.86f,new Vector3(6.1f,.14f,.08f),rot);
                        kit.Box(Accent(n+1),centre+face*.17f+Vector3.down*1.86f,new Vector3(6.1f,.14f,.08f),rot);
                        n++;stats.Billboards++;
                    }
            }

            // ---------------------------------------------------------------- brake markers
            {
                float lastCorner=-999;
                for(float d=150;d<length+150;d+=2)
                {
                    float c=Curve(d);
                    if(Mathf.Abs(c)<1/95f||d-lastCorner<260)continue;
                    // A real braking zone: the 150 m before the corner is (almost) straight.
                    if(MaxCurve(d-95,52)>1/170f)continue;
                    lastCorner=d;
                    int outside=c>0?-1:1;
                    for(int k=1;k<=3;k++)
                    {
                        float md=d-k*50;if(PostBad(md,outside))continue;
                        var foot=P(md,outside*PostLateral);var kit=style.KitAt(foot);
                        float h=2.6f+k*.5f;
                        kit.Beam(style.Dark,foot+Vector3.down*.2f,foot+Vector3.up*h,.5f,.5f);
                        for(int b=0;b<k;b++)kit.Box(style.KerbA,foot+Vector3.up*(h-.45f-b*.55f),new Vector3(.56f,.22f,.56f),Quaternion.LookRotation(T(md)));
                        kit.Box(style.Steel,foot+Vector3.up*(h+.08f),new Vector3(.62f,.16f,.62f),Quaternion.LookRotation(T(md)));
                        stats.Markers++;
                    }
                }
            }
            Debug.Log($"{style.Prefix} trackside v02: {stats.BoardRuns} LED board runs, {stats.KerbCorners} kerbed corners, {stats.Arches} light arches, {stats.Billboards} billboards, {stats.Markers} brake markers.");
            return stats;
        }

        // A solid strip following the road between two lateral offsets (signed: -left, +right).
        static void Slab(PolygonSceneryKit kit,Material m,float d0,float d1,float a,float b,float y0,float y1)
        {
            float l0=Mathf.Min(a,b),l1=Mathf.Max(a,b);
            kit.Prism(m,new[]{P(d0,l0,y0),P(d0,l1,y0),P(d1,l1,y0),P(d1,l0,y0)},
                        new[]{P(d0,l0,y1),P(d0,l1,y1),P(d1,l1,y1),P(d1,l0,y1)});
        }
    }
}
