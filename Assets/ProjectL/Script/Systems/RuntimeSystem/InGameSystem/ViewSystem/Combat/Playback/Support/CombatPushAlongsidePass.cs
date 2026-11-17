using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 밀림과 함께 가는 피격(넉백 피격 연출, 임시 — 2026-11-11 사용자 "피격과 동시에 밀림")의 통과 규칙입니다(피격 구조 정리 설계 HC3).
    /// 그 유닛의 밀림 걸음과, 피격과 밀림 사이에 끼는 그 유닛의 사실·특성 발동 사건을 막지 않습니다. 맞는 순간 함께 밀리기 시작합니다
    /// (같은 유닛 사건은 차례로 재생돼, 끼인 사건이 막히면 밀림 걸음도 그 뒤로 밀립니다 — 2026-11-11 진단 로그).
    /// </summary>
    /// <remarks>
    /// 기다리는 것: 그 유닛의 다른 캐릭터 사건(피격·사망·재배치 — 반응·유닛 실행을 열어 이 실행과 다툼), 행동 사건, 공격 출처를 여는 사건.
    /// 끊기지 않고, 자세를 빌리지 않습니다(걸음 배치와 함께 못 씀). 필드가 없는 객체라 <see cref="Instance"/> 하나를 함께 씁니다(설계 P2).
    /// </remarks>
    internal sealed class CombatPushAlongsidePass : ICombatRunSubjectPass
    {
        internal static readonly CombatPushAlongsidePass Instance = new();

        private CombatPushAlongsidePass()
        {
        }

        public bool Passes(CombatEvent item)
        {
            if (item == null || CombatEventTypeTable.IsActionStart(item) || CombatEventTypeTable.OpensAttackSource(item))
                return false;
            return item is not CombatCharacterEvent character ||
                character.Kind == CombatEventKind.Moved && !CombatMovementKinds.IsSelfDriven(CombatMovementKinds.Of(character));
        }

        public bool IsCutBy(CombatEvent item) => false;

        public bool Cuttable => false;

        public bool ForbidsPose => true;
    }
}
