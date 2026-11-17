using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    internal static class MagicBulletTestData
    {
        internal static CombatCommandInput Input(CellCoord[] points = null,
            PlayerSkillUpgradeKey upgrade = PlayerSkillUpgradeKey.None, CellCoord? center = null,
            CharacterCombatFacts[] targets = null, StructureCombatFacts[] structures = null,
            StaticTerrainFact[] terrain = null, int attack = 100, int range = 4, int reduction = 25,
            int cap = int.MaxValue, TraitSpec extraTrait = null, ObstacleContentProfile[] obstacles = null, TraitSpec[] extraTraits = null)
        {
            var basis = PlayerSkillTestData.Input();
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(10, EntityKey.Unit_Sniper,
                center ?? new CellCoord(3, 3), Owner.Player, attack, main: true, actionUse: UnitActionUse.Attacked),
                appointRange: range, traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(reduction) });
            var characters = new[] { actor }.Concat(targets ?? Array.Empty<CharacterCombatFacts>()).ToArray();
            var owned = characters.Where(c => c.OwnedId.HasValue)
                .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            var environment = new MovementEnvironmentFacts(terrain ?? Array.Empty<StaticTerrainFact>());
            var state = basis.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment),
                structures: structures ?? new[] { PowderBlossomFixture.Structure(50, EntityKey.Structure_Headquarters, 7, 7, 1000, Owner.Enemy) },
                playerSkill: new PlayerSkillBattleState(basis.MainId, actor.EntityId,
                    new PlayerSkillDefinition(PlayerSkillKey.MagicBullet, EntityKey.Unit_Sniper, 2), upgrade));
            var traits = upgrade == PlayerSkillUpgradeKey.Judgment ? Array.Empty<TraitSpec>()
                : new TraitSpec[] { new PiercingFalloffTraitSpec(reduction, cap) };
            if (upgrade == PlayerSkillUpgradeKey.Judgment)
            {
                actor = actor.With(stats: actor.Stats.With(traitStatuses: Array.Empty<TraitStatusSnapshot>()));
                characters[0] = actor;
            }
            foreach (var addedTrait in (extraTraits ?? Array.Empty<TraitSpec>()).Concat(extraTrait == null ? Array.Empty<TraitSpec>() : new[] { extraTrait }))
            {
                traits = traits.Concat(new[] { addedTrait }).ToArray();
                TraitStatusSnapshot status = addedTrait switch
                {
                    KnockbackTraitSpec knockback => new KnockbackTraitStatusSnapshot(knockback.DistanceCells),
                    ImpactBoltTraitSpec impact => new ImpactBoltTraitStatusSnapshot(impact.DistanceCells),
                    CrashImpactTraitSpec crash => new CrashImpactTraitStatusSnapshot(crash.DistanceCells, crash.WallDamagePercent, crash.UnitDamagePercent),
                    _ => throw new ArgumentException("지원하지 않는 테스트 특성입니다.", nameof(extraTrait))
                };
                actor = actor.With(stats: actor.Stats.With(traitStatuses: actor.Stats.TraitStatuses.ToArray().Concat(new[] { status }).ToArray()));
                characters[0] = actor;
            }
            var profile = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Sniper, attack, CategoryKey.Main, traits),
                new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var input = new CombatCommandInput(1, CombatCommandKind.UsePlayerSkill, actor.EntityId, default, state,
                characters, basis.Profiles.Concat(new[] { profile }).ToArray(), basis.Main, owned,
                owned.ToDictionary(c => c.Id, _ => 0), basis.Souls, basis.Artifacts,
                battleInstanceId: basis.BattleInstanceId, membership: new RunDeckSnapshot(basis.Membership.RunId, 1,
                    owned.Select(c => c.Id).OrderBy(id => id.Value).ToArray()),
                environmentBindings: new EnvironmentSemanticBindings((terrain ?? Array.Empty<StaticTerrainFact>()).Select(t => t.Id).ToArray(),
                    Array.Empty<long>(), (obstacles ?? Array.Empty<ObstacleContentProfile>()).Select(o => o.EntityId).ToArray()),
                obstacleContents: obstacles ?? Array.Empty<ObstacleContentProfile>(),
                playerSkillRequest: new PlayerSkillRequest(points ?? Array.Empty<CellCoord>(), basis.PlayerSkillSource),
                playerSkillSource: basis.PlayerSkillSource);
            return input;
        }

        internal static CombatCommandInput Copy(CombatCommandInput input, CharacterCombatFacts[] characters = null,
            CombatAttackProfile[] profiles = null, CombatCommandState state = null, PlayerSkillRequest request = null, CombatBattleFacts battle = null) =>
            new(input.Number, input.Kind, input.ActorId, input.Aim, state ?? input.State,
                characters ?? input.Characters, profiles ?? input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, battle: battle ?? input.Battle, battleInstanceId: input.BattleInstanceId, membership: input.Membership,
                environmentBindings: input.EnvironmentBindings, obstacleContents: input.ObstacleContents,
                playerSkillRequest: request ?? input.PlayerSkillRequest, playerSkillSource: input.PlayerSkillSource);
    }
}
