using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    /// <summary>기존 전투·턴·조회 번호와 행위자를 함께 전달하는 불변 값이다.</summary>
    public readonly struct PlayerSkillSourceStamp : IEquatable<PlayerSkillSourceStamp>
    {
        public long BattleInstanceId { get; }
        public long ControllerTurnSequence { get; }
        public long QueryRevision { get; }
        public EntityId ActorId { get; }

        public PlayerSkillSourceStamp(long battleInstanceId, long controllerTurnSequence, long queryRevision, EntityId actorId)
        {
            if (battleInstanceId <= 0) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            if (controllerTurnSequence < 0) throw new ArgumentOutOfRangeException(nameof(controllerTurnSequence));
            if (queryRevision <= 0) throw new ArgumentOutOfRangeException(nameof(queryRevision));
            if (actorId.IsEmpty) throw new ArgumentException("행위자 ID가 필요합니다.", nameof(actorId));
            BattleInstanceId = battleInstanceId;
            ControllerTurnSequence = controllerTurnSequence;
            QueryRevision = queryRevision;
            ActorId = actorId;
        }

        public bool Equals(PlayerSkillSourceStamp other) => BattleInstanceId == other.BattleInstanceId &&
            ControllerTurnSequence == other.ControllerTurnSequence && QueryRevision == other.QueryRevision && ActorId == other.ActorId;

        public override bool Equals(object obj) => obj is PlayerSkillSourceStamp other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(BattleInstanceId, ControllerTurnSequence, QueryRevision, ActorId);
    }

    public sealed class PlayerSkillBattleState
    {
        public CharacterInstanceId MainInstanceId { get; }
        public EntityId ActorId { get; }
        public PlayerSkillDefinition Definition { get; }
        public PlayerSkillUpgradeKey UpgradeKey { get; }
        public int RemainingUses { get; }
        public long LastUsedControllerTurnSequence { get; }

        public PlayerSkillBattleState(CharacterInstanceId mainId, EntityId actorId, PlayerSkillDefinition definition,
            PlayerSkillUpgradeKey upgrade)
            : this(mainId, actorId, definition, upgrade, definition?.StageUseLimit ?? 0, 0)
        {
        }

        private PlayerSkillBattleState(CharacterInstanceId mainId, EntityId actorId, PlayerSkillDefinition definition,
            PlayerSkillUpgradeKey upgrade, int remainingUses, long lastUsedSequence)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (mainId.IsEmpty) throw new ArgumentException("메인 보유 ID가 필요합니다.", nameof(mainId));
            if (actorId.IsEmpty) throw new ArgumentException("메인 전투 ID가 필요합니다.", nameof(actorId));
            if (upgrade != PlayerSkillUpgradeKey.None && !definition.SupportsUpgrade(upgrade))
                throw new ArgumentException("스킬에 속하지 않는 강화입니다.", nameof(upgrade));
            if (remainingUses < 0 || remainingUses > definition.StageUseLimit)
                throw new ArgumentOutOfRangeException(nameof(remainingUses));
            if (lastUsedSequence < 0) throw new ArgumentOutOfRangeException(nameof(lastUsedSequence));

            MainInstanceId = mainId;
            ActorId = actorId;
            UpgradeKey = upgrade;
            RemainingUses = remainingUses;
            LastUsedControllerTurnSequence = lastUsedSequence;
        }

        public PlayerSkillBattleState WithUsage(int remainingUses, long lastUsedSequence) =>
            new(MainInstanceId, ActorId, Definition, UpgradeKey, remainingUses, lastUsedSequence);

        public bool SameValues(PlayerSkillBattleState other) => other != null &&
            MainInstanceId == other.MainInstanceId && ActorId == other.ActorId && Definition.SameValues(other.Definition) &&
            UpgradeKey == other.UpgradeKey && RemainingUses == other.RemainingUses &&
            LastUsedControllerTurnSequence == other.LastUsedControllerTurnSequence;
    }

    /// <summary>수락된 스킬 한 번의 사용량 변경을 나타내는 게시 근거다.</summary>
    public sealed class PlayerSkillUseSettlement
    {
        public EntityId ActorId { get; }
        public int ExpectedRemainingUses { get; }
        public int FinalRemainingUses { get; }
        public long ExpectedLastUsedControllerTurnSequence { get; }
        public long FinalLastUsedControllerTurnSequence => ControllerTurnSequence;
        public long ControllerTurnSequence { get; }

        public PlayerSkillUseSettlement(EntityId actorId, int expectedRemainingUses, long expectedLastUsedSequence,
            long controllerTurnSequence)
        {
            if (actorId.IsEmpty) throw new ArgumentException("행위자 ID가 필요합니다.", nameof(actorId));
            if (expectedRemainingUses <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRemainingUses));
            if (expectedLastUsedSequence < 0) throw new ArgumentOutOfRangeException(nameof(expectedLastUsedSequence));
            if (controllerTurnSequence <= expectedLastUsedSequence)
                throw new ArgumentOutOfRangeException(nameof(controllerTurnSequence));

            ActorId = actorId;
            ExpectedRemainingUses = expectedRemainingUses;
            FinalRemainingUses = expectedRemainingUses - 1;
            ExpectedLastUsedControllerTurnSequence = expectedLastUsedSequence;
            ControllerTurnSequence = controllerTurnSequence;
        }
    }
}
