using System;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>
    /// 로비 이동 연출 값입니다. 캐릭터는 도착 칸까지 한 칸씩 뛰고, 방향이 바뀌는 걸음 앞과 도착 뒤에 돕니다(로비 설계 §15 HM5).
    /// </summary>
    internal sealed class LobbyMotionData : IData
    {
        /// <summary>걸음 꼭대기가 정착 높이보다 올라가는 높이입니다(유한, 0 이상).</summary>
        internal float HopHeight { get; }

        /// <summary>한 칸 걸음 시간입니다(초, 유한 양수).</summary>
        internal float HopSeconds { get; }

        /// <summary>걸음 전·도착 뒤 회전 시간입니다(초, 유한, 0 이상). 0이면 바로 돕니다.</summary>
        internal float TurnSeconds { get; }

        internal LobbyMotionData(float hopHeight, float hopSeconds, float turnSeconds)
        {
            HopHeight = hopHeight;
            HopSeconds = hopSeconds;
            TurnSeconds = turnSeconds;
        }

        /// <summary>로비 이동 값 조건의 유일한 원본입니다. DB 생성 때 한 번 검사합니다.</summary>
        internal void Validate()
        {
            if (!float.IsFinite(HopHeight) || HopHeight < 0f)
                throw Invalid(nameof(HopHeight), "뛰기 높이는 유한한 0 이상의 값이어야 합니다.");
            if (!float.IsFinite(HopSeconds) || HopSeconds <= 0f)
                throw Invalid(nameof(HopSeconds), "한 칸 걸음 시간은 유한한 양수여야 합니다.");
            if (!float.IsFinite(TurnSeconds) || TurnSeconds < 0f)
                throw Invalid(nameof(TurnSeconds), "회전 시간은 유한한 0 이상의 값이어야 합니다.");
        }

        private static InvalidOperationException Invalid(string field, string message) =>
            new($"[LobbyMotionData] {field}: {message}");
    }
}
