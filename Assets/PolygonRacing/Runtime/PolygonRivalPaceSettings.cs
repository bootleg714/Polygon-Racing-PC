using UnityEngine;

namespace PolygonRacing
{
    [CreateAssetMenu(fileName="PolygonRivalPace",menuName="Polygon Racing/Opponent Pace Settings")]
    public sealed class PolygonRivalPaceSettings : ScriptableObject
    {
        [Header("Baseline pace - shared by all three opponents")]
        [Tooltip("Relative to the player's configured topSpeed, before drafting, personality and catch-up.")]
        [Range(.85f,1.15f)] public float topSpeedFactor=1.01f;
        [Range(.6f,1.2f)] public float accelerationFactor=.98f;
        [Tooltip("Scales how often opponents make small mistakes (braking early or running wide). 0 = never.")]
        [Range(0,2)] public float mistakeFrequency=1;
        [Tooltip("Scales how eagerly opponents pull out to pass and defend the inside before a bend.")]
        [Range(0,1.5f)] public float aggression=1;

        [Header("Catch-up by time gap to the player")]
        public bool rubberBanding=true;
        [Tooltip("No assistance while the opponent is within this many seconds of the player.")]
        [Range(0,3)] public float neutralGapSeconds=.8f;
        [Tooltip("Full assistance at this many seconds of gap.")]
        [Range(1,10)] public float fullGapSeconds=3.5f;
        [Tooltip("Extra speed and cornering skill for an opponent behind the player.")]
        [Range(0,.15f)] public float catchUpBoost=.04f;
        [Tooltip("Speed reduction (and more mistakes) for an opponent ahead of the player, so the player can close in.")]
        [Range(0,.15f)] public float leaderEase=.07f;
        [Tooltip("Seconds to blend toward an adjustment; larger values are more gradual.")]
        [Range(1,10)] public float responseSeconds=4;

        // Retained so existing assets keep their saved values; replaced by the second-based gaps above.
        [HideInInspector] public float cornerGrip=10.6f;
        [HideInInspector] public float neutralGapMeters=40;
        [HideInInspector] public float fullAdjustmentGapMeters=220;
        [HideInInspector] public float maximumCatchup=.08f;
        [HideInInspector] public float maximumLeaderEase=.04f;

        // Positive gap means this opponent is behind the player.
        public float TargetAdjustment(float gapSeconds)
        {
            if(!rubberBanding)return 0;
            float neutral=Mathf.Max(0,neutralGapSeconds);
            float full=Mathf.Max(neutral+.1f,fullGapSeconds);
            float strength=Mathf.SmoothStep(0,1,Mathf.InverseLerp(neutral,full,Mathf.Abs(gapSeconds)));
            return gapSeconds>0?strength*Mathf.Clamp(catchUpBoost,0,.15f):-strength*Mathf.Clamp(leaderEase,0,.15f);
        }
        void OnValidate()
        {
            topSpeedFactor=Mathf.Clamp(topSpeedFactor,.85f,1.15f);
            accelerationFactor=Mathf.Clamp(accelerationFactor,.6f,1.2f);
            mistakeFrequency=Mathf.Clamp(mistakeFrequency,0,2);aggression=Mathf.Clamp(aggression,0,1.5f);
            neutralGapSeconds=Mathf.Clamp(neutralGapSeconds,0,3);
            fullGapSeconds=Mathf.Clamp(Mathf.Max(neutralGapSeconds+.1f,fullGapSeconds),1,10);
            catchUpBoost=Mathf.Clamp(catchUpBoost,0,.15f);leaderEase=Mathf.Clamp(leaderEase,0,.15f);
            responseSeconds=Mathf.Clamp(responseSeconds,1,10);
        }
    }
}
