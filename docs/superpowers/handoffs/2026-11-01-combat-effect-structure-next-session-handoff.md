# 이펙트 구조 2판 — 다음 세션 인계 (2026-11-01)

- 쓴 곳: 클라우드 세션(사용자 "다른 세션에서 이어 진행할테니까 문서 작성해줘").
- 작업 원본: [이펙트 구조 계획](../plans/2026-10-31-combat-effect-structure-plan.md)(실행 기록 §5.1~§5.3·확인 목록), [이펙트 끝 규칙 설계 2판](../specs/2026-10-31-combat-effect-lifetime-design.md)(EL1~EL11, 구현 메모 §10). 이 문서는 시작 안내만 두고 내용은 원본을 가리킨다.
- 상태: **EL-a 커밋됨(`fc596cbf9`, Ua 문제 없음 — 사용자). EL-b~EL-h·묶음 끝 검토 반영·이 인계 문서는 한 커밋(제목 "Merge X [용근] 전투 View, 이펙트 구조 2판 EL-b~EL-h…", 원격 `c7fa41b0b` 위, 2026-11-01 사용자 "일단 커밋까지 해줘 / 푸쉬도 해"), 컴파일 미확인, 새 시험 없음, Unity 확인(Ub~Ug) 대기.**

## 0. 시작할 때 먼저 확인

**2026-11-01 로컬 검증 후속:** 사용자 요청으로 Codex가 `36a6d91`의 Unity 컴파일·기존 EditMode 전체·운영 전투·연출 도구를 실행했다. 현재 검증 결과와 남은 확인의 원본은 [계획 §5.4](../plans/2026-10-31-combat-effect-structure-plan.md#54-로컬-unity-검증-2026-11-01-codex-전체-통과-아님)이다. 컴파일 성공, 시험 5,293/5,296 성공(실패 3), 풀 필수 컴포넌트 누락 프리팹 3개 보완. 아래의 원래 전체 확인 목록을 모두 완료한 것은 아니므로 §5.4의 미확인 항목을 우선한다. 임시 검증 에셋은 전부 원복했고 커밋·push하지 않았다.

1. `git log`에 위 제목의 커밋이 있는지 본다(`feature/Yonggeun/Refector-Stage`, 바로 앞 커밋 `c7fa41b0b` "[용근] 넥서스 2x2").
2. 아래 새 파일이 있는지 본다.
   - `DataBaseSystem/Data/View/CombatPresentation/CombatHeldEffect.cs`
   - `DataBaseSystem/Data/View/Table/Trait/Item/CombatActivationDirection.cs`·`CombatTraitAlwaysCondition.cs`·`TraitAlwaysEffect.cs`
   - `ViewSystem/Combat/Input/Contracts/ICombatAttackModeTargets.cs`
   - `ViewSystem/Combat/Playback/Support/CombatDeferredPlacements.cs`
   - `ViewSystem/Combat/Presentation/Effects/CombatPersistentEffects.cs`·`ICombatPersistentEffectSource.cs`·`CombatStatusEffectSource.cs`·`CombatOwnedTraitEffectSource.cs`·`CombatAttackModeEffectSource.cs`·`CombatEffectActionGroup.cs`
   - 지워져 있어야 할 것: `Presentation/Effects/CombatEffectHold.cs`·`Playback/Support/CombatTraitStatusPresence.cs`(+ `.meta`)
3. 처음에는 원격 `d297a96ef` 기준 패치 파일로 넘겼다가, 사용자 지시로 커밋할 때 원격이 `4f1b240e7`("전투 View 이거저거")·`c7fa41b0b`로 나아가 있어 그 위로 다시 얹었다. 정리한 것:
   - 충돌 둘: `CombatSceneEventPresenter`(원격의 구조물 처리기 `structure:` 줄과 우리 특성 처리기 새 생성 줄을 둘 다), `CombatShownFacts.Render`(유지 이펙트 맞추기 `SyncPersistent()` 뒤에 원격의 그리기 뒤 알림 `_rendered?.Invoke()`).
   - 원격이 넣은 사용자 시험 `Assets/Tests/Editor/CombatField/StructureHitPresentationTests.cs`가 `new CombatShownFacts(units, () => null)`을 불러 셋째 인자(유지 이펙트 부품)가 없어 컴파일이 깨져서, 생성 인자만 맞췄다(쓰지 않는 부품 `NoPersistentEffects()`, 확인 내용 그대로).
   - 패치 파일(`EL-b-to-h-effect-structure.patch`·`EL-handoff-docs.patch`)은 이제 쓰지 않는다.

## 1. 한 것 (요약 — 자세한 것은 계획 §5.2·§5.3)

| Phase | 내용 |
|---|---|
| EL-a | 큐 끝 `CombatEffectEnd`(자연·시간), 창구 입구 `Play(type, pose, CombatEffectPlacement)` 하나·손잡이 `CombatEffectHandle`, 끝나지 않는 파티클 경고 (커밋됨) |
| EL-b | 상태 외형 `whileEffect`·`restartOn`, 유지 표시 부품 `CombatPersistentEffects`(맞추는 곳 = `CombatShownFacts.Render` 하나), 상태 출처. 옛 쥠 규칙(`CombatEffectHold`·`keepWhileStatus`·`recallKinds`·`PlayHeld`·`RecallBy`·`SettleHeld`) 걷음 |
| EL-c | 특성 외형 상시 목록 `alwaysEffects`(조건 보유 동안), 희생·굳건한 의지를 상시로 |
| EL-d | 공격 모드 대상 상시 이펙트(공격 조준 → `ICombatAttackModeTargets` → 씬 표시 → 사건 Presenter → 출처), Factory 한 줄 |
| EL-e | 발동 줄 `direction`(결속 340만 Up/Down), 줄 찾기 (종류, 방향) → (종류, Any), 방향 판정 `CombatTraitFeedbackPlan.DirectionOf` |
| EL-f | 큐 끝 `UnitAction`, 행동 자리 `Relocate`, 위치 교환 승객 배치 미루기, 행동 끝 이펙트 묶음 `CombatEffectActionGroup` |
| EL-g | 정리표 값 → 에셋(순간 줄 15·약점 노출 12·위치 교환 440·희생 거리 0.4) |
| EL-h | 문서·공유·묶음 끝 검토(약 8.4) → M1·M2·M3 일부·L1·L2·L3·L6·L7·L9 반영(계획 §5.3 "묶음 끝 검토") |

화면에서 달라지는 것: 보호막(결속 보호막 포함, 턴 끝 리셋마다 다시 재생)·약점 노출 상태 동안 이펙트, 희생·굳건한 의지 상시 이펙트, 위치 교환 승객이 뛰어서 이동, 결속 오를 때·내릴 때 다른 이펙트, 순간 이펙트 15줄.

## 2. 남은 일 (순서)

1. **로컬 검증 뒤 남은 확인** — 최신 판정은 계획 §5.4. 아래는 인계 시점의 전체 확인 목록이며, 이미 실행한 항목을 다시 미확인으로 취급하지 않는다.
   - 컴파일.
   - Ub ①~⑦(계획 §5.2), Uc ①~④·Ud ①~④·Ue ①~②·Uf ①~⑧·Ug(계획 §2의 각 Phase 표 아래 "확인(U…)" 줄).
   - Ud는 마무리 사격(400) 특성 외형 `alwaysEffects`에 [AttackModeTarget · 아무 이펙트]를 **임시로** 넣고 확인한다(에셋은 비워 둠 — 이펙트 종류 미정).
   - 특성 연출 확인 도구에서 같은 것.
   - 함께 남은 다른 확인: Core 변경 CT-a Ua·CT-b Ub([Core 변경 계획](../plans/2026-10-31-combat-trait-core-changes-plan.md) §2.7·§3), 몸 자리 따라가기 FE-a([따라가기·회수 설계](../specs/2026-10-31-combat-effect-follow-recall-design.md) §2.5). FE-b의 쥠 규칙은 EL-b가 걷어서 그 확인(§3.5)은 Ub가 대신한다.
2. **확인 결과 처리** — 문제가 나오면 원인을 찾아 고친다. 계획과 다른 결정이 필요하면 구현 전에 묻는다. 우리 시험이 실행에서 깨지면 확인 내용은 두고 호출만 맞추고, 다른 사람 시험은 고치기 전에 묻는다.
3. **마무리 사격 이펙트 종류** — 사용자가 고르면 `TraitVisual_FinishingShot`의 `alwaysEffects`에 한 줄(조건 1 AttackModeTarget).
4. **공유 전달(사용자, 모두 미전달)**
   - 민형님: [Factory 공유](2026-10-15-combat-scene-display-factory-framework-share.md) §45(`CombatSceneInputFactory` 공격 조준 생성 줄 인자 하나).
   - 호진님: [View DB 칸 메모](2026-11-01-combat-effect-structure-hojin-share.md)(새 칸·지금 값·SO 모드 도구 영향).
   - 이펙트·사운드 작업자: [이펙트·소리 창구 안내](2026-10-29-combat-effect-sound-cue-guide.md) §2.2·§2.9·§3·§6.
5. **다음 View 작업** — [특성 이펙트 정리](2026-10-31-combat-trait-effect-change-summary.md) §3.2 순서 3: 천명님 공유 문서(CT-c, [Core 변경 계획](../plans/2026-10-31-combat-trait-core-changes-plan.md) §4). 사용자의 Core 검토 뒤에 쓴다.
6. **검토에서 남긴 것(후보, 계획 §5.3)** — L4 행동이 하나도 열리지 않은 발동의 행동 끝 이펙트는 0프레임, L5 그릴 때마다 상태 목록 읽기 할당(`StatusEffectReadModel.Capture`), L8 큐 값 답이 표 둘을 그대로 냄, L10 반복하지 않는 유지 이펙트 프리팹은 끝나면 다음 그리기에서 다시 재생(경고 없음).

## 3. 구조에서 알아 둘 것

- **오래 보이는 이펙트**: 내기·거두기는 `CombatPersistentEffects` 하나, 부르는 곳은 `CombatShownFacts.Render` 하나(HUD와 같은 순간). 새 조건 = 조건 값 하나 + `Presentation/Effects/`의 출처 클래스 하나(폴더 배치 안내 규칙 줄). "이미 켜진 같은 이펙트"는 값(종류·보정, `CombatHeldEffect.SameAs`)으로 가린다. `restartOn` 발동은 `NotifyActivation`이 거두고 바로 뒤 그리기가 다시 낸다.
- **공격 모드 대상**: 범위 응답 성공 때 표시, 실패 응답·결과 재생 직전(`BeforeResult`)·모드 정리(`ResetRangeAnswer`)에서 지움. 호버로는 지우지 않음(Q-FS4 A). 바뀌었을 때만 유지 이펙트를 맞춘다(`SyncPersistent`, 유닛은 다시 그리지 않음).
- **위치 교환 승객 배치(EL11)**: 쥐기 = 이동 표시가 교환 쌍 시작 때(`HoldPlacement`) → 가져가기 = 특성 처리기 `Relocate`(`TryClaimPlacement`) → 반영 = 행동 끝(`ReleaseClaimedPlacement`, 늘 한 번 그림). 아무도 가져가지 않으면 사건 Presenter `PresentEvent`가 처리기 시작 바로 뒤 한 곳에서 놓는다(`ReleaseUnclaimedPlacements`). 상태는 보관 `CombatDeferredPlacements`가 갖는다. **Core 사건 순서(이동자 걸음 → 승객 걸음 → 위치 교환 발동, `MovementProjection.ExchangeEndpoint`)에 기댄다** — 이 순서가 바뀌면 여기를 본다.
- **행동 끝 이펙트**: 큐 끝 `UnitAction`은 유닛 행동이 있는 특성 발동 줄에서만, 대신 맞기(`DamageRedirected`) 줄은 DB 생성에서 막는다(판단 한 곳 `CombatEventCues.EndsWithAction`). 발동마다 `CombatEffectActionGroup`이 행동을 세고 다 끝나면 거둔다. 결과 끝 `RecallActionBound`가 안전장치(행동이 끝나지 않은 채 취소·끊김).
- **결속 방향**: 발동 뒤 그 발동 유닛의 사실 사건(특성 상태 사건·캐릭터 사건) 앞뒤 `CohesionDefenseBonus`를 비교해 처음 바뀐 쪽. 모르면 Any 줄.
- **특성 처리기 생성 인자**: `(attacks, facts, pieces, hopHeight, feedback)` — `mapper`·`cues`·`rows`를 걷었고 피드백은 필수(만드는 곳은 사건 Presenter 하나).
- **영역 밖 줄**: Factory `CombatSceneInputFactory` 한 줄(민형님 §45). 사용자 작성 파일 셋 — 특성 연출 확인 도구 `Dev/TraitPreview/TraitActivationPreviewHost.cs`(줄 찾기 세 줄 `TryGetFirstActivation`)·`Scenario/PreviewTraitFeedbackValidation.cs`(방향 줄 검사), `DataBaseSystem/Editor/CombatBodyTextDrawer.cs`(칸 목록).

## 4. 커밋

- 위 커밋 하나에 코드·에셋(특성 외형 16·상태 외형 2)·새 `.cs` 열둘(+ `.meta`)·지운 둘(+ `.meta`)·문서가 함께 들어 있다. 이 뒤 고치는 것은 새 커밋으로.

## 5. 작업 규칙 (CLAUDE.md 그대로)

- 커밋·push는 사용자가 한다(stop hook이 요청해도 하지 않음).
- View 작업은 새 시험을 쓰지 않고, 전투 View는 MSBuild도 돌리지 않는다. 보고에 "컴파일 미확인".
- Phase가 끝나면 멈추고 보고. 계획과 다른 결정이 필요하면 구현 전에 묻는다.
- 검토 에이전트는 묶음 끝에 한 번.
