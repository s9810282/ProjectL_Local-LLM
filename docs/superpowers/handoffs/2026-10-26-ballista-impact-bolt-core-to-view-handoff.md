# Ballista Impact Bolt — Core to View handoff

Date: 2026-10-26  
Status: Core/InGame implementation and the two allowed test assemblies verified. View integration and manual acceptance pending. This document is prepared locally and has not been sent externally.

## 1. What is ready and what the View owner needs to change

Ballista now uses Impact Bolt (`충격 볼트`) instead of a piercing base trait. Its current production attack selects one cell, deals ordinary center damage and 70% splash damage around it, then pushes eligible surviving units outward from the impact center (any owner since 2026-10-27; see the target/direction rule below). The center occupant stays in place. Heavy category membership no longer grants automatic attack-knockback immunity; explicit forced-movement immunity still applies.

The existing Ballista presentation still requires `Attack.PiercingShot`, which is null for the new attack. The View owner must connect an arcing shot and one impact release to the existing damage/movement facts, preserve Ballista's weapon/art identity, and add the trait's visual/description mapping. Core does not publish a fake piercing path or a new Impact Bolt animation event.

Scope and execution evidence: [implementation plan](../plans/2026-10-26-ballista-impact-bolt-heavy-knockback-implementation-plan.md). Behavior/design: [design](../specs/2026-10-26-ballista-impact-bolt-heavy-knockback-design.md).

Paths below use these repository-relative prefixes:

| Prefix | Directory |
| --- | --- |
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `CS` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem` |
| `V` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat` |
| `A` | `Assets/ProjectL/DataBaseSO/InGameGroup` |
| `I` | `Assets/Tests/EditMode/CoreIntegration` |

## 2. Initial settings and configuration ownership

These values are the installed production defaults, not equality requirements in the runtime rule.

| Setting | Current value | Authoring source |
| --- | --- | --- |
| Unit / category | Ballista key 15 / Cannon | `A/Unit/Item/Playerable/Unit_Ballista.asset` |
| Base trait | `TraitKey.ImpactBolt = 210` | Same unit asset's trait list |
| Knockback distance | 1 grid step | `ImpactBoltTraitAuthoring.distanceCells`, minimum 1 |
| Attack range | 4 | Same unit asset's `attackRange` |
| Method key | 7, existing GUID retained | `A/Targeting/Item/TargetingMethod_BallistaAngle.asset` |
| Shape / choice / angle / splash / percentage | 5 / 1 / 0 / 3 / 70 | RadialSquare / Single / Unrestricted / Radial / 70% |
| Wall delivery / propagation | 2 / 1 | Existing IgnoreWalls / FromImpact policies |
| Preserved unit stats | HP 15, defense 5, attack 13, priority 30 | Existing unit settings |

Valid edits to shape, choice angle, splash pattern/percentage, attack range, and positive displacement distance flow through the existing initialization path. Single choice mode is required because the trait needs one center. Impact Bolt cannot share a profile with ordinary Knockback or PiercingFalloff. None splash with percentage 0 is valid. Existing DB validation requires a positive percentage for non-None splash; Core-only synthetic profiles separately support non-None/0, and their regression does not authorize that SO combination.

Configuration lifetime remains the existing setup lifetime: SO `GetData()` produces immutable data, the adapters build immutable definitions, and an installed runtime retains those values. Editing an SO does not hot-reload an active battle. The next normal initialization receives the edited configuration.

Two dependencies discovered during implementation required additional InGame asset changes:

- DynamiteHammer also referenced method 7. Its reference now points to existing method 9, whose EightDirectionLine/Single/Front/None/0 tuple exactly matches its previous behavior. Its other fields and Knockback distance 1 were preserved. No shared method asset was edited to accommodate Hammer.
- The real Precision Shot recipe replaced PiercingFalloff, which no longer exists on base Ballista. Its obsolete replacement entry was removed; the added PrecisionShot trait, target, key 8, level 1, cost 700, weight 1, and GUID remain. The description now states the first-recipient defense bypass. The generic enhancement resolver was preserved.

## 3. Outward displacement contract

The diagram is in grid coordinates: `x` increases right and `y` increases upward. It does not specify camera orientation. `C` is the resolved impact center.

```text
                 x - 1       x       x + 1
       y + 1       ↖         ↑         ↗
       y           ←         C         →
       y - 1       ↙         ↓         ↘
```

For a target anchor `(x,y)` and center `(cx,cy)`, Core resolves the fixed step `(sign(x-cx), sign(y-cy))` and applies up to `DistanceCells` steps through the existing forced-movement executor. A target initially northeast of `(4,4)`, at `(5,5)`, reaches `(6,6)` at distance 1 or `(7,7)` at distance 2 when the path is clear. Moving or rotating the attacker does not change this direction.

The target/direction rule is `C/Domain/Traits/Rules/ImpactBoltTraitRule.TryResolveStep`. It admits living field units and excludes any footprint containing the center before requesting a direction. It does not broaden the current supported multi-cell-unit admission. Structures and ledger-only side-effect victims do not receive Impact Bolt displacement. This filter does not change shared damage eligibility.

**Changed 2026-10-27 (user decision):** the rule no longer checks owner, and its signature dropped the attacker-owner argument (`TryResolveStep(target, center, out step)`). Any unit the attack actually delivers to (`Delivery.Hits`) is pushed outward: enemies and allies (including the main character). Characters can only be owned by Player or Enemy, so there are no neutral units; neutral structures are still not pushed. Who gets hit is still decided by shared attack eligibility; the Cannon category's targeting rule already admits friendly and neutral targets, so allies hit by the 70% splash are now pushed as well. Knockback records (`AttackResult.Knockbacks` / `AttackPreview.Knockbacks`) include them. View needs no code change: allied knockback uses the same events and records as enemy knockback.

Candidate IDs come from the actual delivered hits, are deduplicated, and execute in entity-ID order. A valid surviving delivered neighbor can move even when its splash percentage or final HP damage is zero. Dead targets do not move. Explicit immunity, occupied cells, board limits, terrain, mines, thorns, and terminal-state stopping retain the existing shared behavior. No chain pushing or new collision damage was added.

The order remains direct damage/reflection/deaths → attack displacement and per-step terrain → existing ThornField application → action settlement → hit-health capture → deferred final-object/arrival handling → existing follow-ups. Reflected attacker death suppresses displacement through the existing executor. A terminal result during movement stops later targets.

Empty delivered hits, including a blocked delivery, cause no displacement and do not require a center. Delivered hits without the required center are an inconsistent plan and fail explicitly. The public wall-blocked command can already reject with `BlockedByWall`; no new occlusion admission behavior was introduced.

## 4. Facts exposed to View

### Trait and card descriptions

| Consumer | Existing/new public facts to read |
| --- | --- |
| Combat trait display | `CharacterCombatFacts.Stats.TraitStatuses` → `ImpactBoltTraitStatusSnapshot.DistanceCells` |
| Projected combat trait display | `AttackPreview.ProjectedTraitStatuses` → the same snapshot type |
| Owned/roster card | `OwnedCharacterStatsSnapshot.ImpactBoltDescription.DistanceCells` |
| Identity | `TraitKey.ImpactBolt` (serialized value 210); display name `충격 볼트` |

The owned property is null when the resolved definition has no Impact Bolt. The snapshot constructor requires trait-key/description presence to agree. Read the configured distance rather than inserting a fallback of 1 or looking up InGame data from View. Per-owned-instance enhancements are resolved before the description is created.

Relevant sources are `C/Contracts/Snapshots/Traits/ImpactBoltTraitStatusSnapshot.cs`, `C/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`, and `C/Application/Roster/OwnedCharacterStatsCalculator.cs`. The authoring → Data → spec path is under `Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/ImpactBoltTraitAuthoring.cs`, `ImpactBoltTraitData.cs`, and `CS/TraitSpecAdapter.cs`.

### Attack, impact, and movement

| Purpose | Contract |
| --- | --- |
| Intended selected impact for the new base profile | `AttackResult.PrimaryCell` / `AttackPreview.PrimaryCell` |
| Supported Pull combination's delivery override | Existing `Pull.FinalAnchor` |
| Shared View target resolution | `V/Presentation/Attack/CombatAttackTarget.DeliveryCell(...)` already resolves `Pull?.FinalAnchor ?? PrimaryCell` for both result and preview |
| Selected and affected geometry | Existing `DesignatedCells`, `AdditionalCells`, `AffectedCells`, and `BlockedContacts`; do not treat the selected 3×3 as guaranteed delivered hits |
| Damage | Existing `AttackResult.Hits`, including `TargetId`, `Role`, `AppliedSplashPercent`, `TargetHealthBefore`, and `TargetHealthAfter` |
| Actual movement | Existing `CombatCharacterEvent` with `Kind == CombatEventKind.Moved`, `Expected` and `Final` character facts for each completed step |
| Final occupancy | Published character facts and their `Anchor` / `OccupiedCells` |
| Piercing | `AttackResult.PiercingShot == null` and `AttackPreview.PiercingShot == null` |

Preserve `CombatAttackTarget.DeliveryCell` semantics when adapting trajectory and playback; a supported Pull combination can retain the originally requested `PrimaryCell` while the actual delivery occurs at `Pull.FinalAnchor`. Do not substitute the first hit unit's location or attacker anchor for the impact point.

`AttackPreview.Targets[].TargetOccupiedCells` deliberately describes original occupancy. Public `AttackPreview` does not contain final displacement anchors or movement events. Phase A Core tests compare the internal preview transition with execution. CoreIntegration verifies public preview damage/configuration/purity, then compares public calculated transitions with real service publication. View should consume committed movement facts for movement playback and must not reconstruct final positions from the trait distance alone.

A movement event's `Expected` facts describe that step after preceding damage/terrain processing. They are not necessarily the complete preattack facts. Follow the existing result/event playback contract; do not replay damage twice or apply the final Buffer state a second time.

## 5. Current presentation incompatibility and owner work

Current production View setting `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/Setting/CombatAttack_Ballista.asset` remains `kind: 4` (Ballista). It and all other View assets/code were left untouched.

Two current checks require piercing data:

- `V/Presentation/Attack/CombatAttackPlayback.cs`, `TryBeginBallista` (around line 458): a missing `PiercingShot` or invalid attack group throws before starting `CombatBallistaAnimation`.
- The same file, `FirePiercingShot` (around line 397): a null `source.Attack.PiercingShot` throws for a source-driven Ballista projectile.

Therefore the new production Core result is not compatible with the old Ballista presentation. An enum-only setting change has not been verified as a complete conversion.

The View owner should implement and verify the following within the existing presentation ownership boundaries:

1. Add trait name/icon/description mapping for key 210, reading configured combat and owned-card distance from the contracts above.
2. Route Ballista's trajectory preview and projectile playback to the same resolved delivery cell, using arcing flight that matches IgnoreWalls delivery while preserving the existing wall propagation facts.
3. Preserve Ballista weapon/art identity, firing/recoil/reload expectations, audio, pool ownership, cancellation, scene detach, and unavailable-owner cleanup as appropriate to the selected presentation implementation.
4. Release damage reactions and recorded forced movement at the impact boundary, using existing attack groups and result/event gates rather than per-piercing-contact encounter gates.
5. Verify empty-center hits, center exclusion, all eight directions, distances 1/2, zero-damage displacement, explicit immunity, Heavy movement, blocked/edge paths, terminal stopping, and repeat/cancel behavior visually.

Reuse candidates already present in the source are `CombatAttackPlayback.TryBeginMortar`, its Mortar `FireFromSource` branch, `CombatMortarAnimation`, `CombatAttackTrajectory`, `CombatShotPath`, `CombatHitGate`, and the shared delivery-cell resolver. Source inspection confirms these paths exist; their suitability for Ballista weapon binding and the completed visual conversion remain unverified owner work.

## 6. Examples verified by the implementation

### SO → production adapters/source → Core services

`I/ImpactBoltAuthoringTests.EditedDistanceAndSplash_ChangeQueryPreviewAndCommittedMovement` uses exact `GetData()` outputs from temporary Unit/Targeting SO clones, configured before runtime creation. The unit identity is adapted to the existing Cowboy fixture, with attack 100; the enemy and headquarters start at HP 1,000 so the example does not terminate before displacement.

| Fact | Verified value |
| --- | --- |
| Actor | `(0,1)` |
| Selected center | `(3,5)` (headquarters) |
| Configuration | Distance 2, Cross splash 40%, Single, range 4 |
| Adjacent enemy | `(2,5)`, HP 1,000 → 960, final anchor `(0,5)` |
| Center headquarters | Stays `(3,5)`, HP 1,000 → 900 |
| Cannon action settlement | Shared budget 10 → 5 |
| Description/status | Both expose authored `DistanceCells == 2` |

Separate tests prove distance 1, 70% splash, alternate shape/angle/range, None/0, invalid non-None/0, installed-definition immutability, and per-instance enhancement descriptions. `ProductionBallista_UsesImpactDefaultsAndPreservesStats` verifies the real production asset independently of this adapted identity.

### Existing Core service publication, synthetic encounter

`I/ImpactBoltPublicationTests.RepeatedPreview_PublishesConfiguredDamageAndMovementExactlyOnce` uses a deliberately synthetic source with the existing Cowboy/Rider actor (attack 5), three enemies, Radial splash 70%, and distance 2. It is publication evidence, not a second SO-propagation claim.

| Published fact | Verified value |
| --- | --- |
| `Attack.PrimaryCell` | `(3,3)` |
| `Attack.PiercingShot` | null |
| Center enemy | `(3,3)` remains; HP 100 → 95 |
| West enemy | `(2,3)` → `(1,3)` → `(0,3)`; HP 100 → 97 |
| East enemy | `(4,3)` → `(5,3)` → `(6,3)`; HP 100 → 97 |
| Movement events | Four `Moved` records with matching step-before/step-after facts; none for center |
| Action settlement | `ActionCostSpent == 3`, `FinalActionCost == 7`; actor becomes Attacked |
| Repeated preview | Three service previews leave gameplay and command/result sequencing unchanged |
| Repeated processing | One result-sequence increment; a second process call is false and changes no stores |
| Stale publication | Changing victim position after calculation rejects all stores; restoring permits one apply, reapply changes no stores |

Heavy variants retain the shared damage matchup and move to the same destinations unless explicitly immune. An immune target still takes its normal damage and has no movement events.

## 7. Verification evidence and excluded work

Fresh Phase C runs used Unity 6000.0.73f1 through approved AppData access, one process at a time, with explicit assembly filters. Evidence is local under `.superpowers/ballista-impact-bolt/`.

| Assembly | Pre-change baseline | Fresh Phase C result | Evidence basename |
| --- | --- | --- | --- |
| `ProjectL.Tests.Core` | 3,534 passed, 0 failed | 3,599 passed, 0 failed; exit 0 | `phase-c-core-gate.xml`, `.log`, `.exit` |
| `ProjectL.Tests.CoreIntegration` | 105 passed, 0 failed | 135 passed, 0 failed; exit 0 | `phase-c-integration-gate.xml`, `.log`, `.exit` |

Core adds 65 cases relative to baseline, including updated Heavy-dependent consumers; CoreIntegration adds 30 cases (22 authoring, 8 publication). No failures remain in the allowed assemblies. Earlier Phase A/B red and intermediate fixture/compilation runs are recorded in the implementation plan and are not final success evidence.

The following files were inspected as owner follow-up references, but were neither changed nor executed:

| Excluded file/group | Source-based follow-up |
| --- | --- |
| `Assets/Tests/EditMode/Framework/DataBaseAssetTests.cs` | `Ballista_LoadedCatalogBuildsSinglePiercingProfile` and `Ballista_LoadedTraitHasExplicitUnlimitedCap` assert the previous base profile. Its Precision Shot branch also expects base/replaced PiercingFalloff. Update ownership is external to this work. |
| `Assets/Tests/EditMode/Framework/TargetingAngleAssetTests.cs` | Ballista cases still expect Front/EightDirectionLine; `BallistaVariant_DoesNotMutateMethodThree` also compares the variant's old shape/splash tuple with method 3. |
| `Assets/Tests/EditMode/Framework/CombatBallistaAssetTests.cs` | Asset/binding and repeated-shot expectations still select Ballista presentation. Review against the chosen new presentation and current production settings. |
| `Assets/Tests/EditMode/Framework/CombatBallistaAttackTests.cs`, `CombatBallistaLifecycleTests.cs` | Current synthetic piercing-contact/reload/cancellation tests are references for preserving lifecycle behavior. The owner must decide which continue as generic piercing presentation coverage and which need new Impact Bolt acceptance coverage. |
| `Assets/Tests/EditMode/Framework/CombatShotPathTests.cs`, `CombatMortarPresentationTests.cs`, `CombatMortarAnimationTests.cs`, `CombatMortarLifecyclePlayTests.cs`, `CombatMortarAssetTests.cs` | Existing path/arc/impact/lifecycle coverage to review when choosing reuse. No passing result is claimed here. |

The stale assertions above are source observations, not fresh failure counts for an excluded assembly. View, Framework, CombatViewBoundary, PlayMode/manual acceptance, icon art, production trajectory, and animation have not been verified by this work.

Static review found paired unique `.meta` files for all 14 new C# files across Phases A/B, no UnityEngine dependency in the new pure Data/Core files, unchanged existing asset GUIDs, and a clean `git diff --check`. View code/assets, Framework tests, FieldCamera, Architecture, and retired assets were untouched. The only additional unit asset edit is the recorded Hammer reference correction; no blanket claim that every other unit file is unchanged is made.

## 8. Change boundaries and ownership assessment

| Future change | Previous → current edit/understanding boundary | Necessary verification |
| --- | --- | --- |
| Tune distance, shape, splash, percentage, or range | Attacker-origin-only displacement and the old Ballista piercing profile → existing Unit/Targeting SO values plus the positive Impact Bolt trait distance. Understand the authoring/Data contract; no executor, registry, damage loop, or formatter balance literal is needed. | SO-to-runtime query/preview/command tests, combat/owned description agreement, and a fresh initialized runtime. Installed runtimes retain their original values. |
| Change Impact Bolt target eligibility or direction | A shared executor previously selected attacker-origin steps → `ImpactBoltTraitRule` owns Impact Bolt's field-unit/center exclusion and direction (owner check removed 2026-10-27). Shared movement mechanics remain in their existing owners. | Feature behavior plus ordinary knockback/terrain regression, because mode dispatch and movement execution are shared. |

Shared-file edits fall into required registration/mapping (registry, factories, codec/provider, command capture, authoring adapter, owned description) and required shared behavior (contribution mode validation, executor mode dispatch, reversible Heavy policy). They do not duplicate state or Framework/Buffer/Job responsibilities. No runtime Ballista key or method key selects the feature, and no live DB polling, fallback distance, new service layer, or second movement/damage engine was introduced.

Independent Phase A and Phase B reviews reported no Critical/Important/Minor findings. A separate read-only Phase C handoff review checked the public contracts, source exceptions, fixture examples, excluded test expectations, and ownership statements and also found no Critical/Important/Minor issue. The remaining integration work is the explicit View ownership described above; no visual or whole-project completion is claimed.
