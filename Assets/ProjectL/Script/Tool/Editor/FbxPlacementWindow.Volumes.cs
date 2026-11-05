using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showVolumeCatalog;
        [SerializeField] private bool volumePreview = true;
        [SerializeField] private Volume selectedVolume;
        [SerializeField] private Volume importVolume;
        [SerializeField] private List<VolumeProfile> workspaceProfiles = new List<VolumeProfile>();
        private Vector2 _volumeScroll;
        private UnityEditor.Editor _volumeProfileEditor;

        private void BindWorkspaceVolumes()
        {
            foreach (Volume volume in workspaceRoot.GetComponentsInChildren<Volume>(true))
            {
                if (volume.sharedProfile == null) continue;
                if (EditorUtility.IsPersistent(volume.sharedProfile))
                    volume.sharedProfile = FbxPlacementVolumes.CopyProfile(volume.sharedProfile);
                TrackVolumeProfile(volume.sharedProfile);
            }
            UpdateVolumePreview();
        }

        private void TrackVolumeProfile(VolumeProfile profile)
        {
            if (profile != null && !EditorUtility.IsPersistent(profile) && !workspaceProfiles.Contains(profile)) workspaceProfiles.Add(profile);
        }

        private void ClearVolumeProfileEditor()
        {
            if (_volumeProfileEditor != null) DestroyImmediate(_volumeProfileEditor);
            _volumeProfileEditor = null;
        }

        private void ReleaseWorkspaceProfiles()
        {
            ClearVolumeProfileEditor();
            foreach (VolumeProfile profile in workspaceProfiles) FbxPlacementVolumes.DestroyProfile(profile);
            workspaceProfiles.Clear();
            selectedVolume = null;
        }

        private bool IsWorkspaceProfileObject(Object target)
        {
            foreach (VolumeProfile profile in workspaceProfiles)
                if (profile != null && (target == profile || (target is VolumeComponent component && profile.components.Contains(component)))) return true;
            return false;
        }

        private void UpdateVolumePreview()
        {
            sceneViewState.fxEnabled = true;
            sceneViewState.showImageEffects = volumePreview;
            // SceneView의 화면 효과 토글만으로는 URP 카메라가 Volume을 평가한다는
            // 보장이 없다. 작업실은 모든 레이어의 맵 Volume을 미리보기에 포함한다.
            if (camera != null && camera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            {
                cameraData.renderPostProcessing = volumePreview;
                cameraData.volumeLayerMask = ~0;
            }
            Repaint();
        }

        private void MoodChanged()
        {
            hasUnsavedChanges = true;
            UpdateVolumePreview();
        }

        private void DrawVolumeCatalog()
        {
            using var area = new GUILayout.AreaScope(new Rect(8, 6, CatalogWidth - 16, CatalogContentHeight - 12));
            using var scroll = new EditorGUILayout.ScrollViewScope(_volumeScroll);
            _volumeScroll = scroll.scrollPosition;
            GUILayout.Label("맵 볼륨 · 무드", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            volumePreview = EditorGUILayout.ToggleLeft("후처리 미리보기", volumePreview);
            if (EditorGUI.EndChangeCheck()) UpdateVolumePreview();
            EditorGUILayout.LabelField("블룸·색 보정 등은 재베이크 없이 바로 반영됩니다. 맵의 모든 레이어 Volume을 미리봅니다.", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("글로벌 볼륨 추가", GUILayout.Height(27)))
                DeferEditorAction(() =>
                {
                    selectedVolume = FbxPlacementVolumes.Add(workspaceRoot, PlacementParent);
                    TrackVolumeProfile(selectedVolume.sharedProfile);
                    volumePreview = true;
                    MoodChanged();
                });
            importVolume = (Volume)EditorGUILayout.ObjectField("기존 Volume", importVolume, typeof(Volume), true);
            using (new EditorGUI.DisabledScope(importVolume == null))
                if (GUILayout.Button("기존 Volume 가져오기", GUILayout.Height(24)))
                    DeferEditorAction(() =>
                    {
                        Volume imported = FbxPlacementVolumes.Import(workspaceRoot, importVolume);
                        TrackVolumeProfile(imported.sharedProfile);
                        selectedVolume = imported;
                        volumePreview = true;
                        MoodChanged();
                    });
            EditorGUILayout.LabelField("현재 씬의 Global/Local Volume을 작업실용 복사본으로 가져옵니다. 원본 Profile은 변경하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(6);
            Volume[] volumes = workspaceRoot.GetComponentsInChildren<Volume>(true);
            foreach (Volume volume in volumes)
                if (GUILayout.Toggle(selectedVolume == volume, volume.name, "Button")) selectedVolume = volume;
            if (selectedVolume == null || !Owns(selectedVolume.transform))
            {
                EditorGUILayout.HelpBox("볼륨을 추가하거나 목록에서 선택하세요. 프로필은 맵 SO와 함께 저장됩니다.", MessageType.Info);
                return;
            }
            Volume current = selectedVolume;
            GUILayout.Space(8);
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField("이름", current.name);
            bool enabled = EditorGUILayout.Toggle("볼륨 켜기", current.enabled);
            float weight = EditorGUILayout.Slider("영향도", current.weight, 0, 1);
            float priority = EditorGUILayout.FloatField("우선순위", current.priority);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { current, current.gameObject }, "맵 볼륨 설정");
                current.name = name;
                current.enabled = enabled;
                current.weight = weight;
                current.priority = priority;
                MoodChanged();
            }
            VolumeProfile source = (VolumeProfile)EditorGUILayout.ObjectField("프로필 가져오기", null, typeof(VolumeProfile), false);
            if (source != null)
                DeferEditorAction(() =>
                {
                    VolumeProfile copy = FbxPlacementVolumes.CopyProfile(source);
                    TrackVolumeProfile(copy);
                    Undo.RecordObject(current, "맵 볼륨 프로필 변경");
                    current.sharedProfile = copy;
                    ClearVolumeProfileEditor();
                    MoodChanged();
                });
            EditorGUILayout.LabelField("원본 프로필의 복사본을 편집합니다. 변경은 맵 SO 저장으로 보관하세요.", EditorStyles.wordWrappedMiniLabel);
            if (current.sharedProfile == null)
            {
                EditorGUILayout.HelpBox("적용할 Volume Profile을 가져오세요.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField(current.sharedProfile.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("각 효과의 Override 체크를 켜면 값이 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
            UnityEditor.Editor.CreateCachedEditor(current.sharedProfile, null, ref _volumeProfileEditor);
            EditorGUI.BeginChangeCheck();
            _volumeProfileEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) MoodChanged();
        }
    }
}
