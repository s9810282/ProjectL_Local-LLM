# 전투 유닛 애니메이션 클립·이벤트 기반 계획 (AC-1~AC-5)

- **2026-10-19 AC-5 추가:** AC-1~AC-4 구현·Unity 확인 완료(§8.5). 원거리(곡사·직선) 클립 공격 AC-5의 Task 계획은 §9(설계 5차 §10, 결정 Q-AC13 B·Q-AC14~Q-AC17 A). 코드 착수 전, 계획에서 정한 P9~P13 확인 대기. **7기준 구조 검사(설계 §11, 8.3/10) 뒤 Q-AC18 A·Q-AC19 A·K3·K16·K17 반영 결정(2026-10-19) → §9.0b·AC-5b Task 고침. AC-5a 코드 완료(§9.1, 컴파일·Unity 확인 대기). 다음 AC-5b.**

- 설계: [unit-animation-clip-design](../specs/2026-10-19-combat-unit-animation-clip-design.md) **4차**(AC1~AC10, Idle 바닥 층). 3차 결정 Q-AC1~Q-AC8 모두 A, 4차 결정(컨트롤러는 Idle만·바로 갈아 끼움)은 사용자 설명.
- 작성: 2026-10-19. 사용자 지시: "설계 3차 기준으로 AC-1~AC-4 Task 계획 작성해줘", 이어서 "오케이 그러면 이거 기반으로 다시 설계해줄래, 카메라 작업도 거의 마무리 단계야"(4차 반영, §0.5). AC-5(원거리 발사 조각)는 Q-AC7 A대로 첫 원거리 클립이 들어온 뒤 따로 계획한다.
- 상태: **계획 4차 반영, 결정 완료(설계 Q-AC9 = §0.1 P1 A, Q-AC10 A, 2026-10-19), 코드 착수 전. 카메라 C6(실행 대여 묶음 분리)이 커밋돼(`3c50a379`) 다시 점검했다(§0.7, 9.0). Q-AC11 A 결정(2026-10-19, AC-2 첫 Task T10a로 표시 대여 칸 부품, 목표 9.3). AC-0 건너뜀(사용자). **AC-1 코드 완료(2026-10-19, §8.1) + 하위 에이전트 3개 원칙 검토·보완(§8.2, 8.0 → 9.3): MSBuild 오류 0, 커서 시험 13 PASS, Unity 확인 대기.** Q-AC12 A(무기는 한 테이크로, §0.8). AC-2 착수 조건: AC-1 Unity 확인·커밋, 폴더 정리 Q-M2 순서(궤적·경로 입력 조건은 풀림).**
- 경로 약어:
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `CD/` = `C/Display/`
  - `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`
  - `UV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Unit/Item/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
- 위치는 메서드 이름으로 적는다. 새 형식 이름은 이 계획에서 확정한 이름이다(설계의 "가칭"을 대신한다).

## 0. 착수 전

### 0.1 계획에서 찾은 설계 보정 — 결정 완료(P1 = 설계 Q-AC9, A, 2026-10-19)

- **P1. 이벤트 컴포넌트를 어느 층에 둘까**
  - 확인한 사실:
    - `DataBaseSystem`은 `ViewSystem`을 한 곳도 참조하지 않는다(코드 검색 0건). View → DB 방향만 있다.
    - `DataBaseSystem`에는 MonoBehaviour가 하나도 없다. 프리팹 검사(`LobbyVisualPrefabRules`·`StageMapViewDataSO` 등)는 Unity 기본 형식(Renderer·Collider 등)만 본다.
    - 기차의 바인딩 검사(`RailwayTrainVisualBinding.Validate`)는 런타임 설치 때 View 쪽에서 한다.
  - 설계 AC1·AC2는 DB 검사(`UnitVisualData.Validate`)가 View 쪽 컴포넌트(`CombatAnimationEvents`)를 찾고, DB가 그 함수 이름을 `nameof`로 읽게 되어 있다. 이대로 만들면 DB → View 참조가 처음 생긴다(평가 E12에서 놓친 점).
  - **A(권장):** 이벤트 컴포넌트를 "입력 계층 부품"으로 `DV/`에 둔다.
    - 프리팹에 붙이는 작업 입력이라는 점에서 SO와 같은 층이다. 함수 셋은 비어 있는 이름표라 런타임 동작이 없다(AC2).
    - 함수 이름(`nameof`)·이벤트 읽기·검사가 DB 쪽 한 곳에 모인다. View 런타임(클립 채널)은 DB 쪽 형식을 읽기만 해, 지금의 참조 방향과 같다.
    - 설계대로 부트의 DB 생성에서 실패한다.
    - `DataBaseSystem`의 첫 MonoBehaviour가 된다. 호진님 공유 메모에 적는다.
  - B: 컴포넌트를 `CD/`에 두고, 이벤트 읽기·검사를 View 쪽 유닛 표시 준비 때로 옮긴다. 층 규칙은 그대로지만, 부트가 아니라 그 유닛이 전투에 처음 나올 때 실패한다. 실제 카탈로그 에셋 시험이 부트 전 확인을 대신한다.
  - C: 함수 이름을 DB 상수와 컴포넌트 함수 두 곳에 두고 시험으로 맞춘다. 원본이 둘이 된다(SSOT 약함).
- **결정(2026-10-19, 사용자 "Q-AC9(컴포넌트는 입력 계층)과 Q-AC10(…) 둘 다 A로 정해"): A.** 아래 Task는 A 기준이다.

### 0.2 계획에서 정한 것 — 설계 뜻은 그대로(보고)

- **P2. 칸 열거형을 만들지 않는다.** 평가 E6으로 공격 칸(선딜레이 있음)과 피격 칸의 값 형식이 달라졌다. 그래서 열거형 + 한 목록 대신 이름 있는 두 칸(공격 항목·피격 항목)을 둔다. 사망 칸을 더할 때는 "필드 하나 + 읽기 하나 + 조립 한 줄"이다.
- **P3. 휴식 자세는 Transform 값을 기억했다가 되돌린다.** (4차: 컨트롤러가 없는 유닛에만 쓴다. 컨트롤러가 있으면 Idle로 돌아간다, 설계 AC9.) 설계 §5는 "Animator 기본값 쓰기 또는 다시 묶기"를 Unity 확인 항목으로 두었다. 계획은 Unity의 의미에 기대지 않는다. 준비 때 Animator 아래 Transform의 로컬 위치·회전·크기를 한 번 기억하고, 반환 때 되돌린다.
  - 대가: Transform 밖의 속성 곡선(재질·블렌드셰이프 등)은 되돌리지 않는다. 그래서 "전투 클립은 Transform만 움직인다"를 작업 안내에 적는다.
- **P4. 표시가 꺼진 공격자의 클립 공격은 예외다(평가 E15).** 코드 길은 자세 대여에서 예외가 난다. 클립 길도 조립을 시작하지 못하면 예외로 맞춘다. 피격 처리기의 "피격 조립을 시작하지 못했습니다" 예외와 같은 규칙이다.
- **P5. AC-4까지 공격 칸은 Melee만 허용한다.** 발사 조각(AC-5)이 없는데 Linear에 공격 칸을 넣으면, 이벤트 타이밍에 투사체 없이 바로 해제된다. 그래서 DB 검사를 Melee만으로 시작하고, AC-5에서 Linear를 더한다(설계 AC1에 반영됨). 규칙의 원본은 `CombatUnitAnimationRules.CanUseAttackClip` 하나다(§8.2).
- **P6. 선딜레이는 지금 쓰는 둘만 둔다.** 피격(해제) 선딜레이와 소리 선딜레이다. 이펙트·발사 선딜레이는 쓰는 조각이 생길 때 더한다(설계 AC8 표의 나머지 값). 쓰는 곳 없는 입력 칸을 미리 만들지 않는다(지침 §27).
- **P7. 신호 이름은 `ClipStart`·`ClipEvent`다.** 컴포넌트 함수 이름(`ClipStart()`·`ClipEvent()`)과 맞춰, 클립 조각만 내는 신호라는 것이 이름에 드러나게 한다. `End`는 그대로이고, 카메라 K4의 `Hit`은 카메라 K-2가 더한다. 열거 값의 번호는 들어온 순서대로 붙이고, 시험은 번호에 기대지 않는다(평가 G6).
- **P8. 구현 방식:** 최근 Phase처럼 직접 구현(워크시트 없음)으로 적었다. 실습 워크시트가 필요하면 착수 지시 때 말씀해 주시면 된다.

### 0.3 착수 조건과 기준선 (AC-0, 코드 없음)

1. ~~P1 결정.~~ **완료(2026-10-19, A).**
2. ~~가방 작업 커밋.~~ **완료.** 가방 작업(P-1~P-4)은 Unity 확인 뒤 커밋됐다(2026-10-19 확인, `PackItemSlotView.cs` 등 추적·변경 없음).
3. **기준 커밋:** 착수 때의 HEAD. 2026-10-19 확인 때는 `927962fb`이고, 이 계획이 고칠 파일에 커밋 전 변경이 없었다.
   - **2026-10-19 재점검(§0.7):** HEAD `3c50a379`(카메라 구조 개선 = C6). 이 계획이 고칠 파일에는 여전히 커밋 전 변경이 없다. 작업 트리에는 호버 U-1·U-2(Core 미리보기·Buffer·입력 파일과 새 파일)가 커밋 전으로 있어 기준선·MSBuild에 섞인다(CC6).
   - **2026-10-19 Q-AC11 결정 때 다시 확인:** 호버는 `950a4b34`로 커밋됐다(CC6의 그 부분은 풀림). 대신 작업 트리에 다른 세션의 진행 중 변경이 있다 — 경로 입력 분리(`CombatPathInput`·`CombatInteraction`·`CombatInputRouter`·`CombatReturnRequest`·`CombatMovementRangeRequest`·`ICombatMovementFeedback` + 새 파일 넷)와 궤적 TJ-1(`CombatShotPath`·`CombatAttackTarget`·발사 애니메이션 셋·`CombatProjectilePool`), `Combat.unity`. AC-1·AC-2가 고칠 파일과는 겹치지 않는다. 그래서 AC-0 기준선·Phase 검증은 대여 분리·호버 세션처럼 **격리 빌드(HEAD + 이 계획의 파일만, 남의 진행 중 파일은 HEAD 판으로)**로 한다. AC-4(T20)는 궤적 TJ-1이 커밋된 뒤 착수한다(`CombatAttackTarget.Resolve`를 그쪽이 고치는 중).
4. **csproj 대조:** Unity csproj의 Compile 목록과 디스크 `.cs`를 비교한다. 빠진 파일은 extra.targets로 더한다(메모리 `projectl-build-check`).
5. **기준선:** MSBuild 오류·경고 목록, 관리 실행 전체(메서드 단위). 관리 실행 도구는 [분리 계획 §7](2026-10-18-combat-scene-event-presenter-split-plan.md#7-관리-실행-방법-세션-스크래치-도구)이나 [시작 아이템 계획 §5.1](2026-10-19-dev-starting-items-plan.md#51-i-0-기준선-2026-10-19-head-8c34b864)의 방법으로 다시 만든다.
6. **실행 뒤 정리:** 관리 실행이 남기는 `Assets/__TornadoLegacy_*.asset`는 `git status`로 확인하고 지운다. 커밋돼 있는 이전 두 벌은 그대로 둔다.
7. **카메라 작업과 순서(평가 G6):** 이 계획의 AC-3과 [카메라 계획](2026-10-19-combat-camera-pieces-plan.md) K-2가 `CombatPieceRun.cs`·`CombatPieceContext.cs`·`CombatPieceStart.cs`를 함께 고친다. 먼저 착수한 쪽이 신호 일반화 Task를 끝내고 사용자가 커밋한 뒤 다른 쪽이 착수한다. AC-1·AC-2는 이 파일들을 고치지 않으므로 K 작업과 겹치지 않는다.
   - **2026-10-19 갱신:** 카메라 K-1~K-3이 끝나 커밋됐다(`c343203d` 등). 신호 일반화는 K-2가 했다. 이 조건은 풀렸다.
   - 카메라 원칙 평가 C5("클립 계획 T15·T16이 낡음")는 4차 반영(§0.5)에서 고쳤다.
8. **폴더 정리와 순서(남음):** [폴더 정리 계획](2026-10-19-combat-folder-restructure-plan.md) Q-M2가 결정 대기다. 폴더 정리를 먼저 하면 이 계획의 `C/`·`CD/` 경로는 그쪽 M-4.3이 새 폴더로 고치고, 이 계획의 새 파일(채널·조각·재생 단위 등)도 그쪽 대응표에 행을 더한다. 이 계획을 먼저 하면 새 파일을 지금 평면 폴더에 만들고, 폴더 정리가 함께 옮긴다. 두 작업이 같은 파일을 동시에 고치지 않게 한쪽이 끝나고 커밋된 뒤 다른 쪽을 시작한다.
9. **카메라 평가 C6과 순서(남음):** [카메라 원칙 평가](../specs/2026-10-19-combat-camera-principles-evaluation.md) C6은 `CombatPieceRun`(642줄)의 대여 빌리기·되돌리기·접근자를 "실행 대여 묶음"으로 뽑는 동작 동일 리팩터링을 **클립 AC-3 전에** 하라고 권한다(별도 계획). 그 분리가 먼저 들어오면 T16(클립 채널 분기)은 그 묶음에 채널 하나를 더하는 일로 줄어든다. 하지 않으면 T16은 지금 실행의 대여 분기 세 곳에 더한다. AC-1·AC-2는 `CombatPieceRun`을 고치지 않으므로 이 순서와 상관없이 착수할 수 있다.
   - **2026-10-19 평가(§0.6 CK1·CK2) 뒤 권장:** C6 분리를 AC-3 전에 한다. 그 계획에 표시 쪽 대여 칸 부품(CK2)도 함께 넣으면 T11의 클립 대여가 그 부품을 쓴다. 이 경우 T11(AC-2)도 그 분리 뒤로 미루거나, T11만 지금 모양으로 만들고 분리가 함께 옮긴다. 사용자 결정 대기.
   - **완료(2026-10-19, `3c50a379`):** [대여 분리 계획](2026-10-19-combat-piece-run-leases-plan.md) L-0~L-3. 대여는 `C/CombatPieceRunLeases.cs`(166줄)로 나뉘었고 실행은 499줄이다. 그 계획은 실행 쪽만 나눴고 표시 쪽 대여 칸(CK2)은 넣지 않았다 → §0.7 Q-AC11.

### 0.4 추천안(P1 A) 원칙 평가 (2026-10-19)

- 사용자 요청: "너의 추천안으로 갔을 때의 SSOT와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성 평가랑, 연출 플로우 전체를 UML로 보여줘".
- 결과와 UML(구조·부트·공격·피격자·대여 수명·신호 규칙)은 아티팩트 [전투 클립 연출 흐름](https://claude.ai/artifact/DysLcFNSLmR4okBskHTYdF)에 있다. 평가 기준은 [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)다.
- 요약: SSOT는 보완 셋 뒤 지킴, SOLID는 보완 하나 뒤 지킴, JMH 지침·FW 계약은 지킴(층 방향 유지가 A의 목적), 변경 독립성은 좋아짐(순서 하나).
- 고칠 것과 반영한 곳:

| # | 정도 | 기준 | 찾은 것 | 반영 |
|---|---|---|---|---|
| G1 | 중간 | SSOT | 같은 신호 장치의 시간 규칙이 두 계획에서 달랐다. 카메라 K-2.3은 "시작 안 신호는 시작의 남은 시간을 이어 받음", 이 계획 T16은 "언제 와도 0" | T16을 K-2.3 규칙으로 고침. 규칙의 원본은 조각·조립 설계 BP2 보충 한 곳(먼저 구현하는 쪽이 적음) |
| G6 | 중간 | 변경 독립성 | AC-3과 카메라 K-2가 같은 파일 셋을 고친다 | §0.3 7번(순서), P7(번호) |
| G2 | 낮음 | SSOT | "이벤트 컴포넌트는 하나, 같은 GameObject에 Animator" 규칙을 DB 검사(T6)와 표시 준비(T11)가 따로 검사 | T2에 `FindIn`, T6·T11이 그것을 부름 |
| G3 | 낮음 | SSOT | `CombatClipData`가 세 시각을 필드와 커서 시작 값 두 벌로 가짐 | T3·T4·T13 |
| G4 | 낮음 | SOLID(L) | 조각 계약 문서 "조각은 신호를 직접 보내지 않는다"가 새 신호 입구와 어긋남 | T15(계약 문서 고침, K-2.2와 같은 Task에서 한 번) |
| G5 | 낮음 | JMH 지침·층 | DataBaseSystem의 첫 MonoBehaviour라 "DB = 입력 계층" 관례를 넓힘 | T8 공유 메모에 범위를 적음 |

- 관찰만 하는 것: O1 `FieldUnitView` 대여 배관 넷째 반복(다섯째 채널 때 공통 부품 검토), O2 실행의 채널 분기 세 곳(알려진 한계), O3 공격 재생 비대화(AC-5 때, 설계 E7), O4 재생 단위의 소비자는 클립 조각 하나(변경 이유가 달라 유지).
- **P1은 아직 사용자 결정 전이다.** 이 평가는 A를 고른다고 가정한 것이다.

### 0.5 설계 4차 반영 (2026-10-19)

- **바뀐 것:**
  - Idle은 프리팹 Animator의 컨트롤러(Idle만)가 원본이고, 클립 채널이 그 컨트롤러를 그래프 바닥에 깔아 사건 클립을 위에 얹는다(설계 AC9). 그래프는 유닛 표시 수명이고, 사건 클립 입력만 대여마다다(설계 AC10). → T9~T14를 다시 썼다.
  - 대여 계약의 `Evaluate`를 `SetTime`으로 바꾼다. A안에서는 평가를 Unity가 한다(설계 I2).
  - 기억한 Transform 값으로 되돌리기(P3)는 컨트롤러가 없는 유닛에만 쓴다. 컨트롤러가 있으면 Idle로 돌아간다.
  - Q-AC10 A면 평가 직후 루트를 되돌릴 수 없어, 루트 곡선 금지를 에디터 검사로 막는다(T3·T6·T7).
- **카메라 K-2가 이미 한 것(설계 F3):** 신호 장치(맥락 `SignalCallback`, 실행 `Signal`, 미도착 규칙, BP2 보충)와 조각 계약 문서. 그래서 평가 G1·G4·G6은 해소됐고, AC-3은 신호 값 둘과 채널 분기만 더한다(T15·T16·T19를 다시 썼다). 신호 번호는 `End = 1`·`Hit = 2` 다음이다.
- **결정 완료(2026-10-19):** 설계 Q-AC9 A(= P1, 컴포넌트는 입력 계층)·Q-AC10 A(Unity 게임 시간 그래프 + 루트 곡선 에디터 검사). 아래 Task는 이 결정 그대로다.

### 0.6 커밋된 카메라 작업 위에서 이 계획 그대로 갈 때의 원칙 평가 (2026-10-19)

- 사용자 요청: "지금 이대로 작업 시 카메라 작업 내용 SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성로 평가해".
- 대상: 카메라 K-1~K-6(커밋됨, HEAD `927962fb`)의 코드와 [카메라 원칙 평가](../specs/2026-10-19-combat-camera-principles-evaluation.md) 2차, 이 계획 4차. 기준은 [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), 점수는 1~10(10 = 지적할 것이 없음). 코드 읽기 기준이고 코드 변경은 없다.
- 읽은 카메라 코드: `CombatPieceRun`(642줄, 대여 분기·카메라 줌 경로·신호), `CombatPieceContext`(`SignalCallback`), `CombatPieceStart`(`End = 1`·`Hit = 2`), `CD/CombatCameraWindow`(줌 대여·흔들림 겹침), `CombatRoarEventHandler.Compose`(흔들림 `After(facing, Hit)`), `CombatCharacterEventHandler.ComposeHit`(끝 선택 인자 `camera`·`cameraMotion`), `CombatAttackPlayback.TryBeginFacingMotion`(해제 → 타격 알림 순서), `CombatFacingAttackPiece`, 사건 Presenter 생성자, 시험 `CombatPieceRunTests` Harness.

#### 결론

| 기준 | 그대로 | 계획 보완 뒤(CK3~CK5) | + C6 분리 먼저(CK1·CK2) | 깎은 이유(그대로 기준) |
|---|---|---|---|---|
| SSOT | 8 | 9 | 9 | −1 CK4: "대상이 맞는 순간"을 근접 클립 길만 `Hit`으로 내지 않는다. −1 CK2: 대여 수명 규칙(버전·취소 콜백·늦은 복원 무시)이 다섯째 사본이 된다 |
| SOLID | 7 | 8 | 9 | −2 CK1: 642줄 실행에 유닛 채널 넷째 분기를 세 곳(빌리기·되돌리기·접근자)에 더한다(카메라 C6·C7을 키움). −1 CK3: 피격 조립 인자가 "유닛 재료 → 카메라 → 유닛 재료"로 섞인다 |
| FW 지침·계약 | 9 | 9 | 9 | Framework·Factory·ServiceKeys·`ViewGroup.asset` 변경 0, 새 Service·Key 0. 시간 기준이 카메라와 같다(CK6). −1: DB 층 첫 MonoBehaviour(G5, 알고 받아들임) |
| 변경 독립성 | 7 | 7 | 9 | −2 CK1: AC-3이 카메라가 만든 실행 파일 셋을 고치는데, 카메라 평가가 권한 C6 분리가 같은 파일을 고친다(순서가 필요). −1 CK1: 채널 하나에 실행 세 곳 |
| **종합** | **7.8** | **8.3** | **9.0** | 네 기준 평균 |

- 카메라 쪽 결정과 맞물려 **잘 맞는 것:**
  - 신호 장치는 카메라 K-2 것을 그대로 쓰고 값 둘(`ClipStart`·`ClipEvent`)만 더한다. 시간·미도착 규칙의 원본은 BP2 보충 한 곳이다(설계 F3, 옛 G1·G4·G6 해소).
  - `Hit`(Gate 해제)과 `ClipEvent`(클립 큐)를 나눈 결정(설계 E1)이 카메라 코드에서도 맞다. 피격 선딜레이가 0보다 크면 두 순간이 다르고, 카메라 흔들림은 `Hit`에 건다(`CombatPieceSignal.Hit` 문서가 이미 이 구분을 적음).
  - 해제 규칙 원본은 Gate 하나다. T21이 `HitAll`을 `ReleaseAll`로 모으면, 카메라 K-2가 만든 `TryBeginFacingMotion`의 해제 람다도 같은 곳을 쓴다.
  - 피격 흔들림(K-4)은 피격 조립 `AtStart`, 곧 해제가 피격 사건을 연 순간이다. 피격 클립이 들어와도 이 순간은 그대로라 흔들림 시점이 바뀌지 않는다.
  - 카메라 줌은 장면 채널이고 클립은 유닛 채널이라 대여가 겹치지 않는다. 둘 다 취소되면 같은 `OnLeaseCancelled`로 간다.
  - Factory 변경 0: 공격 처리기는 Presenter 생성자 안에서 만들고, 넘길 `piecePlayer`·`_presentation`은 그 자리에 이미 있다(T24).

#### 발견 사항

| # | 등급 | 기준 | 내용 | 반영 |
|---|---|---|---|---|
| CK1 | 중간 | SOLID-S·O, 독립성 | 카메라 C6·C7을 키운다. 지금 `CombatPieceRun`은 유닛 채널 셋을 `switch` 두 곳(`TryBorrow`·`RestoreLeases`)과 접근자 셋(`ScaleOf`·`PoseOf`·`FlashOf`, 같은 선형 검색)에, 장면 채널(카메라 줌)을 따로 된 경로에 둔다. T16 그대로면 `Lease` 필드·`switch` 두 곳·접근자 하나를 더해 넷째 사본이 된다. 카메라 평가는 이 분리를 "클립 AC-3 전에" 하라고 권했다(별도 계획, 세션 카드는 카메라 쪽에 있음) | §0.3 9번 권장을 "AC-3 전 C6 분리"로 굳힘. AC-1·AC-2는 실행 파일을 안 고치므로 C6과 나란히 갈 수 있다. T16에 두 경우의 할 일을 적음 |
| CK2 | 낮음 | SSOT·DRY | 대여 한 칸의 수명 규칙(현재 대여 + 버전, 취소 콜백 꺼내기, 늦은 복원 무시)이 `FieldUnitView`의 자세·크기·플래시 셋과 `CombatCameraWindow` 줌 하나에 있다. T11의 `PresentationUnitClip`이 다섯째다. 옛 관찰 O1의 "다섯째 때 공통 부품 검토" 문턱에 닿았다 | C6 분리 계획에 둘째 부분(표시 쪽 "대여 칸" 부품, 동작 동일)으로 넣을 것을 권장. 하지 않으면 T11은 지금 모양을 따르고 관찰로 남긴다(T11에 적음) |
| CK3 | 낮음 | SOLID-I·읽기 | K-4가 `ComposeHit` 끝에 선택 인자 `camera`·`cameraMotion`을 붙였다. T18처럼 `hitClip = null`을 그 뒤에 붙이면 유닛 재료(`settings`·`canFlash`)와 클립 사이에 카메라가 낀다 | T18: `hitClip`을 `canFlash` 바로 뒤에 둔다(유닛 재료 → 장면 재료 순). 형식이 달라 자리 실수는 컴파일 오류로 잡히고, 고칠 호출은 둘(`TryBeginHit`, `CombatHitCompositionTests` 149행)이다 |
| CK4 | 낮음 | SSOT | 신호 표는 `Hit` = "대상이 맞는 순간(Gate 해제)"이고 정면 공격 조각(K-2)·발사 조각(AC-5)이 낸다. 근접 클립의 해제 조각만 내지 않아, 같은 사실을 코드 길은 `After(x, Hit)`, 근접 클립 길은 `After(해제, End)`로 적게 된다. 포효가 나중에 클립 항목을 쓰면 흔들림 연결의 신호 이름까지 바뀐다 | T22: 해제 조각이 해제 직후 `Hit`을 낸다(정면 공격 모션과 같은 순서 "해제 먼저"). 순서 규칙은 `CombatPieceSignal.Hit` 문서 한 곳에 적는다. 설계 AC6 신호 표·AC7 표 고침 |
| CK5 | 낮음 | 명명 | 새 `CombatAttackFacePiece`(목표 보기)와 기존 `CombatFacingAttackPiece`(정면 공격 모션 실행)가 단어 순서만 다르다 | T22: `CombatFaceTargetPiece`로 바꿈(§6 파일 목록도) |
| CK6 | 참고 | SSOT(시간) | 시간 기준이 카메라와 같다. 연출 Tick = 부트의 `Time.deltaTime`, 창구 시계 = `Time.time`, 줌 조각 = Tick 누적, 클립 그래프 = `DirectorUpdateMode.GameTime`(배율 적용 시간). 시간 배율·배속이 생기면 함께 움직인다. 클립 재생 단위의 시각(Tick 누적)과 Idle(그래프)이 같은 프레임에 평가된다(설계 F16) | 수용 |
| CK7 | 참고 | 독립성 | 클립 공격 조립에 카메라 항목(공격 흔들림 등)을 넣으려면 공격 처리기도 `camera`·`cameraMotion`을 받아야 하고, 넣는 조건(카메라 C14)이 다섯째로 반복된다. 이번 범위에는 카메라 항목이 없다 | 그때 C14 도우미와 함께. 이번에는 바꾸지 않음 |
| CK8 | 참고 | 검증 | 카메라 K-2가 새 시험 `CombatPieceSignalTests`(신호 장치)를 만들었고, `CombatPieceRunTests` Harness가 카메라 창구를 만든다. T19 회귀 목록에 신호 시험과 카메라 창구 시험이 빠져 있었다 | T19 회귀 목록에 `CombatPieceSignalTests`·`CombatCameraWindowTests` 추가 |

- 높음 0, 중간 1(CK1 = 카메라 C6), 낮음 4, 참고 3. 점수를 가장 많이 올리는 것은 CK1(C6 분리를 AC-3 전에)이다. CK3~CK5·CK8은 계획 글만 고치는 일이라 이 평가 때 반영했다.

### 0.7 C6 완료 뒤 재점검 (2026-10-19)

- 사용자 요청: "지금 C6 작업 마무리했는데 계획 다시 점검해, 내가 말하던 기준 잊지 말고". 기준은 §0.6과 같다(SSOT·SOLID·JMH Framework 지침·FW 계약·변경 독립성, [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)). 코드 읽기 기준이고 코드 변경은 없다.
- 대조한 코드: HEAD `3c50a379`의 `C/CombatPieceRunLeases.cs`·`C/CombatPieceRun.cs`(499줄)·`C/CombatComposition.cs`(채널 열거 `CameraZoom = 101`, `IsSceneChannel`)·`C/CombatPieceContext.cs`(`run.Leases`), `T/CombatPieceRunLeasesTests.cs`, `T/CombatPieceRunTests.cs` 시험 가짜, `CD/FieldUnitView.cs` 대여 셋, `CD/ICombatUnitTargets.cs`. 다른 진행 작업: [공격 궤적 계획](2026-10-19-combat-attack-trajectory-plan.md)(TJ), 호버 U-1·U-2(작업 트리, 커밋 전), [폴더 정리 계획](2026-10-19-combat-folder-restructure-plan.md)(Q-M 대기), [이동 범위 소유자 설계](../specs/2026-10-19-combat-movement-range-tracker-design.md)(`CombatPathInput`만, 겹침 없음).
- 이 계획이 기대던 자리(`ComposeHit` 서명, `HitAll`, `CombatPieceSignal.Hit = 2`, `CombatFacingAttackPiece`, Presenter 생성자의 조각 실행 재생기·공격 처리기 조립, 시험 가짜 이름)는 모두 그대로다.

#### 결론

| 기준 | §0.6 계획 보완 뒤 | 지금(C6 완료) | + Q-AC11 A | 깎은 이유(지금 기준) |
|---|---|---|---|---|
| SSOT | 9 | 9 | 10 | −1 CC2: `FieldUnitView`의 대여 수명 규칙(현재 대여·버전, 끝내기 순서, 늦은 복원 무시)이 클립으로 넷째 사본이 된다 |
| SOLID | 8 | **9** | 9 | CK1 해소(실행에 채널 분기를 더하지 않음). −1: 공격의 두 길(코드·클립) 비대화 관찰(설계 E7, AC-5 때 재검토) |
| FW 지침·계약 | 9 | 9 | 9 | Framework·Factory·ServiceKeys·`ViewGroup.asset` 변경 0 그대로. −1: DB 층 첫 MonoBehaviour(G5, 알고 받아들임) |
| 변경 독립성 | 7 | **9** | 9 | CK1 해소(새 유닛 채널 = 열거 값 + 묶음 한 칸 + 꺼내기 한 줄). −1: 순서 조건 셋(CC5 궤적 TJ-3과 같은 파일, CC6 호버 미커밋 트리, CC7 폴더 정리 Q-M2) |
| **종합** | 8.3 | **9.0** | **9.3** | §0.6이 예상한 "C6 먼저 9.0"과 같다 |

#### 발견 사항

| # | 등급 | 기준 | 내용 | 반영 |
|---|---|---|---|---|
| CC1 | 해소 | SOLID·독립성 | CK1(카메라 C6·C7)이 풀렸다. 대여는 `CombatPieceRunLeases`가 맡고, 채널 하나 = `TryBorrowOne`의 한 칸(빌린 자리에서 되돌리기 기록) + 꺼내기 한 줄이다. 열거 값만 더하고 칸을 빠뜨리면 빌리기가 "대여 방법을 모르는 채널" 예외라 조용히 틀리지 않는다. 유닛 채널 번호는 비어 있어 `Clip = 4`가 그대로 맞는다(열거 문서가 이 계획을 가리킴) | T16을 묶음 기준으로 다시 씀(`CombatPieceRun.cs`는 고치지 않음) |
| CC2 | 낮음 | SSOT·DRY | CK2는 그대로다. 대여 분리는 실행 쪽만 나눴다. `FieldUnitView`는 채널마다 필드·버전, 시작 검사 둘, 끝내기 순서(취소 꺼냄 → 복원 → 버전 올림 → 취소 콜백, `finally`), 중첩 대여 클래스의 `IsCurrent`·`TakeCancellation`·늦은 복원 무시를 따로 가진다(자세·크기·플래시). 클립이 넷째다. 카메라 줌 대여는 "맵이 빠지면 되돌리지 않고 취소만" 하는 다른 규칙이라 같은 사본으로 세지 않는다(§0.6 CK2의 "다섯째"를 바로잡음) | Q-AC11(아래), T11에 두 경우 |
| CC3 | 낮음 | 독립성·검증 | 대여 분리(사용자 결정 A)가 `T/CombatPieceRunTests.cs` 시험 가짜에 `FakeTarget.ThrowOnScaleRestore`·`FakeScale.ThrowOnRestore`를 더했다. AC-2가 같은 가짜에 클립 멤버를 더할 때 두 멤버를 남긴다. 새 `T/CombatPieceRunLeasesTests.cs`는 채널을 하나씩 세는 사례(모든 채널 빌리기·되돌리기 순서, 밖 취소 콜백, 꺼내기 예외 문구)가 있어 클립 채널을 넣어야 한다 | §1 시험 가짜, T19 |
| CC4 | 참고 | 일관성(ISP) | `ICombatUnitTargets`의 외형 값 읽기는 `AttackSettingsOf`·`ReactionSettingsOf`다. T12의 `AnimationOf`만 이름 모양이 다르다. 궤적 계획(P4)은 위치 읽기를 이 계약에 넣지 않으려고 새 좁은 계약을 만들었지만, 클립 묶음은 같은 종류의 "키별 외형 값"이라 이 계약이 맞는 자리다 | T12: `AnimationSettingsOf`로 바꿈 |
| CC5 | 낮음 | 독립성 | 궤적 TJ-3이 `CD/FieldUnitPresenter.cs`에 `ICombatUnitPlacementReader` 명시 구현을 더한다. T12도 같은 파일(클립 채널 준비·해제, 클립 값 읽기)을 고친다. 궤적 TJ-1은 `CombatAttackTarget.Resolve`에 형태 하나를 더하고 옛 형태를 위임으로 바꾼다. T20은 옛 형태를 부르기만 하고 `CombatAttackMotion`(TJ-1이 안 고침)을 고치므로 같은 파일은 없고, 목표 해석 원본은 그대로 `Resolve` 하나다 | §6 순서에 "`FieldUnitPresenter`는 먼저 착수한 쪽이 커밋한 뒤 다른 쪽", T20에 한 줄 |
| CC6 | 낮음 | 독립성·검증 | 작업 트리에 호버 U-1·U-2가 커밋 전이다(Core 미리보기·Buffer·`CombatInteraction`·`CombatPathInput`·`FieldView` 등 + 새 파일 여섯). 이 계획의 파일과 겹치지 않지만, AC-0 기준선·MSBuild에 섞이면 회귀 원인을 가르기 어렵다 | §0.3 3번. 권장: 호버 U-2 Unity 확인·커밋 뒤 AC-0. 아니면 대여 분리·호버 세션처럼 다른 세션 파일을 HEAD 판으로 고정한 격리 빌드. **갱신:** 호버는 `950a4b34`로 커밋됐고, 지금은 경로 입력 분리·궤적 TJ-1이 진행 중이라 격리 빌드로 정함(§0.3 3번) |
| CC7 | 참고 | 독립성 | 폴더 정리 Q-M2가 아직 결정 전이다. 폴더 정리를 먼저 하면 그쪽 대응표에 `CombatPieceRunLeases` 한 줄(대여 분리 계획 §6.4)이 필요하고, 이 계획을 먼저 하면 §6의 새 파일 목록이 대응표에 들어가야 한다 | §0.3 8번 그대로(바뀐 것 없음) |

- 높음 0, 중간 0, 낮음 4(CC2·CC3·CC5·CC6), 참고 2, 해소 1. CC3~CC5는 계획 글만 고치는 일이라 이 점검 때 반영했다.

#### Q-AC11. 표시 쪽 대여 칸 부품을 AC-2에서 만들까 (CC2) — 결정 완료: A

- **결정(2026-10-19, 사용자 "Q-AC11은 A로 해. AC-2 첫 Task(T10a)로 표시 대여 칸 부품을 만들고 클립 대여도 그걸 쓰게 해."): A.** 아래 T10a·T11과 §6 파일 목록은 A 기준이다. 목표 점수 9.3(SSOT 10·SOLID 9·FW 9·독립성 9).
- 아래는 결정 때 비교한 안이다.

- **A(권장): AC-2 첫 Task로 동작 동일 리팩터링을 한다(T10a).**
  - 새 `CD/UnitPresentationLeaseSlot.cs`가 "현재 대여·버전, 늦은 복원 판정(`IsCurrent`), 끝내기 순서(취소 꺼냄 → 복원 → 버전 올림 → 취소 콜백, `finally`)"를 한 곳에 갖는다.
  - `FieldUnitView`의 자세·크기·플래시가 이 칸을 쓰고, 채널마다 다른 것(시작 검사와 예외 문구, 스냅숏, 복원 내용)만 남긴다. 예외 문구·순서·`OnDisable`·`Bind` 동작은 그대로다.
  - 그다음 T11의 클립 대여가 같은 칸을 쓴다. 카메라 줌 대여는 규칙이 달라 건드리지 않는다.
  - 이유: 규칙 하나가 네 곳에서 한 곳이 된다(SSOT 9 → 10). `FieldUnitView`는 AC-2가 어차피 고치는 파일이라 새 파일 겹침이 없다.
  - 대가: AC-2 Task 하나. Unity 회귀로 `CombatUnitPresentationWindowTests`(Unity 사례)·`UnitModelAppearanceTests`·`CombatStandardAttackTests`·`BossRoarTests`를 본다.
- B: 클립 대여를 지금 모양 그대로 하나 더 만들고(넷째 사본), 다섯째 유닛 채널 때 부품으로 모은다. AC-2가 가장 짧다.
- C: 대여 분리처럼 별도 계획·세션으로 먼저 한다. A와 결과가 같고 절차가 하나 더 든다.

### 0.8 들어온 실제 모델과 구조 대조 (2026-10-19, 코드 변경 없음)

- 사용자 요청: "현재 모델들이 들어가는 방향으로 현재 구조 맞는지 확인해줘"(뼈대 이름은 맞추는 방향으로 고칠 예정). 대상은 민형님 커밋 `cad28dba`·`371f8e56`의 `Resources/Characters/Colored/Models/`(FBX 13개).
- 방법: `.meta` 가져오기 설정과, FBX 바이너리를 직접 읽는 진단 도구(이 세션 스크래치 `fbx/FbxDump.cs`: Model 계층·테이크별로 움직이는 노드)로 확인했다. 클립 길이·실제 움직임은 Unity에서 봐야 한다.

#### 확인한 사실

| # | 사실 |
|---|---|
| M1 | 모두 Generic 리그, 아바타 없음(`avatarSetup: 0`), 가져오기 설정 기본값(`clipAnimations: []`) — 반복 꺼짐, 이벤트 없음 |
| M2 | **적 4개**(`Enemy/`): 파일마다 같은 뼈대(`Scull/뼈대…` 39개)와 **같은 테이크 9개**(Cannon·FixSo·Sabow·Stage1Boss의 Idle·Attack, CannonAttackBody)가 다 들어 있다. 최상위 노드가 둘 이상이라 Unity 경로는 `Scull/뼈대/…`. 모델과 클립이 같은 파일이라 그대로 붙는다 |
| M3 | **아군 애니메이션 `Playerble/Animation.fbx`**: 계층이 한 줄로 이어져 있다 — `Barllista`(발리스타 무기 아마추어) → 그 뼈대 아래 `BaseModel`(캐릭터 아마추어) → 오른손 뼈대(`뼈대.013.R`) 아래 `MoningStar`(모닝스타 무기 아마추어). 그래서 캐릭터 테이크의 경로는 `Barllista/뼈대/BaseModel/뼈대/…`, 모닝스타는 `…/뼈대.013.R/MoningStar/…`다 |
| M4 | **아군 모델**(Akimbo·Cowboy·Shotgun·Sniper·Ballista): 최상위 `아마츄어` + 메시들, 경로 `아마츄어/뼈대/…`. 뼈대 이름은 `Animation.fbx`의 부분집합이다(같은 본 구조). Cowboy는 오른손 뼈대 아래에 무기 아마추어 `아마츄어.001`이 있다(모닝스타 자리). Ballista.fbx에는 발리스타 무기 아마추어가 없다(무기는 메시만) |
| M5 | 무기 동작은 **캐릭터와 다른 테이크**다: `BaseModel|MoningStarAttack` + `MoningStar|Attack`, `BaseModel|BarllistaAttack` + `Barllista|Attack.001`, Idle도 같다. CannonAttack·CannonAttackBody는 둘 다 `Scull` 전체를 움직이는 변형 둘이다(나뉜 것 아님) |
| M6 | 테이크마다 아마추어 노드 자신(`Scull`·`BaseModel` 등)의 위치·회전·크기 키가 있다. 경로가 비어 있지 않아 루트 곡선 검사(T3)는 통과한다 |
| M7 | 피격 테이크 없음. 정리 안 된 테이크가 있다(`Action`, `BaseModel|BaseModelAction`, `BaseModel|아마츄어Action`, `.001` 사본). Sniper·Parasol·Cactuscannon·Horse 테이크 없음, Cactuscannon·Parasol은 뼈대도 없다 |
| M8 | 연결은 아직 없다. 적·보스·`Animation.fbx`를 쓰는 프리팹·컨트롤러가 없고, `*_Colored` 아군 프리팹 5개는 DB 외형 표가 가리키지 않는다(표는 옛 프리팹) |
| M9 | 유닛 프리팹 = 루트(Transform·BoxCollider) + FBX 인스턴스 자식. 표시가 루트에 `FieldUnitView`를 붙이고, 코드 모션(자세·크기·방향)은 **루트**만 움직인다. Animator는 FBX 인스턴스(자식)에 있으므로 서로 겹치지 않는다 |

#### 판정 — 맞는 것

- 그래프 방식(Animator + `AnimationClipPlayable`, Idle은 `AnimatorControllerPlayable`)은 Generic·아바타 없음과 맞는다(M1).
- 코드 모션과 Animator가 다른 Transform을 맡아 충돌이 없다(M9). 이벤트 컴포넌트는 FBX 인스턴스(Animator 있는 오브젝트)에 프리팹 덮어쓰기로 붙이면 `FindIn` 규칙에 맞는다.
- 클립 이벤트는 FBX 가져오기 설정의 Events에 찍으면 `clip.events`로 읽힌다(T3 그대로). 루트 곡선 검사도 통과한다(M6).
- 클립 묶음 SO 하나를 아군이 공유하고, Idle이 다르면 Override Controller를 쓰는 설계(AC8·T8 안내)와 맞는다.
- 적은 파일 하나에 모델·클립이 다 있어 바로 쓸 수 있다(M2). Stage1Boss(근접)가 AC-4까지의 실제 시험 대상으로 가장 알맞다.
- 피격 칸은 비워도 된다(AC1).

#### 판정 — 어긋나는 것

| # | 등급 | 내용 | 권장 |
|---|---|---|---|
| X1 | 높음(에셋) | 아군 클립 경로가 `Barllista/뼈대/BaseModel/뼈대/…`라 아군 모델(`아마츄어/뼈대/…`)에 붙지 않는다. **아마추어 이름만 `BaseModel`로 바꿔서는 부족하다**(앞서 말씀드린 "이름 하나만"은 틀림, M3) | 민형님: `Animation.fbx`에서 `BaseModel`을 발리스타 뼈대에서 떼어 최상위로 내보내고(발리스타 동작은 따로), 아군 모델의 아마추어를 `BaseModel`로, Cowboy 무기 아마추어를 `MoningStar`로 맞춘다. 다시 내보낼 때 최상위 노드가 하나만 남으면 Unity가 그 노드를 없애 경로가 또 바뀌므로, 가져오기 설정 Preserve Hierarchy를 켜 둔다 |
| X2 | 중간(설계) | 무기 동작이 캐릭터와 다른 테이크다(M5). 설계는 사건마다 클립 **하나**(채널 위층 하나)이고 Idle 컨트롤러도 하나다 | A(권장): 행동마다 캐릭터+무기를 **한 테이크로** 합쳐 내보낸다(블렌더 NLA 트랙 이름을 맞추면 한 테이크가 됨). 설계·계획 그대로. B: 묶음에 "부속(무기) 클립" 칸과 채널 위층을 하나 더 둔다(설계 변경, AC-2 T10 범위 늘어남). **결정(2026-10-19, 사용자 "Q-AC12는 A로 해"): A** — 요청은 민형님 메모 §2. **→ B로 바뀜(2026-10-19, 사용자 "B로 하자. 부속 클립 지원 설계부터 고쳐서 보여줘."):** 민형님이 무기 테이크를 따로 내보냄(`CannonBone|CannonAttackBody`), "NLA 이름을 맞추면 한 테이크"는 확인하지 않은 내용이었음. 설계 §12(AC16~AC19, 결정 대기 Q-AC20~Q-AC22) |
| X3 | 중간(계획) | 들어온 아군 공격은 대부분 원거리다(쌍권총·샷건·발리스타). 계획 P5로 AC-4까지 공격 클립은 근접만이라 이 클립들은 AC-5(발사 조각) 전까지 Idle만 쓸 수 있다. 근접은 Stage1Boss·Cowboy(모닝스타)·적 일부 | AC-4 뒤 AC-5를 바로 계획할지 사용자 결정. 그 전까지 아군은 Idle만으로도 화면 이득이 있다(컨트롤러만 연결하면 AC-2 전에도 Unity가 Idle을 돌림) |
| X4 | 낮음(AC-5 범위) | 발리스타 무기는 지금 코드(`BallistaWeaponView` 활시위·재장전)가 움직인다. 무기 클립까지 들어오면 같은 무기를 둘이 움직인다. Cactuscannon은 뼈대가 없어 코드 모션(곡사) 그대로 | AC-5 설계 때 정함(클립이 있으면 코드 무기 동작을 끔 등) |
| X5 | 낮음(에셋 설정) | Idle 반복 꺼짐, 공격 이벤트 셋 없음, 정리 안 된 테이크(M7) | 가져오기 설정에서 Idle은 Loop Time 켜기, 공격 클립에 `ClipStart`·`ClipEvent`·`ClipEnd`. 안 쓰는 테이크는 지우거나 무시 |
| X6 | 낮음 | 적 테이크가 파일 4개에 같은 것으로 중복(M2). 적 FixSo·Sabow·Cannon이 어느 유닛(EntityKey)인지 이름만으로 모름 | 적 클립은 파일 하나를 원본으로 정해 쓴다(어느 것이든 경로 같음). 유닛 대응은 민형님·기획 확인 |
| X7 | 낮음(프리팹) | 새 모델 프리팹이 DB에 연결 안 됨(M8). 표시 몸(BoxCollider)은 프리팹 루트에 있어 모델 크기가 바뀌면 히트 타이밍 몸도 맞춰야 한다 | 프리팹 교체 때 BoxCollider 크기 확인 |

- **AC-1 코드는 바꿀 것이 없다.** Q-AC12 A로 정해져 AC-2~AC-4 계획도 그대로다(사건마다 클립 하나, Idle 컨트롤러 하나).
- **에셋 요청:** X1·X2·X5·X6을 민형님 요청 메모 [`handoffs/2026-10-19-unit-animation-model-request-minhyeong.md`](../handoffs/2026-10-19-unit-animation-model-request-minhyeong.md)에 적었다(미전달).

## 1. 공통 규칙

- **위치·형식:** 네임스페이스는 폴더 경로를 따른다. Allman, `sealed` 기본, 한국어 XML doc. `.meta`는 32자리 hex GUID로 새로 만든다.
- **바꾸지 않는 것:** Core·Buffer·Factory·ServiceKeys·`ViewGroup.asset`·DB 에셋·씬·프리팹. 클립을 쓰는 실제 유닛이 없으므로 에셋은 건드리지 않고, 시험은 메모리에서 만든 클립과 GameObject로 한다.
- **시험 가짜:** 표시 계약(`ICombatUnitPresentationTarget`)과 표시 집합 계약(`ICombatUnitTargets`)에 멤버를 더하면 시험 가짜도 함께 고친다. 대상은 `CombatPieceRunTests`(`FakeTarget`·`FakeTargets`·`FakeLifetime`), `CombatUnitPresentationWindowTests`(`FakeTarget`·`FakeTargets`), `CombatUnitHudFeedbackTests`(`FakeTargets`)다. `CombatPieceRunLeasesTests`는 `CombatPieceRunTests`의 가짜를 쓴다. 궤적 작업(`823aa3c9`) 뒤로 `CombatAttackTrajectoryTests`(`FakeTargets`)도 표시 집합 계약을 구현한다(AC-2에서 확인, §8.3).
  - `CombatPieceRunTests`의 `FakeTarget.ThrowOnScaleRestore`·`FakeScale.ThrowOnRestore`(대여 분리 사용자 결정 A)는 지우지 않는다(§0.7 CC3).
- **Phase마다 확인:**
  - MSBuild 오류 0, 새 경고 0(기준선 대비)
  - 관리 실행 기존 시험 상태 변화 0(메서드 단위로 PASS → 다른 상태 없음), 새 순수 시험 PASS
  - Unity Test Runner 확인 목록(사용자)
- Phase마다 멈추고 보고한다.

## 2. AC-1 — 데이터·이벤트 셋 (설계 AC1·AC2·AC4·AC5의 커서, E6·E9·E11)

### T1 이벤트 커서 `C/CombatClipEventCursor.cs` (새, 순수)

- `internal sealed class CombatClipEventCursor`(원칙 검토로 struct → class, §8.2 S5): 생성자 `(float start, float eventTime, float end)`. 시각 규칙은 `CombatClipData.FindTimeError`(원본 하나)이고, 어기면 `ArgumentOutOfRangeException`.
- 종류 `internal enum CombatClipEventKind { Start = 1, Event = 2, End = 3 }`(같은 파일). 값이 내는 순서다(커서가 기댐).
- `internal bool TryNext(float time, out CombatClipEventKind kind)`: 지금 시각까지 아직 내지 않은 다음 이벤트를 하나씩 낸다. 구간은 (이전, 지금]이고, 0초 이벤트는 처음 부를 때 낸다. 같은 시각이면 Start → Event → End 순이다.
- `IsFinished`: End를 냈는지. 낸 뒤에는 더 내지 않는다.
- 재생 하나에 하나를 만들고(할당 한 번), `TryNext`는 할당하지 않는다(지침 §19, Tick 경로).

### T2 이벤트 컴포넌트 `DV/CombatAnimationEvents.cs` (새, MonoBehaviour, P1 A)

- `[DisallowMultipleComponent, RequireComponent(typeof(Animator))] internal sealed class CombatAnimationEvents : MonoBehaviour`
- 함수 셋 `public void ClipStart()`·`ClipEvent()`·`ClipEnd()`: 본문은 오류 로그 한 번뿐이다.
  - 로그: `[CombatAnimationEvents] 전투 클립 이벤트는 전투 재생기가 부릅니다. Animator 이벤트가 켜져 있습니다: {gameObject.name}`. 인스턴스마다 한 번만 남긴다.
  - XML doc: "애니메이션 창에서 고르는 이름표입니다. 실행 중에는 불리지 않습니다(Animator 이벤트를 끔, 설계 AC2)."
- `internal Animator Animator`: 같은 GameObject의 Animator를 돌려준다.
- `internal static CombatAnimationEvents FindIn(GameObject root)`(평가 G2): `root` 아래(비활성 포함)에서 이 컴포넌트를 찾는다. 없으면 null, 둘 이상이거나 Animator가 없으면 `InvalidOperationException`(문구에 `root` 이름). "하나, Animator와 같은 GameObject" 규칙은 이 함수에만 있고, DB 검사(T6)와 표시 준비(T11)가 함께 부른다.
- 함수 이름의 유일한 원본이다. 다른 곳은 `nameof(CombatAnimationEvents.ClipStart)` 등으로만 쓴다.

### T3 이벤트 읽기 `DV/CombatClipEvents.cs` (새, 정적)

- `internal static void Read(AnimationClip clip, out float start, out float eventTime, out float end)`(평가 G3: 커서가 아니라 세 시각을 돌려준다)
  - `clip.events`를 읽는다. 함수 이름이 셋 중 하나가 아니면 실패한다.
  - 셋이 정확히 하나씩 있어야 하고, 순서는 시작 ≤ 이벤트 ≤ 끝, 시각은 [0, 클립 길이] 안이어야 한다.
  - 실패는 `InvalidOperationException`이다. 문구에 클립 이름과 이유(빠짐·중복·순서·범위·모르는 함수 이름)를 넣는다.
- 루트 곡선 검사는 원칙 검토 뒤 `DV/CombatUnitAnimationRules.cs`로 옮겼다(T6a, §8.2 S3). 이 클래스는 이벤트 셋만 읽는다.
- 이벤트를 찍은 Unity 매개변수 값은 보지 않는다.

### T4 클립 값 `DV/CombatClipData.cs`·`DV/CombatClipAttackData.cs`·`DV/CombatUnitAnimationData.cs` (새)

- `CombatClipData`: `Clip`(AnimationClip), `StartTime`·`EventTime`·`EndTime`(세 시각만, 평가 G3). 재생 길이는 `EndTime`이다. 커서는 재생 단위가 재생할 때 이 시각으로 만든다(T13).
- `CombatClipAttackData`: `Clip`(`CombatClipData`), `HitDelay`(피격 선딜레이), `SoundDelay`(소리 선딜레이). 둘 다 초이고 유한한 0 이상이다(P6).
- `CombatUnitAnimationData`: `Attack`(`CombatClipAttackData`, 없으면 null), `Hit`(`CombatClipData`, 없으면 null).
- 모두 불변이고 생성자에서 인자를 검사한다.

### T5 클립 묶음 SO `DV/CombatUnitAnimationSO.cs` (새)

- `[CreateAssetMenu(fileName = "CombatUnitAnimation", menuName = "ProjectL/Database/View/Combat Unit Animation")] internal sealed class CombatUnitAnimationSO : ScriptableObject`
- 직렬화 필드:
  - `[Header("공격")] attackClip`, `hitDelay`(Tooltip: "이벤트 타이밍 뒤 대상이 맞기까지의 초. 피격자의 피격 모션이 이때 시작합니다"), `soundDelay`(Tooltip: "이벤트 타이밍 뒤 공격 소리까지의 초")
  - `[Header("피격")] hitClip`
- `internal CombatUnitAnimationData CreateData()`: 매번 새로 만든다(P2).
  - 클립이 하나도 없으면 실패한다(묶음을 연결한 뜻이 없음).
  - 클립마다 사건 클립 규칙(`CombatUnitAnimationRules.RequireEventClip`: 길이 양수·반복 금지·루트 곡선 금지)과 T3 읽기.
  - 선딜레이는 유한한 0 이상(`CombatClipAttackData.IsValidDelay`). 공격 클립이 있을 때만 본다.
  - 문구: `[CombatUnitAnimationSO] 클립 묶음 값이 잘못되었습니다: {이유}, Asset: {name}`(`CombatAttackPresentationSO`처럼 에셋 이름을 붙임). 규칙·읽기의 예외는 이유에 담아 다시 던진다.
  - 툴팁은 허용 공격 종류를 적지 않고 규칙 클래스를 가리킨다(규칙 문장은 한 곳, §8.2).

### T6 외형 표 칸 `UV/UnitVisualDataSO.cs`

- 필드 `[Tooltip("공격·피격 애니메이션 클립 묶음입니다. 비워 두면 코드 모션만 씁니다. 여러 유닛이 공유할 수 있습니다.")] [SerializeField] private CombatUnitAnimationSO animationPresentation;`
- 읽기 `public CombatUnitAnimationData AnimationPresentation`: 비면 null, 있으면 `CreateData()`.
- `Validate`(DB 생성 때 한 번, `UnitVisualTable` 생성자가 부름, E11): 공격 설정을 한 번 만들고 `CombatUnitAnimationRules.ValidateUnit(prefab, animationPresentation, attack, "Key: …")` 한 줄을 부른다(원칙 검토 S1 — 클립 규칙을 호진님 파일 밖으로).

### T6a 클립 규칙 `DV/CombatUnitAnimationRules.cs` (새, 원칙 검토로 더함, §8.2)

- `ValidateUnit`: `FindIn(prefab)`을 **묶음과 상관없이** 부른다. 컴포넌트가 있으면(그 Animator를 AC-2 채널이 넘겨받음) 컨트롤러(Idle) 클립의 루트 곡선을 본다. 묶음이 있으면 `CreateData()`, 컴포넌트 필수, 공격 칸이면 `CanUseAttackClip`.
- `CanUseAttackClip(attack)`: AC-5 전에는 Melee만(P5). AC-5는 이 함수 하나를 고친다.
- `RequireEventClip(clip)`: 길이 유한한 양수·반복 금지·루트 곡선 금지.
- `RequireNoRootCurves(clip, owner)`(`#if UNITY_EDITOR` 안): 경로가 빈 Transform 곡선이면 실패. 플레이어 빌드에서는 이 검사만 빠진다(설계 I3).
- 문구 머리말 `[CombatUnitAnimationRules]`.

### T7 시험

- `T/CombatClipEventCursorTests.cs`(새, 관리 실행): 0초 이벤트는 첫 호출에, 구간 경계 (이전, 지금], 한 번에 여러 개를 시각 순으로, 같은 시각의 순서, End 뒤 없음, 잘못된 값 거절.
- `T/CombatUnitAnimationSettingsTests.cs`(새, Unity — 메모리 `AnimationClip`과 `clip.events`라 관리 실행에서는 NATIVE):
  - 바른 묶음 → 시각·선딜레이가 그대로 담김
  - 이벤트 빠짐·중복·순서·범위·모르는 함수 이름 → 각각 실패 문구
  - 반복 클립·음수 선딜레이·빈 묶음 → 실패
  - `UnitVisualData`: 묶음 없음 → 지금처럼 통과(null), 컴포넌트 없음·둘 → 실패, 공격 칸 + 공격 SO 없음·Mortar·Linear → 실패, Melee → 통과
  - 묶음 없이 컴포넌트만(Idle만 쓰는 유닛): 통과, 컨트롤러 Idle 루트 곡선 → 실패, 컴포넌트 둘 → 실패(§8.2에서 더함)
  - 루트 곡선: 묶음 클립에 경로 빈 칸 곡선 → 실패, 프리팹 Animator 컨트롤러의 Idle 클립에 경로 빈 칸 곡선 → 실패, 자식 경로 곡선만 → 통과(컨트롤러는 메모리 `AnimatorController`로 만든다)
  - 테스트 자산은 `DataBaseTestAssets`로 만든다(`CombatAttackPresentationSettingsTests`와 같은 방식).
- 기존: `DataBaseAssetTests`(실제 카탈로그, 묶음을 쓰는 유닛이 없어 그대로 통과해야 함), `CombatAttackPresentationSettingsTests`.

### T8 공유 `docs/superpowers/handoffs/2026-10-19-unit-animation-clip-hojin-share.md` (새)

- 호진님께: `UnitVisualData`에 칸 하나(`animationPresentation`), 새 SO 형식, `DV/`의 첫 MonoBehaviour(P1 A, 이유), 검사 규칙, 에셋 작업 안내(클립마다 이벤트 셋, Transform만 움직임 P3, 컴포넌트는 Animator가 있는 GameObject에 하나). 미전달로 둔다.
- DB 폴더 MonoBehaviour의 범위(평가 G5): 프리팹에 붙는 작업 입력의 이름표·자리 표시와 그 규칙 함수(`FindIn`)만 둔다. 런타임 상태·`Update`·서비스 참조는 두지 않는다. 같은 종류가 또 생기면 같은 규칙을 따른다.
- 컨트롤러 작업 안내(설계 AC9): 컨트롤러에는 Idle만 둔다(Idle끼리 전환은 가능, 파라미터·공격·피격 상태·루트 모션 없음). Idle과 사건 클립은 Animator GameObject 자신의 Transform을 움직이지 않는다. 이벤트 컴포넌트가 붙은 유닛은 전투 중에 그 컨트롤러를 전투 코드가 넘겨받아 재생하고, 붙지 않은 유닛은 Unity가 평소처럼 재생한다. 유닛마다 Idle 클립만 다르면 Override Controller를 쓴다.

### AC-1 확인

- 관리 실행: T7 커서 시험 PASS, 기존 상태 변화 0.
- Unity Test Runner: `CombatUnitAnimationSettingsTests`, `CombatAttackPresentationSettingsTests`, `DataBaseAssetTests`.
- 재생: 바뀌는 화면이 없다(묶음을 쓰는 유닛 없음). 부트가 그대로 되는지만 본다.

## 3. AC-2 — 클립 채널·재생 단위 (설계 AC3·AC5·AC9·AC10, E5, P3)

### T9 대여 계약 `CD/ICombatUnitClip.cs` (새)

- `IsAvailable`
- `Play(AnimationClip clip)`: 사건 클립을 위층에 꽂고 시각 0. 바닥(Idle) 가중치는 0이 된다.
- `SetTime(float time)`: 사건 클립 시각만 정한다. **평가하지 않는다.** 평가는 그래프 주인이 한다(설계 I2, Q-AC10 A면 Unity).
- `RestoreClip()`: 위층을 빼고 바닥으로 돌아간다. 이미 끝났거나 다른 대여로 바뀌었으면 아무 일도 하지 않는다.
- remarks는 `ICombatUnitFlash`와 같은 수명 규칙이다.

### T10 채널 구현 `CD/UnitClipChannel.cs` (새, E5·AC9·AC10)

- `internal sealed class UnitClipChannel : IDisposable`, 생성자 `(CombatAnimationEvents events)`:
  - Animator·그 Transform·컨트롤러(`runtimeAnimatorController`, 없으면 null)를 읽는다. 그다음 Animator의 컨트롤러 칸을 비운다(우리 그래프 하나만 Animator를 움직이게, AC9). `applyRootMotion = false`, `fireEvents = false`, `cullingMode = AlwaysAnimate`.
  - 그래프를 만든다: `DirectorUpdateMode.GameTime`(Q-AC10 A), 출력 → `AnimationMixerPlayable`(입력 둘). 컨트롤러가 있으면 입력 0에 `AnimatorControllerPlayable`을 꽂고 가중치 1(기차 컨트롤러 길과 같은 생성).
  - 컨트롤러가 있으면 Animator를 켜고 그래프를 돌린다(`Play`). Idle이 Unity 시간으로 돈다.
  - 컨트롤러가 없으면 Animator 아래 Transform 로컬 값을 한 번 기억하고(P3) Animator를 끈 채 그래프를 멈춰 둔다.
- `Begin(AnimationClip clip)`: 입력 1에 `AnimationClipPlayable`(속도 0, IK 끔)을 꽂고 시각 0, 가중치는 입력 0 = 0·입력 1 = 1(바로 갈아 끼움). 컨트롤러가 없으면 Animator를 켜고 그래프를 돌린다. 이미 위층이 있으면 예외다.
- `SetTime(float time)`: 입력 1 클립의 시각만 바꾼다.
- `End()`: 입력 1을 빼서 파괴하고 입력 0 가중치를 1로 되돌린다. 컨트롤러가 없으면 그래프를 멈추고 Animator를 끄고 기억한 Transform 값을 되돌린다. 여러 번 불러도 안전하다.
- `Pause()`·`Resume()`: 표시가 꺼지고 켜질 때 그래프를 멈추고 다시 돌린다(컨트롤러가 있을 때만 다시 돌림).
- `Dispose()`: 그래프를 파괴한다. 여러 번 불러도 안전하다.
- 시험용 읽기(내부): `HasBase`, `HasOverlay`, `IsGraphValid`, `IsPlaying`.

### T10a 표시 대여 칸 부품 `CD/UnitPresentationLeaseSlot.cs` (새, Q-AC11 A, 동작 동일, AC-2 첫 Task)

- 칸 하나가 현재 대여·버전, 늦은 복원 판정(`IsCurrent(lease, version)`), 끝내기(`Reset(restore)`: 취소 콜백을 꺼내고 → 복원 → 버전 올림 → 취소 콜백, 복원 예외가 나도 `finally`로 뒤 둘을 함)를 갖는다.
- `FieldUnitView`의 자세·크기·플래시가 이 칸을 쓴다. 시작 검사 둘과 예외 문구, 대여의 스냅숏·복원 내용, `IsAvailable`의 채널별 조건은 채널 쪽에 그대로 둔다.
- 바뀌지 않는 것: 예외 문구, `OnDisable`·`Bind`·`ResetForPool`의 끝내기 순서, 취소 콜백 한 번, 늦은 복원 무시. 카메라 줌 대여는 건드리지 않는다(맵이 빠지면 되돌리지 않는 다른 규칙).
- 시험: 새 `T/UnitPresentationLeaseSlotTests.cs`(관리 실행, 순수 — 끝내기 순서·복원 예외 뒤에도 버전 올림과 취소 콜백 한 번·늦은 복원 무시·다시 빌리기). 기존 회귀는 AC-2 확인 목록과 같다.
- 순서: T10a를 먼저 끝내고(기존 세 채널만 옮긴 상태로 MSBuild·관리 실행 차이 0 확인) 그다음 T11이 클립 대여를 더한다. 한 Task 안에서 옮김과 새 채널이 섞이지 않게 한다.

### T11 표시 계약·표시 `CD/ICombatUnitPresentationTarget.cs`·`CD/FieldUnitView.cs`

- 계약: `bool SupportsClip { get; }`, `ICombatUnitClip BeginClip(Action cancelled)`(이전 대여가 남았거나 꺼진 표시면 예외).
- `FieldUnitView`:
  - `internal void PrepareClipChannel()`: 처음 한 번 `CombatAnimationEvents.FindIn(gameObject)`로 찾아 `UnitClipChannel`을 만든다. null이면 만들지 않고(`SupportsClip = false`), Animator도 건드리지 않는다. 그러면 Unity가 컨트롤러를 평소처럼 재생한다(설계 AC4). 규칙 위반의 예외는 `FindIn`이 낸다(평가 G2).
  - 대여 객체 `PresentationUnitClip : ICombatUnitClip`: 플래시 대여와 같은 ID·버전·취소 콜백 규칙이다. `Play` → `Begin`, `SetTime` → `SetTime`, `RestoreClip` → `End`.
    - T10a의 대여 칸을 쓴다(Q-AC11 A). 클립 대여에 남는 것은 채널 고유의 것(시작 검사 — 채널이 준비됐는지, `Play`·`SetTime`·`RestoreClip` 내용)뿐이다.
  - `ResetClip()`: 플래시의 `ResetHitFlash`와 같은 모양이다(대여 취소 + `End`). `Bind`(ID가 바뀜)·`ResetForPool`·`OnDisable`에 넣는다.
  - `OnDisable`에서 `Pause`, `OnEnable`에서 `Resume`(지금 HUD 동기화와 같은 자리).
  - `internal void ReleaseClipChannel()`: `Dispose`. 표시 집합 해제 때와 `OnDestroy`에서 부른다.

### T12 표시 집합·창구 `CD/ICombatUnitTargets.cs`·`CD/FieldUnitPresenter.cs`·`CD/CombatUnitPresentationWindow.cs`

- `ICombatUnitTargets.AnimationSettingsOf(EntityKey key)` → `CombatUnitAnimationData`(없으면 null). 이름은 같은 계약의 `AttackSettingsOf`·`ReactionSettingsOf`에 맞췄다(§0.7 CC4). `FieldUnitPresenter`는 `_visuals.GetUnitVisual(key).AnimationPresentation`을 돌려준다.
- `FieldUnitPresenter`는 궤적 TJ-3도 고친다(`ICombatUnitPlacementReader` 명시 구현). 먼저 착수한 쪽이 커밋한 뒤 다른 쪽이 고친다(§0.7 CC5).
- `FieldUnitPresenter`: 표시를 만들 때(`PrepareMovementAppearance` 옆) `view.PrepareClipChannel()`, 해제 때(`ReleaseMovementAppearance` 옆) `view.ReleaseClipChannel()`.
- 창구:
  - `TryBeginClip(EntityId id, Action cancelled, out ICombatUnitClip clip)`: 켜진 표시이고 `SupportsClip`일 때만.
  - 클립 묶음 값은 표시 준비 때 한 번 읽어 보관한다. 외형 표의 `AnimationPresentation`은 읽을 때마다 `clip.events`를 꺼내고 다시 검사하므로(에디터에서는 루트 곡선 목록까지), 사건마다 읽지 않는다(§8.2 FW6).
  - `TryGetHitClip(EntityId id, out CombatClipData clip)`: 켜진 표시의 피격 칸(반응 설정 읽기와 같은 조건).
  - `TryGetAttackClip(EntityId id, out CombatClipAttackData clip)`: 표시가 있으면 공격 칸(공격 설정 읽기 `TryGetAttackPresentation`과 같은 조건, P4).

### T13 재생 단위 `C/CombatClipPlayback.cs` (새)

- 생성자 `(ICombatUnitClip lease, CombatClipData data, Action started, Action eventTiming, Action ended)`. 콜백은 null이어도 된다(받는 곳이 없으면 null). 커서는 생성자에서 `data`의 세 시각으로 만든다(평가 G3).
- `Begin()`: `lease.Play(clip)` → 커서로 0초 이벤트 발화.
- `Tick(float deltaTime)`: 시각을 더하고 `lease.SetTime(min(시각, EndTime))` → 커서가 내는 이벤트를 순서대로 해당 콜백으로 부른다. End를 내면 `IsComplete`. 그 시각의 자세는 같은 프레임 Unity 평가에서 보인다(런타임이 `Update`에서 돌아 평가보다 앞섬, 설계 F16).
- `Cancel()`: 그 뒤로 콜백을 부르지 않는다. 콜백 안에서 취소되면 남은 발화를 멈춘다(버전 확인). 대여 복원은 하지 않는다(대여 주인의 일).
- 음수 시간은 0으로 본다. 끝난 뒤 Tick은 아무 일도 하지 않는다.

### T14 시험

- `T/CombatClipPlaybackTests.cs`(새, 관리 실행, 가짜 대여): 시작 0초 발화, 이벤트 구간 발화, 한 Tick에 여러 개, End에서 완료·넣는 시각은 `EndTime`에서 멈춤, 취소 뒤 없음, 콜백 안 취소.
- `T/UnitClipChannelTests.cs`(새, Unity): 메모리 클립(자식 Transform 곡선) + `CombatAnimationEvents`가 붙은 모델. EditMode에서는 Unity 시간이 흐르지 않으므로 그래프를 직접 `Evaluate(dt)`해 확인한다(`FieldRailwayPresenterTests` 방식).
  - **컨트롤러 있는 모델**(메모리 `AnimatorController`, Idle 상태 하나): 준비 뒤 Animator 컨트롤러 칸이 비고 Animator는 켜짐, 그래프가 돌고 Idle 곡선이 자식을 움직임
  - `Begin` → 위층 클립 자세(가중치 0/1), `SetTime`이 자세를 바꿈, `End` → 위층이 파괴되고 Idle 자세로 돌아감
  - **컨트롤러 없는 모델:** 준비 때 Animator 꺼짐, `Begin` 때 켜짐, `End` 뒤 자식 값이 재생 전과 같고(P3) Animator 꺼짐
  - `FieldUnitView` 대여: 풀 반환·다른 ID 재대여·비활성화에서 위층이 빠지고 취소 콜백 한 번, 비활성화 때 그래프 멈춤·재활성 때 다시 돎, 파괴·`ReleaseClipChannel` 때 그래프 파괴, 늦은 복원은 새 대여를 건드리지 않음
  - **컴포넌트 없는 모델:** `SupportsClip = false`, Animator의 컨트롤러 칸과 켜짐 상태를 건드리지 않음
- `T/CombatUnitPresentationWindowTests.cs`(기존, 사례 추가): `TryBeginClip`(꺼짐·미지원 false), `TryGetHitClip`·`TryGetAttackClip` 조건.
- 기존 회귀: `UnitModelAppearanceTests`(같은 리셋 길), 창구·HUD 피드백 시험, `FieldRailwayPresenterTests`.

### AC-2 확인

- 관리 실행: T10a 칸 시험·T14 관리 사례 PASS, 기존 상태 변화 0. T10a 직후(클립 대여 전)에 한 번, AC-2 끝에 한 번 본다.
- Unity Test Runner: `UnitClipChannelTests`, `CombatUnitPresentationWindowTests`, `UnitModelAppearanceTests`, `CombatStandardAttackTests`(표시 준비 길이 바뀜), `FieldRailwayPresenterTests`(같은 Playable 쓰임새), `BossRoarTests`(크기·자세 대여가 T10a 칸을 씀).
- 재생: 일반 전투 한 판(바뀌는 화면 없음. 이벤트 컴포넌트를 붙인 유닛이 없으므로 Animator는 그대로).

## 4. AC-3 — 신호 값·클립 조각·피격 클립 (설계 AC6, P7)

전제: 신호 장치(맥락 `SignalCallback`, 실행 `Signal`, 시간·미도착 규칙, 조각·조립 설계 BP2 보충, 조각 계약 문서)는 카메라 K-2가 만들었다(설계 F3). 이 Phase는 신호 값과 채널 분기, 조각만 더하고 신호 처리는 고치지 않는다.

### T15 신호 값 `C/CombatPieceStart.cs`

- `CombatPieceSignal`에 `ClipStart`·`ClipEvent`를 `Hit` 다음 번호로 더한다(P7, 번호는 들어온 순서). doc은 설계 AC6 신호 표의 뜻을 가리킨다.

### T16 클립 채널 `C/CombatComposition.cs`·`C/CombatPieceRunLeases.cs`·`C/CombatPieceContext.cs`

C6 분리(`3c50a379`) 기준이다(§0.7 CC1). `C/CombatPieceRun.cs`는 고치지 않는다.

- 조립: `CombatPieceChannel.Clip = 4`(유닛 채널, 장면 채널 100번대와 겹치지 않음). 열거 문서의 "클립 계획의 `Clip = 4`" 문구를 실제 값 설명으로 바꾼다. 적기는 기존 `Lease(unit, Clip)`를 쓴다(장면 채널이 아니라 전용 함수가 필요 없음).
- 대여 묶음: `TryBorrowOne`에 한 칸 — `_window.TryBeginClip(request.Unit, cancelled, out ICombatUnitClip clip)`이면 `Hold(request, clip, clip.RestoreClip)`. 꺼내기 한 줄 `Clip(EntityId unit)`(없으면 "조립이 빌리지 않은 클립 채널입니다" 예외, 다른 꺼내기와 같은 모양).
- 맥락: `Clip(unit) => _run.Leases.Clip(unit)`.
- 신호 처리(`Signal`·`SkipUnsignalled`)·빌리는 순서(유닛 채널 → 장면 채널)·되돌리는 순서는 그대로다.

### T17 클립 조각 `C/CombatClipPiece.cs` (새)

- 생성자 `(EntityId unit, CombatClipData clip)`.
- `Begin`: `context.Clip(unit)` 대여로 `CombatClipPlayback`을 만든다. `started` = `context.SignalCallback(ClipStart)`, `eventTiming` = `context.SignalCallback(ClipEvent)`, `ended`는 null(끝은 `IsEnded`)이다. 그다음 `Begin()`.
- `Tick` → 재생 단위 `Tick`, `IsEnded` → `IsComplete`, `Cancel` → 재생 단위 `Cancel`.
- 클립 대여는 실행이 실행 끝에 되돌린다(기존 규칙). 그래서 같은 실행의 다른 항목이 클립 끝보다 오래 가면, 그동안 모델은 끝 프레임 자세로 남았다가 실행이 끝날 때 Idle로 돌아간다. doc에 적는다.

### T18 피격 클립 `C/CombatCharacterEventHandler.cs`

- `ComposeHit`에 선택 인자 `CombatClipData hitClip = null`을 **`canFlash` 바로 뒤**에 더한다(§0.6 CK3, 유닛 재료 → 장면 재료 순). 서명은 `ComposeHit(subject, beginReaction, settings, canFlash, hitClip = null, camera = null, cameraMotion = null)`이다.
  - 카메라 인자를 자리로 넘기던 호출 둘을 고친다: `TryBeginHit`, `CombatHitCompositionTests`(카메라 흔들림 사례). 형식이 달라 빠뜨리면 컴파일 오류다.
  - 있으면 `Lease(subject, Clip)` + `Add(new CombatClipPiece(subject, hitClip), AtStart(), waits: true)`.
  - 플래시는 지금처럼 `AtStart`다(이벤트 타이밍에 맞추는 것은 이번 범위가 아니다). 피격 흔들림(카메라 K-4)도 `AtStart` 그대로다(해제가 피격 사건을 연 순간).
- `TryBeginHit`: `_presentation.TryGetHitClip(id, out var hitClip)`의 결과를 넘긴다.

### T19 시험

- 신호 장치 자체의 시험은 카메라 K-2가 했다(`CombatPieceRunTests`). 이 Phase는 새 값이 그 장치로 도는지만 본다.
- **주의(§8.3에서 확인):** 클립 조각·피격 조립은 `CombatClipData`(= `AnimationClip`)를 받으므로, 그 값을 만드는 사례는 관리 실행에서 NATIVE다(Unity 전용). 관리 실행으로 볼 수 있는 것은 클립 값 없이 도는 사례(대여 묶음 칸·꺼내기 예외·피격 클립 없음)뿐이다.
- `T/CombatClipPieceTests.cs`(새, 위 주의대로 대부분 Unity 전용, 가짜 클립 대여 `FakeClip`을 `CombatPieceRunTests`에 추가):
  - 대여를 실행 시작에 빌림, `ClipStart`(시작 안, 0초)·`ClipEvent`(Tick 안) 신호 순서, `After(clip, ClipEvent, 지연)` 항목이 신호 순간부터 지연을 셈
  - End → 실행 끝 → 클립 복원(바닥으로), 취소 → 복원
  - 피격 칸 대여 실패(표시 미지원) → 실행 시작 안 함
- `T/CombatPieceRunLeasesTests.cs`(기존, 사례에 클립 채널 추가, §0.7 CC3): 모든 채널 빌리기·되돌리기 순서(클립을 유닛 채널 사이에 적어도 적은 순서대로, 장면 채널은 뒤), 밖 취소 콜백 채널 목록, 꺼내기 예외 문구.
- `T/CombatHitCompositionTests.cs`(기존, 사례 추가): 피격 클립이 있으면 클립 끝까지 완료를 기다림, 없으면 지금과 같음.
- **기존 조립 전체 회귀(E14):** `CombatPieceRunTests`, `CombatPieceRunLeasesTests`(대여 분리), `CombatPieceRunPlayerTests`, `CombatPieceSignalTests`(카메라 K-2 신호 장치, §0.6 CK8), `CombatAbilityCompositionTests`(포효·소환), `CombatDeathCompositionTests`, `CombatHitCompositionTests`, `CombatFlashPieceTests`, `CombatScalePieceTests`, `CombatRisePieceTests`, `BossRoarTests`, 카메라 시험(`CombatCameraPieceTests`·`CombatCameraWindowTests`).

### AC-3 확인

- 관리 실행: T19 PASS, 기존 상태 변화 0.
- Unity Test Runner: 위 회귀 목록 + `CombatSceneEventPresenterTests`, `CombatStandardAttackTests`.
- 재생: 일반 전투·보스 전투 한 판씩(피격 클립을 쓰는 유닛이 없어 화면은 같아야 함).

## 5. AC-4 — 근접 클립 공격 (설계 AC7, E2·E3·E13·E15, P4·P5)

### T20 방향 공유 함수 `CD/CombatAttackFacing.cs` (새, E2)

- `internal static Quaternion Toward(Vector3 origin, Vector3 target, Vector3 up, Quaternion fallback)`: `CombatAttackMotion.Begin`의 계산(목표 쪽 수평 방향, 거의 0이면 `fallback`)을 그대로 옮긴다.
- `internal static Quaternion For(in AttackResult attack, FieldGridMapper mapper, Vector3 origin)`: `CombatAttackTarget.Resolve`로 목표를 구하고, 공격 방향(`RotationToWorld(AttackDirection)`)을 `fallback`으로 `Toward`를 부른다.
  - **실행 때 바뀜(§8.5):** 궤적 P-V1 뒤 외형별 목표점 원본이 `CombatShotPath.TryTarget`이라 `For(kind, attack, mapper, origin)`가 그것을 부른다(근접 결과는 같음).
- `CombatAttackMotion.Begin`은 `Toward`를 부른다(동작 같음).
- 궤적 TJ-1이 `CombatAttackTarget.Resolve`에 형태 하나를 더하고 옛 형태를 위임으로 바꿔도 `For`는 옛 형태를 부르면 된다. 목표 해석 원본은 그대로 `Resolve` 하나다. TJ-1은 `CombatAttackMotion`을 고치지 않는다(§0.7 CC5).

### T21 출처 수명 해제 입구 `C/ICombatSourceLifetime.cs`·`C/CombatAttackPlayback.cs`

- 계약에 `void ReleaseAll(CombatEvent source, int version)`을 더한다. 문서: "세대가 같을 때만 이 출처의 대상을 모두 해제합니다. 해제 규칙은 Gate가 가집니다."
- 공격 재생은 기존 `HitAll`과 같은 동작으로 구현한다(`version == _generation()`이면 `_gate.ReleaseAll(source)`). `HitAll`도 이것을 쓰게 해 한 곳에 둔다.
- `CombatPieceRunTests.FakeLifetime`에 기록을 더한다.

### T22 조각 둘 `C/CombatFaceTargetPiece.cs`·`C/CombatReleasePiece.cs` (새)

- `CombatFaceTargetPiece(EntityId attacker, Func<ICombatUnitPose, Quaternion> facing)`: 시간 없는 조각. `context.Pose(attacker)`에 `ApplyPose(pose.Position, facing(pose))`. 방향 계산은 처리기가 넘긴 함수(T20 `For`)라, 관리 실행 시험에서는 가짜 함수를 쓴다.
  - 이름은 기존 `CombatFacingAttackPiece`(정면 공격 모션 실행)와 헷갈리지 않게 정했다(§0.6 CK5).
- `CombatReleasePiece(Action release)`: 시간 없는 조각. 시작에서 `release()`를 부르고, 이어서 `context.SignalCallback(Hit)`으로 받은 콜백을 부른 뒤 끝난다(§0.6 CK4).
  - 순서는 정면 공격 모션(`TryBeginFacingMotion`)과 같은 "해제 먼저"다. 해제가 연 피격 사건과 `Hit`으로 시작한 항목이 같은 순간에 보인다.
  - `CombatPieceSignal.Hit` 문서를 고친다: 내는 조각에 해제 조각을 더하고, 순서 규칙("해제 뒤에 냄")을 그 문서 한 곳에 적는다.

### T23 클립 공격 조립 `C/CombatClipAttack.cs` (새, E3)

- `internal static CombatComposition Compose(CombatAttackEvent attack, CombatClipAttackData clip, Func<ICombatUnitPose, Quaternion> facing, Action release, CombatEventAudio sound)`
  - `ForAction(FromSource(attack, 공격자, FirstRelease))`, `Lease(공격자, Pose)`, `Lease(공격자, Clip)`
  - 항목: 0 방향 `CombatFaceTargetPiece`(`AtStart`) → 1 클립(`AtStart`) → 2 해제 `CombatReleasePiece`(`After(1, ClipEvent, clip.HitDelay)`, 해제 뒤 `Hit`) → 3 소리 `CombatEventSoundPiece(sound, attack)`(`After(1, ClipEvent, clip.SoundDelay)`)
  - doc에 설계 AC7 표를 요약하고, 이펙트·발사 항목은 쓰는 조각이 생길 때 더한다고 적는다(P6). 카메라 항목을 더할 때는 타격 순간에 `After(2, Hit)`로 건다(포효의 `After(facing, Hit)`와 같은 신호, §0.6 CK4·CK7).
- 포효가 나중에 이 항목 묶음을 쓸 수 있게 공개 범위는 `internal static`이다. 포효 처리기는 이번에 바꾸지 않는다.

### T24 공격 처리기 분기 `C/CombatAttackEventHandler.cs`·`C/CombatSceneEventPresenter.cs`

- 생성자에 `CombatPieceRunPlayer pieces`, `CombatUnitPresentationWindow presentation`을 더한다. Presenter 생성자(`attack:` 인자)에서 넘긴다. Factory는 바뀌지 않는다(F10).
- `TryBeginBeforeFacts`:
  - `attack.Attack.Succeeded`이고 `TryGetAttackClip(공격자, out clip)`이면 클립 길이다. `Compose(attack, clip, pose => CombatAttackFacing.For(attack.Attack, _mapper(), pose.Position), () => _attacks.ReleaseAll(attack, version), _sound)`를 `_pieces.TryBegin(composition, completed, version)`에 넘긴다.
    - false면 `InvalidOperationException("[CombatAttackEventHandler] 클립 공격 조립을 시작하지 못했습니다(공격자 표시가 꺼졌거나 클립을 쓸 수 없음): {공격자}")`(P4).
  - 아니면 지금처럼 `_attacks.TryBegin(...)`.
- 클래스 doc에 "두 길의 공통 규칙"(설계 AC7 표)을 짧게 적는다(E13).

### T25 시험

- `T/CombatClipAttackCompositionTests.cs`(새, 관리 실행, `Harness`·`FakeClip`·`FakeLifetime`):
  - 출처 시작(첫 해제 정책) → 방향 적용 → 클립 시작
  - 해제는 이벤트 타이밍 + 피격 선딜레이, 소리는 + 소리 선딜레이(선딜레이 0이면 그 프레임에 시간 0으로)
  - `Hit` 신호는 해제 직후 같은 순간이고, 피격 선딜레이가 0보다 크면 `ClipEvent`보다 늦다(`After(2, Hit)` 가짜 조각으로 확인, §0.6 CK4)
  - 전체 종료(출처 끝)는 클립 끝 이벤트 뒤, 자세·클립 대여 복원
  - 취소 → 출처 비움·대여 복원, 해제·소리 없음
  - 클립 대여 실패 → 시작 안 함(처리기가 예외로 바꿈)
- `T/CombatAttackFacingTests.cs`(새, Unity — `Quaternion.LookRotation`이 네이티브): 목표 앞쪽, 목표가 출발점과 같을 때 `fallback`, `For`가 옛 `TryBeginDirect` + `Begin`과 같은 회전.
- `T/CombatClipAttackSceneTests.cs`(새, Unity, `CombatStandardAttackTests` 방식):
  - 근접 공격자 외형 = Melee 공격 SO + 클립 묶음(메모리 클립: 시작 0, 이벤트 .2, 끝 .5, 피격 선딜레이 .05), 모델에 Animator + `CombatAnimationEvents`
  - 공격 결과(공격 + 피해자 피해 사건) 재생: 피해자 피격 반응은 .25초 이전에 시작하지 않고 그 프레임에 시작, 공격 소리는 소리 선딜레이 시점, 다음 행동 사건은 클립 끝 뒤
  - 같은 장면에서 클립 없는 공격자는 지금처럼 코드 모션
  - 공격자 표시를 끈 뒤 공격 → 코드 길과 같은 종류의 예외(P4)
- **회귀(E14):** `CombatStandardAttackTests`, `CombatHitGateTests`, `BossRoarTests`·`CombatAbilityCompositionTests`(출처 수명 계약 변경), `CombatSceneEventPresenterTests`, `CombatEventHandlerTableTests`, 공격 연출 시험(Ballista·곡사 포함).

### AC-4 확인

- 관리 실행: T25 관리 사례 PASS, 기존 상태 변화 0.
- Unity Test Runner: T25 Unity 사례 + 회귀 목록.
- 재생: 일반 전투·보스 전투 한 판씩(클립을 쓰는 유닛이 없어 화면은 같아야 함). 실제 근접 클립 재생은 클립이 들어온 뒤 확인 목록(§7)으로 옮긴다.

## 6. 공유·기록

- **Core:** 바꾸지 않으므로 Core 전달 문서에 적을 것이 없다.
- **민형님:** Factory·ServiceKeys·`ViewGroup.asset`을 바꾸지 않아 공유할 것이 없다.
- **호진님:** T8 메모(외형 표 칸, 새 SO, `DV/`의 첫 MonoBehaviour, 에셋 작업 안내). 미전달.
- **카메라 설계와의 순서(4차 갱신):** 신호 일반화는 카메라 K-2가 먼저 만들었다. 이 계획은 그 장치를 쓰고 값 둘만 더한다(§4). 실행 대여 묶음 분리(C6)도 끝나(`3c50a379`) AC-3은 묶음에 한 칸을 더한다(§0.7).
- **다른 진행 작업과 같은 파일(§0.7):** 궤적 TJ-3과 `CD/FieldUnitPresenter.cs`(CC5), 폴더 정리 M과 `C/`·`CD/` 전체(CC7, Q-M2). 먼저 착수한 쪽이 커밋한 뒤 다른 쪽이 고친다.
- **현강님(공유 메모 대상 아님, 보고만):** 이벤트 컴포넌트를 붙인 유닛은 전투 중 Animator의 컨트롤러 칸이 비워진다(우리 그래프가 넘겨받음, 설계 AC9). 프리팹 자체는 바뀌지 않는다.
- **커밋 때 함께 넣을 새 파일(각 `.meta` 포함):**
  - AC-1: 목록의 원본은 §8.1·§8.2의 커밋 묶음(검토 보완으로 `DV/CombatUnitAnimationRules.cs`가 더해짐).
  - AC-2: `CD/ICombatUnitClip.cs`, `CD/UnitClipChannel.cs`, `C/CombatClipPlayback.cs`, `T/CombatClipPlaybackTests.cs`, `T/UnitClipChannelTests.cs`, `CD/UnitPresentationLeaseSlot.cs`, `T/UnitPresentationLeaseSlotTests.cs`(T10a, Q-AC11 A)
  - AC-3: `C/CombatClipPiece.cs`, `T/CombatClipPieceTests.cs`
  - AC-4: `CD/CombatAttackFacing.cs`, `C/CombatFaceTargetPiece.cs`, `C/CombatReleasePiece.cs`, `C/CombatClipAttack.cs`, `T/CombatAttackFacingTests.cs`, `T/CombatClipAttackCompositionTests.cs`, `T/CombatClipAttackSceneTests.cs`

## 7. 실제 클립이 들어온 뒤 확인할 것 (완료 조건 아님)

- 실제 FBX 클립의 `clip.events`가 런타임에 읽히는지(모델 가져오기 설정에서 찍은 이벤트)
- 리그 일치: 다른 FBX의 클립이 유닛 모델을 실제로 움직이는지
- 근접 클립 공격: 이벤트 타이밍 + 피격 선딜레이에 피격자 반응이 시작되는지, 소리 시점, 클립 끝 뒤 다음 행동
- 피격 클립: 피격 반응(코드)과 함께 재생되는지, 끝까지 다음 사건을 기다리는지
- Idle: 입력 국면·결과 재생 중 모두 도는지, 공격·피격 클립으로 바로 바뀌고 끝나면 Idle로 돌아오는지, 바뀔 때 끊김이 거슬리는지(거슬리면 섞는 시간 값을 더함)
- 루트 곡선 검사가 실제 Idle·사건 클립에서 통과하는지
- 컨트롤러 없는 유닛의 휴식 자세: 클립 끝 뒤 원래 자세로 돌아오는지(P3, Transform 밖 속성 곡선이 없는지)
- 사망 반응 중 Idle이 어색하지 않은지(어색하면 사망 클립 칸 또는 사망 때 Idle 멈춤을 다음에 정함)
- 사건 클립이 끝나 위층을 뺀 뒤, Idle이 움직이지 않는 뼈(예: 무기 뼈)가 사건 클립의 마지막 자세에 남지 않는지(Animator가 묶음 속성을 다시 잡을 때의 동작, §8.3). 남으면 위층을 가중치 0으로 남겨 두거나 Idle에 그 뼈의 곡선을 넣는 쪽을 정함

## 8. 실행 기록

### 8.1 AC-1 데이터·이벤트 셋 (2026-10-19, 사용자 "AC-0 스킵하고 AC-1 진행해")

- **AC-0은 건너뜀(사용자 지시).** 그래서 관리 실행 기준선 비교(기존 시험 상태 변화)는 하지 않았다. 기존 시험은 Unity 묶음으로 확인한다(아래).
- **만든 것(T1~T6, 계획대로):**
  - T1 `C/CombatClipEventCursor.cs`(+ `CombatClipEventKind`), T2 `DV/CombatAnimationEvents.cs`, T3 `DV/CombatClipEvents.cs`(`Read`·`RequireNoRootCurves`), T4 `DV/CombatClipData.cs`·`DV/CombatClipAttackData.cs`·`DV/CombatUnitAnimationData.cs`, T5 `DV/CombatUnitAnimationSO.cs`, T6 `UV/UnitVisualDataSO.cs`(칸 `animationPresentation`, 읽기 `AnimationPresentation`, `Validate` → `ValidateAnimation`).
  - T7 시험 `T/CombatClipEventCursorTests.cs`(13), `T/CombatUnitAnimationSettingsTests.cs`(24, Unity 전용). T8 `docs/superpowers/handoffs/2026-10-19-unit-animation-clip-hojin-share.md`(미전달).
- **계획 글보다 구체화한 것(원칙 SSOT, 결정 변경 아님):**
  - 시각 규칙(유한, 0 ≤ 시작 ≤ 이벤트 ≤ 끝)의 원본을 `CombatClipData.FindTimeError` 하나로 두고, 값 생성자·이벤트 읽기(`Read`)·커서가 함께 부른다. 클립 길이 안 검사만 `Read`에 있다.
  - 선딜레이 규칙도 `CombatClipAttackData.IsValidDelay`·`DelayRule` 하나를 값 생성자와 SO가 함께 쓴다.
  - "묶음에 클립이 하나 이상" 규칙은 입력 SO에만 둔다(값은 둘 다 비어도 만들 수 있음 — 입력 계층의 뜻이라서).
  - `UnitVisualData.Validate`는 공격 설정을 한 번만 만들어 묶음 검사(근접만)에 넘긴다(기존 동작 같음).
  - 선딜레이는 공격 클립이 있을 때만 본다(공격 설정 SO가 종류별로 쓰는 값만 보는 것과 같은 방식).
- **검증:**
  - MSBuild: 작업 트리에 다른 세션의 진행 중 변경(궤적 TJ-3·경로 입력 분리)이 있어 그대로는 그쪽 파일에서 오류가 난다(`ICombatAttackTrajectoryFeedback` 등 csproj 밖 새 파일). 그래서 **격리 빌드**(남의 수정 `.cs` 27개는 HEAD 판, 남의 새 파일은 제외, 이 Phase 파일만 더함)로 확인했다: **오류 0**, 경고 19개는 모두 기존 파일(이 Phase 파일 0).
  - 관리 실행(Unity 내장 Mono, 메서드마다 따로): `CombatClipEventCursorTests` **13/13 PASS**. `CombatUnitAnimationSettingsTests` 24·`CombatAttackPresentationSettingsTests` 43·`ViewDataTableTests` 57은 모두 NATIVE(Unity 전용), FAIL 0.
  - 도구: 이 세션 스크래치 `ac1/isolate.sh`(격리 targets 생성), `tool/run.sh`(순번 실행). 다른 세션의 `Runner.exe`를 복사해 썼다.
- **Unity 확인(사용자):** Test Runner `CombatUnitAnimationSettingsTests`(새, 24)·`CombatClipEventCursorTests`(새, 13)·`CombatAttackPresentationSettingsTests`·`ViewDataTableTests`·`DataBaseAssetTests`(실제 카탈로그 — 묶음을 쓰는 유닛이 없어 그대로 통과해야 함). 재생은 부트·전투 진입이 그대로인지만 본다(화면 변화 없음).
- **관찰(다음 Phase):** 궤적 TJ-3이 지금 `CD/FieldUnitPresenter.cs`를 고치는 중이다(작업 트리). AC-2 T12가 같은 파일을 고치므로 그쪽 커밋 뒤에 T12를 한다(§0.7 CC5).
  - **갱신(§8.2 때):** 궤적 TJ-1~TJ-3은 `823aa3c9`, 경로 입력 분리는 `7994e518`로 커밋돼 이 조건과 "AC-4는 TJ-1 커밋 뒤"는 풀렸다. 작업 트리에 다른 세션의 Combat 코드 변경이 없어 격리 빌드도 필요 없었다. 남은 AC-2 착수 조건은 **AC-1 Unity 확인·커밋**과 **폴더 정리 Q-M2 순서** 둘이다.
- **커밋 때 함께 넣을 새 파일(각 `.meta`):** `C/CombatClipEventCursor.cs`, `DV/CombatAnimationEvents.cs`, `DV/CombatClipEvents.cs`, `DV/CombatClipData.cs`, `DV/CombatClipAttackData.cs`, `DV/CombatUnitAnimationData.cs`, `DV/CombatUnitAnimationSO.cs`, `T/CombatClipEventCursorTests.cs`, `T/CombatUnitAnimationSettingsTests.cs`. 고친 파일 `UV/UnitVisualDataSO.cs`. 문서: 공유 메모·이 계획. 같은 작업 트리의 다른 세션 변경은 이 묶음이 아니다. **§8.2에서 `DV/CombatUnitAnimationRules.cs`(+ `.meta`)가 더해졌다.**

### 8.2 AC-1 원칙 검토·보완 (2026-10-19, 사용자 "SSOT·SOLID·JMH Framework 지침·FW 계약·기능 변경의 독립성 기준으로 1-10 스케일 검토 및 보완해, 하위 에이전트 3개")

- 하위 에이전트 셋이 나눠 봤다: ① SSOT ② SOLID ③ FW 지침·계약 + 변경 독립성. 대상은 AC-1 코드·시험과 §0.8·공유 메모 둘. 지적은 코드를 다시 읽어 확인한 뒤 반영했다.

| 기준 | 검토 | 보완 뒤 | 남은 감점 |
|---|---|---|---|
| SSOT | 8 | 9.5 | 작업용 요약(메모)이 규칙을 다시 적음 — 원본 가리킴 줄을 달아 받아들임 |
| SOLID | 8 | 9.5 | `CombatClipAttackData.Clip`(값 `CombatClipData`)이라 `Attack.Clip.Clip`으로 읽힘(참고, 바꾸지 않음) |
| FW 지침·계약 | 9 | 9 | DB 층 첫 MonoBehaviour(결정 P1 A, 받아들임). Framework 지침 §2 표 반영은 민형님께 요청(메모 §5, 문서는 민형님 영역) |
| 변경 독립성 | 7 | 9 | 가져오기 설정(`.meta`)을 누가 맡을지 미정(민형님 메모 R4), 폴더 정리 Q-M2 순서 |
| **종합** | **8.0** | **9.3** | |

- **고친 것(코드):**
  - 클립 규칙을 호진님 파일 밖 `DV/CombatUnitAnimationRules.cs` 하나로 모음(SOLID S1·S3, 독립성 1, SSOT 2·3). `UnitVisualData.Validate`는 한 줄 호출만 남김. 사건 클립 규칙(길이·반복·루트 곡선)은 `RequireEventClip`이 원본이고 SO가 칸마다 부르므로, 칸을 더해도 검사가 빠지지 않음(SOLID S2). 근접만 규칙은 `CanUseAttackClip` 하나(AC-5는 이 함수만). `CombatClipEvents`는 이벤트 셋 읽기만.
  - **"Animator를 넘겨받는 유닛" 조건 통일(SSOT 1·SOLID S4):** 검사가 묶음 유무가 아니라 컴포넌트 유무로 돈다. 묶음 없이 컴포넌트만 붙은 유닛(Idle만 쓰는 원거리 아군 — AC-5 전 실제로 생길 형태)도 컴포넌트 규칙·컨트롤러 루트 곡선을 부트에서 본다. 지금 컴포넌트가 붙은 프리팹이 없어 기존 에셋 결과는 같다.
  - **커서 struct → `sealed class`(SOLID S5·FW 3):** `readonly` 필드에 담기면 복사본만 바뀌어 같은 이벤트를 계속 내는 함정을 없앰. 재생 시작에 할당 한 번, `TryNext`는 할당 0 그대로. 순서는 열거 값에서 끌어냄(SSOT 5).
  - 문구·주석: SO 툴팁은 규칙 클래스를 가리킴(SSOT 3), 함수 이름은 `cref`로(SSOT 7), `FindIn` 문서에 특성 둘은 편집 도움 장치임을 적음(SSOT 6).
  - 시험: 복사한 문구 대신 이름 상수·`FindTimeError` 사용(SSOT 4), 컴포넌트만 붙은 경우 셋 추가(통과·Idle 루트 곡선 실패·둘 실패), 커서 시험은 클래스에 맞게 `ref` 제거.
- **고친 것(문서):** P5·T1·T3·T5·T6(+ T6a)·T7·T12(표시 준비 때 한 번 읽기, FW 6) 글, §6 파일 목록은 §8.1·§8.2를 가리키게, 착수 조건 갱신(궤적 `823aa3c9`·경로 입력 `7994e518` 커밋으로 풀림), 설계 AC1 DB 검사 줄, 호진님 메모(경로 `Script/`, 규칙 원본 줄, 컴포넌트만 붙은 유닛, "유한한"), 민형님 메모(테이크 이름 바뀌면 참조가 조용히 끊김, 루트 곡선은 에디터에서만, Framework 문서 반영 부탁 §5), 폴더 정리 대응표 `Presentation/Pieces/`에 `CombatPieceRunLeases`·`CombatClipEventCursor`.
- **바꾸지 않은 것:** SOLID S7(이름 겹침, 참고), FW 4(Framework 문서는 민형님께 요청만).
- **검증:** 다른 세션 Combat 코드가 커밋돼 격리 없이 빌드. Unity가 csproj를 다시 만들어 일부 새 파일이 들어가 있어, 추가 targets는 "빼고 다시 넣기"로 바꿈. MSBuild **오류 0**, 경고 19개는 모두 기존 파일. 관리 실행 `CombatClipEventCursorTests` **13/13 PASS**, `CombatUnitAnimationSettingsTests` 27개(새 3 포함)는 Unity 전용(NATIVE), FAIL 0.
- **Unity 확인(사용자):** §8.1 목록 그대로(`CombatUnitAnimationSettingsTests`는 이제 27개).
- **Unity 확인 결과(2026-10-19, 사용자 "문제 없음 확인 진행해"):** AC-1 시험 문제 없음. 이어서 AC-2를 T10a부터 진행했다(§8.3).

### 8.3 AC-2 클립 채널·재생 단위 (2026-10-19, 사용자 "문제 없음 확인 진행해" — AC-2를 T10a부터)

- **T10a 표시 대여 칸 부품(Q-AC11 A, 동작 동일):** `CD/UnitPresentationLeaseSlot.cs`(`Hold`·`IsCurrent`·`Release`·`Reset(restore)`). `FieldUnitView`의 자세·크기·플래시가 이 칸을 쓴다. 대여 클래스에는 시작 조건·스냅숏·복원 내용만 남았다.
  - 계획 글과 다른 점: 늦은 복원 판정을 버전 숫자 대신 **대여 객체 같음**(`ReferenceEquals`)으로 했다. 대여마다 새 객체라 뜻이 같고, 칸이 버전과 대여를 따로 맞출 필요가 없다. 끝내기 순서(취소 콜백 꺼냄 → 복원 → 칸 비움 → 취소 콜백, 복원 예외에도 `finally`)는 계획 그대로.
  - 기존 시험 고침 둘(대여 칸으로 옮긴 직접 결과): `CombatMortarPresentationTests`(5줄)·`CombatStandardAttackTests`(1줄)가 리플렉션으로 읽던 private 필드 `_presentationPose`가 없어져, 같은 값을 주는 내부 읽기 `FieldUnitView.CurrentPresentationPose`로 바꿨다(구조 평가 R8 — private 필드 결합 하나가 줄어듦).
  - 검증(T10a 직후, 클립 대여 전): MSBuild 오류 0·새 경고 0. 관리 실행 바뀐 클래스 묶음 기존 476개 상태 변화 0(PASS 139·NATIVE 335·UNSUPPORTED 2), 새 칸 시험 8개 PASS.
- **만든 것(T9~T14):**
  - T9 `CD/ICombatUnitClip.cs`(계획대로).
  - T10 `CD/UnitClipChannel.cs`: 계획대로(컨트롤러 칸 비움·`GameTime` 그래프·섞개 입력 둘·컨트롤러 없으면 Animator 끔과 휴식 자세 기억·바로 갈아 끼움·`Pause`/`Resume`). 구체화한 것:
    - `Dispose`가 넘겨받은 **컨트롤러와 Animator 켜짐 상태를 돌려준다.** 표시 집합 해제(`FieldUnitPresenter.Release`) 뒤에도 풀의 표시는 남고 다음 대여 때 채널을 다시 만들기 때문에, 돌려주지 않으면 둘째 채널이 컨트롤러를 못 찾아 Idle을 잃는다(시험 `ViewReleaseClipChannel_…_CanPrepareAgain`).
    - 휴식 자세 기억에서 Animator GameObject 자신의 Transform은 뺀다(루트 곡선 금지, Animator가 루트에 붙으면 자세 대여 자리와 겹침).
    - 시험용 읽기는 `HasBase`·`HasOverlay`·`Graph`(계획의 `IsGraphValid`·`IsPlaying`은 `Graph.IsValid()`·`IsPlaying()`, EditMode 시험이 `Graph.Evaluate`를 직접 부름).
  - T11 `CD/ICombatUnitPresentationTarget.cs`(`SupportsClip`·`BeginClip`), `CD/FieldUnitView.cs`(클립 대여 `PresentationUnitClip`은 T10a 칸 사용, `PrepareClipChannel`·`ReleaseClipChannel`·`ResetClip`, `OnDisable` 끝내기 + `Pause`, `OnEnable` `Resume`, `OnDestroy`에서 해제). 구체화한 것: 이벤트 컴포넌트 찾기는 인스턴스마다 한 번(대여마다 `GetComponentsInChildren`를 다시 부르지 않음), `ReleaseClipChannel`은 남은 대여를 먼저 끝냄(취소 콜백 한 번 규칙 유지), 시험·진단용 읽기 `ClipChannel`.
  - T12 `CD/ICombatUnitTargets.cs`(`AnimationSettingsOf`), `CD/FieldUnitPresenter.cs`(대여 준비 `Rent`·`CreateView`에서 `PrepareClipChannel`, 해제 `Release`에서 `ReleaseClipChannel`, 오류 묶음 문구에 "클립 채널"), `CD/CombatUnitPresentationWindow.cs`(`TryBeginClip`·`TryGetHitClip`·`TryGetAttackClip`). 클립 묶음 값 보관(FW6)은 **`FieldUnitPresenter`가 외형 키별로** 한다: `Rent` 때 채우고, 없으면 처음 물을 때 채우며, `Configure`가 비운다(외형 표가 바뀔 수 있어 `_bodyShapes`와 같은 수명).
  - T13 `C/CombatClipPlayback.cs`(계획대로). 시작 전 `Tick`·두 번째 `Begin`은 예외, 시작 전 취소면 `Begin`이 클립을 꽂지 않는다.
  - T14 시험: `T/CombatClipPlaybackTests.cs`(새 11), `T/UnitClipChannelTests.cs`(새 9), `T/CombatUnitPresentationWindowTests.cs`(새 3).
- **계획과 다른 점(보고):**
  - `CombatClipPlaybackTests`는 **Unity 전용**이다. 계획은 관리 실행으로 적었지만 클립 값(`CombatClipData`)이 `AnimationClip`을 요구해 Unity 밖에서는 만들 수 없다. 시각 규칙 자체는 `CombatClipEventCursorTests`(관리 실행)가 본다.
  - 시험 가짜 넷: 계획 §1의 셋 + 궤적 작업 뒤로 생긴 `CombatAttackTrajectoryTests.FakeTargets`(한 줄). `CombatPieceRunTests.FakeTarget`은 `SupportsClip = false`·`BeginClip` 예외(클립 조각은 AC-3), 되돌리기 실패 플래그 둘은 그대로(CC3).
- **관찰(다음 Phase·실제 클립 때):**
  - 위층을 빼면 Animator가 묶음 속성을 다시 잡는다. Idle이 움직이지 않는 뼈는 사건 클립의 마지막 자세에 남을 수 있다 — 실제 클립 확인 목록 §7에 더했다.
  - 컨트롤러 없는 유닛에서 Animator가 표시 루트(`FieldUnitView`와 같은 GameObject)에 붙으면 체력 글자 등 표시 자식도 휴식 자세 되돌림 대상이 된다. 지금 모델 구조(Animator는 FBX 자식, §0.8)에서는 생기지 않는다.
  - Animator 넘겨받기 설정 넷(컨트롤러 칸·루트 모션·이벤트·컬링)이 기차 `TrackVisual`과 같은 줄로 두 곳에 있다(낮음, 쓰임이 달라 지금은 그대로).
- **원칙 자체 점검(하위 에이전트 없이, 9.3 목표 대비):** SSOT 9(대여 수명 규칙은 칸 하나를 네 채널이 공유, 시각 규칙은 커서·`FindTimeError`, 컴포넌트 규칙은 `FindIn`, 묶음 값 보관은 표시 집합 한 곳 / 감점: 위 Animator 설정 두 곳) · SOLID 8.5(그래프·대여 수명·시각과 이벤트를 세 클래스로 / 감점: 표시 계약에 멤버 둘이라 가짜 넷 수정, `FieldUnitView`의 대여 종류 다섯) · FW 9(Factory·ServiceKeys 무변경, `Tick`·`SetTime` 할당 0, 그래프는 해제·파괴·풀 반환 모든 길에서 끝남) · 독립성 9(조각 실행·처리기 무변경, 컴포넌트 없는 유닛은 Animator를 건드리지 않아 화면 같음 / 감점: 다른 작업 시험 가짜 한 줄) = **약 8.9**. 9.3 목표의 남은 차이는 AC-3에서 클립 조각이 실행 대여 묶음에 한 칸으로 들어가는지(§0.7)로 다시 본다.
- **검증:**
  - MSBuild(작업 트리의 `.cs` 변경이 모두 이 작업이라 격리 없이): **오류 0**, 경고 19개는 T10a 때와 같음(새 경고 0). 산출 DLL에 새 형식 들어감.
  - 관리 실행(바뀐 클래스 묶음 31개 클래스, T10a 때 묶음 + `CombatAttackTrajectoryTests`·새 시험 클래스): 기존 **495개 상태 변화 0**(PASS 152·NATIVE 341·UNSUPPORTED 2, 기준 = T10a 산출물 bin4). 새 시험: 창구 2 PASS·1 NATIVE, `CombatClipPlaybackTests` 11·`UnitClipChannelTests` 9는 NATIVE(모두 `AnimationClip` 생성 internal call에서 멈춤을 출력으로 확인), `CombatClipEventCursorTests` 13 PASS. FAIL 0.
- **Unity 확인(사용자):** Test Runner `UnitClipChannelTests`(새 9)·`CombatClipPlaybackTests`(새 11)·`CombatUnitPresentationWindowTests`(새 3 포함)·`UnitPresentationLeaseSlotTests`(새 8)·`UnitModelAppearanceTests`·`CombatStandardAttackTests`·`CombatMortarPresentationTests`(T10a 리플렉션 고침)·`FieldRailwayPresenterTests`·`BossRoarTests`·`CombatPieceRunTests`·`CombatPieceRunLeasesTests`·`CombatUnitHudFeedbackTests`·`CombatAttackTrajectoryTests`. 재생: 일반 전투 한 판(화면 변화 없음 — 포효 크기·자세, 피격 플래시, 이동 반투명이 그대로인지).
- **커밋 때 함께 넣을 새 파일(각 `.meta`):** `CD/UnitPresentationLeaseSlot.cs`, `CD/ICombatUnitClip.cs`, `CD/UnitClipChannel.cs`, `C/CombatClipPlayback.cs`, `T/UnitPresentationLeaseSlotTests.cs`, `T/CombatClipPlaybackTests.cs`, `T/UnitClipChannelTests.cs`. 고친 파일: `CD/FieldUnitView.cs`, `CD/FieldUnitPresenter.cs`, `CD/CombatUnitPresentationWindow.cs`, `CD/ICombatUnitPresentationTarget.cs`, `CD/ICombatUnitTargets.cs`, 시험 `CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`·`CombatUnitHudFeedbackTests`·`CombatAttackTrajectoryTests`. AC-1 묶음(§8.1·§8.2)이 아직 커밋 전이면 AC-2가 그것을 쓰므로 함께 넣는다.
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 AC-2 시험 문제 없음 확인했어. AC-3 진행해."):** AC-2 시험 문제 없음. 이어서 AC-3을 진행했다(§8.4).

### 8.4 AC-3 신호 값·클립 조각·피격 클립 (2026-10-19, 사용자 "AC-3 진행해", 진행 중 "MS build 스킵")

- **만든 것(T15~T19, 계획대로):**
  - T15 `C/CombatPieceStart.cs`: `CombatPieceSignal.ClipStart = 3`·`ClipEvent = 4`(doc은 설계 AC6 신호 표의 뜻, `ClipEvent`에는 선딜레이 = `After(클립 항목, ClipEvent, 선딜레이)`와 `Hit`과 다른 사실임을 적음). 신호 처리(`Signal`·미도착 규칙)는 고치지 않았다.
  - T16 `C/CombatComposition.cs`(`CombatPieceChannel.Clip = 4`, 열거 문서의 "클립 계획의 `Clip = 4`" 문구를 실제 규칙 설명으로), `C/CombatPieceRunLeases.cs`(`TryBorrowOne` 한 칸 + 꺼내기 `Clip(unit)`), `C/CombatPieceContext.cs`(`Clip(unit)` 한 줄). `C/CombatPieceRun.cs`는 고치지 않았다(§0.7 CC1 그대로).
  - T17 `C/CombatClipPiece.cs`(새): AC-2 재생 단위를 감싸고 시작·이벤트 타이밍을 신호로 낸다. 끝은 `IsEnded`. 대여는 실행이 실행 끝에 되돌리므로 다른 항목이 더 길면 끝 프레임 자세로 남는다는 점을 doc에 적음.
  - T18 `C/CombatCharacterEventHandler.cs`: `ComposeHit(subject, beginReaction, settings, canFlash, hitClip = null, camera = null, cameraMotion = null)`. 피격 클립이 있으면 `Lease(subject, Clip)` + 클립 조각(같은 순간, 기다림). 항목 순서는 반응 → 플래시 → 클립 → 흔들림(유닛 재료 → 장면 재료). `TryBeginHit`은 `TryGetHitClip` 결과를 넘긴다. 플래시·피격 흔들림은 `AtStart` 그대로.
  - T19 시험: `T/CombatClipPieceTests.cs`(새 7 — 시작 안 0초 `ClipStart`, Tick 안 `ClipEvent`(그 프레임 시간 없음), 선딜레이는 신호부터 셈, 끝 이벤트 뒤에도 실행 끝까지 대여 유지·넣는 시각은 끝 시각에서 멈춤, 취소 → 복원, 클립 채널 없는 표시 → 실행 시작 안 함, 인자 검사), `T/CombatPieceRunLeasesTests.cs`(기존 사례에 클립 채널 — 빌리기·되돌리기 순서, 꺼내기 문구, 밖 취소 다섯 채널), `T/CombatHitCompositionTests.cs`(새 사례 1 — 피격 클립이 있으면 클립 끝까지 완료를 기다림, 카메라를 자리로 넘기던 두 곳을 이름 있는 인자로), `T/CombatPieceRunTests.cs`(가짜 표시에 클립 대여 `FakeClip`, 되돌리기 실패 플래그 둘은 그대로 CC3).
- **계획과 다른 점:**
  - 계획은 `CombatHitCompositionTests`의 카메라 자리 인자 고칠 곳을 "카메라 흔들림 사례" 하나로 적었지만, 시험 도우미 `Begin`도 자리로 넘기고 있어 두 곳을 고쳤다(형식이 달라 빠뜨리면 컴파일 오류).
  - T19는 §4 주의대로 클립 값을 만드는 사례가 Unity 전용이다(새 클립 조각 시험 7 중 6, 피격 클립 사례 1). 클립 값 없이 도는 사례(대여 묶음 시험의 클립 칸, 인자 검사)만 관리 실행 대상이다.
- **검증:**
  - **MSBuild·관리 실행 안 함(사용자 "MS build 스킵").** 첫 빌드 시도에서 난 오류 하나(`CombatClipPiece` 형식을 찾을 수 없음)는 추가 targets에 새 파일 `CombatClipPiece.cs`를 넣지 못한 도구 쪽 문제였다(스크래치 targets 파일 갱신 실패). 코드 문제는 아니지만 **컴파일은 확인하지 않았다** — Unity 컴파일로 확인한다.
  - 대신 시험의 기대값을 실행 코드와 대조했다: 시작 안 신호는 `StartItem`이 `Begin` 뒤 낸 순서대로 처리해 기다리던 항목을 그 자리에서 시작(새 시험 첫 사례의 기록 순서), 취소는 진행 중인 항목만 `Cancel`(취소 사례의 기록이 `clip.Restore` 하나).
- **Unity 확인(사용자):** 컴파일, Test Runner `CombatClipPieceTests`(새 7)·`CombatHitCompositionTests`(새 1 포함)·`CombatPieceRunLeasesTests`, 회귀 `CombatPieceRunTests`·`CombatPieceRunPlayerTests`·`CombatPieceSignalTests`·`CombatAbilityCompositionTests`·`CombatDeathCompositionTests`·`CombatFlashPieceTests`·`CombatScalePieceTests`·`CombatRisePieceTests`·`BossRoarTests`·`CombatCameraPieceTests`·`CombatCameraWindowTests`·`CombatSceneEventPresenterTests`·`CombatStandardAttackTests`. 재생: 일반 전투·보스 전투 한 판씩(피격 클립을 쓰는 유닛이 없어 화면 같음).
- **원칙 자체 점검:** SSOT 9(신호 뜻은 설계 AC6 표가 원본·열거 doc은 가리킴, 채널 대여 수명은 대여 묶음 한 칸) · SOLID 9(조각은 재생 단위를 감싸기만, 실행 무변경 — 새 채널이 묶음 한 칸 + 꺼내기 한 줄 + 맥락 한 줄로 끝남, §0.7 목표 그대로) · FW 9(Factory·ServiceKeys 무변경, Tick 할당 0 — 조각 시작 때 재생 단위·커서·신호 콜백 둘만) · 독립성 9(피격 클립이 없으면 조립이 지금과 같음, 감점: 공개 조립 함수 서명 변경으로 시험 두 곳 수정) = **약 9.0**.
- **커밋 때 함께 넣을 새 파일(각 `.meta`):** `C/CombatClipPiece.cs`, `T/CombatClipPieceTests.cs`. 고친 파일: `C/CombatPieceStart.cs`, `C/CombatComposition.cs`, `C/CombatPieceRunLeases.cs`, `C/CombatPieceContext.cs`, `C/CombatCharacterEventHandler.cs`, 시험 `CombatPieceRunTests`·`CombatPieceRunLeasesTests`·`CombatHitCompositionTests`. AC-1·AC-2 묶음이 커밋 전이면 함께(AC-3이 그것을 씀).
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-3 시험 문제 없음 확인했어. AC-4 진행해."):** AC-3 컴파일·시험 문제 없음. 이어서 AC-4를 진행했다(§8.5).

### 8.5 AC-4 근접 클립 공격 (2026-10-19, 사용자 "AC-4 진행해")

- **MSBuild·관리 실행 안 함:** AC-3 진행 중 사용자 지시("MS build 스킵")를 이 Phase에도 따랐다. **컴파일·시험은 Unity에서 확인한다.**
- **만든 것(T20~T25):**
  - T20 `CD/CombatAttackFacing.cs`(새): `Toward(origin, target, up, fallback)`는 `CombatAttackMotion.Begin`의 계산을 그대로 옮긴 것이고, 코드 모션이 이 함수를 부른다(동작 같음). `For(kind, attack, mapper, origin)`는 목표점·공격 방향·정규화한 Mapper 위쪽으로 `Toward`를 부른다 — 코드 모션 길과 같은 입력이라 같은 회전.
  - T21 `C/ICombatSourceLifetime.cs`에 `ReleaseAll(source, version)`, `C/CombatAttackPlayback.cs`가 구현하고 기존 `HitAll`도 이것을 부름(해제 입구 한 곳). `CombatPieceRunTests.FakeLifetime`에 기록.
  - T22 `C/CombatFaceTargetPiece.cs`(새, 빌린 자세의 지금 위치에서 방향만 씀), `C/CombatReleasePiece.cs`(새, 해제 뒤 `Hit`). `CombatPieceSignal.Hit` 문서에 내는 조각 둘과 "해제 뒤에 냄" 규칙을 원본으로 적음.
  - T23 `C/CombatClipAttack.cs`(새): `ForAction(FromSource(공격, 공격자, 첫 해제))` + 자세·클립 채널, 항목 0 방향(시작) → 1 클립(시작) → 2 해제 `After(1, ClipEvent, 피격 선딜레이)` → 3 소리 `After(1, ClipEvent, 소리 선딜레이)`. doc에 설계 AC7 표 요약·카메라는 `After(2, Hit)`.
  - T24 `C/CombatAttackEventHandler.cs`: 생성자에 조각 실행 재생기·유닛 표시 창구, 성공한 공격이고 공격 클립이 있으면 클립 길, 시작 못 하면 예외(P4), 클래스 doc에 두 길의 공통 규칙(E13). `C/CombatSceneEventPresenter.cs`는 생성 한 줄(Factory 무변경).
  - T25 시험: `T/CombatClipAttackCompositionTests.cs`(새 7 — 시작 순서, 해제·소리 시점, `Hit`은 해제 직후·이벤트 타이밍보다 늦음, 전체 종료 = 클립 끝 뒤 대여 복원 → 출처 끝, 취소, 클립 채널 없음 → 시작 안 함, 인자 검사), `T/CombatAttackFacingTests.cs`(새 6, Unity), `T/CombatClipAttackSceneTests.cs`(새 2, Unity — 실제 표시·Presenter·순차 재생으로 피격 시점·소리 시점·전체 종료·Animator 끔, 표시 꺼진 공격자 예외).
- **계획과 다른 점(보고):**
  - **방향 함수의 목표점 원본:** 계획 T20은 `CombatAttackTarget.Resolve`를 부른다고 적었지만, 궤적 평가 개선 P-V1(`823aa3c9` 뒤)로 외형별 목표점 원본이 `CombatShotPath.TryTarget`이 됐고 코드 모션도 그것을 부른다. 그래서 `For`는 `TryTarget`을 부르고 외형 종류를 인자로 받는다(근접은 `Resolve`로 가서 결과가 같음). `Resolve`를 직접 부르면 외형별 분기가 두 벌이 된다(궤적 평가 V1과 같은 문제). 처리기는 공격 연출 설정의 종류를 넘긴다(클립 묶음 검사가 공격 설정을 요구하므로 항상 있음, 없으면 예외).
  - `CombatClipAttackSceneTests`는 `CombatStandardAttackTests`를 가져다 쓰지 않고 같은 조립을 파일 안에 둔다. 그 준비가 표시를 먼저 만들어, 뒤에 클립 묶음·이벤트 컴포넌트를 넣을 수 없기 때문이다(표시 준비 때 채널·묶음 값을 정함, §8.3).
  - 계획의 "같은 장면에서 클립 없는 공격자는 코드 모션" 사례는 따로 두지 않았다. 코드 모션 길은 기존 `CombatStandardAttackTests`·`CombatCodeMotionHitTests`가 그대로 지킨다(회귀).
  - 조립 시험은 §4 주의대로 클립 값을 만드는 사례가 Unity 전용이다(7 중 6). 방향 시험은 계획대로 Unity 전용.
- **시험 기대값 대조(빌드 없이):** 출처 완료 실행은 대여 → 출처 열기 → 항목 순(`SourceCompletion_OpensTheSourceBeforeItems_AndClosesAfterRestoring`), 끝은 대여 복원 → 출처 끝, 취소는 진행 중 조각 취소 → 출처 비움 → 대여 복원(`Stop`), 시작 안 신호는 `Begin` 뒤 처리(해제 → `Hit`), 지연은 신호 프레임 시간을 빼고 셈(`CombatPieceSignalTests`). 클립 길의 Gate 열기는 코드 길(`BeginAnimation`)과 같은 `CombatHitGate.Begin`(공격 사건 판은 행위자를 꺼내 같은 함수를 부름).
- **원칙 자체 점검:** SSOT 9(방향 계산·목표점·해제 입구·`Hit` 순서 규칙이 각 한 곳, 감점: 두 길의 공통 규칙 표가 설계 AC7과 처리기 doc 요약 두 곳) · SOLID 9(조각은 시간 없는 작은 동작, 조립은 정적 함수 하나, 처리기는 분기만, 출처 수명 계약에 입구 하나) · FW 9(Factory·ServiceKeys 무변경, Tick 할당 0, 공격당 조립 할당은 포효와 같은 수준) · 독립성 9(클립이 없으면 코드 길 그대로, 포효 처리기 무변경, 감점: 공격 처리기 생성자 서명 변경 — 부르는 곳 한 곳) = **약 9.0**.
- **Unity 확인(사용자):** 컴파일, Test Runner `CombatClipAttackCompositionTests`(새 7)·`CombatAttackFacingTests`(새 6)·`CombatClipAttackSceneTests`(새 2), 회귀 `CombatStandardAttackTests`·`CombatCodeMotionHitTests`·`CombatHitGateTests`·`BossRoarTests`·`CombatAbilityCompositionTests`·`CombatSceneEventPresenterTests`·`CombatEventHandlerTableTests`·`CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatPieceRunTests`·`CombatPieceSignalTests`. 재생: 일반 전투·보스 전투 한 판씩(클립을 쓰는 유닛이 없어 화면 같음). 실제 근접 클립 재생은 §7.
- **커밋 때 함께 넣을 새 파일(각 `.meta`):** `CD/CombatAttackFacing.cs`, `C/CombatFaceTargetPiece.cs`, `C/CombatReleasePiece.cs`, `C/CombatClipAttack.cs`, `T/CombatAttackFacingTests.cs`, `T/CombatClipAttackCompositionTests.cs`, `T/CombatClipAttackSceneTests.cs`. 고친 파일: `CD/CombatAttackMotion.cs`, `C/ICombatSourceLifetime.cs`, `C/CombatAttackPlayback.cs`, `C/CombatPieceStart.cs`, `C/CombatAttackEventHandler.cs`, `C/CombatSceneEventPresenter.cs`, 시험 `CombatPieceRunTests`. 폴더 정리 계획 대응표에 AC-2~AC-4 새 파일 줄을 더함. AC-1~AC-3 묶음이 커밋 전이면 함께.
- **계획 AC-1~AC-4 끝.** 남은 것: AC-5(원거리 발사 조각, 첫 원거리 클립 뒤 따로 계획), 실제 클립이 들어온 뒤 확인 목록 §7, 민형님 모델 재내보내기 요청(§0.8, 미전달).
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-4 시험 문제 없음 확인했어."):** AC-4 컴파일·시험 문제 없음. 계획 AC-1~AC-4 코드·Unity 확인 모두 끝(커밋은 사용자).

## 9. AC-5 — 원거리(곡사·직선) 클립 공격 계획 (2026-10-19, 설계 5차 §10)

- 설계: 5차 §10(AC11~AC15), 결정 Q-AC13 B(곡사 + 직선)·Q-AC14~Q-AC17 A(§10.5). 사실 F19~F26.
- 상태: **Task 계획 작성, 코드 착수 전.** AC-1~AC-4는 Unity 확인 완료(§8.5).
- 공통 규칙은 §1 그대로다. 단, 이 Phase는 AC-1 시험(`CombatUnitAnimationSettingsTests`)의 "직선·곡사 공격 칸 거절" 사례를 규칙 변경의 직접 결과로 고친다.

### 9.0 계획에서 정한 것(보고, 설계 뜻은 그대로)

| # | 정한 것 | 이유 |
|---|---|---|
| P9 | 포구 위치는 **클립 대여**(`ICombatUnitClip.TryGetMuzzle`)로 읽는다. 표시 계약(`ICombatUnitPresentationTarget`)·창구에 멤버를 더하지 않는다 | 포구는 클립 채널과 같은 컴포넌트(`CombatAnimationEvents`)에 있고, 발사 조각은 이미 그 유닛의 클립 대여를 빌린다. 계약 하나·가짜 넷을 덜 고친다 |
| P10 | "발사 조각을 쓰는 공격 종류"(Linear·Mortar) 판정 함수 하나 `CombatUnitAnimationRules.FiresProjectile(kind)`를 DB 검사(포구 필수·피격 선딜레이 0)와 조립 분기가 함께 부른다 | 원거리 판정이 두 곳에 생기지 않게(SSOT) |
| P11 | 직선 클립의 출발점은 포구 그대로(코드 길의 0.2 앞 오프셋 없음), 종점은 목표점을 발사 높이만큼 올린 점(코드 길과 같음) | 포구가 이미 포신 끝이다. 종점 높이는 피격 몸 높이라 코드 길과 같게 둔다 |
| P12 | 출처 발사만 진행 중일 때 공격 재생 `Tick`은 다른 진행을 막지 않는다(반환 true). 막기·완료는 조각 실행과 Gate가 맡는다 | 클립 공격의 막기는 포효처럼 행동 실행이 한다(AC7) |
| P13 | 피격 선딜레이 0 검사는 클립 묶음 SO가 아니라 외형 행 검사(`ValidateUnit`)에서 한다 | 공격 종류는 외형 행이 알고, 묶음은 여러 유닛이 공유할 수 있다 |

### 9.0b 구조 검사 반영(설계 §11.3, 결정 2026-10-19)

사용자: "Q-AC18·Q-AC19 모두 A로 하고 K3·K16·K17도 AC-5에 넣어. 계획 고친 뒤 AC-5a부터 진행해." P9~P13은 그대로.

| # | 반영 | 자리 |
|---|---|---|
| Q-AC18 A | 곡사 코드 모션도 `CombatAttackFacing.Toward`, 빈 방향 = 공격 방향(생성자로 받음) | AC-5b T29c |
| Q-AC19 A | 투사체 진행(풀 사전·Tick·회수·출처 발사 기록)을 `CombatProjectileFlights`로 먼저 분리(동작 동일), 출처 발사는 그쪽에 | AC-5b T29a·T29b |
| K3 | 클립 공격 조립 입구 둘(`ComposeMelee`·`ComposeRanged`, 공통 앞부분 private 하나) | AC-5b T31 |
| K16 | "해제 뒤 `Hit`" 함수 하나 `CombatHitMoment.ReleaseThenSignal(release, signal)`를 정면 공격 모션·해제 조각·발사 조각이 부름 | AC-5b T29d |
| K17 | 설계 AC8 값의 자리 표에 "발사 지점" 줄 | 설계(반영함) |

### AC-5a — 발사 지점·검사 규칙

**T26 `DV/CombatAnimationEvents.cs`**
- 선택 칸 `[SerializeField, Tooltip("원거리(직선·곡사) 공격 클립의 발사 지점입니다(포구 뼈·빈 GameObject). 근접만 쓰는 유닛은 비워 둡니다.")] private Transform muzzle;`, 읽기 `internal Transform Muzzle`.
- 클래스 doc의 DB 층 MonoBehaviour 범위(평가 G5)에 "발사 지점 자리 표시"를 더한다.

**T27 `DV/CombatUnitAnimationRules.cs`**
- `FiresProjectile(CombatAttackPresentationKind kind)`: Linear·Mortar면 true(P10).
- `CanUseAttackClip(attack)`: Melee·Linear·Mortar(Ballista 제외, AC11). doc의 "AC-5 전에는 근접만" 문구를 고친다.
- `ValidateUnit`: 공격 칸이 있고 `FiresProjectile(attack.Kind)`이면 `events.Muzzle`이 있어야 하고 `animation.Attack.HitDelay == 0`이어야 한다(P13, 설계 AC14). 문구에 유닛·이유.
- `DV/CombatClipAttackData.cs`·`DV/CombatUnitAnimationSO.cs`: 피격 선딜레이 doc·툴팁에 "원거리는 0(발사 = 이벤트 타이밍, 규칙은 `CombatUnitAnimationRules`)" 한 줄.

**T28 포구 읽기 `CD/ICombatUnitClip.cs`·`CD/UnitClipChannel.cs`·`CD/FieldUnitView.cs`**
- 계약 `bool TryGetMuzzle(out Vector3 position)`: 포구가 없으면 false. 그 프레임 Unity 평가 전 위치다(설계 AC13).
- 채널이 생성 때 `events.Muzzle`을 기억하고 읽기를 준다. 대여(`PresentationUnitClip`)는 `RequireAvailable` 뒤 채널에 묻는다.
- 시험 가짜 셋(`CombatPieceRunTests.FakeClip`·`CombatUnitPresentationWindowTests` 안 `FakeClip`·`CombatClipPlaybackTests.FakeClip`)에 멤버를 더한다.

### AC-5b — 출처 발사 입구·발사 조각·조립

**T29a `C/CombatProjectileFlights.cs`(새, Q-AC19 A, 동작 동일) — AC-5b 첫 Task**
- 공격 재생의 투사체 부분을 옮긴다: 프리팹별 풀 사전(`PoolOf(prefab)`, 지금 세 곳의 "없으면 만들기"를 한 곳으로), 프레임 진행(`Tick(deltaTime, Func<bool> stillCurrent)` — 지금처럼 스냅숏 뒤 풀마다 진행하고 세대가 바뀌면 멈춤), 정리(`Clear`·`Release`).
- 공격 재생은 `_flights`를 갖고, 코드 모션 넷은 지금처럼 풀을 받는다. 공격 재생 `Tick`의 풀 진행은 `_flights.Tick`, 나머지(무기·재장전·Gate·코드 모션)는 그대로.
- 확인: 기존 공격 시험 묶음(`CombatStandardAttackTests`·`CombatCodeMotionHitTests`·`CombatMortarPresentationTests`·`CombatBallistaAttackTests`·`CombatBallistaAssetTests`·`CombatAttackPresenterTests`)이 그대로여야 한다. 이 Task만 따로 Unity 확인을 받으면 좋다(옮김과 새 기능 분리).

**T29b `C/ICombatShot.cs`(새)·`C/CombatProjectileFlights.cs`·`C/CombatAttackPlayback.cs` — 출처 발사**
- `internal interface ICombatShot { void Recall(); }` — 쏜 투사체 하나의 회수 손잡이.
- 투사체 진행 쪽: `FireArc`·`FireLinear` 출처 발사(콜백·회수 손잡이·발사 기록). 진행 조건 = "코드 모션 진행 중 **또는** 출처 발사 진행 중"(P12).
- 공격 재생 쪽 `internal ICombatShot FireShot(CombatAttackEvent source, CombatAttackPresentationData settings, Vector3 from, FieldGridMapper mapper, Action hit, Action arrived, int version)`(설계 AC12) — 목표점·접촉 진행률·세대 확인은 공격 재생, 날리기·회수는 투사체 진행:
  - Mortar: 목표점 `CombatShotPath.TryTarget(Mortar, …)`(없으면 코드 길과 같은 예외), `FireArc(from, target, FlightDuration, ArcHeight, WorldUp, Spin, …)`, 착탄 → `hit` → `arrived`.
  - Linear: 목표점 `TryTarget(Linear, …, origin = from)`을 발사 높이만큼 올림(P11), 접촉 진행률 `LinearContactProgress(source, from, to, mapper)`, `FireLinear(from, to, speed, Vector3.up, 진행률 → 지나면 hit 한 번, 도착 → (hit 안 했으면 hit) → arrived)`.
  - 그 밖의 종류는 예외. 콜백은 세대가 같을 때만. 풀은 프리팹별 공유 사전(`_projectiles`)을 그대로 쓴다.
- 출처 발사 기록 `_shots`(쏜 것·세대). `Tick`: 풀 Tick 조건이 `_attack.IsPlaying || _shots.Count > 0`, `_attack.Tick`은 코드 모션이 돌 때만, 출처 발사만 있으면 반환 true(P12). 도착·회수 때 기록에서 뺀다.
- `Cancel`·`Release`: 기록을 비운다(투사체는 지금처럼 풀 정리가 회수).
- 클래스 doc에 "출처 발사(클립 공격)" 한 줄.

**T29c `CD/CombatMortarAnimation.cs`·`C/CombatAttackPlayback.cs`(Q-AC18 A)**
- 곡사 코드 모션의 방향을 `CombatAttackFacing.Toward(origin, target, 정규화한 위쪽, 빈 방향)`로. 빈 방향은 생성자 인자(공격 방향 `mapper.RotationToWorld(AttackDirection)`)로 받는다(지금은 유닛 지금 방향). 곡사는 주 칸이 있어야 쏘므로 화면 변화는 없어야 한다.

**T29d `C/CombatHitMoment.cs`(새, K16)**
- `internal static Action ReleaseThenSignal(Action release, Action signal)`: 해제 뒤 신호를 부르는 동작 하나. doc은 `CombatPieceSignal.Hit` 문서를 가리킨다(규칙 문장은 그곳 하나).
- 정면 공격 모션(`TryBeginFacingMotion`의 람다), 해제 조각, 발사 조각이 부른다.

**T30 `C/CombatFirePiece.cs`(새)**
- 생성자 `(EntityId attacker, Func<Vector3, Action, Action, ICombatShot> fire, Action release)`. 외부 진행 조각이다.
- `Begin`: `context.Clip(attacker).TryGetMuzzle(out from)`(없으면 예외 — DB 검사가 막으므로 규칙 위반), `hitSignal = context.SignalCallback(Hit)`, `end = context.EndCallback()`, `_shot = fire(from, CombatHitMoment.ReleaseThenSignal(release, hitSignal), end)`.
- `Tick`은 없음, `IsEnded`는 false(끝은 콜백), `Cancel`은 `_shot?.Recall()`.

**T31 `C/CombatClipAttack.cs`·`C/CombatAttackEventHandler.cs`·`C/CombatPieceStart.cs`(K3)**
- 입구 둘: `ComposeMelee(attack, clip, facing, release, sound)`(AC-4 `Compose`의 이름 바꿈) · `ComposeRanged(attack, clip, facing, fire, release, sound)`(항목 2 = 발사 조각 `After(1, ClipEvent)`, 선딜레이 없음, 설계 AC14). 공통 앞부분(출처·대여·방향·클립 항목)과 소리 항목은 private 함수 하나씩.
- 처리기: 공격 연출 설정의 종류로 `CombatUnitAnimationRules.FiresProjectile(kind)`면 `ComposeRanged`(발사 입구 = `(from, hit, arrived) => _attacks.FireShot(attack, settings, from, mapper, hit, arrived, version)`), 아니면 `ComposeMelee`.
- `CombatPieceSignal.Hit` doc: 내는 조각에 발사 조각을 더한다(맞음 때 해제 뒤).
- 기존 시험 `CombatClipAttackCompositionTests`의 `Compose` 호출을 `ComposeMelee`로(이름 바꿈의 직접 결과).

### AC-5c — 시험·공유

**T32 시험**
- `T/CombatUnitAnimationSettingsTests.cs`(기존 사례 고침 — 규칙 변경의 직접 결과): 직선·곡사 공격 칸은 포구 있고 피격 선딜레이 0이면 통과, 포구 없음·피격 선딜레이 > 0 → 실패, Ballista → 실패.
- `T/CombatClipAttackCompositionTests.cs`(사례 추가, Unity 전용): 원거리 — 이벤트 타이밍에 가짜 `fire`가 포구 위치로 불림, 맞음 콜백 → 해제 → `Hit`, 도착 콜백 → 실행 끝(대여 복원 → 출처 끝), 취소 → `Recall`, 포구 없음 → 예외.
- `T/CombatClipShotTests.cs`(새, Unity): 공격 재생 `FireShot` — 곡사 착탄에 맞음 → 도착, 직선 몸 접촉 진행률에 맞음 뒤 도착, 코드 모션 없이도 풀 Tick·Tick 반환 true, 세대가 바뀌면 콜백 없음, `Recall`·`Cancel` 뒤 투사체 없음.
- `T/CombatClipAttackSceneTests.cs`(사례 추가, Unity): 곡사 클립 공격자(포구 지정) — 이벤트 타이밍 전 포탄 없음, 이벤트 뒤 포탄, 착탄 때 피격, 전체 종료.
- 회귀: AC-4 목록 + `CombatMortarPresentationTests`·`CombatBallistaAttackTests`·`CombatStandardAttackTests`(공격 재생 Tick 변경), `CombatAttackTrajectoryTests`.

**T33 공유·요청(미전달로 둠)**
- 민형님: `CannonEnemy` 프리팹(FBX + `CombatAnimationEvents`(Animator와 같은 GameObject) + `muzzle` 지정 + Idle 컨트롤러 `CannonIdle`만), 공격 클립 이벤트 셋 찍기(`CannonAttack`·`CannonAttackBody` 중 쓸 것). 기존 모델 요청 메모에 절을 더한다.
- 호진님: `UnitVisual_UndeadCannonCart` 항목(곡사 공격 SO·클립 묶음: 공격 칸 = 공격 테이크, 피격 선딜레이 0, 소리 선딜레이). 기존 공유 메모에 절을 더한다.
- Core: `Unit_UndeadCannonCart`(·`Unit_DynamiteHammer`)가 `UnitDataTable`에 없음 — 데이터 경계 전달 문서에 요청으로 적는다(Core 수정은 하지 않음).
- 폴더 정리 계획 대응표: `CombatFirePiece`·`ICombatShot`.

### AC-5 확인

- 빌드: 사용자 지시가 없으면 AC-3·AC-4처럼 MSBuild를 건너뛰고 Unity 컴파일로 확인한다(지시가 바뀌면 그대로).
- Unity Test Runner: T32 목록 + 회귀.
- 재생: 일반 전투·보스 전투 한 판씩(클립을 쓰는 유닛이 없어 화면 같음). 실제 곡사 클립 재생은 에셋(T33)이 들어온 뒤 — 이벤트 위치·포구·포탄 출발 모양·착탄 시점을 본다.
- **커밋 때 새 파일(각 `.meta`):** `C/CombatProjectileFlights.cs`, `C/ICombatShot.cs`, `C/CombatHitMoment.cs`, `C/CombatFirePiece.cs`, `T/CombatClipShotTests.cs`.
- 폴더 정리 대응표 줄: `CombatProjectileFlights`·`ICombatShot`·`CombatHitMoment`(`Presentation/Attack/`), `CombatFirePiece`(`Presentation/Pieces/`).

### 9.1 AC-5a 실행 기록 (2026-10-19, 사용자 "계획 고친 뒤 AC-5a부터 진행해")

- **계획 고침(먼저):** 설계 §11.3에 결정 기록(Q-AC18 A·Q-AC19 A·K3·K16·K17), 설계 AC8 값의 자리 표에 "발사 지점" 줄(K17), 계획 §9.0b와 AC-5b Task(T29a 투사체 진행 분리 → T29b 출처 발사 → T29c 곡사 방향 → T29d 해제 뒤 신호 함수 → T30 발사 조각 → T31 조립 입구 둘).
- **만든 것(T26~T28, 계획대로):**
  - T26 `DV/CombatAnimationEvents.cs`: 선택 칸 `muzzle`(툴팁은 규칙 클래스를 가리킴)·읽기 `Muzzle`, 클래스 doc의 DB 층 범위에 "원거리 발사 지점".
  - T27 `DV/CombatUnitAnimationRules.cs`: `FiresProjectile(kind)`(직선·곡사, P10), `CanUseAttackClip` = 근접 또는 `FiresProjectile`(Ballista 제외), `ValidateUnit`이 원거리 공격 칸이면 포구 필수·피격 선딜레이 0(P13). `DV/CombatClipAttackData.cs`(`HitDelay` doc)·`DV/CombatUnitAnimationSO.cs`(툴팁)에 "원거리는 0" 한 줄.
  - T28 `CD/ICombatUnitClip.cs`(`TryGetMuzzle`, P9), `CD/UnitClipChannel.cs`(생성 때 포구를 기억, `TryGetMuzzle`), `CD/FieldUnitView.cs`(클립 대여가 가용성 확인 뒤 채널에 물음).
- **시험:**
  - 기존 고침(규칙 변경의 직접 결과): `CombatUnitAnimationSettingsTests`의 "근접 아닌 공격 칸 거절" 사례에서 직선·곡사를 빼고(공격 설정 없음·Ballista만 남김, 문구 "Ballista 제외"), 도우미 `Visual`·`Unit`에 포구 지정 인자.
  - 새 사례: 같은 파일 7(직선·곡사 × 포구·선딜레이 0 통과 / 포구 없음 실패 / 피격 선딜레이 > 0 실패, `FiresProjectile` 판정), `UnitClipChannelTests` 2(클립 대여로 포구 읽기, 포구 없는 채널).
  - 시험 가짜 셋에 `TryGetMuzzle`(`CombatPieceRunTests.FakeClip`은 `Muzzle` 지정 가능, 나머지 둘은 false).
- **검증:** MSBuild·관리 실행 안 함(AC-3의 사용자 지시를 이어 감). **컴파일은 Unity로 확인한다.**
- **원칙 자체 점검(7기준):** SSOT 9(원거리 판정·포구·선딜레이 규칙이 각 한 곳) · SOLID 8.5(클립 대여에 포구 읽기, K4 받아들임) · FW 9 · 독립성 9(Ballista는 판정 함수 하나로 제외) · 캡슐화 8.5(포구는 private 직렬화 칸 + 내부 읽기) · C#·Unity 9(`SerializeField` + 툴팁, 프리팹 안 Transform 참조) · If·Tick 9(프레임 경로 변화 없음, 포구는 발사 때 한 번 읽음) = **약 8.9**.
- **Unity 확인(사용자):** 컴파일, Test Runner `CombatUnitAnimationSettingsTests`(새 7 포함)·`UnitClipChannelTests`(새 2 포함)·`CombatClipPlaybackTests`·`CombatUnitPresentationWindowTests`·`CombatPieceRunTests`·`CombatClipPieceTests`·`CombatClipAttackCompositionTests`·`DataBaseAssetTests`(실제 카탈로그 — 클립 묶음을 쓰는 유닛이 없어 그대로). 재생은 바뀌는 화면이 없다.
- **커밋:** AC-5a는 새 파일 없음. 고친 파일: `DV/CombatAnimationEvents.cs`, `DV/CombatUnitAnimationRules.cs`, `DV/CombatClipAttackData.cs`, `DV/CombatUnitAnimationSO.cs`, `CD/ICombatUnitClip.cs`, `CD/UnitClipChannel.cs`, `CD/FieldUnitView.cs`, 시험 `CombatUnitAnimationSettingsTests`·`UnitClipChannelTests`·`CombatClipPlaybackTests`·`CombatUnitPresentationWindowTests`·`CombatPieceRunTests`, 문서(설계·이 계획).
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-5a 시험 문제 없음 확인했어. AC-5b 진행해. T29a(투사체 진행 분리) 끝나면 한 번 멈춰줘."):** AC-5a 문제 없음.

### 9.2 AC-5b 실행 기록

#### T29a 투사체 진행 분리 (2026-10-19, 동작 동일 — 사용자 지시로 여기서 멈춤)

- **만든 것:** `C/CombatProjectileFlights.cs`(새). 프리팹별 풀 사전(`PoolOf`), 프레임 진행(`Tick(deltaTime, version)` — 풀 사본을 떠서 차례로 진행하고 콜백이 재생 세대를 바꾸면 남은 풀을 건너뛰고 false), 정리(`Clear`·`Release`).
- **옮긴 것(`C/CombatAttackPlayback.cs`, 514 → 478줄):** 필드 `_projectileRoot`·`_projectiles`·`_projectileTickSnapshot`과 정리 함수 `ClearProjectiles`·`ReleaseProjectiles`를 지우고 `_flights` 하나로. 풀 "없으면 만들기" 세 곳(직선·Ballista·곡사)은 `PoolOf` 한 줄. `Tick`의 풀 진행은 `_flights.Tick`, `Cancel`·`Release`는 `_flights.Clear`·`_flights.Release`. 클래스 doc에 "투사체는 투사체 진행에 맡김".
- **동작이 같은 근거(코드 대조):**
  - 생성자 예외 순서·종류 같음(창구 → 투사체 부모 → 세대 → 몸 조회). 투사체 부모의 null 판정은 Unity null 연산 그대로.
  - 풀을 만드는 시점 같음(직선은 자세 대여 전, Ballista는 무기 기록 뒤, 곡사는 목표점 확인 뒤). 프리팹이 null이면 지금도 사전 조회가 `ArgumentNullException`이라 같음.
  - 진행: 사본·순서·세대 확인·`false` 반환·`finally`의 사본 비우기와 `_ticking` 해제가 같음. 세대 확인은 생성 때 받은 세대 읽기로 하고, 프레임마다 클로저를 만들지 않는다(할당 0 유지).
  - 정리 순서 같음(`Cancel`: 코드 모션 → 투사체 → Ballista 무기, `Release`: 코드 모션 → 투사체, 풀 사전은 해제 때 비움).
  - **다른 점 하나:** 투사체 정리가 실패했을 때 안쪽 묶음 예외의 문구가 `[CombatAttackPlayback] 공격 표시 정리에 실패했습니다.` → `[CombatProjectileFlights] 투사체 정리에 실패했습니다.`(바깥 문구는 그대로). 이 문구를 단정하는 시험은 없다(검색).
- **검증:** MSBuild·관리 실행 안 함(사용자 지시 이어 감). **컴파일·회귀는 Unity로 확인한다.**
- **원칙 자체 점검(7기준):** SSOT 9.5(풀 만들기·진행·정리가 한 곳, 세 벌이던 "없으면 만들기"가 하나) · SOLID 9(공격 재생은 Gate·코드 모션·무기·접촉, 투사체 진행은 따로) · FW 9.5(프레임 할당 0 유지) · 독립성 9(투사체 진행 방식이 바뀌어도 공격 재생은 그대로) · 캡슐화 9(풀 사전이 투사체 진행 안에 숨음) · C#·Unity 9 · If·Tick 9(검사·순서 같음) = **약 9.1**.
- **Unity 확인(사용자, 이 Task만):** 컴파일, Test Runner `CombatStandardAttackTests`·`CombatCodeMotionHitTests`·`CombatMortarPresentationTests`·`CombatBallistaAttackTests`·`CombatBallistaAssetTests`·`CombatBallistaLifecycleTests`·`CombatAttackPresenterTests`·`CombatSceneEventPresenterTests`·`CombatDisplayReuseTests`·`BossRoarTests`. 재생: 직선(ShotGun 등)·곡사(CactusCannon)·Ballista 공격이 있는 전투 한 판(화면 같음).
- **커밋 때 새 파일(각 `.meta`):** `C/CombatProjectileFlights.cs`. 고친 파일: `C/CombatAttackPlayback.cs`.
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 T29a 회귀 시험 문제 없음 확인했어. AC-5b 나머지(T29b부터) 진행해."):** T29a 문제 없음.

#### T29b~T31 (2026-10-19)

- **T29b 출처 발사:**
  - `C/ICombatShot.cs`(새): 회수 손잡이 `Recall()`.
  - `C/CombatProjectileFlights.cs`: `HasShots`, 출처 발사 `FireArc`·`FireLinear`(맞음 진행률 `hitAt`), 발사 기록(내부 `Shot`: 맞음 한 번, 도착 때 아직 안 맞았으면 맞음 먼저 → 도착, 세대가 다르면 콜백 없음, `Recall`은 그 투사체만 회수), `Clear`·`Release`가 기록도 비움(콜백 없음).
  - `C/CombatAttackPlayback.cs`: `FireShot(source, settings, from, mapper, hit, arrived, version)` — 곡사 = `TryTarget(Mortar)` 주 칸 착탄(없으면 코드 길과 같은 예외), 직선 = `TryTarget(Linear, …, from)`을 발사 높이만큼 올린 종점 + 코드 길과 같은 `LinearContactProgress`(유한 확인 → 0~1), 그 밖의 종류는 예외. `Tick`은 코드 모션이 없을 때 출처 발사가 있으면 `TickShotsOnly`(투사체만 진행, 다른 진행을 막지 않음 — 세대가 바뀌면 false, P12).
- **T29c 곡사 방향(Q-AC18 A):** `CD/CombatMortarAnimation.cs` 생성자에 빈 방향 인자(`TryBeginMortar`가 공격 방향을 넘김), `Begin`의 방향은 `CombatAttackFacing.Toward(…, 정규화한 위쪽, 빈 방향)`. 기존 시험 `CombatMortarAnimationTests`의 생성 한 줄에 인자를 더함(직접 결과, 목표가 출발점 위·아래가 아니라 값은 쓰이지 않음).
- **T29d 해제 뒤 신호(K16):** `C/CombatHitMoment.cs`(새) `ReleaseThenSignal(release, signal)`. 정면 공격 모션(`TryBeginFacingMotion`)·해제 조각이 이 함수를 쓰도록 고침, 발사 조각도 씀.
- **T30 발사 조각:** `C/CombatFirePiece.cs`(새) — 시작에서 클립 대여의 포구(없으면 예외), 맞음 콜백 = `ReleaseThenSignal(release, Hit 신호)`, 도착 콜백 = 끝 입구, 취소 = 회수.
- **T31 조립 입구 둘(K3)·처리기:** `C/CombatClipAttack.cs`를 `ComposeMelee`(AC-4 `Compose` 이름 바꿈)·`ComposeRanged`(항목 2 = 발사 조각 `After(1, ClipEvent)`)로, 공통 앞부분 `BeginClip`·소리 `AddSound`는 private 하나씩. `C/CombatAttackEventHandler.cs`가 `CombatUnitAnimationRules.FiresProjectile(종류)`로 고르고 원거리면 발사 입구 `FireShot`을 넘김. `C/CombatPieceStart.cs` `Hit` 문서에 발사 조각·`CombatHitMoment`. 기존 시험 `CombatClipAttackCompositionTests`의 `Compose` 호출 셋을 `ComposeMelee`로(이름 바꿈의 직접 결과).
- **계획과 다른 점:** 없음. 새 동작 시험(출처 발사·발사 조각·원거리 조립·곡사 장면)은 계획대로 AC-5c에서 쓴다.
- **검증:** MSBuild·관리 실행 안 함(사용자 지시 이어 감). 컴파일·회귀는 Unity로 확인한다.
- **원칙 자체 점검(7기준):** SSOT 9(원거리 판정·목표점·접촉 진행률·해제 뒤 신호·방향 계산이 각 한 곳, 곡사 방향 두 벌 해소) · SOLID 8.5(투사체 날리기·기록은 투사체 진행, 목표·맞는 순간은 공격 재생 — 공격 재생은 `FireShot`·`TickShotsOnly`가 더해져 다시 커짐(478 → 533줄, T29a 전 514줄보다 큼 — 출처 발사의 목표·접촉 규칙이 코드 길과 같은 곳에 있어야 해서), 조립 입구 둘) · FW 9(프레임 할당 0, 발사 때 기록 하나) · 독립성 9(근접·코드 길 그대로, 새 원거리 종류는 판정 함수 + 입구 한 갈래) · 캡슐화 9(발사 기록은 투사체 진행 안의 private 클래스, 밖에는 회수 손잡이만) · C#·Unity 9 · If·Tick 8.5(코드 모션 없을 때 `HasShots` 한 번 확인, 직선 맞음 진행률은 공격 재생·투사체 진행 두 곳에서 범위 확인) = **약 8.9**.
- **Unity 확인(사용자):** 컴파일, 회귀 `CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatCodeMotionHitTests`·`CombatBallistaAttackTests`·`CombatAttackPresenterTests`·`BossRoarTests`·`CombatAbilityCompositionTests`·`CombatPieceSignalTests`·`CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`·`CombatSceneEventPresenterTests`. 재생: 직선·곡사·Ballista·보스 포효가 있는 전투 한 판(화면 같음).
- **커밋 때 새 파일(각 `.meta`):** `C/ICombatShot.cs`, `C/CombatHitMoment.cs`, `C/CombatFirePiece.cs`(+ T29a의 `C/CombatProjectileFlights.cs`). 고친 파일: `C/CombatAttackPlayback.cs`, `CD/CombatMortarAnimation.cs`, `C/CombatReleasePiece.cs`, `C/CombatClipAttack.cs`, `C/CombatAttackEventHandler.cs`, `C/CombatPieceStart.cs`, 시험 `CombatMortarAnimationTests`·`CombatClipAttackCompositionTests`.
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-5b 회귀 시험 문제 없음 확인했어. AC-5c 진행해."):** AC-5b 문제 없음.

### 9.3 AC-5c 시험·공유 실행 기록 (2026-10-19)

- **코드 고침 하나(시험 설계 중 발견):** `C/CombatProjectileFlights.cs`의 발사 기록 — 지난 세대의 투사체가 도착하면 콜백만 건너뛰고 기록에서는 빠지지 않았다. 도착하면 세대와 상관없이 기록에서 빼고, 콜백은 지금 세대일 때만 부르게 고쳤다. 정상 취소(`Cancel` → 기록 비움)에서는 생기지 않던 경우라 화면 동작은 같다.
- **시험(T32):**
  - `T/CombatClipShotTests.cs`(새 7, Unity): 곡사 착탄 = 맞음 → 도착(코드 모션 없이 진행, 진행이 다른 진행을 막지 않음·공격 재생 `IsComplete`), 직선 몸 없음 = 도착 때 맞음, 직선 몸 있음 = 접촉 진행률(.875)에 맞음 뒤 도착, 세대 바뀜 = 콜백 없음, `Recall`은 그 투사체만, `Cancel` 뒤 콜백·투사체 없음, 근접·Ballista·주 칸 없는 곡사 거절.
  - `T/CombatClipAttackCompositionTests.cs`(사례 5 추가, Unity): 원거리 — 이벤트 타이밍에 선딜레이 없이 포구에서 쏨, 맞음 → 해제 → `Hit`, 클립이 끝나도 도착까지 실행이 남고 도착 때 대여 복원 → 출처 끝, 취소 → 투사체 회수, 포구 없음 → 예외, 발사 입구 없음 → 인자 예외. 시험 기록 필드는 [SetUp]에서 비움.
  - `T/CombatClipAttackSceneTests.cs`(사례 1 추가 + 준비 나눔, Unity): 공격자 외형을 사례마다 만드는 `Arrange(종류)`로 나눔(표시 준비 때 클립 채널·묶음 값이 정해져서). 곡사 클립 공격자(포구 지정) — 이벤트 전 포탄 없음, 이벤트 때 포탄, 비행 중 맞지 않음, 착탄 때 피격 사건, 소리 선딜레이, 전체 종료는 클립 끝 뒤. 기존 근접 사례 둘은 `Arrange(Melee)` 한 줄씩.
- **공유·요청(T33, 미전달):**
  - 호진님 메모 §4 규칙 줄 고침(공격 클립 허용 = 근접·직선·곡사, 원거리는 포구·피격 선딜레이 0) + §7 `언데드 마인카트` 외형 항목(공격 설정은 곡사 재사용 가능 — 투사체 값만 읽음, 클립 묶음 = `CannonAttack`·`CannonAttackBody` 중 하나·피격 선딜레이 0, 등록 순서).
  - 민형님 메모 §6 `CannonEnemy` 프리팹(이벤트 컴포넌트·포구·`CannonIdle` 컨트롤러·몸 BoxCollider, `CatusCannonWeaponView` 불필요)·공격 클립 이벤트(`ClipEvent` = 포탄이 떠나는 프레임).
  - **계획과 다른 점 — Core 요청은 쓰지 않았다:** [Core 인계](../handoffs/2026-10-16-undead-cannon-cart-view-handoff.md)가 이미 `UnitDataTable`·`TargetingMethodDataTable` 등록·외형 항목·적 배치를 **View 작업자 몫**으로 넘겼다(등록은 외형 항목과 함께). 그래서 요청이 아니라 메모 둘이 그 문서를 가리킨다. 그 문서의 벽 정책 서술(`RequireClear`)과 지금 SO 저장값(`IgnoreWalls`)이 다른 점은 Core 영역이라 보고만 한다(설계 F21).
- **검증:** MSBuild·관리 실행 안 함(사용자 지시 이어 감). 새 시험 13개는 모두 Unity 전용(투사체 프리팹·클립 값).
- **원칙 자체 점검(7기준, AC-5 전체):** SSOT 9(원거리 판정·포구·목표점·접촉 진행률·해제 뒤 신호·방향이 각 한 곳, 곡사 방향 두 벌 해소) · SOLID 8.5(투사체 진행 분리, 조립 입구 둘 / 공격 재생 533줄) · FW 9(프레임 할당 0) · 독립성 9(근접·코드 길 무변경, 새 원거리 종류는 판정 함수 한 줄 + 입구 한 갈래) · 캡슐화 9(발사 기록은 private, 밖에는 회수 손잡이) · C#·Unity 9 · If·Tick 9(코드 모션 없을 때 `HasShots` 한 번) = **약 8.9**(설계 §11 검사 8.3 → 예상 8.6보다 조금 높음).
- **Unity 확인(사용자):** `CombatClipShotTests`(새 7)·`CombatClipAttackCompositionTests`(새 5 포함)·`CombatClipAttackSceneTests`(새 1 포함, 기존 2 준비 변경)·회귀 `CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatCodeMotionHitTests`.
- **커밋 때 새 파일(각 `.meta`):** `T/CombatClipShotTests.cs`. 고친 파일: `C/CombatProjectileFlights.cs`, 시험 `CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`, 문서(설계·계획·공유 메모 둘). **AC-5 전체 커밋 묶음:** §9.1·§9.2·§9.3의 목록 합(새 `C/CombatProjectileFlights.cs`·`C/ICombatShot.cs`·`C/CombatHitMoment.cs`·`C/CombatFirePiece.cs`·`T/CombatClipShotTests.cs`).
- **AC-5 끝.** 남은 것(코드 밖): 민형님 프리팹·클립 이벤트, 호진님 외형 항목, 표 등록·적 배치(View 작업자, Core 인계 문서), 실제 곡사 클립 재생 확인(§7).
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-5c 시험 문제 없음 확인했어."):** AC-5c 컴파일·시험 문제 없음. **AC-1~AC-5 코드·Unity 확인 모두 끝**(커밋은 사용자). 남은 것은 코드 밖(민형님 프리팹·클립 이벤트, 호진님 외형 항목, 표 등록·적 배치 — View 몫, 실제 곡사 클립 재생 확인 §7).

### 9.4 곡사 적 클립 묶음 에셋 (2026-10-19, 사용자 "일단 Enemy 기준 CombatUnitAnimationSO 만들어줄래")

- **확인 사실:** 사용자가 작업 중인 트리에 `Prefabs/Characters/Colored/Enemy/CannonEnemy.prefab`(FBX 변형 + BoxCollider, 커밋 전)과 외형 항목 `UnitVisual_Cannon`(키 25, 공격 표시·클립 묶음 칸 비어 있음, 커밋 전)이 생겼다. 적 FBX 넷은 같은 테이크 9개를 갖고, Unity 클립 이름은 `Scull|…` 꼴(예: `Scull|CannonAttack`). `CannonEnemy.fbx`는 `clipAnimations: []`(기본 테이크, 이벤트 없음).
- **만든 것:** `DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/CombatUnitAnimation_Cannon.asset`(+ `.meta`) — 공격 클립 `Scull|CannonAttack`(fileID `3152241188546012841`, `CannonEnemy.fbx`), 피격 선딜레이 0(원거리 규칙), 소리 선딜레이 0, 피격 클립 없음. 클립 fileID는 Library 가져오기 결과에서 읽음(`Scull|CannonAttackBody` = `-1095298166988572756`, `Scull|CannonIdle` = `2208886828609018748`).
- **연결하지 않음:** `UnitVisual_Cannon.animationPresentation`에 넣으면 부트 DB 검사가 실패한다 — 클립에 이벤트 셋이 없고, 프리팹에 `CombatAnimationEvents`·`muzzle`이 없다. 이벤트를 찍고 프리팹을 갖춘 뒤 연결한다(공유 메모 호진님 §7·민형님 §6).
- **추가 확인(사용자 질문 넷에 답하며):** `CannonEnemy.fbx`는 Avatar Definition = No Avatar(`avatarSetup: 0`)라 가져온 모델 루트에 **Animator가 없다**(Library 가져오기 결과에 Animator 객체 없음) → 프리팹 루트에 Animator를 직접 붙인다. 사용자가 폴더를 나눠 이 에셋은 `CombatPresentation/AnimationClip/`, 공격 표시 SO 넷은 `CombatPresentation/Code/`로 옮겨졌다.
- **민형님 재내보내기(`13eb1dbc`) 확인:** `CannonEnemy.fbx`에 포신 아마추어 `CannonBone`과 무기 전용 테이크 `CannonBone|CannonAttackBody`(fileID `4125226451315635580`)가 생겼다. 기존 클립 fileID는 그대로라 클립 묶음 SO 참조는 유효하다. 몸(`Scull|CannonAttack`)과 무기를 **두 클립으로 같이 재생**하는 것은 지금 코드가 받지 않는다(사건마다 클립 하나, 채널 위층 하나 — Q-AC12 A). 사용자 결정 대기: 한 테이크 요청 유지(A) / 부속 클립 지원(B, 설계 변경).

## 10. AC-6 — 부속 클립(무기 클립을 몸 클립과 같이 재생) 계획 (2026-10-19, 설계 6차 §12)

- 결정: Q-AC20~Q-AC22 모두 A(사용자 "Q-AC20~Q-AC22 모두 A로 해. 계획 쓰고 AC-6a부터 진행해. MSBuild 생략하고 AC-6c 까지 쭉 진행"). **MSBuild·관리 실행 안 함, Phase 사이에 멈추지 않음**(사용자 지시) — 컴파일·시험은 Unity에서 사용자가 확인.
- 아래 `C/` = `ViewSystem/Combat/`, `DV/` = `DataBaseSystem/Data/View/CombatPresentation/`, `T/` = `Assets/Tests/EditMode/Framework/`.

### 10.0 계획에서 정한 것(보고, 설계 뜻은 그대로)

| # | 정한 것 | 이유 |
|---|---|---|
| P14 | 부속 목록 **모양 규칙의 원본은 `CombatClipData.FindCompanionError`**(빈 칸·중복·주 클립과 같은 클립) 하나. 값 생성자(어기면 `ArgumentException`)와 규칙 함수 `CombatUnitAnimationRules.RequireCompanionClips`(어기면 `InvalidOperationException`)가 같이 부른다 | 시각 규칙 `FindTimeError`와 같은 방식(값 불변 + DB 문구) |
| P15 | 주 클립 없이 부속만 있는 칸은 클립 묶음 SO가 거절한다 | 시점의 원본이 없는 부속은 뜻이 없음 |
| P16 | 채널은 층 배열을 보관하지 않고 `SetTime`이 레이어 섞개의 입력을 돈다(프레임 할당 0). `Begin` 도중 실패하면 만든 위층을 지우고 예외를 그대로 낸다 | 반쯤 만든 위층이 "이전 사건 클립이 남음"으로 다음 시작을 막지 않게 |
| P17 | **설계 빈틈 보완:** 클립이 끝나면 **컨트롤러가 있어도** Animator 아래 Transform을 준비 때 값으로 되돌린다(같은 프레임의 Unity 평가에서 Idle이 자기 뼈를 다시 덮음). 준비 때 값 기억도 컨트롤러 유무와 상관없이 한다(지금은 컨트롤러 없을 때만) | 위층을 빼면 Idle이 움직이지 않는 뼈(무기 뼈)는 그래프 바인딩에서 빠져 **공격 마지막 자세에 멈춘다**. 설계 AC9 "끝나면 Idle로"의 뜻을 무기 뼈까지 지킴. 컨트롤러 없는 유닛의 동작은 그대로 |
| P18 | 채널 시험 도우미 `EventClip()`이 사건 클립 값(`CombatClipData`)을 돌려준다(부속은 선택 인자) | 기존 시험의 호출 줄 무변경 |

### AC-6a — 값·입력·부속 검사 (화면 변화 없음)

- **T32 값 `DV/CombatClipData.cs`:** `Companions`(`IReadOnlyList<AnimationClip>`, 없으면 빈 목록, 생성 때 복사), 생성자 끝 선택 인자 `companions`, `FindCompanionError`(P14). 문서: "사건 클립 = 주 클립 + 부속".
- **T33 규칙 `DV/CombatUnitAnimationRules.cs`:** `RequireCompanionClips(main, companions)` — 모양(P14) → 부속마다 `RequireEventClip`(길이·반복·루트 곡선) + 이벤트 없음 → 에디터에서만 곡선 겹침 없음(주 클립·다른 부속과 같은 경로·형식·속성).
- **T34 입력 `DV/CombatUnitAnimationSO.cs`:** 칸 `attackCompanionClips`(공격 클립 다음)·`hitCompanionClips`(피격 클립 다음), `ReadClip(클립, 부속, 칸 이름)`이 부속 규칙을 부르고 값에 넘김, P15.
- **T35 시험 `T/CombatUnitAnimationSettingsTests.cs`(Unity 전용):** 부속이 순서대로 실림·없으면 빈 목록, 부속 이벤트 거절, 곡선 겹침 거절, 반복 부속 거절, 빈 칸·중복·주 클립과 같음 거절(값·SO 둘 다), 주 클립 없는 부속 거절.

### AC-6b — 채널 레이어 섞개·대여 계약

- **T36 채널 `C/Display/UnitClipChannel.cs`:** 위층 = `AnimationLayerMixerPlayable`(층 0 주 클립, 층 1..N 부속, 모두 가중치 1·속도 0·IK 끔), `Begin(CombatClipData)`, `SetTime`이 모든 층(P16), `End`가 위층째 파괴(`DestroySubgraph`) 뒤 준비 때 자세로 되돌림(P17), 컨트롤러 없으면 지금처럼 그래프 멈춤·Animator 끔.
- **T37 대여 `C/Display/ICombatUnitClip.cs`·`C/Display/FieldUnitView.cs`:** `Play(CombatClipData clip)`(문서: 대여는 값의 시각을 쓰지 않음).
- **T38 재생 단위 `C/CombatClipPlayback.cs`:** `_lease.Play(_data)`.
- **T39 시험:** 가짜 대여 셋(`CombatClipPlaybackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`)의 `Play` 서명(기록은 주 클립 그대로 — `Played` 단정 셋 무변경), `T/UnitClipChannelTests.cs` 도우미(P18) + 새 시험(Unity 전용): 부속 층이 자기 뼈를 움직이고 주 클립도 그대로(U-C1), 짧은 부속은 마지막 자세 유지(U-C2), 끝나면 Idle 바깥 무기 뼈가 준비 때 자세로(P17), 컨트롤러 없는 유닛도 둘 다 되돌림.

### AC-6c — 에셋·공유

- **T40 에셋:** `CombatPresentation/AnimationClip/CombatUnitAnimation_Cannon.asset`의 `attackCompanionClips`에 `CannonBone|CannonAttackBody`(fileID `4125226451315635580`). 외형 항목 연결은 여전히 이벤트·프리팹 준비 뒤(계획 §9.4).
- **T41 공유 메모:** 민형님 §1·R2(무기 테이크 따로 가능, 이벤트는 몸 테이크에만, 무기 테이크는 무기 뼈만)·§6(곡사 적 포신 테이크), 호진님 §2(새 칸)·§4(부속 규칙)·§7(곡사 적 부속).
- **T42 기록:** 계획 §10 실행 기록, CLAUDE.md, 폴더 정리 대응표(새 파일 없음 — 줄 추가 없음).

### AC-6 확인(사용자, Unity)

- 컴파일, `CombatUnitAnimationSettingsTests`·`UnitClipChannelTests`·`CombatClipPlaybackTests`·`CombatClipPieceTests`·`CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`·`CombatHitCompositionTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`.
- 실제 곡사 적 재생은 이벤트·프리팹·외형 연결 뒤(설계 §12.5 U-C3).

### 10.1 AC-6a~AC-6c 실행 기록 (2026-10-19, 사용자 "계획 쓰고 AC-6a부터 진행해. MSBuild 생략하고 AC-6c 까지 쭉 진행")

- **MSBuild·관리 실행 안 함, 컴파일 미확인**(사용자 지시). Phase 사이에 멈추지 않음.
- **AC-6a(T32~T35):** `DV/CombatClipData.cs` — `Companions`(생성 때 복사, 없으면 `Array.Empty`), 생성자 끝 선택 인자, 모양 규칙 원본 `FindCompanionError`(P14). `DV/CombatUnitAnimationRules.cs` — `RequireCompanionClips`(모양 → 부속마다 `RequireEventClip` + 이벤트 없음 → 에디터 전용 곡선 겹침 `RequireNoSharedCurves`, `EditorCurveBinding` 사전). `DV/CombatUnitAnimationSO.cs` — 칸 `attackCompanionClips`·`hitCompanionClips`(기본 빈 배열), `ReadClip(클립, 부속, 칸)`, 주 클립 없는 부속 거절(P15), "모두 비어 있음" 판정이 부속도 봄. 시험 `T/CombatUnitAnimationSettingsTests.cs` 새 8개(실림·이벤트·주 클립과 겹침·부속끼리 겹침·반복·루트 곡선·목록 모양 3사례(값·SO 둘 다)·주 클립 없음) + `Bundle` 도우미 선택 인자·`Companion` 도우미.
- **AC-6b(T36~T39):** `C/Display/UnitClipChannel.cs` — 위층 `AnimationLayerMixerPlayable`(층 0 주·층 1..N 부속, 가중치 1, `ConnectLayer`), `Begin(CombatClipData)`(도중 실패 시 위층 지움, P16), `SetTime`이 레이어 섞개 입력을 돎(할당 0), `End`가 `DestroySubgraph` 뒤 **언제나** 준비 때 자세로 되돌림(P17, 준비 때 기억도 컨트롤러와 상관없이), 컨트롤러 없으면 그래프 멈춤·Animator 끔(옛 `Rest`를 `RestorePose` + 분기로). `C/Display/ICombatUnitClip.cs`·`C/Display/FieldUnitView.cs` 대여 `Play(CombatClipData)`, `C/CombatClipPlayback.cs` `_lease.Play(_data)`. 시험: 가짜 대여 셋 `Play` 서명(`CombatPieceRunTests.FakeClip.Played`는 주 클립 그대로 — 단정 셋 무변경), `T/UnitClipChannelTests.cs` 도우미 `EventClip(params 부속)`이 값을 돌려줌(P18, 기존 호출 줄 무변경)·모델에 무기 뼈 `Weapon`·`WeaponClip`, 새 4개(부속 층이 무기 뼈만·주 클립 그대로, 짧은 부속 마지막 자세 유지, 컨트롤러 있을 때 끝나면 무기 뼈가 준비 때 자세(P17), 컨트롤러 없을 때 둘 다 되돌림).
- **AC-6c(T40~T42):** `CombatUnitAnimation_Cannon.asset`에 `attackCompanionClips: [CannonBone|CannonAttackBody]`, `hitCompanionClips: []`. 민형님 메모 §1·R2 다시 씀(무기 테이크 따로 가능, 이벤트는 몸 테이크에만, 무기 테이크는 무기 뼈만, 무기 Idle은 컨트롤러 레이어)·§3 규칙 한 줄·§6(몸 `Scull|CannonAttack`에 이벤트, 포신 테이크는 부속, No Avatar라 Animator 직접 붙임). 호진님 메모 §2 표·§4 부속 규칙·§5 무기 테이크·§7(공격 = 몸 + 포신 부속, "둘 다 가능" 정정, 사용자가 만든 `UnitVisual_Cannon` 알림). 새 파일 없음(폴더 정리 대응표 줄 추가 없음).
- **계획과 다른 점:** 없음(P14~P18은 계획에 적은 대로).
- **원칙 자체 점검(7기준):** SSOT 9(시점·길이 원본 = 주 클립, 모양 규칙 `FindCompanionError`, 클립 규칙 `CombatUnitAnimationRules`) · SOLID 8.5(대여·채널이 쓰지 않는 시각까지 든 값을 받음) · FW 9(Factory·ServiceKeys 무변경, 프레임 할당 0) · 독립성 9(무기 수·종류는 에셋만, 조각·조립·처리기·창구 무변경) · 캡슐화 8(K11 그대로) · C#·Unity 8.5 · If·Tick 9(부속 유무 갈래 없음) = **약 8.7**(설계 예상과 같음).
- **커밋 때(새 파일 없음):** 고친 코드 `DV/CombatClipData.cs`·`DV/CombatUnitAnimationRules.cs`·`DV/CombatUnitAnimationSO.cs`·`C/Display/UnitClipChannel.cs`·`C/Display/ICombatUnitClip.cs`·`C/Display/FieldUnitView.cs`·`C/CombatClipPlayback.cs`, 시험 `CombatUnitAnimationSettingsTests`·`UnitClipChannelTests`·`CombatClipPlaybackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`, 에셋 `CombatUnitAnimation_Cannon.asset`(SO 코드와 한 커밋 — 새 칸), 문서(설계 §12·계획 §0.8·§9.4·§10·공유 메모 둘). AC-1~AC-5 묶음이 아직 커밋 전이면 함께.
- **Unity 확인 대기:** 컴파일, §10 "AC-6 확인"의 시험 묶음. 새 시험 12개 모두 Unity 전용(메모리 `AnimationClip`).
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 AC-6 시험 문제 없음 확인했어."):** AC-6 컴파일·시험 문제 없음(레이어 섞개 U-C1·U-C2, P17 되돌림 포함). **AC-6 끝**(커밋은 사용자). 남은 것은 코드 밖: `Scull|CannonAttack` 이벤트 셋, 프리팹(Animator·`CombatAnimationEvents`·`muzzle`·Idle 컨트롤러), `UnitVisual_Cannon`에 공격 표시·클립 묶음 연결, 실제 재생 확인(설계 §12.5 U-C3).

### 10.2 곡사 적(UndeadCannon) 에셋 세팅 (2026-10-19, 사용자 "너가 UndeadCannon에 관한거 전부 다 세팅한번 해볼래? ClipEvent는 임의로 잡아도 됨")

- **이미 사용자가 해 둔 것(확인):** `CannonEnemy.prefab` 최상위에 Animator(컨트롤러 `UndeadCannon.controller`)·`CombatAnimationEvents`·BoxCollider, 최상위 자식 `Muzzle`(로컬 0, 0.928, 0.898), `UnitVisual_Cannon`에 공격 표시(`CombatAttack_CactusCannon`, 곡사)·반응(기본)·클립 묶음(`CombatUnitAnimation_Cannon`) 연결, `UnitDataTable`에 `Unit_UndeadCannonCart` 등록. 프리팹 최상위가 그대로 `FieldUnitView`가 되고 위치는 배치가 정하므로(`FieldUnitPresenter.CreateView`) 최상위 Animator·자식 `Muzzle` 구조가 맞다.
- **이번에 고친·더한 것:**
  - `CannonEnemy.fbx.meta`(민형님 파일, 사용자 지시): `clipAnimations`에 테이크 10개 전부(빠지는 클립이 없게), 각 `internalID` = 기존 fileID(참조 유지), 구간 = FBX 테이크 길이(30fps, 몸 공격 45·포신 35·Idle 23프레임 등). `Scull|CannonIdle` Loop Time 켬, `Scull|CannonAttack`에 이벤트 셋(**`ClipStart` 0초 · `ClipEvent` 0.6초(정규화 0.4, 임의 — 사용자 허락) · `ClipEnd` 1.5초**), 포신 `CannonBone|CannonAttackBody`는 이벤트 없음·반복 끔. 원본은 스크래치에 백업.
  - `UndeadCannon.controller`: Idle 상태(`Scull|CannonIdle`)의 모션이 **공격 클립(`Scull|CannonAttack`)을 가리키던 것**을 `Scull|CannonIdle`(fileID `2208886828609018748`)로 고침.
  - `CannonEnemy.prefab`: `CombatAnimationEvents.muzzle` = 자식 `Muzzle`(비어 있었음).
  - `UnitVisual_Cannon.displayName` = `언데드 마인카트`(Core 인계 문서의 승인 이름, 사용자가 둔 `UndeadCannon`에서 바꿈).
  - `TargetingMethodDataTable`에 `TargetingMethod_UndeadCannonCart`(지정 방식 키 8, 겹침 없음) 등록 — 유닛 SO가 참조하는데 표에 없었음.
- **확인 사실:** FBX 계층은 `CannonBone`(아마추어) → `뼈대`(수레) → `Scull`(몸 아마추어) …, `CannonBone` → `뼈대.001`(포신, 메시 `Cylinder002`). 포신 테이크는 `CannonBone`·`뼈대`·`뼈대.001` 셋만, 몸 테이크는 `Scull` 아래 31개만 움직여 곡선 겹침이 없다(부속 검사 통과 예상). 루트 곡선 없음.
- **남긴 것(보고):** `Muzzle`이 최상위 자식이라 포신 반동을 따라가지 않음(따라가게 하려면 에디터에서 `CannonBone/뼈대.001` 아래로 끌어 넣기 — 월드 위치 유지), 아이콘(`timelineIcon`·`infoIcon`) 없음(검사는 통과, 타임라인·유닛 정보에 그림 없음), 벽 정책(Core 인계 `RequireClear` vs SO `IgnoreWalls`, Core 영역), 프리팹의 뼈 `m_LocalEulerAnglesHint` 덮어쓰기 다수(에디터 힌트, 동작 영향 없음).
- **적 배치:** 적 로드아웃(`EnemyLoadout_1~5`, Demon·Gravekeeper·Ballista 가중치)에 넣어야 전투에 나온다 — 콘텐츠 변경이라 사용자 결정 대기(권장: `Combat_Normal_A`만 쓰는 `EnemyLoadout_1`에 가중치 1).
- **Unity 확인할 것:** FBX 다시 가져오기 뒤 클립 묶음 SO·컨트롤러의 클립 칸이 그대로인지(`internalID` 유지), `DataBaseAssetTests`, Boot 진입(DB 검사 통과), 배치 뒤 실제 재생(설계 §12.5 U-C3).
- **적 배치(2026-10-19, 사용자 "EnemyLoadout_1에 언데드 마인카트를 가중치 1로 넣어줘."):** `EnemyLoadout_1`(`Combat_Normal_A`만 씀) 항목에 `Unit_UndeadCannonCart` 가중치 1(Demon·Gravekeeper·Ballista와 같음, 뽑는 수 3 그대로). 로드아웃 검사(유닛 있음·가중치 ≥ 1) 통과.

### 10.3 나머지 적(FixSo·Sabow·Stage1Boss) 클립 이벤트·묶음 SO 세팅 (2026-10-19, 사용자 "Enemy 나머지 애들도 모델 말고 지금처럼, 이벤트 및 SO 세팅 해줘")

- **범위:** 곡사 적(§10.2)과 같은 FBX 클립 설정 + 클립 묶음 SO만. 프리팹·모델 교체·외형 표 연결·컨트롤러는 하지 않았다("모델 말고").
- **확인 사실:**
  - 세 FBX(`Models/Enemy/FixsoEnemy.fbx`·`SabowEnemy.fbx`·`Stage1Boss.fbx`, 민형님 파일)는 `clipAnimations: []`(기본 테이크)였고, 테이크는 셋 모두 같은 9개(`Scull|` Cannon·FixSo·Sabow·Stage1Boss의 Attack·Idle과 `CannonAttackBody`, 30fps). 포신 테이크 `CannonBone|CannonAttackBody`는 곡사 적 FBX에만 있다.
  - **기본 클립 fileID는 테이크 이름으로 정해져 네 FBX가 같다** — Library 가져오기 결과(직렬화 파일 객체 표)를 읽어 확인(스크래치 `fbxids/ClipIds.cs`): FixSoAttack `7868780871576686653`, FixSoIdle `-7714747872119919769`, SabowAttack `-5351055023807019676`, SabowIdle `8597609857143474270`, Stage1BossAttack `5411536164789108443`, Stage1BossIdle `-2326357195674975809`(곡사 적 §10.2의 값과 같음). 그래서 `internalID`에 그대로 넣어 기존 참조(다른 브랜치에 있을 수 있음)를 지켰다.
  - 이 FBX 클립을 쓰는 프리팹·에셋은 저장소에 없다(GUID 검색 0). 외형 표의 적 프리팹(`Enemy.prefab` Demon·`EnemyUnit_UnderTaker.prefab` Gravekeeper·`1StageBoss.prefab`)은 아직 옛 모델이다.
  - 계층: 곡사 적과 달리 `Scull`이 FBX 최상위 자식이고 테이크가 `Scull` 자신의 위치·회전·크기도 움직인다. 클립 경로는 `Scull`(빈 경로 아님)이라 **Animator를 FBX 루트(= `Scull`의 부모)에 두면** 루트 곡선 검사를 통과한다(`Scull`에 두면 실패).
- **바꾼 것:**
  - FBX 메타 셋(원본은 스크래치 `fbxids/backup/`): `clipAnimations`에 그 파일의 테이크 9개 전부(빠지는 클립이 없게, 곡사 적 설정을 바탕으로 스크립트 `fbxids/make_clips.pl`), 각 `internalID` = 위 fileID, 구간 = 테이크 길이. **자기 Idle만 Loop Time**, **자기 Attack에만 이벤트 셋 `ClipStart` 0 · `ClipEvent` 0.4(정규화, 곡사 적과 같은 임의 값) · `ClipEnd` 1** — 초로는 FixSo 0·0.267·0.667, Sabow 0·0.307·0.767, Stage1Boss 0·0.4·1.0. 다른 테이크는 반복·이벤트 없음.
  - 새 클립 묶음 SO 셋(`ViewGroup/CombatPresentation/AnimationClip/`, + `.meta`): `CombatUnitAnimation_FixSo`(공격 = `FixsoEnemy.fbx`의 `Scull|FixSoAttack`), `CombatUnitAnimation_Sabow`(`SabowEnemy.fbx`의 `Scull|SabowAttack`), `CombatUnitAnimation_Stage1Boss`(`Stage1Boss.fbx`의 `Scull|Stage1BossAttack`). 셋 모두 부속·피격 클립 없음, 선딜레이(피격·소리·이펙트) 0, 이펙트 Empty — 피격 선딜레이 0이라 근접·원거리 어느 쪽에 연결해도 검사를 통과한다.
- **연결하지 않은 이유:** 외형 항목에 묶음을 연결하면 그 프리팹에 `CombatAnimationEvents`가 있어야 부트 검사를 통과하는데, 지금 프리팹은 옛 모델이다. FixSo·Sabow가 어느 유닛(EntityKey)인지도 아직 모른다(§0.8 X6, 민형님 요청 메모 R5).
- **연결할 때 할 일(프리팹 작업):** 그 FBX로 프리팹을 만들고 FBX 루트에 Animator(Idle만 있는 컨트롤러 — 자기 `…Idle` 클립) + `CombatAnimationEvents`, 원거리면 `muzzle`, 외형 항목에 공격 표시와 이 묶음 연결. `ClipEvent` 시각은 실제 동작을 보며 FBX 가져오기 설정에서 고친다.
- **Unity 확인할 것:** 세 FBX 다시 가져오기(경고 없음, 클립 9개), 묶음 SO 인스펙터에서 공격 클립이 보이는지. 연결 전이라 DB 부트 검사에는 들어가지 않는다.
