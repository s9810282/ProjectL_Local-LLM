# 전투 행동별 Profiler 캡처·분석 안내 — 2026-11-10

> 2026-11-10 C2 조사: 연속 기록·변환을 재활성화했다. Editor 컴파일과 11,065프레임 전체 재변환을 확인했고, 머티리얼 배열 길이를 셰이더와 같은 256개로 수정한 뒤 약 141초 배치·적 턴 재생에서 크래시가 발생하지 않았다. 간헐 크래시의 완전한 해결 증명은 아니다. 자세한 증거는 [연속 기록 계획](../plans/2026-11-10-profiler-continuous-capture-plan.md)의 C2 기록을 따른다.

- 작성: Codex
- 상태: Phase 1~3 코드와 Phase 4 정적 검토·사용 안내 완료. **컴파일 미확인**, Unity 재생·Player 검증 대기.
- 설계: [전투 행동별 계측](../specs/2026-11-10-combat-action-profiler-design.md)
- Task·검증 기록: [구현 계획](../plans/2026-11-10-combat-action-profiler-plan.md)

## 1. 캡처와 내보내기

1. Unity가 스크립트 변경을 반영한 뒤 Console의 컴파일 오류를 확인한다.
2. CPU Profiler 녹화를 켜고 배치·호버·공격·적 행동을 재현한다. 최초 실행과 반복 실행을 각각 포함한다. Deep Profile은 기본적으로 끈다.
3. 녹화를 멈춘 뒤 `ProjectL > Profiler > 기록 내보내기 (Claude 분석용)`을 실행한다. 기존 `.data`를 불러와 내보내도 된다.
4. `Logs/ProfilerExport/<시각-식별자>/`의 다섯 파일을 함께 분석한다. 메뉴 `내보낸 폴더 열기`로 위치를 열 수 있다.

| 파일 | 용도 |
|---|---|
| `frames.csv` | 프레임 CPU·PlayerLoop·EditorLoop·GPU·GC 등 기존 지표 |
| `markers.csv` | 경로별 누적·평균·최대와 total/self/GC 각각의 최대 프레임 |
| `frame_markers.csv` | 모든 프레임의 Main Thread 계층 경로·시간·호출 수·GC 원본 |
| `actions.csv` | `Combat.Action.`로 시작하는 행동 마커만 추린 프레임별 표 |
| `summary.md` | 기존 요약과 행동 경로별 최대 프레임, 해당 프레임의 PlayerLoop·EditorLoop·GC |

익스포터는 프레임별 원본을 순차 기록한다. 취소·실패한 실행의 임시 폴더는 정리하며, 기존 내보내기를 덮어쓰지 않는다. 녹화 중 내보내기는 버퍼에서 밀려난 프레임이 생길 수 있어 권장하지 않는다. 이전 캡처에 새 마커가 없다면 `actions.csv`는 헤더만 생기고 요약에 미관측 안내가 나온다. **이전 파일에 행동 정보를 소급해서 만들지는 않는다.**

### 전체 플레이 연속 기록 (2026-11-10 추가)

최근 버퍼만 내보내면 초반 배치가 밀려날 수 있다. 전체 플레이를 남길 때는 다음 순서를 사용한다.

1. **Play 전에** `ProjectL > Profiler > 전체 플레이 자동 기록`을 켠다. 체크는 현재 Editor 세션 동안 유지된다.
2. Play를 시작하고 유닛 배치·보스 포효 등 확인할 동작을 수행한다.
3. **Unity Play를 종료한다.** raw 기록을 닫은 뒤 자동으로 CSV 변환을 시작한다. 긴 기록은 변환 시간이 걸리며 진행 창에서 취소할 수 있다.
4. `ProjectL > Profiler > 내보낸 폴더 열기`에서 최신 결과를 확인한다. 기존 다섯 파일에 기록 출처·범위·변환 상태를 담은 `capture.json`이 추가된다.

원본은 `Logs/ProfilerCapture/<고유 ID>/capture-000.raw`, `capture-001.raw`…와 `capture.json`, 분석 출력은 `Logs/ProfilerExport/<고유 ID>/`다. 플레이 중에는 Unity 네이티브 바이너리 기록을 사용하고 Hierarchy 순회·CSV 변환은 종료 후에 수행한다. 기록 원본은 최근 300프레임 버퍼와 별개다. 2026-11-10 이전 기록은 `capture.raw` 하나다.

#### 긴 기록과 메모리 (2026-11-11)

Unity는 원본 파일 하나를 불러올 때 그 안의 모든 프레임을 메모리에 올린다(프레임당 약 0.8MB, 원본 크기의 약 5배). 2026-11-10에 8분(47,466프레임, 원본 7.4GB) 기록을 변환하다 가상 메모리가 바닥나 Editor가 죽었다. 지금은 다음처럼 동작한다.

- **기록을 나눈다.** 게임 프레임 5,000개(약 50초)마다 다음 구간 파일로 바꾼다. 콘솔에 "연속 기록 구간 N 시작"이 나오고, 구간 목록은 `capture.json`의 `segments`에 적힌다.
- **구간을 하나씩 변환한다.** 구간 하나(메모리 약 4GB)를 불러 읽고 내려놓은 뒤 다음 구간을 불러, 같은 출력 폴더 하나에 잇는다. 진행 창에 "구간 k/n"이 나온다. `저장된 raw 또는 data 전체 내보내기...`에서 구간 파일 아무것이나 골라도 그 기록 전체를 변환한다.
- **불러오기 전에 메모리를 어림한다.** 가장 큰 원본 파일 × 5.5가 남은 가상 메모리에서 4GB를 뺀 양보다 크면, 자동 변환은 하지 않고 원본만 남긴다(`capture.json`의 `exportStatus = SkippedTooLarge`, 콘솔에 필요량·남은 양·할 일). 메뉴로 시작하면 확인 창이 뜨고, Enter·Esc는 변환하지 않는다. "그래도 변환"은 다른 프로그램을 닫은 뒤에만 고른다. 구간이 여럿이면 구간마다 불러오기 직전에 다시 보고, 모자라면 그 구간부터 `SkippedMemory`로 남기고 읽은 데까지만 출력한다(`Incomplete`).
- **변환이 끝나면 Profiler 버퍼를 비운다.** 메모리가 바로 돌아온다. 그 기록을 Profiler 창에서 보려면 구간 파일을 다시 Load한다.
- 2026-11-10 이전의 큰 `capture.raw` 하나짜리 기록(예: `20261110-224015-…`, 7.4GB)은 나뉘지 않아 이 PC에서는 변환되지 않는다. 원본만 보존한다.
- 출력도 길이에 비례해 커진다. `frame_markers.csv`는 프레임당 약 220KB라 8분 기록이면 약 10GB다. 디스크 여유를 확인한다.
- 구간 번호를 이은 방법은 `capture.json`에 남는다: 구간마다 `loadedFirstFrame`·`loadedLastFrame`·`outputOffset`·`loadStatus`, 전체 `renumberedSegments`(번호를 앞 구간 뒤로 옮긴 구간 수)·`segmentGapFrames`(구간 사이 빈 번호)·`segmentOverlapFrames`(두 파일에 다 있어 한 번만 쓴 프레임)·`missingSegments`(읽지 못한 구간). 빈 번호나 읽지 못한 구간이 있으면 `Incomplete`다.
- 기록 폴더를 다른 곳으로 옮겨도 `저장된 raw 또는 data 전체 내보내기...`로 그 폴더의 구간 파일을 고르면 같은 폴더의 `capture.json`으로 기록 전체를 변환한다.

- 이미 Play 중이면 `지금부터 연속 기록 시작`으로 해당 시점부터 기록한다. `연속 기록 중지 및 내보내기`는 raw를 닫고 실제 Play 종료 뒤 변환한다. 이 경우 시작 이전·중지 이후는 포함하지 않는다.
- 변환 취소·실패 후에는 `마지막 연속 기록 다시 내보내기`를 사용한다. Editor를 다시 열었거나 다른 파일을 고를 때는 `저장된 raw 또는 data 전체 내보내기...`를 사용한다. 원본은 삭제하지 않는다.
- 모든 CSV를 작성한 뒤 폴더 이름 확정만 실패하면 `.partial-*` 폴더도 보존한다. 해당 폴더의 완성 파일을 확인할 수 있지만 실패 상태를 정상 완료로 해석하지 않는다. 일시적 접근 거부는 최대 0.5초 동안 재시도한다.
- 원본을 불러오기 전 현재 Profiler 버퍼를 `Logs/ProfilerCapture/buffer-before-load-*.data`로 보존한다. 로드와 변환은 Play 종료 후 가능하다. 변환 뒤 버퍼를 비우므로 이 백업은 보통 최근 몇백 프레임만 담는다(2026-11-10 이전에는 앞 변환의 프레임이 버퍼에 남아 1GB가 넘는 백업이 생겼다).
- 스크립트 리로드·Editor 종료·기록 설정 외부 변경은 `Interrupted`로 남긴다. 정상 중지는 기록을 끈 상태로 두고 CPU 영역·Profile Editor·logFile 설정을 복원한다. 기존 다른 바이너리 기록이 켜져 있으면 시작하지 않는다.
- `summary.md`에서 출처·시작/종료·관측 프레임 범위·변환 상태를 확인한다. `Incomplete`는 중단, 읽기 스킵, Unity 누락 알림, 관측 범위보다 적은 출력, 구간 사이 빈 번호 또는 읽지 못한 구간을 뜻한다. `Converted`는 변환 완료이며 무손실 증명은 아니다. 행동 마커 0건도 별도 경고한다.

기존 `기록 내보내기 (Claude 분석용)`은 여전히 **현재 Profiler에 남은 버퍼만** 변환한다. 전체 플레이를 새로 기록하는 메뉴와 구분한다. 연속 기록 코드는 Editor 전용이므로 Player 빌드에는 포함되지 않는다.

**C2 Editor 컴파일·재변환 확인.** 약 91초 원본에서 0~11064 전체 프레임과 프레임 5273의 배치 제출이 출력됐다. raw 로드 시에도 적용되던 300프레임 제한은 변환 동안 native history를 확장하고 끝나면 원래 설정 길이로 복원하여 처리한다. 포효 재현, Domain Reload 옵션별 대조 및 중단 경로 검증은 남아 있다. 구현·검토 기록은 [연속 기록 계획](../plans/2026-11-10-profiler-continuous-capture-plan.md)을 따른다.

## 2. 배치·포효 프레임 찾기

`summary.md`의 **행동 마커별 최대 처리 프레임**에서 아래 이름을 찾는다. `actions.csv`의 `name` 필터로 같은 종류의 모든 프레임을 확인하고, 느린 행의 `frame`으로 `frames.csv`와 `frame_markers.csv`를 연결한다.

| 확인 대상 | 검색할 마커 또는 접두사 |
|---|---|
| 배치 조작 | `Combat.Action.Interaction.Placement.` |
| 메인 배치 제출 | `Combat.Action.Command.PlaceMainCharacter.Submit` / `.Queued` |
| 손패 유닛 배치 제출 | `Combat.Action.Command.PlaceUnit.Submit` / `.Queued` |
| 배치 결과 처리 | `Combat.Action.Playback.Player.Character.Placed.` |
| 호버 | `Combat.Action.Interaction.UnitHover.`, `.EnvironmentHover.`, `.NexusHover.` 및 `Combat.Action.UI.` |
| 공격 제출 | `Combat.Action.Command.Attack.` |
| 적 이동 | `Combat.Action.Playback.Enemy.Move.Voluntary.` |
| 적 밀림·끌어오기 | `Combat.Action.Playback.Enemy.Move.Forced.` / `.Move.Pull.` |
| 적 공격 | `Combat.Action.Playback.Enemy.Attack.` |
| 보스 포효 | `Combat.Action.Playback.Enemy.Roar.` |
| 소환 | `Combat.Action.Playback.Enemy.Summon.` |
| 특성·추가 공격 | `Combat.Action.Playback.<진영>.Effects.` 및 `.Attack.` |

위 이름은 실행 시 관측을 보장하는 목록이 아니라 코드의 분류다. 표시 사실에서 주체를 찾을 수 없으면 `Unknown`, 주체가 없는 사건은 `System`이다. 소환의 진영은 소환자, 피해·사망의 진영은 피해 대상이다. 턴으로 진영을 판정하지 않아 플레이어 턴의 적 반격도 Enemy로 분류된다.

포효는 `Begin → Tick → Hit → Tick → End` 형태로 읽는다. Hit 뒤에도 크기·줌 복귀가 남을 수 있고, 취소되면 `Cancel`이다. CPU Scope는 매 호출 안에서 닫힌다. `Tick`의 ms는 그 프레임의 CPU 작업량이며 포효가 지속된 실시간 길이가 아니다.

## 3. 단계와 수치 해석

| 단계 | 의미 |
|---|---|
| `Input.<Request>.Handle` / `.Blocked` | 입력 수신 및 수신 경계의 차단 |
| `Interaction.*` / `UI.*` | 기능·UI 소유자의 호출 비용. 선택·취소·드래그 등 시도도 포함 |
| `Command.<Kind>.Submit` | View Buffer에 제출하는 동기 호출 |
| `Command.<Kind>.Queued` / `.Blocked` | View 제출 번호가 생김 / Gate 차단·제출 번호 없음. Core 성공 판정과 다름 |
| `Preview.<Kind>.Request` / `.Apply` | 미리보기 제출 / 응답 소비·표시 반영. 거절 응답의 적용도 포함 |
| `Playback.*.Dispatch` / `.Handle` | 사건 진입 / 일반 사건 Presenter 내부 처리. 이동·기차·국면 전환의 진입도 포함 |
| `Prepare` / `Begin` | 준비·대여 시도 / 실행 시작 처리. 조각 실행은 실제 실행을 열었을 때 Begin |
| `Tick` / `MotionTick` / `Interpolate` | 실행 진행 / 외부 모션 진행 / 실제 이동 보간 구간 |
| `Hit` / `End` / `Cancel` | 유효 타격 신호·대상 해제 / 실제 연출 종료 / 취소 정리 |
| `ProjectileBegin/Tick/End/Cancel` | 발사체의 실제 수명. 발사 때의 진영을 각 비행에 보관 |
| `Poll` / `PointerRefresh.Poll` | 자동 갱신·조회. 새로운 사용자 입력 횟수가 아님 |

부모·자식 total은 서로 포함하므로 합하지 않는다. `Dispatch`와 `Handle`, Gate의 Hit와 조각의 Hit도 같은 행동의 서로 다른 처리 경로다. **마커 호출 수를 곧바로 클릭 수·공격 횟수·명중 수로 세지 않는다.** 자동 포인터 갱신은 하위 Interaction을 다시 호출할 수 있으므로 경로의 `PointerRefresh.Poll`도 함께 본다. 카메라 `Apply`는 허용된 프레임의 컨트롤 호출이며 실제 위치 변경 횟수가 아니다.

`avg_*`는 캡처 전체 프레임 기준, `avg_present_*`는 그 경로가 관측된 프레임 기준이다. 최대값 동률은 첫 프레임을 남긴다. `first_frame/last_frame`은 관측 범위이고, 동일 종류의 여러 유닛 실행을 하나로 연결한 기간이 아니다. 개별 유닛 ID 추적은 제공하지 않는다. GPU·카운터의 빈 값은 미관측이며 0이 아니다. 원본도 Unity Hierarchy의 Main Thread·동일 이름 합침·EditorOnly 샘플 숨김 설정을 따른다.

## 4. 빌드 영향

새 마커의 선언·초기화·분류·Scope·진단 필드는 전부 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에 있다. **일반 비개발 Player에는 이번 계측 코드가 컴파일 대상에서 제외된다.** 익스포터는 Editor 폴더에만 있다. 기존 프로젝트의 다른 Profiler 코드는 이번 변경의 제외 대상이 아니다.

에디터와 Development Build에는 마커 기록과 진영 조회의 비용이 있다. 정적 마커 초기화·JIT가 첫 실행에 잡힐 수 있으므로 첫 사용과 반복 사용을 따로 측정한다. 이름은 enum/기존 사건 형식 표에서 초기화 때 만들며 매 입력·Tick마다 문자열·목록·클로저를 새로 만들지 않는다. 초기화 이후의 실제 GC와 측정 비용은 Unity에서 확인해야 한다.

Unity는 Begin/End를 `ENABLE_PROFILER` 조건으로 처리하지만, AutoScope를 포함한 새 코드를 확실히 제외하기 위해 외부 컴파일 조건도 적용했다. [Unity 6 ProfilerMarker 문서](https://docs.unity3d.com/cn/6000.0/ScriptReference/Unity.Profiling.ProfilerMarker.html), [Unity 6000.0 구현](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.cs).

## 5. 구현 경계·정적 점검

- 입력 SSOT는 `CombatInputRouter.Request`, `CombatViewCommandKind`, `CombatViewPreviewKind`다. sparse enum 값을 인덱스로 쓰며 빈 값은 Unknown으로 둔다.
- 사건 SSOT는 `CombatEventTypeTable.EventTypes`·Actor/Subject/AttackSource와 Character Kind다. 새 처리기·라우터·서비스를 복제하지 않았다.
- 실행 객체에 불변 분류만 붙이고 원래 완료·취소·자원 반환을 따른다. 프레임을 넘는 Scope나 별도 진행 상태 머신은 없다.
- 생성·조립은 기존 CombatSceneEventPresenter에서 이루어지고, 계측용 서비스 등록·Group Export·소비자 서비스 조회는 없다. FW 서비스 계층과 UI 등록·입력 바인딩은 그대로다.
- UI 클릭·Hover·Submit은 기존 수신자를 사용한다. 새 리스너나 UnityEvent 연결은 없다. 공용 Deck/Option UI의 같은 수신자는 다른 화면에서도 호출될 수 있으므로 마커 접두사가 Combat 씬의 증거는 아니다.
- Core·FieldCamera·Architecture·에셋·기존 Test는 수정하지 않았다. 현재 코드 트리에 별도 개발 Layer 조작 UI는 없어 새 기능을 만들지 않았다.
- 릴리스 분기의 계측 잔존 여부, 전처리/괄호 짝, 기존 실행 토큰 차이, CSV 열·정밀도·이스케이프·최대값 동률·임시 출력 정리를 정적으로 검토했다. 실행 검증과 구분한다.

## 6. Unity 인수 대기

- [ ] 컴파일 오류 없음. 실제 내보내기에서 다섯 파일 생성, 구 캡처에서 actions 미관측 안내.
- [ ] 배치·회수·호버·공격·이동/되돌리기·아이템·확인창·카메라·정보 UI의 기존 동작과 마커 확인.
- [ ] 적 근접/원거리/반격, 조기 공격, 동시 투사체, 포효·소환의 최초/반복 실행 캡처.
- [ ] 포효 Hit 이후 Tick, 정상 End와 Cancel·씬 해제, 기존 연발 피해 숫자 확인.
- [ ] actions의 최대 프레임을 Unity CPU Hierarchy·PlayerLoop·기존 Core 마커와 대조.
- [ ] 1프레임/긴 캡처, 시작·끝 잘림, 같은 종류 동시 실행, 녹화 중 스킵·내보내기 취소 확인.
- [ ] Development Build 실기기 측정, 일반 비개발 Player의 실행·새 계측 제외 확인.
- [ ] 정적 초기화 이후 호버/진행 구간에서 새 GC가 발생하지 않는지 측정.

Test 코드·MSBuild·Unity 배치 실행은 수행하지 않았다. **컴파일 미확인**.

## 7. 기능 소유자별 마커 위치

공통 Input/Command/Preview 마커는 `CombatInputProfiler`가 기존 enum으로 등록한다. Input은 Receiver, Submit은 Gate와 Move의 직접 제출, Preview Request는 기존 SetPreview 호출, Apply는 배치·범위·공격·이동·행동 가능 응답 소비 위치에 들어 있다. 아래 표는 기능별 고정 Scope의 코드 위치다. Playback의 동적 분류는 §2~3의 규칙과 `CombatEventProfiler`를 따른다.

| 소유 파일·메서드 | 마커 |
|---|---|
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void SelectDeckUnit` | `Combat.Action.Interaction.Placement.Select` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void CancelDeckSelection` | `Combat.Action.Interaction.Placement.Deselect` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.Placement.Drag` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void SelectPressed` | `Combat.Action.Interaction.Placement.Pick` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void SelectReleased` | `Combat.Action.Interaction.Placement.Release` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal void Cancel()` | `Combat.Action.Interaction.Placement.Cancel` |
| [PlacementDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/PlacementDragInteraction.cs) · `internal bool TryReturnUnderPointer` | `Combat.Action.Interaction.Placement.Return` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `internal void Enter` | `Combat.Action.Interaction.Attack.Enter` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.Attack.Aim` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `internal void Cancel` | `Combat.Action.Interaction.Attack.Cancel` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `internal void SelectReleased` | `Combat.Action.Interaction.Attack.Release` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `private void SubmitTarget` | `Combat.Action.Interaction.Attack.Confirm` |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) · `internal void Exit()` | `Combat.Action.Interaction.Attack.Exit` |
| [CombatMovementInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs) · `internal bool Begin` | `Combat.Action.Interaction.Move.Begin` |
| [CombatMovementInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.Move.Drag` |
| [CombatMovementInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs) · `internal void Confirm` | `Combat.Action.Interaction.Move.Confirm` |
| [CombatMovementInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs) · `internal void Cancel()` | `Combat.Action.Interaction.Move.Cancel` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `internal void BeginPlanning()` | `Combat.Action.Interaction.MovePath.Enter` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `public void PointerMoved` | `Combat.Action.Interaction.MovePath.PointerMoved` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `public void SelectPressed` | `Combat.Action.Interaction.MovePath.Select` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `public void SelectReleased` | `Combat.Action.Interaction.MovePath.Release` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `public void Cancel()` | `Combat.Action.Interaction.MovePath.Cancel` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `internal void LeaveMoveMode()` | `Combat.Action.Interaction.MovePath.Exit` |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) · `internal void Refresh()` | `Combat.Action.Interaction.MovePath.Poll` |
| [CombatMovementUndoInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatMovementUndoInput.cs) · `internal bool Request()` | `Combat.Action.Interaction.UndoMove.Request` |
| [CombatItemDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatItemDragInteraction.cs) · `internal bool Select` | `Combat.Action.Interaction.Item.Select` |
| [CombatItemDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatItemDragInteraction.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.Item.Drag` |
| [CombatItemDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatItemDragInteraction.cs) · `internal bool SelectReleased` | `Combat.Action.Interaction.Item.Release` |
| [CombatItemDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatItemDragInteraction.cs) · `internal bool RequestUnequip` | `Combat.Action.Interaction.Item.Unequip` |
| [CombatItemDragInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatItemDragInteraction.cs) · `internal void Cancel()` | `Combat.Action.Interaction.Item.Cancel` |
| [CombatUnitRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.UnitHover.PointerMoved` |
| [CombatUnitRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs) · `internal void Poll` | `Combat.Action.Interaction.UnitHover.Poll` |
| [CombatUnitRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs) · `internal void Suspend()` | `Combat.Action.Interaction.UnitHover.Suspend` |
| [CombatUnitRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs) · `internal void Clear()` | `Combat.Action.Interaction.UnitHover.Clear` |
| [CombatEnvironmentHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentHover.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.EnvironmentHover.PointerMoved` |
| [CombatEnvironmentHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentHover.cs) · `internal void SelectPressed` | `Combat.Action.Interaction.EnvironmentHover.Pin` |
| [CombatEnvironmentHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentHover.cs) · `internal void CancelSelection()` | `Combat.Action.Interaction.EnvironmentHover.Unpin` |
| [CombatEnvironmentHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentHover.cs) · `internal void Refresh` | `Combat.Action.Interaction.EnvironmentHover.Poll` |
| [CombatEnvironmentHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentHover.cs) · `internal void Clear()` | `Combat.Action.Interaction.EnvironmentHover.Clear` |
| [CombatNexusRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatNexusRangeHover.cs) · `internal void Refresh` | `Combat.Action.Interaction.NexusHover.Poll` |
| [CombatNexusRangeHover.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatNexusRangeHover.cs) · `internal void Clear()` | `Combat.Action.Interaction.NexusHover.Clear` |
| [CombatCameraInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs) · `internal void PanChanged` | `Combat.Action.Interaction.Camera.Pan` |
| [CombatCameraInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs) · `internal void Scrolled` | `Combat.Action.Interaction.Camera.Zoom` |
| [CombatCameraInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs) · `internal bool Tick` | `Combat.Action.Interaction.Camera.Poll` |
| [CombatCameraInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs) · `internal void Clear()` | `Combat.Action.Interaction.Camera.Clear` |
| [CombatDeploymentConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatDeploymentConfirmation.cs) · `internal void Request()` | `Combat.Action.UI.EnterCombat.Request` |
| [CombatDeploymentConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatDeploymentConfirmation.cs) · `public void Respond` | `Combat.Action.UI.EnterCombat.Respond` |
| [CombatDeploymentConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatDeploymentConfirmation.cs) · `internal void Close()` | `Combat.Action.UI.EnterCombat.Close` |
| [CombatEndTurnConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatEndTurnConfirmation.cs) · `internal void Request()` | `Combat.Action.UI.EndTurn.Request` |
| [CombatEndTurnConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatEndTurnConfirmation.cs) · `public void Respond` | `Combat.Action.UI.EndTurn.Respond` |
| [CombatEndTurnConfirmation.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/ActionButtons/CombatEndTurnConfirmation.cs) · `internal void Close()` | `Combat.Action.UI.EndTurn.Close` |
| [CombatAbandonController.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatAbandonController.cs) · `internal bool TryRequestAbandon()` | `Combat.Action.UI.Abandon.Request` |
| [CombatDeckPlacementInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatDeckPlacementInput.cs) · `public void Select` | `Combat.Action.UI.DeckUnit.Select` |
| [CombatDeckPlacementInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatDeckPlacementInput.cs) · `public void Cancel()` | `Combat.Action.UI.DeckUnit.Cancel` |
| [CombatItemInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatItemInput.cs) · `public void Select` | `Combat.Action.UI.Item.Select` |
| [CombatItemInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatItemInput.cs) · `public void Cancel()` | `Combat.Action.UI.Item.Cancel` |
| [CombatItemInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/UiEntry/CombatItemInput.cs) · `public void RequestUnequip` | `Combat.Action.UI.Item.Unequip` |
| [HandCardHoverView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/HandCardHoverView.cs) · `public void OnPointerEnter` | `Combat.Action.UI.HandCard.HoverEnter` |
| [HandCardHoverView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/HandCardHoverView.cs) · `public void OnPointerExit` | `Combat.Action.UI.HandCard.HoverExit` |
| [CombatUnitInfoView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfoView.cs) · `private void HoverAbilityEnter` | `Combat.Action.UI.UnitInfo.AbilityHoverEnter` |
| [CombatUnitInfoView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfoView.cs) · `private void ResetAbilityHover()` | `Combat.Action.UI.UnitInfo.AbilityHoverExit` |
| [CombatUnitInfoView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfoView.cs) · `private void HoverStatusEnter` | `Combat.Action.UI.UnitInfo.StatusHoverEnter` |
| [CombatUnitInfoView.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfoView.cs) · `private void ResetStatusHover()` | `Combat.Action.UI.UnitInfo.StatusHoverExit` |
| [UnitHUD.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.UnitHUD.Click` |
| [UnitHUD.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs) · `void IUIInputReceiver.HoverEnter` | `Combat.Action.UI.UnitHUD.HoverEnter` |
| [UnitHUD.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs) · `void IUIInputReceiver.HoverExit` | `Combat.Action.UI.UnitHUD.HoverExit` |
| [PackUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/PackUI.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.Pack.Click` |
| [PackUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/PackUI.cs) · `void IUIInputReceiver.HoverEnter` | `Combat.Action.UI.Pack.HoverEnter` |
| [PackUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/PackUI.cs) · `void IUIInputReceiver.HoverExit` | `Combat.Action.UI.Pack.HoverExit` |
| [CombatActionPanel.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatActionPanel.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.ActionPanel.Click` |
| [CombatActionPanel.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatActionPanel.cs) · `void IUIInputReceiver.HoverEnter` | `Combat.Action.UI.ActionPanel.HoverEnter` |
| [CombatActionPanel.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatActionPanel.cs) · `void IUIInputReceiver.HoverExit` | `Combat.Action.UI.ActionPanel.HoverExit` |
| [BattleChangerUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/BattleChangerUI.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.PhaseButton.Click` |
| [BattleChangerUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/BattleChangerUI.cs) · `void IUIInputReceiver.HoverEnter` | `Combat.Action.UI.PhaseButton.HoverEnter` |
| [BattleChangerUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/BattleChangerUI.cs) · `void IUIInputReceiver.HoverExit` | `Combat.Action.UI.PhaseButton.HoverExit` |
| [CombatInfoHoverUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatInfoHoverUI.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.CombatInfo.Click` |
| [CombatInfoHoverUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatInfoHoverUI.cs) · `void IUIInputReceiver.HoverEnter` | `Combat.Action.UI.CombatInfo.HoverEnter` |
| [CombatInfoHoverUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatInfoHoverUI.cs) · `void IUIInputReceiver.HoverExit` | `Combat.Action.UI.CombatInfo.HoverExit` |
| [UnitHUD.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs) · `void IUISecondaryClickReceiver.SecondaryClick` | `Combat.Action.UI.UnitHUD.SecondaryClick` |
| [CombatCardInfoUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/CombatCardInfoUI.cs) · `void IUnitCardInfoReceiver.Toggle` | `Combat.Action.UI.CardInfo.Toggle` |
| [CombatCardInfoUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/CombatCardInfoUI.cs) · `private void CloseInfo()` | `Combat.Action.UI.CardInfo.Close` |
| [CombatCardInfoUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/CombatCardInfoUI.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.CardInfo.Click` |
| [DeckCardDragBinding.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCardDragBinding.cs) · `public void OnBeginDrag` | `Combat.Action.UI.DeckCard.BeginDrag` |
| [DeckCardDragBinding.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCardDragBinding.cs) · `public void OnDrag` | `Combat.Action.UI.DeckCard.Drag` |
| [DeckCardDragBinding.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCardDragBinding.cs) · `public void OnEndDrag` | `Combat.Action.UI.DeckCard.EndDrag` |
| [DeckCardHoverBinding.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCardHoverBinding.cs) · `public void OnPointerEnter` | `Combat.Action.UI.DeckCard.Enter` |
| [DeckCardHoverBinding.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCardHoverBinding.cs) · `public void OnPointerExit` | `Combat.Action.UI.DeckCard.Exit` |
| [CombatAttackDragGesture.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatAttackDragGesture.cs) · `internal void BeginPress` | `Combat.Action.Interaction.AttackDrag.Press` |
| [CombatAttackDragGesture.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatAttackDragGesture.cs) · `internal bool TryPromote` | `Combat.Action.Interaction.AttackDrag.PromoteToMove` |
| [CombatAttackDragGesture.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatAttackDragGesture.cs) · `internal ReleaseRoute Release()` | `Combat.Action.Interaction.AttackDrag.Release` |
| [CombatAttackDragGesture.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatAttackDragGesture.cs) · `internal void Reset()` | `Combat.Action.Interaction.AttackDrag.Reset` |
| [CombatInputRouter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputRouter.cs) · `internal void RefreshPointer` | `Combat.Action.Input.PointerRefresh.Poll` |
| [CombatCameraInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs) · `internal void PointerMoved` | `Combat.Action.Interaction.Camera.PointerMoved` |
| [OptionUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs) · `void IUIInputReceiver.Click` | `Combat.Action.UI.Option.Click` |
| [OptionUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs) · `public void RequestOpen` | `Combat.Action.UI.Option.Open` |
| [OptionUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs) · `public void RequestClose` | `Combat.Action.UI.Option.Close` |
| [OptionUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.cs) · `private void ShowConfirm` | `Combat.Action.UI.Option.ConfirmOpen` |
| [FieldTornadoPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldTornadoPresenter.cs) · `internal void TickAppearance` | `Combat.Action.Playback.System.TornadoAppearance.Poll` |
| [FieldTerrainPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldTerrainPresenter.cs) · `internal void TickAppearance` | `Combat.Action.Playback.System.TerrainAppearance.Poll` |
| [FieldRailwayPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldRailwayPresenter.cs) · `internal void Begin(RailwayGimmickActivationSnapshot` | `Combat.Action.Playback.System.Railway.Begin` |
| [FieldRailwayPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldRailwayPresenter.cs) · `internal void Tick` | `Combat.Action.Playback.System.Railway.Tick` |
| [CombatBodyTextWindow.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Text/CombatBodyTextWindow.cs) · `public bool Tick` | `Combat.Action.Playback.System.BodyText.Poll` |
