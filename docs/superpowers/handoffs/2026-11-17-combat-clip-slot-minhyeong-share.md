# 전투 클립 칸 다시 짜기 — 민형님 공유 (2026-11-17, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 민형님(캐릭터 모델·애니메이션).
> 원본: [클립 칸 다시 짜기 설계](../specs/2026-11-17-combat-unit-clip-slot-redesign-design.md)(CS1~CS11)·[계획](../plans/2026-11-17-combat-unit-clip-slot-redesign-plan.md) §5. 컴파일·Unity 확인 문제 없음(사용자, Ua~Ud). 커밋은 사용자.
> 이 메모는 **민형님 파일(유닛 프리팹)이 바뀐 것**과 **앞으로 클립을 만들 때 지킬 규칙**만 적습니다.

## 1. 바뀐 민형님 파일 — 유닛 프리팹의 `CombatAnimationEvents`

- 옛 칸 셋(`muzzle` 발사 지점·`effectPoint` 이펙트 지점·`additionalAttackPoints` 둘째 쌍)을 **Event Points 목록 하나**(`eventPoints`)로 합쳤습니다.
- 옮기기 도구(`ProjectL > Combat > 클립 칸 옮기기 (1)`)가 프리팹 11개의 값을 옮겼습니다. 번호 규칙:

| Event Points 칸 | 옛 칸 |
|---|---|
| 0 | 첫 쌍의 발사 지점(`muzzle`) |
| 1 | 첫 쌍의 이펙트 지점(`effectPoint`) |
| 2·3 | 둘째 쌍의 발사·이펙트 지점(에블린 둘째 총) |

- 끝의 빈 칸은 줄였습니다. 지점 Transform 자체(위치·회전)는 그대로입니다.
- 11-17 새 클립 연결 메모([2026-11-17-combat-unit-new-clips-minhyeong-share](2026-11-17-combat-unit-new-clips-minhyeong-share.md))에 적은 로즈(Sniper) 프리팹의 `Muzzle`·`EffectPoint`도 이제 Event Points 0·1입니다.

## 2. 새 모델 프리팹을 만들 때

- Animator가 있는 GameObject에 `CombatAnimationEvents` 하나를 붙이고, 발사·이펙트 지점이 있으면 **Event Points**에 위 번호 규칙대로 넣습니다.
- 원거리 공격의 이벤트 줄(클립 묶음 Events)의 "지점"과 공격 연출 큐의 `ActorEventPoint`가 이 번호를 씁니다. 가리키는 번호가 비어 있으면 Play 시작 때 멈춥니다.

## 3. 클립 만들 때 지킬 규칙(클립 묶음 SO가 읽는 것)

클립 묶음 SO(`CombatUnitAnimation_*`)의 칸(공격·피격) 하나 = **동작**입니다: 앞 단계(공격만) → 본 단계 → 뒤 단계. 단계마다 몸 + 탑승물(말) + 부속(무기·올가미) 클립이 같은 시각으로 돕니다.

| 클립 | 이름표(Animation Event) |
|---|---|
| 본 단계 몸 클립 | `ClipStart`·`ClipEnd` 하나씩(필수), `ClipEvent` 하나(필수 — 공격 임팩트·발사 순간, 피격 타이밍), `ClipEvent2`·`ClipEvent3`(선택) |
| 앞·뒤 단계 몸 클립 | `ClipStart`·`ClipEnd` 하나씩만. `ClipEvent`류를 찍으면 Play 시작 때 멈춥니다 |
| 탑승물(말)·부속(무기·올가미) 클립 | 이벤트를 찍지 않습니다. 몸·다른 클립과 같은 뼈를 움직이면 안 됩니다(곡선 겹침 검사) |

- `ClipEvent`·`ClipEvent2`·`ClipEvent3`는 시작과 끝 사이면 **순서가 자유**입니다(2026-11-17 — Event2가 Event1보다 앞서도 됨).
- 민형님 도구가 새 클립마다 넣는 자리값 `ClipEvent`(길이 절반)는 본 단계 몸 클립에만 두고, 앞·뒤 단계로 쓸 클립에서는 지워 주세요(예: 카우보이 `CowBoy_PullBack`은 지웠습니다).
- 루트 곡선 금지·Idle 반복은 지금 규칙 그대로입니다. **Idle(말·무기 Idle 포함)은 컨트롤러 층**(`CharacterAnimationSetup`)이 돌립니다 — 클립 묶음에는 Idle 칸이 없습니다.
- 말 클립은 부속이 아니라 **탑승물** 칸에 넣습니다(지금 `CombatUnitAnimation_Sabow`·`_Bommber`·`_Cowboy`의 말). 카우보이 올가미는 부속입니다.

## 4. 클립 묶음 SO의 Events 칸

- 머리말에 적었듯 **`[Event1, 0, 0]`은 적지 않아도 늘 맨 앞에 있습니다**(공격 = 공격 애니메이션 임팩트 타이밍, 피격 = 피격 타이밍). 묶음 14개의 이 줄은 지웠습니다.
- 줄을 더하는 것은 두 발 이상 쏘거나(원거리 — 줄마다 한 발, 지점 = Event Points 번호) 여러 번 맞을 때(근접 — 줄마다 맞는 순간)입니다. 예: 에블린 `[Event2, 2, 0]` = ClipEvent2에 둘째 총으로 한 발.
- 블렌드(In·Between·Out)는 칸마다 켜고 초를 정합니다. 지금은 웨이드 공격만 켜 있습니다(0.1·0.1·0.2).

## 5. 바꾸지 않은 것

- FBX·클립 파일 자체, 컨트롤러, 프리팹의 다른 컴포넌트는 바꾸지 않았습니다(카우보이 PullBack의 자리값 `ClipEvent` 하나만 지움 — 사용자 결정).
- Factory·서비스 연결 줄 변경 없음.
