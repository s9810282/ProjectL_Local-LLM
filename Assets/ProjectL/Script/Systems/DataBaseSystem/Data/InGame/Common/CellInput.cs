using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Common
{
    /// <summary>
    /// SO 입력용 셀 좌표입니다. Contracts의 CellCoord는 Unity가 직렬화할 수 없어서
    /// 입력은 이 구조체로 받고 GetData에서 CellCoord로 변환합니다.
    /// </summary>
    [Serializable]
    internal struct CellInput
    {
        [SerializeField] private int x;
        [SerializeField] private int y;

        public CellInput(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public CellCoord ToCellCoord() => new CellCoord(x, y);

        /// <summary>입력 배열을 CellCoord 배열로 바꿉니다. null이면 빈 배열을 반환합니다.</summary>
        public static CellCoord[] ToCellCoords(CellInput[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return Array.Empty<CellCoord>();

            var result = new CellCoord[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
                result[i] = inputs[i].ToCellCoord();
            return result;
        }
    }
}
