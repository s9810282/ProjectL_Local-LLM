using System;
using Cinemachine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>로비 공간의 저작 참조입니다. 런타임 동작은 View 시스템이 담당합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbySceneRoot : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera lobbyCamera;
        [SerializeField] private LobbyCanvasView ui;
        [Header("로비 공간")]
        [FormerlySerializedAs("boardRoot")]
        [SerializeField] private Transform worldRoot;

        internal CinemachineVirtualCamera LobbyCamera => lobbyCamera;
        internal LobbyCanvasView UI => ui;
        internal Transform WorldRoot => worldRoot;

        internal void Validate()
        {
            // 출력 Camera는 씬 소유가 아니라 연결 때 받습니다(카메라 이전 설계 L1). 여기서는 가상 카메라만 봅니다.
            // 이름의 유일성은 카메라 카탈로그가 씬 연결 때 검사합니다.
            if (lobbyCamera == null || lobbyCamera.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("[Lobby] 같은 씬에 로비 가상 카메라가 필요합니다.");
            if (lobbyCamera.Follow != null || lobbyCamera.LookAt != null)
                throw new InvalidOperationException("[Lobby] 고정 카메라는 타깃이 없는 직교 카메라여야 합니다.");

            LobbySceneLayout.Validate(this);
            // 로비 씬 루트의 연결 검사는 이 한 곳입니다(가상 카메라·worldRoot·UI). 로비 연결은 이것만 부릅니다(로비 리팩토링 2차 설계 N-51).
            if (ui == null || ui.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("[Lobby] LobbySceneRoot.ui에 같은 씬의 LobbyCanvasView가 필요합니다.");
            ui.ValidateComposition();
        }
    }
}
