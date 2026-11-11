# 튜토리얼 구현 계획

- **2026-11-10 후속 계획:** 최신 게임 규칙·공격/턴 종료 실습·수동 설명 진행·UI 점선/모델 아웃라인은 [확정 설계](../specs/2026-11-10-tutorial-current-rules-refresh-design-Hojin.md)와 [새 Phase별 Task 계획](2026-11-10-tutorial-current-rules-refresh-plan-Hojin.md)을 따른다. 아래의 완료 표와 T-8은 교체 전 구현 이력이다. 이번 실행 순서는 새 계획이 대체하며, 관련 미확인 사항은 새 계획 Phase E에서 인수한다. 새 Phase A(자동 진행 제거·진입 대기·카드 개폐 소유권 정리)는 코드·에셋에 반영했고 사용자 Unity 확인 대기다. 상세 상태와 검증 근거는 새 계획 §9.2를 따른다. 새 실습 판정·입력 제한은 Phase B에 반영했고 정적 검수/Unity 확인 항목은 새 계획 §9.3에 있다. 강조·17단계 정의 교체는 미착수다.
- 작성자: 호진(이호진)
- 상태(2026-10-27 기록): T-1~T-7 코드 완료·사용자 커밋. T-1~T-5 튜토리얼 시험은 Unity 통과(2026-10-20), T-6·T-6R·T-7과 보완들의 화면·재생은 Unity 확인 대기, T-8 미착수. 당시 HEAD의 1-1 정의 `targetMapKey`가 10000이었다(설계 §16). 2026-11-10 로컬 값은 사용자 변경으로 1이며 새 계획에서 보존한다.
- 설계: [튜토리얼 설계](../specs/2026-10-18-tutorial-buffer-job-public-contract-design-Hojin.md). 이 계획의 “설계 §”는 이 문서다.
- 정리: 2026-10-27 Task 단계·체크리스트와 실행 기록(옛 §12.1~§12.15)을 결과 요약으로 줄였다. 코드 주석이 부르는 옛 절 번호(§5·§6·§7·§8·§12.2·§12.3·§12.6)는 §3 각 절 제목에 “(옛 §n)”으로 적었다. 끝난 Task 단계·시험 수치·검증 로그 경로는 git `10914570`.

## 0 작업 규칙

- Phase가 끝나면 멈추고 보고한다. 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다. 커밋·push는 사용자가 한다.
- **시험 코드 추가·수정 금지(2026-10-20 사용자, T-6R 구현 중):** “Test코드는 오류안나게만 하고 다음 작업에서는 Test코드는 수정이나 추가해서 만들지 마세요.” 그 뒤로는 컴파일이 깨지는 곳만 맞춘다. 빌드 통과를 시험 통과로 기록하지 않는다.
- **Core 코드는 바꾸지 않는다.** 전투 View는 설계 §15.10 범위만 바꾸고 전투 작업자에게 알린다.
- **한 커밋 묶음:** View DB 표 코드 + `DataBaseSO/ViewGroup/TutorialDefinition/` + `ViewGroup.asset`(없으면 DB 생성 실패) / 튜토리얼 입력 코드 + `PlayerInputActions.inputactions`(없으면 입력 설정 검사 실패) / 튜토리얼 화면 코드 + `TutorialUI.prefab` + `Combat.unity`(프리팹이 없으면 모든 Combat 진입이 멈춤).
- 경로 약어: `S/` = `Assets/ProjectL/Script/Systems/`, `V/` = `S/RuntimeSystem/InGameSystem/ViewSystem/`, `L/` = `S/RuntimeSystem/LifeCycleSystem/LifeCycles/`, `B/` = `S/RuntimeSystem/BufferSystems/`, `T/` = `Assets/Tests/EditMode/`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| T-1 | 공개 계약과 상태 Store(Buffer) | 완료 | Unity 시험 통과(2026-10-20) | `3b875440` |
| T-2 | 정의 표(View DB)와 1-1 정의 에셋 | 완료 | 튜토리얼 시험 통과(2026-10-20) | `3b875440` |
| T-3 | `TutorialPlan`·Job 일곱과 Store 전이 | 완료 | Plan·Job·Buffer·정의 시험 통과(2026-10-20) | `89cbc44e` |
| T-4 | 실행 소유자(트리거·화면 연결·1-1 판정·타이머·즉시 마감) | 완료 | ViewSystem·Timer 시험 통과(2026-10-20). 로그 재생 미확인 | `89cbc44e` |
| T-5 | 튜토리얼 전용 입력 | 완료 | `TutorialInputTests` 통과(2026-10-20, 시험 1건 고친 뒤) | `89cbc44e` |
| T-6 | UI 프리팹·표시(초기) | 완료 | Unity 미확인 | `34ee0de4` |
| T-6R | 실제 덱 카드·대상 옆 설명·실습 띠·표시 수명 보완 | 코드·에셋·코드 리뷰 완료 | Unity 확인 대기 | `34ee0de4` |
| 덮개 0.95 | 덮개 알파 13/255 → 0.95 | 완료 | Unity 확인 대기 | `34ee0de4` |
| 19 버튼 표시 | 설명 중에도 행동 버튼 패널 표시(입력 비활성) | 코드·리뷰 완료 | Unity 확인 대기 | `34ee0de4` |
| 19 Attack만 | `AttackButton`을 Attack 버튼 하위로 좁힘 | 완료 | Unity 확인 대기 | `34ee0de4` |
| T-7 | 전투 입력 “이동만 허용” | 코드·컴파일·리뷰 완료 | Unity 확인 대기. 정책은 2026-10-26 V4-g로 바뀜(§3.10) | `34ee0de4` |
| 14 체력만 | `MainCharacterHealth`를 아이콘·수치로 좁힘 | 완료 | Unity 확인 대기 | `34ee0de4` |
| 21 비용 추가 | `UnitInfoActionCosts = 11` | 완료 | Unity 확인 대기 | `34ee0de4` |
| 병합 복구 | 병합으로 되돌아간 덮개 알파·21 참조 복구 | 완료 | Unity 확인 대기 | `c9543d5d` |
| T-8 | 통합 확인과 마무리 | 미착수 | — | — |

남은 실행 순서는 T-6R·T-7의 Unity 확인 → T-8로 고정했다(2026-10-20).

## 2 남은 일

**확인 필요(먼저)**

- [ ] **1-1 정의 `targetMapKey` 10000:** 지금은 튜토리얼이 꺼져 있다. 의도한 임시값인지 사용자 확인(경위는 설계 §16). 켜려면 1로 되돌린다.
- [ ] **새 Core 규칙과 문구·대상 재검토:** 15 “현재 이 유닛의 턴입니다.”, 유닛별 행동 코스트 설명, `CurrentActorUnit`(지금 필드 메인 캐릭터)(설계 §16). 기획·사용자 결정 뒤 정의 에셋을 고친다.

**Unity 화면·재생 확인 대기**

- [ ] T-6·T-6R: 일반 진입(StageSelect → 1장 첫 전투)과 개발 Combat 직행 각각에서 8·9 실제 카드 강조, 10 전투 버튼, 13~21 대상 옆 설명, 1280×720·1920×1080·2560×1440과 창 크기 변경 때 화면 밖·긴 문구 잘림·HUD/조작부 가림, 설명 중 클릭·설정/가방·Enter/Space 차단과 한 클릭 한 설명, 실습 띠가 입력을 가로채지 않음, 기존 배치 확인창 공존, 종료 뒤 임시 카드 목록·안내 정리.
- [ ] 덮개가 검정 95%로 보이는지(병합 복구 포함).
- [ ] 19: 행동 버튼 패널이 보이고 Attack만 밝으며 설명 클릭이 공격으로 이어지지 않는지.
- [ ] 14: 체력 아이콘·숫자만 밝은지. 원형 테두리 일부가 구멍 여백 8에 걸칠 수 있다(프리팹 대조, 미확인).
- [ ] 21: 하단 이동·공격 비용 영역. 초상화 배경 가장자리 8단위가 여백에 걸칠 수 있다(씬 대조, 미확인).
- [ ] T-7: 이동 실습의 버튼 비활성·키 시도 안내·드래그 경로 보존, 늦은 복귀 미실행, 18 통과 뒤 19 대기 제한, 일반 전투·완료 뒤 입력 복구. 2026-10-26 정책(수동 턴 종료·배치 국면 재진입)은 용근님 4라운드 인수 M11에서 사용자 정상 보고를 받았다([4라운드 계획](2026-10-25-combat-view-stage-round4-plan.md) §8 표 12행). 이 보고가 T-8 전체 통합 확인을 대신하지는 않는다.
- [ ] T-4 로그 재생: Play From Boot → 1장 첫 전투에서 `[Tutorial] 화면 연결 …: 정의 1 Start 요청 …`과 세션 진행 줄, 1층 상점·이벤트나 두 번째 전투에서는 `실행 대상이 아닙니다(…)`. 개발 로그 유지 여부는 T-8에서 정한다.

**Unity 시험 결과를 받지 않은 것**

- [ ] T-1 묶음의 core-system 병합 뒤 재확인(`TutorialBufferTests`, `CharacterRosterServiceTests`, `ChapterGraphPreparationTests`, `ArtifactEditingServiceTests`).
- [ ] T-3: `DataBaseAssetTests`(튜토리얼 시험)와 가짜를 고친 7개 시험 클래스.
- [ ] T-4: `InGameViewFaultCleanupTests`·`RecoveryRequestLifetimeTests`·`CombatMovementViewTests`·`CoreStageCombatCycleTests`·`GlobalInputCompositionTests`.
- [ ] T-5: `GlobalInputCompositionTests`와 입력 에셋을 읽는 시험(`LobbyInputTests`·`KeyBindingTests`·`UnityStageInputTests`·`ViewInputCompositionTests`).
- [ ] T-6: `TutorialViewUITests`·`TutorialViewOverlayCellsTests`와 Boot → Combat 스모크(§3.6).

**지금 에셋과 다른 기존 시험 기대값(시험 수정 금지라 그대로)**

- [ ] `TutorialDefinitionTableTests.NewDefinition_DefaultsToPlannedTimeAndOverlay`·`TutorialViewUITests`(`:96`)·`DataBaseAssetTests.TutorialDefinition_OneOneMatchesPlannedFlow`가 옛 알파 13/255를 기대한다. `DataBaseAssetTests`는 `TargetMapKey == 1`도 기대해 지금 에셋(10000)과 다르다.
- [ ] `TutorialViewUITests.Locator_UsesVisibleGraphicsUnion_AndSkipsMissingTargets`가 체력 대상에 초상화·체력 전체 합집합을 기대한다(지금은 체력 하위만).

**T-8 통합 확인(미착수)**

- [ ] 설계 §14.3의 계약 1~11과 §15.8 사례를 시험 이름 또는 재생 증거로 채운다. 관리 시험·Unity EditMode·재생 증거를 구분한다.
- [ ] 실제 `TutorialDefinition_1-1.asset`의 13단계 `[8,9,10,12,13,14,15,16,17,18,19,20,21]` 전체로 일반 진입·개발 직행 각각 진행한다(12·18은 실습이며 설명 화면이 아님).
- [ ] 1층 상점·이벤트 뒤 첫 전투, 실습 중 승패·포기, 씬 이탈·오류 정리, 완료·중단 뒤 재진입, 설정창·창 비활성화·Enter/Space·창 크기 변경을 확인한다.
- [ ] Framework 공용 파일 공유 메모(민형님)를 쓰고 설계·계획에 최종 상태를 반영한다.

**보류·후속**

- [ ] [T-7 전달 메모](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md) 전달(미전달).
- [ ] 늘 이동할 수 없는 실습 데이터의 재시도·안내 정책(설계 §16).
- [ ] 1장 1층 전투 고정 협의(설계 §15.12).

## 3 작업별 결과

### 3.1 T-1 공개 계약과 상태 저장소 (옛 §3·§12.1·§12.1a)

- **한 것:** `B/Tutorial/`에 enum 열, `TutorialCommandData`, `TutorialRequest`(internal), `TutorialRequestResult`, `TutorialDefinitionInfo`, `TutorialContextSnapshot`, `TutorialSessionSnapshot`, `TutorialStepRule`(+`TutorialDefinitionRules`), `TutorialBuffer`(공개 Sink 셋·내부 Store). `TutorialBufferKey`, 시험 `TutorialBufferTests`.
- **규칙:** 번호 단조 증가·재사용 없음, `HandledRequestNumber`는 연속된 마지막 번호, 결과·종료 세션 보존. `CanContinue`·`CombatInputRestriction`은 저장하지 않고 계산한다. 세션 설치 규칙(받아들인 Start로만 새 세션, 결과-요청 관계, revision +1, Ending 의도 고정, Completed는 Ending 경유)은 Store 한 곳이 막는다. 전투 기준점은 Snapshot 내부 값이며 Start를 받아들이는 Job이 설치한다.
- **검토 반영:** 제한을 Snapshot에 저장하지 않고 단계 규칙으로 계산(M1), 게시 때 결과-요청 관계 검사(M2), 세션 전이 규칙을 Store에(M3), 기준점은 Snapshot 내부 값(M4).
- **계획과 다른 점:** 브랜치를 새로 만들며(`feature/Hojin/StageSelcet` → `StageSelect`) 커밋 전 파일이 사라져 세션 기록으로 복원했다(옛 §12.1a). `.meta`는 새 GUID다(참조하는 곳 없음).
- **다른 작업자 파일:** 공용 `BufferSystemGroup`(`IBufferQuery` 속성 넷), `BufferSystemFactory`(등록). 시험 `ChapterResetSpy`·`ArtifactEditingServiceTests`(전달 줄 넷), `CharacterRosterServiceTests`(등록 한 줄).
- **확인:** Unity Test Runner 통과(2026-10-20, 사용자). 커밋 `3b875440`.

### 3.2 T-2 정의 데이터와 1-1 정의 (옛 §4·§12.2)

- **한 것:** `S/DataBaseSystem/Data/View/Table/Tutorial/`(`TutorialDefinitionDataTableSO`, `Item/TutorialDefinitionDataSO`, `Item/TutorialStepData`, `Item/TutorialFocusTarget`), `TutorialDefinitionTable`(`Get`·`TryGet`·`TryGetByTargetMap`·`GetAll`), 에셋 `TutorialDefinitionDataTable.asset`·`Item/TutorialDefinition_1-1.asset`(키 1, 맵 1, 5초, 단계 13개), 시험 `TutorialDefinitionTableTests`. 정의 → 규칙 변환 `CreateInfo`·`CreateRules`를 정의 Data에 두고 `Validate`도 `CreateRules`를 불러 규칙 불변식을 `TutorialStepRule` 한 곳에 둔다.
- **정한 것(2026-10-20 사용자):** 필드 형식, 설명 구간은 저장하지 않음, 단계 키 = 슬라이드 번호(실습 12·18), 보일 대상 표, 검사 규칙. 내용은 설계 §15.11이 원본이다.
- **다른 작업자 파일:** 공용 `ViewDataBase`(`IViewQuery` 속성), `ViewTableKey`(키), `ViewDataBaseFactory`(한 줄), `ViewGroup.asset`(참조 한 줄). 시험 `IViewQuery` 가짜 일곱(ArtifactPopupConfig·GlobalInputComposition·LobbyInput·LobbyViewSystem·UnityStageInput·ViewInputComposition·ViewPopupComposition) 한 줄씩, `DataBaseAssetTests`에 `TutorialDefinition_OneOneMatchesPlannedFlow`와 Factory 표 확인 한 줄.
- **확인:** 튜토리얼 시험 통과(2026-10-20, 사용자). 같은 날 `DataBaseAssetTests`의 튜토리얼 무관 실패 8건(지형·레일·선 머티리얼·기차·노드 외형)은 범위 밖으로 두었다(사용자 결정). 커밋 `3b875440`.

### 3.3 T-3 TutorialPlan·Job과 Store 전이 (옛 §5·§12.3)

- **한 것:** `L/Tutorial/`의 `TutorialPlanSources`(struct), `TutorialFacts`·`TutorialFactsReader`, `TutorialSessionChanges`, `TutorialProgression`, `Plans/TutorialPlan`·`TutorialPlanStep`, `Jobs/`(요청 처리·종료·최종 게시·설명 시작·다음 대기 전환·만료 진행·실습 통과). `V/Tutorial/ITutorialViewExecution`·`TutorialViewPresentationKind`. 시험 `TutorialPlanTestKit`·`TutorialPlanTests`·`TutorialJobTests`. `TutorialPracticeGoal`을 `B/Tutorial/`로 옮김(GUID 유지).
- **결정(2026-10-20 사용자):** 처음엔 Plan의 사실 읽기가 정의 표를 직접 읽는 안이었으나 “다른 곳에서 쓰는 Job·Plan 방식으로”라는 지시로, 정의 표가 internal이라 실습 통과 조건을 단계 규칙(`TutorialStepRule`)에 넣었다. Plan은 Buffer와 `service`만 읽는다. Plan·Job은 어떤 전이를 할지만 정하고 세션 설치 규칙을 다시 구현하지 않는다.
- **설계와 다르게 읽거나 더한 것(사용자 확정 뒤 설계 문구 고침):** ① 실습 통과의 유닛 = 이동 결과가 났을 때의 행동 유닛(설계 §15.5) ② 설명 시작 Job이 빌리기 직전에 §8.1 전체를 다시 확인 ③ §9.1에 “Starting → 첫 단계”, “실습 안내 표시 끝 → 통과 대기” 두 줄 추가 ④ 실행 소유자가 (세션, revision)마다 마지막 표시의 종류와 완료를 답함 ⑤ 입력 빌리기 실패는 Ending(ExecutionFault) → 정리 → Faulted.
- **다른 작업자 파일:** 공용 `JobRootType`(`Tutorial = 6`), `JobRegisterSystem`(등록 한 줄), `InGameViewSystemGroup`(`TutorialExecution`, 생성자 선택 인자). 시험 `IInGameViewSystemGroupService` 가짜 일곱(CombatEntryIntegration·CombatProgressJob·CombatProgressTestDriver·LobbyDepartureIntegration·RecoveryRequestLifetimeIntegration·RecoverySceneConnection·RecoveryUI) 한 줄씩.
- **확인:** `TutorialPlanTests`·`TutorialJobTests`·`TutorialBufferTests`·`TutorialDefinitionTableTests` 통과(2026-10-20, 사용자). 커밋 `89cbc44e`.

### 3.4 T-4 실행 소유자 (옛 §6·§12.4)

- **한 것:** `V/Tutorial/TutorialViewSystem`(런타임 수명, `ITutorialViewExecution` 구현). Initialize는 정의마다 `CreateInfo()`·`CreateRules()`를 Store에 한 번 넘기고 Combat 씬 연결·해제를 등록한다. Tick은 시간을 깎고 오류·로딩 아님 + Runner 빔이면 `Register(Tutorial, Next)`. Attach는 연결 번호를 게시하고 1-1이면 Start 제출. Detach는 Job 없이 마감. Shutdown은 남은 요청 Failed·활성 세션 Cancelled(RuntimeStopped). 부품 `TutorialViewTimer`·`TutorialViewTarget`(1-1 판정, 전투가 없으면 대상 아님)·`TutorialViewPresentationRecord`. 시험 `TutorialViewTestRig`·`TutorialViewSystemTests`·`TutorialViewTimerTests`.
- **정한 것(기술 선택):** Combat 씬 연결 110·해제 −110(지금은 `SceneNodeOrder.TutorialAttach/Detach`, 2026-10-22 용근님이 옮김). 개발 로그 `[Tutorial] 세션 …`(에디터·개발 빌드만, 유지 여부는 T-8).
- **검토 반영:** 진행 중 `ReturnInput` 실패를 정리 실패로 기록, Factory 조립 시험, 세션 마감 실패가 연결 해제를 막지 않음, 정리 결과가 있는 세션은 그 결과로 마감.
- **다른 작업자 파일:** 공용 `InGameViewSystemFactory`(민형님 작성, 생성·Export·`tutorial` 인자·오류 정리 콜백·개발 로그). `InGameViewFaultCleanup`(선택 인자 `tutorial`)은 호진 작성 파일이다(2026-10-11 View 구조 작업).
- **확인:** `TutorialViewSystemTests`·`TutorialViewTimerTests` 통과(2026-10-20, 사용자). 로그 재생 미확인. 커밋 `89cbc44e`.

### 3.5 T-5 튜토리얼 전용 입력 (옛 §7·§12.5)

- **한 것:** `PlayerInputActions.inputactions` 끝에 `Tutorial` map(액션 `Next`). `V/TutorialInput/`(`TutorialInputNames`·`TutorialInputLayers`·`TutorialInputController`·수신 자리·`TutorialInputFactory`). `V/Tutorial/TutorialViewInputAdapter`(누를 때 `CanContinue`면 세션·revision 기억, 뗄 때 Continue), `TutorialViewInputLease`(`OverrideLayer`, 빌리기 직전 설정 Layer 재확인 — 잡혀 있으면 같은 프레임 경합으로 false), `TutorialViewEventSystemSelection`. 시험 `TutorialInputTests`. T-4 입력 대역 `TutorialViewInputStandIn` 삭제.
- **정한 것(2026-10-20 사용자):** map·Layer `Tutorial`, 액션 `Next`(`Press(behavior=1)`, `<Mouse>/leftButton`), 입력 파일은 `V/TutorialInput/`.
- **다른 작업자 파일:** `InGameViewSystemFactory`(입력 조립·수신 자리), 시험 `GlobalInputCompositionTests` 기대값(Controller 7 → 8, Layer 목록에 `Tutorial`).
- **확인:** `TutorialInputTests`에서 `EventSystemSelection_ClearsAndRestoresOnlyWhenNothingElseWasSelected` 하나가 EditMode의 EventSystem 등록 문제로 실패해 시험만 고친 뒤 전부 통과(2026-10-20, 사용자). 커밋 `89cbc44e`. T-6 전에는 설명 중 화면의 설정 버튼이 예외를 냈다(T-6 화면 막으로 해소).

### 3.6 T-6 초기 UI와 표시 (옛 §8·§12.6)

- **한 것:** `V/Tutorial/TutorialViewUI`(`UIBase`, Parallel, 필수 참조·Canvas Overlay·정렬 100 이상·GraphicRaycaster·막 전체 덮기·실습 안내 Raycast 끔 검사), `TutorialViewOverlay`(구멍 뺀 칸만 그림, `TutorialViewOverlayCells`), `TutorialViewFocusNotifier`, `TutorialViewFocusLocator`, `TutorialViewUIDisplay`. 표시 계약 `ITutorialViewDisplay`에 `Attach`·`Detach`. 실습 통과 때 실습 안내를 닫는 누락을 고침(`TutorialProgression`). 대역 `TutorialViewImmediateDisplay` 삭제. 에디터 도구 `Assets/ProjectL/Editor/Tutorial/TutorialUISetup.cs`(메뉴 ProjectL > Tutorial > 튜토리얼 UI 프리팹 만들고 Combat 씬에 두기, 사용자 요청 — 옛 §12.6). `TutorialUI.prefab`(Canvas 110, 공용 `NoticePannel` 복사본 셋)과 Combat 씬 인스턴스.
- **정한 것(2026-10-20 사용자, 옛 §8 “정한 것 1”):** 대상 위치는 넓게, 안내 셋은 모두 튜토리얼 프리팹, 적은 아무 적 하나. 막힌 안내 2초·구멍 여백 8은 프리팹 값.
- **그 뒤 바뀐 것:** `UIId.Tutorial`은 `34ee0de4`에서 29였고, 같은 날 병합 `9f7ac828`에서 현강님의 `CombatCategoryInfo = 29`·`CombatCostInfo = 30`과 겹쳐 31이 됐다. 옛 손패 조회·위치 1회 계산·고정 설명 박스는 T-6R로 대체했다.
- **알려진 것(M5):** 1-1이 아닌 전투도 Combat 연결에서 튜토리얼 화면을 찾으므로 프리팹이 없으면 모든 Combat 진입이 예외로 멈춘다. 그 전에는 Boot → Combat 스모크(`ConnectedWallBootSmokeTests`·`StageMapCombatEntryBootSmokeTests`·`StageMapNodeMovementBootSmokeTests`·`CombatTimelineRetirementBootSmokeTests`·`HojinCombatAttackDragIntegrationTests`·`ShopStagePlaySmokeTests`·`StageMapDepthCenteringBootSmokeTests`·`LobbyBootSmokeTests` Combat 사례)가 실패했다.
- **다른 작업자 파일:** 공용 `UIIDs`(`Tutorial`, 지금 31).
- **확인:** Unity 미확인. 커밋 `34ee0de4`.

### 3.7 T-6R UI 보완 (옛 §8.1·§12.7·§12.8)

- **배경:** 2026-10-20 기획 원본 대조(설계 부록 A) 뒤 사용자 “네 그 방향으로 설계를 확정하고, 기존 설계 문서와 Task 계획에 반영해주세요.”
- **한 것:** `TutorialViewFocusLocator`가 활성 `DeckCardList`의 실제 `UnitCardUI`와 Main Owned ID를 읽음. `TutorialViewCardVisibility`·`TutorialViewCardVisibilityObserver`(개발 직행 카드 목록 임시 개방·외부 닫힘 시 의무 버림). `TutorialStepData`에 `explanationAnchor`·`explanationPlacement`, 새 `TutorialViewExplanationPlacement`, 1-1 에셋 기준 대상·방향. `TutorialViewExplanationLayout`(후보 위치·화면 안 보정). `ITutorialViewDisplay.RefreshLayout()`. 프리팹 크기 설명 520×120·실습 760×88·막힌 안내 520×64, 생성 도구 신규 값도 맞춤(기존 프리팹 덮어쓰기 방지 유지). 씬은 고치지 않고 프리팹 변경을 상속.
- **슬라이드 11:** 기존 `CombatDeploymentConfirmation`·`CombatActionNotice`를 그대로 쓴다(사용자 추가 지시).
- **시험:** 지시 전에 쓴 카드·배치 계산·정의 검증·표시 갱신 시험(`TutorialViewCardVisibilityTests`·`TutorialViewExplanationLayoutTests`·`TutorialViewFocusLocatorTests` 등)은 보존했고, 지시 뒤에는 컴파일 오류 정리만 했다.
- **확인:** 관리 빌드 오류 0, 최종 코드 리뷰 결함 없음. Unity 컴파일은 같은 프로젝트가 다른 Unity에서 열려 있어 돌리지 못했다. Unity 확인 대기. 커밋 `34ee0de4`.

### 3.8 덮개 투명도 정정 (옛 §12.9)

- 사용자 “약 95% 투명으로 말고 5% 투명으로 해주세요.” → 1-1 정의 실제 색, `TutorialDefinitionDataSO` 신규 기본값·설명, `TutorialUI.prefab` Overlay 초기색, `TutorialUISetup` 신규 기본값을 알파 0.95로 맞췄다. 실행 색은 계속 정의에서 읽는다.
- 옛 알파를 기대하는 기존 시험은 그대로 두었다(§2). 커밋 `34ee0de4`.

### 3.9 19번 행동 버튼 표시와 Attack만 강조 (옛 §12.10·§12.11)

- **원인:** `CombatActionButtons.Read`가 Layer가 Combat이 아니면 숨김이라, 설명 중(튜토리얼 Layer) 19의 공격 버튼이 꺼지고 대상 조회에서도 빠졌다.
- **한 것:** 튜토리얼 Layer이면서 `GetDesiredLayer`가 Combat일 때 행동 버튼을 보이되 입력은 비활성(`CombatActionButtons`의 `CanUseReturn` 등 표시 측 허용 값, 기존 4인자 생성자 호환). 이어서 `CombatActionPanel`이 공격 버튼 RectTransform을 읽기 전용으로 주고 `AttackButton` 대상만 그 하위로 좁혔다(설명 회피 영역은 패널 전체).
- **다른 작업자 파일:** 전투 View `CombatActionButtons`(지금 `Combat/Input/ActionButtons/`), `CombatActionPanel`(지금 `Combat/Display/Hud/`).
- **확인:** 관리 빌드 오류 0, 리뷰 결함 없음. Unity 확인 대기. 커밋 `34ee0de4`.

### 3.10 T-7 이동 실습 입력 제한 (옛 §9·§12.12)

- **한 것(2026-10-20):** 전투 View 여덟 파일. `CombatViewSystem`이 `IBufferQuery`에서 튜토리얼 상태·요청 Sink를 받고, 검사마다 제한을 읽어 Policy의 허용식을 버튼·Router·최종 명령/미리보기 검사에 공유한다. 이동 명령·이동/범위 미리보기 허용, 공격·복귀·아이템 차단, 공격·복귀·턴 종료 버튼 비활성(빈 범위 예외의 턴 종료만 허용). Router가 복귀·공격 전환·턴 종료를 경로 취소·모드 종료 전에 막고 실제 시도만 `ReportBlockedInput` 한 건. 빈 범위 근거는 `CombatMovementRangeTracker`가 수락된 `Succeeded && Destinations.Length == 0` 응답의 전투·행위자·시작 칸·결과 순번으로 보관·무효화. 늦은 복귀 견적은 `CombatPathInput`에서 최신 제한으로 막음(`CombatReturnEstimateTracker`는 그대로).
- **다른 작업자 파일(전투 View, 지금 위치):** `Combat/Input/CombatInputFacts`·`CombatInputPolicy`·`CombatInputRouter`, `Input/ActionButtons/CombatActionButtons`, `CombatViewSystem`, `Input/MovePath/CombatMovementRangeTracker`·`CombatPathInput`, `Input/Interaction/CombatInteraction`. 전달 메모 [T-7](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md)(미전달).
- **확인:** 관리 빌드 오류 0·새 경고 0, 시험 파일 해시 변화 없음, 범위·최종 리뷰 결함 0. Unity 확인 대기. 커밋 `34ee0de4`.
- **그 뒤 바뀐 것(다른 작업자):** 2026-10-24 용근님 V2-f가 Sink 둘을 `Input/Assist/CombatBlockedInputReport`로 모았다. 새 Core(천명님 `d4a6d6fd`)에서 복귀가 폐지됐다. 2026-10-26 V4-g(Q-V32 A·Q-V38 A)가 “빈 범위일 때만 턴 종료”를 “MoveOnly에서도 수동 플레이어 턴 종료 + 미완료 실습의 배치 국면 재진입”으로 바꿨다(설계 §15.10). 옛 빈 범위 추적 API는 후속 정리 부채로 남았다.

### 3.11 14번 체력·21번 비용 강조 (옛 §12.13·§12.14)

- **14:** `MainCharacterHealth`가 `MainCharactorUI` 전체를 잡던 것을 `MainCharactorUI.HealthBarTransform`(기존 `healthBarRoot`, 읽기 전용) 하위로 좁혔다. 회피 영역은 상단 HUD 전체 그대로.
- **21:** `TutorialFocusTarget.UnitInfoActionCosts = 11`을 21에만 지정. `CombatUnitInfo`에 `moveCostRoot`·`attackCostRoot`를 연결해 읽기 전용으로 주고 네 모서리 합집합으로 구한다. `CombatSceneRoot.UnitInfo` 누락 때 조용히 건너뛰던 분기를 예외로 고쳤다(검토 지적).
- **다른 작업자 파일:** 현강님 `MainCharactorUI`, `CombatUnitInfo`(지금 `Combat/Display/Hud/`, 현강님 작성).
- **확인:** 관리 빌드 오류 0. 체력 시험 하나는 옛 기대값이라 그대로(§2). Unity 확인 대기. 커밋 `34ee0de4`.

### 3.12 병합 뒤 덮개·21번 연결 복구 (옛 §12.15)

- **원인:** 병합 `526fe7e5`에서 1-1 정의 알파가 둘째 부모의 13/255로 합쳐져 덮개가 거의 투명해졌고, 21번 하단 비용 참조도 빠졌다.
- **한 것:** 알파 0.95, `moveCostRoot`·`attackCostRoot`를 `UnitInfo/Cost/Move`·`UnitInfo/Cost/Atk`에 다시 연결(제품 변경은 이 세 값). 사용자가 씬에 다시 넣은 TutorialUI 인스턴스 하나와 `targetMapKey: 1`·`autoAdvanceSeconds: 5`를 보존했다.
- **확인:** 관리 빌드 오류 0. Unity 확인 대기. 커밋 `c9543d5d`. 그 뒤 `83e53dac`에서 `targetMapKey`가 다시 10000이 됐다(§2).

### 3.13 T-8 통합 확인 (옛 §10)

미착수. 할 일은 §2 “T-8 통합 확인”이다.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [T-7 튜토리얼 이동 실습 입력 — 전투 View 전달](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md) | 용근님(전투 View) | 미전달(용근님이 V4-g·V4-m·V5-d 기록을 덧붙임) |
| Framework 공용 파일 변경(`JobRootType`·`JobRegisterSystem`·Buffer/View 그룹·Factory·서비스 키·`ViewDataBaseFactory`·`UIIDs`) | 민형님 | 미작성(T-8) |
| [전투 View 구조 개선 호진님 공유](../handoffs/2026-10-22-combat-view-structure-hojin-share.md) | 받은 메모(용근님 → 호진) | 튜토리얼 코드 변경 내역 |
