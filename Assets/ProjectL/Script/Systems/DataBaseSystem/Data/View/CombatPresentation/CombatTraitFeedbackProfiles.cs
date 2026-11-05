using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    internal enum CombatTraitStateEvidenceKind
    {
        None = 0,
        AccelerationHeld = 1,
        TraitAttackGrant = 2,
        WeaknessExposure = 3
    }

    /// <summary>표현이 읽을 상태 근거와 횟수 종류입니다. 게임의 발동 규칙은 갖지 않습니다.</summary>
    internal readonly struct CombatTraitFeedbackProfile
    {
        internal CombatTraitFeedbackProfile(TraitKey trait, TraitActivationKind kind, CombatTraitStateEvidenceKind evidence, CombatBodyTextCount count = CombatBodyTextCount.None)
        {
            Trait = trait;
            Kind = kind;
            Evidence = evidence;
            Count = count;
        }

        internal TraitKey Trait { get; }
        internal TraitActivationKind Kind { get; }
        internal CombatTraitStateEvidenceKind Evidence { get; }
        internal CombatBodyTextCount Count { get; }
        internal bool SupportsStateApplied => Evidence != CombatTraitStateEvidenceKind.None;

        internal bool Allows(CombatBodyTextCount count) => count == CombatBodyTextCount.None || count == Count;
    }

    /// <summary>데이터 검증·Inspector·사건 해석에서 함께 읽는 유일한 지원 표입니다.</summary>
    internal static class CombatTraitFeedbackProfiles
    {
        private static readonly IReadOnlyList<CombatTraitFeedbackProfile> _profiles = Array.AsReadOnly(new[]
        {
            new CombatTraitFeedbackProfile(TraitKey.Acceleration, TraitActivationKind.AttackGranted,
                CombatTraitStateEvidenceKind.AccelerationHeld, CombatBodyTextCount.CompletedMovementCells),
            new CombatTraitFeedbackProfile(TraitKey.CleanSweep, TraitActivationKind.AttackGranted,
                CombatTraitStateEvidenceKind.TraitAttackGrant),
            new CombatTraitFeedbackProfile(TraitKey.ReconSquad, TraitActivationKind.StatusApplied,
                CombatTraitStateEvidenceKind.WeaknessExposure),
            new CombatTraitFeedbackProfile(TraitKey.HuntersMark, TraitActivationKind.StatusApplied,
                CombatTraitStateEvidenceKind.WeaknessExposure)
        });
        internal static IReadOnlyList<CombatTraitFeedbackProfile> All => _profiles;

        static CombatTraitFeedbackProfiles()
        {
            var keys = new HashSet<(TraitKey, TraitActivationKind)>();
            foreach (CombatTraitFeedbackProfile profile in _profiles)
                if (!keys.Add((profile.Trait, profile.Kind)))
                    throw new InvalidOperationException($"[CombatTraitFeedbackProfiles] 지원 프로필 키가 중복됐습니다: {profile.Trait}/{profile.Kind}");
        }

        internal static CombatTraitFeedbackProfile Get(TraitKey trait, TraitActivationKind kind)
        {
            foreach (CombatTraitFeedbackProfile profile in _profiles)
                if (profile.Trait == trait && profile.Kind == kind)
                    return profile;
            return default;
        }
    }
}
