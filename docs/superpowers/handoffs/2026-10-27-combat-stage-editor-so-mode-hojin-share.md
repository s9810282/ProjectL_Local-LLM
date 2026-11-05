# 전투 스테이지 편집 SO 모드 — 호진님 공유 (StageMap·View DB, 2026-10-27, 미전달)

- 작성자: 호진(이호진)

(사용자 본인 영역 메모: StageMap·View DB 등록 영향 정리)

전투 스테이지 편집 도구에 **SO 모드**가 생겼습니다. 기획자가 전투 스테이지 SO를 만들고 **Root 표·View 표에 등록**할 수 있습니다. 이 등록은 StageMap의 Run 노드 후보에 바로 영향을 줍니다. 그래서 공유합니다. 기획자용 사용법은 [`Tool/SO모드 사용법.md`](../../../Assets/ProjectL/Script/Tool/SO모드%20사용법.md)입니다.

호진님 코드는 바꾸지 않았습니다.

## 1. 등록이 StageMap에 주는 영향

- 도구의 **표에 등록**은 Stage Root를 `StageRootDataTable`에, 짝 View를 `StageViewPropDataTable`에 **함께** 넣습니다. **표에서 빼기**도 함께 뺍니다. Group·Catalog는 바꾸지 않습니다.
- 등록하면 그 Stage Root의 맵 키·맵 역할대로 Run 후보가 됩니다. Regular는 일반 노드 후보이고, Boss는 보스 노드입니다.
- 등록은 저장된 에셋 기준입니다. 저장 검사(`GetData()` + 짝 View + 스테이지 키 유일)를 통과해야 등록됩니다.
- 새 스테이지 키 제안은 틀 키의 백 단위입니다(일반 11xx, 보스 21xx). 상점 1201·강화 1202·이벤트 1301과 겹치지 않게 다른 Root 키 전체와 비교합니다.
- 빼면 Run 후보에서 빠지고, 개발 진입 Play From Boot `PlacedStage`(등록된 일반 스테이지만 받음)로도 열 수 없습니다.
- **맵 생성 규칙을 봅니다(병합 대응 A2, 2026-10-27 추가).** 등록·빼기, 그리고 등록된 스테이지의 맵 키·맵 역할을 바꾼 저장 때, 바뀐 뒤 표 상태로 그 맵을 판정합니다. 보스 수는 `StageGeneratorSystem` 규칙의 사본으로 보고, 출현 종류별 후보와 맵 모양은 Core 계약 `ChapterStagePool`·`ChapterGraphSpec`을 만들어 봅니다([SO 모드 설계 §10](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#10-병합-대응-a1a2-mg1mg9)).
  - 이번 변경이 맵을 깨뜨리면 막습니다. 전부터 깨진 맵이면 경고 뒤 진행합니다.
  - 보스가 둘이 되면 "기존 보스를 표에서 빼고 진행"을 묻습니다. 고르면 **다른 스테이지(옛 보스)의 Root·짝 View를 표에서 뺍니다.** 짝 View가 없으면 Root만 뺍니다.
  - 맵 키에 맵 SO가 없으면 막지 않고 안내만 합니다.
  - 표를 Inspector에서 직접 고치면 이 검사를 거치지 않습니다.

## 2. 도구가 만드는 에셋

| 만드는 것 | 이름·위치 | 표 |
|---|---|---|
| 새 스테이지 한 벌 | `Stage_<이름>`·`Combat_<이름>`·`StageViewProp_<이름>`, 틀과 같은 폴더 | **미등록**으로 만듦 |
| 공유 SO 전용 복제(필드·로스터·경제·적 로드아웃) | `<원본>_<스테이지 이름>`, 원본 폴더 | 원본이 든 표에 등록(새 키) |
| 새 필드 / 아군 줄 수를 바꾼 공유 필드 | `Field_<가로>x<세로>…` 또는 `<원본>_<스테이지 이름>` | `FieldDataTable`에 등록 |

필드 아군 줄 수(아군 배치 깊이)를 스테이지별로 바꿀 수 있습니다. 공유 필드면 전용 복제한 뒤 바꾸므로 다른 스테이지는 그대로입니다. 이 줄 수는 스테이지 칸 목록(`playerDeploymentCells`)이 비었을 때만 쓰입니다. 적 배치 깊이는 Core가 읽지 않아 도구가 고치지 않고, 새 필드는 틀 필드 값을 그대로 둡니다(2026-10-28 바뀜, 천명님 `5a1bfbe1f` 대응, [SO 모드 설계 §11](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#11-아군-배치-구역-칸-az1az9)).

아군 배치 구역 칸 목록은 전투 데이터 SO에 바로 씁니다(새 SO·표 등록 없음).

## 3. View DB 표 필드 이름

도구는 View DB 외형 표(유닛·지형·오브젝트 외형)와 필드 보기 SO를 **필드 이름**으로 읽습니다. 용도는 한국어 이름과 3D 미리보기입니다. 읽는 필드 목록은 [전투 View 사본 공유 §3](2026-10-27-combat-stage-editor-so-mode-view-share.md)에 있습니다. 이 필드 이름이 바뀌면 도구의 `CombatStageSoAssets.cs` 상수도 함께 바꿔야 합니다.

맵 생성 규칙(§1) 때문에 StageMap 쪽 SO도 **필드 이름·형식 이름**으로 읽습니다(읽기만, 2026-10-27 추가). 바뀌면 같은 파일의 상수를 맞춰야 합니다.

- 맵 SO(`StageMapDataSO`, 표 `StageMapDataTableSO`): `mapKey`·`depthCount`·`widthMax`·`seed`·`connectionAlgorithm`(이름으로 Core 연결 방식에 대응)·`nearestConnectionWeight`·`otherConnectionWeight`·`additionalConnectionChance`, 출현 칸 넷 `combatDistribution`·`shopDistribution`·`recoveryDistribution`·`enhancementDistribution`의 `weight`·`minimumCount`. 출현 칸 → 종류 짝은 `StageMapDataSO.GetData`의 사본입니다.
- Stage Root: `mapRole`(enum 이름이 "Regular"면 일반, 그 밖은 보스)과 `stageData`가 가리키는 상세 SO 형식 이름(`CombatStageData`·`ShopStageData`·`EventStageData`·`EnhancementStageData` → 전투·상점·회복·강화).
- 맵 SO 하나를 읽지 못하면 그 맵만 "읽지 못함"으로 보이고 도구는 계속 열립니다.

아군 배치 구역 대응(2026-10-28 추가) 때문에 InGame SO 셋을 더 읽습니다. 바뀌면 같은 파일의 상수를 맞춰야 합니다.

- 전투 데이터 `CombatStageData.playerDeploymentCells`(`CellInput`의 `x`·`y`): 읽고 씁니다(y → x 정렬).
- 적 로드아웃 `EnemyLoadoutDataSO.spawnCount`: 읽기만(참고 수·경고 ② "출현 수 > 적이 놓일 칸").
- 지형 정의 `TerrainDefinitionDataSO.kind`: 읽기만. 열거 **이름**이 `Wall`·`Cliff`면 벽·낭떠러지로 봅니다(Core `EnvironmentTerrainRules.BlocksVoluntaryEntry`의 사본). 열거 이름이 바뀌면 도구 참고 수가 틀어집니다(게임 동작은 그대로).

## 4. 보고만

- `Test` 스테이지(`InGameGroup/Stage/Root/Test.asset`, 키 10000, 맵 1, Regular)가 Root 표에 등록돼 있어 지금 Run 후보입니다. 의도인지 확인해 주세요. (→ 정정 2026-10-27: 지금은 틀립니다. 재령님 `ba9b05fe`에서 `Test`가 Root 표에서 빠져 Run 후보가 아닙니다. [SO 모드 계획의 남은 일](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md)(옛 병합 대응 목록 D2)에 같은 내용이 있습니다.) (→ 다시 바뀜 2026-10-29: 이 정정 줄도 지금은 틀립니다. `Test`가 다시 Root 표에 등록돼 맵 1 Regular Run 후보이고, View가 바닥 타일 방식(`PrefabWithTiles`, 17 × 17, 용근님 `954527d9`)입니다. 같은 날 강화 스테이지 `Stage_Enhancement`는 Root 표에서 빠졌고 맵 1 강화 가중치는 0, 층 수는 5입니다.)
- 기획자가 도구로 만든 시험 스테이지·전용 복제 에셋이 커밋에 섞일 수 있습니다. 병합 때 함께 봐 주세요.
- 필드 SO의 적 배치 깊이(`enemyDeploymentDepth`)는 Core가 읽지 않는데, 인스펙터에 그대로 보이고 1 이상 검사도 남아 있습니다(천명님 `5a1bfbe1f`). 툴팁이나 필드 정리는 DB 담당과 정해 주세요(Core 설계 Q-PZ2). 도구는 공유 필드 값 편집 위에 "적 배치 깊이(Enemy Deployment Depth)는 Core가 읽지 않습니다" 줄을 둡니다(2026-10-28 추가).
- 맵 1의 보스는 `Stage_Normal_A`(1101)입니다. 재령님 `ba9b05fe`가 역할을 Regular → Boss로 바꾸고 `Stage_Boss`(2101)·`Test`(10000)를 Root 표에서 뺐습니다(2026-10-27 확인). 키 규칙(보스 21xx)과 다르니 의도인지 확인해 주세요. 같은 날 작업 트리에서는 미등록 `Stage_Boss`의 역할이 Regular로 바뀌어 있었습니다(커밋 전). 미등록이라 지금 맵에는 영향이 없고, 등록하면 전투 후보가 됩니다.
