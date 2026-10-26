using UnityEngine;

namespace PolygonRacing
{
    [DefaultExecutionOrder(150)]
    public sealed class PolygonStartLights : MonoBehaviour
    {
        public MeshRenderer[] Lenses;
        public Material DarkMaterial,RedMaterial;
        PolygonRace race;
        int displayed=-1;
        void Awake(){SetCount(0);}
        void LateUpdate()
        {
            if(!race)race=Object.FindFirstObjectByType<PolygonRace>();
            SetCount(race?race.StartLightCount:0);
        }
        void OnDisable(){SetCount(0);}
        void SetCount(int count)
        {
            if(count==displayed||Lenses==null)return;
            displayed=count;
            for(int i=0;i<Lenses.Length;i++)
                if(Lenses[i])Lenses[i].sharedMaterial=i<count?RedMaterial:DarkMaterial;
        }
    }
}
