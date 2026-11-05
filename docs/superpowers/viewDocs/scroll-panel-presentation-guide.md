# 두루마리 자식 UI 프리팹 사용 안내 (2026-11-02)

구현: Codex / Phase 1~4. **컴파일 미확인, Unity 표시·재생 미확인.**

## 배치와 내용 편집

프리팹: `Assets/ProjectL/Prefabs/UI/Common/ScrollPanelPresentation.prefab`.

기존 `UIBase` 화면의 Canvas 아래, **부모 Rect가 화면 전체 크기인 위치**에 이 프리팹을 넣는다. 프리팹 루트는 화면 전체로 Stretch한다. 작은 패널의 자식으로 넣으면 배경도 그 패널 크기로 제한된다. 새 최상위 화면을 만들 경우에는 그 화면 자체의 UIId·ParentID·Policy·서비스 등록/입력 수명을 먼저 연결한다. 이 프리팹을 미등록 팝업 서비스로 사용하지 않는다.

원하는 버튼·글자·목록 등은 다음 위치에 넣는다.

```text
ScrollPanelPresentation
└─ SlideRoot
   └─ RevealViewport
      └─ ContentRoot  ← 여기에 일반 UI 배치
```

- `ContentRoot` 크기는 연출 컴포넌트가 결정한다. 여기에 배치한 UI의 내부 레이아웃은 사용자가 정한다.
- ContentRoot에 펼침 크기를 따라 줄어드는 Layout을 걸지 않는다. Viewport 폭만 변하고 내용의 기준 크기는 고정된다.
- 종이 비율은 `Preferred Size`, 가장자리 안전 여백은 `Content Inset`, 화면 가장자리 여백은 `Screen Margin`으로 조정한다.
- 일반 Image·TMP·Button의 마스크를 켜 둔다. `overrideSorting` 자식 Canvas와 `ignoreParentGroups` 자식 CanvasGroup은 열기 때 구성 오류로 거부한다.
- 프리팹은 편집을 위해 펼친 상태로 저장돼 있다. 실행 초기화 시에는 숨겨지며 `PlayOpen()`을 받아야 표시한다.

## 공개 API

컴포넌트 네임스페이스:

```csharp
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;
```

| API | 의미 |
|---|---|
| `PlayOpen()` | 아래에서 올라오고 좌우로 펼쳐진다. 이미 열린 상태면 그대로 유지 |
| `PlayClose()` | 현재 상태에서 말리고 내려간 뒤 배경을 밝힌다. 이미 숨겨졌으면 false, 완료 알림 없음 |
| `ResetHidden()` | 강제 Hide/씬 이탈용. 즉시 초기화하며 완료 알림 없음 |
| `Opened` | 펼침 완료. 이 시점부터 자식 UI 입력 허용 |
| `Closed` | 말림·퇴장·배경 복원 완료. 그 뒤 기존 화면 닫기 경로를 호출 |
| `IsFullyOpen` | 연출 완료/자식 입력 가능. UISystem의 논리적 열린 상태와 다름 |
| `IsVisible` / `IsAnimating` | 연출 상태 조회 |
| `ContentRoot` | 내용을 넣는 RectTransform 참조 |

열기 중 닫기 또는 닫기 중 열기는 현재 위치에서 방향을 바꾼다. 같은 요청을 반복해도 재생을 처음부터 시작하지 않는다. `Time.timeScale = 0`이어도 연출은 진행한다.

## 기존 화면에 연결

아래는 **기존 UIBase의 해당 훅에 합칠 호출 예시**다. 독립된 새 화면이나 새 UIId를 만드는 예제가 아니다.

```csharp
// 기존 화면의 직렬화 참조
[SerializeField] private ScrollPanelPresentation scroll;

protected override void OnUIOpened()
{
    // 이 화면의 기존 열림 작업도 유지한다.
    scroll.Closed += OnScrollClosed;
    scroll.PlayOpen();
}

protected override void OnUIHidden()
{
    scroll.Closed -= OnScrollClosed;
    scroll.ResetHidden();
    // 이 화면의 기존 숨김/해제 작업도 유지한다.
}

// 기존 Controller가 사용 완료를 판단했을 때 요청한다.
private void BeginNormalClose()
{
    if (!scroll.PlayClose()) OnScrollClosed();
}

private void OnScrollClosed()
{
    // 단순 화면의 예다. PopupViewScope 소유 화면은 그 소유자의
    // 기존 닫기 경로를 여기서 호출하고 Scope가 UI 서비스를 닫게 한다.
    UISystemService.CloseUI(ID);
}
```

`PopupViewScope.Close()`를 먼저 호출한 다음 연출을 시작하면 부모가 즉시 숨겨져 말림이 보이지 않는다. **연출 완료 → 기존 Controller/Scope의 닫기 → UI 서비스 Hide** 순서를 유지한다. 기존 `PopupInputSession`과 호스트 차단도 완료 시점까지 소유 화면에 남겨 둔다.

자식의 사용 완료 버튼은 `UIInputBinding`/`IUIInputReceiver`로 기존 Controller에 전달한다. `Button.onClick.AddListener`나 Inspector UnityEvent를 새로 연결하지 않는다. 이 프리팹은 버튼의 기능과 사용 완료 조건을 모른다.

`CloseUI`, `HideAllUI`, 부모 비활성화, 씬 해제처럼 강제로 닫는 경로는 애니메이션을 기다리지 않는다. 정리 중 `Closed` 알림도 발생하지 않는다. 프리팹은 자식 객체를 파괴하거나 선택 데이터를 초기화하지 않으므로 기능 소유자가 재사용 상태를 준비한다.

## 외형과 소유권

- `ScrollPanelPresentation`: 진행도·상승 위치·마스크·입력 허용 및 배경 복원.
- `ScrollPaperGraphic`: 종이 본체와 양 끝의 접힌 그림을 탑뷰 메시로 배치. 내용 UI는 그리지 않음.
- `ProjectL/UI/ScrollPaper`: 아틀라스의 색·투명도와 UI 마스크 표시. 음영은 그림에 포함하며 실제 3D 카메라를 사용하지 않음.
- `Resources/View/ScrollPanel/CartoonScrollAtlas.png`: 3072×2048 투명 아틀라스. 왼쪽 75%는 크림색 본체, 오른쪽 25%는 갈색 외곽선·접힘 단면이 있는 세로 말림. `PaperShadow.png`와 공용 Material을 함께 사용.
- 외형 원본은 `tools/scroll-panel/cartoon-scroll-atlas.svg`. SVG를 3072×2048 투명 PNG로 내보낸다. Unity PNG importer는 sRGB·알파 투명·Clamp·Bilinear·MipMap 없음·최대 4096·비압축이다. 기존 WornPaperAlbedo/Relief는 보존하지만 현재 Material에서는 사용하지 않음.
- 기존 Canvas/카메라/레이어/UISystem/입력 서비스는 변경하지 않음. 공유 Material을 런타임에 수정하거나 매 프레임 생성하지 않음.

검정 배경은 화면 포인터를 가린다. 게임 단축키와 Core 명령 차단은 기존 입력 소유자의 책임이며 Graphic raycast만으로 대체하지 않는다.

## Unity에서 확인할 항목

1. 새 코드/Shader 임포트 오류·Missing Script·핑크 Material이 없는지 확인한다.
2. 소유 화면을 정상 서비스 경로로 열면 아래에서 상승하고, 펼친 폭만큼 자식 Image/TMP/Button이 보이는지 확인한다.
3. 펼침 중에는 버튼 클릭/Submit이 막히고, 완전 펼침 뒤에만 동작하는지 확인한다.
4. 사용 완료 후 내용이 가려지며 말리고, 종이가 내려간 뒤 검정 배경이 사라지는지 확인한다.
5. 중간 반전·빠른 중복 요청·여러 차례 재사용에서 위치/알파/입력이 남지 않는지 확인한다.
6. 강제 Hide·부모 비활성화·씬 이탈 때 즉시 숨겨지고 완료 콜백이 뒤늦게 오지 않는지 확인한다.
7. 16:9·넓은 화면·작은 창에서 종이가 화면 안에 맞고 내용 크기/마스크가 일치하는지 확인한다.
8. 팝업 동안 게임 입력 Layer가 기존 소유자에 의해 계속 차단되는지 실제 화면 연결 Phase에서 확인한다.

이번 Phase에서는 View Test 코드, MSBuild, Unity 컴파일·재생 검증을 수행하지 않았다.

## Phase 2 — 이미 연결한 화면

2026-11-02 사용자 요청으로 다음 두 프리팹에 공용 프리팹을 **중첩**했다. 이 화면에는 위의 수동 연결 예시를 중복해서 붙이지 않는다.

- `Prefabs/UI/Deck/DeckEditUI.prefab`: 스테이지 맵 덱 편집/출전 전 덱 편집. 열기 버튼은 기존 위치에 유지한다. 드래그 미리보기는 카드와 같은 ContentFrame 좌표계를 사용한다.
- `Prefabs/UI/StageOfferUI.prefab`: 이벤트 노드의 용병·훈련·상점·회복 공통 레이아웃. 개별 카드 이미지/선택 동작은 그대로다.

두 프리팹의 기존 UI 참조를 유지하고 큰 배경만 투명하게 바꿨다. 종이의 인스턴스 설정은 1840×1020, 안쪽 여백은 Phase 3 기준 99.36×175다. 본체 폭 비율 0.84를 적용한 ContentRoot 크기는 이전과 같은 1346.88×670이다. `ScrollContentFit`이 기존 1920×1080 배치를 안전 영역에 비율 유지로 맞추며, 펼침에 따른 자식 레이아웃 재계산은 하지 않는다.

`StageSelect.unity`가 이미 이 두 원본 프리팹을 사용하므로 씬 파일을 별도 편집하지 않았다. 덱 편집 프리팹에 있던 사용자의 Rider 슬롯 머티리얼 변경은 유지한다. 목록 열람 전용 `LobbyDeckUI`는 이번 덱 편집 대상과 다르므로 변경하지 않았다.

`StagePopupViewLifetime`이 정상 종료 연출을 기다리고 기존 Scope를 닫는다. 출전과 맵 갱신은 `CloseThen`의 완료 후속이다. 대기 중 새 작업·새 열기는 막고 모달 입력 수명은 유지한다. 강제 Hide/부모 비활성화/Dispose는 완료 후속을 취소한다. 두루마리 계약이 없는 화면은 기존 동기 닫기 경로를 유지한다.

증강용 `TraitUI`도 Phase 4부터 두루마리를 연결했다. 필수 여부는 소유 화면의 `RequiresScrollPresentation`이 결정하며 StageOfferUI와 TraitGrowthUI 모두 true다. 두 대상 화면의 참조 누락은 예외로 알린다. 두루마리를 사용하지 않는 다른 구현의 null 연출 경로는 유지한다.

추가 Unity 확인: 덱 편집 카드 드래그/슬롯 정보/출전 확인창, 용병·훈련 구매 후 떠나기, 회복 선택·완료, 닫힘 중 중복 입력·씬 이탈, 두루마리를 사용하지 않는 증강 화면의 닫기. **컴파일·재생 미확인**이며 Test 코드와 MSBuild 실행은 추가하지 않았다.

## Phase 3 — 참고 그림 외형 적용

새 참고에 맞춰 얼룩·찢김·요철 음영을 크림색 면, 짙은 갈색 선, 각진 접힘과 말림 단면으로 교체했다. 공통 프리팹과 Material을 통해 덱·이벤트 화면에 함께 적용된다. 기본 프리팹은 x 여백 73.76, 최종 ContentRoot 1062.08×560이며 기존 내용 영역을 유지한다. 종이 PNG의 검정/체커 배경은 포함하지 않는다.

에셋을 조합한 닫힘·중간·펼침 이미지로 실루엣과 팔레트를 확인했고 프리팹 참조와 기존 내부 크기를 정적으로 확인했다. 이는 Unity 출력 확인이 아니다. Unity에서 양 끝 접힘의 표시·종이 면의 색·투명 가장자리·열고 닫을 때 내용 가림·덱과 이벤트의 최종 배치를 확인한다. **컴파일 미확인.**

## Phase 4 — 증강 연결

`Prefabs/UI/TraitUI.prefab` 안에 공용 프리팹을 중첩했다. 기존 Panel/Cards/ErrorPanel을 ScrollContentFrame 안에 옮겼고 카드 3장과 제목·설명·오류 안내의 기존 참조를 유지한다. 크기/여백/ContentFit 기준은 이벤트 화면과 같다. 큰 Panel 배경만 투명화했고 기존 카드 이미지는 유지한다. `TraitGrowth/TraitGrowthUI.prefab`과 StageSelect 씬은 기존 원본 참조를 통해 적용된다.

증강이 처음 열리면 상승·좌우 펼침 뒤 입력을 받고, 다음 제안이 남으면 펼쳐진 종이 안의 내용만 갱신한다. 마지막 선택의 결과 확인 뒤 제안이 없으면 기존 Controller의 Close 요청을 공통 수명 부품이 받아 말림·퇴장·배경 복원을 기다린 후 기존 Scope를 닫는다. 그동안 IsOpen과 입력 세션을 유지한다. 강제 Hide·Dispose·씬 이탈은 즉시 정리한다. UIId.TraitGrowth와 Parallel 정책·선택 요청·결과 확인 방식은 변경하지 않는다.

Unity 확인: 스테이지 진입 시 참조 예외 없음, 증강 최초 열기와 내용 마스크, 펼침 중 입력 차단·펼침 뒤 선택/Submit, 연속 제안의 내용 교체, 마지막 제안 완료 뒤 말림과 암전 복원, 부모 비활성화/씬 이탈, 화면 비율별 배치. **컴파일·재생 미확인.** Test 코드·MSBuild·Unity 실행 없음.
