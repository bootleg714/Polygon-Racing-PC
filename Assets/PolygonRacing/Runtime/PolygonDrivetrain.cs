using System;

namespace PolygonRacing
{
    // Display/audio drivetrain only: does not change the accepted vehicle acceleration.
    public sealed class PolygonDrivetrain
    {
        public const float WheelRadius = .39f, FinalDrive = 5.2f;
        public const float IdleRpm = 1100f, RedlineRpm = 9000f, UpshiftRpm = 7600f, DownshiftRpm = 3500f;
        public const float MinimumShiftInterval = .55f;
        static readonly float[] Ratios = { 2.6933f, 2.0669f, 1.6285f, 1.3153f, 1.0961f, .9395f };
        public int Gear { get; private set; } = 1;
        public float Rpm { get; private set; } = IdleRpm;
        public float Throttle { get; private set; }
        public float CoupledRpm { get; private set; }
        float sinceShift = MinimumShiftInterval;
        public static int GearCount => Ratios.Length;
        public static float Ratio(int gear) => Ratios[gear-1];
        public static float WheelCoupledRpm(float speed, int gear)
            => Math.Max(0, speed) / WheelRadius * (60f / (2f * (float)Math.PI)) * Ratio(gear) * FinalDrive;
        public void Reset()
        {
            Gear=1;Rpm=IdleRpm;CoupledRpm=0;Throttle=0;sinceShift=MinimumShiftInterval;
        }
        public void Step(float speed, float throttle, float dt)
        {
            if(dt<=0)return;
            Throttle=Math.Max(0,Math.Min(1,throttle));
            sinceShift+=dt;
            float coupled=WheelCoupledRpm(speed,Gear);
            // Downshifts must track braking immediately, even when a large speed drop
            // requires several safe gears in one frame. Only upshifts use the cooldown.
            bool downshifted=false;
            while(Gear>1 && WheelCoupledRpm(speed,Gear)<=DownshiftRpm &&
                  WheelCoupledRpm(speed,Gear-1)<RedlineRpm)
            { Gear--;downshifted=true; }
            if(downshifted)sinceShift=0;
            else if(sinceShift>=MinimumShiftInterval && Gear<GearCount && coupled>=UpshiftRpm)
            { Gear++;sinceShift=0; }
            CoupledRpm=WheelCoupledRpm(speed,Gear);
            // Low-speed clutch slip permits a free-revving idle/launch without stalling.
            float slip=(float)Math.Max(0,1-Math.Max(0,speed)/5f);
            float launchRpm=IdleRpm+Throttle*1800f*slip;
            Rpm=Math.Max(CoupledRpm,launchRpm);
        }
    }
}
