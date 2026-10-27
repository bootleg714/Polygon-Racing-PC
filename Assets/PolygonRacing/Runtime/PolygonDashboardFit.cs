using UnityEngine;

namespace PolygonRacing
{
    // Independently fit four single-line regions within the existing physical panel.
    public sealed class PolygonDashboardFit : MonoBehaviour
    {
        // Normalized panel-local rectangles, with 5% outside padding and row gutters.
        static readonly Rect[] Regions={new Rect(-.45f,.12f,.90f,.33f),
            new Rect(-.45f,-.17f,.43f,.25f),new Rect(.02f,-.17f,.43f,.25f),
            new Rect(-.45f,-.45f,.90f,.23f)};
        const float SpeedScale=.67f,DetailScale=.65f;
        readonly TextMesh[] rows=new TextMesh[4];
        readonly MeshRenderer[] renderers=new MeshRenderer[4];
        TextMesh display;
        Transform panel,extraRoot;
        Vector3 initialScale,initialPosition;
        Quaternion initialRotation;
        public bool Ready {get;private set;}

        public void Configure(TextMesh text,Transform panelTransform)
        {
            if(Ready)return;
            if(!text||!panelTransform||!panelTransform.GetComponent<MeshRenderer>()||!text.GetComponent<MeshRenderer>())
            {Debug.LogWarning("Cockpit dashboard fit: panel or text renderer missing.");return;}
            display=text;panel=panelTransform;initialScale=text.transform.localScale;
            initialPosition=text.transform.localPosition;initialRotation=text.transform.localRotation;
            rows[0]=text;renderers[0]=text.GetComponent<MeshRenderer>();
            extraRoot=new GameObject("Cockpit dashboard rows - runtime").transform;
            extraRoot.SetParent(text.transform.parent,false);
            for(int i=1;i<rows.Length;i++)
            {
                var go=new GameObject("Dashboard row "+i,typeof(TextMesh));go.transform.SetParent(extraRoot,false);
                var row=go.GetComponent<TextMesh>();row.font=text.font;row.fontSize=text.fontSize;row.characterSize=text.characterSize;
                row.lineSpacing=text.lineSpacing;row.anchor=TextAnchor.MiddleCenter;row.alignment=TextAlignment.Center;
                row.fontStyle=text.fontStyle;row.richText=text.richText;row.color=text.color;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=renderers[0].sharedMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                rows[i]=row;renderers[i]=renderer;
            }
            Ready=true;
        }
        public void SetRows(string main,string left,string right,string status)
        {
            if(!Ready)return;
            SetRow(0,main,SpeedScale);SetRow(1,left,DetailScale);SetRow(2,right,DetailScale);SetRow(3,status,DetailScale);
        }
        void SetRow(int index,string value,float preferredScale)
        {
            var text=rows[index];var t=text.transform;var region=Regions[index];
            if(text.text!=value)text.text=value;
            // The panel and labels share the car parent. Preserve the original text depth.
            var center=panel.TransformPoint(new Vector3(region.center.x,region.center.y,0));
            var local=t.parent.InverseTransformPoint(center);local.z=initialPosition.z;
            t.localPosition=local;t.localRotation=initialRotation;
            t.localScale=new Vector3(initialScale.x*preferredScale,initialScale.y*preferredScale,initialScale.z);
            if(string.IsNullOrEmpty(value))return;
            float factor=RequiredScale(t,renderers[index],region);
            if(factor<=0){t.localScale=new Vector3(initialScale.x*.25f,initialScale.y*.25f,initialScale.z);return;}
            if(factor<1)
                t.localScale=new Vector3(initialScale.x*preferredScale*factor*.98f,initialScale.y*preferredScale*factor*.98f,initialScale.z);
        }
        float RequiredScale(Transform textTransform,MeshRenderer textRenderer,Rect region)
        {
            var bounds=textRenderer.localBounds;
            if(bounds.size.x<=0||bounds.size.y<=0)return 0;
            var origin=panel.InverseTransformPoint(textTransform.position);
            float availableX=Mathf.Min(origin.x-region.xMin,region.xMax-origin.x);
            float availableY=Mathf.Min(origin.y-region.yMin,region.yMax-origin.y);
            if(availableX<=0||availableY<=0)return 0;
            float extentX=0,extentY=0;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
            {
                var corner=bounds.center+new Vector3(x*bounds.extents.x,y*bounds.extents.y,0);
                var point=panel.InverseTransformPoint(textTransform.TransformPoint(corner));
                extentX=Mathf.Max(extentX,Mathf.Abs(point.x-origin.x));
                extentY=Mathf.Max(extentY,Mathf.Abs(point.y-origin.y));
            }
            if(extentX<=0||extentY<=0)return 0;
            return Mathf.Min(1,Mathf.Min(availableX/extentX,availableY/extentY));
        }
        void OnDestroy()
        {
            if(extraRoot)Destroy(extraRoot.gameObject);
            if(display){display.transform.localScale=initialScale;display.transform.localPosition=initialPosition;display.transform.localRotation=initialRotation;}
            Ready=false;
        }
    }
}
