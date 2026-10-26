using UnityEngine;

namespace PolygonRacing
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PolygonCircuitSelector : MonoBehaviour
    {
        [SerializeField] PolygonCircuitLayout layout;

        public PolygonCircuitLayout Layout
        {
            get=>layout;
            set=>layout=value;
        }

        void Awake()
        {
            PolygonCircuit.SetLayout(layout);
        }
    }
}
