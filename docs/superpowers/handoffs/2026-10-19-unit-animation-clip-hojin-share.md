# 외형 표 칸 추가 공유: 유닛 전투 클립 묶음 `animationPresentation` (용근 → 호진님)

- 작성일: 2026-10-19. 작성: 용근(View). 브랜치 `feature/Yonggeun/Stage`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련: [클립 설계](../specs/2026-10-19-combat-unit-animation-clip-design.md) AC1·AC2·AC9·AC10, [클립 계획](../plans/2026-10-19-combat-unit-animation-clip-plan.md) T1~T8(AC-1)

## 1. 요약

유닛마다 다른 공격·피격 애니메이션 클립을 쓸 수 있게 기반을 넣었습니다. 지금은 **데이터와 검사만** 들어갔고, 전투 화면은 바뀌지 않습니다(재생은 AC-2~AC-4에서 붙습니다). 묶음을 연결한 유닛이 없으므로 기존 에셋은 그대로 통과합니다.

## 2. 호진님 파일에서 바뀐 것

| 파일 | 바뀐 것 |
|---|---|
| `Script/Systems/DataBaseSystem/Data/View/Table/Unit/Item/UnitVisualDataSO.cs` | 칸 `animationPresentation`(`CombatUnitAnimationSO`, 비워도 됨), 읽기 `AnimationPresentation`, `Validate`에 **한 줄**(`CombatUnitAnimationRules.ValidateUnit(…)`). 공격 설정은 `Validate`에서 한 번 만들어 그 줄에도 넘깁니다(기존 동작 같음). 클립 규칙은 이 파일에 두지 않았습니다 — 규칙이 바뀌어도 이 파일은 그대로입니다 |

새 파일(모두 `DataBaseSystem/Data/View/CombatPresentation/`):

| 파일 | 내용 |
|---|---|
| `CombatUnitAnimationSO.cs` | 클립 묶음 SO(메뉴 `ProjectL/Database/View/Combat Unit Animation`). 공격 클립 + 부속 클립 목록 + 피격 선딜레이·소리 선딜레이 + 이펙트·이펙트 선딜레이(§8), 피격 클립 + 부속 클립 목록(부속은 2026-10-19 6차) |
| `CombatUnitAnimationData.cs`·`CombatClipAttackData.cs`·`CombatClipData.cs` | 불변 값. 시각 규칙의 원본은 `CombatClipData.FindTimeError`, 부속 목록 모양(빈 칸·중복·주 클립과 같음)의 원본은 `CombatClipData.FindCompanionError` |
| `CombatUnitAnimationRules.cs` | **전투 클립 규칙의 원본**(아래 §4 전부) |
| `CombatClipEvents.cs` | 클립 이벤트 셋 읽기 |
| `CombatAnimationEvents.cs` | 이벤트 이름표 컴포넌트(MonoBehaviour, 아래 §3) |

## 3. `DataBaseSystem`의 첫 MonoBehaviour (`CombatAnimationEvents`)

- **왜 DB 쪽인가:** 외형 표 검사(DB 생성 때)가 이 컴포넌트를 찾고 함수 이름을 읽어야 합니다. View 쪽에 두면 DB → View 참조가 처음 생기므로, 프리팹에 붙이는 **작업 입력**(SO와 같은 층)으로 보고 DB 쪽에 두었습니다(클립 계획 P1 A, 사용자 결정).
- **이 폴더 MonoBehaviour의 범위:** 프리팹에 붙는 작업 입력의 이름표·자리 표시와 그 규칙 함수(`FindIn`)만 둡니다. 런타임 상태·`Update`·서비스 참조는 두지 않습니다. 같은 종류가 또 생기면 같은 규칙을 따라 주세요.

## 4. 검사 규칙 (DB 생성 때, 어기면 부트에서 실패)

규칙의 원본은 코드(`CombatUnitAnimationRules`·`CombatClipEvents.Read`·`CombatClipData.FindTimeError`)와 [클립 설계](../specs/2026-10-19-combat-unit-animation-clip-design.md) AC1·AC2입니다. 아래는 작업용 요약입니다.

- **이벤트 컴포넌트가 붙은 유닛**(묶음이 없어도, 예: Idle만 쓰는 원거리 아군): 프리팹 안에 `CombatAnimationEvents`가 **하나**, Animator와 **같은 GameObject**에 있어야 하고, 그 컨트롤러(Idle) 클립이 Animator GameObject **자신**의 Transform을 움직이면 실패합니다(루트 곡선, 에디터에서만).
- **묶음을 연결한 유닛:**
  - 프리팹에 이벤트 컴포넌트가 있어야 합니다.
  - 묶음에 클립이 하나 이상 있어야 합니다. 클립은 길이가 유한한 양수이고 **Loop Time을 끄며**, 루트 곡선이 없어야 합니다(에디터에서만).
  - 클립마다 이벤트 셋 `ClipStart`·`ClipEvent`·`ClipEnd`가 정확히 하나씩, 시작 ≤ 이벤트 타이밍 ≤ 끝, 클립 길이 안에 있어야 합니다. 다른 이름의 이벤트가 있으면 실패합니다.
  - 선딜레이(피격·소리)는 유한한 0 이상의 초입니다(공격 클립이 있을 때만 봄).
  - 공격 클립은 공격 표시가 **근접(Melee)·직선(Linear)·곡사(Mortar)**인 유닛에 넣을 수 있습니다(Ballista 제외, 2026-10-19 AC-5에서 넓힘).
  - **원거리(직선·곡사) 공격 클립**이면 이벤트 컴포넌트에 **발사 지점(`muzzle`)**을 지정해야 하고, **피격 선딜레이는 0**이어야 합니다(이벤트 타이밍에 바로 쏘고, 맞는 순간은 투사체가 정함).
  - **공격 칸에 이펙트(`attackEffect`)를 고르면** 이벤트 컴포넌트에 **이펙트 지점(`effectPoint`)**을 지정해야 합니다(근접·원거리 모두, §8). 이펙트 선딜레이는 다른 선딜레이와 같은 규칙입니다.
  - **부속 클립**(2026-10-19 6차, 클립 설계 §12): 칸(공격·피격)마다 주 클립과 같은 시각으로 도는 부속 클립(무기 등)을 둘 수 있습니다. 부속은 빈 칸·중복·주 클립과 같은 클립이 없어야 하고, 주 클립과 같은 클립 규칙(길이·Loop Time 끔·루트 곡선 없음)에 더해 **이벤트가 없어야** 하며, 주 클립·다른 부속과 **같은 곡선(같은 뼈의 같은 속성)을 움직이면** 실패합니다(겹침은 에디터에서만). 주 클립 없이 부속만 넣은 칸도 실패합니다.

## 5. 에셋 작업 안내

- **클립:** 시작·이벤트 타이밍·끝에 애니메이션 이벤트를 하나씩 찍고 함수 이름을 `ClipStart`·`ClipEvent`·`ClipEnd`로 합니다. 이벤트 타이밍은 이펙트·소리·피격 같은 연출의 기준 시점이고, 선딜레이는 묶음 SO에 적습니다. 끝 이벤트 시각이 재생 길이입니다.
- **움직이는 것:** 모델 자식의 Transform만 움직입니다. Transform 밖의 속성 곡선(재질·블렌드셰이프 등)은 클립이 끝나도 되돌리지 않습니다.
- **컨트롤러(Idle):** 컨트롤러에는 Idle만 둡니다(Idle끼리 전환은 괜찮음). 파라미터·공격·피격 상태·루트 모션은 넣지 않습니다. 유닛마다 Idle 클립만 다르면 Override Controller를 씁니다.
- 이벤트 컴포넌트가 붙은 유닛은 전투 중에 그 컨트롤러를 전투 코드가 넘겨받아 재생합니다(AC-2부터). 붙지 않은 유닛은 Unity가 평소처럼 재생합니다.
- 묶음 SO 하나를 여러 유닛이 공유할 수 있습니다.
- **무기 테이크(부속 클립):** 모델 작업에서 무기가 따로 테이크로 나오면(예: `CannonBone|CannonAttackBody`) 주 클립 칸 아래 부속 클립 목록에 넣습니다. 이벤트는 주 클립(몸)에만 찍습니다. 공격이 끝나면 Idle이 움직이지 않는 무기 뼈는 프리팹 기본 자세로 돌아갑니다(무기 Idle은 컨트롤러 레이어로).

## 6. 병합 때 주의

- `UnitVisualDataSO.cs`에 칸이 하나 늘었습니다. 같은 파일을 고치셨다면 필드·읽기·`Validate`의 한 줄 세 곳을 함께 맞춰 주세요.
- `Validate`가 이제 모든 유닛 프리팹에서 이벤트 컴포넌트를 찾습니다(없으면 아무 검사도 하지 않음). 지금 컴포넌트가 붙은 프리팹은 없어 기존 에셋은 그대로 통과합니다.
- 기존 외형 에셋은 다시 저장하지 않았습니다(새 칸은 비어 있는 값으로 읽힘).

## 7. 추가(2026-10-19, AC-5): 곡사 적 `언데드 마인카트`(`Unit_UndeadCannonCart`, 키 25) 외형 항목

원거리 클립 공격이 들어가면서 첫 사용 유닛이 곡사 적 `CannonEnemy` 모델(민형님 `Models/Enemy/CannonEnemy.fbx`)입니다. 이 유닛은 Core 쪽이 게임 데이터만 만들어 두었고, **외형 항목·표 등록·적 배치는 View 쪽 일**로 인계되어 있습니다([Core 인계](2026-10-16-undead-cannon-cart-view-handoff.md) "View integration remaining"). 외형 표 쪽에 필요한 것은 아래입니다.

- **외형 항목 `UnitVisual_UndeadCannonCart`:** 키 `Unit_UndeadCannonCart`, 표시 이름 `언데드 마인카트`, 프리팹(민형님 메모 §6), 아이콘. `UnitVisualDataTable`에 등록.
- **공격 표시:** 곡사(Mortar). 기존 `CombatAttack_CactusCannon`을 같이 써도 됩니다. 클립 공격은 공격 설정 중 **투사체 값(프리팹·비행 시간·곡사 높이·회전)만** 읽고, 버킷·반동·발사 비율 값은 읽지 않습니다. 포탄 모양이 다르면 새 공격 설정을 만듭니다.
- **클립 묶음 SO:** 공격 칸 = 몸 테이크 `Scull|CannonAttack`, 공격 부속 = 포신 테이크 `CannonBone|CannonAttackBody`(민형님 재내보내기 `13eb1dbc`), **피격 선딜레이 0**, 소리 선딜레이는 원하는 값. 피격 칸은 비워 둡니다(피격 클립 없음). (처음 적은 "`CannonAttack`·`CannonAttackBody` 둘 다 가능"은 바뀜 — `Scull|CannonAttackBody`는 포신 동작이 몸 아마추어에 따라 나온 테이크로 보여 쓰지 않습니다.) **만들어 둠(2026-10-19):** `ViewGroup/CombatPresentation/AnimationClip/CombatUnitAnimation_Cannon.asset`(공격 `Scull|CannonAttack` + 부속 `CannonBone|CannonAttackBody`, 선딜레이 둘 다 0). 클립 이벤트와 프리팹이 갖춰지기 전엔 외형 항목에 연결하지 않습니다(연결하면 DB 검사가 실패).
- **외형 항목 진행(2026-10-19):** 사용자가 `UnitVisual/Item/Enemy/UnitVisual_Cannon`(키 25, 프리팹 `Prefabs/Characters/Colored/Enemy/CannonEnemy.prefab`)을 만들어 표에 넣고 공격 표시(`CombatAttack_CactusCannon`)·반응(기본)·클립 묶음(`CombatUnitAnimation_Cannon`)을 연결했습니다. 위 이름 `UnitVisual_UndeadCannonCart` 대신 이 항목을 씁니다. 표시 이름은 `언데드 마인카트`로 맞췄고, **아이콘(`timelineIcon`·`infoIcon`)은 아직 없습니다**(검사는 통과, 화면에 그림 없음). InGame 쪽은 `UnitDataTable`·`TargetingMethodDataTable`에 등록됐고, 적 배치는 `EnemyLoadout_1`(`Combat_Normal_A` 스테이지)에 가중치 1로 넣었습니다(사용자 결정).
- **프리팹 쪽 요구(검사가 봄):** Animator가 있는 GameObject에 `CombatAnimationEvents` 하나 + 발사 지점(`muzzle`) 지정, 컨트롤러는 `CannonIdle`만. `CatusCannonWeaponView`는 필요 없습니다(클립이 포신을 움직임).
- **InGame 표 등록(Core 인계 2단계):** `TargetingMethodDataTable`·`UnitDataTable` 등록은 외형 항목이 준비된 뒤 함께 합니다(외형 없이 먼저 켜면 DB 생성이 실패할 수 있음). 적 배치(적 로드아웃·스테이지 등장)는 콘텐츠 담당과 정합니다.
- 등록 뒤 `DataBaseAssetTests`를 돌려 주세요.

## 7a. 추가(2026-10-19): 나머지 적 클립 묶음 SO — 만들어 두고 연결 안 함

민형님 적 모델 `FixsoEnemy`·`SabowEnemy`·`Stage1Boss`의 공격 테이크에 이벤트 셋을 넣고 클립 묶음 SO를 만들어 두었습니다(`ViewGroup/CombatPresentation/AnimationClip/CombatUnitAnimation_FixSo`·`_Sabow`·`_Stage1Boss`, 선딜레이 0·이펙트 없음, [클립 계획](../plans/2026-10-19-combat-unit-animation-clip-plan.md) §10.3). **외형 항목에는 연결하지 않았습니다** — 지금 적 프리팹(`Enemy.prefab`·`EnemyUnit_UnderTaker.prefab`·`1StageBoss.prefab`)은 옛 모델이라, 연결하면 이벤트 컴포넌트가 없어 부트 검사가 실패합니다. 그 모델로 프리팹을 만든 뒤 연결해 주세요.

- **프리팹 요구:** FBX 루트(= `Scull`의 부모)에 Animator(Idle 클립 하나만 있는 컨트롤러)와 `CombatAnimationEvents` 하나. 이 세 모델은 테이크가 `Scull` 자신도 움직이므로 Animator를 `Scull`에 붙이면 루트 곡선 검사에서 실패합니다. 원거리 유닛이면 `muzzle`도 지정합니다.
- **유닛 대응:** `Stage1Boss`는 보스(`UnitVisual_StageBoss1`, 키 18)로 보입니다. `FixSo`·`Sabow`가 어느 유닛인지는 민형님께 묻는 중입니다.

## 8. 추가(2026-10-19): 공격 칸 이펙트 — 새 칸 둘과 이펙트 지점

공격 클립의 이벤트 타이밍(`ClipEvent`)에 이펙트를 낼 수 있게 됐습니다([이펙트 설계](../specs/2026-10-19-combat-clip-effect-design.md), [이펙트 계획](../plans/2026-10-19-combat-clip-effect-plan.md) EF-1·EF-2). 기본값이 "이펙트 없음"이라 **기존 에셋은 그대로 통과하고 화면도 같습니다.** 호진님 파일(`UnitVisualDataSO.cs`)은 이번에 바뀌지 않았습니다.

- **클립 묶음 SO(`CombatUnitAnimationSO`) 공격 칸에 새 칸 둘:**
  - `attackEffect`(`EffectType`, 기본 `Empty` = 이펙트 없음) — 이벤트 타이밍에 낼 이펙트. 종류는 Render DB 이펙트 표(민형님)의 `EffectType`을 그대로 씁니다(기차·스테이지 맵 칸과 같은 방식).
  - `effectDelay`(초, 0 이상) — 이벤트 타이밍 뒤 이펙트까지의 선딜레이. 피격·소리 선딜레이와 같은 규칙이고, 이펙트가 Empty여도 공격 클립이 있으면 검사합니다.
- **이벤트 컴포넌트(`CombatAnimationEvents`)에 선택 칸 `effectPoint`(Transform):** 이펙트를 낼 지점(무기 끝·총구 등). **위치와 회전을 모두** 씁니다. 이펙트는 그 순간 그 자리에 놓이고 유닛을 따라가지 않습니다(필드 루트 아래).
- **새 검사(DB 생성 때, `CombatUnitAnimationRules`):** 공격 칸에 이펙트를 고른 유닛은 프리팹 이벤트 컴포넌트에 `effectPoint`를 지정해야 합니다. 근접·원거리 모두이고, 원거리 유닛도 발사 지점(`muzzle`)이 대신하지 않습니다(같은 Transform을 넣어도 됩니다). 정의되지 않은 `EffectType` 값도 실패합니다.
- **부트가 잡지 못하는 것:** 고른 종류가 Render DB 이펙트 표에 실제로 등록됐는지는 View DB 검사가 보지 않습니다. 등록 안 된 종류면 **처음 이펙트를 낼 때** 오류로 멈춥니다. 지금 등록된 종류는 `TestFire`·`Boom`뿐이고, 전투용 이펙트 등록은 민형님께 요청했습니다(Factory 공유 문서 §19).
- **재생 확인 방법(선택):** 클립 묶음 공격 칸에 `Boom`을 임시로 넣고 프리팹에 `effectPoint`를 지정 → 이벤트 타이밍 + 선딜레이에 그 자리에 이펙트. 확인 뒤 되돌려 주세요.

## 9. 추가(2026-10-19): 투사체 접촉 이펙트 — 공격 표시 SO의 새 칸 하나

투사체가 대상에 닿는 순간(대포 착탄·Ballista 관통 등) 그 자리에 이펙트를 낼 수 있게 됐습니다([접촉 이펙트 설계](../specs/2026-10-19-combat-projectile-impact-effect-design.md), [계획](../plans/2026-10-19-combat-projectile-impact-effect-plan.md) IE-a·IE-b). 기본값이 "이펙트 없음"이라 **기존 에셋은 그대로 통과하고 화면도 같습니다.** 호진님 파일(`UnitVisualDataSO.cs`)은 바뀌지 않았습니다.

- **공격 표시 SO(`CombatAttackPresentationSO`, 메뉴 `ProjectL/Database/View/Combat Attack Presentation`)에 새 칸 `impactEffect`**(`EffectType`, 기본 `Empty`, "발사체" 머리말 아래):
  - 곡사 = 착탄점(주 칸 중심), 직선 = 주 대상 몸에 닿는 자리, **Ballista = 관통하는 대상마다** 그 자리에 한 번씩. 화살이 경로 끝(벽·사거리 끝)에 닿을 때는 내지 않습니다.
  - 이펙트는 투사체의 **수평 진행 방향**을 봅니다(필드 위쪽 기준). 폭발처럼 대칭인 이펙트는 차이가 없고, 관통처럼 방향 있는 이펙트는 화살 쪽을 봅니다. 그 자리에 놓이고 따라가지 않습니다(필드 루트 아래, §8과 같은 창구).
  - 코드 모션 유닛(선인장 대포·Ballista 등)과 클립 원거리 유닛(언데드 마인카트) 모두 이 칸 하나를 씁니다. 클립 묶음 SO의 `attackEffect`(§8, 공격자 이펙트 지점에서 이벤트 타이밍에)와는 다른 순간·자리입니다 — 둘 다 넣으면 둘 다 나옵니다.
- **검사(DB 생성 때, 공격 표시 SO):** 정의되지 않은 `EffectType` 값은 실패, **근접(Melee) 표시는 Empty만** 됩니다(투사체가 없음 — 근접 타격 이펙트는 §8의 공격 칸 이펙트로).
- **이펙트 종류 판정 한 곳:** "정의된 이펙트 종류" 규칙이 `DataBaseSystem/Data/View/CombatPresentation/CombatPresentationEffectRules.cs` 하나로 모였습니다(§8의 클립 칸·이펙트 창구·폭발 Presenter·이번 칸이 함께 부름). 옛 `CombatClipAttackData.IsKnownEffect`·`EffectRule`은 지웠습니다 — 같은 파일을 병합하실 때 참고해 주세요.
- **부트가 잡지 못하는 것:** §8과 같습니다. Render DB 이펙트 표에 실제로 등록됐는지는 첫 재생 때 오류로 드러나고, 전투용 이펙트 등록은 민형님께 요청 중입니다(Factory 공유 문서 §19).
- **재생 확인 방법(선택):** 예를 들어 `Code/CombatAttack_CactusCannon`의 `impactEffect`에 `Boom`을 임시로 넣고 선인장 대포 공격 → 착탄점에 이펙트. 확인 뒤 되돌려 주세요.

## 10. 추가(2026-10-20): 피격 이펙트 — 반응 SO의 새 칸 하나

맞은 유닛 몸에 따라 다른 피격 이펙트를 낼 수 있게 됐습니다([피격 이펙트 설계](../specs/2026-10-20-combat-hit-effect-design.md), [계획](../plans/2026-10-20-combat-hit-effect-plan.md)). 기본값이 "이펙트 없음"이라 **기존 에셋은 그대로 통과하고 화면도 같습니다.** 호진님 파일(`UnitVisualDataSO.cs`)은 바뀌지 않았습니다.

- **반응 SO(`CombatUnitReactionPresentationSO`, 메뉴 `ProjectL/Database/View/Combat Unit Reaction Presentation`)에 새 칸 `hitEffect`**(`EffectType`, 기본 `Empty`, "피격" 머리말 아래 플래시 칸 뒤):
  - 맞는 순간 **맞은 유닛 몸 상자 중심**(프리팹 루트 BoxCollider 중심, 콜라이더가 없으면 발밑)에 한 번 냅니다. **치명타(사망)도** 냅니다.
  - 이펙트의 +Z가 **맞은 방향(공격 출처 반대쪽)**을 봅니다 — 피격 때 유닛이 밀리는 방향과 같습니다. 놓이고 따라가지 않습니다(§8과 같은 창구).
  - 공격·가시·기차 등 **피해 원인과 상관없이** 체력이 줄면 냅니다.
  - §9의 `impactEffect`(무기 쪽, 접촉 자리)와는 다른 사실입니다 — 대포가 맞히면 둘 다 나옵니다. 한쪽만 원하면 다른 쪽을 Empty로 두세요.
- **유닛마다 다르게 하려면:** 지금 반응 에셋은 `CombatUnitReaction_Default` 하나를 모든 캐릭터가 같이 씁니다. 칸을 채우면 모든 캐릭터가 같은 이펙트를 냅니다. 유닛별로 다르게 하려면 반응 에셋을 복제해 칸을 바꾸고, 그 유닛 외형 항목의 `reactionPresentation`에 연결해 주세요.
- **검사(DB 생성 때):** 정의되지 않은 `EffectType` 값은 실패합니다(이펙트 종류 규칙 한 곳, §9). Render DB 등록 여부는 §8과 같이 첫 재생 때 드러납니다.

## 11. 추가(2026-10-27): 연출 범용 구조 — 외형 표 칸 셋의 뜻이 바뀜 (미전달)

근거는 [범용 연출 구조 설계](../specs/2026-10-27-combat-presentation-generic-structure-design.md)(G1~G8)와 [계획](../plans/2026-10-27-combat-presentation-generic-structure-plan.md) §13.3~§13.6입니다. 외형 표(`UnitVisualData`)의 칸 이름 셋(`attackPresentation`·`reactionPresentation`·`animationPresentation`)은 그대로이고, 그 칸이 가리키는 SO의 모양이 바뀌었습니다.

- **공격 표시 SO(`CombatAttackPresentationSO`)는 이제 묶음입니다.** 칸은 `projectile`(투사체 부품)·`codeMotion`(코드 모션 부품)·`cues`(공격 칸 큐) 셋입니다. 종류(`kind`) 칸은 없어졌고, 투사체 부품의 타입에서 나옵니다(없음 = 근접, 직선 = Linear, 곡사 = Mortar).
  - 새 메뉴: `ProjectL/Database/View/Combat Projectile (Straight)`·`(Arc)`, `Combat Code Motion (Melee)`·`(Shoot)`·`(Mortar)`. 부품 에셋 8개는 `DataBaseSO/ViewGroup/CombatPresentation/Projectile/`·`CodeMotion/`에 있습니다. 공격 묶음 5개가 옛 값과 같은 부품을 참조합니다.
  - 옛 `impactEffect`(착탄 이펙트)는 공격 칸 큐의 이펙트(시점·자리 = Contact)입니다.
- **반응 SO(`CombatUnitReactionPresentationSO`)** 의 `hitFlashColor`·`hitFlashDuration`·`hitEffect`는 피격 칸 큐 `hitCues`·사망 칸 큐 `deathCues`로 옮겼습니다. 설정 표의 피격·사망 흔들림 여섯 칸도 이 반응 기본 에셋의 큐로 옮겼습니다(값 그대로).
- **클립 묶음 SO(`CombatUnitAnimationSO`)** 의 `soundDelay`·`attackEffect`·`effectDelay`는 걷었습니다(9개 모두 0·Empty였음). 소리 시점·이펙트는 공격 칸 큐에 둡니다. `hitDelay`는 그대로입니다.
- **외형 표 `Validate` 한 줄**(이 파일의 우리 줄, `e0ee626d`): 반응 값을 만들어 `CombatUnitAnimationRules.ValidateUnit`에 넘깁니다. 피격 클립이 있는데 반응 묶음이 없으면, 이펙트 지점·발사 지점 자리 큐를 쓰는데 공격 클립·그 지점이 없으면, 피격 칸 큐가 기준 순간(Cue)을 쓰는데 피격 클립이 없으면 부트에서 거절합니다. 지금 에셋은 모두 통과하도록 옮겼습니다.
- **사건 Presenter의 캐릭터 처리기 생성 줄**(토네이도 인자 둘이 붙은 줄): 캐릭터 처리기가 카메라 연출 값을 더 받지 않아 `values.CameraMotion` 인자 하나를 뺐습니다. 토네이도 인자 둘은 그대로입니다.
- 토네이도 날림(`TryBeginTornado`)·`TornadoPresentationSettings`는 고치지 않았습니다. 나중에 같은 형식(사건 묶음 + 큐)으로 옮길 후보로 설계에 적었습니다.

## 12. 추가(2026-11-17): 클립 칸 다시 짜기·유닛 소리 — 묶음 칸 모양과 외형 표 칸 (미전달)

근거는 [클립 칸 다시 짜기 설계](../specs/2026-11-17-combat-unit-clip-slot-redesign-design.md)(CS1~CS11)·[계획](../plans/2026-11-17-combat-unit-clip-slot-redesign-plan.md) §5와 [유닛 소리를 유닛마다 설계](../specs/2026-11-17-combat-unit-sound-per-unit-design.md)(US1~US7)·[계획](../plans/2026-11-17-combat-unit-sound-per-unit-plan.md) §6입니다. 컴파일·Unity 확인 문제 없음(사용자). 커밋은 사용자.

**클립 묶음 SO(`CombatUnitAnimationSO`)의 칸 모양이 바뀌었습니다.**
- 칸은 `attack`·`hit`(동작) 둘과 공격 전용 시작 기준 `motionStart`(AfterLead/WithLead)·`motionStartDelay`(WithLead만, 음수 = 그만큼 먼저 시작)입니다. 옛 `attackClip`·`hitClip`·`shots`·`hitDelay`·`attackSpeed`·시작 모드 칸은 옮기기 도구로 옮긴 뒤 지웠습니다.
- 동작 = 앞 단계(공격만)·본 단계·뒤 단계(단계마다 몸·탑승물·부속 클립) + 이벤트 줄 목록(`events`, 번호·지점·지연) + 재생 배율 `animSpeed` + 블렌드 셋(`blendIn`·`blendBetween`·`blendOut`, 켜짐·초).
- **`[Event1, 0, 0]`은 적지 않아도 늘 맨 앞에 있습니다**(머리말에 공격 = 임팩트 타이밍, 피격 = 피격 타이밍). 묶음 14개의 이 줄은 지웠고, 에블린 공격만 `[Event2, 2, 0]` 한 줄이 남았습니다.
- 큐 시점 이름: `Cue` → `Event1`, `Cue2`·`Cue3` → `Event2`·`Event3`, `Hit` → `Contact` + 고르기 `FirstHit`(값은 옮겨 둠). 큐의 Event1~3은 묶음 이벤트 목록이 아니라 본 단계 몸 클립 표식이 있으면 됩니다.
- 큐 자리 `ActorEffectPoint`·`ActorMuzzle`은 `ActorEventPoint` 하나 + 번호(`eventPoint`) + 회전(`eventPointRotation` Point/Actor)입니다.

**외형 표(`UnitVisualData`) 칸이 바뀌었습니다.**
- 맨 끝에 머리말 "전투 소리"와 목록 셋 `attackSounds`·`hitSounds`·`deathSounds`(줄 = 번호 `sound` · 시점 `anchor` · 고르기 `contactFilter` · 지연 `delay`)를 더했습니다. 공격·피격·사망 소리의 원본입니다.
- `attackSoundSlotIndex` 칸은 지웠습니다(공격 줄의 번호로 옮김 — 현강님 칸이라 현강님께도 따로 알림).
- `unitSoundPreset`은 아군의 선택·이동·취소(와 장애물 피해·파괴)만 씁니다. 사망은 사망 줄입니다.
- 검사 `Validate`가 소리 줄을 `CombatUnitAnimationRules.ValidateUnit`(새 인자 `sounds`)에 넘깁니다. 그 유닛 공격 종류·클립에 맞지 않는 시점, 반응 묶음 없는 외형의 피격 줄은 Play 시작 때 멈춥니다.
- 외형 에셋 26개에 지금 소리 그대로 줄을 옮겨 넣었습니다(대조표 유닛 소리 계획 §6.2).

**공유 묶음 SO에서 소리 칸을 걷었습니다.** 공격 표시 SO의 `cues.sound`·`additionalSounds`, 반응 SO의 `hitCues.presetSounds`·`deathCues.sound`가 없어졌습니다(이펙트·흔들림·플래시는 그대로).
