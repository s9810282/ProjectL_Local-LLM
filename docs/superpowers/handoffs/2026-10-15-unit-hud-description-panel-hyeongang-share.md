# 유닛 HUD 상태 설명 칸 연결 부탁 (용근 → 현강님)

- 작성일: 2026-10-15. 작성: 용근(View). 브랜치 `feature/Yonggeun/Stage`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련: [스테이지 1 보스 View 활성화 설계](../specs/2026-10-15-stage1-boss-view-activation-design.md) D3·D4, [활성화 계획](../plans/2026-10-15-stage1-boss-view-activation-plan.md) §6.5

## 1. 요약

- 보스 전투 작업에서 유닛 HUD 상태 칸에 **설명 문자열을 처음으로 채웠습니다.** 보스의 포효 카운터 "포효 3/5"와 목표 표시 "격파 목표"입니다.
- 그런데 `Prefabs/UI/CombatOnly/HUD/HUD.prefab`의 `UnitHUD`에서 **`descriptionPanel`·`descriptionText`가 비어 있어**(`{fileID: 0}`) 설명이 화면에 나오지 않습니다. `UnitHUD.CanDescribe`가 두 참조를 요구하기 때문에 지금은 아이콘도 마우스에 반응하지 않습니다.
- **부탁:** HUD 프리팹에 설명 패널을 만들어 두 칸에 연결해 주세요(§3). 코드 변경은 필요 없습니다.
- **`UnitHUD.cs`·`HUD.prefab`은 고치지 않았습니다.** 저희가 바꾼 것은 HUD에 넘기는 값(`UnitHUDStatus`의 설명)뿐입니다(§2).

## 2. 저희가 바꾼 것 (HUD에 넘기는 값)

`FieldUnitPresenter.RenderUnitHud`가 `UnitHUD.SetStatuses`에 넘기는 목록입니다. `UnitHUDStatus` 형식과 호출 방식은 그대로입니다.

| 항목 | 전 | 후 |
|---|---|---|
| 특성 상태 칸 설명 | 모두 빈 문자열 | **포효 특성만** `"{TraitVisual 표시 이름} {HitCount}/{HitThreshold}"` (예: "포효 3/5"). 다른 특성은 그대로 빈 문자열 |
| 전투 목표 대상 | 없음 | 지정 보스 목표 전투에서 그 보스의 **첫 칸**에 키 `BattleObjective`, 아이콘 null, 설명 "격파 목표" |

- 숫자는 Core 스냅샷(`RoarTraitStatusSnapshot`) 값을 그대로 씁니다. View가 세지 않습니다.
- 아이콘 아트가 아직 없어 사령술·포효·넉백·격파 목표 모두 `statusIconPrefab`의 기본 스프라이트로 보입니다. 보스 HUD 아이콘 순서는 [격파 목표][사령술][포효][넉백]이고, 설명이 있는 1번째·3번째만 반응합니다.
- 일반 전투의 유닛은 설명이 계속 비어 있어 지금과 같습니다.

## 3. 부탁: 설명 패널 연결

1. `HUD.prefab` 안에 설명 패널 오브젝트(배경 `Image` + 자식 `TMP_Text`)를 하나 만들어 주세요. 위치·크기·글꼴은 현강님이 정해 주시면 됩니다.
2. `UnitHUD`의 `descriptionPanel`에 패널 오브젝트를, `descriptionText`에 그 텍스트를 연결해 주세요.
3. 패널의 활성 여부는 `UnitHUD.RefreshDescription`이 정하므로 프리팹에서 꺼 두셔도 됩니다.
4. **패널 배경과 텍스트의 Raycast Target은 꺼 주세요.**
   - 켜져 있으면 패널이 아이콘을 덮을 때 HoverExit → 패널 숨김 → 다시 Hover가 반복돼 깜빡일 수 있습니다.
   - 아래 §4의 UI 가림 판정에도 패널이 끼게 됩니다.

HUD는 `FieldUnitView.AttachUnitHud`가 만든 World Space Canvas(`GraphicRaycaster` 포함, 크기 `unitHudScale` 0.02) 아래에 생성됩니다. 패널 크기는 이 배율에서 보이는 크기로 맞춰야 합니다.

## 4. 연결 뒤 바뀌는 동작 (알고 계시면 좋은 것)

- 설명이 있는 아이콘(지금은 보스의 1번째·3번째)은 `Raycast Target`이 켜지고 클릭하면 설명이 고정됩니다. 이것은 `UnitHUD`의 기존 동작입니다.
- 전투 입력은 공격 중 **UI 위의 클릭을 필드 공격으로 쓰지 않습니다**(`CombatInputRouter`, 행동 버튼 설계 B5). 그래서 보스 HUD 아이콘 바로 위를 클릭하면 그 뒤 칸 공격이 되지 않습니다. 아이콘이 작아 문제는 적을 것으로 보지만, 불편하면 알려 주세요.

## 5. 확인 방법

- EditMode: `CombatObjectiveDisplayTests`(문구 생성: "포효 3/5" 형식, 다른 특성 빈 설명, 목표 대상 고르기).
- 재생:
  1. Play From Boot 설정을 Combat 시작, Battle Definition = Database, Dev Battle Stage = 보스 스테이지로 둡니다.
  2. 밤이 될 때까지 진행한 뒤 보스 HUD 3번째 아이콘에 마우스를 올리면 "포효 0/5"가 나와야 합니다.
  3. 밤에 보스를 때릴 때마다 1씩 오릅니다. 0 피해도 셉니다. 낮에 때리면 오르지 않습니다.
  4. 5번째 피격이 들어간 명령 직후 포효하고 0/5로 돌아가므로, 화면에는 4/5 다음 0/5가 보이는 것이 정상입니다.
  5. 1번째 아이콘에는 "격파 목표"가 나와야 합니다.

## 6. 후속 후보 (지금 부탁드리는 것은 아님)

- 사령술·포효·넉백 아이콘 아트(`TraitVisual` 아이콘 칸)와 "격파 목표" 아이콘. 목표 아이콘은 지금 넣을 자리가 없어, 필요하면 둘 곳을 함께 정해야 합니다.
- 마우스를 올리지 않아도 아이콘 위에 숫자를 상시로 보이는 표시. `UnitHUD` 변경이 필요해 보스 작업 범위에서 뺐습니다. 필요하다고 보시면 알려 주세요.
