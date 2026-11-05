using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// 버튼에 마우스를 올리면 살짝 눌리고, 누르면 더 눌리는 연출입니다. 대상(보통 버튼 글자·그림)의 크기만 바꾸며 입력·클릭 처리는 하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 버튼 자신이 아니라 자식을 줄입니다. 클릭 영역이 줄면 가장자리에서 들어감·나감이 반복되기 때문입니다.
    /// 일시 정지 중에도 UI가 움직이도록 시간 배율과 무관한 시간을 씁니다. 버튼이 누를 수 없거나 꺼지면 원래 크기로 돌아갑니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class UIButtonPressEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("크기를 바꿀 대상입니다. 버튼의 자식(글자·그림)을 연결합니다.")]
        [SerializeField] private RectTransform target;
        [Tooltip("마우스를 올렸을 때의 크기 배율입니다.")]
        [SerializeField, Range(.5f, 1f)] private float hoverScale = .93f;
        [Tooltip("누르고 있을 때의 크기 배율입니다.")]
        [SerializeField, Range(.5f, 1f)] private float pressedScale = .86f;
        [Tooltip("다음 크기까지 가는 초입니다.")]
        [SerializeField, Min(0f)] private float duration = .06f;

        private Selectable _selectable;
        private Vector3 _baseScale;
        private bool _hasBase;
        private bool _hovered;
        private bool _pressed;
        private float _current = 1f;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            CaptureBase();
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _current = 1f;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _pressed = false;
        }

        private void Update()
        {
            float goal = Goal();
            if (Mathf.Approximately(_current, goal)) return;
            _current = duration <= 0f
                ? goal
                : Mathf.MoveTowards(_current, goal, (1f - pressedScale) / duration * Time.unscaledDeltaTime);
            Apply();
        }

        private float Goal()
        {
            if (_selectable != null && !_selectable.IsInteractable()) return 1f;
            if (_pressed) return pressedScale;
            return _hovered ? hoverScale : 1f;
        }

        private void CaptureBase()
        {
            if (_hasBase || target == null) return;
            _baseScale = target.localScale;
            _hasBase = true;
        }

        private void Apply()
        {
            CaptureBase();
            if (_hasBase) target.localScale = _baseScale * _current;
        }
    }
}
