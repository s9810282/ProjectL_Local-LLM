# BattleChanger 연결 (2026-10-18)

## 설계·Task

- 등록 단위는 Combat 씬의 BattleChanger 버튼 한 개다. UIBase, UIId.BattleChanger(23), ParentID.None, Parallel 정책이며 고정 개방으로 뒤로가기 이력에서 제외한다.
- Phase 1: UI 등록·씬 참조 연결 → 기존 전투 진입 수신 창구 연결 → 단계/대기/연출에 따른 숨김·재표시 → 컴파일·입력 검증. 이번 구현 범위는 이 Phase다.
- 클릭 시 UISystem.CloseUI를 거쳐 숨긴 뒤 IStageInputReceiver.EnterCombatRequested를 호출한다. 키 입력과 같은 Receiver → Router → Gate → 입력 Buffer 경로를 사용한다.
- 클릭 여부와 관계없이 밤 단계, 진입 대기, 결과 표시·후처리 중에는 숨긴다. NightEnd 표시 완료 후 배치 단계이면 다시 연다. 실제 클릭 가능 여부는 기존 CombatInputPolicy.CanSubmit에서 파생한다.

## 조립·수명·규약 리뷰

- 생성 주체: Unity 씬이 컴포넌트를 생성하고 CombatSceneRoot가 직렬화 참조를 보관한다. GlobalSystemFactory는 기존 UISystem만 생성·내부 등록한다.
- 등록: 기존 BindLoadedUI(-200) → UISystem.BindScene의 UIBase 수집·ExportUIService → BindUISystemService/BindUIService 경로를 사용한다. UIInputBinding은 이때 한 번 연결한다.
- 상위 Export: 기존 GlobalSystemGroup.UIServiceCommand를 유지한다. 별도 서비스·루트 재등록·Builder 중복 등록은 없다. 소비자는 UISystem이 주입한 IUISystemService로 개폐한다(FW-001/008/009).
- 전투 View는 씬 설치와 입력 생성이 성공한 뒤 입력 수신 창구를 연결하고, Tick 끝·입력 처리 뒤 기존 facts를 넘긴다. 개폐는 IUISystemService, 실제 SetActive는 BattleChangerUI.Open/Hide만 담당한다.
- 오류·제출 정지 시 숨기고 씬 해제 시 입력 참조를 끊는다. 씬 객체는 기존 씬 수명에 따라 해제한다. UI 없는 가짜 Display 테스트의 루트만 null 참조를 허용하며 운영 씬은 ValidateReferences에서 필수 연결을 검사한다.
- SSOT: 단계·대기·표시 진행 상태는 기존 Buffer/Gate/Playback의 facts다. UI에는 표시 여부 캐시만 둔다. Core·InGame DB를 직접 호출/조회하지 않는다.
- SOLID·독립성: 표현·개폐 판단은 BattleChangerUI에 두고 CombatViewSystem은 기존 사실과 입력 창구만 연결한다. 입력 구현·규칙 판정·키 바인딩은 기존 소유자에게 맡긴다. 공용 변경은 UIId 23 추가이며 새 서비스 등록은 없다.
- CombatActionPanel 확인: NightEnd 이후 SyncInputLayer가 배치 Layer로 복귀하면 CombatActionButtons.Visible=false가 되고 공격·턴 종료·복귀 버튼이 각각 비활성화된다. 패널 루트 자체는 끄지 않는다. 이번 변경에서 기존 패널의 수명은 수정하지 않았다.

## 검증

- MSBuild Compile로 운영 코드 컴파일 확인. 기존 CS0649 경고는 남는다.
- BattleChangerUITests: EventSystem의 왼쪽 클릭/Submit 후 숨김·중복 입력 차단, 외부 진입 대기/밤/밤 종료 표시 중 숨김, 배치 복귀 재표시, 제출 정지 숨김을 검증하는 3개 사례를 추가했다.
- Unity EditMode 실행과 실제 재생 확인은 대기다. 코드 컴파일과 Unity 실행 통과를 구분한다.
