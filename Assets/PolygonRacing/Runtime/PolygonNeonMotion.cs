using UnityEngine;

namespace PolygonRacing
{
    // Transit and fans escape the driver's scenery static batching before Start runs.
    [DefaultExecutionOrder(-100)]
    public sealed class PolygonNeonMotion : MonoBehaviour
    {
        public enum Motion { Spin, Shuttle }
        public Motion Mode;
        public float AngularSpeed=38,Speed=19,Phase;
        public Vector3 StartPoint,EndPoint;
        float travel;
        PolygonDriver player;
        void Awake()
        {
            var scenery=transform;
            while(scenery&&scenery.name!=PolygonNeonArt.SceneryName)scenery=scenery.parent;
            if(scenery&&scenery.parent)
            {
                var root=scenery.parent;var moving=root.Find(PolygonNeonArt.AnimatedName);
                if(!moving){moving=new GameObject(PolygonNeonArt.AnimatedName).transform;moving.SetParent(root,false);}
                transform.SetParent(moving,true);
            }
            player=Object.FindFirstObjectByType<PolygonDriver>();
            travel=Mathf.Repeat(Phase,1);
        }
        void Update()
        {
            if(player&&player.IsPaused)return;
            if(Mode==Motion.Spin)transform.Rotate(Vector3.forward,AngularSpeed*Time.deltaTime,Space.Self);
            else
            {
                float distance=Vector3.Distance(StartPoint,EndPoint);if(distance<1)return;
                travel=Mathf.Repeat(travel+Speed*Time.deltaTime/(distance*2),1);
                float t=Mathf.PingPong(travel*2,1);transform.localPosition=Vector3.Lerp(StartPoint,EndPoint,t);
                transform.localRotation=Quaternion.LookRotation((travel<.5f?1:-1)*(EndPoint-StartPoint));
            }
        }
    }
}
