using System.Collections;
using UnityEngine;

namespace PolygonRacing
{
    // Runtime-only overlays sit in front of the existing wheel display and nine LED faces.
    // One component on the existing steering pivot survives car refreshes without duplicates.
    [DisallowMultipleComponent]
    public sealed class PolygonCarInstruments : MonoBehaviour
    {
        public const string RuntimeRootName = "PC steering instruments - runtime";
        PolygonDriver driver;
        TextMesh gearText,rpmText;
        MeshRenderer[] lights;
        Mesh lightMesh;
        Material[] lightMaterials;
        Material textMaterial;
        Transform root;
        int lastGear=-1,lastRpm=-1,lastLights=-1;
        bool layoutReady;
        const float GearHalfWidth=.022f,RpmHalfWidth=.034f,TextHalfHeight=.022f;

        void Start()
        {
            driver=GetComponentInParent<PolygonDriver>();
            if(!driver){enabled=false;return;}
            var shader=Resources.Load<Shader>("PolygonFlatPC");
            var fontShader=Resources.Load<Shader>("PolygonGantryLettering");
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!shader||!fontShader||!font){Debug.LogError("PC steering instruments: required shader or font missing.");enabled=false;return;}
            root=new GameObject(RuntimeRootName).transform;root.SetParent(transform,false);
            textMaterial=new Material(fontShader){name="PC steering screen text"};textMaterial.mainTexture=font.material.mainTexture;
            Font.textureRebuilt+=RefreshFont;
            gearText=MakeText("Gear readout",font,new Vector3(-.043f,.025f,-.021f),.027f);
            rpmText=MakeText("RPM readout",font,new Vector3(.030f,.025f,-.021f),.0105f);
            lightMaterials=new Material[4];
            var colors=new[]{new Color(.025f,.035f,.035f),new Color(.12f,.92f,.28f),new Color(1,.72f,.06f),new Color(1,.09f,.04f)};
            for(int i=0;i<4;i++)
            {
                var m=new Material(shader){name="Steering shift lamp "+i};
                m.SetColor("_BaseColor",colors[i]);m.SetColor("_EmissionColor",i==0?Color.black:colors[i]*.38f);m.SetFloat("_Cull",0);
                lightMaterials[i]=m;
            }
            lightMesh=new Mesh{name="Steering shift lamp face"};
            lightMesh.vertices=new[]{new Vector3(-.006f,-.004f,0),new Vector3(.006f,-.004f,0),new Vector3(.006f,.004f,0),new Vector3(-.006f,.004f,0)};
            lightMesh.triangles=new[]{0,1,2,0,2,3};lightMesh.RecalculateBounds();
            lights=new MeshRenderer[9];
            for(int i=0;i<lights.Length;i++)
            {
                var go=new GameObject("Shift light "+(i+1),typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(-.064f+i*.016f,.065f,-.019f);
                go.GetComponent<MeshFilter>().sharedMesh=lightMesh;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=lightMaterials[0];
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;lights[i]=r;
            }
            // Hide text during calibration so oversized test strings never flash on screen.
            gearText.transform.localScale=Vector3.zero;rpmText.transform.localScale=Vector3.zero;
            StartCoroutine(CalibrateText());
            RefreshDisplay();
        }
        TextMesh MakeText(string label,Font font,Vector3 position,float size)
        {
            var go=new GameObject(label,typeof(TextMesh));go.transform.SetParent(root,false);go.transform.localPosition=position;
            var text=go.GetComponent<TextMesh>();text.font=font;text.fontSize=64;text.characterSize=size;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.55f,1,.83f);
            go.GetComponent<MeshRenderer>().sharedMaterial=textMaterial;return text;
        }
        IEnumerator CalibrateText()
        {
            // Measure actual TextMesh local bounds after Unity builds each glyph mesh. Every
            // gear and all five-digit repeated numerals are covered, including the RPM label.
            // The margins keep both readouts inside the original screen and below the LEDs.
            float gx=0,gy=0,rx=0,ry=0;
            var gearBounds=new Vector2[PolygonDrivetrain.GearCount];var rpmBounds=new Vector2[10];
            var endOfFrame=new WaitForEndOfFrame();
            for(int i=0;i<10;i++)
            {
                if(i<PolygonDrivetrain.GearCount)gearText.text=(i+1).ToString();
                rpmText.text=new string((char)('0'+i),5)+"\nRPM";
                yield return endOfFrame;
                if(!gearText||!rpmText)yield break;
                if(i<PolygonDrivetrain.GearCount)
                {
                    AccumulateBounds(gearText,out float x,out float y,ref gx,ref gy);
                    gearBounds[i]=new Vector2(x,y);
                }
                AccumulateBounds(rpmText,out float a,out float b,ref rx,ref ry);
                rpmBounds[i]=new Vector2(a,b);
            }
            if(gx<=0||gy<=0||rx<=0||ry<=0)
            { Debug.LogError("PC steering instrument text bounds are unavailable; display remains hidden.");yield break; }
            float gs=Mathf.Min(1f,Mathf.Min(GearHalfWidth/gx,TextHalfHeight/gy));
            float rs=Mathf.Min(1f,Mathf.Min(RpmHalfWidth/rx,TextHalfHeight/ry));
            foreach(var b in gearBounds)
                if(b.x*gs>GearHalfWidth+.00001f||b.y*gs>TextHalfHeight+.00001f)
                { Debug.LogError("PC steering gear text exceeds its screen area.");yield break; }
            foreach(var b in rpmBounds)
                if(b.x*rs>RpmHalfWidth+.00001f||b.y*rs>TextHalfHeight+.00001f)
                { Debug.LogError("PC steering RPM text exceeds its screen area.");yield break; }
            gearText.transform.localScale=Vector3.one*gs;rpmText.transform.localScale=Vector3.one*rs;
            layoutReady=true;lastGear=-1;lastRpm=-1;RefreshDisplay();
        }
        static void AccumulateBounds(TextMesh text,out float x,out float y,ref float maxX,ref float maxY)
        {
            var bounds=text.GetComponent<MeshRenderer>().localBounds;
            x=Mathf.Abs(bounds.center.x)+bounds.extents.x;y=Mathf.Abs(bounds.center.y)+bounds.extents.y;
            maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);
        }
        void RefreshFont(Font font){if(gearText&&gearText.font==font&&textMaterial)textMaterial.mainTexture=font.material.mainTexture;}
        void LateUpdate(){if(driver&&root)RefreshDisplay();}
        void RefreshDisplay()
        {
            var state=driver.Drivetrain;
            int rpm=Mathf.RoundToInt(state.Rpm/100f)*100;
            if(layoutReady&&state.Gear!=lastGear){gearText.text=state.Gear.ToString();lastGear=state.Gear;}
            if(layoutReady&&rpm!=lastRpm){rpmText.text=rpm.ToString()+"\nRPM";lastRpm=rpm;}
            int count=Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(4200,PolygonDrivetrain.RedlineRpm,state.Rpm)*10),0,9);
            if(count==lastLights)return;
            for(int i=0;i<lights.Length;i++)lights[i].sharedMaterial=lightMaterials[i<count?(i<3?1:i<6?2:3):0];
            lastLights=count;
        }
        void OnDestroy()
        {
            Font.textureRebuilt-=RefreshFont;
            if(root)Destroy(root.gameObject);
            if(lightMesh)Destroy(lightMesh);
            if(lightMaterials!=null)foreach(var m in lightMaterials)if(m)Destroy(m);
            if(textMaterial)Destroy(textMaterial);
        }
    }
}
