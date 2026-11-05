using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 투사체 소리 값입니다. 공격 값에서 만들고 원거리 공격 조립이 큐 붙이기에 넘깁니다.
    /// 클립은 각 탄의 발사·접촉에 비행·착탄 지연을 더하고, 코드 모션은 기존 Cue·Hit 시점을 씁니다.
    /// 여러 탄의 피해 후속을 여는 집계 Hit과 개별 착탄 소리의 Contact는 별개입니다.
    /// </summary>
    /// <remarks>
    /// 설정에서 꺼낸 연출 값 묶음이라 <c>Playback/Support/</c>에 둡니다(폴더 배치 안내 §2, 투사체·피격 소리 묶음 끝 검토 낮음 1).
    /// 공격 값의 칸 넷을 그대로 옮긴 값이므로, 범용 연출 구조 검토 M3(공격 값을 부품 값 묶음으로)를 할 때 DB 쪽 투사체 소리 값으로 바꾸며 걷습니다.
    /// </remarks>
    internal sealed class CombatProjectileSounds
    {
        private CombatProjectileSounds(int impactPreset, float impactDelay, int flightPreset, float flightDelay)
        {
            ImpactPreset = impactPreset;
            ImpactDelay = impactDelay;
            FlightPreset = flightPreset;
            FlightDelay = flightDelay;
        }

        internal int ImpactPreset { get; }
        internal float ImpactDelay { get; }
        internal int FlightPreset { get; }
        internal float FlightDelay { get; }

        /// <summary>
        /// 공격 값의 투사체 소리입니다. 값 조건은 공격 값 객체가 이미 봤습니다. 소리를 낼지(프리셋 -1·슬롯 연결 대기)는 큐 붙이기의 한 곳이 거릅니다
        /// (묶음 끝 검토 낮음 2 — 여기서 따로 null을 돌려주면 같은 판단이 두 곳이 됨). 근접은 프리셋이 -1이라 항목이 생기지 않습니다.
        /// </summary>
        internal static CombatProjectileSounds From(CombatAttackPresentationData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatProjectileSounds(settings.ImpactSoundPreset, settings.ImpactSoundDelay, settings.FlightSoundPreset,
                settings.FlightSoundDelay);
        }
    }
}
