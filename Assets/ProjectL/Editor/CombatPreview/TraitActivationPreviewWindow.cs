using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>특성 연출 입력·프리셋·실행만 담당하는 에디터 창입니다.</summary>
    public sealed class TraitActivationPreviewWindow : EditorWindow
    {
        [SerializeField]
        private TraitActivationPreviewInput input = new();
        [SerializeField]
        private TraitActivationPreviewPreset preset;
        [SerializeField]
        private bool scenarioMode;
        [SerializeField]
        private TraitPreviewScenarioInput scenario = new();
        [SerializeField]
        private TraitPreviewScenarioPreset scenarioPreset;
        private readonly PreviewScenarioEditor _scenarioEditor = new();
        private string _validation = "";
        private Vector2 _scroll;
        private string _search = "";
        private bool _showMissing;
        [MenuItem("ProjectL/Combat/Trait Activation Preview")]
        public static void Open() => GetWindow<TraitActivationPreviewWindow>("Trait Activation");
        private void OnEnable()
        {
            EditorApplication.update += UpdateSession;
            EditorApplication.projectChanged += AssetChanged;
            Undo.postprocessModifications += OnModified;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateSession;
            EditorApplication.projectChanged -= AssetChanged;
            Undo.postprocessModifications -= OnModified;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnDestroy()
        {
            // Play Maximized 진입 시 Unity가 편집 레이아웃의 창을 임시 파괴합니다. 사용자 닫기로 처리하지 않습니다.
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            PreviewPlayStartScene.CloseAfterLayout();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("연출 확인 / 합성 결과 — 실제 발동 조건·피해 계산 검증은 별도 전투 환경에서 합니다.", MessageType.Info);
            if (!string.IsNullOrEmpty(PreviewPlayStartScene.Error))
                EditorGUILayout.HelpBox(PreviewPlayStartScene.Error, MessageType.Error);
            TraitActivationPreviewHost host = TraitActivationPreviewBridge.Host;
            EditorGUILayout.LabelField(host != null ? host.Status : "테스트 환경 대기", EditorStyles.wordWrappedLabel);
            if (host != null && !host.HasMapCamera)
                EditorGUILayout.HelpBox("맵 카메라가 연결되지 않아 줌·흔들림 검증은 지원하지 않습니다.", MessageType.Warning);
            bool nextMode = GUILayout.Toolbar(scenarioMode ? 1 : 0, new[] { "간단 재생", "조립 재생" }) == 1;
            if (nextMode != scenarioMode)
            {
                host?.Stop();
                scenarioMode = nextMode;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (scenarioMode)
                DrawScenario(host);
            else
            {
                EditorGUI.BeginChangeCheck();
                string[] simplePresets = AssetDatabase.FindAssets("t:TraitActivationPreviewPreset").Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
                int quickSimple = EditorGUILayout.Popup("프리셋 빠른 불러오기", 0, new[] { "선택하세요" }.Concat(simplePresets.Select(System.IO.Path.GetFileNameWithoutExtension)).ToArray());
                if (quickSimple > 0)
                {
                    Undo.RecordObject(this, "간단 프리셋 불러오기");
                    preset = AssetDatabase.LoadAssetAtPath<TraitActivationPreviewPreset>(simplePresets[quickSimple - 1]);
                    input = preset.input.Copy();
                }

                _search = EditorGUILayout.TextField("특성 검색", _search);
                string[] paths = TraitActivationPreviewBridge.RegisteredTraitPaths().Where(p => p.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(p => p).ToArray();
                string[] names = new[]
                {
                    "특성 선택"
                }.Concat(paths.Select(System.IO.Path.GetFileNameWithoutExtension)).ToArray();
                int selected = Array.IndexOf(paths, AssetDatabase.GetAssetPath(input.traitAsset)) + 1;
                int next = EditorGUILayout.Popup("특성", selected, names);
                if (next != selected)
                    input.traitAsset = next == 0 ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(paths[next - 1]);
                if (GUILayout.Button("원본 SO Inspector 열기") && input.traitAsset != null)
                {
                    Selection.activeObject = input.traitAsset;
                    EditorGUIUtility.PingObject(input.traitAsset);
                }

                _showMissing = EditorGUILayout.Toggle("줄 없음도 확인", _showMissing);
                int[] kinds = TraitActivationPreviewBridge.Kinds(input.traitAsset);
                if (_showMissing)
                    input.kind = (TraitActivationKind)EditorGUILayout.EnumPopup("Activation Kind", input.kind);
                else if (kinds.Length > 0)
                {
                    int index = Math.Max(0, Array.IndexOf(kinds, (int)input.kind));
                    index = EditorGUILayout.Popup("Activation Kind", index, kinds.Select(k => ((TraitActivationKind)k).ToString()).ToArray());
                    input.kind = (TraitActivationKind)kinds[index];
                }
                else
                    EditorGUILayout.HelpBox("이 에셋에 Activation 줄이 없습니다. 줄 없음 확인을 켜거나 원본 SO에 줄을 작성하세요.", MessageType.Info);
                var serialized = new SerializedObject(this);
                serialized.Update();
                SerializedProperty data = serialized.FindProperty("input");
                foreach (string name in new[]
                {
                    "context",
                    "stageViewKey",
                    "units",
                    "sourceIndex",
                    "targetIndices",
                    "cells",
                    "magnitude"
                }

                )
                    EditorGUILayout.PropertyField(data.FindPropertyRelative(name), true);
                if (input.context != TraitActivationPreviewContext.Moment)
                    foreach (string name in new[]
                    {
                        "attackerIndex",
                        "direction",
                        "targetCell",
                        "damage",
                        "kill"
                    }

                    )
                        EditorGUILayout.PropertyField(data.FindPropertyRelative(name), true);
                foreach (string name in new[]
                {
                    "repeat",
                    "observationSeconds",
                    "repeatInterval"
                }

                )
                    EditorGUILayout.PropertyField(data.FindPropertyRelative(name), true);
                serialized.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                    host?.Stop();
                EditorGUILayout.HelpBox(TraitActivationPreviewBridge.Describe(input), MessageType.None);
                if (host != null)
                    EditorGUILayout.LabelField($"Scope 발급 {host.ScopeAcquisitionCount}회 / 재생 완료 {host.CompletedCount}회");
                EditorGUILayout.Space();
                preset = (TraitActivationPreviewPreset)EditorGUILayout.ObjectField("프리셋", preset, typeof(TraitActivationPreviewPreset), false);
                if (GUILayout.Button("프리셋 불러오기") && preset != null)
                {
                    host?.Stop();
                    input = preset.input.Copy();
                }

                if (GUILayout.Button("현재 설정을 새 프리셋으로 저장"))
                    SavePreset();
            }

            if (host != null && !string.IsNullOrEmpty(host.Diagnostic))
                EditorGUILayout.HelpBox(host.Diagnostic, MessageType.None);
            EditorGUILayout.EndScrollView();
            if (host != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(host.BoundaryPause ? "자동 계속" : "묶음 경계에서 멈춤"))
                        host.SetBoundaryPause(!host.BoundaryPause);
                    using (new EditorGUI.DisabledScope(!host.CanAdvance))
                        if (GUILayout.Button("다음 진행"))
                            Run(host.Advance);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("환경 열기 / 재생"))
                    Run(Play);
                if (GUILayout.Button("중단·초기화"))
                    Run(() => host?.Stop());
                if (GUILayout.Button("환경 종료"))
                {
                    PreviewPlayStartScene.CancelPending();
                    Run(() => host?.Shutdown());
                    if (PreviewPlayStartScene.IsActive)
                        EditorApplication.isPlaying = false;
                }
            }
        }

        private void Play()
        {
            TraitActivationPreviewBridge.ValidateScenario(scenarioMode ? scenario : TraitPreviewScenarioImport.FromSimple(input));
            if (EditorApplication.isPlaying)
            {
                if (TraitActivationPreviewBridge.Host == null)
                    throw new InvalidOperationException("현재 Play는 테스트 전용 씬이 아닙니다. Play를 종료하고 환경을 여세요.");
                if (scenarioMode)
                    TraitActivationPreviewBridge.Host.Play(scenario);
                else
                    TraitActivationPreviewBridge.Host.Play(input);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            TraitActivationPreviewSceneBuilder.EnsureScene();
            if (scenarioMode)
                PreviewPlayStartScene.BeginPlay(scenario, TraitActivationPreviewSceneBuilder.ScenePath);
            else
                PreviewPlayStartScene.BeginPlay(input, TraitActivationPreviewSceneBuilder.ScenePath);
        }

        private void DrawScenario(TraitActivationPreviewHost host)
        {
            EditorGUILayout.HelpBox("이동/공격은 조건을 자동 발동시키지 않습니다. 상태 변경과 Activation을 함께 조립하세요. 상태 편집은 출처별 효과·가속·재생 진행을 지원하며 HP/귀환은 명시 블록입니다.", MessageType.Info);
            string[] paths = AssetDatabase.FindAssets("t:TraitPreviewScenarioPreset").Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            int quick = EditorGUILayout.Popup("프리셋 빠른 불러오기", 0, new[] { "선택하세요" }.Concat(paths.Select(System.IO.Path.GetFileNameWithoutExtension)).ToArray());
            if (quick > 0)
            {
                host?.Stop();
                Undo.RecordObject(this, "조립 프리셋 불러오기");
                scenarioPreset = AssetDatabase.LoadAssetAtPath<TraitPreviewScenarioPreset>(paths[quick - 1]);
                scenario = TraitPreviewScenarioImport.Upgrade(scenarioPreset.input);
                _validation = "";
            }

            var serialized = new SerializedObject(this);
            serialized.Update();
            _scenarioEditor.Draw(serialized, scenario);
            if (serialized.ApplyModifiedProperties())
            {
                host?.Stop();
                _validation = "";
            }

            if (GUILayout.Button("구성 확인"))
            {
                PreviewPlayStartScene.ClearError();
                try
                {
                    _validation = TraitActivationPreviewBridge.ValidateScenario(scenario);
                }
                catch (Exception error)
                {
                    _validation = "구성 오류: " + error.Message;
                }
            }

            if (_validation.Length > 0)
                EditorGUILayout.HelpBox(_validation, MessageType.None);
            EditorGUILayout.LabelField($"Scope 발급 {host?.ScopeAcquisitionCount ?? 0}회 / 전체 완료 {host?.CompletedCount ?? 0}회");
            scenarioPreset = (TraitPreviewScenarioPreset)EditorGUILayout.ObjectField("조립 프리셋", scenarioPreset, typeof(TraitPreviewScenarioPreset), false);
            if (GUILayout.Button("조립 프리셋 불러오기") && scenarioPreset != null)
            {
                host?.Stop();
                Undo.RecordObject(this, "조립 프리셋 불러오기");
                scenario = TraitPreviewScenarioImport.Upgrade(scenarioPreset.input);
                _validation = "";
            }

            if (GUILayout.Button("조립 프리셋으로 저장"))
                SaveScenario();
            if (GUILayout.Button("간단 입력을 조립으로 가져오기"))
            {
                host?.Stop();
                Undo.RecordObject(this, "간단 입력 가져오기");
                scenario = TraitPreviewScenarioImport.FromSimple(input);
                _validation = "";
            }
        }

        private void SaveScenario()
        {
            string path = EditorUtility.SaveFilePanelInProject("조립 입력 저장", "TraitScenario", "asset", "공통 블록과 입력만 저장합니다.");
            if (string.IsNullOrEmpty(path))
                return;
            var saved = CreateInstance<TraitPreviewScenarioPreset>();
            saved.input = TraitPreviewScenarioImport.Upgrade(scenario);
            AssetDatabase.CreateAsset(saved, path);
            AssetDatabase.SaveAssetIfDirty(saved);
            scenarioPreset = saved;
        }

        private void UpdateSession()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void SavePreset()
        {
            string path = EditorUtility.SaveFilePanelInProject("특성 연출 입력 저장", "TraitPreview", "asset", "입력만 저장합니다.");
            if (string.IsNullOrEmpty(path))
                return;
            var saved = CreateInstance<TraitActivationPreviewPreset>();
            saved.input = input.Copy();
            AssetDatabase.CreateAsset(saved, path);
            AssetDatabase.SaveAssets();
            preset = saved;
        }

        private void OnUndoRedo()
        {
            _validation = "";
            AssetChanged();
            Repaint();
        }

        private void AssetChanged() => Run(() => TraitActivationPreviewBridge.Host?.Stop());
        private UndoPropertyModification[] OnModified(UndoPropertyModification[] modifications)
        {
            if (modifications.Any(m => m.currentValue.target is ScriptableObject && m.currentValue.target != this))
                AssetChanged();
            return modifications;
        }

        private void Run(Action action)
        {
            try
            {
                PreviewPlayStartScene.ClearError();
                action();
            }
            catch (Exception error)
            {
                PreviewPlayStartScene.ReportError(error);
            }
        }
    }
}
