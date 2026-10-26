using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PolygonRacing
{
    // Car-mounted course display. Head tracking, camera and race logic are untouched.
    [DefaultExecutionOrder(250)]
    public sealed class PolygonMiniMap : MonoBehaviour
    {
        PolygonDriver player;
        PolygonRival[] rivals;
        Transform panel,playerMarker;
        Transform[] rivalMarkers;
        Vector2 center;
        float scale;
        Shader shader;
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int> triangles=new List<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
        static void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            var driver=Object.FindFirstObjectByType<PolygonDriver>();
            if(!driver||Object.FindFirstObjectByType<PolygonMiniMap>())return;
            var go=new GameObject("Cockpit course mini-map");SceneManager.MoveGameObjectToScene(go,driver.gameObject.scene);go.AddComponent<PolygonMiniMap>();
        }
        Material Material(string name,Color color)
        {
            var mat=new Material(shader){name=name};mat.SetColor("_Color",color);materials.Add(mat);return mat;
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            if(Vector3.Cross(b-a,c-a).z>0){var swap=b;b=d;d=swap;}
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            meshes.Add(mesh);vertices.Clear();triangles.Clear();return mesh;
        }
        Transform Draw(string name,Mesh mesh,Material mat,int order)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(panel,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingOrder=order;return go.transform;
        }
        Vector3 Map(Vector3 p,float z=0)=>new Vector3((p.x-center.x)*scale,(p.z-center.y)*scale-.012f,z);
        void Ribbon(Vector3 a,Vector3 b,float width)
        {
            var delta=b-a;delta.z=0;if(delta.sqrMagnitude<.00000001f)return;
            var side=new Vector3(-delta.y,delta.x,0).normalized*width*.5f;
            Quad(a-side,a+side,b+side,b-side);
        }
        Mesh Dot()
        {
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI*2/16,b=(i+1)*Mathf.PI*2/16;int n=vertices.Count;
                vertices.Add(Vector3.zero);vertices.Add(new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*.0055f);vertices.Add(new Vector3(Mathf.Sin(b),Mathf.Cos(b),0)*.0055f);
                triangles.AddRange(new[]{n,n+1,n+2});
            }
            return Finish("Mini-map car dot");
        }
        void Start()
        {
            player=Object.FindFirstObjectByType<PolygonDriver>();if(!player){enabled=false;return;}
            shader=Resources.Load<Shader>("PolygonMiniMap");
            if(!shader){Debug.LogError("Mini-map shader missing: copy PolygonMiniMap.shader into Assets/PolygonRacing/Resources.");enabled=false;return;}
            rivals=Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None);
            var bounds=new Bounds(PolygonCircuit.AtDistance(0),Vector3.zero);
            for(int i=1;i<=256;i++)bounds.Encapsulate(PolygonCircuit.AtDistance(PolygonCircuit.Length*i/256));
            center=new Vector2(bounds.center.x,bounds.center.z);scale=Mathf.Min(.376f/Mathf.Max(1,bounds.size.x),.238f/Mathf.Max(1,bounds.size.z));
            panel=new GameObject("Mini-map - car mounted translucent display").transform;panel.SetParent(player.transform,false);
            panel.localScale=Vector3.one*.8f;
            panel.localPosition=new Vector3(.57f,.93f,.53f);panel.localRotation=Quaternion.LookRotation(panel.localPosition-PolygonDriver.Seat);
            var background=Material("Mini-map translucent teal",new Color(.015f,.13f,.15f,.28f));
            var border=Material("Mini-map teal frame",new Color(.13f,.68f,.64f,.8f));
            var route=Material("Mini-map ivory route",new Color(.91f,.95f,.87f,1));
            var pit=Material("Mini-map coral pit",new Color(1,.46f,.36f,1));
            Quad(new Vector3(-.22f,-.165f,.02f),new Vector3(.22f,-.165f,.02f),new Vector3(.22f,.165f,.02f),new Vector3(-.22f,.165f,.02f));
            Draw("Translucent map panel",Finish("Mini-map panel mesh"),background,0);
            var corners=new[]{new Vector3(-.22f,-.165f,.015f),new Vector3(.22f,-.165f,.015f),new Vector3(.22f,.165f,.015f),new Vector3(-.22f,.165f,.015f)};
            for(int i=0;i<4;i++)Ribbon(corners[i],corners[(i+1)%4],.0018f);
            Draw("Thin teal map frame",Finish("Mini-map frame mesh"),border,1);
            for(int i=0;i<256;i++)Ribbon(Map(PolygonCircuit.AtDistance(i*PolygonCircuit.Length/256)),Map(PolygonCircuit.AtDistance((i+1)*PolygonCircuit.Length/256)),.0027f);
            Draw("Fixed orientation circuit outline",Finish("Mini-map track mesh"),route,3);
            if(PolygonCircuit.Layout==PolygonCircuitLayout.CoastalExpressway)
            {
                for(int i=0;i<64;i++)Ribbon(Map(PolygonMarinaPit.Point(Mathf.Lerp(PolygonMarinaPit.Entry,PolygonMarinaPit.Exit,i/64f)),-.003f),Map(PolygonMarinaPit.Point(Mathf.Lerp(PolygonMarinaPit.Entry,PolygonMarinaPit.Exit,(i+1)/64f)),-.003f),.0015f);
                Draw("Marina pit branch",Finish("Mini-map pit mesh"),pit,4);
            }
            var start=Map(PolygonCircuit.AtDistance(0),-.005f);
            Quad(start+new Vector3(-.004f,-.004f,0),start+new Vector3(.004f,-.004f,0),start+new Vector3(.004f,.004f,0),start+new Vector3(-.004f,.004f,0));
            Draw("Start finish marker",Finish("Mini-map start square"),Material("Mini-map start",new Color(.35f,1,.65f,1)),5);
            Mesh dot=Dot();rivalMarkers=new Transform[rivals.Length];
            var colors=new[]{new Color(.1f,.48f,1,1),new Color(1,.8f,.12f,1),new Color(.15f,.95f,.4f,1)};
            for(int i=0;i<rivals.Length;i++)rivalMarkers[i]=Draw("Opponent dot "+rivals[i].GridSlot,dot,Material("Mini-map opponent "+i,colors[Mathf.Clamp(rivals[i].GridSlot,0,2)]),10);
            vertices.AddRange(new[]{new Vector3(0,.011f,0),new Vector3(.007f,-.008f,0),new Vector3(-.007f,-.008f,0)});triangles.AddRange(new[]{0,1,2});
            playerMarker=Draw("Player heading arrow",Finish("Mini-map player mesh"),Material("Mini-map player",new Color(1,.95f,.8f,1)),15);
            var label=PolygonWorld.Label("Mini-map title",panel,"CIRCUIT",new Vector3(0,.141f,-.005f),.0013f,new Color(.6f,.95f,.88f));
            label.GetComponent<Renderer>().sortingOrder=20;
        }
        void LateUpdate()
        {
            if(!player||!panel||!playerMarker)return;
            bool show=!player.RaceLocked;if(panel.gameObject.activeSelf!=show)panel.gameObject.SetActive(show);if(!show)return;
            playerMarker.localPosition=Map(player.transform.position,-.014f);
            var forward=player.transform.forward;playerMarker.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg);
            for(int i=0;i<rivals.Length;i++)
            {
                bool visible=rivals[i]&&rivals[i].Participating;
                if(rivalMarkers[i].gameObject.activeSelf!=visible)rivalMarkers[i].gameObject.SetActive(visible);
                if(visible)rivalMarkers[i].localPosition=Map(rivals[i].transform.position,-.01f);
            }
        }
        void OnDestroy()
        {
            if(panel)Destroy(panel.gameObject);foreach(var mat in materials)if(mat)Destroy(mat);foreach(var mesh in meshes)if(mesh)Destroy(mesh);
        }
    }
}
