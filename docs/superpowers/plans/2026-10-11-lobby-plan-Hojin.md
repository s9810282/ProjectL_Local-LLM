# 로비 계획

- 작성자: 호진(이호진)
- 상태: 기본 로비 완료(시설 셋 이동·클릭, 덱 빈 팝업, 아티팩트 편집, 기본 새 Run 출발, 역할별 SO·`Lobby.prefab` 시설 연결). 마지막 사용자 Unity 확인은 2026-10-27(검사 후속 정리 R-a 위). 남은 기능은 LV6·LV7·LV8B·LV9와 LV10의 로비 쪽 결정이다(§2).
- 설계: [로비 설계](../specs/2026-10-11-lobby-design-Hojin.md). 결정 본문·계약은 설계에 있고 이 계획은 진행·결과·확인 상태만 담는다.
- 안내: [로비 안내](../viewDocs/lobby-guide-Hojin.md)(출발·설정 연결 이력·모델/SO 편집 방법).
- 정리: 2026-10-27 로비 계획(1~6부)과 `2026-10-20-lobby-run-summary-plan-Hojin.md`·`2026-10-23-lobby-job-plan-restructure-plan-Hojin.md`·`2026-10-24-lobby-output-camera-migration-plan-Hojin.md`·`2026-10-25-lobby-layout-db-plan-Hojin.md`·`2026-10-25-lobby-role-so-plan-Hojin.md`·`2026-10-26-lobby-prefab-facility-binding-plan-Hojin.md`, 옛 인계 `handoffs/2026-10-20-lobby-run-summary-internal-handoff.md`를 이 문서로 합쳤다. 끝난 Task 단계·시험 수치·로그 경로는 git `10914570`에 있다.

## 1 진행 요약

작업 규칙(사용자 지시): 커밋·push는 사용자가 한다. 10-20부터 새 시험·fixture를 만들지 않고, 우리 변경으로 깨지는 기존 시험만 최소로 맞춘다(RS·LJ Q-LJ6 B·10-24 이후 모든 Phase). 10-24~10-26 작업 일부는 Codex가 구현했다([Claude–Codex 연결 계획](2026-10-24-claude-codex-unity-workflow-plan-Hojin.md)).

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| LV1 | 로비 씬·고정 카메라·Boot 기본 시작 Lobby | 완료 10-12 | 자동 검증. 로비 진입은 이후 사용자 재생에서 반복 확인 | `2d5393b0` |
| LV2 | 같은 시설 누름·놓음 → 들기·이동·내려놓기 → 기능 1회 | 완료 10-12 | 자동 검증. 시설 클릭·이동은 10-24·10-25 사용자 확인 | `2d5393b0` |
| LVM1·LVM1G·LVM2·LVM3 | 바닥·시설 모델 View DB, 한 칸 배치, 모델 생성·정리, 교체 인수 | 완료 10-12. 구조는 R1·A·L·R·LB-1로 바뀜 | 자동 검증 | `2d5393b0`·`c772d9b8` |
| LV3 | 공통 팝업 입력 수명 유지, 설정 UI·연결 철회 | 설정 철회 10-12(사용자 요청). 설정 연결은 UI 작업자 인수 대기 | 자동 검증 | `c772d9b8` |
| 격자 | 8×6 → 7×5 → 11×9 → 4×4 → 격자 규칙 종료(A) | 이력 | — | §3.5 |
| LV4 | 덱 시설 → 빈 덱 팝업 → 닫기 | 완료 10-12 | 자동·Game View 캡처. 덱 팝업 열기·닫기 사용자 확인 10-24 | `3205df40` |
| 정면 도착 | 아티팩트·덱 도착 칸 변경, 시설을 바라봄 | 완료 10-13 | 자동·Game View 캡처 | `3205df40` |
| LC4 | 기존 Core 아티팩트 계약 재사용 | 사용자 승인 10-13 | — | 문서 |
| LV5 | 아티팩트 편집을 StageSelect에서 로비 4번 시설로 | 완료 10-13 | 자동·Game View 캡처. 팝업 동작 사용자 확인 10-24 | `3205df40` |
| LV8A | 기본 로드아웃 1로 새 Run → StageSelect | 완료 10-13 | 자동. 출발은 LJ Phase 4 사용자 확인 10-23 | `1a7267aa` |
| LV6·LV7·LV8B·LV9 | 선택 캐릭터 표시, 덱 선택·확정, 선택 덱 출발, 통합 인수 | 미착수(LC0~LC3 대기) | — | — |
| LV10 | Run 종료 뒤 로비 복귀 | 전투 쪽 연결 완료(10-16~10-22). 로비 쪽 결정 둘 남음 | 포기 복귀 사용자 확인 10-23·10-24 | 전투 쪽 |
| 아킴보 | 캡슐 대신 아킴보 고정 표시 | 완료 10-16 → 10-25 §11 방식으로 바뀜 | 자동 | `24f4f3aa` |
| RS-L1·RS-C1 | Run 결과(엔딩) 표시: 로비 자동 표시 → 전투 씬 | RS-C1 완료 10-20, 로비 잔여 인스턴스 정리 10-26 | UI 개폐만 확인. 전체 경로 미확인 | `83e53dac`·`cb76bd2c` |
| LJ Phase 0~5 | 로비 실행 계층을 트리거 → Plan → Job으로 | 완료 10-23 | Phase 0~4 사용자 확인 | `e46398d4`·`1a386b64` |
| 카메라 P1~P4 | Boot Main Camera 재사용, 고정 `LobbyCamera` 하나, 팝업 출력 마스크 | 완료 10-24 | 사용자 재생·기존 EditMode 전부 통과 10-24 | `1fcd474f` |
| 정리 P1~P3 | 덱 UI 버튼 한 번 연결, 표시 데이터 검사 캐시, 옛 기본값 | 완료 10-24(P3 기본값은 A에서 없어짐) | P1 사용자 확인. P2·P3 단독 확인 없음 | `83fb2767` |
| C | 로비 Canvas 5 → 3 | 완료 10-24 | 사용자 기능 확인 | `83fb2767` |
| R1 | 방 모델을 View DB `RoomPrefab`으로 | 완료 10-24 | 사용자 확인·기존 EditMode 통과 | `83fb2767` |
| A·B | 직접 Anchor·3D 클릭 Collider, 전투 메인 외형 공유 | 완료 10-24(B는 10-25 §11로 바뀜) | 사용자 확인 | `83fb2767` |
| L-0~L-3 | 시작·도착 칸·이동·표시 보정을 DB로, 클릭 영역을 방 프리팹으로 | 완료 10-25 → R-1에서 역할별로 나뉨 | 로비 진입·시설 클릭 사용자 확인 | `6c56a50d` |
| R-1~R-3·후속 | 역할별 SO 넷, 테이블 Inspector, 방·바닥 직속 생성, 캐릭터 런타임 생성·Akimbo 직접 연결 | 완료 10-25 | 사용자 확인(§3.21) | `6c56a50d` |
| LB-1 | 모델 SO가 `Lobby.prefab` 선택, 시설 kind·같은 객체 BoxCollider | 코드 완료 10-26 | 단독 확인 없음. 뒤 R-a 사용자 확인(10-27)에 포함 | `8ee9984e` |
| HM | 칸마다 뛰는 이동(설계 §15) | 완료 10-29(HM-a 경로 모양 분리, HM-b 로비 뛰기, HM-c 문서·검토) | HM-a Ua·HM-b Ub 문제 없음(사용자) | 커밋 전 |
| 호버 빛남 | 시설 호버 동안 안내 글자 밝게 + 빛 번짐(번쩍임 없음), 값은 새 역할 SO(설계 §16) | 코드·에셋 완료 11-02 | 컴파일·재생 미확인(설계 §16.4) | 커밋 전 |

## 2 남은 일

### 2.1 기능

- [ ] **LV6 선택 메인 캐릭터 표시**: 옛 안은 얼굴(7번 `MainCharactorUI`)과 3D 모델을 같은 선택 키로 연결하는 것이었다. 지금 승인 계약은 모델 SO의 Akimbo 직접 연결이다(§3.21). 얼굴만 연동할지, 모델도 선택에 따라 바꿀지부터 다시 정한다(10-25 정리).
- [ ] **LV7 덱 선택·확정**: `LobbyDeckPopupController`는 빈 목록과 "덱 정보를 준비 중입니다."만 보인다. LC0 정책과 LC2 공개 계약을 먼저 확인한다. View DB 전체·현재 Run 보유를 후보로 쓰지 않는다.
- [ ] **LV8B 선택 덱 출발**: LV7 + LC0·LC1·LC3 뒤 LV8A 출발 수명을 재사용한다. 기본 키 1 성공을 선택 덱 성공으로 보지 않는다.
- [ ] **LV9 통합 인수·문서**: Boot 기본 Lobby·Stage/Combat 개발 직행·상점·회복·아티팩트·덱 회귀, 수동 인수(이동·취소·UI 위 시작 입력 차단·카메라 복구·전투 반영), 공유 문서. 옛 계획의 `viewDocs/lobby-usage-guide.md`는 [로비 안내](../viewDocs/lobby-guide-Hojin.md)가 대신했고, 예정했던 `handoffs/2026-10-11-lobby-framework-ui-camera-share.md`는 만들지 않았다.
- [ ] **LV10 로비 쪽 결정**: ① 로비 재진입 때 덱·아티팩트 상태를 어떻게 보일지(LV7과 함께) ② 맵 도메인이 멈췄을 때의 탈출 경로·문구([View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md) D12, [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)의 여정 포기 결정 대기와 함께 볼 것).
- [ ] **LV3 설정 UI 연결**: UI 작업자가 설정 화면·버튼·UIId 등록·닫기/Back·표시 상태 계약을 주면, 사용자 요청 뒤 Common 입력·이동 취소·모달 복귀를 연결한다. 인수 조건은 [로비 안내](../viewDocs/lobby-guide-Hojin.md) 2부.
- [x] **HM 칸마다 뛰는 이동**: 완료(§3.23). 커밋은 사용자가 한다.
- [ ] **출발 진행·거절 안내**: 10-20 `3bc33b20`부터 화면에 없다. 참조 0이던 `LobbyDepartureText`·`LobbyDepartureStatusUI`는 10-27 검사 후속 정리에서 지웠다([삭제 공유](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md), 커밋 전). 다시 만들 때는 그 메모의 방식(등록된 `LobbyCanvasView` 자식 부품, 요청 질문만 읽기)을 따른다.

LC0~LC3은 [Core 전달 문서 §13.12](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#1312-로비-core-계약-요청-2026-10-11-미전달)에 요청으로 적었고 미전달·회신 없음이다. 천명님 커밋에 들어온 [로비 메인 선택·Run 동일성 설계](../specs/2026-10-23-lobby-main-selection-run-lock-design.md)(제안 상태, Phase A~C 미착수)가 LC0~LC3과 겹칠 수 있다 — 확인 필요.

### 2.2 확인 대기

- [ ] **엔딩 전체 경로**: 실제 최종 보스 처치 → 전투 엔딩 → 확인 → 로비. RS-C1·10-26 정리 모두 UI 개폐만 확인했다.
- [ ] **R-2 Inspector**: 저장·재선택, 역할 타입 제한의 실제 조작, 중복·Missing·다른 타입 목록 복구.
- [ ] **최신 구조의 세부 동작**: 모든 이동 조합·중간 취소·오류 정리·팝업 숨김/복원·화면비별 가림. 사람의 물리 마우스·자연 속도 체감·Profiler 측정은 처음부터 하지 않았다.
- [ ] **아티팩트 팝업 4:3**: LV5 때 1280×960에서 설명 오른쪽·보드 왼쪽이 잘렸다(옛 고정 구도 한계). 카메라 이전(§3.15)으로 보드 배치가 바뀐 뒤 다시 보지 않았다.
- [ ] **정리 P1 별도 입력**: 키보드·게임패드 Submit 결과 미보고.
- [ ] **C**: Global UI와의 앞뒤 순서, 해상도별 배치 미보고(10-26 화면 UI 소유 정리로 로비 Canvas가 다시 바뀌었다 — [스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md)).

10-25 정리 원칙: 옛 체크박스(LV1-6·LV2-6·LVM3-5 등)와 옛 실패 수는 그대로 옮기지 않는다. 사용자가 확인한 진입·이동·시설 클릭·재진입은 다시 미확인으로 두지 않는다.

### 2.3 보류

- **공용 UI 이력 문제**: `SettingsUI.Hide` 관찰자 안에서 같은 설정을 다시 열면 `UISystem.CloseUI` 후처리가 새 Back 이력을 지운다(10-12 LV3에서 재현). 사용자 지시로 공용 `UISystem`은 고치지 않았다. [UI 작업자 인계](../handoffs/2026-10-12-lobby-settings-ui-history-handoff-Hojin.md) 미전달·미수정.

## 3 작업별 결과

### 3.1 LV1 공간·고정 카메라·Boot 진입

- 2026-10-12, 기준 `fb3f418a`. `Lobby.unity`, `LobbySceneRoot`·`LobbyFacilityView`·`LobbyFacilityKind`(Artifact·StageEntry·Deck)·`LobbyViewSystem`·`LobbyCameraController`를 만들었다. 시설·캐릭터는 공간 확인용 임시 형상이다.
- 조립: `LobbyViewSystem`을 AddModule 한 번. UI BindScene(-200) → CameraGroup(-190) → LobbyLifeCycle(0) → Lobby Attach(100), Exit(-100). 7번 `MainCharactorUI`·8번 Option은 Boot의 `GlobalUICanvas` 하나를 재사용한다. 로비 진입은 NewRun·EnterChapter를 하지 않는다.
- Boot 시작 enum에 `Lobby = 2`를 끝에 더하고(Combat 0·Stage 1 유지) 기본 시작을 Lobby로 했다.
- 바뀐 것: 씬 번호 옛 Lobby 3 → 지금 `SceneIndices` Boot 0·Lobby 1·Stage 2·Combat 3(10-13 사용자 결정). `Title.unity`는 사용자 결정으로 지웠고 Boot가 시작 화면을 맡는다(10-13).
- 바뀐 것: 출력 Camera·Brain을 로비 씬에 두던 구성 → 지금 Boot Main Camera 재사용(§3.15).
- 확인: 자동 검증(세 화면비 합성 렌더). 일반 Game View 육안 인수는 하지 않았다.

### 3.2 LV2 시설 입력·체스 말 이동

- 2026-10-12. 입력 경로 `UnityInputBridge → Submit → Lobby Controller → 누름 소유 슬롯 → LobbyInteraction`. 새 `LobbyInput/*`(`ILobbyInputReceiver`·`LobbyReceiverSlot`·`LobbyInputController`·`LobbyInputNames`·`LobbyInputLayers`)와 `LobbyInputServiceKey`, `PlayerInputActions`에 Lobby 맵만 추가(기존 GUID 유지).
- `LobbyFacilityPicker`(출력 카메라 Ray + 시설 Collider), `ScreenUIPointerBlocker`(GraphicRaycaster 판별을 공용으로 뽑고 `StageMapPointerBlocker`가 위임), `LobbyMotionSettings`·`LobbyCharacterMotion`(`IDelayService` 예약 하나로 들기·이동·내려놓기, 완료 1회 소비, 취소는 마지막 정상 위치 복원).
- 같은 시설에서 누르고 놓아야 이동한다. UI 시작·다른 시설 놓음·강제 Canceled는 실행하지 않는다. 이동 중 추가 목적지는 받지 않는다.
- 당시 운영 시설은 미연결(`CanExecute` false). 표시값 높이 0.35·들기 0.12초·이동 0.35초·내려놓기 0.12초는 기획값이 아니다.
- 바뀐 것: 이동 값은 씬 → L-1에서 DB → R-1에서 `LobbyMotion_Default`(§3.21).

### 3.3 LVM1~LVM3 바닥·시설 모델 View DB와 한 칸 배치

- 2026-10-12. LVM1: View DB 표 `LobbyVisual`(Data·SO·TableSO·`LobbyVisualPrefabRules`·`ILobbyVisualTableQuery`, `ViewTableKey.LobbyVisual`, `ViewGroup.asset` 등록)과 프리팹 넷(`LobbyFloor`·`LobbyArtifactFacility`·`LobbyStageEntryFacility`·`LobbyDeckFacility`). 사용자 요청으로 씬의 받침판 일곱(`Platform`·`FacilityTopFixture`·`ChessPieceBaseFixture`)을 빼고 몸체를 바닥에 붙였다. 추가 승인으로 가려진 아티팩트 안내 글자 높이만 올렸다.
- LVM1G: 사용자 확정 "모든 시설 한 칸, 플레이어는 칸 중앙, 시설 앞 별도 도착 칸". Core.Contracts `CellCoord` 재사용, `LobbyBoardMapper`·`LobbyGridPlacement`.
- LVM2: `LobbyViewSystem`이 InitData로 View DB를 받고 `LobbyVisualSet`이 바닥·시설 모델을 생성·소유·정리한다. 씬 표시 계층은 빈 표시 루트로 바꿨다.
- LVM3: 교체 인수 시험과 [로비 안내](../viewDocs/lobby-guide-Hojin.md) 3부(모델 교체 방법).
- 시험: `IViewQuery` 시험 가짜 셋(`ArtifactPopupConfigTests`·`LobbyInputTests`·`UnityStageInputTests`)에 새 속성만 더했다.
- 바뀐 것: 시설 모델 슬롯·표시 루트 → R1·L·R·LB-1을 거쳐 지금은 모델 SO가 방(`Lobby.prefab`)·바닥·캐릭터만 고른다(§3.22). `LobbyBoardMapper`는 A에서 지웠다.

### 3.4 LV3 공통 팝업 입력 수명과 설정 UI 철회

- 2026-10-12. 처음 LV3는 설정 UI(`GlobalSettingsButton`·`SettingsUI`·UIId 15·`SettingsInput`·`LobbyPopupCoordinator`)까지 만들었다.
- **되돌림(10-12 사용자 요청)**: "설정 UI와 관련 연결·테스트만 되돌리고 공통 팝업 입력 수명은 유지". 설정은 UI 작업자 담당이라 계약을 받은 뒤 연결하기로 했다. 설정 관련 새 파일 29개를 지우고 `GlobalUICanvas`·`UIIDs`·`RunTimeSystemFactory`·`GlobalUITests`를 원래대로 돌렸다.
- 남긴 것: `PopupInputSession`(토큰·입력 소유·복귀 조건, Stage 어댑터 `StagePopupSession`이 위임), 설정과 무관한 Deck 정책(로비 Attach에서 `CloseUI(Deck)`, Stage/Combat 정상 진입에서 `OpenUI(Deck)`). 순수 Lobby→Stage Deck 검사는 `LobbyDeckBootSmokeTests`로 옮겼다.
- 찾은 것: 공용 UI 이력 문제(§2.3).

### 3.5 격자 변경 이력

| 날짜 | 격자 | 근거 |
|---|---|---|
| 10-12 | 8×6, 간격 1.6, 시작 (4,1) | LVM1G |
| 10-12 | 7×5, 시작 (3,1) — 사용자가 홀수 격자 요청 뒤 선택 | `c772d9b8` |
| 10-13 | 11×9 — 사용자 변경. 구현·검증 기록 없음 | `a79cbfaa` |
| 10-20 | 4×4, X/Z 간격 4.25/3.85 — 씬에 들어감. 계획 기록 없음 | `89cbc44e` |
| 10-24 | 정리 P3가 코드 기본값을 4×4·시작 (1,2)에 맞춤 | §3.16 |
| 10-24 | A가 런타임 격자 규칙을 끝냄(4×4는 바닥 외형일 뿐) | §3.19 |
| 10-25 | L-1·R-1: 배치판 4×4·시작/도착 칸을 `LobbyBoard_Default`에 저장 | §3.20·§3.21 |

### 3.6 LV4 덱 표시와 독립 팝업

- 2026-10-12, 기준 `c772d9b8`. 덱 시설 정상 도착 → 빈 덱 화면 → 닫기 → 시설 입력 복귀. 운영 후보는 빈 목록이다.
- 새 `LobbyDeckPopupController`·`LobbyDeckPresenter`·`LobbyFacilityActions`(Deck만 실행), `Screens/Lobby/{LobbyDeckUI,LobbyDeckDisplayModel,LobbyDeckCardContent,LobbyDeckListItemView}`, `LobbyDeckPanel.prefab`, `UIId.LobbyDeck = 15`, Layer `LobbyDeckPopup`(Common만).
- 계획과 다른 점: 실제 InputSystem이 미등록 Layer를 거부해 사용자가 "Layer 한 줄 등록하고 계획 정정"을 승인했다(`RunTimeSystemFactory.inputLayers`에 한 줄). 맵 Disable 중 재진입으로 새 Lobby Layer가 지워지는 문제를 찾아 Attach 세대 확인을 더했다.
- 아티팩트는 열기 의도 시험과 이전 목록만 준비했다(실제 이전은 LV5).
- 바뀐 것: 팝업 수명 소유 → 10-15 비전투 View 구조 개선에서 `LobbyViewScope`·`PopupViewScope`·`LobbyDeckPopupViewScreen`이 맡는다([View 구조 계획](2026-10-13-view-structure-plan-Hojin.md)). 외부 Hide 뒤 다음 Tick 복귀 규칙은 설계 쪽에 있다.

### 3.7 시설 정면 도착과 플레이어 회전

- 2026-10-13 사용자 요청: 아티팩트·덱 도착 칸을 건물 사이 보라색 표시 위치로, 스테이지 도착 칸은 유지, 캐릭터가 고른 시설을 수평으로 바라봄. 취소는 위치·회전을 함께 복원한다.
- `LobbyCharacterMotion`에 시설 중심을 받는 Begin 오버로드, `LobbyInteraction`이 시설 중심을 넘긴다. 당시 7×5 기준 도착 칸 Artifact (2,2)·StageEntry (3,2)·Deck (4,2).
- 남긴 것: 덱 도착 칸에서 건물이 캡슐 일부를 가렸다(요청 칸을 그대로 두고 보정하지 않음).
- 확인: 자동·실제 Game View 캡처. 커밋 `3205df40`.

### 3.8 LC4 기존 Core 계약 재사용

- 2026-10-13 조사: 보드 초기화·번호 편집·새 Run 보존·전투 효과 API가 이미 있어 새 Core 개발이 필요 없다. 처음에는 외부 Core 회신을 선행 조건으로 뒀다.
- 같은 날 사용자 승인으로 정정: 기존 계약을 재사용해 LV5를 진행한다. 외부 회신은 선행 조건이 아니다. 근거와 판정은 설계의 LC4 절.

### 3.9 LV5 아티팩트 로비 이전

- 2026-10-13. 첫 로비에서 4번 시설 → 이동·회전 → 아티팩트 배치 화면. 닫으면 도착 칸에 남고 고정 카메라·시설 입력으로 돌아간다. StageSelect의 아티팩트 열기 버튼·팝업·배치판·편집 카메라를 지웠다.
- 보드 준비 한 곳: 새 `IArtifactBoardPreparation`·`ArtifactBoardPreparation`(서비스 + 모듈 한 번 등록). Boot는 Runtime Tick을 계속 돌리며 독립 준비 배치의 완료·보드 게시·Runner Idle 뒤 최초 구간을 한 번 요청한다. Stage/Combat 개발 직행도 같은 경계를 쓴다.
- 편집 한 곳: `ArtifactEditFlow`(GUID 유지)를 Lobby 폴더로 옮겼다. StageFlow는 맵·상점만 맡는다. 정상 로비 이탈(-200)은 미등록 명령 없음·마지막 처리 번호·Runner Idle을 확인한다.
- 씬: `LobbyWorld` 아래 바닥·시설·캐릭터·Anchor를 모으고 `ArtifactPopupCanvas`·`ArtifactWorldRoot`·`ArtifactWorldBackground`를 로비로 옮겼다. `LobbyArtifactCamera`(Priority 20)는 닫으면 `LobbyCamera`(10)로 돌아갔다.
- 바뀐 것: Flow 구조 → LJ(§3.14)가 Flow 셋을 지우고 Plan·Job으로. 별도 아티팩트 카메라 → 카메라 이전(§3.15)에서 지움.
- 남긴 것: 4:3 잘림(§2.2). 커밋 `3205df40`.

### 3.10 LV8 기본 출발 LV8A와 선택 덱 출발 LV8B

- 2026-10-13 사용자 승인: LV8을 LV8A(기본 출발)와 LV8B(선택 덱 출발)로 나누고 LV8A만 구현한다. 기본 로드아웃 1로 **항상 새 Run**을 시작해 StageSelect로 간다. 기존 Run 이어하기·LV6·LV7은 선행 조건이 아니다. 기본 키 원본은 `StagePlan.DefaultLoadoutKey = 1`이다.
- LV8A 결과: 스테이지 입장 시설 도착 → 번호 있는 출발 요청 → 남은 아티팩트 명령·프리뷰 정착과 Runner Idle → `StartNewRun(1)` → 로스터 확인 → `Change(Stage)`. 정상 거절은 `RosterAcquireResult.FailureReason`을 보관하고 다시 실행하지 않는다. 진행 중·보존 전투가 있으면 Core 호출 전에 막는다.
- 새 `BufferSystems/LobbyView/{LobbyDepartureRequest,LobbyViewBuffer}`, `LobbyRunStartJob`. Buffer 공개 이름은 같은 날 사용자 요청으로 `ILobbyViewBufferReadSink/WriteSink`·`LobbyViewBufferKey.Read/Write`·`IBufferQuery.LobbyViewReadSink/WriteSink`로 통일했다(구현 클래스 `LobbyViewBuffer`, GUID 유지).
- `BufferSystemGroup.cs`에는 새 접근자·조회만 더했다(Core 전달 문서 §5.12).
- 바뀐 것: `LobbyDepartureFlow`·`Queued` 단계·`MarkQueued`/`CompleteJob` → LJ에서 지움(§3.14).
- LV8B: 미착수(§2.1). 커밋 `1a7267aa`.

### 3.11 LV10 Run 종료 뒤 로비 복귀

- 2026-10-13 사용자 결정: Run이 끝나면 로비로 돌아온다. 전투 이탈 코드(`CombatPlan`·이탈 Job)는 용근님 영역이라 함께 정한다.
- 바뀐 것: 10-16 사용자 확정 "이번은 일반 패배만 로비" → 10-20 포기·최종 승리도 로비(Run이 끝나면 언제나 로비) → 10-22 용근님 전투 구조 개선 Phase 1이 전투 쪽 새 Run을 걷었다. 지금 새 Run은 로비 출발(LV8A)만 만들고, 목적지는 `CombatPlan.DestinationOf` 한 곳이다. 정책 원본은 [전투 Job·Plan 설계 J7](../specs/2026-10-10-combat-job-plan-restructure-design.md#j7-이탈-연쇄-k21-유지), 기록은 [10-16 후속](2026-10-10-combat-job-plan-restructure-plan.md#2026-10-16-일반-패배의-로비-복귀-후속)·[10-20 후속](2026-10-10-combat-job-plan-restructure-plan.md#2026-10-20-자발적-포기의-로비-복귀-후속)·[전투 View 구조 개선 설계](../specs/2026-10-22-combat-view-structure-improvement-design.md).
- 옛 "정할 것" 1(새 Run 생성 위치)·2(포기·최종 승리 범위)는 위로 풀렸다. 3(재진입 표시)·4(맵 정지 탈출)는 §2.1.
- 확인: LJ Phase 4 재생 목록(전투 → 로비 복귀 → 다시 출발해도 새 Run 한 번, 재진입 뒤 아티팩트 편집)을 사용자가 "문제 없음"으로 확인했다(10-23). 전투 포기 뒤 로비 복귀·팝업 재사용도 10-24 확인. 최종 승리 경로는 미확인(§2.2).

### 3.12 로비 플레이어 고정 아킴보

- 2026-10-16 사용자 요청: 캡슐 플레이어를 아킴보로. `Lobby.unity`의 표시 자식을 `StageVisual_PlayerAkimbo.prefab` 인스턴스 하나로 바꿨다(이동 루트·Anchor·카메라 유지). LV6(선택 캐릭터 연결)이 아니다. 커밋 `24f4f3aa`.
- 바뀐 것: 씬 고정 인스턴스 → 10-24 B(전투 `UnitVisual_MainCharacter` 공유) → 10-25 §11(모델 SO의 Akimbo 프리팹 직접 연결, 런타임 생성).

### 3.13 RS Run 결과(엔딩) 표시

- RS-L1(10-20, `83e53dac`): 최종 승리 뒤 로비에 도착하면 `LastSummary`(Victory, 처음 보는 객체 참조)를 한 번 보이는 안. 결과 모듈·세션·팝업 Controller·`UIId.EndingRunSummary = 33`(32는 DeckEdit)·Layer `EndingRunSummaryPopup`·`EndingPanel.prefab`(사용자 지시: Ending 이름, LobbyCanvas 아래 단일 루트). 첫 표시에서 `LobbyInputFactory` 등록 배열에 Layer가 빠진 것을 찾아 더했다.
- **바뀐 것(10-20 사용자 확정)**: "전투 씬에서 EndingPanel 표시 → 닫으면 로비". RS-C1(`cb76bd2c`)이 RS-L1을 대신했다. 로비 자동 표시 모듈·세션·Controller·Screen·전용 Layer·host 노출을 지웠다. UI는 `Screens/Stage/EndingRunSummaryUI.cs`, 프리팹은 `Prefabs/UI/EndingPanel.prefab`(GUID `c5e3f2aa04b348e29e6013347c2d3976` 유지, MovedFrom). UIId 33은 Parent None·Policy Parallel, `OpenUI(ID, true)` 고정 표시다. 최종 여부와 세 통계(`GoldSpent`·`FameEarned`·`EnemiesDefeated`)는 Core가 게시한 값만 쓴다.
- RS-C1은 당시 보스 승리만 정리를 먼저 하고(EndBattle → CompleteChapter → 결과 표시) 확인 뒤 새 Run을 준비해 로비로 갔다. 이 보스 특수 경로와 전투 쪽 새 Run은 10-22 용근님 전투 구조 개선 Phase 1이 걷었다(지금은 모든 결과가 정리 → 표시 → 확인 → 떠나기).
- 10-24 용근님 쪽: 엔딩 패널이 병합 `9e0c0f2b`에서 Combat 씬에서 빠진 것(H1)을 사용자가 `BossEndingCanvas`(켜짐) 아래에 다시 두고 연결했다.
- 10-26 로비 잔여 정리(사용자 요청): `Lobby.unity`에 남은 EndingPanel 인스턴스(`9900000000`)를 지웠다. 지금 Combat 씬에 `BossEndingCanvas / EndingPanel`과 `CombatEndUI.ending`(`1470310029`) 연결이 있다. 표시 경로: 정리 → 완료 표시 때 `CombatEndUI`가 전투 승리와 `RunOutcome.Victory`를 확인해 `EndingRunSummaryUI.Present` → 확인 뒤 `CombatPlan.DestinationOf` → Lobby. 중간 챕터 보스는 일반 승리 화면이다. 컴파일·실제 보스 승리 경로는 미확인.
- 확인: 임시 요약(Core·Buffer에 쓰지 않음)으로 표시·세 값·확인 버튼·EventSystem 클릭/Submit·BackCloseUI 뒤 입력 복원까지만. 개발 Boss 정의가 진입 대상 불일치로 실패해 실제 보스 처치 경로는 보지 못했다(§2.2).
- Run 요약 설계(§10.5·§10.6)는 천명님 문서 정리 `4709feba`에서 지워졌다(옛 경로 `specs/2026-10-20-run-summary-statistics-design.md`).

### 3.14 LJ 로비 실행 계층 Job·Plan 재구성

- 2026-10-23, 기준 `8644c702`. 설계 결정 Q-LJ1~Q-LJ9·P1~P10은 설계에 있다. 착수 기준점 **Q-T1: B 결정**(10-23 사용자, 권장 A 대신) — 지금 브랜치에서 시작하고 용근님 브랜치와 겹치는 줄(`LifeCycleSystemFactory.CreateComposition`·`JobRegisterSystem`·`LobbyDepartureIntegrationTests`)은 병합 때 맞춘다.
- 계획에서 정한 것 **LP1~LP9 그대로 확정(10-23 사용자)**:

| ID | 내용 |
|---|---|
| LP1 | `LobbyArtifactDomain`이 아티팩트 사실을 읽고 `LobbyProgress`가 로비 사실과 합쳐 `LobbyProgressFacts`를 만든다. 확인 Job은 도메인 안에서 만들어 순환이 없다 |
| LP2 | `LobbyArtifactRequestKind { Edit, Preview }` |
| LP3 | 확인 Job 우선순위 3(호출 Job과 같아 바로 뒤) |
| LP4 | 보드 초기화 생성·우선순위를 `ArtifactBoardInitializeJob.Create`(`LifeCycleSystem/Common/`)로 |
| LP5 | `LobbyLifeCycle` 생성자 인자 모두 필수, 대체 생성 없음 |
| LP6 | `LobbyStepKind { None, ArtifactEdit, ArtifactPreview, Depart, Broken }`·`LobbyStep` |
| LP7 | 할 일 없음은 공유 빈 목록 |
| LP8 | 로스터 확인 문구는 옛 Flow 문구 그대로 |
| LP9 | 비게 되는 `LifeCycles/Artifact/Jobs`·`Plans` 폴더 삭제 |

| Phase | 결과 | 계획과 다른 점 | 확인 |
|---|---|---|---|
| 0 | 아티팩트 편집 Plan·Job을 `LifeCycles/Lobby/Artifact/`로, 보드 초기화 Job을 `LifeCycleSystem/Common/`으로(GUID 유지). `ArtifactLifeCycle.cs`는 남김 | 목록에 없던 `LobbyDomainStopTests` using도 맞춤 | 사용자 문제 없음 |
| 1 | 새 `ArtifactEditFacts`·`LobbyArtifactDomain`·`LobbyArtifactCheckJob`·`LobbyProgressFacts`·`LobbyProgress`·`LobbyStep`, `LobbyPlan.Decide`·`CanLeave`, 서비스 키 둘·그룹 속성 둘 | `ILifeCycleSystemGroupService` 구현 시험 가짜 셋에 속성 추가(§4.2) | 사용자 문제 없음 |
| 2 | 출발 요청 질문 `CanResubmit`·`IsRejected`·`IsFaulted`, Presenter는 질문만 읽음 | — | 사용자 문제 없음 |
| 3 | `LobbyRunStartJob` 안 로스터 확인(Q-LJ9 B), 새 `LobbyTransitionJob` | 문구 꼬리표만 `[LobbyRunStartJob]`, `LobbyDepartureTests` 컴파일 맞춤 | 사용자 문제 없음 |
| 4 | `LobbyLifeCycle`은 트리거(Runner가 비면 `Register(Lobby, Next)`, -200에서 `CanLeave`), `LobbyPlan.Register(service, jobType, buffers, lifeCycles)`, Flow 셋 삭제, 요청 단계에서 `Queued`·`IsJobCompleted`·`MarkQueued`·`CompleteJob` 삭제. 동작 변화는 설계 §5 B1~B11 | `Fail`은 끝나지 않은 단계에서만, 성공 검사는 전이 표 하나, 생성자는 `ILobbyProgressReader`, `StageRunOriginRegressionTests`는 파일 안 작은 대역, Unity 확인에서 `LobbyArtifactDepartureTests` 추가 삭제 | 사용자 문제 없음(재생 목록 포함) |
| 5 | 안내·공유 메모 셋(§4.1) | — | 문서 |

- Phase 4 전 성능 점검(사용자 요청): 로비 대기 중 매 프레임 길에 할당이 없도록 `Next`의 할 일 없음은 목록을 만들기 전에 `NoJobs`를 돌려준다.
- 커밋: Phase 0~1 `e46398d4`, Phase 2~5 `1a386b64`.

### 3.15 카메라 이전 — Boot 출력 재사용과 고정 LobbyCamera 하나

- 2026-10-24, 설계 3판 §7 사용자 승인. 기준 develop 병합 `7e8d8eb4`. 출력은 Boot Main Camera, 로비 씬에는 고정 가상 카메라 `LobbyCamera` 하나다. 구도 이동·줌 코드는 넣지 않는다.
- 구현 결정 카메라 LC1~LC10(Core 계약 LC0~LC4와 다른 번호다): LC1 출력 출처 `Func<Camera>`(운영 `Camera.main`)를 Attach 때 한 번 읽음 / LC2 출력 Camera·Brain 필수, 가상 카메라 고정 직교·타깃 없음 / LC3 Scope가 배경 Canvas `worldCamera` 저장·연결·되돌림, 렌더 모드·깊이는 연결 뒤 검사 / LC4 `ArtifactWorldBackground` 이벤트 카메라 비움 / LC5 별도 아티팩트 카메라 칸 안 만듦 / LC6 `IArtifactPopupOutput { Enter; Exit }`·`ArtifactPopupOutputMask` / LC7 출력 이름은 출력 Camera의 `gameObject.name` / LC8 UI 층을 한 번 읽고 없으면 예외 / LC9 표시 뒤 Enter, 숨김·Dispose 입구에서 먼저 Exit / LC10 구도 값 보관·`Customize` 없음, 보드 배치 원본은 씬 `ArtifactWorldRoot`.

| Phase | 결과 |
|---|---|
| P1 | `LobbyViewSystem.BindServices`에 출력 카탈로그·출력 카메라 출처, `LobbyViewScope` 생성자 `output` 필수, `LobbySceneRoot`·`ArtifactSceneRoot`의 `outputCamera` 칸 삭제, 씬 팝업 Factory `Create(root, input, host, output)` |
| P2 | 아티팩트 팝업이 열린 동안만 Boot 출력 Culling Mask에 UI 층을 더하고 닫기·이탈·오류 정리에서 되돌림 |
| P3 | 사용자가 `LobbyOutputCamera`·`LobbyArtifactCamera`를 지우고 Canvas 카메라 칸을 비움. 사용자 위임으로 `ArtifactWorldRoot`·`ArtifactWorldBackground` 배치를 고정 구도에 맞춤(씬 값이 원본). 씬에 Unity Camera·Brain·MainCamera 태그 0, 가상 카메라 1 |
| P4 | 공유 메모(§4.1) |

- P3 재생 뒤 고친 것: ① 배경 Canvas 검사 실패 — 카메라가 비어 있는 Screen Space Canvas는 Overlay처럼 동작한다. 렌더 모드 검사를 출력 카메라 연결 뒤(`ValidateConnectedBackground`)로 옮겼다. ② 화면 흐림 — 사용자가 "스테이지 선택 카메라 기준"을 골라 `LobbyCamera`에 `CinemachineVolumeSettings`(DefaultVolumeProfile)를 더했다. 사용자 "화면 흐림은 사라졌어".
- 확인(10-24 사용자): 팝업 열기·닫기·드래그 배치·구도 유지, 전투 포기 뒤 로비 복귀·팝업 재사용·Culling Mask 복원·콘솔 오류 없음, "기존 EditMode 테스트도 전부 통과했어. 확인 완료로 기록하고 로비 카메라 이전 작업 마무리해줘". 일반 패배·최종 승리·낮/밤 전환·오류 주입·Profiler는 따로 하지 않았다.
- 커밋 `1fcd474f`. 커밋 때 무관한 에디터 변경(StageSelect 루트 순서·폰트 아틀라스·FMOD)은 따로 정하기로 했다.

### 3.16 정리 P1~P3

- 설계 D1~D4(로비 점검 1·3·4번). 작업 규칙: 새 시험·fixture·단언 없음, MSBuild 없음.
- **P1(D1)**, 10-24, 기준 `4ab8a2d4`: `LobbyDeckUI`만 고쳤다. 닫기 버튼은 `EnsureCloseBinding`으로 한 번만 연결, 목록 항목은 Instantiate 직후에만 `Bind`, `RebindControls` 삭제, `ClearControlInput`은 수신자를 null로 바꾸지 않는다. 사용자 확인(10-24): 덱 팝업 열기·닫기·닫기 버튼·닫은 뒤 로비 조작 정상. 키보드·게임패드 Submit은 미보고.
- **P2(D2)**, 10-24: `LobbyVisualData`만 고쳤다. 선택 시설의 Unity-null을 C# null로 정규화하고, 구조 검사가 모두 성공한 뒤 결과를 기억한다(이후는 생존 확인만). 단독 Unity 확인 기록은 없다. 같은 코드 위에서 R1(10-24) 사용자가 기존 EditMode 통과·로비 동작 정상을 보고했다.
- **P3(D4)**, 10-24 사용자 승인 "현재 씬 유지, 4×4·시작 (1,2)"(옛 11×9 계획을 대신): `LobbySceneRoot` 기본값 여섯을 씬 값에 맞추고, 우리 `LobbyViewSystemTests` fixture에 `rowSpacing = 0f` 한 줄. 바뀐 것: 같은 날 A가 격자 필드 자체를 지워 이 기본값은 지금 없다.
- 커밋 `83fb2767`.

### 3.17 C Canvas 통합

- 2026-10-24 사용자 요구: 로비 Canvas 다섯을 줄이고, `Panel` 이름 오브젝트에는 Canvas를 두지 않으며 `Canvas → Panel` 계층으로, `GlobalUICanvas` Sort Order 5 아래(튜토리얼 제외).
- 결과(사용자 직접 승인): 배경 둘(`LobbyBackgroundCanvas` Screen Space-Camera, `ArtifactWorldBackground` World Space)은 두고, Overlay 셋을 `LobbyCanvas`(Sort Order 2, Scaler 1920×1080·match 0.5, Raycaster 하나) 아래 `LobbyPanel`·`LobbyDeckPanel`·`ArtifactPopupPanel`로 합쳤다. `LobbyDeckPanel.prefab`에서 Canvas·Scaler·Raycaster를 지웠다(GUID 유지). 안내 TMP 셋의 Raycast Target을 껐다. 우리 `LobbySceneAssetTests` 한 줄을 `GetComponentInParent<Canvas>()`로.
- 조사 중 본 다른 화면(보고만): `Combat.unity` `CombatEndCanvas` 6, `DeckEditUI.prefab` 100, `GlobalUICanvas` 안 `OptionCanvas` 99, `TutorialUI.prefab` 110(예외 대상).
- 확인(10-24 사용자): 시설 클릭·덱/아티팩트 팝업 열기·닫기·드래그·닫은 뒤 로비 조작 정상. 커밋 `83fb2767`.

### 3.18 R1 방 모델 DB 이전

- 2026-10-24 사용자 승인. 모델 Data/SO에 필수 `RoomPrefab`, `LobbySceneRoot.RoomVisualRoot`, `LobbyVisualSet`이 방도 생성·정리. 새 `Prefabs/Lobby/Visuals/LobbyRoom.prefab`(identity 래퍼 안에 원본 `Lobby.prefab` 중첩)을 운영 SO에 연결하고 씬의 직접 방 인스턴스를 지웠다. 원본 `Lobby.prefab`은 보존.
- 작업 시작 전 사용자가 지운 임시 시설 프리팹 셋을 보존했다. 우리 시험 다섯 파일만 맞췄다.
- 확인(10-24 사용자): 방 배치·시설 클릭·아티팩트 팝업의 방 숨김/복원·전투 포기 뒤 로비 복귀 정상, 기존 EditMode 통과. 커밋 `83fb2767`.
- 바뀐 것: 표시 루트 → §3.21 §10에서 방·바닥을 `LobbyWorld` 직속 생성으로. 래퍼 `LobbyRoom` → LB-1에서 운영 참조를 `Lobby.prefab`으로.

### 3.19 A 직접 Anchor와 B 전투 메인 외형 공유

- 사용자 결정(10-24): 위치 구조는 작업자 추천에 맡김, 3D 클릭과 전용 Collider는 유지.
- **A**: `LobbySceneRoot` 격자 여섯 필드·시설 칸 두 필드 삭제, `boardRoot` → `worldRoot`, `LobbyGridPlacement` → 읽기 전용 `LobbySceneLayout`(GUID 유지), `LobbyBoardMapper` 삭제. 씬 위치는 기존 런타임 결과로 굳히고, 도착 Anchor는 시설 자식으로, Collider는 `SelectionArea` 자식으로 옮겼다. 시설 안내 글자(FacilityLabel) 셋 삭제. 끝난 격자 기능의 시험 메서드 열 개(변환·간격 여섯, 점유·인접 넷)를 지웠다. 확인(10-24 사용자): 시설 클릭·이동·팝업 숨김/복원·전투 포기 뒤 복귀 정상, 기존 EditMode 통과.
- **B**: 사용자가 "이번 B만" Codex의 코드·문서·씬 수정을 허용했다. 모델 SO `characterVisual`(UnitVisual SO 참조 → 키)로 전투 `UnitVisual_MainCharacter`를 조회하고, 새 `LobbyCharacterVisual`이 복제본(Collider 끔·Layer 0·Animator root motion/events 끔)을 소유한다. Boot에서 `KeyNotFoundException`이 나 `UnitVisualDataTable.asset`에 `UnitVisual_MainCharacter`(GUID `5278bed9a6964cf9a7ec88b19bc0c271`) 한 줄을 등록했다(지금도 등록돼 있음). 확인(10-24 사용자): 외형·Idle·크기·접지·시설 이동·팝업·전투 포기 뒤 복귀 정상.
- 바뀐 것: B의 UnitVisual 조회 → 10-25 §11에서 모델 SO의 Akimbo 프리팹 직접 참조. 커밋 `83fb2767`.

### 3.20 L-0~L-3 배치·이동·표시 보정 DB 1차

- 2026-10-25. 사용자 "계획대로 L-1 C# 계약·소비자 이전" 승인으로 Q-L1(기존 SO 확장)·Q-L2(`Rotation4`)·P4(공통 이동 값 검사 위치)를 적용했다. **Q-L6(10-25 사용자)**: 진입·재연결 때 위치와 방향을 모두 DB 시작 설정으로 되돌린다.
- L-1(C#): 새 `LobbyLayoutData`·`LobbyBoard`(시작/도착 칸 → 월드 자세)·`LobbyRoomBinding`(방 구성 검증·생성물 시설 수집)·`ChessPieceMotionRules`(이동 값 공통 검사). 시설의 `ArrivalAnchor`, SceneRoot의 시설·시작 Anchor·이동 값 참조 삭제. 씬 Anchor 소유 계약이 없어져 우리 시험 메서드 일곱을 지웠다.
- L-2(에셋): L-1 뒤 재생에서 캐릭터 루트 축척 예외(중간 상태)가 나 사용자가 L-2를 승인했다. 씬 Anchor 넷 삭제, 시설 세 묶음을 `LobbyRoom.prefab`으로, `LobbyVisual_Default`에 4×4 배치판·시작/도착 칸·이동 값·표시 보정 저장. 옮기기 전후 자세·클릭 박스 모서리·캐릭터 표시 행렬이 같음을 정적으로 대조했다.
- L-3(문서): 사용자 "로비 진입이랑 시설 클릭 잘 돼." [로비 안내](../viewDocs/lobby-guide-Hojin.md) 3부 갱신.
- 후속(10-25 사용자 요청): `ChessPieceMotionRules`를 `DataBaseSystem/Data/View/Movement/`로 옮겼다(런타임 Common 폴더 정리는 [View 구조 계획](2026-10-13-view-structure-plan-Hojin.md)).
- 바뀐 것: 한 SO의 Layout → R-1에서 역할별 SO 넷. 커밋 `6c56a50d`.

### 3.21 R-1~R-3 역할별 SO 넷과 후속

- 사용자 선택: 모델 / 배치판·정착 칸 / 이동 연출 / 캐릭터 표시 보정 네 역할. 10-25 "계획대로 R-1 계약·소비자·에셋 일괄 이전".
- **R-1**: `LobbyVisualDataSO` → `LobbyModelDataSO`(MovedFrom), `LobbyVisualData` → `LobbyModelData`, `LobbyLayoutData` → `LobbyBoardData`(모두 GUID 유지). 새 `LobbyBoardDataSO`·`LobbyMotionDataSO`/`Data`·`LobbyCharacterPresentationDataSO`/`Data`. Query는 `Models`·`Board`·`Motion`·`CharacterPresentation` 넷을 공개한다. 에셋 `LobbyVisual_Default` → `LobbyModel_Default`(GUID `64b4c66bbe2e17f449e843256009488f` 유지), 새 `LobbyBoard_Default`·`LobbyMotion_Default`·`LobbyCharacterPresentation_Default`. 테이블 `databaseItems` 네 참조가 유일한 원본. 확인: 사용자 "로비 진입이랑 시설 클릭 잘 돼."
- **R-2**: 새 `DataBaseSystem/Editor/LobbyVisualDataTableSOEditor.cs` — 역할별 typed ObjectField 넷, 잘못된 목록은 오류와 원본 목록 편집으로 잠금. 확인(사용자): 네 칸 표시·연결 변경·Undo/Redo 정상. 저장·재선택·오류 목록 복구는 미확인(§2.2).
- **R-3**: [로비 안내](../viewDocs/lobby-guide-Hojin.md) 3부를 역할별 SO로.
- **§10 방·바닥 빈 부모 제거**(10-25 사용자 승인): 사용자가 씬에서 지운 `RoomVisualRoot`·`FloorVisualRoot`의 코드 연결을 없애고 방·바닥을 `LobbyWorld` 바로 아래에 만든다. 폐기된 바닥 슬롯 TestCase 여덟 삭제.
- **§11 캐릭터 런타임 생성**(10-25 사용자 지시): `MainCharacterDisplayFixture`·`CharacterVisualRoot`를 씬에서 지우고 `LobbyCharacterVisual`이 `LobbyWorld` 아래 이동 루트·보정 루트·모델 복제본을 만든다. 프리팹 출처는 사용자 선택 "로비 모델 SO에 Akimbo 프리팹 직접 연결" — `characterVisual` → `characterPrefab`(`Akimbo.prefab`, GUID `836049dcf6ea66245ae56c7a5419936f`), UnitVisual 조회 삭제, 검사는 `LobbyModelData.ValidateCharacterPrefab` 한 곳. 사용자가 보고한 우리 시험 컴파일 오류 둘(CS1061·CS1501)을 한 줄씩 고쳤다.
- 확인(10-25 사용자): 컴파일 오류 해소·로비 진입·캐릭터 이동·시설 클릭·재진입 정상. 지금 `LobbySceneRoot` 직속 자식은 `LobbyWorld` 하나다. 커밋 `6c56a50d`.

### 3.22 LB-1 Lobby.prefab 시설 자동 연결

- 2026-10-26 사용자 "진행해." 모델 Data는 `LobbyModelData(roomPrefab, floorPrefab, characterPrefab)` — 선택 시설 모델 슬롯 셋을 지웠다. `LobbyFacilityView`는 직렬화 `kind`만 갖고 같은 객체의 BoxCollider를 연결 때 수집·검증한다. 이름·형제 순서로 종류를 정하지 않는다. 방 원본의 저작 변환을 복제본에 보존한다(바닥은 identity 유지). 생성물은 방·바닥 둘.
- 에셋: `Lobby.prefab`(GUID `70cbd2cc01d789e4e98518a1e3241ef3`)의 Artifact/StartStage/Deck에 `LobbyFacilityView` kind 0/1/2를 더했다(사용자 Transform·Collider 보존). `LobbyModel_Default`의 roomPrefab을 이 프리팹으로 바꿨다. `LobbyRoom.prefab`은 지우지 않았다.
- 우리 시험을 같은 객체 Collider 계약으로 옮겼다. 시설 메시가 없는 통합 방이라 이동 기하 smoke의 시설 메시 검사를 지웠다.
- 확인: 외부 `dotnet build` 오류 0(Unity 실행 아님). Unity EditMode는 다른 에디터가 열려 있어 돌리지 못했다. 이 코드 위의 검사 후속 정리 R-a를 사용자가 10-27 확인·커밋했다(`45b20161`, 재생 로비 진입 → 시설 셋 → 출발 포함, [검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md)). 선택 박스의 화면 가시성·통합 방과 이동 경로 간섭은 따로 보지 않았다.
- 커밋 `8ee9984e`.

### 3.23 HM 칸마다 뛰는 이동

- 설계는 [로비 설계 §15](../specs/2026-10-11-lobby-design-Hojin.md#15-칸마다-뛰는-이동-hm1hm6-2026-10-29-초안)다. 결정은 Q-HM1~Q-HM3 모두 A, 구현 선택은 TP-HM1~TP-HM10 그대로다.
- 추가 결정(사용자, 2026-10-29)
  - StageSelect 파일은 고치지 않는다("1로"). 지금 설정 생성자를 그대로 두고 경로 모양 생성자를 더한다.
  - 들기 모양은 Common에 둔다(A).
- 구현 선택(TP-HM, 코드 주석이 가리킴)

| TP | 결정 |
|---|---|
| TP-HM1 | 이름: `ChessPiecePath`·`ChessPieceLiftPath`(Common), `LobbyViewHopPath`(Lobby) |
| TP-HM2 | 계약은 `Plan`(경로 준비, 전체 시간)·`Evaluate`(경과 시간 → 자세, 지금 회전은 대체 값) |
| TP-HM3 | 바라볼 점 → 회전 식은 `ChessPiecePath.FacingRotation` 하나 |
| TP-HM4 | `Plan`은 Delay 시각을 읽고 토큰을 다시 확인한 뒤에 부름 |
| TP-HM5 | 시간이 유한 양수가 아니면 예외. 도착 위치는 정확히 맞추고, 회전은 마지막 자세 |
| TP-HM6 | 설정 생성자 유지(StageSelect 무변경), 경로 모양 생성자 추가 |
| TP-HM7 | `LobbyBoard`는 `WorldRoot` 변환을 연결 때 한 번 잡음. 칸 → 위치 식은 `PositionOf` 하나 |
| TP-HM8 | `CellAt`은 칸 중심에서 칸 크기 0.1% 넘게 벗어나거나 판 밖이면 예외 |
| TP-HM9 | 같은 방향 기준은 0.1°(전투 걸음 회전과 같은 값, 따로 둠) |
| TP-HM10 | 구간 목록은 `Plan`마다 다시 채우고, `Evaluate`는 할당 없음 |
- **HM-a 경로 모양 분리**
  - 새 `ViewSystem/Common/ChessPiecePath.cs`·`ChessPieceLiftPath.cs`를 만들고 `ChessPieceMotion.cs`를 고쳤다. StageSelect·로비 파일은 고치지 않아 동작이 같다.
  - Ua 문제 없음(사용자).
- **HM-b 로비 뛰기**
  - 새 `ViewSystem/Lobby/LobbyViewHopPath.cs`를 만들었다.
  - `LobbyBoard`(`PositionOf`·`CellAt`·`IsAlongBoardZ`), `LobbyViewInput` 생성 한 줄, 이동 SO 셋(`hopHeight`·`hopSeconds`·`turnSeconds`), `ChessPieceMotionRules` 주석을 고쳤다.
  - Ub 문제 없음(사용자). 사용자가 확인하며 `hopHeight`를 맞췄다(지금 에셋 1).
- **HM-c 문서·검토**
  - 로비 설계 §1.2·§2.2·§4.2·§5.1·§5.8과 §15.9 구현 메모, 로비 안내 §1.1·§2·§4.2·확인 목록, 스테이지 맵 설계의 `ChessPieceMotion` 줄 셋을 고쳤다.
  - 묶음 끝 검토(새 에이전트 하나)는 높음·중간 0, 낮음 3이었다. 낮음 1(문서 값)만 반영했다(설계 §15.9).
  - Codex 검수는 연결 전이라 하지 않았다.
- 계획과 다른 점: 설계 §15.9의 넷이다.
  1. 예약 문맥의 바라볼 점 칸 삭제
  2. `Plan` 예외 처리
  3. 같은 칸·다른 위치일 때 예외
  4. 마지막 걸음 목표 = 받은 도착 위치
- 새 시험은 없다. 컴파일·재생은 사용자 Unity 확인이다.
- **커밋 묶음(사용자)**
  - 새 `.cs` 셋과 Unity가 만든 `.meta` 셋
  - `ChessPieceMotion.cs`·`LobbyBoard.cs`·`LobbyViewInput.cs`·`ChessPieceMotionRules.cs`
  - `LobbyMotionData.cs`·`LobbyMotionDataSO.cs`·`LobbyMotion_Default.asset`(한 커밋)
  - 문서 넷(로비 설계·계획·안내, 스테이지 맵 설계)
  - `BOOM.prefab`·`ArtifactBoardInitializeJob.cs`는 이 작업이 아니다.

## 4 공유 메모와 다른 작업자 파일

### 4.1 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [Core 전달 문서 §13.12 로비 Core 계약 요청(LC0~LC3)](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#1312-로비-core-계약-요청-2026-10-11-미전달) | 천명님 | 미전달 |
| [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.12 LV8A Buffer 조립 | 천명님 | 기록(전달 기록 없음) |
| [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.51 로비 Job·Plan으로 고친 천명님 시험 | 천명님 | 미전달 |
| [UI 이력 문제 인계](../handoffs/2026-10-12-lobby-settings-ui-history-handoff-Hojin.md) | UI 작업자(현강님) | 미전달(사용자 보류) |
| [로비 Job·Plan Framework 공유](../handoffs/2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md) | 민형님 | 미전달 |
| [로비 Job·Plan 전투 공유](../handoffs/2026-10-23-lobby-job-plan-combat-share-yonggeun-Hojin.md) | 용근님 | 미전달 |
| [StageSelect 카메라 Framework 인계](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md) §9 로비 고정 카메라·Boot 출력 재사용 | 민형님 | 미전달 |

### 4.2 공용·다른 작업자 파일을 고친 기록

| 작업 | 파일(영역) | 고친 것 |
|---|---|---|
| LV1 | `LifeCycleSystem/Base/SceneIndices.cs`(민형님), `GameBootstrap`(용근님 부트 영역), Build Settings | Lobby 씬 번호·경로, 시작 enum `Lobby` |
| LV2·LV4 | 루트 `RunTimeSystemFactory.cs`(민형님) | 로비 입력 슬롯·Controller·Layer 등록, `LobbyDeckPopup` Layer 한 줄(10-12 사용자 승인) |
| LV4·RS | `UIIDs.cs`(공용 UI) | `LobbyDeck = 15`, `EndingRunSummary = 33` |
| LV5·LJ | `LifeCycleSystemFactory`·`LifeCycleSystemGroup`·`LifeCycleSystemServiceKey`·`JobRegisterSystem` 로비 줄(민형님 연결 줄) | 보드 준비 서비스, 로비 사실·도메인 서비스, 로비 Plan 대리자 |
| LV8A | `BufferSystemGroup.cs`·`BufferSystemFactory.cs`·`DataBaseGroupServiceKey.cs`(Buffer·Factory·키) | 로비 출발 Buffer 접근자·조회·키(Core 전달 문서 §5.12) |
| LVM1 | `ViewDataBaseFactory.cs`(Factory)·`ViewDataBase.cs`·`ViewGroup.asset` | LobbyVisual 표 조립·등록 |
| RS-C1 | `CombatPlan`·`CombatExitJobs`·`CombatExitContext`·`ICombatProgressView`·`CombatViewSystem`·`CombatEndUI`·`Combat.unity`(용근님 전투 영역) | 보스 승리 선행 정리·엔딩 연결. 용근님이 10-21 분석(SK1)에서 다루고 10-22 개선 Phase 1에서 걷었다 |
| LJ Phase 1 | `BootStartButtonTests.Group`(현강·호진님), `TestLifeCycleGroup`(용근·호진님) | 그룹 인터페이스 새 속성 둘 추가(null) |
| LJ Phase 4 | `StageRunOriginRegressionTests`(천명님), `LobbyDepartureTests` 삭제(천명님 `f6a18f3f` 줄 포함), `LobbyDepartureIntegrationTests`·`LobbyDepartureBootSmokeTests`·`FrameworkRuntimeCompositionTests`(여러 작업자) | 지운 요청 단계·Flow에 맞춤(확인 내용 유지) |

## 5 관련 계획

- [검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md): 로비 이름 조회 제거(N-2)·필수 View 의존(R-a)·참조 없는 출발 안내 파일 삭제.
- [새 Core 규칙 대응 계획](2026-10-26-new-core-lobby-stage-response-plan-Hojin.md): 로비·스테이지 선택의 새 Core 대응(결정 대기).
- [스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md): 10-26 화면 UI 소유 정리(`LobbyCanvasView`·`LobbyBackgroundView` 등 로비 Canvas 분리, `334fb533`).
- [View 구조 계획](2026-10-13-view-structure-plan-Hojin.md): `LobbyViewScope`·`PopupViewScope`(10-15), 런타임 Common 폴더 정리(10-25).


### 2026-10-30 아티팩트 전용 배경 제거

- 사용자 승인: LobbyBackgroundCanvas는 유지하고 ArtifactWorldBackground를 제거한다. 앞선 배경 Canvas 둘 유지 결정은 이 범위에서 대체한다.
- Lobby 씬의 전용 배경과 자식 Image, Presenter 참조를 제거했다. 전용 배경 레이아웃 클래스·메타, 카메라 렌더 이벤트 구독, 거리·여백 설정도 함께 제거했다.
- 드래그 표시 초기화는 PrepareDragDisplay로 유지하며 보드 피킹·출력 마스크·로비 WorldRoot 숨김/복귀·UISystem 등록/개폐 계약은 유지한다. Core와 FieldCamera는 변경하지 않았다.
- SSOT/SOLID/FW: 배경 소유권은 기존 LobbyBackgroundView에 유지한다. 새 서비스나 등록 경로를 추가하지 않고 전용 배경 책임만 제거한다.
- 검증: 제거 대상 참조와 씬 직렬화 연결 정적 검사. Unity 컴파일·재생은 미확인.

### 2026-11-02 로비 방 시설 안내 UI 허용

- 사용자 지시: 로비 프리팹의 Canvas·TMP 안내 표시를 로비에만 허용한다.
- 범위: 설계 §5.4의 방 전용 추가 허용 목록만 확장한다. Task는 `ValidateRoom`의 기존 추가 허용 경로에 UI 타입 판정을 연결하고 설계 문서를 맞추는 단일 수정이다.
- 구현: `LobbyVisualPrefabRules.IsRoomUiComponent`가 설계 §5.4의 UI 타입만 정확히 허용한다. 바닥 검사·공용 `ViewPrefabRules`·시설 셋과 Collider 조건은 유지한다. 사용자가 편집한 `Lobby.prefab`은 수정하지 않는다.
- SSOT·SOLID·JMH/FW·기능 독립성: 로비 방 허용 판단은 기존 DB 검사 한 곳에 둔다. 공용 검사·Factory 조립·서비스 등록·화면 개폐 계약은 변경하지 않는다. 안내 글자는 방에 속한 표시 요소이며 독립 화면·팝업을 새로 만들지 않는다. 이전에는 안내 UI를 추가하면 DB 생성이 막혔고, 이후에는 방 전용 허용 경로로 통과하므로 다른 기능의 구현·검증 범위를 넓힐 필요가 없다.
- 검증: 변경 diff와 현재 프리팹의 UI 컴포넌트 구성을 정적으로 대조한다. Test 코드 작성·MSBuild 실행 없음. **컴파일 미확인**, Unity 재생 미확인. 사용자 확인: 새 Play에서 DB 생성·로비 진입, 시설 안내 표시와 기존 시설 클릭·팝업 동작.

### 2026-11-02 로비 프리팹 Layer 검사 해제

- 사용자 지시: "Default로만 되게 하지 말고 다른 Layer들도 되게". 시설 안내 글자를 새 Layer `UIRender`(29)에 두어 `[LobbyModelData] RoomPrefab: 'Text (TMP)'의 Layer는 Default여야 합니다.`로 부트가 멈췄다.
- 구현: `LobbyVisualPrefabRules.ValidateStructure`의 Layer 검사를 걷었다(방·바닥 모두). 설계 §5.5 4번·안내 §3.2를 고쳤다.
- 검증: 컴파일 미확인, Unity 재생 미확인. 커밋 때 `ProjectSettings/TagManager.asset`(Layer 이름)을 `Lobby.prefab`·`Lobby.unity`와 함께 넣는다.

### 2026-11-02 시설 호버 글자 빛남

- 사용자 요청: 아티팩트·덱 편집·여정 시작 시설을 호버하면 글자가 반짝이고, 값은 SO에서 고친다. 결정은 설계 §16.1(글자 색 + 빛 번짐, Light 안 씀).
- 같은 날 바뀐 순서: "한 번 번쩍 후 유지" → "계속 번쩍이면서 어느 정도 밝은 채"(사이 시간 칸 추가) → "반복 번쩍임 말고 조금 쎄게 빛나는 것". 마지막 안에서 번쩍임 칸 넷(오름·내림·사이·세기)과 시계·프레임마다 하는 일을 걷고, 기본값을 더 밝게(글자 거의 흰색, 빛 알파 1, 크기 0.45) 바꿨다.
- 구현: 설계 §16.2·§16.5. 정한 것 P1~P6은 설계 §16.3(사용자 확인 대상).
- 검증: Test 코드 작성·MSBuild 실행 없음. **컴파일 미확인**, Unity 재생 미확인. 확인 목록은 설계 §16.4.
