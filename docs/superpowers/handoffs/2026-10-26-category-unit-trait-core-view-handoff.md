# Category and Unit Trait Rework — Core / View Handoff

Date: 2026-10-26
Status: Phase D code implemented and verified in the two authorized assemblies; three independent-review findings fixed. Manual scene/art acceptance remains open. No commit or push.

## Scope and owners

The approved A–C calculation/content changes are consumed by the existing attack interaction, ordered event playback, unit motion lanes, shown facts, unit HUD, and shared card formatter. There is no new action mode, skill button, service, event subscription graph, mark timer, or gameplay store. Core remains authoritative for eligibility, A/P, actual destination/path, damage, marks, and settlement. View routes immutable facts and owns display resources.

Plan: [category and unit trait rework](../plans/2026-10-26-category-and-unit-trait-rework-implementation-plan.md), D1. Gameplay: [design](../specs/2026-10-26-category-and-unit-trait-rework-design.md), sections 5–6. The user authorized Phase D and limited test execution to Core and CoreIntegration. Framework tests were not run.

## Consumer contracts

- `AttackRangeResult.BasicAttackCells` supplies the basic range; `RangeCells` supplies the complete selection range. `AttackAims` remains the legal input authority. `AttackTargetingInteraction` passes both lists through `ICombatInteractionFeedback.ShowRangeCells(cells, basicCells = null)`. The field displays basic cells with the attack palette and the supplied set difference with the existing movement-range palette. This does not calculate gameplay reach. One-list callers retain their previous appearance. Colors do not determine selection rules.
- `AttackPreview.Pull` and `AttackResult.Pull` are nullable `AttackPullSnapshot` values: `TargetId`, `StartAnchor`, `FinalAnchor`, immutable `Path` (actual entered cells). `ShowAffectedCells(cells, pull = null)` displays the supplied destination with the planned-movement palette, including friendly movement-only previews. Existing health previews use Core's target/secondary-damage facts. The existing `CombatAttackTarget.DeliveryCell` resolves Pull direct-hit endpoints from supplied FinalAnchor while Core PrimaryCell remains the original selected cell. Preview trajectories, attack facing, code attacks, clip shots and contact endpoints share this resolution. Movement-only Pull has no direct-attack trajectory. Clear/failure/mode-exit paths remove both affected cells and destination.
- `CombatPullEvent.Movement` uses an ordinary event route with `CombatPullEventHandler`. Through `CombatUnitMotionSet.BeginPull`, it borrows the target's existing `CombatUnitMotionPlayback` lane and placement lease. It traverses only the supplied path and completes after interpolation. It has no damage origin/concurrency group, so the ordered scheduler treats it as a barrier before subsequent terrain, hit, and health events. No generic Moved events are synthesized. Pull results with empty Hits complete without direct attack animation, firing or attack sound; ordinary empty-cell attacks retain their previous presentation. Obstacles have no Pull event; their ordinary attack playback is unchanged.
- `CombatHuntersMarkEvent(TargetId, Expected, Final)` updates the target's `CharacterCombatFacts.Debuffs.HuntersMark` in `CombatShownFacts`; null Final removes it. Other debuffs remain. `FieldUnitPresenter` adds one `HuntersMarkDebuff` entry to existing `UnitHUD` statuses. An attacker's trait and a target's debuff have separate keys. The indicator uses the existing mark visual/default icon and supplied percentage. No lifetime is inferred from source death, turns, or cancellation.
- `PullTraitStatusSnapshot.ExtraRangeCells` supplies combat-card text. `OwnedCharacterStatsSnapshot.PullDescription` supplies the same configuration outside combat via `OwnedCharacterStatsCalculator`; the Pull key and descriptor must coexist. Existing deck/card models carry it without InGame DB reads. `UnitCardTraitContent.Create(visuals, keys, thorn, mark = null, pull = null)` and the combat-status overload share formatting. Mark percentages retain their existing immutable descriptor.

## Lifetime and ordering

Pull shares the existing step duration, placement interpolation, result preparation, Tick, cancellation generation, and cleanup. It keeps pre-hit HP/debuffs unchanged while moving. At completion, the displayed anchor changes; subsequent Core events update terrain effects and HP. The existing shown-fact owner tracks only which Pull arrivals are already displayed and projects historical departure/intermediate health snapshots at the current displayed arrival. It preserves event HP, stats, debuffs, death and origins. Actual movement/relocation or leaving the field clears the marker, as do result start, cancellation, refresh and clear; later knockback is not pinned. A truncated lethal-hazard path ends at the supplied last cell. Terrain effects follow published order after Pull; there is no new per-cell contact protocol.

Cancel releases the placement lease and discards the callback without consuming a Core mark. Scene clear/release clears shown facts and returns existing unit/HUD rentals. Mark indicators derive from those facts, with no separate object ownership or cleanup loop. Pointer input, submit behavior, shared AP, and panel registration remain in existing owners.

## Verification and test disposition

Evidence: `.superpowers/sdd/category-and-unit-trait-rework/`. `D-behavior-red` showed eight intended failures out of nine cases; `D-bindings-green` passed those nine. `D-range-red-confirmed` independently failed the missing two-input range contract; `D-range-green` passed all eleven cases, including actual published-result ordering. Test-helper compile errors were corrected before behavioral RED and are not behavioral evidence.

`D-core-integration-pre-review.xml/.log`: Unity exit 0; Core 3,534/3,534, CoreIntegration 98/98, total 3,632, no skipped/failed cases. Thirteen new integration cases cover Pull-before-damage and actual published-result playback, no-Pull movement control, one mark apply/reapply/consume indicator, cancellation/scene exit, configured combat-card text 0/2/7, real owned-stat projection and formatter parity 0/2/7, and basic/extension/destination layer cleanup. HUD/motion tests use real Unity objects; the range test substitutes only the final field renderer.

No prior Core/CoreIntegration case was deleted. Four Framework test doubles only accept extended optional feedback parameters for compilation; their tests were not executed. No test-assembly link was added. Existing real-asset authoring checks inside CoreIntegration ran. `Framework.DataBaseAssetTests` was not executed under the user's limit; Phase D changes no database assets.

## Boundary and change locality

- Rule placement/path integrity: View adds routing, immutable fact replacement, interpolation, and text formatting. There are no unit-ID gameplay branches, A+P/destination/bonus calculations, gameplay mutation, or parallel publication paths. Core changes only expose the existing Pull definition through the owned-stat read model, following the mark/Thorn descriptor pattern.
- SSOT: Core owns one mark slot and configured values. Shown facts remain the existing event-time projection; field layers and interpolation are transient display state. Buffer/transaction ownership is unchanged.
- Tuning the extension or percentage needs no View implementation/registration change. Supplied descriptors reach combat/owned cards. Eligibility edits stay in their rule owners without reading the other trait's presenter. Configured-value tests demonstrate propagation; reduced future edit effort remains an expectation, not measured productivity evidence.
- Motion/event changes require ordering, cancellation, shown-fact and actual publication checks. Configuration still requires authoring/admission/preview/execution checks. Shared terrain/transaction regressions remain in the two full suites. Framework and manual acceptance are separately open.

## Remaining acceptance

Manual scenes were not run: inner/outer enemy attacks, blocked Pull, friendly movement with terrain damage, unchanged obstacles, A=3/P=2, mark apply/consume/non-stacking, edited percentage, and scene-exit cleanup. Automated cases cover logic and selected display components, not final scene composition, readability, frame timing, or appearance.

Final Pull/mark icons, animation and VFX require art acceptance. Existing motion and HUD fallback visuals are used; no final art, new shader/renderer, or camera behavior is claimed. Verify the combat scene in both cell-prefab/grid-outline modes before release.

## Independent review and fix evidence

One fresh-context read-only review found three Important issues, all accepted: historical terrain health snapshots could rewind a completed Pull, movement-only Pull could play an attack, and ranged consumers still aimed at the original cell. No Critical/Minor findings. The author applied one fix pass; no second review is claimed. `D-review-all-green.xml/.log` passes20/20 focused cases, including emitted Core intermediate nonlethal/lethal thorn events, departure normalization, subsequent movement, actor-backed attack/no-attack controls, preview and actual linear/arc source-shot endpoints. The earlier mortar attempt failed a missing test-fixture weapon binding, not the endpoint assertion; its corrected additional control passes. Projectile checks inspect the actual flight endpoint, not only a helper return.

The existing unrelated DeckEdit ThornField description omission is outside this phase. Prior A–C retirement review remains at its checkpoints; multi-cell Pull is explicitly outside the design. Neither limitation excuses the three repaired integration bugs.

Final two-assembly evidence: `D-final-verified.xml/.log`, exit0, Core3,534/3,534 + CoreIntegration105/105 =3,639/3,639, no skips/failures. All3,619 prior case identities remain and20 D cases are added. No manual scene/art or Framework execution claim.

Latest gate: `D-final-acceptance-verified.xml/.log`, exit0, same3,639/3,639 count, including normalization followed by forced movement inside the same result. The boundary change is also recorded in the existing View/Core handoff section5.51.
