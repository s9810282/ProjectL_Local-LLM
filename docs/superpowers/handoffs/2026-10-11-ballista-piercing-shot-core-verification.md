# Ballista Piercing Shot: Core and SO Verification

Date: 2026-10-11

Status: Tasks 1-4 Core acceptance and the user-authorized Ballista SO migration are complete. Projectile playback and scene acceptance remain external work.

## Scope and implementation

| Commit / change | Delivered behavior |
| --- | --- |
| `69686602` | Immutable delivery/encounter contracts, Effect role, result/preview correspondence. |
| `7a8c7e6d` | One aligned actor-origin trace, spatial encounter order, wall/structure/range/board termination. |
| `ee2b909f` | Independent selected identity and delivery, shared falloff budget, command/preview/AI integration. |
| Task 4 acceptance commit | Production-shaped pure Core fixture, this record, and completed handoff. |
| SO migration commit 874a7adc | Method 3 Single selection, explicit Ballista cap, loaded-asset verification. |

The latest user request expanded the original Core-only boundary to two SO assets and necessary loaded-asset tests. Runtime View/UI, buffers, Jobs, database adapter, SO schemas, scenes, and prefabs were not changed. The unrelated untracked economy plan was preserved.

## Verified content migration

- `TargetingMethod_3.asset`: EightDirectionLine / **Single** / None / splash percent 0; key 3 and GUID `fed398bfcedb40f6912dd5d8d61ed661` retained. Reference scan found only Ballista and its existing table registration.
- `Unit_Ballista.asset`: reduction 30 retained; `maxTargetsPerRay: 2147483647` explicitly authored. Managed-reference ID `7942825186083471360` and authoring class retained.
- Attack power 10, range 99, attack cost 4, movement cost 3, category, health, ownership eligibility, and all other live stats retained.
- Removed only unused Ballista YAML fields `cannonAreaType`, `cannonAreaRange`, and `pierceRayCount`; no C# consumer was found. No global defaults or zero-cap semantics changed.
- Two loaded tests in `DataBaseAssetTests` exercise actual catalog/query/adapter conversion and trait data, rather than inspecting YAML text.

## Test evidence

Editor: **Unity 6000.0.73f1**, Windows, approved execution with AppData access. No cache deletion or Unity upgrade. Runner and artifacts are retained under `.superpowers/sdd/2026-10-11-ballista-piercing-shot-implementation-plan/` (git-ignored).

| Run directory | Result | Editor exit | Meaning |
| --- | --- | --- | --- |
| `t4-so-red` | 0/2 passed | 2 | Original assets failed actual loading: Direction piercing rejected; omitted cap loaded as 0. |
| `t4-core` | **1871/1871 passed**, 0 skipped | 0 | Entire Core assembly, including all new contract/trace/command/AI cases and production-shaped fixture. |
| `t4-so-green` | **87/87 passed**, 0 skipped | 0 | DataBaseAssetTests, BattleDefinitionAdapterTests, TargetingMethodDataTests. |

Each directory contains `results.xml` and `editor.log`. Both GREEN logs contain zero C# compiler errors. Dependent assemblies compiled without edits to their runtime source. Compilation and these focused Framework tests do not constitute full Framework or scene/playback acceptance.

```powershell
& ./.superpowers/sdd/2026-10-11-ballista-piercing-shot-implementation-plan/run-tests.ps1 -Label t4-so-red -Assembly ProjectL.Tests.Framework -Filter 'ProjectL.Tests.Framework.DataBaseAssetTests.Ballista_'
& ./.superpowers/sdd/2026-10-11-ballista-piercing-shot-implementation-plan/run-tests.ps1 -Label t4-core -Filter ''
& ./.superpowers/sdd/2026-10-11-ballista-piercing-shot-implementation-plan/run-tests.ps1 -Label t4-so-green -Assembly ProjectL.Tests.Framework -Filter 'ProjectL.Tests.Framework.DataBaseAssetTests;ProjectL.Tests.Framework.BattleDefinitionAdapterTests;ProjectL.Tests.Framework.TargetingMethodDataTests'
```

The pure Core `ProductionBallistaProfile_DeliversOneShotAndSpendsFourActionPoints` constructs the intended Ballista profile in memory (no Unity asset APIs). It asserts one rightward route, A/ID9 → B/ID2 → C/ID7, pre-matchup damage 10/7/4, AP cost 4, preview/result parity, and unchanged input. Existing contract fixtures also cover defensive array copies; all eight vectors, cap exhaustion, blocked contacts, structures, mitigation, artifacts, and AI cases ran in the full assembly.

## Contract examples and regressions

With O=(1,3), A9=(3,3), B2=(5,3), C7=(6,3), aim B, common damage 10 and reduction 30, encounters are 9/2/7 with damage 10/7/4 before target modifiers. Public hit arrays retain entity-ID ordering. `PrimaryTargetId` remains B; only a delivered hit on B is Primary and other delivered hits are Effect with applied splash percent 100.

Cap 1 hits only A while preserving B's selected cell and ID. Zero cap/common damage retains a legal aim and charges once, with no encounters/traveled cells and an origin endpoint. A legal empty Cannon aim keeps an empty selected ID; all actual hits are Effect. Removing the trait restores B-only damage. Walls before B reject admission; structures can intercept a legal shot before B. Fully mitigated encounters still consume the budget.

Migrated historical Direction-piercing command cases reside in `AttackTargetAdmissionTests`, `CombatCommandEffectTests`, `DistanceDamageScaleTraitTests`, `EnemyMovementPathTests`, `ObstacleReactionTests`, `PiercingFalloffTraitTests`, `StaticTerrainIntegrationTests`, `TargetingPreviewParityTests`, `WallAttackAdmissionTests`, `WallAttackAiTests`, and `WallPierceTests`. `TargetingSplashDamageTests` retained Task 1's empty-Single adjustment. Shared non-piercing geometry tests and standalone multi-ray collector checks remain intact; no production caller uses the legacy multi-ray collector.

## Review and decisions

A fresh read-only reviewer checked the feature range from `6eab9c1c` through Tasks 1-3 and the Task 4 working changes, including all five plan Review Focus areas. No Critical, Important, or Minor findings were reported. The reviewer independently read both GREEN XML summaries without rerunning tests.

Decisions retained from the execution ledger:

- Use the existing feature checkout after the managed worktree could not be written under the sandbox; no merge or push is performed.
- Use approved Unity execution for AppData access; restore only generated FMOD scan metadata after tests.
- Contracts validate alignment/contiguity; map bounds belong to geometry. Raw geometry traces and public admitted/budgeted delivery records have different empty-route constraints.
- Retain wall-blocked diagnostic contacts independently of actual travel. Ignore-walls travel includes wall cells but does not hit their occupants.
- Migrate the obsolete empty-Single assertion in Task 1; existing hit/preview enum and percent validation needed no duplicate implementation.
- Keep legacy multi-ray helpers only for explicit standalone regression coverage. Blast-only victims remain in settlement projections/events, without fabricated direct encounters.
- Apply the latest SO authorization narrowly; retain GUIDs, keys, managed-reference identity, and defaults. Preserve logs/XML/ledger for reproducible evidence instead of deleting the execution workspace.
- Review exclusions remain explicit: View playback/forwarding/lifecycle/scene acceptance is pending. Future simultaneous PiercingFalloff + ThornField authoring is not covered by this release's acceptance and must be reviewed/tested before use; no current Ballista asset combines them.

If those boundaries change, revisit the corresponding contract or integration tests instead of relying on these scoped acceptance results. No deferred code-review minors were identified.

## Remaining integration

See [View and SO handoff](2026-10-11-ballista-piercing-shot-view-so-handoff.md). SO migration is complete. View owners still need to verify immutable forwarding, selected-but-unhit/empty-aim UI, Effect-role logs, one projectile with ordered impacts, cancellation/rebinding, and actual scene behavior. Exact travel direction comes from `PiercingShot.Step`, not four-way `AttackDirection`. No visual acceptance is claimed.
