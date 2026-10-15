# Artifact E1 Buffer Publication Handoff

Date: 2026-10-02

## Delivery and responsibility

E1 extends the existing canonical ArtifactBuffer with initialization, edit-result and preview publication. It keeps one physical Buffer and the existing Factory/ExportServices/query/key route. No assembly reference, Framework implementation, calculation service, Job or LifeCycle behavior is added.

ArtifactBuffer owns state and output retention. ArtifactBufferSnapshot owns structural validation, defensive storage copying and conversion to detached calculation input. ArtifactData's existing copying remains the storage path. Geometry, bounds, collision, definition lookup, inventory selection and battle admission remain Core/execution responsibilities. BufferSystemGroup adds only five permission queries; it contains no new Artifact processing methods.

This keeps the added work within the existing Buffer responsibility. Future calculation rules extend Core, and input timing or popup flow extends the existing Job/LifeCycle path. E1 adds no retained occupancy cache, pending-command queue, extra state owner or generic manager.

## Delivered API

The interfaces below are declared alongside the existing Artifact read/write interfaces and derive from IServiceModule. All resolve to the same ArtifactBuffer instance.

| Permission | Member | IBufferQuery property | ArtifactBufferKey |
| --- | --- | --- | --- |
| IArtifactBufferReadSink | Current | ArtifactReadSink | Read |
| IArtifactInitializationWriteSink | bool TryInitialize(ArtifactState initialState) | ArtifactInitializationWriteSink | Initialize |
| IArtifactEditWriteSink | void PublishEdit(ArtifactEditProposal proposal) | ArtifactEditWriteSink | Edit |
| IArtifactPreviewWriteSink | void PublishPreview(ArtifactPlacementPreview preview) | ArtifactPreviewWriteSink | Preview |
| IArtifactEditResultReadSink | ArtifactActionResult? LastEditResult | ArtifactEditResultReadSink | EditResults |
| IArtifactPreviewReadSink | ArtifactPlacementPreview? LastPreview | ArtifactPreviewReadSink | Previews |

ArtifactBufferSnapshot.ToCalculationState() returns a new ArtifactState with independent item and nested cell arrays. It preserves dimensions, ordering, IDs, membership, anchors, rotations and cells. Empty.ToCalculationState() throws InvalidOperationException. Systems services can use this conversion without their own mapping loops.

## Publication semantics

- TryInitialize rejects null first, accepts the first valid state and returns true, then returns false without replacing state or outputs on repeated calls. An explicit legacy Clear can reopen initialization; ordinary flow transitions do not call it.
- PublishEdit and PublishPreview reject null and require initialized Current. Faults propagate without partial publication.
- Accepted edits construct and validate the complete owned snapshot before assigning Current and LastEditResult. Accepted no-ops also publish a result. Rejected edits retain the exact Current snapshot and publish the failure result.
- Both accepted and rejected edits clear LastPreview. New preview publication replaces only LastPreview. Absent outputs are null rather than fabricated default results.
- Snapshot construction validates positive dimensions, nonnegative IDs, unique instance IDs across both lists, valid rotations, list membership, pool anchor/rotation representation and nonempty unique cells per item. It does not check placement bounds, cross-item collisions or definition-derived geometry.
- A2 validates proposal/result metadata at the Contracts boundary. Snapshot validation also protects storage from publicly accessible proposal arrays changed before publication. It does not require a result target ID to remain in the final state: ReturnToPool intentionally reports the old target while reissuing a pool ID.

Publication is synchronous and sequential, with no callbacks/yields between preparation and assignments. This is not a concurrency or transaction guarantee; the real E3 execution path must preserve the stated ordering.

## Compatibility and bypass audit

The existing IArtifactBufferWriteSink.SetSnapshot/Clear remain compatible pending A4 caller migration. SetSnapshot clears obsolete result/preview outputs; Clear empties Current and both outputs together. A3 must receive the narrow permissions above rather than the legacy reset/replacement permission.

The source audit found no direct production caller of Artifact SetSnapshot or Clear. Production declarations, service export, key and group resolution still expose the legacy write capability. Existing callers are fixture setup in ArtifactBufferTests, MapProgressBufferTests and ChapterGraphPreparationTests, with ResetSpy forwarding the query. A4 must repeat the audit and restrict or retire that capability before closing migration.

Popup output clearing and final reset authority remain E3/A4 details. E1 introduces no reset call on popup close, battle cleanup, chapter change, stage transition or lobby return.

## Verification

Unity 6000.0.73f1 EditMode filter:

`ProjectL.Tests.Framework.ArtifactBufferTests;ProjectL.Tests.Framework.MapProgressBufferTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests`

Before E1, 51/51 tests passed. The new tests first failed compilation only because the planned Buffer/snapshot/query members were absent; no RED assertions ran. After implementation, 79/79 passed: 31 ArtifactBuffer cases (28 new), 8 MapProgressBuffer cases and 40 ChapterGraphPreparation cases, with zero failures or skips.

Coverage includes initialize-once, full detached conversion, accepted/no-op/rejected publication, preview replacement, invalid structural input, mutated proposal faults, null/uninitialized faults, real factory/group identity, legacy output reset and preserving Artifact state through Run reset. An explicit boundary case confirms Buffer does not repeat placement bounds/collision rules.

The combined filter also included ArtifactPlacementRulesTests, ArtifactEditCalculatorTests and retained ArtifactServiceTests: exit 0, 186/186 passed, zero failures/skips. A fresh read-only E1 reviewer found no Critical, Important or Minor issues. Later A3/E3/A4 and concurrent publication were correctly excluded from this E1 acceptance.

Local evidence: `.superpowers/sdd/2026-10-02-artifact-core-migration-implementation-plan/E1/` contains baseline, RED, GREEN and combined logs/results, pre-E1 source copies and incoming file hashes. Incoming code/assets are preserved, including restoration of the FMOD importer's changed scan timestamp. git diff --check passed. Existing unrelated audio using-directive warnings remain outside E1. No commit or push was made.

## Next boundary

A3 now binds the delivered permissions and ToCalculationState, exposes the agreed CoreGroup.Artifacts service and injects IArtifactEditCalculation from factory-only concrete construction. See the [A3 service handoff](2026-10-03-artifact-a3-service-handoff.md), dated 2026-10-03. All 30 service cases pass; no further Buffer storage responsibility was added by A3. Legacy authority retirement remains A4.

E3 uses existing Jobs for timing/completion and LifeCycle for flow entry/exit. Authored board dimensions, the real initialization caller, intent delivery, result correlation, hover freshness, popup output clearing and final reset authority still require feature hookup. E1 tests establish Buffer behavior and composition, not production UI/Job connectivity.

## A4 closure update — 2026-10-03

A4 is delivered; see the [migration closure handoff](2026-10-02-artifact-core-migration-handoff.md). Legacy state owners/lease/catalog/old-map transfers and all Artifact SetSnapshot/Clear writer/export/query/key paths are retired after caller/GUID audit. Narrow E1 publication and A3 access remain. Useful assertions have current calculation/Buffer/combat coverage; obsolete owner mechanics are explicitly retired. Full approved assemblies pass 1300/1305 with exactly the same five pre-A4 Economy failures; source review is clean. Earlier counts and next-step notes above describe that delivery's historical state. E3 production startup/input/Job/UI activation and explicit final-reset lifetime decisions remain pending.