# 비전투 View 구조 개선 — 용근님 공유

- 작성자: 호진(이호진)

용근님께 드리는 비전투 View 구조 개선 최종 메모와 P3 아티팩트 흡수 사전 메모를 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-비전투-view-구조-개선--용근님-전투-view-공유-2026-10-15) | 비전투 View 구조 개선 — 용근님 전투 View 공유 (2026-10-15) |
| [2부](#2부-p3-아티팩트-view-흡수--용근님-공유-메모-2026-10-15) | P3 아티팩트 View 흡수 — 용근님 공유 메모 (2026-10-15) |

---

## 1부. 비전투 View 구조 개선 — 용근님 전투 View 공유 (2026-10-15)

- 상태: **최종 메모 작성 완료·미전달.** 사용자가 직접 전달한다. [P3 사전 메모](#2부-p3-아티팩트-view-흡수--용근님-공유-메모-2026-10-15)는 사용자 확인·직접 전달 예정 기록을 유지하며 실제 전달 완료는 확인되지 않았다.
- 원본: [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md) D4·D11·D15·D20, §6·§12와 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md). 둘은 2026-10-27에 옛 비전투 View 설계·계획을 합친 문서이며, § 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`).

**최신 코드 대조(2026-10-16):** HEAD `70de006629feaf880f876c7b8e41fa14297237cf`를 소스·문서로 대조했다. 아래 P3~P5 구조를 유지하며 이후 회복 연결과 일반 패배 목적지 변경을 구분한다. 기존 자동 검증·Play·Profiler 기록은 당시 증거이며 이번에 재실행하지 않았다. 전달 상태는 여전히 미전달이다.

P3 사전 안내대로 아티팩트 독립 View 모듈을 제거하고 `LobbyViewScope` 소유 `ArtifactPopupController`로 옮겼다. 현재 `InGameViewSystemFactory`는 Combat·StageMap·Lobby View와 별도 ER3에서 추가한 `RecoveryRequestLifetime`을 등록한다. 공용 오류 정리는 현재 아래 호출을 사용한다(생성자의 `lobby`·`recovery`는 선택 인자).

```csharp
new InGameViewFaultCleanup(faults, combatView.StopInput,
    stageMapView.StopInput, lobbyView.StopInput,
    recovery: recovery.StopInput);
```

정리 순서는 현재 **전투 → 맵/상점·회복 표시 → 로비 → 회복 요청 수명**이다. P3 당시 세 View의 선후관계와 전투 StopInput 첫 호출을 유지한다. 아티팩트 정리는 `LobbyViewSystem.StopInput`에 포함되고 씬 Scope가 없어도 자기 입력 Buffer를 비운다. `InGameViewFaultCleanup`은 캡처한 콜백을 null 검사한 뒤 `CleanupErrors.Try`로 실행하고 `ThrowIfAny`로 보고한다. R6 후속에서 private `TryStop`을 제거했으며 한 호출의 예외가 다음 정리를 막지 않는다. 전투 View·입력 Slot/Controller·연출·Core 파일은 비전투 구조 개선 대상이 아니었다. 이후 회복 요청 수명의 등록·정리 확장은 [회복 연결 계획 ER3](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)을 따른다.

P4에서 **로비 아티팩트 처리 번호 누락**은 해당 도메인만 로그 1회 후 정지하도록 바꿨다. `LobbyFlow`는 그 도메인의 확인/등록과 두 아티팩트 이탈 검사를 건너뛰어 출발 처리를 계속한다. 공용 오류·Runner·출발 자체의 이탈 조건은 유지하며 출발 보드는 Core 현재 상태를 따른다. View가 편집을 다시 제출하지 않는다. **전투의 F1 공용 오류 정지는 그대로**이고, 실제 Core/Job 예외를 단순 기록 누락으로 취급하지 않는다([정책 원본(옛 §6)](../specs/2026-10-13-view-structure-design-Hojin.md)).

로비 Enter 100에는 `LobbyViewSystem` 하나만 붙는다. [씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md#02-비전투-view의-씬-연결-수명)에 Scope 게시·현재 소유자 확인·Exit -100/StopInput 폐기 규칙을 반영했다. StageMap도 Scope가 표시·입력·상점 수명을 소유하며 StopInput에서 맵 표시까지 정리한다. 정상 Detach의 AMB 정지 책임은 System에 남는다.

**일반 패배**는 2026-10-16에, **자발적 포기**는 2026-10-20에 새 Run 준비 후 로비로 복귀하도록 연결됐다(`CombatPlan.RunEndDestination`). 최종 승리도 2026-10-20부터 같은 목적지(로비)이며, 정책 원본은 [전투 Job·Plan 설계 J7](../specs/2026-10-10-combat-job-plan-restructure-design.md#j7-이탈-연쇄-k21-유지)이다. 로비 재출발 때 새 Run 생성 위치 통합과 실제 재진입 표시 등 [LV10 전체](../plans/2026-10-11-lobby-plan-Hojin.md#311-lv10-run-종료-뒤-로비-복귀)는 후속이다. 별도 ER3~ER5 회복 구현과 ER6 검증·잔여 사람 확인은 위 회복 계획을 따른다.

같은 날 맵 이동의 Combat 진입 표시 대기는 [노드 이동 공유](2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md)와 그 원본 설계·계획을 따른다. P3.8의 즉시 게시·무Layer 조합은 [설계(옛 §9)](../specs/2026-10-13-view-structure-design-Hojin.md)의 승인된 검증 보존 예외이며, 과거 P3 검증 수치를 최신 전체 보장으로 확대하지 않는다. 이 문서 정리에서 새 API·Core 요청이나 제품 변경은 없다.

---

## 2부. P3 아티팩트 View 흡수 — 용근님 공유 메모 (2026-10-15)

- **상태:** 사용자 확인 완료·직접 전달 예정(2026-10-15). 실제 전달 완료는 확인되지 않았다. P3 코드 이전·자동 검증·사용자 Play 4항목 확인을 완료했다. 아래 줄 번호는 P3 시작 작업 트리의 위치다.
- **전달 담당:** 사용자가 직접 전달한다.
- **계획:** [View 구조 계획 P3(옛 비전투 View 구조 개선 계획)](../plans/2026-10-13-view-structure-plan-Hojin.md).
- **설계:** [View 구조 설계(옛 비전투 View 구조 개선 설계)](../specs/2026-10-13-view-structure-design-Hojin.md) D4·D15, §5.1·§12.
- **확인한 시작점:** `feature/Hojin/Stage_Shop_Event`, HEAD `92440b83862293eff276a9f62605d02b0176f718`와 현재 미커밋 P1·P2 작업 트리. P2 코드·자동 검증·사용자 Play 확인은 완료했다.
- **지금 상태(2026-10-16 대조):** 아래는 P3 사전 안내와 당시 검증 이력이다. HEAD `70de006629feaf880f876c7b8e41fa14297237cf`의 소스·문서 대조에서 P3 사전 계획·줄 번호·검증 수치를 당시 기록 그대로 유지했으며, 아래 수치는 최신 코드에서 새로 실행한 검증이 아니다. P4 기록 누락 정책 차이·P5 맵 Scope·최종 공용 연결, 이후 회복 요청 수명이 추가된 현재 Factory/정리 순서, R6·P2.5/P3.8 후속과 일반 패배 로비 복귀는 [용근님 최종 공유 메모](#1부-비전투-view-구조-개선--용근님-전투-view-공유-2026-10-15)와 그 원본 링크를 따른다. 새 메모도 미전달이며, 이 문서의 직접 전달 예정·실제 전달 미확인 상태를 전달 완료로 바꾸지 않는다.

### 공유 내용

아티팩트 View의 독립 모듈을 로비 Scope 소유 팝업으로 옮긴다. `ArtifactPopupViewFactory`·`ArtifactPopupController`·`ArtifactPopupViewScreen`을 만들고, 로비가 생성·Tick·폐기를 맡는다. 기존 `ArtifactViewSystem`·`ArtifactPopupSession` 일반 C# 파일과 meta는 이 단계에서 제거할 계획이다. 씬·프리팹, `ArtifactSceneRoot`·`ArtifactPopupView`·`ArtifactPopupUI`의 타입·파일 위치·네임스페이스, UI/Camera/Core/Buffer 계약은 유지한다.

이 과정에서 아래 **공용 파일 두 개**를 바꾼다. 병합 충돌을 피하도록 **P3 진행 중에는 이 두 파일의 동시 편집을 피해 주세요.**

| 공용 파일 | 현재 위치와 변경 계획 |
|---|---|
| `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | 현재 33행의 아티팩트 View 생성, 35행의 `lobbyView.BindArtifact(artifactView)`, 38행의 아티팩트 `ExportServices`, 42~43행 오류 정리 생성자의 아티팩트 인자를 제거한다. 필요 없어진 아티팩트 using도 제거한다. 로비의 기존 모듈 등록은 유지한다. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/InGameViewFaultCleanup.cs` | 생성자를 `(RuntimeFaultBoundary faults, Action combat, Action stageMap, Action lobby = null)`로 바꾼다. 아티팩트 필드·정리 호출을 제거하고, 정리 순서를 **전투 → 맵/상점 → 로비**로 둔다. 기존 정리 오류 수집을 P1의 `CleanupErrors`에 맡기되 오류 뒤 나머지 정리와 재진입 중 종료 처리는 유지한다. |

Factory의 변경 후 정리 호출은 다음 형태다.

```csharp
new InGameViewFaultCleanup(faults, combatView.StopInput,
    stageMapView.StopInput, lobbyView.StopInput);
```

전투 View 생성·서비스 등록·그룹의 전투 참조, `OrderedCombatPresentation`, 전투 StopInput의 **첫 호출 순서**는 그대로다. `CombatViewSystem`, `InGameViewSystemGroup`, 전투 입력 Slot/Controller, `CombatViewSystemTests`는 수정 대상이 아니다. 아티팩트 입력 정리는 `LobbyViewSystem.StopInput`으로 흡수하며 씬 Scope가 없어도 자기 입력·프리뷰 Buffer를 비운다. Core 숫자·처리 기록은 초기화하지 않는다.

### 전투 테스트 호출 호환

계획이 가리킨 `CombatViewSystemTests`:1125의 호출은 현재 작업 트리에서 **1283행**에 있다.

```csharp
Cleanup = new InGameViewFaultCleanup(Faults, View.StopInput, () => { }, () => { });
```

이 4인자 호출은 변경 후 `faults, combat, stageMap, lobby`에 들어가므로 **테스트 파일을 바꾸지 않아도 서명상 호환**된다. 마지막 두 인자는 기존과 같이 아무 작업도 하지 않는다. P3 네이티브 실행·19프로젝트 Rebuild에서 실제 컴파일을 확인했고, 최종 전체의 `ProjectL.Tests.CombatViewBoundary`도 9/9 통과했다. 전투 테스트 파일은 시작 원문 그대로다.

### 검증과 작업 경계

- P0·P1·P2 원본 XML과 실패 목록을 보존하고 P3 결과를 비교한다. P2 전체 기준선은 6,516건: 통과 6,434·기존 실패 81·Explicit 제외 1이다.
- 기존 테스트의 검증 내용을 유지하고 아티팩트 접근 경로만 새 Factory/Controller로 옮긴다. 기대값 변경은 승인된 외부 숨김/열림의 다음 Tick 처리(D10·D19)와 공용 정리 순서(D15)다.
- 아티팩트 할당 검사, 재진입/실패 기준 목록, 전체 EditMode, 별도 Explicit 상점, 컴파일·경고 비교와 사용자 Play를 확인한다.
- 이번 작업은 **P3까지**다. P4의 로비 기록 누락 정책과 LifeCycle 변경, P5 맵/상점 이전은 이번 변경에 포함하지 않는다.

최종 자동 검증: 전체 6,520건 중 통과 6,439·기존 실패 80·Explicit 제외 1, 새 실패 0. P2 대비 신규 4개 통과·기존 오류 정리 실패 1개 통과, 나머지 결과/실패 메시지 동일이다. P3 필터 884/888(기존 실패 4), 필수 재진입 27/27·할당 검사·별도 상점 1/1 통과, 빌드 오류 0·새 경고 0이다. 기존 테스트 이름 이동에 따른 경고 한 건의 위치 변경은 별도로 기록했다. 정적/자동 리뷰는 승인, 사용자도 Play 4항목에 “4항목 모두 통과”로 응답하여 P3 완료로 기록했다. 상세 근거·변경한 테스트 관찰의 의미와 한계는 계획의 P3 실행 기록에 있다.

### 사용자 확인 기록

P3.0은 이 메모를 사용자가 확인하고 전달 여부를 정한 뒤 P3.1을 시작하도록 정했다. 사용자는 2026-10-15에 “메모 확인 · 직접 전달 예정”으로 응답했다. 사용자 검토·전달 여부 결정 조건은 충족했다. 실제 전달 완료는 별도 확인되지 않았으며 Codex가 외부로 전달한 기록은 없다. 같은 응답에서 Unity 종료도 확인했다. 함께 제안한 P3 사전 대조 보완안은 사용자가 “기존 계획 유지 · 보완 재검토”로 답했으므로 반영하지 않고 기존 계획의 구현 경로를 재검토한다.
