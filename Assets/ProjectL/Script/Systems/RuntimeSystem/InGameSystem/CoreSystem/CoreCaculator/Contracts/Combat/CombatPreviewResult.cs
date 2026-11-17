using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 요청 번호로 식별하는 미리보기 응답이다. 게임 변경 결과와 수명을 분리한다.</summary>
    public sealed class CombatPreviewResult
    {
        public int RequestNumber { get; }
        public AttackRangeResult? Range { get; }
        public AttackPreview? Attack { get; }
        public MovePreview? Move { get; }
        public MovementRangeResult? MoveRange { get; }
        public CombatPlacementPreview? Placement { get; }
        public CombatActionAvailability ActionAvailability { get; }
        public MovementPreviewContext MovementContext { get; }
        /// <summary>필드 유닛 하나의 공격 범위 칸 조회 응답이다(호버 공격 범위 설계 UR2).</summary>
        public UnitAttackRangeResult? UnitRange { get; }
        /// <summary>보스가 지금 포효하면 어떻게 되는지의 예고 응답이다(포효 예고 설계 RF2).</summary>
        public RoarForecastResult? RoarForecast { get; }
        public PlayerSkillAvailability SkillAvailability { get; }
        public PlayerSkillSelectionResult SkillSelection { get; }
        public PlayerSkillForecast SkillForecast { get; }
        public PlatformForecastResult PlatformForecast { get; }
        /// <summary>자격 아군 모두의 일반 행동 가능 여부 묶음 응답이다(입력 유닛 순, 새 SSOT 설계 SN2). 자격 아군이 없으면 빈 목록이다.</summary>
        public IReadOnlyList<CombatActionAvailability> ActionAvailabilities { get; }
        private CombatPreviewResult(int number, AttackRangeResult? range = null, AttackPreview? attack = null,
            MovePreview? move = null, MovementRangeResult? moveRange = null, CombatPlacementPreview? placement = null,
            CombatActionAvailability actionAvailability = null, MovementPreviewContext movementContext = null,
            UnitAttackRangeResult? unitRange = null, RoarForecastResult? roarForecast = null,
            PlayerSkillAvailability skillAvailability = null, PlayerSkillSelectionResult skillSelection = null, PlayerSkillForecast skillForecast = null,
            IReadOnlyList<CombatActionAvailability> actionAvailabilities = null, PlatformForecastResult platformForecast = null)
        {
            if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
            if ((range.HasValue ? 1 : 0) + (attack.HasValue ? 1 : 0) + (move.HasValue ? 1 : 0) +
                (moveRange.HasValue ? 1 : 0) + (placement.HasValue ? 1 : 0) + (actionAvailability != null ? 1 : 0) +
                (unitRange.HasValue ? 1 : 0) + (roarForecast.HasValue ? 1 : 0) + (skillAvailability != null ? 1 : 0) +
                (skillSelection != null ? 1 : 0) + (skillForecast != null ? 1 : 0) + (actionAvailabilities != null ? 1 : 0) +
                (platformForecast != null ? 1 : 0) != 1)
                throw new ArgumentException("미리보기 내용은 하나만 필요합니다.");
            RequestNumber = number; Range = range; Attack = attack; Move = move; MoveRange = moveRange; Placement = placement;
            ActionAvailability = actionAvailability;
            MovementContext = movementContext;
            UnitRange = unitRange;
            RoarForecast = roarForecast;
            SkillAvailability = skillAvailability;
            SkillSelection = skillSelection;
            SkillForecast = skillForecast;
            ActionAvailabilities = actionAvailabilities;
            PlatformForecast = platformForecast;
        }
        public static CombatPreviewResult ForPlatformForecast(int number, PlatformForecastResult forecast) =>
            new(number, platformForecast: forecast ?? throw new ArgumentNullException(nameof(forecast)));
        public static CombatPreviewResult ForSkillAvailability(int number, PlayerSkillAvailability availability) =>
            new(number, skillAvailability: availability ?? throw new ArgumentNullException(nameof(availability)));
        public static CombatPreviewResult ForSkillSelection(int number, PlayerSkillSelectionResult selection) =>
            new(number, skillSelection: selection ?? throw new ArgumentNullException(nameof(selection)));
        public static CombatPreviewResult ForSkillForecast(int number, PlayerSkillForecast forecast) =>
            new(number, skillForecast: forecast ?? throw new ArgumentNullException(nameof(forecast)));
        public static CombatPreviewResult ForAttackRange(int number, AttackRangeResult range)
        {
            if (range.AttackerId.IsEmpty || (range.Succeeded == (range.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("범위 결과가 초기화되지 않았습니다.", nameof(range));
            return new CombatPreviewResult(number, range, null);
        }
        public static CombatPreviewResult ForAttack(int number, AttackPreview attack)
        {
            if (attack.AttackerId.IsEmpty || (attack.CanAttack == (attack.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("공격 예상 결과가 초기화되지 않았습니다.", nameof(attack));
            return new CombatPreviewResult(number, null, attack);
        }
        public static CombatPreviewResult ForMove(int number, MovePreview move, MovementPreviewContext context = null) => new(number, move: move, movementContext: context);
        public static CombatPreviewResult ForMoveRange(int number, MovementRangeResult range) => new(number, moveRange: range);
        public static CombatPreviewResult ForPlacement(int number, CombatPlacementPreview placement) => new(number, placement: placement);
        public static CombatPreviewResult ForActionAvailability(int number, CombatActionAvailability availability) =>
            new(number, actionAvailability: availability ?? throw new ArgumentNullException(nameof(availability)));
        public static CombatPreviewResult ForUnitAttackRange(int number, UnitAttackRangeResult range)
        {
            if (range.UnitId.IsEmpty || (range.Succeeded == (range.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("유닛 공격 범위 결과가 초기화되지 않았습니다.", nameof(range));
            return new CombatPreviewResult(number, unitRange: range);
        }
        public static CombatPreviewResult ForRoarForecast(int number, RoarForecastResult forecast)
        {
            // default 값은 생성 검사를 건너뛰므로 여기서 한 번 더 본다(다른 결과와 같은 규칙).
            if (forecast.BossId.IsEmpty || (forecast.Succeeded == (forecast.FailureReason != AttackFailureReason.None)))
                throw new ArgumentException("포효 예고 결과가 초기화되지 않았습니다.", nameof(forecast));
            return new CombatPreviewResult(number, roarForecast: forecast);
        }
        /// <summary>
        /// 행동 가능 여부 묶음 응답을 만든다(새 SSOT 설계 SN2). 목록은 배열로 복사하며, 빈 항목·빈 개체·같은 개체 두 번은 받지 않는다.
        /// 빈 목록은 받는다(자격 아군 0명).
        /// </summary>
        public static CombatPreviewResult ForActionAvailabilities(int number, IReadOnlyList<CombatActionAvailability> availabilities)
        {
            if (availabilities == null) throw new ArgumentNullException(nameof(availabilities));
            var copy = new CombatActionAvailability[availabilities.Count];
            var actors = new HashSet<EntityId>();
            for (int i = 0; i < copy.Length; i++)
            {
                CombatActionAvailability availability = availabilities[i];
                if (availability == null || availability.ActorId.IsEmpty || !actors.Add(availability.ActorId))
                    throw new ArgumentException("행동 가능 여부 묶음에 빈 항목·빈 개체·같은 개체가 있습니다.", nameof(availabilities));
                copy[i] = availability;
            }
            return new CombatPreviewResult(number, actionAvailabilities: copy);
        }
    }
}
