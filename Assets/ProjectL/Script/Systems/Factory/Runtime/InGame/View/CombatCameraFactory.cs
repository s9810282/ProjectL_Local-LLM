using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>명시적인 씬 카메라와 기존 조작·효과 협력 객체를 표시 연결 하나로 조립합니다.</summary>
    internal static class CombatCameraFactory
    {
        /// <param name="groundY">회전 중심을 찾을 바닥 평면의 월드 높이입니다. 호출자는 <see cref="GroundHeightOf"/>로 필드에서 읽습니다(카메라 회전 계획 TP-11).</param>
        internal static CombatCameraSession Create(CombatSceneCameraBinding binding,
            ICameraCatalogService catalog, ICameraEffectService effects, float groundY)
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
                var view = new CombatCameraView(window, snapshots, settings, groundY);
                var selection = new CombatCameraSelection(catalog, snapshot);
                var session = new CombatCameraSession(window, view, selection);
#if UNITY_EDITOR
                session.WatchEditorSettings(binding.EditorSettings);
#endif
                return session;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(window.DetachCameras, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatCameraFactory] 카메라 조립과 정리에 실패했습니다.");
                throw;
            }
        }

        /// <summary>
        /// 카메라 회전 중심을 찾을 바닥 평면 높이 = 필드 Cells Root의 월드 높이입니다(카메라 회전 계획 TP-11). 운영·특성 확인 조립이 함께 씁니다.
        /// Cells Root가 없으면 원인 문구로 거절합니다.
        /// </summary>
        internal static float GroundHeightOf(FieldView field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (field.CellsRoot == null)
                throw new InvalidOperationException("[CombatCameraFactory] 필드 Cells Root가 없어 카메라 회전 중심 높이를 정할 수 없습니다.");
            return field.CellsRoot.position.y;
        }
    }
}
