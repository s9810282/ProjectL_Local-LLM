using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>Systems가 기존 전투·재화 Buffer와 정의에서 읽은 명령별 입력이다.</summary>
    public sealed class CombatBattleFacts
    {
        public int ReserveCapacity { get; }
        public long NightLength { get; }
        public int TimelineSeed { get; }
        public int PlayerDeploymentDepth { get; }
        public int EnemyDeploymentDepth { get; }
        public IReadOnlyList<CharacterInstanceId> DrawOrder { get; }
        public int DrawCursor { get; }
        public long NextEntityId { get; }
        public int Gold { get; }
        public int? ManualDrawGoldCost { get; }
        public IReadOnlyList<CategorySpec> Categories { get; }
        public IReadOnlyList<CellCoord> RailCells { get; }
        public IReadOnlyList<StageGimmickSpec> GimmickDefinitions { get; }
        public CombatBattleFacts(int reserveCapacity, long nightLength, int timelineSeed,
            int playerDeploymentDepth, int enemyDeploymentDepth, IReadOnlyList<CharacterInstanceId> drawOrder,
            int drawCursor, long nextEntityId, int gold, int? manualDrawGoldCost,
            IReadOnlyList<CellCoord> railCells = null, IReadOnlyList<StageGimmickSpec> gimmicks = null,
            IReadOnlyList<CategorySpec> categories = null)
        {
            if (reserveCapacity < 0 || nightLength < 1 || playerDeploymentDepth < 0 || enemyDeploymentDepth < 0 ||
                drawCursor < 0 || drawCursor > drawOrder.Count || nextEntityId < 1 || gold < 0 || manualDrawGoldCost < 0)
                throw new ArgumentException("전투·드로우 입력 값이 잘못되었습니다.");
            ReserveCapacity = reserveCapacity; NightLength = nightLength; TimelineSeed = timelineSeed;
            PlayerDeploymentDepth = playerDeploymentDepth; EnemyDeploymentDepth = enemyDeploymentDepth;
            DrawOrder = Array.AsReadOnly(drawOrder.ToArray()); DrawCursor = drawCursor; NextEntityId = nextEntityId;
            Gold = gold; ManualDrawGoldCost = manualDrawGoldCost;
            RailCells = Array.AsReadOnly((railCells ?? Array.Empty<CellCoord>()).ToArray());
            GimmickDefinitions = Array.AsReadOnly((gimmicks ?? Array.Empty<StageGimmickSpec>()).ToArray());
            Categories = categories == null ? null : Array.AsReadOnly(categories.ToArray());
        }
    }
}
