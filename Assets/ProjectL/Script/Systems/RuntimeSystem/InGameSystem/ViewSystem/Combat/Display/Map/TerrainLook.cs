using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 지형 표시 하나를 빌릴 풀의 열쇠입니다: 종류와 이웃 모양(지형 이웃 모양 설계 TC4). 회전은 열쇠가 아니라 그리기 값입니다.
    /// </summary>
    internal readonly struct TerrainLookKey : IEquatable<TerrainLookKey>
    {
        internal TerrainLookKey(TerrainContentKind kind, TerrainEdgeShape shape)
        {
            Kind = kind;
            Shape = shape;
        }

        internal TerrainContentKind Kind { get; }
        internal TerrainEdgeShape Shape { get; }

        public bool Equals(TerrainLookKey other) => Kind == other.Kind && Shape == other.Shape;
        public override bool Equals(object obj) => obj is TerrainLookKey other && Equals(other);
        public override int GetHashCode() => ((int)Kind << 8) | (int)Shape;
        public override string ToString() => $"{Kind}/{Shape}";

        public static bool operator ==(TerrainLookKey left, TerrainLookKey right) => left.Equals(right);
        public static bool operator !=(TerrainLookKey left, TerrainLookKey right) => !left.Equals(right);
    }

    /// <summary>지형 원본 하나를 어떤 모양·방향으로 그릴지의 선택 결과입니다(<see cref="CombatTerrainLook.Resolve"/>).</summary>
    internal readonly struct TerrainLook
    {
        internal TerrainLook(TerrainLookKey key, Rotation4 rotation)
        {
            Key = key;
            Rotation = rotation;
        }

        internal TerrainLookKey Key { get; }
        internal Rotation4 Rotation { get; }
        internal TerrainContentKind Kind => Key.Kind;
        internal TerrainEdgeShape Shape => Key.Shape;
    }
}
