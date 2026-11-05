# Combat 유닛 사전 준비 구현 검토와 로딩 계약 공유

작성·갱신일: 2026-11-03 · Codex

상태: P1~P4 코드 반영과 정적 검토 완료. **컴파일 미확인**, Unity 기능·성능 인수 대기.

공유 대상: 민형님(공용 로딩 계약). 공유용 문서만 작성했으며 외부 전달하지 않음.

선택한 출전 유닛의 최초 생성·HUD 활성화 비용을 Combat 로딩 준비 구간으로 옮겼다. 실제 배치의 프레임 시간 감소와 첫 배치 신규 생성 0은 아직 측정으로 확인하지 않았다. [설계](../specs/2026-11-02-combat-selected-unit-prewarm-design.md)와 [Task·성능 인수 절차](../plans/2026-11-02-combat-selected-unit-prewarm-plan.md)가 기준이다. 사용자의 “P4까지 쭉 진행하고 검토” 지시로 이번에는 Phase별 중단 없이 연결했다.

## 1. 구현 범위

아래 코드 경로의 `Combat/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `GlobalSystem/`은 같은 RuntimeSystem 아래다.

| Phase | 반영 내용 | 주요 파일 |
|---|---|---|
| P1 | RunDeck.OrderedIds를 보유 ID·종류·현재 전투 개체에 대응. 같은 종류 여러 개체와 아직 드로우되지 않은 선택을 유지. | `Combat/Display/Unit/CombatUnitPreparationReader.cs`, `Combat/CombatViewSystem.cs` |
| P2 | 기존 종류별 풀과 모델·HUD·클립·체력 텍스트 생성 경로 사용. 준비 대여를 동시에 보유하여 같은 객체 하나를 반복 준비하지 않음. | `CombatUnitPreparationLease.cs`, `FieldUnitPresenter.cs`, `FieldUnitView.cs` |
| P3 | 한 Tick에 한 개체 준비. AfterLoad 뒤 실제 HUD Canvas에서 활성화하고 마지막 데이터 적용 후 UI 갱신 프레임을 지난 뒤 반환. 완료 후 Reveal. | `CombatUnitPreparation.cs`, `UnitHUDLayer.cs`, `GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs`, `GlobalSystem/LoadingSystem/` |
| P4 | 실제 배치의 기존 Acquire/Bind/Apply가 풀 객체와 최신 Buffer 값을 사용. 정상 배치에 강제 Canvas 갱신을 추가하지 않음. 생성·HUD 적용·결과 반영 계측 추가. | `FieldUnitPresenter.cs`, `CombatViewConnection.cs`, `CombatSceneDisplay.cs`, `CombatViewSystem.cs`, `CombatSceneEventPresenter.cs` |

실제 전투 데이터가 없는 유닛은 공통 HUD 위젯과 숫자 글리프 등 표현 자원만 준비한다. 임시 EntityId·전투 수치·Core 명령은 만들지 않는다. 모델은 Renderer·Collider와 애니메이션 이벤트를 막은 상태로 활성화하고 준비 뒤 원래 상태를 복원한다. GPU 셰이더 전체, 모든 공격·VFX, 닫힌 정보창·툴팁은 이번 준비 대상이 아니다.

씬·프리팹 연결 변경은 필요하지 않았다. 새 C# 파일 4개에는 각각 `.meta`를 추가했다. Core·FieldCamera·Architecture·공용 CustomPool·EntityViewSet·UIBase는 수정하지 않았다. 기존 사용자 에셋 변경은 그대로 두었다.

## 2. 검토 중 보완한 부분

| 검토 항목 | 보완과 현재 동작 |
|---|---|
| 입력 조기 허용 | 준비 작업 상태와 LoadingSystem.IsLoading을 기존 진입 대기 조건에 합쳤다. Shown은 Install 성공 의미를 유지한다. 보드 연출이 없는 경우도 준비·Reveal 뒤 입력을 다시 연결한다. |
| 상호 대기 | 준비 작업은 LoadingSystem이 Tick한다. 보드 연출은 준비와 로딩 종료 뒤 진행하므로 준비가 로딩 종료를 기다리지 않는다. |
| 부분 생성·반환 실패 | 생성 실패 자원을 정리한다. 반환의 한 단계가 실패해도 나머지를 시도하고 오류를 합친다. 완료 처리 전에 모든 준비 대여를 반환한다. |
| 취소 재진입 | Cancel은 멱등적이다. 대여 목록에서 먼저 제거한 뒤 Dispose하여 콜백 중 중복 취소가 같은 대여를 다시 반환하지 않는다. 로딩 Tick도 콜백 뒤 현재 작업·단계를 다시 확인한다. |
| 오래된 연결 | 연결 객체·BattleInstance·RunId·DeckRevision을 대조한다. Cancelled/Failed를 입력 허용이나 성공 완료로 해석하지 않는다. |
| 같은 씬 재연결 | 일반 반환은 준비 자원을 보존한다. Clear는 기존 클립·재질 해제 범위를 유지한다. 최종 Display.Release에서 모델 풀까지 종료하여 이전 연결의 해제된 여분 객체가 새 준비 객체보다 먼저 대여되는 일을 막는다. |
| UI 수명 | HUD 가시성은 UnitHUDLayer와 기존 소유자 Open/Hide를 통한다. 준비 표시의 입력·레이캐스트를 막고, 취소에도 소유자 가시성 복원을 시도한다. 불필요한 동일 활성화 요청만 UnitHUD 안에서 줄인다. |

코드 경로 검토에서 남은 확정 결함은 발견하지 못했다. 이 판단은 컴파일·Unity 실행 검증을 포함하지 않는다. 공용 로딩 변경, 실제 Canvas 갱신, 최대 선택 수량의 메모리·진입 시간은 아래 인수 항목으로 남긴다.

## 3. SSOT·SOLID·JMH/FW 검토

| 기준 | 검토 결과 |
|---|---|
| SSOT | 선택은 RunDeck, 전투 사실은 Character Buffer, 필드 개체 연결은 EntityViewSet, 자원은 기존 풀, 준비 완료는 연결당 작업 한 곳, 로딩 단계는 LoadingSystem이 소유한다. 준비 수량·대여 수량 사전은 계측용이며 선택이나 할당의 원본으로 사용하지 않는다. |
| SRP | 선택 읽기, 프레임별 진행, 임시 대여 정리, HUD 표시, 로딩 전이를 분리했다. CombatViewSystem과 SceneDisplay는 기존 연결·조립을 담당한다. |
| OCP | LoadingSystem은 씬별 준비 계약만 소비한다. Combat 분기·유닛 수·HUD 타입을 넣지 않았다. |
| LSP | 기존 동기 Node, 준비 없는 씬, 정상 풀 반환과 UI Open/Hide 의미를 유지한다. 준비의 Complete는 정리를 마친 성공만 뜻한다. |
| ISP·DIP | 준비 대상 읽기는 RunDeck/Character 읽기 Sink, 로딩은 ILodingPreparation에 의존한다. 새 전역 서비스나 부모 Context 중복 등록은 없다. |
| FW-001~004·016 | 기존 서비스 바인딩과 Initialize 등록을 사용한다. 동기 씬 Enter와 AfterLoad의 finally 순서를 유지하고 그 뒤 준비를 진행한다. |
| FW-008~009·013~014 | HUD 내부 위젯 소유권, Canvas 구독 해제, 기존 CustomPool 콜백 순서를 유지한다. 준비 대여를 가짜 필드 개체로 등록하지 않는다. |
| JMH·기능 독립성 | 선택 수량만 준비한다. 대상 읽기 변경은 Reader, UI 준비 변경은 HUD/Layer, 로딩 진행 변경은 공용 계약에 모인다. 이번 공용 계약의 영향 때문에 Lobby·Stage 전환은 회귀 확인이 필요하다. |

기준: [프로젝트 설계 원칙 §4](2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [JMH 지침](../framework/Framework%20Development%20Guidelines.md), [Framework 프로그래머 안내](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md).

## 4. 공용 로딩 계약 공유

`ILodingRegisterService.RegisterScenePreparation(int, LodingPreparationNode)`를 추가했다. 등록된 생성 함수는 매 씬 진입의 새 Pending 작업을 돌려준다. 기존 동기 LodingNode의 Priority 순서는 바꾸지 않으며 준비 작업끼리는 등록 순서대로 실행한다.

```text
SceneEnterLoaded → AfterLoad(finally 포함)
→ 준비 Begin → Tick 반복 → 대여·구독 정리와 Complete
→ BeginReveal → 기존 진입 연출·조작
```

`ILodingPreparation`은 State, Begin(bool isCovered), Tick(float), Cancel()만 공개한다. 준비 없는 씬은 기존 동작을 유지한다. 표현 없는 로딩도 준비 완료를 기다리며 작업은 직접 시각 노출을 막아야 한다. `LoadingProgress`는 씬 로드 진행률이므로 준비 중 1이고, `IsLoading`은 준비와 Reveal 동안 true다.

Cancelled/Failed 또는 예외는 로딩 성공으로 바꾸지 않는다. Abort/Shutdown은 현재 작업을 취소하고, 기능 소유자도 Clear/Detach/Release에서 같은 작업을 취소할 수 있다. Cancel은 중복 호출에 안전해야 한다. 작업은 `IsLoading == false`를 기다리면 안 된다. Global은 Combat 구현을 참조하지 않으며 Factory나 서비스 등록 계층의 재배치는 없다.

## 5. 계측과 Unity 인수

| ProfilerMarker | 읽는 방법 |
|---|---|
| `Combat.Units.PreparationTick`, `PrepareSelected` | 로딩 중 개체별 준비 비용. 모든 마커는 같은 동기 호출 안에서 시작·종료한다. |
| `Combat.Units.CreateModel`, `CreateHUD`, `CreateHealthLabel`, `CreateClipChannel` | 실제 자원 생성에만 표시한다. 준비 수량 내 첫 배치와 복귀 후 재배치에 새 생성이 없는지 확인한다. |
| `Combat.Units.ApplyHUD` | 최신 사실 적용 비용. 준비 후에도 남을 수 있는 갱신 비용이다. |
| `Combat.Units.PreparedCapacityMiss`, `CreateOutsidePreparedCapacity` | 준비 성공 뒤 추가 모델 생성의 진단 구분. 준비 중 생성은 아직 준비 수량이 확정되지 않아 Outside에도 기록된다. 종류·선택 수량·실제 대여와 함께 읽는다. |
| `Combat.Result.PresentEvent`, `Combat.Result.Finalize` | 입력 다음 프레임의 사건 표시와 최종 결과 갱신을 구분한다. |

사용자 Unity 확인 순서:

1. 변경 코드를 컴파일하고 Console 오류를 확인한다. 컴파일·재생의 확인 주체는 사용자다. Test 코드와 MSBuild는 추가·실행하지 않았다.
2. Combat 진입에서 로딩 애니메이션 진행, 모델·HUD 노출 없음, 준비 중 조작 불가, Reveal 후 기존 보드/Nexus/적 연출과 입력 허용 순서를 확인한다.
3. 메인·선택 서브·동일 종류 여러 개체·초기 손패 밖 선택을 배치한다. 최신 체력/실드/아이템/상태 표시, 선택·호버, 이동·회수·재배치를 확인한다.
4. 보드 연출 없음, 타일 없음, 같은 씬 재진입, 일반 씬 왕복, 로딩 표현 없음, 중간 오류·취소에서도 대기나 자원 누적이 없는지 확인한다. Lobby·Stage 전환도 포함한다.
5. 계획의 동일 조건 비교 절차로 첫 배치 생성 횟수와 CPU/JIT/GC.Alloc, 로딩 추가 시간·최장 프레임·메모리 증가를 기록한다. 준비 수량 밖 생성과 남은 Core/JIT·렌더·VFX 비용은 구분한다.

2026-11-03 computer-use로 실행을 시도했지만 Unity가 수정 전 `ProjectL.Systems.dll`을 사용하고 있음을 확인했다. 그 실행은 변경 코드의 재생 확인이나 개선 성능 캡처로 사용하지 않았다. 재생을 종료하고 Profiler 기록을 껐다. **유효한 변경 후 성능 수치는 없으며, P4의 실행 인수 항목은 미완료로 유지한다.**

정적 확인은 변경 diff·호출/수명 경로·파일 형식과 `.meta`/문서 링크 대조에 한정한다. 커밋·push는 하지 않았다.
