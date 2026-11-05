# Core 버프·디버프 변경에 대한 View 연동 설계

상태: **승인된 A~F 구현 진행 기록은 [Task 계획](../plans/2026-10-31-status-effect-view-integration-plan.md) §11을 따른다.** Core 설정 응답과 실제 에디터 검증은 대기다. 이 문서는 설계 원본이며 실행 상태의 원본은 Task 계획이다.

기준: `a0ec23c0e`의 공개 계약을 병합한 현재 `0a4d3303c`와 작업 트리. 기존 몸체 텍스트 Phase F~J의 미커밋 작업도 보존한다. Core 인계 문서의 과거 View 경로 설명은 현재 코드와 대조했으며, 그대로 미구현 목록으로 복사하지 않았다. 기준은 [Core 공개 인계](../handoffs/2026-10-30-buff-debuff-system-core-view-handoff.md), [공동 구조 기준 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리), [JMH 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), Assets/AGENTS.md다.

## 1. 목표와 경계

실제 Core가 공개한 상태를 사건 순서대로 HUD에 표시하고, 보유 특성 설명·발동 연출·조립 시연이 같은 계약을 쓰게 한다. 상태를 주는 특성과 수신자가 가진 효과를 구분한다. 예를 들어 사냥꾼의 표식은 특성이고 약점 노출은 효과다.

- Core가 상태·유효값·출처·수명·피해/회복을 결정한다. View는 수명 감소, 최대 출처 선정, 중첩/교체, 보호막 소비, 회복 주기 진행을 계산하지 않는다.
- Core·FieldCamera·Architecture·Framework 기반 파일은 구현 범위가 아니다. 부족한 Core 조회는 [경계 인계 §55](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#55-core-버프디버프-view-연동을-위한-전투-밖-특성-설정-조회-요청-2026-10-31-초안미전달)로 요청한다.
- 카테고리=현재 HP바 옆, 아이템=고정 슬롯, 상태=버프→디버프 배치를 유지한다. 보유 특성/격파 목표를 상태 영역에 다시 넣지 않는다.
- 공유 HUD의 Description Panel/Text 생성·연결은 기존 사용자 결정대로 후속이다. 이번에는 기존 설명 문자열 전달 경로까지 연결한다. 몸체 텍스트 프리팹과 HUD 설명은 별개다.
- 기존 효과/Shake/UnitActions/추가 공격·취소 수명을 유지한다. 모든 특성의 연출을 새로 등록하는 작업은 아니다.
- 새 View 테스트 코드·자동 검증 메뉴를 만들지 않는다. 구현 후 컴파일·정적 대조와 DB 에셋 변경에 필요한 기존 DataBaseAssetTests를 수행하고, Play·가독성·Profiler는 사용자가 확인한다. 커밋·push·임의 스테이징은 하지 않는다.

## 2. 현재 코드에서 확인한 상태

| 항목 | 현재 상태 | 이번 처리 |
|---|---|---|
| `CombatTraitStateEvent` 순서 | EventTypeTable에 Facts/Final.EntityId/Origin 등록됨 | 재등록하지 않고 root/child·공격 묶음 경계를 대조 |
| 전체 사실 반영 | FactEventHandler가 `state.Final`을 그대로 ApplyCharacter | 유지. 새 효과/회복 주기가 다른 HP·이동 사실과 공존하는지 검증 |
| HUD 직후 피드백 | Presenter가 ApplyFacts → Render → AfterFacts 호출 | 이 순서를 유지하고 새 근거 해석기 연결 |
| 옛 표식 사건 | ID 39 처리기가 다른 디버프 6칸을 보존하며 옛 표식만 반영 | 신규 운영 경로에서 사용하지 않음. 읽기 호환과 새 약점 노출을 구분 |
| 상태 HUD | CombatUnitStatuses가 옛 UnitStatusKind 9종과 Debuffs/TraitEffects/가속을 개별 조회 | 효과 Snapshot 8종 기준으로 전환 |
| 분류 | 상태 SO의 Polarity와 Core Classification이 둘 다 존재 | 현재 효과 분류는 Core 한 곳. 외형 SO의 중복 분류 제거 |
| 위압 | 병합 시 PriorityPenalty 읽기·HUD `{1}` 문구만 제거 | 특성 SO의 우선도 문구·가용성 문구까지 정리 필요 |
| 사냥꾼의 표식 카드 | 다른 아군만 적용·공격 시 소모·소모 전 무기한 문구가 남음 | 약점 노출의 설정 증가율/대상 진영 턴 끝 수로 변경 |
| 재생 | 전투 TraitStatus에 회복량/주기/진행값 있음. 정적 SO는 모든 캐릭터 행동 종료로 설명 | 자기 진영 턴 종료 정책으로 수정, 실제 회복은 기존 HP 사건 사용 |
| 전투 밖 카드 | OwnedCharacterStatsSnapshot에 HuntersMarkDescription은 있으나 Regeneration/Intimidation 설정 DTO 없음 | 표식은 즉시 연동 가능. 두 특성의 정량 설명은 Core 요청 후 연결 |
| 상태 시연 | StateKind가 Acceleration/AttackGrant 두 가지, 공격력 부여는 레거시 DTO로 작성 | 정규 상태 입력·출처별 명시 변경으로 확장 |
| 상태 아이콘 | 기존 9 SO 중 실제 Sprite는 AttackBonus·Acceleration 두 개만 연결 | 기존 선택적 기본 아이콘 정책 유지. 약점 노출의 깨진 방패 아트 인수는 별도 확인 |

Core 인계에 적힌 “TraitState 미등록”, “옛 표식 갱신이 세 디버프만 보존”은 현재 View에 해당하지 않는다. 이를 근거로 재생기를 다시 만드는 계획은 세우지 않는다. 사용자가 병합 커밋을 완료했으며 이번 문서 작업은 Git 병합 상태를 변경하지 않는다.

## 3. 원본과 데이터 흐름

```text
Core Result.Events의 현재 사건 / CharacterBuffer의 최종 불변 사실
  → 기존 CombatShownFacts의 현재 표시 사실
  → Core 공개 StatusEffectReadModel.Capture(그 사실)
  → 효과별 표시 변환 + 기존 UnitStatusVisual 조회
  → UnitHUDStatus 목록 → 기존 UnitHUD 슬롯·설명 수명

TraitActivation + 대응 TraitState
  → 기존 불변 FeedbackPlan / private Session
  → Render 직후 기존 텍스트·Effects·Shake
```

`CharacterData.StatusEffectSnapshots`는 해당 CharacterData와 같은 시점의 Core 조회값이다. 중간 재생에서 최신 CharacterBuffer의 목록을 가져와 쓰면 안 된다. 기존 FieldUnitPresenter가 사실을 받는 공용 경로는 그대로 두고, 그 `CharacterCombatFacts`에 대해 Core의 공개 **읽기 전용** Capture를 호출한다. 이 API는 같은 사건의 Core 계산을 읽는 것이며 View가 Domain 규칙을 복사하는 것이 아니다. 현재 경로의 Core 유효 공격/방어 조회도 같은 사실을 사용한다.

Capture/설명 조립은 HUD 사실 갱신 때 수행한다. 새 Update·타이머·Buffer 폴링·전역 상태 저장소를 만들지 않는다. 성능 문제가 재현되기 전 별도 효과 상태 캐시는 추가하지 않는다. 이후 캐시가 필요해도 불변 사실 참조에 종속된 표시 파생값만 허용한다.

## 4. 효과 키와 첫 표시 범위

키는 Core `StatusEffectKey`, 분류는 Snapshot.Classification이 원본이다. `TraitKey`를 효과 키로 쓰거나 옛 `UnitStatusKind` 숫자를 새 enum에 캐스팅하지 않는다.

| 효과 키 | Core가 제공하는 요약 | 출처/수명 표시 | 기존 HUD 이전 |
|---|---|---|---|
| AttackIncrease=1 | TotalAmount, AppliedAmount, HeldForAttackAmount | 부여분의 출처별 ControllerTurnEnds. Held는 별도 AttackCompletion | 공격력 부여·가속을 **한 아이콘**으로 합침 |
| AttackDecrease=2 | ReductionPercent | 다음 공격 완료까지. 첫 적용값을 읽음 | 위압 → 공격력 감소 |
| DefenseIncrease=3 | Amount | CurrentEligibility. 출처/턴을 만들지 않음 | 새 방어력 증가 외형 행 |
| DefenseDecrease=4 | EffectiveDefense | 출처별 ReductionPercent, BattleEnd | 옛 방어 감소·방어구 부수기 → 한 효과 |
| Shield=7 | RemainingEligibleBudget | 출처별 RemainingBudget/Eligible/LastResetControllerTurnCount. ReconciledBudget | 보호막 유지. 비활성·소진 출처도 상세에 남김 |
| Disarm=10 | ActionRestriction의 Disarm | 출처별 RemainingAffectedSideTurnEnds | 무장 해제 |
| Bind=11 | ActionRestriction의 Bind | 출처별 RemainingAffectedSideTurnEnds | 발목 부수기 → 속박 |
| WeaknessExposure=13 | MaximumIncreasePercent | 출처별 IncreasePercent/RemainingAffectedSideTurnEnds | 옛 표식 아이콘 경로 대신 약점 노출 |

OutgoingDamageIncrease=5·IncomingDamageDecrease=6은 대상/지형 문맥에서 평가된 기여분이 필요하다. 현재 Capture는 상시 효과 행을 내지 않는다. 기존 공격 연출/피해 기여분 경로를 유지하고 보유 특성만으로 배지를 만들지 않는다. MovementRangeIncrease=8·AttackRangeDecrease=9·KnockbackImmunity=12도 현재 공개 Snapshot/생산 경로가 없는 키를 아이콘으로 합성하지 않는다. 키 13개와 지금 표시 가능한 8종은 구분한다.

재생은 별도의 Healing 효과가 아니다. 설정/진행은 RegenerationTraitStatusSnapshot, 실제 체력은 HP 사실, 실제 양은 HealthRecovered 발동/HP delta가 원본이다. 설정 회복량을 실제 회복량으로 재생하거나 HUD 갱신에서 주기를 진행하지 않는다.

## 5. View DB 이전

기존 `UnitStatusVisualDataTableSO`·`UnitStatusVisualTable`·서비스 키/조회 경로를 재사용한다. 두 번째 효과 표를 만들지 않는다.

- 입력 `kind: UnitStatusKind`를 `effectKey: StatusEffectKey`로 명시적으로 이전한다. Data는 Key/DisplayName/Icon/DescriptionTemplate를 가진다. 분류 입력은 제거하고 Core Classification을 소비한다.
- Query의 Get/TryGet 인자는 StatusEffectKey로 바꾼다. 필수 8종 누락·None/미정의·중복 키·형식에 맞지 않는 설명 틀은 대상 키/에셋을 명시한 예외로 거절한다.
- 실제 Sprite가 없는 기존 행의 선택적 기본 아이콘은 유지한다. 약점 노출은 최종 아트 인수에서 **깨진 방패** 연결을 확인한다. 자원이 확인되지 않은 상태를 아트 완료로 처리하지 않으며 새 아이콘을 임의 생성하지 않는다.
- 기존 대표 에셋의 GUID를 유지하며 이름을 효과명으로 옮긴다. 기존 설명이 잘못된 의미를 담고 있으면 새 계약으로 고친다.

| 기존 대표 SO | 이전 효과 / 예정 에셋 이름 |
|---|---|
| UnitStatusVisual_AttackBonus | AttackIncrease / UnitStatusVisual_AttackIncrease |
| UnitStatusVisual_Intimidation | AttackDecrease / UnitStatusVisual_AttackDecrease |
| UnitStatusVisual_DefenseReduction | DefenseDecrease / UnitStatusVisual_DefenseDecrease |
| UnitStatusVisual_HuntersMark | WeaknessExposure / UnitStatusVisual_WeaknessExposure |
| UnitStatusVisual_AnkleBreaker | Bind / UnitStatusVisual_Bind |
| UnitStatusVisual_Disarm, Shield | 같은 이름·GUID 유지 |
| 없음 | DefenseIncrease 새 SO/meta |

중복되는 옛 Acceleration·ArmorBreak SO는 삭제하지 않고 활성 테이블에서 뺀다. 새 필드/설명으로도 명시적으로 이전해 잘못된 옛 숫자가 다른 효과로 읽히지 않게 한다. 비등록 에셋을 다시 등록해 키가 중복되면 진단한다. 코드·SO·활성 테이블 참조를 같은 Phase에서 전환하고 구/신형 영구 fallback을 남기지 않는다. 모든 참조가 이전된 뒤 옛 UnitStatusKind 코드/meta를 함께 정리한다. Missing Script 옛 에셋 48개는 대상이 아니다.

생성/등록 경로는 `ViewDataBaseFactory.Create → UnitStatusVisualTable.ExportServices → ViewTableKey.UnitStatusVisual → ViewDataBase/IViewQuery → CombatDisplayTables → CombatSceneDisplayFactory/FieldUnitPresenter`다. 기존 Query 등록 하나를 유지하며 루트 재등록·resolver 우회를 추가하지 않는다.

## 6. 표시 변환·상세·입력 수명

`CombatUnitStatuses`는 사실을 읽어 목록을 만드는 기존 진입점이다. 내부 변환을 효과별 등록과 공통 출처/수명 표시로 나눈다.

- `ICombatEffectFormatter`: Key와 해당 typed Snapshot의 요약/세부값 변환만 받는 좁은 내부 계약. 새 Framework 서비스가 아니다.
- `CombatEffectFormatters`: Factory에서 조립한 불변 등록 목록. 키/허용 Snapshot 형식/요약 틀 값 수를 함께 정의해 별도 지원 표가 생기지 않게 한다. 같은 ActionRestriction 변환은 Disarm/Bind에 재사용한다.
- `CombatStatusDescriptionFormatter`: 공통 출처·수명·활성 여부 문구. 집계나 만료 판단은 하지 않는다. 효과마다 다른 typed 수치를 위한 구체 변환과 공통 표현만 결합한다.
- SO의 설명 틀은 **요약 표현**, 수치는 Snapshot이다. 출처/수명 공통 문구는 formatter 한 곳이다. 기존 위치 인자 틀의 값 수 검증을 유지한다.

HUD는 Snapshot 순서(버프→디버프, 같은 분류의 효과 키 순서)를 유지한다. 키는 `StatusEffect:<숫자>`처럼 효과 식별만 사용한다. 출처나 남은 수명이 바뀌어도 같은 아이콘/고정 설명을 재사용한다. 출처마다 아이콘을 늘리지 않는다. 보호막의 비활성 출처만 남아도 Core가 행을 주면 그 행과 상세를 유지한다.

상세 예시: 약점 노출 30%, 출처 A 20%/대상 진영 턴 끝 2회, B 30%/1회. 다음 Core 상태가 A 20%/1회만 주면 같은 아이콘의 요약/상세를 바꾼다. View가 B를 삭제하거나 A를 승자로 계산하지 않는다. 값 0·출처가 없는 DefenseIncrease·가속 Held·nullable legacy Origin을 각 계약대로 구분한다.

Origin이 있으면 Kind/OwnerInstanceId/DefinitionKey/RuleKey의 공개 식별을 보존한다. 이름은 기존 표시 조회로 해석할 수 있을 때만 붙이며, 사망/이탈한 출처를 조회하지 못해도 효과를 지우지 않는다. nullable Origin은 “출처 정보 없음”으로 표현한다. RuleKey를 사용자에게 게임 규칙 이름처럼 노출하지 않는다. LastResetControllerTurnCount는 과거 초기화 정보이며 남은 턴으로 표시하지 않는다.

기존 UnitHUD.SetStatuses의 재사용·hover·클릭 고정·키별 재선택·풀 반환을 유지한다. 같은 Key의 설명 변경은 열린 설명에도 반영하고, 효과 소멸은 기존 고정을 해제한다. 아이템/카테고리·상태 레이아웃은 바꾸지 않는다. Description Panel/Text가 비어 있으면 기존처럼 설명 UI는 열리지 않는다. 새 팝업이나 서비스로 우회하지 않는다.

## 7. 사건 순서와 발동 연출

### 7.1 전체 사실과 표시 기하

기존 TraitState 등록·ApplyCharacter(Final)·Render 후 통지는 유지한다. 결과 시작/완료/Cancel/재바인딩이 상태 시계나 회복을 진행하면 안 된다. 결과 끝 Buffer 치환은 중간 사건 소비의 대체 증거가 아니다.

`CombatShownFacts.ForPresentation`의 기존 끌어오기 도착 보정은 CombatCharacterEvent에만 적용된다. 새 상태 사건이 공격 중 과거 위치를 담는 경우에도 이미 보여 준 도착점·HP 순서가 되돌아가지 않는지 함께 확인한다. 보정이 필요한 경우 기존 `_pullArrivals`가 증명하는 표시 기하만 `WithPlacement`로 옮기고, 상태/HP/경험치/아이템/행동/Stats는 사건 Final을 보존한다. 실제 이동·이탈 사건이 오면 기존처럼 보정을 끝낸다. 이벤트 자체를 새 객체로 바꿔 FeedbackPlan의 사건 식별 연결을 끊지 않는다. 이 경계 밖에 일반적인 상태 합치기 규칙을 만들지 않는다.

옛 ID 39는 읽기 호환에 한정한다. 이를 약점 노출로 변환하거나 신규 시연에서 생성하지 않는다. Core의 보존 계약/타입 삭제는 이 작업에서 하지 않는다. Hunter의 별도 지정 대상 Mark와 영구 보상도 그대로 유지한다.

### 7.2 기존 몸체 피드백과 약점 노출

기존 Profiles·Evidence 등록·불변 Plan/private Session·프리팹 풀을 재사용한다.

- 가속 X N은 실제 자발 이동 수이며 AttackIncrease 총량으로 나누거나 대체하지 않는다. CleanSweep은 해당 특성 출처의 AttackIncreaseStatus 전후 값을 읽도록 canonical 상태로 옮긴다. 다른 출처의 증가를 자기 발동으로 보지 않는다.
- 새 `WeaknessExposure` 근거는 수신 대상·같은 공격 Origin·발동 Source/특성에 해당하는 공개 출처의 추가/값·수명 갱신을 확인한다. 여러 출처 중 최대값이 그대로인 새 부여나 약한 값으로의 갱신도 상태 변경이다.
- **HuntersMark/StatusApplied의 Magnitude는 현재 1이다. 증가율이 아니다.** 증가율은 typed 상태에서 읽으며, Magnitude와 수치 차이 비교로 대응하지 않는다. RuleKey 문자열로 출처를 새로 만들어 내지 않는다.
- 기존 공용 프로필에 HuntersMark/StatusApplied의 StateApplied 지원을 등록하고, 해당 SO에 `StateApplied / BodyText Targets / Count None` 줄을 추가한다. 문구는 기본 특성 이름, 외형/유지/페이드는 기존 공통 프리팹 방식이다. 파티클·Shake는 임의 추가하지 않는다.
- 같은 출처·같은 값·같은 수명으로 Core가 새 사건을 내지 않으면 재출력하지 않는다. 상태의 매 HUD 갱신으로 문구를 만들지 않는다. 불명확한 대응은 기존 사전 진단/운영 경고 정책을 사용한다.
- 희생의 실제 나가기 시작, 가속/깔끔한 뒷정리의 HUD 직후 시작, UnitActions 동시 시작/피격 후 복귀, 공격 Gate/generation/Cancel 의미는 유지한다.

재생 진행 변경만으로 새 버프 텍스트를 만들지 않는다. 실제 HealthRecovered 발동/HealthChanged를 기존 회복 연출로 보이며, 구성 회복량·경과 애니메이션으로 HP를 조작하지 않는다.

## 8. 카드·특성 설명·퇴역 처리

| 대상 | 변경 |
|---|---|
| HuntersMark | “적중 후 약점 노출 부여”, 설정 증가율/대상 진영 턴 끝 수. 첫 적중 소급 적용·다른 아군 한정·일회 소모·무기한 문구 제거 |
| Intimidation | 공격력 감소만 설명. 다음 공격 완료 수명. 반복 피격 우선도 감소/일부 미지원 문구 제거 |
| Regeneration | 자기 진영 턴 종료 기준 회복량/주기. 모든 캐릭터 행동 종료 문구 제거. 완료 진행과 설정 주기를 구분 |
| MoveAttackDiscount=20 | 활성 TraitVisual 테이블에서 제외, SO/GUID는 보존. 획득/동작을 View가 복원하지 않음 |
| Retired/Deferred | Core가 준 Availability 사유를 구분해 표시. View가 별도 가용성 목록·상수 37종을 소유하지 않음 |

정책 문구는 TraitVisual.Description 한 곳에 두고, UnitCardTraitContent의 구형 표식 전용 정책 문구를 없앤다. 정량 설정은 typed Snapshot에서 가져와 공통 카드 변환에서 덧붙인다. SO의 `n`을 런타임 규칙 값으로 사용하지 않는다. 전투 카드/선택 정보·덱/로스터가 같은 변환을 재사용하고, 호출자가 다른 시점의 설정을 섞지 않는다.

HuntersMarkDescription은 OwnedCharacterStatsSnapshot → DeckEditModelBuilder → DeckEditCardModel/DeckEditUI → UnitCardTraitContent 및 UnitCardUI에 전달되는 경로를 유지하면서 Duration도 소비한다. 전투 Regeneration/Intimidation은 같은 사실의 TraitStatuses에서 읽을 수 있다.

**외부 의존성 CVI-R1:** 전투 밖 OwnedCharacterStatsSnapshot에는 Regeneration/Intimidation의 정량 설정이 없다. 해당 Core 공개 조회를 요청하며 View가 InGame DB/Unit SO를 읽거나 전투 중 유닛을 찾아 대신 채우지 않는다. Core 응답 전에는 올바른 일반 설명까지만 가능하고, 전투 밖 두 특성의 정량·강화 차이 인수는 대기다. 수치를 만들어 넣거나 이 항목을 완료 처리하지 않는다. 조회가 제공된 뒤에만 덱/로스터 모델·UI 인자를 확장한다.

기존 키만 아는 개발 표시에는 숫자 없는 정책 문구가 허용된다. 반면 Core가 정량 설정을 제공한다고 보장하는 실제 조회 경로에서 데이터가 누락/형식 불일치면 대상 특성·필드를 명시해 거절한다. Retired의 역사적 표시는 진단/읽기 호환과 운영 후보 노출을 구분한다. 도구는 활성 TraitVisual 등록을 기준으로 선택/사전 검사하며 Core 가용성을 임의 판정하지 않는다. 전투 밖 카드의 Availability도 같은 Owned 조회에서 모델/공통 카드 변환까지 전달한다. 성장 팝업은 기존처럼 Core가 준 choice.Description과 후보를 사용하며 View가 퇴역 목록을 복제하거나 후보를 재판정하지 않는다.

## 9. 조립 시연 확장

하드코딩한 특성별 시나리오 실행기를 만들지 않는다. 기존 Packet/Movement/Attack/Activation/State 조립을 유지하고 명시 Health/Return 블록을 덧붙이며 새 **명시적 사실 입력**만 추가한다.

- 기존 StateKind 숫자 0=Acceleration, 1=AttackGrant와 프리셋 ID/GUID를 보존한다. 새 효과 상태/재생 진행 입력은 enum 뒤에 추가한다. v1→v2 Import를 한 번 수행하며 관리 참조 직렬화는 쓰지 않는다.
- 정규 효과 입력은 Key·수신 유닛·안정적인 시연 Source ID·공개 Origin·typed 수치·수명·Eligible을 갖는다. 같은 Source ID의 변경/제거는 사용자가 지정한다. 키마다 필요한 입력만 표시하고 Core DTO 제약과 공용 표시 변환으로 사전 검사한다.
- 기존 AttackGrant 작성은 canonical AttackIncreaseStatus로 옮기며 무관한 StatusEffects를 보존한다. 레거시 DTO로 전체 상태를 다시 만들지 않는다. 가속 Held는 trait status로 유지한다.
- DefenseIncrease는 AppliedStatus가 아니라 Core가 공개한 CohesionDefenseBonus의 명시 입력이다. 가속 Held와 마찬가지로 가짜 출처/기간을 만들지 않는다. 시연의 TraitAttackBonus 보정은 기존 합계식을 복사하지 않고 공개 Core 읽기 API로 받는다.
- 최대 출처 선정/피해 배율/턴 만료/회복/귀환 정리는 도구가 자동 계산하지 않는다. 턴 경과 후 남은 값, 회복 후 HP, 귀환 전후 제거 결과를 각각 State/Health/Return 입력으로 명시한다. Core 규칙 검증과 시연 결과를 구분한다.
- 약점 노출 2출처→fallback→소멸·약한 값/짧은 수명 갱신, 가속+일반 공격력 부여, 보호막 활성/비활성, Disarm/Bind·방어 감소 공존, 재생 주기/실제 회복량·두 귀환 예제를 기존 조립 블록으로 저장한다.
- 기존 가속/깔끔한 뒷정리/희생/ManualStep 프리셋은 동작·시간·입력을 보존한다. 기존 StateApplied 단독 Moment의 제한은 유지한다.

새 State는 CombatTraitStateEvent 전체 사실을 만들고 운영과 동일한 표시/피드백/DB/프리팹 풀을 사용한다. 시연을 위해 운영 CharacterBuffer에 쓰거나 Core 명령을 우회하지 않는다.

## 10. 구조 검토와 검증 기준

| 기준 | 이번 적용 |
|---|---|
| SSOT | 효과·분류·수명·유효값은 Core, 외형/요약 틀은 기존 View 표, 특성 설명은 TraitVisual, 표시 사실은 기존 ShownFacts. 옛 enum/분류/상시 배지 중복 제거 |
| S | 사건 사실 반영, Core 읽기, 효과 변환, 공통 상세, DB 조회, UI 입력/배치를 분리 |
| O | typed 효과 추가는 해당 formatter/등록/외형으로 한정. HUD 레이아웃·공격·몸체 풀을 수정하지 않음 |
| L | 기존 비차단 Accent·Player·완료/Cancel/Release·UI 재사용 계약 유지 |
| I | formatter는 한 Snapshot/외형, 출처 이름은 읽기 조회만 사용. 전체 CoreGroup/Flow 전달 금지 |
| D | View DB/기능 협력 객체 조립은 Factory, 소비는 기존 IViewQuery와 주입된 협력 객체. Root resolver 직접 조회 없음 |
| 캡슐화 | private Session/불변 Plan·불변 formatter 등록. 상태 원본·UI 풀 내부 목록을 외부에 노출하지 않음 |
| if/Tick | 필요한 nullable 출처·선택적 아이콘·소실·취소 검사 유지. 상태/수명 갱신 Tick 없음. 표현 변경을 Buffer 폴링으로 감지하지 않음 |

JMH/FW 확인 경로: 기존 DB Factory의 Query 생성/Export → View 그룹/키 → IViewQuery → CombatDisplayTables → Scene Factory의 변환 주입 → Presenter/UnitHUD 소비·종료까지 추적한다. 기존 Query만 등록하므로 같은 Builder 인터페이스 중복(FW-001), Build 후 변경(FW-003), 하위 서비스 루트 재노출을 만들지 않는다. 새 실행 모듈이 없어 FW-002 AddModule을 임의 추가하지 않는다. Core/내부 Store 우회(FW-004), UI 이력/풀 반환 경계도 유지한다. Bus·Mesh·JobQueue를 바꾸지 않은 것을 그 계약의 실행 통과로 과장하지 않는다.

UI 등록 단위는 기존 UnitHUDLayer/UIGroup과 UnitHUD 위젯·Info/Hover 패널이다. 효과 슬롯은 그 세부 위젯이며 새 UIId/정책/독립 팝업/MonoBehaviour를 추가하지 않는다. 외부 개폐는 기존 UISystem, Button 입력은 UIInputBinding/IUIInputReceiver, 아이템 우클릭은 기존 Secondary 경로를 유지한다.

변경 독립성(구조 기준 §4.6): 기존에는 상태 하나의 의미가 enum·줄 표·분류 SO·특성 이름·여러 설명에 흩어졌다. 이전 후 아이콘/문구는 효과 SO, 수치 표현은 해당 formatter, 상태 사실은 기존 재생, 새 의존성은 Factory에 머문다. 공통 키/취소/풀·설명 고정 수명이 바뀌는 경계는 통합 확인하며, 관련 없는 이동/카메라 구현을 다시 설계하지 않는다.

필수 인수: 전체 상태와 HP/기하 보존, 2출처·fallback·갱신·소멸, 양쪽 귀환/출처 이탈, 보호막 수명 구분, 재생 cap/주기/종료 순서, 일반/강화 카드, 별도 Hunter Mark 유지, 가속 X N·깔끔한 뒷정리·희생 회귀, 취소/재바인딩 중복 없음. 방법/Phase는 Task 계획을 따른다. 컴파일 성공·합성 시연·실제 인게임 발동은 서로 다른 증거다.

## 11. 의존성과 완료 구분

| 의존성 | 상태 | 막는 범위 |
|---|---|---|
| canonical 상태·Snapshot·읽기 API | 병합 제공 | 즉시 설계/후속 구현 가능 |
| 현재 전체 사실/발동 프리팹 경로 | 코드 존재, 앞 작업 Play 인수 별도 | 이번 연동에서 보존·회귀 확인 |
| CVI-R1 전투 밖 재생/위압 설정 | 요청 초안·미전달 | 두 카드의 정량/강화 설명만 대기. 나머지 Phase 진행 가능 |
| 약점 노출 깨진 방패 Sprite | 파일명/meta 검색으로 전용 자원 확정 못함 | 최종 전용 아트 인수. 임의 특성 아이콘 대체를 완료로 기록하지 않음 |
| HUD Description Panel/Text | 사용자 후속 보류 | 상세 문자열은 준비, 실제 설명 패널 가시성 인수 제외 |
| 기존 DataBaseAssetTests의 ShotGun 입력 | 시험은 HuntersMark30%/1, 현재 운영 에셋은 병합에서 유지한 Sacrifice | 실행 결과와 입력 가정 불일치를 분리해 기록. Core 소유 시험/특성을 임의 수정하지 않음 |
| Core 인계 StageKey2101 등 과거 진단 | 이번 View에서 재현/검증하지 않음 | 본 작업의 무관한 수리 범위에 추가하지 않음 |

구현/검증 결과는 Task 계획 §11에 기록한다. Core 요청은 작성만 했으며 외부 전달하지 않았다. 현재 Play 유지 지시에 따라 실제 에디터 검증은 대기다.
