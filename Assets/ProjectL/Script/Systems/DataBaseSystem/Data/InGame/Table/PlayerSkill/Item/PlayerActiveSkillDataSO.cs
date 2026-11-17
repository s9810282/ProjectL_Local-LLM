using System;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item
{
    [CreateAssetMenu(fileName = "NewPlayerActiveSkillData", menuName = "ProjectL/Database/InGame/PlayerActiveSkillData")]
    internal sealed class PlayerActiveSkillDataSO : DatabaseItem
    {
        [SerializeField] private PlayerSkillKey skillKey;
        [SerializeField] private UnitDataSO mainCharacter;
        [SerializeField, Min(1)] private int stageUseLimit = 1;
        [SerializeField] private PlayerSkillUpgradeKey firstUpgrade;
        [SerializeField] private PlayerSkillUpgradeKey secondUpgrade;

        public override IData GetData()
        {
            string identity = $"[{nameof(PlayerActiveSkillDataSO)}] {name}, Skill={skillKey}, Main={mainCharacter?.Key}";
            try
            {
                if (mainCharacter == null)
                    throw new InvalidOperationException("메인 유닛 참조가 필요합니다.");
                var unit = (UnitData)mainCharacter.GetData();
                if (unit.Role != UnitRole.MainCharacter)
                    throw new InvalidOperationException("스킬 소유 유닛은 메인 역할이어야 합니다.");
                PlayerMainDefinitionPolicy.Validate(unit.Key, unit.Role, unit.Category, unit.OwnerEligibility);
                var definition = new PlayerSkillDefinition(skillKey, unit.Key, stageUseLimit);
                definition.ValidateTraits(unit.Traits.Select(t => t.TraitKey));
                if (firstUpgrade == secondUpgrade || !definition.SupportsUpgrade(firstUpgrade) || !definition.SupportsUpgrade(secondUpgrade))
                    throw new InvalidOperationException("스킬에 속하는 서로 다른 강화 갈래 두 개가 필요합니다.");
                return new PlayerActiveSkillData(definition);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                throw new InvalidOperationException($"{identity}: {exception.Message}", exception);
            }
        }
    }
}
