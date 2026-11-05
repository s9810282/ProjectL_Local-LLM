# ViewStage 테이블 Task 계획

## Phase 1 — DB 조립 전환

1. `ViewStageDataSO`·테이블·조회 계약을 추가한다.
2. 기존 StageViewProp 에셋을 ViewStage 에셋으로 이전하고 ViewGroup에서 옛 환경 표를 제외한다.
3. Factory가 ViewStage를 한 번 조립하고 기존 소비자에는 호환 조회를 제공한다.
4. DB 에셋·EditMode·Unity 컴파일을 확인한다.

Phase 1 종료 뒤 Unity에서 Catalog 조립과 Combat 진입을 확인한 뒤, 필요하면 Phase 2에서 소비자 API를 `IViewStageTableQuery`로 직접 전환한다.

## Phase 2 — 맵 저장과 ViewStage 카탈로그 연결

1. 맵 작업실에 명시적 `StageKey` 입력과 현재 카탈로그 항목 표시를 추가한다.
2. 프리팹 저장 경로를 맵 패키지 기준으로 유지한다. 프리팹, `MapDefinitionSO`, 베이크 라이트 폴더는 같은 경로에 저장한다.
3. 저장 성공 뒤 `DataBaseSO/ViewGroup`의 활성 `ViewStageDataSO`를 StageKey로 생성 또는 갱신하고 `ViewStageDataTableSO`에 한 번만 등록한다.
4. 갱신은 맵 참조만 바꾸고 기존 프랍 보정·환경 회전은 보존한다. 새 항목은 기본값으로 만든다.
5. Unity에서 새 저장, 같은 StageKey 재저장, 중복/미등록 데이터 오류, Catalog 조립을 확인한다.

Phase 2는 Phase 1의 Unity 검증 완료 뒤 시작한다. 소비자 API 직접 전환은 별도 Phase로 유지한다.
