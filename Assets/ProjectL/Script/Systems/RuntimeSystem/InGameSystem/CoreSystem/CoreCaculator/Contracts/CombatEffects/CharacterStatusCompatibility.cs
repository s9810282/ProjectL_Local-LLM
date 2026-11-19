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
            List<AppliedStatus> entries = null;
            if (effects != null)
            {
                for (int i = 0; i < effects.AttackGrants.Count; i++)
                    (entries ??= new List<AppliedStatus>()).Add(effects.AttackGrants[i].ToStatus());
                for (int i = 0; i < effects.Shields.Count; i++)
                    (entries ??= new List<AppliedStatus>()).Add(effects.Shields[i].ToStatus());
            }
            if (debuffs?.Intimidation != null) (entries ??= new List<AppliedStatus>()).Add(debuffs.Intimidation.ToStatus());
            if (debuffs?.Disarm != null) (entries ??= new List<AppliedStatus>()).Add(debuffs.Disarm.ToStatus());
            if (debuffs?.AnkleBreaker != null) (entries ??= new List<AppliedStatus>()).Add(debuffs.AnkleBreaker.ToStatus());
            if (debuffs?.ArmorBreak != null)
            {
                var stacks = debuffs.ArmorBreak.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                    (entries ??= new List<AppliedStatus>()).Add(stacks[i].ToStatus());
            }
            return entries == null ? StatusEffectSet.Empty : new StatusEffectSet(entries);
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

        private static bool IsDebuff(AppliedStatus status) =>
            status is AttackDecreaseStatus or DefenseDecreaseStatus ||
            status is ActionRestrictionStatus && status.Key is StatusEffectKey.Disarm or StatusEffectKey.Bind;
        private static bool IsTraitEffect(AppliedStatus status) => status is AttackIncreaseStatus || status is ShieldStatus;

        private static readonly Func<AppliedStatus, bool> DebuffFilter = IsDebuff;
        private static readonly Func<AppliedStatus, bool> TraitEffectFilter = IsTraitEffect;

        private static bool Matches(StatusEffectSet statuses, StatusEffectSet imported, Func<AppliedStatus, bool> owns)
        {
            if (statuses.Entries.Count == 0 && imported.Entries.Count == 0) return true;
            List<AppliedStatus> current = null, incoming = null;
            foreach (var s in statuses.Entries) if (owns(s)) (current ??= new List<AppliedStatus>()).Add(s);
            foreach (var s in imported.Entries) if (owns(s)) (incoming ??= new List<AppliedStatus>()).Add(s);
            if (current == null && incoming == null) return true;
            current ??= new List<AppliedStatus>();
            incoming ??= new List<AppliedStatus>();
            var keys = new List<StatusEffectKey>();
            foreach (var s in current) if (!keys.Contains(s.Key)) keys.Add(s.Key);
            foreach (var s in incoming) if (!keys.Contains(s.Key)) keys.Add(s.Key);
            foreach (var key in keys)
                {
                    var mine = new List<AppliedStatus>();
                    var theirs = new List<AppliedStatus>();
                    foreach (var c in current) if (c.Key == key) mine.Add(c);
                    foreach (var c in incoming) if (c.Key == key) theirs.Add(c);
                    if (!new StatusEffectSet(mine.ToArray()).SameValues(new StatusEffectSet(theirs.ToArray()))) return false;
                }
            return true;
        }

        internal static bool MatchesDebuffs(StatusEffectSet statuses, CharacterDebuffState debuffs) => Matches(statuses, Import(debuffs, null), DebuffFilter);
        internal static bool MatchesTraitEffects(StatusEffectSet statuses, CharacterTraitEffects effects) => Matches(statuses, Import(null, effects), TraitEffectFilter);

        private static StatusEffectSet Replace(StatusEffectSet current, StatusEffectSet imported, Func<AppliedStatus, bool> owns) =>
            Matches(current, imported, owns) ? current :
                new StatusEffectSet(current.Entries.Where(s => !owns(s)).Concat(imported.Entries.Where(owns)).ToArray());

        internal static StatusEffectSet ReplaceDebuffs(StatusEffectSet current, CharacterDebuffState debuffs) =>
            debuffs == null ? current : Replace(current, Import(debuffs, null), DebuffFilter);

        internal static StatusEffectSet ReplaceTraitEffects(StatusEffectSet current, CharacterTraitEffects effects) =>
            effects == null ? current : Replace(current, Import(null, effects), TraitEffectFilter);
    }
}
