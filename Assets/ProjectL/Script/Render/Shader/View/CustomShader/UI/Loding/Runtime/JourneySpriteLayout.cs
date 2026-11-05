using System;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>원본 좌표, 아틀라스 배치, 말 분할 경계의 유일한 원본.</summary>
    public static class JourneySpriteLayout
    {
        public const int AtlasSize = 2048;
        public const int HorseWidth = 543;
        public const int HorseHeight = 724;
        public const int HorseSourceFrame = 1;
        public const float HorseDisplayScale = 240f / HorseWidth;
        public const float WagonDisplayScale = 430f / 1536f;
        public static readonly RectInt Wagon = new RectInt(0, 0, 1536, 1024);
        public static readonly RectInt HorseBody = new RectInt(0, 1100, HorseWidth, HorseHeight);
        public static readonly RectInt HorseHind = new RectInt(550, 1100, HorseWidth, HorseHeight);
        public static readonly RectInt HorseFore = new RectInt(1100, 1100, HorseWidth, HorseHeight);
        public static Vector2 SolidUv => new Vector2(2046.5f / AtlasSize, 1f - 2046.5f / AtlasSize);

        private static readonly Vector2[] BodyPoints =
        {
            new Vector2(0, 0), new Vector2(543, 0), new Vector2(543, 440), new Vector2(421, 440),
            new Vector2(385, 408), new Vector2(351, 455), new Vector2(302, 478), new Vector2(235, 475),
            new Vector2(175, 444), new Vector2(118, 405), new Vector2(74, 420), new Vector2(0, 420)
        };
        private static readonly Vector2[] HindPoints =
        {
            new Vector2(88, 395), new Vector2(123, 400), new Vector2(179, 444), new Vector2(179, 516),
            new Vector2(198, 580), new Vector2(157, 590), new Vector2(103, 582), new Vector2(97, 528),
            new Vector2(96, 467), new Vector2(83, 442)
        };
        private static readonly Vector2[] ForePoints =
        {
            new Vector2(234, 458), new Vector2(301, 458), new Vector2(308, 545), new Vector2(317, 598),
            new Vector2(274, 611), new Vector2(219, 599), new Vector2(226, 547)
        };

        public static ReadOnlySpan<Vector2> BodyMask => BodyPoints;
        public static ReadOnlySpan<Vector2> HindMask => HindPoints;
        public static ReadOnlySpan<Vector2> ForeMask => ForePoints;

        public static Vector2 GetUv(RectInt region, Vector2 point) => new Vector2(
            (region.x + point.x) / AtlasSize,
            1f - (region.y + point.y) / AtlasSize);
    }
}
