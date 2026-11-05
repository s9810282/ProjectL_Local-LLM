using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Item
{
    /// <summary>Object 규칙의 입력 SO입니다. 프리팹과 스테이지 위치는 별도 입력입니다.</summary>
    [CreateAssetMenu(fileName = "NewObjectDefinition", menuName = "ProjectL/Database/InGame/ObjectDefinition")]
    public sealed class ObjectDefinitionDataSO : DatabaseItem
    {
        [SerializeField] private ObjectContentKind kind = ObjectContentKind.Mine;

        [Header("Mine")]
        [Tooltip("지뢰에만 적용하는 피해입니다.")]
        [SerializeField, Min(0)] private int damage;

        [Header("Flag")]
        [Tooltip("깃발에만 적용하는 행동 우선순위 보너스입니다.")]
        [SerializeField, Min(0)] private int priorityBonus;

        public ObjectContentKind Kind => kind;

        public override IData GetData()
        {
            return new ObjectDefinitionData(kind,
                kind == ObjectContentKind.Mine ? damage : 0,
                kind == ObjectContentKind.Flag ? priorityBonus : 0);
        }
    }
}
