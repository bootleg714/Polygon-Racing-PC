using System;
using System.IO;
using PolygonRacing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class TrailerCapture {
 static readonly string[] scenes={"CoastalExpressway","DowntownCircuit","RivieraHarbour","SakuraHills","NordwaldRing","NeonMeridian","IonRift","AuroraForge","StarfallPort"};
 static readonly float[] knots={7.0f,5.2f,19.0f,11.0f,48.4f,16.0f,12.4f,16.2f,12.4f};
 public static void Run(){try {
  string output=Path.GetFullPath("../frames"); Directory.CreateDirectory(output);
  QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");

  for(int s=0;s<scenes.Length;s++){
   EditorSceneManager.OpenScene("Assets/PolygonRacing/Scenes/"+scenes[s]+".unity"); PolygonCircuit.SetLayout((PolygonCircuitLayout)(s+1));
   var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=2};target.Create();
   var go=new GameObject("Trailer capture camera"); var camera=go.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=2500;camera.nearClipPlane=.2f;camera.fieldOfView=68;camera.allowHDR=true;camera.useOcclusionCulling=false;
   var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=-1;data.volumeTrigger=go.transform;camera.targetTexture=target;
   string dir=Path.Combine(output,s.ToString("00"));Directory.CreateDirectory(dir);float start=PolygonCircuit.DistanceAtKnot(knots[s]);
   for(int f=0;f<45;f++){
    float d=start+f*3.4f; float side=(s%2==0?1:-1)*(s%3==0?14:5); float height=s%3==0?12:s%3==1?3.4f:6;
    var eye=PolygonBanking.SurfacePoint(d,side,height);var look=PolygonBanking.SurfacePoint(d+65,0,2);go.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(look-eye));
    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
    RenderTexture.active=target;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,f.ToString("000")+".jpg"),tex.EncodeToJPG(88));Object.DestroyImmediate(tex);
   }
   RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(go);target.Release();Object.DestroyImmediate(target);Debug.Log("TRAILER_SCENE_DONE "+scenes[s]);
  }
  Debug.Log("TRAILER_CAPTURE_COMPLETE");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}

