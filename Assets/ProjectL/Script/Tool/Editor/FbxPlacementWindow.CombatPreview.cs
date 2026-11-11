using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        private FbxCombatStagePreview _combatPreview;
        private CombatStageSoView _combatPreviewView;
        private VisualElement _combatPreviewPanel;
        private Image _combatPreviewImage;
        private Label _combatPreviewTitle;
        private Label _combatPreviewMessage;
        private GameObject _combatPreviewSource;
        private bool _combatPreviewNeedsRebuild;
        private string _combatPreviewFailure;
        private double _nextCombatPreviewTime;
        private int _combatRenderedWidth;
        private int _combatRenderedHeight;

        private void BuildCombatPreviewPanel()
        {
            RemoveCombatPreviewPanel();
            _combatPreviewPanel = new VisualElement { name = "fbx-combat-preview", pickingMode = PickingMode.Ignore };
            _combatPreviewPanel.style.position = Position.Absolute;
            _combatPreviewPanel.style.top = PlacementToolbarHeight + 12;
            _combatPreviewPanel.style.right = 12;
            _combatPreviewPanel.style.width = 420;
            _combatPreviewPanel.style.height = 290;
            _combatPreviewPanel.style.paddingLeft = 7;
            _combatPreviewPanel.style.paddingRight = 7;
            _combatPreviewPanel.style.paddingTop = 6;
            _combatPreviewPanel.style.paddingBottom = 7;
            _combatPreviewPanel.style.backgroundColor = new Color(.12f, .12f, .12f, 1);
            _combatPreviewTitle = new Label("전투 공용 구도") { pickingMode = PickingMode.Ignore };
            _combatPreviewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _combatPreviewTitle.style.height = 22;
            _combatPreviewImage = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _combatPreviewImage.style.flexGrow = 1;
            _combatPreviewImage.style.minHeight = 0;
            _combatPreviewImage.style.backgroundColor = new Color(.08f, .08f, .08f, 1);
            _combatPreviewMessage = new Label { pickingMode = PickingMode.Ignore };
            _combatPreviewMessage.style.whiteSpace = WhiteSpace.Normal;
            _combatPreviewPanel.Add(_combatPreviewTitle);
            _combatPreviewPanel.Add(_combatPreviewImage);
            _combatPreviewPanel.Add(_combatPreviewMessage);
            rootVisualElement.Add(_combatPreviewPanel);
        }

        private void RemoveCombatPreviewPanel()
        {
            _combatPreviewPanel?.RemoveFromHierarchy();
            _combatPreviewPanel = null;
            _combatPreviewImage = null;
            _combatPreviewTitle = null;
            _combatPreviewMessage = null;
        }

        private void OnCombatPreviewPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode) return;
            ReleaseCombatPreview();
            _combatCameraFrame = null;
            _combatCameraProblem = null;
            _combatCameraFingerprint = null;
        }

        private void UpdateCombatPreview()
        {
            if (_combatPreviewPanel == null || EditorApplication.timeSinceStartup < _nextCombatPreviewTime) return;
            _nextCombatPreviewTime = EditorApplication.timeSinceStartup + .1;
            bool available = !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
                && !FbxPlacementBaker.IsRunning && FbxPlacementWorkspace.IsWorkspaceRoot(workspaceRoot);
            if (!available)
            {
                ReleaseCombatPreview();
                _combatPreviewPanel.style.display = DisplayStyle.None;
                return;
            }
            _combatPreviewPanel.style.display = DisplayStyle.Flex;
            if (_combatStageContext == null || _combatContextMapPrefab != OpenedCombatPreviewPrefab())
                RefreshCombatStageContext();
            _combatPreviewTitle.text = _combatStageContext.IsReady
                ? $"전투 공용 구도 · Stage {_combatStageContext.StageKey}"
                : "전투 공용 구도";
            if (!_combatStageContext.IsReady || _combatPreviewView == null)
            {
                ReleaseCombatPreview();
                SetCombatPreviewMessage(_combatStageContext.Problem ?? _combatPreviewSourceProblem ?? "전투 ViewStage를 다시 읽으세요.");
                return;
            }
            if (_combatCameraFrame == null)
            {
                ReleaseCombatPreview();
                if (_combatCameraProblem == null && _pendingEditorAction == null)
                    DeferEditorAction(RefreshCombatCameraSource);
                SetCombatPreviewMessage(_combatCameraProblem ?? "저장된 Combat 씬 카메라를 읽고 있습니다.");
                return;
            }
            if (_combatPreviewFailure != null && _combatPreviewSource == workspaceRoot)
            {
                SetCombatPreviewMessage(_combatPreviewFailure);
                return;
            }
            try
            {
                _combatPreview ??= new FbxCombatStagePreview();
                bool changed;
                if (_combatPreviewNeedsRebuild || _combatPreviewSource != workspaceRoot)
                {
                    _combatPreview.Rebuild(workspaceRoot, _combatPreviewView, _combatStageContext);
                    changed = true;
                }
                else changed = _combatPreview.Sync(workspaceRoot, _combatPreviewView, _combatStageContext);
                _combatPreviewNeedsRebuild = false;
                _combatPreviewSource = workspaceRoot;
                Rect rect = _combatPreviewImage.contentRect;
                float pixels = EditorGUIUtility.pixelsPerPoint;
                float availableWidth = rect.width > 0f ? rect.width : 384f;
                float availableHeight = rect.height > 0f ? rect.height : 216f;
                int width = Mathf.RoundToInt(Mathf.Max(64f, Mathf.Min(availableWidth, availableHeight * 16f / 9f) * pixels));
                int height = Mathf.RoundToInt(width * 9f / 16f);
                if (changed || _combatRenderedWidth != width || _combatRenderedHeight != height || _combatPreviewImage.image == null)
                {
                    _combatPreviewImage.image = _combatPreview.Render(_combatCameraFrame.Value, width, height);
                    _combatRenderedWidth = width;
                    _combatRenderedHeight = height;
                    _combatPreviewPanel.MarkDirtyRepaint();
                }
                _combatPreviewMessage.text = $"16:9 프레임 {width}×{height} · 조명·무작위 타일 참고용";
                _combatPreviewFailure = null;
            }
            catch (Exception exception)
            {
                ReleaseCombatPreview();
                _combatPreviewSource = workspaceRoot;
                _combatPreviewFailure = "전투 미리보기 오류: " + exception.Message;
                SetCombatPreviewMessage(_combatPreviewFailure);
            }
        }

        private void SetCombatPreviewMessage(string message)
        {
            _combatPreviewImage.image = null;
            _combatPreviewMessage.text = message;
            _combatPreviewPanel.MarkDirtyRepaint();
        }

        private void ReleaseCombatPreview()
        {
            _combatPreview?.Dispose();
            _combatPreview = null;
            _combatPreviewSource = null;
            _combatRenderedWidth = _combatRenderedHeight = 0;
            if (_combatPreviewImage != null) _combatPreviewImage.image = null;
        }
    }
}
