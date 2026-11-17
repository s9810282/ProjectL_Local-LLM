# Player Active Skills — Core to View Integration Handoff

Updated: 2026-11-15. Core implementation and Phase G delivery are complete with the baseline failures explicitly retained in section 10. No View UI, scene, animation, VFX or upgrade acquisition economy is delivered here.

Authoritative rules: [Core design](../specs/2026-11-15-player-active-skills-core-design.md). Execution evidence: [implementation plan, section 6](../plans/2026-11-15-player-active-skills-core-implementation-plan.md). These are the implemented contracts, not the earlier proposed API names.

## 1. Ownership and access

Use the existing injected resolver once during binding:

```csharp
var buffers = resolver.GetService(BufferSystemKey.Buffer);
var core = resolver.GetService(InGameSystemGroupServiceKey.Core);
var input = buffers.CombatViewWriteSink;
var state = buffers.CombatStateReadSink;
var latestResult = buffers.CombatResultReadSink;
var history = buffers.CombatResultHistoryReadSink;
var roster = core.Characters;
```

Types/namespaces:

- `IBufferQuery`: `ProjectL.Systems.RuntimeSystem.BufferSystems.Core`.
- `BufferSystemKey`: `ProjectL.Systems.ServiceKeys.Runtime`.
- `IInGameCoreSystemGroupService`: `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem`.
- `InGameSystemGroupServiceKey`: `ProjectL.Systems.ServiceKeys.Runtime.InGame`.
- Input DTOs: `ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView`.
- Skill DTOs: `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills`.
- Events/results: the existing `CoreCaculator.Contracts.Combat`, `.Results`, `.Common` and `.Ids` namespaces.

`CombatSystemFactory` constructs the shared assembler, battle lifetime and publisher, then registers the existing command and preview services in the Core group. The existing Combat Plan/Jobs call `core.CombatCommands.ProcessNextCommand()` and `core.CombatPreview.ProcessLatestPreview()`. View writes requests and consumes responses; it must not add another processing loop or register another skill service. The G integration harness calls the same services synchronously for testing; that is not a new View lifecycle.

Live ownership remains: CharacterBuffer owns each Main instance's upgrade; CombatStateBuffer owns the prepared skill binding, remaining uses and last-used controller-turn sequence; existing combat result/preview Buffers own publication. View owns only local selection, hover and presentation. Do not read InGame DB tables from View, write Core state, or submit a calculated forecast transition.

## 2. Exact request and response surface

```csharp
CombatViewPreviewData.ForPlayerSkillAvailability(EntityId actorId)
CombatViewPreviewData.ForPlayerSkillSelection(
    EntityId actorId, IReadOnlyList<CellCoord> selectedPoints)
CombatViewPreviewData.ForPlayerSkillForecast(
    EntityId actorId, IReadOnlyList<CellCoord> completePoints)
CombatViewCommandData.ForPlayerSkill(
    EntityId actorId, IReadOnlyList<CellCoord> completePoints,
    PlayerSkillSourceStamp expectedSource)

int ICombatViewBufferWriteSink.SetPreview(CombatViewPreviewData preview)
int ICombatViewBufferWriteSink.Submit(CombatViewCommandData command)
bool ICombatViewBufferWriteSink.ClearPreview(int number)
void ICombatViewBufferWriteSink.AcknowledgePresented(int resultSequence)
```

The new input command kind is `CombatViewCommandKind.UsePlayerSkill = 16`, matching `CombatCommandKind.UsePlayerSkill = 16`. Preview kinds are `SkillAvailability = 11`, `SkillSelection = 12`, `SkillForecast = 13`. Factories validate actor/argument shape and copy point lists into immutable `PlayerSkillRequest` values. Request numbering is issued only by the existing Buffer; previews and commands share its counter.

Poll `state.TryGetLatestPreview(out CombatPreviewResult response)`. The response has `RequestNumber` and exactly one arm:

| Arm | Fields to consume |
|---|---|
| `SkillAvailability` | `Source`, `SkillKey`, `UpgradeKey`, `MainInstanceId`, `RemainingUses`, `StageUseLimit`, `UsedThisTurn`, `CanUse`, `FailureReason` |
| `SkillSelection` | `Source`, immutable `SelectedPoints`, `RequiredPointCount`, `PrefixIsValid`, `IsComplete`, `NextCandidateCells`, `FailureReason` |
| `SkillForecast` | `Source`, immutable `SelectedPoints`, `CanUse`, `FailureReason`, `Coverage`, `Prediction` (`PlayerSkillResult`, null when rejected) |

`PlayerSkillSourceStamp` contains `long BattleInstanceId`, `long ControllerTurnSequence`, `long QueryRevision`, `EntityId ActorId`. It bundles existing identity/version facts; it creates no new version authority.

`state.HasBattle`, `BattleInstanceId`, `QueryRevision`, `Current.CommandState.Turn.ControllerTurnSequence`, `HandledCommandNumber`, `AnsweredPreviewNumber` and `LatestResultSequence` are the existing read contracts. The prepared skill can be read at `state.Current.CommandState.PlayerSkill`; availability is the authoritative current admission answer. A dead/off-field Main, unavailable turn, exhausted uses or prohibiting status can invalidate a previously enabled button.

## 3. Selection and casting for all six forms

Always start with availability for the prepared selected Main actor. Do not infer ownership from a Sub, current category or character artwork. An ordinary attack already used does not prohibit a skill; casting neither consumes nor restores ordinary movement/attack permission. Bind permits casting; Disarm and Stun prohibit it.

| Skill / branch | Selection prefix sequence | Complete forecast input | Primary behavior |
|---|---|---|---|
| Powder Blossom / None | `[]` is complete | `[]` | 8-cell mask, center excluded; 50% attack; direct enemy kill may schedule another burst |
| Powder Blossom / Bloom | `[]` is complete | `[]` | Starts at 8, then 20, then 36 cells; expansion caps, reactivation count does not; next cast restarts at 8 |
| Powder Blossom / Condemnation | `[]` is complete | `[]` | 20 cells immediately, 50% attack, one-turn Stun, no Powder Blossom reactivation |
| Magic Bullet / None | `[] → [A] → [A,B]` | `[A,B]` | 100% attack, bend at A, continue through/beyond B |
| Magic Bullet / Distortion | `[] → [A] → [A,B] → [A,B,C]` | `[A,B,C]` | 100% attack, bends at A and B, continues through/beyond C |
| Magic Bullet / Judgment | `[] → [A] → [A,B]` | `[A,B]` | First eligible target only; 150%, physical-defense-only bypass, one incoming-direction distance-two displacement request |

For Magic Bullet, A is cardinal from the caster. B is perpendicular through A; C is perpendicular through B. Each segment's selected-point distance uses the current caster's effective attack range. Zero-length segments, wrong direction/range, walls and board overflow reject. Empty and occupied cells are valid points. Query candidates instead of reconstructing this geometry in View.

Powder masks are clipped to the board, pass through walls and exclude allies; their row widths are 3/3/3, 3/5/5/5/3 and 3/5/7/7/7/5/3, each excluding the center. Expanded masks are not full squares. Multi-cell targets are deduplicated. Eligible KillChainAttack resolves before a scheduled next burst; its kills do not schedule another Powder burst. Common damage/contact/kill/defense/reflection conditions remain in force; ordinary aim/movement transformations and ordinary-action-completion effects do not apply to the skill.

Base/Distortion Magic Bullet pierces eligible allies and attackable structures, including surviving structures. Encounter count, falloff and target budget continue across bends and deduplicate entities globally. Judgment stops after its first target even if that target dies or moves. Its direction/distance overrides competing displacement traits, while immunity, occupancy, terrain, bounds and interruption remain active. Requested distance is not guaranteed travel.

Knockback direction for Base/Distortion (2026-11-17, [design](../specs/2026-11-17-magic-bullet-knockback-direction-design.md)): when the caster owns a knockback trait, each pushed target (character, obstacle or death-trait corpse) moves along the incoming direction of the segment that first hit it, with the trait's distance. A sacrifice guardian uses the direction of the original target it covered. The caster-to-target direction is no longer used for these shots. View keeps reading `Knockbacks[].Step` and the path; no View change is required.

## 4. Asynchronous View sequence and stale handling

The following snippets are separate calls from the existing interaction/response lifecycle, not a synchronous loop:

```csharp
int availabilityNumber = input.SetPreview(
    CombatViewPreviewData.ForPlayerSkillAvailability(actorId));

int selectionNumber = input.SetPreview(
    CombatViewPreviewData.ForPlayerSkillSelection(actorId, selectedPoints));

int forecastNumber = input.SetPreview(
    CombatViewPreviewData.ForPlayerSkillForecast(actorId, completePoints));
```

Before displaying a response, check `response.RequestNumber` against the locally owned request number and check the expected response arm. Check the source actor/battle/turn/revision against current Buffer values, and check returned points against the local selection. A response replaced while calculating is discarded by the service; Buffer publication independently checks these same source/selection boundaries. A previously installed response can still become obsolete after another command, so do not retain it as an authorization indefinitely.

```csharp
if (state.TryGetLatestPreview(out var response) &&
    response.RequestNumber == forecastNumber &&
    response.SkillForecast is PlayerSkillForecast forecast && forecast.CanUse)
{
    // 현재 전투·턴·조회 버전·행위자·로컬 선택을 대조한 뒤 사용자 확인 시 제출한다.
    int commandNumber = input.Submit(CombatViewCommandData.ForPlayerSkill(
        actorId, completePoints, forecast.Source));
}

bool cancelled = input.ClearPreview(forecastNumber);
```

Use only the request number owned by that interaction. `ClearPreview(otherNumber)` returns false and leaves another request intact. Cancellation of local selection/preview consumes no use. Clearing a preview does not cancel a command already submitted. The parameterless `ClearPreview()`/`Clear()` belong to existing boundary cleanup, not arbitrary popup dismissal.

Execution independently captures current facts and checks the submitted `ExpectedSource`; it does not consume or install the forecast. A stale source returns `SourceChanged`/`BattleChanged` without gameplay/RNG/use mutation. Refresh availability/selection after such a rejection. Do not replace a stale command's source with a new stamp while retaining a stale calculated result.

## 5. Public forecast limitations

Accepted public forecasts always have `Coverage = KnownInformationConditional`, including worlds containing no secret effects. Rejected forecasts have `Coverage = None` and `Prediction = null`. This label must not be interpreted as a hidden-source-presence flag.

The same complete command engine runs against permitted Player-visible facts, including known child attacks, status, forced movement, terrain, death and pending reactions. Hidden mines/range terrain/tornadoes are not allowed to influence visible HP, paths, outcome events, acceptance or coverage. Private RNG seeds and environment allocation progress are projected canonically for this public calculation. Thus a forecast's random branch is a conditional known-information outcome, not a reservation or promise of the live random outcome. Show this limitation in the eventual UI.

Full-information internal computation retains actual RNG and is compared with execution separately. A forecast may legitimately differ from actual execution when hidden facts or private RNG matter. Merely hiding an event while displaying its secret-derived final HP would violate the contract. Prediction events and use settlement are read-only display data; they have no real result sequence and must never be acknowledged as an executed result. The internal forecast-input marker is preserved through copies and `RequireAuthoritativeCalculation()` blocks its installation.

## 6. Results, events and acknowledgment

`buffers.CombatResultReadSink.TryGetLatest(out CombatResult result)` reads the latest result. `buffers.CombatResultHistoryReadSink.Results` retains ordered history for the current/just-ended battle; the next preparation clears history. Use the existing playback consumer/cursor so intermediate results are not skipped. Do not assume a command number equals a result sequence.

`CombatResult` exposes `BattleInstanceId`, `ControllerTurnSequence`, `Sequence`, `CommandNumber`, `CommandKind`, `ActorId`, `Accepted`, `Reason`, `Events`, `PlayerSkill` and optional `Outcome`. An accepted cast uses `UsePlayerSkill`, has one `PlayerSkillResult` and has no ordinary `ActionUseSettlement`. A rejected cast is still a handled result with a sequence; consume its rejection and complete the normal acknowledgment path. Duplicate handled processing does not issue another result or charge.

`PlayerSkillResult` exposes `SkillKey`, `UpgradeKey`, ordered `Events` and `UseSettlement`. The settlement contains actor, expected/final remaining uses, expected/final last-used controller sequence and the current controller sequence. It decrements exactly once even for an empty cast or early terminal chain. Child attacks/bursts never submit another command or charge another use.

Render the outer `CombatResult.Events` stream once, in causal order. `PlayerSkillResult.Events` references overlapping instances for grouping; do not play it as a second stream. Outer events can also include pending reactions after the primary skill.

| Event/data | View handling |
|---|---|
| `CombatPlayerSkillAttackEvent`, `CombatEventKind.PlayerSkillAttacked = 51` | New primary skill occurrence; dispatch explicitly instead of assuming `CombatAttackEvent`/ordinary `AttackResult` |
| `Attack.ActorId`, `AttackOrdinal`, `AttackGroup` | Actor and zero-based burst/projectile occurrence identity; primary groups are distinct |
| `AffectedCells` | Actual burst mask or delivered projectile cells |
| `ProjectilePath` | Ordered actual traveled cells; may stop before a selected point; never move the caster along it |
| `IncomingDirections`, `ImpactCells` | Dictionaries keyed by contact `DeliveryIndex`; physical entry direction/contact cell, separate from the common damage origin |
| `Hits` / `DamageByRecipient` | Actual recipients after redirection; HP before/after, contact origins, damage breakdown; do not infer recipients from aimed cells or final positions |
| `Knockbacks` | Requested distance, actual path, step and stop reason (`Completed`, `Blocked`, `Interrupted`, `Immune`); use existing forced-movement playback |
| `TraitActivations` / `CategoryActivations` | Grouping/summary data; child attacks remain their existing effect events |
| Existing health, status, death, terrain/object, thorn, tornado, reflection and child-effect events | Preserve their actual event kinds, sources and order. Environment damage remains environment damage; movement agency remains `Forced` |

Hit summaries and health events describe the same causal changes. Follow the existing playback convention rather than subtracting damage twice. A hit summary can precede later terrain/death effects: final gameplay state is the published Buffer, while playback follows the ordered events. Projectile impact data is not a character relocation, and a corpse/death anchor is not a living movement event.

```csharp
if (latestResult.TryGetLatest(out var result) && result.Sequence > presentedSequence)
{
    // 기존 결과 소비자가 번호 순서대로 사건을 처리하고 연출 완료 뒤 확인한다.
    // input.AcknowledgePresented(result.Sequence);
}
```

`AcknowledgePresented(int)` is called once the existing consumer has finished that result; it is monotonic. Keep normal exit/completion waiting and failure handling intact.

## 7. Representative outcomes and failures

| Scenario | Expected contract |
|---|---|
| Valid nonempty cast | One accepted command/result, ordered skill/common events, one use debit, unchanged ordinary action permission |
| Valid empty Powder/Magic cast | Accepted; a primary occurrence with empty hits; one use debit, no `NoTargets` rejection |
| Base/Bloom direct enemy kill | Primary burst → eligible KillChainAttack → next primary burst; one command and one use settlement for the whole chain |
| Bloom burst reaches the last objective | Subsequent work stops at terminal state; the accepted cast is still settled once and completion is published |
| Judgment first hit dies/is immune/blocked | Projectile ends at first eligible contact; displacement may be absent, immune, shortened or interrupted; never resumes the ray |
| Old `ExpectedSource` | Rejected result with `SourceChanged`/`BattleChanged`; handled number advances, gameplay/uses/RNG do not |
| Cancel another preview number | Returns false; leaves the currently owned request intact |

Gameplay failure enum values are: `None`, `NoActiveBattle`, `NotPlayerCombatTurn`, `NotSelectedMain`, `ActorUnavailable`, `NoSkillConfigured`, `Disarmed`, `Stunned`, `StageUsesExhausted`, `UsedThisTurn`, `WrongPointCount`, `PointOutsideBoard`, `PointOnWall`, `PointOutOfRange`, `InvalidSegmentDirection`, `SegmentBlockedByWall`, `SourceChanged`, `BattleChanged`. `CombatResult.Reason` contains the code name, not localized UI text. An incomplete prefix may be a valid selection but cannot forecast/execute. DTO misuse and inconsistent publication throw; do not turn configuration exceptions into a normal disabled-button success.

## 8. Out-of-battle upgrade API

```csharp
PlayerSkillUpgradeAvailability ICharacterRosterService.ReadPlayerSkillUpgradeAvailability()
PlayerSkillUpgradeApplyResult ICharacterRosterService.TryApplyPlayerSkillUpgrade(
    RunId expectedRunId, long expectedRosterRevision,
    CharacterInstanceId mainId, PlayerSkillUpgradeKey selectedBranch)
```

Read through `core.Characters`. Availability supplies `RunId`, `RosterRevision`, `MainId`, nullable `Definition`, `CurrentBranch`, `CanApply`, `FailureReason`. Apply returns `Succeeded`, `FailureReason`, installed/current `RunId`, `MainId`, `SkillKey`, `Branch`, `RosterRevision`. Successful installation advances the existing roster revision once. Refresh from the returned revision/current query after applying; do not predict a new revision in View.

```csharp
var offer = roster.ReadPlayerSkillUpgradeAvailability();
if (offer.CanApply)
{
    var applied = roster.TryApplyPlayerSkillUpgrade(
        offer.RunId, offer.RosterRevision, offer.MainId, selectedBranch);
    // 실패 코드를 표시하고 현재 조회로 선택/버전을 갱신한다.
}
```

Allowed branches are Bloom/Condemnation for Powder Blossom and Distortion/Judgment for Magic Bullet. They are mutually exclusive, one-way from None, owned by that Main instance, preserved across stages/growth/specialization and cleared by a genuine new Run. Switching deck Main never transfers an upgrade. Any active prepared battle, including deployment and a completed battle awaiting cleanup, rejects upgrade changes. Failure values: `None`, `NoActiveRun`, `InBattle`, `InvalidMain`, `NoSkillConfigured`, `InvalidBranch`, `AlreadyUpgraded`, `SourceChanged`. No acquisition price, offer, reward or shop UI is implemented.

## 9. Operational configuration and remaining View work

The active InGame catalog registers `PlayerActiveSkillDataTable.asset` and its two rows under `Assets/ProjectL/DataBaseSO/InGameGroup/PlayerSkill/`. `IHasPlayerActiveSkillTable.PlayerActiveSkills` is exported through the existing InGame query context, used by roster/battle preparation, not read by View. Missing optional fixture mappings mean no skill; malformed/duplicate registered mappings fail with source identity.

| Main definition | Bound skill | Stage allowance | Notes |
|---|---|---:|---|
| `Unit_Akimbo` | PowderBlossom | 1 | Current starting loadout retains Akimbo |
| `Unit_Sniper` | MagicBullet | 1 | MainCharacter/PlayerOnly; authored range 4, cardinal targeting, one 25%/four-target PiercingFalloff entry retained |

Do not copy fixture allowance 2 into production. Category specialization does not change the bound unit identity. Genuine new battle preparation binds the selected Main's authored definition and stored branch and initializes the allowance once. Rebind, repeated active preparation, selection or preview cannot refill it. The existing controller-turn sequence reopens the turn opportunity; it does not restore stage uses.

View still needs: skill controls and availability refresh; local point selection/hover/cancel; conditional forecast wording; event-51 playback; branch labels/icons and any future acquisition screen; actual input gating and result acknowledgment integration. In Unity verify all six forms, affected-cell overlays, selected-versus-traveled path, ally/structure hits, interrupted displacement, statuses and child events, empty/terminal/rejected flows, stale response after movement/turn/battle changes, cancellation ownership, scene rebind, pause/resume and ordinary action preservation. Confirm production Sniper selection through the real UI when that UI exists. Core EditMode evidence does not establish these visual/PlayMode behaviors.

## 10. Verification record

G intake matched all 9,973 Phase F source entries after final document hashes were substituted. New `PlayerSkillEndToEndTests` uses test-owned definitions and real runtime Factory → roster upgrade → battle entry/preparation → services → Buffer publication → acknowledgment. Its six-form behavior fixture deliberately binds either skill to its test Main; canonical Akimbo/Sniper eligibility is independently covered by F's two production-catalog cases, also selected by G's full assembly run.

G focused run `g-focused-corrected-20261115-084632` passed 21/21: ten new end-to-end cases and eleven existing public-disclosure cases. The first attempt stopped at a missing test namespace before NUnit and was corrected. Full-information comparisons include ordered event values/origins, skill/use/outcome, final gameplay state including RNG, actual character changes and valid health-claim instance ownership. Public-disclosure cases independently compare permitted worlds with differing hidden effects/RNG.

The mandatory full run `g-full-20261115-084734` selected both assemblies: **7,061 total, 7,003 passed, 44 failed, 14 skipped**, exit 2. Core: 5,895 passed / 2 failed / 11 skipped; CoreIntegration: 1,108 passed / 42 failed / 3 skipped. Of the 44 failures, 36 match Phase 0 by qualified case name, failure site, message and first source location. Four deeper stack traces have changed caller frames; their first failure locations and reasons are identical.

The eight new failures were all in `EnemyStageBehaviorCorpus`: its fixed test roster still uses Sniper as a Sub while its stage adapter read the newly Main-eligible production Sniper definition. The adapter now pins only that test definition's role to Regular and preserves the unit's stats, traits, attack profile, stage/environment configuration and all assertions. Production Sniper remains MainCharacter; runtime role validation was not relaxed. This is the only modification to an existing test in G; G changes no product code or production asset.

Corrective run **`g-corpus-corrected-20261115-085850`: 30/30 passed**, exit 0, no skips: all eight formerly failing corpus cases, ten G end-to-end cases and twelve authoring/production cases. The post-fix full assembly suite was not rerun; broad coverage and correction coverage are separate evidence. No newly introduced failure remains unresolved in this evidence. Do not report 7,061 all-pass or replace F's scoped 262/270 with an invented full-pass result.

Retained baseline failures:

| Fixture | Count |
|---|---:|
| CombatCameraZoomPoseTests | 1 |
| CombatStructureInspectionTests | 1 |
| DataBaseAssetTests | 8 |
| DeckMainChangePolicyTests | 8 |
| DeckSlotInfoPanelTests | 13 |
| EnemyAiTuningAssetTests | 1 |
| RunGoldFacilityTests | 2 |
| TraitActivationContractTests | 1 |
| TraitActivationOutcomeTests | 1 |

Evidence root: `Library/CodexTests/PlayerActiveSkills/phase-g/`. Each run retains invocation, prelaunch source manifest, log, exit code and NUnit XML/failures when NUnit ran. `known-baseline-failures.csv` lists all 36 names, reasons and locations; `corrected-regressions.csv` lists the eight corrections; each behavioral run has a case-level baseline comparison. The full-run manifest SHA-256 is `C0B91A0D368B1EFA5ADE626D284073C25FA7E1728E9EE833A015B3820C9036D4`. The final tested manifest is `g-corpus-corrected-20261115-085850/tested-source.csv`, SHA-256 `FE9CA0B9C160CAAE3B15CBA54FD6CC28EAD8BFFB251E0B16549F0600A4C1F556`; use `final-document-hashes.csv` for post-test documentation. No commit/push or View implementation was performed. The Unity playback items in section 9 remain unverified.
