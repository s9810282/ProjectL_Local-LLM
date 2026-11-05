using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item
{
    /// <summary>지형 규칙의 입력 SO입니다. 표시 리소스와 스테이지 좌표는 포함하지 않습니다.</summary>
    [CreateAssetMenu(fileName = "NewTerrainDefinition", menuName = "ProjectL/Database/InGame/TerrainDefinition")]
    public sealed class TerrainDefinitionDataSO : DatabaseItem
    {
        [SerializeField] private TerrainContentKind kind = TerrainContentKind.HighGround;

        [Header("WeakGround")]
        [Tooltip("약한 지반에만 적용하는 진입 한도입니다.")]
        [SerializeField, Min(1)] private int entryThreshold = 3;

        [Header("PresenceModifier")]
        [Tooltip("위치 능력치 보정 지형에만 적용합니다. 음수도 허용합니다.")]
        [SerializeField] private int attackBonus;
        [SerializeField] private int maxHealthBonus;

        [Header("Tornado")]
        [Tooltip("토네이도의 첫 발동까지 완료해야 하는 진영 턴 수입니다. 양 진영의 완료를 합산합니다.")]
        [SerializeField, Min(1)] private long firstActivationAfterUnitTurns = 2;

        [Tooltip("첫 발동 이후 반복 간격입니다. 양 진영의 완료한 턴을 세며 배치로 돌아가도 초기화하지 않습니다.")]
        [SerializeField, Min(1)] private int repeatIntervalUnitTurns = 2;

        [Tooltip("중심을 기준으로 한 정사각형의 한 변 길이입니다. 양의 홀수만 허용합니다.")]
        [SerializeField, Min(1)] private int tornadoSideLength = 3;
        [Tooltip("한 번 이동할 때 진행하는 최대 대각선 칸 수입니다.")]
        [SerializeField, Min(1)] private int tornadoDiagonalDistance = 3;
        [Tooltip("이동 전 방향을 알리는 완료 진영 턴 수입니다. 첫 발동과 반복 간격을 넘을 수 없습니다.")]
        [SerializeField, Min(1)] private int tornadoWarningLeadControllerTurns = 1;

        public TerrainContentKind Kind => kind;

        public override IData GetData()
        {
            return new TerrainDefinitionData(kind,
                kind == TerrainContentKind.WeakGround ? entryThreshold : 0,
                kind == TerrainContentKind.PresenceModifier ? attackBonus : 0,
                kind == TerrainContentKind.PresenceModifier ? maxHealthBonus : 0,
                kind == TerrainContentKind.Tornado ? firstActivationAfterUnitTurns : 0,
                kind == TerrainContentKind.Tornado ? repeatIntervalUnitTurns : 0,
                tornadoSideLength, tornadoDiagonalDistance, tornadoWarningLeadControllerTurns);
        }
    }
}
