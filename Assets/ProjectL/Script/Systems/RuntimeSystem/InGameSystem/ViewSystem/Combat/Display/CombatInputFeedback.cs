using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 이 씬 연결의 입력 피드백 계약 묶음입니다(씬 표시 나누기 설계 DS2). 입력 Factory가 계약 이름으로 꺼내 각 입력 기능에 넘깁니다.
    /// </summary>
    /// <remarks>
    /// 계약은 실제로 일하는 객체(주인)가 구현합니다. 어느 주인이 어느 계약을 맡는지는 이 생성자 한 곳이 정합니다(계획 TP-1).
    /// 표시(<see cref="CombatSceneDisplay"/>)는 이 묶음을 넘기기만 하고 계약을 구현하지 않습니다. 해제 뒤 규칙(D9)은 주인마다
    /// 공유 수명 또는 자기 해제 상태로 지킵니다(설계 DS1·P4). 공격 모드 대상만 해제 뒤 값을 저장하고 그리지 않습니다(설계 P10).
    /// 새 입력 피드백 계약은 속성 하나와 그 주인으로 더합니다. 표시는 고치지 않습니다.
    /// </remarks>
    internal sealed class CombatInputFeedback
    {
        /// <param name="pointer">집기·매핑·UI 가림·커서 장치의 주인입니다.</param>
        /// <param name="placement">배치 Drag 고스트·원본 숨김의 주인입니다.</param>
        /// <param name="field">호버 칸·기믹 범위·본부 범위의 주인입니다.</param>
        /// <param name="attack">공격 범위·호버 범위·궤적·넉백 미리보기·포효 예고·플레이어 스킬 표시의 주인입니다.</param>
        /// <param name="events">공격 모드 대상 이펙트의 주인입니다.</param>
        /// <param name="movement">이동 초안·범위·편집의 주인입니다.</param>
        /// <param name="hud">가방 끌기 그림을 가방에 넘기는 HUD 표면입니다.</param>
        internal CombatInputFeedback(CombatPointerMapper pointer, CombatPlacementFeedback placement, FieldView field,
            CombatAttackFeedback attack, CombatSceneEventPresenter events, CombatMovementPlanningPresenter movement, CombatHudSurfaces hud)
        {
            if (pointer == null) throw new ArgumentNullException(nameof(pointer));
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (movement == null) throw new ArgumentNullException(nameof(movement));
            if (hud == null) throw new ArgumentNullException(nameof(hud));

            Picker = pointer;
            Mapping = pointer;
            Occlusion = pointer;
            Cursor = pointer;
            Placement = placement;
            HoverCell = field;
            GimmickRange = field;
            NexusRange = field;
            AttackRange = attack;
            UnitRange = attack;
            Trajectory = attack;
            Knockback = attack;
            // 포효 예고는 넉백 미리보기를 공격 조준과 나눠 쓰므로 그 주인이 맡습니다(포효 예고 계획 TP-19).
            RoarForecast = attack;
            // 플레이어 스킬 표시는 영향 칸·궤적·넉백·예상 체력의 주인이 맡습니다(전투 스킬 View 계획 TP-3).
            PlayerSkill = attack;
            AttackModeTargets = events;
            Movement = movement;
            ItemDrag = hud;
        }

        /// <summary>화면 좌표 → 칸 집기입니다.</summary>
        internal ICombatPointerPicker Picker { get; }

        /// <summary>화면 좌표 → 격자 매핑과 칸 안쪽 여백입니다.</summary>
        internal ICombatPointerMapping Mapping { get; }

        /// <summary>필드 입력을 막는 UI 가림 판정입니다.</summary>
        internal ICombatPointerOcclusion Occlusion { get; }

        /// <summary>커서 그리드 스냅 장치입니다.</summary>
        internal ICombatCursorDevice Cursor { get; }

        /// <summary>배치 Drag 고스트·원본 숨김입니다.</summary>
        internal ICombatPlacementFeedback Placement { get; }

        /// <summary>호버 칸입니다(호버 추적이 씀).</summary>
        internal ICombatHoverCellFeedback HoverCell { get; }

        /// <summary>공격 조준의 범위·영향·막힘·예상 체력입니다.</summary>
        internal ICombatAttackRangeFeedback AttackRange { get; }

        /// <summary>유닛·환경 위험 호버 범위입니다.</summary>
        internal ICombatUnitRangeFeedback UnitRange { get; }

        /// <summary>공격 궤적입니다.</summary>
        internal ICombatAttackTrajectoryFeedback Trajectory { get; }

        /// <summary>넉백 미리보기입니다.</summary>
        internal ICombatKnockbackPreviewFeedback Knockback { get; }

        /// <summary>공격 모드 대상 이펙트입니다.</summary>
        internal ICombatAttackModeTargets AttackModeTargets { get; }

        /// <summary>이동 초안·범위·편집입니다.</summary>
        internal ICombatMovementFeedback Movement { get; }

        /// <summary>기믹(낙석·하붑 등) 호버 범위입니다.</summary>
        internal ICombatGimmickRangeFeedback GimmickRange { get; }

        /// <summary>본부 증원 호버 범위입니다.</summary>
        internal ICombatNexusRangeFeedback NexusRange { get; }

        /// <summary>포효 예고의 바닥 범위·넉백 미리보기입니다(포효 예고 설계 RF4·RF5).</summary>
        internal ICombatRoarForecastFeedback RoarForecast { get; }

        /// <summary>플레이어 액티브 스킬 모드의 후보·점·선·예보 그림입니다(전투 스킬 View 설계 SK2).</summary>
        internal ICombatPlayerSkillFeedback PlayerSkill { get; }

        /// <summary>끄는 아이템 그림입니다.</summary>
        internal ICombatItemDragFeedback ItemDrag { get; }
    }
}
