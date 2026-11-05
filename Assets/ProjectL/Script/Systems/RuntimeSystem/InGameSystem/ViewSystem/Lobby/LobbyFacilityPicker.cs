using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 출력 카메라와 저작한 시설 Collider로 시설을 선택합니다. 시설마다 같은 객체의 클릭 영역은 만들 때 한 번 찾아 둡니다.
    /// 포인터를 움직일 때마다 읽는 런타임 캐시라 DB 입력 계층(<see cref="LobbyFacilityView"/>)이 아니라 여기에 둡니다(로비 리팩토링 2차 설계 N-59).
    /// </summary>
    internal sealed class LobbyFacilityPicker
    {
        private readonly Camera _camera;
        private readonly IReadOnlyList<LobbyFacilityView> _facilities;
        private readonly BoxCollider[] _colliders;

        internal LobbyFacilityPicker(Camera camera, IReadOnlyList<LobbyFacilityView> facilities)
        {
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
            // 구성(같은 객체의 활성 BoxCollider 하나)은 DB 생성 때 원본 프리팹에서 검사가 끝났습니다(TP-R13).
            _colliders = new BoxCollider[facilities.Count];
            for (int i = 0; i < facilities.Count; i++)
                _colliders[i] = facilities[i] != null ? facilities[i].GetComponent<BoxCollider>() : null;
        }
        internal bool TryPick(float x, float y, out LobbyFacilityView facility)
        {
            facility = null;
            if (_camera == null || !float.IsFinite(x) || !float.IsFinite(y) || !_camera.pixelRect.Contains(new Vector2(x, y)))
                return false;

            Ray ray = _camera.ScreenPointToRay(new Vector3(x, y, 0f));
            float nearest = _camera.farClipPlane;
            // 저작된 선택 Collider만 검사하므로 캐릭터·바닥을 시설로 선택하지 않습니다.
            for (int i = 0; i < _facilities.Count; i++)
            {
                LobbyFacilityView candidate = _facilities[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;
                Collider collider = _colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;
                if (collider.Raycast(ray, out RaycastHit hit, nearest))
                {
                    nearest = hit.distance;
                    facility = candidate;
                }
            }
            return facility != null;
        }
    }
}
