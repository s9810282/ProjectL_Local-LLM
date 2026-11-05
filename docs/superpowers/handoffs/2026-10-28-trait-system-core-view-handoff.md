# Trait System — Core-to-View Handoff (F1)

Prepared: 2026-10-29. **Prepared for owner review; not externally delivered.**

This is **F1 from the original [Configurable Trait System Implementation Plan](../plans/2026-10-28-trait-system-implementation-plan.md)**, scheduled after Phase E+. The planned 2026-10-28 filename is retained. It is not a separate refactoring handoff. It includes the later corrections and R8–R10 contracts finalized by the [remediation plan, including X and D](../plans/2026-10-29-trait-system-remediation-plan.md).

## 1. Delivery boundary and evidence

Core supplies immutable occurrence, state, movement-source and attack-outcome facts through the existing CombatResult/Buffer path. **Ordinary activations are ordered Effects events; interception reactions are attached to the owning attack start.** The older blanket Events-only instruction is superseded for interception.

| Work | Status |
|---|---|
| Original A–E/E+ and subsequent remediation A–D/X | Implemented and verified within their recorded Core scope; combined D review found no new actionable defect |
| This original-plan F1 preparation | Prepared; no external message or recipient acceptance recorded |
| View code/routing/UI/visuals and Flow integration | Pending their owners' implementation and acceptance |
| F2 production values/assets and joint PlayMode/manual/device acceptance | Pending separate authorization and evidence |
| Legacy enhancement retirement R0–R3 | Core retirement verified; current31 producers below. View cleanup/content/delivery remain pending (§11) |

No code, tests, SO, scenes or prefabs are changed by F1. No commit/push or external message is authorized by document preparation. View owns approach/return offsets, animations, hit-release timing, appearances and scene wiring. No new trait Buffer, polling loop or duplicate gameplay calculator is needed.

**Historical original F1 source basis:** `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06` **plus the uncommitted working tree**. HEAD alone is not this implementation. F1 rechecked all **1,792** entries in `.superpowers/trait-system/TR-X3-source-manifest.json`, SHA256 `afe74fa47a1c3020a1b145d59215aeae1f99f64cab7aaf9e755fbb2c063d27d0`, with no source drift.

Historical original F1 full gates, reused then without another Unity launch: **Core 4154/4154; CoreIntegration 337/338; zero skips.** The remaining failure is `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`, `KeyNotFoundException: Stage data not found. StageKey: 2101.` CoreIntegration is not all green. Remediation added 61 passing cases; separate baseline maintenance repaired eight historical failures and changed one fixture/test name. See remediation §11.6 for exact attribution. The original E+ 4102/4106 and 317/322 counts are historical.

**Current retirement basis (R3, 2026-10-29):** HEAD `7f4e1d9dcaa5d5248a635d23c7ad9fcd5b1789fb` plus uncommitted R0–R3 changes; fresh sequential Core4089/4089 and CoreIntegration344/345, zero skips, same sole StageKey2101 failure. See the [retirement handoff](2026-10-29-legacy-enhancement-retirement-handoff.md) for the 6,948-file manifest/XML hashes, final per-case obligation ledger and prepared owner requests. This supersedes current producer/status claims, not the historical evidence above.

The ignored `.superpowers/trait-system/` evidence contains final XML, manifests, changed-source overlays and combined review diff. These are not automatically included in a future commit/delivery; include required artifacts explicitly when external delivery is authorized. Consumer compilation and Core tests do not prove View playback.

Path prefixes below are repository-relative:

| Prefix | Root |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `V/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `T/`, `TI/` | `Assets/Tests/EditMode/`, `Assets/Tests/EditMode/CoreIntegration/` |

## 2. Single playback ownership

`CombatResult.Events` supplies the ordered sequence. Its existing action/result carriers expose what must be available at their start; top-level copies are not extra occurrences.

| Occurrence | Canonical carrier | Avoid duplicate playback |
|---|---|---|
| Ordinary numeric/state/lifecycle activation | `CombatEffectsEvent.Traits`, by `(TraitKey, TraitActivationKind)` | Ordinary `AttackResult.TraitActivations` entries are summaries |
| Root interception | `CombatAttackEvent.Attack.TraitActivations`, entries with `Interception` | Do not replay from Effects or a second pass over `CombatResult.Attack` |
| Roar interception | `CombatRoarEvent.Attack.Value.TraitActivations` when Attack exists | Use its attacker/group, not the outer command actor; empty Roar has no fabricated attack |
| Native KillChain child | Existing KillChainAttack start activation and its `ChildAttack` | No new `CombatAttackEvent`, extra cost, or parent reaction entry; retired follow-up producers are absent |
| Pull travel | `CombatPullEvent.Movement.Steps` on the existing dedicated lane | Path and `AttackResult.Pull` describe the same travel; no second Moved stream |
| Knockback/ImpactBolt travel | Ordered character movement or structure displacement facts | `AttackResult.Knockbacks` is a summary, not another motion |
| Activation plus dedicated result | Activation cues identify cause; the dedicated result performs its existing work | Do not replace or replay Pull/mark/Roar results twice |

Root summaries retain ordinary root-interval/creditor/group occurrences plus canonical root interceptions. Child starts/reactions stay with their child scope. Native interception payloads are rejected in `CombatEffectsEvent`; current producers emit no standalone DamageRedirected occurrence there. A legacy activation object can still be constructed without a new payload; it is not sufficient to infer an interception relationship.

Keep `V/CombatResultPlayback.cs` generation/token, completion/failure/cancel guards and the existing `V/CombatViewSystem.cs` finalization path: `AcknowledgePresented(result.Sequence)` then `CompleteFinalization(token)` after presentation completes. Do not acknowledge when a trait cue merely begins. Result/event/activation position and owning scope distinguish occurrences; TraitKey or AttackGroup alone is not a global deduplication key.

## 3. Common activation, correlation and units

Source: `C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs`; stable kinds: `C/Contracts/Common/TraitActivationKind.cs`.

```csharp
public TraitActivationSnapshot(EntityId sourceUnitId, EntityId? targetId,
    TraitKey traitKey, TraitActivationKind traitActivationKind, int magnitude,
    IReadOnlyList<EntityId> targetIds = null, IReadOnlyList<CellCoord> targetCells = null,
    CombatDamageOrigin resultOrigin = null, Rotation4? attackDirection = null,
    CellCoord? targetCell = null);
public TraitActivationSnapshot WithInterception(AttackInterceptionSnapshot interception);
public TraitActivationSnapshot WithChildAttack(AttackOutcomeSnapshot childAttack);
```

| Member | Meaning |
|---|---|
| SourceUnitId | Trait owner/activating unit, distinct from damage creditor; its body may no longer be visible |
| TargetIds / TargetId | Canonical copied unique list; scalar exists only for exactly one target. Null scalar can mean zero or several targets |
| TargetCells | Copied affected geometry, distinct from the selected aim cell; no duplicate cells |
| TraitKey / TraitActivationKind | Stable routing keys; preserve existing serialized enum numbers |
| Magnitude | Nonnegative, kind-specific unit in §7; not universally HP or percentage |
| ResultOrigin | Related result's physical cell, creditor and group; null means no damage correlation |
| AttackDirection / TargetCell | Optional resolved child aim, including a direction with no recipient |
| Interception / ChildAttack | Copied immutable payloads, mutually exclusive; absent on ordinary legacy activations |

`C/Contracts/Combat/CombatDamageOrigin.cs` retains `CombatDamageOrigin(CellCoord? physicalCell, EntityId? creditedCharacterId, long attackGroup = 0)`. Null physical cell/creditor are permitted; group 0 means legacy/ungrouped. Do not create animation groups or reconstruct creditor from SourceUnitId. Current native contacts/interceptions require a nonempty attacking creditor and positive group, with matching full origin including physical cell. Groups are scoped to the published action/result context.

Do not globally filter Magnitude=0. Real empty Roar/child actions, Taunt selection, prevented movement and applied shielded interceptions can be meaningful. Eligibility-only checks, unchanged refresh, zero configured grants, capped-zero healing, discarded simulation and preview calculation do not commit occurrences.

## 4. Attack-attached reactions and actual recipients

### Interception and child outcome

Files: `C/Contracts/Results/AttackInterceptionSnapshot.cs`, `AttackOutcomeSnapshot.cs`.

```csharp
public AttackInterceptionSnapshot(EntityId guardianId, EntityId protectedId,
    EntityId actualRecipientId, int deliveryIndex);
public AttackOutcomeSnapshot(IReadOnlyList<AttackHitResult> hits,
    IReadOnlyList<TraitActivationSnapshot> reactions);
```

The relationship exposes GuardianId, ProtectedId, ActualRecipientId and DeliveryIndex. IDs are nonempty, guardian differs from protected, and the nonnegative index is local to this attack. Current Sacrifice has guardian = actual recipient = activation SourceUnitId, and protected = its single TargetId. The enclosing ResultOrigin owns the group; no duplicate group field exists.

`WithInterception` requires DamageRedirected and valid identity/origin. `WithChildAttack` accepts only FollowUpAttack/KillChainAttack, with start source equal to its attacking creditor and a positive group. Outcome Hits/Reactions are copied/read-only; actual recipients and delivery indices are unique. Each redirect/reaction matches one original contact and its full origin. Empty outcome remains accepted by this unchanged public contract for an actually delivered empty child shot, not a canceled action. After retirement, native KillChain is the current child producer; FollowUpAttack remains an accepted reserved kind with no runtime producer. Constructor compatibility does not supply a targetless shot.

Reactions describe real consumption/application, including zero HP loss after shield absorption. Unused/rejected reservations and Finishing/InstantDefeat have no interception. Guardian approach/return is visual only: do not change Core coordinates, AP or hazard state. Original-contact knockback policy is unchanged; follow the recorded movement target rather than assuming the guardian must move.

### Hits, contacts and previews

Files: `C/Contracts/Results/AttackHitResult.cs`, `AttackHitContactSnapshot.cs`, `AttackTargetPreview.cs`, `AttackResult.cs`, `AttackPreview.cs`.

```csharp
public AttackHitContactSnapshot(int deliveryIndex, EntityId originalTargetId,
    AttackTargetRole role, int appliedSplashPercent, CombatDamageOrigin origin);
public AttackHitResult(EntityId targetId, int targetHealthBefore, int targetHealthAfter,
    AttackTargetRole role, int appliedSplashPercent, int healthNormalizationLoss,
    int healthRecovery, IReadOnlyList<AttackHitContactSnapshot> contacts);
public AttackTargetPreview WithContacts(IReadOnlyList<AttackHitContactSnapshot> contacts);
```

The original seven-parameter hit constructor and preview signatures remain. Contacts=null is legacy self-contact. Native rows consistently use copied non-null contacts; an empty native list denotes an auxiliary row without direct contact. Contact properties have the constructor names/capitalization: DeliveryIndex, OriginalTargetId, Role, AppliedSplashPercent, Origin.

- Hits.TargetId is the **actual recipient**. Its HP, recovery, normalization, damage and death facts must not be copied from the protected unit. `DamageDealt = before - after - HealthNormalizationLoss + HealthRecovery`.
- A guardian can be a direct target and protect several hits. One actual HP row aggregates its distinct contact/reaction indices. Uniform role/percentage remains on the row; mixed contacts produce Role=None and AppliedSplashPercent=100. Inspect Contacts for original roles; None does not mean no hit.
- PrimaryTargetId, selected cells/direction and piercing encounters preserve original aim. Piercing counts original contacts, not guardian rows. Knockbacks also identify original contacts.
- Native preview uses actual recipient cells/defense/HP. A displaced original target without a direct hit can have an IsDirectHit=false auxiliary row; do not animate a numeric hit for it.
- Root and child HP/recovery scopes are separate. Root hit freeze precedes destination-arrival effects and child attacks, so it need not equal final command HP. New displacement-only preview rows use that same freeze; existing Pull/pre-hit arrival remains earlier than the hit. Later ordered health/death facts retain their own timing.
- Immutable contacts/reactions survive result/preview and legacy AttackExecutionResult copies. Old calls compiling does not mean old consumers understand the new semantics.

## 5. Typed movement and event-time state

### Movement

Files: `C/Contracts/Combat/CombatMovementCause.cs`, `CombatEvent.cs`, `C/Contracts/Results/AttackPullStepSnapshot.cs`, `AttackPullSnapshot.cs`.

```csharp
public enum CombatMovementAgency { Voluntary = 1, Forced = 2 }
public CombatMovementCause(CombatMovementAgency agency, TraitKey? traitKey = null);
public AttackPullStepSnapshot(CellCoord from, CellCoord to, CombatMovementCause cause);
public static AttackPullSnapshot FromSteps(EntityId targetId, CellCoord startAnchor,
    IReadOnlyList<AttackPullStepSnapshot> steps);
```

CombatCharacterEvent.MovementCause and CombatStructureEvent.MovementCause are optional. Old six-/four-parameter constructors remain, with additive seven-/five-parameter overloads. Structure displacement keeps its existing event kind; identify movement from anchors/cause rather than inventing a structure Moved event.

| Motion | Cause / compatibility |
|---|---|
| Ordinary voluntary Moved, including current Leap | Existing null cause retains voluntary compatibility |
| Knockback / ImpactBolt | Forced with the actual deciding Knockback/ImpactBolt key, not a trait inferred from the displaced unit |
| Environment displacement | Forced/null key; tornado retains Relocated and TornadoSourceId |
| Legacy Relocated with null new field | Still forced by its existing kind/source |
| Pull | Continuous orthogonal adjacent Steps, each From/To/Cause = Forced/Pull |
| Immunity, blocked zero travel, visual interception offsets | No fabricated movement step |

The old `AttackPullSnapshot(targetId, startAnchor, finalAnchor, path)` constructor derives typed steps. Path is the read-only To-cell projection and FinalAnchor is the last reached cell. Never animate unreached destinations or reveal hidden hazards through metadata. No new dash, per-step attack or position-swap producer is delivered.

### Character state and derived stats

`CombatTraitStateEvent` has kind TraitStateChanged and exposes Expected, Final, optional Origin, and paired nullable ExpectedStats/FinalStats:

```csharp
public CombatTraitStateEvent(CharacterCombatFacts expected, CharacterCombatFacts final,
    CombatDamageOrigin origin = null, CombatEffectiveStatsSnapshot expectedStats = null,
    CombatEffectiveStatsSnapshot finalStats = null);
```

Apply Final to the existing shown-facts model at this event, not from the final Buffer snapshot early. Supplied `CombatEffectiveStatsSnapshot.AttackPower`/PhysicalDefense are Core-derived event-time display values. `B/Character/CharacterData.cs` also exposes EffectiveAttackPower/EffectivePhysicalDefense for current/final reads. Stats.PhysicalDefense is baseline DEF. Context-dependent bypass/reduction and attack-only multipliers belong to the attack preview/result; View does not recalculate them from definitions.

| State | Read/lifetime contract |
|---|---|
| TraitEffects.AttackGrants | TraitAttackGrant has Origin, Amount, RemainingControllerTurnEnds. Current Core decrements at controller-end boundaries; View never ticks its own duration |
| TraitEffects.Shields | TraitShieldGrant has Origin, RemainingBudget, Eligible, LastResetControllerTurnCount. Reset, eligibility, absorption and source removal are distinct; other sources survive removal |
| Cohesion defense | Existing TraitEffects and Core-derived effective stats, not a mirrored aura calculator |
| Disarm / AnkleBreaker | Typed Debuffs fields consume affected-side ends; expiry/cleanup has ordered facts |
| ArmorBreak | Source stacks and exact effective defense composition remain Core-owned |
| Hunter | HunterTraitStatusSnapshot.TargetId/SuccessfulRewards are stage state. OwnedCharacterRecord.HunterAttackBonus persists through that Run and is cleared for a new Run |
| Acceleration / StationaryFire | Typed statuses retain held ATK / own-turn moved state; consume/reset facts rather than duplicating counters |
| Death/stage cleanup | CombatTemporaryStateCleanup and lifecycle facts clear temporary state; disposal without a playable result does not fabricate events |

`EffectOriginKey` (`C/Contracts/CombatEffects/EffectOriginKey.cs`) is the source identity for grants/trails: Kind, OwnerInstanceId, DefinitionKey, RuleKey. Preserve the complete key. Trait source OwnerInstanceId identifies its runtime entity; it is not the persistent owned-character ID or the damage creditor. Retain source identity even when the creator body disappears.

**CharacterDebuffState has six fields**, in constructor order: Intimidation, DefenseReduction, HuntersMark, Disarm, AnkleBreaker, ArmorBreak. A HuntersMark-only update must preserve the other five. Hunter (lock/Run bonus) and HuntersMark (consumable target debuff) are different mechanics.

## 6. Typed trail sources

`CombatMovementSourceEvent(MovementTileFact expected, MovementTileFact final)` permits Expected=null only for TraitTrailFact creation. Final is non-null. Later changes retain source ID/type and valid successor configuration. Handle creation before dereferencing Expected.

`C/Contracts/Combat/TraitTrailFact.cs` carries Id, Cell, Active, KnownTo, Origin, CreatingSide, derived Key, MovementRangeBonus, AttackRangeReduction, AttackDamageReductionPercent and RemainingOpposingTurnEnds. Units are range cells, percent and opposing-side end count. It does not trigger an arrival rule. Its trait origin is GroundCompaction or DustScreen; it is not a replacement static-terrain enum.

Render overlays by source ID, including several sources on one cell. Creation adds Final; changes replace that same ID; Active=false/remaining=0 deactivates it. Preserve underlying terrain and weaker active overlays when a stronger source expires. A source can outlive its creator's visible body. KnownTo and filtered preview visibility remain authoritative.

Accepted voluntary start/reached steps emit trails. Forced motion adds no voluntary trail/Acceleration progress. Existing Core allocation owns IDs. Active sources decrement when CreatingSide differs from the ended side; cleanup deactivates remaining trails. View does not tick duration or allocate gameplay IDs.

## 7. Producer and magnitude reference

The **current31 executable registered identities** below are verified after R0–R3 retirement (§11): all22 new traits and nine surviving legacy producers. The original E+37-producer matrix in implementation plan §11.6 is historical and unchanged; the six retired F1 rows are archived in §7A below. The current table retains the subsequent Hunter and interception/child ownership corrections. StatusConsumed, StatusExpired, StatusRemoved and StatusReset are exact enum members; shortened lifecycle labels refer to them.

Producer filenames below resolve under `C/Application/Combat/`: TraitAttackNotifications.cs, TraitKillSettlement.cs, TraitRestrictionApplication.cs, TraitPresenceReconciler.cs, TraitTurnLifecycle.cs, TraitTrailCalculator.cs, CombatTemporaryStateCleanup.cs, HunterApplication.cs, SacrificeDamagePlanner.cs, AttackDirectDamageCalculator.cs, KillChainAttackCalculator.cs, AttackKnockbackCalculator.cs, PullAttackCalculator.cs, HuntersMarkApplication.cs, CombatDebuffApplication.cs, BossRoarCalculator.cs and CombatDamageCalculator.cs. Pure rules/factories are not View dependencies; this map is for locating the owning producer when a contract discrepancy is reported.

| Key | Kind / Magnitude | Applied boundary | Persistent / dedicated read path | Regression family |
|---|---|---|---|---|
| Vanguard | AttackGranted: ATK per recipient; StatusConsumed: 1 end; StatusExpired/StatusRemoved: 0 | TraitAttackNotifications / turn and stage lifecycle | TraitEffects.AttackGrants | Grant |
| CleanSweep | AttackGranted: ATK; StatusConsumed: 1 end; StatusExpired/StatusRemoved: 0 | TraitAttackNotifications / lifecycle | TraitEffects.AttackGrants | Grant |
| CallOfBlood | HealthRecovered: actual capped HP | TraitKillSettlement before recovery | Health | Kill |
| MoraleBoost | AttackGranted: per-recipient ATK from one kill; StatusConsumed: 1 end; StatusExpired/StatusRemoved: 0 | TraitKillSettlement / turn lifecycle | TraitEffects.AttackGrants | Kill |
| Disarm | StatusApplied: remaining side ends; StatusConsumed: 1 end; StatusExpired/StatusRemoved: 0 | TraitRestrictionApplication / lifecycle | Debuffs.Disarm | Restriction |
| AnkleBreaker | StatusApplied: remaining side ends; StatusConsumed: 1 end; StatusExpired/StatusRemoved: 0 | TraitRestrictionApplication / lifecycle | Debuffs.AnkleBreaker | Restriction |
| SoloAction | ShieldGranted/Reset/Absorbed/Removed: budget delta | TraitPresenceReconciler / common damage | TraitEffects.Shields by origin | Shield |
| Cohesion | Shield lifecycle: budget delta; DefenseChanged: DEF points | Presence / common damage / public derived projection | TraitEffects.Shields; effective DEF projection | Shield |
| Entrenchment | DamageModified: applied reduction HP | Incoming contribution / applied numeric damage | Attack preview and damage breakdown; no mirrored aura | Defense |
| Sacrifice | DamageRedirected: actual redirected HP loss | Applied consumption/damage; attached to owning root/Roar/child start | Interception + actual Hits/Contacts; Sacrifice status | Defense |
| Taunt | TauntSelected: 0 for selected forced decision | Existing enemy AI taunt branch | Taunt owner status; actual move/attack decision | Decision |
| SteadfastWill | KnockbackPrevented: attempted cells | Existing immunity decision | Own status; knockback outcome | Decision |
| GroundCompaction | TrailCreated: 1; TrailChanged: remaining ends; TrailRemoved: 0; RangeChanged: cells when used | Accepted start/step / opposing expiry / applied range decision | Typed TraitTrailFact; existing allocator | Movement |
| DustScreen | Trail lifecycle as above; DamageModified: reduction HP; range remains query context | Accepted path / expiry / applied incoming/outgoing branch | Typed TraitTrailFact; public/filtered range query | Movement |
| Leap | MovementChanged: exceptional completed steps | Actual accepted traversal/arrival decision | Ordered movement facts; no hidden-source revelation | Movement |
| Acceleration | AttackGranted: held ATK gain; StatusConsumed: captured ATK | MoveSucceeded / once at attack finish | Acceleration status; TraitAttackBonus | Movement |
| StationaryFire | StatusApplied/StatusReset: moved flag; DamageModified: HP difference | MoveSucceeded / own start / target-damage contribution | StationaryFire status; final hit breakdown | Movement |
| ArmorBreaker | ArmorBroken: configured percent; StatusRemoved: 0 | After actual hit before armor state result | Debuffs.ArmorBreak exact source stacks | Cannon |
| FinishingShot | InstantDefeat: actual target HP | Pre-strike qualifying branch before common typed defeat | Death DamageCause; no numeric shield/Sacrifice spend | Cannon |
| TrackingShot | DamageModified: applied HP difference | Current-HP target contribution before hit result | Current health / hit breakdown | Cannon |
| FocusedBombardment | DamageModified: applied HP difference | Frozen action qualification / applied strike | Existing scope only; hit breakdown | Cannon |
| Hunter | HunterTargetSelected: 0; AttackGranted: positive Run ATK delta only; StatusReset/StatusRemoved: 0 | Participation/action finish / same attributed death / stage reset | Hunter status; owned HunterAttackBonus | Hunter |
| PiercingFalloff | DamageModified: actual encounter reduction HP | Actual native encounter before hit result | PiercingShot delivery record; preserved encounters | LegacyModifier |
| Knockback | KnockbackApplied: completed cells (0 for a real blocked attempt) | Actual knockback result before forced movement | AttackResult.Knockbacks and movement | LegacyDisplacement |
| DistanceDamageScale | DamageModified: applied distance-stage HP difference | Applied distance contribution | Hit damage breakdown; no persistent mirror | LegacyModifier |
| KillChainAttack | KillChainAttack: requested damage after held defense, before trait shields/HP cap | Existing repeat occurrence, add same result origin and aim | ChildAttack at each existing start; existing parent action scope | LegacyFollowUp |
| Roar | RoarTriggered: affected-cell count, including empty counter reset | Existing accepted reaction before CombatRoarEvent | Roar status / dedicated result | LegacyDisplacement |
| Intimidation | StatusApplied: retained attack-reduction percent; StatusConsumed/StatusRemoved: 0 | Existing applied debuff / attack-consume / lifecycle | Debuffs.Intimidation; priority component remains deferred | Restriction |
| ImpactBolt | KnockbackApplied: completed cells | Actual Impact Bolt result before force movement | Existing knockback result; distinct trait key | LegacyDisplacement |
| Pull | PullApplied: completed cells | Before existing CombatPullEvent | Dedicated pull result and movement | LegacyDisplacement |
| HuntersMark | StatusApplied: charges; StatusConsumed: bonus damage-stage delta before held defense/shields/HP cap; StatusRemoved: 0 | Before existing apply/consume/remove mark result | Debuffs.HuntersMark; dedicated result | Restriction |

For controller-end grant/restriction countdown occurrences, StatusConsumed has Magnitude=1 (one consumed end), while remaining duration is in the final state. ShieldGranted carries the new remaining budget; ShieldRemoved carries the old remaining budget; ShieldReset carries the nonnegative restored budget delta. A grouped shield occurrence with unequal per-recipient amounts uses Magnitude=0; inspect paired source budgets for each recipient, never interpret that zero as no change.

DamageModified is a stage delta in damage points, not necessarily actual HP loss; shields/caps can change the latter. The current KillChainAttack numeric packet remains after held defense and before trait shields/HP caps; actual HP is in ChildAttack.Hits. DamageRedirected and HealthRecovered use actual HP loss/gain. ArmorBroken's percentage is not resulting effective DEF. Read paired state/result facts for per-recipient outcomes rather than redistributing Magnitude.

Exact enum values remain in `C/Contracts/Common/TraitActivationKind.cs`: AttackGranted=90, HealthRecovered=100, DamageRedirected=110, ArmorBroken=120, HunterTargetSelected=130, DamageModified=140, through RoarTriggered=360. FollowUpAttack=80 (reserved, no current producer), KillChainAttack=70 (native producer) and deferred values remain stable. Use enum members, not contiguous numeric-range assumptions.

**Availability is not production activation.** The six retired identities report unavailable reason Retired; consume the existing Buffer availability facts, not a duplicate View policy or Enum.IsDefined. `C/Application/Combat/DeferredCombatFeatures.cs` currently disables Focus, MoveAttackDiscount, ThornField, Necromancy and Regeneration. Intimidation's priority component reports DeferredPriorityComponent. Position Swap and Recon Squad are not registered identities to enable through this handoff. No remaining ImplementationPending flag for the 22 scoped traits means their code exists, not that approved category pools, balance or visuals are ready. Main/enhanced content and all TBD production values remain F2; fixture values are not designer approval.

## 7A. Historical retired F1 producer rows

These six rows are preserved verbatim from the original37-producer F1 checkpoint. They describe pre-retirement producers, **not current support or a request to implement their visuals**. R1 removed their implementations and R2 removed only their exclusive request hooks. The original E+ matrix/results remain in implementation plan §11.6.

| Key | Kind / Magnitude | Applied boundary | Persistent / dedicated read path | Regression family |
|---|---|---|---|---|
| ChainExplosion | FollowUpAttack: requested damage after held defense, before trait shields/HP cap; actual origin/aim | Existing accepted child occurrence only | ChildAttack at existing start; no duplicate occurrence | LegacyFollowUp |
| LuckyBullet | FollowUpAttack: requested damage after held defense, before trait shields/HP cap; actual origin/aim | Existing accepted child occurrence only | ChildAttack at existing start; no duplicate occurrence | LegacyFollowUp |
| DoubleShot | FollowUpAttack: 0 for delivered directional action; resolved aim | Existing accepted independent action before its results | Direction/selected cell + ChildAttack, even no recipient | LegacyFollowUp |
| PrecisionShot | DefenseBypassed: first-recipient actual DEF points | Applied recipient-ordinal contribution | Actual delivered ordinal / hit breakdown | LegacyModifier |
| ContemptForWeak | DamageModified: applied HP difference | Applied current target comparison contribution | Current actor/target ATK / hit breakdown | LegacyModifier |
| DoorBreaching | DefenseBypassed: actual ignored DEF points | Applied distance-qualified bypass, positive effective DEF | Existing bypass contribution / hit breakdown | LegacyModifier |

## 8. Ordered examples for consumer tests

These are causal partial sequences, not templates for sorting events. Other legitimate facts may intervene; do not assume everything between two rows shares a group.

| Scenario | Interpretation |
|---|---|
| Multi-target modifier | Root attack start → applied DamageModified/DefenseBypassed for that recipient/origin → shield/HP result. Preserve compatible origin grouping; never multiply recorded HP again |
| Multi-recipient grant | One AttackGranted names recipients from one notification/kill → their state facts. Separate kills, owners and strikes remain separate occurrences |
| Sacrifice | Owning root/Roar/child start already contains interception and actual-recipient Hits → cap/state/shield/HP/death. Start guardian visuals before impact and use original aim plus actual body. Follow recorded movement policy |
| Finishing | Attack start → InstantDefeat → Died with DamageCause=InstantDefeat. No Sacrifice or numeric shield spend inferred |
| Armor Breaker | Actual hit/HP → ArmorBroken → armor state. It cannot modify the already-applied strike retroactively |
| Shield reset/removal | Source-specific ShieldReset/ShieldRemoved → trait state/effective-stat facts. Another source survives removal; reset is not absorption |
| Post-kill recovery/reward | Attributed death → HealthRecovered/AttackGranted → actual health/state/owned changes. Reward stays after the cause and is not replayed on final refresh |
| Hunter transition | Participation/action finish resolves lock → HunterTargetSelected/state with committed RNG. Stage reset clears lock/cap while the Run bonus persists separately; new Run is a distinct reset |
| Start-cell trail | Accepted movement start → TrailCreated → source creation with null Expected → first voluntary step |
| Entered-cell trail | Reached Moved → TrailCreated/source creation → subsequent consequences. Interrupted/unreached cells produce no trail; hidden hazards stay hidden |
| Trail expiry | Opposing end → TrailChanged/TrailRemoved → same-ID successor with changed count/Active. Own-side end does not consume it; weaker sources survive |
| Protected target pushed onto destination mine | Root hit belongs to guardian; original target's non-direct preview retains attack-freeze HP → later arrival/mine/death updates that target. Zero direct damage does not suppress later environmental death |

## 9. Concrete View integration work

These are current source observations, not completed consumer changes. Implementation and View regression/manual evidence belong to the View owner.

| File / owner | Observation → work required | Acceptance |
|---|---|---|
| `V/Playback/Ordering/CombatEventTypeTable.cs` and handler table | Effects is Facts-only without activation origin/subject/child routing; **CombatTraitStateEvent has no row**. Register state handling and route activations/child starts using existing handler structure | Every supported event resolves; state updates at event time; missing required wiring fails explicitly |
| `V/Playback/Handlers/CombatFactEventHandler.cs`, `V/Playback/Support/CombatShownFacts.cs` | HuntersMark uses only the first three debuff constructor arguments. Preserve all six fields and consume TraitStateChanged.Final plus supplied effective stats | Mark apply/consume/remove preserves restrictions/ArmorBreak; HUD/shown state does not read future final facts |
| `V/Playback/Ordering/CombatEventSubjects.cs`, `CombatConcurrentGroups.cs` | Existing grouping uses group + creditor within an action segment; absent/mixed origins/missing subjects remain outside grouping. New activation/state/source metadata needs routing | Distinguish activating source, actual subjects and creditor. Same-origin hits remain concurrent without merging unrelated causes or bypassing barriers |
| `V/Playback/Support/CombatEnemyPresentationPacing.cs` | Voluntary motion is inferred from actor and whether an attack started | Prefer explicit cause when present; retain ordinary null Moved and legacy Relocated compatibility |
| `V/Playback/Handlers/CombatPullEventHandler.cs` and movement lane | Dedicated Pull already owns Path playback | Consume Steps on that same lane, retaining interruptions/barriers and exactly one motion |
| Existing attack/Roar handlers, `V/Presentation/Attack/CombatAttackTarget.cs`, attack pieces | Current aim handling does not implement the new guardian/child payloads | Separate original aim from actual contact body; start reactions before hit release; do not create a new gameplay action for child playback |
| `V/Playback/Handlers/CombatFactEventHandler.cs` source branch | Only StaticTerrainFact kind changes render terrain | Support null Expected and typed source overlays, overlap/expiry and underlying-terrain preservation |
| `V/CombatResultPlayback.cs`, `V/CombatViewSystem.cs` | Existing finalization/token/acknowledgment owns result lifetime | Preserve one completion/failure/cancel path, release resources and reject late callbacks across scene teardown |
| Appearance tables/UI | Existing entry is `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs` | View owns cues/icons/text/Hunter marker/aura/trails/VFX/audio; independent panels/screens follow UIBase/UISystem and normal registration |

A consumer should not need Sacrifice selection, shield arithmetic or Hunter RNG internals to draw these facts. Shared payload/grouping changes legitimately need shared regression checks. Do not hide that dependency behind a new dispatcher or authoritative state cache. View reads Buffer/ViewGroup facts, not InGameGroup rules, and does not write Core state. Framework/Buffer/Job responsibilities and FieldCamera/Architecture ownership remain unchanged.

## 10. Acceptance and owner return evidence

Existing Core evidence includes `T/TraitActivationContractTests.cs`, `TraitActivationOutcomeTests.cs`, `TraitActivationMovementTests.cs`, `SacrificeTraitTests.cs`, `CannonThresholdTraitTests.cs`, `TraitTrailTests.cs`; native fixtures include `TI/TraitActivationPublicationTests.cs`, `TraitPresentationContractTests.cs`, `SacrificePublicationTests.cs`, `TraitTrailPublicationTests.cs`, `LeapPublicationTests.cs`, `PullPublicationTests.cs`. Full obligations/results are in the linked plans and final manifest. They do not replace consumer checks below.

- [ ] Record accepted source/contract revision and the owner for each gap in §9.
- [ ] Receive/accept the prepared verified [R3 retirement handoff](2026-10-29-legacy-enhancement-retirement-handoff.md) before owner-side consumer/content cleanup; current coverage is now31 while original F1/E+ evidence stays historical. Delivery/acceptance is not implied by document preparation.
- [ ] Add View regression coverage for event registration, debuff preservation, event-time effective stats, origin/subject concurrency, root/Roar/child reactions, original aim versus actual recipient, mixed/repeated contacts, reserved empty-outcome contract compatibility and exactly-once replay/acknowledgment. No targetless retired producer is required.
- [ ] Cover voluntary/forced causes, Knockback/ImpactBolt distinction, legacy compatibility, hidden preview facts, one Pull lane and interrupted motion.
- [ ] Cover source creation with null Expected, overlapping trails, opposing-side expiry, base terrain and source-owner disappearance.
- [ ] Cover shielded zero-HP interception, mixed-delta multi-recipient shield reset/removal, HuntersMark stage delta versus actual HP, post-kill ordering, Hunter lock/cap/Run bonus, Finishing, original-contact displacement and root versus final HP boundaries.
- [ ] Verify fault/cancel/resource release, late callbacks and scene transitions while reactions/children are active.
- [ ] Jointly verify representative attack/kill, movement, enemy-turn, stage-change and real new-Run cases in PlayMode/manual playback. Record test run, content/scene revision, reviewer and unresolved differences.
- [ ] Obtain designer-approved numbers/Main/enhanced pools and validate actual SO serialization/content in F2. Do not enable deferred traits or substitute fixtures.
- [ ] Track handoff delivered, View accepted, Flow accepted, content valid, joint acceptance and production activation independently; all remain pending until evidenced.

Preparation authorizes no external message. Record delivery/acceptance only after they happen. The known StageKey 2101 failure and the nonblocking missing direct public Hunter-summary assertion in remediation §11.3 remain recorded limitations, not silently repaired or waived by this handoff.


## 11. Verified R0–R3 retirement and pending owner work

The [retirement handoff](2026-10-29-legacy-enhancement-retirement-handoff.md) now supplies the current source manifest, fresh full gates, exact baseline failure analysis and final obligation ledger. Core retirement is complete. The original F1 source checkpoint/results above remain historical; the current table has31 producers and §7A preserves the six retired rows. No View implementation, content activation or recipient acceptance is claimed.

| Identity / seam | Current state | View implication |
|---|---|---|
| DoorBreaching130, ContemptForWeak140, PrecisionShot150, DoubleShot160, LuckyBullet170, ChainExplosion180 | Retired; numbers reserved, early registry/catalog/authoring rejection | Consume existing Buffer availability reason Retired. Enum.IsDefined is not executable support; do not duplicate Core policy |
| Enhancement products1/3/4/5/6/7/8 | Retired; table/default stage detail retained with empty candidate arrays | Product IDs and Trait keys are different namespaces. Preserve common shop/presentation schemas |
| ChangeRider2 | Reserved for the separate Main specialization plan | No working specialization offer, UI or new TraitKey is supplied |
| All22 new + nine surviving legacy traits | 31 executable producers (§7) | Keep common activation/interception/state/movement work, including native KillChain ChildAttack |
| Focus, MoveAttackDiscount, ThornField, Necromancy, Regeneration | Independently deferred | Base ThornField40/DefenseReduction/ThornFieldDescription remain; no deferred feature is enabled |

R2 proved zero surviving request-capability implementations and production constructors before removing FollowUpAttackExecutor and its five-file exclusive hook family. Native KillChain/root/Roar reactions, parent attacks/knockback/ImpactBolt/arrivals, both Hunter completion calls and public contracts remain. FollowUpAttack80 is a stable reserved kind with no current producer; generic child/outcome/source/group handling still serves KillChain70.

### Prepared View cleanup and Heavy authoring request

The final handoff §6 requests owner-side removal of exactly these two SO/meta pairs and their rows in TraitVisualDataTable:

- Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoubleShot.asset, GUID e76ae27ba9394163aa6a171e512e3bad.
- Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoorBreaching.asset, GUID37538d104da04d798bfe54388ac92022.

A fresh asset/prefab/scene GUID scan finds only the two table users; the View owner must recheck before its coordinated cleanup/validation. These View assets/table are unchanged by Core retirement. **Request prepared; not externally delivered, received or accepted.** No UI, scenes, prefabs or Flow were changed.

The user explicitly selected temporary empty Heavy steps/pool while View/content authors approved replacement SOs. Cohesion, Sacrifice, Taunt, Entrenchment and SteadfastWill already exist in Core; the absence of their production pool entries does not mean their implementations are missing. Ordinary XP/stat growth and existing growth choice/copy/receipt/rebind infrastructure remain. Author valid normal/enhanced values and satisfiable three-choice/reachable enhancement paths; do not substitute guessed values or fixtures. Main/Rider/Cannon pools were already empty. Production activation/manual acceptance remains pending.

### Shop/Flow and later work

The enhancement root1202 and node visual remain reserved, with shared DB/shop schemas and ordinary shops intact. The current production root table does not register1202 and map enhancement distribution is0; this is preserved dormant content, not operational entry. Service/rule/direct transaction tests reject legacy Sub entry/purchase with unchanged currency/owned/purchase state. Empty content does not authorize deleting shared UI/Flow or the surviving Enhancements/growth composition directory.

[Main specialization](../plans/2026-10-29-main-specialization-plan.md) remains separate future work: no once-per-Run category offer, prices or new controls are enabled. Retirement R4 cost removal is also unimplemented; current cost contracts remain. Await each verified public-contract handoff before consumer changes.

The source documents were frozen/hashed after a read-only coordination check found their owning chat idle and no other active Project_L writer. This narrow amendment updates current support/status and preserves historical evidence; idle status is not owner approval or delivery. Track View cleanup, delivery/acceptance, Flow, F2 values/pools, Main/R4 and joint PlayMode/manual/device acceptance independently. No external message or commit/push occurred.
