using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement
{
    /// <summary>
    /// 배치 구역 판정의 원본이다. 아군은 아군 배치 구역 칸에만, 적은 아군 배치 구역 밖에만 놓인다.
    /// 판 범위·점유·진입 불가 지형 검사는 부르는 쪽이 따로 한다.
    /// </summary>
    public sealed class DeploymentZonePolicy
    {
        private readonly PlayerDeploymentZone _playerZone;

        public DeploymentZonePolicy(PlayerDeploymentZone playerZone)
        {
            _playerZone = playerZone ?? throw new ArgumentNullException(nameof(playerZone));
        }

        /// <summary>
        /// 이 Owner가 매개변수의 셀들을 전부 놓을 수 있는지 확인한다.
        /// </summary>
        public bool CanDeploy(Owner owner, CellCoord[] occupiedCells)
        {
            if (owner == Owner.Player)
            {
                for (int i = 0; i < occupiedCells.Length; i++)
                {
                    if (_playerZone.Contains(occupiedCells[i]) == false)
                        return false;
                }

                return true;
            }

            if (owner == Owner.Enemy)
            {
                for (int i = 0; i < occupiedCells.Length; i++)
                {
                    if (_playerZone.Contains(occupiedCells[i]))
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
