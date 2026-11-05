namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>
    /// 시설 종류 목록의 원본입니다. 시설 수·도착 칸 목록·방 시설 셋·실행 표가 모두 이 목록에서 셉니다(로비 리팩토링 2차 설계 M-13).
    /// </summary>
    internal static class LobbyFacilityKinds
    {
        /// <summary>정의된 시설 종류를 열거 값 순서로 담습니다.</summary>
        internal static readonly LobbyFacilityKind[] All = (LobbyFacilityKind[])System.Enum.GetValues(typeof(LobbyFacilityKind));

        /// <summary>시설 종류 수입니다.</summary>
        internal static int Count => All.Length;

        /// <summary>정의된 시설 종류인지입니다.</summary>
        internal static bool IsDefined(LobbyFacilityKind kind) => System.Enum.IsDefined(typeof(LobbyFacilityKind), kind);
    }
}
