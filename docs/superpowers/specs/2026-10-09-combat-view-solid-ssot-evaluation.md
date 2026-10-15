# 전투 View SOLID와 SSOT 구조 평가

현재 전투 View는 상태 소유권과 입력·연출 책임의 분리가 잘 잡혀 있다. 전면 재설계보다 타임라인 경계, 조정자에 모인 UI 책임, UI 입력 규칙과의 차이를 순서대로 개선하는 편이 적절하다.

- **작성일:** 2026-10-09
- **기준 코드:** `2b25f64a`. 이전 상세 UML의 기준 `88c845d0` 이후 격자 아웃라인과 이동 UX 변경을 포함한다.
- **대상:** 전투 View 담당자. `ViewSystem/Combat`을 중심으로 연결된 Buffer·골격·UI·Factory 경계와 관련 테스트를 확인한다. Core 내부 계산과 다른 작업자 영역의 전체 평가는 포함하지 않는다.
- **검증 범위:** 코드와 테스트를 읽은 정적 평가다. Unity 실행·EditMode 테스트 실행·프로파일링 결과를 추가한 것은 아니다. 성능 항목은 호출·순회·할당 경로에 대한 판단이다.
- **상태:** 평가와 개선 권고. 구현 Task나 Phase 착수 승인은 아니다. 구현은 별도 설계 → Task 계획 → Phase별 리뷰를 따른다.
- **후속(2026-10-09):** 개선은 [개선 설계](2026-10-09-combat-view-solid-improvement-design.md)와 View 계획 §7.68에서 Phase P1~P5로 진행한다.

## 1 평가 기준과 기존 문서의 관계

프로젝트 적용 기준은 [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [JMH Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md), [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [Assets UI 입력 규칙](../../../Assets/AGENTS.md)이다.

[10월 9일 골격·View 평가](2026-10-09-combat-skeleton-view-evaluation.md)는 `88c845d0`의 골격과 View를 함께 다룬다. 이 문서는 현재 View를 SOLID·SSOT와 일반적인 구조 품질 기준으로 평가한 후속 문서다. [상세 UML](2026-10-09-combat-skeleton-view-detailed-uml.md)은 실행 흐름을 이해하는 자료로 사용하되, 이후 추가된 필드 표시 구조까지 반영한 최신 UML로 간주하지 않는다.

판정에는 다음 원칙을 적용한다.

- 파일 길이와 의존성 개수는 조사 신호다. SRP는 **서로 다른 변경 이유가 한 객체에 모이는지**로 판단한다.
- `switch`의 존재만으로 OCP 위반이라고 판단하지 않는다. 확장할 때 수정해야 하는 범위와 빈도를 본다.
- 인터페이스 사용 여부와 계층 경계 준수 여부를 구분한다. 인터페이스를 통해 호출해도 프로젝트의 데이터 흐름을 벗어날 수 있다.
- 표현용 스냅샷·캐시와 게임 상태의 원본을 구분한다. 캐시의 수명·무효화·쓰기 소유권이 명확한지 확인한다.
- 추상화는 실제 변경과 검증을 돕는 만큼만 추가한다. JMH 지침 §27의 과도한 추상화 금지를 함께 적용한다.

## 2 SOLID 5원칙 평가

| 원칙 | 판정 | 근거와 개선 방향 |
|---|---|---|
| SRP 단일 책임 | 대체로 양호, 조정자 주의 | Router는 입력 분배, Gate는 제출·대기, 배치·공격 객체는 각 상호작용, Playback은 재생 수명을 담당한다. `CombatViewSystem`에는 씬 수명·결과 후처리·포기 확인창·개발 패널·타임라인 UI 제어가 함께 있다. |
| OCP 개방·폐쇄 | 표시 확장 양호, 입력·사건 확장 보통 | 필드 표시는 `IFieldHighlightRenderer`로 구현을 교체한다. 새로운 입력이나 사건 종류는 Router·Policy·사건 Presenter의 분기를 수정해야 한다. |
| LSP 리스코프 치환 | 계약 설계 양호, 검증 보강 가능 | 연출 완료·실패·취소, 표시기 Release·재구성 계약이 명시돼 있다. 늦은 콜백·중복 완료·재진입을 다루는 코드와 테스트가 있다. 모든 구현의 행동 계약을 검증 완료했다고 판단하지는 않는다. |
| ISP 인터페이스 분리 | 대체로 양호 | ReadSink/WriteSink, PointerMapping, MovementFeedback처럼 소비자별 계약이 있다. `ICombatInteractionFeedback`에는 배치 고스트와 공격 범위·비용 표시가 함께 노출된다. |
| DIP 의존성 역전 | 대체로 양호, 계층 경계 예외 | Buffer·입력·골격 서비스는 인터페이스로 받고 연출 구현도 주입한다. View의 Core 타임라인 직접 호출은 프로젝트 데이터 흐름의 예외다. |

### 2.1 조정자에 남길 책임과 분리할 책임

[CombatViewSystem]은 현재 1,015줄이다. 문제는 줄 수보다 씬 연결 방식, 결과 처리 순서, 포기 확인창, 개발 패널, 타임라인 UI 정책이 바뀔 때 같은 클래스를 수정한다는 점이다.

`FinalizeResult`의 순서는 조정자의 핵심 책임으로 유지하는 편이 좋다. 화면 반영 → 대기 해제 → Layer 동기화 → 입력 후처리 → 명령 정리 → 결과 확인의 흐름을 한곳에서 읽을 수 있기 때문이다. 이 순서를 여러 객체로 흩뜨리면 호출 순서에 대한 의존을 이해하기 어려워진다.

분리 후보는 포기 확인창 제어와 개발 패널이다. 포기 확인창은 지역 상태와 Open·Confirm·Cancel·Close 동작을 작은 객체로 묶고, ViewSystem은 생성·동기화·정리 호출을 맡을 수 있다. 이탈 순서는 기존 골격이 계속 소유한다. 개발 패널도 실제 수정 범위에 맞춰 별도 분리를 검토한다.

[CombatSceneDisplay]는 약 500줄이지만 대부분 Presenter 위임이다. 크기만으로 추가 분해할 필요는 없다.

### 2.2 실제 확장 지점의 품질

필드 표시의 분리는 좋은 사례다.

| 구성요소 | 책임 |
|---|---|
| `FieldHighlightState` | 칸별 강조 층, 호버, 배치·경로 지정 표시 상태 |
| `FieldHighlightLayers`·Palette | 강조 우선순위와 색 매핑 |
| `IFieldHighlightRenderer` | 표시기의 구성·갱신·해제 계약 |
| `FieldCellColorRenderer`·`FieldOutlineRenderer` | 표시 방식별 자원과 렌더링 |

[FieldView]의 작은 모드 선택 `switch`는 현재 두 구현을 조립하는 자리로 타당하다. 이를 없애기 위한 별도 등록 프레임워크는 필요하지 않다.

반면 [CombatSceneEventPresenter]의 `CanPresent`는 지원 사건을 한곳에서 나열한다. 공격·피격·사망 연출이 늘면 해당 클래스가 반복적으로 수정될 수 있다. 실제 연출 종류가 늘어나는 시점에 사건별 구현 분배를 검토하되, `OrderedCombatPresentation`과 Playback의 순서·완료 확인 계약은 유지한다.

### 2.3 넓은 계약의 판단

`ICombatBattleDisplay`는 씬 표시 능력을 묶는 계약이며, 하위 소비자는 필요한 좁은 계약을 받는다. 묶음 인터페이스라는 이유만으로 ISP 위반으로 보지 않는다.

다만 [ICombatInteractionFeedback]에는 배치 Drag의 고스트·원본 숨김과 공격의 범위·영향·차단 벽·비용 표시가 함께 있다. 두 소비자의 변경이 서로에게 전파되거나 테스트 대역 구현 부담이 커질 때, 공통 호버와 배치·공격 피드백을 소비자 기준으로 분리할 수 있다. 모든 클래스에 새 인터페이스를 추가할 필요는 없다.

## 3 SSOT와 상태 소유권

| 사실 | 현재 소유자 | 평가 |
|---|---|---|
| 전투 상태·캐릭터·국면 | Core가 게시하는 Buffer | 양호 |
| 입력 요청과 번호 | `CombatViewBuffer` | 양호 |
| 표시 완료 순번 P | `CombatViewBuffer` | 별도 영구 완료 카운터가 없어 양호 |
| 시작·턴 종료 대기 | `CombatCommandGate` | 양호 |
| 진행 중 결과의 재생 수명 | `CombatResultPlayback` | 양호 |
| 공통 입력 허용 정책 | `CombatInputPolicy` | 양호. 연결 수명·전투 정체성·context 일치는 각 입구에서 추가 확인한다 |
| 포인터 대상 선택 | `CombatInputRouter.ResolvePointerTarget` | 양호 |
| 필드 강조 상태 | `FieldHighlightState` | 양호. 상태 변경 권한의 캡슐화는 보완 가능 |
| 전투 골드·남은 드로우 표시 | `CombatDisplayValues`가 원본을 읽거나 계산 | 파생 저장값을 만들지 않아 양호 |
| 타임라인 | Core 직접 계산과 Buffer 조회 두 경로 | 의미·갱신 시점 계약 통일 필요 |

연출 중간 상태의 사건 스냅샷, 동일 표시를 생략하기 위한 캐시, 요청 번호와 연결된 공격 범위 캐시는 그 자체로 SSOT 위반이 아니다. 게임 판정의 원본을 대체하지 않고 명확한 수명과 무효화 조건을 갖는 표현 상태다. 배치 Drag와 이동 경로 지정 상태도 의미와 소유자가 다르므로 하나의 전역 모드로 합칠 이유가 없다.

[CombatDisplayValues]의 `StageGold`는 `State.StageGold`를 읽고, `RemainingDraws`는 원본 값에서 계산한다. P7과 SSOT의 좋은 적용이다.

작은 보완점은 `FieldView.HighlightState`다. 주석은 읽기 전용 사용을 요구하지만 반환 타입에는 변경 메서드가 있다. 현재 오용을 확인한 것은 아니며, 단일 소유권을 타입보다 관례에 맡긴 상태다. 필요하면 읽기 전용 조회만 노출해 변경 경로를 FieldView에 제한한다.

## 4 주요 경계와 규칙 차이

### 4.1 타임라인의 출처와 표시 시점

| 소비자 | 데이터 출처 | 갱신 경로 |
|---|---|---|
| 유닛 HUD의 턴 순서 | Core `GetTimeline()` 예측 계산 | 전투 설치·결과 표시 갱신 |
| 화면 `TimelineUI` | Buffer `CommandState.Timeline` | `MonoBehaviour.Update()` |

직접 호출은 [CombatViewSystem]의 `ReadDisplayValues` 630~636행, Buffer 소비는 [TimelineUI] 116행에 있다. Core의 `GetTimeline`은 실제로 `CombatTurnCalculator.Forecast`를 호출한다.

판정은 다음과 같다.

1. View가 규칙에서 나온 값을 Buffer로 받는다는 P6 경계를 벗어난다.
2. 두 경로의 예측 의미·범위·갱신 시점이 같다는 보장이 소비자 코드에 없다. 실제 표시 불일치가 재현됐다는 뜻은 아니다.
3. 일반 결과는 연출 완료 뒤 확정 표시하지만 Timeline UI는 최신 Buffer를 별도 Update에서 읽는다. 화면별로 어느 시점의 상태를 보여줄지 계약이 필요하다.

`ICombatProcessingService` 인터페이스를 사용하므로 직접 호출을 단순히 DIP 위반이라고 부르는 것은 부정확하다. **프로젝트 계층 경계 위반과 SSOT 일관성 위험**으로 구분한다.

VR6의 시작 조건은 Core 타임라인 게시 계약 인수다. `CommandState.Timeline`이라는 값이 있다는 사실만으로 조건이 충족됐다고 단정하지 않는다. 예측 범위·최초 게시·무효화·전투 또는 상태 버전·빈 값·종료와 재진입 동작을 대조한 뒤 소비자를 교체해야 한다. Core 계산 위치를 옮기는 것만으로 성능 향상을 보장하지도 않는다.

### 4.2 전투 버튼과 현재 UI 입력 규칙

[Assets UI 입력 규칙](../../../Assets/AGENTS.md)은 버튼 입력에 `UIInputBinding`과 `IUIInputReceiver`를 사용하도록 규정한다. 현재 다음 전투 UI는 `Button.onClick.AddListener`로 연결한다.

| 위치 | 버튼 |
|---|---|
| [CombatAbandonPanel] `Bind`, 44~46행 | 포기·확인·취소 |
| [CombatHud] `ShowCompletion`·`BindReturn`, 66·84행 | 완료 확인·복귀 |
| [CombatSceneDisplay] `ShowDevLayerPanel`, 206~207행 | 개발 Layer 전환 |

구독 해제 코드가 있으므로 곧바로 누수로 판단하지 않는다. 현재 프로젝트 규칙과 일치하지 않는 기술 부채로 기록한다.

[CombatDisplayReuseTests] 389·436행에는 `onClick.Invoke()`를 직접 호출하는 검증도 있다. 입력 연결을 정리할 때는 EventSystem 핸들러를 통해 클릭·키보드 또는 게임패드 Submit 전달, 비활성·재연결 동작을 확인해야 한다.

### 4.3 별도 UI Update와 오류 경계

전투 조정자의 Tick·입력·버튼 콜백은 `RuntimeFaultBoundary`로 오류를 모은다. [TimelineUI]와 [CombatLogUI]의 자체 Update는 이 경계 밖이다. 해당 UI에서 예외가 나도 같은 경계의 입력 정지와 런타임 Tick 중단이 자동으로 적용되지는 않는다.

표시 시점과 오류 처리 책임을 UI 담당자와 함께 정리할 필요가 있다. Buffer를 직접 읽는 것 자체는 읽기 경계 위반이 아니다.

성능도 두 UI를 구분한다. Timeline은 매 Update에 표시를 갱신하지만, CombatLog는 `_readCount == results.Count`이면 반환한다. 두 화면이 모두 매 프레임 전체 내용을 다시 만든다고 평가하지 않는다.

### 4.4 새 필드 표시기의 해제 실패 처리

[FieldOutlineRenderer]의 `ReleaseHandles`는 핸들을 순서대로 Dispose한다. 하나의 Dispose가 예외를 던지면 이후 핸들의 반환과 `Release`의 남은 정리가 중단될 수 있다. 여러 정리 실패를 모으면서 나머지도 시도하는 기존 연출 정리 코드와 수준이 다르다.

이는 예외 경로의 구조적 위험이며, 실제 자원 누수를 재현한 결과는 아니다. [FieldOutlineRendererTests]에는 정상 해제 시 모든 핸들이 반환되는 검증이 있다. 보완 시에는 중간 Dispose 실패를 주입해 나머지 반환과 상태 정리가 이어지는지를 확인하는 것이 적절하다. 필요한 수정은 우선 View 소유 표시기 범위에서 검토한다.

## 5 기타 구조 품질

| 요소 | 판정과 근거 |
|---|---|
| 응집도·결합도 | 입력 소유자와 Presenter의 응집도는 좋다. 변경 영향은 `CombatViewSystem`에 가장 집중된다. |
| DRY 지식 중복 방지 | 공통 입력 정책과 포인터 대상 판정이 통합됐다. 이동·복귀는 Core context 검증이 필요해 별도 제출 경로를 사용한다. 경로가 둘이라는 이유만으로 합치지 않는다. |
| 캡슐화 | Buffer의 읽기·쓰기 계약 분리는 좋다. 가변 강조 상태 노출과 넓은 상호작용 피드백 계약은 보완 여지가 있다. |
| 상태와 시간 순서 | 결과 순번·재생 토큰·후처리 순서가 명확하다. 별도 Update UI와의 표시 시점 계약은 보강 대상이다. |
| 수명·자원 관리 | Attach/Detach, 세대 무효화, 입력 해제, 풀·핸들 반환이 명시돼 있다. 새 아웃라인 표시기의 해제 예외 경로는 §4.4를 따른다. |
| 오류 처리 | 전투 View의 주요 진입점은 공통 오류 경계에 연결된다. 별도 UI Update는 §4.3의 예외다. |
| 테스트 가능성 | Policy·Playback·Refresh·강조 상태를 씬 없이 검증할 수 있다. 표시 인터페이스를 테스트 대역으로 교체할 수 있다. |
| KISS·YAGNI | 현재 분리는 실제 책임을 따라 만들어져 대체로 적절하다. 이동 입력은 경로 수명과 초안 편집이 이미 나뉘므로 추가 분해보다 상태 전이와 검증 유지가 우선이다. |
| 가독성·문서 일치 | 한국어 계약 주석과 설계 참조가 많다. 다만 `CombatViewSystem`의 옛 Interaction 책임 설명, `OrderedCombatPresentation`의 한 줄 다중 문장 등은 현재 구조·형식과 맞출 여지가 있다. |
| JMH·FW 계약 | 확인한 조립 경로에서 서비스·모듈 등록, 한 번의 Build, 수명 단계, 로드 시 한 번의 씬 탐색을 따른다. 전체 FW-001~016 구현을 모두 감사한 결과는 아니다. |

### 5.1 테스트 근거와 한계

[CombatPresentationCleanupTests]는 지연된 사건 콜백을 취소 뒤 무효화하는지, 정리 실패 뒤 다른 정리를 계속 시도하는지, 재진입 취소가 반복 호출로 번지지 않는지를 다룬다.

[CombatViewSystemTests]는 종료로 새 제출을 막아도 남은 결과를 끝까지 소비하는지, 연출 실패 시 P를 올리지 않는지, 씬 해제 뒤 완료 통지가 무효가 되는지를 다룬다. 단순 호출 여부보다 실제 구조 계약을 검증하는 테스트라는 점이 강점이다.

다만 `CombatViewSystem.BindServices` 테스트 입구는 `_combatQueries`와 `_ui`를 받지 않는다. 이 값들은 `PostBindService`에서 별도로 설정되고, 관련 호출에는 null일 때 생략하는 경로가 있다. 해당 테스트 입구를 사용하는 검증만으로 운영 타임라인 조회·UI 연결까지 검증했다고 간주하면 안 된다.

### 5.2 성능은 측정 후보만 제시

| 경로 | 구조상 관찰 | 후속 확인 |
|---|---|---|
| `CombatSceneEventPresenter.RenderEvents` | 사건 표시마다 유닛·구조물 사실을 다시 전달한다 | 사건 수와 개체 수에 따른 실제 비용 |
| `ReadDisplayValues` | 설치·결과 갱신에서 Core 타임라인 예측을 요청한다 | 게시 계약 변경 전후 계산 횟수·시간·할당 |
| `TimelineUI.Update` | 변경 버전과 별개로 표시 갱신을 호출한다 | 변경 없는 프레임의 UI 처리 비용 |
| `FieldOutlineRenderer.Refresh` | 층별 칸 순회 후 표시 캐시로 동일 메시 갱신을 생략한다 | 보드 크기·포인터 빈도에 따른 순회와 배열 생성 비용 |
| `CombatPointerMapper` | 경로 포인터 매핑에서 UI Raycast를 수행한다 | UI 규모별 호출 횟수와 CPU 비용 |

풀·표시 캐시·변경 칸 추적은 긍정적이다. 위 항목을 실제 병목 순위로 해석하거나 측정 없이 증분 렌더링으로 교체하지 않는다.

## 6 개선 우선순위

다음은 제안 순서다. 외부 계약 대기는 독립적인 View 작업을 막지 않는다.

| 순위 | 개선 항목 | 범위와 시작 조건 | 완료 판단 |
|---|---|---|---|
| 1 | 타임라인 게시 계약과 표시 시점 통일 | Core 계약 확인·인수 후 VR6. Timeline UI 담당자와 소비 시점 합의 | HUD·Timeline이 합의한 원본과 시점을 사용하고 View의 Core 직접 조회 제거. 종료·재진입·빈 값·예측 범위 검증 |
| 2 | 전투 버튼 입력을 현재 UI 규칙에 맞추기 | 전투 View 버튼 연결·관련 테스트. 공용 UI 계약은 기존 것을 사용 | 클릭·Submit·비활성·재연결 동작을 EventSystem 전달로 검증 |
| 3 | 포기 UI와 개발 패널 책임 분리 | 별도 설계·계획 후 View 내부 구현. 결과 후처리 순서는 조정자에 유지 | 확인창 수명, 입력 차단, 완료와 포기의 경합, Detach·오류 정리 동작 보존 |
| 4 | 표시기 해제 실패 처리와 상태 접근 범위 보완 | View 소유 필드 표시기·강조 상태 경계 | 일부 해제 실패에도 나머지 정리 시도. 변경 경로와 읽기 소비자 구분 |
| 5 | 사건별 연출 분배와 측정 기반 최적화 | 실제 연출 종류 증가 또는 측정 결과가 필요성을 보일 때 | 기존 완료·취소·P 확인 계약 유지. 최적화 전후 동작·측정 비교 |

Core 수정이 필요하면 View가 직접 구현하지 않고 [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 요청을 정리한다. 이 평가만으로 Core 요청 작성·전달이나 다른 작업자 파일 변경이 완료된 것은 아니다.

## 7 코드 근거

행 번호는 기준 커밋 `2b25f64a`에 대한 위치이며 이후 수정 시 달라질 수 있다.

| 파일 | 확인 위치 |
|---|---|
| [CombatViewSystem] | 157~212 서비스 연결, 327~334 서비스·모듈 등록, 508~522 씬 탐색, 575~607 결과 후처리, 610~636 타임라인, 890~1011 포기·개발 패널 |
| [CombatInputPolicy] | `CanSubmit`, `CanPreview`, `CanEditPath` |
| [CombatResultPlayback] | `Begin`, `TryBeginFinalization`, `Abort`, `OnFinished` |
| [CombatSceneDisplay] | `Refresh`, `Release`, `ShowDevLayerPanel` |
| [CombatSceneEventPresenter] | 100~132 사건 지원·표시·전체 사실 전달 |
| [FieldView] | 44~55 상태 소유·노출, 218~239 표시기 선택 |
| [FieldOutlineRenderer] | `Refresh`, `Apply`, 153~167 해제, 242행 `ReleaseHandles` |
| [TimelineUI] | 87행 Update, 90행 `RefreshTimeline`, 116행 Buffer 타임라인 소비 |
| [CombatLogUI] | 144행 Update, 158행 변경 없는 기록의 반환 |

[CombatViewSystem]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs
[CombatSceneDisplay]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs
[CombatInputPolicy]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputPolicy.cs
[CombatResultPlayback]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatResultPlayback.cs
[CombatSceneEventPresenter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneEventPresenter.cs
[CombatDisplayValues]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayValues.cs
[ICombatInteractionFeedback]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/ICombatInteractionFeedback.cs
[FieldView]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldView.cs
[FieldOutlineRenderer]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldOutlineRenderer.cs
[CombatAbandonPanel]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatAbandonPanel.cs
[CombatHud]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatHud.cs
[TimelineUI]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/TimelineUI.cs
[CombatLogUI]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatLogUI.cs
[CombatPresentationCleanupTests]: ../../../Assets/Tests/EditMode/Framework/CombatPresentationCleanupTests.cs
[CombatViewSystemTests]: ../../../Assets/Tests/EditMode/Framework/CombatViewSystemTests.cs
[CombatDisplayReuseTests]: ../../../Assets/Tests/EditMode/Framework/CombatDisplayReuseTests.cs
[FieldOutlineRendererTests]: ../../../Assets/Tests/EditMode/Framework/FieldOutlineRendererTests.cs
