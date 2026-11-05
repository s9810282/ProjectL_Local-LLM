using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>진입 원본의 종류입니다(이동 접촉 설계 M1). 몸 조회가 종류별 표시에서 갈립니다.</summary>
    internal enum CombatContactSourceKind
    {
        /// <summary>가시밭 칸(<see cref="CombatThornConsumedEvent"/>)입니다.</summary>
        Thorn,
        /// <summary>오브젝트 원본(<see cref="CombatObjectActivationEvent"/>)입니다.</summary>
        Object,
        /// <summary>
        /// 오브젝트가 아닌 일반 진입 규칙 타일 원본(<see cref="CombatMovementSourceEvent"/>, <c>TriggersArrivalRule</c>)입니다.
        /// 지금 Core에 이 원본을 만드는 진입 규칙이 없어 확장점이고, 몸 조회도 규칙이 생길 때 더합니다(설계 M2).
        /// </summary>
        Tile,
    }

    /// <summary>
    /// 결과의 각 걸음(<c>Moved</c>) 뒤에서 그 칸 진입 사건을 원본별 접촉 묶음으로 대응합니다(이동 접촉 설계 M1).
    /// 근거는 Core 사건 순서뿐입니다: Core는 걸음마다 <c>Moved</c>를 기록하고 곧바로 그 칸의 지형 진입 등록과 진입 처리
    /// (가시 → 위험 타일 → 오브젝트 → 회복 타일)를 기록하며, 원본마다 효과(피해·회복·보유 효과·사망 등)를 바로 뒤에 둡니다. 칸·이름으로 추측하지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>걸음 구간 = <c>Moved</c> 뒤 ~ 다음 이동 사건(누구든) 또는 새 행동(<see cref="CombatEventTypeTable.IsActionStart"/>) 전.</para>
    /// <para>
    /// 접촉 원본 사건 = 가시 소비, 이 걸음의 이동자가 발동한 오브젝트, 들어간 칸의 일반 진입 규칙 타일 변경입니다.
    /// 접촉 묶음 = 원본 사건 하나와, 다음 원본 사건 전까지의 사건들입니다. 오브젝트 발동 뒤 같은 원본의 변경(<see cref="CombatMovementSourceEvent"/>)은
    /// 그 오브젝트 묶음입니다. 경로 도착 처리·이동 성공 효과는 마지막 묶음 뒤에 붙습니다(설계 M1).
    /// </para>
    /// <para>
    /// 구간에서 첫 원본보다 앞선 사건(약한 지반 진입 등록·낙하·넉백 가시 피해 등)은 묶음이 없어 접촉으로 해제되지 않습니다. 시작 시점은 걸음 접촉 Gate의
    /// 규칙 3(걸음 사건 완료 뒤 — 순차 재생이라 첫 묶음 해제 때, 묶음이 없는 걸음이면 보간 끝)이고, 이동자 자신의 사건이면 규칙 1(남은 걸음 연출 끝)입니다(설계 M4).
    /// </para>
    /// <para>
    /// 공격 넉백은 가시를 사건 없이 지우므로(Core <c>AttackKnockbackCalculator</c>) 가시 묶음이 없고, 요청 거리를 다 쓴 칸의 오브젝트 발동은
    /// 공격 정산 사건 뒤(S12)에 기록됩니다. 해제 상태는 걸음 접촉 Gate가 소유합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatStepContacts
    {
        /// <summary>걸음 하나와 그 접촉 묶음들입니다(Core 순서).</summary>
        internal sealed class Step
        {
            private readonly List<Group> _groups = new();

            internal Step(CombatCharacterEvent moved) => Moved = moved;

            internal CombatCharacterEvent Moved { get; }
            internal IReadOnlyList<Group> Groups => _groups;

            internal Group Add(CombatEvent source, CombatContactSourceKind kind, CellCoord cell, long sourceId)
            {
                var group = new Group(this, _groups.Count, source, kind, cell, sourceId);
                _groups.Add(group);
                return group;
            }
        }

        /// <summary>원본 하나의 접촉 묶음입니다. 몸 조회에 필요한 원본 사실은 원본 사건에서 읽습니다.</summary>
        internal sealed class Group
        {
            internal Group(Step step, int index, CombatEvent source, CombatContactSourceKind kind, CellCoord cell, long sourceId)
            {
                Step = step;
                Index = index;
                Source = source;
                Kind = kind;
                Cell = cell;
                SourceId = sourceId;
            }

            internal Step Step { get; }
            /// <summary>걸음 안에서의 순서(0부터)입니다.</summary>
            internal int Index { get; }
            internal CombatEvent Source { get; }
            internal CombatContactSourceKind Kind { get; }
            internal CellCoord Cell { get; }
            /// <summary>오브젝트·타일 원본 ID입니다. 가시는 0입니다.</summary>
            internal long SourceId { get; }
        }

        private readonly Dictionary<CombatCharacterEvent, Step> _steps = new();
        private readonly Dictionary<CombatEvent, Step> _segments = new();
        private readonly Dictionary<CombatEvent, Group> _groups = new();

        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Clear();
            Step step = null;
            Group group = null;
            foreach (CombatEvent item in result.Events)
            {
                if (CombatEventTypeTable.IsMovement(item) || CombatEventTypeTable.IsActionStart(item))
                {
                    group = null;
                    step = null;
                    if (item is CombatCharacterEvent moved && moved.Kind == CombatEventKind.Moved)
                    {
                        step = new Step(moved);
                        // 같은 사건 인스턴스가 결과에 두 번 있으면 첫 위치를 씁니다(재생 준비가 예외로 멈추지 않게).
                        if (!_steps.TryAdd(moved, step)) step = null;
                    }
                    continue;
                }
                if (step == null) continue;
                if (!_segments.TryAdd(item, step)) continue;
                if (TryOpen(step, item, out Group opened))
                    group = opened;
                if (group != null) _groups.TryAdd(item, group);
            }
        }

        /// <summary>그 걸음의 접촉 묶음입니다. 결과에 없는 걸음이면 false입니다.</summary>
        internal bool TryGetStep(CombatCharacterEvent moved, out Step step) => _steps.TryGetValue(moved, out step);

        /// <summary>그 사건이 속한 걸음 구간입니다(묶음이 없는 앞 사건 포함).</summary>
        internal bool TryGetSegment(CombatEvent item, out Step step) => _segments.TryGetValue(item, out step);

        /// <summary>그 사건이 속한 접촉 묶음입니다(원본 사건 포함). 묶음이 없으면 false입니다.</summary>
        internal bool TryGetGroup(CombatEvent item, out Group group) => _groups.TryGetValue(item, out group);

        internal void Clear()
        {
            _steps.Clear();
            _segments.Clear();
            _groups.Clear();
        }

        /// <summary>
        /// 이동자 몸이 걸음 시작 칸 → 끝 칸으로 옮겨 갈 때 원본 몸에 처음 닿는 걸음 진행률(0~1)입니다(설계 M2).
        /// 닿지 않으면(원본이 경로 밖) 1(걸음 끝)입니다.
        /// </summary>
        /// <param name="moverStart">걸음 시작 칸에서 보간 중 표시 방향(걸음 방향으로 돈 뒤)의 이동자 몸입니다. 반 크기는 걸음 내내 같다고 봅니다.</param>
        /// <param name="moverEndCenter">같은 방향으로 걸음 끝 칸에 놓인 이동자 몸의 중심(콜라이더 오프셋 포함)입니다.</param>
        internal static float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source) =>
            CombatContactGeometry.TrySegmentContact(moverStart.Center, moverEndCenter, moverStart.HalfSize, source, out float time)
                ? time : 1f;

        private static bool TryOpen(Step step, CombatEvent item, out Group opened)
        {
            opened = null;
            switch (item)
            {
                case CombatThornConsumedEvent thorn:
                    opened = step.Add(item, CombatContactSourceKind.Thorn, thorn.Source.Cell, 0);
                    return true;
                case CombatObjectActivationEvent activation:
                    // 이 걸음의 이동자가 발동한 오브젝트만 접촉 원본입니다(깃발 떨어뜨림 즉시 획득 등 다른 도착자는 앞 묶음에 붙습니다).
                    if (activation.ActivatorId != step.Moved.Final.EntityId) return false;
                    opened = step.Add(item, CombatContactSourceKind.Object, activation.Source.Cell, activation.Source.Id);
                    return true;
                case CombatMovementSourceEvent change:
                    MovementTileFact source = change.Final ?? change.Expected;
                    // 일반 진입 규칙 타일(Core ResolveEntry의 위험·회복 단계 원본)만 진입 원본입니다. 오브젝트 원본 변경(발동 뒤 비활성·떨어뜨림)과
                    // 위치·존재로만 작용하는 원본(약한 지반 진입 등록·도착 보정, TriggersArrivalRule = false)은 앞 묶음에 붙습니다.
                    if (source == null || source is ObjectSourceFact || !source.TriggersArrivalRule) return false;
                    // 걸음이 들어간 칸(기준 칸, Core ResolveEntry의 enteredCell)의 원본만 진입 원본입니다.
                    if (step.Moved.Final.Anchor != source.Cell) return false;
                    opened = step.Add(item, CombatContactSourceKind.Tile, source.Cell, source.Id);
                    return true;
                default:
                    return false;
            }
        }
    }
}
