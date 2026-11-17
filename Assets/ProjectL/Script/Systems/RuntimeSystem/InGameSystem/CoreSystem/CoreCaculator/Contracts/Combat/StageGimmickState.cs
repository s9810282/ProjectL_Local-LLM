using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public readonly struct StageGimmickBoundary
    {
        public Owner EndedSide { get; }
        public long CompletedControllerTurnCount { get; }
        public StageGimmickBoundaryPhase Phase { get; }

        public StageGimmickBoundary(Owner endedSide, long completedControllerTurnCount,
            StageGimmickBoundaryPhase phase = StageGimmickBoundaryPhase.Scheduled)
        {
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            if (completedControllerTurnCount < 1) throw new ArgumentOutOfRangeException(nameof(completedControllerTurnCount));
            if (!Enum.IsDefined(typeof(StageGimmickBoundaryPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            EndedSide = endedSide;
            CompletedControllerTurnCount = completedControllerTurnCount;
            Phase = phase;
        }
    }

    public enum RockfallGimmickPhase
    {
        Cooldown,
        Warning
    }

    public enum HaboobGimmickPhase
    {
        Active,
        RelocationWarning
    }

    public abstract class StageGimmickState
    {
        public StageGimmickId Id { get; }
        public StageGimmickKey Key { get; }
        public long NextBoundaryCount { get; }

        private protected StageGimmickState(StageGimmickId id, StageGimmickKey key, long nextBoundaryCount)
        {
            if (id.IsEmpty) throw new ArgumentException("기믹 식별자가 필요합니다.", nameof(id));
            if (nextBoundaryCount < 1) throw new ArgumentOutOfRangeException(nameof(nextBoundaryCount));
            Id = id;
            Key = key;
            NextBoundaryCount = nextBoundaryCount;
        }

        public abstract bool SameValues(StageGimmickState other);

        /// <summary>
        /// 이 기믹이 지금 확정적으로 점유해 유닛의 이동·배치·강제 이동 착지를 막는 칸이다. 공격 대상이나 차폐가 아니다.
        /// 점유가 없는 기믹은 빈 목록이다. 새 점유 기믹은 이 값만 재정의하고 공통 점유 규칙은 고치지 않는다.
        /// </summary>
        public virtual IReadOnlyList<CellCoord> BlockingCells => Array.Empty<CellCoord>();

        internal static IReadOnlyList<CellCoord> CopyCells(IReadOnlyList<CellCoord> cells, bool allowEmpty)
        {
            var copy = (cells ?? throw new ArgumentNullException(nameof(cells))).ToArray();
            if ((!allowEmpty && copy.Length == 0) || copy.Any(c => c.X < 0 || c.Y < 0) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("기믹 칸은 음수·중복 없이 유효한 영역이어야 합니다.", nameof(cells));
            return Array.AsReadOnly(copy.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }
    }

    public sealed class RockfallGimmickState : StageGimmickState
    {
        internal EffectOriginKey BindOrigin => new EffectOriginKey(EffectOwnerKind.StageGimmick, Id.Value, (int)Key, "rockfall-bind");
        public RockfallGimmickPhase Phase { get; }
        public IReadOnlyList<CellCoord> WarnedCells { get; }

        public RockfallGimmickState(StageGimmickId id, RockfallGimmickPhase phase, long nextBoundaryCount,
            IReadOnlyList<CellCoord> warnedCells) : base(id, StageGimmickKey.Rockfall, nextBoundaryCount)
        {
            if (!Enum.IsDefined(typeof(RockfallGimmickPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            WarnedCells = CopyCells(warnedCells, true);
            if (phase == RockfallGimmickPhase.Cooldown && WarnedCells.Count != 0)
                throw new ArgumentException("낙석 대기 중에는 경고 칸이 없습니다.", nameof(warnedCells));
            Phase = phase;
        }

        public override bool SameValues(StageGimmickState other) => other is RockfallGimmickState state &&
            Id == state.Id && Phase == state.Phase && NextBoundaryCount == state.NextBoundaryCount && WarnedCells.SequenceEqual(state.WarnedCells);
    }

    public sealed class HaboobGimmickState : StageGimmickState
    {
        public HaboobGimmickPhase Phase { get; }
        public IReadOnlyList<CellCoord> Cells { get; }

        public HaboobGimmickState(StageGimmickId id, HaboobGimmickPhase phase, long nextBoundaryCount,
            IReadOnlyList<CellCoord> cells) : base(id, StageGimmickKey.Haboob, nextBoundaryCount)
        {
            if (!Enum.IsDefined(typeof(HaboobGimmickPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            Cells = CopyCells(cells, false);
            Phase = phase;
        }

        public override bool SameValues(StageGimmickState other) => other is HaboobGimmickState state &&
            Id == state.Id && Phase == state.Phase && NextBoundaryCount == state.NextBoundaryCount && Cells.SequenceEqual(state.Cells);
    }

    public sealed class StageGimmickStateSet
    {
        public static StageGimmickStateSet Empty { get; } = new StageGimmickStateSet(Array.Empty<StageGimmickState>());
        public IReadOnlyList<StageGimmickState> Entries { get; }

        public StageGimmickStateSet(IReadOnlyList<StageGimmickState> entries)
        {
            var copy = (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            if (copy.Any(e => e == null) || copy.Select(e => e.Id).Distinct().Count() != copy.Length ||
                copy.Select(e => e.Key).Distinct().Count() != copy.Length)
                throw new ArgumentException("기믹 상태의 종류와 식별자는 중복될 수 없습니다.", nameof(entries));
            Entries = Array.AsReadOnly(copy.OrderBy(e => e.Id.Value).ToArray());
        }

        public StageGimmickState Get(StageGimmickId id) => Entries.SingleOrDefault(e => e.Id == id) ??
            throw new InvalidOperationException("준비된 기믹 상태가 없습니다.");

        public StageGimmickStateSet Replace(StageGimmickState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (Get(state.Id).Key != state.Key) throw new InvalidOperationException("기믹 상태의 종류를 바꿀 수 없습니다.");
            return new StageGimmickStateSet(Entries.Select(e => e.Id == state.Id ? state : e).ToArray());
        }

        public StageGimmickStateSet Without(StageGimmickId id)
        {
            Get(id);
            return new StageGimmickStateSet(Entries.Where(e => e.Id != id).ToArray());
        }

        public bool SameValues(StageGimmickStateSet other) => other != null && Entries.Count == other.Entries.Count &&
            Entries.Zip(other.Entries, (a, b) => a.SameValues(b)).All(equal => equal);

        // 설정과 저장 사실의 대응만 검사한다. 후보 선정·피해·진행 규칙은 계산기가 소유한다.
        public void ValidateDefinitions(IReadOnlyList<StageGimmickSpec> definitions, int width, int height)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (definitions.Any(d => d == null) || definitions.Select(d => d.GimmickKey).Distinct().Count() != definitions.Count)
                throw new InvalidOperationException("기믹 정의가 비어 있거나 중복되었습니다.");
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition is RailwayGimmickSpec) continue;
                if (definition is not RockfallGimmickSpec && definition is not HaboobGimmickSpec &&
                    definition is not TornadoGimmickSpec && definition is not MeteorGimmickSpec && definition is not LavaChainGimmickSpec)
                    throw new InvalidOperationException("지원하지 않는 기믹 정의입니다.");
                var entry = Get(new StageGimmickId(i + 1));
                if (entry.Key != definition.GimmickKey) throw new InvalidOperationException("기믹 정의와 상태 종류가 다릅니다.");
            }
            foreach (var entry in Entries)
            {
                if (entry.Id.Value > definitions.Count || definitions[entry.Id.Value - 1].GimmickKey != entry.Key)
                    throw new InvalidOperationException("기믹 상태에 대응하는 정의가 없습니다.");
                if (entry is TornadoGimmickState tornado && definitions[entry.Id.Value - 1] is TornadoGimmickSpec tornadoSpec)
                {
                    tornado.ValidateDefinition(tornadoSpec, width, height);
                    continue;
                }
                if (entry is MeteorGimmickState meteor && definitions[entry.Id.Value - 1] is MeteorGimmickSpec meteorSpec)
                {
                    meteor.ValidateDefinition(meteorSpec, width, height);
                    continue;
                }
                if (entry is LavaChainGimmickState chain && definitions[entry.Id.Value - 1] is LavaChainGimmickSpec)
                {
                    chain.ValidateBoard(width, height);
                    continue;
                }
                var cells = entry is RockfallGimmickState rockfall ? rockfall.WarnedCells :
                    entry is HaboobGimmickState haboob ? haboob.Cells : throw new InvalidOperationException("지원하지 않는 기믹 상태입니다.");
                if (cells.Any(c => c.X >= width || c.Y >= height)) throw new InvalidOperationException("기믹 칸이 보드 밖입니다.");
            }
        }
    }
}
