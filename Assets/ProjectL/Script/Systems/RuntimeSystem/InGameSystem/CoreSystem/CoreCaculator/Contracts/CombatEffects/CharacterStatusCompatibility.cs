using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>이전 입력은 정규 원본으로 바꾸며 레거시 조회는 불변 투영만 제공합니다.</summary>
    public static class CharacterStatusCompatibility
    {
        public static StatusEffectSet Import(CharacterDebuffState debuffs, CharacterTraitEffects effects)
        {
            var entries = new List<AppliedStatus>();
            if (effects != null)
            {
                entries.AddRange(effects.AttackGrants.Select(g => g.ToStatus()));
                entries.AddRange(effects.Shields.Select(g => g.ToStatus()));
            }
            if (debuffs?.Intimidation != null) entries.Add(debuffs.Intimidation.ToStatus());
            if (debuffs?.Disarm != null) entries.Add(debuffs.Disarm.ToStatus());
            if (debuffs?.AnkleBreaker != null) entries.Add(debuffs.AnkleBreaker.ToStatus());
            if (debuffs?.ArmorBreak != null) entries.AddRange(debuffs.ArmorBreak.Stacks.Select(s => s.ToStatus()));
            return entries.Count == 0 ? StatusEffectSet.Empty : new StatusEffectSet(entries);
        }

        public static CharacterDebuffState ProjectDebuffs(StatusEffectSet statuses) => ProjectDebuffs(statuses, CharacterDebuffState.Empty);

        internal static CharacterDebuffState ProjectDebuffs(StatusEffectSet statuses, CharacterDebuffState legacy)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            legacy ??= CharacterDebuffState.Empty;
            if (MatchesDebuffs(statuses, legacy)) return legacy;
            var attack = statuses.Entries.OfType<AttackDecreaseStatus>().SingleOrDefault();
            var disarm = statuses.Entries.OfType<ActionRestrictionStatus>().SingleOrDefault(s => s.Key == StatusEffectKey.Disarm);
            var bind = statuses.Entries.OfType<ActionRestrictionStatus>().SingleOrDefault(s => s.Key == StatusEffectKey.Bind);
            var stacks = statuses.Entries.OfType<DefenseDecreaseStatus>().Select(s => new ArmorBreakStack(s)).ToArray();
            return new CharacterDebuffState(attack == null ? null : new IntimidationDebuffState(attack),
                legacy.DefenseReduction, legacy.HuntersMark, disarm == null ? null : new DisarmState(disarm),
                bind == null ? null : new AnkleBreakerState(bind), stacks.Length != 0 ? new ArmorBreakState(stacks) :
                    legacy.ArmorBreak?.Stacks.Count == 0 ? legacy.ArmorBreak : null);
        }

        public static CharacterTraitEffects ProjectTraitEffects(StatusEffectSet statuses, int cohesionDefenseBonus) =>
            new(statuses, cohesionDefenseBonus);

        internal static CharacterTraitEffects ProjectTraitEffects(StatusEffectSet statuses, CharacterTraitEffects legacy)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            return MatchesTraitEffects(statuses, legacy) ? legacy :
                new CharacterTraitEffects(statuses, legacy.CohesionDefenseBonus);
        }

        private static bool IsDebuff(AppliedStatus status) => status is AttackDecreaseStatus || status is ActionRestrictionStatus || status is DefenseDecreaseStatus;
        private static bool IsTraitEffect(AppliedStatus status) => status is AttackIncreaseStatus || status is ShieldStatus;

        private static bool Matches(StatusEffectSet statuses, StatusEffectSet imported, Func<AppliedStatus, bool> owns)
        {
            var current = statuses.Entries.Where(owns).ToArray();
            var incoming = imported.Entries.Where(owns).ToArray();
            return current.Select(s => s.Key).Concat(incoming.Select(s => s.Key)).Distinct().All(key =>
                new StatusEffectSet(current.Where(s => s.Key == key).ToArray()).SameValues(
                    new StatusEffectSet(incoming.Where(s => s.Key == key).ToArray())));
        }

        internal static bool MatchesDebuffs(StatusEffectSet statuses, CharacterDebuffState debuffs) => Matches(statuses, Import(debuffs, null), IsDebuff);
        internal static bool MatchesTraitEffects(StatusEffectSet statuses, CharacterTraitEffects effects) => Matches(statuses, Import(null, effects), IsTraitEffect);

        private static StatusEffectSet Replace(StatusEffectSet current, StatusEffectSet imported, Func<AppliedStatus, bool> owns) =>
            Matches(current, imported, owns) ? current :
                new StatusEffectSet(current.Entries.Where(s => !owns(s)).Concat(imported.Entries.Where(owns)).ToArray());

        internal static StatusEffectSet ReplaceDebuffs(StatusEffectSet current, CharacterDebuffState debuffs) =>
            debuffs == null ? current : Replace(current, Import(debuffs, null), IsDebuff);

        internal static StatusEffectSet ReplaceTraitEffects(StatusEffectSet current, CharacterTraitEffects effects) =>
            effects == null ? current : Replace(current, Import(null, effects), IsTraitEffect);
    }
}
