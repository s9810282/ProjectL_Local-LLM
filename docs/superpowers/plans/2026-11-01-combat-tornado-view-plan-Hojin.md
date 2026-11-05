# 토네이도 전투 View 계획

- 작성자: 호진(이호진)
- 상태: TN-a~TN-f·이동하면서 날리기(TN10)·묶음 끝 검토 반영까지 코드 완료(2026-11-01). TN-a·TN-b Unity 문제 없음, 나머지는 컴파일 미확인·재생 확인 대기(§4). 토네이도 추가 수정은 사용자 지시로 낙석 뒤로 미룸. TP-4·TP-10은 칸 표시를 빼면서 철회했다.
- 정리: 2026-11-02 끝난 Phase의 Task 목록을 §2 요약으로 줄이고 옛 지형 토네이도 계획을 §5로 합쳤다. 옛 판은 git `3ad97e734`.
- 설계: [토네이도 View 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md)
- 기준: [Core → View 인계](../handoffs/2026-11-01-tornado-rockfall-haboob-core-view-handoff.md) §4·§8·§9.3
- 다음 순서: [낙석](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md) → [하붑](../specs/2026-11-01-combat-haboob-view-design-Hojin.md)

## 0 진행 규칙

- Phase가 끝나면 멈추고 보고한다. 커밋·push는 사용자가 한다.
- 새 시험을 만들지 않고 MSBuild를 돌리지 않는다. 보고에 "컴파일 미확인"을 적는다. 컴파일·재생 확인은 사용자가 Unity에서 한다.
- Core는 고치지 않는다. 공유 파일을 고치면 Phase TN-f에서 담당자 공유 문서에 적는다.
- 작업 트리의 다른 변경(`StageTest.prefab` 등)은 건드리지 않는다.

코드로 확인한 기존 시험 영향은 없다. `CombatFieldEventPorts`를 만드는 곳은 Factory 둘뿐이고, 들어 올림 조각과 처리기를 만드는 시험도 없다. 옛 Core 토네이도 시험(`TornadoEnvironmentTests` 등)은 Core 형식만 쓰므로 그대로 둔다.

## 1 계획에서 정한 것(확인 대상)

| ID | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 칸 이동 곡선은 새 칸 없이 `approachCurve`를 쓴다 | 값을 늘리지 않는다. 따로 조절이 필요하면 그때 더한다 |
| TP-2 | 칸 이동 시간 기본 0.25초(3칸 0.75초) | 임시 값, SO에서 조절 |
| TP-3 | 필드 창구에 좁은 계약 `ICombatTornadoField` 하나를 넘기고 `FieldTornadoPresenter`가 구현한다 | 사용자 "제일 정석적인 방법으로". 필요한 입구만 받는 프로젝트 기준(FW-101, 인터페이스 분리)에 맞고 `ICombatTrajectoryCanvas` 등 선례가 있다 |
| TP-4 | 철회. 칸 표시가 없어 이펙트만 칸 사이를 미끄러져 간다 | 사용자 결정(Q-TN1) |
| TP-5 | 몸 배율과 위치 보정에 모두 (`SideLength` ÷ 기준 칸 수)를 곱한다 | 지금 보정 (−0.5, −0.5)은 프리팹 축 보정이라 배율과 함께 커진다. Unity에서 확인한다 |
| TP-6 | 화살표는 중심에서 방향으로 (반지름 + 1)칸 표면에 둔다. TN-b 재생을 보고 조정한다 | 경고가 있으면 그 칸은 항상 판 안이다(`TornadoGeometry.Fits`) |
| TP-7 | 재배치 사건에 `TornadoSourceId`만 있고 `TornadoContact`가 없으면 예외다 | 새 Core는 늘 함께 낸다. 어긋나면 계약 위반이다 |
| TP-8 | 진입 때 확대 등장은 유지하고 다음 사건을 막는다. 이동은 전환 없이 직접 옮긴다 | `CombatTornadoAppearance`를 그대로 쓴다 |
| TP-9 | 용근님 특성 미리보기(`CombatTraitPreviewFactory`)에도 같은 표시를 만들어 창구에 넘긴다 | 창구 인자가 필수다. 미리보기 동작은 바꾸지 않는다. **2026-11-04 바뀜:** 평가 C4-02(Q-N2 A)로 미리보기도 운영과 같은 `CreateFieldPorts`를 불러 토네이도 외형 연결을 받는다. 지금 미리보기 시나리오에 토네이도가 없어 화면은 같다 |
| TP-10 | 철회. 3×3 칸 표시를 두지 않는다. 최종 이펙트는 TA가 프리팹으로 준다 | 사용자 결정(Q-TN1) |
| TP-11 | 확인용 `Test` 스테이지 배치는 사용자가 하고, 이 계획의 커밋 목록에 넣지 않는다 | 스테이지 데이터는 콘텐츠 결정이다 |
| TP-12 | SO 모드 패널의 가로 방향은 자동·왼쪽·오른쪽 선택이다 | 저장 값 0·−1·1에 맞춘다 |

## 2 Phase 요약

| Phase | 내용 | 결과 |
|---|---|---|
| TN-a | 설정 SO 칸(칸 이동 시간·화살표·몸 기준 칸 수·경고 소리), 있는 프리팹 연결 | Unity 문제 없음 |
| TN-b | 새 표시 `FieldTornadoPresenter`, 폭풍이 보임 | Unity 문제 없음 |
| TN-c | 던짐 연출(닿은 유닛) | 컴파일 미확인, 몸통 안 보임 수정은 사용자 "보임" |
| TN-d | 처리기 `CombatTornadoEventHandler`, 경고·이동 사건 재생 | 컴파일 미확인 |
| TN-e | 호버 설명·소리·SO 모드 도구(가로 방향), 설명 문구 | 컴파일 미확인 |
| TN-f | 옛 지형 토네이도 길 걷기, 문서·공유, 묶음 끝 검토(약 9.06) → M1·M2·L7 반영 | 컴파일 미확인 |
| TN10 | 폭풍이 유닛을 날리는 동안 멈추지 않게 이동하면서 날리기 | 컴파일 미확인 |

## 3 커밋 묶음

사용자가 Phase마다 또는 묶어서 커밋한다. 한 커밋이어야 하는 것:

- TN-a: 설정 SO 코드와 `TornadoPresentation_Default.asset`, 새 `.cs`와 `.meta`.
- TN-e: 설명 에셋은 Te.1과 함께.
- TN-b~TN-d는 차례로 쌓는다. TN-b만 커밋한 상태로 토네이도를 놓으면 닿을 때 전투가 멈춘다. 운영 스테이지에는 아직 토네이도가 없다.

## 4 실행 기록

### 방향 화살촉 점등(2026-11-03 사용자 "화살촉만 덩그러니 놓여 있어 별로, 점등하는 느낌으로" → "앞으로 살짝 움직였다가 꺼지고 다시 원위치에서 켜짐 반복", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 화살촉 하나·자리 규칙(중심에서 방향으로 반지름 + 1칸)은 그대로. 경고 동안 한 주기 = 원위치에서 켜짐 → 진행 방향으로 밀리며(처음 빠르고 끝 느림) 어두워짐 → 꺼짐, 반복. 숨었다 켜질 때마다 주기 처음부터.
- 규칙 원본 새 `DataBaseSystem/Data/View/Table/Terrain/Item/TornadoArrowPulse.cs`(`TryEvaluate`), 연출 설정 SO "방향 화살촉 점등" 칸 넷(`directionArrowPulseSeconds` 0.9·`LitRatio` 0.7·`Distance` 0.4·`EndBrightness` 0.4, 주기 0이면 점등 없음 = 옛 모습), 기본 에셋 같은 값(코드와 한 커밋).
- 공용 화살촉 `FieldArrowhead`: `SetColor`(Show의 색 갈래를 꺼냄)·`SetLit`(그리기만 끔, `IsVisible`은 표시 상태라 경고 소리 판단 `IsAimed`가 꺼진 구간에도 그대로). 이동 경로·넉백 화살촉은 동작 같음.
- `FieldTornadoPresenter`: 인스턴스가 원위치·방향·켜진 뒤 초·재질 색(`_BaseColor`)을 갖고, 새 `Display/Map/FieldTornadoArrowPulse.cs`(루트에 붙는 프레임 콜백, 결과 재생이 끝난 플레이어 차례에도 돌아야 해서 재생 Tick이 아님)가 매 프레임 `TickPulse`를 부름. 색은 재질 색 × 밝기를 ColorSystem 덮어쓰기로.
- 새 `.cs` 둘의 `.meta`는 Unity가 만듦(함께 커밋). Factory·사건 처리기 무변경.

### 이펙트 파티클 크기 방식 고침(2026-11-02 사용자 "바깥 프리팹에서 고쳐", 에셋만, 새 시험 없음, Unity 확인 대기)

- 보고: 바닥 Smoke만 크고 소용돌이는 원래 크기로 나옴.
- 원인: 이펙트 프리팹 `Script/Render/Effect/EventEffect/Toneido/Toneido.prefab`(민형님)의 파티클 셋(Toneido·Smoke·Rock)이 Scaling Mode Local이라 자기 Transform 크기만 쓴다. 바깥 프리팹이 준 `Toneido` 크기 0.26은 소용돌이에만 들고 자식 Smoke·Rock에는 안 내려가며, 코드 배율(외형 `Local Scale` × 칸 비율)과 등장 커짐도 셋 다 무시했다.
- 바깥 프리팹 `Terrain_Tornado_Toneido.prefab`의 Hierarchy 덮어쓰기 여섯은 `5770ce1b6`에서 이펙트 프리팹 GUID가 `03c1c8ad…` → `b98255d5…`로 바뀐 뒤 옛 GUID를 가리켜 무시되고 있었다.
- 고침: 그 여섯을 걷고 지금 이펙트 프리팹의 파티클 셋에 Scaling Mode Hierarchy 덮어쓰기를 넣었다. 이펙트 프리팹 원본·코드는 그대로.
- 외형 배율: 사용자가 Unity에서 `TerrainVisual_Tornado` `Local Scale`을 10 → 1로 바꿔 두어 그대로 둠. 이제 소용돌이 실제 배율 = 1 × 칸 비율 1.5 × 0.26 = 0.39(고치기 전 0.26의 1.5배), Smoke·Rock도 0.39(고치기 전 1). 고치기 전 소용돌이 크기를 그대로 원하면 0.667.
- 함께 달라지는 것: 등장·사라짐 커짐 연출이 파티클에도 보이고, 칸 수가 바뀌면 이펙트 전체가 함께 커진다. 필드 칸 루트(`Combat.unity` cellsRoot) 배율은 1이라 영향 없음.
- 남긴 것: 같은 옛 GUID를 가리키는 재질 덮어쓰기 셋(`Terrain_Tornado_Toneido_Layer2~4.mat`)은 무시되는 채로 둠(지금 이펙트는 다른 재질을 씀). 민형님께 전달.
- 확인: Smoke·Rock과 소용돌이 비율이 이펙트 원본과 같다, 토네이도가 나타날 때 커지며 나온다, 크기가 괜찮은지(아니면 `Local Scale`로 조정).
- 등장 곡선(2026-11-03 사용자 "한번 너가 수정을 해주세요", 에셋만, Unity 확인 대기): `TornadoPresentation_Default` `appearanceCurve` = (0,0) → (0.15, 0.9) → (1,1)(처음 15%에 90%까지 커지고 나머지는 천천히 맞춰짐, 사이에서 1을 넘지 않는 기울기). `appearanceSeconds` 1은 사용자 값 그대로. 편집 중 `approachCurve` 첫 키가 0.8로, `appearanceCurve` 끝이 1을 넘게 저장돼 둘 다 데이터 검사((0,0) 시작·(1,1) 끝·키 값 0~1)에 걸릴 상태였다 — `approachCurve`는 원래 값으로 되돌림.

### 이동하면서 날리기 코드 완료(2026-11-01 사용자 "오직 토네이도만 영향이 있다면 고치는게 맞다", 설계 TN10, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 던짐 사건: `TryBeginTornado`가 실행을 시작하자마자 사건 완료를 알린다. 착지 사실 반영은 실행 끝 콜백에 남겼다.
- 막기: 형식 표 칸 `overlapsUnitRuns`(토네이도 사건 넷만), `CombatSubjectBlocking.Blocks` 한 줄.
- 빨려 들어가는 목표: `CombatTornadoLiftTarget`이 지금 중심을 묻는 함수를 받는다. 접촉 자리 + 몸이 움직인 양. 새 계약 멤버 `TryGetBodyPosition`.
- 사용자에게 네 곳이라고 답한 것 중 폭풍 이동 실행(`ForAction`)은 고치지 않았다. 코드를 읽어 보니 행동 실행은 유닛 실행과 함께 돌고(`TryBegin`은 같은 범위끼리만 막음, `Tick`은 모든 실행 진행), 도착 전 던짐 시작을 막아 지금 그대로가 맞다.
- 다른 전투에 영향이 없는 근거: 형식 표 칸은 토네이도 사건 넷만 켜므로 `Blocks`의 새 줄은 다른 사건에서 늘 거짓이다. `TryBeginTornado`는 `TornadoSourceId`가 있는 던짐만 탄다. 들어 올림 값과 조각은 이 처리기만 쓴다.
- 남는 멈춤: 착지 뒤 그 유닛의 사건, 주체 없는 다른 사건(지뢰 발동 등)은 공중 유닛이 착지할 때까지 기다리고 그동안 폭풍도 기다린다.
- 확인: 유닛 하나·둘을 지날 때 폭풍이 멈추지 않는다, 유닛이 폭풍을 따라 오른다, 착지 칸과 HUD가 맞다, 지뢰 칸 착지, 숨겨진 폭풍, 착지로 전투가 끝나는 경우, 같은 턴 다른 유닛 연출에 변화 없음.

### TN-f 옛 길 걷기·문서 완료(2026-11-01, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 걷은 것(호출 없음 확인 뒤):
  - 사건: 형식 표 `CombatTornadoEvent` 줄, 사실 처리기 갈래와 인자 둘, 소리 표 갈래.
  - 창구: `CombatFieldEventPorts`의 `hideTornado`·`showTornado`, Presenter `showTornadoAt`, Factory 줄(전투·미리보기).
  - 지형 표시: `FieldTerrainPresenter`의 외형 전환·`Hide`·`ShowAt`·2×2 자리·높이·`TryGetVisualCenter`·`TornadoSettings`. 호출이 없어진 `TryGetBody`와 몸 모양 캐시·`Instance.Prefab`도 함께. 외형 연출 넷은 무너짐 집합만 남았다.
  - 기타: `FieldUnitPresenter.BodyBoxOf`, `EnvironmentVisualKinds`·`CombatEnvironmentText`의 `TornadoSourceFact` 갈래, `CombatTornadoAppearance.Hide`(`ShowAt`·`Settings`는 private으로).
- 계획에 적지 않았던 것: `TryGetBody`·`BodyBoxOf`·`Appearance.Hide` 삭제(계획의 "들어 올림 조회"에 들어가는 것으로 판단), 걸음 접촉 주석 한 줄.
- Core의 `CombatTornadoEvent`·`TornadoSourceFact`는 그대로다. Core가 옛 사건을 내지 않고(`new CombatTornadoEvent`는 천명님 시험 `CombatChangeEvidenceCoverageTests` 한 곳뿐), 옛 사실은 전투 준비가 환경에서 뺀다.
- 문서: 폴더 배치 안내, 설계 §9 구현 메모, 사운드 작업자 안내(이동 줄·방향 예고 줄), SO 모드 사용법(토네이도 줄·가로 방향·저장 규칙), 옛 토네이도 설계·계획 머리 표시.
- 공유(작성만, 모두 미전달): 민형님 Factory 공유 §45, 용근님 새 메모 `handoffs/2026-11-01-combat-tornado-view-yonggeun-share-Hojin.md`, 천명님 Core 전달 문서 §58, 전투 View 사본 공유 끝 절.
- 확인: TN-d 목록 다시 한 번, 지형(벽·고지대·약한 지반 붕괴) 표시에 변화 없음.

### 묶음 끝 검토 반영(2026-11-01 사용자 "셋 다 고쳐", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- M1: 도구가 토네이도에 외형 회전을 막는다. 패널은 읽기 줄 "토네이도는 회전 없음"(이미 적힌 항목은 지우기 버튼), 정의를 토네이도로 바꾸면 같은 변경에서 항목을 지운다, 저장 검사 `CheckPairs`가 문제로 낸다. 패널 인자를 bool에서 까닭 문자열 `SoFixedRotationReason`으로 바꿔 이웃 모양과 같은 길을 쓴다.
- M2: `IsTornado`가 정의 없음에 예외 대신 false를 답한다. 부르는 곳의 `Load != null` 확인 둘을 걷었고, 3D 미리보기는 색 상자로 그린다. 빈 정의는 SO 검사가 그대로 알린다.
- L7: 계약 멤버 `ICombatTornadoField.IsShown`을 `IsAimed`(보이고 화살표가 켜졌나)로 바꿨다. 이동 소리는 0번 중심에서 화살표를 끄기 전에, 경고 소리는 화살표를 맞춘 뒤에 묻는다. Core는 갈 곳이 없으면 방향을 (0,0)으로 정하고 화살표는 그때 꺼져 있다(`ApplyDirection`).
- 그대로 둔 것: 낮음 L1~L6·L8·L9(표 아래), 취소 때 화살표. 몸 자리 `localOffset`은 재생 확인 대상이다.

### TN-f 묶음 끝 검토(2026-11-01, 새 에이전트 하나, 코드만 읽음, 코드 변경 없음)

약 9.06/10(SSOT 8.75·SOLID 9.0·FW 10·독립성 8.5). 높음 0, 중간 2, 낮음 9. 읽어서 찾은 컴파일 오류 없음.

| 번호 | 내용 | 권장 |
|---|---|---|
| M1 결함 | 도구가 토네이도에 외형 회전을 허용한다. 지정하면 Core가 토네이도 ID를 지형 분류에서 빼서 `FieldTerrainPresenter.ValidateTerrainRotations`가 전투 진입 때 멈춘다. 지금 에셋은 비어 있어 안 드러난다 | 이웃 모양 지형처럼 막기(패널 읽기 줄, 정의 바꿀 때 항목 지움, 저장 문제) |
| M2 결함 | 3D 미리보기가 정의 없는 지형에서 `IsTornado` 예외를 낸다(Repaint마다). 이 줄 자체는 TN 전부터 있었다 | `Load != null` 확인 |
| L1 | 도구 사본 셋(`TornadoBodyRatio`·`TornadoPosition`·`TornadoProblem`), 주석으로 밝힘 | 그대로 |
| L2 | `GimmickId - 1` 규칙이 `TornadoAreaFact.Read`와 겹침 | 그대로 또는 Core 요청(주기를 영역 사실에) |
| L3 | 캐릭터 처리기가 표시 계약 전체를 받고 `Settings`만 씀 | 설정만 받기 |
| L4 | 처리기 `ApplyFacts`가 소리를 냄 | 그대로(TN8 결정) |
| L5 | `CombatTornadoAppearance`의 축소 후 재배치는 사건이 자리를 맞춰 정상 흐름에서 안 돎 | 안전망으로 두거나 걷기 |
| L6 | `CombatTornadoLiftTarget`은 값 둘의 묶음뿐 | 그대로 |
| L7 결함 | 판이 좁아 안 움직이면(방향 없음) 이동 소리가 나고, 경고 소리도 날 자리에 들어감 | 방향이 있을 때만 |
| L8·L9 | 소리 하나 더할 때 고칠 곳, 설정 생성자 위치 인자 17개 | 그대로 |

점수 밖: 취소 때 몸은 꺼지고 화살표가 다음 `Render`까지 남을 수 있음(취소는 진입·해제·오류 경로뿐이라 영향 적음).
확인 필요(재생): `TerrainVisual_Tornado` `localOffset` (−0.5, −0.5) × 비율 1.5가 3×3 가운데에 맞는지. 인계 §4.3은 옛 반 칸 보정을 없애라고 하고 TP-5는 프리팹 축 보정으로 봤다.

### TN-e 코드·에셋 완료(2026-11-01, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 호버: `CombatEnvironmentText.Build`가 공개 영역과 기믹 정의를 받아 덮는 칸마다 설명을 더한다. `{n}`은 그 원본의 `RepeatIntervalControllerTurns`. 시험에서 부르는 곳은 없다.
- 문구: `TerrainVisual_Tornado.asset` 설명 교체. 초안에 옛 강조색(`<color=#FF6262>`)만 살렸고, 옛 "캐릭터나 장애물이 있는 타일로는 이동시키지 않습니다" 줄은 초안대로 뺐다.
- 소리: 처리기가 0번 중심에 `CombatEventSounds.TornadoMove()`, 경고로 끝난 원본 완료에 `TornadoWarning(preset)`을 낸다. 둘 다 보이는 폭풍만이다. 경고 슬롯은 연결 대기(null)라 지금 소리 없음. 처리기 생성자에 `CombatEventAudio` 인자(Presenter 한 줄, Factory 무변경). 옛 `CombatTornadoEvent` 소리 갈래는 TN-f에서 걷는다.
- 도구 3D: `TornadoPosition(center, side)` = 중심 칸 + 덮는 칸 타일 바탕 최댓값. 보정·배율에 게임과 같은 비율(`TornadoBodyRatio`)을 곱한다. 2D는 그대로 중심 한 칸이다.
- 도구 검사: `TornadoProblem`이 판에 안 들어감·안쪽 중심에 방향 없음을 저장 문제로 낸다. 판 크기는 Core `TornadoGeometry.Fits`를 부르고, 방향 규칙만 사본(Core internal)이다.
- 도구 패널: '가로 방향' 자동·왼쪽·오른쪽(0, −1, 1). 작업본 칸 `tornadoHorizontalSign`, 읽기·쓰기, 작업본 형식 3.
- 계획에 없던 것 둘: ① 좌우 뒤집기·대칭 복사가 가로 방향도 뒤집는다(안 뒤집으면 대칭이 깨짐). ② 형식 3으로 올려 옛 초안·창에 남은 작업본은 복원하지 않는다(옛 초안이 방향을 0으로 덮는 것을 막음, 이전 칸 추가와 같은 규칙).

### TN-d 코드 완료(2026-11-01, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 새 `Playback/Handlers/CombatTornadoEventHandler.cs`, `Presentation/Pieces/CombatTornadoMovePiece.cs`(`.meta`는 Unity가 만든다). 사건 형식 표 네 줄, 처리기 칸 `Tornado`, 처리기 표 인자, Presenter 조립 한 줄.
- 요약·진입 시작은 할 일 없음. 중심 1번 이상은 몸을 옮기고 도착하면 사건을 끝낸다. 0번은 화살표를 끈다. 원본 완료는 최종 자리·방향을 맞춘다.
- 계약 입구 둘 추가(`TryGetBodyMove`, `MoveBody`). 숨겨진 원본은 이 입구가 거짓이라 몸 없이 던짐만 재생한다.
- 소리는 아직 옛 갈래뿐이다(TN-e).

### TN-c 중 몸통 안 보임 수정(2026-11-01, 사용자 "보임" 확인, 새 시험 없음)

- 증상(사용자): `FieldTornado_1`에 Dust 바람만 보이고 몸통이 없다.
- 원인: 민형님 `c6e5478ce`(10-30)이 `Toneido.prefab`의 몸 층 Layer1~4를 `playOnAwake`·`looping` 끔 + Pause Until Replaced로 바꿨다. 층은 켜져도 저절로 재생하지 않는다. Dust와 루트만 저절로 재생한다.
- 수정: `CombatTornadoAppearance.ShowAt`이 켤 때 모든 파티클을 비우고 재생한다(`PlayParticles`). 프리팹은 고치지 않았다.
- 같은 커밋이 `Tornado.shadergraph`도 바꿨다. 재질 값이 어긋나 모양이 다르면 그때 민형님 재질과 대조한다.

### TN-c 코드 완료(2026-11-01, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- `TryBeginTornado`: `TornadoContact`로 중심을 찾고(없으면 예외, TP-7), 자리는 그 칸 표면, 설정은 `ICombatTornadoField.Settings`. 표시를 읽지 않아 숨겨진 폭풍도 같은 길이다.
- `CombatTornadoLiftPiece`·`CombatTornadoLiftTarget`: 몸 이탈 판정, 숨김 콜백, `Top`, 몸 상자를 걷었다. 폭풍은 던짐으로 사라지지 않는다.
- 처리기 생성자: 대리자 둘 → 필수 `ICombatTornadoField`. `CombatSceneEventPresenter` 조립 한 줄.
- 계획 밖: 호출이 없어진 `CombatShownFacts.BodyBoxOf`를 지웠다. 사용자 컴파일에서 `FieldTerrainPresenter:398`의 옛 3인자 생성이 드러나, Tf.1에 있던 `FieldTerrainPresenter.TornadoLiftTarget`과 창구 `tornadoLiftTarget`(Factory 줄 둘)을 앞당겨 지웠다. `FieldUnitPresenter.BodyBoxOf`, `TornadoSettings`, `TryGetVisualCenter`는 TN-f에서 호출을 확인하고 지운다.
- 알려진 상태: 경고·이동 사건은 아직 건너뛴다. 이동 중 접촉 던짐은 재생되지만 폭풍은 결과 끝에 새 자리로 옮겨 간다(TN-d).

### TN-b 코드 완료(2026-11-01, Unity 확인 문제 없음 — 사용자, 새 시험 없음, 시험 변경 없음)

- 새 `Display/Map/FieldTornadoPresenter.cs`, `ICombatTornadoField.cs`(`.meta`는 Unity가 만든다). 허용 영역으로만 인스턴스를 만들고, 진입·결과 끝 `Render`, 원본마다 몸·화살표 하나.
- `CombatSceneDisplay`: 생성자 인자, `RefreshCore`에서 `Render`, 정리 목록 셋에 추가.
- 창구 필수 인자 `tornado`. 표시 Factory는 외형 Tick·진행·막기·취소를 지형과 합쳐 넘긴다. 미리보기 Factory는 빈 표시를 넘기고 외형 연결은 그대로다(TP-9, 2026-11-04 바뀜 — TP-9 참고).
- 계획과 다른 점 둘:
  - 풀을 쓰지 않는다. 전투 하나의 원본이 몇 개뿐이고 전투 동안 사라지지 않아서, `Clear`에서 없앤다.
  - 계약 입구는 이번에 쓰는 넷(`Settings`, `IsShown`, `Place`, `SetDirection`)만 뒀다. 이동 입구는 TN-d에서 더한다.
- 알려진 상태: 폭풍에 닿으면 옛 들어 올림 조회가 예외를 던져 전투가 멈춘다(TN-c에서 고침). 경고·이동 사건은 아직 건너뛰고, 결과 끝에 축소 후 새 자리에서 확대된다.

### TN-a 코드·에셋 완료(2026-11-01, Unity 확인 문제 없음 — 사용자, 새 시험 없음, 시험 변경 없음)

- 설정 칸 넷: `moveStepSeconds`(0.25), `directionArrowPrefab`, `bodyReferenceCells`, `warningSoundPreset`(−1). 화살표가 비면 부트 DB 생성에서 실패한다.
- 기본 에셋: `Arrow.prefab` 연결, 기준 칸 수 2(지금 프리팹이 2×2라서). 코드 기본값은 3이고 에셋 값이 이긴다.
- 화면 변화 없음. 아직 읽는 코드가 없다.
- 커밋: `TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`·`TornadoPresentation_Default.asset`은 한 커밋이다. 에셋 없이 코드만 들어가면 부트 DB 생성이 실패한다.

## 5 옛 지형 토네이도 결과 (10-17~10-31, View 코드 걷음)

<a id="g1-실행-기록--2026-10-17"></a>
<a id="phase-g2--외부-core-계약-인수와-normal-a-입력-연결"></a>
<a id="사용할-core-계약대조-완료"></a>
<a id="tornado-2x2-plan"></a>

설계 §11. Task·시험 수치·증거 경로는 git `3ad97e734`의 옛 계획.

| 작업 | 결과 | 커밋 |
|---|---|---|
| G0~G2 | 기획 정합, Normal A 최초 무작위 배치 연결 | `a1e54df2`·`f433c082` |
| G3.0 | 개발 진입 "지정 스테이지"(용근님 영역, 사용자 동의) | `9f6e5b8f` |
| P0·P1 | Toneido 원본 확인, 전투 wrapper·복사 재질 | `fba8cf98` |
| TR-0~TR-2 | 사건 시점 사라짐·다시 생김, 몸 조회(유닛 Y 보류) | `89cbc44e`·`34ee0de4` |
| G4 | 실제 네 칸 판정 | 10-27 철회 |
| Phase 0~3·낙하·진입 정정 | 판정 한 칸·외형 2×2, 진영 3턴 발동(Core), 생성·소멸, 상승·낙하, 진입 즉시 발동(Core) | `48e7b533`·`1ab12e55` |
| Phase 4 | 운영 재생 인수 — 중단, 11-01 Core 기믹으로 대신함 | — |
| H0~H3 | 성배 시험 본부 공격 칸 — 계획만, 진행 안 함 | — |

- 남긴 공유: Core 전달 문서 §5.58·§29·§45(미전달). 용근님 전투 View 파일·시험 다수를 고쳤고 공유 메모는 쓰지 않았다(옛 계획 §9 표, git).
