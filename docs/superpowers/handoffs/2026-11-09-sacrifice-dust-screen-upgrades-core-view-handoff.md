# Sacrifice and Dust Screen rank upgrades — Core to View handoff

## Training cards and purchase

Both production traits cap at rank 2. Read `CurrentProgressReadSink.Shop.TrainingChoices` for eligible upgrades. Each `TrainingChoiceSnapshot` includes `OwnedId`, `TraitKey`, `CurrentRank`, `NextRank`, `Price`, `CanPurchase`, and `CurrentValues` / `NextValues`. The visit captures the definitions and price at entry. Render the returned price.

| Trait | Rank 1 → rank 2 in `TraitRankValues` | Effective settings |
| --- | --- | --- |
| Sacrifice | `bool PreventsLethalSacrificeDamage` false → true. `int Amount` (shield grant) 0 → 10. | `SacrificeTraitSpec.PreventsLethalSacrificeDamage` (bool) and `ShieldOnActivation` (int). Radius 2 and one trigger per enemy turn at both ranks. |
| Dust Screen | `bool ExemptsAlliedAttackRangePenalty` false → true. `Amount` is 0 at both ranks. | `DustScreenTraitSpec.ExemptsAlliedAttackRangePenalty` (bool). Range penalty 1, attack damage reduction 20%, and one opposing turn end until expiry at both ranks. |

`SecondAmount` is null for both traits. Give each changed boolean a localized label in View-owned text. Show both the redirected-hit survival effect and the shield grant on Sacrifice's card. Show that allies of the trail creator ignore its range penalty on Dust Screen's card and omit the unused numeric line. Use the returned values so the text follows the actual effects.

Submit `ShopViewCommandData.PurchaseTraining(context, choice.OwnedId, choice.TraitKey, choice.CurrentRank)` using the current `ShopVisitContext`. Read the corresponding `StageCommandResults` shop result, including the completion result. The receipt carries the previous and final ranks and the price. Direct Core purchase publishes `Shop.TrainingReceipt` during the visit. The shop command also attempts visit completion after a successful purchase. A nonterminal rank mismatch returns `StaleRank`. A repeat against the same still-open purchased visit returns `PurchaseAlreadyUsed`. At the cap in a fresh visit, requests return `RankLimitReached` for both the previous and current rank. A mismatched entry context returns `StaleEntry` when the request reaches active-entry validation. The next visit omits the capped trait from its choices.

## Character cards and combat

- For the owned card, request a refresh through `CharacterStatsRequestSink` and read `CharacterStatsReadSink.ReadOwned().Items`. The matching `OwnedCharacterStatsSnapshot` exposes `TraitRanks` and `TraitSettings`. During combat, `CombatStateReadSink.Current.TraitSettings.For(characterFacts)` resolves the installed settings for that character.
- Play `CombatResult.Events` from the combat result Buffer in order. Sacrifice's redirected hit applies the protector's existing shield before settling HP. Rank 2 leaves the protector at a minimum of 1 HP for that redirected hit. The new shield is granted after that settlement and may absorb subsequent hits. For a hit with HP loss, play the HP change before the shield state change and the `TraitActivationKind.ShieldGranted` activation. The grant and redirect activation can still occur at zero HP loss. Read the canonical shield status for the remaining budget and keep separate damage governed by its own result.
- Read `CombatStateReadSink.Current.Environment` for trail facts. `TraitTrailFact.ExemptsAlliedAttackRangePenalty` captures the flag at trail creation alongside `CreatingSide`. For range highlighting, use the published attack preview or the side-aware `TraitTrailCalculator.RangeReductionAt(cell, environment, queryingSide)` with the querying actor's `Owner`. The two-argument overload is unfiltered. Both factions still receive the trail's attack damage reduction.

## Formatter and asset gap

`Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingCardContentBuilder.cs` currently renders only `Amount` and `SecondAmount` in `ValueLine`. That method requires `RankValueFormat` and throws when it is empty. Both of these production View rows have an empty `rankValueFormat`:

- `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Heavy/TraitVisual_Sacrifice.asset`
- `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Rider/TraitVisual_DustScreen.asset`

After the Core SO activation, opening the trait cards for a trainee with either eligible choice hits that exception in the current formatter. The View owner needs to add the localized effect text and format the numeric shield value and the boolean transitions. The Dust Screen card needs a boolean presentation path that can render without a numeric format. UI acceptance remains pending.

## User Unity checklist

- Inspect both production growth SOs and confirm the rank-2 managed references and configured values in the Inspector.
- Open a training visit with each eligible trait. Confirm the Sacrifice card shows survival on the redirected hit and shield 0 → 10. Confirm Dust Screen describes the allied range exemption and the formatter reports no missing text.
- Buy each upgrade and verify the updated character stats. Reopen training and confirm there is no rank-3 offer for either capped trait.
- Trigger Sacrifice and compare the protector's HP and shield playback with the ordered Core events.
- Compare attack range highlighting for allies and opponents on a rank-2 Dust Screen tile, and confirm both sides still receive the damage reduction.
