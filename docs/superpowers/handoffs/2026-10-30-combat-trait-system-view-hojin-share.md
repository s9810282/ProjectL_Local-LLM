# 전투 특성 시스템 View 대응 — 호진님 공유 (View DB 표, 2026-10-30, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 호진님(View DB 표·외형 에셋).
> 상태: 코드·에셋 완료, Unity 컴파일 확인(사용자), 재생 확인 일부 대기. TS-a~TS-e는 커밋(`034b0b26`), TS-f는 커밋 전.
> 관련 문서: [View 설계](../specs/2026-10-29-combat-trait-system-view-design.md) · [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) · [View 계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §4·§7 · [특성과 발동 시기](../viewDocs/combat-trait-activation-timing.md) · 앞 공유 [특성 발동 연출](2026-10-29-combat-trait-activation-hojin-share.md)

## 요약

- 상태 외형 표에 줄 여섯이 생겼습니다(§1). 아이콘은 비어 있고 문구는 초안입니다.
- 특성 외형 표(`TraitVisualData`)에 흔적 칸 둘이 생겼고, 땅 다지기·먼지 연막 에셋에 임시 흔적을 연결했습니다(§2).
- 발동 연출 줄(`TraitActivationVisual`)이 바뀌었습니다: 공격 표시 덮어쓰기 제한이 풀렸고, 세 번째 형태 "유닛 행동" 칸이 생겼습니다(§3). 희생 에셋에 줄 하나를 넣었습니다.
- 바꾼 에셋 중 셋(땅 다지기·먼지 연막·희생)은 호진님 `6b0755be2`의 확인용 에셋입니다.

## 1. 상태 외형 표 — 새 줄 여섯

| 에셋 (`DataBaseSO/ViewGroup/UnitStatusVisual/Item/`) | 종류 `UnitStatusKind` | 구분 | 설명 틀(초안, `{0}` 칸 하나) |
|---|---|---|---|
| `UnitStatusVisual_Disarm` | `Disarm = 4` | 디버프 | "공격할 수 없습니다. 이 진영 턴이 {0}번 더 끝나면 풀립니다." |
| `UnitStatusVisual_AnkleBreaker` | `AnkleBreaker = 5` | 디버프 | "스스로 이동할 수 없습니다. 이 진영 턴이 {0}번 더 끝나면 풀립니다." |
| `UnitStatusVisual_ArmorBreak` | `ArmorBreak = 6` | 디버프 | "방어력이 감소했습니다({0}회 중첩). 스테이지가 끝날 때까지 유지됩니다." |
| `UnitStatusVisual_AttackBonus` | `AttackBonus = 7` | 버프 | "특성으로 얻은 공격력 +{0}." |
| `UnitStatusVisual_Shield` | `Shield = 8` | 버프 | "피해를 {0}만큼 먼저 막습니다." |
| `UnitStatusVisual_Acceleration` | `Acceleration = 9` | 버프 | "다음 공격의 공격력 +{0}. 공격하면 소모됩니다." |

- 표 `UnitStatusVisualDataTable.asset`에 여섯 줄을 등록했습니다.
- **에셋을 지우거나 표에서 빼면 전투 진입이 멈춥니다.** 전투 진입 때 상태 줄 표(`Display/Unit/CombatUnitStatuses`)가 줄마다 외형이 있는지, 틀이 그 줄의 값 수(여섯 모두 하나)를 받는지 확인합니다.
- 부탁: 아이콘 채우기, 문구 다듬기(틀 칸 `{0}`은 남겨 주세요).

## 2. 특성 외형 표 — 흔적 칸 둘

- 새 칸(`TraitVisualDataSO`):
  - `trailPrefab`(선택) — 그 특성이 남긴 흔적 칸에 놓을 프리팹입니다. 비우면 흔적이 그려지지 않고 경고가 한 번 나옵니다.
  - `trailLocalOffset` — 칸 표면에서 옮기는 값(필드 기준 월드 단위)입니다. 검사는 유한한 수인지만 봅니다.
- 어느 특성이 흔적을 남기는지는 Core가 정합니다. 이 칸은 외형만 줍니다.
- 연결한 에셋: `TraitVisual_GroundCompaction`(340) → `Prefabs/Default/Environment/Trail/Trail_GroundCompaction.prefab`, `TraitVisual_DustScreen`(350) → `Trail_DustScreen.prefab`. 둘 다 임시 아트(반투명 Quad, 칸보다 작게 0.8, 정렬 순서 −2)입니다.
- 두 에셋에 `activations: []` 한 줄도 적었습니다. Unity가 저장할 때 넣는 기본값과 같습니다.

## 3. 발동 연출 줄 — 바뀐 것

- **공격 표시 덮어쓰기 제한이 풀림:** 옛 검사 "공격 표시는 추가 공격 줄(발동 종류 목록)에만"을 걷었습니다. 추가 공격인지는 이제 발동 종류 이름이 아니라 Core 사실의 추가 공격 결과(`ChildAttack`)로 정합니다. 칸은 어느 줄에도 있지만, 실제로 쓰이는 것은 추가 공격 결과가 있는 발동뿐입니다. 클립 유닛에 계열이 다른(근접/원거리) 덮어쓰기를 넣으면 경고 한 번 뒤 그 유닛의 공격 표시로 칩니다.
- **새 칸 `unitAction` — 유닛 행동(세 번째 형태):** 그 발동이 유닛에게 시키는 화면 동작입니다. Core 위치는 바뀌지 않습니다.

| 칸 | 값 | 뜻 |
|---|---|---|
| `actor` | `None`(기본) / `Source` / `Targets` | 누가 하나. `None`이면 행동 없음(지금 화면 그대로) |
| `place` | `Stay` / `GuardFront` / `TowardTarget` | 제자리 / 지켜진 유닛 앞(공격자 쪽, 대신 맞기에서만) / 대상 쪽 |
| `distance` | 0~1.5(칸 비율) | 기준점에서 떨어지는 거리 |
| `look` | `Walk` / `Hop` | 옮기는 모양(뛰기 높이는 전투 연출 설정 값) |
| `outSeconds`·`holdSeconds`·`returnSeconds` | 0 이상 | 나가기(제자리면 돌기)·머물기(사건 순간 행동만)·돌아오기 |
| `face` | `Keep` / `Target` / `Attacker` | 머무는 동안 볼 쪽(`Attacker`는 대신 맞기에서만) |

  - DB 검사는 범위·열거 값만 봅니다(범위 밖이면 특성·발동 종류를 붙인 오류로 DB 생성이 멈춤). 어느 발동 종류가 가로채기인지는 Core가 정하므로 DB가 막지 않습니다.
  - 실행 때 경고 한 번 뒤 고쳐 쓰는 경우: 사건 순간 발동에 `GuardFront`(→ `Stay`), 추가 공격 발동에 행동 값(→ 행동 건너뜀), 대신 맞기 줄의 `actor`가 `Source`가 아님(→ 행동 건너뜀).
- **희생 에셋에 넣은 줄:** `TraitVisual_Sacrifice` 발동 줄 하나 — `kind: 110`(`DamageRedirected`), 큐 빈 값, 행동 `Source`·`GuardFront`·0.6·`Hop`·나가기 0.2·머물기 0·돌아오기 0.25·`Attacker`(임시 값, 화면을 보고 다듬을 것). 이 에셋에 없던 흔적 칸 둘도 기본값으로 적었습니다.

## 4. 남은 에셋 작업 (부탁)

- 새 특성 22종의 발동 연출 줄(큐·행동)은 아직 비어 있습니다. 어떤 특성이 어떤 발동 종류를 언제 내는지는 [특성과 발동 시기](../viewDocs/combat-trait-activation-timing.md)와 계획 §4 표에 있습니다. 줄을 채우면 코드 변경 없이 나옵니다.
- 상태 외형 여섯의 아이콘(§1).
- 흔적 프리팹 두 개의 실제 아트(§2).

## 5. 2026-10-31 이펙트 큐 새 칸 `stayInPlace`, 발동 줄 새 칸 `keepWhileStatus`·`recallKinds` (미전달)

- 모든 이펙트 큐 항목(특성 외형 표 발동 줄의 큐, 반응 묶음의 피격·사망 칸, 공격 표시 칸)에 bool 칸 `stayInPlace`(인스펙터 "Stay In Place")가 생겼습니다([이펙트 따라가기·회수 설계](../specs/2026-10-31-combat-effect-follow-recall-design.md) FE1).
- 꺼짐(기본): 자리가 ActorBody·SubjectBody이면 낸 뒤 그 유닛의 위치를 따라갑니다(위치만, 회전은 낼 때 그대로). 따라가던 유닛 표시가 사라지면 이펙트도 바로 멈춥니다.
- 켬: 지금처럼 낸 자리에 남습니다. 다른 자리(클립 이펙트 지점·발사 지점·사건 칸·접촉)는 이 칸과 상관없이 늘 낸 자리입니다.
- 공유 반응 에셋 `CombatUnitReaction_Default`의 피격·사망 이펙트 두 줄은 지금 모습을 지키려고 켜 두었습니다(사망 이펙트가 죽은 유닛 표시와 함께 끊기지 않게).
- 발동 줄(특성 외형 표의 `activations` 항목)에 칸 둘이 생겼습니다(설계 §3, 줄 그리기에도 보임):
  - `keepWhileStatus`(Keep While Status): 켜면 그 줄의 몸 자리 이펙트를 그 유닛에 이 특성이 건 상태가 켜져 있는 동안 유지하고, 상태가 사라지면(턴 만료·소비·제거·되돌리기 등) 자연스럽게 회수합니다. 이펙트는 반복(Loop) 파티클이어야 오래 남습니다.
  - `recallKinds`(Recall Kinds): 같은 특성의 이 발동 종류가 그 유닛에 오면 회수합니다(예: 단독 행동 보호막 부여 210 줄에 230 ShieldRemoved). 공통 상태 목록 밖의 효과용이고, 자기 종류·겹침은 DB 검사가 막습니다.
  - 같은 유닛에 그 줄 이펙트가 이미 켜져 있으면 새로 내지 않습니다. 칸 자리 이펙트는 유지되지 않습니다.
