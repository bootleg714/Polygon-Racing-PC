using UnityEngine;

namespace PolygonRacing
{
    [CreateAssetMenu(fileName="PolygonRivalPace",menuName="Polygon Racing/Opponent Pace Settings")]
    public sealed class PolygonRivalPaceSettings : ScriptableObject
    {
        [Header("Baseline pace - shared by all three opponents")]
        [Tooltip("Relative to the player's configured topSpeed, before drafting and catch-up.")]
        [Range(.85f,1.15f)] public float topSpeedFactor=1.01f;
        [Range(.6f,1.2f)] public float accelerationFactor=.98f;
        [Range(7,14)] public float cornerGrip=10.6f;

        [Header("Gentle race-distance catch-up")]
        public bool rubberBanding=true;
        [Tooltip("No gap assistance inside this distance, ahead or behind.")]
        [Min(5)] public float neutralGapMeters=40;
        [Tooltip("Reach the full adjustment at this gap. Uses total race distance, not wrapped track position.")]
        [Min(10)] public float fullAdjustmentGapMeters=220;
        [Tooltip("Maximum extra straight speed when behind. Does not raise corner grip.")]
        [Range(0,.15f)] public float maximumCatchup=.08f;
        [Tooltip("Maximum straight-speed reduction for an opponent well ahead.")]
        [Range(0,.1f)] public float maximumLeaderEase=.04f;
        [Tooltip("Seconds to blend toward an adjustment; larger values are more gradual.")]
        [Range(1,10)] public float responseSeconds=4;

        // Positive gap means this opponent is behind the player.
        public float TargetAdjustment(float gapMeters)
        {
            if(!rubberBanding)return 0;
            float neutral=Mathf.Max(5,neutralGapMeters);
            float full=Mathf.Max(neutral+1,fullAdjustmentGapMeters);
            float strength=Mathf.SmoothStep(0,1,Mathf.InverseLerp(neutral,full,Mathf.Abs(gapMeters)));
            return gapMeters>0?strength*Mathf.Clamp(maximumCatchup,0,.15f):-strength*Mathf.Clamp(maximumLeaderEase,0,.1f);
        }
        void OnValidate()
        {
            topSpeedFactor=Mathf.Clamp(topSpeedFactor,.85f,1.15f);
            accelerationFactor=Mathf.Clamp(accelerationFactor,.6f,1.2f);cornerGrip=Mathf.Clamp(cornerGrip,7,14);
            neutralGapMeters=Mathf.Max(5,neutralGapMeters);
            fullAdjustmentGapMeters=Mathf.Max(neutralGapMeters+1,fullAdjustmentGapMeters);
            maximumCatchup=Mathf.Clamp(maximumCatchup,0,.15f);maximumLeaderEase=Mathf.Clamp(maximumLeaderEase,0,.1f);
            responseSeconds=Mathf.Clamp(responseSeconds,1,10);
        }
    }
}
