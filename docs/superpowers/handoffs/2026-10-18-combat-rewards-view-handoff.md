# Combat Rewards — View Handoff

Date: 2026-10-18

Status: Core R0-R5 delivery record. This is a local handoff; View implementation and external delivery are separate. Final verification and limitations are in plan §15 (Git history: `2026-10-18-combat-rewards-plan.md#15-execution-record--r5-2026-10-18`). The approved design (Git history: `2026-10-18-combat-rewards-design.md`) owns gameplay decisions. The B1-B3 internal boundary follow-up is complete; its verification record (Git history: `2026-10-18-combat-reward-boundaries-plan.md#9-execution-record--b3-2026-10-18`) supplements the original delivery without changing these View contracts.

## 1. Display contracts

Use `IBufferQuery` from the existing Buffer registration. View consumes these values; it does not query InGame DB, calculate progression, infer rewards from enemies, or write balances.

| UI element | Exact read contract | Meaning |
|---|---|---|
| Live combat gold | `IBufferQuery.CurrencyReadSink.Amount : int` | Current Run balance, including spending and immediate attributed-kill grants. Existing `CombatHud.Render(int runGold)` displays this balance. |
| Live fame, if needed | `IBufferQuery.FameReadSink.Amount : int` | Current cumulative fame in this runtime, retained across Run reset. |
| Clear screen data | `IBufferQuery.CombatStateReadSink.Completion.Rewards : BattleRewardReceipt` | Immutable receipt of the installed victorious settlement. Check Completion and Rewards for null. |
| Stage N-N | `reward.StageDepth : int`, `reward.StageWidthIndex : int` | Completed node depth and zero-based index within that depth. Format `Stage {StageDepth}-{StageWidthIndex + 1}`. Depth 2/index 0 is Stage 2-1. Never derive either from StageKey. |
| Gold +NNN on clear | `reward.TotalGoldEarned : int` | Gross battle grants, checked `KillGoldEarned + ClearGold`; it is not the current balance or balance difference. |
| Reward breakdown | `reward.KillGoldEarned`, `reward.ClearGold`, `reward.ClearFame : int` | Sum of eligible kill gold across the battle; configured clear gold/fame. |
| Initial participant portraits | `reward.ClearParticipantIds : IReadOnlyList<CharacterInstanceId>` | Frozen cohort, main first then stable owned IDs. Look up each ID in `reward.Characters`, then resolve that row's EntityKey through existing View resources. |
| Per-character reward row | `reward.Characters : IReadOnlyList<CharacterRewardReceipt>` | Union of initial participants and characters with pending kill XP. Additional rows are possible for later killers; they do not become initial-participant portraits. |
| Character identity | `row.CharacterId : CharacterInstanceId`, `row.CharacterKey : EntityKey` | Stable owned identity and definition key. Multiple owned instances may share a key. Scene EntityIds and surviving objects are not the row identity. |
| XP gain | `row.KillExperience`, `row.ClearExperience`, `row.TotalExperience : int` | Fixed attributed kill XP; eligible clear XP; checked sum applied once on victory. |
| XP bar before | `row.LevelBefore`, `row.ExperienceBefore`, `row.RequiredExperienceBefore : int` | Owned level, progress within that level, and threshold at the start of this settlement. |
| XP bar after | `row.LevelAfter`, `row.ExperienceAfter`, `row.RequiredExperienceAfter : int` | Actual installed owned progression after settlement. For the final bar use After values. Animate multiple level crossings using the existing presentation policy. |
| Battle occurrence | `reward.BattleInstanceId : long` | Monotonically allocated battle lifetime identity. A later battle can reuse StageKey; never use StageKey alone to cache a receipt. |

The receipt and its nested lists are immutable defensive copies. Character rows expose get-only scalar values. In a legacy/developer definition with progression disabled, XP and threshold values are zero and progression does not change; hide or disable the XP bar when its threshold is zero. Do not divide by zero or reconstruct thresholds in View.

## 2. Gameplay semantics relevant to View

- Default initial hand is **main + up to three subs** (total 4). Main remains manually placed from slot 0. Explicit custom hand sizes keep their meaning.
- The first accepted EnterCombat captures living deployed owned allies before entry effects. Starting with fewer than four is valid. Hand members are not included. Later nights, draws, deaths or returns do not replace that cohort.
- A participant that later dies or returns to Hand still receives its clear XP. An off-cohort killer receives its kill XP only. A victory before initial capture has no clear XP or participant portraits; other eligible rewards still apply.
- Kill gold is immediate. Kill XP stays pending in `CharacterData.ToFacts().Experience.Earned` until victory. This value contains kill XP only, not clear XP; do not add it to the receipt again.
- Fixed gold/XP: Demon 10/10, Gravekeeper 20/20, Heavy 30/30, Undead Cannon Cart 25/25, boss unit 300/500. Ballista is excluded. Enemy level does not multiply rewards. Both kill components require owned-ally attribution; actorless/uncredited deaths grant neither.
- Victory adds configured 100 gold, 50 fame and 100 XP per cohort member. Defeat/abandonment grant no clear reward or pending XP application. An attributed kill in a defeat still follows the immediate kill-gold rule. Main death retains defeat precedence even when the boss dies simultaneously.

## 3. Publication and existing completion flow

Settlement is already applied atomically before Completion becomes visible. Do not implement a claim/grant call when the user confirms the clear screen. Core now creates one immutable canonical settlement and finalized XP rows; receipt projection and Buffer consume that evidence without recalculating eligibility, amounts or thresholds. The internal input-copy API preserves command context. These changes add no View-facing read member, state owner, refresh trigger or confirmation step; continue consuming the existing receipt below.

1. Existing result-sequence handling consumes `CombatResultReadSink.TryGetLatest` / `CombatStateReadSink.LatestResultSequence`, plays the results, refreshes from installed Buffer values and acknowledges presentation. Keep the current currency/HUD refresh path.
2. `CombatPlan` sees Completion, suspends new submissions and waits for remaining presentation. It requests `PresentCompletion` only when the View is attached and no presentation remains.
3. `CombatViewSystem.PresentCompletion(BattleCompletionChanges)` accepts the current completion reference once. Existing `ICombatBattleDisplay.ShowCompletion` receives it and the confirmation callback. Read `completion.Rewards` there to populate the clear screen.
4. The existing callback changes `CompletionDisplay` from Showing to Confirmed. A subsequent Plan evaluation schedules ExitBattle and the existing EndBattle/continuation/new-Run/scene sequence. Confirmation must not mutate Core/Buffer or call CompleteBattle again.

PresentCompletion rejects a null, stale or already-shown completion and a call while presentation is pending. The current reference/lifetime guards remain. There is no new ClaimRewards or ContinueRewards API. Existing experience events may be presented or consumed as facts through the current event registry; clear XP is reflected in ExperienceApplied.Amount and actual owned changes.

## 4. Availability, lifetime and errors

Before completion, Completion/Rewards can be null. Defeat/abandonment has no reward receipt. Legacy isolated definitions without ClearRewards can also produce a null receipt. Production rewarded victory requires a receipt; treat an unexpected null in Database-mode victory as an integration fault rather than guessing values.

EndBattle retains Completion and the receipt for reads during exit. Preparing a new battle or resetting/publishing a new Run clears it. If the exit path immediately creates a new Run, complete the display/confirmation before that reset; do not fetch the prior receipt afterward. Retaining a reference for display does not grant again.

Fame starts at 0 in a newly constructed runtime and survives chapter changes, EndBattle, PublishNewRun and Run reset. Disk persistence/application-restart save support is not part of this delivery. Run gold retains its existing reset policy.

Stale balances/state, malformed receipt identity/cohort/progression, wrong completed-node coordinates, missing configured rewards and checked overflow fail before partial publication. Keep the existing fault boundary; do not substitute reward defaults or retry a stale transition.

## 5. Compiled display example

This exact expression is compiled as `CombatClearRewardIntegrationTests.RewardLabel` and executed against an installed registered-Core receipt in the R5 integration tests:

```csharp
string RewardLabel(BattleRewardReceipt reward) =>
    $"Stage {reward.StageDepth}-{reward.StageWidthIndex + 1} / Gold +{reward.TotalGoldEarned}";
```

For the portraits iterate ClearParticipantIds, select the matching Characters row by CharacterId, and render ExperienceAfter / RequiredExperienceAfter with LevelAfter. A Demon + Gravekeeper battle displays Gold +130, even if 4 gold was spent between grants. Adding a Minecart kill displays Gold +155. Current Run balance remains independently readable through CurrencyReadSink.

## 6. Deferred connections/content

- Implement clear-screen layout, portrait/XP animation and any fame display in View using the contracts above. No View script, scene, prefab or presentation asset was changed by this feature.
- The user explicitly excluded editing `ViewSystem/Dev/DevBattleInitializationSource`. Its developer Fixture UnitSpecs omit KillReward, so an attributed kill fails the deliberate missing-definition validation. Fixture progression is also disabled. Use **Database** battle-definition mode. The View owner can later configure the Fixture definition explicitly; no Core fallback should hide missing configuration.
- Undead Heavy is registered as `EntityKey.Unit_UndeadHeavy = 26`, unit table entry and dedicated targeting key 9. Stats: HP/attack 10/10, defense 5, movement 2, move/attack costs 1/2, maximum AP 6, priority/Delay 30/35, 1x1 footprint, EnemyOnly, Heavy category, attack range 1, EightDirectionLine/Single/no splash/angle 1, Regeneration 10, rewards 30/30. Protection uses offsets (-1,1),(0,1),(1,1),(-1,2),(0,2),(1,2), shared reduction 50%. This is the approved oriented shape, not a mirrored neighborhood.
- Minecart (key 25) has a configured saved Unit asset but is not in the operational Unit table. R5 uses that saved asset explicitly in its test-only definition for the 25/25 variation; this does not activate an operational spawn or table entry. Coordinate activation/targeting/resources as a separate content integration.
- Heavy production spawn assignment and View visual resources remain unassigned. Critical hits are a separate follow-up. Do not execute the older View-inclusive UR4 plan from this handoff.
- Framework registration and fake-query migration are described in the [Framework share](2026-10-18-combat-rewards-framework-share.md).

## 7. Boundary follow-up verification

B3 verified the full Core assembly (3,343/3,343) and related Framework integration (192/192), complete XML and Unity exit 0 for both. Coverage includes Database-backed two/four participants, gross gold 130/155 despite spending, participant/nonparticipant XP, pre-start victory, actual next-night cohort preservation, reflection/automatic publication, receipt lifetime and fame across Run reset. No additional code, content or View changes were necessary in B3. This is automated evidence, not manual clear-screen/scene acceptance. All deferrals in section 6 and runtime-only fame persistence remain unchanged.
