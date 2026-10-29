using UnityEngine;

namespace PolygonRacing
{
    // A single distant launch ship; detach before static batching and honor race pause.
    [DefaultExecutionOrder(-100)]
    public sealed class PolygonStarfallLaunch : MonoBehaviour
    {
        public Vector3 StartPoint;
        public float TravelHeight=180, Speed=12, Phase=.08f;
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
            if(TravelHeight<=0)return;
            travel=Mathf.Repeat(travel+Speed*Time.deltaTime/(TravelHeight*2),1);
            transform.localPosition=StartPoint+Vector3.up*(Mathf.PingPong(travel*2,1)*TravelHeight);
        }
    }
}
