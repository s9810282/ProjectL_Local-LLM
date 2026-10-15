# Artifact A2 Calculation Handoff

Date: 2026-10-02

## Delivered scope

A2 adds pure initialization, edit proposals and spatial previews. A1 remains the shared geometry dependency. Existing Artifact owners remain in place; this delivery does not connect runtime editing, Buffer publication, Framework composition or UI execution.

`IArtifactEditCalculation` belongs to Core.Contracts. `ArtifactEditCalculator` belongs to Core.Application and implements the port without fields, retained catalogs, inventory, layout, leases or revisions. It references Core.Contracts and Core.Domain only. No assembly references or service registrations were changed.

## Public calculation port

The interface exposes `CreateInitialState`, `Place`, `Move`, `ReturnToPool`, `ExpandBoard`, `PreviewPlace` and `PreviewMove` with the signatures specified by A2 in the migration plan. Callers provide fresh command-local state and definition facts; the calculator performs no DB query or Buffer access.

- Initialization preserves supplied definition order and `InitialCount`, with contiguous instance IDs starting at zero. Duplicate definition IDs and total-count overflow throw before inventory allocation.
- Place selects the first matching pool item and preserves the order of remaining items. Failure precedence is missing definition, missing inventory, bounds, then collision. A mismatched supplied definition throws.
- Move preserves identity and permits overlap with its own old cells. Missing instances take precedence over definition/rotation checks. Existing targets require a matching definition.
- Identical anchor/rotation moves validate the definition and rotation, then return successful detached unchanged data.
- Return appends a definition-local pool item at zero anchor/R0. Its fresh ID is `checked(maximum current pool/placed instance ID + 1)`, including the removed target. The success result retains the removed placed instance ID.
- Expansion validates size first, then supplied definition facts. Definitions must have distinct IDs and cover every placed artifact. Placements rebuild locally through A1 in their original order. Bounds/collision failures return `BoardRebuildFailed`; missing, null or duplicate facts throw.
- Pool cells remain definition-local. Placed cells are rotated absolute coordinates. Rejection and exceptions leave supplied state and prior proposals unchanged.

## Transfer ownership and validation

`ArtifactEditProposal` contains a get-only action result and nullable state. Success requires state; rejection requires null. Default/invalid result metadata and malformed state structure are rejected before constructing the proposal.

Structural validation covers positive dimensions, nonnegative IDs, globally unique instance IDs, valid rotation values, correct pool/placed membership, zero pool anchor/R0, and nonempty unique cells within each item. It does not query definitions or replay board bounds, collision, inventory selection or battle admission.

`ArtifactState.DetachedCopyOf` copies both item arrays and every nested cell array. The proposal uses this single helper; calculator operations assemble outputs without implementing another deep state copier. Existing `ArtifactState`/`ArtifactItemState` constructors retain their prior semantics, and the existing script GUID is preserved. The copied state remains the existing transfer carrier, not a runtime owner.

`ArtifactPlacementPreview` reuses detached `ArtifactShapeSnapshot`. Spatial outcomes (`None`, `OutOfBounds`, `CellOccupied`) require a shape. Lookup failures (`DefinitionNotFound`, `InstanceNotFound`) require null shape. Other reasons and inconsistent combinations throw.

## Preview semantics and no-op compatibility

Previews calculate spatial validity through A1 using the supplied definition. They do not check inventory ownership, consume items or guarantee mutation admission. Preview/edit geometry agrees when the stored placement and supplied definition use the same geometry.

There is an explicit compatibility exception if a same-ID definition changes geometry: PreviewMove describes the supplied candidate geometry, while an identical Move preserves the existing layout as a successful no-op. A changed two-cell definition can therefore produce an OutOfBounds preview for an existing edge placement while the identical Move succeeds without changing its stored single cell. This is covered by regression tests. No inverse shape calculation or second preview policy was introduced. Production consumers must preserve the distinction between spatial preview and authoritative command execution; E3 owns production presentation decisions.

## Existing behavior coverage mapping

| Retained ArtifactServiceTests behavior | New coverage |
| --- | --- |
| Factory initialization from definition counts | InitialInventory_UsesInitialCountAndSuppliedOrder; duplicate/count-overflow cases |
| Catalog/snapshot cell independence | ProposalAndDetachedCopy_OwnBothItemArraysAndNestedCells; SuccessfulProposals_DetachUnchangedItemsFromInputArrays; A1 input-preservation tests |
| Shape rotation and preview geometry | A1 rotation/asymmetric-shape tests; PreviewPlaceAndMove_MatchCorrespondingEditGeometry |
| Placement failure precedence and inventory preservation | Place_PreservesFailurePrecedence; Place_SelectsFirstMatchingPoolItemAndPreservesRemainingOrder; RejectedOperations_PreserveInputAndPriorProposals |
| Move identity, self-overlap and other-instance collision | Move_KeepsIdentityAndAllowsSelfOverlap; Move_IntoAnotherInstance_PreservesBothPlacements |
| Successful identical movement | IdenticalMove_IsDetachedNoOpEvenWhenDefinitionCellsChange; R0 case in Move_KeepsIdentityAndAllowsSelfOverlap |
| Expansion preservation and size rejection | Expansion_PreservesEveryItemAndUsesNewDimensions; Expansion_RebuildsDistinctDefinitionsForMultipleInstancesInOrder; size/rebuild/fact rejection tests |
| Zero IDs, return identity and target-free expansion result | ReturnToPool_AllocatesFreshIdButResultTargetsRemovedInstance; ReturnToPool_AppendsAfterExistingPoolAndAllocatesAcrossBothLists; expansion success assertions |
| Invalid rotation preservation | InvalidRotation_WithAvailableTargetThrowsBeforeMutation |
| Spatial preview with empty inventory | EmptyInventory_PreviewIsSpatialWhilePlaceRejectsNotInPool |
| Placed-effect compilation and lease/disposal lifetime | Retained existing tests; not responsibilities of this calculation port. A4 must audit these separately before retirement |

## Verification

Unity 6000.0.73f1 EditMode filter:

`ProjectL.Tests.Core.ArtifactPlacementRulesTests;ProjectL.Tests.Core.ArtifactEditCalculatorTests;ProjectL.Tests.Core.ArtifactServiceTests`

Final run: exit code 0, 107 passed, 0 failed, 0 skipped (59 calculation/contract tests, 27 A1 tests, 21 retained service tests). The initial missing-contract RED run and two failing identical-move corrective cases were observed before implementation/fix.

Local evidence is retained under `.superpowers/sdd/2026-10-02-artifact-core-migration-implementation-plan/`, including `A2-final/results.xml`, `A2-final/editor.log`, exit codes and the progress ledger. Existing unrelated compiler warnings are recorded in the log; no new calculation/contract compilation errors were present.

Next-step update, 2026-10-02: the user has agreed to E2's narrow `CoreGroup.Artifacts` service exposure and E4's factory-only concrete calculator construction. A3 will have InGameCoreSystemFactory construct ArtifactEditCalculator and inject IArtifactEditCalculation; the editing service will use only that port. E1 now delivers the Buffer permissions/conversion/publication required by A3; see the [E1 handoff](2026-10-02-artifact-e1-buffer-handoff.md). The combined E1/A1/A2/retained-service regression passed 186/186 tests. A3 is now delivered; see the [A3 service handoff](2026-10-03-artifact-a3-service-handoff.md).

E3 uses the existing Job system for command timing/completion and existing LifeCycle code for flow entry/exit. Authored board dimensions, exact intent/bootstrap hookup, popup output clearing and final reset authority remain integration details. A4 legacy-owner retirement remains subject to its caller/bypass audit. E1 clears LastPreview after accepted and rejected edits; this does not change A2 calculation behavior.


Next-step update, 2026-10-03: A3 implements the approved factory/port/service/group route. All 30 new service cases pass; the expanded 490/495 result retains exactly five existing Economy authoring failures. A4 retirement and E3 production activation remain separate pending gates.

## A4 closure update — 2026-10-03

A4 is delivered; see the [migration closure handoff](2026-10-02-artifact-core-migration-handoff.md). Legacy state owners/lease/catalog/old-map transfers and all Artifact SetSnapshot/Clear writer/export/query/key paths are retired after caller/GUID audit. Narrow E1 publication and A3 access remain. Useful assertions have current calculation/Buffer/combat coverage; obsolete owner mechanics are explicitly retired. Full approved assemblies pass 1300/1305 with exactly the same five pre-A4 Economy failures; source review is clean. Earlier counts and next-step notes above describe that delivery's historical state. E3 production startup/input/Job/UI activation and explicit final-reset lifetime decisions remain pending.