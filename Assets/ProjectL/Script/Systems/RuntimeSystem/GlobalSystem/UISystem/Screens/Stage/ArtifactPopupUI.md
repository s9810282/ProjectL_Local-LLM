# ArtifactPopupUI 연결

> **2026-10-06 최신 변경:** 사용자 요청으로 Artifact/Close와 아티팩트 ESC 닫기 연결을 제거했습니다. 팝업은 화면의 뒤로가기 버튼으로 닫습니다. Common/OpenSettings의 ESC 바인딩은 유지합니다. 아래 ESC 구현·검증 기록은 변경 전 이력입니다. 이번 변경 후 실제 Play 재검증은 별도입니다.

StageSelect 화면의 아티팩트 팝업 UI입니다. 열기·닫기 버튼, 하단 보유 목록, 이름·효과·상태 문구를 맡습니다. 3D 보드·모델·드래그·입력 Layer·Core 요청은 `ArtifactViewSystem`(View)이 맡습니다. `UIId.Artifact`와 일반 `UIBase`를 사용합니다.

## Hierarchy

```text
ArtifactPopupCanvas (Canvas, CanvasScaler, GraphicRaycaster, ArtifactSceneRoot, ArtifactPopupView, ArtifactPopupUI)
├─ ArtifactOpenButton (Button)            열기 버튼. Panel 밖에 둔다
│  └─ Label (TMP_Text)
└─ Panel (기본 비활성)                     열고 닫는 별도 자식
   ├─ InventoryBackdrop (Image)
   ├─ PoolArea (RectTransform, GridLayoutGroup)   하단 보유 목록·반환 영역
   ├─ StatusText (TMP_Text)
   ├─ CloseButton (Button)
   │  └─ Label (TMP_Text)
   ├─ Title (TMP_Text)
   ├─ ArtifactName (TMP_Text)
   └─ EffectDescription (TMP_Text)
```

| Inspector 필드 | 연결 대상 |
| --- | --- |
| Open Button | `ArtifactOpenButton`의 Button. Panel 안에 두면 `Validate`에서 예외입니다. |
| Panel | 별도 자식 `Panel`. ArtifactPopupUI 오브젝트 자체를 연결하면 안 됩니다. |
| Close Button | Panel 안의 `CloseButton` Button. |
| Pool Area | `PoolArea` RectTransform. 슬롯이 여기에 생성되고, 배치물을 여기에 놓으면 반환합니다. |
| Slot Prefab | `Prefabs/UI/Artifact/ArtifactItem.prefab`의 Button. Transition·Navigation은 None입니다. |
| Status Text / Artifact Name / Effect Description | 상태·결과 문구, 이름, 효과 설명 TMP_Text. |

## 열기·닫기

- 루트(`ArtifactPopupCanvas`)는 항상 켜 둡니다. `Open`/`Hide`는 Panel만 켜고 끕니다. UISystem은 씬 바인딩·초기화 때 닫힌 UI의 `Hide`를 부르므로, 루트를 끄면 열기 버튼까지 사라집니다.
- 열기 버튼 → `IUISystemService.OpenUI(UIId.Artifact)` → `Open`. View가 연결돼 있고 오류 상태가 아닐 때만 엽니다.
- 닫기 버튼 → `CloseUI(UIId.Artifact)` → `Hide`. 뒤로가기(`BackCloseUI`)·`HideAllUI`도 같은 `Hide`를 탑니다.
- ESC는 InputSystem `Artifact` 맵의 `Close` 액션입니다. 팝업이 열려 있을 때만 받고, View가 `BackCloseUI`를 부릅니다.
- `Open`/`Hide`는 연결된 `ArtifactViewSystem`에 열림·숨김을 알립니다. View는 이때 3D 보드와 입력 Layer(`Artifact` ↔ `StageSelection`)를 바꿉니다. 이벤트 구독이 아니라 씬 연결 때 한 번 `Connect`하는 방식입니다.

## 입력

- 열기·닫기·목록 슬롯은 `UIInputBinding`이 직접 전달합니다. Inspector의 Button On Click에는 아무것도 등록하지 않습니다.
- 목록 슬롯의 Hover·Click은 이름·효과 설명을 표시합니다. 문구는 View DB `ArtifactViewData`의 `DisplayName`·`Description`입니다.
- 드래그 시작·놓기·Q/E 회전·우클릭 회수는 UI 입력이 아니라 InputSystem `Artifact` Layer로 View가 받습니다.

## 데이터

- 보유 목록은 `BindUIService`에서 받은 `ArtifactBuffer`(`ArtifactReadSink`)의 현재 스냅샷으로 그립니다. 열려 있을 때만 스냅샷 참조가 바뀌었는지 보고 다시 그립니다.
- 슬롯 Button은 재사용합니다. 목록이 줄면 남는 슬롯을 숨깁니다. 슬롯 안의 모양은 Buffer의 로컬 셀로 그립니다(`ArtifactPoolItemView`).
- 상태·결과 문구의 내용은 View가 정하고 UI는 표시만 합니다.
