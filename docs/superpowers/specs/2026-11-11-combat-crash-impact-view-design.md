# 작렬(CrashImpact) 전투 View 대응 설계

- 작성: 2026-11-11, View(용근)
- 상태: **결정 완료(2026-11-11 사용자 "권장안대로" = Q-CV1~Q-CV5 모두 A)**, 코드 변경 없음, Task 계획 [crash-impact-view-plan](../plans/2026-11-11-combat-crash-impact-view-plan.md)
- 받은 것: [Core → View 인계](../handoffs/2026-11-10-crash-impact-view-handoff.md)(천명님 `810dfc682`), Core 설계 [crash-impact-trait-design](2026-11-10-crash-impact-trait-design.md)
- 점검 기준: SSOT · SOLID · JMH Framework 지침·FW 계약 · 기능 변경의 독립성 · 캡슐화 · C#/Unity 권장 구조 · 무의미한 If/Tick 남발

## 1. 확인 사실 (코드 대조, 2026-11-11)

### 1.1 Core가 주는 것

인계 문서와 코드가 맞습니다.

| 순간 | 사실 | 코드 |
|---|---|---|
| 날기 시작 | `CombatEffectsEvent` 안 `KnockbackApplied`(키 520). 대상 = 날아간 유닛, 칸 = 시작 + 경로, 근원 = 직접 타격 근원. 이동 사건보다 앞 | `AttackKnockbackCalculator.cs:140-142, 149-158` |
| 날아가는 중 | `Moved`, `MovementCause` = `Forced` + `TraitKey.CrashImpact` | 같은 파일 `:134-136` |
| 부딪힘 | `CrashImpactCollided`(410). 대상 = 날아간 유닛(+ 부딪힌 유닛), 칸 = 막은 칸 하나(**맵 밖일 수 있음**), 선택 칸 = 멈춘 칸, 근원 = 부딪힘 근원(새 공격 그룹) | `CrashImpactCollision.cs:63-66` |
| 부딪힘 피해 | 체력 사건, 근원 = 부딪힘 근원, 날아간 유닛 → 부딪힌 유닛 순 | 같은 파일 `:67-83` |
| 미리보기 | `AttackKnockbackSnapshot.Collision`(막혀 멈춘 캐릭터 넉백 + 작렬일 때만), 부딪힌 유닛 보조 행 | `AttackKnockbackSnapshot.cs` |

### 1.2 View에 이미 되는 것 (코드 변경 없이)

- **재생 순서.** 사건 형식 표(`CombatEventTypeTable`)가 발동 사건의 근원을 `ResultOrigin`에서 읽습니다(`:376-389`). 그래서 부딪힘 발동과 부딪힘 피해는 같은 근원 열쇠로 한 동시 묶음이 되고, 직접 타격 묶음과는 따로입니다. 순차 재생이 Core 순서(타격 → 걸음 → 부딪힘)대로 돕니다. 인계 §4의 첫 확인은 **코드 변경 없이 Unity 재생 확인 항목**입니다.
- **보조 행 예상 체력.** `CombatUnitHudFeedback.ShowAttackHealthPreview`가 `AttackPreview.Targets` 전부를 돌아, 부딪힌 유닛(보조 행)의 HUD에도 예상 체력이 이미 나옵니다(`:34-44`).
- **부딪힘 피해의 피격 반응.** 일반 체력 사건이라 캐릭터 처리기의 피격 조립을 그대로 탑니다(Unity 확인 항목).

### 1.3 빠진 것

- 특성 외형 키 520이 없습니다 → HUD·정보창 특성 칸에서 빠집니다.
- 설명 토큰 줄이 없습니다(`TraitDescriptionBindings`).
- 발동 연출 줄이 없어 두 발동이 연출 없이 지나갑니다.
- 날아가는 걸음은 일반 밀림(`CombatMovementKind.Forced`, 걷기)으로 보입니다.
- 넉백 화살표는 막힌 것(`Blocked`, 빨강)과 부딪힘을 구분하지 않습니다.

### 1.4 찾은 결함 (발동 줄을 넣는 순간 드러남)

**F1 — 맵 밖 발동 칸에서 전투가 멈춥니다.** `CombatCuePoints.ForActivation(TraitActivationSnapshot, …)`(`:119-142`)은 줄의 큐가 칸 자리를 쓰는지와 상관없이 **모든 `TargetCells`의 자리를 먼저 계산**하고, `CellPose`는 필드 밖 칸이면 예외입니다(`:99-100`). 맵 끝에 부딪힌 작렬 발동에 줄이 하나라도 있으면 F1입니다. 이 함수의 문서 주석은 이미 "찾지 못한 몸·칸은 목록에 넣지 않고 넘깁니다"라고 적고 있어, 코드가 주석과 어긋난 것입니다. 지금까지는 맵 밖 칸을 주는 발동이 없어서 드러나지 않았습니다.

### 1.5 겹치는 파일

`CombatCuePoints.cs`·`CombatCuePoint.cs`·`CombatPresentationCueRules.cs`는 커밋되지 않은 재구성 작업(`SelectedCell` 자리 추가, [재구성 설계](2026-11-11-combat-reconstruction-view-design.md))이 고치고 있습니다. 이 설계의 CV4는 그 변경 위에서 하고, 같은 커밋 묶음이거나 재구성 묶음이 먼저 커밋되는 것을 전제로 합니다.

## 2. 설계

### CV1 특성 외형 에셋 (데이터만)

- 새 `ViewGroup/TraitVisual/Item/TraitVisual_CrashImpact.asset`(키 520, 이름 "작렬", 아이콘 빔), `TraitVisualDataTable.asset` 끝에 등록합니다.
- 설명(초안): "기본 공격으로 피해를 입힌 대상을 {날아가는거리}칸 날려 보냅니다.\n벽·구조물에 부딪히면 대상이 최대 체력의 {벽피해율}%, 유닛에 부딪히면 둘 다 대상 최대 체력의 {유닛피해율}% 피해를 받습니다."
- 코드 변경은 없습니다. 에셋과 표 등록은 한 커밋입니다.

### CV2 설명 토큰 한 줄

`TraitDescriptionBindings`에 `CrashImpact` 줄 하나(`DistanceCells`·`WallDamagePercent`·`UnitDamagePercent`). 토큰 원본은 이 표 한 곳이고, 다른 특성과 같은 꼴입니다.

### CV3 발동 연출 줄 둘 (데이터만)

| 줄 | 시점 | 큐(권장 자리) |
|---|---|---|
| `KnockbackApplied` | ActivationStart | 대상 몸(`SubjectBody`)에 날기 시작 이펙트(선택, 비워도 됨) |
| `CrashImpactCollided` | ActivationStart | 대상 몸(`SubjectBody`, 날아간 유닛·부딪힌 유닛 각각)에 충돌 이펙트 + 카메라 흔들림 |

- **칸 자리를 쓰지 않습니다.** 막은 칸(`EventCell`)은 맵 밖일 수 있고, 멈춘 칸(`SelectedCell`)은 날아간 유닛 몸과 같은 자리입니다. 몸 자리가 두 유닛 모두를 덮습니다.
- 유닛 행동·추가 공격 칸은 쓰지 않습니다.
- 어떤 이펙트 종류(`EffectType`)를 쓸지는 사용자가 정합니다(Q-CV3).

### CV4 맵 밖 발동 칸 (F1 고침)

- `ForActivation(TraitActivationSnapshot, …)` 안에서 필드 밖 칸은 목록에 넣지 않습니다. 선택 칸도 필드 밖이면 null입니다. 주석이 말하던 동작으로 코드를 맞추는 것입니다.
- 칸 자리의 규칙 원본은 그대로 `CellPose` 하나입니다. 순간 큐(`ForCell`) 등 다른 호출자는 지금처럼 필드 밖이면 예외입니다. Core가 필드 밖 칸을 주지 않는 사건이라 예외가 계약 위반을 알려 주기 때문입니다.
- 판정은 `mapper.IsInside` 한 번이고, 칸 하나당 이미 하던 검사를 예외 대신 건너뜀으로 바꾸는 것이라 검사가 늘지 않습니다.
- DB 검사에서 "작렬 부딪힘 줄은 EventCell 금지"로 막는 안은 쓰지 않습니다. 특성 하나의 사실(막은 칸이 맵 밖일 수 있음)이 DB 규칙에 새어 들어가고, 다음에 같은 성질의 발동이 생기면 또 한 줄이 필요해서입니다.

### CV5 날아가는 걸음 모양 (선택, Q-CV1)

- 이동 종류에 `Flung = 2`(날려 감)를 더합니다. 기존 확장 규칙(`CombatStepStyles` 머리 주석: 열거 값 하나 + 성질 한 줄 + 모양 + 표 한 줄)을 그대로 따릅니다.
- 판정은 원본 `CombatMovementKinds.Of` 한 곳입니다. 원인이 `Forced`이고 원인 특성이 `CrashImpact`면 `Flung`입니다.
- 성질은 밀림과 같습니다(`IsSelfDriven` = false → 회전 없음, 접촉 Gate 규칙 그대로).
- 모양은 새 클래스 없이 **걷기 모양을 다른 시간으로** 씁니다(`CombatWalkStepStyle(flungStepSeconds, climbHop)`). 빠르게 미끄러지듯 일직선으로 날아갑니다. 값은 설정 표 "유닛 이동 연출" 묶음의 새 칸 `flungStepSeconds`(기본 0.08초, 조건 함수로 > 0 검사)입니다.
- 걸음 레인·배치 대여·접촉 Gate·이동 표시는 바뀌지 않습니다(종류 → 모양 표가 흡수).
- 판정 자리를 코드 한 줄로 둘지(권장), 특성 외형 표의 칸으로 둘지는 Q-CV2입니다.

### CV6 미리보기 부딪힘 표지 (선택, Q-CV4)

- 표지 종류에 `Collision = 4`를 더합니다. 바꾸기 원본 `CombatKnockbackPreview.KindOf`가 멈춤 이유 대신 넉백 기록 하나를 받아, `Collision != null`이면 `Collision`, 아니면 지금 규칙입니다(원본 한 곳 유지).
- 색은 `FieldView` 씬 값 `knockbackCollisionColor` 하나를 더해 `FieldKnockbackMarkers`의 색 갈래에 한 줄로 넣습니다(기본 주황, 씬에서 조정).
- 막은 칸 자체에 표지를 놓지는 않습니다. 맵 밖일 수 있고, 화살표 끝이 이미 그 앞 칸을 가리킵니다.

## 3. 7기준 점검 (설계 기준)

| 기준 | 점검 | 결과 |
|---|---|---|
| SSOT | 토큰 = `TraitDescriptionBindings`, 이동 종류 판정 = `CombatMovementKinds.Of`, 표지 종류 = `KindOf`, 칸 자리 = `CellPose`. 새 사본 없음 | 9.5 |
| SOLID | 새 이동 종류·표지 종류는 기존 확장점(열거 + 표 한 줄)으로. Q-CV2 A면 `Of`가 특성 키 하나를 아는 것이 OCP 낮음 1 | 9.0 |
| FW 계약 | Factory·서비스 키 변경 없음. 설정 칸이 값 묶음을 거치면 연결 줄 하나 가능(민형님 공유) | 9.5 |
| 독립성 | CV1~CV3은 데이터만, CV4는 함수 하나, CV5·CV6은 서로 독립이라 따로 넣고 뺄 수 있음 | 9.5 |
| 캡슐화 | 새 공개 멤버 없음. `KindOf`는 internal 그대로 | 9.5 |
| C#/Unity | 열거 값 명시, 에셋은 코드와 한 커밋, `MovedFrom` 대상 없음 | 9.5 |
| If/Tick | Tick 검사 추가 없음. `Of`에 조건 한 줄, `ForActivation`은 예외 → 건너뜀(검사 수 같음) | 9.5 |

예상(알려진 문제 기준) **약 9.4**. 남는 낮음은 Q-CV2 A의 특성 키 한 줄입니다.

## 4. 결정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-CV1 | 날아가는 걸음 모양(CV5)을 이번에 넣나 | **A 넣음** / B 일반 밀림 그대로 |
| Q-CV2 | `Flung` 판정 자리 | **A `CombatMovementKinds.Of`에 원인 특성 한 줄**(작음, 같은 꼴의 특성이 늘면 B로) / B 특성 외형 표에 "밀림 걸음 모양" 칸(데이터로 고름, 판정에 표를 주입해야 해 연결이 늘어남) |
| Q-CV3 | 두 발동 줄의 이펙트·흔들림 값 | **A 날기 시작 = 비움, 부딪힘 = 대상 몸 `Boom` + 흔들림(포효와 같은 값)** — 임시, 사용자가 에디터에서 바꿈 |
| Q-CV4 | 미리보기 부딪힘 표지(CV6)를 넣나 | **A 넣음(색만 다름)** / B 지금처럼 Blocked |
| Q-CV5 | F1(CV4) 고침 범위 | **A 특성 발동 자리 함수만 건너뜀** / B `CellPose` 자체를 건너뜀으로(모든 호출자, 다른 계약 위반이 조용해짐) |

## 5. Phase(안)

| Phase | 내용 | Unity 확인 |
|---|---|---|
| CV-a | CV1 에셋·표 등록, CV2 토큰 한 줄, CV4 F1 고침, CV3 발동 줄 둘 | 정보창·HUD 특성 칸, 설명 숫자, 맵 끝·벽·구조물·유닛 부딪힘, 바로 옆이 막힌 경우, 재생 순서(타격 → 날아감 → 부딪힘), 부딪힌 유닛 피격 반응 |
| CV-b | CV5 이동 종류 `Flung`·설정 칸·기본 에셋 값 | 날아가는 걸음 모양, 일반 넉백·충격 볼트는 그대로 |
| CV-c | CV6 표지 종류·색 | 호버 화살표 색(부딪힘 / 막힘 / 밀림 / 면역), 보조 행 예상 체력 |
| CV-d | 문서·공유·묶음 끝 검토 한 번 | — |

새 시험은 쓰지 않습니다(View 작업 규칙). 기존 시험은 컴파일이 깨지는 곳만 맞춥니다(`CombatStepStyles` 생성자를 부르는 우리 시험, `KindOf`를 부르는 우리 시험이 있으면 호출만).

## 7. 구현 메모 (2026-11-11, 컴파일 미확인)

- 설계와 다른 점: Q-CV3 A의 흔들림 값은 계획 TP-2대로 사망 흔들림 값(0.3·0.3초)을 넣었다(포효 값은 세기 1·1초라 큼, 에디터에서 바꿀 수 있음). 날기 시작 줄은 큐가 비어 두지 않았다(TP-1).
- 나머지는 설계대로다. 기록은 계획 §4.1.

## 6. 공유(CV-d, 모두 미전달 예정)

- 천명님: 인계 §4 답 — 재생 순서는 근원 열쇠로 따로 묶여 코드 변경 없음, 바로 옆이 막힌 경우도 칸 하나짜리 발동으로 처리됨. Core 요청 없음.
- 민형님: 설정 칸이 Factory 값 묶음 줄을 거치게 되면 그 한 줄(CV-b에서 확인).
- 호진님: 특성 외형 표에 새 에셋 하나(설정 표 메모에 `flungStepSeconds`).
