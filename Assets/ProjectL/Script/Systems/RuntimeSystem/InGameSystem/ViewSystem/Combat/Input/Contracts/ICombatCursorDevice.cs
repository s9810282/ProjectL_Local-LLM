using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 커서 그리드 스냅이 쓰는 장치·좌표 계약입니다(커서 그리드 스냅 설계 C3). 구현은 포인터 매퍼(<c>CombatPointerMapper</c>)가 맡고 Unity 입력 장치 호출은 그곳에만 둡니다(씬 표시 나누기 설계 DS2).
    /// </summary>
    internal interface ICombatCursorDevice
    {
        /// <summary>씬 설정으로 스냅이 켜져 있는지입니다(설계 G3).</summary>
        bool IsCursorSnapEnabled { get; }

        /// <summary>
        /// 스냅 감도입니다(설계 G5). 새 칸의 경계에서 칸 폭의 이 비율만큼 안쪽으로 들어와야 스냅합니다. 0이면 경계를 넘자마자입니다.
        /// 재생 중 Inspector에서 바꿔 비교할 수 있게 호출마다 읽습니다.
        /// </summary>
        float CursorSnapThreshold { get; }

        /// <summary>칸 중심의 화면 좌표입니다. 격자 연결 전·해제 뒤·카메라 없음·카메라 뒤쪽·카메라 화면 밖이면 false입니다.</summary>
        bool TryCellCenterToScreen(CellCoord cell, out Vector2 screen);

        /// <summary>OS 커서를 화면 좌표로 옮깁니다. 장치가 없거나 창이 포커스가 아니면 옮기지 않고 false입니다.</summary>
        bool TryWarpCursor(Vector2 screen);
    }
}
