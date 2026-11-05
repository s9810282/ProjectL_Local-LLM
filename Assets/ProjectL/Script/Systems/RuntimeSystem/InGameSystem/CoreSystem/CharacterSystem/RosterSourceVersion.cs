using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// Core 계산이 읽은 보유 원본의 버전 묶음이다. 계산 전에 <see cref="Capture(IBufferQuery)"/>로 잡고, 외부(DB·흐름) 읽기 뒤
    /// 게시 요청 직전에 <see cref="IsCurrent(IBufferQuery)"/>로 비교한다. 게시의 원자성과 최종 기대값 대조는 Buffer 거래가 맡는다.
    /// </summary>
    /// <remarks>
    /// 덮는 범위: Run 식별자, 로스터 버전(보유 기록 값·영혼 수·ID별 메인 지속 상태·획득·비우기), 확정 덱 인스턴스(덱 버전).
    /// 덮지 않는 것: 재화·Run 결과·전투 유무·현재 스테이지 선택. 이런 사실에 의존하는 작업은 그 값을 따로 잡아 비교한다.
    /// </remarks>
    internal readonly struct RosterSourceVersion
    {
        internal RunId RunId { get; }
        internal long RosterRevision { get; }
        /// <summary>확정 덱이다. 없으면 null이다. 덱 Buffer는 변경마다 새 인스턴스를 설치하므로 참조로 비교한다.</summary>
        internal RunDeckSnapshot Deck { get; }

        private RosterSourceVersion(RunId runId, long rosterRevision, RunDeckSnapshot deck)
        {
            RunId = runId;
            RosterRevision = rosterRevision;
            Deck = deck;
        }

        internal static RosterSourceVersion Capture(IBufferQuery buffers)
        {
            if (buffers == null) throw new ArgumentNullException(nameof(buffers));
            return Capture(buffers.RunProgressReadSink, buffers.CharacterReadSink, buffers.RunDeckReadSink);
        }

        internal static RosterSourceVersion Capture(IRunProgressBufferReadSink run, ICharacterBufferReadSink characters,
            IRunDeckReadSink deck)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            deck.TryGetCurrent(out var current);
            return new RosterSourceVersion(run.RunId, characters.RosterRevision, current);
        }

        internal bool IsCurrent(IBufferQuery buffers)
        {
            if (buffers == null) throw new ArgumentNullException(nameof(buffers));
            return IsCurrent(buffers.RunProgressReadSink, buffers.CharacterReadSink, buffers.RunDeckReadSink);
        }

        internal bool IsCurrent(IRunProgressBufferReadSink run, ICharacterBufferReadSink characters, IRunDeckReadSink deck)
        {
            var now = Capture(run, characters, deck);
            return now.RunId == RunId && now.RosterRevision == RosterRevision && ReferenceEquals(now.Deck, Deck);
        }
    }
}
