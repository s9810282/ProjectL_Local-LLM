# 전투 유닛 새 애니메이션 클립 연결 — 민형님 공유 (2026-11-17, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 민형님(캐릭터 모델·애니메이션).
> 기준: 민형님 `4f2f496`(FBX 교체)·`fbb9780d4`(유닛마다 Idle·Attack·Hit·추가 클립, 폴더 정리) 뒤 develop 병합.
> 설계·기록: [연결 설계](../specs/2026-11-17-combat-unit-new-clips-wiring-design.md), [Task 계획](../plans/2026-11-17-combat-unit-new-clips-wiring-plan.md) 실행 기록 §5.
> 커밋 전 작업 트리 기준입니다. Unity 확인은 사용자가 마지막에 한 번 합니다.

## 1. 새 클립을 전투에 연결함

전투는 유닛마다 클립 묶음 SO(`DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/`)의 공격 칸·피격 칸에서 클립을 골라 재생합니다. 새 세트(11-16)로 바꾼 값은 아래와 같습니다.

| 묶음 | 공격 = 몸 + 부속 | 피격 = 몸 + 부속 |
|---|---|---|
| Enemy/Cannon | `Enemy_Cannon_Attack` + `Cannon_Attack` | `Enemy_Cannon_Hit` + `Cannon_Hit` |
| Enemy/FixSo | `Enemy_Miner_Attack` | `Enemy_Miner_Hit` |
| Enemy/Heavy | `EnemyHeavy_Attack` | `EnemyHeavy_Hit` |
| Enemy/Sabow | (민형님이 넣은 새 클립 그대로) | `Enemy_Horse_Shovel_Hit` + `Enemy_Horse_Hit` |
| Enemy/Stage1Boss | `Stage1Boss_Attack` | `Stage1Boss_Hit` |
| Playerable/Akimbo | `Akimbo_Attack` | `Akimbo_Hit` |
| Playerable/Ballista | `Ballista_Sniper_Attack` + `Ballista_Attack` | `Ballista_Sniper_Hit` + `Ballista_Hit` |
| Playerable/Bommber | (민형님이 넣은 새 클립 그대로) | `Horse_Player_Boomber_Hit` + `Horse_Player_Hit` |
| Playerable/Catus | `CactusCannon_Attack` + `Slingshot_Attack` | `CactusCannon_Hit` + `Slingshot_Hit` |
| Playerable/Cowboy | `CowBoy_Attack` + `Horse_Attack`·`Lasso_Attack` | `CowBoy_Hit` + `Horse_Hit`·`Lasso_Hit` |
| Playerable/Shotgun | `ShotGun_Attack` | `ShotGun_Hit` |
| Playerable/Sniper(새 묶음) | `Sniper_Attack` | `Sniper_Hit` |

- 민형님이 고쳐 두신 Ballista 묶음의 공격 칸도 새 세트로 바꿨습니다(옛 `BaseHumanBone_Cannon_Attack` + `BarristarBone_Ballistar_Attack` → 위 표).
- 카우보이의 옛 공격 클립(`BaseHumanBone_lassoAttack.001` + `lasso.001_Attack.002`)은 새 모델과 뼈 경로가 하나도 맞지 않아(0/217) 11-15 뒤로 공격 동작이 안 나왔습니다. 새 클립으로 바꾸면서 풀립니다.
- 새 클립은 모두 전투 규칙에 맞았습니다: 몸 클립은 이벤트 셋(`ClipStart`·`ClipEvent`·`ClipEnd`) 하나씩·반복 꺼짐·루트 곡선 없음, 부속 클립은 이벤트 없음·몸 클립과 같은 곡선(경로 + 속성) 없음. 감사합니다.
- **`ClipEvent`가 모두 클립 길이의 정확히 절반에 있습니다.** 실제 타격·발사 프레임은 우리가 Unity에서 보고 `.anim`의 이벤트 시각을 고칩니다(사용자 결정). 고친 값은 이 메모 §2에 덧붙입니다.

## 2. 민형님 클립을 고친 것

- `Prefabs/Characters/New/Playerable/Akimbo/Animation/Akimbo_Attack.anim`: 두 번째 발사 이벤트 **`ClipEvent2`를 0.90625초(길이 1.208초의 3/4)에 더했습니다.** Akimbo는 두 총을 차례로 쏘고, 전투의 발사 목록이 "이벤트 1 → 총구 0, 이벤트 2 → 총구 1"이라 이 이벤트가 없으면 부트가 멈춥니다("발사 1의 이벤트 Event2가 없습니다").
  - **이 클립을 다시 내보내실 때 `ClipEvent2`가 빠지면 부트가 멈춥니다.** 다시 내보내시면 알려 주시거나, 같은 자리에 `ClipEvent2`를 넣어 주세요(시각 규칙: `ClipEvent` ≤ `ClipEvent2` ≤ `ClipEnd`).
- (CW-c 뒤 덧붙임) 이벤트 시각을 고친 클립과 값.

## 3. 민형님 프리팹을 고친 것

| 프리팹(`Prefabs/Characters/New/`) | 바꾼 것 | 이유 |
|---|---|---|
| `Enemy/EnemyCannon`·`Playerable/Ballistar`·`Playerable/CactusCannon`·`Enemy/EnemyShabow`·`Playerable/Boomber` | 안쪽 모델의 `BoxCollider`를 빈 루트로 옮김(같은 월드 자리 — 중심에 안쪽 모델 위치를 더함) | 전투는 몸 모양을 **프리팹 루트의** `BoxCollider`에서만 읽습니다. 안쪽에 있으면 몸 상자 대신 점 하나로 접촉을 계산하고 전투마다 경고가 났습니다(10-29부터). **정할 것:** Ballistar·CactusCannon은 모델을 뒤로 옮겨 둔 만큼 상자도 칸 가운데보다 뒤에 있어(루트 기준 z 범위 약 −0.75~−0.05, −0.90~−0.20) 앞에서 온 직선 탄이 상자에 안 닿을 수 있습니다 — 상자를 칸 가운데로 다시 잡을지 사용자 결정 대기 |
| `EnemyCannon`·`Ballistar`·`CactusCannon` | 루트 태그 `EditorOnly` → `Untagged` | 11-11에 들어간 태그. 빌드에서 지워질 수 있어서(사용자 결정) |
| `Playerable/CactusCannon` | 새총(`Slingshot`) 렌더러 뼈 목록에 6번째 뼈 `뼈대.004_end`를 더함 | 새 FBX의 새총 메시는 뼈 6개인데 프리팹 렌더러는 5개라 Unity가 새총을 그리지 않았습니다 |
| `Playerable/Sniper`(새 변형 프리팹) | 루트 크기 0.7, 모든 오브젝트 레이어 6, 루트에 `BoxCollider`(1 × 1.35 × 1, 중심 높이 0.63)·`CombatAnimationEvents`·`CombatUnitRagdoll`(꺼짐), 라이플 `큐브.001` 아래 `Muzzle`·`EffectPoint`(라이플 기준 (−1.2673, −0.0032, −0.0536) = 총구 끝 단면 가운데) | 마탄 메인(로즈)을 새 모델로 바꾸려고(새 Akimbo와 같은 모양) |

- **FBX를 바꿀 때 주의할 것:** 풀린 프리팹(Unpacked)은 FBX가 바뀌어도 렌더러의 뼈 목록이 따라가지 않습니다. 메시의 뼈 수가 바뀌면 그 메시가 안 보입니다(새총이 이 경우). FBX를 바꾼 뒤에는 풀린 프리팹의 메시가 보이는지 한 번 봐 주세요.
- 클립은 Animator가 붙은 오브젝트 기준 경로로 붙습니다. 캐논 셋·라이더 둘처럼 빈 루트로 감싸도 Animator가 안쪽 모델에 있으면 경로가 맞습니다(이번에 다 대조함).
- **확인 중:** 캐논 셋(Ballistar·CactusCannon·EnemyCannon)은 안쪽 모델의 Animator가 꺼져 있습니다(11-11). 전투에서는 우리 클립 채널이 Animator를 켜서 괜찮지만, Unity 클립 미리보기에 이 프리팹을 넣으면 안 움직입니다. 사용자가 확인하는 중이고, 정해지면 덧붙입니다.

## 4. 지운 클립

사용자 지시("아예 안 쓰는 클립은 제거")로, 연결을 끝낸 뒤에도 어디서도 참조하지 않는 `Prefabs/Characters/New` 아래 클립 **47개(+ `.meta`, 약 249MB)**를 지웠습니다. 목록은 [연결 설계](../specs/2026-11-17-combat-unit-new-clips-wiring-design.md) 부록 A에 있습니다. 옛 `BaseHumanBone_*` 세트, `Playerable_*`·`Enemy_*` 앞붙이 복사본, `.001`·`.002`·`_Duplicate` 뒷붙이 복사본이 대부분입니다. 지운 파일은 git 이력에 있어 되돌릴 수 있습니다.

- **남긴 것:** 새 세트 중 아직 안 쓰는 8개(`Akimbo_Landing`·`Akimbo_SpecialAttack`·`CowBoy_PullBack`·`Horse_PullBack`·`Lasso_PullBack`·`Gravedigger_Attack`·`Gravedigger_Hit`·`Gravedigger_Recovery`) — 스킬·특성 동작 칸을 따로 설계할 때 쓸 것. 참조가 남은 옛 클립 둘(`Stage1Boss/…/BaseHumanBone_Spical_Attack`, `Boomber/…/BaseHumanBone_Horse_Shovel_Attack`).
- 남은 같은 내용 클립: `EnemyCannon/…/Cannon_Idle`과 `Ballistar/…/Ballista_Idle`(곡선 없는 무기 Idle) 한 쌍.

## 5. 전투가 클립에 요구하는 것(다시 정리)

- 컨트롤러에는 **Idle만** 둡니다(공격·피격 상태·파라미터·루트 모션 없음). 공격·피격은 묶음 SO가 고르고 전투 재생이 위층에 얹습니다.
- 사건 클립(공격·피격): 이벤트 셋 하나씩, 반복 꺼짐, 루트(Animator 오브젝트 자신) 곡선 없음.
- 부속 클립(말·무기·올가미): 이벤트 없음, 몸 클립과 같은 곡선(경로 + 속성) 없음, 길이는 몸 클립과 같게.
- 원거리 유닛 프리팹: `CombatAnimationEvents`의 총구(`muzzle`) 필수.
