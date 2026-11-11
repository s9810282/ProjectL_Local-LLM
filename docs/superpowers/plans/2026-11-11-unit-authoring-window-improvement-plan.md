# 유닛 통합 에디터 개선 Task 계획 (UA-a~UA-d)

- 설계: [unit-authoring-window-improvement-design](../specs/2026-11-11-unit-authoring-window-improvement-design.md) (UA1~UA9, Q-UA1~Q-UA6 A — 2026-11-11 사용자)
- 작성: 2026-11-11, 코드 착수 전
- 규칙: 새 시험 없음(View·에디터 도구), Phase 끝마다 멈추고 보고, 컴파일·Unity 확인은 사용자, 커밋·push는 사용자.
- 대상 파일: `Assets/ProjectL/Script/Tool/Editor/` 의 `UnitAuthoring*.cs` 다섯 + 새 둘. 게임 코드·SO 코드 변경 없음.

## 0. 계획에서 정한 것 (사용자 확인 대상)
- **P1** 카탈로그 다시 읽기 트리거에 `EditorApplication.projectChanged`(0.5초 디바운스)를 쓴다 — 바깥에서 에셋을 바꿔도 목록이 따라옴.
- **P2** 요청 기록 `version = 2`. 옛 기록(version 0)은 `rosters` 빈 배열로 읽고, 진행 중 옛 요청은 로스터 없이 끝까지 진행한다.
- **P3** 로스터 SO 추가 위치는 `unitDefinitions` 배열 끝, 빼기는 그 참조인 칸만(다른 칸 순서 유지).
- **P4** "쓰는 곳"(UA9)·로스터 빼기 경고(Q-UA2)는 스테이지·로드아웃 SO를 직렬화 필드 이름으로 읽는다(`unitRoster`·`enemyLoadout`·증원 풀·`unitDefinitions` 등). 필드 이름이 바뀌면 경고 한 줄 뒤 건너뜀(도구가 멈추지 않음).
- **P5** 짝 Visual 만들기(Q-UA5)는 고른 Visual을 복제해 키·표시 이름만 바꾸고, enum 추가 없이 Visual 테이블에 등록(생성 요청 상태 기계를 쓰지 않는 즉시 작업, Undo 없음·실패 시 만든 파일 삭제).

## UA-a 카탈로그·오류 경계 (UA1·UA6·UA8)
- a.1 새 `UnitAuthoringCatalog.cs`: 행(Unit·Visual·키·키 이름·표시 이름·진영·테이블 등록 둘·로스터 목록·문제 목록), 테이블·로스터·로스터 테이블 목록, `EntityKey` 소스 파싱 보관. 행마다 try.
- a.2 창이 `_units`·`_unitTables`·`_visualTables`·`_unusedKeys` 대신 카탈로그를 읽음. `RefreshCatalog`는 카탈로그 다시 만들기 + 고른 상태 다시 맞추기만.
- a.3 OnGUI 전체 `try/finally`(스크롤·가로 묶음 닫기, `ExitGUIException` 다시 던짐), `DrawEditing`의 매 프레임 `Key()` → 행 값.
- a.4 `OnInspectorUpdate` 완료 열기 한 번만(실패도 기록), 고르기 두 곳 지역 변수 → 성공 뒤 한꺼번에.
- a.5 `CheckTable` 문구: 테이블 형식에서 기대 형식, "n번 칸 비었음/형식 다름".
- a.6 문제 있는 행: 원본 목록에서 회색 + 이유, 편집은 허용.
- 확인(사용자): 창 열기 속도, MainCharacter·Shielder 이유 표시, 편집 중 SO 삭제해도 레이아웃 오류 없음.

## UA-b 목록·상세 화면 (UA2·UA3)
- b.1 좌우 분할(끌어 바꾸는 경계, 너비 EditorPrefs), 왼쪽 검색·필터(아군/적/전체·문제만)·행(표시 이름·키·상태 아이콘·아이콘 이미지).
- b.2 오른쪽 고정 머리줄([검증·저장] [되돌리기] [Unit 선택] [Visual 선택] `*`), 아래만 스크롤. 생성은 머리줄 [새 유닛] → 오른쪽이 생성 화면.
- b.3 외형 묶음 넷(기본·연출·소리·큐), 큐 접힘, 장애물이 아니면 장애물 큐 숨김 + 값 있으면 경고.
- b.4 미저장 대화상자(다른 유닛 고르기·창 닫기·새 유닛), 되돌리기 = `ImportAsset(ForceUpdate)` 뒤 SerializedObject 다시.
- b.5 배너 출처 표시, "등록 상태 확인" 버튼 삭제(행에서 표시).
- b.6 짝 없는 유닛: 규칙만 편집 + [짝 Visual 만들기](P5).

## UA-c UnitRoster (UA4)
- c.1 새 `UnitAuthoringRosters.cs`: 찾기·포함·추가·빼기·이 키를 쓰는 EnemyLoadout/증원(로스터를 쓰는 스테이지 기준) 찾기.
- c.2 기록: `version`·`rosters`·`rosterRegistered`, 옛 기록 읽기(P2).
- c.3 생성 화면 로스터 체크 목록(이름·키·쓰는 스테이지 수), 기본값 Q-UA1 A, 아군 전용 안내.
- c.4 `ValidateRequest`에 로스터 검사(형식·`ValidateData`·로스터 테이블 등록·같은 Catalog), `Finish` 마지막에 로스터마다 등록 + `Save()`.
- c.5 `Cleanup` 맨 앞에 로스터 빼기 → 저장, 그다음 테이블·에셋.
- c.6 편집 화면 "UnitRoster" 묶음 체크(Undo), 저장은 바뀐 로스터 SO 함께 검증·저장, 빼기 경고(Q-UA2 A).
- 확인(사용자): 새 적 생성 → `UnitRoster_1`에 들어감 → EnemyLoadout에 넣어 전투 진입, 취소 시 로스터에서 빠짐, 편집 탭 넣기/빼기·Undo.

## UA-d 생성 화면·요청 견고성·바로가기 (UA5·UA7·UA9)
- d.1 키 이름·번호 즉시 검사(보관한 소스), Trim 뒤 경로, enum 토글 되켜기 다시 채우기, 빈 번호 제안은 보관 값으로.
- d.2 생성 칸 설명·진영 자격(Q-UA3 A), 공유 참조 접힌 목록.
- d.3 원본 지문: 대기 중 바뀜 → [바뀐 원본으로 계속] [취소], 미저장 원본 → [원본 저장 후 생성].
- d.4 컴파일 대기: 시작 콜백 없음 120초 하나만, 진행 중 컴파일은 시간으로 안 끊음.
- d.5 재시작 뒤 멈춤 안내·[재개], 기록 손상 [기록 파일 열기] [기록 버리기], `resultOpened` 기록 이동, 끝난 요청 [닫기].
- d.6 "쓰는 곳"(EnemyLoadout·로드아웃·초기 배치·증원, 읽기·Ping).
- d.7 기획자용 사용법 한 쪽 `Tool/유닛 통합 에디터 사용법.md`.

## 실행 기록

### UA-a~UA-d 코드 완료 (2026-11-11, 사용자 "UA-d 까지 쭉 진행해")
- P1~P5 그대로. 사용자 지시로 Phase 사이에 멈추지 않고 이어서 함.
- 검증: 도구 어셈블리만 MSBuild(새 파일 둘을 그 프로젝트에만 넣은 스크래치 빌드) — 오류 0, 바꾼 파일 경고 0. **Unity 컴파일·창 동작 미확인**, 새 시험 없음.
- 새 파일: `UnitAuthoringCatalog.cs`·`UnitAuthoringRosters.cs`(+ `.meta`, Unity 기본 형식으로 만듦), `Tool/유닛 통합 에디터 사용법.md`.
- 바뀐 파일: `UnitAuthoringWindow.cs`·`UnitAuthoringWindow.Editing.cs`(다시 씀), `UnitAuthoringOperation.cs`·`UnitAuthoringAssets.cs`·`UnitAuthoringPendingStore.cs`.
- 계획과 다른 점
  - 창 닫기 때 미저장 확인은 대화상자 대신 Unity의 `hasUnsavedChanges`/`SaveChanges`/`DiscardChanges`(같은 효과, 닫기 취소도 됨).
  - 되돌리기는 `ImportAsset` 대신 디스크 판을 `InternalEditorUtility.LoadSerializedFileAndForget`로 읽어 `CopySerialized`(메모리 판을 확실히 덮음, Undo 기록).
  - 원본 목록에서 문제 있는 유닛은 회색 항목 대신 목록에서 빼고 "원본으로 쓸 수 없는 유닛 n개" 접힌 칸에 이유를 둠(팝업은 항목을 끌 수 없어서).
  - 컴파일 대기 상한: 시작 신호 없음 120초 + 재로드 확인 120초(설계는 하나라고 적음).
- Unity 확인(사용자)
  1. 창 열기·목록(아군/적·검색·문제만), MainCharacter·Shielder에 느낌표와 이유.
  2. 편집: 값 바꾸기 → `*` → 저장/되돌리기, 다른 유닛 고를 때·창 닫을 때 확인 창.
  3. 로스터 넣기/빼기·Undo·저장, EnemyLoadout에 쓰는 유닛(예: Demon) 빼기 경고.
  4. 새 적 생성(enum 추가) → 재컴파일 뒤 Unit·Visual 테이블·`UnitRoster_1`에 들어감 → 취소하면 셋 다 빠짐.
  5. 생성 화면 키 입력 중 경고, 원본 미저장 경고·[원본 저장].
  6. "쓰는 곳" 목록·[찾기].
  7. 끝난 요청 [닫기], 창을 다시 열어도 편집 탭으로 튀지 않음.

### 컴파일 중 기록 잠금 수정 (2026-11-11)
- 사용자 보고 "바꿀 파일을 제거할 수 없습니다가 자꾸 뜸, 재검사하면 됨". 원인: `assemblyCompilationFinished`가 어셈블리마다 `request.json`을 다시 써 잠금에 걸리고, 저장 실패를 요청 실패로 처리. `EntityKey.cs`도 연 채로 `File.Replace`.
- 고침: 어셈블리 콜백은 오류 처음만 기록, 컴파일 중 저장 실패는 미뤘다 다시 씀(재로드 직전 한 번 더), 도메인이 바뀌었으면 컴파일 끝으로 봄, 교체는 `ReplaceFile`(재시도 5번 → 덮어쓰기), `EntityKey.cs`는 비교 뒤 닫고 교체. 커밋 `dc1f84af6`에 포함.

### 유닛 삭제 (2026-11-11, 사용자 "유닛 삭제 기능도 넣어줘", Q-UA6 바뀜)
- 사용자 결정: 쓰는 곳을 경고창으로 보인 뒤 참조도 함께 뺌, enum은 체크했을 때만 지움.
- 새 `UnitAuthoringDeletion.cs`(+ `.meta`): `Prepare`(테이블·참조 칸·GUID로 찾은 다른 파일·enum 사용처, 아무것도 안 바꿈) → `Execute`(참조 칸 빼기 → 테이블 빼기 → SO 휴지통 → 선택 enum 제거·재컴파일). `UnitEntityKeySource.RemovedBytes`. 편집 화면 맨 아래 [유닛 삭제…]·체크.
- 도구 어셈블리 MSBuild 오류 0, Unity 미확인. 확인: 시험 유닛을 만들어 EnemyLoadout·로스터에 넣은 뒤 삭제 → 경고창 내용, 참조·테이블에서 빠짐, 휴지통, enum 체크 시 줄 삭제·재컴파일.
