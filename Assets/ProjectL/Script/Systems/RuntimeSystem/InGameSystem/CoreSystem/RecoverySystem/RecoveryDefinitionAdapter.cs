using System;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    /// <summary>지정된 회복 스테이지의 선택지를 저작 순서대로 검증하고 복사한다.</summary>
    internal static class RecoveryDefinitionAdapter
    {
        internal static RecoveryStageSpec Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) =>
            Read(database, chapterKey, stageKey, out _);

        /// <summary>진입 제안에 판단 근거를 담을 수 있도록 정의의 지도 역할도 함께 돌려준다.</summary>
        internal static RecoveryStageSpec Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey,
            out StageMapRole mapRole)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (stageKey.IsEmpty) throw new ArgumentException("회복 스테이지 키가 필요합니다.", nameof(stageKey));
            var query = database.InGameQuery
                ?? throw new InvalidOperationException("인게임 DB 조회가 필요합니다.");
            var stages = query.StageTableQuery
                ?? throw new InvalidOperationException("스테이지 DB 조회가 필요합니다.");
            var root = stages.GetStage(stageKey.Value)
                ?? throw new InvalidOperationException("회복 스테이지 정의가 필요합니다.");
            root.Validate();
            if (root.StageKey != stageKey.Value || root.MapKey != chapterKey ||
                root.MapRole == StageMapRole.Boss || root.StageData is not EventStageDetail recovery)
                throw new InvalidOperationException("대상은 지정된 챕터의 보스가 아닌 회복 스테이지여야 합니다.");
            mapRole = root.MapRole;

            var table = query.RecoveryOptionTableQuery
                ?? throw new InvalidOperationException("회복 선택지 DB 조회가 필요합니다.");
            var options = new RecoveryOptionSpec[recovery.RecoveryOptionKeys.Count];
            for (int i = 0; i < options.Length; i++)
            {
                int key = recovery.RecoveryOptionKeys[i];
                var data = table.GetRecoveryOption(key)
                    ?? throw new InvalidOperationException("회복 선택지 정의가 필요합니다.");
                data.Validate();
                if (data.OptionId != key)
                    throw new InvalidOperationException("조회한 회복 선택지의 키가 요청과 다릅니다.");
                options[i] = new RecoveryOptionSpec(new RecoveryOptionId(data.OptionId), data.Cost,
                    new RecoveryHealSpec(data.HealMode, data.HealValue));
            }
            return new RecoveryStageSpec(stageKey, options);
        }
    }
}
