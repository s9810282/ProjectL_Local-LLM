using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience
{
    /// <summary>
    /// 계산 단계 하나(Finish)의 경험치 정산이다. 그 단계에서 죽은 적의 처치 경험치를 가해 아군의 전투 사실에 쌓고(Earned),
    /// 승리로 끝나는 단계면 쌓인 경험치를 보유 기록에 반영하고 메인 랜덤 성장을 지속 상태에 더한다(경험치·레벨업 설계 §3.2~§3.4).
    /// 사망·피해 판정은 하지 않고 이미 만들어진 투영과 사건만 읽는다. Buffer를 모른다.
    /// </summary>
    internal static class CombatExperienceSettlement
    {
        /// <summary>
        /// 투영의 아군 사실에 획득 경험치를 반영하고, 승리 단계면 보유 기록 변경과 성장한 메인 지속 상태를 돌려준다.
        /// 진행 설정이 꺼져 있으면 상태를 바꾸지 않고 승리 표시용 0 경험치 내역만 만든다.
        /// </summary>
        internal static ExperienceSettlement Settle(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, List<CombatEvent> events, StageOutcomeSnapshot outcome,
            IReadOnlyList<CreditedKill> kills, CombatRewardFacts rewards, StageClearRewardSpec clearRewards)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (events == null) throw new ArgumentNullException(nameof(events));

            var spec = input.Battle?.Progression ?? default;
            if (spec.IsEnabled) Earn(projected, events, kills);
            return outcome.IsStageEnded && outcome.Outcome == StageOutcome.Victory
                ? Apply(input, projected, events, spec, rewards, clearRewards)
                : ExperienceSettlement.None;
        }

        private static void Earn(Dictionary<EntityId, CharacterCombatFacts> projected,
            List<CombatEvent> events, IReadOnlyList<CreditedKill> kills)
        {
            foreach (var kill in kills)
            {
                if (kill.Experience == 0) continue;
                var current = projected[kill.RecipientId];
                projected[kill.RecipientId] = WithExperience(current,
                    CharacterExperienceFacts.ForAlly(checked(current.Experience.Earned + kill.Experience)));
                events.Add(CombatExperienceEvent.Earned(kill.VictimId, kill.RecipientId, kill.Experience));
            }
        }

        private static ExperienceSettlement Apply(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, List<CombatEvent> events, LevelProgressionSpec spec,
            CombatRewardFacts rewards, StageClearRewardSpec clearRewards)
        {
            var changes = new List<OwnedCharacterChange>();
            var rows = new List<CharacterRewardReceipt>();
            MainCharacterPersistentState main = null;
            var participants = rewards.ClearParticipantIds.ToHashSet();
            if (participants.Any(id => !input.Owned.Any(o => o.Id == id) ||
                projected.Values.Count(c => c.OwnedId == id && c.Owner == Owner.Player) != 1))
                throw new InvalidOperationException("클리어 참여자의 보유 기록과 전투 사실이 일치하지 않습니다.");
            int clearXp = spec.IsEnabled ? clearRewards?.ExperiencePerParticipant ?? 0 : 0;
            // 행 대상: 처치 경험치가 있거나, 클리어 참여자이거나, 스테이지 기록이 있는 보유 아군(스테이지 기록 설계 Q-S6).
            foreach (var facts in projected.Values.Where(c => c.OwnedId.HasValue &&
                (c.Experience.Earned > 0 || participants.Contains(c.OwnedId.Value) || !c.StageRecord.IsEmpty))
                         .OrderBy(c => c.EntityId).ToArray())
            {
                var record = input.Owned.Single(o => o.Id == facts.OwnedId.Value);
                int killXp = spec.IsEnabled ? facts.Experience.Earned : 0;
                int participantXp = participants.Contains(record.Id) ? clearXp : 0;
                int amount = checked(killXp + participantXp);
                var final = record;
                if (amount > 0)
                {
                    var gain = ExperienceRules.Gain(record, amount, spec);
                    final = gain.Final;
                    var change = new OwnedCharacterChange(record, final);
                    changes.Add(change);
                    var growth = Array.Empty<MainGrowthKind>();
                    if (record.Role == CharacterOwnershipRole.Main && gain.LevelsGained > 0)
                    {
                        growth = GrowMain(input, projected, facts, record, final.Level, spec, out main);
                        // 최대 체력이 바뀌면 기존 체력 사건으로도 알린다(View가 이미 다루는 사건).
                        CombatCommandEvents.Health(events, facts, projected[facts.EntityId]);
                    }
                    events.Add(CombatExperienceEvent.Applied(facts.EntityId, change, amount,
                        gain.LevelsGained, growth));
                }
                rows.Add(new CharacterRewardReceipt(record.Id, record.CharacterKey, killXp, participantXp,
                    record.Level, record.Experience, spec.IsEnabled ? ExperienceRules.Required(record.Role, record.Level, spec) : 0,
                    final.Level, final.Experience, spec.IsEnabled ? ExperienceRules.Required(final.Role, final.Level, spec) : 0,
                    facts.StageRecord));
            }

            var roles = input.Owned.ToDictionary(o => o.Id, o => o.Role);
            return new ExperienceSettlement(changes, main, rows.OrderBy(c =>
                roles[c.CharacterId] == CharacterOwnershipRole.Main ? 0 : 1).ThenBy(c => c.CharacterId).ToArray());
        }

        /// <summary>
        /// 메인이 도달한 레벨마다 랜덤 성장 하나를 지속 상태에 더하고(X5), 성장한 지속 상태로 메인 전투 사실을 다시 만든다.
        /// 현재 체력은 그대로다(X4). Buffer가 메인 전투 사실과 지속 상태를 대조하므로 둘을 함께 바꾼다.
        /// </summary>
        private static MainGrowthKind[] GrowMain(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected,
            CharacterCombatFacts facts, OwnedCharacterRecord record, int finalLevel, LevelProgressionSpec spec,
            out MainCharacterPersistentState grown)
        {
            // 전투 중 지속 상태에서 바뀌는 것은 현재 체력뿐이므로, 그 단계의 메인 체력을 넣은 입력 지속 상태가 기대값이다.
            // SetPersistentMain의 체력 일치 검사는 이 구성으로 늘 성립한다. 지속 상태 원본 대조는 Buffer가 input.Main으로 한다.
            var expected = input.Main.WithCurrentHealth(facts.Health.CurrentHealth);
            grown = expected;
            var growth = new MainGrowthKind[finalLevel - record.Level];
            for (int level = record.Level + 1; level <= finalLevel; level++)
            {
                var kind = MainGrowthRules.Choose(input.Battle.TimelineSeed, record.Id, level);
                growth[level - record.Level - 1] = kind;
                grown = MainGrowthRules.Apply(grown, kind, spec);
            }

            projected[facts.EntityId] = CharacterSettlementCalculator.SetPersistentMain(facts, expected, grown)
                .Characters[0].Final;
            return growth;
        }

        /// <summary>전투 사실의 경험치 묶음만 바꾼 사본이다. 다른 값은 그대로 넘긴다.</summary>
        internal static CharacterCombatFacts WithExperience(CharacterCombatFacts c, CharacterExperienceFacts experience) =>
            // null을 넘기면 예전처럼 생성자 기본값(소유자별 기본 경험치)이 된다. With의 null은 "그대로"라서 여기서 기본값을 넘긴다.
            c.With(experience: experience ??
                (c.Owner == Owner.Enemy ? CharacterExperienceFacts.EnemyDefault : CharacterExperienceFacts.AllyDefault));
    }

    /// <summary>경험치 정산 결과다. 보유 기록 변경과, 메인이 성장했으면 성장한 지속 상태(현재 체력은 그 단계 값)를 가진다.</summary>
    internal readonly struct ExperienceSettlement
    {
        internal static ExperienceSettlement None => new(Array.Empty<OwnedCharacterChange>(), null, Array.Empty<CharacterRewardReceipt>());

        internal IReadOnlyList<OwnedCharacterChange> Owned { get; }
        /// <summary>메인 랜덤 성장이 있었으면 성장한 지속 상태, 없으면 null이다.</summary>
        internal MainCharacterPersistentState GrownMain { get; }
        internal IReadOnlyList<CharacterRewardReceipt> Characters { get; }

        internal ExperienceSettlement(IReadOnlyList<OwnedCharacterChange> owned, MainCharacterPersistentState grownMain,
            IReadOnlyList<CharacterRewardReceipt> characters)
        {
            Owned = owned ?? throw new ArgumentNullException(nameof(owned));
            GrownMain = grownMain;
            Characters = Array.AsReadOnly((characters ?? throw new ArgumentNullException(nameof(characters))).ToArray());
        }
    }
}
