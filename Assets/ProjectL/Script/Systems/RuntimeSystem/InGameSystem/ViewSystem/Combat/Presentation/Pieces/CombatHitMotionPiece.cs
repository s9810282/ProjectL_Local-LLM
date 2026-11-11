using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 스스로 진행하는 피격 반응 조각입니다(연발 앞 탄의 접촉 피격, 2026-11-11). 반응 부품(<see cref="CombatUnitReactionPresenter"/>)을 이 조각이 갖고 Tick합니다.
    /// </summary>
    /// <remarks>
    /// 보통 피격 반응(<see cref="CombatReactionPiece"/>)은 외부 진행 조각이라 반응 재생기가 진행합니다. 그러나 공격 실행이 히트 전인 동안에는
    /// 반응 재생기가 Tick되지 않으므로(조각 실행 재생기 Tick, 설계 §6 Q1) 연발의 앞 탄처럼 출처를 풀기 전에 내는 피격은 이 조각으로 냅니다.
    /// 움직임 식은 같은 반응 부품 하나입니다. 자세는 조립이 빌린 것(<see cref="CombatPieceChannel.Pose"/>)을 쓰고, 반응 부품이 끝나며 먼저 되돌리면
    /// 실행 끝의 되돌리기는 지난 대여라 아무 일도 하지 않습니다.
    /// </remarks>
    internal sealed class CombatHitMotionPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatUnitReactionPresentationData _settings;
        private readonly CombatCharacterEvent _damage;
        private readonly FieldGridMapper _mapper;
        private readonly CombatUnitReactionPresenter _reaction = new();
        private bool _ended;

        /// <param name="damage">맞은 방향을 정하는 피해 사건입니다(반응 밀림 방향과 같은 원본, <see cref="CombatHitDirection"/>).</param>
        internal CombatHitMotionPiece(EntityId unit, CombatUnitReactionPresentationData settings, CombatCharacterEvent damage, FieldGridMapper mapper)
        {
            if (unit.IsEmpty) throw new ArgumentException("피격 반응 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // 대여를 잃으면(표시가 꺼지거나 풀로 돌아감) 실행이 사라짐으로 알리므로 이 조각은 끝나기만 합니다.
            _reaction.BeginHit(_settings, context.Pose(_unit), _damage, _mapper, () => _ended = true, () => _ended = true);
        }

        public void Tick(float deltaTime) => _reaction.Tick(deltaTime);

        public void Cancel() => _reaction.Cancel();
    }
}
