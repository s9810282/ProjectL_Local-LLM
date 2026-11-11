# 전투 View — 적 전용 특성 재구성 연출 설계 (초안)

- 작성일: 2026-11-11
- 상태: **결정 완료(2026-11-11 사용자: Q-RE4 "직접 구현", 나머지 권장 A)**, 7기준 보완 §9, Task 계획 [reconstruction-view-plan](../plans/2026-11-11-combat-reconstruction-view-plan.md), 코드 착수 전
- 요청: 사용자 "재구성 작업 확인하자" → 확인 결과(채팅) → "3번의 경우 EnemyRecover.Prefab 추가했고 이걸로 모습 변경. 4번의 경우 Core 작업 범위 측정 후, 직접할지 Core에 전달할지 결정. 나머지는 권장안으로 설계"
- Core 원본: [Core 전달 문서 §67](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)(적 전용 특성 재구성·자폭·공포 View 인계), 천명님 설계 [enemy-death-traits-design](2026-11-10-enemy-death-traits-design.md)

## 1. 지금 사실 (2026-11-11 코드 대조)

### 1.1 Core가 내는 것

| 때 | 사건 순서 | 사실 |
|---|---|---|
| 치명타(공격·즉사·낙석) | 발동 `ReconstructionStarted`(370, 크기 = 잃은 체력) → `CombatCharacterEvent` `Reconstructed`(49) | 사건 전 사실 = 맞기 전, 사건 뒤 사실 = **체력 최대**·`ReconstructionStatus`(남은 자기 진영 턴 시작 2)·사용함, Lv2부터 보호막(최대 체력 50%). 사망 사건이 없다(`CombatDamageApplication.cs:173-183`·`:300-309`, `RockfallGimmickRule.cs:115-116`). 낙석은 발동이 모든 수신자 사건 뒤 |
| 자기 진영 턴 시작 | 남은 수 2 → 1(상태 사건만) → 부활: 발동 `ReconstructionEnded`(380) → 상태 사건(재구성 중 해제) → Lv3이면 주변 8칸 밀기(`KnockbackApplied` + 강제 `Moved`) | `ReconstructionTurnApplication.cs:20-46` |

- 재구성 중에는 이동·공격을 못 하고(거절 사유 52), 피해·강제 이동은 받는다. 다시 치명타를 받으면 이번에는 죽는다(사용함).
- 공개 공격 미리보기도 재구성을 반영한다. 대상 예상 체력 `TargetHealthAfter` = 최대 체력이다(§67.4).

### 1.2 View가 지금 하는 것

| 항목 | 지금 | 근거 |
|---|---|---|
| 치명타 시점 | 공격 출처의 후속으로 잡혀 **맞는 순간(그 대상 해제)까지 기다림** — 발동 370도 같은 피해 근원이라 같이 기다림. 낙석은 근원이 없어 차례대로 | `CombatHitFollowUps.Begin`·`CombatHitGate.CanBegin` 규칙 2 |
| 치명타 연출 | 캐릭터 처리기의 셋 갈래(토네이도·사망·피격) 모두 아님 → **사실만 반영**: 피격 반응·피격 큐(이펙트·흔들림·소리)·플래시·피해 숫자 없음, 체력 바가 최대로 **차오름**, "재구성 중" 아이콘 생김 | `CombatCharacterEventHandler.TryBeginHit`(`Kind != HealthChanged`면 false), `CombatVolleyDamageNumbers.Loss` |
| 재구성 중 모습 | 원래 모델이 서 있음 | 모습을 바꾸는 칸 없음 |
| 발동 370·380 이펙트·글자 | 재구성 특성 SO의 발동 줄로 됨(지금 줄 없음) | 특성 처리기 |
| 재구성 중 유지 이펙트 | `UnitStatusVisual_Reconstructing.whileEffect`로 됨(지금 비어 있음) | 상태 유지 이펙트 출처 |
| HUD 아이콘·설명 | 됨("재구성 중", 남은 자기 턴 시작 수) | `CombatUnitStatusFactory` 표시 변환(11-10 추가) |
| Lv2 보호막·Lv3 밀기 | 기존 보호막 상태 외형·넉백 연출로 됨 | — |
| 거절 사유 52 | 기본 문구 "사용불가"(예외 없음, 적 전용이라 플레이어가 거의 못 봄) | `CombatActionStateText`·`CombatActionNotice` 기본 갈래 |
| 공격 미리보기 | 예상 체력 = 최대 → UnitHUD가 현재 체력으로 잘라 **"피해 없음"처럼 보임**. 미리보기 대상에 재구성 여부 칸 없음 | `UnitHUD.ApplyHealthPreview`(`Clamp(preview, 0, current)`), `AttackTargetPreview` |

### 1.3 `EnemyRecover.prefab`

- 민형님 `2c5cf9a07`(11-11)이 넣은 `Prefabs/Characters/New/Enemy/EnemyRecover.prefab` = `Resources/Characters/New/Enemy/EnemyRecover.fbx` 변형 하나(위치 보정 x −0.77, y축 180° 회전). Animator·몸 상자·`FieldUnitView`·이벤트 컴포넌트 없음. FBX 가져오기는 Generic, 클립 없음.
- 유닛 표시는 외형 프리팹 전체가 인스턴스 하나이고 뿌리에 `FieldUnitView`가 붙는다(`FieldUnitPresenter.CreateView`). 몸 상자(BoxCollider)·HUD 기준점은 그 프리팹이 갖는다.
- 모델 렌더러를 다루는 부품: 외형 층 `UnitModelAppearance`(이동 투명도·피격 플래시·윤곽, 준비 때 렌더러를 잡음 — `FieldUnitView.PrepareMovementAppearance`), 래그돌 `CombatUnitRagdoll`(원래 뼈대·Animator·SkinnedMeshRenderer), 모델 경계 `TryGetModelBounds`(켜진 렌더러만). Cartoon 재질 교체(`SetCartoonMaterialFallback`)가 "외형 층을 먼저 놓고 바꾼 뒤 다시 잡는" 선례다.

## 2. 결정(사용자, 2026-11-11)

| 번호 | 내용 | 결정 |
|---|---|---|
| ① | 치명타 순간 피격 반응 | 권장안: `Reconstructed`도 피격으로 재생, 피해 숫자 = 맞기 전 체력 |
| ② | 체력 바 | 권장안 (b): 피격 반응이 끝난 뒤 사건 뒤 사실(체력 최대)을 반영 |
| ③ | 재구성 중 모습 | **`EnemyRecover.prefab`으로 모습 변경** |
| ④ | 미리보기 | Core 작업 범위를 잰 뒤 직접/전달을 정함(§5, Q-RE4) |

## 3. 설계

### RE1 치명타 = 피격 연출 + 늦은 사실 반영 (①·②)

- 캐릭터 처리기 피격 갈래 조건에 `Reconstructed`를 더한다. 조건은 "체력이 줄었다"가 아니라 "피격으로 보이는 사건"이므로, 판정을 함수 하나(`IsHitLike(e)` 가칭: `HealthChanged`이고 체력이 줄었음 **또는** `Reconstructed`)로 둔다.
- 조립은 지금 피격 조립(`ComposeHit`: 반응 + 피격 큐 + 플래시 + 피격 클립)을 그대로 쓴다. 다른 점은 사실 반영 시점 하나다.
  - 일반 피격: 반영 → 그리기 → 반응 시작(지금 그대로).
  - 재구성: 반응 시작 → **반응 끝(조립 완료)에서** 사건 뒤 사실 반영 → 그리기 → 완료 알림. 반응 동안 HUD는 맞기 전 체력이다. 끝에서 체력 최대·"재구성 중" 아이콘·보호막·모습 변경(RE2)이 한 프레임에 바뀐다.
  - 재생이 취소되면 반영하지 않는다. 결과 끝·정리의 최종 사실이 맞춘다(낭떠러지 추락 `BeginFallDeath`와 같은 규칙).
- 같은 주체의 뒤 사건은 주체 막기 규칙(`CombatSubjectBlocking`)으로 이 실행이 끝날 때까지 기다리므로, 뒤 사건은 늘 반영된 사실 위에서 시작한다.
- 피해 숫자: `CombatVolleyDamageNumbers.Loss`가 `Reconstructed`를 받고, 잃은 양 = **사건 전 사실의 현재 체력**(사건 뒤는 최대라 차가 음수). 발동 370의 크기와 같은 값이지만 사건 하나 안에서 읽는다. 탄별 나눔(`CanSplit`)은 지금 규칙(수치 피해 원인)을 그대로 따른다.
- 맞은 방향·피격 이펙트 자리는 지금 피격과 같은 함수(`CombatHitDirection`, `SubjectBodyOf`)다.

### RE2 상태가 정하는 모습 바꾸기 (③)

- **원본:** 상태 외형 표 `UnitStatusVisual`에 새 칸 `whileAppearance`(프리팹, 비우면 모습 그대로). `UnitStatusVisual_Reconstructing`에 `EnemyRecover.prefab`을 연결한다(Q-RE1 A). 상태 유지 이펙트 `whileEffect`와 같은 줄 — "이 상태인 동안 무엇이 보이나"를 상태 외형 한 곳이 정한다.
- **판단:** 보인 사실 그리기(`FieldUnitPresenter.RenderFacts`)가 유닛마다 "지금 상태 중 모습을 정하는 첫 상태"를 묻는다. 묻는 곳은 상태 줄 표 `CombatUnitStatuses`(HUD와 같은 표시 규칙 `IsShown`·같은 순서) 한 곳이다. 둘 이상이면 HUD 순서의 첫 상태(지금은 재구성 중 하나뿐).
- **적용:** 새 부품 `Display/Unit/UnitStatusAppearance`(가칭, `FieldUnitView`가 가짐 — `FieldUnitView`는 이미 922줄이라 갈래를 늘리지 않고 부품에 맡김):
  - 바꿀 때: 외형 층을 놓고(`ReleaseMovementAppearance`) → 원래 모델 렌더러를 끄고(enabled = false, 뼈대·Animator·클립 채널은 그대로) → 프리팹 인스턴스를 유닛 뿌리의 자식으로 켬(처음이면 만듦, 프리팹마다 하나 보관) → 외형 층을 다시 잡음. 그래서 재구성 중에 다시 맞으면 플래시가 잔해에 나온다.
  - 되돌릴 때: 같은 순서로 반대.
  - 같은 프리팹이면 아무것도 안 함(매 그리기에서 비교 한 번).
- 몸 상자·HUD 기준점·자리·회전·크기는 원래 유닛 것을 그대로 쓴다(잔해는 뿌리의 자식이라 넉백·밀림·피격 자세를 따라감). 프리팹 안의 위치·회전 보정(x −0.77, 180°)은 프리팹 값 그대로 쓴다.
- 풀 반환(`ResetForPool`)·`Configure`·해제는 원래 모습으로 되돌리고 잔해 인스턴스는 보관(풀 재사용). 표시 해제 때 지운다.
- **부활:** 상태 사건(재구성 중 해제)의 사실 반영 = 그리기에서 원래 모습으로 돌아간다. 바로 앞의 발동 380 줄 이펙트가 그 순간을 덮는다(Q-RE5 A).
- **재구성 중 사망(두 번째 치명타):** 사망 반응을 시작할 때 모습이 바뀌어 있으면 래그돌을 쓰지 않는다(`launchRagdoll = false`, 지금 공격 아닌 사망과 같은 갈래). 잔해가 그대로 사망 반응을 받고 사라진다(Q-RE2 A). 원래 모델은 꺼져 있어 래그돌이 보이지 않기 때문이다.

### RE3 공격 미리보기 (④)

- 미리보기 대상마다 Core가 "이 공격으로 재구성된다" 칸을 준다(§5). View는 그 대상의 HUD 예상 체력을 **0**으로 보인다(치명 피해 예상, Q-RE3 A). 다른 대상은 지금 그대로 `TargetHealthAfter`.
- 잘라 보이는 UnitHUD 규칙(`Clamp(preview, 0, current)`)은 그대로라 UnitHUD(현강님) 무변경.
- 미리보기 판정(어느 유닛이 재구성되나)을 View가 체력 수로 짐작하지 않는다. "피해만으로는 0인데 예상 체력이 남는다"는 위스키 같은 맞은 뒤 회복과 구분할 수 없다.

### RE4 데이터로 넣을 것 (코드 없음, 사용자·아트)

- 재구성 특성 SO(`TraitVisual_Reconstruction`, 키 490) 발동 줄: 370(쓰러지는 순간 — 피격과 같은 순간), 380(부활 순간). 이펙트 종류·자리는 아트가 정함(예: 370 `SubjectBody`, 380 `ActorBody`).
- `UnitStatusVisual_Reconstructing.whileEffect`(선택): 잔해 위에 붙는 이펙트.
- 아이콘.

## 4. 범위 밖

- 거절 사유 52 전용 문구("재구성중")는 넣지 않는다(적 전용, 플레이어가 적을 조작하지 않음). 필요해지면 한 줄.
- 잔해가 바뀌는 순간의 전환 연출(먼지·서서히 사라짐 등)은 이펙트 줄로 덮는다. 모델 페이드는 만들지 않는다.
- 재구성 중 유닛 호버 공격 범위: Core 조회가 범위를 주면 지금처럼 보인다(공격 못 하는 동안에도). 바꾸지 않는다.

## 5. 미리보기 Core 작업 범위 측정 (④)

- 지금: `AttackPreview`에 발동 목록이 없고, `AttackTargetPreview`·`AttackHitResult`에 재구성 칸이 없다. 예상 체력 = 맞기 전 + 회복(재구성 = 최대만큼) − 피해.
- 필요한 Core 변경(추가만):

| 파일 | 변경 | 규모 |
|---|---|---|
| `Application/Combat/CommandAttackLedger.cs` | 범위 시작(`BeginScope`) 때 재구성 가능 유닛을 기억하고, 피격 체력 포착(`CaptureHitHealth`) 때 "이번에 재구성 사용함으로 바뀐 유닛" 집합을 만든다 | 약 10줄 |
| `Contracts/Results/AttackHitResult.cs` | 속성 `TargetReconstructed` + 생성자 끝 선택 인자 `reconstructed = false` | 약 5줄 |
| `Application/Combat/AttackResultFactory.cs` | `CreateHit`이 장부 값을 넘김 | 1줄 |
| `Contracts/Results/AttackTargetPreview.cs` | 속성 `TargetReconstructs`, 결과에서 만들 때 복사, 다른 생성자 끝 선택 인자 | 약 8줄 |

- 기존 호출: `new AttackHitResult(` 23곳·`new AttackTargetPreview(` 13곳(대부분 시험) — 선택 인자라 **고칠 곳 0**. 결과 검사 `CombatChangeEvidenceReader`는 새 칸을 보지 않아도 된다(검사 대상 아님).
- 판정 원본은 Core 하나(재구성 규칙 `ReconstructionTraitRule.CanTrigger`·사용함 표시). View는 칸만 읽는다.
- **권장(Q-RE4):** A 이 작업에서 직접(사용자 지시로 Core 수정, Core 전달 문서 §5에 기록·천명님 공유) — 추가만이고 기존 호출·시험이 그대로라 위험이 작다. B 천명님께 요청(Core 전달 문서에 요청 절) — RE3만 Core 답 뒤로 미루고 RE1·RE2는 먼저 한다.

## 6. 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-RE1 | 모습 프리팹을 어디에 두나 | **A 상태 외형 `UnitStatusVisual` 새 칸**(상태가 모습을 정함, `whileEffect`와 같은 줄, 모든 재구성 유닛이 같은 잔해) / B 유닛 외형 `UnitVisual`에 "재구성 모습" 칸(유닛마다 다른 잔해) / C 재구성 특성 외형 |
| Q-RE2 | 재구성 중 다시 죽으면 | **A 잔해 그대로 사망 반응(래그돌 끔)** / B 원래 모델로 되돌린 뒤 래그돌 |
| Q-RE3 | 재구성될 대상의 미리보기 | **A 예상 체력 0(치명 예상)** / B 지금처럼(피해 없음처럼 보임) |
| Q-RE4 | 미리보기 Core 칸 | A 이 작업에서 직접(§5) / B 천명님께 요청 — 사용자 결정 |
| Q-RE5 | 부활 순간 | **A 상태가 풀리는 사실 반영 순간 바로 원래 모습(380 이펙트로 덮음)** / B 짧은 전환 연출을 코드로 |

## 7. 원칙 점검(7기준, 예상)

- **SSOT:** 모습 판단 원본 = 상태 외형 표 한 곳 + 보인 사실(상태). 재구성 판정은 Core 하나, View는 사건 종류·사실·Core 칸만 읽음. 피해 숫자는 사건 안의 값.
- **SOLID/SRP:** 모습 바꾸기는 새 부품 하나(`FieldUnitView`는 들고만 있음), 처리기는 판정 함수 하나와 반영 시점 갈래 하나.
- **FW 계약:** Factory·서비스 키 무변경 예상(상태 줄 표는 이미 `Configure`로 들어옴). DB는 SO 칸 하나 + 에셋.
- **독립성:** 다른 상태에 모습을 줄 때 에셋 칸만 채우면 됨. 재구성을 빼도 다른 경로에 영향 없음.
- **불필요/우회:** 사실 반영을 늦추는 것은 낙석 추락의 선례와 같은 꼴.
- **C#/Unity:** 인스턴스는 처음 한 번 만들고 보관(전투 중 할당은 첫 재구성 때 한 번). 미리 만들기는 계획에서 정함.
- **매 프레임:** 그리기마다 프리팹 비교 한 번, 그 밖에 매 프레임 일 없음.
- 예상: 지금 구조 점수에 감점 없이 기능 추가(알려진 문제 기준).

## 8. Phase(안)

| Phase | 내용 | 확인 |
|---|---|---|
| RE-a | RE1 피격 연출·늦은 사실 반영·피해 숫자 | 재구성 적을 쓰러뜨리는 공격·낙석 |
| RE-b | RE2 상태 외형 칸·모습 부품·사망 래그돌 규칙·에셋 연결 | 쓰러짐 → 잔해 → 부활, 재구성 중 피격·밀림·사망 |
| RE-c | RE3 미리보기(Q-RE4 A면 Core 칸 + View, B면 요청 문서만) | 재구성될 적 호버 |
| RE-d | 문서·공유·묶음 끝 검토 | — |

- 시험: View 작업이라 새 시험 없음(CLAUDE.md). 컴파일·재생은 사용자 Unity.
- 공유: Core 칸(Q-RE4 A)은 Core 전달 문서 §5 + 천명님, 상태 외형 표 칸은 호진님 메모(상태 외형 표 넣는 법), UnitHUD는 무변경(현강님 없음), Factory는 바뀌면 민형님.
- 착수 전 확인: 이 세션에서 바꾼 파일(재구성 SO·상태 SO·`CombatCuePoints` 등)이 아직 커밋 전이다. 같은 `UnitStatusVisual` 코드·에셋을 고치므로 먼저 커밋하면 묶음이 깨끗하다.

## 9. 결정과 7기준 보완 (2026-11-11)

### 9.1 결정

- Q-RE1 A(상태 외형 새 칸) · Q-RE2 A(재구성 중 사망은 잔해 그대로, 래그돌 끔) · Q-RE3 A(재구성 대상 예상 체력 0) · **Q-RE4 A(이 작업에서 Core 칸을 직접 구현 — 사용자 명시 지시, Core 전달 문서 §5에 기록)** · Q-RE5 A(상태가 풀리는 순간 원래 모습).

### 9.2 7기준으로 다시 본 것과 보완

| 기준 | 초안에서 걸린 곳 | 보완 |
|---|---|---|
| SSOT | "재구성 사건은 피격으로 보인다·잃은 양 = 사건 전 체력·사실은 반응 끝에 반영"을 처리기와 피해 숫자가 따로 알게 됨 | 규칙 하나 `Playback/Support/CombatCharacterHits`(정적)에 셋(피격으로 보이나·잃은 체력·늦은 반영)을 두고 두 곳이 부름 |
| SSOT | 미리보기 "재구성되나"를 Core 장부가 직접 비교하면 재구성 규칙 사본이 생김 | 판정은 Core 재구성 규칙 파일의 함수 하나(`ReconstructionTraitRule.Triggered(before, after)`)가 정하고 장부는 부르기만 |
| SOLID(SRP) | `FieldUnitView`(922줄)에 모습 바꾸기 상태·렌더러 목록이 들어감 | 새 부품 `Display/Unit/UnitStatusAppearance`가 잔해 인스턴스·원래 렌더러·"마지막으로 본 사실"을 가짐. `FieldUnitView`는 외형 층 놓기/다시 잡기 순서만(그 층의 주인이라서) |
| SOLID(ISP) | 사망 갈래가 "모습이 바뀌었나"를 물을 곳 | 표시 계약 `ICombatUnitPresentationTarget`에 읽기 하나(`ShowsReplacedAppearance`), 창구가 유닛 ID로 묻는 입구 하나. 구현은 `FieldUnitView` 하나라 고칠 가짜 없음 |
| FW 계약 | — | Factory·서비스 키·Builder 무변경. DB는 상태 외형 SO 칸 하나(Data 생성자 끝 선택 인자) + 에셋 |
| 독립성 | 다른 상태에 모습을 줄 때 | 에셋 칸만. 재구성 코드를 빼도 피격·사망·HUD 경로는 그대로 |
| 캡슐화 | 원래 렌더러를 끄는 일이 밖으로 새면 래그돌·외형 층과 부딪침 | 렌더러 켜짐은 부품만 바꾸고(다른 코드는 켜짐을 건드리지 않음 — 보이기는 GameObject, 래그돌은 Animator·Collider), 바깥에는 bool 하나만 보임 |
| C#/Unity | 잔해를 매번 만들고 지우면 할당 | 프리팹마다 인스턴스 하나를 유닛 뿌리 자식으로 보관, 끄고 켜기만. 유닛 표시가 지워질 때 함께 지워짐. 몸 상자는 외형 프리팹 에셋 뿌리에서 읽어(`CombatBodyShapeCache`) 잔해와 무관, FBX 충돌체 없음 |
| If/Tick | 그리기마다 상태 읽기(`StatusEffectReadModel.Capture`는 목록을 새로 만듦)를 부르면 사건마다 모든 유닛에서 할당 | 부품이 사실 객체 참조를 기억해, **사실이 바뀐 유닛만** 모습을 판단(HUD 사실 기억 GC-c와 같은 꼴). Tick 없음 |

- 남는 작은 차이(받아들임): 모습을 바꾸는 순간 외형 층을 다시 잡으므로, 그 유닛에 걸려 있던 윤곽 대여가 끝난다(Cartoon 재질 교체와 같은 성질). 바꾸는 때는 재생 중 사실 반영이라 호버 윤곽이 걸려 있는 일은 드물다.
- 잔해에는 Cartoon 재질 교체가 걸리지 않는다(교체는 처음 잡은 렌더러만). 잔해 재질은 프리팹이 정한다.

## 10. 구현 메모 (2026-11-11, RE-a~RE-c 코드 완료, 컴파일 미확인)

- 계획과 다른 점
  - **사망을 붙잡은 동안은 모습을 맞추지 않는다**(계획에 없던 것). 사망 갈래는 반응을 시작한 직후 사망 사실을 반영하는데, 그 사실에 재구성 상태가 지워져 있으면 잔해가 반응 도중 원래 모델로 돌아간다. `FieldUnitPresenter.RenderFacts`가 `HoldsDeath`인 유닛은 건너뛴다(Q-RE2 A를 지키는 조건).
  - 잔해 인스턴스는 유닛 뿌리 레이어를 물려받는다(유닛 프리팹은 Entity(6), `EnemyRecover`는 Default).
  - 다른 개체로 묶일 때(`Bind`)도 모습을 되돌린다(풀 반환과 같은 정리).
  - 외형 층이 준비되지 않은 표시(색 서비스 없음)는 층을 놓고 잡지 않고 모델만 바꾼다.
  - `AttackTargetPreview`는 readonly struct라 큰 생성자에 `TargetReconstructs = false` 대입을 넣었다(C# 9 모든 칸 대입).
  - 에셋의 프리팹 참조 번호는 FBX 변형 프리팹 뿌리 번호 규칙(PrefabInstance 번호 XOR 원본 뿌리 번호)으로 계산했다 — 이미 있는 변형 참조 넷과 맞춰 확인.
- 새 파일: `Playback/Support/CombatCharacterHits.cs`, `Display/Unit/UnitStatusAppearance.cs`(+ `.meta` 각각).
- 시험·Factory·서비스 키 무변경. Core 변경 기록은 Core 전달 문서 §5.85.
- **묶음 끝 검토 반영(2026-11-11 사용자 "권장대로 반영", 컴파일 미확인, 계획 §4.2):**
  - M1: RE2의 "공격 아닌 사망 = 래그돌 없음" 전제가 틀렸다(`launchRagdoll`은 밀기 방향만 정하고 래그돌은 컴포넌트가 있으면 시작). 래그돌 판단을 래그돌을 쥔 자세 대여(`FieldUnitView.PresentationUnitPose.TryBeginDeathRagdoll`) 한 곳으로 옮겨, 모습이 바뀌었으면 false → 사망 반응이 몸 전체를 기울여 잔해가 쓰러진다. 그래서 표시 계약 읽기·창구 입구·처리기 조건(계획 TP-9)은 걷었다.
  - M2: 넉백·작렬 부딪힘 보조 행 미리보기도 칸을 받는다(`AttackTargetPreview` 큰 생성자 끝 선택 인자 `targetReconstructs`, 계획 TP-11 바뀜).
  - L1: 표시 위치 보존의 체력류 사건 판정을 `CombatCharacterHits.ChangesHealthOnly` 하나로(재구성 포함).
  - L4: 잔해를 만들다 실패하면 원래 모습 그대로(실패할 수 있는 일을 상태를 바꾸기 전에).
  - L5: 공격 장부는 범위 시작에 입력 목록 참조만 쥐고, 피격 체력 고정 때 고정한 대상만 판정한다(재구성이 있을 때만 집합 생성).
  - L7: `IsHitLike`의 재구성 사건 사망 검사를 걷고 Core 생성자 보장을 주석으로.
