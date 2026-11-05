# 위치 교환·정찰 부대·효과 인식 이동 취소 View 대응 설계

작성: 2026-10-31, View/Codex. 상태: **Core 병합0403babef의 실제 계약 인수 후 A~E 코드 및 F의 코드 컴파일·정적 검증/문서 완료**. 실제 에디터/DB 시험·Play는 사용자 후속이다. 실행 상태 원본은 [Task 계획 §10](../plans/2026-10-31-position-swap-recon-undo-view-integration-plan.md#10-실행-기록--2026-10-31)이다.

## 1. 기준과 범위

- 최초 계획 기준: `8ac16c1ed`. 구현은 사용자 병합 `0403babef`(Core `2bd95ad34`)와 작업 트리를 기준으로 했다. Body Text 설정은 `62aa4a0a7`, 이전 Core 효과 View 연동은 `71390ac71`에 포함돼 있다. 작성 시점 FMOD 캐시·전투 Test/Akimbo·가속 외형 SO·배경 타일 프리팹·Combat 씬의 사용자 변경을 보존한다.
- 새 계약 근거: 사용자가 첨부한 [Core 인계 문서](C:/Users/Hyeon2-400/Downloads/2026-10-31-position-swap-recon-undo-core-view-handoff.md). SHA256 `81c484389df9618f1080bbba3aa2dcde54c6d07a5a803dda4f851cfb1c489881`. 최초 첨부는 이 PC의 Downloads에 있다. 현재는 병합된 [저장소 Core 인계](../handoffs/2026-10-31-position-swap-recon-undo-core-view-handoff.md)와 실제 구현/설계/계획을 인수했다. 문서의 구현/시험 기록을 View 실행 성공으로 사용하지 않는다.
- 계획 작성 당시 없던 PositionSwap/ReconSquad·Participants/RemovedEffects·EffectiveIncreasePercent·무기한 수명은0403babef에서 제공됐다. 아래 새 API를 실제 공개 구현과 대조해 참조했으며 문서만으로 Core 타입을 복제하지 않았다.
- 구조 기준: [공동 설계 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), 특히 §4.6, [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), Assets/AGENTS.md.
- 기존 [효과 View 연동](2026-10-31-status-effect-view-integration-design.md), [이동 취소 View 연결](2026-10-30-movement-undo-view-design.md), [몸체 텍스트 3차 설정](2026-10-31-trait-body-feedback-design.md#10-3차--activation별-문구색크기위치)을 재사용한다. 이 문서와 다른 과거 효과 합산·보호막 이력 표시 설명은 새 계약 적용 시 본 설계로 갱신한다.

목표는 새 Core가 확정한 교환/복원·상태·장비·가용성을 순서대로 표시하는 것이다. View가 이동 효과의 사용 확정, 체크포인트 보관, 역적용, 피해 배율, 귀환 정리를 계산하지 않는다. Core·FieldCamera·Architecture·Framework 기반과 무관한 카메라/맵/HUD 줌 작업은 수정하지 않는다. 운영 획득 풀·강화 정의·유닛 특성 장착·StageKey2101·옛 Missing Script48개·성장 SerializeReference 수리는 범위 밖이다.

## 2. 현재 코드에서 재사용할 것

| 경계 | 현재 확인 | 이번 판단 |
|---|---|---|
| 전체 상태 | EventTypeTable의 TraitState 등록, FactEventHandler.ApplyTraitState, Render→AfterFacts 존재 | 첨부 §8의 미등록 지적은 과거 상태. 새 처리기/사건 스트림/상태 저장소 추가 없음 |
| 발동 | CombatEffectsEvent의 Traits와 `(TraitKey, Kind)` 외형 조회 존재 | 새 특성도 기존 Activation 경로로 연결 |
| 가용성 | Facts의 QueryRevision, Tracker의 캐시 키/수락/Read에서 SourceRevision 대조 | 첨부 §8의 revision 누락 지적은 현재 미해결 사항이 아님. 공유 Tracker를 다시 만들지 않음 |
| 취소 제출 | UndoInput이 ActorId·battle·turn·MoveCommandNumber를 Gate에 제출 | 같은 버튼/키/Flow/Job 재사용, 승객용 명령 없음 |
| 이동 미리보기 | Core가 준 MovementContext 전체를 보관해 Move에 전달 | 승객 정보를 View가 재구성하지 않음. 새 Context를 임의 복사/축약하지 않음 |
| Field→Field Returned | Return 경로의 ShowReturn이 방향 정리·전체 Final 적용·Render | 귀환 풀 반환/역방향 걷기와 구분. 쌍 복원 단위만 보완 |
| 흔적 | TraitTrailFact의 ID/Active로 Show, 마지막 Environment로 Render | 비활성 원본을 삭제하지 않고 표시 인스턴스만 회수 |
| 최종 사실/수명 | 결과 끝 Display.Refresh, 그 뒤 acknowledgment; ResultPlayback의 세대/Abort | 그대로 유지. Cancel/재연결은 추가 게임 실행 없음 |

이 경로의 보존 검토와 새 출력의 실제 Play 검증은 다르다. 기존 단일 되돌리기 사용자 확인을 쌍 복원 성공으로 확대하지 않는다.

## 3. Core 병합 대기와 인수 조건

| ID | 현재 상태 | 해제 조건 / 막는 범위 |
|---|---|---|
| G1 계약 인수 | **해제:0403babef** | 사용자가 제공/병합한 정확한 커밋에서 아래 공개 타입·생성자·검증을 확인. 새 타입을 사용하는 효과/카드/프로필/시연 구현은 그 전까지 대기 |
| G2 교환 사건 대응 | **해제: 실제 출력 소스 대조** | 전방 교환 종료점의 두 Moved와 취소의 한/두 Returned를 실제 결과에서 유일하게 대응할 수 있어야 함. 관련 사실 사이 상태 사건/도착 후속의 순서 확인. 쌍 표시 Phase만 제한 |
| UI/아트 | 기존 사용자 후속 | HUD Description Panel/Text, 깨진 방패/신규 특성 아이콘 확정. 문자열·기본 아이콘 사용과 실제 패널/전용 아트 인수 구분 |
| CVI-R1 | 기존 별도 대기 유지 | 전투 밖 재생/위압 설정. 이번 ReconSquadDescription 제공과 혼동하지 않음 |

G1 확인 목록: TraitKey440/450·Availability, 실제 MovementCause/Context.PositionSwapPassenger, MovementUndoResult.Participants/RemovedEffects, WeaknessExposureEffectSnapshot.EffectiveIncreasePercent, WeaknessExposureStatus의 수명/nullable clock/Movement, 새 LifetimeKind, ReconSquadTraitStatusSnapshot 및 OwnedCharacterStatsSnapshot.ReconSquadDescription. 생성자·불변성·Equality/SameValues·공개 Result 구성 경계도 확인한다.

문서만 보고 새 enum을 숫자로 캐스팅하거나 View에 Core 타입/호환 별칭을 복제하지 않는다. 병합 명령·충돌 해결도 이번 요청에서 수행하지 않는다. G1은 Core 기능 추가 요청이 아니라 인수 조건이다. 제공됐다고 적힌 데이터가 실코드에 없거나 G2 대응이 모호하면 그 소비 항목만 대기로 두고 [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 구체 요청을 기록한다. 임의 규칙이나 인접 사건 추측으로 메우지 않는다.

## 4. 효과 요약·상세·아이콘

### 4.1 약점 노출

`CombatUnitStatusFactory`의 WeaknessExposure formatter는 G1 후 `EffectiveIncreasePercent`를 읽는다. 효과 키13·기존 SO·아이콘 키 `StatusEffect:13`은 유지한다. Recon10+Recon20+HuntersMark30은 **50% 한 아이콘**이다. 같은 그룹의 최대값과 그룹 간 합계는 Core가 정하며 View는 Max/Sum·callback 문자열·보유자 ID로 다시 집계하지 않는다.

Sources는 억제된 출처도 원래 순서대로 모두 상세에 표시한다. 공개된 개별 증가율·출처 이름/ID·수명을 옮기고, UsedSources/기여 여부를 임의로 계산해 승자를 강조하지 않는다. RuleKey는 효과 동일성 확인에 공개 계약대로 사용할 수 있지만 사용자 문구나 새 규칙 이름으로 만들지 않는다. 이동 기여분은 제공된 Movement 식별을 그대로 보존한다.

수명 표시는 공통 formatter의 한 곳에서 처리한다.

| 공개 수명 | 문구 의미 |
|---|---|
| AffectedSideTurnEnds + 양수 잔여 횟수 | 대상 진영 턴 종료 N회 남음 |
| UntilReserveReturnOrDeath + 잔여 횟수 null | 대상이 예비 영역으로 귀환하거나 사망할 때까지 |

null을0턴·무한대 숫자로 바꾸지 않는다. 귀환하는 **생산자**와 효과를 받은 **대상**을 구분한다. View가 출처 사망/귀환이나 진영 턴 경과로 Sources를 지우지 않는다. HuntersMark 정책 설명의 “전체 출처 중 최대 하나” 문구와 가이드/프리셋 기대값은 새 그룹 합산 의미로 갱신한다. 증가율/반경/기간 숫자를 SO 정책 문자열에 복제하지 않는다.

### 4.2 보호막 활성 아이콘과 이력

이미 제공된 `ShieldEffectSnapshot.RemainingEligibleBudget`으로 활성 HUD 표시 여부를 정한다. **유효 예산이 양수일 때만 Shield 아이콘**을 만든다. Sources가 남았다는 사실만으로 켜지 않는다. 표시 중 상세에는 비활성·소진 출처도 포함한다. 모두 비활성/0이면 아이콘·고정 설명을 정리하지만 canonical Sources/초기화 이력은 변경하지 않는다.

이 필터는 `CombatUnitStatuses`의 표현 선택이며 Core 캡처/피해/소비 계산이 아니다. 다른 효과의0값을 일괄 숨기는 범용 규칙으로 확대하지 않는다. 새 표·Polarity enum·캐시를 추가하지 않는다. 이 부분은 현재 공개 API만으로 준비할 수 있다.

## 5. 신규 특성 외형·카드·몸체 피드백

G1 후 기존 TraitVisual 표에 `TraitVisual_PositionSwap`·`TraitVisual_ReconSquad` SO/meta를 등록한다. 초기 표시명은 “위치 교환”·“정찰 부대”이며 View 입력에서 편집한다. 전용 Sprite를 임의 생성하지 않고 기존 선택적 아이콘 경로를 사용한다. 획득 풀/강화/운영 유닛을 만들지 않는다.

- PositionSwap/MovementChanged: ActivationStart, Body Text Source, Count None, 기본 특성 이름. 양쪽 배치는 character facts만 결정한다. UnitActions/공격 덮어쓰기/임의 파티클·Shake는 추가하지 않는다.
- ReconSquad/StatusApplied: StateApplied, Body Text Targets, Count None, 기본 특성 이름. 대응 대상 상태·HUD를 반영한 직후 문구를 한 번 시작한다. Source가 받는 버프로 오인하지 않는다.
- 색·크기·Position Offset·직접 문구·Prefab Override는 완료된 몸체 텍스트3차 입력을 그대로 쓴다. 초기값은 흰색28/offset0·공통 프리팹·기존 Hold/Fade다. 기존 가속 SO 사용자 편집값을 다시 초기화하지 않는다.

Recon 설정은 전투의 TraitStatuses 및 전투 밖 **같은 Owned 조회의 ReconSquadDescription**에서 IncreasePercent/Radius를 읽어 공통 카드 변환에 전달한다. UnitCardUI, DeckEditModelBuilder/CardModel/DeckEditUI를 같은 읽기로 연결한다. SO는 정책, Snapshot은 수치다. 미보유·누락·타입/키 불일치를 구분해 필수 설정 누락은 진단한다. 기본10/반경1을 View 상수로 대체하지 않는다. 성장 후보/강화 설명은 Core가 준 choice.Description을 유지한다.

현재 약점 노출 근거의 SingleOrDefault는 timed HuntersMark의 단일 출처를 전제로 한다. Recon에 그대로 재사용하지 않는다. `CombatWeaknessExposureEvidence`는 공개 Origin과 optional Movement로 식별되는 **출처 목록의 전후 변경**을 비교하고, 프로필/Factory에 Recon을 추가한다. 여러 이동 기여분·최대값 불변·억제 출처의 신규 부여도 대응한다. 한 대상/발동당 문구 한 번이며 기존 동일값 재부여·단순 HUD 갱신·역적용/귀환 정리가 새 forward 문구를 만들지 않는다.

발동 Source/Target/ResultOrigin 및 상태 사건 객체의 식별을 보존한다. Recon Magnitude의 의미는 실제 생산자를 확인하며 HuntersMark의1이나 증가율로 추정하지 않는다. 실제 새 소스의 상태 근거를 대응할 수 없다면 기존 구성 진단/운영 오류 정책을 사용하고 가까운 다른 State에 붙이지 않는다.

## 6. 위치 교환·쌍 복원의 표시 단위

### 6.1 전방 교환

이동 요청·범위·비용·교환 자격은 기존 Core 미리보기/Move를 그대로 사용한다. 승객 Context를 통째로 전달하며 View가 아군 끝점/발자국/면역을 재판정하지 않는다. PathMovement는 이동 주체의 경로만이다. 승객용 경로·이동 횟수·가속/정찰 발동을 합성하지 않는다.

G2에서 실제 두 종료점 사건이 유일하게 식별되면, Result 내 사건 참조를 묶는 내부 불변 `CombatPlacementBatchPlan`(가칭)을 만든다. 저장하는 것은 원래 사건 참조/인덱스와 표시 완료 연계뿐이며 별도 세계/체크포인트를 저장하지 않는다. 전방 교환은 주체의 기존 걸음 도착 완료 경계에서 두 Final 배치를 함께 적용하고 한 번 Render한다. 승객은 종료점 교환 배치로 표시하고 별도 걷기 경로를 만들지 않는다. 승객의 최종 방향·행동·거리도 자기 Final을 사용한다.

이 배치에 포함한 원래 두 사건은 기존 순차 소비에서 각각 한 번 완료 처리한다. 도착 피해/수집/상태 후속은 쌍 배치가 확정된 뒤 기존 사건 순서로 재생한다. Activation의 두 TargetIds/TargetCells는 큐의 요약이지 추가 이동 명령이 아니다. 단순히 다음 두 Moved·가까운 칸·Magnitude1이라는 이유로 묶지 않는다. G2가 불명확하면 이 그룹 구현을 대기시키고 Core에 출력 대응 확인만 요청한다.

### 6.2 되돌리기

단일/쌍 Returned 모두 Expected/Final이 Field이면 필드 복원이다. 새 Participants는 해당 실제 사건을 대응/검증하는 읽기 정보로 쓰고, scalar와 Participants를 서로 다른 상태로 적용하지 않는다. 단일은 기존 즉시 ShowReturn을 유지하고 쌍은 두 Final을 함께 반영해 Render한다. 역방향 걷기·도착 효과·예비 영역 정리·표시 풀 반환은 없다.

관련 없는 TraitState/피해/장비 사건을 넘어 사실을 한꺼번에 끌어오거나 Core 순서를 재정렬하지 않는다. 실제 복원 사건 구간과 상태 후속 관계가 확인된 경우만 배치한다. 새 표시 묶음은 기존 Player/generation·완료/실패·Cancel/Release를 따른다. 표시 완료 전 Gate 해제/acknowledgment를 앞당기지 않는다. 선택/hover는 같은 EntityId를 유지하고 Core에서 유효하지 않게 된 대상만 기존 정책으로 정리한다.

### 6.3 효과·귀환·최종 상태

RemovedEffects는 실제로 역적용된 기여분의 요약이다. HUD/공격력/흔적은 ordered full facts와 source deactivation으로 갱신하며 요약을 보고 한 번 더 제거하지 않는다. MovementEffectUses/DamageExecutions는 Core 검증 증거이므로 새 재생 스트림으로 등록하지 않는다.

실제 Hand/EnemyWaiting 귀환은 Returned 뒤 cleanup TraitState, 필요하면 장비 사건이 더 이어진다. 각 Final 전체를 적용하고 마지막에는 공개 Buffer로 맞춘다. 처음 Returned만 보고 상태/장비가 최종이라고 고정하지 않는다. 아이템 환불·수량·골드·밤 폐기·HP/최대HP·Hunter 보상·가속/재생 진행 초기화는 Core 계산을 표시한다. Main이 Field에 남는 밤에는 예비 영역 정리를 View가 추가하지 않는다.

## 7. 입력 최신성과 실제 사용 확정

Tracker/Facts/UndoInput의 현재 SourceRevision·battle·turn·result·actor 대조를 유지한다. 원래 이동자와 다른 유닛이 효과를 사용해도 QueryRevision이 바뀌면 기존 Sync에서 응답을 무효화/재조회한다. revision·결과 순번을 MoveCommandNumber로 바꾸지 않는다.

양수 피해가 보호막으로 전부 흡수돼도 Core가 사용 확정한 이동은 취소 불가다. 억제 출처·0입력·미리보기·거절·다른 체크포인트를 View가 추론하지 않는다. 다른 유닛의 이동을 다시 취소했다고 이전 이동의 잠금을 풀지 않는다. 일시 점유의 해제 후 같은 토큰이 다시 허용되는 것도 새 Core 답을 따른다. 새 히스토리·유닛별 토큰 표·추가 Tick/이벤트 구독은 없다. 기존 버튼/R키/전경 입력 차단/튜토리얼/대기/Flow·Job 경계는 바꾸지 않는다.

## 8. 조립 시연 v3

기존 조립 도구를 확장한다. 특성별 시나리오 실행기·실제 규칙 시뮬레이터를 만들지 않는다.

- 저장 버전3을 추가하고 v1→기존 AttackGrant 이전→v2→v3을 사본에서 명시 변환한다. 기존 유닛/묶음/블록/Source ID·에셋 GUID·시간·참조를 보존한다. 기존 v2 약점 노출은 timed/이동 식별 없음으로 이전한다. 기존 enum 번호0~7을 유지하고 새 블록을 뒤에 추가한다. SerializeReference는 없다.
- Effect 입력에 노출 수명과 optional Movement 식별을 추가한다. 실제 Core enum/불변 타입으로 변환하고 UI에는 그 키에 유효한 필드만 보여 준다. timed는 양수 횟수, untimed는 null이다. 무기한을0턴으로 직렬화하지 않는다. stable Source ID의 대상/키/Origin/Movement 변경은 명시 제거 전 거절한다.
- 일반 `FieldPlacement`(가칭) 블록에 한/두 유닛의 명시 최종 칸/회전과 필요한 공개 사건 값을 입력한다. 기대 사실은 현재 ledger에서 가져오고 지정하지 않은 HP/상태/아이템을 보존한다. 기존 Return의 Hand/EnemyWaiting 의미는 유지한다.
- 결과/취소 메타데이터가 필요한 경우 실제 공개 생성자와 검증 조건을 만족시켜야 한다. 공개 API만으로 안전한 합성이 불가능하면 해당 블록/프리셋만 Core 확인 대기로 둔다. Core 검증 우회·내부 Store 주입·가짜 rollback은 금지한다.
- 시연 Source ID는 편집용 식별이고 MovementEffectId는 입력한 공개 사실이다. 자체 battle/turn/명령 숫자를 운영 토큰으로 제출하지 않는다. 자동 턴 만료·접촉 탐색·사용 확정·피해 계산·귀환 정리는 없다.

새 데이터 프리셋은 PositionSwapRestore, ReconExposureGroups(10+20+30→50), ReconMoveRemoval(정확한 기여분 제거·독립 HP 유지), ShieldHistory(아이콘0/비활성 정리), ReserveCleanup(명시 상태 정리와 귀환 순서)다. Native 사용 잠금·실제 아이템 재고 환불·최대HP 정규화의 전체 검증을 합성 예제로 주장하지 않는다. 기존5개 효과 프리셋과 가속/깔끔한 뒷정리/희생/ManualStep는 보존하고, 의미가 달라지는 부분만 Import·가이드에서 명시한다.

## 9. 책임·생성·등록·수명

기준 경로는 `Assets/ProjectL/Script/Systems/`다.

| 영역 | 책임 / 변경 경계 |
|---|---|
| `CombatUnitStatusFactory`, `CombatUnitStatuses`, `CombatStatusDescriptionFormatter` | 공개 유효값·수명 문구·Shield 활성 슬롯 필터 |
| `UnitCardTraitContent`, UnitCardUI, Deck 모델/생산자/UI | 같은 조회의 Recon 설정 전달·공통 표시 |
| `CombatTraitFeedbackProfiles`, `CombatTraitStateEvidence`, `CombatTraitFeedbackFactory` | Recon 등록·출처 목록 근거. 텍스트 풀/외형 입력 재구현 없음 |
| `Playback/Ordering`·`Support`의 placement plan/협력 객체 | 사건 식별·쌍 표시 완료 연결. 재생 전체 정책을 특성 switch로 확장하지 않음 |
| 기존 MovementPlayback/Presentation·ShownFacts/유닛 창구 | 원래 Final의 묶음 적용/표시. 상태 복원 계산 없음 |
| 기존 SceneEvent/Playback Factory | 필요한 협력 객체 생성·주입·소유/해제 연결만 담당 |
| Tracker/Facts/UndoInput·ResultPlayback/ViewRefresh | 원칙적으로 보존·병합 대조/회귀 확인. 실제 결함이 확인된 좁은 범위만 수정 |
| `Dev/TraitPreview/Scenario`·`Editor/CombatPreview`·ScenarioFactory | v3 입력/Import/writer 등록/진단·합성 데이터 |
| View TraitVisual SO/표·기존 WeaknessExposure SO | 표시 이름/정책/발동 줄. 새 효과 표/규칙 숫자 복제 없음 |

DB 경로는 기존 `ViewDataBaseFactory → TraitVisualTable/UnitStatusVisualTable.ExportServices → ViewTableKey/IViewQuery → CombatDisplayTables → Scene Factory/Presenter`다. 신규 서비스·상위 루트 재Export·Build/AddModule은 없다. 새 내부 협력 객체는 해당 Factory에서 생성하고 기존 Presenter/Playback 연결이 보관/Cancel/Release한다.

UI 등록 단위는 기존 UnitHUDLayer/UnitHUD·정보창/슬롯 및 CombatActionPanel이다. 새 UIId·독립 팝업·Button.onClick/UnityEvent 구독을 만들지 않고 기존 UISystem·UIInputBinding/IUIInputReceiver·핀/hover/해제 규약을 따른다. Description Panel/Text 미연결은 그대로 후속이다.

## 10. 구조 검토와 인수 경계

| 기준 | 적용 |
|---|---|
| SSOT | 집계/사용잠금/역적용/수명은 Core, 현재 사실은 Buffer, 재생 중 사실은 기존 ShownFacts, 표현은 View 표, 조회 수명은 Tracker. Participants/요약/증거로 중복 상태 생성 없음 |
| S | typed 효과 변환·상세·입력 최신성·배치 사건 계획·실제 표시·Factory 조립 분리 |
| O | 새 특성은 외형/프로필/근거 등록, 새 시연은 writer/블록 데이터. 일반 공격/카메라/프레임워크 수정 없음 |
| L | 기존 ICombatPresentationPlayer의 비차단/완료·Cancel·Release 및 Returned/단일 이동 계약 보존 |
| I | 표시에는 필요한 사실/배치 요청, 카드에는 같은 Snapshot의 설정, View에는 좁은 Query만 전달. 전체 CoreGroup/Store 전달 없음 |
| D | 기존 DB/입력 포트와 Factory 주입 사용. 일회용 서비스 인터페이스나 루트 resolver 우회 없음 |
| 캡슐화 | 배치 계획은 불변 원래 사건 참조, 실행 상태는 기존 private 수명. 체크포인트/가용성 수정 API 없음 |
| JMH/FW | FW-001~003 등록/Build 중복 없음, FW-004 내부 우회 쓰기 없음, FW-005~009 기존 입력/UI 수명, 표시 자원은 기존 Scope/Handle·자체 풀 계약 유지. Bus/Mesh/Queue 미변경을 실행 통과로 과장하지 않음 |
| if/Tick | nullable 수명·구성/선택/취소의 필요한 분기만 유지. 새 효과 시계·프레임별 출처 재집계·승객 검색/씬 검색 없음. 배치 계획은 Result당 한 번 구성 |
| 독립성 §4.6 | 요약/정책 변경은 해당 formatter/SO, 근거 추가는 profile/evidence, 쌍 표시 변경은 placement 경계, 의존성은 Factory. 공통 사건 완료/Cancel 경계만 이동·끌어오기·희생·추가 공격 회귀 대상으로 명시 |

인수 증거는 문서 정적 대조, 실제 코드 컴파일/에셋 임포트, 합성 Play, 실제 Core 명령/사건 Play를 분리한다. 새 View 테스트 코드는 만들지 않는다. 기존 EditMode/DB 시험이 필요하면 사용자 에디터 검증 조건을 먼저 확인하고, 현재 Play 유지 지시 아래에서는 종료/Refresh/Test Runner를 강행하지 않고 대기로 남긴다. 기존 Core integration StageKey2101 및 성장 Refid 진단은 별도 이슈다.

새 효과 API/카드/시연은 G1 전까지 완료로 기록하지 않는다. 실제 pair 출력 확인은 G2 후다. 사용자 Play·아트·설명 패널은 코드/문서 완료와 별도이며 Core 테스트 수치로 대체하지 않는다. 세부 기대 결과·Phase와 상태는 Task 계획을 따른다.


## 11. 구현 인수 상태

사용자 A~F 일괄 승인 및 병합 확인 지시에 따라 기존 확정 설계로 구현했다. 세부 실행·파일·검증과 제한은 Task 계획 §10이 원본이다. 교환의 승객은 주체 도착과 같은 Render에 반영하고 자기 원래 사건만 완료 처리한다. 단독/쌍 취소는 ordered Field Returned를 표시한다. 합성 Returned는 공개 Result 검증에 맞춘 단독 묶음이며 효과 역적용은 별도 명시 State로 표현한다. Native mixed-effect undo와 실제 사용 잠금의 인수는 사용자 Play 목록에 남긴다.
