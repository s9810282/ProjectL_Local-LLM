# 아군 배치 구역 칸 단위 — SO 모드 도구 할 일 (호진님께)

- 작성: 2026-10-28, View 작업(용근)
- 상태: **공유 메모, 미전달.** SO 모드 도구 코드는 고치지 않았다(2026-10-28 사용자 "레벨 툴은 그냥 두고").
- 관련 문서:
  - 천명님 [아군 배치 구역 설계](../specs/2026-10-27-combat-player-deployment-zone-design.md): 규칙 원본 §2, 도구 몫 PZ7.
  - [아군 배치 구역 View 설계](../specs/2026-10-28-combat-player-zone-view-design.md): 이 메모는 PV5의 할 일 목록이다.
  - 계획: [player-zone-view-plan](../plans/2026-10-28-combat-player-zone-view-plan.md).

## 1. 무엇이 바뀌었나 (Core, 천명님 `5a1bfbe1`, 2026-10-27)

- **적 배치 구역이 없어졌다.** 고정 적·적 구조물·본부·적 출현·전투 시작 재배치는 모두 "판 안이면서 아군 구역이 아닌 칸"이면 된다.
- **아군 배치 구역만 칸 집합이다.** 값은 `Contracts/Common/PlayerDeploymentZone`이고, 모양은 줄 구역 `Rows(n)`과 칸 구역 `Of(칸)` 둘이다. 판정은 `Domain/Placement/DeploymentZonePolicy` 하나다.
- **입력 자리는 스테이지 SO `CombatStageData.playerDeploymentCells`**(`CellInput[]`)다.
  - 비어 있으면 필드 SO의 `playerDeploymentDepth`(아래부터 N줄)로 대신한다. 대체 규칙의 원본은 `BattleDefinitionAdapter` 한 곳이다.
  - 필드 SO의 `enemyDeploymentDepth`는 남아 있지만(검사는 1 이상) Core는 읽지 않는다.
- 저작 검사(부트 DB 생성): 칸 목록이 있으면 중복 없음·판 안이다(`CombatStageData.ValidatePlayerDeploymentCells`).
- 전투 진입 때 Core 검사: 고정 적·적 구조물·본부가 아군 구역 안이면 예외로 멈춘다.

## 2. 지금 SO 모드 도구가 틀리게 보이는 것 (코드 대조, 2026-10-28)

| 자리 | 지금 | 새 규칙 |
|---|---|---|
| `CombatLayoutWindow.SoMode.cs:1743-1749` 경고 | "적 진영(위 N줄) 밖 고정 적 → Core가 멈춤" | 반대다. 위 줄 밖의 적은 정상이고, **아군 구역 안의 적**이 멈춤이다 |
| `:2066-2070` 구역 색·이름(`ZoneColor`·`ZoneName`) | 아군(아래 N줄) / 적(위 N줄) / 중립 | 아군 구역 / 나머지. 칸 목록이 있으면 그 칸이 아군 구역이다 |
| `:1733-1740` 진영 줄 수 슬라이더 | 아군·적 둘, 합 < 세로 | 적 슬라이더는 의미가 없다. 아군 줄 수는 "칸 목록이 비었을 때 쓰는 값"이다 |
| `CombatStageSoAssets.cs:812-817` `MaxDepthBeside`·`EnemyStartRow` | "합 < 세로" 사본, 적 시작 줄 | 쓸 곳이 없다 |
| 새 필드 만들기(`:1955-1970`, `CombatStageSoCreation.cs:124`) | 적 배치 깊이 입력 | 필드 검사(1 이상)만 맞추면 된다(예: 1 고정) |
| 작업본(`CombatStageSoDocument`) | `playerDeploymentCells`를 읽지도 쓰지도 않는다 | 칸 구역 스테이지는 맵 색이 실제 구역과 다르게 보인다 |

- 저장은 안전하다. SO 모드 `Write`는 다루는 필드 이름만 쓰고 `playerDeploymentCells`는 건드리지 않는다. 그래서 다른 곳에서 칠한 칸이 도구 저장 때문에 지워지지는 않는다. 새로 만들기·복제(`CopyAsset`)는 틀의 칸 목록을 그대로 이어받는다.

## 3. 권장 고침 (도구 담당이 정함)

1. **구역 읽기 하나.** 작업본에 칸 목록이 있으면 `PlayerDeploymentZone.Of(칸)`, 비면 `Rows(필드 아군 줄 수)`로 만든다.
   - 도구 asmdef가 `ProjectL.Core.Contracts`를 이미 참조하므로 판정은 Core 값의 `Contains`를 부르면 된다.
   - 대체 규칙(비면 줄 수)만 사본이다. 원본 `BattleDefinitionAdapter` 자리를 주석으로 단다.
2. **작업본에 칸 목록**(`railCells`와 같은 길, `CombatStageSoAssets.cs:561`·`:655`)을 둔다. 놓인 물건이 아니라 칸 표시라서 겹침 검사에는 끼우지 않는다.
3. **맵 색·이름**은 아군 구역 / 나머지로 바꾼다. 2D·3D 덧그리기와 선택 칸 정보가 같은 함수를 쓰면 된다.
4. **경고 교체.**
   - 아군 구역 안의 고정 적·적 구조물·본부 → "전투 진입 때 Core가 멈춥니다". 판정은 `DeploymentZonePolicy.CanDeploy(Owner.Enemy, 칸)`을 부른다(asmdef가 `ProjectL.Core.Domain`도 참조).
   - 아군 구역 밖 칸이 하나도 없으면 경고한다.
5. **슬라이더**: 적 줄 수는 걷는다. 아군 줄 수는 칸 목록이 비었을 때만 켠다.
6. (선택) "아군 구역" 붓. 칠하기는 이미 `CombatStageData` 인스펙터 격자에 있으므로 꼭 필요하지는 않다(§4).

## 4. 같은 칸을 고치는 다른 화면: `CombatStageData` 인스펙터 격자 (2026-10-28, View 작업)

- 자리: `Systems/DataBaseSystem/Editor/CombatStageDataEditor.cs`의 "아군 배치 구역" 묶음.
- 동작:
  - 필드 크기만 한 격자에서 드래그로 칠하고 지운다. 끌기 한 번이 Undo 한 번이다.
  - 칸 목록은 y → x 순서로 저장한다.
  - 칸 목록이 비어 있으면 줄 구역을 옅게 보이고, 처음 칠할 때 그 줄 구역을 칸 목록으로 펼친다.
  - "줄 구역으로 되돌리기" 버튼은 목록을 비운다. 마지막 한 칸은 지우지 않는다(비우면 줄 구역으로 돌아가서).
  - 칠할 때 판 밖 칸과 겹친 칸은 함께 지워 저장한다(부트 DB 생성이 거절하는 칸이라서).
- 함께 보이는 것: 고정 적·적 구조물·본부·장애물·오브젝트·지형의 앵커 칸을 색 네모로 보이고, 범례가 붙는다. 구역 안의 적은 빨강, 오브젝트는 노랑 테두리와 경고로 표시한다.
- 판정은 Core `PlayerDeploymentZone`에 묻는다.
- SO 모드에 붓을 넣는다면 저장 형식(정렬·마지막 칸 규칙)을 이것과 맞추면 두 화면이 같은 결과를 낸다.

## 5. 이 메모를 쓰며 바꾼 호진님 줄

없다. SO 모드 도구 파일(`Tool/Editor/CombatLayoutWindow.SoMode.cs`·`CombatStageSo*.cs`)은 손대지 않았다.
