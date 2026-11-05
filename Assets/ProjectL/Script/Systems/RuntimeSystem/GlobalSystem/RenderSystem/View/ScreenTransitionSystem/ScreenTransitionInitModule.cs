using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>화면 전환의 초기 설정을 정의하는 유일한 코드 모듈입니다.</summary>
    internal sealed class ScreenTransitionInitModule : IScreenTransitionInitModule
    {
        internal const string MainCameraName = "Main Camera";
        private readonly Func<string, int> _findLayer;

        internal ScreenTransitionInitModule() : this(LayerMask.NameToLayer)
        {
        }

        internal ScreenTransitionInitModule(Func<string, int> findLayer)
        {
            _findLayer = findLayer ?? throw new ArgumentNullException(nameof(findLayer));
        }

        public ScreenTransitionInitData Load() => new()
        {
            MainCamera = MainCameraName,
            FixedLayer = GetLayerMask("Default") | GetLayerMask("BothRendering") |
                GetLayerMask("DayCameraRender") | GetLayerMask("NightCameraRender"),
            CircleCenter = Vector2.zero,
            TransitionWidth = 5.48f
        };

        private int GetLayerMask(string name)
        {
            int layer = _findLayer(name);
            if (layer < 0 || layer > 31)
                throw new InvalidOperationException($"화면 전환 고정 레이어가 정의되지 않았습니다: {name}");
            return 1 << layer;
        }
    }
}
