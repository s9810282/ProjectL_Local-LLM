# Stage Merge Verification Record

Date: 2026-10-04

## Integration scope

The user authorized merging `origin/feature/Yonggeun/Stage` into `feature/Cheon/refactor/core-system`, including the View adaptations required by the confirmed drag-path contract. This is a local merge; no push is included.

- Core parent: `87348b86e89647d060774d1cf1070c5224cecd2f`.
- Stage parent: `0d0dcc7074b2dd2a74743d9a66201bcf0b2a5e09`.
- Twelve conflicted files were resolved by retaining the incoming scene/entry/Flow/placement/attack ownership and integrating the current movement contract.
- The incoming CombatSceneRoot GUID and its scene references are preserved.

## Ownership and contracts

Core calculates gameplay and existing Character/Combat Buffers own runtime state. The merge does not change movement calculators, Buffer ownership, Framework infrastructure or assembly references relative to the Core parent. There is no endpoint Move compatibility API or endpoint-to-route reconstruction.

The incoming CombatViewSystem remains the whole-result owner. CombatPathInput coordinates draft and Return input through the existing CombatMovementInteraction. CombatInteraction retains placement and attack. OrderedCombatPresentation processes ordered result events within the existing View tick; CombatMovementPlayback handles movement only. Final Buffer reconciliation and the single AcknowledgePresented call remain in CombatViewSystem after all result events finish.

The actual Combat scene uses existing field, unit, hand and HUD presenters, with a movement quote label and Return button. Continuous grid mapping, inner-cell inset 0.15 and movement-step duration 0.12 seconds are explicit input/presentation tuning. Return snaps to the checkpoint rather than replaying a reverse route.

See [current integration instructions, section 8](../guides/unit-movement-attack-core-handoff.md) and the [merge plan](../plans/2026-10-04-stage-merge.md).

## Verification

Unity 6000.0.73f1 ran through the approved execution path with AppData access. The final EditMode run of the Core, Framework and CombatViewBoundary test assemblies passed **2,255/2,255**, with zero failures, zero skipped tests and Unity exit code 0.

Local evidence: `.superpowers/sdd/2026-10-04-drag-path-movement/merge-final.xml`, `merge-final.log` and `merge-final.exit-code.txt`. These generated artifacts are intentionally not committed.

The actual-scene integration test loads the authored Combat scene, database, color scopes and pooled presenters. It exercises screen-space drag input, the Core quote, movement command processing, timed movement playback, acknowledgement and the real Return button/refund. This is automated EditMode evidence, not a claim of completed manual gameplay acceptance.

Independent review found three important integration issues, all reproduced before correction:

1. Canceling attack mode could leave drag input disabled. Movement mode now derives from the existing interaction mode.
2. Canceling a draft could leave Return unavailable. Guarded Return-preview refresh restores it without repeated pending requests.
3. A presentation callback could detach the owner during Tick. The owner now checks its lifetime before continuing that tick.

An additional cleanup test reproduced a presentation-detach exception skipping display release. Cleanup now attempts both operations independently. A stale movement-preview regression also verifies that attack feedback is not cleared by an unrelated response. The focused post-review run passed 114/114 before the final full run. No critical review finding remained.

## Integration rulings

- Scene-free Core/developer compositions explicitly use an empty View group. They do not initialize a live scene View with missing mandatory services. If a caller later needs presentation, its composition must supply those services; gameplay requires no compatibility branch.
- An incoming integration fixture had overlapping deployment zones. Its geography was corrected, and its attack setup now walks an explicit validated route into range. Production deployment/range rules were preserved.
- RecordInputOnly does not manufacture an actor or movement-preview context. Validated drag movement requires live actor/preview processing; existing placement/attack recording remains available.
- Non-movement events retain the incoming immediate presentation behavior while applying event facts in order. The sequencing adapter does not invent attack VFX or another scheduler.

## Remaining dependencies

- Authored mine, Object and destination-modifier facts and live perception integration remain content/integration gates. No production values were invented.
- Normal F2 exit/transition draining remains separate pre-existing work.
- Polished non-movement VFX and drag lift/ghost styling remain View presentation work.
- Manual camera zoom, varied frame rate and UI-overlap acceptance still need gameplay checks.

The earlier T12 verification record is a historical snapshot. Its missing-scene and missing-authored-economy limitations are superseded by this merged scene and the green final run.
