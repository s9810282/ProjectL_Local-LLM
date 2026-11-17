using ProjectL.Framework.DataBase;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 방·바닥 프리팹 선택의 원본입니다. 조회마다 별개의 읽기 전용 데이터를 만듭니다.</summary>
    [MovedFrom(true, sourceNamespace: "ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item", sourceAssembly: "ProjectL.Systems", sourceClassName: "LobbyVisualDataSO")]
    [CreateAssetMenu(fileName = "NewLobbyModelData", menuName = "ProjectL/Database/View/LobbyModelData")]
    internal sealed class LobbyModelDataSO : DatabaseItem
    {
        [Tooltip("시설 종류와 같은 객체의 BoxCollider를 담는 방 프리팹입니다. 저작한 루트 위치·회전·크기를 유지합니다.")]
        [SerializeField] private GameObject roomPrefab;
        [SerializeField] private GameObject floorPrefab;

        public override IData GetData()
        {
            return new LobbyModelData(roomPrefab, floorPrefab);
        }
    }
}
