# 전투 공격 궤적 대상별 선 설계 (2026-11-14)

> 사용자 요청: "샷건처럼 곡사가 아닌 광역 공격은 궤적 선이 공격 중심으로 이어지는데, 중심이 아니라 타겟 유닛들에게 이어졌으면 한다."
> **결정(2026-11-14 사용자 "권장안대로 하고 설계 문서 써줘"): Q-PT1~Q-PT5 모두 A.** P1~P5 그대로(사용자 "그대로, PT-a 진행해"). **PT-a·PT-b 완료(2026-11-14, 컴파일 미확인, 새 시험 없음, 기록 §8).** **추가 §9 끌어오기 화살표(사용자 "카우보이는 근접이지만 특성으로 먼 적을 끌어와 때린다, 예외 처리 필요" → "권장안대로 하고 설계 문서에 추가해줘" = Q-PL1 A·Q-PL2 A, P-PL1·P-PL2 그대로(사용자 "그대로, PL-a 진행해"). **PL-a 코드 완료(2026-11-14, 컴파일 미확인, 새 시험 없음, 기록 §9.6).**
> 원래 궤적 설계는 [attack-trajectory-design](2026-10-19-combat-attack-trajectory-design.md)(TJ1~TJ9, 구현 평가 §10)이다. 이 문서는 그 설계의 TJ4(모양 표)·TJ5(보이는 때)·TJ1(궤적 하나 = 메시 하나)을 고친다.
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `CC/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`. 줄 번호는 HEAD `620bb80aa` 기준이다.

## 0. 요지

- 직선 종류(샷건·저격·아킴보 등 `CombatAttackPresentationKind.Linear`)의 **가능 응답 궤적**을 "대상마다 한 줄"로 바꾼다.
- 선을 받는 대상은 Core가 준 역할로 고른다.
  - `Primary`(단일 칸 조준의 주 대상): 지금과 같은 선 하나. 끝은 주 칸이다(실제 탄과 같음).
  - `Designated`(방향 조준·직접 지정 칸의 대상, 샷건 부채꼴): 대상마다 한 줄. 끝은 그 대상이 서 있는 위치다.
  - `Additional`(스플래시)·`Effect`(특성 경로)·접촉 없는 보조 피해 대상: 선 없음.
- 그래서 단일 칸 공격(저격·아킴보)은 화면이 지금과 같고, 샷건만 여러 줄이 된다.
- 선을 받을 대상이 하나도 없으면(빈 부채꼴) 지금처럼 가장 먼 영향 칸으로 한 줄이다.
- 막힌 궤적·곡사·근접·전체 조준은 바꾸지 않는다.
- **실제 발사 연출은 바꾸지 않는다.** 샷건 탄은 여전히 하나가 가장 먼 칸으로 날아간다. 미리보기와 실제가 달라지는 이 차이는 §5에 기록한다.

## 1. 확인한 사실 (코드 대조)

### 1.1 지금 궤적이 중심으로 가는 이유

- 가능 응답은 `AttackTargetingInteraction`이 응답을 통째로 넘긴다(`V/Input/Interaction/AttackTargetingInteraction.cs:409-417`).
- 경로 구성 `CombatAttackTrajectory.Show`(`V/Display/Feedback/CombatAttackTrajectory.cs`)는 목표점 하나를 `CombatAttackStyle.TryTarget`으로 정한다.
- 직선·근접의 목표점은 `CombatAttackTarget.Resolve` 하나다(`V/Presentation/Attack/CombatAttackTarget.cs:32`).
  - 주 칸이 있으면 그 칸 중심이다.
  - 방향 조준(샷건)은 주 칸이 없어서, 공격 방향 앞의 가장 먼 영향 칸 하나를 고른다.
- 이 함수는 실제 직선 탄도 같이 쓴다(구현 평가 P-V1, `CombatAttackStyle.cs` 직선·근접 공통 `TryBeginCodeMotion`). 지금 샷건 탄은 그 칸 하나로 날아간다.
- 샷건 외형의 공격 연출은 `CombatAttack_Linear.asset`(직선 종류)이다.

### 1.2 Core가 주는 대상 정보

- 가능 응답의 `AttackPreview.Targets`(`CC/Contracts/Results/AttackPreview.cs:28`)에 맞는 대상이 모두 있다.
- 대상마다 `TargetId`, `Role`, `IsDirectHit`, `TargetOccupiedCells`가 있다(`CC/Contracts/Results/AttackTargetPreview.cs`).
- 역할은 `AttackTargetResolver.ResolveRole`(`CC/Domain/Combat/AttackTargetResolver.cs:80-94`)이 차지한 칸으로 정한다.
  - 주 칸을 차지하면 `Primary`
  - 직접 지정 칸(`DesignatedCells`)을 차지하면 `Designated`
  - 추가 칸(스플래시)만 차지하면 `Additional`
- `IsDirectHit`는 "공격 전달 접촉이 있음"이다(`AttackTargetPreview.cs:69`). 스플래시 대상도 접촉이 있어 `true`다. 그래서 `IsDirectHit`만으로는 스플래시를 거를 수 없고, **역할로 골라야 한다**.
- 희생(대신 맞기)으로 수호자가 맞으면 `TargetId`는 실제로 맞은 수호자이고 역할은 원래 대상의 역할이다(`CC/Application/Combat/CommandAttackLedger.cs:39-44`). 선은 실제로 맞는 수호자에게 간다.

### 1.3 그리는 쪽

- 캔버스 계약 `ICombatTrajectoryCanvas`(`V/Display/Field/ICombatTrajectoryCanvas.cs`)는 경로 하나만 받는다.
- 그리기 `FieldTrajectoryLine`은 선 하나 = 메시 하나 = 표시 오브젝트 하나다(TJ1). 점 배열 사전 `_points`는 선마다 따로 있다.
- 계약을 쓰는 곳은 `CombatAttackTrajectory`·`FieldView` 둘뿐이고, 시험 참조는 없다.

## 2. 설계

### PT1. 어떤 종류가 대상마다 그리나 (Q-PT1 A)

- 공격 종류 전략에 질문 하나를 더한다: `CombatAttackStyle.DrawsPerTarget`(기본 `false`, `CombatLinearStyle`만 `true`).
- 곡사는 `false`다(사용자 요청 "곡사는 아닌데"). 근접은 궤적이 없다(TJ4 그대로).
- "어느 종류인가"는 전략이 답하고, "어느 대상에 선을 긋나"는 경로 구성이 정한다(§PT2). 지금의 책임 나눔(TJ2, View 3라운드 W8)과 같다.

### PT2. 어느 대상에 선을 긋고, 끝은 어디인가 (Q-PT1 A·Q-PT2 A)

규칙의 원본은 `CombatAttackTrajectory` 한 곳이다("어느 증거가 궤적을 끊나"와 같은 자리).

| 대상 역할 | 선 | 끝점(월드, 바닥) |
|---|---|---|
| `Primary` | 하나 | 지금과 같은 `style.TryTarget` 결과(주 칸 중심) |
| `Designated` | 대상마다 하나 | 대상이 서 있는 위치(`ICombatUnitPlacementReader.TryGetPlacedPosition`) |
| `Additional`·`Effect`·접촉 없음 | 없음 | — |

- 끝점에서 발사 높이 올리기와 출발점 0.2 앞 당기기는 지금처럼 전략의 `Segment`가 선마다 한다. 출발점(공격자 위치)은 모든 선이 같다.
- `Primary`의 끝을 주 칸으로 두는 이유: 단일 칸 공격은 실제 탄이 주 칸으로 날아간다. 대상 위치로 바꾸면 여러 칸 유닛(보스)을 칠 때 미리보기와 실제가 어긋난다. 그래서 단일 칸 공격은 지금과 똑같이 둔다.
- 서 있는 위치를 읽지 못하는 대상(구조물·장애물, 꺼진 표시)은 `TargetOccupiedCells` 칸 중심들의 평균을 끝으로 쓴다.
- 선 순서는 `Targets` 순서이고, 같은 `TargetId`는 한 번만 긋는다.
- 끝이 출발점과 거의 같아 길이가 0인 선은 그리기가 알아서 숨긴다(`FieldTrajectoryLine.Show`의 `MinLength`, 지금 규칙).

### PT3. 선 받을 대상이 없을 때 (Q-PT3 A)

- 직선 종류인데 `Primary`·`Designated` 대상이 하나도 없으면, 지금처럼 `style.TryTarget` 결과(가장 먼 영향 칸)로 한 줄을 긋는다.
- 그래서 "대상 없음 → 지금과 같음", "대상 있음 → 대상마다"가 된다.

### PT4. 막힘·다른 종류는 그대로 (Q-PT5 A)

- 막힌 응답은 대상 목록이 없다. 지금처럼 `Direct` 벽에서 끊긴 한 줄이다(TJ6 그대로).
- 곡사는 지금처럼 주 칸으로 곡선 한 줄이다. 근접·전체 조준은 궤적이 없다. 끌어오기처럼 이동만 하는 공격도 궤적이 없다.

### PT5. 선 여러 개를 그리는 구조

선 하나 = 메시 하나(TJ1)는 유지하고, 그 선을 여러 개 갖는다.

- **경로 값** `FieldTrajectoryPath`(새, `V/Display/Field/`): 점 배열과 개수를 묶은 readonly struct.
- **캔버스 계약** `ICombatTrajectoryCanvas`
  - `ShowAttackTrajectory(Vector3[] localPath, int count, bool blocked)` → `ShowAttackTrajectories(IReadOnlyList<FieldTrajectoryPath> paths, bool blocked)`
  - 목록의 i번째를 i번째 선에 그리고, 남는 선은 숨긴다. `ClearAttackTrajectory()`는 그대로 둔다.
- **그리기 묶음** `FieldTrajectoryLines`(새, `V/Display/Field/`)
  - `FieldTrajectoryLine` 목록을 갖는다. `FieldView`가 지금 `FieldTrajectoryLine` 하나 대신 이것을 갖는다.
  - 필드를 구성할 때는 선 하나만 만든다. 그보다 많은 경로가 오면 모자란 선을 그때 만들어 목록에 더하고, 해제할 때까지 다시 쓴다.
  - 해제는 선마다 각각 시도하고 실패는 모아 한 번 던진다(지금 `FieldTrajectoryLine.Release` 규칙과 같음).
  - 막힘 표지는 선마다 있다(지금처럼 막힘일 때만 보임). 막힘 응답은 늘 선 하나라 표지도 하나만 보인다.
- `FieldTrajectoryLine` 자체는 바꾸지 않는다.

### PT6. 같은 입력 건너뛰기와 할당

- `CombatAttackTrajectory`의 "지난번과 같은 입력이면 캔버스를 부르지 않음"(성능 설계 §3.3, 구현 평가 P-V5)을 선 목록으로 넓힌다. 같은 Mapper·공격자·막힘 여부, 선 수, 선마다의 시작·끝·끊는 진행률이 모두 같아야 건너뛴다.
- 직선 경로 버퍼(2점)는 선마다 하나씩 목록으로 갖고, 늘어날 때만 만든다. 경로 값 목록·지난 입력 목록도 한 번 만든 것을 다시 쓴다.
- 호버 칸이 바뀔 때만 다시 계산한다. 매 프레임 계산은 없다(지금과 같음).
- 할당은 처음 보는 선 수에 닿을 때만 생긴다(선 오브젝트·메시·색 핸들·점 배열 하나씩).

### PT7. 미리보기와 실제가 다른 점 (Q-PT4 A)

- 샷건의 미리보기는 대상마다 선이 나가지만, 실제 탄은 하나가 가장 먼 칸으로 날아간다.
- 원래 궤적 설계의 원칙 "궤적 = 실제 탄과 같은 함수"(구현 평가 P-V1)에서 `Designated` 선만 벗어난다. `Primary` 선과 빈 부채꼴 선은 지금처럼 같은 함수를 쓴다.
- 실제 탄도 대상마다 쏘려면 투사체 발사·맞는 순간 Gate·대상별 해제를 고쳐야 한다. 이번 범위가 아니다(§6 후보).

## 3. 고치는 파일

| 파일 | 변경 |
|---|---|
| `V/Presentation/Attack/CombatAttackStyle.cs` | `virtual bool DrawsPerTarget => false` |
| `V/Presentation/Attack/CombatLinearStyle.cs` | `DrawsPerTarget => true` |
| `V/Display/Feedback/CombatAttackTrajectory.cs` | PT2·PT3 규칙, 선 목록 버퍼, 지난 입력 목록(PT6), 캔버스 새 입구 호출. 막힘 경로도 선 하나짜리 목록으로 넘김 |
| `V/Display/Field/ICombatTrajectoryCanvas.cs` | `ShowAttackTrajectories` |
| `V/Display/Field/FieldTrajectoryPath.cs` (새, `.meta` 함께) | 경로 값 |
| `V/Display/Field/FieldTrajectoryLines.cs` (새, `.meta` 함께) | 선 묶음 |
| `V/Display/Field/FieldView.cs` | 선 묶음을 만들고 해제, 캔버스 새 입구 구현 |
| `docs/superpowers/viewDocs/combat-view-folder-guide.md` | 새 파일 두 줄 |

- 입력 쪽(`AttackTargetingInteraction`)·공격 피드백(`CombatAttackFeedback`)·Factory·DB 값·셰이더는 바꾸지 않는다. 입력 쪽은 이미 응답을 통째로 넘긴다.
- 모두 우리 영역(전투 View)이라 다른 작업자에게 넘길 공유 메모가 없다.
- 시험은 쓰지 않는다(CLAUDE.md, View 작업 시험 금지). 이 계약을 참조하는 기존 시험도 없다. MSBuild도 돌리지 않는다. 컴파일과 재생은 사용자가 Unity에서 확인한다.

## 4. 원칙 점검

- **SSOT**
  - "어느 대상에 선을 긋나"는 `CombatAttackTrajectory` 한 곳이다.
  - "어느 종류가 대상마다 긋나"는 전략의 `DrawsPerTarget` 한 곳이다.
  - 역할 판정은 Core 값을 그대로 쓰고, View에서 다시 계산하지 않는다.
  - 예외: `Designated` 선의 끝은 실제 탄의 목표점 함수와 다르다(PT7, 결정으로 받아들임).
- **SOLID**
  - 그리기는 경로만 받고 대상·공격을 모른다(지금과 같음).
  - 새 종류가 대상마다 그리려면 전략 하나에 `DrawsPerTarget`만 켜면 된다.
- **Framework 지침**
  - Mesh·Color 핸들은 선마다 지금과 같은 방식으로 등록하고 거둔다. 하위 Render는 바꾸지 않는다.
- **기능 변경의 독립성**
  - 단일 칸 공격·곡사·막힘·근접은 화면이 같다.
  - 바뀌는 것은 직선 종류의 `Designated` 대상이 있는 경우뿐이다.
- **성능**
  - 호버 칸이 바뀔 때만 계산하고, 버퍼는 다시 쓴다(PT6).

## 5. 설계에서 정한 것 (사용자 확인 대상)

- **P1.** 선 대상은 역할로 고른다(`Primary`·`Designated`). 질문 때 권장한 "직격(`IsDirectHit`) 대상마다"는 스플래시 대상도 `true`라서 단일 칸 + 스플래시 공격이 여러 줄이 되어 버린다(§1.2). 역할로 고르면 Q-PT1 A가 뜻한 결과(단일 칸 공격은 그대로, 샷건만 여러 줄)가 된다.
- **P2.** `Primary` 선의 끝은 대상 위치가 아니라 주 칸(지금과 같음)이다. Q-PT2 A(대상 위치)는 `Designated` 선에만 쓴다(PT2 이유).
- **P3.** 서 있는 위치를 못 읽는 대상(구조물·장애물)은 차지한 칸 중심 평균을 끝으로 쓴다.
- **P4.** 선은 필드 구성 때 하나만 만들고, 더 필요하면 그때 만들어 다시 쓴다(선 수 상한 없음).
- **P5.** 희생으로 수호자가 대신 맞으면 직접 지정 칸 대상의 선은 수호자에게 간다(Core `TargetId` 그대로). 주 대상 선은 끝이 늘 주 칸이라 실제 탄처럼 원래 칸으로 간다(PT2, 구현 검토 반영).

## 6. 다음 후보 (이번 범위 밖)

- 실제 샷건 탄도 대상마다 쏘기(PT7의 차이를 없앰).
- 원래 궤적 설계에 남은 보류 항목: MultiLine UV 고침, 둥근 점, 막힘 X 표지 아트, 곡사 클립 유닛의 발밑 출발점.

## 7. Phase와 Unity 확인

- **PT-a 코드**: §3의 코드 파일. 컴파일 미확인으로 보고한다.
- **PT-b 문서**: 폴더 배치 안내, 원래 궤적 설계 머리에 "TJ1·TJ4·TJ5는 이 문서가 고침" 한 줄.

Unity 확인(사용자):

1. 샷건 방향 조준: 부채꼴 안 유닛마다 선이 하나씩 나가고, 끝이 유닛 몸 쪽(발사 높이)인지.
2. 샷건 빈 부채꼴: 지금처럼 가장 먼 칸으로 한 줄인지.
3. 저격·아킴보 단일 칸: 지금과 똑같이 주 칸으로 한 줄인지.
4. 스플래시가 있는 단일 칸 공격: 스플래시 대상에는 선이 없는지.
5. 부채꼴에 구조물·장애물이 걸릴 때: 그 칸 쪽으로 선이 가는지.
6. 막힌 칸: 지금처럼 벽에서 끊긴 한 줄과 막힘 표지가 보이는지.
7. 대상 수가 다른 칸 사이를 오갈 때 남는 선이 남지 않는지(예: 3줄 → 1줄).
8. 공격을 확정하면 재생 시작과 동시에 선이 모두 사라지는지.
9. 곡사(CactusCannon): 지금과 같은 곡선 한 줄인지.

## 8. 구현 기록 (PT-a·PT-b, 2026-11-14)

- 컴파일 미확인(MSBuild 안 돌림, CLAUDE.md). 새 시험 없음. 이 계약을 참조하는 기존 시험 없음.
- 바꾼 파일: §3 표 그대로. 새 파일 `FieldTrajectoryPath.cs`·`FieldTrajectoryLines.cs`는 `.meta`와 함께 커밋한다.
- 문서: 폴더 배치 안내 `Display/Field/` 줄, 원래 궤적 설계 머리 한 줄.
- 계획과 다른 점: 막힘 경로도 선 하나짜리 목록으로 같은 입구를 쓴다(캔버스 입구 하나).

### 8.1 검토(새 에이전트 하나) 반영

결과: 컴파일 오류 없음. 높음 0, 중간 1, 낮음 4.

- **중간(반영)**: 호버 중 선을 더 만들다 실패하면 지난 경로가 남았다. → `FieldTrajectoryLines.Show`가 실패하면 모든 선을 숨기고 다시 던진다.
- **낮음(반영)**: Core의 접촉 없는 보조 행도 역할이 `Primary`일 수 있다(`AttackResultFactory.cs:193-198`, 희생으로 수호자가 맞고 원래 대상이 보조 행으로 함께 올 때). 이 경우 주 대상 선이 두 번 겹쳐 그려졌다. → `IsLineTarget`이 `IsDirectHit`도 보고(§PT2 표의 "접촉 없음 → 선 없음"과 맞춤), 주 대상 선은 한 번만 긋는다.
- **낮음(반영)**: P5 범위가 PT2와 어긋났다 → §5 P5 문구를 고쳤다.
- **낮음(반영)**: `_lastLines.AddRange`가 임시 배열을 만들 수 있다 → 하나씩 옮긴다. 대상 struct는 `ref readonly`로 읽는다.
- **낮음(그대로)**: 해제 실패 예외가 `AggregateException` 두 겹으로 감싸진다. 동작 문제가 아니라 진단 깊이만 다르다.


## 9. 끌어오기 공격의 미리보기 (PL1~PL5, 2026-11-14 추가)

> 사용자 요청: "카우보이는 공격 자체는 근접이지만 특성으로 원거리에 있는 적을 끌어와 타격한다. 이런 경우의 예외 처리도 필요하다."
> 결정(2026-11-14 사용자 "권장안대로 하고 설계 문서에 추가해줘"): **Q-PL1 A**(끌려오는 길을 넉백 화살표로, 공격 점선 없음)·**Q-PL2 A**(화살표 색 = 끌어오기 도착 칸 색). P-PL1·P-PL2 그대로(사용자 "그대로, PL-a 진행해"). PL-a 코드 완료(기록 §9.6).

### 9.1 확인한 사실 (코드 대조)

- 끌어오기 특성(`PullTraitAuthoring`, 사거리 +2칸)을 가진 유닛은 카우보이 하나다(`Unit_Cowboy.asset`, 사거리 1). 공격 연출은 근접(`UnitVisual_Cowboy` → `CombatAttack_Melee`)이다.
- Core는 끌려오는 대상을 공격 방향의 한 걸음(`Path[0] − StartAnchor`)으로 곧게 당긴다(`CC/Application/Combat/PullAttackCalculator.cs:105`). 응답에는 `AttackPreview.Pull`(`AttackPullSnapshot`: 대상·시작 칸·경로·걸음·도착 칸)이 실린다. 막혀 덜 오면 실제로 들어간 칸까지만 기록한다.
- 지금 View가 끌어오기에서 쓰는 것:
  - 실제 타격 칸 = 도착 칸(`V/Presentation/Attack/CombatAttackTarget.cs:14-15` `DeliveryCell`). 공격 방향·맞는 순간은 이미 맞다.
  - 늘어난 사거리 칸(`PullRange` 층)과 도착 칸 하나(`PullDestination` 층, `V/Display/Feedback/CombatAttackFeedback.cs:143-149`).
  - 궤적 선은 없다. 근접 전략이 출발점을 답하지 않고(`CombatAttackStyle.TryResolveTrajectoryOrigin` 기본 false), 끌어오기만 하는 응답(직접 맞는 대상 없음)은 궤적을 지운다(`CombatAttackTarget.IsMovementOnly`).
- 비어 있는 것: **어느 대상이 어느 쪽으로 끌려오는지.** 도착 칸만 칠해져 있어, 멀리 떨어진 적이 끌려온다는 사실이 화면에 없다.
- 넉백 미리보기(`V/Display/Feedback/CombatKnockbackPreview.cs`)가 이미 "대상 점유 칸의 이동 방향 앞 가장자리에 화살표 하나 + 도착 칸 층"을 그린다. 화살표 자리·높이 규칙(`MarkOf`), 점유 칸 읽기(`FillOccupied`), 표지 종류 → 색(`FieldKnockbackMarkers`)이 한 곳씩 있다. 공격 조준이 가능 응답마다 `ShowAim(attack)`을 부른다(`AttackTargetingInteraction.cs:421-425`, 재생 중에는 그리지 않음 — 넉백 결정 Q-KBb1 A).

### PL1. 끌려오는 대상에 화살표 하나 (Q-PL1 A)

- 가능 응답에 `Pull`이 있으면 끌려오는 대상의 점유 칸 앞 가장자리(끌려오는 쪽)에 화살표 하나를 그린다. 자리·높이·회전은 넉백 화살표와 같은 규칙이다(같은 함수, PL4).
- 방향은 Core 첫 걸음(`Steps[0].To − Steps[0].From`)이다. View가 공격자 쪽 방향을 다시 계산하지 않는다.
- 공격 궤적 점선은 긋지 않는다. 실제 연출은 "끌려옴 → 근접 박치기"라, 공격자에서 먼 적으로 가는 선은 화면에서 일어나지 않는 일이다(Q-PL1 B를 버린 이유).
- 끌어오기만 하는 응답(맞는 대상 없음)에도 화살표는 나온다(움직임은 일어나므로).
- 끌어오기와 넉백이 한 응답에 함께 있으면 화살표가 둘이다(지금 데이터에는 없음).

### PL2. 도착 칸은 지금 층 하나 그대로

- 도착 칸 원본은 지금처럼 `CombatAttackFeedback.ShowAffectedCells`의 `PullDestination` 층 하나다. 넉백 미리보기의 도착 칸 목록에는 끌어오기 도착 칸을 넣지 않는다(같은 칸을 두 층이 칠하지 않게, SSOT).

### PL3. 색 (Q-PL2 A)

- 새 표지 종류 `FieldKnockbackMarkKind.Pull = 5`. ~~그리는 쪽은 이 종류를 끌어오기 도착 칸과 같은 색(`FieldView.movementDraftColor`)으로 칠한다. 새 씬 색 칸은 만들지 않는다.~~
- **바뀜(2026-11-14 사용자 "권장안대로 A로 진행해", §9.7):** 화살표 색은 `FieldView` 넉백 색 묶음의 새 씬 값 `pullArrowColor` 하나다. 기본값 = 끌어오기 도착 칸 색(`movementDraftColor`, `Combat.unity` 값과 같음)이라 화면은 그대로이고, 도착 칸 색과 따로 조절할 수 있다. 모양(재질·길이·너비·띄움)은 계속 View DB `StageFieldViewDataSO`의 "Knockback Preview" 묶음을 넉백과 함께 쓴다.
- **바뀜(2026-11-14, [필드 색 DB 설계](2026-11-14-combat-field-colors-db-design.md) FC1):** `pullArrowColor`를 포함한 필드 색은 `FieldView` 씬 값에서 View DB `StageFieldViewData`의 필드 색 묶음(`Colors`)으로 옮겼다. 기본값·뜻은 그대로다.
- 넉백 종류 바꾸기(`KindOf`, 멈춘 이유 → 종류)는 넉백 기록만 받는다. 끌어오기는 멈춘 이유로 색을 나누지 않는다(막혀 덜 와도 같은 색, 도착 칸이 실제 멈추는 칸이라 그것으로 보인다).

### PL4. 구조 (조준 출처 하나에 기록 둘)

- `CombatKnockbackPreview`가 한 응답의 "강제 이동 표지"를 모두 만든다. 출처 규칙(조준 > 전경 입력 중이면 없음 > 포효 예고, 새 SSOT 설계 SN3)은 그대로이고, 조준 출처가 있다는 조건만 넓힌다: `_aimShown = Knockbacks.Count > 0 || Pull != null`.
- 화살표 한 개를 만드는 식(`MarkOf`)은 기록 형식을 받지 않고 (걸음, 시작 칸, 종류)를 받게 나눈다. 넉백은 `(Step, StartAnchor, KindOf(넉백))`, 끌어오기는 `(첫 걸음, StartAnchor, Pull)`로 부른다. 자리·높이 규칙은 한 곳에 남는다.
- 점유 칸은 같은 응답 대상 미리보기에서 읽는다(지금 `FillOccupied`). 대상 목록에 없으면 넉백과 같이 예외다(P-PL1 구현 때 확인 — Core가 끌어오기 대상 행을 늘 만듦).
- 포효 예고 출처도 같은 `Append`를 지나지만 포효에는 끌어오기가 없어 달라지는 것이 없다.
- 계약·캔버스(`ICombatKnockbackCanvas.ShowKnockbacks`)·공격 조준·Factory는 바꾸지 않는다. 그리는 쪽 `FieldKnockbackMarkers`에 색 하나(생성 인자)와 `ColorOf` 갈래 하나, `FieldView` 생성 줄 인자 하나가 는다.

### PL5. 바뀌지 않는 것

- 근접 전략의 궤적 없음, `IsMovementOnly`의 궤적 지움, 실제 타격 칸(`DeliveryCell`), 끌어오기 사거리·도착 칸 층, 재생 중 그리지 않는 조건, 결과 재생의 끌어오기 이동 연출(`CombatPullEventHandler`)은 그대로다.

### 9.2 고치는 파일

| 파일 | 바꾸는 것 |
|---|---|
| `V/Display/Field/ICombatKnockbackCanvas.cs` | 표지 종류 `Pull = 5` 한 줄과 문서 |
| `V/Display/Feedback/CombatKnockbackPreview.cs` | 조준 출처 조건, 끌어오기 화살표 더하기, `MarkOf` 인자 나누기, 끌어오기 점유 칸 대체(시작 칸) |
| `V/Display/Field/FieldKnockbackMarkers.cs` | 끌어오기 색 생성 인자·`ColorOf` 갈래 |
| `V/Display/Field/FieldView.cs` | 표지 부품 생성 줄에 `movementDraftColor` 인자 하나 |

새 파일·에셋·Core 변경·Factory 변경 없음. 바뀌는 줄은 모두 우리 줄이다(넉백 미리보기 KB·포효 예고 RF·새 SSOT SN3). 이 형식들을 부르는 기존 시험은 구현 때 grep으로 확인하고, 생성자 인자 때문에 깨지는 곳만 맞춘다(새 시험 없음).

### 9.3 원칙 점검

- **SSOT**: 끌려오는 방향·도착 칸은 Core `Pull` 그대로. 화살표 자리 규칙은 `MarkOf` 하나를 넉백과 함께 쓰고, 도착 칸은 `PullDestination` 층 하나(PL2), 색은 도착 칸 색을 다시 쓴다.
- **SOLID**: 강제 이동 표지 구성 하나가 넉백·끌어오기 기록을 같은 화살표로 바꾼다. 종류가 하나 늘어도 그리는 쪽은 색 갈래 하나만 는다.
- **FW 계약**: Framework·GlobalSystem·Core 무변경.
- **변경 독립성**: 공격 궤적(선) 쪽은 건드리지 않는다. 끌어오기 표시를 빼려면 `Append`의 끌어오기 몇 줄만 걷으면 된다.

### 9.4 설계에서 정한 것 (사용자 확인 대상)

- **P-PL1.** 끌어오기 대상이 응답 대상 목록에 없으면 예외 대신 시작 칸 하나로 화살표를 둔다(끌어오기만 하는 응답 때문). 구현 때 Core 대상 목록을 대조해, 늘 들어 있으면 넉백과 같이 예외로 바꾸고 기록한다.
- **P-PL2.** 화살표 방향은 첫 걸음이다. Core가 한 방향으로만 당기므로 경로 전체 방향과 같다. 경로 칸마다 화살표를 늘어놓지 않는다(넉백과 같음).

### 9.5 Phase와 Unity 확인

- **PL-a 코드**: §9.2 파일. 컴파일 미확인으로 보고한다. 끝나면 새 에이전트로 검토한다.
- **PL-b 문서**: 이 절의 구현 기록, 넉백 미리보기 설계 머리에 "끌어오기 화살표는 궤적 대상별 선 설계 §9" 한 줄.

Unity 확인(사용자):

1. 카우보이 공격 모드에서 늘어난 사거리 칸의 적에 호버: 그 적의 카우보이 쪽 가장자리에 도착 칸 색 화살표가 나오고, 도착 칸이 지금처럼 칠해지는지.
2. 공격 궤적 점선은 나오지 않는지(지금과 같음).
3. 기본 사거리(붙어 있는) 적에 호버: 끌어오기가 없으면 화살표가 없는지.
4. 끌려오는 길이 막혀 덜 오는 경우: 화살표는 같은 색, 도착 칸은 실제 멈추는 칸인지.
5. 다른 칸으로 호버를 옮기거나 공격 모드를 끝내면 화살표가 사라지는지, 포효 예고가 있으면 그것이 다시 보이는지.
6. 공격을 확정하면 재생 중에는 화살표가 없고, 끌려오는 연출이 지금과 같은지.
7. 넉백 공격(다른 유닛)의 화살표 색·자리가 지금과 같은지.

### 9.6 구현 기록 (PL-a, 2026-11-14)

- 컴파일 미확인(MSBuild 안 돌림, CLAUDE.md). 새 시험 없음. 바꾼 형식을 부르는 기존 시험 없음(`Assets` 전체 grep: `FieldKnockbackMarkers` 생성은 `FieldView` 한 곳, `CombatKnockbackPreview`·`FieldKnockbackMarkKind`는 시험 참조 없음).
- 바꾼 파일: §9.2 표 그대로. `FieldView`는 생성 줄 인자 하나와 `movementDraftColor` 툴팁("끌어오기 도착 칸과 끌려오는 대상의 화살표도 이 색") 한 줄.
- **P-PL1 확인 결과 — 예외로 둠:** Core는 맞지 않는 이동 대상(넉백·작렬 부딪힘·끌어오기)에도 보조 대상 행을 늘 만든다(`CC/Application/Combat/AttackResultFactory.cs`의 `movementTargets`, 끌어오기 대상 포함). 끌어오기만 하는 응답에서도 대상 행이 있어, 설계의 "없으면 시작 칸 하나" 대체를 두지 않고 넉백과 같이 Core 계약 위반 예외로 했다(설계 §9.4가 미리 정한 갈래). 대상 행의 점유 칸은 Core 입력(끌려오기 전) 캐릭터의 칸이라 시작 자리 기준이다.
- 계획과 다른 점: `FillOccupied`가 기록 대신 (대상, 시작 칸)을 받게 바뀌었다(넉백·끌어오기 함께 씀). 예외 문구가 "넉백 기록의 대상" → "강제 이동 기록의 대상"으로 바뀌었다.

#### 9.6.1 검토(새 에이전트 하나) 반영

결과: 컴파일 대조 문제 없음, 높음 0·중간 0·낮음 4. 기존 넉백·포효 경로 동작 같음, P-PL1의 Core 주장(끌어오기 대상 행이 늘 있음, 점유 칸·시작 칸 모두 끌려오기 전 자리)과 걸음 0 없음·한 방향 걸음 확인.

- **낮음 1(기록만)**: 같은 대상이 끌려온 뒤 넉백되면 넉백 도착 칸·화살표가 끌려오기 전 점유 칸 기준이라 어긋날 수 있다(Core 전달 문서 §5.54 "넉백 전 이동으로 점유 칸 어긋남"과 같은 원인, 기존 문제). 지금 데이터에는 끌어오기 + 넉백을 함께 가진 유닛이 없다.
- **낮음 2(반영)**: 포효 예고도 같은 `Append`를 지나 포효에 끌어오기가 생기면 도착 칸 층 없이 화살표만 나온다 → 지금은 닿지 않음을 클래스 주석에 적었다.
- **낮음 3(반영)**: 넉백만 말하던 주석 다섯(`CombatKnockbackPreview` 규칙 문구·`_aimShown`·`ClearAim`, `ICombatKnockbackCanvas` 계약 요약, `FieldView` 클래스 문서)을 "넉백·끌어오기"로 고쳤다.
- **낮음 4(그대로)**: 결과 재생 중에는 끌어오기 도착 칸 층만 보이고 화살표는 없다(넉백 결정 Q-KBb1 A와 같은 조건).

### 9.7 끌어오기 화살표 색 분리 (2026-11-14 추가)

- 사용자: "Knockback Preview처럼 StageFieldViewDataSO에 빼는 게 좋지 않아?" → 확인: 화살표 모양 값은 이미 SO의 넉백 묶음을 함께 쓰고, 색은 넉백 색도 `FieldView` 씬 값(넉백 설계 KB6). 후보 A(색만 씬 값으로 따로)·B(SO에 끌어오기 모양 묶음 따로 — 화살촉 부품이 크기·재질 하나라 부품 추가 필요)·C(넉백 색까지 SO로 — KB6 뒤집기) 중 **A**(사용자 "권장안대로 A로 진행해").
- 바꾼 것: `FieldView`에 `pullArrowColor`(툴팁, 기본값 = 이동 예정 경로 색) 한 칸, 표지 부품 생성 줄 인자를 `movementDraftColor` → `pullArrowColor`, `movementDraftColor` 툴팁과 `FieldKnockbackMarkKind.Pull` 문서 고침. 컴파일 미확인, 새 시험 없음. 씬 파일은 안 고침 — 다음 저장 때 칸이 생기고 그 전에는 코드 기본값(지금 색과 같음)을 쓴다.
- Unity 확인: 끌어오기 화살표 색이 지금과 같고, `Combat.unity` FieldView 인스펙터 넉백 색 묶음 끝에 `Pull Arrow Color` 칸이 보이며 바꾸면 화살표만 바뀌는지(도착 칸은 그대로).
- **대신함(2026-11-14):** 이 색 칸은 [필드 색 DB 설계](2026-11-14-combat-field-colors-db-design.md) FC1로 View DB 필드 색 묶음(`Colors`)에 있다(`FieldView` 씬 칸은 지움). 위 Unity 확인은 설계 §7의 U-4로 갈음한다.
