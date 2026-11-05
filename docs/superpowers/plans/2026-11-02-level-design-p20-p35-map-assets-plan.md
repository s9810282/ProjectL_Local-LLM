# 레벨디자인 20~35페이지 맵 SO 제작 계획·기록

- 날짜: 2026-11-02
- 설계: [맵 SO 제작 설계](../specs/2026-11-02-level-design-p20-p35-map-assets-design.md)
- 범위: Phase A 신규 맵 16벌(48 SO + 각 meta), Phase B Root/View 표 등록과 기존 Test2 보스의 등록 교체, Phase C 하붑 6개 맵의 PPT 시작 좌표 입력.
- 상태: **Phase A·B·C 완료. 하붑 6개 맵의 PPT 시작 좌표 반영·정적 대조 완료. 컴파일 미확인, Unity 검사·재생 대기.**

## Phase A — 16개 맵 파일 제작

1. 원본 20~35페이지를 렌더링하고 도형 좌표·색·테두리를 추출한다.
2. 13×13 9개·15×15 7개의 칸 중복/누락과 범례를 확인한다.
3. 기존 필드·유닛·본부·지형·경제·View 참조를 대조한다.
4. 설계의 파일명·키로 맵 16벌과 meta를 생성한다. 기존 파일이 있으면 덮어쓰지 않는다.
5. 원본의 좌표 집합과 생성 파일을 독립적으로 다시 읽어 대조한다. 본부 점유·보스 목표·금지 지형/적/아군 구역 겹침·GUID·타일 범위도 확인한다.
6. 결과와 사용자 확인 항목을 이 문서에 기록하고 **Phase 종료 리뷰 지점에서 멈춘다**.

## Phase A 실행 기록

- 원본 16페이지 PNG 확인 완료. 도형 추출 결과 모든 페이지에서 169/225칸, 좌표 중복·누락 없음.
- 하붑 위치 자동 결정에 대한 사용자 확인 완료. 원본의 시작 위치 지정과 현행 Core의 차이는 설계 §3에 기록했다.
- 원본 식별 SHA-256: `33a400780259d55c6a034e814f389d269b65bf8941131ce9a8b3d21e429451fe`. 작업 뒤 원본 파일 해시가 같음을 확인했다.
- 신규 SO 48개와 meta 48개 생성 완료. Root/Combat/View 키·GUID 참조, 참조 대상 파일 존재, GUID 중복 없음 확인. 기존 표에는 등록하지 않았다.
- 생성 파일을 다시 읽어 16개 맵의 적·본부/보스·지형·아군 구역·레일·모래 폭풍 영역·하붑 모양을 PPTX에서 추출한 좌표 집합과 대조했다. 불일치 없음. 검토한 원본 격자는 총 3,096칸이다.
- 전체 집계: 벽 707칸, 낭떠러지 103칸, 약한 지반 94칸, 아군 구역 480칸, 일반 적 94명 + 보스 1명. 일반 맵의 2×2 본부 15개는 유닛 수에 포함하지 않는다.
- 맵 밖 배치, 고정 적/본부와 벽·낭떠러지·아군 구역의 겹침, 중복 sourceId 없음. 17×17 바닥 범위 안에 각 필드가 들어가며 간격은 1.5로 일치한다.
- Test 코드 작성·MSBuild·Unity 실행 없음. 이 결과는 직렬화 값의 정적 대조이며 SO GetData·게임 동작 성공의 증거가 아니다. **컴파일 미확인.**
- Phase A 완료로 리뷰 지점에서 멈춘다. 이번 작업에 챕터 출현 후보 등록·기존 보스 교체는 포함하지 않았다.

## Phase B — Root/View 표 등록

- 요청: “Root 에도 등록해줘”. 설계 §6을 추가하고 등록을 진행한다.
- B1: 현재 Root/View 등록 키와 챕터 보스 확인. 기존 보스는 Test2(10001), 새 보스는 P35(2135)다.
- B2: Root/View 두 표에 P20~P35를 등록하고 Test2 등록만 해제한다. 기존 일반 Test와 비전투 후보, Test2 에셋 파일은 유지한다.
- B3: GUID·키 중복, 두 표의 전투 키 대응, 보스 한 개, 공유 DB 참조와 예상 항목 수를 정적으로 확인한다.
- B4: 결과를 기록하고 Phase 완료 리뷰 지점에서 멈춘다. 컴파일·재생·DataBaseAssetTests는 Unity에서 사용자 확인이다.

### Phase B 실행 기록

- 현재 등록 상태 확인 완료. Root 7개, View 2개이며 챕터 1 보스는 Test2 하나다.
- Root/View 표에 P20~P35의 16쌍 등록 완료. 기존 보스 Test2(10001)의 Root/View 등록을 P35(2135)로 교체했고, Test2 Root/Combat/View 에셋과 meta는 보존했다.
- Root 22개·View 17개, 일반 전투 후보 16개·보스 P35 하나를 확인했다. 두 표의 GUID·키 중복 없음, 신규 16쌍의 키/Combat 참조 일치, 필드·로스터·경제·적 유닛·본부의 공유 표 등록을 확인했다.
- InGameGroup → StageRootDataTable, ViewGroup → StageViewPropDataTable 참조가 연결돼 있음을 확인했다. 기존 비전투 후보와 일반 Test는 유지했다.
- 두 표의 diff와 공백 검사를 통과했다. 신규 맵 배치·공유 정의·StageMap·Core·런타임·시험 코드는 변경하지 않았다. **컴파일 미확인.** Unity 재생·DataBaseAssetTests는 미실행이다.
- Phase B 완료로 리뷰 지점에서 멈춘다.

## Phase C — 하붑 시작 위치 지정

- 요청: Core 지원 확인 후 “넣어줘”. 설계 §7의 고정 12칸 모양과 시작 anchor를 적용한다.
- C1: 최신 PPT의 해당 6페이지에서 회색 테두리 12칸을 다시 읽고 기존 모양·시작 anchor를 확인한다.
- C2: 해당 6개 Combat SO에 `fixedInitialAnchor=1`과 `initialAnchor`를 추가한다. 다른 값·meta·표·코드는 변경하지 않는다.
- C3: 생성 결과를 다시 읽고 anchor + 12칸 상대 모양이 PPT의 회색 영역과 정확히 같은지, 전체가 판 안인지, 변경 범위가 두 필드뿐인지 확인한다. 결과를 기록하고 Phase 리뷰 지점에서 멈춘다.

### Phase C 실행 기록

- 현재 PPT SHA-256: `bdf874b79e54bb73ad70fe53cdb224f42a5a8e8fbfc45af48a54806725451c85`. Phase A 이후 파일이 갱신돼 최신 원본을 다시 읽었다. 해당 6페이지의 모양·anchor는 이전 추출과 일치한다.
- 6개 Combat SO에 `fixedInitialAnchor=1`과 `initialAnchor` 입력 완료: P27 (2,3), P28 (5,5), P31 (10,4), P33 (7,5), P34 (2,3), P35 (5,3). 좌표는 0부터 시작하는 판 좌하단 기준이며 anchor는 4×4 경계의 비어 있는 좌하단 모서리다.
- 저장된 SO를 다시 읽어 사용자 이미지의 고정 12칸 모양, anchor를 더한 전체 좌표와 최신 PPT 회색 영역의 일치, 판 범위 내 배치를 확인했다. 6개 모두 일치한다. 시작 위치만 고정하며 이후 재배치는 기존 Core 규칙을 따른다.
- 각 SO의 변경은 두 필드 4줄씩, 총 24줄 추가뿐이다. 나머지 신규 맵 SO·meta 90개의 해시는 변경 전과 같다. 범위 내 diff 공백 검사 통과. Core·편집기·표는 변경하지 않았다.
- Test 코드 작성·MSBuild·Unity 실행 없음. **컴파일 미확인.** Unity 재생과 `DataBaseAssetTests`는 사용자 확인 항목이다. Phase C 완료로 리뷰 지점에서 멈춘다.

## 생성 맵 목록

각 링크는 Stage Root다. 같은 `LD_P번호`의 Combat SO는 `InGameGroup/Stage/Combat`, View SO는 `ViewGroup/StageViewProp/Item`에 있다. 일반 맵은 본부 파괴 목표, P35는 지정 보스 처치 목표다.

| 페이지 | StageKey | 크기 | 일반 적 / 보스 | 아군 칸 | 벽 / 낭떠러지 / 약한 지반 | 기믹 |
|---|---:|---|---|---:|---|---|
| [P20](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P20.asset) | 1120 | 13×13 | 4 / 0 | 29 | 31 / 5 / 12 | 없음 |
| [P21](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P21.asset) | 1121 | 13×13 | 8 / 0 | 24 | 25 / 6 / 6 | 기차 |
| [P22](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P22.asset) | 1122 | 13×13 | 6 / 0 | 28 | 47 / 2 / 4 | 기차 |
| [P23](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P23.asset) | 1123 | 13×13 | 7 / 0 | 20 | 43 / 2 / 4 | 모래 폭풍 |
| [P24](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P24.asset) | 1124 | 13×13 | 5 / 0 | 27 | 44 / 10 / 0 | 모래 폭풍 |
| [P25](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P25.asset) | 1125 | 13×13 | 7 / 0 | 31 | 31 / 4 / 1 | 낙석 |
| [P26](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P26.asset) | 1126 | 13×13 | 5 / 0 | 15 | 71 / 8 / 6 | 낙석 |
| [P27](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P27.asset) | 1127 | 13×13 | 6 / 0 | 27 | 51 / 3 / 16 | 하붑 |
| [P28](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P28.asset) | 1128 | 13×13 | 6 / 0 | 26 | 46 / 2 / 5 | 하붑 |
| [P29](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P29.asset) | 1129 | 15×15 | 7 / 0 | 27 | 85 / 0 / 0 | 기차, 모래 폭풍 |
| [P30](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P30.asset) | 1130 | 15×15 | 0 / 0 | 45 | 0 / 0 / 0 | 모래 폭풍, 낙석 |
| [P31](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P31.asset) | 1131 | 15×15 | 6 / 0 | 33 | 42 / 15 / 8 | 모래 폭풍, 하붑 |
| [P32](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P32.asset) | 1132 | 15×15 | 7 / 0 | 34 | 37 / 10 / 10 | 기차, 낙석 |
| [P33](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P33.asset) | 1133 | 15×15 | 7 / 0 | 31 | 76 / 6 / 10 | 기차, 하붑 |
| [P34](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P34.asset) | 1134 | 15×15 | 7 / 0 | 45 | 28 / 24 / 0 | 낙석, 하붑 |
| [P35](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Root/Stage_LD_P35.asset) | 2135 | 15×15 | 6 / 1 | 38 | 50 / 6 / 12 | 기차, 낙석, 하붑 |

## Unity에서 확인할 항목

- SO 편집기에서 `LD_P20`~`LD_P35`를 열고 2D 배치와 원본 슬라이드를 비교한다.
- 3D 배경·카메라·바닥 범위, 약한 지반·낭떠러지·벽의 외형과 적 표시를 확인한다.
- 편집기의 검사/GetData 검증과 `DataBaseAssetTests`를 실행한다. 전체 DB 검사와 맵별 배치 검사를 함께 확인한다.
- 재생 진입 시 원본에 일반 적이 없는 P30, 단일 지정 보스 P35, 각 기믹의 단독·조합을 확인한다. 하붑 6개 맵은 PPT의 고정 시작 위치와 12칸 모양, 이후 무작위 재배치를 확인한다. **현재 PlacedStage는 표에 등록된 Regular만 지원**하므로 일반 맵은 PlacedStage에서 선택하고, P35는 일반 챕터 보스 진입 경로로 확인한다.
- Phase B 등록 뒤 새 Run에서 일반 전투 후보와 P35 보스 진입을 확인한다. 기존 진행 중 Run 또는 에디터 개발 설정이 Test2 키를 고정했다면 새 등록 상태에 맞게 다시 시작·선택한다.
