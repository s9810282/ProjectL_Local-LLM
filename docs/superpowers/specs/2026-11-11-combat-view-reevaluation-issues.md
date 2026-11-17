# 전투 View 재평가 문제 목록 (2026-11-11)

> [재평가 보고서](2026-11-11-combat-view-reevaluation.md)의 근거 목록이다. 기준 판 `424242ab8`, **코드 변경 없음**. 정적 검토이고 컴파일·Unity 실행은 하지 않았다.
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `S/` = `Assets/ProjectL/Script/Systems/`.
> 지난 문제의 원문(위치·내용·실패 시나리오·고칠 방향)은 [11-04 문제 목록](2026-11-04-combat-view-evaluation-issues.md)에 있다. 여기에는 오늘 다시 확인한 결과만 적는다.

## 차례

1. 지금 중간 11개
2. 새로 찾은 문제 85개 (중간 3 · 낮음 81 · 기반 한계 1)
3. 지난 문제 301개 다시 확인
4. 지난 문제와 겹친 새 지적
5. 검증에서 버린 새 후보 9개

## 1. 지금 중간 11개

| id | 기준 | 출처 | 상태 | 제목 |
|---|---|---|---|---|
| [C1-22](#c1-22) | C1 | 11-04 | 그대로 | 정보창 HP 막대 규칙(분모·스냅·보호막 채움)이 UnitHUD 사본 |
| [C1-38](#c1-38) | C1 | 11-04 | 커짐 | 약한 지반 진입·붕괴 소리 프리셋을 코드 숫자로 박아 DB 설정 값을 무시 |
| [C1-42](#c1-42) | C1 | 11-04 | 그대로 | 곡사 애니메이션이 CombatAttackMotion의 공통 수명 규칙을 상속 없이 따로 구현 |
| [C1-57](#c1-57) | C1 | 11-04 | 그대로 | 특성 → 상태 근거 표가 Core의 특성 효과 규칙을 코드에 다시 적음 |
| [C1-N01](#c1-n01) | C1 | 새로 찾음 | — | 유닛 모델 Renderer 판정 규칙이 세 벌로 갈라짐(IsUnitModelPart·Cartoon 대체·상태 모습) |
| [C1-N02](#c1-n02) | C1 | 새로 찾음 | — | HUD 가용성 무효화가 Core의 여러 칸 유닛 보류 판정식을 View에 다시 적어 판단함 |
| [C1-N03](#c1-n03) | C1 | 새로 찾음 | — | '조준 넉백이 포효 넉백을 이긴다' 결정이 두 객체에 다른 기억으로 따로 있음 |
| [C2-01](#c2-01) | C2 | 11-04 | 줄어듦 | 필드 기믹(토네이도·낙석·하붑·가시·흔적)에 공통 수명 계약이 없어 10여 곳에 이름으로 나열됨(OCP) |
| [C2-13](#c2-13) | C2 | 11-04 | 커짐(낮음 → 중간) | CombatCharacterEventHandler가 피해 숫자·피격·사망·추락 사망·토네이도 던짐을 함께 맡음 |
| [C4-02](#c4-02) | C4 | 11-04 | 그대로 | 사건 Presenter 생성자가 처리기·재생기 20여 개를 직접 조립하는 두 번째 조립 지점 |
| [C10-02](#c10-02) | C10 | 11-04 | 그대로 | Unity 프레임 콜백(FieldView.LateUpdate·이펙트 따라가기·토네이도 맥동·HUD 투영)의 예외가 F1 오류 경계에 닿지 않음 |

지난 문제 8개(그때 중간 7, 낮음이었다가 커진 C2-13 하나)의 원문은 11-04 목록에 있고, 오늘 다시 확인한 근거는 §3에 있다. 새 중간 셋의 원문은 §2에 있다.

## 2. 새로 찾은 문제 85개

11-04 뒤 바뀌거나 새로 생긴 파일(바뀐 320 · 새 71)에서 찾았다. 검증 경로: ER = 증거·반론 두 검증자가 같은 판정, ERJ = 둘이 갈려 판정자가 정함, B = 낮음 묶음 검증.

### C1 SSOT (29, 중간 3)

#### C1-N01

**유닛 모델 Renderer 판정 규칙이 세 벌로 갈라짐(IsUnitModelPart·Cartoon 대체·상태 모습)** — 중간, 검증 ER

- 위치: `V/Display/Unit/FieldUnitView.cs:386-393`, `V/Display/Unit/UnitModelBoundsExclusion.cs:5-8`, `V/Display/Unit/CartoonModelMaterialFallback.cs:20-42`, `V/Display/Unit/UnitStatusAppearance.cs:104-112`
- 내용: FieldUnitView.IsUnitModelPart(TMP·Canvas·UnitModelBoundsExclusion 제외)가 '한 곳'이라고 UnitModelBoundsExclusion 문서에 적혀 있다. 그런데 CartoonModelMaterialFallback 생성자는 루트 아래 MeshRenderer·SkinnedMeshRenderer를 거르지 않고 모두 모은다(체력 숫자 TMP 3D 글자, 선택 커서 포함). UnitStatusAppearance.CaptureOriginals는 TMP·Canvas만 빼고 UnitModelBoundsExclusion은 빼지 않는다. 그런데도 주석은 '외형 층과 같은 규칙'이라고 적고 있어 이미 어긋났다.
- 실패 시나리오: 적 표시는 CreateView에서 HealthLabel(TextMeshPro, MeshRenderer)을 먼저 붙인다. 첫 그리기의 SetCartoonMaterialFallback(true)가 이 글자 재질(TMP SDF)을 Cartoon 복제본(_BaseMap=글꼴 아틀라스)으로 바꾸므로, 체력 숫자가 보이는 경로에서는 글자가 깨져 보이고 적마다 쓸모없는 Material 복제가 생긴다. 아군 랙돌 사망의 임시 Cartoon 변환도 커서 재질까지 바꾼다. 상태 표에 행동을 막지 않는 WhileAppearance 상태를 하나 더하면, 모습이 바뀐 동안 선택 아군의 커서 Renderer가 꺼져 커서가 보이지 않는다.
- 고칠 방향: IsUnitModelPart를 internal 정적 규칙 하나로 올리고, CartoonModelMaterialFallback·UnitStatusAppearance가 같은 판정을 받아 쓰게 한다.
- 검증: 이 규칙은 세 벌로 실제로 갈라져 있다. FieldUnitView.cs:386-393의 IsUnitModelPart는 TMP·Canvas·UnitModelBoundsExclusion을 빼고, UnitModelBoundsExclusion.cs:5-8은 판정을 이 한 곳만 한다고 적는다. 그런데 UnitStatusAppearance.cs:103-111은 TMP·Canvas만 빼면서 주석에는 '외형 층과 같은 규칙'이라고 적었고, CartoonModelMaterialFallback.cs:20-29는 아무것도 빼지 않고 Mesh/SkinnedMeshRenderer를 모두 모은다. 체력 글자는 3D TextMeshPro다(Combat.unity의 템플릿 스크립트 GUID 9541d86e…, FieldUnitPresenter.cs:937에서 유닛 자식으로 붙음). 그래서 적을 처음 그릴 때(FieldUnitPresenter.cs:490) TMP 재질도 Cartoon 복제본으로 바뀐다. 다만 적 캐릭터와 적 구조물은 같은 Render 안에서 늘 ShowUnitHud(true)를 받는다(FieldUnitPresenter.cs:521·559, FieldUnitView.cs:123-128). 그러면 글자가 꺼지므로 '글자가 깨져 보인다'는 주장은 지금 운영 흐름에서는 일어나지 않는다. 남는 비용은 적마다 쓰이지 않는 Material 복제 하나다. 커서…
- 같이 합친 지적: 상태 모습 교체가 '유닛 모델 부분' 규칙을 따로 베껴 커서 제외가 빠짐 / 상태 모습 교체가 유닛 모델 일부 판정을 베끼며 커서 제외 조건을 빠뜨림 / 유닛 모델 렌더러 고르기 규칙이 세 곳에 따로 있고, 새 규칙(UnitModelBoundsExclusion)은 한 곳에만 들어감

#### C1-N02

**HUD 가용성 무효화가 Core의 여러 칸 유닛 보류 판정식을 View에 다시 적어 판단함** — 중간, 검증 ER

- 위치: `V/Input/Interaction/CombatHudActionAvailability.cs:107-127`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackQueries.cs:55-64`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatActionAdmission.cs:60-72`, `V/Input/Interaction/CombatHudActionAvailability.cs:107-135`
- 내용: CombatHudActionAvailability.ObserveCharacters는 '다른 유닛의 공격 가용성에 영향을 주는 사실'을 View에서 직접 고른다. 판단식은 `Area == Field && OccupiedCells.Count != 1`로, Core CombatAttackQueries.DefersMultiCellAttack와 한 글자씩 같다. 나머지 유닛은 자기 사실(SameValues)이 바뀌었을 때만 다시 묻는다. 그래서 Core가 가용성을 정할 때 다른 유닛·환경을 보는 규칙은 View의 이 손 사본과 맞아야만 HUD 아이콘이 갱신된다. 예를 들어 MovementAllowance는 행위자 칸의 환경(VisibleTo)을 본다.
- 실패 시나리오: Core 보류 규칙이 바뀌는 경우(예: 죽은 여러 칸 유닛 제외, 또는 인접 유닛·지형에 따른 새 공격 제한 추가)를 보자. 같은 턴에 아군 B가 행동해 그 조건이 바뀌어도 자기 사실이 그대로인 아군 A는 dirty가 되지 않는다. 그러면 A의 HUD 아이콘이 턴이 끝날 때까지 옛 '공격 가능/불가'로 남는다.
- 고칠 방향: 조회 판(QueryRevision)이 바뀌면 적격 유닛을 모두 dirty로 두는 것을 기본으로 한다. 줄이고 싶으면 Core가 '가용성 의존 사실이 바뀐 유닛'을 사실로 주게 한다.
- 검증: 지적은 실재합니다. CombatHudActionAvailability.cs:113의 `Area == Field && OccupiedCells.Count != 1`은 Core CombatAttackQueries.cs:63-64의 DefersMultiCellAttack을 글자 그대로 옮긴 사본입니다. 124줄은 '자기 사실(SameValues)이 바뀌었거나 다칸 유닛 유무가 바뀌었을 때만 다시 묻는다'고 가정하는데, 이는 Core GetAvailability(CombatActionAdmission.cs:67-72)가 읽는 범위를 View가 짐작해 옮긴 것입니다. 지금은 두 쪽이 맞습니다. HUD가 보이는 CanMove·CanAttack(CombatUnitInfo.cs:166)은 행위자 자신의 사실, 프로필(UnitBehaviorRules), 다칸 보류에만 기대고, MovementAllowance의 환경 보정(VoluntaryMovementRules.cs:17-20)은 null 여부를 바꾸지 않아 CanMove에 닿지 않습니다. 그래서 지금 운영에서 결함은 없습니다. 다만 Core가 보류식을 고치거나 다른 유닛·환경에 기대는 공격·이동 제한을 더하면 View 무효화 규칙은 아무 알림 없이 낡습니다. 그러면 자기 사실이 그대로인 유닛의 HUD 아이콘이 턴 끝까지 옛 값으로 남습니다. 결과는 조건부이고 실수하기 쉬운 숨은 사본이어서 mediu…
- 같이 합친 지적: HUD 행동 가능 재조회 판단이 Core 가용성 규칙의 의존 범위·다칸 판정식을 그대로 베낌

#### C1-N03

**'조준 넉백이 포효 넉백을 이긴다' 결정이 두 객체에 다른 기억으로 따로 있음** — 중간, 검증 ER

- 위치: `V/Display/Feedback/CombatKnockbackPreview.cs:30-77`, `V/Input/Interaction/CombatRoarForecastTracker.cs:53-54`, `V/Input/Interaction/CombatRoarForecastTracker.cs:106-113`, `V/Input/Interaction/CombatRoarForecastTracker.cs:209-213`
- 내용: 포효 예고 넉백과 공격 조준 넉백이 같은 넉백 캔버스를 쓴다. 어느 쪽이 이길지를 두 곳이 따로 정한다. (1) CombatKnockbackPreview는 `_aimShown`과 포효 답을 복사한 `_roar`를 들고, 조준이 있으면 포효를 그리지 않다가 ClearAim 때 `_roar`로 다시 그린다. (2) CombatRoarForecastTracker는 `_hideKnockback = hasForeground`를 들고, 전경 입력 중에는 빈 넉백 목록을 넘겨 (1)의 `_roar` 사본을 비운다. 포효 넉백도 tracker._answers(원본), tracker._attacks(작업 목록), preview._roar(사본) 세 곳에 있다. (1)의 '지운 뒤 다시 그리기'는 (2) 때문에 전경 입력 중에는 실제로 빈 목록만 다시 그린다. 두 규칙이 서로를 보정하는 관계라 한쪽만 바꾸면 깜빡임이나 넉백 누락이 생긴다.
- 실패 시나리오: 이동 모드(전경 입력)에서는 포효 넉백을 보이게 바꾸려고 tracker의 hasForeground 조건만 '공격 조준 중'으로 좁힌다. 그러면 preview 쪽 규칙만 남는다. 공격 조준이 호버 칸을 옮길 때마다 ClearAim→DrawRoar가 포효 넉백을 한 프레임 그렸다가 다시 조준 넉백으로 바뀌어 깜빡인다. 반대로 preview의 _aimShown만 고치면 tracker가 넘긴 빈 목록 때문에 포효 넉백이 영영 돌아오지 않는다.
- 고칠 방향: 우선순위 결정을 한 곳에 둔다. 예를 들어 넉백 미리보기가 조준·포효 두 출처를 받아 스스로 고르게 하고 tracker는 답만 넘긴다. 또는 tracker가 숨김을 전부 맡고 preview는 사본 없이 받은 것만 그린다.
- 검증: 주장대로입니다. CombatKnockbackPreview.cs:30-77에서는 `_roar` 사본과 `_aimShown`으로 '조준이 이긴다'를 정하고, ClearAim 때 DrawRoar로 포효 넉백을 다시 그립니다. 이 객체의 remarks(19-20줄)와 ICombatRoarForecastFeedback.cs:12-13은 "어느 출처를 그릴지는 이 객체 한 곳/구현이 정하며, 부르는 쪽은 Core 응답을 그대로 넘긴다"고 적습니다. 그런데 CombatRoarForecastTracker.cs:106-113·209-213은 `_hideKnockback = hasForeground`(CombatSceneInput.cs:159에서 `_interaction.IsInputOwnerActive`)일 때 빈 목록을 넘깁니다. 이렇게 같은 우선순위 결정을 하나 더 들고 있고, 그 목적은 preview의 'ClearAim→DrawRoar'가 호버마다 일으킬 깜빡임을 막는 것입니다(AttackTargetingInteraction.cs:115·424의 Clear→Show 사이). 지금은 두 규칙이 서로를 보정해서 운영에서 생기는 결함은 없습니다. 하지만 문서가 결정하는 곳이 하나라고 잘못 안내해서, 한쪽만 고치면 깜빡임이나 넉백 누락이 생깁니다. 이는 '바꿀 때 실수하기 쉬운 사본'이라 medium입니다. known C1-03(필드 생존 판정식)과는…

#### C1-N04

**셰이더 속성 이름·색 속성 선택 규칙을 문자열로 다시 적음** — 낮음, 검증 B

- 위치: `V/Display/Unit/CartoonModelMaterialFallback.cs:149-162`, `V/Display/Unit/UnitModelAppearance.cs:199-202`, `V/Display/Unit/UnitModelAppearance.cs:239`, `V/Display/Unit/CombatUnitActionCursorPresenter.cs:134-156`
- 내용: CartoonModelMaterialFallback.Convert는 "_DissolveTex"·"_DissolveAmount"를 문자열로 다시 적는다. ColorShaderPropertyKey.DissolveTexture/DissolveAmount가 이미 있고, UnitModelAppearance는 그 키를 쓴다. '_BaseColor가 없으면 _Color' 선택 규칙도 UnitModelAppearance·행동 커서·Cartoon 대체 세 곳에 따로 있다.
- 검증: CartoonModelMaterialFallback.cs:158-159는 "_DissolveTex"·"_DissolveAmount"를 문자열로 적는다. UnitModelAppearance.cs:199-200은 같은 속성을 ColorShaderPropertyKey.DissolveAmount/DissolveTexture에서 얻는다. '_BaseColor가 없으면 _Color' 선택도 UnitModelAppearance.cs:239, CombatUnitActionCursorPresenter.cs:134-155, CartoonModelMaterialFallback.cs:150에 따로 있다. 셰이더 속성 이름이 바뀌면 일부만 따라가 어긋날 수 있지만, 지금은 값이 같아 낮음이다.
- 같이 합친 지적: 주 색 셰이더 속성 '_BaseColor'/'_Color' 고르기 규칙이 문자열로 여러 곳에 적혀 있음

#### C1-N05

**표면 추적이 몸 추적의 '아직 같은 묶음' 판정을 복사함** — 낮음, 검증 B

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:237-241`, `V/Display/Unit/FieldUnitPresenter.cs:249-260`
- 내용: TrackSurfacePresence 닫힘의 bound 식이 TrackBodyPresence의 식(EntityId·BodyTrackingVersion·표 대조)을 그대로 복사한다. 추적 무효 조건을 바꿀 때 두 곳을 함께 고쳐야 한다.
- 검증: FieldUnitPresenter.cs:239-240 TrackBodyPresence의 bound 식(EntityId·BodyTrackingVersion·_views 대조)을 254-255 TrackSurfacePresence가 그대로 복사한다. 추적 무효 조건이 바뀌면 두 곳을 함께 고쳐야 한다. 같은 파일 바로 아래라 놓칠 위험이 작아 낮음이다. known C1-03·C1-04는 다른 판정식이다.

#### C1-N06

**출혈 다음 턴 피해를 View가 중첩 합으로 계산** — 낮음 · 기반 한계(감점 없음), 검증 B

- 위치: `V/Display/Unit/CombatStatusDescriptionFormatter.cs:64-76`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/PeriodicStatusApplication.cs:29-32`
- 내용: BleedingDamage는 Core PeriodicStatusApplication이 중첩마다 Tick하는 규칙을 합으로 흉내 낸다. View 안에서는 한 곳(HUD 변환·설명 문구 공유)으로 모였지만, Core가 피해 규칙(중첩 상한·감쇠 등)을 바꾸면 따라 바뀌지 않는다. Core가 합계 값을 주지 않는 것은 기반 쪽 사실 모양이다.
- 검증: CombatStatusDescriptionFormatter.cs:69-76 BleedingDamage는 Core PeriodicStatusApplication.cs:30-31이 중첩마다 Tick하는 규칙을 합으로 흉내 낸다. Core가 감쇠·상한을 바꾸면 따라가지 않는다. 그러나 View 안에서는 한 곳으로 모였고(HUD 변환·설명 문구 44 공유), Core가 합계를 주지 않는 것은 기반이 주는 사실의 모양이다. 그러니 남은 사본은 기반 한계로 본다.

#### C1-N07

**건물 피격의 손실 계산·검사가 메서드마다 반복되고 null 검사가 들쭉날쭉함** — 낮음, 검증 B

- 위치: `V/Display/Unit/CombatStructureHitFeedback.cs:29-54`, `V/Display/Unit/CombatStructureHitFeedback.cs:24-54`, `V/Selection/ICombatInspectionFactsReader.cs:50-53`, `V/Playback/Support/CombatVolleyDamageNumbers.cs:93-108`
- 내용: ShowHit와 ShowContactHit가 Expected−Final 체력 손실식과 CanShow 검사를 따로 적는다. null 인자 검사는 ShowContactHit에만 있다.
- 검증: CanShow가 CanInspectStructure를 다시 적은 부분은 C1-35가 다룬다. 새 부분은 구조물 잃은 체력식이다. ShowHit(CombatStructureHitFeedback.cs:31)와 ShowContactHit(51)가 Expected−Final을 각자 적고, CombatVolleyDamageNumbers.Loss(CombatVolleyDamageNumbers.cs:102-104)는 Kind 조건과 Math.Max(0, …)를 더해 세 번째로 다르게 셈한다. 캐릭터 쪽은 CombatCharacterHits.HealthLoss 한 곳을 쓴다. 고치면 어긋남을 막는 것이 주된 이득이라 C1로 옮긴다. ShowContactHit에만 있는 null 검사 차이는 취향 수준이다. 지금 세 식의 결과는 같아 낮음이다.
- 같이 합친 지적: 구조물 피격 표시 자격·잃은 체력 식이 판정마다 다시 적혀 있음
- 처음 지적: C5 낮음 → 검증 뒤 C1 낮음

#### C1-N08

**증원 본부 호버가 Core의 '현재 넥서스' 판정식을 View에서 다시 씀** — 낮음, 검증 ER

- 위치: `V/Input/Assist/CombatNexusRangeHover.cs:33-53`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementGeometry.cs:12-21`, `S/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs:42-44`
- 내용: CombatNexusRangeHover.Refresh는 Reinforcements.IsEnabled·NexusReinforcements null을 먼저 보고, 구조물마다 EntityId == NexusId ∧ Owner == Enemy ∧ !IsDead인지 다시 판정합니다. 같은 판정은 Core의 NexusReinforcementGeometry.Area(CombatStateData.ReadNexusReinforcementArea가 부름)에 이미 있습니다. 그런데 두 사본은 벌써 어긋나 있습니다. Core는 Definition.Key == Structure_Headquarters와 국면 None/Finished를 더 보고, View는 이를 보지 않습니다. 지금은 Area가 빈 목록을 돌려주면 Clear로 끝나서 겉으로 드러나지 않습니다. View에 꼭 필요한 것은 NexusId로 구조물을 찾고 그 점유 칸에 포인터가 있는지뿐입니다.
- 실패 시나리오: Core가 넥서스 조건을 바꾼다고 합시다(예: 플레이어 쪽 증원 본부 허용, 파괴 뒤에도 범위 공개). 그러면 Core의 Area는 칸을 돌려주는데 View의 Owner == Enemy·!IsDead 검사가 먼저 걸러, 호버해도 범위가 보이지 않습니다. 고치려면 Core와 View 두 곳을 함께 찾아 바꿔야 합니다.
- 고칠 방향: View 쪽 조건은 'EntityId == NexusId이고 점유 칸에 포인터가 있음'만 남깁니다. 보일지는 ReadNexusReinforcementArea가 빈 목록인지로 정합니다.
- 검증: 코드는 주장과 같습니다. CombatNexusRangeHover.cs:34·42-43은 IsEnabled·NexusReinforcements null·Owner == Enemy·!IsDead를 다시 봅니다. 같은 판정이 NexusReinforcementGeometry.cs:16-20(Key == Structure_Headquarters, 국면 None/Finished 조건까지)에도 있고, 46줄에서 이를 CombatStateData.cs:43-44를 거쳐 다시 부릅니다. 그래서 규칙 사본이 실제로 있습니다. 다만 지금은 어긋나도 Core가 빈 목록을 돌려주면 47-48줄이 Clear로 끝나서 운영 결함이 없습니다. 실패 시나리오는 Core가 넥서스 조건을 넓힐 때만 생기는 가정이고, 그때 결과도 범위가 안 보이는 것뿐이라 시험에서 바로 드러납니다. View는 어차피 NexusId로 구조물을 찾아 점유 칸을 확인해야 하므로, 실제로 중복된 것은 한 메서드 안의 조건 두 개(Owner·IsDead)입니다. 국소적인 사본이어서 medium을 low로 내립니다. FieldUnitPresenter.cs:453의 HQ 판정은 다른 판정(정보 선택 자격)이고, C11-04(토네이도 조회 할당)와는 뿌리가 달라 중복이 아닙니다.
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N09

**튜토리얼 제한 허용표가 명령용과 미리보기용 두 switch로 나뉘어 서로 맞아야 함** — 낮음, 검증 ER

- 위치: `V/Input/CombatInputPolicy.cs:162-214`, `V/Input/CombatInputPolicy.cs:150-157`, `S/RuntimeSystem/BufferSystems/Tutorial/TutorialStepRule.cs:55-68`
- 내용: AllowsTutorialCommand와 AllowsTutorialPreview가 제한 종류(MoveOnly·AttackPractice·EndTurnOnly)마다 따로 switch를 갖습니다. 재배치 허용(AllowsTutorialRedeployment)도 별도입니다. 그런데 두 표는 서로 맞아야 합니다. 예를 들어 Attack 명령을 허용하려면 AttackRange·Attack 미리보기도 허용해야 하고, UndoMove를 허용하려면 ActionAvailability 미리보기도 허용해야 합니다. Buffer의 TutorialStepRule.CanPass는 'AttackEnemyOnce 목표는 AttackPractice 제한에서만 통과 가능' 같은 지식을 이 표를 전제로 따로 들고 있습니다. 이번에 새 미리보기 종류 RoarForecast를 StageOf에는 넣었지만 튜토리얼 표에는 넣지 않았습니다. 그래서 제한이 하나라도 걸리면 포효 예고가 조용히 나오지 않습니다(의도인지는 코드로 알 수 없음).
- 실패 시나리오: 새 실습 제한을 추가하면서 명령 표에만 Attack을 넣고 미리보기 표의 AttackRange를 빠뜨렸다고 합시다. 그러면 공격 버튼과 키가 RejectTutorialAttackMode에 막혀 공격 모드에 들어갈 수 없습니다. CanPass는 그 목표를 통과 가능으로 보므로, 튜토리얼이 끝나지 않는 상태에 빠집니다.
- 고칠 방향: 제한마다 허용 행동(이동·공격·되돌리기·턴 종료·재배치)을 적은 표 하나를 두고, 명령 종류와 미리보기 종류는 그 행동에서 파생합니다.
- 검증: 주장대로 코드에 있다. 제한마다 무엇을 허용할지가 CombatInputPolicy.cs:162-186(AllowsTutorialCommand)과 189-211(AllowsTutorialPreview) 두 switch에 따로 적혀 있다. 그런데 Router는 공격 모드를 Preview 표의 AttackRange로 막고(CombatInputRouter.cs:226-228), 버튼은 Command 표의 Attack으로 판단한다(CombatInputSurfaces.cs:65-66, CombatActionButtons.cs:66). 그래서 두 표가 서로 맞아야 한다는 약속이 코드에 드러나 있지 않다. RoarForecast는 CanQueryInStableState(150-159) → CanPreview → AllowsTutorialPreview를 거치므로, 제한이 하나라도 걸리면 `_ => false`로 막힌다. 다만 포효 예고 설계·계획 문서에는 튜토리얼 이야기가 없고, 막히는 쪽이 기본이라 지금 운영 데이터에서 생기는 결함은 확인되지 않았다. 실패 시나리오도 프로그래머가 새 제한 enum을 더하는 경우의 가정이다. 두 표는 한 파일 안에 붙어 있어 고칠 곳이 국소적이고, 빠뜨리면 실습해 보는 즉시 드러나므로 medium에서 low로 낮춘다. CanPass(TutorialStepRule.cs:55-68)는 범위 밖 Buffer/Tutoria…
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N10

**계측 #if가 요청·제출 호출식을 두 벌로 적게 하여 배포 갈래가 편집기에서 컴파일되지 않음** — 낮음, 검증 ERJ

- 위치: `V/Input/Interaction/CombatHudActionAvailability.cs:97-101`, `V/Input/Interaction/CombatUnitRangeHover.cs:311-315`, `V/Input/Interaction/CombatActionAvailabilityTracker.cs:95-99`, `V/Input/Interaction/PlacementDragInteraction.cs:366-373`
- 내용: CombatInputProfiler는 개발 빌드에만 있다. 그래서 미리보기 요청·명령 제출마다 `#if ... CombatInputProfiler.SetPreview(_x, data) #else _x.SetPreview(data) #endif` 꼴로 같은 대입식을 두 번 적는다. 이 슬라이스에만 8곳이고, CombatCommandGate에도 같은 꼴이 있다. #else 갈래는 편집기·개발 빌드에서 컴파일되지 않아 한쪽만 고친 실수가 배포 빌드에서야 드러난다. 이미 빠뜨린 곳도 있다. 새로 만든 포효 예고 요청은 감싸지 않아 RoarForecast Request 마커만 계측에서 빠졌다.
- 실패 시나리오: 요청 데이터를 바꾸는 경우를 보자. 예를 들어 CombatViewPreviewData.Move에 인자를 더하면서 #if 갈래만 고친다. 편집기·개발 빌드 시험은 모두 통과하지만 배포 빌드는 컴파일이 깨지거나, 옛 식이 남아 다르게 동작한다. 감싸기를 잊은 곳(포효 예고)은 계측 표에서 조용히 빠진다.
- 고칠 방향: CombatInputProfiler.SetPreview/Submit을 두 빌드 모두에 두고 #if는 그 안에서만 쓴다(배포에서는 바로 위임). 호출부는 한 줄로 만든다.
- 검증: 주장은 맞다. `CombatInputProfiler.cs:1-65` 전체가 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에 있다. 그래서 호출하는 곳마다 `#if 계측 호출 #else 직접 호출`을 두 벌로 적는다. 증거에 든 곳(`CombatHudActionAvailability.cs:97-101`, `CombatUnitRangeHover.cs:311-315`, `CombatActionAvailabilityTracker.cs:95-99`, `PlacementDragInteraction.cs:366-370`, `CombatMovementInteraction.cs:392-396·449-453`, `CombatMovementRangeTracker.cs:80-84`, `CombatCommandGate.cs:51-55`) 말고도 `AttackTargetingInteraction.cs:354-358·459-461·518-520`에 같은 꼴이 있어 모두 11곳이다. `CombatRoarForecastTracker.cs:128`만 감싸지 않아 RoarForecast Request 마커가 빠진 것도 사실이다. ProfilerMarker는 배포 빌드에서도 컴파일되고 아무 일도 하지 않으므로, 이 #if는 기반이 강제한 것이 아니다(`base_limit=false`). 다만 심각도는 low로 낮춘다. 두 벌이 바로 붙은 두…
- 같이 합친 지적: 계측 #if/#else가 제출·미리보기 호출 자체를 두 벌로 둠
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N11

**CancelPresentation과 Stop이 배경 조회 정리 목록을 따로 손으로 적음** — 낮음, 검증 B

- 위치: `V/Input/CombatSceneInput.cs:275-307`
- 내용: 가용성·HUD 가용성·포효 예고·환경 호버 정리가 CancelPresentation(275-282)과 Stop(288-307)에 따로 나열돼 있습니다. 이번에 포효 예고를 붙이면서도 두 곳을 모두 고쳤습니다. 다음 배경 조회를 붙일 때 한쪽을 빠뜨리면 정지 또는 표시 취소 때 예고가 남습니다.
- 고칠 방향: 배경 조회 정리를 하나의 private 메서드로 모으고, 두 경로가 그것을 부릅니다(Stop은 오류 모으기로 감쌈).
- 검증: CombatSceneInput.cs:275-282(CancelPresentation)와 288-307(Stop)이 가용성·HUD 가용성·포효 예고·환경 호버 정리를 각자 손으로 나열한다. 배경 조회를 하나 더 붙일 때 한쪽만 고치면 표시 취소 또는 정지 때 표시가 남는다. 두 경로의 차이(Stop vs Clear, CancelInputs vs Stop)는 의도된 것이지만 공통 네 줄은 같은 목록이다. 조정자 쪽 Detach·StopInput·OnInputClosed를 다룬 C1-01과는 다른 곳이라 새 문제로 본다. 낮음이다.

#### C1-N12

**배경 미리보기 요청 수명(답 확인·슬롯 잃음 판단·번호 비우기)이 네 곳에 따로 적혀 있음** — 낮음, 검증 ER

- 위치: `V/Input/Interaction/CombatHudActionAvailability.cs:70-83`, `V/Input/Interaction/CombatHudActionAvailability.cs:149-155`, `V/Input/Interaction/CombatRoarForecastTracker.cs:173-187`, `V/Input/Interaction/CombatRoarForecastTracker.cs:223-229`
- 내용: 배경 요청 규칙 중 '내도 되는가'(ShouldRequestBackground)만 CombatPreviewSlotRule 한 곳에 있다. '자기 요청을 잃었나'는 새 HUD 조회와 포효 예고에 같은 식 `!HasPreview || Preview.Number != _request || answeredNumber >= _request`로 한 번씩 적혀 있다. 호버에는 answeredNumber가 빠진 다른 꼴이, 선택 가용성 추적기에는 OwnsSlot(종류·개체까지 봄) 꼴이 있다. '내 답인가'(행위자·전투·턴·조회 판 일치)도 HUD 조회(73-74)가 가용성 추적기 Consume(128-131)을 그대로 다시 적었다. 요청 번호·대상·ClearRequest 묶음 역시 클래스마다 새로 만든다.
- 실패 시나리오: Buffer 슬롯 규칙이 바뀌는 경우를 생각해 보자. 예를 들어 Core가 답한 번호를 슬롯이 비워질 때까지 따로 두게 되는 경우다. 이때 한 사본만 고치면 다른 조회기는 남의 요청을 자기 요청이 밀려난 것으로 잘못 보고 매 Tick 다시 요청한다. 반대로 끝난 요청을 계속 기다리며 슬롯을 놓지 않을 수도 있다. 지금도 호버만 answeredNumber를 보지 않아서 같은 상황에 다르게 반응한다.
- 고칠 방향: CombatPreviewSlotRule에 'IsOwnRequestLost(hasPreview, slotNumber, own, answeredNumber)'를 둔다. 행동 가능성 답의 정체성 검사는 CombatActionAvailability 쪽 함수 하나로 모은다. 그 뒤 네 조회기가 이것을 부르게 한다.
- 검증: 사본은 실제로 있다. '잃음' 식 `!HasPreview || Preview.Number != _request || answeredNumber >= _request`가 CombatHudActionAvailability.cs:79와 CombatRoarForecastTracker.cs:183에 글자 그대로 두 번 적혀 있다. 반면 '내도 되는가' 규칙은 CombatPreviewSlotRule.cs:21-32 한 곳에만 있다. 행동 가능성 답의 정체성 검사(행위자·전투·턴·조회 판)도 HUD(73-74)와 Tracker Consume(129-131)에 따로 있다. 그래서 CombatActionAvailability에 정체성 필드가 하나 늘면 두 곳을 함께 고쳐야 한다. 다만 지금 운영 흐름에서 잘못 동작하는 곳은 없다. 호버(CombatUnitRangeHover.cs:324)는 answeredNumber를 보지 않지만, PreviewAnswered(166-170)가 자기 번호의 답이면 내용과 상관없이 받아서 요청을 비우므로 따로 확인할 필요가 없다. Tracker의 OwnsSlot(123-124)도 자기 소비 방식에 맞춘 꼴이다. 즉 네 꼴은 각 클래스의 소비 방식 차이에서 나온 것이고, 지금 어긋나서 생기는 결함은 아니다. 실패 시나리오는 Buffer 슬롯 규칙이 바뀌는 가정 위에 서 있고(CombatViewBuffer.cs:113-13…
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N13

**포효 임박 판정이 Core 카운터 의미를 증가량 없이 다시 계산함** — 낮음, 검증 B

- 위치: `V/Input/Interaction/CombatRoarForecastTracker.cs:68-83`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/BossRoarCalculator.cs:34`, `V/Input/Interaction/CombatRoarForecastTracker.cs:34-35`, `V/Input/Interaction/CombatRoarForecastTracker.cs:65-83`
- 내용: IsImminent는 `HitThreshold - HitCount <= 1`로 임박 여부를 View에서 계산한다. 같은 스냅숏에 CountPerHit가 함께 오는데도 쓰지 않는다. Core 발동 규칙은 `HitCount >= HitThreshold`이고, 한 번 맞을 때 얼마나 오르는지는 Core 데이터(RoarTraitData)가 정한다. 주석이 한계를 밝혀 두었지만, DB 값만 바뀌어도 예고가 늦어지는 사본이다.
- 고칠 방향: Core가 '다음 증가로 발동' 여부를 사실로 주게 한다. 그 전까지는 스냅숏의 CountPerHit를 써서 `HitCount + CountPerHit >= HitThreshold`로 계산한다.
- 검증: CombatRoarForecastTracker.cs:72-83의 IsImminent는 HitThreshold - HitCount <= 1(상수 35)로 임박을 정한다. 같은 스냅숏에 있는 CountPerHit·CountPerOwnSideTurnEnd·CountPerOpposingSideTurnEnd(RoarTraitStatusSnapshot.cs:16-22)는 쓰지 않는다. Core의 발동 규칙은 카운터 >= 임계다(BossRoarCalculator.cs:34). 증가량은 DB(UnitTraitData.cs:183)가 정하므로, 증가량이 2인 보스가 생기면 예고가 늦게 뜬다. 주석(68-71)이 이 한계를 밝혀 두었고 지금 데이터는 모두 1이라 낮음이다. Core가 '다음이면 발동'을 사실로 주지 않는 것은 기반 쪽 모양이지만, 이미 받은 증가량 값을 무시하는 것은 View의 몫이다.
- 같이 합친 지적: 포효 임박 판정이 Core 카운터로 '다음 한 번이면 포효'를 증가량 1로 가정해 다시 계산함

#### C1-N14

**재생 중 놓기 유지가 제출 거절 조건 중 IsPresenting 하나만 다시 적음** — 낮음, 검증 B

- 위치: `V/Input/Interaction/PlacementDragInteraction.cs:195-202`, `V/Input/Interaction/CombatItemDragInteraction.cs:116-120`, `V/Input/CombatInputPolicy.cs:85-99`
- 내용: 배치·아이템 Drag가 '명령이 거절될 것이니 손에 남긴다'를 facts.IsPresenting으로 직접 판단한다. 실제 거절 원본인 CombatInputPolicy.CanSubmit은 이것 말고도 대기 명령·Layer·국면·튜토리얼 허용으로도 거절한다. 그런 거절에서는 지금처럼 Drag가 끝나고 제출도 되지 않는다.
- 고칠 방향: '지금 놓으면 거절되는 일시 사유'를 Policy 한 곳(예: CanSubmitLater)에 두고 두 Drag가 그것을 읽게 한다.
- 검증: PlacementDragInteraction.cs:196-202와 CombatItemDragInteraction.cs:118-119가 '지금 놓으면 거절되니 손에 남긴다'를 facts.IsPresenting 하나로 각자 판단한다. 거절의 원본인 CombatInputPolicy.CanSubmit(85-99)은 대기 명령·Layer·국면·튜토리얼로도 거절한다. 그중 대기 명령처럼 곧 풀리는 사유에서는 Drag가 끝나고 제출도 되지 않는다. 결국 일시 거절 규칙의 일부만 두 곳에 사본으로 있는 것이다. Policy 안의 바탕 조건 나열(C1-09)과는 다른 곳이고 다른 규칙이다. 낮음이다.

#### C1-N15

**MayUseUnitAction가 특성 행동의 '누가 움직이나' 규칙을 따로 한 벌 더 갖고 있음** — 낮음, 검증 ER

- 위치: `V/Playback/Handlers/CombatTraitEventHandler.cs:127-145`, `V/Playback/Handlers/CombatTraitEventHandler.cs:85-125`, `V/Playback/Handlers/CombatTraitEventHandler.cs:161-194`, `V/Playback/Support/CombatPreAttackPlayback.cs:217-230`
- 내용: CombatTraitEventHandler.MayUseUnitAction(127-145)은 조기 공격이 특성 행동의 채널을 빼앗지 않게 하려고 '이 특성 사건에서 어느 유닛이 행동하는가'를 다시 계산한다. 추가 공격(ChildAttack)과 가로채기를 건너뛰고, 줄이 있는지 보고, 위치 교환이면 발동 유닛·대상, Actions의 Actor가 Source면 발동 유닛, 아니면 대상으로 본다. 실제로 행동을 여는 곳은 TryBeginBeforeFacts(85-125)·BeginUnitActions(161-194)·_positionSwap.TryBegin이다. 같은 규칙이 이쪽에는 다른 모양으로 들어 있고, 서로 맞는지 묶어 주는 것이 없다. CombatPreAttackPlayback.HasPrecedingDependency(217-230)가 이 사본으로 WaitForEntry를 정한다.
- 실패 시나리오: BeginUnitActions에 새 행동 주체(예: Source와 Targets를 함께 쓰는 Actor 값)나 새 Place 갈래를 더하고 MayUseUnitAction은 그대로 둔다. 그러면 공격자가 그 특성 사건의 행동 유닛인데도 MayUseUnitAction이 false를 돌려준다. 조기 공격이 공격자의 자세·클립 채널을 먼저 빌리고, 뒤에 온 특성 행동은 HasUnitRun 때문에 '다른 연출 중' 경고 한 번만 남기고 건너뛴다.
- 검증: 사본은 실제로 있습니다. CombatTraitEventHandler.cs:127-144의 MayUseUnitAction은 "누가 움직이나"를 혼자 다시 계산합니다. 추가 공격·가로채기를 건너뛰고, 줄이 없으면 넘기고, row.PositionSwap이면 발동 유닛과 대상을, Actor==Source이면 발동 유닛을, 아니면 대상을 봅니다. 실제로 행동을 여는 길은 BeginUnitActions(161-194), BeginRelocations(198-208), 그리고 _positionSwap.TryBegin입니다. TryBegin은 CombatPositionSwapPresentation.cs:36-49의 배치 계획 _activations로 판정하므로 기준이 다릅니다. 셋을 묶어 주는 공용 판정은 없습니다. CombatSceneEventPresenter.cs:203이 이 사본을 CombatPreAttackPlayback.cs:217-230의 HasPrecedingDependency에 넘깁니다. 다만 지금 운영 코드에서는 어긋나지 않습니다. 추가 공격(IsChildAttackStart == ChildAttack != null), 가로채기 건너뜀, Source/그 밖(None 포함) 갈래가 BeginUnitActions와 같습니다. Relocate·GuardFront도 행동 유닛을 바꾸지 않습니다. 위치 교환 판정은 실제보다 넓게 잡아서 조기…
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N16

**결과 단위 부품의 시작·끝·취소 목록이 세 곳에 흩어져 있고 이미 서로 다름** — 낮음, 검증 ER

- 위치: `V/Playback/CombatSceneEventPresenter.cs:220-239`, `V/Playback/CombatSceneEventPresenter.cs:280-296`, `V/Playback/CombatSceneEventPresenter.cs:407-432`
- 내용: 결과마다 상태를 갖는 부품(_preAttackPlayback·_preAttackStarts·_selfDestruct·_knockbackLeadIns·_damageNumbers·_feedback·_positionSwap·_movement·_players·_pacing)이 BeginPresentation(220-239)에서 Begin되고, Refresh(280-296)와 CancelMovementCore(407-432)에서 따로따로 Clear·Cancel된다. 이번 변경에서 부품 다섯 개가 이 목록들에 각각 끼어들었는데 넣은 범위가 다르다. Refresh는 _selfDestruct·_preAttackStarts·_preAttackPlayback만 비우고 _damageNumbers·_knockbackLeadIns·_feedback은 비우지 않는다. 결과 수명을 한 곳에서 책임지는 주인이 없다.
- 실패 시나리오: 결과마다 대여나 몸 유지를 붙잡는 부품을 새로 더하면서 BeginPresentation과 CancelMovementCore에만 넣고 Refresh에는 빠뜨린다(지금 _knockbackLeadIns·_damageNumbers가 이 모양이다). 그 부품이 대여를 쥐고 있으면 결과가 정상으로 끝난 뒤에도 다음 Begin 전까지 대여가 남는다. 이 상태로 결과 화면이나 다음 턴 입력 표시에 붙잡힌 모델이 보인다.
- 검증: 증거 줄은 주장과 맞다. 결과 단위 부품은 CombatSceneEventPresenter.cs:220-239(BeginPresentation)에서 시작하고, 정리는 Refresh(280-296)와 CancelMovementCore(407-432)에 손으로 따로 적혀 있다. Refresh는 _selfDestruct·_preAttackStarts·_preAttackPlayback만 비우고, _damageNumbers·_knockbackLeadIns·_feedback·_positionSwap은 비우지 않는다. 다만 실패 시나리오의 핵심인 "대여가 남는다"는 지금 코드에서 일어나지 않는다. Refresh가 비우지 않는 부품은 모두 결과 사건을 찾아보는 표일 뿐이고(CombatKnockbackLeadIns.cs:22-23의 HashSet·Dictionary, CombatVolleyDamageNumbers.cs:20-21, CombatTraitFeedbackSession.cs:11-14), 각자 Begin 첫머리에서 Clear하고 다시 채운다(CombatKnockbackLeadIns.cs:35-36, CombatVolleyDamageNumbers.cs:36, CombatPositionSwapPresentation.cs:39). 위치 교환 이펙트 묶음은 지연이 끝나면 놓인다(CombatPositionSwapPresentation.cs:86-90)…
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N17

**주체 막기 예외(밀림 동시 피격)가 막기 규칙 원본 밖 조각 실행 재생기에 따로 있음** — 낮음, 검증 B

- 위치: `V/Playback/Players/CombatPieceRunPlayer.cs:152-176`, `V/Playback/Players/CombatSubjectBlocking.cs:36-68`
- 내용: CombatSubjectBlocking은 스스로 '주체 기준 막기 규칙의 원본'이라고 적는다(36-39). 그런데 CombatPieceRunPlayer.PassesPushAlongsideHit(169-176)은 같은 질문(IsActionStart·OpensAttackSource·TryGetSubject)을 다시 묻고 그 앞에서 None을 돌려줘 원본을 건너뛴다. 막기 판정이 두 곳에 나뉘었다.
- 검증: CombatPieceRunPlayer.FindBlock(158-160)은 PassesPushAlongsideHit가 참이면 None을 돌려주고 CombatSubjectBlocking.Blocks를 부르지 않는다. PassesPushAlongsideHit(166-173)는 Blocks(60-64)의 '모든 유닛을 기다리는 사건' 분류(IsActionStart·OpensAttackSource·TryGetSubject)를 그대로 다시 적는다. 그런데 Blocks가 스스로 원본이라고 적은 문서(36-39)에는 이 예외가 없다. 그래서 Blocks에 기다림 조건이 새로 생겨도 밀림 동시 피격 경로는 그 조건을 건너뛴다. 국소적인 규칙 사본이라 낮음이다.

#### C1-N18

**밀림 앞 피격 판정 IsHit이 CombatCharacterHits.IsHitLike 식을 복사함** — 낮음, 검증 ER

- 위치: `V/Playback/Support/CombatKnockbackLeadIns.cs:25-30`, `V/Playback/Support/CombatCharacterHits.cs:17-27`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:290-296`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:287-296`
- 내용: CombatKnockbackLeadIns.IsHit은 `HealthChanged && 사건 전·뒤 생존 && 체력 감소` 식을 CombatCharacterHits.IsHitLike에서 그대로 복사해 Reconstructed만 뺀 사본입니다. CombatCharacterHits는 스스로를 이 판정의 원본이라고 적고 있습니다. 그런데 IsHit의 XML doc은 '캐릭터 처리기의 피격 시작과 이 목록이 같은 판정을 씁니다'라고 적지만, 처리기는 IsHitLike를 부르고 목록은 IsHit을 씁니다. 두 식이 같다는 것은 지금 우연히 맞을 뿐입니다.
- 실패 시나리오: 피격으로 보는 조건을 IsHitLike에서만 넓히면(예: 체력 대신 보호막만 준 사건도 피격으로 봄) 그런 피격 뒤의 넉백은 lead-in 목록에 들지 않습니다. 그러면 자세를 빌리는 보통 피격 반응이 끝난 뒤에야 밀리기 시작해서 '피격과 동시에 밀림' 연출이 그 경우에만 깨집니다.
- 고칠 방향: IsHit을 `CombatCharacterHits.IsHitLike(e) && !CombatCharacterHits.AppliesFactsAfterReaction(e)`로 바꾸고 doc을 실제 관계에 맞게 고칩니다.
- 검증: 지적은 맞습니다. CombatKnockbackLeadIns.cs:28-30의 IsHit은 CombatCharacterHits.cs:25-26에 있는 HealthChanged 갈래 식을 글자 그대로 다시 적었습니다(재구성만 뺌). CombatCharacterHits.cs:7-8은 자기가 피격 판정의 원본이라고 적고 있고, IsHit의 doc(KnockbackLeadIns.cs:26)은 처리기와 '같은 판정을 쓴다'고 하지만 처리기는 IsHitLike를 부릅니다(CombatCharacterEventHandler.cs:290). 그래서 사본이 있고, doc도 오해를 부릅니다. 다만 비용은 작습니다. 처리기는 IsHitLike를 먼저 통과시킨 뒤에야 _knockbackLeadIns.Contains를 봅니다(290·296줄). 그래서 두 식이 어긋나는 방향은 IsHitLike만 넓히는 경우 하나뿐이고, 그때 새로 들어온 피격은 보통 피격 길(298줄 이후)로 갑니다. 결과는 '맞는 순간 함께 밀림' 연출이 그 경우에만 빠지는 것이고, 예외·상태 불일치는 없습니다. 미래 변경을 전제로 한 조건부이고, 같은 폴더 안 세 줄 사본에 연출 저하만 남으므로 medium이 아니라 low로 낮춥니다. 이슈의 증거 줄 86-91은 지금 63줄짜리 파일이라 존재하지 않습니다(현재 줄 번호는 25-30). known 후보 C1-60('아직 보이지 않은 사망' …
- 같이 합친 지적: 넉백 앞 피격 판정 IsHit이 원본 IsHitLike의 식을 다시 적고, 같은 판정을 쓴다는 주석도 틀림 / 밀림 앞 피격 판정식이 피격 규칙 원본(CombatCharacterHits)과 별도로 다시 적혀 있음 / 넉백 앞 피격 판정이 피격 판정 원본의 체력 조건을 다시 적음
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N19

**공격 슬롯 '소리 있음' 판정을 DB 수 규칙 대신 >= 0으로 다시 적음** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatEventSounds.cs:91-92`, `V/Playback/Support/CombatSoundPresets.cs:28-34`
- 내용: CombatSoundPresets.AttackSlot은 없으면 `None`(= CombatPresentationValueRules.NoSoundPreset)을 돌려줍니다. 그런데 CombatSoundCue.Attack은 IsSound가 아니라 `slotIndex >= 0`으로 판정합니다. IsSound의 doc은 사건 소리 단서가 이 판정 하나를 쓴다고 적고 있습니다.
- 고칠 방향: CombatSoundPresets.IsSound(slotIndex)를 쓰거나 슬롯 전용 규칙을 DB 수 규칙에 둡니다.
- 검증: CombatEventSounds.cs:91-92 CombatSoundCue.Attack은 'slotIndex >= 0'으로 직접 판정한다. CombatSoundPresets.cs:27-34는 '소리 없음' 원본을 DB 수 규칙 NoSoundPreset/IsSoundPreset(CombatPresentationValueRules.cs:53-56)에 두고, 사건 소리 단서가 이 판정 하나를 쓴다고 적는다. 같은 파일의 Effect류(77,83,204)는 IsSound를 쓰므로 공격 슬롯만 규칙 사본이다. 지금은 값이 같아 국소적이라 낮음이다. known C1-38(약한 지반·추락 프리셋을 코드 숫자로 박음)과는 다른 곳·다른 규칙이다.

#### C1-N20

**ForPresentation이 CombatCharacterEvent 복사 인자 나열을 두 벌 가짐** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatShownFacts.cs:276-277`, `V/Playback/Support/CombatShownFacts.cs:294-296`
- 내용: 위치 보존 갈래와 끌어오기 도착 갈래가 같은 8개 인자의 생성자 호출을 손으로 따로 적습니다. 바로 이 복사에서 칸이 빠진 결함(주석의 C10-13)이 있었습니다. 사건에 칸이 늘면 두 곳을 같이 고쳐야 합니다.
- 고칠 방향: 사실만 바꾼 사본을 만드는 지역 함수 하나(WithFacts(expected, final))로 모읍니다.
- 검증: CombatShownFacts.cs:276-277과 294-296이 같은 8개 인자의 new CombatCharacterEvent(...) 나열을 손으로 따로 적는다. 296줄 위 주석이 이 복사에서 칸이 빠졌던 과거 결함(C10-13)을 언급하므로 어긋날 수 있다는 근거가 있다. 사건 칸이 늘면 두 곳을 같이 고쳐야 한다. 지금은 일치해 낮음이다. known C2-11(CombatShownFacts의 책임 과다)은 클래스 분리 문제라 이것을 풀지 않는다.

#### C1-N21

**피격 젖힘 곡선을 CombatHitLean이 원본이라 하지만 피격 반응은 같은 식을 따로 계산** — 낮음, 검증 ER

- 위치: `V/Presentation/Reaction/CombatHitLean.cs:7-11`, `V/Presentation/Reaction/CombatHitLean.cs:28-30`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:155-159`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:78-80`
- 내용: CombatHitLean은 '피격 반응과 넉백이 같은 식을 쓴다'며 곡선 sin(π×경과/피격 시간)의 원본이라고 적혀 있습니다. 넉백(CombatPushLean)은 RiseAmount를 쓰지만, 피격 반응(CombatUnitReactionPresenter.Tick)은 Mathf.Sin(Mathf.PI * (_elapsed / HitDuration))을 직접 계산합니다. 사망 기울기 축도 AxisOf와 같은 Cross 식을 다시 적었습니다.
- 실패 시나리오: 피격 반응 곡선을 바꾸려고(예: SmoothStep이나 가장 크게 젖히는 시점 변경) CombatUnitReactionPresenter.Tick의 식만 고치면, 넉백의 오르기 곡선과 오르기 시간(RiseSeconds = 피격 시간의 절반)은 옛 식을 그대로 씁니다. 그러면 '맞는 순간 젖히며 밀림'이 피격 반응과 다른 모양·시간으로 보입니다.
- 고칠 방향: 피격 반응의 젖힘 양도 CombatHitLean에 전체 곡선 함수(예: HitAmount)를 두고 그것을 부르게 합니다. 사망 축도 AxisOf를 씁니다.
- 검증: 지적은 맞습니다. CombatHitLean.cs:7-11 주석은 이 클래스가 젖힘 곡선 sin(π×경과/피격 시간)의 원본이라고 하고, RiseSeconds(22-26, 피격 시간의 절반)와 RiseAmount(29-30)는 이 곡선을 전제로 짜여 있습니다. 그런데 CombatUnitReactionPresenter.cs:157은 같은 식 Mathf.Sin(Mathf.PI*(_elapsed/HitDuration))을 따로 계산합니다. 반응 곡선만 고치면 넉백 오르기(CombatPushLean.cs:75,87)와 펴기 시간(CombatUnitMotionPlayback.cs:283)이 옛 곡선을 그대로 따릅니다. 사망 축(Presenter.cs:80)은 AxisOf(HitLean.cs:15)와 같은 Cross 식을 다시 적은 것인데, 정규화한 결과가 같아 어긋날 일이 없는 사소한 사본입니다. 운영 결함은 없고, 곡선을 바꾸는 경우에만 같은 폴더의 두 파일에서 겉모습이 어긋날 뿐입니다. 해당 기능도 '임시'라고 적혀 있어 국소적이므로 medium을 low로 낮춥니다. known_candidates C5-U0297(Version·재시작 검사)과 C7-13(머묾 구간 반복 쓰기)은 뿌리가 달라 중복이 아닙니다.
- 처음 지적: C1 중간 → 검증 뒤 C1 낮음

#### C1-N22

**CombatWaitPiece가 초 검사 규칙을 따로 다시 적었다** — 낮음, 검증 B

- 위치: `V/Presentation/Pieces/CombatWaitPiece.cs:16-21`, `V/Presentation/Pieces/CombatFlashPulses.cs:27-28`
- 내용: 다른 조각·시간표는 '시간 조건은 설정 Data 한 곳'(CombatPresentationSettingsData.ValidateSeconds, 설계 I-5)을 쓴다(CombatFlashPulses, CombatEntryEnemyRows 등). 그런데 새 CombatWaitPiece만 NaN·무한·음수 검사를 손으로 다시 쓰고, 메시지 없는 ArgumentOutOfRangeException을 던진다. 규칙이 바뀌면(예: 상한 추가) 이 조각만 어긋난다.
- 고칠 방향: CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds))를 쓴다.
- 검증: CombatWaitPiece.cs:16-21이 NaN·무한·음수 검사를 손으로 다시 적고 메시지 없는 ArgumentOutOfRangeException을 던진다. 같은 초 조건은 CombatPresentationSettingsDataSO.cs:361-365 ValidateSeconds 한 곳에 있고 CombatFlashPulses.cs:27-28 등은 그것을 부른다. C1-50은 시간 조각의 끝 판정식 사본이라 다른 규칙이므로 새 문제로 둔다.

#### C1-N23

**선행 발동 지원표가 Core의 '어떤 특성이 어떤 발동 Kind를 내는가'를 View에 다시 적음** — 낮음, 검증 B

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatPreAttackTraitKinds.cs:13-30`, `V/Playback/Ordering/CombatPreAttackTraitMap.cs:66-93`
- 내용: CombatPreAttackTraitKinds._entries는 (Pull→PullApplied, Vanguard→AttackGranted, CleanSweep→AttackGranted)처럼 특성 키와 발동 Kind의 짝을 고정해 둔다. 설정(SO)은 Kind만 고르는데, 재생 대응(CombatPreAttackTraitMap)은 Supports(trait, kind)로 특성까지 맞아야 받아들인다. Core가 같은 Kind(AttackGranted·PullApplied)를 내는 특성을 새로 만들면 이 표를 함께 고쳐야 한다. 고치지 않으면 지정 모드 공격에서 그 발동은 조용히 기준에서 빠진다. 또 공격 요약에 Pull이 있는데 대응하는 짝이 없으면 Build가 예외로 멈춘다(Map 92-93줄). 이 경로는 지정 모드를 쓰는 결과에서만 탄다.
- 고칠 방향: 지원 기준을 Kind 하나로 줄이거나(설정 선택지와 같은 축), 특성 짝이 꼭 필요하면 그 이유와 새 특성을 더할 때 고칠 곳을 표 주석에 적는다.
- 검증: CombatPreAttackTraitKinds.cs:13-18은 (Pull→PullApplied, Vanguard·CleanSweep→AttackGranted) 짝을 고정한다. CombatPreAttackTraitMap.cs:69가 Supports(trait, kind)로 특성까지 맞을 때만 받아들인다. Core는 이미 AttackGranted를 다른 특성(TraitKillSettlement.cs:39 Rampage, 69 MoraleBoost)에서도 낸다. 그래서 공격 전 발동 특성이 새로 생기면 View 표를 함께 고쳐야 하고, 빠뜨리면 그 발동은 조용히 기준에서 빠진다. 다만 지정 모드 공격 결과에서만 타는 좁은 경로라 low가 맞다.

#### C1-N24

**'적 턴 시작 사건' 판정이 처리기와 턴 표시 두 곳에 있음** — 낮음, 검증 B

- 위치: `V/Playback/Handlers/CombatTurnEventHandler.cs:19-31`, `V/Display/Turn/CombatTurnDisplayPresenter.cs:23-29`
- 내용: CombatTurnEventHandler.IsEnemyTurnStart가 걸러서 _showEnemyTurnStart를 부르는데, 받는 쪽 CombatTurnDisplayPresenter.ShowEnemyTurnStart가 같은 식(Kind==ControllerTurnStarted && Final.ControllerPhase==Enemy)을 다시 검사한다.
- 실패 시나리오: 적 턴 시작을 다른 사건 종류로 알리게 바꾸면서 한쪽만 고치면, 처리기는 간격 전에 안내를 부르지만 표시는 조용히 무시한다. 적 턴 안내가 결과 끝 그리기로 늦게 뜬다.
- 고칠 방향: 표시 쪽 검사를 빼고 처리기 판정만 남기거나, 판정을 한 정적 함수로 모아 둘 다 부르게 한다.
- 검증: CombatTurnEventHandler.cs:29-31의 IsEnemyTurnStart(Kind==ControllerTurnStarted && Final.ControllerPhase==Enemy)가 걸러서 콜백을 부른다. 그런데 받는 쪽 CombatTurnDisplayPresenter.cs:26-28이 같은 식을 다시 검사한다. 판정이 바뀔 때 한쪽만 고치면, 처리기는 간격 전에 표시를 부르지만(ShowsBeforeGap) 표시가 무시해 안내가 늦게 뜰 수 있다. 사본은 두 곳이고 짧은 식이라 low다.

#### C1-N25

**새 깜빡임 곡선 검사가 투사체 완급 곡선 검사의 상수·몸통을 복사하고 IsFinite를 또 정의함** — 낮음, 검증 B

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatFlashPulseShape.cs:15-18`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatFlashPulseShape.cs:59-91`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatProjectileEasing.cs:16-19`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatProjectileEasing.cs:68-112`
- 내용: 새 파일 CombatFlashPulseShape는 CombatProjectileEasing에서 ValidationSamples=64, Tolerance=.0001f, 키 수·유한 키·표본 0~1 범위 검사를 그대로 복사했다. 값 규칙 원본 CombatPresentationValueRules.IsFinite가 같은 계층(internal)에 있는데도, IsFinite를 이 두 파일과 CombatClipTimings에 각각 다시 정의한다.
- 실패 시나리오: 곡선 편집 허용 오차나 표본 수를 바꿀 때 한 파일만 고치면, 같은 곡선 편집기 입력이 투사체 곡선에서는 통과하고 깜빡임 곡선에서는 DB 생성 오류가 된다(또는 그 반대).
- 고칠 방향: 0~1 표본 곡선 검사(키 수·유한·표본 범위·상수)를 CombatPresentationValueRules 옆 공용 함수로 빼고, 두 곡선은 자기 고유 규칙(끝 키·단조)만 더한다. IsFinite는 CombatPresentationValueRules 것을 쓴다.
- 검증: CombatFlashPulseShape.cs:16-18의 ValidationSamples=64·Tolerance=.0001f와 FindError(59-77, 키 수·유한 키·표본 0~1 범위)가 CombatProjectileEasing.cs:17-19·81-105를 그대로 복사했다. IsFinite 사본(FlashPulseShape 89줄, ProjectileEasing 112줄, ClipTimings 132줄 대 CombatPresentationValueRules.cs:23)은 known C1-58과 겹친다. 하지만 이 문제의 핵심인 곡선 검사 몸통·상수 복사는 새 것이라 새 문제로 둔다. 그 부분만 보면 국소 사본이라 low다.

#### C1-N26

**선행 발동 대응표가 사건 형식 표를 거치지 않고 '공격을 가진 사건' 판정을 따로 적음** — 낮음, 검증 B

- 위치: `V/Playback/Ordering/CombatPreAttackTraitMap.cs:153-168`, `V/Playback/Ordering/CombatEventTypeTable.cs:257-266`
- 내용: 사건 형식별 사실은 CombatEventTypeTable이 원본이고, 공격 출처는 TryGetAttackSource로 답한다. 그런데 새 파일 CombatPreAttackTraitMap.TryGetAttack은 CombatAttackEvent와 Attack이 있는 CombatRoarEvent를 자기 switch로 따로 가린다.
- 실패 시나리오: 공격 결과를 담는 새 사건 형식을 형식 표에 한 줄 더하면 출처·Gate는 따라오지만, 선행 발동 대응표는 그 사건을 모른다. 그러면 그 공격의 당김·버프 선행 연출 기준 시작이 조용히 순차 재생으로 떨어진다.
- 고칠 방향: 형식 표 줄에 '부모 공격 결과(AttackResult)' 칸을 더하고 TryGetAttack이 그 칸을 읽게 한다(추가 공격 발동 제외 규칙도 표 쪽에 둔다).
- 검증: CombatPreAttackTraitMap.cs:153-168의 TryGetAttack은 CombatAttackEvent와 Attack이 있는 CombatRoarEvent를 자기 switch로 가린다. 사건 형식별 공격 출처의 원본은 CombatEventTypeTable.cs:257-266 TryGetAttackSource다. 원본은 추가 공격 시작도 포함하지만 이쪽은 의도적으로 뺀다. 그래도 'AttackResult를 가진 사건 형식' 목록이 두 곳에 있어, 새 공격 사건 형식을 형식 표에만 더하면 선행 발동 기준이 조용히 빠진다. known C2-23(TypeRow 위치 인자)과는 다른 문제라 새 low다.

#### C1-N27

**밀림 젖힘 여부를 표시 회전 기억의 '있음'으로 추론. 그 기억에는 방향 돌기도 씀** — 낮음, 검증 B

- 위치: `V/Presentation/Movement/CombatPushLean.cs:49-55`, `V/Presentation/Movement/CombatPushLean.cs:86-87`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:293-297`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:438-455`
- 내용: CombatUnitPresentationWindow._facing(표시 회전)에는 걸음·끌어오기의 방향 돌기(CombatUnitMotionPlayback.TurnToward/BeginTurn)와 새로 생긴 밀림 젖힘(CombatPushLean.Apply)이 함께 쓴다. 그런데 '이미 젖힌 채 밀리는 중'이라는 사실을 따로 두지 않는다. CombatPushLean.BeginStep, CombatCharacterEventHandler.TryBeginHit, CombatMovementPresentation(펴기 여부)이 각각 `TryGetFacing(id, out _)`로 추론한다. 지금은 사건 순서 덕분에 방향 돌기 기억이 밀림 전에 지워지지만, 한 기억이 두 뜻을 갖는다.
- 고칠 방향: 젖힘 상태(유닛별 '젖힘 중' 표시)를 CombatPushLean 또는 표시 창구에 명시적으로 두고, 세 호출자는 그것을 묻는다.
- 검증: CombatPushLean.cs:52는 TryGetFacing의 '있음'을 '이미 젖힘'으로 읽는다. CombatPushLean.cs:93-94의 젖힘과 CombatUnitMotionPlayback.cs:438-455의 방향 돌기가 같은 SetFacing 기억에 쓰므로, 이 기억 하나가 두 뜻을 갖는다. 같은 추론이 CombatCharacterEventHandler.cs:296(밀림 피격 길 선택)과 CombatMovementPresentation.cs:103(펴기 여부)에도 있다. 지금은 사건 순서로 방향 돌기 기억이 먼저 지워져 결함은 확인되지 않았다. 그래서 숨은 사실 사본에 따른 유지 비용인 low로 본다.

#### C1-N28

**캐릭터 사실 세 벌(Buffer·완료 스냅숏·보인 사실)을 한 표시 판단에 섞어 씀** — 낮음, 검증 B

- 위치: `V/Selection/CombatSelectionDisplay.cs:95-102`, `V/Selection/CombatSelectionDisplay.cs:131-135`, `V/Selection/CombatSelectionState.cs:17-20`, `V/Display/Feedback/CombatAttackFeedback.cs:187-195`
- 내용: CombatSelectionDisplay.RefreshActionCursors는 커서 대상·행동 상태를 `_snapshot.Characters`(마지막 결과 끝의 완료 사실)에서 읽는다. 그런데 선택 행위자는 `_selection.SelectedActorId`, 즉 재생 중인 보인 사실(CombatShownFacts)로 검증한 값을 쓴다. 이 함수는 보인 사실이 그려질 때마다 불린다(BindInspectionRendered→inspection.Refresh→RefreshInspectionSelection). 그래서 재생 중에는 보인 사실로 고른 선택과 결과 전 사실로 정한 커서 색·가시성이 섞인다. 입력 쪽 호버도 같다. Buffer 최신 목록으로 유닛을 고르지만, CombatAttackFeedback.ShowHoverRange는 같은 유닛을 완료 스냅숏에서 다시 찾고, 없으면 InvalidOperationException을 던진다. 두 목록이 같다는 보장은 '표시 확인 전에는 IsPresenting'이라는 호출 순서뿐이다.
- 고칠 방향: 커서는 선택과 같은 출처(보인 사실)에서 읽거나, 재생 중에는 갱신하지 않는다고 명시한다. 호버 범위 원점 칸은 범위 답이나 입력이 넘긴 사실에서 받아 스냅숏 재조회를 없앤다.
- 검증: CombatSelectionDisplay.cs:131-135 RefreshActionCursors는 커서 사실을 _snapshot.Characters(완료 스냅숏)에서 읽는다. 그런데 선택 행위자는 CombatSelectionState.cs:17-18처럼 보인 사실 reader로 검증하고, 이 함수는 96-102 RefreshInspectionSelection을 거쳐 재생 중에도 불린다. 호버도 같다. CombatAttackFeedback.cs:188-194는 입력이 Buffer 목록으로 고른 유닛을 스냅숏에서 다시 찾고, 없으면 InvalidOperationException을 던진다. 두 목록이 같다는 보장은 호출 순서뿐이다. 운영 결함은 확인되지 않아 low다. known C1-55·C1-15(reader 주입·선택 ID 사본)와는 다른 출처 혼합 문제다.

#### C1-N29

**조기 공격 재생이 다른 재생기의 '진행 중' 플래그 셋을 직접 조합해 시작 배타를 판단함** — 낮음, 검증 ER

- 위치: `V/Playback/Support/CombatPreAttackPlayback.cs:121-158`, `V/Playback/Support/CombatPreAttackPlayback.cs:217-229`, `V/Playback/Handlers/CombatTraitEventHandler.cs:127-143`, `V/Playback/Support/CombatAttackStart.cs:130`
- 내용: 새 CombatPreAttackPlayback.Flush는 예약된 공격 클립을 시작해도 되는지(129-130줄)와 원래 사건에 합류해도 되는지(146-147줄)를 _pieces.HasActionRun, _attacks.HasSource, _attacks.IsPlaying 조합으로 두 번 따로 판단합니다. 재생기 시작 배타의 원본은 실행 목록의 막는 이유 조회입니다(CombatPresentationPlayers.FindBlock / CombatStartQuery, Presenter의 RequireStart·CanBeginPresentationEvent). 이 판단은 그것을 거치지 않고, 공격 재생의 겹침 허용(CombatAttackPlayback의 AllowsOverlap Gate)도 보지 않습니다. CombatAttackStart 130줄에도 비슷한 플래그 조합(HasActionRun·HasUnitRun)이 따로 있습니다. HasPrecedingDependency(217-229줄)는 결과 사건 열을 직접 해석합니다. 그 일부는 특성 처리기가 이 용도로 연 내부 판정 MayUseUnitAction(특성 처리기 127-143줄)이라, 특성 처리기가 줄의 행동을 고르는 규칙(105-120줄)과 나란히 두 벌이 됩니다.
- 실패 시나리오: 공격 겹침 규칙을 바꾸거나(예: 히트 뒤 다음 공격 클립 허용) 공격자를 붙잡는 새 재생기를 실행 목록에 더해도 CombatPreAttackPlayback의 두 조합은 따라오지 않습니다. 그러면 겹침이 허용된 상황에서도 예약 클립이 IsPlaying 때문에 계속 늦어지고 '시작을 늦춥니다' 경고만 남습니다. 거꾸로 새 재생기가 쓰는 공격자 자원을 막지 못하면 대여 실패로 Discard됩니다. 특성 줄의 행동 고르기 규칙만 바꾸면 MayUseUnitAction이 어긋나 조기 공격이 선행 위치 교환보다 먼저 시작합니다.
- 고칠 방향: 조기 시작과 합류 가능 여부를 실행 목록의 시작 조회(새 CombatStartProfile)로 묻습니다. 특성 줄의 행동 판정은 특성 처리기의 실제 고르기와 같은 함수 하나로 묶습니다.
- 검증: 증거 줄은 주장과 같다. CombatPreAttackPlayback.cs:129-130과 146-147이 `_pieces.HasActionRun || _attacks.HasSource || _attacks.IsPlaying`를 같은 파일 안에서 두 번 따로 조합한다. CombatAttackStart.cs:130의 TryBeginDeferred는 HasActionRun을 한 번 더 보고 HasUnitRun까지 더한다. 그래서 '조기 공격이 공격자 자원을 지금 써도 되는가'의 판단이 세 곳에 흩어져 있고, CombatStartQuery.cs의 CombatStartProfile 주석("호출자가 불리언으로 조합하지 않고 이 값에서 정합니다")과도 어긋난다. MayUseUnitAction(CombatTraitEventHandler.cs:127-143)은 BeginUnitActions(161-175)의 가로채기·추가 공격 건너뛰기와 행위자 고르기를 손으로 옮긴 사본이다. 다만 비용은 주장보다 작다. 이 질문은 다른 사건과 겹쳐 일찍 시작해도 되느냐는 더 좁은 질문이라, FindBlock(Start)를 그대로 쓰면 반응·걸음·대기에 막혀 조기 시작이 안 된다. 따라서 '원본을 우회한다'기보다 아직 프로필이 없는 별도 규칙에 가깝다. 실패해도 대부분 안전 쪽으로 기운다. 조합이 막거나 TryBeginDeferredAttack이 false를 내면 b…
- 처음 지적: C4 중간 → 검증 뒤 C1 낮음

### C2 SOLID (5)

#### C2-N01

**표시 계약 ICombatBattleDisplay가 조정자가 안 쓰는 입력용 멤버(Feedback·CameraControl)를 실음** — 낮음, 검증 B

- 위치: `V/Display/ICombatBattleDisplay.cs:18-43`, `V/Display/CombatSceneDisplay.cs:120-130`
- 내용: 주석이 스스로 '입력 피드백 묶음과 카메라 시점은 조정자가 쓰지 않고 입력 Factory로 넘기기만 합니다. 입력 Factory 계약을 바꾸지 않으려고 여기에 둡니다'라고 적는다. 조정자 쪽 계약이 입력 조립의 편의 때문에 넓어져 있다(ISP). 입력 피드백 묶음을 바꾸면 표시 계약도 따라 바뀐다.
- 고칠 방향: 입력 Factory에는 표시 대신 입력 피드백 묶음·카메라 시점만 담은 작은 값을 넘기고, 표시 계약에서 두 멤버를 뺀다.
- 검증: ICombatBattleDisplay.cs:17의 remarks가 입력 피드백 묶음과 카메라 시점은 조정자가 쓰지 않고 입력 Factory로 넘기기만 한다고 스스로 적는다. 실제로 Feedback(33)·CameraControl(44)을 쓰는 곳은 CombatSceneInputFactory.cs:75·103뿐이다. 조정자 계약이 입력 조립 편의 때문에 넓어진 ISP 위반이고, 입력 묶음 형식을 바꾸면 표시 계약도 따라 바뀐다. 범위 안 설계 결정이라 기반 한계가 아니다. 국소적이라 낮음이다.

#### C2-N02

**Cartoon 기본 재질을 Resources 경로 문자열과 정적 캐시로 숨겨 읽음** — 낮음, 검증 ER

- 위치: `V/Display/Unit/CartoonModelMaterialFallback.cs:13-14`, `V/Display/Unit/CartoonModelMaterialFallback.cs:77-84`, `V/Display/Unit/UnitModelAppearance.cs:332-341`, `V/Display/Unit/FieldUnitPresenter.cs:490`
- 내용: CartoonModelMaterialFallback.GetDefaults가 Resources.Load("Shared/Shaders/CartoonDissolveDefaults")를 정적 필드에 캐시한다. Systems 전체에서 Resources.Load를 쓰는 곳은 이것 하나다. 연출 값은 View DB(CombatPresentationSettings)나 씬 참조로 받는 것이 이 범위의 입력 경로인데, 이 재질은 그 경로를 건너뛴다. UnitModelAppearance의 디졸브 텍스처도 같은 정적 입구에 기댄다. 진입 참조 검사(FindMissingReference)는 이 의존을 모른다.
- 실패 시나리오: 재질 파일을 옮기거나 이름을 바꾸면 설치·참조 검사는 통과한다. 그 뒤 전투 첫 적 그리기(또는 증원 적이 처음 나올 때, 또는 아군 랙돌 디졸브 시작 때) InvalidOperationException이 나서 전투 도중 F1로 멈춘다. 다른 셰이더 기본값을 쓰려면 정적 경로 상수를 고쳐야 하고 DB로는 바꿀 수 없다.
- 고칠 방향: 기본 재질을 View DB 연출 설정이나 FieldUnitPresenter의 SerializeField로 받아 Configure 때 넘기고, 참조 검사에 넣는다.
- 검증: 코드는 주장과 같습니다. CartoonModelMaterialFallback.cs:13-14에 경로 상수와 정적 캐시 `_defaults`가 있고, :77-84에서 `Resources.Load`로 읽으며 없으면 InvalidOperationException을 던집니다. Systems 안에서 Resources.Load를 쓰는 곳은 이것 하나뿐입니다. 이 재질은 FieldUnitView.cs:401 생성자(적 표시를 처음 켤 때, FieldUnitPresenter.cs:490)와 UnitModelAppearance.cs:336 디졸브 경로에서 정적 입구로 직접 불립니다. 그래서 View DB 주입이나 설치 시 참조 검사를 거치지 않는 숨은 전역 의존입니다. Convert의 :158-162도 ColorShaderPropertyKey에 이미 있는 "_DissolveAmount"·"_DissolveTex" 이름을 문자열로 다시 적습니다. 다만 Assets/ProjectL/Resources/Shared/Shaders/CartoonDissolveDefaults.mat은 지금 있으므로, 운영 데이터에서 실패가 나지는 않습니다. 실패 시나리오는 누군가 에셋 경로를 바꿀 때만 생깁니다. 또 framework-programmer-guide.md:29는 "로드 리소스"를 Resources에 두라고 하고, :485는 "Resources 이동" 때 함께 확인…
- 같이 합친 지적: CartoonModelMaterialFallback이 정적 캐시 + Resources.Load 경로 문자열로 기본 재질을 얻고, 다른 클래스도 이 정적 입구를 직접 부름(DIP) / Cartoon 기본 재질을 DB가 아닌 Resources 문자열 경로·정적 캐시로 읽음
- 처음 지적: C3 중간 → 검증 뒤 C2 낮음

#### C2-N03

**TryBeginBeforeFacts가 가상 메서드 두 벌이라 어느 쪽을 덮었는지에 따라 동작이 갈림** — 낮음, 검증 B

- 위치: `V/Playback/Handlers/CombatEventHandler.cs:34-38`, `V/Playback/Handlers/CombatStructureEventHandler.cs:30-37`, `V/Playback/CombatSceneEventPresenter.cs:514`
- 내용: 기반 클래스에 3인자와 4인자 가상 메서드가 함께 있고(34-38), 4인자는 기본으로 3인자를 부른다. 틀은 4인자만 부른다. 캐릭터 처리기는 4인자만 덮어서, 3인자로 부르면 조용히 false가 된다. 구조물 처리기는 둘 다 덮는다. 계약이 하나여야 할 단계가 두 개다.
- 검증: CombatEventHandler.cs:34-38에 3인자·4인자 가상 TryBeginBeforeFacts가 함께 있다. 틀(CombatSceneEventPresenter.cs:514)은 4인자만 부른다. CombatCharacterEventHandler(87)는 4인자만 덮어서 3인자로 부르면 조용히 false가 된다. CombatStructureEventHandler(30-37)는 시험(StructureHitPresentationTests:72,82)이 3인자를 부르니 둘 다 덮는다. 같은 단계의 계약이 두 개라 새 처리기가 어느 쪽을 덮을지 헷갈리기 쉽다. 국소적인 계약 중복이라 낮음이다.

#### C2-N04

**곡사(Mortar) 전용 지식이 일반 유닛 연출 계약과 공격 시작에 새어 나옴: 구체 무기 타입 계약 멤버와 CombatMortarStyle 다운캐스트(OCP·LSP)** — 낮음, 검증 ER

- 위치: `V/Display/Unit/ICombatUnitPresentationTarget.cs:76`, `V/Display/Unit/FieldUnitView.cs:675`, `V/Display/Unit/CombatUnitPresentationWindow.cs:338-349`, `V/Presentation/Attack/ICombatAttackStyleContext.cs:22`
- 내용: 유닛 연출 대상의 일반 계약 ICombatUnitPresentationTarget이 캐릭터 전용 구체 MonoBehaviour인 CatusCannonWeaponView를 MortarWeapon 속성으로 드러냅니다. 구현은 부를 때마다 GetComponent를 합니다. 이 멤버는 CombatUnitPresentationWindow.TryGetMortarWeapon과 ICombatAttackStyleContext.TryGetMortarWeapon으로 다시 퍼집니다. CombatAttackStyle에는 이미 종류별 가상 훅(TryResolveTrajectoryOrigin·Segment·FireShot 등)이 있는데도, 클립 공격 조립은 `CombatAttackStyles.Of(kind) is CombatMortarStyle mortar`로 구체 스타일을 꺼내 LaunchAimPoint를 부릅니다(11-06에 추가된 줄). 곡사형 무기 장치가 하나 더 생기면 계약 다섯 곳(대상 계약·FieldUnitView·창구·스타일 문맥·CombatAttackPlayback)과 공격 시작의 다운캐스트를 함께 고쳐야 합니다.
- 실패 시나리오: 곡사 궤적을 쓰는 두 번째 원거리 외형(다른 무기 컴포넌트, 혹은 CombatMortarStyle을 상속하지 않는 새 Arc 스타일)을 클립 공격으로 붙이면, CombatAttackStart의 다운캐스트가 null을 돌려준다. 그러면 발사 조준점(launchAimPoint) 없이 무기 피치가 기본 방향을 향하고, 경고 없이 조준이 틀어진다. 새 무기 컴포넌트를 지원하려면 일반 계약에 멤버를 하나 더 붙여야 한다.
- 고칠 방향: CombatAttackStyle에 virtual Vector3? LaunchAimPoint(shape, settings, mapper)(기본 null)를 두고 다운캐스트를 없앱니다. 무기 장치는 일반 역할 계약(예: ICombatWeaponRig)으로 추상화해, 표시가 처음 준비될 때 한 번 찾아 둡니다.
- 검증: 이 문제는 두 갈래다. 그중 계약 쪽(ICombatUnitPresentationTarget.cs:76의 `CatusCannonWeaponView MortarWeapon`, FieldUnitView.cs:675의 매번 GetComponent, 창구와 ICombatAttackStyleContext.cs:22로 퍼지는 사슬)은 known C2-09와 줄까지 같다. 그래서 이번 판정의 범위는 겹치지 않는 부분, 곧 2026-11-06 커밋 5f55da054에서 새로 들어온 CombatAttackStart.cs:158-160의 `CombatAttackStyles.Of(kind) is CombatMortarStyle mortar` 다운캐스트로 본다. C2-09를 고쳐도 이 다운캐스트는 그대로 남으므로 중복이 아니라 새 문제다. 코드는 주장과 같다. CombatAttackStyle.cs:21-87에는 종류별 가상 훅(DrawsArc·TryResolveTrajectoryOrigin·Segment·FireShot)이 있지만 발사 조준점 훅은 없고, LaunchAimPoint는 sealed CombatMortarStyle.cs:31-35에만 있다. 지금 종류는 Linear·Melee·Mortar 셋이고(CombatAttackPresentationKind.cs:7-9) 곡사는 Mortar 하나뿐이어서 운영 흐름은 맞게 동작한다. 실패 시나리오는 곡사형 …
- 같이 합친 지적: 클립 공격 시작이 종류 전략을 `is CombatMortarStyle`로 내려받아 곡사만 따로 처리한다 / 곡사 조준점을 전략 가상 멤버 대신 구체 타입 검사로 얻음
- 처음 지적: C2 중간 → 검증 뒤 C2 낮음

#### C2-N05

**대여 계약에서 숨은 기능을 is로 꺼냄: 클립 대여→무기 피치, 자세 대여→래그돌** — 낮음, 검증 B

- 위치: `V/Presentation/Pieces/CombatClipPiece.cs:51-53`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:81-83`, `V/Display/Unit/CombatWeaponPitchRig.cs:123-126`, `V/Display/Unit/ICombatUnitPose.cs:21-24`
- 내용: CombatClipPiece는 ICombatUnitClip 대여를 `lease is ICombatWeaponPitchClip`로, CombatUnitReactionPresenter는 ICombatUnitPose를 `pose is ICombatUnitRagdollPose`로 검사해 기능을 꺼냅니다. 지금 구현은 하나뿐이라(FieldUnitView 안의 PresentationUnitClip·PresentationUnitPose) 검사가 늘 참입니다. 하지만 계약만 보면 그 기능이 있는지 알 수 없고, 다른 구현(미리보기·래퍼)에서는 조용히 기능이 빠집니다.
- 고칠 방향: 대여 계약에 SupportsWeaponPitch/TryBeginDeathRagdoll처럼 '지원 여부 + 기본 거절' 멤버를 두거나, 창구가 대여를 줄 때 역할 계약을 함께 돌려줍니다.
- 검증: CombatClipPiece.cs:52의 `lease is ICombatWeaponPitchClip`, CombatUnitReactionPresenter.cs:81의 `pose is ICombatUnitRagdollPose`는 대여 계약에 없는 능력을 형 검사로 꺼낸다. 계약만 봐서는 지원 여부를 알 수 없고, 다른 구현에서는 조용히 기능이 빠진다. known C2-15와 뿌리(숨은 능력 다운캐스트)는 같지만 대상(ICombatGimmickRangeFeedback·FieldUnitView)이 달라, 하나를 고쳐도 이쪽은 남는다. 그래서 새 low로 둔다.

### C3 FW 지침·계약 (1)

#### C3-N01

**행동 커서 색을 ColorSystem 대신 PropertyBlock 직접 쓰기로 다시 구현** — 낮음, 검증 B

- 위치: `V/Display/Unit/CombatUnitActionCursorPresenter.cs:132-176`, `V/Display/Unit/UnitModelAppearance.cs:14-16`, `S/RuntimeSystem/GlobalSystem/RenderSystem/Core/ColorSystem/ColorTypeKey.cs:23-26`
- 내용: 새 파일 CombatUnitActionCursorPresenter.CursorEntry는 '이동함' 색을 직접 만든 MaterialPropertyBlock과 문자열 "_BaseColor"·"_Color"로 칠합니다. Global ColorSystem에는 이미 IColorSystemService.CreateColorHandle → IColorHandle.SetOverrideData와 MaterialBaseTypeKey.MainColor("_BaseColor")가 있습니다. 같은 폴더의 UnitModelAppearance는 '색은 ColorSystem의 공개 손잡이로만 바꿉니다(FW-203)'라고 적고 그 길을 씁니다. 지침 §28(있는 기능 다시 만들지 않기)과 어긋나고, 색 속성 이름 사본이 하나 더 생겼습니다(MaterialBaseTypeKey와 UnitModelAppearance의 _BaseColor/_Color 고르기).
- 고칠 방향: 커서 생성 때 IColorSystemService를 받아 CreateColorHandle로 손잡이를 만든다. 이동 표시는 SetOverrideData(MainColor 또는 UnitModelAppearance와 같은 키 고르기)로 쓴다. 커서를 파괴할 때 손잡이를 Dispose한다.
- 검증: CombatUnitActionCursorPresenter.cs:132-164의 CursorEntry는 "_BaseColor"/"_Color" 속성 ID를 직접 만들고, MaterialPropertyBlock을 만들어 SetPropertyBlock으로 색을 칠한다. 같은 폴더의 UnitModelAppearance.cs:15·318은 'ColorSystem 손잡이(CreateColorHandle)로만 색을 바꾼다(FW-203)'는 규칙대로 그 길을 쓴다. ColorTypeKey.cs:25-26에는 MainColor("_BaseColor") 키도 이미 있다. 그래서 있는 기능을 다시 만든 것이고(지침 §28), 색 속성 고르기 사본이 하나 더 생겼다. 국소적이라 낮음이 맞다. known C1-03·C5-U0183과는 관계가 없다.

### C4 기능 변경 독립성 (4)

#### C4-N01

**TurnDisplayUI 하나를 턴 안내와 배치 안내 두 주인이 숨은 차례로 나눠 씀** — 낮음, 검증 B

- 위치: `V/Display/Turn/CombatTurnDisplayPresenter.cs:31-57`, `V/Presentation/Entry/CombatDeploymentPresentation.cs:59-80`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:96`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:175`
- 내용: CombatTurnDisplayPresenter는 Combat 국면이 아니면 결과 끝마다 Clear()로 TurnDisplayUI를 닫는다. CombatDeploymentPresentation은 같은 UI로 배치 안내를 띄우고, 안내가 '완료 없이' 닫히면 Cancel해 덱 카드를 열지 않는다. 지금은 진입 연출이 남은 동안 조정자가 결과를 소비하지 않아서(Refresh가 없어서) 둘이 부딪치지 않는다. 그러나 이 안전은 조정자 Tick 차례와 표시의 RenderStatus → RenderDeployment 차례에 숨어 있다.
- 실패 시나리오: 진입 연출 중에도 결과 끝 그리기(Refresh)를 하도록 Tick을 바꾸면, 배치 국면의 RenderStatus가 Announcement 중인 '배치 단계' 안내를 Dismiss한다. HasCompleted가 거짓이라 배치 표시가 Cancel하고 덱 카드가 열리지 않는다.
- 고칠 방향: TurnDisplayUI의 열기·닫기 주인을 하나로 모으거나, 턴 안내가 배치 안내 진행 중에는 닫지 않도록 한 곳에서 판정한다.
- 검증: CombatTurnDisplayPresenter.cs:31-37은 StagePhase가 Combat이 아니면 Render 때마다 Clear로 TurnDisplayUI를 Dismiss한다(52-57). CombatDeploymentPresentation.cs:59-80은 같은 UI(CombatSceneDisplayFactory.cs:96·175 모두 root.TurnDisplay)로 배치 안내를 띄운다. 그리고 HasCompleted 없이 닫히면 Cancel해 덱 카드를 열지 않는다. 배치 국면에서 안내가 떠 있는 동안 Refresh가 돌면 RenderStatus(CombatSceneDisplay.cs:220)가 안내를 닫아 버린다. 지금 이를 막는 것은 진입 연출이 남은 동안 결과를 소비하지 않는 조정자 차례뿐이라 숨은 의존이다. known C4-04는 진입 연출 전용 멤버가 퍼진 문제라 이 두 주인 공유와는 다르다.

#### C4-N02

**행동 커서 기능이 FieldUnitPresenter를 거쳐 흩어짐** — 낮음, 검증 B

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:152-174`, `V/Display/Unit/FieldUnitPresenter.cs:669`, `V/Display/Unit/FieldUnitPresenter.cs:708-709`, `V/Display/Unit/FieldUnitPresenter.cs:887`
- 내용: 커서 프리팹·오프셋·색 SerializeField, 생성, 검증 입구, Refresh 전달, Hide/Forget/Clear/Release 호출이 모두 1,000줄짜리 FieldUnitPresenter의 Configure·Return·DiscardView·Release·ReleasePools에 박혀 있다. 그래서 커서 기능 하나를 바꿔도 유닛 표시 풀·반환 코드를 함께 읽고 고쳐야 한다. CombatSelectionDisplay는 _units.RefreshActionCursors로 한 번 더 돌아 들어간다.
- 검증: 커서 동작 자체는 CombatUnitActionCursorPresenter로 나뉘어 있지만, 프리팹·오프셋·색 SerializeField와 생성(FieldUnitPresenter.cs:152-155), 검증 입구(158-162), turn·phase 인자를 그대로 넘기는 Refresh 중계(164-167), Release·ReleasePools·Return·DiscardView의 Clear/Release/Hide/Forget 호출(708, 887-888, 911, 이후 Forget)이 1,004줄 유닛 Presenter에 박혀 있다. 그래서 커서 설정 값 하나를 늘려도 유닛 표시 Presenter를 고쳐야 하고, CombatSelectionDisplay.cs:131-135는 _units를 거쳐 돌아 들어간다. 수명 훅은 view 반환에 묶여 있어야 하므로 비용은 국소적이고 낮음이 맞다. C4-04(진입 연출)·C4-20(설정 입구 순서)과는 다른 기능이다.

#### C4-N03

**튜토리얼 금지 이동 안내 상태가 경로 입력 수명 곳곳에 섞임** — 낮음, 검증 B

- 위치: `V/Input/MovePath/CombatPathInput.cs:61-64`, `V/Input/MovePath/CombatPathInput.cs:216-219`, `V/Input/MovePath/CombatPathInput.cs:527-550`, `V/Input/MovePath/CombatPathInput.cs:582-584`
- 내용: CombatPathInput(843줄)에 금지 이동 시도 추적 필드 셋과 판정 셋이 더해졌다. 그 상태의 리셋·분기는 Detach, OnPreview 맨 앞, SelectPressed, SelectReleased, CancelInputs에 흩어져 있다. 특히 OnPreview의 이른 반환이 숨은 순서 의존이다. 튜토리얼 제한 중에는 AllowsInput이 거짓이어서 CancelInputs가 이 상태를 지우는데, 그것을 피하려고 넣은 것이다. 튜토리얼 안내 규칙을 바꾸려면 경로 지정·이동 범위 수명까지 읽어야 한다.
- 고칠 방향: 누른 칸·결과 번호·보고 판단을 작은 추적기(예: CombatBlockedMoveAttempt)로 떼어 둔다. 경로 입력은 누름·뗌만 넘기게 한다.
- 검증: CombatPathInput.cs(843줄)에 금지 이동 추적 필드 셋(62-64)과 판정 셋(528-550)이 더해졌다. 리셋은 206·461·584(CancelInputs)에, 분기는 OnPreview 앞의 이른 반환(216-219)과 472-476·497-499에 흩어져 있다. OnPreview의 이른 반환은 CancelInputs가 그 상태를 지우지 않게 하려는 숨은 순서 의존이다. 그래서 튜토리얼 안내 규칙을 바꾸려면 경로 지정 수명까지 읽어야 한다. 기능 하나에 국한된 섞임이라 낮음이다. 기존 C4-13·C1-34와는 다른 상태다.

#### C4-N04

**프로파일 표지(ProfilingEvent)를 처리기마다 #if 세 줄로 직접 달아야 함** — 낮음, 검증 B

- 위치: `V/Playback/Handlers/CombatCharacterEventHandler.cs:121-123`, `V/Playback/Handlers/CombatReinforcementEventHandler.cs:40-42`, `V/Playback/Support/CombatEventProfiler.cs:51-58`, `V/Presentation/Attack/CombatAttackPlayback.cs:186-191`
- 내용: 캐릭터·소환·증원·토네이도·낙석·하붑 처리기가 조립마다 `#if ... composition.ProfilingEvent = e; #endif`를 반복한다(캐릭터 처리기 안에만 7곳). 포효 처리기처럼 빠뜨린 곳은 다른 길(Completion.Source)로 분류된다. 새 처리기나 조립을 더할 때마다 이 줄을 기억해야 한다.
- 검증: composition.ProfilingEvent = e를 #if 안에서 손으로 넣는 곳이 처리기 여섯 곳이다(캐릭터 처리기 122·179·221·252·319·366·397, 소환 50, 증원 41, 토네이도 42, 낙석 85, 하붑 50). 포효 처리기는 넣지 않아 CombatEventProfiler.Of(53-57)의 Completion.Source 대체 길로 분류된다. 새 조립마다 이 줄을 기억해야 하고, 빠뜨리면 개발 빌드 계측 분류만 틀어진다. 운영 동작에는 영향이 없어 낮음이다. known C1-42·C1-43과는 관계없다.
- 같이 합친 지적: 계측 이름을 가변 속성 여러 층과 호출 순서로 넘김

### C5 불필요 반복·검사 (19)

#### C5-N01

**시험 입구를 걷은 뒤 남은 진입 연출·루트 null 갈래** — 낮음, 검증 B

- 위치: `V/Display/CombatSceneDisplay.cs:95-114`, `V/Display/CombatSceneDisplay.cs:173-196`, `V/Display/CombatSceneDisplay.cs:222`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:172-202`
- 내용: 운영 Factory는 CombatSceneDisplay에 entryPresentation을 늘 넘기고(new CombatSceneDisplay 호출은 Factory 한 곳, 시험 호출 없음), AttachCreated의 호출자는 Attach 하나뿐이며 늘 씬 로드 때 찾은 루트를 넘긴다. 그런데도 표시 생성자의 entryPresentation = null 기본값과 _entryPresentation?. 다섯 곳, 연결 상태의 선택 인자·EntryPresentation?. 검사, AttachCreated의 선택 인자 둘과 root == null 갈래, BackdropFor의 다른 루트 갈래가 남아 있다. 바뀐 주석이 스스로 '운영 표시는 늘 넘깁니다(없으면 null)', 'null 갈래는 지금 호출자가 없음', '운영 연결은 늘 같은 루트로 붙어 이 갈래를 타지 않습니다'라고 적는다.
- 고칠 방향: entryPresentation·preparation을 필수 인자로 바꾸고 ?. 검사, root null 갈래, BackdropFor 비교를 걷는다.
- 검증: CombatViewSystem.cs:433-436의 Attach가 root null을 거절한 뒤 AttachCreated(464)로 늘 같은 루트와 created.EntryPresentation을 넘긴다. 그런데 AttachCreated(480-481)의 선택 인자 둘, root != null 검사(510)와 root == null 갈래(512), CombatViewConnection.cs:36-37의 선택 인자, CombatLoadedScene.cs:57 BackdropFor의 다른 루트 갈래가 남아 있다. 주석도 스스로 '호출자가 없음'이라 적는다. CombatSceneDisplay.cs:95·114·173·178·196 entryPresentation null 기본값과 ?.는 known C5-09와 같지만, 조정자·연결·씬 쪽의 root/선택 인자 갈래는 C5-09·C5-06이 가리키지 않는 새 곳이다. 그래서 겹치지 않는 그 부분을 낮음으로 본다.

#### C5-N02

**빈 메서드에 계측 마커를 붙임** — 낮음, 검증 B

- 위치: `V/Display/Hud/CombatActionPanel.cs:27-32`, `V/Display/Hud/CombatActionPanel.cs:130-144`, `V/Display/Hud/HandCardHoverView.cs:85-93`
- 내용: CombatActionPanel의 HoverEnter·HoverExit는 본문이 비어 있는데, 이번에 ProfilerMarker 선언과 Auto() 범위를 더했다. 아무 일도 하지 않는 구간을 재는 마커라 Profiler에 의미 없는 항목만 늘고 코드가 길어진다. HandCardHoverView.OnPointerExit도 bool 하나를 쓰는 데 마커를 둔다.
- 고칠 방향: 빈 본문이나 한 줄 본문의 마커는 걷는다.
- 검증: CombatActionPanel.cs:30-31에서 마커를 선언하고, 본문이 빈 HoverEnter·HoverExit(130-144)에 Auto() 범위만 둔다. 아무 일도 하지 않는 구간을 재는 헛코드다. HandCardHoverView.cs:87-93도 bool 한 줄 대입에 마커를 둔다. 편집기·개발 빌드 한정이라 비용은 읽기·Profiler 잡음 수준이다.

#### C5-N03

**사실이 바뀐 유닛마다 상태 읽기를 두 번 함** — 낮음, 검증 B

- 위치: `V/Display/Unit/CombatUnitStatuses.cs:34-66`, `V/Display/Unit/FieldUnitPresenter.cs:505`, `V/Display/Unit/FieldUnitPresenter.cs:569`
- 내용: RenderFacts가 SyncStatusAppearance → TryGetAppearance에서 StatusEffectReadModel.Capture를 한 번 부르고, 바로 뒤 RenderUnitHud → AddTo에서 같은 사실로 또 부른다. 두 곳의 문서가 모두 '목록을 새로 만든다'고 적는 읽기를 같은 사실에 두 번 한다.
- 검증: 사실이 바뀐 캐릭터 하나에서 FieldUnitPresenter.cs:505 SyncStatusAppearance가 TryResolveChange를 거쳐 ResolveAppearance(188-189), 곧 CombatUnitStatuses.TryGetAppearance로 가서 StatusEffectReadModel.Capture를 부른다(CombatUnitStatuses.cs:57). 바로 뒤 506줄 RenderUnitHud도 ShowsHudFacts가 거짓이면 AddTo(569줄)에서 같은 사실로 Capture를 또 부른다(CombatUnitStatuses.cs:38). 같은 사실 객체면 두 경로 모두 건너뛰므로 사실이 바뀐 유닛에서만 생기는 국소적인 중복 읽기다. C5-U0182(예상 체력 지우기·표시 정리 두 번)와는 다른 경로다.

#### C5-N04

**호출자 없는 멤버·분기가 '지금 부르는 곳 없음' 주석과 함께 남음** — 낮음, 검증 B

- 위치: `V/Display/Unit/CombatObjectiveDisplay.cs:9-41`, `V/Display/Unit/FieldUnitView.cs:74-78`, `V/Display/Unit/UnitClipChannel.cs:118-125`, `V/Display/Unit/UnitHUDLayer.cs:28-31`
- 내용: CombatObjectiveDisplay의 ObjectiveStatusKey·ObjectiveStatusDescription·DescribeTrait, FieldUnitView.CurrentPresentationPose·ClipChannel, UnitClipChannel.HasBase·HasOverlay·Graph, UnitHUDLayer.AttachUISystem의 null 분기는 모두 이번에 바뀐 주석이 직접 '부르는 곳이 없다'고 적은 죽은 코드다.
- 검증: FieldUnitView·UnitClipChannel·UnitHUDLayer의 죽은 멤버는 C5-05가, CombatObjectiveDisplay 상수·DescribeTrait(CombatObjectiveDisplay.cs:9-41)는 C5-U0380이 이미 다룬다. 따라서 이 문제는 known과 겹치지 않는 나머지로 본다. (1) CombatPathInput.BeginPlanning(CombatPathInput.cs:262-270)은 Assets 전체에 호출자가 없다. 그런데 계측이 붙어 있고, 주석은 'SelectPressed가 부른다'고 적는다. 실제로는 480-481줄이 TryBeginPlanning(null)을 직접 부르며 'BeginPlanning이 검사한다'고 적는다. (2) CombatClipTimings.EventTime(CombatClipTimings.cs:50)과 CombatClipData.EventTime(CombatClipData.cs:43)은 운영 참조가 0인데, CombatClipEventIds.cs:59는 이것을 기준 시각으로 가리킨다. 읽기 비용만 있어 낮음이다.
- 같이 합친 지적: 이번 판에 고친 줄에 호출자 없는 멤버가 남거나 새로 생김(계측·'부르는 곳 없음' 주석까지 더함)

#### C5-N05

**CombatMapTileAlignment가 호출자 없는 껍데기로 남고 문서가 아직 그것을 원본이라 함** — 낮음, 검증 B

- 위치: `V/Display/Map/CombatMapTileAlignment.cs:1-20`, `V/Display/Map/StageViewPropPresenter.cs:22`, `V/Display/Map/StageViewPropPresenter.cs:175-185`, `V/Display/Field/FieldGridMapper.cs:47`
- 내용: StageViewPropPresenter.ShowTiles가 CombatMapPlacementLayout.Align으로 바뀌어 CombatMapTileAlignment(RequireLevel·YawCorrection·AnchorShift)를 부르는 운영 코드가 없다. 시험에서도 부르지 않는다(grep 결과 0건). 이 클래스는 CombatTileAlignmentMath를 한 줄씩 넘기기만 하는 껍데기다. 그런데 클래스 remarks는 '축 대응은 이 클래스 한 곳'이라 하고, StageViewPropPresenter remarks(22줄)와 FieldGridMapper.CornerLocal 문서(47줄)도 맞춤 계산의 주인을 CombatMapTileAlignment로 적는다. 읽는 사람이 실제 원본(CombatTileAlignmentMath·CombatMapPlacementLayout) 대신 죽은 클래스를 고치게 된다.
- 고칠 방향: CombatMapTileAlignment를 지우고, StageViewPropPresenter·FieldGridMapper 문서가 CombatMapPlacementLayout/CombatTileAlignmentMath를 가리키게 고친다.
- 검증: CombatMapTileAlignment(CombatMapTileAlignment.cs:17-46)의 세 멤버는 모두 CombatTileAlignmentMath에 한 줄로 넘기기만 한다. Assets 전체 grep에서 코드 참조는 0이고, 나오는 것은 StageViewPropPresenter.cs:22 cref, FieldGridMapper.cs:47 <c>, 에디터 도구 주석뿐이다. 그런데 remarks는 '축 대응은 이 클래스 한 곳'이라 하고, 두 문서도 이 클래스를 맞춤 계산의 주인으로 가리켜 실제 원본 대신 죽은 껍데기를 고치게 만든다. 죽은 코드를 지우는 것이 주된 해결이라 C5 낮음이다.

#### C5-N06

**회전 필드는 Build에서 예외로 막히는데 FieldGridDecal의 회전 경고·숨김 경로가 그대로 남음** — 낮음, 검증 B

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:175-185`, `V/Display/Field/FieldOutlineRenderer.cs:130`, `V/Display/Field/FieldGridDecal.cs:24`, `V/Display/Field/FieldGridDecal.cs:51-60`
- 내용: FieldOutlineRenderer.Build가 이제 Cells Root가 0.01°보다 많이 돌아가 있으면 예외를 던진다. FieldGridDecal은 FieldOutlineRenderer만 같은 cellsRoot로 만들고 Show도 Refresh에서만 부른다. 그래서 FieldGridDecal.Show의 같은 각도 검사, _warnedRotation 필드, 경고 뒤 Hide 경로는 운영에서 닿지 않는다. 그 클래스 remarks도 옛 동작('회전하지 않았을 때만 그립니다')을 설명한다.
- 고칠 방향: 회전 판정은 FieldOutlineRenderer.Build 한 곳에 두고 FieldGridDecal의 검사·필드·remarks를 걷는다.
- 검증: FieldOutlineRenderer.Build(FieldOutlineRenderer.cs:178-181)가 Cells Root 회전이 0.01°를 넘으면 예외를 던진다. FieldGridDecal은 같은 cellsRoot로 이 렌더러만 만든다(130줄, 다른 생성처 없음). 그래서 FieldGridDecal.Show의 같은 각도 검사·_warnedRotation·경고 뒤 Hide(FieldGridDecal.cs:24, 51-60)는 Build 뒤에 루트를 돌리지 않는 한 닿지 않는 겹친 방어다. 33줄 summary의 '필드가 회전돼 있으면 숨깁니다'도 이제는 예외로 막힌다는 사실과 다르다. 국소적이라 낮음이다.

#### C5-N07

**CanShowGuidance에 같은 조건이 겹쳐 있음** — 낮음, 검증 B

- 위치: `V/Input/CombatInputPolicy.cs:55-65`, `V/Input/CombatInputPolicy.cs:348-357`, `V/Input/CombatInputFactsReader.cs:66-66`
- 내용: 새 CanShowGuidance는 !facts.IsPresenting과 !CombatProgressFacts.IsUnpresented(Presented, Latest)를 함께 봅니다. 그런데 IsPresenting은 Reader가 바로 그 IsUnpresented로 채운 값이라 둘은 같은 조건입니다. 또 PassesCommon이 이미 보는 IsInputClosed·IsEntryPresentationPending을 IsInputBlocked가 다시 봅니다. CanSubmit의 공통 관문을 다시 쓰지 않고 따로 펼쳐 놓아서 겹침이 생겼습니다.
- 고칠 방향: IsUnpresented 줄을 지우고, IsInputBlocked 대신 facts.IsModalOpen만 남깁니다.
- 검증: CombatInputPolicy.cs:56-63의 CanShowGuidance는 !facts.IsPresenting(59)과 !IsUnpresented(63)를 함께 본다. 그런데 CombatInputFactsReader.cs:66이 IsPresenting을 바로 그 IsUnpresented로 채우므로 둘은 같은 조건이다. IsInputBlocked(50-53)의 IsInputClosed·IsEntryPresentationPending은 PassesCommon(348-357)이 이미 본다. StagePhase가 Deployment 또는 Combat인지 묻는 61줄도 enum 값이 둘뿐이라(StagePhase.cs:5-6) 늘 참이다. 새 함수 하나 안의 겹친 검사라 C1-09(함수마다 바탕 조건을 나열함)와는 따로 본다. 낮음이다.

#### C5-N08

**늘 넘어오는 캐릭터 목록에 대한 null 갈래가 일부 남아 동작이 갈림** — 낮음, 검증 B

- 위치: `V/Input/Interaction/CombatUnitRangeHover.cs:225`, `V/Input/Interaction/CombatItemDragInteraction.cs:145`, `V/Input/Interaction/CombatItemDragInteraction.cs:185`, `V/Input/Interaction/PlacementDragInteraction.cs:270`
- 내용: 이번 정리(C5-02)는 '부르는 곳이 Buffer 목록을 늘 넘긴다'를 전제로 공격 취소·FindDeckUnit의 null 갈래를 걷었다. 그런데 유닛 호버 Pick·아이템 FindTarget/RequestUnequip·배치 TryReturnUnderPointer에는 null이면 조용히 넘어가는 갈래가 남았다. 같은 묶음의 포효·가용성 추적기는 같은 목록이 null이면 예외를 던진다.
- 고칠 방향: 남은 null 갈래를 걷는다(또는 묶음 입구 한 곳에서 ArgumentNullException으로 통일한다).
- 검증: characters == null일 때 조용히 넘어가는 갈래가 아직 남아 있다. CombatUnitRangeHover.cs:225, CombatItemDragInteraction.cs:145·185, PlacementDragInteraction.cs:270이다. 같은 묶음의 CombatRoarForecastTracker.cs:160-161과 CombatActionAvailabilityTracker.cs:61-62는 같은 목록이 null이면 ArgumentNullException을 던진다. 부르는 곳이 늘 Buffer 목록을 넘기므로 앞의 갈래들은 닿지 않는 방어 검사이고, 오류 정책도 둘로 갈린다. 정리 수준이라 낮음이다.

#### C5-N09

**조기 공격 완료 검사를 결과 시작 경로에서 세 번 함** — 낮음, 검증 B

- 위치: `V/Playback/CombatSceneEventPlayback.cs:76`, `V/Playback/CombatSceneEventPresenter.cs:223-243`
- 내용: CombatSceneEventPlayback.BeginPresentation이 _events.CanBeginResult(76)를 보는데, 그 안에 이미 _preAttackPlayback.IsComplete가 있다(243). 그 뒤 사건 Presenter의 BeginPresentation이 같은 조건을 또 검사해 던진다(223).
- 검증: CombatSceneEventPlayback.BeginPresentation(76-77)은 _events.CanBeginResult가 거짓이면 던진다. CanBeginResult(Presenter 243)에는 이미 _preAttackPlayback.IsComplete와 FindBlock(Result)가 들어 있다. 그런데 하나뿐인 호출자 뒤에서 Presenter.BeginPresentation(223-226)이 IsComplete를 또 검사해 던지고, RequireStart(Result())로 막기 조회도 다시 한다. 겹친 방어 검사라 헛일만 있고 결함은 없으니 낮음이다. known C5-16(Core 보장 재검사)이나 C5-U0222(이동 중간 계층)와는 다른 곳이다.

#### C5-N10

**소속 맵이 포효 공격까지 묶고 검증하지만 소비자는 일반 공격만 씀** — 낮음, 검증 B

- 위치: `V/Playback/Ordering/CombatPreAttackTraitMap.cs:57-97`, `V/Playback/Ordering/CombatPreAttackTraitMap.cs:153-168`, `V/Playback/Support/CombatPreAttackPresentationStarts.cs:35-58`, `V/Playback/Support/CombatPreAttackPlayback.cs:69-74`
- 내용: CombatPreAttackTraitMap.TryGetAttack(153-168)은 CombatRoarEvent도 출처로 넣어 소속 검사·바인딩을 만든다. 그런데 설정을 모으는 CombatPreAttackPresentationStarts.Begin(35-58)과 예약하는 CombatPreAttackPlayback.Reserve(69-88)는 CombatAttackEvent만 받는다. 포효 쪽 바인딩은 쓰이지 않는 일이고, 그 소속이 깨지면 쓰지도 않는 데이터 때문에 Build가 결과 전체를 던진다.
- 검증: CombatPreAttackTraitMap.TryGetAttack(153-168)은 CombatRoarEvent도 출처로 넣어 Build(57-97)에서 소속 검사와 바인딩을 만든다. 그런데 CombatPreAttackPresentationStarts.Begin(41-48)은 _settings에 CombatAttackEvent 색인만 넣는다. TrySelect가 _settings[EventIndex]로 거르니 포효 바인딩은 절대 쓰이지 않는다. Reserve(CombatPreAttackPlayback.cs:72)도 CombatAttackEvent만 받는다. 쓰지 않는 바인딩의 검증이 실패하면 그 결과의 Build가 던지는 경로도 생긴다. 다만 이것은 Core 데이터가 깨진 경우뿐이라 낮음으로 둔다.

#### C5-N11

**호출자가 늘 넘기는 선택 인자와 쓰지 않는 대체값이 남음** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatAbilityMotionSettings.cs:12-13`, `V/Playback/Support/CombatCameraMotionSettings.cs:23-25`, `V/Playback/Support/CombatEventPresentationValues.cs:26-48`, `V/Playback/Support/CombatTraitFeedbackPlayback.cs:34-38`
- 내용: CombatAbilityMotionSettings.Instant와 CombatCameraMotionSettings.None은 doc에 직접 '지금 쓰는 곳은 없습니다'라고 적혀 있는 죽은 멤버입니다. CombatEventPresentationValues의 fallMotion·stepLook·leapMotion = null 기본값과 `??` 대체도, 유일한 생성 길인 From이 늘 값을 넘겨 닿지 않습니다. CombatTraitFeedbackPlayback의 `preAttackStarts = null`도 유일한 호출자(presenter 183)가 늘 넘기는데, 그래서 `_preAttackStarts?.`가 4곳에 생겼습니다.
- 고칠 방향: Instant·None 멤버와 null 기본값을 지우고 인자를 필수로 받습니다. preAttackStarts도 필수 인자로 받아 null 검사를 합니다.
- 검증: CombatAbilityMotionSettings.cs:12-13 Instant와 CombatCameraMotionSettings.cs:23-25 None은 doc에 '지금 쓰는 곳은 없습니다'라고 적혀 있고, grep해도 정의 줄 말고는 참조가 없다. CombatEventPresentationValues.cs:40-48의 fallMotion·stepLook·leapMotion = null과 ?? 대체는 유일한 생성인 From(68)이 늘 값을 넘겨 닿지 않는다. CombatTraitFeedbackPlayback.cs:35-37의 preAttackStarts = null은 유일한 호출(Presenter 183)이 늘 넘기는데, 그래서 ?. 4곳(80,118,136,145)이 생겼다. 원인은 known C5-08과 같은 종류지만 C5-08은 이동 재생·처리기 파일을 가리켜 고쳐도 이곳은 풀리지 않으므로 새 문제다.

#### C5-N12

**CombatVolleyDamageNumbers의 겹친 검사** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatVolleyDamageNumbers.cs:37-45`, `V/Playback/Support/CombatVolleyDamageNumbers.cs:53-66`
- 내용: ContactNumbers는 입구를 만들 때 events를 모두 `_split`에 넣으므로, 람다 안의 `!_split.Contains(item)`은 같은 세대에서 늘 거짓입니다. _split은 Begin·Clear에서만 비워지고, 그때는 세대 검사가 앞에서 막습니다. 게다가 `return`이라 하나만 빠져도 나머지 대상의 숫자까지 건너뜁니다. Begin의 `events.Contains(item)`은 result.Events를 한 번만 돌므로 늘 거짓이고, List 선형 검색만 더합니다.
- 고칠 방향: 두 Contains 검사를 지우고 세대 검사만 남깁니다.
- 검증: CombatVolleyDamageNumbers.cs:54-55가 입구를 만들 때 events를 모두 _split에 넣는다. _split은 Clear(87-90)에서만 비워지고 Clear는 결과 시작·취소 때 세대 변경과 함께 불리므로, 62줄의 !_split.Contains(item)는 같은 세대에서 사실상 늘 거짓이다. 45줄 Begin의 events.Contains(item)도 result.Events를 한 번만 돌아 겹친 선형 검색일 뿐이다. 동작 비용은 작고 읽기 비용만 있어 낮음.

#### C5-N13

**카메라 연결이 늘 카메라 하나·효과 서비스 있음인데 여러 개·없음 경로를 유지** — 낮음, 검증 B

- 위치: `S/Factory/Runtime/InGame/View/CombatCameraFactory.cs:14-33`, `V/Presentation/Cameras/CombatCameraView.cs:25`, `V/Presentation/Cameras/CombatCameraView.cs:48-60`, `V/Presentation/Cameras/CombatCameraSnapshot.cs:19-30`
- 내용: CombatCameraFactory는 binding·effects가 null이면 예외를 던지고, 스냅숏 하나만 담은 배열을 넘깁니다. 바인딩은 ImpulseListener 채널이 0이 아니어야 통과시킵니다. 그런데도 CombatCameraView에는 카메라 0개 경로(HasCameras)와 여러 카메라의 처음 렌즈값이 같은지 보는 검사가 남아 있고, CombatCameraWindow에는 _effects == null 경로가 남아 있습니다. CombatCameraSnapshot의 선택 인자 transform = null 경로도 남아 있는데, 유일한 생성 경로는 Capture이고 언제나 transform을 넘깁니다. CombatCameraView는 ICombatCameraControl을 구현하지만 밖에 내보이는 것은 Session.Control뿐입니다.
- 고칠 방향: 카메라 하나 계약으로 좁힙니다(배열을 단일 스냅숏으로, transform 필수로, effects 필수로). View의 인터페이스 구현을 걷거나 Session을 없애고 View가 수명 검사를 맡게 합니다.
- 검증: CombatCameraFactory.cs:14-33은 binding·effects null을 거부하고 스냅숏 하나짜리 배열만 넘긴다. 그런데도 CombatCameraView.cs:50-51·80-94의 카메라 0개 경로(HasCameras)와 58-61의 여러 카메라 렌즈값 비교, CombatCameraWindow.cs:56·59의 _effects != null 검사가 남아 있다. CombatCameraView가 ICombatCameraControl을 구현(25)하지만, 밖에는 CombatCameraSession.Control(Session.cs:25, CombatSceneDisplay.cs:130)만 나간다. 이 중 Snapshot의 transform = null 부분(Snapshot.cs:19-30, View 56-57)은 known C5-07과 같지만, 카메라 개수·효과 없음 일반화가 핵심인 새 부분이라 새 문제로 보고 낮음으로 매긴다.

#### C5-N14

**카메라 구성 검사가 한 번 연결에서 여러 번 겹침** — 낮음, 검증 B

- 위치: `V/Presentation/Cameras/CombatSceneCameraBinding.cs:21-60`, `S/Factory/Runtime/InGame/View/CombatCameraFactory.cs:24-25`, `V/Presentation/Cameras/CombatCameraView.cs:91-107`, `V/Presentation/Cameras/CombatCameraZoomPose.cs:20-48`
- 내용: CombatCameraFactory.Create가 binding.ReadSettings()와 binding.Capture()를 차례로 부르고, 둘 다 ValidateReferences를 처음부터 다시 실행합니다. ValidateReferences는 CombatCameraZoomPose.ValidateCamera와 IsBound 검사를 하는데, UseZoomPose이면 CombatCameraView 생성자가 ValidateCamera를, View.Begin이 IsBound를 또 확인합니다. 바인딩은 UseZoomPose와 관계없이 ValidateCamera를 돌리므로, 자세 줌을 쓰지 않는 설정에서도 Body·Aim 단계가 있는 카메라를 거절합니다.
- 고칠 방향: 검사를 바인딩 한 번(ValidateReferences 한 번 뒤 settings·snapshot을 함께 돌려주는 입구)으로 모읍니다. 자세 줌 검사는 UseZoomPose일 때 한 곳에서만 합니다.
- 검증: CombatCameraFactory.cs:24-25가 ReadSettings와 Capture를 차례로 부르고, 둘 다 ValidateReferences(CombatSceneCameraBinding.cs:22-33)를 처음부터 다시 돈다. ValidateReferences(53-56)의 ValidateCamera·IsBound 검사는 UseZoomPose일 때 CombatCameraView.cs:68(생성자)과 104-106(Begin)이 또 한다. 또 바인딩은 UseZoomPose와 상관없이 ValidateCamera를 부르는데, 이는 ZoomPose.cs:19 doc('SO에 켜진 카메라만 검사')과 어긋난다. 연결 때 한 번 도는 경로라 비용은 읽기·유지 비용이고, 자세 줌을 끈 설정에서 Body/Aim 카메라를 거절하는 것도 시작 때 명확한 예외로 드러나므로 낮음이다. known C5-07·C1-47과는 다른 곳이다.

#### C5-N15

**운영 호출자가 없는 생성자·멤버가 새로 남음(시험 전용)** — 낮음, 검증 B

- 위치: `V/Presentation/Effects/CombatEffectWindow.cs:58-86`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:135-136`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:96-97`, `V/Presentation/Cameras/CombatCameraZoomPosePath.cs:16-45`
- 내용: CombatEffectWindow의 surfaceOf 없는 두 생성자(Identity 투영)는 운영 조립에서 쓰지 않습니다. 운영은 두 곳 모두 surfaceOf를 넘깁니다. CombatCameraZoomPosePath의 internal 생성자는 시험에서만 쓰는데, 이 생성자는 _reference를 zoomOut으로 정하므로 운영에서 EvaluateAtReference와 함께 쓰면 자세가 틀립니다. CombatBodyTextWindow.Show는 TryShow로 옮긴 뒤 운영 호출자가 없고, CombatSceneCameraBinding.CameraName도 호출자가 없습니다.
- 고칠 방향: surfaceOf를 필수로 하고 Identity 생성자를 걷습니다. 자세 경로 생성자는 private으로 하고 시험은 FromOffsets를 씁니다. Show와 CameraName은 지웁니다.
- 검증: CombatEffectWindow.cs:58-61·74-77의 Identity 생성자는 시험(Assets/Tests 두 곳)에서만 쓰고, 운영(CombatSceneDisplayFactory.cs:135, CombatTraitPreviewFactory.cs:96)은 surfaceOf를 넘긴다. CombatCameraZoomPosePath.cs:15-26의 internal 생성자는 _reference=zoomOut으로 정하는데, 외부 호출은 시험뿐이다(운영은 FromOffsets→private 생성자). CombatBodyTextWindow.cs:67-70 Show와 CombatSceneCameraBinding.cs:18-19 CameraName은 grep해도 호출자가 없다. known C5-06·C5-09와 같은 종류지만 다른 파일이라 새 문제다.

#### C5-N16

**선로 착지 소리 경로가 const false로 늘 꺼진 죽은 코드다** — 낮음, 검증 B

- 위치: `V/Presentation/Entry/CombatEntryRailwayAppearance.cs:14`, `V/Presentation/Entry/CombatEntryRailwayAppearance.cs:85`, `V/Presentation/Entry/CombatEntryRailwayAppearance.cs:114-123`
- 내용: RailLandingSoundEnabled가 const false라 Collect에서 RailLandingPending = isRail && false가 늘 거짓이고, Tick의 소리 세기 갈래(_railLandingsSinceSound, '2개마다 한 번' 숫자 2, SfxEvent.RailDeploy)는 운영에서 닿지 않는다. 그런데도 생성자는 이 경로만을 위해 IAudioSystemService를 필수로 받고 Cancel은 세기 값을 비운다. 켜고 끄는 결정이 코드 상수에 묻혀 있고, 간격 숫자 2도 설정 표가 아닌 코드에 적혀 있다.
- 고칠 방향: 쓰지 않을 거면 소리 갈래·필드·생성자 인자를 지우고, 남길 거면 켜기와 간격을 철도 낙하 설정(CombatRailwayDropSettings)으로 옮긴다.
- 검증: CombatEntryRailwayAppearance.cs:14가 const false라 85줄 RailLandingPending는 늘 거짓이고 114-123줄 소리 갈래(_railLandingsSinceSound, 숫자 2, SfxEvent.RailDeploy)는 운영에서 닿지 않는다. 그런데도 생성자(36-42)는 이 경로만을 위해 IAudioSystemService를 필수로 받고 Cancel(129)은 세기를 비운다. 죽은 경로가 인자 사슬까지 끌고 다니는 국소 헛일이다.

#### C5-N17

**적 턴 시작 표시 콜백이 null 기본값 선택 인자로 Factory·Presenter·처리기 세 층을 지남** — 낮음, 검증 B

- 위치: `S/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs:22-32`, `V/Playback/CombatSceneEventPresenter.cs:117`, `V/Playback/Handlers/CombatTurnEventHandler.cs:13-26`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:153-155`
- 내용: 새로 넣은 `Action<CombatTurnEvent> showEnemyTurnStart = null`은 CombatEventPlaybackFactory.Create, CombatSceneEventPresenter 생성자, CombatTurnEventHandler 생성자에 모두 null 기본값으로 붙는다. 처리기는 `_showEnemyTurnStart != null`과 `?.Invoke`로 이를 다시 확인한다. 운영 조립은 늘 이 값을 넘기고, 빠지는 곳은 특성 미리보기 하나뿐이다. 프로젝트는 선택 인자를 걷어 내는 정리를 해 왔는데(평가 C5-02), 같은 꼴을 새로 더한 셈이다. 운영에서 실수로 빠뜨려도 조용히 표시가 사라진다.
- 고칠 방향: 필수 인자로 바꾸고, 미리보기는 아무것도 하지 않는 콜백을 직접 넘긴다.
- 검증: showEnemyTurnStart가 CombatEventPlaybackFactory.cs:22, CombatSceneEventPresenter.cs:117, CombatTurnEventHandler.cs:15에 모두 `= null` 선택 인자로 붙어 있고, 처리기는 21줄 `_showEnemyTurnStart != null`과 26줄 `?.Invoke`로 다시 확인한다. 운영은 CombatSceneDisplayFactory.cs:153에서 늘 넘기고, 미리보기(CombatTraitPreviewFactory.cs:104)만 생략한다. 그래서 운영에서 이 인자를 빠뜨려도 컴파일되고 적 턴 안내가 조용히 결과 끝으로 밀린다. known C5-08과 꼴은 같지만 다른 처리기·인자 사슬이라 새 문제로 둔다. 국소적인 정리 수준이라 low다.

#### C5-N18

**조정자가 표시 계약(ICombatBattleDisplay)과 구체 CombatSceneDisplay에 함께 의존하고, 호출자 하나뿐인 AttachCreated에 닿지 않는 갈래가 남음** — 낮음, 검증 B

- 위치: `V/CombatViewSystem.cs:451-465`, `V/CombatViewSystem.cs:480-520`, `V/Display/ICombatSceneFactory.cs:15-16`, `V/Display/ICombatBattleDisplay.cs:1-40`
- 내용: ICombatSceneFactory.CreateDisplay가 구체 CombatSceneDisplay를 돌려줍니다. CombatViewSystem.Attach는 계약에 없는 CreateUnitPreparation·Inspection·EntryPresentation을 구체형에서 꺼내 AttachCreated로 넘깁니다. AttachCreated(ICombatBattleDisplay, …)는 Attach 한 곳에서만 불리는데도 선택 인자(entryPresentation = null, preparation = null)와 root == null 갈래를 갖고 있습니다(문서에도 '호출자가 없음'이라고 적혀 있음). 계약으로 나눈 이점은 없고 두 경로를 함께 읽어야 합니다.
- 고칠 방향: Inspection·EntryPresentation·CreateUnitPreparation을 ICombatBattleDisplay(또는 생성 결과 묶음)로 올리고 팩토리가 계약형을 돌려주게 합니다. 그 뒤 AttachCreated를 Attach로 합치고, 쓰이지 않는 선택 인자와 null 갈래를 지웁니다.
- 검증: 구체 CombatSceneDisplay를 돌려주는 생성 계약(ICombatSceneFactory.cs:15-16)은 known C2-18의 증거와 겹친다. 이 문제에서 새로운 몫은 AttachCreated 쪽이다. CombatViewSystem.cs:480-481의 AttachCreated는 464줄 Attach 한 곳에서만 불리고(운영 코드 grep 기준) 늘 non-null entryPresentation·preparation·root를 받는다. 그런데도 선택 인자 `= null`과 510·513줄 root == null 갈래를 갖고 있고, 문서(478줄)도 '호출자가 없음'이라고 적는다. 따라서 주된 기준을 닿지 않는 경로인 C5로 옮기고, 국소 정리 수준이라 low로 본다.
- 처음 지적: C2 낮음 → 검증 뒤 C5 낮음

#### C5-N19

**조기 공격 출처가 같은 세대 확인을 한 호출 안에서 두 번 함** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatDeferredAttackSource.cs:29`, `V/Playback/Support/CombatDeferredAttackSource.cs:41-64`, `V/Playback/Support/CombatDeferredAttackSource.cs:75-76`
- 내용: 새 파일 `CombatDeferredAttackSource`의 `ReleaseAll`·`EndSource`는 먼저 `Matches(source, version)`로 `version == _version && version == _generation()`을 확인합니다. 그 직후 `CanRelease`가 `_version == _generation()`을 다시 확인합니다. 앞 검사가 이미 참이면 뒤 검사도 늘 참이라, 겹친 세대 확인입니다(`_opened`·`_cancelled` 확인만 새 정보). `Join`도 `_version != _generation()`을 확인한 바로 다음 `CanRelease`로 같은 비교를 다시 합니다.
- 고칠 방향: `CanRelease`를 `_opened && !_cancelled`로 줄이고, 세대 비교는 Matches·Join 입구 한 곳에만 둡니다. 외부 공개 속성으로 세대 확인이 필요하면 따로 이름을 붙입니다.
- 검증: CombatDeferredAttackSource.cs:76의 Matches가 이미 version == _version && version == _generation()을 확인한다. 그런데 ReleaseAll(53-56)·EndSource(58-64)는 그 직후 CanRelease(29)로 _version == _generation()을 다시 본다. Join(41-50)도 입구에서 _version != _generation()을 확인한 뒤 CanRelease로 같은 비교를 되풀이한다. 다만 CanRelease는 CombatAttackStart.cs:134에서 밖으로도 쓰이므로, 속성 안의 세대 비교 자체는 필요하고 클래스 내부 호출만 겹친다. 헛일만 있는 국소 정리라 낮음이다. known에 해당 항목은 없다.

### C6 C#·Unity 구조 (6)

#### C6-N01

**서비스 묶음 검사가 두 곳에 갈리고, 인자가 아닌 값에 지운 메서드의 인자 이름으로 ArgumentNullException** — 낮음, 검증 B

- 위치: `V/CombatViewSystem.cs:172-220`, `V/CombatViewBindings.cs:24-28`, `V/CombatViewBindings.cs:158-161`, `V/Input/Interaction/AttackTargetingInteraction.cs:92-110`
- 내용: CombatViewBindings.From은 넷만 InvalidOperationException으로 검사하고, 나머지 열셋은 Bind가 ArgumentNullException("state"), ("slot") 같은 문자열 이름으로 검사한다. 그 이름은 지운 BindServices의 옛 인자 이름이고, Bind의 인자는 bindings 하나다. 서비스가 비어 있는 것은 상태 오류라 예외 규약(인자는 ArgumentNullException, 상태는 InvalidOperationException)과 맞지 않는다. 같은 묶음의 필수 규칙이 두 파일에 갈려 있다.
- 고칠 방향: 필수 검사를 CombatViewBindings.From 한 곳으로 모아 서비스 이름을 담은 InvalidOperationException으로 던진다.
- 검증: CombatViewSystem.cs:186-218 Bind의 인자는 bindings 하나다. 그런데 비어 있는 묶음 필드에 ArgumentNullException("state")·("slot") 등 옛 인자 이름을 문자열로 던진다. 이는 상태 오류이므로 InvalidOperationException 규약과 맞지 않는다. 주석(169-170)이 옛 이름을 일부러 유지한다고 적는다. CombatViewBindings.cs:158-161은 넷만 InvalidOperationException으로 검사해 필수 규칙이 두 파일에 갈려 있다. AttackTargetingInteraction.cs:109의 hover null은 CombatHoverTracker.cs:21이 'feedback' 이름으로 알리고, CombatClipTimings.cs:39는 없는 매개변수 'eventTime'을 쓴다. 국소적인 관례 위반이다.
- 같이 합친 지적: 새 hover 인자의 null을 다른 인자 이름으로 알림 / CombatClipTimings 생성자가 없는 매개변수 이름 "eventTime"으로 ArgumentOutOfRangeException을 던짐

#### C6-N02

**MonoBehaviour 조준 리그에 ?. 사용** — 낮음, 검증 B

- 위치: `V/Display/Unit/UnitClipChannel.cs:111`, `V/Display/Unit/UnitClipChannel.cs:170-179`, `V/Display/Unit/UnitClipChannel.cs:275`, `V/Input/ActionButtons/CombatInputSurfaces.cs:60-77`
- 내용: UnitClipChannel은 CombatWeaponPitchRig(MonoBehaviour)를 _pitchRig?.Clear/SetTime/ResetAim으로 부른다. 리그가 파괴되면 Unity null을 C# null로 보지 않아, SetTime이 pitch.data에서 MissingReferenceException을 낸다.
- 검증: CombatInputSurfaces의 _root?.(CombatInputSurfaces.cs:62-64, 74-76)는 C6-01과 같은 곳이라 이 문제의 범위에서 뺀다. 새 곳은 둘이다. UnitClipChannel은 MonoBehaviour인 CombatWeaponPitchRig(GetComponent로 받음, UnitClipChannel.cs:62)에 ?.를 쓴다(111, 170, 179, 275줄). 같은 파일 91·207줄은 == null을 써서 판정 방식이 섞여 있다. CombatSceneDisplayFactory.cs:211의 unitHudLayer?.Release()도 바로 다음 블록의 == null과 다르다. 리그는 같은 모델에 붙어 함께 파괴되므로 실제 결함 경로는 드물다. 관례 위반으로 낮음이다.
- 같이 합친 지적: MonoBehaviour인 CombatSceneRoot에 ?. 를 씀 / 조립 실패 정리에서 UnitHUDLayer(Unity 컴포넌트)에 `?.`를 씀

#### C6-N03

**CombatEventProfiler가 필수 의존을 검사하지 않고 ?.Invoke를 되풀이함** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatEventProfiler.cs:19-24`, `V/Playback/Support/CombatEventProfiler.cs:48-61`
- 내용: 유일한 생성(presenter 147)은 `_facts.FindCharacter`를 넘기는데, 생성자에 ArgumentNullException이 없습니다. 그래서 Of·AttackOf에서 `_findCharacter?.Invoke`를 4번 씁니다(개발 빌드 전용).
- 고칠 방향: 생성자에서 null이면 예외를 던지고, 호출은 `_findCharacter(actor)?.Owner`로 씁니다.
- 검증: CombatEventProfiler.cs:21-24 생성자가 findCharacter null을 검사하지 않는다(프로젝트 규칙: 필수 인자는 ArgumentNullException). 그래서 Of·AttackOf(41,50,51,55)에서 _findCharacter?.Invoke가 네 번 되풀이된다. 개발 빌드 전용 프로파일러라 영향은 국소적이어서 낮음이다.

#### C6-N04

**BeginDeath가 사망 사건 null을 인자 예외 없이 역참조** — 낮음, 검증 B

- 위치: `V/Presentation/Reaction/CombatUnitReactionSet.cs:64-68`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:67-83`
- 내용: CombatUnitReactionSet.BeginDeath는 death.Final.EntityId를 바로 읽어 NullReferenceException이 납니다. CombatUnitReactionPresenter.BeginDeath도 death를 검사하지 않고, launchRagdoll일 때만 Resolve에서 늦게 실패합니다. 같은 파일의 BeginHit은 damage를 ArgumentNullException으로 검사합니다.
- 고칠 방향: 두 BeginDeath 입구에서 death를 ArgumentNullException으로 먼저 검사합니다.
- 검증: CombatUnitReactionSet.cs:64-68의 BeginDeath는 death.Final.EntityId를 바로 읽어 null이면 ArgumentNullException이 아니라 NRE가 난다. CombatUnitReactionPresenter.cs:67-83도 death를 검사하지 않고(계측만 death?.로 받음) launchRagdoll일 때 Resolve에서 늦게 실패한다. 같은 파일의 다른 인자는 ANE로 검사하므로 예외 규약이 국소적으로 어긋난다. 운영 호출자는 늘 사건을 넘기므로 낮음.

#### C6-N05

**깜빡임 시간 입력의 인스펙터 하한(Min 0)이 값 규칙(0보다 큼)보다 느슨함** — 낮음, 검증 B

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatDeathBlinkVisualInput.cs:19-21`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatDeathBlinkVisualData.cs:19-28`
- 내용: CombatDeathBlinkVisualInput.seconds는 `[Min(0f)]`라 인스펙터에서 0을 넣을 수 있다. 하지만 값 객체는 `RequirePositive`로 0을 거절한다. 툴팁도 0보다 커야 한다고 한다. 켜진 상태에서 0을 넣으면 편집 때는 막히지 않고, 부트(DB 생성) 때 예외로 멈춘다. 같은 묶음의 다른 입력(attackSpeed `[Min(0.01f)]`, pulses `[Min(1)]`)은 하한을 규칙에 맞춰 두었다.
- 고칠 방향: `[Min(.01f)]`처럼 양수 하한을 둔다.
- 검증: CombatDeathBlinkVisualInput.cs:19-21의 seconds는 `[Min(0f)]`라 인스펙터에서 0을 넣을 수 있다. 그런데 CombatDeathBlinkVisualData.cs:27의 RequirePositive가 0을 거절하고, 툴팁도 '0보다 커야'라고 적는다. 그래서 0을 넣으면 편집 때는 막히지 않고 DB 생성 때 예외가 난다. 같은 파일 17줄 pulses는 `[Min(1)]`로 규칙과 맞춰 두었다. 국소적인 SerializeField 검증 불일치라 low다.

#### C6-N06

**랙돌이 사망 순간마다 본을 이름 문자열로 찾고, 미리 검사하지 않음** — 낮음, 검증 ER

- 위치: `V/Display/Unit/CombatUnitRagdoll.cs:20-63`, `V/Display/Unit/CombatUnitRagdoll.cs:72-103`, `V/Display/Unit/CombatUnitRagdoll.cs:272-296`, `V/Display/Unit/FieldUnitView.cs:732-752`
- 내용: 새 파일 CombatUnitRagdoll은 사망 연출을 시작할 때마다(Begin) GetComponentsInChildren<Transform>으로 모든 본을 훑습니다. 그리고 하드코딩한 이름표로 본을 찾습니다. Rigify는 "DEF-"+역할, Legacy는 "뼈대.011.L" 같은 표, 부착물은 "DEF-pelvis"·"shoulder"·"DEF-rigid_" 조합입니다. 본이 없으면 그 자리에서 InvalidOperationException을 던집니다. 이 이름 규칙을 프리팹 준비·DB 생성 때 확인하는 곳이 없습니다. 지침 §14(문자열 조회 최소화)를 어긴 것입니다. 같은 저장소의 클립 이벤트 이름은 DB 생성 때 표(CombatClipEventIds)와 ReadTimings로 미리 거르는데, 랙돌만 운영 사건 경로에서 처음 이름을 확인합니다. 덤으로 질량·반지름·관절 각도도 코드 상수입니다.
- 실패 시나리오: 적 프리팹(EnemyMiner 등 boneLayout을 쓰는 다섯 개)의 리그를 다시 내보내 본 하나의 이름이 바뀐다(예: DEF-forearm.L → DEF-forearm_L). 또는 새 유닛에 CombatUnitRagdoll을 붙이면서 boneLayout을 잘못 고른다. 전투 설치·DB 생성은 통과한다. 그 유닛이 처음 죽는 순간 GetBone이 '본을 찾을 수 없습니다' 예외를 던지고, 그 예외는 사망 반응 → 재생 경로로 퍼져 전투가 F1으로 멈춘다.
- 고칠 방향: Begin 때마다 이름으로 찾지 말고, 본 참조를 직렬화한 표(역할 → Transform)로 둔다. 아니면 유닛 준비(PrepareSelectedUnit·풀 생성) 때 한 번 찾아 검사하고 결과를 보관한다. 물리 상수는 연출 설정 표로 옮기는 것을 검토한다.
- 검증: 코드는 주장대로다. CombatUnitRagdoll.Begin(CombatUnitRagdoll.cs:72-90)은 사망할 때마다 FindNamedBones(92-103)로 GetComponentsInChildren<Transform>을 훑는다. GetBone(272-278)은 "DEF-"+역할 또는 LegacyBoneNames(44-64)로 본을 찾고, 없으면 InvalidOperationException을 던진다. FieldUnitView.cs:732-753의 TryBeginDeathRagdoll은 그 예외를 다시 던지고, CombatUnitReactionPresenter.cs:81-83이 사망 반응 시작 때 이것을 부른다. 프리팹 13개(boneLayout 0이 11개, 1이 2개)가 이 컴포넌트를 쓰지만, 준비·DB 생성 때 본 이름을 검사하는 곳은 없다(Ragdoll을 참조하는 다른 코드는 Dev/CombatRagdollPreview뿐이다). 다만 지침 §14는 System/Service를 문자열로 찾지 말라는 규칙이라 본 이름 조회와는 맞지 않는다. 그래서 C3가 아니라 Unity 구조 관례(SerializeField 참조와 검증 방식, 설정 오류를 늦게 드러냄)인 C6으로 본다. 실패 시나리오는 지금 데이터에서 생기지 않는다. 리그를 다시 내보내거나 boneLayout을 잘못 고르는 것처럼 앞으로 에셋을 바꿀 때만 생긴다.…
- 처음 지적: C3 중간 → 검증 뒤 C6 낮음

### C8 캡슐화 (2)

#### C8-N01

**CombatFieldDisplay가 정리·해제 묶음을 가변 Action[]로 그대로 내놓음** — 낮음, 검증 B

- 위치: `V/Display/Map/CombatFieldDisplay.cs:67-97`, `V/Display/CombatSceneDisplay.cs:186`
- 내용: BoundClears·BoundReleases·ObjectClears·ObjectReleases·GimmickClears·GimmickReleases가 생성 때 만든 Action[]를 그대로 돌려준다. 받는 쪽(CombatSceneDisplay)이 원소를 바꾸거나 지우면 필드 표시의 정리 차례가 소리 없이 바뀐다. 문서는 '정리 목록의 기믹 이름은 여기 한 곳'이라 하지만 배열이 가변이라 그 보장이 표면에서 지켜지지 않는다.
- 고칠 방향: IReadOnlyList<Action>로 노출한다.
- 검증: CombatFieldDisplay.cs:67-75에서 Action[]를 만들고 81-97에서 internal Action[] 속성으로 그대로 내놓는다. 받는 쪽은 CombatSceneDisplay.cs:117·186·262-263·284-291에서 읽기만 하지만, 타입상 원소를 바꿀 수 있어 doc이 말하는 '기믹 이름은 여기 한 곳'이라는 보장이 표면에서 지켜지지 않는다. 같은 어셈블리 안 소비자 하나뿐이라 영향은 국소적이다. 그래서 낮음이다. C1-02(정리 목록 항목이 서로 다름)와는 뿌리가 다르다.

#### C8-N02

**조각 실행 재생기의 Profiler를 만든 뒤 바깥에서 set으로 꽂음** — 낮음, 검증 B

- 위치: `V/Playback/Players/CombatPieceRunPlayer.cs:45-48`, `V/Playback/CombatSceneEventPresenter.cs:163-166`, `V/Presentation/Pieces/CombatComposition.cs:168-173`, `V/Presentation/Pieces/CombatComposition.cs:322`
- 내용: CombatPieceRunPlayer.Profiler가 internal get/set이고(46), 사건 Presenter가 생성 뒤에 대입한다(164). 개발 빌드에만 있지만 조립 뒤에 바꿀 수 있는 공개 표면이고, 생성자 주입으로 충분하다.
- 검증: CombatPieceRunPlayer.cs:46의 Profiler는 internal get/set이고, Presenter가 생성 뒤에 대입한다(CombatSceneEventPresenter.cs:163-166, _attacks.Profiler도 같다). CombatComposition.cs:170-172의 ProfilingEventType·ProfilingEvent도 열린 setter다. 그래서 다른 변경을 막는 RequireOpen(322)을 건너뛰어 MarkUsed 뒤에도 바뀐다. 개발 빌드에만 있는 계측 표면이라 낮음이다. known C8-03(운영 의존의 null 허용)과는 다른 표면이다.
- 같이 합친 지적: 계측용 ProfilingEvent·ProfilingEventType·Profile이 조립 잠금을 건너뛰는 열린 setter다

### C9 수명·자원 (1)

#### C9-N01

**밀림 젖힘의 오르기 상태가 펴기 없이 끝난 경로에서 비워지지 않는다** — 낮음, 검증 B

- 위치: `V/Presentation/Movement/CombatPushLean.cs:49-50`, `V/Presentation/Movement/CombatPushLean.cs:70-84`, `V/Playback/Support/CombatMovementPresentation.cs:102-110`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:335`
- 내용: CombatPushLean의 _rising·_settings·_upright는 BeginRestore(방향 복귀·펴기)와 레인 Cancel의 Stop()에서만 비워진다. 그런데 이동 표시의 EndMovement는 경로 끝 유닛이 죽었거나 필드 밖이거나 표시 회전이 없으면 펴기를 부르지 않고 바로 completed()로 끝난다. 레인은 결과가 끝나도 남으므로(Cancel은 재생 취소 때만), 오르기가 끝나기 전에 경로가 끝나면(짧은 Flung 걸음에서 가능) 그 유닛 레인의 다음 걸음 Tick에서 _pushLean.Tick이 옛 똑바른 자세·축으로 SetFacing을 쓴다. 다음 밀림 걸음은 '_rising && 같은 유닛'이라 새 방향을 잡지 않고 옛 축을 이어 간다.
- 고칠 방향: 걸음 레인이 경로 끝(FinishStep의 restore 아님 갈래)이나 새 자발 걸음 시작에서 _pushLean.Stop()을 부르거나, 이동 표시가 펴기를 건너뛸 때 레인에 정지를 알린다.
- 검증: CombatPushLean의 _rising은 BeginRestore(CombatUnitMotionPlayback.cs:275)와 Cancel(360)의 Stop에서만 비워지고, CombatMovementPresentation.cs:102-110은 유닛이 죽었거나 필드 밖이거나 회전이 없으면 펴기 없이 completed()로 끝난다. 레인은 유닛별 사전(CombatUnitMotionSet.cs:35)에 남으므로 그 유닛이 다시 걸으면 335줄 _pushLean.Tick이 옛 자세로 SetFacing을 쓰고, 밀린 걸음이면 49-50줄 때문에 옛 축을 잇는다. 다만 오르기 시간이 HitDuration*0.5(CombatHitLean.cs:22-26)로 짧고 남는 경로가 사망·필드 밖 유닛이라 실제로 보이는 경우는 좁아 낮음.

### C10 정확성·오류 처리 (7)

#### C10-N01

**외형 소유자를 다시 만들면 이동 투명도 층이 조용히 사라짐** — 낮음, 검증 B

- 위치: `V/Display/Unit/FieldUnitView.cs:396-416`, `V/Display/Unit/FieldUnitView.cs:616-628`, `V/Display/Unit/CombatUnitPresentationWindow.cs:232-255`
- 내용: ChangeStatusAppearance·SetCartoonMaterialFallback은 ReleaseMovementAppearance로 UnitModelAppearance를 버리고 새로 만든다. 하지만 이전 층 값(이동 투명도·디졸브)을 옮기지 않는다. 창구의 _movementOpacity 대여는 여전히 현재(IsCurrent)로 남는다. 플래시는 조각이 다음 Tick에 다시 쓰고 외곽선은 대여가 무효가 되어 다시 잡히지만, 이동 투명도는 되찾는 길이 없다.
- 실패 시나리오: 입력 미리보기로 원본을 흐리게 둔 동안 같은 유닛의 사실이 바뀌어 상태 모습이 바뀌면, 원본이 불투명으로 돌아가는데 창구는 투명도가 걸려 있다고 본다(드문 경로).
- 고칠 방향: 외형을 다시 만들 때 현재 층 값(투명도·디졸브)을 새 소유자에 다시 적용하거나, 투명도 대여를 무효로 표시한다.
- 검증: FieldUnitView.cs:405-416 SetCartoonMaterialFallback와 616-628 ChangeStatusAppearance는 ReleaseMovementAppearance(517-522)로 UnitModelAppearance를 버리고 PrepareMovementAppearance로 새로 만든다. 새 객체는 _opacity=1(UnitModelAppearance.cs:24)로 시작하고 이전 투명도·디졸브 값을 옮기지 않는다. CombatUnitPresentationWindow.cs:232-255의 이동 투명도 대여는 SetMovementOpacity를 한 번만 쓰고 다시 적용하지 않으므로, 이동 미리보기 중 외형이 다시 만들어지면 흐림이 조용히 풀린다. 둘이 겹치는 조건이 드물고 화면 표시만 틀어져 낮음이다.

#### C10-N02

**연발 숫자 나누기 표시를 조립 때 확정해서, 조기 공격이 버려지면 피해 숫자가 아예 나오지 않음** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatVolleyDamageNumbers.cs:267-293`, `V/Playback/Support/CombatAttackStart.cs:126-136`, `V/Playback/Support/CombatPreAttackPlayback.cs:97-111`, `V/Playback/Support/CombatPreAttackPlayback.cs:194-205`
- 내용: CombatVolleyDamageNumbers.ContactNumbers는 숫자 입구를 만드는 순간, 즉 CombatAttackStart.ComposeClip 안에서 그 출처의 피해 사건을 _split에 넣습니다. _split에 든 사건은 Core 피해 사건 때 ShowUnsplit이 전체 숫자를 건너뜁니다. 조기 공격(CombatPreAttackPlayback)의 실행이 접촉 전에 버려지면 TryJoin이 원래 공격 사건을 연출 없이 바로 완료합니다. 버려지는 길은 Discard입니다(빌린 표시가 사라진 unavailable 콜백, 공격자가 보인 사실에서 죽음). 이때 탄별 숫자도 전체 숫자도 나오지 않고 체력 바만 줄어듭니다.
- 실패 시나리오: 선택 특성 시작 모드인 연발 원거리 공격이 조기 실행으로 시작됩니다. 첫 접촉 전에 실행이 unavailable로 버려집니다. 그러면 원래 공격 사건은 바로 완료되고, 대상의 피해 사건에서는 숫자가 _split이라 생략됩니다. 결과적으로 피해 숫자가 한 번도 보이지 않습니다.
- 고칠 방향: _split 표시를 첫 접촉 숫자를 낼 때 하거나, 실행이 버려지거나 시작에 실패하면 그 출처의 _split을 되돌립니다.
- 검증: CombatVolleyDamageNumbers.cs:54-55는 ContactNumbers가 호출되는 조립 순간 출처 사건을 _split에 넣고, ShowUnsplit(71-75)은 IsSplit(85)이면 전체 숫자를 건너뛴다. CombatAttackStart.cs:126-136의 TryBeginDeferred는 ComposeClip을 시작 성공 전에 부르고, CombatPreAttackPlayback.cs:194-205 Discard 뒤 TryJoin(97-111)은 원래 공격을 바로 Complete하므로 그 대상의 피해 숫자가 한 번도 나오지 않는다. 다만 버려지는 경로(공격자가 보인 사실에서 죽음, 빌린 표시가 사라진 unavailable)가 드물다. 체력 바는 줄어들고 숫자 표시만 빠지는 국소 결함이라 낮음이 맞다. known C1-41/43/44와는 관계없다.
- 같이 합친 지적: 조기 공격 실행이 버려지면 연발 피해 숫자가 사라짐

#### C10-N03

**유지 이펙트가 숨은 유닛에서는 표면 위치가 없다며 아예 나지 않음** — 낮음, 검증 ER

- 위치: `V/Presentation/Effects/CombatPersistentEffects.cs:18-25`, `V/Presentation/Effects/CombatPersistentEffects.cs:74-83`, `V/Presentation/Effects/CombatEffectWindow.cs:96-101`, `V/Display/Unit/FieldUnitPresenter.cs:249-260`
- 내용: 바뀐 Sync는 bodyOf(사실 기준 몸 자리, 숨김과 관계없이 값이 있음) 다음에 SurfacePositionOf를 새로 묻고, null이면 건너뜁니다. SurfacePositionOf는 TrackSurfacePresence 읽기를 쓰는데, 이 읽기는 view.isActiveAndEnabled가 거짓이면(같은 표시가 숨었을 뿐인 FE11 상태) Position을 null로 돌려줍니다. 그래서 숨은 유닛은 _live에 들어가지 않고, 다음 사실 그리기(Sync)가 다시 돌 때까지 이펙트가 없습니다. 바꾸기 전에는 Play가 숨은 유닛에도 이펙트를 냈고, 그리기만 받침이 끄고 켰습니다(클래스 설명 23줄 '끌기 동안 숨김'과 bodyOf 문서도 그 동작을 말합니다). 등장 연출은 _entryEffectsDeferred로 피하지만, 그 밖에 숨은 채 Sync가 도는 경로는 막지 않습니다.
- 실패 시나리오: 결과 취소의 StopAll 등으로 유지 이펙트가 밖에서 끝났거나 상태가 새로 붙은 순간, 그 유닛 표시가 비활성(배치 끌기 원본 숨김 등)인 채 사실 그리기 Sync가 돕니다. surface가 null이라 이펙트를 내지 않고 _live에도 기록하지 않습니다. 유닛이 다시 보여도 다음 사실 그리기까지 상태 유지·특성 상시 이펙트가 빠져 HUD 아이콘과 맞지 않습니다.
- 고칠 방향: 숨은 경우(Bound && Position null)는 bodyOf 자리나 사실 표면 자리로 Play하고 받침의 숨김 처리에 맡깁니다. 또는 표시 상태 변화 때 Sync를 다시 부르게 합니다.
- 검증: 코드는 주장대로입니다. f0dbaa152에서 CombatPersistentEffects.cs:77-79에 SurfacePositionOf 검사가 새로 들어갔고, 그 값이 null이면 Play하지 않으며 _live에도 넣지 않습니다. CombatEffectWindow.cs:96-101(SurfacePositionOf)와 FieldUnitPresenter.cs:249-260(TrackSurfacePresence)은 표시가 Bound여도 isActiveAndEnabled가 거짓이면 null을 돌려줍니다. 그래서 '끌기 동안 숨김은 받침이 처리한다'는 클래스 설명(CombatPersistentEffects.cs:23)과 동작이 어긋납니다. 다만 운영에서 Bound인데 숨은 표시가 생기는 길은 SetHiddenEntity 하나뿐입니다(FieldUnitPresenter.cs:651-659, 호출은 PlacementDragInteraction.cs:337의 배치 끌기). 이 끌기는 Deployment 층에서만 시작되고(PlacementDragInteraction.cs:416), 시작할 때 CanPreview가 대기 중 명령과 PlayerInput 단계를 확인합니다(PlacementDragInteraction.cs:317, CombatInputPolicy.cs:106-113). 그래서 끌기 중에 Render나 SyncPersistent가 돌면서 새…
- 처음 지적: C10 중간 → 검증 뒤 C10 낮음

#### C10-N04

**실패 복구 catch의 throw;에 닿지 않고 원래 예외는 늘 AggregateException으로 감싸짐** — 낮음, 검증 B

- 위치: `V/Presentation/Cameras/CombatCameraSession.cs:43-49`, `S/RuntimeSystem/ErrorHandling/CleanupErrors.cs:31-36`, `S/Factory/Runtime/InGame/View/CombatCameraFactory.cs:35-41`, `V/Presentation/Cameras/CombatCameraView.cs:124-130`
- 내용: CombatCameraSession.Begin의 catch는 errors를 원래 예외 하나로 시작한 뒤 CleanupErrors.ThrowIfAny를 부릅니다. ThrowIfAny는 Count > 0이면 늘 AggregateException을 던지므로 다음 줄 throw;는 닿지 않습니다. 정리가 성공해도 원래 예외 형식(InvalidOperationException 등)은 감싸진 채 올라갑니다. 같은 모양이 CombatCameraFactory.Create와 CombatCameraView.Begin에도 있습니다.
- 고칠 방향: errors를 null로 시작해 정리 실패만 모으고, 정리 실패가 있을 때만 원래 예외와 함께 AggregateException을 던집니다. 아니면 throw;로 원래 예외를 그대로 다시 던집니다.
- 검증: CombatCameraSession.cs:45-48, CombatCameraFactory.cs:37-40, CombatCameraView.cs:128-131은 errors를 원래 예외 하나로 시작한 뒤 CleanupErrors.ThrowIfAny를 부른다. ThrowIfAny(CleanupErrors.cs:32-36)는 Count > 0이면 늘 AggregateException을 던지므로, 다음 줄 throw;는 닿지 않는다. 그 결과 정리가 성공해도 원래 예외 형식이 늘 감싸여 올라가 의도(원래 예외 재던지기)와 다르다. 오류 자체는 드러나므로 낮음이다. 고치면 예외 모양이 바로잡히므로 주된 기준은 C10으로 본다. known C3-03은 공용 CleanupErrors를 쓰지 않는 문제라 다르다.
- 처음 지적: C5 낮음 → 검증 뒤 C10 낮음

#### C10-N05

**BodyText Hide 뒤 루트가 다시 켜지지 않는데 TryShow는 시작했다고 답함** — 낮음, 검증 B

- 위치: `V/Presentation/Text/CombatBodyTextWindow.cs:72-100`, `V/Presentation/Text/CombatBodyTextWindow.cs:214-224`, `V/Display/CombatSceneDisplay.cs:295-301`, `V/Presentation/Pieces/CombatBodyTextPiece.cs:25-31`
- 내용: Hide()는 활성 글자를 거두고 _resources.Root를 끕니다. 창구는 해제되지 않은 채 계속 쓸 수 있고, 루트를 다시 켜는 곳이 없습니다. 그 뒤 TryShow는 대여·구독을 하고 true를 돌려주지만(문서: '글자 실행을 시작했는지'), 꺼진 루트 아래라 보이지 않습니다. CombatBodyTextPiece는 이 true를 보고 started를 부릅니다.
- 고칠 방향: Hide 뒤 상태를 기록해 TryShow가 false를 돌려주거나 예외를 던지게 합니다. 또는 Show 때 루트를 다시 켭니다.
- 검증: CombatBodyTextWindow.cs:214-225 Hide는 Cancel 뒤 _resources.Root를 끄고, 그 파일 어디에도 다시 켜는 곳이 없다. 그런데 TryShow(73-96)는 해제 검사만 하고 대여·구독 뒤 true를 돌려주며, CombatBodyTextPiece.cs:27-30은 이 값을 보고 started를 부른다. Hide는 CombatSceneDisplay.cs:300 ShowCompletion(전투 완료)에서만 불려서, 그 뒤 글자가 나올 경로는 거의 없다. 잠재 반쪽 상태라 낮음이 맞다.

#### C10-N06

**Nexus 착지 이펙트가 끝나지 않는 데이터 결함을 경고만 하고 넘어간다** — 낮음, 검증 B

- 위치: `V/Presentation/Entry/CombatEntryNexusAppearance.cs:18-21`, `V/Presentation/Entry/CombatEntryNexusAppearance.cs:119`, `V/Presentation/Entry/CombatEntryNexusAppearance.cs:153-160`
- 내용: FallingRocks 프리팹이 Looping이거나 끝 알림이 빠진 데이터 결함을, 착지 뒤 3초(코드 상수) 지나 Debug.LogWarning 한 번으로만 알리고 진입을 계속한다. 프로젝트의 오류 정책(F1)이나 부트 검사로 드러나지 않아, 경고를 놓치면 남은 이펙트가 적 등장 내내 살아 있다가 진입 Cancel에서야 거둬진다. 기다림 상한 3초도 다른 낙하 값과 달리 설정(CombatNexusDropSettings)이 아닌 코드에 있다.
- 고칠 방향: 프리팹 Looping·끝 알림 유무를 이펙트 표 부트 검사로 막고, 상한은 낙하 설정 값으로 옮긴다.
- 검증: CombatEntryNexusAppearance.cs:18-21·119·153-160은 FallingRocks 프리팹이 Looping이거나 끝 알림이 빠진 데이터 결함을 착지 뒤 3초(코드 상수) 후 Debug.LogWarning 한 번으로만 알리고 진입을 계속한다. 무한 대기를 막으려는 완화라 진행은 멈추지 않고 남은 이펙트는 Cancel에서 거둬지므로 상태가 크게 틀어지지 않지만, 데이터 결함이 오류 정책이나 부트 검사로 드러나지 않는다. C4-28(이펙트 종류 상수)·C3-05(대여 재구현)와는 다른 지점이다.

#### C10-N07

**넉백 앞 피격 판정이 행동 구간을 보지 않아 앞 행동의 피격이 '밀림과 함께 가는 피격'으로 바뀔 수 있음** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatKnockbackLeadIns.cs:150-172`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:296-297`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:354-371`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementProjection.cs:195-197`
- 내용: CombatKnockbackLeadIns.Begin은 결과 전체에서 유닛마다 마지막 캐릭터 사건(_last)만 기억합니다. 그래서 '스스로 하지 않는 Moved 바로 앞의 같은 유닛 캐릭터 사건이 체력 감소 피격'이면 둘 사이에 행동 시작(IsActionStart)이나 다른 행동이 끼어 있어도 그 피격을 밀림 앞 피격으로 넣습니다. Core에는 피해 사건 없이 강제 Moved를 내는 길이 있습니다. 위치 교환 승객(MovementProjection 197: Forced·PositionSwap)과, 피해가 0이라 체력 사건이 없는 공격 강제 이동이 그 예입니다. 이 경우 그 유닛이 앞 행동에서 받은 피격이 TryBeginHit에서 BeginPushHit로 갑니다. 그러면 자세를 빌리는 피격 반응(젖혔다 폄) 없이 대기 조각과 큐만 나오고, 사건은 바로 완료됩니다.
- 실패 시나리오: 한 결과 안에서 유닛 Q가 행동 1에서 체력 감소 피격을 받습니다. 그 뒤 Q에게 다른 캐릭터 사건 없이, 다른 행동의 위치 교환 승객 이동(Forced Moved)이 옵니다. 그러면 행동 1의 피격이 밀림 앞 피격으로 분류되어 피격 반응 동작이 빠지고 즉시 완료됩니다. 젖힘은 한참 뒤의 승객 이동 때 나옵니다.
- 고칠 방향: _last를 행동 구간(CombatEventTypeTable.IsActionStart)마다 비우거나, 피격과 밀림 걸음이 같은 피해 출처(CombatHitFollowUps의 이동 후속)일 때만 목록에 넣습니다.
- 검증: 제시된 줄 150-172는 틀렸다(파일은 63줄). 실제 논리는 CombatKnockbackLeadIns.cs:37-50에 있다. 결과 전체에서 유닛마다 마지막 캐릭터 사건(_last)만 기억하고, 스스로 하지 않는 Moved 바로 앞 사건이 IsHit면 행동 경계와 상관없이 밀림 앞 피격으로 넣는다. 위치 교환 승객은 MovementProjection.cs:197에서 Forced Moved로 나오고, CombatMovementKinds.Of는 Forced를 자발 이동이 아닌 것으로 분류한다. 그래서 한 결과 안에서 앞 행동의 피격 뒤에 다른 캐릭터 사건 없이 승객 이동이 오면, 그 피격이 CombatCharacterEventHandler.cs:296-297을 거쳐 BeginPushHit(354-371)로 가 자세 반응 없이 바로 완료된다. 클래스 remarks(16-18)의 'Core가 피해 뒤 밀기 시작한다'는 가정도 이 경로에서는 깨진다. 결과는 사실이 틀어지지 않는 드문 조건부 연출 차이이고 기능도 '임시'로 표시돼 있어 낮음으로 둔다. known C2-13·C5-15·C1-60과는 뿌리가 다르다.

### C11 핫패스 GC (1)

#### C11-N01

**조기 공격 Flush가 예약마다 매 호출 클로저 객체를 할당함** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatPreAttackPlayback.cs:121-135`, `V/Playback/CombatSceneEventPresenter.cs:396-403`
- 내용: Flush의 for 본문에서 지역 변수 `reservation`을 `_ => Discard(reservation)` 람다가 캡처합니다. 그래서 컴파일러가 반복마다 클로저 표시 클래스를 만듭니다. Flush는 매 Tick과 매 사건 표시 뒤에 불리므로, 예약이 살아 있는 동안(지연~클립 끝) 예약 하나당 프레임마다 할당이 생깁니다.
- 고칠 방향: unavailable 콜백을 Reservation 생성 때 한 번 만들어 필드에 둡니다.
- 검증: CombatPreAttackPlayback.cs:121-135에서 for 본문의 지역 변수 reservation을 '_ => Discard(reservation)' 람다(133)가 캡처한다. 그래서 C# 컴파일러는 그 범위에 들어갈 때마다, 즉 반복마다 클로저 객체를 할당한다. Flush는 CombatSceneEventPresenter.cs:396-403 TickPresentation에서 매 프레임 불린다. 그러나 할당은 살아 있는 예약 수(보통 0~수 개)만큼이고 예약이 있는 짧은 구간에만 생기므로, 의미 있는 낭비는 아니어서 낮음이다.

### C12 주석·문서 (10)

#### C12-N01

**'AbilityHoverExit' 마커가 실제 호버 나감이 아니라 초기화 경로를 잼** — 낮음, 검증 B

- 위치: `V/Display/Hud/CombatUnitInfoView.cs:25-27`, `V/Display/Hud/CombatUnitInfoView.cs:465-474`, `V/Display/Hud/CombatUnitInfoView.cs:183`, `V/Display/Hud/CombatUnitInfoView.cs:361-362`
- 내용: 마커 Combat.Action.UI.UnitInfo.AbilityHoverExit는 ResetAbilityHover에 붙어 있다. 그런데 ResetAbilityHover를 부르는 곳은 BindHoverText, 숨김·비우기 경로, 숨은 칸 RefreshAbilityHover이다. 실제 특성 칸 호버 나감(AbilityInputReceiver.HoverExit)에는 마커가 없다. 그래서 이름과 잰 구간이 다르다. StatusHoverExit도 같은 식으로 Reset에 붙어 갱신 경로까지 함께 센다.
- 고칠 방향: 마커를 AbilityInputReceiver.HoverExit로 옮기거나 이름을 Reset으로 바꾼다.
- 검증: 마커 'AbilityHoverExit'(CombatUnitInfoView.cs:25)는 ResetAbilityHover(465-474)에 붙어 있다. 그 함수는 BindHoverText(183), 숨김·비우기 ResetHover(361-362), 428에서 불린다. 실제 특성 호버 나감 AbilityInputReceiver.HoverExit(584-589)에는 마커가 없다. 이름과 잰 구간이 달라 계측을 잘못 읽게 한다. StatusHoverExit는 실제 HoverExit(602)도 Reset을 거쳐 반쯤만 맞다.

#### C12-N02

**대여 칸·상태 모습 주석이 지금 사용과 다름** — 낮음, 검증 B

- 위치: `V/Display/Unit/UnitPresentationLeaseSlot.cs:6`, `V/Display/Unit/UnitStatusAppearance.cs:20-22`, `V/Display/Unit/CombatUnitPreparationLease.cs:48-55`
- 내용: UnitPresentationLeaseSlot 문서의 채널 목록(자세·서는 자리·크기·플래시·클립)에 새로 칸을 쓰는 아웃라인(_outlineSlot)이 빠졌다. UnitStatusAppearance는 '렌더러 켜짐은 이 부품만 바꿉니다'라고 적지만, CombatUnitPreparationLease(빌린 표시)도 Renderer.enabled를 끄고 되돌린다.
- 검증: 대여 칸 채널 목록에서 아웃라인(_outlineSlot, FieldUnitView.cs:65)이 빠진 UnitPresentationLeaseSlot.cs:6은 C12-04의 recheck가 이미 남은 항목으로 적었다. 그래서 새 범위는 UnitStatusAppearance.cs:20-21뿐이다. 이 주석은 '렌더러 켜짐은 이 부품만 바꿉니다'라고 적지만, CombatUnitPreparationLease.cs:49-55·120도 빌린 표시의 Renderer.enabled를 끄고 되돌린다. 렌더러 켜짐의 주인을 잘못 알게 하는 설명이지만 국소적이라 낮음이다.

#### C12-N03

**팔레트 문서는 본부 증원 범위 색을 '테두리 색'이라 하나 층 표는 채우는 층** — 낮음, 검증 B

- 위치: `V/Display/Field/FieldHighlightPalette.cs:96`, `V/Display/Field/FieldHighlightLayers.cs:136`, `V/Display/Field/FieldView.cs:84-86`
- 내용: FieldHighlightPalette.NexusReinforcementRange의 XML doc은 '테두리 색'이다. 하지만 층 표의 NexusReinforcementRange 줄은 채움=true, 평소에도 그림=true라 IdleFillLayers에 들어가 평소에도 칸을 채운다. FieldView 툴팁도 '테두리와 윗면 채우기를 함께 보입니다'라고 적는다. 바로 옆 RockfallWarning·RoarForecast 문서는 '(테두리·채우기)'로 적혀 있다.
- 고칠 방향: 문서를 '테두리·채우기 색'으로 고친다.
- 검증: FieldHighlightPalette.cs:96은 NexusReinforcementRange를 '테두리 색'이라 적는다. 하지만 층 표 FieldHighlightLayers.cs:136은 이 층을 채움 true, 평소에도 그림 true로 두고, FieldView.cs:86 툴팁도 '테두리와 윗면 채우기를 함께 보입니다'라고 적는다. 바로 옆 RockfallWarning·RoarForecast 문서는 '(테두리·채우기)'로 적혀 있어 이 줄만 틀렸다. 설명 하나가 틀린 국소 문제라 낮음이고, known C1-24·C10-02·C4-11과는 관계가 없다.

#### C12-N04

**포효 예고 설명의 '밤 전투 아님이면 지웁니다'가 코드에 없음** — 낮음, 검증 B

- 위치: `V/Input/Interaction/CombatRoarForecastTracker.cs:25-26`, `V/Input/Interaction/CombatRoarForecastTracker.cs:154-156`
- 내용: 클래스 remarks에는 결과 재생 중·입력 닫힘·종료·밤 전투 아님이면 지운다고 적혀 있다. 하지만 CanKeep은 전투 유무·조회 판·Combat 단계·재생·입력 닫힘·종료만 보고, 밤 전투 사실은 어디서도 읽지 않는다.
- 고칠 방향: 설명에서 '밤 전투 아님'을 지우거나 실제 조건(Combat 단계 아님)으로 고친다.
- 검증: CombatRoarForecastTracker.cs:25에 '밤 전투 아님이면 지웁니다'라고 적혀 있다. 실제 조건은 CanKeep(154-156)의 StagePhase == Combat 검사이고, Core도 같은 뜻을 InCombat·StagePhase.Combat로 본다(BossRoarCalculator.cs:132). 그러니 '조건이 코드에 없다'는 지적은 정확하지 않다. 하지만 '밤'은 밤 삭제 설계(CombatCommandState.cs:19, CombatEvent.cs:18)로 지운 개념이다. 지금 코드에 없는 판정을 하는 것처럼 읽히는 낡은 이름이라 낮음으로 남긴다.

#### C12-N05

**형식 표 설명이 지운 '턴 사건 종류별 전환' 열을 아직 적음** — 낮음, 검증 B

- 위치: `V/Playback/Ordering/CombatEventTypeTable.cs:84`
- 내용: 이번 변경에서 transition 열과 TransitionOf, CombatPhaseTransitionKind를 지웠다. 그런데 표 remarks(84)는 아직 '턴 사건 종류별 전환'을 줄 함수가 답한다고 적는다.
- 검증: CombatEventTypeTable.cs:84 remarks는 '턴 사건 종류별 전환'을 줄 함수가 답한다고 적는다. 그런데 TransitionOf·CombatPhaseTransitionKind는 저장소 어디에도 없다(grep 결과 없음). 지운 열을 아직 설명하는 낡은 주석이라 낮은 C12다. known C2-23(TypeRow 위치 인자)과 C1-38은 다른 문제다.

#### C12-N06

**자폭 몸 유지 주석이 '깜빡임 없으면 지금 사망 반응'이라는 옛 규칙을 적음** — 낮음, 검증 B

- 위치: `V/Playback/Support/CombatSelfDestructDeaths.cs:19-33`, `V/Playback/Support/CombatSelfDestructDeaths.cs:65-93`, `V/Playback/Support/CombatActivationRows.cs:79-80`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:236-249`
- 내용: 사망 처리기는 깜빡임 값이 없어도 터질 유닛의 몸을 Keep합니다(늘 몸 남김). 그런데 CombatSelfDestructDeaths의 필드 주석(30), Keep doc(72 '깜빡임 사망의'), Detonate doc(85 '유지가 없으면(지금 사망 반응을 쓴 유닛)'), 클래스 설명(19-21), CombatActivationRow.DeathBlink doc('터질 유닛은 지금 사망 반응을 씁니다')은 깜빡임이 없으면 사망 반응을 쓴다고 적고 있어, IsExploding doc(65)과도 맞지 않습니다.
- 고칠 방향: '깜빡임이 없으면 깜빡임 없이 몸만 남김'으로 주석을 맞춥니다.
- 검증: CombatSelfDestructDeaths.cs:30 필드 주석 '(그 유닛은 지금 사망 반응)', 72 Keep doc '깜빡임 사망의 몸 유지', 85 Detonate doc '유지가 없으면(지금 사망 반응을 쓴 유닛)', CombatActivationRows.cs:79 '터질 유닛은 지금 사망 반응을 씁니다'가 옛 규칙을 적는다. 실제로 CombatCharacterEventHandler.cs:244-248은 깜빡임이 없어도 Keep하고, IsExploding doc(65)도 '늘 몸 남김'이라 서로 어긋난다. 클래스 설명(19-21)은 오히려 맞으므로 증거 일부는 과하지만 주석 불일치는 실재한다. 낮음.

#### C12-N07

**이름·공급원이 바뀐 뒤 남은 옛 설명** — 낮음, 검증 B

- 위치: `V/Presentation/Effects/CombatEffectFollower.cs:76-85`, `V/Presentation/Cameras/CombatCameraView.cs:30`, `V/Presentation/Cameras/CombatCameraView.cs:178`, `V/Presentation/Cameras/CombatCameraWindow.cs:61`
- 내용: CombatEffectFollower.Begin의 XML <param name="body">가 실제 인자 trackPosition과 다릅니다. 카메라 쪽은 공용 씬 바인딩으로 바뀌었는데 설명에 옛 공급원이 남아 있습니다. CombatCameraView 30줄 '기존 모드는 프리팹 렌즈값', CombatCameraWindow 61줄 '앞 맵은 먼저 비웁니다', 76줄 '배경이 남는 표시 해제', 133줄과 CombatCameraView 178줄 '맵을 비우기 전에'입니다. CombatPersistentEffects의 bodyOf 문서는 '찾지 못하면 내지 않는다'고만 하는데, 실제로는 표면 위치가 없어도(숨김) 내지 않습니다.
- 고칠 방향: param 이름을 trackPosition으로 고치고, 맵·배경·프리팹 표현을 씬 바인딩·연결 해제 기준으로 바꿉니다.
- 검증: CombatEffectFollower.cs:79-85의 <param name="body">가 실제 인자 trackPosition(85줄)과 달라 문서가 인자에 붙지 않는다(summary는 trackPosition을 씀). CombatPersistentEffects.cs:51의 bodyOf 설명과 카메라 쪽 '맵을 비우기 전에'(CombatCameraView.cs:178, CombatCameraWindow.cs:61·133) 같은 옛 표현도 지금 목록·연결 해제 구조와 어긋난다. 읽기 비용만 있는 국소적 문서 정합성 문제다.

#### C12-N08

**ActorBody·SubjectBody 이펙트를 표면에 두는데 자리 문서는 아직 '몸 상자 중심'이라고 한다** — 낮음, 검증 B

- 위치: `V/Presentation/Pieces/CombatCuePoints.cs:215-216`, `V/Presentation/Pieces/CombatCuePoints.cs:233-239`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatCuePoint.cs:16-23`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatUnitReactionPresentationSO.cs:14`
- 내용: CreatePiece가 ActorBody·SubjectBody 자리를 AtSurface로 보내 effects.SurfaceOf(point.position), 곧 몸 XZ 아래의 필드 표면에 이펙트를 놓는다. 그런데 DB 입력 계층의 CombatCuePoint 문서는 ActorBody를 '행위자 몸 상자 중심', SubjectBody를 '주체 몸 상자 중심'이라고 하고, 반응 SO 툴팁도 'SubjectBody = 맞은 유닛 몸 중심'이라고 한다. ForActor 문서도 actorBody를 몸 상자 중심이라고만 적어서, 실제로 놓이는 자리(표면)를 설명하지 않는다. 또 At에 붙어 있던 주석('정해진 자리의 조각입니다…')이 이제 AtSurface 위에 놓였다.
- 고칠 방향: CombatCuePoint·SO 툴팁·ForActor 문서를 '몸 XZ 아래 표면, 방향은 몸 방향'으로 고치고, At 주석을 At 위로 되돌린다.
- 검증: CombatCuePoints.cs:215-216이 ActorBody·SubjectBody를 AtSurface로 보내 239줄 effects.SurfaceOf(point.position)으로 표면에 놓는데, DB 입력 계층 CombatCuePoint.cs:16-23은 '몸 상자 중심'이라 하고 CombatUnitReactionPresentationSO.cs:13 툴팁도 '맞은 유닛 몸 중심'이라 한다. 또 236줄 '정해진 자리의 조각' 주석이 At이 아니라 AtSurface 위에 놓였다. 연출 값을 넣는 사람이 오해할 수 있으나 결과는 화면에서 바로 보이므로 낮음.

#### C12-N09

**CombatPieceRun의 Version 문서가 #if 계측 속성 쪽에 붙었다** — 낮음, 검증 B

- 위치: `V/Presentation/Pieces/CombatPieceRun.cs:121-126`
- 내용: '실행을 시작한 재생 세대입니다' summary 바로 아래에 #if UNITY_EDITOR || DEVELOPMENT_BUILD Profile 속성을 끼워 넣었다. 그래서 에디터·개발 빌드에서는 이 문서가 Profile에 붙고, Version은 문서를 잃는다.
- 고칠 방향: #if 블록을 summary 위로 옮기거나 Profile에 따로 문서를 단다.
- 검증: CombatPieceRun.cs:121-126에서 '실행을 시작한 재생 세대입니다' summary 다음에 #if UNITY_EDITOR || DEVELOPMENT_BUILD Profile 속성이 끼어, 에디터·개발 빌드에서는 그 문서가 Profile에 붙고 Version은 문서를 잃는다. 정리 수준의 국소 문서 문제다.

#### C12-N10

**설정 표 요약은 '하위 설정 묶음은 수 규칙을 바로 부른다'고 하지만 보드 등장 설정은 아직 설정 표의 조건 함수를 씀** — 낮음, 검증 B

- 위치: `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:8-12`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatBoardRevealSettings.cs:33-42`
- 내용: CombatPresentationSettingsData의 새 요약은 하위 설정 묶음(보드 등장·Nexus 낙하 등)이 수 규칙(CombatPresentationValueRules)을 바로 부른다고 적는다. 그러나 CombatBoardRevealSettings.Validate는 sweepSeconds·tileSeconds에 여전히 `CombatPresentationSettingsData.ValidateSeconds`를 부른다. depth·overshoot만 바뀌었다.
- 고칠 방향: 요약을 실제에 맞추거나, 보드 등장 시간도 같은 방식으로 바꾼다.
- 검증: CombatPresentationSettingsDataSO.cs:10-11 요약은 하위 설정 묶음(보드 등장 등)이 수 규칙을 바로 부른다고 적는다. 그러나 CombatBoardRevealSettings.cs:39-40은 sweepSeconds·tileSeconds에 여전히 CombatPresentationSettingsData.ValidateSeconds를 부른다. 설명과 코드가 어긋나는 주석 정합성 문제다. 동작에는 영향이 없어 low다. known C12-26(Coner 오타)과는 관계없다.

## 3. 지난 문제 301개 다시 확인

| 그때 | 풀림 | 줄어듦 | 그대로 | 커짐 |
|---|---|---|---|---|
| 중간 29 | 18 | 5 | 5 | 1 |
| 낮음 268 | 47 | 22 | 187 | 12 |
| 기반 한계 4 | 0 | 0 | 4 | 0 |

'줄어듦'은 문제가 남았지만 범위가 작아진 것, '커짐'은 같은 문제가 더 넓어진 것이다. 지금 심각도가 '—'이면 풀린 것이다.

### C1 SSOT (57)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c1-01"></a>C1-01 | 낮음 | 낮음 | 그대로 | 연결 정리 순서가 Detach·StopInput·OnInputClosed에 세 벌로 손으로 적힘 | 연결 정리 순서를 Detach·StopInput·OnInputClosed가 아직 각자 적는다. 그래서 한 곳에만 새 정리 단계를 넣는 실수 경로가 그대로다. |
| <a id="c1-02"></a>C1-02 | 낮음 | 낮음 | 줄어듦 | CombatSceneDisplay 정리 목록이 _fieldBound·Install 실패·Clear·Release로 갈려 이미 항목이 다름 | 기믹·오브젝트 정리 목록은 CombatFieldDisplay 한 곳의 묶음이 됐고, Install 실패와 Clear가 그 묶음을 같이 쓴다. 그래서 '새 기믹을 Clear에만 넣는' 실수는 줄었다. 그래도 Clear·Release·Install catch의 나머지 항목은 아직 손으로 서로 다르게 적혀 있다. |
| <a id="c1-03"></a>C1-03 | 낮음 | 낮음 | 커짐 | '필드 위 살아 있는 유닛' 판정식이 전투 View 약 15곳에 따로 있고 정의가 이미 갈림 | 공용 술어는 아직 없다. CharacterRuntimeArea.Field 판정은 20개 파일 33곳에서 23개 파일 36곳으로 늘었고, 새 포효 예고·행동 커서 쪽에도 같은 판정식 사본이 생겼다. |
| <a id="c1-04"></a>C1-04 | 낮음 | 낮음 | 그대로 | 진입 적 수집이 RenderFacts의 '그려지는 유닛' 조건을 따로 적고 어긋나면 예외 | 진입 수집은 여전히 RenderFacts의 그리기 조건을 따로 적는다. 두 조건이 어긋나면 예외가 난다. 같은 모양의 CollectEntryStructures도 하나 더 생겼다. |
| <a id="c1-05"></a>C1-05 | 중간 | — | 풀림 | 환경 원본 '보임'(Active && IsKnownToPlayer)·분류·장애물 칸 판정이 표시·입력 7곳에 따로 있음 | 보임(Active && 플레이어 공개), 분류 소속, Object 형식 예외, 살아 있고 칸을 점유한 장애물 판정이 모두 CombatEnvironmentFacts로 모였다. 표시 3곳과 입력 3곳은 그 함수만 부르며, 범위 안에서 `.Active`나 KnownTo를 직접 보는 곳은 Facts 밖에 남아 있지 않다(grep으로 확인). 그래서 공개 정책이 바뀔 때 한 곳만 놓쳐 어긋나는 시나리오는 더는 성립하지 않는다. |
| <a id="c1-06"></a>C1-06 | 낮음 | 낮음 | 그대로 | 튜토리얼 '실습 목표 대기' 판정을 View가 다시 조립 | View가 튜토리얼 실습 목표 대기 판정을 따로 조립한다. TutorialFacts.IsSessionBattle 원본을 쓰지 않는 것도 그대로다. |
| <a id="c1-07"></a>C1-07 | 낮음 | 낮음 | 그대로 | 거절 안내 문구 두 벌, 'InvalidMainCharacter' 사유만 원문 리터럴 | 파일이 바뀌지 않았다. 문구가 두 벌이고 InvalidMainCharacter만 리터럴로 Core 사유와 비교하는 것도 그대로다. |
| <a id="c1-08"></a>C1-08 | 낮음 | 낮음 | 그대로 | 배치 진입이 Core의 '메인 배치' 규칙을 다른 식별 방식으로 다시 판정 | View가 Core의 메인 배치 규칙을 다른 식별 방식으로 다시 판정하는 사전 검사가 Request·Respond 양쪽에 그대로 있다. |
| <a id="c1-09"></a>C1-09 | 낮음 | 낮음 | 그대로 | CombatInputPolicy 함수마다 바탕 조건을 따로 나열해 겹침·누락이 생김 | 클래스 doc은 '복제하지 않는다'고 하지만, HasBattle·IsBattleDisplayed·¬IsTerminal·¬IsEntryPresentationPending를 여러 함수(카메라·환경 호버 포함)가 여전히 각자 나열한다. |
| <a id="c1-11"></a>C1-11 | 중간 | — | 풀림 | 도약 구간 끊김 규칙이 CombatLeapPlan과 레인 BeforeEvent에 따로 있고 이미 다름 | 도약 끊김 규칙이 CombatLeapPlan.EndsLeap 정적 함수 하나로 모였고 계획 Build와 레인 BeforeEvent가 함께 부른다. 새 중단 사건을 한쪽에만 더해 계획·레인이 어긋나는 실패 시나리오가 더는 성립하지 않는다(이동 사건은 계획의 걸음 대응과 레인 BeginStep의 EndLeap이 같은 방향으로 닫음). |
| <a id="c1-12"></a>C1-12 | 낮음 | 낮음 | 그대로 | 진입 켜짐 조건이 PrepareDisplay와 Prepare에 두 벌 | 보류 시작과 진입 시작의 켜짐 조건이 여전히 두 벌이다. 한쪽만 고치면 _boardDeferred가 계속 참으로 남을 수 있다. |
| <a id="c1-13"></a>C1-13 | 낮음 | 낮음 | 그대로 | 보드·선로 등장의 Body·Tick·Cancel 골격과 낙하 곡선이 파일마다 사본 | Body·Tick·Cancel 골격(복원→비우기→ThrowIfAny)과 낙하 곡선이 여전히 파일마다 사본이다. |
| <a id="c1-14"></a>C1-14 | 중간 | — | 풀림 | 표시 캐릭터 사본 둘: 목표 강조는 결과 시작 스냅숏, 선택 강조는 보인 사실 | 목표 강조가 선택 강조와 같은 보인 사실(CombatShownFacts)에서 칸을 읽고, 사건마다 Render→inspection.Refresh→RefreshInspectionSelection 경로로 다시 그려진다. 그래서 재생 중 보스가 죽거나 밀려나면 사건 시점에 바로 따라가므로, 옛 칸에 남던 실패 시나리오가 더는 성립하지 않는다. 남은 스냅숏은 재생 중이 아닐 때만 쓰이는 행동 커서와 호버 원점용이다. |
| <a id="c1-15"></a>C1-15 | 낮음 | 낮음 | 그대로 | 선택 ID·국면 사본을 UnitHUDLayer·CombatUnitInfo가 각자 들고 밀어 넣기 4곳에 의존 | 밀어 넣는 곳이 CombatSceneDisplay에서 CombatSelectionDisplay로 옮겨졌을 뿐이다. 선택 ID 사본, 네 곳에서 밀어 넣기, 국면 사본 둘이 그대로 남아 있다. |
| <a id="c1-16"></a>C1-16 | 낮음 | 낮음 | 그대로 | HUD 보임을 FieldUnitView·UnitHUDLayer 두 곳이 쓰고 HUD 기억 무효화가 다섯 길에 흩어짐 | HUD 보임을 쓰는 곳이 여전히 둘이다. 준비 쪽은 옛 사본으로 되돌린 뒤 다시 맞추는 보정만 더해졌다. 무효화 길도 세 클래스에 흩어져 있다. |
| <a id="c1-17"></a>C1-17 | 낮음 | 낮음 | 그대로 | 공용 Hover 패널 사용처를 두 소유자가 그림자 상태와 숨김 번호 폴링으로 각자 추정 | 두 사용처가 여전히 _shown/_observedHideVersion과 _abilityHoverShown/_abilityHoverVersion으로 공용 패널 사용자를 따로 추정하고, UnitInfo LateUpdate가 환경 호버가 닫은 패널을 다시 연다. |
| <a id="c1-19"></a>C1-19 | 낮음 | 낮음 | 줄어듦 | 특성 확인(Preview) Factory가 운영 표시 조립·필드 포트·정리 목록을 따로 베껴 이미 어긋남 | 필드 포트는 이제 공용 CreateFieldPorts 하나로 만들어 terrain+tornado 조합이 어긋나던 문제는 없어졌다. 그래도 presenter 묶음·정리 목록은 여전히 두 벌이고, 미리보기는 BindStructureDamage를 부르지 않으며 rockfall 기본값도 다르다. |
| <a id="c1-21"></a>C1-21 | 중간 | — | 풀림 | 강조 층의 '원점 열기'·호버 범위 소속이 층 표 밖에 하드코딩됨 | 원점 열기 층과 호버 범위 층 소속이 이제 층 표 한 줄의 필수 칸에서 파생된다. 새 호버 범위 층을 열거와 표에 더하면 원점 열기도 함께 정해야 컴파일된다. 그래서 'Of()가 default를 돌려줘 조용히 닫힌 변으로 그려짐' 시나리오가 성립하지 않는다. CombatAttackFeedback.AttackLayerOf(240-249)의 tone→층 대응은 원점 소속과는 다른 사실이라 같은 사실의 사본이 아니다. |
| <a id="c1-22"></a>C1-22 | 중간 | 중간 | 그대로 | 정보창 HP 막대 규칙(분모·스냅·보호막 채움)이 UnitHUD 사본 | 분모 Math.Max(max, hp+shield), 스냅 조건, 보호막 채움·enabled 규칙이 지금도 두 파일에 글자 그대로 따로 있다. 둘이 함께 쓰는 것은 HealthFillAnimation뿐이라 어긋날 위험은 그때와 같다. |
| <a id="c1-23"></a>C1-23 | 낮음 | 낮음 | 그대로 | 렌더 부품마다 Mesh·Color 핸들 생성과 Color→Mesh→Destroy 해제 순서를 따로 구현 | FieldTrajectoryLine·FieldArrowhead는 그대로고 FieldCellFill도 주석 한 줄만 바뀌었다. 렌더 부품마다 생성·Register·null 검사·해제 순서를 따로 쓰는 구조가 그대로 남아 있다. |
| <a id="c1-24"></a>C1-24 | 낮음 | 낮음 | 그대로 | FieldView.OnDestroy가 Release의 부품 해제 사슬을 따로 복사 | Release와 OnDestroy가 지금도 '궤적 → 넉백 표지 → 표시기' 해제 사슬을 각자 try/finally로 적는다. |
| <a id="c1-25"></a>C1-25 | 낮음 | 낮음 | 그대로 | 렌더 레이어 번호 20을 프로젝트 레이어 설정과 별개로 코드에 적음 | 세 파일 모두 바뀌지 않았다. 레이어 번호 20은 지금도 TagManager와 따로 손으로 적힌 상수이고, 낙석·가시 표시가 이 상수를 쓴다. |
| <a id="c1-26"></a>C1-26 | 낮음 | 낮음 | 줄어듦 | 스테이지 외형 회전 표 찾기·검증 규칙이 Object·Terrain 표시에 두 벌 | 이중 루프 검증은 CombatEnvironmentFacts.IsObject/IsTerrain 한 곳으로 모였다. 다만 'stageKey > 0 && TryGetStageEnvironmentVisual' 찾기와 같은 모양의 검증 메서드는 아직 두 Presenter에 따로 있다. |
| <a id="c1-27"></a>C1-27 | 낮음 | 낮음 | 그대로 | '재생 중이면 Destroy, 아니면 DestroyImmediate' 파괴 정책이 파일마다 복사됨 | 'isPlaying이면 Destroy, 아니면 DestroyImmediate' 분기가 Map 파일 8개에 그대로 있다. 옛 판과 사본 수가 같다. |
| <a id="c1-28"></a>C1-28 | 낮음 | 낮음 | 커짐 | ObjectViewSet·TerrainViewSet이 대여 맞추기·실패 되돌리기 정책을 두 벌로 가짐 | _pass 고르기, 예외 시 Clear 후 AggregateException, TryGet이 여전히 두 벌이다. 게다가 ObjectViewSet에 TerrainViewSet과 같은 VisitViews가 새로 생겨 겹치는 멤버가 하나 늘었다. 국소적이라 낮음은 그대로다. |
| <a id="c1-29"></a>C1-29 | 낮음 | 낮음 | 그대로 | 흔적 겹침 높이 0.002가 테두리 층 높이 상수를 참조하지 않고 다시 적힘 | StackStep 0.002f는 여전히 FieldOutlineRenderer의 private LayerHeightStep을 참조하지 않고 다시 적은 값이다. |
| <a id="c1-30"></a>C1-30 | 낮음 | 낮음 | 그대로 | 체력 숫자 보임 조건이 SetEntryHudHidden·ShowUnitHud에 다른 모양으로 두 번 | 같은 체력 숫자 보임 규칙이 여전히 모양을 달리해 두 곳에서 계산된다. |
| <a id="c1-31"></a>C1-31 | 낮음 | — | 풀림 | 공격 조준 호버 표시 정리 목록이 ClearShotPreview와 ResetRangeAnswer 람다에 두 벌 | 호버 그림 지우기 목록이 배열 둘 한 곳으로 모였고 ClearHoverDisplay·ClearShotPreview·ResetRangeAnswer가 모두 그 목록을 돌아, 새 그림을 한 곳에만 더해도 Exit 때 지워진다. |
| <a id="c1-33"></a>C1-33 | 낮음 | 낮음 | 그대로 | 공격 방향→칸 변화 8방향 표를 Core에서 베껴 둠 | TryGetDirectionVector 8방향 표가 Core GetDirectionStep 사본으로 남아 있다. |
| <a id="c1-34"></a>C1-34 | 낮음 | 낮음 | 그대로 | 경로 입력의 정리 순서와 표시 실패 정리 규칙이 여러 벌 | 정리 순서가 Detach와 CancelInputs에 두 벌 있고, '표시 실패 시 같은 판이면 LeaveMoveMode' 규칙이 세 곳에 복사돼 있다. |
| <a id="c1-35"></a>C1-35 | 낮음 | 낮음 | 그대로 | 구조물 표시 자격(적·고유 장애물 아님)을 피격 처리기·HUD가 따로 적어 세 벌이고 이미 다름 | 피격 처리기 판정이 CombatStructureHitFeedback.CanShow로 옮겨졌을 뿐 CanInspectStructure를 쓰지 않고 같은 식을 다시 적으며, HUD는 여전히 다른 식(Enemy \|\| 장애물)이라 세 벌이 남아 있다. |
| <a id="c1-36"></a>C1-36 | 낮음 | 낮음 | 그대로 | 표식 사건 반영이 디버프 6종 필드 목록을 손으로 다시 나열 | 표식 사건 반영이 여전히 CharacterDebuffState를 선택 인자 6개로 손으로 다시 만든다. |
| <a id="c1-37"></a>C1-37 | 낮음 | 낮음 | 그대로 | '공격 그룹 0 = 근원 미지정' 판정이 세 곳에 숫자 비교로 따로 있음 | AttackGroup 0 비교가 세 곳에 그대로 있고 CombatAttackSource에 판정 멤버가 생기지 않았다. |
| <a id="c1-38"></a>C1-38 | 중간 | 중간 | 커짐 | 약한 지반 진입·붕괴 소리 프리셋을 코드 숫자로 박아 DB 설정 값을 무시 | 붕괴 소리는 여전히 코드 숫자다(그때 2, 지금 0). 설정 표의 weakGroundCollapseSoundPreset은 여전히 읽는 곳이 없다. 게다가 이제 추락 소리도 FallSoundPreset을 읽지 않고 프리셋 없이 고정 칸에서 낸다(옛 판에서는 presets.Fall()로 표 값을 읽었다). DB 값을 무시하는 소리가 둘로 늘었다. |
| <a id="c1-39"></a>C1-39 | 낮음 | 낮음 | 그대로 | 가속 특성 '완료 이동 칸 수'를 View가 사건을 세어 다시 계산(자발 이동 판정도 사본) | 이동 칸 수를 여전히 사건 수로 세고 자발 판정도 CombatMovementKinds.Of 대신 직접 적는다. |
| <a id="c1-40"></a>C1-40 | 낮음 | 낮음 | 그대로 | '다음 행동 경계까지' 사건 훑기 멈춤 조건이 세 곳에 조금씩 다르게 있음 | 두 파일 모두 바뀌지 않았고, 세 멈춤 조건이 각자 적혀 있으며 방향 찾기만 Returned를 보지 않는 차이도 그대로다. |
| <a id="c1-41"></a>C1-41 | 낮음 | 낮음 | 그대로 | 공격 행위자 몸 자리를 StandingBodyOf와 다른 회전 규칙으로 다시 만듦 | ActorBodyOf는 바뀌지 않았고, 몸 자리 규칙이 StandingBodyOf와 따로 두 벌인 채로 남아 있다. 서는 자리를 빌린 공격자는 두 조회가 다른 회전을 돌려준다. |
| <a id="c1-42"></a>C1-42 | 중간 | 중간 | 그대로 | 곡사 애니메이션이 CombatAttackMotion의 공통 수명 규칙을 상속 없이 따로 구현 | CombatMortarAnimation은 지금도 CombatAttackMotion을 상속하지 않는다. 수명 깃발, Tick 중첩 금지, 쓸 수 없을 때 cancelOwner를 부른 뒤 Cancel하는 절차, 정리 예외 묶기를 따로 구현한다. 비유한 위치 예외 종류(InvalidOperation과 ArgumentOutOfRange)와 Fail 예외 모양의 어긋남도 그대로다. |
| <a id="c1-43"></a>C1-43 | 낮음 | 낮음 | 그대로 | '투사체를 쏘는 종류'를 DB 규칙과 전략 표가 따로 답함 | '투사체를 쏘는 종류'를 DB 규칙 함수와 전략 표가 따로 답한다. 전략 표(CombatAttackStyles.Build)도 둘이 같은지 검사하지 않는다. |
| <a id="c1-44"></a>C1-44 | 낮음 | 낮음 | 그대로 | 곡사 궤적 미리보기가 공격 처리기의 '클립 길/코드 모션 길' 선택 규칙을 복제 | 곡사 궤적 미리보기가 공격 처리기의 길 선택 규칙(공격 클립 유무)을 따로 한 벌 더 갖고 있다. |
| <a id="c1-45"></a>C1-45 | 낮음 | 낮음 | 그대로 | 근접 타격 순간 0.55가 같은 파일에 리터럴로 여러 번 흩어짐 | 파일이 바뀌지 않았다. 근접 타격 순간 0.55가 여전히 리터럴로 여러 번 적혀 있다. |
| <a id="c1-46"></a>C1-46 | 낮음 | 낮음 | 그대로 | 특성 상시 이펙트 찾기 반복문이 두 출처에 복사됨 | 두 출처가 같은 일을 각자 적는다. TraitStatuses를 돌며 null·IsAvailable을 거르고, TryGetTraitVisual을 부르고, AlwaysEffects를 Condition으로 거른다. |
| <a id="c1-47"></a>C1-47 | 낮음 | 낮음 | 그대로 | 보기 크기를 CombatCameraView._size와 CombatCameraWindow._viewLens가 따로 기억 | 카메라 목록과 지금 보기 크기를 여전히 두 클래스가 따로 든다. Dispatch가 중간에 던지면 _viewLens는 새 값, _size는 옛 값으로 어긋난다. |
| <a id="c1-48"></a>C1-48 | 낮음 | 낮음 | 그대로 | CombatPieceStart에 지연을 더하는 분기가 두 곳에 사본 | 'Kind == AtStart ? AtStart(d+delay) : After(...)' 분기가 여전히 두 곳에 따로 있다. CombatPieceStart에는 지연을 더하는 연산이 없다. |
| <a id="c1-49"></a>C1-49 | 낮음 | 낮음 | 그대로 | CombatStandMovePiece만 걸음 값 검사를 설정 Data 한 곳 대신 다른 경로로 함 | Scale·Zoom 조각은 CombatPresentationSettingsData.ValidateSeconds 등에 맡긴다. StandMovePiece만 여전히 CombatPresentationValueRules.RequireNonNegative와 IsDefined로 따로 검사한다. |
| <a id="c1-50"></a>C1-50 | 낮음 | 낮음 | 커짐 | 시간 조각 끝 판정식이 조각마다 사본이고 줌 조각 보장이 '같은 식'에 의존 | '_began && _elapsed >= 시간' 끝 판정과 Tick 누적이 조각마다 따로 있다. 새로 생긴 CombatWaitPiece까지 다섯 벌로 늘었고, 줌 조각 문서는 아직 '크기 조각과 같은 식'이라는 데 기댄다. |
| <a id="c1-51"></a>C1-51 | 낮음 | 낮음 | 그대로 | 전투 화면 Canvas 상수(UI 층 5, 정렬 50/-10, 1920x1080)가 Factory 두 곳에 숫자로 흩어짐 | 몸 글자 Canvas는 여전히 1920x1080을 직접 적고, HUD Factory만 UnitHUDProjection을 읽는다. 층·정렬 숫자도 그대로다. |
| <a id="c1-52"></a>C1-52 | 낮음 | 낮음 | 줄어듦 | CombatEventPlaybackFactory가 settings와 그 파생값 values·transitionSettings를 함께 받음 | 국면 전환이 재생 조립에서 빠지면서 transitionSettings 인자는 사라졌다. 그래도 원본 settings와 그 파생값 values가 아직 따로 들어온다. |
| <a id="c1-53"></a>C1-53 | 낮음 | 낮음 | 그대로 | 유닛 레벨 결정 규칙(적 레벨, 소유 기록 있으면 그 레벨)이 정보창과 UnitCardUI에 따로 있음 | 두 파일 모두 바뀌지 않아 레벨 결정 규칙이 정보창과 UnitCardUI에 따로 있다. |
| <a id="c1-54"></a>C1-54 | 낮음 | 낮음 | 그대로 | 결과 화면 스테이지 표기 '깊이-너비+1'을 승리·패배 경로가 각자 조합 | 두 경로가 아직 같은 형식 문자열을 각자 만든다. |
| <a id="c1-55"></a>C1-55 | 낮음 | 낮음 | 커짐 | 선택 상태와 정보 Presenter가 같은 표시 사실 reader를 따로 주입받음 | 같은 보인 사실 reader를 Factory가 같은 객체를 넘기는 관례로만 맞추는 구조가 남았고, 새 CombatSelectionDisplay가 세 번째 주입처로 늘었다. |
| <a id="c1-56"></a>C1-56 | 낮음 | 낮음 | 그대로 | 피격 기울기 상한 60이 상수와 Inspector Range에 두 번 적힘 | 상한 60이 아직 상수와 Inspector Range 리터럴 두 곳에 있다. |
| <a id="c1-57"></a>C1-57 | 중간 | 중간 | 그대로 | 특성 → 상태 근거 표가 Core의 특성 효과 규칙을 코드에 다시 적음 | e70ad3d6 이후 CombatTraitFeedbackProfiles·CombatTraitStateEvidence·CombatTraitFeedbackPlan은 바뀌지 않았고(TraitActivationVisual만 깜빡임 관련 변경), 특성→Core 상태 타입 대응 표가 범위 안 코드 사본으로 그대로 남아 있다. Core가 발동-상태 연결을 주지 않는 기반 한계를 흡수하는 방식이 범위 안 사본이라 그 몫은 감점 대상이다. |
| <a id="c1-58"></a>C1-58 | 낮음 | 낮음 | 줄어듦 | 수 값 규칙 원본이 있는데 IsFinite·IsNonNegative·클립 이벤트 번호 검사 사본이 따로 있음 | 클립 이벤트 번호 검사 두 벌은 CombatClipEventIds.IsDefined 하나로 합쳐졌다. 그래도 private IsFinite 사본 세 개(카메라 설정·클립 시각·투사체 이징)는 남아 있다. |
| <a id="c1-59"></a>C1-59 | 낮음 | 낮음 | 그대로 | '소리 없음' 상수 NoSoundPreset이 있는데 -1 리터럴을 여러 곳에 다시 적음 | -1 리터럴이 상수 대신 아직 여러 곳에 적혀 있다. |
| <a id="c1-60"></a>C1-60 | 낮음 | 낮음 | 그대로 | '아직 보이지 않은 사망' 판정이 처리기·동시 사망 입구·소리 표에 따로 있고 조건이 이미 다름 | '아직 보이지 않은 사망' 판정이 처리기·동시 사망 입구·소리 표·기차 접촉에 그대로 따로 있고, 조건 차이(소리 표의 Final.IsDead 없음, 동시 사망만 EntityId 검사)도 그대로다. |
| <a id="c1-61"></a>C1-61 | 낮음 | 낮음 | 그대로 | 진입 연출의 적 Nexus 선택이 본부 키 식별식을 다시 적음 | View 안의 식은 IsEnemyNexus 하나로 모였다. 그래도 Core와 같은 본부 키 식별식을 View가 다시 적는다. |

### C2 SOLID (33)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c2-01"></a>C2-01 | 중간 | 중간 | 줄어듦 | 필드 기믹(토네이도·낙석·하붑·가시·흔적)에 공통 수명 계약이 없어 10여 곳에 이름으로 나열됨(OCP) | CombatSceneDisplay의 정리 목록 세 벌은 CombatFieldDisplay의 GimmickClears·GimmickReleases 묶음으로 모여, 설치 실패와 Clear가 같은 목록을 쓴다. 그래서 지난 시나리오(Release에는 넣고 설치 실패·Clear에서 빠뜨림)는 생길 위험이 줄었다. 하지만 공통 기믹 계약은 여전히 없고, 진입 보류 표시·포트·Factory 람다·처리기 표에 기믹별 나열이 남아 있어 새 기믹을 추가할 때 고칠 곳은 여전히 여러 파일로 번진다. |
| <a id="c2-02"></a>C2-02 | 중간 | 낮음 | 줄어듦 | CombatViewSystem 조정자가 서비스 배선·씬 수명·결과 소비·입력 분배·표면 파생을 모두 맡는 God class | 결과 소비·입력 분배·카메라/환경 호버/행동 버튼 맞춤·서비스 조회가 별도 객체로 나갔고(1179→934줄, CanContinue 11곳·ReferenceEquals 3곳), 정보창 차단은 IsInputOwnerActive 질의 하나가 되어 지난 실패 시나리오(새 입력 소유자 → 정보창 같이 열림)는 더 성립하지 않는다. 다만 수명 관리, 진행 호스트, 연결 재확인 계약, 서비스 인자 검사, 입력 재료 16개 조립은 여전히 한 클래스에 있어 입력 서비스를 추가할 때마다 이 파일도 고쳐야 하는 정도가 남았다. |
| <a id="c2-03"></a>C2-03 | 중간 | — | 풀림 | CombatSceneDisplay가 표시 조정·배경음 정책·결과 화면·행동 버튼·피드백 위임·HUD 층 파괴까지 맡는 God 퍼사드 | 배경음 정책(CombatBgmPolicy), 결과 화면·행동 버튼·HUD 표면·필드·선택 표시를 따로 나눴다. 입력 피드백 통과 메서드 약 45개는 CombatInputFeedback 묶음이 실제 주인을 노출하는 방식으로 바뀌었다. 그래서 피드백 하나를 더하거나 배경음 규칙을 고칠 때 CombatSceneDisplay·38인자 호출을 함께 고쳐야 한다는 시나리오가 더는 성립하지 않는다. 남은 일(Prewarm 호출, HUD 층 GameObject 파괴, 정리 순서 목록)은 표시 수명이라는 한 가지 책임 안에 든다. |
| <a id="c2-04"></a>C2-04 | 중간 | 낮음 | 줄어듦 | 클립 이벤트 종류가 enum 4개와 대응 switch 여러 곳에 흩어져 이벤트 추가마다 6~8곳 수정(OCP) | 재생·조각·커서·원거리 조립·큐 규칙의 이벤트별 switch·콜백·하드코딩이 표 두 개(DB 번호 표, View 신호 표) 조회로 바뀌었다. 표는 정적 생성자에서 검사하고 CombatAttackStart가 Validate로 연결 시점에 건드리므로, 연결을 빠뜨려 탄·소리가 조용히 빠지는 실패 시나리오는 더는 성립하지 않는다. 다만 새 번호마다 enum 값 넷(Id·CueAnchor·PieceSignal·AnimationEvents 메서드)과 표 줄 둘을 고쳐야 하는 평행 선언은 남아 있다. 빠뜨리면 컴파일이나 시작 검사에서 드러나므로 낮음이다. |
| <a id="c2-05"></a>C2-05 | 낮음 | 낮음 | 커짐 | 걸음 레인이 도약을 모양 추상 밖 곁길로 처리함(구체 모양 형 검사·표 우회) | 도약을 모양 추상 밖 곁길로 처리하는 구조가 그대로다. 게다가 밀림 젖힘(StartStep 431)과 이동 효과음(StartMotion 463)에도 구체 모양 형 검사가 새로 붙어, 새 모양을 넣을 때 함께 고칠 분기가 늘었다. |
| <a id="c2-06"></a>C2-06 | 낮음 | 낮음 | 그대로 | 상태 설명 문구가 상태 형식·수명·출처 종류 switch 사슬이라 새 상태마다 여러 곳 수정, 빠지면 실행 중 예외 | 구조는 그대로이고 case만 늘었다. 새 상태마다 Factory, switch 두 곳, 외형 표를 함께 고쳐야 한다. |
| <a id="c2-07"></a>C2-07 | 낮음 | 낮음 | 커짐 | FieldUnitPresenter(893줄)가 풀·사실 그리기·HUD 내용 조립·접촉 몸 기하·준비 대여·진입 수집을 모두 맡음 | 893줄에서 1004줄로 늘었다. 몸 기하(BodyAt·BodyCenterAt), 표면 추적, 미리 만들기, 구조물 진입 수집 같은 책임이 더 얹혔다. 행동 커서 그리기만 따로 떨어져 나갔다. |
| <a id="c2-08"></a>C2-08 | 낮음 | 낮음 | 커짐 | FieldUnitView가 HUD·체력 숫자·Transform·대여 5종·외형·클립 준비를 한 클래스에 모아 채널 추가가 여러 곳으로 흩어짐 | 728줄에서 985줄로 늘었다. 아웃라인 대여·강조 계약·카툰 재질 교체·상태 모습·랙돌이 같은 클래스에 더해졌다. 채널 하나를 더하려면 여전히 칸, Begin, Reset, 정리 배열, 중첩 클래스, 계약을 함께 고쳐야 한다. |
| <a id="c2-09"></a>C2-09 | 낮음 | 낮음 | 그대로 | 범용 유닛 표시 계약에 특정 유닛 무기 구체 타입(CatusCannonWeaponView)이 박혀 계약 사슬로 퍼짐(DIP) | 범용 표시 계약이 여전히 sealed 구체 타입 CatusCannonWeaponView를 요구한다. 이 타입은 창구·스타일 문맥·곡사 전략으로 퍼져 있다. |
| <a id="c2-11"></a>C2-11 | 낮음 | 낮음 | 그대로 | CombatShownFacts가 보인 사실·검사 조회·위치 교환 보류·끌어오기 재작성·유지 이펙트·몸 기하를 한 클래스에 모음 | 파일은 487줄로 그대로 하나의 클래스다. 검사 조회·위치 교환 보류·끌어오기 재작성·유지 이펙트 동기화·몸 기하를 모두 갖고 있고, ApplyCharacter 안에 _placements.Drop 부수효과(94-109)도 남아 있다. |
| <a id="c2-12"></a>C2-12 | 낮음 | 낮음 | 그대로 | FieldRailwayPresenter가 설치·열차 PlayableGraph·이펙트 수명·재생 시계를 한 클래스에서 맡음 | 한 클래스가 여전히 설치, PlayableGraph, EffectScope, 재생 시계(Begin/Tick/CleanupPlayback)를 맡는다. 재진입 깃발 7개도 그대로다. |
| <a id="c2-13"></a>C2-13 | 낮음 | 중간 | 커짐 | CombatCharacterEventHandler가 피해 숫자·피격·사망·추락 사망·토네이도 던짐을 함께 맡음 | 339줄·13인자에서 538줄·16인자로 커졌고, 자폭 사망·밀림 동시 피격·연발 접촉 피격 갈래가 같은 처리기에 더해져 바뀌는 이유가 더 늘었다. |
| <a id="c2-14"></a>C2-14 | 낮음 | 낮음 | 그대로 | AttackTargetingInteraction이 모드 수명·조준 기하·이유 요청·결과 표시 조율을 한 클래스에 묶음 | 모드 수명·범위 캐시·호버/이유 요청 번호·표시 조율·조준 기하가 여전히 한 클래스(682→767줄)에 있다. 표시 계약 타입이 나뉘어 같은 타입 위치 인자 혼동 위험은 사라졌다. |
| <a id="c2-15"></a>C2-15 | 낮음 | 낮음 | 줄어듦 | 계약에 없는 능력을 형변환으로 꺼냄(ICombatGimmickRangeFeedback 형식 검사, FieldUnitView 다운캐스트) | ICombatGimmickRangeFeedback 형식 검사는 입력 피드백 묶음 속성으로 바뀌어 없어졌다. 그래도 유닛 대여 반환의 FieldUnitView 다운캐스트는 남아 있다. |
| <a id="c2-17"></a>C2-17 | 낮음 | — | 풀림 | ICombatInteractionFeedback(멤버 17개)를 소비자마다 겹치지 않는 일부만 씀(ISP) | ICombatInteractionFeedback가 지워지고 공격 범위·호버 칸·배치 고스트 계약 셋으로 나뉘어 각 소비자가 필요한 계약만 받는다. |
| <a id="c2-18"></a>C2-18 | 낮음 | 낮음 | 그대로 | 구현 하나뿐인 생성 계약 ICombatSceneFactory·ICombatSceneInputFactory가 정적 Factory로 위임만 하고 구체 타입을 돌려줌(과도한 추상화) | 구현이 하나뿐인 두 계약이 정적 Factory에 위임만 한다. CreateFactsReader 10인자 전달과 CreateDisplay의 구체 타입 반환도 그대로다. |
| <a id="c2-19"></a>C2-19 | 낮음 | 낮음 | 그대로 | View 조회 묶음 CombatDisplayTables가 파생 객체를 조립하고 Factory 계층 CombatUnitStatusFactory를 직접 부름(DIP) | 조회 묶음이 아직 파생 객체(상태 변환기·소리 프리셋·큐)를 직접 조립하고, View 런타임에서 정적 Factory를 직접 부른다. 옛 판과 같은 구조다. |
| <a id="c2-20"></a>C2-20 | 낮음 | 낮음 | 그대로 | CombatPieceRun이 출처를 쓰지 않는 진입 연출에도 ICombatSourceLifetime을 강요해 '던지는 가짜'를 넘기게 함(ISP/LSP) | 이름만 바뀌었다. 출처를 쓰지 않는 진입도 던지는 가짜 출처 수명을 넘겨야 하는 구조는 그대로다. |
| <a id="c2-21"></a>C2-21 | 낮음 | 낮음 | 그대로 | 배치 진입·턴 종료 확인창이 같은 수명 구조를 두 쌍으로 복제하고 묶음은 확인창을 세 집계에 하나씩 나열 | Entry·Confirmation 두 쌍이 거의 같은 수명 구조를 복제한다. 묶음이 세 집계에 확인창을 하나씩 나열하고 정리를 손으로 쓰는 것도 그대로다. |
| <a id="c2-22"></a>C2-22 | 낮음 | 낮음 | 그대로 | 처리기 표 Create가 같은 형식 인자 12개 + 칸 switch라 사건 형식마다 서명·switch·조립 줄을 고침(OCP) | 구조는 그대로이고 증원 칸이 추가되며 같은 절차(서명·switch·조립 줄 수정)를 한 번 더 밟았다. |
| <a id="c2-23"></a>C2-23 | 낮음 | 낮음 | 그대로 | 사건 형식 표 TypeRow가 위치 인자 15개라 열 추가가 같은 파일 다섯 곳 수정이고 순서 실수가 컴파일됨 | TypeRow가 여전히 위치 인자 14개 생성자이고 같은 대리자 형식(actor/subject, isMovement/opensAttackSource)이 겹쳐 Wrap 순서 실수가 컴파일된다. |
| <a id="c2-24"></a>C2-24 | 낮음 | 낮음 | 그대로 | CombatEnvironmentHover 한 클래스에 설명 툴팁·호버 영역·토네이도 클릭 고정이 상태 깃발을 공유하며 얽힘 | 설명 툴팁·호버 영역·토네이도 클릭 고정이 여전히 한 클래스에서 깃발을 공유하고, 숨김 번호 갈래가 선택 영역을 다시 그린다(넥서스 범위는 별도 객체로 붙었지만 Refresh 흐름은 그대로). |
| <a id="c2-25"></a>C2-25 | 낮음 | 낮음 | 그대로 | FieldOutlineRenderer(787줄)가 슬롯·높이 분할·겹침 분할·경로 구간 계산·데칼을 한 클래스에서 맡음 | 파일이 826줄로 오히려 늘었다. 슬롯·Collect·높이 분할·겹침 분할·경로 구간·데칼을 여전히 한 클래스가 맡는다. |
| <a id="c2-26"></a>C2-26 | 낮음 | 낮음 | 그대로 | CombatAttackPlayback이 수명 관리 외에 접촉 시각 계산·진단 경고·접촉 이펙트까지 맡음 | 수명 조정, 접촉 진행률 계산, 진단 경고, 접촉 이펙트 재생이 여전히 한 클래스에 있다. 지금은 프로파일러 필드까지 더해졌다. |
| <a id="c2-27"></a>C2-27 | 낮음 | 낮음 | 그대로 | CombatCueSources가 큐 답 외에 외형 표 전달과 몸 글자 프리팹 수집·검증까지 맡음 | CombatCueSources는 바뀌지 않았다. 큐 답 객체가 여전히 외형 표 전달과 몸 글자 프리팹 수집·검증까지 맡는다. |
| <a id="c2-28"></a>C2-28 | 낮음 | 낮음 | 그대로 | UnitHUDLayer가 투영 외에 선택 패널 UI 동적 등록·열기·정렬까지 맡음 | 레이어가 투영·가시성 말고도 UI 등록·열기·정렬·준비 배치·위젯 투영을 그대로 함께 맡는다. |
| <a id="c2-29"></a>C2-29 | 낮음 | — | 풀림 | 유닛 정보창 CombatUnitInfo가 국면별 덱 카드 목록(UnitCard) 개폐 정책까지 맡음 | 정보창 Render에서 UIId.UnitCard를 닫고 여는 코드가 빠졌다. 국면별 덱 목록 개폐는 이제 CombatDeploymentPresentation이 맡는다. |
| <a id="c2-30"></a>C2-30 | 낮음 | 낮음 | 그대로 | 전역 연출 설정 CombatPresentationSettingsData에 12개 넘는 기능 값 약 55개와 순서 고정 검사가 평평하게 몰림 | 하위 묶음은 여전히 셋뿐이고, 나머지 기능 값과 순서 고정 검사는 평평한 한 클래스에 그대로 몰려 있다. |
| <a id="c2-31"></a>C2-31 | 낮음 | 낮음 | 그대로 | CombatRockfallDropPiece가 조각 계약의 '자원은 직접 빌리지 않는다' 보장을 어김(LSP) | 계약 문서는 지금도 '자원은 직접 빌리지 않는다'고 적는다. 그런데 낙석 조각은 Begin에서 _field.RentStone으로 직접 빌리고 Finish/Cancel에서 직접 돌려준다. |
| <a id="c2-32"></a>C2-32 | 낮음 | 낮음 | 그대로 | 걸음 모양 계약이 고정 접촉 모양에도 닿지 않는 ContactProgress 구현을 강요함(ISP) | 파일이 바뀌지 않았다. 고정 접촉 모양(뛰기·도약)은 아무도 부르지 않는 ContactProgress(Landing·1f)를 여전히 구현해야 한다. |
| <a id="c2-34"></a>C2-34 | 낮음 | 낮음 | 그대로 | 멤버 없는 표지 인터페이스 ICombatAttackUnitPose와 같은 일을 하는 Begin 경로 두 벌(과도한 추상화) | 표지 인터페이스와 같은 일을 하는 Begin 경로 두 벌이 그대로 있다. |
| <a id="c2-35"></a>C2-35 | 낮음 | 낮음 | 그대로 | Display 좌표 변환기 FieldGridMapper가 Presentation 타입 CombatBody에 의존해 폴더 의존이 양방향이 됨(DIP) | 기반 좌표 변환기가 지금도 Presentation 타입 CombatBody에 의존해 Display↔Presentation 양방향 의존이 남았다. |
| <a id="c2-36"></a>C2-36 | 낮음 | 낮음 | 그대로 | 연출 부품(Pieces)이 상위 Playback 조정자 CombatSceneEventPresenter의 문자열 상수에 의존함(DIP) | Pieces의 CombatPieceRun과 CombatPieceRunLeases가 여전히 상위 조정자의 CombatSceneEventPresenter.CleanupFailureMessage를 직접 참조한다. |

### C3 FW 지침·계약 (9)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c3-01"></a>C3-01 | 중간 | — | 풀림 | 토네이도 도착 칸을 Core가 주는 DestinationCells 대신 View가 Travel·Footprint로 다시 계산 | View가 TornadoGeometry.Travel·Footprint와 정의 재탐색(GimmickId-1, SourceId)으로 도착 칸을 다시 계산하던 사본이 사라지고 Core가 공개한 DestinationCells를 그대로 쓰므로, Core 이동 규칙이 바뀌어도 어긋나지 않고 Member() 예외 경로도 이 기능에서 없어졌다. |
| <a id="c3-03"></a>C3-03 | 낮음 | 낮음 | 그대로 | 정리 실패 모으기를 공용 CleanupErrors 대신 공격 연출 파일마다 손으로 구현 | 공격 연출 네 곳이 아직 공용 CleanupErrors를 쓰지 않고, 풀만 예외 모양이 다르다. |
| <a id="c3-04"></a>C3-04 | 낮음 | 낮음 | 그대로 | 낙석 돌만 Framework CustomPool 대신 손으로 만든 Stack 풀을 씀 | 낙석 돌만 아직 _stones 목록과 _freeStones Stack으로 만든 손 풀을 쓴다. 파괴된 인스턴스를 건너뛰는 처리도 없다. |
| <a id="c3-05"></a>C3-05 | 낮음 | 낮음 | 그대로 | Nexus 낙하가 조각 실행기(CombatPieceRun)의 자세 대여·잃음·세대 처리를 따로 구현 | Nexus 낙하는 아직 조각 실행기의 대여·세대 처리를 따로 다시 짠다. 바닥 눌림이 더해져 조각 계약 밖의 몫도 늘었다. |
| <a id="c3-06"></a>C3-06 | 낮음 | 낮음 | 그대로 | 아이템 칸 갱신 한 줄 때문에 View가 Core Application 층(ItemCharacterChanges)에 의존 | ShowEquippedItem이 여전히 Core Application 층의 ItemCharacterChanges.WithItems를 쓴다. |
| <a id="c3-07"></a>C3-07 | 낮음(기반 한계) | 낮음 | 그대로 | View가 표시값을 Buffer 사실 대신 Core Application 규칙 함수로 직접 계산(기반 한계) | View가 Core Application 정적 함수로 표시값을 계산하는 것은 그대로다(기반 한계, base_limit=true). |
| <a id="c3-08"></a>C3-08 | 낮음(기반 한계) | 낮음 | 그대로 | Global 서비스에 없는 UI 기능(확인창·동적 등록)을 형식 검사(downcast)로 꺼냄(기반 한계) | 두 downcast가 자리만 옮긴 채(BindServices→Bind) 남아 있다. GlobalSystemGroup이 두 계약을 서비스로 내주지 않아 생긴 기반 한계(base_limit)다. 실패하면 예외로 드러난다. |
| <a id="c3-09"></a>C3-09 | 낮음 | 낮음 | 그대로 | UISystemService가 null이면 HUD 부품이 UISystem을 거치지 않고 직접 Open/Hide | 세 곳 모두 서비스가 null이면 여전히 직접 Open()/Hide()로 넘어간다. 운영 연결이 빠져도 오류가 나지 않는다. |
| <a id="c3-10"></a>C3-10 | 낮음 | 낮음 | 그대로 | 이펙트 개별 회수를 위해 Framework EffectPool 노드의 ParticleSystem·Renderer를 직접 조작 | 요청별 정지 API가 없다는 기반 한계를 우회하는 범위 안 직접 조작이 그대로 있다(base_limit=false). |

### C4 기능 변경 독립성 (24)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c4-02"></a>C4-02 | 중간 | 중간 | 그대로 | 사건 Presenter 생성자가 처리기·재생기 20여 개를 직접 조립하는 두 번째 조립 지점 | 미리보기·운영 두 곳의 ports 위치 인자 사본은 CreateFieldPorts 하나(이름 인자)로 모여 일부 줄었지만, 주된 문제인 Presenter 생성자의 두 번째 조립 지점은 그대로이고 오히려 부품·처리기가 늘어 약 100줄이 되었습니다. 새 처리기 추가가 여전히 ports·Create·Presenter 생성자로 번지므로 같은 심각도로 남습니다. |
| <a id="c4-03"></a>C4-03 | 낮음 | 낮음 | 그대로 | 진입 연출·유닛 준비의 수명 지식이 조정자 15곳 남짓에 흩어짐 | 연결 객체는 여전히 EntryPresentation·Preparation 참조만 든다. 수명·대기 판단은 조정자 여러 곳에 흩어져 있고, 덱 UI 차단 방식도 여전히 둘(속성 null / canReceiveUiInput)이다. |
| <a id="c4-04"></a>C4-04 | 낮음 | 낮음 | 커짐 | 진입 연출이 표시 클래스 여러 곳에 전용 멤버와 Install 순서 의존을 퍼뜨림 | Install 순서 의존과 구체 표시 타입 의존이 그대로다. 그 위에 구조물·Object·바닥 눌림 단계가 더해져 진입 전용 멤버가 FieldObjectPresenter와 수집기 셋으로 더 퍼졌다. |
| <a id="c4-05"></a>C4-05 | 중간 | — | 풀림 | 도약 중간 경유의 배치 대여 유지가 자세·서는 자리를 빌리는 다른 기능과 숨은 순서로 묶임 | 레인의 HealthChanged·HealthNormalized 사건 종류 목록과 특성 처리기의 선택 콜백 beforeUnitAction(PauseLeap)이 없어졌고(저장소 전체 grep 0건), 창구가 자세·서는 자리 대여 요청 때 도약 경유 배치를 스스로 양보한다. 새 피격 반응 사건이나 새 서는 자리 기능이 레인 목록·콜백을 몰라도 RequireNoPlacement 예외나 CanBeginStand 거짓으로 빠지지 않으므로 실패 시나리오가 성립하지 않는다. |
| <a id="c4-06"></a>C4-06 | 낮음 | 낮음 | 그대로 | 걸음 레인 재료·연출 값이 값 묶음 → Presenter → 레인 집합 → 레인 여러 층을 뚫고, 중간층이 기본값으로 조용히 대체 | 레인 집합은 지금도 자기가 쓰지 않는 레인 재료 10개를 필드로 들고 Lane()에서 그대로 넘긴다. 선택 인자와 ?? Default 대체도 여러 층에 남아 있다. |
| <a id="c4-07"></a>C4-07 | 낮음 | 낮음 | 그대로 | 입력 사실 Reader·입력 재료 하나를 더하려면 그대로 넘기기만 하는 생성 서명 다섯 층을 뚫어야 함 | 같은 10개 인자를 그대로 넘기는 생성 서명 다섯 층(조정자 → 인터페이스 → 어댑터 → 정적 Factory → Reader 생성자)이 그대로다. 구현은 아직 하나뿐이다. |
| <a id="c4-08"></a>C4-08 | 중간 | — | 풀림 | 큐 창구들이 선택 인자 사슬로 처리기 → 공격 조립 3입구 → CombatCueSinks를 뚫고 내려감 | 창구가 이제 ForAttack이 만든 CombatCueSinks 묶음 하나로 세 입구를 거쳐 CombatCueComposer까지 내려가고, effects·camera·projectileSounds 같은 낱개 선택 인자(기본값 null)는 없어졌다. 새 창구는 ForAttack 인자와 SinksFor 한 줄에서 필수 인자로 컴파일 때 걸리므로, '한 입구 호출에서만 빠뜨려 그 큐가 조용히 사라지는' 시나리오는 더는 성립하지 않는다(남은 선택 인자 canRelease·launchAimPoint·contactFeedback은 창구가 아니다). |
| <a id="c4-09"></a>C4-09 | 낮음 | 낮음 | 그대로 | CombatPieceRunPlayer가 쓰지 않는 의존 다섯 개를 받아 CombatPieceRun에 넘기기만 함 | 재생기는 여전히 window·camera·generation·mapper(·기본 lifetime)를 직접 쓰지 않고 CombatPieceRun 생성자로 넘기기만 한다. |
| <a id="c4-10"></a>C4-10 | 낮음 | 낮음 | 그대로 | 공격 연출 값 객체가 모든 종류 값을 27인자 평평한 생성자로 받아, 부품 값·새 종류 하나가 SO·CreateData·생성자·검증 사슬을 모두 뚫음 | 공격 값 객체와 CreateData 구조가 바뀌지 않아, 새 칸이나 새 종류를 넣으면 여전히 여러 층을 함께 고쳐야 한다. |
| <a id="c4-11"></a>C4-11 | 낮음 | 낮음 | 그대로 | 강조 색·층 하나 추가가 FieldView 직렬화 필드·22개 Color 위치 인자·팔레트 switch·테두리 열기 목록을 함께 뚫음 | 테두리 열기·호버 범위 목록은 층 표 열로 옮겨져 그 부분은 풀렸다. 하지만 색 하나를 늘리면 여전히 SerializeField, 위치 인자, TryGet switch를 함께 고쳐야 한다. 실제로 인자는 22개에서 24개로, 선택 인자는 둘로 늘어 자리 바뀜·누락 위험도 그대로다. |
| <a id="c4-12"></a>C4-12 | 낮음 | 낮음 | 그대로 | 입력 요청 하나의 처리 갈래가 수신 창구·조정자 RouteInput·CombatSceneInput.Route·Router 네 곳에 흩어짐 | 특별 갈래가 조정자에서 CombatSceneInputDriver로 옮겨졌을 뿐, 수신 창구·분배기·CombatSceneInput.Route·Router 네 곳에 흩어진 구조가 그대로다. 기본 else의 환경 선택 취소와 FocusLost 기대도 남아 있다. |
| <a id="c4-13"></a>C4-13 | 중간 | 낮음 | 줄어듦 | 입력 소유자를 하나 더하면 포인터 대상 판정식 인자가 늘어 경로 입력·환경 호버까지 고쳐야 함 | 경로 입력과 환경 호버는 이제 facts.PointerTarget만 읽고, CombatInputFacts는 인자가 모두 필수입니다. 그래서 '경로 입력·환경 호버까지 서명을 고쳐야 하고 Reader가 값을 빠뜨려도 컴파일된다'는 핵심 시나리오는 더 성립하지 않습니다. 남은 것은 두 가지입니다. 모드형 소유자를 하나 더하면 정적 판정식의 bool 인자와 그 호출 2곳(CombatInteraction·FactsReader 대체 호출)을 함께 고쳐야 합니다. Router의 UI 막힘 목록과 IsRangeHoverTarget 목록도 손으로 나열돼 있습니다. 둘 다 입력 폴더… |
| <a id="c4-15"></a>C4-15 | 낮음 | 낮음 | 줄어듦 | '다음 사건 시작을 막는가' 규칙이 Factory 람다·사건 재생 조정·재생기 목록 세 곳에 나뉘고, 두 Factory가 다르게 조립 | 미리보기가 운영과 같은 포트 함수를 쓰게 됐고 국면 전환도 사건 재생에서 빠져 두 조립의 어긋남은 없어졌다. 그래도 '다음 사건을 막는가' 규칙은 아직 Factory 람다와 기차 접촉의 목록 밖 조건으로 나뉘어 있다. |
| <a id="c4-16"></a>C4-16 | 낮음 | 낮음 | 줄어듦 | 연출 설정 항목 클래스가 무관한 DB 표·런타임 부품의 값 조건 함수 창고가 됨 | 보드 등장은 이제 ValueRules를 직접 불러 그 오용은 없어졌다. 그래도 다른 DB 표와 런타임 부품 약 20개 파일이 설정 항목의 조건 함수를 부르고, CombatRisePiece는 여전히 ValidateRiseDepth를 공유한다. |
| <a id="c4-18"></a>C4-18 | 낮음 | 낮음 | 그대로 | 생성 뒤 Bind* 호출 순서가 운영 Factory 한 곳의 관례에 기대고, 미리보기 조립은 일부를 빠뜨림 | 생성 뒤 Bind 순서에 기대는 구조가 그대로이고, 미리보기 조립은 아직 BindStructureDamage를 빠뜨린다. |
| <a id="c4-19"></a>C4-19 | 낮음 | 낮음 | 그대로 | 이펙트 창구의 Rider 높이 보정이 Presenter 생성자의 뒤늦은 BindBodyCategories에 숨은 순서 의존 | 창구는 Factory가 만들고 병과 조회는 Presenter 생성자가 뒤에 꽂는 순서 의존이 그대로다. 창구가 유닛 병과 규칙까지 맡는 것도 같다. |
| <a id="c4-20"></a>C4-20 | 낮음 | 낮음 | 그대로 | FieldUnitPresenter 설정이 네 입구로 흩어져 호출 순서에 숨은 의존이 있고 두 조립이 같은 순서를 반복 | ConfigureHUDCategories는 없어졌다. 그래도 설정 입구는 셋(색·Configure·BindItemUnequip)으로 나뉘어 있고, 두 조립이 같은 순서를 반복하며, 순서 의존도 그대로다. |
| <a id="c4-22"></a>C4-22 | 낮음 | 낮음 | 그대로 | 테두리 표시가 채우기 부품의 Apply 순서와 내부 깊이 상태에 기댐 | FieldCellFillStack은 바뀌지 않았다. 테두리는 지금도 채우기 부품의 Apply 순서와 내부 깊이 상태에 기댄다. |
| <a id="c4-23"></a>C4-23 | 낮음 | 낮음 | 그대로 | 점유 거절 뒤 확정이 끝나려면 Poll이 같은 옛 응답을 다시 넘겨야 함(숨은 호출 의존) | 확정 거절 처리가 여전히 다음 Tick에 Poll이 같은 옛 응답을 다시 넘기는 데 숨어서 기댄다. |
| <a id="c4-24"></a>C4-24 | 낮음(기반 한계) | 낮음 | 그대로 | 넉백 후속 대응이 Core 공격 처리의 사건 기록 순서에 숨은 의존을 가짐 | 파일이 바뀌지 않았다. Core 사실 모양(넉백에 근원 없음)이 강제한 순서 의존이 그대로이며, 기반 한계 낮음이다. |
| <a id="c4-27"></a>C4-27 | 낮음 | 낮음 | 커짐 | 진입 적 등장이 보스 소환 등장 값(SummonRise*)을 그대로 써 두 연출이 묶임 | 적 등장(이름이 CombatEntryRiseAppearance로 바뀜)은 여전히 소환 상승 값을 쓰고, 보드 위 구조물 진입도 같은 값을 쓰게 되어 묶인 범위가 넓어졌다. 주석상 의도(평가 C10-06)지만 독립 조정 불가는 그대로다. |
| <a id="c4-28"></a>C4-28 | 낮음 | 낮음 | 그대로 | Nexus 착지 이펙트 종류만 코드 상수(EffectType.FallingRocks)라 낙석 외형과 묶임 | 다른 낙하 값은 설정에 있지만 Nexus 착지 이펙트 종류만 아직 코드에 고정돼 낙석 외형과 묶여 있다. |
| <a id="c4-29"></a>C4-29 | 낮음 | 낮음 | 그대로 | 배경 표시(StageViewPropPresenter)가 낙석 기능의 앵커 이름·필드·초기화를 직접 가짐 | 배경 클래스가 여전히 낙석 앵커를 직접 들고 있다. 이름 상수 3개, 필드 4개, 속성 4개, 이름 검색 3회, Release 초기화 4줄이며 FieldRockfallPresenter가 이 상수를 참조한다. |
| <a id="c4-30"></a>C4-30 | 낮음 | 낮음 | 그대로 | 표시 조립 Factory가 GlobalSystem TurnCost UI를 닫는 실행 정책을 함께 수행 | 표시 조립 Factory가 여전히 TurnCost UI를 닫는 실행 정책을 수행하고 카메라 대체식도 두 번 적혀 있다. |

### C5 불필요 반복·검사 (56)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c5-01"></a>C5-01 | 중간 | — | 풀림 | 꺼 둔 단계(아침) 화면 전환 경로가 조립·설정 검사·재생 판정에 그대로 남음 | CombatPhaseTransitionPresenter.cs와 CombatPhaseTransitionSettings.cs가 지워졌다(name_status D). 범위 안 코드 어디에도 PhaseTransition·TransitionOf·morningTransition·RequireMapCamera·TransitionLayer가 남아 있지 않다. 그래서 쓰지 않는 레이어 이름이나 카메라 이름 검사 때문에 조립이 멈출 수 없고, 늘 false인 IsPlaying 조건도 재생 Gate에 없다. |
| <a id="c5-02"></a>C5-02 | 중간 | — | 풀림 | 입력 계층이 늘 넘겨받는 협력자를 선택 인자로 받고 null 갈래를 곳곳에 둠 | 지적했던 협력자는 모두 생성자에서 '?? throw'로 필수가 되었고 '= null' 기본값도 없어졌습니다. AttackTargetingInteraction은 가용성 응답이 없을 때 공격 가능이 아니라 거짓으로 읽습니다. 그래서 조립에서 인자를 빠뜨려도 공격 사전 거절·안내가 조용히 사라지는 시나리오는 성립하지 않습니다. 남은 '?.'는 실제 수명 때문에 비는 참조뿐입니다. Stop 뒤의 _path·_environmentHover·_camera, BindRoot 전의 _root가 그것입니다. |
| <a id="c5-03"></a>C5-03 | 낮음 | 낮음 | 그대로 | 쓰지 않는 HandPresenter·HandCardHoverView가 Combat 씬에 켜진 채 남아 매 프레임 Update | remarks의 시험 언급만 고쳐졌다. 쓰지 않는 컴포넌트는 Combat.unity에 켜진 채 남아 Update 등을 돌고, 약 300줄 코드도 그대로다. |
| <a id="c5-05"></a>C5-05 | 낮음 | 낮음 | 그대로 | '시험·진단용 읽기'라 적힌 운영 멤버를 시험도 운영도 쓰지 않음 | 주석이 '시험·진단용'에서 '부르는 곳 없음'으로 정직해졌을 뿐이다. 죽은 멤버와 null 분기는 그대로 남아 있다. |
| <a id="c5-06"></a>C5-06 | 낮음 | 낮음 | 그대로 | 조립 Factory에 쓰지 않는 Create 래퍼·기본 오류 경계·backdrop=null 분기·늘 null인 selectionChanged 인자 | InGameViewSystemFactory.Create는 저장소에 호출자가 없고, 기본 오류 경계·backdrop 기본값·늘 null인 selectionChanged 인자와 BindSelectionChanged 두 길이 그대로 있다. |
| <a id="c5-07"></a>C5-07 | 낮음 | 낮음 | 그대로 | 맵 카메라 사실의 시험용 Transform 선택 인자 때문에 운영에 null 검사가 생김 | 타입 이름은 CombatCameraSnapshot으로 바뀌었지만 선택 인자 transform은 그대로다. 그래서 운영에서 닿지 않는 null 분기와 소비자의 재검사가 남아 있다. |
| <a id="c5-08"></a>C5-08 | 낮음 | 낮음 | 그대로 | 재생 처리기·이동 재생이 늘 받는 협력자를 선택 인자로 두고 null 갈래·기본값을 겹쳐 둠 | thornBody·objectBody·leapMotion은 여전히 '= null'이고, leapMotion ?? Default가 세 곳에 있다. 처리기의 '없으면 건너뜀' 선택 인자(consumeThorn·hideObject·showTerrainChange·showTrail·positionSwap·camera)도 그대로다. 그런데 유일한 생성 호출은 늘 값을 넘긴다. |
| <a id="c5-09"></a>C5-09 | 낮음 | 낮음 | 줄어듦 | 운영에서 쓰지 않는 표시 생성 경로(의존 없는 계획 Presenter 생성자, entryPresentation 기본값 null) | 쓰지 않던 계획 Presenter 생성자는 없어졌다. 하지만 운영에서 늘 채워지는 entryPresentation의 null 기본값과 '?.' 검사 4곳은 그대로다(조정자의 AttachCreated·연결 기본값도 같음). |
| <a id="c5-10"></a>C5-10 | 낮음 | 낮음 | 그대로 | CombatViewSystem 연결·표시 경로와 표 묶음이 같은 전제를 층마다 다시 검사 | 층마다 같은 전제를 다시 검사하는 구조가 그대로 남아 있다. SyncActionAvailability만 입력 분배(SyncAvailability)로 옮겨졌을 뿐이다. |
| <a id="c5-11"></a>C5-11 | 낮음 | 낮음 | 그대로 | CreateSceneInteraction이 이미 보장된 값 11개를 다시 null 검사 | CreateSceneInteraction(이제 private)이 Sources 생성자에서 이미 검사됐거나 직전에 만든 값을 여전히 모두 다시 null 검사한다. |
| <a id="c5-12"></a>C5-12 | 낮음 | — | 풀림 | 해제 확인이 CombatSceneDisplay와 하위 Presenter·Mapper에서 겹침 | Display가 표시 의도 메서드를 감싸 _isReleased를 보던 층이 없어졌다. 이제 입력 피드백 호출은 주인(Presenter·Mapper)에게 바로 가고, 해제 확인도 주인 한 층에서만 한다. |
| <a id="c5-13"></a>C5-13 | 낮음 | 낮음 | 그대로 | 공격 Tick 경로마다 deltaTime 검사·중첩 Tick 가드·IsCurrent 확인을 층층이 반복 | dt 검사는 Presenter·Motion·Mortar(116)·Pool(156)에 여전히 겹쳐 있다. 중첩 Tick 가드와 IsCurrent 두 번 확인도 그대로다. |
| <a id="c5-14"></a>C5-14 | 낮음 | 낮음 | 그대로 | 가용성 응답의 개정 번호·입력 가능 조건을 늘 참인 채로 다시 확인 | Read의 개정 번호 비교는 Consume(answer.SourceRevision==_queryRevision)과 Matches(_queryRevision==facts.QueryRevision)로 늘 참이고, Undo ReadAnswer의 앞선 재검사도 그대로다. |
| <a id="c5-15"></a>C5-15 | 낮음 | 낮음 | 그대로 | 토네이도 착지 콜백이 CombatPieceRunPlayer가 보장한 세대 검사를 두 번 더 함 | 토네이도 착지 콜백이 재생기가 보장한 세대 검사를 소리 앞뒤로 그대로 다시 한다. |
| <a id="c5-16"></a>C5-16 | 낮음 | 낮음 | 그대로 | Core 사건 생성자·호출 순서가 보장한 조건을 View 소비자가 다시 검사 | Core 생성자 보장은 같고 View 쪽 '?.' 재비교·형식 표 대체식·IsSelfDrivenStep 겹친 조건이 그대로 남아 있다. |
| <a id="c5-17"></a>C5-17 | 낮음 | 낮음 | 그대로 | 연출 설정을 표 생성·표시 조립·부품 생성·Prepare마다 거듭 Validate | 표 생성·Display/Preview Factory·BoardReveal·Schedule·Railway/Nexus/GroundCompression 생성자에서 같은 설정 검사가 여전히 거듭되고 Camera 대체식도 두 번이다. |
| <a id="c5-18"></a>C5-18 | 낮음 | 낮음 | 그대로 | 카메라 설정 SO와 클립 시각 읽기가 같은 검사를 SO와 값 생성자에서 연달아 두 번 | 카메라 설정 SO와 클립 시각 읽기 모두 같은 검사를 앞에서 한 번, 값 생성자에서 또 한 번 돈다. |
| <a id="c5-19"></a>C5-19 | 낮음 | 낮음 | 그대로 | 적 연출 간격 계산의 늘 참인 삼항식·안 쓰는 인자·한 줄 전달 검증 계층 | 늘 참인 삼항식, 쓰지 않는 result 인자, 한 줄짜리 ValidateSeconds 전달 계층이 모두 남아 있다. |
| <a id="c5-20"></a>C5-20 | 낮음 | 낮음 | 그대로 | CombatEndUI가 전체 참조 검사(슬롯 BindOwner 포함)를 한 표시 흐름에서 여러 번 반복 | 파일이 바뀌지 않았고 BindUIService·Present·Open마다 전체 참조 검사와 BindOwner가 반복된다. |
| <a id="c5-22"></a>C5-22 | 낮음 | 낮음 | 그대로 | 곡사 무기 자세 진행 값을 쓸 때마다 Validate(IsChildOf)를 다시 함 | BeginPose와 창구가 이미 한 검사를 진행 값을 쓸 때마다 다시 한다. |
| <a id="c5-23"></a>C5-23 | 낮음 | 낮음 | 그대로 | 전투 중 바뀌지 않는 회전 표를 결과 끝마다 다시 찾고 다시 검증 | Object·Terrain Render는 결과 끝마다 여전히 회전 표를 다시 찾고 회전 × 분류 Id를 다시 검증한다. 전투 중 스테이지는 바뀌지 않는다. |
| <a id="c5-u0025"></a>C5-U0025 | 낮음 | 낮음 | 그대로 | CombatCursorSnap의 SnappedCell·Reset은 운영·시험 어디서도 부르지 않음 | 저장소 전체(Assets/Tests 포함) grep에서 SnappedCell·Reset 호출자가 없다. |
| <a id="c5-u0029"></a>C5-U0029 | 낮음 | 낮음 | 그대로 | 쓰이지 않는 입력 멤버, 그중 HasCurrentAcceptedEmptyMovementRange는 사실을 읽을 때마다 계산되고 추적기 상태까지 지움 | HasCurrentAcceptedEmptyMovementRange는 여전히 읽는 곳이 없는데 사실을 읽을 때마다 계산되고 추적기 상태를 지운다. 읽기 doc에 그 부작용을 적었을 뿐 사슬은 남아 있다. Gate.Pending도 읽는 곳이 없다. |
| <a id="c5-u0047"></a>C5-U0047 | 낮음 | 낮음 | 그대로 | CombatStepContacts의 쓰이지 않는 표면(TryGetSegment·Group.SourceId·Tile 갈래) | 파일이 바뀌지 않았다. TryGetSegment와 Group.SourceId는 저장소를 grep해도 읽는 곳이 없다. Tile 갈래는 여전히 확장점이고 몸 조회도 처리하지 않는다. |
| <a id="c5-u0065"></a>C5-U0065 | 낮음 | 낮음 | 그대로 | 진입 보류 시작의 기믹 Clear 셋과 _prepared는 운영에서 닿지 않는 경로를 막는 방어 코드 | Begin의 Clear 셋은 운영에서 늘 빈 표시를 비우는 방어 코드다. 맞지 않는 '재설치' 주석도 그대로다. |
| <a id="c5-u0066"></a>C5-U0066 | 낮음 | 낮음 | 그대로 | CombatTornadoAppearance의 닿지 않는 조건과 파괴 직전 복원 | CombatTornadoAppearance는 바뀌지 않았고, FieldTornadoPresenter.Remove도 파괴 직전에 Reset을 그대로 부른다. |
| <a id="c5-u0099"></a>C5-U0099 | 낮음 | 낮음 | 그대로 | InspectUnit이 선택 변경 콜백이 이미 한 HUD 선택·강조 갱신을 다시 함 | InspectUnit이 CombatSelectionDisplay로 옮겨졌지만, 콜백이 이미 한 HUD 선택·강조 갱신을 바로 뒤에서 또 한다. |
| <a id="c5-u0108"></a>C5-U0108 | 낮음 | 낮음 | 그대로 | 갱신마다 같은 계산을 두 번 함(칸별 종류 수집, 층별 열린 변) | 칸별 종류 수집을 두 번 하는 것과 층별 열린 변 계산이 채우기·테두리에서 겹치는 것 모두 그대로다. |
| <a id="c5-u0109"></a>C5-U0109 | 낮음 | 낮음 | 그대로 | 셰이더가 덮어써 쓰이지 않는 재질 값과 늘 참인 검사 | 파일이 바뀌지 않았다. 덮어써지는 재질 값을 넣고 검사하는 것과 늘 참인 조건이 그대로 있다. |
| <a id="c5-u0110"></a>C5-U0110 | 낮음 | 낮음 | 그대로 | 호출자 없는 운영 멤버 FieldCellColorRenderer.CellAt | 주석은 사실대로 고쳐졌다. 하지만 Assets 전체 grep에서 FieldCellColorRenderer.CellAt을 부르는 곳이 여전히 없어 죽은 운영 멤버가 남아 있다. |
| <a id="c5-u0119"></a>C5-U0119 | 낮음 | 낮음 | 그대로 | CombatUnitInfo.Configure가 쓰지 않는 표 4개와 이미 받은 UI 서비스를 다시 받음 | Configure는 여전히 쓰지 않는 표 4개를 받아 null만 검사한다. ui도 BindUISystemService가 넣은 값을 다시 덮어쓴다. statuses 검사는 아직 Prepare 뒤에 있다. |
| <a id="c5-u0129"></a>C5-U0129 | 낮음 | 낮음 | 그대로 | 같은 프레임에 칸 변경과 전체 변경이 겹치면 GridOutline이 전체 Refresh를 두 번 함 | 동작이 바뀌지 않고 주석에 적히기만 했다. 같은 프레임에 칸 변경과 전체 변경이 겹치면 GridOutline에서 전체 Refresh가 두 번 돈다. |
| <a id="c5-u0130"></a>C5-U0130 | 낮음 | 낮음 | 그대로 | FieldOutlineRenderer.CanReuse의 설정 비교가 운영에서는 늘 참 | 조립이 CombatFieldDisplay로 옮겨졌지만 이곳도 readonly 설정 하나만 넘긴다. 그래서 운영에서 CanReuse는 늘 참인데 항목별 비교가 그대로 남아 있다. |
| <a id="c5-u0131"></a>C5-U0131 | 낮음 | 낮음 | 그대로 | 같은 칸 간격을 세 필드에 들고, 쓰이지 않는 운영 멤버가 남음 | 같은 칸 간격을 세 필드에 두는 것이 그대로다. FieldView.HighlightState와 FieldTrajectoryLine.IsShown도 아직 아무 곳에서 읽지 않는다. |
| <a id="c5-u0144"></a>C5-U0144 | 낮음 | 낮음 | 그대로 | FieldThornPresenter.ActiveCount·TryGet에 호출자가 없음 | 파일이 바뀌지 않았다. grep해도 ActiveCount·TryGet(cell, out GameObject)을 부르는 곳이 없다. |
| <a id="c5-u0145"></a>C5-U0145 | 낮음 | 낮음 | 그대로 | 가시 표시만 SetActive·SetParent 재진입 방어 기계를 따로 가짐 | 가시 표시에만 따로 있는 방어 장치가 그대로 남아 있다. _visibilityDepth, _pendingDestruction, _destroying, SetActiveSafely, CanContinue, 단계마다 하는 _released 재확인이다. |
| <a id="c5-u0158"></a>C5-U0158 | 낮음 | — | 풀림 | 호출자가 없는 Count/ActiveCount와 한 줄 전달 ResolveKind | ObjectViewSet·TerrainViewSet의 Count, FieldTrailPresenter.ActiveCount, 한 줄 전달 ResolveKind가 모두 지워졌다. |
| <a id="c5-u0165"></a>C5-U0165 | 낮음 | 낮음 | 그대로 | 구조물 HUD는 사실 그리기마다 내용 전체를 다시 씀(유닛 HUD의 GH3 캐시와 다름) | 유닛 HUD는 같은 사실이면 다시 쓰지 않지만(GH3), 구조물 HUD는 여전히 사실 그리기마다 내용 전체를 다시 쓴다. |
| <a id="c5-u0181"></a>C5-U0181 | 낮음 | 낮음 | 커짐 | 대여 5종이 같은 시작 조건·IsCurrent 식을 각자 복제하고, 칸이 이미 지키는 조건을 다시 검사함 | 아웃라인 대여가 더해져 시작 조건 사본과 IsCurrent 사본이 다섯 벌에서 여섯 벌로 늘었다. 칸이 이미 지키는 조건을 다시 검사하는 것도 그대로다. |
| <a id="c5-u0182"></a>C5-U0182 | 낮음 | 낮음 | 그대로 | 예상 체력 지우기와 표시 정리가 같은 경로에서 두 번 돎 | 예상 체력 지우기가 RenderFacts와 Apply에서 두 번 돌고, 풀 반환 때 ReleasePresentation이 ResetForPool과 OnDisable에서 두 번 도는 구조가 그대로다. |
| <a id="c5-u0183"></a>C5-U0183 | 낮음 | 낮음 | 커짐 | 재생/편집 분기 Destroy 도우미가 여러 벌이고, 외형 클래스의 도우미를 다른 기능이 빌려 씀 | 인라인 분기와 PlacementGhost의 빌려 쓰기가 남아 있다. 새 파일(CartoonModelMaterialFallback·CombatUnitActionCursorPresenter)에도 같은 Destroy 도우미·분기가 더 생겨 사본이 늘었다. |
| <a id="c5-u0192"></a>C5-U0192 | 낮음 | 낮음 | 그대로 | 쓰이지 않는 공개 멤버(TargetMode/Mode, HasRangeAnswer, IsShown, ItemKey) | TargetMode/Mode, HasRangeAnswer 두 벌, IsShown, ItemKey는 View·Factory·Tests grep에 읽는 곳이 없고, State도 파일 밖에서 읽지 않는다. |
| <a id="c5-u0206"></a>C5-U0206 | 낮음 | 낮음 | 그대로 | CombatMovementInteraction·범위 요청에 호출자 없는 멤버와 늘 같은 세대 검사가 남음 | 호출자 없는 FocusLost·CaptureLost·Snapshot과 결과를 바꾸지 못하는 수명·세대 비교가 그대로 남아 있다. |
| <a id="c5-u0222"></a>C5-U0222 | 낮음 | 낮음 | 그대로 | CombatMovementPlayback 중간 계층이 닿지 않는 검사만 더하고 취소 정리를 두 번 돌게 함 | 중간 계층과 Cancel의 Detach→CancelPresentation 이중 취소 경로가 그대로라 한 번의 취소에 이동 취소가 두 번 돈다. |
| <a id="c5-u0224"></a>C5-U0224 | 낮음 | 낮음 | 그대로 | 쓰이지 않는 멤버와 운영에서 닿지 않는 ObjectCreated 갈래 | Count는 여전히 읽는 곳이 없고, Core가 CombatObjectCreatedEvent를 만들지 않는데(운영 코드에 생성 호출 없음) 형식 표·순간 종류·소리·큐 갈래가 남아 있다. |
| <a id="c5-u0240"></a>C5-U0240 | 낮음 | 낮음 | 그대로 | 운영에서 닿지 않는 경로: 옛 사냥 표식 사건 소리 갈래, CombatMovementPresentation.Prepare(CombatResult) | Assets 운영 코드에서 new CombatHuntersMarkEvent를 찾으면 0건이고 시험 코드에서만 5건 나온다. 그런데도 소리 갈래가 남아 있다. Prepare(CombatResult) 오버로드도 여전히 호출자가 없다. |
| <a id="c5-u0241"></a>C5-U0241 | 낮음(기반 한계) | 낮음 | 그대로 | 연결 대기 SFX 칸이 null이라 오브젝트·추락·아이템·비행·피격·토네이도 예고 소리가 늘 None | 기반 한계(GlobalSystem의 SfxEvent에 칸 없음)는 그대로다. 추락 소리는 이제 UnitEvent.Fall을 쓰지만, 오브젝트·아이템·비행·피격·토네이도 예고 소리는 여전히 늘 None이다. base_limit 낮음으로 남긴다. |
| <a id="c5-u0248"></a>C5-U0248 | 낮음 | 낮음 | 그대로 | 운영에서 쓰이지 않는 완료 정책·멤버가 남아 있음 | CombatHitGate.IsActive는 이제 HasSource를 거쳐 조기 공격 쪽에서 쓰인다. 하지만 Fired·AnimationEnd 정책과 NotifyFired, ActiveCount, 4인자 FireLinear, Lease.Instance, Presenter.Clear, Resolve 오버로드, PrimaryTargetId는 여전히 운영 호출자가 없다. |
| <a id="c5-u0249"></a>C5-U0249 | 낮음 | 낮음 | 그대로 | 없어진 Ballista 관통 경로를 위해 일반화한 코드가 남아 있음 | 없어진 관통 경로를 위해 일반화한 다중 접촉 구조와 호출자가 하나뿐인 ContactProgress/WarnContact 계층이 그대로 남아 있다. |
| <a id="c5-u0276"></a>C5-U0276 | 낮음 | 낮음 | 줄어듦 | 쓰이지 않는 생성자·콜백 인자·속성(죽은 코드) | 3-float 커서 생성자와 eventTiming2·3 선택 인자는 순번 콜백 하나로 바뀌며 없어졌다. ended 인자·필드(늘 null)와 아무도 읽지 않는 CombatPieceContext.Version은 남았다. |
| <a id="c5-u0295"></a>C5-U0295 | 낮음 | 낮음 | 그대로 | 기차 경로·접촉 대상의 옛 '점 몸' API가 호출자 없이 남아 있음 | 파일이 바뀌지 않았다. Vector3 ContactTime·TryGetContactTime, Target(…, Vector3) 생성자, PointOf는 시험까지 포함한 grep에서 호출자가 없다. |
| <a id="c5-u0297"></a>C5-U0297 | 낮음 | 낮음 | 그대로 | 반응 Presenter는 항목마다 새로 만들어 한 번만 쓰는데 재사용용 장치를 품음 | Presenter는 Set Entry와 새 CombatHitMotionPiece 어디서든 쓰일 때마다 새로 만들어 한 번만 쓴다. 그런데도 재시작 검사(IsPlaying·_cleaning)와 아무도 읽지 않는 공개 Version을 그대로 품고 있다. |
| <a id="c5-u0309"></a>C5-U0309 | 낮음 | 낮음 | 그대로 | SelectedActorCells가 같은 ID로 사실 조회를 두 번 함 | 호출 자리가 CombatSelectionDisplay로 옮겨졌을 뿐 SelectedActorCells는 강조 갱신마다 같은 ID로 조회를 두 번 한다. |
| <a id="c5-u0318"></a>C5-U0318 | 낮음 | 낮음 | 줄어듦 | 호출자 없는 옛 입구가 남아 있다(Read·FindTimeError·3시각 생성자·Sound·KnownRule) | 다섯 멤버 중 Read·CombatClipData.FindTimeError·세 float 생성자는 지워지고 주석도 고쳐졌으나, CombatEventCues.Sound와 KnownRule은 여전히 호출자 없이 남아 있다. |
| <a id="c5-u0380"></a>C5-U0380 | 낮음 | 낮음 | 그대로 | 기능을 걷어 낸 뒤 남은 클래스·상수·인자(행동 상태 문구, 목표 상태 칸, traitVisuals) | CombatObjectiveDisplay의 doc만 실제에 맞게 고쳐졌다. 죽은 정적 클래스, 쓰지 않는 상수·메서드, traitVisuals 인자와 두 Factory의 전달은 그대로다. |
| <a id="c5-u0381"></a>C5-U0381 | 낮음 | 낮음 | 그대로 | 덱 카드 Hover 경로 끝이 빈 메서드라 사슬 전체가 헛일임 | CombatDeckHoverInput은 바뀌지 않았고 사슬 끝은 여전히 빈 메서드라 hover 경로 전체가 헛일이다. 클래스 doc도 옛 동작을 적고 있다. |

### C6 C#·Unity 구조 (20)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c6-01"></a>C6-01 | 낮음 | 낮음 | 줄어듦 | CombatInputSurfaces가 Unity 객체(씬 루트·UI)에 `?.`를 써서 파괴 판정을 거치지 못함 | UI 하위 참조의 ?.는 지워졌다. Sync와 정리 경로인 Hide에는 Unity 객체 _root에 대한 ?.가 아직 남아 있어, 파괴된 루트를 거르지 못하는 경로가 줄어든 채로 있다. |
| <a id="c6-02"></a>C6-02 | 낮음 | 낮음 | 그대로 | 정보창 HP 막대 감소 연출을 FixedUpdate·fixedDeltaTime으로 돌림 | HP Fill 연출은 아직 FixedUpdate에서 Time.fixedDeltaTime으로 돈다. 같은 패널의 슬라이드는 unscaledDeltaTime을 써서 시간 기준이 서로 다르다. |
| <a id="c6-03"></a>C6-03 | 낮음 | 낮음 | 그대로 | MainCamera 대체 정책이 세 곳에 있고 포인터만 Ray마다 Camera.main을 다시 조회 | MainCamera 대체 정책이 여전히 Factory 두 곳과 포인터 매퍼의 호출마다 대체 세 군데에 있어 카메라가 바뀌면 포인터만 새 카메라를 쓴다. |
| <a id="c6-04"></a>C6-04 | 낮음 | 낮음 | 그대로 | FieldUnitView.MortarWeapon이 읽을 때마다 GetComponent | 속성 접근마다 GetComponent를 부르고, 곡사 공격 한 번에 두 번 조회한다. 같은 파일의 clipEvents는 한 번만 찾는 관례와 여전히 다르다. |
| <a id="c6-05"></a>C6-05 | 낮음 | 낮음 | 그대로 | CombatSceneRoot의 문자열 반환 검사 함수가 예외를 던지는 하위 검증을 부름 | 문자열 반환 검사 함수가 여전히 예외를 던지는 하위 검증을 부른다. 그래서 에디터 delayCall 경로에서 경고 대신 예외가 나고 뒤 검사를 건너뛴다. |
| <a id="c6-06"></a>C6-06 | 낮음 | 낮음 | 그대로 | CombatBodyTextPool.Rental.Dispose가 멱등이 아님 | Rental.Dispose에 반환 깃발이 여전히 없어 두 번째 Dispose가 예외를 던진다. 파일은 바뀌지 않았다. |
| <a id="c6-07"></a>C6-07 | 낮음 | 낮음 | 그대로 | CombatTornadoLiftTarget struct의 불변식이 생성자에만 있어 default 값이 검사를 건너뜀 | 두 파일 모두 바뀌지 않았다. default 값은 검사를 건너뛰고, 소비자가 겹친 재검사를 한다. |
| <a id="c6-08"></a>C6-08 | 낮음 | 낮음 | 그대로 | CombatTornadoLiftPiece가 인자에서 온 null 설정을 InvalidOperationException으로 던짐 | target.Settings가 null이면 여전히 InvalidOperationException을 던진다. 같은 생성자의 다른 인자 검사는 ArgumentException이고, 형제 조각은 같은 null 설정에 ArgumentNullException을 쓴다. |
| <a id="c6-09"></a>C6-09 | 낮음 | 낮음 | 그대로 | CombatAttackMotion 생성자가 target·up 오류에도 nameof(duration)으로 던짐 | 파일이 바뀌지 않았다. target이 유한하지 않거나 up이 0이어도 여전히 duration 인자 오류로 알린다. |
| <a id="c6-10"></a>C6-10 | 낮음 | 낮음 | 줄어듦 | 이동 레인 집합·레인의 예외 규약·들여쓰기 어긋남 | 0열에서 시작하던 `/// <summary>` 줄은 Movement 폴더에서 없어졌다(grep으로 확인). ①(null 인자에 InvalidOperationException)과 ②(BeginPull 인자 검사 없음·문구 접두어 없음), Lane() 줄바꿈은 그대로 남아 있다. |
| <a id="c6-11"></a>C6-11 | 낮음 | 낮음 | 그대로 | 필드 부품(FieldCellFill·FieldArrowhead·FieldGridDecal)의 예외·IDisposable 관례가 다름 | FieldCellFill은 주석 한 줄, FieldGridDecal은 배치 계산만 바뀌었다. 예외 종류, Register null 검사, IDisposable 차이는 그대로다. |
| <a id="c6-12"></a>C6-12 | 낮음 | 낮음 | 그대로 | FieldOutlineRenderer만 config 재질 누락에 InvalidOperationException을 던짐 | 생성자가 config 재질 누락에 여전히 InvalidOperationException을 던진다. 형제 부품의 ArgumentException(nameof(config)) 관례와 다르다. |
| <a id="c6-13"></a>C6-13 | 낮음 | 낮음 | 그대로 | CombatBodyTextPool·Railway 경로에서 인자 문제에 InvalidOperationException이 섞임 | 같은 null 인자에 Rent와 Prewarm이 다른 예외를 던지고, Railway 쪽은 빈 인자 목록에 여전히 InvalidOperationException을 던진다. |
| <a id="c6-14"></a>C6-14 | 낮음 | 낮음 | 그대로 | 연출 값 생성자(CombatBodyTextData·CombatCameraSettingsData)의 예외 종류·인자 이름 어긋남 | 두 파일 모두 바뀌지 않아 예외 종류·인자 이름 어긋남이 그대로다. |
| <a id="c6-15"></a>C6-15 | 낮음 | — | 풀림 | CombatSceneDisplay 생성자 인자 검증이 절반만 됨 | 생성자가 필수 인자 17개를 모두 같은 규칙으로 검사한다. 그래서 _fieldBound를 만들 때 NRE가 나거나 Release에서야 NRE가 나던 경로가 없어졌다. |
| <a id="c6-16"></a>C6-16 | 낮음 | 낮음 | 그대로 | OrderedCombatPresentation.Play가 result·onFinished를 검사하지 않아 늦게 NRE로 드러남 | Play는 여전히 result·onFinished null 검사 없이 foreach와 늦은 finished(...) 호출에서 NRE가 난다. |
| <a id="c6-17"></a>C6-17 | 낮음 | 낮음 | 그대로 | CombatUnitPreparationLease 생성자는 인자를 검사하지 않으면서 Dispose는 null을 방어함 | 생성자는 여전히 ArgumentNullException 검사를 하지 않는다. 오히려 view null을 허용하는 분기가 더해졌고, Dispose의 null 방어와 계약이 엇갈리는 것도 그대로다. |
| <a id="c6-18"></a>C6-18 | 낮음 | 낮음 | 그대로 | 연출 입력 열거·형식 여럿에 한국어 XML doc이 없음 | 파일이 바뀌지 않아 연출 입력 열거들의 한국어 XML doc 누락이 그대로다. |
| <a id="c6-19"></a>C6-19 | 낮음 | 낮음 | 그대로 | Playback/Support 파일의 XML doc 누락·필드 배치 혼재·전체 이름 표기 | 지적한 네 파일의 문서 누락, 필드 배치 혼재, 전체 이름 표기가 모두 그대로다. |
| <a id="c6-20"></a>C6-20 | 낮음 | 낮음 | 그대로 | CombatSceneRoot·CombatDisplayTables의 Allman·공백 규칙 위반 | 지적된 Allman·공백 위반이 줄 번호만 바뀐 채 그대로 남아 있다. |

### C7 매 프레임 반복 (13)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c7-01"></a>C7-01 | 낮음 | 낮음 | 그대로 | 환경 호버 Refresh가 캐시 비교 전에 UI Raycast·칸 집기를 해서 커서가 멈춰도 매 프레임 돈다 | 조정자가 Tick 끝을 입력 분배기로 옮겼을 뿐, 커서가 멈춰도 매 프레임 UI Raycast와 칸 집기를 캐시 비교보다 먼저 한다. |
| <a id="c7-02"></a>C7-02 | 중간 | — | 풀림 | 같은 프레임·같은 포인터 좌표의 UI Raycast·칸 집기를 소비자마다 따로 반복 | 이 문제의 비용은 같은 프레임·같은 좌표로 EventSystem.RaycastAll이 4~5번 도는 것이었습니다. 지금은 모든 소비자가 거치는 단일 UI 판정 HitsUi가 프레임·좌표 단위로 답을 캐시해 한 번만 돕니다. 카메라 Ray로 칸을 집는 일은 아직 소비자마다 따로 하지만, 평면 교차 몇 번이고 할당도 없어 의미 있는 프레임 비용이 아닙니다. |
| <a id="c7-03"></a>C7-03 | 낮음 | 낮음 | 그대로 | 사건·걸음마다 모든 유닛·구조물·유지 이펙트·선택 정보창을 통째로 다시 그림 | 사건·걸음마다 모든 유닛·구조물 전체를 다시 그리고 유닛마다 HUD 예상 체력 쓰기를 하는 구조가 그대로다. |
| <a id="c7-04"></a>C7-04 | 낮음 | 낮음 | 그대로 | 경로 입력이 매 Tick 같은 마지막 응답을 다시 받아 초안·고스트를 다시 그리거나 빈 정리를 반복 | 이미 처리한 응답 번호를 거르는 장치가 아직 없다. 그래서 불허 구간에는 빈 CancelInputs가, 지정 중에는 같은 초안·고스트 다시 그리기가 매 Tick 반복된다. |
| <a id="c7-05"></a>C7-05 | 낮음 | 낮음 | 그대로 | 한 Tick 안에서 입력 사실(CombatInputFacts)을 11~12번 다시 조립 | 입력 분배로 옮긴 뒤에도 한 Tick 안에서 입력 사실을 7~8번 다시 조립한다(조정자 쪽 조건부 읽기는 따로 센다). 배경 조회 둘이 한 번 읽기를 함께 쓰게 된 것만 줄었다. 비용은 여전히 작다. |
| <a id="c7-06"></a>C7-06 | 낮음 | 낮음 | 그대로 | UnitHUDLayer가 카메라·유닛이 멈춰도 매 프레임 모든 HUD를 투영하고 선택 HUD를 선형 탐색 | 카메라·기준점이 그대로여도 매 프레임 HUD 수만큼 투영을 계산하고 선택 HUD를 선형 탐색한다. 위젯 alpha도 무조건 쓴다. 이름만 바뀌었을 뿐 구조는 같다. |
| <a id="c7-07"></a>C7-07 | 낮음 | 낮음 | 그대로 | 따라가는 이펙트·상태 설명창·몸 위 글자가 위치가 그대로여도 매 프레임 Transform을 씀 | 세 곳 모두 위치가 그대로여도 매 프레임 Transform이나 RectTransform에 쓴다. 바뀔 때만 쓰는 막이는 아직 없다. |
| <a id="c7-08"></a>C7-08 | 낮음 | 낮음 | 그대로 | 행동 표면 맞춤이 매 Tick 같은 값 비교 전에 참조 11개를 검사하고, 정보창 가용성 색을 비교 없이 다시 씀 | 매 Tick 참조 검사가 같은 값 비교보다 먼저 돌고, 정보창 가용성도 비교 없이 다시 쓰는 구조가 그대로다. |
| <a id="c7-09"></a>C7-09 | 낮음 | 낮음 | 그대로 | 토네이도 화살촉 점등 Tick이 할 일이 없어도 매 프레임 돌고, 표 조회와 꺼진 인스턴스 순회를 반복 | Update가 전투 내내 TickPulse를 부른다. TickPulse는 매 프레임 Settings로 GetTerrainVisual을 다시 조회하고 꺼진 인스턴스까지 돈다. 켜진 수에 따라 컴포넌트를 끄는 처리도 없다. |
| <a id="c7-10"></a>C7-10 | 낮음 | 낮음 | 그대로 | 걸음 레인이 전투 내내 쌓이고 재생이 없어도 매 프레임·매 사건 전부 훑음 | 레인은 _lanes에 넣기만 하고 지우지 않는다. 진행하는 걸음이 없어도 IsActive·AllowsOverlap·CanBegin·BeforeEvent·ResumeLeaps·Tick이 모든 레인을 훑는다. |
| <a id="c7-11"></a>C7-11 | 낮음 | 낮음 | 그대로 | 정착 대기 IsSettled가 매 프레임 남은 사건 전체를 O(n²)로 다시 훑음 | IsSettled가 그대로 _first부터 끝까지 훑고 아직 시작 안 한 후속마다 CanStart로 다시 훑는다(정착 대기 조각은 바뀌지 않음). |
| <a id="c7-12"></a>C7-12 | 낮음 | 낮음 | 그대로 | 보드·선로 등장 Tick이 이미 끝난 몸에도 매 프레임 위치를 다시 씀 | 끝난 몸을 건너뛰는 표시가 없어 진입 동안 같은 값을 계속 쓴다. 진입 때 한 번뿐이라 낮음 그대로다. |
| <a id="c7-13"></a>C7-13 | 낮음 | 낮음 | 그대로 | 사망 머묾 구간에 같은 최종 자세를 매 프레임 다시 씀 | 랙돌이 아닌 사망은 머묾 구간에서 tilt가 1로 고정되는데도 매 프레임 ApplyPose로 같은 최종 자세를 다시 쓴다. 디졸브 값은 바뀌므로 그 쓰기는 따로 필요하다. |

### C8 캡슐화 (13)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c8-01"></a>C8-01 | 중간 | — | 풀림 | CombatSceneInput이 부품을 구체 타입으로 내놓아 조정자가 Interaction 내부 단계·모드 깃발을 직접 조합 | 부품 속성이 모두 private이 되어 조정자·분배·결과 소비는 묶음 수준 입구만 부르고, 정보창 판단은 IsInputOwnerActive 하나에서 나오므로 새 전경 소유자가 생겨도 InspectUnit이 끼어드는 시나리오가 성립하지 않는다. 결과 단계 순서도 CombatResultConsumer 한 곳이 묶음 입구로만 조합한다. |
| <a id="c8-02"></a>C8-02 | 낮음 | 낮음 | 줄어듦 | 조정자·유닛 표시의 호출자 없는 시험 입구가 운영 null 갈래와 클립 Graph 우회 통로를 만든다 | 조정자 쪽 시험 입구와 runDeck/loading null 갈래가 걷혀, 늦은 F1 예외 시나리오는 더 이상 성립하지 않는다. 반면 호출자 없는 유닛 클립 입구(ClipChannel.Graph)는 남아 있어 대여 우회 통로가 그대로다. |
| <a id="c8-03"></a>C8-03 | 낮음 | 낮음 | 그대로 | 재생 지원 부품이 운영에서 늘 넘기는 의존을 시험 편의로 null 허용하고 첫 발동 때 예외 | 운영에서는 늘 넘기는 의존을 null로 받을 수 있는 구조가 그대로다. null이면 생성 때가 아니라 첫 해당 발동에서야 예외가 난다. |
| <a id="c8-04"></a>C8-04 | 낮음 | 낮음 | 그대로 | 시험 때문에 internal static으로 연 FieldCellFillStack.SetOpening | 운영에서는 클래스 안에서만 쓰는데 시험 때문에 internal static으로 열린 상태가 그대로다. |
| <a id="c8-05"></a>C8-05 | 낮음 | 낮음 | 그대로 | Buffer 쓰기 계약에 번호 없는 ClearPreview()가 호출자 없이 열려 소유자 번호 보호를 우회 가능 | 파일이 바뀌지 않았고 저장소에서 계약 오버로드 ClearPreview()의 외부 호출자는 여전히 없다(ArtifactDragController의 호출은 다른 Buffer 계약). |
| <a id="c8-06"></a>C8-06 | 낮음 | 낮음 | 그대로 | FieldUnitView가 '유일한 입구' 창구 밖에서 대여 시작 메서드를 internal로 열어 둠 | 창구를 거치지 않는 internal 대여 입구가 그대로 열려 있고, 밖에서 부르는 곳이 없는 BeginAttackPose·BeginReactionPose도 남아 있다. |
| <a id="c8-07"></a>C8-07 | 낮음 | 낮음 | 그대로 | FieldUnitView가 Hud·HealthLabel을 그대로 내줘 남이 표시 기억(_shownHealth·_hudFacts)을 우회해 씀 | Hud·HealthLabel을 그대로 내주고, 준비 대여와 Presenter가 표시 기억(_shownHealth·_hudFacts)을 거치지 않고 직접 쓰는 구조가 그대로다. |
| <a id="c8-08"></a>C8-08 | 낮음 | 낮음 | 그대로 | 바깥 클래스 둘이 FieldUnitView 자식 Renderer·Collider를 각자 스냅숏 떠서 끄고 켬 | FieldUnitView의 자식 표시 상태를 바깥 두 클래스가 각자 끄고 켜는 구조가 그대로다. 표시 주인은 이 숨김을 모른다. |
| <a id="c8-10"></a>C8-10 | 낮음 | 낮음 | 그대로 | 공격 전략 맥락이 구체 투사체 풀 전체(Tick·Clear·Release)를 내줌 | 전략 맥락은 아직 구체 풀 전체를 내준다. 그래서 전략이 Tick·Clear·Release를 부를 수 있고, 진행·정리는 Flights만 한다는 계약과 어긋난다. |
| <a id="c8-11"></a>C8-11 | 낮음 | 낮음 | 그대로 | 반응 목록이 내부 Presenter 객체를 취소 손잡이로 밖에 내줌 | BeginHit/BeginDeath는 지금도 내부 CombatUnitReactionPresenter를 그대로 돌려준다. 처리기는 그것을 Cancel(id, presenter)의 표지로만 쓰지만, 받은 쪽이 Tick·Cancel·Release를 직접 부를 수 있다. |
| <a id="c8-12"></a>C8-12 | 낮음 | 낮음 | 그대로 | 큐·부속 클립 목록이 원시 배열을 IReadOnlyList로 그대로 반환 | CombatClipData는 바뀌었지만 Copy는 여전히 원시 배열을 IReadOnlyList로 돌려주고, CombatEventCues의 방식 혼재도 그대로다. |
| <a id="c8-13"></a>C8-13 | 낮음 | 낮음 | 그대로 | 정보창 내부에서만 쓰는 Show·ShowCombat·AnimateTransition이 internal | Show·ShowCombat은 ShowInspection 안에서만, AnimateTransition은 Update에서만 불린다. 그런데 셋 다 여전히 internal이다. |
| <a id="c8-14"></a>C8-14 | 낮음 | — | 풀림 | CombatSceneInputFactory 내부 조립 단계 셋이 internal | 내부 조립 단계 셋이 모두 private으로 줄어 같은 어셈블리에서 부분 조립할 수 없다. |

### C9 수명·자원 (6)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c9-01"></a>C9-01 | 낮음 | — | 풀림 | Detach의 씬 루트 풀기(UnbindRoot)만 오류 수집 밖에 있어, 실패하면 남은 정리와 루트 구독 해제를 모두 건너뜀 | UnbindRoot가 실패해도 이제 실패를 모은 뒤 Abort·StopInput·입력 Release·_connection=null·Display.Release가 이어진다. 루트 구독도 finally로 풀리므로 다음 Attach가 거절되는 시나리오가 사라졌다. |
| <a id="c9-02"></a>C9-02 | 낮음 | 낮음 | 그대로 | CellPrefab 모드 셀의 ColorHandle을 돌려주지 않아 전역 ColorSystem에 전투마다 쌓임 | 주석 낱말만 바뀌었고 FieldCellPool·ColorSystem 해제 동작은 바뀌지 않았다(ColorSystem에는 ColorTypeKey만 추가). CellPrefab 모드에서 셀 색 손잡이가 전투마다 전역 ColorSystem에 쌓이는 길이 그대로다. 운영 표가 GridOutline이라 설정에 따라서만 열리는 길이어서 낮음을 유지한다. |
| <a id="c9-03"></a>C9-03 | 낮음 | 낮음 | 줄어듦 | 선로 발동마다 EffectScope를 새로 받아 이펙트 시스템에 쌓임 | Scope를 표시 수명에 한 번만 받게 바뀌어 쌓임(주된 실패)은 없어졌다. 창구를 쓰지 않는 따로 된 정리 경로는 남아 있다. |
| <a id="c9-04"></a>C9-04 | 낮음 | — | 풀림 | 무너짐 끝 콜백 하나가 실패하면 그 외형과 FinishAll의 나머지 외형이 풀로 돌아가지 않음 | 콜백 하나가 실패해도 나머지 외형은 모두 풀로 돌려준다. 실패는 모아서 드러낸다. 그래서 나머지가 추적되지 않은 채 활성으로 남는 시나리오는 더는 성립하지 않는다. |
| <a id="c9-05"></a>C9-05 | 낮음 | 낮음 | 줄어듦 | 표시 조립 실패 정리가 일부만 되돌려 가시밭 루트·선로·지형 등 이미 만든 소유 객체를 남김 | 실패 정리는 여전히 일부만 되돌리지만, 검토자가 대안으로 제시한 대로 remarks가 남는 객체와 씬 언로드 회수를 받아들인 한계로 밝혀 오해 소지가 줄었다. |
| <a id="c9-07"></a>C9-07 | 낮음 | — | 풀림 | ReleasePresentation이 한 칸 정리 중 예외가 나면 나머지 칸 정리를 건너뛴 채 풀로 돌아감 | ReleasePresentation이 각 단계를 CleanupErrors.RunAll로 돌린다. 그래서 한 칸의 취소 콜백이 던져도 나머지 칸이 정리된다. |

### C10 정확성·오류 처리 (19)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c10-01"></a>C10-01 | 중간 | — | 풀림 | 카메라 이동·줌 뒤 포인터를 다시 집지 않아 공격·배치 호버는 옛 칸에 남고, 클릭은 다른 칸을 고름 | 카메라가 실제로 움직이거나 줌이 보간되는 프레임(과 그 다음 한 Tick)에 마지막 포인터 좌표로 PointerMoved를 다시 보내므로, 공격 조준 호버·배치 고스트·유닛 호버가 지금 포인터 아래 칸을 다시 집습니다. 미리보기 칸과 클릭 칸이 어긋나는 시나리오가 더는 성립하지 않습니다. |
| <a id="c10-02"></a>C10-02 | 중간 | 중간 | 그대로 | Unity 프레임 콜백(FieldView.LateUpdate·이펙트 따라가기·토네이도 맥동·HUD 투영)의 예외가 F1 오류 경계에 닿지 않음 | FieldView.FlushPending은 옛 판과 같고, 이펙트 받침·토네이도 맥동·HUD 투영 콜백 어디에도 오류 경계 보고가 더해지지 않았다. 새로 생긴 CombatUnitRagdoll.LateUpdate(266-270)도 같은 방식이지만 하는 일이 작아 심각도는 그대로 중간이다. |
| <a id="c10-03"></a>C10-03 | 중간 | — | 풀림 | Nexus 착지 이펙트가 저절로 끝나기를 기한 없이 기다려, 끝나지 않는 프리팹이면 전투 시작이 영구히 멈춤 | 완료 조건이 `!impactLive \|\| impactWaitOver`로 바뀌었다. 그래서 FallingRocks가 Looping이거나 끝 알림이 빠져도 착지 3초 뒤 진입이 끝나고 남은 이펙트는 Cancel이 거둔다. 진입이 영원히 기다려 입력·명령·포기가 멈추던 시나리오는 더는 성립하지 않는다. |
| <a id="c10-06"></a>C10-06 | 중간 | — | 풀림 | 보드가 올라오는 동안 숨기지 않은 보드 위 물체(오브젝트·장애물·꺼진 Nexus)가 타일보다 먼저 떠 보임 | 진입 준비가 이제 장애물 같은 구조물과 그 HUD, 낙하가 꺼진 Nexus, 필드 Object를 보드 단계 동안 숨긴다. 이들은 타일이 다 올라온 뒤 구조물 → Object → Nexus 순서로 보인다. 그래서 화약통·지뢰를 두거나 nexusDrop을 꺼도 물체가 빈 구멍 위에 떠 보이지 않는다. |
| <a id="c10-07"></a>C10-07 | 중간 | — | 풀림 | 커서 스냅이 옮길 칸 중심이 UI 아래·화면 밖인지 보지 않아 커서를 UI 밑으로 끌어당김 | 칸 중심이 UI 밑이면 Warp하지 않고 _snapped=cell로 기억해 같은 칸의 보이는 부분에서 커서가 자유로우므로 클릭할 수 있다. 화면 밖 중심은 pixelRect 검사로 걸러져 커서가 창 밖으로 나가지 않는다. |
| <a id="c10-08"></a>C10-08 | 중간 | — | 풀림 | FieldView는 Y축 회전 필드를 허용하는데, GridOutline 표시기는 경고 한 번만 남기고 강조를 전혀 그리지 않음 | GridOutline 모드에서 Cells Root가 돌아가 있으면 이제 FieldOutlineRenderer.Build가 예외를 던지고, FieldView.Build가 만든 것을 거둔 뒤 다시 던진다. 툴팁도 모드에 맞게 고쳐졌다. 그래서 '경고 한 번만 남기고 강조가 조용히 안 그려지는' 실패 시나리오는 더는 생기지 않는다(FieldGridDecal의 경고 경로는 이 Build 검사 뒤라 GridOutline에서는 닿지 않음). |
| <a id="c10-09"></a>C10-09 | 낮음 | — | 풀림 | 튜토리얼 제한 검사가 입구마다 달라, 배치 진입은 안내 없이 무시되고 켜 둔 공격 모드는 빠져나갈 수 없음 | 배치 진입이 Router로 넘어가 튜토리얼 막힘 보고를 받는다. 옛 판의 Cancel·AttackToggle 앞 RejectTutorialAttackMode가 빠지거나 뒤로 옮겨져, 제한 아래서도 켜 둔 공격 모드를 끌 수 있다. |
| <a id="c10-10"></a>C10-10 | 낮음 | — | 풀림 | 같은 결과에서 한 유닛의 두 번째 이동 경로는 이동 시작 알림(이동 효과음)을 받지 못함 | 결과 단위 유닛 집합으로 이동 시작 알림을 한 번만 보내던 경로가 없어지고, 걸음 레인이 보간 시작 때 걸음마다 소리를 내므로 같은 유닛의 두 번째 경로가 소리를 잃는 시나리오가 성립하지 않는다(밀림 걸음은 설계상 소리 없음). |
| <a id="c10-11"></a>C10-11 | 낮음 | — | 풀림 | 추락 사망 갈래가 쓰지도 않는 반응 설정이 있어야 열림 | TryBeginDeath가 이제 HasFallen을 반응 설정 확인보다 먼저 보고 BeginFallDeath로 가므로, 반응 값이 없는 유닛도 추락 연출을 낸다. |
| <a id="c10-12"></a>C10-12 | 낮음 | — | 풀림 | 공격자 표시가 없을 때 코드 모션 시작 결과가 종류마다 다름(곡사는 false, 직선·근접은 예외) | 이제 TryBeginMotion이 공격자 표시가 있는지 먼저 확인한다. 덮어쓰기 경로에서도 확인하므로 표시가 없으면 종류와 관계없이 전략에 가기 전에 false를 돌려준다. 그래서 직선·근접만 예외를 던지는 차이가 더는 생기지 않는다. |
| <a id="c10-13"></a>C10-13 | 낮음 | — | 풀림 | ForPresentation이 끌어오기 도착 사건을 다시 만들 때 DamageCause·MovementCause·Tornado 정보를 잃음 | 끌어오기 도착 갈래가 새 사건을 만들 때 이제 위치 보존 갈래(276-277)와 같은 인자를 모두 넘긴다. 그래서 피해 원인·이동 원인·토네이도 칸이 기본값이나 null로 바뀌는 일이 없다. |
| <a id="c10-14"></a>C10-14 | 낮음 | — | 풀림 | 배치 Drag에서 칸이 바뀌었는데 미리보기를 못 내면 옛 요청 번호가 남아, 옛 칸의 답이 새 칸에 칠해짐 | 새 칸에서 미리보기를 못 내면 옛 요청 번호를 0으로 놓아, 옛 칸의 늦은 답이 PreviewAnswered 첫 검사에서 걸러진다. |
| <a id="c10-15"></a>C10-15 | 낮음 | 낮음 | 그대로 | 흔적 겹침 차례(StackIndexOf)가 놓는 순서와 다른 흔적의 지난 Fact에 의존해 같은 높이로 겹침 | StackIndexOf는 아직 다른 인스턴스의 지난 Fact를 보고 차례를 센다. Render는 Tiles 순서대로 하나씩 놓고, Show는 옮긴 흔적만 다시 계산한다. 그래서 같은 높이로 겹치는 경로가 그대로 있다. |
| <a id="c10-16"></a>C10-16 | 낮음 | — | 풀림 | EntityViewSet 종류 교체 중 대여가 실패하면 null View 항목이 남고 대여 수가 두 번 줄어듦 | _rent가 던져도 null View 항목이 남지 않는다. 그래서 End가 다시 반환하거나 TryGet이 null을 돌려주는 일이 없고, 대여 수가 두 번 줄지도 않는다. |
| <a id="c10-17"></a>C10-17 | 낮음 | — | 풀림 | unitHudPrefab 누락을 경로마다 다르게 처리함(준비는 예외, 사실 그리기는 HUD 없이 조용히 진행) | HUD 프리팹이 빠졌을 때 조용히 건너뛰는 길이 없어졌다. 운영과 미리보기 모두 진입 검사에서 막힌다. |
| <a id="c10-18"></a>C10-18 | 낮음 | — | 풀림 | 진입 중에는 토네이도 생성 전환이 진행되지 않아, 보드 완성 뒤에도 진입이 끝날 때까지 크기가 0 | 진입 중 남은 단계에서 토네이도 생성 전환이 진행된다. 그래서 보드가 완성된 뒤 크기 0으로 남는 일이 없다. |
| <a id="c10-20"></a>C10-20 | 낮음 | — | 풀림 | 특성이 4개 이상이면 첫 번째 때만 경고하고, 이후로는 조용히 잘라 표시 | 특성 칸이 고정 배열 3개에서 출처별로 필요한 만큼 늘어나는 AbilitySection으로 바뀌었다. 그래서 4개째부터 조용히 잘리는 경로가 없다. |
| <a id="c10-21"></a>C10-21 | 낮음 | — | 풀림 | 큐 시점 사용 판정에서 플래시만 IsOn을 보고, 세기 0 흔들림은 '사용'으로 셈 | UsesAnchor가 흔들림도 IsOn(세기>0)일 때만 세도록 바뀌었다. 그래서 세기 0 흔들림만 Cue에 둔 피격 칸은 더 이상 거절되지 않는다. |
| <a id="c10-22"></a>C10-22 | 낮음 | — | 풀림 | 테두리 정렬 순서의 상한 검사가 낙석 느낌표(90)가 아니라 궤적(100)만 봄 | 생성자가 마지막 테두리 순서를 RockfallWarningMark(90)와 비교하게 바뀌었다. 이제 테두리가 느낌표에 닿으면 구성 때 예외가 나서 실패 시나리오가 성립하지 않는다. |

### C11 핫패스 GC (17)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c11-01"></a>C11-01 | 낮음 | 낮음 | 그대로 | 이동 모드가 아닐 때 매 Tick 부르는 이동 범위 Clear가 일찍 돌아가는 경로에서도 캡처 환경을 매번 만든다 | Clear가 IsEmpty로 일찍 돌아가도 캡처 지역이 메서드 최상위 블록에 있어 진입 때 캡처 객체가 만들어지는 구조가 그대로다. |
| <a id="c11-02"></a>C11-02 | 중간 | 낮음 | 줄어듦 | 유지 이펙트 맞추기가 Render마다 필드 유닛마다 상태 읽기를 새로 할당하고, HUD도 같은 유닛에 또 할당한다 | 유지 이펙트 출처와 HUD가 모두 사실 객체 참조로 캐시하게 바뀌었다. 그래서 Render마다 필드 유닛 전부(×2)를 다시 읽던 낭비는 없어졌다. 다만 사실이 바뀐 유닛 하나에는 HUD·유지 이펙트·모습 판정이 Capture를 따로 2~3번 부른다. 남은 것은 국소적인 중복 할당이라 낮음으로 본다. |
| <a id="c11-03"></a>C11-03 | 낮음 | 낮음 | 그대로 | 끌어당김 걸음 보간 중 Core 사실 객체(CharacterCombatFacts)를 매 프레임 다시 만든다 | TickPull은 여전히 보간하는 프레임마다 At()으로 새 CharacterCombatFacts와 칸 배열을 만든다. next는 _pullIndex가 바뀔 때만 달라지는데 필드에 들고 있지 않는다. |
| <a id="c11-04"></a>C11-04 | 낮음 | 낮음 | 그대로 | 환경 설명 호버가 칸이 바뀔 때마다 ReadTornadoAreas(LINQ·새 객체)를 2~3번 부르고 인터페이스 foreach 4개로 열거자를 박싱한다 | 칸이 바뀔 때 ReadTornadoAreas가 Build·Refresh(클릭 시 Collect까지) 2~3번 불리고 Build의 foreach 넷은 그대로 인터페이스 열거다. |
| <a id="c11-05"></a>C11-05 | 낮음 | 낮음 | 줄어듦 | 호버·범위 미리보기 응답마다 칸 목록을 ToArray로 복사해 표시에 넘기지만 받는 쪽은 한 번 훑고 끝난다 | 공격 조준 쪽 ToArray 복사는 그대로다. 유닛 호버 쪽은 이제 복사본을 답 캐시에 보관하므로 복사에 쓰임이 생겼다. |
| <a id="c11-06"></a>C11-06 | 낮음 | 낮음 | 그대로 | 공격 조준 표시가 응답마다 인터페이스 foreach로 열거자를 박싱하고 끌어오기 도착 칸 배열을 새로 만든다 | 조준 응답 경로의 인터페이스 foreach와 한 칸 배열 생성이 그대로 남아 있다. |
| <a id="c11-07"></a>C11-07 | 낮음 | — | 풀림 | 열린 변 계산이 호버 갱신마다 인터페이스 foreach로 열거자를 여러 번 박싱한다 | 파일 안의 foreach가 모두 IReadOnlyList 인덱스 for로 바뀌었다(grep으로도 foreach 없음). 그래서 호버 갱신 때 열거자 박싱이 더는 생기지 않는다. |
| <a id="c11-08"></a>C11-08 | 낮음 | 낮음 | 그대로 | 타격·사건마다 인터페이스 컬렉션을 foreach로 돌아 열거자를 박싱한다 | 해제할 때마다, 그리고 사건마다 인터페이스 컬렉션을 foreach로 돌아 열거자를 박싱하는 부분이 그대로다. |
| <a id="c11-09"></a>C11-09 | 낮음 | 낮음 | 그대로 | 특성 프로필 조회 Get이 IReadOnlyList 필드를 foreach로 돌아 호출마다 열거자를 할당한다 | Get이 아직 인터페이스 열거로 돌아 호출마다 열거자를 할당하고, 호출하는 쪽도 그대로다. |
| <a id="c11-10"></a>C11-10 | 낮음 | 낮음 | 그대로 | 기차 경과 콜백 Advance가 매개변수 캡처 람다 때문에 매 프레임 캡처 환경을 할당한다 | FieldRailwayPresenter.Tick이 매 프레임 _advanced를 부른다. Advance 안의 사망 완료 람다는 매개변수 group·version을 캡처하므로, 접촉이 없는 프레임에도 메서드에 들어갈 때마다 캡처 환경이 할당된다. |
| <a id="c11-11"></a>C11-11 | 낮음 | 낮음 | 그대로 | 조각 실행 Tick의 _tickSnapshot.AddRange(_runs)가 Mono에서 매 프레임 임시 배열을 할당한다 | Tick이 여전히 AddRange로 스냅숏을 채워 Mono에서 실행이 있는 프레임마다 임시 배열이 생긴다. |
| <a id="c11-12"></a>C11-12 | 낮음 | 낮음 | 그대로 | 사건마다 도는 HUD 다시 쓰기·구조물 피해 숫자에서 문자열·박싱·대리자를 할당한다 | 할당하는 모양이 그대로다. 사실이 바뀐 유닛에서만 도는 사건 단위 경로라 낭비는 작다. |
| <a id="c11-13"></a>C11-13 | 낮음 | 낮음 | 그대로 | 사건·결과 끝마다 흔적 이름 문자열을 새로 만들고, 토네이도 자리 계산마다 Footprint 목록을 새로 할당한다 | Apply마다 흔적 이름 문자열을 새로 만든다. BodyPosition도 부를 때마다 Footprint로 새 List를 만든다. 사건 빈도라 낮음 그대로다. |
| <a id="c11-14"></a>C11-14 | 낮음 | 낮음 | 그대로 | 피격·사망 반응 하나마다 Entry·Presenter와 캡처 람다 여러 개를 새로 만든다 | 반응을 시작할 때마다 Entry, 그 안의 new CombatUnitReactionPresenter, 인자를 캡처한 시작 람다, 완료·불가 람다 두 개가 여전히 새로 할당된다. |
| <a id="c11-15"></a>C11-15 | 낮음 | 낮음 | 그대로 | 몸 글자 표시 하나마다 Entry·Rental과 추적 대리자 두 겹을 새로 만든다 | Show 한 번마다 Entry·Rental 할당과 TrackBody의 두 겹 클로저(presence 람다 + 감싼 람다)가 그대로 남아 있다. |
| <a id="c11-16"></a>C11-16 | 낮음 | 낮음 | 그대로 | 결과마다 짜는 특성 피드백 계획이 중첩 루프 안에서 캡처 람다 LINQ를 반복한다 | 세 파일 모두 바뀌지 않았다. 결과마다 짜는 계획이 중첩 루프 안에서 캡처 람다와 LINQ로 할당하는 것도 그대로다. |
| <a id="c11-17"></a>C11-17 | 낮음 | 낮음 | 그대로 | 직선 투사체 발사마다 경고용 대상 이름을 enum.ToString으로 미리 만든다 | 직선 발사 경로가 정해질 때마다 enum.ToString 문자열을 미리 만든다. 이 문자열은 경고를 낼 때만 쓰인다. |

### C12 주석·문서 (25)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="c12-01"></a>C12-01 | 낮음 | — | 풀림 | 운영 코드 주석이 저장소에 없는 시험·시험 도우미·리플렉션 계약을 근거로 든다 | 없는 시험·도우미·리플렉션 계약을 근거로 들던 주석이 지워졌다. 순서 제약은 '시험 없음'으로 바르게 적혔다. V 아래를 grep해도 옛 이름이 나오지 않는다. |
| <a id="c12-02"></a>C12-02 | 낮음 | 낮음 | 그대로 | 입력 묶음 설명의 개수('여덟')·목록·조건이 지금 12개 객체 구성과 다름 | 개수가 '여덟'에서 '열둘'로 고쳐졌지만 그새 묶음이 14개로 늘어 다시 어긋났다. 목록·'세 표면' 같은 설명도 여러 곳에서 틀린 채다. 타일 높이 주석은 이제 CombatFieldDisplay 생성 위로 옮겨져 그 부분만 고쳐졌다. |
| <a id="c12-03"></a>C12-03 | 낮음 | — | 풀림 | 조정자 주석이 지운 '타임라인 Core 직접 조회'와 옛 동작을 설명함 | 타임라인·Core 직접 조회 문구와 쓰이지 않던 CombatSystem using이 지워졌다. 함께 지적된 Refresh·Services·SceneRoot·Connection 요약도 지금 동작에 맞게 고쳐졌다. |
| <a id="c12-04"></a>C12-04 | 낮음 | 낮음 | 줄어듦 | 유닛 표시(Display/Unit) 요약이 지금의 의존·책임·대여 방식과 다르고 고아 XML doc이 남음 | 지적한 (1)~(5)는 대부분 고쳐졌다. 다만 대여 칸의 채널 목록이 새로 생긴 아웃라인 칸을 빠뜨려, 같은 종류의 낡은 목록이 같은 자리에 남았다. |
| <a id="c12-05"></a>C12-05 | 낮음 | 낮음 | 줄어듦 | 없는 타입 이름 'ColorScop'으로 색 손잡이를 설명하고 칸 부모 설명이 틀림 | V/ 전체에서 'ColorScop' 주석이 사라졌고 FieldCellFill의 부모 설명도 바로잡혔다. 옛 이름은 필드 _colorScopes에만 남았다. |
| <a id="c12-06"></a>C12-06 | 낮음 | — | 풀림 | PlacementGhost '유효 색 하나만'과 ICombatBattleDisplay.Refresh '동적 값만' 설명이 지금 동작과 다름 | 두 설명이 지금 색 선택과 다시 그리기 범위에 맞게 고쳐졌다. |
| <a id="c12-07"></a>C12-07 | 낮음 | — | 풀림 | FieldView·FieldOutlineRenderer의 메서드 XML doc이 엉뚱한 메서드 위에 붙음 | 두 파일 모두 doc·주석이 해당 메서드 바로 위로 옮겨져 설명이 메서드와 맞는다. |
| <a id="c12-08"></a>C12-08 | 낮음 | — | 풀림 | CombatMovementPresentation의 BeginStep 설명이 _placements 필드 위에 붙음 | BeginStep 설명이 BeginStep 위(55줄)로 옮겨졌다. _placements 필드에는 맞는 일반 주석이 붙어 있다. |
| <a id="c12-09"></a>C12-09 | 낮음 | — | 풀림 | 이동 경로 부품 설명은 '그림자를 설정하지 않음'이지만 화살촉은 그림자를 켬 | remarks가 '띠와 같게 화살촉도 그림자를 켬(shadows: true)'으로 바뀌어 Build·FieldArrowhead 동작과 맞는다. |
| <a id="c12-10"></a>C12-10 | 낮음 | — | 풀림 | FieldView 강조 우선순위·묶음 수 주석이 원본 층 표(FieldHighlightLayers.Rows)와 어긋남 | 주석에서 우선순위 사슬 사본과 틀린 개수가 지워지고 Rows를 원본으로 가리키게 바뀌었다. 지금 묶음 목록도 Header들과 맞는다. |
| <a id="c12-11"></a>C12-11 | 낮음 | — | 풀림 | 지형·선로·오브젝트 표시의 예외 접두사·안내 문구·요약이 실제 상황과 다름 | 세 곳(예외 접두사, Install 안내 문구, 검증 요약) 모두 지금 동작과 맞게 고쳐졌다. |
| <a id="c12-12"></a>C12-12 | 낮음 | — | 풀림 | 재생기 분리 뒤에도 예외 태그·프레임 순서 설명이 옛 Presenter 기준으로 남음 | 예외 태그가 실제 클래스명으로 바뀌었고, 조각 재생기는 자기 정리 문구를 쓰며, TickPresentation·실행 목록 설명이 실제 순서(외형·몸 글자 포함)와 맞는다. |
| <a id="c12-13"></a>C12-13 | 낮음 | — | 풀림 | 공격 전략으로 옮긴 예외 메시지에 옛 클래스 태그 [CombatAttackPlayback]가 남음 | 세 전략 파일의 예외 태그가 실제로 던지는 클래스 이름으로 바뀌었다. 전략 파일에서 [CombatAttackPlayback] 태그를 찾으면 0건이다. |
| <a id="c12-14"></a>C12-14 | 낮음 | 낮음 | 줄어듦 | 재생 조정·국면 전환·필드 창구 문서가 지금 코드와 다르거나 문장이 깨짐 | 깨진 요약·phaseTransition·PhaseTransitionPresenter(삭제)·FieldEventPorts 목록은 고쳐졌다. Presenter 생성자 문서에는 선택 인자 수와 ports 목록 어긋남이 작게 남았다. |
| <a id="c12-15"></a>C12-15 | 낮음 | — | 풀림 | CombatFactEventHandler 요약이 처리하지 않는 '구조물'을 반영 대상에 적음 | 요약에서 구조물을 반영 대상에서 빼고 구조물 처리기를 가리키게 고쳤고, Create 설명에도 구조물 칸이 들어갔다. |
| <a id="c12-16"></a>C12-16 | 낮음 | — | 풀림 | ApplyFacts 계약은 '표시 사실 반영'이지만 낙석·토네이도 처리기는 그 단계에서 소리·큐를 냄 | ApplyFacts 계약 문서에 낙석·토네이도의 소리·큐 부수 효과 허용이 명시돼 구현과 계약이 어긋나지 않는다. |
| <a id="c12-17"></a>C12-17 | 낮음 | — | 풀림 | 이펙트 부품 주석의 개수('넷'·'셋')와 사망 유지 숨김 설명이 코드와 다름 | 세 주석이 지금 속성 수·하는 일·Collect의 IsDead 제외 동작과 맞게 고쳐졌다. |
| <a id="c12-18"></a>C12-18 | 낮음 | — | 풀림 | 조각(Pieces) 문서가 Hit 내는 조각·ForAction 용도·새 채널 절차·흔들림 판정 위치를 틀리게 적음 | 지적한 네 문서가 모두 지금 사용처·절차에 맞게 고쳐졌다. CueComposer 116줄과 RoarEventHandler 102줄에서 CanShake를 실제로 확인했다. |
| <a id="c12-19"></a>C12-19 | 낮음 | 낮음 | 줄어듦 | 입력 계약(Input/Contracts) 문서가 지금 사용처와 다르거나 문장이 깨짐 | 네 항목 중 사용처·깨진 문장은 고쳐졌고, ICombatPointerMapping의 이력용 Playback.Ordering using만 남았다(ICombatEventPlayback은 cref가 아닌 <c>로만 언급). |
| <a id="c12-20"></a>C12-20 | 낮음 | 낮음 | 줄어듦 | 행동 버튼 계약은 복귀 슬롯을 '폐지·호출 안 함'이라 적지만 지금은 이동 되돌리기로 호출됨 | '폐지·호출 안 함'이라는 틀린 설명은 사라졌다. 남은 것은 옛 인자 이름 returnToCheckpoint 하나이고, doc이 그 사실을 밝혀 둔다. |
| <a id="c12-21"></a>C12-21 | 낮음 | — | 풀림 | 입력 소유자·선택 취소 설명이 실제 어댑터·루프 동작보다 좁게 적힘 | 두 설명이 이제 어댑터의 Poll 무효화와 SelectCanceled의 실제 단계(넘겨받기 무효화→경로→Drag→유닛 호버)를 적는다. |
| <a id="c12-22"></a>C12-22 | 낮음 | — | 풀림 | 이동 명령·미리보기의 Cell 설명('이동 목표 칸')이 경로만 넣는 지금 생성 방식과 다름 | 명령·종류·미리보기의 Cell 설명이 모두 '배치는 칸, 이동은 경로'로 고쳐져 지금 생성 방식과 맞다. |
| <a id="c12-23"></a>C12-23 | 낮음 | — | 풀림 | 재사용한 값 검사 함수의 오류 문구가 실제 검사 값과 맞지 않고 '값 조건은 여기 한 곳' 요약이 틀림 | 보드 등장 깊이와 글자 간격이 칸 이름을 받는 공용 규칙으로 검사돼 틀린 문구가 나오지 않는다. 클래스 요약도 ValueRules를 원본으로 적도록 고쳐졌다. |
| <a id="c12-24"></a>C12-24 | 낮음 | — | 풀림 | 큐 규칙·큐 자리·투사체 소리 주석이 지금 규칙표와 어긋남 | 지적한 주석 넷(요약·remarks·큐 자리·투사체 소리)이 모두 지금 규칙표와 연결 상태에 맞게 고쳐졌다. |
| <a id="c12-26"></a>C12-26 | 낮음 | 낮음 (C6) | 그대로 | 보드 등장 모드 이름 오타 Coner(Corner)가 열거·직렬화 칸·툴팁·메서드 이름에 퍼짐 | Coner 오타가 열거값·직렬화 칸·툴팁·메서드에 그대로 남아 있다. 주석이 아니라 식별자 관례 문제라 주된 기준은 C6으로 본다. |

### G1 (3)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="g1-01"></a>G1-01 | 낮음 | 낮음 (C9) | 그대로 | 취소 정리가 조각 실행(서는 자리)을 반응(자세)보다 먼저 되돌려, 서는 자리에서 맞던 수호자가 집 자리가 아니라 서는 자리에 남는다 | CancelAll은 여전히 등록 순서라 조각(서는 자리) 복원이 반응(자세) 복원보다 먼저 돌고, RestoreStand가 남은 자세를 먼저 접지 않아 수호자가 서는 자리에 남는 시나리오가 그대로다(ApplyStand의 자세 검사는 취소 경로와 무관). |
| <a id="g1-02"></a>G1-02 | 낮음 | — | 풀림 | 걸음·끌어오기 레인과 토네이도 이동 조각의 취소가 보간 중인 Transform을 사실 자리로 되돌리지 않아, F1 정지 화면에 칸 사이 위치가 남는다 | 이제 이동 취소는 모든 대여를 되돌린 뒤 유닛을 보인 사실 자리로 다시 그린다. 토네이도 이동 조각도 취소하면 목표 자리로 옮긴다. 그래서 F1 정지 화면에 칸 사이 위치가 남는 시나리오가 더는 성립하지 않는다. |
| <a id="g1-03"></a>G1-03 | 낮음 | — | 풀림 | [보강] 투사체 풀 반환·재대여가 TrailRenderer를 비우지 않아, 연속 발사에서 이전 착탄점부터 꼬리가 이어진다 | 발사를 시작할 때 새 자리로 옮기고 켠 다음 꼬리를 비운다. 다시 빌린 인스턴스가 이전 착탄점에서 선을 긋는 일은 더는 없다. |

### G2 (2)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="g2-01"></a>G2-01 | 낮음 | — | 풀림 | 진입 숨김(Renderer·_entryHudHidden)이 풀 초기화에 없고, Nexus 취소는 풀로 돌아간 표시를 '다른 개체'로 보고 복원을 건너뜀 | 풀 반환이 진입 HUD 숨김을 풀고, 두 진입 소유자가 같은 IsSameOrReturned 정책을 쓴다. Renderer도 늘 복원하므로, 반환된 표시에 숨김이 영구히 남는 경로가 없다. |
| <a id="g2-02"></a>G2-02 | 낮음 | — | 풀림 | 기존 필드 View 준비 대여가 View 신원을 고정하지 않아, 준비 중 그 View가 반환·재대여되면 옛 HUD 보임 스냅숏을 새 주인에 되돌림 | 기존 표시 준비 대여가 신원을 잡아 두고, 바뀌면 Validate에서 실패한다. Dispose는 옛 스냅숏을 쓴 뒤 지금 상태로 SyncHudActive를 다시 맞추므로 새 주인의 HUD가 틀어지지 않는다. |

### G3 (3)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="g3-01"></a>G3-01 | 중간 | — | 풀림 | 배치·아이템 Drag가 결과 재생 중에도 이어지지만, 재생 중에 놓으면 제출이 조용히 거절돼 배치·장착이 안내 없이 사라짐 | 재생 중(P<S) 칸·대상 위에서 놓으면 거절될 제출을 시도하지 않고 Drag를 유지해, 재생이 끝난 뒤 다음 누름·뗌에서 놓게 바뀌었습니다. 배치·장착이 안내 없이 사라지던 경로가 없어졌고, 유지 판정이 CanSubmit의 거절 식과 같은 IsUnpresented를 써서 경계가 맞습니다. |
| <a id="g3-02"></a>G3-02 | 낮음 | — | 풀림 | 입력 소유자 정리가 상태 깃발을 먼저 내린 뒤 보호 없이 단계를 이어 가서, 한 단계가 실패하면 F1 Stop이 다시 정리할 수 없음(숨긴 원본·고스트·가방 Drag 아이콘이 남음) | 배치·아이템 Drag 정리가 단계마다 실패를 모으며 끝까지 시도하므로 HideGhost 실패에도 원본 표시·가방 아이콘 숨김이 불린다. 바깥 루프가 보호 없이 끊겨도 뒤 소유자는 깃발이 살아 있어 F1 Stop이 정리한다. |
| <a id="g3-03"></a>G3-03 | 낮음 | 낮음 (C10) | 그대로 | '활성 전경 소유자는 하나' 불변식을 아무도 확인하지 않음: 경로 배타는 호출자 순서에, 넘겨받기는 BeginForeground 밖의 직접 전환에 기댐 | '활성 전경 소유자는 하나' 불변식을 검사하는 곳이 여전히 없다. 경로 배타는 호출자 순서에, 넘겨받기는 BeginForeground 밖의 직접 전환에 기댄다. |

### G4 (1)

| id | 그때 | 지금 | 상태 | 제목 | 오늘 확인한 근거 |
|---|---|---|---|---|---|
| <a id="g4-01"></a>G4-01 | 낮음 | — | 풀림 | 재바인딩(BindServices 두 번째 호출)이 옛 입력 연결을 놓지 않고 새 CombatInputConnection으로 바꿔 끼움 | 연결 중 재바인딩을 막아, 옛 Receiver가 슬롯에 묶인 채 새 입력 연결로 덮이는 경로가 없어졌다. |

## 4. 지난 문제와 겹친 새 지적

새로 찾은 지적 가운데 지난 문제와 뿌리·위치가 같은 것은 따로 세지 않고 지난 문제의 지금 판정을 따랐다.

| 새 지적 | 겹친 지난 문제 | 처음 지적 | 판정 |
|---|---|---|---|
| 사건 Presenter 생성자가 부품 25개 조립과 숨은 순서 의존을 떠안아, 기능마다 이 파일을 고쳐야 함 (3건 합침) | [C4-02](#c4-02) | 중간 | 지난 중간 그대로(다시 검증 안 함) |
| CombatCharacterEventHandler가 사건 종류가 다른 연출 일곱 갈래를 한 클래스에 모음(의존 16개) (2건 합침) | [C2-13](#c2-13) | 중간 | 지난 중간 그대로(다시 검증 안 함) |
| 필드 기믹 종류(토네이도·낙석·하붑 등)를 조립·정리·진입 보류·포트 목록마다 따로 적어, 새 기믹 하나에 8곳 넘게 손대야 함(OCP) (2건 합침) | [C2-01](#c2-01) | 중간 | 지난 중간 그대로(다시 검증 안 함) |
| 추락·약한 지반 소리가 DB 프리셋을 읽지 않고 코드 상수로 남 (2건 합침) | [C1-38](#c1-38) | 중간 | 지난 중간 그대로(다시 검증 안 함) |
| InspectUnit이 선택 바뀜 알림이 이미 한 HUD 선택·강조를 다시 함 | [C5-U0099](#c5-u0099) | 낮음 | 낮음(검증 B) |
| 표시 정리·해제·설치 실패 목록이 HUD·선택·버튼 쪽은 손으로 따로 적힌 세 벌 | [C1-02](#c1-02) | 낮음 | 낮음(검증 B) |
| 쓰지 않는 HandPresenter를 계속 고침 | [C5-03](#c5-03) | 낮음 | 낮음(검증 B) |
| FieldCellColorRenderer.CellAt은 부르는 곳이 없는 죽은 멤버 | [C5-U0110](#c5-u0110) | 낮음 | 낮음(검증 B) |
| FieldHighlightPalette의 gimmickHoverRange·roarForecast 기본값 인자는 쓰이지 않고, 빠뜨리면 투명색이 됨 | [C4-11](#c4-11) | 낮음 | 낮음(검증 B) |
| 공격 조준 응답마다 CombatAttackFeedback이 작은 배열과 박싱 열거자를 만듦 | [C11-06](#c11-06) | 낮음 | 낮음(검증 B) |
| 입력 사실의 IsAttackActive·IsItemDragActive·HasCurrentAcceptedEmptyMovementRange를 읽는 곳이 없는데 매번 계산함 | [C5-U0029](#c5-u0029) | 중간 | 낮음(검증 D) |
| 같은 Tick 안에서 입력 사실을 여러 번 읽고, 이미 있는 포인터 대상을 다시 계산함 | [C7-05](#c7-05) | 낮음 | 낮음(검증 B) |
| 카메라 입력 판정이 IsCameraRequest와 묶음의 if 사슬 두 곳에 있음 | [C4-12](#c4-12) | 낮음 | 낮음(검증 B) |
| 입력 묶음의 부품 수·목록 설명이 지금 코드와 다름 | [C12-02](#c12-02) | 낮음 | 낮음(검증 B) |
| 조기 공격의 '살아 필드에 있음' 판정을 새 람다로 또 적음 | [C1-03](#c1-03) | 낮음 | 낮음(검증 B) |
| 처리기 칸을 하나 더할 때마다 Create 인자·switch·enum·형식 표·조립을 모두 고침 | [C2-22](#c2-22) | 낮음 | 낮음(검증 B) |
| 조립이 늘 넘기는 협력자를 `= null` 선택 인자로 받고 `?.`로 갈라, 운영에서 닿지 않는 null 길이 생김 | [C5-08](#c5-08) | 중간 | 낮음(검증 D) |
| 사건 Presenter 주석 두 곳이 지금 코드와 어긋남 | [C12-14](#c12-14) | 낮음 | 낮음(검증 B) |
| 필드 위 살아 있는 유닛 판정이 유지 이펙트 출처마다 사본 | [C1-03](#c1-03) | 낮음 | 낮음(검증 B) |
| 보드 진입을 할지의 조건이 PrepareDisplay와 Prepare에 두 벌 있다 | [C1-12](#c1-12) | 낮음 | 낮음(검증 B) |
| 출력 카메라의 Camera.main 대체 규칙이 조립 안 두 줄과 포인터 매퍼에 따로 있음 | [C6-03](#c6-03) | 낮음 | 낮음(검증 B) |
| 특성 미리보기 조립이 운영 표시 조립의 presenter 생성·연결을 따로 한 벌 더 가짐 | [C1-19](#c1-19) | 낮음 | 낮음(검증 B) |
| InGameViewSystemFactory.Create는 호출자가 없고, 오류 경계 기본값 경로도 운영에서 닿지 않음 | [C5-06](#c5-06) | 낮음 | 낮음(검증 B) |
| '필드 위 행동 가능한 아군' 판정과 '그 칸의 유닛' 집기 규칙이 여러 파일에 다시 적혀 있음 | [C1-03](#c1-03) | 중간 | 낮음(검증 ER) |
| '아직 보이지 않은 사망' 판정이 소리·사망 처리기·동시 사망 입구 세 곳에 조금씩 다르게 있음 | [C1-60](#c1-60) | 중간 | 낮음(검증 D) |
| FieldUnitPresenter(1004줄 MonoBehaviour)가 풀·HUD·몸 기하·진입 수집·행동 커서·선택 준비 대여·숨김을 모두 맡음(SRP) | [C2-07](#c2-07) | 중간 | 낮음(검증 D) |
| 강조 색 묶음이 같은 형식의 위치 인자 24개와 기본값 색을 받아, 새 층의 색을 빠뜨려도 조용히 투명이 된다 | [C4-11](#c4-11) | 낮음 | 낮음(검증 B) |
| 연결 정리 순서가 Detach·StopInput(·OnInputClosed)에 사본으로 있음 | [C1-01](#c1-01) | 중간 | 낮음(검증 D) |
| 조정자 Bind가 서비스 수명 부품을 순환 람다로 직접 조립하고, 입력 서비스 값 사슬에 조정자가 끼어 있음 | [C4-07](#c4-07) | 중간 | 낮음(검증 ER) |
| 표시 조립 Factory가 Global UI(TurnCost)를 닫는 정책을 실행함 | [C4-30](#c4-30) | 낮음 | 낮음(검증 B) |
| 환경 호버가 포인터·카메라가 그대로여도 매 프레임 UI RaycastAll과 칸 판정을 다시 한다 | [C7-01](#c7-01) | 중간 | 낮음(검증 D) |
| 경로 입력 감시가 매 프레임 지난 응답을 다시 받아, 허용되지 않는 동안 매 프레임 취소 전체를 반복한다 | [C7-04](#c7-04) | 중간 | 낮음(검증 D) |
| 이펙트 받침이 따라가는 유닛이 멈춰 있어도 매 프레임 위치를 다시 계산해 쓴다 | [C7-07](#c7-07) | 낮음 | 낮음(검증 B) |

## 5. 검증에서 버린 새 후보 9개

| 후보 | 처음 지적 | 버린 근거 |
|---|---|---|
| 선택된 하나만 보이는데 행동 가능 아군마다 커서를 만들어 둠 | C5 낮음 | CombatUnitActionCursorPresenter.cs:47-51에서 커서는 anchor마다 _cursors에 한 번만 만들어 캐시하고, 다음 Refresh부터는 다시 만들지 않는다. SetMoved는 값이 같으면 바로 돌아가고(166-175), SetVisible도 activeSelf가 같으면 아무것도 쓰지 않는다(178-180). 그래서 비용은 행동 가능한 아군 수(몇 개)만큼 처음 한 번 Instantiate하는 것뿐이다. 이것은 선택을 바꿀 때 생성 비용을 미리 내는 프리웜과 같고, rubric은 드물게 한 번 도는 할당을 문제로 보지 않는다. 구체적인 비용이 없다. |
| 계측 코드의 군더더기: 같은 구간을 재는 마커 둘, 나뉜 #if 블록, 같은 이름공간 별칭 | C5 낮음 | CombatDeploymentConfirmation.cs:119-127은 Accept/Decline 마커를 Respond 마커와 겹쳐 건다. 이것은 전체 구간과 갈래별 구간을 나눠 보는 흔한 계측 방식이라 헛일이 아니다. CombatCameraInput.cs:31-43의 #if 블록이 둘로 나뉜 것과 CombatCommandGate.cs:1-3의 별칭 using은 형식 취향일 뿐이다. 어긋날 지점이나 비용이 없다. |
| 행동 버튼 값 묶음을 만들자마자 풀어 넘기고, 순서 규칙을 근거 없이 적음 | C5 낮음 | CombatActionButtonState(CombatActionButtonState.cs:13-34)는 CombatSceneInput.ReadActionButtons(CombatSceneInput.cs:171-173)가 값 넷을 한 번에 돌려주는 반환 묶음이라 하는 일이 있다. Driver(CombatSceneInputDriver.cs:189-190)가 받아서 풀어 넘기는 것은 형식 선택일 뿐이다. 순서 문장(10-11)도 근거가 없지 않다. CanRequest가 부르는 _readFacts는 Reader의 부작용(C5-U0029의 추적기 상태 지움)이 있는 읽기라 '순수 읽기라 순서 무관'이라는 전제가 성립하지 않는다. 구체 비용이 없다. |
| 증원 처리기가 소환 처리기의 세 단계를 그대로 베낌 | C1 낮음 | CombatReinforcementEventHandler.cs:30-44와 CombatSummonEventHandler.cs:38-50은 각각 한 줄짜리 위임이다. 칸 모으기는 이미 공용인 CombatPlaybackHighlight.AddOccupiedCells, 사실 반영은 _facts.ApplyCharacter, 조립은 CombatSummonEventHandler.Compose를 부른다. 등장 흐름의 규칙(강조 칸·사실·조립 내용)은 이미 한 곳에 있다. 두 파일에 남은 것은 사건 형식별 형변환과 Created 읽기뿐이라 어긋날 지점이 거의 없다. 정리 취향 수준이다. |
| 이미 가져온 네임스페이스를 global:: 별칭으로 다시 가져옴 | C5 낮음 | CombatSceneEventPresenter.cs:2-3의 global:: 별칭은 17줄 using Playback.Support와 겹친다. 두 형식은 Support 안에 하나씩만 있어(CombatPlaybackProfile.cs:8, CombatEventProfiler.cs:12) 충돌을 피하는 쓸모도 없다. 하지만 using 두 줄의 중복은 IDE 경고 수준이고 동작·변경·읽기 비용이 사실상 없다. rubric이 요구하는 구체적 비용이 없는 정리 취향이다. |
| 소속 맵 Build가 결과마다 사전 6개를 새로 만들고, 성공 경로에서도 오류 문구를 미리 조합함 | C11 낮음 | CombatPreAttackTraitMap.Build(45-55)의 사전 여섯 개는 결과당 한 번, 그것도 UsesSelectedTrait 클립이 있는 결과에서만 만든다(CombatPreAttackPresentationStarts.cs:50-53이 settings가 없으면 건너뜀). ResolvePull(134)의 보간 문자열도 PullApplied 선행 발동마다 한 번이다. 매 프레임·매 사건 경로가 아니라 결과 시작에서 드물게 도는 할당이다. rubric C11의 핫패스에 해당하지 않고 비용도 무시할 만하다. |
| 조기 공격의 생존 검사와 시작 검사가 달라 비활성 공격자에서 예외가 남 | C10 중간 | 두 검사가 다른 것은 맞습니다. Flush(CombatPreAttackPlayback.cs:124-125)는 _isAlive(CombatSceneEventPresenter.cs:199-203: 사실이 살아 있음·필드·HasTarget)로만 예약을 버리고, CanBeginAttackClip(CombatUnitPresentationWindow.cs:317-322)은 비활성이나 !SupportsClip이면 예외를 던집니다. 그러나 이 예외는 일반 경로와 같은 정책입니다. 일반 경로 TryBegin은 활성 여부를 보지 않는 TryGetAttackClip(:299-306)으로 BeginClip에 들어가고, 시작하지 못하면 '공격자 표시가 꺼졌거나 클립을 쓸 수 없음'으로 InvalidOperationException… |
| BodyText 계측 이름이 CombatPlaybackProfile의 접두사를 문자열로 다시 적음 | C1 낮음 | CombatBodyTextWindow.cs:16이 'Combat.Action.Playback.System.BodyText.Poll'을 직접 적는 것은 맞지만, 같은 'System.*' 마커 직접 표기가 FieldRailwayPresenter.cs:25-28, FieldTerrainPresenter.cs:28, FieldTornadoPresenter.cs:29에도 같은 관례로 있다. CombatPlaybackProfile.cs:26-29는 재생기별 단계 마커 생성기이고, 이 마커들은 에디터·개발 빌드 전용 계측 이름일 뿐 운영 동작이 어긋날 지점이 없다. 구체 비용이 없는 표기 취향으로 본다. |
| 배치 표시 Cancel의 '_cards != null' 검사가 하는 일과 상관없다 | C5 낮음 | _cards의 타입 DeckCardList는 UIGroup이고 ID가 UIId.UnitCard다(GlobalSystem/UISystem/Screens/Global/DeckCardList.cs:20·83). 따라서 CombatDeploymentPresentation.cs:115의 '_cards != null'은 닫으려는 바로 그 UI가 파괴됐는지 보는 Unity 파괴 판정이다. 생성자 보장(29-35)은 만든 시점만 보장하므로 늘 참인 검사가 아니고, '하는 일과 상관없다'는 주장도 맞지 않는다. |

