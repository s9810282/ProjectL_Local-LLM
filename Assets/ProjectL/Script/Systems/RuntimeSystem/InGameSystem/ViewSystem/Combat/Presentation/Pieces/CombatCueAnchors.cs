using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 조립 하나의 기준 시점 표입니다(범용 연출 구조 설계 G3, 계획 Gd.5·P18). 처리기가 뼈대를 만들며 채우고, 큐 붙이기(<see cref="CombatCueComposer"/>)가 읽습니다.
    /// 시작(Start)은 실행 시작이고(공격 조립 앞 항목이 있으면 그 뒤, <see cref="StartAt"/>), 기준 순간·맞음·끝은 뼈대 항목의 신호에 잇습니다. 접촉(Contact)은 조립 항목이 아니라 공격 재생의 접촉 입구가 냅니다.
    /// </summary>
    internal sealed class CombatCueAnchors
    {
        private enum AnchorKind
        {
            Unknown,
            RunStart,
            Signal,
            Contact,
        }

        private static readonly int Size = MaxAnchor() + 1;
        private readonly int[] _items;
        private readonly CombatPieceSignal[] _signals;
        // 시작(Start)을 실행 시작 대신 이 시점에 잇습니다(공격 조립 앞 항목, 유닛 행동 계획 TP-14). 없으면 실행 시작입니다.
        private CombatPieceStart? _start;
        private IReadOnlyList<CombatProjectileCueTiming> _projectileTimings;

        // 기준 시점 열거에 대응이 없는 값이 생기면 표를 처음 쓸 때 실패시킵니다(공격 종류 표 CombatAttackStyles와 같은 방식).
        static CombatCueAnchors()
        {
            foreach (CombatCueAnchor anchor in System.Enum.GetValues(typeof(CombatCueAnchor)))
            {
                if (KindOf(anchor) == AnchorKind.Unknown)
                    throw new InvalidOperationException($"[CombatCueAnchors] 기준 시점 대응이 없습니다: {anchor}");
            }
        }

        internal CombatCueAnchors()
        {
            _items = new int[Size];
            _signals = new CombatPieceSignal[Size];
            for (int i = 0; i < Size; i++)
                _items[i] = -1;
        }

        /// <summary>기준 시점을 뼈대 항목 <paramref name="item"/>의 <paramref name="signal"/>에 잇습니다. 시작·접촉은 잇지 않습니다(정해진 대응).</summary>
        internal CombatCueAnchors Set(CombatCueAnchor anchor, int item, CombatPieceSignal signal)
        {
            if (KindOf(anchor) != AnchorKind.Signal)
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "뼈대 신호에 잇는 기준 시점은 Cue·선택 이벤트 시점·Hit·End입니다.");
            if (item < 0) throw new ArgumentOutOfRangeException(nameof(item));
            _items[(int)anchor] = item;
            _signals[(int)anchor] = signal;
            return this;
        }

        /// <summary>
        /// 시작(Start)을 실행 시작 대신 <paramref name="start"/>에 잇습니다(유닛 행동 계획 TP-14). 공격 조립 앞에 수호자 나가기가 있으면 공격의 시작 큐도 그 뒤입니다.
        /// 실행 시작(<see cref="CombatPieceStartKind.AtStart"/>, 지연 0)을 넘기면 지금처럼 실행 시작입니다.
        /// </summary>
        internal CombatCueAnchors StartAt(CombatPieceStart start)
        {
            _start = start.Kind == CombatPieceStartKind.AtStart && start.Delay <= 0f ? null : start;
            return this;
        }

        /// <summary>
        /// 원거리 클립의 탄별 발사·접촉 시점입니다(작은 묶음 설계 SB1, 계획 TP-2). 넣지 않았으면 null이고, 투사체 소리는 코드 모션처럼 Cue·Hit에 붙습니다.
        /// 창구가 아니라 이 조립 하나의 시점 사실이라 창구 묶음(<see cref="CombatCueSinks"/>)이 아니라 여기 둡니다.
        /// </summary>
        internal IReadOnlyList<CombatProjectileCueTiming> ProjectileTimings => _projectileTimings;

        /// <summary>원거리 클립 조립이 만든 탄별 시점을 넣습니다(<see cref="CombatClipRangedAttack.Add"/>).</summary>
        internal CombatCueAnchors SetProjectileTimings(IReadOnlyList<CombatProjectileCueTiming> timings)
        {
            _projectileTimings = timings ?? throw new ArgumentNullException(nameof(timings));
            return this;
        }

        /// <summary>
        /// 그 기준 시점 + 지연의 항목 시점입니다. 접촉 시점은 조립 항목이 아니라 false입니다.
        /// 이 조립에 잇지 않은 시점을 큐가 고르면 예외입니다(DB 검사가 먼저 막아야 하는 데이터 오류, 설계 G6).
        /// </summary>
        internal bool TryStart(CombatCueAnchor anchor, float delay, out CombatPieceStart start)
        {
            switch (KindOf(anchor))
            {
                case AnchorKind.RunStart:
                    start = _start is not { } moved
                        ? CombatPieceStart.AtStart(delay)
                        : moved.Kind == CombatPieceStartKind.AtStart
                            ? CombatPieceStart.AtStart(moved.Delay + delay)
                            : CombatPieceStart.After(moved.Item, moved.Signal, moved.Delay + delay);
                    return true;
                case AnchorKind.Contact:
                    start = default;
                    return false;
                case AnchorKind.Signal:
                    int item = _items[(int)anchor];
                    if (item < 0)
                        throw new InvalidOperationException($"[CombatCueAnchors] 이 조립에 없는 기준 시점을 큐가 골랐습니다: {anchor}");
                    start = CombatPieceStart.After(item, _signals[(int)anchor], delay);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "정의되지 않은 기준 시점입니다.");
            }
        }

        private static AnchorKind KindOf(CombatCueAnchor anchor) => anchor switch
        {
            CombatCueAnchor.Start => AnchorKind.RunStart,
            CombatCueAnchor.Cue or CombatCueAnchor.Hit or CombatCueAnchor.End => AnchorKind.Signal,
            CombatCueAnchor.Contact => AnchorKind.Contact,
            // 클립 선택 이벤트 시점(Cue2 이상)은 번호 표가 정합니다(작은 묶음 설계 SB2).
            _ when CombatClipEventIds.IsEventAnchor(anchor) => AnchorKind.Signal,
            _ => AnchorKind.Unknown,
        };

        private static int MaxAnchor()
        {
            int max = 0;
            foreach (CombatCueAnchor anchor in System.Enum.GetValues(typeof(CombatCueAnchor)))
                max = Math.Max(max, (int)anchor);
            return max;
        }
    }
}
