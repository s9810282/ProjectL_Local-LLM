# 전투 진입 보드 순차 등장 계획

- 날짜: 2026-11-02 (R9 갱신: 2026-11-03)
- 설계 원본: [전투 진입 보드 순차 등장 설계](../specs/2026-11-02-combat-board-reveal-design.md)
- 상태: R1~R9 코드 구현과 정적 검토 완료. R4 기본 동작 사용자 확인 완료. R5~R9 컴파일 미확인, Unity 인수 대기.
- 진행 승인: 2026-11-02 “Phase 차례대로 전체 진행해”. 이번 작업은 R1 결과를 중간 보고한 뒤 R2까지 연속 진행한다. 아래의 Phase 종료 멈춤은 이 지시로 대체한다.
- 사용자 확정: 로딩 종료 직후, 아군 측→적 측 순차 등장, 지형은 해당 타일과 동반, 아군 별도 등장 없음, 적은 보드 완성 뒤 기존 소환 동작 사용.
- 제안값·표시 정책·구현 경계는 설계 §1·§4·§9를 따른다. 사용자 확정 사항과 구분한다.

## Phase 0 설계와 Task 계획

- [x] 현재 생성 타일의 참조·정렬·높이 표 수명 확인.
- [x] 로딩 Enter/AfterLoad와 화면 걷힘 완료의 시점 차이 확인.
- [x] OnTile/ReplaceTile 부모와 활성 상태 소유권, 기존 적 소환 조각·설정 확인.
- [x] 결과 번호 P < S와 별개의 진입 진행 계약 필요성 확인.
- [x] 사용자 지형/적 표시 답변 반영, SSOT·SOLID 5원칙·JMH/FW·기능 독립성 점검.
- [x] R1·R2 변경 범위와 Unity 확인 항목 작성.

리뷰 지점: 설계와 구현 계획을 보고한다. 이 기록은 런타임 구현 완료가 아니다.

## Phase R1 보드와 적 등장 부품

- [x] R1.1 시작 전 작업 트리 재확인. 기존 Combat.unity, 유닛 에셋, 폰트·FMOD 변경을 보존한다. 하위 AGENTS 지침을 적용한다.
- [x] R1.2 View 연출 설정에 보드 설정 묶음 추가. 유한 수·0 경계 검사는 설정 한 곳에 두고, 기존 SummonRise 값은 그대로 소비한다. 새 `.cs`는 `.meta`와 함께 추가한다.
- [x] R1.3 생성 타일 참조를 정렬 이후 명시적으로 전달하는 작은 대상 계약 준비. 전체 `placed`를 기준으로 완성 자세·필드 방향 깊이를 수집하고 실제 Transform을 중복 등록하지 않는다.
- [x] R1.4 지형 Presenter에서 이미 그린 Terrain 외형의 칸·Transform을 제공. OnTile/ReplaceTile 소속 타일, 큰 타일 공유, 작은 타일의 기존 칸 중심 대응, 타일 없는 칸을 설계대로 연결한다. 렌더/Pool 부모는 유지한다.
- [x] R1.5 보드 연출 부품에 준비·순차 상승/초과/안착·완료·취소 구현. 시작 전 표시 억제와 원래 상태 복구, 타일 교체 활성 소유권, 최종 높이 표 불변을 유지한다.
- [x] R1.6 적 진입 조립 준비. 필드 적 목록만 대상으로 기존 CombatRisePiece·SummonRise 설정·Pose 대여/조각 실행을 재사용한다. 모델/HUD 억제를 기존 단일 숨김 ID와 분리하고, 시작 자세 적용 뒤 모델을 드러낸다. 가짜 Core 사건은 만들지 않는다.
- [x] R1.7 적 0명·한 줄 보드·시간 0·중간 취소·반복 정리 경로를 정적으로 검토한다. 생성/표시/대여 실패는 구체적 대상과 원인을 담아 기존 오류 경계로 전파한다.
- [x] R1.8 일반 공격·이동·소환 처리 변경 없이 부품과 설정으로 제한됐는지 diff 검토. 컴파일 미확인과 R2 미연결을 명시하고 보고 후 멈춘다.

R1 종료 시 부품이 준비되며, 실제 전투 진입 자동 실행은 R2에서 연결한다. R1만으로 게임에서 연출이 보인다고 보고하지 않는다.

## Phase R2 로딩 종료와 전투 진입 연결

- [x] R2.1 Factory에서 진입 단계 소유자·보드·적 부품을 생성하고 필요한 참조만 연결한다. 기존 Global Context로 읽은 로딩 상태를 제공한다. 새 루트 서비스나 별도 MonoBehaviour 타이머를 만들지 않는다.
- [x] R2.2 Combat Install이 완성 위치에서 지형/유닛/필드 구성을 끝낸 후, 로딩 걷힘 전에 진입 시작 상태 준비. ShowBattle의 Shown 의미를 유지하고 일반 갱신에서 재준비하지 않는다.
- [x] R2.3 첫 IsLoading false Tick부터 보드→격자→적→완료를 진행. 시간 중복 적용과 일반 사건 Tick 중복을 방지한다. 타일 없는 배경·기능 꺼짐은 설계의 건너뛰기 정책 적용.
- [x] R2.4 ICombatProgressView/Host에 진입 대기 읽기를 연결하고 CombatProgressReader/Facts에 전달. 상태는 진입 소유자 한 곳이며 기존 P/S·IsPresenting 식은 유지한다.
- [x] R2.5 CombatPlan에 진입 중 명령·미리보기·자동 진행·정리/이탈 대기 연결. 승패/포기의 제출 정지 우선순위 보존. 입력 사실/정책은 동일한 진입 대기로 손패·포인터·버튼·포기·전투 카메라 조작을 막고 완료 후 한 번 동기화한다.
- [x] R2.6 Tick의 진입 대기 중 일반 결과·가용성/미리보기 소비를 막는다. 기존 전투 중 소환/결과 재생과 진입 실행이 같은 자세 채널을 동시에 빌리지 않게 한다.
- [x] R2.7 씬 이탈·첫 구성 실패·오류 정지·Detach에서 진입 취소→표시 반환→배경 삭제 순서 연결. 모든 변경 자세/표시/대여 복구, 지난 완료 콜백 무효화, 재진입 시 새 준비를 확인한다.
- [x] R2.8 인터페이스 변경으로 컴파일이 깨지는 기존 시험 대역/호출만 최소 맞춤. Test 코드 신설과 MSBuild 실행은 하지 않는다. 다른 사람 시험의 실행 기대를 바꿔야 하면 먼저 사용자에게 묻는다.
- [x] R2.9 SSOT·SOLID 5원칙·JMH/FW·기능 독립성과 아래 수동 인수표를 대조한다. 실제 미실행 항목을 통과로 기록하지 않는다. 보고 후 멈춘다.

## 예상 변경 위치

모든 코드 경로는 `Assets/ProjectL/Script/Systems/` 기준이다.

| 범위 | 대상 |
|---|---|
| 새 연출 부품 | `RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/` 아래 진입 전용 부품·대상/설정 소비 타입과 `.meta` |
| 설정 | `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/`의 보드 설정 묶음과 필요 시 기존 연출 설정 에셋 |
| 대상/표시 연결 | `Combat/Display/Map/StageMapTiles.cs`, `StageViewPropPresenter.cs`, `FieldTerrainPresenter.cs`, `Combat/Display/Unit/FieldUnitPresenter.cs` 및 표시 억제에 필요한 내부 위젯 |
| 씬·조립 | `Combat/Display/CombatSceneDisplay.cs`, `Combat/CombatViewSystem.cs`, 연결 수명·입력 Facts/Policy, `Factory/Runtime/InGame/View/`의 해당 조립 |
| 진행 골격 | `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`의 ICombatProgressView, CombatProgressReader/Facts, CombatPlan와 View의 CombatViewProgress/Host |
| 기록 | 이 계획과 설계. Factory/공개 진행 계약 변경 내용을 해당 공유 문서로 정리하되 외부 발송은 하지 않음 |

`Combat/`는 `RuntimeSystem/InGameSystem/ViewSystem/Combat/`를 뜻한다. 실제 필요한 파일만 수정한다. Core, FieldCamera, Architecture, 기존 씬/에셋의 무관한 수정은 포함하지 않는다.

## Unity 인수 항목

| 항목 | 기대 |
|---|---|
| 로딩 | 로딩이 걷히는 동안 완성된 보드/적이 먼저 비치지 않고, 완전히 걷힌 직후 시작 |
| 방향 | 배경 회전·반전과 관계없이 아군 쪽에서 적 쪽으로 진행 |
| 지형 | 타일과 지형이 같은 박자/변위로 움직이고 완성 위치·회전·크기가 기존과 같음 |
| 바닥 교체 | 약한 지반·낭떠러지 등 교체 외형만 보이며 원래 타일이 다시 켜지지 않음 |
| 크기 | 소수 크기 타일·한 타일 여러 칸·타일 맵 밖 지형·한 줄 보드가 정상 |
| 적 | 보드 완료 전 적 모델/HUD가 보이지 않고, 완료 후 기존 소환 상승·회전·시간값으로 등장 |
| 조작 | 진입 중 손패 배치·전투 명령·호버/미리보기·포기·카메라 조작과 자동 진행이 대기하고 완료 후 정상 |
| 수명 | 씬 이탈·오류·중간 취소·재진입에서 위치/숨김/대여가 남지 않고 지난 완료가 새 입력을 열지 않음 |
| 기존 동작 | 전투 중 소환, 지형 붕괴/복구, 타일 후보·Random 회전, 최종 높이·유닛 배치 위치 유지 |
| 제외 경로 | 생성 타일 없는 맵·연출 꺼짐에서 기존 즉시 표시 유지 |

에셋 값 변경 시 사용자에게 DataBaseAssetTests 확인도 인계한다. 모든 컴파일·재생·EditMode 확인은 사용자가 Unity에서 수행한다.

## 실행 기록

- 2026-11-02: Phase 0 작성. 로딩 서비스, 타일/지형 표시, 소환 조각, 입력/진행 계약을 읽고 사용자의 연출 요구를 반영했다.
- Phase 0 당시 런타임 코드·씬·에셋·Test 코드 변경 없음. 컴파일 미확인. MSBuild·Unity 재생·테스트를 실행하지 않았다.
- 기존 사용자 변경: FMOD 캐시, Unit_Ballista/Unit_Cowboy/Unit_ShotGun, 폰트 두 개, Combat.unity. 이번 단계에서 수정하지 않았다.
- 커밋·push 없음.

### R1 결과

- 보드 전용 설정, 실제 타일·지형 대상 수집, 완성 자세와 Renderer 표시 보관, 깊이에 따른 상승·초과·안착, 반복 취소/복구를 구현했다.
- 초기 적 목록은 별도 표시 억제를 사용하며, 기존 `CombatRisePiece`·SummonRise 설정·Pose 대여 실행을 재사용한다. 실제 전투 소환 처리기는 수정하지 않았다.
- 적 0명, 시간 0, 한 깊이 보드, 타일 없는 지형, 표시 대여 실패, 중간 취소 경로를 소스로 검토했다. 실제 재생을 검증한 것은 아니다.
- R1 부품 완료와 R2 미연결 상태를 중간 보고한 뒤 사용자 전체 진행 지시에 따라 R2를 이어갔다.

### R2 결과

- Install 완료 뒤 준비하고 `IsLoading == false`인 첫 Tick부터 보드→격자→적 순서로 진행하도록 연결했다. `Shown`·결과 P/S·일반 재생의 뜻은 유지했다.
- 진행 객체의 `IsPending`을 `ICombatProgressView.IsEntryPresentationPending`으로 읽는다. 진행 Plan의 명령·미리보기·자동 처리·정리/이탈과 입력 정책의 Layer·손패·포인터·버튼·포기·카메라 조작이 같은 사실을 사용한다.
- 첫 구성 실패, 오류 정지, Clear/Release, Detach에서 진입 취소가 표시 반환보다 먼저 일어나게 했다. 직접 Release 경로에서도 복구한다.
- CellPrefab/GridOutline 표시를 내부 필드 루트로 묶었다. Global 풀의 테두리는 부모 비활성화만으로 숨겨지지 않아 기존 빈 칸 Update로 함께 숨긴다.
- 기존 시험에서 변경 인터페이스의 별도 구현이 발견되지 않아 Test 파일은 수정하지 않았다. 테스트 신설·MSBuild·Unity 실행도 하지 않았다.

### 정적 검토와 남은 인수

| 점검 | 결과 |
|---|---|
| SSOT | 진입 단계는 `CombatBoardEntryPresentation` 한 곳. 각 Facts는 현재 값을 읽는 사본. 칸 높이와 바닥 교체의 기존 소유권 유지 |
| S·O | 시간 계산, 적 조립, 전체 순서, Factory 조립을 분리. 기존 이동·공격·소환 조각 내부는 수정하지 않음 |
| L·I | 기존 Shown·IsAttached·P < S 유지. 별도 진입 계약은 Pending/Tick/Cancel만 제공. 적 조립은 일반 완료만 사용 |
| D·FW/JMH | Global Context로 로딩 조회. 새 서비스/MonoBehaviour 타이머/중복 등록 없음. 기존 모듈 수명과 오류 경계 사용 |
| 기능 독립성 | 보드 튜닝은 보드 설정/부품, 적 동작은 기존 소환 조각, 조립은 전용 Factory에서 변경 가능 |
| 파일 검사 | 변경 코드의 diff 공백 검사, 새 C#의 .meta 존재와 경로/네임스페이스·기존 API 연결을 정적으로 확인 |
| 제외 영역 | Core·FieldCamera·Architecture·기존 씬/에셋·Test 코드 수정 없음. 작업 중 관측한 다른 문서 변경도 보존 |
| 실행 검증 | **컴파일 미확인.** Unity 재생·EditMode·성능 측정 미실행. 위 Unity 인수표 전체가 사용자 확인 대기 |

공개 진행 계약과 조립 경로는 [Framework 공유 메모](../handoffs/2026-11-02-combat-board-reveal-framework-share.md)에 정리했다. 문서만 작성했으며 외부 발송하지 않았다.

## Phase R3 Default·Coner와 증가 묶음

사용자 후속 요청으로 설계 §11을 추가했다. 증가 방식은 비동기 답변에서 묶음 크기 증가로 확정했다. 이번 Phase 완료 후 보고한다.

- [x] R3.1 `CombatBoardRevealMode` enum과 .meta 추가. 기존 Default=0 보존, Coner=1 추가.
- [x] R3.2 기존 Board Reveal 설정에 Mode와 Coner Batch Growth(기본 1, 유한한 0 이상) 추가. 기존 에셋 값은 보존.
- [x] R3.3 준비 시 시작 시간표 계산을 분리. Default의 기존 깊이 비례 지연을 유지하고 Coner의 대각 순서·증가 묶음·전체 시간 분배를 구현.
- [x] R3.4 타일별 지형 동기화와 중복 개수 방지, 타일 없는 지형, 마지막 잔여 묶음·1타일·시간 0·증가량 0/소수/큰 값 경로를 정적으로 검토.
- [x] R3.5 원래 자세 복구·기존 소환/로딩/입력 수명 유지와 SSOT·SOLID·FW/JMH·기능 독립성 검토. 새 파일 .meta·diff 검사. Test 코드·MSBuild·Unity 실행 없음.
- [x] R3.6 Inspector 설정 위치·값 예시와 Unity 확인 항목을 보고. 컴파일 미확인 표시.

Unity 추가 인수: Default가 기존과 같은지, Coner가 좌하단→우상단인지, 증가량 1/2/0.5/0에서 묶음 크기가 의도대로인지, 지형이 타일 묶음에 붙고 두 번 움직이지 않는지, 마지막 잔여 묶음 이후 적 소환으로 이어지는지 확인한다.

R3 결과: 기존 준비 시 깊이 계산을 별도 시간표로 옮겼고 Default의 식은 유지했다. Coner는 실제 타일만 세고 지형이 같은 지연을 공유한다. 묶음 크기는 남은 타일 수로 제한한 뒤 정수로 바꿔 큰 증가량의 변환 넘침을 피한다. 시작 지연은 묶음 진행률 × SweepSeconds로 계산한다. 새로운 enum/시간표 .cs와 .meta 2쌍을 추가했다. 기존 에셋/씬·Factory·진입 상태/취소·소환·입력/자동 진행은 이번 R3에서 수정하지 않았다.

Inspector: `CombatPresentationSettings_Default` → Combat Presentation Settings Data → 전투 진입 보드 등장 → Board Reveal. Mode=Coner를 선택하고 Coner Batch Growth를 조절한다. 1은 1→2→3, 2는 1→3→5, 0.5는 1→1→2→2, 0은 매번 1개다. Sweep Seconds는 전체 시작 시간 범위를 정한다. 실제 컴파일·재생·EditMode는 실행하지 않았으며 **컴파일 미확인**이다.

## Phase R4 배경 구역 연결

사용자의 `RevealArea_*` 구역 등장 연결 요청에 따라 설계 §12를 추가했다. 기존 프리팹 정리 결과는 보존하고, 이번 Phase는 코드 연결 후 보고한다.

- [x] R4.1 배경 수집 부품과 .meta 추가. 지정 경로의 직속 구역만 수집하고 완성 위치의 필드 X·Z 거리로 가장 가까운 생성 타일에 대응한다. 구역 없음/타일 없음 정상 경로와 잘못된 부모·자식 중첩 오류를 구분한다.
- [x] R4.2 StageViewPropPresenter의 타일 정렬 뒤 수집·Release 초기화를 연결하고, 전용 Factory가 같은 대상 목록을 진입 소유자에게 전달한다.
- [x] R4.3 기존 Prepare 대상에 구역을 추가한다. 기존 시간표·보간·숨김·취소와 적 소환/입력 대기를 재사용한다. 구역을 Coner 묶음 개수에 추가하지 않는다.
- [x] R4.4 현재 프리팹의 29개 구역과 카메라/조명 제외를 정적으로 대조한다. 타일·지형과 Transform 중첩 없음, 완료/오류/Release 복구, 기존 모드/설정 유지와 설계 원칙을 검토한다. Test 코드·MSBuild·Unity 실행 없음.
- [x] R4.5 diff·새 .meta 검사와 구현 기록. 컴파일 미확인 및 Unity 인수 항목 보고.

Unity 추가 인수: 로딩이 걷힐 때 배경 소품이 먼저 남아 있지 않은지, 구역과 가까운 타일이 함께 올라오는지, 구역의 건물/소품 내부 배치가 유지되는지, Default/Coner 모두 배경 안착 뒤 적이 등장하는지, 중간 이탈/오류/재진입 후 위치·표시가 복구되는지 확인한다.

R4 결과: `CombatBackdropRevealTargets`가 구역과 가까운 실제 타일을 연결하고, `StageViewPropPresenter` → `CombatBoardEntryFactory` → `CombatBoardEntryPresentation.Prepare`로 전달한다. 별도 Inspector 연결은 필요 없다. 구역도 기존 보드 설정과 타일 시간표를 공유하므로 Default·Coner 순서, 묶음 증가량, 상승 시간·깊이·초과 높이를 함께 따른다. 구역 수는 타일 묶음 수를 늘리지 않는다. 새 서비스·진행 상태·타이머 없이 기존 완료/취소 복구를 사용한다.

정적 확인: 기존 29개 구역·182개 직속 원본 오브젝트의 부모 연결을 재확인했고, 프리팹은 구역 정리 직후 사본과 동일하다. Day Camera와 조명 4개는 등록 구역 밖에 있다. 생성 타일과 지형은 별도 루트이며, 구역과 생성 타일의 부모·자식 중첩은 구성 오류로 거절한다. Factory의 필드 좌표계, 시간표의 타일 참조 중복 제거, 설치 실패/취소/Release의 복구 우선순위를 소스로 검토했다. SSOT·SOLID·FW/JMH·기능 독립성은 설계 §12 기준을 유지한다. 새 .cs/.meta와 변경 diff를 확인했다.

이번 R4에서 프리팹·씬·설정 에셋·Core·FieldCamera·Architecture·Test 코드는 변경하지 않았다. 테스트 작성·MSBuild·Unity 실행 없음. **컴파일 미확인**, 위 Unity 추가 인수는 사용자 확인 대기다.

2026-11-02 사용자 후속 확인: 배경 구역 등장 동작을 확인하고 관련 변경의 Git 스테이징을 요청했다. R4 기본 동작은 사용자 확인 완료다. 모든 모드·중간 취소·오류·재진입 인수 항목 전체를 완료했다는 의미는 아니며, Codex의 컴파일·재생 직접 실행은 없었다.

## Phase R5 적 Nexus 낙하

사용자 지정 대상은 배경 건물이 아닌 Structure 적 Nexus다. 설계 §13을 따른다. 이번 Phase는 설정·동작·진입 연결을 완료한 뒤 보고한다.

- [x] R5.1 Nexus Drop 설정과 .meta 추가. 기존 SO에 묶음 연결/검증 및 기본 설정 에셋 반영.
- [x] R5.2 FieldUnitPresenter에서 살아 있는 적 Headquarters 표시만 수집. Install의 Structures를 진입 Prepare로 전달.
- [x] R5.3 Nexus 낙하 부품과 .meta 추가. 숨김/HUD, 자세 대여, 가속 낙하, 지면 착지 이펙트 1회, 자연 종료 대기와 취소 정리.
- [x] R5.4 Entry의 보드→Nexus→적 단계 및 Factory의 같은 이펙트 창구 전달. 대상 없음/꺼짐 경로와 실패/이탈 복구 검토.
- [x] R5.5 SSOT·SOLID·FW/JMH·기능 독립성, 실제 API 호출/.meta/diff 정적 검사. Test 코드·MSBuild·Unity 실행 없이 컴파일 미확인 보고.

Unity 인수: 로딩 종료/보드 상승 중 Nexus 모델·HUD가 안 보이는지, 보드 완성 후 위에서 가속 낙하해 원래 자리/방향에 맞는지, 지형 높이가 반영된 바닥 면에서 FallingRocks가 1회 재생되는지, 이펙트 종료 뒤 적 소환인지, Nexus 없음/사망/아군·다른 구조물/옵션 꺼짐 경로, 낙하·이펙트 중 오류/씬 이탈·재진입 시 자세·HUD·이펙트 복구를 확인한다. 설정 에셋을 변경하므로 DataBaseAssetTests도 사용자 확인 항목이다.

R5 결과: 대상은 사용자 정정에 따라 적 Nexus만이다. `FieldUnitPresenter.CollectEntryNexuses`가 현재 Buffer Structures와 표시를 대응하고, `CombatEntryNexusAppearance`가 기존 자세 대여·표시 억제·HUD·이펙트 손잡이의 수명을 관리한다. 입력/자동 진행 대기는 기존 Entry.IsPending을 그대로 사용한다. 보드 단계가 끝난 프레임은 Nexus Begin만 하고, 다음 프레임부터 시간을 소비하므로 같은 deltaTime을 두 단계에 적용하지 않는다.

착지 자리의 원본은 기존 점유 칸·지형 높이를 반영한 완성 자세다. 원래 자세로 정확히 복귀한 뒤 FallingRocks를 한 번 내며, 이펙트의 자연 회수가 확인되면 기존 적 소환을 시작한다. 원래 Renderer 비활성 상태를 보존하고, 취소는 자세 반환/이펙트 즉시 회수/표시·HUD 복원을 각각 시도한다. EntityId 재바인딩과 대여 취소 콜백의 지난 세대도 가린다. Nexus 없음·옵션 꺼짐·0초 낙하·0 높이 및 중간 실패는 소스로 검토했다.

SSOT·SOLID·FW/JMH·기능 독립성은 설계 §13 기준을 유지한다. 기존 Global 효과 서비스와 같은 전투 효과 창구를 Factory가 전달하며 새 등록/Export는 없다. 설정/동작 새 파일 2개와 .meta를 확인했고, 변경 코드·문서 diff 공백 검사를 완료했다. 배경 프리팹·구역·타일 시간표·Core·FieldCamera·Architecture·Test 코드 변경 없음. **컴파일 미확인**이며 MSBuild·Unity 재생·테스트를 실행하지 않았다.

조정: `CombatPresentationSettings_Default` → Combat Presentation Settings Data → 전투 진입 적 Nexus 낙하 → Nexus Drop. 클래스 초깃값과 최초 반영값은 Enabled=true, Height=10(월드 단위), Seconds=0.6이다. 이후 사용자 스테이징 요청 시점의 에셋 튜닝값 Height=30, Seconds=0.8을 보존해 함께 포함했다. FallingRocks의 자연 종료 시간은 기존 이펙트 프리팹의 수명이다.

## Phase R6 기믹·선로·상시 이펙트 표시 지연

사용자 선택은 보드 완성 후 기믹 일괄 표시이며 기찻길도 포함한다. 추가 요청으로 유닛 상시 이펙트는 유닛 등장 완료 뒤에 재생한다. 설계 §14 범위로 이번 Phase를 구현한 뒤 보고한다.

- [x] R6.1 보류 전용 부품/.meta 추가. 기믹 사실 캡처와 기존 Render 호출, 선로 표시 억제/복구, 상시 효과 공개/취소 연결.
- [x] R6.2 FieldRailwayPresenter의 설치 루트 표시 API 추가. Paths와 열차 발동 수명은 유지.
- [x] R6.3 ShownFacts의 SyncPersistent 보류와 명시적 공개를 추가하고 EventPresenter에서 연결. 최초 그리기 전 보류, Clear 초기화, 일회성 효과 독립성 유지.
- [x] R6.4 Factory에서 같은 부품을 SceneDisplay/Entry에 주입. 초기 Refresh 전 준비, 보드 완료 후 기믹, 적 등장 완료 후 상시 효과, 취소 시 미공개 작업 폐기.
- [x] R6.5 일반 지형/적 소환/낙하 유지, 연출 꺼짐/타일 없음/재설치, 오류/해제 경로와 SSOT·SOLID·FW/JMH·기능 독립성 정적 검토. .meta/diff 확인. Test 코드·MSBuild·Unity 실행 없음.

Unity 인수: 보드 상승 중 모래바람·토네이도(화살표 포함)·낙석 경고·선로가 먼저 보이지 않고 보드 완료 때 함께 공개되는지, 기차 맵의 Rail/RailTile·밖 연장 구간이 모두 보이며 열차는 발동 때만 나타나는지, Nexus FallingRocks가 정상 재생되는지, 상시/상태 유지 이펙트는 적 등장 완료 후 재생되고 이후 특성 발동·이동·상태 해제에도 정상인지, 연출 꺼짐/타일 없음과 중간 취소/재진입에 표시 보류가 남지 않는지 확인한다.

R6 결과: `CombatEntryDeferredDisplay`와 .meta를 추가했다. 로딩 중 기존 선로 설치/경로 준비를 마치고, 첫 Refresh 전에 선로 표시와 기믹/상시 효과를 보류한다. 보드 완료 때 선로·모래바람·토네이도·낙석 경고를 공개하고, 적 등장 완료 때 현재 표시 사실로 상시 효과만 동기화한다. 일반 Terrain의 타일 동반 등장과 Nexus FallingRocks는 기존 경로를 유지한다.

정적 검토: 같은 보류 객체가 Factory→SceneDisplay/Entry에 전달되는지, 최초 이벤트 Bind→PrepareDisplay→Refresh→자세 준비 순서인지 확인했다. ShownFacts.Replace는 표시 보류를 초기화하지 않고, Clear는 초기화한다. 진입 중 여러 SyncPersistent 호출도 같은 입구에서 억제된다. 정상 완료는 명시적 RevealUnits를 호출하고, Cancel은 스냅샷과 보류만 버려 새 효과를 만들지 않는다. 선로는 설치 루트만 숨겨 Paths를 보존하며 열차 개별 비활성도 유지한다. 연출 꺼짐/생성 타일 없음/재설치와 부분 준비 실패·반복 Cancel·Clear/Release 순서를 소스로 대조했다.

SSOT·SOLID·FW/JMH·기능 독립성은 설계 §14 기준을 유지한다. 새 서비스 등록/루트 Export/MonoBehaviour 타이머 없음. 생성/조립은 Factory, 표시 보류는 전용 부품, 진행 시간은 Entry, 기믹 인스턴스와 상시 효과 집합은 기존 소유자에 남는다. 새 .meta GUID·호출 지점·diff 공백을 확인했다. 변경으로 서명이 깨지는 기존 시험 호출은 발견되지 않았다.

이번 작업의 씬·프리팹·설정 에셋·Core·FieldCamera·Architecture·Test 코드 변경 없음. 기존 `CombatSceneDisplay`의 `_inspection.Synchronize(..., state.TraitSettings)` 사용자 변경을 보존했다. Test 코드 작성·MSBuild·Unity 재생/테스트 실행 없음. **컴파일 미확인**, 위 인수 항목은 사용자 확인 대기다.

## Phase R7 — 선로 순차 낙하

외부 연장 정책은 후속 R8에서 맵 내부 생성으로 대체한다. 아래는 당시 구현 기록이다.

사용자 요청 범위: 타일 완료 후 0번 쪽에서 반대편까지 선로 낙하. 설계 §15를 따른다.

- [x] R7.1 설치된 RailTile/Rail 쌍을 직선별로 순서대로 제공하고 해제 수명에 연결한다.
- [x] R7.2 전용 낙하 설정·실행 부품·.meta를 추가한다. 보류/순차 공개/착지/취소 복구를 담당한다.
- [x] R7.3 Factory와 Entry의 보드→선로→Nexus 단계를 연결하고 기본 설정 에셋에 값을 추가한다.
- [x] R7.4 호출 지점·0 방향/바깥 연장·다중 선로·비활성/빈 목록·실패/취소·GUID·diff 및 SSOT/SOLID/FW/기능 독립성을 정적으로 확인하고 공유 메모를 갱신한다.

Unity 인수: 가로/세로 선로가 보드 전체 안착 뒤 0번 쪽 끝부터 순차 낙하하는지, RailTile과 Rail 및 밖 연장 구간이 함께 움직이는지, 모든 선로 안착 후 Nexus/적/상시 효과 순서인지 확인한다. Height/Seconds/Segment Gap Seconds 및 0값·선로 없음·Railway Drop/Board Reveal 끄기·중간 취소/재진입·다중 선로·Reverse가 켜진 열차 발동도 확인한다. 설정 에셋 변경으로 DataBaseAssetTests를 사용자 확인 항목에 포함한다. Test 코드 작성·MSBuild·Unity 실행 없이 정적 검토하며 **컴파일 미확인**으로 보고한다.

R7 결과(2026-11-03): 전용 설정/실행/조각 타입 3개와 .meta를 추가했다. 직선의 칸 순서는 기존 Layout의 +X/+Y를 사용하고, 0번 쪽 외부 조각은 앞에 삽입해 바깥 끝에서 시작한다. 반대편 외부 조각은 뒤에 이어 붙인다. 선로별 순번으로 지연을 주고 Tile/Rail은 같은 지연과 높이를 사용한다. 기본 에셋에 Height=10, Seconds=0.45, SegmentGapSeconds=0.08을 추가했다.

정적 검토: 설치 전 조립된 읽기 전용 목록이 Install/Clear를 반영하고, Install→첫 Refresh→Prepare 이후에만 조각 자세를 수집하는지 확인했다. 보드 완료 시 루트가 공개돼도 대기 조각의 Renderer는 숨김을 유지하며 Begin/Tick 때 공개한다. 빈 선로/비활성은 즉시 다음 단계로 넘어가고 시간 0/간격 0도 각 시작 시각 또는 동시 착지로 처리한다. 진입 Cancel이 선로 Clear/Release보다 먼저 자세·표시를 복구한다. 기존 열차 Paths/Reverse/발동/애니메이션 코드는 변경하지 않았다. 호출 지점과 새 GUID 고유성·diff 공백을 확인했으며 서명 변경으로 깨지는 기존 시험 호출은 발견되지 않았다.

SSOT/SOLID 5원칙/FW·JMH/기능 독립성은 설계 §15 기준을 유지한다. 설정은 한 곳, 설치 쌍은 Presenter, 진행은 Entry, 자세 변경은 낙하 부품, 생성/연결은 Factory가 맡는다. Core·FieldCamera·Architecture·Test 코드·씬/프리팹 변경 없음. 기존 사용자 작업과 R6 변경을 보존했다. Test 작성·MSBuild·Unity 컴파일/재생/시험 실행 없음. **컴파일 미확인**, Unity 인수는 사용자 확인 대기다.

## Phase R8 — 맵 내부 선로만 생성

사용자 결정(2026-11-03): “이걸 460 조각이나 생성해야하나? 맵 사이즈에 맞추는게”. 설계 §16에 따라 기존 외부 230칸씩 생성을 제거한다.

- [x] R8.1 FieldRailwayPresenter의 바깥 연장 생성/등록을 제거한다.
- [x] R8.2 열차 경로 계산은 보존하고 OutsideCells/TravelPaddingCells 설명을 실제 역할에 맞춘다.
- [x] R8.3 생성 대상·순서·낙하 소요 시간·경로 보존·diff를 정적으로 검토하고 공유 메모를 갱신한다.

Unity 인수: 보드 밖 RailTile/Rail이 생성되지 않는지, 보드 첫 선로부터 지연 없이 순차 낙하하는지, 끝 칸의 착지 후 Nexus/적이 이어지는지 확인한다. 열차의 실제 발동·접촉과 가로/세로/Reverse·선로 없는 맵·취소/재진입을 확인한다. 설정 에셋 수치는 수정하지 않는다. Test 코드·MSBuild·Unity 실행 없이 정적 검토하며 **컴파일 미확인**으로 인계한다.

R8 결과: 선로 생성의 유일한 AddSegment 호출은 `track.Cells` 순회다. 외부 연장 생성용 ExtendEnd를 삭제했고 직선별 목록도 맵 내부 칸 수와 같다. 양끝 경계까지 연결된 직선은 기존 대비 460조각(레일/받침 프리팹 920개)과 36.8초의 시작 간격 누적을 줄인다. 10칸 예시는 `(10-1)×0.08+0.45=1.17초`다. 실측 시간이 아닌 설정/코드 기준 계산이다.

정적 검토: AddTrain 조립/경로 구간이 HEAD와 동일함을 확인했다. OutsideCells의 런타임 소비는 열차 이동 여유 계산에만 남았으며 열차 발동/접촉/속도는 바꾸지 않았다. 새 상태·설정 수치·클래스·서비스/Export/등록 없음. 설계 §16의 SSOT/SOLID/FW·JMH/기능 독립성을 유지한다. 사용자가 조절한 Railway Drop Height=30도 보존했다. Core·FieldCamera·Architecture·Test 코드·설정 에셋·프리팹 변경 없음. diff 공백 검사 완료. Test 작성·MSBuild·Unity 실행 없음, **컴파일 미확인**.

## Phase R9 — 양 끝 5칸 연장 (2026-11-03)

최종 사용자 요청은 “양쪽 처음과 마지막 셀에서 5개씩 더 나가게”다. 앞선 한쪽 3칸 요청을 대체한다.

설계: 직선별 첫 선로 중심에서 반대 방향으로 5칸, 마지막 중심에서 진행 방향으로 5칸을 표시용으로 추가한다. `RailwayVisualData.RailExtensionCells`(0 이상 정수, 기본/운영 에셋 5)가 표시 연장 개수의 원본이다. 기존 OutsideCells/TravelPaddingCells는 열차 이동 여유로 유지한다. 첫 외부 끝→맵 내부→마지막 외부 끝 순서로 기존 낙하 목록에 넣고, RailTile/Rail은 같은 조각이다. N칸 직선의 총 조각은 N+10, 현재 간격에서는 R8 대비 0.8초가 늘어난다. Core의 실제 RailCells/접촉/열차 경로를 늘리지 않는다.

- [x] R9.1 선로 표시 설정/검증/툴팁과 운영 에셋에 Rail Extension Cells=5를 추가한다.
- [x] R9.2 직선 양 끝에 지정 개수만 생성하고 기존 순서/낙하 수명에 포함한다.
- [x] R9.3 개수·순서·열차 경로 보존·diff를 정적으로 검토하고 공유 기록을 갱신한다.

SSOT/SOLID: 표시 길이는 SO, 배치/목록은 Presenter, 낙하는 기존 부품이 담당하며 새 상태/유닛별 분기 없이 기존 계약을 유지한다. 새 의존성/서비스/등록/Export/조회 없음(FW/JMH). 표시 연장 수정은 설치와 설정 안에서 끝나며 열차 이동/투사체/진입 단계 내부를 수정하지 않는다. Unity 인수는 양끝 5칸, 0 설정, 가로/세로/Reverse, 순차 낙하와 실제 열차 발동이다. 설정 에셋 변경에 따른 DataBaseAssetTests는 사용자 확인 항목이다. Test 코드/MSBuild/Unity 실행 없이 **컴파일 미확인**으로 보고한다.

R9 결과: 첫 구간은 5→1 순으로 첫 칸 앞에, 마지막 구간은 1→5 순으로 끝 칸 뒤에 추가한다. 각 구간은 기존 한 칸 길이/폭/높이를 쓰고, 기존 AddSegment가 레일/받침을 같은 조각으로 묶는다. RailExtensionCells=0이면 추가 루프가 실행되지 않는다. AddTrain 조립/경로 구간이 HEAD와 동일함을 정적으로 확인했고 diff 공백 검사도 완료했다. 기존 열차 편성·이동/접촉·다른 사용자 설정은 보존했다. 새 .cs/.meta·Core·FieldCamera·Architecture·Test 코드 수정 없음. Test 작성/MSBuild/Unity 실행 없음. **컴파일 미확인**, Unity 및 DataBaseAssetTests는 사용자 인수 대기다.

## Phase R10 — StageTest 배경 구역 정리 (2026-11-03)

사용자 요청은 `Prefabs/Stage/MapProp/Test/Stages/StageTest.prefab`을 NewStageTest와 같은 방식으로 분할하는 것이다. 설계: `DayMap/BackGroundProp` 기준 X·Z 피벗을 16×16 단위로 나누고 `RevealArea_X…_Z…`를 직속 자식으로 만든다. 구역 중심은 `(X 인덱스 × 16 + 8, 0, Z 인덱스 × 16 + 8)`이며 회전/크기는 항등이다. Map/Props와 Map/rock의 소품을 포함한 188개 원본 루트를 45개 구역에 넣는다. 원본 피벗 기준 분류이며 메시 자체를 절단하지 않는다.

Props의 기존 오프셋을 포함해 완성 배치를 보존하고 기존 회전·크기·활성 상태·컴포넌트·재질·중첩 프리팹 연결/.meta GUID는 유지한다. 소품 내부 계층은 그대로 두고, 비워진 Map/Props/rock 빈 오브젝트도 참조 보존을 위해 남긴다. Day Camera/조명은 기존 위치와 부모를 유지한다. 기존 R4 수집 규칙이 이 구역을 자동으로 읽으므로 런타임 코드·DB/씬 연결을 추가하지 않는다.

- [x] R10.1 원본 사본을 확보하고 위치별 빈 구역 생성·소품 재부모화를 수행한다.
- [x] R10.2 전후 위치/회전/크기, 원본 ID/참조/프리팹 오버라이드, 부모·자식 일치, 카메라/조명/참조 프리팹/.meta 보존을 정적으로 대조한다.
- [x] R10.3 결과와 Unity 인수 항목을 기록한다.

SSOT는 프리팹 구역 Transform과 기존 R4 수집 규칙이다. S/O: 에셋 조립만 변경하고 등장 구현을 재사용한다. L: 기존 배경 설치·등장·복구 계약 유지. I/D: 새 인터페이스/의존성 없이 기존 수집 결과를 사용한다. FW/JMH: 서비스 생성·등록·Export·조회 변경 없음. 기능 독립성: 해당 프리팹과 작업 기록만 변경하며 타일 시간표·Core·카메라 구현에 손대지 않는다. Test 코드/MSBuild/Unity 실행 없이 정적 데이터 대조만 수행한다. Unity 인수는 배치·재질·중첩 프리팹 유지와 해당 배경을 사용하는 스테이지 진입 시 구역별 동반 등장이다. **컴파일 미확인**.

R10 결과: 45개 빈 GameObject/Transform을 추가하고 188개 원본 루트(중첩 프리팹 182개, 일반 루트 6개)를 재배치했다. 기존 Map에는 비워진 rock/Props만 남기고, smallrock 등 소품 내부 계층은 유지했다. 기존 모든 fileID와 중첩 PrefabInstance 183개가 보존됐으며, 변경은 대상 부모·루트 X/Z 위치·자식 목록뿐이다. Renderer/MeshFilter/Collider/Light/MonoBehaviour와 내부 프리팹 오버라이드는 동일하다. 부모·자식 불일치와 미해결 로컬 참조는 0개다.

원본 좌표와 재조립 좌표의 차이는 소수 연산 오차 범위이며, float 정밀도 환산 최대 차이는 약 0.000003815 단위다. 원본 프리팹 .meta, NewStageTest 및 그 .meta는 해시가 동일하다. diff 공백 검사를 완료했다. 작업 전 사본과 일회성 편집/대조 자료는 Git 제외 경로 `Temp/CodexStageReveal20261103/`에 보관했다. 프리팹 에셋과 이 기록만 수정했으며 새 Test 코드·MSBuild·Unity 실행 없음. **컴파일 미확인**, 실제 배치·등장 연출은 Unity 인수 대기다.

2026-11-03 후속: 반복 사용을 위한 [맵 배경 구역 분할 도구](2026-11-03-map-reveal-partition-tool-plan.md)를 추가했다. `ProjectL > 맵 배경 구역 분할`에서 크기 조절·Scene 뷰 미리보기·재분할·Undo를 제공한다. 경로/접두사는 Editor와 기존 수집기가 Tool.Runtime의 같은 계약을 사용한다. RT1 코드·정적 검토 완료, 컴파일/Unity 인수 대기.
