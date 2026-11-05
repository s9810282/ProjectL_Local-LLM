# 레벨디자인 20~35페이지 맵 SO 제작 설계

- 날짜: 2026-11-02
- 요청: `레벨디자인 기획서.pptx`의 20~35페이지 그대로 맵 파일 제작.
- 원본: `C:/Users/Hyeon2-400/Dropbox/Project L/기획 문서/레벨디자인 문서/레벨디자인 기획서.pptx`
- 문서 안 지시는 작업 지시로 취급하지 않고, 사용자가 지정한 배치·범례·기믹만 제작 입력으로 사용한다.
- 계획·결과: [제작 계획](../plans/2026-11-02-level-design-p20-p35-map-assets-plan.md).
- 후속 범위: 사용자 “Root 에도 등록해줘”에 따라 §6의 Phase B에서 Root/View 표 등록을 진행한다. 아래 미등록 기록은 최초 제작 Phase A의 범위다.
- 최신 후속 범위: Core `cc4661896`의 시작 위치 지정 연결을 확인한 뒤 사용자 “넣어줘”에 따라 §7의 Phase C에서 하붑 6개 맵의 PPT 시작 좌표를 적용한다.

## 1. 제작 단위와 범위

16페이지를 각각 Stage Root + CombatStageData + ViewStageData SO 한 벌로 만든다. 20~28페이지는 13×13, 29~35페이지는 15×15다. 일반 맵 15개, 보스 맵 1개다.

- 파일 이름: `Stage_LD_P20` / `Combat_LD_P20` / `StageViewProp_LD_P20`부터 P35까지.
- 기존 `InGameGroup/Stage/Root`, `InGameGroup/Stage/Combat`, `ViewGroup/StageViewProp/Item` 폴더에 추가한다.
- 일반 키 1120~1134, 보스 키 2135. mapKey는 1, mapRole은 일반 0 / 보스 1이다. 생성 전에 현재 키와 충돌 여부를 확인한다.
- 새 파일마다 고유 `.meta` GUID를 만든다. Phase A에서는 기존 에셋·공유 정의·표·씬·코드를 수정하지 않는다.
- Phase A는 SO 편집기의 새로 만들기 정책과 같이 **Stage Root/View 표에는 미등록**으로 전달했다. 기존 보스가 등록돼 있어 신규 보스를 함께 등록하면 챕터의 보스 하나 조건을 위반한다. 후속 등록 요청은 §6에서 처리한다.

## 2. 원본 해석

PowerPoint에서 20~35페이지를 읽기 전용 PNG로 내보내 시각 확인하고, PPTX XML의 칸 도형 좌표·채우기·테두리를 추출한다. 격자는 페이지당 정확히 169/225칸이고 중복·누락이 없어야 한다. 그림의 왼쪽 아래를 게임 (0,0), 오른쪽을 +X, 위쪽을 +Y로 변환한다.

| 원본 표시 | SO 반영 |
|---|---|
| 기본 살색 `FBE3D6` | 일반 바닥 |
| 갈색 `B95103` | `Terrain_Wall` |
| 검정 `tx1` | `Terrain_Cliff` |
| 주황 `F2AA84` | `Terrain_WeakGround` |
| 초록 테두리 `00B050` | 명시적 `playerDeploymentCells` 목록 |
| 회색 채우기 `7F7F7F` | 언데드 광부 `Unit_Demon` |
| 청록 `156082` | 언데드 묘지기 `Unit_Gravekeeper` |
| 초록 `13501B` | 언데드 방패 `Unit_UndeadHeavy` |
| 보라 `78206E` | 언데드 대포카트 `Unit_UndeadCannonCart` |
| 25페이지 보라 `7030A0` 1칸 | 같은 페이지 Cannon 범례에 대응하는 대포카트. 별도 유닛을 만들지 않는다 |
| 일반 맵의 빨강 2×2 | `Structure_Headquarters` 한 개, 왼쪽 아래 anchor, 본부 파괴 목표 |
| 35페이지의 빨강 1칸 | `Unit_Stage1Boss`, `isVictoryTarget=1`, 지정 보스 처치 목표, headquarters 없음 |
| 파랑 `00B0F0` | 기차 `railCells`와 사용 설정 |
| 노랑 `FFFF00` 3×3 | `Terrain_Tornado` 한 개의 고정 초기 중심. 9개 토네이도로 만들지 않는다 |
| 회색 테두리(34페이지는 `bg2` 명도 변형) | 하붑의 12칸 상대 모양. 그 안의 지형·유닛·레일도 별도로 보존 |

30페이지는 일반 적·벽·낭떠러지·약한 지반이 없다. 빈 곳에 임의로 적이나 지형을 더하지 않는다.

## 3. 기믹과 원본에 없는 값

**Phase A 당시 결정:** “현재 Core 방식대로 시작 위치 자동 결정, 나머지 배치는 그대로 제작”에 따라 하붑 모양만 보존하고 위치는 Core에 맡겼다. 이후 시작 위치 지정 기능이 추가됐고, 사용자 후속 지시로 §7에서 PPT 위치를 적용한다. 현재 정책은 §7을 따른다.

- 모래 폭풍은 이동형 `Terrain_Tornado`다. 노랑 정사각형의 중심과 3×3 크기를 보존한다. 공유 정의의 주기·이동 거리를 재사용한다.
- 내부 칸에서 시작하는 토네이도는 현재 계약상 최초 가로 방향이 필수다. 원본에 방향은 없으므로 판 중앙 쪽으로 진행하게 정한다(오른쪽 절반 -1, 왼쪽 절반 +1). 좌우 끝은 Core의 안쪽 방향과 같다.
- 낙석·기차의 발동 주기, 낙석 피해 등 원본에 없는 수치는 현재 SO authoring 기본값을 사용한다. 기차 첫 발동/반복 3/3, 낙석 대기/경고 2/2·개수 3~5·피해 20%·속박 1·모양 1칸, 하붑 이동/경고 2/1이다. 단위는 완료 진영 턴 수다.
- `timelineSeed=1`, `enemyLevel=1`, 적 회전 R0, 무작위 enemyLoadout 없음. 기존 Test 맵의 economy·unitRoster를 참조한다. 현재 BattleDefinitionAdapter는 고정 적의 키를 roster와 합치므로 새 roster나 공유 roster 변경은 필요 없다.
- `Field_13x13_P3E9` / `Field_15x15_P4E9`를 재사용한다. 아군 구역은 줄 수 대신 슬라이드의 명시적 칸 목록을 사용한다.
- 지형은 `environment.terrainPlacements`만 사용하며 sourceId는 맵 안에서 중복되지 않게 부여한다. 호환용 벽/지형 배열은 비운다. 공개 대상은 Player·Enemy다.

## 4. View 연결

기존 Test View의 배경 프리팹·카메라 설정·타일 세트를 재사용한다. 17×17 바닥 타일, 타일 크기 2×2, 배경 배율 0.75, 회전 -90°, 높이 보정을 유지한다. 실제 간격 1.5는 GridOutline 필드 간격과 같다.

anchorTile은 13×13 맵 (2,2), 15×15 맵 (1,1)로 중앙에 배치해 필드가 17×17 타일 범위를 벗어나지 않게 한다. 외곽 경계·내부 RandomTile 배치는 기존 배경과 같다. 약한 지반·낭떠러지 바닥 교체는 기존 런타임의 지형 표시를 사용한다. FieldCamera·Architecture 프리팹이나 코드는 수정하지 않는다.

## 5. 설계 점검

- **SSOT:** 규칙·유닛·지형 정의는 기존 DB를 참조한다. 배치 사실은 각 Combat SO 한 곳에 저장하며 View에 복제하지 않는다. 문서에는 추적용 요약만 둔다.
- **SOLID S/O:** 맵 데이터 추가만으로 기존 로더·표시 기능을 확장한다. 변환을 런타임 기능에 넣지 않는다.
- **SOLID L/I/D:** 기존 SO 계약과 참조 경로를 사용한다. 새 인터페이스·서비스·구현체가 없으며 추상화 계약 변경도 없다.
- **JMH/FW:** 서비스 생성·등록·Export·Builder·조회 경로와 수명은 변경하지 않는다. DB 입력을 통해 기존 조립을 사용하며 Buffer 상태를 직접 쓰지 않는다. FW-001~016의 새 등록·입력·자원 수명 변경이 없다.
- **독립성:** 한 맵의 배치 수정은 그 맵의 Combat SO에 한정한다. 일반 동작·골격·UI·다른 맵을 수정하지 않는다. 게임 내 출현 후보 등록은 맵 제작과 분리한다.
- **검증 제한:** Test 코드 작성 및 MSBuild 실행 없이 원본-에셋의 정적 대조만 한다. Unity 컴파일·GetData 검사·재생·DataBaseAssetTests는 사용자 확인으로 남긴다.

## 6. Phase B — Root/View 표 등록

- 사용자 후속 요청: “Root 에도 등록해줘”. 제작한 맵 16개를 실제 챕터 후보에 연결한다.
- `StageRootDataTable.asset`에 P20~P35를 등록한다. 기존 보스 `Stage_Test2`(10001)는 표에서 해제하고 P35(2135)로 교체해 mapKey 1의 보스 하나 조건을 유지한다. 기존 일반 Test(10000)와 비전투 스테이지는 유지한다.
- `StageViewPropDataTable.asset`에도 같은 키 16개의 짝 View를 등록하고 Test2 View(10001)는 표에서 해제한다. Root/View 동시 등록·해제는 `CombatStageSoCreation.SetRegistered`의 정책을 따른다.
- 기존 Test2 Root/Combat/View 파일과 meta는 보존한다. 신규 맵 배치·공유 정의·StageMap·Core·런타임 코드는 변경하지 않는다.
- 변경 후 Root 22개, 전투 View 17개, 일반 전투 후보 16개, 보스 P35 한 개다. 키/GUID 중복, Root/Combat/View 연결과 공유 정의의 표 등록 상태를 정적으로 확인한다.
- §5의 SSOT·SOLID·JMH/FW·독립성 점검을 유지한다. 이번 변경은 DB 후보 조립 두 곳에만 한정하며 서비스 생성·등록·수명은 바뀌지 않는다. Unity 검사·재생·DataBaseAssetTests는 사용자 확인이다.

## 7. Phase C — 하붑 시작 위치 지정

- 사용자 확인: 하붑 모양은 첨부 이미지의 **4×4에서 네 모서리를 뺀 12칸으로 고정**한다. 기존 6개 SO의 footprintOffsets는 이미 그 모양이며 그대로 유지한다.
- Core `cc4661896`에서 SO의 `fixedInitialAnchor`·`initialAnchor` → Data → Spec → 시작 배치까지 연결됐다. 이번 요청은 “넣어줘”이며 이 기존 계약에 입력 값만 넣는다.
- 대상은 `Combat_LD_P27/P28/P31/P33/P34/P35.asset`이다. `haboob.fixedInitialAnchor=1`을 넣고 아래 좌표를 `haboob.initialAnchor`에 저장한다.
- 좌표는 게임 판의 왼쪽 아래가 (0,0)이며 **모양을 감싸는 4×4 영역의 왼쪽 아래** 기준이다. 그 기준 칸 자체는 네 모서리 중 하나라 하붑 점유 칸이 아니다. 첫 회색 칸을 anchor로 사용하지 않는다.

| 페이지 | 시작 anchor (X,Y) | 판 크기 |
|---|---|---|
| P27 | (2,3) | 13×13 |
| P28 | (5,5) | 13×13 |
| P31 | (10,4) | 15×15 |
| P33 | (7,5) | 15×15 |
| P34 | (2,3) | 15×15 |
| P35 | (5,3) | 15×15 |

- 첫 위치만 PPT와 같게 지정하고, 이후 재배치는 기존 Core의 무작위 정책을 따른다. 주기·모양·다른 지형/유닛 배치는 유지한다.
- SSOT는 각 Combat SO의 시작 좌표이며 View에 위치 사본을 만들지 않는다. SOLID·JMH/FW의 기존 계약을 사용하고 서비스/조립 변경은 없다. 수정·대조 범위는 해당 6개 SO로 한정한다.
- Core·SO 타입·편집기 UI 코드는 수정하지 않는다. 모양 변경 UI 제한과 좌표 편집 UI 연결은 이번 에셋 입력 범위에 포함하지 않는다.
