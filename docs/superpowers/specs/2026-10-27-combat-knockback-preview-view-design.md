# 전투 넉백 미리보기 View 설계 초안

- 작성일: 2026-10-27. 코드 변경 없음(설계만).
- **결정 완료(2026-10-27 사용자): Q-KB1~Q-KB6·Q-KB-T 모두 권장안 A.** Task 계획은 [knockback-preview-view-plan](../plans/2026-10-27-combat-knockback-preview-view-plan.md)(KB-a~KB-c)이다. 계획에서 이 설계와 다르게 정한 것은 계획 §0.2(TP-1·TP-2)이다.
- **KB-b 검토 뒤 바뀐 것(2026-10-27 사용자 결정 Q-KBb1·Q-KBb3 A, 계획 §7.2):** P3은 "체력 예상 조건을 걸지 않는다"에서 **"체력·비용 예상과 같은 조건(결과 재생 중이 아님)에서만 그린다"**로 바뀌었다(미리보기 답이 재생 중에도 오기 때문). KB2의 화살표 높이는 **점유 칸과 그 앞 칸(판 안) 표면 중 가장 높은 값**으로 바뀌었다(고지대 쪽으로 밀릴 때 묻히지 않게).
- 사용자 요청: 공격 모드 호버 때 궤적(노란 선)이 착탄 칸까지 보이고, 착탄 칸 기준으로 주변 대상이 **어디로 밀릴지** 미리 보이게 한다(레퍼런스: Into the Breach 화면). 설계는 7기준(SSOT·SOLID·FW 계약·기능 변경의 독립성·캡슐화·C#/Unity 권장 구조·무의미한 If/Tick)을 염두에 둔다.
- 관련 원본:
  - [Core 넉백 미리보기 설계](2026-10-27-attack-knockback-preview-design.md)(천명님, §4 공개 계약·§6 미리보기와 실제 차이·§7 View 인계)
  - [공격 궤적 설계](2026-10-19-combat-attack-trajectory-design.md)(TJ1~TJ9, 좁은 계약·캔버스·그리기 부품 선례)
  - [e413008 대응 정리](2026-10-26-combat-view-core-e413008-response-inventory.md) B6·Q-E5(밀려날 칸 미리보기). Core가 요청 없이 값을 제공해 이 항목이 풀린다. 정리 문서 자체는 고치지 않는다(10-26 사용자 지시).
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `Core/` = `…/CoreSystem/CoreCaculator/`.

## 0. 요청을 지금 화면에 대응

| 레퍼런스 화면 | 우리 쪽 | 이번에 할 일 |
|---|---|---|
| 노란 궤적 | 공격 궤적 점선(TJ-1~TJ-3). Ballista는 BC-a 뒤 곡사 길이라 착탄 칸까지 곡선 | 없음 |
| 파란 착탄 칸 | 호버 칸 = 미리보기 `PrimaryCell`(충격 볼트 중심). 공격 호버 색으로 이미 칠함 | 없음 |
| 영향 칸 | 영향 칸 층 `Affected`(충격 볼트는 3×3) | 없음 |
| 대상마다 밀리는 방향 화살표 | 없음 | **새로 그림** |
| 밀린 뒤 자리 | 없음(끌어오기만 도착 칸 층이 있음) | **새로 그림** |

레퍼런스는 십자 4방향이지만 우리 규칙은 **8방향**이다(F2). View는 Core가 준 방향을 그대로 그린다.

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | Core 공개 값 `AttackPreview.Knockbacks`: 대상마다 `TargetId`·`TargetKind`(Character/Structure)·`StartAnchor`·`Step`(8방향 한 칸)·`RequestedDistance`·`Path`·`FinalAnchor`·`Stop`(Completed/Blocked/Interrupted/Immune). Buffer 형식은 그대로라 지금 받는 공격 미리보기에 같이 온다. 기록 대상은 반드시 미리보기 `Targets` 안에 있다(생성 때 검사) | `Core/Contracts/Results/AttackKnockbackSnapshot.cs`, `AttackPreview.cs:107-109`, Core 설계 §4 |
| F2 | 방향: 충격 볼트는 `sign(대상 기준 칸 − 착탄 중심)`, 일반 넉백은 `sign(대상 − 공격자)`. 대각선 칸 대상은 대각선으로 밀린다. 밀 수 있는 장애물(구조물)도 기록된다 | `ImpactBoltTraitRule.cs:24`, `AttackKnockbackCalculator.cs:70·84` |
| F3 | Core는 다칸 대상의 최종 점유 칸을 싣지 않는다. "현재 점유 칸 + (`FinalAnchor − StartAnchor`)"로 얻는다. 같은 응답의 `AttackTargetPreview.TargetOccupiedCells`가 원래 자리다 | Core 설계 §2 표·§4.1 |
| F4 | `Interrupted`는 사망을 뜻하지 않는다. 사망은 `AttackTargetPreview.TargetKilled`로 본다. 체력 예상은 이미 HUD가 그린다 | 계약 주석, `AttackTargetingInteraction.cs:325` |
| F5 | 공격 호버 흐름: 응답 하나(`ApplyAttackAnswer`, `:299`)가 영향 칸·끌어오기 도착 칸(`:303`)·호버 상태·막힌 벽·궤적·체력/비용 예상을 고친다. 지우는 곳은 셋이다. ① 호버 표시 지우기 `ClearHoverDisplay`(`:505`, "새 호버 표시를 더하면 이곳 한 곳", 구현 평가 P-V2) ② 모드 정리 `ResetRangeAnswer`(`:477`, 세대 검사 때문에 따로) ③ 결과 재생 직전 `BeforeResult`(`:206`, 궤적만 지움 — 영향 칸·끌어오기 도착 칸은 결과 후처리까지 남음) | `C/Input/Interaction/AttackTargetingInteraction.cs` |
| F6 | 궤적 선례: 좁은 계약 `ICombatAttackTrajectoryFeedback`(입력 쪽) → `CombatSceneDisplay`(해제 검사) → `CombatAttackFeedback` → 경로 구성 `CombatAttackTrajectory`(`Display/Feedback`) → 캔버스 `ICombatTrajectoryCanvas`(`FieldView` 명시 구현) → 그리기 부품 `FieldTrajectoryLine`(`Display/Field`, 필드 Build/Release 수명, ColorSystem으로 색 덮어쓰기). 모양 값은 View DB `StageFieldViewData`, 색은 `FieldView` 씬 값. 입력 쪽 인자는 끝 선택 인자(`:67`, `_trajectory?.`) | 공격 궤적 설계 TJ2·TJ8 |
| F7 | 끌어오기 도착 칸 선례: 층 `PullDestination`(테두리만, 이동 초안 색)을 `CombatAttackFeedback.ShowAffectedCells`의 둘째 인자로 칠한다. 천명님 `PullAndMarkPresentationTests:233-241`이 두 인자 함수를 리플렉션으로 부르고 `ClearAffectedCells`로 함께 지워지는지 본다 | `CombatAttackFeedback.cs:102-112` |
| F8 | 강조 층은 표 한 줄이 원본이다(`FieldHighlightLayers.Rows`, View 3라운드 W10). 열거는 지금 13개(ushort라 16까지). 같은 색·다른 층 선례가 있다(`AllyHoverRange` = 공격 범위 색, "층만 나눔"). GridOutline 아웃라인 손잡이는 층마다 생겨 우리 `FieldOutlineRendererTests:84·312`의 손잡이 수가 층 수를 따른다 | `C/Display/Field/FieldHighlightLayers.cs` |
| F9 | 판 위 표지로 쓸 화살표 에셋이 없다. 이름이 Arrow인 것은 `Script/Render/Effect/Arrow/`(셰이더 그래프·`ArrowVFX.fbx`) 이펙트 묶음뿐이다(파일 이름만 확인). 궤적 막힘 표지 칸도 비어 있다 | `StageFieldView_Default.asset:37` |
| F10 | 좌표: `FieldGridMapper.CellCenterToLocal`(칸 밖이면 예외), `SurfaceHeightOf(점유 칸)`(가장 높은 표면). 칸 폭·길이는 private이라 "한 칸 방향 → 로컬 변위" 함수가 없다 | `C/Display/Field/FieldGridMapper.cs:66·98` |
| F11 | `AttackTargetingInteraction`을 만드는 곳: 운영 `CombatSceneInputFactory.cs:150`(이름 인자 `trajectory: display`), 시험 `CombatInputTestFactory:33`·`AttackTrajectoryInteractionTests:130`(우리), `WallAttackInteractionTests:242`(천명님, 위치 인자 다섯). `ICombatBattleDisplay` 구현은 `CombatSceneDisplay`와 우리 `CombatViewSystemTests.FakeDisplay` 둘 | grep |
| F12 | Core 미리보기에 없는 것: 숨은 지형 때문의 차이, 같은 방향 추가 공격·처치 연쇄의 넉백, 도착 칸 오브젝트 피해 | Core 설계 §6 |

## 2. 설계

### KB1. 책임 나누기

궤적과 같은 사슬로 둔다(F6). 판단은 Core, 시점은 입력, 배치 계산은 구성, 그리기는 필드 부품이다.

| 부품 | 하는 일 | 모르는 것 |
|---|---|---|
| 입력 `AttackTargetingInteraction`(있음) | **언제** 보이고 지우나. 공격 가능 응답이면 보이고, 호버 표시를 지울 때·모드 정리·결과 재생 직전에 지운다 | 방향·칸·모양 |
| 계약 `ICombatKnockbackPreviewFeedback`(새, `C/Input/Contracts/`) | `ShowKnockbackPreview(in AttackPreview)`·`ClearKnockbackPreview()`. 공격 조준만 받는다. `ICombatBattleDisplay`가 상속한다 | — |
| 구성 `CombatKnockbackPreview`(새, `C/Display/Feedback/`) | 기록 → 도착 칸 목록·표지 목록(종류·필드 로컬 위치·회전). 멈춘 이유 → 표지 종류 바꾸기는 여기 한 곳 | 입력 시점, 인스턴스·색 |
| 캔버스 `ICombatKnockbackCanvas`(새, `C/Display/Field/`, `FieldView` 명시 구현) | `Mapper`·`ShowKnockbackMarks(표지 목록)`·`ClearKnockbackMarks()` | Core 형식, 공격 |
| 그리기 `FieldKnockbackMarkers`(새, `C/Display/Field/`) | 화살표 인스턴스를 필요한 수까지 늘려 다시 쓰고, 종류별 색을 덮어쓴다. 필드 Build/Release 수명 | 무엇이 왜 밀리나 |
| 층 `FieldHighlightLayer.KnockbackDestination`(새 줄) | 도착 칸 테두리. 색은 층 표 한 줄 | — |
| 값 | 모양: View DB `StageFieldViewData`(화살표 프리팹·띄움 높이). 색: `FieldView` 씬 값 셋 | — |

표지 종류 열거 `FieldKnockbackMarkKind { Push, Blocked, Immune }`는 필드 쪽 형식이다. 필드 부품은 Core의 `AttackKnockbackStop`을 모른다(캡슐화).

### KB2. 무엇을 그리나 (Q-KB1·Q-KB2 권장 기준)

| `Stop` | 화살표 | 도착 칸 |
|---|---|---|
| `Completed` | 밀림 색 | 점유 칸 + (`FinalAnchor − StartAnchor`) |
| `Interrupted` | 밀림 색 | 멈춘 자리(위와 같은 식). 사망 여부는 HUD 체력 예상이 보인다(F4) |
| `Blocked`, 경로 있음 | 막힘 색 | 멈춘 자리 |
| `Blocked`, 경로 없음 | 막힘 색 | 없음 |
| `Immune` | 면역 색 | 없음 |
| 기록 없음 | 없음 | 없음 |

- 화살표 자리: 점유 칸 중 `Step` 쪽으로 가장 앞선 칸들의 중심에서 반 칸 더 간 곳(칸 가장자리, 대각선이면 꼭짓점). 높이는 점유 칸 표면 최고(`SurfaceHeightOf`) + 설정 띄움 높이.
- 화살표 회전: `Step`을 필드 로컬 방향으로 바꾼 쪽을 본다. 바꾸는 함수는 `FieldGridMapper`에 하나 더한다(`StepToLocal(CellCoord step)`, F10). 좌표 변환의 원본은 지금처럼 mapper 하나다.
- 대상이 여럿이면 기록마다 하나씩 그린다. 충격 볼트는 최대 8개, 일반 넉백은 피격 대상 수만큼이다.

### KB3. 흐름

입력(`AttackTargetingInteraction`, 바꾸는 줄 넷):

```text
ApplyAttackAnswer   if (CanAttack) { 영향 칸 표시; 넉백 표시(attack) }   ← 기존 if/else 안에 한 줄씩, 새 조건 없음
                    else           { 영향 칸 지움; 넉백 지움 }
ClearHoverDisplay   + 넉백 지움(호버 표시 지우기 한 곳, P-V2)
ResetRangeAnswer    + 넉백 지움(세대 검사 람다 하나)
BeforeResult        + 넉백 지움(궤적과 같은 자리, Q-KB3)
```

표시(궤적과 같은 사슬):

```text
CombatSceneDisplay.ShowKnockbackPreview(preview)        해제 뒤면 무시
 → CombatAttackFeedback.ShowKnockbackPreview(preview)    전달만
   → CombatKnockbackPreview.Show(preview)
       기록마다: 대상 점유 칸 찾기 → 도착 칸 → 표지 종류·자리·회전
       → FieldView.SetLayerCells(KnockbackDestination, 도착 칸)
       → ICombatKnockbackCanvas.ShowKnockbackMarks(표지)
```

재생에는 쓰지 않는다. 결과 재생은 지금처럼 `Moved` 사건이 원본이다(Core 설계 §5.3).

### KB4. 구성 규칙 (`CombatKnockbackPreview`)

- 점유 칸은 **같은 응답의** `Targets[].TargetOccupiedCells`에서 찾는다. Buffer를 따로 읽지 않는다. 같은 순간의 값이라 어긋날 수 없다.
- 기록 대상이 `Targets`에 없으면 `InvalidOperationException`이다. Core가 생성 때 보장하는 계약이 깨진 것이라 조용히 건너뛰지 않는다.
- 도착 칸은 `Path`가 비어 있지 않을 때만 만든다. 각 점유 칸에 `FinalAnchor − StartAnchor`를 더한다.
- `Stop` → 표지 종류 바꾸기는 `switch` 하나다. 모르는 값이면 예외다.
- 필드가 구성되기 전(`Mapper == null`)이면 지운다.
- 도착 칸 목록·표지 목록은 필드로 들고 다시 쓴다(응답마다 새 배열을 만들지 않음).

### KB5. 그리기 부품 (`FieldKnockbackMarkers`)

- 필드를 구성할 때 빈 부모(`FieldKnockback`, 셀 루트 아래)만 만든다. 표지 인스턴스는 처음 필요할 때 만들고, 가장 많이 쓴 수만큼 남겨 다시 쓴다.
- `Show(표지)`: 앞쪽 n개를 자리·회전에 놓고 켠다. 종류가 바뀐 인스턴스만 ColorSystem 색을 덮어쓴다(`MaterialBaseTypeKey.MainColor`, 궤적과 같은 길). 나머지는 끈다.
- `Clear()`: 모두 끈다. `Release()`: 색 핸들을 거두고 부모를 파괴한다. 실패는 모아 한 번 던진다(궤적 부품과 같은 꼴).
- 프리팹은 Renderer 하나를 가져야 한다(검사, KB6).

### KB6. 값

- View DB `StageFieldViewData`에 머리말 "Knockback Preview"와 칸 둘을 더한다.
  - `knockbackArrowPrefab`(필수, Renderer 필요)
  - `knockbackArrowLift`(0 이상, 표면 위로 띄우는 높이)
  - 검사 `ValidateKnockbackPreview()`. 표시 모드와 상관없이 본다.
- `FieldView` 씬 색 셋, 머리말 "넉백 미리보기 색"을 궤적 색 옆에 둔다.
  - `knockbackColor`(밀림), `knockbackBlockedColor`(막힘), `knockbackImmuneColor`(면역)
  - 기본값은 노랑·빨강·회색이다. 씬에는 다음 저장 때 칸이 생긴다.
- 도착 칸 층 색은 층 표 한 줄에서 정한다. 권장은 끌어오기 도착 칸과 같은 이동 초안 색이다(P5).
- 에셋: `StageFieldView_Default.asset`에 화살표 프리팹을 연결한다. **코드와 에셋은 한 커밋**이다(없으면 DB 생성 실패).

### KB7. 수명·성능

- 매 프레임 하는 일이 없다. 호버 응답이 올 때만 다시 놓는다(응답은 호버 칸이 바뀔 때만 온다).
- 인스턴스는 다시 쓰고, 필드 해제 때만 파괴한다. 응답마다 할당은 늘어날 때 말고는 없다.
- 결과 재생 직전에 지운다(Q-KB3). 실제 밀림 재생 위에 옛 화살표가 겹치지 않는다.

## 3. 설계에서 정한 것 (확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| P1 | 다칸 대상의 점유 칸은 같은 응답의 `TargetOccupiedCells`를 쓴다 | 같은 순간의 값. Buffer 의존을 새로 만들지 않음 |
| P2 | 기록 대상이 미리보기 대상에 없으면 예외 | Core 계약 위반을 숨기지 않음 |
| P3 | 표시 시점은 기존 공격 가능 분기 안이고, 체력 예상 조건(`CanShowHealthPreview`)은 걸지 않는다 | 영향 칸·궤적과 같은 시점. 조건을 하나 더 두지 않음 |
| P4 | `CombatKnockbackPreview`는 `CombatAttackFeedback` 생성자 안에서 만든다(필드만 필요) | Factory·`CombatAttackFeedback` 생성자·천명님 `PullAndMarkPresentationTests:224` 무변경. 갈아 끼울 서비스가 아니라 공격 표시의 한 부분 |
| P5 | 도착 칸 층 색 = 끌어오기 도착 칸과 같은 이동 초안 색, 테두리만 | "공격이 옮길 칸"을 같은 모양으로 |
| P6 | 화살표 색은 ColorSystem 덮어쓰기(프리팹 하나 + 씬 색 셋) | 궤적과 같은 길. 프리팹 변형 셋을 두지 않음 |
| P7 | 프리팹이 없는 맨 설정(시험이 직접 만든 설정)이면 필드가 표지 부품을 만들지 않는다. DB 검사는 필수로 강제한다 | 궤적 계획 P1과 같은 꼴(F6) |
| P8 | 같은 입력 건너뛰기(궤적의 `Drawn`)는 두지 않는다 | 응답이 호버 칸이 바뀔 때만 와서 효과가 없음 |

## 4. 7기준 점검 (권장안 기준, 알려진 문제 기준 예상)

| 기준 | 예상 | 근거 | 남는 것 |
|---|---|---|---|
| SSOT | 9.0 | 넉백 규칙·방향·막힘·면역의 원본은 Core 하나(View 계산 0). 점유 칸은 같은 응답. 좌표 변환은 mapper 하나. 멈춘 이유 → 종류는 한 곳. 층 색은 표 한 줄 | 낮음: "공격이 옮길 칸"이 층 둘(끌어오기·넉백)이다. 소유자와 지우는 시점이 달라 층을 나눔(`AllyHoverRange` 선례). Q-KB6 |
| SOLID | 8.75 | SRP: 시점(입력)·배치(구성)·그리기(필드) 분리. ISP: 좁은 계약·좁은 캔버스. DIP: 입력은 계약만 앎 | 낮음: P4(부품 안에서 생성) |
| FW 계약 | 9.5 | 서비스·모듈·Buffer·ServiceKeys 무변경. ColorSystem·필드 수명은 있는 것을 씀. Factory는 연결 줄 하나(Q-KB4 A) | — |
| 기능 변경의 독립성 | 9.0 | 계약·구성·부품·층 한 줄·DB 칸 둘·입력 네 줄을 걷으면 사라진다. 궤적·영향 칸·끌어오기 동작은 그대로 | — |
| 캡슐화 | 9.0 | 필드 부품은 Core 형식을 모름(자체 종류 열거). 입력은 기하를 모름 | — |
| C#/Unity 권장 구조 | 8.75 | 프리팹 + 인스턴스 재사용, Tick 없음, 해제 실패 모으기 | 낮음: P7 맨 설정 갈래(궤적과 같은 기존 성질) |
| 무의미한 If/Tick | 9.0 | 새 Tick 0. 새 조건은 경로 비어 있음·mapper 없음·종류 switch뿐이고 모두 뜻이 있음. Q-KB4 A면 선택 인자 `?.` 없음 | (Q-KB4 B면 낮음: 운영에서 늘 있는 의존에 `?.` 넷) |
| **합계** | **약 9.0** | | |

## 5. 영향

코드(권장안 기준):

| 파일 | 변경 |
|---|---|
| `C/Input/Contracts/ICombatKnockbackPreviewFeedback.cs` (+ `.meta`) | 새 계약 |
| `C/Display/ICombatBattleDisplay.cs` | 상속 목록에 하나 |
| `C/Display/CombatSceneDisplay.cs` | 두 메서드(해제 검사 뒤 전달) |
| `C/Display/Feedback/CombatAttackFeedback.cs` | 구성 부품 생성, 두 메서드 |
| `C/Display/Feedback/CombatKnockbackPreview.cs` (+ `.meta`) | 새 구성 |
| `C/Display/Field/ICombatKnockbackCanvas.cs` (+ `.meta`) | 새 캔버스 계약, 표지 값·종류 열거 |
| `C/Display/Field/FieldKnockbackMarkers.cs` (+ `.meta`) | 새 그리기 부품 |
| `C/Display/Field/FieldView.cs` | 캔버스 명시 구현, 색 셋, 부품 Build/Release |
| `C/Display/Field/FieldGridMapper.cs` | `StepToLocal` |
| `C/Display/Field/FieldHighlightLayers.cs` | `KnockbackDestination = 1 << 13`, 표 한 줄 |
| `C/Input/Interaction/AttackTargetingInteraction.cs` | 생성자 인자, 네 줄 |
| `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs` | 연결 줄 하나(민형님 공유) |
| `DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs` | 칸 둘·검사 |
| `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` | 화살표 프리팹 연결 |
| 임시 화살표 프리팹(위치는 Q-KB5) | 새 에셋 |

Core·Buffer·Framework·GlobalSystem·씬 변경 없음.

시험(새 `[Test]` 없음, 상시 지시):

| 시험 | 작성 | 영향 | 처리 |
|---|---|---|---|
| `CombatViewSystemTests.FakeDisplay` | 우리 | 계약 멤버 둘(컴파일) | 빈 구현 |
| `CombatInputTestFactory` | 우리 | Q-KB4 A면 인자(컴파일) | 아무것도 안 하는 가짜를 기본으로 |
| `AttackTrajectoryInteractionTests:130` | 우리 | Q-KB4 A면 인자(컴파일) | 가짜 넘김 |
| `WallAttackInteractionTests:242` | 천명님 | Q-KB4 A면 인자(컴파일) | 가짜 넘김 한 줄, Core 전달 문서 §5에 기록 |
| `FieldOutlineRendererTests:84·312` 등 | 우리 | 층 하나 → 손잡이 수 기대값(실행) | 기대값 맞춤(Q-KB-T) |
| `ViewDataTableTests` 설정 도우미 둘(`:473·485`) | 우리 | 화살표 필수 → 맞는 설정이 검사에서 실패(실행) | 도우미에 화살표 프리팹 한 줄씩(궤적 TJ-2 때와 같음) |

공유(미전달로 작성):
- 민형님 Factory 공유: 입력 Factory 연결 줄 하나.
- 호진님: `StageFieldViewData` 칸 둘(View DB 표, 궤적 TJ-2 공유 메모 선례).
- 천명님 Core 전달 문서 §5: `WallAttackInteractionTests` 한 줄(Q-KB4 A일 때). View 요청은 없다.

## 6. 정할 것 — 결정 완료(2026-10-27, 모두 권장안 A)

| 번호 | 질문 | 권장(= 결정) | 다른 안 |
|---|---|---|---|
| Q-KB1 | 어떻게 보이나 | **A 화살표(대상 앞 가장자리, 밀리는 방향) + 도착 칸 테두리.** 방향과 거리가 모두 보이고, 끌어오기 도착 칸과 모양이 같다 | B 도착 칸 테두리만: 가장 가볍지만 방향이 안 보인다. C 시작→도착 점선 + 화살촉: 거리는 더 잘 보이지만 대상마다 메시가 하나씩 들고, 짧은 넉백(1칸)에서는 A와 차이가 적다 |
| Q-KB2 | 멈춘 이유를 어떻게 가르나 | **A 화살표 색 셋(밀림·막힘·면역), 프리팹 하나.** 막힘·면역은 도착 칸이 없어서 "안 움직임"이 함께 보인다. `Interrupted`는 밀림과 같고, 사망은 HUD 체력 예상이 보인다 | B 막힘 자리(충돌)·면역 표지 프리팹을 따로 더 둠: 더 분명하지만 자원 둘이 더 들고 지금 아트가 없다. 아트가 생기면 B로 넓힐 수 있다. C 면역은 그리지 않음: Core가 면역을 따로 기록한 뜻(Core Q-K3)을 버린다 |
| Q-KB3 | 언제 지우나 | **A 결과 재생 직전(`BeforeResult`), 궤적과 같은 자리.** 실제로 밀리는 재생 위에 옛 화살표가 남지 않는다 | B 영향 칸처럼 결과 후처리까지 남김: 줄 하나가 줄지만, 대상이 밀려난 뒤 빈 칸에 화살표가 남는다 |
| Q-KB4 | 입력 쪽 연결 | **A 필수 인자.** 운영에서 늘 있는 의존이라 `?.`를 두지 않는다(V5 "운영 조립은 전부 필수" 방향). 시험 조립 셋은 컴파일만 맞춘다(천명님 한 줄 포함) | B 궤적처럼 끝 선택 인자: 시험 무변경이지만 `?.` 넷이 생긴다. 궤적 인자는 이번에 고치지 않는다(둘 다 필수로 바꾸는 것은 따로 할 정리, 한 클래스에 두 방식이 남는 것은 낮음으로 기록) |
| Q-KB5 | 화살표 자원 | **A View DB 필수 프리팹 + 임시 프리팹.** 임시 프리팹은 사용자가 에디터에서 기본 도형(납작한 큐브 둘)으로 만들어 `Prefabs/Combat/`에 두고, 아트가 나오면 바꾼다. 코드와 에셋은 한 커밋 | B 코드로 메시(궤적 재질 재사용): 자원이 필요 없지만 그리기 코드가 늘고 아트로 바꾸기 어렵다. C 선택 칸(없으면 화살표 없이 도착 칸만): 빠뜨려도 조용히 안 보인다 |
| Q-KB6 | 끌어오기 도착 칸과 합칠지 | **A 이번에는 따로.** 층만 나눈다(선례 `AllyHoverRange`). 끌어오기 동작·천명님 시험 무변경 | B 한 소유자로 합침: "공격이 옮길 칸"이 한 곳이 된다. 대신 천명님 `PullAndMarkPresentationTests:233-241`이 보는 뜻("영향 칸과 함께 지워짐")이 바뀌어 허락이 필요하고, 끌어오기 도착 칸이 결과 재생 전에 사라지는 보이는 차이가 생긴다. 하려면 따로 Phase로 |
| Q-KB-T | 우리 시험 실행 깨짐 맞춤 | **a~b 모두 맞춤.** a `FieldOutlineRendererTests` 손잡이 수(층 하나 더함, 10-26 병합 때와 같은 꼴), b `ViewDataTableTests` 설정 도우미 둘에 화살표 프리팹 | — |

## 7. Phase(안)

| Phase | 내용 | 화면 |
|---|---|---|
| KB-a 값·그리기 부품 | `StageFieldViewData` 칸 둘·검사, 임시 프리팹 연결(사용자 에디터), `FieldView` 색 셋·캔버스·부품 수명, `FieldKnockbackMarkers`, `FieldGridMapper.StepToLocal`, 층 한 줄, 시험 맞춤(Q-KB-T) | 변화 없음(아무도 안 부름) |
| KB-b 구성·입력 연결 | `CombatKnockbackPreview`, `CombatAttackFeedback` 두 메서드, 계약·`ICombatBattleDisplay`·`CombatSceneDisplay`, `AttackTargetingInteraction` 네 줄·인자, 입력 Factory 연결 줄, 시험 컴파일 맞춤 | 넉백 미리보기가 나옴 |
| KB-c 문서·공유 | 공유 메모 셋, 폴더 배치 안내(새 파일 넷), CLAUDE.md | — |

Phase마다 멈추고 보고한다. MSBuild는 돌리지 않고 "컴파일 미확인"으로 적는다(상시 지시). KB-a 끝에는 Unity에서 DB 생성(부트)과 기존 시험만 확인하면 된다.

## 8. Unity 확인 목록(안, KB-b 뒤)

1. 충격 볼트(Ballista): 적 둘레 3×3에 호버 → 상하좌우·대각선 대상마다 바깥쪽 화살표와 도착 칸.
2. 착탄 중심에 있는 대상·아군에는 화살표가 없다(기록 없음).
3. 벽·유닛 바로 앞 대상: 막힘 색 화살표, 도착 칸 없음. 몇 칸 가다 막히면 막힘 색 + 멈춘 칸.
4. 면역 대상: 면역 색 화살표, 도착 칸 없음.
5. 가시·낙사로 경로 중 사망: 밀림 색 + 멈춘 칸, HUD 체력 0.
6. 밀 수 있는 장애물(다칸이면 점유 칸 전체가 옮겨진 자리).
7. 고지대 위 대상: 화살표가 표면 위에 뜬다.
8. 호버 칸을 옮기면 바로 바뀌고, 칸 밖·불가 칸·공격 모드 종료·결과 재생 시작 때 사라진다.
9. 일반 넉백 특성 유닛이 있으면 공격자 기준 방향.

## 9. 범위 밖

- Core 미리보기에 없는 차이(F12): 숨은 지형, 추가 공격·처치 연쇄의 넉백, 도착 칸 오브젝트 피해(Core Q-K4). 미리보기와 실제가 다를 수 있다.
- 결과 재생은 그대로 `Moved` 사건이다.
- 끌어오기 도착 칸 합치기(Q-KB6 B), 궤적 인자 필수화, 충돌·면역 전용 아트.

## 10. 구현 메모 (2026-10-27, KB-a~KB-c, 기록 원본은 계획 §7)

- **구현된 모양:** 이 설계의 KB1~KB7 그대로다. 아래 다섯만 다르다.
  1. 캔버스가 도착 칸과 표지를 한 호출로 받는다(계획 TP-1). 구성 `CombatKnockbackPreview`는 캔버스 계약만 알고, 어느 층에 칠할지는 `FieldView`가 안다.
  2. 화살표 색은 Renderer마다·재질 칸마다 덮어쓴다(계획 TP-2, ColorSystem 인자가 재질 칸 번호). 재질의 `_BaseColor` 검사는 표지 부품이 필드 구성 때 한다(TP-3).
  3. **P3이 바뀌었다(결정 Q-KBb1 A):** 넉백은 체력·비용 예상과 같은 조건(`CanShowHealthPreview`, 결과 재생 중이 아님)에서만 그린다. Plan이 결과 재생 중에도 미리보기에 답하기 때문이다(`CombatPlan` "미리보기는 연출 중에도 받는다"). 궤적은 그대로다.
  4. **KB2의 높이가 바뀌었다(결정 Q-KBb3 A, 일괄 검토 뒤 대각선 옆 칸):** 점유 칸과 화살표 앞 절반이 걸치는 칸(판 안) 표면 중 가장 높은 값이다. 걸치는 칸은 앞 칸이고, 대각선이면 꼭짓점에서 만나는 양옆 칸 둘도 더한다. 화살표는 그림자를 드리우지도 받지도 않는다(표지 부품이 끔).
  5. KB4의 "필드를 구성하기 전이면 지운다"는 "아무것도 하지 않고 끝낸다"가 됐다(KB-b 검토 낮음 3). 구성 전에는 칠한 층도 표지 부품도 없어 지울 것이 없다.
- **지우는 자리:** 도우미 `ClearShotPreview()`(넉백 + 궤적)를 호버 표시 지우기와 결과 재생 직전이 같이 부르고, 모드 정리는 세대 검사 때문에 따로 지운다. 불가 응답에서는 따로 지우지 않는다(그 요청을 보낼 때 이미 지웠음).
- **자원:** 임시 화살표는 사용자가 만든 `Prefabs/Combat/Arrow.prefab`이다(KB-b 검토 뒤 +Z 방향·충돌체 없음으로 고침, 결정 Q-KBb2 A). 아트가 나오면 프리팹만 바꾸면 된다.
- **Core 질문(Core 전달 문서 §5.54):** 같은 공격 안에서 넉백 전에 대상이 옮겨지는 경우(끌어오기 + 넉백 등)의 점유 칸 어긋남, 2연발 반복 넉백이 미리보기에 없는 것.
- **공유(모두 미전달):** 민형님 Factory 공유 §33·§34, 호진님 메모 §15, 천명님 Core 전달 문서 §5.54.
- **뒤에 바뀐 것(2026-10-27 사용자 요청, 계획 §7.5):**
  - 도착 칸은 전용 색(`FieldView.knockbackDestinationColor`)으로 테두리와 채우기를 그린다. 이로써 P5("끌어오기 도착 칸과 같은 이동 초안 색, 테두리만")는 바뀌었다.
  - 칸 테두리끼리의 위아래는 정렬 순서 표 `FieldSortingOrders`가 정한다. 사용자 허락으로 민형님 `AreaOutlineData`에 정렬 순서 칸을 더했다(민형님 공유 §34).
