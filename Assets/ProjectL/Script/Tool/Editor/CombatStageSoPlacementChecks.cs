using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // FU1·FU2: Core CellEnvironmentQuery.ValidateInitialPlacement 및 EnvironmentPlacementRules의
    // 벽/낭떠러지와 고정 배치 겹침 두 조건만 사전 확인합니다(2026-10-31 기준).
    // 전체 전투 준비를 대신하지 않으며 입력 데이터는 변경하지 않습니다.
    internal static class CombatStageSoPlacementChecks
    {
        internal static List<CombatStageSoIssue> Check(IEnumerable<(string Label, List<Vector2Int> Cells, CombatStageSoIssueTarget Target)> targets,
            HashSet<Vector2Int> walls, HashSet<Vector2Int> cliffs)
        {
            var problems = new List<CombatStageSoIssue>();
            foreach (var target in targets)
            {
                foreach (var cell in target.Cells)
                {
                    string terrain = walls.Contains(cell) ? "벽" : cliffs.Contains(cell) ? "낭떠러지" : null;
                    if (terrain != null)
                        problems.Add(new CombatStageSoIssue($"{target.Label}: {terrain}과 점유 칸이 겹칩니다 ({cell.x}, {cell.y}).", cell, target.Target));
                }
            }
            return problems;
        }
    }
}
