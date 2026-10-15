# 전투 View — Framework 공용 변경 예정과 F1 공유

> **최신 공용 변경 인계:** [2026-10-05 공동 Framework 공유](2026-10-05-combat-view-skeleton-framework-share.md). 아래의 예정/미착수/Unity 대기는 당시 기록이며 현재 상태는 공동 인계와 계획을 우선한다. 문서 작성과 외부 전달·합의는 구분한다.

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 Combat 담당)
- 대상: Framework 담당자(민형님). F1·개발 모드 부분은 Core 담당자(천명님)께도 공유
- 기준: `feature/Yonggeun/Stage` HEAD `ede27fef`, [전투 View 설계 8차](../specs/2026-09-30-combat-view-design.md), [구현 계획](../plans/2026-10-01-combat-view-plan.md) Phase B1·B2
- 상태: **전달용 문서 준비 완료. 외부 전달·답변·합의는 없음. 코드 변경도 아직 없다.** 승인을 기다리느라 B1을 막지는 않는다(계획 §3). 답변이 오면 §7에 적는다.

## 1. 요약

전투 씬을 실제로 띄우기 위해 View 작업이 아래 네 가지를 만든다. `FrameworkRuntime`·Runner·Register·Core·Buffer 코드는 바꾸지 않는다.

1. **부트스트랩과 오류 경계(W1·W17):** `GameBootstrap`(MonoBehaviour)이 DB 생성 → 런타임 조립 → 수명 단계 → 매 프레임 Tick → 종료를 맡는다. 첫 오류에서 Tick·입력·전환을 멈추는 `RuntimeFaultBoundary`를 런타임당 하나 만든다. Framework 가이드가 "연동 담당 코드"에 맡기고 DR-001로 남긴 구동 역할을 FW-016 계약대로 채우는 View 쪽 구현이며, Framework가 이 형태를 지정한 것은 아니다(확인 요청 Q1).
2. **Factory 인자(W11):** 루트 Factory가 준비 입력·실행 모드·오류 경계를 받아 하위 Factory로 전달한다.
3. **씬 번호 정리(W3):** 모든 LifeCycle이 `SceneIndices` 한 곳의 상수를 쓴다. Boot 0·Stage 1·Combat 2, Lobby 3·Artifact 4는 예약.
4. **개발용 기록 모드(W8·W20):** 개발 빌드에서만 준비까지 실제로 실행하고 이후 처리 Job 등록을 막는 `RecordInputOnly`. 기본은 `Live`.

## 2. 공용 파일 변경 예정

경로는 `Assets/ProjectL/Script/` 기준이다(테스트·설정 파일 제외).

| 파일 | 변경 | 보존할 것 |
|---|---|---|
| `Systems/Factory/Runtime/RunTimeSystemFactory.cs` | `FrameworkRuntimeFactory.Create(IBattleInitializationSource battleSource = null, CombatExecutionMode executionMode = Live, RuntimeFaultBoundary faultBoundary = null)`. 경계가 null이면 로그를 연결한 기본 경계를 하나 만든다 | 단일 `Create` 메서드(오버로드 추가 없음). 인자 없는 호출의 의미는 지금과 같음(source 없음, Live). 입력 슬롯·Controller 조립 순서 |
| `Systems/Factory/Runtime/InGame/InGameSystemFactory.cs` | 같은 세 값을 받아 Core Factory에는 `battleSource`만, View Factory에는 모드·경계를 전달 | Core에 모드·경계를 넘기지 않음. 기존 `InGameCoreSystemFactory.Create(battleSource)` 그대로 |
| `Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | 모드·경계를 받아 `CombatViewSystem`에 전달(C에서 등록), 경계의 입력 정리 콜백에 `StopInput` 연결 | View 그룹 구조 |
| `Systems/Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 모드·경계를 받아 `CombatLifeCycle` → `CombatFlow`로 전달 | 다른 LifeCycle 조립 |
| `Systems/RuntimeSystem/Common/RuntimeFaultBoundary.cs` (새 폴더) | 공유 오류 상태. 네임스페이스 `ProjectL.Systems.RuntimeSystem.Common`. View·LifeCycle·Unity 구체 타입을 참조하지 않음 | 새 서비스 등록·범용 이벤트 없음 |
| `Systems/RuntimeSystem/LifeCycleSystem/Base/SceneIndices.cs` (새 파일) | Boot 0·Stage 1·Combat 2·Lobby 3·Artifact 4 | — |
| `LifeCycles/{Lobby,Stage,Artifact,Combat}/…LifeCycle.cs` | `SceneIndex` getter만 상수로 교체 | 각 LifeCycle의 Flow·규칙 |
| `ProjectSettings/EditorBuildSettings.asset` | Boot 0·StageSelect 1·Combat 2 (에디터 작업). 기존 0번 `Stage.unity`는 목록에서 뺌 | 씬 파일 자체는 삭제하지 않음 |
| `Assets/Tests/EditMode/Framework/FrameworkRuntimeCompositionTests.cs` | `Create.Invoke(null, null)`을 정확한 인자 배열로 교체 | 테스트 의도 |

`ChapterGraphPreparationTests`의 Factory 도우미는 이미 매개변수 기본값 배열로 호출하므로 수정이 필요 없을 것으로 본다. B1에서 실행해 확인한다.

### 2.1 씬 번호 변경표

| LifeCycle | 지금 | 변경 후 | 이번 Build Settings |
|---|---|---|---|
| (Boot, LifeCycle 없음) | — | 0 | `Assets/Scenes/Boot.unity` 새로 추가 |
| Stage(스테이지 맵) | 1 | 1 | 기존 `StageSelect.unity` |
| Combat | 3 | 2 | `Assets/Scenes/Combat.unity` 새로 추가 |
| Lobby | 0 | 3(예약) | 없음. 시작 대상으로 거부(기존 `Title.unity`가 후보) |
| Artifact | 2 | 4(예약) | 없음. 시작 대상으로 거부 |

지금 Lobby 0은 Build Settings의 0번(`Stage.unity`)과, Artifact 2는 새 Combat 2와 겹친다. 예약 번호에는 빈 씬을 만들지 않으며 실제 씬을 넣을 때 표와 Build Settings를 함께 갱신한다.

**기존 `Stage.unity`는 Build Settings에서 뺀다.** 이름과 달리 옛 전투 씬이며(옛 View 스크립트 참조와 옛 UI 포함), 새 구조의 Stage(스테이지 맵)는 `StageSelect.unity`다. 파일은 삭제하지 않는다. 이 씬을 테스트에 쓰던 분이 있으면 알려 주기 바란다.

## 3. F1 — 오류 뒤 같은 런타임을 다시 Tick하지 않기

**문제(코드 확인):** `JobRunnerSystem`은 `Execute()` 예외가 나도 현재 Job을 지우지 않는다. `FrameworkRuntime.Tick()`은 예외를 전파한다. 다음 프레임에 같은 런타임을 Tick하면 같은 Job이 다시 실행된다. Core 쪽 최종 점검 문서([Task 8R6](https://github.com/ANTI-LIKE/Project_L/blob/feature/Cheon/refactor/core-system/docs/superpowers/handoffs/2026-10-02-task8r6-core-closure-and-view-handoff.md))도 같은 점을 지적하고 F1을 처리 경로 활성화 전 조건으로 둔다.

**대응:** Runner를 고치지 않고 구동자 쪽에서 막는다.

| 입구 | 동작 |
|---|---|
| Bootstrap의 초기화·시작 전환·Tick | `try/catch`에서 경계에 보고. 이후 `Update`는 Tick·전환을 호출하지 않음 |
| View 입력 콜백(Unity 입력·개발 Layer 버튼) | 각 입구를 같은 경계로 감쌈. Unity 콜백은 Tick 밖에서 오므로 Tick의 catch에 기대지 않음 |
| 같은 Tick 후반의 `CombatFlow` | 등록 전에 같은 경계의 `IsFaulted`를 읽어 새 Job 등록 0 |

첫 오류 처리 순서: 오류 상태를 먼저 고정 → View 입력 정리(`StopInput`: 연출 취소, Drag·숨김 정리, `Unbind`, `ClearLayer`) → 원인과 정리 실패를 모아 **한 번** 보고. 실행 중인 Tick 스택에서 `Shutdown`을 부르지 않는다. 종료는 Bootstrap의 공통 경로에서 입력 정리 → `runtime.Shutdown()` 최대 1회 → `finally`로 소유 참조 정리. 기존 `FrameworkRuntime`은 하위 Shutdown 예외에서 순회를 멈추므로 Bootstrap이 모든 Framework 자원 정리를 보장한다고 주장하지 않는다.

**위치(W17):** 오류 경계는 Flow(LifeCycle)와 View가 같이 읽으므로 View/Boot 폴더가 아니라 `RuntimeSystem/Common`에 둔다. Bootstrap이 생성·소유하고 Factory 인자로 주입한다.

## 4. 개발용 실행 모드와 개발 Layer 버튼

- `CombatExecutionMode { Live = 0, RecordInputOnly = 1 }`은 조립 때 고정한다. `RecordInputOnly`는 `UNITY_EDITOR || DEVELOPMENT_BUILD`에서만 허용하고 비개발 조립에서는 명시적으로 거부한다.
- `RecordInputOnly`에서는 실제 준비 Job까지 실행하고, 이후 명령·미리보기·자동 진행 Job은 `CombatFlow`가 등록하지 않는다. 입력은 실제 `CombatViewBuffer`에 기록되며 결과·확인 번호는 만들지 않는다. 규칙 원본은 [골격 설계 §4.1](../specs/2026-09-30-combat-flow-skeleton-design.md)의 0행·4a행이다.
- 기록 모드에서 이동·공격을 확인하려면 입력 Layer를 전투로 바꿔야 한다. 공용 키 바인딩을 추가하지 않고 **Combat 씬 Canvas의 개발 패널 버튼**(UnityEngine.UI)으로 한다. Framework 전역 EventSystem을 구독하지 않으며 Live에서는 패널을 끄고 등록하지 않는다.

## 5. 바꾸지 않는 것

`FrameworkRuntime`, `JobRunnerSystem`·`JobRegisterSystem`, 공용 입력 시스템(`UnityInputBridge`·Controller·Layer 서비스), Core 서비스, Buffer 저장 구조, CombatPlan·Job 몸통. 다른 LifeCycle은 SceneIndex getter 한 줄만 바뀐다.

## 6. 확인 요청 (민형님)

| 번호 | 질문 | 답이 없을 때 진행 |
|---|---|---|
| Q1 | 가이드는 구동을 "연동 담당 코드"에 맡기고 DR-001로 남겨 두었다. View가 `GameBootstrap`(MonoBehaviour + Boot 씬 + `DontDestroyOnLoad`)으로 그 역할을 맡아도 되는가? Framework 쪽에 부트스트랩·런타임 구동자 계획이 따로 있으면 중복을 피하고 맞추겠다. 맡는 일·맡지 않는 일은 [설계 §10.0](../specs/2026-09-30-combat-view-design.md) | View의 `GameBootstrap`으로 진행(W1) |
| Q2 | `RuntimeSystem/Common` 폴더를 공통 보조 객체 위치로 써도 되는가? | 이 위치로 진행(W17) |
| Q3 | 씬 번호를 §2.1처럼 바꾸는 데 다른 작업자의 씬 계획과 충돌이 있는가? | 예약 번호로 분리해 진행 |
| Q4 | 루트 Factory의 선택 인자 세 개(단일 `Create`)에 이견이 있는가? | 제안대로 진행(W11) |
| Q5 | 장기적으로 Runner가 예외 Job을 스스로 멈추도록 바꿀 계획이 있는가? 생기면 Flow의 오류 검사 필요성을 다시 본다 | 지금은 구동자 정지 + Flow 검사 |

### 6.1 DR-003 임시 연결 (2026-10-02, 부트스트랩을 처음 실제로 돌리다 발견)

- **증상:** Boot 재생 시 `OutlineSystem.Initialize`(`Sub/OutlineSystem/OutLineSystem.cs:56`)에서 NullReferenceException. `RenderDataBaseFactory`의 Material 테이블 등록과 `RenderDataBase`의 `MetrialTableQuery` 할당이 주석 처리돼 있고 Material 테이블 에셋도 없기 때문이다. 가이드 DR-003에 적힌 그 문제다. 지금까지는 런타임을 Initialize까지 돌린 호출자가 없어 드러나지 않았다.
- **확인:** `origin/feature/JMH/Shader`(`b2ab3cf9`)에 Material 테이블 연결이 들어 있다. 다만 그 브랜치는 Render 구조를 다시 짜는 중이고(Outline 위치 이동, 키 `_AreaOutline`·`_SelectOutline`), Stage 브랜치와 충돌해 통째로 합칠 수 없었다.
- **Stage 브랜치에 넣은 임시 연결(사용자 결정):**

| 변경 | Shader 브랜치와의 관계 |
|---|---|
| `RenderDataBaseFactory.cs`: `MetrialTable` 생성·Export 두 줄 주석 해제 | Shader 브랜치와 **같은 변경** |
| `RenderDataBase.cs`: `MetrialTableQuery` 할당 주석 해제 | **같은 변경** |
| `Data/Render/Table/Item/MetrialDataSO.cs` → `MaterialDataSO.cs`(`.meta`·GUID `71d386c8…` 유지) | **같은 이름 변경**(파일 이름 = SO 클래스 이름) |
| 새 `DataBaseSO/RenderGroup/TempMaterialTable/`(테이블 1·아이템 1), `RenderGroup.asset`에 등록 | **임시.** 아이템 키 `_Outline` → `Render/Shader/Outline/SelectionOutline.mat`(Shader 브랜치의 `_SelectOutline`과 같은 머티리얼). 현재 Stage 브랜치의 `OutlineSystem`이 `_Outline` 키를 찾기 때문 |

- **Shader 브랜치를 합칠 때:** 코드 두 줄과 파일 이름 변경은 같아서 그대로 합쳐진다. `RenderGroup.asset`의 테이블 목록은 양쪽이 한 줄씩 더해 충돌하며, 같은 타입의 Material 테이블이 둘이면 DB 생성이 실패할 수 있다. **`TempMaterialTable` 폴더와 그 등록 줄을 지우고 Shader 브랜치의 `MaterialTable`을 쓰면 된다.** Outline 코드도 Shader 쪽 구조로 바뀌므로 `_Outline` 키는 그때 필요 없어진다.
- **Q6:** Shader 브랜치의 Render DB·Outline 변경을 develop(또는 Stage 브랜치)에 언제 합칠 예정인가? 합치는 쪽이 위 임시 연결을 지우는 것으로 정리해도 되는가?

## 7. 천명님께 함께 알릴 것

- F1은 위 Bootstrap + 경계 하나로 구현하고 골격 Phase 6이 인수한다. Core에는 준비 입력(`IBattleInitializationSource`)만 전달하며 모드·경계는 넘기지 않는다.
- 개발 Live는 명시적 fixture + 개발 전용 진행 준비(`InitializeRoster` → `ChapterProgression.EnterChapter` → `CombatEntry.RequestEntry`(6A/B3, §9))로 검증한다. 운영 DB 준비(T1/G7) 성공과 구분한다. Core 확인 사항은 [Core 전달 메모](2026-10-02-combat-view-core-notes.md)에 따로 정리했다.
- 8R6 문서의 "세 Job 미연결" 표는 Core 브랜치 기준이다. Stage 브랜치(`ede27fef`)에서 이미 `ProcessNextCommand`·`ProcessLatestPreview`·`AdvanceCombat`에 연결했다.

## 8. 전달·답변 기록

| 대상 | 전달 | 답변 | 합의 내용 |
|---|---|---|---|
| 민형님 | 미전달 | — | — |
| 천명님(§7) | 미전달 | — | — |

전달은 사용자가 한다. 답변이 오면 이 표와 View 계획 §7에 적고, 설계와 달라지는 답이면 구현 전에 다시 정한다.


## 9. 6A/B3 공용 등록·진입 서비스 변경 (2026-10-04, 미전달)

- `IJobRegisterSystem`에 `RegisterCombatEntry(CombatEntryRequest)`를 추가했다. 기존 Register(root, type)와 다른 루트 Plan, enum 값·Runner 동작은 유지한다. 기존 인터페이스 대역은 새 멤버 구현이 필요하다.
- `ILifeCycleSystemGroupService.CombatEntry`와 내부 타입 키를 추가했다. 기존 CombatLifeCycle이 서비스·모듈을 함께 Export하며 별도 모듈/Runner를 만들지 않는다.
- LifeCycle의 명시적 ISystemTick은 비활성 시에도 진입 결과를 확인한다. 기존 Manager→ILifeCycleModule.Tick은 활성 전투 Flow만 실행하므로 Flow가 중복 Tick하지 않는다.
- 새 요청 객체는 캡처 대상과 해당 결과·수명만 보관한다. 수락 후 Manager.Change를 한 번 호출하며 예외 경계는 기존 F1 그대로다. 외부 콜백·이벤트 버스·Core 큐·범용 payload 없음.
- 규칙·파일·검증의 원본은 [골격 설계 §6.3](../specs/2026-09-30-combat-flow-skeleton-design.md#63-명시적-전투-진입-계약-k20), [골격 계획 §6.10](../plans/2026-09-30-combat-flow-skeleton-plan.md#610-phase-6a-실행-기록-2026-10-04-코드-완료unity-검증-대기), View 계획 §7.17이다. Unity 검증 대기이며 담당자 합의를 받았다는 뜻은 아니다. 전달은 사용자가 한다.
