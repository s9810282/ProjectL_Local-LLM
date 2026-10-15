# LifeCycle as a runtime peer

**Goal / user specification:** LifeCycle and GlobalSystem must be siblings, not parent and child.

**Execution:** Continue inline in the current workspace. Preserve user edits, file contents other than required references, GUIDs and assembly boundaries. No commit.

- [x] Add a regression check for independent root-level LifeCycle composition and absence of Global ownership.
- [x] Move RuntimeSystem/GlobalSystem/LifeCycleSystem to RuntimeSystem/LifeCycleSystem and Factory/Runtime/Global/LifeCycle to Factory/Runtime/LifeCycle with metadata.
- [x] Update namespaces and consumers; register LifeCycle once at the runtime root, after its dependencies. Remove Global's LifeCycle accessor and registration.
- [x] Compile and run affected managed tests, audit GUIDs/namespaces, review independently and update current documentation.

Registration order will be Buffer, Global, InGame, LifeCycle; reverse shutdown therefore disposes LifeCycle before its dependencies. LifeCycle continues to resolve Global's loading service through the root resolver.

## Verification

- The new managed regression failed before the change because Global exposed LifeCycleServiceCommand; it passed after removing ownership. The Unity-native full composition test now asserts direct root lookup of LifeCycle.
- Moved 17 C# files and 33 metadata files (50 total); all destination files exist, old paths are absent, and all metadata SHA-256 hashes are unchanged. See `2026-09-22-lifecycle-move-manifest.json`.
- All 350 namespace declarations under Script still match their folders. No old Global/LifeCycle namespace references remain in source or serialized assets. Production code has exactly one LifeCycle factory registration, in the root factory.
- All 12 standalone Unity-reference compilation targets passed. Executed 17 managed tests: LifeCycle composition 1, JobSystem 8, MapProgress 6, loading callback 1, query contract 1; all passed.
- Editor import and the full runtime composition test requiring native Mesh/GameObject creation were not executed. Current structure and access rules are documented in Script/README.md.
- Temporary compiler/test output: `%TEMP%/ProjectL-LifeCycle-Peer`.
- Independent read-only review found no actionable defects in root registration, resolver binding, dispatch order or stale references. README was updated to the final sibling structure.
