using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 표시 중인 유닛의 지금 위치를 읽기만 하는 좁은 계약입니다(공격 궤적 계획 P4). 구현: <see cref="FieldUnitPresenter"/>.
    /// 궤적 미리보기의 직선 시작점이 씁니다. 실제 직선 공격이 자세를 빌릴 때 읽는 위치(표시 Transform 위치)와 같은 값입니다.
    /// 대여·자세·배치를 바꾸지 않습니다.
    /// </summary>
    internal interface ICombatUnitPlacementReader
    {
        /// <summary>그 ID의 표시가 대여 중이고 켜져 있으면 월드 위치를 주고 true입니다. 아니면 false입니다.</summary>
        bool TryGetPlacedPosition(EntityId id, out Vector3 position);
    }
}
