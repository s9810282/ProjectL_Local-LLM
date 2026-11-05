# 아군 배치 구역 칸 단위 + 적 배치 제한 삭제 — Task 계획

- 작성: 2026-10-27, Core 작업(천명)
- 설계: [player-deployment-zone-design](../specs/2026-10-27-combat-player-deployment-zone-design.md)(PZ1~PZ7, 결정 §9)
- 상태: **계획, 코드 착수 전.** §0의 P1~P6은 사용자 확인 대상이다.
- 범위: Core(Contracts·Domain·Application·CombatSystem), Buffer `CombatStateData`, DB `CombatStageData`·`CombatStageDetail`, Core·CoreIntegration 시험.
- **손대지 않는 것:**
  - Framework 시험(`Assets/Tests/EditMode/Framework/`) — 고치지도 돌리지도 않는다(사용자 지시). 옛 입구로 컴파일만 유지한다.
  - View(`ViewSystem/`), 개발 진입(`DevBattleInitializationSource`·`GameBootstrap*`), 편집 도구(`Tool/`), `FieldData`·`FieldDataSO`.
- 진행 규칙:
  - Phase마다 멈추고 보고한다.
  - MSBuild는 돌리지 않는다. 컴파일과 시험은 사용자가 Unity Test Runner로 Core·CoreIntegration 어셈블리를 돌려 확인한다.
  - 커밋·push는 사용자가 한다.

## 0. 계획에서 정한 것 (확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| P1 | 새 생성자는 계약마다 **하나씩**만 더한다. `StageConfig`는 모든 인자를 받는 형 하나이고, 옛 생성자 6개는 모두 이 새 생성자로 이어진다 | 입구가 늘어나지 않게 |
| P2 | `CombatPreparation` 새 생성자는 `drawCursor`와 `playerZone`을 **필수**로 받는다(`…, drawOrder, int drawCursor, PlayerDeploymentZone playerZone, CellCoord[] railCells = null, …`) | 옛 생성자는 줄 수가 선택 인자다. 새 쪽도 선택이면 인자를 생략한 기존 호출이 두 생성자 사이에서 모호해진다(CS0121) |
| P3 | `DeploymentZonePolicy`의 옛 생성자 `(int, int, int)`는 **지운다.** 시험 한 줄만 맞춘다 | 바깥(Framework·View)이 쓰지 않는다. 옛 입구를 남길 이유가 없다 |
| P4 | 옛 입구에 `[Obsolete]`를 붙이지 않는다. XML 주석에 "전환 입구 — PZ-d에서 지움"만 단다 | 바깥 수십 곳의 경고가 다른 경고를 가린다(설계 PZ6) |
| P5 | DB 저작 검사는 Core 규칙을 그대로 부른다: `PlayerDeploymentZone.Of(cells)`(중복 거절) + `FitsBoard(w, h)` | 규칙 원본 하나(SSOT). DB가 이미 Core Contracts 형식(`CellCoord`)을 쓰는 같은 어셈블리다 |
| P6 | **새 시험을 더한다**(§4). 단위 시험은 `ProjectL.Tests.Core`, 전달 경로 시험은 `ProjectL.Tests.CoreIntegration`에 둔다 | 새 값·판정·출현 범위와 계층 사이 전달을 지금 시험이 덮지 않는다. 원하지 않으면 §4를 빼고 기존 시험 맞춤만 한다 |

## 1. Phase PZ-a — Core 구역 값·판정·적 제한 삭제

끝나면: 아군 배치는 지금과 같다(어댑터가 필드 아군 줄 수로 줄 구역을 넘김). 적은 아군 구역 밖이면 어디든 놓이고 나온다. 바깥 코드는 그대로 컴파일된다.

| Task | 파일 | 할 것 |
|---|---|---|
| T1 | 새 `CoreCaculator/Contracts/Common/PlayerDeploymentZone.cs` | 설계 PZ1 그대로 만든다. `Rows(int depth)`(depth < 0이면 예외), `Of(IEnumerable<CellCoord>)`(null·중복 예외, 중복 좌표를 문구에 넣음, y → x 정렬 복사), `Empty`, `Contains`, `IsEmpty`, `CellsOutside(width, height)`(칸 구역의 판 밖 칸, 줄 구역은 빈 목록), `FitsBoard`(줄 구역은 `depth ≤ height`, 칸 구역은 `CellsOutside`가 빔 — 판 안 규칙은 이 한 곳), `RowDepth`, `ListedCells`. sealed 불변이고 한국어 XML 주석을 단다. `.meta`는 Unity가 만들고, 커밋 때 함께 넣는다 |
| T2 | `Domain/Placement/DeploymentZonePolicy.cs` | 생성자 `(PlayerDeploymentZone zone)`(null 예외). `CanDeploy`: Player = 모든 칸 `Contains`, Enemy = 어느 칸도 `Contains`가 아님, Neutral = false. 옛 생성자는 지운다(P3) |
| T3 | `Contracts/Commands/StageConfig.cs` | ① 새 생성자 `(w, h, reserve, PlayerDeploymentZone playerZone, seed, enemyPlacements, railCells, gimmicks, structurePlacements)`. 검사는 `playerZone` null 예외, `IsEmpty`면 예외, `!FitsBoard(w, h)`면 `ArgumentOutOfRangeException(nameof(playerZone))`. ② 옛 생성자 6개는 `PlayerDeploymentZone.Rows(playerDeploymentDepth)`로 ①에 이어지고, 적 줄 수는 받기만 한다. 옛 검사 `:156-165`는 지운다. ③ 속성: `PlayerZone`(새), `PlayerDeploymentDepth => PlayerZone.RowDepth ?? 0`, `EnemyDeploymentDepth => 0`(둘 다 전환 입구 주석) |
| T4 | `Contracts/Combat/BattlePreparation.cs`(`CombatPreparation`) | 새 생성자(P2). 검사는 `!FitsBoard`면 예외(빈 구역 허용). 옛 생성자는 `Rows(playerDeploymentDepth)`로 이어지고, 옛 검사 `:53-55`의 합 검사는 지운다. 속성은 T3와 같은 셋 |
| T5 | `Contracts/Combat/CombatBattleFacts.cs` | 새 생성자 `(reserve, seed, PlayerDeploymentZone playerZone, drawOrder, drawCursor, 나머지 선택 인자 그대로)`. 옛 생성자는 `Rows(playerDepth)`로 이어지고 `enemyDepth < 0` 검사는 지운다. 속성은 T3와 같은 셋 |
| T6 | `Application/Combat/BattlePreparationCalculator.cs` | `:181-186` 새 `CombatPreparation` 생성자에 `stage.PlayerZone`을 넘긴다. `:242-243` `new DeploymentZonePolicy(stage.PlayerZone)`. 예외 문구 `:244` "초기 배치가 소유자의 배치 구역을 벗어났습니다"는 "초기 배치가 아군 배치 구역 규칙에 맞지 않습니다"로 바꾼다(적에게는 "구역 밖"이 아니라 "아군 구역 안"이 거절 이유라서) |
| T7 | `Application/Combat/CombatPlacementCalculator.cs:80-84` | `Zones()` → `new DeploymentZonePolicy(CombatTurnCalculator.RequiredBattle(input).PlayerZone)`. 부르는 두 곳(`:30`·`:164`)은 그대로 둔다 |
| T8 | `Application/Combat/EnemySpawnCalculator.cs:49-58` | 정책을 한 번 만들고, `y = 0 .. MapHeight-1`, `x = 0 .. MapWidth-1`(y → x)을 돌며 기존 두 조건에 `policy.CanDeploy(Owner.Enemy, new[] { cell })`를 더한다. 주석 `:55`·`:62`는 그대로 둔다 |
| T9 | `BufferSystems/Combat/CombatStateData.cs` | `PlayerZone` 속성을 더한다. 생성자 둘은 `preparation.PlayerZone`과 `source.PlayerZone`을 복사한다. 옛 두 속성은 저장하지 않고 T3와 같은 계산식으로 바꾼다(View가 읽는 전환 입구) |
| T10 | `CoreSystem/CombatSystem/CombatInputAssembler.cs:74-76` | 새 `CombatBattleFacts` 생성자에 `battle.PlayerZone`을 넘긴다 |
| T11 | `CoreSystem/CombatSystem/BattleDefinitionAdapter.cs:89-90·105-108` | 새 `StageConfig` 생성자에 `PlayerDeploymentZone.Rows(field.PlayerDeploymentDepth)`(PZ-a 한정, PZ-b에서 대체 규칙으로 바뀜)를 넘긴다. `:105-108`의 다시 만들기는 `stage.PlayerZone`을 그대로 옮긴다 |
| T12 | Core 시험 맞춤(§3) | 기대가 바뀌는 다섯은 설정만 맞추고, 정책 생성자 한 줄은 호출만 맞춘다 |
| T13 | 새 Core 시험(§4, P6) | — |
| T14 | 남은 사본·설명 찾기 | `grep -rn "DeploymentDepth\|배치 구역\|EnemyDeployment" CoreCaculator CombatSystem BufferSystems`로, Core 로직이 옛 속성을 더 읽지 않는지 확인한다(전환 입구 정의만 남아야 함). 주석의 "위쪽 N줄" 설명은 고친다 |

**PZ-a Unity 확인(사용자)**
1. 컴파일 오류 0(Framework 시험 포함 전체).
2. Test Runner `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 실패가 있으면 결과 XML(LocalLow)로 확인한다.
3. 재생(가중치 적 출현 스테이지, 예: `Combat_Normal_A`)
   - 적이 아래 2줄을 뺀 곳 어디에나 나온다.
   - 아군은 지금처럼 아래 2줄에만 놓인다.
   - 적 구역 색이 사라진다(예상된 동작).
   - 밤 끝 → 배치 → 전투 시작 때 적이 다시 배치된다.

## 2. Phase PZ-b — DB 아군 칸 목록과 대체 규칙

끝나면: 스테이지 에셋에 아군 칸을 넣으면 그 칸만 아군 구역이 된다. 비우면 필드 아군 줄 수(지금과 같음)다.

| Task | 파일 | 할 것 |
|---|---|---|
| T15 | `DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageDetail.cs` | `IReadOnlyList<CellCoord> PlayerDeploymentCells` 속성을 더한다. 생성자 **끝** 선택 인자 `IReadOnlyList<CellCoord> playerDeploymentCells = null`(호출 19곳 그대로), 저장은 읽기 전용 복사본 |
| T16 | `…/Combat/CombatStageData.cs` | `[Header("Deployment")]` + `[Tooltip("아군 배치 구역 칸입니다. 비우면 필드의 아군 배치 줄 수(아래부터)를 씁니다. 적은 아군 구역 밖 어디에나 놓입니다.")] [SerializeField] private CellInput[] playerDeploymentCells = Array.Empty<CellInput>();`. `CreateStageData`가 `CellInput.ToCellCoords(playerDeploymentCells)`를 넘긴다 |
| T17 | 같은 파일 `:178-182` | 필드가 있고 칸이 비어 있지 않을 때 검사한다(P5). ① `PlayerDeploymentZone.Of(cells)`가 중복으로 `ArgumentException`을 던지면 `InvalidOperationException($"[CombatStageData] {name}: …", inner)`로 다시 던진다(Core 문구에 중복 좌표가 들어 있음). ② `zone.CellsOutside(field.ColumnCount, field.RowCount)`가 비어 있지 않으면 `InvalidOperationException`을 던진다. 문구에는 **에셋 이름(`name`)**, 판 크기, 판 밖 좌표 목록을 넣는다. 스테이지 키는 `StageRootDataSO`가 갖고 이 SO에는 없으므로 넣지 않는다(2026-10-27 외부 검토 반영) |
| T18 | 같은 파일 툴팁 `:105`·`:114` | `mainCharacterAnchor`: "아군 배치 구역 안이어야 합니다…"(그 뒤 문장은 그대로). `enemyHeadquartersAnchor`: "아군 배치 구역 밖이어야 합니다." |
| T19 | `BattleDefinitionAdapter.cs:89-90` | **대체 규칙의 원본:** `combat.PlayerDeploymentCells.Count > 0 ? PlayerDeploymentZone.Of(combat.PlayerDeploymentCells) : PlayerDeploymentZone.Rows(field.PlayerDeploymentDepth)`. 한국어 주석 한 줄로 원본임을 적는다 |

| T19a | CoreIntegration 전달 경로 시험(§4 아래 표) | `CoreIntegrationTestData`에 스테이지 아군 칸 생성 인자를 더하고, 칸 구역·빈 목록 대체 두 사례를 넣는다 |

- SO 직렬화 필드를 새로 더하는 것이다. 기존 에셋은 빈 배열로 읽혀서 에셋을 바꾸지 않아도 된다.
- 민형님·호진님 SO 모드 도구는 필드 이름으로 SO를 읽으므로 이 칸을 모른 채 그대로 동작한다.

**PZ-b Unity 확인(사용자)**
1. 컴파일 오류 0, Core·CoreIntegration 전체.
2. 부트 DB 생성 성공(모든 에셋 칸 목록이 비어 있음).
3. 시험용으로 `Test` 스테이지 인스펙터에 아군 칸 몇 개(떨어진 조각 포함)를 넣고 재생한다.
   - 그 칸에만 아군을 놓을 수 있다.
   - 적은 그 칸을 뺀 곳에 나온다.
   - 판 밖 칸이나 중복 칸을 넣으면 부트에서 멈춘다.
   - View 아군 색과 놓을 수 있는 칸 표시는 나오지 않는다(예상, 설계 PZ7). 시험이 끝나면 칸을 지울지 사용자가 정한다.

## 3. 기존 Core 시험 맞춤 (T12)

확인하는 내용은 바꾸지 않고, 바뀐 규칙에서 같은 상황이 되게 설정만 맞춘다. 모두 천명 작성이다(계획 실행 때 `git blame`으로 다시 확인).

| 파일 | 줄 | 지금 | 맞춤 |
|---|---|---|---|
| `EnemySpawnCalculationTests.cs` | `:54` `Cliffs_…` | `new StageConfig(8, 6, 8, 2, 2)` → 적 칸 y4~5 | `new StageConfig(8, 6, 8, 4, 2)` → 아군 y0~3, 적 칸 y4~5. 주석 `:48`을 "적이 놓일 수 있는 칸은 아군 구역(y0~3) 밖인 y4~5이며…"로 |
| 같은 파일 | `:73` `Obstacles_…` | 같음(적 칸 16) | 같은 맞춤(적 칸 16 유지) |
| `CombatCommonCommandTests.cs` | `:171` `EnemyRedeployment_…AndRejectsFullZone` | `new CombatBattleFacts(8, 37, 1, 1, …)` → 적 칸 = y7 | `new CombatBattleFacts(8, 37, 7, 1, …)` → 아군 y0~6, 적 칸 = y7 |
| `WallMovementTests.cs` | `:133` `EnemyRedeployment_ExcludesWallsAndRejectsWhenNoCellRemains`(false·true) | `new CombatBattleFacts(8, 42, 2, 1, …)` → 적 칸 = y7. 바뀐 규칙에서는 y2~7이 후보라, 전체 줄이 막혀도 성공하고(true), 부분 막힘에서도 대체 칸이 (1,7)이 아니라 y6 쪽이 된다(false) | 아군 줄 수 2 → 7 → 적 칸 = y7. 메인 (7,4)는 점유일 뿐이라 상관없다 |
| `WeakGroundMovementTests.cs` | `:140` `EnemyRedeployment_ExcludesCliffsAndDoesNotInventAnAvailableCell`(false·true) | 같음(낭떠러지 줄) | 같은 맞춤(2 → 7) |
| `EnemyRedeploymentResolverTests.cs` | `:56` | `new DeploymentZonePolicy(1, 1, 5)` | `new DeploymentZonePolicy(PlayerDeploymentZone.Rows(1))`(using 한 줄) |

- 위 두 줄(벽·약한 지반 재배치)은 2026-10-27 외부 검토가 찾아 더했다. 그 뒤 `enemyOrigins`·`RedeployEnemies`를 쓰는 Core 시험 10개 파일을 다시 훑었다. 나머지는 원위치가 비어 있어 대체 칸을 찾지 않거나(`EnemyRedeploymentResolverTests:32`, `MineEnvironmentTests:315`, `ObjectArrivalOrderTests:119`, `CombatRewardParticipationTests:82·104`, `CombatItemLifetimeTests`), 이미 판 전체가 적 칸이라(`BossTurnTests:114`, 아군 0·적 8) 결과가 같다.

- 시험 이름의 "FullZone"은 "적이 놓일 칸이 모두 찬 경우"라는 뜻으로 그대로 맞으므로 이름은 바꾸지 않는다.
- 다시 확인할 것(바뀌지 않을 것으로 봄):
  - `BattlePreparationRuleTests.EnemyPlacement_RejectsPlayerOnlyDefinitionsAndWrongZone`: 적 (1,0)은 아군 구역 안이라 여전히 거절된다.
  - `CombatRewardParticipationTests.RejectedRedeployment_…`
  - `InitialTornadoPlacementTests`: 토네이도는 구역을 보지 않고 주석만 "배치 구역"을 말한다.
  - 줄 수 속성을 읽어 계약을 다시 만드는 CoreIntegration 둘(`ImpactBoltBattleSource.cs:51`, `ControllerTurnCountPublicationTests.cs:142·169`)과 Core `ExperienceAttributionPathTests.cs:87-88`: DB·시험 정의가 모두 줄 구역이라 같은 구역으로 돌아온다.
- 운영 DB의 가중치 출현 자리를 단언하는 시험은 찾지 못했다. PZ-a 실행 결과의 실패에서 다시 본다.

## 4. 새 Core 시험 (T13, P6 — `ProjectL.Tests.Core`)

| 새 파일 | 사례 |
|---|---|
| `PlayerDeploymentZoneTests.cs` | `Rows(2)`가 y0·y1을 포함하고 y2는 포함하지 않음 / `Of`가 y → x로 정렬함 / `Of` 중복은 예외 / `Rows(-1)`은 예외 / `FitsBoard`: 줄 수 > 높이, 칸이 판 밖 / `IsEmpty`: `Rows(0)`, 빈 `Of` / `RowDepth`: 줄 구역은 값, 칸 구역은 null |
| `DeploymentZonePolicyTests.cs` | 칸 구역에서 아군은 구역 칸만, 적은 구역 밖만 / 다칸 적 중 한 칸이 구역 안이면 거절 / Neutral 거절 |
| `EnemySpawnCalculationTests.cs`(기존 파일에 사례 추가) | 아군 구역 밖의 가운데 줄도 후보가 됨 / 칸 구역(떨어진 조각)의 칸에는 나오지 않음 |
| `StageConfig` 검사(기존 `InitialEnemyPlacementSpecTests.cs` 옆 또는 새 파일, 실행 때 정함) | 빈 구역 예외 / 판 밖 칸 예외 / 옛 생성자의 줄 수 합 ≥ 높이가 더는 예외가 아님 / 옛 속성 계산(`PlayerDeploymentDepth`, `EnemyDeploymentDepth` = 0) |

**CoreIntegration 전달 경로 시험(2026-10-27 외부 검토 반영)**

단위 시험만으로는 `DB → StageConfig → CombatPreparation → Buffer → CombatBattleFacts` 중 한 곳이 줄 수로 되돌려도 통과할 수 있다. 칸 구역의 옛 줄 수 속성은 0이라, 전달이 빠지면 아군 구역이 통째로 사라진다. 그래서 `ProjectL.Tests.CoreIntegration`에 끝에서 끝까지 확인하는 시험을 둔다. 이 시험은 PZ-b(T19 어댑터 대체 규칙) 뒤에 들어간다.

| 새 파일 | 사례 |
|---|---|
| `PlayerDeploymentZonePublicationTests.cs` | **칸 구역:** 가짜 DB(`CoreIntegrationTestData`, 8×6 필드, 아군 줄 수 2)의 스테이지에 아군 칸 `{(1,0), (4,3)}`을 넣는다. ① 정의 읽기(`DatabaseSource(...).Read`) 뒤 `Stage.PlayerZone`이 그 두 칸이다. ② 전투 준비 뒤 Buffer `CombatStateData.PlayerZone`이 같은 두 칸이다. ③ 명령 제출(메인 (1,0) 수락 → 서브 (4,3) 수락, 옛 줄 밖 → 서브 (2,0) 거절 `InvalidPlacement`, 옛 줄 안이지만 구역 밖)으로 `CombatBattleFacts`까지 칸 구역이 왔음을 확인한다. ④ 명령 반영 뒤 Buffer 구역이 그대로다(복사 생성자) |
| 같은 파일 | **빈 목록 → 줄 수 대체:** 같은 스테이지에 칸 목록을 비운다. 정의의 `PlayerZone.RowDepth`가 2이고, 서브 (2,0)은 수락, (4,3)은 거절된다 |

- 틀: `CoreIntegrationTestData`에 스테이지 아군 칸을 넣는 생성 인자(선택, 기본 빈 목록)를 하나 더한다. 덱 설치·명령 제출은 기존 `BossStagePreparationTests`·`ConfiguredMovementBattle`의 방식을 따른다(실행 때 가장 짧은 쪽을 고른다).
- 이 시험은 `CoreIntegrationTestData`(천명 작성 공용 틀)를 고친다. 생성 인자만 더하므로 다른 사용처는 그대로다.
- Framework 시험은 고치지 않는다.

## 5. Phase PZ-c — 문서·공유

| Task | 할 것 |
|---|---|
| T20 | `docs/superpowers/guides/unit-movement-attack-core-handoff.md` §5-2·`:422-423`·`:541`·`:640`의 "아래·위 N줄", "Enemy 배치 구역" 설명을 설계 §2 규칙으로 고친다(예시 코드 `:54-55`·`:408-409`는 옛 생성자라 그대로 동작함을 한 줄 적는다) |
| T21 | Core 전달 문서 §43(배치 구역 요청)에 응답 한 단락: 적 구역 삭제 결정, 이 설계·계획 링크, 요청 Q1~Q5에 대한 답(Q2는 적 구역이 없어져 해당 없음, Q3는 전환 입구로 유지) |
| T22 | 공유 메모 셋을 **채팅 글로** 낸다(메모리 규칙, 파일 없음): 용근님(View 구역 색을 `PlayerZone.Contains`로, 적 구역 색 걷기, 개발 진입 새 생성자, SO 모드 적 줄 수 슬라이더·아군 칸 붓, 옛 입구는 PZ-d에서 지움), 호진님(진영 칸 초안 DZ의 적 부분이 이 결정으로 바뀜, DB 새 칸 이름 `playerDeploymentCells`), 민형님(DT·엑셀 도구 줄 수 표시) |
| T23 | 설계 문서에 구현 메모(계획과 달라진 점)를 적는다 |

## 6. 커밋 묶음 (사용자)

1. PZ-a: T1~T14. `PlayerDeploymentZone.cs`와 `.meta`, 새 시험 파일과 `.meta`를 함께 넣는다. Core 계약과 사용처가 한 번에 바뀌므로 나눌 수 없다.
2. PZ-b: T15~T19a. DB 코드와 CoreIntegration 시험(새 파일 + `.meta`)이 바뀐다. 에셋은 칸 목록을 넣었을 때만 바뀐다.
3. PZ-c: 문서.

## 7. 위험과 대응

- **같은 시드에서 적 출현 자리가 바뀐다**(설계 §5). 저장된 Run이나 재현 기록이 출현 자리를 기대하면 달라진다. 지금 그런 기록은 알지 못한다.
- **칸 구역 스테이지의 화면:** View가 옮기기 전에는 아군 색과 놓을 수 있는 칸이 안 보인다. 실제 레벨에 칸 목록을 쓰기 전에 용근님 작업이 필요하다(T22).
- **전환 입구의 함정:** 칸 구역을 옛 속성으로 다시 만들면 아군 줄 수 0이 되어 실패한다. Core 로직은 옛 속성을 읽지 않는다(T14에서 확인). 바깥(View·Framework 시험·개발 진입)은 PZ-d 전까지 이 점을 공유 메모에 적어 둔다.

## 8. 실행 기록

### 8.1 PZ-a 코드 완료 (2026-10-27, 컴파일 미확인, Unity 시험 미실행)

사용자 지시는 "P1~P6 그대로 하고 PZ-a 진행해"였다. T1~T14를 계획대로 했다. MSBuild는 돌리지 않았다.

- **새 파일(`.meta`는 Unity가 만듦, 커밋 때 함께):**
  - `Contracts/Common/PlayerDeploymentZone.cs`
  - 시험 `PlayerDeploymentZoneTests.cs`·`DeploymentZonePolicyTests.cs`·`StageConfigPlayerZoneTests.cs`
- **바꾼 Core·Buffer:**
  - `DeploymentZonePolicy`(옛 생성자 삭제)
  - `StageConfig`·`CombatPreparation`·`CombatBattleFacts`(새 생성자 하나씩 + 옛 생성자는 전환 입구 + 계산 속성)
  - `BattlePreparationCalculator`·`CombatPlacementCalculator`·`EnemySpawnCalculator`
  - `CombatStateData`·`CombatInputAssembler`·`BattleDefinitionAdapter`(아직 `Rows(필드 아군 줄 수)`)
- **기존 시험:** §3의 다섯 곳(설정만)과 정책 생성자 한 줄을 고쳤다. 모두 천명 작성(`git blame` 확인). `EnemySpawnCalculationTests`에는 새 사례 둘(아군 줄 밖 전체 후보, 칸 구역 제외)을 더했다.
- **계획과 다른 점:**
  - `CombatBattleFacts` 옛 생성자는 음수 줄 수일 때 예전과 같은 `ArgumentException`을 던진다. `Rows`가 던지는 `ArgumentOutOfRangeException`을 그대로 두면 NUnit `Throws<ArgumentException>`(정확한 형식 비교)를 쓰는 곳이 깨질 수 있어서다.
  - 정책의 null 칸 검사는 지웠다. `CellCoord`가 구조체라 늘 거짓이던 검사다.
- **T14 확인:** Core·Buffer 로직에서 옛 줄 수 속성을 읽는 곳은 없다. 남은 것은 전환 입구 정의와, 어댑터가 DB `FieldData.PlayerDeploymentDepth`를 읽는 한 줄뿐이다.
- **예외 문구:** 고정 적 거절 문구가 "초기 배치가 아군 배치 구역 규칙에 맞지 않습니다."로 바뀌었다. 옛 문구를 단언하는 Core·CoreIntegration 시험은 없다.
- **줄 끝:** git bash `sed`가 CRLF를 LF로 바꿔서 `unix2dos`로 되돌렸다. 저장소 `core.autocrlf=true`라 diff는 내용 줄만 나온다.

**사용자 확인 대기:** 계획 §1의 PZ-a Unity 확인 1~3.

### 8.2 PZ-a Unity 시험 결과 (2026-10-27 14:30, 사용자 실행, 결과 XML 확인)

- **`ProjectL.Tests.Core`: 3749 / 3749 통과.** 새 시험 셋(13사례), 출현 새 사례, 고친 재배치 시험 다섯이 모두 들어 있다.
- **`ProjectL.Tests.CoreIntegration`: 167 / 168 통과.** 실패한 하나는 이번 변경과 무관하다.
  - 시험: `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`
  - 오류: `KeyNotFoundException: Stage data not found. StageKey: 2101`
  - 원인: 재령님 `ba9b05fe`("렙디 수정")가 `StageRootDataTable.asset`에서 `Stage_Boss`(2101, guid `d722b1ff…`)와 guid `fae0b71f…` 항목을 뺐다. 그래서 운영 DB에 보스 스테이지가 없다. 시험은 구역 코드에 닿기 전, 스테이지를 읽는 곳(`StageTable.GetStage`)에서 멈춘다.
  - 처리: 의도한 변경인지 재령님께 확인할 일이다. 표와 시험은 고치지 않았다.
- 컴파일 오류는 없다(시험이 돌았음).
- 남은 것: 계획 §1 PZ-a 확인 3(재생).

### 8.3 PZ-a 재생 확인 · PZ-b 코드 완료 (2026-10-27, 컴파일 미확인, Unity 시험 미실행)

- PZ-a 재생 확인 문제 없음(사용자).
- 사용자 지시는 "PZ-b 진행해"였다. T15~T19a를 계획대로 했다.
- **DB:**
  - `CombatStageDetail`: `PlayerDeploymentCells` 속성과 생성자 끝 선택 인자를 더했다. 호출 19곳은 그대로다.
  - `CombatStageData`:
    - `[Header("Deployment")] playerDeploymentCells`를 더했다. 전용 인스펙터는 `DrawPropertiesExcluding`이라 새 칸이 자동으로 보인다.
    - 저작 검사 `ValidatePlayerDeploymentCells`를 더했다. 중복은 Core `Of`의 문구에 에셋 이름을 붙여 다시 던지고, 판 밖은 에셋 이름·판 크기·좌표를 문구에 넣는다.
    - 툴팁 둘을 고쳤다.
  - `System.Linq` using 한 줄을 더했다.
- **어댑터:** 대체 규칙의 원본을 두었다. 칸 목록이 있으면 `Of`, 비었으면 `Rows(필드 아군 줄 수)`다.
- **CoreIntegration:**
  - `CoreIntegrationTestData`에 `playerDeploymentCells` 선택 생성 인자를 더했다. 스테이지 필드 초기화는 생성자로 옮겼고, 기존 사용처는 그대로다.
  - 새 `PlayerDeploymentZonePublicationTests`(칸 구역, 빈 목록 대체)를 더했다. 진입은 `ConfiguredMovementBattle`과 같은 순서로 따로 거친다. 그 틀은 정해진 칸에 배치까지 마쳐서 칸 구역과 맞지 않는다.
- **계획 밖 한 줄:** `ConfiguredMovementBattle.cs:219`(천명 작성)의 "적 배치 깊이 3" 주석이 이제 틀려서 바로잡았다.
- **에셋 변경 없음:** 모든 스테이지의 칸 목록이 비어 있어 동작은 PZ-a와 같다.
- `.meta`: PZ-a의 새 파일 넷은 Unity가 만들었다. 새 시험 `PlayerDeploymentZonePublicationTests.cs`의 `.meta`는 다음 Unity 열기 때 생긴다.

**사용자 확인 대기:** 계획 §2 PZ-b Unity 확인 1~3.

### 8.4 PZ-b Unity 시험 결과 (2026-10-27 14:38, 사용자 실행, 결과 XML 확인)

- `ProjectL.Tests.Core`: 3749 / 3749 통과.
- `ProjectL.Tests.CoreIntegration`: 169 / 170 통과.
  - 새 `PlayerDeploymentZonePublicationTests` 두 사례는 통과했다.
  - 실패 하나는 §8.2와 같은 보스 스테이지 2101 미등록이다(재령님 `ba9b05fe`, 이번 변경과 무관).
- 남은 것: PZ-b 재생 확인(계획 §2의 2·3, 선택), PZ-c 문서·공유.

### 8.5 PZ-c 문서 완료 (2026-10-27, 코드 변경 없음)

- T20 `guides/unit-movement-attack-core-handoff.md`를 고쳤다.
  - `StageConfig` 속성 표: `PlayerZone`을 더하고, 옛 두 속성은 전환 입구로 적었다.
  - §5-2 배치 구역: 아군 구역 하나만 있고, 적은 아군 구역 밖이다.
  - 초기 적 오류 설명 두 곳.
  - 예시 코드(줄 수 생성자)는 그대로 동작해서 고치지 않았고, 속성 표에 그 사실을 한 줄 적었다.
- T21 Core 전달 문서 §43에 Core 응답 단락을 더했다. 결정 변경, 계약, 입력 자리, Q1~Q5 답, View에 남은 일을 적었다. 미전달이다.
- T23 설계 §10에 구현 메모를 적었다.
- T22 공유 메모 셋(용근·호진·민형님)은 채팅 글로 냈다. 파일은 없고 미전달이다.
- **계획 끝(PZ-a~PZ-c).** PZ-d(전환 입구 걷기)는 View가 옮긴 뒤 따로 한다.
- 커밋 묶음은 §6대로다. 새 `.cs` 다섯은 `.meta`와 함께 넣는다. 문서 넷(설계·계획·안내·Core 전달)도 커밋 대상이다.

### 8.6 메인 캐릭터 위치 칸 숨김 (2026-10-27, 사용자 "그냥 숨기기만 하자", 컴파일 미확인)

- View 작업자가 "건물·메인 캐릭터 anchor는 안 쓰는데 필요하냐"고 물어서, 칸별로 확인했다(채팅 보고).
  - 적 본부 칸: 스테이지 4개가 쓰므로 남긴다.
  - 추가 적 건물 칸: 모두 비어 있지만 Core 기능이 살아 있어 기획 확인 대상으로 남긴다.
  - 메인 캐릭터 위치: Core가 읽지 않는다.
- `CombatStageData.mainCharacterAnchor`·`mainCharacterRotation`에 `[HideInInspector]`를 붙이고, `[Header("Main Character")]`·툴팁 대신 주석 두 줄을 달았다. 값·직렬화·API는 그대로다.
- 실제 삭제는 PZ-d 때 SO 모드 도구(`CombatStageSoAssets.cs`가 이름으로 `Require`)와 함께 한다. `CombatStageDetail`(호출 16곳)·`BattleInitialization`(48곳) 생성자도 그때 정리한다.
