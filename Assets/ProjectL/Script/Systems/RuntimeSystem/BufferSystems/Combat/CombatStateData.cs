using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Combat
{
    /// <summary>전투 전체 수명에 속하는 캐릭터 외 상태의 불변 조회값입니다.</summary>
    public sealed class CombatStateData
    {
        public RunDeckSnapshot Membership { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int ReserveCapacity { get; }
        /// <summary>난수 입력 이름을 유지한다. 유닛 순서나 밤 길이로 사용하지 않는다.</summary>
        public int TimelineSeed { get; }
        public CombatCommandState CommandState { get; }
        public MovementEnvironmentFacts Environment => CommandState.Environment;
        public StageGimmickStateSet Gimmicks => CommandState.Gimmicks;
        public IReadOnlyList<TornadoAreaFact> ReadTornadoAreas(Owner observer) => TornadoAreaFact.Read(CommandState, StageGimmicks, observer);
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        public CombatFlowPhase Phase => CommandState.Phase;
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        /// <summary>아군 배치 구역이다. 적은 이 구역 밖이면 어디든 놓인다. View는 이 칸으로 구역을 칠한다.</summary>
        public PlayerDeploymentZone PlayerZone { get; }
        /// <summary>전환 입구다. 줄 구역이면 그 줄 수, 칸 구역이면 0이다. View가 PlayerZone으로 옮기면 지운다.</summary>
        public int PlayerDeploymentDepth => PlayerZone.RowDepth ?? 0;
        /// <summary>전환 입구다. 적 배치 구역이 없어 늘 0이다. View가 PlayerZone으로 옮기면 지운다.</summary>
        public int EnemyDeploymentDepth => 0;
        public IReadOnlyList<StructureCombatFacts> Structures => CommandState.Structures;
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> StageGimmicks { get; }
        /// <summary>전투 동안 바뀌지 않는 유닛별 특성 설정값이다. 설명 숫자는 <c>TraitSettings.For(유닛)</c>로 읽는다(설명 수치 공개 설계 SP3).</summary>
        public BattleTraitSettings TraitSettings { get; }
        public NexusReinforcementSpec NexusReinforcements { get; }

        /// <summary>현재 넥서스의 기하 범위다. 실제 증원 가능 칸이나 숨은 방해물은 공개하지 않는다.</summary>
        public IReadOnlyList<CellCoord> ReadNexusReinforcementArea() =>
            NexusReinforcementGeometry.Area(CommandState, NexusReinforcements);

        internal CombatStateData(BattlePreparation battle)
        {
            Membership = battle.Membership;
            CombatPreparation preparation = battle.Combat;
            BoardWidth = preparation.BoardWidth;
            BoardHeight = preparation.BoardHeight;
            ReserveCapacity = preparation.ReserveCapacity;
            TimelineSeed = preparation.TimelineSeed;
            CommandState = battle.CommandState;
            EnvironmentBindings = preparation.EnvironmentBindings;
            ObstacleContents = preparation.ObstacleContents;

            var drawOrder = new CharacterInstanceId[preparation.DrawOrder.Count];
            for (int i = 0; i < drawOrder.Length; i++)
                drawOrder[i] = preparation.DrawOrder[i];

            DrawOrder = Array.AsReadOnly(drawOrder);
            DrawCursor = preparation.DrawCursor;
            PlayerZone = preparation.PlayerZone;
            RailCells = preparation.RailCells;
            StageGimmicks = preparation.StageGimmicks;
            TraitSettings = preparation.TraitSettings;
            NexusReinforcements = preparation.NexusReinforcements;
        }

        internal CombatStateData(CombatStateData source, CombatCommandState final,
            EnvironmentSemanticBindings environmentBindings = null)
        {
            Membership = source.Membership;
            CommandState = final;
            EnvironmentBindings = environmentBindings ?? source.EnvironmentBindings;
            ObstacleContents = source.ObstacleContents;
            BoardWidth = source.BoardWidth; BoardHeight = source.BoardHeight;
            ReserveCapacity = source.ReserveCapacity;
            TimelineSeed = source.TimelineSeed; DrawOrder = source.DrawOrder; DrawCursor = source.DrawCursor;
            PlayerZone = source.PlayerZone; RailCells = source.RailCells;
            StageGimmicks = source.StageGimmicks;
            TraitSettings = source.TraitSettings;
            NexusReinforcements = source.NexusReinforcements;
        }
    }
}
