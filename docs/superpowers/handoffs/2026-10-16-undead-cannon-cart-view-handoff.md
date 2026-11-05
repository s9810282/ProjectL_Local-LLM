# Undead Minecart (Undead Cannon Cart) — Core data and View handoff

## Scope and decisions

The user is working as the Core programmer for this task. Deliver the gameplay definition and verification, then hand off View integration. The approved unit is **언데드 마인카트 / Undead Minecart**, initially called **언데드 대포 카트 / Undead Cannon Cart**, category **Cannon** (the initial Rider request was corrected). These names refer to the same new unit, with the existing code identifier `Unit_UndeadCannonCart`; no third unit is created. The user explicitly clarified that it is an **enemy-only unit**, separate from the player Cactus Cannon. Reuse existing Cannon and radial splash calculations, with no traits. No new attack calculator or runtime system is needed.

Splash percentage is editable authoring data, initially **50**, not a hard-coded rule. The cart has its own targeting SO so tuning it cannot change Cactus Cannon. The existing Cactus method remains at 70. The percentage applies only to additional targets, before existing category and defense adjustments; 6 damage at 50% gives 3 before those adjustments. Integer division truncates fractional results. Existing authoring validation requires a positive percentage when splash is enabled; selecting None requires zero.

## Delivered definition

| Item | Value / location |
|---|---|
| Identity | `EntityKey.Unit_UndeadCannonCart = 25` |
| Unit SO | `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_UndeadCannonCart.asset` |
| Targeting SO | `Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/Item/TargetingMethod_UndeadCannonCart.asset` |
| Method key | 8 |
| Category | Cannon (40) |
| Owner eligibility | EnemyOnly (1) |
| Health / attack / priority | 20 / 6 / 30 |
| Attack range / move cost / attack cost | 2 / 6 / 4 |
| Shape / choice / angle | RadialSquare (5) / Single (1) / Unrestricted (0) |
| Splash / percentage | Radial (3) / 50 |
| Traits | Empty |
| Shared maximum action cost | 6, supplied by `CharacterActionCostRules.Maximum` |

Unspecified settings retain the current Cactus unit values: Regular, 1×1 footprint, physical defense 0, move range 3, delay 50, voluntary movement/basic attacks enabled, no forced-movement immunity. Owner eligibility is explicitly EnemyOnly, not inherited from Cactus. The unit's display name belongs to the future UnitVisual entry, not the Core definition.

### Wall behavior and Cannon rules

Preserve the current Cactus SO's explicit `RequireClear / FromImpact` wall policy. Its Mortar animation is a presentation choice; it does **not** imply `IgnoreWalls` in Core. The user was told about the current wall setting and approved matching Cactus. If future design requires firing over walls, change the unit's delivery policy explicitly and verify it; do not infer it from animation kind or entity name.

Cannon still allows empty/friendly direct targets, includes friendly splash damage, excludes its own occupied cell from damage, and rejects direct aims at the actor and eight adjacent cells. Range remains 2. No ThornField trait is authored, so this unit must not create thorn fields.

## View integration remaining

The saved gameplay assets are intentionally **not activated in the live catalog**. No artwork, prefab, scene, starting roster, shop, enemy loadout, or availability route was selected in this Core task.

1. Reuse the delivered Unit SO and targeting SO; do not duplicate their gameplay values in a View component or another Unit SO.
2. Register the targeting SO in `InGameGroup/Targeting/TargetingMethodDataTable.asset` and the Unit SO in `InGameGroup/Unit/UnitDataTable.asset` when the corresponding valid UnitVisual entry is ready.
3. Create a UnitVisual keyed by `Unit_UndeadCannonCart`, with the display name `언데드 마인카트`, selected enemy prefab and required icons. Register it in `ViewGroup/UnitVisual/UnitVisualDataTable.asset`.
4. Reuse the existing Mortar presentation path (`CombatAttack_CactusCannon.asset`, `CombatMortarAnimation`, `CatusCannonWeaponView`) if the selected prefab satisfies its weapon/pivot requirements. A new unit-specific presentation asset is needed only if its visual settings differ. The existing ThornField visual system must continue to consume actual Core thorn state; do not spawn thorns from the Mortar animation.
5. Choose the intended enemy loadout/stage spawn entry with the content owner. This EnemyOnly unit does not belong in the player roster or shop acquisition pool. A saved Unit SO alone does not place an enemy in a battle.
6. After activation, run `DataBaseAssetTests` and verify real scene playback, targeting/preview, projectile arrival, damage display, and no thorns in Unity.

To tune splash damage, select **TargetingMethod_UndeadCannonCart.asset → Splash Percent** in the Inspector and enter a whole-number percentage. The Unit SO already references it. Rebuild/re-enter battle after changing authoring data; existing runtime definitions are immutable snapshots, not live Inspector bindings.

## Design review

- **SSOT:** unit statistics belong to the Unit SO; targeting shape and percentage belong to its dedicated targeting SO; maximum action cost remains the existing common rule; runtime facts remain Core-owned.
- **SOLID:** this is registration/authoring through existing contracts. No new responsibility, calculator branch, subclass, broad service interface or concrete dependency is introduced. Existing substitution and interface contracts are unchanged.
- **JMH/FW:** existing SO-to-immutable-data conversion and Core execution contracts are reused. No module registration, lifecycle, event subscription, Buffer ownership or Framework change is introduced.
- **Change independence:** later percentage changes touch only the cart's targeting asset. Cactus retains its own method. View assembly uses keys and presentation references; it does not implement damage rules. Shared target admission/damage contracts remain the deliberate common boundary.

## Verification

Initial native Unity **6000.0.73f1** verification, EditMode, 2026-10-16 (before the subsequent enemy-ownership clarification):

| Fixture | Passed / total |
|---|---|
| `UndeadCannonCartTests` | 7 / 7 |
| `AttackTargetAdmissionTests` | 47 / 47 |
| `TargetingSplashDamageTests` | 29 / 29 |
| `CactusLobbedAttackTests` | 4 / 4 |
| `TargetingAngleAssetTests` | 9 / 9 |
| Mandatory `DataBaseAssetTests` | 30 / 33 |
| Combined | **126 passed, 3 failed / 129** |

The new tests load the actual saved assets, exercise UnitSpec conversion and the real command calculator, compare preview with applied health, verify attack cost4, friendly splash damage, no thorns, near-cell rejection and range2. A cloned targeting SO is edited through `SerializedObject`: 30% produces1 additional damage, the initial50% produces3, and100% produces6; primary damage stays6 and both saved methods remain unchanged. The command fixture uses equal Cannon categories and no other damage modifiers. This is Core/asset acceptance, not View playback or a full live-catalog/battle-bootstrap acceptance test.

The three common DB failures match previously recorded failures in the unchanged stage/material configuration:

1. `EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage1101 terrain index5 expects(7,3), actual(6,3).
2. `EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage2101 headquarters Y expects8, actual0.
3. `LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: `Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat` is absent.

No unrelated stage/material or test expectation was changed. This is not an all-green common DB suite. These failures also appear in the earlier Dynamite Hammer DU1 evidence; this session did not rerun a pre-change common-DB baseline. Existing tracked product/config hash comparison confirms only this task's enum addition remains changed against its starting snapshot, after restoring the verified Unity-generated FMOD scan-path rewrite. Earlier uncommitted work is preserved.

Evidence (local, git-ignored): [final XML](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/green-01.xml), [final log](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/green-01.log), [filter](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/green-01-filter.txt). Exit code2 reflects the three DB failures. No compilation errors occurred in this run; three existing compiler warnings were emitted (CS8632 in `CombatCommandEffects` and `CombatCommandTestData`, CS0105 in `LobbyDepartureIntegrationTests`).

Test-first evidence: the initial test compilation exposed a missing test-only `using`, which was corrected. [RED XML](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/red-02.xml) then shows7 expected failures from the missing new identity/assets before implementation. An independent read-only reviewer inspected the scoped assets, identity, tests and handoff under `requesting-code-review` and found no actionable defects. The reviewer read the native result and did not execute Unity. No visual playback acceptance or live-catalog activation is claimed by this handoff.

### Enemy-ownership follow-up — 2026-10-16

The user clarified that the cart is an enemy unit. Changed only its saved `ownerEligibility` from PlayerOnly(0) to EnemyOnly(1); retained key25, both asset GUIDs, targeting method8 and all other gameplay values. Cactus remains the distinct player-only key16 with its original trait and method4.

The existing attack fixture now uses an unowned Enemy actor, a Player primary target and an Enemy additional target, with the established `Automatic / Advance / command number0` enemy attack contract. This verifies real enemy attack calculation and friendly splash without implying that an AI decision, enemy loadout or scene spawn has been integrated. The initial fixture conversion exposed the wrong player command context, which was corrected without changing production code. [Enemy RED](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/enemy-red-02.xml) then had6 passes and exactly1 expected failure: saved owner eligibility was PlayerOnly instead of EnemyOnly.

Fresh native Unity validation after the asset correction: **UndeadCannonCartTests7/7 pass; DataBaseAssetTests30/33 pass; combined37/40 pass**, exit2. The same three stage/material failures listed above remain, with no new failure. See [enemy final XML](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/enemy-green.xml) and [log](../../../.superpowers/sdd/2026-10-16-undead-cannon-cart/enemy-green.log). View handoff now specifies the display name `언데드 마인카트` and enemy loadout/stage spawn integration. The original code/asset name remains `UndeadCannonCart`; no duplicate unit or display asset was added.

This document is prepared for the View programmer; it has not been sent externally. No commit or push is performed by Codex.
