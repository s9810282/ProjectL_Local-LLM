# Combat 선택 유닛과 HUD 사전 준비 설계

작성일: 2026-11-02  
작성: Codex  
갱신일: 2026-11-03

상태: P1~P4 코드 반영과 정적 검토 완료. 컴파일과 Unity 기능·성능 인수 대기.

선택한 출전 유닛의 모델과 HUD를 Combat 로딩 중 준비하여, 마우스를 놓아 배치를 확정하는 프레임의 최초 생성·초기화 부담을 줄인다. 준비 완료는 객체 생성뿐 아니라 최초 활성화와 UI 갱신까지 포함한다. 실행 순서는 [Task 계획](../plans/2026-11-02-combat-selected-unit-prewarm-plan.md)을 따른다.

## 1 사용자 요구와 범위

사용자가 요청한 네 단계를 구현 기준으로 삼는다.

1. 선택한 출전 유닛 목록과 수량으로 준비 대상을 정한다.
2. Combat 로딩 중 실제 HUD Canvas에 연결하여 모델·애니메이션·HUD를 생성하고 공개된 표시 데이터를 적용한다.
3. 로딩 화면 뒤에서 활성화하고 첫 UI 갱신이 끝날 때까지 기다린다.
4. 준비한 객체를 유지하고 배치 확정 때 재사용하여 위치와 최신 상태를 적용한다.

여기서 입장 전은 **Combat 씬의 Canvas와 카메라가 연결된 뒤, 로딩 화면이 걷히고 조작이 허용되기 전**이다. 이전 씬에서 객체를 만들고 씬 사이로 옮기는 방식은 도입하지 않는다. 준비는 메인 스레드에서 여러 프레임으로 나눈다.

포함: 선택 덱 읽기, 유닛 표시의 기존 풀 예열, HUD 최초 갱신, 로딩 완료 대기, 배치 재사용, 취소·해제, 성능 계측.

제외: Core 규칙·배치 결과 변경, 가짜 배치 명령 제출, FieldCamera·Architecture 수정, 전체 HUD 구조 개편, 전투 결과의 전체 갱신 제거, 모든 VFX·셰이더 변형 예열, 전역 영구 풀, 성능과 무관한 UI 수정.

## 2 측정 근거와 현재 코드

### 측정 기록

2026-11-02 Unity Editor Play Mode에서 Deep Profile 없이 측정했다. 아래 값은 대표 프레임이며 평균이나 실제 빌드 성능이 아니다. 당시 메모리 부족 경고가 있었으므로 개선 전후 비교에서는 측정 조건과 메모리 상태도 기록한다.

| 캡처 | 프레임 | CPU 시간 | 확인한 구간 |
|---|---:|---:|---|
| [첫 배치](../../../Logs/unit-placement-first-2026-11-02.data) | 6399 | 348.23ms | GameBootstrap.Update 325.97ms, 그 아래 Mono.JIT 251.79ms, Instantiate 5.82ms |
| [두 번째 새 유닛 배치](../../../Logs/unit-placement-second-2026-11-02.data) | 9093 | 101.31ms | JIT 15.98ms, Instantiate 14.50ms, 카메라 렌더링 CPU 구간 43.29ms |
| [기존 유닛 이동](../../../Logs/unit-placement-move-2026-11-02.data) | 838 / 839 | 22.67 / 18.00ms | 입력 뒤 결과 반영 프레임의 GameBootstrap.Update 3.34ms |

첫 배치의 주요 관측 비용은 JIT다. 위 JIT 전체를 HUD 또는 View 코드에 귀속할 근거는 아직 없고, 두 번째 배치의 렌더링 CPU 시간도 셰이더 컴파일이나 GPU 시간으로 단정하지 않는다. 새 두 유닛의 종류도 다르다. GC.Alloc은 할당량이며 GC 정지 시간의 증거가 아니다. Logs의 캡처는 로컬 산출물이다.

### 코드상 연결 지점

경로의 `Combat/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`다.

| 현재 위치 | 현재 동작과 계획상 의미 |
|---|---|
| `Combat/Display/Unit/FieldUnitPresenter.cs` | 종류별 CustomPool의 초기 개수는 0. Rent에서 필요 시 모델 생성·이동 외형·클립 채널을 준비하고, RenderFacts에서 HUD를 확보한다. |
| `Combat/Display/Unit/FieldUnitView.cs` | 체력 텍스트·HUD를 소유하며 ResetForPool은 표시 상태를 정리하고 비활성화한다. Clear 시 클립 채널을 해제하므로 다음 준비 때 재생성 여부도 확인해야 한다. |
| `Combat/Display/Unit/EntityViewSet.cs` | 실제 EntityId와 View 대여의 원본. 화면에 없는 대상을 End에서 반환한다. 사전 준비 객체를 가짜 필드 개체로 넣지 않는다. |
| `Combat/Display/Unit/UnitHUDLayer.cs` | 공용 Overlay Canvas의 HUD 등록·투영·CanvasGroup 가시성을 소유한다. |
| `GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs` | UIWidgetBase 기반. 소유자 가시성은 Open/Hide를 통하며, 기본 구현은 GameObject 활성 상태를 바꾼다. 데이터 설정은 하위 아이콘 활성화와 레이아웃 작업도 수행한다. |
| `Combat/CombatViewSystem.cs` | 로딩 Enter 노드에서 Install 후 입력을 생성한다. 첫 표시 성공과 진입 연출 대기는 이미 별도로 존재한다. |
| `GlobalSystem/LoadingSystem/LodingNode.cs` | Action 기반 동기 작업이다. 현재 계약만으로 여러 프레임의 준비 완료를 기다릴 수 없다. |
| `GlobalSystem/LoadingSystem/LodingSystem.cs` | SceneEnterLoaded와 AfterLoad 실행 직후 BeginReveal을 호출한다. AfterLoad에는 카메라 연결도 있어 기존 순서를 보존해야 한다. |

Unity UI 활성화에는 콜백, UI 등록, 메시·레이아웃 갱신, Canvas 배치 재구성이 포함될 수 있다. Canvas 사각형의 빈 영역 전체를 렌더링한다는 뜻은 아니다. 갱신은 프레임 끝의 렌더 직전에 수행되므로 같은 프레임에서 켜고 끄는 것으로 완료 처리하지 않는다. [Unity UI 최적화](https://unity.com/how-to/unity-ui-optimization-tips), [Canvas 갱신 시점](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html).

## 3 준비 대상과 데이터 원본

### P1 확정 선택을 원본으로 사용

- 대상 원본은 `IRunDeckReadSink.TryGetCurrent()`의 `RunDeckSnapshot.OrderedIds`다. 전체 Owned 목록이나 현재 손패만을 대상으로 삼지 않는다.
- `ICharacterBufferReadSink.TryGetOwned`로 각 선택 ID의 EntityKey를 읽는다. 동일 종류의 서로 다른 개체는 선택 개수만큼 준비하며 종류가 같다는 이유로 1개로 축약하지 않는다.
- 현재 CharacterData는 OwnedId로 대응시킨다. 이미 필드에 표시된 선택 유닛은 기존 대여를 준비 수량에 포함해 중복 생성하지 않는다.
- 준비 목록은 해당 씬 연결·전투 번호·RunId·DeckRevision에 묶인 일회성 작업 입력이다. 새로운 선택 상태나 규칙 원본이 아니다. 준비가 끝나면 불필요한 입력 참조를 놓는다.
- 필수 선택·보유 기록·외형 참조가 없거나 서로 맞지 않으면 대상 ID와 원인을 포함한 예외로 보고한다. 조용히 건너뛰어 준비 완료로 표시하지 않는다.

### P2 아직 드로우되지 않은 유닛

현재 Core는 메인과 초기 지급된 서브만 CharacterCombatFacts로 설치한다. 선택 덱의 나머지 유닛은 드로우 뒤 전투 개체가 생긴다. 따라서 모든 선택 유닛에 전투 수치가 이미 있다고 가정하면 안 된다.

- 전체 선택 유닛의 모델·클립 채널·체력 텍스트·HUD 및 View DB의 이름 등 공통 표현 자원은 먼저 준비한다.
- CharacterData가 이미 있는 유닛은 그 사실로 실제 체력·아이콘·아이템·상태를 적용한다.
- 아직 전투 사실이 없는 유닛은 준비 전용 API로 공통 위젯·텍스트 생성 경로를 초기화한다. 임시 EntityId나 가짜 전투 수치를 만들어 정상 SetUnit 경로에 넣지 않는다. 상태·전투 수치는 게시된 이후 적용한다.
- 준비 대상에 없는 동적 상태 아이콘, 전투 중 소환·변신·추가 개체는 기존 생성 경로를 유지한다. 최초 배치 시 생성 0의 보장은 사전 준비한 자원과 수량에 한정한다.

## 4 생성과 풀 수명

### P3 기존 소유자와 생성 경로 재사용

`FieldUnitPresenter`가 모델·HUD·클립 자원과 기존 종류별 풀을 계속 소유한다. 별도의 영구 캐시, 두 번째 풀, 사전 준비용 가짜 EntityViewSet을 만들지 않는다.

준비는 정상 대여와 같은 생성·초기화 경로를 사용한다. 그 경로에 HUD 확보를 포함하는 명시적 준비 입구를 두되, 공용 CustomPool 안에 유닛 초기화 로직을 넣지 않는다. HUD는 최종 사용할 UnitHUDLayer 아래에 생성하고, 배치 때 부모를 옮기거나 다시 생성하지 않는다.

같은 종류 N개를 준비할 때는 N개의 서로 다른 객체를 동시에 확보한 뒤 준비한다. Get과 Return을 즉시 반복하여 같은 객체 하나만 N번 준비하는 실수를 금지한다. 생성·활성화는 기본적으로 한 Tick에 한 개체씩 진행하며, 한 Instantiate 자체를 중단할 수 있다는 전제는 두지 않는다.

### P4 표시 준비와 정상 대여 분리

준비 중인 View는 필드의 정상 대여로 공개하지 않는다. 선택·호버·픽킹·점유 칸·전투 사실에 영향을 주지 않고, 애니메이션 이벤트로 전투·오디오·VFX 동작을 발생시키지 않는다. 클립 채널 생성까지 포함하되 모든 공격 클립 재생이나 모든 셰이더의 GPU 예열까지 보장하지 않는다.

준비 완료 뒤 임시 바인딩·미리보기·선택·애니메이션 상태를 정리하고 기존 풀에 정확히 한 번 반환한다. 정상 배치에서는 기존 Acquire → Bind → Apply 흐름으로 최신 Buffer 값을 다시 적용한다. 준비 때의 체력을 실제 배치 상태로 보관해 쓰지 않는다.

기존 ResetForPool과 HUD Open/Hide 계약은 유지한다. 첫 예열 뒤에도 재활성화와 데이터 변경으로 갱신 비용은 남을 수 있다. 이번 단계에서 모든 HUD를 영구 활성화하거나 UIBase 전체의 숨김 의미를 변경하지 않는다. 반복 활성화가 잔여 병목이면 별도 측정 근거로 다음 설계를 만든다.

## 5 첫 활성화와 UI 완료 조건

### P5 로딩 화면 뒤에서 실제 갱신

1. 실제 Canvas·출력 카메라·소유 UI 연결이 끝난 뒤 준비를 시작한다.
2. 준비할 HUD에 데이터를 먼저 적용하고, 그 데이터가 그려질 위젯을 활성화한다. 평상시에 닫혀 있는 독립 정보창·툴팁을 UI 이력에 몰래 열어 예열하지 않는다.
3. UnitHUDLayer가 관리하는 준비용 표시 범위에서 화면 안의 유효한 Rect와 렌더 가능한 상태를 사용한다. 로딩 화면은 완전히 가린 상태를 유지하고 모든 준비 HUD의 입력·레이캐스트는 막는다.
4. 데이터 적용 이후 실제 Canvas 갱신 주기를 지나고, 다음 Tick에서 완료를 확인한다. 고정 초 단위 대기나 첫 preWillRenderCanvases 콜백만으로 완료를 판정하지 않는다. 관련 Canvas/TMP 갱신 순서를 확인하고 필요한 완료 추적은 해제 가능한 수명으로 둔다.
5. 준비 표시를 정리하고 풀에 반환한 뒤에만 로딩 완료를 알린다. 모든 유닛이 끝나기 전에는 BeginReveal을 호출하지 않는다.

비활성 부모 아래에서 자식 SetActive(true)만 호출한 상태, 화면 밖으로 보내 갱신이 생략된 상태, alpha 0만 설정한 상태를 최초 UI 생성 완료의 증거로 삼지 않는다. `ForceUpdateCanvases` 또는 TMP 강제 갱신을 사용할 경우에도 그 호출이 Start·실제 렌더링·GPU 준비까지 완료시킨다고 간주하지 않는다. 강제 갱신을 정상 배치 경로에 넣지 않는다.

로딩 표현이 없는 개발 경로에서도 입력 완료 조건은 유지한다. 이 경우 HUD의 명시적 메시·레이아웃 준비로 시각 노출을 막으며, 실제 렌더까지 예열한 운영 경로와 측정 결과를 구분한다.

## 6 로딩과 진입 연출 연결

### P6 로딩 준비 완료 계약 추가

실행 순서는 다음과 같다.

```text
씬 로드
→ 기존 SceneEnterLoaded 실행과 Combat Install
→ 기존 AfterLoad 실행과 카메라 연결
→ 선택 유닛 준비 작업을 여러 Tick에 걸쳐 실행
→ 준비 HUD 정리와 풀 반환 완료
→ 로딩 BeginReveal
→ 기존 보드와 Nexus와 적 등장 연출
→ 기존 조건에 따라 조작 허용
```

현재 AfterLoad의 호출 순서와 실패 시 finally 실행은 유지한다. 준비 완료 대기는 AfterLoad 이후, BeginReveal 이전에 추가한다. 로딩 표현이 없는 경우에도 준비 완료 뒤 Idle로 전환한다. 준비 작업이 없는 다른 씬의 기존 경로는 그대로 유지한다.

LoadingSystem에는 씬별 준비 작업의 생성·Tick·완료·취소를 다루는 좁은 계약을 추가한다. 후보 이름은 `ILodingPreparation`과 `RegisterScenePreparation`이며 기존 철자 Loding을 유지한다. 등록은 초기 서비스 연결 시, 작업 객체는 각 씬 진입 때 만든다. LoadingSystem은 이 계약만 알며 Combat 타입을 참조하지 않는다. 작업 목록의 진행과 IsLoading은 LoadingSystem 한 곳이 소유한다.

준비 부품은 기존 SceneDisplay/Factory에서 조립하고 현재 연결에 귀속시킨다. `CombatViewSystem`에는 연결·취소 전달과 준비 상태 조회만 둔다. 준비 자체를 Bootstrap 또는 보드 연출 코드에 넣지 않는다. 새 독립 전역 System·Service는 만들지 않는다.

입력과 골격이 읽는 기존 진입 대기 조건에는 같은 준비 작업의 Pending을 포함한다. 첫 Install 성공인 Shown을 사전 준비 완료로 재해석하지 않는다. 로딩 준비 작업은 LoadingSystem이 Tick하므로, `IsLoading == false`를 기다리는 보드 연출과 상호 대기하지 않는다. 진입 연출이 꺼져 있거나 대상 타일이 없는 경우에도 준비 중 입력이 먼저 열리면 안 된다.

공용 변경 범위는 LoadingSystem의 최소 계약·상태 전이와 필요한 조립 연결이다. 구현 때 기존 계약 문서와 공유 메모에 갱신하고 외부 발송은 하지 않는다. FieldCamera나 Architecture 수정이 필요한 것으로 판명되면 이 범위로 임의 확대하지 않는다.

## 7 실패와 해제

준비 상태는 연결당 한 객체가 소유하고 `Pending → Complete`, `Pending → Cancelled/Failed`로 끝낸다. 여러 위치에 완료 bool을 복제하지 않는다.

- Clear·Detach·로딩 오류·종료에서는 준비를 먼저 취소하고, 보유 중인 대여·표시 복원·Canvas 콜백을 정리한 뒤 기존 씬 자원을 해제한다.
- 부분 생성 실패 시 이미 생성한 자원과 남은 대여를 모두 정리한다. 오류는 기존 RuntimeFaultBoundary로 전달한다. 실패를 정상 Complete로 바꾸지 않는다.
- Loading Abort와 Detach가 중복 호출해도 취소·반환은 한 번이며, 하나의 정리 실패 때문에 나머지 자원을 남기지 않는다.
- 지난 연결의 완료 콜백은 새 연결의 로딩이나 입력을 열 수 없다. 연결 객체와 전투 번호를 대조한다.
- 준비되지 않은 종류 또는 추가 수량은 정상 풀의 생성 경로를 사용하고, 예상했던 준비 수량 부족과 구분하여 계측한다.
- 일반 풀 반환, 전투 Clear, 최종 씬 Release의 서로 다른 해제 범위를 유지한다. Clear 뒤 재준비에서는 해제된 클립·재질과 HUD 연결의 유효성을 다시 확인한다.

2026-11-03 구현 상세: 준비 반환은 기존 풀과 클립·HUD를 보존한다. Clear는 준비를 먼저 취소하고 기존 클립·재질 해제 범위를 유지한다. 최종 Display.Release는 HUD뿐 아니라 모델 풀도 종료한다. 같은 씬을 다시 연결할 때 이전 연결에서 해제된 자원을 가진 여분 객체가 새 준비 객체보다 먼저 대여되는 것을 막는다. 준비는 연결당 한 번이며 새 연결에서 다시 만든다.

## 8 구조 검토

기준은 [설계 원칙과 기능 독립성](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)이다.

| 기준 | 적용 |
|---|---|
| SSOT | 선택은 RunDeck, 수치는 Character Buffer, 실제 대여는 EntityViewSet, 자원은 기존 풀, 로딩은 LoadingSystem, 준비 완료는 준비 작업 한 곳. |
| S | 대상 읽기, 자원 준비, HUD 갱신, 로딩 진행, Factory 조립을 각각 기존 책임에 둔다. |
| O | 씬별 준비 계약으로 확장한다. LoadingSystem에 Combat 분기나 유닛 개수를 넣지 않는다. |
| L | 동기 LodingNode와 준비 작업 없는 씬의 의미, UI Open/Hide, Pool 반환 순서를 유지한다. Complete는 반환까지 끝난 성공만 의미한다. |
| I | 준비 부품에는 선택·캐릭터 읽기와 표시 준비에 필요한 좁은 입구만 준다. 전체 BufferQuery나 모든 Global 서비스를 보관하지 않는다. |
| D | Global은 준비 계약에 의존하고, Factory가 Combat 구현을 연결한다. View는 InGame DB와 Core 호출로 값을 계산하지 않는다. |
| FW | FW-001~003의 기존 등록·모듈·Builder 수명을 유지하고, FW-004의 내부 상태 우회 금지, FW-008~009 UI 소유와 이력, FW-013 구독 해제, FW-014 풀 콜백, FW-016 초기화 순서를 따른다. |
| JMH | §16~18의 사용량 기반 예열과 풀 수명, §20~21의 구독 해제, §27~28의 필요한 만큼의 추상화를 적용한다. |

HUD의 레이아웃 초기화가 바뀌면 HUD 준비 부분과 해당 검증만 변경한다. 선택 수량 정책의 소비 방식이 바뀌면 대상 읽기 부분만 변경한다. 새 로딩 참여 기능은 기존 계약으로 등록한다. 로딩 완료 계약 자체를 변경하는 이번 작업에서는 Lobby·Stage 전환도 회귀 확인해야 하며, 이 공통 영향은 감추지 않는다.

## 9 성능 인수 기준

선택한 유닛의 첫 배치 때 모델·HUD·체력 텍스트·클립 채널의 신규 생성이 발생하지 않는지, 첫 활성화 비용이 로딩 준비 구간으로 이동했는지 확인한다. 전체 배치 시간을 특정 수치로 낮춘다고 미리 보장하지 않는다. 측정 후 남는 Core JIT·VFX·렌더링 비용은 별도 원인으로 기록한다.

최대 선택 수량의 준비 시간, 준비 중 최장 프레임, 메모리 증가, 대기 HUD의 상시 비용, 재진입 후 자원 수를 함께 비교한다. 불필요하게 모든 보유 유닛을 준비해 로딩 시간과 메모리를 늘리지 않는다. 세부 절차와 Phase별 중단 지점은 Task 계획을 따른다.

## 10 구현과 검토 기록

2026-11-03 사용자가 P4까지 연속 구현 후 검토를 지시했다. 개별 Phase 중단 없이 계획 범위를 연결했으며, [구현 검토와 로딩 계약 공유](../handoffs/2026-11-03-combat-unit-prewarm-review-handoff.md)에 구조 검토·계측 구간·Unity 인수 항목을 기록했다. 컴파일 미확인. 변경 전 어셈블리로 실행된 확인은 개선 전후 측정에 사용하지 않는다.
