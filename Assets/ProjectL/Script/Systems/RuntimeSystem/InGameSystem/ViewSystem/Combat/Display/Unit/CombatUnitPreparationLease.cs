using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>필드 대여와 구분되는 준비 수명입니다. 동시에 보유해 같은 풀 객체를 중복 준비하지 않습니다.</summary>
    internal sealed class CombatUnitPreparationLease : IDisposable
    {
        private readonly FieldUnitView _view;
        private readonly EntityKey _key;
        private readonly bool _rented;
        private readonly UnitHUDLayer _layer;
        private readonly Action<EntityKey, FieldUnitView> _return;
        // 기존 필드 표시를 빌린 경우(rented = false) 그 표시의 신원입니다. 준비 중 다른 개체로 바뀌면 오류로 알립니다(평가 G2-02).
        private readonly EntityId _entityId;
        private readonly int _bindVersion;
        private Renderer[] _renderers;
        private bool[] _rendererStates;
        private Collider[] _colliders;
        private bool[] _colliderStates;
        private Animator[] _animators;
        private bool[] _eventStates;
        private bool _disposed;

        internal CombatUnitPreparationLease(FieldUnitView view, EntityKey key, bool rented,
            UnitHUDLayer layer, Action<EntityKey, FieldUnitView> returnView)
        {
            _view = view;
            _key = key;
            _rented = rented;
            _layer = layer;
            _return = returnView;
            if (!rented && view != null)
            {
                _entityId = view.EntityId;
                _bindVersion = view.BodyTrackingVersion;
            }
        }

        internal void Activate(bool isCovered)
        {
            if (_rented)
            {
                // 모델의 Awake/OnEnable은 실행하되 필드 픽킹·애니메이션 이벤트·시각 노출은 막습니다.
                // GPU 셰이더 예열을 보장하는 경로가 아닙니다.
                _renderers = _view.GetComponentsInChildren<Renderer>(true);
                _rendererStates = new bool[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _rendererStates[i] = _renderers[i].enabled;
                    _renderers[i].enabled = false;
                }
                _colliders = _view.GetComponentsInChildren<Collider>(true);
                _colliderStates = new bool[_colliders.Length];
                for (int i = 0; i < _colliders.Length; i++)
                {
                    _colliderStates[i] = _colliders[i].enabled;
                    _colliders[i].enabled = false;
                }
                _animators = _view.GetComponentsInChildren<Animator>(true);
                _eventStates = new bool[_animators.Length];
                for (int i = 0; i < _animators.Length; i++)
                {
                    _eventStates[i] = _animators[i].fireEvents;
                    _animators[i].fireEvents = false;
                }
                _view.gameObject.SetActive(true);
                if (!_view.isActiveAndEnabled)
                    throw new InvalidOperationException("[CombatUnitPreparation] 유닛의 부모가 비활성 상태입니다.");
                if (_view.HealthLabel != null)
                {
                    // 사실이 없는 유닛도 숫자 글리프 자원은 준비합니다. 실제 체력으로 게시하지 않습니다.
                    _view.HealthLabel.SetText("0123456789/");
                    _view.HealthLabel.ForceMeshUpdate(true);
                }
            }
            _layer.BeginPreparation(_view.Hud, isCovered);
            _view.Hud.PrepareActiveMeshes();
        }

        internal void Validate()
        {
            if (_disposed || _view == null || _view.Hud == null || (_rented && !_view.isActiveAndEnabled))
                throw new InvalidOperationException("[CombatUnitPreparation] 준비 중인 유닛 또는 HUD가 해제됐습니다.");
            if (!_rented && (_view.EntityId != _entityId || _view.BodyTrackingVersion != _bindVersion))
                throw new InvalidOperationException("[CombatUnitPreparation] 준비 중 기존 필드 표시가 다른 개체로 바뀌었습니다.");
            _layer.ValidatePreparation(_view.Hud);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            List<Exception> errors = null;
            if (_layer != null && _view != null)
                CleanupErrors.Try(() => _layer.EndPreparation(_view.Hud), ref errors);
            // EndPreparation은 시작 때 떠 둔 보임으로 되돌립니다. 기존 필드 표시는 그 사이 바뀐 지금 상태로 다시 맞춥니다(평가 G2-02).
            // 빌린 표시(rented)는 아래 반환(ResetForPool)이 다시 맞춥니다.
            if (!_rented && _view != null) CleanupErrors.Try(_view.ResyncHudVisible, ref errors);
            if (_rented)
            {
                if (_view != null)
                {
                    CleanupErrors.Try(() => _view.gameObject.SetActive(false), ref errors);
                    CleanupErrors.Try(RestoreComponents, ref errors);
                }
                // 외부에서 파괴된 객체도 대여 계측 수명은 종료합니다. 반환 입구가 Unity null을 처리합니다.
                CleanupErrors.Try(() => _return(_key, _view), ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatUnitPreparation] 준비 대여 정리에 실패했습니다.");
        }

        private void RestoreComponents()
        {
            if (_renderers != null)
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].enabled = _rendererStates[i];
            if (_colliders != null)
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null) _colliders[i].enabled = _colliderStates[i];
            if (_animators != null)
                for (int i = 0; i < _animators.Length; i++)
                    if (_animators[i] != null) _animators[i].fireEvents = _eventStates[i];
        }
    }
}
