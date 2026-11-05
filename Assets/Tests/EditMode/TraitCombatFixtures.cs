using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;

namespace ProjectL.Tests.Core
{
    internal static class TraitCombatFixtures
    {
        internal static CombatCommandInput Equip(CombatCommandInput input, EntityKey key, params TraitSpec[] traits)
        {
            var profile = input.Profiles.Single(p => p.Definition.Key == key);
            var s = profile.Definition;
            var spec = new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                traits, s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward);
            var registry = TraitRegistry.CreateDefault();
            // 기존 명령 fixture의 퇴역 메인 키를 운영 카탈로그로 다시 활성화하지 않습니다.
            var statuses = traits.Select(t =>
            {
                var registration = registry.GetRequiredRegistration(t.TraitKey);
                var definition = registration.TraitDefinitionFactory.BuildDefinition(t);
                registration.TraitRuntimeStateFactory.BuildRuntimeState(definition);
                var type = typeof(TraitStatusSnapshot).Assembly.GetType(typeof(TraitStatusSnapshot).Namespace + "." + t.TraitKey + "TraitStatusSnapshot");
                return t is ReconSquadTraitSpec recon ? new ReconSquadTraitStatusSnapshot(recon.IncreasePercent, recon.Radius)
                    : (TraitStatusSnapshot)Activator.CreateInstance(type);
            }).ToArray();
            return CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.CharacterKey == key ? c.With(stats: c.Stats.With(traitStatuses: statuses)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == key ? new CombatAttackProfile(spec, p.Method, p.OwnedId) : p).ToArray());
        }

        internal static CombatCommandInput Characters(CombatCommandInput input, params CharacterCombatFacts[] characters)
        {
            var owned = characters.Where(c => c.OwnedId.HasValue).Select(c => input.Owned.SingleOrDefault(o => o.Id == c.OwnedId)
                ?? new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            var counts = owned.ToDictionary(o => o.Id, o => input.SoulCounts.GetValueOrDefault(o.Id));
            return input.WithWorld(input.State, characters, input.Main, counts, input.Souls, input.EnvironmentBindings)
                .WithPersistentFacts(owned, input.ItemInventory, input.GoldBalance, input.FameBalance);
        }

        internal static CombatCommandTransition Attack(CombatCommandInput input) => CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
        internal static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId.Value == id)?.Final ?? result.Input.Characters.Single(c => c.EntityId.Value == id);
    }
}
