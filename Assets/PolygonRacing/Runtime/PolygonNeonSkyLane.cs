using UnityEngine;

namespace PolygonRacing
{
    // Skyline pass (October 29, 2026): one ring of flying traffic. The whole lane is a single
    // baked mesh that slowly turns around the circuit centre, so dozens of craft cost one renderer
    // and one Update. It leaves "Circuit - editable scenery" in Awake, before PolygonDriver.Start
    // static-batches that hierarchy (the wind-turbine lesson), and holds still while paused.
    [DefaultExecutionOrder(-100)]
    public sealed class PolygonNeonSkyLane : MonoBehaviour
    {
        public float DegreesPerSecond=3;
        PolygonDriver player;
        void Awake()
        {
            var t=transform.parent;
            while(t&&t.name!="Circuit - editable scenery")t=t.parent;
            if(t&&t.parent)transform.SetParent(t.parent,true);
            player=Object.FindFirstObjectByType<PolygonDriver>();
        }
        void Update()
        {
            if(player&&player.IsPaused)return;
            transform.Rotate(0,DegreesPerSecond*Time.deltaTime,0,Space.World);
        }
    }
}
