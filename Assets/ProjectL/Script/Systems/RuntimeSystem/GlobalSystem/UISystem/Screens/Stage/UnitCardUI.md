# UnitCardUI 연결

## 구조 (2026-10-06 손패 카드화 H1)

표시와 데이터 조회를 나눴습니다. 덱 정보 카드와 전투 손패 카드가 같은 외형을 씁니다.

| 대상 | 역할 |
|---|---|
| `UnitCardView` + `Prefabs/UI/UnitCard.prefab` | 표시 전용. 아래 Inspector 참조·Visuals SO·패널 Hover/고정을 가집니다. UIBase가 아니라 여러 장을 둘 수 있습니다. 외형은 이 프리팹 하나만 고칩니다 |
| `UnitCardUI` + `Prefabs/UI/UnitCardUI.prefab`(UnitCard.prefab의 Variant) | 덱 정보 카드. Buffer·DB·보유 카드를 조회해 같은 오브젝트의 `UnitCardView`에 넘깁니다(`RequireComponent`). 공개 API는 이전과 같습니다 |
| 전투 손패(`HandPresenter`) | `UnitCard.prefab`을 여러 장 빌려 값만 넘깁니다. 패널 버튼은 숨깁니다 |

`DeckButtonUI`의 **Unit Card** 필드에는 `UnitCardUI.prefab`(Variant)을 연결합니다. 설계: `docs/superpowers/specs/2026-10-06-hand-card-design.md`.

레이아웃은 에디터에서 구성합니다. 카드 루트 아래에 Synergy/Function 패널을 서로 같은 부모를 가진 별도 자식 오브젝트로 배치하세요.

## Inspector (`UnitCardView`)

- Level Text: Level을 표시할 TMP_Text.
- Portrait: 캐릭터 Image.
- Name Text: 이름 TMP_Text.
- Details Text: HP / DEF / ATK / ATK COST / MAX COST / PRIORITY를 표시할 TMP_Text.
- Synergy Button / Function Button: 우측 Index Button 두 개.
- Synergy Panel / Function Panel: 각각의 자식 패널 GameObject.
- Synergy Icon / Description: 시너지 패널의 Image와 TMP_Text.
- Function Icon / Name / Description: 특성 패널의 Image와 이름·설명 TMP_Text.
- Visuals: `Create > ProjectL > UI > Unit Card Visuals`에서 만든 SO.

Visuals SO의 Characters에 캐릭터 `EntityKey`, 이름, Synergy 아이콘과 설명을 등록합니다. Buffer의 CharacterKey로 해당 항목을 찾습니다. 초상화는 Unit Visual Data Table에 등록된 SO의 `InfoIcon`을 사용하며, Visuals SO의 Portrait는 사용하지 않습니다. 이름이 비어 있으면 UnitVisual DB의 DisplayName을 사용합니다. InfoIcon이 없으면 Portrait Image를 숨깁니다. TimelineUI는 같은 테이블의 `TimelineIcon`을 사용합니다. DB 쿼리는 기존 UISystem의 GetUIData 경로로 전달됩니다.

Index Button의 UIInputBinding은 Awake에서 자동 연결되며, Hover·Click·Submit을 대상 인터페이스로 직접 전달합니다. Inspector의 OnClick에 동일 메서드를 중복 등록하지 마세요. Canvas의 GraphicRaycaster와 EventSystem이 필요합니다.

Function 패널은 CharacterBuffer의 `CharacterData.Stats.TraitStatuses`에 있는 Key로 Trait Visual Data Table을 조회해 DisplayName, Icon, Description을 표시합니다. InGame Unit DB는 조회하지 않습니다. 특성이 없거나 Buffer 조회값·표시 데이터가 없으면 내용을 비우고 아이콘을 숨깁니다. 여러 특성은 Buffer 순서대로 이름·설명을 줄바꿈해 표시하고 첫 번째 표시 특성의 아이콘을 사용합니다. 설명에 입력한 리터럴 `\n`도 줄바꿈으로 표시합니다. 기존 Visuals SO의 Function 필드는 사용하지 않습니다.

## 데이터 지정

```csharp
unitCard.SetCharacter(character.EntityId, level: 3);
unitCard.Open();
// Level만 바뀌는 경우
unitCard.SetLevel(4);
// 선택 해제
unitCard.SetCharacter(EntityId.Empty);
```

기존 UISystem이 UIBase를 수집하여 Buffer 서비스와 DB 쿼리를 바인딩합니다. ID는 `UIId.UnitCard`입니다. 현재 UISystem은 ID별 단일 UI를 등록하므로 이 컴포넌트는 씬에 하나 배치합니다.

조회 ID는 전투 개체 `EntityId`이며, 보유 카드 `CharacterInstanceId`와 다릅니다. 전투 종료 등으로 Buffer에서 해당 개체가 사라지면 표시 내용과 패널을 비웁니다. 활성 상태에서는 Buffer 조회값이 교체될 때 갱신하고, Open 시에도 다시 조회합니다.

덱에서는 `SetOwnedCharacter(owned.Id)`를 사용합니다. 보유 ID와 연결된 전투 개체가 있으면 메인·서브 모두 최신 Buffer의 Health/Stats를 표시합니다. 연결된 전투 개체가 없으면 능력치와 Function 내용을 비웁니다. 보유 목록은 ID·캐릭터 키·역할만 제공하므로 InGame Unit DB 조회나 영혼 보너스 공식으로 보충하지 않습니다. 같은 캐릭터 종류의 카드도 보유 ID별로 조회하며, 이름·초상화·시너지 등의 외형은 기존 View 데이터 경로를 사용합니다.

MainCharactorUI도 능력치·Category는 Buffer 조회값만 사용합니다. 전투 밖에서는 지속 상태의 현재 HP만 표시하고 최대 HP는 `-`, 체력바는 빈 상태로 둡니다. Category 조회값이 없으면 해당 초상화·시너지 이미지는 숨깁니다.

## DeckButtonUI 연결

`DeckButtonUI`의 **Unit Card** 필드에 공용 `UnitCardUI`를 연결합니다. 각 덱 버튼에 카드 컴포넌트를 붙일 필요가 없으며, 버튼 생성 시 Hover/Click을 자동 연결합니다.

카드 루트는 ScrollRect Content 바깥의 별도 오브젝트로 배치합니다. 예를 들어 DeckButtonUI 루트 아래에서 덱 Panel과 UnitCard를 형제 오브젝트로 두면 됩니다. 카드가 덱 항목의 포인터 영역을 덮지 않도록 레이아웃을 구성하세요.

- 덱 항목 Hover: 해당 카드 표시. 버튼을 벗어나면 닫기.
- 덱 항목 Click: 카드 고정. 같은 항목을 다시 Click하면 닫기.
- 다른 덱 항목 Hover/Click: 그 항목의 카드로 전환.
- 고정 상태에서는 커서를 카드로 옮겨 Synergy/Function 버튼을 조작할 수 있습니다.
- 덱 닫기·비활성화·표시 대상이 보유 목록에서 제거됨: 카드 닫기.
- 선택적으로 카드의 닫기 Button에 `DeckButtonUI.CloseUnitCard`를 연결할 수 있습니다.

덱은 Level을 전달하지 않으므로 Level은 `-`입니다. Level 데이터가 추가되면 덱의 ShowCharacter에서 함께 전달하면 됩니다.

HP는 현재 / 최대 HP, ATK는 보너스를 포함한 EffectiveAttackPower를 표시합니다. 나머지 능력치는 CharacterData.Stats에서 읽습니다. 현재 Buffer에는 Level 필드가 없으므로 호출 측에서 전달하며, 전달하지 않으면 `-`로 표시합니다.

## 패널 동작

- Hover 진입: 해당 패널 열기, 다른 패널 닫기.
- Hover 이탈: 고정되지 않은 해당 패널 즉시 닫기.
- Click: 해당 패널 고정. 같은 버튼을 다시 Click하면 닫기.
- 다른 Index Hover/Click: 기존 패널의 고정을 해제하고 해당 패널로 전환.
- Hide, 비활성화, 선택 대상 변경: 두 패널 닫기.

코드 컴파일을 확인했습니다. 실제 포인터 입력과 레이아웃은 Unity에서 Inspector 연결 후 확인해야 합니다.
