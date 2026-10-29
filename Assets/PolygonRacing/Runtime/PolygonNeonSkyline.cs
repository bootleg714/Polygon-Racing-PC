using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonRacing
{
    // Skyline & landmarks pass (October 29, 2026), shared by the four neon circuits.
    //   - A ring of distant megastructures beyond the circuit, tall enough to break the skyline,
    //     with lit bands, edge lights, a big animated screen and a beacon crown.
    //   - A few vertical sky beams (thin opaque emissive columns that bloom into light pillars).
    //   - Rings of flying traffic far above the track (PolygonNeonSkyLane): each lane is one baked
    //     mesh of small craft and freighters that slowly turns around the circuit centre.
    // Everything is opaque, generated at editor time, adds no lights, and stays well away from the road.
    public sealed class PolygonNeonSkylineStyle
    {
        public string Prefix="Neon";
        public Transform Parent;
        public Func<Vector3,PolygonSceneryKit> KitAt;
        public Material Body,Steel,Glass,Panel,Window,SkyBeam,TailLight,HeadLight;
        public Material[] Accents;
        public int Megastructures=10;
        public float MegaMinHeight=380,MegaMaxHeight=780,MegaRingMin=380,MegaRingMax=780,MegaWidthMin=38,MegaWidthMax=64;
        public int SkyBeams=3;
        public int Lanes=4,CraftPerLane=14;
        public float LaneLift=190,LaneSpacing=38;
        public bool Freighters=true;
    }

    public static class PolygonNeonSkyline
    {
        public struct Bounds2{public Vector3 Center;public float Radius,MinY,MaxY;}
        static float Hash(float x,float z,int seed)=>Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f+seed*37.719f)*43758.5453f,1);

        public static Bounds2 Measure()
        {
            var b=new Bounds2{MinY=float.MaxValue,MaxY=float.MinValue};
            float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
            for(int i=0;i<256;i++)
            {
                float d=PolygonCircuit.Length*i/256;var p=PolygonCircuit.AtDistance(d);float y=PolygonCircuit.Elevation(d);
                minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);
                b.MinY=Mathf.Min(b.MinY,y);b.MaxY=Mathf.Max(b.MaxY,y);
            }
            b.Center=new Vector3((minX+maxX)*.5f,0,(minZ+maxZ)*.5f);
            b.Radius=Mathf.Max(maxX-minX,maxZ-minZ)*.5f;
            return b;
        }

        public static Bounds2 Build(PolygonNeonSkylineStyle s)
        {
            var b=Measure();
            Material Accent(int i)=>s.Accents[((i%s.Accents.Length)+s.Accents.Length)%s.Accents.Length];
            int megas=0,beams=0,craft=0;

            // ------------------------------------------------------------ megastructures
            for(int i=0;i<s.Megastructures;i++)
            {
                float ang=(i+Hash(i,3,1)*.6f)*Mathf.PI*2/s.Megastructures;
                float dist=b.Radius+Mathf.Lerp(s.MegaRingMin,s.MegaRingMax,Hash(i,5,2));
                var p=b.Center+new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang))*dist;p.y=b.MinY-45;
                PolygonCircuit.ProjectLapFlat(p,out float offset);if(offset<260)continue;
                float h=Mathf.Lerp(s.MegaMinHeight,s.MegaMaxHeight,Hash(i,7,3));
                float w=Mathf.Lerp(s.MegaWidthMin,s.MegaWidthMax,Hash(i,9,4))*.5f;
                var toCentre=b.Center-p;toCentre.y=0;toCentre.Normalize();
                Megastructure(s.KitAt(p),p,h,w,toCentre,s,Accent(i),i);megas++;
            }

            // ------------------------------------------------------------ sky beams
            for(int i=0,tries=0;i<s.SkyBeams&&tries<60;tries++)
            {
                float ang=Hash(tries,11,5)*Mathf.PI*2,r=b.Radius*Mathf.Lerp(.35f,1.05f,Hash(tries,13,6));
                var p=b.Center+new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang))*r;
                PolygonCircuit.ProjectLapFlat(p,out float offset);if(offset<70)continue;
                p.y=b.MinY-30;
                SkyBeam(s.KitAt(p),p,1900,s.SkyBeam);i++;beams++;
            }

            // ------------------------------------------------------------ flying traffic
            for(int lane=0;lane<s.Lanes;lane++)
            {
                float t=s.Lanes>1?lane/(float)(s.Lanes-1):0;
                float r=b.Radius*Mathf.Lerp(.42f,1.15f,t);
                float y=b.MaxY+s.LaneLift+lane*s.LaneSpacing;
                int dir=(lane&1)==0?1:-1;
                var go=new GameObject($"{s.Prefix} sky traffic lane {lane+1}").transform;
                go.SetParent(s.Parent,false);go.localPosition=new Vector3(b.Center.x,y,b.Center.z);
                var kit=new PolygonSceneryKit($"{s.Prefix} sky lane {lane+1}");
                for(int k=0;k<s.CraftPerLane;k++)
                {
                    float a=(k+Hash(k,lane,7)*.5f)*Mathf.PI*2/s.CraftPerLane;
                    float rr=r+(Hash(k,lane,8)-.5f)*40;
                    var pos=new Vector3(Mathf.Cos(a)*rr,(Hash(k,lane,9)-.5f)*14,Mathf.Sin(a)*rr);
                    var fwd=new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a))*dir;
                    bool freighter=s.Freighters&&k%5==2;
                    Craft(kit,pos,fwd,freighter,s,Accent(lane+k));craft++;
                }
                kit.Emit(go);
                float speed=48+lane*9;                          // metres per second along the ring
                go.gameObject.AddComponent<PolygonNeonSkyLane>().DegreesPerSecond=dir*speed/r*Mathf.Rad2Deg;   // +Y rotation moves craft along fwd
            }
            Debug.Log($"{s.Prefix} skyline v01: {megas} megastructures, {beams} sky beams, {s.Lanes} traffic lanes ({craft} craft).");
            return b;
        }

        public static void Megastructure(PolygonSceneryKit kit,Vector3 p,float h,float w,Vector3 toCentre,PolygonNeonSkylineStyle s,Material accent,int seed)
        {
            float top=w*.68f;
            kit.Chamfered(s.Body,p,w,w,top,top,0,h,w*.18f);
            // Setback crown and mast.
            kit.Chamfered(s.Body,p+Vector3.up*h,top*.72f,top*.72f,top*.45f,top*.45f,0,h*.14f,top*.12f);
            kit.Column(s.Steel,p+Vector3.up*(h*1.14f),2.2f,.6f,70,6);
            kit.Box(accent,p+Vector3.up*(h*1.14f+72),new Vector3(5,5,5));
            // Lit floor bands that wrap the tower.
            for(float y=55,n=0;y<h-10;y+=42,n++)
            {
                float half=Mathf.Lerp(w,top,y/h)+.5f;
                kit.Box(((int)n+seed)%3==0?accent:s.Window,p+Vector3.up*y,new Vector3(half*2,1.4f,half*2));
            }
            // Two vertical edge lights on the faces that look toward the circuit.
            var side=Vector3.Cross(Vector3.up,toCentre);
            foreach(var dir in new[]{toCentre,side})
                kit.Beam(accent,p+dir*(w+.6f)+Vector3.up*30,p+dir*(top+.6f)+Vector3.up*(h-4),1.6f,1.6f);
            // A large animated screen facing the circuit.
            float sy=h*.42f,half2=Mathf.Lerp(w,top,sy/h);
            var rot=Quaternion.LookRotation(toCentre);
            kit.Box(s.Body,p+toCentre*(half2+.5f)+Vector3.up*sy,new Vector3(half2*1.5f,h*.28f,1.0f),rot);
            kit.Box(s.Panel,p+toCentre*(half2+1.1f)+Vector3.up*sy,new Vector3(half2*1.35f,h*.25f,.2f),rot);
        }

        public static void SkyBeam(PolygonSceneryKit kit,Vector3 p,float height,Material m)
        {
            kit.Column(m,p,3.2f,1.1f,height,6);
        }

        static void Craft(PolygonSceneryKit kit,Vector3 p,Vector3 fwd,bool freighter,PolygonNeonSkylineStyle s,Material accent)
        {
            // Craft are drawn ~2x real size: they fly 200-300 m up and must read as traffic, not specks.
            const float k=2.2f;
            var rot=Quaternion.LookRotation(fwd);
            Vector3 L(float x,float y,float z)=>p+rot*(new Vector3(x,y,z)*k);
            // A long light trail behind every craft turns each lane into a visible moving stream.
            kit.Box(accent,L(0,0,freighter?-28:-16),new Vector3(.5f,.35f,freighter?30:22)*k,rot);
            if(freighter)
            {
                kit.Box(s.Body,p,new Vector3(7,3.4f,26)*k,rot);
                kit.Box(s.Steel,L(0,2.1f,-2),new Vector3(5,.8f,16)*k,rot);
                kit.Box(accent,L(0,-1.85f,0),new Vector3(5.6f,.3f,22)*k,rot);
                foreach(int x in new[]{-1,1})
                {
                    kit.Box(s.TailLight,L(x*2.6f,0,-13.1f),new Vector3(1.4f,1.4f,.4f)*k,rot);
                    kit.Box(s.HeadLight,L(x*2.4f,.4f,13.1f),new Vector3(1.2f,.5f,.3f)*k,rot);
                }
                return;
            }
            kit.Box(s.Body,p,new Vector3(3.4f,1.2f,8)*k,rot);
            kit.Box(s.Glass,L(0,.85f,1.2f),new Vector3(2,.6f,2.8f)*k,rot);
            kit.Box(accent,L(0,-.7f,0),new Vector3(2.6f,.2f,6)*k,rot);
            kit.Box(s.TailLight,L(0,.1f,-4.15f),new Vector3(2.8f,.6f,.35f)*k,rot);
            kit.Box(s.HeadLight,L(0,0,4.15f),new Vector3(2.2f,.35f,.35f)*k,rot);
        }

        // Starfall Port: a space-elevator tether rising out of sight, ringed with lights.
        public static void Tether(PolygonSceneryKit kit,Vector3 p,float height,PolygonNeonSkylineStyle s,Material accent)
        {
            kit.Chamfered(s.Body,p,40,40,24,24,0,120,8);
            kit.Column(s.Steel,p+Vector3.up*118,7,4.5f,height,8);
            for(float y=180;y<height;y+=70)kit.Column(accent,p+Vector3.up*y,6.2f,6.2f,2.2f,8);
            kit.Box(accent,p+Vector3.up*121,new Vector3(52,2,52));
        }

        // Starfall Port: a huge carrier hovering over the far harbour.
        public static void Mothership(PolygonSceneryKit kit,Vector3 p,Vector3 forward,PolygonNeonSkylineStyle s,Material accent)
        {
            var rot=Quaternion.LookRotation(forward);
            Vector3 L(float x,float y,float z)=>p+rot*new Vector3(x,y,z);
            kit.Prism(s.Body,new[]{L(-80,-18,-240),L(80,-18,-240),L(110,-18,60),L(0,-18,260),L(-110,-18,60)},
                             new[]{L(-60,22,-220),L(60,22,-220),L(85,22,50),L(0,22,210),L(-85,22,50)});
            kit.Box(s.Body,L(0,40,-90),new Vector3(90,40,150),rot);
            kit.Box(s.Window,L(0,40,-14.6f),new Vector3(70,6,.6f),rot);
            foreach(int x in new[]{-1,1})
            {
                kit.Beam(accent,L(x*108,-19,60),L(x*78,-19,-238),2.5f,2.5f);
                for(int e=0;e<3;e++)kit.Box(s.TailLight,L(x*(20+e*24),0,-241),new Vector3(16,16,2),rot);
            }
            for(int k=0;k<14;k++)kit.Box(s.HeadLight,L((k%2==0?-1:1)*(30+k*4),-19.5f,-200+k*28),new Vector3(4,1,4),rot);
        }
    }
}
