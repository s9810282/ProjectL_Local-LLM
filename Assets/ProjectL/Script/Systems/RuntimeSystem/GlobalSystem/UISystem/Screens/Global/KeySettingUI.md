# 키 설정 — 설계·Task (2026-10-14)

## 설계

- 대상 6개: EnterCombat → Deployment.EnterCombat, TurnEnd → Combat.EndTurn, AttackMode → Combat.AttackMode, Return → Combat.Return, RotateL/R → Artifact.RotateLeft/Right. Enter(맵 Confirm)와 옵션 ESC는 변경 대상에서 제외한다.
- 일반 4개끼리 중복 금지, 회전 2개끼리 중복 금지. 일반/회전 사이 중복은 허용한다(사용자 확정).
- Tab, CapsLock, NumLock, Fn, Windows, Ctrl, Alt는 지정 불가. 좌우 키를 모두 포함한다. ESC는 키로 지정하지 않고 입력 대기를 끝낸 뒤 옵션 닫기를 요청한다(2026-10-15 변경). Unity에 전달되지 않는 하드웨어 Fn/OS 단축키는 감지할 수 없으며 바인딩에도 포함되지 않는다.
- KeySetting Open: 현재 유효 키 표시, Guid = `변경할 키를 클릭`. ButtonKey 입력: 해당 Text 비움, Guid = `변경할 키를 누르세요.`.
- 금지 키: `지정 불가한 키 입력입니다.`. 중복: `키를 중복 할당할 수 없습니다.`. 변경하지 않고 다음 키 입력을 기다린다. 정상: 런타임 복제본에 override, 키 표시와 기본 안내 복원.
- 다른 ButtonKey 클릭은 이전 대기를 취소하고 기존 키 표시를 복원한다. 탭 이동, 확인창 표시, 비활성화, 종료에서도 캡처를 해제한다. 캡처 중 키 이벤트를 소비해 중복 동작과 UI Submit의 동시 실행을 막는다. ESC는 소비 후 CloseRequested 결과를 KeySettingUI에 전달하며, 키 이름/안내를 복원한 뒤 명시적으로 연결된 OptionUI.RequestClose를 한 번 호출한다. 변경이 있으면 저장 Confirm, 없으면 바로 옵션을 닫는 기존 정책을 유지한다.
- 옵션 Open에서 원래 바인딩을 보관한다. 키 변경은 즉시 적용. 저장 Confirm Yes: PlayerPrefs에 저장하여 재시작에 복원. No 또는 강제 Hide: 옵션 Open 시점으로 복구(사용자 확정). 탭 이동은 변경을 유지한다.

## 구조·계약 검토

- SSOT: 현재 키는 UnityInputBridge 소유 런타임 InputActionAsset의 effectivePath, 기본 키는 원본 inputactions. UI는 키 상태를 별도로 소유하지 않는다. 옵션 편집 시작 값은 취소용 스냅샷만 보관한다.
- SOLID: KeyBindingService가 캡처·검증·override·저장, KeySettingUI가 버튼/문구 표시, OptionUI가 편집 수명과 저장 확인, 루트 Factory가 액션 이름을 조립한다. Global 입력은 Stage/Artifact 구체 타입을 참조하지 않는다.
- FW-001/002/003: 서비스 인터페이스 등록, 기존 Bridge 모듈의 수명으로 초기화/해제, Build 1회. FW-004/005/006/007: RouteTable/Configure는 변경하지 않고 기존 Submit 경로 유지. 물리 키 override만 변경한다.
- 캡처의 키 이벤트 구독은 설정 값 선택을 위한 임시 구독이다. 게임 Controller를 직접 호출하지 않으며, 성공/취소/비활성화/종료에 Dispose한다. 입력 후보마다 키 배열을 새로 만들지 않는다.
- UIInputBinding/IUIInputReceiver 사용. Inspector UnityEvent와 Button.onClick 구독 없음. Core·FieldCamera·Architecture 변경 없음.

## Phase K1 Task

1. 입력 서비스 계약·루트 액션 매핑·Bridge 수명 연결.
2. 키 캡처, 금지/중복 검사, 저장/복원과 편집 취소.
3. 기존 KeySetting 프리팹의 6개 ButtonKey·Text·Guid 연결, Option 저장 확인 연결.
4. 실제 키 입력 이벤트/포인터·Submit 테스트, 관리 컴파일, 프리팹 참조 검사.
5. 결과 기록 후 멈춤. Unity EditMode·실제 재생 확인은 별도 검증 상태로 명시한다.

## 공용 변경 공유

2026-10-15 ESC 변경: KeyBindingResult에 CloseRequested를 추가했다. 캡처가 이벤트를 소비한 상태에서 종료 결과를 전달하고, KeySettingUI가 기존 IOptionInputReceiver로 옵션 닫기를 요청한다. 원래 Settings.Close 액션이 같은 ESC를 다시 처리하지 않도록 유지한다. 서비스 등록·RouteTable·Layer·Core 변경은 없다. Unity EditMode에서 KeyBindingTests, OptionUITests, SoundSettingUITests, OptionInputLayerTests 총 58건 모두 통과했다. 입력 대기 해제·바인딩 유지·단일 닫기 요청·변경 있음 Confirm·변경 없음 바로 닫기를 포함한다.

InputSystemFactory의 선택 인수에 키 정의 목록 추가, InputSystemGroup의 KeyBindings 서비스 접근자 추가, InputSystemServiceKey에 키 서비스 추가. 루트는 기존 StageInputNames/ArtifactInputNames를 재사용하여 6개를 주입한다. 전달 문서는 준비한 상태이며 외부 발송하지 않았다.

## K1 실행·검증 기록

- 6개 버튼, 금지 키·중복 그룹, 안내 문구, 옵션 편집 시작/Yes 저장/No·강제 종료 복구, PlayerPrefs 로드와 저장 데이터 원자적 검증을 구현했다.
- 프리팹의 실제 구조는 일반 항목의 `Button/ButtonKey`, 회전 항목의 `Rotate/ButtonL·ButtonR/ButtonKey`다. 기존 오브젝트 이름과 배치를 유지하고 Button·TMP 참조만 직렬화했다. 6개 고유 액션, Button/TMP 타입, 빈 Inspector 클릭 리스너를 확인했다.
- 최초 Systems·Framework 테스트 관리 컴파일 성공. 기존 파일의 CS0649·CS0105 외 이번 변경의 새 경고 없음.
- 첫 Unity EditMode: 60건 중 54건 통과, 6건 실패. 금지 키·중복 그룹·저장/복구는 통과. 실패는 EditMode의 입력 모드와 일반 MonoBehaviour/EventSystem 수명을 런타임처럼 가정한 테스트 환경에서 발생했다.
- 수정: 키 테스트는 Unity 공식 InputTestFixture로 입력 환경을 격리하고, UI 테스트는 EditMode에서 EventSystem과 OnDisable 수명 알림을 명시한다. 테스트 asmdef에 Unity.InputSystem.TestFramework 참조를 추가했다. 저장 데이터 손상 시 전체 기본값 유지 테스트도 추가했다.
- 수정 후 재실행은 프로젝트를 연 다른 Unity 인스턴스로 인해 중단됐다. 사용자가 저장 후 Unity를 닫고 알려주기로 했다. 최신 재실행 결과와 실제 재생 확인은 아직 대기다.
- 임시 빌드는 재생성 전 Systems DLL과 최신 다른 테스트 소스의 API 차이로 후속 전체 테스트 어셈블리 확인이 불일치했다. 이 오류 때문에 Core나 다른 작업자의 코드를 수정하지 않았다. Unity 재실행에서 의존 어셈블리를 함께 재생성해 확인한다.


### 후속 재검증 (2026-10-14, 옵션 Phase 9)

앞선 재실행 대기를 해소했다. Unity EditMode에서 KeyBindingTests와 OptionUITests, SoundSettingUITests, OptionInputLayerTests 총 57건 모두 통과했다. 공식 InputTestFixture와 명시적인 EditMode 수명 알림 수정, 저장 값 손상 시 원자적 거부, 키 변경 후 원복의 HasChanges 해제까지 검증했다. 최신 Systems·Framework 테스트 관리 컴파일도 성공하여 앞선 임시 DLL 불일치 기록을 대체한다. 실제 재생 확인은 여전히 별도다.
