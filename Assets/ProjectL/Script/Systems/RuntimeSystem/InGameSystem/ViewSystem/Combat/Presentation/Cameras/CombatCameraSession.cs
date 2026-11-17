using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEditor;
#endif
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>Factory가 조립한 카메라 협력 객체의 표시 연결 수명입니다. 입력·효과 알고리즘과 객체 생성은 기존 담당자에게 맡깁니다.</summary>
    internal sealed class CombatCameraSession : ICombatCameraControl
    {
        private CombatCameraView _view;
        private CombatCameraSelection _selection;
        private bool _begun;
        private bool _released;

        internal CombatCameraSession(CombatCameraWindow window, CombatCameraView view, CombatCameraSelection selection)
        {
            Window = window ?? throw new ArgumentNullException(nameof(window));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

#if UNITY_EDITOR
        private CombatCameraSettingsSO _editorSettings;
        private int _editorRevision;

        internal void WatchEditorSettings(CombatCameraSettingsSO settings)
        {
            _editorSettings = settings;
            _editorRevision = settings.EditorRevision;
        }

        private void RefreshEditorSettings()
        {
            if (!_begun || _editorSettings == null || _editorRevision == _editorSettings.EditorRevision || !Window.CanZoom)
                return;
            _editorRevision = _editorSettings.EditorRevision;
            try
            {
                _view.ApplyEditorSettings(_editorSettings.CreateData());
            }
            catch (Exception error)
            {
                Debug.LogWarning($"카메라 설정 갱신 실패: {error.Message}", _editorSettings);
            }
        }
#endif

        internal CombatCameraWindow Window { get; }
        internal ICombatCameraControl Control => this;

        public float EdgeThickness => _begun ? _view.EdgeThickness : 0f;
        public float ZoomSeconds => _begun ? _view.ZoomSeconds : 0f;

        /// <summary>카탈로그 수집 뒤 선택·자세 연결을 시작하고, 실패하면 소유 자원을 끝까지 정리합니다.</summary>
        internal void Begin()
        {
            if (_released)
                throw new ObjectDisposedException(nameof(CombatCameraSession));
            if (_begun)
                throw new InvalidOperationException("[CombatCameraSession] 카메라가 이미 연결됐습니다.");
            try
            {
                _selection.Begin();
                _view.Begin();
                _begun = true;
#if UNITY_EDITOR
                if (_editorSettings != null)
                    EditorApplication.update += RefreshEditorSettings;
#endif
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(Release, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatCameraSession] 카메라 연결과 복구에 실패했습니다.");
                throw;
            }
        }

        public bool Pan(Vector2 keyDirection, Vector2 edgeDirection, float deltaTime) =>
            _begun && _view.Pan(keyDirection, edgeDirection, deltaTime);

        public bool Zoom(int steps) => _begun && _view.Zoom(steps);

        public bool Rotate(int steps, float deltaTime) => _begun && _view.Rotate(steps, deltaTime);

        public bool RotateDrag(float screenWidths) => _begun && _view.RotateDrag(screenWidths);

        /// <summary>늦은 입력을 먼저 막고 자세·렌즈·대여·Priority를 복원합니다. 반복 호출은 아무 일도 하지 않습니다.</summary>
        internal void Release()
        {
            if (_released)
                return;
            _released = true;
#if UNITY_EDITOR
            EditorApplication.update -= RefreshEditorSettings;
#endif
            _begun = false;
            CombatCameraView view = _view;
            CombatCameraSelection selection = _selection;
            _view = null;
            _selection = null;

            List<Exception> errors = null;
            // 이미 씬과 함께 파괴됐다면 Transform·효과 카탈로그를 통해 복구하려 하지 않습니다.
            if (selection.HasCamera)
                CleanupErrors.Try(view.Restore, ref errors);
            CleanupErrors.Try(Window.DetachCameras, ref errors);
            CleanupErrors.Try(selection.Release, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatCameraSession] 카메라 해제에 실패했습니다.");
        }
    }
}
