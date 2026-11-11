# 특성 외형 표 칸·상태 외형 표 — 호진님 공유 (2026-10-29, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 호진님.
> 상태: 코드·에셋 완료, **컴파일 미확인**(TA-a~TA-c는 Unity 확인 문제 없음, TA-d·TA-e는 확인 대기), 커밋 전.
> 관련 문서: [특성 발동 연출 설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md) TA2·TA9 · [계획 §6.2·§6.4·§6.5](../plans/2026-10-29-combat-trait-activation-presentation-plan.md)

## 요약

- 특성 외형 표(`TraitVisualData`)에 칸이 하나 생겼습니다. 증강 화면처럼 이름·아이콘·설명만 읽는 쪽은 영향이 없습니다(§1).
- 특성 외형 에셋 둘을 새로 만들어 표에 등록했습니다(행운의 탄환 170·연쇄 폭발 180, §1.2).
- 새 View DB 표 **상태 외형 표**(`UnitStatusVisual`)를 만들었습니다. 유닛에 걸린 상태(버프·디버프)의 HUD 칸 외형입니다(§2).

## 1. 특성 외형 표의 새 칸

### 1.1 발동 연출 줄 목록 `activations`

- 자리: `DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs`의 `TraitVisualData`. 줄 형식은 같은 폴더의 새 파일 `TraitActivationVisual.cs`(+ `.meta`)입니다.
- 줄 하나 = 발동 종류(`TraitActivationKind`) 하나. 담는 것:
  - 큐 `CombatVisualCueInput`(이펙트·흔들림, 사실 사건 큐 칸과 같은 꼴). 큐 칸은 새 값 `CombatCueEvent.TraitActivation = 12`(시점 Start만, 이펙트 자리 ActorBody·SubjectBody·EventCell)입니다.
  - 공격 표시 덮어쓰기 `CombatAttackPresentationSO`(선택, 추가 공격 줄만 — 처치 연쇄 `KillChainAttack`·추가 공격 `FollowUpAttack`). 비우면 그 유닛의 일반 공격 표시를 씁니다.
- 읽기: `TryGetActivation(kind, out …)`. 추가 공격 줄 판정 원본은 `TraitActivationVisual.IsFollowUpAttackKind` 하나입니다.
- 검사(부트 DB 생성 때, 표가 이미 부르는 `Validate()` 안): 빈 줄, 같은 발동 종류 두 줄, 정의되지 않은 종류, 큐 칸 규칙 위반, 추가 공격이 아닌 줄의 덮어쓰기, 덮어쓰기의 이펙트 큐가 행위자 지점 자리(ActorEffectPoint·ActorMuzzle — 판정 `CombatPresentationCueRules.UsesActorPoint`)를 씀 → 예외. 덮어쓰기는 어느 유닛이 쓸지 몰라 유닛마다 다른 지점 조건을 볼 수 없어서입니다.
- 공격 클립 유닛에 계열(근접/원거리)이 다른 덮어쓰기가 오면 전투 때 경고 한 번 뒤 그 유닛의 공격 표시로 칩니다(전투를 멈추지 않음).
- 지금 에셋은 모두 목록이 비어 그대로 통과합니다. Unity가 인스펙터에서 다시 저장하면 `activations: []` 한 줄이 생깁니다(값 같음). 작업 트리의 `TraitVisual_DoubleShot`·`_KillChainField`·`_Knockback.asset` 변경이 이것입니다.
- **증강 화면 영향:** 없습니다. `TraitGrowthPopupModelBuilder`가 읽는 이름·아이콘·설명은 그대로입니다.

### 1.2 새 특성 외형 에셋 둘

- `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_LuckyBullet.asset`(키 170, "행운의 탄환")·`TraitVisual_ChainExplosion.asset`(키 180, "연쇄 폭발")(+ `.meta`), `TraitVisualDataTable.asset` 끝에 등록.
- 아이콘은 비었고, 설명은 규칙 코드를 보고 쓴 초안입니다(천명님께도 문구 확인을 부탁함).
- 증강 화면에 영향: Core 전달 문서 §51의 "외형 행이 없는 특성 키 140·150·170·180" 중 170·180이 풀렸습니다. 140·150은 아직 없습니다.

## 2. 새 View DB 표 — 상태 외형 표 `UnitStatusVisual`

### 2.1 무엇

- 유닛에 걸린 상태 하나의 HUD 칸 외형입니다. 특성 외형 표와 따로 둡니다(특성은 유닛이 가진 것, 걸린 상태는 남이 건 것이라 같은 특성이 건 상태라도 아이콘·문구가 다를 수 있음).
- 키는 상태 종류 `UnitStatusKind`(None 0·Intimidation 1·DefenseReduction 2·HuntersMark 3, View 열거)입니다.
- 칸: `kind`·`displayName`(비우면 종류 이름)·`icon`(비우면 HUD 기본 아이콘)·`polarity`(Debuff 0·Buff 1)·`descriptionTemplate`(`{0}`·`{1}`에 그 상태의 값).
- HUD 설명 = "이름: 틀에 값을 넣은 글", 틀이 비면 이름만 보입니다.
- 화면 순서: 격파 목표 → 아이템 → 디버프 → 버프 → 보유 특성.

### 2.2 파일

| 종류 | 자리(+ `.meta`·폴더 `.meta`) |
|---|---|
| 표 SO | `DataBaseSystem/Data/View/Table/UnitStatus/UnitStatusVisualDataTableSO.cs` |
| 종류·구분·Data·SO | `…/UnitStatus/Item/UnitStatusKind.cs`·`UnitStatusPolarity.cs`·`UnitStatusVisualData.cs`·`UnitStatusVisualDataSO.cs` |
| 조회 | `Queries/ViewDataBase/ViewTable/UnitStatusVisualTable.cs`(`Get`·`TryGet`, 종류 검사·중복 검사) |
| 등록(연결 줄) | `ViewTableKey.UnitStatusVisual`, `IViewQuery.UnitStatusVisualTableQuery`, `ViewDataBaseFactory` 생성 줄 하나 |
| 에셋 | `DataBaseSO/ViewGroup/UnitStatusVisual/UnitStatusVisualDataTable.asset`, `Item/UnitStatusVisual_HuntersMark`·`_Intimidation`·`_DefenseReduction.asset`, `ViewGroup.asset`의 표 목록 한 줄 |

- **코드와 에셋은 한 커밋**입니다. 표 에셋이 없으면 Boot DB 생성이, 상태 에셋이 없으면 Combat 진입이 실패합니다.
- 메뉴: 표는 `ProjectL/Database/View/UnitStatusVisualDataTable`, 줄은 `ProjectL/Database/View/UnitStatusVisualData`.

### 2.3 지금 에셋 셋 (모두 디버프, 아이콘 없음)

| 에셋 | 이름 | 설명 틀 | 칸에 들어가는 값 |
|---|---|---|---|
| `UnitStatusVisual_HuntersMark` | 사냥꾼의 표식 | 다른 아군의 일반·추가 공격 피해 {0}% 증가. 공격 시 소모됩니다. | {0} 추가 피해율 |
| `UnitStatusVisual_Intimidation` | 위압 | 다음 공격까지 공격력 {0}% 감소. 우선도 {1} 감소. | {0} 공격력 감소율, {1} 우선도 감소 |
| `UnitStatusVisual_DefenseReduction` | 방어 감소 | 물리 방어력 {0}% 감소. 자기 턴이 {1}번 더 끝나면 풀립니다. | {0} 감소율, {1} 남은 자기 턴 끝 횟수 |

- 표식 문구는 옛 하드코딩(천명님 `4bd6b574`)과 같아 화면이 같습니다.
- 한계: 틀은 값을 늘 넣어서, 위압을 처음 맞으면 "우선도 0 감소"가 보입니다.

### 2.4 에셋을 고치거나 더할 때

- 문구·아이콘·구분만 바꾸면 에셋만 고칩니다.
- 칸 번호는 그 종류가 주는 값 수 안에서만 씁니다(예: 표식은 `{0}`만). 칸 수와 차례의 원본은 전투 View 상태 줄 표의 그 종류 줄이고(위 §2.3 표는 지금 값), SO 툴팁도 그 줄 표를 가리킵니다. 넘으면 Combat 진입 때 `[CombatUnitStatuses] … 설명 틀이 값 N개로 만들어지지 않습니다`로 멈춥니다. 검사는 DB 생성이 아니라 전투 진입 때입니다(값 차례의 원본이 전투 View 줄 표라서).
- 같은 종류 두 에셋, `None` 종류, 정의되지 않은 구분은 Boot DB 생성에서 멈춥니다.
- 새 상태 종류를 보이려면 Core 사실, `UnitStatusKind` 값 하나, 이 표의 에셋 하나, 전투 View 상태 줄 표(`Display/Unit/CombatUnitStatuses`)의 줄 하나가 필요합니다. 줄 표는 전투 View(용근)가 더합니다. Core가 걸린 상태 공통 목록을 내면(Core 요청 R5 보강) 줄 표의 읽기만 바뀝니다.

## 3. 부탁

- 따로 하실 일은 없습니다. 브랜치에서 `TraitVisualDataSO.cs`·`ViewGroup.asset`·`ViewDataBase.cs`를 고치셨다면 병합 때 위 칸·줄을 남겨 주세요.
- SO 모드 도구나 다른 화면에서 걸린 상태 외형이 필요하면 이 표를 쓰시면 됩니다(같은 표를 두 번 만들지 않게 알려 드립니다).

## 4. 2026-11-11 추가 — 상태 외형 새 칸 `whileAppearance`(재구성 연출, 미전달)

- 상태 외형 표 SO(`UnitStatusVisualDataSO`)에 칸 하나를 더했습니다: `whileAppearance`(GameObject). 그 상태가 유닛에 보이는 동안(HUD 아이콘과 같은 순간) 원래 모델 대신 보일 모델 프리팹입니다. 비우면 모습 그대로입니다.
- 지금 쓰는 곳은 `UnitStatusVisual_Reconstructing`(재구성 중) 하나이고, 값은 민형님 `Prefabs/Characters/New/Enemy/EnemyRecover.prefab`(잔해)입니다.
- 넣는 모델은 렌더러만 두세요. 유닛 아래 자식으로 놓여 넉백·피격 자세를 따라가고, 몸 상자·HUD·자리는 원래 유닛 것을 씁니다. 레이어는 코드가 유닛 레이어(Entity)로 맞춥니다. 충돌체·`FieldUnitView`는 넣지 않습니다.
- 상태가 여럿이면 HUD 순서의 첫 상태가 정합니다(판단은 전투 View `CombatUnitStatuses.TryGetAppearance` 한 곳).
- 코드와 에셋은 한 커밋입니다(코드 없이 에셋만 들어가면 알 수 없는 칸이라 무시되고, 에셋 없이 코드만 들어가면 잔해가 안 보일 뿐입니다).
- 설계 [reconstruction-view-design](../specs/2026-11-11-combat-reconstruction-view-design.md) RE2.

