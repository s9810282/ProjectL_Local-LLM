# 보유 유닛 전투 외 능력치 조회 — View 인수 문서

- 작성일: 2026-10-14
- 작성: Core 작업자(천명)
- 대상: 로비·스테이지 선택 View 작업자(호진님), 전역 덱·HUD UI 담당
- 상태: **작성 완료·미전달.** Core 구현(S1~S3)은 끝났고 Unity EditMode 테스트는 통과했다(사용자 확인). View 연결은 아직 없다.
- 근거: 설계 (Git history: `2026-10-13-owned-character-stats-query-design.md`)(D1~D8, 검토 §13), 계획 (Git history: `2026-10-14-owned-character-stats-query-plan.md`)
- 코드 기준: `92440b83` + 이 작업의 미커밋 변경. 커밋한 뒤 해시를 이 줄에 적는다.

## 1. 무엇이 생겼나

전투 밖에서 **현재 Run의 보유 유닛 전체(메인 + 서브)**의 배치 전 능력치를 읽는 조회가 생겼다. 영혼과 메인 지속 성장이 반영돼 있고, 지형·위치·전투 임시 효과는 들어 있지 않다.

이전에는 전투가 끝나면 `ICharacterBufferReadSink.Characters`가 비어서, 전투 밖에서는 최대 HP·병과·서브 능력치를 표시할 수 없었다. 그래서 `MainCharactorUI`는 현재 HP만, `UnitCardUI`는 능력치 칸을 비워 두고 있다.

## 2. 얻는 법

바인딩 지점(예: `PostBindService`)에서 한 번 얻어 보관한다. `CombatViewSystem`이 `CoreGroup.Combat`을 얻는 방식과 같다.

```csharp
ICharacterStatsReadSink stats = resolver.GetService(SystemGroupsService.InGame).CoreGroup.CharacterStats;
CharacterStatsQueryResult result = stats.ReadOwned();
```

- 표시 클래스에는 `ICharacterStatsReadSink`만 넘긴다. `CoreGroup` 전체를 넘기지 않는다.
- 런타임에서 직접 꺼내는 경로(`runtime.GetService(InGameSystemServiceKey.CharacterStats)`)는 없다. 회복 조회와 같다.
- 계약 위치: `Systems/RuntimeSystem/BufferSystems/Character/ICharacterStatsReadSink.cs`, 결과 형식은 `CoreCaculator/Contracts/Snapshots/`의 `CharacterStatsQueryResult`·`CharacterStatsQueryStatus`·`OwnedCharacterStatsSnapshot`·`CharacterBaselineStatsSnapshot`.

## 3. 결과 읽기

### 3.1 상태

| `Status` | 뜻 | 화면 처리 |
|---|---|---|
| `Ready` | 전투가 없고 보유 유닛이 있다. `Items`가 하나 이상이다. | 목록·상세 표시 |
| `EmptyRoster` | 전투가 없고 보유 유닛이 없다(첫 로비 등 Run 전). | 빈 상태 표시. 표시용 임시 Run·로드아웃 미리보기를 만들지 않는다(설계 D5). |
| `BattleActive` | 전투가 있다(처리 완료 뒤 `EndBattle` 전 포함). `Items`는 비어 있다. | 이 조회로 표시하지 않는다. 전투 중에는 기존 `Characters`를 쓴다. |

- `RunOutcome`: `Ready`여도 `Defeat`·`Victory`일 수 있다. 끝난 Run의 보유 정보가 남아 있는 경우다. 구분 표시 여부는 View가 정한다.
- 예외: DB 초기화·바인딩 전 호출, 보유 유닛 정의 누락, 역할 불일치 같은 데이터 오류는 예외로 보고한다. Core는 오류를 빈 목록으로 숨기지 않는다. View도 잡아서 빈 화면으로 바꾸지 말고 기존 오류 경계로 보낸다.

### 3.2 항목 필드

`Items`는 `ICharacterBufferReadSink.Owned` 순서다. 메인 먼저 같은 화면 정렬은 View가 정한다.

| 필드 | 의미 |
|---|---|
| `OwnedId` | 카드 식별. 선택·고정 슬롯은 이 값으로 한다. |
| `CharacterKey` | 이름·이미지 등 View DB 외형 조회 전용. 같은 키의 카드가 여러 장일 수 있으므로 식별에 쓰지 않는다. |
| `Role` | 메인/서브 |
| `Level` | 보유 레벨. 표시만 한다. 서브의 레벨 성장은 Core가 이미 `Stats`에 반영했다(2026-10-17 레벨 성장 설계 (Git history: `2026-10-17-unit-level-stat-growth-design.md`), 옛 D4 대체). View가 레벨로 수치를 다시 계산하지 않는다. |
| `SoulCount` | 이 카드의 영혼 수. 영혼 효과는 이미 `Stats`에 들어 있다. |
| `Stats.CurrentHealth` | 메인은 지속 체력(0일 수 있음), 서브는 다음 전투를 시작할 때 체력(= 최대 체력). 전투 잔여 체력이 아니다(설계 D6). |
| `Stats.MaxHealth` | 영혼 포함 최대 체력 |
| `Stats.AttackPower` | 영혼 포함 공격력. **View에서 영혼을 다시 더하지 않는다.** |
| `Stats.PhysicalDefense`, `MoveMethod`, `MoveRange`, `MoveCost`, `AttackRange`, `MaxActionCost`, `AttackCost`, `Priority`, `Delay` | 배치 전 기준값 |
| `Stats.CategoryKey`, `Stats.TraitKeys` | 정적 키. 명칭·아이콘·설명문은 View DB(병과·특성 외형)에서 찾는다. |

들어 있지 않은 값: 현재 행동 자원, 남은 이동, 특성 스택, 지형·위치 보정, 전투 임시 보정, `EntityId`, 손패 슬롯.

## 4. 언제 읽나 — 매 프레임 호출 금지

`ReadOwned()`는 호출할 때마다 DB 정의를 변환하고 계산을 새로 한다(할당이 생긴다). `Update`에서 부르지 않는다. Core에는 변경 이벤트나 캐시가 없다(설계 §7).

다시 읽을 시점(예):

- 화면을 열 때, 다시 활성화될 때
- 상점 구매 결과를 소비한 뒤(새 서브 카드)
- 회복 결과를 소비한 뒤(메인 체력)
- 전투가 끝나 스테이지 선택으로 돌아온 뒤(영혼·메인 체력 정산)
- 새 Run을 시작한 뒤(로비 출발 등)

기존 전역 UI(`MainCharactorUI`·`UnitCardUI`·`DeckButtonUI`)는 Buffer 조회값을 매 프레임 참조 비교한다. 이 UI들을 조회로 바꿀 때 `ReadOwned()`를 비교 루프에 넣지 않는다. 변화 감지가 필요하면 가벼운 원본 신호가 바뀔 때만 다시 읽는 방식을 View가 정한다. 원본 신호 예: `Owned` 항목 참조·개수, `TryGetMainState` 참조, 카드별 `GetSoulCount`.

다시 읽었는데 선택했던 `OwnedId`가 없으면 선택을 해제한다. 화면이 닫히거나 Run이 바뀌면 표시 캐시를 비운다. 이전 Run의 스냅샷을 새 Run의 카드처럼 쓰지 않는다.

## 5. 로비·Run 수명에서 주의할 점

- **첫 로비 진입:** `EmptyRoster`다. 로비 계획 LV1-5("화면 표시만을 위해 NewRun을 실행하지 않는다")와 같은 정책이다.
- **로비 출발(LV8A):** `StartNewRun(1)`을 부르므로 출발 뒤에는 새 Run의 수치가 된다. 로비에서 보던 값(이전 Run)과 다를 수 있다.
- **Run 종료 뒤 로비 복귀(LV10, 미착수):** 로비에서 보이는 값은 새 Run을 언제 만드느냐(LV10의 "정할 것 1")에 따라 달라진다.
  - 지금처럼 전투 이탈에서 새 Run을 만들면: 로비에서 `Ready` + `RunOutcome.InProgress`(새 Run)
  - 로비 출발에서만 만들면: 로비에서 `Ready` + `RunOutcome.Defeat`/`Victory`(끝난 Run)
  - 두 경우 모두 이 조회로 처리할 수 있다. 화면 문구는 LV10 결정 때 정한다.
- **스테이지 선택:** 전투가 끝나 `EndBattle`이 지난 뒤에는 `Ready`다. 전투 씬 안(`HasBattle`)에서는 `BattleActive`다.

## 6. 다른 화면 값과의 관계(테스트로 고정됨)

- **전투 생성 값:** 같은 유닛의 전투 시작 값과 비교하면, 위치 보정이 없는 칸에서 체력·공격력(`EffectiveAttackPower`)·이동·행동 자원·비용·병과·특성이 같다(`OwnedCharacterStatsCalculatorTests`, `CharacterStatsQueryServiceTests`). 지형·위치 보정이 붙은 칸에서 전투 값이 다른 것은 정상이다.
- **회복 화면 값:** 메인의 현재·최대 체력은 회복 화면(`CoreGroup.RecoveryState`)과 같은 원본·같은 공식이다(`CharacterStatsRuntimeCompositionTests`).

## 7. View에서 하지 않을 것

- InGame DB를 직접 읽어 능력치를 계산하기
- 영혼·유물 보너스를 다시 더하기, 레벨로 수치를 올리는 공식을 만들기
- `CharacterKey`로 카드를 구분하기
- 전투 중 이전 조회 스냅샷을 현재 값처럼 표시하기
- 예외를 잡아 빈 목록으로 바꾸기

## 8. View 연결 뒤 Unity 확인 목록

설계 §11.2를 화면 기준으로 옮긴 것이다. Core 테스트 통과가 이 항목들의 완료를 뜻하지 않는다.

1. 로비 첫 진입: 빈 상태가 보이고 예외가 없다.
2. 끝난 Run의 보유 정보가 남은 로비(LV10 결정에 따라): 메인 HP 0을 포함해 표시되고 Run 종료가 구분된다.
3. 스테이지 선택: 전투 뒤 영혼 획득·메인 체력 변화가 반영된다. 회복 뒤 메인 체력이 회복 화면과 같다.
4. 상점 구매 뒤 새 서브 카드가 목록에 나온다.
5. 새 Run 뒤 다시 읽으면 이전 카드 선택이 해제된다.
6. 전투 중에는 이 조회로 표시하지 않는다(기존 전투 표시 유지).
7. 씬 전환(로비 ↔ 스테이지 선택 ↔ 전투) 뒤에도 같은 서비스로 읽힌다(런타임은 Boot에서 한 번 조립).
8. `ReadOwned()`가 매 프레임 호출되지 않는다(로그나 Profiler로 확인).

## 9. 참조

| 구분 | 경로 |
|---|---|
| 읽기 계약 | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/ICharacterStatsReadSink.cs` |
| 결과 형식 | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/` 아래 4개 |
| 조회 서비스(Core) | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterStatsQueryService.cs` |
| 그룹 공개 | `IInGameCoreSystemGroupService.CharacterStats` (`.../CoreSystem/InGameCoreSystemGroup.cs`) |
| 테스트 | `Assets/Tests/EditMode/OwnedCharacterStatsCalculatorTests.cs`, `Assets/Tests/EditMode/Framework/CharacterStatsQueryServiceTests.cs`, `Assets/Tests/EditMode/Framework/CharacterStatsRuntimeCompositionTests.cs` |
| 관련 계획 | [로비 계획 LV1-5·LV8A·LV10](../plans/2026-10-11-lobby-plan-Hojin.md), [비전투 View 구조 개선 설계](../specs/2026-10-13-view-structure-design-Hojin.md) |
