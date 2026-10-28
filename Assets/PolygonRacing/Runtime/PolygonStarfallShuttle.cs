using UnityEngine;

namespace PolygonRacing
{
    [DefaultExecutionOrder(-100)]
    public sealed class PolygonStarfallShuttle : MonoBehaviour
    {
        public Vector3 StartPoint,EndPoint;
        public float Speed=25,Phase=.2f;
        float travel;
        PolygonDriver player;
        void Awake()
        {
            var scenery=transform;
            while(scenery&&scenery.name!=PolygonStarfallPortArt.SceneryName)scenery=scenery.parent;
            if(scenery&&scenery.parent)
            {
                var root=scenery.parent;var moving=root.Find(PolygonStarfallPortArt.AnimatedName);
                if(!moving){moving=new GameObject(PolygonStarfallPortArt.AnimatedName).transform;moving.SetParent(root,false);}
                transform.SetParent(moving,true);
            }
            player=Object.FindFirstObjectByType<PolygonDriver>();travel=Mathf.Repeat(Phase,1);
        }
        void Update()
        {
            if(player&&player.IsPaused)return;
            float length=Vector3.Distance(StartPoint,EndPoint);if(length<1)return;
            travel=Mathf.Repeat(travel+Speed*Time.deltaTime/(length*2),1);
            transform.localPosition=Vector3.Lerp(StartPoint,EndPoint,Mathf.PingPong(travel*2,1));
            transform.localRotation=Quaternion.LookRotation((travel<.5f?1:-1)*(EndPoint-StartPoint));
        }
    }
}
