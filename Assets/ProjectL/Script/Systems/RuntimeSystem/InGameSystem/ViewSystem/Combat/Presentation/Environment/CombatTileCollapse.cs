using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment
{
    /// <summary>
    /// 바닥 교체 외형 하나가 사건 시점에 빠질 때(약한 지반 붕괴)의 떨림 → 가라앉기입니다(타일 교체 설계 TR8).
    /// 시작 자리에서 위치만 바꾸고, 끝내기·돌려주기는 집합 <see cref="CombatTileCollapseSet"/>과 지형 표시가 맡습니다.
    /// </summary>
    /// <remarks>
    /// 떨림은 필드 가로 두 축으로 정해진 사인 흔들림이라 같은 값이면 늘 같은 모양입니다. 두 축의 빠르기를 조금 달리해 한 방향으로만 흔들려 보이지 않게 합니다.
    /// 가라앉기는 유닛 추락과 같은 곡선(<see cref="CombatSinkPiece.SinkDistance"/>)입니다.
    /// </remarks>
    internal sealed class CombatTileCollapse
    {
        // 둘째 축의 빠르기 배율입니다. 첫 축과 맞물리지 않는 값이라 흔들림이 원이나 직선으로 굳지 않습니다.
        private const float SecondAxisRate = 1.37f;
        private readonly Transform _view;
        private readonly Vector3 _origin;
        private readonly Vector3 _across;
        private readonly Vector3 _along;
        private readonly Vector3 _up;
        private readonly CombatTileCollapseSettings _settings;
        private float _elapsed;

        /// <param name="view">흔들고 가라앉힐 외형입니다. 시작 자리는 지금 월드 위치입니다.</param>
        /// <param name="fieldX">필드 가로(X) 축의 월드 방향입니다.</param>
        /// <param name="fieldZ">필드 세로(Z) 축의 월드 방향입니다.</param>
        /// <param name="fieldUp">필드 위쪽 축의 월드 방향입니다.</param>
        internal CombatTileCollapse(Transform view, Vector3 fieldX, Vector3 fieldZ, Vector3 fieldUp, CombatTileCollapseSettings settings)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _across = fieldX.sqrMagnitude > 0f ? fieldX.normalized : Vector3.right;
            _along = fieldZ.sqrMagnitude > 0f ? fieldZ.normalized : Vector3.forward;
            _up = fieldUp.sqrMagnitude > 0f ? fieldUp.normalized : Vector3.up;
            _origin = view.position;
        }

        /// <summary>떨리는 중인지입니다. 이 동안 다음 사건 시작을 막습니다(타일 교체 설계 Q-CL1 A).</summary>
        internal bool IsShaking => _elapsed < _settings.ShakeSeconds;

        /// <summary>떨림과 가라앉기가 모두 끝났는지입니다.</summary>
        internal bool IsDone => _elapsed >= _settings.TotalSeconds;

        /// <summary>시간을 보내고 외형 위치를 놓습니다. 외형이 파괴됐으면 시간만 보냅니다.</summary>
        internal void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_view == null) return;

            if (IsShaking)
            {
                float phase = 2f * Mathf.PI * _settings.ShakeFrequency * _elapsed;
                // 끝으로 갈수록 약해져, 가라앉기가 시작 자리에서 이어질 때 튀지 않습니다(묶음 끝 검토 L7).
                float strength = _settings.ShakeStrength * (1f - (_elapsed / _settings.ShakeSeconds));
                _view.position = _origin + (_across * (strength * Mathf.Sin(phase))) + (_along * (strength * Mathf.Sin(phase * SecondAxisRate)));
                return;
            }

            float sink = CombatSinkPiece.SinkDistance(_elapsed - _settings.ShakeSeconds, _settings.SinkSeconds, _settings.SinkDepth);
            _view.position = _origin - (_up * sink);
        }
    }
}
