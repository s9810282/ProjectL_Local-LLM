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

        internal Transform MovementRoot => _movementRoot != null ? _movementRoot.transform : null;

        private LobbyCharacterVisual() { }

        /// <summary>비활성 상태에서 복제본 정책을 적용합니다. 호출자가 소유권을 보관한 뒤 Show를 호출합니다.</summary>
        internal static LobbyCharacterVisual Create(Transform worldRoot, GameObject prefab, LobbyCharacterPresentationData presentation)
        {
            if (worldRoot == null) throw new ArgumentNullException(nameof(worldRoot));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));
            // 프리팹과 보정 값은 DB 생성(LobbyModelData·LobbyCharacterPresentationData) 때 검사가 끝났습니다(LR5-3, 로비 리팩토링 2차 설계 N-19).
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var owner = new LobbyCharacterVisual();
            try
            {
                owner._movementRoot = new GameObject("LobbyCharacterRoot");
                owner._movementRoot.SetActive(false);
                owner._movementRoot.transform.SetParent(worldRoot, false);
                var container = new GameObject("LobbyCharacterVisual");
                container.transform.SetParent(owner._movementRoot.transform, false);
                container.transform.SetLocalPositionAndRotation(presentation.CharacterOffset,
                    Quaternion.Euler(0f, presentation.CharacterYaw, 0f));
                container.transform.localScale = Vector3.one * presentation.CharacterScale;
                GameObject instance = UnityEngine.Object.Instantiate(prefab, container.transform, false);
                // 원본 프리팹의 루트 보정 대신 로비 DB의 보정을 적용합니다.
                ChessPieceClone.Prepare(instance);
                return owner;
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

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
            _movementRoot = null;
            if (container == null) return;
            container.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(container);
            else UnityEngine.Object.DestroyImmediate(container);
        }
    }
}
