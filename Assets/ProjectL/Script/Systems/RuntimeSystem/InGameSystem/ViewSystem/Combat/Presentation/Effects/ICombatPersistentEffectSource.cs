using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 오래 보이는 이펙트의 출처 하나입니다(이펙트 끝 규칙 설계 EL4, 계획 TP-E23). 지금 보인 사실에서 "지금 보여야 할 것"을 채우기만 하고,
    /// 내기·거두기는 유지 표시 부품(<see cref="CombatPersistentEffects"/>)이 합니다. 새 조건 = 출처 클래스 하나입니다.
    /// </summary>
    internal interface ICombatPersistentEffectSource
    {
        /// <summary>
        /// 보인 사실 <paramref name="shown"/>에서 지금 보여야 할 유지 이펙트를 <paramref name="wants"/>에 더합니다. 목록을 비우지 않습니다.
        /// 같은 열쇠를 두 번 더하면 부품이 처음 것만 씁니다.
        /// </summary>
        void Collect(IReadOnlyList<CharacterCombatFacts> shown, List<CombatPersistentEffectWant> wants);
    }

    /// <summary>
    /// 유지 이펙트 하나의 열쇠입니다: (몸 유닛, 출처). 출처는 상태 키(상태 외형 유지 이펙트) 또는 (특성, 보유 유닛, 항목 번호)(특성 상시 목록)입니다(설계 EL4).
    /// </summary>
    internal readonly struct CombatPersistentEffectKey : IEquatable<CombatPersistentEffectKey>
    {
        private CombatPersistentEffectKey(EntityId body, StatusEffectKey status, TraitKey trait, EntityId owner, int index)
        {
            Body = body;
            Status = status;
            Trait = trait;
            Owner = owner;
            Index = index;
        }

        /// <summary>이펙트가 붙는 몸의 유닛입니다.</summary>
        internal EntityId Body { get; }
        internal StatusEffectKey Status { get; }
        internal TraitKey Trait { get; }
        internal EntityId Owner { get; }
        internal int Index { get; }

        /// <summary>그 유닛에 보이는 상태 하나의 유지 이펙트 열쇠입니다. 같은 상태가 출처 여럿으로 걸려도 하나입니다(HUD 아이콘과 같음).</summary>
        internal static CombatPersistentEffectKey OfStatus(EntityId body, StatusEffectKey status) => new(body, status, default, default, -1);

        /// <summary>특성 상시 목록 항목 하나의 열쇠입니다. 보유 유닛과 항목 번호로 나눕니다.</summary>
        internal static CombatPersistentEffectKey OfTrait(EntityId body, TraitKey trait, EntityId owner, int index) =>
            new(body, StatusEffectKey.None, trait, owner, index);

        public bool Equals(CombatPersistentEffectKey other) =>
            Body == other.Body && Status == other.Status && Trait == other.Trait && Owner == other.Owner && Index == other.Index;

        public override bool Equals(object obj) => obj is CombatPersistentEffectKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Body, Status, Trait, Owner, Index);
    }

    /// <summary>지금 보여야 할 유지 이펙트 하나입니다: 열쇠, 이펙트 값, 이 이펙트를 거두고 다시 내는 발동 종류(없으면 빈 목록).</summary>
    internal readonly struct CombatPersistentEffectWant
    {
        internal CombatPersistentEffectWant(CombatPersistentEffectKey key, CombatHeldEffect effect, IReadOnlyList<TraitActivationKind> restartOn = null)
        {
            Key = key;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            RestartOn = restartOn ?? Array.Empty<TraitActivationKind>();
        }

        internal CombatPersistentEffectKey Key { get; }
        internal CombatHeldEffect Effect { get; }
        internal IReadOnlyList<TraitActivationKind> RestartOn { get; }
    }
}
