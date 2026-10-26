using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing
{
    // Scenery pass v01: faceted model generators shared by the coastal art builder and the
    // forest refresh menu. Editor-time only (no update loops). Closed shapes are wound
    // outward from a local centre; thin sheets (fronds, sails) get both windings so they
    // render from either side with ordinary back-face culling.
    public static class PolygonSceneryMeshes
    {
        sealed class Builder
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<int> indices=new List<int>();
            void Raw(Vector3 a,Vector3 b,Vector3 c)
            {int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);}
            // Unity treats cross(b-a, c-a) as the front side.
            public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 centre)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3-centre)<0)Raw(a,c,b);else Raw(a,b,c);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 centre){Tri(a,b,c,centre);Tri(a,c,d,centre);}
            public void Sheet(Vector3 a,Vector3 b,Vector3 c){Raw(a,b,c);Raw(a,c,b);}
            public Mesh Finish(string name)
            {
                var mesh=new Mesh{name=name};
                if(vertices.Count>65000)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Small deterministic generator (same sequence as the design preview).
        sealed class Lcg
        {
            long state;
            public Lcg(int seed){state=seed&0x7fffffff;}
            public float Next(){state=(state*1103515245+12345)&0x7fffffff;return state/(float)0x7fffffff;}
        }

        static Vector3 Ring(float angle,float radius,float y,float offsetX=0,float offsetZ=0)
            =>new Vector3(offsetX+Mathf.Cos(angle)*radius,y,offsetZ+Mathf.Sin(angle)*radius);

        // ------------------------------------------------------------ palms
        static readonly Vector2[] PalmVariants={new Vector2(13,1.9f),new Vector2(11,1.2f),new Vector2(14.5f,2.6f)};   // height, lean
        public const int PalmVariantCount=3;

        // Segmented, curving trunk: each segment flares at its base, giving the ringed palm bark.
        public static Mesh PalmTrunk(int variant)
        {
            float h=PalmVariants[variant].x,lean=PalmVariants[variant].y;
            var b=new Builder();const int segments=8,sides=7;
            for(int s=0;s<segments;s++)
            {
                float y0=h*s/segments,y1=h*(s+1)/segments;
                float r=.52f-.26f*s/segments,rb=r*1.10f,rt=r*.90f;
                float x0=lean*(y0/h)*(y0/h),x1=lean*(y1/h)*(y1/h);
                var centre=new Vector3((x0+x1)*.5f,(y0+y1)*.5f,0);
                float rn=(.52f-.26f*(s+1)/segments)*1.10f;
                for(int k=0;k<sides;k++)
                {
                    float a=k*Mathf.PI*2/sides,c=(k+1)*Mathf.PI*2/sides;
                    b.Quad(Ring(a,rb,y0,x0),Ring(c,rb,y0,x0),Ring(c,rt,y1,x1),Ring(a,rt,y1,x1),centre);
                    if(s<segments-1)
                        b.Quad(Ring(a,rt,y1,x1),Ring(c,rt,y1,x1),Ring(c,rn,y1,x1),Ring(a,rn,y1,x1),centre+Vector3.down);
                }
            }
            return b.Finish("Ringed palm trunk "+variant);
        }

        // Half of a 12-frond crown (half 0 or 1), so alternate fronds can use two greens.
        public static Mesh PalmCrown(int variant,int half)
        {
            float h=PalmVariants[variant].x,lean=PalmVariants[variant].y;
            var root=new Vector3(lean,h,0);var b=new Builder();
            const int fronds=12,spans=7;
            var fold=new Vector3(0,.16f,0);
            for(int f=half;f<fronds;f+=2)
            {
                float a=f*Mathf.PI*2/fronds+variant*.2f;
                var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-d.z,0,d.x);
                bool longFrond=f%4<2;
                float length=longFrond?5.2f:4.2f,rise=longFrond?1.3f:1.0f,droop=longFrond?3.4f:2.4f;
                System.Func<float,Vector3> rib=s=>root+d*(length*s)+new Vector3(0,rise*4*s*(1-s)-droop*s*s,0);
                for(int i=0;i<spans;i++)
                {
                    float s0=i/(float)spans,s1=(i+1)/(float)spans,sm=(s0+s1)*.5f;
                    var r0=rib(s0);var r1=rib(s1);var rm=rib(sm);
                    float w0=i==0?.12f:Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*s0)),.7f);
                    float w1=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*s1)),.7f);   // Max: sin(PI) < 0 in float -> NaN
                    foreach(float sg in new[]{1f,-1f})
                    {
                        var e0=r0+side*(sg*w0);var e1=r1+side*(sg*w1);
                        var tooth=rm+side*(sg*Mathf.Max(w0,w1)*1.05f)+d*(length/spans*.35f);
                        b.Sheet(r0+fold,r1+fold,e0);
                        b.Sheet(r1+fold,e1,tooth);
                        b.Sheet(r1+fold,tooth,e0);
                    }
                }
            }
            return b.Finish("Serrated palm crown "+variant+"-"+half);
        }

        public static Mesh PalmCoconuts(int variant)
        {
            float h=PalmVariants[variant].x,lean=PalmVariants[variant].y;
            var root=new Vector3(lean,h-.45f,0);var b=new Builder();
            for(int k=0;k<4;k++)
            {
                float a=k*Mathf.PI/2+.5f;
                var c=root+new Vector3(Mathf.Cos(a)*.45f,-.15f*(k%2),Mathf.Sin(a)*.45f);
                Octahedron(b,c,.3f);
            }
            return b.Finish("Coconut cluster "+variant);
        }

        static void Octahedron(Builder b,Vector3 c,float r)
        {
            var x=new[]{c+Vector3.right*r,c+Vector3.left*r};var y=new[]{c+Vector3.up*r,c+Vector3.down*r};var z=new[]{c+Vector3.forward*r,c+Vector3.back*r};
            foreach(var px in x)foreach(var py in y)foreach(var pz in z)b.Tri(px,py,pz,c);
        }

        // ------------------------------------------------------------ rocks
        // Noisy, squashed icosphere: base y = 0, top y = 1, radius about 1 (same footprint as the old rock).
        public static Mesh Boulder(int seed,float roughness=.28f,float squash=.62f,string name="Faceted boulder")
        {
            var (points,faces)=Icosphere();var rng=new Lcg(seed);
            var dirs=new Vector3[4];var phases=new float[4];
            for(int i=0;i<4;i++)dirs[i]=new Vector3(rng.Next()*2-1,rng.Next()*2-1,rng.Next()*2-1);
            for(int i=0;i<4;i++)phases[i]=rng.Next()*6.283f;
            var shaped=new Vector3[points.Count];float minY=float.MaxValue,maxY=float.MinValue;
            for(int i=0;i<points.Count;i++)
            {
                var p=points[i];float noise=0;
                for(int k=0;k<4;k++)noise+=Mathf.Sin(Vector3.Dot(p,dirs[k])*3.1f+phases[k]);
                var q=p*(1+roughness*noise/2);
                q.y=Mathf.Max(q.y*squash,-.08f);
                shaped[i]=q;minY=Mathf.Min(minY,q.y);maxY=Mathf.Max(maxY,q.y);
            }
            for(int i=0;i<shaped.Length;i++)shaped[i].y=(shaped[i].y-minY)/(maxY-minY);
            var b=new Builder();var centre=new Vector3(0,.45f,0);
            foreach(var f in faces)b.Tri(shaped[f.x],shaped[f.y],shaped[f.z],centre);
            return b.Finish(name+" "+seed);
        }

        static (List<Vector3>,List<Vector3Int>) Icosphere()
        {
            float t=(1+Mathf.Sqrt(5))/2;
            var v=new List<Vector3>{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),
                new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),
                new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
            for(int i=0;i<v.Count;i++)v[i]=v[i].normalized;
            int[] f={0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                     3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1};
            var cache=new Dictionary<long,int>();
            int Mid(int i,int j)
            {
                long key=Mathf.Min(i,j)*100000L+Mathf.Max(i,j);
                if(cache.TryGetValue(key,out int index))return index;
                v.Add((v[i]+v[j]).normalized);cache[key]=v.Count-1;return v.Count-1;
            }
            var faces=new List<Vector3Int>();
            for(int k=0;k<f.Length;k+=3)
            {
                int a=f[k],b=f[k+1],c=f[k+2];int ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);
                faces.Add(new Vector3Int(a,ab,ca));faces.Add(new Vector3Int(b,bc,ab));
                faces.Add(new Vector3Int(c,ca,bc));faces.Add(new Vector3Int(ab,bc,ca));
            }
            return (v,faces);
        }

        // Stepped sandstone cliff: four stacked irregular prisms with ledges. Base 0, top 1, radius ~1.
        public static Mesh Mesa(int seed)
        {
            var rng=new Lcg(seed);var b=new Builder();const int sides=9;
            float[] heights={0,.38f,.66f,.86f,1};float[] radii={1,.84f,.64f,.40f};
            var offsets=new Vector2[4];
            for(int i=1;i<4;i++)offsets[i]=new Vector2((rng.Next()-.5f)*.18f,(rng.Next()-.5f)*.18f);
            var wobble=new float[4,sides];
            for(int l=0;l<4;l++)for(int k=0;k<sides;k++)wobble[l,k]=.82f+rng.Next()*.3f;
            Vector3[] previousTop=null;
            for(int l=0;l<4;l++)
            {
                float y0=heights[l],y1=heights[l+1];var o=offsets[l];
                var bottom=new Vector3[sides];var top=new Vector3[sides];
                for(int k=0;k<sides;k++)
                {
                    bottom[k]=Ring(k*Mathf.PI*2/sides,radii[l]*wobble[l,k],y0,o.x,o.y);
                    top[k]=new Vector3(o.x+(bottom[k].x-o.x)*.9f,y1,o.y+(bottom[k].z-o.y)*.9f);
                }
                var centre=new Vector3(o.x,(y0+y1)*.5f,o.y);
                for(int k=0;k<sides;k++){int j=(k+1)%sides;b.Quad(bottom[k],bottom[j],top[j],top[k],centre);}
                if(previousTop!=null)
                    for(int k=0;k<sides;k++){int j=(k+1)%sides;b.Quad(previousTop[k],previousTop[j],bottom[j],bottom[k],new Vector3(o.x,y0-1,o.y));}
                previousTop=top;
            }
            var cap=Vector3.zero;foreach(var p in previousTop)cap+=p;cap/=sides;
            for(int k=0;k<sides;k++)b.Tri(previousTop[k],previousTop[(k+1)%sides],cap,cap+Vector3.down);
            return b.Finish("Stepped sandstone cliff "+seed);
        }

        // ------------------------------------------------------------ boats
        // Local metres: bow toward +z, about 6.6 m long and 3 m wide.
        public static Mesh BoatHull()
        {
            var b=new Builder();var centre=new Vector3(0,.4f,0);
            float[,] sections={{-3.0f,1.15f,.95f},{-1.5f,1.45f,.95f},{0,1.5f,1.0f},{1.6f,1.25f,1.1f},{2.9f,.55f,1.3f},{3.6f,0,1.45f}};
            var rings=new Vector3[sections.GetLength(0)][];
            for(int s=0;s<rings.Length;s++)
            {
                float z=sections[s,0],w=sections[s,1],deck=sections[s,2];
                rings[s]=new[]{new Vector3(-w,deck,z),new Vector3(-w*.85f,.35f,z),new Vector3(0,-.35f,z),new Vector3(w*.85f,.35f,z),new Vector3(w,deck,z)};
            }
            for(int s=0;s<rings.Length-1;s++)
            {
                var a=rings[s];var c=rings[s+1];
                for(int i=0;i<4;i++)b.Quad(a[i],a[i+1],c[i+1],c[i],centre);
                b.Quad(a[4],a[0],c[0],c[4],centre+Vector3.down*5);
            }
            for(int i=1;i<4;i++)b.Tri(rings[0][0],rings[0][i],rings[0][i+1],centre+Vector3.forward*5);
            return b.Finish("Sailboat hull");
        }

        public static Mesh BoatCabin()
        {
            var b=new Builder();var centre=new Vector3(0,1.45f,-.6f);
            var lo=new[]{new Vector3(-.8f,1,-1.6f),new Vector3(.8f,1,-1.6f),new Vector3(.8f,1,.4f),new Vector3(-.8f,1,.4f)};
            var hi=new[]{new Vector3(-.65f,1.85f,-1.45f),new Vector3(.65f,1.85f,-1.45f),new Vector3(.65f,1.85f,.1f),new Vector3(-.65f,1.85f,.1f)};
            for(int k=0;k<4;k++)b.Quad(lo[k],lo[(k+1)%4],hi[(k+1)%4],hi[k],centre);
            b.Quad(hi[0],hi[1],hi[2],hi[3],centre+Vector3.down*5);
            return b.Finish("Sailboat cabin");
        }

        public static Mesh BoatRigging()
        {
            var b=new Builder();
            Beam(b,new Vector3(0,1,.8f),new Vector3(0,12.5f,.8f),.09f);      // mast
            Beam(b,new Vector3(0,2.2f,.8f),new Vector3(0,2.2f,-2.6f),.07f);  // boom
            return b.Finish("Sailboat mast and boom");
        }

        public static Mesh BoatSails()
        {
            var b=new Builder();
            b.Sheet(new Vector3(0,2.3f,.65f),new Vector3(0,12.2f,.72f),new Vector3(0,2.3f,-2.5f));   // mainsail
            b.Sheet(new Vector3(0,1.2f,1.0f),new Vector3(0,11.5f,.9f),new Vector3(0,1.25f,3.4f));    // jib
            return b.Finish("Sailboat sails");
        }

        static void Beam(Builder b,Vector3 from,Vector3 to,float r)
        {
            var d=(to-from).normalized;var up=Mathf.Abs(d.y)<.9f?Vector3.up:Vector3.right;
            var u=Vector3.Cross(d,up).normalized;var v=Vector3.Cross(u,d);var centre=(from+to)*.5f;
            for(int k=0;k<4;k++)
            {
                float a=k*Mathf.PI/2+Mathf.PI/4,c=a+Mathf.PI/2;
                var oa=(u*Mathf.Cos(a)+v*Mathf.Sin(a))*r;var oc=(u*Mathf.Cos(c)+v*Mathf.Sin(c))*r;
                b.Quad(from+oa,from+oc,to+oc,to+oa,centre);
            }
        }

        // ------------------------------------------------------------ lighthouse (local y = 0 at the base)
        // Seven tapered octagonal tiers; colour 0 = cream tiers, 1 = coral tiers plus the dome.
        public static Mesh LighthouseTiers(int colour)
        {
            var b=new Builder();const int sides=8,tiers=7;
            for(int t=0;t<tiers;t++)
            {
                if(t%2!=colour)continue;
                float y0=t*3f,y1=(t+1)*3f,r0=2.8f-t*.17f,r1=2.8f-(t+1)*.17f;
                OctPrism(b,r0,r1,y0,y1,false,false);
            }
            if(colour==1)
            {
                const float y=24.6f,r=2.1f;var apex=new Vector3(0,y+2.2f,0);
                for(int k=0;k<sides;k++)
                {
                    var p0=Ring((k+.5f)*Mathf.PI*2/sides,r,y);var p1=Ring((k+1.5f)*Mathf.PI*2/sides,r,y);
                    b.Tri(p0,p1,apex,new Vector3(0,y+.5f,0));
                    b.Tri(p0,p1,new Vector3(0,y,0),new Vector3(0,y+1,0));
                }
            }
            return b.Finish("Lighthouse tiers "+colour);
        }

        public static Mesh LighthouseGallery()
        {
            var b=new Builder();
            OctPrism(b,2.2f,3.4f,20.6f,21.3f,true,true);     // flared gallery deck
            for(int k=0;k<16;k++)
            {
                float a=k*Mathf.PI*2/16;var p=new Vector3(Mathf.Cos(a)*3.2f,0,Mathf.Sin(a)*3.2f);
                Beam(b,p+Vector3.up*21.3f,p+Vector3.up*22.4f,.06f);
            }
            OctPrism(b,3.25f,3.25f,22.35f,22.5f,true,true);  // hand rail
            return b.Finish("Lighthouse gallery");
        }

        public static Mesh LighthouseLantern()
        {
            var b=new Builder();OctPrism(b,1.7f,1.7f,21.3f,24.6f,false,false);
            return b.Finish("Lighthouse lantern glazing");
        }

        static void OctPrism(Builder b,float r0,float r1,float y0,float y1,bool capBottom,bool capTop)
        {
            const int sides=8;var centre=new Vector3(0,(y0+y1)*.5f,0);
            var p0=new Vector3[sides];var p1=new Vector3[sides];
            for(int k=0;k<sides;k++){float a=(k+.5f)*Mathf.PI*2/sides;p0[k]=Ring(a,r0,y0);p1[k]=Ring(a,r1,y1);}
            for(int k=0;k<sides;k++){int j=(k+1)%sides;b.Quad(p0[k],p0[j],p1[j],p1[k],centre);}
            for(int k=1;k<sides-1;k++)
            {
                if(capBottom)b.Tri(p0[0],p0[k],p0[k+1],centre+Vector3.up*5);
                if(capTop)b.Tri(p1[0],p1[k],p1[k+1],centre+Vector3.down*5);
            }
        }

        // ------------------------------------------------------------ forest
        // Unit-height pine (scale the transform by the tree height). Variants: tier count / proportions.
        public static readonly Vector2[] PineVariants={new Vector2(1,3),new Vector2(1.15f,4),new Vector2(.85f,3)};   // height scale, tiers

        public static Mesh PineTrunk()
        {
            var b=new Builder();const int sides=5;const float top=.45f,r0=.034f,r1=.012f;
            var centre=new Vector3(0,top*.5f,0);
            for(int k=0;k<sides;k++)
            {
                float a=k*Mathf.PI*2/sides,c=(k+1)*Mathf.PI*2/sides;
                b.Quad(Ring(a,r0,0),Ring(c,r0,0),Ring(c,r1,top),Ring(a,r1,top),centre);
            }
            return b.Finish("Pine trunk");
        }

        public static Mesh PineCrown(int variant)
        {
            int tiers=(int)PineVariants[variant].y;var rng=new Lcg(100+variant*7);var b=new Builder();const int sides=7;
            for(int t=0;t<tiers;t++)
            {
                float f=t/(float)tiers;
                float baseY=.18f+.62f*f,top=t<tiers-1?baseY+(.42f-.08f*f):1;
                float radius=.34f-.22f*f,twist=rng.Next()*6.283f;
                var rim=new Vector3[sides];
                for(int k=0;k<sides;k++)
                {
                    float r=radius*(k%2==0?1:.78f);
                    rim[k]=Ring(twist+k*Mathf.PI*2/sides,r,baseY-(k%2==0?.012f:0));
                }
                var apex=new Vector3(0,top,0);var under=new Vector3(0,baseY+.05f,0);
                var centre=new Vector3(0,(baseY+top)*.5f,0);
                for(int k=0;k<sides;k++)
                {
                    int j=(k+1)%sides;
                    b.Tri(rim[k],rim[j],apex,centre);
                    b.Tri(rim[k],rim[j],under,centre+Vector3.up);
                }
            }
            return b.Finish("Tiered pine crown "+variant);
        }

        // Distant rounded mountain: base 0, top 1, radius ~1.
        public static Mesh Mountain(int seed)=>Boulder(seed,.38f,1f,"Faceted mountain");
    }
}
