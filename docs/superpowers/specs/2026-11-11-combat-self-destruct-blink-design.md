# 전투 자폭 깜빡임(터지기 전 붉게 깜빡임) 설계 초안

작성: 2026-11-11 · 상태: **결정 완료(2026-11-11 사용자 "권장대로, 계획 써줘" = Q-SD1~Q-SD5 모두 A).** 같은 날 사용자 "자폭 이펙트를 다른 쪽에서 만들고 있어 넣을 수 있게" → SD8 추가. Task 계획은 [self-destruct-blink-plan](../plans/2026-11-11-combat-self-destruct-blink-plan.md). 코드 변경 없음.

## 0. 요청

사용자(2026-11-11): "자폭 효과를 가진 유닛이 터질 때, 몸이 붉게 빛나면서 몇 번 깜빡이다가 터지면 좋을 것 같다. 진짜 라이팅이 아니라 색만 붉게."
→ 히트 플래시 구조 확인 → 사용자 "A안으로 설계"(깜빡임은 **사망 조립**에서, "자폭할지"는 결과 안의 Core 발동 390으로 판단).

## 1. 지금 코드에서 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 피격 플래시는 텍스처를 건드리지 않는다. 외형 소유자가 ColorSystem 손잡이(PropertyBlock)로 `_BaseColor`(없으면 `_Color`)를 `Lerp(원래색, 플래시색, 세기)`로 덮고, 마지막 층이 끝나면 원래 블록으로 되돌린다. 이동 반투명·디졸브·아웃라인 층과 따로다. | `UnitModelAppearance.cs:329` |
| F2 | 플래시 사슬: 큐 값 `CombatFlashCueEntry`(색·시간·시점·지연) → 조립 `CombatCueComposer`가 Flash 채널을 빌리고 조각을 넣음 → `CombatFlashPiece`가 세기 1 → 0을 **한 번** 선형으로 → 대여 `CombatPieceRunLeases` → 창구 `TryBeginFlash` → `FieldUnitView` 플래시 칸. | `CombatCueComposer.cs:109`, `CombatFlashPiece.cs` |
| F3 | 플래시 큐는 **피격 칸에만** 둘 수 있다(시점은 Start·Cue). | `CombatPresentationCueRules.cs:178` |
| F4 | 자폭은 "피해로 죽으면" 명령 완료 정산에서 터진다. 사건 순서 = [자폭 유닛 사망] → … → [발동 사건 390 `SelfDestructExploded`] → [주변 피해]. 390은 발동 하나짜리 `CombatEffectsEvent`이고, 출처 = 죽은 유닛, `TargetCells` = 폭발 칸(주변 8칸), `TargetCell` = 사망 칸, 근원 = (사망 칸, 그 유닛, 새 공격 그룹). | `DeathTraitApplication.cs:90-118`, `TraitActivationKind.cs:31` |
| F5 | Core는 낙사한 유닛, 승패가 정해진 뒤의 사망에는 자폭을 내지 않는다. 즉 **"터지는지"는 390이 있는지로만 알 수 있다**(View가 규칙을 베끼면 틀림). | `DeathTraitApplication.cs:20-21,55-59` |
| F6 | 지금 View는 사망 사건에서 사망 반응(랙돌·디졸브)으로 몸을 지운다. 390이 올 때는 몸이 없다. | `CombatCharacterEventHandler.cs:141-190`, `CombatUnitReactionPresenter.cs:138-142` |
| F7 | `TraitVisual_SelfDestruct.asset`의 `activations: []` — 390 연출 줄이 없어 지금은 폭발 연출이 전혀 없다. | 에셋 |
| F8 | 동시 묶음: 사망 사건은 그 공격 그룹 묶음 안, 390과 주변 피해는 새 공격 그룹 묶음이다. 묶음 뒤 사건은 앞 묶음이 모두 끝난 뒤 시작한다(동시 처리 Q-C2 A). 390의 주체 = 출처(죽은 유닛)다. | `CombatEventTypeTable.cs:351-372`, 동시 처리 설계 C-1 |
| F9 | 특성 발동 큐의 자리 `EventCell` = `TargetCells`(자폭은 주변 8칸)이고 가운데(사망 칸)를 가리키는 자리는 없다. `ActorBody`는 살아 있는 출처만 찾는다(죽은 유닛은 비어 이펙트가 빠짐). | `CombatCuePoints.cs:115-137`, `CombatShownFacts.cs:385-395,460` |
| F10 | 사망 몸 유지(`HoldDeath`)는 놓을 때까지 사망 사실 반영 뒤에도 모델을 남긴다. 지금은 사망 반응이 끝·취소 두 길에서 놓는다. | `CombatUnitPresentationWindow.cs:207,470-495`, `CombatUnitReactionPresenter.cs:210-229` |
| F11 | 발동 줄의 특정 발동 전용 칸 선례: 위치 교환 시간 연출 `positionSwap`(PositionSwap·MovementChanged 줄에서만). | `TraitActivationVisual.cs:56-60,166-186` |
| F12 | DB에 자폭 특성을 가진 유닛이 **아직 없다**(`SelfDestructTraitAuthoring`을 쓰는 에셋 0). | grep |
| F13 | 넉백으로 죽은 사망 특성 보유자는 Core가 시체를 옮기지만(발동 400 `CorpseDisplaced`, 위치 사건 없음) View는 이것을 보이지 않는다. | `AttackKnockbackCalculator.cs:222`, View grep 0 |

## 2. 설계

### SD1 "자폭할지"의 원본 = 결과 안의 390 (Core 사실 하나)

새 `Playback/Support/CombatSelfDestructDeaths`(가칭)가 결과를 시작할 때(`CombatSceneEventPresenter.BeginPresentation`) 결과 사건을 한 번 훑어 **390 발동의 출처 유닛**을 모은다(`CombatPlacementBatchPlan.Build(result)`와 같은 꼴).

- 출처마다: 발동 사실, 그 발동 줄의 깜빡임 값(SD2, 줄 보관 `CombatActivationRows`에서).
- View는 자폭 특성 보유·피해 사망·낙사·승패 판정을 하지 않는다(F5). 390이 없으면 지금 사망 그대로다.
- 결과 시작·취소·해제 때 비운다(낙사 표지 `RetainFall`·`ForgetFalls`와 같은 수명).

### SD2 깜빡임 값 = 자폭 390 발동 줄의 전용 칸 (Q-SD1)

`TraitActivationVisual`에 `deathBlink` 칸(색·횟수·전체 시간)을 더한다.

- 허용 줄 규칙은 `TraitActivationVisual.AllowsDeathBlink(owner, kind)` 한 곳 = `SelfDestruct` · `SelfDestructExploded`(위치 교환 칸 F11과 같은 꼴). 다른 줄에서 켜면 DB 생성이 멈춘다.
- 꺼짐 = 횟수 0 또는 시간 0(줄 값 `null`). 시간 조건은 설정 Data의 `ValidateSeconds` 하나를 쓴다.
- 기본값(에셋): 빨강, 3번, 0.9초(조정 값).

### SD3 깜빡임 조각 = 플래시 조각에 횟수 (Q-SD3)

`CombatFlashPiece`에 횟수(pulses)를 더한다. 한 번 길이 = 전체 시간 / 횟수, 매번 세기 1 → 0 선형. **횟수 1이면 지금 피격 플래시와 같다**(피격 큐 값은 늘 1).
색은 F1 그대로 같은 색 층·같은 Flash 채널을 쓴다. 텍스처·재질·라이팅은 바꾸지 않는다.

### SD4 자폭 사망 조립 (A안)

사망 처리기 `TryBeginDeath`가 낙사 갈래 다음에 묻는다: **SD1에 이 유닛이 있고, 그 줄에 깜빡임 값이 있고, `CanFlash(id)`면** 자폭 사망 조립, 아니면 지금 사망 반응(회귀 없음).

자폭 사망 조립(유닛 실행, 사망 실행, 일반 완료, Flash 채널 대여):

| # | 항목 | 시점 | 기다림 |
|---|---|---|---|
| 0 | 깜빡임 조각(SD3) | 시작 | 예 |
| — | 사망 칸 큐(반응 값의 `DeathCues`: 소리·흔들림·피격 이펙트) | 기준 시점 Start = 깜빡임 시작, End = 깜빡임 끝 | 큐 규칙대로 |

- 사망 반응(랙돌·디졸브)은 쓰지 않는다. 몸은 `HoldDeath`로 남기고 사망 사실은 지금처럼 바로 반영한다.
- 사망 칸 큐는 지금 사망과 같은 기준이라(치명타 피격 이펙트·사망 소리가 시작 순간) "맞음 → 깜빡임 → 터짐" 순서가 된다.
- 깜빡임 시간만큼 그 공격 묶음이 길어지고, 390·주변 피해 묶음은 그 뒤에 시작한다(F8). 주변 유닛 반응이 폭발과 같은 순간에 시작하는 것은 묶음 규칙이 보장한다.

### SD5 몸이 사라지는 순간 = 자기 390 시작 (Q-SD2)

몸 유지는 SD1 객체가 보관하고, **그 유닛의 390 발동 사건이 시작할 때** 특성 처리기가 놓는다(큐보다 먼저). 결과 취소·해제 때는 SD1이 남은 유지를 모두 놓는다(F10의 "끝·취소 두 길"을 SD1 수명이 맡음).

- 하나만 터지면 "깜빡임 끝 = 390 시작"이라 깜빡임 끝에 놓는 것과 같다.
- 한 공격에 자폭 유닛 둘이 죽으면 둘이 함께 깜빡이고, Core 순서(EntityId 순)대로 하나씩 터진다. 이때 **각자 자기 폭발 순간에 사라진다**(깜빡임 끝에 놓으면 둘째는 빈 칸에서 터짐). 둘째는 첫 폭발·그 피격 반응 동안 원래 색으로 서 있다(깜빡임 대여는 사망 조립 끝에 돌아감).
- 연쇄(폭발이 다른 자폭 유닛을 죽임)는 묶음이 이어져 "깜빡 → 펑 → 깜빡 → 펑"이 된다.

### SD6 폭발 이펙트 = 390 줄의 큐, 새 자리 `ActivationCell` (Q-SD4)

`CombatCuePoint.ActivationCell`(발동 선택 칸 `TargetCell`, 칸 표면·필드 방향)을 더하고 특성 발동 큐 칸에서만 허용한다(`CombatPresentationCueRules.AllowsPoint`). 자리 해석은 `CombatCuePoints.ForActivation` 한 곳(칸 자리 규칙 `CellPose` 재사용), `TargetCell`이 없으면 그 큐는 빠진다.

- 자폭 390 줄 에셋: 가운데 폭발 `Boom` @ `ActivationCell`(회전 보정은 화약통과 같은 값), 필요하면 주변 8칸 `EventCell` 작은 이펙트·흔들림.
- 날아간 유닛의 멈춘 칸을 선택 칸으로 내는 다른 발동(작렬 410)도 같은 자리를 쓸 수 있다(일반 자리).

### SD8 다른 쪽에서 만드는 자폭 이펙트를 꽂는 자리

지금 이펙트는 모두 같은 길로 나온다.

1. **종류:** Render DB 열거 `EffectType`(`DataBaseSystem/Data/Render/Table/Effect/Enum/EffectType.cs`) 값 하나.
2. **프리팹:** Render DB 이펙트 표 `RenderGroup/EffectTable` — 항목 에셋 하나(`effectType`·풀 수·프리팹)를 표 `EffectDataTable.asset`에 등록. 등록되지 않은 종류는 첫 재생 때 이펙트 시스템이 예외를 낸다.
3. **언제·어디서:** View DB의 큐 항목(종류·시점 anchor·자리 point·지연·위치/회전 보정·끝 규칙). 예: 포효 발동 줄 = `Stage1Boss1` @ ActorBody·Start, 화약통 파괴 = `Boom` @ EventCell·회전 (−90,0,0).
4. **재생:** 조립(`CombatCueComposer`·특성은 `CombatMomentCuePlayback.TryPlayAccent`) → 자리 해석(`CombatCuePoints`) → 조각(`CombatEffectPiece`) → 이펙트 창구(`CombatEffectWindow.Play`) → GlobalSystem `EffectScope.Play`.

그래서 자폭 이펙트의 자리는 **자폭 390 발동 줄의 큐 항목 하나**이고, 코드 없이 바꿀 수 있다.

- 이번 작업은 그 큐 항목을 **임시 `Boom`**으로 채운다(타이밍 확인용). 새 이펙트가 오면 ①~②(종류 값·표 항목)를 만들고 ③의 종류 값 하나만 바꾼다.
- **주의(종류 값 순서):** `Cure = 5` 뒤의 값은 번호가 암묵적이다(`Stage1Boss1` = 6 …). 에셋은 번호로 저장되므로 새 값을 **중간에 끼우면 뒤 값을 쓰는 모든 큐·표 항목이 다른 이펙트를 가리킨다.** 새 값은 맨 끝에 붙이거나 번호를 적는다.
- 이펙트 모양에 맞춰 고를 수 있는 것: 가운데 하나(`ActivationCell`), 주변 8칸마다(`EventCell`), 위치·회전 보정, 끝 규칙(자연·시간). 크기 배율 칸은 없다(프리팹 크기로 맞춤).
- 프리팹은 스스로 끝나야 한다(반복 파티클이면 끝 규칙을 시간으로). 끝나지 않으면 창구가 경고한다.

### SD7 범위 밖

- 폭발 소리: 소리 표에 특성 갈래가 없다(특성 발동 연출 계획 P5). 사망 소리는 깜빡임 시작에 그대로 난다.
- 시체 옮김(F13): 넉백으로 죽은 자폭 유닛은 몸이 원래 칸에서 깜빡이고 폭발은 옮겨진 칸(`TargetCell`)에서 난다. 400 연출이 없는 기존 한계이고, 공포 특성도 같다 → 후속 후보.
- 공포(Fear) 등 다른 사망 특성 연출.
- Core 변경 없음. Factory 변경 없음(SD1은 Presenter 생성자 안에서 만들어 처리기 둘에 넘김).

## 3. 정할 것

| # | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-SD1 | 깜빡임 값 자리 | **A** 자폭 390 발동 줄의 전용 칸(SD2) | B 전역 설정 표 `CombatPresentationSettings` 한 묶음(모든 사망 특성 공통) |
| Q-SD2 | 몸이 사라지는 순간 | **A** 자기 390 시작(SD5) | B 깜빡임 끝(유지를 사망 조립 끝에 놓음, 여럿 동시 사망 때 빈 칸 폭발) |
| Q-SD3 | 깜빡임 모양 | **A** 같은 간격 N번(SD3, 횟수 1 = 지금 플래시) | B 점점 빨라짐(가속 값 하나 더) |
| Q-SD4 | 폭발 이펙트 자리 | **A** 390 줄 큐 + 새 자리 `ActivationCell`(SD6) | B 자폭 사망 조립 끝에 몸 자리 이펙트(390 줄을 안 씀, 값 자리가 둘로 나뉨) |
| Q-SD5 | 확인용 자폭 유닛(F12) | **A** 사용자가 확인할 적에 자폭 특성을 붙임(데이터 결정) | B Claude가 로컬 임시 에셋으로 붙이고 커밋에서 뺌 |

## 4. 원칙 점검

- **SSOT:** "터지는지" = Core 390 하나(SD1). 깜빡임 값 = 390 줄 하나. 색 바꾸기 = 외형 소유자 하나(F1, 새 경로 없음). 폭발 자리 = Core `TargetCell`.
- **SRP:** SD1 객체는 "이 결과의 자폭 예정과 몸 유지"만, 조각은 색 곡선만, 사망 처리기는 조립 고르기만 한다.
- **OCP·독립성:** 줄에 깜빡임 값이 없으면 지금 사망 그대로. 피격 플래시는 횟수 1이라 화면 같음. 새 자리 `ActivationCell`은 다른 큐 칸에 영향 없음.
- **FW:** 색은 ColorSystem 공개 손잡이만(FW-203, 기존 경로). Framework·GlobalSystem·Factory 무변경.
- **바꿀 때 함께 볼 곳:** 사망 처리기·특성 처리기 생성자 인자(직접 만드는 기존 시험은 호출만 맞춤), View DB 칸 둘(호진님 공유), 큐 자리 열거 값(그리기 목록·규칙 표).

## 5. Phase(안)

- **SD-a 값:** `deathBlink` 칸·허용 규칙, `ActivationCell` 자리·큐 규칙·자리 해석, 플래시 조각 횟수. 화면 변화 없음.
- **SD-b 재생:** SD1 객체·결과 수명, 자폭 사망 조립·사망 갈래, 390 시작 때 놓기, 취소·해제 정리.
- **SD-c 에셋·문서:** 자폭 390 줄(깜빡임 값·폭발 큐), 공유 메모(호진님: 발동 줄 칸·큐 자리), 묶음 끝 검토.

View 작업이라 새 시험은 쓰지 않는다. 컴파일·재생 확인은 사용자가 Unity에서 한다.

## 6. 구현 메모 (2026-11-11, 계획 §6)

- **SD-a 값:** `CombatDeathBlinkVisualInput`·`CombatDeathBlinkVisualData`, 발동 줄 `deathBlink`·`AllowsDeathBlink`, 큐 자리 `ActivationCell = 7`, `CombatFlashPiece` 횟수(필수, 피격 = 1). 설계와 다른 점: 선택 칸은 그 자리를 고른 큐가 있을 때만 칸 자리로 바꾼다(다른 발동의 선택 칸이 멈추지 않게). 상태 반영 시점 입구에는 선택 칸을 더하지 않았다(몸 자리만 받는 줄).
- **SD-b 재생:** `Playback/Support/CombatSelfDestructDeaths`(SD1·SD5), 사망 처리기 자폭 갈래(낙사 → 반응 설정 → 자폭 → 일반)·`ComposeSelfDestructDeath`, 특성 처리기가 발동 반복 맨 앞에서 몸을 내림, Presenter가 결과 시작·취소(대여를 되돌린 뒤)·끝(최종 사실 전)에 남은 유지를 놓음. Factory 무변경.
- **SD-c 에셋:** `TraitVisual_SelfDestruct.asset`에 390 줄 — 깜빡임 빨강·3번·0.9초, 큐 = `SelfDestruction`(18) @ `ActivationCell`·Start·회전 보정 0·끝 Seconds 5초. 처음에는 임시 `Boom`이었고, 민형님 자폭 이펙트(`5e6980555`, 프리팹만)가 들어와 Claude가 Render DB에 등록(SD8 ①·②: `EffectType` 맨 끝 `SelfDestruction = 18`, 표 항목 에셋·등록)하고 종류 값을 바꿨다.
- **SD4·SD5 바뀜(2026-11-11 사용자 "늘 몸 남김"):** 터질 유닛은 깜빡임을 쓸 수 없어도(줄에서 꺼짐·색 층 없음·반응 설정 없음) 사망 반응 없이 몸만 남겼다가 자기 폭발 순간에 내린다. 자폭 이펙트가 몸이 부서지는 모습이라 죽은 모습을 따로 보이면 안 된다. 깜빡임 없는 길에서는 사망 칸 큐가 나오지 않는다(사건 소리는 남).
- **SD3 넓힘(2026-11-11 사용자 "칸 더해줘"):** 깜빡임 값에 최대 세기·끝 회차 길이 비율(점점 빨라지기)·회차 곡선을 더했다. 기본값이면 같은 간격·바로 붉어졌다 일정하게 돌아옴(Q-SD3 A와 같음). 세기 계산은 View 회차 시간표 `CombatFlashPulses` 한 곳, 곡선 규칙은 DB `CombatFlashPulseShape` 한 곳이고, 피격 플래시는 한 회차 기본값이라 옛 값과 같다.
- **확인(Claude, Unity 배치·테스트 코드 없음):** 단계마다 컴파일 성공, 실제 카탈로그 전체 DB 생성 시험 통과(390 줄 포함), `StructureHitPresentationTests` 22 통과. `DataBaseAssetTests`의 InGame 데이터 실패 7건은 세 번 모두 같았다(무관으로 봄, HEAD 기준 판 미실행). 깜빡임·폭발 화면은 자폭 적이 생긴 뒤 사용자 확인(계획 Uc).
