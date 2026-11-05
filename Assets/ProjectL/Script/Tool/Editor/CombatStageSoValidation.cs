using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using UnityEditor;
using Object = UnityEngine.Object;
using CellCoord = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord;
using RockfallGimmickSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RockfallGimmickSpec;
using HaboobGimmickSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.HaboobGimmickSpec;

namespace ProjectL.Tool.Editor
{
    // 스테이지 에셋 셋(저장본 또는 저장 전 복제본)을 검사합니다. 배치 규칙의 원본은 SO의 GetData()이고,
    // Root·View 짝과 고정 배치의 벽/낭떠러지 겹침을 더 봅니다(FU-a). 나머지 규칙은 Core가 검사합니다.
    internal static class CombatStageSoValidation
    {
        // 작업본을 저장 전 복제본 셋에 써서 검사합니다. 실제 에셋은 건드리지 않습니다(설계 SM2, 계획 P11).
        // 스테이지 키는 모든 Stage Root에서 유일해야 하므로 다른 Root의 키를 함께 받습니다.
        internal static List<string> CheckDocument(CombatStageSoDocument document, Object root, Object combat, Object view, ICollection<int> otherStageKeys, bool clearLegacyPlacements = false) =>
            CheckDocumentIssues(document, root, combat, view, otherStageKeys, clearLegacyPlacements).Select(issue => issue.Message).ToList();

        internal static List<CombatStageSoIssue> CheckDocumentIssues(CombatStageSoDocument document, Object root, Object combat, Object view, ICollection<int> otherStageKeys, bool clearLegacyPlacements = false)
        {
            var problems = new List<CombatStageSoIssue>();
            if (root == null || combat == null)
            {
                problems.Add(new CombatStageSoIssue("Stage Root 또는 전투 데이터 에셋을 찾을 수 없습니다."));
                return problems;
            }
            if (otherStageKeys.Contains(document.stageKey))
                problems.Add(new CombatStageSoIssue($"다른 Stage Root가 이미 쓰는 스테이지 키입니다: {document.stageKey}"));
            Object rootCopy = Copy(root), combatCopy = Copy(combat), viewCopy = view != null ? Copy(view) : null;
            // 이 스테이지만 쓰는 공유 SO의 저장 안 한 값도 복제본에 담아, 전투 데이터 복제본이 그 복제본을 가리키게 합니다(계획 P11).
            var sharedCopies = new Dictionary<string, Object>();
            try
            {
                foreach (var edit in document.ActiveSharedEdits())
                {
                    Object asset = CombatStageSoAssets.Load(edit.guid);
                    if (asset == null)
                    {
                        problems.Add(new CombatStageSoIssue($"고친 공유 SO를 찾을 수 없습니다: {edit.guid}"));
                        continue;
                    }
                    Object sharedCopy = Copy(asset);
                    EditorJsonUtility.FromJsonOverwrite(edit.json, sharedCopy);
                    sharedCopies[edit.guid] = sharedCopy;
                    var sharedProblems = new List<string>();
                    TryGetData(sharedCopy, "공유 SO", sharedProblems);
                    problems.AddRange(sharedProblems.Select(message => new CombatStageSoIssue(message)));
                }
                CombatStageSoAssets.Write(document, rootCopy, combatCopy, viewCopy,
                    guid => sharedCopies.TryGetValue(guid, out Object edited) ? edited : null);
                if (clearLegacyPlacements) CombatStageSoAssets.ClearLegacyPlacements(combatCopy);
                // 본부 파괴 목표인데 본부가 없으면 게임은 이길 수 없어 멈추지도 않습니다. 저장 전에 막습니다(설계 §18).
                if (!document.HasHeadquarters && CombatStageSoAssets.IsHeadquartersObjective(document, combat))
                    problems.Add(new CombatStageSoIssue("승리 조건이 본부 파괴인데 본부가 없습니다. 승리 조건을 바꾸거나 오른쪽 '참조'에서 본부 정의를 고르세요."));
                // 낙석 오류를 머리말과 함께 한 번만 내고, 복제본에서만 꺼서 나머지 검사를 잇습니다(계획 P-RF1).
                if (!TryCheckRockfall(document.rockfall, problems)) CombatStageSoAssets.DisableRockfall(combatCopy);
                if (!TryCheckHaboob(document.haboob, problems)) CombatStageSoAssets.DisableHaboob(combatCopy);
                foreach (string objectName in CombatStageSoAssets.RockfallUniqueObjectNames)
                {
                    if (CombatStageSoAssets.RockfallObjectCount(document, view, objectName) > 1)
                        problems.Add(new CombatStageSoIssue($"맵 프리팹에 {objectName}가 둘 이상이라 전투 진입 때 멈춥니다(낙석이 꺼져 있어도). 하나만 남기세요."));
                }
                problems.AddRange(Check(rootCopy, combatCopy, viewCopy).Select(message => new CombatStageSoIssue(message)));
                problems.AddRange(CombatStageSoAssets.CheckFixedPlacements(rootCopy, combatCopy, viewCopy));
            }
            catch (Exception exception) { problems.Add(new CombatStageSoIssue(exception.Message)); }
            finally
            {
                Object.DestroyImmediate(rootCopy);
                Object.DestroyImmediate(combatCopy);
                if (viewCopy != null) Object.DestroyImmediate(viewCopy);
                foreach (Object sharedCopy in sharedCopies.Values) Object.DestroyImmediate(sharedCopy);
            }
            return problems;
        }

        // 값 규칙의 원본은 Core RockfallGimmickSpec입니다(설계 §17 RF4). 꺼져 있으면 게임도 값을 쓰지 않으므로 보지 않습니다.
        private static bool TryCheckRockfall(CombatStageSoRockfall rockfall, List<CombatStageSoIssue> problems)
        {
            if (!rockfall.enabled) return true;
            try
            {
                new RockfallGimmickSpec(rockfall.cooldownTurns, rockfall.warningTurns, rockfall.minimumRocks, rockfall.maximumRocks,
                    rockfall.maximumHealthLossPercent, rockfall.bindTurns,
                    rockfall.footprintOffsets.Select(cell => new CellCoord(cell.x, cell.y)).ToList());
                return true;
            }
            catch (Exception exception)
            {
                problems.Add(new CombatStageSoIssue("낙석 값: " + exception.Message));
                return false;
            }
        }

        // 실제 위치를 추측하지 않고 설정값만 Core 공개 계약으로 검사합니다.
        private static bool TryCheckHaboob(CombatStageSoHaboob haboob, List<CombatStageSoIssue> problems)
        {
            if (!haboob.enabled) return true;
            try
            {
                // 시작 위치를 지정했으면 함께 넘겨 음수 검사도 Core 계약이 맡습니다(설계 §25 HAT3). 판 크기 검사는 창의 SoProblems가 합니다.
                new HaboobGimmickSpec(haboob.relocationIntervalTurns, haboob.warningLeadTurns,
                    haboob.footprintOffsets.Select(cell => new CellCoord(cell.x, cell.y)).ToList(),
                    haboob.fixedInitialAnchor ? new CellCoord(haboob.initialAnchor.x, haboob.initialAnchor.y) : (CellCoord?)null);
                return true;
            }
            catch (Exception exception)
            {
                problems.Add(new CombatStageSoIssue("하붑 값: " + exception.Message));
                return false;
            }
        }

        internal static List<string> Check(Object root, Object combat, Object view)
        {
            var problems = new List<string>();
            if (root == null || combat == null)
            {
                problems.Add("Stage Root 또는 전투 데이터 에셋을 찾을 수 없습니다.");
                return problems;
            }
            // Root의 GetData는 전투 데이터의 GetData를 다시 부르므로, 전투 데이터가 통과했을 때만 부릅니다.
            if (TryGetData(combat, "전투 데이터", problems)) TryGetData(root, "Stage Root", problems);
            if (view == null) problems.Add("짝 View Stage Data가 없습니다. 같은 스테이지 키의 ViewStageDataSO가 필요합니다.");
            else TryGetData(view, "View", problems);
            try { CheckPairs(root, view, problems); }
            catch (Exception exception) { problems.Add(exception.Message); }
            return problems;
        }

        private static void CheckPairs(Object root, Object view, List<string> problems)
        {
            if (view == null) return;
            var document = CombatStageSoAssets.Read(root, view, new CombatStageSoNames());
            int viewKey = CombatStageSoAssets.ViewStageKey(view);
            if (viewKey != document.stageKey)
                problems.Add($"스테이지 키가 다릅니다: Stage Root {document.stageKey}, View {viewKey}.");
            var terrainIds = new HashSet<long>(document.terrains.Select(terrain => terrain.sourceId).Concat(document.legacyTerrainSourceIds));
            foreach (var rotation in document.view.terrainRotations)
                if (!terrainIds.Contains(rotation.sourceId))
                    problems.Add($"View 지형 회전의 sourceId {rotation.sourceId}가 전투 데이터 지형에 없습니다.");
            // 이웃 모양 지형은 이웃 칸이 모양과 회전을 정해, 외형 회전이 적혀 있으면(R0이어도) 전투 화면이 멈춥니다
            // (게임 CombatTerrainLook.Resolve, 병합 대응 설계 MG4). 외형 표는 검사 때마다 새로 읽습니다(계획 P-MG1).
            var edgeKinds = CombatStageSoAssets.EdgeShapeTerrainKinds();
            foreach (var terrain in document.terrains)
                if (CombatStageSoAssets.UsesEdgeShapes(terrain.definitionGuid, edgeKinds) &&
                    document.view.terrainRotations.Exists(rotation => rotation.sourceId == terrain.sourceId))
                    problems.Add($"이웃 모양 지형 {CombatStageSoAssets.NameOf(terrain.definitionGuid)}(sourceId {terrain.sourceId})에 외형 회전이 있습니다. " +
                                 "모양이 회전을 정하므로 전투 화면이 멈춥니다. 고른 칸에서 외형 회전을 지우세요.");
            // 토네이도도 같습니다. Core가 원본 ID를 지형 분류에서 빼서 FieldTerrainPresenter.ValidateTerrainRotations가 전투 진입 때 멈춥니다.
            foreach (var terrain in document.terrains)
                if (CombatStageSoAssets.IsTornado(terrain.definitionGuid) &&
                    document.view.terrainRotations.Exists(rotation => rotation.sourceId == terrain.sourceId))
                    problems.Add($"토네이도 {CombatStageSoAssets.NameOf(terrain.definitionGuid)}(sourceId {terrain.sourceId})에 외형 회전이 있습니다. " +
                                 "토네이도는 회전을 쓰지 않아 전투 화면이 멈춥니다. 고른 칸에서 외형 회전을 지우세요.");
            var objectIds = new HashSet<long>(document.objects.Select(item => item.sourceId));
            foreach (var rotation in document.view.objectRotations)
                if (!objectIds.Contains(rotation.sourceId))
                    problems.Add($"View 오브젝트 회전의 sourceId {rotation.sourceId}가 전투 데이터 오브젝트에 없습니다.");
        }

        // 문구에 원래 에셋 이름이 나오도록 복제본 이름을 맞춥니다.
        private static Object Copy(Object asset)
        {
            Object copy = Object.Instantiate(asset);
            copy.name = asset.name;
            return copy;
        }

        private static bool TryGetData(Object asset, string label, List<string> problems)
        {
            if (!(asset is DatabaseItem item))
            {
                problems.Add($"{label}({asset.name}): DB 항목(DatabaseItem)이 아닙니다.");
                return false;
            }
            try
            {
                item.GetData();
                return true;
            }
            catch (Exception exception)
            {
                problems.Add($"{label}({asset.name}): {exception.Message}");
                return false;
            }
        }
    }
}
