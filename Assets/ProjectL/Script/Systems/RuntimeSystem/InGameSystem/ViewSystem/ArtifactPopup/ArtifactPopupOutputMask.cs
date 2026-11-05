using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 팝업이 열린 동안 출력 마스크에 설정의 층을 더하고, 닫을 때 대여 전 상태로 되돌립니다.
    /// 아티팩트 팝업 Factory가 만듭니다(로비 리팩토링 2차 설계 N-61).
    /// </summary>
    internal sealed class ArtifactPopupOutputMask : IArtifactPopupOutput
    {
        private readonly IUnityCameraCatalogService _outputs;
        private readonly IPopupViewHost _host;
        private readonly string _outputName;
        private readonly int _layerMask;
        private IUnityCameraCatalogHandle _handle;
        private UnityCameraRenderState _before;
        private bool _captured;

        internal ArtifactPopupOutputMask(IUnityCameraCatalogService outputs, Camera output, IPopupViewHost host,
            string layerName)
        {
            _outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
            if (output == null) throw new ArgumentNullException(nameof(output));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _outputName = output.gameObject.name;
            // 층 이름은 DB 생성 때 프로젝트에 있는지 검사가 끝났습니다(ArtifactPopupConfigData.Validate).
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new InvalidOperationException($"[ArtifactPopup] 출력 마스크에 추가할 층 '{layerName}'이 필요합니다.");
            _layerMask = 1 << layer;
        }

        public void Enter()
        {
            if (_handle != null || !_host.IsCurrent) return;
            IUnityCameraCatalogHandle handle = _outputs.GetHandle(_outputName);
            if (handle == null)
                throw new InvalidOperationException($"[ArtifactPopup] 출력 카메라 '{_outputName}'의 핸들이 필요합니다.");
            _handle = handle;
            try
            {
                if (!_host.IsCurrent)
                {
                    Exit();
                    return;
                }
                _before = handle.Capture();
                _captured = true;
                if (_host.IsCurrent) handle.SetCullingMask(_before.CullingMask | _layerMask);
                if (!_host.IsCurrent) Exit();
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(Exit, ref errors);
                throw new AggregateException("[ArtifactPopup] 출력 마스크 연결 중 실패했습니다.", errors);
            }
        }

        public void Exit()
        {
            IUnityCameraCatalogHandle handle = _handle;
            if (handle == null) return;
            UnityCameraRenderState before = _before;
            bool captured = _captured;
            // 정리 중 재진입하더라도 이전 대여를 두 번 복원하거나 반환하지 않습니다.
            _handle = null;
            _before = default;
            _captured = false;
            List<Exception> errors = null;
            // 교체된 연결은 새 연결의 출력 설정을 덮지 않고 대여만 반환합니다.
            if (captured && _host.IsCurrent)
                CleanupErrors.Try(() => handle.Restore(before), ref errors);
            CleanupErrors.Try(handle.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[ArtifactPopup] 출력 마스크 정리 중 실패했습니다.");
        }
    }
}
