#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    /// <summary>현재 경로 견적의 슬롯, 승인 문맥과 제출 번호를 소유합니다.</summary>
    internal sealed class CombatMovementQuote
    {
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatViewBufferReadSink _read;
        private readonly ICombatStateBufferReadSink _state;
        private EntityId _actor;
        private long _battle;
        private int _submittedNumber;
        private long _version;
        private MovementPreviewContext _context;

        internal CombatMovementQuote(ICombatViewBufferWriteSink write, ICombatViewBufferReadSink read,
            ICombatStateBufferReadSink state)
        {
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        internal MovementPath Path { get; private set; }
        internal int Number { get; private set; }
        internal bool HasUnhandled => _submittedNumber > _state.HandledCommandNumber;
        private bool OwnsSlot => Number != 0 && _read.HasPreview && _read.Preview.Number == Number
            && _read.Preview.Data.Kind == CombatViewPreviewKind.Move && _read.Preview.Data.EntityId == _actor
            && HasSamePath(_read.Preview.Data.Path);
        internal bool IsCurrent(long version) => _version == version && OwnsSlot;
        internal bool IsApproved => _context != null && OwnsSlot;

        internal void Request(EntityId actor, MovementPath path, long version)
        {
            _context = null;
            _battle = _state.BattleInstanceId;
            _actor = actor;
            _version = version;
            Path = path;
            var data = CombatViewPreviewData.Move(actor, path);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Number = CombatInputProfiler.SetPreview(_write, data);
#else
            Number = _write.SetPreview(data);
#endif
        }

        internal bool Matches(CombatPreviewResult response, long version) =>
            response != null && response.Move.HasValue && response.RequestNumber == Number
            && _state.HasBattle && _battle == _state.BattleInstanceId && IsCurrent(version);

        // 호출자가 현재 요청의 응답임을 대조한 뒤 승인된 문맥만 남깁니다.
        internal bool Accept(CombatPreviewResult result)
        {
            var move = result.Move.Value;
            var context = result.MovementContext;
            _context = null;
            if (!move.CanMove || context == null || !context.HasExpectedFacts || context.BattleInstanceId != _battle ||
                context.ActorId != _actor || context.Observer != Owner.Player || context.IntentKind != CombatCommandKind.Move ||
                !HasSamePath(context.Path) || move.ValidStepCount != Path.StepCount ||
                move.PathMovement == null || !HasSamePath(move.PathMovement.RequestedPath))
                return false;
            _context = context;
            return true;
        }

        internal bool TrySubmit()
        {
            if (!IsApproved || HasUnhandled || !_state.HasBattle || _battle != _state.BattleInstanceId)
                return false;
            var command = CombatViewCommandData.Move(_actor, Path, _context);
            _context = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _submittedNumber = CombatInputProfiler.Submit(_write, command);
#else
            _submittedNumber = _write.Submit(command);
#endif
            Clear();
            return true;
        }

        internal void Clear()
        {
            int number = Number;
            Number = 0;
            Path = null;
            _context = null;
            _write.ClearPreview(number);
        }

        // 경로 Drag 중 매 프레임 불려 열거자 없이 칸을 차례로 비교합니다(GC 끊김 설계 GH4). 뜻은 SequenceEqual과 같습니다.
        internal bool HasSamePath(MovementPath path)
        {
            if (Path == null || path == null)
                return false;
            if (ReferenceEquals(Path, path)) return true;
            IReadOnlyList<CellCoord> left = Path.Cells;
            IReadOnlyList<CellCoord> right = path.Cells;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
                if (!left[i].Equals(right[i]))
                    return false;
            return true;
        }
    }
}
