# 전투 투사체 접촉 이펙트 설계 (착탄·관통)

작성일: 2026-10-19. 상태: **결정 완료(2026-10-19, 사용자 "Q-I1~Q-I4 모두 A로 해. Task 계획 쓰고 IE-a부터 진행해.")**, Task 계획 [projectile-impact-effect-plan](../plans/2026-10-19-combat-projectile-impact-effect-plan.md). **IE-a~IE-c 완료(IE-a·IE-b Unity 컴파일·시험 문제 없음, 2026-10-19 사용자).**

사용자 요청: "지금 투사체 공격의 경우 투사체가 상대방에게 접촉 시 이펙트 출력 가능한 구조인가? ex) Cannon의 폭탄 착탄 시 폭발 이펙트, 발리스타 화살이 적관통 시 관통 이펙트 등" → (지금은 안 됨, 빠진 것 셋) → "설계 부터하는데 SSOT / SOLID 5원칙 / JMH Framework 개발 지침·FW 계약 / 기능 변경의 독립성 / 캡슐화 / C# 및 유니티 권장 구조 / 무의미한 If문 혹은 Tick 검사 검토하고 진행해".

관련: [클립 이펙트 설계](2026-10-19-combat-clip-effect-design.md)(EF1~EF6 — 이펙트 창구·조각, 결정 Q-E1~Q-E4 A). 그 설계 Q-E3 C("코드 모션 공격 길")를 투사체 접촉에 한정해 채운다. 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`.

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| I-F1 | 전투 연출에서 이펙트가 나오는 자리는 공격 클립 `ClipEvent`의 공격자 이펙트 지점(`CombatEffectPiece`) 하나다. 그 밖에 기차·환경 폭발이 따로 있다. 투사체 접촉 순간에는 이펙트를 거는 값·자리가 없다 | `C/CombatEffectPiece.cs`, `C/Display/CombatEnvironmentEffectPresenter.cs`, `FieldRailwayPresenter.cs` |
| I-F2 | 투사체 접촉 순간은 네 길에서 한 점씩 이미 난다: 곡사 코드 모션 착탄 콜백(`CombatMortarAnimation.Fire`), 직선 코드 모션 `SendHit`(주 대상 몸 접촉 진행률 또는 도착), Ballista 대상별 접촉(시각 순 정렬, 닿을 때마다 그 대상만 `_gate.Release(e, target)`), 클립 원거리 `FireShot`의 맞음 콜백(곡사 착탄·직선 접촉) | 각 파일, `C/CombatAttackPlayback.cs` |
| I-F3 | 네 길 모두 공격 재생(`CombatAttackPlayback`)이 만들고 맞음 콜백을 넣는다. 직선 코드 모션은 근접과 같은 입구(`TryBeginDirect`)를 쓰고, 정면 공격 모션(포효 박치기)도 그 입구를 지난다 | `TryBegin`·`TryBeginDirect`·`TryBeginMortar`·`TryBeginBallista`·`FireShot` |
| I-F4 | 접촉 위치는 발사 때 이미 정해진다: 곡사 = 착탄점(`CombatShotPath.TryTarget`), 직선·Ballista = 발사 경로(from→to)의 접촉 진행률 t. 맞음 콜백은 위치를 넘기지 않는다 | `CombatLinearAttackAnimation.Advance`(`_hitAt`), `CombatBallistaAnimation`(`_contacts`) |
| I-F5 | 이펙트 창구 `CombatEffectWindow.Play(종류, 월드 위치, 회전)`은 필드 루트 아래에 놓고 따라가지 않으며, 재생 취소(`StopAll`)·해제(`Release`)는 Presenter가 이미 부른다. 창구는 Presenter가 갖고 있고 공격 재생도 Presenter가 만든다 — **Factory는 바뀌지 않는다** | `CombatSceneEventPresenter.cs:50·82·172·227` |
| I-F6 | "정의된 이펙트 종류" 판정이 세 곳에 따로 있다: 클립 값 `CombatClipAttackData.IsKnownEffect`, 창구 `Play`, 폭발 Presenter 생성자(EF 자체 점검 L1) | 각 파일 |
| I-F7 | 공격 표시 SO(`CombatAttackPresentationSO`, 용근 파일)는 종류별 투사체 값의 원본이다. 클립 공격도 투사체 값은 여기서 읽는다(AC15). 값 생성자는 위치 인자 23개이고 호출은 SO 한 곳뿐이다 | `DV/CombatAttackPresentationSO.cs`·`Data.cs`, grep |
| I-F8 | 곡사는 빈 칸·아군 칸도 쏠 수 있다(Core 인계 "empty/friendly direct targets"). 맞은 유닛이 없어도 착탄은 있다 | [Core 인계](../handoffs/2026-10-16-undead-cannon-cart-view-handoff.md) |
| I-F9 | 투사체 애니메이션을 만드는 시험은 클래스마다 도우미 한 곳이다(`CombatMortarAnimationTests`·`CombatStandardAttackTests`·`CombatBallistaAttackTests`) | grep |

## 2. 설계

### IE1. 값 — 공격 표시 SO의 접촉 이펙트 칸 (Q-I1)

- `CombatAttackPresentationSO`에 `impactEffect`(`EffectType`, 기본 Empty = 없음)를 투사체 묶음("발사체: 직선 · 곡사 · Ballista") 머리말 아래에 둔다. 값 `CombatAttackPresentationData.ImpactEffect`(생성자 **끝 이름 있는 인자**, 기본 Empty — 위치 인자 23개 뒤에 하나를 끼우지 않음).
- 뜻: "투사체가 대상에 닿는 순간 그 자리에 내는 이펙트". 곡사 = 착탄, 직선 = 주 대상 몸 접촉(없으면 도착), Ballista = 대상마다(관통).
- 투사체의 성질이므로 클립 묶음이 아니라 여기다. 코드 모션 유닛(선인장 대포·Ballista)은 클립 묶음이 없고, 클립 원거리 유닛도 투사체 값은 이 SO에서 읽는다(I-F7).
- 검사(SO `Validate`): 정의된 종류여야 하고, **근접(Melee)은 Empty만**(근접은 투사체가 없어 칸이 뜻이 없음 — 근접 타격 이펙트는 EF의 공격 칸 이펙트가 맡음).

### IE2. 이펙트 종류 규칙 하나로 (Q-I4)

- 새 정적 규칙 `DV/CombatPresentationEffectRules.cs`: `IsKnown(EffectType)`(Empty 포함, 값 칸 검사용), `IsPlayable(EffectType)`(Empty 아님 + 정의됨, 내는 쪽 검사용), 문구 상수.
- 부르는 곳: 클립 값 `CombatClipAttackData`(기존 `IsKnownEffect`는 이 규칙을 부르는 한 줄로 두거나 호출처를 옮김 — 계획에서 정함), 공격 표시 SO `impactEffect`, 창구 `CombatEffectWindow.Play`, 폭발 Presenter 생성자. EF 점검 L1(판정 세 곳)을 같이 없앤다. 동작은 같다.

### IE3. 접촉 사실 값 `CombatProjectileContact` (새, `C/Display/`)

- `readonly struct`: `Position`, `Rotation`. 만드는 함수 하나 `Along(from, to, progress, up, fallback)`:
  - 위치 = from→to를 progress(0~1로 맞춤)만큼 간 점. 곡사 착탄은 progress 1(= 착탄점).
  - 회전 = 투사체의 **수평 진행 방향**(필드 위쪽 기준, Q-I3) — 방향 계산 원본 `CombatAttackFacing.Toward(from, to, up, fallback)`을 그대로 부른다(공격 방향 계산과 같은 함수).
- 접촉 값은 **발사 순간 한 번** 만든다(경로가 그때 정해짐, I-F4). 프레임 경로에서 만들지 않는다.

### IE4. 접촉 사실을 내는 쪽 — 애니메이션·출처 발사

- 맞음 콜백이 접촉 사실을 함께 넘긴다:
  - `CombatMortarAnimation`·`CombatLinearAttackAnimation`: `Action hit` → `Action<CombatProjectileContact> hit`(발사 때 만든 값을 착탄·접촉 때 넘김).
  - `CombatBallistaAnimation`: `Action<EntityId> hit` → `Action<EntityId, CombatProjectileContact> hit`. 대상별 접촉 목록에 진행률과 함께 값을 넣는다(지금 정렬·세대 확인 그대로).
  - 근접(`CombatMeleeAttackAnimation`)은 그대로(투사체 없음).
- 애니메이션은 **사실만** 알린다(이펙트를 모름). 세대 확인은 지금처럼 콜백 앞에서 한다(늦은 접촉에 이펙트가 나지 않음).
- 투사체 풀·진행(`CombatProjectilePool`·`CombatProjectileFlights`)은 바뀌지 않는다(접촉 시각은 애니메이션·출처 발사가 이미 정함).

### IE5. 무엇을 낼지 정하는 쪽 — 공격 재생 한 곳

- `CombatAttackPlayback`이 창구(선택 인자, 없으면 이펙트 없음 — 시험 조립)를 생성자로 받는다. Presenter가 이미 가진 창구를 넘긴다(Factory 무변경, I-F5).
- 한 함수 `Contacted(settings, contact)`: 그 공격 표시의 `ImpactEffect`가 있으면 창구로 낸다. 네 길이 모두 이 함수를 부른 뒤 지금의 해제를 그대로 부른다:
  - 곡사 코드 모션: `contact => { Contacted(settings, contact); releaseAll(); }`
  - 직선 코드 모션: 같은 꼴(`TryBeginDirect`에서 직선일 때만, 근접은 지금 `hit` 그대로)
  - Ballista: `(target, contact) => { Contacted(settings, contact); _gate.Release(e, target); }`
  - 클립 원거리 `FireShot`: 발사 때 접촉 값을 만들고 맞음 콜백을 `Contacted` + 기존 `hit`으로 감쌈(출처 발사 기록·세대 확인은 투사체 진행이 그대로 함)
- 순서: 이펙트를 낸 뒤 해제한다(같은 프레임, 해제가 연 피격 연출보다 먼저 보임). 이펙트 등록이 없으면 Framework가 첫 재생에서 예외를 던지는 정책은 EF와 같다(I-F3 E-F3).
- 회수: 기존 그대로(결과 재생 취소 = `StopAll`, 해제 = `Release`, Q-E4 A). 이 설계에서 새 수명 코드는 없다.

### IE6. Ballista 끝 (Q-I2)

- 권장: 대상 접촉마다만 낸다(요청 "적 관통 시"). 화살이 경로 끝(벽·사거리 끝)에 닿을 때 이펙트는 이번에 두지 않는다 — 필요하면 같은 값 꼴로 칸 하나(`endEffect`)와 도착 콜백 한 줄로 더한다.

### 정면 공격 모션(포효)과의 관계

- 포효 박치기는 대부분 근접 표시라 영향이 없다. 직선 표시 유닛이 정면 공격 모션을 쓰면 끝점 도착에 접촉 이펙트가 난다(직선의 "도착 = 맞음" 규칙 그대로). 별도 갈래를 두지 않는다.

## 3. 7기준 검토 (초안 → 보완)

### 3.1 초안에서 찾은 것과 보완

| # | 기준 | 초안의 문제 | 보완(위 설계에 반영) |
|---|---|---|---|
| D1 | SOLID(SRP)·SSOT | 처음 안: 애니메이션 셋 + 출처 발사가 각자 창구를 받아 이펙트를 냄 — "접촉 → 이펙트" 규칙 네 벌, 애니메이션이 이펙트를 앎 | 애니메이션은 접촉 **사실**만 넘기고(IE4), 무엇을 낼지는 공격 재생 함수 하나(IE5) |
| D2 | SSOT | 처음 안: 값을 클립 묶음 SO에 둠 — 코드 모션 유닛엔 묶음이 없고, 클립 유닛도 투사체 값은 공격 표시 SO에서 읽어 원본이 둘이 됨 | 공격 표시 SO 한 칸(IE1) |
| D3 | SSOT | 이펙트 종류 판정이 세 곳(EF L1)인데 새 칸이 넷째가 됨 | 규칙 하나로 모음(IE2) |
| D4 | 무의미한 If/Tick | 처음 안: 투사체 위치를 매 프레임 읽어 접촉 지점을 구함 | 발사 때 한 번 만든 접촉 값을 넘김(IE3), 프레임 비용 0 |
| D5 | 독립성·FW | 처음 안: 투사체 풀 콜백 서명을 바꿈(풀은 모든 투사체 공유) | 풀·투사체 진행 무변경, 바뀌는 건 애니메이션 맞음 콜백(시험 도우미 셋, I-F9) |
| D6 | C#/Unity 권장 | 위치 인자 23개 생성자 가운데에 끼우면 순서 실수 위험 | 끝 이름 있는 인자(IE1) |
| D7 | 캡슐화 | 접촉 값이 바뀌는 값이면 콜백 받는 쪽이 고칠 수 있음 | `readonly struct`, 만드는 함수 하나(IE3) |

### 3.2 보완 뒤 점수(예상)

| 기준 | 점수 | 근거 · 남는 것 |
|---|---|---|
| SSOT | 9 | 접촉 이펙트 종류 = 공격 표시 SO 한 칸, 접촉 위치·회전 = `CombatProjectileContact.Along` 하나(회전은 `CombatAttackFacing.Toward`), 종류 판정 = 규칙 하나, "접촉 → 이펙트" = 공격 재생 함수 하나 |
| SOLID | 8.5 | 애니메이션 = 사실, 공격 재생 = 결정, 창구 = 내기. 남는 것: 공격 재생(533줄)에 함수 하나·인자 하나가 더해짐(K2 관찰 계속) |
| JMH 지침·FW | 9 | Factory·ServiceKeys·Framework 무변경, 프레임 할당 0(접촉 값 struct, 콜백은 발사 때 이미 만들던 것). Scope 누적(G2)은 기존 창구 그대로라 늘지 않음 |
| 독립성 | 9 | 이펙트 추가 = SO 칸 값만(코드 모션·클립 두 길 공통), 풀·조립·조각·처리기 무변경 |
| 캡슐화 | 9 | 접촉 값 불변, 창구 Scope 안, 애니메이션은 이펙트를 모름 |
| C#·Unity 권장 | 8.5 | readonly struct·이름 있는 인자·필드 루트에 놓기(EF E-R1). 남는 것: 이펙트 등록 누락이 첫 재생 때에야 드러남(Framework 정책, EF와 같음) |
| 무의미한 If/Tick | 9 | 새 Tick 없음, 접촉마다 "칸이 비었나" 한 번(접촉은 사건당 한두 번) |
| **종합** | **약 8.9** | |

## 4. 정할 것

- **Q-I1. 값 자리:** **A(권장) 공격 표시 SO `impactEffect` 한 칸**(투사체 종류만, 근접은 Empty만) / B 클립 묶음 SO 공격 칸(코드 모션 유닛은 못 씀) / C 새 표(투사체 외형별) — 원본이 늘어남.
- **Q-I2. Ballista:** **A(권장) 대상 접촉마다만** / B 접촉 + 경로 끝에도 같은 이펙트 / C 접촉 이펙트와 끝 이펙트 칸 따로.
- **Q-I3. 회전:** **A(권장) 투사체 수평 진행 방향(필드 위쪽 기준)** — 폭발처럼 대칭인 이펙트엔 차이 없고, 관통처럼 방향 있는 이펙트가 화살 쪽을 봄 / B 필드 위쪽 고정(방향 없음).
- **Q-I4. 이펙트 종류 판정 정리:** **A(권장) 규칙 하나로 모으고 창구·클립 값·폭발 Presenter·새 칸이 부름**(동작 같음) / B 새 칸만 기존 `IsKnownEffect`를 부름(판정 세 곳 그대로).

## 5. Phase 개요 (결정 뒤 Task 계획으로)

| Phase | 내용 | 화면 |
|---|---|---|
| IE-a | 규칙 하나(IE2) + 공격 표시 SO 칸·값·검사(IE1) + 시험 | 변화 없음(기본 Empty) |
| IE-b | 접촉 값(IE3), 애니메이션 셋 맞음 콜백(IE4), 공격 재생 연결·`FireShot`(IE5), Presenter가 창구를 넘김, 시험(접촉 값·네 길 장면) | 칸을 채운 유닛만 이펙트 |
| IE-c | 공유 메모(호진님: 공격 표시 SO 새 칸), 기록 | — |

바뀌는 파일(예상): `DV/CombatAttackPresentationSO.cs`·`Data.cs`·`CombatClipAttackData.cs`, 새 `DV/CombatPresentationEffectRules.cs`, 새 `C/Display/CombatProjectileContact.cs`, `C/Display/CombatMortarAnimation.cs`·`CombatLinearAttackAnimation.cs`·`CombatBallistaAnimation.cs`·`CombatEffectWindow.cs`·`CombatEnvironmentEffectPresenter.cs`, `C/CombatAttackPlayback.cs`, `C/CombatSceneEventPresenter.cs`(한 줄). 시험 도우미 셋(I-F9)과 새 시험. 새 파일 둘은 `.meta`와 함께.
