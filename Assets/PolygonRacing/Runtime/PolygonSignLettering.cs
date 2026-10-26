using UnityEngine;

namespace PolygonRacing
{
    // Keeps the shared depth-tested sign lettering material bound to the dynamic font atlas,
    // so roadside sign text is hidden behind rocks and cliffs instead of drawing through them.
    public sealed class PolygonSignLettering : MonoBehaviour
    {
        public Font Font;
        public Material DepthMaterial;
        void OnEnable(){Font.textureRebuilt+=Rebuilt;Bind();}
        void OnDisable(){Font.textureRebuilt-=Rebuilt;}
        void Rebuilt(Font font){if(font==Font)Bind();}
        void Bind(){if(Font&&DepthMaterial&&Font.material)DepthMaterial.mainTexture=Font.material.mainTexture;}
    }
}
