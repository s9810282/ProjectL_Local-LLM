# 전투 View 평가·점검 — 12기준 100점 (2026-11-04)

> 사용자 요청: "현재 전투 View 평가 및 점검 진행, 1-100 스케일. SSOT · SOLID · JMH Framework 지침·FW 계약 · 기능 변경의 독립성 · 불필요한 반복 작업·일일이 검사 · C#·유니티 권장 구조 · 불필요한 매 프레임 반복, 이외에 추천할 평가 항목이 있으면 포함".
> 기준 판 `e70ad3d6`. **코드 변경 없음.** 정적 검토이고 컴파일·Unity 실행·Profiler 측정은 하지 않았다.
> 검증을 통과한 문제 301개 전체(위치·내용·실패 시나리오·고칠 방향)는 [문제 목록](2026-11-04-combat-view-evaluation-issues.md)에 있다.

## 0. 결과

| # | 기준 | 점수 | 중간 | 낮음 | 깎인 까닭(중간) |
|---|---|---|---|---|---|
| C1 | SSOT | **22.5** | 9 | 51 | 같은 판정·식이 두 곳 이상: 환경 '보여 줄지' 식 6곳, 도약 끊김 규칙, 표시 캐릭터 사본, 강조 층 속성이 층 표 밖, HP 막대 규칙, 약한 지반 소리 번호, 곡사 수명, 특성 근거 표, 토네이도 도착 칸 |
| C2 | SOLID 5원칙 | **60.0** | 4 | 29 | 조정자·씬 표시 God 클래스, 필드 기믹 공통 계약 없음, 클립 이벤트 종류 사슬 |
| C3 | JMH Framework 지침·FW 계약 | **90.0** | 0 | 5 | — (조립·데이터 경계 계약은 지킴) |
| C4 | 기능 변경의 독립성 | **60.0** | 4 | 19 | 두 번째 조립 지점(사건 Presenter), 도약 배치 대여의 숨은 결합, 큐 창구 인자 사슬, 포인터 대상 판정식 |
| C5 | 불필요한 반복 작업·일일이 검사 | **75.0** | 2 | 53 | 꺼 둔 화면 전환 경로, 늘 넘기는 협력자를 선택 인자로 |
| C6 | C#·유니티 권장 구조 | **90.0** | 0 | 20 | — |
| C7 | 불필요한 매 프레임 반복 | **82.5** | 1 | 12 | 같은 프레임·같은 좌표의 포인터 판정을 소비자마다 |
| | **요청 7기준 평균** | **68.6** | 20 | 189 | |
| C8 | 캡슐화·정보 은닉 (추천) | 82.5 | 1 | 12 | 입력 묶음이 부품을 구체 타입으로 내놓음 |
| C9 | 수명·자원 관리 안전성 (추천) | 90.0 | 0 | 14 | — |
| C10 | 정확성·오류 처리 (추천) | 37.5 | 7 | 13 | 잠재 결함 7 |
| C11 | 핫패스 힙 할당 (추천) | 82.5 | 1 | 16 | Render마다 필드 유닛마다 상태 읽기 할당 |
| C12 | 주석·문서 정합성 (추천) | 90.0 | 0 | 24 | — |
| | **12기준 평균** | **71.9** | 29 | 272 | |

- **높음 0.** 늘 틀리게 동작하거나 멈추는 것은 찾지 못했다. 결함 일곱(C10)은 모두 특정 조작 순서나 데이터·설정에서 난다.
  - 조작 순서로 나는 넷: 카메라 이동 뒤 클릭, 재생 중 Drag, UI 밑 칸 커서 스냅, 콜백 예외.
  - 지금 데이터에서 잠복한 셋: Nexus 이펙트, 보드 위 물체, 필드 회전.
- 낮음은 모든 기준이 상한(−10)에 닿았다. 그래서 점수 차이는 중간이 만든다. 90점은 "중간 0 + 낮음 상한"이다.
- 표의 낮음 수에는 기반 한계 넷(감점 없음, §7)을 넣지 않았다.
- 가장 약한 곳은 **SSOT(22.5)와 정확성(37.5)**이다. 구조(SOLID·독립성 60)가 그다음이다. Framework 계약·C#/Unity 관례·수명·주석은 중간 0이다.

## 1. 추천 항목 다섯 — 왜 더했나

| # | 항목 | 이유 |
|---|---|---|
| C8 | 캡슐화·정보 은닉 | "부품을 그대로 내놓아 바깥이 내부 단계를 조합"하는 문제는 SRP·SSOT로 잡히지 않는다. 10-20 7기준 검토에서도 쓴 항목이다 |
| C9 | 수명·자원 관리 안전성 | 이 코드의 결함 이력 대부분이 대여·풀·정리 순서에서 나왔다(10-24 V-M2 풀 반환 크기, 10-30 H1 서는 자리 Reset) |
| C10 | 정확성·오류 처리 | 나머지 기준은 구조를 본다. 실제로 틀리게 동작하는 경로와 F1 오류 경계 우회는 따로 센다 |
| C11 | 핫패스 힙 할당 | 11-03 GC 끊김 작업의 후속 점검이다. C7(헛도는 일)과 비용 종류가 달라 나눴다 |
| C12 | 주석·문서 정합성 | 한국어 XML doc과 설계 절 참조가 많다. 낡은 설명은 다음 변경을 잘못 이끈다 |

## 2. 평가 방법

**범위(점수에 들어감) 487개 파일, 57,786줄**
- `ViewSystem/Combat/**` 415개(52,012줄)
- `BufferSystems/CombatView/*`, `CombatEndUI`
- `Factory/Runtime/InGame/View/*`에서 Lobby·StageMap Factory를 뺀 것
- `DataBaseSystem/Data/View/CombatPresentation/*`, `Data/View/Table/CombatPresentationSettings/**`(전투 연출 값 입력 계층)

**범위 밖(기반·다른 영역)**: Framework, GlobalSystem(`CombatEndUI` 제외), Core, 전투 골격(`LifeCycles/Combat/`, 10-24에 따로 평가), 다른 View DB 표, 다른 씬, 시험. 범위 밖 코드 자체는 감점하지 않았다.

**감점표**
- 10-24 이후 View 측정과 같은 표를 100점으로 옮겼다(×10): 높음 −20, 중간 −7.5, 낮음 −2.5.
- 낮음은 기준마다 −10까지만 깎는다.
- 같은 근본 원인에서 온 것은 기준 안에서 −15까지 깎는다(높음 하나는 그대로). 이번 중간 29개는 기준 안에서 근본 원인이 모두 달라 이 상한이 걸리지 않았다.
- 문제 하나는 주된 기준 하나에만 둔다.
- 기반이 강제한 한계는 감점하지 않는다. 그 한계를 흡수하는 범위 안 구조가 사본·우회를 만들면 그 몫만 깎는다.
- 심각도 정의
  - 높음: 지금 운영에서 틀리게 동작·정지하거나, 구조 위반이 커서 그 종류의 변경마다 여러 영역을 함께 고쳐야 함
  - 중간: 조건부 결함, 실수하기 쉬운 사본·우회·넓은 의존, 조용히 묻히는 오류, 자주 도는 경로의 의미 있는 낭비
  - 낮음: 읽기·유지 비용, 국소 위반

**절차**
1. **찾기 41명.**
   - 슬라이스 26명: 범위 파일을 폴더별로 약 2,200줄씩 나눠 **끝까지 읽음**. 26개 모두 목록의 파일을 다 읽었다.
   - 렌즈 15명: 범위 전체를 한 관점으로 훑음. 렌즈는 규칙 사본, 상태 사본, SOLID, FW 계약, 변경 시나리오 8개, 숨은 결합, 죽은 코드, C#/Unity, 평소 프레임, 재생·입력 프레임, 캡슐화, 수명, 결함 사냥, 할당, 주석이다.
   - 완전성 비평 1명이 빠진 영역 넷을 찾았고, 보강 4명이 그 넷을 봤다. 넷은 재생 취소·연결 끊김, 미리 만들기와 사전 준비의 겹침, 입력 소유자 전환, 다음 전투의 잔여 상태다.
   - 원시 문제는 420 + 9개다.
2. **합치기.** 기준별 합치기 12명이 같은 문제를 묶었다. 이어서 영역 버킷 9명이 기준을 넘는 중복을 판정했다. 남은 후보는 307개다.
3. **적대적 검증**
   - 중간 이상 후보 71개: 증거·심각도 검증자와 반론 검증자가 따로 판정했다. 둘이 갈리면 판정자가 하나 더 판정하고, 셋의 가운데 값을 썼다.
   - 낮음 후보 236개: 8개 묶음 검증자가 판정했다. 중간 이상으로 올린 것은 다시 위의 세 검증을 거쳤다.
   - 결과: 중간 이상 71 → 중간 29·낮음 40·버림 2(찾기 때 높음 하나는 중간으로). 낮음 236 → 낮음 232·버림 4. **남은 문제는 301개**(중간 29·낮음 272)다.
4. **점수.** 위 감점표로 계산했다. 같은 판정 자료로 다시 계산해도 같은 점수가 나온다.

**독립성**
- 검토자에게 `CLAUDE.md`의 현재 상태 서술, 과거 평가 문서, git 기록을 근거로 쓰지 못하게 했다.
- 찾기 41명 중 33명과 합치기·검증 단계 전부는 `CLAUDE.md` 없이 읽기 전용으로 돌았다.
- 먼저 끝난 8명(S01·S10·S11·S19·S20·L02·L09·L10)은 `CLAUDE.md`가 주입된 채 돌았다. 사용량 한도 때문에 도중에 방식을 바꿨다.

## 3. 이전 평가와 비교 (참고 — 직접 비교하지 않는다)

| 평가 | 기준 판 | 범위 | 검토 | 기준 | 점수(100점) |
|---|---|---|---|---|---|
| 10-24 View 단계 독립 평가 | `8ed3f8cb` | ViewSystem/Combat 241개·31,916줄 + Buffer·CombatEndUI·Factory 셋 | 검토자 3명 + 중간 이상 직접 대조 | 6기준 | 74 (7.4) |
| 10-25 3라운드 측정 | 3라운드 끝 | 같은 범위 | 블라인드 A/B 3명 | 6기준 | 88 (8.8) |
| **이번** | `e70ad3d6` | 487개·57,786줄 | 찾기 41 + 보강 4 + 검증 약 200 | 12기준 | 요청 7기준 68.6 / 12기준 71.9 |

- 범위가 1.6배다(ViewSystem/Combat 241 → 415개). 찾는 깊이가 크게 다르고 기준 구성도 다르다. 감점 방식이라 더 넓고 깊게 찾을수록 점수가 내려간다.
- 참고로 중간 이상 문제의 밀도는 1만 줄당 10-24 약 4.1개(13/31.9k), 이번 약 5.0개(29/57.8k)다.
  - 깊이 차이를 생각하면 크게 나빠졌다고 보기 어렵다.
  - 다만 늘어난 연출 기능(기믹·진입 연출·특성 피드백·도약) 쪽에 SSOT·결합 문제가 몰렸다.

## 4. 중간 문제 29개

검증 표기는 "증거 / 반론 [/ 판정]" 검증자의 심각도다. 경로의 `V/`는 `ViewSystem/Combat/`, `S/`는 `Assets/ProjectL/Script/Systems/`다.

### 4.1 C10 정확성·오류 처리 (7) — 조건부 결함

1. **C10-01 카메라 이동·줌 뒤 포인터를 다시 집지 않는다**
   - 위치: `V/Input/Cameras/CombatCameraInput.cs:75-84`, `V/Input/Interaction/AttackTargetingInteraction.cs:144-181·403-416`
   - 문제: 키·화면 가장자리·휠로 시점을 옮겨도 포인터 소비자에게 알리지 않는다. 공격 조준·배치 Drag·유닛 호버는 PointerMoved 때만 칸을 집는다. 환경 호버만 매 프레임 다시 집는다.
   - 영향: 공격 모드에서 A 칸 미리보기를 본 뒤 마우스를 두고 카메라를 옮긴다. 미리보기는 A에 남지만 클릭은 새로 집은 B를 제출한다. 배치도 같다.
   - 고칠 방향: 시점이 바뀐 프레임에 마지막 좌표로 PointerMoved를 한 번 다시 보낸다. 또는 포인터 표본을 한 곳에서 만든다(C7-02와 같은 자리).
   - 검증: 중간 / 중간
2. **C10-02 Unity 프레임 콜백의 예외가 F1 오류 경계에 닿지 않는다**
   - 위치: `V/Display/Field/FieldView.cs:311-365`(LateUpdate → FlushPending), `V/Presentation/Effects/CombatEffectFollower.cs:101-140`, `V/Display/Map/FieldTornadoArrowPulse.cs:9-26`, `UnitHUDLayer.RefreshPositions`(Canvas 콜백)
   - 문제: 이 경로들은 Runtime Tick 밖에서 돌고 예외를 보고하지 않는다. 같은 코드베이스의 `OnDisable`은 F1에 보고한다.
   - 영향: 그리기가 예외를 내면 대기 칸만 사라진다. 화면은 옛 범위인데 상태·입력은 새 범위를 쓰고, F1 없이 전투가 계속된다.
   - 참고: 강조 그리기 쪽은 11-03 GC 끊김 계획 §3.7 L2에서 받아들인 차이("그리기 실패는 Unity 콘솔")다.
   - 고칠 방향: 콜백 입구를 `OnDisable`처럼 감싸 RuntimeFaultBoundary로 보고한다. 또는 플러시를 Tick 끝의 명시 호출로 옮긴다.
   - 검증: 중간 / 중간
3. **C10-03 Nexus 착지 이펙트가 끝나기를 기한 없이 기다린다**
   - 위치: `V/Presentation/Entry/CombatEntryNexusAppearance.cs:110-123`, `V/Presentation/Effects/CombatEffectPlacement.cs:41-45`, `CombatEffectWindow.cs:166-177`
   - 문제: 완료 조건이 "착지 ∧ 이펙트가 풀로 돌아옴"이다. 이펙트는 자연 끝(Stay)이라 시간 상한이 없다.
   - 영향: 낙석과 함께 쓰는 `FallingRocks` 프리팹을 Looping으로 바꾸거나 `ParticleStopNotifier`를 빼면, Nexus 스테이지 진입이 영구히 멈춘다. 입력·명령·포기가 모두 기다리고 경고 한 줄만 남는다. 지금 에셋은 약 1초 뒤 끝난다(잠복).
   - 고칠 방향: 착지만으로 완료하거나, 시간 끝·상한을 둔다.
   - 검증: 낮음 / 중간 / 중간
4. **C10-06 보드가 올라오는 동안 숨기지 않은 보드 위 물체가 먼저 떠 보인다**
   - 위치: `V/Presentation/Entry/CombatBoardEntryPresentation.cs:76-92`, `CombatEntryNexusAppearance.cs:54-55`, `V/Display/Unit/FieldUnitPresenter.cs:364-376·428-452`
   - 문제: 진입 준비가 숨기는 대상은 타일·배경·지형·선로·적 Nexus(낙하 켜짐)·필드 적뿐이다.
   - 영향: 타일 스테이지에 화약통·지뢰·장애물을 두거나 `nexusDrop`을 끄면, 약 1.4초 동안 그 물체와 HUD가 빈 구멍 위에 떠 있다. 지금 운영 스테이지에는 이런 배치가 없다(잠복).
   - 고칠 방향: 보드 위 표시를 수집 입구 하나로 모아 보드 단계 동안 숨긴다.
   - 검증: 중간 / 중간
5. **C10-07 커서 스냅이 옮길 칸 중심의 UI 가림·화면 밖을 보지 않는다**
   - 위치: `V/Input/Assist/CombatCursorSnap.cs:49-69`, `V/Display/Feedback/CombatPointerMapper.cs:121-150`
   - 문제: 포인터 위치의 UI 가림만 보고, 칸 중심 화면 좌표로 바로 Warp한다.
   - 영향: 아래쪽 칸이 덱 UI 밑으로 반쯤 들어가면 커서가 UI 아래 칸 중심으로 끌려간다. 그 칸은 끝내 누를 수 없다. 칸 중심이 화면 밖이면 커서가 창 밖으로 나간다. 운영 씬은 snapCursor가 켜져 있다.
   - 고칠 방향: Warp 전에 중심이 카메라 pixelRect 안이고 UI에 가리지 않을 때만 옮긴다.
   - 검증: 중간 / 중간
6. **C10-08 FieldView는 Y축 회전을 허용하는데 GridOutline 표시기는 경고 한 번 뒤 아무것도 그리지 않는다**
   - 위치: `V/Display/Field/FieldView.cs:37-38·608-627`, `FieldOutlineRenderer.cs:172-177·223-226`
   - 영향: Cells Root를 90° 돌린 GridOutline 스테이지는 Build는 통과한다. 그런데 이동·공격 범위·호버가 전혀 보이지 않는다. 지금 운영 필드는 돌아가 있지 않다(잠복).
   - 고칠 방향: GridOutline 모드의 회전은 Build에서 예외로 막고, 툴팁을 모드에 맞춘다.
   - 검증: 중간 / 낮음 / 중간
7. **G3-01 배치·아이템 Drag가 결과 재생 중에도 이어지고, 재생 중에 놓으면 제출이 조용히 거절된다**
   - 위치: `V/Input/Interaction/PlacementDragInteraction.cs:148-161·302-321·366-393`, `CombatItemDragInteraction.cs:89-105`, `V/Input/CombatInputPolicy.cs:81-103`
   - 문제: 미리보기(`CanPreview`)는 재생 중에도 허용하고, 제출(`CanSubmit`)은 거절한다. `TrySubmit`은 0을 돌려줄 뿐 안내가 없다.
   - 영향: 배치 결과 재생 중 유닛 B를 집어 초록 고스트 칸에 놓으면, B가 원래 자리로 돌아간다. 명령도 안내도 없다. 아이템 장착도 같다.
   - 고칠 방향: 재생·후처리 중에는 Drag 시작·미리보기도 막아 두 정책의 경계를 맞춘다. 또는 놓은 것을 보류했다가 확인 뒤 제출한다.
   - 검증: 중간 / 중간

### 4.2 C1 SSOT (9)

1. **C1-05 환경 원본 '보여 줄지' 식이 여섯 곳에 따로 있다**
   - 위치: `V/Display/Map/CombatEnvironmentFacts.cs:9-24`, 그 밖에 `FieldTrailPresenter.cs:142`·`ObjectViewSet.cs:52`·`TerrainViewSet.cs:224-225`·`CombatEnvironmentText.cs:53`·`CombatDustScreenHoverCells.cs:33`·`CombatHazardLookup.cs:41`
   - 문제: 공용 판단 클래스가 `IsKnownToPlayer`만 준다. `Active && IsKnownToPlayer`는 각자 적고, 그중 둘은 주석에 스스로 "유일한 판단"이라고 쓴다.
   - 영향: 공개 정책을 바꿀 때 한 곳을 빠뜨리면, 그리지 않은 지뢰의 위험 범위가 호버에 나오거나 그리지 않은 흔적이 설명창에 나온다.
   - 고칠 방향: `CombatEnvironmentFacts.IsShown(원본)` 하나를 둔다.
   - 검증: 중간 / 중간
2. **C1-11 도약 구간 끊김 규칙이 계획과 레인에 둘이다**
   - 위치: `V/Presentation/Movement/CombatLeapPlan.cs:58-83`, `CombatUnitMotionPlayback.cs:303-320`
   - 문제: 같은 조건(행동 시작·출처 열기·끌어오기, 도약 유닛의 사망·이탈·재배치·복귀·앵커)이 두 곳에 있다. 범위도 이미 다르다.
   - 영향: 새 중단 사건을 레인에만 더하면 다음 중간 걸음에서 `BeginStep`이 예외를 던져 이동 도중 F1로 멈춘다.
   - 고칠 방향: 끊김 판정 함수 하나를 둘이 부른다.
   - 검증: 중간 / 중간
3. **C1-14 표시 캐릭터 사본이 둘이다**
   - 위치: `V/Display/CombatSceneDisplay.cs:66-71·282-317·362-391`
   - 문제: 선택 강조는 보인 사실(사건마다 갱신)을 쓰고, 목표 강조·호버 원점은 결과 끝 스냅숏 `_displayedCharacters`를 쓴다.
   - 영향: 재생 중 목표 보스가 죽거나 밀리면 모델과 선택 강조는 따라간다. 목표 강조는 결과 끝까지 옛 칸에 남는다.
   - 고칠 방향: 목표 강조도 보인 사실로 그린다.
   - 검증: 중간 / 중간
4. **C1-21 강조 층의 '원점 열기'·호버 범위 소속이 층 표 밖에 있다**
   - 위치: `V/Display/Field/FieldAttackOutlineOpening.cs:13-27·95-97`, `FieldHighlightLayers.cs:109-122·153-197`
   - 문제: 층 표는 "층을 더하면 이 표 한 줄"이라고 안내하고, 정적 검사도 표만 본다.
   - 영향: 새 호버 범위 층을 표에만 더하면 검사는 통과한다. 그런데 원점 쪽 변이 닫힌 채 조용히 다르게 그려진다.
   - 고칠 방향: 두 속성을 층 표 칸으로 옮긴다.
   - 검증: 낮음 / 중간 / 중간
5. **C1-22 정보창 HP 막대 규칙이 UnitHUD 사본이다**
   - 위치: `V/Display/Hud/CombatUnitInfoView.cs:78-81·297-329`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs:263-278·326-340`
   - 문제: 분모 `Max(max, hp+shield)`, 스냅 조건, 보호막 채움이 줄 단위로 같다. 주석도 "HUD와 같이"다.
   - 영향: HUD 규칙만 바꾸면 같은 유닛의 필드 HUD와 정보창 막대 비율이 달라진다.
   - 고칠 방향: 막대 규칙 함수 하나를 둘이 부른다. UnitHUD는 GlobalSystem(현강님 줄)이라 함께 정한다.
   - 검증: 중간 / 중간
6. **C1-38 약한 지반 진입·붕괴 소리 번호가 코드에 박혀 설정 값을 무시한다**
   - 위치: `V/Playback/Support/CombatEventSounds.cs:156-158·178-186`, `CombatSoundPresets.cs:15-18·80-81`
   - 문제: 설정 표에 `weakGroundCollapseSoundPreset`(기본 −1 = 소리 없음)이 있고 `WeakGroundCollapse()`도 있다. 그런데 호출자가 없고, 붕괴는 2·진입은 0/1을 코드로 낸다.
   - 영향: 기획자가 표 값을 바꿔도 소리가 그대로다.
   - 검증: 중간 / 중간
7. **C1-42 곡사 애니메이션이 공통 공격 모션 수명을 상속하지 않고 따로 구현한다**
   - 위치: `V/Presentation/Attack/CombatAttackMotion.cs:47-165`, `CombatMortarAnimation.cs:15-46·74-158`
   - 문제: 직선·근접은 기반을 상속한다. 곡사만 시작·취소·정리 깃발을 따로 들고 있고, 비유한 값에 던지는 예외 종류도 이미 다르다.
   - 영향: 수명 정책을 기반에서만 고치면 곡사만 옛 순서로 정리된다.
   - 고칠 방향: 곡사도 `CombatAttackMotion`을 상속한다.
   - 검증: 중간 / 중간
8. **C1-57 특성 → 상태 근거 표가 Core 특성 효과 규칙을 다시 적는다**
   - 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs:38-48`, `V/Playback/Support/CombatTraitStateEvidence.cs:48-80`
   - 문제: '가속 = HeldAttackBonus 변화', 'CleanSweep = 공격력 증가 부여' 같은 대응을 상수 표로 들고 있고, Core 상태 타입을 직접 찾는다.
   - 영향: Core가 효과 상태를 바꾸면 DB 검사는 통과하고, 그 특성의 이펙트·글자가 매번 빠진다.
   - 참고: Core가 발동-상태 연결을 주지 않는 한계는 있다. 감점은 그 흡수를 사본 표로 한 몫이다.
   - 고칠 방향: Core 전달 문서에 연결 사실을 요청한다.
   - 검증: 낮음 / 중간 / 중간
9. **C3-01 토네이도 도착 칸을 Core 사실 대신 View가 다시 계산한다**
   - 위치: `V/Input/Assist/CombatTornadoDestinationCells.cs:13-40`, Core `TornadoAreaFact.cs:21-42`, `CombatEnvironmentDescriptions.cs:56-76`
   - 문제: Core가 같은 Travel로 `DestinationCells`를 이미 준다. View는 정의를 다시 찾아(GimmickId−1·SourceId) Travel·Footprint를 다시 부른다.
   - 영향: Core가 이동 계산을 바꾸면 고정한 도착 영역이 실제와 달라진다. 정의 규칙이 바뀌면 예외(F1)가 난다.
   - 고칠 방향: `area.DestinationCells`를 모으기만 한다.
   - 검증: 중간 / 중간

### 4.3 C2 SOLID (4)

1. **C2-01 필드 기믹에 공통 수명 계약이 없어 10여 곳에 이름으로 나열한다 (OCP)**
   - 위치: `V/Display/CombatSceneDisplay.cs:91-99·197-210·264·393-414`, `CombatEntryDeferredDisplay`, `V/Playback/CombatFieldEventPorts.cs:33-60`, Factory
   - 문제: 토네이도·낙석·하붑·가시·흔적마다 필드·생성자 인자·null 검사·정리 목록 세 벌·진입 보류·포트·처리기를 손으로 적는다.
   - 영향: 새 기믹 하나가 10개 넘는 파일로 번진다. 정리 목록 하나를 빠뜨려도 컴파일·검사가 잡지 못한다.
   - 고칠 방향: 기믹 표시 공통 계약(Bind/Render/Clear/Release/TickAppearance)을 두고 Factory 목록 하나로 조립한다.
   - 검증: 중간 / 중간(찾기 때 높음)
2. **C2-02 조정자 `CombatViewSystem`이 God 클래스다**
   - 위치: `V/CombatViewSystem.cs`(1,179줄, 필드 30, `BindServices` 인자 18)
   - 문제: 서비스 배선·씬 수명·연결/분리·결과 소비·진행 호스트·입력 분기·매 Tick 표면 파생을 함께 맡는다. 외부 호출 뒤 재확인 줄도 많다(`CanContinue` 28곳).
   - 영향: 새 입력 소유자를 넣을 때 `RouteInput`의 정보창 차단 조건을 따로 고치지 않으면, 새 모드에서 클릭할 때 정보창이 함께 열린다.
   - 고칠 방향: 결과 소비·입력 분배·표면 동기화를 연결 수명 객체로 옮기고, 조정자는 수명·순서만 맡는다.
   - 검증: 중간 / 중간
3. **C2-03 `CombatSceneDisplay`가 God 퍼사드다**
   - 위치: `V/Display/CombatSceneDisplay.cs:49-110·130-169`
   - 문제: 협력 객체 30여 개, 생성자 38인자, 공개 멤버 약 60, 통과 메서드 약 45개다.
   - 영향: 입력 피드백 하나 추가와 배경음 규칙 변경이 같은 생성자·RefreshCore에서 충돌한다. 같은 타입 위치 인자를 바꿔 넣어도 컴파일된다.
   - 고칠 방향: 배경음 정책, 입력 피드백 하위 계약, 완료·행동 버튼 표면을 나눈다.
   - 검증: 중간 / 중간
4. **C2-04 클립 이벤트 종류가 enum 넷과 대응 switch 여러 곳에 흩어져 있다 (OCP)**
   - 위치: `V/Presentation/Pieces/CombatClipEventCursor.cs:7-23·75-81`, `CombatClipPlayback.cs:17-44·80-88`, `CombatClipPiece`, `CombatPresentationCueRules.AnchorOf`
   - 영향: 이벤트 하나를 추가하려면 6~8곳을 고친다. 한 곳을 빠뜨리면 그 신호를 기다리는 발사·큐가 조용히 빠진다.
   - 고칠 방향: Id↔Signal↔Anchor 표 하나와 `Action<CombatClipEventId>` 콜백 하나로 바꾼다.
   - 검증: 중간 / 낮음 / 중간

### 4.4 C4 기능 변경의 독립성 (4)

1. **C4-02 사건 Presenter 생성자가 처리기·재생기 20여 개를 직접 조립한다(두 번째 조립 지점)**
   - 위치: `V/Playback/CombatSceneEventPresenter.cs:96-170`, `CombatFieldEventPorts.cs:33-60`(14개 위치 인자), `CombatEventHandlerTable.cs:27-29`
   - 영향: 새 기믹 처리기 하나에 ports 인자·미리보기 Factory 위치 인자·Create·생성자를 함께 고친다. `Func<bool>` 위치 인자의 순서를 바꿔도 컴파일된다.
   - 고칠 방향: 조립을 `CombatEventPlaybackFactory`로 옮긴다. Presenter는 완성된 표·목록만 받는다(C2-01과 함께).
   - 검증: 중간 / 낮음 / 중간
2. **C4-05 도약 중간의 배치 대여 유지가 자세·서는 자리 기능과 숨은 순서로 묶여 있다**
   - 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:303-334`, `V/Display/Unit/CombatUnitPresentationWindow.cs:36-53·365-369`
   - 문제: 레인이 '자세를 여는 사건 종류'를 자기 목록으로 들고 있다. 실제로 자세를 여는 조건은 캐릭터 처리기에 따로 있다.
   - 영향: 피격 반응을 다른 상태 사건으로 넓히면 `RequireNoPlacement` 예외로 F1이 난다. 서는 자리를 빌리는 새 기능이 `PauseLeap`을 넘기지 않으면 행동이 경고만 남기고 빠진다.
   - 고칠 방향: 대여 창구가 다른 대여 요청이 오면 도약 배치를 스스로 양보한다(C1-11과 함께).
   - 검증: 중간 / 중간
3. **C4-08 큐 창구가 선택 인자 사슬로 공격 조립 세 입구를 뚫고 내려간다**
   - 위치: `V/Presentation/Pieces/CombatClipAttack.cs:50-52·80-83·125-131`, `CombatCodeMotionAttack.cs:45-47`, `CombatCueComposer.cs:26-28`
   - 영향: 새 창구를 한 입구에만 넘겨도 기본 null이라 통과한다. 그러면 클립 없는 원거리 유닛의 그 큐만 조용히 사라진다.
   - 고칠 방향: 처리기가 `CombatCueSinks`를 한 번 만들어 묶음 하나로 넘긴다.
   - 검증: 중간 / 낮음 / 중간
4. **C4-13 입력 소유자를 하나 더하면 포인터 대상 판정식 서명이 바뀌어 경로 입력·환경 호버까지 고친다**
   - 위치: `V/Input/CombatInputRouter.cs:54-62·99-107·227-250`, `V/Input/MovePath/CombatPathInput.cs:118-129`
   - 문제: `ResolvePointerTarget(layer, attackActive, itemDragActive)`의 호출처가 5곳이다. `CombatInputFacts`의 27인자 대부분이 선택 인자다.
   - 영향: 새 소유자의 깃발 전달을 Reader에서 빠뜨려도 컴파일되고, 경로 편집·환경 호버가 새 모드 중에도 켜진다.
   - 고칠 방향: 활성 전경 소유자의 대상을 `CombatInteraction`이 답하게 한다.
   - 검증: 중간 / 낮음 / 중간

### 4.5 C5·C7·C8·C11 (5)

1. **C5-01 꺼 둔 아침 화면 전환 경로가 조립·검사·재생 판정에 남아 있다**
   - 위치: `V/Playback/CombatPhaseTransitionPresenter.cs:11-111`(TryBegin 본문이 주석, 늘 false), `CombatSceneEventPlayback.cs:69-74·126-137·162-181`, Factory 두 곳
   - 영향: 쓰지 않는 기능의 설정·카메라 이름 검사 때문에, 이름을 바꾸면 전투 씬·특성 미리보기 조립이 멈춘다. 재생 Gate를 고치는 사람은 늘 거짓인 조건 다섯을 함께 읽어야 한다.
   - 고칠 방향: 되살릴 계획이 없으면 함께 걷어 낸다. 되살릴 계획이 있으면 조립에서 빼고, 꺼져 있을 때는 검사하지 않는다. 파일은 민형님 코드(`e18c8313`)이고 자동 전환을 끈 것은 호진님(`b9c4f3b4`, 10-29 "밤 전환 안되게 테스트로 수정")이라, 되살릴 계획은 두 분께 확인하고 고치면 공유한다(11-04 후속 확인으로 보충).
   - 검증: 중간 / 중간
2. **C5-02 입력 계층이 늘 넘겨받는 협력자를 선택 인자로 받고 null 갈래를 둔다**
   - 위치: `V/Input/CombatInputRouter.cs:75-87·142-145·218-222`, `AttackTargetingInteraction`·`CombatInteraction`·`PlacementDragInteraction`, `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:78-82`
   - 문제: 운영 조립은 하나뿐이고 늘 값을 넘긴다. 시험 호출도 없다.
   - 영향: 새 조립 경로가 하나를 빠뜨려도 공격 사전 거절·거절 안내·궤적이 오류 없이 사라진다.
   - 고칠 방향: 필수 인자로 바꾸고 생성자에서 `?? throw`로 막는다.
   - 검증: 중간 / 낮음 / 중간
3. **C7-02 같은 프레임·같은 좌표의 UI Raycast·칸 집기를 소비자마다 따로 한다**
   - 위치: `V/Input/CombatInputRouter.cs:179-186·224-256`, `CombatAttackDragGesture.cs:56-64`, `CombatUnitRangeHover.cs:99-111`
   - 문제: 포인터가 움직인 프레임에 `RaycastAll`이 4~5번, 카메라 Ray가 3~4번 돈다.
   - 영향: HUD·덱·가방·정보창 UI가 많을수록 마우스를 움직이는 프레임마다 비용이 그 배수로 커진다.
   - 고칠 방향: 프레임마다 포인터 표본 하나를 만들어 소비자가 읽는다(C10-01과 같은 자리).
   - 검증: 중간 / 중간
4. **C8-01 `CombatSceneInput`이 부품을 구체 타입으로 내놓아, 조정자가 내부 단계·모드 깃발을 조합한다**
   - 위치: `V/Input/CombatSceneInput.cs:63-112`, `V/CombatViewSystem.cs:405-407·836-842·1103-1109`
   - 영향: 새 전경 소유자를 추가하면 `IsInputOwnerActive`는 따라가지만, 조정자의 깃발 셋 조합은 그대로다. 그래서 새 모드 중에 정보창이 끼어든다(C2-02와 같은 증상의 다른 면).
   - 고칠 방향: 묶음 수준 입구(BeforeResult·AfterResult·Tick·CanInspectOnPress)를 두고 부품은 숨긴다.
   - 검증: 중간 / 낮음 / 중간
5. **C11-02 유지 이펙트 맞추기가 Render마다 필드 유닛마다 상태 읽기를 새로 할당한다**
   - 위치: `V/Presentation/Effects/CombatStatusEffectSource.cs:29-41`, Core `StatusEffectReadModel.cs:15-44`, `V/Playback/Support/CombatShownFacts.cs:325-336`
   - 문제: `Capture`가 호출마다 List·LINQ·배열·스냅숏을 만든다. Render는 사건 순간·걸음 도착·결과 끝마다 돈다.
   - 영향: 유닛 12명이 연쇄 이동하면 도착마다 할당이 수십 개 생기고, 재생 중 GC가 잦아진다.
   - 참고: HUD 쪽 중복 할당은 11-03 GH3(같은 사실이면 건너뜀)로 이미 줄었다.
   - 고칠 방향: 사실이 바뀐 유닛만 다시 읽거나, Render 한 번에 유닛별 `Capture`를 한 번만 한다.
   - 검증: 중간 / 중간

## 5. 개선 권장 묶음 (우선순위·예상 점수)

| 묶음 | 내용 | 중간 | 누적 예상(12기준 / 요청 7기준) |
|---|---|---|---|
| 지금 | | 29 | 71.9 / 68.6 |
| A 결함 | C10 일곱: 포인터 다시 집기(C7-02와 함께), Unity 콜백 F1 경계, Nexus 대기 상한, 진입 숨김 대상, 커서 스냅 대상 검사, 필드 회전 계약, 재생 중 Drag | 7 | 76.3 / 68.6 |
| B SSOT | C1 아홉: 환경 `IsShown` 하나, 도약 끊김 판정 하나, 목표 강조는 보인 사실로, 층 표 칸 둘, HP 막대 규칙, 소리 설정 값, 곡사 상속, 특성 근거(Core 요청), 토네이도 `DestinationCells` | 9 | 81.9 / 78.2 |
| C 구조 | 기믹 공통 계약 + 조립을 Factory로(C2-01·C4-02), 조정자·씬 표시 나누기(C2-02·C2-03), 입력 묶음 입구·포인터 대상 답(C8-01·C4-13), 클립 이벤트 표(C2-04), 큐 창구 묶음(C4-08), 도약 대여 양보(C4-05) | 9 | 87.5 / 86.8 |
| D 헛일·프레임·할당 | 화면 전환 경로 정리(C5-01), 선택 인자 필수로(C5-02), 포인터 표본 하나(C7-02), 상태 읽기 한 번(C11-02) | 4 | 90.0 / 90.0 |
| 90 넘기 | 기준마다 낮음을 넷 미만으로 줄인다([문제 목록](2026-11-04-combat-view-evaluation-issues.md)) | — | 최대 100 |

- 같은 자리끼리 묶어 고친다.
  - 포인터: C10-01·C7-02
  - 입력 묶음: C2-02·C8-01·C4-13
  - 기믹: C2-01·C4-02
  - 도약 레인: C1-11·C4-05
- 다른 작업자와 닿는 것
  - C1-22: UnitHUD, 현강님 GlobalSystem
  - C5-01: 민형님 `CombatPhaseTransitionPresenter`(자동 전환을 끈 것은 호진님 `b9c4f3b4`)
  - C1-57: Core 요청, 천명님
  - C10-02: `UnitHUDLayer`, 현강님 줄 포함
- 묶음 A의 C10-02는 11-03에 받아들인 차이(L2)를 되돌리는 결정이다. 사용자 결정이 필요하다.

## 6. 잘 된 구조

검토자들이 꼽은 강점을 범위 전체에서 모았다.

- **사실의 원본이 하나**
  - 결과 재생 수명은 `CombatResultPlayback` 하나가 갖고, 완료 기준은 Buffer의 P/S다.
  - 입력 판단 사실(`CombatInputFacts`)은 판단마다 새로 읽고 저장하지 않는다.
  - 강조 상태는 `FieldHighlightState` 하나이고, 표시기는 읽기 계약으로만 본다.
- **지식 표와 정적 검사**
  - `CombatEventTypeTable`(사건 형식 한 줄)과 처리기 표는 표에서 파생하고 검사한다.
  - `FieldHighlightLayers` 층 표는 정적 생성자가 누락·중복·받침 순서를 잡는다.
  - `CombatAttackStyles` 전략 표가 있고, 실제 발사·궤적 미리보기·클립이 같은 기하(`CombatShotPath`·`CombatAttackTarget`·`CombatAttackFacing`)를 쓴다.
- **대여·정리 규칙 한 곳**
  - `UnitPresentationLeaseSlot`·`CombatPieceRunLeases`가 늦은 복원을 무시하고 역순으로 회수한다.
  - 정리 경로 대부분이 `CleanupErrors`로 실패를 모으며 이어 간다.
  - `Clear`(풀 재사용)와 `Release`(파괴)를 나눴다.
- **입력**
  - 정책 한 곳(`CombatInputPolicy`), 대기 원본 하나(`CombatCommandGate`)가 있다.
  - 미리보기 슬롯 규칙은 `CombatPreviewSlotRule` 한 곳이고, 비우기는 `ClearPreview(number)`다.
  - 조준·넉백·체력 미리보기는 Core 응답을 그대로 쓰고 규칙을 흉내 내지 않는다(예외: C1-57·C3-01).
- **Framework 계약**
  - Builder는 한 번만 Build하고 ExportServices로 노출한다.
  - View가 쓰는 WriteSink는 `CombatViewBuffer`와 튜토리얼 요청뿐이다. InGameGroup은 읽지 않는다.
  - 렌더 자원은 DTO로 Update하고 Dispose한다.
  - `FindObjectsByType`는 씬 로드 한 곳에만 있다.
- **관례**
  - 487개 모두 네임스페이스가 폴더 경로와 같고, sealed가 기본이다.
  - 범위 안 struct는 readonly다(직렬화용 `CombatAttackPoint` 하나는 getter만 연다).
  - cref 1,157개가 모두 지금 코드로 풀리고, 설계 절 참조가 실제 문서와 맞는다.
- **프레임**
  - 평소 프레임 경로에 힙 할당이 없다: readonly struct 사실, 할 일이 없으면 첫 줄에서 반환, 같은 값이면 쓰지 않음.
  - 강조는 프레임당 한 번 그린다(11-03 GH1).
  - TMP 숫자는 할당 없는 `SetText("{0}", n)`로 쓴다.

## 7. 기반 한계 (감점 안 함)

| ID | 내용 |
|---|---|
| C3-08 | Global 서비스에 없는 UI 기능(확인창·동적 등록)을 형식 검사(downcast)로 꺼냄 |
| C3-07 | 공개 사실이 없어 표시값 일부를 Core Application 규칙 함수로 직접 계산 |
| C4-24 | 넉백 후속 대응이 Core 공격 처리의 사건 기록 순서에 기댐 |
| C5-U0241 | AudioSystem에 슬롯이 없어 연결 대기 SFX 칸이 null(오브젝트·추락·아이템·비행·피격·토네이도 예고 소리 없음) |

## 8. 검증에서 버린 후보 (6)

C10-04(근접 클립 Cue2·Cue3 예외), C10-05(특성 피드백 조용히 뺌), C1-18(미리 만들기 셈 규칙 사본), C1-10(BattleChangerUI·PackUI 보임 규칙), C5-24(합치기 잔여), C12-25(선로·Nexus 낙하 켜짐 설명). 버린 이유는 [문제 목록](2026-11-04-combat-view-evaluation-issues.md) 끝에 있다.

## 부록. 검토 범위

**슬라이스(파일을 끝까지 읽은 검토자 26명)**

| ID | 폴더 | 파일 | 줄 |
|---|---|---|---|
| S01 | 루트(조정자·수명) | 14 | 2,239 |
| S02 | Display 루트·Feedback | 12 | 2,398 |
| S03 | Display/Field (1) | 15 | 2,860 |
| S04 | Display/Field (2) | 15 | 3,043 |
| S05 | Display/Hud | 9 | 1,698 |
| S06 | Display/Map (1) | 15 | 2,661 |
| S07 | Display/Map (2)·Turn | 16 | 1,720 |
| S08 | Display/Unit (1) | 15 | 2,362 |
| S09 | Display/Unit (2) | 20 | 2,314 |
| S10 | Input 루트·ActionButtons | 20 | 2,293 |
| S11 | Input/Assist·Cameras·Contracts | 26 | 1,348 |
| S12 | Input/Interaction | 19 | 2,911 |
| S13 | Input/MovePath·UiEntry·Playback 루트 | 16 | 2,729 |
| S14 | Playback/Handlers | 14 | 1,366 |
| S15 | Playback/Ordering·Players | 24 | 2,448 |
| S16 | Playback/Support | 28 | 3,283 |
| S17 | Presentation/Attack | 25 | 3,270 |
| S18 | Presentation/Cameras·Effects | 17 | 1,882 |
| S19 | Presentation/Entry·Environment | 15 | 1,411 |
| S20 | Presentation/Movement | 17 | 1,827 |
| S21 | Presentation/Pieces (1) | 29 | 2,870 |
| S22 | Presentation/Pieces (2) | 17 | 1,241 |
| S23 | Presentation/Railway·Reaction·Text·Selection·Buffer·CombatEndUI | 23 | 2,670 |
| S24 | Factory(View) 전투 조립 | 15 | 1,100 |
| S25 | DV/CombatPresentation (1) | 40 | 2,823 |
| S26 | DV/CombatPresentation (2)·설정 표 | 11 | 1,019 |

**렌즈(범위 전체를 한 관점으로 본 검토자 15명)**
- L01 규칙·판정식·상수 사본
- L02 상태 사본·동기화
- L03 SOLID
- L04 FW 계약·있는 기능
- L05 변경 시나리오 8개
- L06 숨은 결합
- L07 불필요한 검사·죽은 코드
- L08 C#·Unity 관례
- L09 평소 프레임
- L10 재생·입력 프레임
- L11 캡슐화
- L12 수명·자원
- L13 결함 사냥
- L14 핫패스 할당
- L15 주석·문서

**보강(빠진 영역 4명)**
- G1 결과 재생 중 취소·연결 끊김
- G2 미리 만들기·사전 준비·풀 재사용 겹침
- G3 입력 소유자 전환·취소
- G4 다음 전투의 잔여 상태·결과 화면·Buffer 계약
