# 아티팩트 팝업 UI 이전 설계 — 현강 UI 구조 적용

> **2026-10-06 최신 변경:** 사용자 요청으로 Artifact/Close와 아티팩트 ESC 닫기 연결을 제거했습니다. 팝업은 화면의 뒤로가기 버튼으로 닫습니다. Common/OpenSettings의 ESC 바인딩은 유지합니다. 아래 ESC 구현·검증 기록은 변경 전 이력입니다. 이번 변경 후 실제 Play 재검증은 별도입니다.

- 작성일: 2026-10-06
- 작성: View 작업(스테이지 선택·아티팩트 담당)
- 상태: **사용자 검토용 초안. 구현 전.**
- 브랜치: `feature/Hojin/Stage_Shop_Event`
- 상위 결정: [팝업 계획](../plans/2026-10-05-artifact-popup-plan.md) D1 변경(2026-10-06). 진행·검증 기록은 팝업 계획 §9에만 적는다.

## 0. 결정 (사용자, 2026-10-06)

| 번호 | 결정 |
|---|---|
| U0 | 모든 UI는 현강 작업자의 UI 구조·문서를 따른다 |
| U1 | 팝업의 UI 전체(열기·닫기 버튼, 하단 보유 목록, 이름·효과·상태 문구, 클릭 차단)를 `UIBase` 화면 하나로 옮긴다. 3D 보드·모델·드래그·배경은 View에 그대로 둔다 |
| U2 | 새 `UIId`는 현강님 파일 `UIIDs.cs`에 직접 한 줄 추가하고 공유 문서에 기록한다 |
| U3 | 팝업을 열고 닫는 것은 UISystem(`OpenUI`/`CloseUI`, 뒤로가기 이력)이 맡는다 |
| U4 | ESC로 팝업을 닫는다(뒤로가기). 이번 범위에 넣는다 |
| U5 | 열기 버튼 소유는 현강 문서 기준을 따른다 → `DeckButtonUI.md`의 "UI 컴포넌트가 바깥 버튼 필드를 갖고 별도 자식 Panel만 켜고 끈다" 구조이므로 `ArtifactPopupUI`가 소유한다 |

## 1. 기준

| 기준 | 내용 |
|---|---|
| [Assets/AGENTS.md](../../../Assets/AGENTS.md) | 버튼은 `UIInputBinding`·`IUIInputReceiver`. `Button.onClick.AddListener`·delegate·Inspector UnityEvent 금지. UI 입력에 이벤트 구독을 새로 만들지 않는다. InputSystem이 액션 맵·Layer를 소유한다 |
| [Script README](../../../Assets/ProjectL/Script/README.md) 54·56행 | Stage·Combat 로드 때 LifeCycle 진입 전에 UI 씬 바인딩(`BindScene`) |
| [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-008·009·202, DR-010 | 씬 UI는 `UIBase` 상속·등록. 이력 유지. BindScene은 실제 Hide·구독 해제를 보증하지 않는다 |
| 화면 문서 `DeckButtonUI.md`·`CombatLogUI.md` | 바깥 버튼 + 자식 Panel 토글, Inspector OnClick 미사용, 화면별 `.md`에 Hierarchy·Inspector 표 |
| 선례 `TimelineUI`(현강 `2fde199e`, 미병합) | View 시스템이 `IUISystemService`로 `OpenUI`/`CloseUI`를 부른다. UI는 `BindUIService`에서 Buffer를, `GetUIData`에서 DB를 직접 읽는다 |

## 2. 지금 어긋난 점

1. 팝업 UI가 `UIBase`가 아니고 UISystem에 등록되지 않는다(D1 옛 결정).
2. 열기·닫기 버튼이 `onClick.AddListener`로 연결된다(`ArtifactViewSystem` 197·198행).
3. 목록 슬롯 Hover·Click이 `ArtifactPopupView.DetailRequested` 이벤트 구독으로 전달된다(199행).

## 3. 나누기

| 구성 | 위치 | 맡는 일 | 하지 않는 일 |
|---|---|---|---|
| `ArtifactPopupUI` (새, `UIBase`, `IUIInputReceiver`) | `GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs` (현강 화면 폴더) | Overlay Panel 표시·숨김, 열기·닫기 버튼, 하단 보유 목록 슬롯, 이름·효과·상태 문구, 투명 클릭 차단, UI 영역의 화면 좌표 판정 | 3D 그리기, 입력 Layer 전환, Core 요청, 드래그 판단 |
| `ArtifactViewSystem` (기존) | `ViewSystem/ArtifactPopup/` | 3D 보드·드래그·프리뷰, 입력 슬롯·Layer, Scheduler, 상태 문구의 내용 결정 | UI 오브젝트 직접 조작, 버튼 리스너 |
| `ArtifactPopupView` (기존, 3D 전용으로 축소) | 같은 폴더 | WorldRoot·모델·셀 프리뷰·World Space 배경·맵 표시 숨김 | UI 목록·문구(→ `ArtifactPopupUI`) |
| `ArtifactSceneRoot` (기존) | 같은 폴더 | `popupView`(3D)·`popupUI`·`outputCamera` 참조 보관 | 버튼 참조(→ `ArtifactPopupUI`) |

- 클래스 이름 `ArtifactPopupView`는 씬 컴포넌트 GUID와 테스트를 유지하기 위해 그대로 둔다. UI 필드(`poolArea`·`itemPrefab`·`statusText`·`artifactName`·`effectDescription`)는 `ArtifactPopupUI`로 옮긴다.
- 네임스페이스는 CLAUDE.md 규칙대로 폴더 경로(`ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage`)를 쓴다. `StageUI`와 같다. (현강님 `DeckButtonUI`·`TimelineUI`는 `ProjectL.Systems`인데 서로 달라 경로 규칙을 따른다.)

## 4. 연결

```text
[열기]  열기 Button ─UIInputBinding(slot Open)─► ArtifactPopupUI.Click
            └─ host.CanOpen 이면 UISystem.OpenUI(UIId.Artifact)
                 └─ ArtifactPopupUI.Open(): Panel 켬, 목록 그림 → host.PopupOpened()
                      └─ ArtifactViewSystem: 3D 켬, 입력 슬롯 Bind, Layer = Artifact, 프리뷰 무효화, 그림

[닫기]  닫기 Button(slot Close) → UISystem.CloseUI(UIId.Artifact)
        뒤로가기 BackCloseUI / HideAllUI 도 같은 경로
            └─ ArtifactPopupUI.Hide(): Panel 끔, Hover·상세 정리 → host.PopupHidden()
                 └─ ArtifactViewSystem: 드래그 취소, 입력 해제, 프리뷰 무효화, 3D 끔,
                    Layer가 Artifact일 때만 StageSelection 복귀(D2)

[씬 이탈] ArtifactViewSystem.Detach(Exit -100)
            1) ui.Disconnect()  — 이후 Hide가 host를 부르지 않게 먼저 끊는다
            2) 열려 있으면 UISystem.CloseUI(UIId.Artifact) — 이력 정리 + Panel 끔
            3) 자체 정리: 대기 요청 취소, 3D 끔, Layer가 Artifact일 때만 ClearLayer(복귀 안 함, D2)
```

### 4.1 두 좁은 계약 (명시적 수신자 연결, 이벤트 구독 아님)

```csharp
// View → UI 가 쓰는 것. 구현: ArtifactPopupUI
internal interface IArtifactPopupScreen
{
    void Connect(IArtifactPopupHost host);   // AttachScene에서 한 번
    void Disconnect();                       // Detach 첫 단계
    void ShowStatus(string message);         // 문구 내용은 View가 정한다
    void ShowDetail(int artifactId);         // 이름·설명 조회는 UI가 한다(DB 한 곳)
    bool TryPickPool(Vector2 screen, out int artifactId);
    bool IsReturnArea(Vector2 screen);
    bool IsBlockingArea(Vector2 screen);     // 닫기 버튼·상세 영역
}

// UI → View 가 쓰는 것. 구현: ArtifactViewSystem
internal interface IArtifactPopupHost
{
    bool CanOpen { get; }                    // 연결됨·오류 아님·보드 초기화 대기 허용
    void PopupOpened();
    void PopupHidden();
}
```

- 연결 방식은 `UIInputBinding.Bind`·입력 슬롯 `Bind`와 같은 "수신자를 한 번 연결" 방식이다. C# `event`를 쓰지 않는다(AGENTS.md, DR-008).
- 두 인터페이스는 같은 어셈블리(`ProjectL.Systems`)라 `internal`로 둔다. 위치는 `ViewSystem/ArtifactPopup/`.

### 4.2 UI가 직접 읽는 것 (현강 방식)

- `BindUIService(resolver)`: `BufferSystemKey.Buffer`의 `ArtifactReadSink`(보유 목록), `SystemGroupsService.Global`의 `UIServiceCommand`(열기·닫기 호출용. `UIBase`의 서비스 필드는 private이라 resolver에서 받는다).
- `GetUIData(query)`: View DB `ArtifactViewTableQuery`(아이콘·이름·설명).
- 목록은 열려 있을 때만 스냅샷 참조가 바뀌었는지 보고 다시 그린다(DeckButtonUI의 `Update` 비교 방식).
- View 시스템도 같은 Buffer·DB를 읽지만 각자 원본을 읽을 뿐 사본을 동기화하지 않는다(SSOT).

### 4.3 입력

| 대상 | 방식 | slot |
|---|---|---|
| 열기 버튼 | `UIInputBinding.Attach(openButton, this, Open)` | -1 |
| 닫기 버튼 | 같은 방식 | -2 |
| 목록 슬롯 | 슬롯 프리팹에 `Button`(Transition None) 추가, Hover·Click → 상세 표시 | 0 이상 |
| 드래그 시작·놓기·회전·우클릭 회수 | 기존 InputSystem `Artifact` Layer → `ArtifactViewSystem` (변경 없음) | — |
| ESC (U4) | InputSystem `Artifact` 맵에 새 액션 `Close`(`<Keyboard>/escape`) → `ArtifactInputController` → `IArtifactInputReceiver.Close()` → `ArtifactViewSystem`이 `IUISystemService.BackCloseUI()` 호출 → `Hide` → `PopupHidden` | — |

- **열기 버튼(U5):** `DeckButtonUI.md`와 같은 구조다. `ArtifactPopupUI` 컴포넌트에 `Open Button` 필드를 두고, 버튼은 Panel 밖·`ArtifactPopupUI` 루트 아래에 둔다. 루트는 항상 활성이고 `Open`/`Hide`는 자식 Panel만 켜고 끈다(Deck 문서: "Panel은 별도 자식, 루트 자체를 연결하면 안 됨". 기존 D13과도 같음).
- **`Hide`가 루트를 끄면 안 되는 이유:** UISystem은 `BindScene`·`Initialize` 때 열리지 않은 등록 UI를 모두 `Hide()`한다(`HideClosedRegisteredUI`). 루트를 끄면 열기 버튼까지 사라진다. 이때는 host가 아직 연결되지 않았으므로 `PopupHidden`을 부르지 않는다. `Hide`는 여러 번 불려도 안전해야 한다.
- **ESC 경로의 근거:** AGENTS.md "InputSystem owns action maps, shortcuts, and input layers". 뒤로가기는 FW-009대로 `IUISystemService`를 거친다(`UIBase.Hide` 직접 호출 금지). 액션 이름 `Close`는 기존 `Settings` 맵의 `Close`(ESC)와 같은 이름 규칙이다. 입력 슬롯은 팝업이 열려 있을 때만 연결되므로 닫힌 상태의 ESC는 이 경로로 오지 않는다.
- **ESC 겹침 주의:** `Artifact` Layer는 `Common` 맵도 켠다(`ArtifactInputLayers`). `Common/OpenSettings`도 ESC다. 지금은 이 액션을 등록한 Controller가 없어 전달되지 않는다(`UnityInputBridge` 192행). 설정 화면이 구현되면 ESC가 두 곳으로 갈 수 있으므로 입력 담당(용근님)에게 공유한다. 이번에 `Common` 맵이나 Layer 구성은 바꾸지 않는다.
- 드래그 중 ESC: `Hide` → `PopupHidden`에서 기존 닫기 정리(드래그 취소·프리뷰 무효화)를 그대로 탄다. 대기 중 편집은 기존 D15대로 계속 실행된다.
- `DetailRequested` 이벤트와 `ArtifactPoolItemView`의 포인터 콜백은 없앤다.

## 5. UIId

- `UIId.Artifact = 13`. 12는 현강님 미병합 브랜치(`origin/feature/La/AudioSystem_R`, `origin/feature/Yonggeun/Stage`)의 `Timeline`이 쓰고 있어 피한다.
- 공용 파일 한 줄 변경이므로 공유 문서에 기록한다(U2).

## 6. 점검

| 항목 | 결과 |
|---|---|
| SSOT | 보유 목록 원본은 `ArtifactBuffer`, 문구 원본은 View DB. 상세 문구 조회는 UI 한 곳, 상태 문구 내용 결정은 View 한 곳 |
| S | UI = 2D 표시·UI 입력, View = 3D·입력 Layer·Core 요청. 현재 `ArtifactPopupView`의 UI·3D 혼합을 나눈다 |
| I | View가 UI에 요구하는 것과 UI가 View에 요구하는 것을 각각 좁은 인터페이스로 둔다 |
| D | 서로를 씬 검색으로 찾지 않는다. SceneRoot 참조를 Attach 때 한 번 받고 `Connect`로 넘긴다 |
| FW-008 | `UIBase` 상속, 기본 `ExportUIService` 사용, Initialize 뒤에만 `OpenUI` |
| FW-009 | 일반 화면으로 열어 이력에 넣는다. 뒤로가기로 닫혀도 `Hide` → host 경로로 Layer가 돌아온다 |
| DR-010 | BindScene이 Hide를 보증하지 않으므로 씬 이탈 때 View가 명시적으로 `CloseUI`한다 |
| AGENTS.md | onClick·이벤트 구독 제거, `UIInputBinding` 사용. ESC는 InputSystem 액션으로 받는다(단축키는 InputSystem 소유) |
| 키 SSOT | ESC 바인딩은 `PlayerInputActions` 한 곳, 액션 이름은 `ArtifactInputNames` 상수 한 곳 |
| 과한 추상화 | 인터페이스 2개는 각각 실제 구현 1 + 테스트 가짜 1이 쓴다 |

## 7. 영향

- 코드: `UIIDs.cs`(1줄), 새 `ArtifactPopupUI.cs`·`IArtifactPopupScreen.cs`·`IArtifactPopupHost.cs`, `ArtifactViewSystem`·`ArtifactPopupView`·`ArtifactSceneRoot`·`ArtifactPoolItemView`·`IArtifactPopupPresenter` 수정.
- 입력(U4): `PlayerInputActions.inputactions`의 `Artifact` 맵에 `Close` 액션·ESC 바인딩, `ArtifactInputNames`·`ArtifactInputController`·`IArtifactInputReceiver`(`Close()` 추가)·`ArtifactInputSlot` 전달, 입력 테스트의 가짜 수신자.
- 씬: `StageSelect.unity`의 팝업 Canvas에 `ArtifactPopupUI` 추가, UI 참조를 옮겨 연결, 슬롯 프리팹에 `Button`.
- 테스트: `ArtifactPopupViewTests`·`ArtifactViewSystemTests`(PresenterSpy)·`ArtifactPopupBootSmokeTests`(필드 리플렉션) 수정. 새 테스트는 §8.
- 문서: 현강 형식의 `Screens/Stage/ArtifactPopupUI.md`, 팝업 계획 D1·§9, 입력 안내 상단·액션 표(ESC), 공유 문서(현강님: UIId 13 / 용근님: Artifact 맵 `Close`와 `Common/OpenSettings`의 ESC 겹침).

## 8. 검증

- EditMode: Open 경로(Click → OpenUI → Open → PopupOpened → Layer Artifact), 닫기 버튼·ESC(`Close` 입력 → BackCloseUI)·HideAllUI 모두 Layer 복귀, 드래그 중 ESC는 요청 없이 정리, 씬 바인딩 때의 `Hide`는 루트·열기 버튼을 끄지 않고 host를 부르지 않음, 입력 액션 파일에 `Artifact/Close` 바인딩 존재, `CanOpen` false면 열지 않음, Detach 순서(Disconnect 뒤 CloseUI → Layer는 ClearLayer만), 오류 상태 입력 거부 유지, 버튼이 onClick 리스너 0개.
- Boot smoke: Boot → StageSelect → 열기·닫기·재열기, 다른 씬 왕복 뒤 BindScene 재등록.
- Play(사용자): 실제 마우스로 열기·닫기·뒤로가기, 목록 Hover 상세, 배치·회수 회귀.

## 9. 열린 질문

- (해결, U4) ESC는 이번 범위에 넣는다.
- (해결, U5) 열기 버튼은 현강 `DeckButtonUI.md` 구조를 따라 `ArtifactPopupUI`가 소유한다. 현강님 문서에 StageSelect 공용 HUD 규칙이 생기면 그 문서를 따른다.
- 남은 것: ESC와 `Common/OpenSettings` 겹침은 설정 화면 구현 때 입력 담당과 정한다(§4.3).
