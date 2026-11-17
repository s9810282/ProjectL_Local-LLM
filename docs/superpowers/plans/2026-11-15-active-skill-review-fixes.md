# Active Skill Review Corrections

> **For agentic workers:** Use `superpowers:executing-plans` for this corrective phase. The user's explicit instructions override skill defaults about tests, commits, and approval.

**Goal:** Correct the reviewed active-skill defects without changing ordinary attack rules or Buffer ownership.

**Architecture:** Keep attack completion in the existing delivery executor. Keep skill/trait compatibility in the skill definition and pass the actual optional skill binding through existing preparation and growth boundaries. Reuse the existing contact ledger for projectile range effects.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core and Systems assemblies.

## Approved decisions / specification

- The user approved rejecting Powder Blossom with Impact Bolt during battle preparation and growth candidate selection.
- Projectile range effects and enemy counting must exclude candidates beyond the projectile's stopping limit.
- The user approved refreshing the enemy count immediately before each hit: previous original enemy contacts plus currently living future contacts within the remaining encounter budget. A destroyed barrel may remove future candidates; only later hits see that change. Earlier damage is never recomputed from the final path.
- The user explicitly authorized committing and pushing these corrections on 2026-11-15. Existing unrelated asset/settings edits must be excluded.

## Approach

Reuse the existing owners rather than adding a skill service or duplicating state. Do not infer a configured skill from the unit key: use the actual `PlayerSkillDefinition`. Reject the incompatible trait at each input boundary using one predicate, including effective category defaults and restored growth. Bound range effects from actual original contacts, not potential contacts beyond the stopping point.

A second combat simulation or fixed-point loop is rejected: exact-count bonuses can change barrel destruction and make the implied final count oscillate. The damage-count timing must be explicit.

## Tasks (one corrective phase)

- [x] Move Intimidation completion to the shared attack executor; preserve ordinary/reaction order and ordinary-action-only item completion.
- [x] Add one Powder Blossom / Impact Bolt compatibility rule; connect SO/catalog validation, effective battle preparation, growth candidates/choice/history, and specialization/path validation.
- [x] Limit projectile range effects to actual contacts and implement the user's enemy-count timing decision.
- [x] Inspect the final diff and all changed call sites; obtain an independent review; check whitespace, encoding, and preservation of unrelated changes.

**Authorized delivery:** Commit only this task's changes and push the current branch, then verify local/remote commit identity. The actual publication result belongs to the delivery message after push; this document records the reviewed code before publication.

## Verification scope and limits

Do not add or modify test code, or run Unity/MSBuild, under the supplied verification restrictions. Static inspection is not a substitute for compilation or runtime acceptance. Report compilation and runtime verification as unconfirmed.

User-side verification must cover:

- Intimidation application/expiry for Magic Bullet and each Powder Blossom burst; ordinary/reaction/kill-chain regression and forecast input immutability.
- Authored/native, acquired, restored, and specialization-default Impact Bolt rejection for a configured Powder Blossom main; unaffected non-skill units and other legal combinations.
- Growth offers, stale offer choice, restored history, and catalog future-path validation agreeing on eligible traits.
- Judgment's first enemy/ally/structure, zero budget, budget exhaustion across bends, multi-cell deduplication, and barrel explosions skipping dead later candidates.
- Lava Cannon and Focused Bombardment respecting the approved projectile boundary/timing; ordinary attacks retaining their existing captured-range semantics.

## Change locality

Compatibility changes belong to the skill contract; the DB, roster, and battle boundaries only supply current effective traits. This requires additional binding arguments but no other feature's rule implementation. Projectile geometry remains skill-owned; shared delivery owns damage and contacts. Regression across shared attack consumers remains necessary because their execution ordering is shared.

## Execution record

- Intimidation correction: three source files, static ordering review completed; no runtime verification.
- The pre-existing ten modified asset/settings files were checked byte-for-byte before this follow-up and remain outside the task.
- Compatibility uses `PlayerSkillDefinition.SupportsTrait` as its single predicate. Boundaries pass the actual skill definition and effective trait keys; existing non-skill profile/distance validation remains unchanged.
- Projectile range effects intersect captured enemies with the existing contact ledger's original target IDs. Damage counting reuses the same live-delivery predicate as the hit loop and creates only a temporary target-damage scope.
- Static scenario trace: with budget two, a barrel followed by a fragile enemy initially counts that enemy; if the barrel explosion kills it, the next hit skips it without spending a slot and counts the next living candidate. The barrel's already settled damage remains unchanged.
- Final independent static review found no remaining actionable issue in the 16 changed source files. `git diff --check` passed; source files use valid UTF-8/LF with no duplicate using directives or replacement characters. Tests were not modified or executed, and compilation/runtime behavior remains unverified.
