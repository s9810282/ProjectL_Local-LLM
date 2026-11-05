using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 현재 화면 좌표의 화면 공간 UI 가림을 검사합니다. 루트 Canvas가 월드 공간인 Graphic(로비 방의 시설 안내 글자 등)은
    /// 가림으로 보지 않습니다(로비 리팩토링 2차 설계 M-05). 중첩 Canvas는 자기 직렬화 값이 아니라 루트 Canvas의 렌더 모드를 따르므로
    /// 루트로 판정합니다(예: 공용 설정창 OptionCanvas는 직렬화 값이 월드 공간이지만 화면 공간 루트 아래에 있습니다).
    /// </summary>
    internal sealed class ScreenUIPointerBlocker
    {
        private readonly List<RaycastResult> _hits = new();
        // 포인터 이동마다 GetComponent를 하지 않도록 Raycaster마다 한 번 판정해 둡니다. EventSystem이 바뀌면 비웁니다.
        private readonly Dictionary<GraphicRaycaster, bool> _screenSpace = new();
        private EventSystem _eventSystem;
        private PointerEventData _pointer;

        internal bool IsBlocked(float x, float y)
        {
            EventSystem current = EventSystem.current;
            if (current == null)
            {
                _eventSystem = null;
                _pointer = null;
                return false;
            }
            if (_eventSystem != current || _pointer == null)
            {
                _eventSystem = current;
                _screenSpace.Clear();
                _pointer = new PointerEventData(current);
            }
            _pointer.Reset();
            _pointer.position = new Vector2(x, y);
            _pointer.displayIndex = 0;
            try
            {
                current.RaycastAll(_pointer, _hits);
                for (int i = 0; i < _hits.Count; i++)
                {
                    if (_hits[i].module is GraphicRaycaster raycaster && IsScreenSpace(raycaster))
                        return true;
                }
                return false;
            }
            finally
            {
                _hits.Clear();
            }
        }

        private bool IsScreenSpace(GraphicRaycaster raycaster)
        {
            if (_screenSpace.TryGetValue(raycaster, out bool screen)) return screen;
            Canvas canvas = raycaster.GetComponent<Canvas>();
            screen = canvas == null || canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
            _screenSpace.Add(raycaster, screen);
            return screen;
        }
    }
}
