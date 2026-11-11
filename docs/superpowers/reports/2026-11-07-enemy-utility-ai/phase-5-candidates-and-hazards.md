# Phase 5 bounded candidates, threat, and remaining hazards

The user authorized Tasks 5.1 and 5.2 on 2026-11-08. Work continued in the existing uncommitted checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46` on `feature/Cheon/Core/NewCombatSytem`. Phase 4 code and evidence were inherited. Phase 5 code and tests are confined to Core. The new candidate and threat paths are dormant in the legacy selector. Gameplay defaults to immediate Advance and Phase 6 is outside this delivery.

## Implementation and ownership

- **Candidate generation.** One decision context retains completed public visibility, hostile units and headquarters, walls, elevation and one threat field. The existing per-actor traversal indexes keep self-exclusion and Leap/landing semantics with the movement owner. Attack intents use the existing target-directed aim cursor and exact selection/delivery checks. The shortlist retains distinct aim or path identities and keeps both target families available when slots permit.
- **Movement.** One local reach traversal retains predecessors and entry exposure. Destination and Hold-relative escape ranking use the same hazard key: known terminal outcome, possible HP loss upper bound, known loss, then possible Bind duration. This places a known surviving endpoint before a possibly lethal endpoint. It reconstructs routes for retained endpoints only. Existing pursuit provides the long-distance route and mandatory taunt fallback. Taunt and escape reservations share the movement cap. If a configured cap leaves one slot, the taunt route takes precedence. A Phase 6 consumer must enforce the reported mandatory taunt before considering untagged moves or Hold.
- **Escape refinement.** The selected escape destination can receive one deterministic alternative search with a maximum of 256 expansions. Labels distinguish spent steps so a longer, safer prefix survives. Already-admitted improvements survive a cap reached before they are dequeued. The caller receives explicit truncation. Railway loss applies at the boundary endpoint. Published thorn damage is a raw entry estimate and actual protection still settles in the existing combat owner. Unclassified entry callbacks retain uncertainty.
- **Movement engagement estimate.** Single direct geometry checks the endpoint's public terrain range, choice angle, current Haboob and wall clipping. Complex direction, splash and piercing engagement remains a geometric estimate. It influences shortlist ordering only. Exact current-position attacks and mandatory pursuit use the existing admission and delivery rules.
- **Threat.** The field keeps the greatest estimated single-attack loss per board cell. It uses current player positions, enemy-visible geometry and intrinsic restrictions. The player's spent action ledger does not suppress the next opportunity estimate. Target-specific protection, future movement, hypothetical piercing order and pull extension remain partial. FuturePlayerChoice and HiddenEnvironment are retained.
- **Hazards.** Existing gimmick owners emit current visible Tornado contact, its complete warned sweep, current Haboob cells and known relocation timing. The forecast uses shared live order and geometry. Tornado immunity uses the existing behavior query. A possible throw preserves uncertain occupancy and possible Railway/Rockfall loss. Public fixed Haboob placement remains known if the rule has no alternative anchor. Random relocation produces future uncertainty. Prediction leaves RNG and actual settlement with the existing owners.

The read-only review found and the implementation corrected a cap boundary that could discard an already-admitted safer goal. The focused test proves the retained path when the search hits its cap. The review also led to shared public geometry and more accurate direct engagement ranking. The old Phase 4 placeholder hazard test was updated to assert the current registry order and concrete region/sweep facts. Its ordering and source-isolation purpose is preserved.

Changing a movement rule still requires route admission and movement regression. Changing a hazard query requires its own rule and the shared composition/terminal tests. Changing shortlist ranking uses the candidate intent and exposure contracts. The scorer can consume those contracts without reading a hazard's live settlement implementation.

## Focused verification

| Run | Selected | Passed | Failed | Purpose |
|---|---:|---:|---:|---|
| `phase-5-behavior-red2` | 26 | 4 | 22 | Six candidate API assertions, five placeholder threat assertions and eleven missing compound forecast behaviors |
| `phase-5-focused-third` | 56 | 52 | 4 | The four failures exposed the same two-sided wall visibility contract in threat geometry |
| `phase-5-focused-fourth` | 127 | 127 | 0 | Candidate, threat, combined hazard, previous forecast and live gimmick tests before the conservative rank fix |
| `phase-5-risk-order-contract-red` | 1 | 0 | 1 | The old rank treated a possibly lethal endpoint as an improvement over a known survivor |
| `phase-5-verified-focused` | 128 | 128 | 0 | Final focused cases and explicit capture on the same source |

The final focused selection includes 14 candidate cases, 6 threat cases, 14 combined hazard cases and 23 hazard forecast cases. Compiler-only attempts are separately retained. The initial fixture for the rank regression failed to establish a nonlethal damage premise. The direct exposure contract then reproduced the ranking error and passed after correction. The wall tests passed after validating walls from their canonical public source and keeping enemy-observed range and elevation. Compiler/setup results and fixture-premise failures do not count as behavior evidence.

## Full Core regression

| Assembly | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| ProjectL.Tests.Core | 5,200 | 5,168 | 25 | 7 |
| ProjectL.Tests.CoreIntegration | 1,011 | 962 | 47 | 2 |

All 72 failures match the Phase 0 full name, label and message. There are zero changed failure identities. The dedicated capture ran in the focused selection and was skipped as explicit during the full Core run. All 34 new behavioral cases passed. The frozen 86 attack Preview vectors, four movement Preview vectors, seven original routes and eleven decision fixture groups (500 action rows, 55 input hashes) passed. No frozen oracle was changed.

The `phase-5-verified-focused`, `phase-5-verified-core` and `phase-5-verified-integration` source manifests match all 111 changed C# files in this checkout. Compared with `phase-4-compatibility-integration`, Phase 5 touched 15 Core and test C# files. XML, exit status and manifests remain in those run folders. Exact failure comparison is in `Logs/EnemyAi/phase-5-verified-integration/regression-comparison.json`. After all Editors exited, the three Unity-generated FMOD/font changes matched the prior generated hashes and were backed up and restored. No source or test work was reset.

## Performance and bounded storage evidence

`Logs/EnemyAi/phase-5-verified-focused/` retains raw `phase-5-steps.csv`, `phase-5-totals.csv`, `phase-5-runtime.txt` and `phase-5-summary.json`. The N5 reference uses five enemies. Eight-enemy boards are frozen stress fixtures. Unity 6000.0.73f1 Windows batch mode, Stopwatch frequency 10,000,000 Hz, seed 1, ten repeated samples after the first per fixture. The threat field is built once before the candidate timing. Each row is an elapsed observation. The summed samples exclude CSV output and do not measure rendered frames.

| Warmed operation | Samples | p95 | p99 | Maximum | Above 1 ms |
|---|---:|---:|---:|---:|---:|
| Forecast Step | 2,600 | 0.0025 ms | 0.0081 ms | 0.0193 ms | 0 |
| Threat Step | 99,830 | 0.0020 ms | 0.0049 ms | 12.9292 ms | 1 |
| Candidate Step | 132,670 | 0.0079 ms | 0.0110 ms | 3.1230 ms | 1 |
| Forecast lookup on retained path | 240 | 0.0112 ms | 0.0166 ms | 0.0198 ms | 0 |
| Threat lookup | 240 | 0.0006 ms | 0.0009 ms | 0.0010 ms | 0 |

| Fixture | Board cells | Hazard facts | Threat cells | Attacks / moves | Local reach nodes | Candidate summed p95 |
|---|---:|---:|---:|---:|---:|---:|
| N5 | 256 | 0 | 50 | 2 / 4 | 11 | 0.3262 ms |
| W8 | 1,024 | 0 | 64 | 0 / 4 | 14 | 24.8484 ms |
| U8 | 1,024 | 0 | 64 | 0 / 4 | 14 | 7.9080 ms |
| A8 | 256 | 0 | 50 | 1 / 4 | 11 | 0.4473 ms |
| X8 | 256 | 39 | 50 | 2 / 4 | 11 | 0.4642 ms |
| H8 | 256 | 25 | 38 | 0 / 4 | 11 | 0.6416 ms |

Local reach counts are separate from existing long-range pursuit work. The timed W8/U8 candidates can include that fallback. The raw overrun rows are W8 sample 10 Candidate Step (3.1230 ms, CSV line 142183) and U8 sample 1 Threat Step (12.9292 ms, line 156184). First X8 forecast work also exceeded 1 ms twice (1.7529 and 1.0063 ms, lines 224386–224387). The first H8 forecast maximum was 0.9502 ms. The previous focused capture also observed one warmed outlier each in threat and candidate work (3.0767 and 3.1042 ms). This harness measures elapsed Step boundaries and leaves runtime cause unresolved. The corpus used zero full Previews. The independent threat test also checked for zero real attack projection setup. The shortlists stayed within four attacks and four moves. The measured fixtures spent zero optional refinement expansions. Dedicated fixtures verified the refinement cap, truncated result and an improved legal route. The threat map stores at most one entry per board cell. Exact byte counts are unavailable because the 4 KiB Mono allocation probe returned zero.

**G-atom remains unmet.** Task 2.0d and live frame/user Flow acceptance remain open. For activation, the measured overruns still need an evidenced disposition. The allocation result stays unavailable. The new candidate path is available for future integration under the approved Phase 3 development versus activation distinction. Phase 3 user compilation/playback and F1–F8 remain pending. Stop at Phase 5 and continue from this report only when the user authorizes Phase 6.

