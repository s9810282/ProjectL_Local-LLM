# Ballista 공격을 곡사(Cannon)와 같게 — A1 계획 (2026-10-26, 코드 변경 없음)

- **목적:** 새 Core(`e413008c`)에서 Ballista가 공격할 때마다 예외로 멈추는 문제(정리 문서 A1)를 푼다. 사용자 지시는 "바뀐 특성보다 공격 형태부터, 일반 Cannon과 같게"다. 클립 이벤트 타이밍에 포구에서 곡선 한 발을 쏜다. 화살은 실제 타격 칸(`DeliveryCell`)에 떨어지고, 떨어지는 순간 맞은 대상을 모두 해제한다.
- **기준:** HEAD `f7fd2520`(천명님 `e413008c` 병합 커밋). 줄 번호는 이 판 기준이다.
- **입력:** [대응 항목 정리](../specs/2026-10-26-combat-view-core-e413008-response-inventory.md) A1, 천명님 [충격 볼트 인계](../handoffs/2026-10-26-ballista-impact-bolt-core-to-view-handoff.md) §5.
- **약어:** `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `VDB/` = `Assets/ProjectL/DataBaseSO/ViewGroup/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- **범위 밖:** 충격 볼트 특성 표시·거리 설명(B1·B2), 끌어오기 안내·채움(B3·B4), 아이콘(B5), 밀려날 칸 미리보기(B6), 관통 길 정리(C1), 다른 사람 시험(정리 문서 §3). 밀치기 이동은 기존 넉백 이동 후속이 재생하므로 코드는 바꾸지 않고 재생으로 확인만 한다(§5).

## 1. 지금 사실

**실패 경로**

- 공격 처리기는 공격 클립이 있으면 클립 길로 간다(`V/Playback/Handlers/CombatAttackEventHandler.cs:60-61`).
  - 원거리면 발사 조각이 전략의 `FireShot`을 부른다(`:77-79`).
  - Ballista 전략은 `PiercingShot`이 null이면 예외를 던진다(`V/Presentation/Attack/CombatBallistaStyle.cs:71-72`).
  - 새 Core는 늘 null이라 공격마다 F1로 멈춘다.

**곡사(Cannon)의 두 길**

| 길 | 쓰는 유닛 | 발사 지점 | 궤적 미리보기 출발점 |
|---|---|---|---|
| 코드 모션 | 선인장 대포 `UnitVisual_CactusCannon`(클립 없음) | 곡사 무기 `CatusCannonWeaponView` 총구 | 곡사 무기 총구 |
| 클립 | 언데드 대포 카트 `UnitVisual_Cannon`(클립 `CombatUnitAnimation_Cannon`) | 클립 포구 `CombatAnimationEvents.muzzle` | 적이라 그리지 않음 |

- 클립 길 곡사 발사는 `V/Presentation/Attack/CombatMortarStyle.cs:77-86`이다.
  - `DeliveryCell` 중심에 착탄한다(주 칸이 없으면 예외).
  - `FireArc`로 비행 시간·곡사 높이·회전 값을 쓴다.
  - 착탄 순간 접촉 이펙트를 낸 뒤 맞음을 알린다.
- 해제 입구는 기본값(이 출처의 대상 모두 해제)이다(`V/Presentation/Attack/CombatAttackStyle.cs:88-92`).
- 공격 방향(`CombatAttackFacing.For`)과 목표점은 종류 전략의 `TryTarget(DeliveryCell, …)` 하나다. 곡사·Ballista 모두 그렇다.

**Ballista 자산은 곡사 조건을 이미 만족한다**

- **외형 설정:** `VDB/CombatPresentation/Setting/CombatAttack_Ballista.asset`은 `kind: 4`다. 곡사 검사 칸(`flightDuration 0.8`, `arcHeightInCells 2`, `spinDegreesPerSecond (60,30,15)`, `prepareDuration 0.15`, `returnDuration 0.25`, `bucketPitch -20`)을 모두 갖고 있다. 그래서 곡사 검사(`DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationSO.cs:93-104`)를 통과한다. 투사체 프리팹(화살)·접촉 이펙트(4)도 그대로다.
- **프리팹:** `Prefabs/Characters/Colored/Playerable/Ballista.prefab`에 `CombatAnimationEvents`(포구·이펙트 지점 있음)가 있고, 곡사 무기는 없다.
- **클립 묶음:** `CombatUnitAnimation_Ballista`는 피격 선딜레이가 0이다. 그래서 원거리 클립 규칙(`CombatUnitAnimationRules.cs:42-50`)을 통과한다.

**종류만 바꾸면 궤적에서 멈춘다**

- 곡사 전략의 궤적 출발점은 곡사 무기 총구다(`CombatMortarStyle.cs:30-41`).
- 창구 `TryGetMortarWeapon`은 무기가 없으면 예외다(`V/Display/Unit/CombatUnitPresentationWindow.cs:234-244`).
- 그래서 외형 종류만 곡사로 바꾸면, 공격 모드에서 칸을 호버할 때마다 예외(F1)가 난다. 정리 문서의 "궤적이 사라진다"는 실제로는 예외다.
- 언데드 대포는 적이라 이 길에 닿은 적이 없다.

**Core 쪽**

- 지정 방식 7은 사각 범위·칸 지정(`choiceMode 1` = Single)이다. 인계 §4는 의도한 착탄 칸을 `PrimaryCell`로 정한다.
- 이동 사건 없이 피해만 있는 공격에서 `CombatHitFollowUps`는 `PiercingShot`이 null이면 이동 후속을 모두 히트 해제 대상으로 본다(`V/Presentation/Attack/CombatHitFollowUps.cs:111`). 그래서 충격 볼트 밀치기는 기존 넉백과 같은 길로 착탄 뒤 재생된다.

## 2. 정할 것

**결정(2026-10-26 사용자):**
- **Q-BC1 A.** 사용자는 "그냥 발리스타를 EnemyCannon이랑 맞추면 되는 거 아냐?"라고 물었다. 종류만 바꾸면 궤적 미리보기에서 멈춘다는 설명을 들은 뒤 "①로 진행해"라고 정했다.
- **Q-BC2 B.** 궤적 출발점은 유닛 위치다. 궤적이 발밑에서 시작하는 것을 받아들였다.
- **Q-BT1 기대값만 바꿈.** ①의 설명에 포함해 허락받았다.
- 아래 §4 Task 중 T1·T2·T6은 빠진다.

### Q-BC1 방법

| 안 | 내용 | 장점 | 단점 |
|---|---|---|---|
| **A (권장)** | 외형 설정 `kind`를 곡사(3)로 바꾸고, 곡사 전략의 궤적 출발점을 클립 길 유닛에도 맞춘다 | 언데드 대포와 같은 코드 한 길이다. 곡사 착탄 규칙이 한 곳이다. 지시 "Cannon과 같게"를 그대로 따른다. 바뀌는 우리 시험은 기대값 하나다 | Ballista 전략·관통 길을 쓰는 유닛이 없어진다(C1에서 정함) |
| B | Ballista 전략을 곡선 한 발로 고쳐 쓴다(종류 Ballista 유지) | Ballista만의 연출 자리가 남는다 | 곡사와 같은 규칙(목표점·`FireArc`·착탄 해제)이 두 전략에 생긴다(SSOT). 동작이 바뀌어 실패하는 우리 시험이 넷이다: `CombatClipShotTests` Ballista 사례 셋, `CombatShotPathTests:125`의 Ballista 단언 |

정리 문서 Q-E1 권장은 B였다. A로 바꾸는 이유는 둘이다. 문서가 A를 권하지 않은 까닭이던 궤적은 출발점 규칙 하나로 풀린다(Q-BC2). 그리고 지시가 "Cannon과 같게"다.

### Q-BC2 곡사 궤적 출발점(공격 클립이 있는 유닛)

갈래 기준은 "공격 클립이 있으면 클립 길, 없으면 곡사 무기"다. 공격 처리기가 클립 길과 코드 길을 고를 때 보는 사실(`TryGetAttackClip`)과 같다. 갈래의 자리는 곡사 전략의 `TryResolveTrajectoryOrigin` 하나다.

| 안 | 출발점 | 바뀌는 곳 | 비고 |
|---|---|---|---|
| **A (권장)** | 클립 포구의 지금 위치 | 표시 계약 `ICombatUnitPresentationTarget`에 읽기 멤버 하나(`FieldUnitView` 명시 구현), 창구 읽기 하나, 우리 시험 가짜 둘에 한 줄씩(컴파일) | 실제 발사(`CombatFirePiece`가 클립 포구에서 쏨)와 같은 원본이다. 곡사 코드 길의 "무기 총구"와 짝이 맞는다 |
| B | 유닛 배치 위치(바닥) | 곡사 전략만 | 계약은 바뀌지 않는다. 직선 궤적과 같은 관례다. 궤적이 유닛 발밑에서 시작한다(실제 화살은 포구에서 나감) |

### Q-BT1 우리 시험 `CombatBallistaAssetTests`

- `T/CombatBallistaAssetTests.cs:25`가 실제 DB의 Ballista 외형 종류를 `Ballista`로 단언한다.
- A를 하면 이 단언이 곡사로 바뀐다. 확인하는 내용이 이번 변경 그 자체다.
- 권장은 기대값만 `Mortar`로 바꾸는 것이다. 다른 단언(속도·발사 높이·발사 시간·투사체 프리팹)은 에셋 값이 그대로라 남긴다.
- 다른 안은 시험을 지우는 것이다.

## 3. 계획에서 정한 것 (확인 대상)

- **P1** Ballista 종류 값, Ballista 전략, 관통 길, DB 규칙 "Ballista 공격 표시면 공격 클립 필수"(`CombatUnitAnimationRules.cs:32`)는 그대로 둔다. 쓰는 유닛만 없어진다. 걷을지는 C1(Q-E6)에서 정한다.
- **P2** Ballista 에셋의 직선 칸(`projectileSpeed 30`, `projectileHeight 0.6`)은 곡사가 읽지 않지만 그대로 둔다. 기존 시험이 값을 본다.
- **P3** 화살 회전은 에셋 값 그대로 둔다. 곡사 투사체는 출발 때 수평 방향을 보고, 회전 값으로 돈다. 곡선 접선은 따르지 않는다(`CombatProjectilePool.cs:239-244`, `:296-297`). 그래서 화살이 수평에 가깝게 누운 채 천천히 돈다. 재생을 보고 바꿀지 정한다. 바꾸게 되면 에셋 값만 고친다(회전 0 등).
- **P4** 클립 포구가 없을 때의 처리:
  - 공격 클립이 있는데 포구를 읽을 수 없으면 예외로 둔다. 곡사 무기가 없을 때와 같은 데이터 오류다.
  - DB 검사가 원거리 클립 유닛의 포구를 필수로 보므로 운영에서는 오지 않는다.
  - 표시가 없으면 지금처럼 false(궤적 없음)다.
- **P5** "곡사인데 공격 클립이 없으면 곡사 무기 필수"는 DB에서 검사할 수 없다. DataBaseSystem이 ViewSystem 형식(`CatusCannonWeaponView`)을 모르기 때문이다. 지금처럼 첫 공격·첫 호버 때 창구가 예외로 알린다(언데드 대포도 같음). 이번 범위 밖이다.
- **P6** 공유는 천명님 Core 전달 문서에 인계 §5의 2·4항 처리 방식을 한 줄 적는다(외형 종류를 곡사로, 착탄 한 번 해제). 미전달이다. 민형님·현강님·호진님 파일은 바뀌지 않는다.

## 4. Task (Phase BC-a 하나)

전투 View 규칙대로 MSBuild를 돌리지 않고, 새 `[Test]`는 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다.

| Task | 내용 | 파일 |
|---|---|---|
| T1 | 클립 포구 읽기: 표시 계약 멤버 `bool TryGetClipMuzzle(out Vector3 position)`(채널이 없거나 포구가 없으면 false), `FieldUnitView` 명시 구현(`_clipChannel`의 `TryGetMuzzle`, 대여 없이 읽음) | `V/Display/Unit/ICombatUnitPresentationTarget.cs`, `V/Display/Unit/FieldUnitView.cs` |
| T2 | 창구 읽기 `CombatUnitPresentationWindow.TryGetClipMuzzle(EntityId, out Vector3)`: 표시가 없으면 false, 포구가 없으면 예외(P4) | `V/Display/Unit/CombatUnitPresentationWindow.cs` |
| T3 | 곡사 전략 출발점: 공격 클립이 있으면 클립 포구, 없으면 곡사 무기 총구(지금 그대로). 곡사 높이는 같다. 클래스·메서드 설명도 함께 고친다 | `V/Presentation/Attack/CombatMortarStyle.cs` |
| T4 | 궤적 설명 한 줄: "곡사 = 무기 총구" → "곡사 = 실제 발사 지점(클립 길은 클립 포구, 코드 길은 무기 총구)" | `V/Display/Feedback/CombatAttackTrajectory.cs:201` |
| T5 | 에셋 `kind: 4` → `3` | `VDB/CombatPresentation/Setting/CombatAttack_Ballista.asset` |
| T6 | 시험 컴파일 맞춤: 가짜 표시 둘에 새 멤버 한 줄(`false`)을 넣는다. 둘 다 우리 시험이다 | `T/CombatPieceRunTests.cs:867`, `T/CombatUnitPresentationWindowTests.cs:476` |
| T7 | Q-BT1 결정대로 기대값을 바꾼다 | `T/CombatBallistaAssetTests.cs:25` |
| T8 | 정적 검토: 새 에이전트 하나가 계약·갈래·시험 영향을 본다. 높음·중간은 반영하고, 낮음은 기록한다 | — |
| T9 | 기록: 이 문서 §7 실행 기록, P6 공유 한 줄, CLAUDE.md 현재 상태 | 문서 |

- 바뀌지 않는 것: Core·Buffer·Framework·Factory·씬·프리팹·클립 묶음, Ballista 전략, 공격 처리기, 공격 재생.
- Q-BC2가 B면 T1·T2·T6이 빠진다. T3은 "공격 클립이 있으면 배치 위치"가 된다.

## 5. 확인 (사용자, Unity)

**컴파일·시험 묶음**

- `CombatAttackTrajectoryTests`, `CombatClipShotTests`, `CombatShotPathTests`
- `CombatMortarPresentationTests`, `CombatMortarAnimationTests`, `CombatMortarAssetTests`
- `CombatUnitPresentationWindowTests`, `CombatPieceRunTests`, `CombatBallistaAssetTests`, `CombatUnitAnimationSettingsTests`
- 천명님 `CoreIntegration/PullAndMarkPresentationTests`(곡사 궤적 사례: 곡사 무기만 있고 클립은 없어 지금 길 그대로)

**재생**

1. Ballista 공격 모드에서 칸을 호버하면, 포구 근처에서 대상 칸 중심으로 곡선 점선이 보인다(예외 없음).
2. 공격하면 클립 이벤트 타이밍에 화살이 곡선으로 날아 칸 중심에 떨어진다. 접촉 이펙트가 나오고, 같은 순간 영향 칸 대상들이 함께 피격 반응한다.
3. 충격 볼트로 주변 대상이 착탄 뒤 밀려난다. 중심 칸 대상은 그대로다. 여러 대상은 동시에 밀린다.
4. 빈 칸을 지정하면 화살이 떨어지고 끝난다.
5. 반복 공격, 턴 종료·포기·씬 이탈 때 투사체가 남지 않는다.
6. 선인장 대포(코드 길) 궤적·공격과 언데드 대포 공격은 그대로다.
7. 화살 회전을 보고 조정이 필요한지 정한다(P3).

## 6. 원칙 점검 (권장안 A·A 기준, 바뀌는 범위)

| 기준 | 판단 | 예상 |
|---|---|---|
| SSOT | 곡사 착탄 규칙은 한 전략에만 있다. 클립 발사 지점 원본(포구) 하나를 실제 발사와 궤적이 같이 쓴다. 클립 길·코드 길 갈래는 처리기와 같은 사실을 읽는다 | 9.5 |
| SOLID | 전략 표·조립은 그대로다. 표시 계약·창구에 읽기가 하나씩 는다(창구가 이미 큼, 낮음) | 9.0 |
| FW 계약 | 바뀌지 않는다 | 10 |
| 기능 변경 독립성 | Ballista 외형 값 한 줄로 공격 형태가 바뀐다. Core 무관이다 | 9.5 |
| 캡슐화 | 포구는 읽기만 열고, 채널은 숨긴 채 둔다 | 9.5 |
| C#/Unity 권장 | 명시 구현, 대여 없는 읽기다 | 9.5 |
| 무의미한 If/Tick | 새 갈래 하나는 두 길의 실제 차이다. Tick은 없다 | 9.5 |

남는 낮음:
- 쓰는 유닛 없는 Ballista 전략·관통 길(C1).
- 화살 회전(P3).
- 곡사 무기 필수를 DB에서 못 보는 것(P5, 기존).

B·B면 SSOT가 약 8.0이다(곡사 규칙 두 벌). 시험 변경도 넷으로 는다.

결정은 A·B다. A·A와 달라지는 점은 둘이다. 표시 계약과 창구가 늘지 않아 SOLID·캡슐화가 그대로다. 대신 궤적 출발점(유닛 위치)이 실제 발사 지점(클립 포구)과 달라 낮음이 하나 는다. 직선·옛 Ballista 궤적과 같은 관례다.

## 7. 실행 기록

### Phase BC-a (2026-10-26, 컴파일 미확인, 새 시험 없음)

Q-BC2 B라서 T1·T2·T6(표시 계약·창구·시험 가짜)은 하지 않았다.

| Task | 한 것 |
|---|---|
| T3 | `V/Presentation/Attack/CombatMortarStyle.cs` `TryResolveTrajectoryOrigin`: `presentation.TryGetAttackClip(attackerId, out _)`이면 `placements.TryGetPlacedPosition`(없으면 false), 아니면 지금처럼 곡사 무기 총구. 곡사 높이는 같다. 클래스 설명에 두 길의 발사 지점(코드 모션 = 무기 총구, 클립 = 클립 포구)을 적었다 |
| T4 | `V/Display/Feedback/CombatAttackTrajectory.cs` `TryResolveShot` 설명: "곡사 = 무기 총구, 공격 클립이 있으면 유닛 위치" |
| T5 | `VDB/CombatPresentation/Setting/CombatAttack_Ballista.asset` `kind: 4` → `3` |
| T7 | `T/CombatBallistaAssetTests.cs:25` 기대값 `Ballista` → `Mortar`, 클래스 설명에 이유 한 줄. 다른 단언은 그대로다 |

- 바뀐 파일 넷 모두 CRLF가 그대로다.
- 시험 영향을 소스로 대조했다. 곡사 궤적을 실제로 타는 시험은 천명님 `CoreIntegration/PullAndMarkPresentationTests.cs:336`의 mortar 사례 하나다. 이 사례는 곡사 무기를 붙이고 클립 묶음이 없어 무기 길 그대로다. `CombatAttackTrajectoryTests`·`CombatAttackFeedbackTests`에는 곡사 사례가 없다.
- 작업 트리에는 이번 변경이 아닌 것도 있다: `Assets/Scenes/Combat.unity`, FMOD 캐시, `New Fmod Event Catalog.asset`. 에디터가 바꾼 것으로 보이며 손대지 않았다.
- P6 공유: [Core 전달 문서 §41](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 인계 §5의 1~5항 처리 상태를 적었다(미전달).

**T8 정적 검토(새 에이전트 하나):** 높음·중간 0, 낮음 6.

문제 없음으로 확인한 것:
- 실제 공격 경로: 처리기 → 기본 해제 입구(`HitAll`) → 원거리 조립 → 클립 포구 → 곡사 `FireShot`. 착탄 순서는 접촉 이펙트 → 모두 해제 → `Hit` → 도착이다.
- DB 검사 둘(곡사 값, 원거리 클립 규칙)을 통과한다.
- 궤적: Ballista는 곡선, 선인장 대포는 무기 총구 길 그대로다. 두 조회의 "표시 없음" 판정은 같은 `_targets.TryGet`이라 다르지 않다.
- 방향·목표점이 옛 Ballista와 같은 칸 중심이다. 지정 방식 7은 Single이라 성공한 공격에는 주 칸이 있다.
- 종류 `Ballista`에 기대는 분기는 DB 규칙 `CombatUnitAnimationRules.cs:32` 하나뿐이다.
- 깨질 시험이 없다.

낮음 처리:

| 번호 | 내용 | 처리 |
|---|---|---|
| L1 | "Ballista면 공격 클립 필수" 부트 검사가 이 유닛에 더는 걸리지 않는다. 클립을 비우면 실행 중 "곡사 무기 없음" 예외(F1)가 된다 | 기록(P5와 같음, 언데드 대포도 같은 처지) |
| L2 | "공격 클립이 있으면 클립 길" 판정이 처리기(`CombatAttackEventHandler.cs:60`)와 곡사 궤적 출발점 두 곳에 있다(같은 창구 메서드를 읽음) | 기록 |
| L3 | 궤적이 발밑에서 시작한다(옛 Ballista 궤적은 발사 높이로 올렸다) | 사용자 결정 Q-BC2 B, 그대로 |
| L4 | 주석 셋: 궤적 출발점 표현, 곡사 클래스 설명의 유닛 이름, 기반 전략의 "무기 빠진 곡사 = 예외" | **반영**(`CombatAttackTrajectory.cs:201`, `CombatMortarStyle.cs:15`, `CombatAttackStyle.cs:47`) |
| L5 | `CombatBallistaStyle`·관통 접촉 진행률·DB 규칙 `:32`를 쓰는 에셋이 0개 | P1·C1(Q-E6)에서 정함 |
| L6 | 화살이 곡사 회전 값(60, 30, 15)으로 돈다 | P3, 재생으로 확인 |

바뀐 파일은 다섯이다: 코드 셋, 에셋 하나, 시험 하나. 커밋 때 함께 넣는다. 사용자 Unity 확인은 §5 목록으로 한다.
