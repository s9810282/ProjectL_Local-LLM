using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>공통 팝업의 입력 소유와 호스트 복귀를 관리합니다.</summary>
    internal sealed class PopupInputSession
    {
        private readonly IInputLayerService _layers;
        private readonly InputLayer _restoreLayer;
        private readonly Func<bool> _canRestore;
        private Scope _owner;
        private object _version;

        internal PopupInputSession(IInputLayerService layers, InputLayer restoreLayer, Func<bool> canRestore)
        {
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _canRestore = canRestore ?? throw new ArgumentNullException(nameof(canRestore));
            if (!restoreLayer.IsValid)
            {
                throw new ArgumentException("팝업 종료 후 복귀할 입력 Layer가 유효해야 합니다.", nameof(restoreLayer));
            }
            _restoreLayer = restoreLayer;
        }

        /// <summary>
        /// 모달 입력 수명을 열고 그 토큰을 돌려줍니다. 동기 콜백 때문에 수명을 쥐지 못했으면(복귀 불가로 바로 끝냄, 다른 소유자로 넘어감)
        /// <c>null</c>을 돌려줍니다. 호출자는 그때 열기를 멈춥니다(로비 리팩토링 2차 설계 N-05).
        /// </summary>
        internal object Begin(InputLayer? allowedLayer)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException("[PopupInputSession] 이미 열린 모달 입력 수명이 있습니다.");
            }

            InputLayer previousLayer = _layers.CurrentLayer;
            var scope = new Scope(allowedLayer);
            _owner = scope;
            _version = scope;
            try
            {
                if (allowedLayer.HasValue)
                {
                    _layers.ChangeLayer(allowedLayer.Value);
                }
                else
                {
                    _layers.ClearLayer();
                }

                // 외부 전환 콜백 안의 요청은 보류될 수 있습니다. 미확립 수명은 반환하지 않습니다.
                if (ReferenceEquals(_owner, scope) && !OwnsLayer(scope))
                {
                    _owner = null;
                    if (ReferenceEquals(_version, scope) && _layers.CurrentLayer.Equals(previousLayer))
                    {
                        _layers.ClearLayer();
                    }
                    throw new InvalidOperationException("[PopupInputSession] 모달 입력 Layer가 확립되지 않았습니다.");
                }

                // Disable의 동기 취소 알림 뒤 외곽 Layer 변경이 계속될 수 있습니다.
                if (ReferenceEquals(_owner, scope) && !_canRestore())
                {
                    End(scope, false);
                    return null;
                }
                if (!ReferenceEquals(_owner, scope))
                {
                    if (ReferenceEquals(_version, scope)) ClearOwned(scope);
                    return null;
                }
                return scope;
            }
            catch
            {
                if (ReferenceEquals(_owner, scope))
                {
                    _owner = null;
                }
                if (ReferenceEquals(_version, scope))
                {
                    ClearOwned(scope);
                }
                throw;
            }
        }

        internal void End(object token, bool restore)
        {
            if (token == null || !ReferenceEquals(_owner, token))
            {
                return;
            }

            Scope scope = _owner;
            bool restoring = false;
            // 조건 확인과 Layer 변경 모두 콜백을 실행할 수 있으므로 먼저 무효화합니다.
            _owner = null;
            try
            {
                if (!OwnsLayer(scope))
                {
                    return;
                }

                bool canRestore = restore && _canRestore();
                if (!ReferenceEquals(_version, scope) || !OwnsLayer(scope))
                {
                    return;
                }

                if (canRestore)
                {
                    restoring = true;
                    _layers.ChangeLayer(_restoreLayer);
                    // 복귀 중 fault/Detach가 발생하면 새 소유자의 요청은 유지합니다.
                    if (ReferenceEquals(_version, scope))
                    {
                        bool healthy = _canRestore();
                        if (ReferenceEquals(_version, scope) && !healthy &&
                            _layers.CurrentLayer.Equals(_restoreLayer))
                        {
                            _layers.ClearLayer();
                        }
                    }
                }
                else
                {
                    ClearOwned(scope);
                }
            }
            catch
            {
                if (ReferenceEquals(_version, scope))
                {
                    if (restoring && _layers.CurrentLayer.Equals(_restoreLayer))
                    {
                        _layers.ClearLayer();
                    }
                    else
                    {
                        ClearOwned(scope);
                    }
                }
                throw;
            }
        }

        private bool OwnsLayer(Scope scope) => _layers.CurrentLayer.Equals(scope.Layer ?? default);

        private void ClearOwned(Scope scope)
        {
            if (OwnsLayer(scope))
            {
                _layers.ClearLayer();
            }
        }

        private sealed class Scope
        {
            internal readonly InputLayer? Layer;

            internal Scope(InputLayer? layer) => Layer = layer;
        }
    }
}
