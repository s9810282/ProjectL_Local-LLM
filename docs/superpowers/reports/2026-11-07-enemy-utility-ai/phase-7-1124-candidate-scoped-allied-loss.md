# Phase 7 stage 1124 candidate-scoped allied loss

Run date 2026-11-08. Checkout `feature/Cheon/Core/NewCombatSytem` at `9d8c9ce45` with the existing Phase 7 worktree changes.

## Core change

`CombatCommandEffects.AssessmentCoverageSteps` continues reporting the same assessment gaps. It now signals separately when the effect needs a battle-wide allied damage bound. Regeneration resolves healing at the turn boundary. Leap affects the route, whose exposure is already evaluated by movement admission and arrival coverage. Knockback enters the bound only for an attack from its owner. Other unclassified effects, unresolved reactions, artifacts and the existing unbounded arrival path retain conservative coverage.

`CombatActionAssessment` consumes that separate signal. Direct allied damage estimates, route contact, actual legality, preview certainty and utility weights retain their existing contracts. The change touches the shared effect coverage scan, its assessment caller and one Core regression fixture. Framework, Job, View and the stage assets are outside this change. Understanding the fix requires the effect trigger, the action candidate and the existing arrival bound. Validation covers the related Core selection and assessment, integration preparation and publication, and the three Unity comparisons below.

## Regression and Unity comparison

The two new attack and move cases failed before the implementation with 2,000 permille against an expected zero. After implementation, the focused Core run passed 104/104, including the new tests and the existing A8/X8/H8 cases. The focused CoreIntegration run passed 53/53. Both runs had zero skips.

Three Unity 6000.0.73f1 Editor runs reused the previous 1124 seed, authored environment, five-enemy setup and scripted player input. The adjacent comparison placed the first Demon at (4,3) and Main at (4,2) through the copied runtime definition used previously. The result counts below are accepted enemy unit publications, excluding the side-end and its automatic hazards.

| Scenario | Before | After |
|---|---|---|
| Authored, no player move, sides 1 and 2 | 0 / 0 | 4 moves / 3 moves |
| Authored, scripted player move before each end turn | 0 / 0 | 4 moves / 4 moves |
| Adjacent attack diagnostic, one side | 0 | 4 moves |

The initial adjacent attack was admitted with a 0-permille possible allied loss bound, down from 5,000. Its score improved from −15,226 to −226 against the same Hold score of −466. It remained eligible. The Demon selected the move to (1,3), which scored 0 and published successfully. The adjacent side had no attack publication. The 240-permille opponent damage estimate was still coarse due to the retained coverage gap.

The authored movement runs both accepted (6,2) → (4,2) and the path request from (4,2) to (3,4). The original tornado relocated Main to (0,6) in the before run and (8,2) in the after run. The first enemy side's actions had changed the board. Both moves were presented. All five measured enemy sides completed and returned to player input with presentation caught up. The three Editor processes exited 0.

The two sides in a run share the same starting settings and scripted commands. The second side's resulting board depends on the actions actually selected in the first side. These traces measure behavior and publication. CPU and allocation gates were outside this rerun.

## Evidence and restore

The before traces are under `Logs/EnemyAi/phase-7-1124-decision-trace/`. New raw decision and publication traces and the Red/Green NUnit XML are under `Logs/EnemyAi/phase-7-1124-candidate-scope/`. Both stage asset SHA-256 hashes match their entry values. The diagnostic wrapper and trace scripts were removed, the factory and session sources were restored, and the post-fix manifest matched all 121 changed C# hashes. Compared with the prior manifest, the only C# changes are the two Core sources and the Core regression fixture. Immediate Advance remains the default game route.

