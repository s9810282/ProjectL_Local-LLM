using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showStaticSettings = true;
        [SerializeField] private bool staticIncludeChildren = true;

        private void DrawStaticSettings()
        {
            List<Transform> selection = GetWorkspaceSelection();
            if (selection.Count == 0) return;
            GUILayout.Space(8);
            showStaticSettings = EditorGUILayout.Foldout(showStaticSettings, "정적 오브젝트 설정", true, EditorStyles.foldoutHeader);
            if (!showStaticSettings) return;
            staticIncludeChildren = EditorGUILayout.ToggleLeft(new GUIContent("자식 오브젝트에도 적용", "FBX 내부 메시와 비활성 자식까지 함께 변경합니다."), staticIncludeChildren);
            List<GameObject> targets = FbxPlacementStaticSettings.GetTargets(selection, staticIncludeChildren);
            EditorGUILayout.LabelField("적용 범위: " + targets.Count + "개 오브젝트", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모두 켜기"))
                {
                    FbxPlacementStaticSettings.SetAll(targets, true);
                    Changed();
                }
                if (GUILayout.Button("모두 끄기"))
                {
                    FbxPlacementStaticSettings.SetAll(targets, false);
                    Changed();
                }
            }
            DrawStaticFlag(targets, StaticEditorFlags.ContributeGI, "전역 조명 참여 (베이크 대상)", "고정된 배경의 조명을 베이크합니다. 켜면 메시가 라이트맵을 받도록 설정합니다.");
            DrawStaticFlag(targets, StaticEditorFlags.OccluderStatic, "다른 물체를 가리는 가림막", "가림막 컬링 계산에서 다른 물체를 가리는 오브젝트로 사용합니다.");
            DrawStaticFlag(targets, StaticEditorFlags.OccludeeStatic, "가림막 뒤에서 숨길 대상", "가림막 컬링 계산에서 가려졌을 때 표시를 생략할 수 있는 오브젝트입니다.");
            bool hasGi = false;
            foreach (GameObject target in targets)
                hasGi |= (GameObjectUtility.GetStaticEditorFlags(target) & StaticEditorFlags.ContributeGI) != 0;
            using (new EditorGUI.DisabledScope(hasGi))
                DrawStaticFlag(targets, StaticEditorFlags.BatchingStatic, "정적 배칭", "고정된 메시를 묶어 렌더링합니다. 라이트맵을 자동 연결하는 베이크 대상에서는 제외합니다.");
            DrawStaticFlag(targets, StaticEditorFlags.ReflectionProbeStatic, "반사 프로브에 포함", "반사 프로브를 베이크할 때 이 오브젝트를 포함합니다.");
#pragma warning disable CS0618
            DrawStaticFlag(targets, StaticEditorFlags.NavigationStatic, "내비게이션 대상 (이전 방식)", "Unity의 이전 내비게이션 베이크 플래그입니다. NavMeshSurface 설정은 별도로 관리합니다.");
            DrawStaticFlag(targets, StaticEditorFlags.OffMeshLinkGeneration, "이동 연결 자동 생성 (이전 방식)", "Unity의 이전 Off Mesh Link 자동 생성 플래그입니다.");
#pragma warning restore CS0618
            if (hasGi)
                EditorGUILayout.LabelField("베이크 대상은 라이트맵 자동 연결을 위해 정적 배칭을 제외합니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawStaticFlag(List<GameObject> targets, StaticEditorFlags flag, string label, string tooltip)
        {
            bool all = true, any = false;
            foreach (GameObject target in targets)
            {
                bool enabled = (GameObjectUtility.GetStaticEditorFlags(target) & flag) != 0;
                all &= enabled;
                any |= enabled;
            }
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = any && !all;
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), all);
            if (EditorGUI.EndChangeCheck())
            {
                FbxPlacementStaticSettings.SetFlag(targets, flag, value);
                Changed();
            }
            EditorGUI.showMixedValue = previousMixed;
        }
    }
}
