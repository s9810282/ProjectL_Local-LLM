# 스테이지 선택 View 평가 — 문제 목록

- 작성자: 호진(이호진). 2026-11-05 실행, 기준 판 `ecfd52b7b`.
- 보고서: [스테이지 선택 View 평가 보고서](2026-11-05-stage-select-view-evaluation-Hojin.md). 평가 설계: [평가 설계](2026-11-05-stage-select-view-evaluation-design-Hojin.md).
- 검증을 통과한 문제 116개(중간 14, 낮음 102)와 버린 후보 2개를 적는다. 높음은 없다.
- 번호: `C-`는 합치기 단계에서 중간 이상 후보였던 것, `N-`은 낮음 후보였던 것이다. **심각도와 주된 기준은 각 항목의 "최종" 줄이 원본**이고, 그 아래 "후보 심각도"·"주된 기준" 줄은 합치기 때 값이다(최종 줄과 다르면 최종 줄을 따른다).
- 판정 방식: 중간 이상 후보는 증거 검증자·반론 검증자가 따로 판정했고, 갈린 8개(C-08·C-11·C-12·C-17·C-24·C-25·C-27·C-28)는 판정자가 셋째로 판정해 가운데 값을 썼다. 낮음 후보는 검증자 둘이 반씩 판정했다(올림 없음).
- 경로 약어: `VS/` = `RuntimeSystem/InGameSystem/ViewSystem/`, `VS/SM/` = `VS/StageMap/`. 접두어 없는 `StageMap…`·`Deck…` 파일은 `VS/StageMap/` 또는 `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/`, LifeCycle 파일은 `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/`. 줄 번호는 기준 판 기준이다.
- (현강 UI) 칸: 현강님만 쓴 UI 파일에 있는 문제. 평가 설계 §2.3에 따라 리팩토링에서는 결함·호출 맞춤만 고친다. 해당: C-24(일부), N-34(일부), N-47, N-48, N-87.

## 최종 집계

| 기준 | 중간 | 낮음 | 점수 |
|---|---|---|---|
| C1 SSOT | 3 (C-02·C-04·C-12) | 21 | 67.5 |
| C2 SOLID | 1 (C-19) | 7 | 82.5 |
| C3 FW 계약 | 0 | 2 | 95.0 |
| C4 변경 독립성 | 2 (C-01·C-22) | 6 | 75.0 |
| C5 반복·검사 | 0 | 20 | 90.0 |
| C6 C#·Unity | 0 | 9 | 90.0 |
| C7 매 프레임 | 0 | 3 | 92.5 |
| C8 캡슐화 | 0 | 3 | 92.5 |
| C9 수명 | 1 (C-10) | 4 | 82.5 |
| C10 정확성 | 5 (C-05·C-06·C-09·C-15·C-18) | 16 | 52.5 |
| C11 핫패스 할당 | 1 (C-13) | 1 | 90.0 |
| C12 주석 | 0 | 6 | 90.0 |
| C13 UI 규약 | 1 (C-26) | 4 | 82.5 |
| 합 | 14 | 102 | 13기준 평균 83.3 |

- 계산: 기준마다 100 − 중간 7.5×개수 − min(낮음 2.5×개수, 10). 같은 근본 원인 묶음이 한 기준 안에서 −15를 넘는 곳은 없었다(가장 큰 곳 C1 B5 −12.5, C10 B2 −12.5).
- 낮음 102개의 기준별 개수는 최종 줄 기준이다(C-11은 C10 → C5, C-24는 C4 → C1로 옮김, 버린 N-12(C10)·N-62(C2)는 뺌).

## 근본 원인 묶음

| 묶음 | 근본 원인 | 속한 항목 |
|---|---|---|
| B1 | 팝업 열림·대기 상태가 팝업 묶음 안의 질문이 아니라 Scope에서 컨트롤러별로 손 조합됨(공통 계약·목록 없음) | C-01, C-17, N-06 |
| B2 | 덱 편집의 평가·거절 결과가 화면에 닿지 않고, 상태가 "현재 후보"가 아닌 것을 가리킴 | C-06, C-08, N-32, N-33 |
| B3 | View가 덱 칸 구조(병과당 한 칸·4칸·병과 순서)를 Core 규칙과 따로 정함 | C-09, C-24, N-29, N-41 |
| B4 | 팝업 닫기 후속(`afterClose`)이 버려지는 경로에 대체 알림이 없음(`StagePopupViewLifetime`) | C-10, N-12 |
| B5 | "제출 → 실행 → 결과 확인" 요청 수명·입력 Buffer가 기능마다 다른 꼴로 사본 | C-11, C-12, C-14, C-28, N-57 |
| B6 | 복원 억제 표지(`_skipNextRestore`·`_hiddenContext`)가 방문·닫힘 이유와 묶이지 않음 | C-15, N-22 |
| B7 | 입력 콜백(런타임 Tick 밖)의 예외가 F1 오류 경계에 닿지 않음 | C-18, N-81 |
| B8 | 에디터 카메라 구도 도구가 런타임 클래스·입력에 `#if UNITY_EDITOR`로 섞임 | C-22, N-13, N-19, N-20, N-21, N-35, N-88 |
| B9 | 루트를 켠 채 패널만 토글하는 화면이 UIBase Open/Hide 수명을 우회해 자체 열림 상태를 가짐 | N-38, N-39 |
| B10 | Stage 도메인 정지가 사실로만 남고 화면·F1로 이어지지 않음 | C-05, N-64 |
| B11 | 노드·요청에 대한 "이름 있는 질문"이 Buffer/공용 규칙에 없어 각자 식을 씀 | C-03, N-02, N-71, N-72, N-73 |
| B12 | 비정상 경로를 `Debug.Log`로만 보고 | C-16, N-25 |
| B13 | 넓은 서비스 묶음·내부 부품 노출로 부품 의존이 서명에 안 드러남 | C-19, N-03 |

---

## 문제 — 후보 C (최종 중간 14·낮음 16)

### C-01 팝업 열림·대기 판정이 Scope 안에 여러 벌로 손 조합되고 팝업이 공통 계약 없이 여러 곳에 나열됨
- **최종: 중간** (C4) — 높음 후보에서 낮춤(검증 둘 다 중간). 새 팝업 추가는 드물고 고칠 곳은 두 파일
- 주된 기준: C4 / 관련 기준: C1, C8, C2
- 후보 심각도: **높음** (원본: L2-1 높음, S1-03 중간, L1-1 중간, L6-1 중간)
- 근본 원인 묶음: B1
- (현강 UI): 아님
- 위치:
  - `VS/StageMap/StageMapViewScope.cs:62-63, 69-70, 185, 191-192, 198-199, 207-214(209-211), 222, 238, 288-307`
  - `VS/StageMap/StageMapViewPopups.cs:16-36, 43-86, 89-98, 101-109` (S1: `:23-109`)
  - `Factory/Runtime/InGame/View/StageMapSceneFactory.cs:30-39(40)`
  - `VS/StageMap/StageMapViewServices.cs:35-93`, `StageMapViewSystem.cs:71-110`
  - `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/StageScreenRootUI.cs:10-11, 30-39`
- 내용: 팝업 넷(상점·회복·덱·증강)이 구체 타입 필드로 보관되고 `IsOpen`·`IsWaiting`·`TryRestore`가 공통 인터페이스가 아니다. Scope의 `CanUseDevelopmentTools`·`IsAnyPopupOpen`·`CanShowSelection`·`CanOpenDeckEdit`·`OpenTraitGrowthWhenIdle`·`InterceptEntry`·`CancelReturnWhileBusy`가 `IsReady`·`_isShown`·`IsPresenting`·`IsDepartureBusy`·`HasPendingRequest`·팝업 IsOpen/IsWaiting·`IsSelectionLayer`를 각기 다른 부분집합으로 조합한다(예: `CanOpenDeckEdit`는 대기·처리 전 요청을 안 봄, `InterceptEntry`는 `Shop/Recovery.IsWaiting`을 안 봄, `OpenTraitGrowthWhenIdle`은 `IsAnyPopupOpen` 대신 팝업 셋을 재나열, `:185`는 `HasPendingRequest`를 두고 인라인 사본). `StageMapViewPopups`는 만들기·Tick·Dispose를 팝업마다 손으로 반복하고 `RestoreNoncombat`는 상점/회복 if 사슬이다. 조합 차이가 의도인지 코드로 알 수 없다.
- 실패 시나리오: 새 시설 팝업을 더하면 Popups 5곳, Scope 판정 4~5곳 + 복구 분기, SceneFactory, Services 생성자·속성, System 인자, ScreenRoot 필드·검증을 함께 고쳐야 한다. 한 판정에서 빠뜨리면 그 팝업이 열린 채 선택 화면이 다시 뜨거나 증강 팝업이 겹쳐 열린다(컴파일로 안 잡힘). 실제로 드러난 결함 하나는 C-17.
- 고칠 방향: 팝업 컨트롤러에 `IsOpen`·`IsWaiting`·`TryRestore`·`Tick`·`Dispose` 좁은 계약을 두고 Popups가 목록으로 돌며 `IsAnyOpen`·`IsAnyWaiting`(또는 `IsBusyExcept(…)`)을 내고, Scope는 공통 한가 조건 `IsMapIdle` 하나 + 판정별 차이만 적는다.
- 원본 ID: L2-1, S1-03, L1-1, L6-1

### C-02 노드 마커 점프 이동 연출(시간 비율·이동 소리·시작/완료 순서)이 두 클래스에 사본
- **최종: 중간** — 검증 유지
- 주된 기준: C1 / 관련 기준: C4
- 후보 심각도: 중간 (원본: S1-01 중간, S2-02 중간, L1-2 중간, L2-7 중간, L6-18 낮음)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `VS/StageMap/StageMapNodeMovement.cs:67-79(68-80, 72-75)`, `VS/StageMap/StageCombatDeparture.cs:81-91(86-89)`
- 내용: `BeginMarkerMove` → `TryGetNode` → `MapRoot` 로컬 좌표 → `ChessPieceMotionSettings(jump, d*12/59, d*35/59, d*12/59)` → `Begin(dest, dest)` → `IsMoving`이면 `UnitEvent.UnitMovement`/`UnitMovementPreset.Base` 재생, 완료 쪽(`TryConsumeCompletion` → `Dispose` → `CompleteMarkerMove`)까지 두 클래스에 같은 줄로 있다. 비율 12/59·35/59는 이름 없는 상수다.
- 실패 시나리오: 점프 리듬·소리·목적지 계산을 한쪽만 고치면 비전투 노드 이동과 전투 출전 사전 이동이 다르게 움직이거나 다른 소리를 낸다.
- 고칠 방향: 마커 이동 시작·완료를 한 함수/작은 도우미(예: `StageMarkerJump`, `ChessPieceMotionSettings.FromDuration`)로 모으고 비율을 이름 있는 상수로.
- 원본 ID: S1-01, S2-02, L1-2, L2-7, L6-18

### C-03 "전투 노드인가" 판정이 View(출전 가로채기)와 LifeCycle(명령 분기)에 다른 식으로 두 벌
- **최종: 낮음** (C1) — 낮춤(검증 둘 다). `IsStart`가 `StageKey == 0`이라 두 식이 지금 같은 값
- 주된 기준: C1 / 관련 기준: C4
- 후보 심각도: 중간 (원본: S1-02 중간, L1-3 중간, L2-5 중간)
- 근본 원인 묶음: B11
- (현강 UI): 아님
- 위치: `VS/StageMap/StageCombatDeparture.cs:67-77(67-70, 71-77)`, `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageProgress.cs:182-192`
- 내용: View는 `!node.IsStart && node.StageType == StageType.Combat`, LifeCycle은 `node.StageKey != 0 && node.StageType == StageType.Combat`. 시작 노드 표현도 다르다. 인접 판정(`NextNodeIds`)은 Core 이동 규칙의 부분 사본으로 View에만 있고, 같은 함수가 `GetStages`를 두 번 돈다(→ N-72).
- 실패 시나리오: 전투형 노드 종류 추가·시작 노드 정의 변경을 한쪽만 반영하면 (a) 덱 확인 뒤 명령이 Plan에서 `MapCommand`로 가 Core가 거절하거나 출전 수명(`CompleteAccepted`)과 Node 영수증이 엇갈리고, (b) 덱 확인·사전 이동 없이 바로 전투 진입이 걸린다. 지금 데이터에서 드러나는지는 불확실.
- 고칠 방향: 공용 판정(`StageNodeRules.IsCombat(node)` 또는 Buffer `StageProgressData.IsCombat`) 하나를 두 곳이 부른다.
- 원본 ID: S1-02, L1-3, L2-5

### C-04 증강 진행 캐시 열쇠(ProgressKey)가 Core 진행 읽기 원본 목록의 손 사본
- **최종: 중간** — 검증 유지
- 주된 기준: C1 / 관련 기준: C10
- 후보 심각도: 중간 (원본: S7-1 중간, L1-4 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageTraitGrowthProgress.cs:72-113(72-108)`(`ProgressKey.Read`), `:63-70`(Refresh)
- 내용: Core `ReadTraitGrowthProgress`가 읽는 원본(RunId·Outcome·HasBattle·Characters.Count·RosterRevision·ActiveOffer·HandledCommandNumber·AcknowledgedResultNumber) 8개를 열쇠로 복사해 같으면 Core를 다시 읽지 않는다. Core 원본 목록이 두 곳에 있다. 같은 열쇠를 `CreateGrowthOfferCheck`도 쓴다.
- 실패 시나리오: Core가 진행 판정에 새 원본을 더하면 열쇠는 그대로라 `IsPending`·`NeedsOffer`가 낡은 값 → 증강 팝업이 안 열리거나 노드 진입이 계속 막히고, 준비 뒤 원본 대조가 틀려 증강 도메인이 잘못 멈출 수 있다. 예외 없이 조용히 틀린다.
- 고칠 방향: Core가 진행 버전(열쇠)을 Buffer에 게시하고 View는 그 값 하나만 비교(Core 요청), 또는 읽기 원본 목록과 열쇠를 같은 곳에서 정의.
- 원본 ID: S7-1, L1-4

### C-05 Stage 도메인(진입·상점·회복) 정지가 F1이 아니라 LogError로 끝나 화면이 무기한 대기
- **최종: 중간** — 검증 유지
- 주된 기준: C10 / 관련 기준: C9
- 후보 심각도: 중간 (원본: S7-2 중간, L4-5 중간)
- 근본 원인 묶음: B10
- (현강 UI): 아님
- 위치: `StageLifeCycle.cs:215-216`(`ReportStoppedDomain` = `Debug.LogError`), `StageProgress.cs:152-163`, `Plans/StagePlan.cs:60-71, 83, 88, 90`(L4: `:320-328`), `Jobs/StageCheckJob.cs:33-35`; View 대기 `VS/SM/StageMapInteraction.cs:182`, `VS/SM/StageMapViewScope.cs:191`, `VS/SM/Shop/ShopPopupController.cs:69, 75`, `VS/SM/Recovery/RecoveryPopupController.cs:207`; 대조 `VS/SM/TraitGrowth/TraitGrowthPopupController.cs:95-96`
- 내용: 확인 Job이 처리 기록 누락을 찾으면 도메인 깃발만 세우고 `Debug.LogError`. View는 진입·상점·회복의 멈춤 사실을 읽지 않는다. 증강만 `IsGrowthStopped`를 읽어 F1로 올려 도메인마다 처리가 다르다. `Stop`은 `Halt`와 달리 `CancelPending`도 하지 않는다.
- 실패 시나리오: 맵 명령 처리 번호 누락 → Entry 멈춤 → 노드 요청이 Buffer에 남아 모든 노드 클릭이 무시되고 선택 화면이 안 뜸. 상점 멈춤이면 상점 `_pending`이 남아 버튼 잠김·떠나기 불가. 오류 화면 없음.
- 고칠 방향: 진입·상점·회복 정지도 F1로 올리거나, 도메인별 멈춤을 사실로 내 해당 화면이 오류 보고·닫기.
- 원본 ID: S7-2, L4-5

### C-06 덱 편집 안내 문구(Status)·병과 수(Categories)를 만들기만 하고 화면에 그리지 않음 — 거절·낡음 이유가 사라짐
- **최종: 중간** — 검증 유지
- 주된 기준: C10 / 관련 기준: C5, C12
- 후보 심각도: 중간 (원본: S4-1 중간, L6-2 중간, S4-8 낮음)
- 근본 원인 묶음: B2
- (현강 UI): 아님
- 위치: `VS/SM/Deck/DeckEditModelBuilder.cs:18, 36(주석), 46, 75-91`, `VS/SM/Deck/DeckEditDisplayModel.cs:77-80`, `VS/SM/Deck/DeckEditText.cs:8-67`(전체), 소비 `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs:195-220`
- 내용: `DeckEditText.Status`가 확정·거절 사유·"다시 평가해 주세요"·편집 불가 이유를 만들어 `model.Status`에 담고, `SortedDictionary`+`StringBuilder`로 `Categories` 글도 만들지만 `DeckEditUI.Render`는 `Deck`·`Owned`·`CanEdit`·`CanConfirm`만 읽는다(전체 grep에서 읽는 곳 없음). Builder 주석 "Core의 거절 사유는 Status에 남깁니다"가 이것에 기댄다.
- 실패 시나리오: 놓기 거절·`Stale`·`WrongPhase`(`Unavailable`)·`MissingMain` 등이면 확정·출전 버튼만 꺼지고 이유가 안 보여, 사용자는 "다시 평가"를 눌러야 함을 모른다. 편집마다 쓸모없는 할당.
- 고칠 방향: 화면에 상태 글 칸을 두어 그리거나, 안 그릴 거면 Status·Categories·DeckEditText를 걷고 주석을 고친다.
- 원본 ID: S4-1, L6-2, S4-8

### C-07 DeckEditUI가 View 표 조회를 `as`·`?.`로 받아 없으면 조용히 null로 그림
- **최종: 낮음** (C13) — 낮춤(검증 둘 다). 운영 조립이 늘 조회를 넘기고 표가 없으면 DB 생성에서 먼저 멈춤
- 주된 기준: C13 / 관련 기준: C10
- 후보 심각도: 중간 (원본: S5-1 중간, L6-3 중간, L3-3 낮음)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs:103-109, 233-236(234-236), 454-465`; 대조 `VS/SM/StageMapViewServices.cs:342`, `TrainingCardContentBuilder.cs:56-57`, `TraitGrowthPopupModelBuilder.cs:82-83`
- 내용: `GetUIData`가 `(query as IHasViewQuery)?.ViewQuery…`로 유닛·특성·병과 외형 표를 받고 실패하면 null. 그릴 때 이름 = 키 숫자, 초상·아이콘 = 없음. 같은 화면 특성 표는 `UnitCardTraitContent.Read`가 null이면 예외, 같은 유닛 외형 표를 맵 서비스 묶음은 필수로 받는다 — 같은 공급 경로에 정책이 둘.
- 실패 시나리오: 조립 순서·쿼리 형식이 바뀌면 덱 화면이 키 숫자·빈 그림으로 "정상" 표시되고 오류 경계에 아무것도 안 남는다(특성 상세를 열 때서야 다른 문구 예외). 운영 DB Factory가 늘 보장해 지금 드러날 가능성은 낮음(불확실).
- 고칠 방향: `IHasViewQuery`가 아니거나 표가 null이면 `InvalidOperationException("[DeckEditUI] UIId.DeckEdit: …")`. 행 없음만 정상 대체 표시.
- 원본 ID: S5-1, L6-3, L3-3

### C-08 거절된 놓기가 바뀌지 않은 현재 후보까지 Invalid로 만들어 확정·출전이 막힘
- **최종: 낮음** (C10) — 판정자 낮음(증거 낮음·반론 중간). Core가 그 교체를 거절하는 정상 경로를 못 찾음, "다시 평가"로 풀림
- 주된 기준: C10 / 관련 기준: —
- 후보 심각도: 중간 (원본: S4-2 중간)
- 근본 원인 묶음: B2
- (현강 UI): 아님
- 위치: `VS/SM/Deck/DeckEditDraft.cs:100-119, 152-165, 221-241`, `VS/SM/Deck/DeckEditPopupController.cs:99-103, 183-195`
- 내용: `CreatePlacementRequest`는 수락 전 현재 후보를 바꾸지 않는 가상 요청을 `_pendingRequest`로 두지만, 거절되면 `ApplyEvaluation → Apply`가 `State = Invalid`·`Reason = 거절 사유`로 바꾼다. `_candidates`는 직전 Valid 그대로라 상태가 버려진 제안을 가리킨다. `Committed`도 요청 때 먼저 지운다(:116).
- 실패 시나리오: 유효 덱에서 보유 카드를 놓았다 Core가 거절(`UnsupportedUnit` 등) → 화면은 그대로인데 `CanConfirm`·`CanDepart` 거짓 → `StartCombat`이 `CanDepart`로 바로 return(:188), 이유도 안 보임(C-06). "다시 평가"로만 풀림.
- 고칠 방향: 놓기 거절은 현재 후보 상태를 바꾸지 않고 "마지막 거절 사유"만 남기거나, 거절 뒤 현재 후보를 다시 평가.
- 원본 ID: S4-2

### C-09 View가 Core에 없는 "병과당 한 장" 칸 규칙을 강제하고 어기면 F1로 멈춤
- **최종: 중간** — 검증 유지
- 주된 기준: C10 / 관련 기준: C1
- 후보 심각도: 중간 (원본: S4-3 중간, 조건부·불확실)
- 근본 원인 묶음: B3
- (현강 UI): 아님
- 위치: `VS/SM/Deck/DeckEditDisplayModel.cs:10-30`(`DeckEditSlotLayout`), `VS/SM/Deck/DeckEditModelBuilder.cs:39-43`, `VS/SM/Deck/DeckEditDraft.cs:106-113`; 대조 Core `CoreCaculator/Application/Roster/DeckRules.cs:27-109`
- 내용: Core `DeckRules`는 개수·중복 ID·메인 1장·지원 유닛만 본다(병과 중복, `Boss`·`None` 병과를 막지 않음). View는 덱을 병과별 고정 네 칸으로만 그려, 같은 병과 둘이거나 칸 없는 병과면 `Build`·`CreatePlacementRequest`가 `InvalidOperationException` → Guard → F1. `DeckEditDraft` 머리 주석 "덱 규칙은 판단하지 않고"와도 어긋난다.
- 실패 시나리오: 로드아웃·로비 출발 등으로 Core가 수락한 덱에 같은 병과 서브가 둘이면, 맵에서 덱 편집을 여는 순간(`Show → Render`) 런타임 전체가 정지. 실제 에셋에서 발생하는지는 불확실.
- 고칠 방향: 기획 규칙이면 Core `DeckRules`에 넣어 원본을 하나로 하고 View는 Core 거절로 받기, 아니면 칸 구조를 Core 규칙과 맞추기.
- 원본 ID: S4-3

### C-10 출전 확정 뒤 닫기 연출이 끊기면 출전·취소 콜백이 모두 사라져 맵이 영구 잠김
- **최종: 중간** — 검증 유지
- 주된 기준: C9 / 관련 기준: C10
- 후보 심각도: 중간 (원본: S4-4 중간(불확실), L4-3 중간)
- 근본 원인 묶음: B4
- (현강 UI): 아님
- 위치: `VS/SM/Deck/DeckEditPopupController.cs:85-97, 209-230`, `VS/SM/StagePopupViewLifetime.cs:96-111, 117-138, 140-145`, `VS/SM/Deck/DeckEditPopupViewScreen.cs:10, 21`, `VS/SM/StageCombatDeparture.cs:95-118`, `VS/SM/StageMapViewScope.cs:191, 198-199, 236-248`
- 내용: `Depart`는 `_depart`·`_cancelDeparture`를 먼저 비우고 `CloseThen(false, depart)`로 두루마리 닫기 끝에 출전을 맡긴다. 수명 부품은 닫는 중 외부 Hide(`!_scope.IsOpen`)·부모 비활성(`!_scroll.isActiveAndEnabled`)·오류면 대기 닫기를 취소하고 `afterClose`를 버린다. `Hide`가 받는 `_cancelDeparture`는 이미 null이라 `StageCombatDeparture.Cancel`이 안 불린다.
- 실패 시나리오: 착지 → 출전 → 닫기 재생 중 Canvas 꺼짐/UISystem이 DeckEdit을 직접 닫음 → 출전 부품이 `_arrived=true, _submitted=0, _motion=null`로 남음 → `IsDepartureBusy` 계속 참 → 노드 클릭 삼킴, 선택 화면·덱 버튼 막힘, 오류 보고 없음. 발생 경로는 드묾(불확실).
- 고칠 방향: 닫기 후속이 버려지는 갈래에서 "닫힘 실패" 콜백을 부르게 하거나, `_cancelDeparture`는 후속이 실제 실행된 뒤에만 비운다.
- 원본 ID: S4-4, L4-3

### C-11 이전 연결이 남긴 상점 명령이 있으면 새 연결의 상점 팝업이 열리지 않음(넘겨받기 갈래 미도달)
- **최종: 낮음** (C5) — 판정자 낮음(증거 낮음·반론 버림), 기준 C10 → C5. 연결 교체 때 `Detach`가 상점 명령을 먼저 지워 실패 시나리오는 사실 아님. 닿지 않는 넘겨받기 갈래·낡은 주석만 남음
- 주된 기준: C10 / 관련 기준: C9
- 후보 심각도: 중간 (원본: S3-01 중간, 조건부)
- 근본 원인 묶음: B5
- (현강 UI): 아님
- 위치: `VS/SM/Shop/ShopPopupController.cs:96-97, 103, 128-129, 254-265`; 참고 `VS/SM/StageMapViewScope.cs:175`
- 내용: `Dispose`는 현재 연결일 때만 `_writer.Clear()`라 교체된 연결의 상점 명령이 Buffer에 남고, 지우는 곳은 이 컨트롤러뿐이다. 새 컨트롤러는 `ChapterKey = 0`·`_displayContext = null`로 시작하고, 문맥을 잡는 두 길(`TryRestore`·`Show`의 `TryCaptureContext`)은 `_input.Commands.Count == 0`을 요구한다. `Tick`의 넘겨받기는 `ChapterKey` 불일치·문맥 null이면 return하므로 문맥 없는 새 컨트롤러에서 성립하지 않는다.
- 실패 시나리오: 구매 요청 직후 StageMap 연결 교체 → 새 연결에서 상점 복원이 늘 false, 인수한 화면은 제출 막힌 옛 그림 → 씬을 떠날 때까지 상점을 다시 못 연다. 교체 빈도는 불확실.
- 고칠 방향: 넘겨받기가 명령의 방문 문맥으로 문맥을 잡게 하거나, 현재 방문 것이 아닌 남은 명령을 지우는 규칙을 한 곳에, 또는 요청 수명을 런타임 소유자로(C-12).
- 원본 ID: S3-01

### C-12 "제출 → 실행 → 결과 소비" 요청 수명이 상점·회복·증강에서 세 꼴로 구현되고 "확인 안 한 결과" 식이 여러 층에 있음
- **최종: 중간** (C1) — 판정자 중간(증거 중간·반론 낮음). 회복·증강 요청 수명 사본·"확인 안 한 결과" 식 두 벌. C-11 근거는 뺌
- 주된 기준: C1 / 관련 기준: C4
- 후보 심각도: 중간 (원본: S3-02 중간, L1-5 낮음)
- 근본 원인 묶음: B5
- (현강 UI): 아님
- 위치: 회복 `VS/Recovery/RecoveryRequestLifetime.cs` 전체; 증강 `VS/TraitGrowth/TraitGrowthRequestLifetime.cs`(전체, `:43-45, 136-141`) + `VS/SM/TraitGrowth/TraitGrowthPopupController.cs:64-66, 161-175`; 상점 `VS/SM/Shop/ShopPopupController.cs:39, 201-215, 248-355`; `StageTraitGrowthProgress.cs:32-40`
- 내용: 내 요청 번호·실행 예약·결과 대조·ack/제거·정지·종료를 세 곳이 따로 정한다. 회복·증강 소유자는 `TryBeginExecution`·`NotifyExecutionFinished`·`Shutdown`·`RequireAvailable`·`Fail`이 거의 같은 사본이고, 상점은 런타임 소유자 없이 씬 수명 컨트롤러가 Buffer에 직접 제출·제거(씬 이탈 = 취소), 증강은 번호는 런타임이 들고 ack는 화면이 Buffer에 직접 쓴다. "확인 안 한 결과"(`Handled > Acknowledged`)는 증강 소유자·컨트롤러에 두 벌이고, 컨트롤러 `IsPending`은 Core 상태(`AwaitingAcknowledgement`)와 Buffer 식을 OR로 겹쳐 쓴다.
- 실패 시나리오: 새 시설 팝업(용병 영입 등)이 따를 원본이 없고 정지·종료·교체 규칙을 세 곳에서 따로 맞춰야 한다. 확인 규칙 변경 시 팝업은 "남음", Plan은 "없음"으로 어긋날 수 있다. C-11·C-14가 이 차이에서 나왔다.
- 고칠 방향: 번호 보관·예약·종료·Shutdown 공통을 요청 수명 부품 하나로, 차이(취소 API, ack 주체)만 전략으로. "확인 안 한 결과" 식은 한 속성으로.
- 원본 ID: S3-02, L1-5

### C-13 회복 팝업이 열린 동안 매 Tick 회복 상태 전체를 Core에서 다시 투영(할당 포함, 결과 프레임엔 두 번)
- **최종: 중간** — 검증 유지
- 주된 기준: C11 / 관련 기준: C7, C5
- 후보 심각도: 중간 (원본: S3-03 중간, L5-7 낮음)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `VS/SM/Recovery/RecoveryPopupController.cs:110-135(118, 121, 123, 126, 128), 213-214`; 근거 `CoreSystem/RecoverySystem/RecoveryStateQueryService.cs:20-25`, `RecoveryStateProjection.cs:35-66`
- 내용: 표시 중이면 매 프레임 `TryReadDisplay` → `_state.TryGetCurrent`(호출마다 `Capture`·`Project`로 새 스냅숏·선택지 목록·영수증·예상 회복량 계산), 결과가 있으면 같은 Tick에 한 번 더 읽고 `IsDisplay`도 반복. 쓰는 값은 문맥 비교·완료 여부뿐. 모델 빌더 주석(`RecoveryPopupModelBuilder.cs:31` "열기와 요청 완료 때만")의 의도와도 어긋남.
- 실패 시나리오: 회복 팝업을 띄워 둔 채 머물면 프레임마다 GC 할당과 Core 투영 계산.
- 고칠 방향: 문맥·완료만 보는 가벼운 읽기(CurrentProgress의 Recovery 문맥 등)로 바꾸거나 결과 번호·Buffer 변경 때만 투영. 결과 프레임은 첫 읽기 값을 재사용.
- 원본 ID: S3-03, L5-7

### C-14 숨김 중 도착한 회복 결과의 영수증을 버리고, Scope는 좁은 "대기" 정의를 봄(상점과 규칙 반대)
- **최종: 낮음** (C10) — 낮춤(검증 둘 다). 대기 중 팝업을 밖에서 닫는 운영 경로를 못 찾음
- 주된 기준: C10 / 관련 기준: C1
- 후보 심각도: 중간 (원본: S3-04 중간(조건부), L1-6 낮음(불확실))
- 근본 원인 묶음: B5 (관련 B1)
- (현강 UI): 아님
- 위치: `VS/SM/Recovery/RecoveryPopupController.cs:61, 73, 102-108, 207, 216-221`; 대조 `VS/SM/Shop/ShopPopupController.cs:141, 254-265`; Scope `VS/SM/StageMapViewScope.cs:192, 210`
- 내용: `Hide`가 `ClearPendingDisplay()`로 영수증을 지워 요청은 소유자에서 실행·소비되지만 `HandleResult` 주체가 사라져 완료 뒤 `_mapChanged`·불일치 안내가 실행되지 않는다. 컨트롤러 안 열기·제출은 소유자 `HasPending`을, 밖에 내는 `IsWaiting`은 자기 영수증(`_pendingNumber > 0`)만 보고, Scope는 `IsWaiting`으로 선택 화면·증강 열기를 판단한다.
- 실패 시나리오: 회복 선택 직후 외부가 팝업을 닫음 → Core는 처리하지만 맵 갱신 없음, 결과 대기 중 증강 팝업이 열릴 수 있음. 재연결 시 이전 요청이 소유자에 남아 있으면 Scope는 한가하다고 보고 `TryRestore`는 `HasPending`으로 거절해 회복 팝업이 다시 안 뜸(불확실).
- 고칠 방향: 상점처럼 영수증은 폐기 때만 지우고 표시 토큰만 끊거나, Scope의 대기 판정을 소유자 `IRecoveryRequestStatus.HasPending`으로.
- 원본 ID: S3-04, L1-6

### C-15 불일치 닫기 뒤 상점·회복을 다시 열 길이 없고 선택 화면도 돌아오지 않아 방문이 막힘(전투 노드 클릭 시 F1)
- **최종: 중간** — 검증 유지(둘 다 중간). 줄 번호 정정: `ShopResultPolicy` 436-445 → 36-45
- 주된 기준: C10 / 관련 기준: C9
- 후보 심각도: 중간 (원본: L4-1 중간, S3-13 낮음(불확실))
- 근본 원인 묶음: B6
- (현강 UI): 아님
- 위치: `VS/SM/Shop/ShopPopupController.cs:104, 316-321, 357-367, 380-386`; `VS/SM/Recovery/RecoveryPopupController.cs:35, 76-78, 102-108, 118-124, 179-186`; `VS/SM/StageMapSelectionPresentation.cs:60-66, 76-88`; `VS/SM/StageMapScreenPresentation.cs:35-51`; `VS/SM/Shop/ShopResultPolicy.cs:436-445`
- 내용: 상점 Mismatch·이상 거절·재조정 닫기와 회복 StateMismatch·상태 변화 닫기는 `returnToSelection = false`. 상점은 `_skipNextRestore`로 다음 복원을 건너뛰고, 회복은 `_hiddenContext`로 같은 방문 재오픈을 영구 거부(`Show`에서만 지워짐). 팝업이 열릴 때 닫은 `StageSelect`를 되살리는 경로가 없고 노드는 미클리어로 남는다.
- 실패 시나리오: 훈련소 `StaleRank` → `CloseAndRefresh(false)` → 맵 다시 그림 → 복원이 skip으로 false, 이후 복원 호출 없음 → 다른 노드 클릭은 `CurrentStageUncleared`(로그만) → 전투 노드 클릭 시 `OpenForCombat`의 `OpenChildUI`가 "부모 UI가 열려 있지 않습니다" → Guard 없음 → Scope Tick → F1. 회복도 같은 꼴.
- 고칠 방향: "완료 닫기"와 "불일치 닫기"를 나눠 불일치는 억제 표지를 걸지 않고 복원·선택 화면 복귀를 하게 하고, `OpenForCombat`은 부모 열림을 조건에 넣거나 Guard로 감싼다.
- 원본 ID: L4-1, S3-13

### C-16 노드 진입 거절·출전 거절·미연결 클릭이 플레이어에게 보이지 않음(Debug.Log만)
- **최종: 낮음** (C10) — 낮춤(검증 둘 다). 오류가 묻히는 것이 아니라 정상 거절의 안내가 없음
- 주된 기준: C10 / 관련 기준: C1
- 후보 심각도: 중간 (원본: L4-2 중간)
- 근본 원인 묶음: B12
- (현강 UI): 아님
- 위치: `VS/SM/StageMapEntryObserver.cs:91-92`, `VS/SM/StageMapViewScope.cs:309`, `VS/SM/StageCombatDeparture.cs:65-66, 77, 105-109`, `VS/SM/Shop/ShopPopupController.cs:311, 320, 334`, `VS/SM/Recovery/RecoveryPopupController.cs:183`
- 내용: Core 거절 사유(`CurrentStageUncleared`·`NotConnected`·`AlreadyCleared`·`FinalNodeReached`)는 콘솔 로그만. 출전 흐름은 말을 옮기고 덱 창을 연 뒤 Core가 거절하면 `Cancel`로 말이 원위치로 순간 이동. 미연결 전투 노드는 조용히 무시, 미연결 비전투 노드는 거절 로그만 — 같은 "미연결" 클릭 결과가 노드 종류마다 다르다.
- 실패 시나리오: 현재 노드 미클리어 상태에서 다음 노드 클릭 → 화면 변화 없음. 출전 덱 확정(덱 변경은 적용됨) 후 Core 거절 → 말이 튀고 이유 표시 없음.
- 고칠 방향: 거절 사유를 문구 표·호버 패널로 짧게 안내, 출전 취소도 같은 안내, 미연결 클릭을 한 규칙으로.
- 원본 ID: L4-2

### C-17 일반 덱 열기 조건에 "처리 전 노드 요청"이 없어 클릭 직후 덱을 열면 착지 때 상점·회복 복원이 모달 입력 충돌로 F1
- **최종: 낮음** (C10) — 판정자 낮음(증거 중간·반론 낮음, 불확실). F1 경로는 성립하나 노드 클릭~수락 사이 짧은 창에 덱 버튼을 눌러야 함
- 주된 기준: C10 / 관련 기준: C1
- 후보 심각도: 중간 (원본: L4-4 중간)
- 근본 원인 묶음: B1
- (현강 UI): 아님
- 위치: `VS/SM/StageMapViewScope.cs:191, 198-199, 207-214, 288-307`, `VS/SM/Shop/ShopPopupController.cs:101-116`, `VS/SM/Recovery/RecoveryPopupController.cs:71-88`; 범위 밖 근거 `Common/PopupInputSession.cs:33-36`
- 내용: `CanOpenDeckEdit`에 `HasPendingRequest`·`IsSelectionLayer`가 없다(같은 파일 `CanShowSelection`·증강 열기에는 있음). 비전투 노드 요청 후 수락·이동 표시 전 1~수 프레임 동안 덱을 열 수 있고, `RestoreNoncombat`는 다른 팝업 열림·입력 Layer를 보지 않는다.
- 실패 시나리오: 상점 노드 클릭 → 덱 버튼 클릭 → 덱 열린 채 말 이동 → 착지 → `Shop.TryRestore` → `PopupInputSession.Begin` "이미 열린 모달 입력 수명" 예외 → catch → `_faults.Report` → F1. 설정창(`OverrideLayer`) 동안 열기 시도도 같은 꼴(timeScale 0이라 경로 좁음, 불확실).
- 고칠 방향: 덱 열기 조건에 `HasPendingRequest`·`IsSelectionLayer`를 더하고, 복원·출전 열기는 "다른 팝업 없음·선택 Layer"일 때만 시도하고 아니면 다음 Tick으로 미룬다.
- 원본 ID: L4-4

### C-18 맵 선택 입력 콜백 예외가 F1 오류 경계에 닿지 않음(로비와 달리 감싸지 않음)
- **최종: 중간** — 검증 유지(둘 다 중간). 줄 번호 정정: 슬롯 161-188 → 73-102
- 주된 기준: C10 / 관련 기준: C3
- 후보 심각도: 중간 (원본: L4-6 중간)
- 근본 원인 묶음: B7
- (현강 UI): 아님
- 위치: `VS/StageMap/StageMapViewInput.cs:38-44`, `VS/SM/StageMapInteraction.cs:99-120, 179-191`, `ViewSystem/StageSelectionInput/StageSelectionReceiverSlot.cs:161-188`
- 내용: 입력 콜백은 InputSystem 콜백(Tick 밖)에서 불리는데 수신자(`StageMapInteraction`/편집기 감싸개)를 오류 보고 감싸개로 감싸지 않았다. `InterceptEntry`·`BlocksEntryForGrowth`만 자체 try/Report가 있고 `Submit`(명령 `Validate`)·피드백·픽커·카메라 드래그 예외는 콘솔에만 남는다(로비는 `FaultReportingLobbyReceiver`로 감쌈).
- 실패 시나리오: `SelectStageNode` 검사 실패나 피드백 예외 → 콘솔 한 줄, 런타임 계속, 클릭 무반응, `EndSelection`만 된 중간 상태가 F1 없이 남음.
- 고칠 방향: `IStageSelectionInputReceiver` 오류 보고 감싸개를 슬롯 연결 직전에 둔다.
- 원본 ID: L4-6

### C-19 서비스 묶음 `StageMapViewServices`(인자 35~38개)를 부품마다 통째로 받음
- **최종: 중간** — 검증 유지
- 주된 기준: C2 / 관련 기준: C8, C4
- 후보 심각도: 중간 (원본: L2-3 중간, S1-07 낮음, L6-10 낮음)
- 근본 원인 묶음: B13
- (현강 UI): 아님
- 위치: `VS/StageMap/StageMapViewServices.cs:35-93, 95-138(35-137)`; 받는 곳 `StageMapViewScene.cs:20-26`, `StageMapViewEntry.cs:24-31`, `StageMapViewInput.cs:14-18`, `StageMapViewScope.cs:39-53`, `StageMapSceneFactory.cs:24-40`; 만드는 곳 `StageMapViewSystem.cs:71-110`
- 내용: 표시·진입·입력·팝업 네 갈래가 쓰는 서비스(DB 표·Buffer 읽기/쓰기·UI·입력·카메라·오디오·증강 칸 7·회복 3·상점 2)를 한 묶음에 모으고 각 부품이 전체에 의존. 예: Scene은 `Backgrounds`·`Colors`·`Cameras`·`MapData`·`FindVisual`만, Input은 `Slot`·`Layers`·`Requests`·`RequestWriter`·`Feedback`·`MapData`만 쓴다.
- 실패 시나리오: 팝업 하나의 의존을 바꾸면 Services 생성자·속성·System 인자를 고치고 무관한 Scene·Input·Entry까지 검토 대상, 시험 대역은 35칸을 채워야 함, 누가 무엇을 쓰는지 grep으로만 앎.
- 고칠 방향: 부품 생성자에 실제로 쓰는 칸만 넘기거나 쓰는 쪽별 작은 묶음으로 나누고, 큰 묶음은 System/Factory 안에서만 푼다.
- 원본 ID: L2-3, S1-07, L6-10

### C-20 Stage 도메인(상점·회복·증강)이 LifeCycle 다섯 파일에 하나씩 나열됨
- **최종: 낮음** (C4) — 낮춤(검증 둘 다). 도메인 넷 고정, 확장 때만
- 주된 기준: C4 / 관련 기준: C2
- 후보 심각도: 중간 (원본: L2-2 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `StageProgressFacts.cs:5-13, 35-50`, `StageProgress.cs:37-40, 55, 66-68, 93-101, 125-141, 152-161`, `Plans/StageStep.cs:3-20`, `Plans/StagePlan.cs:53-93, 111-191`
- 내용: 도메인 하나마다 멈춤 bool, `Reset`·`Stop` 스위치 줄, `IsStopped` 합, `HasUnregisteredRequest` 합, 사실 칸 1~2개, `StageStepKind` 값, `Decide` 줄, `Next` case가 따로 있고 목록 정의가 한 곳에 없다.
- 실패 시나리오: 새 도메인을 더할 때 `IsStopped`나 `HasUnregisteredRequest`에서 빠뜨리면 `StageDeckEditWindow.ReadReason`이 그 도메인 요청을 못 봐 처리 중에 덱 편집이 열린다.
- 고칠 방향: 도메인마다 "멈춤·미등록 요청·다음 Job"을 갖는 작은 단위 목록을 두고 Progress·Plan이 목록을 돈다(판단 순서만 Plan에).
- 원본 ID: L2-2

### C-21 상점 화면이 일반 상점과 훈련소(방문 종류)를 한 Builder·Controller에서 분기로 처리
- **최종: 낮음** (C2) — 낮춤(검증 둘 다). 셋째 방문 종류 가정뿐
- 주된 기준: C2 / 관련 기준: C4
- 후보 심각도: 중간 (원본: L2-4 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `VS/SM/Shop/ShopPopupModelBuilder.cs:24-42, 86-100, 115-119, 138-205`, `VS/SM/Shop/ShopPopupController.cs:37, 76, 150-163, 183-195, 279-293`
- 내용: 상품 종류는 `IShopOfferContentBuilder`로 열려 있지만 방문 종류는 `VisitKind == Training` 분기와 훈련 전용 상태(`_trainee`·`_traineeEntry`·`_unitCards`·`_traitCards`)가 Builder 안에 섞이고, 컨트롤러도 `TrySelectUnit`·`TryBack`·단계 입력 잠금·`MatchesResult`의 `PurchaseTraining` case로 훈련소를 안다.
- 실패 시나리오: 세 번째 방문 꼴(단계가 있는 상점)을 더하면 Builder 분기·상태·입력 처리·결과 대조를 모두 고치고 일반 상점 경로까지 회귀 위험.
- 고칠 방향: 방문 종류별 모델 단계(`IShopVisitModel`류)로 나눠 Builder는 고르기만, 카드 선택/뒤로가기는 단계 객체에 위임.
- 원본 ID: L2-4

### C-22 에디터 카메라 구도 도구가 런타임 클래스·입력 배선 여러 곳에 `#if UNITY_EDITOR` 갈래로 섞임
- **최종: 중간** — 검증 유지
- 주된 기준: C4 / 관련 기준: C2, C3, C6
- 후보 심각도: 중간 (원본: L2-9 중간, S2-03 낮음, L3-10 낮음, L6-26 낮음(불확실))
- 근본 원인 묶음: B8
- (현강 UI): 아님
- 위치: `VS/StageMap/StageMapCameraDrag.cs:31-33, 83-86, 126-136, 155-193`; `StageMapViewScene.cs:27-29, 57-60`; `StageMapViewInput.cs:25-28, 41-44, 126-129, 142-145, 336-355`; `StageMapViewScope.cs:119-122`; `VS/StageSelectionInput/StageSelectionInputController.cs:27-34, 37-74(63-73)`; `StageSelectionReceiverSlot.cs:34-36, 65-68, 103-114`; `IStageSelectionInputReceiver.cs:3-12`; `StageSelectionInputNames.cs:14-17, 179-183`; `VS/StageMap/Editor/StageMapSceneRootEditor.cs:1-10`, `StageMapCameraFramingPending.cs:1-14`, `StageMapCameraFramingReceiver.cs`, `StageMapCameraFramingSettings.cs`
- 내용: 운영 끌기 부품이 `_previewSettings`로 `SetBounds`(오프셋 건너뜀)·`SetOffset`(세션 값 재기록·`PreviewFraming` 재귀)을 가르고, `SetBounds`가 `EndDrag`를 불러 `_dragging` 저장·복원 우회가 필요하다. 씬·입력·Scope Tick·입력 컨트롤러·슬롯·수신 계약이 모두 에디터 갈래를 가진다. `ViewSystem/StageMap/Editor`는 Systems asmdef 아래라 Editor 폴더 제외가 안 돼 파일마다 `#if`로 막는다(가이드는 `Tool/Editor`). 에디터에서는 `Cancel`을 Raw로 받아 회전에도 쓰고 Performed 때만 취소, 빌드는 `Press(Cancel, …)` — 같은 액션 해석이 빌드 종류마다 다르다.
- 실패 시나리오: 런타임 끌기 규칙을 고칠 때 에디터 갈래를 함께 읽고 두 모드를 맞춰야 하고, 구도 도구를 고칠 때 런타임 9곳을 건드린다. `#if` 하나 누락 시 Player 빌드 실패, 에디터 확인(회전 + 취소)이 빌드 동작(취소만)을 대변하지 못한다.
- 고칠 방향: 구도 도구를 Editor asmdef/에디터 전용 부품으로 분리하고 런타임에는 "구도 기준값을 바꾸는 입구"(`SetFraming`) 하나만, Cancel은 같은 처리기로 받고 에디터 회전은 별도 액션/수신자 쪽에서.
- 원본 ID: L2-9, S2-03, L3-10, L6-26

### C-23 덱 편집 열기 사실을 `StageLifeCycle`이 직접 구현해 수명 클래스에 덱 규칙이 들고, 사실이 LifeCycle·Progress에 나뉨
- **최종: 낮음** (C4) — 낮춤(검증 둘 다). 불가 판단은 `ReadReason` 한 곳
- 주된 기준: C2 / 관련 기준: C4
- 후보 심각도: 중간 (원본: L2-12 중간, S7-6 낮음)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `StageLifeCycle.cs:26, 36-38, 72-73, 84, 196-211(199-211)`, `IStageDeckEditFacts.cs:7-35`, `StageProgress.cs:30-31, 54-71, 88-89`, `Plans/StagePlan.cs:103`, 맵 쪽 `VS/SM/StageMapViewScope.cs:198-199`
- 내용: `StageLifeCycle`이 `IStageEntryAttemptRead`·`IStageDeckEditFacts`·`IStageTraitGrowthFacts`를 명시 구현한다. `HasOpenInteraction`처럼 덱 전용 판정이 수명 클래스에 있고, 절반은 `_progress`에 위임·절반은 LifeCycle이 따로 받은 `_current`·`_stages`·`_roster`로 계산한다(같은 sink 두 벌 보관). "로스터가 있나"가 LifeCycle(`Count > 0`)과 Plan(`Count == 0`)에 따로. 열기 조건이 창·Scope·LifeCycle 세 곳.
- 실패 시나리오: "상점 안에서는 덱 편집 허용" 같은 규칙 변경에 진입·이탈·Job 등록을 맡은 LifeCycle을 고쳐야 하고, Bind 순서가 바뀌면 `_current` 가드와 `_progress` 바인딩이 어긋날 수 있다.
- 고칠 방향: 사실 계약 구현을 LifeCycle 밖 어댑터(또는 `StageProgress` 한 곳)로 옮기고 덱 판정은 `StageDeckEditWindow` 한 곳에.
- 원본 ID: L2-12, S7-6

### C-24 덱 편집 규칙(병과 칸 순서·필터·병과 일치·메인 고정)이 UI·초안·필터에 나뉨
- **최종: 낮음** (C1) — 판정자 낮음(증거 중간·반론 낮음), 기준 C4 → C1. 카드·초안 병과가 같은 `basis.Stats`에서 나와 국소 사본만 남음 (현강 UI 일부)
- 주된 기준: C4 / 관련 기준: C1
- 후보 심각도: 중간 (원본: L2-11 중간, L1-9 낮음, S5-7 낮음, S4-9 낮음(불확실))
- 근본 원인 묶음: B3
- (현강 UI): **예(부분 — `DeckOwnedFilter.cs`)**
- 위치: `VS/SM/Deck/DeckEditDisplayModel.cs:9-30(13-29), 40`(`DeckEditSlotLayout`); `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckOwnedFilter.cs:13, 15, 61-69` (현강 UI); `DeckEditUI.cs:33, 470-471, 494-504, 590-593, 622-625`; `VS/SM/Deck/DeckEditDraft.cs:100-104, 121-127, 130-142`; `VS/SM/Deck/DeckEditModelBuilder.cs:68-73`
- 내용: 슬롯 순서↔병과 원본은 `DeckEditSlotLayout`인데 필터는 1→Heavy·2→Rider·3→Cannon을 switch로 따로 적는다. "카드 병과 = 칸 병과" 판정은 UI 세 곳(카드 모델 `Category`)과 초안(`Basis.Stats` 조회, 없으면 `None`)이 다른 출처로 하며, 보유 ID→능력치 찾기도 `CategoryOf`·`FindStats` 두 벌. 메인 교체 금지는 `DeckEditUI.allowMainCharacterChange`(직렬화 bool)만 안다.
- 실패 시나리오: 병과 추가·순서 변경 시 필터가 빠지면 새 병과 유닛이 "All" 외 필터에 안 보임. 능력치가 빠진 메인 카드는 화면엔 Main 칸에 그려지는데 `RemoveSlot(0)`이 못 찾음. 메인 고정을 Run 상태로 바꾸려면 UI MonoBehaviour를 고쳐야 하고 다른 입력 경로엔 규칙이 안 걸림.
- 고칠 방향: 필터는 `DeckEditSlotLayout.CategoryAt(index)` 사용, 보유 ID→병과 함수 하나, 놓기 가능 판정(병과 일치·메인 고정)을 View 규칙 함수 하나로.
- 원본 ID: L2-11, L1-9, S5-7, S4-9

### C-25 DeckEditUI 한 클래스(659줄, 인터페이스 6개)가 표시·출전·드래그·교체 모드·상세창·보유 목록 배치·필터를 모두 가짐
- **최종: 낮음** (C2) — 판정자 낮음(증거 낮음·반론 중간). 바깥은 좁은 계약만 의존, 한 파일 안 유지 비용
- 주된 기준: C2 / 관련 기준: C4
- 후보 심각도: 중간 (원본: S5-2 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs:22-23, 131-151, 222-247, 367-373, 415-452, 470-520, 540-586, 588-643`
- 내용: `IUIInputReceiver`·`IDeckEditDisplay`·`IDeckCombatDisplay`·`IDeckEditDragReceiver`·`IDeckEditInteractionReceiver`·`IScrollPopupDisplay`를 한 객체가 구현하고 바뀌는 이유가 출전 버튼·드래그 고스트·보유 목록 수동 배치·상세창 정책·필터로 나뉜다. 열기 조건 조합도 여러 입구에 반복(N-40).
- 실패 시나리오: 보유 목록 레이아웃·드래그 고스트 변경도 출전·교체 모드 상태 필드가 섞인 파일을 읽고 각 입구의 Cancel 순서(CancelDrag/CancelChangeMode/CloseInfo)를 다시 맞춰야 한다.
- 고칠 방향: 보유 목록(생성·배치·필터)과 드래그 고스트를 화면 내부 부품으로 나누고 DeckEditUI는 상태 조합·입력 전달만.
- 원본 ID: S5-2

### C-26 덱 편집 화면 묶음의 필수 구성 검사가 씬 연결 검사 밖(Awake·첫 Open·첫 출전)에서 늦게 돌아 조용히 죽는 버튼이 생김
- **최종: 중간** — 검증 유지
- 주된 기준: C13 / 관련 기준: C10
- 후보 심각도: 중간 (원본: L3-1 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님(위치에 현강 UI `OwnedUnitSlot.cs` 포함, 원인은 `DeckEditUI`·`StageSelectPanelUI`)
- 위치: `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs:101, 131-144, 319-378`; `StageSelectPanelUI.cs:26-62`; `StageMapCanvasView.cs:54-56`; `DeckCombatConfirmUI.cs:26-39`; `OwnedUnitSlot.cs:116, 168-176`; `VS/StageMap/StageMapViewSystem.cs:261-275`
- 내용: 씬 연결 때 `ValidateRoot` → `StageMapCanvasView.ValidateComposition`이 상점·회복·증강 화면은 내부 참조까지 검사하지만, 덱 편집은 `StageSelectPanelUI.ValidateComposition`에서 부모·Canvas 관계만 본다. 내부 참조(Panel·버튼·덱 칸·보유 목록·필터·상세창) 검사는 `Awake → Prepare`, 출전 확인창 버튼은 `DeckCombatConfirmUI.Open` 첫 호출, 출전 버튼·확인창 위치는 `RenderDeparture(true)` 첫 호출에서 돈다.
- 실패 시나리오: `deckSlots` 하나가 비거나 `slotInfoPanel`이 Panel 밖이면 Awake 예외(콘솔만, F1 밖) → `_prepared` 거짓 → `openButton` 바인딩 안 됨 → 씬 연결은 성공하고 덱 열기 버튼은 무반응. 확인창 버튼 누락은 빈 칸 출전 확인 순간에야 드러남.
- 고칠 방향: `DeckEditUI`에 상태를 바꾸지 않는 `Validate()`(Prepare 검사부 + 확인창 버튼·위치 + 보유 슬롯 프리팹)를 두고 `StageSelectPanelUI.ValidateComposition`에서 부른다.
- 원본 ID: L3-1

### C-27 시작 노드에 건물 프리팹이 있으면 시작 노드가 클릭 대상이 되어 진입 요청을 기록함
- **최종: 낮음** (C10) — 판정자 낮음(증거 낮음·반론 중간). 갈 수 없는 노드는 원래 모두 제출되고 Core가 거절, 시작 노드는 새 피해 아님. 줄 번호 정정 `StageNodeView.cs:42-58, 100-105, 118-135`
- 주된 기준: C10 / 관련 기준: C12
- 후보 심각도: 중간 (원본: S2-01 중간, 조건부·결과 화면 불확실)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `VS/StageMap/StageNodeView.cs:229-245, 287-292`; `StageMapInteraction.cs:76-91, 174-186`; `StageNodeHoverInfo.cs:134-135`
- 내용: `StartVisualPrefab`은 선택 칸이고, 있으면 `Show`가 시작 노드에도 건물·`BoxCollider`를 만든다. `ApplyAppearance`가 시작 노드를 늘 "완료 아님"으로 봐 현재 위치가 시작이 아니면 클릭 상자를 켠다. 맵 입력은 시작 노드를 거르지 않아 `SelectStageNode(chapter, 시작 노드)`를 Buffer에 쓴다. 호버만 시작 노드를 거른다. `:230` 주석 "시작 노드는 누를 일이 없습니다"와 다르다.
- 실패 시나리오: 시작 외형을 넣은 맵에서 한 칸 나아간 뒤 시작 건물을 누르면 갈 수 없는 노드 명령이 기록되어 Core 거절 경로를 타고(표시는 C-16처럼 없음), 그 위에선 배경 끌기도 시작되지 않는다.
- 고칠 방향: 시작 노드는 클릭 상자를 만들지 않거나 `hit.enabled = buildingShown && !node.IsStart`로, "시작 노드는 집지 않는다"를 노드 뷰 한 곳에서.
- 원본 ID: S2-01

### C-28 View 입력 Buffer 꼴이 네 벌이고 한 칸짜리 둘(회복·증강)은 소비 확인 규칙이 서로 다름
- **최종: 낮음** (C1) — 판정자 낮음(증거 중간·반론 낮음). 계약 차이는 의도됨, 위험 몫은 C-12에서 셈
- 주된 기준: C1 / 관련 기준: C4
- 후보 심각도: 중간 (원본: S6-1 중간)
- 근본 원인 묶음: B5
- (현강 UI): 아님
- 위치: `RuntimeSystem/BufferSystems/RecoveryView/RecoveryViewBuffer.cs:46-111`, `RuntimeSystem/BufferSystems/TraitGrowthView/TraitGrowthViewBuffer.cs:53-88`, `RuntimeSystem/BufferSystems/StageView/StageViewBuffer.cs:55-94`, `RuntimeSystem/BufferSystems/ShopView/ShopViewBuffer.cs:34-62`
- 내용: 번호 발급(`_lastIssuedNumber`·`int.MaxValue`)·다음 요청 찾기·"번호까지 지우기"가 Stage·Shop에 거의 같은 사본. 회복은 호출자가 대조 뒤 `AcknowledgeResult(number)`, `Clear`는 입력만 지우고 확인 번호 유지, 넘침은 `int.MaxValue` 검사. 증강은 Buffer가 Run·결과 Buffer를 받아 스스로 대조·성공 시 입력 삭제, `Clear`는 확인 번호 0, 넘침은 `checked`, 정리 계약도 별도 internal store.
- 실패 시나리오: 새 팝업 입력이 따를 원본이 없고 번호 규칙(넘침·0·Clear 뒤 확인 번호) 하나를 고치려면 네 파일을 봐야 하며, 호출자는 Buffer마다 "확인 뒤 입력이 남는가"를 따로 알아야 한다.
- 고칠 방향: 번호 발급·대기열을 공용 부품(`ViewCommandQueue<T>`, `SingleViewCommandSlot<T>`)으로, 한 칸 Buffer 확인 규칙을 하나로.
- 원본 ID: S6-1

### C-29 상점 훈련 요청 등급 범위 1~2가 Core 등급 상한(3)의 사본이고 넘으면 예외
- **최종: 낮음** (C1) — 낮춤(검증 둘 다). Core 상한도 리터럴, 지금 후보로는 예외에 안 닿음
- 주된 기준: C1 / 관련 기준: C10
- 후보 심각도: 중간 (원본: S6-2 중간)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `RuntimeSystem/BufferSystems/ShopView/ShopViewCommand.cs:73` (원본 규칙 `CoreCaculator/Application/Shop/TrainingRules.cs:46`)
- 내용: `ShopViewCommandData.Validate`가 `ExpectedRank < 1 || > 2`면 `ArgumentOutOfRangeException`. Core는 같은 경우를 실패 사유 `RankLimitReached`로 돌려준다.
- 실패 시나리오: 최대 등급을 4로 올리면 Core는 등급 3 훈련을 허용하는데 View `Submit`이 예외 → F1. 상한을 낮추면 Buffer 검사 의미가 어긋난다.
- 고칠 방향: Buffer는 "1 이상"만 보고 상한은 Core 사유에 맡기거나 Core 공개 상수 하나에서 읽는다.
- 원본 ID: S6-2

### C-30 `IStageDeckEditFacts` 계약 주석·멤버 이름이 구현과 다르고, 사라진 "Flow"를 가리키는 설명이 여러 곳
- **최종: 낮음** (C12) — 낮춤(검증 둘 다). 동작 영향 없음
- 주된 기준: C12 / 관련 기준: —
- 후보 심각도: 중간 (원본: L6-4 중간, S7-12 낮음, L6-5 낮음)
- 근본 원인 묶음: —
- (현강 UI): 아님
- 위치: `IStageDeckEditFacts.cs:4, 18-19, 18-31, 27-28, 30-31`; `StageLifeCycle.cs:203-207(206)`; `StageProgress.cs:54-68(61, 66-68)`; `Jobs/ShopCommandJob.cs:32`; `Jobs/StageNodeEntryJob.cs:32`; `VS/SM/StageMapEntryObserver.cs:51`; `StageDeckEditWindow.cs:45`
- 내용: `HasPendingFlowCheck`는 "맵·상점·회복 요청의 처리 확인을 기다리는지"라 적혔지만 구현은 전투 진입 시도 하나(`_attempts.HasAttempt`)만 본다. `IsFlowStopped`·`HasUnregisteredRequest`는 "맵·상점·회복"만 적었지만 증강도 포함한다. "Flow가 입력 수명과 처리 번호를 확인", "다음 Flow Tick 전" 등 옛 Stage Flow 기준 설명·이름이 남았다(지금은 `StagePlan`·`StageProgress`·`StageCheckJob`).
- 실패 시나리오: 덱 편집 불가 사유를 고치는 사람이 상점·회복 처리 대기도 막힌다고 오해하거나, 증강 정지 때 왜 Stopped인지 못 찾고, 없는 Flow 클래스를 찾는다.
- 고칠 방향: 주석·멤버 이름을 구현(전투 진입 시도만, 증강 포함)과 지금 주체(Plan·확인 Job)에 맞춘다(`HasCombatEntryAttempt` 등).
- 원본 ID: L6-4, S7-12, L6-5

---

## 문제 — 후보 N (최종 낮음 86·버림 2)

### N-01 진입 관찰자가 순수 Buffer 읽기 뒤마다 `_canWork()` 재확인(스스로 정한 B7 규칙과 어긋남)
- **최종: 낮음** — 검증 유지
- C5 (관련 C7) / 원본 낮음×3 / — / 아님
- 위치: `VS/StageMap/StageMapEntryObserver.cs:38-90(40, 43, 56, 60, 86, 88, 90, 105), 148-200`; 규칙 `StageMapViewScope.cs:17-20`, `StageMapViewEntry.cs:64, 66, 99`
- 내용·실패: 매 Tick 대리자 호출 3~10회, 어느 확인이 필요한지 구분 불가. 고칠 방향: 맨 앞 한 번 + 외부 콜백 뒤만.
- 원본 ID: S1-04, L5-1, L6-8

### N-02 "처리 전 요청 있음"·"번호 N 입력이 남았나"를 여러 표현·도우미로 물음
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음×4 / B11 / 아님
- 위치: `StageCombatDeparture.cs:141`, `StageMapInteraction.cs:182`, `Shop/ShopPopupController.cs:103, 128`(`Commands.Count`) 대 `StageMapViewScope.cs:68, 185`, `StageMapEntryObserver.cs:42, 105`(`TryGetNextCommand(0)`); `Plans/StagePlan.cs:160, 193-198`(HasMapInput) 대 `StageEntryAttempts.cs:73-78(273-278)`(HasInput); 상점 `StagePlan.cs:173-174` 대 `StageProgress.cs:177-179`
- 내용·실패: Buffer가 처리 끝난 명령을 남기게 바뀌면 표현끼리 어긋남. 고칠 방향: Buffer 계약에 `HasPending`/`Contains(number)` 하나.
- 원본 ID: S1-05, S2-10, L1-11, S7-5

### N-03 씬·입력·진입 부품이 내부 부품을 공개하고 Entry↔Departure를 사후 setter로 주입
- **최종: 낮음** — 검증 유지
- C8 (관련 C2) / 원본 낮음×2 / B13 / 아님
- 위치: `StageMapViewScene.cs:32-35`, `StageMapViewInput.cs:20`, `StageMapViewEntry.cs:22, 48, 78`, `StageMapViewScope.cs:99, 101-104, 256, 270, 275`
- 내용·실패: Departure 없는 동안 `PresentAccepted`가 조용히 일반 이동으로 처리, 정리 순서 바꿀 때 Scope·Entry·Input 함께 수정. 고칠 방향: Scene 의도 메서드, Departure는 생성/Start 인자로 필수.
- 원본 ID: S1-06, L6-9

### N-04 선택 화면 부모가 한 번 숨으면 그 연결 동안 되살아나지 않음
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음(불확실) / — / 아님
- 위치: `VS/StageMap/StageMapSelectionPresentation.cs:40-56, 120-126`, `StageMapViewScope.cs:80`
- 내용·실패: `OnRootHidden`이 `_lifetime`을 null로, `CanOpenScreens` 고정 거짓 → 팝업·선택 화면 모두 멈춤, 보고 없음. 그런 닫기 경로 존재 여부 미확인. 고칠 방향: 오류 보고 또는 재열기.
- 원본 ID: S1-08

### N-05 Attach의 `previous?.Supersede()`가 이미 폐기된 Scope에만 닿음
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음(불확실) / — / 아님
- 위치: `StageMapViewSystem.cs:180-189`, `StageMapViewScope.cs:144-149`
- 내용·실패: 동작 문제 없음, 교체 경로 실재 여부 판단 곤란. 고칠 방향: 걷거나 조건 주석.
- 원본 ID: S1-09

### N-06 맵·상점 입력 Buffer 비우기를 두 주인이 다른 조건 표현으로 함
- **최종: 낮음** — 검증 유지
- C1 (관련 C4, C5) / 원본 낮음×3 / B1 / 아님
- 위치: `StageMapViewScope.cs:175`, `StageMapViewSystem.cs:161-162, 248-249`, `Shop/ShopPopupController.cs:96-97`
- 내용·실패: `IsCurrent` 대 `ReferenceEquals(_lastPublishedScope, owner)`; 한쪽만 바꾸면 다음 연결 입력을 지울 수 있고, 새 팝업 Buffer를 System에 더할지 판단 필요. 고칠 방향: 정리 주인 하나.
- 원본 ID: S1-10, L1-12, L2-14

### N-07 늘 넘기는 인자를 선택(null 기본값)으로 둠
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음×5 / — / 아님
- 위치: `StageMapInteraction.cs:28-46(35-36), 64, 96, 162, 174-186`; `StageMapViewInput.cs:32-33, 133-140`; `StageMapViewScope.cs:86`; `Shop/ShopPopupController.cs:52`; `Recovery/RecoveryPopupController.cs:45`; `Shop/ItemShopOfferContentBuilder.cs:15-18, 24`; `Recovery/RecoveryRequestLifetime.cs:151, 199`; `StageNodeVisualData.cs:15-16`; 그 밖 S1-11 재검사(`StageCombatDeparture.cs:41, 90, 169`, `StageMapViewSystem.cs:212-219`)
- 내용·실패: 연결을 빠뜨려도 컴파일·실행되어 끌기·UI 가림·증강 막기·선택 복귀가 조용히 꺼짐. `blocksEntry`·`interceptEntry` 훅 둘이 같은 모양(→ N-78). 고칠 방향: 필수 인자 + `ArgumentNullException`.
- 원본 ID: S1-11, S2-09, L6-13, S3-09, S6-11

### N-08 팝업·판정 수·증강·blocksEntry에 관한 낡은 주석
- **최종: 낮음** — 검증 유지
- C12 / 원본 낮음×2 / — / 아님
- 위치: `StagePopupViewLifetime.cs:12`, `StageMapViewScope.cs:15, 72-75, 105`, `StageMapViewEntry.cs:9-12`, `StageSelectionInputLayers.cs:14`, `StageMapInteraction.cs:32`, `StageMapViewInput.cs:31`
- 내용·실패: 증강·출전 추가 뒤 미갱신, `blocksEntry`는 화면을 띄우지 않음. 고칠 방향: 지금 구성으로.
- 원본 ID: S1-12, L6-7

### N-09 마커 해제가 편집 모드를 고려하지 않음
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / 아님
- 위치: `VS/StageMap/StageCurrentMarkerView.cs:35-38` 대 `:86-93`
- 내용·실패: `Detach`가 늘 `Object.Destroy` → 편집 모드에서 오류·객체 남음. 고칠 방향: 파괴 도우미 하나.
- 원본 ID: S1-13

### N-10 View가 회복 상태를 Core 서비스에서 직접 읽음(Buffer 경유 아님)
- **최종: 낮음** — 검증 유지(불확실). 회복 상태는 Buffer 읽기 계약이라 경계 위반 아님, 덱 Core 서비스 직접 호출 몫만 남음
- C3 / 원본 낮음(불확실) / — / 아님
- 위치: `StageMapViewSystem.cs:90, 93`, `StageMapViewScope.cs:296`
- 내용·실패: `CoreGroup.RecoveryState`·`CoreGroup.Deck`을 묶음에 넣음, 상점은 Buffer로 읽어 경계가 섞임. 고칠 방향: Buffer로 내거나 허용 근거를 계약 주석에.
- 원본 ID: S1-14

### N-11 추가 화면 계약을 `as` 캐스트로 찾고 출전 부품은 구체 컨트롤러에 의존
- **최종: 낮음** — 검증 유지
- C2 (관련 C8, C13) / 원본 낮음×4 / — / 아님
- 위치: `StageOfferPopupViewScreen.cs:21`, `StagePopupViewLifetime.cs:52`, `Deck/DeckEditPopupController.cs:16, 25-29, 39, 50-70, 61-62, 81, 96, 180, 183-230`, `Deck/DeckEditPopupViewScreen.cs:21`, `StageCombatDeparture.cs:30, 46, 117, 159`
- 내용·실패: 두루마리 계약 없으면 닫기 연출만 조용히 사라짐, 출전 계약 없으면 첫 출전에서야 예외. 고칠 방향: 필수 계약을 생성 인자로 받고, 출전은 좁은 계약에 의존.
- 원본 ID: S1-15, S4-12, L6-17, L2-13

### N-12 두루마리 닫기 대체 경로가 복귀·후속 동작을 버림
- **최종: 없음** (-) — 버림. 정상 닫기는 같은 호출 안에서 `Closed`를 내 대체 경로에 안 닿음
- C10 / 원본 낮음(불확실) / B4 / 아님
- 위치: `VS/StageMap/StagePopupViewLifetime.cs:104-110`
- 내용·실패: `IsVisible`·`IsAnimating`이 `Closed` 알림보다 먼저 거짓이 되면 정상 닫기인데 `ReturnToSelection`·`AfterClose`가 빠짐. 고칠 방향: 대체 경로도 요청 값 사용.
- 원본 ID: S1-16

### N-13 에디터 구도 상태가 잘못되면 Scope Tick 예외로 F1, 맵 평면 판정 두 벌
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음(에디터 전용) / B8 / 아님
- 위치: `StageMapCameraDrag.cs:170-184`, `StageMapViewScope.cs:121`, `Editor/StageMapCameraFramingSettings.cs:34-43, 78-80`
- 내용·실패: `Unlock`이 검증 없이 세션에 씀 → 다음 Tick 정지. 고칠 방향: 검증·대체, 평면 함수 하나, 경고로 낮춤.
- 원본 ID: S2-04

### N-14 재진입 없는 표 조회 뒤 세대 검사 반복
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageMapViewDisplay.cs:84-88, 104-110, 126-137`, `StageNodeView.cs:227-228`, `Background/StageMapBackgroundDisplay.cs:41-46`
- 내용·실패: 어느 검사가 재진입 대비인지 불명, 기계적 복제. 고칠 방향: Instantiate·SetActive 뒤만.
- 원본 ID: S2-05

### N-15 DB·Buffer가 보장한 조건을 표시·런타임에서 다른 문구로 재검사
- **최종: 낮음** — 검증 유지
- C5 (관련 C1) / 원본 낮음×4 / — / 아님
- 위치: `StageLinkView.cs:21-22, 44-46, 256-257, 279-281`; `Background/StageMapBackgroundDisplay.cs:47-51, 68-72`; `StageMapViewDisplay.cs:256-262, 319-328`; `StageNodeView.cs:218-219`; `StageMapViewData.cs:57-58, 85-100` 대 `StageMapNodeLayout.cs:15-17`; `StageMapCameraDrag.cs:69, 177, 183` 대 `Editor/StageMapCameraFramingSettings.cs:80`; `StageMapBackgroundTable.cs:26`; `Recovery/RecoveryPopupModelBuilder.cs:83-84`
- 내용·실패: 규칙 변경 시 두 곳 수정, 문구의 "0.01" 리터럴 사본. 고칠 방향: DB 검사·Buffer 등록 한 곳만.
- 원본 ID: S2-06, L1-13, S6-16, L6-14(StageLinkView·BackgroundTable 부분)

### N-16 `DisplayIdentity`가 `StageMapViewData` 표시 칸을 하나씩 사본·비교
- **최종: 낮음** — 검증 유지
- C4 / 원본 낮음×2 / — / 아님
- 위치: `StageMapViewDisplay.cs:140-143, 216-290`
- 내용·실패: 칸 추가 시 세 곳 수정, 빠뜨리면 다시 그리기 무시(지금은 설정이 안 바뀌어 드묾). 고칠 방향: Data 참조 비교 또는 늘 교체.
- 원본 ID: S2-07, L2-8

### N-17 쓰이지 않는 View 멤버·갈래(Show 오버로드·NodeMovement 명시 구현·닿지 않는 null·DeckEditUI 공개 멤버·버리는 호출)
- **최종: 낮음** — 검증 유지
- C5 (관련 C12, C8) / 원본 낮음×4 / — / 아님
- 위치: `StageMapViewDisplay.cs:55-67`, `StageMapNodeMovement.cs:13, 41, 45-46, 59`, `DeckEditUI.cs:32-47, 98, 476`
- 내용·실패: 오버로드 문서 "null은 외형 미지정"이 실제 예외와 다름, 툴팁 "Open 시 적용"이 실제와 다름, 죽은 경로를 실제로 오해. 고칠 방향: 걷거나 private, 문서 수정.
- 원본 ID: S2-08, L6-11, S5-9, S5-16

### N-18 깊이 축·맵 평면 규칙이 다섯 곳에 암묵적으로 있음
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / — / 아님
- 위치: `StageMapNodeLayout.cs:42-46`, `StageMapViewScene.cs:81`, `StageLinkView.cs:285-286`, `Editor/StageMapCameraFramingSettings.cs:78-80`, `StageMapCameraDrag.cs:67, 182`
- 내용·실패: 레이아웃 방향·노드 높이 변경 시 카메라 범위·연결선·구도 검사가 조용히 어긋남. 고칠 방향: 레이아웃이 축·평면을 내줌.
- 원본 ID: S2-11

### N-19 에디터 구도 미리보기가 매 프레임 SessionState 읽기·키 문자열 할당
- **최종: 낮음** — 검증 유지
- C11 / 원본 낮음×2(에디터 전용) / B8 / 아님
- 위치: `StageMapViewScope.cs:119-122`, `StageMapViewScene.cs:59`, `StageMapCameraDrag.cs:157-192`, `Editor/StageMapCameraFramingSettings.cs:22-29, 85-88`, `Editor/StageMapSceneRootEditor.cs:109, 148-156`
- 내용·실패: 프레임당 문자열 1~15개, Inspector는 값이 같아도 재기록. 고칠 방향: 키 한 번 생성, 버전 값만 비교.
- 원본 ID: S2-12, L5-5

### N-20 Game 뷰 포커스를 내부 타입 이름 문자열로 판정
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음(에디터 전용) / B8 / 아님
- 위치: `Editor/StageMapCameraFramingReceiver.cs:31-32`
- 내용·실패: 이름 바뀌거나 Simulator 창이면 입력이 늘 막힘. 고칠 방향: 공개 API.
- 원본 ID: S2-13

### N-21 확정 구도 반영이 다른 씬을 추가로 열고 그대로 둠
- **최종: 낮음** — 검증 유지
- C9 / 원본 낮음(에디터 전용) / B8 / 아님
- 위치: `Editor/StageMapCameraFramingPending.cs:97-108`
- 내용·실패: 모르는 사이 두 씬 저장, 실패해도 열린 씬 남음. 고칠 방향: 실패 시 닫기·대상 씬 열려 있을 때만.
- 원본 ID: S2-14

### N-22 `_skipNextRestore`가 방문과 묶이지 않은 1회성 깃발
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음(불확실) / B6 / 아님
- 위치: `Shop/ShopPopupController.cs:43, 103-104, 380-386`
- 내용·실패: 앞선 조건에서 소비되지 않고 남으면 다른 상점 노드의 첫 자동 복원이 건너뛰어짐. 고칠 방향: 건너뛸 방문 `EntryId` 기억.
- 원본 ID: S3-05

### N-23 외형 행 누락 정책이 팝업마다 다름(조용한 대체 vs 예외)
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음 / — / 아님
- 위치: 대체 `Shop/ShopPopupViewFactory.cs:72-77`, `Shop/UnitShopOfferContentBuilder.cs:33-34`, `Shop/EnhancementShopOfferContentBuilder.cs:27-28`, `Shop/ItemShopOfferContentBuilder.cs:24`, `Shop/TrainingCardContentBuilder.cs:32`, `TraitGrowth/TraitGrowthPopupModelBuilder.cs:37-40, 94`; 예외 `Recovery/RecoveryPopupModelBuilder.cs:81-90`, `Shop/TrainingCardContentBuilder.cs:56-57`, `TraitGrowth/TraitGrowthPopupModelBuilder.cs:82-83`
- 내용·실패: 유닛 외형 등록 누락 시 키 숫자 이름 노출, 오류 없음. 고칠 방향: 조회 도우미 한 곳에서 정책, 대체면 경고.
- 원본 ID: S3-06

### N-24 유닛 이름 대체 식 `_findName(key) ?? key.ToString()` 네 곳
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / — / 아님
- 위치: `Shop/UnitShopOfferContentBuilder.cs:34`, `Shop/EnhancementShopOfferContentBuilder.cs:27`, `Shop/TrainingCardContentBuilder.cs:32`, `TraitGrowth/TraitGrowthPopupModelBuilder.cs:94`
- 내용·실패: 대체 규칙 변경 시 네 곳. 고칠 방향: 팩토리 `FindName`이 대체까지.
- 원본 ID: S3-07

### N-25 `[StageMap]` Debug.Log 보고 도우미 세 벌, 비정상 경로도 Log 수준
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음×2 / B12 / 아님
- 위치: `Recovery/RecoveryPopupViewFactory.cs:48`, `Shop/ShopPopupViewFactory.cs:78`, `StageMapViewScope.cs:309`; 사용 `Shop/ShopPopupController.cs:311, 320, 329, 334`, `Recovery/RecoveryPopupController.cs:122, 183`
- 내용·실패: 수준·머리말 변경 누락, "구매는 처리됐지만 완료 미처리" 등이 콘솔 필터에 묻힘. 고칠 방향: 도우미 하나, 비정상은 Warning.
- 원본 ID: S3-08, L6-24

### N-26 팝업 컨트롤러의 `IsDisplay(token)`/`CanWork` 재검사가 동기 호출마다 반복
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `Recovery/RecoveryPopupController.cs:116-128, 157-161, 174-195, 201-203`; `Shop/ShopPopupController.cs:130-135, 338-354(349), 371-376`
- 내용·실패: 어느 호출이 표시를 바꾸는지 안 드러남, `:349`는 Tick에서 이미 거른 조건. 고칠 방향: 외부 콜백 뒤만.
- 원본 ID: S3-10

### N-27 ShopResultPolicy: 정상 거절 사유 여섯이 두 switch에 사본, 명시 갈래·default 중복, `IsUnexpected` 주석이 동작과 다름
- **최종: 낮음** — 검증 유지
- C1 (관련 C12) / 원본 낮음×2 / — / 아님
- 위치: `Shop/ShopResultPolicy.cs:29-35, 36-45, 50-68, 71`; 사용 `Shop/ShopPopupController.cs:317-318`
- 내용·실패: 사유 추가 시 분류만 고치면 문구가 "상태가 바뀌었습니다", 주석은 "로그"인데 실제는 F1. 고칠 방향: 사유 → (분류, 문구) 표 하나, 주석 수정.
- 원본 ID: L1-10, S3-11

### N-28 `using` 정리(자기 상위 네임스페이스·순서)
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / 아님
- 위치: `Recovery/RecoveryPopupController.cs:1`, `Shop/ShopPopupController.cs:1, 13`, `Shop/ShopPopupModelBuilder.cs:1`
- 원본 ID: S3-12

### N-29 덱 칸 수 4가 세 곳(View 칸 배치·화면 계약·Core 최대 장수)
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B3 / 아님
- 위치: `Deck/DeckEditDisplayModel.cs:12`, `GlobalSystem/UISystem/Screens/Stage/IDeckCombatDeparture.cs:7`, Core `DeckRules.cs:12`, 사용 `Deck/DeckEditPopupController.cs:193`
- 내용·실패: 하나 놓치면 빈 칸 경고 오류·진입 예외. 고칠 방향: `DeckEditSlotLayout.Count`만 쓰고 `IDeckCombatDisplay.DeckSlotCount` 걷기.
- 원본 ID: S4-5

### N-30 맵 선택 입력 Controller가 전투 `StageSelectInput`의 누름·뗌 상태 기계를 복사, 이름도 혼동
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음×2 / — / 아님
- 위치: `VS/StageSelectionInput/StageSelectionInputController.cs:76-112`, `VS/StageInput/StageSelectInput.cs:38-78`
- 내용·실패: Select 상호작용 변경 시 두 해석기 수정, 이름 때문에 엉뚱한 파일 수정. 고칠 방향: 공용 해석기, 전투 쪽 이름 변경.
- 원본 ID: S4-6, L6-19

### N-31 쓰이지 않는 Confirm 액션·슬롯 ExportServices·루트 서비스 키
- **최종: 낮음** — 검증 유지
- C5 (관련 C3) / 원본 낮음×2 / — / 아님
- 위치: `StageSelectionInput/IStageSelectionInputReceiver.cs:23`, `StageSelectionInputController.cs:26`, `StageSelectionReceiverSlot.cs:10, 99, 117-123`, `StageMap/StageMapInteraction.cs:127-130`, `ServiceKeys/Runtime/StageSelectionInputKey.cs:6-14`
- 내용·실패: 등록 경로가 있다고 오해, 남은 키가 루트 직접 조회·재등록 우회를 부름. 고칠 방향: 걷거나 `[Obsolete]`.
- 원본 ID: S4-7, L3-8

### N-32 보유 목록에서 사라진 후보는 덱 칸에 안 보이고 지울 수 없으며, 주석은 None 카드가 온다고 함
- **최종: 낮음** — 검증 유지
- C10 (관련 C12) / 원본 낮음×2 / B2 / 아님
- 위치: `Deck/DeckEditModelBuilder.cs:34-37`, `Deck/DeckEditDraft.cs:82-97, 130-142`, `Deck/DeckEditDisplayModel.cs:37-38`
- 내용·실패: Core가 계속 `UnknownOwnedId`로 거절, 팝업 재오픈 전까지 고칠 방법 없음. 고칠 방향: "알 수 없는 카드" 칸 표시 또는 명시 알림, 주석 정정.
- 원본 ID: S4-10, S4-16

### N-33 출전 Run 기억 `_departureRun`이 Basis 없이 열리면 이전 값이 남음
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음(불확실) / — / 아님
- 위치: `Deck/DeckEditPopupController.cs:29, 77, 149-158`
- 내용·실패: 첫 읽기 실패 뒤 정상 Rebase에서 Run이 같아도 팝업이 닫히고 출전 취소, 또는 Run 바뀜을 놓침. 고칠 방향: 첫 정상 Rebase에서 기록, 닫을 때 지움.
- 원본 ID: S4-11

### N-34 덱 편집 표시 모델이 GlobalSystem UI ↔ StageMap View 양방향 의존
- **최종: 낮음** — 검증 유지
- C4 (관련 C3) / 원본 낮음×2 / — / 부분(위치에 `DeckEditSlotView`·`OwnedUnitSlot`·`DeckSlotInfoStats` 포함)
- 위치: `Deck/DeckEditDisplayModel.cs:33, 70`, `IDeckEditDisplay.cs:2, 17`, `DeckEditUI.cs:9-10, 198`, `DeckEditSlotView.cs:3`, `OwnedUnitSlot.cs:3`, `DeckSlotInfoUI.cs:5`, `DeckSlotInfoStats.cs:2`, `StageOfferScreenUI.cs:4`, `StageOfferLayoutView.cs:4`; 반대 `Deck/DeckEditPopupController.cs:4`, `StagePopupViewLifetime.cs:4-6`, `StageMapSceneRoot.cs:2`
- 내용·실패: 모델 칸 변경 시 공용 UI 폴더도 수정, 어셈블리 분리 시 순환. 고칠 방향: 표시 모델·칸 배치를 UI 계약 쪽으로.
- 원본 ID: S4-13, L3-7

### N-35 에디터 카메라 회전·달리 누름 상태가 수신자 해제 때 정리되지 않음
- **최종: 낮음** — 검증 유지
- C9 / 원본 낮음(에디터 전용) / B8 / 아님
- 위치: `StageSelectionInput/StageSelectionInputController.cs:37-73`, `StageSelectionReceiverSlot.cs:52-72`
- 내용·실패: 회전 중 View 교체 시 이전 수신자는 끝을 못 받고 새 수신자는 끝만 받음. 고칠 방향: Unbind에서 셋 모두 해제 알림.
- 원본 ID: S4-14

### N-36 `Depart` 실패 갈래에서 Render 두 번
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `Deck/DeckEditPopupController.cs:214-228`
- 원본 ID: S4-15

### N-37 덱 편집 열기 버튼만 `Action` 대리자로 연결
- **최종: 낮음** — 검증 유지
- C13 / 원본 낮음×2 / — / 아님
- 위치: `DeckEditUI.cs:77, 116, 276-279`, `IDeckEditDisplay.cs:12-13`, `Deck/DeckEditPopupController.cs:46, 122, 128-132`
- 내용·실패: AGENTS.md 대리자 연결 금지, 입력 경로 둘로 갈려 끊기 관리 분리, null이면 조용히 무시. 고칠 방향: 명시 수신 계약(`IDeckEditOpenReceiver`).
- 원본 ID: S5-3, L3-2

### N-38 열림 상태·알림·"알림 중 재열림" 보호 패턴이 DeckEditUI와 StageOfferScreenUI에 두 벌, 예외 처리도 다름
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B9 / 아님
- 위치: `DeckEditUI.cs:153-193`, `StageOfferScreenUI.cs:55-82, 122-134`
- 내용·실패: DeckEditUI는 첫 구독자 예외에서 나머지 건너뜀 → 팝업 수명이 닫힘을 못 받음. 고칠 방향: 열림 상태·알림 부품 하나.
- 원본 ID: S5-4

### N-39 Hide 재정의(base 미호출)와 OnUIHidden으로 정리가 나뉘어 루트 비활성 경로에서 열림 상태·알림이 안 맞음
- **최종: 낮음** — 검증 유지
- C13 / 원본 낮음×2(불확실) / B9 / 아님
- 위치: `DeckEditUI.cs:99, 153-193(172-193), 645-651`, `DeckCombatConfirmUI.cs:46-53`, `StageOfferScreenUI.cs:38, 55-82, 122-134`, `IStageOfferDisplay.cs:10`, `IDeckEditDisplay.cs:10`
- 내용·실패: UISystem Hide 없이 부모가 꺼지면 `IsOpen == true`로 남고 Scope가 닫힘을 모름, `DeckCombatConfirmUI`는 이전 선택 복원이 Hide에만. 지금 운영 결함은 미확인. 고칠 방향: 정리를 `OnUIOpened/OnUIHidden` 한 함수로.
- 원본 ID: S5-5, L3-4

### N-40 상호작용 가능 조건이 `CanInteract`와 입구마다 다시 적힘
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / — / 아님
- 위치: `DeckEditUI.cs:281-283, 467, 590-591, 620-624`
- 원본 ID: S5-6

### N-41 병과 아이콘 조회가 두 경로
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B3 / 아님
- 위치: `DeckEditUI.cs:234-236, 463-465`
- 원본 ID: S5-8

### N-42 UI 준비·구성 검사 뒤 같은 검사를 다시 하거나 늘 거짓인 조건을 검사
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음×3 / — / 아님(위치에 현강 UI `OwnedUnitSlot.cs`·`DeckEditSlotView.cs` 포함)
- 위치: `DeckEditUI.cs:188, 334, 342, 359-365, 398-404, 647`, `DeckEditSlotView.cs:149`, `DeckSlotInfoUI.cs:398, 429`, `OwnedUnitSlot.cs:13, 158, 291`, `StageOfferCardView.cs:48, 76, 82`, `StageScreenRootUI.cs:285`, `StageSelectPanelUI.cs:348`, `StageMapCanvasView.cs:34, 40, 62`, `Jobs/StageJob.cs:35-36`
- 내용·실패: 읽기 비용, 어떤 칸이 선택인지 판단 곤란. 고칠 방향: 소유자 Prepare 한 곳.
- 원본 ID: S5-10, S5-11, L6-14(UI·StageJob 부분)

### N-43 제안 레이아웃 그리기 한 번에 카드 활성 갱신 세 번
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageOfferScreenUI.cs:91-97`, `StageOfferLayoutView.cs:285-286, 343, 352, 375-382`, `StageOfferCardView.cs:176, 179-190`
- 원본 ID: S5-12

### N-44 제안 레이아웃 Prepare가 필수 참조 누락을 조용히 건너뜀
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음 / — / 아님
- 위치: `StageOfferLayoutView.cs:217-229`, `StageOfferScreenUI.cs:50-53, 55-66`
- 내용·실패: 구성 검사를 안 거치는 경로면 Render에서 원인 없는 NRE(운영은 검사가 막음). 고칠 방향: Open에서 Validate 또는 예외.
- 원본 ID: S5-13

### N-45 덱 편집·제안·출전 확인 화면이 Exclusive라 다른 팝업을 Scope 밖에서 닫을 수 있음(TraitGrowthUI와 기준 다름)
- **최종: 낮음** — 검증 유지
- C13 / 원본 낮음(불확실) / — / 아님
- 위치: `DeckEditUI.cs`, `StageOfferUI.cs`, `DeckCombatConfirmUI.cs`(Policy 미지정); 대조 `TraitGrowthUI.cs:428-435`
- 원본 ID: S5-14

### N-46 DeckSlotInfoUI `changeButton`이 필수인데 쓰이지 않고 주석은 옛 Phase를 가리킴
- **최종: 낮음** — 검증 유지
- C5 (관련 C12) / 원본 낮음×2 / — / 아님
- 위치: `DeckSlotInfoUI.cs:16-17, 33-34, 81-83`
- 원본 ID: S5-15, L6-20

### N-47 덱 상세창 표시 부품이 전투 정보창에 공유되고 오류 머리말이 `[UnitInfo]`
- **최종: 낮음** — 검증 유지
- C4 / 원본 낮음 / — / **예(현강 UI)**
- 위치: `DeckSlotInfoStats.cs:14-76, 79-91`; 사용처 `Screens/Global/CombatCardInfoUI.cs:16, 89`
- 원본 ID: S5-17

### N-48 네 번째 이후 특성 복제 슬롯 배치 순서가 뒤집히거나 겹쳐 가려짐
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음(불확실) / — / **예(현강 UI)**
- 위치: `DeckSlotInfoAbilities.cs:87-98`
- 원본 ID: S5-18

### N-49 StageUI: 코드상 여는 곳이 없고 서비스 연결 검사·필드 이름·문구·Open/Hide가 규약과 다름
- **최종: 낮음** — 검증 유지
- C5 (관련 C13, C6) / 원본 낮음×3(불확실) / — / 아님
- 위치: `StageUI.cs:11-38`
- 원본 ID: S5-19, L3-9, L6-25

### N-50 StageOfferCardView 툴팁(선택)·Validate(필수)·null 갈래가 서로 다르고 예외 문구 영어
- **최종: 낮음** — 검증 유지
- C12 (관련 C13) / 원본 낮음×2 / — / 아님
- 위치: `StageOfferCardView.cs:17-19, 38-44, 48-53, 80-83`
- 내용·실패: 툴팁대로 비우면 씬 연결 구성 검사에서 맵 진입이 멈춤. 고칠 방향: 필수/선택 하나로 확정.
- 원본 ID: S5-20, L3-5

### N-51 StageLegendUI의 ID가 그룹 화면 ID와 같음
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / 아님
- 위치: `StageLegendUI.cs:16`, `StageSelectPanelUI.cs:21`
- 원본 ID: S5-21

### N-52 노드 건물 프리팹 필수 조건이 DB 검사에 없고 표시 시점에만 검사
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음 / — / 아님
- 위치: `DataBaseSystem/Data/View/Table/StageNode/Item/StageNodeVisualData.cs:35-44`, `StageNodeVisualDataSO.cs:24-25`; `VS/SM/StageNodeView.cs:44-45, 177-179`
- 원본 ID: S6-3

### N-53 노드 외형 `DisplayName`은 아무도 읽지 않는 이름 사본
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageNodeVisualData.cs:23-25, 33`, `StageNodeVisualDataSO.cs:28-29`
- 원본 ID: S6-4

### N-54 쓰이지 않는 DB 조회 멤버와 Buffer `Commands` 전체 노출
- **최종: 낮음** — 검증 유지. 단 "Buffer `Commands`가 쓰이지 않음" 몫은 버림(다섯 곳에서 씀)
- C5 (관련 C8) / 원본 낮음×2 / — / 아님
- 위치: `StageNodeVisualTable.cs:13-17, 25, 32, 49-52, 55-68, 86`, `RecoveryOptionVisualTable.cs:12, 14, 39-50`, `IStageMapBackgroundTableQuery.cs:12-13`, `StageViewBuffer.cs:14, 48`, `ShopViewBuffer.cs:11, 27`
- 원본 ID: S6-5, L6-12(키 부분은 N-31)

### N-55 스테이지·상점 요청이 같은 검사를 세 번 거치고 문맥 검사는 버리는 생성자 호출
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageViewCommand.cs:22-27, 30-34, 48`, `StageViewBuffer.cs:57`, `ShopViewCommand.cs:40, 52-54, 57-60, 62-67, 85`, `ShopViewBuffer.cs:36`
- 원본 ID: S6-6

### N-56 TraitGrowthViewBuffer만 의존·입력 검사가 빠져 형제 Buffer 관례와 다름
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음×2 / — / 아님
- 위치: `TraitGrowthViewBuffer.cs:11-20, 15-19, 48-52, 64-68, 72`
- 원본 ID: S6-7, L6-23

### N-57 증강 소비 확인 불변식·결과 대조가 Buffer와 트랜잭션 두 곳
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B5 / 아님
- 위치: `TraitGrowthViewBuffer.cs:72-74, 78`, `BufferSystems/Core/TraitGrowthTransactions.cs:127-128, 133-136`
- 원본 ID: S6-8

### N-58 화면 글 필수 칸·줄 종류 누락이 부트가 아니라 쓰는 순간에 드러남
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음 / — / 아님
- 위치: `StageOfferTextData.cs:72-75, 81, 83-92, 135-151, 192-196`, `StageOfferTextDataSO.cs:40, 64`
- 원본 ID: S6-9

### N-59 "조회마다 새로 만드는 값" 주석이 실제(표가 공유)와 다름
- **최종: 낮음** — 검증 유지
- C12 / 원본 낮음 / — / 아님
- 위치: `RecoveryOptionVisualData.cs:7`, `StageMapViewData.cs:8`, `StageNodeVisualData.cs:12`
- 원본 ID: S6-10

### N-60 `TryGetNodeVisual`이 Try 계약인데 예외를 던짐
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / 아님
- 위치: `StageNodeVisualTable.cs:70-81`
- 원본 ID: S6-12

### N-61 화면 글 줄 종류 확장이 열거·값 수 switch·툴팁 세 곳, 모든 팝업 글이 공유 열거 하나에
- **최종: 낮음** — 검증 유지
- C4 (관련 C2) / 원본 낮음×2 / — / 아님
- 위치: `StageOfferLineKind.cs:4-22`, `StageOfferErrorKind.cs`, `StageOfferTextData.cs:107-133`(`ValueCountOf`), `StageOfferTextDataSO.cs:21-24`, `StageOfferTextTable.cs`
- 원본 ID: S6-13, L2-15

### N-62 View DB Data 검사가 GlobalSystem 렌더 키에 의존
- **최종: 없음** (-) — 버림. 런타임 ColorSystem과 같은 원본 키를 읽는 올바른 의존
- C2 / 원본 낮음(불확실) / — / 아님
- 위치: `StageMapViewData.cs:3, 92`
- 원본 ID: S6-14

### N-63 화면 글 SO 하나가 세 용도(팝업 화면·증강·호버 이름)를 납작한 칸으로 겸함
- **최종: 낮음** — 검증 유지
- C2 / 원본 낮음 / — / 아님
- 위치: `StageOfferTextDataSO.cs:36-69`, `StageOfferTextData.cs:25-75, 135-165`, `StageOfferTextTable.cs:11-27, 83-107`
- 원본 ID: S6-15

### N-64 Broken으로 멈춘 진입 시도가 남아 씬 이탈 검사를 예외로 만듦
- **최종: 낮음** — 검증 유지
- C9 / 원본 낮음(불확실) / B10 / 아님
- 위치: `StagePlan.cs:68-69, 128-130`, `StageEntryAttempts.cs:55-59`, `StageLifeCycle.cs:140-165, 178-184`
- 원본 ID: S7-3

### N-65 진입 요청 상태를 이름 있는 질문 대신 `Status` 값으로 직접 판정
- **최종: 낮음** — 검증 유지
- C8 / 원본 낮음 / — / 아님
- 위치: `StageEntryAttempts.cs:54-61`
- 원본 ID: S7-4

### N-66 StagePlan.Next가 Job 목록 생성 중에 진행 상태를 바꾸고 다른 Plan 등록을 재진입으로 일으킴
- **최종: 낮음** — 검증 유지
- C3 / 원본 낮음×2 / — / 아님
- 위치: `Plans/StagePlan.cs:124-133`, `StageProgress.cs:144-148`, 호출 `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs:65`, `StageLifeCycle.cs:109`
- 원본 ID: S7-7, L3-6

### N-67 `IStageProgress`의 변경 입구가 공개 LifeCycle 그룹에 노출
- **최종: 낮음** — 검증 유지
- C8 / 원본 낮음 / — / 아님
- 위치: `IStageProgress.cs:12-34`, `StageLifeCycle.cs:58`
- 원본 ID: S7-8

### N-68 LifeCycle 쪽 늘 거짓인 인자·이미 보장된 null 재검사
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageLifeCycle.cs:96-100, 201-207, 230-231`, `Jobs/StageJob.cs:35-36`, `Plans/StagePlan.cs:114-115, 158`
- 원본 ID: S7-9

### N-69 가변 struct Job과 class Job이 섞이고 한 파일에 형식 둘
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음×2 / — / 아님
- 위치: `Jobs/StageJob.cs:13, 53-74`, `Jobs/ShopCommandJob.cs:8`, `Jobs/StageNodeEntryJob.cs:8`
- 원본 ID: S7-10, L6-16

### N-70 StageJob 기본 챕터 1 하드코딩·넓은 의존
- **최종: 낮음** — 검증 유지
- C2 / 원본 낮음 / — / 아님
- 위치: `Jobs/StageJob.cs:15, 39, 42`
- 원본 ID: S7-11

### N-71 노드 속성(시작·클리어)을 원본 속성 대신 여러 식으로 읽음
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B11 / 아님
- 위치: `StageMapViewDisplay.cs:316`, `StageProgress.cs:188`, `StageNodeView.cs:100`, `Shop/ShopPopupController.cs:73, 228`, `StageLifeCycle.cs:209-211`
- 원본 ID: L1-7

### N-72 노드 ID로 노드 찾기·연결 확인 루프가 여러 곳, 출전 가로채기는 같은 목록을 두 번 받아 끝까지 훑음
- **최종: 낮음** — 검증 유지
- C1 (관련 C11) / 원본 낮음×2 / B11 / 아님
- 위치: `StageNodeHoverInfo.cs:127-142`, `StageCombatDeparture.cs:66-77`, `StageProgress.cs:184-191`, `Shop/ShopPopupController.cs:224-230`
- 원본 ID: L1-8, L5-6

### N-73 "맵 준비됨" 조건이 View와 LifeCycle에 따로
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / B11 / 아님
- 위치: `StageMapViewReader.cs:34-40`, `StageProgress.cs:71`
- 원본 ID: L1-14

### N-74 "스테이지 키 우선, 없으면 종류·역할 공통" 조회 규칙이 두 표에 따로, 불일치 검사도 한쪽만
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / — / 아님
- 위치: `StageNodeVisualTable.cs:70-84`, `StageOfferTextTable.cs:68-76`
- 원본 ID: L1-15

### N-75 "현재 선택이 이 방문 문맥인가" 비교가 상점·회복에 따로
- **최종: 낮음** — 검증 유지
- C1 / 원본 낮음 / — / 아님
- 위치: `Shop/ShopPopupController.cs:217-246`, `Recovery/RecoveryPopupController.cs:209-211`
- 원본 ID: L1-16

### N-76 진입 관찰자가 `StageEntryIdentity`가 이미 비교한 번호를 다시 비교
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음 / — / 아님
- 위치: `StageMapEntryObserver.cs:44-45` (근거 `StageProgress.cs:146-147`)
- 원본 ID: L1-우연1

### N-77 Scope `:218` 주석("팝업이 없으면 거짓, Core가 거절")이 실제 경로와 다름
- **최종: 낮음** — 검증 유지
- C12 / 원본 낮음(불확실) / — / 아님
- 위치: `StageMapViewScope.cs:218` (대조 `:97`)
- 원본 ID: L1-우연2

### N-78 노드 진입 정책이 입력·Scope·출전 부품 세 갈래로 나뉨
- **최종: 낮음** — 검증 유지
- C4 / 원본 낮음 / — / 아님
- 위치: `StageMapInteraction.cs:33-46, 179-191`, `StageMapViewScope.cs:220-248`, `StageMapViewInput.cs:24-36`
- 원본 ID: L2-6

### N-79 카메라 이동 범위 규칙(시작·Boss 노드)이 카메라가 아닌 씬 부품에
- **최종: 낮음** — 검증 유지
- C2 / 원본 낮음 / — / 아님
- 위치: `StageMapViewScene.cs:68-82`, `StageMapCameraDrag.cs:63-87`
- 원본 ID: L2-10

### N-80 출전 수락 뒤 입력 제거를 "취소"로 처리해 덱 닫기·마커 재배치가 돈다
- **최종: 낮음** — 검증 유지
- C9 / 원본 낮음 / — / 아님
- 위치: `StageCombatDeparture.cs:105-109, 149-161`, `StageMapEntryObserver.cs:79-80`
- 원본 ID: L4-7

### N-81 상점 입력 의도 일부(`SelectCard`·`Exit`)가 try 밖
- **최종: 낮음** — 검증 유지
- C10 / 원본 낮음 / B7 / 아님
- 위치: `Shop/ShopPopupController.cs:150-155, 157-169`
- 원본 ID: L4-8

### N-82 이동 중 같은 프레임에 현재 노드를 두 번 읽고 맞춤, 평소에도 매 프레임 폴링
- **최종: 낮음** — 검증 유지
- C7 / 원본 낮음 / — / 아님
- 위치: `StageMapViewEntry.cs:100-108`, `StageMapViewScope.cs:253-257`, `StageMapViewReader.cs:53-73`, `StageCombatDeparture.cs:99-100`
- 원본 ID: L5-2

### N-83 증강 남음 판정이 한 프레임에 여러 번 원본 열쇠를 다시 만듦
- **최종: 낮음** — 검증 유지
- C7 / 원본 낮음 / — / 아님(관련 C-04)
- 위치: `TraitGrowth/TraitGrowthPopupController.cs:64, 81, 88`, `StageMapViewScope.cs:207-214`, `StageTraitGrowthProgress.cs:32-40, 63-70, 97-102`, `StageProgress.cs:140-141`, `StageLifeCycle.cs:192-196`
- 원본 ID: L5-3

### N-84 포인터 이동마다 UI 전체 RaycastAll을 노드 집기보다 먼저 함
- **최종: 낮음** — 검증 유지
- C7 / 원본 낮음 / — / 아님
- 위치: `StageMapInteraction.cs:59-74, 174-177`, `StageMapPointerBlocker.cs:12-16`, `StageNodeView.cs:118-135`
- 원본 ID: L5-4

### N-85 배경음이 서비스 묶음에 "없다"는 주석이 코드와 반대
- **최종: 낮음** — 검증 유지
- C12 / 원본 낮음 / — / 아님
- 위치: `StageMapViewServices.cs:31, 52, 128`, `StageMapViewSystem.cs:38-39, 110-111`
- 원본 ID: L6-6

### N-86 예외 문구가 영어이거나 소유자 머리말이 없음
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / 아님
- 위치: L6-15 원본 목록 그대로(`StageMapViewSystem.cs:59, 171, 175, 264`; `StageMapViewData.cs:60, 71, 80, 104`; `RecoveryOptionVisualData.cs:26, 28`; `StageNodeVisualData.cs:43`; `RecoveryOptionVisualTable.cs:30, 35, 44`; `StageMapViewTable.cs:27, 29, 36`; `StageNodeVisualTable.cs:36`; `StageOfferTextTable.cs:46, 61, 105`; `StageLinkView.cs:22, 27, 32`; `StageNodeView.cs:32`; `StageOfferCardView.cs:42`; `StageUI.cs:31-32`; `StageJob.cs:36, 44, 68`; `StagePlan.cs:45`; `StageLifeCycle.cs:231`; 머리말 없음 `RecoveryViewBuffer.cs:54, 58`, `TraitGrowthViewBuffer.cs:58, 72`, `StageSelectionReceiverSlot.cs:47`, `ShopPopupModelBuilder.cs:62, 66, 70, 129`, `ShopPopupController.cs:318`, `RecoveryRequestLifetime.cs:51, 229`, `TraitGrowthRequestLifetime.cs:63, 147`)
- 원본 ID: L6-15

### N-87 덱 슬롯 표시 글·병과 이름(열거 이름)·강조색을 코드에 박음
- **최종: 낮음** — 검증 유지
- C6 / 원본 낮음 / — / **예(현강 UI)**
- 위치: `DeckEditSlotView.cs:72-75, 120-121`, `DeckSlotInfoStats.cs:48`
- 원본 ID: L6-21

### N-88 에디터 구도 도구에 진단용 Dolly 상태 기록이 남음
- **최종: 낮음** — 검증 유지
- C5 / 원본 낮음(에디터 전용) / B8 / 아님
- 위치: `Editor/StageMapCameraFramingReceiver.cs:78, 123-126, 185, 190`, `Editor/StageMapCameraFramingSettings.cs:29-32, 42`, `Editor/StageMapSceneRootEditor.cs:27`
- 원본 ID: L6-22

---

## 기반 한계·범위 밖

범위 밖 파일**만** 가리키는 원본은 없었다(모든 원본에 범위 안 위치가 있음). 아래는 원본 일부가 기반에 기대거나 고칠 방향이 범위 밖을 요구하는 것으로, 감점에서 그 몫을 빼고 봐야 한다.

| 관련 항목 | 기반·범위 밖 몫 | 범위 안에 남는 몫 |
|---|---|---|
| N-82 (L5-2) | Buffer에 변경 알림이 없어 현재 노드 폴링 자체는 기반 한계일 수 있음(원본도 불확실) | 같은 프레임 두 번 읽기·첫 선택 전 반복 순회 |
| C-04 (S7-1·L1-4), N-83 | 권장 해법(진행 버전 게시·Core 캐시)은 Core 요청이 필요 | View의 원본 목록 손 사본 |
| C-09 (S4-3), N-29 | Core `DeckRules` 내용 자체는 범위 밖(대조 기준) | View의 고정 칸 구조·예외 |
| C-29 (S6-2) | Core `TrainingRules` 상한은 원본(범위 밖) | Buffer의 사본 상한·예외 |
| C-17 (L4-4) | `PopupInputSession`의 "이미 열린 모달" 예외는 범위 밖 공용 부품의 계약 | 범위 안 열기 조건 누락 |
| N-65 (S7-4) | Running·Faulted 질문 추가는 `CombatEntryRequest`(전투 골격, 범위 밖) 몫 | 있는 `HasExited`를 안 쓴 것 |
| N-66 (S7-7·L3-6) | 전투 진입 요청이 "Runner가 비어 있어야" 하는 조건은 범위 밖 `CombatEntryService` | Plan이 등록 순간 상태를 바꾸는 꼴 |
| N-84 (L5-4) | `ScreenUIPointerBlocker`(ViewSystem/Common)의 RaycastAll 구현은 범위 밖 | 호출 순서(가림 검사를 집기보다 먼저) |
| N-45 (S5-14) | `UISystem.CloseUnrelatedHistory`의 Exclusive 동작은 기반 | 범위 안 화면의 Policy 선택 불일치 |
| N-47 (S5-17) | 공유 사용처 `Screens/Global/CombatCardInfoUI`는 범위 밖 | 범위 안 부품 위치·이름·머리말 |
| N-62 (S6-14) | DataBaseSystem → RuntimeSystem 참조가 기반 구조상 허용된 의존일 수 있음(불확실) | — |
| C-18 (L4-6) | 비교 근거 `FaultReportingLobbyReceiver`는 로비(범위 밖) | 맵 입력 수신자를 감싸지 않은 것 |
| C-15 (L4-1) | `UISystem.OpenChildUI`의 "부모 UI 미열림" 예외는 기반 계약 | 범위 안 복원 억제·복귀 누락·Guard 없음 |

원본이 "문제 없음"으로 확인한 범위(감점 대상 아님): 서비스 등록·조회 경로(L3·S3·S2), InGame DB 미참조(L3), UI 입력 바인딩·핀 규약(L3·S2), 운영 핫패스 할당(L5 총평), 상품 종류 확장·배경·배치 계산 독립성(L2).
