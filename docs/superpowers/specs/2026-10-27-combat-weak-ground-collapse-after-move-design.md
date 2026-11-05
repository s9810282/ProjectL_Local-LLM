# 전투 약한 지반 — 이동 명령 끝 붕괴 설계 초안

- 작성일: 2026-10-27. 코드·에셋 변경 없음(설계만).
- 사용자 요청: "약한 지반을 키고 싶은데, 붕괴 시점을 유닛의 이동 후로 변경하려면 좀 번거롭나?" → 붕괴 시점 두 안(이동 명령 끝만 / 모든 행동 끝) 중 **이동 명령 끝만**을 골랐다(10-27 사용자 "A(이동 명령 끝만)로 하자. 설계 초안부터 써줘").
- **결정(10-27 사용자 "모두 권장안대로 하되, Q-WG2의 경우는 넉백으로 밟은 것도 포함해야"):** Q-WG1·Q-WG3~Q-WG7은 A, **Q-WG2는 B(넉백·끌어오기로 밟은 것도 셈)**. 2판에서 이에 맞춰 WG1·WG2·§4·§7을 고쳤다. 넉백을 세면 그 진입이 일어난 공격 명령 끝에도 무너져야 예정 붕괴가 남지 않는다. 그래서 Q-WG8(넉백으로 찬 칸이 무너지는 시점)이 새로 생겼다. **Q-WG8 A로 결정(10-27 사용자 "Q-WG8은 A(그 공격 명령 끝)로 하고 Task 계획 써줘").** Task 계획은 [weak-ground-collapse-plan](../plans/2026-10-27-combat-weak-ground-collapse-plan.md)(WG-a~WG-c, TP-1~TP-10)이다. 계획에서 이 설계와 다르게 읽은 것은 TP-3 하나다(WG6의 "거절" = 사건 시점에는 하지 않고 결과 끝에 맡김).
- 관련 원본:
  - 천명님 옛 구현 `2d0509df`(10-11, 낭떠러지·약한 지반 추가)와 보류 `d4a6d6fd`(10-24, 새 시스템).
  - 천명님 [플레이어 턴 공유 행동 비용 설계](2026-10-23-player-turn-shared-actioncost-design.md) 기능 표의 "Weak ground" 줄(보류 이유와 기획 결정 "주인 턴 끝 vs 플레이어 턴 끝").
  - 낭떠러지 임시 연출(10-27, CLAUDE.md 현재 상태): 추락 사망 = 가라앉기·작아지기, 바닥 = 검은 사각형 메시.
  - [고지대 View 설계](2026-10-20-combat-high-ground-view-design.md)(바닥 지형 외형, 그릴지 = 프리팹 유무).
- 영역: Core 계산기는 천명님 영역이다. Core를 고치려면 사용자의 명시 지시가 있어야 하고, 고친 것은 Core 전달 문서 §5에 남긴다(Q-WG1). View 쪽은 우리 영역이다.
- 경로 약칭: `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/`, `C/` = `…/ViewSystem/Combat/`.

## 0. 요약

- **Core (2판, Q-WG2 B·Q-WG8 A 기준):** 옛 함수 본문 둘을 되살리고, 모든 명령의 공통 끝(`MovementProjection.Complete`)에서 빈 함수를 부르던 한 줄을 붕괴 한 줄로 바꾼다. 강제 진입 세기 호출은 그대로 둔다. Buffer 검사·상태 형식·사건 형식은 그대로다.
- **View:** 추락 연출은 오늘 만든 것이 그대로 받는다. 새로 할 것은 약한 지반 외형, 설명 문구, 무너지는 순간 칸을 낭떠러지로 바꾸는 입구다.
- **무거운 쪽:** 천명님 시험의 기대값 변경(허락 필요)과 확인용 배치다. 약한 지반을 쓰는 스테이지가 지금 없다.

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 약한 지반 계산 셋(`RegisterEntry`·`CollapsePending`·`ResolveInterruptedActionEnd`)은 본문이 비어 있다(인자 검사만). 부르는 곳은 이동 경로 매 칸(`CombatMovementCalculator.cs:105`), 강제 진입(`MovementProjection.cs:385`, 캐릭터만), 모든 명령 끝(`MovementProjection.Complete` `:426`)이다. 옛 턴 끝 호출(`CombatTurnCalculator`, 토네이도 뒤)은 지워졌다 | `Core/WeakGroundCalculator.cs:12·18·24`, `git show 2d0509df` |
| F2 | 보류 이유: 옛 규칙은 "유닛 턴 끝에 붕괴"였는데, 플레이어 턴 공유 행동 비용으로 바뀌며 유닛 턴 끝이 사라졌다. 천명님은 "기능째 보류, 풀리지 않는 예정 붕괴를 쌓지 않음"으로 두고 시점을 기획 결정으로 남겼다 | 플레이어 턴 설계 기능 표 |
| F3 | 옛 본문. `RegisterEntry`: 살아 있는 필드 캐릭터가 그 칸에 있어야 하고(아니면 예외), 분류된 활성 약한 지반이면 진입 +1. `CollapsePending`: 분류된 예정 붕괴 원본을 ID 순서로 모두 낭떠러지로 바꾼 뒤, 그 칸의 살아 있는 필드 캐릭터·장애물을 ID 순서로 한 번씩 떨어뜨리고(사망 정산은 미룸) 사망 경계를 한 번 처리한다. 쓰는 멤버(`Character`·`TerrainQueryCells`·`Environment`·`Bindings`·`CurrentInput`·`ResolveDeathBoundary`)가 지금 `MovementProjection`에 모두 있어 거의 그대로 복사된다 | `git show 2d0509df:…/WeakGroundCalculator.cs` |
| F4 | 상태 규칙은 살아 있다. `StaticTerrainFact`의 `RegisterEntry`·`Collapse`·`PendingCollapse`(진입 수 ≥ 한도)·`RemainingEntries`(한도 − 진입 수, 0 미만 없음). 명령 안 환경 `CommandEnvironmentState.ChangeTerrain`은 "한 번 진입" 또는 "예정된 붕괴"만 받고, 바꿀 때마다 `CombatMovementSourceEvent`를 남긴다 | `StaticTerrainFact.cs:24·26·52·60`, `CommandEnvironmentState.cs:31` |
| F5 | Buffer `ValidateGroundChange`가 한 명령 안의 "마지막 진입 → 붕괴"를 받는다(중간 예정 상태는 사건에 남음). 전투 중 약한 지반·낭떠러지 원본의 추가·삭제는 거절한다 | `CombatStateBuffer.cs:411` |
| F6 | 이동 계산 `ProjectRoute` 순서: 경로 칸마다 `Enter` → 진입 세기(`:105`) → 진입 규칙 → (반복) → 경로 도착(`:111`, 영혼 수집·위치 보정) → 이동 성공 효과(`:112`, 이동자가 살아 있고 승패가 안 났을 때만). 아군·적 이동이 모두 이 길이다(적 AI `EnemyAiTurnService.cs:42`). 이동 미리보기(`:22`)와 이동 맥락(`MovementContextCalculator`)도 같은 함수를 `preview: true`(플레이어에게 보이는 환경)로 쓴다 | `Core/CombatMovementCalculator.cs` |
| F7 | 추락 사건 순서: `CombatFallEvent` → 체력 0 사건이 곧 `Died`(`CombatCommandEvents.Health`) → 사망 경계. 넉백 낭떠러지 추락과 같은 길이다 | `MovementProjection.ApplyFall` `:198` |
| F8 | 약한 지반 정의 `Terrain_WeakGround`(한도 3)를 배치한 스테이지가 없다. 낭떠러지는 `Test` 스테이지 한 칸뿐이다 | 에셋 GUID 검색 |
| F9 | 장애물은 약한 지반 위에 작성할 수 없고(준비 거절), 밀린 장애물은 세지 않는다. 이동 경로는 다른 유닛 칸에 들어가지 않는다. 그래서 이동 명령 끝에 무너지는 칸 위에 있을 수 있는 대상은 이동자뿐이다 | `BattleEnvironmentPreparationTests:65`, `ObstacleReactionTests:331` |
| F10 | 적 경로 찾기는 지뢰 칸과 들어갈 수 없는 칸(낭떠러지)만 피한다. 약한 지반은 모른다 | `EnemyMovementPathfinder.cs:45` |
| F11 | View 외형 `TerrainVisual_WeakGround`: 바닥 형상 모드, 프리팹 비어 있음 → 그리지 않는다. 설명은 "캐릭터가 총 {n}회 밟으면, 해당 턴 종료 시 붕괴하여 낭떠러지로 변합니다. 앞으로 {남은횟수}회 더 밟을 수 있습니다."이다. 치환은 `CombatEnvironmentText.cs:73` | 에셋 |
| F12 | View 지형은 결과 끝 갱신(`FieldTerrainPresenter.Render`)에서만 다시 그린다. 사건 시점 갱신 선례는 토네이도다: 사실 처리기 `case CombatTornadoEvent` → 필드 창구 `HideTornado`·`ShowTornado` → `FieldTerrainPresenter.Hide`·`ShowAt`(같은 원본·같은 인스턴스, 위치만). 종류가 바뀌는 갱신 입구는 없다(종류가 바뀌면 `TerrainViewSet.Render`가 옛 View를 반환하고 새로 빌림). 사실 처리기에는 `CombatMovementSourceEvent` 갈래가 없다 | `C/Playback/Handlers/CombatFactEventHandler.cs:62`, `C/Display/Map/FieldTerrainPresenter.cs:245·256`, `TerrainViewSet.cs:46` |
| F13 | 사건 순서 표에서 `CombatMovementSourceEvent`는 사실 칸이고 주체가 없어 걸음 레인이 막지 않는다(겹침 규칙 3). 이동자 자신의 사망은 걸음 끝(방향 복귀 포함)까지 기다린다(규칙 1). 약한 지반 원본은 걸음 접촉 원본이 아니다(`TriggersArrivalRule = false`) | `CombatEventTypeTable.cs:139`, `CombatStepContactGate.cs` 주석, `CombatStepContacts.cs:159` |
| F14 | 추락 연출: 사실 처리기가 `CombatFallEvent`에 표지를 남기고, 사망 처리기가 그 표지를 보면 가라앉기·작아지기 조립을 쓴다(10-27). 붕괴 추락도 같은 사건 순서(F7)라 그대로 받는다 | `CombatCharacterEventHandler.BeginFallDeath` |
| F15 | 필드 창구 `CombatFieldEventPorts`는 칸이 모두 필수다. 만드는 곳은 운영 `CombatSceneDisplayFactory.cs:139`(우리 파일)와 천명님 `PullAndMarkPresentationTests.cs:422` 두 곳이다 | grep |
| F16 | **우리 시험 `TerrainVisualDatabaseTests.cs:228-234`가 "약한 지반·낭떠러지 외형은 프리팹 없음"을 확인한다. 오늘 스테이지한 낭떠러지 프리팹 연결로 이미 실패할 것이다**(실행은 안 함, 코드 대조) | `ActualCatalog_ProvidesPlaceholderCubesTornadoParticlesAndGroundShapeVisuals` |

## 2. 설계

### WG1. 규칙 (이 절이 정책 원본)

1. **세는 진입:** 캐릭터가 실제로 들어간 칸마다 +1이다. 이동 경로는 지나가기만 해도 센다(옛 규칙, Q-WG3). **넉백·끌어오기(강제 진입)도 센다(Q-WG2 B, 옛 규칙).** 여러 칸 밀리면 지나간 칸마다 센다. 장애물(구조물)이 밀린 것·배치·손패 복귀는 세지 않는다. 아군·적 모두 같다.
2. **붕괴:** 진입이 일어난 명령이 끝나는 순간, 한도에 닿은 약한 지반이 모두 낭떠러지로 바뀐다(Q-WG8 A). 이동 명령이면 이동이 끝날 때, 공격 명령이면 넉백·끌어오기까지 끝난 뒤다. 그 칸에 서 있던 대상은 떨어져 즉사한다.
   - 이동 명령에서 떨어질 수 있는 것은 이동을 마친 이동자뿐이다(F9).
   - 공격 명령에서는 그 칸으로 밀리거나 끌려와 멈춘 대상이다. 같은 공격에서 장애물이 밀려 들어와 있으면 장애물도 떨어진다(옛 `CollapsePending`이 장애물도 다룸).
3. **지나간 칸:** 비어 있으므로 낭떠러지로만 바뀐다(이동자·밀린 대상 뒤에서 무너짐).
4. **승패가 난 뒤:** 명령 중 승패가 이미 났으면(지뢰로 메인 사망, 공격으로 마지막 적 처치 등) 무너뜨리지 않는다(옛 규칙의 `Terminal` 건너뜀). 행동자만 죽고 승패가 안 났으면 무너뜨린다.
5. **턴 경계:** 턴 끝·밤 끝·적 국면 끝에는 따로 일이 없다. 진입이 일어난 명령이 그 끝에서 무너뜨리므로 명령 끝에 예정 붕괴가 남지 않는다(불변식). 천명님 보류 조건 "풀리지 않는 예정 붕괴를 쌓지 않음"을 지킨다.
6. **숨은 약한 지반:** 실행은 세고 무너뜨린다. 미리보기는 보이는 환경만 본다(옛 규칙). 숨은 약한 지반이 무너지면 낭떠러지도 숨어 그려지지 않으므로, 데이터는 약한 지반을 양쪽 공개로 두기를 권한다(작성 메모, 코드로 막지 않음).

### WG2. Core에서 바꾸는 곳

2판(Q-WG2 B·Q-WG8 A 기준). 1판의 "이동 계산 끝 한 줄 + 강제 진입 세기 걷기"는 쓰지 않는다.

| 곳 | 바꿈 |
|---|---|
| `WeakGroundCalculator.RegisterEntry`·`CollapsePending` | `2d0509df` 본문을 그대로 되살린다 |
| `MovementProjection.Complete` `:426` | `WeakGroundCalculator.ResolveInterruptedActionEnd(this);`를 `if (!Terminal) WeakGroundCalculator.CollapsePending(this);`로 바꾼다. `Complete`는 이동·공격·반응 공격·배치·턴 명령이 모두 지나는 공통 끝이라, 붕괴 시점이 이 한 곳이다 |
| `ResolveInterruptedActionEnd` | 지운다(위 줄이 대신함). Q-WG7이 B였다면 빈 함수로 남겼겠지만 A로 정해졌다 |
| `MovementProjection.ResolveForcedTerrainEntry` `:385` | 그대로 둔다(강제 진입도 셈, 규칙 1). 부르는 곳은 넉백(`AttackKnockbackCalculator.cs:169`)·끌어오기(`AttackForcedMovement.cs:57`)·이동 중 환경 밀기(`MovementEnvironmentCalculator.cs:191`) 셋이고, 모두 같은 명령의 `Complete`로 끝난다 |
| `WeakGroundCalculator` 클래스 주석 | 규칙 한 줄(진입한 명령 끝에 붕괴, 강제 진입도 셈) |

- **P1 붕괴 자리(2판에서 바뀜):** 명령 공통 끝 한 곳이다. 그래서 이동 명령에서는 이동 성공 효과 **뒤**에 무너진다. 떨어질 이동자의 특성 발동 사건이 먼저 나가고 곧바로 떨어질 수 있다(떨어진 뒤 효과는 의미가 없어 결과는 같고, 화면에 특성 발동이 잠깐 보일 수 있음). 이동 쪽만 효과 앞에 두려면 호출이 두 곳이 되어 1판처럼 나누지 않는다.
- **P2 미리보기:** 같은 계산을 쓴다(따로 끄지 않음).
  - 이동 미리보기: 결과에 추락이 들어가지만 View는 이동 미리보기 사건을 그리지 않는다.
  - **공격 미리보기:** 붕괴를 예상하지 못한다. 공격 미리보기 값(`AttackPreview`)은 `CombatAttackCalculator.cs:150`에서 `Complete`(`:159`)보다 먼저 만들어진다. 그래서 넉백으로 무너져 떨어질 대상도 체력 예상·처치 예상에는 살아남는 것으로 나온다. 공격 결과 `Hits`의 맞은 뒤 체력도 같다(붕괴는 공격 결과 뒤 별도 사건). Buffer는 맞은 뒤 체력을 최종 체력과 대조하지 않아 거절되지는 않는다(검색 확인).
  - 경고 표시와 공격 미리보기 보완은 범위 밖이다(§6).
- **P3 승패 건너뜀 자리:** 부르는 쪽 한 줄(`!Terminal`)에 둔다. 옛 함수의 "배치 중간 승패로 멈추지 않음"은 그대로다.
- **P4 추락 처치 귀속:** 붕괴 추락은 `ApplyFall`이 피해 원천 없이 체력을 0으로 만든다(F7). 지금 넉백으로 낭떠러지에 바로 떨어뜨릴 때와 같은 처리이고, 이번에 바꾸지 않는다.

### WG3. 적 AI (Q-WG4)

적 경로 찾기는 약한 지반을 모른다(F10). 그대로 두면 적이 한도에 닿는 칸에서 멈춰 스스로 떨어질 수 있다. 권장은 이번에는 그대로 두고, 플레이 확인 뒤 기획이 정하게 하는 것이다(Core AI는 천명님 영역이고, 유도해 떨어뜨리는 플레이가 의도일 수도 있음).

### WG4. View — 약한 지반 외형 (Q-WG5)

- 임시 프리팹 `Prefabs/Default/Environment/Terrain_WeakGround.prefab`: 낭떠러지와 같은 꼴이다. 루트(회전 없음) 아래 자식 `Surface` = 기본 Quad, X 90, 높이 0.01, 배율 1.05, 레이어 Default, 그림자 끔.
- 재질 `Environment/WeakGround/WeakGroundSurface.mat`: URP Unlit **Transparent**, 갈색 반투명(예: `#6B4A2B`, 알파 0.45).
  - 반투명은 깊이를 쓰지 않아서, 격자 데칼이 아래 바닥에 찍힌 뒤 그 위를 물들인다. 그래서 격자가 비쳐 보인다(Unity 확인 대상).
  - 낭떠러지는 불투명 검정이라 격자를 덮는다. 갈색 → 검정 바뀜이 "무너짐"을 알린다.
- `TerrainVisual_WeakGround.prefab`에 연결한다. 바닥 높이는 0 그대로다(표면 높이 표·이웃 모양과 무관).
- 남은 횟수는 호버 설명이 보여 준다(이미 있음). 칸 위 숫자는 범위 밖이다.

### WG5. View — 설명 문구

`TerrainVisual_WeakGround.description`을 바꾼다(에셋 값만).

> 캐릭터가 총 {n}회 밟으면, 그 행동이 끝날 때 `<color=#FF6262>`붕괴하여 낭떠러지로 변합니다.`</color>` 그 위에 서 있던 캐릭터는 `<color=#FF6262>`즉사`</color>`합니다. 밀려나 밟아도 셉니다.
>
> {남은횟수}회 더 밟으면 무너집니다.

- "앞으로 n회 더 밟을 수 있습니다"는 마지막 한 번을 밟으면 무너진다는 뜻이 흐려서 바꾼다.
- 예정 상태가 명령 끝에 남지 않으므로(규칙 5) 호버에는 1 이상만 보인다.
- 넉백·끌어오기도 세므로(Q-WG2 B) "이동하며"가 아니라 "밟으면", 시점은 "그 행동이 끝날 때"로 쓴다(1판 문구는 "이동하며 … 그 이동이 끝날 때"였다).

### WG6. View — 무너지는 순간 칸 바꾸기 (Q-WG6)

| 부품 | 하는 일 |
|---|---|
| 사실 처리기 `CombatFactEventHandler` | 갈래 하나: `CombatMovementSourceEvent`에서 `Final`이 `StaticTerrainFact`이고 종류가 바뀌었으면 필드 창구로 넘긴다. 진입 수만 바뀐 사건은 외형이 같아 무시한다 |
| 필드 창구 `CombatFieldEventPorts` | 칸 하나 `ShowTerrainChange(MovementTileFact, FieldGridMapper)`(필수, 토네이도 칸 옆). Presenter가 사실 처리기에 넘긴다(토네이도와 같은 꼴) |
| 운영 조립 `CombatSceneDisplayFactory` | 지형 표시의 새 입구를 창구에 넘긴다(한 줄, 민형님 Factory 공유) |
| 지형 표시 `FieldTerrainPresenter.ShowChanged` | 원본 하나를 새 사실로 다시 보이게 한다. 보일지·외형 선택·대여는 `TerrainViewSet`의 새 입구 하나가 맡는다 |
| `TerrainViewSet` 원본 하나 입구 | `Render`의 "원본 하나 처리"(보일지 판단 → 외형 선택 → 열쇠가 바뀌면 반환 후 대여 → 적용)를 함수로 빼서 `Render`와 이 입구가 함께 쓴다(판단이 두 벌이 되지 않게). 분류는 마지막 `Render`의 것을 쓴다(전투 중 분류는 바뀌지 않음, F5). 이웃 모양을 쓰는 종류나 표면 높이가 다른 종류로의 바뀜은 거절한다(이웃·높이 표까지 바뀌어야 해서). 약한 지반·낭떠러지는 둘 다 해당 없음 |

- **시점:** 사건 순서는 붕괴 사건 → 추락 사건 → 사망이다(F7). 사실 칸은 걸음이 막지 않으므로(F13) 이동자의 마지막 걸음 보간이 끝나면 칸이 검게 바뀐다. 이동자의 사망은 걸음 끝까지 기다렸다가 가라앉는다. 지나간 칸들도 같은 순간에 함께 바뀐다.
- **공격 명령(2판):** 붕괴 사건은 공격 결과의 넉백·끌어오기 걸음 뒤에 온다. 밀린 대상의 사망은 그 대상의 걸음 끝까지 기다린다(규칙 1). View 쪽 추가 일은 없다(같은 사실 처리기 갈래·같은 추락 연출).
- **정리:** 결과 끝 `Render`가 최종 사실로 다시 맞춘다(같은 원본·같은 열쇠면 그대로 둠). 취소·해제는 기존 `Render`·`Clear` 경로가 맡는다.

### WG7. 확인용 배치

약한 지반을 배치한 스테이지가 없다(F8). 고지대 때처럼 사용자가 에디터에서 한 스테이지에 임시로 몇 칸 놓고, 확인 뒤 되돌린다. 빨리 보려면 정의의 한도 3을 임시로 1로 낮춘다.

## 3. 바뀌는 파일 (예상)

| 쪽 | 파일 |
|---|---|
| Core | `WeakGroundCalculator.cs`, `MovementProjection.cs`(`Complete` 한 줄). 2판에서 `CombatMovementCalculator.cs`·`ResolveForcedTerrainEntry`는 바꾸지 않는다 |
| View 코드 | `CombatFactEventHandler.cs`, `CombatFieldEventPorts.cs`, `CombatSceneEventPresenter.cs`(넘기기 한 줄), `FieldTerrainPresenter.cs`, `TerrainViewSet.cs`, `CombatSceneDisplayFactory.cs`(한 줄) |
| 에셋 | 새 `Terrain_WeakGround.prefab`·`WeakGround/WeakGroundSurface.mat`(+ `.meta`, 폴더 `.meta`), `TerrainVisual_WeakGround.asset`(프리팹·설명) |
| 시험 | §4 |
| 문서 | Core 전달 문서 §5.55, 민형님 Factory 공유 한 줄, 폴더 배치 안내(새 파일 없음 — 해당 없을 수 있음) |

새 `.cs` 파일은 없다.

## 4. 시험 영향 (코드 대조, 실행 안 함)

| 시험 | 작성 | 지금 | 바뀐 뒤 | 처리(Q-WG7 A 기준) |
|---|---|---|---|---|
| `WeakGroundMovementTests.MovementDoesNotCountOrCollapseWeakGround` | 천명님 | 통과 | 실패(세고 무너짐) | 새 규칙(세고, 끝에 무너지고, 이동자는 안 떨어짐)으로 기대값 변경 |
| `WeakGroundMovementTests.HiddenGround_RemainsHiddenAndUnchangedDuringMovement` | 천명님 | 통과 | 실패(실행이 숨은 지반을 셈) | 미리보기 단언은 그대로, 실행 단언만 새 규칙 |
| `WeakGroundTurnTests.EndBoundaries_DoNotCollapsePendingGround` | 천명님 | 통과 | `false` 실패(직접 부른 붕괴가 무너뜨림), `true` 컴파일 실패(함수 삭제) | 사례 삭제(턴 끝 무붕괴는 `ControllerCompletion_…`이 이미 확인) |
| `WeakGroundTurnTests.EntryBoundary_DoesNotArmACollapse` | 천명님 | 통과 | 실패(직접 부르면 무너짐) | 계산기 단위 사례로 기대값 변경 |
| `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround` | 천명님 | 통과 | 통과 | 그대로 |
| `WeakGroundPublicationTests.PreviewAndEntry_PublishExactCountPendingAndOneResult_…` | 천명님 | **이미 실패로 보임**(진입 수 1 → 2 기대, 10-24 보류 뒤 안 셈) | 실패(이동자가 그 칸에서 무너져 떨어짐) | 새 규칙으로 다시 씀 |
| `WeakGroundPublicationTests.Collapse_PublishesEverySourceAndFallWithFinalOccupantsInOneAcceptedResult` | 천명님 | **이미 실패로 보임**(턴 끝 붕괴 기대) | 실패 | 이동 끝 붕괴로 다시 씀 |
| `WeakGroundMovementTests.ForcedAttackRelocations_DoNotChangeWeakGroundOrAttackerSettlement` (2판 추가) | 천명님 | 통과 | 실패(넉백 진입을 셈) | 새 규칙(밀린 칸이 세어짐, 공격자 정산은 그대로)으로 기대값 변경 |
| `CliffFallTests.AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees(WeakGround)` (2판 추가) | 천명님 | 통과 | 실패(넉백 진입 수 0·원본 사건 없음 기대, `:179`·`:182`) | 약한 지반 사례만 진입 수 1·원본 사건 있음으로(한도 2 미만이라 붕괴 없음), 낭떠러지 사례는 그대로 |
| `RejectedRoute_…`, `PlayerPlacement_…`, `ObstacleReactionTests.Knockback_SurvivingObstacleMovesWithoutGroundCount`(장애물은 안 셈), `CliffFallTests.EnvironmentForcedEntry_…`(낭떠러지 사례만), `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_…`, `DeferredCombatFeatureTests.EnvironmentClocksDoNotActivateOrArmCounters` | 천명님 | 통과 | 통과(규칙 1·5) | 그대로 |
| `TerrainVisualDatabaseTests.ActualCatalog_…` `:228-234` | 우리 | **오늘 낭떠러지로 이미 실패** | 약한 지반도 | 두 종류 모두 "프리팹 있음, 높이 0"으로 기대값 변경 |
| `PullAndMarkPresentationTests.cs:422` | 천명님 | 통과 | 컴파일 실패(창구 칸 하나) | 생성 인자 하나 컴파일 맞춤 |
| `CombatEnvironmentHoverTests.WeakGroundDescription_…`, `TerrainVisualDatabaseTests.ActualCatalog_GroundShapeHoverDescriptionsDoNotThrow` | 천명님·우리 | 통과 | 통과(문구 토큰 그대로) | 그대로 |

- 새 `[Test]`는 만들지 않는다(전투 View 상시 지시). 천명님 시험은 사례를 새 규칙으로 바꾸는 것이라 "호출만 맞춤"을 넘는다. 그래서 Q-WG7로 묻는다.
- "이미 실패로 보임"은 코드 대조 판단이다. Unity Test Runner 결과로 확인해야 한다.

## 5. 공유

- **천명님 — Core 전달 문서 §5.55:** 규칙(WG1), 바꾼 줄(WG2), 고친 시험(§4), 기획 결정 질문("주인 턴 끝 vs 플레이어 턴 끝")에 대한 이번 답(밟은 행동이 끝날 때, Q-WG8 A 기준), 적 AI 미반영(WG3), 공격 미리보기가 붕괴 추락을 예상하지 못함(P2). 미전달.
- **민형님 — Factory 공유 한 줄:** `CombatSceneDisplayFactory` 창구 칸. 미전달.

## 6. 범위 밖

- 이동 미리보기 경고("여기서 멈추면 무너짐"). Core 미리보기에는 추락이 들어가지만 View가 그리지 않는다.
- 칸 위 남은 횟수 숫자, 붕괴 이펙트·소리.
- 적 AI 회피(Q-WG4 A로 이번엔 안 함).
- 공격 미리보기의 붕괴 예상(P2). 넉백으로 무너져 떨어질 대상이 체력·처치 예상에 살아남는 것으로 나온다. 고치려면 Core 공격 계산에서 붕괴를 공격 결과 만들기 앞으로 옮겨야 해서 범위가 커진다.

## 7. 정할 것

### Q-WG1. Core 변경은 누가 하나

| 안 | 내용 |
|---|---|
| **A (권장)** | 사용자 지시로 이 세션이 직접 고치고 Core 전달 문서 §5.55에 남긴다 |
| B | Core 전달 문서에 요청만 적고 천명님이 구현한다 |

이유: 바꿀 곳이 옛 본문 복원 + 세 줄로 작고, View 확인과 한 묶음으로 봐야 시점이 맞는다. 천명님이 기획 결정으로 남긴 항목이라 공유는 꼭 한다.

### Q-WG2. 넉백·끌어오기로 밟은 것도 세나

| 안 | 내용 |
|---|---|
| **A (권장)** | 세지 않는다. 이동 명령 안의 진입만 센다. 명령 끝에 예정 붕괴가 남지 않는다 |
| B | 센다. 한도에 닿으면 다음 이동 명령 끝까지 예정으로 남는다 |

이유: B면 예정 붕괴가 남아, 어느 유닛이든 다음에 이동을 마치는 순간 지도 반대편의 칸이 무너지고 그 위의 대상이 떨어진다. 원인과 결과가 멀어지고, 밤 재배치·턴 끝에 남은 예정 상태를 어떻게 할지 규칙이 더 필요하다. 천명님 보류 조건과도 어긋난다.

**결정(10-27 사용자): 센다.** 위 이유의 문제는 B의 뒷부분("다음 이동 명령 끝까지 남김") 때문이다. 세는 것 자체는 옛 규칙이라 Core가 더 쉽다(호출을 걷지 않음). 남는 것은 무너지는 시점이라 Q-WG8로 따로 묻는다.

### Q-WG8. 넉백·끌어오기로 한도에 닿은 칸은 언제 무너지나 (2판 새 질문, 10-27 A로 결정)

| 안 | 내용 |
|---|---|
| **A (권장)** | 그 공격 명령 끝에 무너진다. 붕괴 호출을 모든 명령의 공통 끝(`MovementProjection.Complete`) 한 곳에 둔다. 이동 명령 끝 규칙도 같은 한 줄이 맡는다 |
| B | 다음 이동 명령 끝까지 예정으로 남긴다(Q-WG2 표의 B 그대로) |

이유:
- A면 "밟은 행동이 끝나면 무너진다" 하나로 이동·넉백이 같다. 명령 끝에 예정 붕괴가 남지 않아 밤·턴 경계 규칙이 필요 없고, Core 변경은 오히려 1판보다 줄 하나 적다.
- A의 비용: 이동 명령에서 무너짐이 이동 성공 효과 뒤로 간다(P1). 공격 미리보기가 붕괴 추락을 예상하지 못한다(P2). 천명님 시험 두 사례가 더 바뀐다(§4).
- B는 Q-WG2 표에 적은 문제(원인과 결과가 멀어짐, 경계 규칙 추가, 보류 조건과 어긋남)가 그대로 남는다.
- 사용자가 고른 시점 "이동 명령 끝만"은 넉백을 세지 않을 때의 답이었다. 넉백을 세면 A는 그 시점을 "밟은 행동(이동·공격) 끝"으로 넓히는 것이다.

### Q-WG3. 지나가기만 해도 세나

| 안 | 내용 |
|---|---|
| **A (권장)** | 센다(경로의 실제 진입 모두, 옛 규칙) |
| B | 멈춘 칸만 센다 |

이유: 옛 규칙·옛 시험·Buffer 검사가 모두 A 기준이라 바꿀 곳이 가장 적다. 이동자 뒤에서 무너지는 장면도 생긴다.

### Q-WG4. 적 AI가 무너질 칸을 피하나

| 안 | 내용 |
|---|---|
| **A (권장)** | 이번에는 그대로 둔다. 적이 스스로 떨어질 수 있다. 플레이 확인 뒤 기획이 정한다 |
| B | 적 경로 찾기가 한도에 닿는 칸에서 멈추지 않게 한다(Core AI 변경) |

### Q-WG5. 약한 지반 외형

| 안 | 내용 |
|---|---|
| **A (권장)** | 이 작업에서 임시 반투명 갈색 사각형(WG4)을 만든다 |
| B | 사용자가 에디터에서 만든다 |
| C | 그리지 않는다(호버 설명만) |

이유: C면 어느 칸이 약한 지반인지 호버해야만 알 수 있다. A는 낭떠러지와 같은 꼴이라 만드는 법이 정해져 있다.

### Q-WG6. 칸이 검게 바뀌는 시점

| 안 | 내용 |
|---|---|
| **A (권장)** | 무너지는 사건 순간(WG6) |
| B | 결과 끝(코드 없음). 이동자는 약한 지반 위에서 가라앉고, 다 끝난 뒤에 검게 바뀐다 |

### Q-WG7. 시험

| 안 | 내용 |
|---|---|
| **A (권장)** | 천명님 시험 중 보류를 확인하던 사례(§4의 여섯, 2판에서 여덟)를 새 규칙 사례로 바꾸는 것을 허락한다. 우리 `TerrainVisualDatabaseTests`는 두 종류 "프리팹 있음"으로 바꾼다 |
| B | 천명님 시험은 그대로 두고 실패 목록만 Core 전달 문서로 넘긴다(`ResolveInterruptedActionEnd`는 빈 함수로 남김) |

우리 `TerrainVisualDatabaseTests`의 낭떠러지 단언(F16)은 오늘 스테이지한 낭떠러지 작업 때문에 이미 깨진다. 확인하는 내용이 바뀌는 것이라 묻는다. 이 질문의 답과 함께 고치는 것을 권한다.

## 8. 7기준 예상 (알려진 문제 기준)

| 기준 | 예상 | 근거 |
|---|---|---|
| SSOT | 9 | 규칙 원본은 Core 계산기 하나, 붕괴 시점은 한 줄. View의 보일지·외형 선택은 `TerrainViewSet` 한 곳을 `Render`와 공유한다 |
| SOLID | 8.5 | 사실 처리기는 바뀜만 알리고 외형은 지형 표시가 안다. 창구 칸이 하나 는다 |
| FW 계약 | 9 | 창구·Factory·View DB 꼴 그대로 |
| 기능 변경의 독립성 | 8.5 | 끄기는 호출 한 줄. 천명님 시험 여덟이 같이 움직인다 |
| 캡슐화 | 8.5 | 원본 하나 입구가 이웃 모양·높이 바뀜을 거절해 표·높이 표 규칙을 지킨다 |
| C#/Unity 권장 구조 | 8.5 | 풀 재사용, 반투명 사각형. 격자 비침은 Unity 확인 대상 |
| 무의미한 If/Tick | 9 | 매 프레임 일 없음. 빈 `ResolveInterruptedActionEnd`를 걷는다 |

평균 약 **8.7**이다(2판도 같음, 붕괴 시점이 한 곳이 되어 SSOT는 더 단단해지고 공격 미리보기 차이가 하나 는다). 남는 낮음은 적 AI 미반영(WG3), 이동 미리보기 경고 없음, 공격 미리보기가 붕괴 추락을 예상하지 못함(P2)이다.

## 9. Phase (안)

| Phase | 내용 | 끝 |
|---|---|---|
| WG-a Core | WG2, 천명님 시험(Q-WG7), Core 전달 문서 §5.55 | 컴파일 미확인 보고 |
| WG-b View | WG4·WG5·WG6, 우리 시험 기대값, 민형님 공유 한 줄 | 컴파일 미확인 보고 |
| WG-c 확인 | 사용자 임시 배치(WG7), Unity 확인 §10, 묶음 끝 검토 하나 | 사용자 확인 |

Q-WG1은 A로 정해졌다(이 세션이 Core를 직접 고침). 착수는 Q-WG8 결정 뒤 Task 계획부터다.

## 10. Unity 확인 (안)

1. 컴파일, 기존 시험(§4 표 대로).
2. 약한 지반 칸이 갈색 반투명으로 보이고 격자가 비친다(낮·밤).
3. 호버 설명의 {n}·{남은횟수}가 맞고, 밟을 때마다 남은 횟수가 준다(지나가기 포함).
4. 마지막 한 번을 밟고 멈추면: 걸음이 끝나면 칸이 검게 바뀌고 → 이동자가 가라앉으며 작아지고 → 사망 처리.
5. 마지막 한 번을 지나가기만 하면: 이동이 끝날 때 그 칸만 검게 바뀌고 이동자는 무사하다.
6. 넉백·끌어오기로 약한 지반에 밀리면 남은 횟수가 준다(여러 칸 밀리면 지나간 칸마다). 마지막 한 번으로 밀려 멈춘 대상은 공격 연출이 끝난 뒤 칸이 검게 바뀌고 떨어진다(Q-WG8 A). 공격 호버의 체력 예상에는 이 추락이 나오지 않는다(P2).
7. 적 이동도 센다. 적이 마지막 칸에서 멈추면 떨어진다(Q-WG4 A).
8. 무너진 칸에는 다시 들어갈 수 없다(낭떠러지 규칙).
9. 결과 끝 갱신 뒤에도 같은 모습(튀지 않음).

## 11. 구현 메모 (2026-10-27, WG-a~WG-c, 컴파일 미확인)

실행 기록 원본은 [계획 §7](../plans/2026-10-27-combat-weak-ground-collapse-plan.md#7-실행-기록)이다. 이 설계와 달라진 것만 적는다.

- **붕괴 자리(WG2 2판):** 계획 TP-1대로 `MovementProjection.Complete` 맨 앞 한 줄이다. 플레이어 턴 끝·적 국면 끝은 `Complete`를 지나지 않아(`CombatTurnCalculator.Changed`) 턴 경계에는 붕괴가 없다.
- **WG6 "거절"의 뜻(계획 TP-3):** 사건 시점 갱신은 지금 보이는 원본만 바꾼다. 안 보이거나, 이웃 모양을 쓰거나, 표면 높이가 다르면 예외 없이 결과 끝 `Render`에 맡긴다(토네이도 `ShowAt`과 같은 규칙).
- **`TerrainViewSet` 공유(WG6):** 원본 하나 처리(`ShowOne`)와 분류 밖 보일지 조건(`IsShownFact`)을 `Render`·`TryReplace`가 함께 쓴다. `TryReplace`는 항목이 있는 원본만 받아 분류를 다시 보지 않는다(계획 TP-4).
- **알려진 제약:** `Complete(state: …)`로 상태를 미리 만들어 넘기는 호출(전투 시작)에서 붕괴하면 넘긴 상태가 낡는다. 전투 시작에는 진입이 없어 지금은 생기지 않는다(코드 주석에 남김).
