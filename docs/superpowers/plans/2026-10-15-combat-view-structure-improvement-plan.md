# 전투 View 구조 개선 Task 계획 (SV, 2026-10-15)

- 작성: View 작업, 2026-10-15.
- 상태: **SV0~SV7 코드·Phase별 검토 완료(MSBuild 오류 0, 관리 실행 HEAD 대비 회귀 0). SV8 인수 문서 작성. 리팩토링은 여기서 종료(사용자 결정, §12.13), 사용자 Unity 확인 대기.** 사용자 결정(2026-10-15): "추천안대로 진행해, SV0부터 SV7까지 쭉 실행해", "SV 단위 작업 하나 끝날 때마다 서브에이전트 하나로 검토해". 실행 상태는 이 문서 §12에만 적는다.
- 결정 적용: Q2=A, Q3=A, Q5=A, Q6=A(추천안). **Q1은 사용자 지시로 C(전체 연속)** — 1단계 뒤 정지 없이 진행하고, Phase마다 새 검토 에이전트 1개로 대신 점검한다. **Q4는 Claude 직접 구현**("쭉 실행해"), Phase별 워크시트는 만들지 않는다(필요하면 뒤에 커밋 기준 줄 번호로 작성).
- 평가 원본: [전투 View 구조 평가](../specs/2026-10-15-combat-view-structure-evaluation.md)의 발견 사항 E1~E23과 §4.4 시나리오 S-A~S-H.
- 판단 기준: [설계 인계 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md). SOLID 5원칙·SSOT·FW/JMH·조립 분리·기능 변경의 독립성.
- 기준 코드: HEAD `4cfceaa4`, 작업 트리 깨끗.

## 1. 범위와 완료 목표

조정자 `CombatViewSystem`은 **Framework·씬 수명, 결과 소비 순서(재진입 확인 포함), 진행 계약**을 계속 소유한다. 정리 대상은 세 묶음이다.

1. **실패 경로 소유권**(E7·E15·E23·E17): 조립·설치 실패 뒤 반환·구독 해제·결과 소비 정지.
2. **판정식·계약 정리**(E5·E9·E12·E22): 의미를 바꾸지 않는 SSOT·ISP 정리.
3. **입력 쪽 조립·기능 분리**(E1·E2·E3·E4·E6·E16·E18·E19·E21): 조정자가 경로와 배치·공격을 따로 알지 않고, 입력 기능 생성은 Factory로 옮긴다.

| 변경 종류 | 완료 때의 목표 |
|---|---|
| 입력 기능 하나의 내부 동작 변경 | 해당 기능 클래스와 그 fixture만 수정·검증 |
| 입력 기능의 생성 인자·의존성 변경(조정자가 이미 받는 의존성 안에서) | 입력 Factory + 해당 기능. 새 Global 서비스면 PostBindService·BindServices·Rig도 바뀐다 |
| 결과 전·후처리에 참여하는 입력 기능 추가 | 입력 기능 묶음(`CombatInteraction`) + Router 대상. 조정자는 수정하지 않음 |
| 재생 계약 멤버 추가 | 선언 + Display + 가짜 + 실제 구현 위치 |
| 수명 조건 변경 | 조정자 안의 이름 있는 판정식 한 곳 |
| 공통 순서(결과 확인·Layer·정지) 변경 | 공통 수명 변경임을 명시하고 `CombatViewSystemTests` 통합 회귀로 검증 |

**지키는 것:** 줄 수·필드 수를 목표로 삼지 않는다. 새 거대 클래스·무타입 `Parts`/서비스 가방·후설정 setter를 늘리지 않는다(Factory 반환은 이미 있는 타입 하나씩). 입력 기능 묶음은 상태·정책 없이 고정 순서 위임만 갖고, 포인터 대상 선택은 Router 한 곳에 둔다. 상태·규칙 원본(Gate·Playback·Abandon·Buffer)을 복제하지 않는다. 내부 협력 객체를 Framework System·Service로 등록하지 않는다.

**포함하지 않는 것(평가 §0·§5):** Audio, VR6, `ICombatViewService` 축소, SD-C1, R6(응답 전달 재설계), `ICombatBattleDisplay` 해체(E8), 사건 Presenter·기능 내부 부품 생성 이전(E10), 활성 행위자 강조 분리(E11), E13·E14, Core·Buffer·씬·프리팹·DB 변경. 커밋·push·외부 전달은 사용자가 한다.

## 2. 사용자 결정 필요

| ID | 질문 | 선택지 | 추천 |
|---|---|---|---|
| Q1 | 진행 단계 | A: 1단계(SV1~SV3) 뒤 정지·재검토, 그 뒤 2단계(SV4~SV7) / B: 1단계만 / C: 전체 연속 | **A**. 1단계는 의미 보존·실패 경로라 위험이 낮고, 2단계는 결과 전후 호출 순서를 옮기므로 1단계 결과를 보고 다시 검토한다(검토자 1 권고) |
| Q2 | 행동 버튼 갱신 시점(E3) | A: Tick 끝에서 한 번 파생 + 입력 직후(`RouteInput`) 유지 + 정지·제출 정지·해제의 즉시 숨김 유지 / C: 지금 유지 | **A**. 호출 지점 지식이 사라져 새 상태는 규칙만 고친다. 키 입력과 행동 패널 버튼은 `RouteInput`으로 즉시 갱신되고, 개발 Layer 전환·포기 확인창 열기만 그리기 기준 0~1프레임 늦을 수 있다(잘못된 기록은 Policy의 Layer 검사와 확인창 입구 검사가 막음, SV6 검토 정정). D12를 뒤집는 것이 아니라 **적용 범위 보완**으로 적는다: "닫힌 사건(결과 게시·이탈 시작)으로만 바뀌는 값은 이벤트, 여러 입력 경로에서 바뀌는 View 내부 값은 Tick 파생"(선례: 구조 리팩토링 설계(2026-10-14) D11 타임라인 보임, 복귀 버튼 매 Tick 갱신). 초안의 B(단일 깔때기)는 지금의 `SyncActionButtons`와 같아서 뺐다 |
| Q3 | 미리보기 응답 전달 통일 | A: 구조만 옮기고 경로의 매 Tick 호출 동작 유지 / B: 이번에 통일 | **A**(세 검토 동의). B는 응답 소비·수명 감시·복귀 재요청을 나누는 R6 재설계라 따로 다룬다 |
| Q4 | 진행 방식 | A: Claude 직접 구현 + Phase마다 실습 워크시트 / B: 워크시트만, 사용자가 직접 구현 | 사용자 선택. SV2 판정식 표·SV6 프레임 순서는 워크시트로 남길 가치가 있고, SV3은 기계적이라 워크시트 실익이 작다 |
| Q5 | 설치 실패 뒤 Tick과 입력 객체 생성 시점(E23) | A: 입력 객체(Gate·Interaction·Router·스냅·경로)를 **Install 성공 뒤** 한 번에 만들고, 첫 그리기 성공 전에는 미리보기·결과를 소비하지 않음(D13 유지) / B: 입력 객체를 **Install 전**에 한 번에 만들고(경로 생성자는 부작용 없음) D13 문구를 "생성은 Install 전, 연결·사용은 Install 성공 뒤"로 바꿈 | **A**. D13을 유지하고 반쯤 설치된 표시를 건드리지 않는다. "Install 실패 뒤 결과를 소비하지 않음"은 동작 변경이지만 운영은 이미 오류 정지라 영향이 없다 |
| Q6 | 재생 계약 조각 정리 형태(E12) | A: 흡수 — 소비자 없는 조각 6개를 `ICombatEventPlayback`에 흡수, 소비자 있는 `ICombatMovementPlaybackPresenter`만 유지(새 파일 0, D6 문서 형식 갱신) / B: 분할 — 한 파일에 계약 하나(새 파일 5개 + .meta) | **A**. D6의 의도(숨은 형식 검사 제거·계약 명시)는 그대로이고 형식만 바뀐다. 설계 결정의 형식 변경이므로 사용자 확인 |

## 3. Phase 순서

| 단계 | Phase | 작업 | 발견 | 위험 |
|---|---|---|---|---|
| — | **SV0** | 착수 기준선·결정 반영 | — | 없음 |
| 1 | **SV1** | 조립·설치 실패 경로 소유권 | E7·E15·E23·E17 | 낮음(E23은 동작 변경, Q5) |
| 1 | **SV2** | 연결 활성 판정식 정리 | E5 | 낮음(의미 보존 표 필수) |
| 1 | **SV3** | 재생 계약 선언·계약 파일 정리 | E9·E12·E22 | 낮음(csproj 파일 목록 주의) |
| — | **정지** | 1단계 보고·사용자 Unity 확인·2단계 재검토 | — | — |
| 2 | **SV4** | 입력 기능 묶음 대칭화(경로 편입·Router 생성자·포인터 대상) | E2·E4·E18·E19 | **중간**(호출 순서) |
| 2 | **SV5** | 입력 기능 조립 Factory·표시 서비스 묶음 | E1·E16 | 낮음~중간(테스트 생성 호출) |
| 2 | **SV6** | 행동 버튼 갱신 시점 | E3 | 중간(Q2) |
| 2 | **SV7** | 복귀 요청 값 객체·감시 이름 분리(축소안) | E6·E21 | 낮음~중간 |
| — | **SV8** | 통합 인수·시나리오 전후 비교 | 전체 | — |

공통 리뷰 절차(SD-D와 같음): Phase마다 구현 → MSBuild(VS 2022 MSBuild + extra.targets) → 관리 실행기 대상 fixture → 새 Phase 검사 에이전트(읽기 전용) → 보고·정지. Unity EditMode·재생은 사용자가 하며 보고에서 직접 실행과 구분한다. 다음 Phase는 사용자 지시 뒤 시작한다.

## 4. SV0 — 착수 기준선

- HEAD·작업 트리·동시 편집 확인. 커서 스냅 감도(§7.79)처럼 병행 변경이 있으면 보존한다.
- **리플렉션 이름 보존 목록:** `CombatViewSystem._combatQueries`·private 메서드 `OnCombatSceneDisabled`(`CombatViewSystemTests.cs:712-714`가 `Delegate.CreateDelegate`로 잡음), `CombatPathInput._shownPath`, `CombatSceneDisplay._pointer·_events·_railway·_thorns·_fieldConfig`, 사건 Presenter `_reaction·_audio·_shownCharacters·_attacks`, `CombatAttackPlayback._ballistaWeapons`(`CombatBallistaAttackTests.cs:351-352`), `CombatSceneRoot`의 이벤트 백킹 필드 `FocusLost`·`Disabled`(SV1 새 테스트), 슬롯 `_receiver`. 이름 고정 테스트: `CombatTimelineViewSystemTests.cs:66-68`(우선순위). SV 비대상이지만 같은 영역: `CombatPointerMapper._pointerEventData·_uiHits`, `FieldUnitPresenter`·`PlacementGhost`·`FieldView`·반응·레일 표시의 비공개 필드(SV0 검토 목록).
- **프레임 순서 기록**(SV6 근거): 키 입력 → Runtime Tick, EventSystem 순서 미정, Tick 안 그룹 순서(평가 §4.3).
- E22의 public 메서드 테스트 사용처 확인.
- 사용자 Unity 기준선: 전체 EditMode 통과 여부, GridOutline·CellPrefab 두 모드 전투 재생(배치 Drag·경로·공격·복귀·턴 종료·포기·완료).
- 산출물: Q1~Q6 기록, 대상 fixture 목록, SV1 변경 명세.

## 5. SV1 — 조립·설치 실패 경로 소유권 (E7·E15·E23·E17)

- **SV1.a 인수 전 실패 반환(E7):** `Attach`에서 Factory가 만든 Display를 `AttachDisplay`가 인수하지 못하면(예외 또는 :350 오류 상태 조용한 반환) 그 Display를 정리하고 원래 예외를 다시 던진다. 순서는 D9대로 **`_presentation.Detach` → `display.Release`**다(`_presentation.Attach` 뒤 실패면 연출이 이미 새 Display를 들고 있다). 정리 실패는 `AggregateException`으로 함께 던진다.
  - 사전 검사(중복 연결·활성 전투) 실패에서는 Release하지 않는다. 공유하는 `units`·`hand`·`ghost`·`field`를 살아 있는 Display에서 빼앗게 된다. `AttachDisplay`를 직접 부르는 테스트(`CombatViewSystemTests.cs:199-204`, 이미 연결된 인스턴스 재전달)는 이 정리를 거치지 않는다.
  - 소유권 이전 지점(`AttachDisplay`가 `_display`에 저장하는 순간)을 XML doc에 적는다.
- **SV1.b 구독 해제(E15):** `Attach`는 `ReferenceEquals(_display, created)`일 때만 `FocusLost`·`Disabled`를 구독한다. Detach는 구독 해제를 `_display == null` 조기 반환 앞으로 옮긴다.
- **SV1.c 설치 실패 뒤 소비 정지(E23, Q5=A):** 첫 그리기 성공(`_battleShown`) 전에는 Tick이 미리보기·결과를 소비하지 않는다. 동작 변경으로 명시하고 테스트로 고정한다. SV4에서 입력 객체 생성을 Install 뒤로 옮기는 전제다.
- **SV1.d (저비용) step 검증 이동(E17):** step 값 검증을 `CombatSceneRoot.ValidatePresentationTiming`으로 올리고, Factory가 가시밭 표시를 사건 Presenter보다 먼저 만들어 값으로 넘긴다(할당 전 클로저 제거). 잘못된 step이 Configure 전에 실패하게 되는 것도 동작 변경으로 기록한다.
- **파일:** `CombatViewSystem.cs`, `CombatSceneRoot.cs`, `CombatSceneDisplayFactory.cs`, `CombatSceneEventPresenter.cs`(검증 중복 정리 여부만), 테스트 `FakeCombatPresentation.cs`(`ThrowOnAttach` 추가), `CombatViewSystemTests.cs`, 실제 씬 fixture(`CombatDisplayReuseTests`).
- **검증:** 가짜 연출 `Attach` 예외 → Release 1회·연출 Detach 선행·원래 예외 전파. 실제 씬 fixture에서 실패 뒤 `FieldThorns_Inactive` 0 → 재Attach 1(`ReleaseCount`보다 판별력이 높음). 오류 상태 Attach → 구독 0·Display 반환. Install 실패 → 결과 게시 → Tick이 소비하지 않음·Detach Release 1회. 잘못된 step → Configure 부작용 없음. 기존 중복 Attach·Install 실패 회귀.

## 6. SV2 — 연결 활성 판정식 정리 (E5)

- **목표 계층:** 직선 계층이 아니라 다음 구조로 둔다. 입력 준비는 재생 계속의 하위가 아니다.

| 이름 | 판정 | 쓰는 곳 |
|---|---|---|
| `IsSceneActive` | 오류 아님 ∧ 진입 활성 ∧ Display 연결 | 포기 `:132`(그대로) |
| `CanContinue` | `IsSceneActive` ∧ 재생 연결 | `:777`(정확히 보존) |
| `IsCurrent(t)` | `CanContinue` ∧ 재생 토큰 일치 | `:799`(동치: 진입 null 검사는 BindServices 뒤에만 도달, Detach는 Abort가 Display 해제보다 먼저) |
| `IsInputReady` | `IsSceneActive` ∧ 수신 연결 | facts `:751`(정확히 보존: 수신 Bind는 `:646`만, Unbind는 Display 해제 전), 개발 Layer `:135`(`_interaction` 검사를 Display로 바꿈 — 정상·오류 정지에서 동치, SV1 뒤에는 부분 연결이 없어짐을 기록) |
| 수신 람다 | 진입 활성 ∧ Display 연결 | `:220`(수신 내부의 오류·연결·확인창 검사는 D11상 수신 객체 소유라 옮기지 않음) |

- **묶으면 안 되는 것:** `IsInputReady`에 재생 연결을 넣지 않는다(StopInput·Detach는 Abort 뒤 Unbind를 하고 그 사이 슬롯이 보내는 `SelectCanceled` 정리를 받아야 함, R2 — `CombatViewSystemTests.cs:626` 회귀). 외부 계약 `IsAttached`(`:146`, `CombatProgressReader.cs:89`)의 의미는 바꾸지 않는다. D9 전투 동일성은 입력 판정식에 넣지 않는다.
- **파일:** `CombatViewSystem.cs`만.
- **검증:** `CombatViewSystemTests`(특히 :626 R2)·`CombatPanelControllerTests`·`CombatInputReceiverTests` 회귀.

## 7. SV3 — 재생 계약 선언·계약 파일 정리 (E9·E12·E22)

- **SV3.a(E9):** `CombatSceneEventPlayback`·`CombatSceneEventPresenter`에서 `: ICombatEventPlayback` 선언만 거둔다(멤버는 유지). 가드 없는 내부 객체를 연출 `Attach`에 넘기는 실수가 컴파일 오류가 된다. 테스트 영향 0건(grep 확인).
- **SV3.b(E12, Q6=A):** 소비자 없는 조각 6개를 `ICombatEventPlayback`에 흡수하고 `ICombatMovementPlaybackPresenter`(소비자: `CombatMovementPlayback`, 테스트 `MovementOnlyPresenter`)는 유지한다. 소비자 0인 `ICombatMovementPresenter`는 지우고 `ICombatBattleDisplay`가 `ICombatMovementFeedback`을 직접 상속한다.
  - 파일: `ICombatPresentationControl.cs`는 비게 되어 삭제(+ .meta). `ICombatMovementPresenter.cs`에는 `ICombatPointerMapping`만 남으므로 `ICombatPointerMapping.cs`로 이름 변경(.meta GUID 유지). `ICombatMovementPlaybackPresenter.cs`에는 그 계약 하나만 남는다.
- **SV3.c:** 위 4파일의 남는 public 계약을 한 번에 `internal`로 바꾼다(public `ICombatMovementPresenter`가 다른 계약을 상속하므로 하나씩 바꾸면 CS0061). 노출하는 public 형식·다른 asmdef 참조 0건, 테스트는 IVT(`Systems/AssemblyInfo.cs:3`).
- **SV3.d(평가 §3.4):** 사건 Playback의 `_presentationVersion`을 범위가 드러나는 이름으로 바꾸고(리플렉션 대상 아님), 사건 Presenter의 "이 값 하나만 원본" 주석에 범위(Presenter와 하위 부품)를 적는다.
- **SV3.e(E22):** 운영 호출자가 없는 public 메서드(`CombatPathInput.cs:125-127·343`, `CombatMovementInteraction.cs:99-101`)를 SV0 확인 결과에 따라 지우거나 private으로.
- **주의:** Unity가 만든 csproj는 파일을 직접 나열하므로 삭제·이름 변경 뒤 MSBuild가 CS2001을 낸다. extra.targets에 Remove를 넣거나 사용자 Unity 컴파일로 확인한다. 문서 갱신: 리팩토링 설계 D5·D6, 후속 설계 SD-B의 구현 목록.
- **검증:** 재생·연출 fixture 전체(`CombatSceneEventPresenterTests`·`CombatPresentationCleanupTests`·`CombatRailwayContactSceneTests`·Ballista·Mortar·Standard·Enemy), `CombatViewSystemTests`.

## 8. SV4 — 입력 기능 묶음 대칭화 (E2·E4·E18·E19)

- **목표:** 조정자가 `_pathInput`을 직접 알지 않게 한다. `CombatInteraction`이 배치·공격·경로 세 기능의 수명 메서드를 대표하고, Router는 경로 대상도 Interaction으로 보낸다. Router의 setter 3개(`SetPathInput`·`SetCursorSnap`·`SetPointerOcclusion`)를 없앤다.
- **생성 시점(Q5=A):** Gate·Interaction·Router·스냅·경로를 ShowBattle의 Install 성공 뒤 한 번에 만든다. Router는 스냅·UI 가림을 생성자 인자로 받는다. 그 사이 이 객체를 읽는 경로(개발 Layer·행동 버튼·포기·facts·StopInteraction)는 null 안전하거나 입력 미연결로 동작하지 않음을 확인했다. 결과 소비 쪽은 SV1.c가 막는다.
- **포인터 대상(E19):** facts에는 원 사실(`IsAttackActive`)을 담고, 경로는 `CombatInputRouter.ResolvePointerTarget`을 직접 불러 판정한다. 판정식은 Router 한 곳이다.
- **메서드와 순서(현재 동작 보존):**

| 시점·요청 | 현재 순서 | SV4 뒤 |
|---|---|---|
| Tick | 경로 `OnPreview`(최신 응답이 있을 때) → `RefreshReturnPreview` → [신호 때] `PreviewAnswered` | `Poll(latest?)`(기능 중립 이름, SV4 검토 반영) → `PreviewAnswered` |
| 결과 전 | 경로 `CancelInputs` → 공격 `BeforeResult` | `BeforeResult()`가 같은 순서 |
| Finalize | Refresh → Gate.Resolve → Layer → `AfterResult`(배치·공격) → 처리 번호 삭제 → 확인 → Complete → 경로 견적 갱신 → 버튼 | `AfterResult` 그대로, **`AfterAcknowledged()`를 Complete 뒤 따로 둠**(경로 편집 허용이 "재생 중 아님"을 요구) |
| 표현 취소(`CancelInputPresentation`) | 경로 취소 → 배치·공격 표현 취소 | `CancelInputs()`(경로 → 배치 → 공격) |
| 정지(`StopInteraction`) | 경로 참조 끊기 → Try(경로 Detach) → Gate.Clear → Try(Stop) | Gate.Clear → Try(`Stop()`), Stop 안에서 RunAll(경로 Detach, 배치·공격 정리). **정지 뒤 경로 호출 0회 보존**, 정리 오류 수집 방식 보존 |
| 개발 Layer 전환 | 배치·공격 표현 취소만 | 그대로(`CancelPresentation`은 경로 미포함 유지, 평가 §3.4 기록) |
| FocusLost | 경로 취소 → 표현 취소 | `CancelInputs()` |
| SelectCanceled | 경로 취소 → 배치 취소 | `SelectCanceled()` 안에서 경로 먼저 |
| Cancel | Interaction 취소 → 경로 Cancel(취소 + 견적 재요청) | 같은 순서(공격 종료가 먼저여야 견적이 즉시 재요청됨) |
| EndTurn | 경로 취소 → 사실 다시 읽기 → 제출 → [성공 시] 표현 취소 | `CancelPath()` → facts → Gate → `CancelPresentation()` |
| AttackMode | 경로 취소 → 진입 시도 | `CancelPath()` → `AttackModeRequested` |
| AttackToggle(모드 중) | Interaction 취소만, 경로 호출 없음 | **`CancelAttack()`을 따로 둠**(통합 Cancel이면 견적이 1 Tick 앞당겨짐) |
| Return / EnterCombat | 경로 `RequestReturn` / 경로 호출 없음 | `RequestReturn()` / 그대로 |
| 포인터 | 대상 판정 → 경로면 경로 처리 후 종료 → 공격이면 UI 가림 → Interaction → 스냅(경로 지정 중 포함) | Router가 대상·가림 판정 유지. Interaction 포인터 메서드에 Path 분기, 스냅 모드는 `IsCursorSnapMode` |

- **파일:** `CombatViewSystem.cs`, `CombatInteraction.cs`, `CombatInputRouter.cs`, `CombatInputFacts.cs`, `CombatPathInput.cs`(접근 범위·포인터 판정). 테스트: Router·Interaction·ViewSystem·PathInput fixture, `PathInputTestFacts.cs`. `CombatInteractionTests` 65케이스가 경로 객체를 요구하므로 공용 fixture(경로용 `ICombatPointerMapping`·`ICombatMovementFeedback` 가짜)를 만든다.
- **검증:** Router 20·Interaction 65·ViewSystem 70 중 경로·결과 후처리 관련, `CombatMovementFeedbackTests`·`CombatMovementPlanningInputTests`·`CombatPathInputCleanupTests`·`CombatMovementViewTests`·`CombatMovementRangeFeedbackTests`·`CombatCursorSnapTests`·`CombatPanelControllerTests`, Wall 입력 회귀.

## 9. SV5 — 입력 기능 조립 Factory (E1·E16)

- **목표:** 배치·공격·경로 기능 객체와 `CombatInteraction` 합성은 전투 입력 Factory가 만든다. `CombatInteraction` 생성자는 (배치, 공격, 경로)를 받고 Gate·Audio 전달 인자가 사라진다. 커서 스냅도 Factory가 만든다. **Factory 메서드는 이미 있는 타입 하나씩을 돌려준다**(`CreateInteraction`, `CreateCursorSnap`). {Interaction, Router, Snap, Path} 같은 묶음 구조체는 만들지 않는다.
- **조정자에 남는 것:** Gate(조정자가 Resolve·Clear·HasPending으로 쓰는 상태 원본)와 Router(조정자의 facts 공급자를 받음). 조정자 생성자·BindServices의 포기·개발 Layer·수신 조립도 범위 밖이다.
- **기능 내부 부품은 옮기지 않는다:** 경로는 Movement·RangeRequest의 수명과 `InnerCellInset` 원본을 소유한다(주입하면 수명 소유가 갈리고 값 출처가 둘이 될 수 있음). 배치·공격 안의 HoverTracker도 같다.
- **E16:** `CombatDisplayServices.From(global)`을 두어 `CombatDisplayTables.From`과 대칭으로 만든다.
- **파일:** 새 `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`(+ .meta), `CombatInteraction.cs`, `CombatViewSystem.cs`, `CombatDisplayServices.cs`. 테스트는 직접 생성 대신 Factory를 써서 조립 경로를 하나로 둔다(Interaction 4·Path 2·Attack 1·Router 4곳 영향). 옛 생성자를 테스트용으로 남기지 않는다.
- **공유:** Factory 폴더는 공용이다. 미전달인 `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md`에 절을 더해 민형님께 한 번에 전달하도록 준비한다.
- **검증:** Interaction·Router·PathInput fixture, 실제 Factory를 쓰는 ViewSystem 통합 케이스.

## 10. SV6 — 행동 버튼 갱신 시점 (E3, Q2)

- **Q2=A:** `Tick`을 바깥 래퍼와 안쪽 본문으로 나눠, 본문의 조기 반환 6곳(`:243·251·256·269·277·293`)과 관계없이 끝에서 한 번 `SyncActionButtons`를 부른다. 입력 직후(`RouteInput :850-851`)는 유지한다. 즉시 숨김은 유지한다: `StopInput`(:479), `SuspendSubmissions`(LifeCycle이 View 뒤에 Tick하므로 래퍼에 맡기면 1프레임 늦음 — 명시적 숨김 추가), Detach·Release. 나머지 호출(`:136` 개발 Layer 콜백, `:601`, `:624`, `:794`, `:840`)을 없앤다.
- **문서:** 개선 설계 D12에 적용 범위 보완 문구, 행동 버튼 설계 B6 문구 갱신.
- **파일:** `CombatViewSystem.cs`, `CombatDevLayerController.cs`(콜백 인자), 테스트.
- **테스트 수정:** RecordInputOnly 즉시 단언(`CombatViewSystemTests.cs:1092-1093·1100`)은 Tick 뒤 단언으로. 제출 정지 즉시 숨김(`:1119-1121`)은 그대로 통과해야 한다.
- **검증:** 행동 버튼 B3·B6 계약(공격 모드 진입·취소, 제출·대기, 새 결과 S>P, 개발 Layer 전환, 포기 확인창 열기), RecordInputOnly 숨김, 패널의 같은 값 재기록 없음(`CombatActionPanel.cs:108-109`). Unity: UI 버튼 클릭 1프레임, 이탈 시작 때 숨김 시점.

## 11. SV7·SV8

- **SV7 복귀 요청 축소안(E6·E21):** `CombatMovementRangeRequest` 선례처럼 복귀 요청의 정체성(요청 번호·행위자·원함)과 응답 일치 판정만 값 객체로 옮긴다. Buffer 게시·표시(`SetReturnState`)·감시는 경로에 남긴다. `RefreshReturnPreview`를 경로 지정 감시와 복귀 견적 재요청 두 이름으로 나눈다(호출 순서 유지). 경로 취소가 언제나 복귀 요청을 지우는 동작, 단일 미리보기 슬롯 경쟁 규칙, 복귀 표시 실패가 초안 정리를 막지 않는 오류 수집을 그대로 둔다.
  - 파일: `CombatPathInput.cs`, 새 값 객체 파일(+ .meta). 리플렉션 이름 `_shownPath`는 경로에 남는다.
  - 검증: 복귀 버튼 A6, 슬롯 경쟁(`CombatMovementRangeFeedbackTests`·`CombatMovementPlanningInputTests`), `CombatPathInputCleanupTests`, FocusLost 오류 보고(`CombatViewSystemTests.cs:688-704`).
  - 2단계 재검토 때 실익이 작으면 이름 분리(E21)만 남기고 값 객체는 뺄 수 있다.
- **SV8 통합 인수:** 평가 §4.4 시나리오 S-A~S-H를 코드 수정 없이 모의해 전후 수정 파일·읽을 계약·검증 범위를 대조한다. 원칙 재점검, 사용자 Unity 전체 확인 인수, 새 검사 에이전트.

## 12. 검토·실행 기록

### 12.1 계획 작성과 검토 에이전트 대조 (2026-10-15)

- 평가·계획 초안 작성 뒤, 읽기 전용 검토 에이전트 3개(①조정자·조립·Framework ②입력 기능 경로 ③표시·재생 경로)가 코드와 대조했다. 세 검토 모두 **"수정 후 진행"** 판정. 파일 수정·빌드·테스트·Unity 실행은 하지 않았다.
- 반영한 주요 지적:
  - ① E5 계층 정정(입력 준비는 재생 계속의 형제, 재생 연결을 입력 조건에 넣지 않음), E7 운영 영향 보정과 E15(조용한 반환 경로 구독 누수)·E16(표시 서비스 `new`) 추가, SV1 정리 순서(연출 Detach 선행)·사전 검사 경계, E13 수용(D11 독립성), Q2-B 제거와 D12 보완 문구, SuspendSubmissions 즉시 숨김 유지, 1단계 뒤 정지 권고.
  - ② E2 위치 보정, E4 "응답 소비·수명 감시·복귀 재요청" 서술, E18(Router setter)·E19(포인터 대상 사본)·E21·E22 추가, E23(설치 실패 뒤 Tick NRE 위험) 발견, SV4 순서 표(`AfterAcknowledged`·`CancelAttack` 분리, 정지 뒤 경로 호출 0회), SV6(복귀) 축소안, E14 수용, E20 기록.
  - ③ E9 개선 승격(내부 객체의 계약 선언 거두기), E12에 `ICombatMovementFeedback.cs` 추가·흡수안, SV7(활성 행위자) 삭제와 E11 수용, E10 시나리오 정정, E17(step 검증 위치) 추가, 재생 세대 카운터 6종은 SSOT 위반 아님, S-H 시나리오·Audio 하드코딩 기록 추가, 리플렉션 보존 목록 보강.
- 검토 사이 의견 차이와 정리:
  - **입력 객체 생성 시점:** ①은 Install 전 일괄 조립(D13 문구 변경), ②는 Install 뒤 생성 + 첫 그리기 성공 전 소비 정지를 권했다. Q5로 사용자 결정에 올리고 D13을 유지하는 ②안을 추천한다.
  - **행동 버튼:** ①·③은 Q2-A 지지(패널이 같은 값이면 쓰지 않음), ②는 입력 직후 같은 프레임 반영을 이유로 신중. A안에 `RouteInput` 직후 갱신을 남겨 두 의견을 함께 반영했다.
- 직접 확인: `CombatSceneRoot.ValidatePresentationTiming`에 step 검증 없음, `FieldThornPresenter` 생성자가 보관 GameObject를 만듦, 공격 방향 벡터 주석("Core의 방향 정의와 같은"), `ICombatMovementPresenter` 소비자 0, 재생 계약 조각 중 소비자 있는 것은 `ICombatMovementPlaybackPresenter`뿐, `CombatCommandGate.cs:35-36` 현재 값 확인 주석, `CombatProgressReader.cs:89`의 `IsAttached` 사용.
- 코드 변경 없음. 커밋·push 없음.

### 12.2 SV0 착수 기준선 (2026-10-15)

- **기준:** HEAD `4cfceaa4`, 코드 작업 트리 깨끗(문서 4개만 변경: CLAUDE.md·View 계획 §7.80·평가·이 계획). 동시 편집 없음. Unity 에디터가 실행 중이라 batchmode 테스트는 쓰지 않는다.
- **빌드 도구(스크래치패드):** `build.sh`(VS 2022 MSBuild, `ProjectL.Tests.Framework.csproj`, 산출물 스크래치), `build2.sh`(새·삭제 파일 목록 반영 + `CombatViewSystemTests.cs`를 **스크래치 패치 사본**으로 바꿔 컴파일 — Rig의 씬 루트 GameObject만 지연(null) 처리, 저장소 파일은 그대로), `runeach.sh`(Unity 내장 Mono로 메서드마다 별도 프로세스 실행. 네이티브 호출에서 런타임이 멈추면 `NATIVE`로 분류).
- **기준선 빌드:** 오류 0, 고유 경고 16(SD-D 기록과 같음).
- **기준선 관리 실행(fixture 단위):** Router 20, Interaction 65, Gate 5, Policy 48, CursorSnap 13, Receiver 6, Panel 9, MovementFeedback 26, MovementPlanningInput 14, PathInputCleanup 1, MovementRangeFeedback 21, MovementRangeRequest 30, ResultPlayback 14, PresentationCleanup 6, TimelineViewSystem 16, ViewRefresh 12, MovementDraft 41 — 모두 통과.
- **기준선 메서드별(패치 Rig):** `CombatViewSystemTests` 통과 62·네이티브 4(거절 결과 Debug.Log 3·씬 루트 1)·실패 0. Wall 8/네이티브 1, DisplayReuse 7/네이티브 18, MovementView 0/19, SceneEventPresenter 0/23(네이티브는 Unity 확인 대상).
- **리플렉션 보존 목록:** §4대로(`_combatQueries`·`_shownPath`·Display `_pointer·_events·_railway·_thorns·_fieldConfig`·Presenter `_reaction·_audio·_shownCharacters`·슬롯 `_receiver`, Timeline 우선순위 테스트).
- **E22 확인:** 호출자가 전혀 없는 것은 `CombatPathInput.SelectCanceled`(:343) 하나다. 경로의 `FocusLost`/`CaptureLost`(:125-127)는 `CombatMovementRangeFeedbackTests.cs:165-166`이, Movement의 것(:99-101, 이미 internal)은 `MovementDraftTests.cs:313`이 쓴다. SV3.e는 `SelectCanceled`만 지운다.
- **프레임 순서:** 평가 §4.3대로(키 입력 → Tick, EventSystem 순서 미정, Tick 안 그룹 순서).
- **사용자 Unity 기준선:** 이번 연속 실행에서는 따로 받지 않았다. SD-D5의 "모두 통과" 확인(HEAD `4cfceaa4` 전 미커밋 상태와 같은 코드)을 기준으로 삼고, 최종 확인은 SV8 대신 사용자 Unity 실행으로 받는다.
- **SV0 검토(새 에이전트 1개, 판정 "수정 필요(소폭)") 반영:**
  - **분류기 결함:** 모든 테스트가 정리(Dispose) JIT에서 "internal call" 경고를 내므로 첫 `runeach.sh`는 실패도 NATIVE로 분류했다("실패 0"이 구조적으로 0). 고친 규칙: 요약 줄이 없으면(런타임 중단) NATIVE, 실패 줄이 모두 `MissingMethodException assembly:<unknown assembly>`(네이티브 internal call)이면 NATIVE, 그 밖의 실패는 FAIL. 비교는 메서드 이름 단위로 하고 PASS가 다른 상태로 바뀌면 회귀로 본다. 단위는 메서드이며 케이스 수를 함께 적는다. 위 메서드별 기준선 수치는 고친 분류기로 다시 측정한 §12.3 표가 대체한다.
  - **패치 Rig 해석:** `AttachRoot_WithoutViewDatabase_IsRejected`는 네이티브가 아니라 패치로 `Root`가 null이 되어 다른 예외가 먼저 나는 **패치 유발 단언 실패**다(Unity 전용). `build2.sh`에 패치 3줄 적용 검사를 넣었다.
  - **csproj 누락:** Unity가 만든 `ProjectL.Tests.Framework.csproj`에 `CombatEnemyPresentationTests.cs`가 없다(디스크의 다른 파일은 모두 포함). 스크래치 targets(`newtests.txt`)로 포함해 컴파일한다. SV1에서 이 파일에 넣은 테스트는 SV3 빌드에서 처음 컴파일 확인됐다.
  - **E22:** 결론 유지. 경로 `FocusLost`/`CaptureLost`는 운영 호출자 0, **테스트 전용 별칭**이다. Movement의 것은 이미 internal이다.
  - 리플렉션 목록 누락을 §4에 보탰다.

### 12.3 HEAD 기준선 재측정 (고친 분류기, 2026-10-15)

산출물은 HEAD 코드 + 패치 Rig(`base2`). 단위는 메서드(괄호는 통과 케이스 수). NATIVE는 Unity 확인 대상이다.

| fixture | 통과 | NATIVE | FAIL |
|---|---|---|---|
| CombatViewSystemTests | 62(66) | 3(거절 결과 Debug.Log) | 1(`AttachRoot…`, 패치 유발) |
| Router / Interaction / Gate / Policy / CursorSnap | 12(20) / 49(65) / 5 / 15(48) / 10(13) | 0 | 0 |
| Receiver / Panel / MovementFeedback / PlanningInput / PathInputCleanup | 6 / 9 / 22(26) / 8(14) / 1 | 0 | 0 |
| MovementRangeFeedback / MovementRangeRequest / MovementDraft | 12(21) / 15(30) / 34(41) | 0 | 0 |
| ResultPlayback / PresentationCleanup / TimelineViewSystem / ViewRefresh | 14 / 5(6) / 10(16) / 7(12) | 0 | 0 |
| ProgressIntegration / ProgressReader | 10(15) / 10 | 0 | 0 |
| Wall / DisplayReuse | 8(12) / 7 | 1 / 18 | 0 |
| MovementView·SceneEventPresenter·RailwayContactScene·PointerMapping·Placement/Attack/MovementDraft/PlanningDisplay·Standard/Ballista/Mortar·MortarAsset·UnitDeath·RailwayContactPlayback·AttackHealthPreview | 0 | 전부 | 0 |

`CombatEnemyPresentationTests`는 csproj 누락으로 HEAD 산출물에 없다(SV3 빌드부터 포함). 비교는 `compare.sh`(메서드 단위)로 한다.

### 12.4 SV1 조립·설치 실패 경로 소유권 (2026-10-15)

- **구현:** `CombatViewSystem.Attach` → `AttachCreated(created, root)`(인수 전 예외면 `catch when (_display == null)`에서 연출 Detach → 표시 Release 뒤 원래 예외, 오류 상태 조용한 반환이면 정리 후 반환, 인수했을 때만 씬 루트 구독), `ReleaseUnowned`, Detach의 구독 해제를 조기 반환 앞으로, Tick의 첫 그리기 성공 전 정지(`_battleShown`). `CombatSceneRoot.ValidatePresentationTiming`에 이동 step 검사, Factory가 가시밭 표시를 사건 Presenter보다 먼저 만들어 `thorns.Consume`을 직접 전달(할당 전 클로저 제거).
- **테스트:** `CombatViewSystemTests` 4건(설치 실패 뒤 결과 미소비, 인수 전 실패의 연출 Detach→Release 순서·재연결, 오류 상태 반환, 씬 루트 구독/해제 — 마지막은 Unity 전용), `CombatEnemyPresentationTests` step 4케이스, `CombatDisplayReuseTests` 실제 씬 1건(인수 전 실패 뒤 `FieldThorns_Inactive` 0, fixture에 연출 주입 인자), `FakeCombatPresentation`에 `ThrowOnAttach`·`OnDetach`·`DetachCount`.
- **동작 변경(계획 Q5=A대로):** 첫 그리기 실패 뒤 Tick은 미리보기·결과를 소비하지 않는다(운영은 이미 오류 정지라 영향 없음). 잘못된 이동 step은 Configure 전에 씬 설정 검사에서 거절된다.
- **검증:** MSBuild 오류 0·경고 HEAD와 같음(파일·메시지 기준). 관리 실행 결과는 §12.7(SV4까지 누적 실행)에 함께 적는다.
- **검토:** 첫 검토 에이전트가 사용량 한도로 중단되어 같은 스냅샷(`snap/sv1`)으로 다시 실행 중.

### 12.5 SV2 연결 활성 판정식 (2026-10-15)

- **구현:** `IsSceneActive`(오류 아님 ∧ 진입 활성 ∧ 표시 연결) 위에 재생 쪽 `CanContinue()`(∧ 재생 연결)와 입력 쪽 `IsInputReady`(∧ 슬롯 연결)를 형제로 두고, `IsCurrent(t)` = `CanContinue()` ∧ 토큰 일치. 포기·개발 Layer·수신 창구·facts·IsCurrent가 이 이름만 쓴다. `IsAttached`(외부 계약)는 그대로.
- **검토(새 에이전트 1개, 판정 "수정 필요" — 주석만):** 4곳 모두 도달 가능한 모든 시점에서 옛 식과 동치임을 확인(수신 Bind는 입력 객체 생성 뒤 `SyncInputLayer` 한 곳, Unbind는 표시·Router를 놓기 전, Abort가 표시 해제보다 먼저, `IsFaulted`가 StopInput보다 먼저). 반영: `CombatInputReceiver`·`CombatDevLayerController`의 옛 판정 설명 주석을 "조정자의 이름 있는 판정(IsSceneActive / IsInputReady)" 참조로 교체, R2 근거를 `IsSceneActive` remarks로 이동, `RouteInput`에 "Bind ⇒ Router 존재" 불변식 명시, `IsCurrent` 근거 보완. 권고 4("`_battleShown`을 입력 객체 생성 뒤로")는 SV4에서 반영. 권고 2(Drag 중 Detach의 R2 회귀 변형 테스트)는 이번 범위에서 추가하지 않았다(기존 `CombatViewSystemTests`의 SelectCanceled 회귀가 남아 있고, 수신 판정에 재생 연결을 섞지 않는다는 설계 기록으로 대신).

### 12.6 SV3 재생 계약 선언·계약 파일 정리 (2026-10-15)

- **구현:** 소비자 없는 조각 6개를 `ICombatEventPlayback` 멤버로 흡수(직접 9 + `ICombatMovementPlaybackPresenter` 상속 4 = 13, 서명 동일). `CombatSceneEventPlayback`·`CombatSceneEventPresenter`의 계약 선언 제거(멤버 유지). `ICombatMovementPresenter` 삭제·`ICombatBattleDisplay`가 `ICombatMovementFeedback` 직접 상속. `ICombatPresentationControl.cs` 삭제(+.meta), `ICombatMovementPresenter.cs` → `ICombatPointerMapping.cs`(.meta GUID `3139cac2…` 유지). 남은 public 계약 3개 internal. Playback 버전 필드 `_cleanupVersion`·Presenter 세대 주석 범위 명시. `CombatPathInput.SelectCanceled` 삭제. 문서: 리팩토링 설계 D5·D6, 후속 설계 SD-B에 날짜 붙인 덧붙임.
- **주의(커밋 때):** `ICombatPointerMapping.cs`와 `.cs.meta`를 함께 추가하고 옛 두 파일 삭제를 함께 커밋한다. Unity가 csproj를 다시 만들기 전 MSBuild는 스크래치 targets로 Remove/Include해 확인했다.
- **검증:** MSBuild 오류 0·경고 HEAD와 같음.
- **검토(새 에이전트 1개, 판정 "수정 필요(경미)" — 주석·문서만):** 멤버 집합 보존, 명시적 구현 0, internal 전환 안전(Systems 밖 참조 0, public 형식 노출 0), GUID 보존·고아 참조 0을 확인. 반영: `CancelRailwayContacts` 주석("실패 정리 버전"), 리팩토링 설계 D5 덧붙임, `ICombatPointerMapping` 주석(소비자 CursorSnap·실제 구현 `CombatPointerMapper`), `CanPresent` 주석을 의무형("판단해야 합니다")으로, `_cleanupVersion` 주석에 레일 Tick 건너뛰기 판정 추가. 권고대로 두 클래스의 public 멤버는 그대로 둔다.

### 12.7 SV4 입력 기능 묶음 대칭화 (2026-10-15)

- **구현:** `CombatInteraction`이 경로 입력을 세 번째 기능으로 받고(생성 인자, null이면 경로 위임 무동작 = 정지 뒤와 같은 상태) 수명 메서드 `Poll`·`BeforeResult`·`AfterAcknowledged`·`CancelInputs`(경로 포함)·`CancelPresentation`(경로 제외)·`CancelPath`·`CancelDragOrAttack`·`Cancel`·`SelectCanceled`·`RequestReturn`·`Stop`과 포인터 Path 분기·`IsPathPlanning`·`IsCursorSnapMode`를 둔다. Router는 setter 3개를 없애고 커서 스냅·UI 가림을 생성 인자로 받으며 경로 대상도 Interaction으로 보낸다. facts는 `IsPathTarget` 대신 원 사실 `IsAttackActive`를 담고 경로가 `ResolvePointerTarget`으로 직접 판정한다(E19). 조정자는 `_pathInput`을 모르고, 입력 객체를 첫 그리기 성공 뒤 `CreateInput()`에서 만든 다음 `_battleShown`을 세운다(SV2 검토 권고 4).
- **계획과 다른 점(기록):** ① 계획 §8의 "경로용 공용 fixture" 대신 경로를 선택 인자로 두었다. 배치·공격만 검증하는 테스트는 경로 없이 만들며, 이는 정지 뒤와 같은 상태라 새 분기가 없다. 운영 조립은 SV5 Factory(`CreateSceneInteraction`)가 언제나 경로를 넘긴다. ② 표의 `CancelAttack` 대신 `CancelDragOrAttack`으로 HEAD의 옛 `Cancel` 의미(Drag면 Drag, 아니면 공격)를 정확히 보존했다. ③ 정지 순서가 "경로 Detach → Gate.Clear → Interaction.Stop"에서 "Gate.Clear → Interaction.Stop(경로 Detach → 배치·공격)"으로 바뀌었고 정리 실패가 한 겹 중첩된다. Gate.Clear는 던지지 않고 경로 Detach는 Gate를 읽지 않으며 중첩 구조를 단언하는 소비자가 없어 관측 차이가 없다(SV4 검토 확인).
- **테스트:** Router·Interaction 생성 인자, setter → 생성 인자, facts 헬퍼·`PathInputTestFacts`·Policy 테스트의 `isPathTarget` → `isAttackActive`, 새 `InteractionStop_DetachesPath_AndLaterInputsDoNotReachIt`(Probe에 `ReturnStates` 카운터 추가).
- **검증:** MSBuild 오류 0·경고 HEAD와 같음. 관리 실행 38 fixture에서 HEAD 기준선 대비 PASS → 다른 상태 0건(새 테스트만 NEW). 새 테스트는 스크래치 변이 두 개(정지에서 Detach 누락, 참조 유지)를 모두 실패로 잡았다.
- **검토(새 에이전트 1개, 판정 "수정 필요" — 테스트 보강만):** §8 표 전 행 순서 일치, 생성 시점 이동의 null 안전, E19 동치, God class 위험 없음을 확인. 반영: 새 테스트가 정지 직후 경로 자신의 지정 상태·범위 표시 정리와 이후 `SetReturnState` 호출 0회를 단언(위 변이로 판별력 확인), `PollPath` → `Poll`(기능 중립), `Poll`이 응답 처리 안 재진입 정지 뒤 놓인 경로를 다시 부르지 않음, 취소 메서드 주석에 경로 포함/제외 표시, `_gate` 주석 정정. 남은 경로 지식(조정자의 `CombatPathInput` 생성)은 SV5에서 Factory로 옮겼고, 경로용 사실 공급자 `_readPathFacts`(선행 미처리 명령 포함 사실)는 조정자에 남는다.

### 12.8 SV5 입력 기능 조립 Factory (2026-10-15)

- **구현:** 새 `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`(+ .meta 새 GUID `2eb80d61…`): `CreateSceneInteraction`(경로 입력 + `CreateInteraction`), `CreateInteraction`(배치·공격 생성 + 경로와 묶음, 테스트도 이 경로), `CreateCursorSnap`. `CombatInteraction` 생성자는 기능 객체 셋을 받는다. 조정자는 Gate·Router만 직접 만든다. `CombatDisplayServices.From(IGlobalSystemGroupService)`로 표시 서비스 선택을 옮겼다(E16). Framework 공유 문서(미전달) §4에 SV1·SV5 변경을 덧붙였다.
- **SV1 검토 후속(같은 단계에서 반영):** `AttachDisplay` → `TakeDisplay`(마지막 문장이 `_display` 저장, 오류 상태면 false) + `ContinueAttach`. `AttachCreated`가 인수 여부로 정리·구독을 정하고, 첫 그리기 안 재진입 Detach면 구독하지 않는다. step 조건을 `CombatSceneEventPresenter.ValidateStepSeconds`(한국어 메시지)로 공유. 정리 실패 시 원인 예외가 맨 앞인지 보는 테스트 추가. Factory remarks·Detach doc·`_battleShown` 주석·FakeCombatPresentation 요약 정정.
- **검증:** MSBuild 오류 0·경고 HEAD와 같음. Router 13(21)·Interaction 49(65)·ViewSystem 66(70, 패치 Rig FAIL 2는 Unity 전용)·Wall·Panel·MovementFeedback 통과.
- **검토(새 에이전트 1개, 판정 "수정 필요(경미)" — 공유 문서만):** §9 제약(이미 있는 타입 하나씩 반환, 무타입 묶음 없음, Gate·Router는 조정자, 기능 내부 부품 미이동, 옛 생성자 없음)과 테스트의 `CreateInteraction`(경로 없음)이 운영과 같은 조립 경로임을 확인. `TakeDisplay` 마지막 문장이 `_display` 저장이라 "예외로 끝나면 언제나 인수 전"이 구조적으로 성립. step helper는 예외 형식·ParamName이 같아 기존 테스트 영향 없음. 반영: 공유 문서의 운영 경로 표(`AttachCreated`)·정리 실패 시 `AggregateException`·재진입 Detach 뒤 구독 생략·`CreateInteraction` 서술 정정, `CreateSceneInteraction`이 경로 생성 전에 Gate를 검사, 재진입 Detach 뒤 구독·이중 반환이 없는지 보는 테스트(Unity 전용) 추가.
- **S-A 실제 범위(SV8 비교용):** 새 의존성이 Factory 인자(display·state·characters·inputRead·inputWrite·gate·readPathFacts·audio)에 이미 있으면 공격 구현 + Factory + `CreateInteraction`을 부르는 테스트 5곳 + Wall 단위 fixture. 조정자와 `CombatInteraction`은 빠진다. 조정자가 갖고 있지만 Factory 인자에 없는 의존성이면 `CreateInput` 호출 한 줄이 더 바뀌고, 새 Global 서비스면 PostBindService·필드·전달(그리고 BindServices로 받으면 그 서명과 호출 3곳)이 더 바뀐다.
- **계획 §9와 다른 점(기록):** 검증 대상 자체를 만드는 단위 fixture(`CombatMovementFeedbackTests.cs:428`·`CombatPathInputCleanupTests.cs:20`의 경로 입력, `WallAttackInteractionTests.cs:247`의 공격 입력, `CombatInputRouterTests`의 가짜 장치용 `new CombatCursorSnap`)는 직접 생성을 유지했다. Factory에 단일 기능 메서드가 없고 가짜 장치는 표시 타입이 아니기 때문이다.

### 12.9 SV6 행동 버튼 갱신 시점 (2026-10-15, Q2=A)

- **구현:** `Tick` = `TickCore` + 끝에서 `CanContinue()`이면 `SyncActionButtons()` 한 번. 없앤 호출: 결과 후처리 끝, `SyncInputToCore`(삭제, 호출은 `SyncInputLayer`), `SyncSubmissionState` 끝, `CancelInputPresentation` 끝, 개발 Layer 컨트롤러 `layerChanged` 콜백 인자. 유지: `RouteInput` 직후 갱신(키·UI 버튼 입력은 같은 프레임), `StopInput` 숨김, Detach·전투 정리의 Release·Clear 숨김. 추가: `SuspendSubmissions`가 즉시 숨김(전투 Plan이 View 다음에 Tick).
- **관측 차이(계획대로, SV6 검토로 정정):** 키 입력과 행동 패널 버튼은 수신 창구 → `RouteInput`이라 즉시 갱신된다. 개발 Layer 전환·포기 확인창 열기는 그 프레임의 View Tick 끝에서 반영된다(그리기 기준 0~1프레임). 첫 그리기는 씬 Enter 노드가 Global Loading Tick 안에서 돌아 같은 프레임 View Tick 끝에 반영된다. 결과 후처리·새 결과 관측은 Tick 안이라 같은 프레임이다. 그 사이 잘못된 기록은 Policy의 Layer 검사와 확인창 열림 중 입구 검사가 막는다. 제출 정지의 즉시 숨김은 공개 입구의 보장이며, 운영 경로(제출 정지 Job은 View Tick 전 Global Job Runner에서 실행)에서는 관측 차이가 없다.
- **문서:** 개선 설계 D12에 적용 범위 보완(닫힌 사건 집합 = 이벤트, 여러 입력 경로의 View 내부 값 = Tick 파생), 행동 버튼 설계 B6에 갱신 덧붙임.
- **테스트:** RecordInputOnly 개발 Layer 전환 뒤 Tick 전 숨김·Tick 뒤 보임 단언, 제출 정지 즉시 숨김 + 이후 Tick에도 숨김 유지 단언.
- **검증:** MSBuild 오류 0·경고 HEAD와 같음. 관리 실행 38 fixture 회귀 0, ViewSystem 66(70)·Panel 9 통과.
- **검토(재실행한 새 에이전트 1개, 판정 "수정 필요" — 동작 결함 없음):** 결과 후처리·새 결과가 같은 Tick 끝에서 한 번만 쓰이고(E3의 중간값 두 번 계산 제거), 첫 그리기는 같은 프레임 View Tick 끝에 반영되며, 매 Tick 비용에 할당이 없고 패널 재기록 방지가 유지됨을 확인. 반영: ① 개발 Layer 컨트롤러에 남아 있던 `layerChanged` 매개변수·필드·호출·주석 삭제(호출 측 인자만 빠져 있었음), 행동 버튼 설계 파일 표 갱신. ② 근거 정정 — 제출 정지 Job은 Global Job Runner Tick에서 View Tick **전에** 실행되므로 즉시 숨김은 "공개 입구의 보장, 운영 경로 관측 차이 없음"으로, 행동 패널 버튼은 `RouteInput`으로 즉시 갱신되며 늦을 수 있는 것은 개발 Layer·포기 열기(그리기 0~1프레임)뿐으로, 안전망은 Gate가 아니라 Policy의 Layer 검사와 확인창 입구 검사로(조정자 주석·B6·계획 Q2·이 절). B6 제목의 "매 Tick 아님"에 SV6 변경 표시. ③ 테스트 보강 — `ActionButtons_EndTurnFollowsResultLifetime_AtTickEnd`(결과 관측 Tick 끝 턴 종료 비활성 → 후처리 Tick 끝 활성, 갱신을 `TickCore` 앞으로 옮긴 스크래치 변이를 실패로 잡음), `ActionButtons_AttackHighlight_ImmediateOnButton_ClearedAtTickAfterAbandonOpen`. 권고 반영: `SyncActionButtons` 가드에 `_battleShown`, 개발 Layer 테스트 메시지를 의도("갱신 지점을 Tick 끝 하나로")로, D12 덧붙임의 D11 출처 명시, 포기 컨트롤러의 낡은 "Tick의 Sync" 주석 정정.

### 12.10 SV7 복귀 요청 축소안 (2026-10-15)

- **구현:** 새 `CombatReturnRequest`(+ .meta 새 GUID `5897ef2c…`): 요청 번호·행위자·원함과 슬롯 소유·응답·context 일치 판정만 갖는다(`CombatMovementRangeRequest` 선례). Buffer 게시·`SetReturnState` 표시·입력 허용은 경로 입력에 남는다. `CombatPathInput.RefreshReturnPreview` → `Refresh()` = `WatchPlanning`(경로 지정 수명 감시) → `RefreshReturnEstimate`(복귀 견적 재요청)로 두 단계에 이름을 붙였다(E21). 견적 재요청이 원래 "원함"을 건드리지 않던 동작은 `Begin`이 원함을 보존하고 사용자 요청만 `MarkWanted()`로 세워 그대로 지켰다.
- **테스트:** 호출 이름 변경 5곳(`rig.Input.Refresh()`), 새 `CombatReturnRequestTests` 6건(빈 요청, 슬롯 소유 상실, 재요청 시 원함 유지·한 번 소비, 응답 판정 3조건, context 없음 거절, Reset).
- **검증:** MSBuild 오류 0·경고 HEAD와 같음. 관리 실행 39 fixture에서 HEAD 기준선 대비 PASS → 다른 상태 0건, 새 테스트 통과. 복귀 버튼 실제 흐름(`CombatMovementViewTests`)은 네이티브라 Unity 확인 대상.
- **검토(새 에이전트 1개, 판정 "수정 필요" — 주석 1줄):** HEAD 복귀 코드와 줄 단위로 대조해 조건·순서·Buffer 쓰기·표시 호출이 같고(재요청이 원함을 보존하는 것 포함), 슬롯 소유 판정식 3벌이 1벌로 줄었으며 §27 과잉 추상화가 아님을 확인. 반영: `_return` 필드 주석의 책임 방향 정정(게시·표시·감시는 경로 입력), `WatchPlanning`·`RefreshReturnEstimate` 주석 정밀화, `Begin` 인자 검사(선례와 같음), 실제 Core 응답 context로 전투·행위자·의도 조건을 각각 판별하는 `Matches_UsesBattleActorAndReturnIntent_OfRealCoreContext`와 `Begin` 거절 테스트 추가, 재요청 뒤 새 번호 소유 단언. `OwnsPreview` 서명을 선례(`bool, in CombatViewPreviewRequest`)에 맞추는 권고는 호출부가 Buffer 읽기 싱크를 이미 쓰고 있어 그대로 두었다. 관측자·`HasExpectedFacts` 조건은 실제 context를 다르게 만들기 어려워 관리 테스트로 분리 검증하지 못했다.

### 12.11 SV0~SV7 마무리와 Unity 확인 목록 (2026-10-15)

- **진행 방식 기록:** 사용자 지시로 1단계 뒤 정지 없이 SV0~SV7을 연속 실행했고, SV마다 새 검토 에이전트 1개로 대신 점검했다(SV1·SV2·SV6은 첫 검토가 사용량 한도로 끊겨 같은 스냅샷으로 재실행). 모든 검토 판정은 "수정 필요(주석·문서·테스트 보강)"였고 제품 동작 결함은 0건이었다. 지적은 모두 반영했다. Phase별 워크시트는 만들지 않았다.
- **검증 범위의 한계:** 관리 실행은 Unity 내장 Mono로 순수 C# 경로만 확인한다. 네이티브 호출이 있는 fixture(재생·실제 씬·연출), 패치 Rig에서 씬 루트를 쓰는 테스트, Debug.Log를 지나는 테스트는 Unity 확인 대상이다. Unity Test Runner 결과와 구분한다.
- **사용자 Unity 확인(요청):**
  1. 컴파일: Unity가 csproj를 다시 만들면서 새 파일 4개(`CombatSceneInputFactory`, `CombatReturnRequest`, `ICombatPointerMapping`, `CombatReturnRequestTests`)와 csproj에 빠져 있던 `CombatEnemyPresentationTests.cs`가 포함되는지.
  2. EditMode 전체. 특히 Unity 전용: `CombatViewSystemTests`의 `AttachRoot_WithoutViewDatabase_IsRejected`·`AttachCreated_SubscribesSceneRootOnlyWhenOwned_AndDetachUnsubscribes`·`AttachCreated_ReentrantDetachDuringFirstDraw_DoesNotSubscribeOrReleaseTwice`·거절 결과 3건, `CombatDisplayReuseTests.AuthoredCombatScene_FailureBeforeOwnership_ReleasesCreatedDisplay`, `CombatEnemyPresentationTests`(새 step 4케이스 포함), `CombatMovementViewTests`(복귀 버튼 흐름), 재생·연출 fixture 전체.
  3. 재생(GridOutline·CellPrefab 두 모드): 배치 Drag, 경로 Drag·취소·복귀 버튼(견적 표시·승인 제출), 공격 버튼 토글·조준·UI 위 클릭 무시, 턴 종료(결과 재생 중 비활성 → 끝나면 활성), 공격 모드 중 포기 확인창 열기(강조가 꺼짐), RecordInputOnly 개발 Layer 패널, 포기·승패 완료.
- **커밋 때 주의:** 새 파일은 .cs와 .meta를 함께(`CombatSceneInputFactory`, `CombatReturnRequest`, `ICombatPointerMapping`(옛 `ICombatMovementPresenter` 이름 변경, GUID 유지), `CombatReturnRequestTests`). 삭제 2쌍(`ICombatPresentationControl`, `ICombatMovementPresenter`)과 이름 변경을 같은 커밋에. Framework 공유 문서(§4 추가)는 미전달.
- **남은 것:** SV8(시나리오 S-A~S-H 전후 대조·원칙 재점검·Unity 결과 인수). 범위 밖으로 남긴 것: Audio, VR6, R6(응답 전달 재설계), E8·E10·E11 수용 항목, E20 Core 요청 후보.

### 12.12 SV8 통합 인수 (2026-10-15)

- **착수:** 사용자 "SV8 진행해". 코드 변경은 아래 .meta 수정 하나뿐이다.
- **인수 평가:** [SV 인수 평가](../specs/2026-10-15-combat-view-structure-acceptance.md) — E1~E23 처리 결과, 시나리오 S-A~S-I 전후 대조(S-I "새 입력 기능"을 추가), 조정자에 남은 것(입력 기능 호출 이전 16곳·직접 생성 5개 → 묶음 10곳·Factory 1곳·Router 생성 1곳), 원칙 재점검, 증거·한계·이력 정정.
- **Unity 컴파일 증거(Editor.log):** 15:44 에디터 재시작 뒤 스크립트 컴파일·재로드 성공, `error CS` 없음, csproj 재생성(새 파일 포함). 테스트 실행 결과는 아니다.
- **기존 결함 발견·수정:** 같은 로그의 경고로 `CombatEnemyPresentationTests.cs.meta` GUID가 33자리(2026-10-13 `4afddbf5`부터)라 Unity가 파일을 무시해 온 것을 찾았다. 32자리로 고쳤다(프로젝트 전체 .meta 검사에서 다른 손상 없음). 이 fixture는 다음 Unity 실행에서 처음으로 컴파일·실행되므로, 이번 변경과 무관한 기존 실패가 나올 수 있다. 리팩토링 계획 R0.6 기록에 정정을 덧붙였다.
- **검토(새 에이전트 1개, 판정 "수정 필요" — 문서만):** .meta 33자리 무효·Assets·Packages 3,869개 .meta 전수 정상·새 GUID 참조 없음·줄 수·S-A·S-E·S-F·처리표 표본을 확인. 반영: 조정자 호출 수를 호출 지점 단위로 통일(이전 19곳·생성 5 → 묶음 11곳·Factory 2·생성 2), `.cs` 수정 30개, S-B의 이전·지금 기준 통일, S-I·원칙 O에 조정자가 바뀌는 조건 4가지 명시, Unity 컴파일 증거를 .meta 수정 전후로 나눠 기록(수정 뒤 재컴파일·csproj 포함 확인), S-C·S-D 정밀화, 기준선 fixture 37개, 시나리오 표에 검증 열 추가, 평가 §3.4 항목(개발 Layer 경로 미취소 이유·Poll 뒤 재확인 없음·facts 기본값 의미 변화), 실패 원인 가르기 절차.
- **커밋 때 추가 주의:** 수정한 `CombatEnemyPresentationTests.cs.meta`를 포함하고, SV와 무관한 `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`(Unity가 바꾼 FMOD 경로 한 줄)은 따로 판단한다.
- **Unity Test Runner:** 사용자가 실행했다("유니티 테스트 돌렸어"). 결과는 Test Runner 창에만 있고 파일로 남지 않아 받지 못했다(남은 `TestResults.xml`은 2026-10-02 실행분). 사용자에게 통과·실패 수와 실패 이름을 요청했다.
- **남은 것:** Unity Test Runner 결과 인수(인수 평가 §8의 원인 가르기 순서), 재생 확인(§12.11 목록).

### 12.13 리팩토링 종료와 재평가 (2026-10-15)

- **사용자 결정:** "일단 리펙토링 여기까지 하고 현재구조 … 다시 평가해줘". SV 리팩토링은 SV7(코드)·SV8(인수 문서)에서 멈춘다. 이 계획에 새 Phase를 더하지 않는다.
- **재평가:** [구조 재평가](../specs/2026-10-15-combat-view-structure-reevaluation.md) — 지금 구조의 절대 판정(인수 §5는 SV 전과의 상대 판정), 남은 문제 N1~N10, 기능 단위 변경 시나리오, 방향 평가, 재개 때 후보 순서(N5 → N4 → N1 → N3(a) → N2(a), VR6와 함께). 코드 변경 없음.
- **남은 것:** 위 §12.12와 같다(Unity Test Runner 결과 인수, 재생 확인). 재개는 SV 인수·커밋 뒤 새 계획 문서로 한다.
