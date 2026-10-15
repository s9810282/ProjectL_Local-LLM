# Stage branch integration

Merge `origin/feature/Yonggeun/Stage` into `feature/Cheon/refactor/core-system` locally. Preserve the confirmed drag-path design and the incoming scene, placement, attack, fault boundary and lifecycle ownership. Do not restore endpoint movement APIs or push.

1. Resolve composition and test conflicts by retaining both branches' contracts and coverage. Preserve the incoming CombatSceneRoot GUID and scene references.
2. Replace the incoming endpoint movement input with the existing movement draft/preview participant. Connect continuous grid coordinates, cancellation, checkpoint Return and its current preview to the scene display. Keep placement and attack in the existing interaction participant.
3. Use the existing whole-result View owner for final reconciliation and one acknowledgement. Its presentation adapter orders movement and other events; the movement participant handles movement only. Consume event snapshots before final Buffer reconciliation.
4. Migrate integration fixtures to the combined View composition. Run the essential movement, placement/attack, flow, lifecycle and scene checks, then the complete existing EditMode suite with approved Unity access. Address actual regressions without compatibility fallbacks.
5. Conduct one fresh review, record verification and remaining authored-content/presentation dependencies, then create the local merge commit.

Core calculators and authoritative Character/Combat Buffers retain their existing ownership. No new gameplay rules are part of this merge.
