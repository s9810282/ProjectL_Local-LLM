using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>정보창이 한 번 적용하는 불변 표시값입니다.</summary>
    internal sealed class CombatInspectionInfo
    {
        internal CombatInspectionInfo(CombatInspectionFacts facts, string name, string description, Sprite icon, CharacterInfoSection character)
        {
            Id = facts.Id;
            Name = name;
            Description = description;
            Icon = icon;
            Owner = facts.Owner;
            Health = facts.Health;
            MaxHealth = facts.MaxHealth;
            Shield = facts.Character?.TraitEffects.ActiveShieldTotal ?? 0;
            Character = character;
            StructureAttackPower = facts.StructureAttackPower;
        }
        internal EntityId Id { get; }
        internal string Name { get; }
        internal string Description { get; }
        internal Sprite Icon { get; }
        internal Owner Owner { get; }
        internal int Health { get; }
        internal int MaxHealth { get; }
        internal int Shield { get; }
        internal CharacterInfoSection Character { get; }
        internal int? StructureAttackPower { get; }
    }

    internal sealed class CharacterInfoSection
    {
        internal CharacterInfoSection(int? level, Sprite category, UnitCardTraitContent[] traits,
            IReadOnlyList<UnitHUDStatus> statuses, int attack, int defense, string categoryName = "")
        {
            Level = level;
            Category = category;
            CategoryName = categoryName;
            Traits = traits;
            Statuses = statuses;
            Attack = attack;
            Defense = defense;
        }
        internal int? Level { get; }
        internal Sprite Category { get; }
        internal string CategoryName { get; }
        internal UnitCardTraitContent[] Traits { get; }
        internal IReadOnlyList<UnitHUDStatus> Statuses { get; }
        internal int Attack { get; }
        internal int Defense { get; }
    }

    /// <summary>외형 의존성과 종류별 표시 변환을 정보창 밖에 둡니다. HP는 항상 입력 사실을 사용합니다.</summary>
    internal sealed class CombatInspectionInfoMapper
    {
        private readonly IUnitVisualTableQuery _units;
        private readonly ITraitVisualTableQuery _traits;
        private readonly ICategoryVisualTableQuery _categories;
        private readonly CombatUnitStatuses _statuses;
        internal CombatInspectionInfoMapper(IUnitVisualTableQuery units, ITraitVisualTableQuery traits,
            ICategoryVisualTableQuery categories, CombatUnitStatuses statuses)
        {
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        }
        internal CombatInspectionInfo Map(CombatInspectionFacts facts, IReadOnlyList<OwnedCharacterRecord> owned,
            TraitSettingsSet settings)
        {
            var visual = _units.GetUnitVisual(facts.Key);
            return new CombatInspectionInfo(facts, visual.DisplayName, visual.Description, visual.InfoIcon,
                facts.Character != null ? Character(facts, owned, settings) : null);
        }
        private CharacterInfoSection Character(CombatInspectionFacts facts, IReadOnlyList<OwnedCharacterRecord> owned,
            TraitSettingsSet settings)
        {
            var character = facts.Character;
            int? level = character.Experience.EnemyLevel;
            if (character.OwnedId.HasValue)
                foreach (OwnedCharacterRecord item in owned)
                    if (item.Id == character.OwnedId.Value) { level = item.Level; break; }
            var statuses = new List<UnitHUDStatus>();
            _statuses.AddTo(character, statuses);
            return new CharacterInfoSection(level, _categories.GetCategoryVisual(character.Stats.Category.Key).WhiteIcon,
                UnitCardTraitContent.Create(_traits, character.Stats.TraitStatuses.Span, settings), statuses.AsReadOnly(),
                CombatEffectiveStatsCalculator.AttackPower(character), CombatEffectiveStatsCalculator.PhysicalDefense(character),
                character.Stats.Category.Key.ToString().ToUpperInvariant());
        }
    }
}
