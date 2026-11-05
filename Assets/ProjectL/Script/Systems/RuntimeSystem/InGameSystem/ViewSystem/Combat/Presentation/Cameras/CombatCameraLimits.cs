using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 전투 카메라 조작의 제한 규칙입니다(카메라 조작 설계 CC1·CC3·CC4). 이동과 줌이 이 함수만 씁니다. 상태가 없습니다.
    /// 값의 조건(최소 ≤ 최대 등)은 <see cref="CombatCameraSettingsData.FindError"/>가 원본이고, 여기서는 그 범위 안으로 맞추기만 합니다.
    /// </summary>
    internal static class CombatCameraLimits
    {
        /// <summary>이동 오프셋(x = 좌우, y = 상하, 카메라 화면 축의 월드 단위)을 설정의 최소·최대 안으로 맞춥니다(결정 Q-CC3 A).</summary>
        internal static Vector2 ClampOffset(Vector2 offset, CombatCameraSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new Vector2(
                Mathf.Clamp(offset.x, settings.PanMin.x, settings.PanMax.x),
                Mathf.Clamp(offset.y, settings.PanMin.y, settings.PanMax.y));
        }

        /// <summary>보기 크기(직교 크기)를 설정의 줌 최소·최대 안으로 맞춥니다.</summary>
        internal static float ClampSize(float size, CombatCameraSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return Mathf.Clamp(size, settings.ZoomMin, settings.ZoomMax);
        }
    }
}
