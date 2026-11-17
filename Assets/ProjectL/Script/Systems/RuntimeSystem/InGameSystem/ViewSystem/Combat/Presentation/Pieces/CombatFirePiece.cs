using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 원거리 클립 공격의 한 발입니다. 접촉을 집계에 알리고 물리 Contact 신호를 내며 종점 도착 때 끝납니다.
    /// 피해 후속 해제는 모든 탄을 받는 집계 조각이 맡습니다.
    /// </summary>
    /// <remarks>
    /// 외부 진행 조각입니다. 투사체 진행은 공격 재생의 투사체 진행(<see cref="CombatProjectileFlights"/>)이 하고, 끝은 맥락의 끝 입구로 바로 받습니다.
    /// 포구 위치는 실행이 빌린 그 유닛의 클립 대여에서 읽습니다(계획 P9). 실행이 취소되면 그 투사체만 회수합니다.
    /// </remarks>
    internal sealed class CombatFirePiece : ICombatPresentationPiece
    {
        private readonly EntityId _attacker;
        private readonly int _point;
        private readonly Func<Vector3, Action<EntityId, bool>, Action, ICombatShot> _fire;
        private readonly Action<EntityId, bool> _contact;
        private ICombatShot _shot;
        private bool _closed;
        private bool _contactSignaled;

        /// <param name="attacker">클립 채널을 빌린 공격자입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="fire">발사 입구입니다. 인자는 (발사 지점, 접촉 콜백(대상, 이 탄의 마지막 접촉인지), 도착 콜백)입니다(<see cref="CombatAttackPlayback.FireShot"/>).</param>
        /// <param name="point">발사 지점 번호입니다. 기존 기본 총구는 0입니다.</param>
        /// <param name="contact">이 발사의 접촉을 집계하는 입구입니다. 관통 탄은 조우마다 부릅니다(관통·죽음의 손 View 설계 §3.1).</param>
        internal CombatFirePiece(EntityId attacker, int point, Func<Vector3, Action<EntityId, bool>, Action, ICombatShot> fire,
            Action<EntityId, bool> contact)
        {
            if (attacker.IsEmpty)
                throw new ArgumentException("쏠 공격자가 비어 있습니다.", nameof(attacker));
            _attacker = attacker;
            if (point < 0) throw new ArgumentOutOfRangeException(nameof(point), point, "발사 지점 번호는 0 이상이어야 합니다.");
            _point = point;
            _fire = fire ?? throw new ArgumentNullException(nameof(fire));
            _contact = contact ?? throw new ArgumentNullException(nameof(contact));
        }

        public bool IsEnded => false;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.Clip(_attacker).TryGetEventPoint(_point, out Vector3 from, out _))
                throw new InvalidOperationException(
                    $"[CombatFirePiece] 발사 지점이 없습니다(외형 표 검사가 막아야 함): {_attacker}, 지점 {_point}");
            Action contact = context.SignalCallback(CombatPieceSignal.Contact);
            Action end = context.EndCallback();
            ICombatShot shot = _fire(from, (target, final) =>
            {
                if (_closed) return;
                _contact(target, final);
                // 물리 Contact 신호(탄별 착탄 소리 시점 등)는 이 탄의 첫 접촉에 한 번입니다.
                if (_contactSignaled) return;
                _contactSignaled = true;
                contact();
            }, () =>
            {
                if (_closed) return;
                _closed = true;
                _shot = null;
                end();
            });
            if (shot == null)
                throw new InvalidOperationException($"[CombatFirePiece] 발사 손잡이가 없습니다: {_attacker}, 지점 {_point}");
            // 발사 안의 동기 접촉이 실행을 취소하거나 바로 도착했으면 뒤늦게 받은 손잡이를 회수합니다.
            if (_closed) shot.Recall();
            else _shot = shot;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
            _closed = true;
            ICombatShot shot = _shot;
            _shot = null;
            shot?.Recall();
        }
    }
}
