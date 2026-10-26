using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing
{
    // Scenery pass v02: a small multi-material mesh kit for buildings, stands and crowds.
    // Everything added is baked into one mesh per material when Emit is called, so a whole
    // landmark costs a handful of draw calls. Closed solids are wound outward from their own
    // centre; flags and awnings use double-sided sheets. Detail pieces always stand proud of
    // the surface they sit on (never coplanar), which keeps the headset free of flicker.
    public sealed class PolygonSceneryKit
    {
        sealed class Part
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public readonly List<int> Indices=new List<int>();
        }
        readonly Dictionary<Material,Part> parts=new Dictionary<Material,Part>();
        readonly List<Material> order=new List<Material>();
        readonly string label;
        public PolygonSceneryKit(string label){this.label=label;}

        Part For(Material m)
        {
            if(!parts.TryGetValue(m,out var part)){part=new Part();parts.Add(m,part);order.Add(m);}
            return part;
        }
        void Raw(Material m,Vector3 a,Vector3 b,Vector3 c)
        {
            var p=For(m);int n=p.Vertices.Count;
            p.Vertices.Add(a);p.Vertices.Add(b);p.Vertices.Add(c);p.Indices.Add(n);p.Indices.Add(n+1);p.Indices.Add(n+2);
        }
        public void Tri(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 centre)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3-centre)<0)Raw(m,a,c,b);else Raw(m,a,b,c);
        }
        public void Quad(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 centre){Tri(m,a,b,c,centre);Tri(m,a,c,d,centre);}
        public void Sheet(Material m,Vector3 a,Vector3 b,Vector3 c){Raw(m,a,b,c);Raw(m,a,c,b);}
        public void SheetQuad(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d){Sheet(m,a,b,c);Sheet(m,a,c,d);}

        // Ring-to-ring solid with both end caps (rings must have the same point count).
        public void Prism(Material m,Vector3[] bottom,Vector3[] top)
        {
            var centre=Vector3.zero;foreach(var p in bottom)centre+=p;foreach(var p in top)centre+=p;centre/=bottom.Length*2;
            int n=bottom.Length;
            for(int i=0;i<n;i++){int j=(i+1)%n;Quad(m,bottom[i],bottom[j],top[j],top[i],centre);}
            for(int i=1;i<n-1;i++){Tri(m,bottom[0],bottom[i],bottom[i+1],centre);Tri(m,top[0],top[i],top[i+1],centre);}
        }

        public void Box(Material m,Vector3 centre,Vector3 size)=>Box(m,centre,size,Quaternion.identity);
        public void Box(Material m,Vector3 centre,Vector3 size,Quaternion rotation)
        {
            var h=size*.5f;
            Vector3 V(float x,float y,float z)=>centre+rotation*new Vector3(x*h.x,y*h.y,z*h.z);
            Prism(m,new[]{V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),V(-1,-1,1)},new[]{V(-1,1,-1),V(1,1,-1),V(1,1,1),V(-1,1,1)});
        }

        // Four side walls of a box, no top or bottom (8 triangles): for recessed glass bands
        // whose top and bottom are hidden by the wall slabs above and below.
        public void Sleeve(Material m,Vector3 centre,Vector3 size)=>Sleeve(m,centre,size,Quaternion.identity);
        public void Sleeve(Material m,Vector3 centre,Vector3 size,Quaternion rotation)
        {
            var h=size*.5f;
            Vector3 V(float x,float y,float z)=>centre+rotation*new Vector3(x*h.x,y*h.y,z*h.z);
            var b=new[]{V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),V(-1,-1,1)};var t=new[]{V(-1,1,-1),V(1,1,-1),V(1,1,1),V(-1,1,1)};
            for(int i=0;i<4;i++){int j=(i+1)%4;Quad(m,b[i],b[j],t[j],t[i],centre);}
        }

        // Rectangular beam between two points (width across, height roughly vertical).
        public void Beam(Material m,Vector3 from,Vector3 to,float width,float height)
        {
            var d=(to-from).normalized;var up=Mathf.Abs(d.y)<.95f?Vector3.up:Vector3.right;
            var u=Vector3.Cross(up,d).normalized*(width*.5f);var v=Vector3.Cross(d,u).normalized*(height*.5f);
            Prism(m,new[]{from-u-v,from+u-v,from+u+v,from-u+v},new[]{to-u-v,to+u-v,to+u+v,to-u+v});
        }

        // Thin open rail (no end caps, 8 triangles): for long fence rails seen side-on.
        public void Rail(Material m,Vector3 from,Vector3 to,float size)
        {
            var d=(to-from).normalized;var up=Mathf.Abs(d.y)<.95f?Vector3.up:Vector3.right;
            var u=Vector3.Cross(up,d).normalized*(size*.5f);var v=Vector3.Cross(d,u).normalized*(size*.5f);
            var a=new[]{from-u-v,from+u-v,from+u+v,from-u+v};var b=new[]{to-u-v,to+u-v,to+u+v,to-u+v};
            var centre=(from+to)*.5f;
            for(int i=0;i<4;i++){int j=(i+1)%4;Quad(m,a[i],a[j],b[j],b[i],centre);}
        }

        // Chamfered rectangle (octagon) footprint from y0 to y1, tapering from (hx0,hz0) to (hx1,hz1).
        public void Chamfered(Material m,Vector3 baseCentre,float hx0,float hz0,float hx1,float hz1,float y0,float y1,float chamfer)
        {
            Prism(m,Octagon(baseCentre,hx0,hz0,y0,chamfer),Octagon(baseCentre,hx1,hz1,y1,chamfer));
        }
        static Vector3[] Octagon(Vector3 c,float hx,float hz,float y,float k)
        {
            k=Mathf.Min(k,Mathf.Min(hx,hz)*.45f);
            return new[]{
                c+new Vector3(-hx+k,y,-hz),c+new Vector3(hx-k,y,-hz),c+new Vector3(hx,y,-hz+k),c+new Vector3(hx,y,hz-k),
                c+new Vector3(hx-k,y,hz),c+new Vector3(-hx+k,y,hz),c+new Vector3(-hx,y,hz-k),c+new Vector3(-hx,y,-hz+k)};
        }

        // Regular n-sided column.
        public void Column(Material m,Vector3 baseCentre,float r0,float r1,float height,int sides=8)
        {
            var a=new Vector3[sides];var b=new Vector3[sides];
            for(int i=0;i<sides;i++)
            {
                float t=(i+.5f)*Mathf.PI*2/sides;var d=new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));
                a[i]=baseCentre+d*r0;b[i]=baseCentre+d*r1+Vector3.up*height;
            }
            Prism(m,a,b);
        }

        // Seated spectator facing 'facing' (horizontal). 'seat' is the top of the step they sit on.
        public void Person(Vector3 seat,Vector3 facing,float scale,Material shirt,Material skin,Material hair,Material legs,bool cap,bool raisedArms)
        {
            var f=new Vector3(facing.x,0,facing.z).normalized;var r=Vector3.Cross(Vector3.up,f);var up=Vector3.up;
            Vector3 L(float across,float height,float forward)=>seat+(r*across+up*height+f*forward)*scale;
            // Legs: thighs forward, shins down to the step in front.
            Beam(legs,L(0,.47f,.02f),L(0,.47f,.42f),.34f*scale,.14f*scale);
            Beam(legs,L(0,.47f,.40f),L(0,.02f,.44f),.30f*scale,.13f*scale);
            // Torso: tapered octagon from hips to shoulders.
            var bottom=new Vector3[8];var top=new Vector3[8];
            for(int i=0;i<8;i++)
            {
                float t=(i+.5f)*Mathf.PI*2/8;float c=Mathf.Cos(t),s=Mathf.Sin(t);
                bottom[i]=L(c*.17f,.46f,s*.11f-.02f);top[i]=L(c*.21f,.98f,s*.12f-.02f);
            }
            Prism(shirt,bottom,top);
            // Arms.
            foreach(float side in new[]{-1f,1f})
            {
                var shoulder=L(side*.2f,.92f,-.01f);
                var hand=raisedArms?L(side*.3f,1.55f,.06f):L(side*.23f,.58f,.24f);
                Beam(shirt,shoulder,hand,.09f*scale,.09f*scale);
            }
            // Head: a faceted diamond with hair or a cap on top.
            var h=L(0,1.17f,0);float hr=.13f*scale,hy=.15f*scale;
            var pts=new[]{h+r*hr,h-r*hr,h+f*hr,h-f*hr};var crown=h+up*hy;var chin=h-up*hy;
            for(int i=0;i<4;i++)
            {
                var a=pts[i==0?0:i==1?2:i==2?1:3];var b=pts[i==0?2:i==1?1:i==2?3:0];
                Tri(skin,a,b,crown,h);Tri(skin,a,b,chin,h);
            }
            if(cap)
            {
                Box(hair,L(0,1.29f,0),new Vector3(.27f,.07f,.27f)*scale,Quaternion.LookRotation(f));
                Box(hair,L(0,1.27f,.17f),new Vector3(.22f,.03f,.13f)*scale,Quaternion.LookRotation(f));
            }
            else Box(hair,L(0,1.27f,-.02f),new Vector3(.25f,.08f,.25f)*scale,Quaternion.LookRotation(f));
        }

        // Cheaper spectator for big crowds (about 35 triangles): torso, head and hair only;
        // legs are hidden behind the bench in front. Optional raised arms.
        public void PersonLite(Vector3 seat,Vector3 facing,float scale,Material shirt,Material skin,Material hair,bool raisedArms)
        {
            var f=new Vector3(facing.x,0,facing.z).normalized;var r=Vector3.Cross(Vector3.up,f);
            Vector3 L(float across,float height,float forward)=>seat+(r*across+Vector3.up*height+f*forward)*scale;
            var bottom=new Vector3[6];var top=new Vector3[6];
            for(int i=0;i<6;i++)
            {
                float t=(i+.5f)*Mathf.PI*2/6;float c=Mathf.Cos(t),s=Mathf.Sin(t);
                bottom[i]=L(c*.19f,.05f,s*.13f);top[i]=L(c*.22f,.98f,s*.12f);
            }
            Prism(shirt,bottom,top);
            if(raisedArms)foreach(float side in new[]{-1f,1f})
                Beam(shirt,L(side*.2f,.9f,0),L(side*.32f,1.55f,.05f),.09f*scale,.09f*scale);
            var h=L(0,1.17f,0);float hr=.13f*scale,hy=.15f*scale;
            var pts=new[]{h+r*hr,h+f*hr,h-r*hr,h-f*hr};var crown=h+Vector3.up*hy;var chin=h-Vector3.up*hy;
            for(int i=0;i<4;i++)
            {
                var a=pts[i];var b=pts[(i+1)%4];
                Tri(skin,a,b,chin,h);
                // Upper half of the head is the hair / cap colour.
                Tri(hair,a,b,crown,h);
            }
        }

        // Bakes everything into one child object per material.
        public void Emit(Transform parent)
        {
            foreach(var m in order)
            {
                var part=parts[m];if(part.Indices.Count==0)continue;
                var mesh=new Mesh{name=label+" "+m.name};
                if(part.Vertices.Count>65000)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(part.Vertices);mesh.SetTriangles(part.Indices,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(label+" - "+m.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=m;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            parts.Clear();order.Clear();
        }
    }
}
