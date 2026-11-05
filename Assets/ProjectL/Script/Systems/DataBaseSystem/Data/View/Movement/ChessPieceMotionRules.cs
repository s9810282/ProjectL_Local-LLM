using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Movement
{
    /// <summary>
    /// 체스 말 들기 이동 설정(<c>ChessPieceMotionSettings</c>)의 값 조건입니다. 지금은 StageSelect 노드 말 이동이 씁니다.
    /// 로비는 칸마다 뛰는 이동이라 자기 값 조건(<c>LobbyMotionData.Validate</c>)을 씁니다.
    /// </summary>
    internal static class ChessPieceMotionRules
    {
        internal static float Validate(float liftHeight, float liftSeconds, float travelSeconds, float lowerSeconds)
        {
            if (!IsFinite(liftHeight) || liftHeight < 0f)
                throw new ArgumentOutOfRangeException(nameof(liftHeight), "들어 올리는 높이는 유한한 0 이상의 값이어야 합니다.");
            ValidateDuration(liftSeconds, nameof(liftSeconds));
            ValidateDuration(travelSeconds, nameof(travelSeconds));
            ValidateDuration(lowerSeconds, nameof(lowerSeconds));

            float totalSeconds = liftSeconds + travelSeconds + lowerSeconds;
            if (!IsFinite(totalSeconds))
                throw new ArgumentOutOfRangeException(nameof(lowerSeconds), "이동 구간 시간의 합은 유한해야 합니다.");
            return totalSeconds;
        }

        private static void ValidateDuration(float seconds, string parameterName)
        {
            if (!IsFinite(seconds) || seconds <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, "이동 구간 시간은 유한한 양수여야 합니다.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
