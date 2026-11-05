using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    internal interface ICombatTraitStateEvidence
    {
        CombatTraitStateEvidenceKind Kind { get; }

        bool Matches(TraitActivationSnapshot trait, CombatTraitStateEvent state);
    }

    /// <summary>등록된 공개 사실 해석을 선택합니다. 생성 후 등록 목록은 변경할 수 없습니다.</summary>
    internal sealed class CombatTraitStateEvidenceRegistry
    {
        private readonly Dictionary<CombatTraitStateEvidenceKind, ICombatTraitStateEvidence> _readers = new();
        internal CombatTraitStateEvidenceRegistry(IEnumerable<ICombatTraitStateEvidence> readers)
        {
            if (readers == null)
                throw new ArgumentNullException(nameof(readers));
            foreach (ICombatTraitStateEvidence reader in readers)
            {
                if (reader == null || reader.Kind == CombatTraitStateEvidenceKind.None || !Enum.IsDefined(typeof(CombatTraitStateEvidenceKind), reader.Kind) || !_readers.TryAdd(reader.Kind, reader))
                    throw new ArgumentException("상태 근거 등록이 비었거나 중복·미정의입니다.", nameof(readers));
            }

            foreach (CombatTraitFeedbackProfile profile in CombatTraitFeedbackProfiles.All)
                if (profile.SupportsStateApplied && !_readers.ContainsKey(profile.Evidence))
                    throw new InvalidOperationException($"[CombatTraitFeedback] 상태 근거가 등록되지 않았습니다: {profile.Evidence}");
        }

        internal bool Matches(TraitActivationSnapshot trait, EntityId target, CombatTraitStateEvent state)
        {
            if (state.Final.EntityId != target || !SameOrigin(trait.ResultOrigin, state.Origin))
                return false;
            CombatTraitStateEvidenceKind kind = CombatTraitFeedbackProfiles.Get(trait.TraitKey, trait.TraitActivationKind).Evidence;
            return _readers.TryGetValue(kind, out var reader) && reader.Matches(trait, state);
        }

        private static bool SameOrigin(CombatDamageOrigin a, CombatDamageOrigin b) => a == null ? b == null : b != null && a.AttackGroup == b.AttackGroup && a.CreditedCharacterId == b.CreditedCharacterId && a.PhysicalCell == b.PhysicalCell;
    }

    internal sealed class CombatAccelerationEvidence : ICombatTraitStateEvidence
    {
        public CombatTraitStateEvidenceKind Kind => CombatTraitStateEvidenceKind.AccelerationHeld;

        public bool Matches(TraitActivationSnapshot trait, CombatTraitStateEvent state) => trait.Magnitude > 0 && Held(state.Final) - Held(state.Expected) == trait.Magnitude;
        private static int Held(CharacterCombatFacts facts) => facts.Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().SingleOrDefault()?.HeldAttackBonus ?? 0;
    }

    internal sealed class CombatAttackGrantEvidence : ICombatTraitStateEvidence
    {
        public CombatTraitStateEvidenceKind Kind => CombatTraitStateEvidenceKind.TraitAttackGrant;

        public bool Matches(TraitActivationSnapshot trait, CombatTraitStateEvent state) => trait.Magnitude > 0 && Granted(state.Final, trait) - Granted(state.Expected, trait) == trait.Magnitude;
        private static int Granted(CharacterCombatFacts facts, TraitActivationSnapshot trait) => facts.StatusEffects.Entries.OfType<AttackIncreaseStatus>().Where(g => CombatTraitEffectSource.IsFrom(g, trait)).Sum(g => g.Amount);
    }

    /// <summary>최대 증가율의 변화가 아닌 해당 출처의 실제 추가·갱신을 확인합니다. 발동 수치는 생산자마다 다르므로 출처 목록의 실제 변경을 봅니다.</summary>
    internal sealed class CombatWeaknessExposureEvidence : ICombatTraitStateEvidence
    {
        public CombatTraitStateEvidenceKind Kind => CombatTraitStateEvidenceKind.WeaknessExposure;
        public bool Matches(TraitActivationSnapshot trait, CombatTraitStateEvent state)
        {
            var before = state.Expected.StatusEffects.Entries.OfType<WeaknessExposureStatus>()
                .Where(s => CombatTraitEffectSource.IsFrom(s, trait)).ToArray();
            foreach (var after in state.Final.StatusEffects.Entries.OfType<WeaknessExposureStatus>())
            {
                if (CombatTraitEffectSource.IsFrom(after, trait) && !before.Any(after.SameValues))
                    return true;
            }
            return false;
        }
    }

    internal static class CombatTraitEffectSource
    {
        // 공개된 소유자·특성 식별로 대응합니다. RuleKey 문자열을 생성하거나 새 효과를 부여하지 않습니다.
        internal static bool IsFrom(AppliedStatus status, TraitActivationSnapshot trait) => status.Origin is EffectOriginKey origin &&
            origin.Kind == EffectOwnerKind.Trait && origin.OwnerInstanceId == trait.SourceUnitId.Value && origin.DefinitionKey == (int)trait.TraitKey;
    }

}
