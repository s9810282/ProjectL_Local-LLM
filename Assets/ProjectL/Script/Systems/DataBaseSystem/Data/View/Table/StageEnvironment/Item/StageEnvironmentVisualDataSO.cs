using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item
{
    /// <summary>환경 원본 하나의 외형 회전입니다. 위치와 규칙 설정은 포함하지 않습니다.</summary>
    internal readonly struct EnvironmentVisualRotation
    {
        public long SourceId { get; }
        public Rotation4 Rotation { get; }

        internal EnvironmentVisualRotation(long sourceId, Rotation4 rotation)
        {
            if (sourceId < 1 || !System.Enum.IsDefined(typeof(Rotation4), rotation))
            {
                throw new InvalidOperationException("외형 회전에는 양수 원본 ID와 유효한 Rotation4가 필요합니다.");
            }

            SourceId = sourceId;
            Rotation = rotation;
        }
    }

    /// <summary>스테이지 원본 ID별 Terrain/Object 외형 회전입니다. 빠진 원본은 R0으로 표시합니다.</summary>
    internal sealed class StageEnvironmentVisualData : IData
    {
        public int StageKey { get; }
        public IReadOnlyList<EnvironmentVisualRotation> TerrainRotations { get; }
        public IReadOnlyList<EnvironmentVisualRotation> ObjectRotations { get; }

        internal StageEnvironmentVisualData(int stageKey, IReadOnlyList<EnvironmentVisualRotation> terrainRotations,
            IReadOnlyList<EnvironmentVisualRotation> objectRotations)
        {
            if (stageKey < 1)
            {
                throw new InvalidOperationException("스테이지 표현에는 StageRoot와 같은 양수 Stage Key가 필요합니다.");
            }

            var terrain = (terrainRotations ?? Array.Empty<EnvironmentVisualRotation>()).ToArray();
            var objects = (objectRotations ?? Array.Empty<EnvironmentVisualRotation>()).ToArray();
            var all = terrain.Concat(objects).ToArray();
            if (all.Any(p => p.SourceId < 1 || !System.Enum.IsDefined(typeof(Rotation4), p.Rotation)) ||
                all.Select(p => p.SourceId).Distinct().Count() != all.Length)
            {
                throw new InvalidOperationException("Terrain/Object 외형 회전의 원본 ID는 유일하고 회전 값은 유효해야 합니다.");
            }

            StageKey = stageKey;
            TerrainRotations = Array.AsReadOnly(terrain);
            ObjectRotations = Array.AsReadOnly(objects);
        }

        public Rotation4 GetTerrainRotation(long sourceId) => TryFind(TerrainRotations, sourceId, out Rotation4 rotation) ? rotation : Rotation4.R0;
        public Rotation4 GetObjectRotation(long sourceId) => TryFind(ObjectRotations, sourceId, out Rotation4 rotation) ? rotation : Rotation4.R0;

        /// <summary>
        /// 원본 ID의 Terrain 외형 회전을 적었는지입니다. R0을 적은 것과 적지 않은 것을 구별합니다.
        /// 이웃 모양을 쓰는 지형에는 회전을 적을 수 없어서(모양이 회전을 정함) 표시 쪽이 이것으로 검사합니다(지형 이웃 모양 설계 Q-C3).
        /// </summary>
        public bool TryGetTerrainRotation(long sourceId, out Rotation4 rotation) => TryFind(TerrainRotations, sourceId, out rotation);

        /// <summary>향후 소비자가 Buffer의 분류와 대조합니다. InGame SO나 DB 규칙은 읽지 않습니다.</summary>
        public void ValidateBindings(EnvironmentSemanticBindings bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            if (TerrainRotations.Any(p => !bindings.TerrainSourceIds.Contains(p.SourceId)) ||
                ObjectRotations.Any(p => !bindings.ObjectSourceIds.Contains(p.SourceId)))
            {
                throw new InvalidOperationException("외형 회전의 원본 ID가 Buffer의 Terrain/Object 분류와 맞지 않습니다.");
            }
        }

        private static bool TryFind(IReadOnlyList<EnvironmentVisualRotation> rotations, long sourceId, out Rotation4 rotation)
        {
            foreach (var entry in rotations)
            {
                if (entry.SourceId == sourceId)
                {
                    rotation = entry.Rotation;
                    return true;
                }
            }

            rotation = Rotation4.R0;
            return false;
        }
    }

    [CreateAssetMenu(fileName = "StageEnvironmentVisual", menuName = "ProjectL/Database/View/StageEnvironmentVisual")]
    public sealed class StageEnvironmentVisualDataSO : DatabaseItem
    {
        [Serializable]
        private struct RotationInput
        {
            [SerializeField, Min(1)] private long sourceId;
            [SerializeField] private Rotation4 rotation;

            public EnvironmentVisualRotation ToData() => new EnvironmentVisualRotation(sourceId, rotation);
        }

        [Tooltip("StageRootDataSO와 같은 Stage Key입니다. 규칙 쪽 SO는 참조하지 않습니다.")]
        [SerializeField, Min(1)] private int stageKey = 1;
        [Tooltip("CombatStageData의 Terrain Source Id별 외형 회전입니다. 기존 인라인 지형도 같은 ID로 지정할 수 있습니다.")]
        [SerializeField] private RotationInput[] terrainRotations = Array.Empty<RotationInput>();
        [Tooltip("CombatStageData의 Object Source Id별 외형 회전입니다. 생략하면 R0입니다.")]
        [SerializeField] private RotationInput[] objectRotations = Array.Empty<RotationInput>();

        public override IData GetData()
        {
            var terrain = (terrainRotations ?? Array.Empty<RotationInput>()).Select(p => p.ToData()).ToArray();
            var objects = (objectRotations ?? Array.Empty<RotationInput>()).Select(p => p.ToData()).ToArray();
            return new StageEnvironmentVisualData(stageKey, terrain, objects);
        }
    }
}
