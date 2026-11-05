# 전투 유닛 애니메이션 모델 요청 (민형님)

- 작성: 2026-10-19, View(용근) 쪽. **작성 완료, 외부 전달 전.**
- 대상: 민형님 커밋 `cad28dba`·`371f8e56`의 `Assets/ProjectL/Resources/Characters/Colored/Models/`(FBX 13개).
- 관련: [클립 설계](../specs/2026-10-19-combat-unit-animation-clip-design.md), [클립 계획](../plans/2026-10-19-combat-unit-animation-clip-plan.md) §0.8(받은 모델 대조 기록).

## 1. 요약

모델 잘 받았습니다. 전투에서 유닛마다 Idle과 공격 클립을 재생하는 기반을 만들고 있어서, 받은 FBX의 계층과 테이크를 확인했습니다.

- **적 4개는 지금 그대로 쓸 수 있습니다.** 모델과 클립이 한 파일 안에 있어 경로가 맞습니다.
- **아군은 다시 내보내기가 필요합니다.** 애니메이션 파일과 아군 모델의 계층이 달라 클립이 모델에 붙지 않습니다(§2 R1).
- **무기 동작은 지금처럼 캐릭터와 다른 테이크로 내보내셔도 됩니다**(2026-10-19 바뀜). View가 캐릭터 테이크(주 클립)와 무기 테이크(부속 클립)를 같은 시각으로 같이 재생합니다(§2 R2).

## 2. 요청

### R1. 아군 계층 맞추기 (꼭 필요)

Unity의 Generic 클립은 뼈대를 이름 하나가 아니라 **Animator에서 시작하는 전체 경로**로 찾습니다. 경로가 하나라도 다르면 그 뼈대는 움직이지 않습니다(Animation 창에 `(Missing!)`).

| | 지금 계층 | 클립 경로 |
|---|---|---|
| `Playerble/Animation.fbx` | `Barllista`(발리스타 아마추어) → `뼈대` → **`BaseModel`** → … → `뼈대.013.R` → `MoningStar` | `Barllista/뼈대/BaseModel/뼈대/…` |
| 아군 모델(Akimbo·Cowboy·Shotgun·Sniper·Ballista) | `아마츄어` → `뼈대` → … | `아마츄어/뼈대/…` |

뼈대 이름은 아군 모델이 `Animation.fbx`의 부분집합이라 같습니다. 다른 것은 그 위 단계입니다. 그래서 아마추어 이름만 바꿔서는 부족하고, 아래 넷이 함께 필요합니다.

1. `Animation.fbx`에서 `BaseModel`을 발리스타 뼈대에서 떼어 **최상위**로 내보내 주세요. 발리스타 동작은 §2 R3을 봐 주세요.
2. 아군 모델의 아마추어 이름을 `아마츄어` → **`BaseModel`**로 맞춰 주세요.
3. Cowboy의 무기 아마추어(`오른손 뼈대.013.R` 아래 `아마츄어.001`)를 **`MoningStar`**로 맞춰 주세요(`Animation.fbx`의 모닝스타와 같은 자리·이름).
4. Unity 가져오기 설정(Model 탭)에서 **Preserve Hierarchy를 켜** 주세요(`Animation.fbx`와 아군 모델 모두). 최상위 노드가 하나만 남으면 Unity가 그 노드를 없애 경로가 또 바뀝니다.

확인 방법: Unity에서 Akimbo 모델을 열고 `BaseModel|Idle` 클립을 붙였을 때 Animation 창에 `(Missing!)`이 없으면 됩니다.

### R2. 무기 테이크는 따로 내보내도 됩니다 (2026-10-19 바뀜, View 쪽 결정)

처음에는 "행동 하나 = 테이크 하나(캐릭터+무기)"를 부탁드리려 했는데, 블렌더가 아마추어마다 테이크를 따로 만드는 게 기본이라 View 쪽이 맞추기로 했습니다(클립 설계 §12).

- 지금처럼 **캐릭터 테이크와 무기 테이크를 따로** 내보내시면 됩니다. 예: `BaseModel|MoningStarAttack`(캐릭터) + `MoningStar|Attack`(무기), `Scull|CannonAttack`(몸) + `CannonBone|CannonAttackBody`(포신).
- View는 캐릭터 테이크를 **주 클립**, 무기 테이크를 **부속 클립**으로 두고 같은 시각으로 같이 재생합니다.
- **이벤트(`ClipStart`·`ClipEvent`·`ClipEnd`)는 캐릭터 테이크에만** 찍습니다. 무기 테이크에는 이벤트를 찍지 않습니다(찍으면 DB 검사에서 막힘).
- 무기 테이크는 **무기 뼈만** 움직여야 합니다. 캐릭터 뼈를 같이 움직이면(예: 다른 아마추어용 동작이 캐릭터 아마추어에도 따라 나온 `Scull|CannonAttackBody`) 캐릭터 동작을 덮어 DB 검사에서 막습니다.
- 길이는 맞추지 않아도 됩니다. 무기 테이크가 짧으면 마지막 자세를 유지하고, 길면 캐릭터 테이크의 끝 이벤트에서 잘립니다.
- 무기 Idle이 있으면 Idle 컨트롤러에 **레이어를 하나 더** 두고 무기 Idle을 넣어 주세요. 없으면 공격이 끝난 뒤 무기 뼈는 프리팹 기본 자세로 돌아갑니다.
- 무기가 따로 움직이지 않는 행동(쌍권총·샷건 등)은 지금처럼 캐릭터만 있으면 됩니다.
- **테이크 이름은 다시 내보내도 바뀌지 않게** 유지해 주세요. Unity의 반복·이벤트 설정이 테이크 이름에 붙어 있어서, 이름이 바뀌면 설정이 사라집니다. 또 클립 묶음 SO·컨트롤러가 그 클립을 가리키는 참조도 끊깁니다. 끊긴 칸은 오류 없이 코드 모션으로 돌아가므로 눈치채기 어렵습니다.

### R3. 발리스타 (알림)

- `Ballista.fbx`에는 발리스타 무기 아마추어가 없습니다(무기는 메시만, `아마츄어`는 캐릭터). `Barllista` 테이크(활시위 등)를 쓰려면 발리스타 모델에도 같은 이름·자리의 아마추어가 있어야 합니다.
- 지금 활시위·재장전은 View 코드가 움직이고 있습니다. 무기를 클립으로 바꿀지는 원거리 공격 클립 작업 때 같이 정하겠습니다. 그 전까지는 R1(캐릭터 `BaseModel` 최상위)만 맞춰 주시면 됩니다.

### R4. Unity 가져오기 설정 (어느 쪽이 할지 알려 주세요)

View 쪽에서 해도 됩니다. 다만 같은 `.meta`를 동시에 고치면 충돌하니 한쪽이 맡겠습니다.

- **Idle 클립:** Animation 탭에서 Loop Time 켜기.
- **사건 클립(공격 등):** Loop Time 끄기, Events에 함수 이름 `ClipStart`·`ClipEvent`·`ClipEnd`를 **하나씩** 찍기. 순서는 시작 ≤ 이벤트 타이밍 ≤ 끝이고, 끝 이벤트 시각이 재생 길이입니다. 이벤트 타이밍은 이펙트·소리·피격이 시작되는 기준점입니다(원거리는 발사 순간). 다른 이름의 이벤트가 있으면 DB 검사에서 막힙니다.
- **안 쓰는 테이크:** `Action`, `BaseModel|BaseModelAction`, `BaseModel|아마츄어Action`, `.001` 사본은 지우셔도 되고 그대로 두셔도 됩니다(쓰지 않음).
- **Animator Controller:** Idle 상태만 둡니다(파라미터·공격·피격 상태 없음). 유닛마다 Idle만 다르면 Override Controller로 바꿉니다.

### R5. 확인 부탁

1. 적 `FixSo`·`Sabow`·`Cannon`이 각각 어느 전투 유닛인지 알려 주세요(유닛 외형 표에 연결하려고).
2. 적 테이크 9개가 적 FBX 4개에 똑같이 들어 있습니다. 동작에는 문제가 없고, View 쪽은 각 적이 자기 파일의 클립을 쓰겠습니다. 정리하실 계획이면 알려 주세요.
3. 피격 모션 계획이 있는지 알려 주세요. 없으면 피격은 지금처럼 코드 반응만 씁니다.
4. Sniper(테이크 없음), Parasol·Cactuscannon(뼈대 없음)의 애니메이션 계획이 있는지 알려 주세요. 없으면 Idle 없이 지금 코드 모션만 씁니다.

## 3. 전투 클립 규칙 (지금 파일들은 모두 지키고 있음)

- Animator가 붙는 오브젝트(FBX 최상위) **자신**은 움직이지 않습니다. 아마추어·뼈대만 움직입니다.
- 루트 모션을 쓰지 않습니다. 위치 이동은 전투 코드가 맡습니다.
- 뼈대 Transform만 움직입니다. 재질·블렌드셰이프 애니메이션은 클립이 끝나도 되돌리지 않습니다.
- 무기 테이크(부속 클립)는 무기 뼈만 움직이고 이벤트를 찍지 않습니다(R2).

## 4. View 쪽에서 하는 것

- 이벤트 컴포넌트 `CombatAnimationEvents`를 유닛 프리팹의 FBX 인스턴스(Animator 있는 오브젝트)에 붙이고, 클립 묶음(SO)을 유닛 외형 표에 연결합니다.
- 위 규칙(이벤트 셋·반복·루트 곡선)은 DB를 만들 때 검사하므로, 어긋나면 부트에서 바로 알 수 있습니다. 단, 루트 곡선 검사는 Unity 에디터에서만 돕니다(곡선 목록을 에디터에서만 읽을 수 있음).
- 프리팹 교체 때 유닛 루트의 BoxCollider(히트 타이밍에 쓰는 몸)도 새 모델 크기에 맞춥니다.
- **임시 처리(2026-10-20):** R1 재내보내기 전까지 View가 에디터 메뉴 `ProjectL > Animation > 아군 클립 경로 변환 (임시)`로 `Animation.fbx` 테이크를 `.anim`(`Prefabs/Characters/Colored/Playerable/AnimationClip/`)으로 복사해 씁니다. 경로는 `Barllista/뼈대/BaseModel/…` → `아마츄어/…`(아마추어 자체 곡선은 뺌), 무기 `Barllista/…` → `Animation/Barllista/…`(Ballista 프리팹 안의 `Animation` 인스턴스)로 바꾸고, 아군 클립 묶음 SO·Idle 컨트롤러의 참조를 복사본으로 바꿉니다. 이름이 `Idle`로 끝나는 테이크 복사본은 Loop Time을 켭니다(FBX 설정에서 꺼져 있어서). FBX의 이벤트 설정을 고치면 도구를 다시 돌려야 반영됩니다. 재내보내기 뒤에는 참조를 FBX 클립으로 되돌리고 복사본은 정리합니다.

## 5. 참고: Framework 문서 반영 부탁

- 이번에 `DataBaseSystem`에 처음으로 MonoBehaviour(`CombatAnimationEvents`, 이벤트 이름표 컴포넌트)가 들어갔습니다. DB 검사가 이 컴포넌트를 찾아야 해서 View가 아니라 DB 쪽(작업 입력 층)에 두기로 정했습니다.
- 범위는 "프리팹에 붙는 작업 입력의 이름표·자리 표시와 그 규칙 함수만, 런타임 상태·`Update`·서비스 참조 없음"입니다.
- `framework-programmer-guide.md` §2 배치 표(DataBaseSystem = SO 타입·Query)에 이 예외를 한 줄 적어 주실 수 있을지 여쭙니다. 문서는 민형님 영역이라 View 쪽에서 고치지 않았습니다.

## 6. 추가(2026-10-19): 곡사 적 `CannonEnemy` 프리팹·공격 클립 이벤트

R5.1의 `Cannon`은 **`Unit_UndeadCannonCart`(언데드 마인카트, 키 25)**입니다(사용자 확인). 원거리 클립 공격(AC-5)이 코드로 준비되어, 이 모델로 처음 원거리 클립을 씁니다. 요청은 아래입니다.

- **프리팹:** `CannonEnemy.fbx` 인스턴스를 유닛 프리팹으로. Animator가 있는 GameObject(FBX 최상위)에 `CombatAnimationEvents` 하나를 붙이고,
  - **발사 지점(`muzzle`)**에 포탄이 나갈 자리(포신 끝)를 지정해 주세요. 포신이 움직이는 뼈가 있으면 그 뼈의 자식 빈 GameObject를 권합니다(포신을 따라 움직임).
  - Animator Controller는 `CannonIdle`만(파라미터·공격 상태 없음). 포신 Idle 테이크가 생기면 레이어를 하나 더 둡니다(R2).
  - `CannonEnemy.fbx`는 가져오기 설정이 Avatar = No Avatar라 모델 최상위에 Animator가 없습니다. 프리팹 최상위에 Animator를 직접 붙입니다(`CombatAnimationEvents`를 먼저 붙이면 Animator가 함께 붙음).
  - 유닛 루트에 몸 BoxCollider(히트 타이밍에 쓰는 몸)를 모델 크기에 맞춰 주세요.
  - `CatusCannonWeaponView`(아군 선인장 대포의 버킷 연출)는 필요 없습니다. 클립이 포신을 움직입니다.
- **공격 클립 이벤트:** 몸 테이크 **`Scull|CannonAttack`**에 `ClipStart`·`ClipEvent`·`ClipEnd`를 하나씩 찍어 주세요. **`ClipEvent`는 포탄이 포신을 떠나는 프레임**입니다(그 순간 포구에서 포탄을 쏩니다). Loop Time은 끕니다. 가져오기 설정을 누가 할지는 R4를 따릅니다.
- **포신 테이크(2026-10-19 재내보내기 `13eb1dbc`):** `CannonBone|CannonAttackBody`는 부속 클립으로 몸 테이크와 같이 돕니다. **이벤트는 찍지 않고** Loop Time만 끕니다. View 클립 묶음 `CombatUnitAnimation_Cannon`에 이미 넣어 두었습니다(R2).
- `구체` 메시는 포탄이 아니라고 들었습니다(우리가 쏘는 포탄과 겹치지 않음).
- **진행(2026-10-19, View가 함 — R4):** `CannonEnemy.fbx.meta`의 Animation 탭 설정을 View 쪽에서 넣었습니다. 테이크 10개를 모두 클립 목록에 두고(기존 참조 유지), `Scull|CannonIdle` Loop Time 켬, `Scull|CannonAttack`에 이벤트 셋(`ClipEvent`는 임시로 0.6초 — 포탄이 떠나는 프레임으로 옮겨 주세요), 포신 테이크는 이벤트 없음. 다시 내보내실 때 테이크 이름이 같으면 이 설정이 유지됩니다. 프리팹(`Prefabs/Characters/Colored/Enemy/CannonEnemy.prefab`)·컨트롤러(`UndeadCannon.controller`)도 만들어져 있습니다.
- 외형 표 항목·표 등록은 호진님 메모 §7과 [Core 인계](2026-10-16-undead-cannon-cart-view-handoff.md)를 따릅니다.

## 7. 추가(2026-10-19): 나머지 적 FBX 가져오기 설정 — View가 함

- **바꾼 파일:** `FixsoEnemy.fbx.meta`·`SabowEnemy.fbx.meta`·`Stage1Boss.fbx.meta`(Animation 탭만, 모델·메시 설정은 그대로). 곡사 적(§6)과 같은 방식입니다.
  - 테이크 9개를 모두 클립 목록에 두고, 클립 ID는 지금 기본 ID 그대로입니다(기존 참조 유지).
  - 파일마다 **자기 Idle만 Loop Time**, **자기 Attack에만 이벤트 셋**(`ClipStart` 0 · `ClipEvent` 정규화 0.4 · `ClipEnd` 끝) — `Scull|FixSoAttack`·`Scull|SabowAttack`·`Scull|Stage1BossAttack`. **`ClipEvent`는 임시 값**이니 실제 타격(근접)·발사(원거리) 프레임으로 옮겨 주세요.
  - 다시 내보내실 때 테이크 이름이 같으면 이 설정이 유지됩니다.
- **프리팹을 만드실 때:** 이 세 모델은 곡사 적과 달리 `Scull`이 FBX 최상위 자식이고 테이크가 `Scull` 자신도 움직입니다. Animator와 `CombatAnimationEvents`는 **FBX 루트(`Scull`의 부모)**에 붙여 주세요(`Scull`에 붙이면 루트 곡선 검사에서 실패). 컨트롤러는 자기 Idle 하나만.
- **R5 질문 그대로:** `FixSo`·`Sabow`가 어느 전투 유닛인지 알려 주세요. View 클립 묶음(`CombatUnitAnimation_FixSo`·`_Sabow`·`_Stage1Boss`)은 만들어 두었고, 프리팹이 생기면 외형 표에 연결합니다.
