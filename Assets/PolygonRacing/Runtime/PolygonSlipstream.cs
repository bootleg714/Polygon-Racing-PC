using UnityEngine;

namespace PolygonRacing
{
    public static class PolygonSlipstream
    {
        public const float SpeedBonus = .08f;
        public const float AccelerationBonus = .12f;

        const float MinimumGap = 4.5f;
        const float MaximumGap = 28f;
        const float FullStrengthGap = 9f;
        const float MaximumLateralOffset = 5.2f;
        const float FullStrengthLateralOffset = 3.5f;
        const float BuildSeconds = 1.25f;
        const float CarryoverSeconds = 1.1f;
        const float FadeSeconds = .55f;

        public static float Measure(Vector3 followerPosition, Vector3 followerForward, float followerDistance,
            Vector3 targetPosition, Vector3 targetForward, float targetDistance)
        {
            float gap = Mathf.Repeat(targetDistance - followerDistance, PolygonCircuit.Length);
            if (gap < MinimumGap || gap > MaximumGap) return 0;

            Vector3 followerCenter = PolygonCircuit.AtDistance(followerDistance);
            Vector3 targetCenter = PolygonCircuit.AtDistance(targetDistance);
            followerPosition.y = 0;
            targetPosition.y = 0;
            float followerOffset = Vector3.Distance(followerPosition, followerCenter);
            float targetOffset = Vector3.Distance(targetPosition, targetCenter);
            if (followerOffset > PolygonWorld.RoadHalfWidth + 1
                || targetOffset > PolygonWorld.RoadHalfWidth + 1) return 0;

            Vector3 tangent = PolygonCircuit.Tangent(PolygonCircuit.AngleAtDistance(followerDistance));
            Vector3 side = Vector3.Cross(tangent, Vector3.up).normalized;
            Vector3 separation = targetPosition - followerPosition;
            float longitudinal = Vector3.Dot(separation, tangent);
            float lateral = Mathf.Abs(Vector3.Dot(separation, side));
            if (longitudinal <= 0 || Mathf.Abs(longitudinal - gap) > 5.5f
                || lateral > MaximumLateralOffset) return 0;
            if (Vector3.Dot(followerForward, targetForward) < .88f
                || Vector3.Dot(followerForward, tangent) < .8f
                || Vector3.Dot(targetForward, tangent) < .8f) return 0;

            float rangeStrength = Mathf.InverseLerp(MaximumGap, FullStrengthGap, gap);
            float alignmentStrength = Mathf.InverseLerp(MaximumLateralOffset, FullStrengthLateralOffset, lateral);
            return rangeStrength * alignmentStrength;
        }

        public sealed class Charge
        {
            public float Buildup { get; private set; }
            public float Strength { get; private set; }
            float carryover;
            float lastAlignment;

            public float Advance(float alignment, float deltaTime)
            {
                if (alignment > 0)
                {
                    Buildup = Mathf.MoveTowards(Buildup, 1, deltaTime / BuildSeconds);
                    carryover = CarryoverSeconds;
                    lastAlignment = alignment;
                    Strength = Buildup * alignment;
                }
                else if (carryover > 0)
                {
                    carryover = Mathf.Max(0, carryover - deltaTime);
                    Strength = Buildup * lastAlignment;
                }
                else
                {
                    Buildup = Mathf.MoveTowards(Buildup, 0, deltaTime / FadeSeconds);
                    Strength = Buildup * lastAlignment;
                    if (Buildup <= 0) lastAlignment = 0;
                }

                return Strength;
            }

            public void Reset()
            {
                Buildup = Strength = carryover = lastAlignment = 0;
            }
        }
    }
}
