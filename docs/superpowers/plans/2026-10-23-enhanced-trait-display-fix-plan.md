# Enhanced Trait Display Fix Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` or `superpowers:subagent-driven-development` when implementation is explicitly authorized. Work task-by-task, record evidence, and stop at each approved Phase boundary. Do not commit or push; the user does that.

**Goal:** Make purchased DoubleShot/DoorBreaching visible and show each instance's effective ThornField values in both owned deck cards and combat information.

**Architecture:** Reuse the existing shared `UnitCardTraitContent` converter. Add the two missing View records, expose only the missing configured ThornField payload in the existing owned-stats read result, and format that payload alongside the already available combat snapshot. Keep gameplay rules, purchase transactions and presentation lifetimes unchanged.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core Contracts/Application assemblies, uGUI/TMP, NUnit EditMode tests, existing ScriptableObject tables.

**Spec:** [Enhancement shop review](../specs/2026-10-23-enhancement-shop-code-review.md), ES01 and ES02 only. The existing [enhancement View integration plan](2026-10-19-enhancement-view-integration-plan-Hojin.md) covers shop entry, purchase and next-battle integration; this plan adds the previously uncovered post-purchase ability-description work.

**Status:** Phases A, B and C implementation applied on 2026-10-23 with separate explicit user approval. Phase C adds 19 passing cases; the final graphics-enabled selected run passes 101/123 with 22 exact baseline failures. Eight actual-asset render cases fit after local TMP autosizing. Operating Boot -> Lobby -> StageSelect -> Combat was inspected in Play, but the complete purchase -> deck inspection -> next-combat manual acceptance remains open. Earlier A/B red gates remain as recorded below. Stopped at the Phase C review boundary; implementation delivery is not full acceptance.

## Global constraints

- English implementation documents; Korean code comments and Korean player-facing copy.
- No commits, staging or pushes. Preserve concurrent development-tools and other dirty work.
- Scope is the operating Stage1202 enhancement recipes: DoubleShot, DoorBreaching and PoisonThorn. Do not activate additional recipes, characters, TraitVisuals unrelated to these fixes, or new shop stages.
- Keep hidden-Cliff work out of this plan: the user stated it is not planned content. Multi-cell units, Main selection/locking, purchase history, prices and combat balance are also out of scope.
- Core owns effective values. View formats published facts; it must not read InGame enhancement recipes, infer enhancement from CharacterKey, or recompute gameplay rules.
- Do not change UIBase registration, popup lifetime, input routing, pin/hover behavior, or Framework service exports.
- Existing optional Sprite fields may remain null. Missing new artwork is not a reason to omit an ability's name or description.
- New C# and asset files require `.meta` files; preserve existing GUIDs. Use block namespaces, Allman braces, 4 spaces, UTF-8 and LF.
- Unity execution must use the approved path with AppData access. Do not delete caches or upgrade Unity in response to the known sandbox issue. Do not start a second Editor against the same open project.
- Phase B below touches Core contracts/calculation. It is a specifically identified prerequisite for the complete fix, not permission for unrelated Core work. Record that boundary in the existing Core/View handoff during the authorized implementation, preserving concurrent edits.

## Review focus

1. Two Cactus Cannon instances with the same CharacterKey can have different ThornField values; neither display may overwrite the other.
2. An owned card outside combat currently has only TraitKeys. A combat-only fix must not be reported as complete.
3. A visible or pinned card must refresh after enhancement, and a later unenhanced instance/new Run must not retain enhanced text.
4. Real operating assets, not only synthetic query fixtures, must contain the added trait rows exactly once; null icons remain valid.
5. Effective numeric values must come from the current read result; tests use non-production values to detect hardcoded 5/7/30 text.

## 1. Current route and the narrow contract gap

```text
CombatUnitInfo / a runtime UnitCardUI
  -> UnitStatsSnapshot.TraitStatuses
  -> UnitCardTraitContent.Create(statuses)

Owned UnitCardUI outside combat
  -> OwnedCharacterStatsSnapshot.Stats.TraitKeys
  -> UnitCardTraitContent.Create(keys)
```

Today both converter overloads end at a key-only static TraitVisual description. Combat already supplies `ThornFieldTraitStatusSnapshot` with `ThornDamage`, `LifetimeUnitTurns`, `DefenseReductionPercent` and `TickPhase`, but the converter discards those values. Owned stats never publish that payload.

`OwnedCharacterStatsCalculator` already resolves the exact owned instance's enhancement into a `UnitDefinition`. It can project the configured ThornField values at that point without rereading recipes in View, building a battle, allocating EntityIds, or initializing mutable combat trait state.

## 2. File and interface map

| File | Planned responsibility |
|---|---|
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoubleShot.asset` (+ meta, new) | Key160, ability name and fixed current-recipe description |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoorBreaching.asset` (+ meta, new) | Key130, ability name and fixed current-recipe description |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset` | Register those records once |
| `.../CoreCaculator/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` | Add immutable configured ThornField description payload to the existing owned read result |
| `.../CoreCaculator/Application/Roster/OwnedCharacterStatsCalculator.cs` | Fill that payload from the already resolved effective definition |
| `.../UISystem/Screens/Global/UnitCardTraitContent.cs` | Shared, instance-aware ThornField description formatting |
| `.../UISystem/Screens/Global/UnitCardUI.cs` | Pass owned payload to the shared converter; retain existing refresh/lifetime |
| `.../ViewSystem/Combat/Display/Hud/CombatUnitInfo.cs` | Existing full-snapshot consumer; change only if required by the shared converter signature |

Paths abbreviated with `...` above are resolved by the exact existing file links in section 4. No new service, Buffer store, generic formatting engine, or broad trait-definition exposure is planned.

## Phase A — Register the missing operating trait visuals

### Task A1: Operating asset coverage

**Files:** Create the two TraitVisual assets and metas listed above; modify `TraitVisualDataTable.asset`; create `Assets/Tests/EditMode/Framework/EnhancedTraitVisualAssetTests.cs` and its meta.

**Consumes:** Existing `TraitVisualDataSO` / `ITraitVisualTableQuery`, the actual `Enhancement_Default.asset` references, and authored enhancement definitions.

**Produces:** Runtime View lookup of keys 130 and 160. No new C# product interface.

- [x] Add `OperatingRecipes_AddedTraitsHaveRegisteredVisuals`: load the recipes actually referenced by `Enhancement_Default.asset`, inspect their added trait keys, and require a registered, nonempty name/description for each. Do not extend the requirement to inactive recipes or every TraitKey.
- [x] Add exact assertions for `DoubleShot=160` and `DoorBreaching=130`, unique registration, and unchanged existing table entries. Icons may be null.
- [x] Run the fixture and retain the expected failure identifying missing keys 130/160.
- [x] Create/register the two records. Use the following player-facing copy, consistent with the current operating recipes:

| Key | Name | Description |
|---|---|---|
| 160 | 2연발 | 주변 8칸의 적을 처치하면 같은 방향으로 한 번 더 공격합니다. 추가 행동 비용은 없으며, 이 추가 공격으로 2연발이 다시 발동하지 않습니다. |
| 130 | 도어 브리칭 | 주변 8칸의 적을 공격할 때 대상의 물리 방어력을 무시합니다. |

- [ ] Run `EnhancedTraitVisualAssetTests` and `DataBaseAssetTests`; verify table/GUID references and unchanged operating recipe parameters.
  - Executed both fixtures. New tests pass 4/4; the DB gate remains red at 33/54 with the same 21 baseline failures. Table/GUID checks pass and recipe file hashes are unchanged. See section 5.1; this unchecked item records the remaining gate, not an unexecuted test run.
- [x] Report Phase A and stop. It fixes missing names/descriptions only; it does not complete PoisonThorn's per-instance display.

## Phase B — Expose configured ThornField facts for owned cards

### Task B1: Minimal owned-read payload

**Files:** Modify `OwnedCharacterStatsSnapshot.cs`, `OwnedCharacterStatsCalculator.cs`, `Assets/Tests/EditMode/OwnedCharacterStatsCalculatorTests.cs`, `Assets/Tests/EditMode/OwnedEnhancementResolutionTests.cs`; update direct snapshot constructors in `Assets/Tests/EditMode/RunDeckContractTests.cs`, `Assets/Tests/EditMode/Framework/DeckButtonUITests.cs` (two calls), and `Assets/Tests/EditMode/Framework/OwnedStatsUITestServices.cs`. Re-scan constructors at execution time in case concurrent work adds callers. Update any exact public-property shape assertion that names this type.

**Produces:**

```csharp
public ThornFieldTraitStatusSnapshot ThornFieldDescription { get; }

public OwnedCharacterStatsSnapshot(
    CharacterInstanceId ownedId, EntityKey characterKey,
    CharacterOwnershipRole role, int level, int soulCount,
    CharacterBaselineStatsSnapshot stats,
    ThornFieldTraitStatusSnapshot thornFieldDescription)
```

The last argument is explicit and nullable when ThornField is absent. Reuse this existing immutable payload because its four fields are configured values; do not create a combat runtime state or another DTO with identical fields. Document its baseline/configuration meaning in Korean.

Constructor invariant: ThornField is present in `Stats.TraitKeys` if and only if the payload is non-null. Preserve all current identity/stat validation. The payload does not become a second stored gameplay source; it is a read projection from the effective owned definition.

- [x] Add `OwnedThornDescription_UsesEachResolvedInstance`: two same-key owned characters, one base and one PoisonThorn, produce respectively `(5,5,0,Ended)` and `(7,5,30,Started)` for damage/lifetime/reduction/tick phase. Neither source record nor catalog changes.
- [x] Add `OwnedThornDescription_NoTraitHasNoPayload`, constructor mismatch rejection, and a non-production configured-value case such as damage17/lifetime9/reduction40.
- [x] Run the focused tests and capture the expected missing-contract failures.
- [x] Add the property/constructor argument. In `OwnedCharacterStatsCalculator`, map an existing effective `ThornFieldTraitDefinition` directly to the immutable snapshot. Do not create `TraitRuntimeState`, `UnitTraitStateSet`, or a synthetic battle merely to obtain display values.
- [x] Update constructor callers with explicit null for no-Thorn records and the appropriate payload for Thorn records. Do not add a fallback that silently drops required parameters.
- [ ] Verify `OwnedCharacterStatsCalculatorTests`, `OwnedEnhancementResolutionTests`, `CharacterBaselineStatsTests`, `CharacterStatsQueryServiceTests`, and `CharacterStatsRuntimeCompositionTests`. Confirm roster-revision refresh propagates the new projection without a new cache or source of truth.
  - All five areas ran: 54/55 pass, with one exactly reproduced query-fixture baseline failure. The added real-service Reader test passes and confirms enhancement-only revision refresh. Including constructor consumers gives 72/80, with all eight failures unchanged from baseline. See section 5.2; this unchecked item retains the unresolved green gate.
- [x] Record the narrow Core read-contract addition in the existing Core/View handoff during implementation, then report Phase B and stop. View integration is still pending.

## Phase C — Shared formatting and both UI consumers

### Task C1: Format the published instance values

**Files:** Modify `UnitCardTraitContent.cs` and `UnitCardUI.cs`; create `Assets/Tests/EditMode/Framework/UnitCardTraitContentTests.cs` and meta; extend `CombatUnitInfoTests.cs` and `CharacterDisplayStatsTests.cs`. Retain `CombatUnitInfo`'s full-snapshot route.

**Consumes:** Phase A registered visual records; Phase B `OwnedCharacterStatsSnapshot.ThornFieldDescription`; current combat `ThornFieldTraitStatusSnapshot`.

**Interfaces:** Retain `Create(ITraitVisualTableQuery, ReadOnlySpan<TraitStatusSnapshot>)`. Add an owned-description overload:

```csharp
internal static UnitCardTraitContent Create(
    ITraitVisualTableQuery visuals,
    ReadOnlySpan<TraitKey> traits,
    ThornFieldTraitStatusSnapshot thornFieldDescription);
```

Keep the existing key-only overload for genuinely key-only consumers and empty fallback content. The owned UnitCardUI branch must explicitly use the new payload overload; it must not continue selecting the old overload.

- [x] Add `ThornDescription_BaseAndEnhancedStayIndependent`: format base5 and enhanced7/30 side-by-side, then base again. Require each description to retain its own values, and no defense-reduction sentence for zero reduction.
- [x] Add `ThornDescription_CombatAndOwnedUseSameFormatter`, `ThornDescription_UsesConfiguredValuesInsteadOfConstants`, and `OtherTraits_KeepVisualOrderAndDescriptions`. Preserve first-visible-trait icon selection, static text for unrelated traits, and existing newline behavior.
- [x] Add integration tests for a pinned owned card refreshing after an enhancement-only roster revision, switching between same-key instances, combat information consuming effective payload, and return to an unenhanced/new-Run instance without stale text.
- [x] Run those tests and retain failures showing static damage5/key-only owned output.
- [x] Route both overloads to one small ThornField formatting helper in `UnitCardTraitContent`. Use View-owned wording and the published numbers only. Keep the existing TraitVisual name and icon; no new PoisonThorn TraitKey or global base-stat mutation.
- [x] Use the following wording pattern for ThornField, substituting the current payload. Append the last sentence only when reduction is positive:

```text
공격한 칸 주변에 {LifetimeUnitTurns}턴 동안 유지되는 가시를 생성합니다.
가시에 닿은 캐릭터는 {ThornDamage}의 피해를 받으며, 해당 칸의 가시는 사라집니다.
접촉 대상의 방어력을 {DefenseReductionPercent}% 감소시킵니다.
```

This is presentation formatting, not a new damage/duration formula. Preserve TickPhase in the Core read payload; do not infer expiry state or remaining turns from it. Detailed status-expiry UI is outside this fix.

- [x] Change the owned branch in `UnitCardUI.SetPanelContent` to pass `_displayedStats.ThornFieldDescription`. Preserve the runtime-character route and all update/pin/input behavior.
- [x] Do not globally change the missing-TraitVisual runtime policy for unrelated traits in this task. Phase A's production asset coverage test prevents the scoped omission.
- [x] Run the focused formatting and integration suites plus `UnitCardViewTests`, `DeckCardHoverTests`, `HandCardHoverTests`, `EnhancementShopPopupModelTests`, and `EnhancementStatusPublicationTests` where their actual dependencies are affected. Run the project's required DB asset validation after asset changes. Report exact failures rather than assuming prior Framework failures are unchanged.
  - Executed and compared exact full failure names/messages; 101/123 pass, with 22 pre-existing failures. This checked execution/reporting item does not close the zero-failure gate.

### Task C2: Acceptance and review checkpoint

- [ ] In Unity, inspect a purchased DoubleShot and DoorBreaching in an owned deck card and the next combat information panel; names and descriptions must be visible while original Shotgun ability text remains.
- [ ] Inspect base and PoisonThorn Cactus Cannon instances: deck and combat display agree on 5 versus 7/30; no global replacement of the base description.
- [ ] Check long Korean text for clipping/overflow, click-pinned content, hover behavior, close/reopen, and new-Run cleanup. Use existing UI components; adjust layout only if the new text demonstrably requires it and keep the change local.
  - Eight actual-asset render cases and EditMode pin/reopen/identity/reset tests pass. Full manual interaction acceptance remains open; see section 5.3.
- [x] Record actual EditMode results separately from manual Play evidence. Do not claim that the earlier 48-test managed result validates these UI changes.
- [x] Update ES01/ES02 status and the plan execution log with files, test evidence, and any remaining manual checks. Report Phase C and stop; no Main-selection, hidden-Cliff, history-card, or additional recipe work follows automatically.

## 3. Verification commands and execution notes

Use the running Editor's supported test route if it already owns this project. If it is closed, the equivalent PowerShell batch command is:

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'D:\My_ProjectWork\Github_desktop\Project_L' `
  -runTests -testPlatform EditMode `
  -testFilter 'ProjectL.Tests.Framework.EnhancedTraitVisualAssetTests;ProjectL.Tests.Framework.UnitCardTraitContentTests;ProjectL.Tests.Framework.CombatUnitInfoTests;ProjectL.Tests.Framework.CharacterDisplayStatsTests' `
  -testResults 'D:\My_ProjectWork\Github_desktop\Project_L\.utmp\enhanced-trait-display\results.xml' `
  -logFile 'D:\My_ProjectWork\Github_desktop\Project_L\.utmp\enhanced-trait-display\editor.log'
```

Create the log directory first. Use the approved AppData-access execution path. Substitute each Phase's named fixtures for the filter, and require completed XML with the intended nonzero test count and zero failures for the selected gate. Do not infer success merely from process exit or compilation. The command is a planned verification instruction and was not executed while writing this plan.

## 4. Existing work and exact source references

- [Review ES01/ES02](../specs/2026-10-23-enhancement-shop-code-review.md).
- [Existing enhancement View plan](2026-10-19-enhancement-view-integration-plan-Hojin.md), especially operating Stage1202 and purchase/next-battle evidence. Do not repeat completed shop integration as new work.
- [UnitCardTraitContent.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs).
- [UnitCardUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardUI.cs).
- [CombatUnitInfo.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfo.cs).
- [OwnedCharacterStatsSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs).
- [OwnedCharacterStatsCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/OwnedCharacterStatsCalculator.cs).
- [ThornFieldTraitStatusSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/Traits/ThornFieldTraitStatusSnapshot.cs).
- [TraitVisualDataSO.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs), which already permits null icons.

## 5. Execution log

- 2026-10-23: Plan created. Product implementation not started. Existing plan overlap checked; the missing owned-card payload was identified and included as Phase B. Earlier review results are baseline evidence only. Hidden-Cliff work is excluded following the user's product-scope clarification.

### 5.1 Phase A implementation and verification — 2026-10-23

**Authorization and changes.** The user authorized Phase A only. Added `TraitVisual_DoubleShot.asset` (160, GUID `e76ae27ba9394163aa6a171e512e3bad`) and `TraitVisual_DoorBreaching.asset` (130, GUID `37538d104da04d798bfe54388ac92022`), their metas, and exactly two references at the end of the operating TraitVisual table. Both use the copy in Task A1 and null icons. The previous 11 row references retain their identity and order. Added `EnhancedTraitVisualAssetTests.cs` and its meta. No product C# changed.

**Test boundary.** The new fixture reads the candidates referenced by the actual `Enhancement_Default.asset`, then projects their `AddedTraits` keys through the actual ViewGroup's registered TraitVisual table and existing `ITraitVisualTableQuery`. It checks authored names directly because `TraitVisualData.DisplayName` otherwise falls back to the enum name. Separate checks cover numeric keys, exact copy, asset identity, optional icons, unique registration, and preservation of prior rows. PoisonThorn replaces an existing trait and therefore contributes no added key to this Phase A coverage test. Inactive recipes are not made new coverage requirements.

**Execution.** Used installed Unity 6000.0.73f1 with approved AppData access, `-batchmode -nographics -runTests -testPlatform EditMode`, and filter `ProjectL.Tests.Framework.EnhancedTraitVisualAssetTests;ProjectL.Tests.Framework.DataBaseAssetTests`. Confirmed no running Editor before each launch. No cache deletion or Unity/package upgrade was performed. The initial compile-only attempt rejected `Assert.Multiple` with CS0117; the test was corrected to use supported assertions before collecting the RED result.

| Evidence | New fixture | DataBaseAssetTests | Combined |
|---|---|---|---|
| Before product asset changes | 0 passed / 4 expected failures | 33 passed / 21 failed | 58 executed |
| After registration | 4 passed / 0 failed | 33 passed / 21 failed | 58 executed |

The RED coverage message lists both `DoubleShot: 160` and `DoorBreaching: 130`. All 21 existing DB failure names and full messages match exactly between the two completed XML reports; no new failure was introduced in this selected run. Recipe assets/metas retain their pre-edit SHA-256 hashes. This is targeted regression evidence, not a green DB suite, repository-wide result, or manual UI acceptance. In particular, the existing recipe-parameter test cases stop at outdated asset paths, so their assertions did not validate the current parameters; unchanged hashes establish that this phase did not alter them.

Local evidence (git-ignored): `.utmp/enhanced-trait-display/phase-a-red.xml`, `phase-a-red-tests.log`, `phase-a-after.xml`, `phase-a-after.log`, `red-failures.json`, `after-failures.json`, and `recipe-hashes-before.json`. Both completed runs exited with code 2 because test failures remained.

**Change independence and architecture review.** The defect was a missing content registration at an existing key-based boundary, not a new gameplay or UI lifetime responsibility. The minimum change fills that boundary and makes the operating recipe-to-visual dependency executable in an asset test.

| Scope | Before | After / actual evidence |
|---|---|---|
| Modification | A static trait description belongs to its View asset and table registration. | Same boundary: two new assets/metas and two table references; one test fixture/meta and this execution record. No additional interface, cache, or service. |
| Understanding | Requires the operating recipe's added TraitKey, View data contract, table registration, and shared card converter. | Same contracts suffice. The test follows serialized operating recipe references instead of duplicating purchase or combat rules. Purchase charging, Buffer installation, and Job internals are not dependencies of this edit. |
| Verification | Actual assets must load through the registered query; DB changes require `DataBaseAssetTests`. | New fixture checks the local connection; required DB integration suite was also run before and after. Manual deck/combat rendering, clipping, and pinned refresh remain Phase C acceptance work. |

`ViewDataBaseFactory.Create` still constructs `TraitVisualTable`; `TraitVisualTable.ExportServices` still registers `ITraitVisualTableQuery` in the View builder; `ViewDataBase` exposes it through `IViewQuery`; `UnitCardUI` and `CombatUnitInfo` still use `UnitCardTraitContent`. No new root re-export or alternate consumer path was added. SRP remains split between authored presentation and gameplay facts; OCP is exercised by extending existing data; LSP, ISP, and DIP contracts are unchanged. SSOT is preserved: no new gameplay state or recomputation. Framework, Factory, ServiceKeys, SystemGroup, asmdef, Core, Buffer, and Job structural/code deltas are zero. A broader split or consolidation has no demonstrated benefit for this change and was not implemented.

**Remaining gate and scope.** Phase A content implementation and its four focused tests are complete. The plan's zero-failure DB gate is not met because of the unchanged baseline below; those unrelated data/expectation repairs are not included. Phase B/C, PoisonThorn per-instance descriptions, and manual Play acceptance remain outstanding. No staging, commit, or push was performed.

**Independent review.** A separate read-only reviewer checked the final assets, table, tests, runtime consumer path, and both completed XML reports. No actionable Critical, Important, or Minor findings were reported. The review independently confirmed the exact 21 baseline failure name/message matches and the zero structural delta above. Its scope exclusions match the remaining work just listed; no excluded behavior is claimed as validated by Phase A.

#### Existing DB failures reproduced before and after

All names below belong to `ProjectL.Tests.Framework.DataBaseAssetTests`. Messages are compact summaries of the matching full messages retained in the JSON/XML evidence.

| Test / case | Matching failure |
|---|---|
| `AllAuthoredUnits_ExportFivePercentBaseDefense` | Retired Main key cannot be used as a player definition. |
| `Ballista_LoadedCatalogBuildsSinglePiercingProfile` | Expected `(10, 99, 4, 3)`; actual `(13, 4, 4, 6)`. |
| `EnhancementRecipes_ActualAssetsApplyApprovedChangesAndPreserveBasis` — DoubleShot | Missing old `Unit/Item/Unit_ShotGun.asset` path. |
| Same test — DoorBreaching | Missing old `Unit/Item/Unit_ShotGun.asset` path. |
| Same test — PoisonThorn | Missing old `Unit/Item/Unit_CactusCannon.asset` path. |
| Same test — PrecisionShot | Missing old `Unit/Item/Unit_Ballista.asset` path. |
| Same test — ChainExplosion | Missing old `Unit/Item/Unit_DynamiteHammer.asset` path. |
| `EnhancementRecipes_CatalogRegistersFourValidatedRowsAndKeepsChainPending` | Expected four recipes; actual also contains ChainExplosion. |
| `EnhancementStageRegistration_UsesRuntimePoolAndCommonShopVisuals` | Expected false; actual true. |
| `EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition` | Expected 11; actual 12. |
| `EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab` | Expected length 11; actual 12. |
| `Factory_PreservesExistingViewTables` | Expected true; actual false. |
| `InitialUnit_TargetingMethodMatchesApprovedProfile` — Unit_MainCharacter | UnitTable has no Unit_MainCharacter row. |
| `LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride` | Missing `Script/Render/Shader/Line/LineShader.mat`. |
| `RailwayVisual_RestoredMeshesMaterialsAndAnimationHaveValidReferences` | Expected same as null; actual TrainHead mesh. |
| `Shotgun_HeavyContentAndSharedTargetingReachOperationalCatalog` | Expected `(40, 6, 30, 2, 4, 2)`; actual `(35, 7, 30, 2, 3, 2)`. |
| `StageNodeVisuals_PreserveBuildingsAndAllowMissingIconPrefabs` — Combat/Regular | Expected null; actual NomalStage_Icon. |
| Same test — Combat/Boss | Expected null; actual BossStage_Icon. |
| Same test — Event/Regular | Expected null; actual HealingStage_Icon. |
| Same test — Shop/Regular | Expected null; actual ShopStage_Icon. |
| `TutorialDefinition_OneOneMatchesPlannedFlow` | Expected 1; actual 10000. |

### 5.2 Phase B implementation and verification — 2026-10-23

**Authorization.** The user separately authorized Phase B after receiving the Phase A report, including its unchanged DB failures. Existing Phase A edits were preserved. This phase changes only the approved owned-read contract/projection; Phase C formatting and UI acceptance remain unstarted.

**Implementation.** `OwnedCharacterStatsSnapshot` now exposes get-only `ThornFieldDescription` with the required seventh constructor argument. The existing immutable `ThornFieldTraitStatusSnapshot` carries configured damage, lifetime, defense reduction and tick phase. A Korean XML comment distinguishes configured values from active combat state or remaining duration. The constructor rejects either mismatch between the ThornField key in `Stats.TraitKeys` and payload presence while retaining its prior identity/stat validation. There is no optional argument or legacy overload that could silently discard the payload.

`OwnedCharacterStatsCalculator.CalculateForMains` copies the four fields from the exact owned instance's already resolved `ThornFieldTraitDefinition`. It does not initialize runtime trait state, allocate combat entities, create a synthetic battle, mutate the catalog, or infer enhancement from `CharacterKey`. Four existing no-Thorn constructors were updated with explicit null: one in `RunDeckContractTests`, two in `DeckButtonUITests`, and one in `OwnedStatsUITestServices`. A fresh source scan found no additional direct/target-typed/reflection callers or exact public-property shape assertion requiring migration.

**Added evidence.** Six new cases verify no-trait null, both constructor mismatch directions, non-production `(17,9,40,Started)` values, same-key base/enhanced records with `(5,5,0,Ended)` versus `(7,5,30,Started)`, and refresh after an enhancement-only roster revision. The refresh test binds the real `CharacterStatsQueryService` to real Buffers and the existing shop DB fixture, then uses the existing `OwnedCharacterStatsReader`. It confirms that revision increases once, identity/growth/souls remain unchanged, the new projection is read, the previously returned payload retains its base values, an unchanged second refresh is skipped, and no combat state/entities are created. It does not simulate a purchase or assert UI text/pinning behavior.

**RED and regression execution.** Before product edits, the combined baseline ran 74 cases: 66 passed and eight failed. The first filter used two file-based names that were not actual fixtures; a supplemental run explicitly covered the three methods in the partial `FrameworkRuntimeCompositionTests` class and all four existing `OwnedStatsReaderTests`. The new typed tests then produced expected CS1061/CS1729 failures because the property and seven-argument constructor did not exist. That RED attempt was a compile-time contract check, not an executed NUnit/XML result. After implementation, the corrected combined filter ran all 80 intended cases, including all six new cases.

| Fixture / selected area | Before Phase B | After Phase B |
|---|---|---|
| `OwnedCharacterStatsCalculatorTests` | 19/19 | 23/23 |
| `OwnedEnhancementResolutionTests` | 4/4 | 5/5 |
| `CharacterBaselineStatsTests` | 13/13 | 13/13 |
| `CharacterStatsQueryServiceTests` | 10/11 | 10/11 |
| Character-stats methods in `FrameworkRuntimeCompositionTests` | 3/3 | 3/3 |
| `RunDeckContractTests` | 9/9 | 9/9 |
| `DeckButtonUITests` | 4/11 | 4/11 |
| `OwnedStatsReaderTests` | 4/4 | 5/5 |
| **Total** | **66/74** | **72/80** |

All six new cases pass. The five required calculator/query/composition areas total 54/55 after the change. All eight failures in the broader constructor-consumer run match the pre-change full test names and failure messages exactly; no new failing case appears. The required query gate is therefore still red, and this result is not repository-wide or manual acceptance. No assets changed in Phase B; Phase A's separate DB result remains as recorded in section 5.1.

Evidence is in `.utmp/enhanced-trait-display/phase-b-baseline.xml`, `phase-b-baseline-supplement.xml`, `phase-b-red.log`, `phase-b-after.xml`, their corresponding logs, and `phase-b-baseline-failures.json` / `phase-b-after-failures.json`. The final Unity process exited with code 2 because the eight baseline failures remain. Unity ran through the approved AppData-access path with no second Editor, cache removal, or version/package change.

| Existing failing test | Matching failure summary |
|---|---|
| `CharacterStatsQueryServiceTests.BattleLifecycle_RejectsDuringBattle_AndMatchesCreationBeforeAndAfter` | `ArgumentException`: fixture register lacks `Unit_DynamiteHammer`, parameter `key`. |
| `DeckButtonUITests.CardDragSelectsForExistingFieldRelease_AndBlocksDisabledOrRightDrag` | Expected 1; actual 0. |
| `DeckButtonUITests.GlobalCanvasKeepsFourDesignedCardsAndSiblingControls` | Expected scale 0.6; actual 0.5. |
| `DeckButtonUITests.HideDuringPlacement_RestoresPanel_AndReopenReadsCurrentPlacementState` | Expected 185; actual -30. |
| `DeckButtonUITests.HoverWorksOnPlacementDisabledCard_AndExitOrCloseClearsExactlyOnce` | Expected 1; actual 0. |
| `DeckButtonUITests.MainFirstAndThreeSubCards_ReuseInstances_AndBlockOnlyPlacement` | Expected 1; actual 0. |
| `DeckButtonUITests.NoncombatCard_UsesOwnedBaselineAndId_RefreshesSouls_AndClearsRemovedSelection` | Expected labeled multiline HP/DEF/ATK text, length 68; actual current format length 57. |
| `DeckButtonUITests.StageSelectPolicyCollapsesDeck_OnEveryEntry_AndCombatCanOpenIt` | `InvalidOperationException`: GlobalUIPresentation requires MainCharactor UI registration. |

**Change independence and ownership.** Before this change, the owned result exposed only TraitKeys, so a future View description could not consume configured ThornField numbers. After it, the same ID-bearing read result transports those numbers explicitly. Modification scope is two Core product files, five existing test files, this plan and the [Core/View handoff section 5.50](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#550-owned-thornfield-description-projection-2026-10-23-authorized-phase-b). Constructor fixture edits are required contract migration, not gameplay coupling. Understanding scope is the immutable snapshot invariant and effective-definition mapping; View need not understand purchase charging, mutable trait state, or battle preparation. Verification retains existing baseline/query/composition checks and adds the real revision-to-reader path.

Buffer remains the sole owner of roster records, installation and `RosterRevision`; the calculator remains the owner of derived read facts. Existing `OwnedStatsProjection`, query service, and reader transport/invalidate the complete result without product changes. SRP and SSOT are preserved; the existing payload avoids a duplicate DTO. OCP is served by extending the current read result; no LSP/ISP/DIP substitution, dependency, or service-registration changes are introduced. Framework, Factory, ServiceKeys, SystemGroup, asmdef, Buffer, Job and View product deltas are zero. A separate cache, service, or hierarchy would not reduce this actual change boundary and was not added.

**Independent review and stop.** A separate read-only reviewer checked the final code, callers, contracts, selected evidence and full failure comparison, and found no actionable Critical/Important/Minor issue. The existing Core/View handoff records the explicitly authorized Core exception. Phase B implementation and its six new cases are delivered; the unchanged required query failure remains an open gate. Phase C rendering/pinning integration, manual Play acceptance, and ES02's complete visible fix remain outstanding. No unrelated baseline repair, staging, commit, push, or external message was performed.

### 5.3 Phase C implementation and verification — 2026-10-23

**Authorization and implementation.** The user separately authorized Phase C. Existing A/B changes were retained. `UnitCardTraitContent` keeps the key-only overload and adds the planned owned-payload overload. Both owned and combat routes pass their published ThornField payload to one small formatter. Only ThornField with a non-null payload receives dynamic text; zero defense reduction omits that sentence. Names, icons, trait ordering, static descriptions/newline conversion and the missing-visual policy remain unchanged. `UnitCardUI.SetPanelContent` explicitly supplies `_displayedStats?.ThornFieldDescription`. `CombatUnitInfo` already consumes full trait snapshots and requires no product-code edit. No new cache, service or lifecycle was introduced.

**Test scope.** Added `UnitCardTraitContentTests.cs` and meta: six formatter cases plus eight operating-layout cases. Extended `CombatUnitInfoTests` with three cases using the actual Combat scene, including non-production 17/9/40 values, reset and original Shotgun text alongside DoubleShot/DoorBreaching. Extended `CharacterDisplayStatsTests` with two real Buffer/query/owned-card cases using the nested card from `GlobalUICanvas`: enhancement-only roster revision refresh while pinned, hide/reopen, same-key identity switching and new-Run reset. These tests publish fixture facts through existing contracts; they do not stand in for an actual UI purchase transaction.

**RED and regression evidence.** The pre-C selected baseline executed 104 cases (81 passed, 23 failed). The new overload first produced expected CS1501 contract failures. A temporary signature-only seam then allowed a behavioral RED run: 23 cases, 14 passed and nine failed, consisting of eight new static-description failures plus one baseline failure. After formatting and consumer integration, 115 cases ran with 92 passes and the same 23 failures. All eleven new behavior cases passed.

**Measured local layout correction.** The temporary rendering probe loaded the actual `UnitCard.prefab` and `Combat.unity` ability components and operating TraitVisual rows. Four scenarios in each layout covered base ThornField, enhanced ThornField, Shotgun plus DoubleShot, and Shotgun plus DoorBreaching. Seven of eight initially overflowed at fixed size 36; a persisted eight-case regression reproduced seven failures. Enabled TMP autosizing 20-36 only on ability name and description in each asset: four components, twelve serialized field changes. No geometry, hierarchy, font asset, input or panel lifetime change was retained. Rendered descriptions use sizes 22.7-36; combined Shotgun names use 34.5. All eight after-images were visually inspected and their overflow flags cleared. Tests check actual rendered `textBounds`, full visible character count and overflow; TMP nominal `preferredHeight` alone does not represent the autosized bounds.

The final selected EditMode run used Unity 6000.0.73f1 with AppData access and graphics enabled (omitted `-nographics` to exercise the existing RenderTexture hover test):

| Fixture | Passed / total |
|---|---|
| `UnitCardTraitContentTests` | 14/14 |
| `CombatUnitInfoTests` | 13/13 |
| `CharacterDisplayStatsTests` | 3/4 |
| `UnitCardViewTests` | 9/9 |
| `DeckCardHoverTests` | 3/3 |
| `HandCardHoverTests` | 11/11 |
| `EnhancementShopPopupModelTests` | 9/9 |
| `EnhancementStatusPublicationTests` | 2/2 |
| `EnhancedTraitVisualAssetTests` | 4/4 |
| `DataBaseAssetTests` | 33/54 |
| **Total** | **101/123** |

All 19 new cases pass. The remaining 22 full failure names and complete messages match the pre-C baseline exactly: the 21 DB cases listed in section 5.1 and `CharacterDisplayStatsTests.PublishedEffects_UpdateDisplayStats_AndRemovingEffectsRestoresValues`, which throws `InvalidOperationException: 전투 준비에는 진입 때 포착한 참가 목록이 필요합니다.` The earlier `HandCardHoverTests.Overlap_UsesSameFixedRectForRaycastAndPicking_WhileOnlyVisualMoves` baseline failure (`RenderTexture.Create failed`) disappeared with graphics enabled. The selected gate remains red; this is not repository-wide green or complete manual acceptance. Unity exited with code 2 due to those existing failures.

**Actual Play evidence and limits.** The PC initially had GameBootstrap overrides enabled for Combat/Fixture, loadout1/chapter1/node2, 8x7 field and depth2/2. That attempt failed during battle preparation with `OwnedUnitSpecResolver.BuildProfiles`: `InvalidOperationException: 보유 개체의 기본 공격 프로필이 없습니다.` This phase does not change that path; no unrelated repair was attempted. Temporarily disabling the override allowed the normal Boot title -> Lobby -> StageSelect -> Combat route using actual UI clicks. The Combat scene and hand rendered. Restored the original override and stopped Play/closed the verification Editor afterward. This establishes startup/entry only: an actual enhancement purchase, subsequent owned-card interaction and next-battle enhanced information were not completed manually. C2's purchase, pin/hover/reopen and full Run-reset manual checklist remains open despite the automated coverage.

**Evidence and cleanup.** Local ignored evidence is under `.utmp/enhanced-trait-display/`: `phase-c-baseline.xml`, `phase-c-contract-red.log`, `phase-c-behavior-red.xml`, `phase-c-after.xml`, `phase-c-layout-red.xml`, `phase-c-final-graphics.xml`, corresponding logs and failure JSON comparisons, `visual-before/` and `visual-after/` (eight PNGs and `metrics.json` each), and `phase-c-manual-play.log`. The temporary capture script/meta were removed from `Assets`; the scratch copy remains with the evidence. Unity-generated FMOD cache timestamps and dynamic font atlas/glyph additions were backed up locally and restored to their pre-task tracked content; neither is a delivered product edit. Concurrent Lobby Main design/plan edits were preserved.

**Change independence and ownership.**

| Scope | Before | After / actual evidence |
|---|---|---|
| Modification | The shared converter discarded typed trait facts; owned cards could only call the key-only path. | Two View C# files carry/format the existing payload; two local UI assets adjust four existing text components; three test files (one new) verify the boundary. |
| Understanding | Accurate owned text would otherwise require reconstructing effective enhancement rules outside the published result. | The formatter needs only TraitVisual presentation and the immutable per-instance payload. It does not inspect recipes, purchase charging, Buffer installation, or mutable combat trait internals. |
| Verification | Shared formatting affects owned cards, combat information and actual text bounds. | Required DB, converter/consumer, hover and shop/publication regressions ran; rendered layouts were checked. The full purchase-to-next-battle manual route remains explicitly separate. |

Core remains the source of effective numbers, Buffer owns identity/state/revisions, and View formats those facts. SRP keeps formatting separate from selection/lifetime; existing consumers and data contracts carry dependencies without a new hierarchy. LSP/ISP/DIP and service registration are unchanged. Framework, Factory, ServiceKeys, SystemGroup, asmdef, Core, Buffer and Job deltas **within Phase C** are zero; the separately authorized Phase B Core read-contract edits remain part of the cumulative working tree. A broader abstraction or split has no demonstrated benefit here and was not introduced.

**Independent review and stop.** A fresh read-only reviewer found no actionable Critical/Important/Minor issue and independently checked the 19 new passes, exact 22 baseline matches and before/after overflow evidence. Review and rendering do not close the manual acceptance gates above. ES01/ES02 and Core/View handoff section 5.50 now record delivery and these limits. Phase C implementation is delivered at its review boundary. No unrelated fixes, additional recipes, Main-selection work, staging, commit, push or external message followed.
