# 두루마리 자식 UI 프리팹 설계 — 상승·펼침·닫힘 (2026-11-02)

작성: Codex / 사용자 요청에 따른 신규 View 기능.

## 1. 요구와 범위

- 열기: 검정 반투명 배경 → 화면 아래에서 종이 상승 → 좌우로 펼쳐지며 자식 UI 노출.
- 표시: 낡고 찢어진 종이. `ContentRoot` 아래의 일반 uGUI/TMP UI를 원래 배치로 표시한다.
- 정상 종료: 입력 차단 → 자식 UI가 종이 안으로 가려지며 말림 → 아래로 퇴장 → 배경 밝아짐.
- 미리보기의 탑뷰 외형을 유지한다. 이번 요청의 상승이 이전 낙하 시안을 대체한다. 종이를 눕히는 회전은 없다.
- Phase 1은 재사용 가능한 **기존 화면 내부의 연출 위젯 프리팹**이다. 특정 로비/전투 화면 교체, UIId 추가, 게임 규칙 연결은 하지 않는다.

## 2. 등록과 수명

`Assets/AGENTS.md`의 화면 등록 규약을 따른다. 독립 화면을 미등록 MonoBehaviour로 대체하지 않는다.

- 등록 단위는 이 위젯을 포함하는 기존 `UIBase` 화면이다. UIId·ParentID·Policy·생성/등록/서비스 바인딩 시점은 그 화면이 소유한다. 위젯에는 별도 UIId/서비스/Framework 모듈이 없다.
- 생성은 화면 프리팹 조립 또는 해당 화면 Factory가 맡는다. `ScrollPanelPresentation`은 자식 참조만 사용한다.
- 열기: `IUISystemService.OpenUI(ownerId)` → 화면 내부에서 `presentation.PlayOpen()`.
- 정상 종료: 화면의 기존 Controller가 사용 완료를 판단 → `PlayClose()` → `Closed` 알림 → 소유자의 기존 `IUISystemService.CloseUI(ownerId)` 경로.
- `CloseUI`, `HideAllUI`, 씬 해제 등 강제 Hide는 동기 계약을 유지한다. 소유 화면은 `ResetHidden()`으로 즉시 초기화한다. 강제 정리는 정상 닫힘 완료 알림을 발생시키지 않는다.
- 기존 `PopupViewScope.Close()`는 동기 닫힘을 전제로 한다. 이번 Phase에서 수정하지 않는다. 실제 화면 연결 시에는 그 메서드를 호출하기 **전에** 닫힘 연출을 완료해야 한다. 입력 세션도 이때까지 기존 소유자가 유지한다.
- 화면 전체의 열림/이력과 위젯의 연출 진행은 다른 사실이다. `IsFullyOpen`은 입력 가능한 연출 완료 여부이며 UISystem의 열린 상태를 대신하지 않는다.

## 3. 렌더링과 계층

이번 요구는 카메라 회전 없는 탑뷰와 임의 자식 UI다. 별도 카메라·RenderTexture 대신 **uGUI 메시와 종이 전용 음영 셰이더**를 사용한다. 말린 끝의 곡면 음영과 고해상도 색/요철 텍스처로 입체감을 낸다. 이전 답변의 RenderTexture는 가능 방식 소개였으며 구현 확정안은 아니었다. 실제 3D 시점 회전은 이번 범위 밖이다.

```text
ScrollPanelPresentation  (RectTransform / CanvasGroup / 연출 컴포넌트)
├─ Dim                  (Image: 화면 전체 검정 배경)
└─ SlideRoot            (RectTransform / CanvasGroup)
   ├─ Shadow            (RawImage: 부드러운 접지 그림자)
   ├─ Paper             (ScrollPaperGraphic: 종이와 양쪽 말림)
   └─ RevealViewport    (RectMask2D)
      └─ ContentRoot    (RectTransform / CanvasGroup: 사용자가 UI를 넣는 곳)
```

- Canvas를 내장하지 않는다. 기존 화면의 Canvas 아래에 넣는다. 기존 Canvas·FieldCamera·레이어·Render 설정을 바꾸지 않는다.
- 전체 화면 크기에 맞춰 종이를 축소하되 종이의 가로세로 비율은 유지한다.
- ContentRoot는 항상 펼친 상태의 고정 크기다. 마스크 폭만 종이 펼침 정도에서 계산한다. 글자·버튼을 늘리거나 레이아웃을 재배치하지 않는다.
- 내용은 찢김·구멍보다 안쪽의 안전 여백에 배치한다. 범용 자식 UI를 종이 알파로 다시 잘라내는 기능은 포함하지 않는다.
- `overrideSorting`으로 마스크를 벗어나는 Canvas, `ignoreParentGroups`로 입력 차단을 우회하는 CanvasGroup, 비 Maskable Graphic은 열기 때 구성 오류로 거부한다. 화면 밖 별도 Dropdown 팝업은 이 위젯의 클리핑 보장 대상이 아니다. 일반 Image/TMP/Button은 기존 uGUI 마스크를 따른다.
- 텍스처는 미리보기의 생성 결과를 PNG 에셋으로 저장한다. 실행 중 종이 노이즈를 생성하지 않는다.

## 4. 상태·입력·반복 호출

- 연출 진행도 하나(`0 = 숨김`, `1 = 완전 펼침`)와 요청 방향이 원본이다. 배경 알파·상승 위치·종이 폭·자식 마스크·입력 가능 여부는 파생한다.
- 열기/닫기 중 반대 요청은 현재 진행도에서 방향만 바꾼다. 같은 요청을 반복해도 중복 재생/완료 알림을 만들지 않는다.
- `Time.unscaledDeltaTime`을 사용해 일시정지 중에도 연출한다.
- 자식 입력은 완전 펼침에서만 허용한다. 닫기 요청 즉시 비활성화한다. 배경 포인터는 연출 시작부터 완전 종료까지 차단한다.
- 게임 단축키/입력 Layer 차단은 기존 화면·PopupInputSession의 책임이다. 검정 Image의 raycast가 게임 입력까지 차단한다고 간주하지 않는다.
- 배경 클릭·ESC 자동 종료, 사용 완료 자동 판정은 넣지 않는다. 자식 기능은 기존 입력 수신 경로에서 소유 Controller로 사용 완료를 전달한다.
- 비활성화/파괴 시 초기화하고 완료 알림을 취소한다. 다시 사용할 때 자식 UI를 파괴하거나 재생성하지 않는다.

## 5. SSOT·SOLID·FW·독립성 검토

| 항목 | 적용 |
|---|---|
| SSOT | 진행도는 Presentation만 소유. Graphic/Mask/Dim은 파생값. 자식 기능의 선택·완료 상태를 복제하지 않음 |
| S | Presentation은 연출 수명/레이아웃, Graphic은 종이 메시, Shader/텍스처는 외형, 기존 Controller는 기능 완료와 화면 전환 |
| O | ContentRoot의 UI/프리팹 Variant 교체로 기능 확장. 공통 연출에 상점/현상수배 분기를 추가하지 않음 |
| L | UIBase의 동기 Open/Hide·이력 계약을 수정하지 않음. 비활성화 초기화와 정상 완료 알림을 구분 |
| I | 위젯 API는 PlayOpen/PlayClose/ResetHidden 및 완료 알림. Core/전체 서비스 저장소를 받지 않음 |
| D | 화면이 명시적 참조로 위젯을 연결. 씬 검색·전역 singleton·서비스 우회 없음. 현재 교체 필요 없는 Graphic에 추상 계층을 추가하지 않음 |
| FW-001~003 | 새 서비스/Builder/모듈 없음. 등록과 상위 Export 추가 없음 |
| FW-004~009 | 기존 소유 화면의 UI/입력 경계 보존. Button은 UIInputBinding/IUIInputReceiver 경로를 유지 |
| FW-010~012 | UI CanvasRenderer가 소유하는 위젯 메시. Framework MeshHandle/Effect/Light 자원을 대여하지 않으므로 해당 대여 계약 적용 없음 |
| FW-013~016 | Framework 이벤트/Pool/Queue/Runtime 구조 변경 없음. 완료 알림 구독 해제는 소유 화면 책임 |

변경 시나리오: 종이 색/결은 텍스처·Material만, 말림 형태는 Graphic만, 열림 시간은 Presentation 설정만, 자식 기능은 ContentRoot와 기능 Controller만 변경한다. 조립 변경은 프리팹 또는 화면 Factory에만 둔다. 공통으로 남는 경계는 종이 크기·안전 여백·펼침 폭이며, 이 계약을 바꾸면 자식 배치/마스크를 함께 확인한다.

## 6. 검증과 제외

- Test 코드·MSBuild·Unity 컴파일/Play 확인은 수행하지 않는다. 코드/GUID/프리팹 참조의 정적 확인과 Unity 확인을 구분한다.
- 사용자가 Unity에서 열기/닫기/중간 반전/재사용/부모 비활성화/화면 비율/마스크 밖 입력을 확인한다.
- Core, FieldCamera, Architecture, 기존 화면/공용 UI 계약, 다른 작업 중 변경은 수정하지 않는다.

기준: `Assets/AGENTS.md`, Framework 프로그래머 가이드 FW-008/009, 설계 세션 인계 §4.6. API 근거: [RectMask2D](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.RectMask2D.html), [CanvasGroup](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CanvasGroup.html), [unscaledDeltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-unscaledDeltaTime.html).

## 7. Phase 2 적용 — 덱 편집과 이벤트 화면 (2026-11-02)

사용자가 실제 연결 대상으로 덱 편집과 이벤트 노드 화면을 지정했다. `DeckEditUI`는 DeckEdit/StageSelect 관계, `StageOfferUI`는 Shop 또는 Recovery/StageScreenRoot 관계를 그대로 유지한다. 화면의 기존 생성·등록·바인딩 시점과 Factory/Group/서비스 조회 경로를 변경하지 않는다. 콘텐츠는 중첩된 공용 프리팹의 ContentRoot 아래 고정 크기 프레임으로 옮기고, 프레임 맞춤 부품이 표시 영역에 맞춘다. 펼침 마스크의 크기로 기능 레이아웃을 다시 계산하지 않는다.

`IScrollPopupDisplay`는 연출 참조만 제공하는 선택적 표시 계약이다. 두 대상 화면과 기존 PopupViewScreen 어댑터가 제공한다. StagePopupViewLifetime은 해당 계약이 있을 때만 정상 닫기를 대기하고, 없으면 기존 동기 닫기를 유지한다. 진행도는 여전히 ScrollPanelPresentation 하나가 소유하며, 수명 부품은 완료 시 실행할 닫기 요청/후속 동작만 소유한다. 새 인터페이스는 자식 기능 판정이나 서비스 저장소를 노출하지 않는다.

정상 종료는 `닫기 요청 → 새 기능 입력 차단 → 말림/퇴장 완료 → 기존 Scope.Close → 기존 후속 동작` 순서다. 덱 출전·맵 갱신은 완료 후속으로 보존한다. 닫기 대기 동안 CanWork/CanOpen은 false이며 기존 모달 입력 세션은 유지한다. 외부 동기 Hide·씬 해제·Dispose는 대기를 취소하고 후속 동작을 버린다. 오래된 화면의 Closed가 다음 화면을 닫지 않게 콜백을 해제한다. PopupViewScope와 UISystem 자체의 동기 닫기 의미는 변경하지 않는다.

변경 독립성: 콘텐츠/레이아웃 수정은 대상 프리팹·기존 View만, 공통 말림 외형은 기존 ScrollPaperGraphic/Material만, 닫기 수명은 StagePopupViewLifetime만 수정한다. Controller 변경은 닫기 직후 동작을 완료 후속으로 전달하는 부분과 닫기 대기 입력 차단에 한정한다. 두루마리 없는 증강 화면의 동기 닫기 경로도 영향 확인 항목에 포함한다. Core/FieldCamera/Architecture/공용 UI 등록 계약은 수정하지 않는다.

## 8. Phase 3 참고 이미지에 맞춘 외형 교체 (2026-11-02)

사용자가 새로 제공한 닫힌 두루마리/펼친 두루마리 이미지를 외형 기준으로 사용한다. 크림색의 깨끗한 면, 짙은 갈색 외곽선, 황갈색 접힘 면, 두툼한 말림과 끝 단면을 적용한다. 이전 얼룩·찢김 요구는 이번 참고의 외형으로 대체하며 열림/닫힘과 UI 입력·등록 수명은 바꾸지 않는다.

- 외형 원본은 `tools/scroll-panel/cartoon-scroll-atlas.svg`의 직접 작성한 벡터다. 제공 이미지는 참고 자료이며 배경/체커보드를 런타임 에셋으로 가져오지 않는다.
- 새 PNG 아틀라스는 왼쪽 75%가 종이 본체, 오른쪽 25%가 세로 말림이다. Graphic은 본체의 고정 UV를 드러내고, 양 끝은 동일 말림 그림을 대칭 배치한다. 닫힐 때는 겹쳐져 하나의 말린 종이 형태가 된다.
- 본체 폭 비율은 0.84로 바꿔 말림을 위한 여백을 확보한다. 기존 ContentRoot의 최종 폭을 유지하도록 기본 안쪽 x 여백은 73.76, 덱/이벤트 인스턴스는 99.36으로 함께 보정한다. y 여백·내용 배치·ContentFrame의 기준 크기는 유지한다.
- 이미 음영이 그려진 아틀라스를 사용하는 UI 셰이더로 바꾸고 기존 거친 요철 음영을 제거한다. 공용 프리팹과 Material이 새 아틀라스를 참조하며 기존 GUID/중첩 연결은 유지한다. 옛 텍스처는 삭제하지 않는다.
- SSOT: 팔레트·선·단면의 원본은 SVG, 배치 비율은 Graphic, 열림 진행은 Presentation, 내용은 기존 화면이 소유한다. SOLID/독립성: 기능 Controller·StagePopupViewLifetime·UISystem·Core·카메라는 변경하지 않는다. Material/Graphic의 공용 외형 변경이므로 두 적용 화면을 Unity 확인 대상에 함께 포함한다. FW 등록/입력 계약 변경은 없다.
- Test 코드·MSBuild·Unity 실행 없이 에셋 및 참조를 정적으로 확인한다. 이미지 미리보기는 에셋 조합 결과이며 Unity 출력 검증과 구분한다.

## 9. TraitUI 공통 레이아웃 참조 검사 수정 (2026-11-02)

사용자 Unity 실행에서 증강용 TraitUI가 공통 StageOfferLayoutView의 필수 scrollPresentation 검사로 진입을 중단했다. Phase 2의 적용 대상은 덱/이벤트이고 증강은 기존 동기 경로 유지이므로, 공유 레이아웃에서 두루마리 참조 자체는 선택 사항으로 돌린다. 구체적인 화면이 필수 여부를 결정하며 StageOfferUI는 필수, TraitGrowthUI는 기본 선택 정책을 따른다. 화면의 공개 참조 조회·구성 검사·Open 전에 필수 정책을 검사해 이벤트의 잘못된 구성을 숨기지 않는다. 레이아웃은 참조가 있을 때만 열기/초기화를 호출한다. StagePopupViewLifetime의 기존 null 연출 경로를 사용하고 새 상태·서비스·프리팹/씬 변경은 추가하지 않는다. UI 등록/입력 수명과 SSOT/SOLID/FW 계약은 그대로다.

## 10. Phase 4 — 증강 두루마리 적용 (2026-11-02 사용자 요청)

사용자가 증강에도 같은 연출을 요청했으므로 §9의 증강 적용 제외를 대체한다. 공유 레이아웃의 선택 참조 계약은 유지하고 TraitGrowthUI도 RequiresScrollPresentation을 true로 지정한다. TraitUI 원본 안에 공용 두루마리를 중첩하고, 기존 제목·카드 3장·안내를 ContentFrame 안에 둔다. 기존 객체 ID·카드 참조·UIId.TraitGrowth·Parallel Policy·등록/바인딩은 유지한다. 공통 종이 크기 1840×1020·여백 99.36×175와 고정 1920×1080 ContentFrame 맞춤을 재사용한다. 기존 큰 배경은 투명화한다.

열기는 StageOfferScreenUI→레이아웃→공용 연출, 완료 닫기는 기존 TraitGrowthPopupController→StagePopupViewLifetime→Closed→기존 Scope.Close 경로를 그대로 쓴다. 제안이 더 남으면 종이를 다시 열지 않고 내용만 갱신한다. 마지막 결과를 확인해 제안이 없어졌을 때 말림/퇴장/암전 해제 후 기존 입력 수명을 닫는다. 강제 Hide·Dispose·씬 이탈은 기존 즉시 정리다. 증강 제안/선택/결과 확인·요청 소유자·Core는 수정하지 않는다.

SSOT/SOLID/독립성: 원본 상태는 기존 Buffer/요청 소유자, 진행도는 Presentation, 필수 여부는 소유 화면, 조립은 TraitUI 프리팹에 둔다. 공통 연출에 증강 분기를 추가하지 않는다. 기존 UIBase/FW-008·009와 입력 수명을 유지하고 새 서비스·카메라·이력 경로는 만들지 않는다. StageSelect가 TraitGrowthUI→TraitUI를 참조하므로 씬과 외부 프리팹은 수정하지 않는다. Test 코드·MSBuild·Unity 실행 없이 참조와 구조를 정적으로 확인한다.
