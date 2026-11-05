# View 구조 계획

- 작성자: 호진(이호진)
- 상태: 비전투 View Scope P0~P6 완료(사용자 Play 확인 2026-10-15), 화면별 입력 조립 Phase1~3·병합 정합 완료(2026-10-18), 책임 분리 Phase1·2 완료(2026-10-18), Common 폴더 정리 완료(2026-10-25). 책임 분리 Phase3~6은 미착수다. 공유 메모는 모두 미전달이다.
- 설계: [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)
- 정리: 2026-10-27 `2026-10-13-noncombat-view-scope-plan-Hojin.md`, `2026-10-17-view-input-composition-plan-Hojin.md`, `2026-10-18-view-responsibility-separation-plan-Hojin.md`, `2026-10-25-runtime-common-folder-cleanup-Hojin.md`를 이 문서로 합쳤다. 끝난 Task 단계·시험 수치·증거 경로·기존 실패 목록은 git `10914570`에 있다.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| Scope P0 | 기준선 고정(HEAD `92440b83`), 재진입·실패 경로 기준 목록, 설계 §5.2 재대조 | 완료 2026-10-14 | 자동 실행 기록 | 없음 |
| Scope P1 | `CleanupErrors`, 팝업 계약 셋, `PopupViewScope`, 덱 팝업 이전 | 완료 2026-10-15 | 자동 검증 | `ba0c9d7e` |
| Scope P2 | `LobbyViewScope`, 출발 Presenter·문구 | 완료 2026-10-15 | 사용자 Play 3항목 통과 | `ba0c9d7e` |
| Scope P3 | 아티팩트 흡수(`ArtifactViewSystem`·`ArtifactPopupSession` 삭제) | 완료 2026-10-15 | 사용자 Play 4항목 통과 | `ba0c9d7e` |
| Scope P4 | 로비 기록 누락 정책(D11·D20) | 완료 2026-10-15, 이후 로비 Job·Plan 재구성이 대체 | 자동 검증(Play 조건 없음) | `ba0c9d7e` |
| Scope P5 | `StageMapViewScope`, 상점 이전, `StagePopupSession` 삭제, P5.8 같은 맵 객체 보존 | 완료 2026-10-15 | 사용자 Play 4항목 통과 | `ba0c9d7e` |
| Scope P6 | 문서 정리 18개, 공유 메모 3개 | 완료 2026-10-15 | 문서 검사 | `cb08fc77` |
| R6 후속 | `InGameViewFaultCleanup.TryStop` 복사본 제거 → `CleanupErrors` | 완료 2026-10-15 | 자동 검증 | `cb08fc77` |
| P2.5/P3.8 검증 후속 | 폐기 순서·실제 StopInput·호스트 교체 검증 강화, 좁은 예외 승인 | 완료 2026-10-15 | 자동 검증 | `cb08fc77` |
| P6 재대조 | 최신 코드(`70de0066`) 기준 문서 17개 수정 | 완료 2026-10-16 | 문서 검사 | `24f4f3aa` |
| 입력 Phase1 | 화면별 입력 Factory·`InputRegistrationSet`·`ViewGroup.Input`, 루트 슬롯 공개 제거 | 완료 2026-10-17 | 자동 집중 검증 | `4d6d739d` |
| 입력 Phase2(View_Refactory) | 옵션 Global 소유 + Live 전용 호환 보완 | 완료 2026-10-17 | 자동 집중 검증 | `55aa9a3a` |
| 입력 Phase2(StageSelcet) | 옵션 Global 소유(두 실행 모드 유지 브랜치) | 완료 2026-10-18 | 자동 집중 검증 | `8ef10977` |
| 입력 Phase3 | 통합 검증, 현재 가이드 갱신 | 완료 2026-10-18 | 자동 검증, 현행 2실패 남음 | `d76360e6` |
| 입력 병합 정합 | `d76360e6` + `9d3cc811` 병합, Live 전용 확정 | 완료 2026-10-18 | 자동 집중 검증, TextNotice 2실패 | `add48b35` |
| 책임 분리 Phase1 | V1 팝업 Factory 조립 시점 | 완료 2026-10-18 | 자동 집중 검증 | `2769ccdb` |
| 책임 분리 Phase2 | V2 진입 표시 계약, V3 맵 공통 모달 Layer | 완료 2026-10-18 | 자동 집중 검증 | `e244a929` |
| 책임 분리 Phase3~6 | V4 로비 씬 소유, V5 화면별 SceneUI, V6 포기 요청, 통합·문서 | 미착수 | — | — |
| Common 폴더 정리 | `RuntimeSystem/Common`·`LifeCycleSystem/Common`을 책임별 위치로 | 완료 2026-10-25 | 컴파일 확인(2026-10-26 사용자), 재생 미확인 | `6c56a50d` |

## 2 남은 일

- [ ] 책임 분리 Phase3(V4 로비 아티팩트 씬 소유): 사용자 지시 대기. 할 일은 설계 §8.4.
- [ ] 책임 분리 Phase4(V5 화면별 SceneUI): 사용자 지시 대기. Boot(`GameBootstrap`)는 용근님 작업 영역이고 카메라 연결 코드와 같은 파일이므로 착수 전 범위를 확인한다.
- [ ] 책임 분리 Phase5(V6 포기 요청): 용근님 C 단계 C-a(2026-10-23)로 대상 코드가 이미 정리됐다. 이 Phase를 취소할지 사용자 확인.
- [ ] 책임 분리 Phase6(영향 통합·문서): Phase3~5 뒤. 민형님 서비스 키 분리(`b308c88`) 인수 체크리스트(키 매핑·부모 Context 경유)를 읽기 전용으로 남긴다.
- [ ] 책임 분리 Phase1 선택 후속: 기본 상태와 다른 Recovery 상태를 주입해 `_recoveryState`만 덮어쓰는 회귀까지 구별하는 검사(리뷰 Minor 1, 제품 결함 아님).
- [ ] 입력 조립 때 남은 실패 넷의 지금 상태 확인 필요: `LobbyBootSmokeTests.Boot_AllStartModesInitializeCatalog_CombatFixture`(개발 fixture 카탈로그에 `Unit_CactusCannon` 없음), `LobbyBootSmokeTests.BootToLobby_ShowsSpaceWithoutStartingRun`(1920×1080 표시 겹침), `TextNoticeUITests.HideVersion_TracksExternalDisableAndRepeatedUnHover`·`Prefab_BindsBothTextsAndHoverTarget_WithoutBlockingPointer`(현강님 UI). 모두 이 작업이 만든 실패가 아니며 고치지 않았다.
- [ ] 수동 확인 미실시: 입력 조립 전체 화면 수동 인수(로비 → 덱·아티팩트 → 복귀, 맵 → 상점·회복 → 복귀, 착지 → 전투, 옵션 열린 채 종료, 실제 장치·FMOD), 책임 분리 Phase1·2 수동 Play, Common 정리 재생.
- [ ] 공유 메모 전달(§4).
- [ ] 설계 §11의 보류: D12 맵 정지 탈출, `PopupViewScope` 남는 한계, 루트 조립 잔여 사례, 옛 이름 주석.

## 3 작업별 결과

### 3.1 비전투 View Scope (P0~P6, 2026-10-14~16)

브랜치 `feature/Hojin/Stage_Shop_Event`. 코드 기준은 P0~P5가 `92440b83` + 작업 트리, P6가 `ba0c9d7e`, 재대조가 `70de0066`이다. Phase마다 멈추고 보고했다.

#### 계획에서 정한 세부 (동작 보존, 2026-10-13)

| ID | 내용 |
|---|---|
| C1~C3 | 계획 중 찾은 세부를 사용자가 추천안으로 정했다 → 설계 D18·D19·D20 |
| R1 | Scope 교체 규칙. `IsDisposed`(폐기 시작, 새 작업·늦은 콜백 차단)와 `IsSuperseded`(새 Scope 게시, Layer·UI 변경 차단)를 따로 둔다. System은 게시 직전에 이전 Scope의 `Supersede()`를 부르고, `Detach`는 `_scope`가 같은 객체일 때만 비운다. 더 새 Scope가 있으면 이전 Attach는 멈춘다 |
| R2 | `LobbyViewSystem.StopInput`은 Scope가 없어도 아티팩트 입력 Buffer를 비운다 |
| R3 | 로비 Tick 순서: 아티팩트 → 덱 → 출발 갱신 → 시설 상호작용 → 출발 갱신 |
| R4 | 스테이지 맵의 `SetPopupBlocked`는 아무것도 하지 않는다. 맵 입력은 팝업 Layer가 막는다 |
| R5 | 리플렉션으로 쓰는 필드 이름 유지: `LobbyViewSystem._cameras`, `StageMapViewSystem._requestWriter`, 아티팩트 Controller의 `_drag` |
| R6 | `InGameViewFaultCleanup.TryStop`도 `CleanupErrors`로 바꾼다. null 건너뛰기는 호출하는 쪽에 둔다 |
| R7 | 이름이 없어지는 클래스의 시험 파일 이름 변경: `ArtifactViewSystemTests` → `ArtifactPopupControllerTests`, `StagePopupSessionTests` → `PopupInputSessionSelectionTests` |

#### P0 기준선 (2026-10-14)

EditMode 전체와 상점 Explicit 시험을 돌려 기존 실패 목록을 고정했다. 재진입·실패 경로 시험 목록(P0.3)을 매 Phase 통과 조건으로 썼다. P0.5에서 노드 아이콘·가로 중앙 배치가 들어간 맵 코드와 설계 §5.2를 다시 대조해 P5 조건을 보강했다(아이콘 크기·간격·방향, 중앙 배치, 두 카메라 역할 구분, DepthCentering smoke의 `_display` 조회를 Scope 경로로). 코드 변경은 없다.

#### P1 공용 부품·덱 (2026-10-14~15)

- 한 것: `Common/CleanupErrors`, 내부 계약 `IPopupViewScreen`·`IPopupViewController`·`IPopupViewHost`, `PopupViewScope`, `LobbyDeckPopupViewScreen`. 덱 Controller의 상태 enum·토큰·열림 구독·Cleanup 복사본을 지우고 공용 Scope에 맡겼다. 로비 System에는 P2까지 쓸 임시 `DeckPopupHost`를 두었다.
- 계획과 다른 점(리뷰 보완): 인수 중 Begin/Show가 화면을 닫으면 숨김 전이를 보존한다. 소유 Open/Close 도중 다른 구독자가 Dispose해도 UI 서비스 호출이 돌아오기 전에는 UI 재호출·Layer 변경을 하지 않는다. End(restore)·차단 해제 콜백의 재열림은 다음 Tick 인수 대기로 보존한다.
- 기대값 변경: D10에 따라 덱의 외부 숨김 시험을 "다음 Tick에 복귀"로 바꿨다(`ExternalHideAll_DoesNotRestoreLobby` → `ExternalHideAll_RestoresLobbyOnNextTick`).
- 한계: 외부 UI 알림 전체 발송의 시작·끝을 모르는 경계와 전환 Tick 할당(설계 §4.2).

#### P2 LobbyViewScope (2026-10-15)

- 한 것: `LobbyDepartureText`·`LobbyDeparturePresenter`, `LobbyViewScope`(12 의존성 나열), R1의 두 상태. System은 서비스·Bind 진입점·루트 검증·게시/폐기·Tick·StopInput만 남기고 세대 번호·`Owns`·`TryCleanup`·임시 Host를 지웠다. 폐기 순서(D17) 시험과 재진입 시험을 더했다.
- 계획과 다른 점(리뷰 보완): Dispose 뒤 아티팩트 허용은 자기가 받은 토큰을 Unbind하는 동안만 준다. System의 `RequireRunning`을 시작 중 검사 지점에 넘겨 원래 예외를 유지한다. R2는 P3.4로 미뤘다.
- 확인: 사용자 Play 3항목 통과(2026-10-15) — 로비 시설 셋, 덱이 열린 채 씬 이탈, Lobby → StageSelect → Lobby 왕복.
- 이후: `LobbyDepartureText`는 2026-10-27 지웠다(설계 §5.1).

#### P3 아티팩트 흡수 (2026-10-15)

- 사전: 용근님 P3.0 메모를 확인하고 사용자가 "직접 전달 예정"이라 답했다.
- 한 것: `ArtifactPopupViewScreen`·`ArtifactPopupController`·sealed `ArtifactPopupViewFactory`(만든 객체를 추적하지 않음). `LobbyViewScope`가 생성·Tick·폐기를 소유한다. 로비 System이 Scope 없이도 아티팩트 입력·프리뷰 Buffer를 비운다(R2). `InGameViewFaultCleanup`·`InGameViewSystemFactory`의 아티팩트 줄을 정리했다(D15). 옛 두 타입과 `.meta`를 지웠다.
- 계획과 다른 점: 설계의 P3 사전 대조 보완안 넷(생성 인터페이스, Boot false 기대값, 보호 파일 주석 정리, UI fault false 기대값)을 사용자가 거절해 원래 계획 경로로 했다. Boot smoke의 `Session.StopInput` 검사는 Controller `Hide(false)` → 수동 UI Close → Dispose로 옮겼다(Hide를 영구 StopInput과 같다고 주장하지 않음). rebound 시험 하나는 콜백 안 즉시 게시와 같은 순서가 아니다 → 이후 P3.8 예외로 정리했다. 외부 Hide 직후 바로 Open하던 시험에 Tick 한 번을 넣었다(D10).
- 남은 것: 보호 파일 설명 두 곳(`ArtifactSceneRoot` cref, `ArtifactLifeCycle`)에 옛 이름이 지금도 남아 있다. [아티팩트 안내](../viewDocs/artifact-guide-Hojin.md)의 옛 경로 둘은 입력 Phase3에서 고쳤다.
- 확인: 사용자 Play 4항목 통과(2026-10-15) — 배치·회전·이동·되돌리기와 닫기 뒤 시설 입력, 열린 채 씬 이탈, 외부 숨김 뒤 다음 프레임 복귀, 오류 정지 뒤 입력 정리.

#### P4 로비 기록 누락 정책 (2026-10-15)

- 한 것: `LobbyLifeCycle`의 기록 누락 보고를 `Debug.LogError`로, `LobbyFlow`의 Tick·이탈 준비가 정지한 아티팩트를 건너뛰게 했다(제품 두 파일). 새 `LobbyDomainStopTests`.
- 확인: 자동 검증. 이 Phase에는 Play 조건이 없었다.
- 이후: 2026-10-23 로비 Job·Plan 재구성이 `LobbyFlow`·`ArtifactEditFlow`를 없애고 정책을 `LobbyArtifactDomain`·`LobbyPlan`으로 옮겼다(설계 §6). 같은 커밋(`1a386b64`)에서 옛 Flow 시험 넷(`LobbyDomainStopTests`·`LobbyArtifactFlowTests`·`LobbyArtifactDepartureTests`·`LobbyDepartureTests`)도 지웠고 그 규칙은 재생 확인 목록으로 옮겼다([로비 계획](2026-10-11-lobby-plan-Hojin.md)).

#### P5 스테이지 맵과 상점 (2026-10-15)

- 한 것: `StageMapViewScope`(Display·Reader·Picker·CameraDrag·Interaction·EntryObserver·상점), `ShopPopupViewScreen`·`ShopPopupViewFactory`(옛 PostBindService 인자 13개 조립을 옮김), 상점 Controller의 수명 부분을 `PopupViewScope`로. `StagePopupSession`을 지우고 시험을 `PopupInputSessionSelectionTests`로 옮겼다(GUID 유지).
- 착수 전 사용자 승인(2026-10-15): ① StopInput은 Scope와 맵 표시까지 정리한다. AMB 즉시 정지는 더하지 않고 정상 Detach의 정지를 유지한다. ② Core가 회복 Event를 받게 된 뒤 맞지 않던 옛 Event 거절 smoke 하나를 "첫 회복 수락 → 같은 미완료 노드의 실제 거절"로 갱신한다. ③ 임시 Profiler 시험을 넣어 측정한 뒤 지운다.
- P5.8 같은 맵 객체 보존: 상점 완료 뒤 맵 다시 그리기가 노드·외형·아이콘·연결선을 모두 다시 만들던 것을 측정한 뒤, 사용자가 "같은 맵이면 기존 객체 보존"을 골랐다(설계 D21~D23). 제품은 Display·NodeView·Scope 세 파일만 고쳤다. Pool·새 서비스는 만들지 않았다. 보존 뒤 같은 맵 다시 그리기에서 객체를 새로 만들지 않음을 실제 객체 수명으로 확인했다.
- 계획과 다른 점: 외부 Open/Close 직후 Layer를 보던 카메라 시험 하나를 다음 Tick 검사로 옮겼다. P5.8e(추가 사용자 승인): 실제 smoke 두 파일(`ShopStagePlaySmokeTests`·`StageMapDepthCenteringBootSmokeTests`)의 옛 재생성 단언을 보존 기대값으로 옮겼다.
- 확인: 사용자 Play 4항목 통과(2026-10-15) — 상점 진입·구매(중복 제출 잠금·3초 문구)·나가기·완료 표시·다음 전투, 마우스를 누른 채 상점 열기, 상점이 열린 채 씬 이탈, 외부 숨김 뒤 다음 프레임 맵 입력 복귀.

#### P6 문서와 공유 (2026-10-15)

기존 문서 15개(SA 설계 D5, 로비 설계·계획, 회복 계획 §10·§11, 아티팩트 팝업 계획·설계, 오류 정리 조정, 시퀀스 UML, 현강님 아티팩트 UI 접점, 런타임 씬 연결 안내, CLAUDE.md 등)를 고치고 공유 메모 셋을 새로 썼다(§4). 코드 대조 중 `InGameViewFaultCleanup`에 `TryStop` 복사본이 남은 것(R6 누락)을 찾았다. P6는 문서 범위라 코드는 고치지 않았다.

#### R6 후속 (2026-10-15)

사용자 별도 승인으로 `InGameViewFaultCleanup`의 `TryStop`을 지우고 `CleanupErrors.Try`·`ThrowIfAny`에 위임했다. 정리 순서·null 건너뛰기·재진입 guard·원래 예외와 메시지는 그대로다. 운영 파일 1, 시험 파일 1(null·실패 사례 넷 추가).

#### P2.5/P3.8 검증 후속 (2026-10-15)

최종 감사에서 찾은 검증 공백 셋을 사용자 승인으로 보완했다. 제품 동작은 그대로다.

- P2.5: 폐기 순서 시험을 8단계 실제 상태 관찰로 강화했다.
- P3.8 실제 정지: 실제 Boot·Core·DB smoke에서 소유 `LobbyViewSystem.StopInput`을 실행해 화면 닫힘·Buffer 비움·번호 보존·모델·슬롯·카메라 반환·재Attach 거절·늦은 입력 거절을 검사했다.
- P3.8 호스트 교체: 옛 "즉시 게시·무Layer" 조합을 재현한 임시 시험이 현재 계약에서 실패함을 확인한 뒤, 사용자가 "현재 수명 계약 유지 + 명시적 예외 + 두 경계 검증 강화"를 골랐다(설계 §3.3).

#### P6 최신 코드 재대조 (2026-10-16)

HEAD `70de0066`에서 P6 문서와 공유 메모를 최신 코드와 다시 맞췄다. 18개를 검토해 17개를 고쳤다. 회복 정리 순서(전투 → 맵/상점·회복 팝업 → 로비/아티팩트 → 회복 요청 수명), 실제 `UIId.Recovery = 19`, 일반 패배 로비 복귀 정책을 구분했고 새 공유 메모는 만들지 않았다. Unity·시험·빌드는 다시 돌리지 않았다.

### 3.2 화면별 입력 조립 (Phase1~3, 2026-10-17~18)

입력 동작·Framework 공용 계약·입력 에셋은 바꾸지 않았다. Phase마다 멈추고 보고했다.

- **Phase1(2026-10-17, `4d6d739d`):** 화면별 Factory 넷·키 정의 둘(`StageInputKeyBindings`·`ArtifactInputKeyBindings`)·`InputRegistrationSet`·`InGameViewInputContext`·조립 결과 둘(`InGameViewSystemComposition`·`InGameSystemComposition`)을 더하고 루트의 슬롯 공개를 없앴다. Combat·StageMap·Lobby 소비자와 루트 조회 smoke를 새 경로로 옮기고 서비스 대역 일곱에 `Input`을 더했다. 새 `ViewInputCompositionTests`. 이 Phase까지 옵션 Router는 루트 소유였다(중간 상태).
- **Phase2 View_Refactory(2026-10-17, `55aa9a3a`):** 시작 HEAD `187ca19b`가 용근님 `c9b8c4d6`의 `CombatExecutionMode` 삭제와 Phase1의 옛 실행 모드 인수가 섞여 컴파일 실패였다. 사용자에게 보고하고 멈춘 뒤 **추가 승인**으로 Factory 세 곳과 `ViewInputCompositionTests`의 인수만 Live 전용으로 정리했다. 옵션 Router를 Global 소유·마지막 모듈로, `OptionCloseRequests`를 더하고 OptionUI 조회를 바꿨다. 새 `GlobalInputCompositionTests`, `UnityStageInputTests`에 마지막 Canceled 검사 하나.
- **Phase2 StageSelcet(2026-10-18, `8ef10977`):** 같은 옵션 이전을 두 실행 모드가 있는 브랜치(시작 `e244a929`)에 했다. 제품 넷(`GlobalSystemFactory`·`GlobalSystemGroup`·`OptionUI`·`RunTimeSystemFactory`). Live 전용 변경은 섞지 않았다.
- **Phase3(2026-10-18, `d76360e6`):** 제품·통합 smoke에 남은 루트 슬롯·옵션 직접 조회는 없었다. 회복 smoke 다섯이 "수락 결과 뒤 2프레임이면 명령이 비었다"는 가정으로 실패해, `StageMapRecoveryEntryBootSmokeTests`의 고정 프레임 대기만 "명령 소비까지 최대 30초 대기"로 바꾸고 fault 확인을 더했다(현재 제품은 착지 완료 다음 Tick에 명령을 소비). 현재 가이드를 고쳤다(아래 마지막 항목). 현행 제품 그대로 남은 실패 둘은 §2.
- **병합 정합(2026-10-18, `add48b35`):** `d76360e6`과 `9d3cc811`을 병합하며 충돌 넷을 풀었다. Runtime·InGame·View Factory는 Live 전용이고 삭제된 실행 모드를 복원하지 않는다. `GlobalUIPresentation`·`IUITextPresentation`의 서비스 전용 등록은 보존했다. 집중 검사에서 `TextNoticeUITests` 둘이 실패했다(병합 대상 원본과 같은 시험·프리팹, 고치지 않음).
- **다른 작업자 문서를 고친 기록(Phase3):** `viewDocs/stage-input-guide.md`(루트 슬롯 예제 → View context·화면 Factory), `Systems/Documentation/framework-architecture.md`·`framework-programmer-guide.md`(민형님, 생성·등록·InitData 구분과 Global Export·종료 순서), `GlobalSystem/UISystem/Screens/Global/OptionUI.md`(현강님, Global 소유·조회·종료). FW 계약 ID·의미는 바꾸지 않았다. 호진 문서는 스테이지 선택 입력 안내·아티팩트 안내를 고쳤다.

### 3.3 책임 분리 (Phase1·2, 2026-10-18)

브랜치 `feature/Hojin/StageSelcet`, 설계 기준 `a8d4c073`. 두 실행 모드를 유지했다. 민형님 `b308c88`(서비스 키·카메라)과 입력 Phase2 `55aa9a3a`는 병합하지 않고 경계만 적었다(설계 §11).

- **Phase1(`2769ccdb`):** V1. `StageMapViewSystem`·`LobbyViewSystem`의 PostBindService는 생성 함수만 준비하고 Initialize가 기본 Factory를 조립한다. 새 `ViewPopupCompositionTests`(데이터 우선·서비스 우선, 명시 주입 전·후, 한쪽만 연결한 실패). 수정 전 서비스 우선 순서의 실패 셋(회복 외형 fallback, 아티팩트 DB 누락 예외, 명시 Factory 덮어쓰기)을 재현하고 고쳤다. 리뷰 Minor 1은 §2.
- **Phase2(`e244a929`, 시작 `2769ccdb`):** V2 `IStageMapEntryPresentation`(Observer 인자 타입만 좁힘, `StageMapNodeMovement`가 명시 구현). V3 `ShopPopupInputLayers` → `StageMap/StageMapPopupInputLayers`(GUID·`.meta` 유지, 이름 `"ShopPopup"`·허용 맵 유지). 제품 소비자 셋과 시험 소비자 여덟의 참조만 옮겼다. `StageMapEntryObserverTests`는 프리팹·Delay 조립을 제어 가능한 표시 역할로 바꾸고 두 사례를 더했다. 역할·선언만 옮기는 리팩터라 일부러 RED를 만들지 않았다.
- **이후:** V3 자리는 2026-10-27 `StageSelectionInputLayers.Popup`("StageMapPopup")으로 다시 옮겼다(검사 정리 Q-C2, [검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md)).
- 계획했던 공유 메모(`2026-10-18-view-responsibility-separation-framework-share.md`, "필요 시")는 만들지 않았다.

### 3.4 Common 폴더 정리 (2026-10-25, `6c56a50d`)

`RuntimeSystem/Common`·`LifeCycleSystem/Common`을 실제 책임 위치로 옮겼다. 파일·네임스페이스 이전만 했고 등록·호출 계약은 그대로다.

| 옛 위치 | 새 위치 | 이유 |
|---|---|---|
| `RuntimeSystem/Common/RuntimeFaultBoundary.cs` | `RuntimeSystem/ErrorHandling/` | 여러 런타임 기능이 공유하는 오류·정지 경계 |
| `RuntimeSystem/Common/CleanupErrors.cs` | `RuntimeSystem/ErrorHandling/` | 해제 중 오류 수집 |
| `RuntimeSystem/Common/ChessPieceMotionRules.cs` | `DataBaseSystem/Data/View/Movement/` | DB와 연출이 쓰는 이동 설정 값 조건. 런타임 상태에 의존하지 않음 |
| `LifeCycleSystem/Common`의 세 파일 | `LifeCycleSystem/ArtifactPreparation/` | 부트의 아티팩트 보드 최초 준비. 로비 전용이 아니고 로비 진입보다 앞선 수명 |

- 원칙: Common을 다른 Common으로 옮기지 않는다. 오류 처리를 로비에 넣으면 전투·스테이지·UI가 로비에 의존하게 되므로 넣지 않는다. `ViewSystem/Common`은 대상이 아니었다.
- 결과: C# 136개(운영 69, 시험 67)의 네임스페이스·using·완전 이름·XML 참조만 바뀌었다. 파일·폴더 `.meta` GUID는 보존했다. DB의 `LobbyLayoutData`가 `RuntimeSystem/Common`을 가져오던 의존이 없어졌다. MSBuild·Unity 시험은 사용자 규칙대로 돌리지 않았다.
- 확인: 그 뒤 다른 작업에서 `CleanupErrors` using 누락 하나를 고친 다음 사용자가 컴파일 오류 없음을 확인했다(2026-10-26, [스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md)). 재생 확인은 따로 없다.
- 같은 날 로비 SO 분리는 역할별 SO 넷으로 정해졌다([로비 설계](../specs/2026-10-11-lobby-design-Hojin.md)).

## 4 공유 메모

| 메모 | 받는 사람 | 내용 | 전달 |
|---|---|---|---|
| [비전투 Scope UI 공유](../handoffs/2026-10-15-noncombat-view-scope-ui-share-hyeongang-Hojin.md) | 현강님 | `OpenStateChanged` 구독 한 곳, 화면 어댑터, `XxxContent` 이름 구분, 외부 숨김·열림 다음 Tick | 미전달 |
| [비전투 Scope 전투 공유](../handoffs/2026-10-15-noncombat-view-scope-combat-share-yonggeun-Hojin.md) | 용근님 | P3 사전 안내(공용 Factory·오류 정리 줄)와 최종 메모(기록 누락 정책 차이) | 미전달. 사전 안내는 사용자 "직접 전달 예정"(2026-10-15), 실제 전달 미확인 |
| [비전투 Scope Framework 공유](../handoffs/2026-10-15-noncombat-view-scope-framework-share-minhyeong-Hojin.md) | 민형님 | Framework 계약 무변경, View 모듈 하나 감소, DR-009 해석, R6 후속 | 미전달 |
| [입력 조립 Framework 공유](../handoffs/2026-10-17-view-input-composition-framework-share-Hojin.md) | 민형님(현강님 OptionUI 변경 포함) | 화면 입력 조립·소유, 옵션 Global 소유, 종료 순서 | 미전달 |
| [맵 팝업 Layer 이름·자리 변경](../handoffs/2026-10-27-stage-map-popup-layer-share-yonggeun-Hojin.md) | 용근님 | V3 Layer 이름 `"StageMapPopup"`·자리 이동(검사 정리 작업 소속) | 미전달 |
| [공용 오류 정리 조정](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md) | 용근님 | 공용 정리 순서·연결 기록 | 미전달·미합의 |
