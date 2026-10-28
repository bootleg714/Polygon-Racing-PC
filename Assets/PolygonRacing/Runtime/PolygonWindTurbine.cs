using UnityEngine;

namespace PolygonRacing
{
    // Turns a wind turbine rotor about its own forward (hub) axis; pausing (time scale 0) stops it.
    public sealed class PolygonWindTurbine : MonoBehaviour
    {
        public float DegreesPerSecond=22;

        // PolygonDriver.Start statically batches everything under "Circuit - editable scenery",
        // which would freeze the blades. Awake runs first, so step out of that root (keeping the
        // world pose) and stay a normal moving object.
        void Awake()
        {
            var p=transform.parent;
            while(p&&p.name!="Circuit - editable scenery")p=p.parent;
            if(p&&p.parent)transform.SetParent(p.parent,true);
        }

        void Update()
        {
            transform.Rotate(0,0,DegreesPerSecond*Time.deltaTime,Space.Self);
        }
    }
}
