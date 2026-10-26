using UnityEngine;

namespace PolygonRacing
{
    // Only the title facing the viewer is visible; the shader also respects depth.
    public sealed class PolygonGantryLettering : MonoBehaviour
    {
        public TextMesh FrontText,RearText;
        public Material DepthMaterial;
        Camera view;
        MeshRenderer front,rear;
        void OnEnable()
        {
            Font.textureRebuilt+=FontRebuilt;
            BindAtlas();
        }
        void OnDisable(){Font.textureRebuilt-=FontRebuilt;}
        void FontRebuilt(Font font)
        {
            if(FrontText&&FrontText.font==font)BindAtlas();
        }
        void BindAtlas()
        {
            if(DepthMaterial&&FrontText&&FrontText.font)
                DepthMaterial.mainTexture=FrontText.font.material.mainTexture;
        }
        void LateUpdate()
        {
            if(!view)view=Camera.main;
            if(!view)return;
            if(!front&&FrontText)front=FrontText.GetComponent<MeshRenderer>();
            if(!rear&&RearText)rear=RearText.GetComponent<MeshRenderer>();
            bool approaching=Vector3.Dot(view.transform.position-transform.position,transform.forward)<0;
            if(front)front.enabled=approaching;
            if(rear)rear.enabled=!approaching;
        }
    }
}
