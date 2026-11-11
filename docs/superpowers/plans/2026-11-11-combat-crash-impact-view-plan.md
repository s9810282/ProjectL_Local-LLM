# 작렬(CrashImpact) 전투 View 대응 Task 계획

- 작성: 2026-11-11, View(용근)
- 설계: [crash-impact-view-design](../specs/2026-11-11-combat-crash-impact-view-design.md) (CV1~CV6, 결정 Q-CV1~Q-CV5 모두 A — 2026-11-11 사용자 "권장안대로")
- 상태: **계획 작성, 코드 착수 전.** 계획에서 정한 것 TP-1~TP-10은 사용자 확인 대상이다(§1).
- 진행 규칙: Phase가 끝나면 멈추고 보고한다. 새 시험은 쓰지 않는다. MSBuild는 돌리지 않는다(컴파일 미확인, Unity 확인은 사용자). 묶음 끝 검토는 CV-d에서 한 번 한다.

## 0. 착수 조건

- `CombatCuePoints.cs`(CV4)는 커밋되지 않은 재구성 작업(`SelectedCell`)이 고치고 있다. **그 변경 위에서** 고치고, 커밋 때는 재구성 묶음과 함께 넣거나 재구성 묶음을 먼저 커밋한다.
- 시험 참조 확인(2026-11-11): `CombatStepStyles`·`CombatKnockbackPreview.KindOf`·`FieldKnockbackMarkers` 생성자·`CombatEventPresentationValues` 생성자를 부르는 시험은 없다. 고칠 시험이 없을 것으로 본다.

## 1. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 날기 시작(`KnockbackApplied`) 줄은 **만들지 않는다** | Q-CV3 A = "비움". 큐가 빈 줄은 줄이 없는 것과 같은 화면이고, 빈 줄만 늘어난다. 나중에 이펙트를 넣을 때 줄을 만든다 |
| TP-2 | 부딪힘 흔들림 값은 **사망 흔들림 값(세기 0.3·0.3초·모양 3)** 으로 한다 | Q-CV3 A는 "포효와 같은 값"이었지만, 포효 값은 세기 1·1초라 부딪힐 때마다 흔들기에는 크다. **결정과 다른 점이라 확인 필요** — 포효 값을 원하면 그대로 넣는다 |
| TP-3 | 부딪힘 이펙트 = `Boom`(2), 자리 `SubjectBody`(4), 시점 Start(1), 회전 보정 (−90, 0, 0) | 자폭 외형과 같은 이펙트·보정. 몸 자리라 날아간 유닛·부딪힌 유닛 둘 다에 나온다 |
| TP-4 | 외형 에셋은 기존 에셋 YAML을 본떠 새 GUID로 만든다. 아이콘 빔 | 기존 신규 특성 외형과 같은 방법 |
| TP-5 | F1 고침은 `ForActivation(TraitActivationSnapshot, …)` 안에서 `TargetCells`와 `TargetCell` 둘 다 `IsInside`로 거른다. 필드 변환은 칸이 있을 때 한 번 읽는다 | 설계 CV4·Q-CV5 A. `CellPose`의 예외 규칙은 그대로 |
| TP-6 | 날려 감 걸음 시간은 설정 표 "유닛 이동 연출" 묶음의 새 칸 `flungStepSeconds`(기본 0.08초, 조건 = 걸음 시간 조건 `ValidateStepSeconds`)이다 | 같은 묶음의 걸음 값과 한 자리 |
| TP-7 | 그 값은 사건 Presenter 값 묶음 `CombatEventPresentationValues`의 **필수 인자**(`stepClimbHop` 뒤)로 받는다. 자발 이동 값 객체 `CombatStepLookSettings`에는 넣지 않는다 | 생성자를 부르는 곳이 `From` 하나라 필수 인자가 안전하다. 자발 이동 값 객체에 밀림 값을 섞지 않는다. Factory는 `From`만 부르므로 바뀌지 않는다 |
| TP-8 | `Flung` 모양 = `CombatWalkStepStyle(flungStepSeconds, StepClimbHop)` | 높이 다른 칸 뜀 값은 밀림과 같이 쓴다. 새 모양 클래스 없음(설계 CV5) |
| TP-9 | 표지 바꾸기 `KindOf`의 인자를 멈춤 이유 → 넉백 기록(`AttackKnockbackSnapshot`)으로 바꾼다. 호출자는 같은 파일 한 곳 | 바꾸기 원본 한 곳 유지(설계 CV6) |
| TP-10 | 부딪힘 표지 색은 `FieldView` 씬 값 `knockbackCollisionColor`(기본 (1, 0.55, 0.1, 1) 주황), `FieldKnockbackMarkers` 생성자에 인자 하나 | 다른 표지 색과 같은 자리·같은 꼴. `Combat.unity`는 다음 저장 때 칸이 생긴다 |

## 2. Phase와 Task

### CV-a 데이터 + F1 고침 (CV1·CV2·CV3·CV4)

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | `ViewSystem/Descriptions/Traits/TraitDescriptionBindings.cs` | `[TraitKey.CrashImpact]` 줄: 토큰 `날아가는거리`(`DistanceCells`)·`벽피해율`(`WallDamagePercent`)·`유닛피해율`(`UnitDamagePercent`). 키 순서는 알파벳 자리(`CleanSweep` 뒤) |
| Ta.2 | `ViewSystem/Combat/Presentation/Pieces/CombatCuePoints.cs` | `ForActivation(TraitActivationSnapshot, …)`: 필드 밖 칸은 목록에서 빼고, 선택 칸이 필드 밖이면 null(TP-5). 문서 주석에 "필드 밖 칸(작렬의 맵 끝 부딪힘 등)은 넣지 않는다" 한 줄. `CellPose`는 그대로 |
| Ta.3 | 새 `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_CrashImpact.asset`(+ `.meta`) | 키 520, 이름 "작렬", 설명(설계 CV1 문구, 토큰 셋), 발동 줄 하나 `CrashImpactCollided`(410): ActivationStart, 이펙트 TP-3, 흔들림 TP-2 |
| Ta.4 | `DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset` | 끝에 새 에셋 한 줄 |

Unity 확인(Ua):
1. 해머 정보창·HUD 특성 칸에 "작렬"이 나오고, 설명 숫자가 3·10·10으로 바뀐다.
2. 벽·맵 끝·구조물·유닛 부딪힘에서 이펙트·흔들림이 나오고, 맵 끝에서 멈추지 않는다(F1).
3. 바로 옆이 막힌 경우(한 칸도 못 감)에도 부딪힘 연출과 피해가 나온다.
4. 재생 순서가 "타격 → 날아감 → 부딪힘 피해"로 보인다(인계 §4).
5. 부딪힌 유닛이 피격 반응·피해 숫자를 보인다. 여러 대상이 날아갈 때 순서가 Core 순서와 같다.
6. 다른 특성 발동 연출(자폭·포효 등)이 그대로다.

커밋: 에셋 둘과 표는 한 커밋(없으면 표 참조가 깨짐). `CombatCuePoints.cs`는 §0대로.

### CV-b 날려 감 걸음 (CV5)

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` | "유닛 이동 연출" 묶음에 `flungStepSeconds`(툴팁·`Min(.01f)`)·읽기 `FlungStepSeconds`·`Validate()`에 걸음 시간 조건 한 줄(맨 뒤, 파일 규칙) |
| Tb.2 | `DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` | `flungStepSeconds: 0.08` |
| Tb.3 | `ViewSystem/Combat/Presentation/Movement/CombatMovementKind.cs` | `Flung = 2`(작렬처럼 날려 가는 밀림) |
| Tb.4 | `ViewSystem/Combat/Presentation/Movement/CombatMovementKinds.cs` | `Of`: 원인이 Forced이고 원인 특성이 `CrashImpact`면 `Flung`. `IsSelfDriven`: `Flung => false`. 문서 주석에 한 줄 |
| Tb.5 | `ViewSystem/Combat/Playback/Support/CombatEventPresentationValues.cs` | 필수 인자 `flungStepSeconds`(TP-7)·검사·속성 `FlungStepSeconds`·`From`이 설정 값을 넘김 |
| Tb.6 | `ViewSystem/Combat/Presentation/Movement/CombatStepStyles.cs` | 생성자 셋째 인자 `flung`, `From`이 `new CombatWalkStepStyle(values.FlungStepSeconds, values.StepClimbHop)`(TP-8). 머리 주석의 "밀림은 늘 걷기" 문장 고침 |

Unity 확인(Ub):
1. 작렬로 날아가는 걸음이 일반 밀림보다 빠르게 일직선으로 간다.
2. 일반 넉백·충격 볼트·끌어오기·자발 이동은 그대로다.
3. 날아가는 동안 유닛이 돌지 않고, 고지대 턱이 있는 경로에서도 높이가 자연스럽다.

커밋: 설정 SO 코드와 기본 에셋은 한 커밋(칸이 없으면 기본값 0.08로 읽혀 동작은 같다).

### CV-c 미리보기 부딪힘 표지 (CV6)

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | `ViewSystem/Combat/Display/Field/ICombatKnockbackCanvas.cs` | `FieldKnockbackMarkKind.Collision = 4` |
| Tc.2 | `ViewSystem/Combat/Display/Feedback/CombatKnockbackPreview.cs` | `KindOf(AttackKnockbackSnapshot)`: `Collision != null`이면 `Collision`, 아니면 지금 멈춤 이유 규칙(TP-9). `MarkOf`의 호출 한 줄 |
| Tc.3 | `ViewSystem/Combat/Display/Field/FieldKnockbackMarkers.cs` | 생성자 인자 `collisionColor`·필드·`ColorOf` 한 줄 |
| Tc.4 | `ViewSystem/Combat/Display/Field/FieldView.cs` | 씬 값 `knockbackCollisionColor`(TP-10, 넉백 색 묶음 안)·생성 줄 인자 하나 |

Unity 확인(Uc):
1. 해머 공격 호버 때 벽·유닛 앞에서 멈출 대상의 화살표가 부딪힘 색으로, 일반 넉백 유닛의 막힘은 지금 색 그대로다.
2. 부딪힌 유닛(공격 대상 아님)의 HUD에 예상 체력이 나온다.
3. 면역·완주 표지는 그대로다.

### CV-d 문서·공유·검토

| Task | 내용 |
|---|---|
| Td.1 | 설계 §7 구현 메모(계획과 다른 점), 폴더 배치 안내에 바뀐 규칙이 있으면 한 줄(이동 종류 셋) |
| Td.2 | 공유(모두 미전달): 천명님 — 인계 §4 답(재생 순서·바로 옆 막힘, 요청 없음), Core 전달 문서에 한 절(제목 갱신). 호진님 — 특성 외형 표 새 에셋·설정 표 새 칸. 현강님 — `TraitDescriptionBindings` 줄(현강님 파일이면) |
| Td.3 | 묶음 끝 검토 에이전트 하나(7기준, 바뀐 파일 전부) → 결과 보고, 반영은 사용자 결정 |
| Td.4 | 평가 아티팩트 '적용 기록'은 이 작업이 평가 목록 밖이라 차수 기록만(점수 변화 없음) |

## 3. 바뀌는 파일 요약

- 코드: `TraitDescriptionBindings.cs`, `CombatCuePoints.cs`, `CombatPresentationSettingsDataSO.cs`, `CombatMovementKind.cs`, `CombatMovementKinds.cs`, `CombatEventPresentationValues.cs`, `CombatStepStyles.cs`, `ICombatKnockbackCanvas.cs`, `CombatKnockbackPreview.cs`, `FieldKnockbackMarkers.cs`, `FieldView.cs`
- 에셋: 새 `TraitVisual_CrashImpact.asset`(+ `.meta`), `TraitVisualDataTable.asset`, `CombatPresentationSettings_Default.asset`
- 새 `.cs` 없음. Factory·서비스 키·Core 변경 없음. 시험 변경 없음(예상).

## 4. 실행 기록

### 4.1 CV-a~CV-d 코드·에셋·문서 (2026-11-11, 사용자 "CV-d 까지 진행해" = TP-1~TP-10 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 Ua·Ub·Uc 대기, 커밋 전)

- **CV-a:** `TraitDescriptionBindings`에 `CrashImpact` 줄(토큰 셋). `CombatCuePoints.ForActivation(TraitActivationSnapshot, …)`이 필드 밖 칸·선택 칸을 건너뜀(필드 변환은 칸이 있을 때 한 번 읽음, 연결 전이면 `CellPose`가 지금처럼 예외 — 판정 `IsShownCell`). 새 `TraitVisual_CrashImpact.asset`(+ `.meta`, 키 520, 부딪힘 줄 하나: `Boom`·`SubjectBody`·Start·(−90,0,0), 흔들림 0.3·0.3·모양 3)·표 끝 한 줄.
- **CV-b:** 설정 SO `flungStepSeconds`(0.08, 검사는 `Validate()` 맨 뒤)·읽기 `FlungStepSeconds`·기본 에셋 한 줄, `CombatMovementKind.Flung = 2`, `CombatMovementKinds.Of`(원인 Forced + 작렬 → `Flung`)·`IsSelfDriven`(`Flung` = false), 값 묶음 필수 인자 `flungStepSeconds`(`stepClimbHop` 뒤), `CombatStepStyles` 셋째 인자 = `CombatWalkStepStyle(FlungStepSeconds, StepClimbHop)`. 이동 종류를 쓰는 곳은 모두 `IsSelfDriven`을 거쳐 다른 곳은 고치지 않음. 끌어오기 레인은 그대로 `Forced` 모양.
- **CV-c:** `FieldKnockbackMarkKind.Collision = 4`, `KindOf(AttackKnockbackSnapshot)`(부딪힘 사실이 있으면 Collision), `FieldKnockbackMarkers` 생성자 인자·색 갈래, `FieldView.knockbackCollisionColor`(기본 주황)·생성 줄.
- **CV-d:** 호진님 설정 표 메모 §16, Core 전달 문서 §70(인계 §4 답, 요청 없음, 제목 갱신) — 모두 미전달. 민형님 공유 없음(Factory 무변경). 폴더 배치 안내는 새 파일·새 규칙이 없어 고치지 않음. 평가 아티팩트 적용 기록은 평가 목록 밖 작업이라 이번에는 올리지 않음(사용자가 원하면 차수만 더함).
- **계획과 다른 점:** 없음. 시험 참조 없음을 다시 확인(`Assets/Tests`에 바뀐 서명 호출 0).
- **줄 끝(기록):** 설정 기본 에셋은 Git Bash `sed -i`로 줄을 넣으면서 작업 사본이 LF가 됐다(저장소 판도 LF라 diff는 한 줄).

### 4.2 묶음 끝 검토 (2026-11-11, 새 에이전트 하나, 읽기 전용)

7기준 약 **8.9**(SSOT 9.0·SOLID 8.5·FW 9.5·독립성 9.0·캡슐화 9.0·C#/Unity 9.0·If/Tick 8.5). 컴파일 오류 후보 없음, 외형 에셋은 DB 검사를 지남(대조: 발동 줄·큐 규칙·설명 토큰).

| 번호 | 심각도 | 내용 | 권장 |
|---|---|---|---|
| H1 | **높음(결함, 직접 확인)** | 맵 끝 부딪힘 피해의 근원 칸이 맵 밖이라, 맞은 방향 `CombatHitDirection.Resolve`(`:43`)의 `CellCenterToWorld`가 예외 → 피격 반응·피격 이펙트·래그돌 셋이 부르는 순간 F1. CV4는 큐 자리만 고쳐 Ua 2번이 실패할 것. 계획 밖 파일 | 방향을 칸 차이로 구함: `FieldGridMapper`에 `StepToWorld(step) = 필드 루트 TransformVector(StepToLocal(step))` 한 줄, `Resolve`가 `StepToWorld(victim − source)`를 씀. 판 안 칸이면 옛 식과 같은 벡터(칸 중심 식이 선형) |
| L2 | 낮음 | `ForActivation`에서 칸마다 `IsInside`를 두 번 보고, `field == null`이면 참을 돌려 예외를 미루는 꼴이 읽기 어렵다(설계 "검사가 늘지 않음"은 틀림) | 연결 전이면 위에서 바로 예외, 칸마다 `IsInside` 한 번 |
| L3 | 낮음 | 맵 밖 칸 건너뛰기가 모든 발동 종류에 걸려 다른 발동의 계약 위반도 조용해짐(Q-CV5 A로 받아들인 차이) | 그대로 |
| L4 | 낮음 | 이동 종류 판정이 특성 키 하나를 앎(OCP, Q-CV2 A) | 그대로(늘면 Q-CV2 B) |
| L5 | 낮음 | `CombatStepStyles` 머리 주석 "새 종류 = 모양 클래스 하나"가 `Flung`(걷기 다시 씀)과 안 맞음 | 주석 고침 |
| L6 | 낮음 | `flungStepSeconds` 검사 세 번(설정·값 묶음·걷기 모양) — 기존 `stepSeconds`와 같은 꼴 | 그대로 |
| L7 | 낮음 | 설명 문구 "최대 체력의 N% 피해"는 방어 전 값, "벽·구조물"에 맵 끝·고지대 턱 빠짐 | 기획 확인(문구는 에셋에서 바꿈) |
| 참고 | — | 부딪힘 발동 주체 = 작렬 보유자. 날아간 유닛 걸음이 끝나기 전 이펙트가 시작되면 몸 자리를 날기 전 자리에서 읽을 수 있음 — 묶음 뒤 사건 규칙(Q-C2 A)으로 막힐 것으로 보나 끝까지 따라가지 않음 | Unity 확인 Ua 4번에 포함 |

반영은 사용자 결정 대기.

### 4.3 검토 권장 반영 (2026-11-11 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **H1:** `FieldGridMapper.CellOffsetToWorld(offset)`(필드 루트 `TransformVector(StepToLocal(offset))`, 판 밖 칸도 받음) 한 줄, `CombatHitDirection.Resolve`가 칸 중심 위치 둘의 차이 대신 `CellOffsetToWorld(victim − source)`를 씀. 칸 중심 식이 선형이라 판 안 칸이면 옛 벡터와 같다(화면 변화 없음). 이제 맵 끝 부딪힘 피해의 피격 반응·피격 이펙트·래그돌이 막은 칸 쪽에서 튕겨 나오는 방향을 받는다. 피해 근원 칸을 화면 좌표로 바꾸는 곳은 이 함수 하나뿐임을 다시 확인(`PhysicalCell` 사용처 대조).
- **L2:** `CombatCuePoints`에 칸 자리 식 `PoseOf`·연결 확인 `RequireField`를 나눔. `CellPose`는 그대로(연결 전·필드 밖 예외), `ForActivation`은 칸이 있으면 연결을 한 번 확인하고 칸마다 `IsInside` 한 번으로 건너뜀(`IsShownCell` 삭제).
- **L5:** `CombatStepStyles` 머리 주석 — 새 종류는 있는 모양 클래스를 다른 값으로 다시 쓰거나 새 클래스 하나.
- 그대로: L3·L4·L6. L7(설명 문구)은 기획 확인 대상으로 남김.
- 바뀐 파일 더함: `ViewSystem/Combat/Display/Field/FieldGridMapper.cs`, `ViewSystem/Combat/Presentation/Reaction/CombatHitDirection.cs`(둘 다 우리 줄).

### 4.4 해머 공격 이펙트·부딪힘 이펙트 바꿈 (2026-11-11 사용자 "해머 공격 시 작은 Boom → CombatAttack_Hammer 만들어 추가, 부딪힘은 RockFall 이펙트를 작은 크기로", 에셋만, 컴파일·Unity 미확인)

- 새 `ViewGroup/CombatPresentation/AttackSetting/CombatAttack_Hammer.asset`(+ `.meta`): `CombatAttack_Melee`와 같은 코드 모션·소리 시점에 이펙트 큐 하나 — `Boom`(2), 기준 Hit(맞는 순간), 자리 `ActorBody`(해머 몸, 모델 방향), 위치 보정 (0, 0, 0.8)(몸 앞쪽, 임시), 회전 보정 (−90, 0, 0). 근접 공격 큐는 `ActorEffectPoint`·`ActorBody`만 받는데, `Boomber.prefab`의 이펙트 지점(`effectPoint`)이 비어 있어 몸 자리로 넣었다.
- `UnitVisual_Boomber`(키 24)의 공격 표시를 `CombatAttack_Melee` → `CombatAttack_Hammer`로. 다른 근접 유닛 여섯은 `CombatAttack_Melee` 그대로.
- `TraitVisual_CrashImpact` 부딪힘 줄 이펙트 `Boom` → `FallingRocks`(15), 회전 보정 (0,0,0)(낙석 충돌 큐와 같음). 흔들림은 사용자가 에디터에서 바꾼 0.6·0.6 그대로.
- **크기는 아직 못 줄임:** 이펙트 큐에 크기 칸이 없다. 두 프리팹(`Render/Effect/AttackEffect/Boom/BOOM.prefab`, `Render/Effect/EventEffect/FallingRocks/FallingRocks.prefab`)의 파티클은 모두 크기 방식 Local(1)이라 부모를 줄여도 작아지지 않는다(모든 Transform 배율 1). 방법은 사용자 결정 대기.

### 4.5 작은 이펙트 복사본 (2026-11-11 사용자 "작은 프리팹 (B)", 컴파일·Unity 미확인)

- 새 프리팹 `BOOM_Small.prefab`·`FallingRocks_Small.prefab`(원본 복사, 루트 0.5배, 파티클 크기 방식 Local → Hierarchy — 원본은 그대로), `EffectType.BoomSmall = 18`·`FallingRocksSmall = 19`, Render 이펙트 표 항목 둘·표 두 줄.
- `CombatAttack_Hammer` 이펙트 `Boom` → `BoomSmall`, `TraitVisual_CrashImpact` 부딪힘 이펙트 `FallingRocks` → `FallingRocksSmall`.
- Render 쪽은 민형님 영역이라 사용자 지시로 고치고 민형님 Factory 공유 §56에 적음(미전달).
- Unity 확인: 해머 공격 맞는 순간 작은 Boom(위치는 몸 앞 0.8, 망치 쪽이 아니면 보정값 조정), 맵 끝·벽·구조물·유닛 부딪힘에 작은 낙석, 다른 곳의 Boom·낙석(화약통·지뢰·자폭·낙석 기믹·스테이지 맵 노드 떨어짐)은 원래 크기 그대로. Hierarchy 방식은 부모(필드 루트) 배율도 따르므로 필드 루트가 1배가 아니면 크기가 다를 수 있음.
