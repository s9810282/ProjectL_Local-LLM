using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>Combat 연결 수명의 공용 HUD Canvas를 조립합니다. 위치·줌 비례 크기·가시성은 Layer가 담당합니다.</summary>
    internal static class CombatUnitHUDFactory
    {
        internal static UnitHUDLayer Create(Transform parent, Camera outputCamera)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (outputCamera == null) throw new ArgumentNullException(nameof(outputCamera));
            var root = new GameObject("CombatUnitHUDCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.layer = 5;
                root.transform.SetParent(parent, false);
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = -10;
                canvas.targetDisplay = outputCamera.targetDisplay;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = UnitHUDProjection.ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
                UnitHUDLayer layer = root.AddComponent<UnitHUDLayer>();
                layer.Configure(outputCamera);
                return layer;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }
    }
}
