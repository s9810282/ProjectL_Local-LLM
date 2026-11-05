using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item
{
    /// <summary>장애물 규칙의 입력 SO입니다. 종류와 체력·점유·반응 수치의 원본입니다.</summary>
    [CreateAssetMenu(fileName = "NewObstacleDefinition", menuName = "ProjectL/Database/InGame/ObstacleDefinition")]
    public sealed class ObstacleDefinitionDataSO : DatabaseItem
    {
        [SerializeField] private ObstacleContentKind kind = ObstacleContentKind.PowderBarrel;

        [Header("Footprint")]
        [SerializeField, Min(1)] private int footprintWidth = 1;
        [SerializeField, Min(1)] private int footprintHeight = 1;

        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealth = 20;

        [Header("PowderBarrel")]
        [Tooltip("화약통에만 적용하는 폭발 피해입니다.")]
        [SerializeField, Min(0)] private int explosionDamage;

        [Header("Supplies")]
        [Tooltip("보급품에 피해를 주었을 때 공격자에게 지급하는 공격력 보너스입니다.")]
        [SerializeField, Min(0)] private int hitAttackBonus;
        [Tooltip("보급품을 파괴했을 때 지급하는 보너스입니다. 피격 보너스와 동시에 지급하지 않습니다.")]
        [SerializeField, Min(0)] private int destructionAttackBonus;

        public ObstacleContentKind Kind => kind;
        public EntityKey Key => ObstacleContentProfile.KeyFor(kind);

        public override IData GetData()
        {
            return new ObstacleDefinitionData(kind, footprintWidth, footprintHeight, maxHealth,
                kind == ObstacleContentKind.PowderBarrel ? explosionDamage : 0,
                kind == ObstacleContentKind.Supplies ? hitAttackBonus : 0,
                kind == ObstacleContentKind.Supplies ? destructionAttackBonus : 0);
        }
    }
}
