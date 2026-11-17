# Piercing and Dead Hand — Core to View Handoff

Date: 2026-11-14

[Design](../specs/2026-11-14-piercing-and-dead-hand-design.md) · [Plan and verification](../plans/2026-11-14-piercing-and-dead-hand-implementation-plan.md)

## Commands and previews

The existing CombatView Buffer accepts these factories.

```csharp
CombatViewCommandData.Attack(EntityId attacker, AttackAim aim,
    long expectedBattleInstanceId, long expectedControllerTurnSequence, AttackSweepSide? sweepSide = null)
CombatViewPreviewData.Attack(EntityId attacker, AttackAim aim, AttackSweepSide? sweepSide = null)
CombatViewPreviewData.AttackRange(EntityId attacker)
```

For SoulEater use `AttackAim.ForDirection(AttackDirection.Up)` or another offered direction. Fill the command identity from `CombatStateReadSink.BattleInstanceId` and `Current.CommandState.Turn.ControllerTurnSequence`. Previews use the request number from `CombatViewWriteSink.SetPreview`; accept the matching response from `CombatStateReadSink.TryGetLatestPreview`. Clear a selection's outstanding preview with `ClearPreview(requestNumber)`.

```csharp
var aim = AttackAim.ForDirection(AttackDirection.Up);
var defaultRight = CombatViewPreviewData.Attack(attacker, aim);
var leftPreview = CombatViewPreviewData.Attack(attacker, aim, AttackSweepSide.Left);
var rightPreview = CombatViewPreviewData.Attack(attacker, aim, AttackSweepSide.Right);

var defaultCommand = CombatViewCommandData.Attack(attacker, aim, battleId, turnSequence);
var leftCommand = CombatViewCommandData.Attack(attacker, aim, battleId, turnSequence, AttackSweepSide.Left);
var rightCommand = CombatViewCommandData.Attack(attacker, aim, battleId, turnSequence, AttackSweepSide.Right);
```

Use the same aim and side for the final preview and the single confirmed submission. Omitting the side resolves to facing-relative Right. Explicit enum values are Left = 1 and Right = 2. Use the published `Sweep.Facing` and `Sweep.Side` for arrows and presentation.

| Facing | Relative Left | Relative Right |
|---|---|---|
| Up / R0 | Board Left | Board Right |
| Right / R90 | Board Up | Board Down |
| Down / R180 | Board Right | Board Left |
| Left / R270 | Board Down | Board Up |

Cell and Direction use the existing attack-facing convention. All uses the actor's existing field rotation. Preserve each targeting mode's normal admission rules. For Direction, a clicked unit can help pick the direction. Core resolves the actual enemies again when it processes the submitted direction; a preview cannot reserve targets or destinations. For Single, the selected enemy remains the actual target, and the legal basic range provides the boundary. For All, the selected full mask provides the boundary.

## Read contracts and playback

- Read `CombatPreviewResult.Attack.Value.Sweep` and `CombatResult.Attack.Value.Sweep` when present. `AreaCells` supplies the sweep boundary and can extend past the board. Use the attack preview's board-clipped `DesignatedCells` for the attack overlay. For SoulEater the directional mask is three cells wide and two deep at base range. `CapturedTargetIds` lists the eligible enemy units selected by the attack. Walls can leave valid enemies in the mask while blocking their forced movement. Allies and structures are outside the sweep target set.
- `AttackSweepSnapshot` carries `Origin`, `ActorId`, `Facing`, `Side`, `AreaCells`, `CapturedTargetIds`, ordered `Targets` and `Interrupted`. Each target row carries `TargetId`, `StartAnchor`, `IntendedEndpoint`, `EnteredCells`, nullable `FinalAnchor` and `Stop`. An intended endpoint is one tile beyond the row's outer mask edge and can be outside the board. Actual paths and anchors come from the snapshot.
- `Completed` reached the intended endpoint. `Blocked` stopped on the last legal cell, possibly at the start. `Immune` stays at the start and remains a possible hit. `Interrupted` can reflect death, a terrain relocation, or combat termination. `NotProcessed` marks a target after combat or the actor stopped the sweep. Read `FinalAnchor` and the accompanying combat events to distinguish surviving, relocated, and removed units.
- `CombatResult.Events` includes `CombatSweepEvent` with `CombatEventKind.Swept = 50`. Its `Sweep` is the same finalized snapshot as the attack result, and `AttackGroup` matches the attack group and ordinary damage attribution. Match a group within its combat result and use `Sweep.Origin` for the trait source. The group starts with the header, followed by its forced movement and entry effects, then the corresponding `CombatAttackEvent` and ordinary grouped damage. Entry terrain effects, including damage and death, retain their event order during movement.
- Use the normal state-change events for placements, movement, relocations, HP, death, and stage outcome. Use the sweep header to prepare one movement presentation group. Parallel animation can use the reported routes, with an explicit barrier before the ordinary attack damage presentation and with terrain interruption ordered by the relevant entries. Avoid playing a forced route twice through both the header and its corresponding `Moved` events. A lethal entry can leave the sweep result with zero ordinary hits. Sacrifice may route a later hit to a different defender. Use the reported damage recipient and do not infer an additional sweep move.
- For rank-aware descriptions read `CombatStateReadSink.Current.TraitSettings.For(character)`, where `character` is the combat facts from the Character Buffer. Call `TryGet<DeadHandTraitSpec>(out var spec)` and use `spec.DamagePercent`. Use the existing owned-card trait settings outside battle. Core applies the resolved value once to the basic attack base with half-up rounding, then settles existing damage rules. Use the attack preview/result for HP and damage values. Public previews use the observer's known environment, so hidden effects may change the live route or result.

Piercing still publishes its ordered encounters via `AttackResult.PiercingShot` and `AttackPreview.PiercingShot`. Its primary aim uses the basic attack range and a hostile non-obstacle target. Friendly encounters along that delivery line still consume an encounter and use the normal ordered falloff.

## Unity integration checklist

SoulEater is registered in InGame as unit key 27, targeting method 11, and Dead Hand trait key 530 with rank percentages 100/100/100. The current View event table routes the sweep header to Skip. Placement and field presentation require a registered UnitVisual. Use a test-owned deployment or fixture after adding the visual references. Acquisition, UnitVisual/TraitVisual registration, final display naming, attack-side controls, and a playback handler for `Swept` remain View or content work. View compilation, interactive behavior, and playback acceptance are unverified.

- [ ] Register the unit and trait visuals. Check the prefab, facing, animation, hand graphic, trait localization, and description formatted from the published percentage.
- [ ] Check all four attack-direction overlays, Left/Right arrows relative to each facing, and the omitted-side default. Check the mask near field edges and compare shown routes with the actual final cells for walls, blockers, immunity, and terrain interrupts.
- [ ] Check that the hand enters from the opposite side of the chosen sweep direction. Animate resolved targets in the same presentation group, then present ordinary attack damage after that group. Check terrain activation/death during movement and Sacrifice on the resulting board.
- [ ] Cancel at the aim and side stages and verify zero action use and no stale overlay. Check stale hover replacement, one confirmed submission, terminal interruption, scene exit, and release of playback handles and pooled visuals on normal completion, cancellation and errors.
