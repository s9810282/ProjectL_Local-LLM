# 사용 가능한 특성 37종의 Activation 연출 작성 설계

> **현재 상태 확인:** 가용성·실제 발동 Kind·저작 제약은 [2026-10-31 참조 문서](../viewDocs/trait-effects-and-activation-reference.md)를 따른다. 아래 37종/47줄은 과거 코드에서 세운 작성안으로 보관한다.

- 날짜: 2026-10-30. 작성: Codex, View 작업.
- 상태: **설계 초안. 이번 요청은 계획까지이며 코드·SO·프리팹 구현은 시작하지 않는다.**
- 기준: `d4809683b` 위 현재 작업 트리의 Core·View 코드. 특성별 발동 시기 참고는 [현존 특성 표](../viewDocs/combat-trait-activation-timing.md), 이번 작성 대상과 연출 제안의 원본은 이 문서 §4다.
- Task: [Activation 연출 작성 계획](../plans/2026-10-30-trait-activation-authoring-plan.md).
- 사용: [특성 미리보기 안내](../viewDocs/trait-activation-preview-guide.md).

## 1. 범위와 현재 상태

현재 Core 등록은 42종이고 `DeferredCombatFeatures`가 허용하는 특성은 37종이다. 집중·이동 후 공격 할인·가시밭·사령술·재생은 제외한다. 위압의 우선도 감소도 작성하지 않는다. 로스터의 성장 선택 노출 여부와 Core 지원 여부는 구분한다.

이번 목표는 37종이 발동했다는 것을 기존 사건 재생 경로로 읽을 수 있게 하는 것이다. 특성 발동 규칙, 피해 계산, 사건 순서, 상태의 남은 턴을 View에 추가하지 않는다. 타이밍·Magnitude·대상 수를 근거 없이 추측해 효과를 만들지 않는다.

| 항목 | 현재 상태 | 이번 설계의 처리 |
|---|---|---|
| TraitVisual 에셋 | 40종. PrecisionShot·ContemptForWeak 없음 | 두 에셋과 표 등록을 추가하는 Task 포함 |
| Activation 줄 | 희생 / DamageRedirected 1개 | 재사용·대조. 덮어쓰지 않음 |
| 흔적 | 땅 다지기·먼지 연막 프리팹 연결 있음 | 기존 지속 표시 유지. 생성 순간 큐만 보완 |
| Render Effect 표 | Boom(2)·GunFire(3)·Hit(4), 세 항목 | 회복·버프를 폭발·피격 효과로 대체하지 않음. 필요한 리소스를 먼저 확보 |
| 추가 공격 | ChildAttack이 있으면 줄 없이도 기본 공격 재생 | 발동 식별 큐만 추가. 공격 표시 덮어쓰기는 초판에 비움 |
| 사냥꾼의 표식 | 현재 StatusApplied·StatusConsumed·StatusRemoved 경로 | 공통 버프·디버프/약점 노출 개편 예고로 적용 대기. 현재 계약용 초안만 작성 |

Core 개편 예고의 원본은 [특성 시스템 계획 §7.14](../plans/2026-10-30-combat-trait-system-view-plan.md#714-core-예고--특성-효과를-공통-버프디버프로-2026-10-30-천명님-메시지-사용자-전달-코드-변경-없음)이다. 아직 오지 않은 약점 노출의 Kind·수명 필드를 이 계획에서 새로 정하지 않는다.

## 2. 작성 원칙

1. **발동 줄 하나의 키는 (TraitKey, Activation Kind)다.** 같은 키를 둘로 나누거나 피해량·강화 여부로 View 분기하지 않는다.
2. **기본은 짧은 순간 큐다.** 실제 걸음·넉백·끌어오기·공격·피격·사망·흔적은 기존 결과 표시가 맡는다. 같은 동작을 Activation에서 다시 만들지 않는다.
3. **유닛 행동은 희생만 사용한다.** 다른 36종은 초판 `UnitAction=None`이다. 강화마다 뛰거나 대상을 따라가면 기존 공격·피격과 자리 대여가 충돌하고, 효과를 읽기 어렵다. 포효는 기존 전용 동작을 유지한다.
4. **정리 발동마다 줄을 만들지 않는다.** 상태의 턴 차감·만료·사망 정리, 흔적 수명 변화는 기존 사실·HUD·흔적 소비자가 반영한다. 같은 사실을 별도 타이머나 지속 이펙트로 다시 소유하지 않는다.
5. **Magnitude 0을 일괄 제외하지 않는다.** 도발 선택·빈 추가 공격·막힌 넉백·보호막 리셋 등에도 뜻이 있다. 줄의 큐는 0 여부로 분기하지 않는다.
6. **수치·좋고 나쁨을 유추하지 않는다.** 결속 DefenseChanged는 증가와 감소가 같은 Kind이고 Magnitude가 절댓값이므로 중립적인 재배치 표시를 쓴다. 피해 배율·보호막 잔량을 파티클 수로 환산하지 않는다.
7. **기본 흔들림·특성 소리는 추가하지 않는다.** 작은 반복 효과와 다중 대상 발동에 화면 흔들림을 넣지 않는다. 특성 줄은 소리 칸이 없으며 추가 공격은 기존 공격 소리를 사용한다.

## 3. 시점과 자리

아래 표의 시점은 모두 **해당 발동을 View가 소비하는 Start**다. Core 계산 시각이나 실제 체력 감소 시각을 뜻하지 않는다. 일반 특성 큐는 Start만 지원하고 지연을 줘도 뒤 사건을 막지 않는다. 초판 지연은 0, 연출 길이는 약 0.15~0.35초를 출발값으로 삼고 수동 재생에서 조정한다. 길이는 규칙 값이 아니다.

- **ActorBody:** 발동 유닛 몸 하나. 공격자·피해 공로자와 항상 같지는 않다.
- **SubjectBody:** Core `TargetIds`에 있는 대상 각각의 현재 몸. 대상 여럿이면 같은 효과가 대상마다 나온다. 본체를 찾을 수 없는 대상에는 기존 경로대로 나오지 않는다.
- **EventCell:** Core `TargetCells` 각각의 칸 표면. 대상 칸을 View가 다시 계산하지 않는다.

특성 발동의 자리에는 ActorEffectPoint·ActorMuzzle·Contact를 쓰지 않는다. 추가 공격 덮어쓰기에도 ActorEffectPoint·ActorMuzzle 사용이 제한된다. 어느 유닛이 특성을 소유할지 고정되지 않기 때문이다.

### 3.1 기존 타이밍의 한계

- 단일 대상의 ResultOrigin 있는 후속은 기존 맞는 순간 묶음을 따른다. 대상 여럿인 발동은 주체가 출처 유닛이 되어 공격 종료 뒤에 보일 수 있다(R11). 큐 지연·원래 대상 복제·발동 쪼개기로 이를 우회하지 않는다.
- 추가 공격의 실제 접촉은 공격 재생이 소유한다. 발동 Start에 붙이는 큐는 **추가 공격 예고/발동 표시**이며, 명중 폭발을 대신하지 않는다. 명중 폭발이 필요하면 호환되는 공격 표시의 Hit/Contact 큐로 별도 조립 검토가 필요하다. 초판에는 그 덮어쓰기를 만들지 않는다.
- 희생 큐의 자리 스냅샷은 수호자가 나가기 전이다. 초판에는 수호자 자기 몸의 발동 표시만 둔다. 이동 뒤 몸의 피격 효과는 기존 피격 표시가 맡는다. 이동 뒤 위치를 맞추려고 고정 delay를 넣지 않는다.
- 희생은 나가기 완료 뒤 공격, 맞는 순간 뒤 해당 수호자의 후속 정리 완료를 기다려 복귀한다. `holdSeconds`는 쓰지 않으며 사망하면 복귀하지 않는다. 복귀 끝 전용 특성 큐 시점은 없다.

## 4. 37종 작성 표

이 표의 **작성 줄**만 실제 에셋 작성 대상이다. 여러 줄은 각각 별도 Kind다. 괄호 안 표현은 붙일 리소스의 의미이며, 아직 존재하지 않는 EffectType이나 프리팹 이름으로 간주하지 않는다. 리소스는 §5에서 확보한다. 같은 종류의 효과를 재사용하되 색 변경·크기 변경은 프리팹/리소스 값에서 결정한다. 현재 큐 입력에 없는 색·스케일 필드를 가정하지 않는다.

### 4.1 피해와 방어 우회 — 10종, 10줄

| 키 / 특성 | 작성 줄 | 붙일 큐와 자리 | 기존 표시와의 관계 |
|---|---|---|---|
| 30 관통 볼트 | DamageModified | SubjectBody: 얇은 관통 잔광 | 실제 투사체·관통 접촉은 그대로 |
| 60 산탄 | DamageModified | SubjectBody: 짧은 산개형 피해 강조 | 거리나 배율에 따른 크기 계산 없음 |
| 130 도어 브리칭 | DefenseBypassed | SubjectBody: 방어를 가르는 짧은 선 | 방어값을 다시 계산하지 않음 |
| 140 약자 멸시 | DamageModified | SubjectBody: 피해 강조 고리 | 공격력 비교를 View에서 하지 않음 |
| 150 정밀 사격 | DefenseBypassed | SubjectBody: 좁은 관통선 | 첫 수신자를 다시 선정하지 않음 |
| 320 진지 구축 | DamageModified | SubjectBody: 짧은 방호막 반짝임 | 발동 소유자가 아닌 실제 보호받은 대상에 표시 |
| 380 고정 사격 | DamageModified | SubjectBody: 묵직한 피해 강조 | 공격 동작 추가 없음 |
| 400 마무리 사격 | InstantDefeat | SubjectBody: 짧은 처형 표시 | 죽음·사라짐은 기존 사망 경로. 별도 사망 연출을 시작하지 않음 |
| 410 추적 사격 | DamageModified | SubjectBody: 조준선 형태의 피해 강조 | 체력 조건을 다시 검사하지 않음 |
| 430 일점 포격 | DamageModified | SubjectBody: 집중된 피해 강조 | 적 수를 View에서 세지 않음 |

### 4.2 공격력과 회복 — 6종, 8줄

| 키 / 특성 | 작성 줄 | 붙일 큐와 자리 | 기존 표시와의 관계 |
|---|---|---|---|
| 220 선봉장 | AttackGranted | SubjectBody: 대상마다 공격력 부여 신호 | 다중 대상 큐가 공격 뒤에 보일 수 있음. 병사 이동 없음 |
| 230 깔끔한 뒷정리 | AttackGranted | SubjectBody: 자신에게 작은 공격력 부여 신호 | 공격마다 발생하므로 강도 낮춤 |
| 250 피의 부름 | HealthRecovered | SubjectBody: 짧은 회복 입자 | 실제 회복량·체력 반영은 Core 사실/체력 사건 |
| 260 사기 증진 | AttackGranted | SubjectBody: 대상마다 공격력 부여 신호 | 다중 대상. 출처 몸에도 같은 큐를 중복 배치하지 않음 |
| 370 가속 | AttackGranted; StatusConsumed | AttackGranted=SubjectBody에 축적 신호; StatusConsumed=ActorBody에 짧은 축적 해소 | 소모 큐는 공격 처리 뒤 발동 위치 그대로. 발사 순간 연출로 쓰지 않음 |
| 420 사냥꾼 | HunterTargetSelected; AttackGranted | 선정=SubjectBody에 짧은 조준 고리; 보상=SubjectBody에 공격력 보상 신호 | 조준 고리는 순간 표시. 지속 표식 HUD를 새로 만들지 않음. Run 보상도 재계산하지 않음 |

### 4.3 상태와 보호막 — 6종, 11줄

| 키 / 특성 | 작성 줄 | 붙일 큐와 자리 | 기존 표시와의 관계 |
|---|---|---|---|
| 110 위압 | StatusApplied | SubjectBody: 위축을 알리는 짧은 상태 부여 신호 | 공격력 감소만 표현. 우선도는 보류 |
| 240 단독 행동 | ShieldGranted; ShieldAbsorbed; ShieldReset | 모두 SubjectBody. 부여=방패 윤곽; 흡수=방패 충격; 리셋=약한 방패 점멸 | 리셋은 증가 수치 표시가 아님. 지속 보호막은 HUD |
| 270 무장 해제 | StatusApplied | SubjectBody: 공격 봉쇄 상태 부여 신호 | 금지 여부·남은 턴은 기존 상태/HUD |
| 280 발목 부수기 | StatusApplied | SubjectBody: 이동 봉쇄 상태 부여 신호 | 초판은 몸 중심의 봉쇄 표시. 발밑 고정 자리나 별도 이동 모션을 추가하지 않음 |
| 290 결속 | DefenseChanged; ShieldGranted; ShieldAbsorbed; ShieldReset | DefenseChanged=SubjectBody 중립적인 방호 재배치 고리; 나머지=단독 행동과 같은 보호막 세 큐 | DefenseChanged를 증가 전용 연출로 만들지 않음 |
| 390 방어구 부수기 | ArmorBroken | SubjectBody: 짧은 방어판 균열 신호 | 스택 수에 따라 View에서 방어 감소율·입자 수를 계산하지 않음 |

### 4.4 이동과 흔적 — 7종, 9줄

| 키 / 특성 | 작성 줄 | 붙일 큐와 자리 | 기존 표시와의 관계 |
|---|---|---|---|
| 50 넉백 | KnockbackApplied | SubjectBody: 짧은 밀림 발동 신호 | 실제 이동은 Forced 걸음. 0이면 이동했다고 표현하지 않는 중립 신호 |
| 200 끌어오기 | PullApplied | SubjectBody: 짧은 견인 신호 | 밧줄·연결선·진행 보간을 새로 만들지 않음 |
| 210 충격 볼트 | KnockbackApplied | SubjectBody: 짧은 충격 신호 | 대상/방향은 실제 걸음이 표현. 아군 포함 여부 재판정 없음 |
| 330 굳건한 의지 | KnockbackPrevented | SubjectBody: 버티기 신호 | 실제 몸을 밀었다 돌려놓지 않음 |
| 340 땅 다지기 | TrailCreated; RangeChanged | 생성=EventCell에 가벼운 흙먼지; 범위=SubjectBody에 이동 범위 증가 신호 | 흔적 프리팹 생성·제거는 MovementSource 사건 소비자 |
| 350 먼지 연막 | TrailCreated; DamageModified | 생성=EventCell에 짧은 연막 피어남; 피해 감소=SubjectBody에 방호 잔광 | 지속 연막을 추가 파티클로 다시 소유하지 않음 |
| 360 뛰어넘기 | MovementChanged | EventCell에 짧은 통과 잔광 | 점프·걸음은 기존 자발 이동 모양. UnitAction으로 두 번 뛰지 않음 |

### 4.5 추가 공격과 특수 행동 — 7종, 7줄

| 키 / 특성 | 작성 줄 | 붙일 큐와 자리 | 기존 표시와의 관계 |
|---|---|---|---|
| 70 난사 | KillChainAttack | ActorBody: 추가 공격 발동 신호 | ChildAttack 기본 공격. 공격 표시 덮어쓰기 비움 |
| 90 포효 | RoarTriggered | ActorBody: 짧은 반응 발동 신호 | 기존 포효 동작·공격·영향 강조 유지. UnitAction·흔들림을 중복 추가하지 않음 |
| 160 2연발 | FollowUpAttack | ActorBody: 재공격 발동 신호 | 같은 방향·빈 공격도 Core ChildAttack 그대로. 덮어쓰기 비움 |
| 170 행운의 탄환 | FollowUpAttack | ActorBody: 추가 공격 발동 신호 | 무작위 적 건물/보스 선정은 Core. 덮어쓰기 비움 |
| 180 연쇄 폭발 | FollowUpAttack | EventCell: 짧은 추가 공격 예고 고리 | 명중 폭발 본체가 아님. ChildAttack 기본 공격, 덮어쓰기 비움 |
| 300 희생 | DamageRedirected | ActorBody: 수호자 발동 신호 + 기존 UnitAction 유지 | Source/GuardFront/Attacker, 현재 나가기 0.2초·복귀 0.25초·거리 0.6 대조. 큐는 이동 전 자리 |
| 310 도발 | TauntSelected | ActorBody: 도발 소유자의 짧은 도전 신호 | SubjectBody는 선택한 적 AI 유닛이다. 적에게 도발 소유자 표식을 붙이지 않음 |

### 4.6 Core 계약 확인 후 적용 — 1종, 2줄 초안

| 키 / 특성 | 현재 계약의 작성 줄 | 붙일 큐와 자리 | 적용 조건 |
|---|---|---|---|
| 190 사냥꾼의 표식 | StatusApplied; StatusConsumed | 모두 SubjectBody. 적용=순간 표식; 소모=순간 표식 발동 | 현재 표식과 사냥꾼(420)의 표적은 별개. 약점 노출 공통 계약이 오면 Kind·소모 의미·상태 표시를 다시 대조하고 이 행을 수정한 뒤 구현. 지금 두 줄을 SO에 먼저 넣지 않음 |

합계는 **37종 / 47줄**이다. 희생 기존 1줄을 포함한다. 표식의 계약 대기 2줄을 빼면 36종 / 45줄이고, 현재 줄에서 **신규 44줄 + 희생 기존 1줄 보완**이 우선 범위다. 모든 줄을 넣어야 기존 효과 계산이 동작한다는 뜻은 아니다. 특히 추가 공격은 줄 없이도 이미 재생된다.

## 5. 리소스와 작성 의존성

현재 Render 표의 세 효과만으로 표 전체를 의미에 맞게 채울 수 없다. 발사·폭발·피격을 회복·공격력 부여·보호막으로 재사용하지 않는다. 먼저 다음 표현 계열의 **사용 가능한 프리팹·등록 키**를 정한다. 계열 하나가 EffectType 하나라는 뜻은 아니며 프리팹 변형이 필요하면 별도 등록한다.

| 리소스 계열 | 구분 모양 | 주요 소비 줄 |
|---|---|---|
| 피해 강조 | 짧은 선·산개·집중 고리 | DamageModified 공격 기여 |
| 방어 우회·균열 | 관통선·갈라진 판 | DefenseBypassed, ArmorBroken |
| 공격력 부여·해소 | 올라가는 신호·해소 잔광 | AttackGranted, 가속 StatusConsumed |
| 상태 부여 | 위축·봉쇄를 구분할 짧은 기호 | 위압·무장 해제·발목 부수기 |
| 방호 | 방패 윤곽·충격·약한 재정렬 | 보호막 세 종류, 결속, 진지 구축, 굳건한 의지, 연막 피해 감소 |
| 회복 | 십자·상승 입자 | HealthRecovered |
| 이동·흔적 순간 | 짧은 통과 잔광·흙먼지·연막 | 이동·흔적 발동 |
| 표적·특수 발동 | 조준 고리·짧은 발동 신호 | 사냥꾼, 추가 공격, 포효, 도발, 희생 |
| 즉사 | 짧은 처형 기호 | InstantDefeat |

색만으로 구분하지 않는다. 큐 입력은 리소스 키·시점·지연·자리·위치/회전 보정을 갖고, 색·크기는 프리팹에서 정한다. 새 재생기·직접 Instantiate 우회를 만들지 않고 기존 EffectSystem Scope 경로에 등록한다. 효과별 지속 시간·동시 대상 수에 맞춰 풀 최대값도 검토한다. 예를 들어 지금 GunFire 풀 최대 3·Hit 최대 9를 다중 버프 큐에 그대로 적용하지 않는다.

필수 선행 의존성은 (1) 위 리소스의 실제 프리팹과 Effect 등록, (2) 두 누락 TraitVisual 및 테이블 연결이다. 구현자가 리소스를 확정할 수 없으면 해당 Phase에서 부족 목록과 담당 전달 내용을 남기고 멈춘다. 임의로 다른 의미의 리소스로 채우지 않는다.

## 6. 의도적으로 작성하지 않는 줄

빈 줄을 만들어 발동 전체를 나열하지 않는다. 아래는 초판 정책이며 §4의 작성 줄이 예외다.

| 발동 종류 | 이번 처리 |
|---|---|
| StatusConsumed | 가속의 실제 축적 소모, 계약 확인 후 표식 소모만 작성. 나머지 턴 차감/소모는 HUD |
| StatusExpired / StatusRemoved / StatusReset | 줄 없음. 만료·사망/전투 정리·표적 해제·이동 여부 리셋은 기존 사실 표시 |
| ShieldRemoved | 줄 없음. 지속 보호막 HUD 반영. 제거 시각을 추측해 별도 이펙트를 회수하지 않음 |
| TrailChanged / TrailRemoved | 줄 없음. Core MovementSource 사건이 기존 흔적을 갱신·회수 |
| 고정 사격 StatusApplied | 줄 없음. 이것은 이동했음 표시이지 공격 준비 완료가 아님 |
| 사냥꾼 StatusRemoved / StatusReset | 줄 없음. 표적의 생존/남은 보상 수로 View가 새 표적을 고르지 않음 |
| 보류된 다섯 특성의 모든 발동 | 작성 범위 밖 |

계속 보이는 오라·추적 표식·상태 숫자·HUD는 이번 Activation 순간 큐와 별도 소유권이다. HUD 새 종류나 지속 오라가 필요해지면 공통 효과 읽기 계약을 확인한 별도 설계로 다룬다.

## 7. 수동 확인과 미리보기 한계

[미리보기 창](../viewDocs/trait-activation-preview-guide.md)은 일반 Moment, ChildAttack, Interception으로 순간 큐·공격·희생을 확인한다. Moment는 상태·보호막·흔적 사실을 실제 적용하거나 걸음을 만들어 주지 않는다. 따라서 여기서 보호막 큐가 보였다는 사실만으로 HUD·실제 흡수·만료·넉백 순서까지 통과로 표시하지 않는다.

- 큐 자체: §4의 Kind, source/targets/cells를 넣어 재생·중단·재생을 수동 확인한다. EventCell 줄에는 cells가 필요하다.
- 실제 연결: 운영 전투에서 공격 전/피해 묶음/피해 뒤/넉백/턴 종료/사망을 확인한다. 해당 상황을 현재 진입 수단으로 재현할 수 없으면 미확인으로 남긴다. Core fixture·미리보기 생성기 확장은 이 계획에 포함하지 않는다.
- 다중 대상: 선봉장·사기 증진·결속·넉백에서 중복 큐, 읽기 어려운 강도, 풀 한도를 확인한다.
- 경계: Magnitude 0, 빈 추가 공격, 사망해 몸이 없는 대상, 출처 소유자와 수신자가 다른 효과, 중단·연속 재생을 확인한다.
- 종료 시 Setting UI 미등록 오류는 기존 사용자 결정대로 유지한다. 이 작업이 해결했다고 표시하지 않는다.
- 새 View 테스트 코드·자동 검증 도구를 만들지 않는다. 컴파일·기존 에셋 검증·수동 재생으로 확인한다. DB 변경 시 AGENTS의 기존 DataBaseAssetTests 실행 지침은 새 테스트 작성 금지와 구분하며, 실행 여부와 결과를 남긴다.

희생의 원래 대상이 밀리는 Core 문제는 [Core 전달 문서 §53](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#53-대신-맞은-공격의-넉백-요청-2026-10-30-view--core-초안-core-파일-변경-없음-미전달--사용자가-나중에-전달)의 후속이다. 그 결과를 View에서 지우거나 가짜 이동으로 보정하지 않는다.

## 8. 원칙 점검

| 기준 | 적용 |
|---|---|
| SSOT | 조건·대상·수치·수명은 Core, 시점/자리 허용은 CombatPresentationCueRules, 연출 값은 TraitVisual/공격 표시/리소스 SO, 작업 대상은 §4 한 곳 |
| SRP | SO는 표현 입력, 기존 처리기는 결과 모양 소비, Factory는 조립. 특성별 기능 클래스를 추가하지 않음 |
| OCP | 새 특성의 같은 결과 모양은 표 줄로 확장. 기존 Handler에 TraitKey별 if/switch를 추가하지 않음 |
| LSP | 기존 공격·피격·희생 완료/취소 계약 유지. 큐 유무가 결과 성공이나 사건 완료 의미를 바꾸지 않음 |
| ISP | 기존 Effect/Camera/Presentation 창구만 사용. 통합 컨텍스트·새 루트 서비스를 만들지 않음 |
| DIP | DB 키와 기존 좁은 창구로 연결. SO에서 Core 실행기·구체 프레임워크 상태에 접근하지 않음 |
| JMH/FW | 서비스 재등록 없음(FW-001/002), Builder 재사용 없음(FW-003), 상태 직접 수정 없음(FW-004), 기존 Scope/Pool/Handle 반환(FW-010~014), 사건/Queue 수명 유지(FW-015/016) |
| 독립성 §4.6 | 특정 특성 색/강도 변경은 그 TraitVisual과 리소스에 한정. 리소스 등록은 Render DB. Core 계약 변경 시 해당 표 행·소비자만 재대조. 관련 없는 Factory·입력·씬 연결은 변경하지 않음 |

## 9. 판단 근거와 별도 결정이 필요한 범위

현재 확인한 코드 원본은 `TraitRegistry.CreateDefault`, `DeferredCombatFeatures`, `TraitAttackNotifications`, `TraitKillSettlement`, `TraitRestrictionApplication`, `TraitPresenceReconciler`, `TraitTurnLifecycle`, `CombatTemporaryStateCleanup`, `TraitTrailCalculator`, `MovementProjection`, `HunterApplication`, `CombatMovementCalculator`, `AttackDirectDamageCalculator`, `CombatCommandEvents`, `EnemyAiTurnService`, `CombatTraitEventHandler`, `CombatGuardLead`, `CombatCuePoints`, `CombatPresentationCueRules`다. 기존 시기 표의 옛 “TS-b/f 미구현” 설명보다 현재 코드를 우선했다.

이 초안의 권장 선택은 **9개 표현 계열 확보 → 기존 재생기만으로 44개 신규 줄 작성 → 실제 전투 연결 확인**이다. 표식 2줄은 계약 인수까지 대기한다. 실제 명중 폭발, 새 특성 소리 칸, 지속 오라, 추가 UnitAction, R11 순서 변경은 이 초안 구현 범위에 넣지 않았다. 이를 원하면 먼저 설계·Task를 수정해 확인한다.
