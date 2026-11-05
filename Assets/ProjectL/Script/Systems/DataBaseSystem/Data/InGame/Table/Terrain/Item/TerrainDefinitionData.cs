using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item
{
    /// <summary>지형 종류의 불변 설정입니다. 위치와 전투 중 누적 상태는 별도 소유자가 가집니다.</summary>
    public sealed class TerrainDefinitionData : IData
    {
        public TerrainContentKind Kind { get; }
        public int EntryThreshold { get; }
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }
        public long FirstActivationAfterUnitTurns { get; }
        public int RepeatIntervalUnitTurns { get; }
        public int TornadoSideLength { get; }
        public int TornadoDiagonalDistance { get; }
        public int TornadoWarningLeadControllerTurns { get; }

        public TerrainDefinitionData(TerrainContentKind kind, int entryThreshold = 0, int attackBonus = 0,
            int maxHealthBonus = 0, long firstActivationAfterUnitTurns = 0, int? repeatIntervalUnitTurns = null,
            int tornadoSideLength = 3, int tornadoDiagonalDistance = 3, int tornadoWarningLeadControllerTurns = 1)
        {
            Kind = kind;
            EntryThreshold = entryThreshold;
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
            FirstActivationAfterUnitTurns = firstActivationAfterUnitTurns;
            RepeatIntervalUnitTurns = repeatIntervalUnitTurns ??
                (kind == TerrainContentKind.Tornado ? TornadoSourceFact.Interval : 0);
            TornadoSideLength = tornadoSideLength;
            TornadoDiagonalDistance = tornadoDiagonalDistance;
            TornadoWarningLeadControllerTurns = tornadoWarningLeadControllerTurns;
            Validate();
        }

        public void Validate()
        {
            if (Kind == TerrainContentKind.None || !System.Enum.IsDefined(typeof(TerrainContentKind), Kind))
            {
                throw new InvalidOperationException("지형 종류가 지정되지 않았거나 지원되지 않습니다.");
            }

            if (Kind == TerrainContentKind.WeakGround ? EntryThreshold < 1 : EntryThreshold != 0)
            {
                throw new InvalidOperationException("약한 지반의 진입 한도는 1 이상이어야 하며 다른 지형에는 지정할 수 없습니다.");
            }

            if (Kind != TerrainContentKind.PresenceModifier && (AttackBonus != 0 || MaxHealthBonus != 0))
            {
                throw new InvalidOperationException("위치 능력치 보정 지형만 공격력과 최대 체력 보정을 가질 수 있습니다.");
            }

            if (Kind == TerrainContentKind.Tornado ? FirstActivationAfterUnitTurns < 1 : FirstActivationAfterUnitTurns != 0)
            {
                throw new InvalidOperationException("토네이도 첫 발동 시점은 1 이상이어야 하며 다른 지형에는 지정할 수 없습니다.");
            }

            if (Kind == TerrainContentKind.Tornado ? RepeatIntervalUnitTurns < 1 : RepeatIntervalUnitTurns != 0)
            {
                throw new InvalidOperationException("토네이도 반복 간격은 1 이상이어야 하며 다른 지형에는 지정할 수 없습니다.");
            }
            if (Kind == TerrainContentKind.Tornado)
            {
                try
                {
                    CreateTornadoMember(1, Array.Empty<Owner>());
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException("토네이도의 영역·거리·경고 간격이 잘못되었습니다.", exception);
                }
            }
        }

        /// <summary>기존 지형 입력의 단일 설정 원본에서 그룹 구성원을 만든다. 최초 중심은 전투 준비가 확정한다.</summary>
        public TornadoMemberSpec CreateTornadoMember(long sourceId, IReadOnlyList<Owner> knownTo,
            CellCoord? center = null, int? horizontalSign = null)
        {
            if (Kind != TerrainContentKind.Tornado) throw new InvalidOperationException("토네이도 정의만 그룹 구성원을 만들 수 있습니다.");
            return new TornadoMemberSpec(sourceId, knownTo, center, horizontalSign, TornadoSideLength,
                TornadoDiagonalDistance, FirstActivationAfterUnitTurns, RepeatIntervalUnitTurns, TornadoWarningLeadControllerTurns);
        }

        /// <summary>정적 지형의 배치를 기존 순수 데이터로 변환합니다.</summary>
        public StaticTerrainPlacementData CreateStaticPlacement(long sourceId, CellCoord cell, IReadOnlyList<Owner> knownTo)
        {
            StaticTerrainKind kind = Kind switch
            {
                TerrainContentKind.HighGround => StaticTerrainKind.HighGround,
                TerrainContentKind.Sandstorm => StaticTerrainKind.Sandstorm,
                TerrainContentKind.Wall => StaticTerrainKind.Wall,
                TerrainContentKind.WeakGround => StaticTerrainKind.WeakGround,
                TerrainContentKind.Cliff => StaticTerrainKind.Cliff,
                _ => throw new InvalidOperationException("정적 지형만 정적 배치로 변환할 수 있습니다.")
            };
            return new StaticTerrainPlacementData(sourceId, cell, kind, knownTo, EntryThreshold);
        }

        /// <summary>위치 능력치 보정 지형의 배치를 기존 순수 데이터로 변환합니다.</summary>
        public TerrainPresencePlacementData CreatePresencePlacement(long sourceId, CellCoord cell, IReadOnlyList<Owner> knownTo)
        {
            if (Kind != TerrainContentKind.PresenceModifier)
            {
                throw new InvalidOperationException("위치 능력치 보정 지형만 위치 보정 배치로 변환할 수 있습니다.");
            }

            return new TerrainPresencePlacementData(sourceId, cell,
                new TerrainPresenceDefinitionData(AttackBonus, MaxHealthBonus), knownTo);
        }

        /// <summary>토네이도의 최초 위치와 발동 시점을 기존 Core 계약으로 변환합니다.</summary>
        public TornadoSourceFact CreateTornadoPlacement(long sourceId, CellCoord cell, IReadOnlyList<Owner> knownTo)
        {
            if (Kind != TerrainContentKind.Tornado)
            {
                throw new InvalidOperationException("토네이도만 토네이도 배치로 변환할 수 있습니다.");
            }

            return new TornadoSourceFact(sourceId, cell, knownTo, FirstActivationAfterUnitTurns,
                repeatIntervalUnitTurns: RepeatIntervalUnitTurns);
        }

        /// <summary>최초 칸을 전투 준비가 정하는 토네이도 배치를 만듭니다. 칸은 받지 않으며 발동 시점은 이 정의를 따릅니다.</summary>
        public InitialRandomTornadoPlacementSpec CreateInitialRandomTornadoPlacement(long sourceId, IReadOnlyList<Owner> knownTo)
        {
            if (Kind != TerrainContentKind.Tornado)
            {
                throw new InvalidOperationException("토네이도만 최초 무작위 배치로 변환할 수 있습니다.");
            }

            return new InitialRandomTornadoPlacementSpec(sourceId, knownTo, FirstActivationAfterUnitTurns, RepeatIntervalUnitTurns);
        }
    }
}
