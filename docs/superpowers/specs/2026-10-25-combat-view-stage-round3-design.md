# 전투 View 단계 3라운드 설계 (2026-10-25)

- **상태:** 설계 초안. 코드 변경은 없다. 사용자 "라운드3에 해당 내용 포함해서 계획 작성해"에 따라 Task 계획([round3-plan](../plans/2026-10-25-combat-view-stage-round3-plan.md))을 함께 썼다.
  - 정할 것은 §8(Q-V19~Q-V28, Q-T7)이다. 계획은 권장안(모두 A)을 전제로 썼다. B를 고르면 그 Phase를 다시 쓴다.
  - 설계에서 정한 것은 §9(P18~P30)다.
  - **결정 완료(2026-10-25 사용자 "커밋했고 추천안대로 진행해, Phase 별 검토는 W 단위로 맞춰서 하는게 낫겠다"):** Q-V19~Q-V27·Q-T7 모두 A. Q-V28은 W 묶음 끝마다 검토로 바뀌었다(W10·W12 하나, W11·W8·W9 셋). 기준 판은 커밋 `35562e96`이다.
- **입력:**
  - [2라운드 측정 §11](2026-10-24-combat-view-stage-round2-design.md): N 8.1 → M 8.7. 남은 중간 넷과 낮음 목록이다.
  - [View 단계 설계 §6](2026-10-24-combat-view-stage-design.md): W8·W9 방향.
  - 2026-10-25 대화의 조정자 재점검: 1159줄, 필드 39, 메서드 53, 인터페이스 8, `BindServices` 인자 17, 책임 아홉 묶음.
- **기준:** 작업 트리 = 2라운드 뒤(커밋 전). 줄 번호는 지금 작업 트리 기준이다. 시작 전에 사용자가 2라운드를 커밋하면(Q-V19) 그 커밋이 측정 기준 판이다.
- **약어:** `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `D/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- **지킬 것(1·2라운드와 같음):**
  - MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다(보고에 "컴파일 미확인").
  - 커밋하지 않는다.
  - Framework·GlobalSystem·Core·골격의 기능·계약은 바꾸지 않는다.
  - 참조 없는 에셋·Missing Script는 지우지 않고 보고한다.

## 0. 목표와 측정

**목표:** 2라운드 측정이 남긴 중간 넷(공격 종류 분기, 강조 층 다섯 곳, 소리 프리셋, Ballista 두 경로)을 푼다. 또 점수에 덜 드러나는 조정자 몰림을 수명별로 나눈다.

**측정:** 1·2라운드와 같다. 라운드가 끝나면 같은 범위를 같은 평가자 셋이 블라인드 A/B로 매긴다(V3-k). 예상은 "알려진 문제 기준"으로만 적는다(지난 차이 0.2~1.8).

**점수와 조정자:** 조정자 크기는 SOLID 낮음 하나다. 그 기준의 낮음이 이미 상한(−1.0)에 걸려 있어 점수에는 거의 드러나지 않는다(대화 10-25). 그래도 W11을 넣는 것은 읽기·변경 범위 때문이다. 점수 예상(§10)에는 W11 몫을 크게 잡지 않았다.

## 1. 대상 문제 (2라운드 측정 §11 기준)

| # | 기준 | 심각도 | 문제 | 묶음 |
|---|---|---|---|---|
| V-M6 | SOLID | 중간 | 공격 연출 종류(Linear·Melee·Mortar·Ballista)를 파일 일곱이 각자 분기한다. 종류 하나를 더하면 일곱 곳을 고친다 | W8 |
| R2-M1 | SOLID | 중간 | 강조 층 하나의 속성(그리는 순서·색·채움·행동 중/평소 마스크)이 다섯 곳에 흩어져 있다. 마스크 하나를 빠뜨리면 GridOutline에서 그 층이 조용히 안 보인다 | W10 |
| V-M10 | 독립성 | 중간 | 유닛·공격 소리 프리셋이 유닛 키 switch에, 포효 소리가 보스 키에, 배경음 프리셋이 상수에 박혀 있다. 유닛을 더하고 switch를 빠뜨리면 조용히 소리가 안 난다 | W9 |
| V-L20 | 불필요 | 중간 | Ballista 코드 모션 길(시위·장전 화살·재장전)은 데이터로 닿지 않는데 계약·상태·궤적 갈래가 남아 있다. 관통 접촉 일정도 코드 길과 클립 길에 두 벌이다 | W8 |
| V-M9 | 있는 기능 | 낮음 | 화면 전환 설정(레이어·카메라 이름·색·속도)이 코드 상수다(민형님 코드) | W9 |
| R1-L | 있는 기능 | 낮음 | 환경 폭발 이펙트 `EffectType.Boom`이 Factory에 고정이다 | W9 |
| V-L8 | (불필요 낮음 L1에 포함) | 낮음 | `CombatSceneEventPresenter` 생성자 선택 인자 약 20개(P17로 미룬 것) | W9 |
| V-M4 잔여 | SOLID | 낮음 | 조정자 1159줄·필드 39·책임 아홉 묶음 | W11 |
| R2-L | SSOT·독립성·불필요 | 낮음 | 2라운드가 남긴 것: 씬 루트 두 필드, 14인자 생성 서명 세 벌, 배치 생성자의 쓰지 않는 `requests` 인자 | W11·W12 |
| R2-L | SSOT·독립성 | 낮음 | 기존 코드의 작은 사본: 주체 막기 규칙 두 벌, 레일웨이 판별을 소리 표가 따로 함, 가시밭 프리팹 검사가 늦음 | W12 |

## 2. 묶음과 순서

순서는 W10 → W11 → W12 → W8 → W9다.

- **W10(강조 층 표)** 가장 작고 다른 묶음과 파일이 겹치지 않는다. 사건 형식 표(2라운드 W7)와 같은 꼴이라 먼저 해 둔다.
- **W11(조정자 나누기 2차)** 사용자가 가장 신경 쓰는 곳이다. W12의 생성 서명 정리와 W9의 Presenter 필수 묶음이 조정자·Factory를 다시 고치므로 그보다 먼저 한다.
- **W12(작은 정리)** W11이 만든 연결 수명 객체 위에서 생성 서명 하나를 정리하고, 작은 사본을 걷는다.
- **W8(공격 종류 전략)** 공격 재생(636줄)을 크게 바꾼다. 그래서 조정자 변경과 섞지 않는다.
- **W9(값은 DB로)** 다른 사람 시험이 가장 많이 닿는다(현강님·호진님·민형님). 허락(Q-T7)을 받은 뒤 마지막에 한다.

## 3. W10 강조 층 표 (R2-M1)

**지금:** 층 하나를 더하려면 다섯 곳을 고친다(2라운드 V2-a에서 아군 호버 층을 더할 때 여섯 곳).
- 열거 `FieldHighlightLayer`
- 그리는 순서 `FieldHighlightLayers.DrawOrder`
- 색 `ColorOf` switch
- 채움 `FillsCell` 식
- 행동 중 마스크 `FieldHighlightState.ActingLayers`(`V/Display/Field/FieldHighlightState.cs:68-70`)
- 평소 마스크 `FieldOutlineRenderer.IdleLayers`(`V/Display/Field/FieldOutlineRenderer.cs:36-37`)

두 마스크는 서로의 여집합이다(행동 중 여덟 + 평소 셋 = 열한 층). 그런데 따로 적혀 있다.

**바꿈:** 층 표 하나. 그리는 순서대로 층마다 한 줄이다.

```csharp
// V/Display/Field/FieldHighlightLayers.cs (스케치)
private static readonly LayerRow[] Rows =
{
    //            층                                     색                                    채움   평소에도 그림
    new LayerRow(FieldHighlightLayer.ObjectiveTarget,    FieldHighlightColor.ObjectiveTarget,  false, true),
    new LayerRow(FieldHighlightLayer.ActiveActor,        FieldHighlightColor.ActiveActor,      false, true),
    new LayerRow(FieldHighlightLayer.MoveRange,          FieldHighlightColor.MoveRange,        true,  false),
    ...
    new LayerRow(FieldHighlightLayer.BlockedWall,        FieldHighlightColor.AttackHoverInvalid, false, false),
};
internal static IReadOnlyList<FieldHighlightLayer> DrawOrder { get; }   // Rows에서 만듦
internal static FieldHighlightLayer IdleLayers { get; }                  // 평소에도 그리는 층의 합
internal static FieldHighlightLayer ActingLayers { get; }                // 나머지 층의 합
internal static FieldHighlightColor ColorOf(FieldHighlightLayer layer)  // 표를 찾음
internal static bool FillsCell(FieldHighlightLayer layer)               // 표를 찾음
```

- 표를 만들 때 열거의 단일 비트 값이 모두 한 번씩 있는지 검사한다(사건 형식 표와 같은 규칙, P20).
- `FieldHighlightState`와 `FieldOutlineRenderer`는 자기 마스크 상수를 지우고 표의 값을 읽는다.
- 호버 색조 → 층(`CombatAttackFeedback.LayerOf`)은 그대로 둔다. 층의 속성이 아니라 "호버 색조가 어느 층에 그리나"라는 입력 쪽 사실이기 때문이다(P21).
- 동작은 같다. 값과 순서를 그대로 옮긴다. 층을 하나 더할 때 고치는 곳은 열거와 표 한 줄이고, 새 색이면 팔레트까지 셋이다.

## 4. W11 조정자 나누기 2차 (V-M4 잔여)

### 4.1 지금

| # | 묶음 | 줄(대략) | 대표 멤버 |
|---|---|---|---|
| 1 | Framework 모듈 수명·Tick | 117 + `BindServices` 87 | `InitData`·`PostBindService`·`BindServices`·`Initialize`·`Tick`·`Shutdown` |
| 2 | 씬 수명 | 63 | −195 배경 세우기, `ReleaseScene`, `ReleaseBackdrop`, 필드 `_loadedRoot`·`_backdrop` |
| 3 | 연결 수명 | 315 | `Attach`~`Detach`, `StopInput`, 필드 `_display`·`_surfaces`·`_input`·`_inspection`·`_deckHover`·`_subscribedRoot`·`_battleShown`·`_battleInstance` |
| 4 | 결과 소비 | 76 | `ConsumeResult`·`FinalizeResult` |
| 5 | 입력 연결 | 73 | `_layers`·`_receiver`·버튼 다리 셋·`SyncInputLayer`·`ReleaseInput`·`FocusLost`·`RequestReturn` |
| 6 | 입력 사실 조립 | 99 | `ReadFacts`(값 25개), 판정식 여섯, 지연 읽기 대리자 셋 |
| 7 | 진행 사실 호스트 | 약 30 | `OnInputClosed`·`CanShowCompletion`·`ShowCompletion` |
| 8 | 배치 확인창 | 44 | 생성·콜백 넷·`RouteInput`의 진입 갈래·`IsModalOpen` |
| 9 | 입력 뒤 맞춤 | 약 40 | `RouteInput`·`SyncEnvironmentHover`·`SyncActionButtons` |

- 주석이 약 314줄이다.
- 필드 39개가 수명 셋(서비스·씬·연결)에 섞여 있다. 연결 수명 필드 여덟은 연결이 없을 때 null이라 `_input?.`·`_surfaces?.` 같은 검사가 곳곳에 있다.
- 2026-10-25 대화에서 "나누면 약 500~600줄"이라고 했던 예상은 너무 낙관적이었다. 다시 재면 5·8·2만 옮겨서는 약 980줄이고, 연결 수명(3)까지 객체로 옮겨야 850줄 안팎이다. 필드는 39 → 약 18이다.

### 4.2 바꿈: 수명마다 객체 하나

| 수명 | 객체(새) | 갖는 것 | 만드는 때 |
|---|---|---|---|
| 서비스 | 조정자 `CombatViewSystem`(남음) | 모듈 수명·Tick, 서비스 받기, 진행 사실 호스트, 결과 소비, 입력 사실 조립, 입력 뒤 맞춤 | Factory |
| 서비스 | **입력 연결 `CombatInputConnection`** (W11-1) | Layer 서비스, 수신 창구, 버튼 다리, Layer 맞춤·해제 | `BindServices` |
| 씬 | **불러온 씬 `CombatLoadedScene`** (W11-3) | 씬 루트 찾기, 배경 세우기·해제 | 씬 로드(−195)에서 하나 |
| 연결 | **연결 `CombatViewConnection`** (W11-4) | 표시, 입력 표면, 연결 입력 묶음, 정보창 창구, 덱 호버, 씬 루트 구독, 첫 그리기, 전투 번호 | `TakeDisplay`의 표시 저장 자리, `Detach`에서 버림 |
| 연결 | **배치 진입 `CombatDeploymentEntry`** (W11-2) | 배치 확인창(현강님 `CombatDeploymentConfirmation` 그대로)과 그 콜백 넷 | 연결 입력 묶음과 함께 |

**W11-1 입력 연결** (`V/Input/CombatInputConnection.cs`)
- 수신 창구(`CombatInputReceiver`)를 만들고 갖는다. Layer 맞춤(`Apply(desired, cancelPresentation)`)과 해제(`Release(ref errors)`)를 한다.
- 버튼 다리 셋은 필드·람다 대신 수신 창구의 메서드를 그대로 넘긴다. 수신 창구가 `BindServices`에서 생기고 버튼 연결(`ContinueAttach`)은 언제나 그 뒤라 `?.`가 필요 없다.
- 시험 입구 `CombatViewSystem.Input`(시험 91곳이 씀)은 서명 그대로 이 객체의 수신 창구를 돌려준다.
- 동작은 같다. Unbind → ClearLayer 순서와, Layer가 바뀔 때만 표현을 정리하는 규칙을 그대로 옮긴다.

**W11-2 배치 진입** (`V/Input/ActionButtons/CombatDeploymentEntry.cs`)
- 배치 확인창과 콜백 넷(`CanEnterFromDeployment`·`SubmitDeploymentEntry`·`CancelDeploymentInputs`·`ShowMainPlacementNotice`)을 한 객체로 묶는다.
- 수명은 서비스 → 연결로 바꾼다(P23).
  - 확인창을 서비스 수명으로 둔 까닭은 수신 창구가 "확인창이 열렸나"를 묻기 때문이었다(C 단계 C-1). 그 물음은 연결이 없으면 언제나 거짓이라, 연결 수명이어도 답이 같다.
  - 콜백이 쓰는 입력 묶음·안내도 연결 수명이다. 그래서 같은 수명에 두면 `_input?.`가 없어진다.
- 확인창 클래스(현강님 파일)는 바꾸지 않는다.
- 동작은 같다. 진입 요청·응답·닫기·맞춤의 호출 시점을 그대로 둔다.

**W11-3 불러온 씬** (`V/CombatLoadedScene.cs`, 루트, Q-V22)
- 씬 루트 찾기(`FindObjectsByType`, 씬 오브젝트를 찾는 유일한 자리)와 배경 세우기·해제를 갖는다.
- 조정자는 −195에서 `Load`, Enter에서 `Root`, Exit·종료에서 `Release`를 부른다.
- `Attach`의 "같은 루트면 그 배경" 판정은 이 객체가 답한다(`BackdropFor(root)`).

**W11-4 연결** (`V/CombatViewConnection.cs`, 루트, Q-V22)
- 연결 하나의 상태를 갖는다. `TakeDisplay`가 표시를 저장하던 자리(소유권 이전, 마지막 실패 지점)에서 만들고, `Detach`가 정리 순서대로 비운 뒤 버린다.
- 정리 순서는 지금 `Detach`와 같다(P24).
  - 구독 해제 → 재생 끊기 → 입력 정지 → 입력 해제 → 참조 비우기 → 연결 상태 되돌리기 → 연출 끊기 → 표시 반환
  - 조정자가 순서를 부르고, 연결 객체는 자기 몫(구독·입력 묶음·덱 호버·HUD 아이템 해제 연결)만 한다.
- `IsAttached`·`CanContinue`·`IsSceneActive`의 "표시 연결"은 `_connection != null`이 된다.
- 씬 루트는 이 객체 하나가 갖는다. 입력 표면은 만들 때 루트를 받는다. 그래서 2라운드가 남긴 "씬 루트 두 필드"가 풀린다.
  - **구현 메모(V3-c, 결정 A — 10-25 사용자 "추천대로"):** 반대로 했다. 루트는 입력 표면 하나가 갖고(`CombatInputSurfaces.Root`), 연결 객체는 구독을 풀 때 표면의 루트를 읽는다. 표면은 지금처럼 구독 순간(`BindRoot`)에 루트를 받는다.
    - 까닭: 표면은 표시를 인수할 때 생기고, 루트는 첫 그리기가 끝난 뒤에 묶인다. 만들 때 루트를 받으면 첫 그리기 실패 때의 `Hide`·`Sync`가 아직 묶지 않은 루트 UI(BattleChanger·가방)를 건드려 지금 동작과 달라진다.
    - "두 필드"가 하나가 되는 것은 같다. 구독 해제가 표면의 묶기 규칙(루트가 있을 때만 묶음)에 기대는 점은 연결 객체 주석에 적었다.

**W11-5 입력 사실 조립은 조정자에 남긴다** (Q-V21)
- `ReadFacts`는 서비스 수명 사실(Buffer·진행 사실·재생)과 연결 수명 사실(첫 그리기·입력 묶음·확인창)을 함께 읽는다. 따로 빼면 그 읽기 계약이 진행 사실 호스트만큼 넓어진다.
- 연결 객체가 생기면 연결 쪽 사실은 그 객체에서 읽는다.

**결과:** 조정자는 약 850줄(주석 약 300줄 포함), 필드 약 18개다. 줄이 크게 줄지 않는 까닭은 남는 일(결과 소비·사실 조립·수명 순서)이 조정자 고유의 일이기 때문이다. 더 줄이려면 결과 소비를 재생 쪽으로 옮겨야 한다(§12, 이번 라운드 밖).
- **실제(V3-c 뒤, 검토 반영 포함):** 1159 → 1039줄, 필드 39 → 26, 메서드 56 → 46이다. 예상보다 약 190줄·필드 여덟이 많다.
  - 줄: 연결 정리 순서를 부르는 일이 조정자에 남았다(P24). "연결 수명" 구역은 315 → 295줄로만 줄었다. 검토 반영(재진입 판단·주석)으로 약 20줄이 늘었다.
  - 필드: 남은 여덟의 대부분은 입력 생성 재료다(`_textPresentation`·`_displayServices`·사실 공급자 셋·`_blockedInput`). V3-d의 재료 묶음(W12-1)이 줄인다.

## 5. W12 작은 정리 (R2-L, 기존 사본)

| # | 지금 | 바꿈 | 다른 사람 줄 |
|---|---|---|---|
| W12-1 | 14인자 입력 생성 서명이 세 벌이다(계약·어댑터·정적 Factory) | 서비스 수명 재료를 묶은 `CombatSceneInputSources` 하나. 서명은 `CreateSceneInput(display, sources)`. 의존 하나를 더하면 묶음 한 곳만 고친다 | 없음 |
| W12-2 | 배치 생성자가 쓰지 않는 `requests`를 검사만 한다(V2-a에서 남김) | 인자를 걷고 Factory 한 줄을 맞춘다 | `PlacementDragInteraction.cs:57`(현강님, 같은 생성자 서명) |
| W12-3 | 주체 막기 규칙이 두 벌이다(`CombatStepPlayer.MotionBlocks`·`CombatSubjectBlocking.Blocks`, 겹침 허용만 다름) | 공유 함수 하나에 겹침 허용 판정을 받게 하고 둘 다 부른다 | 없음 |
| W12-4 | 소리 표가 기차 판별(`gimmick.Activation is RailwayGimmickActivationSnapshot`)을 따로 한다 | 사건 형식 표의 경로(`RouteOf == Railway`)를 읽는다 | `CombatEventSounds.cs:113`(현강님) |
| W12-5 | 가시밭 프리팹이 없어도 DB가 통과하고 가시밭 있는 전투에서야 실패한다 | 가시밭 외형 표 검사에서 프리팹 필수(운영 에셋에는 이미 있음) | 없음(우리 표) |

남기는 것(P29): 경로 정지를 묶음 `_path = null`과 경로 `IsDetached` 두 사실로 아는 것(2라운드 TP-5), `CombatInputOwner`의 두꺼운 기반, 정적 Factory와 인스턴스 어댑터 두 입구(시험 18곳이 정적 Factory를 직접 부름).

**구현 메모(V3-d):**
- W12-3의 "겹침 허용 판정을 받게"는 함수의 셋째 인자(좁은 계약 `ICombatSubjectOverlap`)로 했다. 걸음 재생기만 넘기고, 반응·조각 실행은 넘기지 않는다(사건 완료 뒤 겹침 없음). 겹침 답이 유닛마다라 불리언으로 못 넘기고, 대리자는 매 질문 할당이 생기기 때문이다.
- W12-4는 표에 `IsRailway`를 두어 소리 표와 재생 조정이 함께 부른다.
- W12-5의 검사 자리는 Data `ThornFieldVisualData.Validate()`이고, 표가 부른다.

## 6. W8 공격 종류 전략 (V-M6·V-L20)

### 6.1 지금

종류 분기는 아래에 있다(DB 검사 둘은 따로).
- `CombatAttackPlayback`: 시작 분기, `TryBeginDirect`, `FireShot` switch, Ballista·곡사 시작 본문
- `CombatShotPath.TryTarget`
- `CombatAttackFacing.For`(목표점 해석으로)
- `CombatAttackTrajectory`: `Segment`·`TryResolveShot`
- `CombatAttackEventHandler`: Ballista 해제 입구
- `CombatUnitPresentationWindow`: 곡사·Ballista 무기 조회
- `CombatTurnEventHandler`: Ballista 재장전

외형 에셋별로 실제로 쓰이는 길은 이렇다(2026-10-25 에셋 대조).

| 종류 | 클립 길 | 코드 모션 길 |
|---|---|---|
| Melee | Demon·Heavy·StageBoss1·Cowboy | Gravekeeper |
| Linear | Akimbo·ShotGun | Sniper |
| Mortar | Cannon | CactusCannon(`CatusCannonWeaponView`) |
| Ballista | Ballista | **없음** |

Ballista 코드 모션 길(`CombatBallistaAnimation`·`BallistaWeaponView`·재장전)은 데이터로 닿지 않는다. `BallistaWeaponView`가 붙은 `EnemyUnit_Ballista.prefab`은 어디서도 참조하지 않는다.

### 6.2 바꿈: 종류마다 전략 클래스 하나

```text
V/Presentation/Attack/
  CombatAttackStyle.cs          추상 기본 — 종류 하나의 지식
  CombatMeleeStyle.cs · CombatLinearStyle.cs · CombatMortarStyle.cs · CombatBallistaStyle.cs
  CombatAttackStyles.cs         표 — Of(kind), 열거 값이 모두 있는지 만들 때 검사
  ICombatAttackStyleContext.cs  공격 재생이 전략에 내주는 좁은 맥락
```

| 멤버 | 지금 있는 곳 | 쓰는 곳 |
|---|---|---|
| `TryTarget(...)` 목표점 | `CombatShotPath.TryTarget` switch | 공격 재생·방향·궤적 |
| `Segment(...)` 궤적 출발·끝 | `CombatAttackTrajectory.Segment` | 궤적 |
| `TryResolveTrajectoryOrigin(...)` 궤적 출발 기준점 | `CombatAttackTrajectory.TryResolveShot` | 궤적 |
| `TryBeginCodeMotion(context, ...)` 코드 모션 시작 | `CombatAttackPlayback.TryBegin`·`TryBeginDirect`·`TryBeginMortar`·`TryBeginBallista` | 공격 재생 |
| `FireShot(context, ...)` 클립 발사 | `CombatAttackPlayback.FireShot` switch·`FirePiercingShot` | 공격 재생 |
| `ClipRelease(context, ...)` 클립 해제 입구 | `CombatAttackEventHandler`의 Ballista 갈래 | 공격 처리기 |
| `SupportsFacingMotion` 포효 정면 모션 | `TryBeginDirect`의 종류 검사 | 공격 재생 |

- 공격 재생에는 수명이 남는다: Gate·투사체 진행·Tick·취소, 접촉 진행률 계산, 이펙트. 전략은 좁은 맥락 `ICombatAttackStyleContext`로만 그것을 쓴다(공격 재생이 명시적 구현).
- `CombatShotPath`에는 종류와 무관한 기하(`Raise`·`LinearStart`·`ArcPoint`·`ArcHeight`)만 남는다.
- DB 쪽 종류 검사(`CombatAttackPresentationSO.Validate`·`CombatUnitAnimationRules.FiresProjectile`)는 DataBaseSystem이 View를 참조하지 않아 열거 기준으로 남는다(P26). 종류를 더할 때 DB 검사 한 곳은 따로 고친다.
- 동작은 같다. 각 갈래 본문을 그 전략으로 옮기기만 한다.

### 6.3 Ballista 코드 모션 길 (Q-V24)

권장(A)은 이렇다.
- 코드 모션 길을 걷는다: `CombatBallistaAnimation.cs`, 공격 재생의 Ballista 무기·재장전 상태, `TryBeginBallista`, 턴 처리기의 재장전 대기, 표시 계약 `BallistaWeapon`, 창구의 Ballista 무기 조회, 궤적의 무기 갈래.
- Ballista 클립 길은 "임시 예외"가 아니라 유일한 길이 된다.
- DB 검사가 "Ballista 공격 표시면 공격 클립 필수"를 본다(P27). 클립 없는 Ballista는 부트에서 실패한다. 지금은 공격 순간에 무기가 없어 실패한다.
- `BallistaWeaponView.cs`는 `EnemyUnit_Ballista.prefab`의 컴포넌트라, Missing Script를 만들지 않으려고 직렬화 칸만 남긴 빈 컴포넌트로 남긴다(손패 파일과 같은 처리). 시위·재장전 값 칸 넷(`stringTravel`·`stringDuration`·`stringOscillations`·`reloadDuration`)과 그 검사는 걷는다.
- 시험: Ballista 코드 길만 보던 우리 시험을 지운다(`CombatBallistaAttackTests`의 코드 길 사례, `CombatBallistaLifecycleTests`, `CombatBallistaAssetTests`의 시위·재장전 값 단언). 천명님 두 줄은 Q-T7.

## 7. W9 값은 DB로 (V-M9·V-M10·R1-L·V-L8)

### W9-1 화면 전환 설정과 폭발 이펙트 (민형님 코드, Q-V25)

- View DB 설정 표 `CombatPresentationSettings`(우리 SO)에 칸을 더한다.
  - 화면 전환: 아침·밤 레이어 이름, 카메라 이름, 색, 바뀌는 속도·양
  - 환경 폭발 이펙트
- 기본값은 지금 상수다. 그래서 에셋을 고치지 않아도 값이 같다(P28).
- `CombatPhaseTransitionSettings.CreateDefault()`(민형님)를 `From(settings, findLayer)`로 바꾼다. 레이어가 없으면 예외인 지금 검사는 그대로다.
- 카메라 이름은 표시 Factory가 배경의 맵 카메라 사실(`MapCameras`)에 있는지 검사한다(배경이 있을 때). 지금은 이름이 틀려도 전환이 조용히 엉뚱한 카메라에 걸리거나 안 걸린다.
- 폭발 이펙트는 표시 Factory가 설정 값을 넘긴다. 종류 검사는 `CombatPresentationEffectRules` 하나다.
- 민형님 시험 `CombatPhaseTransitionPresenterTests.cs:33·276`이 `CreateDefault`를 부른다(Q-T7).

### W9-2 소리 프리셋 (Q-V26)

- 유닛 외형 데이터(`UnitVisualDataSO`, 우리 SO 71/75줄)에 칸 둘을 더한다: 유닛 소리 프리셋, 공격 소리 프리셋(없음 = −1).
- 설정 표에는 적 공용 유닛 프리셋(5)과 배경음 프리셋 셋(밤 3·낮 2·완료 뒤 1)을 둔다.
- 새 값 객체 `CombatSoundPresets`(`V/Playback/Support/`)가 표에서 프리셋을 고른다. 소리 표·사건 소리·입력 둘(공격 조준·배치 Drag)이 이것을 받는다.
  - `CombatEventAudio.UnitPreset`·`AttackPreset`(정적 switch)과 `CombatEventSounds.BgmPreset`(상수)는 지운다.
- 포효 소리는 포효한 유닛의 공격 프리셋이다. 지금 데이터에서는 보스만 포효해 값이 같다.
- 외형 에셋 열넷에 지금 switch와 같은 값을 넣는다. 유닛 프리셋은 유닛 분류(InGame DB)에서 나오던 값이라 키마다 지금 분류로 계산해 넣는다(계획 V3-h의 표).
- 시험: 프리셋 값을 정적 함수로 단언하는 다른 사람 시험이 닿는다(Q-T7).
  - 현강님 `GameAudioTests`(185줄)
  - 호진님 `StageMapCombatEntryBootSmokeTests.cs:129-138`
  - 현강님 줄이 든 `CombatEventSoundsTests`
  - 확인 내용(어느 유닛이 어느 프리셋인가)은 그대로 두고, 값을 DB에서 읽게만 맞춘다.

### W9-3 사건 Presenter 필수 묶음 (V-L8, P17)

- 선택 인자 약 20개를 필수 묶음 셋으로 바꾼다.

| 묶음 | 담는 것 | 만드는 곳 |
|---|---|---|
| `CombatEventPresentationValues` | 걸음 시간·적 간격 둘·이동 회전 시간·능력 연출 값·카메라 연출 값·걸음 뜀 높이 | 설정 표 `From` |
| 서비스 | 오디오·카메라 창구·이펙트 창구·투사체 루트 | 표시 Factory |
| `CombatFieldEventPorts` | 가시 소비·환경 효과·재생 강조·가시/오브젝트 몸·오브젝트 숨김·토네이도 숨김/켜기 | 표시 Factory(필드 표시들의 메서드) |

- 처리기·공격 재생·사건 소리의 `effects = null`·`audio == null` 갈래도 함께 걷는다.
- 시험 조립 열셋(우리 열하나, 현강님 `CombatSceneEventPresenterTests.cs:270·515`)은 우리 시험 도우미가 만든 묶음을 넘긴다(Q-T7).

**구현 메모(V3-g~V3-i, 실행 기록은 계획 "Phase V3-g~V3-i"·"W9 검토"):**
- W9-1: "이름이 틀리면 조용히 안 걸림"은 틀렸다. 이전에도 첫 전환 때 카메라 카탈로그 조회에서 실패했고, 이제 표시를 만들 때 이름과 함께 알린다. 보스 스테이지 배경(`Map.prefab`)에는 전환 카메라가 없어 진입이 실패한다(사용자 결정: 임시 세팅이라 그대로).
- W9-2
  - 포효 소리는 포효한 유닛(`BossId`)의 공격 프리셋이다. 보인 사실에 그 유닛이 없으면 소리가 없다.
  - 입력 둘(배치 Drag·공격 조준)은 프리셋 객체 대신 **입력 소리 창구 `Input/Interaction/CombatInputSounds`**(오디오 + 프리셋)를 받는다. 처음 구현의 "오디오가 있으면 프리셋도 필수" 짝이 두 입력에 하나씩 있어 검토 중간으로 잡혔고, 사용자 결정 "작은 객체 하나로 묶음"으로 바꿨다. "0 이상이면 소리"는 `CombatSoundPresets.IsSound` 하나다.
- W9-3
  - 사건 Presenter 생성자는 위 셋에 소리 프리셋 인자 하나가 더해진 여덟이다(`…, audio, sounds, camera, …`). Presenter가 사건 소리를 직접 만들어서다. 처음에는 값 묶음에 넣었다가, 검토의 "값과 표 조회는 책임이 다름"을 받아 사용자 결정 "생성자 별도 인자로"로 옮겼다.
  - 값 묶음 `CombatEventPresentationValues`는 폴더 배치 안내 §2대로 `Playback/Support/`다(위 표의 자리 대신).
  - 시험 도우미 `FieldPorts`의 걸음 접촉 몸 조회는 예외를 던진다(그 조립의 시험은 걸음 접촉을 만들지 않음). 옛 조립의 "몸 없음 → 보간 끝 해제"와 다르지만 닿는 시험이 없다.

## 8. 정할 것

권장은 모두 A다.

| # | 묻는 것 | A(권장) | B | C |
|---|---|---|---|---|
| **Q-V19** | 시작 전 2라운드 커밋 | 사용자가 2라운드를 커밋한 뒤 시작한다(A/B 기준 판) | 커밋 없이 이어서 함(기준은 스크래치 사본) | — |
| **Q-V20** | 조정자 나누기 범위 | 수명별 넷(입력 연결·배치 진입·불러온 씬·연결 객체), 약 850줄·필드 약 18 | 작은 셋만(입력 연결·배치 진입·불러온 씬), 약 980줄·필드 약 30 | A + 결과 소비도 재생 쪽으로(위험 큼) |
| **Q-V21** | 입력 사실 조립 | 조정자에 남김(연결 쪽 사실은 연결 객체에서 읽음) | 읽기 계약을 따로 둔 객체로 뺌 | — |
| **Q-V22** | 새 파일 둘을 루트에 둠 | `CombatLoadedScene`·`CombatViewConnection`을 `V/` 루트에(배치 안내 §0의 "조정자·씬 연결·결과 수명") | `Input/`·`Display/`로 나눔 | — |
| **Q-V23** | 공격 종류 전략의 꼴 | 종류별 클래스 넷 + 표 + 좁은 맥락(§6.2) | 기하(목표점·궤적)만 표로, 시작·발사는 공격 재생에 남김 | — |
| **Q-V24** | Ballista 코드 모션 길 | 걷고, `BallistaWeaponView.cs`는 빈 컴포넌트로 남김(Missing Script 없음) | 걷고 파일도 지움(참조 없는 프리팹에 Missing Script가 생김) | 전략으로 옮겨 남김(불필요 중간 그대로) |
| **Q-V25** | 화면 전환 설정(민형님 코드) | 값은 설정 표, 카메라 이름은 맵 카메라 사실로 검사, 민형님 공유 | 그대로(V-M9 낮음 남음) | — |
| **Q-V26** | 소리 프리셋 | 외형 칸 + 설정 표 + 값 객체, 다른 사람 시험은 DB 값으로(Q-T7) | 포효 소리만 포효한 유닛 키로(V-M10 중간 남음) | — |
| **Q-V27** | Presenter 선택 인자 | 필수 묶음 셋(W9-3) | 3라운드 밖으로 미룸 | — |
| **Q-V28** | Phase 검토 | Phase마다 새 에이전트 하나, W11-4·W8은 셋(큰 Phase) | 모두 하나 | — |
| **Q-T7** | 다른 사람 시험(계획 "결정 대기" 표) | 허락(확인 내용 그대로, 호출·입력만) | 고치지 않고 그 Phase 범위를 줄임 | — |

## 9. 설계에서 정한 것 (확인 대상)

- **P18** 새 파일 위치(배치 안내 따름, 루트 둘은 Q-V22)
  - `V/Input/CombatInputConnection.cs`
  - `V/Input/ActionButtons/CombatDeploymentEntry.cs`
  - `V/CombatLoadedScene.cs`·`V/CombatViewConnection.cs`
  - `V/Input/CombatSceneInputSources.cs`
  - `V/Presentation/Attack/` 아래 전략 일곱
  - `V/Playback/Support/CombatSoundPresets.cs`
  - `V/Playback/CombatEventPresentationValues.cs`·`CombatFieldEventPorts.cs`
- **P19** 새 하위 폴더는 만들지 않는다. 전략은 `Presentation/Attack/`에 둔다.
- **P20** 표(강조 층·공격 종류)는 만들 때 열거 값이 빠짐없이 한 번씩 있는지 검사한다. 사건 형식 표와 같은 규칙이다.
- **P21** 호버 색조 → 층 대응은 층 표에 넣지 않는다(입력 쪽 사실).
- **P22** 시험 입구 `CombatViewSystem.Input`·`AttachDisplay`·`AttachCreated`·`RequestReturn`은 서명 그대로 둔다(시험 91곳 이상). 2라운드 TP-17의 시험 입구 null 허용도 그대로다.
- **P23** 배치 확인창은 연결 수명이다(§4.2 W11-2).
- **P24** 연결 정리 순서는 지금 `Detach`·`StopInput`과 같다. 계획에 전후 순서 표를 둔다.
- **P25** 연결 객체는 조정자 안에서만 쓴다(`internal`, 서비스 계약에 내지 않음).
- **P26** DB 쪽 종류 검사는 열거 기준으로 남긴다(DataBaseSystem → ViewSystem 참조 없음).
- **P27** Ballista는 공격 클립이 필수다(Q-V24 A일 때). 검사는 `CombatUnitAnimationRules` 한 곳이다.
- **P28** 새 설정 칸의 기본값은 지금 상수와 같다. 그래서 설정 에셋을 고치지 않아도 값이 같다. 외형 에셋의 소리 프리셋만 값을 넣는다.
- **P29** 남기는 낮음: 경로 정지 두 사실, `CombatInputOwner` 기반, 생성 입구 둘, 시험 입구 null 허용, 남긴 손패 파일, 빈 `BallistaWeaponView`.
- **P30** 다른 사람 운영 줄은 공유 메모에 빠짐없이 적는다(2라운드 TP-18과 같음). 문서 Phase는 V3-j다.

## 10. 원칙 점검과 예상

**알려진 문제 기준 예상(2라운드 측정 M 8.7에서):**

| 기준 | 지금 | 3라운드 뒤 | 바뀌는 것 |
|---|---|---|---|
| SSOT | 9.0 | 9.0 | 낮음 일곱 → 넷(씬 루트 두 필드·주체 막기·레일웨이 판별이 풀림). 상한에 걸려 그대로 |
| SOLID | 7.5 | **9.0** | 중간 둘(V-M6·강조 층) 해결. 낮음은 상한 그대로 |
| FW 계약 | 9.75 | 9.75 | V-L9(`CombatEndUI.GetUIData`) 남음 |
| 있는 기능 | 9.5 | **10.0** | V-M9·`Boom` 고정 해결 |
| 독립성 | 8.25 | **9.25** | V-M10 해결. 낮음 넷 → 셋(14인자 서명 풀림) |
| 불필요·우회 | 8.25 | **9.0** | Ballista 두 경로 해결. 낮음은 상한 그대로 |
| **합** | **8.7** | **약 9.33** | |

- **새로 생길 수 있는 것:**
  - 전략 기반 클래스·맥락 계약이 두꺼워질 수 있다(SOLID 낮음).
  - 빈 `BallistaWeaponView`(불필요 낮음, 손패 파일과 같은 꼴).
  - 연결 객체와 조정자 사이의 양방향 호출(SOLID 낮음).
  - 이것들을 넣으면 실제는 약 9.1~9.2로 본다(지난 차이를 생각해).
- **조정자:** 점수 몫은 거의 없다(SOLID 낮음 상한). 필드 39 → 약 18, 줄 1159 → 약 850은 읽기·변경 범위의 개선이다.
  - V3-c 뒤 실제는 1039줄·필드 26이다(§4.2 "실제"). V3-d의 재료 묶음 뒤 필드는 약 20으로 본다. 점수 예상은 바꾸지 않는다(낮음 상한).

## 11. 다른 사람 줄과 공유 (V3-j, 모두 미전달로 작성)

| 사람 | 바꾸는 줄(blame, 지금 작업 트리) | 묶음 |
|---|---|---|
| **현강님** | 조정자의 배치 확인창 줄(`_textPresentation`·`_deploymentConfirmation` 필드, 생성 `:264-265`, `Close` 셋, `Sync`, `RouteInput` 진입 갈래 `:1090-1095`, 콜백 넷 `:1132-1157`)과 덱 호버 줄(필드·서비스 멤버 `:119-120`, `:520-521`·`:739-740`)을 연결 객체·배치 진입으로 옮김. `PlacementDragInteraction.cs:57`(생성자 서명). `CombatEventSounds.cs:113-116`(기차 판별). `CombatEventAudio`·`CombatEventSounds`의 프리셋 줄(W9-2). 시험 `GameAudioTests`·`CombatEventSoundsTests`·`CombatSceneEventPresenterTests:270·515`(Q-T7) | W11·W12·W9 |
| **호진님** | 시험 `HojinCombatAttackDragIntegrationTests.cs:130`(조정자 `_display` 리플렉션 → 연결 객체, Q-T7. `:129`는 우리 줄), `StageMapCombatEntryBootSmokeTests.cs:129-138`(프리셋, Q-T7), `CombatSceneEventPresenter`의 호진님 줄 여섯(W9-3, 구현 때 blame으로 확정) | W11·W9 |
| **천명님** | 조정자 `_layers`(`:100`)·`_battleInstance`(`:85`)와 Layer·입력 해제 메서드의 중괄호 줄(`:856`·`:873`·`:910`·`:915`·`:922`·`:1130`)을 입력 연결·연결 객체로 옮김. `CombatEventAudio`의 천명님 줄(W9-2). 시험 `CombatBallistaAttackTests`의 두 줄(Q-V24 A일 때, Q-T7 — V3-e에서 고정물로 남아 글자 그대로라 바꾸지 않음). Core 전달 문서 §35(요청 없음) | W11·W8·W9 |
| **민형님** | `CombatPhaseTransitionSettings.cs`(42줄, W9-1), `CombatPhaseTransitionPresenter.cs`의 설정 받는 줄, 시험 `CombatPhaseTransitionPresenterTests.cs:33·276`(Q-T7). Factory 공유 §25(표시 Factory 줄·연결 줄) | W9 |

- 줄 번호는 지금 작업 트리 기준이다. 사용자 커밋 뒤 각 Phase 시작 때 blame으로 다시 센다.
- **작성 완료(V3-j, 2026-10-25, 모두 미전달):** 현강님 메모 §10, 호진님 메모 §12, 천명님 Core 전달 문서 §35, 민형님 Factory 공유 §25. 실제로 바뀐 줄은 위 표와 다르다(구현 때 blame으로 확정, 각 메모가 원본).
  - 더해진 것: 현강님 입력 소리 줄(`PlacementDragInteraction`·`AttackTargetingInteraction`), 현강님 시험 `CombatSceneEventPresenterTests:529`·`CombatInteractionTests:76`(사용자 결정·허락), 천명님 시험 `GameAudioTests:52·56`, 민형님 `FieldOutlineRenderer.cs:428` 제안.
  - 빠진 것: 민형님 `CombatPhaseTransitionPresenter.cs`(설정을 받는 줄은 그대로라 바꾸지 않음), 천명님 `CombatBallistaAttackTests` 두 줄(글자 그대로), 현강님 `GameAudioTests:80`(그대로, 바뀐 `:81`은 우리 줄).

## 12. 이번 라운드 밖

- **결과 소비를 재생 쪽으로(Q-V20 C):** 조정자를 600줄대로 줄이려면 필요하다. 결과 확인 순서(K1·K14)가 걸려 있어 따로 설계한다.
- **남기는 낮음:**
  - V-L2: 결과 화면이 판정을 다시 계산함(현강님·호진님 파일)
  - 엔딩 부모 조건 사본
  - 위험 종류 목록 세 곳(현강님 `CombatEnvironmentText` 포함)
  - 배경 요청 순서
  - `"InvalidMainCharacter"` 문자열
  - V-L18: 시험 전용 멤버
  - 빈 선택 참조 둘
  - `lastHit`
  - V-L9 `GetUIData`
  - V-L11·V-L12
- **손패 파일:** 씬 `Hand`의 `HandPresenter` 컴포넌트를 뗄지는 사용자 결정 대기다(1라운드 Q-V5).

## 13. 3라운드 측정 (2026-10-25, V3-k)

**방법:** 2라운드 측정([2라운드 설계 §11](2026-10-24-combat-view-stage-round2-design.md))과 같다.
- **범위:** `ViewSystem/Combat/` 전체, `CombatView` Buffer, `CombatEndUI`, 전투 View Factory 넷, `Combat.unity`다. W9가 바꾼 View DB 데이터(외형·설정 칸)는 범위 밖 기반이라 두 판이 같은 것을 읽었다.
- **두 판:** 스크래치패드 `view-ab3/`의 이름 없는 폴더 둘(S·T)로 꺼냈다(S 260개, T 275개). 배정은 무작위였고, 결과는 T = 3라운드 뒤 작업 트리, S = 기준 커밋 `35562e96`이다.
- **평가자:** 하위 에이전트 셋(SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회). 지시문은 2라운드 것에서 판 이름·파일 수·범위 한 줄만 바꿨다. 어느 쪽이 새 판인지 알리지 않았고, 우리 문서·메모리·시험·git 기록은 읽지 못하게 했다.
- **감점:** 높음 −2, 중간 −0.75, 낮음 −0.25, 낮음은 기준마다 −1.0까지.
- **합치기:** 같은 문제는 한 기준에서만 셌다. 기준선·2라운드 측정에서 둔 기준을 따랐다.
  - 강조 층 다섯 곳(SSOT·SOLID가 셈) → SOLID
  - 공격 종류 분기 V-M6(독립성·SOLID) → SOLID
  - Ballista 두 경로 V-L20(SSOT·불필요) → 불필요
  - 소리 프리셋 V-M10(독립성·있는 기능) → 독립성
  - 화면 전환·`Boom` V-M9(독립성·있는 기능) → 있는 기능
  - 사건 Presenter의 null 허용 의존 V-L8(독립성·SOLID·불필요) → 불필요
- **직접 대조:** 중간 이상 주장을 코드로 대조했다. 모두 맞았다.
  - S의 높음 하나(가시밭이 보이지 않음)는 3라운드 Phase가 아니라 **사용자가 직접 고친 것**(2026-10-25 "가시밭 프리팹이 안생기네" → "문제 찾았으니까…")이 T에 들어 있어서 T에만 고쳐져 있다. `FieldThornPresenter.cs:25` 레이어 2 → 20, `Prefabs/Combat/*.prefab`의 레이어 20이다.
  - 두 판 공통의 새 중간(칸 높이 함수 선택)은 곡사 목표점(`CombatMortarStyle`)과 가시밭 배치가 바닥 함수 `CellCenterToWorld`를 쓰는 것으로 확인했다.

**점수(합친 뒤):**

| 기준 | S(`35562e96`) | T(3라운드 뒤) | 계획 §0 예상(T) |
|---|---|---|---|
| SSOT | 9.0 | **9.0** | 9.0 |
| SOLID | 6.75 | **8.25** | 9.0 |
| FW 계약 | 9.75 | **9.75** | 9.75 |
| 있는 기능 사용 | 8.75 | **9.25** | 10.0 |
| 독립성 | 7.5 | **8.25** | 9.25 |
| 불필요·우회 | 4.75 | **8.25** | 9.0 |
| **합** | **7.75** | **8.8** | 9.33 |

- 평가자 원점수(합치기 전)는 S 6.9 / T 8.8이다. T는 겹쳐 센 것이 없어 같다.
- **S는 2라운드 측정의 M(8.7)과 같은 코드인데 7.75로 0.95 낮다.** 평가자가 새로 찾거나 올린 것 때문이다.
  - 불필요 높음 하나: 가시밭 레이어(S만, 위 대조). −2
  - 공통 중간 셋: 조정자(낮음 → 중간, SOLID), 입력 조립의 시험용 null 허용(불필요), 칸 높이 함수 선택(독립성, 새로 찾음)
  - 화면 전환 값이 낮음 → 중간으로 올랐다(있는 기능).
- **T는 예상보다 0.54 낮다.** 두 판 공통의 새 중간 셋(SOLID·불필요·독립성 각 −0.75)이 0.375를 깎았다. 있는 기능의 새 낮음 셋(가시밭 레이어 숫자, HUD Canvas 고정 값·`Camera.main` 대체, 연출 시간 일부 코드 고정)이 0.125를 깎았다. 이 둘을 빼면 약 9.3으로, 알려진 문제 기준 예상(9.33)과 맞는다.
- **판 차이 T − S = +1.04**(1라운드 +0.9, 2라운드 +0.6).

**기준선 문제의 상태(T):**
- **해결:**
  - 강조 층 다섯 곳(SOLID 중간, W10)
  - V-M6 공격 종류 분기(SOLID 중간, W8)
  - Ballista 두 경로 V-L20(불필요 중간, V3-e)
  - V-M10 소리 프리셋(독립성 중간, W9-2)
  - V-M9 화면 전환·`Boom`(있는 기능, W9-1)
  - V-L8 사건 Presenter null 허용(불필요, W9-3). 안쪽 조립 함수에는 낮음으로 남았다(아래).
  - 2라운드에 우리 변경이 남긴 낮음: 씬 루트 두 필드, 14인자 서명 세 벌, 배치 `requests` 인자
  - 2라운드에 새로 찾은 낮음: 주체 막기 사본, 기차 판별, 가시밭 프리팹 검사
- **남음:**
  - 조정자(SOLID 중간, 1057줄. 수명 셋·결과 소비·사실 조립·입력 분배·버튼 동기화가 한 클래스, 재진입 검사가 콜백마다)
  - 입력 조립의 시험용 null 허용(불필요 중간): `CreateInteraction`의 선택 협력자 일곱, 묶음 `CombatInteraction`의 위임 생략, `AttachCreated`의 루트·정보창, 이펙트 없는 카메라 창구, 확인창 `as` 캐스트
  - 낮음: 결과 화면의 포기·최종 승리 판정, 엔딩 부모 조건 사본, `lastHit`, `attackActiveIndicator`, 손패 파일, 시험 전용 멤버
- **새로 찾음(기존 코드, 두 판 모두):**
  - 중간: 칸의 월드 위치를 바닥 함수와 표면 함수 중 무엇으로 쓸지 호출자가 고른다. 곡사 착탄·관통 종점·가시밭은 높은 칸에서도 바닥에 놓인다(독립성).
  - 낮음: 강조 색 하나에 다섯 곳(팔레트), 표시 정리 목록 네 벌, 배경음 절차 세 곳, 대기 명령 사실 두 경로, 입력 정리 순서 세 곳, UI가 `GetUIData` 대신 Factory `Configure`로 표를 받음(FW), 큰 클래스들(`CombatPathInput` 808줄 등), 사건 Presenter의 생성자 안 조립
- **새로 생김(우리 변경, T만, 모두 낮음):**
  - 적 전용 유닛의 외형 유닛 프리셋이 읽히지 않음(W9 검토가 남긴 것)
  - 입력 생성 재료 캐시를 두 입구가 따로 무효화함(W12-1)
  - 원거리 여부를 DB 규칙 `FiresProjectile`과 전략 `FireShot` 기본값이 따로 앎(W8 검토가 남긴 것)
  - 전략 맥락의 종류별 멤버, 전략 API가 구체 창구를 받음(W8)
  - 이동 경로 테두리 색이 층 표 밖(민형님 줄, V3-j 제안)
  - 빈 `BallistaWeaponView`, 운영에서 쓰지 않는 `Fired` 완료 정책·`Instant`·`None`
  - 위층은 필수인데 안쪽 조립 함수·걸음 묶음에 null 허용이 남음
- **사용자 변경(3라운드 Phase 밖):** 가시밭 레이어 20이 S의 높음을 풀었다. 다만 상수 이름 `IgnoreRaycastLayer`와 값(BothRendering 레이어)이 어긋나고 레이어 번호를 직접 쓴다(T 낮음 하나, 평가자 셋이 모두 짚음).

**커밋에 주는 것:** V3-f 기록의 "커밋에서 뺄 것: `Prefabs/Combat/*.prefab` 레이어 변경"은 "W8 변경이 아니다"라는 뜻이었다. 이 프리팹 변경과 `FieldThornPresenter.cs`의 레이어 20은 사용자의 가시밭 수정이라, 함께 넣어야 가시밭이 보인다(커밋은 사용자 결정).

**다음 라운드 후보:**
- 칸 높이: 표면 기준 배치 규칙 하나(곡사 착탄·관통 종점·가시밭)
- 입력 조립의 시험용 null 허용 걷기(사건 Presenter와 같은 꼴, 안쪽 조립 함수 포함)
- 조정자의 결과 소비·사실 조립 나누기
- 가시밭 레이어를 이름으로(`NameToLayer`) 또는 프리팹 레이어 그대로
