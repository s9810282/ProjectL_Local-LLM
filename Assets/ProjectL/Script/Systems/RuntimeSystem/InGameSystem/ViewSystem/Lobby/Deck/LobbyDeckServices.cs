using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>
    /// 덱 팝업이 쓰는 서비스 묶음입니다(로비 메인 선택 설계 LM2). <see cref="LobbyViewServices"/>가 한 번 만들고,
    /// 덱 팝업 Factory만 받습니다. 모든 칸이 필요하므로 만들 때 null만 검사합니다.
    /// </summary>
    internal sealed class LobbyDeckServices
    {
        internal LobbyDeckServices(IUISystemService ui, Func<EntityKey, UnitVisualData> unitVisuals)
        {
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            UnitVisuals = unitVisuals ?? throw new ArgumentNullException(nameof(unitVisuals));
        }

        /// <summary>덱 화면 열기·닫기 창구입니다.</summary>
        internal IUISystemService UI { get; }

        /// <summary>유닛 외형 조회입니다. 없는 키는 표가 원인 키를 담은 예외를 던집니다.</summary>
        internal Func<EntityKey, UnitVisualData> UnitVisuals { get; }
    }
}
