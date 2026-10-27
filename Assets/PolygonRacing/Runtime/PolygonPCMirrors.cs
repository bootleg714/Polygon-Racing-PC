using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PolygonRacing
{
    // Player-only, runtime-owned rear views in the existing faceted mirror housings.
    [DisallowMultipleComponent]
    public sealed class PolygonPCMirrors : MonoBehaviour
    {
        public const string RuntimeRootName = "PC rear-view mirrors - runtime";
        [Range(64,1024)] public int textureWidth=256;
        [Range(32,512)] public int textureHeight=86;
        [Range(1,60)] public int updatesPerSecond=30;
        [Range(20,80)] public float verticalFieldOfView=36;
        [Range(0,25)] public float outwardAngle=10;
        [Range(50,1000)] public float viewDistance=300;
        public bool mirrorShadows=false;

        PolygonDriver driver;
        Transform runtimeRoot;
        readonly Camera[] cameras=new Camera[2];
        readonly RenderTexture[] textures=new RenderTexture[2];
        readonly Material[] materials=new Material[2];
        readonly float[] due=new float[2];
        readonly List<Renderer> hidden=new List<Renderer>();
        readonly List<bool> previousHidden=new List<bool>();
        Mesh surfaceMesh;
        int nextSide;
        bool subscribed,hiding;

        void OnEnable()
        {
            if(!Application.isPlaying)return;
            driver=GetComponent<PolygonDriver>();
            var shader=Resources.Load<Shader>("PolygonPCMirror");
            if(!driver||!driver.view||!shader)
            {Debug.LogError("PC mirrors: player camera or mirror shader is missing.");return;}
            try
            {
                runtimeRoot=new GameObject(RuntimeRootName).transform;runtimeRoot.SetParent(transform,false);
                surfaceMesh=CreateFace();
                int width=Mathf.Clamp(textureWidth,64,1024),height=Mathf.Clamp(textureHeight,32,512);
                for(int i=0;i<2;i++)
                {
                    float side=i==0?-1:1;
                    var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32;
                    var rt=new RenderTexture(width,height,16,format,RenderTextureReadWrite.Linear)
                    {name=i==0?"PC left mirror view":"PC right mirror view",antiAliasing=1,filterMode=FilterMode.Bilinear,useMipMap=false,autoGenerateMips=false};
                    textures[i]=rt;
                    if(!rt.Create())throw new System.InvalidOperationException("Could not create mirror render texture.");
                    var mat=new Material(shader){name=i==0?"PC left mirror image":"PC right mirror image"};
                    mat.SetTexture("_MirrorTex",rt);materials[i]=mat;
                    var face=new GameObject(i==0?"Left mirror image":"Right mirror image",typeof(MeshFilter),typeof(MeshRenderer));
                    face.transform.SetParent(runtimeRoot,false);
                    // Existing glass spans x +/- .069, y +/- .0245, front z .156.
                    face.transform.localPosition=new Vector3(side*.64f,.86f,.1545f);
                    face.GetComponent<MeshFilter>().sharedMesh=surfaceMesh;
                    var renderer=face.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;

                    var go=new GameObject(i==0?"Left rear-view camera":"Right rear-view camera",typeof(Camera));
                    go.transform.SetParent(runtimeRoot,false);go.transform.localPosition=new Vector3(side*.64f,.86f,.19f);
                    float angle=Mathf.Clamp(outwardAngle,0,25)*Mathf.Deg2Rad;
                    go.transform.localRotation=Quaternion.LookRotation(new Vector3(side*Mathf.Sin(angle),0,-Mathf.Cos(angle)),Vector3.up);
                    var cam=go.GetComponent<Camera>();cameras[i]=cam;
                    cam.enabled=false;cam.targetTexture=rt;cam.depth=driver.view.depth-10+i*.01f;
                    cam.fieldOfView=Mathf.Clamp(verticalFieldOfView,20,80);cam.aspect=(float)width/height;
                    cam.nearClipPlane=.25f;cam.farClipPlane=Mathf.Clamp(viewDistance,50,1000);
                    cam.clearFlags=CameraClearFlags.Skybox;cam.backgroundColor=RenderSettings.fogColor;
                    cam.allowHDR=true;cam.allowMSAA=false;cam.useOcclusionCulling=true;
                    int uiLayer=LayerMask.NameToLayer("UI");cam.cullingMask=driver.view.cullingMask;
                    if(uiLayer>=0)cam.cullingMask &= ~(1<<uiLayer);
                    var data=go.AddComponent<UniversalAdditionalCameraData>();
                    data.renderPostProcessing=false;data.renderShadows=mirrorShadows;
                    data.requiresDepthTexture=false;data.requiresColorTexture=false;
                }
                RenderPipelineManager.beginCameraRendering+=BeforeCamera;
                RenderPipelineManager.endCameraRendering+=AfterCamera;subscribed=true;
                due[0]=due[1]=Time.unscaledTime;
            }
            catch(System.Exception ex){Debug.LogException(ex);Cleanup();}
        }
        static Mesh CreateFace()
        {
            var mesh=new Mesh{name="PC mirror face"};
            mesh.vertices=new[]{new Vector3(-.065f,-.0215f,0),new Vector3(.065f,-.0215f,0),new Vector3(.065f,.0215f,0),new Vector3(-.065f,.0215f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();return mesh;
        }
        void LateUpdate()
        {
            if(!driver||!runtimeRoot)return;
            for(int i=0;i<2;i++)if(cameras[i])cameras[i].enabled=false;
            if(driver.IsPaused)return; // retain the most recent image without extra render work
            float now=Time.unscaledTime,period=1f/Mathf.Clamp(updatesPerSecond,1,60);
            // Stagger left and right across frames; at most one extra camera renders per frame.
            for(int k=0;k<2;k++)
            {
                int i=(nextSide+k)%2;
                if(!cameras[i]||now<due[i])continue;
                cameras[i].enabled=true;due[i]=Mathf.Max(due[i]+period,now);nextSide=1-i;break;
            }
        }
        void BeforeCamera(ScriptableRenderContext context,Camera camera)
        {
            if(!driver||(camera!=cameras[0]&&camera!=cameras[1]))return;
            RestorePlayer();hidden.Clear();previousHidden.Clear();
            driver.GetComponentsInChildren(true,hidden);
            foreach(var r in hidden){previousHidden.Add(r&&r.forceRenderingOff);if(r)r.forceRenderingOff=true;}
            hiding=true;
        }
        void AfterCamera(ScriptableRenderContext context,Camera camera)
        {
            if(camera==cameras[0]||camera==cameras[1])RestorePlayer();
        }
        void RestorePlayer()
        {
            if(!hiding)return;
            for(int i=0;i<hidden.Count;i++)if(hidden[i])hidden[i].forceRenderingOff=previousHidden[i];
            hidden.Clear();previousHidden.Clear();hiding=false;
        }
        void OnDisable(){Cleanup();}
        void Cleanup()
        {
            RestorePlayer();
            if(subscribed){RenderPipelineManager.beginCameraRendering-=BeforeCamera;RenderPipelineManager.endCameraRendering-=AfterCamera;subscribed=false;}
            for(int i=0;i<2;i++)
            {
                if(cameras[i]){cameras[i].enabled=false;cameras[i].targetTexture=null;cameras[i]=null;}
                if(materials[i]){Destroy(materials[i]);materials[i]=null;}
                if(textures[i]){textures[i].Release();Destroy(textures[i]);textures[i]=null;}
            }
            if(surfaceMesh){Destroy(surfaceMesh);surfaceMesh=null;}
            if(runtimeRoot){Destroy(runtimeRoot.gameObject);runtimeRoot=null;}
        }
    }
}
