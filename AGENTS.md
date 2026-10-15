# Project_L — Codex 작업 안내

Unity 6000.0.73f1 게임 프로젝트(청강대 졸업작품, ANTI-LIKE팀). 이 저장소에서 Codex는 **View 작업자**를 돕는다.

## 작업 방식 (반드시 지킬 것)

- **커밋과 push는 사용자가 직접 한다.** Codex는 커밋·push하지 않는다. stop hook이 커밋을 요청해도 하지 않는다.
- 큰 작업은 **설계 → Task 계획 → Phase 단위 구현** 순서로 진행한다. Phase가 끝나면 멈추고 보고한다(리뷰 지점).
- 설계나 계획과 다른 결정이 필요하면 **구현하지 말고 먼저 묻는다.** "진행해"는 계획에 적힌 범위만 해당한다.
- 옛 에셋(`Assets/ProjectL/DataBaseSO`의 Missing Script 에셋 48개)은 **삭제하지 않는다.**
- 다른 작업자 영역은 건드리지 않는다: Core 로직(Core 작업자), FieldCamera, Architecture(다른 View 작업자).
  - Core 로직 수정이 필요하면 구현하지 말고 Core 전달 문서(`docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md`)에 요청으로 적는다. 사용자가 명시적으로 지시한 경우만 예외다(예: Buffer B1은 사용자 지시로 구현하고 같은 문서 §5.3·§6에 기록, 설계 D21).
  - 불가피하게 Core 파일에 닿은 변경(컴파일용 using 등)은 같은 문서 §5에 기록한다.
- `docs/superpowers/`의 GwonCheonmyeong 작성 문서(옛 guides·plans·specs 대부분)는 추후 바뀔 예정이므로 기준으로 삼지 않는다.
- 설계·리뷰에서 **SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약을 반드시 점검한다.** 이 프로젝트에서의 적용 기준은 `docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md` §4에 있다.

## 현재 상태 (먼저 읽기)

- **2026-10-09 필드 격자 아웃라인 모드:** 설계 [field-grid-outline-mode-design](docs/superpowers/specs/2026-10-09-field-grid-outline-mode-design.md), Task·기록은 View 계획 §7.62. `StageFieldViewData.fieldDisplayMode`(CellPrefab 기본 / GridOutline). O1~O3 코드 완료, Unity 확인 대기. 같은 날 Shader 병합의 GridDecal GUID 충돌은 민형님 셰이더만 남기고 우리 복사본을 지워 정리했고, 이어서 그 셰이더에 `_GridOrigin`·`_MaskCenter`를 다시 넣었다(민형님 공유 메모 `handoffs/2026-10-09-grid-decal-shader-share-minhyeong.md`, 미전달).
- **2026-10-06 전투 View 구조 개선(VR):** 평가 [structure-review](docs/superpowers/specs/2026-10-06-combat-view-structure-review.md), 설계 [refactor-design](docs/superpowers/specs/2026-10-06-combat-view-refactor-design.md), Task·상태는 View 계획 §7.40~7.41. VR1(대기 `CombatCommandGate`, 이동 허용 `CanEditPath`) Unity 확인 완료. VR2·VR3(Router·배치/공격 분리) Unity 확인 완료(§7.45). VR0 메모 미전달. VR4(`CombatPointerMapper`) Unity 확인 완료(§7.46). VR5(TraitVisual 필수·타임라인 상수·형식 정리) Unity 확인 완료(§7.47). 기록 모드 전투 포인터 → 공격 소유자 라우팅 Unity 확인 완료(§7.48). 남은 View 작업 목록은 §7.49. **손패 카드화** 설계 [hand-card-design](docs/superpowers/specs/2026-10-06-hand-card-design.md), Task는 §7.50, H1·H2 확인 완료(§7.51~7.52), H3 씬 연결·3D 손패 파일 삭제·현강님 메모(미전달) 완료, Unity 확인 대기(§7.53), 천명님 형식 메모 미전달. 다음 VR6(타임라인 소비자 교체)은 Core 타임라인 Buffer 공개 뒤에만 시작한다. **남은 골격·View 작업 목록은 View 계획 §7.56**(§7.49를 대체). **4-V 포기 버튼·확인창 코드 완료(§7.58), Combat.unity에 `CombatAbandonPanel` 연결 전에는 Combat 진입이 멈춘다.**
- **2026-10-05 View 후속 세션 분리:** 사용자는 View 후속 작업을 다른 세션에서 진행한다. 시작 안내는 [View 전용 다음 세션 인계](docs/superpowers/handoffs/2026-10-05-combat-view-next-session-handoff.md). 문서 정리 커밋은 `c3bc5ee1`. 구조 평가에서 나온 X1·X3·X4 보완은 계획 후보이며 아직 구현하지 않았다. 기존 완료 검증은 유지하고 골격·Core 소유 파일과 동시 편집을 피한다.
- **전투 View·골격:** 코드 기준 `bfe63a27`. 일반 승리의 StageSelect 복귀까지 연결됐고 **골격 Phase 7·View Phase F 문서 인계 완료**. 최신 시작점은 [2026-10-05 공동 인계](docs/superpowers/handoffs/2026-10-05-combat-view-skeleton-session-handoff.md), [통합 UML HTML](docs/superpowers/specs/2026-10-05-combat-current-uml.html). 실행 기록 원본은 골격 계획 §6.15·View 계획 §7.24, 정책 원본은 골격 설계 §4.1·§6.3·§6.4와 View 설계 11차다. 포기 공개 API·T1/G7·두 재화 분리·새 Run 시작은 2026-10-06 Core 병합으로 제공됐다(전투 HUD는 `CombatStateData.StageGold`, View 계획 §7.54~7.56). 포기 F2 연결·StageSelect 노드 진입 인수가 남았다, 패배·최종 승리 목적지는 보류. 아래 날짜별 메모는 과거 이력이며 현재 상태로 되돌리지 않는다.
  - **2026-10-03 세션 인계:** 사용자 요청으로 골격 구현은 다른 세션에서 진행한다. `81c8c38`은 `491cae4d`에 병합됐다. 해당 세션은 `docs/superpowers/plans/2026-09-30-combat-flow-skeleton-plan.md`의 **6A.0**과 Core 인수 가이드 **§2.8**부터 읽는다. 대상/결과 전달·활성화 수명을 먼저 확정하고 View B3의 Bootstrap·기록 모드 대응과 함께 연결한다. 문서 갱신은 구현 완료가 아니다. 같은 파일의 동시 편집을 피한다.
  - **Phase 0 완료(2026-10-02, 계획 §7.7).** 공유 문서: `docs/superpowers/handoffs/2026-10-02-combat-view-framework-share.md`(민형님), `…/2026-10-02-combat-view-core-notes.md`(천명님) — 둘 다 미전달. **Phase A 완료**(계획 §7.8, 정정 뒤 테스트 통과): B1 재생 중 Artifact 외형 테이블(아이콘 없는 3개 등록)이 DB 생성을 막아 테이블 등록만 비움(호진님 메모 `handoffs/2026-10-02-artifact-view-table-note.md`, 미전달). 새 테이블 3개, 전투 스테이지 11개 `economy`, `Unit_MainCharacter`·외형, `PlayerLoadout_1`(메인 + Akimbo·ShotGun·Sniper 각 1 — Core 규칙상 시작 서브는 정확히 3명, 규칙 문의는 Core 메모 §3a).
  - **Phase B1 완료(2026-10-02, 계획 §7.9):** `SceneIndices`, `RuntimeSystem/Common/RuntimeFaultBoundary`, `CombatExecutionMode`, `ViewSystem/Boot/GameBootstrap`, Factory 4개 인자, LifeCycle 씬 번호, Boot·Combat 씬·Build Settings. 테스트 통과, 재생: Combat 진입 → G7 준비 실패 1회 보고·정지.
  - **Phase B2 코드 완료(2026-10-02, 계획 §7.10, 검증 대기):** Flow 실행 모드·공유 오류 검사(골격 §4.1 0행·4a행), `ViewSystem/Dev/DevBattleInitializationSource`(개발 정의 전용 fixture), GameBootstrap 개발 설정(`useDevBattle`·로드아웃 1·챕터 1·노드 2, 당시 Live만 `EnterChapter`→`ConfirmStageEntry`; 현재 호출자는 B3로 이전), CombatFlowTests 5개 추가. MSBuild 개발/비개발 정의 모두 오류 0·새 경고 0. 남은 것: 사용자 EditMode·B2.5 재생.
  - **Phase E2(계획 §7.14, 사용자 기능 확인·잔여 검증은 §7.18):** 새 Core는 개체 조준을 받지 않는다(Single=칸, Direction=방향, All=전체). View는 지정 방식을 InGame DB에서 읽지 않고 **공격 범위 응답의 `AttackAims`**로 판단한다. 범위 응답 전 호버 요청·기록 0, RecordInputOnly는 응답 없이 칸 기준. FieldView 강조 층(호버 > 영향 > 범위 > 행위자 > 정적). fixture ShotGun = Fan/Direction.
  - **Phase A2 코드 반영(설계 §21.2, 계획 §7.19):** 지정 방식 DB·UnitDataSO 참조·유닛 5개 연결. 검증 상태는 계획 §7.19, 새 API·생성자 변경·Core 조립(T1/G7) 요청은 DB 경계 인계 §5.6을 따른다. A2와 골격 6B의 F2는 서로의 전체 완료를 기다리지 않는다.
  - **Phase E(계획 §7.13, 사용자 기능 확인·잔여 검증은 §7.18):** `CombatInteraction`(순수 C#, Drag·조준·단일 대기·미리보기 규칙), Policy `CanSubmit`·`CanPreview`·`CanChangeDevLayer`, 집기·표시 의도 계약(`ICombatPointerPicker`·`ICombatInteractionFeedback`), `PlacementGhost`, 개발 Layer 패널 연결. Combat.unity의 Ghost·DevLayerPanel·버튼 2개 참조는 연결돼 있다. E.8 예외 주입·세부 회귀 증거는 기능 재생 확인과 구분한다.
  - **Phase D 완료(2026-10-02, 계획 §7.12):** `ViewSystem/Combat/Display/`에 옛 develop-4week 필드·유닛·손패·HUD 이식(`FieldView`·`FieldUnitPresenter`·`HandPresenter`·`CombatHud`, `EntityViewSet` ID별 재사용). ViewSystem은 `ICombatBattleDisplay`(구현 `CombatSceneDisplay`)에 값만 넘긴다(설계와 다른 결정, 확인 대기). Combat.unity 씬 편집 절차는 §7.12.
  - **Phase C 완료(2026-10-02, 계획 §7.11):** `ViewSystem/Combat/`에 `CombatViewSystem`(모듈만 등록, Combat Enter 100·Exit -100, 결과 소비·종료 입력 해제·StopInput/Detach), `CombatResultPlayback`, `CombatViewRefresh`, `CombatInputPolicy`, `ICombatPresentation`·즉시 연출, `CombatSceneRoot`. View Factory가 StopInput을 경계에 연결, 경계에 `DisconnectStopInput` 추가. **`Combat.unity`에 CombatSceneRoot 하나가 필요**(없으면 진입 때 오류 정지). 계획과 다르게 한 4건(서비스 미등록, 캐릭터·재화·View DB는 D로, SceneRoot 참조는 D·E로, 경계 해제 메서드)은 §7.11에서 사용자 확인 대기.
  - **씬 이름 주의:** 옛 `Stage.unity`는 옛 **전투** 씬이고 `StageSelect.unity`가 **스테이지 맵**이다. 새 Build Settings: Boot 0(새) · StageSelect 1 · Combat 2(새 `Combat.unity`). 옛 `Stage.unity`는 목록에서만 빼고 삭제하지 않는다. `Title.unity`는 향후 Lobby(3) 후보.
  - **Render DB 임시 연결(DR-003):** `OutlineSystem`이 `_Outline` 머티리얼을 요구해 Material 테이블 연결을 풀었다(민형님 Shader 브랜치와 같은 두 줄 + `MaterialDataSO.cs` 이름 변경). `DataBaseSO/RenderGroup/TempMaterialTable`은 **임시**이며 Shader 브랜치 병합 때 지운다(Framework 공유 문서 §6.1·Q6).
  - **Play From Boot:** 메뉴 `ProjectL > Play From Boot`(이 PC만, 기본 꺼짐)를 켜면 어느 씬에서 Play해도 Boot부터 시작한다(`Tool/Editor/PlayFromBootToggle.cs`). `ProjectL > Play From Boot 설정...` 창에서 GameBootstrap 값(시작 구간·실행 모드·개발 설정)을 이 PC에서만 덮어쓴다(EditorPrefs, `ViewSystem/Boot/GameBootstrapEditorOverrides.cs`, 에디터 전용). 켜져 있으면 재생 시작 때 콘솔에 덮어쓰기 로그가 남는다.
  - **다른 View 작업자 공유:** `docs/superpowers/handoffs/2026-10-02-view-workers-runtime-share.md`(Boot 시작·씬 번호·BindScene 1회·AudioSystem 미등록·Factory 서명·DB 변경, 미전달).
  - **씬에 붙는 시스템 만드는 법(예시 코드 포함):** `docs/superpowers/viewDocs/runtime-scene-binding-guide.md`. GameBootstrap은 직접 쓰지 않고, 시스템이 씬 번호에 Enter/Exit을 등록해 SceneRoot를 한 번 찾아 연결한다. Bootstrap 역할 B-1~B-8은 View 설계 §10.0(Framework가 지정한 형태가 아니라 View 쪽 결정).
  - 새 `.cs`가 Unity csproj에 아직 없을 때 컴파일 확인: VS 2022 MSBuild + `CustomAfterMicrosoftCommonTargets`로 새 파일만 Compile에 더한다(산출물은 스크래치패드).
  - Core 인수 기준은 `e0b02cc7`까지다. 기존 `069327c3` 미병합·`81c8c38` 진입 이전 안내는 과거 기준이다. `EnterBattle` 호출자 이전은 반영됐으며 DB 기본 source 성공과 구분한다. 현재 이동 연동 계약은 `docs/superpowers/guides/unit-movement-attack-core-handoff.md` §8이다.
  - W12·W23: 목표는 두 모드 모두 로스터·EnterChapter 뒤 chapter/node를 골격 진입 경계로 전달하는 것이다. Combat 선택 선확정과 수락 후 중복 Prepare를 제거한다. 병합 직후 기록 모드 준비 실패는 View 계획 §7.15 이력이며 B3 코드 수정·검증은 §7.17에 기록돼 있다. 개발 fixture는 2026-10-06부터 요청 스테이지의 DB 경제(시작 골드·드로우 비용)를 쓴다(View 계획 §7.54). View가 Core Buffer에 직접 쓰지 않는다.
  - W13~W15: 현재 Core 국면으로 배치/전투 Layer 동기화(밤 종료 복귀 포함), 무상태 입력 Policy와 시작/턴 종료의 단일 대기, 정상 종료 시 새 제출만 차단하고 마지막 결과 소비는 유지. 책임·외부 경계·SOLID·성능 기준은 설계 §13, 회귀 검증은 계획 E.6·E.9.
  - W16~W19: 재생 상태는 `CombatResultPlayback`만 소유, 공유 오류 객체는 `RuntimeSystem/Common`에 배치. 연출의 완료/실패·Cancel·자원 해제 계약, 정적 필드/동적 표시 분리·View 재사용을 적용한다. 구현 계획 §8에 확장 시 조건과 필요한 검증을 기록했다.
  - 8차에서 미뤘던 자원 해제 계약 검증(X3) 중 이동 연출은 이미 도입됐으므로 6B F1/F2 인수에서 대조한다. 비이동 VFX와 기기·규모별 측정(X4)은 후속 확장이다. W20: RecordInputOnly의 개발 Layer 선택은 Combat Canvas 버튼(계획 E.4a), 검증 context 없는 경로 Move/Return 기록은 제공하지 않는다.
- **전투 골격:** Phase 0~5·6A/F1 완료 유지, 일반 승리 F2 코드와 문서 Phase 7 인계 완료. 전체 6B는 포기·보류 목적지 연결이 남았다. 검증 출처는 골격 §6.14~15와 공동 인계 §2를 따른다. `5078ee0e`의 사용자 검증 확인과 `bfe63a27`의 관리 회귀/외부 빌드 증거를 구분한다.
  - **2026-10-06 K21(코드 반영, Unity 검증 대기):** 패배·최종 승리 → **새 Run 준비 후 StageSelect**(최종 목적지 Title은 화면 생기면 교체). EndBattle 뒤 `CombatExitSequence`가 Core `CompleteChapter`/`Outcome`으로 Run 종료를 판정하고 `Register(Combat, NewRun)`(Core `RunStart.StartNewRun(1)`, Stage와 같은 `NewRunJob`) 완료 뒤 전환. 연결 정의 없음(`DefinitionUnavailable`)은 임시로 Run 종료. 기록은 골격 계획 §6.16·설계 §6.4. 2026-10-07: S5(옛 로스터 Job 삭제)·S6(NextEntry fixture 수정) 완료, 병합으로 빠졌던 K21 테스트 복원. 남은 순서는 View 계획 §7.56 "진행 순서".
  - **2026-10-07 K22 자발적 포기(완료, Unity 테스트 통과 2026-10-07):** `LifeCycles.CombatExit.RequestAbandon()`(Live 전용, 목적지 인자 없음). 남은 처리·표시 → Core `AbandonBattle()` 1회 → 완료 확인 → EndBattle → 새 Run → StageSelect. 대기 중 실제 승패가 먼저 나오면 그 승패로 이탈. 기록은 골격 설계 §6.5·계획 §6.17. 포기 버튼은 View 세션(§7.56 4-V).
  - **골격 마무리(2026-10-07):** 인계는 [골격 최종 인계](docs/superpowers/handoffs/2026-10-07-combat-skeleton-final-handoff.md). 바로 할 수 있는 골격 작업은 끝났다. S2(StageSelect 노드 진입)는 다른 작업자 담당. 남은 골격은 외부 조건 대기: S3 챕터 계속(DB에 챕터 연결 값이 없음 → DB 값 + Core Source 필요), Title이 생기면 `RunEndDestination` 교체, 운영 로드아웃 선택. 순서 원본은 View 계획 §7.56.
  - **개발 진입 옵션 분리(S7, 2026-10-07):** `GameBootstrap.useDevBattle` → `useDevProgress`(씬 값 유지) + `devBattleDefinition`(Fixture/Database). T1/G7 해소로 Combat 직접 시작에서도 Database = 운영 DB 정의를 쓸 수 있다. 설정 창 "Battle Definition"으로 바꾼다. Database에서는 노드 그래프도 DB 맵 설정이라 Dev Node Id가 전투 노드여야 한다.
  - **2026-10-05 문서 인계 갱신:** 현재 상태는 골격 §6.15·View §7.24를 우선한다. §6.12~14·§7.21~23은 단계별 이력이다. Core 요청 원본은 [2026-10-05 요청 문서](docs/superpowers/handoffs/2026-10-05-combat-core-requests-phase6b.md), 공용 변경은 [공동 Framework 공유](docs/superpowers/handoffs/2026-10-05-combat-view-skeleton-framework-share.md). 준비된 문서를 외부 발송한 것은 아니다.
  - 네 Job 모두 Core에 연결됐다. 세 처리 Job은 `CoreGroup.Combat.ProcessNextCommand()`·`ProcessLatestPreview()`·`AdvanceCombat()`를 호출하고 정상 false도 완료한다.
  - 과거 `ede27fef`의 통합 fixture 15건·Economy 5건 실패는 최신 미해결 목록이 아니다. 병합에서 fixture가 수정됐고 전체 통과 기록을 인수했다(골격 §6.11·View §7.20). 이번 계획 정리에서 Unity를 재실행한 것은 아니다.
  - Flow·Plan·Job은 View(K2), 계산·기록은 Core. 입력 유지(K14), 정상 false도 Job 완료, Flow에서 기록 확인 정책을 유지한다.
  - A2의 DB 지정 방식·유닛 참조/query는 제공됐지만 기본 `DatabaseBattleInitializationSource`는 Core 조립 미연결로 여전히 T1/G7 예외를 던진다. **2026-10-06 해소:** Core 병합으로 DB 소스가 전투 정의를 돌려준다. 운영 경로 확인은 StageSelect 노드 진입 인수 뒤(View 계획 §7.56). fixture Live와 운영 DB 성공을 구분한다. 필요한 Core 작업은 골격 계획 §0.2, 기존 요청 원본은 DB 경계 인계 §5.6이다.
  - **권장 순서:** 골격 계획 §0.1만 따른다. Core 요청은 가능한 작업과 병행하며 F1을 Core 응답 대기로 막지 않는다.
  - **F1은 하나:** View B1·C·E의 GameBootstrap·RuntimeFaultBoundary를 골격 Phase 6이 인수했다. **F2 정상 종료:** 계약은 골격 설계 §6.4. 일반 승리의 LifeCycle 호출자 → CombatExit.RequestNormalExit(Stage) → 결과/Completion 확인 → EndBattle → StageSelect. 패배·챕터 완료의 자동 이탈은 보류하고 포기는 Core 입력 조립 API 인수 뒤 연결한다. 직접 Manager.Change/Exit는 활성 Combat을 우회할 수 없다.
  - 골격 설계는 8차(K20 진입 계약). Flow 분기·모드·공유 오류는 §4.1, 진입/활성화는 §6.3이 원본이다. 기존 오류 경계를 다시 만들지 않는다. 이전 문서의 View B는 B1/B2이며 신규 호출자 이전은 B3다.
- 그 전 작업: 스테이지 내부 입력(Framework InputSystem의 Unity 연결 `UnityBridge` + 배치·전투 Controller + 수신 슬롯) 구현(`feature/Yonggeun/Stage`, 2026-09-30). 인계는 `docs/superpowers/handoffs/2026-09-30-stage-input-local-session-handoff.md`, 입력 사용법은 `docs/superpowers/viewDocs/stage-input-guide.md`에 있다.
- 그보다 전: CombatViewBuffer(View가 쓰고 Core 측이 읽는 전투 입력 Buffer) 구현 완료(2026-09-29). 인계는 `docs/superpowers/handoffs/2026-09-29-combat-view-buffer-local-session-handoff.md`.
- DB-SO 이전 1차 완료(`feature/Yonggeun/DB_SO`, `f32dea2`). 이어서 할 일과 주의 사항은 **`docs/superpowers/handoffs/2026-09-25-db-so-local-session-handoff.md`**에 있다.
- develop(`8a5e703`)을 이 브랜치로 merge해 동기화했다(2026-09-25). DB 팩토리 경로는 develop 기준 `Factory/DBFactory/`(`ProjectL.Systems.Factory.DBFactory`, `.Group`)다.

## 기준 문서

| 문서 | 용도 |
|---|---|
| `Assets/ProjectL/Script/README.md` | 폴더·네임스페이스 구조 |
| `Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md` | Framework/Systems 구조와 조립 흐름 |
| `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md` | 개발 계약 FW-001~016, 확장 지점 |
| `docs/superpowers/specs/2026-09-24-db-so-ingame-view-group-design.md` | **DB-SO 재구성 설계** (규칙 P1~P7, 테이블 스펙, 결정 D1~D21, 코딩 규칙, §11.2 에디터 확인 체크리스트, §14 구현 결과) |
| `docs/superpowers/plans/2026-09-24-db-so-restructure-plan.md` | DB-SO 재구성 Task 계획과 실행 기록 |
| `docs/superpowers/specs/2026-09-24-view-db-orphan-asset-inventory.md` | 옛 SO 에셋 값 (복구 값의 출처) |
| `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` | Core 작업자 전달 사항 |
| `docs/superpowers/handoffs/2026-09-25-db-so-local-session-handoff.md` | 로컬 세션 인계: 작업 총정리, 남은 일 |

## 구조 요약

```text
Assets/ProjectL/Script/
├─ Framework/     공용 기반 (Systems·Core·FMOD를 참조하지 않음)
├─ Systems/       게임 서비스 (ProjectL.Systems 어셈블리) — View 코드도 여기에 둔다
│  ├─ DataBaseSystem/   Data(SO·Data 타입), Queries(Table 조회)
│  ├─ Factory/          생성과 조립
│  ├─ ServiceKeys/      서비스 키
│  └─ RuntimeSystem/    BufferSystems, GlobalSystem(UI·Render·Input…), InGameSystem(Core/View), LifeCycleSystem
│     └─ InGameSystem/CoreSystem/CoreCaculator/  옛 Core (Contracts/Domain/Application, Unity 참조 없음)
├─ Render/        URP 기능
└─ Tool/Editor/   에디터 도구
```

데이터 흐름(새 Core 기준):

```text
InGameGroup (규칙, 순수 C#) ──InitData──► Core 시스템 ──WriteSink──► Buffer
ViewGroup  (표현, Unity 타입 허용) ──InitData──► View 시스템 / UI ◄──ReadSink── Buffer
```

- View는 **InGameGroup을 읽지 않는다.** 규칙에서 나온 값은 Buffer로 받는다. 필요한 Buffer 필드는 View 쪽에서 정의할 수 있다.
- View는 Core에 데이터를 넣지 않는다.

## DB 규칙 (설계 §3 요약)

- **P1** InGame Data는 순수 C#만 사용한다 (`GameObject`, `Vector2Int`, `Color`, SO 참조 등 UnityEngine 타입 금지).
- **P2** SO는 기획 입력 계층이다. Item SO가 직렬화 필드를 갖고 `GetData()`가 순수 불변 Data를 **매번 새로** 만든다. InGame은 순수 Data 파일과 SO 파일을 분리한다.
- **P3** SO끼리의 참조는 입력 계층에서만 허용하고, `GetData()`에서 키로 바꾼다.
- **P4** 공용 타입은 옛 Core.Contracts 것을 재사용한다(`EntityKey`, `Rotation4`, `UnitOwnerEligibility`, `TraitKey`, `CellCoord`). 공격 모양·선택·스플래시는 지정 방식 테이블(`TargetingMethodData`)이, 사거리는 `UnitData.AttackRange`가 가진다(옛 `AppointMethod`·`AttackTargetMode`는 2026-10-06 삭제). `StageType`, `StageMapRole`만 `DataBaseSystem/Data/Common/`에 있다.
- **P5** ViewGroup은 Unity 타입을 쓸 수 있고, InGame과 같은 키로 조회한다.
- **P6** View에 필요한 규칙 값은 Buffer로 받는다.
- **P7** 다른 값에서 계산할 수 있는 표현 필드는 만들지 않는다.

## 코딩 규칙

- 네임스페이스 = `ProjectL.<Script 아래 폴더 경로>`, 블록 네임스페이스.
- Allman 중괄호, 공백 4칸, LF, UTF-8. 클래스는 `sealed` 기본, 접근자 명시.
- 직렬화 필드 `camelCase`, 일반 private 필드 `_camelCase`. 프로퍼티는 식 본문 또는 get-only.
- 서비스: `IXxx : IServiceModule`로 등록, 생명주기가 있으면 `AddModule`도(FW-001/002). Builder는 한 번만 Build.
- DB Query: `XxxTable : IXxxTableQuery, IExportSystemServices`, 생성자 `internal XxxTable(IDataProvider<IDatabaseItem>)`, `Get`(없으면 `KeyNotFoundException`) / `TryGet`.
- 예외: 인자 `ArgumentNullException(nameof(x))`, 상태·데이터 `InvalidOperationException`. 주석과 XML doc은 한국어.

## 주의 (실제로 겪은 문제)

- `Data.InGame` 아래 코드에서는 `Enum.IsDefined` 대신 **`System.Enum.IsDefined`**를 쓴다. `ProjectL.Systems.DataBaseSystem.Data.InGame.Enum` 네임스페이스와 이름이 겹친다.
- `InGameEnum.cs`의 `StageEnum`은 사용하지 않지만 **삭제 금지**. 테스트가 어셈블리 기준 타입으로 쓴다.
- 기존 오타 식별자는 유지한다: `CoreCaculator`, `Loding`, `Scop`, `Metrial`, `EffecDataTableSO`, `EventBuse`, `IMeshCraeteData`.
- 한국어 주석이 깨진 파일(U+FFFD)이 있다(`DatabaseCatalogSO.cs`, Render DB 파일 등). 새 코드에 복사하지 않는다.
- `.cs`를 만들거나 옮길 때 `.meta`를 함께 다룬다. 옮길 때 GUID를 유지하고, SO/MonoBehaviour 네임스페이스를 바꾸면 `MovedFrom`을 붙인다.
- `DataBaseFactory.Create`는 Catalog에 필요한 그룹 에셋과 테이블이 모두 있어야 성공한다.

## 검증

- Unity 에디터에서 컴파일하고 EditMode 테스트(`Assets/Tests/EditMode/`)를 실행한다.
- DB 에셋 값을 바꾸면 `DataBaseAssetTests`(실제 에셋 교차 검증)를 돌린다.
- 새 순수 Data 파일에는 `using UnityEngine`이 없어야 한다(P1).
