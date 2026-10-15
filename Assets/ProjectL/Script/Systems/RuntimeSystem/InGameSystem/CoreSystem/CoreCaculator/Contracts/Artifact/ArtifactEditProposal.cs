#nullable enable
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>게시 전 완성된 행동 결과와 성공 상태를 전달하며 입력 배열과는 독립적입니다.</summary>
    public sealed class ArtifactEditProposal
    {
        public ArtifactActionResult Result { get; }
        public ArtifactState? State { get; }

        public ArtifactEditProposal(ArtifactActionResult result, ArtifactState? state)
        {
            ValidateResult(result);
            if (result.Succeeded)
            {
                if (state == null) throw new ArgumentException("A successful proposal requires state.", nameof(state));
                ValidateState(state);
                State = ArtifactState.DetachedCopyOf(state);
            }
            else if (state != null)
            {
                throw new ArgumentException("A rejected proposal must not contain state.", nameof(state));
            }
            Result = result;
        }

        private static void ValidateResult(ArtifactActionResult result)
        {
            bool valid = result.Succeeded
                ? result.FailureReason == ArtifactActionFailureReason.None &&
                  result.ArtifactId.HasValue == result.InstanceId.HasValue &&
                  (!result.ArtifactId.HasValue || (result.ArtifactId.Value >= 0 && result.InstanceId!.Value >= 0))
                : result.FailureReason != ArtifactActionFailureReason.None &&
                  Enum.IsDefined(typeof(ArtifactActionFailureReason), result.FailureReason) &&
                  !result.ArtifactId.HasValue && !result.InstanceId.HasValue;
            if (!valid) throw new ArgumentException("Invalid artifact action result metadata.", nameof(result));
        }

        private static void ValidateState(ArtifactState state)
        {
            if (state.BoardWidth <= 0 || state.BoardHeight <= 0)
                throw new ArgumentException("Proposal board dimensions must be positive.", nameof(state));

            // 구조만 검증합니다. 범위, 충돌과 정의에 따른 셀 계산은 Core 규칙의 책임입니다.
            var instanceIds = new HashSet<int>();
            ValidateItems(state.PoolItems, false, instanceIds);
            ValidateItems(state.PlacedItems, true, instanceIds);
        }

        private static void ValidateItems(ReadOnlyMemory<ArtifactItemState> source, bool placed, HashSet<int> instanceIds)
        {
            foreach (ArtifactItemState item in source.Span)
            {
                if (item.ArtifactId < 0 || item.InstanceId < 0 || !instanceIds.Add(item.InstanceId))
                    throw new ArgumentException("Proposal item IDs must be nonnegative and instance IDs unique.", "state");
                if (item.IsPlaced != placed || !Enum.IsDefined(typeof(Rotation4), item.Rotation) ||
                    (!placed && (item.Anchor != CellCoord.Zero || item.Rotation != Rotation4.R0)))
                    throw new ArgumentException("Proposal item membership or rotation is invalid.", "state");
                if (item.Cells.Length == 0)
                    throw new ArgumentException("Proposal items require cells.", "state");

                var cells = new HashSet<CellCoord>();
                foreach (CellCoord cell in item.Cells.Span)
                    if (!cells.Add(cell))
                        throw new ArgumentException("Proposal item cells must be unique.", "state");
            }
        }
    }
}
