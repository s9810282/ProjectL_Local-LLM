> R4 current status (2026-10-29): CombatActionNotice no longer produces an AP-insufficient message. The old serialized failure enum value remains reserved; it does not restore a combat resource or affordability rule. Non-cost action-use, controller, stale-context and invalid-target notices remain, with existing input/aim/pending/playback gates unchanged. AP-insufficient notice examples below describe the former implementation. Source migration is delivered; native and manual/PlayMode acceptance must be reported separately.

---

The original implementation notes below are historical where they conflict with the R4 status above.

# 공용 안내·호버 패널 — 2026-10-17

## 확정 범위와 설계

- NoticePannel과 HoverPannel을 GlobalUI로 등록하고 UISystem을 통해 텍스트를 표시한다.
- HoverPannel은 HoverTarget의 자식이다. 기존 전투 포인터 입력이 IUITextPresentation.MoveHover로 좌표를 전달하고 패널은 그 좌표로 HoverTarget을 이동한다. 패널은 InputAction·EventSystem·장치를 조회하지 않는다.
- 커서 오른쪽 아래에 Canvas 로컬 단위 16의 여백을 두고 부모 Rect 안에 패널이 들어가도록 보정한다. 화면보다 큰 내용의 크기 조절·스크롤은 이번 범위가 아니다.
- UnHover는 페이드·지연 없이 즉시 패널을 비활성화한다. 씬 재바인딩과 Runtime Shutdown에서도 호버를 정리한다.
- 두 패널의 배경과 TMP Text는 Raycast Target을 사용하지 않는다.
- `ShowNotice(text)`는 명시적으로 HideNotice를 호출할 때 닫는다. `ShowNotice(text, time)`의 자동 닫힘은 아래 2026-10-19 후속 설계를 따른다.

## 책임·규약 검토

- UITextPresentation: 필요한 패널에 텍스트를 전달하고 IUISystemService.OpenUI/CloseUI를 호출한다. 전투 상태·게임 규칙을 알지 않는다.
- TextNoticeUI: TMP Text·활성 상태·HoverTarget 위치를 표시한다. Buffer·InGame DB를 읽지 않는다.
- 조립: GlobalSystemFactory가 서비스를 생성하고 GlobalSystemGroup이 `IGlobalSystemGroupService.UITextPresentation` 접근자로 노출한다(루트에 따로 등록하지 않는다). 서비스 키는 `ServiceKeys.Runtime.Global.GlobalSystemServiceKey.UITextPresentation`이며 Global 내부 Reader에서만 조회한다.
- SSOT: UI 문구는 전달받은 표시 값이며 전투 상태의 사본을 따로 관리하지 않는다. 입력 좌표는 기존 입력 수신 경로에서 전달받는다. 실제 숨김 수명 번호의 원본은 패널이며 소비자는 캐시 무효화용으로 관측한다.
- SOLID: 텍스트 내용 변경은 호출자, 위치·레이아웃 변경은 패널, 의존성 연결은 Factory가 맡는다. IUITextPresentation은 기존 화면 전환 API와 분리된 좁은 서비스다. 기존 IUISystemService 구현자에게 새 메서드를 강제하지 않는다.
- FW-001~003: 인터페이스로 서비스를 등록하고 기존 Builder 수명 안에서 조립한다. 패널은 기존 UISystem 수명을 사용하므로 별도 Framework Tick 모듈을 만들지 않는다.
- FW-008~009: GlobalUI 루트로 등록하고 열기·닫기는 UISystem을 거친다. 뒤로가기 이력에 추가하지 않는다.
- 기능 독립성: 문구 변경은 정보 제공자에, 커서 이동·여백 변경은 TextNoticeUI에 머문다. 등록·공통 수명이 바뀔 때만 GlobalUI 및 조립 회귀를 함께 확인한다.

## Task·Phase

1. Phase ①: 서비스·패널·프리팹 연결, 커서 추적, 즉시 숨김, 공통 GlobalUI 회귀 검증. 여기서 리뷰한다.
2. Phase ②: Object/Terrain/Obstacle의 공개된 Buffer 정보 조회와 텍스트 구성, 기존 전투 포인터 경로 연결. 아래 기록대로 코드 완료, Unity Play 확인 대기.

## 사용

```csharp
IUITextPresentation text = resolver.GetService(SystemGroupsService.Global).UITextPresentation; // Global Context를 거친다.
text.ShowNotice("안내 문구");
text.HideNotice();
text.ShowHover("대상 이름", "대상 설명");
text.MoveHover(screenPosition); // 입력 수신자가 화면 좌표를 전달한다.
text.UnHover();             // 같은 호출 안에서 즉시 비활성화한다.
```

입력 수신자는 좌표가 갱신될 때 MoveHover(screenPosition)를 호출한다. 이 메서드는 숨겨진 패널을 다시 열지 않는다. 전투 환경 Hover는 이 호출을 이미 연결하므로 기존 전투 입력 설정을 바꾸지 않는다. LateUpdate는 전달된 좌표를 다시 적용해 레이아웃 변화에 맞출 뿐 입력을 읽지 않는다.

## 공용 변경 공유 메모 — 민형님, 미전달

- UIId.Notice(20), Hover(21)를 추가했다.
- GlobalUIBase.OpenWithGlobalUI는 기본 true다. TextNoticeUI만 false로 재정의해 OpenGlobalUI 전체 열기에서 제외한다. 기존 GlobalUI의 기본 동작은 유지한다.
- GlobalSystemFactory에 IUITextPresentation 등록, IGlobalSystemGroupService에 UITextPresentation 접근자를 추가했다(2026-10-18 병합 조정, 아래 기록).
- UISystem은 씬 재바인딩 때 Hover를 닫고 Shutdown 때 Notice/Hover를 닫는다.
- Core·FieldCamera·Architecture는 변경하지 않았다.

## 검증 상태

- Unity 6000.0.73f1 Roslyn과 프로젝트 참조로 Systems 및 Framework 테스트 어셈블리 컴파일 확인. 기존 테스트 파일의 중복 using 경고 1건.
- TextNoticeUITests: 초기 숨김·명시적 열기·반복 UnHover·Notice 독립성, 네 모서리 좌표 보정·숨긴 뒤 이동 무시, 실제 프리팹 참조·Raycast 설정 검사.
- Unity EditMode 테스트 실행 및 Play에서의 실제 커서 추적·화면 가장자리·씬 전환 확인은 별도 기록한다.
- EditMode 배치 실행을 시도했으나 동일 프로젝트가 이미 Unity Editor에서 열려 있어 실행 전에 중단됐다. 테스트 결과 XML은 생성되지 않았다. 열린 Editor의 Test Runner에서 TextNoticeUITests, GlobalUITests, UIGroupTests를 실행해야 한다.

### 2026-10-17 HashSet 용량 확장 오류 수정

- Editor 로그에서 최초 예외는 씬 재바인딩의 ValidateHierarchy였다. 계층 검증 완료 플래그가 세워지지 않아 종료 중 OptionInputRouter.CloseUI에서도 같은 예외가 반복됐다.
- Unity 번들 Mono에서 HashSet에 3개 추가 → 1개 삭제 → EnsureCapacity로 확장하면 SetCapacity의 IndexOutOfRangeException을 재현했다. 삭제 없이 확장하면 성공했다.
- 열린 ID 집합의 기존 항목을 삭제·재작성하지 않고, 등록 UI 수가 현재 용량보다 큰 경우에만 새 HashSet을 사전 할당해 기존 열린 ID를 옮기도록 수정했다. EnsureCapacity(0)은 현재 용량 조회로만 사용한다.
- GlobalUITests.RegistrationGrowth_AfterClosingUI_PreservesOpenParentAndChildHistory 추가: UI 닫기 → 등록 32개 추가 → 열린 부모의 자식 열기 → 뒤로가기 → 부모 닫기를 확인한다.
- 같은 회귀 테스트를 Unity 번들 Mono의 관리 코드 실행으로 대조했다. 수정 전 SetCapacity 예외로 실패, 수정 후 통과. 독립 Mono 실행에서는 Unity 네이티브 내부 호출 바인딩 경고가 출력되므로 이 결과를 Unity Editor 전체 실행 검증으로 간주하지 않는다. Systems·테스트 어셈블리 컴파일도 확인했다.

### Phase ② — 환경 Hover 연결

- 사용자 승인으로 Phase ① 규약·GC 점검 뒤 바로 진행했다. HashSet 재확보는 등록 UI 수가 실제 저장 용량을 넘을 때만 새 객체·배열을 할당한다. 용량이 충분한 반복 열기·닫기는 이 재확보 코드를 실행하지 않는다. 등록 직후 첫 조작에서도 재확보될 수 있으므로 항상 무할당이라고 주장하지 않는다.
- 표시 정책: 같은 칸의 공개된 활성 Terrain/Object를 Buffer 순서로, 이어서 해당 칸을 점유한 살아 있는 Obstacle을 하나의 문구로 합친다. 기본 바닥·일반 유닛·미분류 원본은 설명하지 않는다. Obstacle의 위치·체력은 Structures, 종류별 수치는 ObstacleContents, 분류는 EnvironmentBindings를 사용한다. Object/Terrain은 KnownTo에 Player가 있는 원본만 사용한다.
- 이름·설명은 View DB를 사용한다. Obstacle은 기존 UnitVisual의 이름과 Buffer 수치만 사용하며 별도 규칙 설명 테이블을 만들지 않았다.
- CombatEnvironmentText는 조회·문구 구성, CombatEnvironmentHover는 마지막 포인터 좌표와 표시 캐시를 소유한다. Factory가 필요한 좁은 표·집기·가림·텍스트 계약을 연결하고 CombatViewSystem은 입력·결과 표시 수명만 전달한다. Core·FieldCamera·Architecture는 변경하지 않았다.
- EnvironmentVisualKinds로 종류 매핑을 공유한다. 설치형 Terrain 표시가 고지대·약한 지반·낭떠러지를 건너뛰는 기존 동작은 유지하고, 정보창은 이 종류도 조회한다.
- 기존 PointerMoved 경로를 사용한다. UI 가림·필드 밖·설명 없는 칸이면 즉시 UnHover한다. 포커스 손실·입력 취소·Layer 변경·포기 확인·정지·Detach에서도 정리한다. UI 가림은 공유 포인터 Mapper의 재사용 Raycast 경로이며 Object별 Raycast는 추가하지 않았다.
- 결과 미확인·재생·후처리 중에는 숨긴다. 표시 완료 뒤 포인터가 유효하면 최신 Buffer로 다시 만든다. 포커스 손실이나 명시적 취소로 포인터를 비운 경우 새 PointerMoved 전에는 열지 않는다.
- GC: StringBuilder를 재사용하고 칸·결과 번호가 같으면 문자열을 다시 만들지 않는다. 칸·결과 변경 시 문자열 할당은 발생한다. 결과 번호는 문구 캐시 무효화 용도로만 쓰며 Buffer의 소비·확인 상태를 별도로 소유하지 않는다. 실제 UI Raycast·TMP를 포함한 전체 프레임 무할당 또는 기기 성능은 측정하지 않았다.
- 기능 독립성: 문구·조회 변경은 CombatEnvironmentText, 캐시·이탈 정책 변경은 CombatEnvironmentHover, 의존성 변경은 CombatSceneInputFactory에 모인다. 조정자는 수명 전달만 맡는다. 매핑 변경은 Object/Terrain 표시에도 영향이 있으므로 관련 두 ViewSet 회귀를 함께 확인했다.
- 검증: Systems·Framework 테스트 어셈블리 컴파일 통과(기존 중복 using 경고 1건). CombatEnvironmentHoverTests 4건과 ObjectViewSetTests·TerrainViewSetTests를 합해 Unity 번들 Mono의 관리 코드 테스트 39건 통과. Unity Editor의 EditMode·실제 Play 검증과 구분한다.
- Unity 확인: Boot에서 전투 진입 → Object/Terrain/Obstacle에 Hover → 커서 추적과 다중 대상 문구 → UI·필드 밖으로 이동해 즉시 숨김 → 결과 연출 중 숨김과 종료 뒤 갱신 → 포기 확인·옵션·포커스 손실·씬 이동 시 잔상 없음. 기존 Deck/Hand 고정 유닛 정보와 배치·공격·경로 입력도 함께 확인한다.

#### 공용 변경 공유 보충 — 민형님, 미전달

Phase ②는 새로운 Global 계약을 추가하지 않는다. 기존 IUITextPresentation을 Global Context(`global.UITextPresentation`)에서 받아 주입한다. 환경 표현 종류의 공통 매핑만 View 내부에 모았으며 Buffer·Core 계약은 수정하지 않았다.

### 규약 재점검 후 수정 — 2026-10-17

- 이전의 "차단 문제 없음" 판단은 입력 수집 책임·Unity 경계 API 내부 배열 할당·외부 숨김 캐시를 놓쳤다. 이 기록과 위의 현재 설계를 수정 후 기준으로 삼는다.
- TextNoticeUI의 InputSystemUIInputModule.Point 직접 조회를 제거했다. CombatEnvironmentHover가 기존 PointerMoved 좌표를 MoveHover로 전달한다. 같은 칸의 문자열 캐시를 재사용하는 동안에도 위치는 갱신한다.
- 경계 보정은 활성 자식 Rect 전체를 포함하는 기존 의미를 유지한다. GetComponentsInChildren의 재사용 List 오버로드와 Vector3[4] 코너 배열을 사용한다. 새 자식 수가 목록 용량을 넘는 경우 확장은 가능하지만 안정된 계층에서 매 호출마다 RectTransform[]을 만들지 않는다.
- IUITextPresentation.HoverHideVersion을 추가했다. 패널의 Hide·OnDisable이 수명 번호의 원본이다. 외부 UnHover·직접 Disable 뒤 새 포인터 입력 전에는 재표시하지 않고, 이후 입력에서는 같은 칸·결과여도 캐시를 무효화해 다시 표시한다. 연출·UI 가림 때문에 환경 Hover 자신이 일시 숨김한 경우에는 기존 포인터를 유지해 자동 복귀한다.
- 공용 계약 변경은 수명 번호 조회 프로퍼티 하나다. 호출자의 읽기 캐시를 위한 번호이며 이벤트 구독·전투 상태 복제·새 입력 액션·Layer는 추가하지 않았다. 기존 단독 ShowHover 호출은 위치를 스스로 수집하지 않으므로 입력 수신자가 MoveHover를 함께 호출해야 한다. 현재 운영 호출자인 전투 환경 Hover는 이 연결을 포함한다.
- 검증: Systems·Framework 테스트 어셈블리 컴파일, 환경 Hover·Object/Terrain 매핑 관리 코드 테스트 42건 통과. 외부 UnHover 뒤 같은 칸 이동 재표시는 별도 재현 코드에서도 수정 전 false → 수정 후 true를 확인했다.
- Unity Editor 확인 대기: TextNoticeUITests의 실제 Hide/OnDisable 수명, 확대·축소 및 자식이 패널 밖으로 나오는 경계 보정, 워밍업 뒤 MoveHover의 관리 할당 측정. 이 테스트를 추가했으며 컴파일은 확인했지만 네이티브 실행·Profiler 측정 완료로 기록하지 않는다.

### 2026-10-18 병합 조정 — Global Context 접근자

- `feature/Cheon/refactor/core-system`에 병합하면서 루트 재등록을 접근자로 바꿨다. Framework 개발 가이드 §8 "루트에서 Render·UI·Job을 직접 조회하지 않고 Global Context를 거친다"와 같은 방식(`GlobalUIPresentation` 선례)에 맞추기 위한 사용자 결정이다.
- 변경: `IGlobalSystemGroupService.UITextPresentation` 추가, `GlobalSystemGroup.ExportServices`의 `AddService<IUITextPresentation>` 제거, `CombatViewSystem.PostBindService`는 `global.UITextPresentation`을 전달한다. 키는 JMH의 서비스 키 계층 분리에 따라 `GlobalSystemServiceKey`로 옮겼다(옛 `SystemGroupsService.cs`는 삭제됨).
- `IGlobalSystemGroupService`를 구현하는 테스트 대역 4개(CameraEffectSystemTests·CombatEntryIntegrationTests·LobbyDepartureIntegrationTests·LobbyDomainStopTests)에 같은 접근자를 기존 `GlobalUIPresentation`과 같은 방식으로 더했다. 이어진 `feature/Hojin/StageSelcet` 병합에서 들어온 위임형 대역 `ViewPopupCompositionTests.GlobalServices`에는 `_source.UITextPresentation` 위임을 더했다.
- 서비스 동작·패널·호버 수명은 바꾸지 않았다. 검증은 MSBuild 컴파일까지이며 Unity EditMode·Play 확인은 위 항목과 함께 남아 있다.

### 2026-10-19 Hover 이름·설명 분리

- `ShowHover(name, description)`으로 두 값을 명시적으로 전달합니다. Hover의 `title`은 Name TMP, 기존 `message`는 Description TMP에 연결합니다. Notice는 기존 단일 문구를 유지합니다.
- 전투 환경 표시기는 대응하는 Visual SO의 DisplayName·Description을 분리해 전달합니다. 같은 칸의 여러 환경은 같은 순서로 나열하고, 빈 Description은 이름으로 대체하지 않습니다. 일반 유닛은 설명 대상이 아닙니다.
- 패널 열기·닫기는 기존 UISystem 등록 경로를 사용합니다. 생성·내부 등록은 GlobalSystemFactory, 보관·노출은 GlobalSystemGroup의 UITextPresentation 접근자, 전투 조립은 CombatSceneInputFactory의 주입 경로를 유지합니다. 새 서비스·루트 재등록·입력 구독은 없습니다.
- 문구의 원본은 Visual SO 한 곳이며, 이름·설명 분리는 CombatEnvironmentText와 TextNoticeUI에서 처리합니다. 표시 변경 때문에 전투 규칙·행동 조립을 수정할 필요가 없습니다. 필수 Name 참조 누락은 패널 활성화 전에 예외로 알립니다.
- 관련 테스트를 이름·설명 독립 갱신, 숨김 시 초기화, 실제 프리팹 참조 확인에 맞춰 수정했습니다. Unity EditMode·재생 검증은 아직 실행하지 않았습니다.

### 2026-10-19 Notice 자동 닫힘·전투 실패 안내 재설계

이전에 Discard한 Notice 변경을 복원하지 않고, 사용자가 승인한 기존 Delay 활용안으로 구현한다.

#### 설계·단일 Phase Task

1. 기존 IUITextPresentation에 `ShowNotice(string text, float time)`을 추가한다. UITextPresentation이 IDelayService와 안내 버전·종료 수명을 소유한다. TextNoticeUI는 수정하지 않는다.
2. 새 안내(시간 지정 여부 무관)·수동 HideNotice·Shutdown은 이전 예약을 무효화한다. Delay 취소 API를 추가하지 않고 콜백에서 버전·종료 상태를 확인한다. 실행되지 않은 예약은 기존 Delay 만료 시 회수된다.
3. CombatActionNotice가 Core의 공개 실패 사실을 문구와 표시 시간(0.5초)으로 변환한다. 입력은 좁은 ICombatActionRejectionSink로 실패 사실만 전달한다. 조립은 CombatSceneInputFactory, 최종 결과 전달 순서는 CombatViewSystem이 맡는다.
4. 코드·컴파일·관리 코드 회귀·규약 재검사까지 한 Phase로 수행하고, Unity 패널 확인을 위한 리뷰 지점에서 멈춘다.

#### 적용 동작

- 전투 시작 명령의 `InvalidMainCharacter`: `메인캐릭터가 배치되지 않았습니다!`
- 명령 또는 확정한 공격·이동의 `ActionCostInsufficient`: `ActionCost가 부족합니다!`
- 공격 위치·조준·벽 차단 등 명시된 실패: `공격 대상이 없거나 공격할 수 없는 위치입니다`
- 코스트 부족과 잘못된 공격 칸이 겹치면 코스트 안내만 표시한다. 응답 전 클릭·연출 중 차단·단순 미리보기에는 안내하지 않는다. 그 밖의 구성·수명 실패를 공격 위치 오류로 바꾸지 않는다.
- 시간은 기존 Runtime Tick의 게임 시간(Time.deltaTime)이다. 일시 정지 중에는 경과하지 않으며, 0초는 다음 Delay Tick에서 닫힌다. NaN·무한대·음수는 표시 변경 전에 예외로 거절한다.
- Global 유지 패널의 씬 전환 정책은 유지한다. 예약은 Global 서비스 수명에 속하므로 씬 전환으로 시간을 다시 시작하지 않는다. Shutdown 뒤에는 예약이 UI에 접근하지 않는다.

#### 서비스·UI 연결 및 규약 재검사

| 검사 단계 | 실제 경로·판정 |
|---|---|
| 생성 | GlobalSystemFactory가 기존 Utility Factory 반환 Context에서 IDelayService를 받아 UITextPresentation에 주입한다. CombatSceneInputFactory가 전투용 매핑을 생성한다. |
| 내부 등록 | Global Builder에 IUITextPresentation 1회 + 같은 객체의 ISystemShutdown 모듈 1회. IDelayService는 기존 Utility 내부에만 등록한다. |
| 보관·상위 Export | GlobalSystemGroup이 기존 키로 보관하고 IGlobalSystemGroupService.UITextPresentation으로 제공한다. 루트 Export는 기존 Global Context와 모듈뿐이다. |
| 소비자 조회 | CombatViewSystem.PostBindService → SystemGroupsService.Global → UITextPresentation → 씬 입력 Factory 주입. 루트에서 개별 UI·Delay 서비스를 조회하지 않는다. |
| UI 등록 단위 | 기존 TextNoticeUI / UIId.Notice(20), GlobalUIBase 루트(ParentID.None), 기존 Global 정책·뒤로가기 이력 제외·OpenWithGlobalUI=false 유지. 새 패널·UIId·프리팹 변경 없음. |
| 생성·바인딩·개폐 | 기존 UISystem 씬 등록·바인딩·재사용을 유지한다. UITextPresentation → IUISystemService.OpenUI/CloseUI → TextNoticeUI.Open/Hide 경로로만 개폐한다. |
| 해제 | Global의 역순 Shutdown으로 UITextPresentation 콜백을 UISystem 해제 전에 무효화한다. 전투 씬 입력 정리 시 매핑 참조를 놓는다. |

- **FW 필수 계약:** FW-001 인터페이스 등록·동일 Builder 중복 없음, FW-002 새 종료 수명 모듈 등록, FW-003 Build 전 조립, FW-004·008·009 공개 UI 경로와 기존 등록·이력 유지. 변경 범위에서 위반 없음. Framework 기반·Core·Delay·UISystem 구현은 수정하지 않았다.
- **프로젝트 구조 규약:** 하위 서비스 루트 재등록·소비자 루트 우회 없음. 전투 표시 전용 내부 협력 객체는 서비스로 등록하지 않는다. 운영 경로는 기존 Global Context의 필수 등록을 사용하며, 텍스트 없는 단독 전투 표시 테스트에서만 기존 생략 계약을 유지한다.
- **SSOT·SOLID:** 규칙과 비용 판정은 Core/Buffer 원본을 읽는다. 안내 수명은 UITextPresentation 한 곳, 전투 실패→문구 변환은 CombatActionNotice 한 곳, 생성·주입은 Factory에 둔다. 명령 제출·수락 여부나 공격/이동 가능 조건을 추가로 계산하지 않는다. 입력 명령 계약과 기존 UI 서비스 계약의 책임을 유지하며, 입력은 UI 전체 대신 실패 수신 계약만 의존한다.
- **기능 변경 독립성:** 문구·시간 변경은 CombatActionNotice와 해당 테스트에 머문다. 공통 자동 닫힘 변경은 UITextPresentation·패널 수명 테스트만 영향받는다. 의존성 변경은 두 Factory에 모이며 CombatViewSystem은 매핑을 직접 생성하거나 해석하지 않는다. 입력의 거절 시점 연결이 바뀔 때만 공격·이동 회귀가 필요하다.
- **공용 변경 공유 메모(민형님, 미전달):** GlobalSystemFactory에서 기존 Utility Context 주입과 UITextPresentation 종료 모듈 등록만 추가했다. IUITextPresentation에 시간 지정 오버로드를 추가했으며 기존 Global 접근 경로·서비스 키·Group Export는 유지한다. 외부로 전달하지 않았다.

#### 검증·남은 확인

- Systems 및 관련 테스트 어셈블리 C# 컴파일 통과. 기존 FieldOutlineRenderer CS0162·ColorSystem CS0169 경고 2건 외 새 경고 없음.
- CombatActionNoticeTests·CombatInteractionTests·MovementDraftTests의 관리 코드 테스트 총 126건 실행·통과. 비용/잘못된 공격 칸 우선순위, 이동 확정 전후 비용 실패, 연출 시작 클릭 시 예상 체력 정리 회귀 포함.
- TextNoticeUITests에 예약 교체, 수동 숨김/무기한 안내, Hover 독립성, 0초, 잘못된 시간, 종료 뒤 콜백 검증 추가. 컴파일만 확인했으며 Unity 네이티브 EditMode·Play 실행은 대기한다.
- Unity 확인: 전투 진입 전 메인 미배치, AP 부족 공격/이동, 공격 불가 칸, 0.5초 자동 숨김·재표시 시간 갱신·일시 정지 후 재개. 커밋·push하지 않는다.

## 2026-10-19 환경 Hover 설명 변수 치환

- CombatEnvironmentText가 SO 설명을 표시 값으로 구성할 때 공개 Buffer의 사실로 치환한다. 지뢰 `{n}`은 MineSourceFact.Damage, 약한 지반 `{n}`은 StaticTerrainFact.EntryThreshold, `{남은횟수}`는 RemainingEntries를 사용한다. 남은 횟수는 기존 Buffer 계약 값을 그대로 쓰며 View에서 규칙을 중복 계산하지 않는다.
- SO 원문·이름·색상 태그는 유지한다. 빈 설명과 변수 없는 문구의 기존 동작을 유지하며, 정의하지 않은 토큰은 그대로 둔다. 다른 종류의 설명에 지뢰·지반 의미를 적용하지 않는다.
- 등록 단위·UIId·부모·Policy·Factory/Group 조립·Global Context 조회·개폐·씬 수명은 기존 계약 그대로다. 변경 책임은 문구 생성자에 한정하고 Core·InGame DB 조회·공용 텍스트 패널 변경을 추가하지 않는다(SSOT, SOLID, FW 및 기능 독립성 유지).
- 검증: 변경 코드와 관련 테스트를 기존 Unity 어셈블리 참조로 외부 컴파일 완료. CombatEnvironmentHoverTests 관리 코드 14건 통과(새 치환 6건 포함). 피해 0·반복 토큰·색상 태그·원문 보존·진입 횟수 경계·같은 칸의 결과 후 피해 갱신을 확인했다. Unity EditMode 러너·실제 Hover 재생 확인은 대기한다.

### 전체 Object·Obstacle·Terrain 치환 범위 보완 (2026-10-19)

기존 두 종류 한정 구현을 보완했다. 현재 등록된 12종 전체를 검토했으며, 아래 표가 환경 설명 변수의 현재 계약이다. 별도 UI 등록이나 조립 변경 없이 CombatEnvironmentText에서 표시 문구를 완성한다.

| 분류/종류 | SO 변수 | 공개 Buffer 값/처리 |
|---|---|---|
| Object/Mine | `{n}` | MineSourceFact.Damage |
| Object/Flag | `{n}` | FlagSourceFact.PriorityBonus (규칙 평가 Priority와 구분) |
| Object/SheriffBadge | 없음 | 현재 SO의 보호 1회 고정 문구 유지. SheriffBadgeSourceFact에는 보호 횟수 수치 필드가 없음 |
| Obstacle/PowderBarrel | `{n}` | 해당 EntityId의 ObstacleContentProfile.ExplosionDamage |
| Obstacle/Supplies | `{피격공격력증가량}`, `{파괴공격력증가량}` | 해당 프로필 HitAttackBonus, DestructionAttackBonus. 두 값은 독립적이므로 SO의 기존 `{n}` 2개를 의미별 토큰으로 교체 |
| Terrain/WeakGround | `{n}`, `{남은횟수}` | EntryThreshold, RemainingEntries |
| Terrain/Tornado | `{n}` | RepeatIntervalUnitTurns. SO의 고정 3을 변수로 변경. NextDueCompletedTurn은 반복 간격이 아니므로 사용하지 않음 |
| Terrain/PresenceModifier | `{공격력증가량}`, `{최대체력증가량}` | DestinationTileModifierFact.AttackBonus, MaxHealthBonus. 현재 SO 설명은 빈칸이며 기존 숨김 정책 유지 |
| Terrain/HighGround, Sandstorm, Wall, Cliff | 없음 | 기존 고정 문구 유지. 수치 변수가 없으며 규칙 상수를 View에서 새로 복제하지 않음 |

- SSOT·SOLID·FW: 규칙 수치는 공개 사실만 읽고, 표시용 문자열 치환은 기존 문구 생성자가 담당한다. 여러 값을 가진 보급품·위치 보정은 명시적인 의미별 토큰을 사용한다. 서비스 생성/등록/Export/조회, UIId·부모·Policy, 개폐·해제 경로는 기존 책임에 남긴다. Core 수정·InGame DB 조회·범용 패널의 규칙 해석을 추가하지 않는다.
- 검증: 변경 코드와 관련 테스트 외부 컴파일 통과. CombatEnvironmentHoverTests 관리 코드 26건 통과. 12종 전체의 가변/고정 문구, 서로 다른 보급품 보상, 장애물 EntityId·다중 점유 칸, 토네이도 간격과 발동 시점 구분, 위치 보정 음수 및 빈 설명을 포함한다.
- 남은 Unity 확인: DataBaseAssetTests(보급품·토네이도 SO 설명 변경), EditMode 러너 및 실제 Hover 재생. 관리 코드 실행은 Unity AssetDatabase/네이티브 검증과 구분한다.
