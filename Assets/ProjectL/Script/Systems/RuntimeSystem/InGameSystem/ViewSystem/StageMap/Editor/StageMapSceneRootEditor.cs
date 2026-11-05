#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Editor
{
    /// <summary>그래픽 작업용 구도 미리보기와 씬 카메라 확정을 제공합니다.</summary>
    [CustomEditor(typeof(StageMapSceneRoot))]
    internal sealed class StageMapSceneRootEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var root = (StageMapSceneRoot)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("그래픽 작업용 카메라 구도", EditorStyles.boldLabel);
            if (root.MapCamera == null || root.MapRoot == null || string.IsNullOrEmpty(root.gameObject.scene.path))
            {
                EditorGUILayout.HelpBox("저장된 씬에서 Map Camera와 Map Root를 먼저 연결하세요.", MessageType.Info);
                return;
            }

            var settings = new StageMapCameraFramingSettings(root);
            Transform camera = root.MapCamera.transform;
            EditorGUILayout.HelpBox("가운데 버튼 진단: " + settings.DollyStatus, MessageType.Info);
            if (!settings.IsOpen)
            {
                EditorGUILayout.HelpBox("잠금 상태입니다. 확정 구도는 Map Camera의 Transform과 Lens에 저장됩니다.", MessageType.Info);
                var confirmed = settings.Confirmed;
                if (confirmed != null)
                    EditorGUILayout.HelpBox("확정 구도를 유지하고 있습니다. Play 종료 후 편집 씬에 반영되면 씬을 저장하세요. 임시 확정값은 Unity 종료 시 사라집니다.", MessageType.Info);
                if (GUILayout.Button("구도 조정 잠금 해제"))
                {
                    settings.Unlock();
                }
                EditorGUILayout.LabelField("Play 중에도 잠금을 해제할 수 있습니다.");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector3Field("Position (World)", confirmed != null ? confirmed.position : camera.position);
                    EditorGUILayout.Vector3Field("Rotation (World)", confirmed != null ? confirmed.angles : camera.eulerAngles);
                    EditorGUILayout.FloatField("Size (Lens Orthographic Size)", confirmed != null ? confirmed.size : root.MapCamera.m_Lens.OrthographicSize);
                }
                return;
            }

            EditorGUILayout.HelpBox("Play 중에도 확정 및 잠금이 가능합니다. 확정 구도는 Play 종료 후 편집 씬에 반영됩니다. 이후 씬을 저장하세요. 임시 값은 Unity 종료 시 사라집니다.", MessageType.Info);
            EditorGUILayout.HelpBox("Game 뷰: Alt + 왼쪽 드래그 = 화면 기준 이동, Alt + 오른쪽 드래그 = X/Y 회전, Alt + 가운데 위/아래 드래그 = 전진/후진, Alt + 휠 굴리기 = 확대/축소. Z 회전은 아래에서 조정합니다.", MessageType.None);
            Vector3 position = EditorGUILayout.Vector3Field("Position (World)", settings.Position);
            Vector3 angles = EditorGUILayout.Vector3Field("Rotation (World)", settings.Angles);
            float size = EditorGUILayout.FloatField("Size (Lens Orthographic Size)", settings.Size);
            bool valid = settings.IsValid(position, angles) && StageMapCameraFramingSettings.IsValidSize(size);
            if (valid)
            {
                settings.TrySetPose(position, angles);
                settings.TrySetSize(size);
            }
            if (!valid)
            {
                EditorGUILayout.HelpBox("맵 평면을 바라보는 유효한 위치·회전과 0.01 이상의 유한한 Size를 입력하세요. 잘못된 값은 반영하지 않습니다.", MessageType.Error);
            }

            using (new EditorGUI.DisabledScope((EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) || !valid))
            {
                if (GUILayout.Button("확정 및 잠금"))
                {
                    settings.Confirm();
                }
            }
            if (GUILayout.Button("조정 취소 및 잠금"))
            {
                settings.Lock();
            }
        }
    }
}
#endif
