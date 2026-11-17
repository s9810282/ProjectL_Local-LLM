using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 다음 Run 메인 선택의 쓰기 계약입니다. 덱 팝업만 받습니다(로비 메인 선택 설계 LM1·LM3).
    /// 후보 자격은 Core가 정한 목록 그대로이고, 화면은 이 목록 밖의 키를 만들지 않습니다.
    /// </summary>
    internal interface ILobbyMainSelectionWriter : ILobbyMainSelectionReader
    {
        /// <summary>Core가 돌려준 메인 후보 키입니다. 순서도 Core 응답 그대로이며 바뀌지 않습니다.</summary>
        IReadOnlyList<EntityKey> Candidates { get; }

        /// <summary>
        /// 출발 처리가 입력을 붙잡고 있어 선택을 바꿀 수 없는지입니다. 처리 중에는 팝업이 입력을 받지 않으므로
        /// 이 값은 화면 표시용이 아니라 방어용입니다.
        /// </summary>
        bool IsLocked { get; }

        /// <summary>선택을 바꿉니다. 같은 값이면 아무것도 하지 않고 false입니다.</summary>
        /// <exception cref="System.ArgumentException">후보 밖의 키입니다.</exception>
        /// <exception cref="System.InvalidOperationException">출발이 붙잡고 있습니다(<see cref="IsLocked"/>).</exception>
        bool Select(EntityKey key);
    }
}
