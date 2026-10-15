using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatEventKind
    {
        Moved = 1, Placed, Returned, Drawn, HealthChanged, Died, Attacked,
        EffectsActivated, ArtifactActivated, SoulsCollected, SoulFieldSettled, CombatEntered,
        TurnStarted, TurnEnded, NightEnded, GimmickActivated, StageEnded, HealthNormalized,
        MovementSourceChanged, ThornConsumed, ObstacleReacted, HeldEnvironmentChanged, ObjectActivated, ObjectCreated,
        Relocated, TornadoActivated
    }

    /// <summary>실제 계산 순서대로 보관할 불변 사실이다. 실행 콜백이나 이벤트 버스가 아니다.</summary>
    public abstract class CombatEvent
    {
        public CombatEventKind Kind { get; }
        private protected CombatEvent(CombatEventKind kind) { Kind = kind; }
    }

    public sealed class CombatObjectCreatedEvent : CombatEvent
    {
        public ObjectSourceFact Source { get; }
        public CombatObjectCreatedEvent(ObjectSourceFact source) : base(CombatEventKind.ObjectCreated)
            => Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public sealed class CombatTornadoEvent : CombatEvent
    {
        public TornadoSourceFact Expected { get; }
        public TornadoSourceFact Final { get; }
        public CombatTornadoEvent(TornadoSourceFact expected, TornadoSourceFact final) : base(CombatEventKind.TornadoActivated)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.Id != final.Id) throw new ArgumentException("Tornado 사건의 식별자가 다릅니다.");
        }
    }

    public sealed class CombatCharacterEvent : CombatEvent
    {
        public CharacterCombatFacts Expected { get; }
        public CharacterCombatFacts Final { get; }
        public IReadOnlyList<CombatDamageOrigin> DamageOrigins { get; }
        public long? TornadoSourceId { get; }
        public CombatCharacterEvent(CombatEventKind kind, CharacterCombatFacts expected, CharacterCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null, long? tornadoSourceId = null) : base(kind)
        {
            if (kind != CombatEventKind.Moved && kind != CombatEventKind.Placed && kind != CombatEventKind.Returned &&
                kind != CombatEventKind.Drawn && kind != CombatEventKind.HealthChanged && kind != CombatEventKind.Died &&
                kind != CombatEventKind.HealthNormalized && kind != CombatEventKind.Relocated)
                throw new ArgumentOutOfRangeException(nameof(kind));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (kind == CombatEventKind.Drawn ? expected != null : expected == null || expected.EntityId != final.EntityId)
                throw new ArgumentException("캐릭터 사건의 원본·최종 식별자가 잘못되었습니다.");
            Expected = expected;
            if (kind == CombatEventKind.Relocated ? !tornadoSourceId.HasValue || tornadoSourceId.Value < 1 : tornadoSourceId.HasValue)
                throw new ArgumentException("Tornado 이동 사건에는 원인 원본 식별자가 필요합니다.");
            TornadoSourceId = tornadoSourceId;
            DamageOrigins = CopyOrigins(damageOrigins);
        }

        internal static IReadOnlyList<CombatDamageOrigin> CopyOrigins(IReadOnlyList<CombatDamageOrigin> origins)
        {
            var copy = (origins ?? Array.Empty<CombatDamageOrigin>()).ToArray();
            if (copy.Any(o => o == null)) throw new ArgumentException("피해 발생 근거에 null을 넣을 수 없습니다.");
            return Array.AsReadOnly(copy);
        }
    }

    public sealed class CombatStructureEvent : CombatEvent
    {
        public StructureCombatFacts Expected { get; }
        public StructureCombatFacts Final { get; }
        public IReadOnlyList<CombatDamageOrigin> DamageOrigins { get; }
        public CombatStructureEvent(StructureCombatFacts expected, StructureCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null)
            : base(final?.Health.IsDead == true ? CombatEventKind.Died : CombatEventKind.HealthChanged)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.EntityId != final.EntityId) throw new ArgumentException("구조물 식별자가 다릅니다.");
            DamageOrigins = CombatCharacterEvent.CopyOrigins(damageOrigins);
        }
    }

    public sealed class CombatObjectActivationEvent : CombatEvent
    {
        public ObjectSourceFact Source { get; }
        public EntityId ActivatorId { get; }
        public CombatObjectActivationEvent(ObjectSourceFact source, EntityId activator) : base(CombatEventKind.ObjectActivated)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!source.Active || activator.IsEmpty) throw new ArgumentException("활성 Object와 도착자 식별자가 필요합니다.");
            ActivatorId = activator;
        }
    }

    public sealed class CombatHeldEnvironmentEvent : CombatEvent
    {
        public EntityId CharacterId { get; }
        public CharacterHeldEnvironmentState Expected { get; }
        public CharacterHeldEnvironmentState Final { get; }
        public CombatDamageOrigin Origin { get; }

        public CombatHeldEnvironmentEvent(CharacterCombatFacts expected, CharacterCombatFacts final,
            CombatDamageOrigin origin = null) : base(CombatEventKind.HeldEnvironmentChanged)
        {
            if (expected == null || final == null || expected.EntityId != final.EntityId ||
                expected.HeldEnvironment.SameValues(final.HeldEnvironment))
                throw new ArgumentException("실제 보유 환경 상태 변경과 동일한 캐릭터 식별자가 필요합니다.");
            CharacterId = final.EntityId; Expected = expected.HeldEnvironment;
            Final = final.HeldEnvironment; Origin = origin;
        }
    }

    public sealed class CombatObstacleReactionEvent : CombatEvent
    {
        public EntityId ObstacleId { get; }
        public ObstacleContentKind ContentKind { get; }
        public ObstacleReactionKind Reaction { get; }
        public CombatDamageOrigin Origin { get; }
        public long AttackGroup { get; }
        public CombatObstacleReactionEvent(EntityId obstacleId, ObstacleContentKind contentKind,
            ObstacleReactionKind reaction, CombatDamageOrigin origin, long attackGroup) : base(CombatEventKind.ObstacleReacted)
        {
            if (obstacleId.IsEmpty || !Enum.IsDefined(typeof(ObstacleContentKind), contentKind) ||
                reaction == ObstacleReactionKind.None || !Enum.IsDefined(typeof(ObstacleReactionKind), reaction) || attackGroup < 0)
                throw new ArgumentException("유효한 장애물 반응 사실이 필요합니다.");
            ObstacleId = obstacleId; ContentKind = contentKind; Reaction = reaction;
            Origin = origin ?? throw new ArgumentNullException(nameof(origin)); AttackGroup = attackGroup;
        }
    }

    public sealed class CombatMovementSourceEvent : CombatEvent
    {
        public MovementTileFact Expected { get; }
        public MovementTileFact Final { get; }
        public CombatMovementSourceEvent(MovementTileFact expected, MovementTileFact final)
            : base(CombatEventKind.MovementSourceChanged) { Expected = expected; Final = final; }
    }

    public sealed class CombatThornConsumedEvent : CombatEvent
    {
        public ThornFieldCellSnapshot Source { get; }
        public CombatThornConsumedEvent(ThornFieldCellSnapshot source) : base(CombatEventKind.ThornConsumed) { Source = source; }
    }

    public sealed class CombatAttackEvent : CombatEvent
    {
        public AttackResult Attack { get; }
        public CombatAttackEvent(AttackResult attack) : base(CombatEventKind.Attacked) { Attack = attack; }
    }

    public sealed class CombatEffectsEvent : CombatEvent
    {
        public IReadOnlyList<TraitActivationSnapshot> Traits { get; }
        public IReadOnlyList<CategoryActivationSnapshot> Categories { get; }
        public CombatEffectsEvent(IReadOnlyList<TraitActivationSnapshot> traits, IReadOnlyList<CategoryActivationSnapshot> categories)
            : base(CombatEventKind.EffectsActivated)
        { Traits = Array.AsReadOnly(traits.ToArray()); Categories = Array.AsReadOnly(categories.ToArray()); }
    }

    public sealed class CombatSoulEvent : CombatEvent
    {
        public EntityId CollectorId { get; }
        public SoulFieldChange Field { get; }
        public CharacterChanges Characters { get; }
        public IReadOnlyList<long> ConsumedIds { get; }
        public CombatSoulEvent(SoulFieldChange field, EntityId collectorId = default, SoulActionChanges collection = null)
            : base(collection == null ? CombatEventKind.SoulFieldSettled : CombatEventKind.SoulsCollected)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field)); CollectorId = collectorId;
            Characters = collection?.Characters;
            ConsumedIds = collection?.ConsumedSoulIds ?? Array.Empty<long>();
        }
    }

    public sealed class CombatArtifactEvent : CombatEvent
    {
        public IReadOnlyList<ArtifactActivationSnapshot> Activations { get; }
        public CombatArtifactEvent(IReadOnlyList<ArtifactActivationSnapshot> activations) : base(CombatEventKind.ArtifactActivated)
        { Activations = Array.AsReadOnly(activations.ToArray()); }
    }

    public sealed class CombatTurnEvent : CombatEvent
    {
        public EntityId ActorId { get; }
        public CombatTurnFacts Expected { get; }
        public CombatTurnFacts Final { get; }
        public CombatTurnEvent(CombatEventKind kind, EntityId actor, CombatTurnFacts expected, CombatTurnFacts final) : base(kind)
        {
            if (kind != CombatEventKind.CombatEntered && kind != CombatEventKind.TurnStarted &&
                kind != CombatEventKind.TurnEnded && kind != CombatEventKind.NightEnded) throw new ArgumentOutOfRangeException(nameof(kind));
            ActorId = actor; Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
        }
    }

    public sealed class CombatGimmickEvent : CombatEvent
    {
        public StageGimmickActivationSnapshot Activation { get; }
        public CombatGimmickEvent(StageGimmickActivationSnapshot activation) : base(CombatEventKind.GimmickActivated)
        { Activation = activation ?? throw new ArgumentNullException(nameof(activation)); }
    }

    public sealed class CombatOutcomeEvent : CombatEvent
    {
        public StageOutcomeSnapshot Outcome { get; }
        public CombatOutcomeEvent(StageOutcomeSnapshot outcome) : base(CombatEventKind.StageEnded)
        { if (!outcome.IsStageEnded) throw new ArgumentException("종료된 승패가 필요합니다."); Outcome = outcome; }
    }
}
