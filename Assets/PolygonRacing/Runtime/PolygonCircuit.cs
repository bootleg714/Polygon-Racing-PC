using UnityEngine;

namespace PolygonRacing
{
    public enum PolygonCircuitLayout
    {
        Forest,
        CoastalExpressway,
        Downtown,
        Riviera,
        Sakura,
        Nordwald,
        NeonMeridian,
        IonRift=7,
        AuroraForge=8,
        StarfallPort=9
    }

    // Shared geometry for scenery, race position and AI routes.
    public static class PolygonCircuit
    {
        const int Samples=1024;
        static readonly Vector3[] forestKnots={
            new Vector3(0,0,100),new Vector3(150,0,100),new Vector3(300,0,100),new Vector3(400,0,100),
            new Vector3(435,0,65),new Vector3(428,0,20),new Vector3(385,0,-10),
            new Vector3(417,0,-55),new Vector3(395,0,-98),new Vector3(200,0,-120),
            new Vector3(-35,0,-120),new Vector3(-120,0,-95),new Vector3(-165,0,-50),
            new Vector3(-165,0,15),new Vector3(-115,0,76),new Vector3(-80,0,100)};
        static readonly Vector3[] coastalKnots={
            new Vector3(0,0,0),new Vector3(168,0,0),new Vector3(336,0,0),new Vector3(504,0,0),
            new Vector3(672,0,0),new Vector3(823.2f,0,33.6f),new Vector3(940.8f,0,84),new Vector3(1008,0,252),
            new Vector3(1008,0,403.2f),new Vector3(940.8f,0,537.6f),new Vector3(823.2f,0,621.6f),
            new Vector3(672,0,655.2f),new Vector3(504,0,655.2f),new Vector3(336,0,655.2f),
            new Vector3(168,0,655.2f),new Vector3(0,0,655.2f),new Vector3(-151.2f,0,638.4f),
            new Vector3(-277.2f,0,571.2f),new Vector3(-352.8f,0,453.6f),new Vector3(-369.6f,0,319.2f),
            new Vector3(-327.6f,0,218.4f),new Vector3(-252,0,184.8f),new Vector3(-176.4f,0,126),
            new Vector3(-197.4f,0,50.4f),new Vector3(-294,0,-33.6f),new Vector3(-352.8f,0,-134.4f),
            new Vector3(-252,0,-252),new Vector3(-117.6f,0,-151.2f)};
        // Downtown street circuit: city-block grid with 90-degree corners (about 35 m radius),
        // long boulevards for drafting and two block-step jogs. Knots sit 32 m either side of
        // each grid corner, so consecutive knots on one street keep the road exactly straight.
        static readonly Vector3[] downtownKnots={
            new Vector3(32f,0,0f),new Vector3(182f,0,0f),new Vector3(332f,0,0f),new Vector3(482f,0,0f),new Vector3(632f,0,0f),
            new Vector3(668f,0,0f),new Vector3(700f,0,32f),new Vector3(700f,0,182f),new Vector3(700f,0,332f),new Vector3(700f,0,388f),
            new Vector3(668f,0,420f),new Vector3(518f,0,420f),new Vector3(412f,0,420f),new Vector3(380f,0,452f),new Vector3(380f,0,602f),
            new Vector3(380f,0,668f),new Vector3(348f,0,700f),new Vector3(198f,0,700f),new Vector3(48f,0,700f),new Vector3(-102f,0,700f),
            new Vector3(-252f,0,700f),new Vector3(-288f,0,700f),new Vector3(-320f,0,668f),new Vector3(-320f,0,518f),new Vector3(-320f,0,368f),
            new Vector3(-320f,0,292f),new Vector3(-288f,0,260f),new Vector3(-152f,0,260f),new Vector3(-120f,0,228f),new Vector3(-120f,0,78f),
            new Vector3(-120f,0,32f),new Vector3(-88f,0,0f),new Vector3(-32f,0,0f)};
        // Riviera harbour circuit: harbour start, uphill drag to the casino square, downhill to a
        // tight hairpin, seafront tunnel, harbour chicane, pool section, final hairpin. The route
        // stays in X/Z; rivieraHeights gives the road elevation at each knot (same B-spline basis).
        static readonly Vector3[] rivieraKnots={new Vector3(-120f,0,0f),new Vector3(-20f,0,0f),new Vector3(80f,0,0f),new Vector3(150f,0,0f),new Vector3(184f,0,8f),new Vector3(200f,0,40f),new Vector3(204f,0,110f),new Vector3(214f,0,180f),new Vector3(222f,0,250f),new Vector3(240f,0,300f),new Vector3(280f,0,335f),new Vector3(330f,0,350f),new Vector3(380f,0,352f),new Vector3(410f,0,340f),new Vector3(440f,0,346f),new Vector3(470f,0,330f),new Vector3(482f,0,295f),new Vector3(478f,0,260f),new Vector3(455f,0,240f),new Vector3(420f,0,236f),new Vector3(392.9f,0,237.9f),new Vector3(377.3f,0,230.2f),new Vector3(371f,0,214f),new Vector3(377.3f,0,197.8f),new Vector3(392.9f,0,190.1f),new Vector3(420f,0,190f),new Vector3(470f,0,186f),new Vector3(520f,0,180f),new Vector3(540f,0,150f),new Vector3(548f,0,90f),new Vector3(550f,0,20f),new Vector3(545f,0,-50f),new Vector3(532f,0,-110f),new Vector3(515f,0,-160f),new Vector3(505f,0,-190f),new Vector3(470f,0,-205f),new Vector3(455f,0,-240f),new Vector3(440f,0,-275f),new Vector3(400f,0,-290f),new Vector3(330f,0,-292f),new Vector3(260f,0,-290f),new Vector3(220f,0,-280f),new Vector3(190f,0,-292f),new Vector3(140f,0,-300f),new Vector3(80f,0,-300f),new Vector3(40f,0,-288f),new Vector3(0f,0,-296f),new Vector3(-60f,0,-300f),new Vector3(-120f,0,-298f),new Vector3(-160f,0,-282f),new Vector3(-172f,0,-250f),new Vector3(-176f,0,-180f),new Vector3(-178f,0,-100f),new Vector3(-176f,0,-50f),new Vector3(-168f,0,-14f)};
        static readonly float[] rivieraHeights={0f,0f,0f,0.5f,1.5f,3f,9f,17f,26f,33f,37f,39f,40f,40f,39.5f,38f,35f,32f,29f,27f,26f,25.5f,25f,24.5f,24f,22f,20f,17f,14f,11f,9f,7f,5.5f,4f,3f,2.4f,2f,1.6f,1.3f,1.2f,1f,1f,1f,1f,1f,1f,1f,1f,1f,1f,1f,0.8f,0.5f,0.2f,0f};
        // Sakura Hills: a figure-eight in rolling countryside. The downhill run from the top of the
        // east loop passes under the back straight on a bridge (lower crossing near knot 19.9, upper
        // near knot 45.4, about 9.5 m apart), so race position on this layout uses height too.
        static readonly Vector3[] sakuraKnots={
            new Vector3(440,0,80),new Vector3(520,0,80),new Vector3(600,0,80),new Vector3(680,0,80),
            new Vector3(745,0,92),new Vector3(785,0,135),new Vector3(795,0,195),
            new Vector3(765,0,250),new Vector3(810,0,300),new Vector3(760,0,355),new Vector3(810,0,410),
            new Vector3(780,0,475),new Vector3(735,0,515),new Vector3(670,0,530),
            new Vector3(600,0,525),new Vector3(545,0,500),new Vector3(490,0,460),
            new Vector3(410,0,410),new Vector3(330,0,360),new Vector3(250,0,310),new Vector3(170,0,260),new Vector3(90,0,205),new Vector3(10,0,150),
            new Vector3(-80,0,100),new Vector3(-180,0,80),new Vector3(-290,0,80),new Vector3(-400,0,82),
            new Vector3(-470,0,84),new Vector3(-494,0,90),new Vector3(-503,0,110),new Vector3(-496,0,132),new Vector3(-472,0,146),new Vector3(-440,0,156),
            new Vector3(-380,0,185),new Vector3(-320,0,200),new Vector3(-280,0,240),
            new Vector3(-275,0,300),new Vector3(-295,0,360),new Vector3(-290,0,420),new Vector3(-250,0,465),new Vector3(-190,0,480),
            new Vector3(-130,0,460),new Vector3(-60,0,420),new Vector3(10,0,375),new Vector3(80,0,330),new Vector3(150,0,285),new Vector3(220,0,240),
            new Vector3(275,0,195),new Vector3(320,0,160),
            new Vector3(345,0,148),new Vector3(378,0,140),new Vector3(386,0,110),new Vector3(410,0,88)};
        static readonly float[] sakuraHeights={4.8f,4.1f,3.4f,2.7f,2.0f,2.0f,2.7f,4.8f,8.3f,11.8f,15.3f,17.4f,18.8f,18.8f,18.1f,16.7f,14.6f,11.8f,8.3f,4.8f,2.7f,2.0f,2.7f,4.1f,5.5f,6.2f,6.2f,6.2f,6.2f,6.2f,6.2f,6.9f,7.6f,9.0f,11.1f,13.2f,16.0f,17.4f,18.1f,18.8f,18.8f,18.4f,17.2f,15.6f,14.0f,12.8f,12.2f,11.2f,10.0f,8.6f,7.4f,6.4f,5.6f};
        // Nordwald Ring: a long, narrow forest circuit with big elevation changes: twisty esses, a
        // crest, a plunge into the valley, a long climb to a banked hairpin at the top of the hill,
        // spectator corners and jumps on the way down, and a long straight back to the line.
        static readonly Vector3[] nordwaldKnots={new Vector3(0,0,0),new Vector3(110,0,0),new Vector3(220,0,0),new Vector3(310,0,12),new Vector3(360,0,60),new Vector3(415,0,52),new Vector3(455,0,105),new Vector3(512,0,98),new Vector3(548,0,150),new Vector3(600,0,160),new Vector3(650,0,225),new Vector3(715,0,290),new Vector3(780,0,355),new Vector3(830,0,430),new Vector3(845,0,515),new Vector3(852,0,575),new Vector3(878,0,602),new Vector3(915,0,605),new Vector3(965,0,640),new Vector3(990,0,715),new Vector3(975,0,800),new Vector3(935,0,860),new Vector3(890,0,885),new Vector3(870,0,915),new Vector3(830,0,925),new Vector3(800,0,955),new Vector3(740,0,955),new Vector3(675,0,930),new Vector3(615,0,945),new Vector3(555,0,905),new Vector3(500,0,870),new Vector3(440,0,822),new Vector3(380,0,800),new Vector3(320,0,812),new Vector3(265,0,800),new Vector3(225,0,808),new Vector3(205,0,835),new Vector3(205,0,875),new Vector3(180,0,920),new Vector3(120,0,942),new Vector3(40,0,970),new Vector3(-50,0,1000),new Vector3(-140,0,1012),new Vector3(-230,0,1045),new Vector3(-320,0,1060),new Vector3(-400,0,1090),new Vector3(-470,0,1115),new Vector3(-530,0,1140),new Vector3(-570,0,1170),new Vector3(-597,0,1195),new Vector3(-620,0,1201),new Vector3(-636,0,1185),new Vector3(-634,0,1161),new Vector3(-614,0,1143),new Vector3(-592,0,1105),new Vector3(-588,0,1055),new Vector3(-595,0,995),new Vector3(-615,0,930),new Vector3(-650,0,880),new Vector3(-640,0,815),new Vector3(-600,0,770),new Vector3(-615,0,710),new Vector3(-655,0,650),new Vector3(-670,0,575),new Vector3(-650,0,500),new Vector3(-605,0,445),new Vector3(-640,0,385),new Vector3(-600,0,325),new Vector3(-580,0,280),new Vector3(-605,0,240),new Vector3(-598,0,205),new Vector3(-620,0,140),new Vector3(-610,0,70),new Vector3(-570,0,22),new Vector3(-500,0,0),new Vector3(-400,0,-8),new Vector3(-300,0,-8),new Vector3(-200,0,-6),new Vector3(-100,0,-2)};
        static readonly float[] nordwaldHeights={30f,30f,29.5f,28.5f,27f,25.5f,24f,23f,22.5f,23f,25f,28f,31.5f,29f,30f,31f,31f,30f,26f,20f,14f,10f,9f,9f,9.5f,9f,7.5f,6f,5f,3.5f,2f,0.5f,0f,0f,0.3f,0.5f,1.5f,3f,5f,8f,11f,14f,17.5f,21f,25f,29f,33f,38f,43f,46f,47f,47f,47f,47f,48f,48.5f,47.5f,45f,42f,39f,36f,34f,31f,33.5f,28f,25f,23f,21.5f,21f,21f,21.5f,22f,23f,24.5f,26f,27f,28f,29f,29.5f};
        // Banked sections: first knot, last knot, degrees (positive raises the right-hand side).
        static readonly Vector3[] nordwaldBanks={new Vector3(48.6f,53.4f,18)};
        // Neon Meridian: a continuous city circuit with a long skyline climb, a sweeping
        // descent, an enclosed undercity, and a raised return. Existing layouts retain their IDs.
        static readonly Vector3[] neonKnots={
            new Vector3(0,0,0),new Vector3(138,0,0),new Vector3(276,0,0),new Vector3(414,0,12),
            new Vector3(518,0,69),new Vector3(581,0,161),new Vector3(615,0,276),new Vector3(638,0,391),
            new Vector3(684,0,500),new Vector3(748,0,598),new Vector3(782,0,713),new Vector3(748,0,828),
            new Vector3(656,0,886),new Vector3(529,0,886),new Vector3(403,0,857),new Vector3(276,0,874),
            new Vector3(173,0,943),new Vector3(40,0,978),new Vector3(-86,0,943),new Vector3(-161,0,851),
            new Vector3(-190,0,736),new Vector3(-167,0,621),new Vector3(-213,0,518),new Vector3(-328,0,489),
            new Vector3(-449,0,541),new Vector3(-564,0,518),new Vector3(-638,0,431),new Vector3(-633,0,305),
            new Vector3(-552,0,207),new Vector3(-460,0,127),new Vector3(-477,0,23),new Vector3(-552,0,-52),
            new Vector3(-558,0,-150),new Vector3(-472,0,-230),new Vector3(-351,0,-253),new Vector3(-230,0,-230),
            new Vector3(-150,0,-161),new Vector3(-132,0,-75),new Vector3(-75,0,-12)};
        static readonly float[] neonHeights={20,20,20,22,28,36,45,56,67,78,86,90,92,89,81,66,48,30,16,10,16,25,32,28,16,8,5,7,15,22,27,24,16,9,8,12,17,20,20};
        static readonly Vector3[] neonBanks={new Vector3(9.4f,13.1f,-5),new Vector3(24.6f,28.5f,-4)};
        // Ion Rift: east-rim climb (4-10), high overlook (10-14), long canyon descent
        // (14-22), reactor gorge (22-25), rolling basin (25-29) and return bridge (29-35).
        // Original, non-crossing plan. Broad grade transitions retain the approved neon feel.
        static readonly Vector3[] ionKnots={
            new Vector3(0,0,0),new Vector3(151.2f,0,0),new Vector3(302.4f,0,0),new Vector3(453.6f,0,0),
            new Vector3(594,0,27),new Vector3(691.2f,0,108),new Vector3(734.4f,0,248.4f),new Vector3(702,0,399.6f),
            new Vector3(745.2f,0,540),new Vector3(777.6f,0,691.2f),new Vector3(745.2f,0,842.4f),new Vector3(658.8f,0,961.2f),
            new Vector3(518.4f,0,1031.4f),new Vector3(356.4f,0,1036.8f),new Vector3(194.4f,0,1009.8f),new Vector3(32.4f,0,1036.8f),
            new Vector3(-129.6f,0,1004.4f),new Vector3(-248.4f,0,907.2f),new Vector3(-264.6f,0,756),new Vector3(-216,0,615.6f),
            new Vector3(-253.8f,0,475.2f),new Vector3(-356.4f,0,405),new Vector3(-502.2f,0,432),new Vector3(-626.4f,0,378),
            new Vector3(-664.2f,0,248.4f),new Vector3(-702,0,108),new Vector3(-723.6f,0,-43.2f),new Vector3(-702,0,-194.4f),
            new Vector3(-604.8f,0,-291.6f),new Vector3(-453.6f,0,-297),new Vector3(-324,0,-259.2f),new Vector3(-248.4f,0,-156.6f),
            new Vector3(-216,0,-64.8f),new Vector3(-151.2f,0,-10.8f),new Vector3(-75.6f,0,0)};
        static readonly float[] ionHeights={60,60,60,60,64,73,86,102,116,129,136,140,141,138,128,113,94,75,57,42,31,22,17,20,31,46,60,68,64,52,42,43,49,55,60};
        static readonly Vector3[] ionBanks={new Vector3(10.2f,13.5f,5),new Vector3(26.3f,29.2f,4)};
        // Arctic loop: rolling ridges, a frozen basin, a forge and an elevated return.
        static readonly Vector3[] auroraKnots={
            new Vector3(0,0,0),new Vector3(150,0,0),new Vector3(300,0,0),new Vector3(450,0,0),
            new Vector3(570,0,35),new Vector3(660,0,120),new Vector3(700,0,245),new Vector3(680,0,375),
            new Vector3(720,0,500),new Vector3(700,0,635),new Vector3(630,0,735),new Vector3(530,0,790),
            new Vector3(390,0,810),new Vector3(260,0,780),new Vector3(140,0,825),new Vector3(15,0,870),
            new Vector3(-120,0,845),new Vector3(-230,0,775),new Vector3(-280,0,655),new Vector3(-260,0,525),
            new Vector3(-315,0,415),new Vector3(-430,0,380),new Vector3(-540,0,405),new Vector3(-640,0,360),
            new Vector3(-685,0,255),new Vector3(-665,0,145),new Vector3(-720,0,35),new Vector3(-690,0,-100),
            new Vector3(-605,0,-190),new Vector3(-480,0,-220),new Vector3(-350,0,-200),new Vector3(-250,0,-130),
            new Vector3(-190,0,-55),new Vector3(-120,0,-10),new Vector3(-60,0,0)};
        static readonly float[] auroraHeights={32,32,32,32,36,43,53,65,77,88,96,100,96,87,74,61,48,37,27,22,25,32,40,50,58,63,57,46,37,30,25,24,26,29,32};
        static readonly Vector3[] auroraBanks={new Vector3(8.8f,11.6f,-5),new Vector3(24.3f,27.6f,4)};
        // Coastal spaceport: rolling waterfront roads, hangars and launch infrastructure.
        static readonly Vector3[] starfallKnots={
            new Vector3(0,0,0),new Vector3(155,0,0),new Vector3(310,0,0),new Vector3(465,0,0),
            new Vector3(610,0,25),new Vector3(725,0,95),new Vector3(780,0,205),new Vector3(770,0,335),
            new Vector3(805,0,465),new Vector3(780,0,590),new Vector3(710,0,690),new Vector3(600,0,760),
            new Vector3(460,0,785),new Vector3(320,0,755),new Vector3(180,0,780),new Vector3(45,0,835),
            new Vector3(-90,0,820),new Vector3(-215,0,755),new Vector3(-270,0,650),new Vector3(-255,0,535),
            new Vector3(-315,0,435),new Vector3(-430,0,395),new Vector3(-550,0,410),new Vector3(-650,0,365),
            new Vector3(-700,0,265),new Vector3(-690,0,155),new Vector3(-740,0,35),new Vector3(-710,0,-95),
            new Vector3(-620,0,-195),new Vector3(-485,0,-235),new Vector3(-350,0,-220),new Vector3(-245,0,-145),
            new Vector3(-185,0,-65),new Vector3(-115,0,-12),new Vector3(-55,0,0)};
        static readonly float[] starfallHeights={25,25,25,25,28,34,43,53,64,72,78,80,76,68,58,49,40,33,27,23,25,31,39,47,52,56,52,45,37,29,24,22,23,24,25};
        static readonly Vector3[] starfallBanks={new Vector3(8.8f,11.7f,-5),new Vector3(25,28,4)};
        static readonly Vector3[] points=new Vector3[Samples+1];
        static readonly float[] sampleHeights=new float[Samples+1];
        static readonly float[] distances=new float[Samples+1];
        static Vector3[] knots;
        static float[] heights;
        static Vector3[] banks;
        static PolygonCircuitLayout activeLayout;
        static bool initialized;

        public static int KnotCount=>knots.Length;
        public static int SampleCount=>Samples;
        public static float RoadHalfWidth { get; private set; }
        public static float Length { get; private set; }
        public static PolygonCircuitLayout Layout=>activeLayout;
        public static string LayoutName=>activeLayout==PolygonCircuitLayout.StarfallPort?"Starfall Port":activeLayout==PolygonCircuitLayout.AuroraForge?"Aurora Forge":activeLayout==PolygonCircuitLayout.IonRift?"Ion Rift":activeLayout==PolygonCircuitLayout.NeonMeridian?"Neon Meridian":activeLayout==PolygonCircuitLayout.CoastalExpressway?"Coastal Expressway":activeLayout==PolygonCircuitLayout.Downtown?"Downtown Street Circuit":activeLayout==PolygonCircuitLayout.Riviera?"Riviera Harbour":activeLayout==PolygonCircuitLayout.Sakura?"Sakura Hills":activeLayout==PolygonCircuitLayout.Nordwald?"Nordwald Ring":"Forest Circuit";
        public static string LayoutId=>activeLayout==PolygonCircuitLayout.StarfallPort?"StarfallPortV01":activeLayout==PolygonCircuitLayout.AuroraForge?"AuroraForgeV01":activeLayout==PolygonCircuitLayout.IonRift?"IonRiftV01":activeLayout==PolygonCircuitLayout.NeonMeridian?"NeonMeridianV01":activeLayout==PolygonCircuitLayout.CoastalExpressway?"CoastalExpresswayV01":activeLayout==PolygonCircuitLayout.Downtown?"DowntownV01":activeLayout==PolygonCircuitLayout.Riviera?"RivieraV01":activeLayout==PolygonCircuitLayout.Sakura?"SakuraV01":activeLayout==PolygonCircuitLayout.Nordwald?"NordwaldV01":"ForestCircuitV12";
        // Distance from the road edge to the barrier line the driver is held inside.
        public static float BarrierMargin=>activeLayout==PolygonCircuitLayout.StarfallPort?2f:activeLayout==PolygonCircuitLayout.AuroraForge?2f:activeLayout==PolygonCircuitLayout.IonRift?2f:activeLayout==PolygonCircuitLayout.NeonMeridian?2f:activeLayout==PolygonCircuitLayout.Riviera?1.3f:activeLayout==PolygonCircuitLayout.Nordwald?2.5f:6.5f;
        public static bool HasElevation=>heights!=null;
        // The route crosses itself (on a bridge), so projections must also compare heights.
        public static bool HasCrossover=>activeLayout==PolygonCircuitLayout.Sakura;

        static PolygonCircuit()
        {
            SetLayout(PolygonCircuitLayout.Forest);
        }

        public static void SetLayout(PolygonCircuitLayout layout)
        {
            if(initialized&&activeLayout==layout)return;
            activeLayout=layout;
            knots=layout==PolygonCircuitLayout.StarfallPort?starfallKnots:layout==PolygonCircuitLayout.AuroraForge?auroraKnots:layout==PolygonCircuitLayout.IonRift?ionKnots:layout==PolygonCircuitLayout.NeonMeridian?neonKnots:layout==PolygonCircuitLayout.CoastalExpressway?coastalKnots:layout==PolygonCircuitLayout.Downtown?downtownKnots:layout==PolygonCircuitLayout.Riviera?rivieraKnots:layout==PolygonCircuitLayout.Sakura?sakuraKnots:layout==PolygonCircuitLayout.Nordwald?nordwaldKnots:forestKnots;
            heights=layout==PolygonCircuitLayout.StarfallPort?starfallHeights:layout==PolygonCircuitLayout.AuroraForge?auroraHeights:layout==PolygonCircuitLayout.IonRift?ionHeights:layout==PolygonCircuitLayout.NeonMeridian?neonHeights:layout==PolygonCircuitLayout.Riviera?rivieraHeights:layout==PolygonCircuitLayout.Sakura?sakuraHeights:layout==PolygonCircuitLayout.Nordwald?nordwaldHeights:null;
            banks=layout==PolygonCircuitLayout.StarfallPort?starfallBanks:layout==PolygonCircuitLayout.AuroraForge?auroraBanks:layout==PolygonCircuitLayout.IonRift?ionBanks:layout==PolygonCircuitLayout.NeonMeridian?neonBanks:layout==PolygonCircuitLayout.Nordwald?nordwaldBanks:null;
            RoadHalfWidth=layout==PolygonCircuitLayout.StarfallPort?10f:layout==PolygonCircuitLayout.AuroraForge?10f:layout==PolygonCircuitLayout.IonRift?10f:layout==PolygonCircuitLayout.NeonMeridian?10f:layout==PolygonCircuitLayout.CoastalExpressway?11f:layout==PolygonCircuitLayout.Downtown?10f:layout==PolygonCircuitLayout.Riviera?7.5f:layout==PolygonCircuitLayout.Nordwald?7f:9f;
            for(int i=0;i<=Samples;i++)
            {
                points[i]=Evaluate(i*Mathf.PI*2/Samples);sampleHeights[i]=ElevationAtAngle(i*Mathf.PI*2/Samples);
                if(i>0)distances[i]=distances[i-1]+Vector3.Distance(points[i-1],points[i]);
            }
            Length=distances[Samples];
            initialized=true;
        }

        public static Vector3 Evaluate(float angle)
        {
            float u=Mathf.Repeat(angle/(Mathf.PI*2),1)*knots.Length;
            int i=Mathf.FloorToInt(u);float t=u-i,t2=t*t,t3=t2*t;
            var p0=knots[(i+knots.Length-1)%knots.Length];var p1=knots[i%knots.Length];
            var p2=knots[(i+1)%knots.Length];var p3=knots[(i+2)%knots.Length];
            return (p0*(1-3*t+3*t2-t3)+p1*(3*t3-6*t2+4)+p2*(-3*t3+3*t2+3*t+1)+p3*t3)/6;
        }

        // Road elevation (metres) at a race distance; zero on flat layouts.
        public static float Elevation(float meters)=>heights==null?0:ElevationAtAngle(AngleAtDistance(meters));
        static float ElevationAtAngle(float angle)
        {
            if(heights==null)return 0;
            float u=Mathf.Repeat(angle/(Mathf.PI*2),1)*heights.Length;
            int i=Mathf.FloorToInt(u);float t=u-i,t2=t*t,t3=t2*t;int n=heights.Length;
            float p0=heights[(i+n-1)%n],p1=heights[i%n],p2=heights[(i+1)%n],p3=heights[(i+2)%n];
            return (p0*(1-3*t+3*t2-t3)+p1*(3*t3-6*t2+4)+p2*(-3*t3+3*t2+3*t+1)+p3*t3)/6;
        }

        // Cross-fall in degrees at a race distance (positive: right-hand side higher). Ramps in and
        // out over 0.8 knot at each end of a banked section; zero on layouts without banking.
        public static float BankDegrees(float meters)
        {
            if(banks==null)return 0;
            float u=Mathf.Repeat(AngleAtDistance(meters)/(Mathf.PI*2),1)*knots.Length,total=0;
            foreach(var b in banks)
            {
                float w=Mathf.Min(Mathf.SmoothStep(0,1,(u-b.x)/.8f),Mathf.SmoothStep(0,1,(b.y-u)/.8f));
                if(w>0)total+=b.z*w;
            }
            return total;
        }
        public static float BankSlope(float meters)=>Mathf.Tan(BankDegrees(meters)*Mathf.Deg2Rad);

        public static Vector3 Tangent(float angle)=>(Evaluate(angle+.0005f)-Evaluate(angle-.0005f)).normalized;

        public static float AngleAtDistance(float meters)
        {
            meters=Mathf.Repeat(meters,Length);int lo=0,hi=Samples;
            while(hi-lo>1){int m=(lo+hi)/2;if(distances[m]<=meters)lo=m;else hi=m;}
            return (lo+Mathf.InverseLerp(distances[lo],distances[hi],meters))*Mathf.PI*2/Samples;
        }

        public static float DistanceAtKnot(float knot)
        {
            float sample=Mathf.Repeat(knot/knots.Length,1)*Samples;
            int index=Mathf.FloorToInt(sample);
            return Mathf.Lerp(distances[index],distances[index+1],sample-index);
        }

        public static Vector3 AtDistance(float meters)=>Evaluate(AngleAtDistance(meters));

        public static float Curvature(float meters)
        {
            var a=Tangent(AngleAtDistance(meters-2));var b=Tangent(AngleAtDistance(meters+2));
            return Vector3.SignedAngle(a,b,Vector3.up)*Mathf.Deg2Rad/4;
        }

        // Closest segment projection returns arc-length fraction, rather than spline angle.
        // Offset is always the horizontal distance. On a crossover layout the point's height also
        // counts (3 m sideways per metre of height), so a car on the bridge never snaps to the road
        // underneath and the reverse; pass a real world height there.
        public static float ProjectLap(Vector3 p,out float offset)=>Project(p,HasCrossover,out offset);
        // Purely horizontal projection (nearest road in plan view), for scenery placement.
        public static float ProjectLapFlat(Vector3 p,out float offset)=>Project(p,false,out offset);
        static float Project(Vector3 p,bool useHeight,out float offset)
        {
            float y=p.y;p.y=0;float best=float.PositiveInfinity,bestFlat=0,bestDistance=0;
            for(int i=0;i<Samples;i++)
            {
                var ab=points[i+1]-points[i];float t=Mathf.Clamp01(Vector3.Dot(p-points[i],ab)/Mathf.Max(ab.sqrMagnitude,.000001f));
                float flat=(p-(points[i]+ab*t)).sqrMagnitude,score=flat;
                if(useHeight){float dy=(y-Mathf.Lerp(sampleHeights[i],sampleHeights[i+1],t))*3;score+=dy*dy;}
                if(score<best){best=score;bestFlat=flat;bestDistance=Mathf.Lerp(distances[i],distances[i+1],t);}
            }
            offset=Mathf.Sqrt(bestFlat);return Mathf.Repeat(bestDistance/Length,1);
        }
    }
}
