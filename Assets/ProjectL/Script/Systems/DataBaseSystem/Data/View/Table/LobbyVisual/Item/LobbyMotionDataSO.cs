using ProjectL.Framework.DataBase;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 이동 연출(칸마다 뛰기)의 높이와 시간을 입력합니다.</summary>
    [CreateAssetMenu(fileName = "NewLobbyMotionData", menuName = "ProjectL/Database/View/LobbyMotionData")]
    internal sealed class LobbyMotionDataSO : DatabaseItem
    {
        [SerializeField, FormerlySerializedAs("liftHeight")] private float hopHeight = 0.35f;
        [SerializeField, FormerlySerializedAs("travelSeconds")] private float hopSeconds = 0.35f;
        [SerializeField] private float turnSeconds = 0.1f;

        public override IData GetData() => new LobbyMotionData(hopHeight, hopSeconds, turnSeconds);
    }
}
