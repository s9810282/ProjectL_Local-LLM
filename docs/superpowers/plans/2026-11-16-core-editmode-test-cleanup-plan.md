# Core EditMode Test Cleanup Implementation Plan

The baseline at `4730de720` selected 7,095 cases across Core and CoreIntegration. Unity took 983.65 seconds, with 37 failures and 14 skips. The user approved removing the duplicate and obsolete production checks, deleting the 23 failing View cases, correcting two Core assertions, and retaining representative inputs for the default run.

## Design

Keep test-owned behavior checks and production asset-reference checks. Asset tuning changes should no longer require copying rank numbers, recovery options, or test stages into the Core tests. Delete only the failing View cases, including the two Deck fixtures whose cases all fail. Retain the remaining camera and structure cases.

The two obsolete Cannon production fixtures expected the live catalog to accept Taunt and Acceleration acquisitions from an earlier pool. The current history resolver rejects traits absent from the live pool. Removing these fixtures also removes coverage of that older compatibility assumption.

The default run keeps attack preview partition sizes 1, 2, and 31, both frozen AI tuning profiles, cache-on versus cache-off comparisons, and the Stage 1127 behavior checks. Use representative inputs in each matrix. Set `PROJECTL_ENEMY_AI_FULL_REGRESSION=1` before opening the Editor to exercise every input and the production-stage corpus. Select the explicit measurement or dump tests directly in the Test Runner window. Reuse the existing baseline files, test fixtures, and Unity test runner. Code changes are confined to tests.

## Phase 1

- [x] Remove the duplicate registry test and exactly 23 failing View cases. Remove paired Unity metadata for deleted files.
- [x] Drop obsolete production snapshots and duplicate recovery visits. Keep authored-spec, DB registration, reference resolution, and test-owned battle and recovery coverage.
- [x] Assert source-level attack-result construction and count only the AI selection event in the existing Core failures.
- [x] Apply representative inputs to the five expensive comparison methods and make the full production-stage measurements opt-in.
- [x] Review the diff and rerun the same Unity 6000.0.73f1 EditMode assembly selection in the existing checkout. Compare test-runner and wall-clock duration, failures, and intentional omissions to the baseline XML.

## Verification

| Metric | Baseline | Final default run |
| --- | ---: | ---: |
| Selected cases | 7,095 | 7,014 |
| Passed | 7,044 | 6,995 |
| Failed | 37 | 0 |
| Skipped | 14 | 19 |
| Test Runner duration | 951.45 s | 140.97 s |
| Wall-clock duration | 983.65 s | 172.85 s |

The 81 removed cases consist of 23 View failures, one duplicate registry check, 51 fixed rank-value snapshots, two old Cannon growth-history cases, two cases tied to stage 1101, and two duplicate production recovery visits. The five additional skips are opt-in production corpus or diagnostic cases. Both corrected Core tests and all three default preview partition cases passed. In the opt-in check, the full preview partition-31 matrix and the Stage 1127 diagnostic dump both passed.

The first verification attempt stopped at compilation because a remaining DB test still needed its adapter import. The final run used the restored import and the stronger production tuning-table assertion from independent review.

## Phase 2 — Preserve the remaining production contracts

Restore Shotgun's native Steadfast Will entry in its existing unit asset. Verify the real authored unit with a test-owned Heavy default replacement. Reuse the recovery definition adapter to verify the exact option order and effect kinds for stages 1301, 1302, 1303, and 2203. Read the registered growth SO data once and compare it with the effective owned trait resolved through the Core catalog for each available rank.

- [x] Add the production checks before restoring the Shotgun asset. Run the changed fixtures and confirm the Shotgun check fails for the missing native trait.
- [x] Restore the original managed-reference entry while keeping Shotgun's current authored settings.
- [x] Run both default EditMode assemblies and compare the results and elapsed time with the Phase 1 final run.

Before the asset change, the 26-case database fixture had one failure. Replacing Heavy's default left Shotgun with zero copies of Steadfast Will. After restoring the asset, the fixture passed in the full run. The recovery test checks option identity, order, and effect kind. The rank test checks the registered SO data against the resolved spec without fixed balance values. It checks all 20 registered trainable traits and their available ranks.

The final default run selected 7,016 cases, with 6,997 passed, zero failed, and 19 skipped. Test Runner duration was 141.14 seconds and wall-clock duration was 167.74 seconds. Phase 1 measured 140.97 and 172.85 seconds respectively. The authored rank-value check took 0.16 seconds. Unity's generated FMOD and font files were restored and verified against their pre-run bytes.

## Phase 3 — Isolate behavior from production tuning

Keep the previous test scenarios and use test-owned traits for publication, undo, expiry, and specialization behavior. Compare production recruitment price, skill uses, and trait authoring against their authored inputs. Keep registration, identity, targeting, and feature policy assertions in their existing tests.

- [x] Replace the live trait inputs in the four publication fixtures and the specialization replacement fixture.
- [x] Check authored price and skill use propagation. Remove balance snapshots from SoulEater, Piercing, terminal rank, and dormant authoring tests while preserving their structural contracts.
- [x] Run the default Unity Core and CoreIntegration assemblies on the same checkout and compare with the Phase 2 XML. Confirm that the chapter lane-change and Stage 1127 checks still execute.

Unity 6000.0.73f1 exited successfully. Core reported 5,896 passed and 11 skipped. CoreIntegration reported 1,101 passed and 8 skipped. There were zero failures and the 19 skipped cases matched Phase 2. The two production chapter fork checks and the three default Stage 1127 AI checks passed.

Test Runner duration was 99.26 seconds versus 141.14 seconds in Phase 2. Wall-clock duration was 123.62 seconds versus 167.74 seconds. These are single-run timings. The FMOD catalog and font hashes matched their pre-run files after restoration.
