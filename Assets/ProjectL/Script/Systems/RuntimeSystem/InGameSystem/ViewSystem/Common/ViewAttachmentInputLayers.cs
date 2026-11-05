using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 연결 하나가 쓰는 입력 Layer 창구입니다. 현재 연결이면 정리 중이어도 바꾸고, 새 연결로 교체된 연결의 Layer 변경만 막습니다.
    /// 스테이지 맵과 로비의 팝업 입력 세션이 씁니다.
    /// </summary>
    internal sealed class ViewAttachmentInputLayers : IInputLayerService
    {
        private readonly IInputLayerService _layers;
        private readonly Func<bool> _isCurrent;

        internal ViewAttachmentInputLayers(IInputLayerService layers, Func<bool> isCurrent)
        {
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        }

        public InputLayer CurrentLayer => _layers.CurrentLayer;

        public void ChangeLayer(InputLayer layer)
        {
            if (_isCurrent()) _layers.ChangeLayer(layer);
        }

        public void ClearLayer()
        {
            if (_isCurrent()) _layers.ClearLayer();
        }
    }
}
