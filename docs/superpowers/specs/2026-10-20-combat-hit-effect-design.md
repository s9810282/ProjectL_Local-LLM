# 전투 피격 이펙트 설계 (맞은 유닛별)

작성일: 2026-10-20. 상태: **결정 완료·HE-a~HE-d 코드·문서 완료(2026-10-20), 바꾼 파일 MSBuild 오류·경고 0, Unity 확인 대기.** 결정 Q-HE1~Q-HE4 모두 A(2026-10-20 사용자 "맞은 유닛별 피격 이펙트로 설계 문서 써 줘. 정할 것 1~4는 모두 A로."), 설계에서 정한 것 P1~P4(§4.2) 확정(2026-10-20 사용자 "P1~P4 이대로 확정. Task 계획 써 줘."). Task 계획·실행 기록 [hit-effect-plan](../plans/2026-10-20-combat-hit-effect-plan.md)(HE-a~HE-d). 구현 중 보정: 사망 사건의 결과 사실은 위치가 없어 맞은 방향은 "결과 위치, 없으면 사건 전 위치"를 쓴다(HE2).

사용자 질문: "히트 시 이펙트 출력 넣으려면 `CombatUnitReactionPresentationSO`에다가 창구 열어서 진행해야 하나, EffectScope 쓰는 방식으로" → 값은 반응 SO에 칸 하나를 두고, 내는 쪽은 새 Scope를 열지 않고 기존 이펙트 창구(`CombatEffectWindow`)를 피격·사망 조립에 넘긴다.

관련: [클립 이펙트 설계](2026-10-19-combat-clip-effect-design.md)(EF1~EF6 — 창구·조각, Q-E1~Q-E4 A, 그 문서 77줄이 이 갈림길을 미뤄 둠), [투사체 접촉 이펙트 설계](2026-10-19-combat-projectile-impact-effect-design.md)(IE1~IE6 — 무기 쪽 접촉 이펙트, 종류 규칙 하나), [조각·조립 설계](2026-10-18-combat-presentation-pieces-design.md)(BP8 사망 조립·BP9 피격 조립). 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`.

## 0. 이 설계가 채우는 칸

| 이펙트 | 무엇에 따라 다른가 | 값 자리 | 위치 | 시점 | 상태 |
|---|---|---|---|---|---|
| 공격 클립 이펙트(EF) | 공격자 클립 | 클립 SO 공격 칸 `attackEffect` | 공격자 `effectPoint` | 클립 이벤트 타이밍 | 있음 |
| 투사체 접촉 이펙트(IE) | 공격자 투사체 | 공격 표시 SO `impactEffect`(근접은 Empty만) | 접촉 지점 | 맞는 순간 | 있음 |
| **피격 이펙트(이 설계)** | **맞은 유닛의 몸** | **반응 SO `hitEffect`** | **맞은 유닛 몸 중심** | **맞는 순간(피격·사망 조립 시작)** | 없음 |

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| H-F1 | 반응 SO는 외형 항목이 참조하는 "피격·사망 표현 입력"이다(맞은 쪽 값). 지금 에셋은 `CombatUnitReaction_Default` 하나를 모든 유닛이 공유한다 | `DV/CombatUnitReactionPresentationSO.cs`, `UnitVisualDataSO.reactionPresentation`, `DataBaseSO/ViewGroup/CombatPresentation/` |
| H-F2 | 피격 조립 `ComposeHit`(반응 + 플래시 + 피격 클립 + 흔들림)과 사망 조립 `ComposeDeath`(사망 반응 + 소리 + 흔들림)는 정적 함수이고, 시작 순간 기다리지 않는 항목(흔들림)을 이미 붙인다 | `C/CombatCharacterEventHandler.cs:163·187` |
| H-F3 | 치명타는 피격 조립으로 가지 않는다. 피격은 `HealthChanged`이고 결과가 살아 있을 때만, 체력이 0이 되면 `Died` 사건이 사망 조립으로 간다 | 같은 파일 `TryBeginHit:112`·`TryBeginDeath:70` |
| H-F4 | 두 조립은 피해 원인과 상관없이 체력이 줄 때 시작한다(공격·가시·기차·약한 지반 등). 시작 순간은 맞는 순간이다(공격은 Gate 해제가 피해 사건을 엶) | 히트 타이밍 설계 H3, 동시 처리 설계 C1 |
| H-F5 | 이펙트 창구는 Scope 하나를 갖고 받은 월드 위치·회전에 놓기만 하며, 회수는 결과 재생 취소(`StopAll`)·해제(`Release`)를 Presenter가 부른다. Presenter가 창구를 갖고 공격 처리기에만 넘긴다 — **Factory 무변경으로 캐릭터 처리기에도 넘길 수 있다** | `C/Display/CombatEffectWindow.cs`, `C/CombatSceneEventPresenter.cs:67·107·110` |
| H-F6 | Framework Scope는 Shutdown까지 쌓이고 요청 하나만 회수하는 API가 없다(EF 사용 평가 G2) | 클립 이펙트 설계 §1 |
| H-F7 | 이펙트 조각 `CombatEffectPiece`는 자리를 **공격자 클립 대여의 이펙트 지점**에서만 읽는다. 호출은 `CombatClipAttack.AddEffect` 한 곳 | `C/CombatEffectPiece.cs`, `C/CombatClipAttack.cs:99` |
| H-F8 | "맞은 방향"(피해 근원 칸 → 맞은 유닛, 모호하면 유닛 뒤쪽, 그것도 없으면 기본 뒤쪽)은 피격 반응의 밀림 방향으로 이미 한 함수에 있다(private) | `C/Display/CombatUnitReactionPresenter.cs:198` `ResolveDirection` |
| H-F9 | 유닛 몸 모양의 원본은 외형 프리팹 루트 BoxCollider이고, 사실 기반 배치(`CenterOf`·`ModelRotation`, 대여 중 Transform은 쓰지 않음)와 합치는 식이 `FieldUnitPresenter`의 private `BodyOf` 하나에 있다. 결과 `CombatBody`는 필드 XZ 2D라 높이가 없다 | `C/Display/FieldUnitPresenter.cs:82·139·171`, `C/Display/CombatBody.cs` |
| H-F10 | 보인 사실에서 대상의 몸을 찾는 조회는 `CombatShownFacts.TryGetBody` 하나다(필드 위·살아 있음·위치 있음). 캐릭터 처리기는 이미 `CombatShownFacts`를 갖고 있다 | `C/CombatShownFacts.cs:82`, 처리기 생성자 |
| H-F11 | 이펙트 종류 규칙은 `CombatPresentationEffectRules`(`IsKnown`·`IsPlayable`) 한 곳이다(IE2). Render DB 등록 여부는 첫 재생 때 Framework가 본다 | `DV/CombatPresentationEffectRules.cs` |
| H-F12 | `CombatClipAttack.cs`에는 Ballista 임시 클립 공격(2026-10-20)의 커밋 전 변경이 있다. 이 설계가 고칠 다른 파일에는 커밋 전 변경이 없다 | `git status` |

## 2. 설계

### HE1. 값 — 반응 SO의 피격 이펙트 칸 (Q-HE1 A)

- `CombatUnitReactionPresentationSO` "피격" 머리말 아래에 `hitEffect`(`EffectType`, 기본 Empty = 없음)를 둔다. 툴팁: "맞는 순간(치명타 포함) 맞은 유닛 몸 중심에 내는 이펙트. 이펙트의 +Z가 맞은 방향(공격 출처 반대쪽)을 봅니다."
- 검사: `CreateData`에서 `CombatPresentationEffectRules.IsKnown`이 아니면 기존 `Invalid(nameof(hitEffect))`(규칙 원본 하나, H-F11).
- 값: `CombatUnitReactionPresentationData.HitEffect`(생성자 **끝 선택 인자**, 기본 Empty — 기존 호출·시험 그대로), `HasHitEffect => CombatPresentationEffectRules.IsPlayable(HitEffect)`(플래시의 `HasHitFlash`와 같은 꼴).
- 뜻은 "맞은 몸에 따라 다른 이펙트"다. 무기에 따라 다른 이펙트는 IE `impactEffect`가 맡는다(맞은 쪽에 두면 공격자마다 달라야 할 사실이 맞은 쪽에 놓여 원본이 틀림).
- 지금 반응 에셋이 하나라 칸을 채우면 모든 유닛이 같은 이펙트를 낸다. 유닛마다 다르게 하려면 반응 에셋을 나눠 외형 항목에 연결한다(값 원본은 그대로 반응 SO 하나의 칸).

### HE2. 맞은 방향 — 반응과 같은 함수 (P1)

- `ResolveDirection`을 새 정적 함수 `C/Display/CombatHitDirection.Resolve(CombatCharacterEvent e, FieldGridMapper mapper, Quaternion current)`로 옮긴다. 피격 반응(밀림)과 피격 이펙트(회전)가 함께 부른다. 반응 동작은 같다(입력 `pose.Rotation` → `current`).
- 유닛 위치는 결과 사실의 위치이고, 없으면 사건 전 위치다(구현 중 보정 — 사망 사건의 결과 사실은 필드에서 빠져 위치가 없음, 피격 반응은 결과 위치가 늘 있어 같음).
- 이펙트 회전 = `Quaternion.LookRotation(맞은 방향, mapper.WorldUp)`. 대칭 이펙트(피·먼지)에는 차이가 없고, 튀는 이펙트(불꽃)는 공격 반대쪽으로 튄다. 사망도 사건의 피해 근원으로 같은 함수를 쓴다.

### HE3. 위치 — 사실 기반 몸 상자 중심 (Q-HE2 A, P2)

- `FieldUnitPresenter`의 `BodyOf`(private) 안 "상자 중심을 표시와 같은 월드 배치로 놓는 식"을 private 도우미 하나로 빼서, 접촉 몸 `BodyOf`와 새 `BodyCenterOf(CharacterCombatFacts, FieldGridMapper) → Vector3`(월드 3D, 높이 포함)가 함께 쓴다. 콜라이더가 없으면 배치 중심(발밑)이다 — 콜라이더 없음 경고는 몸 모양 캐시가 이미 한 번 낸다.
- 조회는 `CombatShownFacts.TryGetBodyCenter(EntityId, out Vector3)` — `TryGetBody`와 같은 찾기 규칙(필드 위·살아 있음·위치 있음, 캐릭터만). 찾지 못하면 false이고 이펙트 항목을 넣지 않는다(보이지 않는 유닛에는 연출이 없다는 반응과 같은 규칙).
- 사실 기반이라 표시 크기 대여(포효로 커진 보스)는 넣지 않는다(접촉 몸과 같은 규칙, 대여 중 Transform은 자세·크기 대여가 섞임).

### HE4. 조각 — 이펙트 조각의 자리 출처를 받는다

- `CombatEffectPiece`의 "창구로 내고 바로 끝" 규칙은 그대로 두고, 자리만 바꿔 끼운다. 생성자를 private로 하고 만드는 함수 둘:
  - `AtClipPoint(unit, effects, effect)` — 지금 동작(시작 때 그 유닛 클립 대여의 이펙트 지점, 없으면 지금 예외 문구 그대로). `CombatClipAttack.AddEffect` 한 줄이 이것을 부른다.
  - `At(effects, effect, Pose at)` — 정해진 월드 자리(피격 이펙트).
- 안에서는 자리 출처 대리자 하나(`Func<CombatPieceContext, Pose>`)를 `Begin`이 부른다. 모드 분기(`if 클립 자리 else 정해진 자리`)를 두지 않는다.

### HE5. 무엇을 낼지 정하는 쪽 — 캐릭터 처리기 도우미 하나 (Q-HE3 A)

- `CombatCharacterEventHandler`가 창구를 생성자 **끝 선택 인자**로 받는다(없으면 이펙트 없음 — 시험 조립). Presenter `:110` 한 줄이 이미 가진 창구를 넘긴다(Factory 무변경, H-F5).
- 도우미 하나 `HitEffectOf(EntityId id, CombatUnitReactionPresentationData settings, CombatCharacterEvent e, ICombatUnitPose pose) → CombatEffectPiece`(없으면 null): 창구가 있고 `settings.HasHitEffect`이고 몸 중심을 찾으면 `CombatEffectPiece.At(창구, settings.HitEffect, new Pose(몸 중심, 회전))`.
- `TryBeginHit`·`TryBeginDeath`가 이 도우미 결과를 조립에 넘긴다. 피격은 사실 반영 뒤(지금 반응 시작 자리), 사망은 사실 반영 전(지금 조립 자리) — 두 경우 모두 보인 사실에서 몸을 찾을 수 있다(사망은 아직 살아 있는 사실, H-F10).
- 조립 함수 둘은 끝 선택 인자 `CombatEffectPiece hitEffect = null`을 받아, 있으면 **시작 순간·기다리지 않는** 항목으로 붙인다(흔들림 항목과 같은 꼴). 완료 시점은 바뀌지 않는다. "낼지 말지" 판단은 도우미 한 곳이고 조립은 붙이기만 한다.

### HE6. 겹침·범위 (Q-HE4 A)

- 투사체가 맞히면 접촉 이펙트(무기, 접촉 지점)와 피격 이펙트(몸, 몸 중심)가 같은 프레임에 둘 다 난다. 서로 다른 사실이라 규칙으로 막지 않는다. 한쪽만 원하면 그 칸을 Empty로 둔다.
- 피해 원인과 상관없이 낸다(H-F4, P4). 여러 대상·같은 원인 사망은 대상마다 하나씩(동시 묶음 그대로).
- 범위 밖: 구조물·장애물 피격(피격 조립이 캐릭터 사건만 다룸), 피격 클립 이벤트 타이밍에 맞추기, 원인 종류별로 고르기.

### HE7. 회수·성능

- 회수는 기존 그대로(재생 취소 = `StopAll`, 해제 = `Release`). 새 수명 코드 없음.
- 새 Tick 없음. 맞을 때마다 칸 확인 한 번 + 몸 중심 계산(캐시된 상자 + 곱셈 몇 번)과 조각 하나·대리자 하나 할당(조립이 이미 항목마다 할당함).

## 3. 7기준 검토 (초안 → 보완)

### 3.1 초안에서 찾은 것과 보완

| # | 기준 | 초안의 문제 | 보완(위 설계에 반영) |
|---|---|---|---|
| D1 | FW·캡슐화 | 처음 안: 반응 SO(또는 반응 Presenter)에 이펙트 창구를 열어 Scope를 받음 — SO는 입력 값만 두는 곳, Scope는 Shutdown까지 쌓이고(H-F6) 회수 규칙이 두 벌 | SO는 값 칸만(HE1), 내기는 기존 창구(HE5) |
| D2 | SSOT | 처음 안: 이펙트 회전을 처리기에서 따로 계산 — "맞은 방향" 둘째 사본 | 반응 함수를 공유 함수로 옮김(HE2) |
| D3 | SSOT | 처음 안: 몸 중심을 처리기에서 Transform·콜라이더로 직접 계산 — 상자 배치 식 둘째 사본, 대여 중 Transform이 섞임 | `FieldUnitPresenter` 도우미 하나를 접촉 몸과 공유, 보인 사실 조회 하나(HE3) |
| D4 | SSOT·무의미한 If | 처음 안: 피격·사망 조립이 각자 "창구 있나·칸 있나·몸 있나"를 검사 — 조건 두 벌 | 처리기 도우미 하나가 조각을 만들고 조립은 붙이기만(HE5) |
| D5 | SOLID(OCP)·무의미한 If | 처음 안: 새 조각 클래스(내고 끝 규칙 사본) 또는 기존 조각에 모드 분기 | 자리 출처만 받는 조각, 만드는 함수 둘(HE4) |
| D6 | 독립성 | 처음 안: 반응 Presenter가 이펙트를 냄 — 반응 시작 실패·취소 갈래에 이펙트가 섞임 | 이펙트는 조립 항목, 반응은 그대로(HE5) |
| D7 | C#·Unity 권장 | 위치·회전을 따로 넘기면 인자 순서 실수 위험 | `UnityEngine.Pose` 하나로, 값 생성자는 끝 선택 인자(HE1·HE4) |

### 3.2 보완 뒤 점수(예상)

| 기준 | 점수 | 근거 · 남는 것 |
|---|---|---|
| SSOT | 9 | 피격 이펙트 종류 = 반응 SO 한 칸, 맞은 방향 = 공유 함수 하나(반응과 같음), 몸 중심 = 접촉 몸과 같은 도우미, "피격 → 이펙트" 판단 = 처리기 도우미 하나, 종류 규칙 = 하나 |
| SOLID | 8.5 | 값·방향·위치·내기·판단이 각자 한 곳. 남는 것: 캐릭터 처리기 생성자 인자 12개, `FieldUnitPresenter`(588줄)에 함수 하나 |
| JMH 지침·FW | 9 | Factory·ServiceKeys·Framework 무변경, Scope 누적은 기존 창구 그대로, 새 Tick 없음 |
| 독립성 | 9 | 이펙트 추가 = SO 칸 값만, 유닛별 = 에셋 연결만. 반응·플래시·클립·흔들림 항목 무변경, 기존 시험 수정 예상 0(모든 서명 변경이 끝 선택 인자) |
| 캡슐화 | 8.5 | SO는 값만, Scope는 창구 안, `Pose` 값. 남는 것: `CombatShownFacts`·`FieldUnitPresenter`에 internal 조회 하나씩 |
| C#·Unity 권장 | 8.5 | `Pose`·`LookRotation`·끝 선택 인자. 남는 것: 이펙트 등록 누락이 첫 재생 때에야 드러남(Framework 정책, EF·IE와 같음) |
| 무의미한 If/Tick | 9 | 새 Tick 없음, 판단은 도우미 한 곳의 조건 셋(창구·칸·몸 — 모두 실제로 갈리는 경우) |
| **종합** | **약 8.8** | |

### 3.3 남는 낮음 (관찰)

- L1 피해 원인 구분 없음 — 가시·기차·약한 지반 피해에도 난다(맞은 유닛별 뜻 그대로, P4). 공격만 원하면 사건의 피해 근원 종류로 거르는 한 줄을 도우미에 더한다.
- L2 공유 반응 에셋 — 칸을 채우면 모든 유닛이 같다(HE1). 유닛별은 에셋 나누기.
- L3 콜라이더 없는 유닛은 발밑에 난다(HE3, 기존 경고가 알림).
- L4 구조물 피격은 범위 밖(HE6).

## 4. 결정

### 4.1 결정 완료 (2026-10-20 사용자, 모두 A)

- **Q-HE1. 이펙트를 정하는 쪽:** A 맞은 유닛별(반응 SO). (B 무기별 — IE `impactEffect` 근접 확장 / C 둘 다)
- **Q-HE2. 위치:** A 몸 상자 중심(프리팹 BoxCollider). (B 배치 위치 + SO 높이 / C 프리팹 지점)
- **Q-HE3. 치명타:** A 사망 조립에도 같은 항목. (B 피격만)
- **Q-HE4. 투사체 접촉 이펙트와 겹침:** A 둘 다 냄. (B 접촉 이펙트가 있으면 생략)

### 4.2 설계에서 정한 것 (확정, 2026-10-20 사용자)

- **P1 회전 = 맞은 방향**(반응 밀림과 같은 함수, 이펙트 +Z가 공격 반대쪽). 다른 안: 회전 없음(필드 위쪽 고정).
- **P2 위치는 사실 기반**(표시 크기 대여 제외, 접촉 몸과 같은 규칙). 다른 안: 인스턴스 Transform(커진 보스에 맞지만 자세 대여가 섞임).
- **P3 몸을 찾지 못하면 항목 없음**(예외 아님).
- **P4 피해 원인과 상관없이 냄.** 다른 안: 공격 피해만(근원 종류로 거름).

## 5. Phase 개요 (Task는 [계획](../plans/2026-10-20-combat-hit-effect-plan.md))

| Phase | 내용 | 화면 |
|---|---|---|
| HE-a | 값: 반응 SO 칸·값·검사(HE1) + 시험(SO 검사, Unity 전용) | 변화 없음(기본 Empty) |
| HE-b | 기반(동작 동일): 맞은 방향 공유 함수(HE2, 반응이 부름), 몸 중심 도우미·조회(HE3), 조각 자리 출처(HE4, `CombatClipAttack` 한 줄) + 시험(방향·몸 중심·조각) | 변화 없음 |
| HE-c | 연결: 처리기 도우미·창구 인자, 두 조립 끝 인자, Presenter 한 줄(HE5) + 시험(조립 항목·피격/치명타 장면, 가짜 이펙트 창구) | 칸을 채운 반응 에셋의 유닛만 |
| HE-d | 공유·기록: 호진님(반응 SO 새 칸, 유닛별은 반응 에셋 연결), 민형님(전투용 `EffectType` 등록 요청 범위에 피격 추가, 이펙트 +Z 규칙), 폴더 정리 대응표에 `CombatHitDirection` 한 줄 | — |

바뀌는 파일(예상): `DV/CombatUnitReactionPresentationSO.cs`·`CombatUnitReactionPresentationData.cs`, 새 `C/Display/CombatHitDirection.cs`(+ `.meta`), `C/Display/CombatUnitReactionPresenter.cs`·`FieldUnitPresenter.cs`, `C/CombatShownFacts.cs`·`CombatEffectPiece.cs`·`CombatClipAttack.cs`(한 줄)·`CombatCharacterEventHandler.cs`·`CombatSceneEventPresenter.cs`(한 줄). 에셋 무변경(새 칸 기본 Empty).

착수 조건: `CombatClipAttack.cs`의 Ballista 임시 클립 공격 변경이 커밋 전이라(H-F12), 그 묶음 커밋 뒤 HE-b를 시작하거나 커밋 때 hunk를 나눈다. HE-a는 바로 가능.
