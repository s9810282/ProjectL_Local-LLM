# Task 2 Runtime State Ownership Inventory

Date: 2026-09-30  
Plan: `docs/superpowers/plans/2026-09-29-character-buffer-effect-migration-plan.md`

## Decision

The current branch has no existing Buffer that owns battle wide runtime state. Task 2 therefore adds a narrow `CombatStateBuffer` under the existing Buffer group. It owns only concrete battle lifetime data already present in the legacy route. It does not contain character health, character resources, View input, database definitions, or a reconstructed `StageSession`.

`origin/feature/Yonggeun/Stage` was inspected without merging it. Its `CombatViewBuffer` is a View-owned command and preview inbox: View writes it and Core reads it. `CombatStateBuffer` therefore does not absorb that queue and battle cleanup does not clear it. The View handoff's R1–R4 values (latest result sequence, handled command number, answered preview number, and flow phase) are combat state and have a typed home in `CombatStateBuffer`; Task 2 initializes and exposes them while later processing tasks publish changes.

`origin/feature/Yonggeun/Stage` was inspected without merging it. Its `CombatViewBuffer` is a View-owned command and preview inbox: View writes it and Core reads it. `CombatStateBuffer` therefore does not absorb that queue and battle cleanup does not clear it. The View handoff's R1–R4 values (latest result sequence, handled command number, answered preview number, and flow phase) are combat state and have a typed home in `CombatStateBuffer`; Task 2 initializes and exposes them while later processing tasks publish changes.

`CharacterBuffer` remains the only owner of the owned roster, stable owned IDs, soul counts, persistent main values, and active character runtime state. Battle cleanup is an operation of the Buffer group so callers do not enumerate individual `Clear` calls.

The current registration structure keeps Buffer service keys in `DataBaseGroupServiceKey.cs`. Task 2 follows that established structure instead of adding Buffer keys to `SystemGroupsService.cs`.

## Field Inventory

| Legacy mutable state | Canonical migrated owner | Read surface | Write surface | Derived data | Lifetime and reset | Direct regression |
|---|---|---|---|---|---|---|
| Owned main/sub cards and next owned ID | `CharacterBuffer` roster records | `Owned`, `TryGetOwned` | `Acquire` | Ascending owned list | Run lifetime; whole roster reset only | Distinct IDs; rejected batch and overflow write nothing; clear does not reuse IDs |
| Soul count per owned card | `CharacterBuffer` soul map | `GetSoulCount` | validated `Apply` | Soul attack/max health bonuses are calculated values | Survives battle and chapter entry; removed only with whole roster reset | Battle cleanup retains soul counts |
| Main current HP and persistent base max HP/attack/action/movement | `CharacterBuffer` main persistent record | `TryGetMainState`; composed active read | validated `Apply` | Active main HP is composed from this record | Survives battle and chapter entry; removed only with whole roster reset | Active and noncombat reads observe one HP value |
| Sub/enemy HP, action, effective stats, Trait/Category state and temporary character effects | `CharacterBuffer` private battle records | `Characters`, `TryGet(EntityId)` | battle preparation and validated `Apply` | Main HP is excluded from the battle record | Battle lifetime; removed by `EndBattle` | Cleanup removes enemies and temporary state while retaining roster |
| Character area, anchor, rotation, reserve slot and occupied cells | `CharacterBuffer` private battle records | immutable `CharacterData` | battle preparation and validated `Apply` | Entity and owned reverse lookup indexes | Battle lifetime | Immutable reads cannot mutate stored cells or state |
| Movement checkpoint anchor, committed distance and pending action cost | `CharacterBuffer` private battle records | immutable `CharacterData` | battle preparation and validated `Apply` | None | Battle lifetime | Replacement keeps the supplied checkpoint as one validated character state |
| Entity ID to owned ID link | `CharacterBuffer` active record | `TryGetOwnedInstance` | installed with battle preparation | Reverse lookup from active records | Battle lifetime | Cleanup removes active links but retains owned records |
| Board size and reserve capacity | `CombatStateBuffer` battle configuration | `Current` | `PrepareBattle` | Occupancy is not an independent writable source | Battle lifetime | Prepare exposes exact values; cleanup removes them |
| Entity allocator next value | `CombatStateBuffer` | `Current.NextEntityId` | preparation; future prepared transition | Separate from owned ID allocation | Battle lifetime | Task 4 validates the character/structure ID range before either store installs |
| Turn, phase, night time and outcome | `CombatStateBuffer` | future typed read records | future prepared transition | None | Battle lifetime | Deferred until the corresponding command slice migrates |
| View coordination: latest result, handled command, answered preview and flow phase | `CombatStateBuffer` | typed read properties | future command/result publication | Monotonic sequence values | Sequence counters survive battle cleanup; current phase resets with the battle | Task 2 exposes initialized values; Task 7 publishes results |
| View coordination: latest result, handled command, answered preview and flow phase | `CombatStateBuffer` | typed read properties | future command/result publication | Monotonic sequence values | Sequence counters survive battle cleanup; current phase resets with the battle | Task 2 exposes initialized values; Task 7 publishes results |
| Timeline participant delays and seed tie breaks | `CombatStateBuffer` | future typed timeline read | future prepared transition | Ordering derives from participant values and seed | Battle lifetime | Seed is stored by Task 2; mutable participants migrate with timeline commands |
| Draw order and cursor | `CombatStateBuffer` | immutable owned ID order and cursor | preparation; future draw transition | Remaining cards derive from order and cursor | Battle lifetime | Preparation copies input order; cleanup removes it |
| Structure placement/health and rail/gimmick definitions | `CombatStateBuffer` typed records | `Current.Structures`, `RailCells`, `StageGimmicks` | preparation; future prepared transition | Occupancy derives from structure and character locations | Battle lifetime | Task 4 stores initial structures and static inputs; mutable gimmick counters remain for command migration |
| Enemy origin and input indices | `CombatStateBuffer` typed records | future setup/AI reads | future prepared transition | Lookup indexes from stored enemy setup records | Battle lifetime | Deferred until enemy bootstrap migration |
| Soul field IDs/cells/lifetimes and thorn cells/counters | `CombatStateBuffer` typed records | future field effect reads | future prepared transition | Cell indexes derive from field entries | Battle lifetime | Deferred until movement/effect command migration |
| Artifact completion/effect receipts | `CombatStateBuffer` typed result record | future result read service | future prepared transition | None | Retained through result publication, cleared at the next battle boundary | Deferred until the first migrated effect result |
| Mutable RNG state | No mutable RNG owner found in `StageSession`; graph generation already uses explicit seed data | command input when needed | calculated result only | Deterministic result derives from explicit seed/input | Command or battle scope according to the migrated rule | Add only when a migrated rule demonstrates advancing RNG state |
| Currency and current/map/stage progress | Existing Currency, Current, Map and Stage Buffers | existing sinks | existing sinks and group reset | Existing indexes | Existing run/chapter rules | Battle entry and cleanup preserve these Buffers |

## Task 2 Publication Boundary

`IBattleBufferWriteSink.PrepareBattle` validates the complete character preparation and combat configuration before installing either store. `EndBattle` clears only battle scoped data from `CharacterBuffer` and `CombatStateBuffer`. `ResetRunProgress` remains the only whole roster reset route and also ends any active battle before clearing run scoped data.

Task 2 does not migrate gameplay calculations. Later command tasks extend the same typed stores when each command's actual fields and atomic publication set are known.

## Task 4 Preparation Boundary

`BattlePreparationCalculator` consumes immutable stage definitions, the current roster/main/soul facts and Task 3's draw order. It checks roles, ownership eligibility, deployment zones, rotated footprints, capacity and the single main requirement. `BattleCharacterCalculator` creates initial health/stat/Trait/Category values using existing Domain initialization rules. These calculators construct no UnitState, StageSession or StageServices and retain no state between calls.

`CombatProcessingService` is registered by the existing Core factory/group/key. Factory injection supplies the sibling roster service; InitData supplies the DB context and PostBindService supplies Buffer access. The existing Combat Start Plan/Job calls `CoreGroup.Combat.PrepareBattle()` through the existing Runner. A completed Job does not execute preparation twice; a new preparation request during an active battle is rejected.

`IBattleInitializationSource` is an internal, provisional definition source injected by the Core factory. Task 4 integration tests supply typed fake stage definitions. The default production source remains unavailable while G7 is unresolved; it throws explicitly and changes no Buffer. Task 8 must supply the agreed DB/current-stage adapter and real stage-entry wiring. This is not a production View invocation contract.

CharacterBuffer owns explicit player-owned links, including after hand/field/excluded transitions. Enemies and structures never obtain links by numeric ID matching. CombatStateBuffer owns the installed draw cursor, separate battle allocator, structures and static board configuration. Buffer-group validation checks cross-store ID/slot/cell/draw consistency before both stores are prepared and installed. Only Buffer cleanup removes battle data and links; Core does not clear View input queues.

Task 4 verification: Unity 6000.0.73f1 EditMode passed 92/92, exit code 0 (`Logs/cbm-task4-verified.xml` and `.log`). The 24 new cases cover preparation, ownership links, invalid preparation atomicity, real Job execution, registered Trait/Category initialization and missing G7 source rejection; 68 relevant existing cases cover Buffer, Job, roster, main placement and draw behaviour. View/PlayMode and full suites were not run.

## Task 3 Noncombat Storage Inventory

The current DB supplies `PlayerLoadoutData` and `ShopStageDetail`, while CurrentProgressBuffer previously stored selection identity only. The migrated purchase path adds `CurrentShopData` there: three frozen `ShopPurchaseSnapshot` offers and a purchased-slot bit mask. Read access is `CurrentProgressReadSink.Shop`; `PrepareShop` installs validated offers through `SetShopOffers`. The internal Current store prepares a replacement mask for the selected node/stage; the Buffer group's `CharacterPurchaseWriteSink` installs that replacement together with acquired character records and the existing CurrencyBuffer amount.

Shop data persists when the same selection is confirmed again and is cleared when the map/node/stage identity changes or current progress is reset. Shop rules and random offer selection stay in Core Application. These values replace ShopStageState as the authority on the new path; the new services never call RunShopService.

RecoveryStageState currently retains the legacy option definitions and selected-option/one-use marker. No migrated recovery command exists in Task 3. Before Task 6 wires recovery, CurrentProgressBuffer must receive typed option and consumption state and the corresponding prepared HP/currency publication. The legacy recovery owner is not reusable on that migrated path.

## Review Corrections and Verification (2026-09-30)

- Validate each explicit final character proposal against the proposed persistent main state before composing the main HP read. Inconsistent proposals reject the entire batch; a persistent-only HP change remains supported.
- Reject default health and stats snapshots at the Buffer storage boundary. Prepare character read projections before installing any records, both for battle preparation and for Apply. Installation publishes already prepared projections without reconstructing them after writes.
- Keep expected-state comparison in the internal `CharacterCombatFactsComparer` helper. Category fields and all five current concrete Trait status types are compared by value. This helper performs no gameplay calculations and introduces no Framework service or public contract.
- Buffer group cleanup ownership, Factory registration, and assembly boundaries remain unchanged.

Verification used Unity 6000.0.73f1 EditMode with approved AppData access:

- CharacterBufferStateTests: 12/12 passed.
- CombatStateBufferTests: 4/4 passed.
- MapProgressBufferTests: 8/8 passed.
- CharacterRosterContractTests: 29/29 passed; Unity exit code 0.
- Evidence: `Logs/cbm-task2-review-fixes.xml` and `Logs/cbm-task1-review-regression.xml`, with matching `.log` files.

Eight new regression cases cover inconsistent and consistent main HP proposals, equivalent and stale status values, and rejected default snapshots followed by a successful retry. Per the requested focused verification scope, no separate RED execution or full Framework suite rerun was performed. Previously recorded unrelated Framework fixture failures are not claimed fixed.
