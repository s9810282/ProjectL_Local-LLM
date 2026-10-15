# DeckButtonUI 연결

`Deck Button`은 Global UI에서 덱 패널을 여는 바깥 버튼입니다. 기존 Inspector의 `Open Button`과 같은 필드이며 표시명만 변경했습니다. `Character Button Prefab`은 패널 목록에 복제할 캐릭터 버튼으로, 기존 `Deck Button Prefab` 필드입니다. 두 필드에는 서로 다른 Button을 연결합니다.

```text
GlobalUICanvas
└─ Deck (Button, Image, DeckButtonUI)
   ├─ DeckPannel
   │  └─ Scroll View (ScrollRect)
   │     └─ Viewport (RectMask2D)
   │        └─ Content
   │           └─ CharacterButtonTemplate (Button, Image, 비활성)
   └─ UnitCard (UnitCardUI, 선택 사항)
```

| Inspector 필드 | 연결 대상 |
| --- | --- |
| Deck Button | 바깥 `Deck` 오브젝트의 Button. DeckButtonUI와 같은 오브젝트여도 됩니다. |
| Panel | Deck 아래의 별도 자식 `DeckPannel` GameObject. Deck 루트 자체를 연결하면 안 됩니다. |
| Scroll View | 패널 내부 ScrollRect. ScrollRect의 Viewport와 Content도 연결합니다. |
| Character Button Prefab | 별도 캐릭터 Button 프리팹 또는 Content 아래 비활성 템플릿. DeckButtonUI 컴포넌트를 붙이지 않습니다. |
| Unit Card | 선택적 공용 UnitCardUI. Content 바깥에 배치합니다. |
| Spacing | 목록 버튼 간격. 버튼 크기는 Character Button Prefab에 제작된 크기를 사용합니다. |

코드가 Content에 GridLayoutGroup과 ContentSizeFitter를 구성하여 가로 5개씩 배치하고 세로 스크롤합니다. 전투 밖에서도 유지되는 CharacterBuffer.Owned의 Main을 맨 앞에 표시하고, 이어서 Sub를 기존 보유 순서대로 표시합니다. 같은 종류의 캐릭터도 보유 개체마다 버튼 하나를 생성합니다.

목록용 Unit 프리팹의 직접 자식 `Charactor`에는 Image, `Name`에는 TMP_Text를 둡니다. UnitCard와 같은 UnitVisualTable에서 캐릭터 EntityKey로 조회한 InfoIcon과 DisplayName을 각각 표시합니다. 데이터나 이름이 없으면 EntityKey를 이름으로 표시하고, InfoIcon이 없으면 Charactor 이미지를 숨깁니다. 버튼 배경 Image는 프리팹 그대로 유지합니다. 별도 이미지 SO나 Inspector 연결은 필요하지 않습니다.

입력은 Awake와 목록 생성 시 UIInputBinding이 자동 연결합니다. Inspector의 Button On Click에는 OpenPanel, ClosePanel 등의 동작을 등록하지 않습니다. Canvas에 GraphicRaycaster, 씬에 EventSystem이 필요하며 왼쪽 클릭과 Submit을 지원합니다.

Close Button 필드는 없습니다. Deck Button을 다시 누르면 DeckPannel을 비활성화합니다. 패널이 열리면 부모 Canvas 아래에 투명한 전체 화면 Button인 DeckOutsideClickArea를 자동 생성해 덱 바로 뒤에 배치합니다. 패널·덱 버튼 바깥을 왼쪽 클릭하면 닫힙니다. 닫히거나 DeckButtonUI가 비활성화되면 바깥 클릭 영역도 비활성화합니다. 패널의 빈 영역 클릭은 부모 Deck Button으로 전파되지 않습니다. 별도 Inspector 연결은 필요하지 않습니다.

바깥 Deck Button에는 Hover 기능이 없습니다. 현재 코드에서 Unit Card를 연결하면 목록의 캐릭터 버튼은 Hover로 카드 미리보기, Click으로 카드 고정을 지원합니다. Unit Card를 비워두면 카드 표시 기능은 동작하지 않습니다.

Click으로 고정한 유닛은 다른 유닛 Button에 Hover해도 전환하지 않습니다. 다른 유닛을 Click하면 해당 유닛으로 고정을 전환하고, 고정한 유닛을 다시 Click하면 닫습니다. 고정이 해제된 뒤에는 Hover 미리보기가 다시 동작합니다. 앞으로 만들 Hand의 유닛 목록에도 같은 규칙을 적용합니다.

Character Button Prefab에는 바깥 Deck Button과 별도의 목록용 프리팹을 연결합니다. Unit Card를 사용할 때는 화면에 배치한 UnitCardUI 인스턴스를 연결합니다.
