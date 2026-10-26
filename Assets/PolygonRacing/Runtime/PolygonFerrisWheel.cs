using UnityEngine;

namespace PolygonRacing
{
    // Slowly turns the amusement-park wheel; gondolas stay hanging upright. Lives outside the
    // statically batched circuit scenery so it can move.
    public sealed class PolygonFerrisWheel : MonoBehaviour
    {
        public Transform Wheel;
        public Transform[] Gondolas;
        public float DegreesPerSecond=3.5f;

        void Update()
        {
            if(!Wheel)return;
            Wheel.Rotate(0,0,DegreesPerSecond*Time.deltaTime,Space.Self);
            if(Gondolas==null)return;
            var upright=transform.rotation;
            foreach(var g in Gondolas)if(g)g.rotation=upright;
        }
    }
}
