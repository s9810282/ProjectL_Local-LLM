# Folder-aligned namespaces implementation plan

**Goal:** Match C# namespaces under Assets/ProjectL/Script to `ProjectL.<relative folder path>`.

**Spec:** User requested the final namespace cleanup after folder reorganization. The user has also moved Core into Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator.

**Architecture:** Preserve files, asset GUIDs, assembly names and boundaries. Update declarations and dependent references, including namespaces split across subfolders. Global renderer components gain namespaces and Unity type migration metadata.

**Execution:** Inline using executing-plans; preserve the current workspace and existing user edits. Do not commit. This is a namespace refactor without intended behavior changes.

- [x] Inventory mismatches and snapshot changed source for comparison; keep all asset metadata untouched.
- [x] Update Core, UIBase, renderer and importer namespaces; update consumers, reflection strings and asmdef rootNamespace values.
- [x] Compile every affected assembly with installed Unity references, including editor/player variants; run existing non-native tests and compare pre-existing failures.
- [x] Verify namespace paths and no old code references; request an independent read-only review and update documentation.

## Review focus

- Folder splits in Traits, Commands, Snapshots and StageGimmicks must retain cross-type visibility through explicit imports.
- Nested Core asmdefs retain their separate assembly boundaries even inside Systems.
- Renderer components keep their GUIDs, class names, serialized fields and camera filter behavior.
- Unity serialized type names and reflection lookups must follow namespace changes; assembly names must not change.
- Editor importer remains excluded from player compilation.

## Execution ledger

- Initial audit found Core still using ProjectL.Core namespaces after the user's move, UIBase missing its Base segment, and seven files declaring global types (six renderer files and one editor importer).
- Completed all 350 namespace declarations under Script to match their folder paths, including an empty ArchitectureEffect namespace. Files containing only assembly attributes need no namespace. Updated dependent imports and the ColorScope test's stale StageComposition namespace.
- Preserved Core assembly names and references. Only rootNamespace changed in its three asmdefs. No asset, prefab, scene or `.meta` file was modified by this refactor.
- Added MovedFrom to UIBase and the five formerly global Unity component/renderer types. Their class names, serialized fields and script GUIDs remain unchanged. No relevant serialized fully qualified or managed-reference type strings required editing.
- Qualified UnityEngine.Shader.PropertyToID to avoid the new ProjectL.Render.Shader namespace shadowing the Unity Shader type. Kept editor-only camera filter imports guarded outside the namespace.
- Baseline and final compilation passed all 12 standalone Unity-reference targets: Core.Contracts, Core.Domain, Core.Application, Framework, Systems, render player/editor, Tool editor, Audio samples, Core tests, boundary tests and Framework tests. Nested Core source is excluded from Systems compilation, matching its asmdef boundaries.
- Compared 372 unique NUnit cases before and after: 361 passed and the same 11 Core tests failed in both runs. Per-test names and outcomes match. Groups: Core 342, boundary 14, Job 8, MapProgress 6, loading callback 1, query contract 1. The existing failures concern timeline expectations and priority validation, not namespace changes.
- Read-only independent review found no actionable regression. Non-ASCII source bytes were preserved, asmdef changes were restricted to rootNamespace, and remaining ProjectL.Core strings identify intentionally unchanged assemblies. Corrected the reviewer's empty namespace finding and cosmetic UIBase indentation.
- Actual Unity import, prefab rendering and Unity-native tests were not executed. Standalone compilation does not substitute for Editor runtime verification.
- Temporary evidence: `%TEMP%/ProjectL-Namespace-Snapshot` (before-change files/type mapping), `ProjectL-Namespace-Baseline` and `ProjectL-Namespace-After` (compiler response files, assemblies and NUnit XML). No commit created.
