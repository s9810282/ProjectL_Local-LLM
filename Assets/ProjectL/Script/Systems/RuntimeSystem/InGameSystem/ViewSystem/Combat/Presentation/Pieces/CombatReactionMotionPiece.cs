using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 피격·사망의 코드 움직임(밀렸다 돌아오며 젖힘 / 쓰러짐·랙돌·디졸브)을 스스로 진행하는 조각입니다(피격 구조 정리 설계 HC1).
    /// 반응 부품(<see cref="CombatUnitReactionPresenter"/>)을 이 조각이 하나 갖고 Tick·취소합니다. 만들기는 <see cref="Hit"/>·<see cref="Death"/> 둘입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 자세는 조립이 빌린 것(<see cref="CombatPieceChannel.Pose"/>)을 씁니다. 반응 부품이 끝나며 자세를 먼저 되돌리면 실행 끝의 되돌리기는 지난 대여라 아무 일도 하지 않습니다.
    /// 대여를 잃으면(표시가 꺼지거나 풀로 돌아감) 실행이 사라짐으로 알리므로 이 조각은 끝나기만 합니다.
    /// </para>
    /// <para>
    /// 조각 실행 재생기가 진행하므로 행동 실행이 히트 전이어도 돕니다(연발 앞 탄처럼 출처를 풀기 전에 내는 피격도 이 조각, 2026-11-11).
    /// 옛 길(외부 진행 조각 + 반응 부품 목록 + 반응 재생기)은 이 조각으로 옮기고 걷었습니다(피격 구조 정리 계획 HC-b1 피격·HC-b2 사망·HC-b3 걷기).
    /// </para>
    /// </remarks>
    internal sealed class CombatReactionMotionPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly Action<CombatUnitReactionPresenter, ICombatUnitPose, Action> _begin;
        private readonly CombatUnitReactionPresenter _reaction = new();
        private readonly Action _end;
        private bool _ended;

        private CombatReactionMotionPiece(EntityId unit, Action<CombatUnitReactionPresenter, ICombatUnitPose, Action> begin)
        {
            if (unit.IsEmpty) throw new ArgumentException("반응 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _begin = begin;
            // 반응 부품의 완료와 "쓸 수 없음"(자세를 잃음) 둘 다 끝으로만 받습니다. 쓸 수 없음은 실행이 대여 취소(OnLeaseCancelled)로
            // 사라짐을 먼저 알리므로, 이 조각이 따로 실패를 내지 않습니다(묶음 끝 검토 D6).
            _end = () => _ended = true;
        }

        /// <summary>피격 반응 조각입니다. 맞은 방향으로 밀렸다 돌아오며 젖힙니다(반응 값의 피격 시간·밀림·각도).</summary>
        /// <param name="damage">맞은 방향을 정하는 피해 사건입니다(반응 밀림 방향과 같은 원본, <see cref="CombatHitDirection"/>).</param>
        internal static CombatReactionMotionPiece Hit(EntityId unit, CombatUnitReactionPresentationData settings, CombatCharacterEvent damage,
            FieldGridMapper mapper)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return new CombatReactionMotionPiece(unit, (reaction, pose, end) => reaction.BeginHit(settings, pose, damage, mapper, end, end));
        }

        /// <summary>
        /// 사망 반응 조각입니다. 쓰러지거나(랙돌을 쓸 수 있으면 랙돌) 머문 뒤 디졸브합니다(반응 값의 사망 시간·머묾 시간).
        /// 끝나거나 취소되면 반응 부품이 <paramref name="releaseModel"/>을 불러 사망 유지를 놓습니다.
        /// </summary>
        /// <param name="launchRagdoll">공격에 맞아 죽었으면 맞은 방향으로 랙돌을 날리는지입니다.</param>
        /// <param name="releaseModel">사망 유지 놓기입니다. 여러 번 불려도 됩니다.</param>
        internal static CombatReactionMotionPiece Death(EntityId unit, CombatUnitReactionPresentationData settings, CombatCharacterEvent death,
            FieldGridMapper mapper, bool launchRagdoll, Action releaseModel)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (death == null) throw new ArgumentNullException(nameof(death));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (releaseModel == null) throw new ArgumentNullException(nameof(releaseModel));
            return new CombatReactionMotionPiece(unit, (reaction, pose, end) =>
                reaction.BeginDeath(settings, pose, death, mapper, launchRagdoll, releaseModel, end, end));
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _begin(_reaction, context.Pose(_unit), _end);
        }

        public void Tick(float deltaTime) => _reaction.Tick(deltaTime);

        public void Cancel() => _reaction.Cancel();
    }
}
