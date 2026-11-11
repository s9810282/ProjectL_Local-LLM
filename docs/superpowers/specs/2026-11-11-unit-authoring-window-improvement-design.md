# 유닛 통합 에디터 개선 설계 (UA1~UA9)

- 작성: 2026-11-11, 설계 초안(코드 변경 없음)
- 대상: `Tool/Editor/UnitAuthoring*.cs`·`UnitEntityKeySource.cs`(용근 `7c817b25`, 우리 도구 — 다른 작업자 전달 없음)
- 요청: 사용자 "유닛 통합 에디터 불편 사항 파악" → "정상 작동하지 않을 때 뜨는 오류 파악" → "유닛 추가 시 UnitRoster에도 등록까지 포함해서 설계"
- 검증: 정적 코드 대조만(Unity 재현 없음). 컴파일·재생 확인은 구현 뒤 사용자.

## 1. 지금 상태 (사실)

### 1.1 구조
| 파일 | 줄 | 책임 |
|---|---|---|
| `UnitAuthoringWindow.cs` | 278 | 창·탭, 생성 화면, 목록 새로고침 |
| `UnitAuthoringWindow.Editing.cs` | 126 | 편집 화면(SerializedObject 두 개 날것 그리기), 검증·저장 |
| `UnitAuthoringOperation.cs` | 307 | 생성 요청 상태 기계(enum 추가 → 재컴파일 대기 → 복제 → 테이블 등록), 취소 |
| `UnitAuthoringAssets.cs` | 330 | 형식 이름 판별, 키 읽기, 짝 찾기, 검증, 등록·삭제 |
| `UnitAuthoringPendingStore.cs` | 73 | `Library/ProjectLUnitAuthoring/request.json` 보관 |
| `UnitEntityKeySource.cs` | — | `EntityKey.cs` 파싱·항목 추가(백업·원자 교체) |

### 1.2 데이터 (2026-11-11 기준)
- Unit SO 14개 / Visual SO 17개(구조물 3 포함), 키 짝은 모두 1:1.
- `UnitDataTable` 12개 등록 — `Unit_MainCharacter`·`Unit_Shielder` 빠짐. `UnitVisualDataTable` 16개.
- **UnitRoster:** `UnitRoster_1` 하나(`UnitRosterDataTable` 한 줄), 전투 스테이지 21개가 모두 이것을 참조. 들어 있는 유닛 8개(ShotGun·Demon·Gravekeeper·CactusCannon·UndeadCannonCart·Cowboy·Ballista·Akimbo). Sniper·Stage1Boss·UndeadHeavy·DynamiteHammer·Shielder는 없다.
- 로스터 규칙(Core `BattleDefinitionAdapter.cs:41-65`): EnemyLoadout 후보·Nexus 증원 풀 유닛은 **스테이지 로스터에 있어야** 하고, 없으면 전투 진입 예외. 아군 소유 카드·직접 배치 적은 로스터 밖이어도 된다. `UnitRosterData.Validate`는 빈 칸·중복을 거절한다.
- 즉 지금 도구로 새 적을 만들어 EnemyLoadout에 넣으면, 로스터 등록을 손으로 하지 않는 한 전투 진입이 멈춘다 — 이번 UnitRoster 범위의 동기.

### 1.3 찾은 문제 (11-10 보고 요약)
- 불편 A~E: 검색 없음·경로 붙은 긴 목록(A1), 표시 이름·아이콘 없음(A2), 짝 없는 유닛 편집 불가(A3), 저장 버튼 스크롤 이탈(B4), 날것 필드·안 쓰는 장애물 큐 칸(B5), 미저장 경고·되돌리기 없음(B6), 등록 상태 수동 갱신(B7), 키 오류 사후 표시·Trim 전 경로(C8), enum 토글 되켜기 충돌(C9), 공유 참조 목록 없음(C10), 생성 때 값 하나(C11), 출현 연결 안내만(C12), 매번 전체 스캔·`Key()`마다 `SerializedObject`(D13), "빈 번호 제안"이 전체 새로고침(D14), 오류 배너 출처 불명(E15), 삭제·해제 없음(E16).
- 오류 1~4:
  - E-D1 `Unit_MainCharacter`: `PlayerMainDefinitionPolicy` "퇴역한 메인 키…"로 원본·저장 불가.
  - E-D2 `UnitVisual_Shielder`: 프리팹 비어 "Prefab is required"로 원본·저장 불가.
  - E-R1 완료 결과 에셋이 사라지면 `OnInspectorUpdate`가 0.1초마다 실패 + 전체 새로고침.
  - E-R2 `DrawEditing`이 `Run` 밖에서 `Key()` → 예외 시 레이아웃 그룹 오류 연쇄.
  - E-R3 `RefreshCatalog`가 중간 예외로 멈춤, E-R4 짝 실패 뒤 반쯤 고른 상태.
  - E-C1 원본 미저장·대기 중 원본 저장 → 재개 불가, E-C2 30초 고정 대기, E-C3 Unity 재시작 뒤 자동 진행 멈춤, E-C4 기록 파일 손상 시 영구 차단, E-C5 테이블 null 칸 문구 오류.
  - E-M1 창 다시 열면 편집 탭으로 튐, E-M2 끝난 요청 영구 표시.

## 2. 목표와 범위
- 새 유닛을 만들면 **Unit 테이블·Visual 테이블·고른 UnitRoster(들)**까지 한 요청으로 등록하고, 실패·취소 때 셋 모두 되돌린다.
- 편집 탭에서 그 유닛이 어느 로스터에 있는지 보고 넣고 뺄 수 있다.
- 위 오류가 창을 깨뜨리거나 반복되지 않게 하고, 데이터 문제는 목록에서 미리 보인다.
- 범위 밖: EnemyLoadout·증원 풀·스테이지 배치 편집(C12는 바로가기만), 유닛 삭제(E16, Q-UA6), E-D1·E-D2 데이터 자체(값은 담당자 결정 — 도구는 표시만).

## 3. 설계

### UA1 카탈로그 한 번 읽기 (D13·D14·E-R3)
- 새 `UnitAuthoringCatalog`(순수 보관 클래스): Unit·Visual·세 테이블(Unit·Visual·Roster)·로스터 SO를 한 번 훑어 **행 목록**을 만든다. 행 = Unit SO, 짝 Visual(없으면 null), 키, 키 이름, 표시 이름, 진영 자격, 등록된 테이블 둘 여부, 들어 있는 로스터 목록, 문제 목록(짝 없음·중복 키·`Validate` 실패 문구·테이블 미등록).
- 키 읽기는 SerializedObject 대신 각 SO의 `GetData()`가 아니라 직렬화 한 번(`using var so`) — 행을 만들 때 한 번만.
- 다시 읽는 때: 창 열기, "목록 새로고침", 생성·취소·저장·로스터 변경 뒤, `EditorApplication.projectChanged`(디바운스 0.5초). OnGUI·OnInspectorUpdate는 행만 읽는다.
- 새로고침은 행마다 try로 감싸 한 행의 예외가 전체를 멈추지 않는다(문제 목록에 적음).

### UA2 왼쪽 목록 + 오른쪽 상세 (A1~A3·B4·E15)
- 창을 좌우로 나눈다(SO 모드와 같은 끌어 바꾸는 경계). 왼쪽: 검색창(키 이름·표시 이름·파일 이름), 필터(아군/적/전체, "문제 있음만"), 행 = 표시 이름 + 키 이름(번호) + 상태 아이콘(경고·미등록·로스터 없음). 아이콘(`infoIcon`)이 있으면 작게.
- 오른쪽 위 고정 머리줄: 고른 유닛 이름, `*`(미저장), [검증·저장] [되돌리기] [Unit 선택] [Visual 선택]. 아래만 스크롤.
- 오류 배너는 출처를 붙인다("생성:" "저장:" "목록:"), 다음 동작에서 같은 출처만 지운다.
- 짝 없는 유닛도 고를 수 있다: 규칙 쪽만 편집하고 외형 쪽에는 "짝 Visual 없음 — [짝 Visual 만들기]"(Q-UA5).

### UA3 편집 화면 묶음 (B5·B6·B7)
- 규칙: SO의 Header 그대로 그린다(지금처럼 PropertyField, `key` 숨김) — Header가 있어 충분.
- 외형: 묶음 머리 넷 — 기본(프리팹·표시 이름·설명·아이콘 둘), 연출(공격·반응·클립), 소리(프리셋 둘), 큐(장애물·구조물). 큐 묶음은 접힌 채 시작, 장애물 키가 아니면 장애물 큐 칸은 숨기고 값이 들어 있으면 경고(Validate와 같은 조건을 `ObstacleContentProfile.IsNativeObstacleKey`로).
- 미저장: 다른 유닛을 고르거나 창을 닫을 때 `*`면 "저장 / 버리기 / 취소" 대화상자. 되돌리기 = 디스크 판으로 다시 읽기(`Resources.UnloadAsset`가 아니라 `AssetDatabase.ImportAsset(ForceUpdate)` 뒤 SerializedObject 다시 만듦).
- 등록 상태(테이블 둘·로스터)는 카탈로그 행에서 바로 보인다(버튼 삭제).

### UA4 UnitRoster 등록 (이번 요청의 중심)
- **생성 화면:** "등록할 UnitRoster" 다중 선택(체크 목록, 로스터 SO 전부 — 이름·키·쓰는 스테이지 수). 기본값(Q-UA1): 원본 유닛이 든 로스터 전부를 체크. 원본이 아군 전용(`PlayerOnly`)이면 기본 체크 없음 + 안내("아군은 로스터 없이도 전투에 나옵니다. EnemyLoadout·증원에 쓸 때만 필요").
- **요청 기록:** `UnitAuthoringRequest`에 `rosters`(GUID 배열)·`rosterRegistered`(같은 길이 bool 배열). 기록 형식이 바뀌므로 `version` 칸을 더하고, 옛 기록(version 없음)은 로스터 없음으로 읽는다.
- **순서:** 지금 Unit 테이블 → Visual 테이블 다음에 로스터마다 하나씩 등록하고 그때마다 `Save()`(지금 등록과 같은 "실제 재확인 → 이미 있으면 건너뜀" 규칙). 로스터 등록은 `unitDefinitions` 배열 끝에 Unit SO 참조 추가(`UnitRosterDataSO`는 Unit SO를 끌어 놓는 형식).
- **검사(요청 시작·재개):** 로스터마다 `UnitRosterDataSO` 형식, 빈 칸·중복 없음(= `UnitRosterData.Validate`를 지금 `ValidateData`로), 로스터 테이블에 등록됨, 같은 Catalog에 Unit 테이블과 함께 있음. 완료 기록이 있는데 바깥에서 빠졌으면 지금처럼 실패.
- **취소·실패 복구:** `Cleanup`이 로스터마다 그 Unit 참조만 빼고(뒤에서부터, 다른 칸 순서 그대로) 저장한 뒤 테이블 둘·에셋을 지운다. 로스터 정리를 맨 앞에 둔다(에셋을 먼저 지우면 로스터에 Missing 참조가 남음).
- **편집 화면:** "UnitRoster" 묶음 — 로스터마다 체크(넣기/빼기 즉시, Undo 지원, 저장은 [검증·저장]이 로스터 SO도 함께). 빼려는 로스터를 쓰는 스테이지의 EnemyLoadout·증원 풀이 이 유닛을 쓰면 경고 + 확인 대화상자(Core 규칙 사본은 두지 않고, 스테이지 SO의 `unitRoster`·`enemyLoadout`·증원 칸을 직렬화로 읽어 "이 유닛 키가 있나"만 본다).
- 로스터 쪽 코드는 새 `UnitAuthoringRosters`(정적, 찾기·포함·추가·빼기·사용처) 하나에 둔다 — `UnitAuthoringAssets`는 이미 330줄.

### UA5 생성 화면 다듬기 (C8~C11)
- 키 이름·번호는 입력마다 `UnitEntityKeySource.Validate`를 미리 돌려(소스 파싱은 카탈로그 새로고침 때 한 번 보관) 칸 아래에 바로 오류 표시, 경로 미리보기는 Trim 뒤 이름.
- enum 추가 토글을 켜면 키 이름을 "Unit_"·번호를 빈 번호 제안으로 다시 채운다(C9).
- "빈 번호 제안"은 보관한 소스·카탈로그로만 계산(D14).
- 생성 때 정할 값: 표시 이름(지금), 설명, 진영 자격(Unit SO `ownerEligibility`) — 셋만(Q-UA3). 나머지는 생성 뒤 편집 탭.
- "공유되는 참조" 접힌 목록: 원본 Visual의 프리팹·연출 SO 셋, Unit의 지정 방식·특성 — 생성 뒤 바꿔야 할 후보로 보여만 준다.
- 원본 목록은 카탈로그 행 중 문제 없는 것만 고를 수 있고, 문제 있는 행은 회색 + 이유(E-D1·E-D2가 여기서 보임).

### UA6 오류 경계 (E-R1·E-R2·E-R4·E-C5)
- OnGUI 본문 전체를 `try/finally`로 감싸 스크롤·레이아웃 그룹을 늘 닫고, 예외는 배너로(`ExitGUIException`은 다시 던짐). GUI 안에서 에셋을 읽는 곳은 카탈로그 행만 보게 해 매 프레임 `Key()` 호출을 없앤다.
- `OnInspectorUpdate`의 완료 열기: 시도하면 성공·실패와 상관없이 `openedResult`를 기록(한 번만), 실패는 배너 한 줄.
- 고르기(SelectTemplate·SetEditing)는 새 상태를 지역 변수로 만든 뒤 성공하면 한꺼번에 바꾼다(반쯤 고른 상태 없음).
- 테이블 검사: 빈 칸·형식 다름은 "`<테이블>` <n>번 칸이 비었습니다 / <형식>이 아닙니다"로(지금 `Key()` 형식 추측 대신 테이블 형식에서 기대 형식을 정함).

### UA7 생성 요청 견고성 (E-C1~E-C4, E-M1·E-M2)
- E-C1: 원본 지문 검사는 **복제 전까지만**(`createdUnit`·`createdVisual`이 이미 있으면 지금도 건너뜀). 대기 중 원본이 바뀌면 실패 대신 "원본이 바뀌었습니다 — [바뀐 원본으로 계속] [취소]" 선택(계속 = 지문 다시 기록). 생성 전 미저장 원본은 [원본 저장 후 생성] 버튼을 같이 보인다.
- E-C2: 30초 고정 → 컴파일이 실제로 시작·끝나는 콜백 기준으로만 판단하고, 시간 상한은 "요청 뒤 컴파일 시작이 없음" 120초 하나만(진행 중 컴파일은 시간으로 끊지 않음).
- E-C3: 창을 열 때 대기 중 요청이 자동 아님이면 머리에 "Unity가 다시 시작돼 멈췄습니다 — [재개]".
- E-C4: 기록 파일을 읽지 못하면 [기록 파일 열기] [기록 버리기](확인 대화상자, 생성물은 건드리지 않음 — 경로 안내).
- E-M1: `openedResult`를 창 필드가 아니라 요청 기록(`resultOpened`)에 둔다.
- E-M2: 완료·취소 요청 줄에 [닫기](기록 파일 삭제, 백업 폴더는 남김).

### UA8 데이터 문제 표시 (E-D1·E-D2)
- 카탈로그 행의 문제 목록이 `ValidateData` 실패 문구를 그대로 보이고, 목록 아이콘·필터로 찾는다. 값은 고치지 않는다 — MainCharacter(퇴역 키 정책, Core·천명님)와 Shielder(프리팹 없음)는 사용자·담당자 결정.
- 문제 있는 유닛도 **편집은 된다**. 저장만 검증 실패면 막는데, "검증 실패해도 저장"은 두지 않는다(DB 생성이 부트에서 실패하므로).

### UA9 바로가기 (C12)
- 편집 탭 아래 "쓰는 곳": 이 유닛 키를 쓰는 EnemyLoadout·로드아웃·스테이지 초기 배치·증원 풀(직렬화 검색, 읽기만, 클릭하면 Ping). 편집은 하지 않는다.

## 4. 파일 계획
- 새: `UnitAuthoringCatalog.cs`(UA1), `UnitAuthoringRosters.cs`(UA4) + 각 `.meta`.
- 바뀜: 창 두 파일(UA2·UA3·UA5·UA6·UA9), `UnitAuthoringOperation.cs`(UA4 순서·취소, UA7), `UnitAuthoringAssets.cs`(UA6 테이블 문구, 카탈로그로 옮길 찾기 함수), `UnitAuthoringPendingStore.cs`(기록 칸·version).
- 게임 코드·SO 코드·에셋 변경 없음(로스터 에셋은 사용자가 도구로 바꿀 때만). 시험 없음(View 작업 규칙, 에디터 도구).

## 5. 단계(안)
- **UA-a** 카탈로그·오류 경계(UA1·UA6·UA8) — 화면 모양 그대로, 오류만 사라짐.
- **UA-b** 목록·상세 화면(UA2·UA3).
- **UA-c** UnitRoster 등록·편집(UA4) + 기록 형식.
- **UA-d** 생성 화면·요청 견고성(UA5·UA7)·바로가기(UA9)·사용법 한 쪽.
- 각 단계 끝에 멈추고 보고, Unity 확인은 사용자.

## 6. 정할 것

**결정(2026-11-11 사용자 "권장안대로"): Q-UA1~Q-UA6 모두 A.** **Q-UA6 바뀜(2026-11-11 사용자 "유닛 삭제 기능도 넣어줘"):** 삭제를 넣는다 — 쓰는 곳(로드아웃·스테이지·로스터 칸, 그 밖의 GUID 참조 파일)을 경고창으로 보인 뒤 참조도 함께 빼고, 테이블에서 빼고, Unit·Visual SO는 휴지통으로. EntityKey 항목은 체크했을 때만 지우며 코드·다른 SO가 그 키를 쓰면 막는다(`UnitAuthoringDeletion`). Q-UA7은 도구 범위 밖으로 남김(데이터 담당 결정). Task 계획은 [unit-authoring-window-improvement-plan](../plans/2026-11-11-unit-authoring-window-improvement-plan.md).

- **Q-UA1 생성 때 로스터 기본 체크** — A 원본이 든 로스터 전부(아군 전용은 없음, 권장) / B 늘 비움 / C 늘 모든 로스터.
- **Q-UA2 편집 탭 로스터 빼기 경고** — A EnemyLoadout·증원 사용처가 있으면 확인 대화상자(권장) / B 경고 없이.
- **Q-UA3 생성 때 정하는 값** — A 표시 이름·설명·진영 자격(권장) / B 표시 이름만(지금).
- **Q-UA4 단계 순서** — A UA-a → b → c → d(권장, 오류부터) / B 로스터(UA-c)를 맨 앞.
- **Q-UA5 짝 없는 유닛** — A 규칙만 편집 + [짝 Visual 만들기](다른 Visual 복제, 권장) / B 편집 불가 그대로 + 이유 표시.
- **Q-UA6 유닛 삭제·등록 해제** — A 이번 범위 밖(권장, Missing·참조 사용처가 넓음) / B 테이블·로스터 해제까지만 넣음.
- **Q-UA7 E-D1·E-D2 데이터** — 도구는 표시만 한다. MainCharacter(`EnemyOnly`로 바꿀지)·Shielder(프리팹) 처리는 따로 결정.
