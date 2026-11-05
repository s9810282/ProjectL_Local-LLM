using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 사건 순간의 종류입니다(이펙트 창구 확장 설계 EC7, 계획 P15). 사실 사건 가운데 사건 순간에 소리·연출 큐가 붙는 것입니다. 0은 쓰지 않습니다.
    /// 무엇을 내는지는 소리 표(<c>CombatEventSounds</c>)와 큐 값 답(<c>CombatCueSources</c>)이 각자 이 종류로 고릅니다.
    /// </summary>
    internal enum CombatMomentKind
    {
        /// <summary>오브젝트 발동(지뢰)·습득(깃발·보안관 배지)입니다. 자리는 오브젝트 칸입니다.</summary>
        ObjectActivated = 1,

        /// <summary>오브젝트 생성입니다. 소리만 있고 큐 칸은 없습니다(결정 Q-EC5 A). 지금 Core는 이 사건을 내지 않습니다.</summary>
        ObjectCreated = 2,

        /// <summary>장애물(화약통·보급품)이 피해를 받았습니다. 자리는 장애물 칸입니다.</summary>
        ObstacleHit = 3,

        /// <summary>장애물이 파괴됐습니다. 자리는 장애물 칸입니다.</summary>
        ObstacleDestroyed = 4,

        /// <summary>유닛·구조물이 낭떠러지로 떨어지기 시작했습니다. 자리는 떨어진 칸입니다.</summary>
        Fall = 5,

        /// <summary>약한 지반이 무너져 낭떠러지가 됐습니다. 자리는 무너진 칸입니다.</summary>
        WeakGroundCollapse = 6,

        /// <summary>아이템이 쓰였습니다(효과가 겹쳐 소모만 된 경우 포함). 자리는 소지자 몸입니다.</summary>
        ItemActivated = 7,
    }

    /// <summary>
    /// 사건 하나의 순간 값입니다(이펙트 창구 확장 설계 EC7, 계획 P15). 사건 형식 표의 순간 칸(<see cref="CombatEventTypeTable.TryGetMoment"/>)이 만들고,
    /// 소리 표와 큐 값 답·내는 곳이 이 값 하나를 읽습니다. 어느 사건·조건이 순간인지는 표의 그 형식 줄 한 곳에만 있습니다.
    /// </summary>
    /// <remarks>
    /// 열쇠는 종류마다 뜻이 다릅니다: 오브젝트 = 오브젝트 종류, 장애물 = 장애물 종류, 아이템 = 아이템 키, 칸 순간(추락·약한 지반) = 0.
    /// 자리 재료는 칸 또는 주체 하나입니다. 장애물 반응은 물리 칸이 없을 수 있어(Core 피해 근원의 칸이 비면) 칸이 없을 수 있습니다.
    /// </remarks>
    internal readonly struct CombatEventMoment
    {
        private CombatEventMoment(CombatMomentKind kind, int key, CellCoord? cell, EntityId subject)
        {
            Kind = kind;
            Key = key;
            Cell = cell;
            Subject = subject;
        }

        internal CombatMomentKind Kind { get; }

        /// <summary>종류마다 뜻이 다른 열쇠입니다(설명은 형식 문서). 값 보관의 열쇠로도 씁니다. 읽을 때는 아래 형식별 속성을 씁니다.</summary>
        internal int Key { get; }

        /// <summary>사건 칸입니다. 주체 자리 순간(아이템)이거나 칸이 없는 장애물 반응이면 null입니다.</summary>
        internal CellCoord? Cell { get; }

        /// <summary>주체입니다(아이템 소지자). 칸 자리 순간이면 비어 있습니다.</summary>
        internal EntityId Subject { get; }

        /// <summary>오브젝트 종류입니다. 오브젝트 순간만 됩니다.</summary>
        internal ObjectContentKind ObjectKind => Kind is CombatMomentKind.ObjectActivated or CombatMomentKind.ObjectCreated
            ? (ObjectContentKind)Key
            : throw new InvalidOperationException($"[CombatEventMoment] 오브젝트 순간이 아닙니다: {Kind}");

        /// <summary>장애물 종류입니다. 장애물 순간만 됩니다.</summary>
        internal ObstacleContentKind ObstacleKind => Kind is CombatMomentKind.ObstacleHit or CombatMomentKind.ObstacleDestroyed
            ? (ObstacleContentKind)Key
            : throw new InvalidOperationException($"[CombatEventMoment] 장애물 순간이 아닙니다: {Kind}");

        /// <summary>아이템 키입니다. 아이템 순간만 됩니다.</summary>
        internal int ItemKey => Kind == CombatMomentKind.ItemActivated
            ? Key
            : throw new InvalidOperationException($"[CombatEventMoment] 아이템 순간이 아닙니다: {Kind}");

        /// <summary>오브젝트 발동·생성 순간입니다. 자리는 오브젝트 칸입니다.</summary>
        internal static CombatEventMoment ForObject(CombatMomentKind kind, ObjectContentKind objectKind, CellCoord cell)
        {
            if (kind is not (CombatMomentKind.ObjectActivated or CombatMomentKind.ObjectCreated))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "오브젝트 순간 종류여야 합니다.");
            return new CombatEventMoment(kind, (int)objectKind, cell, EntityId.Empty);
        }

        /// <summary>장애물 반응 순간입니다. 파괴면 파괴, 그 밖은 피해입니다(사건 소리 슬롯의 옛 갈래와 같음).</summary>
        /// <param name="cell">장애물 칸입니다. Core 피해 근원에 물리 칸이 없으면 null입니다.</param>
        internal static CombatEventMoment ForObstacle(ObstacleReactionKind reaction, ObstacleContentKind obstacle, CellCoord? cell) =>
            new(reaction == ObstacleReactionKind.Destroyed ? CombatMomentKind.ObstacleDestroyed : CombatMomentKind.ObstacleHit,
                (int)obstacle, cell, EntityId.Empty);

        /// <summary>칸 자리 순간(추락·약한 지반 붕괴)입니다. 열쇠는 0입니다.</summary>
        internal static CombatEventMoment AtCell(CombatMomentKind kind, CellCoord cell)
        {
            if (kind is not (CombatMomentKind.Fall or CombatMomentKind.WeakGroundCollapse))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "칸 자리 순간 종류여야 합니다.");
            return new CombatEventMoment(kind, 0, cell, EntityId.Empty);
        }

        /// <summary>아이템 발동 순간입니다. 자리는 소지자 몸입니다.</summary>
        internal static CombatEventMoment ForItem(int itemKey, EntityId holder) =>
            new(CombatMomentKind.ItemActivated, itemKey, null, holder);
    }
}
