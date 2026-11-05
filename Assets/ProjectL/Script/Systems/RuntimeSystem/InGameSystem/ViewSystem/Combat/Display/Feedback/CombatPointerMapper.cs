using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>
    /// 화면 좌표를 칸·격자로 바꾸는 집기·매핑입니다(구조 개선 VR4). 카메라 Ray, UI 가림 Raycast, 필드 Ray 질의를 소유합니다.
    /// </summary>
    /// <remarks>
    /// 수명: 생성(Unbound) → <see cref="Bind"/>(필드 Build 성공 뒤 그 설치의 격자) → <see cref="Unbind"/>(전투 정리·설치 실패, 다음 Bind 허용)
    /// → <see cref="Release"/>(최종 해제, 되살리지 않음). 연결되지 않았거나 해제된 뒤의 Try 계열은 false입니다.
    /// 카메라는 호출마다 지정 카메라 또는 Main Camera로 다시 고르며, UI Raycast 데이터는 같은 EventSystem 동안만 재사용합니다(V1).
    /// 같은 프레임·같은 좌표의 UI 가림 답은 한 번만 구합니다(평가 C7-02).
    /// 표시는 하지 않습니다.
    /// </remarks>
    internal sealed class CombatPointerMapper : ICombatPointerPicker, ICombatPointerMapping
    {
        private readonly float _innerCellInset;
        private readonly List<RaycastResult> _uiHits = new();
        private FieldView _field;
        private Camera _assignedCamera;
        private FieldGridMapper _mapper;
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;
        private bool _isReleased;
        // 이 프레임에 구한 UI 가림 답입니다(평가 C7-02). 프레임·좌표·EventSystem이 같을 때만 다시 씁니다.
        private int _uiAnswerFrame = -1;
        private Vector2 _uiAnswerScreen;
        private bool _uiAnswer;

        internal CombatPointerMapper(FieldView field, Camera assignedCamera, float innerCellInset)
        {
            _field = field != null ? field : throw new System.ArgumentNullException(nameof(field));
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
            _assignedCamera = null;
        }

        public bool TryPickCell(float screenX, float screenY, out CellCoord cell)
        {
            cell = default;
            return TryGetRay(screenX, screenY, out Ray ray) && _field.TryRayToCell(ray, out cell);
        }

        /// <summary>
        /// 화면 좌표가 가리키는 격자 위의 점입니다(고지대 칸은 블록 윗면, 그 밖은 필드 평면, 격자 밖 포함). 경로·공격 Drag가 칸을 셀 때 씁니다.
        /// 규칙은 칸 고르기와 같은 <see cref="FieldView.TryRayToSurface"/> 하나입니다(고지대 View 계획 P14). 바닥 타일 높이를 보지 않는 점이라
        /// 고스트는 <see cref="TryRayToStandingPoint"/>를 씁니다.
        /// </summary>
        internal bool TryRayToSurface(float screenX, float screenY, out Vector3 point)
        {
            point = default;
            return TryGetRay(screenX, screenY, out Ray ray) && _field.TryRayToSurface(ray, out point);
        }

        /// <summary>
        /// 화면 좌표가 가리키는 칸에 설 자리입니다. 가로 자리는 <see cref="TryRayToSurface"/>의 점과 같고, 칸 안이면 높이만 그 칸 표면
        /// (바닥 타일 + 지형, <see cref="FieldGridMapper.OnSurface"/>)으로 맞춥니다. 격자 밖은 포인터 점 그대로입니다.
        /// 이동·배치 고스트가 포인터를 따라갈 때 씁니다(타일 높이 계획 §8.7 L6 — 칸에 붙은 배치 고스트와 같은 높이).
        /// </summary>
        internal bool TryRayToStandingPoint(float screenX, float screenY, out Vector3 point)
        {
            if (!TryRayToSurface(screenX, screenY, out point))
                return false;

            point = _mapper.OnSurface(point);
            return true;
        }

        public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)
        {
            grid = default;
            overCancelArea = false;
            if (!IsBound)
                return false;

            // 실제 UI 레이캐스트로 마우스·터치 입력의 화면 가림을 동일하게 취급한다.
            overCancelArea = HitsUi(screen);

            if (!TryRayToSurface(screen.x, screen.y, out Vector3 point))
                return false;

            grid = _mapper.WorldToGrid(point);
            return _mapper.IsInside(new CellCoord(Mathf.FloorToInt(grid.x), Mathf.FloorToInt(grid.y)));
        }

        /// <summary>칸 중심의 화면 좌표입니다(커서 그리드 스냅). 연결 전·해제 뒤·카메라 없음·카메라 뒤쪽·카메라 화면 밖이면 false입니다.</summary>
        internal bool TryCellCenterToScreen(CellCoord cell, out Vector2 screen)
        {
            screen = default;
            Camera camera = CurrentCamera();
            if (!IsBound || camera == null || !_mapper.IsInside(cell))
                return false;

            // 고지대 칸은 블록 윗면 중심으로 옮겨, 옮긴 커서가 같은 칸으로 다시 잡히게 합니다(고지대 View 계획 P14).
            // 포인터와 같은 격자 높이(바닥 타일 높이는 보지 않음)를 씁니다(타일 높이 설계 TH4 개정 2).
            Vector3 point = camera.WorldToScreenPoint(_mapper.CellOverlayToWorld(cell));
            if (point.z <= 0f)
                return false;

            // 칸 중심이 카메라 화면 밖이면 그 자리로 커서를 옮기지 않는다. 옮기면 커서가 창 밖으로 나간다(평가 C10-07).
            var center = new Vector2(point.x, point.y);
            if (!camera.pixelRect.Contains(center))
                return false;

            screen = center;
            return true;
        }

        /// <summary>
        /// OS 커서를 화면 좌표로 옮깁니다(커서 그리드 스냅). 마우스 장치가 없거나 창이 포커스가 아니면 옮기지 않습니다
        /// (다른 창의 커서를 뺏지 않음). 연결 전·해제 뒤에도 옮기지 않습니다.
        /// </summary>
        internal bool TryWarpCursor(Vector2 screen)
        {
            Mouse mouse = Mouse.current;
            if (!IsBound || mouse == null || !Application.isFocused)
                return false;

            mouse.WarpCursorPosition(screen);
            return true;
        }

        /// <summary>
        /// 화면 좌표 아래에 필드 입력을 막는 UI Graphic이 있는지입니다(행동 버튼 설계 B5).
        /// Hover 전용 통과 계약은 제외한다. 버튼을 누른 떼기를 필드 공격으로 쓰지 않게 Router가 읽습니다.
        /// 연결 전·해제 뒤·EventSystem이 없으면 false입니다.
        /// </summary>
        internal bool IsPointerOverUi(float screenX, float screenY)
        {
            return IsBound && HitsUi(new Vector2(screenX, screenY));
        }

        // 경로 매핑의 취소 영역과 행동 버튼의 UI 가림이 함께 쓰는 유일한 UI 판정이다. Raycast 데이터·목록은 재사용한다(V1).
        // 같은 프레임·같은 좌표의 답은 한 번만 구한다(평가 C7-02). 포인터가 움직인 프레임에 Router·유닛 호버·환경 호버·경로가 같은
        // 좌표로 각자 묻기 때문이다. 그 프레임 안에서 UI가 새로 열려도 그 프레임 동안은 앞의 답을 쓰고, 다음 프레임에 맞춰진다.
        private bool HitsUi(Vector2 screen)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ClearPointerRaycast();
                return false;
            }

            int frame = Time.frameCount;
            if (_uiAnswerFrame == frame && _uiAnswerScreen == screen && _pointerEventSystem == eventSystem)
                return _uiAnswer;

            if (_pointerEventData == null || _pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerEventData = new PointerEventData(eventSystem);
            }

            bool hit = RaycastUi(eventSystem, screen);
            _uiAnswerFrame = frame;
            _uiAnswerScreen = screen;
            _uiAnswer = hit;
            return hit;
        }

        private bool RaycastUi(EventSystem eventSystem, Vector2 screen)
        {
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
                    RaycastResult hit = _uiHits[i];
                    if (!(hit.module is GraphicRaycaster)) continue;
                    // 맞은 객체만 검사한다. 상위 패널/Canvas의 통과 계약으로 실제 버튼까지 무시하지 않는다.
                    if (hit.gameObject != null &&
                        hit.gameObject.TryGetComponent<IUIWorldPointerPassThrough>(out var passThrough) &&
                        passThrough.AllowsWorldPointerThrough)
                        continue;
                    // Hover 배경 아래에 다른 UI가 겹쳐 있으면 그 UI는 여전히 입력을 막는다.
                    return true;
                }

                return false;
            }
            finally
            {
                // 목록 용량만 재사용하고 씬 오브젝트 참조는 다음 입력까지 붙잡지 않는다.
                _uiHits.Clear();
            }
        }

        private Camera CurrentCamera() => _isReleased ? null : _assignedCamera != null ? _assignedCamera : Camera.main;

        private bool TryGetRay(float screenX, float screenY, out Ray ray)
        {
            Camera camera = CurrentCamera();
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
            _uiAnswerFrame = -1;
        }
    }
}
