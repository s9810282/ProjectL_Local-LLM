using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>Systems가 전투 Buffer와 정의에서 읽은 명령별 입력이다.</summary>
    public sealed class CombatBattleFacts
    {
        public int ReserveCapacity { get; }
        public int TimelineSeed { get; }
        /// <summary>아군 배치 구역이다. 배치 판정·적 재배치는 이 값만 읽는다.</summary>
        public PlayerDeploymentZone PlayerZone { get; }
        /// <summary>전환 입구다. 줄 구역이면 그 줄 수, 칸 구역이면 0이다. Core 로직은 읽지 않는다.</summary>
        public int PlayerDeploymentDepth => PlayerZone.RowDepth ?? 0;
        /// <summary>전환 입구다. 적 배치 구역이 없어 늘 0이다. Core 로직은 읽지 않는다.</summary>
        public int EnemyDeploymentDepth => 0;
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        public IReadOnlyList<CategorySpec> Categories { get; }
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> GimmickDefinitions { get; }
        /// <summary>전투 정의에서 읽은 경험치·레벨업 수치다. 기본값은 꺼짐이다.</summary>
        public LevelProgressionSpec Progression { get; }
        public StageClearRewardSpec ClearRewards { get; }
        public NexusReinforcementSpec NexusReinforcements { get; }
        /// <summary>
        /// 전환 입구다. 아군 줄 수로 줄 구역을 만든다. 적 줄 수는 받기만 하고 쓰지 않는다.
        /// 새 코드는 <see cref="PlayerDeploymentZone"/>을 받는 생성자를 쓴다.
        /// </summary>
        public CombatBattleFacts(int reserveCapacity, int timelineSeed,
            int playerDeploymentDepth, int enemyDeploymentDepth, IReadOnlyList<CharacterInstanceId> drawOrder,
            int drawCursor,
            IReadOnlyList<CellCoord> railCells = null, IReadOnlyList<StageGimmickSpec> gimmicks = null,
            IReadOnlyList<CategorySpec> categories = null, LevelProgressionSpec progression = default, StageClearRewardSpec clearRewards = null,
            NexusReinforcementSpec nexusReinforcements = null)
            : this(reserveCapacity, timelineSeed,
                playerDeploymentDepth < 0 ? throw new ArgumentException("전투 입력 값이 잘못되었습니다.") : PlayerDeploymentZone.Rows(playerDeploymentDepth),
                drawOrder, drawCursor, railCells, gimmicks, categories, progression, clearRewards, nexusReinforcements)
        {
        }

        /// <remarks>다음 EntityId는 전투 중 바뀌는 진행값이므로 여기 두지 않는다. 원본은 CombatCommandState.Allocation이다.</remarks>
        public CombatBattleFacts(int reserveCapacity, int timelineSeed,
            PlayerDeploymentZone playerZone, IReadOnlyList<CharacterInstanceId> drawOrder,
            int drawCursor,
            IReadOnlyList<CellCoord> railCells = null, IReadOnlyList<StageGimmickSpec> gimmicks = null,
            IReadOnlyList<CategorySpec> categories = null, LevelProgressionSpec progression = default, StageClearRewardSpec clearRewards = null,
            NexusReinforcementSpec nexusReinforcements = null)
        {
            if (reserveCapacity < 0 || playerZone == null ||
                drawCursor < 0 || drawCursor > drawOrder.Count)
                throw new ArgumentException("전투 입력 값이 잘못되었습니다.");
            ReserveCapacity = reserveCapacity; TimelineSeed = timelineSeed;
            PlayerZone = playerZone;
            DrawOrder = Array.AsReadOnly(drawOrder.ToArray()); DrawCursor = drawCursor;
            RailCells = Array.AsReadOnly((railCells ?? Array.Empty<CellCoord>()).ToArray());
            GimmickDefinitions = Array.AsReadOnly((gimmicks ?? Array.Empty<StageGimmickSpec>()).ToArray());
            Categories = categories == null ? null : Array.AsReadOnly(categories.ToArray());
            Progression = progression;
            ClearRewards = clearRewards;
            NexusReinforcements = nexusReinforcements;
        }
    }
}
