using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item
{
    /// <summary>장애물 규칙의 입력 SO입니다. 종류와 필요 타수·점유·반응 수치의 원본입니다.</summary>
    public sealed class ObstacleDefinitionDataSO : DatabaseItem
    {
        [SerializeField] private ObstacleContentKind kind = ObstacleContentKind.PowderBarrel;

        [Header("Footprint")]
        [SerializeField, Min(1)] private int footprintWidth = 1;
        [SerializeField, Min(1)] private int footprintHeight = 1;

        [Header("Durability")]
        [Tooltip("파괴에 필요한 명중 횟수입니다.")]
        [SerializeField, Min(1)] private int requiredHits;

        [Header("Pillar")]
        [SerializeField, Min(1)] private int fallLength = 3;

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
            return new ObstacleDefinitionData(kind, footprintWidth, footprintHeight, requiredHits,
                kind == ObstacleContentKind.PowderBarrel ? explosionDamage : 0,
                kind == ObstacleContentKind.Supplies ? hitAttackBonus : 0,
                kind == ObstacleContentKind.Supplies ? destructionAttackBonus : 0,
                kind == ObstacleContentKind.Pillar ? fallLength : 3);
        }

#if UNITY_EDITOR
        // 생성 메뉴에서만 새 에셋의 값을 채우며, 기존 직렬화 자료에는 승인된 타수가 필요하다.
        [UnityEditor.MenuItem("Assets/Create/ProjectL/Database/InGame/ObstacleDefinition")]
        private static void CreateAsset() =>
            UnityEditor.ProjectWindowUtil.CreateAsset(CreateForAuthoring(), "NewObstacleDefinition.asset");

        internal static ObstacleDefinitionDataSO CreateForAuthoring()
        {
            var asset = CreateInstance<ObstacleDefinitionDataSO>();
            asset.requiredHits = 1;
            return asset;
        }
#endif
    }
}
