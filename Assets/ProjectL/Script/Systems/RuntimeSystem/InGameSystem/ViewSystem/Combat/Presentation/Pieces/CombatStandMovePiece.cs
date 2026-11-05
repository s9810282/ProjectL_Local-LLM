using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 서는 자리 대여(<see cref="ICombatUnitStand"/>)로 유닛을 지금 자리에서 끝 자리로 옮기는 조각입니다(유닛 행동 설계 AA5). 나가기·돌아오기·제자리 돌기가 같이 씁니다.
    /// 서는 자리 채널은 실행이 빌리고 되돌립니다. Core 위치·걸음 접촉과 상관없는 화면 동작입니다(설계 P3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 끝 자리는 시작할 때 한 번 정합니다(<c>endOf</c>). 그 순간의 대여 자리·Mapper로 계산하므로, 나가기는 그때 보인 사실의 자리를, 돌아오기는 빌릴 때 자리를 씁니다.
    /// 위치는 곧게 잇고, 뛰기면 걸음 높이 식(<see cref="CombatStepHeight.Arc"/>)의 호를 위쪽으로 더합니다. 방향은 같은 진행률로 돌립니다.
    /// </para>
    /// <para>
    /// 그 유닛에 자세 대여가 있으면(피격·사망 반응이 그 자리에서 진행 중) 움직이지 않고 그 프레임은 기다립니다(<see cref="ICombatUnitStand.CanMove"/>).
    /// 대여가 접혔거나(걸음 배치 시작, 설계 AA8) 쓸 수 없게 되면(표시 꺼짐) 아무것도 쓰지 않고 끝납니다. 대여 반환은 실행이 합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatStandMovePiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly Func<ICombatUnitStand, FieldGridMapper, Pose> _endOf;
        private readonly float _duration;
        private readonly CombatStepLook _look;
        private readonly float _hopHeight;
        private ICombatUnitStand _stand;
        private FieldGridMapper _mapper;
        private Pose _start;
        private Pose _end;
        private float _elapsed;
        private bool _ended;

        /// <param name="unit">서는 자리 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="endOf">시작할 때 끝 자리를 정하는 함수입니다(인자: 그 유닛의 서는 자리 대여, Mapper).</param>
        /// <param name="seconds">옮기는 시간(초)입니다. 0이면 움직일 수 있게 되는 첫 순간에 끝 자리에 닿습니다.</param>
        /// <param name="look">옮기는 모양입니다. 뛰기면 <paramref name="hopHeight"/>만큼 호를 그립니다.</param>
        /// <param name="hopHeight">뛰기 높이입니다(걸음 모양 설정의 뛰기 높이, 계획 TP-21). 걷기면 쓰지 않습니다.</param>
        internal CombatStandMovePiece(EntityId unit, Func<ICombatUnitStand, FieldGridMapper, Pose> endOf, float seconds, CombatStepLook look,
            float hopHeight)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("옮길 유닛이 비어 있습니다.", nameof(unit));
            CombatPresentationValueRules.RequireNonNegative(seconds, nameof(seconds));
            CombatPresentationValueRules.RequireNonNegative(hopHeight, nameof(hopHeight));
            if (!CombatEnumValues.IsDefined<CombatStepLook>((int)look))
                throw new ArgumentOutOfRangeException(nameof(look), look, "정의되지 않은 걸음 모양입니다.");
            _unit = unit;
            _endOf = endOf ?? throw new ArgumentNullException(nameof(endOf));
            _duration = seconds;
            _look = look;
            _hopHeight = hopHeight;
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _stand = context.Stand(_unit);
            _mapper = context.Mapper ?? throw new InvalidOperationException("[CombatStandMovePiece] Mapper가 연결되지 않았습니다.");
            _elapsed = 0f;
            if (!_stand.IsAvailable)
            {
                _ended = true;
                return;
            }
            _start = new Pose(_stand.Position, _stand.Rotation);
            _end = _endOf(_stand, _mapper);
            _ended = false;
            Advance(0f);
        }

        public void Tick(float deltaTime) => Advance(deltaTime);

        public void Cancel()
        {
        }

        // 움직일 수 있을 때만 시간을 보탭니다. 끝 자리에 닿으면 끝입니다.
        private void Advance(float deltaTime)
        {
            if (_ended) return;
            if (!_stand.IsAvailable)
            {
                _ended = true;
                return;
            }
            if (!_stand.CanMove) return;
            _elapsed += deltaTime;
            float progress = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            Vector3 position = Vector3.Lerp(_start.position, _end.position, progress);
            if (_look == CombatStepLook.Hop && _hopHeight > 0f)
                position += _mapper.WorldUp.normalized * CombatStepHeight.Arc(0f, 0f, progress, _hopHeight);
            _stand.ApplyStand(position, Quaternion.Slerp(_start.rotation, _end.rotation, progress));
            _ended = progress >= 1f;
        }
    }
}
