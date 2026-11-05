# Heavy protection — Core to View handoff

Status: Core runtime, settled-area query, and Shotgun content implemented through Phase 3. Local handoff; not sent externally. View adoption and interactive field verification are separate acceptance work.

Sources: accepted design (Git history: `2026-10-16-heavy-ally-protection-design.md`), implementation and verification record (Git history: `2026-10-16-heavy-ally-protection-plan.md#10-phase-3-execution-record--2026-10-16`), and [shared boundary record](2026-09-24-view-core-data-boundary-handoff.md#542-heavy-protection-query-and-shotgun-content-2026-10-16-phase-3).

## 1. Public query and access

Use the existing InGame/Core group access path:

```csharp
IHeavyProtectionReadSink query = inGame.CoreGroup.HeavyProtection;
if (query.TryReadArea(selectedEntityId, out HeavyProtectionAreaSnapshot area))
{
    // 반환된 칸을 표시합니다. 빈 목록도 유효한 조회 결과입니다.
    foreach (CellCoord cell in area.Cells)
    {
        // 기존 필드 표시 계층에 칸을 전달합니다.
    }
}
else
{
    // 이전 보호 범위 표시를 지웁니다.
}
```

The example illustrates access only; this change does not add a renderer or highlighting controller. `IHeavyProtectionReadSink` is in `ProjectL.Systems.RuntimeSystem.BufferSystems.Combat`; `EntityId` and `HeavyProtectionAreaSnapshot` are in the existing Core Contracts `Ids` and `Snapshots` namespaces. The interface extends `IServiceModule` and exposes exactly:

```csharp
bool TryReadArea(EntityId provider, out HeavyProtectionAreaSnapshot area);
```

The immutable result exposes `Provider`, `Owner`, `DamageReductionPercent`, and copied `IReadOnlyList<CellCoord> Cells`. Its constructor is `(EntityId provider, Owner owner, int damageReductionPercent, IReadOnlyList<CellCoord> cells)`. Retaining a result does not retain a live Buffer view: later movement, death, or battle exit does not change its cells.

`CombatSystemFactory` exports one `HeavyProtectionQueryService` under `IHeavyProtectionReadSink` and registers it as a module for the existing PostBind pass. `InGameSystemServiceKey.HeavyProtection` and `CoreGroup.HeavyProtection` resolve that instance inside the Core service store. Child services are not published at the root. View should use its existing Core group reference; no additional global lookup or service registration is needed in View.

## 2. Availability, timing, and failure contract

| Result | Meaning | Consumer action |
|---|---|---|
| `false` and `area == null` | No active battle, missing/dead/non-field provider, or provider is not Heavy | Clear any previous protection display |
| `true` with empty `Cells` | A valid living field Heavy whose authored area is entirely outside the board | Treat as a successful response with nothing to highlight |
| `true` with cells | Current settled world cells for a valid Heavy | Display exactly the supplied cells |
| Exception | Query before PostBind, broken Heavy authoring/runtime invariant, or coordinate overflow | Preserve the existing runtime fault handling; do not convert the exception into an unavailable result |

Call on the main thread after the existing Buffer publication has completed. The service reads current settled combat/character state; it does not write commands, previews, results, counters, or gameplay data, and it does not read DB or attack profiles during a query. It stores only the two Buffer read interfaces after PostBind and calculates a fresh response for each call.

A result describes current logical Core state. It is not tied to a presentation replay step and contains no historical battle identifier. Refresh when the selected provider changes and after relevant settled movement/return, health/death, or battle-state publication. Coordinate refresh with existing playback completion so the shown pose and queried settled state agree. Clear on deselection, unavailable response, battle cleanup, scene exit, and before reusing a selected identity in a new battle. Do not retain a response as an indefinitely current area or poll once per cell/per frame.

Speculative move-preview areas, historical replay areas, recipient-membership queries, status icons, and View highlighting are not provided by this change. View does not read Unit/InGame DB to reconstruct protection or repeat translation, clipping, owner, rotation, or damage rules.

## 3. Coordinate and gameplay meaning

The unit SO owns a direct list of signed relative `(x,y)` offsets. Core translates each by the provider anchor and clips to board bounds. Offsets are literal board coordinates: there is no footprint rotation, facing rotation, owner mirroring, wall occlusion, or default implicit shape. View must not rotate or mirror the returned world cells. The area response describes the provider's authored area, not a list of currently protected recipients.

Only living same-owner field characters receive protection. Any occupied recipient cell may intersect the area. The provider excludes its own protection by identity, but another Heavy can protect it. Headquarters, train, and other structures are excluded. Overlap contributes once at the common category rate; the lowest eligible EntityId provides deterministic provenance.

Protection applies only to attack damage, including ordinary direct, splash, piercing, and reaction attacks. Thorn, reflection, terrain/environment damage, and attack-triggered obstacle explosions are excluded. Existing Heavy attack/roar knockback immunity remains separate and does not become immunity to every forced movement source.

An ordinary/reaction attack captures protection once before its first damage. Provider death during that attack does not revoke already captured protection. Each Kill Chain addition captures the current projected world again; each Frenzy wave captures once for the entire wave and refreshes for the next wave. Preview reuses the ordinary capture path without publishing gameplay changes. Reduction is `floor((long)incomingDamage * percent / 100)` after splash/category/physical-defense stages and before terrain received percentages and held-defense/health settlement. At 50%, incoming values `0,1,5,6` have reductions `0,0,2,3`.

## 4. Shotgun content

The existing Shotgun key 11, asset GUID `39a9cd647c4c4d35af33595bbea3064e`, table membership, and View references are preserved.

| Field | Delivered value |
|---|---|
| Category | Heavy / 30 |
| Health / attack | 40 / 6 |
| Priority / attack range | 30 / 2 |
| Move cost / attack cost | 4 / 2 |
| Protection offsets | `(-1,1),(0,1),(1,1),(-1,2),(0,2),(1,2)` |
| Common protection rate | Category configuration, currently 50% |
| Preserved movement / timeline / defense | Move range 3, delay 10, physical defense 0 |
| Preserved identity / behavior | Regular 1×1, PlayerOnly, voluntary movement/basic attack enabled, generic forced-movement immunity disabled |
| Preserved attack | Shared targeting method 2: Fan / Direction / Front, splash None / 0; existing wall policy RequireClear / FromImpact |
| Preserved distance trait | `[150,50]` |

At an interior anchor `(3,2)`, the six returned cells are `(2,3),(3,3),(4,3),(2,4),(3,4),(4,4)`: three columns and two rows on the literal `+Y` side. This matches the confirmed six yellow cells in the character design. The shared targeting asset and Gravekeeper are unchanged.

## 5. Removed Heavy contracts

`DirectionalArmorCategoryRuleSpec` and the directional self-armor implementation were retired. Construction is now `HeavyProtectionCategoryRuleSpec(percent)`. `CategorySnapshot.CreateHeavy(percent, offsets)` exposes copied offsets and the common percentage. Heavy `DefenseFacing`, Heavy `MovedThisTurn`, and category `heavyInitialFacing` authoring are removed. The old numeric rule slot 2 and activation slot 20 remain reserved; the new Heavy protection rule uses 4. No new facing activation is produced. Unrelated Rider refund and Focus movement state remain.

Systems reuses the Domain area calculation through the narrow pure Application `HeavyProtectionAreaQuery.Calculate(...)` facade. No assembly reference to Domain was added to Systems/View. The facade is a Core assembly adaptation; View's contract is the settled-state read sink above.

## 6. Verification and View acceptance

Phase 3 `phase3-regression.xml` ran 409 cases: 406 passed and the same three DB asset failures already recorded in Phases 1–2. The failures concern stage 1101 environment coordinates, stage 2101 center rail data, and the missing Line material; none is a Heavy/Shotgun failure. The record covers query lifecycle/availability, clipped empty versus missing, immutable responses, source movement/death/cleanup, actual service registration/PostBind, no query publication, and attack-capture/query parity across owners, rotations, and board edges. Production Shotgun/catalog tests and native SerializedObject save/import plus coordinate editing on an independent SO copy pass. Phase 2's broad Core/Framework baseline is retained separately; it is not relabeled as a Phase 3 rerun.

Local evidence is under `.superpowers/sdd/2026-10-16-heavy-ally-protection/`: `task31-red.log`, `task32-red-content.xml`, `task32-migrate.log`, and `phase3-regression.xml`/`.log`. The implementation plan carries final review, any follow-up evidence, and exact limitations.

Interactive Inspector manipulation and protection rendering on the actual field have not been observed in this session. View acceptance must confirm that the six cells occupy the intended three-wide/two-deep `+Y` side in the project's real field mapping, that existing visual unit references still work, that refresh occurs after settled movement/playback, and that selection/death/exit clears stale highlighting. No new View display code was introduced to claim this check passed.
