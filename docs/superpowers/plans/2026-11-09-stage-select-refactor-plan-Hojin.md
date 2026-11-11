# 스테이지 선택 리팩토링 계획

- 작성자: 호진(이호진)
- 설계: [스테이지 선택 리팩토링 설계](../specs/2026-11-09-stage-select-refactor-design-Hojin.md)(2판). 결정은 모두 끝났다.
  - Q-SR7·Q-SR8(2026-11-09 사용자): 카메라 구도 도구는 지운다. Core 요청 대신 지금 우리 영역에서 할 수 있는 해법으로 한다.
  - 나머지 Q-SR1~Q-SR6·Q-SR9~Q-SR12(2026-11-09 사용자 "권장대로"): 모두 A.
- 근거: [평가 보고서](../specs/2026-11-05-stage-select-view-evaluation-Hojin.md)·[문제 목록](../specs/2026-11-05-stage-select-view-evaluation-issues-Hojin.md). 평가 기준 판 `ecfd52b7b`, 이 계획의 줄 번호는 HEAD `c995dd455` 기준이다.
- 상태: SR-a 코드 완료(§4.1, Unity 확인 문제 없음 — 사용자, 11-09). SR-b 코드 완료(§4.3, 컴파일 미확인, Unity 확인 대기, b6 여섯은 결정 대기). 둘 다 커밋 전. 다음 SR-c는 사용자 지시로 시작.

## 0 진행 규칙

- Phase 하나가 끝나면 다음 순서로 진행하고 멈춘다. 다음 Phase는 사용자 지시로 시작한다.
  1. 실행 기록(§4)을 쓴다.
  2. Codex 검수를 받는다. 읽기 전용(`codex exec -s read-only`)이고, 표준 입력은 닫는다(`< /dev/null`, 11-09 평가 검수가 입력을 기다리다 멈췄음). 자동으로 다시 돌리지 않는다.
  3. 보고한다.
- 새 시험 코드는 만들지 않는다. MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 쓴다.
  - 기존 시험이 깨지면 호출만 맞춘다. 다른 사람이 쓴 시험도 호출만 맞추고 그 사람에게 넘긴다(호진님 작업 규칙).
  - Phase 착수 전에 `Assets/Tests`에서 바뀌는 형식을 다시 검색한다. 11-09 검색에서는 `DeckEditUI`를 쓰는 현강님 시험 둘(`DeckMainChangePolicyTests`·`DeckSlotInfoPanelTests`)만 걸렸다.
- 바꾸는 줄마다 `git blame`으로 작성자를 확인한다. 범위 파일 안이면 다른 사람 줄도 고치고, §5에 적어 넘긴다.
- 보호 영역은 연결 줄만 바꾼다. Framework·GlobalSystem 내부, Core, `ServiceKeys` 계약, Factory 공용 조립이 그렇다. Core 파일은 고치지 않는다(설계 §1).
- 현강님만 쓴 UI 파일은 결함과 호출 맞춤만 고친다(평가 설계 §2.3). 이 계획에서 닿는 곳은 `DeckSlotInfoAbilities`(N-48) 하나다. 여러 사람이 쓴 `DeckEditUI`는 C-06·C-26의 결함·규약 수정만 한다.
- 프리팹·씬 값을 바꿔야 하면 Unity에서 할 일 목록을 먼저 넘긴다(Codex·사용자 몫).
- 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.
- 공용 `ViewSystem/Common` 부품을 바꾸면 로비 호출처도 함께 확인하고, Unity 확인 목록에 로비 항목을 넣는다.
- 경로 머리: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `SM/` = `V/StageMap/`, `LC/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/`, `UI/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/`.

## 1 Phase 요약

| Phase | 문제 | 예상 13기준 평균(알려진 문제 기준, 설계 §6) |
|---|---|---|
| SR-a 구도 도구 지우기 + 결함 | C-22, C-05, C-10, C-15, C-18, C-06, C-09, C-26, C-16, 낮음 C10·C9·B8 | 83.3 → 약 89.6 |
| SR-b 구조 | C-01, C-19, C-12, C-02, Scope 나누기, 낮음 C2·C4·C8·B1·B13 | 약 93.5 |
| SR-c SSOT | C-04, C1 낮음 | 약 94.2 |
| SR-d 정리 | C3·C5·C6·C12·C13 낮음 | 약 97.5 |
| SR-e 성능 | C-13, C7 낮음, 노드 호버 진입 가능 읽기 | 약 98.7 |
| SR-f 재평가 | 같은 평가 설계로 다시 잼 | — |

- 남기는 낮음(설계 §5): C-24·N-34의 현강님 UI 몫, N-47, N-87, C-25, 그리고 C-04·C-09의 남는 사본 몫.

## 2 Task

### 2.1 SR-a 구도 도구 지우기 + 결함

| Task | 내용 | 파일(주로) | 문제 |
|---|---|---|---|
| a1 | 에디터 카메라 구도 도구를 지운다. `SM/Editor/` 파일 다섯과 `.meta`, 빈 폴더와 폴더 `.meta`. 런타임 갈래는 Scope(접근 구현·`BindCameraFraming`·Tick 미리보기·준비 미리보기·해제 둘), `StageMapCameraDrag`(미리보기 갈래·`_previewSettings`·`_dragging` 우회·`PrepareManualFraming`·`PreviewFraming`·에디터만 쓰던 `GetCurrentFraming`), `StageMapViewScene`(`CameraFraming`·`PreviewCameraFraming`), `StageMapViewInput`(구도 수신자·`TickCameraFraming`), `StageMapSceneRoot`(접근 Bind/Unbind·`CameraFramingKey`). 입력은 `IStageCameraFramingInputReceiver`, 컨트롤러 에디터 갈래 셋(Cancel은 `Press` 하나), 슬롯 구도 전달, 이름 `CameraModifier`·`CameraDolly`. 입력 에셋 액션 둘은 그대로 | `SM/Editor/*`, `SM/StageMapViewScope.cs`, `SM/StageMapCameraDrag.cs`, `SM/StageMapViewScene.cs`, `SM/StageMapViewInput.cs`, `SM/StageMapSceneRoot.cs`, `V/StageSelectionInput/*` | C-22, N-13·N-19·N-20·N-21·N-35·N-88 |
| a2 | Stage 도메인 정지를 오류 경계로 보고한다. `StageProgress` 생성을 생성자 안으로 옮기고 보고를 `_faults.Report(new InvalidOperationException(reason))`로. `IStageTraitGrowthFacts.IsGrowthStopped`·구현 한 줄·증강 팝업 정지 갈래를 걷는다. 주석 넷 | `LC/StageLifeCycle.cs`, `LC/IStageTraitGrowthFacts.cs`, `LC/IStageProgress.cs`, `LC/Plans/StagePlan.cs`, `LC/Jobs/StageCheckJob.cs`, `SM/TraitGrowth/TraitGrowthPopupController.cs` | C-05, N-64 |
| a3 | 출전 부품 Tick에 "도착·미제출·덱 창 닫힘이면 `Cancel()`" 한 줄(`:136-141` 뒤, `:142` 앞). 덱 편집 `Show`가 `_departureRun`을 늘 다시 정한다(Basis 없으면 비움). 출전 표시 계약은 `as` 대신 Factory가 넘기는 필수 인자로 | `SM/StageCombatDeparture.cs`, `SM/Deck/DeckEditPopupController.cs`, `SM/Deck/DeckEditPopupViewFactory.cs` | C-10, N-33, N-11 |
| a4 | 노드 진입 판정을 나눈다. "선택 화면 열림" 질문을 두고(TP-2), Scope에 `CanEnterNode`(지금 바탕 판정 + 선택 화면 열림)를 둔다. `StageMapInteraction`에는 호버용(지금 바탕 판정)과 진입용(`CanEnterNode`) 대리자를 따로 넘기고, `InterceptEntry`도 `CanEnterNode`를 본다(TP-1) | `SM/StageMapViewScope.cs`, `SM/StageMapInteraction.cs`, `SM/StageMapViewInput.cs`, `SM/StageMapSelectionPresentation.cs` | C-15(F1 경로) |
| a5 | 불일치 닫기 정리. 상점 `_skipNextRestore`를 걷고, 불일치 닫기는 `returnToSelection = true`. 회복은 닫는 이유 필드를 두고 `_hiddenContext`는 밖에서 숨겨진 경우에만 건다. 회복 불일치·상태 바뀜 닫기는 `CloseThen(true, _mapChanged)`. 다시 여는 조건과 닫는 조건이 같은 사실을 보는지 맞춘다 | `SM/Shop/ShopPopupController.cs`, `SM/Recovery/RecoveryPopupController.cs` | C-15, N-22 |
| a6 | 입력 감싸개 `FaultReportingStageSelectionReceiver`를 새로 만든다(로비 `FaultReportingLobbyReceiver` 꼴). `StageMapViewInput`이 경계를 받아 `Interaction`을 감싸 슬롯에 건다. `InterceptEntry`·`BlocksEntryForGrowth`의 자체 try는 TP-4 확인 뒤 걷는다. 상점 입력 의도 `SelectCard`·`Exit`의 try 밖 줄도 경계 안으로 | `V/StageSelectionInput/FaultReportingStageSelectionReceiver.cs`(새), `SM/StageMapViewInput.cs`, `SM/StageMapViewScope.cs`, `SM/Shop/ShopPopupController.cs` | C-18, N-81 |
| a7 | 공용 안내 도우미 `StageMapViewNotice`(TP-3)를 새로 만든다. 맵 진입 거절 문구 원본 `StageMapEntryText`(새), `StageMapViewServices`에 안내 서비스(Global Context의 `UITextPresentation`), 진입 관찰자 거절 콜백, 출전 `TryBegin` 결과 열거(가로채지 않음·시작함·막음·연결 안 됨) | `SM/StageMapViewNotice.cs`(새), `SM/StageMapEntryText.cs`(새), `SM/StageMapEntryObserver.cs`, `SM/StageMapViewEntry.cs`, `SM/StageCombatDeparture.cs`, `SM/StageMapViewScope.cs`, `SM/StageMapViewServices.cs`, `SM/StageMapViewSystem.cs` | C-16 |
| a8 | 덱 편집 이유 안내. `DeckEditText`를 안내 문구만 남기고, 표시 모델의 `Status`·`Categories`를 걷는다. 컨트롤러가 거절·편집 불가·낡음 이유가 바뀔 때 `StageMapViewNotice`로 한 번 띄운다. `DeckEditUI`에 다시 평가 버튼 켜고 끄는 입구 하나, 컨트롤러가 Stale·NotReady일 때 켠다. Builder 주석. **C-08:** 놓기(교체) 요청이 거절되면 `DeckEditDraft`가 현재 후보의 평가 상태를 바꾸지 않고 "마지막 거절 사유"만 따로 남긴다(TP-10). 안내는 그 사유로 띄운다 | `SM/Deck/DeckEditText.cs`, `SM/Deck/DeckEditModelBuilder.cs`, `SM/Deck/DeckEditDisplayModel.cs`, `SM/Deck/DeckEditDraft.cs`, `SM/Deck/DeckEditPopupController.cs`, `SM/Deck/DeckEditPopupViewFactory.cs`, `UI/DeckEditUI.cs`, `UI/IDeckEditDisplay.cs` | C-06, C-08 |
| a9 | 칸에 안 맞는 덱. Builder가 예외 대신 칸 밖 카드를 세고 모델 편집 가능을 끈다(`CanEdit && 칸에 맞음`), 컨트롤러가 안내 한 번. 출전은 그대로(`HasChanges` 거짓). `DeckEditDraft` 머리 주석을 사실대로. 칸 구조(`DeckEditSlotLayout`)는 그대로(TP-5) | `SM/Deck/DeckEditModelBuilder.cs`, `SM/Deck/DeckEditDisplayModel.cs`, `SM/Deck/DeckEditDraft.cs`, `SM/Deck/DeckEditPopupController.cs` | C-09, N-32 |
| a10 | 덱 편집 화면 구성 검사. `DeckEditUI.Validate()`(상태 변경 없음, `Prepare` 검사부 + 출전 버튼·확인창 위치 + 보유 슬롯 프리팹, 출전 버튼·확인창 필수), `DeckCombatConfirmUI.Validate()`(버튼 둘). `StageSelectPanelUI.ValidateComposition`이 부른다. `Prepare`는 `Validate()` 뒤 바인딩만 | `UI/DeckEditUI.cs`, `UI/DeckCombatConfirmUI.cs`, `UI/StageSelectPanelUI.cs` | C-26 |
| a11 | 나머지 C10·C9 낮음. 고칠 방향은 문제 목록대로다 | 문제 목록의 위치 | C-14, C-27, N-04, N-23, N-44, N-48(현강 UI 결함), N-52, N-58, N-80 |

- 순서: a1 → a2 → a3 → a4 → a5 → a6 → a7 → a8 → a9 → a10 → a11. a1이 맨 앞인 까닭은 a4·a6이 구도 갈래를 피해 가지 않게 하려는 것이다. a7의 안내 도우미를 a8·a9가 쓴다.
- C-17은 이미 풀렸다(설계 §0). 고치지 않고 재평가에서 확인한다.
- a8·a10은 `DeckEditUI`(현강·용근·민형·호진 줄)를 바꾼다. 현강님 시험 둘이 깨지면 호출만 맞춘다.
- a7은 `StageMapViewSystem`에서 `global.UITextPresentation`을 한 곳 더 넘긴다. Global Context 경로 그대로라 서비스 등록·Export는 바뀌지 않는다.

**Unity 확인(SR-a)** — 재현이 어려운 항목(★)은 건너뛰어도 되고, 그때는 완료가 아니라 "미확인"으로 남긴다.
1. 스테이지 선택 진입 때 맵 카메라가 씬에 저장된 구도로 시작한다. 휠은 맵 이동만, 우클릭은 취소만 한다(Alt를 눌러도 같다).
2. 출전: 전투 노드 → 착지 → 덱 창 → 출전 → 전투 진입이 지금과 같다. 덱 창에서 그냥 닫으면 말과 카메라가 돌아온다.
3. ★ 출전 버튼을 누른 직후(두루마리 닫힘 중) 덱 창이 밖에서 닫히면 출전이 취소되고 노드를 다시 누를 수 있다.
4. 상점·회복·훈련소를 정상 완료하면 지금처럼 맵으로 돌아온다. 완료한 방문은 다시 열리지 않는다.
5. ★ 상점·회복이 **불일치로** 닫히면(예: 열린 채 바깥에서 상태가 바뀜) 선택 화면이 돌아오고, 그 방문이 끝나지 않았으면 다시 열린다. 닫혔다 열리기를 혼자 반복하지 않는다.
6. 선택 화면이 아직 안 열린 순간(첫 표시·팝업에서 돌아온 직후)에는 노드 클릭이 받히지 않고, 열린 뒤에는 받힌다.
7. 갈 수 없는 노드를 누르면 공용 안내가 3초 뜬다. 연결 안 된 전투 노드도 같다.
8. 덱 편집에서 거절되는 놓기를 하면 이유 안내가 뜨고, 덱은 그대로 확정·출전할 수 있다(C-08). 다시 평가가 필요한 상태면 다시 평가 버튼이 보인다.
9. ★ 같은 병과 서브 둘인 로드아웃으로 시작해 덱 편집을 연다. F1 없이 편집이 꺼진 채 열리고 안내가 뜬다. 출전은 된다. 확인 뒤 로드아웃은 되돌린다.
10. 씬에서 `DeckEditUI`의 필수 참조 하나(예: 덱 칸 하나)를 잠시 비우고 스테이지 선택에 들어간다. 첫 덱 열기가 아니라 **씬 연결 때** 오류(F1)로 멈춘다. 확인 뒤 되돌린다.
11. ★ Stage 도메인 정지(처리 기록 누락)가 로그만 남지 않고 F1로 멈춘다. 정상 플레이로는 재현이 어렵다.
12. ★ Tick 밖 입력 콜백에서 예외가 나면 F1로 멈추고 그 뒤 입력을 받지 않는다. 정상 플레이로는 재현이 어렵다.
13. 콘솔에 새 예외가 없다. 로비 입력·팝업도 지금과 같다(공용 부품을 바꾼 경우).

### 2.2 SR-b 구조

| Task | 내용 | 파일(주로) | 문제 |
|---|---|---|---|
| b1 | 팝업 공통 계약 `IStageMapViewPopup`(`IsOpen`·`IsWaiting`·`Tick()`·`IDisposable`)을 컨트롤러 넷이 구현한다. `StageMapViewPopups`는 목록으로 Tick·Dispose를 돌고 `IsAnyOpen`·`IsAnyWaiting`을 낸다. Scope 판정을 `IsMapIdle`·`CanEnterNode` 둘로 모으고 다시 나열한 조건·`CanInteractWithNodes` 별명·낡은 주석·인트로 이중 판정·`TickHover` 셋째 인자 손 조합을 정리한다. 맵·상점 입력 Buffer 비우기 주인을 하나로 | `SM/IStageMapViewPopup.cs`(새), `SM/StageMapViewPopups.cs`, 컨트롤러 넷, `SM/StageMapViewScope.cs`, `SM/StageMapViewSystem.cs` | C-01, N-06 |
| b2 | 서비스 묶음을 쓰는 쪽별로 나눈다(씬 표시·노드 진입·맵 입력·팝업 조립·Scope). 연결 사이에 남는 상태 객체 셋은 System이 갖고 필요한 묶음에만. `Entry.Departure` 사후 setter는 생성 인자로. Scene·Input의 안쪽 부품 공개를 줄인다. 맵 입력의 운영 의존 넷을 필수로 받고 null 갈래를 걷는다 | `SM/StageMapViewServices.cs`(→ 묶음들), `SM/StageMapViewSystem.cs`, `SM/StageMapViewScene.cs`, `SM/StageMapViewEntry.cs`, `SM/StageMapViewInput.cs`, `SM/StageMapInteraction.cs`, `S/Factory/Runtime/InGame/View/StageMapSceneFactory.cs` | C-19, N-03, N-07 |
| b3 | 요청 수명 공통 부품 `ViewRequestExecution`(조합)을 둔다. 회복·증강 소유자가 쓴다. 결과 번호 비교는 소유자마다 이름 있는 속성 하나, 증강 컨트롤러 `:66`은 그것을 읽는다. `StageTraitGrowthProgress`의 Core 상태 읽기는 그대로. Job 둘의 공통 몸통 정리. 상점은 그대로 두고 이유 주석 | `V/Common/ViewRequestExecution.cs`(새), `V/Recovery/RecoveryRequestLifetime.cs`, `V/TraitGrowth/TraitGrowthRequestLifetime.cs`, `SM/TraitGrowth/TraitGrowthPopupController.cs`, `LC/Jobs/RecoveryCommandJob.cs`, `LC/Jobs/TraitGrowthCommandJob.cs`, `SM/Shop/ShopPopupController.cs` | C-12, C-28 일부 |
| b4 | 마커 점프 이동 부품 `StageMapViewMarkerJump`. 두 부품의 차이(카메라 확정 시점, 출전만 착지 확정 게시 전 취소면 카메라 되돌림, 출전 정리 범위)만 자기 쪽에 둔다. 비율 12/59·35/59는 이름 있는 상수 | `SM/StageMapViewMarkerJump.cs`(새), `SM/StageMapNodeMovement.cs`, `SM/StageCombatDeparture.cs` | C-02 |
| b5 | Scope 나누기. 진입 준비 → `StageMapViewEntryPreparation`, 비전투 복원 대기 → `StageMapViewNoncombatRestore`. Scope에는 시작·Tick·정리 순서와 판정만 | `SM/StageMapViewEntryPreparation.cs`(새), `SM/StageMapViewNoncombatRestore.cs`(새), `SM/StageMapViewScope.cs` | (설계 Q-SR11) |
| b6 | 나머지 C2·C4·C8 낮음. N-34는 우리 쪽 몫만 | 문제 목록의 위치 | C-21, N-63, N-70, N-79, C-20, C-23, N-16, N-61, N-78, N-34(일부), N-65, N-67 |

- 순서: b1 → b2 → b4 → b5 → b3 → b6. b1이 판정을 먼저 모아야 b2·b5가 옮기기 쉽다. b3은 맵 Scope와 독립이라 뒤에 둔다.
- b2는 Factory 연결 줄(`StageMapSceneFactory`)을, b3은 `InGameViewSystemFactory.cs:75-79` 생성 줄을 바꿀 수 있다. 민형님 공유 문서에 적을 것을 §5에 모은다. 서비스 키·그룹은 바꾸지 않는다.
- b3은 `V/Common`에 새 부품을 둔다. 로비가 쓰지 않으므로 로비 동작은 바뀌지 않는다.

**Unity 확인(SR-b)**
1. 맵 호버·끌기·휠·노드 클릭, 덱 열기 버튼, 증강 팝업 열기가 지금과 같다(팝업이 열린 동안·결과를 기다리는 동안 막힘).
2. 상점·회복 완료 뒤 복귀, 전투 뒤 맵 복귀의 비전투 복원이 지금과 같다.
3. 비전투 노드 이동·출전 이동의 점프 리듬·소리·카메라 동행이 지금과 같다. 출전 취소 때 카메라가 출발 위치로 돌아온다.
4. 회복·증강 요청(선택·완료·확인)이 지금과 같다. 전투 진입·씬 이탈 때 요청이 정리된다.
5. 맵 진입 연출(첫 방문)이 지금과 같다.

### 2.3 SR-c SSOT

| Task | 내용 | 파일(주로) | 문제 |
|---|---|---|---|
| c1 | 증강 진행 열쇠 대조 검사. `UNITY_EDITOR \|\| DEVELOPMENT_BUILD`에서만, 캐시를 쓴 경우 1초에 한 번 Core를 다시 읽어 결과를 견주고 다르면 오류 경계에 보고(원본 위치 두 곳을 문구에). 열쇠 주석에 원본 위치 | `LC/StageTraitGrowthProgress.cs` | C-04 |
| c2 | C1 낮음 사본을 하나씩 원본으로 모은다. C-24는 우리 쪽 사본만 | 문제 목록의 위치 | C-03, C-24(일부), C-28, C-29, N-02, N-18, N-24, N-25, N-27, N-29, N-30, N-38, N-40, N-41, N-71, N-72, N-73, N-74, N-75 |
| c3 | 증강 소비 확인 불변식·결과 대조를 Buffer 쪽 한 곳으로(천명님 Buffer 줄, §5) | `S/RuntimeSystem/BufferSystems/…/TraitGrowthViewBuffer.cs` 등 | N-57 |

- c1의 오류 경계 보고 경로: `StageTraitGrowthProgress`는 LifeCycle이 가진다. LifeCycle이 이미 받는 `_faults`를 쓰도록 생성 인자를 확인한다(구현 때).

**Unity 확인(SR-c)**: 에디터에서 Run 시작 → 전투 → 레벨업 → 증강 선택 → 다음 노드까지 진행하는 동안 대조 검사 오류가 나지 않는다. 노드 진입·덱 편집·상점 표시가 지금과 같다.

### 2.4 SR-d 정리

| Task | 내용 | 문제 |
|---|---|---|
| d1 | C3 낮음 | N-10, N-66 |
| d2 | C5 낮음(죽은 경로·이미 보장된 검사·늘 넘기는 인자) | C-11, N-01, N-05, N-14, N-15, N-17, N-26, N-31, N-36, N-42, N-43, N-46, N-49, N-53, N-54, N-55, N-68, N-76 |
| d3 | C6 낮음(관례). N-87은 남김 | N-09, N-28, N-51, N-56, N-60, N-69, N-86 |
| d4 | C12 낮음(주석·문서) + 평가 뒤 생긴 낡은 주석(Scope "세 팝업", `StageSelectionInputNames` 휠 이름) | C-30, N-08, N-50, N-59, N-77, N-85 |
| d5 | C13 낮음(UI 규약) | C-07, N-37, N-39, N-45 |
| d6 | 평가 뒤 생긴 죽은 멤버 `StageMapViewScene.ClearBackground` | — |

- d5의 N-37: 열기 버튼은 이미 `UIInputBinding.Attach`를 쓴다. 남은 문제는 그 뒤의 `Action _open`·`BindOpenRequest(Action)` 연결이다(`DeckEditUI.cs:77`·`:116`·`:278`·`:348`, `DeckEditPopupController.cs:46`). 이것을 명시적 수신 계약(예: `IDeckEditOpenReceiver`)으로 바꾸고 연결·해제 호출도 바꾼다. Button 클릭·Submit 동작은 그대로다.
- d5의 N-39(Hide 재정의와 `OnUIHidden` 정리 나뉨)는 `OnUIHidden`으로 맞춘다. 현강님만 쓴 파일이면 손대지 않고 남긴다(구현 때 blame 확인).

**Unity 확인(SR-d)**: 스테이지 선택 전체 흐름(진입·노드 이동·상점·회복·훈련소·증강·덱 편집·출전·복귀)이 지금과 같다. 콘솔에 새 예외가 없다.

### 2.5 SR-e 성능

| Task | 내용 | 문제 |
|---|---|---|
| e1 | 회복 팝업은 열 때·결과 때·`_current.Recovery` 참조가 바뀐 때만 Core 투영. 매 Tick은 참조 비교 하나. 결과 프레임은 첫 읽기 값을 다시 씀. 전제(회복 결과 밖 변경 없음)를 클래스 주석에. 결과 조회(`_status.TryGetResult`)는 참조 변경과 상관없이 지금처럼 매 Tick 본다. 완료 결과는 회복 객체를 바꾸지 않고 클리어만 설치할 수 있어서다(`StageServiceTransactions.cs:205-211`) | C-13 |
| e2 | 이동 중 같은 프레임 현재 노드 두 번 읽기, 증강 남음 판정 여러 번 읽기, 포인터 이동마다 UI RaycastAll | N-82, N-83, N-84 |
| e3 | 노드 호버가 매 프레임 진입 가능 읽기(`StageNodeAvailabilityReader.FillAvailable` → 증강 `IsPending`)를 부르는 것을 "바뀐 때만"으로. 열쇠가 바뀔 때 보이는 노드마다 Core `PrepareStageEntry`를 부르는 것은 그대로 두되 한 프레임에 한 번만 | (평가 뒤 생긴 것, N-83과 함께) |

**Unity 확인(SR-e)**: Profiler로 회복 팝업을 띄워 둔 동안과 노드 위에 포인터를 둔 동안 GC Alloc이 0에 가깝다. 회복 선택(적용)·완료가 지금과 같다 — 특히 완료하면 팝업이 닫히고 맵으로 돌아온다(회복 객체가 바뀌지 않는 완료도 결과로 처리됨). 노드 호버 윤곽선이 지금과 같다.

### 2.6 SR-f 재평가

- 같은 평가 설계로 다시 잰다. 범위는 지금 범위 + 평가 뒤 새 파일(진입 연출·카메라 동행·아웃라인)과 이 리팩토링의 새 파일이다. 기준 판은 SR-e 커밋이다.
- 1차와 견준다. 고친 문제와 새로 찾은 문제를 나눠 적는다.
- 평가 Codex 검수는 표준 입력을 닫고 돌린다.

## 3 계획에서 정한 것 (사용자 확인 대상)

- **TP-1:** 노드 진입 판정 나누기(`CanEnterNode`)는 C-15의 F1 경로 때문에 SR-a에서 먼저 넣는다. 설계는 §2.6(SR-b)에 두었다. SR-b의 b1은 그 위에 나머지 판정을 모은다.
- **TP-2:** "선택 화면 열림"은 UISystem에 공개 조회가 없다(`IUISystemService`에 `IsOpen` 없음, GlobalSystem은 고치지 않음). 그래서 이미 씬 루트에 연결된 선택 화면(`root.UI.Selection`, `StageMapCanvasView.cs:16`)의 **실제 표시 상태**를 `StageMapSelectionPresentation`이 질문 하나로 읽는다. `OpenUI`·`CloseUI` 호출을 기억하는 방식은 쓰지 않는다. UISystem의 외부 `CloseUI`·`HideAllUI`가 그 기록을 거치지 않아 열림 사실과 어긋나기 때문이다. 첫 Open의 동기 콜백 안에서는 UISystem 등록이 끝나지 않았다는 지금 규칙(`UISystem.cs:372-373`, `StageMapSelectionPresentation.cs:50`)은 그대로 지킨다.
- **TP-3:** 공용 안내 표시는 새 작은 도우미 `StageMapViewNotice` 하나로 한다. 3초 상수 하나를 두고, 맵 진입 거절(C-16)과 덱 편집 안내(C-06·C-09)가 함께 쓴다. 문구는 기능마다 자기 문구 클래스(`StageMapEntryText`·`DeckEditText`)에 둔다. 설계에는 이 도우미가 없었다.
- **TP-4:** `InterceptEntry`·`BlocksEntryForGrowth`의 자체 try는 그 판정이 입력 콜백 경로(감싸개 안)에서만 불리는지 확인한 뒤 걷는다. Tick 경로에서도 불리면, 그 Tick 경로가 오류 경계 안인지 확인하고 아니면 남긴다.
- **TP-5:** C-09에서 `DeckEditDraft.CreatePlacementRequest`의 중복 예외(`:111`)는 불변식으로 남긴다. 칸에 안 맞는 덱은 편집이 꺼져 닿지 않는다.
- **TP-6:** 출전 표시 계약(N-11)은 덱 Factory가 필수 인자로 넘긴다. 화면 묶음(`DeckEditPopupViewScreen`)에서 꺼내는 방식은 구현 때 정한다.
- **TP-7:** 구도 도구를 지운 뒤 빈 `SM/Editor/` 폴더와 폴더 `.meta`도 지운다. 입력 에셋 `PlayerInputActions`의 `CameraModifier`·`CameraDolly` 액션, 에디터 `SessionState` 값은 그대로 둔다(보고만).
- **TP-8:** 대조 검사(C-04) 주기는 1초 상수 하나, 조건은 `UNITY_EDITOR || DEVELOPMENT_BUILD`다.
- **TP-9:** 다른 사람 시험이 깨지면(컴파일·실행 모두) 묻지 않고 호출만 맞추고 §5에 적는다. 확인 내용(단언)은 바꾸지 않는다. 근거는 2026-10-26 사용자 지시("다른 작업자의 코드를 바꾸는 것이 테스트 코드라면 문제 없이 바꿔줘")로, 호진님 작업에서는 CLAUDE.md의 "다른 사람 시험은 실행에서 깨지면 먼저 묻는다"(2026-10-22, 전투 View 기준)보다 우선한다. 단언을 바꿔야만 맞출 수 있으면 그때는 묻는다.
- **TP-10:** C-08은 "놓기 거절은 현재 후보 상태를 바꾸지 않고 마지막 거절 사유만 남긴다"로 한다(문제 목록 고칠 방향의 첫째 안). 거절 뒤 Core를 다시 평가하는 둘째 안은 요청이 하나 더 들고 안내가 늦어서 고르지 않았다. `Committed`를 요청 때 먼저 지우는 것(`DeckEditDraft.cs:116` 근처)도 수락 때로 옮긴다.

## 4 실행 기록

(Phase마다 결과 요약만 남긴다: 한 것·계획과 다른 점·확인 상태·커밋.)

### 4.0 계획 Codex 검수 (2026-11-09, 읽기 전용, HEAD `c995dd455`)

- 높음 0·중간 3·낮음 2.
- **수용(반영함):**
  - 중간 TP-2: 열고 닫은 호출을 기억하는 대안은 외부 `CloseUI`·`HideAllUI`와 어긋남 → 실제 선택 화면 표시 상태만 읽도록 확정.
  - 중간 Unity 확인: SR-a 핵심 실패 경로(도메인 정지 F1, 불일치 닫기 뒤 복귀·재복원·반복 방지, 입력 예외 F1, 씬 연결 때 덱 화면 참조 누락) 빠짐 → 추가, 재현 어려운 것은 ★·미확인 규칙. SR-e에 회복 객체가 바뀌지 않는 완료 결과 처리를 e1·확인 목록에 명시.
  - 낮음 C-08: 배정만 있고 후보 상태 바로잡기 Task 없음 → a8에 `DeckEditDraft` 변경·TP-10.
  - 낮음 N-37: `UIInputBinding`은 이미 씀, 남은 것은 `Action` 연결 → d5에 수신 계약 교체로 명시.
- **근거 있는 반려:**
  - 중간 TP-9(다른 사람 시험의 실행 실패는 먼저 물어야 함): 2026-10-26 사용자 지시가 호진님 작업에서는 CLAUDE.md 10-22 규칙보다 우선한다(TP-9에 근거를 적음). 단언을 바꿔야 하는 경우만 묻는다.
- 확인만: a1 삭제 범위에 빠진 참조·과잉 삭제 없음(빌드 구도·휠·Cancel은 남음), 순서 역전 없음, a5·a9·e1 구현 가능, TP-3·TP-4 문제 없음, 낮음 102개 배정 누락 없음.

### 4.1 SR-a 구도 도구 지우기 + 결함 (2026-11-09, 사용자 "그대로, SR-a 진행" = TP-1~TP-10 그대로)

- 상태: 코드 완료. 새 시험 없음. **Unity 확인 문제 없음**(§2.1 목록 1~13, 사용자 "확인 문제 없음", 11-09). 커밋 전.
- 한 것:
  - a1 카메라 구도 도구 지움: `StageMap/Editor/` 다섯(+ `.meta`)과 빈 폴더 `.meta`, 런타임 갈래(Scope 접근 구현·연결·미리보기 셋·해제 둘, `StageMapCameraDrag`의 미리보기 갈래·`_dragging` 우회·에디터 함수 셋·`_initial`·`_boss`, `StageMapViewScene`·`StageMapViewInput`·`StageMapSceneRoot`의 구도 칸), 입력 계약 `IStageCameraFramingInputReceiver`·컨트롤러 에디터 갈래(Cancel은 `Press` 하나)·슬롯 전달·이름 둘. 끌기 부품의 구도는 이제 늘 씬 카메라 값(`_framing` 읽기 전용).
  - a2 Stage 도메인 정지 → `_faults.Report`(생성자에서 `StageProgress` 생성), `IStageTraitGrowthFacts.IsGrowthStopped`·구현·증강 팝업 정지 갈래 걷음, 주석 넷. N-64는 F1 뒤 이탈 검사가 오류 상태를 먼저 봐서 함께 풀림(코드 확인, `StageLifeCycle.cs:147`).
  - a3 출전 부품 Tick에 "도착·미제출·덱 창 닫힘 → `Cancel()`", 덱 편집 `Show`가 `_departureRun`을 늘 다시 정함, 출전 표시 계약은 Factory 필수 인자(`DeckEditPopupViewFactory.Create(screen, departureScreen, …)`, Popups가 같은 `DeckEditUI`를 두 계약으로 넘김).
  - a4 노드 진입 판정: `StageMapSelectionPresentation.IsSelectionShown`(StageSelect 화면 객체 `isActiveAndEnabled`, 등록 UI는 닫히면 숨겨짐), Scope `CanEnterNode`, `StageMapInteraction`은 호버 = 맵 판정·진입 = `canEnterNode`로 나눔, `InterceptEntry`도 `CanEnterNode`.
  - a5 상점 `_skipNextRestore` 걷음·닫기는 늘 선택 화면 복귀(`CloseAndRefresh()`), 회복은 닫는 이유 필드 `_closingByController`(밖에서 숨겨질 때만 `_hiddenContext`)·스스로 닫기는 `CloseThen(true, _mapChanged)`.
  - a6 새 `StageSelectionInput/FaultReportingStageSelectionReceiver`(+ `.meta`), `StageMapViewInput`이 경계를 받아 감싸개를 슬롯에 검, `InterceptEntry`·`BlocksEntryForGrowth` 자체 try 걷음(TP-4: 둘 다 입력 콜백 `RequestEntry`에서만 불림), 상점 `SelectCard`·`Exit` 본문을 try 안으로(N-81).
  - a7 새 `StageMapViewNotice`(3초)·`StageMapEntryText`(+ `.meta`), 서비스 묶음 `Notice`(System이 `global.UITextPresentation`으로 만듦), 진입 관찰자 거절 콜백, 출전 `TryBegin` → `StageCombatDeparture.StartResult`(연결 안 됨이면 안내).
  - a8 덱 편집: 표시 모델 `Status`·`Categories` 걷고 `CanReevaluate`·`FitsLayout`, `DeckEditText.Notice`(상태 안내)·`LayoutMismatch`, 컨트롤러가 이유가 바뀔 때만 안내, `DeckEditUI.Render`가 다시 평가 버튼을 `CanReevaluate`로 켜고 끔. C-08: `DeckEditDraft.ApplyPlacementEvaluation`(덱 규칙 거절이면 상태 그대로 + 거절 사유만)·`TakePlacementRejection`, `Committed` 지우기는 수락 때로.
  - a9 칸에 안 맞는 덱: Builder가 예외 대신 `fitsLayout = false`, 모델 `CanEdit = draft.CanEdit && fitsLayout`.
  - a10 `DeckEditUI.Validate()`(상태 없음, `Prepare` 검사부 + 출전 버튼·확인창 필수·위치) → `Prepare`가 먼저 부름, `DeckCombatConfirmUI.Validate()`, `StageSelectPanelUI.ValidateComposition`이 `deckEditUI.Validate()`, `RenderDeparture`·`BindDeparture`·`IsDepartureConfirmationOpen`의 null 허용 걷음.
  - a11: C-14 회복 `IsWaiting` = 소유자 `HasPending`(제출 조건은 자기 영수증도 계속 봄), C-27 시작 노드는 클릭 상자를 켜지 않음, N-04 선택 화면 부모가 숨으면 맵이 한가할 때 다시 엶, N-23 상점·증강의 유닛·아이템 외형 누락은 예외(키 숫자 대체 걷음), N-44 제안 레이아웃 Open용 `Prepare`가 먼저 `Validate`(Awake는 `TryPrepare`), N-52 노드 외형 DB 검사에 건물 프리팹 필수, N-58 Stage 대상 화면 글의 나가기·완료 버튼 글·가격 꼴 필수(DB, Codex 검수 뒤 줄임), N-80 수락·착지 게시 뒤 입력 제거는 취소가 아니라 정리만.
- 계획과 다른 점:
  - **N-48(현강 UI) 고치지 않음.** `DeckSlotInfoAbilities` 복제 슬롯 배치는 코드 주석("같은 위치에 겹치고 원래 3번 슬롯이 앞에 그려지도록")상 의도된 꼴이라 결함인지 확실하지 않다. 현강님 확인이 필요하다(§5).
  - N-58은 일부만: 화면 종류마다 다른 글 종류(lines)는 View DB가 화면 종류를 몰라 쓰는 순간 검사를 남겼다.
  - C-17은 계획대로 손대지 않음(이미 풀림).
- 시험: 현강님 `DeckMainChangePolicyTests:224`·`DeckSlotInfoPanelTests:272`의 `DeckEditDisplayModel` 생성 인자만 맞춤(단언 그대로, TP-9). `DeckMainChangePolicyTests`가 읽는 옛 `DeckEditUI.prefab`은 `reevaluateButton`·`startCombatButton`·`combatConfirmUI`가 비어 있어, 이 변경 전에도 `Prepare` 검사에서 실패했을 것으로 보인다(이번 변경 무관, Unity 결과로 확인).
- 같은 작업 트리의 다른 작업: Codex 훈련 카드 작업(TC-A, `2026-11-09-training-rank-effect-card-view-*`)의 커밋 전 변경이 있다. 겹친 파일은 셋이고 hunk가 서로 다르다 — `TrainingCardContentBuilder.cs`(우리: `UnitName` 대체 걷음), `ShopPopupViewFactory.cs`(우리: 외형 조회 셋·using, 그쪽: 강화 Builder 삭제), `StageOfferTextData.cs`(우리: Stage 필수 칸, 그쪽: 글 종류). 그 밖의 훈련 카드 파일(에셋·`StageOfferLineKind`·`TraitVisualDataSO`·`ShopResultPolicy`·`TrainingTraitValue*`·`EnhancementShopOfferContentBuilder` 삭제)과 `Tool/Editor/CombatStageSoDraftStore.cs`는 이 작업이 아니다.
- 바꾼 다른 작업자 줄: `DeckEditText`·`DeckEditModelBuilder`·`DeckEditDisplayModel`·`DeckEditDraft`(용근 줄 대부분, 현강 줄 몇), `DeckEditUI`(용근 3·현강 1), `ShopPopupController`(천명 1·민형 1), `RecoveryPopupController`(민형 1). §5에 넘길 내용.
- 커밋(제안): 새 `.cs` 셋(+ `.meta`)·지운 `StageMap/Editor/` 다섯(+ `.meta`, 폴더 `.meta`)과 함께 한 커밋. a1만 따로 해도 된다. 훈련 카드 작업과 겹친 세 파일은 hunk를 나누거나 그 작업과 같은 커밋에.
- 같은 작업 트리에 SO 모드 도구 쪽 변경(`Tool/Editor/CombatLayoutWindow*`, SO 모드 설계·계획)도 있다. 이 작업이 아니다.

### 4.2 SR-a Codex 검수 (2026-11-09, 읽기 전용)

- 높음 1·낮음 2. 컴파일 쪽(지운 형식·바뀐 생성자·반환형을 아직 부르는 곳)은 찾지 못했고, 동작 쪽(정상 출전 충돌·노드 클릭 영구 차단·닫고 다시 여는 고리·N-04 재열기 반복·C-08 상태 오염·안내 중복)도 찾지 못했다. Tick 예외는 `GameBootstrap.Update` 오류 경계로 간다.
- **수용(고침):**
  - 높음 N-58: Stage 대상 글에 불가·뒤로 버튼 글까지 필수로 두면 운영 에셋(회복 `backButtonText`, 상점·용병 `unavailableButtonText`·`backButtonText`가 빈 값)에서 DB 생성이 멈춘다 → 모든 Stage 화면이 쓰는 칸(나가기·완료 버튼 글·가격 꼴)만 필수로 줄임. 네 운영 에셋 모두 이 세 칸이 차 있음을 확인했다. 내 처음 에셋 확인 스크립트가 빈 문자열(`""`)을 빈 값으로 세지 못해 놓쳤다.
  - 낮음 N-44 주석: `Prepare`가 이제 바로 검사하는데 주석은 "미룬다" → Awake용 `TryPrepare`(지금처럼 조용히 미룸, 씬 연결 검사가 F1로 알림)와 Open용 `Prepare`(먼저 `Validate`)로 나눔. Awake 콘솔 예외가 새로 생기지 않게 한 것이다. `StageOfferScreenUI.Awake`는 `TryPrepare`를 부른다(호진 줄).
- **근거 있는 반려:**
  - 낮음 a10 출전 버튼 위치 검사 없음: 옛 `RenderDeparture`도 확인창 위치만 검사했고 출전 버튼 위치는 검사한 적이 없다. 씬 배치를 정적으로 확인하지 못해(프리팹 인스턴스 계층) 새 조건을 넣으면 씬 연결이 멈출 위험이 있다. 계획 a10의 "출전 버튼·확인창 위치"는 "출전 버튼 존재·확인창 위치"로 읽는다. 위치 검사가 필요하면 Unity에서 계층을 확인한 뒤 SR-d(C13)에서 더한다.

### 4.3 SR-b 구조 (2026-11-09, 사용자 "SR-b 진행")

- 상태: 코드 완료(b1·b2·b4·b5·b3, b6은 일부). **컴파일 미확인**, 새 시험 없음, Unity 확인 대기(§2.2 목록 1~5). 커밋 전.
  - 참고: 작업 중 Unity가 열려 있어 Editor.log에 중간 컴파일(21:26) 성공이 남았다. 그 뒤 고친 파일(b6의 Stage 수명·StageJob·카메라 범위·표시 정체)은 다시 컴파일되지 않았다. 이것은 확인 근거가 아니다.
- 한 것:
  - b1 팝업 공통 계약: 새 `StageMap/IStageMapViewPopup`(`IsOpen`·`IsWaiting`·`Start`·`Tick`, `IDisposable`)을 컨트롤러 넷이 명시 구현(덱·증강의 `IsWaiting`은 늘 거짓 — 옛 판정도 상점·회복 대기만 봄). `StageMapViewPopups`는 만든 순서 목록으로 시작·Tick·정리를 돌고 `IsAnyOpen`·`IsAnyWaiting`을 낸다. Scope 판정은 `IsMapIdle`·`CanEnterNode` 둘로(공통 부분 `IsMapQuiet`, 인트로 이중 판정은 `IsIntroInteractive`, 호버 셋째 인자는 `IsHeldByIntro`). 선택 화면 표시·호버·끌기·휠·덱 열기·증강 열기·개발 도구·복원이 `IsMapIdle`만 본다. 다시 나열한 조건·`CanInteractWithNodes` 별명·낡은 주석 걷음. N-06: 맵·상점 입력 Buffer 비우기는 System 한 곳(`_requestWriter`·`_shopWriter`를 System이 직접 가짐). Scope `Dispose`와 상점 컨트롤러 `Dispose`의 비우기 걷음.
  - b2 서비스 묶음 나누기: 새 `StageMapViewSceneServices`(6)·`StageMapViewEntryServices`(10)·`StageMapViewInputServices`(7)·`StageMapViewPopupServices`(23). `StageMapViewServices`는 넷과 Scope가 직접 쓰는 칸만 담는다(로비 2차 R2-c와 같은 꼴). 씬·진입·입력·출전·씬 조립은 자기 묶음만 받는다. 연결 사이에 남는 상태 객체 셋은 System이 만들어 필요한 묶음에만(가능 읽기 → 입력, 인트로 기록·낙하 이펙트 → Scope 칸). N-03: 맵 선택 입력은 `StageMapViewInput` 안에만(`IsCreated`·`ClearPointer`), 출력 카메라는 씬 부품 안에만(`CreatePicker`), 출전은 진입 관찰자 시작 인자(`StartObserver(departure, …)`, 사후 setter 걷음). N-07: `StageMapInteraction`의 끌기·UI 가림·증강 막기·출전 가로채기 필수, null 갈래 걷음.
  - b4 새 `StageMapViewMarkerJump`: 노드 찾기·로컬 좌표·점프 리듬(이름 있는 상수 `RiseShare`·`TravelShare`·`LandShare`)·지연 감싸개·이동 소리·카메라 진행률·말 움직임 끝·거두기. 일반 이동은 착지 때 `CommitCamera`, 출전은 수락 때. 출전만 취소 때 카메라 되돌림(`Cancel(restoreCamera)`). 끊긴 이동의 표시 폐기 조건은 두 부품에 그대로.
  - b5 Scope 나누기: 새 `StageMapViewEntryPreparation`(준비 원인·준비 문맥·버전·이동 문맥·`TryPrepare`·`Cancel`·다시 그리기 시작), 새 `StageMapViewNoncombatRestore`(복원 대기·한가할 때 한 번 되살림). Scope 556 → 335줄. 로딩 준비가 쓰는 입구 셋은 Scope가 그대로 넘긴다(`StageMapLoadingPreparation` 무변경).
  - b3 새 `ViewSystem/Common/ViewRequestExecution`(제출 중·예약·실행 중·정지·종료, `TryBegin`·`TryFinish` 앞부분, `RequireAvailable`, `Fail`, 종료 때 Runner 비우기). 회복·증강 소유자가 조합으로 쓴다(생성자 서명 그대로 → Factory 무변경). 결과 번호 비교는 소유자마다 `HasUnacknowledgedResult` 하나, 증강은 `ITraitGrowthRequestSubmitter.HasUnacknowledgedResult`로 내고 증강 컨트롤러가 읽는다(컨트롤러·Factory·묶음·System의 증강 입력 읽기 칸 걷음). Job 둘은 새 `LifeCycles/Stage/Jobs/StageRequestCommandJob`(추상 몸통)을 상속. 상점은 그대로, 이유 주석.
  - b6 일부: N-65 진입 시도 닫힘을 `HasExited`로, N-70 `StageJob`이 챕터 진행 하나만 받고 첫 챕터는 이름 있는 상수, N-79 카메라 범위 규칙을 새 `StageMapCameraBounds`로, N-16 표시 정체 비교를 맵 설정 참조 비교로(설정은 DB가 만든 불변 Data, System이 한 번 받음), C-23 덱 편집 사실을 새 `LifeCycles/Stage/StageDeckEditFacts`로 옮김(Stage 수명은 만들어 넘기기만, `_current`·`_stages` 필드 걷음), N-78은 b1로 판정이 Scope 둘로 모여 풀린 것으로 봄.
- 계획과 다른 점:
  - **b6 중 여섯(C-20·C-21·N-34·N-61·N-63·N-67)은 하지 않았다.** 설계에 방법이 없고 바꾸는 범위가 크거나(Stage 도메인 목록·상점 방문 종류 나누기·표시 모델 위치), DB SO·에셋 구조가 바뀌거나(N-61·N-63), 보호 영역 계약을 바꿔야 한다(N-67: `ServiceKeys`·LifeCycle 그룹의 `IStageProgress` 노출). 할지·어떻게 할지는 사용자 결정 대기.
  - 출전은 설계의 "Entry 생성 인자"가 아니라 진입 관찰자 시작 인자로 받는다. 진입 부품은 표시 소유권 때문에 팝업보다 먼저 만들어지고 출전은 덱 팝업 뒤에 만들어지기 때문이다. 그래서 진입 관찰자 시작을 팝업·출전 뒤로 옮겼다(그 사이 입력은 `IsReady`가 거짓이라 막히고, 관찰자 Tick은 준비 표시 뒤에만 돈다).
  - 팝업 공통 계약에 `Start()`를 더했다(설계는 `IsOpen`·`IsWaiting`·`Tick`·`IDisposable`). 목록이 시작까지 같은 꼴로 돌게 하려고, 로비 `ILobbyPopup`과 같게 했다.
  - 요청 수명 공통 부품은 보관 번호를 갖지 않는다. 회복은 번호와 명령 전체를 한 값(`RecoveryViewCommand`)으로 대조하므로 번호만 떼면 대조가 두 곳이 된다. 번호 비교 입력(`pending`)은 소유자가 넘긴다.
  - `StageMapViewServices`는 걷지 않고 쓰는 쪽별 묶음 넷을 담는 꼴로 남겼다(로비 R2-c와 같음). 큰 평면 묶음(인자 43개)은 없어졌다.
- 동작 차이(의도, 재생 확인 대상):
  - 진입 관찰자·출전 시작이 팝업 시작 뒤로 옮겨졌다. 준비 표시 전에는 둘 다 돌지 않으므로 화면 동작은 같다고 본다.
  - 맵·상점 입력 Buffer는 Scope 정리가 다 끝난 뒤 System이 한 번 비운다(옛: Scope 정리 중간·상점 정리 중에도 비움).
  - 덱 편집 사실 부품은 서비스가 하나라도 없으면 만들 때 멈춘다(옛: null이면 거짓으로 답함). 운영 연결에서는 모두 있다.
- 시험: 바뀐 형식을 쓰는 시험 없음(착수 전 `Assets/Tests` 검색). 고친 시험 없음.
- 바꾼 다른 작업자 줄: 현강님 이동 소리·오디오 연결 줄(`c5329fcc`: `StageMapNodeMovement`·`StageCombatDeparture`의 소리 재생 → 마커 점프 부품, `StageMapViewServices`·`StageMapViewSystem`의 오디오 칸 → 진입 묶음·System 지역 변수), 천명님 개발 도구 판정 줄(`4782921e`, Scope `CanUseDevelopmentTools` 본문), 용근님 줄(`38869ec`: Scope 시작 순서 한 줄, Stage 수명의 덱 편집 사실 → `StageDeckEditFacts`), 민형님·천명님 `StageJob` 줄(필드·생성자·Execute). §5에 넘길 내용.
- 새 파일(+ `.meta`): `StageMap/` `IStageMapViewPopup`·`StageMapViewSceneServices`·`StageMapViewEntryServices`·`StageMapViewInputServices`·`StageMapViewPopupServices`·`StageMapViewMarkerJump`·`StageMapViewEntryPreparation`·`StageMapViewNoncombatRestore`·`StageMapCameraBounds`, `Common/ViewRequestExecution`, `LifeCycles/Stage/Jobs/StageRequestCommandJob`·`LifeCycles/Stage/StageDeckEditFacts`. `.meta`는 대부분 열린 Unity가 만들었고 `StageMapCameraBounds`·`StageDeckEditFacts` 둘은 직접 만들었다.
- Factory: `StageMapSceneFactory.Create`는 서명 그대로, 본문만 팝업 묶음을 읽는다. `InGameViewSystemFactory` 무변경.
- 커밋(제안): SR-a 커밋 뒤 SR-b 한 커밋. 새 `.cs` 열둘을 `.meta`와 함께.

### 4.4 SR-b Codex 검수 (2026-11-09, 읽기 전용)

- 높음 0·중간 0·낮음 0. 지운 멤버를 부르는 곳·생성자 불일치·구현 누락(Tests 포함)을 찾지 못했다.
- 확인만: Scope 판정이 HEAD 조건식과 같음(SR-a 의도 변경 제외), 준비·복원·관찰자 순서·버전·카메라 복구 조건 유지, 마커 점프 이동의 시작·착지·취소·카메라 확정 시점 유지, 회복·증강 요청 수명과 `HasUnconsumedResult` 의미 유지, `Detach`의 마지막 게시 비교가 다음 연결 입력을 보호, `StageDeckEditFacts`·`DisplayIdentity` 대응, 보호 영역 수정 없음.
- 계획 차이 두 가지(출전을 시작 인자로, b6 여섯 보류)는 타당하다고 봄. 다만 b6 여섯은 미구현이라 SR-b 전체 완료로 보지 않는다(기록 그대로).

## 5 다른 작업자에게 넘길 것 (예정, 미전달)

- **현강님:** `DeckEditUI` 다시 평가 버튼(`model.CanReevaluate`)·`Validate()` 떼어내기·출전 버튼/확인창 필수(SR-a a8·a10), 시험 둘(`DeckMainChangePolicyTests:224`·`DeckSlotInfoPanelTests:272`)의 생성 인자 맞춤, **질문:** `DeckSlotInfoAbilities` 넷째 이후 특성 슬롯이 템플릿 자리에 겹쳐 놓이는 것(N-48)이 의도인지. SR-b: 노드 이동 소리 재생이 `StageMapViewMarkerJump` 한 곳으로 옮겨짐(소리·조건 그대로), 오디오 서비스는 진입 묶음으로.
- **용근님:** `DeckEditText`를 안내 문구만 남김, 표시 모델 `Status`·`Categories` 걷음(a8), 칸에 안 맞는 덱 처리(a9). SR-b: 덱 편집 사실 구현이 `StageLifeCycle`에서 새 `StageDeckEditFacts`로 옮겨짐(값·뜻 그대로, 서비스 누락은 만들 때 예외), `StageDeckEditWindow`는 무변경.
- **천명님:** 증강 Buffer 줄(N-57, c3), 증강 진행 열쇠가 Core 원본(`CharacterRosterService.ReadTraitGrowthProgressCore`·`SameSource`)을 베낀다는 사실과 개발 빌드 대조 검사(c1). Core 요청은 하지 않는다. SR-b: 개발 도구 판정(`CanUseDevelopmentTools`)이 맵 한가함 하나로, `StageJob`이 챕터 진행 서비스 하나만 받음.
- **민형님:** Factory 연결 줄(`StageMapSceneFactory`는 SR-b에서 서명 그대로·본문만 팝업 묶음을 읽음, `InGameViewSystemFactory` 무변경). SR-b: `StageJob` 생성자가 `IInGameSystemGroupService` 대신 `IChapterProgressionCoordinator`를 받음(Plan 한 줄).
- 기록 문서: 스테이지 맵 설계(카메라 구도 도구 "지움" 한 줄).

## 6 커밋 묶음 (제안)

- Phase마다 한 커밋을 권한다. 새 `.cs`는 `.meta`와 함께, 지운 `.cs`는 `.meta`와 함께 넣는다.
- SR-a의 a1(지우기)은 따로 커밋해도 된다. 나머지 SR-a Task와 섞이지 않는다.
- 작업 트리에 이 작업과 무관한 FMOD 에셋 변경 둘(`New Fmod Event Catalog.asset`·`FMODStudioCache.asset`)이 있다. 커밋에서 뺀다.
