using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item
{
    /// <summary>CategoryConfigData의 입력 SO입니다. 테이블에 하나만 둡니다(설정형).</summary>
    [CreateAssetMenu(fileName = "NewCategoryConfigData", menuName = "ProjectL/Database/InGame/CategoryConfigData")]
    internal sealed class CategoryConfigDataSO : DatabaseItem
    {
        [Header("Default Traits")]
        [SerializeField] private TraitGrowthDefinitionDataSO mainDefaultTrait;
        [SerializeField] private TraitGrowthDefinitionDataSO riderDefaultTrait;
        [SerializeField] private TraitGrowthDefinitionDataSO heavyDefaultTrait;
        [SerializeField] private TraitGrowthDefinitionDataSO cannonDefaultTrait;
        [SerializeField] private TraitGrowthDefinitionDataSO bossDefaultTrait;

        [Header("Movement")]
        [SerializeField] private MovementInput mainMovement = new MovementInput(3);
        [SerializeField] private MovementInput riderMovement = new MovementInput(4);
        [SerializeField] private MovementInput heavyMovement = new MovementInput(3);
        [SerializeField] private MovementInput cannonMovement = new MovementInput(2);
        [SerializeField] private MovementInput bossMovement = new MovementInput(0);

        [Header("Level Growth")]
        [Tooltip("레벨이 1 오를 때마다 더하는 능력치입니다. 레벨 1 값은 유닛 정의입니다. 메인은 따로 성장하므로 적용하지 않습니다.")]
        [FormerlySerializedAs("baseLevelGrowth")]
        [SerializeField] private LevelGrowthInput mainLevelGrowth;
        [SerializeField] private LevelGrowthInput riderLevelGrowth;
        [SerializeField] private LevelGrowthInput heavyLevelGrowth;
        [SerializeField] private LevelGrowthInput cannonLevelGrowth;
        [SerializeField] private LevelGrowthInput bossLevelGrowth;

        [Header("Level Progression")]
        [Tooltip("경험치·레벨업 수치입니다. 모든 입력값이 0이면 경험치가 꺼집니다. 켜면 요구량 단계는 1 이상이어야 합니다.")]
        [SerializeField] private LevelProgressionInput levelProgression;

        [Serializable]
        private struct LevelProgressionInput
        {
            [Tooltip("메인이 레벨 L에서 L+1로 오를 때 필요한 경험치 = 이 값 × L")]
            [SerializeField, Min(0)] private int mainExperiencePerLevel;
            [Tooltip("서브가 레벨 L에서 L+1로 오를 때 필요한 경험치 = 이 값 × L")]
            [SerializeField, Min(0)] private int subExperiencePerLevel;
            [Tooltip("메인 레벨업 때 '최대 체력' 선택지가 더하는 양입니다(체력·공격·방어 중 하나를 균등 확률로 고름).")]
            [SerializeField, Min(0)] private int mainMaxHealthGrowth;
            [Tooltip("메인 레벨업 때 '공격력' 선택지가 더하는 양입니다.")]
            [SerializeField, Min(0)] private int mainAttackGrowth;
            [Tooltip("메인 레벨업 때 '방어력' 선택지가 더하는 양입니다.")]
            [SerializeField, Min(0)] private int mainDefenseGrowth;

            // 전체 0만 꺼짐이다. 켜진 값의 범위는 계약 생성자가 확인한다.
            internal LevelProgressionSpec ToData()
            {
                if (mainExperiencePerLevel == 0 && subExperiencePerLevel == 0 && mainMaxHealthGrowth == 0 &&
                    mainAttackGrowth == 0 && mainDefenseGrowth == 0)
                    return default;
                return new LevelProgressionSpec(mainExperiencePerLevel, subExperiencePerLevel,
                    mainMaxHealthGrowth, mainAttackGrowth, mainDefenseGrowth);
            }
        }

        [Serializable]
        private struct LevelGrowthInput
        {
            [SerializeField, Min(0)] private int maxHealthPerLevel;
            [SerializeField, Min(0)] private int attackPerLevel;

            internal CategoryLevelGrowth ToData() => new CategoryLevelGrowth(maxHealthPerLevel, attackPerLevel);
        }

        [Serializable]
        private struct MovementInput
        {
            [Tooltip("자발 이동 허용량(칸)입니다. 0이면 이 카테고리의 모든 유닛이 자발 이동을 할 수 없습니다.")]
            [SerializeField, Min(0)] private int movementAllowance;

            internal int MovementAllowance => movementAllowance;

            internal MovementInput(int movementAllowance)
            {
                this.movementAllowance = movementAllowance;

            }
        }

        public override IData GetData()
        {
            var defaults = new Dictionary<CategoryKey, TraitKey>();
            void AddDefault(CategoryKey category, TraitGrowthDefinitionDataSO definition)
            {
                if (definition != null) defaults.Add(category, definition.Key);
            }
            AddDefault(CategoryKey.Main, mainDefaultTrait);
            AddDefault(CategoryKey.Rider, riderDefaultTrait);
            AddDefault(CategoryKey.Heavy, heavyDefaultTrait);
            AddDefault(CategoryKey.Cannon, cannonDefaultTrait);
            AddDefault(CategoryKey.Boss, bossDefaultTrait);
            var data = new CategoryConfigData(mainMovement.MovementAllowance, riderMovement.MovementAllowance,
                heavyMovement.MovementAllowance, cannonMovement.MovementAllowance, bossMovement.MovementAllowance,
                mainLevelGrowth.ToData(), riderLevelGrowth.ToData(), heavyLevelGrowth.ToData(), cannonLevelGrowth.ToData(),
                bossLevelGrowth.ToData(), levelProgression.ToData(), defaults);
            data.Validate();
            return data;
        }
    }
}
