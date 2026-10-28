using UnityEngine;

namespace PolygonRacing
{
    [DefaultExecutionOrder(-100)]
    public sealed class PolygonStarfallPortMotion : MonoBehaviour
    {
        public float DegreesPerSecond=18;
        public Vector3 Axis=Vector3.forward;
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
            player=Object.FindFirstObjectByType<PolygonDriver>();
        }
        void Update()
        {
            if(player&&player.IsPaused)return;
            transform.Rotate(Axis,DegreesPerSecond*Time.deltaTime,Space.Self);
        }
    }
}
