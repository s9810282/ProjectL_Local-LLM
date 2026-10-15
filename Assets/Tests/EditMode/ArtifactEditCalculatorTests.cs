#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactEditCalculatorTests
    {
        private readonly IArtifactEditCalculation _calculation = new ArtifactEditCalculator();

        [Test]
        public void DefinitionCells_RemainIndependentOfSourceAndTransferSnapshots()
        {
            var cells = new[] { CellCoord.Zero };
            var definition = new ArtifactDefinition(1, 1, cells, Array.Empty<ArtifactEffectData>());
            cells[0] = new CellCoord(9, 0);
            definition.CreateSnapshot().Cells[0] = new CellCoord(9, 0);
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)definition.Cells)[0] = new CellCoord(9, 0));
            ArtifactState state = _calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactEditProposal placed = _calculation.Place(state, 1, definition, CellCoord.Zero, Rotation4.R0);
            Assert.That(placed.Result.Succeeded, Is.True);
            Assert.That(definition.Cells[0], Is.EqualTo(CellCoord.Zero));
            Assert.That(state.PoolItems.Span[0].Cells.ToArray(), Is.EqualTo(new[] { CellCoord.Zero }));
            Assert.That(placed.State!.PlacedItems.Span[0].Cells.ToArray(), Is.EqualTo(new[] { CellCoord.Zero }));
        }

        [Test]
        public void InitialInventory_UsesInitialCountAndSuppliedOrder()
        {
            ArtifactState state = _calculation.CreateInitialState(4, 4, new[]
            {
                Definition(7, 2, CellCoord.Zero, CellCoord.Right),
                Definition(0, 0, CellCoord.Zero), Definition(3, 1, CellCoord.Zero)
            });

            Assert.That(state.BoardWidth, Is.EqualTo(4));
            Assert.That(state.BoardHeight, Is.EqualTo(4));
            Assert.That(state.PlacedItems.Length, Is.Zero);
            Assert.That(state.PoolItems.Length, Is.EqualTo(3));
            AssertItem(state.PoolItems.Span[0], 7, 0, false, CellCoord.Zero, Rotation4.R0,
                CellCoord.Zero, CellCoord.Right);
            AssertItem(state.PoolItems.Span[1], 7, 1, false, CellCoord.Zero, Rotation4.R0,
                CellCoord.Zero, CellCoord.Right);
            AssertItem(state.PoolItems.Span[2], 3, 2, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero);
            ArtifactState empty = _calculation.CreateInitialState(1, 1, Array.Empty<ArtifactDefinition>());
            Assert.That(empty.PoolItems.Length, Is.Zero);
        }

        [Test]
        public void InitialInventory_RejectsDuplicateDefinitionsAndCountOverflow()
        {
            Assert.Throws<ArgumentException>(() => _calculation.CreateInitialState(4, 4,
                new[] { Definition(0, 0, CellCoord.Zero), Definition(0, 1, CellCoord.Zero) }));
            Assert.Throws<OverflowException>(() => _calculation.CreateInitialState(4, 4,
                new[] { Definition(0, int.MaxValue, CellCoord.Zero), Definition(1, 1, CellCoord.Zero) }));
            Assert.Throws<ArgumentException>(() => _calculation.CreateInitialState(4, 4,
                new ArtifactDefinition[] { null! }));
        }

        [TestCase(0, 4)]
        [TestCase(4, 0)]
        [TestCase(-1, 4)]
        public void InitialInventory_RejectsInvalidDimensions(int width, int height)
            => Assert.Throws<ArgumentOutOfRangeException>(() => _calculation.CreateInitialState(
                width, height, Array.Empty<ArtifactDefinition>()));

        [Test]
        public void Place_PreservesFailurePrecedence()
        {
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero, CellCoord.Right);
            var empty = new ArtifactState(4, 4, null, null);
            AssertRejected(_calculation.Place(empty, 0, null, new CellCoord(4, 0), (Rotation4)99),
                ArtifactActionFailureReason.DefinitionNotFound);
            AssertRejected(_calculation.Place(empty, 0, definition, new CellCoord(4, 0), (Rotation4)99),
                ArtifactActionFailureReason.NotInPool);
            var state = new ArtifactState(4, 4,
                new[] { Item(0, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right) },
                new[] { Item(1, 1, true, new CellCoord(3, 0), Rotation4.R0, new CellCoord(3, 0)) });
            AssertRejected(_calculation.Place(state, 0, definition, new CellCoord(3, 0), Rotation4.R0),
                ArtifactActionFailureReason.OutOfBounds);
            AssertRejected(_calculation.Place(state, 0, definition, new CellCoord(2, 0), Rotation4.R0),
                ArtifactActionFailureReason.CellOccupied);
        }

        [Test]
        public void Place_SelectsFirstMatchingPoolItemAndPreservesRemainingOrder()
        {
            var state = new ArtifactState(4, 4, new[]
            {
                Item(1, 5, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero),
                Item(0, 3, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right),
                Item(0, 1, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right)
            }, null);
            string before = Capture(state);
            ArtifactEditProposal proposal = _calculation.Place(state, 0,
                Definition(0, 0, CellCoord.Zero, CellCoord.Right), new CellCoord(1, 1), Rotation4.R90);

            AssertSuccess(proposal, 0, 3);
            Assert.That(proposal.State!.PoolItems.Length, Is.EqualTo(2));
            Assert.That(proposal.State.PoolItems.Span[0].InstanceId, Is.EqualTo(5));
            Assert.That(proposal.State.PoolItems.Span[1].InstanceId, Is.EqualTo(1));
            AssertItem(proposal.State.PlacedItems.Span[0], 0, 3, true, new CellCoord(1, 1), Rotation4.R90,
                new CellCoord(1, 2), new CellCoord(1, 1));
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void RejectedOperations_PreserveInputAndPriorProposals()
        {
            ArtifactDefinition definition = Definition(0, 2, CellCoord.Zero, CellCoord.Right);
            ArtifactState input = _calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactEditProposal first = _calculation.Place(input, 0, definition, CellCoord.Zero, Rotation4.R0);
            ArtifactState state = first.State!;
            string original = Capture(input);
            string accepted = Capture(state);
            var proposals = new[]
            {
                _calculation.Place(state, 99, null, CellCoord.Zero, Rotation4.R0),
                _calculation.Place(state, 0, definition, CellCoord.Zero, Rotation4.R0),
                _calculation.Move(state, 0, definition, new CellCoord(3, 3), Rotation4.R0),
                _calculation.Move(state, 99, null, CellCoord.Zero, Rotation4.R0),
                _calculation.ReturnToPool(state, 99, null),
                _calculation.ExpandBoard(state, 3, 4, Array.Empty<ArtifactDefinition>())
            };
            foreach (ArtifactEditProposal rejection in proposals)
            {
                Assert.That(rejection.Result.Succeeded, Is.False);
                Assert.That(rejection.State, Is.Null);
                Assert.That(Capture(state), Is.EqualTo(accepted));
                Assert.That(Capture(first.State!), Is.EqualTo(accepted));
                Assert.That(Capture(input), Is.EqualTo(original));
            }
            ArtifactEditProposal next = _calculation.Move(state, 0, definition, CellCoord.Right, Rotation4.R90);
            AssertSuccess(next, 0, 0);
            Assert.That(Capture(first.State!), Is.EqualTo(accepted));
        }

        [TestCase(Rotation4.R0, 0, 0, 1, 0)]
        [TestCase(Rotation4.R90, 0, 1, 0, 0)]
        [TestCase(Rotation4.R180, 1, 0, 0, 0)]
        [TestCase(Rotation4.R270, 0, 0, 0, 1)]
        public void Move_KeepsIdentityAndAllowsSelfOverlap(
            Rotation4 rotation, int firstX, int firstY, int secondX, int secondY)
        {
            ArtifactDefinition definition = Definition(0, 1, CellCoord.Zero, CellCoord.Right);
            ArtifactState initial = _calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactState placed = _calculation.Place(initial, 0, definition, CellCoord.Zero, Rotation4.R0).State!;
            string before = Capture(placed);
            ArtifactEditProposal move = _calculation.Move(placed, 0, definition, CellCoord.Zero, rotation);

            AssertSuccess(move, 0, 0);
            AssertItem(move.State!.PlacedItems.Span[0], 0, 0, true, CellCoord.Zero, rotation,
                new CellCoord(firstX, firstY), new CellCoord(secondX, secondY));
            Assert.That(move.State, Is.Not.SameAs(placed));
            Assert.That(Capture(placed), Is.EqualTo(before));
        }

        [Test]
        public void Move_IntoAnotherInstance_PreservesBothPlacements()
        {
            var state = new ArtifactState(4, 4, null, new[]
            {
                Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right),
                Item(1, 1, true, new CellCoord(2, 0), Rotation4.R0, new CellCoord(2, 0))
            });
            string before = Capture(state);
            AssertRejected(_calculation.Move(state, 0, Definition(0, 0, CellCoord.Zero, CellCoord.Right),
                CellCoord.Right, Rotation4.R0), ArtifactActionFailureReason.CellOccupied);
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [TestCase(0, ArtifactActionFailureReason.None)]
        [TestCase(3, ArtifactActionFailureReason.OutOfBounds)]
        public void IdenticalMove_IsDetachedNoOpEvenWhenDefinitionCellsChange(
            int x, ArtifactActionFailureReason previewReason)
        {
            var anchor = new CellCoord(x, 0);
            var state = new ArtifactState(4, 4, null,
                new[] { Item(0, 0, true, anchor, Rotation4.R0, anchor) });
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero, CellCoord.Right);
            string before = Capture(state);
            ArtifactPlacementPreview preview = _calculation.PreviewMove(state, 0, definition, anchor, Rotation4.R0);
            Assert.That(preview.FailureReason, Is.EqualTo(previewReason));
            ArtifactEditProposal proposal = _calculation.Move(state, 0, definition, anchor, Rotation4.R0);

            AssertSuccess(proposal, 0, 0);
            Assert.That(proposal.State, Is.Not.SameAs(state));
            Assert.That(Capture(proposal.State!), Is.EqualTo(before));
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void ReturnToPool_AllocatesFreshIdButResultTargetsRemovedInstance()
        {
            ArtifactDefinition definition = Definition(0, 1, CellCoord.Zero, CellCoord.Right);
            ArtifactState initial = _calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactState placed = _calculation.Place(initial, 0, definition, CellCoord.Up, Rotation4.R90).State!;
            string placedBefore = Capture(placed);
            ArtifactEditProposal returned = _calculation.ReturnToPool(placed, 0, definition);
            AssertSuccess(returned, 0, 0);
            Assert.That(returned.State!.PlacedItems.Length, Is.Zero);
            AssertItem(returned.State.PoolItems.Span[0], 0, 1, false, CellCoord.Zero, Rotation4.R0,
                CellCoord.Zero, CellCoord.Right);
            ArtifactState placedAgain = _calculation.Place(returned.State, 0, definition,
                CellCoord.Zero, Rotation4.R0).State!;
            ArtifactEditProposal returnedAgain = _calculation.ReturnToPool(placedAgain, 1, definition);
            AssertSuccess(returnedAgain, 0, 1);
            Assert.That(returnedAgain.State!.PoolItems.Span[0].InstanceId, Is.EqualTo(2));
            Assert.That(Capture(placed), Is.EqualTo(placedBefore));
            Assert.That(returned.State.PoolItems.Span[0].InstanceId, Is.EqualTo(1));
        }

        [Test]
        public void ReturnToPool_AppendsAfterExistingPoolAndAllocatesAcrossBothLists()
        {
            var state = new ArtifactState(4, 4,
                new[] { Item(1, 8, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) },
                new[]
                {
                    Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero),
                    Item(1, 9, true, CellCoord.Right, Rotation4.R0, CellCoord.Right)
                });
            ArtifactEditProposal result = _calculation.ReturnToPool(state, 0, Definition(0, 0, CellCoord.Zero));
            AssertSuccess(result, 0, 0);
            Assert.That(result.State!.PoolItems.Span[0].InstanceId, Is.EqualTo(8));
            Assert.That(result.State.PoolItems.Span[1].InstanceId, Is.EqualTo(10));
            Assert.That(result.State.PlacedItems.Span[0].InstanceId, Is.EqualTo(9));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReturnToPool_IdOverflowThrowsBeforeAnyMutation(bool maximumInPool)
        {
            var state = new ArtifactState(4, 4,
                maximumInPool ? new[] { Item(1, int.MaxValue, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) } : null,
                new[] { Item(0, maximumInPool ? 0 : int.MaxValue, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) });
            string before = Capture(state);
            Assert.Throws<OverflowException>(() => _calculation.ReturnToPool(state,
                maximumInPool ? 0 : int.MaxValue, Definition(0, 0, CellCoord.Zero)));
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void Expansion_PreservesEveryItemAndUsesNewDimensions()
        {
            ArtifactDefinition definition = Definition(0, 2, CellCoord.Zero, CellCoord.Right);
            ArtifactState initial = _calculation.CreateInitialState(4, 4, new[] { definition });
            ArtifactState placed = _calculation.Place(initial, 0, definition, new CellCoord(1, 1), Rotation4.R90).State!;
            ArtifactEditProposal expanded = _calculation.ExpandBoard(placed, 6, 5, new[] { definition });
            AssertSuccess(expanded, null, null);
            Assert.That(expanded.State!.BoardWidth, Is.EqualTo(6));
            Assert.That(expanded.State.BoardHeight, Is.EqualTo(5));
            AssertItemsEqual(placed.PoolItems, expanded.State.PoolItems);
            AssertItemsEqual(placed.PlacedItems, expanded.State.PlacedItems);
            AssertSuccess(_calculation.Place(expanded.State, 0, definition, new CellCoord(4, 4), Rotation4.R0), 0, 1);
            Assert.That(placed.BoardWidth, Is.EqualTo(4));
        }

        [TestCase(0, 4, ArtifactActionFailureReason.InvalidBoardSize)]
        [TestCase(4, -1, ArtifactActionFailureReason.InvalidBoardSize)]
        [TestCase(3, 5, ArtifactActionFailureReason.BoardShrinkNotAllowed)]
        [TestCase(5, 3, ArtifactActionFailureReason.BoardShrinkNotAllowed)]
        [TestCase(4, 4, ArtifactActionFailureReason.BoardSizeUnchanged)]
        public void Expansion_RejectsInvalidShrinkUnchangedBeforeDefinitionLookup(
            int width, int height, ArtifactActionFailureReason reason)
        {
            var state = new ArtifactState(4, 4, null,
                new[] { Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) });
            string before = Capture(state);
            AssertRejected(_calculation.ExpandBoard(state, width, height, Array.Empty<ArtifactDefinition>()), reason);
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void Expansion_RebuildsDistinctDefinitionsForMultipleInstancesInOrder()
        {
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero);
            var state = new ArtifactState(4, 4, null, new[]
            {
                Item(0, 7, true, CellCoord.Up, Rotation4.R270, CellCoord.Up),
                Item(0, 0, true, CellCoord.Right, Rotation4.R180, CellCoord.Right)
            });
            ArtifactEditProposal proposal = _calculation.ExpandBoard(state, 5, 5, new[] { definition });
            AssertSuccess(proposal, null, null);
            AssertItemsEqual(state.PlacedItems, proposal.State!.PlacedItems);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Expansion_GeometricRebuildFailurePreservesInput(bool collision)
        {
            var state = new ArtifactState(4, 4, null, collision
                ? new[]
                {
                    Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero),
                    Item(1, 1, true, CellCoord.Right, Rotation4.R0, CellCoord.Right)
                }
                : new[] { Item(0, 0, true, new CellCoord(3, 0), Rotation4.R0, new CellCoord(3, 0)) });
            var definitions = new[]
            {
                Definition(0, 0, CellCoord.Zero, collision ? CellCoord.Right : new CellCoord(2, 0)),
                Definition(1, 0, CellCoord.Zero)
            };
            string before = Capture(state);
            AssertRejected(_calculation.ExpandBoard(state, 5, 4, definitions),
                ArtifactActionFailureReason.BoardRebuildFailed);
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void Expansion_MissingNullOrDuplicateDefinitionFactsThrow()
        {
            var state = new ArtifactState(4, 4, null,
                new[] { Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) });
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero);
            string before = Capture(state);
            Assert.Throws<ArgumentException>(() => _calculation.ExpandBoard(state, 5, 4, Array.Empty<ArtifactDefinition>()));
            Assert.Throws<ArgumentException>(() => _calculation.ExpandBoard(state, 5, 4,
                new[] { definition, Definition(0, 0, CellCoord.Zero, CellCoord.Right) }));
            Assert.Throws<ArgumentException>(() => _calculation.ExpandBoard(state, 5, 4,
                new[] { definition, definition }));
            Assert.Throws<ArgumentException>(() => _calculation.ExpandBoard(state, 5, 4,
                new ArtifactDefinition[] { null! }));
            Assert.That(Capture(state), Is.EqualTo(before));
        }

        [Test]
        public void MissingInstance_TakesPrecedenceButExistingInstanceRequiresMatchingDefinition()
        {
            var state = new ArtifactState(4, 4, null,
                new[] { Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) });
            ArtifactDefinition wrong = Definition(1, 0, CellCoord.Zero);
            AssertRejected(_calculation.Move(state, 99, wrong, CellCoord.Zero, (Rotation4)99),
                ArtifactActionFailureReason.InstanceNotFound);
            AssertRejected(_calculation.ReturnToPool(state, 99, wrong), ArtifactActionFailureReason.InstanceNotFound);
            Assert.Throws<ArgumentException>(() => _calculation.Move(state, 0, null, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentException>(() => _calculation.ReturnToPool(state, 0, null));
            Assert.Throws<ArgumentException>(() => _calculation.Move(state, 0, wrong, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentException>(() => _calculation.ReturnToPool(state, 0, wrong));
            Assert.Throws<ArgumentException>(() => _calculation.Place(state, 0, wrong, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentException>(() => _calculation.PreviewPlace(state, 0, wrong, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentException>(() => _calculation.PreviewMove(state, 0, null, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<ArgumentException>(() => _calculation.PreviewMove(state, 0, wrong, CellCoord.Zero, Rotation4.R0));
        }

        [Test]
        public void InvalidRotation_WithAvailableTargetThrowsBeforeMutation()
        {
            ArtifactDefinition definition = Definition(0, 1, CellCoord.Zero, CellCoord.Right);
            ArtifactState state = _calculation.CreateInitialState(4, 4, new[] { definition });
            string before = Capture(state);
            Assert.Throws<ArgumentOutOfRangeException>(() => _calculation.Place(state, 0, definition, CellCoord.Zero, (Rotation4)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => _calculation.PreviewPlace(state, 0, definition, CellCoord.Zero, (Rotation4)99));
            ArtifactState placed = _calculation.Place(state, 0, definition, CellCoord.Zero, Rotation4.R0).State!;
            string placedBefore = Capture(placed);
            Assert.Throws<ArgumentOutOfRangeException>(() => _calculation.Move(placed, 0, definition, CellCoord.Zero, (Rotation4)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => _calculation.PreviewMove(placed, 0, definition, CellCoord.Zero, (Rotation4)99));
            Assert.That(Capture(state), Is.EqualTo(before));
            Assert.That(Capture(placed), Is.EqualTo(placedBefore));
        }

        [Test]
        public void EmptyInventory_PreviewIsSpatialWhilePlaceRejectsNotInPool()
        {
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero, CellCoord.Right);
            var state = new ArtifactState(4, 4, null, null);
            ArtifactPlacementPreview preview = _calculation.PreviewPlace(state, 0, definition, CellCoord.Up, Rotation4.R90);
            Assert.That(preview.FailureReason, Is.EqualTo(ArtifactActionFailureReason.None));
            Assert.That(preview.Anchor, Is.EqualTo(CellCoord.Up));
            Assert.That(preview.Shape!.Cells, Is.EqualTo(new[] { CellCoord.Up, CellCoord.Zero }));
            AssertRejected(_calculation.Place(state, 0, definition, CellCoord.Up, Rotation4.R90),
                ArtifactActionFailureReason.NotInPool);
            Assert.That(state.PoolItems.Length, Is.Zero);
            Assert.That(state.PlacedItems.Length, Is.Zero);
        }

        [TestCase(Rotation4.R0)]
        [TestCase(Rotation4.R90)]
        [TestCase(Rotation4.R180)]
        [TestCase(Rotation4.R270)]
        public void PreviewPlaceAndMove_MatchCorrespondingEditGeometry(Rotation4 rotation)
        {
            ArtifactDefinition definition = Definition(0, 2, CellCoord.Zero, CellCoord.Right);
            var state = new ArtifactState(4, 4,
                new[] { Item(0, 2, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right) },
                new[]
                {
                    Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Right),
                    Item(1, 1, true, new CellCoord(2, 2), Rotation4.R0, new CellCoord(2, 2))
                });
            string before = Capture(state);
            foreach (CellCoord anchor in new[] { CellCoord.Zero, CellCoord.Up, new CellCoord(2, 2), new CellCoord(3, 3), new CellCoord(-1, 0) })
            {
                ArtifactPlacementPreview placePreview = _calculation.PreviewPlace(state, 0, definition, anchor, rotation);
                ArtifactEditProposal place = _calculation.Place(state, 0, definition, anchor, rotation);
                Assert.That(place.Result.FailureReason, Is.EqualTo(placePreview.FailureReason));
                ArtifactPlacementPreview movePreview = _calculation.PreviewMove(state, 0, definition, anchor, rotation);
                ArtifactEditProposal move = _calculation.Move(state, 0, definition, anchor, rotation);
                Assert.That(move.Result.FailureReason, Is.EqualTo(movePreview.FailureReason));
                if (place.Result.Succeeded) AssertPlacedMatchesPreview(place.State!.PlacedItems.Span[2], placePreview);
                if (move.Result.Succeeded) AssertPlacedMatchesPreview(move.State!.PlacedItems.Span[0], movePreview);
            }
            Assert.That(Capture(state), Is.EqualTo(before));
            ArtifactPlacementPreview missingDefinition = _calculation.PreviewPlace(state, 99, null, CellCoord.Zero, rotation);
            Assert.That(missingDefinition.Shape, Is.Null);
            Assert.That(missingDefinition.FailureReason, Is.EqualTo(ArtifactActionFailureReason.DefinitionNotFound));
            ArtifactPlacementPreview missingInstance = _calculation.PreviewMove(state, 99, null, CellCoord.Zero, rotation);
            Assert.That(missingInstance.Shape, Is.Null);
            Assert.That(missingInstance.FailureReason, Is.EqualTo(ArtifactActionFailureReason.InstanceNotFound));
        }

        [Test]
        public void ProposalAndDetachedCopy_OwnBothItemArraysAndNestedCells()
        {
            var poolCells = new[] { CellCoord.Zero, CellCoord.Right };
            var placedCells = new[] { CellCoord.Up };
            var pool = new[] { Item(0, 0, false, CellCoord.Zero, Rotation4.R0, poolCells) };
            var placed = new[] { Item(1, 1, true, CellCoord.Up, Rotation4.R90, placedCells) };
            var state = new ArtifactState(4, 4, pool, placed);
            string before = Capture(state);
            var proposal = new ArtifactEditProposal(ArtifactActionResult.Success(), state);
            ArtifactState copy = ArtifactState.DetachedCopyOf(state);
            poolCells[0] = new CellCoord(9, 9);
            placedCells[0] = new CellCoord(8, 8);
            pool[0] = default;
            placed[0] = default;
            Assert.That(Capture(proposal.State!), Is.EqualTo(before));
            Assert.That(Capture(copy), Is.EqualTo(before));
            Assert.That(proposal.State, Is.Not.SameAs(state));
            Assert.That(copy, Is.Not.SameAs(state));
            Assert.Throws<ArgumentNullException>(() => ArtifactState.DetachedCopyOf(null!));
        }

        [Test]
        public void SuccessfulProposals_DetachUnchangedItemsFromInputArrays()
        {
            ArtifactDefinition definition = Definition(0, 0, CellCoord.Zero);
            var poolCells = new[] { CellCoord.Zero };
            var placedCells = new[] { CellCoord.Up };
            var state = new ArtifactState(4, 4,
                new[] { Item(0, 0, false, CellCoord.Zero, Rotation4.R0, poolCells) },
                new[] { Item(0, 1, true, CellCoord.Up, Rotation4.R0, placedCells) });
            var results = new[]
            {
                _calculation.Place(state, 0, definition, CellCoord.Right, Rotation4.R0),
                _calculation.Move(state, 1, definition, CellCoord.Up, Rotation4.R0),
                _calculation.ReturnToPool(state, 1, definition),
                _calculation.ExpandBoard(state, 5, 4, new[] { definition })
            };
            var before = new string[results.Length];
            for (int i = 0; i < results.Length; i++) before[i] = Capture(results[i].State!);
            poolCells[0] = new CellCoord(9, 9);
            placedCells[0] = new CellCoord(8, 8);
            for (int i = 0; i < results.Length; i++)
                Assert.That(Capture(results[i].State!), Is.EqualTo(before[i]));
        }

        [Test]
        public void Proposal_RejectsInvalidResultStateCombinations()
        {
            var valid = new ArtifactState(4, 4, null, null);
            Assert.Throws<ArgumentException>(() => new ArtifactEditProposal(ArtifactActionResult.Success(), null));
            Assert.Throws<ArgumentException>(() => new ArtifactEditProposal(
                ArtifactActionResult.Failure(ArtifactActionFailureReason.NotInPool), valid));
            Assert.Throws<ArgumentException>(() => new ArtifactEditProposal(default, null));
            var noOp = new ArtifactEditProposal(ArtifactActionResult.Success(), valid);
            Assert.That(noOp.State, Is.Not.SameAs(valid));
            Assert.That(Capture(noOp.State!), Is.EqualTo(Capture(valid)));
        }

        [TestCase("dimensions")]
        [TestCase("negativeArtifactId")]
        [TestCase("negativeInstanceId")]
        [TestCase("duplicateInstanceId")]
        [TestCase("poolMembership")]
        [TestCase("placedMembership")]
        [TestCase("poolAnchor")]
        [TestCase("poolRotation")]
        [TestCase("invalidRotation")]
        [TestCase("emptyCells")]
        [TestCase("duplicateCells")]
        public void Proposal_RejectsMalformedStateStructure(string violation)
        {
            var pool = new[] { Item(0, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) };
            var placed = new[] { Item(1, 1, true, CellCoord.Up, Rotation4.R0, CellCoord.Up) };
            int width = 4;
            switch (violation)
            {
                case "dimensions": width = 0; break;
                case "negativeArtifactId": pool[0] = Item(-1, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero); break;
                case "negativeInstanceId": placed[0] = Item(1, -1, true, CellCoord.Up, Rotation4.R0, CellCoord.Up); break;
                case "duplicateInstanceId": placed[0] = Item(1, 0, true, CellCoord.Up, Rotation4.R0, CellCoord.Up); break;
                case "poolMembership": pool[0] = Item(0, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero); break;
                case "placedMembership": placed[0] = Item(1, 1, false, CellCoord.Up, Rotation4.R0, CellCoord.Up); break;
                case "poolAnchor": pool[0] = Item(0, 0, false, CellCoord.Up, Rotation4.R0, CellCoord.Zero); break;
                case "poolRotation": pool[0] = Item(0, 0, false, CellCoord.Zero, Rotation4.R90, CellCoord.Zero); break;
                case "invalidRotation": placed[0] = Item(1, 1, true, CellCoord.Up, (Rotation4)99, CellCoord.Up); break;
                case "emptyCells": placed[0] = Item(1, 1, true, CellCoord.Up, Rotation4.R0); break;
                case "duplicateCells": placed[0] = Item(1, 1, true, CellCoord.Up, Rotation4.R0, CellCoord.Up, CellCoord.Up); break;
            }
            Assert.Throws<ArgumentException>(() => new ArtifactEditProposal(
                ArtifactActionResult.Success(), new ArtifactState(width, 4, pool, placed)));
        }

        [TestCase(ArtifactActionFailureReason.None)]
        [TestCase(ArtifactActionFailureReason.OutOfBounds)]
        [TestCase(ArtifactActionFailureReason.CellOccupied)]
        [TestCase(ArtifactActionFailureReason.DefinitionNotFound)]
        [TestCase(ArtifactActionFailureReason.InstanceNotFound)]
        public void Preview_RequiresShapeOnlyForSpatialOutcomes(ArtifactActionFailureReason reason)
        {
            var shape = new ArtifactShapeSnapshot(1, 1, new[] { CellCoord.Zero });
            bool spatial = reason == ArtifactActionFailureReason.None || reason == ArtifactActionFailureReason.OutOfBounds ||
                reason == ArtifactActionFailureReason.CellOccupied;
            var preview = new ArtifactPlacementPreview(spatial ? shape : null, CellCoord.Up, reason);
            Assert.That(preview.Shape, spatial ? Is.SameAs(shape) : Is.Null);
            Assert.That(preview.Anchor, Is.EqualTo(CellCoord.Up));
            Assert.That(preview.FailureReason, Is.EqualTo(reason));
            Assert.Throws<ArgumentException>(() => new ArtifactPlacementPreview(spatial ? null : shape, CellCoord.Zero, reason));
        }

        [TestCase(ArtifactActionFailureReason.NotInPool)]
        [TestCase(ArtifactActionFailureReason.LayoutLocked)]
        [TestCase(ArtifactActionFailureReason.BoardRebuildFailed)]
        [TestCase((ArtifactActionFailureReason)99)]
        public void Preview_RejectsNonSpatialReasons(ArtifactActionFailureReason reason)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ArtifactPlacementPreview(null, CellCoord.Zero, reason));

        private static ArtifactDefinition Definition(int artifactId, int count, params CellCoord[] cells)
            => new ArtifactDefinition(artifactId, count, cells, Array.Empty<ArtifactEffectData>());

        private static ArtifactItemState Item(int artifactId, int instanceId, bool placed,
            CellCoord anchor, Rotation4 rotation, params CellCoord[] cells)
            => new ArtifactItemState(artifactId, instanceId, placed, anchor, rotation, cells);

        private static void AssertRejected(ArtifactEditProposal proposal, ArtifactActionFailureReason reason)
        {
            Assert.That(proposal.Result.Succeeded, Is.False);
            Assert.That(proposal.Result.FailureReason, Is.EqualTo(reason));
            Assert.That(proposal.Result.ArtifactId, Is.Null);
            Assert.That(proposal.Result.InstanceId, Is.Null);
            Assert.That(proposal.State, Is.Null);
        }

        private static void AssertSuccess(ArtifactEditProposal proposal, int? artifactId, int? instanceId)
        {
            Assert.That(proposal.Result.Succeeded, Is.True);
            Assert.That(proposal.Result.FailureReason, Is.EqualTo(ArtifactActionFailureReason.None));
            Assert.That(proposal.Result.ArtifactId, Is.EqualTo(artifactId));
            Assert.That(proposal.Result.InstanceId, Is.EqualTo(instanceId));
            Assert.That(proposal.State, Is.Not.Null);
        }

        private static void AssertItem(ArtifactItemState item, int artifactId, int instanceId, bool placed,
            CellCoord anchor, Rotation4 rotation, params CellCoord[] cells)
        {
            Assert.That(item.ArtifactId, Is.EqualTo(artifactId));
            Assert.That(item.InstanceId, Is.EqualTo(instanceId));
            Assert.That(item.IsPlaced, Is.EqualTo(placed));
            Assert.That(item.Anchor, Is.EqualTo(anchor));
            Assert.That(item.Rotation, Is.EqualTo(rotation));
            Assert.That(item.Cells.ToArray(), Is.EqualTo(cells));
        }

        private static void AssertItemsEqual(ReadOnlyMemory<ArtifactItemState> expected, ReadOnlyMemory<ArtifactItemState> actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                ArtifactItemState item = expected.Span[i];
                AssertItem(actual.Span[i], item.ArtifactId, item.InstanceId, item.IsPlaced,
                    item.Anchor, item.Rotation, item.Cells.ToArray());
            }
        }

        private static void AssertPlacedMatchesPreview(ArtifactItemState placed, ArtifactPlacementPreview preview)
        {
            var cells = new CellCoord[preview.Shape!.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = preview.Anchor + preview.Shape.Cells[i];
            Assert.That(placed.Cells.ToArray(), Is.EqualTo(cells));
        }

        private static string Capture(ArtifactState state)
        {
            var text = new StringBuilder().Append(state.BoardWidth).Append(',').Append(state.BoardHeight);
            foreach (ReadOnlyMemory<ArtifactItemState> items in new[] { state.PoolItems, state.PlacedItems })
            {
                text.Append('|');
                foreach (ArtifactItemState item in items.Span)
                {
                    text.Append(item.ArtifactId).Append(',').Append(item.InstanceId).Append(',').Append(item.IsPlaced)
                        .Append(',').Append(item.Anchor.X).Append(',').Append(item.Anchor.Y).Append(',').Append(item.Rotation).Append(':');
                    foreach (CellCoord cell in item.Cells.Span) text.Append(cell.X).Append(',').Append(cell.Y).Append(';');
                    text.Append('/');
                }
            }
            return text.ToString();
        }
    }
}
