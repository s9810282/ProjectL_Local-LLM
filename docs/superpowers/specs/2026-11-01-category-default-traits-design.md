# Default traits by combat category

**Status** — Design for review, 2026-11-01.

## Gameplay contract

A unit receives the default trait configured for its current combat category. Initially Rider grants **Leap** and Heavy grants **Steadfast Will**. Main, Cannon and Boss have no default. The rule covers both sides, including units entering battle after setup. A Main that successfully specializes gains the selected category's benefit immediately, including in roster queries before the next battle. Main identity, growth steps and weapon still come from their existing owners.

These are ordinary traits. A Heavy with Steadfast Will can acquire Leap through an eligible growth offer. A unit already possessing Leap cannot receive a new Acquire offer for Leap. The category benefit costs no growth step. Leap uses its existing traversal and landing rules. Steadfast Will uses its existing knockback immunity and still permits pulling.

## Authoring and alternatives

Use the existing singleton category configuration, `Assets/ProjectL/DataBaseSO/InGameGroup/Category/Item/CategoryConfig_Default.asset`. Add one optional reference per category to an existing `TraitGrowthDefinitionDataSO`. An empty assignment grants nothing. Assign the existing Leap and Steadfast Will assets. The definition asset owns the normal trait and its parameters. Export its key through `CategoryConfigDataSO.GetData()` and carry only immutable Core values beyond the DB adapter. The existing definition table supplies the normal spec. Pool membership controls growth offers independently of the category assignment.

**Designer workflow.** To replace Rider's default, change its reference once and validate the content. On the next definition load, Rider units and Rider-specialized Mains receive the replacement. A previous default remains only where a unit's own definition or growth also supplies it. This works for registered, implemented traits compatible with the affected units, including their targeting methods and other traits. A new effect still needs its own normal implementation and registration. Configuration changes take effect at normal definition-load boundaries. A running battle uses the definitions it captured.

| Approach considered | Decision and trade-off |
| --- | --- |
| Assign the trait in each unit SO | Already works for individual exceptions. New units can miss the assignment and a Main can specialize after its unit definition was loaded. |
| Configure a category reference and compose existing traits | Selected. One editable mapping covers native units and specialization. Effective trait resolution becomes a shared dependency of roster, growth and combat. |
| Force the effect in combat code or add a separately stored equipment slot | Movement and knockback branches would need matching ownership and growth checks. A stored slot would add a second mutable fact for a benefit that follows directly from the category. |

## Effective ownership

Resolve from the raw unit definition, its effective combat category (`UnitSpec.CombatCategoryKey` or the existing specialization resolver), and any owned growth record. Use one pure composition policy for owned and unowned units. The unowned case has no growth history. Always start from the raw definition so an old category default cannot survive a category change by being mistaken for a native trait.

| Source | Composition rule |
| --- | --- |
| Unit-authored traits | Preserve their specs and parameters. Existing duplicate checks within a unit still apply. |
| Category default | Add the catalog's normal spec only if its key is absent. An explicitly authored same-key unit spec takes precedence. |
| Recorded growth | Preserve step order and all existing eligibility checks. An otherwise valid historical Acquire that overlaps only the category grant still consumes its original step and provides one copy. Reject duplicate Acquire records and conflicting native acquisitions as before. A valid Enhance replaces the matching normal spec with the catalog's enhanced spec once. |

Keep the distinction between these sources local to composition. Publish one effective entry per key through the existing trait contracts. A grant causes no Buffer write, acquisition record, refund or extra activation. A previously earned trait survives a later change of default. Future Acquire offers exclude effective keys. Enhance offers still require an eligible step, pool membership and a configured enhanced form. Neither initial default has an enhanced form.

**Existing content.** Heavy `Unit_ShotGun.asset` already explicitly carries Steadfast Will. Preserve that authored trait and verify one effective entry. If Heavy's default changes later, ShotGun will retain its explicit Steadfast Will. Removing that unit-specific assignment would be a separate content decision.

**Compatibility across content revisions.** An existing Acquire record supplies its own ownership when the default changes. An Enhance record requires a normal source. If a future default has an enhanced form and a saved run enhanced it solely through its category, replacing that default can make the history invalid. At definition reload, reject the incompatible record before publishing any state and identify the unit, category and trait. Supporting that content revision for affected runs requires an explicit migration. Verify this case with a fixture alongside a successful reload where a native or acquired source survives. The initial two defaults cannot produce this case.

## Integration boundaries

| Existing owner and observed behavior | Required change |
| --- | --- |
| `UnitSpecAdapter` reads native traits. `CategoryConfigDataSO` currently exports movement and growth settings. | Pass the optional mapping through the category configuration and resolve its keys against registered definitions. Keep raw unit specs available. Legacy fixtures with omitted assignments get no defaults. A configured key missing from the definition table is an authoring error. |
| `OwnedUnitSpecResolver.Resolve` fast-returns for empty growth and otherwise rejects acquiring any existing key. `CharacterStatsQueryService.Calculate` can select a disabled catalog when no owned character has growth. | Apply composition even on a first-level unit. Supply required definitions independently of whether anyone has leveled up. Use the effective traits for baseline stats and trait keys immediately after specialization. |
| `BattlePreparationCalculator` creates an unowned unit catalog and conditionally builds owned definitions. `CombatInputAssembler` rebuilds profiles via `BuildProfiles`. `BattleDefinitionAdapter.ReadUnits` discovers summoned dependencies by walking traits. | Use equivalent effective definitions for initial placement, owned and unowned attack profiles, enemy spawns, summons, command execution and previews. Include dependencies introduced by an effective trait, including on a specialized Main, before finalizing the battle catalog. Reuse existing trait factories and captured content. |
| `TraitGrowthRules.Candidates` computes current keys. `TraitGrowthCatalogValidator` checks reachable choices and specialization using base profiles. `CharacterRosterService.MainSpecialization` prepares and revalidates before installation. | Use the same effective specs in eligibility, hypothetical attack profiles and exhaustive validation. Check content for enemy-only units even though they have no growth path. Include the mapping and its relevant definition values in specialization source comparison. A stale offer or prepared operation must pass the existing revalidation before it can write. |

Keep the category mapping separate from category combat rules such as Cannon's Safe Bombardment. The disabled blanket Heavy immunity in `KnockbackImmunityPolicy` stays disabled. Runtime effect code continues to depend on trait behavior. Buffer owns durable selections and specialization, and View consumes published effective facts. The design needs no new service, Job, mutable category slot or View lookup into InGame data.

## Acceptance and verification

| Scenario | Observable result |
| --- | --- |
| Native Rider and Heavy, both factions, no growth | Each has its configured trait from first use. Initial placement, later entry and attack/movement previews agree with execution. An unaffected unit has its original behavior. |
| Main specializes to Rider or Heavy | Immediately published roster traits and the next battle agree. Base identity remains Main. Cannon specialization does not inherit the Rider or Heavy default. |
| Unit-authored or previously acquired overlap | ShotGun has one Steadfast Will. A Main that acquired Leap before specializing to Rider has one Leap and the same recorded selection. An unrelated learned trait coexists. Repeated queries and battle entry never add copies. |
| Growth selection | Rider cannot draw Leap as an Acquire choice, but another eligible category can. Validate the production pools still supply the required number of choices after filtering and that a rejected choice leaves the record intact. |
| Designer replaces or clears the Rider reference in a fixture | Only the mapping changes. Test a compatible parameterized replacement and verify its values in roster, specialization and combat. Clearing it removes only the category contribution. Reject missing, retired, pending or incompatible definitions before publishing partial state. |
| Prepare and apply cross a configuration change | Specialization follows the existing stale-definition failure contract. Previously offered growth cannot install an ineligible choice. Existing active combat state continues to use its captured definitions. |

Implementation verification includes the existing Leap, Steadfast Will, immunity, growth, specialization and battle publication tests, production `DataBaseAssetTests`, then the Core and CoreIntegration EditMode suites via the approved Unity execution path. These are acceptance criteria, not execution results.

## Change scope

The first implementation connects DB authoring, definition composition and their existing consumers. Afterward, changing one category's default to a compatible registered trait edits one category asset. The designer needs the replacement's requirements and the affected units' compatibility. Verification covers content validation, effective ownership, growth offers and the replacement's behavior. Changing how Leap moves continues to belong to the movement and trait code. The implementation plan may choose internal helper names and parameter plumbing while preserving the ownership and ordering above.
