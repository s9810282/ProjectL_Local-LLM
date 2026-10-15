#nullable enable
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    /// <summary>상태를 보관하지 않고 전달된 사실로 편집 제안을 구성합니다.</summary>
    public sealed class ArtifactEditCalculator : IArtifactEditCalculation
    {
        public ArtifactState CreateInitialState(int width, int height, IReadOnlyList<ArtifactDefinition> definitions)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            ReadDefinitions(definitions);

            int count = 0;
            for (int i = 0; i < definitions.Count; i++) count = checked(count + definitions[i].InitialCount);
            var pool = new ArtifactItemState[count];
            int index = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                ArtifactDefinition definition = definitions[i];
                for (int j = 0; j < definition.InitialCount; j++)
                {
                    pool[index] = CreatePoolItem(definition, index);
                    index++;
                }
            }
            return new ArtifactState(width, height, pool, Array.Empty<ArtifactItemState>());
        }

        public ArtifactEditProposal Place(ArtifactState state, int artifactId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (definition == null) return Reject(ArtifactActionFailureReason.DefinitionNotFound);
            definition = RequireDefinition(artifactId, definition);
            int index = FindPoolItem(state.PoolItems, artifactId);
            if (index < 0) return Reject(ArtifactActionFailureReason.NotInPool);

            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(definition, rotation);
            ArtifactActionFailureReason failure = ArtifactPlacementRules.CheckPlacement(state, shape, anchor, null);
            if (failure != ArtifactActionFailureReason.None) return Reject(failure);

            ArtifactItemState item = state.PoolItems.Span[index];
            ArtifactItemState placed = CreatePlacedItem(item, shape, anchor, rotation);
            return Accept(state.BoardWidth, state.BoardHeight,
                RemoveItem(state.PoolItems, index), AppendItem(state.PlacedItems, placed),
                ArtifactActionResult.Success(artifactId, item.InstanceId));
        }

        public ArtifactEditProposal Move(ArtifactState state, int instanceId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            int index = FindPlacedItem(state.PlacedItems, instanceId);
            if (index < 0) return Reject(ArtifactActionFailureReason.InstanceNotFound);
            ArtifactItemState item = state.PlacedItems.Span[index];
            definition = RequireDefinition(item.ArtifactId, definition);

            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(definition, rotation);
            // 동일한 이동은 기존 배치를 유지합니다. 정의와 회전 오류는 no-op에서도 먼저 검증합니다.
            if (item.Anchor == anchor && item.Rotation == rotation)
                return new ArtifactEditProposal(ArtifactActionResult.Success(item.ArtifactId, item.InstanceId), state);

            ArtifactActionFailureReason failure = ArtifactPlacementRules.CheckPlacement(state, shape, anchor, instanceId);
            if (failure != ArtifactActionFailureReason.None) return Reject(failure);

            var placed = state.PlacedItems.ToArray();
            placed[index] = CreatePlacedItem(item, shape, anchor, rotation);
            return Accept(state.BoardWidth, state.BoardHeight, state.PoolItems.ToArray(), placed,
                ArtifactActionResult.Success(item.ArtifactId, item.InstanceId));
        }

        public ArtifactEditProposal ReturnToPool(ArtifactState state, int instanceId, ArtifactDefinition? definition)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            int index = FindPlacedItem(state.PlacedItems, instanceId);
            if (index < 0) return Reject(ArtifactActionFailureReason.InstanceNotFound);
            ArtifactItemState item = state.PlacedItems.Span[index];
            definition = RequireDefinition(item.ArtifactId, definition);

            // 반환 대상도 포함한 현재 최대 ID를 기준으로 새 ID를 계산하고, 출력 생성 전에 오버플로를 검사합니다.
            int maximumId = -1;
            foreach (ArtifactItemState poolItem in state.PoolItems.Span)
                maximumId = Math.Max(maximumId, poolItem.InstanceId);
            foreach (ArtifactItemState placedItem in state.PlacedItems.Span)
                maximumId = Math.Max(maximumId, placedItem.InstanceId);
            int returnedId = checked(maximumId + 1);

            return Accept(state.BoardWidth, state.BoardHeight,
                AppendItem(state.PoolItems, CreatePoolItem(definition, returnedId)), RemoveItem(state.PlacedItems, index),
                ArtifactActionResult.Success(item.ArtifactId, item.InstanceId));
        }

        public ArtifactEditProposal ExpandBoard(ArtifactState state, int width, int height,
            IReadOnlyList<ArtifactDefinition> placedDefinitions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (width <= 0 || height <= 0) return Reject(ArtifactActionFailureReason.InvalidBoardSize);
            if (width < state.BoardWidth || height < state.BoardHeight) return Reject(ArtifactActionFailureReason.BoardShrinkNotAllowed);
            if (width == state.BoardWidth && height == state.BoardHeight) return Reject(ArtifactActionFailureReason.BoardSizeUnchanged);

            Dictionary<int, ArtifactDefinition> definitions = ReadDefinitions(placedDefinitions);
            foreach (ArtifactItemState item in state.PlacedItems.Span)
                if (!definitions.ContainsKey(item.ArtifactId))
                    throw new ArgumentException("Every placed artifact requires a definition.", nameof(placedDefinitions));

            var rebuilt = new List<ArtifactItemState>(state.PlacedItems.Length);
            foreach (ArtifactItemState item in state.PlacedItems.Span)
            {
                ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(definitions[item.ArtifactId], item.Rotation);
                var partial = new ArtifactState(width, height, null, rebuilt.ToArray());
                if (ArtifactPlacementRules.CheckPlacement(partial, shape, item.Anchor, null) != ArtifactActionFailureReason.None)
                    return Reject(ArtifactActionFailureReason.BoardRebuildFailed);
                rebuilt.Add(CreatePlacedItem(item, shape, item.Anchor, item.Rotation));
            }
            return Accept(width, height, state.PoolItems.ToArray(), rebuilt.ToArray(), ArtifactActionResult.Success());
        }

        public ArtifactPlacementPreview PreviewPlace(ArtifactState state, int artifactId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (definition == null)
                return new ArtifactPlacementPreview(null, anchor, ArtifactActionFailureReason.DefinitionNotFound);
            definition = RequireDefinition(artifactId, definition);
            return Preview(state, definition, anchor, rotation, null);
        }

        public ArtifactPlacementPreview PreviewMove(ArtifactState state, int instanceId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            int index = FindPlacedItem(state.PlacedItems, instanceId);
            if (index < 0)
                return new ArtifactPlacementPreview(null, anchor, ArtifactActionFailureReason.InstanceNotFound);
            definition = RequireDefinition(state.PlacedItems.Span[index].ArtifactId, definition);
            return Preview(state, definition, anchor, rotation, instanceId);
        }

        private static ArtifactPlacementPreview Preview(ArtifactState state, ArtifactDefinition definition,
            CellCoord anchor, Rotation4 rotation, int? ignoredInstanceId)
        {
            ArtifactShapeSnapshot shape = ArtifactPlacementRules.GetShape(definition, rotation);
            return new ArtifactPlacementPreview(shape, anchor,
                ArtifactPlacementRules.CheckPlacement(state, shape, anchor, ignoredInstanceId));
        }

        private static ArtifactDefinition RequireDefinition(int artifactId, ArtifactDefinition? definition)
        {
            if (definition == null || definition.ArtifactId != artifactId)
                throw new ArgumentException("The supplied definition must match the target artifact.", nameof(definition));
            return definition;
        }

        private static Dictionary<int, ArtifactDefinition> ReadDefinitions(IReadOnlyList<ArtifactDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var result = new Dictionary<int, ArtifactDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                ArtifactDefinition definition = definitions[i];
                if (definition == null || !result.TryAdd(definition.ArtifactId, definition))
                    throw new ArgumentException("Definition facts must be non-null and have distinct artifact IDs.", nameof(definitions));
            }
            return result;
        }

        private static int FindPoolItem(ReadOnlyMemory<ArtifactItemState> items, int artifactId)
        {
            for (int i = 0; i < items.Length; i++)
                if (items.Span[i].ArtifactId == artifactId) return i;
            return -1;
        }

        private static int FindPlacedItem(ReadOnlyMemory<ArtifactItemState> items, int instanceId)
        {
            for (int i = 0; i < items.Length; i++)
                if (items.Span[i].InstanceId == instanceId) return i;
            return -1;
        }

        private static ArtifactItemState CreatePoolItem(ArtifactDefinition definition, int instanceId)
            => new ArtifactItemState(definition.ArtifactId, instanceId, false, CellCoord.Zero, Rotation4.R0,
                definition.CreateSnapshot().Cells);

        private static ArtifactItemState CreatePlacedItem(ArtifactItemState item, ArtifactShapeSnapshot shape,
            CellCoord anchor, Rotation4 rotation)
        {
            // A1의 경계 검사가 끝난 셀만 절대 좌표로 변환합니다.
            var cells = new CellCoord[shape.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = anchor + shape.Cells[i];
            return new ArtifactItemState(item.ArtifactId, item.InstanceId, true, anchor, rotation, cells);
        }

        private static ArtifactItemState[] RemoveItem(ReadOnlyMemory<ArtifactItemState> items, int index)
        {
            var result = new ArtifactItemState[items.Length - 1];
            items.Span.Slice(0, index).CopyTo(result);
            items.Span.Slice(index + 1).CopyTo(result.AsSpan(index));
            return result;
        }

        private static ArtifactItemState[] AppendItem(ReadOnlyMemory<ArtifactItemState> items, ArtifactItemState item)
        {
            var result = new ArtifactItemState[checked(items.Length + 1)];
            items.Span.CopyTo(result);
            result[items.Length] = item;
            return result;
        }

        private static ArtifactEditProposal Accept(int width, int height,
            ArtifactItemState[] pool, ArtifactItemState[] placed, ArtifactActionResult result)
            => new ArtifactEditProposal(result, new ArtifactState(width, height, pool, placed));

        private static ArtifactEditProposal Reject(ArtifactActionFailureReason reason)
            => new ArtifactEditProposal(ArtifactActionResult.Failure(reason), null);
    }
}
