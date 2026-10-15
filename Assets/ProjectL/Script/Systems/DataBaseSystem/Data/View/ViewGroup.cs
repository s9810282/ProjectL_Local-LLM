using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View
{
    /// <summary>View가 쓰는 표현 데이터 그룹입니다. Unity 타입(프리팹, 색상 등)을 사용할 수 있습니다.</summary>
    [CreateAssetMenu(fileName = "NewViewGroup", menuName = "ProjectL/Database/ViewGroup")]
    internal sealed class ViewGroup : DatabaseGroup
    {
    }
}
