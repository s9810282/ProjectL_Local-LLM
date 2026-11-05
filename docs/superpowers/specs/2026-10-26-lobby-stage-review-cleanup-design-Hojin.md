# 로비·스테이지 맵 검사 후속 정리 설계

- 작성자: 호진(이호진)
- 상태: 1~4·6번과 묶음 2~8(R-a~R-f) 구현·커밋 끝(마지막 `24c21b3a3`, 10-29). 남은 것은 R-g 측정, 묶음 7 다른 작업자 합의, §1.2의 남은 낮음, Q-S6 후속이다. 진행·확인 상태는 계획.
- 범위: 2026-10-26 Lobby·StageSelect 원칙 검사가 남긴 1~6번과 10-27 5기준 지적(묶음 1~8). 다른 작업자 줄은 §11. 로비 시설 이름 조회(LB-1)는 [로비 설계](2026-10-11-lobby-design-Hojin.md)가 맡는다. Stage 실행 계층(Flow·Plan·Job)은 [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md)의 보류 절을 이 문서 §12.3 묶음 6이 풀었다.
- 계획: [검사 후속 정리 계획](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md)
- 정리: 2026-10-27 `2026-10-26-name-lookup-removal-design-Hojin.md`, `2026-10-26-required-view-dependencies-design-Hojin.md`, `2026-10-27-stage-small-cleanup-design-Hojin.md`, `2026-10-26-stage-popup-submission-design-Hojin.md`를 이 문서로 합쳤다(옛 판 git `10914570`). 2026-10-29 다시 줄임: 구현이 끝난 절은 지금 구조로 바꿔 적고, 대안 비교·검토 표·옛 판 서술은 "바뀐 것" 한 줄로 줄였다. 줄이기 전 판은 git `a8612321e`.
- 번호 주의: 2번의 결정 Q-R1~Q-R5(10-26, §3.1)와 묶음 2~8의 결정 Q-R1~Q-R12(10-28, §3.2)는 이름이 겹치는 다른 항목이다. 이 문서는 앞쪽을 "Q-R1(2번)"처럼 적는다. 계획의 Phase 이름 "R-a·R-b"도 같다.

## 1 검사 결과와 순서

2026-10-26 다른 PC 세션이 호진님이 만든 Lobby·StageSelect 코드만(git blame 호진님 줄) SOLID·SSOT·FW로 검사했다. 높음 0, FW-001~016 위반 0, 중간 8이다. 검사 결과는 채팅에만 남겼다.

| # | 지적 | 설계 | 처리 |
|---|---|---|---|
| 1 | 이름 조회: 노드 자식 다섯을 `transform.Find`로, 로비 카메라 이름 상수와 유일성 검사 | §4 | N-1 `45b20161`, N-2 `8ee9984e`. 로비 시설은 LB-1 |
| 2 | 운영 필수 의존의 null 허용 | §5 | 2번 R-a `45b20161`·R-b `b1dd4962` |
| 3 | View가 진입 요청 단계 값을 직접 해석 | §6 | `45b20161` |
| 4 | 상점·회복 "제출 중" 세 층 + 두 팝업 UI 중복 | §7 | S-a·S-b `354bd8d7d`, S-c `24c21b3a3` |
| 5 | 스테이지 맵 Scope 조립(인자 29, 생성 경로 둘) | §8 | 묶음 2(§12.3), R-a `24c21b3a3` |
| 6 | 작은 정리: 시작 노드 판정, 팝업 Layer 이름, 참조 없는 파일 셋 | §9 | C-2·C-3 `b1dd4962`, C-1 `42be34e6` |

### 1.1 10-27 5기준 점수

10-27에 전체 코드(작성자 무관)를 5기준으로 다시 매겼다. 판은 `8e5180eb`(1·2·3·6번 반영 뒤)이고, 하위 에이전트 다섯이 매긴 뒤 중간 이상은 직접 대조했다. 감점은 높음 −20·중간 −7.5·낮음 −2.5(기준마다 낮음 상한 −10)이고, 높음은 0이다. 결과는 채팅에만 남겼다.

| 기준 | 점수 |
|---|---|
| FW 연결점 | 67.5 |
| 내부 구현 | 54.5 (Lobby 60·StageSelect 52.5) |
| DB SO | 52.5 |
| UI | 52.5 |
| Job·Plan | 52.5 (덱 확정의 Core 직접 호출을 사용자 결정 예외로 빼면 60) |

검사 목록에 없던 새 발견과 처리:

- 출발이 거절돼도 화면·로그 없이 사라진다 → 묶음 R-b가 공용 안내 패널로 보임(LM5).
- View SO 여섯이 `GetData`에서 직렬화 객체를 그대로 돌려준다(DB 규칙 P2) → R-e(DM1).
- 프리팹 검사 규칙의 사본이 4~5곳 → R-e(DM2).
- `Event_Recovery.asset`의 선택지 둘이 미등록 사본 표를 가리킨다 → R-e(DM4).
- 시험 스테이지 10000이 운영 Run 후보에 든다 → 재령님 `ba9b05fe`(10-27 병합)이 `StageRootDataTable`에서 `Test`를 뺐다. `Test.asset`·`Stage_Boss.asset`은 파일만 남는다(삭제하지 않음).

### 1.2 10-27 점수의 지적 목록

10-27 점수를 낸 하위 에이전트 다섯의 보고를 옮겼다(판 `8e5180eb`, 그 뒤 병합 둘 `9a6959f5`·`358063e9`로 범위 안 운영 코드는 바뀌지 않았다). 위치(줄 번호)는 그 판 기준이라 지금은 맞지 않아 뺐고, 2026-10-29 판 `a8612321e`의 처리 상태를 단다. "확인 안 함"은 처리 기록이 없고 이번 정리에서 코드도 보지 않은 것이다. 처리한 Phase는 계획의 묶음 Phase 이름이다(R-a = 묶음 2 등, §1.3).

점수 합칠 때 바꾼 것(같은 원인은 한 기준에서만 셈): Lobby M1·StageSelect A(조립이 Factory 밖)는 FW F1로만, StageSelect D("제출 중" 세 곳)는 UI M1로만 셌고, Lobby M4(시설 종류 분산)와 StageSelect H(생존 검사 약 153회)는 낮음으로 내렸다. 낮음은 기준마다 −10까지만 깎으므로 중간을 모두 고쳐도 각 기준은 90%이고, 100%로 가려면 낮음도 고쳐야 한다.

#### ① FW 연결점 (67.5)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| F1 | 중간 | 팝업 Factory 셋·아티팩트 Factory·Stage Flow 넷을 System·LifeCycle이 직접 만들고, 먼저 넣으면 건너뛰는 null 갈래로 조립 경로가 둘 | 해결: R-a·R-b(생성 계약), R-d2(Flow 지움) |
| F2 | 중간 | Global 등록기 `JobRegisterSystem`이 상점 seed 공급(`new Random().Next`)을 가짐 | 요청: Core 요청 문서 6부(R-f, 미전달) |
| F3 | 중간 | Lobby·Stage 씬 Node 순서(−200/100/−100)가 `SceneNodeOrder` 표 밖 상수 | 제안: 용근님 메모(R-f, 미전달) |
| F5 | 낮음 | `FindFirstObjectByType`·`Camera.main` | 그대로: 출력 카메라는 Boot의 하나(Q-R4 B), 씬 루트 찾기는 Framework 제약 |
| F6 | 낮음 | 낡은 키·주석: "루트에 등록", 쓰는 곳 없는 `DeckEditAvailability` 키, "RC2/RC3에서 제공" | 일부: 주석 고침(R-f), 키 걷기는 민형·용근 메모 |
| F7 | 낮음 | `ArtifactBuffer`의 소비자 없는 번호 없는 결과 읽기 싱크 둘 | 요청: Core 요청 문서 6부 |
| F8 | 낮음 | Plan에 `IBufferQuery` 전체를 넘김(FW-101) | 제안: 민형님 메모(R-f) |
| F9 | 낮음 | `RecoveryRequestLifetime`이 Shutdown 때 공용 `_runner?.Clear()`를 부르고 종료 순서에 기댐 | 남음 |
| F10 | 낮음 | 씬 없는 예약 번호 4에 `ArtifactLifeCycle`을 등록해 빈 로딩 노드 | 제안: 민형님 메모(R-f) |
| F11 | 낮음 | `StageLifeCycle`의 `faults ?? new RuntimeFaultBoundary(...)` | 해결: R-d2(필수) |

#### ② 내부 구현 — Lobby (60)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| LM2 | 중간 | "로비 입력이 열려 있나"를 7곳이 다른 조합으로 판단, 저장 사본 넷 | 해결: R-b(판정 함수 둘, 사본 걷음) |
| LM3 | 중간 | 운영에서 늘 있는 의존(시설 실행·출발 콜백·아티팩트·오디오) null 허용 | 해결: R-b |
| LM5 | 중간 | 출발 거절을 읽지 않고 입력만 다시 엶 | 해결: R-b(질문 셋, `ShowNotice`) |
| LM6 | 중간 | 덱 버튼 숨김 두 벌, Clicker 여닫기 주인 셋 | 해결: R-b(우리 덱 버튼 줄), 로비 안내 글자(Clicker 여닫는 줄 걷음, §12.7) |
| LM4 | 낮음 | 시설 종류 지식(열거·배치판·실행 switch·개수 3)이 흩어짐 | 확인 안 함(R-e는 시설 검사 자리만 옮김) |
| LL1 | 낮음 | 연결 한 번에 같은 DB 값을 3~5번, 방 구조를 2번 검사 | 대부분 해결: R-b(바인딩 때 한 번), R-e(모델 프리팹 재검사 걷음). `LobbyViewServices`의 설정 재검사는 남음 |
| LL2 | 낮음 | 로비 덱 닫기 슬롯 −1과 입력 가능 판정이 Presenter·UI에 두 벌 | 확인 안 함 |
| LL3 | 낮음 | "덱 정보를 준비 중입니다." 고정 문구, 덱 시설은 늘 빈 목록 | 남음(`LobbyDeckPopupController`) |
| LL4 | 낮음 | 씬 연결 System이 배경음 프리셋 번호 0을 코드에 박음 | 남음(배경음은 System에 둠, 계획 §4.4) |
| LL5 | 낮음 | 아티팩트 루트를 비활성 제외·개수 확인 없이 같은 씬에서 두 번 검색 | 확인 안 함 |
| LL6 | 낮음 | Scope ↔ 출발 ↔ 상호작용 구체 타입 상호 참조, 그리지 않는데 이름이 Presenter | 해결: R-b(`LobbyDepartureFlow`, Scope 판정 함수) |
| LL7 | 낮음 | 카메라 핸들을 빌리고 쓰지 않는데 실패 정리 경로가 약 80줄 | 감점 제외(Q-R12 의도된 준비), 대여는 공용 `ViewCameraHandleLease`(R-a·R-b) |
| LL8 | 낮음 | `Start`의 `if (!CanWork) return;` 9번, 출발 갱신이 한 Tick에 두 번 | 확인 안 함 |
| LL9 | 낮음 | `LobbyCharacterVisual`의 `layer = 0` 숫자 | 남음 |

#### ② 내부 구현 — StageSelect (52.5)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| SB | 중간 | 따로 넣는 입구 때문에 운영 의존 null 허용, 오류 처리·닫기 두 갈래 | 해결: S-b(오류 경계), R-a(입구 하나, `StagePopupViewLifetime`) |
| SC | 중간 | Scope 필드 37·인자 28, 모달 판정 셋의 조건이 다름 | 해결: R-a(부품 넷, 판정 함수 셋). 덱 편집 열기 조건 내용은 그대로(Q-R3, 용근님 메모) |
| SE | 중간 | "같은 진입 요청" 판정식 사본 다섯 | 해결: R-c(`StageEntryIdentity`) |
| SF | 중간 | "상점·회복 동시 활성 불가", "Event면 회복" Core 규칙을 View가 단정, 복원 판단이 둘로 나뉨 | 해결: R-c(Buffer 상태만, 한 곳) |
| SG | 중간 | 상점·회복 컨트롤러 뼈대·보고·화면 어댑터 중복, 재열기 막기를 다르게 구현 | 해결: S-a·S-b, R-a(`StagePopupViewLifetime`) |
| SH | 낮음 | 생존 검사 약 153회, 순수 Buffer 읽기 뒤에도 | 해결: R-a(B7) |
| SL1 | 낮음 | 처리 전 요청 판정이 두 표현으로 7곳 | 해결: R-a(`HasPendingRequest` 하나) |
| SL2 | 낮음 | 바뀌지 않는 DB 값 17개를 재사용 비교에 복사, 마커 값 검증 두 번 | 일부: R-f(마커 검사 DB 한 번). 값 복사는 확인 안 함 |
| SL3 | 낮음 | 안 쓰는 상수·멤버, default와 같은 case(`ShopPopupText`·`RecoveryPopupText`·`ShopResultPolicy`) | 확인 안 함 |
| SL4 | 낮음 | 빈 구현 계약 셋, 한 줄 감싸기 `StageMapPointerBlocker` | 남음(`StageMapPointerBlocker` 그대로) |
| SL5 | 낮음 | 연출 비율 `12f/59f`·배경음 프리셋 번호 1이 코드에 | 남음 |
| SL6 | 낮음 | `Camera.main`(F5와 같은 원인) | F5와 같음 |
| SL7 | 낮음 | "방문당 1회"·완료 노드 제출 금지를 View 활성화 조건에 반영 | 확인 안 함 |
| SL8 | 낮음 | 외형이 빠지면 키 문자열로 조용히 대체 | 확인 안 함 |
| SL9 | 낮음 | 같은 실패 사유 enum의 switch 세 벌 | 확인 안 함 |
| SL10 | 낮음 | UISystem 계약이 View 네임스페이스 모델에 의존(`DeckEditDisplayModel`) | 남음(UL3 일부와 같은 원인) |

#### ③ DB SO (52.5)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| DM1 | 중간 | `GetData()`가 직렬화 객체를 그대로 돌려줌(P2): 맵 표시·노드 외형·회복 외형·아티팩트 외형·보드 설정·소모 아이템 외형 | 해결: R-e |
| DM2 | 중간 | 프리팹 검사 규칙 사본 4~5곳이 이미 어긋남 | 해결: R-e(`ViewPrefabRules`) |
| DM3 | 중간 | 로비 방 구성은 런타임에, 노드 부품은 DB 생성 때 검사 | 해결: R-e(시설 표지 DB로, `ValidateRoom`) |
| DM4 | 중간 | `Event_Recovery.asset` 선택지 둘이 미등록 사본 표를 가리킴, 참조 무결성을 DB 생성 때 안 봄 | 에셋은 해결(R-e), 검사는 요청(Core 요청 문서 6부) |
| DM5 | 중간 | 회복 표시 이름이 InGame 회복 값(10, 30)을 문자열로 복사 | 해결: R-e(TP-R11 임시 문구) |
| DL1 | 낮음 | `LobbyModelData`에 검사 캐시 `_structureValidated` | 해결: R-e |
| DL2 | 낮음 | `Data/InGame/Table/Stage/Enum/StageType.cs`·`StageMapRole.cs` 사본, 쓰는 곳 0 | 보고만(지우지 않음) |
| DL3 | 낮음 | 이름이 같은 조회 형식 둘(`StageTable`), 등록 안 된 Excel Master 중복 정의 | 확인 안 함 |
| DL4 | 낮음 | `TryGet` 없음, private `_` 없음, 인터페이스 파일 배치 제각각 | 확인 안 함 |
| DL5 | 낮음 | StageKey 전용 노드 외형이 InGame 종류·역할을 사본으로 가짐 | 확인 안 함 |
| DL6 | 낮음 | 같은 모델을 세 SO가 따로 고르고 배율을 손으로 2배 | 확인 안 함 |
| DL7 | 낮음 | DB 밖 UI SO가 유닛 이름의 둘째 원본, `DeckCharacterImagesSO` 사용처 0 | 확인 안 함 |
| DL8 | 낮음 | 영어 XML doc·예외 문구, 키 없는 문구 | 확인 안 함 |
| DL9 | 낮음 | SO `…StageData`·Data `…Detail`, 같은 키를 `chapterKey`·`MapKey`로 부름 | 확인 안 함 |

#### ④ UI (52.5)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| UM1 | 중간 | 상점·회복 UI가 Buffer·InGame View 그룹을 직접 조회해 제출, "제출 중" 세 곳 | 해결: S-b |
| UM2 | 중간 | `ShopUI`·`RecoveryUI` 약 200줄 복사본이 이미 갈라짐 | 해결: S-a |
| UM3 | 중간 | 팝업 다섯이 루트를 켜 둔 채 Panel만 토글해 UIBase 훅을 우회 | 일부: 상점·회복 열림 상태 원본 하나(S-a). 꼴은 Q-S6 후속 |
| UM4 | 중간 | `DeckEditUI` 열기 버튼을 대리자로 연결(AGENTS 금지) | 남음(용근님 파일, 처리 기록 없음) |
| UM5 | 중간 | `DeckEditUI`의 View DB 조회기·`statusText`·`ownedCards` null 허용 | 남음(용근님 파일, 처리 기록 없음) |
| UL1 | 낮음 | UI에 남은 문구 한 줄, 하드코딩 색 | 확인 안 함 |
| UL2 | 낮음 | 로비 덱만 표시 계약 없음, 닫기 슬롯 −1 두 곳 | 확인 안 함 |
| UL3 | 낮음 | UI 계약이 View 계층 형식에 의존(`IDeckEditDisplay`·`RecoveryUI`) | 일부: `RecoveryUI`는 S-b로 해소, `IDeckEditDisplay` 남음 |
| UL4 | 낮음 | `RecoveryPopupModel`의 `HealthText`·`ReceiptText`를 UI가 안 읽음 | 남음 |
| UL5 | 낮음 | 예외 문구 영어·한국어 섞임 | 일부: `ShopUI`(S-a) |
| UL6 | 낮음 | `ArtifactPopupUI`가 로비 UI인데 자리·네임스페이스가 `Screens.Stage` | 남음 |
| UL7 | 낮음 | `DeckEditUI` ↔ `UnitCardUI` 카드 채우기 사본 | 확인 안 함 |
| UL8 | 낮음 | 나가기 문구 칸을 `GetComponentInChildren`로 추정 | 일부: S-a가 기반 `Awake` 한 번으로 모음, 추정 방식은 그대로 |
| UL9 | 낮음 | `Clicker`·`StageLegendUI`의 base와 같은 재정의 넷 | 일부: `StageLegendUI`(R-f), `Clicker`는 현강님 메모 |

#### ⑤ Job·Plan (52.5, 덱 확정 예외를 빼면 60)

| ID | 심각도 | 내용 | 처리 |
|---|---|---|---|
| J1 | 중간 | "다음 미처리 명령"을 Flow와 Plan이 따로 계산 | 해결: R-d2 |
| J2 | 중간 | static `RecoveryPlan`이 View 그룹의 예약 상태를 바꿈 | 해결: R-d2(예약은 Job `Execute` 안) |
| J3 | 중간 | 원시 `CombatEntryStatus`를 약 8곳에서 해석 | 해결: R-d1·R-d2. 질문이 없는 Running·Closed·Faulted 셋은 `StageEntryAttempts`에 남음(질문은 용근님 파일이라 더하지 않음, B6) |
| J4 | 중간 | 새 Run Job 둘, NewRun/Start 고르기를 LifeCycle이 함 | 해결: R-d1(`RunStartJob`) |
| J5 | 중간 | View가 덱 확정을 Core에 직접 부름 | 사용자 결정 예외로 확정 |
| J6 | 낮음 | 임시 Job 골격 `JobType.Start`(TODO)가 운영에 남음 | 해결: R-f |
| J7 | 낮음 | 운영에서 등록하지 않는 아티팩트 ProcessCommand/Preview 경로 | 해결: R-f |
| J8 | 낮음 | `ArtifactBoardPreparation`이 Plan을 우회해 Runner에 직접 등록, 조용한 null | 해결: R-f(부트 유지, 바로 예외) |
| J9 | 낮음 | Job 안 판단: 하드코딩 챕터 `: 1`, `facts.HasBattle`을 `Decide`가 무시 | 일부: 남은 전투 확인은 `RunStartJob`(R-d1). `StageJob`의 `: 1`은 남음 |
| J10 | 낮음 | `LobbyPlan`이 출발 요청의 원시 `Status`를 읽음 | 해결: R-b |
| J11 | 낮음 | `StageFlow`의 같은 정지 검사 네 번 | 해결: R-d2(Flow 지움) |
| J12 | 낮음 | 낡은 주석("타이틀의 새 게임이 이미 NewRun을 등록한 경우") | 해결: R-d1(그 갈래 걷음) |
| J13 | 낮음 | 요청 수명 모델이 셋 | 줄어듦: R-d2(처리 확인이 같은 Tick, 여러 Tick 기다리는 것은 전투 진입 시도 하나) |

점수 밖 관찰(10-27): 참조 없는 `Screens/Stage/StageUI.cs`(삭제하지 않음), 맵 진입 거절은 `Debug.Log`뿐, 로비 덱 팝업은 실제 데이터가 없음. 확신 낮음: 비전투 이동 틈에 덱 편집을 열면 착지 뒤 복원이 모달 예외로 멈출 수 있음(SC), 입력 독점 중 덱 팝업이 밖에서 닫히면 로비 입력이 계속 막힐 수 있음(LM2), `Event_Recovery`의 미등록 사본이 기획 의도 값이면 DM4는 높음.

### 1.3 100%로 가는 순서

| 묶음 | 내용 | 주로 고치는 것 | Phase |
|---|---|---|---|
| 1 | 4번 상점·회복 제출 공통화(§7) | UM1·UM2·UM3 일부·SG, 낮음 SL3·SL7~9·UL3~5·UL8 | S-a~S-c |
| 2 | 5번 스테이지 맵 Scope 조립 | F1 일부·SB·SC·UM4·UM5, 낮음 F5·SH·SL1·SL2·SL4·SL6·SL10·UL7 | R-a |
| 3 | 로비 입력·출발 정리 | F1 일부·LM2~LM6, 낮음 LL1~LL9·UL2·UL6·J10 | R-b(+ 로비 안내 글자) |
| 4 | DB SO 정리 | DM1~DM5·DL1~DL9·SL5 | R-e |
| 5 | 진입 비교·Core 규칙 단정 | SE·SF | R-c |
| 6 | Stage Job·Plan 재구성 | J1~J4·J9·J11~J13·F8·F9·F11 | R-d1·R-d2 |
| 7 | FW 남은 둘(다른 작업자 합의) | F2(Core)·F3(`SceneNodeOrder` 주인 합의) | R-f(요청·제안만) |
| 8 | 작은 낮음 정리 | F6·F7·F10·J6~J8·UL9 | R-f |
| — | J5 | 사용자 결정 예외로 확정 | — |

다른 작업자 요청·합의가 필요한 것: 묶음 3의 Clicker(현강님, 메모), 묶음 7 전부. 나머지는 고친 뒤 공유했다(§11). 끝나면 같은 방식(블라인드 A/B)으로 다시 매긴다(계획 R-g, 예상은 §12.4).

## 2 지금 구조

1~4·6번과 묶음 2~8 뒤의 원본과 책임이다. 상세는 각 절에 있다.

| 대상 | 원본·책임 | 자리 |
|---|---|---|
| 노드 부품 | 노드 프리팹 루트의 `StageNodeViewParts` 하나가 부품 넷의 참조와 규칙을 갖는다. DB가 원본 프리팹을 한 번 검사하고 노드 표시는 복제본의 참조만 읽는다(§4.1) | `DataBaseSystem/Data/View/Table/StageMap/Item/StageNodeViewParts.cs` |
| 캐릭터(현재 위치 마커) 자리 | 노드 루트. 높이는 SO 마커 보정 한 곳 | `StageCurrentMarkerView`, `StageMapNodeMovement` |
| 씬 조립 | Factory 생성 계약 하나(`IStageMapSceneFactory`·`ILobbySceneFactory`, 구현 `StageMapSceneFactory`·`LobbySceneFactory`)가 Scope를 만든다. System은 `PostBindService`에서 서비스를 묶음 값(`StageMapViewServices`·`LobbyViewServices`, 모든 칸 필수)으로 한 번 모으고, Framework 연결·씬 노드·Scope 게시·배경음만 맡는다(§12.3) | `ViewSystem/StageMap/`·`Lobby/`, `Factory/Runtime/InGame/View/` |
| Scope | 연결 한 번의 수명 부품 묶음. 맵 = `StageMapViewScene`·`StageMapViewEntry`·`StageMapViewInput`·`StageMapViewPopups`, 로비 = `LobbyViewScene`·`LobbyViewInput`·`LobbyViewPopups` + `LobbyDepartureFlow`. `Dispose`는 만든 순서의 역순(B3) | 두 Scope |
| 입력 판정 | Scope 안 private 판정 함수(맵 `CanShowSelection`·`CanOpenDeckEdit`·`CanUseDevelopmentTools`, 로비 `IsInteractionBlocked`·`CanDepart`). 팝업 열림은 알림으로 한 곳, Buffer·Layer는 쓰는 순간 읽음(B4) | 두 Scope |
| 팝업 수명 | 상점·회복·덱 편집 컨트롤러가 작은 부품 `StagePopupViewLifetime` 하나를 가짐(B5) | `ViewSystem/StageMap/` |
| 상점·회복 팝업 | UI 공통 기반 `StageOfferPopupUI`, UI는 수신자(`IShopPopupInput`·`IRecoveryPopupInput`)만 부르고 컨트롤러가 유일한 제출자(영수증 하나)(§7.3) | `UISystem/Screens/Stage/`, `ViewSystem/StageMap/Shop`·`Recovery` |
| 카메라 핸들 | 공용 `ViewCameraHandleLease`(로비·맵). 이름 유일성·등록은 민형님 카메라 카탈로그. 출력 카메라는 Boot Main Camera 하나(`Camera.main`) | `ViewSystem/Common/` |
| 오류 경계 | 로비·맵 System·Scope·팝업과 `StageLifeCycle`이 경계 하나를 생성자 필수로 받음. "정지했나"의 원본 하나 | — |
| 진입 요청 판정 | `CombatEntryRequest`의 이름 있는 질문(§6). "같은 진입" 비교는 값 형식 `StageEntryIdentity` 하나(B6) | `LifeCycles/Combat/`, `ViewSystem/StageMap/` |
| 비전투 팝업 고르기 | `StageMapViewScope.RestoreNoncombat` 한 곳이 Buffer 상태(상점·회복)만 봄. View는 `StageType`을 읽지 않음 | `StageMapViewScope` |
| 시작 노드 | `StageProgressData.IsStart` | `BufferSystems/Stage/StageProgressData.cs` |
| 맵 팝업 Layer | `StageSelectionInputLayers.Popup`(`"StageMapPopup"`) | `ViewSystem/StageSelectionInput/` |
| 로비 출발 | 출발 요청의 질문 셋(`IsQueued`·`IsPrepared`·`IsTransitionSucceeded`)과 `CanResubmit`. 거절은 공용 안내 패널(`IUITextPresentation.ShowNotice`) 한 번, 문구 원본 `LobbyDepartureText` | `LobbyView` Buffer, `LobbyDepartureFlow` |
| Stage 실행 계층 | `StagePlan.Decide` 순서, 사실 `StageProgress`, 처리 뒤 확인 Job, 전투 진입 시도 `StageEntryAttempts`(§12.3 묶음 6) | `LifeCycles/Stage/` |
| 새 Run | `LifeCycles/RunStart/RunStartJob` 하나 + 받는 쪽 `IRunStartOutcome`(로비·Stage) | `LifeCycles/RunStart/` |
| DB 프리팹 검사 | 같은 뜻은 `ViewPrefabRules` 하나, 표마다 다른 요구는 그 표. 로비 방 시설 셋은 `LobbyVisualPrefabRules.ValidateRoom`이 DB 생성 때 | `DataBaseSystem/Data/View/` |
| 로비 안내 글자 | `LobbyPanel` 아래 묶음 `LobbyTexts`, 시설 팝업 동안 꺼짐(§12.7) | `Lobby.unity`, `LobbyCanvasView` |

## 3 결정 기록

검사 후속 전체에 걸친 사용자 지시: 다른 작업자의 **시험 코드**는 묻지 않고 호출만 맞추고 기록한다(10-26). 다른 작업자의 운영 코드 줄은 변경 정리를 넘긴다. 10-27 시험 묶음 삭제 뒤로는 새 시험을 만들지 않는다.

### 3.1 1~4·6번

| ID | 결정 | 날짜·근거 |
|---|---|---|
| Q-N1 | A 노드 루트의 부품 참조 묶음 하나 | 10-26 사용자 "Q-N1·Q-N4·Q-N6 모두 권장안(A)" |
| Q-N2 | 부품 넷 모두 노드 루트의 직접 자식 | 10-26 사용자 "4개 다 노드 아래의 자식" |
| Q-N3 | VisualRoot는 필수 | 바뀐 것: 1판 "없으면 노드 루트 허용" → 필수(10-26, §4.2-8) |
| Q-N4 | A SC-1·LB-1 커밋 뒤 착수 | 10-26. N-2는 한 파일이라 사용자 지시로 먼저 |
| Q-N5 | 해소: 다른 작업자 시험은 묻지 않고 고친다 | 10-26 사용자 지시 |
| Q-N6 | A 용근님 운영 줄(노드 강조 색 다섯·`nodeMask`)도 걷고 변경 정리를 넘긴다 | 10-26 |
| — | 아이콘 그릇은 노드 프리팹의 IconRoot를 쓴다(코드가 만들지 않음) | 10-26 사용자(같은 날 "코드가 만듦"으로 적었다가 정정) |
| Q-R1(2번) | A 운영 필수 의존은 생성·바인딩 때 null이면 예외 | 10-26 사용자 "Q-R1~Q-R5 모두 권장안으로(Q-R1·R2·R3·R5는 A, Q-R4는 B)" |
| Q-R2(2번) | A 스테이지 맵 오류 경계도 함께 | 10-26 |
| Q-R3(2번) | A 덱 편집 팝업 `CoreGroup?.Deck`(용근님 줄)도 필수 | 10-26 |
| Q-R4(2번) | B 로비 아티팩트 팝업 Factory·루트는 범위 밖, 기록만 | 10-26. 바뀐 것: 묶음 R-b가 필수로 바꿈(10-28, B1) |
| Q-R5(2번) | A 로비 먼저, 스테이지 맵은 N-1 커밋 뒤 | 10-26 |
| Q-C1 | A `StageProgressData.IsStart`(민형·용근님 파일) | 10-27 사용자 "Q-C1·C2·C4·C5는 권장안(A)으로, Q-C3은 A(출발 안내 둘 삭제)로" |
| Q-C2 | A 이름 `"StageMapPopup"`(Layer 정의 안에 한 번) + `StageSelectionInputLayers.Popup` | 10-27. 바뀐 것: 이름 상수를 이름 파일로 옮기는 단계는 뺌(10-27 사용자) |
| Q-C3 | A 출발 안내 둘(`LobbyDepartureText`·`LobbyDepartureStatusUI`) 삭제 | 10-27. 묶음 R-b가 같은 이름의 문구 원본 `LobbyDepartureText`를 다른 내용으로 새로 만듦 |
| Q-C4 | A `StageMapCanvasOrder` 삭제 | 10-27 |
| Q-C5 | A C-2 먼저(4번 전) → C-3 → C-1 | 10-27 |
| Q-S1 | A 컨트롤러가 명시적 수신자(`BindInput`)이자 유일한 제출자 | 10-27 사용자 "Q-S1~Q-S7, Q-T1 모두 A로 하는데 그래도 테스트는 조금만 수정해도 되는 정도로". 바뀐 것: 1판 `Requested` 사건 방식 철회(현강님 입력 규칙 위반, §7.2 U1) |
| Q-S2 | A 추상 기반 `StageOfferPopupUI`(프리팹·씬 무변경) | 10-27. 다른 안(위젯 부품·순수 C# 도우미)은 프리팹 재연결·두 벌이 남아 뺌 |
| Q-S3 | A 상점의 "이전 연결이 남긴 입력" 검사(`Commands.Count != 0`)는 그대로(다른 사실) | 10-27 |
| Q-S4 | A 회복 컨트롤러는 제출 입구와 상태 입구를 따로 받는다 | 10-27 |
| Q-S5 | A 수신자 인자는 문맥·ID(명령 값은 컨트롤러가 만든다) | 10-27 |
| Q-S6 | A "루트 켜 둠 + Panel 토글" 꼴은 이번에 유지, 열림 상태 원본만 기반 한 곳으로. 후속 후보 | 10-27 |
| Q-S7 | A 시험 리플렉션 도우미가 기반 형식까지 칸을 찾음(칸은 `private`) | 10-27. 시험 묶음 삭제로 할 일 없음 |
| Q-T1 | A 걷는 동작만 보던 시험은 지우고 옮길 수 있는 것은 옮김 | 10-27. 시험 최소 원칙 T1~T5(§7.5) |

3번은 결정 ID가 없다(10-27 사용자 "3번을 진행해줘"). 계획에서 정한 TP-1~TP-14(2번)·TP-1~TP-9(4번)는 계획에 있다.

### 3.2 묶음 2~8 (10-28)

<a id="125-정할-것-q-r1q-r12-권장-먼저"></a>

10-28 사용자 "참고도 참고지만 더 좋아질 수 있다는 거니까 더 좋게 만들어야겠네요"로 설계 2판(§12)을 썼고, Q-R3·Q-R5·Q-R9·Q-R10·Q-R12는 표대로, 나머지는 "모두 A"로 정했다(10-28). 계획에서 정한 TP-R1~TP-R13과 R-d 착수 때 정한 Q-Rd1~Q-Rd3은 계획 §4.

| ID | 결정 | 날짜·근거 |
|---|---|---|
| Q-R1 | A 시험 입구(`Bind*`·`Attach(root, picker)`·공개 `Root`/`Scope`·null 기본값) 모두 걷음(B1) | 10-28 "모두 A" |
| Q-R2 | A Scope = 수명 부품 넷(B3) | 10-28. 다른 안: 전투처럼 연결 객체 하나에 필드만 옮김 |
| Q-R3 | 덱 편집 열기 조건은 UI(덱 편집) 작업자 몫. 우리 판정은 내용을 바꾸지 않고, 불일치(SC)는 그쪽 메모로 | 10-28 사용자 |
| Q-R4 | B 출력 카메라 `Camera.main` 그대로 | 바뀐 것: A 씬 루트 필드 → B(10-28 사용자 "씬에 카메라는 하나만", 출력 카메라는 Boot에 있어 다른 씬 참조 불가) |
| Q-R5 | Clicker 주인: Boot는 씬 시작 기본 UI, 씬 View는 자기 화면과 잠깐 숨기기만. ① 덱 버튼 닫기 우리 줄(`LobbyViewScope`)을 걷음 ② Clicker 주인 셋은 현강님 몫 | 10-28 사용자 "권장안으로". ②는 §12.7이 대신함 |
| Q-R6 | 출발 거절은 공용 안내 패널 `ShowNotice` | 바뀐 것: A 로비 화면 안내 부품 → 공용 안내 패널(10-28 사용자 "단순 2") |
| Q-R7 | A 새 Run Job 하나를 Stage·로비가 같이 씀 | 10-28. 다른 안: Boot 개발 진입으로 옮김(용근님 파일) / 그대로 |
| Q-R8 | A View 값 형식 `StageEntryIdentity`(B6) | 10-28. 다른 안: `CombatEntryRequest`에 질문 추가(용근님 파일) |
| Q-R9 | `StageType` 셋 유지, 합치기 요청 없음. View는 Buffer 상태로 팝업을 고름(§12.6) | 바뀐 것: 처음 "둘로 합치기" → "3개 유지"(10-28 사용자). Q-R9b는 없어짐 |
| Q-R10 | 묶을 수 있으면 묶는다(검사 규칙 클래스 하나에 정적 함수) → `ViewPrefabRules` | 10-28 사용자. 실행 때 "같은 뜻만 합침"(10-29) |
| Q-R10b | A 로비 시설 표지를 DB 입력 부품으로 옮겨 DB 생성 때 검사 | 10-28 "모두 A" |
| Q-R11 | A 회복 이름에서 숫자를 빼고 숫자는 상세(Buffer 값)만 | 10-28. 문구는 기획 확인 전 임시(TP-R11) |
| Q-R12 | 로비 카메라 핸들 빌림 유지(화면 대상을 볼 때 움직일 준비). LL7 감점 제외 | 10-28 사용자 |
| Q-R12b | A 로비·맵 핸들 대여 코드를 공용 부품 하나로 | 10-28 "모두 A" |
| Q-G5 | B 아티팩트 화면 동안 로비 글자 숨김 | 10-28 사용자 "아티팩트에선 숨김". 권장이던 A(그대로 보임)에서 바꿈 |
| — | 원칙 B4: 매 Tick 검사 대신 알림 + 쓰는 순간 읽기 | 10-28 사용자(계획 TP-R 확인 때) |
| — | B3·B4: 정리 목록 부품·사실 struct + Policy 정적 클래스 없이 역순 호출·Scope 판정 함수 | 10-28 사용자 "권장대로 단순화" |

## 4 1번 이름 조회 걷기

### 4.1 노드 부품 묶음 N-1

- 문제였던 것: 노드 자식 다섯(`HitTarget`·`ClearedIndicator`·`IconRoot`·`VisualRoot`·`CurrentMarkerAnchor`)을 이름 문자열로 찾았다. 이름 약속이 DB 파일 넷과 View 파일 하나에 나뉘었고, `VisualRoot`·`CurrentMarkerAnchor`는 DB 검사에 없었다.
- 고른 접근(Q-N1): 노드 루트의 부품 참조 묶음 하나. 프리팹을 복제하면 Unity가 참조를 복제본 자식으로 바꿔 주므로 노드마다 검색이 없다. 이름 상수 모으기(이름을 바꾸면 여전히 깨짐)와 자식마다 표지 컴포넌트(역할이 고정이라 맞지 않음)는 뺐다.

계약 `StageNodeViewParts`:

- `internal sealed class StageNodeViewParts : MonoBehaviour`, `[DisallowMultipleComponent]`. DB 검사가 봐야 하므로 DB 쪽 `StageMapViewDataSO` 옆에 둔다(전투 `CombatAnimationEvents` 선례). 저작 입력과 규칙 함수만 있고 런타임 상태·서비스 참조는 없다.
- 필수 칸 넷과 읽기: `hitTarget`(Collider)·`clearedIndicator`(GameObject)·`iconRoot`(Transform)·`visualRoot`(Transform).
- `static StageNodeViewParts Of(GameObject node)`: 없으면 `InvalidOperationException`. DB 검사와 노드 표시가 함께 부른다.
- `Validate()`: 넷 모두 노드 루트의 직접 자식(Q-N2). `clearedIndicator` 아래 Renderer. `iconRoot`는 꺼짐·빈 Transform·위치 0·회전 0·크기 1(`StageNodeIconLayout.Configure`가 계산한 값을 덮어쓰므로 저작 값이 없다).

소비자:

- `StageMapViewData`: 부품 검사는 `StageNodeViewParts.Of(nodePrefab).Validate()` 한 번.
- `StageNodeView`: 노드를 만들 때 `Of(instance)`를 한 번 부르고 노드 ID별 사전에 둔다. 건물은 `VisualRoot` 아래, 아이콘은 `IconRoot` 아래.
- 캐릭터 자리는 노드 루트다. `StageMapNodeMovement`는 목적지를 `target.position`에서 계산한다.
- 클릭: `TryPickNode(Ray ray, float pickDistance, out int nodeId)`, `StageNodePicker(Camera camera, StageMapViewDisplay display, float pickDistance)`.
- 프리팹 `Prefabs/StageSelect/Infrastructure/StageMapNode.prefab`: 루트에 부품 묶음, 자식 CurrentMarkerAnchor·Highlight 지움. GUID·남은 값·민형님 `9b39cb02`(프레임 최적화) 값은 그대로다.
- 코드·프리팹·SO 에셋(`StageMapView_Default.asset`)은 한 커밋이어야 한다. 프리팹에 부품 묶음이 없으면 부트의 DB 생성이 실패한다.

### 4.2 걷은 필요 없는 것

1판 뒤 사용자 지적("노드 부분에 필요 없는 부분이 있다")을 코드·프리팹으로 대조해 찾았다. 번호는 채팅 보고 때 순서다(2번 IconRoot는 남기기로 해 빠짐).

| # | 대상 | 근거 | 작성자 |
|---|---|---|---|
| 1 | 자식 CurrentMarkerAnchor | 노드 루트와 같은 자리의 빈 오브젝트. 캐릭터 높이는 SC-1이 SO 값으로 옮김 | 프리팹 |
| 3 | 자식 Highlight | 꺼져 있고 쓰는 코드가 없다 | 프리팹 |
| 4 | SO 노드 강조 색 다섯과 읽기·검사, `RequireFiniteColor` | 읽는 코드·쓰인 기록이 없다 | **용근**(`39b634fe`) |
| 5 | SO 노드 이펙트 둘(hoverEffect·selectEffect) | 읽는 코드가 없다 | 호진(`f76d9c44`) |
| 6 | SO `nodeMask`와 "Node Mask must include" 검사 | DB가 마스크에 HitTarget Layer를 강제해 거름이 늘 통과. `pickDistance`는 남김 | **용근**(`39b634fe`) |
| 7 | `TryGetNodeId`와 사전 `_nodeIdsByRoot` | 운영에서 부르는 곳 0 | 호진 |
| 8 | VisualRoot "없으면 노드 루트" 갈래 | 프리팹에 늘 있고 건물 높이를 가진다 | 호진 |

IconRoot(2번)는 남긴다(사용자 결정 10-26). 그릇이 있어야 배치가 아이콘 프리팹 루트가 아니라 그릇을 덮어써 아이콘의 저작 Transform과 켜짐 상태가 보존된다.

### 4.3 로비 카메라 N-2

- `LobbySceneRoot`의 상수 `CameraName`·이름 비교·`ValidateUniqueCameraName`을 지웠다. 남은 검사는 참조 있음·같은 씬·직교·Follow/LookAt 없음·`LobbySceneLayout.Validate`다.
- 이름 유일성과 등록 여부는 민형님 카메라 카탈로그(`CameraCatalogSystem`)가 씬 연결 때(중복)와 `GetHandle` 때(없는 이름) 검사한다. 상수는 같은 사실을 한 번 더 적은 것이었다. 카탈로그 API·Framework는 바꾸지 않았다.
- 핸들은 직렬화 참조에서 이름을 꺼내 빌린다. 바뀐 것: 설계 "`LobbyViewScope`의 이름 꺼내기 그대로" → 컨트롤러가 참조를 받음(10-26 사용자) → 묶음 R-b가 컨트롤러를 지우고 공용 `ViewCameraHandleLease`를 `LobbyViewScene`이 씀(10-28, Q-R12b).

### 4.4 바뀐 동작

화면에 보이는 동작은 같다.

| 경우 | 전 | 후 |
|---|---|---|
| 노드 자식 이름 변경 | 깨짐 | 자유(연결만 다시) |
| 노드 루트에 부품 묶음 없음, VisualRoot 누락 | 묶음 없음, VisualRoot는 오류 없이 노드 루트 | DB 생성 때 예외 |
| 노드 강조 색·이펙트·마스크 칸 | 효과 없이 보임 | 칸이 사라짐 |
| 클릭 | 마스크 거름(늘 통과) + 거리 제한 | 거리 제한만, 결과 같음 |
| 로비 카메라 이름이 `"LobbyCamera"`가 아님 | 예외 | 허용 |
| 로비 카메라 이름 중복 | `Validate` 예외 | 카탈로그가 씬 연결 때 예외 |

## 5 2번 운영 필수 의존

### 5.1 원인과 기준

운영 조립은 늘 실제 값을 넘긴다. 그런데 시험 조립이 운영 입구(`PostBindService`)를 건너뛰고 `Bind*`만 골라 불러, 운영 코드가 그 빈칸을 "없으면 기능을 끈다"로 받아 주었다. 운영에서 같은 의존이 빠져도 예외 없이 기능만 꺼졌다("클릭해도 아무 일 없음").

기준(Q-R1(2번) A, 전투 2라운드 V2-g "운영 입구 필수"와 같음): 운영 필수 의존은 생성·바인딩 때 null이면 예외로 막는다. 갈래를 두 종류로 나눈다.

- **의존 없음 갈래 — 걷는다.** "그 서비스가 아예 안 들어왔으면 기능을 끈다."
- **수명 중간 갈래 — 남긴다.** "아직 만들기 전이거나 시작이 중간에 끝났다"(`Dispose`의 `?.` 등). 준비 뒤에만 지나는 `?.`는 직접 호출로 바꿨다.

이 규칙은 [View 개발 안내](../viewDocs/view-development-guide-Hojin.md) 의존성 표에 한 줄로 있다. 묶음 2~8의 B1(운영 경로 하나)이 이 규칙을 `Bind*`·시험 입구가 없는 꼴로 끝까지 밀었다.

### 5.2 로비 (R-1, R-2)

- 2번 R-a: 오류 경계를 `LobbyViewSystem`·`LobbyViewScope` 생성자 필수로, 아티팩트 팝업용 경계를 따로 만들던 `?? new RuntimeFaultBoundary(...)`를 지워 "정지했나"의 원본이 하나가 됐다. 출발 Buffer 싱크도 필수.
- 지금(묶음 R-b 뒤): `Bind*`·`RequireConnectionServices`는 없다. 서비스는 `LobbyViewServices`(20칸, 모두 필수)가 만들 때 한 번 검사하고, 출발 싱크를 쓰는 곳은 `LobbyDepartureFlow` 하나다.

### 5.3 스테이지 맵 (R-3, R-4, R-5)

- 2번 R-b: 오류 경계 필수(Scope의 "경계가 없으면 던짐" 갈래를 지워 늘 경계에 보고), 결과·진입 시도 필수, 상점·회복 화면·Factory와 덱 편집 Factory 필수(Q-R3(2번), 용근님 줄 `38869ec1`).
- 결과·진입 시도가 필수인 까닭: 진입 관찰자 `StageMapEntryObserver`가 없으면 진입 요청이 Buffer에서 지워지지 않아, `StageMapInteraction`의 "처리 전 요청이 있으면 새 요청 안 함" 때문에 첫 클릭 뒤 새 진입이 영구히 막혔다.
- `StageScreenRootUI.ValidateComposition`: `shopUI`·`recoveryUI`가 null이면 `InvalidOperationException("[StageScreenRootUI] {필드} 연결이 필요합니다.")`(현강님 `AGENTS.md` 규칙).
- 지금(묶음 R-a 뒤): 서비스는 `StageMapViewServices`(27칸, 모두 필수)로 들어오고, 팝업 Factory 셋은 `StageMapSceneFactory`가 만든다. `BindEntry`·`BindDeck`·`RequireConnectionServices`·두 생성 경로는 없다.

### 5.4 바뀐 동작

운영 조립(지금 연결 그대로)은 같다.

| 경우 | 전 | 후 |
|---|---|---|
| 오류 경계 없이 로비·스테이지 맵 생성 | 경계를 따로 만들거나 "정지 아님"으로 진행 | 생성자에서 예외 |
| 출발 싱크 없이 로비 연결 | 출발 버튼이 조용히 안 됨 | 예외 |
| 결과·진입 시도 없이 스테이지 맵 연결 | 첫 진입 뒤 영구 막힘 | 예외 |
| 상점·회복 화면이나 Factory, 덱 Factory 없음 | 노드에 들어가도 화면 없음 | 예외 |
| Scope 정리 중 표시 소유권 손실, 비전투 복원 오류 | 경계가 없으면 던짐 | 늘 경계에 보고 |

<a id="55-범위-밖으로-둔-이웃"></a>

### 5.5 범위 밖으로 둔 이웃 (처리)

| 위치 | 처리 |
|---|---|
| 로비 아티팩트 팝업 Factory·루트 | Q-R4(2번) B로 기록만 → 묶음 R-b가 필수화(Factory 안 인자까지) |
| 오디오 `_audio?.`(로비·맵) | 묶음 R-a·R-b가 걷음(배경음·로딩은 System 필드) |
| `LobbyViewScope`의 `_actions ?? new LobbyFacilityActions(...)` | 묶음 R-b가 필수로 |
| 스테이지 맵 Scope의 두 생성 경로 | 묶음 R-a |
| 상점·회복 컨트롤러·Factory 안 `_faults == null` | S-b(TP-6) |
| System의 `_shopInput?.Clear()` | 묶음 R-a로 없어짐 |
| 덱 편집 Factory·컨트롤러의 `_faults == null`(용근님) | 묶음 R-a가 필수로(용근님 메모) |
| 상점 컨트롤러 `presentation = null` 기본값 | 남음 |

## 6 3번 진입 요청 이름 있는 질문

- 문제였던 것: `CombatEntryRequest` 머리말 규칙은 "소비자는 단계 값 대신 이름 있는 질문을 읽는다"인데 "준비 완료" 질문이 없어, 같은 식(`Status == Prepared && IsJobCompleted`)이 요청 안과 View 두 곳에 따로 적혀 있었다.
- 범위: View 쪽만(10-27 사용자 "3번을 진행해줘"). 따로 설계·계획 문서는 쓰지 않았다.
- 계약: `CombatEntryRequest.IsPrepared => Status == CombatEntryStatus.Prepared && IsJobCompleted`. "Core가 진입을 수락해 준비를 마쳤고 진입 Job도 끝났다"는 뜻이고 `Result`는 늘 수락이다. 파일은 용근님 `LifeCycles/Combat/CombatEntryRequest.cs`이고, `TryCompleteMapPresentation`이 이 질문을 쓰게 바꾼 줄은 호진님 줄이다. 전이 표·Job은 그대로다.
- View는 `StageMapEntryObserver` 셋·`StageMapNodeMovement` 하나가 질문(`IsPrepared`·`IsCancelled`·`IsActive`·`IsRejected`)을 읽는다. `ViewSystem/`에서 `CombatEntryStatus` 값을 직접 비교하는 곳은 0이다.
- 남은 직접 비교: 용근님 `CombatEntryTransitionJob`의 `switch`(요청을 움직이는 실행 쪽), Stage `StageEntryAttempts`의 Running·Closed·Faulted 셋(질문이 없음, R-d2, §1.2 J3). Stage의 나머지는 R-d1·R-d2가 질문으로 바꿨다.

## 7 4번 제출 중 하나와 팝업 공통 기반

관련 기능 설계는 [상점 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md)와 [회복 설계](2026-10-11-noncombat-event-recovery-design-Hojin.md)다. 결정은 §3.1(Q-S1~Q-S7·Q-T1), 결과는 계획 §3.9~§3.12.

- 범위: `ShopUI`·`RecoveryUI`, 표시 계약 `IShopPopupDisplay`·`IRecoveryPopupDisplay`, `ShopPopupController`·`RecoveryPopupController`와 Factory 둘. 회복 요청 소유자(`RecoveryRequestLifetime`)와 상점 입력 Buffer(`ShopViewBuffer`)는 동작을 바꾸지 않았다.
- 범위 밖: Stage 실행 계층(묶음 6), 카드 `StageOfferCardView`, 모델 Builder, 같은 "루트 켜 둠 + Panel 토글" 꼴의 다른 팝업(`DeckEditUI` 용근님, `ArtifactPopupUI`).
- 바뀐 것: 1판 "UI가 `Requested` 사건으로 의도를 알림" → 같은 폴더 선례(`DeckEditUI`·`IDeckEditInput`)와 같은 명시적 수신자 `BindInput(...)`(10-26 2판 요청 "UI 작업자나 FW 작업자의 구조에서 규격과 규칙을 적절하게 사용하는지").

### 7.1 옛 구조와 문제

- "제출 중"(구매·선택·나가기가 결과를 받을 때까지)을 세 층이 따로 들었다. 상점은 UI(`_submissionPending`·`_pendingNumber`)·컨트롤러(`_pending`)·입력 Buffer(명령 목록), 회복은 UI(번호·명령·세대 값)·컨트롤러(UI 번호를 다음 Tick에 베낌)·요청 소유자.
- UI가 입력 Buffer·요청 소유자에 직접 제출했다. `RecoveryUI`(Global UI)가 InGame View 그룹의 `IRecoveryRequestSubmitter`를 알았다. 제출이 던지면 EventSystem이 삼키므로 `SubmissionFailed` 사건으로 우회했다.
- 두 UI는 약 150줄이 같고 이미 갈라져 있었다.

| # | 차이 | 상점 | 회복 | 지금 |
|---|---|---|---|---|
| D1 | `Hide` 안 닫힘 알림에서 다시 열린 표시 보호 | 없음 | 있음 | 기반이 둘 다 보호 |
| D2 | 제출 중 숨김·재표시로 낡은 번호를 막는 세대 값 | 없음 | 있음 | UI 제출이 없어져 걷음(컨트롤러 표시 토큰이 대신함) |
| D3 | 제출 잠금 해제 시점 | 처음 연결·정리 때만 | 표시마다 | 컨트롤러 `CanSubmitNow` 한 규칙 |
| D4 | 나가기 문구 | 모델 값(천명님 줄) | 프리팹 문구 | 모델 값이 없으면 프리팹 문구 |
| D5 | 나가기 허용 | 늘 허용 | 모델 `CanComplete` | 추상 `CanExit` |
| D6 | 그린 문맥이 비었는지 검사 | 있음 | 없음 | 추상 `HasRendered` |
| D7 | 오류 문구 | 영어 | 한국어 | 한국어 |

다른 사실과 섞지 않는다: 상점 컨트롤러의 `_input.Commands.Count != 0`은 "이전 연결이 남긴 입력이 있나"(Q-S3), 회복 요청 소유자의 `HasPending`은 씬과 무관한 "런타임에 회복 요청이 있나"다. 둘 다 화면의 "내 제출"과 다르다.

### 7.2 UI 규칙·FW 대조

읽은 원본: `Assets/AGENTS.md`, `UISystem/UI-lifecycle.md`·`UIValidation.md`, `Screens/Stage/BattleChangerUI.md`·`DeckButtonUI.md`, `UIBase`·`UIWidgetBase`·`UIGroup`·`UIInputBinding`(모두 현강님), `framework-programmer-guide.md` FW-008·009·013·101·103·202, DR-008·010. 지킨 것: FW-008·FW-202 `UIBase` 등록, FW-009 외부 개폐는 `IUISystemService`, AGENTS 버튼 입력(`UIInputBinding` 한 번), 동적 위젯은 `UIWidgetBase`, 필수 연결 예외, FW-103 구독 수명. FW-001~016 위반은 없다.

| # | 규칙(원본) | 옛 구조 | 처리 |
|---|---|---|---|
| U1 | AGENTS "UI views … receive explicit input calls", BattleChangerUI.md "클릭 → 명시적 수신자 → … → 입력 Buffer" | UI가 직접 `Submit`하고 번호·명령을 듦, 실패를 사건 구독으로 | 명시적 수신자(§7.3) |
| U2 | AGENTS "서비스는 해당 Group/Context 경로로", FW-101 | UI가 Buffer 그룹·InGame View 그룹에서 입구를 꺼냄(계층 방향 역전) | U1로 `BindUIService`의 서비스 읽기 없어짐 |
| U3 | — | 1판 `Requested` 사건 | 철회 |
| U4 | UI-lifecycle "활성 상태 변경과 정리는 `UIBase.Open/Hide`와 훅에서" | 루트를 켜 둔 채 Panel만 토글, 부모 Canvas가 꺼지면 `_isOpen`이 참으로 남음 | 관찰. 꼴 유지, 열림 상태는 기반 한 곳(Q-S6), 현강님 판단 부탁(상점 UI 안내 3부) |
| U5 | 코딩 규칙(예외 문구 한국어) | `ShopUI` 영어 | D7 |

### 7.3 지금 구조

<a id="75-설계"></a>
<a id="74-고른-접근"></a>

**공통 기반** `GlobalSystem/UISystem/Screens/Stage/StageOfferPopupUI.cs`, `public abstract class StageOfferPopupUI : UIBase, IUIInputReceiver`.

- 갖는 것: 직렬화 칸 여덟(`panel`·`cardArea`·`cardPrefab`·`goldText`·`statusText`·`statusBackground`·`exitButton`·`cardSpacing`, 이름·툴팁 그대로, `private` — Unity가 상속한 private 칸을 이름으로 저장해 프리팹·씬 값이 그대로 남는다), `ParentID => UIId.StageScreenRoot`, `IsOpen`·`OpenStateChanged`(열림 상태 원본 하나), `protected void Awake()`, `Open`·`Hide`(D1), `ShowStatus`, `SetCanSubmit`, `Validate`(화면 이름은 형식 이름), `RenderCards(goldLabel, exitLabel, count)`(나가기 문구가 비면 `Awake` 때 읽은 프리팹 문구, D4), `RefreshInteractability`, `Click`의 잠금·나가기 허용·슬롯 검사.
- 하위가 답하는 것: `HasRendered`(D6)·`CanExit`(D5)·`CanAcceptInput`(수신자 있음)·`CardContentAt`(하위는 `Render` 동안만 모델을 든다)·`RequestExit`·`RequestCard`.
- 갖지 않는 것: 번호, 명령, 세대 값, 서비스 참조, 제출 예외 처리. **제출하지 않는다.** 수신자가 없을 때 입력 무시는 AGENTS의 "바인딩 전 대기"라 정상 경로다.

**`ShopUI`·`RecoveryUI`**(각 약 50줄): 그린 문맥과 카드 ID 목록, `Render(model)`, 수신자 하나(`BindInput`, null이면 끊음). `RequestExit`·`RequestCard`는 **그린 문맥과 ID로** 수신자를 부르기만 한다. `RecoveryUI`는 InGame View 계층을 모른다.

**수신자 계약**(UI 네임스페이스, 선례 `IDeckEditInput`, 구현은 View 컨트롤러):

- `IShopPopupInput { Purchase(ShopVisitContext, ShopOfferId); Complete(ShopVisitContext); }`
- `IRecoveryPopupInput { Select(RecoveryEntryContext, RecoveryOptionId); Complete(RecoveryEntryContext); }`
- 인자는 명령 값이 아니라 문맥·ID다. 명령 값은 받는 쪽이 만든다(Q-S5).

**표시 계약**: 상점·회복 모두 `IsOpen`·`OpenStateChanged`·`BindInput`·`Render`·`ShowStatus`·`SetCanSubmit` 여섯(상점 8 → 6, 회복 10 → 6). `SubmissionFailed`·`CompleteSubmission`·`ResetSubmissionState`·`PendingNumber`·`PendingCommand`는 없다.

**컨트롤러 = 수신자이자 유일한 제출자**(`DeckEditPopupController`와 같은 순서):

1. `Start`: `SetCanSubmit(false)` → `BindInput(this)`. `Dispose`: 현재 연결일 때만 `BindInput(null)`(공유 화면을 다음 연결이 이미 잡았을 수 있음), 영수증 지움.
2. 수신자 메서드: 허용(`CanSubmitNow` — 표시·결과 처리·수신자가 같이 부르는 한 계산)과 그린 문맥 일치를 보고 → **먼저** 상태 문구 비우기·`SetCanSubmit(false)`(같은 프레임 두 번째 클릭·키보드 Submit 차단) → 제출 → 영수증(번호·명령·표시 토큰). 제출이 던지면 `try/catch`로 오류 경계에 보고. 제출 중 표시가 바뀌면 영수증을 그 표시에 묶지 않는다(옛 D2).
3. `Tick`: 영수증으로 결과를 대조한다. UI에 묻지 않는다.
4. 결과 처리 끝: 영수증을 지우고 `SetCanSubmit(…)`.

- 상점 특이점: 영수증이 없을 때만 Buffer의 같은 방문 명령을 넘겨받는다(계획 TP-4). Buffer 제거·정리·"남은 입력" 검사는 그대로(Q-S3).
- 회복 특이점: 생성자에 제출 입구 `IRecoveryRequestSubmitter`(상태 입구와 따로, Q-S4). Factory는 그룹의 `RecoveryRequests`(옛 UI가 꺼내던 것과 같은 객체)를 받는다.
- 두 컨트롤러·Factory는 오류 경계 필수다(계획 TP-6). 팝업 수명 공통 부분은 묶음 R-a 뒤 `StagePopupViewLifetime`이 맡는다(B5).

<a id="76-바뀌는-동작"></a>

### 7.4 바뀐 동작

| 동작 | 전 | 후 |
|---|---|---|
| 회복 결과 대조 시작 | 제출 다음 Tick에 UI 번호를 베낌 | 제출 순간 영수증(한 Tick 빠를 수 있음, 화면 차이 없음) |
| 상점 닫힘 알림 안 재열림 | Panel이 꺼짐 | 켜진 채 유지(D1), 운영에서 닿는 경로 없음 |
| 상점 나가기 문구가 빈 값 | 직전 문구 유지 | 프리팹 문구(D4) |
| 상점 제출 잠금 | 처음 연결·정리 때만 풂 | 영수증이 없으면 늘 허용 계산. 이전 연결 명령을 넘겨받아 기다리는 동안은 잠김 |
| 제출 예외 | UI → 사건 → 컨트롤러 → 오류 경계 | 수신자(컨트롤러) → 오류 경계(보고 내용 같음) |
| 그린 방문과 다른 방문의 의도 | Buffer에 들어간 뒤 넘겨받지 않음 | 제출 전에 버림 |
| 회복이 그린 문맥 없이 입력 | 닿는 경로 없음 | 입력을 받지 않음(D6) |
| 오류 문구 | 상점 영어 | 한국어(D7) |

### 7.5 시험 최소 원칙 T1~T5

<a id="77-시험-영향과-최소-원칙"></a>

10-27 사용자 "테스트는 조금만 수정해도 되는 정도로"로 정했다. 같은 날 용근님 `660a10e7`(병합 `358063e9`)이 `ProjectL.Tests.Framework` 시험 묶음을 통째로 지워 다섯 모두 할 일이 없어졌다(T5는 10-27 사용자 "테스트 코드는 만들지 말아줘"로도 취소).

| # | 원칙 |
|---|---|
| T1 | 확인하는 내용은 그대로, 호출만 맞춘다 |
| T2 | 시험 도우미 수신자 하나가 실제 Buffer·요청 소유자로 넘긴다 |
| T3 | 운영 조립을 지나는 시험은 고치지 않는다 |
| T4 | 걷는 동작만 보던 사례는 지운다 |
| T5 | (취소) 새 시험은 컨트롤러로 옮겨 간 운영 동작 둘만: ① 첫 입력이 제출 전에 잠가 같은 프레임 두 번째 입력이 제출되지 않음 ② 제출 중 표시가 바뀌면 영수증을 그 표시에 묶지 않음 |

T5의 두 동작은 재생 확인으로만 본다(계획 §2).

## 8 5번 스테이지 맵 Scope 조립

- 지적: `StageMapViewScope` 생성자 인자 29, 생성 경로 둘(시험 입구 `Attach(root, picker)`와 씬 입구).
- 처리: 4번 뒤로 미뤘다가 묶음 2(§12.3)로 설계했고 묶음 R-a가 구현했다(`24c21b3a3`). 2번이 넘긴 두 생성 경로와 4번 TP-9(System `_shopInput?.Clear()`)도 거기서 걷혔다.

## 9 6번 작은 정리

다른 작업자 사용 확인: 10-27 `45b20161`의 `git blame`과 원격 31개 브랜치에서 이름·GUID를 찾았다. 묶음 셋은 파일이 겹치지 않아 따로 되돌릴 수 있다.

### 9.1 C-1 시작 노드 판정

- `StageProgressData.IsStart => StageKey == 0`("시작 노드는 실제 스테이지가 없어 StageKey가 0"). 파일은 민형님 43줄·용근님 9줄이다. 저장하지 않는 계산 읽기라 DB 규칙 P7에 걸리지 않는다.
- View 일곱 곳(호진님 줄)이 `IsStart`/`!IsStart`를 읽는다.
- 바꾸지 않은 것: `StageNodeVisualTable`의 `StageKey == 0`(외형 데이터의 공통 외형, 뜻이 다름), Core의 같은 판정. Stage(`StageLifeCycle`)에 있던 같은 판정은 묶음 6 뒤 `LifeCycles/Stage`에 남아 있지 않다(10-29 검색).
- 뺀 안: View 전용 규칙 함수, Core 스냅샷의 `IsStart`를 Buffer가 실어 나름(Core 영역).

### 9.2 C-2 팝업 입력 Layer

- Framework `InputLayer.Equals`는 이름만으로 같음을 판정한다. 이름이 곧 Layer의 정체다.
- 문제였던 것: 상점·회복·덱 편집이 함께 쓰는 Layer 이름이 `"ShopPopup"`이었고, 맵 팝업 Layer만 `ViewSystem/StageMap/StageMapPopupInputLayers`에 따로 있었다.
- 지금: `StageSelectionInputLayers.Popup = new("StageMapPopup", StageSelectionInputNames.CommonMap)`. 상점·회복·덱 편집(그리고 증강) 공통 모달 하나이고, 맵 선택 액션 맵을 끄고 공용 맵만 켜며, 버튼은 EventSystem으로 받고 ESC 닫기는 없다. "이름으로 같음을 판정하므로 화면 안에서 하나여야 한다"를 주석에 적었다.
- 이름 문자열은 Layer 정의 안에 한 번 적는다(10-27 사용자). 팝업 Layer는 같은 이름의 액션 맵이 없어 이름 파일로 옮기지 않는다. 이름 문자열은 씬·에셋·저장 데이터에 없다.

### 9.3 C-3 참조 없는 파일 삭제

| 파일 | 작성자 | 안 쓰이게 된 때 |
|---|---|---|
| `ViewSystem/Lobby/LobbyDepartureText.cs` | 호진 28줄·**용근 2줄**(10-20 시작 덱 거절 문구) | `3bc33b20`(10-20 「로비에 있는 Text 임시 조치」) |
| `GlobalSystem/UISystem/Screens/Lobby/LobbyDepartureStatusUI.cs` | 호진 | 같은 커밋 |
| `ViewSystem/StageMap/StageMapCanvasOrder.cs` | 호진 | `ee55d650`(10-26)에서 만들었고 생성하는 코드가 처음부터 없음 |

- 셋 다 `.meta`와 함께 지웠다. 지우기 전에 코드·씬·프리팹·에셋 참조(이름·GUID)가 0임을 다시 확인했다.
- `LobbyDepartureStatusUI`는 예전 판 `Lobby.unity`를 가진 원격 브랜치 여섯(`8-map1-라이팅-텍스쳐-수정`, `AJRLastDance`, `feature/Hojin/Lobby`, `feature/Hojin/Terrain_Tornado`, `feature/Hojin/View_Refactory`, `feature/Yonggeun/Ref-EventPresenter`)에서 씬 GUID로 참조된다. 그 씬을 그대로 가져오면 Missing Script가 된다(공유 메모).
- 바뀐 것: "출발 거절 사유는 화면에 나오지 않음, 다시 만들 때는 로비 화면 자식 부품 + 좁은 표시 계약으로 따로 설계" → 묶음 R-b가 공용 안내 패널 `ShowNotice`와 새 문구 원본 `LobbyDepartureText`(거절 사유 → 한국어, 옛 파일의 거절 갈래 문구만 가져옴)로 보인다(Q-R6, 10-28).

## 10 보류와 결정 대기

| 항목 | 상태 |
|---|---|
| R-g 블라인드 A/B 측정 | 미착수(계획 §4.10, 예상 §12.4) |
| 묶음 7: F2 seed·F3 `SceneNodeOrder`·F8·F10 | 요청·제안을 써 둠, 다른 작업자 답 대기(§11) |
| §1.2의 남은 낮음·"확인 안 함" 항목 | 처리 범위를 정하지 않음 |
| UM4·UM5(`DeckEditUI`, 용근님) | 처리 기록 없음 |
| 5기준 추가 기준 후보(시험 구조·수명·오류 처리·기능 변경 독립성·DB 에셋 위생·씬·프리팹 연결) | 사용자 선택 대기 |
| Q-S6 후속: "루트 켜 둠 + Panel 토글" 꼴 | 프리팹·씬과 `DeckEditUI`·`ArtifactPopupUI`를 함께 정할 때, 현강님 판단 부탁 중 |
| Clicker 남은 것(`Clicker.cs`·`Clicker.md`·`UIId.Clicker`·자동 복귀 기능) | 현강님 결정(§12.7) |
| `LobbyViewServices`의 설정 재검사 | 걷을지 따로 정함(계획 §2) |

## 11 다른 작업자 영향

| 누구 | 파일·줄 | 한 것 | 공유 |
|---|---|---|---|
| 용근 | `StageMapViewDataSO`의 노드 강조 색 다섯·`nodeMask`(`39b634fe`) | 지움(Q-N6) | [노드 설정 메모](../handoffs/2026-10-26-stage-node-settings-share-yonggeun-Hojin.md) |
| 용근 | `StageMapViewSystem.PostBindService` 덱 편집 줄(`38869ec1`) | `BindDeck`·필수화(Q-R3(2번)), 묶음 R-a 뒤 Factory 조립 | 같은 메모의 R-b 절 |
| 용근 | `LifeCycles/Combat/CombatEntryRequest.cs` | `IsPrepared` 추가 | [진입 요청 질문 메모](../handoffs/2026-10-27-combat-entry-request-question-share-yonggeun-Hojin.md) |
| 용근 | `StageMap/Deck/DeckEditPopupController`·`DeckEditPopupViewFactory` | Layer 참조 한 줄(C-2), 수명 부품 `StagePopupViewLifetime` 사용·오류 경계 필수(묶음 R-a), 열기 조건 불일치(Q-R3) | [팝업 Layer 메모](../handoffs/2026-10-27-stage-map-popup-layer-share-yonggeun-Hojin.md) |
| 용근 | 지운 `LobbyDepartureText.cs`의 2줄 | 삭제 | [파일 삭제 메모](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md) |
| 용근 | `ConsumableItemVisualDataSO` | Data + 입력 SO로(TP-R12, 에셋 값 그대로) | [소모 아이템 외형 P2](../handoffs/2026-10-29-consumable-item-visual-data-share-yonggeun-Hojin.md) |
| 용근 | `JobType.ProcessCommand`·`ProcessPreview`(`d1dcb92f`) | 지움(R-f, 메모에는 적지 않음 — R-d2 결정과 같게) | — |
| 용근 | Stage 덱 편집 사실 줄(`38869ec1`) | Flow에서 `StageProgress`로 옮김(계약·동작 같음) | 알리지 않음(10-29 사용자 "안 알려도 됨") |
| 용근 | `SceneNodeOrder`(제안만), `DeckEditAvailability` 키 | 고치지 않음 | [씬 노드 순서 제안](../handoffs/2026-10-29-scene-node-order-lobby-stage-proposal-yonggeun-Hojin.md) |
| 민형·용근 | `BufferSystems/Stage/StageProgressData.cs` | `IsStart` 추가 | [IsStart 메모](../handoffs/2026-10-27-stage-progress-is-start-share-Hojin.md) |
| 민형 | `InGameViewSystemFactory` 로비·맵 줄, `JobRegisterSystem` Plan 줄, `JobRootType` 값 넷·`JobType.NewRun`(번호는 주석), `LobbyJob.cs`·`ArtifactJob.cs` 삭제, `StageSelectionInputKey`·`LobbyInputKey` 주석 | 연결 줄·삭제(R-a·R-b·R-d2·R-f), 열거 값은 메모에 적지 않음(10-29 사용자) | [씬 Factory 공유](../handoffs/2026-10-28-stage-map-scene-factory-share-minhyeong-Hojin.md), 옛 [10-23 로비 Job·Plan 공유](../handoffs/2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md)에 "바뀜" 표시 |
| 천명 | 시험 `DevelopmentNavigationIntegrationTests`(N-1·2번 R-b) | 호출만(시험 묶음은 10-27 지워짐) | 기록만(사용자 지시) |
| 천명 | `ShopUI.cs`의 방문 문맥·나가기 문구·명령 줄(`a5133b32`) | 기반·`ShopUI`·컨트롤러 수신자로 옮김(나가기 문구 빈 값만 동작 차이). `ShopPopupController`의 천명님 47줄은 그대로 | [Core 요청 문서](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md) 5부 |
| 천명 | 팝업 Factory의 `itemVisuals` 세 줄 | 필수로(묶음 R-a) | Core 요청 문서 5부 덧붙임 |
| 천명 | `IRecoveryResultReadSink`·`IRecoveryStateReadSink` 주석, `ArtifactBuffer` 읽기 둘, seed, DB 참조 검사 | 주석만 고침(R-f), 나머지는 요청 | Core 요청 문서 6부 |
| 현강 | `ShopUI.cs`의 카드 `BindOwner`·`Open`·`Hide`·null 검사 줄 | 기반으로 옮김 | [상점 UI 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) 3부 |
| 현강 | `LobbyViewScope`의 Clicker 숨김·복귀 줄(`b691d8ae`) | 걷음(§12.7) | [로비 Clicker 걷음](../handoffs/2026-10-28-lobby-clicker-removed-share-hyeongang-Hojin.md) |
| 전투 작업 영역 | `GameBootstrap.cs`의 `_ui.OpenUI(UIId.Clicker)` | 걷음(§12.7) | 같은 Clicker 메모 |
| 민형 | `ShopUI.cs:2`(using 한 줄) | 줄이 없어짐 | 알릴 것 없음 |
| 예전 `Lobby.unity` 원격 브랜치 여섯 | — | `LobbyDepartureStatusUI` Missing Script 위험 | 파일 삭제 메모 |

모두 미전달이다. 다른 작업자 문서 `viewDocs/stage-input-guide.md:247`(용근님 문서)은 그 줄이 호진님 줄이라 C-2에서 직접 고쳤다.

## 12 묶음 2~8 구조

<a id="12-100-설계-2판--묶음-28-목표-구조-10-28-초안-코드-변경-없음"></a>

10-28 사용자: "참고도 참고지만 더 좋아질 수 있다는 거니까 더 좋게 만들어야겠네요. 지금 설계 부분을 더 좋게 만들어 봅시다." §1.3이 무엇을 고칠지의 목록이고, 이 절은 어떻게 고쳤는지다. 조사 근거는 10-28 읽기 전용 하위 에이전트 넷(기준 `bb29f806`)이다. 결정은 §3.2, 결과는 계획 §4.

<a id="121-전투-해법에서-배운-것"></a>

### 12.1 전투 해법에서 가져온 것

전투 작업이 같은 문제를 먼저 풀었고 남은 약점까지 측정해 두었다. 수명마다 객체 하나, 생성 계약 주입, 운영 입구 필수, 단일 입력 문, 이름 있는 질문, `SceneNodeOrder` 표를 가져오되, 전투에 남은 약점 — 조정자에 남은 정리 순서·사실 조립, 생성 입구 둘(정적 + 어댑터)과 구체 형식 반환, 시험 입구 때문에 남은 null 허용, 진행 클래스와 조정자의 상호 참조, 빈 virtual이 많은 두꺼운 기반 — 는 처음부터 피했다. 전투 측정은 라운드마다 예상보다 0.2~0.54 낮았다.

<a id="122-더-좋게-만드는-원칙-b1b7"></a>

### 12.2 원칙 B1~B7

- **B1 운영 경로 하나.** 시험 입구(`Bind*`, `Attach(root, picker)`, `output == null` 생성 갈래, null 기본값, 공개 `Root`·`Scope`)를 남기지 않는다.
- **B2 조립은 Factory 한 번, System은 Framework 연결만.** System은 서비스 받기·씬 노드 등록·Tick 전달·배경음만 한다. 생성 입구는 생성 계약 하나이고 우리 형식(Scope)을 돌려준다. 람다 지연 생성은 없다.
- **B3 Scope = 수명 부품의 묶음.** Scope가 부품을 차례로 만들고 `Dispose`는 만든 순서의 역순으로 부품마다 `CleanupErrors.Try`를 한 줄씩 부른다. 바뀐 것: 정리 목록 부품 `ViewCleanupList` → 역순 호출(10-28 "권장대로 단순화", 생성자에서 손으로 등록해 순서를 두 곳에 적는 셈이었음).
- **B4 화면마다 입력 문 하나, 사본 없이.** 우리가 바꾸는 사실(팝업 열림)은 알림으로 한 곳에 저장하고, 알림이 없는 사실(Buffer 요청 상태·Layer·정지)은 쓰는 순간 읽고, 판정은 입력이 들어오는 순간 Scope 안 private 판정 함수가 한 번 한다. 부품에는 판정 함수만 넘긴다(부품은 Scope를 모름). 매 Tick 검사하지 않는다. 예외: 알림이 없는 Buffer 결과를 보여야 하는 것(출발 거절)은 Tick에서 그 값 하나만 본다. 바뀐 것: 사실 값 struct + Policy 정적 클래스 → Scope 판정 함수(10-28 "권장대로 단순화", 판정 셋에 비해 무겁고 단락 평가가 사라졌음).
- **B5 상속 대신 작은 부품 조합.** 팝업 컨트롤러 셋의 같은 뼈대(Start·Dispose·Close·생존 확인·보고)는 작은 수명 부품 `StagePopupViewLifetime` 하나를 셋이 갖는다. 인터페이스는 멤버 두셋으로 좁게.
- **B6 비교·판정은 우리 값 형식 하나.** 다른 작업자 클래스(`CombatEntryRequest`)에 멤버를 더하지 않아 기능 변경이 서로 묶이지 않는다.
- **B7 생존 확인은 재진입이 가능한 호출 뒤에만.** UI 열고 닫기·`Instantiate`·외부 콜백 뒤에만 `CanWork`를 다시 보고, 순수 Buffer 읽기 뒤에는 보지 않는다. 규칙은 Scope 머리 주석 한 곳.

<a id="123-묶음별-목표-구조"></a>

### 12.3 묶음별 지금 구조

**묶음 2 — 스테이지 맵 조립**

- 생성: `InGameViewSystemFactory`의 우리 줄이 `new StageMapViewSystem(faults, StageMapSceneFactory.Instance)`. System은 `PostBindService`에서 `StageMapViewServices`(27칸, 모두 필수, `Audio`·`Loading`은 System 필드)를 만들고, 씬 연결 때 생성 계약에 루트와 함께 넘긴다. 팝업 Factory 셋(상점·회복·덱 편집)은 `StageMapSceneFactory`가 만든다.
- Scope 부품 넷: `StageMapViewScene`(노드·배경 표시·카메라 끌기·카메라 핸들) / `StageMapViewEntry`(표시 소유권·노드 이동·진입 관찰자) / `StageMapViewInput`(집기·선택 입력·슬롯·Layer) / `StageMapViewPopups`(팝업 컨트롤러와 입력 세션). 생성자 인자는 (루트, 서비스 묶음, 오류 경계) 셋.
- 정리 순서: 표시 소유권·맵 요청 Buffer를 먼저, 나머지는 역순(팝업 → 선택 화면 → 맵 입력 → 씬 표시). 상점은 입력 Buffer 비우기가 화면 끊기보다 먼저, 덱 편집은 초안 → 끊기 → Scope.
- 판정 함수 셋: `CanShowSelection`(선택 화면 다시 보이기)·`CanOpenDeckEdit`·`CanUseDevelopmentTools`, 공통 조각 `IsReady`·`HasPendingRequest`·`IsAnyPopupOpen`. 덱 편집 열기 조건 내용은 옛 것 그대로(Q-R3).
- 공용(`ViewSystem/Common/`): `ViewCameraHandleLease`(AfterLoad 노드 100에서 빌리고 연결 해제·정지 때 돌려줌, 실패 정리 포함), `ViewAttachmentInputLayers`(교체된 연결의 Layer 변경 막기).
- 배경음은 System에 둔다. 오류 정지로 Scope가 없어져도 정상 이탈 때 AMB를 멈춰야 한다.
- 출력 카메라는 `Camera.main` 그대로(Q-R4 B), 씬 루트를 한 번 찾는 것은 Framework 제약이라 남긴다.

**묶음 3 — 로비 입력·출발**

- 생성: `LobbySceneFactory`가 `ILobbySceneFactory`를 구현하고 아티팩트 팝업 Factory·팝업 부품을 조립한다. `LobbyViewServices`(20칸)는 DB 값·방 프리팹·아티팩트 표시 설정 검사를 만들 때 한 번 한다(그래서 로비 데이터가 잘못되면 Combat 직접 시작도 부트에서 멈춘다).
- Scope 부품: `LobbyViewScene`(시설 방·배경 출력 맞춤·판·캐릭터·카메라 핸들) / `LobbyViewInput`(시설 고르기·말 이동·슬롯·Layer) / `LobbyViewPopups`(덱·아티팩트 팝업·출력 마스크) + `LobbyDepartureFlow`(옛 `LobbyDeparturePresenter`, 그리지 않아 이름을 바꿈).
- 판정 함수 둘: `IsInteractionBlocked`(끝난 연결·시설 팝업·출발이 붙잡음)·`CanDepart`(막히지 않음 + 로비 Layer). 시설 팝업 열림은 `SetPopupBlocked` 알림 한 곳(`_popupBlocked`). "출발이 붙잡음"(`LobbyDepartureFlow.IsHolding`) = 처리 중인 요청이 있거나 이 연결의 요청이 거절·취소로 끝나지 않음.
- 출발 요청 질문 셋 `IsQueued`·`IsPrepared`·`IsTransitionSucceeded`(+ 이미 있던 `CanResubmit`). `LobbyPlan`·`LobbyTransitionJob`은 질문만 읽는다. 거절은 Tick에서 처음 본 순간 `ShowNotice(문구, 3초)` 한 번. 오류로 끝난 출발은 런타임 정지(F1)가 맡는다.
- 정리 순서: 공유 출력(배경 카메라 맞춤·캐릭터 숨김 → 아티팩트 출력) → 출발 요청 취소 → 역순(팝업 → 입력 → 씬 표시).
- 덱 버튼 닫기는 Boot `GlobalUIPresentation.ShowLobby` 하나(Q-R5 ①). 로비 글자는 §12.7.

**묶음 4 — DB SO**

- DM1: SO 여섯(회복 외형·소모 아이템 외형·아티팩트 외형·아티팩트 보드 설정·노드 외형·맵 표시 설정)은 읽기 전용 Data 파일 + 입력 SO다. SO는 안쪽 입력 `Input`(`[SerializeField] private` 칸 + `ToData()`, `EnemyLoadoutDataSO` 꼴)을 갖고 `GetData()`가 부를 때마다 새 Data를 만든다(P2). 칸 이름이 옛 것과 같아 에셋 값은 그대로다.
- DM2: `DataBaseSystem/Data/View/ViewPrefabRules.cs`가 같은 뜻의 검사 원본이다 — 프리팹 에셋인가, 활성 루트, identity 루트, 정적 메시(정점·Material), 보이는가, 표시 보정. 로비 규칙·로비 모델·캐릭터 보정·배경·노드 아이콘·맵 표시 설정·아티팩트 외형이 부른다. 표마다 다른 요구(로비 Layer Default·허용 컴포넌트·LOD, 아이콘 허용 컴포넌트, 맵 LineRenderer·노드 부품)는 그 표에 둔다(10-29 "같은 뜻만 합침").
- DM3: `LobbyFacilityView`·`LobbyFacilityKind`는 `DataBaseSystem/Data/View/Table/LobbyVisual/Item/`(GUID 그대로, `MovedFrom`). 시설 표지는 종류 칸만 갖고 클릭 영역은 같은 객체의 `BoxCollider`를 읽는다. 방 시설 검사(정확히 셋·종류 겹침 없음·활성 BoxCollider 하나·유한한 양수 크기·방 안에서 활성·중첩 없음)는 `LobbyVisualPrefabRules.ValidateRoom`이 DB 생성 때 원본 프리팹에 한 번 한다. View는 복제본에서 모으기만 한다(`LobbyRoomBinding.Create`).
- DM4: `Event_Recovery.asset`은 등록된 표(`InGameGroup/Recovery/Item/`)를 가리킨다. 미등록 사본 표는 지우지 않았다. 참조 무결성 검사를 DB 생성 때 넣는 것은 `InGameDataBaseFactory`(용근·민형) 줄이라 요청(Core 요청 문서 6부).
- DM5: 회복 이름은 숫자 없는 임시 문구(TP-R11).
- DB가 검사한 값을 View가 다시 검사하던 넷(마커·맵 표시·배경·아티팩트 팝업)은 R-f에서 걷었다.

**묶음 5 — 진입 비교·Core 규칙 단정**

- SE: `ViewSystem/StageMap/StageEntryIdentity`(맵 명령 번호·챕터·노드, `Of(command)`·`Of(request)`·`Of(receipt)`, `==`). 전투 결과(`request.Result`)의 챕터·노드 대조는 번호가 없는 Core 결과 비교라 그대로다.
- SF: 비전투 팝업 고르기는 `StageMapViewScope.RestoreNoncombat` 한 곳이다. 상점 상태가 있으면 상점, 끝나지 않은 회복 상태가 있으면 회복, 둘 다면 계약 위반 보고, 둘 다 없으면 아무것도 열지 않는다. "회복 문맥이 선택과 같은가"는 회복 컨트롤러 `MatchesSelection`이 맡는다. 바뀐 것: 계획의 `StageMapNodeContext` 형식 → Scope 판정(두 줄뿐). `CurrentShopData.VisitKind`는 나눠 그리는 화면(메인 강화 상점)이 생기면 그 화면이 읽는다.

**묶음 6 — Stage 실행 계층**

- `StagePlan`: `Start`와 `Next`. `StageLifeCycle`은 진입 때 `Register(Stage, Start)` 하나, Runner가 비면 `Register(Stage, Next)`(로비 꼴). 로스터가 비었으면 `StagePlan`이 새 Run Job을 앞에 붙인다.
- `Decide` 순서: 로딩이면 쉼 → 전투 진입 시도가 끝날 때까지 다른 일을 걸지 않음(끝나면 닫고, 확인 전에 끝나면 진입 도메인 멈춤) → 증강(처리가 준비보다 먼저) → 노드 진입(전투 노드는 전투 진입 요청, 그 밖은 Core 노드 처리) → 상점 → 회복. 멈춘 도메인은 건너뛴다.
- 전투 진입 요청(`ICombatEntryService.RequestEntry`)은 Job이 아니라 `StagePlan.Next`가 판단 직후 부른다. 진입 서비스가 Runner가 빈 순간에만 받기 때문이다. 시도 닫기·도메인 멈춤도 같은 자리다.
- 처리 확인은 처리 Job 바로 뒤 확인 Job(`StageCheckJob`)이 같은 Runner Tick에 본다(맵·상점은 처리 번호와 입력, 회복·증강은 예약이 남았는지, 증강 준비는 원본 열쇠가 바뀌었는지). 그래서 다음 Tick까지 기다리는 상태는 전투 진입 시도(`StageEntryAttempts`) 하나뿐이다. 회복·증강 예약은 처리 Job `Execute` 안에서 한다.
- 파일: `StageProgressFacts`(사실 값, 열거 `StageDomain`·`StageCombatEntryState`)·`IStageProgress`·`StageProgress`(이번 방문의 상태와 사실 읽기, 로비 `LobbyArtifactDomain` 꼴)·`StageEntryAttempts`·`StageTraitGrowthProgress`·`Plans/StageStep`·`Jobs/StageCheckJob`. 서비스 키·그룹 속성 `StageProgress`.
- 등록 자리: Stage `Next` 하나(Q-Rd3 A). `JobRootType`의 열쇠는 기능마다가 아니라 판단하는 주인(수명)마다 하나다. 같은 씬의 새 기능은 `Decide`에 한 줄만 더하고, 도메인마다 Job 만들기·확인·정지는 각자 파일에 둔다.
- 새 Run: `LifeCycles/RunStart/RunStartJob`이 남은 전투 막기·맵 입력 비우기·새 Run 호출·로스터 설치 확인을 한 곳에서 한다(기본 로드아웃 키도 여기). 결과는 `IRunStartOutcome`이 받는다 — 로비 `LobbyRunStartOutcome`(출발 요청에 게시), Stage `StageRunStartOutcome`(거절이면 예외).
- 증강(TraitGrowth) 계약과 조건(10-29 증강 세션이 사용자 지시로 보냄, Q-Rd2 B): 바꾸지 않는 계약은 `IStageTraitGrowthFacts.IsPending`(Stage 활성일 때만, 선택 필요·결과 확인 대기면 참, 확인과 다음 준비 사이에도 참)과 `LifeCycleSystemGroup.StageTraitGrowthFacts` 노출, View 요청 수명(`ITraitGrowthRequestSubmitter`·`ITraitGrowthRequestExecution`·`TraitGrowthRequestLifetime`)이다. 지킬 조건 일곱:
  1. 증강 판단이 맵 진입·상점·회복보다 먼저이고, 상점·회복 명령은 계속 받는다.
  2. 증강 입력은 정지·씬 이탈 때 취소하지 않는다.
  3. 덱 편집 "미등록 요청"에는 제출된 증강 선택(`CanReserveExecution`)만 넣는다.
  4. 처리(예약 번호 → 처리 Job, 예외 때도 종료 통지)가 준비보다 먼저다.
  5. 준비 뒤에도 원본이 그대로면 증강 도메인을 멈추고 오류 로그를 남긴다.
  6. Core 진행 읽기는 원본 열쇠(옛 `TraitGrowthFlow.ProgressKey` 여덟 값)가 바뀔 때만 한다.
  7. 결과 확인은 화면만 한다.
- `faults`·덱 편집 창은 필수다. 덱 편집 사실 셋·`IsPending`은 `StageProgress`에서 읽는다.
- 전투 `CombatNextJob`처럼 판단과 실행을 함께 하지 않는 까닭: Stage에서는 등록 Tick과 실행 Tick 사이에 사실이 바뀔 길이 없다(한 Runner, 한 Tick 하나 등록). 바뀔 길이 생기면 전투 꼴로 옮긴다.
- 바뀐 것: Flow 다섯·Plan 셋(상점·회복·증강)을 지웠다. 계획의 `StageProgressReader`는 따로 두지 않고 `StageProgress`에 합쳤다.

**묶음 7·8**

- 묶음 7(합의 필요): F2 seed는 Core 요청 문서 6부, F3은 용근님 제안, F8·F10은 민형님 제안. 모두 미전달·답 대기.
- 묶음 8: J6·J7 아티팩트 죽은 경로와 `LobbyJob` 표본을 걷어 로비는 `Next`만 건다(로비 설계 P7·Q-LJ4 바뀜). J8 `ArtifactBoardPreparation`은 부트 준비라 Plan 밖에서 Runner에 직접 등록하고(첫 구간 전 부트가 한 번 요청해 세 시작 길을 덮음), 보드 설정이 없으면 `InitData`에서 바로 예외. UL9 `StageLegendUI` 재정의 걷음.

### 12.4 순서와 예상

순서는 4번 S-c → 묶음 2 → 3 → 5 → 6 → 4 → 8·7이었다(B1~B3 꼴을 먼저 세워 3·5가 그 위에 얹히고, 다른 영역 영향이 가장 큰 6은 View 정리 뒤). 끝에 블라인드 A/B 한 번: `bb29f806` + 4번 대 끝난 판(계획 R-g).

예상(알려진 문제 기준, 전투 경험상 실제는 0.2~0.5 낮을 수 있음): FW 67.5 → 95, 내부 구현 54.5 → 95, DB SO 52.5 → 95, UI 52.5 → 95(Q-S6이 남으면 −7.5), Job·Plan 52.5 → 100(J5 예외 확정 시). 100%에 못 미치는 것은 다른 작업자 합의(묶음 7)와 Framework 제약(씬 루트 찾기·카메라 이름 조회)이다. 다만 §1.2에 "남음·확인 안 함" 낮음이 여럿 있어 낮음 상한(−10)까지 깎이는 기준이 있을 수 있다.

### 12.5 결정

묶음 2~8의 결정 표(옛 "정할 것 Q-R1~Q-R12")는 §3.2로 옮겼다.

<a id="126-노드-종류--셋-유지-구분은-상세-종류-q-r9-10-28-사용자-결정"></a>

### 12.6 노드 종류 — 셋 유지, 구분은 상세 종류 (Q-R9)

- 결정(10-28 사용자 "3개 유지"): `StageType` 셋(전투·상점·이벤트)을 그대로 둔다. 바뀐 것: 처음 사용자 방향 "상점을 없애고 전투·이벤트만으로"와 Core 요청 → 취소. 처음 적은 "천명님 요청"은 담당자도 틀렸다(열거형은 DB, 처리 표는 민형님, 갈래는 천명님).
- 까닭: `StageType`은 Root에 따로 적는 값이 아니라 상세 SO가 정한다 — `CombatStageDetail` → Combat, `ShopStageDetail`(아이템 상점)·`EnhancementStageDetail`(강화) → Shop, `EventStageDetail`(회복) → Event. 큰 분류이고 그 안의 종류는 상세 종류가 원본이다. Core도 상세 종류로 가른다(맵 생성 `ChapterStageKind`, `ShopService`는 지금 강화 방문을 `EnhancementShopUnavailable`로 거절). 방문 종류는 Buffer `CurrentShopData.VisitKind`(`Ordinary`·`Enhancement`)로 View에 온다. 둘로 합쳐도 View 작업은 같고 바꿀 곳만 여럿이다.
- View는 종류 이름을 읽지 않고 Buffer 상태로 팝업을 고른다(묶음 5). 메인 강화 상점도 같은 길이고 새 `StageType` 값은 만들지 않는다.
- 보고만(지우지 않음): 같은 이름·값의 사본 `DataBaseSystem/Data/InGame/Table/Stage/Enum/StageType.cs`(민형님 `3a79c81e`)는 쓰는 곳이 없다.

### 12.7 로비 시설 안내 글자 — `LobbyTexts`

<a id="127-로비-시설-안내-글자--clicker에서-lobbypanel-글자로-10-28-2판-코드-변경-없음"></a>

10-28 사용자: "Clicker먼저 합시다", "Lobby에 있는 글씨들을 Deck·Artifact·GameStart 하나씩 묶어서", "LobbyPanel에 있는 Clicker로 나누지 말고 Lobby UI처럼", "UIId.Clicker를 만든 게 제가 아니라면 남겨 주세요". 이 절은 Q-R5 ②(Clicker 주인)를 대신한다. 결과는 계획 §3.13.

지금 구조:

- `Lobby.unity` `LobbyCanvas` → `LobbyPanel` 아래 묶음 `LobbyTexts`(화면 전체로 늘림)에 "로비" 제목(`Lobby`)과 안내 글자 셋(`StartGame` "여정 시작하기", `DeckSetting` "덱 편집하기", `Artifact` "아티팩트 관리하기")이 있다. 아티팩트 패널(`ArtifactPopupPanel/Panel`) 끝에 그 화면의 제목 `ScreenTitle` "아티팩트"가 있다. 제목 문구는 코드에 없고 화면마다 한 곳에 있다.
- `LobbyCanvasView` 칸 `lobbyTexts`(묶음 `GameObject`) 하나. `LobbyViewScope`가 받아 시설 팝업 경계 `SetPopupBlocked`에서 팝업이 열리면 끄고 닫히면 켠다(Q-G5 B). 연결 시작 `Start`에서 한 번 켠다(앞 연결이 팝업 중에 끝나면 꺼진 채 남기 때문). 검사는 `LobbyCanvasView.ValidateComposition`에 있다(R-b 검토 L1). 매 Tick 계산은 없다.
- 결과: 아티팩트 화면에서는 로비 묶음이 꺼지고 아티팩트 제목이 보여 제목이 바뀐 것처럼 보인다. 덱 화면 동안에도 묶음이 숨는다(덱 팝업은 자기 제목이 있고 원래 어두운 막이 덮던 자리).
- Clicker: 코드에서 `UIId.Clicker`를 쓰는 곳은 `Clicker.cs` 자신뿐이다. Boot `OpenUI(UIId.Clicker)`와 Scope의 숨김·복귀 줄은 걷었다. `Clicker.cs`·`Clicker.md`·`UIId.Clicker`·자동 복귀 기능(`UIBase.RestoreWhenHistoryEmpty`)은 현강님 것이라 남기고, 지울지는 현강님이 정한다(메모). 다른 씬에 Clicker에 대응하는 것은 없다(자동 복귀를 켠 UI도 Clicker 하나뿐이었다).

바뀐 것:

- 1판의 새 화면 부품·`LobbyCanvasView` 참조·검사·매 Tick 보임 계산 → 뺌(10-28 사용자). 1판의 Q-G1~Q-G3은 없어졌다.
- "Boot 한 줄은 남김"(옛 Q-G4 A) → 걷음. 사용자가 씬에서 `Clicker` 오브젝트를 빼자 Boot가 "`UIId.Clicker` 미등록" 예외를 냈다(10-28).
- Q-G5: 권장 A(아티팩트 화면에서도 보임) → B 숨김(10-28 사용자 "아티팩트에선 숨김").
- 묶음 이름 `FacilityLabels` → `LobbyTexts`, "로비" 제목을 묶음 안으로, 아티팩트 제목 추가(10-28 사용자 "깔끔하고 유연한 방법이면 A". 다른 안 B는 코드가 제목 문구를 바꿈).
