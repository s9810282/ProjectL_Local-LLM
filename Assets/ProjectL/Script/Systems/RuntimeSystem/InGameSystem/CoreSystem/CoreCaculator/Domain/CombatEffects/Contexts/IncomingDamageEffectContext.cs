using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    /// <summary>
    /// 대상별 피격 경감 평가 입력입니다.
    /// 기존 AttackDamageEffectContext는 공격자 기준으로 남겨두고, 이 단계만 대상 정보를 갖습니다.
    /// </summary>
    internal readonly struct IncomingDamageEffectContext
    {
        public int IncomingDamage { get; }
        public CombatDamageKind Kind { get; }
        public CharacterCombatFacts Recipient { get; }
        public EntityId? DamageSourceId { get; }
        public IReadOnlyList<CharacterCombatFacts> Characters { get; }

        public IncomingDamageEffectContext(int incomingDamage)
            : this(incomingDamage, CombatDamageKind.OrdinaryAttack, null, Array.Empty<CharacterCombatFacts>())
        {
        }

        public IncomingDamageEffectContext(int incomingDamage, CombatDamageKind kind, CharacterCombatFacts recipient,
            IReadOnlyList<CharacterCombatFacts> characters, EntityId? damageSourceId = null)
        {
            if (!Enum.IsDefined(typeof(CombatDamageKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            Recipient = recipient;
            DamageSourceId = damageSourceId;
            Characters = Array.AsReadOnly((characters ?? throw new ArgumentNullException(nameof(characters))).ToArray());
            if (incomingDamage < 0) throw new ArgumentOutOfRangeException(nameof(incomingDamage));
            IncomingDamage = incomingDamage;
        }
    }
}
