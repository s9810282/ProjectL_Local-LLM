# Rampage, Lava Cannon, and Bleeding — Core-to-View Handoff

Date: 2026-11-07

Scope: Core combat, Buffer publication, rank conversion, and three dormant InGame authoring assets. The activation gate below covers the paired View and content work.

## Read contracts

- For current unit state, use `ICharacterBufferReadSink` and `CharacterData.StatusEffectSnapshots`. Consumers holding the event's immutable character facts can call `StatusEffectReadModel.Capture(facts)`. During playback use `CombatTraitStateEvent.Expected/Final` and their optional `ExpectedStats/FinalStats` in event order. The latest Buffer state can be ahead of the event being played.
- Configuration is available as resolved `TraitSpec` values in `TraitSettingsSet`, with the rank map on the owned stats snapshot. `CharacterCombatFacts.StatusEffects` owns recipient status history. `RampageTraitStatusSnapshot.KillAttackBonus` owns Rampage's battle-local kill accumulator. All public status snapshots are immutable projections.
- `CombatEffectsEvent.Traits` uses `StatusApplied` for Lava Cannon and Bleeding and `AttackGranted` for the Rampage kill reward. Use actual state snapshots for persistent display. Bleeding's application magnitude is the current stack count. Lava Cannon's application occurrence has magnitude 1 and can cover both fire and slow for that recipient. Neither magnitude is a damage number. Health/death events carry the `CombatDamageOrigin` of actual damage, including credited entity and attack group.

| Trait key | Resolved configuration |
|---|---|
| `Rampage = 460` | `RampageTraitSpec.BaseAttackBonus` is the normal authored n, `RankAttackBonus` is the offset 0/5/7, and `MovementBonus` is 1/1/2. Configured attack before kills is n/n+5/n+7. |
| `LavaCannon = 470` | `LavaCannonTraitSpec` has fixed rules and no rank payload. |
| `Bleeding = 480` | `BleedingTraitSpec.DamagePerStack` is 1/2/3 at the supplied ranks. Existing stacks retain their original damage and origin after a rank change. |

| Status key | Snapshot and display fields | Lifetime and ownership |
|---|---|---|
| `Fire = 15` | `FireEffectSnapshot.Damage` (2), `RemainingAffectedSideStarts`, and `Sources[0].Origin`. | Debuff. `AffectedSideTurnStarts = 8`. Two future starts of the affected side. Reapplication refreshes the duration and source. |
| `Bleeding = 16` | `BleedingEffectSnapshot.Stacks` contains each immutable `BleedingStack.Damage` and `Origin` in application order. `RemainingAffectedSideStarts` belongs to the whole set. | Debuff. Lifetime 8, three future affected-side starts. Reapplication refreshes the set even at five stacks. The compatibility origin of the aggregate entry is its first retained stack. Use each stack's own origin for source detail. |
| `MovementRangeDecrease = 17` | `MovementRangeDecreaseEffectSnapshot.Magnitude` (1), `ExpiresAtCompletedControllerTurnCount`, and `Sources[0].Origin`. | Debuff. `NextAffectedSideTurnEnd = 9`. The value is an absolute completed-turn counter. Slow expires after the next full turn of the affected side. A reaction during that side's running turn waits through its following turn. |
| `Rampage = 18` | `RampageEffectSnapshot.AttackBonus`, `MovementBonus`, and `KillAttackBonus`. AttackBonus includes the kill portion. | Buff. The snapshot exists for an eligible living field holder strictly below 40% HP. It has empty `Sources` and `ConditionLifetimeKind = CurrentEligibility` (6). The accumulator stays in trait state, and healing to 40% or more clears it. |

Lava uses the accepted attack's captured primary/splash recipients. Reflection can kill the Cannon before all hits and surviving captured enemies still get the area statuses. Bleeding applies on actual damage-bearing contact, including full absorption and a Sacrifice recipient that survives. Fire/bleeding do not count as attack contacts. The affected-side start runs shield expiry/start resets, then the captured periodic batch in entity order, with fire before Bleeding and Bleeding stacks in application order. Core applies each tick through shared damage settlement and publishes the existing terminal result before reinforcement or input continuation. Slow expiry, effective movement, HP eligibility, and reward settlement are Core decisions. View reads their results.

## Required paired View work

1. Register the four typed formatters in `CombatUnitStatusFactory`, with matching entries in `UnitStatusVisualDataTable` and description template argument counts. `CombatUnitStatuses.AddTo` calls `CombatEffectFormatters.Get(snapshot.Key)` first. A visible key with no formatter throws `InvalidOperationException` with `표시 변환이 등록되지 않았습니다`. A formatter without a matching visual fails visual lookup during factory setup. `CombatStatusEffectSource` uses optional `WhileEffect` visuals for persistent VFX.
2. Extend `CombatStatusDescriptionFormatter` for `FireStatus`, `BleedingStatus`, and `MovementRangeDecreaseStatus`, and lifetime cases 8 and 9. The existing default switches throw for unrecognized status types or lifetimes. Render the four public snapshot types using their published fields. Rampage has no source row, so its current condition and bonus breakdown belong in its typed summary.
3. Add `TraitVisualDataTable` entries for keys 460, 470, and 480, including names/icons and descriptions. `UnitCardTraitContent` requires a visual for a displayed trait. Use `TraitDescriptionBindings` if the description uses numeric tokens and bind the resolved `TraitSpec` fields. Provide `RankValueFormat` for the two trainable traits. `TrainingCardContentBuilder` uses current amount, difference, and next amount at slots 0–2, with the same sequence for the second amount at 3–5. Rampage amount means movement and second amount means **attack offset relative to n**. Bleeding amount means per-stack damage. Lava Cannon has no training row.
4. Verify status icons/descriptions, rank text, event playback, reflection/Sacrifice, and tick/slow expiry in a test scene. Use effective movement/attack query data where a total is shown. Use the status breakdown as explanation of an already-projected value.

## Production activation sequence

The three assets are under `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/`.

| Asset | Authored contents |
|---|---|
| `TraitGrowth_Rampage.asset` | CategorySpecific, intended Rider pool. `baseAttackBonus` starts at 0 as a designer placeholder. Normal movement 1, rank 2/3 rows movement 1/2 and offsets 5/7. |
| `TraitGrowth_LavaCannon.asset` | CategorySpecific, intended Cannon pool. Fixed normal definition. |
| `TraitGrowth_Bleeding.asset` | Common. Normal/rank 2/rank 3 damage per stack 1/2/3. |

Complete the View registrations and verify their presentation in Unity. Then choose the production n, register the desired assets in `TraitGrowthDefinitionTable.asset`, and add them to the chosen `CategoryTraitProgression_*.asset` pools. Check existing candidate sufficiency and Main specialization with the chosen live configuration. Existing category-default and loadout assets keep their authored roles. Run `DataBaseAssetTests` and production PlayMode after the paired activation, including acquisition, training for both trainable traits, preview, actual combat, and cleanup. Treat any current failures in `DataBaseAssetTests` separately from new activation failures.

Core edit-mode evidence and existing failure comparison are recorded in [the implementation plan](../plans/2026-11-06-rampage-lava-cannon-bleeding-implementation-plan.md#9-execution-record-and-plan-self-review). PlayMode and production activation acceptance remain user checks.
