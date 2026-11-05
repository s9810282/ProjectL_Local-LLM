# 회복 팝업 배경 안내

- 작성자: 호진(이호진)

회복 팝업의 배경 색이나 그림을 바꾸는 법이다. 배경은 프리팹의 Image에서 직접 정한다(2026-10-15 사용자 지정). View DB 배경 설정이나 별도 표시 코드는 없다. 회복 화면의 구조와 계약은 [비전투 이벤트·회복 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md) §9가 원본이다.

## 1 고칠 프리팹

운영 화면은 [`RecoveryUI.prefab`](../../../Assets/ProjectL/Prefabs/UI/Recovery/RecoveryUI.prefab)이다. 같은 폴더의 `RecoveryPanel.prefab`은 옛 표시 원본이라 StageSelect에 연결되어 있지 않다. 고쳐도 화면에 나오지 않는다.

```text
RecoveryUI
└─ Panel                ← 닫힐 때 함께 숨는다
   ├─ PointerBlocker    ← 전체 화면 배경 + 클릭 차단 Image
   ├─ Content           ← 중앙 패널 배경 Image
   └─ Templates         ← 카드 템플릿(비활성)
```

`RecoveryUI.prefab`에는 자기 Canvas가 없다. StageSelect 공통 Canvas의 `StageScreenRootUI` 아래 자식으로 놓인다(2026-10-26 Canvas 통합). 정렬은 그 공통 Canvas가 정한다.

## 2 지금 값

| 대상 | Source Image | Color |
|---|---|---|
| `Panel/PointerBlocker` | 없음 | `R 0.08 / G 0.09 / B 0.12 / A 1`(상점 패널과 같은 불투명 단색, 뒤 맵을 가림) |
| `Panel/Content` | 없음 | `R 0.08 / G 0.09 / B 0.12 / A 0.96` |

바뀐 것: `PointerBlocker` 색은 처음에 투명이었고, 2026-10-15 사용자 요청으로 위 단색이 됐다.

## 3 단색 바꾸기

1. `Panel/PointerBlocker`(전체 화면) 또는 `Panel/Content`(중앙 패널)의 **Image → Color**를 바꾼다.
2. 프리팹을 저장한다.

## 4 배경 그림 넣기

1. 배경 리소스를 UI용 Sprite로 준비한다.
2. 전체 화면이면 `Panel/PointerBlocker`, 중앙 패널만이면 `Panel/Content`의 Image → **Source Image**에 Sprite를 넣는다.
3. **Color를 흰색·Alpha 1로 바꾼다.** Image의 Color는 Sprite 색에 곱해지므로 어두운 단색을 그대로 두면 그림도 어두워진다.
4. 프리팹을 저장하고 Play에서 회복 팝업을 열어 비율과 문구 가독성을 본다.

그림을 빼고 단색으로 돌아가려면 Source Image를 비우고 원하는 Color를 다시 넣는다. 그림 유무에 따라 색을 자동으로 바꾸는 로직은 없다.

## 5 지킬 것

- 배경은 `Panel` 아래에 둔다. 그래야 팝업이 닫힐 때 같이 숨는다.
- `PointerBlocker`의 Raycast Target과 전체 화면 stretch를 그대로 두고, `Panel`의 첫 자식 자리도 그대로 둔다. 첫 자식이라 가장 뒤에 그려져 카드·문구 뒤에서 맵을 가리고 클릭을 막는다. `Content`의 Raycast 설정도 그대로 둔다.
- Collider나 새 입력 핸들러를 붙이지 않는다.
- [`RecoveryUI`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/RecoveryUI.cs)의 Render는 카드와 문구만 갱신하고 배경 Image의 색·Sprite를 덮어쓰지 않는다.
- 카드 모양은 상점·회복·강화가 함께 쓰는 공통 설명 카드 `StageEventOfferDescriptionCard.prefab`이 정한다. 회복 배경을 바꾸려고 카드 프리팹을 고치지 않는다.
- 전면 배경은 뒤 맵 클릭을 막지만, 글로벌 HUD(덱·설정)의 키보드 Submit·초점까지 막지는 않는다. 그 차단은 후속 U0다.
