using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 캐릭터 사건이 피격으로 보이는지와 그 피격의 잃은 체력·사실 반영 시점, 체력만 바꾸는 사건인지의 원본입니다(재구성 연출 설계 RE1·§9.2).
    /// 캐릭터 처리기의 피격 갈래, 피해 숫자(<see cref="CombatVolleyDamageNumbers"/>), 표시 위치 보존(<see cref="CombatShownFacts"/>)이 이것만 부릅니다.
    /// </summary>
    /// <remarks>
    /// 적 전용 재구성의 치명타는 Core가 체력 사건 대신 <see cref="CombatEventKind.Reconstructed"/>를 냅니다. 그 사건의 사건 뒤 사실은
    /// 체력 최대·재구성 중 상태라(<c>ReconstructionTraitRule.Trigger</c>), 사건 전·뒤 체력 차로는 잃은 양을 셀 수 없고, 사건 뒤 사실을
    /// 반응 전에 반영하면 맞는 순간 체력 바가 차오릅니다. 그래서 잃은 양은 사건 전 현재 체력(치명이라 전부)이고, 사실은 피격 반응이 끝난 뒤 반영합니다.
    /// </remarks>
    internal static class CombatCharacterHits
    {
        /// <summary>
        /// 피격 반응·피격 큐로 보이는 사건인지입니다. 산 채로 체력이 준 체력 사건, 또는 재구성 사건입니다.
        /// 재구성 사건의 사건 전 사실이 살아 있고 사건 뒤 체력이 최대인 것은 Core 사건 생성자가 보장합니다(<c>CombatCharacterEvent</c> 생성자 검사).
        /// </summary>
        internal static bool IsHitLike(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.Kind == CombatEventKind.Reconstructed) return true;
            return e.Kind == CombatEventKind.HealthChanged && !e.Expected.Health.IsDead && !e.Final.Health.IsDead &&
                e.Final.Health.CurrentHealth < e.Expected.Health.CurrentHealth;
        }

        /// <summary>
        /// 밀림 걸음으로 바로 이어지는 피격으로 셀 사건인지입니다(넉백 피격 연출, 임시 — <see cref="CombatKnockbackLeadIns"/>). 피격으로 보이는 사건 중
        /// 재구성만 뺍니다. 재구성은 사실을 반응 뒤에 반영해야 해서(<see cref="AppliesFactsAfterReaction"/>) 밀림과 함께 가는 피격 길(사실을 먼저 반영하고 바로 완료)로 보내지 않습니다.
        /// </summary>
        internal static bool IsPushLeadInHit(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return !AppliesFactsAfterReaction(e) && IsHitLike(e);
        }

        /// <summary>
        /// 앞 피격(다음 피격이 끊어도 되는 실행 — 연발 앞 탄의 접촉 피격)을 끊고 새로 피격·사망을 여는 사건인지입니다. 피격으로 보이는 사건 또는 사망 사건입니다
        /// (2026-11-17 사용자 "A 끊고 다시"). 조각 실행 재생기의 막기 판정과 캐릭터 처리기의 끊기가 이것 하나를 부릅니다.
        /// </summary>
        internal static bool RestartsHit(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return e.Kind == CombatEventKind.Died || IsHitLike(e);
        }

        /// <summary>
        /// 체력(과 그에 딸린 상태)만 바꾸고 자리는 바꾸지 않는 사건인지입니다. 표시 위치를 보존하는 규칙(<see cref="CombatShownFacts.ForPresentation"/>)이
        /// 이 사건의 사실을 보인 자리에 맞춰 반영할 때 읽습니다(묶음 끝 검토 L1 — 재구성도 자리를 바꾸지 않음).
        /// </summary>
        internal static bool ChangesHealthOnly(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return e.Kind == CombatEventKind.HealthChanged || e.Kind == CombatEventKind.HealthNormalized ||
                e.Kind == CombatEventKind.Died || e.Kind == CombatEventKind.Reconstructed;
        }

        /// <summary>
        /// 이 사건으로 잃은 체력입니다. 재구성이면 사건 전 현재 체력, 체력·사망 사건이면 사건 전 − 사건 뒤(0 이상), 그 밖은 0입니다.
        /// </summary>
        internal static int HealthLoss(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return e.Kind switch
            {
                CombatEventKind.Reconstructed => e.Expected.Health.CurrentHealth,
                CombatEventKind.HealthChanged or CombatEventKind.Died =>
                    Math.Max(0, e.Expected.Health.CurrentHealth - e.Final.Health.CurrentHealth),
                _ => 0,
            };
        }

        /// <summary>사건 뒤 사실을 피격 반응이 끝난 뒤 반영하는지입니다. 재구성만 그렇습니다(설계 결정 ②).</summary>
        internal static bool AppliesFactsAfterReaction(CombatCharacterEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return e.Kind == CombatEventKind.Reconstructed;
        }
    }
}
