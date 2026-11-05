using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 사건 재생이 토네이도 표시에 닿는 입구입니다(토네이도 View 계획 TP-3). 표시 수명과 결과 끝 다시 맞추기는 표시 쪽이 맡습니다.
    /// 숨겨진 원본은 인스턴스가 없어 <see cref="IsAimed"/>가 거짓이고, 자리·방향 호출은 아무 일도 하지 않습니다.
    /// </summary>
    internal interface ICombatTornadoField
    {
        /// <summary>토네이도 연출 설정입니다. 숨겨진 원본의 던짐도 같은 값을 씁니다.</summary>
        TornadoPresentationSettingsData Settings { get; }

        /// <summary>그 원본이 보이고 방향 화살표가 켜져 있는지입니다. 판이 좁아 갈 곳이 없으면 거짓입니다.</summary>
        bool IsAimed(long sourceId);

        /// <summary>사건 시점에 그 원본의 몸을 중심 칸에 바로 놓습니다.</summary>
        void Place(long sourceId, CellCoord center, FieldGridMapper mapper);

        /// <summary>사건 시점에 방향 화살표를 맞춥니다. 비었거나 (0,0)이면 끕니다.</summary>
        void SetDirection(long sourceId, CellCoord center, CellCoord? direction, FieldGridMapper mapper);

        /// <summary>몸을 그 중심 칸까지 옮길 시작·끝 월드 자리를 답합니다. 숨겨진 원본이면 거짓입니다.</summary>
        bool TryGetBodyMove(long sourceId, CellCoord center, FieldGridMapper mapper, out Vector3 from, out Vector3 to);

        /// <summary>몸의 지금 월드 자리입니다. 숨겨진 원본이면 거짓입니다.</summary>
        bool TryGetBodyPosition(long sourceId, out Vector3 position);

        /// <summary>이동 연출 중 몸을 월드 자리에 놓습니다.</summary>
        void MoveBody(long sourceId, Vector3 position);
    }
}
