using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private ScriptableObject combatPreviewViewStage;
        private FbxCombatStageContext _combatStageContext;
        private GameObject _combatContextMapPrefab;
        private FbxCombatCameraSource.Frame? _combatCameraFrame;
        private string _combatCameraProblem;
        private string _combatPreviewSourceProblem;
        private static Type _viewStageAssetType;
        private string _combatStageFingerprint;
        private string _combatCameraFingerprint;

        // ViewStage는 미리보기 맥락일 뿐입니다. 이 UI는 작업실 맵·SO·씬을 수정하지 않습니다.
        private void DrawCombatPreviewContext()
        {
            GUILayout.Space(8);
            GUILayout.Label("전투 구도 맥락", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            combatPreviewViewStage = (ScriptableObject)EditorGUILayout.ObjectField(
                "전투 ViewStage", combatPreviewViewStage, ViewStageAssetType(), false);
            EditorGUILayout.LabelField("StageViewProp 폴더의 운영 ViewStage 에셋을 고르세요.", EditorStyles.wordWrappedMiniLabel);
            GameObject openPrefab = OpenedCombatPreviewPrefab();
            if (EditorGUI.EndChangeCheck() || _combatStageContext == null || _combatContextMapPrefab != openPrefab)
                RefreshCombatStageContext();

            if (_combatStageContext.IsReady)
            {
                EditorGUILayout.LabelField("전투 스테이지", $"{_combatStageContext.StageKey} · {_combatStageContext.Mode}");
                EditorGUILayout.LabelField("StageRoot", _combatStageContext.RootName);
                EditorGUILayout.LabelField("필드", $"{_combatStageContext.Field.Columns} × {_combatStageContext.Field.Rows}");
                EditorGUILayout.LabelField("칸 간격", $"{_combatStageContext.FieldView.CellWidth:0.###} × {_combatStageContext.FieldView.CellLength:0.###}");
            }
            else
            {
                EditorGUILayout.HelpBox(_combatStageContext.Problem, MessageType.Warning);
            }

            if (_combatStageContext.IsReady && _combatPreviewView != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("선택 스테이지 배경 보정", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Vector3 position = EditorGUILayout.Vector3Field("위치", _combatPreviewView.positionOffset);
                Vector3 rotation = EditorGUILayout.Vector3Field("회전", _combatPreviewView.rotationOffset);
                bool tiles = _combatStageContext.Mode == "PrefabWithTiles";
                Vector3 scale = _combatPreviewView.scaleOffset;
                if (tiles) CombatMapScaleEditor.Draw(openPrefab, _combatStageContext.FieldView, _combatPreviewView.tiles);
                else scale = EditorGUILayout.Vector3Field("배율", scale);
                Vector3 tileOffset = _combatPreviewView.tiles.offset;
                Vector2Int anchor = _combatPreviewView.tiles.anchorTile;
                if (tiles)
                {
                    EditorGUILayout.LabelField("타일 방식의 배경 위치 X/Z는 기준 타일 정렬로 덮입니다.", EditorStyles.wordWrappedMiniLabel);
                    tileOffset = EditorGUILayout.Vector3Field("타일 오프셋", tileOffset);
                    anchor = EditorGUILayout.Vector2IntField("기준 타일", anchor);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    try
                    {
                        CombatStageSoAssets.WriteStagePreviewOffsets(combatPreviewViewStage, position, rotation, scale,
                            tileOffset, anchor, tiles);
                        RefreshCombatStageContext();
                    }
                    catch (Exception exception) when (!(exception is ExitGUIException))
                    {
                        _combatPreviewSourceProblem = exception.Message;
                    }
                }
            }

            if (GUILayout.Button("스테이지·미리보기 다시 읽기"))
                RefreshCombatStageContext();
            if (GUILayout.Button("저장된 Combat 씬 카메라 읽기"))
                DeferEditorAction(RefreshCombatCameraSource);
            if (_combatCameraFrame is FbxCombatCameraSource.Frame frame)
            {
                EditorGUILayout.LabelField("가상 카메라", frame.Name);
                EditorGUILayout.LabelField("기준 월드 위치", frame.Position.ToString("F3"));
                EditorGUILayout.LabelField("기준 월드 회전", frame.Rotation.eulerAngles.ToString("F1"));
                EditorGUILayout.LabelField("저장된 씬 직교 크기", frame.OrthographicSize.ToString("0.###"));
                EditorGUILayout.LabelField("Zoom Pose", frame.UseZoomPose ? $"사용 · 시작 크기 {frame.ZoomMax:0.###}" : "사용 안 함");
                EditorGUILayout.LabelField("시작 월드 위치", frame.StartPosition.ToString("F3"));
                EditorGUILayout.LabelField("시작 월드 회전", frame.StartRotation.eulerAngles.ToString("F1"));
                EditorGUILayout.LabelField("오른쪽 위 전투 구도에 시작 자세를 적용합니다.", EditorStyles.wordWrappedMiniLabel);
            }
            else if (!string.IsNullOrEmpty(_combatCameraProblem))
            {
                EditorGUILayout.HelpBox(_combatCameraProblem, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("저장된 Combat 씬의 시점은 버튼으로 읽습니다.", EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.LabelField("프랍 이동·Undo는 전투 구도에 자동 반영됩니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("미리보기는 16:9입니다. Game View 비율이 다르면 화면 경계가 달라집니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("조명·베이크·후처리와 무작위 타일 외형은 Play 결과의 참고 표시입니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("프랍은 '맵 SO 저장', 배경 보정 SO는 프로젝트 저장으로 확정합니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void RefreshCombatStageContext()
        {
            GameObject openPrefab = OpenedCombatPreviewPrefab();
            _combatContextMapPrefab = openPrefab;
            _combatStageContext = FbxCombatStageContext.Read(combatPreviewViewStage, openPrefab);
            _combatPreviewView = null;
            _combatPreviewSourceProblem = null;
            if (_combatStageContext.IsReady)
            {
                try { _combatPreviewView = CombatStageSoAssets.ReadStagePreviewView(combatPreviewViewStage); }
                catch (Exception exception) when (!(exception is ExitGUIException))
                {
                    _combatPreviewSourceProblem = exception.Message;
                }
            }
            _combatPreviewNeedsRebuild = true;
            _combatPreviewFailure = null;
            _combatStageFingerprint = CombatStageFingerprint();
        }

        private GameObject OpenedCombatPreviewPrefab() =>
            !string.IsNullOrEmpty(savePath) ? AssetDatabase.LoadAssetAtPath<GameObject>(savePath) : null;

        private static Type ViewStageAssetType()
        {
            if (_viewStageAssetType != null)
                return _viewStageAssetType;
            foreach (Type candidate in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (candidate.Name == CombatStageSoAssets.ViewType)
                    return _viewStageAssetType = candidate;
            }
            return typeof(ScriptableObject);
        }

        private void RefreshCombatCameraSource()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || FbxPlacementBaker.IsRunning)
                return;
            _combatCameraFrame = null;
            _combatCameraProblem = null;
            _combatPreviewFailure = null;
            try
            {
                _combatCameraFrame = FbxCombatCameraSource.Read();
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                _combatCameraProblem = exception.Message;
            }
            _combatCameraFingerprint = CombatCameraFingerprint();
            _combatRenderedWidth = 0;
            Repaint();
        }

        private void OnCombatPreviewProjectChanged()
        {
            if (this == null) return;
            string stage = CombatStageFingerprint();
            if (_combatStageFingerprint != stage)
                RefreshCombatStageContext();
            string camera = CombatCameraFingerprint();
            if (_combatCameraFingerprint == camera) return;
            _combatCameraFingerprint = camera;
            _combatCameraFrame = null;
            _combatCameraProblem = null;
            _combatRenderedWidth = 0;
        }

        private string CombatStageFingerprint()
        {
            var result = new StringBuilder();
            AppendAssetHash(result, AssetDatabase.GetAssetPath(combatPreviewViewStage));
            AppendAssetHash(result, savePath);
            AppendTableHashes(result, CombatStageSoAssets.ViewTableType);
            AppendTableHashes(result, CombatStageSoAssets.RootTableType);
            AppendTableHashes(result, CombatStageSoAssets.FieldViewTableType);
            return result.ToString();
        }

        private static void AppendTableHashes(StringBuilder result, string typeName)
        {
            foreach (UnityEngine.Object table in CombatStageSoAssets.FindAll(typeName))
                AppendAssetHash(result, AssetDatabase.GetAssetPath(table));
        }

        private static void AppendAssetHash(StringBuilder result, string path)
        {
            result.Append(path).Append(':');
            if (!string.IsNullOrEmpty(path)) result.Append(AssetDatabase.GetAssetDependencyHash(path));
            result.Append('|');
        }

        private string CombatCameraFingerprint()
        {
            var result = new StringBuilder();
            AppendAssetHash(result, "Assets/Scenes/Combat.unity");
            if (_combatCameraFrame is FbxCombatCameraSource.Frame frame)
                AppendAssetHash(result, frame.SettingsPath);
            return result.ToString();
        }
    }
}
