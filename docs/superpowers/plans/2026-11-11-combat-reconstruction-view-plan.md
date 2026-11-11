# 전투 View — 적 전용 특성 재구성 연출 Task 계획

- 작성일: 2026-11-11
- 설계: [reconstruction-view-design](../specs/2026-11-11-combat-reconstruction-view-design.md)(RE1~RE4, 결정·7기준 보완 §9)
- 결정: Q-RE1 A · Q-RE2 A · Q-RE3 A · **Q-RE4 A(Core 칸 직접 구현, 사용자 명시 지시)** · Q-RE5 A
- 상태: **코드 착수 전.** 계획에서 정한 것 TP-1~TP-12는 사용자 확인 대상
- 진행 규칙: Phase마다 멈추고 보고(리뷰 지점). View 작업이라 **새 시험을 쓰지 않고 MSBuild도 돌리지 않는다**(CLAUDE.md). 컴파일·재생은 사용자가 Unity에서 하고, 보고에는 "컴파일 미확인"을 적는다. 묶음 끝에 검토 에이전트 하나.
- 원칙 점검: 각 Phase 끝에 7기준(SSOT·SOLID·JMH FW 계약·독립성·캡슐화·C#/Unity 권장·불필요한 If/Tick)을 표로 확인한다(§3).

## 0. 착수 전

1. 이 세션 앞부분의 변경(신규 특성 SO 여섯·상태 SO 다섯·표시 변환·`SelectedCell`·자폭/공포 줄·폭주 문구)이 커밋 전이다. RE-b가 같은 상태 외형 코드·에셋을 고치므로 **먼저 커밋 권장**(사용자).
2. 다른 작업자 줄 확인: 바꿀 파일 중 다른 사람 줄이 있는 곳은 구현 때 `git blame`으로 다시 세어 공유 목록에 적는다. 지금 알려진 것: Core 넷(천명님 영역, Q-RE4 A로 이 작업에서 고침), `UnitStatusVisualDataSO`·`UnitStatusVisualData`(TA-e 우리 파일, 호진님은 에셋 담당으로 공유).

## 1. 계획에서 정한 것 (TP)

| 번호 | 내용 |
|---|---|
| TP-1 | 캐릭터 피격 규칙은 새 정적 클래스 `Playback/Support/CombatCharacterHits` 하나: `IsHitLike(e)`(`HealthChanged`이고 산 채로 체력이 줄었음, 또는 `Reconstructed`), `HealthLoss(e)`(`Reconstructed`면 사건 전 현재 체력, 아니면 사건 전 − 사건 뒤, 0 이상), `AppliesFactsAfterReaction(e)`(`Reconstructed`만). 처리기 피격 갈래와 피해 숫자가 이것만 부른다 |
| TP-2 | 늦은 반영은 낭떠러지 추락과 같은 꼴: 조각 실행의 완료 콜백에서 `ApplyCharacter(Final)` → `Render()` → 원래 완료. 실행이 취소되면 콜백이 오지 않아 반영하지 않는다(결과 끝 최종 사실이 맞춤) |
| TP-3 | 모습 판단은 `CombatUnitStatuses.TryGetAppearance(facts, out GameObject prefab)` 한 곳: 상태 읽기 순서(HUD와 같음) 중 `IsShown`이고 외형에 `WhileAppearance`가 있는 첫 상태 |
| TP-4 | 새 부품 `Display/Unit/UnitStatusAppearance`: 마지막으로 본 사실 객체 참조를 기억해 같으면 아무것도 안 함(판단 함수도 안 부름). 바뀌면 판단 → 지금 모습과 다를 때만 바꿈 |
| TP-5 | 바꿀 때: `FieldUnitView`가 외형 층을 놓고(`ReleaseMovementAppearance`) → 부품이 원래 렌더러 `enabled = false`·잔해 켬(프리팹마다 인스턴스 하나, 유닛 뿌리 자식, 처음이면 만듦) → 외형 층을 다시 잡음(`PrepareMovementAppearance`). Cartoon 재질 교체(`SetCartoonMaterialFallback`)와 같은 순서. 원래 렌더러 = 처음 바꿀 때 잡은 자식 렌더러(TMP·Canvas 아래와 잔해 아래 제외) |
| TP-6 | 되돌림: 원래 렌더러를 다시 켜고 잔해를 끔(지우지 않음). `ResetForPool`·`Configure`가 되돌리고 기억을 비움. 잔해는 유닛 표시가 지워질 때 함께 지워짐 |
| TP-7 | 미리 만들기 없음 — 첫 재구성 때 잔해 인스턴스 하나를 만든다(적 전용·드묾). 끊김이 보이면 그때 `CombatPrewarmPlan`에 재구성 보유 유닛 줄을 더함 |
| TP-8 | 잔해에는 Cartoon 재질 교체가 걸리지 않음(교체는 처음 잡은 렌더러만), 재질은 프리팹이 정함 |
| TP-9 | 재구성 중 사망 래그돌: 사망 갈래가 창구 `CombatUnitPresentationWindow.ShowsReplacedAppearance(id)`(표시 계약 새 읽기 `ICombatUnitPresentationTarget.ShowsReplacedAppearance`)를 물어 참이면 `launchRagdoll = false` |
| TP-10 | Core 판정 `ReconstructionTraitRule.Triggered(before, after)` = `CanTrigger(before)` ∧ 사건 뒤 재구성 상태 사용함 ∧ 살아 있음. 장부가 범위 시작(`BeginScope`)의 사실과 피격 체력 고정(`CaptureHitHealth`)의 사실로 부르고 집합에 담음 |
| TP-11 | Core 칸: `AttackHitResult.TargetReconstructed`(두 생성자 끝 선택 인자 `targetReconstructed = false`, 내용 생성자가 넘김), `AttackTargetPreview.TargetReconstructs`(결과에서 만드는 생성자가 복사, 다른 생성자는 늘 거짓 — 쓸 곳 없어 인자 안 더함). 기존 호출 36곳·시험 무변경 |
| TP-12 | View 미리보기 값 규칙은 `CombatUnitHudFeedback.ShowAttackHealthPreview` 한 곳: `TargetReconstructs`면 0, 아니면 `TargetHealthAfter`. UnitHUD 무변경 |

## 2. Phase와 Task

### RE-a 치명타 피격 연출·늦은 사실 반영·피해 숫자 (RE1)

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | 새 `ViewSystem/Combat/Playback/Support/CombatCharacterHits.cs`(+ `.meta`) | TP-1 규칙 셋. 문서 주석에 Core 사실(사건 뒤 = 체력 최대) 근거 |
| Ta.2 | `Playback/Handlers/CombatCharacterEventHandler.cs` | `TryBeginHit` 조건을 `IsHitLike`로. `AppliesFactsAfterReaction`이면 반응 전 반영을 건너뛰고 완료 콜백을 감쌈(TP-2). 처리기 머리 문서에 한 줄 |
| Ta.3 | `Playback/Support/CombatVolleyDamageNumbers.cs` | 캐릭터 `Loss`를 `CombatCharacterHits.HealthLoss`로(구조물은 그대로), 종류 필터에 `Reconstructed` |
| Ta.4 | 문서 | 폴더 배치 안내 `Playback/Support/` 줄 |

- Ua(사용자 Unity): ① 재구성 적을 공격으로 쓰러뜨림 → 맞는 순간 피격 반응·피격 이펙트·흔들림·플래시·피해 숫자(맞기 전 체력) ② 반응 동안 HUD 체력은 맞기 전, 반응 끝에 최대로 차오르고 "재구성 중" 아이콘 ③ 낙석으로 재구성 ④ 즉사(마무리 사격) 재구성 ⑤ 일반 피격·사망은 지금과 같음 ⑥ 연발 공격(탄별 숫자) 재구성 대상.

### RE-b 잔해 모습 (RE2)

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `DataBaseSystem/Data/View/Table/UnitStatus/Item/UnitStatusVisualDataSO.cs`·`UnitStatusVisualData.cs` | 칸 `whileAppearance`(GameObject, 툴팁: 보이는 동안 원래 모델 대신 보일 모델, 비우면 그대로, 몸 상자·HUD는 원래 유닛 것) → Data `WhileAppearance`(생성자 끝 선택 인자) |
| Tb.2 | `Display/Unit/CombatUnitStatuses.cs` | `TryGetAppearance`(TP-3) |
| Tb.3 | 새 `Display/Unit/UnitStatusAppearance.cs`(+ `.meta`) | TP-4~TP-6 부품: `Sync(facts, resolve, releaseLayers, prepareLayers)` 꼴 또는 `FieldUnitView`가 앞뒤를 감싸는 `TryChange` — 구현 때 둘 중 줄이 적은 쪽 |
| Tb.4 | `Display/Unit/FieldUnitView.cs`·`ICombatUnitPresentationTarget.cs`·`CombatUnitPresentationWindow.cs` | 부품 칸·`SyncStatusAppearance(facts, resolver)`·`ShowsReplacedAppearance`·`ResetForPool`·`Configure` 되돌림, 계약 읽기 하나, 창구 입구 하나 |
| Tb.5 | `Display/Unit/FieldUnitPresenter.cs` | `RenderFacts` 캐릭터 줄에서 `Apply` 뒤 한 줄(구조물은 안 부름) |
| Tb.6 | `Playback/Handlers/CombatCharacterEventHandler.cs` | 사망 갈래 `launchRagdoll`에 TP-9 |
| Tb.7 | 에셋 `DataBaseSO/ViewGroup/UnitStatusVisual/Item/UnitStatusVisual_Reconstructing.asset` | `whileAppearance` = `Prefabs/Characters/New/Enemy/EnemyRecover.prefab`(코드와 한 커밋) |

- Ub: ① 재구성 → 반응 끝에 잔해로 바뀜(원래 모델 안 보임, HUD·체력 바 그대로 따라옴) ② 재구성 중 피격 → 잔해에 플래시·피격 반응 ③ 재구성 중 넉백·밀림 → 잔해가 따라감 ④ 자기 턴 시작 두 번째에 부활 → 원래 모델(발동 380 줄이 있으면 그 이펙트) ⑤ 재구성 중 다시 치명타 → 잔해가 래그돌 없이 사망 반응 후 사라짐 ⑥ 재구성 중 낭떠러지 → 잔해가 가라앉음 ⑦ 다음 전투에서 같은 적 키의 풀 표시가 원래 모습 ⑧ Lv3 부활 밀기.

### RE-c 미리보기 (RE3, Core 직접)

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | Core `Domain/Traits/Rules/ReconstructionTraitRule.cs` | `Triggered(before, after)`(TP-10) |
| Tc.2 | Core `Application/Combat/CommandAttackLedger.cs` | 범위 시작에 재구성 가능 유닛의 사실 기억, `CaptureHitHealth`에서 `Triggered` 집합, 읽기 `Reconstructed(id)` |
| Tc.3 | Core `Contracts/Results/AttackHitResult.cs`·`AttackTargetPreview.cs`, `Application/Combat/AttackResultFactory.cs` | TP-11 |
| Tc.4 | `Display/Unit/CombatUnitHudFeedback.cs` | TP-12 |
| Tc.5 | `docs/.../2026-09-24-view-core-data-boundary-handoff.md` | §5.85 기록(바꾼 줄·이유·시험 무변경·천명님 확인 부탁) + **파일 제목 갱신**(CLAUDE.md 규칙) |

- Uc: ① 재구성 적에 치명 공격 호버 → 예상 체력 0으로 깜빡임 ② 이미 재구성을 쓴 적(재구성 중) → 실제 예상 체력 ③ 일반 적·위스키 회복 대상은 지금과 같음 ④ 실제 공격 결과·재생이 바뀌지 않음.

### RE-d 문서·공유·묶음 끝 검토

- 설계 §10 구현 메모(계획과 다른 점), 폴더 배치 안내(새 파일 둘), CLAUDE.md 현재 상태.
- 공유(미전달): 천명님 = Core 전달 문서 §5.85, 호진님 = 특성 발동 공유 메모(`handoffs/2026-10-29-combat-trait-activation-hojin-share.md`)에 상태 외형 새 칸 절, 민형님 = Factory 무변경이면 없음, 현강님 = UnitHUD 무변경이면 없음.
- 묶음 끝 검토 에이전트 하나(7기준).
- 데이터로 넣을 것(사용자·아트): 재구성 특성 SO 발동 줄 370·380, `whileEffect`(선택), 아이콘.

## 3. 7기준 점검 (계획 그대로일 때 예상)

| 기준 | RE-a | RE-b | RE-c |
|---|---|---|---|
| SSOT | 재구성 피격 지식 = `CombatCharacterHits` 한 곳 | 모습 원본 = 상태 외형 표, 판단 = `TryGetAppearance` 한 곳, 보인 사실이 입력 | 재구성 판정 = Core 규칙 함수 하나, View 값 규칙 한 곳 |
| SOLID | 처리기 갈래 늘지 않음(조건 함수·반영 시점 한 줄) | 부품 하나가 모습 책임, 표시는 층 순서만, 계약 읽기 하나 | 장부 집합·결과 칸 추가만(OCP) |
| FW 계약 | 무변경 | DB 칸(Data 끝 선택 인자) + 에셋, Factory·키 무변경 | Core 공개 계약에 읽기 칸 둘(선택 인자) |
| 독립성 | 다른 사건 종류 무영향 | 다른 상태는 에셋만 | 기존 호출·시험 무변경 |
| 캡슐화 | 규칙 정적 internal | 렌더러·잔해 private, bool 하나 공개 | 장부 집합 private, 읽기 함수 |
| C#/Unity | 할당 없음 | 인스턴스 하나 보관·켜고 끄기, 몸 상자 무관 | 집합 하나(장부 수명) |
| If/Tick | 조건 셋을 함수 셋으로 | 사실 객체가 같으면 판단 안 함, Tick 없음 | 피격 체력 고정 때 한 번 |

## 4. 실행 기록

(착수 뒤 Phase마다 적는다.)

### 4.1 RE-a~RE-d 실행 (2026-11-11 사용자 "RE-d 까지 진행" = TP-1~TP-12 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 Ua·Ub·Uc 대기)

- RE-a: 새 `Playback/Support/CombatCharacterHits.cs`(+ `.meta`), 처리기 `TryBeginHit`(조건 `IsHitLike`, 재구성은 완료 콜백에서 사실 반영)·머리 문서 한 줄, 피해 숫자 `Loss`(캐릭터는 규칙 함수).
- RE-b: 상태 외형 칸 `whileAppearance`(SO·Data 끝 선택 인자), `CombatUnitStatuses.TryGetAppearance`, 새 `Display/Unit/UnitStatusAppearance.cs`(+ `.meta`), `FieldUnitView`(`SyncStatusAppearance`·`ForgetStatusAppearance`·되돌림·계약 읽기), 계약 `ICombatUnitPresentationTarget.ShowsReplacedAppearance`, 창구 `ShowsReplacedAppearance(id)`, `FieldUnitPresenter`(대리자 한 번·`Configure` 때 기억 비움·그리기 한 줄, 사망 붙잡은 동안 건너뜀), 사망 갈래 래그돌 조건, 에셋 `UnitStatusVisual_Reconstructing` → `EnemyRecover.prefab`.
- RE-c: Core 다섯 파일(`ReconstructionTraitRule.Triggered`·장부·`AttackHitResult`·`AttackResultFactory`·`AttackTargetPreview`), View `CombatUnitHudFeedback.ShownHealthOf`, Core 전달 문서 §5.85·제목.
- RE-d: 폴더 배치 안내 갱신 줄, 호진님 공유 메모 §4(`handoffs/2026-10-29-combat-trait-activation-hojin-share.md`, 미전달), 설계 §10 구현 메모. 민형님(Factory 무변경)·현강님(UnitHUD 무변경) 공유 없음.
- 계획과 다른 점: 설계 §10.
- 커밋 묶음: 새 `.cs` 둘(+ `.meta`), View 코드 열, DB 코드 둘 + 에셋 하나(한 커밋), Core 다섯, 문서(설계·계획·Core 전달 문서·폴더 안내·호진님 메모·CLAUDE.md). 이 세션 앞부분의 SO·상태 파일 변경과 섞여 있다.

### 4.2 묶음 끝 검토 (2026-11-11, 새 에이전트 하나, 읽기 전용, 약 8.4)

- 컴파일 오류 없음(생성자 오버로드·struct 대입·별칭·계약 구현 대조), 에셋 참조 번호 맞음, 늦은 반영은 추락과 같은 꼴로 취소·세대·주체 막기 모두 맞음, Core 판정·구조물 거짓·체력 검사 맞음.
- **M1(중간, 코드로 다시 확인함):** 재구성 중 사망의 래그돌이 꺼지지 않는다. `launchRagdoll`은 밀기 방향만 0으로 만들고 `PresentationUnitPose.TryBeginDeathRagdoll`(`FieldUnitView.cs:716-728`)은 `CombatUnitRagdoll`이 있으면 늘 시작한다 → 꺼진 원래 뼈대만 무너지고 잔해는 선 채로 사라짐(Q-RE2 A와 다름). 설계 RE2의 전제 "공격 아닌 사망 = 래그돌 없음"이 틀렸다. 권장: 래그돌 주인(`TryBeginDeathRagdoll`)이 모습이 바뀌었으면 false — 창구 입구·계약 읽기·처리기 조건은 걷음.
- **M2(중간, 확인함):** 넉백·작렬 부딪힘 보조 행 미리보기(`AttackResultFactory.cs:191`)는 큰 생성자라 `TargetReconstructs`가 늘 거짓 → 그런 대상은 여전히 "피해 없음"처럼 보임. TP-11 "쓸 곳 없어 인자 안 더함"이 틀렸다. 권장: 큰 생성자 끝 선택 인자 + 보조 행이 `ledger.Reconstructed(id)`.
- 낮음: L1 표시 위치 보존의 체력류 사건 목록(`CombatShownFacts.ForPresentation`)에 `Reconstructed` 없음(확인함) · L2 모델 렌더러 고르는 규칙 사본 둘 · L3 외형 층 놓기/다시 잡기 순서 사본 둘 · L4 잔해 생성 예외 때 원래 렌더러가 꺼진 채 남음 · L5 장부가 명령마다 모든 캐릭터 `CanTrigger`·LINQ 할당(적 AI 후보 평가 길, 추정) · L6 사실이 바뀐 유닛마다 상태 읽기 두 번 · L7 `IsHitLike`의 쓸모없는 사망 검사 · L8 같은 표 실패 규칙 둘(`Get`/`TryGet`) · L9 잔해 프리팹 내용 검사 없음 · L10 `ForgetHudFacts`가 모습 기억도 비움(이름) · L11 부품이 아군 표시에도 생김 · L12 잔해 재질의 디졸브 여부는 재생 확인 · L13 바꾸는 순간 윤곽·이동 투명도 대여 끝남(받아들임).
- 7기준: SSOT 8.0 · SOLID 8.5 · FW 9.5 · 독립성 8.5 · 캡슐화 8.0 · C#/Unity 8.0 · If/Tick 8.5. M1·M2·L1·L4 반영 시 약 9.0(검토자 추정).
- **반영(2026-11-11 사용자 "권장대로 반영" = M1·M2·L1·L4·L5·L7, 컴파일 미확인, 새 시험 없음, 시험 변경 없음):**
  - M1 `FieldUnitView` 자세 대여 `TryBeginDeathRagdoll`이 `_statusAppearance.IsReplaced`면 false. 걷은 것: `ICombatUnitPresentationTarget.ShowsReplacedAppearance`·`CombatUnitPresentationWindow.ShowsReplacedAppearance`·처리기 `launchRagdoll` 조건(두 파일은 HEAD와 같아짐). TP-9 바뀜.
  - M2 `AttackTargetPreview` 큰 생성자 끝 `targetReconstructs = false`·보조 행(`AttackResultFactory` 넉백·작렬)이 `ledger.Reconstructed(id)`. TP-11 바뀜.
  - L1 `CombatCharacterHits.ChangesHealthOnly`(체력·정규화·사망·재구성)를 `CombatShownFacts.ForPresentation`이 부름.
  - L4 `UnitStatusAppearance.Show`가 원래 렌더러 잡기·잔해 만들기를 상태 바꾸기 전에.
  - L5 `CommandAttackLedger`: 범위 시작은 `plan.Input.Characters` 참조만, `CaptureHitHealth`가 `_hitHealth` 대상만 `Triggered`, 집합은 재구성이 있을 때만.
  - L7 `IsHitLike` 재구성 갈래 = true(Core `CombatCharacterEvent` 생성자 검사 `CombatEvent.cs:141`이 보장).
  - 그대로: L2·L3·L6·L8·L9·L10·L11, L12는 Unity 재생 확인, L13 받아들임.
  - 작업 중 교체 도구가 빈 교체를 잘못 넣은 `>>>` 줄 둘을 찾아 지웠다(`FieldUnitView`·창구, 창구는 HEAD와 같음).

### 4.3 재생 보고 고침 (2026-11-11, 컴파일 미확인)

- 사용자 보고 "InvalidOperationException: 상태 세부 표시가 없습니다: ReconstructionStatus". 원인: HUD 상태 설명 문구(`Display/Unit/CombatStatusDescriptionFormatter.Describe`)가 출처 상태 형식·수명 종류를 갈래로 나누는데, 11-07·11-10 Core가 더한 상태 넷(`FireStatus`·`BleedingStatus`·`MovementRangeDecreaseStatus`·`ReconstructionStatus`)과 수명 둘(`AffectedSideTurnStarts`·`NextAffectedSideTurnEnd`)이 없었다. 재구성만이 아니라 화염·출혈·이동력 감소 HUD 설명도 같은 곳에서 멈췄을 것이다(11-10 표시 변환을 넣을 때 이 갈래를 놓침).
- 고침: 상태 넷·수명 둘 갈래(남은 횟수는 Core 출처 값 `RemainingAffectedSideStarts`). 출혈 피해 합계를 `CombatStatusDescriptionFormatter.BleedingDamage(stacks)` 하나로 모으고 `CombatUnitStatusFactory`의 사본을 걷음(민형님 Factory 공유 §53 고침).
- 남김: 특성 연출 확인 도구 시나리오 입력(`Dev/TraitPreview/.../PreviewStateWriters`)은 새 상태를 받지 않는다(그 상태를 시나리오에 넣을 때만 막힘, 개발 도구).
