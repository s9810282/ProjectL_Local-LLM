# ViewStage 테이블 설계

## 목적

전투 스테이지 키 하나로 프랍과 환경 외형을 함께 조회한다. View는 Core 스테이지 규칙 SO를 읽지 않는다.

## SSOT

`ViewStageDataSO`가 스테이지별 View 사실의 유일한 활성 원본이다.

- 프랍: 맵 프리팹 또는 `MapDefinitionSO`, 위치·회전·크기 보정
- 환경: Terrain/Object 원본 ID별 회전

`StageFieldView`, Unit/Object/Terrain Visual은 스테이지 공용 설정이므로 이 표로 옮기지 않는다.

## 계약

`IViewStageTableQuery.GetViewStage(stageKey)`는 `ViewStageData`를 반환한다. 값에는 `Prop`과 `Environment`가 함께 있다. 누락·중복 키는 DB 조립 시 실패한다.

기존 `IStageViewPropTableQuery`와 `IStageEnvironmentVisualTableQuery`는 Phase 1 호환 창구이며, 같은 `ViewStageTable` 결과를 투영한다. 따라서 활성 DB 원본은 중복되지 않는다.

## 맵 패키지와 카탈로그 에셋의 저장 경계

맵 작업실에서 사용자가 고른 프리팹 저장 경로는 맵 패키지의 기준 경로다. 해당 경로에는 다음 자산을 함께 둔다.

- `<Map>.prefab`: 배치 결과
- `<Map>_Map.asset`: 프리팹과 베이크 조명을 조립하는 `MapDefinitionSO`
- `<Map>_Lighting/Bake_*/`: Unity가 생성한 라이트 텍스처, 베이크 씬, `BakedLighting.asset`

카탈로그에 등록하는 `ViewStageDataSO`는 맵 패키지에 두지 않는다. `Assets/ProjectL/DataBaseSO/ViewGroup/` 아래의 활성 ViewStage 항목 폴더에 보관하고, `ViewStageDataTableSO`만 이 에셋을 참조한다. 맵 패키지는 재사용 가능한 제작 산출물이고, `StageKey`와 맵 패키지를 연결하는 것은 DB 카탈로그의 책임이기 때문이다.

맵 작업실은 명시적으로 입력한 `StageKey`를 사용해 카탈로그 항목을 생성 또는 갱신한다. 갱신 시에는 맵 참조만 바꾸며, 이미 입력한 프랍 보정과 Terrain/Object 회전은 보존한다. 새 항목만 기본 보정값과 빈 회전 목록으로 만든다. 중복 StageKey 또는 표에 없는 기존 항목은 저장을 중단하고 오류를 보인다.
