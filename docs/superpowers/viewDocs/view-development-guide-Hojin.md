# View 기능 개발 가이드

- 작성자: 호진(이호진)
- 갱신일: 2026-10-27
- 대상: 비전투 View(로비·스테이지 맵·팝업·화면 입력)에 새 기능을 만드는 사람
- 결정·계약의 원본: [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md). 이 안내는 그 결정을 따라 일하는 순서다. 결정 내용은 여기서 다시 정하지 않는다.

새 View 기능을 만들 때는 먼저 **누가 소유하는지, 언제 끝나는지, 무엇을 정리하는지, 어디서 만드는지**를 정한다. 이 네 답에 따라 System·Scope·Factory·Controller에 책임을 나눈다. 기능마다 필요한 역할만 나누고, Framework의 서비스 등록·수명 단계·UI·입력·카메라 계약은 그대로 쓴다.

전투 View(`ViewSystem/Combat/`)는 용근님 영역이다. 그쪽 파일 위치는 [전투 View 폴더 배치 안내](combat-view-folder-guide.md)를 따른다.

## 1 역할을 나누는 기준

| 역할 | 맡는 책임 | 지금 예 |
|---|---|---|
| System | Framework에서 서비스·View DB를 받고 씬 Enter/Exit에 연결한다. 씬 루트를 한 번 찾아 Scope를 게시하고 Tick·정지·이탈을 전달한다 | `LobbyViewSystem`, `StageMapViewSystem` |
| 씬 Scope | 씬 연결 한 번의 하위 객체·카메라 핸들·외형·입력·팝업을 소유하고 시작·Tick·폐기를 묶는다. 이 연결이 현재인지 판단한다 | `LobbyViewScope`, `StageMapViewScope` |
| 팝업 Controller | 팝업 하나의 표시·입력 요청·결과 소비. 열기·닫기 수명은 자기 `PopupViewScope`에 맡긴다 | `ArtifactPopupController`, `ShopPopupController`, `RecoveryPopupController`, `LobbyDeckPopupController`, `DeckEditPopupController` |
| `PopupViewScope` | 팝업의 UI 열기·닫기, 열림 알림 구독, 입력 토큰·Layer 반환, 폐기를 공통으로 처리한다 | 팝업마다 한 인스턴스 |
| 화면 어댑터 | UI 화면의 `IsOpen`·`OpenStateChanged`를 `IPopupViewScreen`으로 감싼다 | `ShopPopupViewScreen` 등 |
| 팝업 Factory | 고정 의존성으로 팝업 한 벌을 조립한다. 반환한 객체는 호출한 부모 Scope가 소유한다 | `ArtifactPopupViewFactory`, `ShopPopupViewFactory`, `RecoveryPopupViewFactory`, `DeckEditPopupViewFactory` |
| 화면 입력 Factory | 그 화면의 입력 슬롯과 Controller를 잇고 읽기 전용 등록 정보를 돌려준다 | `StageSelectionInputFactory`, `LobbyInputFactory` |

System만 Framework 모듈로 등록한다. Scope·Controller·Factory·어댑터는 View 안의 일반 객체이며 모듈·서비스·서비스 키로 등록하지 않는다. BGM·AMB 호출은 지금 System에 있다.

표시할 상태와 처리 결과는 Buffer에서 읽고, View는 자기 입력 Buffer의 WriteSink에 요청을 기록한다. 게임 규칙 계산은 Core, 표현용 설정·외형은 View DB가 원본이다. Controller나 Scope에 같은 상태·규칙의 원본을 새로 만들지 않는다.

## 2 Scope를 따로 만드는 기준

Scope를 만들지는 **독립된 사용 수명과 정리 경계가 있는가**로 판단한다. 씬 연결, 일정 시간 유지되는 연출 묶음, 따로 끝낼 수 있는 미리보기처럼 시작과 끝이 분명하고 그 기간에 소유할 자원이 있으면 Scope가 맞다.

새 Scope를 정하기 전에 답한다.

1. **소유자는 누구인가?** System·부모 Scope·Controller 중 누가 참조를 보관하고 폐기하는가.
2. **언제 시작하고 끝나는가?** Attach/Detach, 팝업 연결/폐기, 작업 시작/취소 같은 실제 단위. UI를 잠시 닫는 것과 객체를 폐기하는 것은 다르다.
3. **무엇을 돌려줘야 하는가?** 카메라 핸들, 대여 모델, 입력 슬롯·토큰, 이벤트 구독, Delay 예약과 그 순서.
4. **기존 소유자와 수명이 다른가?** 부모와 같은 기간에 쓰는 작은 기능은 기존 Scope·Controller 안에 둔다. 따로 끝내려면 부모 전체를 폐기해야 한다면 분리를 검토한다.

자원 개수로 정하지 않는다. 카메라 핸들 하나는 기존 소유자가 Handle을 돌려주면 충분하다. [Framework Scope 지침](../framework/Framework%20Development%20Guidelines.md#7-scope-설계)을 함께 본다.

### 기존 팝업 Scope 재사용

새 팝업의 열기·닫기·알림·입력 반환은 기존 `PopupViewScope`와 화면 어댑터로 표현한다. 팝업별 내용만 Controller에 쓴다. 팝업마다 같은 상태 머신을 가진 `XxxPopupScope`를 만들지 않는다.

화면을 닫아도 Controller는 연결된 채 다시 열릴 수 있다. Controller와 그 `PopupViewScope`의 폐기는 부모 연결이 끝날 때다. 팝업이 보이는 기간과 객체가 연결된 기간은 다르다.

지킬 것(원본은 [공통 팝업 계약](../specs/2026-10-13-view-structure-design-Hojin.md#42-popupviewscope)):

- 입력 먼저 연다: 호스트 입력 차단 → 팝업 Layer → `OpenUI`.
- 입력 Layer는 **UI 알림 밖에서만** 바꾼다. 외부 숨김·외부 열림의 Layer 처리는 다음 Tick이다.
- 열림 여부의 원본은 UI(`screen.IsOpen`)다. Controller가 따로 들고 맞추지 않는다.
- 같은 화면의 팝업들은 같은 `PopupInputSession`을 쓴다. 인스턴스는 각 팝업의 `PopupViewScope`가 따로 갖는다.
- **UI는 수신자를 부르고, 제출은 Controller가 한다.** UI는 `BindInput`으로 받은 수신자 계약(예: `IShopPopupInput`·`IRecoveryPopupInput`·`IDeckEditInput`)을 자기가 그린 문맥·ID로 부르기만 하고, 번호·명령·"제출 중" 사실을 갖지 않는다. Controller가 수신자를 구현해 제출 **전에** `SetCanSubmit(false)`로 잠그고, 영수증 하나로 결과를 대조한다(원본 [검사 후속 정리 설계 §7.5](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md#75-설계)).

### 폐기와 연결 교체 구분

부모 Scope의 `IsDisposed`는 새 작업을 막고, `IsSuperseded`는 새 연결로 소유권이 넘어갔는지 알린다. 폐기 중이어도 아직 현재 연결이면 자기 Layer를 정리할 수 있다. 새 연결이 게시되면 옛 콜백과 정리가 새 UI·Layer·차단을 바꾸지 못한다. 팝업은 이것을 `IPopupViewHost.IsCurrent`로 묻는다.

재진입, 부분 생성 실패, UI 서비스 호출 중 미뤄지는 정리는 [수명 규칙](../specs/2026-10-13-view-structure-design-Hojin.md#23-수명-규칙)을 따른다. [P3.8 좁은 예외](../specs/2026-10-13-view-structure-design-Hojin.md#33-승인-예외와-거절한-보완-2026-10-15)는 한 조합만 뺀 사용자 승인이다. 일반 재진입 보존 면제로 넓히지 않고, 새 부모가 Layer를 취하지 않아도 옛 Layer가 회수된다고 가정하지 않는다.

## 3 Factory를 따로 만드는 기준

팝업 Factory는 **씬과 무관하게 같은 고정 의존성을 쓰고, 연결마다 조립할 내용이 복잡할 때** 둔다. 인자 수만 줄이는 중간 계층은 만들지 않는다.

| 구분 | 둘 위치 | 예 |
|---|---|---|
| 연결이 바뀌어도 같은 고정 의존성 | Factory 생성자 | Buffer 싱크, UI 서비스, Delay, 오류 경계, 외형 조회, 표시 설정 |
| 운영에 꼭 필요한 의존 | 생성·바인딩 때 검사하고 없으면 예외. "없으면 기능을 끈다" null 갈래를 두지 않는다. 시험은 진짜나 가짜를 넣는다 | 오류 경계, 출발 싱크, 상점·회복·덱 편집 팝업 Factory([필수 의존 결정](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)) |
| 연결마다 달라지는 대상·문맥 | `Create` 인자 | 씬의 화면·루트, 입력 세션, 현재 호스트, 출력 카메라, 맵 변경 알림 |
| 완성된 객체의 소유·Tick·종료 | 반환받은 부모 Scope | Controller 보관, Start·Tick·Dispose |

Factory는 `Create` 중 씬의 표시 설정이나 카메라 연결을 할 수 있지만 씬 객체를 필드에 두거나 만든 객체를 추적하지 않는다. 반환 전 부분 실패는 Factory가 정리하고, 반환한 객체는 부모 Scope가 책임진다. 의존성과 생성이 단순하면 부모 Scope가 직접 만든다(지금 로비 덱 팝업). 기준의 원본은 [설계 D9](../specs/2026-10-13-view-structure-design-Hojin.md#45-팝업별-factory-d9)다.

### 초기화 단계마다 할 일

Framework는 InitData와 PostBindService의 상대 순서를 고정하지 않는다(FW-016). 어느 순서로 와도 같은 결과가 나오게 단계별로 나눈다([설계 §2.4](../specs/2026-10-13-view-structure-design-Hojin.md#24-조립-위치와-초기화-단계)).

| 단계 | 할 일 |
|---|---|
| InitData | View DB 참조를 저장한다. |
| PostBindService | 부모 Context에서 좁은 서비스를 꺼낸다. DB가 필요한 기본 팝업 Factory는 **생성 함수만** 준비한다. resolver·`IBufferQuery`·Context 전체를 붙잡지 않는다. DB가 필요 없는 Factory는 여기서 바로 만들어도 된다(`DeckEditPopupViewFactory`). |
| Initialize | 기본 팝업 Factory를 한 번 조립한다. 명시적으로 주입된 Factory(`BindShop` 등)는 덮어쓰지 않는다. 그 뒤 로딩 콜백(Enter 100·Exit -100·AfterLoad)을 등록한다. |
| Attach(Enter 100) | 씬 루트를 한 번 찾고 검사한 뒤 Scope를 게시·Start한다. Controller·Unity 객체·입력 Bind는 여기서 생긴다. |
| AfterLoad | 카메라 핸들을 빌린다. |

### 두 Factory의 위치

- **System 조립 Factory:** `Systems/Factory/...`. Boot 때 System 생성·서비스 Export·Group/Runtime 구성을 맡는다. 예: `InGameViewSystemFactory`.
- **기능의 팝업 Factory:** 그 기능 폴더. 씬 연결용 팝업 한 벌을 만든다. 예: `ViewSystem/ArtifactPopup/ArtifactPopupViewFactory`, `ViewSystem/StageMap/Shop/ShopPopupViewFactory`.

화면별 객체 생성·등록은 Runtime 루트(`RunTimeSystemFactory`)나 Global Factory에 넣지 않는다. 루트는 그룹 연결만 한다([사용자 규칙 ①](../specs/2026-10-13-view-structure-design-Hojin.md#36-사용자-규칙)).

## 4 입력을 더하는 방법

화면 입력은 그 화면 입력 폴더가 정의·조립하고 ViewGroup이 슬롯을 소유한다([설계 §7](../specs/2026-10-13-view-structure-design-Hojin.md#7-화면-입력-조립과-소유권)). 입력 슬롯·Controller의 일반 사용법은 [스테이지 내부 입력 안내](stage-input-guide.md)에 있다.

| 하고 싶은 것 | 고칠 곳 |
|---|---|
| 기존 화면에 액션 하나 | 그 화면의 `*InputNames`·Controller·(키 설정 대상이면) `*InputKeyBindings`. 물리 키·GUID는 `PlayerInputActions` 에셋 |
| 기존 화면에 팝업 Layer 하나 | 그 화면의 `*InputLayers`와 `*InputFactory`의 등록 목록. 상위 Factory는 Layer 이름을 모르게 둔다 |
| 새 화면 입력 | 새 입력 폴더에 Names·Layers·Controller·슬롯·`XxxInputFactory.Create()`(결과에 `Slot`·`InputRegistrationSet`)를 만들고, `InGameViewSystemFactory.CreateComposition`에서 한 번 부른다. 슬롯이 View 여럿에 필요하면 `IInGameViewInputContext`에 더한다 |
| View에서 슬롯 받기 | PostBindService에서 `resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input.<화면>` |

- Configure는 `UnityInputBridge.InitData`에서 한 번뿐이다. 화면 전환은 Bind/Unbind·`ChangeLayer`/`ClearLayer`만 한다.
- `InputLayer`는 이름으로 같음을 판정한다. Layer 이름은 화면 안에서 하나여야 하고 정의 안에 한 번만 적는다. 스테이지 맵의 상점·회복·덱 편집은 `StageSelectionInputLayers.Popup`("StageMapPopup") 하나를 함께 쓴다. 로비 덱은 `LobbyInputLayers.DeckPopup`이다.
- 슬롯을 루트에 Export하거나, static으로 공유하거나, 슬롯을 얻으려고 Factory를 다시 부르지 않는다.

## 5 지금 코드에서 따라갈 예시

| 기능 | Scope 판단 | Factory 판단 |
|---|---|---|
| 기존 HUD에 수치·아이콘 추가 | 기존 화면 수명 안. Controller·Presenter에 표시 책임 | 기존 조립 경로에서 연결 |
| 단순한 팝업(로비 덱) | 부모 Scope + 기존 `PopupViewScope` | 부모 Scope가 직접 생성 |
| 의존성이 많은 팝업(아티팩트·상점·회복) | 부모 씬 Scope가 Controller를 소유, Controller마다 `PopupViewScope` | 팝업 Factory |
| 새 씬의 View 전체 | 그 씬 연결을 소유하는 `XxxViewScope` | 복잡한 하위 기능에만. 모든 하위 객체에 Factory를 만들지 않음 |
| 따로 취소·종료하는 미리보기·연출 묶음 | 부모보다 짧은 수명과 반환 경계가 있으면 별도 Scope | 생성이 복잡할 때만 |
| 카메라 핸들 하나 | 기존 소유자가 Handle을 보관·반환 | 대여 호출을 감싸는 Factory는 만들지 않음 |

### 덱의 직접 생성

[`LobbyViewScope.Start`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs)가 `LobbyDeckPresenter`와 `LobbyDeckPopupController`를 직접 만든다. [`LobbyDeckPopupController`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs)는 수명을 자기 `PopupViewScope`에 맡긴다. 덱과 아티팩트는 같은 `PopupInputSession`을 쓴다.

### 아티팩트·상점의 Factory 생성

- [`ArtifactPopupViewFactory`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs): 보드·입력·결과 싱크, UI·Delay·외형 조회·설정을 받아 두고, 연결 때 `Create(root, input, host, output, popupOutput)`로 Controller를 만든다.
- [`ShopPopupViewFactory`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/ShopPopupViewFactory.cs): 고정 의존성을 갖고 `Create(screen, input, host, mapChanged, selection)`로 만든다. `selection`은 팝업 종료 뒤 선택 화면 복귀를 맡는 `StageMapSelectionPresentation`이다(스테이지 맵 설계).
- 두 Factory 모두 `LobbyViewSystem`·`StageMapViewSystem`의 Initialize에서 기본 Factory로 조립된다.

```text
로비 연결
LobbyViewSystem.Attach → 검사 → LobbyViewScope 게시 → Start
    ├─ 덱 Controller 직접 생성
    └─ ArtifactPopupViewFactory.Create → Controller를 받아 소유

시설 선택
이미 만든 Controller.Open → 그 PopupViewScope가 UI·입력 수명 처리

씬 이탈 또는 영구 정지
부모 Scope.Dispose → 팝업·상호작용·입력·외형 정리, 카메라 핸들은 마지막
```

Factory는 팝업을 열 때마다 부르는 함수가 아니다. 씬 연결마다 팝업 한 벌을 만들고, 연결 중 열기·닫기는 그 Controller가 처리한다.

### 화면보다 긴 수명: 회복 요청

회복은 요청의 수명이 화면보다 길다. 요청 번호·명령·실행·소비는 씬 밖 런타임 모듈 `RecoveryRequestLifetime`이 소유하고, 화면의 Scope는 이를 소유하지 않는다. `RecoveryPopupViewFactory`는 고정 Query·상태·UI·외형을 받고 연결마다 화면·입력 세션·호스트·맵 변경 알림으로 Controller를 만든다. `StageMapViewScope`가 Controller를 보관·Tick·폐기하고, Controller는 `PopupViewScope`에 UI·입력 수명을 맡긴다. 외부 Hide는 표시 수명만 끝내고 런타임 요청은 지우지 않는다. 세부는 [회복 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md).

### 새 System 없이 Scope 아래 두기: 맵 노드 이동

맵 노드 이동은 새 System·Factory·팝업 Scope 없이 `StageMapViewScope`가 `StageMapNodeMovement`를 직접 만든다. Scope가 끝날 때 이동·lease를 먼저 무효화하고, 착지 뒤 기존 팝업 복원 경로를 쓴다. 이동은 로비와 같은 `Common/ChessPieceMotion`을 쓴다. 세부는 [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md).

## 6 새 기능 개발 순서

1. 기능의 표시·입력·결과를 적고 어느 System과 부모 연결에 속하는지 정한다.
2. 소유자·시작/종료 조건·정리할 자원과 순서를 정한다. 별도 수명이 필요하면 Scope를 나누고, 기존 수명이면 그 소유자 안에 둔다.
3. 고정 의존성과 연결별 인자를 나눈다. 조립이 복잡하면 기능 Factory를, 단순하면 부모가 직접 만든다. 필요한 좁은 Buffer 싱크와 서비스 인터페이스만 받는다. 초기화 단계(§3)에 맞춰 둔다.
4. 팝업은 기존 `PopupViewScope`·화면 어댑터, UI 입력은 `UIInputBinding`·`IUIInputReceiver`를 쓴다(UI는 현강님 규칙, `Assets/AGENTS.md`). 화면 입력은 §4. 씬 Enter/Exit 연결은 [런타임 씬 연결 안내](runtime-scene-binding-guide.md)를 따른다.
5. 이름을 정한다. View 쪽 새 타입 이름에는 View를 넣는다(`XxxViewScope`, `XxxPopupViewFactory`, `XxxPopupViewScreen`). 팝업 동작 객체는 `XxxPopupController`이고, `XxxContent`는 UI 표시 데이터 이름이라 쓰지 않는다([사용자 규칙 ②③](../specs/2026-10-13-view-structure-design-Hojin.md#36-사용자-규칙)).
6. 달라지는 수명 경계를 확인 목록으로 적는다: 정상 닫기와 Dispose, 생성 중 실패, 정리 일부 실패 뒤 나머지 반환, 중복 Dispose, 교체 중 재진입, 옛 수신자·알림·Delay 콜백, 입력 토큰·Layer·대여 자원 반환. 지금 사용자 규칙은 새 시험 코드를 쓰지 않고 MSBuild·Test Runner를 돌리지 않는 것이다(2026-10-23·10-25). 확인은 사용자의 Unity 재생으로 하고 보고에 "컴파일 미확인"을 적는다. 기존 시험은 바꾼 호출만 맞춘다.
7. SSOT·SOLID·JMH/FW 계약·기능 변경 독립성과 작업 소유 경계를 점검한다([설계 §10](../specs/2026-10-13-view-structure-design-Hojin.md#10-원칙-점검)). 설계·계획과 다른 결정이 필요하면 먼저 묻는다. 승인된 Phase를 마치면 멈추고 보고한다. 커밋·push는 사용자가 한다.

시험을 고칠 때: View SO의 `GetData()`가 내부 직렬화 객체를 돌려주는 경우, 시험에서 그 값을 바꾸면 파일 diff 없이 열린 Editor의 운영 설정이 바뀐다. 설정을 바꾸는 시험 준비는 `Object.Instantiate(sourceSO)` 복제본만 고치고 정리한다.

구체적인 확장 요구나 분리 이점이 없으면 인터페이스·범용 Factory 계층을 미리 늘리지 않는다([과도한 추상화 지침](../framework/Framework%20Development%20Guidelines.md#27-과도한-추상화-금지), [새 기능 확인 순서](../framework/Framework%20Development%20Guidelines.md#28-새로운-기능을-추가하기-전-확인-순서)).

## 7 하지 말 것

- 화면 구체 타입(Combat·Lobby·Stage·Shop·Artifact·Recovery)의 생성·등록을 Runtime 루트나 Global에 넣기. 필요해 보이면 설계 단계에서 멈추고 묻는다.
- Scope·Controller·Factory를 Framework 모듈·서비스로 등록하거나 새 서비스 키·이벤트 버스·`XxxViewServices` 묶음 만들기.
- 팝업마다 열기·닫기·알림 처리를 다시 쓰거나 UI 열림 여부를 따로 들고 있기.
- UI 알림 안에서 입력 Layer 바꾸기, 알림 안에서 UISystem 다시 부르기.
- 화면 연결에 대리자 클로저 쓰기(어댑터로 감싼다).
- PostBindService에서 resolver·`IBufferQuery`·Context 전체를 붙잡아 나중에 다시 조회하기.
- 세대 번호·토큰으로 "이 연결이 현재인가"를 따로 관리하기(Scope 객체가 세대다).
- UISystem 폴더(현강님)·Core·Framework 공용 계약을 이 작업 때문에 고치기.

## 8 참고 문서

- [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md): 역할·수명·팝업·입력 조립·책임 분리 결정의 원본, 흐름(§9)
- [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md): 구현 결과·확인 상태·남은 일
- [런타임 씬 연결 안내](runtime-scene-binding-guide.md): System 등록, SceneRoot, Enter/Exit, Boot 경계, 씬 Node 순서 표
- [스테이지 내부 입력 안내](stage-input-guide.md): 입력 슬롯·Controller 사용법
- [로비 설계](../specs/2026-10-11-lobby-design-Hojin.md)·[스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md): 화면별 세부
- [Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md): 역할, Scope, 추상화, 새 기능 확인 순서
- [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md#6-framework-contract): FW 계약과 등록·실행 단계
- [이 프로젝트의 SSOT와 SOLID 적용 기준](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙)
