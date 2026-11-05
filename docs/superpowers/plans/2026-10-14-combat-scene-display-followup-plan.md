# CombatSceneDisplay 후속 리팩토링 Task 계획

- 작성: View 작업, 2026-10-14
- 설계 원본: [후속 리팩토링 설계](../specs/2026-10-14-combat-scene-display-followup-design.md).
- 상태: **SD-A 코드·MSBuild·새 에이전트 검토 완료(§7.4). SD-B 코드·MSBuild·관리 회귀·새 에이전트 검토 완료(§7.5). SD-C′ 코드·MSBuild·관리 회귀·새 에이전트 검토 완료(§7.6). SD-C0 최초 평가·에이전트 4개 검토 이력은 §7.7. 독립 에이전트 3개 재평가와 조립·기능 분리 방향은 §7.8, 합의 후 Phase 계획 연결은 §7.9. SD-A·B·C′ 현재 Unity 검증은 D5 사용자 전체 확인으로 인수(§7.10). 수정 전 기준선 미확보 이력은 유지. SD-C1 전체 입력 피드백 이전안은 보류.**
- **착수 조건:** R0~R4·D12 변경을 커밋한 뒤 시작한다(SD-0.2). SD-A 착수 때 `5f6b315e`에 포함된 것을 확인했다(§7.4).
- 범위: 단일 Facade 유지, 연결 안전성 보완, 재생 계약 축소, 비용 미리보기 대상 상태 이전과 재평가 이력. 후속 **SD-D0~D5의 Task·실행 상태는 [조립·기능 분리 계획](2026-10-14-combat-scene-display-composition-plan.md)에만 기록**한다.
- 기존 R0~R4의 진행·검증 원본: [기존 구조 리팩토링 계획](2026-10-14-combat-view-structure-refactor-plan.md). 이 문서는 그 상태를 복제하거나 완료 처리하지 않는다.
- 단계 ID는 `SD-A`·`SD-B`·`SD-C′`·`SD-C0`·`SD-C1`이다. 기존 전투 View Phase A/B/C 또는 R단계와 구별한다.
- 각 Phase의 구현·가능한 검증이 끝나면 **새 에이전트 한 개**가 해당 Phase 작업물을 검사한다(2026-10-14 사용자 지시). 발견 사항을 처리한 뒤 변경·검증·검토 결과·실습 워크시트와 다음 범위를 보고하고 멈춘다. 커밋·push·외부 전달은 사용자가 한다.

## 0. 착수와 검증 기준

- [x] 사용자 합의 방향을 설계와 계획으로 기록한다.
- [x] 검토 결과를 설계·계획에 반영한다(§7.2).
- [x] **SD-0.1** 착수 시 미커밋·untracked 파일을 포함한 현재 코드와 기존 R계획의 최신 실행 기록을 다시 대조한다(§7.4).
  - 검토 시점(2026-10-14)의 관측: R0~R4와 D12는 Unity 확인까지 완료됐고, 아직 커밋 전이다.
  - 같은 날 IDE가 열려 있던 예전 버퍼로 `CombatViewSystem.cs`를 덮어쓴 일이 있었다(R계획 §6.8). 착수 전 IDE에서 대상 파일을 디스크 기준으로 다시 연다.
- [x] **SD-0.2** R0~R4·D12 커밋을 확인한 뒤 착수한다. 이 계획이 고치는 `CombatViewSystem`·`CombatSceneDisplay`·`FieldUnitPresenter`·`OrderedCombatPresentation`과 관련 테스트가 그 변경에 포함된다. 기존 변경을 되돌리거나 다시 구현하지 않는다(§7.4).
- [x] **SD-0.3** 해당 Phase에서 바꿀 파일·테스트와 기존 실패 여부를 확인한다. 결과를 실행 기록에 남긴다(SD-A는 §7.4, 이후 Phase에서 다시 확인).

검증은 MSBuild와 Unity를 구분한다.
- MSBuild는 해당 어셈블리와 테스트 어셈블리를 스크래치 출력으로 확인한다. 새 C# 파일이 csproj에 아직 없으면 기존 `CustomAfterMicrosoftCommonTargets` 방식을 사용하고, 생성된 csproj를 수작업 기준 파일로 삼지 않는다.
- Unity는 에디터 컴파일·관련 EditMode·필요한 재생을 확인한다. 실제 씬이 필요한 테스트를 관리 실행기 결과로 대신하지 않는다.

기존 회귀를 재사용하고 부족한 행동만 추가한다. 파일 이동이나 단순 위임을 그대로 복사한 테스트는 만들지 않는다. 이번 범위는 DB·에셋 변경이 없으므로 `DataBaseAssetTests`를 의무 검증으로 추가하지 않는다. 범위 밖 변경이 필요하면 먼저 설계를 검토한다.

## 1. SD-A 중복 Attach 수명 보완

목표: 이미 연결된 root로 공개 `Attach`를 다시 호출해도 현재 표시와 연결이 그대로 남고 새 표시 자원을 만들지 않는다. 결정 원본은 설계 §4.1이다.

| 구분 | 대상 |
|---|---|
| 운영 코드 | `ViewSystem/Combat/CombatViewSystem.cs`의 `Attach`·`AttachDisplay`와 공통 검증 메서드 하나, `CombatSceneDisplay` 생성자 주석 |
| 테스트 | `Assets/Tests/EditMode/Framework/CombatDisplayReuseTests.cs`(실제 씬 fixture) |
| 그대로 둘 것 | Display 생성자·Install·Detach·Release 동작, 씬과 에셋, 일반 생성 실패의 롤백 정책 |

- [x] **SD-A.1 fixture**
  - `CombatDisplayReuseTests.AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`가 이미 실제 씬·실제 View DB·공개 `view.Attach(root)`를 조립한다. 이 설정을 helper로 뽑아 같은 파일에 새 테스트를 둔다.
  - 새 테스트는 `CombatSceneEventPresenterTests` SetUp의 가드를 따른다. Combat 씬이 이미 열려 있으면 Ignore하고, EventSystem을 보존한다. 그래야 정리 때 사용자가 열어 둔 씬을 닫지 않는다.
  - 다음 위치는 쓰지 않는다.
    - `CombatSceneEventPresenterTests`: SetUp에서 Display를 직접 만들어 이미 부작용이 난 상태다.
    - `CombatViewSystemTests`의 Rig 예외 테스트: 참조가 빈 Root라 지금 코드도 같은 예외를 던져 판별력이 없다.
- [ ] **SD-A.2 회귀(현재 코드에서 실패 확인):** 회귀 2케이스 작성·컴파일 완료. 수정 전 실패 재현은 미확보 이력으로 남고, 현재 수정 후 결과는 D5 사용자 전체 확인으로 인수했다(§7.10).
  - 준비: 공개 Attach → Tick → 고스트를 띄운다(`root.Ghost.Show` 또는 Display 경유 `ShowGhost`). 고스트의 활성 참조와 GameObject, `FieldThorns_Inactive`가 1개임을 기록한다.
  - 두 번째 `Attach(root)`가 예외를 던진 뒤 다음을 단언한다.
    - (a) 고스트가 같은 참조이고 GameObject가 살아 있으며 activeSelf다.
    - (b) 가시밭 보관 루트가 여전히 1개다. 운영 DB에서는 `ThornFieldVisual` 프리팹 때문에 항상 생긴다(선례: `CombatMortarAssetTests`의 개수 단언).
    - (c) 연결 상태·입력 Layer·수신 슬롯이 그대로다.
    - (d) `view.Detach()` 뒤 `FieldThorns_Inactive`가 남지 않는다. 거절된 Display의 고아 자원을 잡는다.
  - EditMode는 즉시 파괴하므로 지금 코드에서 (a)(b)(d)가 동기적으로 실패해야 한다.
- [x] **SD-A.3 구현**
  - 검사 조건은 `_display != null`이다. `IsAttached`는 쓰지 않는다. 재생 중단이 재생 연결만 풀고 `_display`를 남기므로, `IsAttached`로 검사하면 그 뒤의 중복 Attach가 통과한다.
  - 공통 검증 메서드 하나에 조건과 오류 문구를 둔다. 공개 `Attach`는 기존 검사(null·오류 정지·미초기화·비활성 진입) 뒤, Display 생성 직전에 부르고, 내부 `AttachDisplay`도 같은 메서드를 쓴다.
  - 자동 Detach·재연결은 추가하지 않는다.
  - `CombatSceneDisplay` 생성자 주석에 "SceneRoot 하나에 살아 있는 Display 하나" 전제를 적는다.
- [x] **SD-A.4 기록:** 다음을 실행 기록에 남긴다(§7.4).
  - Lobby·StageMap(재연결 시 자동 Detach)과의 정책 차이가 의도라는 점
  - 설계 §4.1의 남은 위험 두 가지(생성 뒤 예외 시 Display 미해제, `_sceneRoot` 구독)
  - 실제 중복 경로(Install 실패 뒤 재시도)
- [x] **SD-A.5 검증:** MSBuild 및 현재 Unity 검증 인수. Unity는 D5 사용자 전체 확인 출처이며 당시 수정 전 실패 재현은 미확보(§7.10).
  - 새 테스트, `CombatViewSystemTests`의 `Attach_Twice_IsRejected`·`Attach_AfterFault_DoesNothing`·`Detach_CancelsAndReleases_…`, `CombatDisplayReuseTests` 전체
  - 정상 최초 Attach와 Detach 후 Attach가 그대로인지 대조한다.
- [x] **SD-A.6** 실행 기록·검증 결과·새 에이전트 검토 결과·짧은 실습 워크시트·B 범위를 보고하고 멈춘다(§7.4). Unity 검증 대기는 별도로 유지한다.

완료 기준: 중복 Attach가 자원 생성·기존 표시 변경 없이 거절되고, 정상 연결 수명이 유지된다. 실제 root 경로 검증이 남아 있으면 코드 완료와 Unity 확인 대기를 분리한다.

## 2. SD-B 재생 연결 계약 축소

목표: 연출 연결은 `ICombatEventPlayback`만 요구하며 실제 씬 연결은 계속 Display의 수명 방어를 통과한다. 결정 원본은 설계 §4.2다.

| 구분 | 대상 |
|---|---|
| 운영 코드 | `ICombatPresentation.cs`, `OrderedCombatPresentation.cs`, `ICombatEventPlayback.cs` 주석 |
| 테스트 | `FakeCombatPresentation.cs`, `CombatViewSystemTests.cs`, Display를 실제로 쓰는 기존 씬 테스트 하나 |
| 호출 확인 | `CombatViewSystem.AttachDisplay`, 모든 `ICombatPresentation` 구현·Attach 호출 |
| 그대로 둘 것 | `ICombatBattleDisplay` 상속 구성, 전달되는 실제 Display 객체, 재생 알고리즘·해제 규칙 |

- [x] **SD-B.1** 전체 저장소에서 `ICombatPresentation` 구현과 `Attach(ICombatBattleDisplay ...)` 서명을 다시 찾아 영향 목록을 고정한다. 구현은 2개(`OrderedCombatPresentation`, `FakeCombatPresentation`)이며, 기존 호출부를 유지한 채 컴파일했다(§7.5).
- [x] **SD-B.2** 인터페이스와 구현·테스트 가짜의 인자를 `ICombatEventPlayback`으로 바꾼다.
  - `ICombatPresentation.Attach` 주석에 "해제 뒤 사건 시작을 거절하는 수명 방어 객체(현재 `CombatSceneDisplay`)를 넘긴다"를 적는다.
  - `ICombatEventPlayback` 주석의 구현 목록에 `CombatSceneEventPlayback`(Display 내부 전용)을 더한다.
  - `_eventPlayback` 내부 객체를 ViewSystem에 노출하지 않는다.
- [x] **SD-B.3 같은 객체 전달 회귀 작성:** `FakeCombatPresentation`이 받은 객체를 기록하게 하고, `CombatViewSystemTests`에 ViewSystem이 같은 Display를 넘기는지(`Is.SameAs(display)`) 1건을 추가했다. 재생 계약만 구현한 새 가짜는 만들지 않았다(설계 §4.2). 컴파일 완료, Unity 실행 대기(§7.5).
- [x] **SD-B.4 재생 경로의 해제 뒤 거절 회귀 작성:** 실제 Display를 쓰는 기존 씬 테스트에 Release 뒤 `OrderedCombatPresentation.Play`의 예외와 완료 콜백 불호출 단언을 추가했다. 재생 연결은 유지하고 예외 출처를 확인한다. 컴파일 완료, Unity 실행 대기(§7.5). `FirstInstall_…`은 Display를 직접 불러 B를 검증하지 못한다.
- [ ] **SD-B.5(선택, 이번 범위에서 제외)** 소비자가 없는 `ICombatMovementPresenter`(public)를 정리한다. `ICombatBattleDisplay`가 `ICombatMovementFeedback`을 직접 상속하게 바꾸면 멤버 집합은 그대로다. 외부 참조를 확인하고, 사용자 확인 뒤에만 한다. 이번 SD-B에서는 변경하지 않았다.
- [x] **SD-B.6 검증:** MSBuild·관리 회귀 4케이스 및 현재 실제 씬·ViewSystem 회귀를 D5 사용자 전체 확인으로 인수(§7.10). 실행 기록에 "운영의 실제 방어선은 Detach 순서(재생 Detach가 Display Release보다 먼저)"를 적었다.
- [x] **SD-B.7** 실행 기록·검증 결과·새 에이전트 검토 결과·실습 워크시트·C′ 범위를 보고하고 멈춘다(§7.5). Unity 검증 대기는 별도로 유지한다.

완료 기준: ViewSystem이 같은 Display를 재생에 넘기고, 재생 경로에서도 해제 뒤 사건 시작이 거절되며, 기존 취소·완료 계약이 유지된다. 전달 메서드의 개수 감소는 완료 기준이 아니다.

## 3. SD-C′ 비용 미리보기 대상 이전

목표: HUD 비용 미리보기의 대상을 HUD View의 주인인 `FieldUnitPresenter`가 갖고, Display는 비용 규칙(언제 슬롯을 지우고 채우는지)과 해제 확인만 갖는다. 결정 원본은 설계 §5.2다.

| 구분 | 대상 |
|---|---|
| 운영 코드 | `Display/FieldUnitPresenter.cs`(비용 미리보기 API·Release), `CombatSceneDisplay.cs`(`_hudPreviewActor`·`_blockedWallSet` 제거, Clear·Release 목록) |
| 테스트 | Display 수준 비용 슬롯 회귀(새로), `AttackHealthPreviewDisplayTests.cs`, 해제 뒤 무시 테스트 |
| 그대로 둘 것 | `ICombatBattleDisplay`와 피드백 인터페이스, 공개 입구의 해제 확인, 고스트·Range·체력 미리보기·복귀 견적의 주인 |

- [ ] **SD-C′.1 회귀 먼저(현재 코드에서 통과 확인):** `CombatPointerMappingTests`에 아래 회귀 4건을 먼저 추가하고 운영 코드 수정 전 컴파일을 확인했다. 수정 전 Unity 통과는 미확보 이력으로 남고, 현재 수정 후 결과는 D5 사용자 전체 확인으로 인수했다(§7.10).
  - 공격 비용(A)을 띄운 뒤 `ShowDraft`(B)가 A의 비용을 지우고 B를 띄운다.
  - `ClearAffectedCells`가 비용을 지우되 체력 미리보기는 남긴다.
  - `ClearDraft`가 비용을 지운다.
  - 해제 뒤 `ShowAttackCostPreview`·`ShowAttackBlockers`·`ClearAffectedCells`를 무시한다. 기존 해제 뒤 테스트는 초안·복귀만 본다.
- [x] **SD-C′.2 `FieldUnitPresenter`**
  - `ShowHudCostPreview(actor, cost)`가 이전 대상을 스스로 지우고 새 대상을 기억한다.
  - 지우는 함수를 인자 없는 `ClearHudCostPreview()`로 바꾼다.
  - 자기 Release에서 대상을 정리한다(`_attackHealthPreviewTargets` 방식).
  - `AttackHealthPreviewDisplayTests`의 `ClearHudPreview(id)` 사용을 맞춘다.
- [x] **SD-C′.3 `CombatSceneDisplay`**
  - `_hudPreviewActor`를 지운다. 공개 입구·해제 확인·비용 규칙은 그대로 두고 새 API를 부른다.
  - Clear·Release 목록의 비용 정리는 같은 자리에서 새 함수로 바꾼다.
  - `_blockedWallSet`을 지운다. 벽 칸 목록은 접촉별 벽 칸을 펴는 용도로만 둔다.
  - 새 "해제됨" 플래그를 만들지 않는다.
- [x] **SD-C′.4 검증:** 수정 전·후 MSBuild와 필드 강조 관리 회귀 4건 통과. 현재 Unity EditMode·두 표시 모드 재생은 D5 사용자 전체 확인으로 인수(§7.10). 수정 전 Unity 기준선은 미확보다.
  - EditMode: C′.1 회귀, `AttackHealthPreviewDisplayTests`, `WallAttackInteractionTests`(GridOutline·CellPrefab 양쪽), `CombatPointerMappingTests`, `CombatViewSystemTests.AttackHealthPreview_…`
  - 재생: 공격 모드 비용 표시 → 이동 초안 전환 → 취소, 벽 차단 표시
- [x] **SD-C′.5** 실행 기록·검증 결과·새 에이전트 검토 결과·실습 워크시트·C0 범위를 보고하고 멈춘다(§7.6). Unity 검증 대기는 별도로 유지한다.

완료 기준: Display에 비용 대상 상태가 없고, 비용 슬롯 교체 규칙과 해제 뒤 무시가 회귀로 지켜지며, Clear·Release 순서 의존이 없다.

## 4. SD-C0 남은 직접 구현 확인

이 단계는 문서 작업이다. 새 런타임 클래스나 테스트를 구현하지 않는다. 아래 체크는 최초 평가 완료 이력이며, 당시 현 구조 유지 결론은 사용자 기준 추가 뒤 재평가로 대체됐다(§7.8). 현재 방향 원본은 설계 §5.3이다.

- [x] **SD-C0.1** C′ 뒤 Display의 직접 구현을 다시 나열했다. 씬 수명·공유 대상 조정과 좌표 변환 외에 무상태 표시 변환·호출 계약 방어도 있음을 구분했다(설계 §5.3).
- [x] **SD-C0.2** 최초 평가에서 C1 보류·현재 구조 유지로 정리했다. 이동 초안 이전의 실익을 상태 소유·중복·실패 경계 위주로 판단했던 기록은 §7.7에 남긴다. 현재는 조립과 기능 변경 범위를 함께 평가해 설계 §5.3의 분리 방향을 권고한다. 공격·이동 공용 비용 슬롯과 교체·정리 계약은 계속 유지한다.
- [x] **SD-C0.3** 평가·문서 반영·새 에이전트 검사 결과와 워크시트를 보고하고 멈춘다(§7.7). 구조 평가 종료와 Unity 검증 대기를 구분한다.

## 5. SD-C1 입력 피드백 전체 이전안(보류)

설계 §5.4에 따라 보류한다. 배치·이동·공격·복귀 구현을 새 클래스 하나에 다시 모으면 서로 다른 기능의 변경 이유가 계속 섞인다. 위임 수·호출자 수·새 상태 유무만으로 기능별 분리를 기각하지 않는다. 조립 분리와 응집된 기능별 이전은 §7.8의 별도 후속 방향이며, 이 C1을 완료 처리하거나 구현 범위로 자동 전환하지 않는다.

## 6. 회귀 근거와 검증 범위

아래는 기존 테스트를 고르는 근거다. 표 전체를 매 Phase마다 반복 실행하는 요구가 아니다. 각 Phase 변경과 관련된 묶음을 실행하고, 새 실패나 변경이 있을 때 범위를 넓힌다.

| 계약 | 기존 근거와 추가 확인 |
|---|---|
| 중복 연결·정상 재연결 | `CombatViewSystemTests.Attach_Twice_IsRejected`, `Attach_AfterFault_DoesNothing`, `Detach_CancelsAndReleases_AndOldCompletionDoesNotFinishTheNextAttach` + SD-A.2의 실제 씬 회귀(`CombatDisplayReuseTests`) |
| 재생에 같은 Display 전달·해제 뒤 거절 | SD-B.3의 `Is.SameAs`, SD-B.4의 Release 뒤 `Play` 예외 |
| 지연 사건·취소·재진입 | `CombatPresentationCleanupTests.DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce`, `Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain`, `MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures` |
| 완료 전 표시와 확인 금지 | `CombatViewSystemTests.DelayedResult_IsNotAcknowledgedBeforeCompletion_AndIsNotPlayedTwice`, `RefreshFailure_Propagates_WithoutAcknowledging`, `Finished_ReleasesInputOnly_AndStillAcknowledgesTheLastDelayedResultOnce` |
| 설치 실패·새 Mapper·늦은 사건 | `CombatSceneEventPresenterTests.FirstInstall_BindsAfterBuild_AndLateEventsAfterReleaseCannotRenderOrComplete`, `BuildFailure_DropsOldEventBinding_EvenWhenOldFieldStillExists`, `ClearDuringMovement_ReinstallUsesNewMapper_AndSameSceneResources` |
| 공유 Range·고스트·원본 흐림 | `CombatSceneEventPresenterTests.Reinstall_EndsMovementPlanning_GhostOpacityAndStaleRangeDoNotSurvive`, `CombatPointerMappingTests.DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange` |
| 비용 슬롯 교체·체력 미리보기 독립 정리 | SD-C′.1 회귀, `AttackHealthPreviewDisplayTests`, `CombatViewSystemTests.AttackHealthPreview_ResultBeginRestoresBeforePlay_AndDelayedTickDoesNotClearAgain` |
| 차단 벽 표시 | `WallAttackInteractionTests`(GridOutline·CellPrefab) |
| 해제 뒤 입력 피드백·UI 연결 해제 | `CombatPointerMappingTests.AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed`, `Release_ClearsReferences_AndLateCallDoesNotRaycastOrRecreateData` + SD-C′.1의 비용·벽·영향 칸 |
| 피드백 실패 중 정리 | `CombatPathInputCleanupTests.ReturnCleanupFailure_StillClearsDraft_AndIgnoresLateRelease`, `CombatViewSystemTests.StopInput_ContinuesCleanupAfterPartialFailure_AndReportsBoth` |
| 풀·UI 표시 재사용 | 관련 `CombatDisplayReuseTests` |

A와 B는 재생 알고리즘을 바꾸지 않는다는 전제를 유지한다. 해당 부분에 변경이 생기면 설계와 Phase 범위부터 다시 확인한다.

## 7. 실행 기록

### 7.1 계획 작성

- 2026-10-14: 사용자 요청에 따라 합의한 방향을 설계·Task 계획으로 작성했다. 작성 세션의 독립 검토 3건의 공통 판단인 단일 창구 유지와 부분 개선을 반영했다. 그 검토의 원문은 저장소에 남아 있지 않다.
- 런타임 코드·테스트·씬·에셋 변경은 없다. A·B·C0·C1은 미착수이며, 이 문서 작성은 기존 R단계의 검증 완료를 뜻하지 않는다.

### 7.2 검토 반영 (2026-10-14, 문서만)

- **경위:** 사용자 요청으로 문서 검토를 했다. 이어서 독립 검토 에이전트 3개(전체 구조·SD-A/B 수명 정확성·입력 피드백)를 돌렸고, 각 검토의 핵심 근거를 Claude가 코드에서 다시 확인했다. 사용자가 결론대로 고치기를 승인했다("오케이 고쳐").
- **문서 대조 결과:**
  - 맞음: SD-A·SD-B의 전제, 인용한 테스트 19개, C0의 필드·메서드 이름 8개, 참조 문서 경로, JMH §26·§27.
  - 낡음:
    - D12를 "별도·미승인"으로 적음(실제로는 완료).
    - SD-0.1의 R 진행 관측.
    - SD-A.2의 "가시밭 프로필이 연결된 fixture" 조건. D12 뒤에는 운영 DB에서 항상 보관 루트가 생긴다.
- **반영한 결정:**
  - **Facade 유지·A → B 순서**는 세 검토 모두 동의했다.
  - **SD-A**
    - 검사 조건을 `_display != null`로 못박았다(`IsAttached`는 재생 중단 뒤 거짓이 되어 결함이 다시 생김).
    - 공통 검증 메서드를 의무로 했고, 위치는 "기존 검사 뒤·생성 직전"이다.
    - 실제 중복 경로(Install 실패 뒤 재시도)를 근거로 적었다.
    - 생성자 부작용 제거 대안은 기각했다(재설치 때 고스트 풀 파괴, Install 전 `ShowGhost` 예외, D12 코드 침범).
    - 유닛·손패 `Configure`도 공유 컴포넌트를 덮어쓴다는 점(같은 표라 무해)을 보완했다.
    - fixture를 `CombatDisplayReuseTests`의 실제 씬 경로와 부작용 기준 단언으로 바꿨다.
    - Lobby·StageMap과의 정책 차이와 남은 위험 두 가지를 기록했다.
  - **SD-B**
    - 계약 축소 뒤 `CombatSceneEventPlayback`도 타입상 넘길 수 있게 되는 위험을 주석으로 막는다.
    - 재생 계약만 구현한 가짜(멤버 13개 중복)는 빼고, 같은 객체 전달(`Is.SameAs`)과 재생 경로의 해제 뒤 거절 검증으로 바꿨다.
    - `ICombatMovementPresenter` 정리는 선택 항목으로 더했다.
  - **SD-C**
    - 새 클래스 분리는 보류했다. 직접 구현 13개 중 Display 자신의 상태는 `_hudPreviewActor` 하나뿐이고, 나머지는 공유 자원 조정이거나 전달이다. 그래서 새 클래스는 전달 계층만 늘린다(위임 44 → 최대 57, 변경 지점 4곳).
    - 대신 비용 미리보기 대상을 `FieldUnitPresenter`로 옮기는 SD-C′를 넣었다. `FieldUnitPresenter`는 숨김·체력·투명도 대상을 이미 스스로 기억한다.
    - 쓰지 않는 벽 중복 집합을 지운다(`FieldHighlightState`가 이미 칸마다 한 번만 표시).
    - 비용 슬롯 회귀를 먼저 추가한다.
    - C0는 짧은 확인으로 줄였다.
  - **착수 조건:** R0~R4·D12 커밋 뒤에 시작한다.
- 코드는 바꾸지 않았다.

### 7.3 독립 재검토 보완 (2026-10-14, 문서만)

- 사용자 요청으로 에이전트 3개가 서로의 결과를 공유하지 않고 최신 문서와 미커밋 코드를 검토했다. 기존 방향을 유지하고, 확인된 문서 보완을 사용자 지시에 따라 반영했다.
- 전체 View 계획 §7.78의 안내를 SD-C′ → SD-C0, SD-C1 보류 방향에 맞췄다.
- 설계 §5.3에서 Display의 비용 대상 상태 이전과 공용 비용 슬롯 유지를 구분했다. 이동 표시의 추가 분리를 재평가할 때도 슬롯 교체·정리 계약을 보존하도록 SD-C0.2에 반영했다.
- 설계 §6에 해제 뒤 `ShowDevLayerPanel`은 예외를 던지고, 패널 숨김·UI 연결 해제는 허용하는 기존 계약을 명시했다.
- 런타임 코드·테스트·씬·에셋 변경과 빌드·Unity 테스트 실행은 없다. 각 Phase의 구현 상태와 착수 조건은 유지한다.

### 7.4 SD-A 연결 전제 검사 (2026-10-14, 코드·MSBuild·검토 완료·Unity 확인 대기)

- **착수 확인:** HEAD `5f6b315e8d7cfd106402157fd5618a589ddb04e1`에 R0~R4·D12 코드·테스트·DB 에셋이 함께 들어 있다. 기존 R계획의 Unity 확인 기록을 인수했다. 시작 시 미커밋은 §7.3에서 보완한 문서 3개뿐이고, 운영 코드·테스트는 HEAD와 같았다. 대상 파일을 디스크에서 다시 읽고 수정했다. 외부 IDE 버퍼를 다시 여는 조작은 하지 않았다.
- **기존 실패 확인:** 새로 실행한 Unity 결과는 없다. 아래의 수정 전·후 MSBuild는 통과했으며, 실제 씬 회귀의 실행 상태는 대기로 남긴다.
- **운영 코드:**
  - `CombatViewSystem.Attach`: 기존 null·오류 정지·초기화·활성 전투 검사 뒤, `new CombatSceneDisplay` 직전에 `ThrowIfDisplayAttached()`를 호출한다.
  - 같은 메서드를 `AttachDisplay`의 기존 중복 검사 자리에서도 쓴다. 조건 `_display != null`, 예외 형식·문구·나머지 검사 순서는 유지했다. `IsAttached`를 기준으로 삼거나 자동 Detach를 추가하지 않았다.
  - `CombatSceneDisplay` 생성자에 SceneRoot 하나당 살아 있는 Display 하나와 생성 부작용 전제를 주석으로 명시했다. 생성자 동작은 그대로다.
- **회귀:** `CombatDisplayReuseTests.AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources` 2케이스(`stopInput=false/true`). 정상 Attach·Tick 후와 직접 `StopInput` 후를 각각 대조한다. 고스트 활성 참조·GameObject, 가시밭 보관 루트 1개, 연결 상태·입력 Layer·수신 슬롯 보존, Detach 뒤 고아 루트 없음, 같은 root의 재연결을 검사한다. 입력 정지 케이스는 `IsAttached=false`여도 Display가 남는 경계를 지킨다.
- **fixture:** 기존 `AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`의 실제 씬·View DB 조립을 같은 파일의 `AuthoredCombatSceneFixture`로 공유했다. 원래 행동 단언은 유지했다. 이미 열린 Combat 씬은 Ignore하고, 기존 EventSystem 활성 상태와 current를 복원한다. 조립·정리 실패 때도 씬·이펙트·색 자원과 EventSystem 정리를 모두 시도한다.
- **MSBuild:** `ProjectL.Tests.Framework.csproj`와 의존 어셈블리를 `Temp/CombatSceneDisplaySDA/`에 출력했다. 새 회귀를 추가한 뒤 운영 코드 수정 전 빌드와 수정 후 빌드 모두 오류 0. 최종 빌드는 기존 다른 파일 경고 15건, 변경한 C# 파일 경고 0건이다. 테스트 초안의 `CharacterData` 속성 이름과 지원하지 않는 NUnit `Assert.Multiple` 사용은 운영 코드 수정 전 컴파일 단계에서 바로잡았다.
  - 로그: `Temp/CombatSceneDisplaySDA/baseline-build.log`, `final-build.log`. `NewFiles.targets`는 프로젝트별 중간 산출물 경로만 분리한다. 새 Compile 항목이나 csproj 수작업 수정은 없다.
- **Unity 확인 대기:** 이 프로젝트를 연 Unity 6000.0.73f1 프로세스와 `Temp/UnityLockfile`을 확인했다. 열린 프로젝트에 별도 배치를 실행하지 않았고 에디터를 종료하지 않았다. 수정 전 회귀 실패·수정 후 통과와 `CombatDisplayReuseTests` 전체, 지정한 `CombatViewSystemTests` 3건은 아직 실행하지 않았다. [SD-A 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-a-worksheet.md)를 따른다.
- **정책·범위:** Lobby·StageMap의 자동 Detach와 달리 전투는 명시적 Detach 전 재연결을 거절하는 기존 정책을 유지한다. Install 실패 뒤 Detach 없이 재시도해도 `_display`가 남아 있으므로 생성 전에 거절한다. 생성 뒤 소유권 인수 전 `AttachDisplay` 예외 시 해제 누락, `_display=null`로 정상 반환한 뒤 `_sceneRoot`가 구독될 가능성은 설계 §4.1의 남은 위험으로 유지하며 이번에 일반 롤백을 추가하지 않았다. 위험 설명의 정확한 범위는 C0에서 보완했다(§7.7).
- **원칙 점검:** 중복 조건·오류 문구는 한 메서드(SSOT). 조립·입력·재생·자원 반환의 기존 책임과 검사 우선순위 유지(SRP·LSP). 서비스·모듈·Builder·UI 입력 경로 변경 없음(FW 계약). Core·FieldCamera·Architecture·씬·에셋 변경 없음.
- **Phase 검토:** 구현·MSBuild 뒤 새 에이전트 `sd_a_phase_review`가 읽기 전용 검토를 완료했다. 조치가 필요한 문제 없음. 생성 전 검사·기존 검사 우선순위·공통 오류 조건, 회귀 2케이스의 판별력, fixture의 사용자 씬·EventSystem 보호와 실패 정리, 범위 준수를 확인했다. 검토자는 테스트를 실행하지 않았으며 수정 전 실패 재현·Unity 회귀·실제 진입과 재진입은 남은 검증으로 보고했다.
- **다음 범위:** SD-B의 재생 Attach 계약 축소. SD-A 보고 뒤 멈추며 이번 작업에서 SD-B는 구현하지 않는다.

### 7.5 SD-B 재생 연결 계약 축소 (2026-10-14, 코드·MSBuild·관리 회귀·검토 완료·Unity 확인 대기)

- **착수 확인:** 사용자의 "SD-B 진행하고 똑같이 에이전트로 검사해" 지시에 따라 진행했다. HEAD는 §7.4의 `5f6b315e`이며, SD-A와 문서의 미커밋 변경을 포함해 디스크 상태를 다시 읽었다. SD-B 대상 C# 6개는 시작 시 HEAD와 같았다. SD-A의 운영 코드 2개와 회귀 파일은 변경 전후 해시가 같으며, SD-A Unity 검증 대기도 유지한다.
- **운영 코드:** `ICombatPresentation`·`OrderedCombatPresentation`의 `Attach` 인자를 `ICombatEventPlayback`으로 좁혔다. 주석에 해제 뒤 사건 시작을 거절하는 Display를 넘겨야 한다는 계약과 `CombatSceneEventPlayback`의 Display 내부 전용 용도를 적었다. `CombatViewSystem.AttachDisplay`는 기존대로 `_presentation.Attach(display)`를 호출한다. 재생 알고리즘·취소·Attach/Detach 순서와 인터페이스 상속 구성은 그대로다. 선택 항목 `ICombatMovementPresenter` 삭제는 하지 않았다.
- **회귀 작성:**
  - `FakeCombatPresentation.LastAttachedPlayback`은 마지막으로 받은 객체를 기록한다. `CombatViewSystemTests.Attach_PassesSameDisplayToPresentation` 1건에서 기존 Rig의 Display와 같은 객체인지 확인한다. 새 계약 전용 가짜는 만들지 않았다.
  - 기존 `CombatSceneEventPresenterTests.Explosion_OrderedResultCompletesWhileLegacyParticlesAreStillPlaying`의 정상 완료 단언 뒤 Display를 Release하고, 연결을 유지한 `OrderedCombatPresentation.Play`가 `[CombatSceneDisplay]` 예외를 던지며 완료 횟수를 늘리지 않는지 확인한다. 앞선 재생은 완료됐으므로 진행 중 재생 거절과 구분되고, 먼저 Detach해서 단순 미연결 예외로 통과하지 않게 했다.
- **수명 방어:** 운영의 실제 방어선은 Detach 순서(재생 Detach가 Display Release보다 먼저)다. 이번 회귀는 연결이 남아 있어도 Display의 해제 확인을 통과하는지 추가로 지킨다. 내부 재생 객체를 외부에 넘기거나 해제 정책을 바꾸지 않았다.
- **MSBuild:** `ProjectL.Tests.Framework.csproj`와 의존 어셈블리를 `Temp/CombatSceneDisplaySDB/`에 출력했다. 오류 0, 기존 경고 16건, 새 경고 0건이다. 기존 `FakeDisplay.PointerOverUi` 경고는 수정한 테스트 파일 안에 있으나 이번 변경에서 생긴 경고가 아니다. `NewFiles.targets`는 프로젝트별 중간 산출물 경로만 분리하며, 새 Compile 항목이나 csproj 수정은 없다. 로그: `Temp/CombatSceneDisplaySDB/build.log`.
- **관리 회귀:** Unity의 Mono로 새로 빌드한 테스트 DLL의 아래 메서드를 직접 호출해 **4/4 통과**했다. 이 결과는 Unity Test Runner 실행과 구분한다. 실행기와 로그는 무시된 스크래치 `Temp/CombatSceneDisplaySDB/ManagedRegressionRunner.cs`, `managed-regression.log`에 있다.
  - `CombatPresentationCleanupTests.DelayedEvent_CancelRejectsOldCallback_AndNextResultCompletesOnce`
  - `CombatPresentationCleanupTests.Cancel_ReentrantCleanup_DoesNotRecursivelyCancelAgain`
  - `CombatPresentationCleanupTests.MovementCancelFailure_StillCancelsOtherPresentation_AndCollectsFailures(false/true)`
- **Unity 확인 대기:** 새 같은 객체 전달 회귀와 수정한 실제 씬 회귀, 지연 결과 확인·정상 종료 및 재연결 회귀는 아직 Unity에서 실행하지 않았다. 이 프로젝트를 연 Unity 6000.0.73f1 프로세스와 잠금 파일을 확인했으며 별도 에디터 배치를 실행하지 않았다. [SD-B 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-b-worksheet.md)에 실행 대상을 적었다.
- **원칙 점검:** 재생 소비자의 의존성을 사건 재생 계약으로 제한했다(ISP·DIP). 실제 Display 전달·해제 방어·취소 계약은 유지한다(LSP). 수명 상태를 새로 복제하지 않았고, 테스트 기록 속성은 동작을 결정하지 않는다(SSOT). 서비스·모듈·Builder·UI 입력 경로 변경 없음(FW 계약). Core·FieldCamera·Architecture·씬·에셋 변경 없음.
- **Phase 검토:** 구현·가능한 검증·기록 정리 뒤 새 에이전트 `sd_b_phase_review`가 읽기 전용 검토를 완료했다. 조치가 필요한 문제 없음. 축소한 계약의 구현·호출 적합성, 실제 Display 전달, Release 뒤 Play 회귀의 판별력, 취소·Detach 순서, SSOT·SOLID·FW 계약과 범위 준수, 관리 회귀와 Unity 검증 구분을 확인했다. 검토자는 코드를 수정하거나 테스트를 실행하지 않았으며 워크시트의 Unity 회귀는 남은 검증으로 보고했다.
- **다음 범위:** SD-C′의 비용 미리보기 대상 상태 이전과 중복 벽 집합 제거. 이번 작업에서 시작하지 않고 SD-B 결과를 보고한 뒤 멈춘다.

### 7.6 SD-C′ 비용 미리보기 대상 이전 (2026-10-14, 코드·MSBuild·관리 회귀·검토 완료·Unity 확인 대기)

- **착수 확인:** 사용자의 SD-C′ 진행 지시에 따라 HEAD `5f6b315e`와 SD-A·B의 미커밋 변경을 포함해 대상 파일을 디스크에서 다시 읽었다. `CombatSceneDisplay`의 SD-A 생성자 주석은 보존했고, 나머지 SD-A·B C# 파일은 착수 시 해시와 같다. 이번 변경은 운영 코드 2개·테스트 2개다. SD-A·B의 Unity 검증 대기는 유지한다.
- **회귀 먼저:** `CombatPointerMappingTests`의 기존 실제 씬 fixture에 아래 4건을 추가한 뒤, 운영 코드를 바꾸기 전에 테스트 어셈블리 컴파일을 통과했다. 비용 슬롯의 실제 Image 알파와 체력 텍스트·Fill, 필드 강조 상태를 확인한다. Unity에서 수정 전 통과를 확인한 것은 아니다.
  - `AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview`: 공격 A 비용을 이동 초안 B가 교체한다.
  - `ClearAffectedCells_ClearsCostAndAffectedLayer_PreservesHealthPreview`: 영향 칸·비용만 지우고 체력 예상 표시를 유지한다.
  - `ClearDraft_ClearsCostAndDraftLayer_PreservesHealthPreview`: 초안·호버·비용을 지우고 체력 예상 표시를 유지한다.
  - `AfterRelease_LateAttackFeedback_DoesNotChangeReattachedScene`: Release 후 같은 root에 새 Display를 설치한다. 이전 Display의 비용·벽·영향 칸 호출이 새 표시를 바꾸지 않는지 확인한다. 해제된 하위 객체가 비어 있어 우연히 통과하는 경우를 피했다.
- **상태 이전:** `FieldUnitPresenter`가 `_hudPreviewActor`를 소유한다. `ShowHudCostPreview`는 이전 비용을 지운 뒤 새 대상을 기억하고, 인자 없는 `ClearHudCostPreview()`는 기억한 대상을 비운 뒤 비용만 지운다. Presenter 자체 Release에서도 비용 정리를 시도하며, 실패해도 다른 반환을 계속하고 기존 오류 수집에 합친다. 체력 미리보기 정리와 비용 정리는 독립이다.
- **Display:** `_hudPreviewActor`와 자체 비용 정리 helper를 없애고 Presenter API를 호출한다. 표시할 이동 견적이 없을 때도 비용 슬롯을 지우며, 기존 공개 입구의 해제 확인을 유지한다. Clear·Release 목록의 비용 정리 위치는 그대로다. `_blockedWallSet`을 제거하고 접촉별 벽 칸을 목록으로만 편다. 중복 칸은 기존 `FieldHighlightState.SetLayerCells`에서 칸마다 한 번만 반영한다.
- **Presenter 회귀:** `AttackHealthPreviewDisplayTests`의 기존 비용 정리 호출을 인자 없는 API로 맞췄다. `HudCostPreview_ReplacesPreviousActor_AndClearsWithoutChangingHealth` 1건을 추가해 Display 없이도 대상 교체와 반복 비용 정리가 체력 예상 표시를 보존하는지 확인한다. Unity 실행은 대기다.
- **MSBuild:** `ProjectL.Tests.Framework.csproj`와 의존 어셈블리를 `Temp/CombatSceneDisplaySDC/`의 `baseline-bin/`·`bin/`에 출력했다. 운영 코드 수정 전·후 모두 오류 0, 기존 경고 16건, 새 경고 0건이다. 로그는 `baseline-build.log`, `final-build.log`. `NewFiles.targets`는 중간 출력 경로만 분리했고 csproj나 Compile 항목은 바꾸지 않았다. 착수 시 운영 코드 사본은 같은 스크래치의 `baseline/`에 보관했다.
- **관리 회귀:** 아래 `FieldHighlightStateTests` 4건을 Unity Mono에서 SetUp 후 직접 호출했다. 수정 전·후 모두 **4/4 통과**했으며, 목록 중복 제거·층 독립 정리·이동 경로 보존의 기존 근거다. 새 HUD·실제 씬 회귀의 실행 결과는 아니다. 실행기와 로그는 같은 스크래치의 `ManagedRegressionRunner.cs`, `baseline-managed-regression.log`, `final-managed-regression.log`에 있다.
  - `SetLayerCells_ReportsOnlyAddedOrRemovedCells`
  - `MovementPath_PreservesOrderAndCopiesInput_ReorderNotifiesRenderer`
  - `MovementPath_ClearAndReset_DropPathWithoutOtherLayers`
  - `IsActing_OnlyForActionLayersOrPlacement`
- **Unity 확인 대기:** 프로젝트를 연 Unity 6000.0.73f1 프로세스와 잠금 파일이 계속 활성이다. 별도 에디터 배치를 실행하지 않았다. 새 회귀 5건과 기존 HUD·벽·포인터·ViewSystem 체력 회귀, GridOutline/CellPrefab의 벽 표시와 공격 비용 → 이동 초안 → 취소 재생은 [SD-C′ 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-c-prime-worksheet.md)를 따른다.
- **원칙 점검:** HUD View와 비용 대상 상태의 주인을 Presenter 한 곳으로 모았다(SSOT·SRP). 비용·체력의 독립 정리, 공격·이동 공용 슬롯, Release 뒤 무시 계약을 유지한다(LSP). 새 타입·인터페이스·해제 플래그는 없다(JMH §26·§27). Buffer·Core 규칙 재계산, 서비스·모듈·Builder·UI 입력 경로 변경 없음(FW 계약). Core·FieldCamera·Architecture·씬·에셋은 수정하지 않았다.
- **Phase 검토:** 구현·가능한 검증·기록 정리 뒤 새 에이전트 `sdc_phase_review`가 읽기 전용 검토를 완료했다. 조치가 필요한 문제 없음. 비용 대상 소유권·교체·유효하지 않은 초안 정리, HP/AP 독립, Release 실패 수집·공개 해제 방어, 벽 중복 위임, 회귀 판별력과 fixture의 AP/HP·HUD 프리팹 가정, SSOT·SOLID·FW 및 범위 준수를 확인했다. 검토자는 코드를 수정하거나 테스트를 실행하지 않았으며 새 회귀 5건과 관련 Unity 회귀·두 표시 모드 재생은 남은 검증으로 보고했다.
- **다음 범위:** SD-C0의 남은 직접 구현 평가와 문서 기록. 이번에는 시작하지 않고 SD-C′ 결과를 보고한 뒤 멈춘다. SD-C1은 보류한다.

### 7.7 SD-C0 남은 직접 구현 평가 (2026-10-14, 평가·문서·에이전트 4개 검토 완료)

**이 절은 최초 평가의 이력이다. 현 구조 유지 결론과 새 상태가 생길 때까지 이전을 미루는 조건은 §7.8의 재평가로 대체됐다. 상태 소유권 관측과 기존 구현·검증 이력은 유지한다.**

- **평가 기준:** 사용자 지시 "SD-C0 평가 진행해, 하위 에이전트 4개 추가로 돌리자"에 따라 진행했다. HEAD `5f6b315e`와 미커밋 SD-A/B/C′를 포함한 디스크 코드가 기준이다. C0는 문서 작업이므로 운영 코드·테스트·씬·에셋을 수정하지 않고 빌드나 테스트도 다시 실행하지 않았다.
- **독립 평가 3건:** `sdc0_ownership`(상태 소유권·전체 책임), `sdc0_movement`(이동 초안 이전 대안), `sdc0_lifecycle`(공유 자원·수명·회귀 근거)가 서로의 결과를 제공받지 않고 검토했다. 셋 모두 현재 구조 유지와 C1 보류를 권고했으며, 이번 범위에서 새로 조치할 결함은 보고하지 않았다. 부모가 실제 코드와 호출·정리 경로를 대조해 설계 §5.3에 반영했다.
- **평가 결과:** Display의 가변 정보는 수명 플래그와 벽 칸 임시 목록이다. 비용·체력·숨김 대상, 입력 초안 캐시, 이동 편집 수명과 실제 자원 반환은 기존 소유자에 있다. Display에는 씬 수명·공유 대상 조정, 무상태 표시 변환, 해제 뒤 호출 방어가 남는다. "단순 위임만 남음"이나 "게임 사실이 없으므로 무조건 God Class 아님"으로 단순화하지 않았다.
- **이동 대안:** `ShowDraft`·`ClearDraft`만 기존 이동 Presenter로 옮기는 안은 응집도 면에서 가능한 후속 선택이다. 현재는 이전할 상태·중복·실패 경계의 개선 근거가 작아 유지한다. 전체 이전은 HUD와 Pointer 의존을 추가한다. 기존 이동 Presenter의 Clear가 Install 직전·실패에도 호출되고 초안 정리는 입력에서 별도 단계라는 점을 기록했다. 메서드를 옮기더라도 정리 수명·`_editing` 전제를 임의로 넓힐 수 없다.
- **문서 변경:** 설계 §5.3에 남은 직접 구현 표·대안 비교·재평가 조건을 적었다. §1·§4.1·§5.1의 착수 전 분석은 과거 관측임을 명시했다. [SD-C0 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-c0-worksheet.md)를 추가했다.
- **최종 검사 보완:** 네 번째 에이전트가 기존 Attach 잔여 위험의 문구가 과도하게 넓다고 지적했다. 부모가 코드와 `InstallFailure_Propagates_WithoutBindingInput`을 대조해 설계 §4.1·§5.3 및 §7.4 기록을 정정했다. 해제 누락은 `_display` 소유권 인수 전 예외 위험이며, 인수 후 Install 실패는 Detach로 반환할 수 있다. `_sceneRoot` 구독은 정상 반환 뒤에만 실행되므로 `_display=null` 정상 반환 가능성(운영 경로 미발견)은 별도 잠재 위험으로 구분했다. 런타임 수정은 없다.
- **원칙 점검:** 단일 비용 대상·필드 강조 상태·입력 캐시·자원 반환의 기존 소유권을 유지한다(SSOT·SRP·FW). 공개 해제 계약과 좁은 월드 좌표 입력을 보존한다(LSP·ISP). 기존 클래스 이전을 JMH §27 위반으로 자동 판단하지 않고 실제 이득과 의존성·수명 비용을 비교했다. 신규 클래스·서비스·인터페이스·해제 플래그를 제안하지 않는다.
- **네 번째 에이전트/Phase 검사:** 평가 문서 반영 뒤 새 에이전트 `sdc0_phase_review`가 코드·문서를 대조했다. 위 위험 문구 보완 1건을 반영한 뒤 재검사를 받았고, 조치가 필요한 문제 없음으로 종료했다. 현재 구조 유지·C1 보류를 바꿀 근거는 없으며 Unity 검증 대기는 그대로임을 확인했다. 이 검사를 포함해 하위 에이전트는 총 4개이며, 앞선 세 독립 평가와 최종 작업물 검사를 구분한다. 검토자들은 파일 수정·빌드·테스트를 실행하지 않았다.
- **최종 확인:** 평가 전후 기존 미커밋 C# 12개 해시가 일치하며 `git diff --check`가 통과했다. C0에서는 설계·계획·워크시트만 갱신했다.
- **남은 일:** 구조 평가 범위는 C1을 보류한 채 마무리한다. SD-A/B/C′ Unity 회귀와 C′의 두 표시 모드 재생은 §7.4~7.6대로 대기다. 선택 항목 SD-B.5와 설계 §4.1의 소유권 인수 전 예외·별도 구독 위험은 이번에 구현하거나 완료 처리하지 않았다. 새 기능이나 수명 요구가 생기면 설계 §5.3의 조건에 따라 후속 계획을 검토한다.

### 7.8 기능 변경의 독립성 추가와 조립·기능 구현 재평가 (2026-10-14, 문서 작업)

- **요청과 기준:** 사용자가 SOLID 5원칙·SSOT·JMH/FW에 "조립 분리를 통해 하나의 기능을 수정할 때 다른 기능을 불필요하게 건드리거나 확인할 필요가 없게 한다"를 추가했다. 적용 기준 원본은 [설계 인계 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md)이며 root `AGENTS.md`에서 참조한다. 상태 중복이 없다는 관측만으로 현재 구조를 유지했던 판단을 보완한다.
- **검토 기준 코드:** HEAD `5f6b315e`와 미커밋 SD-A/B/C′를 포함한 디스크 코드. 아래 세 에이전트는 대화 이력을 물려받지 않고 동일 요청·기준·대상 경로만 받은 채 서로의 결과를 공유하지 않고 읽기 전용 검토를 했다.

| 에이전트 | 독립 검토 축 | 주요 판단 |
|---|---|---|
| `composition_boundary_review` | 조립·자원 소유·실패 경계 | 구체 Factory 권고. 생성·Configure·연결을 분리하되 Install/Release를 Factory에 넣지 않음. Create 반환과 `_display` 인수 사이의 정리 담당을 명시할 필요 |
| `feature_implementation_review` | 기능별 직접 구현 | 이동은 기존 Presenter, 배치·공격은 응집된 기능 객체 권고. UI/Mapper/재생은 기존 분리 유지. Refresh 전체를 새 큰 객체로 옮기는 것은 권하지 않음 |
| `change_scope_review` | 수정·이해·검증 영향 범위 | 조립만 분리하면 기능 내부 변경의 범위는 크게 줄지 않음. 기능별 fixture와 공유 계약의 통합 회귀를 구분해야 함 |

- **종합 방향:** 세 검토 모두 단일 Display 입구와 A/B/C′는 유지하면서 현 구조 유지 결론을 수정할 것을 권고했다. 부모가 코드·호출자·검증 fixture와 대조해 설계 §3·§5.3·§5.4·§7에 반영했다. Display에는 공개 계약·공통 수명 순서를 남기고, 생성·설정은 전용 Factory, 이동·배치·공격의 실제 표현 구현은 해당 기능으로 모은다. 전체 입력 피드백을 한 클래스에 옮기는 C1은 다른 기능을 다시 한곳에 모으므로 계속 보류한다.
- **우선순위 차이:** 조립 검토는 소유권 인계를 확정한 Factory Phase를 먼저 권했고, 기능 검토는 기존 이동 Presenter 이전을 첫 기능 Phase로 권했다. 두 작업의 필요성에는 이견이 없다. 현재 설계는 조립 경계 설계와 전용 Factory를 우선하는 방향을 적었으며, 상세 생성자·의존성 묶음과 Phase 순서는 후속 Task에서 확정한다. 거대한 `Parts` 가방을 최종 구조로 남기지 않는다. Refresh의 호출 순서는 유지하고 개별 표시 변환은 후순위로 평가한다.
- **추가 확인:** `CombatSceneRoot.ValidatePresentationTiming`은 `MovementStepSeconds`를 검사하지 않고 `CombatSceneEventPresenter` 생성자가 Units/Hand/Ghost Configure 뒤에 검사한다. 조립 도중 실패가 부분 변경을 남길 수 있는 실제 코드 경로다. 설계 §5.3의 조립 전·조립 도중·소유권 인수 전·인수 후 실패 표에 기록했다. 자동 롤백 보장으로 해석하거나 이번에 고치지 않았다.
- **문서 연결:** root AGENTS, 원칙 원본 §4.6, 후속 설계, 이 계획, C0 워크시트와 전체 View 계획 §7.78을 맞췄다. 이전 평가 이력은 보존하되 현재 방향과 구별한다.
- **검증 범위:** 문서 작업이므로 MSBuild·Unity·테스트를 다시 실행하지 않는다. SD-A/B/C′의 기존 통과 기록과 Unity 검증 대기를 그대로 유지한다.
- **문서 최종 대조:** 독립 평가 완료 뒤 같은 세 에이전트가 각 검토 축으로 통합 문서를 다시 읽었다. 모두 추가 수정 사항 없음으로 보고했다. 이 검사는 최초 독립 평가와 구분하며 추가 에이전트는 만들지 않았다.
- **변경 보존·문서 확인:** 이번 검토 전후 기존 미커밋 C# 12개 해시가 모두 일치한다. 수정한 문서 6개의 로컬 링크에 누락이 없고 `git diff --check`가 통과했다. 런타임 코드·테스트·씬·에셋 변경은 없다.
- **다음 작업:** 구현 전 Task에서 ① Factory가 받을/만들 의존성과 같은 인스턴스를 공유할 대상, ② 조립 실패·소유권 인계 보완의 포함 범위, ③ 이동·배치·공격별 편집 파일과 기능/교차 회귀, ④ Phase별 완료 조건과 새 에이전트 검사 지점을 확정한다. 이 재평가를 구현 착수나 기존 Unity 검증 완료로 처리하지 않는다.

### 7.9 조립·기능 분리 Phase 계획 연결 (2026-10-14)

- 사용자가 재평가 방향에 동의하고 Phase별 Task와 각 Phase 종료 후 새 에이전트 검사를 요청했다. 현재 Task·실행 상태 원본은 [조립·기능 분리 계획](2026-10-14-combat-scene-display-composition-plan.md)이다.
- 설계 §5.5에 순수 조립 추출의 실패 처리 범위, 명시적 주입, 기능 객체 경계와 리뷰 절차를 구체화했다. 새 계획은 SD-D0 준비, D1 Factory, D2 이동, D3 배치, D4 공격, D5 통합 인수로 나눈다. 현재 모두 미착수다.
- 계획 작성 중 현재 HEAD가 `b751fc45a23d6ab7dd97862cdd8f1b2e22245a83`로 바뀐 것을 확인했다. 앞선 미커밋 상태는 당시 기록으로 보존한다. 커밋됐다는 이유로 A/B/C′ Unity 검증 대기를 완료 처리하지 않는다.

### 7.10 D5 사용자 전체 확인에 따른 현재 통합 검증 인수 (2026-10-15)

- 사용자가 D4 이후 현재 미커밋 코드의 Unity 확인을 "전체 확인함"이라고 답했고, 이어 EditMode와 GridOutline·CellPrefab 실제 전투 재생에 대해 **"모두 통과, 실패·Ignore 없음"**을 명시했다. 이를 A/B/C′의 현재 통합 검증 인수 출처로 기록한다.
- D1~D4 고유 선택 69개만으로 A/B/C′ 전체를 대체하지 않는다. 실제 씬 재생·같은 Display 전달, Explosion 재생 완료, 지연/종료/재연결, 비용·HP·포인터 회귀는 사용자 전체 확인 범위로 인수한다. 자동 수집한 테스트 총수나 XML은 없으며 에이전트가 Unity Test Runner를 직접 실행했다고 쓰지 않는다.
- SD-A.2의 수정 전 실패 재현, SD-C′.1의 수정 전 Unity 통과 및 D0 당시 기준선은 미확보 이력을 유지한다. 해당 체크를 현재 코드 통과만으로 완료 처리하지 않는다. §7.4~7.9의 당시 상태·검토 기록은 과거 사실로 보존한다.
- D5의 검증 출처·선택·구조 평가·최종 검사 상태는 [조립 계획 §10.7](2026-10-14-combat-scene-display-composition-plan.md)과 [D5 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d5-worksheet.md)를 따른다. 부분 Configure·Display 인수 전 실패 복구는 별도 후속이며 이번 확인으로 해결됐다고 쓰지 않는다.
