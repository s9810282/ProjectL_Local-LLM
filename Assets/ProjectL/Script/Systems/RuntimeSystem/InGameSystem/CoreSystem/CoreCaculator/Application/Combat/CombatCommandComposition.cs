using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 자동 명령 내부의 계산 결과를 합친다. 중간 결과를 Buffer에 게시하지 않는다.</summary>
    internal static class CombatCommandComposition
    {
        internal static CombatCommandInput Intent(CombatCommandInput i, CombatCommandKind kind, AttackAim aim = default,
            CellCoord cell = default, MovementPath path = null, MovementPreviewContext movementContext = null) =>
            i.WithIntent(kind, aim, cell, path ?? i.Path, movementContext ?? i.MovementContext, sweepSide: null);
        internal static CombatCommandInput Project(CombatCommandTransition t)
        {
            CombatCommandInput result = null;
            foreach (var _ in ProjectSteps(t, value => result = value)) { }
            return result;
        }

        internal static IEnumerable<int> ProjectSteps(CombatCommandTransition t, Action<CombatCommandInput> completed)
        {
            var i = t.Input;
            if (!t.Accepted) throw new InvalidOperationException("거절된 계산을 자동 명령의 중간 변경으로 사용할 수 없습니다.");
            var characters = new Dictionary<EntityId, CharacterCombatFacts>();
            foreach (var c in i.Characters) { characters.Add(c.EntityId, c); yield return 0; }
            foreach (var c in t.Characters.Characters) { characters[c.Final.EntityId] = c.Final; yield return 0; }
            // 이번 단계에서 만든 개체는 다음 단계의 기존 개체가 된다. 할당·소환 기록·목표는 FinalState로 함께 넘어간다.
            foreach (var c in t.Characters.Created) { characters.Add(c.EntityId, c); yield return 0; }
            var counts = new Dictionary<CharacterInstanceId, int>();
            foreach (var pair in i.SoulCounts) { counts.Add(pair.Key, pair.Value); yield return 0; }
            foreach (var c in t.Characters.Souls) { counts[c.Id] = c.FinalCount; yield return 0; }
            // 보유 기록 변경도 다음 단계의 원본이다. 목록 순서(획득 순서)는 유지한다.
            var changes = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            foreach (var c in t.Characters.Owned) { changes.Add(c.Expected.Id, c.Final); yield return 0; }
            var owned = new OwnedCharacterRecord[i.Owned.Count];
            for (int index = 0; index < owned.Length; index++)
            {
                var source = i.Owned[index];
                owned[index] = changes.TryGetValue(source.Id, out var final) ? final : source;
                yield return 0;
            }
            var values = new CharacterCombatFacts[characters.Count];
            int target = 0;
            foreach (var value in characters.Values) { values[target++] = value; yield return 0; }
            completed(i.WithWorld(t.FinalState, values, t.Characters.Main?.Final ?? i.Main,
                counts, t.Field.Final, t.EnvironmentBindings?.Final ?? i.EnvironmentBindings)
                .WithPersistentFacts(owned, t.InventoryChange?.Final ?? i.ItemInventory,
                    t.RewardChange?.FinalGold ?? i.GoldBalance, t.RewardChange?.FinalFame ?? i.FameBalance));
        }
        internal static CombatCommandTransition WithEvents(CombatCommandTransition transition, IReadOnlyList<CombatEvent> events) =>
            new(transition.HealthExecutions, transition.HealthClaims, transition.Input, transition.Accepted, transition.Reason, transition.Characters, transition.Field,
                transition.FinalState, transition.Outcome, transition.Attack, transition.ConsumedSoulIds, transition.Completion,
                transition.Movement, events, transition.PathMovement, transition.EnvironmentBindings, transition.BlockedContacts,
                transition.InventoryChange, transition.RewardChange, transition.RewardSettlement, transition.ActionUseSettlement,
                transition.UndoMovement, transition.MovementEffectUses, transition.PlayerSkill);

        internal static CombatCommandTransition Merge(CombatCommandInput original, IReadOnlyList<CombatCommandTransition> steps)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (steps == null || steps.Count == 0 || steps.Any(s => s == null || !s.Accepted))
                throw new ArgumentException("합성에는 수락된 단계가 하나 이상 필요합니다.", nameof(steps));
            if (steps.Take(steps.Count - 1).Any(s => s.Outcome.IsStageEnded))
                throw new InvalidOperationException("종료된 단계 뒤에는 다른 단계를 합성할 수 없습니다.");
            var final = Project(steps.Last());
            var characters = final.Characters.ToDictionary(c => c.EntityId);
            // 원래 있던 개체는 변경으로, 단계 중 만든 개체는 최종 사실 하나로만 게시한다(같은 ID를 두 번 싣지 않는다).
            var originalIds = original.Characters.Select(c => c.EntityId).ToHashSet();
            var changes = new CharacterChanges(original.Characters.Where(c => !ReferenceEquals(c, characters[c.EntityId]))
                .Select(c => new CharacterStateChange(c, characters[c.EntityId])).ToArray(),
                final.SoulCounts.Where(p => p.Value != original.SoulCounts[p.Key]).Select(p => new SoulCountChange(p.Key, original.SoulCounts[p.Key], p.Value)).ToArray(),
                steps.Any(s => s.Characters.Main != null) ? new MainCharacterPersistentChange(original.MainId, original.Main, final.Main) : null,
                final.Characters.Where(c => !originalIds.Contains(c.EntityId)).OrderBy(c => c.EntityId).ToArray(),
                // 보유 기록은 원래 입력 대비 최종 차이 하나로만 게시한다(영혼 수와 같은 방식).
                original.Owned.Select(o => (Expected: o, Final: final.Owned.Single(f => f.Id == o.Id)))
                    .Where(p => !p.Expected.SameValues(p.Final))
                    .Select(p => new OwnedCharacterChange(p.Expected, p.Final)).ToArray());
            return new CombatCommandTransition(steps.SelectMany(s => s.HealthExecutions), steps.SelectMany(s => s.HealthClaims),
                original, true, "", changes, new SoulFieldChange(original.Souls, final.Souls), final.State,
                steps.Last().Outcome, steps.Select(s => s.Attack).LastOrDefault(a => a.HasValue),
                steps.SelectMany(s => s.ConsumedSoulIds).Distinct().ToArray(), movement: steps.Select(s => s.Movement).LastOrDefault(m => m.HasValue),
                events: steps.SelectMany(s => s.Events).ToArray(), pathMovement: steps.Select(s => s.PathMovement).LastOrDefault(p => p != null),
                environmentBindings: original.EnvironmentBindings.SameValues(final.EnvironmentBindings) ? null :
                    new EnvironmentBindingsChange(original.EnvironmentBindings, final.EnvironmentBindings),
                blockedContacts: steps.SelectMany(s => s.BlockedContacts).ToArray(),
                inventoryChange: original.ItemInventory.SameValues(final.ItemInventory) ? null :
                    new ItemInventoryChange(original.ItemInventory, final.ItemInventory),
                rewardChange: steps.All(s => s.RewardChange == null) ? null :
                    new CombatRewardChange(original.GoldBalance ?? throw new InvalidOperationException("합성 보상 원본 잔액이 없습니다."),
                        final.GoldBalance.Value, original.FameBalance ?? 0, final.FameBalance ?? 0),
                rewardSettlement: MergeRewards(original, steps, final),
                actionUseSettlement: MergeActionUse(steps), undoMovement: steps.Select(s => s.UndoMovement).SingleOrDefault(u => u != null),
                movementEffectUses: steps.SelectMany(s => s.MovementEffectUses).ToArray(),
                playerSkill: steps.Select(s => s.PlayerSkill).SingleOrDefault(skill => skill != null));
        }

        private static ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.UnitActionUseSettlementSnapshot
            MergeActionUse(IReadOnlyList<CombatCommandTransition> steps)
        {
            var settlements = steps.Where(step => step.ActionUseSettlement != null).Select(step => step.ActionUseSettlement).ToArray();
            if (settlements.Length > 1)
                throw new InvalidOperationException("한 게시 단위에는 일반 행동 정산이 하나만 있어야 합니다.");
            return settlements.SingleOrDefault();
        }

        private static CombatRewardSettlement MergeRewards(CombatCommandInput original,
            IReadOnlyList<CombatCommandTransition> steps, CombatCommandInput final)
        {
            int gold = 0;
            int killCount = 0;
            foreach (var step in steps)
            {
                int delta = checked(step.FinalState.Rewards.KillGoldEarned - step.Input.State.Rewards.KillGoldEarned);
                var settlement = step.RewardSettlement;
                if (settlement == null)
                {
                    if (delta != 0 || step.RewardChange != null ||
                        step.Events.OfType<CombatExperienceEvent>().Any(e => e.Kind == CombatEventKind.ExperienceApplied) ||
                        step.Outcome.Outcome == StageOutcome.Victory && step.Input.Battle?.ClearRewards != null)
                        throw new InvalidOperationException("보상 효과가 있는 합성 단계에 정산 근거가 없습니다.");
                    continue;
                }
                if (settlement.KillGold != delta)
                    throw new InvalidOperationException("단계의 처치 골드와 누적 기록 변경량이 다릅니다.");
                gold = checked(gold + settlement.KillGold);
                killCount = checked(killCount + settlement.KillCount);
            }
            if (gold != checked(final.State.Rewards.KillGoldEarned - original.State.Rewards.KillGoldEarned))
                throw new InvalidOperationException("합성 처치 골드가 최초 입력과 최종 기록의 차이와 다릅니다.");
            if (steps.All(s => s.RewardSettlement == null)) return null;
            var terminal = steps.Last().Outcome.IsStageEnded ? steps.Last().RewardSettlement : null;
            var rows = terminal?.Characters ?? Array.Empty<CharacterRewardReceipt>();
            foreach (var row in rows)
            {
                var before = original.Owned.Single(o => o.Id == row.CharacterId);
                var after = final.Owned.Single(o => o.Id == row.CharacterId);
                if (row.CharacterKey != before.CharacterKey || row.LevelBefore != before.Level ||
                    row.ExperienceBefore != before.Experience || row.LevelAfter != after.Level || row.ExperienceAfter != after.Experience)
                    throw new InvalidOperationException("합성 경험치 내역의 앞뒤가 실제 보유 기록과 다릅니다.");
            }
            return new CombatRewardSettlement(killCount, gold, terminal?.ClearGold ?? 0, terminal?.ClearFame ?? 0,
                terminal?.HasClearReward ?? false, rows);
        }
    }
}
