using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement
{
    public sealed class DeploymentZonePolicy
    {
        private readonly int _playerDeploymentDepth;
        private readonly int _enemyDeploymentDepth;
        private readonly int _mapHeight;

        public DeploymentZonePolicy(int playerDeploymentDepth, int enemyDeploymentDepth, int mapHeight)
        {
            this._playerDeploymentDepth = playerDeploymentDepth;
            this._enemyDeploymentDepth = enemyDeploymentDepth;
            this._mapHeight = mapHeight;
        }

        /// <summary>
        /// 이 Owner가 매개변수의 셀들을 전부 자기 배치 구역에 놓을 수 있는지 확인
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public bool CanDeploy(Owner owner, CellCoord[] occupiedCells)
        {
            if (owner == Owner.Neutral)
                return false;

            if (owner == Owner.Player)
            {
                for (int i = 0; i < occupiedCells.Length; i++)
                {
                    if (occupiedCells[i] == null)
                        return false;

                    if (occupiedCells[i].Y >= _playerDeploymentDepth)
                        return false;
                }

                return true;
            }
            else if (owner == Owner.Enemy)
            {
                for (int  i = 0; i < occupiedCells.Length; i++)
                {
                    if (occupiedCells[i] == null)
                        return false;

                    if (occupiedCells[i].Y < _mapHeight - _enemyDeploymentDepth)
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
