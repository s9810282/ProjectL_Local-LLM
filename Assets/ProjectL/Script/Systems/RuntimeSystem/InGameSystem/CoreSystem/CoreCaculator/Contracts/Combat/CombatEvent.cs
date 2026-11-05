using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatEventKind
    {
        Moved = 1, Placed, Returned, HealthChanged = 5, Died, Attacked,
        EffectsActivated, ArtifactActivated, SoulsCollected, SoulFieldSettled, CombatEntered,
        NightEnded = 15, GimmickActivated, StageEnded, HealthNormalized,
        MovementSourceChanged, ThornConsumed, ObstacleReacted, HeldEnvironmentChanged, ObjectActivated, ObjectCreated,
        Relocated, TornadoActivated, Fell,
        /// <summary>전투 중 새 개체 생성이다. 다음 밤 복귀는 기존 Placed를 쓰므로 생성과 구분된다.</summary>
        Summoned,
        /// <summary>보스 포효(반응 공격)다. 결과는 transition의 Attack이 아니라 이 사건으로만 낸다.</summary>
        Roared,
        // 폐기된 추가 공격 사건의 번호 30은 재사용하지 않습니다.
        ItemActivated = 31,
        ItemEquipmentChanged = 32,
        ItemDiscarded = 33,
        /// <summary>처치 하나의 경험치가 가해 아군의 이번 전투 획득량에 쌓였다.</summary>
        ExperienceEarned = 34,
        /// <summary>승리 때 이번 전투 획득량이 보유 기록(레벨·진행 경험치)에 반영됐다.</summary>
        ExperienceApplied = 35, ControllerTurnStarted = 36, PlayerTurnEnded = 37, EnemyPhaseEnded = 38,
        // 옛 표식 기록을 읽기 위한 예약 번호입니다. 새 약점 노출은 TraitStateChanged를 사용합니다.
        HuntersMarkChanged = 39,
        Pulled = 40,
        TraitStateChanged = 41,
        RockfallAdvanced = 42,
        HaboobAdvanced = 43,
        TornadoAdvanced = 44,
        TornadoCenterReached = 45,
        TornadoEntryStarted = 46,
        TornadoMemberAdvanced = 47
    }

    /// <summary>
    /// 원래 명령 정산 뒤 보스가 포효한 사실이다. 피해·넉백 등 세부 결과는 뒤따르는 일반 사건(체력·이동 등)으로 나온다.
    /// 범위 안에 대상이 없으면 Attack은 null이다(카운터는 그래도 0으로 돌아간다).
    /// </summary>
    public sealed class CombatRoarEvent : CombatEvent
    {
        public EntityId BossId { get; }
        public AttackResult? Attack { get; }

        /// <summary>
        /// 포효 반응 공격이 덮는 칸이다(보드 안, 벽 가림 적용). 대상 유무와 관계없이 채우며, Attack이 있으면 그 영향 칸과 같다.
        /// 표시 전용 사실이며 피해 판정에는 쓰지 않는다(View 요청, 2026-10-15 사용자 지시로 View 작업에서 추가).
        /// </summary>
        public IReadOnlyList<CellCoord> AreaCells { get; }

        public CombatRoarEvent(EntityId bossId, AttackResult? attack, IReadOnlyList<CellCoord> areaCells = null)
            : base(CombatEventKind.Roared)
        {
            if (bossId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(bossId));
            BossId = bossId;
            Attack = attack;
            AreaCells = Array.AsReadOnly((areaCells ?? Array.Empty<CellCoord>()).ToArray());
        }
    }

    /// <summary>전투 중 소환으로 새 개체를 만든 사실이다. Created는 등장 효과를 받기 전의 생성 시점 사실이다.</summary>
    public sealed class CombatSummonEvent : CombatEvent
    {
        public CombatSummonRecord Record { get; }
        public CharacterCombatFacts Created { get; }

        public CombatSummonEvent(CombatSummonRecord record, CharacterCombatFacts created) : base(CombatEventKind.Summoned)
        {
            Created = created ?? throw new ArgumentNullException(nameof(created));
            if (created.EntityId != record.SummonedId)
                throw new ArgumentException("소환 사건의 기록과 생성 개체가 다릅니다.", nameof(created));
            Record = record;
        }
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
        public TornadoContactOrigin TornadoContact { get; }
        public CombatDamageCause DamageCause { get; }
        public CombatMovementCause MovementCause { get; }
        public CombatCharacterEvent(CombatEventKind kind, CharacterCombatFacts expected, CharacterCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null, long? tornadoSourceId = null, CombatDamageCause damageCause = CombatDamageCause.NumericDamage)
            : this(kind, expected, final, damageOrigins, tornadoSourceId, damageCause, null)
        {
        }

        public CombatCharacterEvent(CombatEventKind kind, CharacterCombatFacts expected, CharacterCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins, long? tornadoSourceId, CombatDamageCause damageCause,
            CombatMovementCause movementCause, TornadoContactOrigin tornadoContact = null) : base(kind)
        {
            if (kind != CombatEventKind.Moved && kind != CombatEventKind.Placed && kind != CombatEventKind.Returned &&
                kind != CombatEventKind.HealthChanged && kind != CombatEventKind.Died &&
                kind != CombatEventKind.HealthNormalized && kind != CombatEventKind.Relocated)
                throw new ArgumentOutOfRangeException(nameof(kind));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected == null || expected.EntityId != final.EntityId)
                throw new ArgumentException("캐릭터 사건의 원본·최종 식별자가 잘못되었습니다.");
            Expected = expected;
            if (kind == CombatEventKind.Relocated ? !tornadoSourceId.HasValue || tornadoSourceId.Value < 1 : tornadoSourceId.HasValue)
                throw new ArgumentException("Tornado 이동 사건에는 원인 원본 식별자가 필요합니다.");
            if (!Enum.IsDefined(typeof(CombatDamageCause), damageCause) || damageCause == CombatDamageCause.InstantDefeat &&
                (kind != CombatEventKind.Died || expected.Health.IsDead || !final.Health.IsDead))
                throw new ArgumentException("직접 처치 원인은 새 사망 사건에만 사용할 수 있습니다.", nameof(damageCause));
            DamageCause = damageCause;
            if (movementCause != null && (kind != CombatEventKind.Moved && kind != CombatEventKind.Relocated ||
                kind == CombatEventKind.Relocated && (movementCause.Agency != CombatMovementAgency.Forced || movementCause.TraitKey.HasValue)))
                throw new ArgumentException("이동 원인은 실제 이동 사건에만 지정할 수 있습니다.", nameof(movementCause));
            MovementCause = movementCause;
            TornadoSourceId = tornadoSourceId;
            if (tornadoContact != null && (kind != CombatEventKind.Relocated || tornadoSourceId != tornadoContact.SourceId))
                throw new ArgumentException("토네이도 접촉 근거는 같은 원본의 재배치 사건에만 붙일 수 있습니다.");
            TornadoContact = tornadoContact;
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
        public CombatDamageCause DamageCause { get; }
        public CombatMovementCause MovementCause { get; }
        public CombatStructureEvent(StructureCombatFacts expected, StructureCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins = null, CombatDamageCause damageCause = CombatDamageCause.NumericDamage)
            : this(expected, final, damageOrigins, damageCause, null)
        {
        }

        public CombatStructureEvent(StructureCombatFacts expected, StructureCombatFacts final,
            IReadOnlyList<CombatDamageOrigin> damageOrigins, CombatDamageCause damageCause, CombatMovementCause movementCause)
            : base(final?.Health.IsDead == true ? CombatEventKind.Died : CombatEventKind.HealthChanged)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.EntityId != final.EntityId) throw new ArgumentException("구조물 식별자가 다릅니다.");
            if (!Enum.IsDefined(typeof(CombatDamageCause), damageCause) || damageCause == CombatDamageCause.InstantDefeat &&
                (expected.Health.IsDead || !final.Health.IsDead)) throw new ArgumentException("직접 처치는 새 사망이어야 합니다.", nameof(damageCause));
            DamageCause = damageCause;
            if (movementCause != null && expected.Anchor == final.Anchor)
                throw new ArgumentException("움직이지 않은 구조물에는 이동 원인을 지정할 수 없습니다.", nameof(movementCause));
            MovementCause = movementCause;
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

    /// <summary>특성의 실제 적용 시점 상태입니다. 최종 Buffer 사실을 미리 읽지 않고 순서대로 소비합니다.</summary>
    public sealed class CombatTraitStateEvent : CombatEvent
    {
        public CharacterCombatFacts Expected { get; }
        public CharacterCombatFacts Final { get; }
        public CombatDamageOrigin Origin { get; }

        public CombatEffectiveStatsSnapshot ExpectedStats { get; }
        public CombatEffectiveStatsSnapshot FinalStats { get; }

        public CombatTraitStateEvent(CharacterCombatFacts expected, CharacterCombatFacts final, CombatDamageOrigin origin = null,
            CombatEffectiveStatsSnapshot expectedStats = null, CombatEffectiveStatsSnapshot finalStats = null)
            : base(CombatEventKind.TraitStateChanged)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.EntityId != final.EntityId)
                throw new ArgumentException("특성 상태 결과의 개체 식별자가 다릅니다.");
            if ((expectedStats == null) != (finalStats == null)) throw new ArgumentException("유효 능력치의 앞뒤 값은 함께 지정해야 합니다.");
            ExpectedStats = expectedStats;
            FinalStats = finalStats;
            Origin = origin;
        }
    }

    public sealed class CombatMovementSourceEvent : CombatEvent
    {
        public MovementTileFact Expected { get; }
        public MovementTileFact Final { get; }
        public CombatMovementSourceEvent(MovementTileFact expected, MovementTileFact final)
            : base(CombatEventKind.MovementSourceChanged)
        {
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected == null && final is not TraitTrailFact || expected != null &&
                (expected.Id != final.Id || expected.GetType() != final.GetType() || expected.SuccessorFailure(final) != null))
                throw new ArgumentException("이동 원본 결과는 흔적 생성 또는 동일 원본의 유효한 후속 값이어야 합니다.");
            Expected = expected;
        }
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
        {
            if (traits == null) throw new ArgumentNullException(nameof(traits));
            if (traits.Any(t => t == null || t.Interception != null)) throw new ArgumentException("가로채기 반응은 공격 결과에만 둡니다.", nameof(traits));
            Traits = Array.AsReadOnly(traits.ToArray()); Categories = Array.AsReadOnly(categories.ToArray());
        }
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

    public enum CombatArtifactPurpose
    {
        Unspecified,
        AttackPowerContribution,
        Healing,
        Reflection
    }

    public sealed class CombatArtifactEvent : CombatEvent
    {
        public CombatArtifactPurpose Purpose { get; }
        public IReadOnlyList<ArtifactActivationSnapshot> Activations { get; }
        public ThornHeartReflectionContext Reflection { get; }

        public CombatArtifactEvent(IReadOnlyList<ArtifactActivationSnapshot> activations) : this(activations, null)
        {
        }

        public CombatArtifactEvent(IReadOnlyList<ArtifactActivationSnapshot> activations, CombatArtifactPurpose purpose)
            : this(activations, null, purpose)
        {
        }

        public CombatArtifactEvent(IReadOnlyList<ArtifactActivationSnapshot> activations,
            ThornHeartReflectionContext reflection, CombatArtifactPurpose purpose = CombatArtifactPurpose.Unspecified) : base(CombatEventKind.ArtifactActivated)
        {
            if (reflection != null && purpose == CombatArtifactPurpose.Unspecified) purpose = CombatArtifactPurpose.Reflection;
            if (!Enum.IsDefined(typeof(CombatArtifactPurpose), purpose) || (purpose == CombatArtifactPurpose.Reflection) != (reflection != null))
                throw new ArgumentException("Artifact 발동 목적과 반사 문맥이 일치해야 합니다.", nameof(purpose));
            Purpose = purpose;
            if (activations == null) throw new ArgumentNullException(nameof(activations));
            var copy = activations.ToArray();
            if (reflection != null && (copy.Length != 1 || copy[0] == null ||
                copy[0].Origin.Kind != EffectOwnerKind.Artifact))
                throw new ArgumentException("반사 사건에는 Artifact 발동 하나가 필요합니다.", nameof(activations));

            Activations = Array.AsReadOnly(copy);
            Reflection = reflection;
        }
    }

    public sealed class CombatTurnEvent : CombatEvent
    {
        public EntityId ActorId { get; }
        public CombatTurnFacts Expected { get; }
        public CombatTurnFacts Final { get; }
        public Owner? Controller => Kind == CombatEventKind.ControllerTurnStarted ? Final.Controller : Expected.Controller;
        public long ControllerTurnSequence => Kind == CombatEventKind.ControllerTurnStarted ? Final.ControllerTurnSequence : Expected.ControllerTurnSequence;
        public CombatTurnEvent(CombatEventKind kind, EntityId actor, CombatTurnFacts expected, CombatTurnFacts final) : base(kind)
        {
            if (kind != CombatEventKind.CombatEntered && kind != CombatEventKind.ControllerTurnStarted &&
                kind != CombatEventKind.PlayerTurnEnded && kind != CombatEventKind.EnemyPhaseEnded && kind != CombatEventKind.NightEnded) throw new ArgumentOutOfRangeException(nameof(kind));
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
