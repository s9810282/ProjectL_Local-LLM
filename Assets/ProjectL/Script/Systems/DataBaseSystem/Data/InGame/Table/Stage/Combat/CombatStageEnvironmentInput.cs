using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>환경 정의 참조와 스테이지별 배치 입력입니다. 외형 회전은 View SO에서 작성합니다.</summary>
    [Serializable]
    internal sealed class CombatStageEnvironmentInput
    {
        [Serializable]
        private struct TerrainPlacementInput
        {
            [SerializeField] private TerrainDefinitionDataSO definition;
            [SerializeField, Min(1)] private long sourceId;
            [SerializeField] private CellInput cell;
            [Tooltip("Player/Enemy 중 원본을 처음부터 아는 대상입니다. 빈 배열은 양쪽 모두 모르는 상태입니다.")]
            [SerializeField] private Owner[] knownTo;
            [Tooltip("토네이도만 지원합니다. 켜면 전투 준비가 최초 칸을 무작위로 정하며 Cell은 사용하지 않습니다.")]
            [SerializeField] private bool randomizeInitialPosition;
            [Tooltip("토네이도 내부 중심의 최초 가로 방향입니다. -1은 왼쪽, 1은 오른쪽, 0은 가장자리에서 자동 결정합니다.")]
            [SerializeField, Range(-1, 1)] private int tornadoInitialHorizontalSign;

            public void Append(int index, List<StaticTerrainPlacementData> statics,
                List<TerrainPresencePlacementData> presence, List<TornadoSourceFact> tornadoes,
                List<InitialRandomTornadoPlacementSpec> randomTornadoes, List<TornadoMemberSpec> tornadoMembers)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException($"Terrain Placements[{index}]의 Definition이 없습니다.");
                }

                var data = (TerrainDefinitionData)definition.GetData();
                var owners = knownTo ?? Array.Empty<Owner>();
                if (randomizeInitialPosition)
                {
                    if (data.Kind != TerrainContentKind.Tornado)
                    {
                        throw new InvalidOperationException($"Terrain Placements[{index}]: 최초 무작위 위치는 토네이도만 지원합니다.");
                    }

                    randomTornadoes.Add(data.CreateInitialRandomTornadoPlacement(sourceId, owners));
                    tornadoMembers.Add(data.CreateTornadoMember(sourceId, owners,
                        horizontalSign: tornadoInitialHorizontalSign == 0 ? null : tornadoInitialHorizontalSign));
                    return;
                }

                var coord = cell.ToCellCoord();
                switch (data.Kind)
                {
                    case TerrainContentKind.PresenceModifier:
                        presence.Add(data.CreatePresencePlacement(sourceId, coord, owners));
                        break;
                    case TerrainContentKind.Tornado:
                        tornadoes.Add(data.CreateTornadoPlacement(sourceId, coord, owners));
                        tornadoMembers.Add(data.CreateTornadoMember(sourceId, owners, coord,
                            tornadoInitialHorizontalSign == 0 ? null : tornadoInitialHorizontalSign));
                        break;
                    default:
                        statics.Add(data.CreateStaticPlacement(sourceId, coord, owners));
                        break;
                }
            }
        }

        [Serializable]
        private struct ObstaclePlacementInput
        {
            [SerializeField] private ObstacleDefinitionDataSO definition;
            [SerializeField] private CellInput anchor;
            [SerializeField] private Rotation4 rotation;

            public InitialObstaclePlacementSpec ToData(int index)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException($"Obstacle Placements[{index}]의 Definition이 없습니다.");
                }

                return ((ObstacleDefinitionData)definition.GetData()).CreatePlacement(anchor.ToCellCoord(), rotation);
            }
        }

        [Serializable]
        private struct ObjectPlacementInput
        {
            [SerializeField] private ObjectDefinitionDataSO definition;
            [SerializeField, Min(1)] private long sourceId;
            [SerializeField] private CellInput cell;
            [SerializeField] private Owner[] knownTo;

            public InitialObjectPlacementSpec ToData(int index)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException($"Object Placements[{index}]의 Definition이 없습니다.");
                }

                return ((ObjectDefinitionData)definition.GetData()).CreatePlacement(
                    sourceId, cell.ToCellCoord(), knownTo ?? Array.Empty<Owner>());
            }
        }

        [Tooltip("정적 지형·토네이도·위치 능력치 보정을 모두 작성합니다. Source Id는 기존 인라인 지형과 Object를 포함해 유일해야 합니다.")]
        [SerializeField] private TerrainPlacementInput[] terrainPlacements = Array.Empty<TerrainPlacementInput>();
        [Tooltip("장애물의 개체 ID는 Core가 할당합니다. Rotation은 점유 크기와 규칙에 반영됩니다.")]
        [SerializeField] private ObstaclePlacementInput[] obstaclePlacements = Array.Empty<ObstaclePlacementInput>();
        [Tooltip("같은 칸의 Object 진입 순서는 이 배열 순서로 결정됩니다. Source Id의 크기로 정렬하지 않습니다.")]
        [SerializeField] private ObjectPlacementInput[] objectPlacements = Array.Empty<ObjectPlacementInput>();

        public (StaticTerrainPlacementData[] Static, TerrainPresencePlacementData[] Presence,
            TornadoSourceFact[] Tornadoes, InitialObstaclePlacementSpec[] Obstacles,
            InitialObjectPlacementSpec[] Objects, InitialRandomTornadoPlacementSpec[] RandomTornadoes, TornadoGimmickSpec Tornado)
            ToData(IReadOnlyList<StaticTerrainPlacementData> legacyTerrain)
        {
            var statics = new List<StaticTerrainPlacementData>(legacyTerrain);
            var presence = new List<TerrainPresencePlacementData>();
            var tornadoes = new List<TornadoSourceFact>();
            var randomTornadoes = new List<InitialRandomTornadoPlacementSpec>();
            var tornadoMembers = new List<TornadoMemberSpec>();
            for (int i = 0; i < (terrainPlacements?.Length ?? 0); i++)
            {
                terrainPlacements[i].Append(i, statics, presence, tornadoes, randomTornadoes, tornadoMembers);
            }

            var obstacles = new InitialObstaclePlacementSpec[obstaclePlacements?.Length ?? 0];
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacles[i] = obstaclePlacements[i].ToData(i);
            }

            var objects = new InitialObjectPlacementSpec[objectPlacements?.Length ?? 0];
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = objectPlacements[i].ToData(i);
            }

            return (statics.ToArray(), presence.ToArray(), tornadoes.ToArray(), obstacles, objects, randomTornadoes.ToArray(),
                tornadoMembers.Count == 0 ? null : new TornadoGimmickSpec(tornadoMembers));
        }
    }
}
