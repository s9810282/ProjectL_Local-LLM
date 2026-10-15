using System;
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
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int ReserveCapacity { get; }
        public long NightLength { get; }
        public int TimelineSeed { get; }
        public CombatCommandState CommandState { get; }
        public MovementEnvironmentFacts Environment => CommandState.Environment;
        public EnvironmentSemanticBindings EnvironmentBindings { get; }
        public IReadOnlyList<ObstacleContentProfile> ObstacleContents { get; }
        public CombatFlowPhase Phase => CommandState.Phase;
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        public long NextEntityId { get; }
        public int PlayerDeploymentDepth { get; }
        public int EnemyDeploymentDepth { get; }
        public IReadOnlyList<StructureCombatFacts> Structures => CommandState.Structures;
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> StageGimmicks { get; }
        public int? ManualDrawGoldCost { get; }
        /// <summary>이 전투(스테이지)의 골드. 수동 드로우만 쓰며 전투와 함께 끝난다. Run 재화(CurrencyBuffer)와 별개다.</summary>
        public int StageGold { get; }

        internal CombatStateData(BattlePreparation battle)
        {
            CombatPreparation preparation = battle.Combat;
            BoardWidth = preparation.BoardWidth;
            BoardHeight = preparation.BoardHeight;
            ReserveCapacity = preparation.ReserveCapacity;
            NightLength = preparation.NightLength;
            TimelineSeed = preparation.TimelineSeed;
            CommandState = battle.CommandState;
            EnvironmentBindings = preparation.EnvironmentBindings;
            ObstacleContents = preparation.ObstacleContents;

            var drawOrder = new CharacterInstanceId[preparation.DrawOrder.Count];
            for (int i = 0; i < drawOrder.Length; i++)
                drawOrder[i] = preparation.DrawOrder[i];

            DrawOrder = Array.AsReadOnly(drawOrder);
            DrawCursor = preparation.DrawCursor;
            NextEntityId = battle.NextEntityId;
            PlayerDeploymentDepth = preparation.PlayerDeploymentDepth;
            EnemyDeploymentDepth = preparation.EnemyDeploymentDepth;
            RailCells = preparation.RailCells;
            StageGimmicks = preparation.StageGimmicks;
            ManualDrawGoldCost = preparation.ManualDrawGoldCost;
            StageGold = preparation.InitialStageGold;
        }

        internal CombatStateData(CombatStateData source, CombatCommandState final, CombatDrawSettlement draw = null,
            EnvironmentSemanticBindings environmentBindings = null)
        {
            CommandState = final;
            EnvironmentBindings = environmentBindings ?? source.EnvironmentBindings;
            ObstacleContents = source.ObstacleContents;
            BoardWidth = source.BoardWidth; BoardHeight = source.BoardHeight;
            ReserveCapacity = source.ReserveCapacity; NightLength = source.NightLength;
            TimelineSeed = source.TimelineSeed; DrawOrder = source.DrawOrder; DrawCursor = draw?.FinalCursor ?? source.DrawCursor;
            NextEntityId = draw?.FinalNextEntityId ?? source.NextEntityId; PlayerDeploymentDepth = source.PlayerDeploymentDepth;
            EnemyDeploymentDepth = source.EnemyDeploymentDepth; RailCells = source.RailCells;
            StageGimmicks = source.StageGimmicks;
            ManualDrawGoldCost = source.ManualDrawGoldCost;
            StageGold = draw?.FinalGold ?? source.StageGold;
        }
    }
}
