using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>맵 작업실의 읽기 전용 전투 스테이지 맥락입니다. 맵·SO를 변경하지 않습니다.</summary>
    internal sealed class FbxCombatStageContext
    {
        private FbxCombatStageContext(int stageKey, string mode, string rootName,
            CombatStageSoField field, CombatStageSoFieldView fieldView, string problem)
        {
            StageKey = stageKey;
            Mode = mode;
            RootName = rootName;
            Field = field;
            FieldView = fieldView;
            Problem = problem;
        }

        internal int StageKey { get; }
        internal string Mode { get; }
        internal string RootName { get; }
        internal CombatStageSoField Field { get; }
        internal CombatStageSoFieldView FieldView { get; }
        internal string Problem { get; }
        internal bool IsReady => Problem == null;

        internal static FbxCombatStageContext Read(Object source, GameObject openPrefab)
        {
            if (source == null)
                return new FbxCombatStageContext(0, null, null, null, null, "미리보기용 ViewStageDataSO를 선택하세요.");
            try
            {
                var prop = CombatStageSoAssets.ReadStageViewPropSource(source);
                if (prop.StageKey < 1)
                    throw new InvalidOperationException("StageKey는 1 이상이어야 합니다.");
                if (prop.Prefab == null)
                    throw new InvalidOperationException($"StageKey {prop.StageKey}: 배경 방식 {prop.Mode}의 프리팹 참조가 없습니다.");
                if (openPrefab == null)
                    throw new InvalidOperationException("먼저 맵 프리팹을 작업실에 여세요.");
                if (!SameAsset(prop.Prefab, openPrefab))
                    throw new InvalidOperationException(
                        $"선택한 ViewStage는 '{prop.Prefab.name}'을 쓰지만 작업실은 '{openPrefab.name}'을 열었습니다.");

                var stage = CombatStageSoAssets.ReadStageField(prop.StageKey);
                CombatStageSoFieldView fieldView = CombatStageSoAssets.ReadRegisteredFieldView();
                string problem = prop.Mode == "PrefabWithTiles" && fieldView.Approximate
                    ? "바닥 타일 방식에는 GridOutline의 실제 칸 간격이 필요합니다. StageFieldViewDataSO 표시 방식을 확인하세요."
                    : null;
                return new FbxCombatStageContext(prop.StageKey, prop.Mode, stage.Root.name, stage.Field, fieldView, problem);
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                return new FbxCombatStageContext(0, null, null, null, null, exception.Message);
            }
        }

        private static bool SameAsset(Object first, Object second) =>
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first, out string firstGuid, out long firstId)
            && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(second, out string secondGuid, out long secondId)
            && firstGuid == secondGuid && firstId == secondId;
    }
}
