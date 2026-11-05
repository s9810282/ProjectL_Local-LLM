using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>Combat 화면 연결이 튜토리얼 실행 대상이 아닌 이유입니다. 개발 로그와 시험이 읽습니다.</summary>
    internal enum TutorialViewTargetMiss
    {
        None = 0,
        /// <summary>설치된 전투가 없습니다. 이때 Start를 받아들이면 전투 기준점이 0이 됩니다(구현 계획 T-3 검토 L1).</summary>
        NoBattle,
        NoMap,
        /// <summary>현재 맵을 대상으로 하는 정의가 없습니다.</summary>
        NoDefinition,
        NoSelection,
        /// <summary>현재 칸이 전투가 아닙니다.</summary>
        NotCombatNode,
        /// <summary>이 맵에서 이미 클리어한 전투가 있습니다(두 번째 전투부터).</summary>
        CombatCleared
    }

    /// <summary>
    /// "1-1"(대상 맵에서 처음 들어가는 전투) 판정입니다(튜토리얼 설계 §15.12). 공개 Buffer와 정의 표만 읽고 아무것도 바꾸지 않습니다.
    /// </summary>
    internal static class TutorialViewTarget
    {
        /// <summary>
        /// 지금 연결한 Combat 화면에서 실행할 정의를 찾습니다. 대상이면 <see cref="TutorialViewTargetMiss.None"/>과 정의 키를,
        /// 아니면 그 이유와 키 0을 돌려줍니다.
        /// </summary>
        internal static TutorialViewTargetMiss Find(ICombatStateBufferReadSink combat, ICurrentProgressBufferReadSink current,
            IStageProgressBufferReadSink stages, ITutorialDefinitionTableQuery definitions, out int tutorialKey)
        {
            if (combat == null)
                throw new ArgumentNullException(nameof(combat));
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (stages == null)
                throw new ArgumentNullException(nameof(stages));
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            tutorialKey = 0;
            if (!combat.HasBattle)
                return TutorialViewTargetMiss.NoBattle;
            // 1. 현재 맵이 정의의 대상 맵이다. 대상 맵 키는 정의 데이터에만 있다.
            if (!current.HasMap)
                return TutorialViewTargetMiss.NoMap;
            if (!definitions.TryGetByTargetMap(current.CurrentMapKey, out TutorialDefinitionData definition))
                return TutorialViewTargetMiss.NoDefinition;
            if (!current.HasSelection)
                return TutorialViewTargetMiss.NoSelection;

            // 2. 현재 칸이 전투다. 3. 이 맵에서 클리어한 전투가 없다.
            int nodeId = current.CurrentStage.NodeId;
            bool isCombatNode = false;
            foreach (StageProgressData stage in stages.GetStages(current.CurrentMapKey))
            {
                if (stage.StageType != StageType.Combat)
                    continue;
                if (stage.IsClear)
                    return TutorialViewTargetMiss.CombatCleared;
                if (stage.NodeId == nodeId)
                    isCombatNode = true;
            }

            if (!isCombatNode)
                return TutorialViewTargetMiss.NotCombatNode;

            tutorialKey = definition.TutorialKey;
            return TutorialViewTargetMiss.None;
        }
    }
}
