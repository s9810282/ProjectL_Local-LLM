using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HaboobGimmickSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.HaboobGimmickSpec;
using RockfallGimmickSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RockfallGimmickSpec;

namespace ProjectL.Tool.Editor
{
    // 기믹과 격자 편집기의 조립입니다. 실제 위치·규칙·에셋 입출력은 기존 경로를 유지합니다.
    public sealed partial class CombatLayoutWindow
    {
        private CombatStageSoGimmickShapeEditor _soRockfallShape, _soHaboobShape;
        [System.NonSerialized] private CombatStageSoDocument _soShapeDocument;
        private bool _soRockfallDetails = true, _soHaboobDetails = true;
        private bool _soRockfallCoordinates, _soHaboobCoordinates;
        private int _soGimmickJump;

        private void EnsureSoGimmickShapes()
        {
            if (_soRockfallShape == null) _soRockfallShape = new CombatStageSoGimmickShapeEditor();
            if (_soHaboobShape == null) _soHaboobShape = new CombatStageSoGimmickShapeEditor();
            if (ReferenceEquals(_soShapeDocument, soDocument)) return;
            _soRockfallShape.ResetView(); _soHaboobShape.ResetView();
            _soShapeDocument = soDocument;
        }

        private void CancelSoGimmickShapes()
        {
            _soRockfallShape?.Cancel(); _soHaboobShape?.Cancel();
        }

        private void ResetSoGimmickShapeViews()
        {
            _soRockfallShape?.ResetView(); _soHaboobShape?.ResetView();
        }

        // 머리줄/다른 명령이 미완료 획을 통과해 처리되지 않게 합니다.
        private void HandleSoGimmickShapeCancellation()
        {
            if (_soRockfallShape?.IsInteracting != true && _soHaboobShape?.IsInteracting != true) return;
            Event e = Event.current;
            bool escape = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;
            bool command = e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand;
            bool undoKey = e.type == EventType.KeyDown && (e.control || e.command) && (e.keyCode == KeyCode.Z || e.keyCode == KeyCode.Y);
            if (!escape && !command && !undoKey && e.type != EventType.Ignore && e.type != EventType.ScrollWheel) return;
            CancelSoGimmickShapes();
            if (escape) e.Use();
            Repaint();
        }

        private void OnLostFocus()
        {
            CancelSoGimmickShapes(); Repaint();
        }

        private void DrawSoGimmickShape(bool haboob)
        {
            EnsureSoGimmickShapes();
            CombatStageSoGimmickShapeEditor editor = haboob ? _soHaboobShape : _soRockfallShape;
            List<Vector2Int> cells = haboob ? soDocument.haboob.footprintOffsets : soDocument.rockfall.footprintOffsets;
            bool enabled = haboob ? soDocument.haboob.enabled : soDocument.rockfall.enabled;
            string name = haboob ? "하붑" : "낙석";
            EditorGUILayout.LabelField(haboob ? "구름 영역의 모양" : "돌 하나의 모양", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("위치: 전투 중 자동 결정", EditorStyles.miniLabel);
            if (editor.Draw(cells, enabled, out List<Vector2Int> changed))
            {
                ApplySoGimmickShape(haboob, changed, name + " 모양 그리기");
                GUIUtility.ExitGUI();
            }
            if (Event.current.type == EventType.Used || editor.IsInteracting) Repaint();
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button("기본 모양"))
                {
                    var defaults = haboob ? new HaboobGimmickSpec().FootprintOffsets : new RockfallGimmickSpec().FootprintOffsets;
                    ApplySoGimmickShape(haboob, defaults.Select(c => new Vector2Int(c.X, c.Y)).ToList(), name + " 기본 모양 복원");
                    editor.ResetView(); Repaint(); GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.LabelField("그린 모양은 상대 좌표에 자동으로 맞춥니다. 기본 모양은 주기·피해 등 다른 값을 바꾸지 않습니다.", EditorStyles.wordWrappedMiniLabel);
            if (!haboob) EditorGUILayout.LabelField("돌 수 최소/최대는 이 모양을 가진 돌의 수입니다. 모양의 칸 수와 다릅니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void ApplySoGimmickShape(bool haboob, List<Vector2Int> cells, string label)
        {
            CancelSoGimmickShapes();
            List<Vector2Int> before = haboob ? soDocument.haboob.footprintOffsets : soDocument.rockfall.footprintOffsets;
            if (before.SequenceEqual(cells)) return;
            Undo.IncrementCurrentGroup();
            SoChange(label, () =>
            {
                if (haboob) soDocument.haboob.footprintOffsets = cells;
                else soDocument.rockfall.footprintOffsets = cells;
            });
        }

        private void DrawSoGimmickLinks()
        {
            GUILayout.Space(6);
            // 기믹 배치 항목입니다(설계 §26.6). 누르면 그 도구가 되고, 정의 목록처럼 맵으로 끌어 놓을 수 있습니다.
            EditorGUILayout.LabelField("기믹 배치", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("누르면 그 도구가 되고, 맵으로 끌어 놓아도 됩니다.", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!soDocument.IsOpen))
            {
                DrawSoGimmickPaletteItem("하붑 시작 위치", SoTool.HaboobStart, SoHaboobPaletteKey);
                DrawSoGimmickPaletteItem("낙석 켜기", SoTool.Rockfall, SoRockfallPaletteKey);
            }
            GUILayout.Space(4);
            EditorGUILayout.LabelField("기믹 설정", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!soDocument.IsOpen))
            {
                if (GUILayout.Button("낙석 모양·설정")) OpenSoGimmick(false);
                if (GUILayout.Button("하붑 모양·설정")) OpenSoGimmick(true);
            }
        }

        // 끌기 입력을 가리는 열쇠입니다. 정의 GUID와 겹치지 않는 글자입니다.
        private const string SoHaboobPaletteKey = "gimmick:haboob", SoRockfallPaletteKey = "gimmick:rockfall";

        private static bool IsSoGimmickTool(SoTool tool) => tool == SoTool.HaboobStart || tool == SoTool.Rockfall;

        // 정의 목록 항목과 같은 모양·입력입니다(DrawSoPalette). 끌기는 HandleSoPaletteDrag → DropSoPalette → HandleSoCell 한 길입니다.
        private void DrawSoGimmickPaletteItem(string label, SoTool tool, string key)
        {
            Color previous = GUI.backgroundColor;
            if (soTool == tool) GUI.backgroundColor = SelectedButton;
            var content = new GUIContent(label);
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.miniButton);
            // 버튼이 끌기 입력을 삼키므로 버튼보다 먼저 봅니다. 비활성(스테이지 없음)이면 끌기를 시작하지 않습니다.
            if (GUI.enabled) HandleSoPaletteDrag(rect, tool, key, label);
            bool clicked = GUI.Button(rect, content, EditorStyles.miniButton);
            GUI.backgroundColor = previous;
            if (!clicked) return;
            EndStroke();
            soTool = tool;
            ShowSoPlacementProperties();
        }

        // 낙석 도구입니다(설계 §26 GP4, Q-GP4 A). 위치는 Core가 무작위로 고르므로 누른 칸과 상관없이 켜기만 합니다.
        private void EnableSoRockfall()
        {
            if (soDocument.rockfall.enabled)
            {
                SetSoNotice("낙석은 이미 켜져 있습니다. 위치는 전투 중 무작위로 정해집니다.", MessageType.Info);
                return;
            }
            SoChange("낙석 켜기", () => soDocument.rockfall.enabled = true);
        }

        // 낙석 도구 줄입니다: 지금 상태, 끄기, 모양·세부 설정 바로가기(설계 §26.6 구현 보충).
        private void DrawSoRockfallToolBar()
        {
            bool enabled = soDocument.rockfall.enabled;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"낙석: {(enabled ? "켜짐" : "꺼짐")}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                using (new EditorGUI.DisabledScope(!enabled))
                    if (GUILayout.Button("낙석 끄기", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) SoChange("낙석 끄기", () => soDocument.rockfall.enabled = false);
                if (GUILayout.Button("모양·세부 설정", EditorStyles.miniButton, GUILayout.ExpandWidth(false))) OpenSoGimmick(false);
                GUILayout.FlexibleSpace();
            }
        }

        private void OpenSoGimmick(bool haboob)
        {
            OpenSoInspector(SoInspectorSection.Gimmicks);
            _soGimmickJump = haboob ? 2 : 1;
            GUIUtility.ExitGUI();
        }

        private void DrawSoGimmickHeading(string label, bool haboob)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            if (_soGimmickJump != (haboob ? 2 : 1) || Event.current.type != EventType.Repaint) return;
            soDetailsScroll.y = Mathf.Max(0, GUILayoutUtility.GetLastRect().y - 4);
            _soGimmickJump = 0;
            Repaint();
        }
    }
}
