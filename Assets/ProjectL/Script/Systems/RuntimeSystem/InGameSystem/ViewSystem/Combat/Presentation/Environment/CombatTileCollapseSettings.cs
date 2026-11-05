using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment
{
    /// <summary>
    /// 바닥 교체 외형이 사건 시점에 빠질 때(약한 지반 붕괴)의 떨림·가라앉기 값입니다(타일 교체 설계 TR8). 원본은 View DB 설정 표
    /// (<see cref="CombatPresentationSettingsData"/>)이며, 값 조건은 그 설정의 조건 함수 한 곳을 씁니다.
    /// </summary>
    internal sealed class CombatTileCollapseSettings
    {
        internal CombatTileCollapseSettings(float shakeSeconds, float shakeStrength, float shakeFrequency, float sinkSeconds, float sinkDepth)
        {
            CombatPresentationSettingsData.ValidateSeconds(shakeSeconds, nameof(shakeSeconds));
            CombatPresentationSettingsData.ValidateTileCollapseAmount(shakeStrength, nameof(shakeStrength));
            CombatPresentationSettingsData.ValidateTileCollapseAmount(shakeFrequency, nameof(shakeFrequency));
            CombatPresentationSettingsData.ValidateSeconds(sinkSeconds, nameof(sinkSeconds));
            CombatPresentationSettingsData.ValidateTileCollapseAmount(sinkDepth, nameof(sinkDepth));
            ShakeSeconds = shakeSeconds;
            ShakeStrength = shakeStrength;
            ShakeFrequency = shakeFrequency;
            SinkSeconds = sinkSeconds;
            SinkDepth = sinkDepth;
        }

        /// <summary>View DB 설정 표의 값으로 만듭니다.</summary>
        internal static CombatTileCollapseSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatTileCollapseSettings(settings.TileCollapseShakeSeconds, settings.TileCollapseShakeStrength,
                settings.TileCollapseShakeFrequency, settings.TileCollapseSinkSeconds, settings.TileCollapseSinkDepth);
        }

        /// <summary>떨리는 시간입니다(초). 이 동안 다음 사건 시작을 막습니다.</summary>
        internal float ShakeSeconds { get; }

        /// <summary>떨림 세기입니다(필드 가로 두 축으로 흔들리는 거리, 월드 단위).</summary>
        internal float ShakeStrength { get; }

        /// <summary>떨림 빠르기입니다(초당 왕복 수).</summary>
        internal float ShakeFrequency { get; }

        /// <summary>떨림 뒤 가라앉는 시간입니다(초).</summary>
        internal float SinkSeconds { get; }

        /// <summary>가라앉는 깊이입니다(월드 단위, 필드 위쪽 방향의 반대).</summary>
        internal float SinkDepth { get; }

        /// <summary>떨림과 가라앉기를 합친 시간입니다(초). 0이면 연출 없이 바로 끕니다.</summary>
        internal float TotalSeconds => ShakeSeconds + SinkSeconds;
    }
}
