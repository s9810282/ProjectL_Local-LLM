using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>명시적인 씬 카메라와 기존 조작·효과 협력 객체를 표시 연결 하나로 조립합니다.</summary>
    internal static class CombatCameraFactory
    {
        internal static CombatCameraSession Create(CombatSceneCameraBinding binding,
            ICameraCatalogService catalog, ICameraEffectService effects)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));

            var settings = binding.ReadSettings();
            CombatCameraSnapshot snapshot = binding.Capture();
            var snapshots = new[] { snapshot };
            var window = new CombatCameraWindow(effects, () => Time.time);
            try
            {
                window.AttachCameras(snapshots);
                var view = new CombatCameraView(window, snapshots, settings);
                var selection = new CombatCameraSelection(catalog, snapshot);
                return new CombatCameraSession(window, view, selection);
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(window.DetachCameras, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatCameraFactory] 카메라 조립과 정리에 실패했습니다.");
                throw;
            }
        }
    }
}
