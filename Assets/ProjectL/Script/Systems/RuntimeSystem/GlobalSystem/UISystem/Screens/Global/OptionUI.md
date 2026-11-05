# 옵션 UI 1계층 — 2026-10-14

## Phase 12 — 종료 확인과 설정 저장 (2026-10-16)

종료 버튼은 QuitGame Confirm만 연다. 종료 N은 확인창만 닫고 옵션을 유지한다. 종료 Y는 키·음향의 기존 HasChanges를 검사해 변경이 없으면 즉시 종료하고, 변경이 있으면 SaveSettings Confirm을 연다. 저장 Y는 기존 SaveEdit으로 저장, N과 ESC는 기존 CancelEdit으로 취소한 뒤 모두 종료한다. Option 강제 해제 시 대기 상태를 비워 다음 옵션 수명에 종료 의도가 남지 않는다.

Task: Option의 확인 분기에 종료 후 저장 대기 상태 추가 → 저장 처리 이후 기존 Hide로 입력·오디오·시간 복구 → 플랫폼 종료 호출 → EventSystem 순차 확인·Y/N·키/음향 변경·중복 종료 회귀. 종료 결정은 Option, 실제 앱 종료는 OptionGameExit가 맡는다. 기존 Confirm·설정 비교/저장 원본을 재사용하며 Core·전투 종료·타이틀 이동에는 관여하지 않는다. 빌드에서는 Application.Quit, 에디터에서는 Play 모드를 종료한다. 음향의 다음 실행 영구 저장은 기존 범위와 동일하게 추가하지 않는다.

검증: Systems·Framework 테스트 어셈블리 관리 컴파일 성공. EventSystem으로 종료 N, 변경 없음 종료 Y, 키/음향/동시 변경 뒤 저장 Y/N, 저장 ESC, 중복 종료 및 강제 Hide 뒤 대기 상태 정리 회귀 테스트를 추가했다. 종료 대체 객체로 실제 앱 종료를 분리한다. UnityLockfile이 남아 있어 실제 EditMode 실행과 빌드 종료 재생 확인은 대기다. 테스트 컴파일을 테스트 실행 통과로 기록하지 않는다. Phase 12 코드 완료 후 리뷰 지점에서 멈춘다.

## Phase 11 — RunAbandon 연결 (2026-10-16)

RunAbandon은 Confirm만 연다. Y 확인 뒤에만 `ICombatAbandonRequestService.TryRequestAbandon()`으로 기존 전투 포기 요청 상태를 세운다. 수락 시 Option을 닫아 입력 독점·오디오 일시정지·Time.timeScale을 복구하고, 기존 전투 진행 흐름이 남은 연출과 포기·전환을 처리한다. N과 거절된 요청은 Option을 유지한다. 전투 밖·기록 모드·이미 요청했거나 종료된 전투는 기존 정책으로 거절한다. Option은 Core 호출이나 별도 씬 전환을 하지 않는다. 요청 상태와 허용 조건은 CombatAbandonController 한 곳에 유지한다. 기존 씬 패널 참조는 복원하지 않는다.

Task: 기존 포기 컨트롤러에 확인된 외부 요청 입구 추가 → Option의 Confirm Y 연결 → EventSystem Y/N·중복 확인 및 전투 요청 상태 회귀 확인. 이번 Phase는 Option의 포기 연결만 포함한다.

검증: Systems·Framework 테스트 어셈블리 관리 컴파일 성공. EventSystem으로 RunAbandon → Y/N을 전달하는 회귀와 확인 전 미요청·수락 시 일시정지 해제·중복 확인 방지, 전투 미연결·기록 모드 거절 테스트를 추가했다. UnityLockfile과 실행 중인 Unity 프로세스가 있어 동일 프로젝트 배치 실행은 진행하지 않았으며, 실제 EditMode 실행·화면 재생 결과는 대기다. 기존 통과 기록과 구분한다. Phase 11 코드 완료 후 리뷰 지점에서 멈춘다.

## 확정 범위

- `OptionUI : UIBase`, `UIId.Setting`, `UIOpenPolicy.Parallel`.
- Option 버튼 또는 ESC(`Common.OpenSettings`)로 UISystem을 거쳐 `Open()`.
- Close 버튼 또는 다음 ESC(`Settings.Close`)로 UISystem을 거쳐 `Hide()`.
- 옵션이 열린 동안 배경의 게임 입력을 막고, 닫으면 입력과 오디오 상태를 복원한다.
- 자식 설정 화면, 값 저장, 시간 정지는 이번 Phase에 포함하지 않는다.

## 연결과 책임

기존 `GlobalUICanvas` 프리팹의 `Option`에 컴포넌트를 추가하고, 기존 자식 `OptionPannel`과 두 Button을 연결했다. 루트 버튼은 활성 상태로 유지하고 Panel만 표시한다. 같은 유지 Canvas 아래에 있으므로 씬 전환 후에는 일반 UI로 다시 등록·바인딩된다.

- 버튼: `UIInputBinding` → `IUIInputReceiver` → `IUISystemService`. Inspector UnityEvent나 새 입력 구독을 사용하지 않는다.
- ESC: 기존 PlayerInputActions → UnityInputBridge → InputSystem.Submit → OptionInputRouter → UI 서비스. 공용·설정 맵 이름은 `GlobalInputNames`에 두고 기존 화면별 상수는 이를 참조한다.
- 입력: `IInputLayerOverrideService.OverrideLayer(Settings)`의 IDisposable 수명이 독점을 소유한다. 다른 View의 매 프레임 Layer 동기화가 옵션 입력을 덮지 못한다. 독점 중 `ChangeLayer`/`ClearLayer`는 복귀 요청을 갱신한다. 요청이 없으면 이전 Layer, 요청이 있으면 마지막 요청으로 복귀한다. `CurrentLayer`는 실제 독점 Layer를 반환한다. 중첩 독점은 아직 지원하지 않는다.
- 표시: 투명한 전체 Canvas 클릭 차단 영역을 한 번 만들고 옵션 바로 뒤에 배치한다. 빈 Panel 클릭은 부모 Option 버튼으로 전파하지 않는다. 열 때 Close 버튼을 선택하고 닫을 때 이전 선택을 복원한다.
- 오디오: `IGlobalSystemGroupService.AudioServiceCommand.SetGamePaused()` → AudioSystem → AudioManager.SetGamePaused(). Catalog의 GamePause Bus 설정을 따른다. 열기 전 `IsGamePaused`를 보관하여 기존 일시정지를 닫을 때 해제하지 않는다.
- 수명: 중복 Open/Hide는 자원을 중복 획득·반환하지 않는다. 비활성화와 파괴에도 정리하며, Global 소유로 마지막 모듈에 등록한 OptionInputRouter가 UI·입력·오디오 서비스 종료 전에 옵션을 닫는다.

### 현재 입력 조립·조회 — 2026-10-18

GlobalSystemFactory가 OptionInputRouter를 한 번 만들고 Open/Close Controller와 Settings Layer를 화면 등록 정보에 합친다. Controller는 공통 InputSystem에 연결하고 Router 서비스/모듈은 Global에 등록한다. Runtime 루트에는 Router나 옵션 닫기 서비스를 다시 공개하지 않는다. OptionUI는 `resolver.GetService(SystemGroupsService.Global).OptionCloseRequests`를 받아 현재 IOptionInputReceiver를 Bind/Unbind한다. 화면별 Stage/StageSelection/Artifact/Lobby 슬롯 소유는 계속 ViewGroup.Input이다.

종료는 루트 LifeCycle → InGame → Global, Global 내부 OptionRouter → Job → UI → Loding → Input → Audio → Utility → Render 순서다. 옵션 Hide가 편집/capture 취소·override 해제·오디오/timeScale 복원을 마친 뒤 사용 서비스가 종료된다. 상세 계약과 검증은 [입력 설계](../../../../../../../../../docs/superpowers/specs/2026-10-13-view-structure-design-Hojin.md) 및 [Task 실행 기록](../../../../../../../../../docs/superpowers/plans/2026-10-13-view-structure-plan-Hojin.md#32-화면별-입력-조립-phase13-2026-10-1718)을 따른다. 아래 날짜별 Phase 기록은 당시 구현·검증 이력이며, 과거의 루트 소유 문구는 현재 조립 위치를 뜻하지 않는다.

## Task / Phase 1 기록

- [x] 입력·UI·오디오 계약 및 기존 프리팹 확인.
- [x] 옵션 화면, 버튼 수신, 공용 ESC Controller 연결.
- [x] 임시 입력 독점, 오디오 복원 및 프리팹 참조 연결.
- [x] Systems·Framework 테스트 어셈블리 관리 컴파일 성공. Unity 직렬화 필드의 기존 방식에 따른 CS0649 경고가 있다.
- [x] 관리 실행 테스트 6건: Layer 복원·중복 Dispose, 동기화 요청 보관, ClearLayer 보관, 중첩 거부, 종료 뒤 Dispose, Submit 경유 설정 열기·닫기.
- [ ] Unity EditMode의 `OptionUITests` 4건: 포인터/Submit, 비상호작용 버튼, 기존 오디오 정지 유지, 비활성화 정리.
- [ ] Unity 재생: Option/ESC로 열기 → Close/ESC로 닫기, ESC 한 번에 열고 바로 닫히지 않는지, 배경 게임 입력과 클릭 차단, Catalog에 따른 오디오 정지·복원, 씬 전환 뒤 재사용.

SSOT: 표시 수명은 OptionUI, 실제 Layer와 보류 요청은 InputSystem, 오디오 정지는 AudioSystem/AudioManager가 소유한다. SOLID: Controller는 액션 전달, View는 표시 수명, Factory는 조립을 맡는다. FW-001/002/003은 별도 독점 서비스 인터페이스 등록·기존 모듈 등록·단일 Build를 유지했고, FW-005/006/007은 Configure 1회·Submit 경유를 유지한다. Core·FieldCamera·Architecture 파일 변경은 없다.

Phase 1 코드 작업 후 Unity 검증을 위한 리뷰 지점에서 멈춘다.

## Phase 2 — 공용 Confirm 연결 (2026-10-14)

현재 닫기 동작은 아래 내용으로 갱신한다. Canvas Sort Order는 사용자가 설정한 값을 유지했다.

- `ConfirmUI : UIBase`, `UIId.Confirm = 16`, `ParentID = UIId.Setting`. 기존 OptionPannel 아래 Confirm 오브젝트를 재사용하고 Y/N 버튼을 연결했다.
- `ConfirmAction`은 설정 저장·타이틀 이동·진행 저장(예약)·Run 포기 후 로비 이동·게임 종료를 구분한다. 진행 저장은 화면에서 요청하지 않으며 Show에 전달돼도 열리지 않는다.
- 옵션 Close/ESC → 설정 저장 Confirm. 변경 여부 검사는 TODO로 남겨 현재는 매번 확인한다. Y/N 어느 쪽이든 Confirm과 OptionPannel을 함께 닫고 입력·오디오를 복원한다. Y의 저장 실행도 TODO다.
- Title/RunAbandon/Exit → 각각 대응하는 Confirm. Y/N 어느 쪽이든 Confirm만 닫고 옵션의 입력 독점과 오디오 정지를 유지한다. Y의 이동·포기·종료 실행은 모두 TODO다.
- Confirm이 열린 상태에서 ESC는 Confirm만 닫는다. 열린 확인 요청을 다른 버튼 요청으로 덮어쓰지 않는다.
- 버튼 입력은 UIInputBinding과 IConfirmReceiver의 직접 호출을 사용한다. ESC는 루트에서 서비스로 등록한 IOptionCloseRequestService의 명시적인 수신자로 전달하여, 사용자 닫기 요청과 종료 시 강제 정리를 구분한다.
- 기존 확인창 본문은 Image다. 기존 외형을 유지하고 TMP 문구 참조는 선택 항목으로 두었다. 필요할 때 ConfirmUI.message에 Text를 연결하면 종류별 문구를 표시한다.

Phase 2 Task: 스크립트·서비스 연결·프리팹 참조 완료. Systems 및 Framework 테스트 어셈블리 컴파일 성공, 입력 관리 테스트 6건 통과. Y/N 양쪽 분기·부모 유지/종료·ESC 자식 우선·진행 저장 미사용을 포함한 OptionUITests 13건은 컴파일 완료이며 Unity EditMode 실행·실제 재생 확인은 대기한다. 새 서비스는 인터페이스로 한 번 등록하고 기존 Router 모듈 수명을 재사용한다(FW-001/002). 부모·자식 상태와 닫기 이력은 기존 UISystem을 통해 처리한다(SSOT). Core 기능이나 저장·씬 전환 구현은 추가하지 않았다.

## Phase 3 — GamePlay / Sound 화면 연결 (2026-10-14)

- GamePlay 버튼 → 기존 `Pannel/KeySetting`의 `KeySettingUI`, Sound 버튼 → 기존 `Pannel/Sound`의 `SoundSettingUI`.
- 두 스크립트는 UIBase를 상속하며 UIId.KeySetting(17) / SoundSetting(18), ParentID.Setting으로 등록한다. 키·사운드 설정 세부 기능은 TODO로 남겼다.
- 버튼은 UIInputBinding으로 직접 전달한다. 한쪽을 열 때 UISystem으로 다른 쪽을 닫아 한 화면만 표시한다. 별도 선택 상태 복사본은 두지 않는다.
- 두 화면은 Parallel 정책으로 옵션 밖의 기존 화면을 닫지 않는다. Confirm을 열 때 현재 설정 화면은 유지하며, Confirm 표시 중 탭 전환 요청은 무시한다. 옵션을 닫으면 둘 다 숨기고 다음 열기에서 자동으로 탭을 선택하지 않는다.
- 기존 프리팹 배치와 Sort Order를 유지하며 두 컴포넌트와 버튼 참조만 연결한다. 기존 UI 테스트 fixture도 두 화면 참조를 갖도록 갱신했다. 관리 컴파일 확인과 별개로 Unity 재생에서 탭 전환·Confirm 중 유지·옵션 종료 뒤 숨김을 확인해야 한다.

SSOT는 UISystem의 부모·자식 등록과 표시 상태를 재사용하고, UI는 Open/Hide만 담당한다. 추가 서비스·모듈·DB·Core 변경은 없다. Phase 3 연결 후 Unity 확인을 위한 리뷰 지점에서 멈춘다.

## Phase 4 — 사운드 Bus 볼륨 연결 (2026-10-14)

- 기존 Sound 하위 Slider 네 개를 UIValueSlider로 연결했다. Slider의 핸들·Fill·탐색·레이아웃 참조는 유지한다.
- Master → `bus:/`, BGM → `bus:/BGM`, AMB → `bus:/AMB`, SFX → `bus:/PostFX/SFX`. 기존 FMOD Catalog와 Mixer Group 계층을 확인한 경로를 프리팹에 명시했다.
- 범위 0~100, 소수 허용. FMOD 선형 배율은 백분율 / 100이다. 100%는 1(0dB), 50%는 0.5(약 -6.02dB), 0%는 정확히 0(무음, -∞dB)이다. 일정 간격의 dB 곡선을 별도로 두지 않는다.
- 화면을 열 때 IAudioSystemService.TryGetBusVolume으로 실제 사용자 배율을 읽고 SetValueWithoutNotify로 표시한다. 상위 Master 배율이 곱해진 최종 출력값을 다시 Slider 값으로 사용하지 않는다.
- 입력은 기본 Slider의 Set을 확장해 IUISliderReceiver로 직접 전달한다. onValueChanged 구독·Inspector 콜백은 추가하지 않는다. 드래그·키보드 입력은 기본 Slider 경로를 유지한다.
- 값 변경은 IAudioSystemService.TrySetBusVolume → AudioManager.TryGetBus(기존 캐시) → FMOD Bus.setVolume 경로로 즉시 적용한다. UI는 AudioManager나 FMOD를 직접 호출하지 않는다. Bus를 읽지 못하면 해당 Slider를 비활성화하고 경고하며, 다음 화면 열기에서 다시 읽는다.
- 사용자 변경값의 영구 저장·N 선택 시 변경 취소·설정 변경 여부 판정은 기존 TODO로 유지한다. 볼륨의 현재 원본은 FMOD이고, UI에 별도 런타임 볼륨 저장소를 만들지 않았다(SSOT).

Systems·Framework 테스트 어셈블리 컴파일 성공. SoundSettingUITests 6건(초기 읽기 무쓰기, 키보드로 0/50/100%, 재열기, 비상호작용 입력 차단)은 추가·컴파일 완료이며 Unity 실행은 대기한다. 실제 재생에서 네 Bus 각각의 음량·0% 무음·100% 복원·설정 화면 재진입을 확인한다. 기존 Audio 서비스 인터페이스와 모듈 수명을 확장하며 등록·Builder 수명은 변경하지 않았다(FW-001/002/003). KeySetting/SoundSetting의 사용자 변경인 Exclusive 정책을 유지한다. Phase 4 리뷰 지점에서 멈춘다.

## Phase 5 — 기본 KeySetting 표시 (2026-10-14)

옵션 버튼과 공용 ESC를 OptionUI.RequestOpen으로 모았다. UISystem.OpenUI(Setting)으로 부모 등록을 마친 뒤 OpenChildUI(Setting, KeySetting)를 호출한다. 표시 콜백 Open 내부에서 자식을 열면 부모 등록 전이라 실패하므로 요청 경계에서 순서를 보장한다. 옵션을 닫고 다시 열 때 KeySetting을 기본으로 표시하고, 이미 열린 옵션에 대한 중복 열기 요청은 현재 탭을 유지한다. 기존 IOptionCloseRequestService에 연결하는 수신 계약은 열기·닫기를 받는 IOptionInputReceiver로 갱신했다. 추가 서비스 등록이나 프리팹 변경은 없다.

## Phase 6 — Slider Percentage 표시 (2026-10-14)

네 UIValueSlider에 각 Slider 바로 아래 Percentage TMPText를 연결했다. Set 입력을 범위로 제한하고 wholeNumbers 규칙을 적용해 백분율을 계산한 다음, Percentage를 정수 백분율(0%~100%, 표시 반올림)로 먼저 갱신하고 base.Set으로 Slider에 적용한다. Slider와 FMOD에 적용하는 원래 소수 정밀도는 유지한다. SetValueWithoutNotify도 같은 경로를 거치므로 화면 첫 열기·재열기·실패 후 볼륨 복원에서도 문구와 Slider가 함께 갱신된다. TMP.SetText의 숫자 포맷을 사용해 입력 콜백에서 문자열 보간을 하지 않는다. 네 텍스트 참조와 관리 컴파일을 확인하고, Unity에서 실제 표시·드래그·키보드 조절 확인은 대기한다.

## Phase 7 — 옵션 진입 시 뒤의 팝업 보존 (2026-10-14)

입력 반환 리뷰에서 덱 팝업 → 옵션 → 기본 KeySetting(Exclusive)이 덱을 외부 종료시키고, PopupInputSession.End가 현재 Settings Layer를 자신의 Layer로 보지 않아 복귀 요청을 갱신하지 않는 경로를 확인했다. 옵션 반환 뒤 닫힌 LobbyDeckPopup Layer가 남는 현상을 관리 코드로 재현했다.

사용자 승인에 따라 KeySettingUI와 SoundSettingUI를 Parallel로 되돌렸다. 옵션 밖의 덱·상점 팝업은 옵션 진입과 탭 전환으로 닫지 않는다. 두 설정 화면의 상호 배제는 기존 OptionUI 버튼 처리의 상대 CloseUI → 대상 OpenChildUI로 유지한다. PopupInputSession이나 입력 독점 서비스 계약은 변경하지 않는다. 이전 Phase 4의 Exclusive 유지 기록은 이 결정으로 대체한다. Unity에서는 덱 → 옵션 → 탭 전환 → 옵션 닫기 → 덱 닫기 순서로 팝업 보존과 로비 입력 복귀를 확인한다.

## Phase 8 — TimeScale 정지·복원 (2026-10-14)

옵션 Open의 입력 독점 획득 뒤 현재 Time.timeScale을 한 번 저장하고 0으로 설정한다. 실제 OptionUI.Hide 때 이전 값으로 복원하며 Confirm만 닫는 경로에서는 0을 유지한다. 중복 Open/Hide, 초기 숨김은 저장값을 덮거나 임의의 TimeScale을 적용하지 않는다. 열기 실패·비활성화·파괴·런타임 종료는 기존 Hide 정리 경로를 재사용하고, 입력 반환 또는 오디오 복원에 예외가 나도 finally에서 TimeScale 복원을 시도한다. 기존 시간 값이 0·0.5·1·2인 경우의 반복 Open/Hide와 Confirm만 닫을 때 정지 유지 검증을 UI 테스트에 반영했다. 이 변경은 Unity TimeScale 제어이며 별도의 Core Tick 중단 기능은 추가하지 않는다.


## Phase 9 — 변경 없는 옵션 닫기 (2026-10-14)

설계·Task: 옵션 진입 시점을 비교 기준으로 잡고, 닫기 요청에서 키와 사운드의 실제 현재 값을 비교한다. 변경 없으면 UISystem.CloseUI(Setting), 변경 있으면 기존 SaveSettings Confirm을 사용한다. 이미 열린 Confirm은 기존처럼 먼저 닫는다. 변경 후 원래 값으로 복구하면 변경 없음으로 판단한다.

구현: 키는 기존 IKeyBindingService.HasChanges를 재사용한다. 사운드는 SoundSettingUI.BeginEdit에서 기존 buses 목록의 볼륨을 보관하고, HasChanges에서 AudioSystem의 현재 볼륨과 비교한다. Sound 탭을 다시 열어도 기준을 갱신하지 않고, 옵션 Hide에서 EndEdit으로 비교 수명을 종료한다. ESC와 Close Button은 같은 RequestClose를 사용한다. 사운드 저장·취소의 기존 동작은 확장하지 않았다.

검토: SSOT는 키의 런타임 effectivePath와 AudioSystem 볼륨이며, 저장된 초기 볼륨은 비교용 스냅샷이다. Key/Sound가 각 변경 판정을 소유하고 OptionUI는 닫기 분기만 맡는다(SOLID). UIInputBinding과 기존 Submit·UI 이력을 재사용하고 서비스 등록·Builder·Core·FieldCamera·Architecture는 변경하지 않았다(FW 계약 유지).

검증: Systems와 Framework 테스트 어셈블리 관리 컴파일 성공(기존 경고 외 새 경고 없음). Unity EditMode에서 OptionUITests, SoundSettingUITests, KeyBindingTests, OptionInputLayerTests 총 57건 모두 통과. 변경 없음 ESC/Close, 키 변경 Confirm, 사운드 변경/원복, Confirm 우선 닫기, 이전 키 설정의 입력·저장·복구 회귀를 포함한다. 실제 화면 재생 확인은 별도다. Phase 9 완료 후 리뷰 지점에서 멈춘다.


## Phase 10 — 음향 저장 취소 복구 (2026-10-15)

설계·Task: 기존 옵션 Open 시점의 볼륨 스냅샷을 음향 취소에도 재사용한다. 저장 Confirm Yes는 현재 볼륨을 유지하고 편집을 확정하며, No 또는 강제 Hide는 스냅샷으로 복구한다. Sound 탭 Hide/Open은 편집을 확정하거나 비교 기준을 갱신하지 않는다.

구현: SoundSettingUI.EndEdit을 SaveEdit/CancelEdit으로 나눴다. CancelEdit은 처음 읽을 수 있었던 Bus 중 현재 값이 달라진 항목만 AudioSystem.TrySetBusVolume으로 복구하고, 현재 실제 볼륨을 다시 읽어 슬라이더와 Percentage를 SetValueWithoutNotify로 갱신한다. 복구 실패는 경고하고 실제 값으로 표시한다. 반복 취소는 중복 쓰기 없이 끝난다. OptionUI의 저장 Yes에서 SaveEdit을 호출하고 Hide 정리에서 CancelEdit을 호출한다. 음향의 다음 실행 영구 저장은 이번 범위에 추가하지 않았다.

검토: AudioSystem이 현재 음량의 SSOT이며 스냅샷은 취소 목적만 가진다. 음향 편집 수명은 SoundSettingUI, 저장 확인 분기는 OptionUI가 소유한다(SOLID). 기존 명시적 Slider 수신·Audio 서비스·UI 닫기 경로를 사용하며 서비스 등록·Framework 계약·Core·다른 View 작업자 영역을 변경하지 않았다.

검증: Systems 및 Framework 테스트 어셈블리 관리 컴파일 성공(기존 파일 경고 외 새 경고 없음). Yes/No 볼륨 유지·복구와 다시 열기, 탭 재진입 후 네 Bus·슬라이더·% 복구 및 반복 취소의 중복 쓰기 방지 테스트를 추가했다. Unity 배치 EditMode는 프로젝트를 연 다른 Unity 인스턴스로 실행이 차단되어 실제 테스트 결과는 대기다. 이번 변경을 이전 58건 통과 결과에 포함하지 않는다.
