using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 화면 좌표를 칸·손패·격자로 바꾸는 집기·매핑입니다(구조 개선 VR4). 카메라 Ray, UI 가림 Raycast, 필드 Ray·손패 카드 화면 좌표 질의를 소유합니다.
    /// </summary>
    /// <remarks>
    /// 수명: 생성(Unbound) → <see cref="Bind"/>(필드 Build 성공 뒤 그 설치의 격자) → <see cref="Unbind"/>(전투 정리·설치 실패, 다음 Bind 허용)
    /// → <see cref="Release"/>(최종 해제, 되살리지 않음). 연결되지 않았거나 해제된 뒤의 Try 계열은 false입니다.
    /// 카메라는 호출마다 지정 카메라 또는 Main Camera로 다시 고르며, UI Raycast 데이터는 같은 EventSystem 동안만 재사용합니다(V1).
    /// 표시는 하지 않습니다.
    /// </remarks>
    internal sealed class CombatPointerMapper : ICombatPointerPicker, ICombatPointerMapping
    {
        private readonly float _innerCellInset;
        private readonly List<RaycastResult> _uiHits = new();
        private FieldView _field;
        private HandPresenter _hand;
        private Camera _assignedCamera;
        private FieldGridMapper _mapper;
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;
        private bool _isReleased;

        internal CombatPointerMapper(FieldView field, HandPresenter hand, Camera assignedCamera, float innerCellInset)
        {
            _field = field != null ? field : throw new System.ArgumentNullException(nameof(field));
            _hand = hand != null ? hand : throw new System.ArgumentNullException(nameof(hand));
            _assignedCamera = assignedCamera;
            _innerCellInset = innerCellInset;
        }

        public float InnerCellInset => _innerCellInset;

        /// <summary>이번 설치의 격자에 연결됐는지입니다.</summary>
        internal bool IsBound => !_isReleased && _mapper != null;

        /// <summary>필드 Build가 성공한 뒤 그 설치의 격자에 연결합니다. 해제된 뒤에는 연결하지 않습니다.</summary>
        internal void Bind(FieldGridMapper mapper)
        {
            if (_isReleased)
                throw new System.InvalidOperationException("[CombatPointerMapper] 해제된 매퍼는 다시 연결할 수 없습니다.");
            _mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
        }

        /// <summary>전투 정리·설치 실패: 격자와 UI Raycast 재사용 참조를 놓습니다. 다음 설치에서 다시 연결할 수 있습니다.</summary>
        internal void Unbind()
        {
            _mapper = null;
            ClearPointerRaycast();
        }

        /// <summary>최종 해제: 씬 참조를 모두 놓고 이후 호출을 무시합니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            if (_isReleased)
                return;

            _isReleased = true;
            Unbind();
            _field = null;
            _hand = null;
            _assignedCamera = null;
        }

        public bool TryPickCell(float screenX, float screenY, out CellCoord cell)
        {
            cell = default;
            return TryGetRay(screenX, screenY, out Ray ray) && _field.TryRayToCell(ray, out cell);
        }

        // 손패는 화면 공간 카드라 카메라 Ray 없이 화면 좌표로 판정한다(손패 카드화 설계 C7). 연결 수명 규칙은 같다.
        public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId)
        {
            entityId = EntityId.Empty;
            return IsBound && _hand.TryPickUnit(new Vector2(screenX, screenY), out entityId);
        }

        public bool IsOverHandDropZone(float screenX, float screenY)
        {
            return IsBound && _hand.ContainsDropPoint(new Vector2(screenX, screenY));
        }

        /// <summary>화면 좌표가 가리키는 필드 평면의 점입니다(격자 밖 포함). 고스트를 포인터에 붙일 때 씁니다.</summary>
        internal bool TryRayToFieldPlane(float screenX, float screenY, out Vector3 point)
        {
            point = default;
            return TryGetRay(screenX, screenY, out Ray ray) && _field.TryRayToFieldPlane(ray, out point);
        }

        public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)
        {
            grid = default;
            overCancelArea = false;
            if (!IsBound)
                return false;

            overCancelArea = IsOverHandDropZone(screen.x, screen.y);
            // 실제 UI 레이캐스트로 마우스·터치 입력의 화면 가림을 동일하게 취급한다.
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ClearPointerRaycast();
            }
            else
            {
                if (_pointerEventData == null || _pointerEventSystem != eventSystem)
                {
                    _pointerEventSystem = eventSystem;
                    _pointerEventData = new PointerEventData(eventSystem);
                }

                // 포인터 이벤트를 전달하지 않는 Raycast 전용 데이터다. Reset은 used만 초기화한다.
                _pointerEventData.Reset();
                _pointerEventData.position = screen;
                _pointerEventData.displayIndex = 0;
                _uiHits.Clear();
                try
                {
                    eventSystem.RaycastAll(_pointerEventData, _uiHits);
                    for (int i = 0; i < _uiHits.Count; i++)
                    {
                        if (_uiHits[i].module is GraphicRaycaster)
                        {
                            overCancelArea = true;
                            break;
                        }
                    }
                }
                finally
                {
                    // 목록 용량만 재사용하고 씬 오브젝트 참조는 다음 입력까지 붙잡지 않는다.
                    _uiHits.Clear();
                }
            }

            if (!TryRayToFieldPlane(screen.x, screen.y, out Vector3 point))
                return false;

            grid = _mapper.WorldToGrid(point);
            return _mapper.IsInside(new CellCoord(Mathf.FloorToInt(grid.x), Mathf.FloorToInt(grid.y)));
        }

        private bool TryGetRay(float screenX, float screenY, out Ray ray)
        {
            Camera camera = _isReleased ? null : _assignedCamera != null ? _assignedCamera : Camera.main;
            if (!IsBound || camera == null)
            {
                ray = default;
                return false;
            }

            ray = camera.ScreenPointToRay(new Vector3(screenX, screenY, 0f));
            return true;
        }

        private void ClearPointerRaycast()
        {
            _pointerEventData = null;
            _pointerEventSystem = null;
            _uiHits.Clear();
        }
    }
}
