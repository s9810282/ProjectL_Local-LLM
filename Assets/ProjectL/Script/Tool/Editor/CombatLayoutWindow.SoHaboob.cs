using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 하붑 설정 UI입니다. 규칙 검사와 SO 입출력은 기존 Validation/Assets가 맡습니다(설계 §22).
    public sealed partial class CombatLayoutWindow
    {
        private void DrawSoHaboobFootprint(List<Vector2Int> offsets)
        {
            EditorGUILayout.LabelField("하붑 영역 모양(상대 좌표)");
            EditorGUILayout.LabelField("왼쪽 X · 오른쪽 Y: 구름 모양 안의 상대 좌표입니다. 맵에서의 시작 위치는 '시작 위치 지정'으로 정합니다.", EditorStyles.wordWrappedMiniLabel);
            for (int i = 0; i < offsets.Count; i++)
            {
                int index = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    int x = EditorGUILayout.DelayedIntField(offsets[i].x);
                    int y = EditorGUILayout.DelayedIntField(offsets[i].y);
                    if (EditorGUI.EndChangeCheck()) SoChange("하붑 칸 변경", () => offsets[index] = new Vector2Int(x, y));
                    if (GUILayout.Button("지우기", GUILayout.Width(52)))
                    {
                        SoChange("하붑 칸 지우기", () => offsets.RemoveAt(index));
                        GUIUtility.ExitGUI();
                    }
                }
            }
            if (GUILayout.Button("칸 추가"))
            {
                int x = 0;
                while (offsets.Contains(new Vector2Int(x, 0))) x++;
                SoChange("하붑 칸 추가", () => offsets.Add(new Vector2Int(x, 0)));
                GUIUtility.ExitGUI();
            }
        }

        // 전투 시작 위치입니다(설계 §25 HAT1). 하붑이나 지정이 꺼지면 입력만 비활성이고 값은 남깁니다. 이동은 늘 무작위입니다.
        private void DrawSoHaboobStart(CombatStageSoHaboob haboob)
        {
            using (new EditorGUI.DisabledScope(!haboob.enabled))
            {
                if (SoToggle("시작 위치 지정", haboob.fixedInitialAnchor, out bool fixedAnchor))
                    SoChange("하붑 시작 위치 지정 변경", () => haboob.fixedInitialAnchor = fixedAnchor);
                using (new EditorGUI.DisabledScope(!haboob.fixedInitialAnchor))
                {
                    if (SoInt("시작 X", haboob.initialAnchor.x, out int x))
                        SoChange("하붑 시작 위치 변경", () => haboob.initialAnchor = new Vector2Int(x, haboob.initialAnchor.y));
                    if (SoInt("시작 Y", haboob.initialAnchor.y, out int y))
                        SoChange("하붑 시작 위치 변경", () => haboob.initialAnchor = new Vector2Int(haboob.initialAnchor.x, y));
                }
            }
            EditorGUILayout.LabelField("시작 X/Y는 모양을 감싸는 상자의 왼쪽 아래 기준점(상대 좌표 0,0)입니다. 맵 클릭·끌어 놓기는 누른 칸을 구름 가운데 근처로 맞춥니다. 시작 위치 지정을 끄거나 비우면 하붑 사용 여부는 유지하고, 하붑을 사용할 때 무작위로 시작합니다. 이후 이동은 늘 무작위입니다.", EditorStyles.wordWrappedMiniLabel);
            var cells = haboob.StartCells();
            if (cells != null)
                EditorGUILayout.LabelField($"덮는 칸: x {cells.Min(cell => cell.x)}~{cells.Max(cell => cell.x)}, y {cells.Min(cell => cell.y)}~{cells.Max(cell => cell.y)} (맵에 구름 색으로 표시)", EditorStyles.wordWrappedMiniLabel);
        }

        // 배치 도구로 누른 칸을 구름 가운데 근처로 하는 시작 위치입니다(설계 §26 Q-GP1 A): 누른 칸 − (⌊(가로−1)/2⌋, ⌊(세로−1)/2⌋).
        // shape는 Core 공개 계약이 검사한 모양이라 각 축 최솟값이 0이고, 가로 − 1 = 최대 X입니다. 누르기와 끌어 놓기 그림자가 함께 씁니다.
        private static Vector2Int SoHaboobAnchorAt(Vector2Int cell, List<Vector2Int> shape) =>
            new Vector2Int(cell.x - shape.Max(offset => offset.x) / 2, cell.y - shape.Max(offset => offset.y) / 2);

        // 하붑 시작 도구입니다(설계 §26 GP2). 한 번의 Undo로 하붑 사용·시작 위치 지정을 켜고 시작 위치를 정합니다.
        private void PlaceSoHaboobStart(Vector2Int cell, CombatStageSoGrid grid)
        {
            var shape = soDocument.haboob.ShapeOffsets();
            if (shape == null)
            {
                SetSoNotice("하붑 모양이 잘못돼 놓을 수 없습니다. '모양·세부 설정'에서 모양을 먼저 고치세요.", MessageType.Warning);
                return;
            }
            Vector2Int anchor = SoHaboobAnchorAt(cell, shape);
            // 판 밖으로 나가면 놓지 않습니다(설계 §26.6 Q-GP2). 저장 검사(SoProblems)와 같은 "모든 칸이 판 안" 판단입니다.
            if (shape.Any(offset => !grid.Contains(anchor + offset)))
            {
                SetSoNotice("구름이 판 밖으로 나가 놓을 수 없습니다. 판 안쪽 칸을 누르세요.", MessageType.Warning);
                return;
            }
            var haboob = soDocument.haboob;
            if (haboob.enabled && haboob.fixedInitialAnchor && haboob.initialAnchor == anchor) return;
            SoChange("하붑 시작 위치 놓기", () =>
            {
                haboob.enabled = true;
                haboob.fixedInitialAnchor = true;
                haboob.initialAnchor = anchor;
            });
        }

        // 시작 위치를 비워 무작위로 돌립니다(설계 §26.6 Q-GP3). 하붑 사용은 그대로 둡니다.
        private void ClearSoHaboobStart()
        {
            var haboob = soDocument.haboob;
            if (!haboob.fixedInitialAnchor) return;
            SoChange("하붑 시작 위치 비우기", () => haboob.fixedInitialAnchor = false);
        }

        // 하붑 시작 도구 줄입니다: 지금 상태, 비우기, 모양·세부 설정 바로가기(설계 §26.6 구현 보충).
        private void DrawSoHaboobToolBar()
        {
            var haboob = soDocument.haboob;
            using (new EditorGUILayout.HorizontalScope())
            {
                string start = haboob.fixedInitialAnchor ? $"({haboob.initialAnchor.x}, {haboob.initialAnchor.y})" : "무작위";
                GUILayout.Label($"하붑: {(haboob.enabled ? "켜짐" : "꺼짐")} · 시작 기준점: {start}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                using (new EditorGUI.DisabledScope(!haboob.fixedInitialAnchor))
                    if (GUILayout.Button("시작 위치 비우기(무작위로)", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) ClearSoHaboobStart();
                if (GUILayout.Button("모양·세부 설정", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) OpenSoGimmick(true);
                GUILayout.FlexibleSpace();
            }
        }

        // 기믹을 맵으로 끄는 동안 놓일 자리 그림자입니다(설계 §26.6 Q-GP1·Q-GP4 보충). 하붑은 놓일 구름 칸마다, 낙석은 마우스 아래 칸(어디든 켜짐)입니다.
        private void AddSoGimmickDropFrames(List<(Rect Cells, Color Color)> frames, SoTool tool, Vector2Int cell, CombatStageSoGrid grid)
        {
            var shape = tool == SoTool.HaboobStart ? soDocument.haboob.ShapeOffsets() : null;
            if (shape == null)
            {
                frames.Add((SoArea(new RectInt(cell, Vector2Int.one)), tool == SoTool.Rockfall ? GhostValid : GhostInvalid));
                return;
            }
            Vector2Int anchor = SoHaboobAnchorAt(cell, shape);
            bool valid = shape.All(offset => grid.Contains(anchor + offset));
            foreach (var offset in shape) frames.Add((SoArea(new RectInt(anchor + offset, Vector2Int.one)), valid ? GhostValid : GhostInvalid));
        }

        private void DrawSoHaboob(CombatStageSoDocument document)
        {
            var haboob = document.haboob;
            GUILayout.Space(6);
            DrawSoGimmickHeading("하붑", true);
            if (SoToggle("하붑 사용", haboob.enabled, out bool enabled))
                SoChange("하붑 사용 변경", () => haboob.enabled = enabled);
            DrawSoGimmickShape(true);
            DrawSoHaboobStart(haboob);
            _soHaboobDetails = EditorGUILayout.Foldout(_soHaboobDetails, "세부 설정", true);
            if (_soHaboobDetails)
            {
                using (new EditorGUI.DisabledScope(!haboob.enabled))
                {
                    if (SoInt("이동 주기(진영 종료 횟수)", haboob.relocationIntervalTurns, out int interval))
                        SoChange("하붑 이동 주기 변경", () => haboob.relocationIntervalTurns = interval);
                    if (SoInt("이동 전 경고(진영 종료 횟수)", haboob.warningLeadTurns, out int warning))
                        SoChange("하붑 경고 변경", () => haboob.warningLeadTurns = warning);
                }
            }
            _soHaboobCoordinates = EditorGUILayout.Foldout(_soHaboobCoordinates, "고급 좌표", true);
            if (_soHaboobCoordinates)
            {
                using (new EditorGUI.DisabledScope(!haboob.enabled)) DrawSoHaboobFootprint(haboob.footprintOffsets);
            }
            EditorGUILayout.HelpBox("하붑의 이동 위치는 전투 중 무작위로 정해지며 미리 보여 주지 않습니다. 시작 위치를 지정하지 않으면 시작 위치도 무작위입니다. 지정한 시작 위치만 맵에 구름 색으로 그립니다.", MessageType.Info);
            EditorGUILayout.LabelField("시작 위치를 지정하면 구름이 판 안에 들어가는지 저장 검사가 봅니다. 무작위일 때 모양이 필드에 들어가는지는 전투 진입으로 확인하세요. 경고 시간 설정은 별도 경고 이펙트를 추가하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
        }
    }
}
