using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>한 로비 연결의 이동 루트·표시 보정 루트와 캐릭터 복제본을 소유합니다.</summary>
    internal sealed class LobbyCharacterVisual : IDisposable
    {
        private GameObject _movementRoot;
        private GameObject _visualRoot;
        private GameObject _model;
        private GameObject _modelPrefab;
        private int _modelVersion;

        internal Transform MovementRoot => _movementRoot != null ? _movementRoot.transform : null;

        private LobbyCharacterVisual() { }

        /// <summary>비활성 이동 루트와 표시 보정 루트를 만듭니다. 호출자가 첫 모델·소유권을 준비한 뒤 Show를 호출합니다.</summary>
        internal static LobbyCharacterVisual Create(Transform worldRoot, LobbyCharacterPresentationData presentation)
        {
            if (worldRoot == null) throw new ArgumentNullException(nameof(worldRoot));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));
            // 보정 값은 DB 생성 때 검사했습니다. 모델 프리팹은 MainModelPrefab이 선택 키로 조회·검사합니다.
            var owner = new LobbyCharacterVisual();
            try
            {
                owner._movementRoot = new GameObject("LobbyCharacterRoot");
                owner._movementRoot.SetActive(false);
                owner._movementRoot.transform.SetParent(worldRoot, false);
                owner._visualRoot = new GameObject("LobbyCharacterVisual");
                owner._visualRoot.transform.SetParent(owner._movementRoot.transform, false);
                owner._visualRoot.transform.SetLocalPositionAndRotation(presentation.CharacterOffset,
                    Quaternion.Euler(0f, presentation.CharacterYaw, 0f));
                owner._visualRoot.transform.localScale = Vector3.one * presentation.CharacterScale;
                return owner;
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 이동·보정 루트는 보존하고 안쪽 모델만 바꿉니다. 비활성 계층에서 복제본 정책을 적용한 뒤 표시합니다.
        /// 같은 프리팹이면 Idle을 다시 시작하지 않으며, 실패한 복제본은 이 호출이 직접 정리합니다.
        /// </summary>
        internal void ShowModel(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (_movementRoot == null || _visualRoot == null)
                throw new InvalidOperationException("[LobbyCharacterVisual] 정리한 캐릭터의 모델을 바꿀 수 없습니다.");
            // 복제를 생략하는 최신 요청도 재진입 전의 교체를 무효화합니다.
            int version = ++_modelVersion;
            if (_modelPrefab == prefab) return;

            GameObject visualRoot = _visualRoot;
            bool wasActive = visualRoot.activeSelf;
            GameObject pending = null;
            try
            {
                visualRoot.SetActive(false);
                if (!IsCurrentModelChange(visualRoot, version)) return;
                GameObject previous = _model;
                _model = null;
                _modelPrefab = null;
                DestroyOwned(previous);
                if (!IsCurrentModelChange(visualRoot, version)) return;

                pending = UnityEngine.Object.Instantiate(prefab, visualRoot.transform, false);
                if (!IsCurrentModelChange(visualRoot, version)) return;
                ChessPieceClone.Prepare(pending);
                if (!IsCurrentModelChange(visualRoot, version)) return;
                _model = pending;
                _modelPrefab = prefab;
                pending = null;
            }
            finally
            {
                // 소유권을 인계하지 못한 복제본은 실패·중도 폐기 모두 이 호출이 거둡니다.
                DestroyOwned(pending);
                // 비활성 콜백에서 더 최신 모델이 들어와도 원래 켜져 있던 보정 루트는 다시 켭니다.
                // 연결이 끝났으면 필드가 비워져 있으므로 폐기된 표시를 되살리지 않습니다.
                if (wasActive && visualRoot != null && ReferenceEquals(_visualRoot, visualRoot) && _model != null)
                    visualRoot.SetActive(true);
            }
        }

        private bool IsCurrentModelChange(GameObject visualRoot, int version) =>
            _movementRoot != null && ReferenceEquals(_visualRoot, visualRoot) && _modelVersion == version;

        internal void Show()
        {
            if (_movementRoot != null) _movementRoot.SetActive(true);
        }

        internal void Hide()
        {
            if (_movementRoot != null) _movementRoot.SetActive(false);
        }

        public void Dispose()
        {
            GameObject container = _movementRoot;
            ++_modelVersion;
            _movementRoot = null;
            _visualRoot = null;
            _model = null;
            _modelPrefab = null;
            DestroyOwned(container);
        }

        private static void DestroyOwned(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (instance == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(instance);
            else UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
