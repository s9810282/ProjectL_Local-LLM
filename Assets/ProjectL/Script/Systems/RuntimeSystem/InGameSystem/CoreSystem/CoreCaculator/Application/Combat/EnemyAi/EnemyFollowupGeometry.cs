using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 이동 투영 없이 이동 끝 칸에서의 같은 진영 단계 공격 하나를 추정하기 위한 세계다(적 AI 근간 개편 설계 6.4).
    /// 옮긴 행위자는 이동 쪽 공용 도우미(<see cref="CombatMovementCalculator.MovedActor"/>)가 만들고, 이 클래스는 언제 쓸 수 있는지만 정한다.
    /// 정밀 이동 투영이 있으면 그 투영이 이 추정을 대신한다.
    /// </summary>
    internal static class EnemyFollowupGeometry
    {
        /// <summary>
        /// 종점이 교전 필드의 공격 위치이고, 경로에 해결되지 않은 도착 결과가 없고, 붕괴 착지가 아닐 때만 추정한다.
        /// </summary>
        internal static bool IsEligible(EnemyEngagementField field, MovementPath path, CombatAssessmentGap routeGaps, bool collapseLanding) =>
            field != null && !field.IsTruncated && field.IsAttackPosition(path.Endpoint) &&
            routeGaps == CombatAssessmentGap.None && !collapseLanding;

        /// <summary>
        /// 행위자만 종점으로 옮긴 입력이다. 이동 뒤 공격할 수 없거나(행동 사용) 공격 제한(무장 해제 등)이 있으면 없다.
        /// 다른 개체·지형·경로 위 도착 결과는 바꾸지 않는다.
        /// </summary>
        internal static CombatCommandInput Relocate(CombatCommandInput input, CharacterCombatFacts actor, MovementPath path,
            MovementEnvironmentFacts environment)
        {
            var moved = CombatMovementCalculator.MovedActor(input, actor, path, environment);
            if (!UnitActionUseRules.CanStartAttack(moved.ActionUse) || CombatActionAdmission.IsAttackRestricted(moved)) return null;
            var characters = new CharacterCombatFacts[input.Characters.Count];
            for (int i = 0; i < characters.Length; i++)
                characters[i] = input.Characters[i].EntityId == actor.EntityId ? moved : input.Characters[i];
            return input.WithWorld(input.State, characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings)
                .WithActor(actor.EntityId);
        }
    }
}
