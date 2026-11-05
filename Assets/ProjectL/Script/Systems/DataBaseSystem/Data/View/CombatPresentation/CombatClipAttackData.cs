using System;
using System.Collections.Generic;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 칸의 값입니다(클립 설계 AC8, 평가 E6). 클립 값과 공격 칸에만 있는 피격 선딜레이를 나눠 둡니다.
    /// 선딜레이는 이벤트 타이밍 뒤 대상이 맞기까지의 초이고, 뼈대 값입니다. 소리·이펙트 시점은 공격 묶음의 큐로 옮겼습니다(범용 연출 구조 설계 §5).
    /// </summary>
    internal sealed class CombatClipAttackData
    {
        /// <exception cref="ArgumentOutOfRangeException">선딜레이가 <see cref="IsValidDelay"/>에 맞지 않을 때입니다.</exception>
        internal CombatClipAttackData(CombatClipData clip, float hitDelay, IReadOnlyList<CombatClipShot> shots = null)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            if (!IsValidDelay(hitDelay)) throw new ArgumentOutOfRangeException(nameof(hitDelay), hitDelay, DelayRule);
            HitDelay = hitDelay;
            HasExplicitShots = shots != null && shots.Count > 0;
            Shots = ReadShots(shots);
        }

        /// <summary>선딜레이 값 규칙입니다(유한한 0 이상).</summary>
        internal const string DelayRule = "선딜레이는 유한한 0 이상의 초여야 합니다.";

        internal CombatClipData Clip { get; }

        /// <summary>명시 입력인지입니다. 정규화 뒤의 한 발만으로는 기본 단발과 구분할 수 없어 입력 출처를 보관합니다.</summary>
        internal bool HasExplicitShots { get; }

        /// <summary>실행이 읽는 전체 발사 목록입니다. 기본 단발도 포함하며 내부 배열은 노출하지 않습니다.</summary>
        internal IReadOnlyList<CombatClipShot> Shots { get; }

        /// <summary>
        /// 이벤트 타이밍 뒤 대상이 맞기까지(출처 해제, 피격자의 피격 사건 시작)의 초입니다. 근접에서만 씁니다.
        /// 원거리는 이벤트 타이밍에 바로 쏘고 맞는 순간은 투사체가 정하므로 0이어야 합니다(규칙은 <see cref="CombatUnitAnimationRules"/>).
        /// </summary>
        internal float HitDelay { get; }

        /// <summary>선딜레이 규칙입니다. 이 생성자와 입력 SO가 함께 부르고, 판정은 연출 수 규칙(0 이상)을 따릅니다(범용 연출 구조 계획 P17).</summary>
        internal static bool IsValidDelay(float seconds) => CombatPresentationValueRules.IsNonNegative(seconds);

        private IReadOnlyList<CombatClipShot> ReadShots(IReadOnlyList<CombatClipShot> shots)
        {
            if (!HasExplicitShots)
                return Array.AsReadOnly(new[] { new CombatClipShot(CombatClipEventId.Event, 0) });

            var copy = new CombatClipShot[shots.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                CombatClipShot shot = shots[i];
                if (!Clip.Timings.TryGetEventTime(shot.EventId, out float time))
                    throw new ArgumentException($"발사 {i}의 이벤트 {shot.EventId}가 없습니다. 클립: {Clip.Clip.name}", nameof(shots));
                if (time >= Clip.EndTime)
                    throw new ArgumentException($"발사 {i}의 이벤트 {shot.EventId}는 끝보다 앞서야 합니다. 클립: {Clip.Clip.name}", nameof(shots));
                for (int j = 0; j < i; j++)
                {
                    if (copy[j].EventId == shot.EventId && copy[j].Point == shot.Point)
                        throw new ArgumentException($"발사 {i}가 {j}와 같은 이벤트/지점을 두 번 지정했습니다: {shot.EventId}, 지점 {shot.Point}", nameof(shots));
                }
                copy[i] = shot;
            }
            return Array.AsReadOnly(copy);
        }
    }
}
