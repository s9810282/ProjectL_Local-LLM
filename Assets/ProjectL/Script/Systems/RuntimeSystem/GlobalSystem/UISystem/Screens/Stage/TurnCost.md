> R4 current status (2026-10-29): The registered TurnCost UI and its prefab remain dormant for separately coordinated content cleanup. Normal combat no longer opens or renders AP, or submits cost previews. CombatSceneDisplayFactory closes UIId.TurnCost through the existing UI service; CombatSceneRoot no longer requires a TurnCost reference. The cost-only presenter and feedback interface have been removed. Current action availability, aim/range facts and input/pending/playback gates are unchanged. PlayerBudget, quoted/spent prices and the presenter/preview wiring described below are historical contracts. Source migration is delivered; native and manual/PlayMode acceptance must be reported separately.

---

The original implementation notes below are historical where they conflict with the R4 status above.

# TurnCost UI·연출

2026-10-25: 기존 UI·연출을 Combat의 플레이어 공유 턴 AP에 연결했다. Core의 AP 규칙·최대치·비용 계산은 변경하지 않는다.

## Combat 연결

- `CombatSceneRoot.turnCost`가 씬의 필수 참조다. 기존 씬의 Cost1~6 배치와 외형을 보존하고 같은 70 간격으로 Cost7~10을 왼쪽에 추가했다.
- `initialInstances`는 우측 Cost1_Back부터 좌측 Cost10_Back까지의 순서다. 각 슬롯의 `costImage`는 `_Back`, `fillCost`는 같은 번호의 자식 Cost Image다. 최대 AP만큼 낮은 번호부터 배경을 켜고, 잔여 AP만큼 Fill을 채운다.
- 확정 AP가 3→2면 Cost3, 2→1이면 Cost2를 소모한다. 여러 AP를 쓰면 감소한 높은 번호 Fill들에 기존 축소·페이드 연출을 적용하며, 최대치 이내의 빈 `_Back`은 유지한다.
- `CombatSceneDisplayFactory`가 `CombatTurnCostPresenter`를 조립하고 Display에 전달한다. Display는 기존 최종 사실 갱신 시 `CombatStateData.CommandState.PlayerBudget.Current/Maximum`과 Turn을 넘긴다. 개별 유닛 AP나 Stats에서 공유 잔액을 계산하지 않는다.
- 같은 제어자 턴의 감소에는 `SetCost`를 사용하고, 첫 표시·새 제어자 턴·준비 단계 변경에는 `ResetCost`를 사용한다. 새 턴은 잔액 소모처럼 연출하지 않는다. 배치·전투 종료에는 HUD를 닫고, 전투 중 적 턴에도 플레이어 공유 잔액만 표시한다.
- 이동은 유효한 `MovePreview.QuotedActionCost`, 공격은 유효한 `AttackPreview.AttackCost`(Core 정산의 Spent)를 `SetPreview`에 전달한다. 공격 견적 전달은 좁은 `ICombatActionCostFeedback` 계약을 사용하며 궤적 표시 여부와 독립된다. 취소·범위 밖·늦은 응답·입력 불가·씬 해제 때 예상을 지운다.
- 외부 개폐는 Factory가 Global Context에서 받은 기존 `IUISystemService`의 `OpenUI(UIId.TurnCost, true)`/`CloseUI`를 사용한다. UI 없는 격리 표시 시험만 명시적으로 직접 Open/Hide한다. 그룹·슬롯의 활성 변경은 각 UI 구현이 담당한다.
- 씬 수집 때 UISystem이 기존 TurnCost 그룹을 등록하고 자식 슬롯을 소유 멤버로 바인딩한다. AP Presenter는 화면이나 서비스를 중복 생성·등록하지 않는다. 해제는 Display의 Clear/Release 정리 목록에 포함된다.

## 구성과 등록

- `TurnCost : UIGroup`, `UIId.TurnCost = 39`, `ParentID.None`, `UIOpenPolicy.Parallel`.
- 등록 단위는 TurnCost 하나다. `TurnCostSlot : UIWidgetBase`와 Fill Image는 별도로 등록하지 않는다.
- `ProjectL/Prefabs/UI/CombatOnly/TurnCost.prefab`은 기존 Empty/Fill Cost 스프라이트를 연결한 5칸짜리 시작 프리팹이다. 운영 Combat 씬은 기존 씬 외형의 10칸을 직접 연결했다. 슬롯 수는 규칙 값이 아니며 배치할 UI의 최대치에 맞춰 편집한다.
- UIGroup의 `initialInstances`가 멤버 구성 원본이다. 배열 앞부터 채우고 뒤부터 소모한다. 화면 배치도 같은 순서로 맞춘다.
- 슬롯 배경은 슬롯 객체의 Image, Fill은 그 하위의 별도 Image다. 둘 다 입력을 가리지 않는다.
- 씬 배치 시 UISystem.BindScene이 그룹 멤버 소유권을 확정하고 그룹만 등록·바인딩한다. 런타임 생성이라면 기존 IUIRuntimeRegistry를 사용하여 등록·해제한다.
- 외부 개폐는 Global Context의 `UIServiceCommand`를 사용한다. 고정 HUD는 `OpenUI(UIId.TurnCost, true)`, 종료는 `CloseUI(UIId.TurnCost)`다. 실제 활성 상태는 그룹/위젯의 Open/Hide가 바꾼다.

기존 서비스 경로는 GlobalSystemFactory가 UISystem을 생성 → UISystem.ExportServices가 Global 내부 Builder에 IUISystemService 등록 → GlobalSystemGroup이 UIServiceCommand로 보관 → RunTimeSystemFactory의 루트 Builder에는 Global Group만 Export → 소비자가 SystemGroupsService.Global을 조회하는 경로다. 이번 변경은 서비스 등록·Export·조회 경로를 추가하지 않는다. FW-001 중복 등록과 하위 서비스를 루트에 재노출하는 구조를 만들지 않는다.

## 호출 계약

```csharp
// 생성/새 턴/대상 교체: 연출 없이 정확한 표시로 초기화한다.
turnCost.ResetCost(current: 5, maximum: 5);
ui.OpenUI(UIId.TurnCost, true);

// 확정 전: 현재 수량 중 뒤쪽 2칸의 Fill을 깜빡인다.
turnCost.SetPreview(2);
turnCost.ClearPreview(); // 취소

// 사용 확정: 감소한 Fill만 축소하면서 사라진다.
turnCost.SetCost(current: 3, maximum: 5);
```

- 첫 SetCost는 즉시 반영한다. 최대치 변경도 슬롯 재구성이므로 즉시 반영한다. 같은 최대치에서 확정 수량이 감소하면 소모 연출, 증가하면 즉시 복원한다.
- 같은 SetCost/SetPreview를 반복해도 진행 중인 연출과 예상 주기를 재시작하지 않는다. 연속 소모는 이미 진행 중인 다른 슬롯의 연출을 보존한다.
- 그룹 한 곳의 LateUpdate가 unscaledDeltaTime을 전달한다. 슬롯마다 Update·코루틴·입력 구독은 없다.
- 숨김·부모 비활성화는 예상을 지우고 소모 연출을 확정 상태로 완료한다. Open은 수량을 충전하지 않는다. 숨긴 상태에서 갱신한 수량은 즉시 맞추며 늦게 온 예상은 보존하지 않는다.
- 원래 Fill 색과 스케일은 첫 수량 바인딩 때 보관한다. 재사용은 ResetCost, 씬 전환·해제는 기존 UISystem과 생성자의 수명에 따른다. 별도 지연 작업이나 이벤트 해제는 없다.
- maximum은 준비한 슬롯 수 이하여야 한다. 참조·타입·슬롯 구성 오류는 대상과 UIId를 담은 InvalidOperationException, 수량 인자 오류는 ArgumentOutOfRangeException이다. 슬롯 구성 변경은 재바인딩 전 완료하고 ResetCost를 호출한다.

## Expansion 전환

CombatLogExpansion과 PackExpansion은 UIWidgetBase로 전환한다. GUID·직렬화 필드·기존 연출과 호출 경로를 보존한다. 같은 객체의 CombatLogUI/PackUI가 소유 화면이며 독립 UIId를 만들지 않는다. 펼침·접힘은 SetExpanded로 처리하고, OnDisable의 정리는 OnUIHidden으로 옮긴다. 기본 Open/Hide는 같은 GameObject 전체를 제어하므로 펼침 상태 전환에 사용하지 않는다.

## 검증

TurnCostTests: 단일 등록과 고정 표시, 슬롯 순서·수량, 반복 예상과 취소, 연속 소모, 숨김·재표시·재바인딩, 숨김 중 갱신, 잘못된 참조·인자, 프리팹 연결을 검증한다.
ExpansionWidgetTests와 기존 CombatLogUITests: 소유 화면 연결, 독립 등록 금지, 펼침 반전·레이아웃·숨김 초기화를 검증한다.

2026-10-25 검증: 프로젝트의 실제 Unity 참조와 정의를 사용하여 ProjectL.Systems 및 ProjectL.Tests.Framework 전체 소스를 별도 산출물로 컴파일했다(오류 0). EditMode 배치 실행은 기존 Unity 에디터가 같은 프로젝트를 열고 있어 시작하지 못했다. 따라서 테스트 코드의 컴파일은 확인했으나 테스트 실행과 프리팹의 Unity 시각 검증은 대기다.

공유 AP 원본은 기존 CombatStateBuffer이고, UI의 수량·연출 상태는 그룹, 슬롯 구성은 UIGroup이 소유한다. 비용 계산·입력·조립과 표현을 분리한다. 추가 회귀 검사는 3→2→1 소모 대상, 반복 갱신의 예상 위상, 새 턴·적 턴·배치·종료·해제, 최대치별 배경과 Combat 씬의 실제 10개 참조·우측→좌측 배치를 포함한다.
