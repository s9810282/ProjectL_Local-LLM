# Framework / Systems Reorganization Implementation Plan

> Execute inline using superpowers:executing-plans. The user approved the revised in-chat design and requested immediate implementation.

**Goal:** Move the existing foundation and game systems under Assets/ProjectL/Script while preserving assets and separating assembly dependencies.

**Architecture:** Framework owns generic system contracts, stores, builders, runtime dispatch, database bases, pools and queues. Systems owns concrete services, service keys, game data, factories and runtime groups. LifeCycle, UI and Render bases belong to their GlobalSystem subsystem, not Framework.

**Spec:** The approved conversation design: Framework / Systems / Render; LifeCycle registered inside GlobalSystem.

**Tech Stack:** Unity 6000.0.73f1, C#, Unity asmdef and asset GUIDs.

## Constraints
- Continue in the user's current workspace, preserving existing edits and deletions.
- Move source and matching metadata together. Do not restore previously deleted gameplay code.
- Keep ProjectL/Core and existing scene, prefab and database asset locations unchanged.
- Preserve AudioSystem and all current renderer feature changes.
- Framework must not depend on ProjectL.Systems, ProjectL.Core, FMOD or concrete game services.
- Keep class names and serialized members stable; update namespaces and serialized type metadata where required.
- Do not commit the user's accumulated work.

## Tasks
- [x] Record every source/destination and original GUID; normalize the empty destination folder names.
- [x] Move the 158 Framework scripts according to responsibility; move Audio code into GlobalSystem/AudioSystem and existing Render content into Script/Render.
- [x] Separate Framework and ProjectL.Systems assemblies, update namespace imports and reflection references, and expose only the construction APIs required across that boundary.
- [x] Move LifeCycle composition into GlobalSystem, expose its context there, and update composition regression assertions.
- [x] Validate original GUID preservation, surviving source coverage, assembly dependency direction, compilation of Framework/Systems/Core/render/tests, and inspect the final diff.
- [x] Write the current layout and validation results, and obtain an independent review while performing final local checks.

## Review Focus
- No source or asset lost during movement; original GUIDs retained.
- Abstract UI/Render/LifeCycle classes stay in GlobalSystem.
- No duplicate root-level LifeCycle registration; parent forwards initialization and shutdown once.
- Reflection assembly lookups and namespace strings follow the moved types.
- Player compilation excludes editor-only renderer camera checks.

## Execution Ledger
- Planning: source inventory confirms Framework contains 158 C# files; existing Core remains outside this migration.
- Ruling: perform the migration in-place because the user explicitly asked to reorganize the folders they created in this workspace, which already contains their changes.
- Completed: normalized Framework folder naming; moved 233 source/resource files with original GUIDs. Final C# counts: Framework 18, Systems 143 (including new AssemblyInfo), Render 6.
- Composition: LifeCycle is registered last inside GlobalSystem, after JobSystem, and exposed through GlobalSystemGroup.LifeCycleServiceCommand. It is no longer a root service. The composition test asserts both conditions.
- Compatibility: original renderer feature contents, including camera/prefab-stage filtering, are byte-identical to the pre-migration snapshot. Audio source remains in GlobalSystem/AudioSystem. Existing Core, scene, prefab and database asset locations remain unchanged.
- Review: independent read-only reviewer found no actionable defects in assembly direction, lifecycle ownership, namespace/reflection references or asset metadata preservation.

## Validation results

- All 233 manifest destinations exist and retain their original GUIDs. Moved/new metadata GUIDs are unique across Assets. Render contents match the recorded SHA-256 hashes.
- Standalone C# compilation against the installed Unity 6000.0.73f1 reference assemblies passed for 11 targets: Core.Contracts, Core.Domain, Core.Application, Framework, Systems, Render player, Render editor, Audio samples, Core tests, boundary tests and Framework/Systems tests. This is a source compilation check, not a Unity Editor import/build. Compiler warnings were suppressed for this check.
- Framework compiled using only base/Unity engine references; no dependency on Systems, Core or FMOD was found.
- Mono-hosted NUnit results: Core 331 passed / 11 failed; CombatViewBoundary 14 passed; JobSystem 8 passed; MapProgressBuffer 6 passed; loading callback registration 1 passed; StageViewProp query contract 1 passed. Total executed unique tests: 361 passed, 11 failed.
- Core source and all 33 top-level Core test files match HEAD. Its 11 failures concern CombatTimelineScheduler (5), EntityDeploymentFlow (5) and UnitCombatStaticStats (1), with timeline expectations and invalid priority arguments. These were not changed as part of the folder migration.
- Unity-native tests, including full runtime composition, ScriptableObject database tests and renderer interaction tests, were compiled but not executed. Full composition prewarms Mesh/GameObject instances and requires a running Unity test environment. Editor import, Play Mode and prefab-stage behavior still need an in-editor check.
- Changed tracked test/sample files pass `git diff --check` (Git emitted line-ending conversion notices only).
- Current architecture is documented in `Assets/ProjectL/Script/README.md`; old architecture documents are clearly marked as historical.

The temporary validation output for this run is `C:/Users/HYTEAN~1/AppData/Local/Temp/ProjectL-LayoutCheck-8063004bc9734cbcae73346bb02962d1`, containing compiled assemblies, response files and NUnit XML results. No commit was created.
