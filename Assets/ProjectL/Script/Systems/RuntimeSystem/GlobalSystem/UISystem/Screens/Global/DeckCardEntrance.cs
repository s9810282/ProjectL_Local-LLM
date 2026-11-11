using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>기존 네 카드의 순차 등장 시간과 모드별 자세를 계산합니다. 자세 적용은 부채꼴 배치가 맡습니다.</summary>
    [Serializable]
    public sealed class DeckCardEntrance
    {
        /// <summary>직렬화 값 0은 기존 상승을 유지합니다.</summary>
        public enum EntranceMode
        {
            Rise = 0,
            FanSpread = 1
        }

        /// <summary>패널 하단 기준 위치와 Z 각도입니다. 최종 자세의 원본은 부채꼴 배치입니다.</summary>
        internal readonly struct Pose
        {
            internal Pose(Vector2 position, float angle)
            {
                Position = position;
                Angle = angle;
            }

            internal Vector2 Position { get; }
            internal float Angle { get; }
        }

        [Tooltip("Rise는 기존 아래에서 상승, Fan Spread는 왼쪽부터 간격과 회전을 펼칩니다. 다음 등장부터 적용합니다.")]
        [SerializeField] private EntranceMode mode = EntranceMode.Rise;
        [Tooltip("카드 한 장이 최종 자세에 도달하는 시간입니다. 두 모드가 함께 사용합니다.")]
        [SerializeField, Min(0f)] private float cardSeconds = 0.28f;
        [Tooltip("다음 카드가 움직이기 시작할 때까지의 간격입니다.")]
        [SerializeField, Min(0f)] private float staggerSeconds = 0.07f;
        [Header("Fan Spread")]
        [Tooltip("최종 간격·각도 차이 대비 처음 접힌 비율입니다. 작을수록 왼쪽에 좁게 모입니다.")]
        [SerializeField, Range(0f, 1f)] private float foldedSpacingRatio = 0.12f;
        [Tooltip("카드 높이 대비 처음 내려둔 거리입니다. 펼치는 동안 짧게 올라옵니다.")]
        [SerializeField, Range(0f, 0.5f)] private float fanLiftRatio = 0.12f;
        [Tooltip("네 장이 함께 짧게 올라오며 선명해지는 시간입니다. 펼치기와 동시에 진행합니다.")]
        [SerializeField, Min(0f)] private float fanRevealSeconds = 0.1f;
        private RectTransform _panel;
        private RectTransform _canvas;
        private EntranceMode _activeMode;
        private float _elapsed;
        private float _duration;
        private bool _playing;
        private int _cardCount;
        private int _startedCards;

        internal bool IsPlaying => _playing;
        internal float Alpha => _playing && _activeMode == EntranceMode.FanSpread ? FanReveal : 1f;

        internal void Begin(RectTransform panel, int count)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (mode != EntranceMode.Rise && mode != EntranceMode.FanSpread)
                throw new InvalidOperationException($"[DeckCardEntrance] UIId.UnitCard: 지원하지 않는 mode={mode}입니다.");
            ValidateTime(cardSeconds, nameof(cardSeconds));
            ValidateTime(staggerSeconds, nameof(staggerSeconds));
            if (mode == EntranceMode.FanSpread)
            {
                ValidateRatio(foldedSpacingRatio, nameof(foldedSpacingRatio), 1f);
                ValidateRatio(fanLiftRatio, nameof(fanLiftRatio), 0.5f);
                ValidateTime(fanRevealSeconds, nameof(fanRevealSeconds));
            }
            float duration = cardSeconds + staggerSeconds * (count - 1);
            if (mode == EntranceMode.FanSpread) duration = Mathf.Max(duration, fanRevealSeconds);
            ValidateTime(duration, "전체 등장 시간");
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null || !(canvas.rootCanvas.transform is RectTransform canvasRect))
                throw new InvalidOperationException($"[DeckCardEntrance] UIId.UnitCard, '{panel.name}': 상위 Canvas의 RectTransform이 필요합니다.");
            _panel = panel;
            _canvas = canvasRect;
            _activeMode = mode;
            _elapsed = 0f;
            _duration = duration;
            _playing = _duration > 0f;
            _cardCount = count;
            _startedCards = 0;
        }

        /// <summary>시간을 진행하고 이번 호출에서 새로 움직이기 시작한 카드 수를 반환합니다.</summary>
        internal int Tick(float deltaTime)
        {
            if (!_playing) return 0;
            _elapsed = Mathf.Min(_duration, _elapsed + Mathf.Max(0f, deltaTime));
            int previousStarted = _startedCards;
            while (_startedCards < _cardCount && _elapsed >= SlotStart(_startedCards))
                _startedCards++;
            if (_elapsed >= _duration) _playing = false;
            return _startedCards - previousStarted;
        }

        /// <summary>최종 자세와 첫 카드의 기준점으로 현재 자세를 만듭니다. 최종 배치 규칙은 복제하지 않습니다.</summary>
        internal Pose Evaluate(int slot, Pose rest, Pose first, Vector2 cardSize)
        {
            if (!_playing) return rest;
            float eased = SlotProgress(slot);
            if (_activeMode == EntranceMode.FanSpread)
            {
                Vector2 foldedPosition = Vector2.Lerp(first.Position, rest.Position, foldedSpacingRatio);
                Vector2 position = Vector2.Lerp(foldedPosition, rest.Position, eased);
                position.y -= cardSize.y * fanLiftRatio * (1f - FanReveal);
                float foldedAngle = Mathf.Lerp(first.Angle, rest.Angle, foldedSpacingRatio);
                return new Pose(position, Mathf.Lerp(foldedAngle, rest.Angle, eased));
            }

            // Rise는 기존 회전과 X를 유지하고, 카드 윗부분까지 화면 아래에서 올라옵니다.
            float radians = rest.Angle * Mathf.Deg2Rad;
            float restingTop = rest.Position.y + cardSize.y * Mathf.Cos(radians) + cardSize.x * 0.5f * Mathf.Abs(Mathf.Sin(radians));
            float bottom = _panel.InverseTransformPoint(_canvas.TransformPoint(new Vector3(0f, _canvas.rect.yMin, 0f))).y - _panel.rect.yMin;
            float distance = Mathf.Max(0f, restingTop - bottom + 12f);
            return new Pose(rest.Position + Vector2.down * (distance * (1f - eased)), rest.Angle);
        }

        private float SlotProgress(int slot)
        {
            float localTime = _elapsed - SlotStart(slot);
            float t = localTime < 0f ? 0f : cardSeconds > 0f ? Mathf.Clamp01(localTime / cardSeconds) : 1f;
            return EaseOut(t);
        }

        private float SlotStart(int slot) => slot * staggerSeconds;

        private float FanReveal => fanRevealSeconds > 0f ? EaseOut(Mathf.Clamp01(_elapsed / fanRevealSeconds)) : 1f;
        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        internal void Reset()
        {
            _playing = false;
            _elapsed = _duration = 0f;
            _cardCount = _startedCards = 0;
            _panel = _canvas = null;
        }

        private static void ValidateRatio(float value, string field, float maximum)
        {
            ValidateTime(value, field);
            if (value > maximum)
                throw new InvalidOperationException($"[DeckCardEntrance] UIId.UnitCard: {field}는 0~{maximum} 범위여야 합니다.");
        }

        private static void ValidateTime(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"[DeckCardEntrance] UIId.UnitCard: {field}는 유한한 0 이상이어야 합니다.");
        }
    }
}
