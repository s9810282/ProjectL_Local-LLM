# 전투 행동별 Profiler 마커·프레임 내보내기 설계 — 2026-11-10

- 작성: Codex
- 상태: Phase 1~3 코드·Phase 4 정적 검토/사용 안내 반영. Unity 인수 대기, 컴파일 미확인
- 요청: 사용자 행동(배치·호버·공격 등)과 적 이동·공격에 마커를 추가하고, 내보낸 기록에서 해당 프레임을 찾을 수 있게 한다. 일반 배포 빌드 영향을 함께 설명한다.
- 계획: [구현 계획](../plans/2026-11-10-combat-action-profiler-plan.md)

## 1. 문제와 범위

현재 `ProfilerFrameExporter`는 `frames.csv`에 프레임 지표, `markers.csv`에 전체 구간의 경로별 합계를 저장한다. 프레임과 마커의 대응은 잃고, `summary.md`의 느린 5프레임만 계층을 남긴다. `Combat.Result.PresentEvent` 하나로 여러 사건을 감싸므로 배치·공격·포효를 구분할 수 없다.

2026-11-10 16:34:03 기록의 15630프레임은 PlayerLoop 20.113ms, Mono.JIT 9.709ms지만 특정 동작과 연결할 증거가 없다. 기존 기록의 행동 이름을 사후 추정해서 채우지 않는다.

이번 범위는 **전투 화면의 사용자 조작 전체와 전투 결과의 표시·연출**이다. 전투 화면이 사용하는 손패·가방·유닛 정보 UI도 포함한다. 로비·스테이지 선택 화면 전체 계측은 별도 범위다.

- 입력 시도, 입력 Buffer 제출, 결과 수신, 연출 실행을 별도로 측정한다.
- 플레이어·적 이동과 공격, 보스 포효·소환을 구분한다. 사용자 요청의 적 이동·공격만 먼저 분리하고 다른 사건을 공통 이름에 숨기지 않는다.
- 기존 Core 마커도 프레임별로 보존한다. **Core 내부 계산·AI 마커를 새로 추가하는 설계는 아니다.** View의 적 공격 연출 시간이 Core 공격 계산 시간이라는 표기를 금지한다.
- Core·FieldCamera·Architecture, 게임 규칙, 입력 정책, 자원 수명은 변경하지 않는다. 카메라 조작은 전투 View의 입력 창구만 계측한다.
- 기존 변경 중인 연발 피해 숫자 관련 파일은 현재 작업본을 기준으로 계측만 추가하고 기존 변경을 되돌리지 않는다.

## 2. 빌드·성능 계약

| 환경 | 새 계측의 동작 |
|---|---|
| Unity Editor | 행동 마커 사용. 녹화 중 시간·호출·할당 관찰 가능 |
| Development Build | 행동 마커 사용. 연결된 Profiler에서 캡처하고 Editor에서 내보냄 |
| 일반 배포 빌드 | `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 밖의 게임 코드만 남김. 새 마커 선언·생성·조회·분류·메타데이터·호출을 모두 제외 |
| 익스포터 | `Tool/Editor` 전용. Player에서 CSV를 쓰지 않음 |

Unity의 `ProfilerMarker.Begin/End`는 일반 비개발 빌드에서 조건부로 제거된다. 이번 구현은 이 동작에만 의존하지 않고 **AutoScope와 마커를 고르는 부가 코드까지 명시적으로 제외**한다. 기존 프로젝트의 다른 Profiler 코드까지 영향이 0이라고 확대해서 말하지 않는다.

- Editor/개발 빌드의 측정 비용은 0이 아니다. 고빈도 호버·이동에서도 일정 개수의 정적 마커만 사용한다.
- 마커 이름에 유닛 ID·좌표·프레임·명령 번호를 문자열로 붙이지 않는다. 입력마다 문자열 보간, `Enum.ToString`, LINQ, 클로저, 마커 생성을 하지 않는다.
- 기존 enum/type 목록으로 마커 표를 만들 경우 최초 초기화 때 한 번만 만든다. 측정 첫 사용의 초기화 비용이 포함될 수 있음을 안내한다.
- 마커 Scope는 동기 호출 안에서 열고 닫는다. 프레임·비동기 콜백을 가로지르는 `Begin/End`는 사용하지 않는다. 예외·조기 return에도 닫힌다.
- 새 계측이 예외를 던져 게임 흐름을 막지 않는다. 분류할 사실이 없으면 `Unknown`으로 남기고 게임 실행은 기존대로 진행한다.
- 일반 빌드 제외는 구현 정책이며, 실제 Player 빌드 검증은 사용자 확인 전까지 미확인으로 기록한다.

공식 근거: [ProfilerMarker](https://docs.unity3d.com/cn/6000.0/ScriptReference/Unity.Profiling.ProfilerMarker.html), [AutoScope](https://docs.unity3d.com/cn/6000.0/ScriptReference/Unity.Profiling.ProfilerMarker.Auto.html).

## 3. 이름과 측정의 의미

새 행동 마커는 `Combat.Action.` 접두어를 쓴다. 같은 의미의 이름을 호출부마다 복사하지 않고, 해당 기능의 계측 정의 한 곳이 소유한다. 익스포터는 이 접두어와 이름 구조만 이해하고 게임의 사건 switch를 복제하지 않는다.

| 계열 | 예 | 의미 |
|---|---|---|
| Input | `Combat.Action.Input.PointerMoved.Handle` | 실제 수신한 입력 시도의 동기 처리 |
| Interaction | `Combat.Action.Interaction.Placement.Drag` | 기능 소유자가 처리한 드래그·조준·호버 |
| Command | `Combat.Action.Command.PlaceUnit.Submit` | 입력 Buffer 제출 작업 |
| Command | `Combat.Action.Command.PlaceUnit.Queued` / `.Blocked` | View 제출 수락/차단 지점. Core 성공과 구분 |
| Preview | `Combat.Action.Preview.UnitPlacement.Request` / `.Apply` | 실제 미리보기 요청/응답 적용 |
| Event | `Combat.Action.Event.Enemy.CombatRoarEvent.Dispatch` | 진영·사건 종류별 결과 표시 진입 |
| Playback | `Combat.Action.Playback.Enemy.Move.Tick` | 해당 프레임의 적 이동 연출 처리 |
| Playback | `Combat.Action.Playback.Enemy.Roar.Begin` / `.Tick` / `.Hit` / `.End` / `.Cancel` | 포효 실행 단계 |
| UI | `Combat.Action.UI.HandCard.HoverEnter` | 전투 UI의 사용자 조작 |

동작 단위 경과 시간과 CPU 작업 시간을 혼동하지 않는다. `.Tick`은 그 프레임에 실제 호출한 연출 코드의 시간이며, 대기 시간·GPU 렌더링 시간이나 애니메이션 전체 길이가 아니다. `.Queued`, `.Hit`, `.End` 같은 순간 표시는 호출 존재가 중요하며 시간이 매우 작을 수 있다.

원시 입력 콜백과 기능 처리는 중첩될 수 있다. 부모·자식 total을 합산하지 않는다. Self는 해당 마커에 포함되지 않은 다른 스레드의 비용까지 나타내지 않는다.

## 4. 사용자 행동 커버리지

현행 `CombatInputRouter.Request`, `CombatViewCommandKind`, `CombatViewPreviewKind`가 원본이다. 진단용으로 같은 게임 명령 enum을 새로 만들지 않는다. 각 기능 Phase의 완료 조건은 이 목록과 실제 우회 진입점의 전수 대조다.

| 행동 | 계측 위치·누락 방지 조건 |
|---|---|
| 포인터 이동, 누름·놓음, 취소, 포커스 상실 | `CombatInputReceiver`의 입구 및 수신 차단. 이후 Router 처리와 구분 |
| 손패 선택·선택 해제, 메인/서브 배치, 필드 유닛 집기·드래그·회수 | `PlacementDragInteraction`, 손패 UI 진입. 배치 미리보기 Request/Apply 포함 |
| 필드 유닛·넥서스·지형·장애물 호버 및 클릭 고정·해제 | `CombatUnitRangeHover`, `CombatNexusRangeHover`, `CombatEnvironmentHover`. PointerMoved와 자동 Poll/Refresh를 별개 이름으로 기록 |
| 공격 모드 진입·전환·취소, 타깃 조준·드래그, 공격 제출 | `AttackTargetingInteraction` 및 실제 공격 입력 진입점. 범위/공격 미리보기·피해 예상 적용도 포함 |
| 유닛 선택, 이동 경로 작성·수정·확정·취소, 이동 되돌리기 | `CombatPathInput`, `CombatMovementInteraction`, `CombatMovementUndoInput`. Move는 Gate를 우회하므로 직접 Submit 경로를 반드시 포함 |
| 아이템 선택·드래그·장착·교체·해제·취소 | `CombatItemInput`, `CombatItemDragInteraction` 및 가방 UI 진입 |
| 전투 시작·턴 종료·포기 및 확인창 승인/취소 | `UiEntry`의 Entry/Confirmation/Abandon 소유자. UI 요청과 확인 후 실제 제출을 구분 |
| 카메라 키 이동·휠 줌·가장자리 이동 | `CombatSceneInput`·`CombatCameraInput`의 View 측 처리. Router 본문을 우회하는 CameraPan/CameraZoom 포함 |
| 손패·유닛 정보·버프·가방의 hover/click/Submit·팝업 닫기, 개발 Layer 조작 | 해당 전투 UI 입력 수신자. 포인터 인터페이스·UIInputBinding 경로를 유지하고 새 리스너를 만들지 않음 |
| 공통 명령 | PlaceMainCharacter, PlaceUnit, ReturnUnit, EnterCombat, Move, Attack, EndPlayerTurn, EquipItem, UnequipItem, ReplaceItem, UndoMove 전부 |
| 공통 미리보기 | MainCharacterPlacement, UnitPlacement, MoveRange, Move, AttackRange, Attack, UnitAttackRange, ActionAvailability 전부 |

UI와 게임 입력의 같은 클릭은 서로 다른 계층의 비용이다. 이것을 두 번 행동한 것으로 집계하지 않는다. 정지된 포인터를 매 Tick 다시 검사하는 Poll도 새로운 사용자 입력 횟수로 집계하지 않는다.

## 5. 플레이어·적·보스 사건과 연출

### 5.1 사건 분류

- 모든 결과 사건의 Dispatch는 기존 `CombatEventTypeTable.EventTypes`와 `ActorOf`/대상 사실을 재사용한다. 기존 처리기 등록·라우팅 규칙을 복제하지 않는다.
- `CombatCharacterEvent`는 기존 Kind로 배치·이동·회수·재배치·사망 등 세부 사실을 구분한다. 일반 `CombatCharacterEvent` 하나로 접지 않는다.
- 진영은 사건의 CharacterCombatFacts 또는 이미 View가 받은 행위자 사실의 `Owner`로 정한다. 현재 턴으로 정하지 않는다. 플레이어 턴의 적 반격도 Enemy다.
- 공격은 공격자, 이동은 이동 주체, 포효는 BossId의 주체를 기준으로 한다. 여러 진영 대상이 섞인 피해·환경 사건에는 임의의 Enemy 라벨을 붙이지 않는다. 진영 불명/주체 없음은 `Unknown`/`System`이다.
- 사건을 소비하기 전에 사실을 읽되 계측 때문에 InGame DB를 조회하거나 Core를 호출하지 않는다.

### 5.2 여러 프레임에 걸친 실행

| 연출 | 주요 기존 소유자 | 필수 표식 |
|---|---|---|
| 이동·방향 복귀·끌어오기·넉백 | `CombatUnitMotionPlayback` 및 해당 이동 재생기 | 진영·종류별 Begin/Tick/End/Cancel. 준비 대기와 실제 보간을 구분 |
| 일반 공격·원거리 발사/비행·접촉·추가 공격 | `CombatAttackPlayback`, 기존 조각/발사체 실행 | 진영별 Begin/Tick/Hit/End/Cancel. 조기 시작 경로 포함 |
| 보스 포효·소환 및 기타 조각 기반 행동 | 처리기 조립 → `CombatPieceRunPlayer` → `CombatPieceRun` | 사건 종류별 Begin/Tick/Hit(있는 경우)/End/Cancel |
| 피해·회복·사망·상태·지형·증원 등 | 기존 사건 처리기/재생기 | 종류별 Dispatch. 지속 실행이 있으면 해당 소유자 Tick |

포효는 Hit 때 사건 완료를 알린 뒤 작아짐·줌 복귀가 계속될 수 있다. **사건 완료를 연출 End로 사용하지 않는다.** 실제 실행 종료와 취소 경계를 사용한다. 공격·발사체도 기존 실행 수명을 따른다.

기존 실행 객체가 이미 가진 Source/Composition/이동 사건으로 라벨을 얻는다. 진단에만 필요한 불변 분류를 보관해야 하면 개발 빌드 전용으로 실행 객체에 붙이고, 별도의 전투 진행 상태 머신·전역 활성 행동 목록은 만들지 않는다. 처리기의 조립은 분류를 연결하고 계측 구체 구현은 기능별 파일이 맡는다.

동일 종류의 여러 행동이 한 프레임에 겹치면 호출 수를 보존한다. 이번 단계는 **진영·행동 종류별 프레임 식별**이며 개별 유닛 ID별 인과 추적은 아니다. 같은 마커의 연속 관측을 단일 행동 인스턴스라고 단정하지 않는다.

## 6. 익스포터 출력

기존 세 파일과 기존 열의 의미를 유지하고 다음을 추가한다.

| 출력 | 행 단위·필드 |
|---|---|
| `frame_markers.csv` | 프레임 × 계층 경로. `frame,path,depth,name,total_ms,self_ms,calls,gc_bytes`. Main Thread 전체 마커를 0.1ms 제한 없이 보존 |
| `actions.csv` | 위 원본 중 `Combat.Action.` 마커를 뽑은 파생 표. 프레임·마커 이름·경로·분류·처리 단계·total/self·호출 수·GC. 마커 이름이 원본이고 분류 실패 시 원문을 유지 |
| `markers.csv` 추가 열 | total/self/GC의 최대가 나온 프레임 번호를 각각 보존. `frames_present` 기준 평균도 추가하되 기존 전체 프레임 평균을 교체하지 않음 |
| `summary.md` 추가 절 | 행동별 관측 프레임 수, 최초/마지막 관측 프레임, 최대 처리 시간과 프레임 번호, 그 프레임의 PlayerLoop/EditorLoop/할당. 행동별 가장 느린 관측 프레임을 CPU 순위 5개 밖에서도 찾을 수 있게 함 |

- 스레드 범위는 기존 Main Thread 하나다. GPU가 없으면 빈 값이며 0으로 보정하지 않는다. Render Thread/worker 분석으로 표현하지 않는다.
- 계산은 원래 double/float 값에서 하고 CSV ms는 최대 6자리 소수로 남긴다. 반올림한 전체 평균 × 프레임 수로 발생 시 평균을 복원하지 않는다.
- 최대값 동률은 첫 프레임을 사용하고 이를 명시한다. 최대 total과 최대 GC가 같은 프레임이라는 가정도 하지 않는다.
- 이미 지원하는 이전 캡처도 내보낼 수 있다. 새 행동 마커가 없으면 actions 헤더와 '행동 식별 마커 없음'을 남기고 '행동 없음'으로 결론내리지 않는다.
- 전체 프레임×경로 표를 메모리에 쌓지 않는다. Editor 전용 출력 협력 객체가 프레임을 순회하며 순차 기록하고, 기존 합산기는 합계/최대만 유지한다. Scanner는 게임별 행동 이름을 몰라도 된다.
- 고유 임시 출력 폴더에서 작성한 뒤 모두 성공하면 최종 폴더로 확정한다. 취소·예외 때 작성기/Profiler view를 닫고 이번 실행의 임시 산출물만 정리한다. 이전 export는 건드리지 않는다.
- summary의 기존 느린 프레임 트리는 간결하게 유지한다. 생략 없는 근거는 frame_markers에 있다. 메인 스레드 CPU, PlayerLoop, 행동 scope 시간을 별도 열/이름으로 구분한다.
- 녹화 버퍼 시작 이전에 열린 행동이나 끝 이후까지 이어진 행동의 전체 길이를 만들어내지 않는다. first/last는 **관측 범위**다. Begin/End 누락을 정상 완료로 추정하지 않는다.

## 7. 구조 검토

| 기준 | 이번 설계의 적용 |
|---|---|
| SSOT | 입력 종류는 기존 Request/Command/Preview enum, 사건 종류·주체는 기존 사건 표/사실. 진행 수명은 기존 재생기. CSV 요약은 프레임 기록의 파생 결과 |
| SRP | 입력/연출 소유자는 자신의 처리 구간만 계측. Scanner는 Profiler 읽기·집계, 출력기는 직렬화. 계측 분류가 게임 정책을 소유하지 않음 |
| OCP | 기존 enum/type 표에 근거한 마커 등록. 새 사건 때문에 익스포터의 거대 switch를 수정하지 않음. 기능별 세부 계측 정의 분리 |
| LSP | return 값·예외·완료/취소·재진입 보장을 유지. 계측 때문에 추가 콜백이나 게임 상태 전이를 만들지 않음 |
| ISP | 소비자에게 전체 Input/Playback 의존성 묶음을 새로 넘기지 않음. 계측에 필요한 값만 전달 |
| DIP | Core·DB·Global 서비스의 새 의존성 없음. 기존 표시 사실과 공개 입력 경로를 사용. UI가 Core를 직접 호출하지 않음 |
| FW-001~003 | 새 서비스/모듈/루트 export가 필요 없음. 기존 Builder·등록 순서 변경 없음 |
| FW-004~009 | 내부 상태 우회 없음. 입력 Submit 및 UIInputBinding·포인터 경로 유지. 바인딩/등록/화면 이력 변경 없음 |
| FW-010~016 | 렌더 대여 반환, 이벤트·Pool·Queue·런타임 실행 수명 그대로. 진단만을 위한 전역 구독 없음 |
| 기능 변경 독립성 | 입력, 이동, 공격/포효, Editor 출력의 계측 정의를 분리. 조립은 라벨 연결, 기능은 해당 소유자가 구현. 다른 기능까지 읽어야 하는 중앙 계측 관리자 금지 |
| JMH 지침 §27·28 | 프로파일링 서비스·인터페이스·Factory 계층을 새로 만들 이유가 없음. 작은 정적 정의/구체 출력 협력 객체로 시작. Hot Path 할당과 종료 시 파일 핸들 정리 점검 |

## 8. 검증과 현재 상태

View Test 코드 작성 및 전투 View MSBuild 금지. 코드 정적 검토와 CSV 구조 확인은 Codex, Unity 컴파일·재생·개발/일반 Player 확인은 사용자 담당이다. 마커 유무가 게임 동작에 영향을 주지 않는지, 첫 실행/반복 실행, 포인터 정지/이동, 적 반격·보스 포효의 Hit 이후 종료·취소를 확인한다.

2026-11-10 사용자가 Phase 4까지 연속 구현을 승인했다. Phase 1~3 코드와 Phase 4 정적 검토·사용 안내를 반영했다. **컴파일 미확인**, Unity 캡처·재생·Player 인수는 대기다. 실제 마커·출력 해석은 [사용 안내](../viewDocs/combat-action-profiler-guide.md), 상태 원본은 [계획](../plans/2026-11-10-combat-action-profiler-plan.md)이다.
