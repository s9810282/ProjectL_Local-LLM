using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>CatusCannon 인스턴스의 Bucket·발사 피벗과 임시 무기 자세만 소유합니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class CatusCannonWeaponView : MonoBehaviour
    {
        [Tooltip("곡사 공격을 준비할 때 기울일 무기 Bucket입니다.")]
        [SerializeField] private Transform bucket;
        [Tooltip("발사 순간의 월드 피벗입니다. 비워 두면 원본처럼 Bucket 피벗에서 발사합니다.")]
        [SerializeField] private Transform muzzle;

        private Transform _restBucket;
        private Quaternion _restRotation;
        private Action _cancelOwner;
        private int _poseVersion;
        private bool _poseActive;

        internal bool IsAvailable => isActiveAndEnabled && bucket != null && (muzzle == null || muzzle.IsChildOf(transform));
        internal Vector3 MuzzlePosition
        {
            get
            {
                Validate();
                return (muzzle != null ? muzzle : bucket).position;
            }
        }

        internal void Validate()
        {
            if (bucket == null || !bucket.IsChildOf(transform))
                throw new InvalidOperationException("[CatusCannonWeaponView] 이 무기 안의 Bucket 참조가 필요합니다.");
            if (muzzle != null && !muzzle.IsChildOf(transform))
                throw new InvalidOperationException("[CatusCannonWeaponView] 발사 피벗은 이 무기 안에 있어야 합니다.");
        }

        internal int BeginPose()
        {
            Validate();
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("[CatusCannonWeaponView] 비활성 무기로 공격할 수 없습니다.");
            if (_poseActive || _cancelOwner != null)
                throw new InvalidOperationException("[CatusCannonWeaponView] 이전 공격 자세를 먼저 정리해야 합니다.");
            if (_restBucket != bucket)
            {
                _restBucket = bucket;
                _restRotation = bucket.localRotation;
            }
            _poseActive = true;
            ++_poseVersion;
            bucket.localRotation = _restRotation;
            return _poseVersion;
        }

        internal bool IsPoseCurrent(int version) => _poseActive && version == _poseVersion;

        internal void SetPrepareProgress(int version, float progress, float pitch)
        {
            if (!IsPoseCurrent(version))
                return;
            Validate();
            bucket.localRotation = _restRotation * Quaternion.Euler(pitch * Mathf.SmoothStep(0f, 1f, progress), 0f, 0f);
        }

        internal void SetReturnProgress(int version, float progress, float pitch)
        {
            if (!IsPoseCurrent(version))
                return;
            Validate();
            bucket.localRotation = _restRotation * Quaternion.Euler(pitch * (1f - Mathf.SmoothStep(0f, 1f, progress)), 0f, 0f);
        }

        internal void BindCancellation(Action cancelOwner)
        {
            if (cancelOwner == null)
                throw new ArgumentNullException(nameof(cancelOwner));
            if (_cancelOwner != null && _cancelOwner != cancelOwner)
                throw new InvalidOperationException("[CatusCannonWeaponView] 취소 소유자가 이미 연결돼 있습니다.");
            _cancelOwner = cancelOwner;
        }

        internal void UnbindCancellation(Action cancelOwner)
        {
            if (_cancelOwner == cancelOwner)
                _cancelOwner = null;
        }

        internal void ResetPose()
        {
            _poseActive = false;
            ++_poseVersion;
            if (_restBucket != null)
                _restBucket.localRotation = _restRotation;
        }

        private void OnDisable()
        {
            Action cancelOwner = _cancelOwner;
            _cancelOwner = null;
            _poseActive = false;
            ++_poseVersion;
            int disabledVersion = _poseVersion;
            try
            {
                cancelOwner?.Invoke();
            }
            finally
            {
                // 취소 소유자가 새 대여를 시작했다면 이전 비활성화가 그 자세를 덮지 않습니다.
                if (_poseVersion == disabledVersion)
                    ResetPose();
            }
        }
    }
}
