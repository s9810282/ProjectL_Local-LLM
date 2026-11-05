# UIBase 화면·그룹·위젯 수명

## 등록 단위와 정책

- 기존 DeckButtonUI가 UIId.Deck의 Parallel 공통 부모를 맡는다. 기존 Global 유지 수명과 버튼 연결을 보존한다.
- DeckCardList는 Exclusive UIGroup이며 ParentID = Deck이다. 네 UnitCardUI 멤버를 함께 표시한다.
- CombatUnitInfo는 Exclusive이며 ParentID = Deck이다. 카드 목록과 전투 정보창의 전환은 UISystem에서 처리한다.
- Exclusive의 범위는 기존처럼 전체 이력이다. 대상의 조상과 기존 Parallel을 보존하며, 같은 부모에만 한정하지 않는다.
- CombatHud와 CombatActionPanel은 씬에 등록되는 Parallel 화면이다.
- UnitHUDLayer는 공용 HUD Canvas의 Parallel UIGroup이다. 개별 UnitHUD는 그룹 멤버로 관리한다.

새 화면 ID는 CombatHud=35, CombatActionPanel=36, CombatUnitInfo=37, UnitHUDLayer=38이다. 기존 ID 값은 변경하지 않는다.

## 반복 표시 부품

UnitHUD, UnitCardView, PackItemSlotView, CombatRewardSlot, StageOfferCardView, ArtifactPoolItemView는
UIWidgetBase → UIBase를 상속하는 Parallel 내부 위젯이다.

위젯은 독립 이력을 만들지 않고 UIId.None을 사용한다. UISystem은 씬 수집에서 위젯의 소유 화면을
검증하고 별도 등록하지 않는다. 같은 객체의 UnitCardUI 또는 CombatUnitInfo가 카드 위젯을 소유할 수 있다.
동적 제안 카드·아티팩트 슬롯은 생성하는 상위 UI가 BindOwner로 연결한다.
위젯을 독립 등록하려는 호출과 소유 화면 누락·모호성은 예외다.

위젯의 개별 데이터 표시·숨김은 소유 화면이 Open/Hide로 제어한다.
화면 내부 아이콘·선택 표시·마스크·문구 등의 세부 활성 상태까지 독립 UI로 만들지는 않는다.
Tutorial, Expansion, UnitDamageText 등 텍스트 연출은 이번 전환에서 제외했다.

## 생성·등록·소비·해제

- UISystem 생성·등록: GlobalSystemFactory의 Global 내부 Builder.
- 보관·상위 노출: GlobalSystemGroup.UIServiceCommand. 상위에는 기존 Global Context만 노출한다.
- 소비: CombatDisplayServices.From이 Global Context에서 UI 서비스를 받아 Factory로 전달한다.
- 정적 화면: UISystem.BindScene에서 생성된 씬 인스턴스를 수집·등록·바인딩한다.
- 동적 HUD 그룹: CombatSceneDisplayFactory → CombatUnitHUDFactory.Create → UnitHUDLayer.AttachUISystem.
  IUIRuntimeRegistry.RegisterRuntimeUI가 등록·계층 검증·서비스 바인딩·최초 Hide를 완료한 뒤 고정 표시로 연다.
- 개별 HUD: FieldUnitPresenter가 생성하고 UnitHUDLayer.Register가 그룹 멤버·투영 추적을 함께 연결한다.
- 해제: CombatSceneDisplay → UnitHUDLayer.Release → 동적 등록 해제·멤버 제거·투영 콜백 해제 → Canvas 파괴.
  일반 Hide는 멤버 소유권을 유지한다. Release 이후 재사용은 거절한다.

UISystem.Shutdown은 남아 있는 동적 화면을 먼저 Hide하여 입력·렌더 콜백을 끊는다.
씬 재바인딩에서도 살아 있는 동적 UI의 등록 표식을 보존하여 이후 정상 해제할 수 있다.

UIGroup.RemoveInstance는 유닛의 임의 순서 해제를 지원하며 나머지 Stack 순서를 보존한다.
그룹을 다시 열어도 UnitHUD.SetOwnerVisible(false)인 풀/비활성 유닛의 HUD는 열지 않는다.
화면 밖 투영 숨김은 CanvasGroup으로 처리하여 피해 숫자·HP 연출과 고정 상태를 보존한다.

## 개폐와 콜백

등록 화면의 외부 개폐는 IUISystemService.OpenUI/OpenChildUI/CloseUI를 사용한다.
실제 활성 상태 변경과 정리는 UIBase.Open/Hide 및 OnUIOpened/OnUIHidden에서 수행한다.
UIBase는 부모 비활성화·씬 해제의 Unity 콜백도 같은 훅에 연결하고 중복 전환을 막는다.

UI 서비스 없이 생성하는 기존 격리 전투 표시 시험은 명시적으로 직접 Open/Hide를 허용한다.
운영 CombatDisplayServices.From은 Global의 UI 서비스를 항상 전달한다.
프레임별 값 갱신만으로 사용자가 연 카드 목록을 CombatUnitInfo가 다시 닫지 않도록 한다.
