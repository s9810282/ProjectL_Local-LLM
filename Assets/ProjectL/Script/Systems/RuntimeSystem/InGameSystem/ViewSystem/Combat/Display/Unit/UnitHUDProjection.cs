using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>최종 출력 카메라의 투영으로 HUD 위치와 기준 구도 대비 크기를 함께 구합니다.</summary>
    internal static class UnitHUDProjection
    {
        // 2026-10-18 Overlay 이전의 기준 구도입니다. 런타임 카메라 렌즈를 저장하는 값이 아닙니다.
        private const float ReferenceHeight = 1080f;
        private const float ReferenceOrthographicSize = 7f;
        internal static Vector2 ReferenceResolution => new(1920f, ReferenceHeight);

        /// <summary>화면 밖·clip 밖·일시적인 투영 불가는 false입니다. 반환값이 true일 때만 위치와 배율을 씁니다.</summary>
        internal static bool TryProject(Camera camera, Canvas canvas, RectTransform layer, Vector3 world,
            out Vector2 local, out float scale)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            local = default;
            scale = 0f;
            if (camera == null || !Finite(world) || camera.pixelWidth <= 0 || camera.pixelHeight <= 0)
                return false;

            Vector3 screen = camera.WorldToScreenPoint(world);
            if (!Finite(screen) || screen.z < camera.nearClipPlane || screen.z > camera.farClipPlane ||
                !camera.pixelRect.Contains(new Vector2(screen.x, screen.y)))
                return false;

            // 같은 깊이에 있는 카메라 위쪽 월드 1단위입니다. 이 샘플 자체는 화면 밖이어도 됩니다.
            Vector3 sample = camera.WorldToScreenPoint(world + camera.transform.up);
            if (!Finite(sample)) return false;
            float pixelsPerWorldUnit = new Vector2(sample.x - screen.x, sample.y - screen.y).magnitude;
            if (!TryGetScale(pixelsPerWorldUnit, canvas.scaleFactor, out scale)) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer,
                new Vector2(screen.x, screen.y), null, out local) || !Finite(local.x) || !Finite(local.y))
            {
                local = default;
                scale = 0f;
                return false;
            }
            return true;
        }

        /// <summary>픽셀 밀도와 CanvasScaler의 이중 확대를 상쇄하는 순수 수치 계산입니다.</summary>
        internal static bool TryGetScale(float pixelsPerWorldUnit, float canvasScaleFactor, out float scale)
        {
            scale = 0f;
            if (!Positive(pixelsPerWorldUnit) || !Positive(canvasScaleFactor)) return false;
            const float referencePixelsPerWorldUnit = ReferenceHeight / (2f * ReferenceOrthographicSize);
            float calculated = (float)((double)pixelsPerWorldUnit / canvasScaleFactor / referencePixelsPerWorldUnit);
            if (!Positive(calculated)) return false;
            scale = calculated;
            return true;
        }

        internal static bool IsValidBaseScale(Vector3 scale) => Positive(scale.x) && Positive(scale.y) && Positive(scale.z);
        private static bool Positive(float value) => value > 0f && Finite(value);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}