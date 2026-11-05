using ProjectL.Systems.DataBaseSystem.Data.View.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>체스 말 표시 이동에 사용하는 높이와 구간 시간입니다.</summary>
    internal sealed class ChessPieceMotionSettings
    {
        internal float LiftHeight { get; }
        internal float LiftSeconds { get; }
        internal float TravelSeconds { get; }
        internal float LowerSeconds { get; }
        internal float TotalSeconds { get; }

        internal ChessPieceMotionSettings(float liftHeight, float liftSeconds, float travelSeconds, float lowerSeconds)
        {
            float totalSeconds = ChessPieceMotionRules.Validate(liftHeight, liftSeconds, travelSeconds, lowerSeconds);

            LiftHeight = liftHeight;
            LiftSeconds = liftSeconds;
            TravelSeconds = travelSeconds;
            LowerSeconds = lowerSeconds;
            TotalSeconds = totalSeconds;
        }

    }
}
