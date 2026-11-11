using System;
using ProjectL.Framework.DataBase;
using ProjectL.Tool.Runtime;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    [System.Serializable]
    internal sealed class StageViewPropData : IData
    {
        [SerializeField, Min(1)] private int stageKey = 1;
        [Tooltip("배경을 세우는 방식입니다. Prefab = 프리팹만, MapDefinition = 맵 SO, PrefabWithTiles = 구멍 난 바닥 프리팹 + 바닥 타일. 방식에 쓰지 않는 칸은 무시합니다.")]
        [SerializeField] private StageViewPropMode mode = StageViewPropMode.Prefab;
        [Tooltip("Prefab·PrefabWithTiles 방식에서 세울 프리팹입니다.")]
        [SerializeField] private GameObject gameObject;
        [Tooltip("MapDefinition 방식에서 쓰는 맵 작업실의 맵 SO입니다. 이 SO의 프리팹과 베이크 정보를 함께 사용합니다.")]
        [SerializeField] private MapDefinitionSO map;
        [Tooltip("필드 기준 위치 보정값입니다. PrefabWithTiles 방식에서는 높이(y)만 쓰고, 가로(x·z)는 타일 경계가 필드 칸 경계에 맞도록 코드가 정합니다.")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("필드 기준 회전 보정값(오일러 각)입니다. PrefabWithTiles 방식에서는 구멍 축이 필드 축과 평행하도록 가장 가까운 90° 단위로 맞춰집니다.")]
        [SerializeField] private Vector3 rotationOffset;
        [Tooltip("Prefab/MapDefinition에서 원본 로컬 크기에 곱하는 배율입니다. PrefabWithTiles는 이 값을 사용하지 않고 OutlineCellSpacing / 공통 제작 규격으로 자동 계산합니다.")]
        [SerializeField, CombatMapScale] private Vector3 scaleOffset = Vector3.one;
        [Tooltip("켜면 이 스테이지의 낙석 터널 폭발과 폭발 흔들림만 생략합니다. 경고 표시·경고음·돌 낙하·착지 효과·피해는 유지됩니다.")]
        [SerializeField] private bool disableRockfallTunnelExplosion;
        [Tooltip("PrefabWithTiles 방식에서 배경 구멍을 채우는 바닥 타일 배치입니다. 기준점(anchorTile)은 게임 (0,0) 칸이 놓일 타일 칸이고 맵(배경·타일)이 그 자리에 맞춰 옮겨집니다. 칸 위치는 적지 않고 순서로 정해집니다.")]
        [SerializeField] private CombatMapTilesInput mapTiles = new();

        public int StageKey => stageKey;
        public StageViewPropMode Mode => mode;

        /// <summary>낙석 터널 폭발 사용 여부입니다. 생략 값을 저장하므로 필드가 없는 기존 에셋도 켜짐을 유지합니다(편집 설계 RTE).</summary>
        public bool RockfallTunnelExplosionEnabled => !disableRockfallTunnelExplosion;

        /// <summary>세울 프리팹입니다. MapDefinition 방식이면 맵 SO의 프리팹, 다른 방식이면 <c>gameObject</c>입니다.</summary>
        public GameObject GameObject => mode == StageViewPropMode.MapDefinition ? (map != null ? map.Prefab : null) : gameObject;

        /// <summary>MapDefinition 방식의 맵 SO입니다. 다른 방식이면 null이라, 표시는 프리팹을 그대로 세웁니다.</summary>
        public MapDefinitionSO Map => mode == StageViewPropMode.MapDefinition ? map : null;

        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;
        public Vector3 ScaleOffset => scaleOffset;


        /// <summary>
        /// 바닥 타일 배치입니다. 읽을 때마다 검사한 새 값입니다(DB 규칙 P2).
        /// PrefabWithTiles 방식이 아니거나 배치가 비어 있으면 null입니다.
        /// </summary>
        public CombatMapTilesData MapTiles =>
            mode == StageViewPropMode.PrefabWithTiles && mapTiles != null ? mapTiles.CreateData(stageKey) : null;

        /// <summary>
        /// 배경 방식에 필요한 칸을 검사합니다. 배경 규칙의 원본이고, 부트 DB 생성(<c>ViewStageData</c>)이 부릅니다.
        /// </summary>
        internal void Validate()
        {
            switch (mode)
            {
                case StageViewPropMode.Prefab:
                    if (gameObject == null)
                        throw new InvalidOperationException($"[StageViewPropData] Prefab 방식에는 프리팹(gameObject)이 필요합니다. StageKey: {stageKey}");
                    break;
                case StageViewPropMode.MapDefinition:
                    if (map == null)
                        throw new InvalidOperationException($"[StageViewPropData] MapDefinition 방식에는 맵 SO(map)가 필요합니다. StageKey: {stageKey}");
                    map.Validate();
                    break;
                case StageViewPropMode.PrefabWithTiles:
                    if (gameObject == null)
                        throw new InvalidOperationException($"[StageViewPropData] PrefabWithTiles 방식에는 바닥 프리팹(gameObject)이 필요합니다. StageKey: {stageKey}");
                    // 읽기가 배치와 타일 세트를 검사한다(CombatMapTilesInput.CreateData).
                    if (MapTiles == null)
                        throw new InvalidOperationException($"[StageViewPropData] PrefabWithTiles 방식에는 바닥 타일 배치(mapTiles)가 필요합니다. StageKey: {stageKey}");
                    break;
                default:
                    throw new InvalidOperationException($"[StageViewPropData] 알 수 없는 배경 방식입니다: {(int)mode}, StageKey: {stageKey}");
            }
        }
    }

    [CreateAssetMenu(
        fileName = "NewStageViewPropData",
        menuName = "ProjectL/Database/View/StageViewPropData")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.ViewProp.Item", sourceAssembly: "ProjectL.Systems", sourceClassName: "StageViewPropDataSO")]
    internal sealed class StageViewPropDataSO : DatabaseItem
    {
        [SerializeField] private StageViewPropData stageViewPropData;

        public override IData GetData() => stageViewPropData;
    }
}
