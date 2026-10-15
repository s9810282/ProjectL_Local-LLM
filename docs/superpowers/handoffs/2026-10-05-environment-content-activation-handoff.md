# Environment content activation handoff

Updated 2026-10-06. E1–E10 implement and verify the authorized rules, existing Buffer publication and logical View-result compatibility. This is an implementation record and owner handoff, not authorization to enable production content. User-confirmed rules are in the [design](../specs/2026-10-05-environment-content-follow-up-design.md); task evidence is in the [implementation plan](../plans/2026-10-05-environment-content-implementation-plan.md), sections 6–17.

The eight contents are High Ground, Sandstorm, Powder Barrel, Supplies, Sheriff Badge, Mine, Flag and Tornado. Wall and the obsolete HP-99 sentinel are excluded. Explicit test payloads are not production balance values. No production SO, scene, prefab, visual asset, package or assembly was changed by E10. No PlayMode or playable-stage activation evidence is claimed.

## Ownership and readiness

| Owner | Delivered | Remaining production work |
| --- | --- | --- |
| Core | Typed immutable facts; shared movement/range/damage/arrival rules; held lifetimes; turn clock; deterministic proposals | Keep these calculations authoritative when authoring conversion is connected. Multi-cell displacement/contact/rotation remains unsupported. |
| Existing Character/Combat Buffers | Validate expected state and atomically install character, source, semantic binding, allocator, RNG and turn changes | No alternative gameplay state owner or per-content Buffer is needed. |
| Existing View/Flow | `CombatSceneEventPresenter` accepts all five new logical record types; `OrderedCombatPresentation` keeps event order; `CombatViewSystem` finalizes and acknowledges once; Flow waits for that acknowledgement | Dedicated Terrain/Object/Obstacle visuals, held-effect indicators, source disappearance/drop/relocation effects, stacked visuals and Environment forecast UI. |
| DB/authoring | Pure explicit fixture seams and content identities below | Final payloads, typed serialized definitions, stage placement/visibility and conversion/query wiring; resolve independent T1/G7 targeting gate before default activation. |

The actual E10 production change is in `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneEventPresenter.cs`, the existing scene event capability owner. New logical metadata completes in the same ordered event loop. Character/structure display still uses captured facts. Tornado `Relocated` applies the captured destination immediately; it does not manufacture a walking path. There is no second Tick, Core callback, polling store, content-specific acknowledgement or View-triggered gameplay mutation.

Rejected commands intentionally publish a rejected `CombatResult` with no gameplay events and no gameplay mutation, then use the same acknowledgement path. Suppressing that rejection result would break the existing Flow protocol. The plan's proposed test name `RejectedContentCommand_PublishesNoResult` is implemented as `RejectedContentCommand_PublishesNoGameplayEvents_AndAcknowledgesRejection` for this reason.

## Content contracts and owner inputs

### High Ground — E1/E2/E4 complete

- Identity: `StaticTerrainFact`, key `static-terrain`, kind `HighGround`; runtime source ID, cell, active state and faction visibility are separate facts.
- Low-to-high entry costs base AP +2 and exactly one movement step. Descent and high-to-high travel have no surcharge. Preview, admission, actual paid ledger, range and AI agree; Return refunds the recorded ledger.
- Ordinary received damage from a known non-High-Ground physical origin is reduced 30% at the recipient's current cell. High-Ground origins are not reduced; unknown origin does not grant this protection. One-cell knockback rejects low-to-high only. Return is not knockback.
- Arrival does not consume the Terrain. Initial placement and hypothetical query positions contribute immediately; no generic entry trigger or blanket Return barrier is added.
- Tests: `EnvironmentTerrainRuleTests`, `StaticTerrainIntegrationTests`, `EnvironmentDamageTests`, movement/AI and publication regressions recorded in E2/E4.
- Authoring needs: typed kind/cell/visibility and initial placement. View needs: height/cell identity and cost/defense feedback using accepted facts; combined visuals remain deferred.

### Sandstorm — E1/E3/E4 complete

- Identity: `StaticTerrainFact`, key `static-terrain`, kind `Sandstorm`.
- Reduce current effective AppointRange by one only when it exceeds one, after other contributions. Range 0/1 remains unchanged. Departure, Return, initial placement, AI and hypothetical positions use the same current-cell query.
- Reduce ordinary received damage by 20%, including unknown-origin damage. Apply after existing matchup/PhysicalDefense/incoming-reduction stages; compose received percentages successively and floor only at the end (10 × 0.8 × 0.7 → 5). Explicit instant death is not ordinary damage.
- Static lifetime and nonconsuming arrival match High Ground. Tests: `StaticTerrainIntegrationTests`, `EnvironmentDamageTests`, query/preview/AI parity in E3/E4.
- Authoring needs: typed kind/cell/visibility. View needs: Terrain/range/defense presentation and combined visual composition.

### Powder Barrel — E5 complete

- Identity: `EntityKey.Structure_PowderBarrel`, `ObstacleContentKind.PowderBarrel`; existing structure owns HP, footprint and rotation; `ObstacleContentProfile` owns content payload.
- A qualified processing unit reacts as nonlethal Hit or Destroyed, never both. Zero damage is not a reaction. Only destruction explodes; each destroyed Barrel explodes once even when chain paths converge.
- Explicit N raw damage reaches the eight neighbors of the center, clipped to board bounds; center is excluded. Live allied/enemy field Characters and living overlapping structures are eligible once per entity. Terrain and Badge settle ordinary damage. Chains keep original credited Character separate from each blast's physical origin and protection group.
- Destroyed structures create no Character soul reward. This is attack/damage processing, not an arrival Object. Tests: `ObstacleReactionTests`, `EnvironmentDamageTests`, E5/E7 chain/publication cases.
- Authoring needs: structure definition, HP/footprint, explicit explosion damage, placement and profile conversion. View needs: captured structure/reaction/origin/group facts for destruction and chain effects.

### Supplies — E5/E6 complete

- Identity: `EntityKey.Structure_Supplies`, `ObstacleContentKind.Supplies`; existing structure HP/occupancy plus explicit Hit and Destroyed attack-bonus payloads. It does not explode.
- Each exclusive qualified Hit/Destroyed reward adds its explicit bonus to the living originally credited Character. A destroying processing unit receives only the destruction reward. No reward goes to dead credit, an unrelated activator or an originless environmental hit.
- Bonuses accumulate within the combat interval and expire before night-to-day recall, including Main; redeployment cannot restore them. Terminal battle end clears them while unrelated held contributions retain their own lifetimes.
- Tests: `ObstacleReactionTests`, `HeldEnvironmentEffectTests`, night-end/Main/chain and stale publication cases in E5/E6.
- Authoring needs: explicit Hit/Destroyed values, HP/footprint, placement and profile query. View needs: reaction, credited beneficiary and held bonus/expiry indicators.

### Sheriff Badge — E6/E7 complete

- Identity: `SheriffBadgeSourceFact`, key `object-sheriff-badge`, source ID and explicit entry ordinal. One available held charge protects one whole attack, including all hits and zero final damage; consume once per attack group.
- Each Mine blast, Barrel blast and stepped Thorn is an independent protection group. Explicit instant death, including Railway, bypasses the Badge.
- Approved arrival acquires and consumes that field Object. Duplicate pickup consumes the field Object without adding a charge. Day recall retains held state; terminal battle end clears it. Copying/deployment does not recharge a spent Badge.
- Tests: `HeldEnvironmentEffectTests`, `MineEnvironmentTests`, preparation/publication and ordered Badge/Mine cases.
- Authoring needs: typed Object placement, entry order and visibility. View needs: pickup/consumption/one-charge state and whole-attack protection effects.

### Mine — E7 complete

- Identity: `MineSourceFact`, key `object-mine`, explicit nonnegative Damage and entry ordinal; immutable active source is retained in activation records after consumption.
- User-confirmed full 3×3 consists of nine cells, including center, with uniform N raw damage. Live allied/enemy field Characters and living structures overlapping the clipped cells receive one request each. Terrain/Badge and Barrel chains use existing settlement. Mine is immune to direct/AoE damage and does not chain-detonate other Mines.
- Supported arrival: voluntary movement, existing forced movement, attack knockback, player placement, enemy redeployment and E9 Tornado relocation. Origin occupancy, Return and preparation alone do not activate it.
- General Hazard precedes Objects; Objects run by entry ordinal on the latest projection; Recovery follows. Selected sources are consumed. Death, battle end or relocation interrupts remaining Objects without consuming them. Hidden Preview does not reveal hidden identity/state.
- Tests: `MineEnvironmentTests`, `BattleEnvironmentPreparationTests`, `EnvironmentEntryPublicationTests`, `CombatMovementViewTests` captured-source/mixed sequence cases.
- Authoring needs: actual N, typed placement/order/visibility and query conversion. View needs: nine-cell blast/target/provenance, pickup ordering and consumed-source visuals from recorded facts.

### Flag — E8 complete

- Identity: `FlagSourceFact`, key `object-flag`, explicit nonnegative PriorityBonus and entry ordinal. Pickup adds N to Priority; current action continues, and the next action interval uses the bonus (an A→A sequence can occur). Existing reserved times remain unchanged.
- One held Flag maximum. A duplicate field Flag remains active without stacking. Day recall retains it; terminal battle end removes it. Nonterminal death clears held contribution and drops one Flag with a fresh monotonic source ID/ordinal, preserving payload/visibility.
- Normally use the death cell if empty, otherwise a nearest valid empty cell by squared Euclidean distance; ties use the deterministic command RNG. No candidate means clear without allocation/RNG; one candidate consumes no RNG. No special diagonal spawn rule. Exceptional creation under an eligible unit immediately acquires; a unit already holding a Flag does not stack. A new drop does not join an already captured arrival list.
- Tests: `FlagLifecycleTests`, `DynamicEnvironmentPublicationTests`, `EnvironmentEntryPublicationTests`; E10 mixed playback verifies nonterminal death→exactly one drop and immutable source facts.
- Authoring needs: actual N, typed placement/order/visibility. View needs: held Priority, pickup/clear, captured dropped-source creation facts and source icon.

### Tornado — E9 complete

- Identity: `TornadoSourceFact`, key `terrain-tornado`, stable source ID/cell/visibility and next due completed-turn counter. First due immediately after the third aggregate allied/enemy unit completion; repeat every three. Count and source due persist across day/night. Resolve before Supplies expiry/recall.
- Empty source cells still respawn. Unit destination excludes current cell, living Characters and all living structures; Terrain/Objects are allowed. Normalize position before arrival settlement, then apply Terrain/Object/soul/death/Flag rules. Multi-cell units remain unsupported.
- Independent source destination excludes current cell, other active special Terrain and living structures; units and Objects are allowed. Never overwrite Terrain or trigger a second arrival merely by spawning under a unit.
- No candidates skips that choice without RNG; a single candidate also consumes no RNG. Due advances by three. Process multiple due sources by source ID using the current projection; terminal settlement stops remaining processing/respawn.
- Visible forecast uses `CombatTimelineEntryKind.Environment` and `EnvironmentSourceId`, never a manufactured unit or Railway ID. Forecast does not choose future random destinations or consume RNG.
- Tests: `TornadoEnvironmentTests`, `DynamicEnvironmentPublicationTests`, `EnvironmentEntryPublicationTests`, `StageGimmickViewBoundaryTests`; E10 real scene test snaps captured Relocated destination without traversal.
- Authoring needs: initial typed source placement/visibility and RNG seed wiring. View needs: Terrain icon, Environment forecast branch, relocation/respawn effects; current logical acceptance does not draw these effects.

## Actual production authoring owners and gates

- [CombatStageData.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageData.cs) currently serializes Stage/Field/roster/economy, enemy and structure placements, Main/HQ positions and optional Railway. It does not serialize the eight new content placement/payload contracts. Its `CreateStageData` conversion and `CombatStageDetail` query input need a deliberate typed authoring extension.
- [StructureDataTableSO.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Structure/StructureDataTableSO.cs) is the existing structure catalog owner. [StructureDataSO.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Structure/Item/StructureDataSO.cs) creates [StructureData.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Structure/Item/StructureData.cs), which currently holds key, footprint, HP and attack fields. Barrel/Supplies identities exist; their content-profile/reward/explosion payloads still require explicit conversion rather than inference from key alone.
- [DatabaseBattleInitializationSource.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/DatabaseBattleInitializationSource.cs) still validates Buffer binding, reads through `BattleDefinitionAdapter`, then throws **T1/G7** because Method_Key/unit association/query schema is unavailable. Resolve that independent contract before enabling default production battles. E10 does not remove this guard.
- `StageEnvironmentAdapter.Read` accepts explicit fixture static Terrain/Object/Tornado placements; `ReadObstacles` preserves explicit profiles. `BattleEnvironmentPreparation` validates semantic classification and coexistence. These seams prove supported installation but do not imply a serialized production schema exists.
- Default coexistence: Terrain + Object allowed; Terrain + Obstacle and Obstacle + Object prohibited; ordinary floor is not special Terrain. Object entry order is distinct from source ID and rule Priority. Keep source IDs, entry ordinal, faction visibility, balance payloads and stage identity explicit.

Production activation requires a separately authorized enabled-content/stage list, the outstanding authoring/query contracts, final payloads/visibility, visual resources and end-to-end/PlayMode evidence for those concrete configurations. G6 gameplay decisions are closed; G7 production authoring/default targeting and stacked/dedicated visuals remain open. Do not reopen confirmed gameplay merely to fill missing asset fields.

## E10 validation evidence

Evidence and exact final counts are recorded in implementation-plan section 17. Runs use Unity 6000.0.73f1 batch EditMode through the authorized executable path, against `ProjectL.Tests.Core`, `ProjectL.Tests.Framework` and `ProjectL.Tests.CombatViewBoundary`. This verifies logical integration and actual scene Presenter behavior in EditMode; it does not verify PlayMode or production stage authoring.
