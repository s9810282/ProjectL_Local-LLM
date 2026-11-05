using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 연결 한 번의 시설 팝업 항목입니다. 팝업 목록이 Start·Tick·출력 복원·정리를 같은 꼴로 돌고,
    /// 시설 실행 표가 <see cref="Facility"/> 종류의 실행 항목으로 씁니다(로비 리팩토링 2차 설계 M-14).
    /// </summary>
    internal interface ILobbyPopup : ILobbyFacilityAction, IDisposable
    {
        /// <summary>이 팝업을 여는 시설 종류입니다.</summary>
        LobbyFacilityKind Facility { get; }

        /// <summary>부모가 기본 Layer를 연결한 뒤 이미 열린 화면을 인수합니다.</summary>
        void Start();

        void Tick();

        /// <summary>바꾼 공유 출력 설정을 되돌립니다. 바꾸지 않는 팝업은 아무것도 하지 않습니다.</summary>
        void RestoreOutput();
    }
}
