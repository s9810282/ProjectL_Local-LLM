# View 작업자 공유 — 게임 시작 방식·씬 구성·DB 변경 (2026-10-02)

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 Combat 담당)
- 대상: View 작업자 전원(스테이지 맵·Architecture·UI·FieldCamera·사운드). 렌더·Artifact 관련 항목은 민형님·호진님께도 해당
- 기준: `feature/Yonggeun/Stage`, 전투 View Phase B1 완료 시점(2026-10-02). B1 변경은 사용자 커밋 전일 수 있다
- 상태: **전달용 문서. 전달 미실시.** 질문·이견은 §10 표에 적는다

## 0. 한눈에 — 무엇이 바뀌었나

1. **게임은 이제 Boot 씬에서 시작한다.** `GameBootstrap`이 런타임 하나를 만들어 매 프레임 돌린다. 다른 씬에서 바로 Play하면 런타임이 없다 → 메뉴 `ProjectL > Play From Boot`를 켜면 어느 씬에서 Play해도 Boot부터 시작한다(§1).
2. **씬 번호가 바뀌었다.** Boot 0 · StageSelect 1 · Combat 2. **옛 `Stage.unity`는 Build Settings에서 빠졌다**(파일은 남아 있음)(§2).
3. **씬 오브젝트는 판단을 갖지 않는다.** 판단은 게임 시작 때 만든 시스템이 맡고, 씬에 들어올 때 SceneRoot를 한 번 찾아 연결한다(§3, 예시 코드는 안내서).
4. **`BindScene`은 Boot에서 한 번만 불린다.** 씬별 UI를 `UISystem`에 맡기던 방식은 지금 동작하지 않는다(§3.2). **`AudioSystem`은 런타임에 등록돼 있지 않다**(§8).
5. **첫 오류가 나면 런타임이 멈춘다.** 같은 오류가 매 프레임 반복되지 않는다(§4).
6. **Factory 서명이 바뀌었다.** 리플렉션으로 `Create`를 부르는 테스트는 인자 배열을 넘겨야 한다(§5).
7. **DB 에셋이 바뀌었다.** InGameGroup 테이블 12개, 전투 스테이지 Economy, 메인 캐릭터, Artifact 외형 테이블 비움, Render Material 테이블 임시 연결(§6·§7).

## 1. 게임 시작 방식 — Boot 씬과 GameBootstrap

```text
[Boot 씬 → DontDestroyOnLoad]
GameBootstrap ── 소유 ──► FrameworkRuntime (게임 전체에 하나)
  DB 생성 → 런타임 조립 → BindScene → InitData → PostBindService → Initialize
  → 시작 구간으로 한 번 전환(지금은 Combat) → 매 프레임 Tick → 종료 때 Shutdown 1회
```

- 위치: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs`. Boot 씬에 하나만 둔다. 다른 씬에 넣지 않는다.
- Inspector: Database Catalog(`DataBaseSO/DatabaseCatalogSO.asset`), Start Life Cycle(Combat·Stage), Execution Mode(Live·RecordInputOnly — 기록 모드는 개발용).
- **GameBootstrap을 코드에서 찾거나 부르지 않는다.** 서비스 창구가 아니다. 서비스는 각 시스템이 `PostBindService`로 받는다.
- 맡는 일·맡지 않는 일 전체: [전투 View 설계 §10.0](../specs/2026-09-30-combat-view-design.md). Framework가 지정한 형태가 아니라 View 쪽 구현 결정이며, Framework가 비워 둔 구동 역할(가이드 "연동 담당 코드", FW-016, DR-001)을 채운 것이다. 민형님 확인 대기([Framework 공유 Q1](2026-10-02-combat-view-framework-share.md)).

**Play 방법**

| 상황 | 방법 |
|---|---|
| 런타임과 함께 테스트 | Boot 씬에서 Play, 또는 `ProjectL > Play From Boot`를 켜고 아무 씬에서 Play(멈추면 편집하던 씬으로 돌아온다) |
| 런타임 없이 씬만 보기(HMUTest·JMHTest 등) | 토글을 끈 채 그 씬에서 Play |

`Play From Boot`는 **내 PC에만 저장되고 기본값은 꺼짐**이라 다른 사람의 Play 동작은 바뀌지 않는다(`Tool/Editor/PlayFromBootToggle.cs`).

## 2. 씬 번호와 Build Settings

| 번호 | 씬 | LifeCycle | 비고 |
|---|---|---|---|
| 0 | `Assets/Scenes/Boot.unity`(새) | 없음 | GameBootstrap만 |
| 1 | `Assets/Scenes/StageSelect.unity` | `StageLifeCycle`(스테이지 맵) | 기존 씬 |
| 2 | `Assets/Scenes/Combat.unity`(새) | `CombatLifeCycle`(배치·전투) | |
| 3 | (없음, 예약) | `LobbyLifeCycle` | `Title.unity`가 후보 |
| 4 | (없음, 예약) | `ArtifactLifeCycle` | |

- **옛 `Stage.unity`는 이름과 달리 옛 전투 씬**이고(옛 View 스크립트 참조·옛 UI), 스테이지 맵은 `StageSelect.unity`다. 옛 `Stage.unity`는 Build Settings에서만 뺐고 삭제하지 않았다. 이 씬으로 테스트하던 분은 알려 주기 바란다.
- 번호의 단일 기준은 `RuntimeSystem/LifeCycleSystem/Base/SceneIndices.cs`다. 네 LifeCycle의 `SceneIndex`가 이 상수를 쓴다(이전 임시값 Lobby 0·Stage 1·Artifact 2·Combat 3).
- **씬을 추가·이동하면 `SceneIndices`와 Build Settings를 함께 고친다.** GameBootstrap이 시작할 때 Boot·Stage·Combat의 경로·번호를 대조해 틀리면 오류 1회로 멈춘다.
- 로딩 시스템은 씬 번호를 열쇠로 Enter/Exit 처리를 실행한다. 번호가 겹치면 다른 구간의 처리가 함께 실행되므로 숫자를 직접 쓰지 않는다.

## 3. 씬에 붙는 시스템 만드는 법

**안내서: [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md)** (스테이지 맵을 예로 든 예시 코드, MSBuild 컴파일 확인).

### 3.1 두 층

| 층 | 무엇 | 언제 생기나 |
|---|---|---|
| 시스템(일반 C#) | 언제 무엇을 그릴지, 입력 해석, 결과 확인 같은 판단 | 게임 시작 때 Boot에서 한 번 Factory가 만들어 런타임에 등록. 게임 끝까지 유지 |
| 씬 오브젝트(MonoBehaviour) | SceneRoot·Presenter·View·Canvas·프리팹 | 지금처럼 에디터에서 씬에 미리 올려 둔다 |

씬에 들어올 때는 **생성이 아니라 연결(Attach)** 이다. 시스템이 `Initialize`에서 "씬 번호 N이 로드되면 / 떠나면" 할 일을 로딩에 등록하고(View는 Enter priority 100, Exit -100), 로드되면 SceneRoot를 한 번 찾아 붙잡고, 떠날 때 놓는다. Tick·입력 경로에서 `Find`·`GetComponent`·LINQ를 쓰지 않는다.

### 3.2 `BindScene`은 Boot에서 한 번뿐 (UI 담당 필독)

- `FrameworkRuntime.BindScene`은 GameBootstrap이 **Boot 씬(빈 씬)에서 한 번** 부른다. 씬 로드 뒤 다시 불리지 않는다(Framework DR-001 미해결).
- 그래서 `UISystem.BindScene`이 찾는 `UIBase`는 **Boot 씬 기준**이다. Combat·StageSelect 씬에 올린 `UIBase`는 `UISystem`에 등록되지 않는다(시작 때 "UIBase를 찾지 못했습니다" 경고가 그 신호다).
- 지금의 우회: 씬 UI는 그 씬의 SceneRoot가 참조로 들고, 그 씬에 붙는 시스템이 직접 갱신한다(전투 HUD가 이 방식, 설계 §13.3·DR-010).
- 씬별 UI를 `UISystem`으로 관리하려면 로드 뒤 재바인딩 정책이 필요하다. Framework(민형님) 결정 사항이므로 필요하면 같이 요청하자.

## 4. 오류가 나면 — 런타임 정지(F1)

- 런타임당 오류 경계 하나(`RuntimeSystem/Common/RuntimeFaultBoundary.cs`)를 GameBootstrap이 만든다. 초기화·전환·Tick에서 예외가 나면 **원인을 한 번 로그로 남기고 Tick·전환을 멈춘다.** Job Runner는 예외 난 Job을 버리지 않으므로, 멈추지 않으면 같은 Job이 매 프레임 다시 실행된다.
- 복구는 없다. 재생을 다시 시작하면 새 런타임이 뜬다.
- **Unity 입력 콜백은 Tick 밖에서 온다.** 입력을 받는 View 시스템은 콜백 입구를 같은 경계로 감싸야 한다(전투 View Phase C에서 구현 예정, 설계 §10.1).
- 재생 중 Console에 오류가 하나 찍히고 화면이 멈춰 있으면, **첫 오류 줄**이 원인이다. 그 뒤에 이어지는 경고는 대개 결과다.

## 5. Factory 서명 변경 (시스템을 추가하거나 조립 테스트를 쓰는 분)

| Factory | 새 서명 | 전달 |
|---|---|---|
| `FrameworkRuntimeFactory.Create` (`Factory/Runtime/RunTimeSystemFactory.cs`) | `(IBattleInitializationSource battleSource = null, CombatExecutionMode executionMode = Live, RuntimeFaultBoundary faultBoundary = null)` | 경계가 없으면 로그를 남기는 기본 경계를 만든다 |
| `InGameSystemFactory.Create` | 같은 세 인자 | Core에는 준비 입력만, View에는 모드·경계만 |
| `InGameViewSystemFactory.Create` | `(executionMode, faultBoundary)` | View 시스템 등록 자리(지금은 비어 있음) |
| `LifeCycleSystemFactory.Create` | `(executionMode, faultBoundary)` | `CombatLifeCycle`에만 전달 |

- 인자 없는 C# 호출은 그대로 동작한다(기본값 = 지금까지와 같은 조립).
- **리플렉션은 선택 인자를 채우지 않는다.** `MethodInfo.Invoke(null, null)`로 부르면 `TargetParameterCountException`이 난다. 정확한 인자 배열(또는 `GetParameters()`의 기본값 배열)을 넘긴다. `FrameworkRuntimeCompositionTests`를 이렇게 고쳤다.
- View 시스템은 `InGameViewSystemFactory`에서 `Build` **전에** 등록한다(FW-003).

## 6. DB 에셋 변경 (모두 `Assets/ProjectL/DataBaseSO/`)

| 변경 | 내용 | 관련 |
|---|---|---|
| InGameGroup 테이블 9 → 12 | `Category/`(CategoryConfig_Default 50·R0), `Economy/`(StageEconomy_1: 키 1, 시작 골드 200, 드로우 비용 40), `Recovery/`(RecoveryOption 1~3) | DB 계획 T11 |
| 전투 스테이지 11개 | `economy` = StageEconomy_1, `nightLength` 500 추가(이전에는 필드 자체가 없어 DB 생성이 실패했다) | T11 |
| 메인 캐릭터 | `InGameGroup/Unit/Item/Unit_MainCharacter`(키 14) 추가, `ViewGroup/UnitVisual/Item/UnitVisual_MainCharacter`(임시 프리팹 `TestUnit_Base`, 영문 이름) | T11·T12 일부 |
| `PlayerLoadout_1` | 메인 = MainCharacter, 시작 서브 Akimbo·ShotGun·Sniper 각 1. Core 규칙상 시작 서브는 **정확히 3명**이다(T11 구성 4명과 다름, Core 문의 중) | [Core 메모 §3a](2026-10-02-combat-view-core-notes.md) |
| Artifact 외형 테이블 비움 | `ViewGroup/Artifact/ArtifactViewDataTable`에 아이콘 없는 외형 3개가 등록돼 DB 생성이 실패했다. 원래 의도(등록 보류)대로 목록만 비우고 SO 3개는 보존 | [호진님 메모](2026-10-02-artifact-view-table-note.md) |

- **DB는 Render → InGame → View 순서로 만들어지고, 앞 그룹의 오류가 뒤 그룹의 오류를 가린다.** DB 에셋을 고친 뒤에는 `DataBaseAssetTests`(특히 `Catalog_CreatesAllDataBaseGroups`)와 `ArtifactDataTableTests`를 꼭 실행한다. 2026-10-02 기준 모두 통과한다.

## 7. Render DB 임시 연결 (민형님·렌더 관련)

- 증상: 런타임을 끝까지 초기화하자 `OutlineSystem.Initialize`가 `_Outline` 머티리얼을 찾다 NullReferenceException(Framework DR-003).
- 바꾼 것: `RenderDataBaseFactory`·`RenderDataBase`의 Material 테이블 주석 해제, `MetrialDataSO.cs` → `MaterialDataSO.cs`(GUID 유지) — `origin/feature/JMH/Shader`와 같은 변경. 그리고 **임시** `RenderGroup/TempMaterialTable`(키 `_Outline` → `Render/Shader/Outline/SelectionOutline.mat`).
- Shader 브랜치를 합칠 때 `TempMaterialTable` 폴더와 `RenderGroup.asset`의 등록 줄을 지우고 그쪽 `MaterialTable`을 쓴다. 자세한 내용: [Framework 공유 §6.1·Q6](2026-10-02-combat-view-framework-share.md).

## 8. 담당별로 확인할 것

| 담당 | 확인할 것 |
|---|---|
| 스테이지 맵(StageSelect) | 씬 번호 1. 맵 화면을 런타임과 연결하려면 안내서 패턴(시스템이 씬 번호 1에 Enter/Exit 등록, SceneRoot 연결). `StageLifeCycle.OnEnter`는 아직 비어 있다. 구간 전환(맵 → 전투 → 맵) 호출자는 아직 없다(F2) |
| Architecture | `StageSelect.unity` 안의 Architecture 화면도 같은 제약을 받는다: 씬 로드 뒤 `BindScene`이 다시 불리지 않으므로 런타임 서비스가 필요하면 씬에 붙는 시스템을 통해 연결 |
| UI | §3.2 필독. 씬별 `UIBase`는 `UISystem`에 자동 등록되지 않는다 |
| 사운드 | **`AudioSystem`은 어떤 Factory에도 등록돼 있지 않아 런타임에 없다.** `AudioManager`·`AudioBase`는 런타임과 연결되지 않은 채 씬 단위로만 동작한다. 런타임 연결이 필요하면 Global Factory 등록과 씬 재바인딩 정책을 함께 정해야 한다. 옛 `Stage.unity`에 있던 AudioManager는 새 Combat 씬으로 옮기지 않았다 |
| FieldCamera | Combat 씬에 카메라는 자리만 있다(Basic URP 기본 카메라). 전투 View Phase C에서 `CombatSceneRoot`가 참조만 들 예정이며 카메라 동작은 건드리지 않는다 |
| 렌더(민형님) | §7. Shader 브랜치 병합 시점과 임시 연결 정리 |
| Artifact(호진님) | §6 Artifact 외형 테이블. 아이콘이 생기면 다시 등록하고 테스트 기대값 갱신 |
| 테스트를 쓰는 분 | §5 리플렉션 호출, §6 DB 테스트 |

## 9. 아직 없는 것 (오해 방지)

- **구간 사이 전환**(Title → Stage → Combat → Stage)의 호출자·정리 순서: 골격 Phase 6의 F2에서 정한다. 지금 실제 전환은 GameBootstrap의 시작 전환 한 번뿐이다.
- **씬 재바인딩**(로드 뒤 `BindScene`): Framework DR-001 미해결.
- **운영 DB 전투 준비**(T1/G7): Core 작업. 지금 Live로 Combat에 들어가면 `G7: 현재 챕터와 스테이지 선택이 필요합니다.`로 멈추는 것이 정상이다(전투 View Phase B2에서 개발용 준비 입력·진행 위치 준비를 붙인다).

## 10. 연결 문서와 전달 기록

| 문서 | 내용 |
|---|---|
| [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md) | 씬에 붙는 시스템 예시 코드 |
| [전투 View 설계](../specs/2026-09-30-combat-view-design.md) §10.0·§11 | Bootstrap 역할, 씬 번호 |
| [전투 View 계획](../plans/2026-10-01-combat-view-plan.md) §7.7~§7.9 | Phase 0·A·B1 실행 기록 |
| [Framework 공유](2026-10-02-combat-view-framework-share.md) | 공용 변경, F1, Q1~Q6 |
| [Core 메모](2026-10-02-combat-view-core-notes.md) | Reason 코드·재화·시작 서브 수 |
| [Artifact 메모](2026-10-02-artifact-view-table-note.md) | 외형 테이블 정정 |
| [입력 사용 안내](../viewDocs/stage-input-guide.md) | 스테이지 내부 입력 |

| 대상 | 전달 | 질문·이견 |
|---|---|---|
| View 작업자 전원 | 미전달 | — |

전달은 사용자가 한다.
