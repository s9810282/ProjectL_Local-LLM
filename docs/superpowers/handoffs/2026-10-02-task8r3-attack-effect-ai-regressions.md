# Task 8R3 — Attack, Effect and AI Regression Coverage

## Outcome

Task 8R3 reconciles approved attack, trait, category and AI behavior with current fact-based Core calculations and the existing publication boundary. Four existing Core test files add 31 executed cases. No production calculator, processing service, Buffer, Framework, factory, assembly, public API, DB asset or View/Flow implementation changed.

Execution continued on `feature/Cheon/refactor/core-system`, HEAD `83f485d1`, preserving the incoming uncommitted 8R1/8R2 work. Existing View integration remains processing service calls and Buffer reads. Core neither adds combat execution Plan/Job paths nor takes View intent removal or acknowledgement ownership.

Changed tests:

- `Assets/Tests/EditMode/CombatCommandCalculationTests.cs`
- `Assets/Tests/EditMode/CombatCommandEffectTests.cs`
- `Assets/Tests/EditMode/CombatCommonCommandTests.cs`
- `Assets/Tests/EditMode/TargetingPreviewParityTests.cs`

No legacy test, assertion or helper was deleted. The continuation's physical dependency retirement remains 8R5; full Task8 is not complete.

## Method mapping and behavior

The [606-row method matrix](2026-10-02-core-combat-retirement-test-matrix.csv) preserves its original fields and 8R2 decisions. Four `8r3_*` columns record disposition, assertion review, passed witness methods and remaining scope for 330 rows: 305 originally assigned to 8R3, the 22 static-stat rows explicitly named in the task, and mixed rows M334/M344/M406. All 190 distinct listed witness methods passed in the final XML. Of these rows, 131 retain existing current/pure/shared tests directly; they did not need duplicate tests or a new runtime route.

The mapping distinguishes confirmed gameplay, finalized targeting replacements, withdrawn owner mechanics, shared constructor/geometry assertions and explicit deferrals. A representative current witness does not establish identical equivalence to every private-corruption or historical custom-profile assertion. Mixed constructor, adapter and geometry seams remain recorded for 8R5 separation before any fixture removal.

| Area | Verified behavior |
| --- | --- |
| Common damage and roles | Existing splash/category pipeline tests preserve common original damage, Additional truncation before matchup rounding, defense and reduction. Group commands hit friendly units and own structures at full Designated damage; caster exclusion and board clipping remain covered. |
| Repeated commands and casualties | Accepted attacks pay once each; actual overkill is clamped. Repeated attacks kill a target and release its cell for movement. Two casualties remove only IDs2/3 from canonical participants; nonlethal hits retain all five. Existing Systems tests prevent reprocessing the same numbered command. |
| Structure footprints | Primary overlap wins over additional overlap, a structure is hit once, and every cell remains occupied until death. Both former multicell-target tests use structures and were not misclassified as deferred units. Living structure cells block movement and thorn field creation; structures receive ordinary damage without fixed thorn damage. |
| Confirmed geometry and range | Explicit fact fixtures test square Single range1 with eight neighbors and fan Single range1 with four cardinal neighbors/one-cell push. Ballista retains selected cardinal three-ray groups and half-up falloff. Cannon range4 accepts empty far aim and configurable70/35 splash, yielding14/7 damage from20; adjacent exclusion is retained. These are test-defined profiles, not actual DB Method_Key mappings. |
| Preview and AI | Existing Facts_* policy tests preserve overlap ties, indirect splash, allied-hit avoidance, actual HP-loss scoring and pursuit Y/X ordering. New All-mask and insufficient-AP tests reuse the same range/Preview/calculation with independent literal expected damage. Preview publishes only display results and does not consume HP/AP/traits/fields. |
| Trait lifecycle | Two attacks schedule Focus once; the next turn applies one stack. Movement resets Focus and arms discount; Preview leaves discount armed, accepted attack consumes it, and no moved-turn Focus gain is scheduled. Rider's second attack refunds zero, and next turn resets its generic refund state. Heavy facing uses actual movement direction while Preview preserves the input. |
| Traversal and fields | Pending-distance and traversal-sum overflow propagate without input mutation. Retargeted `(1,3) -> (3,3) -> (3,4)` movement consumes only final diagonal path `(2,4),(3,4)`: damage7+5 yields HP88; the abandoned damage9 cell `(2,3)` remains. Field refresh preserves stored damage and longer lifetime; successful end decrements lifetime while rejection preserves it. |
| Forced movement | Ordinary casualties vacate before pushes; only surviving units move. Occupied/off-board destinations keep position/resources/unvisited fields while applying ordinary damage. Combined ordinary/forced-thorn overflow faults before any input changes. Existing arrival/death tests preserve destination death anchors and ordering. |
| Automatic turn composition | Positive initial attack does not move and executes once. Terminal main death skips turn end. Insufficient AP/zero score causes no illegal action and one turn end. Current AI skips a lethal Preview; if no improving move exists, its successful turn end settles pending traversal death once. |
| Terminal/Last Stand integrity | Existing current effect tests retain captured Last Stand values, friendly main defeat, enemy-HQ-only victory and projected terminal events. Actual Buffer publication tests retain first-write validation, result/counter lifetime, rejected gameplay preservation and joint stage/boss-map progress. Artifact lifetime migration remains 8R4. |

Important reconciliations:

- Old Entity-primary death/stale rejection is replaced by current Cell semantics: a now-empty legal cell remains aimable and pays normal cost. An empty Direction still rejects; empty All remains explicitly gated.
- Hostile-only automatic cones/nearest Entity attack ties are superseded by explicit method selection, friendly eligibility and actual Preview HP-loss scoring. Pursuit still uses nearest hostile unit/ID before HQ.
- Historical custom ray counts, horizontal-row Cannon profiles, old effect-owner coordinators, delayed prepared-plan revisions and injected gateway actor death are not reasons to add compatibility runtime APIs.
- `StructureSpec.IsAttackable` is outgoing attack configuration. Current HQs are living damage candidates even when they do not attack; old custom non-damageable blocker fixtures are not production activation evidence.
- M273 is a padded footprint with one actual occupied cell, not a multicell-unit case. Its geometric assertion remains directly tested and must be preserved/split from the old UnitState seam at 8R5. It does not establish a production mapping for padded off-center unit anchors.

## Review and verification

A separate read-only reviewer inspected the four test changes, actual current calculators/effect adapters/AI and the confirmed design. No remaining Critical/Important or Minor findings were reported. The initial fan fixture incorrectly expected diagonals/eight cells at range1; it was corrected against the design's four-cardinal-cell mask. The new Rider sequence also corrected its inconsistent pending-movement remaining-budget fixture. Neither required production changes. The reviewer additionally checked casualty participant removal and final-path field consumption literals.

The final actual Unity 6000.0.73f1 EditMode run includes every correction:

| Assembly | Passed / total | Failed |
| --- | ---: | ---: |
| ProjectL.Tests.Core | 1365 / 1365 | 0 |
| ProjectL.Tests.Framework | 315 / 320 | 5 |
| ProjectL.Tests.CombatViewBoundary | 13 / 13 | 0 |
| Total | **1693 / 1698** | **5** |

Skipped/inconclusive: zero. Unity's log records exit code **2**. The exact failed names and full failure messages were compared with 8R2 and are unchanged. Every failure reports `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r3-final.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r3-final.log'
```

Execution used the approved AppData-access path. Case identities compared with the 8R2 XML show exactly31 additions and zero removals. Matrix witnesses were checked against the final XML. Pre-execution tracked-file SHA-256 comparison, before documentation updates, showed exactly the four intentional test-file changes, preserving incoming 8R2 edits and all production sources. Importer-managed catalog, FMOD cache and sequence settings were restored to the captured incoming bytes. `git diff --check` passed.

This is existing-behavior coverage migration, not a production fix or feature. No fabricated RED replay, independent managed harness, PlayMode or actual gameplay verification is claimed. The suite is not fully green because the five authored DB failures remain.

## Remaining boundaries and next step

- **8R4:** Artifact/soul settlement and lifetime migration, including remaining resource/editing consumers and event receipts.
- **8R5:** Retire audited hosts/owners and old helpers after consumer closure; split surviving pure rule/geometry and public constructor guards before deleting mixed fixtures. Current passing witnesses do not authorize blanket deletion.
- **8R6:** Final ownership/caller audit and completion handoff.
- **Production gates:** Actual targeting DB schema/query/unit mappings, DB-loaded Artifact acceptance, startup timing, View-owned combat Flow/Job wiring and joint gameplay verification remain open. The existing production missing-DB guard remains intact.
- **Explicit deferrals:** Multicell units, custom movement routes, ThornHeart reflection integration, unconfirmed profiles and empty-All policy remain unchanged. Generic Rider rules are tested without activating a Rider character profile.

The next plan task is 8R4. No commit/push or 8R4 implementation was performed in this task.
