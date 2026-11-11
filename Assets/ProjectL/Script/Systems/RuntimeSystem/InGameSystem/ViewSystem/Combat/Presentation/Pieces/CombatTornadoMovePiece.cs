using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 폭풍 몸을 다음 중심까지 옮기는 조각입니다(토네이도 View 설계 TN4). 중심 사건 하나가 조각 하나이고, 끝나면 그 중심의 접촉 사건이 이어집니다.
    /// 시작한 뒤 취소되면 하붑 이동 조각처럼 목표 자리로 옮기고 끝납니다(평가 G1-02). 오류 정지 화면에 칸 사이 몸이 남지 않게 합니다.
    /// 결과 끝 표시 갱신은 그대로 최종 자리로 맞춥니다.
    /// </summary>
    internal sealed class CombatTornadoMovePiece : ICombatPresentationPiece
    {
        private readonly ICombatTornadoField _field;
        private readonly TornadoPresentationSettingsData _settings;
        private readonly long _sourceId;
        private readonly Vector3 _from;
        private readonly Vector3 _to;
        private float _elapsed;
        private bool _begun;

        internal CombatTornadoMovePiece(ICombatTornadoField field, TornadoPresentationSettingsData settings, long sourceId, Vector3 from, Vector3 to)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sourceId = sourceId;
            _from = from;
            _to = to;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _elapsed = 0f;
            IsEnded = false;
            _begun = true;
            _field.MoveBody(_sourceId, _from);
        }

        public void Tick(float deltaTime)
        {
            if (IsEnded) return;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _settings.MoveStepSeconds);
            // 곡선은 들어 올림 접근과 같은 값을 씁니다(계획 TP-1).
            _field.MoveBody(_sourceId, Vector3.LerpUnclamped(_from, _to, _settings.Approach(_elapsed / _settings.MoveStepSeconds)));
            IsEnded = _elapsed >= _settings.MoveStepSeconds;
        }

        public void Cancel()
        {
            if (IsEnded) return;
            IsEnded = true;
            if (_begun) _field.MoveBody(_sourceId, _to);
        }
    }
}
