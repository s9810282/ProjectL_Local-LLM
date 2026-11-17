# Pull Continuation — Phase 3 EditMode Results — 2026-11-14

## 1. Start here

Purpose: let a GPT-6.1 Sol session and a GPT-6 Astra session continue the same scoped work without depending on each other's chat memory. This file does not create chats, select models in the app, or authorize implementation.

Read in this order:

1. Applicable repository `AGENTS.md` instructions, including the user's current overrides.
2. [Behavior design](../specs/2026-11-14-pull-range-and-attack-continuation-design.md).
3. [Implementation plan and authoritative phase/model routing](../plans/2026-11-14-pull-range-and-attack-continuation-plan.md).
4. The current checkpoint in section 3 below, then the actual checkout and diff.

The design owns behavior and confirmed decisions. The plan owns task completion and validation evidence. This handoff owns session edit ownership and the checkout checkpoint. Do not maintain conflicting copies of those records in separate model-specific plans.

**Read revision 2, not a cached earlier plan:** the user's feature-change-locality guideline has been applied to design sections 6.1 and 8.1 and Phase 2. Use the existing `SelectionPreparation` and a single `SelectionSteps` body; do not implement the earlier separate basic-range geometry pass followed by full initial selection. The external guideline's applicable criteria are incorporated in those sections, so its original local file is not a prerequisite on another machine.

## 2. Confirmed behavior and critical limits

This is a summary; the design is authoritative.

- Effective basic range zero disables Pull, including its extra range. Return empty range / normal rejection; do not throw or fault combat. A later legal command must still work.
- After completed Pull movement, current shared rules decide whether a follow-up can begin. Expected gameplay refusal preserves movement/environment effects and action settlement, then skips the attack normally.
- A skipped attack does not emit attack-start bonuses or attack-associated effects, consume attack-only state, or activate attack-completed items.
- Successful Pull evaluates attack-start sources/recipients after arrival effects. A source that died during arrival grants no new attack-start bonus.
- Invalid configuration/state contracts are not silently converted to successful or skipped gameplay. This task prevents normal situations from entering that exceptional path; it does not introduce a whole-game recovery framework.
- The user authorized scoped Unity EditMode execution and the frozen reference correction for Phase 3. Commit, push, new test code, and MSBuild remain outside this work. Documents are English and code comments are Korean.
- Do not expand into ordinary movement cancellation, new View behavior, production content edits, or KillChain targeting/attribution changes.

## 3. Current transfer checkpoint

Snapshot date: 2026-11-14. Verify again on entry; this snapshot is not proof of current state.

| Field | Current value |
|---|---|
| Checkout | `D:\My_ProjectWork\Github_desktop\Project_L` |
| Branch | `feature/Cheon/Core/NewCombatSytem` |
| HEAD | `e4281a38e3e523f88d08d03dcfbe558c6bf9c8b5` |
| Edit owner | Released by the Phase 3 Codex verification session. |
| Authorization | The user authorized Phase 3 review, scoped Unity EditMode execution, and correction of the failing test expectations. |
| Plan progress | Phase 1 and Phase 2 source changes are present. Phase 3 compilation and all 972 selected EditMode checks passed. Custom scenario and scene acceptance remain open. |
| Feature code | Phase 1 zero-range behavior preserved. Phase 2 shares selection preparation/body, removes the Haboob-only rebuild, gates attack effects using `attackStarted`, and keeps one action settlement/finalization path. |
| Documents | The design, plan, and handoff are untracked in this checkout. A new worktree or clone needs the local documents and feature diff. |
| Static evidence | Phase 3 matched all four Phase 2 source hashes and ran scoped `git diff --check` successfully. Source tracing checked shared selection and delivery, settlement, and movement-only playback routing. |
| Compile / runtime / tests | Unity 6000.0.73f1 batch compilation succeeded. After updating the `pull-friendly` and `pull-tornado` preview reference rows, Core passed 885/885 and CoreIntegration passed 87/87. The four initial snapshot failures passed on rerun. Play Mode remains unverified. See the [Phase 3 verification record](../plans/2026-11-14-pull-range-and-attack-continuation-plan.md#phase-3-verification-record-2026-11-14). |
| Next action | Verify the outstanding custom combinations and scene playback listed in the plan. |

There are unrelated local FMOD, font, render-pipeline, and ProjectSettings changes. Do not reset, clean, stage, or include them in this work.

Against HEAD, the integrated feature-code diff is four files, 182 insertions and 92 deletions. Phase 2 received the earlier two-file Haboob fix plus Phase 1, then migrated that behavior into the shared continuation path. Source files were normalized to UTF-8/LF; compare changed hashes to the actual diff, not to an assumed commit. No commits or pushes were made.

Core paths in this table are under `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/`.

| File | SHA-256 at snapshot |
|---|---|
| `CombatAttackCalculator.cs` | `35898CB4EC5E52CAB0DBFB6DD55476BD6CF901D964C9EE0D721107984E07E8C7` |
| `PullAttackCalculator.cs` | `09BDF42B0191D541F5795D2636F4F58EF33B75E89C19048C1A33DF11FB7D655E` |
| `CombatAttackQueries.cs` | `6796C67B7B90C730FCCE71F102105CC85F8C462128E531EB68848195114D950A` |
| `AttackDeliveryExecutor.cs` | `2D2BB41B154E56C6FBEE8E349F97D0D442C7BA0FA8391DEE9774D25D5D106E1D` |

## 4. Receiving a session

1. Confirm the selected model in the app. The model recommendation is in the plan; do not claim a model switch occurred merely because this file names one.
2. Read the documents above. Inspect `git branch --show-current`, `git rev-parse HEAD`, `git status --short`, and the scoped feature diff.
3. Confirm the previous editing session is finished/released. A model-specific session may remain open, but it must not still be editing the same checkout. If ownership is active or stale and cannot be verified, ask before concurrent edits.
4. Check the user's latest instruction for the authorized phase. A handoff or a model change does not itself authorize the next phase.
5. Record the receiving session label and authorized phase in section 3 before editing. Use a chat link/ID only if actually known; do not invent one.
6. Prefer the same checkout for this sequential workflow. If using another worktree/clone, first verify that the three documents and all prerequisite local feature changes are present. Do not reset or recreate the earlier fix from chat memory. Resolve transfer before implementation.
7. Reconcile changed facts with the plan. Completed source work, static review, and owner-executed validation must remain separate statuses.
8. Apply the ownership/migration table in design section 8.1. A new helper must reuse or replace an identified existing responsibility; do not add parallel rules, state, completion paths, or feature registries. Review the actual call chain before claiming reuse.

This document is an operational convention, not an automatic filesystem lock. The user controls which session runs next.

## 5. Releasing a session

Before the phase report, update the plan only for work actually completed and replace section 3 with a fresh checkpoint containing:

- Session label/model and the phase it was authorized to perform.
- Checkout, branch, HEAD, changed feature files, and scoped diff summary; record relevant file hashes when work remains uncommitted.
- Code status, independent review findings/resolutions, static checks, and owner validation results with their exact source. Use `not run`/`unverified` for missing evidence.
- Any incomplete task, known failure, design deviation requiring a user decision, and the exact next task.
- For a moved responsibility, the old implementation removed and the callers now using its owner. For any wider edit or validation requirement, its actual dependency-based reason.
- Ownership `released` and the next recommended receiving session; do not imply that the next phase has been authorized.

If interrupted mid-phase, record the unfinished step and whether the current files are a partial migration. Phase 2 must not be presented as usable when its decision and effect gates are only partially connected. The receiving session continues from the checkpoint after inspecting code; it does not restart the audit or replay completed edits.

## 6. Ready-to-use starting prompts

These templates are for the user to submit in the intended session. Each implementation template authorizes only its named phase when submitted. Section 3 records the current progress.

### GPT-6.1 Sol — Phase 1

```text
Work in D:\My_ProjectWork\Github_desktop\Project_L.
Read docs/superpowers/handoffs/2026-11-14-pull-continuation-model-session-handoff.md,
then its linked design and plan. I approve the documented scope for Phase 1 and
authorize Phase 1 only. Inspect the current checkout/diff and preserve existing
local work. Implement effective-zero Pull range as normal unavailability without
exceptions or stopping combat. Follow the recorded test/compile/commit limits.
Update the plan and handoff checkpoint with evidence, release edit ownership,
and stop for my review. Do not begin Phase 2.
```

### GPT-6 Astra — Phase 2

```text
Work in D:\My_ProjectWork\Github_desktop\Project_L.
Read docs/superpowers/handoffs/2026-11-14-pull-continuation-model-session-handoff.md,
then its linked design and plan. I approve the documented scope for Phase 2 and
authorize Phase 2 only. Verify the Phase 1 handoff and current diff before editing;
do not silently treat missing validation as passed. Implement the common
continuation and attack-start boundary, including skipped attack effects and
confirmed post-arrival notification timing. Preserve public contracts and scope.
Follow the recorded test/compile/commit limits. Update the plan and checkpoint,
release ownership, and stop for my review. Do not begin Phase 3.
```

### GPT-6.1 Sol — Phase 3

```text
Work in D:\My_ProjectWork\Github_desktop\Project_L.
Read docs/superpowers/handoffs/2026-11-14-pull-continuation-model-session-handoff.md,
then its linked design and plan. Review the Phase 3 evidence against the current
integrated diff. Use recorded Unity results and rerun only the approved focused
checks as needed. Keep missing scenario evidence unverified. If a lifecycle or
fixture issue remains, report the trigger, expected behavior, observed result,
and unresolved decision. Update the records and stop.
```

### Resume or review without implementation authorization

```text
Read docs/superpowers/handoffs/2026-11-14-pull-continuation-model-session-handoff.md
in D:\My_ProjectWork\Github_desktop\Project_L and the linked design/plan.
Inspect current state and summarize the next unfinished task and verification gaps.
This is a read-only handoff check; do not begin implementation.
```
