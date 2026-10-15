using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item
{
    /// <summary>UnitData의 입력 SO입니다. 기획 값은 이 에셋에서 수정합니다.</summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "ProjectL/Database/InGame/UnitData")]
    internal sealed class UnitDataSO : DatabaseItem
    {
        [Header("Identity")]
        [SerializeField] private EntityKey key;
        [SerializeField] private UnitRole role = UnitRole.Regular;
        [SerializeField] private UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.PlayerOnly;

        [Header("Footprint")]
        [SerializeField, Min(1)] private int footprintWidth = 1;
        [SerializeField, Min(1)] private int footprintHeight = 1;

        [Header("Core Stats")]
        [SerializeField, Min(1)] private int maxHealth = 10;
        [SerializeField, Min(0)] private int physicalDefense;
        [SerializeField, Min(0)] private int moveRange = 3;
        [SerializeField, Min(1)] private int moveCost = 1;

        [Header("Attack")]
        [Tooltip("공격력이 1 이상이면 필수입니다. InGameGroup의 지정 방식 테이블에 등록해야 합니다.")]
        [SerializeField] private TargetingMethodDataSO targetingMethod;
        [SerializeField, Min(0)] private int attackRange = 1;
        [SerializeField] private CategoryKey category = CategoryKey.Base;
        [SerializeField, Min(0)] private int attackPower = 1;
        [SerializeField, Min(0)] private int maxActionCost = 3;
        [SerializeField, Min(1)] private int attackCost = 1;

        [Header("Attack Walls")]
        [Tooltip("공격자에서 지정 셀까지의 벽 통과 정책입니다. 진영 자격과 벽 점유는 바꾸지 않습니다.")]
        [SerializeField] private AttackWallDelivery wallDelivery = AttackWallDelivery.RequireClear;
        [Tooltip("충격 중심에서 추가 셀로 퍼지는 공격의 벽 통과 정책입니다.")]
        [SerializeField] private AttackWallPropagation wallPropagation = AttackWallPropagation.FromImpact;

        [Header("Timeline")]
        [SerializeField, Min(1)] private int priority = 10;
        [SerializeField, Min(1)] private int delay = 10;

        [Header("Traits")]
        [SerializeReference] private List<UnitTraitAuthoring> traits = new();

        /// <summary>다른 SO가 이 유닛을 참조할 때 쓰는 키입니다.</summary>
        internal EntityKey Key => key;

        public override IData GetData()
        {
            var traitData = new UnitTraitData[traits?.Count ?? 0];
            for (int i = 0; i < traitData.Length; i++)
                traitData[i] = traits[i]?.ToData();

            return new UnitData(
                key,
                role,
                ownerEligibility,
                footprintWidth,
                footprintHeight,
                maxHealth,
                physicalDefense,
                moveRange,
                moveCost,
                attackRange,
                category,
                attackPower,
                maxActionCost,
                attackCost,
                priority,
                delay,
                traitData,
                targetingMethod != null ? targetingMethod.MethodKey : 0,
                new AttackOcclusionSpec(wallDelivery, wallPropagation));
        }
    }
}
