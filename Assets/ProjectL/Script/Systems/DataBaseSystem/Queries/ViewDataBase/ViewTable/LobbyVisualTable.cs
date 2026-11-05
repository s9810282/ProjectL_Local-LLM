using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ILobbyVisualTableQuery : IServiceModule
    {
        LobbyModelData Models { get; }
        LobbyBoardData Board { get; }
        LobbyMotionData Motion { get; }
        LobbyCharacterPresentationData CharacterPresentation { get; }
        LobbyFacilityHoverData FacilityHover { get; }
    }

    internal sealed class LobbyVisualTable : ILobbyVisualTableQuery, IExportSystemServices
    {
        public LobbyModelData Models { get; }
        public LobbyBoardData Board { get; }
        public LobbyMotionData Motion { get; }
        public LobbyCharacterPresentationData CharacterPresentation { get; }
        public LobbyFacilityHoverData FacilityHover { get; }

        internal LobbyVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item == null || item is UnityEngine.Object asset && asset == null)
                {
                    throw new InvalidOperationException("[LobbyVisualTable] 역할 SO 참조가 null 또는 Missing입니다.");
                }

                switch (item)
                {
                    case LobbyModelDataSO models:
                        if (Models != null) throw Duplicate(nameof(Models));
                        Models = ReadData<LobbyModelData>(models, nameof(Models));
                        Models.Validate();
                        break;
                    case LobbyBoardDataSO board:
                        if (Board != null) throw Duplicate(nameof(Board));
                        Board = ReadData<LobbyBoardData>(board, nameof(Board));
                        Board.Validate();
                        break;
                    case LobbyMotionDataSO motion:
                        if (Motion != null) throw Duplicate(nameof(Motion));
                        Motion = ReadData<LobbyMotionData>(motion, nameof(Motion));
                        Motion.Validate();
                        break;
                    case LobbyCharacterPresentationDataSO presentation:
                        if (CharacterPresentation != null) throw Duplicate(nameof(CharacterPresentation));
                        CharacterPresentation = ReadData<LobbyCharacterPresentationData>(presentation, nameof(CharacterPresentation));
                        CharacterPresentation.Validate();
                        break;
                    case LobbyFacilityHoverDataSO hover:
                        if (FacilityHover != null) throw Duplicate(nameof(FacilityHover));
                        FacilityHover = ReadData<LobbyFacilityHoverData>(hover, nameof(FacilityHover));
                        FacilityHover.Validate();
                        break;
                    default:
                        throw new InvalidOperationException($"[LobbyVisualTable] 지원하지 않는 역할 SO입니다: {item.GetType().Name}");
                }
            }

            if (Models == null) throw Missing(nameof(Models));
            if (Board == null) throw Missing(nameof(Board));
            if (Motion == null) throw Missing(nameof(Motion));
            if (CharacterPresentation == null) throw Missing(nameof(CharacterPresentation));
            if (FacilityHover == null) throw Missing(nameof(FacilityHover));
            // 방 프리팹 검사는 LobbyVisualPrefabRules 한 곳이 맡습니다. 글자 셰이더 검사는 호출 순서에 기대지 않습니다(N-58).
            FacilityHover.ValidateLabels(Models.RoomPrefab);
        }

        private static T ReadData<T>(DatabaseItem item, string role) where T : class, IData =>
            item.GetData() as T ?? throw new InvalidOperationException($"[LobbyVisualTable] {role}: SO가 올바른 Data를 반환해야 합니다.");

        private static InvalidOperationException Duplicate(string role) =>
            new($"[LobbyVisualTable] {role}: 같은 역할의 SO를 중복 등록할 수 없습니다.");

        private static InvalidOperationException Missing(string role) =>
            new($"[LobbyVisualTable] {role}: 역할 SO가 정확히 하나 필요합니다.");

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILobbyVisualTableQuery>(this);
        }
    }
}
