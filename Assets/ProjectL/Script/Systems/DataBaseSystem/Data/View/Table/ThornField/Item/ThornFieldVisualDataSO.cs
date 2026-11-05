using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.ThornField.Item
{
    /// <summary>
    /// 가시밭 칸의 3D 외형입니다(구조 리팩토링 설계 D12). 가시밭 칸 목록·남은 턴·피해는 Core(CombatCommandState.ThornFields)가 가집니다.
    /// 특성의 이름·아이콘·설명은 TraitVisual(ThornField)에 있습니다.
    /// </summary>
    [Serializable]
    internal sealed class ThornFieldVisualData : IData
    {
        [Tooltip("가시밭 칸 하나에 놓는 프리팹입니다. 크기·회전은 프리팹 값을 씁니다. 필수이며, 비우면 DB를 만들 때 실패합니다.")]
        [SerializeField] private GameObject prefab;

        /// <summary>필수입니다. 표(<c>ThornFieldVisualTable</c>)가 만들 때 <see cref="Validate"/>로 검사합니다.</summary>
        public GameObject Prefab => prefab;

        /// <summary>프리팹이 없으면 실패합니다(View 3라운드 W12-5). 가시밭이 나오는 전투까지 기다리지 않고 DB를 만들 때 알립니다.</summary>
        internal void Validate()
        {
            if (prefab == null)
                throw new InvalidOperationException("[ThornFieldVisualData] Prefab is required.");
        }
    }

    [CreateAssetMenu(fileName = "NewThornFieldVisualData", menuName = "ProjectL/Database/View/ThornFieldVisualData")]
    internal sealed class ThornFieldVisualDataSO : DatabaseItem
    {
        [SerializeField] private ThornFieldVisualData thornFieldVisualData = new();

        public override IData GetData() => thornFieldVisualData;
    }
}
