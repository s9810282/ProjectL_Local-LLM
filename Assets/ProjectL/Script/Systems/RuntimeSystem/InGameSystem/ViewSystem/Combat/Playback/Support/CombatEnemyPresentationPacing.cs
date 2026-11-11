using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>표시 중인 결과와 사건으로 적 행동·턴 간격을 정합니다. 시간 진행과 Core 상태는 소유하지 않습니다.</summary>
    internal sealed class CombatEnemyPresentationPacing
    {
        private readonly float _actionGapSeconds;
        private readonly float _turnGapSeconds;
        private EntityId _lastEnemyActionActor;
        private bool _movementStarted;
        private bool _turnGapPaid;

        internal CombatEnemyPresentationPacing(float actionGapSeconds, float turnGapSeconds)
        {
            ValidateSeconds(actionGapSeconds, nameof(actionGapSeconds));
            ValidateSeconds(turnGapSeconds, nameof(turnGapSeconds));
            _actionGapSeconds = actionGapSeconds;
            _turnGapSeconds = turnGapSeconds;
        }

        /// <summary>결과 사이에는 턴 간격 이력을 유지합니다. 결과마다 "첫 자발 걸음을 셌는지"만 비웁니다.</summary>
        internal void BeginResult(CombatResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _movementStarted = false;
        }

        /// <summary>
        /// 한 결과에서 적의 첫 스스로 하는 걸음만 행동으로 셉니다. 밀린 걸음(넉백 등)은 행동이 아닙니다. 스스로/밀림 판정은 걸음 사건의 Core 이동 원인을 읽는
        /// <see cref="CombatMovementKinds.Of"/> 한 곳입니다(특성 시스템 View 계획 R4·TP-7, 옛 "결과 행위자 + 공격 전" 추론을 대신함).
        /// </summary>
        internal float BeforeStep(CombatCharacterEvent step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            if (step.Expected.Owner != Owner.Enemy || !CombatMovementKinds.IsSelfDriven(CombatMovementKinds.Of(step)) || _movementStarted)
                return 0f;

            _movementStarted = true;
            return BeginEnemyAction(step.Expected.EntityId);
        }

        /// <summary>
        /// 사건 앞 간격입니다. 간격 종류와 행위자는 사건 형식 표(<see cref="CombatEventTypeTable.PacingOf"/>·<see cref="CombatEventTypeTable.ActorOf"/>)가
        /// 원본이고, 행위자는 호출자가 넘깁니다(구조 원칙 평가 R3, View 2라운드 W7-2). Owner는 최신 Buffer가 아니라 표시 중인 해당 행위자의 사실에서 받습니다.
        /// </summary>
        internal float BeforeEvent(CombatEvent combatEvent, EntityId actorId, Owner? actorOwner)
        {
            if (combatEvent == null)
                throw new ArgumentNullException(nameof(combatEvent));

            switch (CombatEventTypeTable.PacingOf(combatEvent))
            {
                // 성공한 공격과, 공격과 같은 행동 경계인 포효입니다. 뒤따르는 넉백 걸음은 Core 이동 원인이 밀림이라 행동으로 세지 않습니다(BeforeStep).
                case CombatEventPacing.Attack:
                case CombatEventPacing.AttackLike:
                    return actorOwner == Owner.Enemy ? BeginEnemyAction(actorId) : 0f;

                // 소환도 소환자의 행동 하나로 셉니다(설계 D7).
                case CombatEventPacing.Action:
                    return actorOwner == Owner.Enemy ? BeginEnemyAction(actorId) : 0f;

                // 턴 안의 갈래는 사건 종류(Core 값)로 가릅니다.
                case CombatEventPacing.Turn:
                    if (combatEvent.Kind == CombatEventKind.EnemyPhaseEnded)
                    {
                        _lastEnemyActionActor = EntityId.Empty;
                        _turnGapPaid = true;
                        return _turnGapPaid ? _turnGapSeconds : 0f;
                    }

                    if (combatEvent.Kind == CombatEventKind.ControllerTurnStarted)
                    {
                        float gap = ((CombatTurnEvent)combatEvent).Final.Controller == Owner.Enemy && !_turnGapPaid ? _turnGapSeconds : 0f;
                        _lastEnemyActionActor = EntityId.Empty;
                        _turnGapPaid = false;
                        return gap;
                    }

                    if (combatEvent.Kind == CombatEventKind.CombatEntered)
                        Clear();
                    return 0f;

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 동작(등장·커지기·공격 연출)을 시작하지 못해 사실만 보인 포효·소환을 보인 뒤 완료 전까지 머무는 시간입니다(보스 활성화 설계 D6·D7·D6b).
        /// 결과 끝에 나와도 재생 강조가 한 프레임에 사라지지 않게 합니다. 동작을 시작한 사건은 호출자가 묻지 않습니다.
        /// </summary>
        internal float HoldAfter(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                throw new ArgumentNullException(nameof(combatEvent));

            return CombatEventTypeTable.HoldsWhenShownOnly(combatEvent) ? _actionGapSeconds : 0f;
        }

        internal void Clear()
        {
            _lastEnemyActionActor = EntityId.Empty;
            _movementStarted = false;
            _turnGapPaid = false;
        }

        // 조건 원본은 View DB 설정 표 한 곳입니다(연출 조립 설계 §9 I-5).
        internal static void ValidateSeconds(float seconds, string name) =>
            ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item.CombatPresentationSettingsData.ValidateSeconds(seconds, name);

        private float BeginEnemyAction(EntityId actor)
        {
            float gap = _lastEnemyActionActor.IsEmpty ? 0f
                : actor == _lastEnemyActionActor ? _actionGapSeconds : _turnGapSeconds;
            _lastEnemyActionActor = actor;
            return gap;
        }
    }
}
