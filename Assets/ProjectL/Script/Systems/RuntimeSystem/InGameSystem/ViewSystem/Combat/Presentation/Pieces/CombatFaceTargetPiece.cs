using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 공격자를 목표 쪽으로 돌리는 시간 없는 조각입니다(클립 설계 AC7, 계획 T22). 실행이 빌린 자세 대여에 지금 위치 그대로 방향만 씁니다.
    /// 방향 계산은 조립을 만든 처리기가 넘긴 함수(<see cref="CombatAttackFacing.For"/>)라 코드 모션과 같은 방향이 나옵니다.
    /// </summary>
    /// <remarks>정면 공격 모션 조각(<see cref="CombatFacingAttackPiece"/>, 포효의 박치기)과는 다른 조각입니다. 이 조각은 움직이지 않고 돌기만 합니다.</remarks>
    internal sealed class CombatFaceTargetPiece : ICombatPresentationPiece
    {
        private readonly EntityId _attacker;
        private readonly Func<ICombatUnitPose, Quaternion> _facing;

        /// <param name="attacker">자세 채널을 빌린 공격자입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="facing">빌린 자세(지금 위치·방향)에서 볼 방향을 구하는 함수입니다.</param>
        internal CombatFaceTargetPiece(EntityId attacker, Func<ICombatUnitPose, Quaternion> facing)
        {
            if (attacker.IsEmpty)
                throw new ArgumentException("돌릴 공격자가 비어 있습니다.", nameof(attacker));
            _attacker = attacker;
            _facing = facing ?? throw new ArgumentNullException(nameof(facing));
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            ICombatUnitPose pose = context.Pose(_attacker);
            pose.ApplyPose(pose.Position, _facing(pose));
            IsEnded = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
