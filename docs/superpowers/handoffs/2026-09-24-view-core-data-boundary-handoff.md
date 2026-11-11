# 전투 Core 변경·View–Core 경계 인계 — 2026-11-11 갱신 (StageSelect 런 포기 구현 §5.86·§71, 이전: 재구성 공격 미리보기 칸 §5.85)

> **2026-11-11 StageSelect 런 포기 (사용자 승인으로 Core 포함 구현, 컴파일 미확인, 미전달):** `IRunAbandonService`로 맵에서 현재 런을 정리하고 Stage 흐름으로 로비에 복귀합니다(§5.86·§71).

> **2026-11-11 재구성 공격 미리보기 칸(View 작업에서 Core 수정, 사용자 명시 지시 "Q-RE4 직접 구현", 컴파일 미확인, 미전달):** `AttackHitResult.TargetReconstructed`·`AttackTargetPreview.TargetReconstructs`를 더했다(§5.85). 추가만이고 끝 선택 인자라 기존 호출·시험은 그대로다.

> **2026-11-10 적 전용 특성 재구성·자폭·공포(Core 구현 완료, View 대응 필요, 미전달):** 새 특성 키 셋·상태 하나·사건 하나·발동 넷·거절 이유 하나가 생겼다(§67). 운영 적 유닛에는 아직 붙지 않아 화면에는 나오지 않는다. 기획이 특성을 붙이기 전에 View가 상태 `Reconstructing`(19) 표시 변환부터 등록해야 한다 — 없으면 재구성 중인 유닛이 HUD에 나오는 순간 `CombatEffectFormatters`가 예외를 낸다.

> **2026-11-10 범위 정정·§66 요청 철회:** 사용자가 대상을 공격 시 선행 발동 특성으로 한정했다. 현재 Pull/PullApplied, Vanguard·CleanSweep/AttackGranted는 결과 요약과 사건의 동일 객체 참조·선행 순서로 연결할 수 있어 신규 Core 계약을 선행 조건으로 요구하지 않는다. 앞선 광범위 요청은 미전달 상태에서 철회했다. §65의 별도 발동–상태 연결 요청은 유지한다. 구현은 계속 보류하며 이번 변경은 문서만이다.

> **2026-11-05 특성 발동 → 상태 연결 공개 요청(View 질문, 미전달):** 평가 C1-57. View가 사본 표로 짐작하는 "이 발동이 건 상태"를 Core가 함께 공개해 줄 수 있는지 여쭙니다(§65). 코드 변경 없음.

> **2026-11-02 특성 표시 순서·출처 공개(Core 구현):** 10-29 특성 획득 순서 표시 요청에 답하고 출처를 더했습니다(§5.82). `TraitSettingsSet.Display`가 표시 순서의 키와 출처(유닛 기본·병과 고유·레벨업 획득)를 싣습니다. 전투 정보창의 특성 칸 2개 고정 예외(유닛 클릭 때 멈춤)는 View 몫이며 이 변경과 독립입니다. Core·CoreIntegration 시험 통과(새 실패 0), 커밋 전, 미전달입니다.

> **2026-11-02 특성 설정값 공개(Core 구현):** §64의 특성 부분과 §55 CVI-R1에 답했습니다(§64.6). 보유 카드 `OwnedCharacterStatsSnapshot.TraitSettings`, 전투 `CombatStateData.TraitSettings.For(유닛)`로 등급·증강·전직이 반영된 `TraitSpec`을 공개합니다(§5.81). View 인계는 [특성 설정값 공개 View 인계](2026-11-02-trait-settings-view-handoff.md)이고, 옛 특성별 설명 속성 정리를 View에 요청합니다. Core·CoreIntegration 시험 통과, 커밋 전, 미전달입니다.

> **2026-11-02 Description 수치 요청:** 전투뿐 아니라 보유/덱/상점/선택지 등 모든 씬·UI를 대상으로 합니다(§64). View는 환경·기존 공개 특성 5종·회복을 연결했습니다. 나머지 특성 설정, 아이템·유물 효과, 선택 결과·환경값 공개가 남았습니다. 이번 Description 작업의 Core 코드 변경·외부 전달은 없습니다. **컴파일 미확인.**

> **후속 Core 범위 분석:** 사용자 요청으로 계약/생산자/Buffer 영향을 [수정 범위 분석](../specs/2026-11-02-description-core-scope-analysis.md)에 정리했습니다(§64.5). 특성 설정 공통화가 가장 크며, 옛 강화 상품은 현재 비운영입니다. 이번 요청은 분석이고 Core 구현 승인이 아닙니다.

> **2026-11-02 최신 변경:** 훈련 선택지에 지금·다음 등급 수치를 더했습니다(§5.79, 사용자 지시). 잠시 넣었던 세 장 뽑기 `TrainingOffers`(§5.78)는 지웠습니다. 인계 문서의 `TrainingChoices`를 그대로 씁니다. 전달용 예고는 [훈련소 카드 세 장](2026-11-02-training-three-offers-cheon-share-Hojin.md)입니다. **컴파일 미확인**, 천명님 미전달입니다.

> **2026-11-02 최신 병합 반영:** 2103abe의 구조물 공격력 표시와 현재 UI의 흰색 병과 아이콘·정보창 레이아웃을 함께 유지했습니다. 중복된 구조물 조회 함수·기존 시험 fixture·문서 기록을 통합했습니다. 새 시험 작성·MSBuild·Unity 실행은 하지 않았으며 **컴파일 미확인**입니다.

> **2026-11-02 병합 반영:** 6ef36e3의 Night 배경 정리와 현재 브랜치의 RockfallTunnelBoom 낙석 연출 위치를 함께 보존했습니다. 기존 §58~§61 기록을 유지하고 단독 행동 ShieldReset 기록은 §62로 정리했습니다. 이번 병합의 확인은 프리팹 연결·문서 참조 정적 검사이며, 컴파일·재생은 미확인입니다.

> **2026-11-02 최신 변경:** 단독 행동의 자기 진영 종료 조건·주기를 사용자 지시로 수정했습니다(§5.76·§62). 전달용 요약은 [단독 행동 ShieldReset Core 변경](2026-11-02-solo-action-shield-reset-core-changes.md)입니다. **컴파일 미확인** — 사용자 Unity 확인 대기입니다. 앞선 새 시험 작성·MSBuild 실행은 작업 원칙을 지키지 못한 이력이며, 실행 사실은 §62.2에 보존하고 정정은 §62.3을 따릅니다.

> **2026-10-26 현재 소비 구조 인계는 §39, 새 Core 행동 소비 계약은 §38을 따른다(미전달).** V4-n과 V5-a/b/c는 각각 사용자 컴파일·기존 시험·Unity 정상 보고로 인수했고 V5-d 통합 정리를 마쳤다. 추가 Core API 요청/규칙·Buffer 변경은 없다. 과거 미확인과 예전 요청 상태는 각 시점 기록으로 보존하며 현재 결과/한계는 5라운드 계획 §10에 둔다.

> View 작업에서 DB-SO를 새 Framework/Systems 구조에 맞게 재구성한다. 이 문서는 그중 **Core 작업에 영향을 주는 내용**과 **Core 작업자에게 확인받을 사항**을 전달한다.
> 상세 설계는 [DB-SO 재구성 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md)에, 옛 에셋 값은 [고아 에셋 인벤토리](../specs/2026-09-24-view-db-orphan-asset-inventory.md)에 있다.

- 작성일: 2026-09-24 (최종 갱신 2026-11-11, 재구성 공격 미리보기 칸 §5.85)
- 작성: View 작업
- **2026-10-05 최신 Core 요청은 [별도 요청 문서](2026-10-05-combat-core-requests-phase6b.md)가 원본이다.** `e0b02cc7` 병합본과 골격 6B F1 작업 기준으로 남은 요청·담당·검증 조건을 정리했다. 아래 예전 요청의 해결 여부를 현재 미완료 상태로 읽지 않는다. 외부 전달·답변은 아직 확인하지 않았다.
- 기준 브랜치: `develop`
- 상태: **공유 및 확인 요청.** §4·§5는 View 작업에서 구현을 마쳤다(2026-09-24, Phase A~C). §6(Buffer B1)은 원래 Core 작업자에게 요청하려 했으나, **사용자 지시로 View 작업에서 구현했다(2026-09-25, Phase D).** Core 작업자는 그 변경을 확인해 주세요.
- 2026-09-30 추가: **§13은 전투 Flow Phase 0의 전달용 요청이며 전달·답변·합의는 미확인이다.** 기존 DB-SO 구현 내역과 구분한다. 이번 확인 브랜치는 `feature/Yonggeun/Stage`, HEAD `3a2b6777`이다.

### StageSelect·상점·아티팩트·회복·로비 기록 위치 (2026-10-13)

> 2026-10-11 병합 `9d4c76e5`에서 이 작업의 일부 절이 빠졌다가 2026-10-13에 복원됐다. 다른 작업자가 이미 쓰는 번호는 바꾸지 않고, 빠진 절은 뒤 번호로 붙였다. 예전 문서나 사본의 "원래 번호"는 아래 표로 찾는다. 각 항목의 상태 원본은 해당 절이다.

| 주제 | 현재 위치 | 원래 번호 | 상세 원본 |
|---|---|---|---|
| 아티팩트 보드 처음 크기 설정 | §5.7 | 같음 | [아티팩트 팝업 계획](../plans/2026-10-05-artifact-popup-plan-Hojin.md) |
| LV8A 기본 출발 Buffer 조립 | §5.10 | (10-13 신규) | [로비 계획 §14.10](../plans/2026-10-11-lobby-plan-Hojin.md#310-lv8-기본-출발-lv8a와-선택-덱-출발-lv8b) |
| 회복 성배 에셋 정합 | §5.22 | (10-13 신규) | 해당 절 |
| 적 AI 배지 보유 대상 공격 선택 확인 요청 | §13.17(§5.12는 이동 안내) | (10-13 신규) | [적 AI 배지 공격 확인 요청](2026-10-13-combat-core-requests-Hojin.md) |
| StageSelect P7 아티팩트 번호 계약 | §5.13 | 같음 | [P7 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계) |
| 상점 동작 확인용 InGame 임시값 | §5.14 | §5.8 | [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md) S1 |
| StageSelect 입력 Buffer·등록 뼈대 | §5.15 | §5.9 | [SA 계획](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬) P4 |
| StageSelect 공용 오류 입력 정리 | §5.16 | §5.10 | [공용 오류 변경 기록](2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md) |
| 아티팩트 UI 계약 교체 | §5.17 | §5.11 | [SA 계획](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬) P3.10 |
| StageSelect P5/P6 Core 결과 계약 | §5.18 | §5.12 | [P56 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md) |
| V4 회복 입력 Buffer 공용 등록 | §5.19 | (복원 때 추가) | [V4 Core 인계서](2026-10-09-recovery-core-handoff-Hojin.md#4부-v4-회복-입력-buffer--core-인계) |
| 아티팩트 DB·Buffer 이전 | §5.20 | (복원 때 추가) | [아티팩트 DB 이전 인계](2026-09-28-artifact-db-migration-handoff-Hojin.md) |
| 회복 UI·비전투 이벤트 Core 계약 | §13.11 | (10-12 신규) | [회복 Core 계약 요청](2026-10-09-recovery-core-handoff-Hojin.md#2부-회복-이벤트-ui-연결을-위한-core-계약-요청) |
| 로비 Core 계약 | §13.12 | (10-11 신규) | [로비 설계 §10](../specs/2026-10-11-lobby-design-Hojin.md) |
| StageSelect 처리 번호 기록 요청(H1~H4) | §13.13 | 같음 | [처리 번호 요청](2026-10-06-stageselect-core-requests-Hojin.md#4부-stageselect-요청-처리-번호-기록-요청-view--core) |
| Run seed 도입 요청 | §13.14 | §13.10 | [Run seed 요청](2026-10-06-stageselect-core-requests-Hojin.md#3부-run-seed-도입-요청-view--core) |
| 회복 결과 반환 요청(대체됨) | §13.15 | §13.11 | [회복 결과 요청](2026-10-09-recovery-core-handoff-Hojin.md) |
| 통합 기획서와 상점·회복·맵 규칙 차이 | §13.16 | §13.12 | [기획서 차이 요청](2026-10-06-stageselect-core-requests-Hojin.md#2부-통합-기획서와-상점회복스테이지-규칙-차이-view--core기획-확인-요청) |
| 스테이지 선택 → 전투 진입 요청 | 이 문서에 없음 | – | [전투 진입 Core 요청](2026-10-06-stageselect-core-requests-Hojin.md) |

## 1. 요약

| # | 내용 | 성격 | Core 확인 필요 |
|---|---|---|---|
| 1 | 데이터 흐름: InGameGroup → Core 시스템 → Buffer → View | 공유 | ✅ |
| 2 | InGame Data는 순수 C#. SO는 기획 입력 계층 | 공유 | – |
| 3 | InGameGroup 테이블을 View 작업에서 복구·구현 | 공유 | API 확인 |
| 4 | View 작업에서 Core 영역에 손댄 내역 (using 3줄, 데이터 API 변경, Buffer B1) | 공유 | ✅ |
| 5 | **Buffer B1 구현 확인 요청** (StageProgressData, StageGenerator). View 작업에서 구현함 | **확인 요청** | ✅ |
| 6 | 공용 타입은 옛 Core.Contracts 재사용 | 공유 | 정리 계획 |
| 7 | Core 데이터에서 빠지는 View 필드 | 공유 | – |
| 8 | 데이터 주의사항 | 정보 | – |
| 9 | 옛 에셋 보존(2026-10-13: 삭제 완료) | 정보 | – |
| 10 | 전투 필드 Buffer(B2) | 향후 협의 | ✅ |
| 11 | 전투 Flow 소유 경계, 처리 서비스 네 개, F3 결과 읽기·등록·보존, F2 종료 수명(§13) | 전달용 요청 준비 | ✅ |

## 2. 데이터 흐름

새 Core는 기존 `CoreCaculator`(Contracts/Snapshot)가 아니라 **새 Framework/Systems 위의 시스템**으로 동작한다는 전제로 설계했다.

```text
InGameGroup (규칙) ──InitData──► Core 시스템 (InGameCoreSystemGroup)
                                   │  Table을 바로 참조 (변환 계층 없음)
                                   ▼ WriteSink
                                 Buffer (런타임 상태)
                                   │ ReadSink
ViewGroup (표현) ──InitData──►  View 시스템 / UI
```

- **Core는 InGameGroup Table을 바로 참조한다.** 현재 `StageGeneratorSystem`처럼 `InitData(IDataBaseQueryContext)`로 `InGameQuery`를 받는다.
- 예전에는 View(`ProjectL.Unity`의 `FieldDefinitionAdapter`, `StageEntryDataBuilder`)가 SO를 Core Spec으로 바꿔 넣었다. 이제 **View는 Core에 데이터를 넣지 않는다.**
- **View는 InGameGroup을 읽지 않는다.** View에 필요한 규칙 값(크기, 위치, 노드 종류 등)은 **Core가 Buffer에 써 주어야 한다.**

## 3. InGame Data 규칙

| # | 규칙 |
|---|---|
| P1 | InGame Data(Core가 받는 것)는 **순수 C#만** 사용한다. `GameObject`, `Vector2Int`, `Color`, SO 참조 등 UnityEngine 타입 금지 |
| P2 | SO는 **기획 입력 계층**이다. Item SO가 직렬화 필드를 갖고, `GetData()`가 순수 C# 불변 Data를 **매번 새로** 만들어 반환한다 |
| P3 | SO끼리의 참조는 입력 계층에서만 허용한다. `GetData()`에서 **키**로 바꾼다. Core가 받는 Data에는 SO 참조가 없다 |

Core 쪽에서 체감하는 변화:

- Table에서 꺼낸 Data는 **불변**이고 Unity 타입이 없다.
- 다른 데이터는 키로 가리킨다. 예: `CombatStageDetail.EnemyLoadoutKey` → `EnemyLoadoutTable.Get(key)`

## 4. InGameGroup 테이블 (View 작업에서 구현)

옛 SO 데이터를 복구하는 방식으로 아래 테이블을 만든다. 상세 필드와 복구 값은 설계 §5에 있다.

| 테이블 | 키 | 주요 필드 | 상태 |
|---|---|---|---|
| StageMapTable | MapKey (int) | depth, width, 연결 확률·알고리즘·가중치, seed | 기존. P2 적용 |
| StageTable (StageRoot) | StageKey (int) | mapKey, mapRole, stageType, 상세 Data | 기존. P2 적용 |
| (상세) CombatStageDetail | StageRoot가 보유 | timelineSeed, fieldId, rosterKey, enemyLoadoutKey, HQ 키·위치·회전, 초기 배치, 구조물, 레일웨이 | 기존 SO 확장 |
| FieldTable | FieldId (string) | 열·행, reserveCapacity, 양측 배치 깊이 | 신규 |
| UnitTable | EntityKey | 소유 가능, footprint, 체력, 이동, 공격(패턴은 `CellCoord[]`), 우선순위·딜레이, Trait | 신규 |
| StructureTable | EntityKey | footprint, 체력, 피격 가능, 공격력 | 신규 |
| EnemyLoadoutTable | LoadoutKey (int) | (EntityKey, weight)[], spawnCount, seed | 신규 |
| PlayerLoadoutTable | LoadoutKey (int) | (EntityKey, count)[], 시작 손패 수, seed | 신규 |
| UnitRosterTable | RosterKey (int) | EntityKey[] | 신규 |

- **Trait 5종**을 구현한다: Focus, MoveAttackDiscount, PiercingFalloff, ThornField, Knockback. 필드와 검증은 옛 Contracts `*TraitSpec`과 같다.
- Loadout과 Roster의 키는 옛 에셋에 없던 값이라 새로 부여한다: Enemy 1~5, Player 1, Roster 1.
- 쿼리 인터페이스는 기존 패턴(`IXxxTableQuery`, `Get`/`TryGet`)을 따르고 `IInGameQuery`에 추가한다.

## 5. View 작업에서 Core 영역에 손댄 내역

규칙상 View 작업은 Core 로직을 수정하지 않는다. 아래는 DB 재구성 과정에서 **Core 코드 또는 Core가 쓰는 데이터 API에 닿은 변경 전체**다. 5.1·5.2는 로직 변경이 없다. **5.3(Buffer B1)은 사용자 지시로 한 Core 로직 변경이다.**

> 2026-10-13: §5.13~§5.18은 병합 `9d4c76e5`에서 빠졌다가 복원한 절이다(원래 §5.8~§5.13). 번호 대응은 문서 앞의 기록 위치 표를 본다.

### 5.1 Core 파일을 직접 수정한 것 (Phase A)

| 파일 | 변경 | 이유 |
|---|---|---|
| `RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs` | `using ProjectL.Systems.DataBaseSystem.Data.Common;` 1줄 추가 | `StageType`/`StageMapRole`이 `Data/Common/`으로 이동해 컴파일에 필요 |
| `.../StageSystem/StageServiceCollection.cs` | 같은 using 1줄 추가 | 〃 |
| `.../StageSystem/StageServiceManager.cs` | 같은 using 1줄 추가 | 〃 |

Phase A에서는 `CoreCaculator`(Contracts/Domain/Application)를 수정하지 않았다. `BufferSystems`는 Phase D에서 수정했다(5.3). 이후 §5.18(P5/P6)에서 `Contracts/Results/ChapterProgressionResult`에 `UnsupportedStageType`을 추가했다.

### 5.2 Core가 쓰는 데이터 API가 바뀐 것 (DB 폴더에서 수정)

| # | 변경 | Core 영향 |
|---|---|---|
| M1 | `StageType`, `StageMapRole` 위치가 `Data.InGame.Table.Stage.Item` → `DataBaseSystem.Data.Common` | using만 바뀜(5.1) |
| M2 | `StageMapData`가 순수 불변 클래스가 됨(값은 `StageMapDataSO`에서 입력) | 프로퍼티 이름·타입 동일. 수정 불필요 |
| M3 | `StageRootData`가 순수 불변 클래스가 됨. **`StageData` 프로퍼티 타입: `StageBaseData`(SO) → `IStageData`(순수)** | `.StageData.StageType`, `.StageData.Validate()`는 그대로 동작 |
| M4 | `StageBaseData.GetData()`가 SO 자신이 아니라 순수 상세 Data(`CombatStageDetail`/`ShopStageDetail`/`EventStageDetail`)를 반환. SO 쪽 `Validate()`는 제거 | 상세 SO를 직접 다루던 코드가 있다면 영향 |
| – | `CombatStageDetail`에 전투 필드 추가. **Field·Roster가 없으면 검증에서 거부**(옛 StageDefinitionSO 규칙과 동일) | 빈 전투 스테이지 데이터가 더 이상 통과하지 않음 |
| – | `IInGameQuery`에 테이블 6개 추가(Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster) | 추가만 |
| – | `IInGameQuery.StageViewPropTableQuery` 제거 → `IViewQuery`로 이동 | InGame 쪽에서 쓰던 곳 없음 |
| – | `IDataBaseQueryContext`에 `IHasViewQuery`(ViewQuery) 추가 | `InitData(IDataBaseQueryContext)` 구현체 수정 불필요 |
| – | `DataBaseFactory.Create`가 Catalog에 ViewGroup도 요구 | Phase E에서 InGameGroup·ViewGroup 에셋을 만들고 Catalog에 등록함 |

**확인 요청:** M3/M4로 Core가 받는 스테이지 상세가 SO에서 순수 Data로 바뀌었다. 진행 중인 Core 작업에서 상세 SO를 직접 쓰고 있다면 알려 주세요.

### 5.3 Buffer B1 (Phase D, 사용자 지시로 구현)

| 파일 | 변경 |
|---|---|
| `RuntimeSystem/BufferSystems/Stage/StageProgressData.cs` | `StageType StageType`, `StageMapRole MapRole` 프로퍼티 추가. 생성자 끝에 `stageType = default`, `mapRole = default` 인자 추가 |
| `RuntimeSystem/BufferSystems/Stage/StageProgressBuffer.cs` | `SetClear`에서 Data를 다시 만들 때 `StageType`, `MapRole`도 복사 |
| `RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs` | 스테이지 수집 루프에서 `stageKey → StageRootData` 사전을 함께 만들고, 노드를 기록할 때 해당 스테이지의 `StageType`, `MapRole`을 전달(시작 노드는 기본값) |

상세와 이유는 §6.

### 5.4 전투 준비 Job 병합 통합 (2026-09-30, 사용자 지시)

`1a51234c` 병합 시 `CharacterBattlePreparationTests.cs`의 `new CombatJob(...)`를 `new CombatPrepareJob(...)`로 수정했다. 검증 내용은 그대로 두고 현재 View 측 타입 이름에 맞춘 변경이다. Core 서비스·계산기 로직은 들어온 커밋을 유지한다. 준비 API 호출 연결과 충돌 해결은 View의 Job·Plan에서 수행했다. 상세 범위와 검증 결과는 [전투 진행 계획 §6.5](../plans/2026-09-30-combat-flow-skeleton-plan.md#65-1a51234c-병합-통합-기록-2026-09-30)를 따른다. 담당자 간 소유 경계 합의 완료로 간주하지 않는다.

### 5.5 진행 테스트의 InGame 그룹 조립 (2026-10-02, 사용자 결정)

View Phase C에서 `InGameSystemFactory`의 View 그룹에 `CombatViewSystem`이 들어갔다. 이 시스템은 `PostBindService`에서 Global(입력 Layer·로딩 등록)과 입력 수신 슬롯을 받는다. 그래서 Global 없이 Buffer·Job·InGame만 조립하는 `ChapterGraphPreparationTests`의 `Prepare(withJobRuntime: true)`가 바인딩에서 실패했다(`Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry`, `Runtime_DefeatThenExplicitNewRun_RebuildsGraphAndPreservesArtifact`). 사용자 결정으로 그 조립만 바꿨다. InGame 그룹을 `InGameCoreSystemFactory` + 빈 `InGameViewSystemGroup`으로 직접 만든다. 검증 내용과 Core 코드는 그대로다. 앞으로 InGame 그룹 전체를 Global 없이 조립하는 테스트를 만들 때도 같은 방식이 필요하다. 상세는 [View 계획 §7.11](../plans/2026-10-01-combat-view-plan.md#711-phase-c-실행-기록-2026-10-02-코드-완료검증-대기).

### 5.6 View A2 — 공격 지정 방식 DB 제공 (2026-10-04, 미전달)

사용자 요청으로 View 계획 A2의 데이터·조회·에셋을 구현했다. **Core 생산 코드는 수정하지 않았다.** 구현·검증 상태의 원본은 [View 계획 §7.19](../plans/2026-10-01-combat-view-plan.md#719-phase-a2-실행-기록-2026-10-04-코드-완료unity-검증-대기)다.

**새 데이터 API:**

- `Data.InGame.Table.Targeting.Item.TargetingMethodData`: `MethodKey`, `Shape`, `ChoiceMode`, `Splash`, `SplashPercent`를 가진 순수 불변 데이터. SO·TableSO는 `Data/InGame/Table/Targeting/`에 있다.
- `IInGameQuery.TargetingMethodTableQuery`: `GetTargetingMethod(int)`, `TryGetTargetingMethod(int, out TargetingMethodData)`, `GetAllTargetingMethods()`. 서비스 키는 `InGameTableKey.TargetingMethod`다.
- `UnitDataSO.targetingMethod` 참조를 `GetData()`에서 `UnitData.TargetingMethodKey`로 변환한다. `UnitData` 생성자의 마지막에 **필수 `int targetingMethodKey`**가 추가됐다. 옛 공격 필드는 유지한다.
- `AttackPower > 0`이면 양수 키 필수, 공격력이 0이면 미지정 키 0 허용, 음수 키 거부. 양수 키는 공격력과 관계없이 그룹 내 테이블에 있어야 한다. Factory 조립 시 한 번 교차 검사한다.
- 데이터 검증은 `TargetingMethodSpec`의 enum·선택 방식/추가 범위 조합 규칙을 재사용한다. 저작 규칙으로 `Splash.None`이면 0, 그 외에는 1 이상의 비율을 요구하며 상한은 없다. 생성한 Spec을 보관하거나 전투에 전달하지는 않는다.

| MethodKey | Shape | ChoiceMode | Splash / Percent | 연결 유닛 |
|---|---|---|---|---|
| 1 | RadialSquare | Single | None / 0 | Akimbo, Demon, Sniper, MainCharacter |
| 2 | Fan | Direction | None / 0 | ShotGun |

`InGameGroup/Targeting`에 두 방식과 테이블 에셋을 추가하고 InGameGroup에 등록했다. **Ballista·CactusCannon·Gravekeeper용 3~5는 해당 유닛 에셋을 만들 때 추가**한다. 현재 테이블에는 1·2만 있으며 사거리는 계속 `UnitData.AttackRange`가 소유한다.

**Core 후속 요청:** `DatabaseBattleInitializationSource`·`BattleDefinitionAdapter`에서 유닛의 키로 지정 방식 데이터를 조회하여 `TargetingMethodSpec`을 조립한다. 옛 `AppointMethod`/`AttackTargetMode`를 새 enum으로 캐스팅하거나 추정하지 않는다. 나머지 준비 계약을 포함해 기본 DB source를 완성한 뒤 T1/G7 예외를 제거하고, `useDevBattle=false` 실제 데이터로 진입·공격·미리보기까지 검증한다. A2의 테이블 생성 성공은 운영 전투 준비 성공이 아니다. View는 계속 Buffer의 `AttackAims`를 사용한다.

**기존 Core 관련 테스트의 기계적 변경:**

- `ChapterGraphPreparationTests`: 수동 InGameGroup에 빈 지정 방식 테이블 추가.
- `CharacterRosterDatabase`, `BattleDefinitionAdapterTests`: 새 UnitData 생성자 인자 추가. Ballista의 3은 해당 단위 테스트용 키이며 DB 에셋 등록을 의미하지 않는다.
- 위 두 파일과 `CoreStageCombatCycleTests`, `CombatFlowIntegrationTests`, `CombatEntryIntegrationTests`: `IInGameQuery` 테스트 대역에 새 프로퍼티 추가. 해당 테스트의 현재 경로에서 사용하지 않는 조회는 `NotSupportedException`으로 명시했다. Core가 새 조회를 소비할 때 대역도 실제 지정 방식 데이터를 공급하도록 갱신해야 한다.
- 기존 Core 검증 내용은 유지했다. `InGameDataTableTests`와 `DataBaseAssetTests`에는 새 조회·참조 누락·키 매핑 검증을 추가했다.

### 5.7 아티팩트 보드 처음 크기 설정 테이블 (2026-10-05, 사용자 지시, 미전달)

InGame DB에 설정형 테이블 `ArtifactBoardConfig`(항목 1개, 기본 4×4)를 추가했다([아티팩트 팝업 계획](../plans/2026-10-05-artifact-popup-plan-Hojin.md) Phase A, D6·D7). `IInGameQuery`에는 넣지 않고 별도 인터페이스 `IHasArtifactBoardConfig`로 노출하므로 Core 서비스와 테스트용 `IInGameQuery` 구현체는 바뀌지 않는다. 실행 계층인 `StageLifeCycle`이 이 값으로 StageSelect 진입 때 `InitializeBoard`를 한 번 부른다(이미 초기화돼 있으면 Core가 `false`를 돌려주고 상태를 보존).

| 파일 | 변경 | 이유 |
|---|---|---|
| `Assets/Tests/EditMode/Framework/ChapterGraphPreparationTests.cs` | `Prepare`의 InGame 그룹 테이블 목록에 `ArtifactBoardConfigDataTableSO`(기본 항목 1개) 한 줄 추가 | `InGameDataBaseFactory`가 그룹의 모든 테이블을 필수로 찾으므로(`GetDataBase`) 목록에 없으면 조립이 실패한다. 검증 내용은 그대로다 |

앞으로 InGame 그룹을 테이블 목록으로 직접 조립하는 테스트는 이 테이블도 포함해야 한다. 보드 크기가 늘어나는 보상 처리(`ExpandBoard`)와 Run 재시작 시 초기화 정책은 이번 변경에 포함되지 않는다.

### 5.8 User-authorized Run Gold integration (2026-10-11, not sent)

The user explicitly authorized the existing Core/Buffer migration and then the minimal View compatibility work. The Core candidate is now integrated into the current checkout with newer terrain/piercing work preserved. Core manual-draw command/calculation/settlement contracts and combat-local Gold are removed; `CurrencyBuffer.Amount` is the single Run Gold balance. Initial-hand selection and unrelated gameplay behavior remain intact. View reads the existing currency sink and no longer submits manual draws. SO/DB schema and authored assets remain unchanged.

Actual Unity compilation succeeded; all 1,961 Core tests passed. The full EditMode suite still has 39 failures and 1 inconclusive result, and Play Mode smoke checks are pending. Exact scope, independent review, and evidence are in the [Run Gold View compatibility handoff](2026-10-11-run-gold-view-compatibility.md). This note records the authorized Core exception; it is not an external message or a claim of whole-project completion.

### 5.9 환경 SO 배치 입력과 토네이도 목록 (2026-10-12, 사용자 지시, 미전달)

**Current status (2026-10-12):** The original delivery request below is resolved by user-authorized Phase 2; see section 5.9.2. The original SO-only scope and Phase 1 pending list are historical checkpoints.

사용자 요청으로 DB 등록 없이 네 묶음(정적 Terrain, 토네이도·위치 보정 Terrain, Obstacle, Object)의 정의 SO와 CombatStageData 배치 입력을 작성했다. 설계·검증 원본은 [환경 SO 설계](../specs/2026-10-12-environment-so-authoring-design.md)와 [Task 계획 Phase 2](../plans/2026-10-12-environment-so-authoring-plan.md)이다.

Core 파일 변경은 없다. DB 폴더의 CombatStageDetail에 `IReadOnlyList<TornadoSourceFact> TornadoPlacements`와 마지막 선택 생성자 인자 `tornadoPlacements = null`을 추가했다. 기본은 빈 목록이며 복사·비활성/null 거절·다른 Terrain/Object와 ID 중복 검증을 한다. 기존 생성자 호출은 유지된다. 기존 Presence/Static/Obstacle/Object 계약으로 SO 입력을 변환한다. SO 환경 검증은 기존 StageEnvironmentAdapter와 CellEnvironmentQuery를 호출하며 Buffer나 운영 상태를 쓰지 않는다.

Terrain/Object 회전은 사용자 승인에 따라 View의 StageEnvironmentVisualDataSO에 stageKey/sourceId별 외형 값으로 분리했다. 좌표·규칙 정의 SO 참조를 복제하지 않으며 장애물 회전은 기존 계약 그대로 규칙 쪽에 둔다. 런타임 외형 소비·Query/Catalog 등록은 이번 범위가 아니다.

**Core 요청(미전달·미구현):** `CombatSystem/BattleDefinitionAdapter.Read`의 `StageEnvironmentAdapter.Read` 호출에서 `tornadoPlacements: combat.TornadoPlacements`를 전달해 달라. 현재 Object 뒤에서 `wallCells: combat.WallCells`만 전달하여 SO에 입력한 토네이도가 운영 준비 정의에 포함되지 않는다. StageEnvironmentAdapter는 이미 해당 선택 인자를 받으므로 새로운 계산 규칙은 필요하지 않다. 인수 시 초기 토네이도의 ID·좌표·knownTo·첫 발동 시점이 BattleInitialization.Environment/EnvironmentBindings 및 준비 Buffer에 보존되는지 확인한다. 이 요청이 반영되기 전 SO 저작 완료를 운영 토네이도 연결 완료로 보지 않는다.

#### 5.9.1 User-authorized configurable timing — Phase 1 complete (2026-10-12)

The user identified as the Core programmer, approved the configurable tornado timing design (Git history: `2026-10-12-tornado-repeat-interval-design.md`), and explicitly requested the Task plan and Phase 1 (Git history: `2026-10-12-tornado-repeat-interval-plan.md`). This is the authorization for the following Core/Buffer changes; the earlier paragraph describing no Core edits refers to the original SO-authoring work.

`TornadoSourceFact` now stores a positive immutable `RepeatIntervalUnitTurns`, accepted as a trailing optional constructor argument. Existing calls retain interval 3 and the existing positional `active` argument. Activation, visibility copying, and expected-state equality preserve the setting. Runtime activation and timeline forecast continue sharing `AfterActivation`, with checked due-counter addition. `CombatStateBuffer.ValidateEnvironmentChange` rejects changes to the captured interval before transaction installation; it does not recompute scheduling or RNG rules.

Unity compilation passed. Full Core EditMode: **2,150/2,150 passed**. Affected Framework publication fixtures: **67/67 passed**. Focused tornado tests: **33/33 passed**. Behavioral RED and GREEN evidence, commands, and independent Phase review are recorded in the Task plan and `Logs/TornadoRepeat-20261012/`. This does not claim that the unrelated failures observed during the Stage merge are fixed.

**Pending at the Phase 1 checkpoint (now resolved in section 5.9.2):** the repeat-interval SO field, pure definition conversion, explicit asset default, and the `BattleDefinitionAdapter` tornado-list argument requested above. Phase 1 alone did not expose repeat timing in the Inspector or complete production SO delivery. No commits, pushes, or external messages were made.

#### 5.9.2 User-authorized SO timing and Core delivery — Phase 2 complete (2026-10-12)

The user explicitly requested Phase 2 and completion of SO repeat configuration and Core delivery. `TerrainDefinitionDataSO` now exposes `repeatIntervalUnitTurns`, defaulting to the existing Core default 3. Pure `TerrainDefinitionData` captures and validates the setting and passes it into `TornadoSourceFact`. Non-tornado SO conversion supplies zero; omitted pure-data constructor arguments preserve legacy behavior. `BattleDefinitionAdapter.Read` now forwards `combat.TornadoPlacements` to the existing StageEnvironmentAdapter. This resolves the original section 5.9 Core request under explicit authorization.

Configure **First Activation After Unit Turns** and **Repeat Interval Unit Turns** under the definition SO's **Tornado** section, then reference that definition from the stage's Terrain Placements. First 2 / repeat 5 schedules completed individual unit turns 2, 7, 12, 17. Both owners count, with no night reset. Preparation captures the settings; later SO edits do not mutate active combat. The existing Terrain_Tornado asset remains 3 / 3. No production stage placements or rendering were changed.

Actual Unity compilation and final focused EditMode regression: **183/184 passed**, including **26/26** environment entry/publication and **21/21** DataBaseAssetTests. The sole failure is the pre-existing wall-visibility fixture `EnvironmentStageAuthoringTests.StageSO_AuthorsAllFourBundlesAndPreservesObjectOrder`, unchanged from merge verification. The new full SO-to-default-source-to-prepared-Buffer test and actual legacy YAML missing-field test passed. Independent review found no remaining actionable findings after a test-fixture correction. Exact RED/GREEN evidence and authoring procedure are in the Task plan (Git history: `2026-10-12-tornado-repeat-interval-plan.md#phase-2-evidence--2026-10-12`). Phase 1's full Core result remains 2,150/2,150. No commit, push, external message, or Play Mode visual verification was performed.

### 5.10 연결 Wall 실제 셀 형상 — C1·C2 완료 (2026-10-13, 사용자 명시 지시)

사용자는 그림 유지/Core 수정 설계를 선택하고 Core 작업도 본인 담당이라고 밝힌 뒤 “Phase C1, C2 차례대로 진행해”를 지시했다. 이는 아래 Core 변경의 명시 승인이다. 설계는 연결 Wall 형상 설계 (Git history: `2026-10-13-connected-wall-geometry-design.md`), Task·검증 원본은 C1~C3 계획 (Git history: `2026-10-13-connected-wall-geometry-plan.md`)이다. 당시 C2 리뷰 지점에서 멈췄으며 후속 C3 실제 인수 완료는 §5.10.1을 따른다.

CoreCaculator/Domain/Combat의 `WallGeometry.cs`, `WallGeometryBuilder.cs`, `WallSegmentClipper.cs`를 수정하고 `WallConvexPart.cs`와 .meta를 추가했다. WallRun은 불변 Parts로 실제 연결 셀의 형상을 보관한다. 일반 성분의 셀별 조각에서 바깥 볼록 모서리만 CornerCutTicks를 적용하고 내부 공유 변은 닫힌 분할 경계로 취급한다. Clipper는 조각 중 최초 진입 시각을 정확한 정수 분수로 반환한다. 직선 단일 조각 경로, 원본 SourceIds/Cells/최소 ID, 실제 셀 점유, 외곽 열린 경계 및 다른 성분의 대각선 접촉 금지를 보존했다.

공개 Contracts·SO 입력·Buffer 계약·Factory/서비스 등록·View 코드는 변경하지 않았다. AttackOcclusionResolver와 이동/공격/AI 소비자는 기존 계약으로 성분 전체 증거를 받으며 조각 ID를 발행하지 않는다. WallGeometry/Parts는 기존 facts에서 계산하는 파생 불변값이다(SSOT). 조립·교차·정책 책임 분리(SOLID), Unity 없는 Core 및 기존 명령/발행 경계(FW-004)를 유지한다.

검증: C1 기하 **155/155**(독립 외곽선 기준 37,500개 선분 및 원본 입력 역순 포함), C2 추가 명령/정책 **14/14**, 최종 순수 Core 전체 **2,227/2,227**, 실제 Wall Buffer 발행 fixture **10/10** 통과. C1/C2 부분집합은 전체 Core에 포함된다. 기존 꺾인 Wall 준비 거절 기대는 지원 계약에 맞춰 성공/원본 보존 검사로 변경했다. 현재 소스로 별도 관리형 컴파일 오류 0·새 경고 0이며 기존 CS8632 두 건은 변경 파일 밖에 있다. 에셋·씬은 보존했다. Unity 에디터 컴파일/Test Runner/Native 에셋 로딩/Play를 실행한 증거는 없으며 실제 11개 스테이지 인수는 C3다. 커밋·push·외부 전달 없음.

#### 5.10.1 C3 실제 Unity 인수 완료 (2026-10-13)

Core 파일별 변경, 판정 규칙, 유지 계약과 병합 범위는 [연결 Wall Core 변경사항](2026-10-13-connected-wall-core-changes.md)에 정리했다. 상세 실행 증거 원본은 기존 Phase 계획을 따른다.

사용자 C3 지시로 DataBaseAssetTests 23/23·환경 에셋 15/15를 포함한 Native EditMode 178/178과 Database/Live Boot 재생 1/1을 통과했다. 실제 Catalog로 11개 스테이지를 두 표시 모드에서 준비/표시했고, Live에서는 입력 Buffer와 미리보기 context를 통해 ㄴ자 빈칸 이동 및 Wall 셀 거절을 확인했다. 본부 Y 기대만 현재 SO의 8로 보정했다. production Core/View/에셋 변경 없이 테스트 준비/수명을 보완했다. SO/Combat 씬 보호 509개 파일 해시 보존, 임시 Editor 검증 도구 제거, 커밋·push·외부 전달 없음. 상세 범위·실패 보정·증거는 연결 Wall Phase 계획의 C3 실행 기록이 원본이다.

### 5.11 Ballista C4 직접 공격 그룹 게시 (2026-10-13, 사용자 명시 승인)

사용자가 최소 Core 계약을 이번 작업에서 구현하고 인계 내용을 기록하도록 명시적으로 허용했다. `Contracts/Results/AttackResult.cs`에 `AttackGroup`을 추가하고 `Application/Combat/CombatCommandCalculator.cs`가 기존 직접 공격 그룹을 결과에 게시한다. 피해·벽·감쇠·그룹 발급은 변경하지 않았다. 상세 변경·기존 API 호환·명령 내 그룹 범위·View 소비는 [Ballista Core 공격 그룹 인계](2026-10-13-ballista-core-attack-group-handoff.md), 실행 검증은 [공격 계획 §6.4](../plans/2026-10-13-temporary-attack-presentation-plan.md#64-c4-ballista)가 원본이다. 문서는 미전달이다.

### 5.12 LV8A 기본 출발 입력의 Buffer 조립 (2026-10-13, 사용자 승인)

기본 로드아웃 1로 새 Run 출발을 연결하며 `BufferSystems/Core/BufferSystemGroup.cs`에 새 Lobby 출발 Read/Write Sink 접근자와 생성자 조회만 추가했다. 기존 Core 거래·계산·Run 초기화 본문은 변경하지 않았다. 기존 `IRunStartService.StartNewRun(int)`·`RosterAcquireResult`·Artifact 보존 계약을 재사용한다. View가 출발 의도를 보관하고 Flow/Plan/Job이 호출·결과/배출·씬 전환을 소유한다. 구현·검증 원본은 [로비 계획 §14.10](../plans/2026-10-11-lobby-plan-Hojin.md#310-lv8-기본-출발-lv8a와-선택-덱-출발-lv8b)이다. 신규 Core API 개발이나 외부 담당자 합의 완료를 뜻하지 않는다.

같은 날 사용자 요청으로 공개 Buffer 이름도 `ILobbyViewBufferReadSink/WriteSink`, `LobbyViewBufferKey.Read/Write`, `IBufferQuery.LobbyViewReadSink/WriteSink`로 통일했다. 접근자와 조회 참조만 바꿨으며 Core 거래와 기존 출발 동작은 유지한다.

같은 작업에서 `IBufferQuery`를 감싸는 기존 테스트 대역에 새 접근자만 기계적으로 더했다: `ArtifactEditingServiceTests`(2줄), `ChapterResetSpy`(2줄), `CharacterRosterServiceTests`(`LobbyViewBuffer` Export 1줄). 검증 내용은 바꾸지 않았다.

### 5.22 회복 성배 에셋의 기존 Core 계약 정합 (2026-10-13, 사용자 승인)

사용자 지시로 `InGameGroup/Artifact/Item/Artifact_HealingChalice.asset`(1001)의 대상만 `AllMatchingUnits`에서 `MainCharacter`로, 회복량만 10에서 3으로 수정했다. 원본 계약은 `ArtifactEffectCompiler.ValidateHealingChalice`이며, 같은 계약은 [DB 실행 데이터 요구사항 §11](2026-10-08-db-runtime-data-requirements.md#11-artifacts-and-board-initialization--g19g20)에 정리돼 있다. `StageCleared`·아군·고정 회복·지속 0과 기존 보유 수량·형상·ID·GUID는 유지한다. Unity Editor API로 저장하면서 현행 스키마의 중립 `soulGrowth` 값 0도 직렬화됐다. 임시 저작 스크립트는 저장·재로드 확인 후 제거했다.

Core 계산·서비스·Buffer·Framework·UI·카메라 코드는 수정하지 않았다. View의 InGame DB 직접 조회를 추가하지 않았으며, 기존 배치 상태를 Core가 읽고 승리 정산을 Buffer에 게시하는 경로를 재사용한다. [과거 G9 기록](2026-09-29-character-buffer-task0-baseline.md#g9--db-to-core-artifact-mapping)의 성배 불일치를 이번에 해소한 것이며, 다른 아티팩트의 계약·지원 상태까지 해결한 것은 아니다.

검증용 `ArtifactHealingChaliceIntegrationTests`는 실제 Catalog·기본 `DatabaseBattleInitializationSource`·실제 `Artifacts.Place`를 사용한다. 전투 노드만 고정하며, 공개 Core 서비스로 적 HP 1과 메인 손실 체력을 준비한다. 이후 이동 미리보기/문맥·적 AI 진행·공격·본부 파괴·승리 판정은 실제 전투 명령으로 계산한다. 배치 시 +3, 최대 체력 제한 시 +1, 미배치 시 +0과 효과의 아티팩트/배치 인스턴스/대상 ID, 최종·지속 체력 일치, 중복 정산 방지, 다음 운영 전투의 체력 보존을 검증했다. 완료나 승리를 주입하지 않았고, 전투 밸런스 또는 수동 Game View 인수 검증을 뜻하지 않는다.

수정 전 `target.type` 거절을 재현했고, 수정 후 집중 테스트 **8/8 통과**(실제 배치→승리 통합 3건 포함)를 확인했다. 이를 포함한 관련 회귀는 **956/956 통과**, 실패·건너뜀·불확정 0, Unity 종료 코드 0이다. 실제 DB 에셋 교차 검증 25건과 아티팩트 UI 배치·로비 출발·덱·상점·StageSelect 전투 진입·카메라의 별도 Play 진입 smoke도 포함한다. 컴파일 오류 0이며, 증거는 `.superpowers/sdd/2026-10-13-chalice-contract/`의 `authoring-fixed`·`green`·`regression`에 보관한다. 기존 20건의 병합 실패 수정은 사용자 보류 지시를 유지하며, 이번 956건 통과를 전체 테스트 통과로 해석하지 않는다. 커밋·push·스테이징·외부 전달은 하지 않았다.

### 5.14 적 AI의 배지 보유 대상 공격 선택 조사 (2026-10-13, Core 확인 요청·미전달)

> **2026-10-13 이동:** 이 절은 Core 확인 요청이라 "View가 Core 영역에 손댄 내역"을 모으는 §5에 둘 내용이 아니어서 [적 AI 배지 공격 확인 요청](2026-10-13-combat-core-requests-Hojin.md)으로 옮겼다. 옮기면서 받는 사람·영향 범위(배지는 전투 스테이지 11개 모두에 있음)·재현 순서·회신 양식을 채웠다. 요청 연결은 §13.17이다. 이 조사에서 Core·View·Framework·UI·Camera 실행 코드와 DB 에셋은 바꾸지 않았다.

### 5.13 StageSelect P7 아티팩트 번호 계약 (2026-10-11, 사용자 승인, 미전달)

> **2026-10-13 복원:** 병합 `9d4c76e5`에서 빠졌던 절을 같은 번호로 되살렸다. 이후 변경: LV5(2026-10-13)에서 `ArtifactEditFlow`는 `LifeCycles/Lobby/`로 옮겨졌고 아티팩트 편집은 로비에서만 연다([로비 계획 §14.9](../plans/2026-10-11-lobby-plan-Hojin.md#39-lv5-아티팩트-로비-이전)). 번호 계약은 그대로다. 이 절에서 닿은 Core 파일은 `CoreSystem/ArtifactSystem/ArtifactCommandProcessing.cs`(신규), `ArtifactEditingService.cs`(partial 전환), `InGameCoreSystemGroup.ArtifactCommands`이며 파일 목록 원본은 P7 인계다.

사용자가 필요한 Core·Buffer까지 직접 구현하도록 승인해 H1/H2와 아티팩트 입력/Flow/Plan/Job/View를 함께 전환했다. 실제 API·변경 파일·검증·추후 교체 순서는 [P7 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계)를 먼저 읽는다. 외부 담당자 전달·합의 기록은 없다.

- 최초 P7의 ProcessCommand(N, data)/ProcessPreview(N, data)는 사용자 추가 승인으로 교체한다. 현재 보완 계약은 bool CoreGroup.ArtifactCommands.ProcessNextCommand(expectedRequestNumber)/ProcessLatestPreview(expectedRequestNumber)이며 자동 검증을 완료했다. Core가 바인딩된 입력 Read Sink에서 다음 편집/최신 프리뷰를 직접 읽고 ArtifactBuffer에 번호·의도·편집/프리뷰/skip 결과를 게시한다. 입력·Job·View는 처리 결과를 대신 게시하지 않는다.
- Core의 0 기반 ID를 입력 Buffer에서도 허용했다. 입력 Clear·Run 초기화·씬 왕복에 번호를 유지하고 전체 Buffer 재생성 때 함께 초기화한다.
- StageLifeCycle이 실제 ArtifactEditFlow를 연결한다. 기존 ArtifactEditScheduler·ArtifactPreviewSlot·요청 객체·EditJob은 제거했고 PreviewJob은 번호 경로로 교체했다. UI는 기존 표시·수명 계약을 유지한다.
- 숨김은 제출 편집을 보존하고 프리뷰만 취소한다. Session이 자기 입력·슬롯·드래그의 종료를 소유하며 이전 수신자는 재연결 뒤 새 입력을 변경하지 못한다. 공용 오류 콜백 소유권과 전투 실행 코드는 이번에 변경하지 않았다. CombatPlanTests의 Core 가짜에는 새 서비스 접근자만 추가했다.
- 직접 조회 보완의 Job은 기대 번호와 좁은 Core 인터페이스만 받는다. 입력 없음·번호 불일치·이미 처리됨은 false로 정상 완료하며 계산/게시를 하지 않는다. 계산 전/게시 전 번호 검사와 기존 발급 번호 수명으로 취소·교체·같은 Stage 재연결 후 이전 Job의 새 입력 실행을 막는다. 입력 삭제는 View가 맡고, 정상 씬 이탈 Runner 가드·Session Stop 수명을 유지한다. ArtifactEditFlow는 처리 번호 ≥ N 또는 기대 편집 입력이 사라진 경우 이전 기다림을 끝내 새 요청을 등록한다. 같은 기대 입력이 남은 기록 누락은 도메인 정지를 유지한다. P7 당시 Stage/ShopFlow 조건과 맵/상점 Job의 결과 대조는 유지했으며 현재 H3/H4 후속은 §5.18을 따른다.
- 전투와 조회 주체를 맞추되 기대 번호를 남기는 것은 아티팩트 수명 보호를 위한 사용자 승인 구현 결정이다. Framework/UI의 필수 함수 서명으로 주장하지 않는다. P7 당시 유지한 H3/H4는 현재 §5.18의 직접 조회 후속 구현·검증 대상으로 구분한다.
- P8/P9·상점 S5·Run seed R4는 별도이며, 이번 보완 완료 판정은 SA 계획 P7.5~P7.9의 새 검증 기록을 따른다. 최초 P7 검증 이력으로 이번 계약의 통과를 대신하지 않는다.

### 5.14 상점 동작 확인용 InGame 임시값 (2026-10-09, 사용자 지시, 미전달)

> **2026-10-13 복원:** 원래 §5.8. 병합 `9d4c76e5`에서 빠졌다. 이후 변경: `Stage_Shop`은 이후 `StageRootDataTable`에 등록됐고, 아래 S2의 `CreateShopJob`·View `ShopScheduler`는 SA P5/P6에서 제거됐다(현재 코드에 없음). 두 임시값(`Shop_Default` 후보 3개, `startingRunCurrency: 20`)은 지금도 에셋에 그대로 있다.

StageSelect 상점 구현([상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md) S1, 설계 S3·Q6)을 위해 비어 있던 값을 **임시값**으로 채웠다. 기획 값이 오면 교체 대상이며, Excel DT 이전 때도 이 값은 기준이 아니다. Core 코드는 바꾸지 않았다.

| 파일 | 변경 전 | 변경 후 | 이유 |
|---|---|---|---|
| `DataBaseSO/InGameGroup/Stage/Shop/Shop_Default.asset` | `candidates: []` (상점 진입 시 Core 예외) | Akimbo 2, ShotGun 3, Sniper 5 (`Unit_*` UnitDataSO 참조, 예전 `Shop_1` 가격) | 상점 제안 생성에 후보 1개 이상 필요 |
| `DataBaseSO/InGameGroup/Loadout/Item/PlayerLoadout_1.asset` | `startingRunCurrency: 0` | `20` | 구매·회복 결제 확인. 20 = 상점 1~2개 구매 또는 회복 1회 |

- `Stage_Shop`은 아직 `StageRootDataTable`에 등록하지 않았다(상점 완성 뒤 S5.0에서 등록). 그래서 지금 실제 맵에는 상점 노드가 나오지 않는다.
- 시작 재화 0을 직접 검사하는 테스트는 실제 에셋이 아니라 테스트용 DB를 쓰므로 영향이 없다(정적 확인). `DataBaseAssetTests`는 `>= 0`만 검사한다.
- (2026-10-10, S2) `LifeCycles/Stage/Plans/StagePlan.cs`에 `ShopRequestPriority = 3` 상수와 `CreateShopJob(request, shop, nextSeed)` 정적 메서드, `ShopSystem` using 한 줄을 **추가만** 했다. 기존 `Register`·`JobType` 분기·`DefaultLoadoutKey`는 바꾸지 않았다. Job은 View 쪽 `ShopScheduler`가 공용 Runner에 직접 등록한다(아티팩트와 같은 방식).

### 5.15 StageSelect 입력 Buffer·등록 뼈대 (2026-10-10, 사용자 지시, 미전달)

> **2026-10-13 복원:** 원래 §5.9. 병합 `9d4c76e5`에서 빠졌다. P4 당시 기록이며 현재 계약은 §5.13·§5.18을 따른다.

[SA 계획](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬) P4 범위에서 **View 입력 저장소와 실행 조정 뼈대만** 준비했다. Core 호출·처리 번호/게임 결과 게시와 새 운영 경로는 아직 구현하지 않았다. H1~H4 인수 조건은 [처리 번호 요청](2026-10-06-stageselect-core-requests-Hojin.md#4부-stageselect-요청-처리-번호-기록-요청-view--core)과 [SA 설계 §2.6](../specs/2026-10-10-stage-map-design-Hojin.md#31-구조-정렬-sa-사용자-2026-10-10)을 따른다.

| 변경 위치(`Assets/ProjectL/Script/Systems/` 아래) | 실제 변경 | Core 작업 시 확인할 것 |
|---|---|---|
| `RuntimeSystem/BufferSystems/ShopView/` | 구매(`ShopOfferId`)·나가기 입력, `Submit` 번호 발급, Read/Write Sink·`RemoveCommandsThrough`·`Clear` 추가 | 상품 키는 Core가 발급한 실제 키. 구매 규칙·처리 결과를 입력 Buffer에 넣지 않음 |
| `RuntimeSystem/BufferSystems/ArtifactView/` | 명령과 같은 번호 발급기를 쓰는 최신 프리뷰 슬롯, `SetPreview`·`ClearPreview`·`RemoveCommandsThrough`·`Clear` 추가 | H1 명령 처리와 H2 응답/건너뜀 번호는 Core 결과 저장소에 별도로 게시해야 함 |
| `RuntimeSystem/BufferSystems/StageView/StageViewBuffer.cs` | 단일 삭제를 `RemoveCommandsThrough`로 교체, `Clear`는 발급 번호 유지 | 전투·비전투·미지원/없는 노드까지 확대 H4를 인수하기 전 기존 소비자 유지 |
| `ServiceKeys/DataBaseGroupServiceKey.cs`, `Factory/Runtime/Buffer/BufferSystemFactory.cs`, `RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | Shop typed key/Factory Export/`IBufferQuery.ShopViewReadSink`·`ShopViewWriteSink` 등록·조회 추가. 기존 거래 본문은 그대로 | `IBufferQuery` 구현 가짜에도 새 속성이 필요함. 입력 Read/Write는 같은 저장소를 가리킴 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/`, `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/ShopPlan.cs`, `LifeCycles/Artifact/Plans/ArtifactPlan.cs` | `JobRootType.Shop = 4`; 사전 서명 유지, 람다로 필요한 입력 Read Sink 전달. Shop은 `None`만 지원 | `ShopCommandJob`·`StageNodeEntryJob`은 H3·H4 인수 후 P5, 아티팩트 처리 Job은 H1·H2 인수 후 P7. 현재 새 처리 등록은 명시적으로 예외 |

- 번호 수명: 입력 `Clear`·프리뷰 취소·prefix 삭제는 발급 번호를 0으로 돌리지 않는다. 새 Run/Buffer 재생성 때 입력 번호와 Core 번호의 수명은 실제 인수에서 확정해야 한다.
- 새 Flow의 번호 읽기/전투 분기는 임시 `Func` 경계와 테스트 가짜로만 검사했다. 실제 Core 서비스 서명을 정의하거나 처리 번호를 운영 Buffer에 대신 쓰지 않았다. 미지원 노드의 실제 거절 분류도 아직 연결하지 않았다.
- `IStageEntryAttemptRead`는 기존 전투 요청 참조와 맵 번호만 읽게 준비했다. 운영 StageLifeCycle은 아직 이 읽기 대상에 연결하지 않아 빈 결과를 돌려준다. 전투 요청에 맵 번호를 전달하는 변경은 하지 않았다.
- P4 구현 당시 운영은 기존 `StageMapEntryFlow`·상점/아티팩트 스케줄러(W1)였으며 신규 Flow Tick·UI Submit·운영 화면 연결과 P3.9/P3.10은 보류했다. 이후 사용자 승인 P3.9 변경은 아래 §5.16을 따른다. 검증 결과는 SA 계획의 P4 기록을 참조한다. **외부 전달·Core 수락·구현 인수 완료를 뜻하지 않는다.**

### 5.16 StageSelect 공용 오류 입력 정리 (2026-10-10, 사용자 승인, 미전달)

> **2026-10-13 복원:** 원래 §5.10. 병합 `9d4c76e5`에서 빠졌다. 이후 변경: 로비 StopInput이 네 번째 정리 대상으로 추가됐다(`InGameViewFaultCleanup`의 선택 인자 `lobby`).

P3.9에서 단일 공용 콜백을 View 그룹이 소유하고 전투·아티팩트·맵/상점의 StopInput을 호출하도록 연결했다. 오류 시 StageView·ShopView·ArtifactView의 대기 명령과 최신 프리뷰 슬롯을 비우되 **입력 발급 번호·Core 처리 번호/게임 결과는 초기화하거나 대신 쓰지 않는다.** Global Runner Clear·Idle/실패 Job 배출 대기도 없다. 오류 난 같은 런타임은 재개하지 않는다.

변경 기록 보완(2026-10-11): 전투 영역은 `CombatViewSystem.cs`뿐 아니라 `Assets/Tests/EditMode/Framework/CombatViewSystemTests.cs`도 수정했다. 공용 콜백 소유권 변경에 맞춰 테스트 조립/종료 회귀를 갱신한 것이다. 정상 Bootstrap 종료는 Bootstrap과 그룹 Shutdown에서 각각 StopInput을 호출할 수 있으며 반복 정리가 안전해야 한다. 맵/상점/아티팩트 단독 정리는 다른 Layer를 보존하지만, 바인딩된 Combat의 기존 정리는 현재 Layer를 무조건 비우므로 전체 공용 오류 경로의 다른 Layer 보존은 보장하지 않는다. 전투 미바인딩 테스트의 통과를 그 보장으로 확대하지 않는다.

기존 입력 Write Sink만 받았으며 Buffer 계약/서비스 키·Core 계산/게시 코드는 이번에 변경하지 않았다. P3.9 당시 신규 Flow Tick/UI Submit·실제 ShopUI·P3.10은 보류했다. 이후 아티팩트 계약 교체는 아래 §5.17을 따른다. 신규 Flow/UI Submit·실제 ShopUI는 여전히 미연결이다. H1~H4 호출·전투 맵 번호 전달 인수를 완료한 것이 아니다. 소유권·예외 처리·종료 연결 해제와 검증 증거는 [공용 오류 변경 기록](2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md), [SA 계획 P3.9](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)를 따른다.

### 5.17 아티팩트 UI 계약 교체 (2026-10-11, 사용자 승인, 미전달)

> **2026-10-13 복원:** 원래 §5.11. 병합 `9d4c76e5`에서 빠졌다.

P3.10에서 아티팩트 Host/Screen을 제거하고 UI 소유 동기 수명 알림을 Session이 연결/해제하도록 바꿨다. 일반 Hide는 이미 제출된 편집을 보존하며 fault/Detach는 기존 자기 요청/슬롯/Layer 정리만 수행한다. Core 처리 번호·게임 결과·Buffer 계약/키·편집 규칙·기존 스케줄러/입력 실행 경로를 바꾸지 않았다. UI는 Buffer/DB를 직접 조회하지 않는다.

P5/P6·새 Flow/Plan/Job Core 호출·운영 ShopUI·UI 입력 Buffer 제출은 진행하지 않았다. 이번 UI 수명 회귀 통과로 H1~H4/맵 전투 번호 인수를 완료한 것이 아니다. 실제 변경·검증·기존 실패는 [SA 계획 P3.10](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)을 따른다. 이 자료는 미전달이며 Core 작업자 수락/합의가 아니다.

### 5.18 StageSelect P5/P6 Core 결과 계약 (2026-10-11, 사용자 승인, 미전달)

> **2026-10-13 복원:** 원래 §5.12. 병합 `9d4c76e5`에서 빠졌다. 이 절에서 닿은 Core 파일은 `CoreSystem/StageCommandSystem/StageCommandProcessingService.cs`(신규), `InGameCoreSystemGroup.StageCommands`, `CoreCaculator/Contracts/Results/ChapterProgressionResult.cs`의 `UnsupportedStageType` 추가이며 파일 목록 원본은 P56 인계다.

최초 P5/P6에서 사용자 승인으로 H3/H4와 맵·상점 소비자를 로컬 구현했다. 이후 사용자가 H1/H2처럼 **Core가 입력을 직접 조회하도록 H3/H4 보완을 승인했고 로컬 구현·자동 검증을 완료했다.** 외부 전달·Core/전투 작업자 합의·외부 인수 완료는 아니다. 현재 후속 API·수명·교체 순서는 [P56 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md), 완료 증거는 [SA 계획 P6.6~P6.10](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)을 따른다. 위 §5.15~5.17의 미구현/미연결은 각 Phase 당시 이력이다.

- `CoreGroup.StageCommands : IStageCommandProcessingService`의 현재 후속 계약은 `bool ProcessNextShopCommand(int expectedRequestNumber)`, `bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)`, `bool TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result)`다. Core가 ShopView/StageView 입력을 직접 읽고 기존 규칙 서비스·준비→설치 거래에 위임한다. 요청 내용·입력/결과 Sink·결과 대조를 Job에서 제거하며 맵 임시 seed 공급자만 예외다.
- 입력 없음·기대 번호 불일치·이미 처리됨은 상태/번호 변경 없는 false이고 같은 도메인 재진입은 예외다. 유효 입력 확인·guard 뒤 seed 공급자를 한 번 호출하고 호출 뒤 입력 번호를 재확인한다. 기존 동기 Shop/Combat 서비스 실행 뒤 정상 수락/거절은 **DB 콜백에서 입력을 Clear해도 반드시 게시한다**. Clear를 commit 이후 취소/거래 롤백으로 해석하지 않으며 DB 예외 때 기존 거래 원자성·이전 결과 보존을 검사한다.
- `StageViewCommandData.ChapterKey`는 필수다. 실제 표시한 맵의 챕터를 제출하며 표시 전 제출하지 않는다. StageLifeCycle도 입력 챕터로 전투를 분기한다. 다른 챕터 결과는 해당 입력만 정리하고 현재 화면 열기/문구 쓰기를 하지 않는다.
- 번호 있는 RequestEntry는 등록 전에 입력 N·chapter/node와 요청 대응을 검사한다. Prepare의 Core 미실행 false만 내부 Running→Cancelled로 끝내며 일반 Running 취소는 거부한다. 번호 없는 Bootstrap, Prepared→Activating→Active, 취소 Job 배출과 `IStageEntryAttemptRead`의 동일 요청 참조 보존을 유지한다.
- `CurrentProgressBuffer`의 Read/내부 Store와 `IBufferQuery.StageCommandResults`의 맵/상점 마지막 처리 번호·요청 문맥·결과는 유지한다. 입력/결과 번호는 Clear·새 Run·씬 왕복에 유지하고 전체 Buffer 재생성 때 함께 0부터 시작한다. 실행 전 취소/예외의 가짜 기록이나 View/Job 대리 게시를 만들지 않는다.
- 비전투 맵·상점 Flow는 처리 번호 또는 기대 입력 소멸로 기다림을 끝내고 새 요청을 받는다. 같은 입력이 남은 실제 기록 누락만 해당 도메인을 정지한다. 전투 Cancelled는 기존 Job 배출과 View의 해당 입력 제거까지 기다린다. 실제 예외는 기존 공용 fault다.
- 원격 전투 R3(`origin/feature/Yonggeun/Stage`, `c773625a`)는 미병합이다. 별도 CombatEntryService·Prepare/Transition Job에 번호·직접 조회·입력 대상 검증·미실행 취소와 같은 요청 참조 보존을 옮겨야 한다. Transition Job은 Runner 실행 중 전환하므로 로컬 Idle guard를 그대로 복사하지 않는다.
- 최초 P56의 `EnterNode(N, chapter, node, seed)`·`EnterBattle(N, chapter, node)`·`Purchase(N, chapter, node, offer)`·`CompleteShop(N, chapter, node)`와 최신 N/문맥 결과 재사용·오래된/다른 문맥 번호 예외는 **번호·내용 API 이력**이다. 해당 검증 `p56-all.xml`의 **3702/3667 통과/34 기존 실패/1 기존 판정 불가/0 skip**, 추가 실패 0·관련 414/413 통과/0 실패/기존 판정 불가 1·에셋 3건 통과·MSBuild exit 0도 최초 P56 이력이다. 현재 직접 조회 보완의 최종 수치로 재사용하지 않는다.

후속 자동 테스트 증거는 `Logs/StageSelectSAVerification/stage-shop-pull-*`다. 기준선 277/276 통과/실패 0/기존 판정 불가 1, RED 27/10 통과/기대 실패 17, 관련 365/364 통과/실패 0/기존 판정 불가 1이다. 전체 `stage-shop-pull-all.xml`은 3769/3734 통과/기존 실패 34/기존 판정 불가 1/skip 0이며 실패 fullname이 `p7-pull-final-all.xml`과 같아 추가/해소 0이다. Unity exit 2를 전체 통과로 해석하지 않는다. P6.6~P6.9는 완료, P6.10 최종 빌드·독립 검토도 완료했다.

직접 조회는 사용자의 프로젝트 결정이며 Framework/UI가 강제한 함수 형태가 아니다. H1/H2는 §5.13의 완료 기록을 따른다. 수동 Play/Profiler·상점 S5·Run seed R4·P8/P9, 외부 전달·합의는 별도다.

### 5.19 V4 회복 입력 Buffer 공용 등록 (2026-10-12, 사용자 승인, 미전달)

> **2026-10-13 추가:** 복원 점검 때 §5 기록이 없던 변경을 보충했다.

회복 입력 Buffer V4를 연결하며 공용 파일 세 곳에 등록·조회만 추가했다. `BufferSystems/Core/BufferSystemGroup.cs`에 `IBufferQuery.RecoveryViewReadSink`·`RecoveryViewWriteSink` 접근자와 생성자 조회, `Factory/Runtime/Buffer/BufferSystemFactory.cs`에 `RecoveryViewBuffer` Export 한 줄, `ServiceKeys/DataBaseGroupServiceKey.cs`에 `RecoveryViewBufferKey.Read/Write`를 더했다. Core 계산·서비스·결과/transaction은 바꾸지 않았다. 실제 API·검증 원본은 [V4 Core 인계서](2026-10-09-recovery-core-handoff-Hojin.md#4부-v4-회복-입력-buffer--core-인계), 진행 상태는 §13.11이다.

### 5.20 아티팩트 DB·Buffer 이전 (2026-09-28~10-03, 사용자 지시)

> **2026-10-13 추가:** 복원 점검 때 §5에 연결이 없던 변경을 보충했다. 상세 기록의 원본은 [아티팩트 DB 이전 인계](2026-09-28-artifact-db-migration-handoff-Hojin.md)다.

- `8f38e2c1`: `IInGameQuery.ArtifactDefinitionTableQuery` 추가(`Queries/InGameDataBase/InGameDataBase.cs`), `BufferSystems/Artifact/ArtifactBuffer.cs`·`ArtifactData.cs` 수정.
- `05acb3c4`·`f76d9c44`: `ArtifactBuffer` 계약 변경과 이에 맞춘 `ChapterGraphPreparationTests`·`MapProgressBufferTests` 수정.
- 이 계약은 이후 P7(§5.13)에서 번호 계약으로 다시 바뀌었다. 위 커밋의 설명을 현재 API로 읽지 않는다.

### 5.15 b2fbcaf 병합 시 직접 공격 그룹 게시 위치 이전 (2026-10-13)

사용자의 `b2fbcaf` 병합 지시로 해당 브랜치의 공격 명령 단계 분리 구조를 인수했다. `CombatCommandCalculator`의 충돌은 대상 브랜치의 `CombatAttackCalculator` 위임과 공통 헬퍼를 유지하며 해결했다. 기존 §5.11의 직접 공격 그룹 게시 한 줄은 새 `AttackResultFactory.Create`의 `AttackResult.FromFacts` 호출에 `attackGroup: CommandAttackGroups.Direct`로 옮겼다. 그룹 발급·피해·관통·판정 정책을 변경하거나 옛 계산 본문을 중복해서 남기지 않았다. `AttackResult.AttackGroup` 공개 계약과 기존 View 소비는 유지한다. 기존 인계에서 본문 게시 위치가 `CombatCommandCalculator`라고 된 기록은 변경 당시 경로이며 현재 게시 위치는 이 항목을 따른다.

문서 충돌은 양쪽 기록을 보존했다. 기존 Wall §5.10/5.10.1·Ballista §5.11을 유지하고 대상 브랜치의 LV8A·성배·적 AI 요청은 각각 §5.12·§5.13·§5.14로 이어 배치했다. 로비 계획의 §5.10 인계 참조 두 곳도 새 §5.12로 갱신했다. FMOD 캐시 충돌은 타임스탬프만 대상으로 대상 브랜치의 최신 값을 유지했고, 검증 중 열린 원본 에디터가 실제 bank 파일 시각으로 갱신한 캐시도 그대로 인수했다. 이벤트·bank 정의 차이는 없었다. 커밋·push는 하지 않는다.

병합 호환을 위해 View 테스트 2곳(`AttackHealthPreviewDisplayTests`·`CombatInteractionTests`)에서 제거된 `PierceRayHitPreview` 인자를 없앴다. `ConnectedWallCombatTests`의 관통/연결벽 검증은 새 `CollectSingle` API에 같은 오른쪽 진행을 지정하도록 호출만 이전했다. 검사 조건과 기대값은 유지했다.

검증: 새/삭제 소스를 반영한 임시 MSBuild targets로 `ProjectL.Tests.Framework`·`ProjectL.Tests.Core`와 런타임 참조 전체가 컴파일 오류 0이었다. 독립 Unity 6000.0.73f1 프로젝트에서 Core 전체 및 이동·입력·공격 표시의 선택 View 회귀 **2,833/2,833 통과, 실패·건너뜀·불확정 0**을 확인했다. 이동 편집 입력14/표시18, 이동 범위 요청30/피드백21/표시6, Ballista19, 관통 명령36, 연결벽 전투14건을 포함한다. 원본은 `Temp/MergeB2fbcaf/merge-b2fbcaf-tests.xml`·`unity-edit.log`·`msbuild-framework.log`·`msbuild-core.log`다. 모든 View 테스트·로비 운영 Play·실제 씬 연결 전체를 재검증한 것은 아니다. 커밋·push는 사용자에게 남긴다.

### 5.16 Stage 1 boss identity (2026-10-14, explicitly authorized)

The user identified themselves as the Core programmer and explicitly permitted Core edits for the approved boss SO work. Added only `EntityKey.Unit_Stage1Boss = 18` in `CoreCaculator/Contracts/Ids/EntityKey.cs`; all existing members retain their values. This supplies a stable identity for the prepared SO. No Core adapter, calculation, stage, or Buffer behavior was changed in this Phase. SO contracts and remaining integration work are recorded in §14.

### 5.17 Boss UnitData-to-UnitSpec conversion (2026-10-14, Phase 3)

Under the user's explicit Phase 3 request, `CoreSystem/CombatSystem/BattleDefinitionAdapter.cs` now converts `NecromancyTraitData` to `NecromancyTraitSpec`, `RoarTraitData` to `RoarTraitSpec`, and passes the immutable `UnitData.Behavior` to UnitSpec. No runtime calculator, stage, Buffer, source interface, or View change was made. Nine new adapter test cases pass; see §14.3 and the boss SO plan's Phase 3 record. Summon dependency collection remains Phase 4 work.

### 5.18 Summon definition/profile assembly (2026-10-14, Phase 4)

Under the user's Phase 4 request, `BattleDefinitionAdapter.Read` now collects reachable Necromancy summon definitions with a private queue/visited helper, without changing roster membership or placements. Definitions are converted once and sorted by key; the unchanged DatabaseBattleInitializationSource attaches profiles for all of them. Existing missing-data/profile failures and EnemyLoadout restrictions remain intact. No runtime calculators, Buffer, View, operational assets, or source API changed. Verification and remaining activation dependencies are in §14.

### 5.19 Stage objective/placement authoring (2026-10-14, Phase 5)

Under the user's Phase 5 request, `CombatStageData` now authors the existing BattleObjectiveKind and a target flag on each unit placement. `CombatStageDetail` and shared EntityPlacement export these values with old constructor/asset defaults intact; Validate rejects invalid objective/count combinations and any marked structure. No CoreSystem file changed in this Phase. Adapter forwarding is still Phase 6. Tests, the unchanged baseline failure, and boundaries are recorded in the stage plan (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`) and §14.3.

### 5.20 Stage objective forwarding and unit-key repair (2026-10-14, Phase 6 complete)

The user's Phase 6 request implemented authored placement flags into InitialEnemyPlacementSpec and objective kind into BattleInitialization. Actual SO/default-source testing exposed UnitDefinition's old key whitelist. The user explicitly approved replacing it with defined Enum.GetName values beginning with ordinal Unit_, while preserving main-key/role checks. This additional Domain change is implemented and covered by 16 new key/role tests; the adapter has 43 new cases. All 59 pass, including real boss preparation, same-key target identity and failure-publication checks. Full final selection 287/288 retains only the exact existing terrain failure. Independent review found no actionable issue. No operational asset/View changes; Phase 7 remains pending. Details: stage plan execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`).

### 5.21 Roar threshold publication and content ownership (2026-10-14)

Explicit user request: expose the Roar threshold so View can display `HitCount/HitThreshold` without querying InGame data. `RoarTraitStatusSnapshot(int hitCount, int hitThreshold)` requires a positive threshold and compares both values. `TraitEffectProvider` copies the threshold from the definition; `RoarHitCounter` preserves it on increment/reset. These are the three changed CoreSystem production files; no new service/module or Buffer writer was introduced. The definition remains the rule source, and runtime state still stores only count (SSOT/P6; existing FW registration/lifecycle unchanged).

The same request corrects Phase 7 ownership: View may implement/edit SO authoring/data types, register tables, and configure stage placements without Core approval. Only required code changes under `RuntimeSystem/InGameSystem/CoreSystem/` need Core consultation. See the current [View handoff](2026-10-13-boss-summoning-view-handoff.md) for usage and verification evidence. Production stage activation remains pending in the View workflow.

### 5.23 Heavy attack knockback immunity (2026-10-15, Phase HN1)

The user explicitly approved the revised design (Git history: `2026-10-15-heavy-knockback-immunity-design.md`) and HN1 implementation plan (Git history: `2026-10-15-heavy-knockback-immunity-plan.md`), including this Core scope. HN1 is implemented and stopped for user review; this record has not been sent externally.

Core production changes, relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`:

- New `Domain/Combat/KnockbackImmunityPolicy.cs` and `.meta`: internal pure `IsImmune(CategoryKey targetCategory, bool isForcedMovementImmune)`; Heavy OR existing explicit immunity.
- `Application/Combat/AttackKnockbackCalculator.cs`: assemble the target's runtime category and existing explicit immunity, then call the policy at the existing guard after damage/death and before displacement/path/arrival handling. No duplicated Heavy condition in the caller.

Heavy now resists ordinary attack and boss-roar knockback on either side. Damage and death retain existing behavior. General forced-movement immunity, tile pushes, tornado transport, obstacles, DB/schema, public Buffer/API, assembly registration, and View are unchanged. No new DB configuration is required. `Unit_ShotGun.asset` is still Base. Legacy directional self-armor remains until the separate ally-protection-area redesign; HN1 does not complete that redesign.

Policy tests take only enum/bool; integration tests cover target-input provenance, preview parity/immutability, damage/death, multiple targets, and terrain/arrival boundaries. Native Unity 6000.0.73f1 EditMode: baseline 107/107; final 131/131, 0 failed/skipped, exit 0 (24 new cases). RED and disconnected-policy evidence, exact suite counts, warning details, and log/XML locations are recorded in the plan's execution section. The existing `CombatCommandEffects.cs` CS8632 warning remains; no final compiler error. Independent read-only review found no actionable issue. No PlayMode claim, commit, push, or external delivery.

### 5.24 Regeneration value contracts (2026-10-15, Task UR1)

The user explicitly requested Task UR1 of the Regeneration plan (Git history: `2026-10-15-undead-heavy-regeneration-plan.md`). Core scope is implemented and verified; stop here for review. No external delivery occurred.

Relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, new files are `Contracts/Commands/Traits/RegenerationTraitSpec.cs`, `Contracts/Snapshots/Traits/RegenerationTraitStatusSnapshot.cs`, `Domain/Traits/Definitions/RegenerationTraitDefinition.cs`, `Domain/Traits/Rules/RegenerationTraitRule.cs`, `Domain/Traits/States/RegenerationRuntimeState.cs`, `Domain/Traits/Effects/RegenerationEffectRule.cs`, `Domain/Traits/Effects/RegenerationEffectStateSnapshot.cs`, and `Application/Combat/Traits/RegenerationTraitFactories.cs`, each with its meta. Existing-file connections are `Contracts/Common/TraitKey.cs` (Regeneration = 100), `Application/Combat/Traits/TraitRegistry.cs`, `Application/Combat/Traits/TraitEffectProvider.cs`, `Application/Combat/CombatCommandEffects.cs`, and `Domain/Traits/Effects/TraitEffectStateCodec.cs` (Capture/Restore for the new stateless snapshot).

Heal uses the current HealthStateSnapshot maximum, caps overflow-safe addition, does not revive, and rejects nonpositive amounts. Immutable spec/definition/status carry HealAmount; runtime/effect state carries no health or turn counter. The existing effect registry accepts a marker without attack/move/own-turn behavior. No Framework service/module/lifecycle registration changed. Healing arithmetic is isolated in one rule file; shared assembly/snapshot cases have no healing policy (SSOT, SOLID and human change-locality review).

Unity 6000.0.73f1 EditMode: baseline 63/63; behavioral RED 13 failures of 19; final 92/92, 0 failed/skipped, exit 0, including 19 new Regeneration cases. No compiler errors; existing CS8632 and CS0105 warning sites remain. A fresh read-only review found no actionable issue and checked saved evidence without rerunning Unity. Full counts, commands, diagnostics, and local evidence paths are in the plan execution record. No turn-completion healing call, DB authoring, production assets, View behavior, PlayMode claim, commit, or push. UR2-UR4 are pending.

### 5.25 Regeneration turn-completion connection (2026-10-15, Task UR2)

The user explicitly requested UR2 after UR1. Added `CoreCaculator/Application/Combat/RegenerationTurnCalculator.cs` and its meta, plus one call in `CombatTurnCalculator.EndTurn` after TurnEnded and before constructing the environment projection. No other production source changed for UR2. The calculator selects living field holders by EntityId, resolves their RegenerationTraitSpec, calls the existing pure Heal rule with projected health, and publishes actual changes through CombatCommandEvents.Health and the normal transition path. Main persistent HP publication is covered. No new service/module, event kind, counter, direct Buffer write, or own-turn broadcast.

D1/D2 follow the written UR2 contract: accepted self/ally/enemy turn completion heals once, before tornado, weak-ground collapse and night recall. Death interruption and a combat-ending attack do not synthesize a completion. Formula/selection/timing remain separately editable in the rule, recipient calculator and one orchestration call (SSOT/SOLID/FW and human change-locality boundaries).

Unity 6000.0.73f1 EditMode: fresh baseline 154/154; final 179/179, 0 failed/skipped, exit 0, including 25 new tests in RegenerationTurnTests. Tests use real enemy and boss Advance paths, verify no-trigger cases, preserved nonacting discounts, and ordered health events including subsequent environment damage/death. Exact RED evidence and test-fixture correction, command, suite counts and diagnostics are in the plan execution record (Git history: `2026-10-15-undead-heavy-regeneration-plan.md#task-ur2--turn-completion-2026-10-15`). No compiler errors; unchanged CS8632/CS0105 warnings remain. UR3 authoring/adapter and UR4 production content are pending. No assets, View behavior, PlayMode claim, commit, push, or external delivery.

UR2 final review: a fresh read-only reviewer found no actionable issue and verified scoped source, metadata and saved test evidence without rerunning Unity. Stop at the UR2 review boundary.

### 5.26 Regeneration unit authoring and shared adapter (2026-10-15, Task UR3)

The user clarified their Core role, authorized rule-authoring SO work, and explicitly requested UR3 through verification and stop. Added `DataBaseSystem/Data/InGame/Table/Unit/Trait/RegenerationTraitAuthoring.cs` and `RegenerationTraitData.cs` with metas. The only Core production edit is one `RegenerationTraitData -> RegenerationTraitSpec` case in `RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/UnitSpecAdapter.cs`. ReadUnit's signature, existing cases, and both consumer implementations are unchanged.

The existing UnitDataSO SerializeReference list is the single authored source of each unit's healAmount. Exports are fresh immutable pure data; Validate rejects nonpositive values, while existing UnitData validation rejects null/duplicate entries. No separate SO/table, query/service interface, factory registration, runtime state, or existing-trait migration. The new input/data types isolate the feature; shared conversion is one explicit registration point (SSOT/SOLID/P1-P3/FW and human change locality).

Native Unity 6000.0.73f1 EditMode: baseline 148/148; final 162/162, 0 failed/skipped, exit 0, including 14 new cases. Verified serialization/concrete type, property editing, immutable exports, invalid/null/duplicate data, stat/trait preservation, initial and summon-only definitions/profile amounts, and out-of-combat roster keys. The roster fixture uses the existing IUnitTableQuery contract; no new interface was defined. An intermediate test expectation was corrected to the existing ascending TraitKey order; production sorting was unchanged. Final compiler diagnostics contain only existing CS0105 at LobbyDepartureIntegrationTests.cs:46. A fresh independent read-only review found no actionable issue and confirmed saved evidence without rerunning Unity.

Exact commands, RED/final evidence and test counts: UR3 execution record (Git history: `2026-10-15-undead-heavy-regeneration-plan.md#task-ur3--existing-unit-authoring-and-shared-conversion-2026-10-15`). No production unit assets, balance choice, subtype picker, manual Inspector/PlayMode claim, commit, push, or external delivery. UR3 is complete and stopped; UR4 and D3-D6 remain pending.

### 5.27 Intimidation contracts and pure calculation (2026-10-15, Phase SI1)

The user authorized the reviewed Shielder plan (Git history: `2026-10-15-shielder-intimidation-plan.md`) for execution. SI1 adds TraitKey.Intimidation (110), its spec/definition/status and stateless owner-side adapters, plus CharacterDebuffState / IntimidationDebuffState and CombatDebuffCalculator. Existing registry/provider/codec/CombatCommandEffects mappings are extended; no existing trait is migrated.

The victim state holds the original percentage and a zero-or-positive Priority penalty. Apply captures the first percentage, upgrades once with the next caster's penalty, and ignores further applications; Clear returns Empty. Evaluation uses current input values, wide integer percentage arithmetic and Priority clamping to one. Base stats, owned TraitStatuses, Buffer state and existing effect capabilities are unchanged. The calculator does not select victims, consume attacks, or schedule turns.

Verification and the Phase stop are recorded in the plan's SI1 execution record. SI2 must first carry these values through facts/copies/Buffer; SI3 owns actual hit application, expiry and scheduling. No production Shielder content, View changes, commit/push, or external delivery is included.

### 5.28 Intimidation state preservation and publication (2026-10-15, Phase SI2)

The user explicitly requested SI2 and additionally approved the minimal BattleCompletionCalculator / BattleCompletionService changes after the direct-completion bypass was identified. CharacterCombatFacts now ends its constructor with optional `CharacterDebuffState debuffs = null`, normalized to Empty. CharacterData exposes the same immutable facts through Debuffs. CombatDebuffApplication.WithDebuffs replaces only that field; nine existing live-copy owners preserve it, including the Buffer's persistent-main rebuild. The full-facts comparer includes debuff values in its existing optimistic validation.

CombatCommandCalculator.Finish clears dead victims and all terminal-state characters before publication. Direct completion clears the main and settled characters in BattleCompletionCalculator; BattleCompletionService adds other active characters' cleanup to the same completion proposal. Existing completion/progression, duplicate handling, held-effect events and Buffer transaction ownership remain intact. EndBattle still removes battle records; fresh preparation starts Empty. There is no persistent roster debuff, extra service, Job, or second state store.

The SI2 execution record in the plan (Git history: `2026-10-15-shielder-intimidation-plan.md`) is the verification source. The focused native run passes 120/120, including the approved completion additions. The full suite has 6,602 passes, 89 failures and one explicit manual skip; all89 failures exactly match previously reproduced pre-change failures. Fresh read-only review found no actionable issue. SI2 is complete and stopped for review. SI3 still owns actual hit application, attack expiry and pending-action rescheduling; none is implied by the SI2 state plumbing. No commit, push or external delivery occurred.

### 5.29 Intimidation attack and scheduling integration (2026-10-15, Phase SI3)

The user explicitly requested SI3 after SI2 review. CombatAttackCalculator evaluates the debuff after current positive attack contributions and captures that common damage for all attack-derived hits. AttackDirectDamageCalculator and KillChainAttackCalculator fill one execution-local delivered-victim set; CombatDebuffApplication.CompleteAttack applies once to surviving hostile characters and clears the actual attacker's prior Intimidation after the entire ordinary/reaction attack. It changes only Debuffs and never publishes or schedules. Thorn-only, allied, structure and dead victims are excluded; zero-damage delivered hits qualify.

CombatEffectiveStatsCalculator combines current base/flag Priority with the debuff. CombatDebuffScheduleCalculator preserves the remaining-wait fraction using checked arithmetic only when the stored penalty changes; active/opening participants keep their time. CombatCommandCalculator.Finish is its sole caller, comparing the current step's input to final projected facts and retaining proposed turn changes. Dead participants are removed; terminal cleanup does not rescale future waits. Existing turn-entry and summon creation sites use the evaluator. The stored Timeline receives final per-participant times/Priority/active identity; the existing read-only multi-turn forecast remains unchanged.

All nine affected production owners are listed in the SI3 plan record. No service/Job, Buffer writer, persistent cache, event capability or new execution API was added. The only older-test adaptation is the new set argument at KillChainAttackTraitTests' direct internal call. The new attack/schedule/publication fixtures exercise actual command, preview, AI, reaction, forecast and Buffer paths. Focused native verification is 301/301. The full suite has 6,639 passes, 90 failures and one explicit manual skip; all90 failures exactly match SI1 failures reproduced on pre-change code. The railway case that passed in SI2 failed again with its original assertion. Fresh independent review found no actionable issue. Evidence and limits are in the plan execution record (Git history: `2026-10-15-shielder-intimidation-plan.md`). SI3 is complete and stopped for review; SI4-SI6 authoring, View and Shielder content are not included. No commit, push or external delivery occurred.

### 5.30 Intimidation SO input and shared Core conversion (2026-10-15, Phase SI4)

The user explicitly requested SI4 after confirming its DB/Core boundary. Added separate IntimidationTraitAuthoring and pure immutable IntimidationTraitData files in the existing unit trait folder. The two serialized parameters have no production defaults; zero is unconfigured and rejected. Validation requires attackReductionPercent1..100 and priorityReduction>=1. The existing UnitDataSO SerializeReference export and UnitData null/duplicate validation contracts remain. UnitSpecAdapter adds one case preserving both parameters in IntimidationTraitSpec, shared by battle preparation and owned-character stat queries.

New authoring tests save/unload/reload temporary unit/targeting SOs, verify managed-reference type/ID and both parameter pairs, fresh exports, invalid bounds, isolation after edits and coexistence with Regeneration. Consumer additions verify initial/summon-only definitions, attack profiles, preserved Necromancy/Knockback/stat values and roster trait keys without combat-state mutation. Native focused results:213 total,212 passed,1 failed,0 skipped; all18 new cases pass. The sole Stage1101 terrain assertion exactly matches the fresh pre-SI4 baseline. The full suite has 6,658 passes,89 failures and one explicit manual skip; all89 failures exactly match SI3 names/messages. The prior railway presenter failure passed this run; no fix is claimed. Fresh independent review found no actionable issue. SI4 is complete and stopped for Phase review. Commands, artifacts and limitations are in the SI4 execution record (Git history: `2026-10-15-shielder-intimidation-plan.md#si4-execution-record--2026-10-15`). No View, production asset, subtype picker, new service or Buffer change; SI5/SI6 remain unstarted. No manual Inspector/PlayMode claim, commit, push or external delivery.
### 5.31 Thorn Heart contracts and isolated reflection (2026-10-16, Phase TH1)

The user explicitly authorized TH1 in this session. Core changes are limited to Contracts/Combat/CombatEvent.cs (optional ThornHeartReflectionContext and compatible constructor), new Contracts/Combat/ThornHeartReflectionContext.cs, Application/Artifact/SoulEffectCalculator.cs (internal read-only compiled-loadout accessor), Domain/Artifact/Rules/ThornHeartRule.cs (qualifying-hit terminology), and new Application/Artifact/ThornHeartAttackScope.cs and ThornHeartReflection.cs, with their metas. Existing TH0 targeting/Shielder work is preserved.

The isolated scope reuses compiled configuration/origin, owns one calculation-local opportunity (including configured zero), and proposes immutable HP/cause changes for an ordinary enemy hit on a player character at night, independent of positive HP loss. It bypasses defensive modifiers without consuming held protection, caps Applied at current HP, preserves surviving multi-cell occupancy and captures the pre-hit ally/location/group cause with no character kill credit. Reflection source/context validation remains in this feature and the existing event boundary; no Buffer store, Framework service or Unity dependency was added.

The new contract/scope fixtures add 37 cases; the focused native regression selection passes 158/158, exit 0. Full Core passes 2739/2739. Framework has the same 97 TH0 failures plus one additional pre-attack node-entry error; that test passed an unchanged isolated rerun. The full run remains failing. The Task plan execution record (Git history: `2026-10-16-thorn-heart-reflection-plan.md#9-th1-contracts-and-isolated-calculation--2026-10-16`) owns RED/GREEN evidence, the exact comparison, source-backed nondeterministic-node investigation and review stop. The production RequireSupportedAttackSettlement guard and its caller remain; no actual attack, piercing, preview/AI or publication integration is claimed in TH1. No commit, push, staging or external delivery.

### 5.32 Thorn Heart real attack connection (2026-10-16, Phase TH2)

The user explicitly authorized TH2. AttackDirectDamageCalculator now flushes pending direct health at the first qualifying hit, applies TH1's returned reflection with its cause/death anchor, and reports reflected attacker death. Nonpiercing stops before later hit bookkeeping; piercing completes all existing encounters from attack-start facts. Already delivered obstacle impacts retain settlement.

CombatAttackCalculator creates one scope for actual ordinary execution and gates knockback, thorn application, arrivals and kill-chain on reflection death while retaining cost, completed-hit bookkeeping and final settlement. The unsupported guard is removed from SoulEffectCalculator and CommandAttackProjection together with this integration. No Buffer/Factory/View/DB/delivery-contract change. The original blocked-route regression now asserts real reflection and input immutability; two new fixtures add 31 cases.

Final native focused113/113; all Core2770/2770 and five existing Framework publication fixtures53/53, combined2823/2823, no skips, exit0. Independent review has no actionable findings. Exact scope, intermediate fixture corrections, evidence and architectural checks are in plan section10 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#10-th2-attack-connection-and-ordering--2026-10-16`). Full Framework was not rerun or declared fixed. TH3 acceptance remains pending. Stopped for TH2 review; no staging, commit, push or external delivery.

### 5.33 Thorn Heart automated acceptance (2026-10-16, Phase TH3)

The user authorized TH3. No existing Core/Buffer/Factory/View/DB source or production asset changed. Three new test fixtures add seventeen cases for all-aim preview/AI omission with actual AI execution death, event causality/atomic publication, and actual artifact SO/table compilation plus standard single-copy inventory. The strengthened failure snapshot includes persistent main, ownership/soul counts, result identity and run outcome.

Initial valid native acceptance passes17/17; independent read-only review has no actionable P0-P2 finding. Final full Core2774/2774 and Framework4275/4372: the same97 TH0 failure identities/labels/messages, new0/changed0. All85 added reflection cases pass, including the seventeen TH3 cases and strengthened snapshots. Final evidence and remaining manual vectors are owned by plan section11 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#11-th3-acceptance--2026-10-16`). Manual Unity playback remains unchecked because native UI control is unavailable; automated checks do not claim manual final-result/scene-exit observation. No staging, commit, push or external delivery.

### 5.34 Fixed character maximum action cost (2026-10-16, Phase AC1)

The user's explicit AC1 instruction authorized the Core changes in the fixed maximum plan, section3 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#3-ac1--one-invariant-and-atomic-migration`). `Contracts.Common.CharacterActionCostRules.Maximum` now owns6. `UnitSpec`, `UnitDefinition`, `CharacterBaselineStats`, `CharacterBaselineStatsSnapshot`, `UnitStatsSnapshot` and `MainCharacterPersistentState` reject other supplied maxima. `UnitData.Validate()` enforces the same contract; `UnitDataSO` exports the constant and removes the editable field. Existing adapters, baseline calculators, creation/settlement/summon/copy paths already validate or forward the value and were not rewritten.

The developer source, temporary database, eight current Unit assets and valid test fixtures were migrated together. Existing current-point and insufficient-cost intent is retained or represented within0..6; no production attack/movement/terrain rule changes. Asset GUIDs, other serialized values, unregistered boss status and legacy Missing Script assets are preserved. Framework services, lifecycle, Buffer ownership and View read boundaries are unchanged; SSOT/SOLID/change-locality review is recorded in the plan.

Native Unity verification: all53 new tests pass; full Core2,814/2,814 pass. The full7,199-case run plus affected-fixture reruns leave precisely the97 AC0 failures unchanged by full name/label/message (7,102 latest passes). One random shop-path timeout passed its unmodified rerun; final registered cycle34/34 passes. This is combined verification, not a second full-suite pass. No compiler errors; two pre-existing warning kinds reappeared on compilation. Independent review has no remaining actionable finding. Plan section7 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#7-ac1-execution--2026-10-16`) links raw evidence and exact comparisons. AC1 is complete and stopped; AC2 and Cowboy implementation are unstarted. No staging, commit, push or external delivery.

### 5.35 Fixed maximum lifecycle/publication acceptance (2026-10-16, Phase AC2)

The user authorized AC2 and limited AC2 through CF2 to verification of new/modified logic and directly affected contracts. Maximum plan section8 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#8-ac2-execution--2026-10-16`) records the completed logic acceptance. Added only `CharacterActionCostLifecycleTests` and `CharacterActionCostPublicationTests` plus metas; no production or existing fixture edits were needed.

Approved native Unity6000.0.73f1 EditMode ran exactly these fixtures: Core21/21, Framework8/8, total29/29, exit0, no skips/inconclusive results or final compiler errors. They cover creation/summoning, spend4, current-point preservation, turn refill eligibility, capped/once/zero-actual Rider refunds, restricted actions, invalid maximum5 rejection before writes, developer definitions, actual registered SOs and real automatic boss summon publication. Initial fixture compile/input mistakes were corrected without weakening production contracts. Independent read-only review has no actionable finding. The plan links all raw logs/XML and failed-fixture evidence.

No whole-suite rerun or manual HUD/gameplay playback was performed under the user's instruction; AC1's97 baseline failures are historical reference, not revalidated here. All7,672 pre-existing captured product/config files, index and HEAD were preserved. SSOT/SOLID/FW/state ownership boundaries are unchanged. AC2 logic acceptance is complete and stopped; manual playback is deferred, CF0–CF2 remain unstarted. No staging, commit, push or external delivery.

### 5.36 Frenzy immutable contracts and shared authoring (2026-10-16, Phase CF-P1)

The user's explicit CF1-and-CF2 instruction authorized these Core-owned changes. Cowboy plan section12 (Git history: `2026-10-16-cowboy-frenzy-plan.md#12-cf1cf2-execution--2026-10-16`) records the exact file scope and evidence. Trait key120, immutable victim accumulated percentage, checked/floored/clamped Priority calculation and the existing marker registry/provider/codec/capture pattern are implemented. Intimidation apply/clear preserves Frenzy, Frenzy clear preserves Intimidation, and shared equality includes both. No base Priority mutation or duplicate stack counter.

One existing Unit trait authoring entry(default5) exports fresh pure Data; a single shared UnitSpecAdapter case serves battle and owned queries. No new table/service/drawer or operational Cowboy asset. Focused native Unity: new45/45 and direct-boundary12/12,57 unique passing cases, no skips/inconclusive results, both GREEN runs exit0. No full-suite or manual playback rerun. Pre-existing nullable-context warnings reappeared; no new warning source. Independent scoped review found no actionable issue.

All7,678 captured prior product/config files were checked: only the eight planned existing source edits differ, with exactly thirty planned new C#/meta files. Other prior work, index and HEAD preserved. SSOT/all-five-SOLID/FW/change-locality checks are in the plan. Stopped at CF-P1; CF3–CF7 remain unstarted. No staging, commit, push or external delivery.

### 5.37 Frenzy direct hits, free waves, expiry and acceptance (2026-10-16, CF-P3)

The user explicitly authorized CF3–CF6 together, including these Core changes, and requested focused verification without deliberate RED/full/redundant runs. Cowboy plan section13 (Git history: `2026-10-16-cowboy-frenzy-plan.md#13-cf3cf6-execution--2026-10-16`) is the execution/evidence source.

New Core owners: command-local FrenzyAttackLedger and iterative FrenzyAttackCalculator; immutable CombatFrenzyWaveEvent and derived FrenzyPriorityExpiry. Existing direct-damage/orchestration owners call the ledger before indirect reactions and drain waves after the one cost/refund settlement. Existing distance scaling moved unchanged to CombatDamageCalculator for ordinary/wave reuse. CombatEventKind appends FrenzyWave=30. No production Cowboy entity key or asset is activated.

Debuff schedule/turn/timeline owners now rescale once from final totals, select before own-turn expiry, and forecast one expiry at the next own start. Existing CombatDebuffApplication exposes GetFrenzyPriorityExpiry; existing CombatTimelineQuery PostBind reads committed Character facts alongside Combat facts. No factory, new service, Buffer owner or View product edit. Intimidation, reflection, flag-only wait and final publication contracts remain intact.

Native latest results:21/21 unique cases(20 new plus1 directly affected existing); no full-suite/manual playback. One independent-review P2(distance scaling omitted from waves) was corrected and its focused native case passes; no outstanding actionable review findings. Existing work, index and HEAD preserved. Stop at CF-P3; CF7 remains pending. No staging, commit, push or external delivery.

### 5.38 Cowboy key registration only (2026-10-16, partial CF7)

The user explicitly authorized only the remaining Core key registration. Added `EntityKey.Unit_Cowboy = 19`; changed the existing undefined-key rejection fixture from19 to9. No runtime rules or content assets changed. Existing UnitDefinition classification uses defined `Unit_` names, so no additional allowlist is needed. Approved native Unity compilation/EditMode: directly modified rejection method7/7 passed, exit0 ([evidence](../../../.superpowers/sdd/2026-10-16-cowboy-frenzy/CF7-key-only.xml)). Remaining CF7 DB/View content and manual acceptance stay pending. No staging, commit, push or external delivery.

**DB/View continuation:** The user requested a forwardable [Cowboy/Frenzy View handoff](2026-10-16-cowboy-frenzy-view-handoff.md). Remaining CF7 tasks now live there; the original plan keeps Core execution/key-registration evidence. This documentation transfer adds no production asset, Core or View implementation and has not been sent externally.
### 5.34 포효 범위 칸 게시 (2026-10-15, View 작업, 사용자 지시 "Core도 제가 고치기", 미전달)

- **요청:** "포효 시 대상이 없더라도 바닥 색 표시 해줘". 대상이 없으면 Core가 `CombatRoarEvent.Attack = null`만 보내, View는 포효 범위를 알 수 없었다. View가 Fan 모양을 계산하면 Core 규칙 복제라서, 사용자가 Core 수정을 명시적으로 지시했다(CLAUDE.md 예외).
- **Contracts:** `CombatRoarEvent.AreaCells`(`IReadOnlyList<CellCoord>`)를 추가했다. 대상 유무와 관계없이 포효 반응 공격이 덮는 칸이며(보드 안, 벽 가림 적용), `Attack`이 있으면 그 `AffectedCells`와 같다. 생성자 세 번째 인자는 선택(`null` → 빈 목록)이라 기존 호출이 깨지지 않는다. **표시 전용이며 판정에는 쓰지 않는다.**
- **Application(동작 변화 없음):**
  - `CombatAttackDelivery.DirectAffectedCells(reachable)`: 직접 전달 영향 칸 식(지정+추가 칸, 중복 제거, 행·열 순서)을 한 곳으로 뺐다. `ResolveDirect`도 이것을 쓴다.
  - `AttackSelectionEvaluation.EmptyAreaCells`와 `Failure(…, emptyAreaCells)`: 대상이 없어 실패한 위치 없는 지정이 덮었을 칸이다.
  - `CombatAttackQueries.Selection`: 가림 계산(`AttackOcclusionResolver.ResolveCells`)을 빈 대상 검사 앞으로 옮기고, `UnresolvedEmptyAll`/`NoEligibleTargets`와 위치 없는 `BlockedByWall` 실패에 칸을 붙였다. 실패 사유·차단 증거·성공 경로는 바뀌지 않는다.
  - `CombatAttackCalculator.Reaction(…, out emptyAreaCells)`: 호출자는 포효 하나다. 성공 시 포효 사건에 `result.AffectedCells`를 넣는다.
  - `BossRoarCalculator.TryRoar`: 대상이 없을 때 `new CombatRoarEvent(bossId, null, emptyArea)`.
- **시험:** `BossRoarTests`에 새 2개(범위 칸 = 공격 영향 칸이고 대상 유무와 무관, 빈 전체 지정 실패의 사유·차단 증거 유지)를 더하고, `NoTargetInRange_StillRoarsAndResets`에 범위 칸 단언 1줄을 더했다. 관리 실행: `BossRoarTests` 15/15, `ProjectL.Tests.Core` 전체 127개 묶음 실패 0(메서드 1277개 통과, Unity 전용 8개). **Unity EditMode 결과는 아직 없다.** 상세는 [보스 View 활성화 계획](../plans/2026-10-15-stage1-boss-view-activation-plan.md) §6.9.
- **View:** 대상 없는 포효는 `AreaCells`를 재생 강조 층에 칠한다. 칸이 비면 보스 칸으로 대신한다.

### 5.35 보스·가시심장 병합 충돌 해결 (2026-10-16, 사용자 명시 지시)

사용자가 보스와 아티팩트 기능을 모두 보존하도록 충돌 해결을 지시했다. `CombatAttackCalculator.cs`는 포효 범위의 `CellCoord`를 위한 `Contracts.Common`과 가시심장 처리의 `EntityId`를 위한 `Contracts.Ids`를 모두 유지한다. `CombatEvent.cs`도 포효 `AreaCells`의 `Contracts.Common`과 반사 발동 출처 검사의 `Contracts.CombatEffects`를 함께 유지한다. 들어오는 반사·위압·각도 지정 테스트 5곳이 기존 4인자 `Reaction`을 호출하므로, 5인자 계산에 `out _`로 위임하는 호환 overload를 추가했다. 계산/정산 본문과 순서는 변경하지 않는다. 보스 변경 기록은 번호 중복을 피해 §5.34로 옮겼으며 기존 두 View 문서 참조도 갱신했다.

사용자가 별도로 확인한 Targeting 테이블은 기존 1~4와 보스 포효 5, 발리스타 각도 지정 7을 함께 등록한다. GUID·키·개별 에셋 값은 바꾸지 않는다. 소울 드랍 §17과 초기 가시심장 요청 §18을 보존하고, 후속 Core 설계/구현 상태를 §19로 연결한다. 초기 요청의 미구현 표현은 당시 이력이며 현재 상태는 §19와 TH 계획의 실행 기록을 따른다. 커밋·push는 사용자가 수행한다.

Unity 6000.0.73f1의 기존 Editor에서 공개 TestRunnerApi로 검증했다. Core 전체 2776/2776, 보스·반사·타겟팅 관련 Framework 115/115가 통과했다. 별도 DataBaseAssetTests는 30/33으로, 1101 지형 좌표·2101 본부 Y·옛 LineShader 경로의 기존 실패 3건이 NM0 기준선과 이름/전체 메시지/중복 개수까지 같다. 새 실패는 없다. 충돌 해결 외 기능 본문 보존·테이블 GUID 및 키 중복 없음·수정 문서 링크 대상 148개 실존을 확인했다. 임시 검증 스크립트는 Assets에서 제거했다. 증거는 git-ignored `.superpowers/merge/2026-10-16-334e26ea-4481c2f8/`에 남겼으며 Play 육안 확인은 이번 검증에 포함하지 않았다.

### 5.39 Dynamite Hammer identity and authoring acceptance (2026-10-16, DH-P1)

The user explicitly authorized DH1 in the current Core-owned, system-only session. Added only `EntityKey.Unit_DynamiteHammer = 24` to `CoreCaculator/Contracts/Ids/EntityKey.cs`, preserving all existing values and reserved Shielder23. New temporary-authoring tests exercise real table validation and shared UnitSpec/attack-profile conversion without View, Render, stage, or production-content dependencies. Native Unity identity RED1/1 expected failure, then authoring GREEN5/5 pass, exit0; three existing compiler warnings, no new warning/error observed. See plan §10 (Git history: `2026-10-16-dynamite-hammer-plan.md#10-dh1-execution--2026-10-16-dh-p1-review-stop`) for XML/logs, preservation and architecture review. No gameplay rule, production SO/table, scene, View, or assembly-contract change; DH2 remains unstarted. Stopped at DH-P1, no staging/commit/push/external delivery.

### 5.40 Undead Cannon Cart identity and data (2026-10-16, Core-owned task)

The user clarified that this session is Core-owned and approved the unit data plus a View handoff. Added `EntityKey.Unit_UndeadCannonCart = 25`, preserving existing values and prior uncommitted `Unit_DynamiteHammer = 24`. No Core calculator or runtime service changed. The new saved Unit SO references its own targeting method8, initially50% and editable independently of Cactus method4(70%). Traits are empty. Unit/targeting live-table activation and View resources remain with the receiving View programmer. Configuration, inherited wall policy, SSOT/SOLID/FW review, native129-test result(126 pass /3 existing common-DB failures), and remaining integration are recorded in the [Undead Cannon Cart handoff](2026-10-16-undead-cannon-cart-view-handoff.md). No commit, push, or external delivery.

The user's subsequent clarification identifies this same unit as **Undead Minecart**, an **EnemyOnly** unit. Keep the existing `Unit_UndeadCannonCart` key and assets; this is not an additional third unit. The saved owner eligibility and attack fixture ownership are corrected, and the receiving-side route is enemy loadout/stage spawn rather than player acquisition. Current evidence is in the handoff's enemy-ownership follow-up.

### 5.41 Heavy ally protection runtime cutover (2026-10-16, Core-owned Phase 2)

The user explicitly authorized Core Phase 2 of the Heavy protection plan (Git history: `2026-10-16-heavy-ally-protection-plan.md`). Heavy now supplies one non-stacking incoming attack reduction to other living same-owner field characters covered by its authored offsets. Coverage is captured once per ordinary/reaction attack, once per Kill Chain addition, and once per Frenzy wave. Provider death does not invalidate the current capture. Non-attack damage and existing knockback immunity retain their separate paths.

Public construction changes: `HeavyProtectionCategoryRuleSpec(percent)` replaces `DirectionalArmorCategoryRuleSpec(percent, facing)`. `CategorySnapshot.CreateHeavy(percent, offsets)` exposes copied offsets and the common percentage; `DefenseFacing` and Heavy `MovedThisTurn` are removed. Legacy rule number 2 and activation number 20 remain reserved. `CategoryConfigData(percent)` and its SO no longer accept/store initial facing.

The only View source edit is the existing category fixture constructor in `ViewSystem/Dev/DevBattleInitializationSource.cs`: `new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0)` becomes `new HeavyProtectionCategoryRuleSpec(50)`. The same obsolete configuration argument was removed from `Factory/Runtime/Development/TemporaryCoreDatabase.cs`. These are compilation migrations; no View behavior, scene, camera, or lifecycle change is included. Shared constructor fixtures were updated as part of the same cutover.

The settled-area read service, Shotgun asset migration, and final View integration guide remain Phase 3. Do not implement area geometry in View from the new offsets. Phase 2 verification evidence and limitations are recorded in the plan's execution section. This record is local and has not been sent externally.

### 5.42 Heavy protection query and Shotgun content (2026-10-16, Phase 3)

The user authorized Phase 3 of the Heavy plan (Git history: `2026-10-16-heavy-ally-protection-plan.md`). `IInGameCoreSystemGroupService.HeavyProtection` now exposes `IHeavyProtectionReadSink.TryReadArea(EntityId, out HeavyProtectionAreaSnapshot)`. It reads current settled Buffer state, returns copied world cells, and distinguishes unavailable `false/null` from a valid fully clipped `true/empty` area. It throws before binding and on broken invariants/overflow. Registration follows the existing service/module/PostBind lifecycle. Systems calls a narrow pure Application facade to reuse Domain geometry without changing assembly references.

The existing Shotgun SO was saved through Unity SerializedObject with Heavy30, HP40, attack6, priority30, attack range2, move cost4, attack cost2, and six literal `+Y` offsets: `(-1,1),(0,1),(1,1),(-1,2),(0,2),(1,2)`. Its GUID, targeting method2, distance trait `[150,50]`, other approved preserved fields, table membership, and View references remain. Shared targeting and Gravekeeper were not edited.

The [English View handoff](2026-10-16-heavy-protection-view-handoff.md) records exact access, availability/failure/freshness contracts, coordinate convention, removed Heavy fields, gameplay boundaries, verification, and View-owned acceptance. Phase 3 regression is 406/409 with only the same three baseline DB asset failures; all new Heavy/query/content cases pass. Final feature review and any follow-up evidence are in the implementation plan. Interactive real-field highlighting/orientation and View adoption remain unverified acceptance work. No View rendering code, scene, FieldCamera, or Architecture implementation is included. The document has not been sent externally; no commit or push was performed.

### 5.43 Manual initial main deployment and one extra initial sub draw (2026-10-17)

The user identified themselves as the Core programmer and explicitly authorized changes to Core: start each battle with the main available for manual deployment from Deck, increase the default initial sub draw from three to four capped by owned sub count, and preserve existing night-end behavior. This authorization is specific to initial preparation/draw and its acceptance; it does not authorize unrelated Core changes.

Core implementation and focused native Unity verification are complete. `BattlePreparationCalculator` now prepares the main in Hand slot 0 and subs in slots 1..N, with main-inclusive capacity admission. The default and saved operational `StartingHandSize` change from 4 to 5 while preserving its main-inclusive total meaning, existing shuffle/cap behavior and explicit custom totals. Existing main placement/entry admission and night-end implementations are reused unchanged. No production View or Framework lifecycle/registration code was changed.

Native Unity 6000.0.73f1: complete Core 3,098/3,098 pass; direct Framework 99 pass / 3 pre-existing DB failures / 102 total; registered service cycle/publication 45/45 pass. All 12 new pure-Core behavior cases pass. The first broad pre-implementation baseline crashed in native rendering (`GfxDevice::DrawSharedGeometryJobs`) before XML completion, so a completed full Framework/graphical run is not claimed. The [View handoff](2026-10-17-main-deck-initial-placement-view-handoff.md) includes raw evidence, the exact three existing DB failures, manual/graphical limits, SSOT/SOLID/FW/change-isolation review and the remaining Deck UI/input work. Independent read-only runtime review found no actionable issue. No staging, commit, push or external delivery occurred.

### 5.44 서비스 조회 키의 Factory 계층 분리 (2026-10-17, 미전달)

사용자 요청으로 키의 파일·네임스페이스를 실제 Factory 계층에 맞췄다. `InGameCoreSystemGroup.cs`는 `InGameSystemServiceKey`를 `ServiceKeys.Runtime.InGame.Core.InGameCoreSystemServiceKey`로 바꾸고 using을 갱신했다. 기존 14개 서비스 키의 필드 이름·인터페이스·등록·조회 동작은 동일하다.

같은 CoreSystem 아래 `ArtifactEditingService`, `ChapterProgressionCoordinator`, `CharacterRosterService`, `CombatProcessingService`, `DatabaseBattleInitializationSource`, `RecoveryCommandProcessingService`, `RecoveryStageService`, `RecoveryStateQueryService`, `ResultQueryService`, `RunStartService`, `CharacterShopService`, `StageCommandProcessingService`, `StageGeneratorSystem`은 루트 Buffer/DB 키를 참조하는 using만 변경했다. CoreCaculator의 Contracts·Domain·Application과 계산·규칙·상태·서비스 등록은 수정하지 않았다. 전체 설계·Task·검증은 [조회 키 분리 설계](../specs/2026-10-17-service-key-layers-design.md)에 기록한다.

### 5.45 Percentage defense and final attack minimum (2026-10-18)

The user identified themselves as the Core programmer and explicitly requested this implementation. This authorizes the narrow Core change despite the normal View-only ownership boundary. No standalone design or task-plan document is needed for this bounded change. The current policy is experience design §3.4 (Git history: `2026-10-18-experience-level-up-design.md#34-메인-랜덤-성장-x5-x4`), superseding fixed subtraction and the earlier defense-stage-only minimum.

Scope: percentage defense in `TargetDamagePipeline`, final calculated attack minimum after Heavy/terrain, all ten authored Unit defense values set to 5, focused regression tests. Environmental damage and held shield absorption retain their own rules. View displays published values; it does not calculate defense. Verification is recorded here after execution.

**Implemented and verified locally (2026-10-18):**

- RED: native Unity 6000.0.73f1, 83 selected cases, 71 passed / 12 expected failures against the old fixed-subtraction/minimum/content policy. Evidence: `Temp/DefenseVerification/red.xml` and `red.log`.
- Full regression: 8,422 cases, 8,201 passed / 220 failed / 1 inconclusive. Two failures were obsolete expectations directly affected by this change: `PiercingShotCommandTests.FullyProtectedA_ConsumesEncounter_WithUnarmedAllyProvider` (Heavy 100% now leaves 1) and `BattleDefinitionAdapterTests.ReadUnit_PreparedBossPreservesTraitsBehaviorAndStats` (base defense now 5). Both were corrected; the piercing case additionally verifies Preview agreement. Evidence: `Temp/DefenseVerification/green.xml`, `green.log`, and `failures.txt` (all 220 names and messages, before these corrections).
- Final targeted regression: **Core 3,223/3,223 passed; relevant Framework 138 passed / 3 failed**, total 3,364. This includes all authored units exporting defense 5, percentage/overflow boundaries, zero-input preservation, Heavy and terrain minimum, attack Preview/Commit agreement, and existing Buffer publication/query tests. Evidence: `Temp/DefenseVerification/final.xml` and `final.log`. No compiler errors; the same three pre-existing source warnings appeared in RED and subsequent compilation.
- Remaining DB failures: `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition` (stage 1101 has extra source 213), `EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab` (stage 2101 expects 8, receives 0), and `LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride` (missing `LineShader.mat`). These are not defense assertions and were left unchanged.
- The other 218 failures from the broad run, including those three DB failures, remain unresolved. No full pre-change baseline was run in this session, so this record does **not** claim all 218 were proven pre-existing or that the complete suite is green. The inconclusive case was `ConnectedWallBootSmokeTests.DatabaseLiveBoot_DisplaysTwelveWallsAndFiveEnvironmentItems` because its randomly generated first route had no combat node.
- Independent read-only review found no actionable runtime issue. Unity-generated FMOD catalog scan path, dynamic fallback font, and render-texture serialization changes were restored after tests. No new tracked file/metafile, staging, commit, push, or external delivery. Manual gameplay was not tested.

**Follow-up: one final minimum owner (2026-10-18, explicit user request):**

- Removed the intermediate minimum from `TargetDamagePipeline.Calculate` and deleted `EnsureAttackMinimum`. `CombatDamageCalculator.Attack` now applies the minimum directly, exactly once, after terrain and immediately before returning. Public API signatures are unchanged; no replacement API was added.
- Intermediate pipeline tests assert raw zero after full defense/Heavy reduction. Existing final attack, terrain, Heavy-protected piercing, zero-input and Preview/Commit tests retain their gameplay expectations. `TargetDamageBreakdown.FinalDamage` documentation now distinguishes the current calculation-stage result from the final `Attack` result.
- RED: 51 selected tests, 45 passed / 6 expected failures (`Expected 0, actual 1`), demonstrating the redundant intermediate minimum. GREEN: **3,267/3,267 passed**, comprising all **3,223 Core** tests and **44 Framework** command/preview/boss publication tests. Native Unity 6000.0.73f1 exit 0, no compiler errors. Evidence: `Temp/DefenseMinimumOnce/red.xml`, `red.log`, `green.xml`, `green.log`.
- Independent read-only follow-up review found no actionable issue. This run did not repeat the full graphical/DB suite or resolve the earlier 218 remaining failures. Authored defense values remain 5. Unity-generated changes were restored and verified absent from the diff; no commit or push.

**Follow-up: one pipeline calculation per attack target (2026-10-18, explicit user request):**

- `CombatDamageCalculator.Attack` now calls `TargetDamagePipeline.Calculate` once. For character targets, it reuses the existing `EvaluateIncomingDamage` result's `EffectiveReduction` and `FinalDamage` through the existing `TargetDamageBreakdown` constructor. No second splash, matchup, or defense calculation, repeated Heavy subtraction, new API, or signature change is introduced. Structures reuse the first pipeline result.
- Terrain receives the post-Heavy exact damage. The final minimum remains owned by `Attack`, preserving zero intermediate damage even when the returned attack minimum is 1. Held shield absorption and environmental damage are unchanged.
- Extended the existing stage-fact test with primary/splash Heavy 100% cases and assertions for `DamageAfterReceivedPercentages`, covering accidental reuse of pre-Heavy damage or minimum-clamped intermediate damage. Before the refactor, all 13 `EnvironmentDamageTests` passed; this is a behavior-preserving baseline, not a RED run. Evidence: `Temp/DefenseCalculateOnce/baseline.xml` and `baseline.log`.
- After the refactor, native Unity 6000.0.73f1 exited 0: **3,269/3,269 passed**, comprising **3,225 Core** tests and **44 Framework** command/preview/boss publication tests, with no compiler errors. Evidence: `Temp/DefenseCalculateOnce/green.xml` and `green.log`. The full graphical/DB suite was not repeated; the earlier unresolved failures are not claimed fixed. No commit or push.

**Follow-up: remove the obsolete pipeline reduction argument (2026-10-18, explicit signature-change authorization):**

- Removed `incomingReduction` from both existing `TargetDamagePipeline.Calculate` overloads, their forwarding call, validation, cap, and subtraction. The pipeline now returns post-defense damage with `IncomingReduction` initially 0. No compatibility overload, wrapper, helper API, or replacement type was added. The production caller passes only effective defense.
- The removed logic was previously used by the second pipeline call. Once that call was removed, production always passed 0 while direct pipeline tests still passed nonzero reductions. This cleanup removes that leftover responsibility. The real incoming-effect engine's cap/subtraction remains necessary and unchanged; `TargetDamageBreakdown.IncomingReduction` remains a consumed result fact, not an obsolete input.
- Updated all direct callers explicitly. Pipeline tests now verify splash, matchup, defense and zero inputs. Effect-engine tests verify actual reduction, remaining damage and combined-contribution capping. Attack tests verify defense 5 followed by Heavy 50%/100%, primary/splash cases, and positive input truncated to zero by splash. Existing terrain, held-defense, Preview/Commit and publication regressions remain in scope.
- Final audit covers the full defense diff from `f385afda` through the working tree: production declarations, all pipeline references/callers, direct/frenzy/killchain attack paths, remaining intermediate facts, ten authored defense values, and tests. No further actionable duplicate computation, unused reduction input, new API/type, or bypass path was found. Independent read-only review reached the same conclusion; its optional positive-input/zero-splash test suggestion was incorporated. Prior entries describing unchanged signatures are historical results before this explicit signature cleanup.
- Final native Unity 6000.0.73f1 verification: **3,428 total, 3,427 passed / 1 failed**; **Core 3,227/3,227 passed**, Framework 200/201 passed. This includes all authored units exporting defense 5, changed authoring/adapter suites and command/preview/boss publication. No compiler errors. Evidence: `Temp/DefenseIncomingCleanup/final.xml` and `final.log` (exit 2 due to the failed test). The preceding run, before adding the zero-splash boundary case, had the same single failure (`green.xml`/`green.log`; that filename does not imply a passing run).
- Remaining failure: `BattleDefinitionAdapterTests.StageEntry_PreparationFailureLeavesSelectionUnconfirmed(False)` expects an exception for `MainAnchor=(99,99)`, but current preparation puts the main in hand slot 0 without using that anchor, as documented in the 2026-10-17 main-placement handoff. The failing method and preparation path are unchanged by this defense feature, and the failure occurs during entry before attack calculation. It was left outside this cleanup; no full pre-change run was performed to establish a broader baseline. The complete graphical/DB suite was not rerun and earlier failures are not claimed fixed. Unity-generated FMOD/font changes were restored; no staging, commit or push.

SSOT/SOLID/FW/change isolation: the pure Core pipeline owns the defense formula; the existing effect engine supplies evaluated incoming reduction and remaining damage; `CombatDamageCalculator.Attack` composes those facts and alone owns the final attack minimum; authored SO values own starting balance; persistent main state owns earned growth. The existing shared Preview/Commit path is retained. No new service/interface, Factory assembly, lifecycle, Buffer mutation path, or View dependency is introduced. S/O/L/I/D boundaries remain unchanged; FW-001–016 registration and ownership contracts are unaffected. Defense tests and relevant content tests exercise the changed feature without new cross-feature runtime coupling.

### 5.46 필드 유닛 공격 범위 조회 (2026-10-19, View 작업에서 사용자 지시로 구현, 미전달)

사용자가 "유닛 호버 시 그 유닛의 공격 범위"를 요청하고 대상을 모든 필드 유닛(적·차례가 아닌 아군 포함)으로 정하면서 **Core 조회도 직접 구현하라고 명시적으로 지시했다**(2026-10-19). 기존 `AttackRange` 미리보기는 행동 질문(현재 차례의 플레이어 행위자, 전투 국면)이라 조회로 쓸 수 없다. 설계는 [호버 공격 범위 설계](../specs/2026-10-19-combat-unit-hover-attack-range-design.md) UR2, Task·검증은 [계획](../plans/2026-10-19-combat-unit-hover-attack-range-plan.md) U-1(§7.2).

바꾼 Core 파일:

- 새 계약 `Contracts/Results/UnitAttackRangeResult.cs`: 성공·실패 이유(`AttackFailureReason`)·유닛 ID·범위 칸. 조준 목록·AP 판정은 담지 않는다(행동 답은 `AttackRangeResult`).
- `Contracts/Combat/CombatPreviewResult.cs`: `UnitRange` 칸과 `ForUnitAttackRange`. "내용은 하나만" 검사에 새 칸을 더했다.
- `Application/Combat/CombatAttackQueries.cs`:
  - `Actor`의 유닛 쪽 검사(필드·생존 → 기본 공격 가능 → 여러 칸 보류)를 `UnitReady`로 떼었다. `Actor`의 검사 순서·실패 이유는 그대로다.
  - `Range` 본문의 칸 계산(지정 방식 모양·지형 사거리 보정·보드 안·전달 검사·대포 인접 금지·정렬)을 `RangeCells`로 떼었다. `Range`의 결과는 그대로다.
  - 새 `UnitRange`: 종료면 `StageEnded` → 찾기(`EntityNotFound`) → `UnitReady` → `RangeCells`. 차례·국면·소유자는 보지 않고, 지형은 **플레이어 시점**(`VisibleTo(Owner.Player)`)으로 투영한다(설계 Q-UR2 A, 플레이어가 모르는 지형이 사거리 차이로 드러나지 않게).
- `Application/Combat/CombatCommandCalculator.cs`: `GetUnitAttackRange`.
- `CoreSystem/CombatSystem/CombatPreviewService.cs`: 종류 switch 두 곳에 `UnitAttackRange`(명령 종류 매핑 `Attack`, 결과 `ForUnitAttackRange`). 결과 switch의 기본 갈래가 배치 미리보기라 새 종류를 명시했다.
- View 정의 Buffer 칸(Core 파일 아님): `CombatViewPreviewKind.UnitAttackRange = 8`, `CombatViewPreviewData.UnitAttackRange(EntityId)`.

바뀌지 않는 것: 행동 범위·공격 미리보기·명령 계산·AI, Buffer 게시 경로, Plan·Job(새 종류도 기존 미리보기 Job이 처리하며 실패도 응답으로 게시한다).

검증(2026-10-19): MSBuild 오류 0·새 경고 0, Unity 컴파일 성공. 관리 실행 Core 전체 3,249개에서 기존 시험 상태 변화 0, 새 조회 시험 12개 PASS(현재 행위자·적 자기 차례의 `GetAttackRange`와 칸이 같음 포함). Framework는 4,837/5,723개까지 상태 변화 0, 새 미리보기 서비스 시험 2개 PASS. 자세한 내용은 계획 §7.2.

### 5.47 고지대 → 저지대 공격의 Wall 무시 (2026-10-20, Core 작업, View 계약 불변, 미전달)

기획: 고지대에서 저지대로 공격하면 사이에 Wall이 있어도 무시한다. 설계는 고지대 Wall 무시 설계 (Git history: `2026-10-14-high-ground-wall-bypass-design.md`)(D1~D9), Task·실행 기록은 계획 (Git history: `2026-10-14-high-ground-wall-bypass-plan.md`) H1·H2.

규칙: 직격 선분의 출발 칸이 고지대이고 맞는 칸이 고지대가 아니면 그 선분의 Wall 검사를 생략한다. 파급(스플래시·가시밭)은 지금처럼 착탄 칸에서 검사하고, Wall 칸 자체는 여전히 조준·피격할 수 없다. 관통은 칸마다 판정해 Wall 너머 저지대 칸을 지나가다 첫 고지대 칸에서 멈춘다. 고지대 판정은 그 연산의 환경을 따른다(미리보기·범위·AI는 관찰자 공개 환경, 실행은 전체 환경).

바꾼 Core 파일(모두 internal 서명): 새 `Domain/Combat/AttackElevation.cs`(공식의 유일한 위치), `EnvironmentTerrainRules`(`TerrainCells` + 지형 판정 공유), `AttackOcclusionResolver`(`CheckDirect`·`ResolveCells`에 elevation), `PierceTargetCollector.CollectSingle`, `CombatAttackQueries`·`CombatAttackDelivery`·`CombatAttackCalculator`·`EnemyMovementPathfinder`·`FollowUpAttackExecutor`·`FrenzyAttackCalculator`(광란·추가 공격도 같은 규칙, 사용자 확인).

View에 보이는 변화(계약은 그대로): 고지대 공격자에게 Wall 너머 저지대 조준이 `AttackAims`에 들어오고, 무시된 선분은 `AttackBlockedContact`를 만들지 않아 차단 증거(궤적 X 표시 등)가 줄어든다. View는 Core 결과를 그대로 그리면 되고 규칙을 다시 계산하지 않는다(P6). 숨은 고지대에서는 미리보기 허용·실행 `BlockedByWall` 거절이 생길 수 있다(설계 D8, 상태 변화 없음).

바뀌지 않는 것: public Contracts, Buffer, View, SO, 씬, `AttackOcclusionSpec`, 피해 감소(저 → 고 70%)·이동 비용·넉백 공식, 호버 유닛 공격 범위(§5.46, 범위 칸은 Wall 가림을 보지 않음).

검증: Core 시험만(View 시험은 사용자 결정으로 이번에 제외). 새 `AttackElevationTests`·`HighGroundWallBypassTests`, `EnvironmentTerrainRuleTests` 피해 감소 사례, 호출부만 고친 기존 시험 10개. Unity Test Runner 결과 대기.

### 5.48 같은 서브 유닛 중복 보유 금지 (2026-10-20, Core 작업, View 계약 불변, 미전달)

사용자 요청: 상점에 이미 가진 서브 유닛이 다시 뜨지 않게 한다. 설계는 중복 서브 금지 설계 (Git history: `2026-10-20-unique-sub-ownership-design.md`)(U1~U8, Q-U1 A·Q-U2 B·Q-U3 B·Q-U4 A), Task·실행 기록은 계획 (Git history: `2026-10-20-unique-sub-ownership-plan.md`).

규칙: 모든 플레이어 유닛은 같은 정의(키)를 하나만 보유한다. 서브의 카테고리당 2개 한도(런 덱 설계 D3)는 그대로다. 판정은 `CharacterRosterRules.ValidateOwnership` 한 곳이고, 상점 후보·상점 구매·새 Run 시작 로스터·직접 획득이 모두 이 함수를 지나 자동으로 따른다. 실패 이유는 기존 `OwnershipLimitReached` 그대로다.

바꾼 Core 파일: `Application/Roster/CharacterRosterRules.cs` — public 상수 `PerDefinitionLimit`(1)·`SubCategoryLimit`(2) 추가, **public `GetOwnershipLimit` 삭제**(호출처는 시험뿐이었음). `Contracts/Roster/RosterAcquireFailureReason.cs` 주석 한 줄.

View에 보이는 변화(계약은 그대로): 상점에 이미 가진 유닛이 제시되지 않는다. **지금 운영 데이터에서는 상점에 유닛이 하나도 제시되지 않는다** — `Shop_Default`의 유닛 후보가 ShotGun 하나이고 새 Run이 ShotGun을 이미 준다. 상점 후보 추가는 나중에 기획 결정 뒤(Q-U2 B). 구매 실패·로비 출발 문구("보유 한도에 도달했습니다.")는 그대로다.

바뀌지 않는 것: public 실패 이유 enum, Buffer(`CharacterBuffer`는 ID별 저장만), 덱 규칙(`DeckRules`), View, SO·운영 데이터.

시험: Core 규칙·상점 계획기·구매 규칙 시험, Framework 공용 고정 데이터 `CharacterRosterDatabase`의 로드아웃(Cowboy 2 → Cowboy 1 + Dynamite Hammer 1)과 같은 서브 둘을 버퍼에 넣던 사용자 작성 시험들을 고쳤다(목록은 계획 실행 기록). 같은 키 두 장 전제 시험 둘은 사용자 결정으로 삭제. **다른 작업자 시험(보고만, 규칙을 지나지 않아 통과에는 영향 없음):** `DeckButtonUITests`·`CombatDeploymentConfirmationTests`(Akimbo를 서브로 둘, 현강님), `ShopPopupModelBuilderTests`(ShotGun 둘, 호진님) — 플레이에서 생기지 않는 보유 구성이다. Unity Test Runner로 사용자 확인(해결).

### 5.49 관통 진행은 사거리와 무관하게 보드 끝까지 (2026-10-20, Core 작업, View 계약 변경 1건, 미전달)

사용자 결정: Ballista 사거리를 줄여도 관통은 공격이 진행되면 맵 끝까지 간다. 사거리(모래폭풍 -1 포함)는 조준 칸만 제한한다. 정책 원본은 Ballista 관통 설계 (Git history: `2026-10-11-ballista-piercing-shot-design.md`) 머리말의 2026-10-20 개정이다. 지금 운영 Ballista는 사거리 99라 화면 변화는 없고, 사거리를 줄이는 순간부터 차이가 난다.

규칙: 관통은 첫 Wall(종점 고정)·살아 있는 구조물·보드 끝·피해 예산 소진에서만 멈춘다. 판정은 `PierceTargetCollector.CollectSingle` 한 곳이고 공격 실행·미리보기·적 AI·추가 공격이 모두 `CombatAttackDelivery.Resolve`를 지나 같이 따른다.

바꾼 Core 파일: `PierceTargetCollector.CollectSingle`(`range` 인자 삭제, internal), `CombatAttackDelivery.Resolve`(쓰지 않게 된 `environment` 인자 삭제, internal)와 호출 셋(`CombatAttackCalculator`·`EnemyMovementPathfinder`·`FollowUpAttackExecutor`), **public `PiercingShotStopReason.Range`(= 4) 삭제**(번호는 다시 쓰지 않음, `BoardEdge` = 5 그대로).

View에 보이는 변화: `PiercingShotDelivery.StopReason`에 `Range`가 더 이상 오지 않는다. View 코드는 이 값을 분기하지 않았다(확인). Buffer·SO·씬 무변경.

다른 작업자 시험(사용자 결정으로 이쪽에서 고침, 컴파일 유지): `Framework/CombatBallistaAttackTests.EmptyOrNoHitPath_CompletesAtProvidedEndpoint`의 `[TestCase(PiercingShotStopReason.Range)]` 한 줄 삭제(용근님), `ConnectedWallCombatTests`의 `CollectSingle` 호출에서 사거리 인자 `5` 삭제(용근님 줄, 결과 같음). 사용자 작성 시험: `PiercingShotTraceTests`(사거리 표 시험 삭제·여덟 방향 시험을 7×7 보드 끝으로·종료 사유 `BoardEdge`), `PiercingShotContractTests`(`Range` → `BoardEdge`, 값 4 거절 추가), `StaticTerrainIntegrationTests`(모래폭풍 관통 시험 → `Sandstorm_ReducesPierceAimOnly_ShotStillRunsToBoardEdge`, 조준 제한은 그대로, 피격 2 → 3·`BoardEdge`), 호출부만 `AttackDeliveryHitTests`·`AttackTargetAdmissionTests`·`CombatItemDirectDamageTests`·`ThornHeartReflectionDirectTests`·`AttackElevationTests`, 새 `PiercingShotCommandTests.ShortRange_LimitsAimOnly_ShotStillRunsToBoardEdge`(+ `PiercingShotTestData.Input`에 `appointRange` 선택 인자). Unity Test Runner 결과 대기.

### 5.50 Owned ThornField description projection (2026-10-23, authorized Phase B)

The user explicitly authorized Phase B of the [Enhanced Trait Display Fix plan](../plans/2026-10-23-enhanced-trait-display-fix-plan.md#52-phase-b-implementation-and-verification--2026-10-23), including the narrow Core read-contract change. This entry records local delivery; no external handoff was sent.

- `OwnedCharacterStatsSnapshot` adds get-only `ThornFieldTraitStatusSnapshot ThornFieldDescription`. Its constructor now requires that seventh argument explicitly; pass null only when `Stats.TraitKeys` does not contain ThornField. Both presence mismatches throw `ArgumentException`; existing ID/key/role/level/soul/null validation remains.
- The payload contains configured damage, lifetime, defense reduction and tick phase from the owned instance's effective definition. It is not remaining combat duration or mutable trait state. `OwnedCharacterStatsCalculator.CalculateForMains` copies those fields after per-instance enhancement resolution; no shared definition is modified.
- Existing consumers still receive the same ID-bearing snapshot through `OwnedStatsProjection` and `CharacterStatsQueryService`. The existing Reader refreshes it after Buffer-owned `RosterRevision` changes. No new DTO, store, cache, service, root export, or runtime trait initialization was added.
- Four existing no-Thorn test constructors were migrated with explicit null in `RunDeckContractTests`, `DeckButtonUITests` (two), and `OwnedStatsUITestServices`. No View product formatter/consumer, Buffer, Job, Framework, Factory, ServiceKeys, SystemGroup or asmdef was modified.

Verification: six new cases pass, including same-key base/enhanced isolation, both invariant mismatch directions, non-production values, and the actual Buffer-to-query-to-reader refresh path. The required calculator/query/composition areas pass 54/55; adding constructor consumers gives 72/80. All eight failures reproduce the exact pre-change test names and messages. The plan section 5.2 lists each failure and XML/log evidence. This is not a fully green gate or manual UI acceptance.

At the Phase B delivery boundary, View formatting was still pending. The subsequently authorized **Phase C delivery (2026-10-23)** now passes `ThornFieldDescription` from owned `UnitCardUI` into the shared `UnitCardTraitContent` formatter; combat retains its full-snapshot route. View formats published damage/lifetime/reduction without reading recipes, inferring enhancement from CharacterKey, or modifying the global base description. No additional Core/Buffer/Framework contract or ownership change was required.

Phase C adds 19 passing cases, including real Buffer/query/card revision refresh, pinned/reopened and same-key/new-Run isolation, actual combat consumer checks and eight operating-layout cases. The selected Unity run is 101/123; all 22 failures have exact baseline name/message matches. Four existing ability TMP components in `UnitCard.prefab` and `Combat.unity` use autosizing 20-36 after rendered overflow was reproduced. Play reached Boot -> Lobby -> StageSelect -> Combat, but complete purchase-to-next-battle manual acceptance remains open. Detailed evidence, limitations and the stop boundary are in [plan section 5.3](../plans/2026-10-23-enhanced-trait-display-fix-plan.md#53-phase-c-implementation-and-verification--2026-10-23). No external message was sent.
### 5.51 로비 Job·Plan 재구성으로 고친 천명님 시험 (2026-10-23, View 작업, Core 무변경, 미전달)

사용자 지시로 로비 실행 계층(옛 `LobbyFlow`·`LobbyDepartureFlow`·`ArtifactEditFlow`)을 트리거 + `LobbyPlan.Decide` + Job으로 바꿨다. 설계 [lobby-job-plan-restructure-design](../specs/2026-10-11-lobby-design-Hojin.md#11-실행-계층-lifecycleplanjob), Task·실행 기록 [계획](../plans/2026-10-11-lobby-plan-Hojin.md#314-lj-로비-실행-계층-jobplan-재구성). **Core 파일은 바꾸지 않았다.**

천명님 시험에 닿은 것(우리 변경으로 깨져 고침):
- `StageRunOriginRegressionTests` 두 사례(`LobbyNewRunJob_CancelsStageClickBeforeCoreRunReplacement`·`CancelledLobbyDeparture_PreservesStageClickWithoutReplacingRun`). 출발 요청의 `MarkQueued`·`IsJobCompleted`가 없어지고(설계 Q-LJ8 A) Lobby 루트의 `NewRun` 등록이 없어져(Q-LJ5 A) 깨졌다. `MarkQueued` 줄을 걷고, 새 Run Job(`LobbyRunStartJob`)을 직접 만들어 등록·실행하며(파일 안 작은 사실 읽기 대역 `OriginLobbyProgress`, 전투 유무만), `IsJobCompleted` 단언은 `Status == Prepared`로 바꿨다. 확인하는 내용(새 Run이 맵 클릭을 취소함 / 취소된 출발은 Run을 바꾸지 않음)은 그대로다.
- `LobbyDepartureTests.cs`는 대상(옛 Flow)이 없어져 지웠다(호진님 파일, 천명님이 `f6a18f3f`에서 대역 로스터 키를 바꾼 기록이 있다). 지키던 규칙은 설계 §8.2 재생 확인 목록으로 옮겼다.

Core 계약에 기대는 곳(알림): 새 Run 호출 결과의 로스터 대조가 옛 Flow의 다음 Tick에서 **로비 새 Run Job의 호출 직후**로 옮겨졌다(Q-LJ9 B). `IRunStartService.StartNewRun`이 같은 호출 안에서 로스터·메인 지속 상태를 Buffer에 설치한다는 것(`ProgressTransactions.PublishNewRun`)을 전제로 한다. 이 게시를 호출 뒤로 미루게 바뀌면 이 확인이 실패하니 알려 주시면 맞추겠다.

### 5.51 Owned Pull description projection (2026-10-26, authorized Phase D)

The user authorized Phase D of the category/unit-trait plan. Two existing Core read-model files were changed: `CoreCaculator/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` adds optional immutable `PullDescription` with trait/descriptor correspondence validation; `Application/Roster/OwnedCharacterStatsCalculator.cs` maps the existing Pull definition value into that descriptor. This follows the current Hunter's Mark/Thorn description path so owned cards do not read InGame DB or infer defaults. No gameplay formula, Buffer state, publication, Job or service contract changed. Existing View deck/card models carry the descriptor into the shared formatter. Real owned projection/formatter tests at0/2/7 and full Core/CoreIntegration regressions passed; Framework/manual/art remain unrun. Detailed signatures, playback ordering and evidence: [Phase D handoff](2026-10-26-category-unit-trait-core-view-handoff.md). No commit/push or external message.

### 5.52 끌어오기 공개 미리보기의 벽 형상 예외 수정 (2026-10-26, View 작업에서 사용자 지시로 Core 한 줄 수정, 미전달)

**증상(사용자 재생 보고):** 전투 중 끌어오기 유닛(Cowboy)으로 공격 모드(E 키)에 들어가 끌어오기가 필요한 대상을 겨누면 `ArgumentException: 벽은 활성 상태이며 양측에 공개되어야 합니다. 원본: 202@(1,5)`가 나고 오류 경계가 게임을 멈췄다. 스택은 `CombatPreviewJob` → `CombatPreviewService.ProcessLatestPreview` → `CombatAttackCalculator.Execute`(`:128`) → `PullAttackCalculator.RebuildDelivery`(`:111`) → `WallGeometryBuilder.ValidateWalls`(`:53`)다.

**원인:** `RebuildDelivery`가 벽 형상을 `arrival.Environment`에서 다시 만들었다. 공개 미리보기에서 이 환경은 `MovementEnvironmentFacts.VisibleTo(관찰자)` 투영이라 모든 지형의 `KnownTo`가 관찰자 하나로 바뀌어 있다. 그런데 벽 검사는 양측 공개를 요구하므로, 벽이 하나라도 있는 맵이면 늘 예외가 난다. `arrival.CurrentInput().State.Environment`도 같은 투영이다. 첫 계산(`CombatAttackCalculator.Walls`)과 조회(`CombatAttackQueries.Selection`, "벽은 공개 지형이다… 형상에는 원본을 전달한다" 주석)는 원본 환경을 쓰는데, 끌어오기 재계산만 이 규칙을 빠뜨렸다(`4bd6b574`).

**수정(`Application/Combat/PullAttackCalculator.cs`, `RebuildDelivery`):** 벽 형상을 새로 만들지 않고 최초 계획의 형상 `originalPlan.Walls`를 그대로 쓴다. 근거 주석 두 줄을 붙였다.
- 최초 계획의 형상은 일반 공격(`Calculate`)·반응 공격(`Reaction`) 모두 원본 환경으로 만든 것이다(`Walls(input)`).
- Application·Domain에는 명령 도중 `StaticTerrainFact`를 새로 만드는 코드가 없다. 그래서 공개 미리보기가 아닌 실제 명령의 결과는 바뀌지 않는다.
- 사거리·고지대(`elevation`)·선택은 지금처럼 투영 환경을 쓴다.

**검증:** 컴파일·시험은 사용자가 Unity에서 확인한다(컴파일 미확인). 새 시험은 만들지 않았다. 벽이 있는 맵에서 끌어오기 미리보기를 확인하는 시험을 Core 쪽에 하나 더해 두시면 좋겠다.


### 5.52 Core 표시 데이터의 Buffer 경계 수정 (2026-10-27, 사용자 지시로 구현, 미전달)

사용자 요청: UI/View가 Core 데이터를 직접 조회하는 위반 경로를 수정한다. 이번 지시는 해당 Core·Buffer 계약 및 조립 변경을 포함한다.

- `CombatTurnFacts.PlayerTurnsPerNight`를 Core 생성 시 채운다. NightCheckerUI는 CombatStateBuffer에 게시된 턴 사실에서 완료 턴과 상한을 읽는다.
- `CharacterStatsBuffer`가 계산 결과와 원본 버전을 보관한다. UI는 Buffer의 Read/Request Sink만 받는다. `CharacterPresentationPublisher`는 Core Tick에서 요청된 최신 원본을 계산·게시한다. 읽기 중 Core/DB 콜백은 없고 첫 게시·원본 변경 후에는 Pending이다.
- `DeckEditBuffer`로 View 요청(Read/Rebase/Evaluate/Confirm)과 Core 응답을 전달한다. View의 IRunDeckService 직접 참조를 제거했다. 확정 응답과 후속 읽기는 다른 번호이며 후속 읽기 실패로 확정을 재실행하지 않는다. 화면 해제는 결과 보관만 해제하고 제출한 확정은 Core가 한 번 처리한다.
- `RecoveryStateBuffer`에 기존 RecoveryStateQueryService의 투영 결과를 게시하고 StageMap View는 Buffer를 읽는다. 원본 버전이 바뀌면 재게시 전 낡은 결과를 반환하지 않는다.
- Core 수정 파일: CombatTurnFacts.cs, CharacterStatsQueryStatus.cs(Pending 추가), InGameCoreSystemGroup.cs(Tick 전달), RecoveryStateQueryService.cs(Tick 게시), 신규 CharacterPresentationPublisher.cs. 조립은 BufferSystemFactory와 InGameCoreSystemFactory이며 루트 재등록은 없다. 기존 Core 계산 서비스는 Core 내부/기존 Core 시험 계약으로 남고 UI/View는 Buffer 구현만 받는다.
- 상세 설계·수명·생성/등록/Export/소비 경로: `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/BufferPresentationBoundary.md`.
- 검증: Unity C# 응답 파일로 Contracts·Domain·Application·Systems·Framework 테스트 어셈블리 컴파일 통과. Unity 포함 Mono에서 새 경계 회귀 10개(덱 화면의 비동기 표시 전이 포함) 및 기존 덱 서비스·보유 능력치·Reader 검사 총 32개 통과. Unity Editor Test Runner/Play 시각 검증은 미실행이며 두 검증을 구분한다. 커밋·push·외부 발송 없음.

현재 상태 정정 (2026-10-27, 사용자 요청에 따른 부분 복원): Heal 오류 의심으로 변경 범위를 줄이기 위해 위 DeckEdit·회복 관련 구현을 되돌렸다. DeckEditPopupController/Factory와 StageMapViewSystem, RecoveryStateQueryService, 덱·회복 Buffer 서비스 키 및 관련 기존 시험은 직전 수정 전 상태로 복원했다. 신규 DeckEditBuffer·RecoveryStateBuffer, Group의 해당 등록/프로퍼티, 덱 요청 처리·회복 Tick 게시와 관련 시험을 제거했다. CharacterPresentationPublisher는 보유 능력치 게시만 담당한다. 밤 턴 상한·보유 능력치 Buffer·HUD 및 UI 검증 변경은 유지했다. 기존 Core 직접 조회 경로를 복원한 것이므로 위의 “UI/View는 Buffer 구현만 받는다”는 덱·회복에는 더 이상 적용하지 않는다. 새 설계 확장은 하지 않았다.

복원 검증: Contracts·Domain·Application·Systems·Framework 테스트 어셈블리 컴파일 통과. Unity 포함 Mono에서 유지한 경계·덱·보유 능력치 검사 26개와 회복 상태 조회·Heal 처리·회복 명령 검사 20개, 총 46개 통과(인자 없는 Test 메서드 기준). TestCase 및 Unity Editor Test Runner/Play는 미실행. 실제 Heal UI 오류를 재현·수정했다고 단정하지 않으며 이번 작업은 요청 범위의 복원이다. 커밋·push·외부 발송 없음.

### 5.53 개발 진입 스테이지 고정 — `EnterChapter` 오버로드·`PrepareGraph` 선택 인자 (2026-10-27, 사용자 허락으로 View 작업에서 Core 변경, 미전달)

**왜:** Play From Boot 개발 진입에 새 모드(`DevBattleStage.PlacedStage`)를 더했다. 이 모드는 `CombatStageData` 에셋 하나를 받아, 처음 방문하는 노드(Dev Node Id)에 그 스테이지를 실제로 놓고 진입한다. 지금 Selected 모드는 노드는 무작위 스테이지 그대로 두고 전투 정의만 바꿔서, 노드 외형·클리어 기록이 실제 전투와 달랐다. 새 그래프를 만들 때 노드 하나의 스테이지를 정해 둘 입구가 Core에 필요했다. 설계는 [dev-placed-stage-entry-design](../specs/2026-10-26-dev-placed-stage-entry-design.md), 계획은 [dev-placed-stage-entry-plan](../plans/2026-10-26-dev-placed-stage-entry-plan.md)이다. 사용자가 Core 변경 허락을 받아 두었다(2026-10-26).

**바꾼 것:**
- 새 `CoreCaculator/Contracts/StageMap/ChapterStagePin.cs`(+ `.meta`). 순수 값 `(StageNodeId NodeId, StageKey StageKey)`이고, 기본값이면 `ArgumentException`을 던진다.
- `StageSystem/StageGeneratorSystem.cs`
  - 계약·구현: `PrepareGraph(int mapKey, ChapterStagePin pin = null)`.
  - pin이 있으면 생성기 결과를 다 담은 뒤 `PlacePinnedStage`가 그 노드 하나만 바꾼다. 바꾼 노드의 스테이지 종류·맵 역할은 그 스테이지의 루트에서 다시 채운다.
  - 검사 셋은 모두 `ArgumentException`이다: 노드가 그래프에 있는지, 노드가 중간 깊이(1..DepthCount-2)인지, 스테이지가 이 맵의 Regular 목록(중간 노드 후보와 같은 목록)에 있는지. 마지막 검사가 보스 스테이지를 거른다.
  - `ChapterGraphGenerator`·`ChapterGraphSpec`은 바꾸지 않았다. 난수 순서가 같으므로 같은 시드면 그래프 모양은 고정이 없을 때와 같다.
- `ChapterProgressionCoordinator.cs`
  - 계약에 `EnterChapter(int chapterKey, ChapterStagePin pin)` 오버로드를 더했다.
  - 기존 `EnterChapter(int)`는 `EnterChapter(chapterKey, null)` 한 줄이 됐다.
  - 새 본문은 옛 본문 그대로이고 아래만 더했다.
    - 이어가기 갈래에서 pin이 있으면 `InvalidOperationException`.
    - 생성 갈래에서 `PrepareGraph(chapterKey, pin)`을 부른다.
    - 그래프를 새로 만들지 않았는데 pin이 있으면 `InvalidOperationException`.
  - `RunEnded` 조기 반환 순서는 그대로다.

**운영 동작:** 같다. pin이 null이면 옛 코드와 같은 길이다. 다음 챕터 이어가기의 `PrepareGraph(targetChapterKey)` 호출도 그대로다.

**고친 시험(컴파일만 맞춤):** `IChapterProgressionCoordinator`를 구현하는 가짜 여덟 곳에 새 오버로드를 더했다. 필요한 곳에는 using 한 줄(`Contracts.StageMap`)도 더했다. 본문·단언은 그대로다.
- 천명님 시험 여섯. 멤버 한 줄 `EnterChapter(int key, ChapterStagePin pin) => throw new NotSupportedException();`을 더했다.
  - `CoreIntegration/CombatTransitionPublisherTests.cs`(`Progression`)
  - `Framework/BattleCompletionServiceTests.cs`(`Progression`)
  - `Framework/BossObjectivePublicationTests.cs`(`ProgressionProbe`)
  - `Framework/CombatClearRewardIntegrationTests.cs`(`Progression`)
  - `Framework/CombatCommandPublicationTests.cs`(`ProgressionProbe`)
  - `Framework/IntimidationBufferTests.cs`(`DefeatProgression`)
- 우리 시험 하나: `Framework/CombatProgressJobTests.cs`(`FakeChapters`). 같은 한 줄이다.
- 호진님 시험 하나: `Framework/LobbyDepartureIntegrationTests.cs`의 감싸는 가짜(`CountingChapterProgression`). 기존 `EnterChapter(int)`와 같은 꼴로 챕터를 적고 안쪽에 넘긴다(호진님 메모 §13).

**요청:** 없다. 지금 이 오버로드를 부르는 곳은 개발 진입(`GameBootstrap.PrepareDevProgress`)뿐이다. 운영에서 쓸 생각이 없으면 나중에 개발 전용 입구로 옮겨도 된다.

**검증:** 컴파일·시험은 사용자가 Unity에서 확인한다(컴파일 미확인). 새 시험은 만들지 않았다.

### 5.54 넉백 미리보기 View 인수 — 시험 한 줄과 질문 둘 (2026-10-27, 미전달)

**왜:** 천명님 [Core 넉백 미리보기 설계](../specs/2026-10-27-attack-knockback-preview-design.md) §7 인계를 받아 View 표시를 구현했다. 설계는 [넉백 미리보기 View](../specs/2026-10-27-combat-knockback-preview-view-design.md), 계획은 [넉백 미리보기 계획](../plans/2026-10-27-combat-knockback-preview-view-plan.md)이다. View는 `AttackPreview.Knockbacks`를 그대로 그리고 거리·방향·막힘·면역을 계산하지 않는다. 결과 재생은 계속 `Moved` 사건을 쓴다. Core 파일은 바꾸지 않았다. 이 작업으로 [e413008 대응 정리](../specs/2026-10-26-combat-view-core-e413008-response-inventory.md) B6·Q-E5(밀려날 칸 미리보기)는 요청 없이 풀렸다.

**고친 시험(컴파일만 맞춤):** `Framework/WallAttackInteractionTests.cs:242`(천명님). 공격 조준 생성자에 새 필수 인자(넉백 미리보기 표시 의도)가 생겨, 우리 시험 도우미의 아무것도 안 하는 값 `CombatInputTestFactory.NoKnockbackPreview`를 넘기는 한 줄을 더했다. 본문·단언은 그대로다.

**View가 기대는 Core 사실:**
- 기록 대상은 미리보기 `Targets` 안에 있다(`AttackPreview.FromFacts` 검사). 없으면 View가 예외로 멈춘다.
- 다칸 대상의 최종 점유 칸 = 같은 응답의 `TargetOccupiedCells` + (`FinalAnchor − StartAnchor`)(Core 설계 §4.1 안내대로).
- 미리보기 답은 결과 재생 중에도 온다. View는 재생 중에는 넉백을 그리지 않는다(체력 예상과 같은 조건).

**질문(요청 아님, 답에 따라 View를 고칠 수 있음):**
1. `TargetOccupiedCells`는 공격 전 자리다. 같은 공격 안에서 넉백 전에 대상이 먼저 옮겨지는 경우(예: 끌어오기와 넉백이 한 공격에 함께 있을 때)가 있으면 `StartAnchor`가 그 자리와 달라 View의 도착 칸·화살표가 어긋난다. 이런 조합이 지금 규칙상 가능한가? 가능하면 기록에 대상의 시작 점유 칸(또는 최종 점유 칸)을 실어 줄 수 있는가?
2. 2연발 같은 반복 공격의 넉백은 본 공격 기록에 없다(Core 설계 §5.2·§6, `EnhancementDoubleShotTests`). 미리보기에서 첫 밀림만 보이는 것이 의도대로인지 확인 부탁드린다.

**검증:** 컴파일·Unity 확인 문제 없음(사용자). 새 시험은 만들지 않았다.

### 5.55 약한 지반 다시 켬 — 밟은 행동 끝에 붕괴 (2026-10-27, 사용자 지시로 View 작업에서 Core 변경, 미전달)

> 천명님께 넘길 때는 이 작업만 따로 정리한 [약한 지반 Core 공유](2026-10-27-weak-ground-collapse-core-share.md)를 쓴다(커밋 `2805717b`, 같은 내용 + 바꾼 줄 전후·`Complete` 경로 표·질문 셋).

**왜:** 사용자가 약한 지반을 다시 켜기로 했다. 천명님은 10-24(`d4a6d6fd`)에 플레이어 턴 공유 행동 비용으로 바꾸면서 약한 지반을 보류했다. 옛 붕괴 시점인 "유닛 턴 끝"이 없어졌기 때문이다(플레이어 턴 설계 기능 표 "Weak ground" 줄, 기획 결정 "주인 턴 끝 vs 플레이어 턴 끝"). 이번에 사용자가 그 시점을 정했다. **답은 "밟은 행동(명령)이 끝날 때"다.** 사용자가 Core 변경을 직접 지시했다(Q-WG1). 설계는 [weak-ground-collapse-after-move-design](../specs/2026-10-27-combat-weak-ground-collapse-after-move-design.md)(WG1이 규칙 원본), 계획·실행 기록은 [weak-ground-collapse-plan](../plans/2026-10-27-combat-weak-ground-collapse-plan.md)이다.

**규칙(설계 WG1):**
- 캐릭터가 실제로 들어간 칸마다 +1이다. 이동 경로는 지나가기만 해도 세고, 넉백·끌어오기·이동 중 환경 밀기도 센다. 밀린 장애물·배치·손패 복귀는 세지 않는다. 아군·적 모두 같다. 여기까지는 보류 전 규칙 그대로다.
- 한도에 닿은 약한 지반은 **그 진입이 일어난 명령 끝에** 모두 낭떠러지가 된다. 그 위의 캐릭터·장애물은 떨어진다(옛 `CollapsePending` 그대로).
- 명령 중 승패가 이미 났으면 무너뜨리지 않는다.
- 턴 끝·적 국면 끝·밤 끝에는 아무 일도 없다. 진입한 명령이 끝에서 무너뜨리므로 **명령 끝에 예정 붕괴가 남지 않는다.** 보류 때의 조건("풀리지 않는 예정 붕괴를 쌓지 않음")을 지킨다.

**바꾼 것(Core 두 파일):**
- `Application/Combat/WeakGroundCalculator.cs`
  - `RegisterEntry`·`CollapsePending` 본문을 `2d0509df` 그대로 되살렸다(줄 단위 같음, 주석만 더함).
  - `ResolveInterruptedActionEnd`는 지웠다(턴 경계가 없어 할 일이 없음).
- `Application/Combat/MovementProjection.cs` `Complete`
  - 첫 줄 `WeakGroundCalculator.ResolveInterruptedActionEnd(this);`를 `if (!Terminal) WeakGroundCalculator.CollapsePending(this);`로 바꿨다.
  - `Complete`를 지나는 명령: 이동(미리보기 포함), 공격·반응 공격, 배치, 전투 시작. 플레이어 턴 끝·적 국면 끝은 `CombatTurnCalculator.Changed`로 바로 만들어 지나지 않는다.
- 진입을 세는 호출 자리(이동 경로 `CombatMovementCalculator.cs:105`, 강제 진입 `MovementProjection.ResolveForcedTerrainEntry`)는 바꾸지 않았다.
- Contracts·Buffer·사건 형식은 바꾸지 않았다. Buffer `ValidateGroundChange`가 한 명령 안의 "진입 → 붕괴"를 이미 받는다.

**고친 시험(사용자 허락, Q-WG7):** 보류를 확인하던 천명님 시험 여덟 사례를 새 규칙으로 바꿨다. 사례마다 위에 "2026-10-27 View 작업에서 사용자 허락으로 기대값 변경" 주석이 있다.
1. `WeakGroundMovementTests.MovementDoesNotCountOrCollapseWeakGround` → `Movement_CountsEveryEntry_AndCollapsesAtCommandEnd`. 사례 `2L` → `1L`(시작부터 예정 상태인 입력은 이제 생기지 않음). 지나간 칸이 무너지고 D에 멈춘 이동자는 산다.
2. `WeakGroundMovementTests.HiddenGround_RemainsHiddenAndUnchangedDuringMovement` → 보류 전 이름 `HiddenGround_PreviewDoesNotRevealOrCountItButExecutionDoes`. 실행은 숨은 지반도 센다.
3. `WeakGroundMovementTests.ForcedAttackRelocations_DoNotChangeWeakGroundOrAttackerSettlement` → `ForcedAttackRelocations_CountEveryEntryWithoutChangingAttackerSettlement`. 진입 수 0 → 1(보류 전 기대값과 같음), 원본 사건 둘.
4. `WeakGroundTurnTests.EndBoundaries_DoNotCollapsePendingGround` 삭제. 지운 함수를 부르고, 턴 끝 무붕괴는 같은 파일 `ControllerCompletion_…`이 확인한다.
5. `WeakGroundTurnTests.EntryBoundary_DoesNotArmACollapse` → `EntryThenCollapse_TurnsGroundIntoCliffAndDropsOccupant`.
6. `CliffFallTests.AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees(WeakGround)`: 약한 지반 갈래만 진입 수 1·원본 사건 있음.
7. `Framework/WeakGroundPublicationTests.PreviewAndEntry_…` → `PreviewAndEntry_PublishExactCountAndOneResult_WithoutChangingOriginal`. 한도 2 → 3(이동자가 그 칸에 멈춰 한도에 닿으면 떨어지므로). 10-24 보류 뒤 이미 실패하던 것으로 보인다.
8. `Framework/WeakGroundPublicationTests.Collapse_PublishesEverySourceAndFallWithFinalOccupantsInOneAcceptedResult`: 턴 끝 대신 이동 명령 하나로 두 원본을 무너뜨린다. 이것도 이미 실패하던 것으로 보인다.
- 컴파일 맞춤 하나: `CoreIntegration/PullAndMarkPresentationTests.cs:422`(View 필드 창구 칸 하나).

**목록 밖에서 더 고친 시험 둘(묶음 끝 검토에서 찾음, 사용자 허락):** 둘 다 "작성 때부터 예정(2/2)인 지반이 턴 끝에 무너진다"는 옛 규칙에 기댔다. 10-24 보류 뒤에도 이미 실패하던 것으로 보인다(코드 대조, 실행 안 함).
9. `Framework/WeakGroundPublicationTests.KnockedObstacleThenCollapse_PublishesRemovalAndPreservesBindings`: 턴 끝 단계를 걷었다. 밀기 공격의 결과 안에서 지반이 무너지고 화약통 51이 떨어진다(`Destroyed` 반응 없음). 장애물 진입을 세지 않는다는 것은 원본 사건이 붕괴 하나뿐인지로 본다.
10. `Framework/WeakGroundPublicationTests.FreshBattle_InstallsAuthoredZeroProgressInsteadOfPreviousCliff`: 앞 전투에서 무너뜨리는 단계를 턴 끝 대신 이동 명령(지반을 지나감)으로 바꿨다. 새 전투 설치 단언은 그대로다.

**View가 기대는 Core 사실:**
- 붕괴 사건은 `CombatMovementSourceEvent(약한 지반 → 같은 ID 낭떠러지)`이고, 그 뒤에 `CombatFallEvent` → `Died` 순서다. View는 붕괴 사건 때 칸을 낭떠러지로 바꾸고, 추락 사건이 있는 사망은 가라앉기 연출로 그린다.
- 진입만 바뀐 사건(같은 종류, 진입 수 +1)은 외형을 바꾸지 않는다.
- 강제 진입의 진입 사건은 그 강제 이동(`Moved`) **바로 뒤**에 온다(`ResolveForcedTerrainEntry`). View는 이 순서에 기대 진입 사건을 그 이동의 동시 묶음에 붙인다(여러 대상 동시 넉백이 끊기지 않게). 순서가 바뀌면 그 대상만 차례 재생으로 바뀐다(동작은 맞고 동시성만 줄어듦).

**알려진 차이·제약(요청 아님):**
1. **공격 미리보기는 붕괴 추락을 예상하지 못한다.** `AttackResultFactory.Create`(`CombatAttackCalculator.cs:150`)가 `Complete`(`:159`)보다 먼저 미리보기 값을 만든다. 그래서 넉백으로 무너져 떨어질 대상도 체력·처치 예상에는 살아남는 것으로 나온다. 공격 결과 `Hits`의 맞은 뒤 체력도 같다(붕괴는 공격 결과 뒤의 별도 사건). 맞추려면 공격 계산에서 붕괴를 결과 만들기 앞으로 옮겨야 한다.
2. **`Complete(state: …)`로 상태를 미리 만들어 넘기는 호출(전투 시작 `CombatTurnCalculator.cs:54`)**: 그 안에서 붕괴하면 넘긴 상태의 환경이 낡는다. 지금은 전투 시작에 진입이 없어(재배치는 세지 않음) 생기지 않는다. 코드에 주석만 남겼다.
3. **적 AI는 약한 지반을 모른다**(`EnemyMovementPathfinder`는 지뢰와 들어갈 수 없는 칸만 피함). 적이 한도에 닿는 칸에서 멈춰 스스로 떨어질 수 있다. 사용자 결정으로 이번에는 그대로 두었다(Q-WG4, 플레이 확인 뒤 기획이 정함).
4. 붕괴 추락은 `ApplyFall`이 피해 원천 없이 체력을 0으로 만든다. 넉백으로 낭떠러지에 바로 떨어뜨릴 때와 같은 처리이고 바꾸지 않았다.

**질문(요청 아님):**
1. 보류 표의 기획 질문("주인 턴 끝 vs 플레이어 턴 끝")은 이번 답("밟은 행동 끝")으로 닫아도 되는지.
2. 공격 미리보기가 붕괴 추락을 예상하도록 할지(위 차이 1). 하면 View는 체력 예상에 그대로 나온다.

**데이터:** 약한 지반 정의 `Terrain_WeakGround`(한도 3)를 배치한 스테이지는 아직 없다. 확인용 배치는 사용자가 한다. 숨은 약한 지반은 무너지면 낭떠러지도 숨어 화면에 안 보이므로, 양쪽 공개로 두기를 권한다(작성 메모).

**검증:** 컴파일·시험은 사용자가 Unity에서 확인한다(컴파일 미확인). 새 시험은 만들지 않았다.

### 5.56 기차 진영 종료 기준 Phase A (2026-10-27, 사용자 허락으로 View 작업에서 Core 변경, 미전달)

사용자가 기차 재활성화 기준과 Core 수정을 승인하고 **Phase A만 구현 후 보고**하도록 지시했다. 정책은 플레이어·적 전체 국면 종료 합산, 기존 5/5·밤 사이 누계 유지·회수 전 판정이다. 원본은 [설계](../specs/2026-10-27-railway-controller-turn-reactivation-design.md), Task·검증 증거는 [계획 §8](../plans/2026-10-27-railway-controller-turn-reactivation-plan.md#8-실행-기록)이다.

- `CombatTurnFacts.CompletedControllerTurnCount`를 추가했다. `CombatTurnCalculator`의 양 진영 종료에서만 +1, 시작·재진입·밤 전환은 보존한다. `CompletedPlayerTurnCount`·시작 순번의 의미는 바꾸지 않았다.
- `CombatStateBuffer`에서 새 준비는 0, 성공한 각 종료는 +1, 나머지는 보존하도록 검증한다. 원본 비교·정산·최종 게시 경계를 우회하지 않는다.
- `RailwayGimmickSpec`·DB `RailwayData`·Adapter·SO 필드 이름을 ControllerTurns로 이전했다. SO 옛 이름은 FormerlySerializedAs로 읽고 편집 도구의 필드 접근과 단위 표시를 맞췄다. 기존 에셋 숫자·레일·enabled·GUID는 수정하지 않았다.
- 설정은 이미 `CombatPreparation` → `CombatStateData` → `CombatInputAssembler` → `CombatBattleFacts` → `CombatCommandInput.Battle`로 전달된다. 저장소·서비스·Factory를 추가하지 않았다.
- **기차 발동 계산·사망·종료 순서 연결은 아직 구현하지 않았다. `CombatGimmickCalculator` stub은 그대로다.** 다른 보류 기능도 활성화하지 않았다.
- 외부 MSBuild 컴파일 성공. 독립 Mono/NUnit: Core 3695/3695, 신규 게시 통합 8/8 통과. 추가 회귀는 45건 중 44 통과, 1건은 Unity 밖 ScriptableObject native 호출 불가로 실행 환경 실패(신규 8건은 44건에 포함).
- 열린 Unity 프로젝트 잠금 때문에 실제 SO 이전/재저장·DataBaseAssetTests·Unity Test Runner는 대기다. Phase A 전체 검증 통과나 기차 재활성화 완료로 인수하지 않는다. B~D 미착수, 커밋·push·외부 발송 없음.

### 5.57 기차 재활성화 B~D 연결 (2026-10-27, 사용자 지시로 Core 변경, 검증 일괄 대기, 미전달)

Phase A 보고 뒤 사용자가 B를 승인했고, 이어서 **검증을 건너뛰고 Phase D까지 진행, 마지막에 한꺼번에 확인**하도록 지시했다. [설계 §9](../specs/2026-10-27-railway-controller-turn-reactivation-design.md#9-bd-반영-사항과-검증-유보), [계획 §9](../plans/2026-10-27-railway-controller-turn-reactivation-plan.md#9-bd-연결-기록과-일괄-확인-목록)가 원본이다.

- Core `CombatGimmickCalculator` stub을 종료 누계/F/I·레일 대상 집계·동시 직접 사망·공통 Finish 정산으로 교체했다. 기차·연속 직접 사망 사건 형식은 기존 View 계약 그대로다.
- `CombatTurnCalculator`의 양쪽 종료에서 호출한다. 마지막 적 국면은 기차가 끝낸 전투를 회수하지 않도록 `EndNight`를 분리했다. 기존 투영·합성으로 최초 원본 대비 최종 변경 하나를 게시한다.
- 공통 귀속의 명령 행위자 fallback을 기차 내부 정산에서는 비운다. 영혼/깃발 등 다른 보류 no-op은 유지한다. 환경 무귀속 사망의 골드·경험치·Run 처치 수는 기존 공통 정책대로 0이며 클리어 보상은 기존 규칙이다.
- View의 기차 분류·접촉 사망 준비·직접 사망 중복 소비 방지·소리/카메라/취소 수명을 재사용한다. View 생산 코드·Factory·씬·리소스 변경 없음.
- 새 순수 `RailwayGimmickTests`와 .meta를 추가하고, A의 설정 전달 통합 시험은 최초 기차 발동/메인 사망까지 보도록 갱신했다. 옛 기차 보류 시험 중 정의가 없는 사례는 동작을 유지하고 이름만 실제 조건으로 고쳤다.
- **최신 코드의 빌드·테스트·시각 확인은 전부 미실행**이다. A의 이전 통과 수치를 이번 변경의 통과로 인수하지 않는다. 사용자의 최종 일괄 확인 뒤 상태를 갱신한다. 커밋·push·외부 발송 없음.
### 5.58 토네이도 진영 3턴 발동 복원 (2026-10-27, 사용자 명시 허용 Phase 1, 미전달)

**추가 직접 수정 승인·진입 보정:** 사용자가 경유/도착 즉시 발동도 Core에서 직접 수정하도록 명시 승인했다. `TornadoCalculator`, `MovementEnvironmentCalculator`, `MovementProjection`, `TornadoSourceFact` 주석을 수정해 기존 칸별 진입·강제 이동 중단·환경 장부/사건을 재사용했다. 발동 뒤 원본과 due는 기존 `AfterActivation` 계약 유지다. `MovementContextCalculator`, `MovementPreviewContext`에는 토네이도가 실제 읽는 공개 후보/도착지·환경 난수의 오래됨 검사를 보완했다. 마지막 보완 포함 기존 Core 3,608개 재실행 통과. 테스트 코드 추가·수정 없음. 실제 전투의 상승 뒤 복귀 문제는 View 조사 중이며 Phase 4 완료로 처리하지 않았다. 상세 증거는 최신 구현 계획 ‘최신 실행 상태’에 기록한다.

사용자가 이번 Phase 1에 한해 Core 직접 수정을 허용했다. TornadoCalculator·CombatTurnCalculator·BattlePreparationCalculator를 수정하고 InitialTornadoPlacementCalculator/.meta를 원래 GUID로 복원했다. TornadoSourceFact는 턴 단위 주석만 변경했다. 실제 한 칸 판정, ControllerTurnSequence의 진영 3·6·9 완료 경계, 기존 MovementProjection/Complete·난수·사건·도착/사망 처리 경로를 사용한다. 새 카운터·중심/크기 사실·Buffer 우회 쓰기·View 변경은 없다.

밤 종료가 겹치면 토네이도/도착 효과를 먼저 처리하고 생존한 전투만 밤 정산·회수를 진행한다. 최초 무작위 생성도 기존 배치 후보/환경 난수로 복원했다. 공개 `RepeatIntervalUnitTurns` 이름은 호환 유지하며 의미는 완료된 진영 턴이다. 상세 파일/사건/검증은 [최신 계획 Phase 1 완료 기록](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#tornado-2x2-plan)을 따른다.

검증: 동일 소스 복사본의 Unity EditMode Core 3,643/3,643, .NET Core 3,608/3,608. 원본 프로젝트 Systems/Framework 검사 컴파일 오류 0. Factory/Buffer 신규 토네이도 2검사 통과. 관련 fixture 기존 10실패는 변경 전 코드에서도 같은 이름/메시지임을 대조했다. 코드 리뷰 지적 없음. 운영 화면 수동 검증 및 View Phase 2~4는 미착수. Phase 1에서 정지했고 커밋·push·외부 전달하지 않았다.

후속 사용자 지시: 공개 IsDue의 매개변수 이름을 기존 completedUnitTurns로 유지하고 진영 턴 의미를 XML 주석으로 설명했다. 이름 지정 호출 컴파일 성공·관련 관리형 검사 53/53 통과로 재점검의 호환성 정리 후보를 해소했다. 발동 규칙 변경 없음.

### 5.59 유닛 공격 범위 조회에 이동 칸 (2026-10-29, 사용자 지시 "이 세션이 구현"으로 View 작업에서 Core 변경, 컴파일 미확인, 미전달)

- 왜: 전투 View 호버가 아군·적 유닛의 이동 범위 + 그 위 공격 범위를 그린다(설계 [overlap-inset-enemy-move-range-design](../specs/2026-10-29-combat-overlap-inset-enemy-move-range-design.md) EM1·Q-EM1 A·Q-EM2 A). 지금 이동 범위 계산(`CombatMovementCalculator.Range`)은 차례인 행위자·비용·행동 사용을 검사해 플레이어 차례에 적을 물으면 실패한다.
- 바꾼 것(셋):
  - `Contracts/Results/UnitAttackRangeResult.cs`: 새 속성 `MoveCells`(복사본). `Success(unitId, rangeCells, moveCells = null)`·`Failure(reason, unitId, moveCells = null)` 끝 선택 인자(null이면 빈 목록). 기존 호출은 그대로 컴파일된다.
  - `Application/Combat/CombatMovementCalculator.cs`: `Range`의 너비 우선 탐색을 `private Reach(input, actor, environment, allowance)` 하나로 꺼내 `Range`가 부른다(결과 같음). 새 `internal UnitMoveCells(input, unit, environment)` — 필드에 살아 있음·자리 정상·보드 안·들어 옮기는 이동·자발 이동 가능·이동력 있음일 때만 `Reach`, 아니면 빈 목록. 차례·국면·비용·행동 사용은 보지 않는다.
  - `Application/Combat/CombatAttackQueries.cs` `UnitRange`: 유닛을 찾은 뒤 플레이어 시점 지형으로 이동 칸을 먼저 구하고, 공격 실패(`UnitReady` 실패 — 기본 공격 불가·여러 칸 유닛 보류 등)·성공 결과 모두에 담는다(설계 P8). 전투 끝·유닛 없음은 지금처럼 빈 실패다.
- Buffer 종류·미리보기 서비스·요청 흐름은 그대로다(같은 `UnitAttackRange` 요청 하나에 답 하나).
- 시험: 고친 것·새 것 없음. 우리 `UnitAttackRangeQueryTests`는 끝 선택 인자라 그대로 컴파일된다.
- 확인 부탁: 이동 칸 조건이 Core 이동 규칙과 맞는지(특히 적의 이동력 0·`CanMoveVoluntarily` 거짓 유닛, 여러 칸 유닛은 빈 목록).
- 덧붙임(10-29 묶음 끝 검토 L3, 주석만): `Reach`의 문서 주석 "행동 범위와 유닛 이동 칸 조회의 유일한 탐색"을 고쳤다. 적 AI 길 찾기 `EnemyMovementPathfinder`는 따로 탐색하며 아는 지뢰 칸을 빼고 실제 지형을 쓴다. 그래서 `UnitMoveCells`(규칙상 닿는 칸)에는 적이 실제로는 고르지 않는 칸이 들어 있을 수 있다. View 호버는 이 칸을 그대로 그린다. 적이 실제로 갈 칸만 보여야 하면 AI 탐색과 맞추는 것은 Core 쪽 결정이다.
- 덧붙임(10-30, 병합 `6b0755be2` 충돌 정리): 천명님 특성 시스템이 이동력 `CombatActionAdmission.MovementAllowance(input, actor)`와 칸 검사 둘(`MovementRouteValidator.CanTraverse`·`CanEnter`)로 나눈 것을 `Reach`에 합쳤다. `Reach`는 `CanTraverse`로 넓히고(도약 보유 유닛만 넘는 칸이 생김), `Range`·`UnitMoveCells`는 거리가 있고 `CanEnter`인 칸만 남긴다. 도약이 없는 유닛은 결과가 옛것과 같다. 확인 부탁은 [천명님 공유](2026-10-30-combat-trait-system-view-cheon-share.md) §3.

### 5.60 공격 재생 입력을 공격 모양 값으로 — 천명님 시험 한 줄 컴파일 맞춤 (2026-10-29, View 작업, Core 파일 변경 없음, 컴파일 미확인, 미전달)

- 왜: 특성 추가 공격(공격 사건이 없음)도 공격 연출을 다시 쓰도록, 공격 조립·재생·종류 전략이 공격 사건 대신 View 값 `CombatAttackShape`(출처 사건·공격자·방향·타격 칸·영향 칸·주 대상·공격 그룹)를 받게 바꿨다([특성 발동 연출 설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md) TA5, [계획](../plans/2026-10-29-combat-trait-activation-presentation-plan.md) TA-a). 공격 사건 재생은 동작이 같다.
- 천명님 시험: `Assets/Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs:365`의 `attacks.FireShot(new CombatAttackEvent(…), …)`가 컴파일되지 않게 되어 첫 인자를 `CombatAttackShape.Of(new CombatAttackEvent(…))`로 감쌌다. 확인하는 내용(끌어오기 도착 칸으로 날아감)은 그대로다.
- 부탁: 따로 하실 일은 없다. 브랜치에서 이 줄을 고치셨다면 병합 때 감싼 꼴을 남겨 주시면 된다.

### 5.61 재생 계약 연결 함수 — 천명님 시험 가짜 한 줄 (2026-10-30, View 작업 유닛 행동 TS-f3, Core 파일 변경 없음, 컴파일 미확인, 미전달)

- 바뀐 것: View 재생 계약 `ICombatEventPlayback`에 `BindSettlement(ICombatSourceSettlement)` 하나를 더했다. 순차 재생기가 붙을 때 "그 출처의 맞는 순간 후속 중 그 유닛 것이 모두 끝났나" 질문을 표시 쪽에 넘긴다(대신 맞은 수호자가 돌아오기 전에 기다림, [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) AA5·계획 TP-13).
- 천명님 `Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs`의 시험 가짜 `Playback`(`:271`)에 전달 한 줄 `public void BindSettlement(ICombatSourceSettlement settlement) => _presenter.BindSettlement(settlement);`을 넣었다(컴파일 맞춤만, 확인 내용 그대로).
- 요청 없음.

### 5.62 표식 사건 반영 — 천명님 줄, 디버프 여섯 칸 보존 (2026-10-30, View 작업 특성 시스템 TS-a, Core 파일 변경 없음, 미전달)

- 바뀐 것: `ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs`의 표식 갈래(천명님 `4bd6b574`)가 새 `CharacterDebuffState`를 만들 때 디버프 여섯 칸을 모두 옮기고 표식만 바꾼다. 병합 직후에는 셋만 넘겨 새 디버프 셋(무장 해제·발목 꺾기·방어구 파괴)을 지웠다.
- 요청 없음. 표식 줄은 Core 공통 버프·디버프 계약(10-30 천명님 예고, 표식 → 약점 노출)이 오면 그 계약으로 옮긴다. 처음 넣었던 `WithHuntersMark` 요청(§54 R1)은 그 예고로 뺐다(10-30 사용자).

### 5.63 흔적 맞춤 창구 — 천명님 시험 인자 하나 (2026-10-30, View 작업 특성 시스템 TS-e, Core 파일 변경 없음, 미전달)

- 바뀐 것: 필드 창구 `CombatFieldEventPorts`에 필수 칸 `showTrail`(특성 흔적을 사건 시점에 맞춤)이 생겼다.
- 천명님 `Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs:446` 창구 생성에 `(_, _) => { }` 하나(지형 바뀜 다음 자리)를 넣었다. 컴파일 맞춤만, 확인 내용 그대로.

### 5.64 순차 재생기 클래스 선언 줄 — 천명님 줄 (2026-10-30, View 작업 유닛 행동 TS-f3, Core 파일 변경 없음, 미전달)

- 바뀐 것: `ViewSystem/Combat/Playback/Ordering/OrderedCombatPresentation.cs`의 클래스 선언 줄(천명님 줄)에 `ICombatSourceSettlement`를 더했다. 질문 본문 `IsSettled`와 연결 한 줄(§5.61)은 우리 줄이다.
- 요청 없음.

### 5.65 R4-compatible View branch merge (2026-10-30, explicitly authorized)

The user authorized merging the View branch into the current Core branch, resolving conflicts and pushing. Incoming `CombatAttackQueries.UnitRange` / `UnitAttackRangeResult.MoveCells` and shared `CombatMovementCalculator.Reach` are retained. Resolution removes reintroduced funding/price calls while preserving R4 ordinary-action admission and independent hover movement facts. No new Core state, Buffer transaction or Framework service. Core4042/4042, Integration350/351 (unchanged missing StageKey2101), Framework/View16/16; zero skips. Full scope, source commits, content decisions and manual boundaries: [merge record](2026-10-30-yonggeun-stage-merge.md). This completes compilation/regression verification of the incoming §5.59 source without claiming manual presentation acceptance. No external message sent.

### 5.66 가속 성장 SO의 null 참조 목록 재저장 (2026-10-30, 한 에셋 사용자 승인, Core 로직 변경 없음, 미전달)

- 사용자 승인: `TraitGrowth_Acceleration.asset` 한 개를 Unity에서 재저장하고 Play 진입 오류 수를 비교. 기본 정의·강화 null·GUID 유지, 새 테스트 코드 작성 없음.
- 원본은 `enhanced: {rid: -2}`를 사용하지만 `references.RefIds`에 `rid: -2` 항목이 없었다. Unity Inspector에서 값을 원상 유지한 채 저장하자 `- rid: -2`와 빈 `type: {class: , ns: , asm: }` 항목이 추가됐다. 다른 차이는 문자열 이스케이프/줄바꿈·빈 문자열 표기다.
- 확인: 이름·설명·강화 이름/설명을 디코딩해 원본과 비교했으며 동일. 기본 `attackPerMovedCell=1`, normal rid 1000, 강화 null, meta 해시/GUID 유지. 같은 미리보기 Play 진입의 Refid -2 오류는 **22건 → 21건**으로 감소. Play 종료 후 Combat 편집 씬 복귀.
- 같은 null 목록 누락 형식이 남은 성장 SO는 21개다. **나머지 에셋은 승인 범위 밖이라 수정하지 않았다.** 강화 정의를 임의로 채우거나 rid를 다른 값으로 바꾸는 조치가 아니다.
- 비교 자료: `Temp/TraitPreview/acceleration-before-reserialize.asset(.meta)`, `reserialize-play-delta.log`, `remaining-managed-null-assets.txt`. 소스·Core 규칙·기존 Setting 종료 제한은 변경하지 않았다. 이 에셋은 스테이지에 올리지 않았다.

### 5.67 남은 성장 SO 21개의 null 참조 목록 보완 (2026-10-30, 사용자 승인, Core 로직 변경 없음, 미전달)

- §5.66 후속 사용자 “나머지 21개도 정리” 승인으로, 같은 누락 형식의 성장 SO 21개에 가속 SO를 Unity가 저장하며 생성한 null 참조 목록 항목 두 줄만 추가했다. 각 `references.RefIds`에 `- rid: -2`, `type: {class: , ns: , asm: }`를 넣었다. 불필요한 문자열/줄바꿈 재직렬화는 하지 않았다.
- 보존 확인: 21개 모두 삽입한 두 줄을 제거하면 변경 전 원문과 정확히 일치하고, `.meta` 해시도 동일하다. 기본/강화 값·강화 null·GUID를 바꾸지 않았다. 앞서 Unity에서 재저장한 가속까지 합계 22개 보완이다.
- Unity Refresh 후 같은 미리보기 “환경 열기 / 재생”으로 진입했다. 새 로그 구간에서 전용 씬 로드를 확인했고 **Refid -2 오류 0건**이다(수정 전 22건 → 가속 한 개 처리 후 21건 → 전체 처리 후 0건). 비교 재생을 종료하고 Combat 편집 씬으로 복원했다.
- 자료: `Temp/TraitPreview/null-registry-before/`, `remaining-managed-null-assets.txt`, `null-registry-fixed-play.log`. 새 테스트·진단 도구 코드, Core 규칙·공용 서비스 코드는 추가/변경하지 않았다. 기존 Setting 종료 제한과 연출 동작 전체의 재검증은 이 보완의 완료 범위가 아니다. 커밋·push·스테이징은 하지 않았다.

### 5.68 2연발 Retired 분류 임시 해제 (2026-10-30, 사용자 명시 지시, Core 소스 한 줄 변경, 미전달)

- 사용자 “잠깐 retired 빼줄래?”는 직전 대화의 2연발(DoubleShot)을 대상으로 해석했다. `DeferredCombatFeatures.IsTraitRetired`의 `TraitKey.DoubleShot` 조건만 제거했다. 다른 Retired 특성·보류 조건·View 에셋은 바꾸지 않았다.
- 변경 후 분류는 `IsTraitRetired(DoubleShot) == false`, `IsTraitAvailable(DoubleShot) == true`, 불가 사유는 빈 문자열이다. **발동 기능의 복구는 아니다.** 현재 기본 TraitRegistry에 2연발 등록이 없고, Core Application에 2연발 발동 구현도 남아 있지 않다. 이 상태에서 강제로 Definition을 만들면 등록 없음 오류가 날 수 있다. 사용 가능 분류와 실제 구현이 어긋나는 임시 상태임을 사용자에게 알렸다. 실제 복원에는 Core의 정의·저작 변환·등록·발동 구현 인수가 필요하다.
- 검증: `ProjectL.Systems.csproj` MSBuild 종료 코드 0, 경고는 남는다. 로그 `%TEMP%/ProjectL-doubleshot-retired-build.log`. Unity Play·Test Runner는 실행하지 않았고 새 테스트 코드도 만들지 않았다. 기존 `LegacyEnhancementRetirementTests` 및 `LegacyEnhancementRetirementAuthoringTests`의 DoubleShot 사례는 여전히 Retired를 기대하므로 이번 임시 분류 변경과 불일치한다. 해당 Core 테스트는 임의 수정하지 않았다.
- 이번 변경은 사용자 명시 지시에 따른 Core 작업 경계 예외다. 전체 Retired 정책 제거·다른 특성 재활성화·기존 발동 구현 복원·스테이징·커밋·push는 하지 않았다.

### 5.69 Stage gimmick common state and Railway extraction (2026-10-31, explicitly authorized Phase 0 + 1, not sent)

> 현재 상태 대조(2026-11-01): 아래는 Phase 1 당시 기록이다. 현재는 §5.70·§5.71의 Rockfall/Haboob 처리기·SO 매핑·상태 전이 증거가 구현되어 있으며, 실행 순서는 기존 Tornado 훅 뒤 Railway → Haboob → Rockfall이다. 상태는 기존 CombatStateData.Gimmicks로 공개한다. View 연동·운영 콘텐츠·수동 인수는 완료로 보지 않으며, 인계 문서 철회와 계획된 Tornado의 미구현·사용자 소유 범위는 §5.72를 따른다.

The user authorized Phase 0 as Phase 1 preflight and implementation through the Phase 1 stop gate of [the stage gimmick plan](../plans/2026-10-31-stage-gimmick-system-implementation-plan.md). Core/Contracts and the existing Combat Buffer were changed within that scope.

- `CombatGimmickCalculator.TryAdvance(input)` remains compatible; the new boundary overload receives the ended side and already-incremented controller-end count. Explicit rule composition now invokes the extracted Railway rule. The legacy Tornado hook, settlement, cleanup, and recall order remain unchanged.
- `CombatCommandState.Gimmicks` and `CombatStateData.Gimmicks` expose immutable typed state through the existing Combat read sink. Constructors, equality, turn/movement projection, preparation, and real transactions preserve these facts. No new Framework service, Buffer, or gameplay clock was introduced.
- `RockfallGimmickSpec` and `HaboobGimmickSpec` are prepared contract types only. Their production handlers, events, SO mapping, content activation, and View consumers are deferred to later authorized phases. Production preparation rejects these kinds until handlers exist; direct typed preparation fixtures exercise storage only. The Phase 1 Buffer rejects runtime changes to gimmick facts until later phases add typed transition evidence.
- Verification and exact changed files are recorded in the plan's execution record. No FieldCamera/Architecture/View files or production assets were intentionally changed. This entry is local documentation, not an external delivery.

### 5.70 Rockfall gameplay and typed authoring (2026-10-31, explicitly authorized Phase 2, not sent)

Phase 2 adds `RockfallGimmickRule`, registered after Railway, and initializes its immutable state in the existing preparation path. `CombatStageData.rockfall` is disabled-by-default `RockfallGimmickAuthoring`; fresh `RockfallGimmickData` reaches `CombatStageDetail.Rockfall` and the existing DB adapter's prepared `RockfallGimmickSpec`. Existing Railway fields/IDs and all production stage assets remain unchanged.

Existing `CombatStateData.Gimmicks` exposes canonical warning cells/phase/deadline. New `CombatRockfallEvent` (`RockfallAdvanced = 42`) carries Warning/Impact, Expected/Final state and RecipientIds; only terminal Impact may have null Final. Existing evidence readers and Buffer publication validate the transition atomically. `EffectOwnerKind.StageGimmick = 5` uses the stable gimmick ID, Rockfall key and `rockfall-bind` rule in the existing status read model. Direct floored maximum-HP loss bypasses shields and attack attribution; existing settlement and Bind/return cleanup are reused, with no duplicate state, service or Job.

Core 4451/4451; CoreIntegration 594/595, including DB assets 9/9 and all 33 newly added cases. The single retained failure is the pre-existing missing `CinemachineCameraOffset` type. Evidence: `C:/Temp/projectl-gimmicks-phase2-regression/results.xml`; detailed execution record is in the Stage Gimmick implementation plan. Independent read-only review found no actionable findings. View event routing/presentation, playable content activation, Haboob and PlayMode/manual acceptance remain later-Phase/owner work; no external message, commit or push was made.
### 5.71 Haboob progression and direct designation (2026-10-31, explicitly authorized Phase 3, not sent)

`HaboobGimmickRule` prepares the full immutable area using the existing environment RNG after initial environment preparation and runs between Railway and Rockfall after the unchanged legacy Tornado hook. Existing `CombatStateData.Gimmicks` exposes `HaboobGimmickState.Cells`, `Phase` and `NextBoundaryCount` (Active = warning deadline; RelocationWarning = relocation deadline). Warning never samples/reveals a future position. `CombatHaboobEvent` (`HaboobAdvanced = 43`) publishes Step Warning/Relocation and non-null Expected/Final states; relocation can retain the area when it is the only fitting candidate. Existing evidence/Buffer transactions validate and publish these facts atomically.

`HaboobDesignationBlocked = 51` is returned by shared attack selection for differing attacker/primary-cell membership, including empty directly aimed cells. Range/BasicAttackCells/UnitRange directly selectable Single cells use the same helper. Valid-primary splash and Direction/All retain indirect hits; preview, execution and AI reuse the existing selection/delivery path. No View-side membership/status/targeting calculation is required. Actual movement/undo queries read the current canonical area without copying membership to characters.

`CombatStageData.haboob` is disabled-by-default typed `HaboobGimmickAuthoring`; fresh immutable `HaboobGimmickData` maps through `CombatStageDetail.Haboob` and the DB adapter to the prepared spec. Non-default interval4/lead2 and an authored mask were tested through real preparation and publication. No production stage was enabled. Final V2: Core4467/4467, CoreIntegration606/607, all28 new cases passed, DataBaseAssetTests9/9; only the retained missing CinemachineCameraOffset type failure remains. Evidence: `C:/Temp/projectl-gimmicks-phase3-regression/results.xml`. Phase 4 View routes/visuals/target refresh/completion/cancellation/reattachment, production content and PlayMode acceptance remain pending; no external message, commit or push was made.
Phase 3 independent final review found no actionable findings and independently checked the final XML. Phase 3 is complete at its stop gate; the Phase 4 items above remain pending.

### 5.72 Stage gimmick handoff withdrawn (2026-11-01)

The user confirmed that they will implement Tornado and explicitly instructed discarding the premature Stage Gimmick handoff. That local document was deleted and is no longer a deliverable or integration reference. No handoff was sent externally. Do not recreate it without a new request. Implemented Core scope remains common progression, preserved Railway, Rockfall and Haboob; planned Tornado gameplay is unimplemented, user-owned Core work. View is outside the user's ownership. Earlier Phase 4/handoff completion statements are superseded; this cleanup does not authorize new implementation. No product/test/asset changes, Unity run, commit or push were made.

### 5.73 배치 단계 손패 왕복의 방어막 예산 유지 (2026-10-31, 사용자 명시 지시 "코어 고치고", Core 소스 세 파일, 컴파일 미확인, 새 시험 없음, 미전달)

> 현재 코드 대조(2026-11-01): CombatPlacementCalculator.Return만 keepShieldBudget: true를 넘기며, Location·CombatReserveReturnCleanup.Clear의 기본값은 false다. AppliedStatusRules는 참일 때 남은 예산을 보존하고 Eligible만 false로 바꾼다. 이번 병합 정리는 문서와 호출 경로를 정적으로 대조한 것이며, 아래 과거 검증 기록을 새 Unity 실행 결과로 갱신하지 않는다.

- 천명님께 넘길 정리는 [배치 단계 손패 왕복 방어막 Core 공유](2026-10-31-deployment-return-shield-core-share.md)(미전달). 아래는 기록이다.

- 계기: §56. 배치 단계에서 단독 행동 유닛을 손패에 넣었다 다시 놓으면(`ReturnUnit` → `PlaceUnit`) 방어막이 없어지고, 끌어서 옮기면(`PlaceUnit` 필드 → 필드) 그대로라 같은 자리 바꾸기가 방법에 따라 결과가 달랐다.
- 바꾼 것(세 파일):
  - `Domain/CombatEffects/Rules/AppliedStatusRules.cs` — `ClearForReserve(current, keepShieldBudget)` 오버로드. 참이면 방어막의 **남은 예산은 그대로**, `Eligible = false`, 리셋 횟수 그대로. 옛 `ClearForReserve(current)`는 `false`를 넘겨 옛 동작(예산 0) 그대로.
  - `Application/Combat/CombatReserveReturnCleanup.cs` — `Clear(character, events, keepShieldBudget = false)`가 위 함수에 넘김.
  - `Application/Combat/CombatPlacementCalculator.cs` — `Location(…, events = null, keepShieldBudget = false)`가 정리에 넘기고, **배치 단계 전용 `Return`만** `keepShieldBudget: true`로 부름.
- 동작:
  - 손패에 있는 동안: 자격이 없어(단독 행동·결속 모두 필드에 있어야 자격) 흡수·표시가 없다. 공통 상태 목록의 `RemainingEligibleBudget`은 0이라 HUD 아이콘도 없다(옛 동작과 같음). 넣는 순간 `ShieldRemoved`(양 = 그때 남은 예산)도 옛 동작과 같다.
  - 다시 놓을 때: 판정을 통과하면 `ShieldStatusRules.Reconcile`이 남겨 둔 예산을 그대로 쓰고, 자격 꺼짐 → 켜짐이라 `ShieldGranted`(양 = 남은 예산)가 난다. 끌어서 옮긴 것과 같은 예산이다. 판정을 통과하지 못하면 자격 없는 이력으로 남는다(옛 동작과 같음).
  - 새 예산으로 다시 채우지 않는다: 첫 배치는 보통 예산이 가득이라 "새 예산"과 같지만, 밤 종료 회수 뒤의 배치 단계처럼 예산이 0인 유닛은 손패 왕복해도 0 그대로다(끌기와 같음). 그래서 손패 왕복으로 예산을 되살리는 길은 없다. §56 선택지 A 안의 "다시 놓을 때 새 예산"이 아니라 "넣기 전 예산 그대로"를 골랐다.
- 그대로인 것: 밤 종료 회수(`RecallForNight`)·적 대기 진입 등 다른 예비 구역 진입은 예산 0 규칙 그대로. 방어막 말고 손패 정리가 비우는 것(버프·디버프·임시 공격력·아이템 효과·보유 환경 등)도 그대로. 리셋 경계 계산·흡수 규칙·되돌리기 무변경.
- 천명님 문서와 다른 점: [위치 교대·정찰대·되돌리기 인계](2026-10-31-position-swap-recon-undo-core-view-handoff.md) §6 "Visible shields are removed by retaining canonical `ShieldStatus` history with zero budget … Redeployment does not reconstruct a new budget."에서, 배치 단계 손 복귀만 "예산은 남기고 자격만 끔"으로 바뀌었다. 그 문서는 고치지 않았다.
- 시험: 새 시험 없음. 기존 `ReserveReturnEffectResetTests`(천명님, `2bd95ad34`)의 두 시험은 `Location`을 직접 불러 기본값(예산 0)을 타므로 기대값 그대로 맞다. 배치 단계 `ReturnUnit`의 방어막을 확인하는 기존 시험은 찾지 못했다(`CombatCommonCommandTests`의 `shielded`는 보유 환경). 손패 왕복 사례 시험은 천명님 판단에 맡긴다.
- 검증: Unity·MSBuild 미실행(컴파일 미확인). 확인 순서: 배치 단계에서 단독 행동 유닛을 놓아 보호막·이펙트 확인 → 우클릭으로 손패 → 같은 자리 또는 다른 조건 맞는 자리에 다시 놓기 → 보호막 HUD 아이콘·유지 이펙트가 다시 나옴. 조건이 깨지는 자리에 놓으면 둘 다 없음.

### 5.74 도발당함 디버프 상태 (2026-10-31, 사용자 명시 지시 "1번으로 우리가 수정하는 방향으로" → "권장대로 코어 작업 시작해", Core 소스 9개 + 천명님 시험 1개, 컴파일 미확인, 새 시험 없음, 미전달)

- 설계: [도발 디버프 설계](../specs/2026-10-31-combat-taunt-debuff-design.md)(Q-TT1 A·Q-TT2 B·Q-TT3 A·Q-TT4 A·Q-TT5 A, 권장대로), 계획: [특성 이펙트 Core 변경 계획](../plans/2026-10-31-combat-trait-core-changes-plan.md) CT-a(TP-C1~TP-C9 그대로).
- 바꾼 것:
  - `Contracts/CombatEffects/StatusEffectKey.cs` — `Taunted = 14`, 분류 디버프.
  - `Contracts/CombatEffects/AppliedStatus.cs` — 새 `TauntedStatus(origin)`: 값 없음, 출처는 도발 특성만(아니면 예외), `SameValues` = 같은 출처.
  - `Contracts/Snapshots/StatusEffectSnapshot.cs` — 수명 갈래 `TauntedStatus → CurrentEligibility`, 새 `TauntedEffectSnapshot`(출처 목록만).
  - `Domain/CombatEffects/Rules/AppliedStatusRules.cs` — 새 `ReplaceTaunted(current, origins)`: 출처 집합이 같으면 같은 참조, 다르면 **도발당함 항목을 목록 맨 앞에 출처 순으로** 다시 놓음(다른 항목 순서 그대로). `Apply`에는 갈래를 넣지 않음(부여가 아니라 조정이 씀).
  - `Application/Combat/EnemyAi/TauntTargeting.cs` — `Sources(input, characters, actor)` 꼴을 더하고 옛 `Sources(input, actor)`는 `input.Characters`를 넘기는 한 줄. 적 AI 호출 줄(`EnemyAiPolicy.cs:45`·`:125`)은 그대로.
  - `Application/Combat/TraitPresenceReconciler.cs` — `Calculate`가 보호막 다음에 받는 쪽마다 도발당함 출처를 다시 계산(적 진영 + 살아 있음 + 필드만, 판정 = `TauntTargeting.Sources`, 필드에 도발 보유자가 없으면 건너뜀) → `ReplaceTaunted`. `ReconcileAndRecord`가 `RecordShieldActivations` 다음에 새 `RecordTauntActivations`(걸림 `StatusApplied`·풀림 `StatusRemoved`, 발동 유닛 = 보유자, 대상 = 받는 쪽들, 양 0, 근원 = 조정의 근원). 상태 사건은 지금 반복문이 바뀐 유닛마다 냄.
  - `Application/Combat/CombatTemporaryStateCleanup.cs` — 사망·전투 끝 `StatusRemoved` 출처 목록에 도발당함(약점 노출 줄 옆). 손패 복귀(`CombatReserveReturnCleanup`)는 이미 모든 특성 출처를 내서 그대로.
  - `Application/Combat/MovementEffectUndo.cs` — 이동 사건 허용 목록에 도발 `StatusApplied`·`StatusRemoved`, 유닛 하나 되돌림 비교(`SupportsCharacterChange`)는 도발당함을 빼고 비교(새 `WithoutTaunted`).
  - `Application/Combat/StatusEffectReadModel.cs` — `Capture`에 `TauntedEffectSnapshot` 한 줄.
- 판단(TP-C3·TP-C4):
  - 되돌리기 영수증을 새로 만들지 않았다. 도발당함은 자리만으로 정해져 `NormalizeMovementUndo`의 다시 계산이 되돌린 자리에서 그대로 복원한다. 영수증 없이 두면 `SupportsCharacterChange`가 막혀 도발 범위를 바꾸는 이동은 모두 되돌리기가 안 되므로, 그 비교에서만 뺐다.
  - `StatusEffectSet.SameValues`가 순서까지 비교해서, 다시 계산한 목록이 원래 목록과 순서까지 같아야 되돌리기 가능 판정(`ForwardCharactersMatch`)이 통과한다. 그래서 도발당함을 늘 맨 앞에 출처 순으로 둔다. 다른 효과 규칙(`Apply`·`ShieldStatusRules`·`WeaknessExposureRules`·`ClearForReserve`·`EndControllerTurn`·호환 교체)은 모두 자기 항목만 바꾸거나 뒤에 붙여 이 자리가 유지된다(코드 대조).
- 그대로인 것: 적 AI의 도발 판정(상태가 아니라 같은 규칙 함수), 피해 계산·`RequiresOrderedDamage`, 사실 검증 `CombatChangeEvidenceReader`(특성 상태 비교가 정규 목록 출처 정의 키로 찾아 도발당함 변경을 그대로 받음 — 설계 P2), 턴 넘김마다 조정(`CombatTurnCalculator.cs:186`, 설계 P3).
- 알려진 차이: 이동 되돌리기 결과는 도발당함을 상태 사건으로만 바꾸고 `StatusApplied`/`StatusRemoved`는 내지 않는다(보호막도 되돌리기 때 발동을 안 냄).
- 시험(사용자 "권장대로" = Q-TT5 A 허락): 천명님 `StatusEffectContractTests.Catalog_ReservesStableUniqueKeysAndSeparatesClassification`의 기대값만 맞춤(종류 `Range(0, 15)`, 디버프 목록에 `Taunted`). 새 시험 없음. 도발 보유자 주변에 적이 있는 사례에서 사건·상태 목록을 그대로 비교하는 시험(`TauntTraitTests`·`TauntPublicationTests`·`TraitActivationOutcomeTests`·`CombatChangeEvidenceVariantTests`·이동 되돌리기 시험 등)은 실행에서 바뀔 수 있다. 다른 사람 시험이라 깨지면 고치기 전에 묻는다.
- **View 짝(2026-10-31 같은 커밋에 넣음, 계획 §7.2):** HUD 표시 변환 등록(`CombatUnitStatusFactory`), 상태 외형 `UnitStatusVisual_Taunted`(효과 키 14)·표 등록, 설명 갈래 `TauntedStatus`·`CurrentEligibility`. 이것이 없으면 도발당한 적이 생길 때 `[CombatEffectFormatters] 표시 변환이 등록되지 않았습니다: Taunted` 예외였다.
- 검증: Unity·MSBuild 미실행(컴파일 미확인).

### 5.75 공격 범위 응답에 마무리 사격 대상 목록 (2026-10-31, 사용자 "권장대로 코어 작업 시작해" = Q-FS1 A·Q-FS2 A, Core 소스 2개, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

- 설계: [마무리 사격 대상 표시 설계](../specs/2026-10-31-combat-finishing-shot-target-mark-design.md) FS1, 계획 CT-b.
- 바꾼 것:
  - `Contracts/Results/AttackRangeResult.cs` — 새 속성 `InstantDefeatTargetIds`(`ReadOnlyMemory<EntityId>`), 생성자·`Success` 끝 선택 인자 `instantDefeatTargetIds`(null이면 빈 목록, 복사해 보관). `Failure`는 빈 목록.
  - `Application/Combat/CombatAttackQueries.cs` `Range` — 사거리 안 대상(`ids`, 캐릭터·구조물)마다 `FinishingShotApplication.Qualifies(input, actor, 캐릭터, 구조물)`로 거름(순서 = `ids`). 행동 비용 부족 갈래도 범위와 함께 냄.
- 그대로인 것: 판정 식·기준 값(Core 규칙 `FinishingShotTraitRule`), 실제 공격의 즉시 처치, 기존 호출(끝 선택 인자).
- View는 아직 이 목록을 읽지 않는다(FS-b). 화면 변화 없음.
- 검증: Unity·MSBuild 미실행(컴파일 미확인).

### 5.76 단독 행동 보호막의 자기 진영 종료 조건·주기 (2026-11-02, 사용자 "조건 우리가 추가하자, 수정 범위 측정해" → "우리가 구현 진행해", Core 3개 + 시험 2개, 미전달)

- **현재 인수 상태: 컴파일 미확인(사용자 Unity 확인 대기).** 외부 공유용 요약은 [단독 행동 ShieldReset Core 변경](2026-11-02-solo-action-shield-reset-core-changes.md), 앞선 시험 작성·MSBuild 실행에 대한 정정은 §62.3을 따릅니다.
- 원인·설계·범위 원본: §62·§62.1. 사용자 승인에 따라 View 작업에서 이번 Core 변경을 직접 구현했다.
- `Application/Combat/TraitTurnLifecycle.cs`: 수명 정산이 이미 받은 `endedSide`를 보호막 보유 조정에 전달한다.
- `Application/Combat/TraitPresenceReconciler.cs`: 일반 보유 조정은 종료 진영 없음, 종료 보유 조정은 명시한 진영을 내부로 전달한다. 단독 행동은 출처의 진영이 종료 진영과 같을 때만 초기화 후보가 된다. 자기 진영 종료 누계는 기존 사실에서 Player=`CompletedPlayerTurnCount`, Enemy=`CompletedControllerTurnCount - CompletedPlayerTurnCount`로 구한다. 상대 종료에서도 자격·출처 정리와 도발 조정은 계속 수행한다. 결속은 기존 양 진영 종료 초기화를 유지한다.
- `Domain/Traits/Effects/SoloActionEffectRule.cs`: 자기 진영 종료 누계와 설정 주기로 초기화 여부를 정한다. 공통 보호막 규칙에는 합산 종료 누계와 기록 간격 1을 전달해 합산 주기로 중복 판정하지 않는다. 최초 부여와 이후 `LastResetControllerTurnCount`는 기존 합산 누계 기록 단위를 유지한다. 공통 보호막 규칙·공개 계약·Buffer·SO·View·서비스 조립은 수정하지 않았다.
- 새 시험: 기존 `TraitShieldTests.cs`에 38건(아군/적 × 주기 1·2·3 × 잔량 0·6·10 × 최초 부여 누계 0·3 = 36건, 상대 종료 자격 상실/재진입 2건), 기존 `CoreIntegration/TraitShieldPublicationTests.cs`에 실제 양 진영 종료·Buffer 게시 주기 1·2·3의 3건. 기존 시험 기대값은 바꾸지 않았다.
- 검증과 한계는 §62.2에 기록했다. Core 컴파일과 신규 41건 통과. 도발 기존 실패 1건은 수정 전 Core에서도 재현했다. Unity 에디터 Test Runner·실제 이펙트 재생은 미검증이다. 외부 전달·커밋·push는 하지 않았다.

### 5.78 훈련소 방문의 세 장 뽑기 — 철회 (2026-11-02, 커밋 전에 넣고 뺌, 남은 Core 변경 없음)

- **철회(사용자):** 유닛을 먼저 고르는 흐름으로 바뀌었고 모든 유닛의 특성이 3개 이하라 뽑기가 필요 없다. `PickOffers`·`TrainingOffers`와 전달 줄을 모두 지웠다. 아래는 넣었던 내용의 기록이다.

- 설계·계획: [훈련소 View 연결 설계](../specs/2026-11-02-stage-training-view-design-Hojin.md) TV2·[계획](../plans/2026-11-02-stage-training-view-plan-Hojin.md) TV-a. 전달용 예고와 천명님께 묻는 네 가지는 [훈련소 카드 세 장](2026-11-02-training-three-offers-cheon-share-Hojin.md)이다.
- `Application/Shop/TrainingRules.cs`: 새 `PickOffers(choices, random)`. 후보를 섞어 `ShopOfferLimits.Maximum`까지 취한다(완전 무작위).
- `ShopSystem/ShopService.cs` `EnterShop`: 훈련 갈래가 `Choices(training, originOwned, originBalance)` → `PickOffers(…, new Random(seed))`, 결과를 준비 값 끝 인자로 넘긴다.
- `BufferSystems/Current/ShopEntryPreparation.cs`: 끝 선택 인자 `trainingOffers`, 훈련 방문이 아닌데 값이 있으면 `ArgumentException`.
- `BufferSystems/Current/CurrentProgressBuffer.cs`: 값 전달 한 줄.
- `BufferSystems/Current/CurrentShopData.cs`: `TrainingOffers`(세 장 초과·null이면 `ArgumentException`), `WithTrainingPurchase`는 같은 순서로 `CanPurchase`만 끈다.
- 그대로인 것: `TrainingRules.Choices`·`Plan`, `TrainingChoices`, 구매 명령·결과 계약, `ShopFailureReason`.
- 일부러 넣지 않은 것: 세 장 밖 구매 거절(설계 Q-TC3 A). "세 장만 살 수 있다"는 화면이 지키고 Core는 강제하지 않는다.
- 인계 문서 `2026-11-02-run-gold-facilities-trait-ranks-core-view-handoff.md` `:58`·`:108`의 "자르지 않음"과 다르다. 그 문서는 천명님 문서라 고치지 않았다.
- 시험: 기존 시험은 고치지 않았다. 코드 대조로는 통과해야 하지만 실행은 미확인이다.

### 5.79 훈련 선택지의 등급 수치 (2026-11-02, 사용자 "A 특성마다 글", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

- 카드에 "지금 수치 → 다음 수치 (+a)"를 보이려고 표시용 값을 더했다. 규칙은 바꾸지 않았다.
- `Contracts/Enhancements/TraitGrowthDefinition.cs`: `ValuesAt(rank)`로 등급 값 읽기를 모으고 `ResolveRank`가 그것을 부른다(동작 같음).
- `Contracts/Snapshots/TrainingChoiceSnapshot.cs`: `CurrentValues`·`NextValues`(끝 선택 인자, 없으면 null).
- `Application/Shop/TrainingRules.cs` `Choices`: 두 값을 채운다.
- `BufferSystems/Current/CurrentShopData.cs` `WithTrainingPurchase`: 잠글 때 두 값을 함께 복사한다.
- 실패 사유·구매 계약·생성 인자 순서는 그대로다.

### 5.80 훈련 선택지의 특성 글·방문의 산 선택지 (2026-11-02, 사용자 "TU-c 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

- `TrainingChoiceSnapshot`: `Name`·`Description`(끝 선택 인자). `TrainingRules.Choices`가 `TraitGrowthDefinition`의 글을 넣는다. 증강과 같은 원본이고 View 외형 글과 숫자가 달랐다(FinishingShot·MoraleBoost·Hunter·FocusedBombardment).
- `CurrentShopData.TrainingPurchase`: 산 선택지의 구매 직전 값(끝 선택 인자). `WithTrainingPurchase`가 지금 `TrainingChoices`에서 찾아 넣고, 없으면 첫 쓰기 전에 예외다. 구매 뒤 목록에서는 등급이 오르거나 빠져 산 단계를 다시 그릴 수 없어서다.
- 규칙·실패 사유·명령·결과 계약은 그대로다.

### 5.81 특성 설정값 공개 (2026-11-02, 천명 Core 작업, 사용자 "권장안대로" → "SP-a 진행"·"SP-b 진행", Core·CoreIntegration 시험 통과, 커밋 전, 미전달)

- 설계 [trait-settings-publication-design](../specs/2026-11-02-trait-settings-publication-design.md), 계획·실행 기록 [trait-settings-publication-plan](../plans/2026-11-02-trait-settings-publication-plan.md). §64 특성 부분·§55 CVI-R1의 답.
- 새 `Contracts/Snapshots/Traits/TraitSettingsSet.cs`: 유닛의 최종 `TraitSpec` 묶음(키 순, `TryGet`·`TryGet<T>`·`SameValues`).
- `Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`: `TraitSettings`(`stats` 뒤 필수 인자, 키 순서 검사). `OwnedCharacterStatsCalculator`가 resolver 결과를 넘김.
- 새 `Contracts/Combat/BattleTraitSettings.cs`: 전투 동안 고정인 유닛 키 표 + 보유 ID 표, `For(유닛)`(명령 프로필 선택과 같은 규칙)·`ForUnit(키)`. 생성 함수 `OwnedUnitSpecResolver.BuildTraitSettings`(`BuildProfiles` 옆, 같은 계산).
- `Contracts/Combat/BattlePreparation.cs`(`CombatPreparation`, 두 생성자 필수 인자)·`BattlePreparationCalculator`·`BufferSystems/Combat/CombatStateData.cs`(준비·복사 생성자)로 전투 내내 보관.
- 규칙·상태 계약·사건 복사 대상은 그대로다. 옛 특성별 설명 속성 다섯은 View 정리 뒤 지운다(계획 SP-d).
- 바뀐 시험 호출: 천명 7곳, Droute `ControllerTurnCountPublicationTests:143` 인자 하나(사용자 허락, 단언 무변경 — Droute께 알림 필요).

<a id="trait-display-origin"></a>
### 5.82 특성 표시 순서·출처 공개 (2026-11-02, 천명 Core 작업, 사용자 "모두 A" → "TO-a 진행"·"TO-b 진행", Core·CoreIntegration 시험 통과, 커밋 전, 미전달)

- 설계 [trait-display-origin-design](../specs/2026-11-02-trait-display-origin-design.md)(Q-TO1~Q-TO4 A), 계획·실행 기록 [trait-display-origin-plan](../plans/2026-11-02-trait-display-origin-plan.md). [10-29 특성 획득 순서 표시 요청](#trait-acquisition-display-order-request)의 답이며, 출처를 더했다.
- 배경: 11-01 병과 기본 특성(Rider 뛰어넘기·Heavy 굳건한 의지)이 운영에 켜져 특성 3개 유닛이 생겼다. 전투 정보창 `CombatUnitInfoView`가 칸 2개 고정이라 유닛을 누르면 예외로 멈춘다. **그 수정은 View 몫(칸 n개)이고 이 변경과 독립이다.**
- 새 `Contracts/Common/TraitOrigin.cs`(`Unit` 유닛 기본 · `Category` 병과 고유 · `Growth` 레벨업 획득), 새 `Contracts/Snapshots/Traits/TraitDisplayEntry.cs`(키 + 출처).
- `TraitSettingsSet`: `Display`(표시 순서의 키·출처, 키 집합 = `Traits`) 추가. 생성자는 (설정, 출처) 묶음 목록 하나를 받는다(옛 설정만 받는 생성자 삭제). `Traits`는 계속 키 순, `SameValues`는 출처·순서를 보지 않는다.
- 표시 순서: 유닛 기본(DB 유닛 SO 특성 목록 순서) → 병과 고유(최대 1) → 레벨업 획득(획득 순서). 강화·훈련 등급은 자리를 바꾸지 않는다. 출처는 병과 기본 특성 키면 `Category`(유닛이 원래 가졌거나 전직 전에 얻었어도), 성장 Acquire 키면 `Growth`, 그 밖 `Unit`.
- 생산자는 `OwnedUnitSpecResolver.Compose` 한 곳(internal 입구 `ResolveBase(…, display)`·`Resolve(…, ranks, display)`, 공개 서명 그대로). `BuildTraitSettings`(전투)·`OwnedCharacterStatsCalculator`(전투 밖)가 씀.
- 계산·비교 순서(`UnitDefinition`·`UnitStatsSnapshot.TraitStatuses`·`CharacterBaselineStatsSnapshot.TraitKeys`)는 그대로다.
- View가 읽는 법: 전투 `CombatStateData.TraitSettings.For(유닛).Display` 순서로 돌고 상태는 `Stats.TraitStatuses`에서 키로 찾는다. 전투 밖 `OwnedCharacterStatsSnapshot.TraitSettings.Display` 순서, 사용 가능 여부·등급은 지금처럼 키로. 출처를 어떻게 보일지는 UI가 정한다.
- 시험: 바뀐 호출 천명 5곳(`TraitSettingsSetTests`·`BattleTraitSettingsTests:62`·`OwnedCharacterStatsCalculatorTests:103`·`ImpactBoltAuthoringTests:222`·`BattleTraitSettingsPublicationTests:31`, 확인 내용 그대로), 새 `TraitDisplayOriginTests`(7)·`TraitSettingsSetTests` 사례 3·통합 1. Core + CoreIntegration 5,503개 중 실패 25 — 기준선(5,492개 중 25)과 같은 목록, 새 실패 0.

### 5.83 포효 예고 조회 (2026-11-11, View 작업, 사용자 "확인하면서 권장안대로 진행해" = Q-RF1 A 이 세션이 Core 구현, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

설계 [roar-forecast-design](../specs/2026-11-10-combat-roar-forecast-design.md) RF2, 계획 [roar-forecast-plan](../plans/2026-11-10-combat-roar-forecast-plan.md) RF-a·TP-1~TP-8. 사용자 요청은 "포효 카운트가 1 남으면 바닥에 포효 범위, 밀릴 유닛은 밀치기 미리보기처럼"이다. 언제 보일지는 View가 정하고, Core는 "지금 포효하면"의 답만 준다.

- 새 조회: 미리보기 종류 `CombatViewPreviewKind.RoarForecast = 10`(요청 값 `CombatViewPreviewData.RoarForecast(boss)`) → `CombatPreviewService`가 공격 종류 입력으로 조립 → `CombatCommandCalculator.GetRoarForecast(input, rules)` → `BossRoarCalculator.Forecast`. 응답 `CombatPreviewResult.RoarForecast`(새 형식 `Contracts/Results/RoarForecastResult.cs`: 성공·실패 이유·보스·범위 칸·대상이 있을 때의 `AttackPreview`).
- 계산: 실제 포효(`TryRoar`)와 같은 순서(카운터 되돌림 → 전체 지정 반응 공격)를 공개 미리보기로 돌린다. 카운터는 보지 않는다. 전투 끝 `StageEnded`, 밤 전투 아님 `WrongPhase`, 유닛 없음 `EntityNotFound`, 죽음·필드 밖 `NotInCombat`, 재구성 `Reconstructing`, 무장 해제 `Disarmed`. 포효 특성이 없는 유닛을 물으면 예외.
- 천명님 코드에서 바뀐 줄(동작은 기존 호출 모두 같음):
  - `BossRoarCalculator`: 조건 둘(`PhaseFailure`·`UnitFailure`)·카운터 되돌림(`ResetCounter`)·전체 지정 검사(`RequireAllProfile`)를 꺼내 `PendingBosses`·`TryRoar`·`Forecast`가 같이 씀(조건식·되돌림 전이는 옛 것과 같음).
  - `CombatAttackQueries.RestrictionFailure` 새 입구: `IntrinsicUnitReadiness`의 재구성·무장 해제 두 줄을 옮김(같은 결과).
  - 공개 미리보기 관찰자 끝 선택 인자 `previewObserver`: `CombatAttackCalculation` 생성자 → `CombatAttackCalculator.Run`·`ExecuteSteps` → `CommandAttackPlan.PreviewObserver`(비우면 공격자 진영 = 옛 동작) → `PullAttackCalculator` 재구성 계획이 이어받음 → `CommandAttackProjection`의 토네이도 관찰자. 같은 대입이 두 줄 있던 `CommandAttackProjection:213-214`는 한 줄로. 이유: 보스를 공격자로 공개 미리보기를 돌리면 적 시점이라 플레이어가 모르는 지형이 드러날 수 있다(호버 조회 Q-UR2 A와 같은 원칙). 입력 전체를 플레이어 시점으로 투영하는 길은 벽 형상 예외(§5.52와 같은 원인)라 쓰지 않음.
  - `AttackResultFactory`: 반응 공격도 공개 미리보기일 때는 `AttackPreview`를 만든다(`reaction && !plan.PublicPreview`일 때만 비움). 실제 포효는 공개 미리보기가 꺼져 있어 그대로.
- 확인 부탁(천명님): ① 반응 + 공개 미리보기 조합은 처음 쓰는 길이다 — `AttackPreview.FromFacts` 검사(전체 지정, 대상 미리보기, 넉백 대상 포함)가 반응 결과에서도 통과하는지. ② 예고는 공개 미리보기 규칙(자폭·공포 끔, 시체 넉백 없음, 피격 카운트 안 셈)을 따른다 — 실제 포효와 다를 수 있는 곳은 이 셋뿐이라고 보았다.
- 시험: `CommandAttackPlan` 생성자를 부르는 기존 시험 여섯(`AttackProjectionResumptionTests` 등)은 끝 선택 인자라 그대로 컴파일된다. 새 시험 없음(View 작업 지시).
- Buffer: `CombatViewPreviewKind`의 `ActionAvailability = 9`(천명님 줄) 뒤에 쉼표를 붙이고 `RoarForecast = 10`을 더했다. 값 9는 그대로다.
- View 쪽(2026-11-11 완료, 사용자 Unity 확인 문제 없음): 카운터가 1 남은 동안 플레이어 입력에서만 묻고(적 턴·재생 중 없음), 범위는 바닥 층, 넉백은 공격 조준 넉백 미리보기와 같은 화살표로 그린다(조준이 넉백을 그리는 동안은 조준 먼저). 계획 RF-b·RF-c.

### 5.84 전투 첫 동작 Core 준비 서비스 (2026-11-10, 사용자 지시 "우리측에서 작업하는 걸로 하고…" — View 작업에서 Core 변경, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

- 원본: [첫 동작 Core 준비 계획](../plans/2026-11-10-combat-first-use-preparation-plan.md) FU-a·FU-b, [작업 문서](2026-11-10-combat-first-use-preparation-core-work.md), 요청·결정은 §68.
- 목적: 첫 배치·공격·이동·적 턴의 Core 안 JIT(측정 합계 약 436ms)를 Combat 씬 로딩 화면이 가린 동안 미리 끝낸다. 게임 결과·Buffer·사건·난수는 바꾸지 않는다.
- 새 Core 파일 셋:
  - `CombatSystem/ICombatFirstUsePreparation.cs`: `bool PrepareNext()`. 남은 단계 하나를 돌리고, 더 남았으면 true다.
  - `CombatSystem/CombatFirstUsePreparationService.cs`: 조립기·이동 규칙만 받는다. 게시 협력자가 없으므로 구조상 Buffer에 쓸 수 없다.
    - 첫 호출에서 지금 Buffer의 전투로 시작 입력을 조립한다.
    - 전투 번호가 바뀌면 처음부터 다시 한다.
    - 끝까지 마치면 인스턴스 수명 동안 다시 하지 않는다(정적 상태 없음).
  - `CoreCaculator/Application/Combat/CombatFirstUseRehearsal.cs`: public 클래스이고, 표면은 생성자와 `RunNext()`뿐이다.
    - 가상 연쇄 순서: 메인 배치 → 손패 서브 배치(최대 6) → 전투 시작 → 플레이어 입력까지 자동 진행 → 첫 아군의 행동 가능 여부·유닛 공격 범위·이동 범위 → 공격 종류마다 공격 범위·공격 미리보기 → 한 걸음 이동 미리보기·명령 → 공격 명령 → 턴 종료 → 적 턴 자동 진행.
    - 칸·조준은 Core 조회 결과(배치 구역·배치 미리보기·이동 범위·공격 범위)에서 첫 값을 쓴다. 결과는 모두 버린다.
    - 가상 세계는 `CombatCommandComposition.Project`로 잇는다.
    - 다음 행동의 입력은 서비스가 맞는 기대 턴 순번·처리 종류로 새로 조립하고, 가상 세계를 `WithWorld`·`WithPersistentFacts`로 그 위에 옮긴다. `CombatCommandInput` 계약은 바꾸지 않았다.
    - 거절은 예외가 아니다. 이어 갈 상태가 없으면 남은 의존 단계를 건너뛴다.
    - 공격은 사거리 안 대상(`InRangeTargetIds`)이 선 칸을 먼저 조준하고, 대상이 있는 아군으로 공격 명령을 계산한다. 첫 턴에 맞히지 못하면 적 턴 뒤 한 번 더 공격한다. 이동 명령은 `MovementContextCalculator.Matches`가 참일 때만 계산한다(명령 처리와 같은 순서). 묶음 끝 검토 반영(2026-11-11).
- 기존 Core 파일 셋:
  - `CoreProfilerMarkers`: `Core.Combat.FirstUse` 마커.
  - `CoreProfiling`: `CoreProfileSection.FirstUseRehearsal`를 끝에 더했다. 측정기가 `Core.Calc.FirstUseRehearsal` 마커를 자동으로 만든다.
  - `InGameCoreSystemGroup`: 계약 멤버 `CombatFirstUse`와 해석 줄 하나.
- 연결 줄: `Factory/Runtime/InGame/Core/CombatSystemFactory.cs` 한 줄, `InGameCoreSystemServiceKey.CombatFirstUse`. 민형님 Factory 공유 §54에도 적었다.
- 전제(2026-11-10 확인): Core 계산기는 입력만으로 계산한다. `CoreCaculator/Application`·`Domain`·`CombatSystem`에 readonly가 아닌 정적 필드나 정적 컬렉션 캐시가 없다. 그래서 결과를 버리면 남는 상태가 없다고 본다. 계산기에 정적 캐시를 더할 때는 준비 유무로 결과가 달라지지 않게 해야 한다.
- **Core 작업자께:** 다음 입구의 서명이나 뜻을 바꾸면 리허설도 맞춰야 한다.
  - `CombatCommandCalculator`·`CombatMovementCalculator`·`MovementContextCalculator`·`CombatPlacementCalculator`의 공개 입구
  - `CombatCommandComposition.Project`·`Intent`
  - `CombatCommandInput.WithWorld`·`WithPersistentFacts`·`WithActor`
  - 새 규칙 때문에 가상 연쇄가 거절되면 덮는 경로만 줄고 게임 결과는 같다.
- 실패 처리: 준비 중 예외는 오류 경계(F1)로 간다(계획 FU-P4).
- 부르는 쪽(전투 골격, 우리 영역):
  - Combat 씬 로딩 준비 `LifeCycles/Combat/CombatFirstUseLoadingPreparation`과 Job `Jobs/CombatFirstUsePrepareJob`
  - `CombatPlan.Register`의 `JobType.FirstUsePrepare`(= 10) 갈래
  - `CombatEntryService` 준비 등록, `CombatEntryRequest` internal 멤버
- 확인 대기: Unity 컴파일, 직행·맵 진입 로딩, 두 번째 전투, 첫 동작 측정(계획 FU-c).

### 5.85 재구성 공격 미리보기 칸 (2026-11-11, View 작업, 사용자 "Q-RE4 직접 구현", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 미전달)

- 배경: 적 전용 재구성(§67)이 발동할 공격을 호버하면 대상 예상 체력 `TargetHealthAfter` = 최대 체력이라, UnitHUD가 현재 체력으로 잘라 "피해 없음"처럼 보였다. 미리보기 대상에 재구성 여부 칸이 없어 View가 알 수 없었다(체력 수로 짐작하면 위스키처럼 맞은 뒤 회복과 구분이 안 됨). 설계 [reconstruction-view-design](../specs/2026-11-11-combat-reconstruction-view-design.md) RE3·§5, 계획 [reconstruction-view-plan](../plans/2026-11-11-combat-reconstruction-view-plan.md) RE-c.
- 바꾼 것(모두 추가):
  - `Domain/Traits/Rules/ReconstructionTraitRule.cs`: `Triggered(before, after)` = `CanTrigger(before)` ∧ 뒤 사실 살아 있음 ∧ 재구성 사용함. 판정 원본은 이 규칙 하나다.
  - `Application/Combat/CommandAttackLedger.cs`: `BeginScope`가 입력 캐릭터 목록 참조만 쥐고, `CaptureHitHealth`가 체력을 고정한 대상마다 `Triggered`(범위 시작 사실, 지금 사실)로 판정해 재구성된 캐릭터 집합을 만든다(재구성이 있을 때만 집합 생성 — 적 AI 후보 평가도 지나는 길이라 명령마다 모든 캐릭터를 훑지 않음). 읽기 `Reconstructed(id)`(고정 전에는 예외, `HitHealth`와 같은 규칙).
  - `Contracts/Results/AttackHitResult.cs`: 속성 `TargetReconstructed`, 두 생성자 끝 선택 인자 `targetReconstructed = false`(내용 생성자가 넘김).
  - `Application/Combat/AttackResultFactory.cs`: `CreateHit`과 보조 행이 `ledger.Reconstructed(id)`를 넘긴다(`SingleOutcome`은 그대로 거짓).
  - `Contracts/Results/AttackTargetPreview.cs`: 속성 `TargetReconstructs`. 피격 결과에서 만드는 생성자가 복사하고, 큰 생성자는 끝 선택 인자 `targetReconstructs = false`로 받는다. 넉백·작렬 부딪힘 보조 행(`AttackResultFactory`)이 `ledger.Reconstructed(id)`를 넘긴다(View 묶음 끝 검토 M2).
- 바뀌지 않은 것: 계산·사건·결과 검사(`CombatChangeEvidenceReader`는 새 칸을 보지 않음), 공개 생성자 기존 서명(`new AttackHitResult(` 23곳·`new AttackTargetPreview(` 13곳 그대로), 시험.
- 피격 체력 고정 시점(비용 정산 뒤, 강제 도착·연쇄 공격 전) 뒤에 일어난 재구성(예: 연쇄 공격으로 쓰러진 경우)은 이 칸에 들지 않는다. 체력 칸과 같은 시점 규칙이다.
- View가 읽는 곳: `CombatUnitHudFeedback.ShowAttackHealthPreview` — `TargetReconstructs`면 예상 체력 0(치명 예상).
- 천명님 확인 부탁: ① 칸 이름·자리(결과·미리보기 둘), ② 판정을 재구성 규칙 함수에 둔 것, ③ 고정 시점 밖의 재구성을 빼는 것.

### 5.86 StageSelect 런 포기 (2026-11-11, 사용자 승인으로 Core 수정, 컴파일 미확인)

- `RunSystem/RunAbandonService.cs`의 `TryAbandonRun(expectedRunId)`는 비어 있거나 다른 Run ID, 종료된 런, 준비·활성 전투가 남은 경우를 거절한다. 수락 시 기존 `IRunProgressBufferWriteSink.ResetRunProgress()`로 런을 정리한다. 종료 요약은 이전 값을 유지하며 이 요청으로 새 요약을 생성하지 않는다.
- `InGameCoreSystemFactory`가 서비스 객체를 만들고 Core 내부 Builder에 `IRunAbandonService`와 바인딩 모듈을 등록한다. `InGameCoreSystemServiceKey.RunAbandon`으로 Core Group이 보관하며 `StagePlan`이 `InGame.CoreGroup.RunAbandon`을 `StageRunAbandonJob`에 넘긴다. 상위 Export는 기존 그룹 단위다.
- 전투에서 포기할 때는 기존 전투 완료 경로를 쓴다. Stage의 UI와 이탈 연결은 §71에 있다.

## 6. Buffer B1 (View 작업에서 구현, Core 작업자 확인 요청)

`StageGeneratorSystem`의 노드 기록 로직과 `StageProgressBuffer`를 고쳐야 해서 원래는 Core 작업자에게 요청하려 했다(설계 D20). 사용자 지시로 **View 작업에서 구현했다(설계 D21, 2026-09-25).** 아래 내용이 Core 작업 방향과 맞는지 확인해 주세요.

### 6.1 이유

스테이지 맵 View는 `StageProgressBuffer`의 노드 목록을 읽어 그린다. 노드 외형(일반/보스 프리팹)을 고르려면 노드의 종류가 필요한데, 현재 `StageProgressData`에는 `StageKey`만 있다. View는 InGameGroup을 읽지 않으므로 **Core가 노드를 쓸 때 종류를 같이 써 주어야 한다.** ViewGroup의 `StageNodeVisualTable`은 이미 `(StageType, StageMapRole)` 키로 준비되어 있다.

### 6.2 구현 내용

```csharp
// StageProgressData — 필드 2개 추가. 기존 호출(5·6인자)이 깨지지 않도록 기존 isClear 뒤에 기본값 인자로 둔다.
public StageProgressData(int nodeId, int stageKey, int depth, int widthIndex,
    IEnumerable<int> nextNodeIds, bool isClear = false,
    StageType stageType = default, StageMapRole mapRole = default)
```

```csharp
// StageGeneratorSystem — 결과 루프
StageRootData root = node.StageKey == 0 ? null : stagesByKey[node.StageKey];
result.Add(new StageProgressData(node.Id, node.StageKey, node.Depth, node.Width, node.Next,
    stageType: root?.StageType ?? default, mapRole: root?.MapRole ?? default));
```

- `StageGeneratorSystem`: 기존 스테이지 수집 루프에서 `stagesByKey`를 함께 만들고, 노드를 기록할 때 해당 `StageRootData`의 `StageType`, `MapRole`을 전달한다. 생성 알고리즘과 난수 사용은 바꾸지 않았다.
- `StageProgressBuffer`: 클리어 상태를 갱신하면서 Data를 다시 만들 때 새 필드도 복사한다.
- 시작 노드(StageKey 0)는 두 필드가 기본값(`StageType.Event`, `StageMapRole.Regular`)이다. 의미가 없는 값이므로 **`StageKey == 0`을 먼저 확인**해야 한다. View는 시작 노드를 별도 외형(`StageMapViewData.StartVisualPrefab`)으로 그린다.
- 테스트 영향: `MapProgressBufferTests.cs`의 5인자 호출 8곳은 기본값 덕분에 수정하지 않았다.
- 검증(에디터 밖, Phase D·E): 새 에셋으로 만든 InGameGroup에서 맵 1을 생성해 모든 스테이지 노드에 (Combat, Regular)/(Combat, Boss)가 기록되고, 클리어 갱신 뒤에도 유지되며, 해당 키의 노드 외형이 ViewGroup에 있음을 확인했다.

## 7. 공용 타입

- 아래 타입은 **옛 Core.Contracts의 것을 그대로 재사용**한다. `ProjectL.Systems`가 이미 Core.Contracts를 참조하고 있기 때문이다.
  - `EntityKey`, `Rotation4`, `AppointMethod`, `AttackTargetMode`, `UnitOwnerEligibility`, `TraitKey`, `CellCoord`
- `CellCoord`는 좌표와 오프셋(공격 패턴)을 모두 표현한다. SO 입력은 직렬화 가능한 `CellInput`으로 받아 변환한다.
- `StageType`, `StageMapRole`만 새 공용 위치(`DataBaseSystem/Data/Common/`)로 옮긴다.

**확인 요청:** 옛 CoreCaculator(특히 Contracts)를 삭제하거나 정리할 계획이 있으면 알려 주세요. 그때 위 타입을 `Data/Common/`으로 옮기겠습니다.

## 8. Core 데이터에서 빠지는 View 필드

옛 Core SO에 섞여 있던 표현 필드다. InGameGroup으로 복구할 때 **넣지 않는다.** ViewGroup이 대신 맡는다.

| 옛 위치 | 필드 | 대체 |
|---|---|---|
| StageDefinitionSO | `_viewStageType` | View가 Buffer의 StageType + MapRole로 판단 |
| FieldDefinitionSO | `_fieldVisual` | ViewGroup StageFieldViewTable.cellPrefab |
| UnitDefinitionSO / StructureDefinitionSO | `_displayName` | ViewGroup UnitVisualTable.displayName |
| (InGameGroup) StageViewPropTable | StageKey → GameObject | ViewGroup으로 이동 (GameObject는 P1 위반) |

Architecture(Catalog + 정의 5)는 다른 View 작업자가 맡는다. 이번 재구성 범위가 아니다.

## 9. 데이터 주의사항

| 항목 | 내용 |
|---|---|
| Trait 값 | 옛 에셋의 Trait은 SerializeReference 타입명(`ProjectL.Unity...*TraitAuthoring`)에 묶여 있다. 새 타입명과 달라 자동으로 옮겨지지 않으므로 새 에셋에 직접 입력한다. Akimbo: MoveAttackDiscount(1, 1), Sniper: Focus(1, 3) |
| 누락값 | ShotGun, Headquarters 에셋에는 `_ownerEligibility` 값이 없다. 기본값 PlayerOnly로 복구한다 |
| 오타 수정 | ShotGun displayName `ShouGun` → `ShotGun` |
| 미사용 | Field_8x6(스테이지 미참조), PlayerLoadout(StageDefinition 미참조), `StageEnum`(테스트의 어셈블리 기준 타입으로만 사용하므로 유지) |
| 데이터 없음 | EntityKey 14~17(MainCharacter, Ballista, CactusCannon, Gravekeeper)은 유닛 정의 에셋이 없다. [묘지기 인계 문서](2026-09-21-shared-knockback-gravekeeper-view-handoff.md)의 `Gravekeeper.asset`은 이 저장소 이력에 없다 |
| 연결 알고리즘 | 옛 StageMapChapter1에는 연결 알고리즘·가중치 값이 없다. 기본값(Nearest, 10, 1)으로 복구한다 |

## 10. 옛 에셋 보존

- 커밋 `a18d86d`(2026-09-22)에서 삭제된 스크립트 16개 때문에 `Assets/ProjectL/DataBaseSO`의 옛 에셋 48개가 Missing Script 상태다.
- 값은 새 구조의 새 에셋으로 옮겼다. 옛 에셋 48개는 이후 모두 지워졌다: 2026-09-28 `8f38e2c1`(Architecture 6개), 2026-09-29 `6e257b64`(Stage·Field·Unit 등 42개). 2026-10-13 확인: 지워진 48개는 [고아 에셋 인벤토리](../specs/2026-09-24-view-db-orphan-asset-inventory.md)의 48개와 같고, DataBaseSO 에셋 201개 중 스크립트를 잃은 에셋은 0개다. 옛 값은 인벤토리와 `git show 6e257b64^:<경로>`로 볼 수 있다.
- 옛 스크립트는 `git show a18d86d^:Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/<경로>`로 볼 수 있다.

**확인 요청(2026-10-13 해소):** 위와 같이 삭제가 끝나 확인할 것이 없다.

## 11. 향후 협의: 전투 필드 Buffer (B2)

전투 View가 필요로 하는 값이다. 전투 View 작업 때 같이 정의한다.

- 맵 크기, 엔티티 목록(EntityKey, 소유자, 위치, 회전, 점유 셀), 양측 배치 깊이(배치 가능 영역 표시)
- 배치 미리보기(가능/불가) 결과를 View가 받는 경로

## 12. 응답 양식

| # | 확인 사항 | 답변 | 담당 | 날짜 |
|---|---|---|---|---|
| 2 | 데이터 흐름 전제(새 Core는 Systems 시스템, View는 Buffer) | | | |
| 5 | Stage 데이터 API 변경(M3/M4, Combat 검증) 영향 | | | |
| 6 | Buffer B1 구현(View 작업에서 완료) 확인. 시작 노드 기본값 처리 방식 포함 | | | |
| 7 | 옛 CoreCaculator/Contracts 정리 계획 | | | |
| 10 | 옛 에셋 삭제 시점 | 삭제 완료(`8f38e2c1`·`6e257b64`) | – | 2026-10-13 확인 |
| 11 | 전투 필드 Buffer 협의 시점 | | | |

## 13. 전투 진행 골격: Core 서비스·결과·수명 요청 (2026-09-30)

> §13.1~§13.5는 Phase 0 당시 요청 이력이다. `83f485d1` 병합 후 현재 인수 상태와 추가 요청은 **§13.6**, 실제 API·검증 상태는 [골격 계획 §3.1·§6.7](../plans/2026-09-30-combat-flow-skeleton-plan.md)을 따른다.

**상태: 요청 문서 준비 완료, 담당자 전달·답변·합의 미확인. Core 코드 수정 없음.** 상세 기준은 [전투 진행 설계 4차](../specs/2026-09-30-combat-flow-skeleton-design.md), 일정·인수 조건은 [구현 계획 §3](../plans/2026-09-30-combat-flow-skeleton-plan.md#3-선행-계약과-인수-조건)이다. R 번호는 [기존 Core 요청](2026-09-29-combat-view-buffer-core-requests.md)을 사용한다. F1~F3는 이번 Flow 설계의 조건이며 이전 Core 계획의 G 번호와 구분한다.

### 13.1 소유 경계 확인 요청

| 영역 | 이번 설계의 역할 분담 |
|---|---|
| CombatLifeCycle·CombatFlow | View 작업이 수명 연결과 다음 작업의 등록 조건을 구현 |
| CombatPlan·전투 Job 네 개 | **준비 Start까지 포함해 View 작업이 구현.** Core 서비스가 준비되면 Job 몸통의 호출 연결도 View 작업이 담당 |
| JobType | View 작업이 ProcessCommand(3), ProcessPreview(4), AdvanceCombat(5)를 추가하고 Framework 담당자에게 공유 |
| Core 서비스·규칙·상태/결과 기록 | Core 담당자가 제공. Flow·Job에서 규칙 계산이나 Buffer 쓰기를 대신하지 않음 |
| F3 결과 읽기 계약·서비스 등록 | 실제 타입·소유 위치·공개 조회 경로를 Core/Framework 담당자와 확정한 뒤 연결 |
| F2 마지막 결과·EndBattle·중도 이탈 | Core·Framework·View가 수명과 호출 주체를 합의 |

이전 Core 계획 Task 4·8에 등장한 CombatPlan·CombatJob·JobRootType 변경은 위 분담에 맞춰 중복 작업을 피하도록 확인을 요청한다. 옛 계획 자체를 현재 API의 근거로 삼지는 않는다.

### 13.2 처리 서비스 네 개와 기록 요청

Phase 0의 소스 확인 결과, `IInGameCoreSystemGroupService`에는 StageGeneratorSystem·StageServiceManager·ChapterProgression만 있고 전투 서비스 노출은 없다. `CombatStateBuffer`에는 읽기용 순번·처리 번호가 있지만 갱신 구현은 없다. 아래는 **제공 요청**이다.

| 처리 | 필요한 서비스 계약·동작 | 실제 API 상태 |
|---|---|---|
| 준비 | Core가 준비 재료의 출처와 조립을 책임지고 초기 전투 상태를 설치. 정상 반환 후 HasBattle 확인 가능 | 메서드·입력 출처 확인 필요. 기존 BattleWriteSink.PrepareBattle만으로 처리 서비스가 완성된 것은 아님 |
| 명령 | HandledCommandNumber 기준으로 다음 명령 하나를 처리. 수락·거절 모두 처리 번호와 결과를 기록 | `CoreGroup.CombatCommands.TryExecuteNext()`는 설계에서 유지하려는 이름이며 현재 코드에는 없음 |
| 미리보기 | 현재 요청을 읽어 계산하고 응답 번호 기록. 게임 상태·결과 순번은 변경하지 않음 | 서비스 이름·반환 의미·그룹 노출 미정 |
| 자동 진행 | 한 호출의 종료 결과 하나를 상태·결과 순번과 함께 기록. 수락/거절·이유 조회 가능 | 서비스 이름·반환 의미·그룹 노출 미정 |

확인할 사항:

- 네 API의 파일·네임스페이스·어셈블리·정확한 멤버와 CoreGroup 조회 경로
- R1 결과 순번, R2 명령 처리 번호, R3 미리보기 응답 번호, R4 국면 갱신 시점
- 명령 서비스의 bool이 처리 여부를 뜻하는지 확인. 게임 규칙의 수락 여부와 분리
- 한 처리는 한 번의 동기 호출에서 끝나고, Flow가 등록한 경로 외에서 상태·최신 결과를 덮어쓰지 않는 전제
- 준비 재료의 출처(Core 이전 계획 G7 관련), 정상 종료와 예외의 보장

예외는 Job이 원인을 보존해 전파한다. 자동 진행 거절은 정상 반환해도 Flow 정지 대상이며 명령 거절과 구분한다.

### 13.3 F3: 타입·읽기 서비스 등록·실제 결과 기록을 구분

| 제공 단계 | 요청 내용 | 필요한 시점 |
|---|---|---|
| 읽기 타입 | 결과 순번, 처리 종류, 수락/거절, 이유와 조회 방식. 가칭 ICombatResultReadSink의 실제 이름·값 타입·파일·어셈블리 확정 | Phase 4 Flow 단위 구현·검증 준비 전 |
| 읽기 서비스 구현·등록 | 실제 인스턴스 제공, Export/서비스 키 또는 그룹 속성, LifeCycle에서 조회할 수 있는 경로 | **Task 4.4 및 Phase 4 생산 코드 연결 완료 전** |
| 실제 기록·보존 | 자동 진행 결과·상태·순번의 일관된 기록, View 확인 후에도 다음 결과로 교체될 때까지 최신 레코드 조회 가능 | Phase 5 실제 처리 검증 전 |

Runtime 순서는 Runner → View → Flow다. View가 먼저 결과를 확인·정리해도 Flow가 자동 진행 수락 여부를 읽을 수 있어야 한다. 결과 목록을 비우는 정책과 최신 레코드 보존이 충돌하지 않는지 확인을 요청한다. 순번 증가만으로 자동 진행 성공을 판정하지 않는다.

CombatLifeCycle은 전투 진입 전에도 PostBindService를 받는다. 따라서 **타입만 정의하고 실제 읽기 서비스를 등록하지 않은 상태는 LifeCycle 바인딩 완료가 아니다.** Flow 단위 검증에는 가짜를 쓸 수 있지만 생산 바인딩을 통과시키려고 빈 결과 서비스를 임의로 만들지 않는다.

### 13.4 F2: 결과 확인·전투 종료 수명 요청

Framework와 협의할 전환 순서는 새 Flow 등록·View 입력 중단 → 기존 Job 종료 → 마지막 결과 표시·확인과 Core 정리 → 최종 Idle → Change/Exit다. 입력 Clear는 기존 Job이 입력을 읽은 뒤에 수행한다.

Core 쪽 확인 요청:

- 마지막 결과 표시·확인과 `EndBattle` 중 어느 호출이 먼저이며 누가 책임지는가
- 정상 전투 종료와 중도 이탈에서 결과 반영·취소·정리의 차이
- 다음 전투에 이전 상태·미확인 결과가 남지 않도록 번호·결과·준비 수명을 어떻게 보장하는가
- 정리에 별도 Job이 필요한가. 필요하다면 전환 전에 완료를 확인할 경로는 무엇인가

F1의 실행 예외 뒤 Runtime 중단은 별도 Framework 작업이다. 정상 이탈 절차로 치명적 실패를 복구했다고 처리하지 않는다. [Framework 전달 문서](2026-09-30-combat-flow-framework-share.md)에 F1·F2 요청을 함께 적었다.

### 13.5 답변·인수 기록

| 항목 | 요청 문서 | 담당자 전달 | 답변·합의(담당/날짜) | 실제 파일·멤버·등록 경로 | 구현·검증 증거 |
|---|---|---|---|---|---|
| Flow/Plan/Job 소유 경계 | 준비 완료 | 미확인 | 미수신 | 우리 예정 파일은 구현 계획 §4 | 코드 미착수 |
| 준비·명령·미리보기·진행 서비스 / R1~R4 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 읽기 타입 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 읽기 서비스 구현·등록 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F3 결과 기록·보존 | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F2 결과 수명·EndBattle | 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |

Phase 0은 이 요청을 전달 가능한 파일로 준비하는 단계다. 답변을 받기 전 Core 인터페이스·저장 구조·처리 서비스를 View 작업에서 추측해 추가하지 않는다.

### 13.6 병합 후 인수와 F2 추가 요청 (2026-10-02)

**현재 인수:** `83f485d1`의 `ICombatProcessingService` 준비·명령·미리보기·자동 진행 API와 실제 결과 발행·보존. Flow·Plan·Job은 View 소유를 유지한다. View의 Phase 5는 명시적 준비 fixture로 Core 수정 없이 진행할 수 있다. 병합 후 사용자 확인은 컴파일 오류 없음·EditMode 미실행이다.

**추가 요청 초안 — 미전달·미합의:** 현재 `AbandonBattle(ArtifactLoadoutSnapshot)`는 호출자가 정산 입력을 제공해야 한다. View는 Core의 DB·규칙 입력을 조립하지 않으므로, Core가 현재 로드아웃을 읽어 포기 정산까지 수행하는 공개 경계(예: 인자 없는 호출 가능 여부)를 확정해 주기 바란다. 정확한 API 이름·반환·중복 호출 의미를 받기 전 View에서 가칭 구현을 만들지 않는다.

| 확인할 계약 | 현재 코드와 제안 |
|---|---|
| 포기 결과 | 현재는 패배 Completion을 기록하며 새 CombatResult·결과 순번·Finished 전환을 만들지 않는다. 이 의미를 유지하는지 확인 요청 |
| 기존 완료 | 이미 Completion이 있으면 정상 승패 정산을 포기로 덮어쓰지 않는다. View 전환 소유자도 중복 정산을 요청하지 않음 |
| 결과 표시 수명 | 남은 일반 CombatResult 확인 → 필요 시 포기 정산 → Completion 표시 완료 → EndBattle → 전환. 표시 완료는 View 수명 콜백이며 가짜 결과 순번을 만들지 않음 |
| EndBattle | Completion 전에는 거부하고, 정리 뒤 결과·번호와 Completion은 유지하며 다음 준비에서 전투 Completion을 초기화하는 현재 보존 의미 확인 |
| 운영 준비 | 기본 DB 소스는 등록됐지만 T1/G7의 타게팅 Method_Key·유닛 참조/query 계약이 부족함. Core가 출처·조립을 완성하고 운영 DB 성공 증거를 제공하는 별도 인수 필요 |

F1은 View의 GameBootstrap·단일 RuntimeFaultBoundary로 구현하고 골격 Phase 6에서 인수한다. F2 전환 요청 소유자·공개 정지 경로는 Framework와 별도 협의한다. 이번 작업은 문서 요청 준비이며 Core 코드 수정·외부 메시지 전송·담당자 합의 완료가 아니다. 상세 순서와 검증은 [설계 §6.2·§12.4](../specs/2026-09-30-combat-flow-skeleton-design.md)와 [인수 안내 §2.7](../plans/2026-09-30-combat-flow-core-intake-guide.md)를 따른다.

### 13.7 전투 View 착수 메모 (2026-10-02)

전투 View Phase 0에서 Core에 보낼 답변·확인 요청을 [전투 View Core 전달 메모](2026-10-02-combat-view-core-notes.md)에 따로 정리했다. 내용: 준비 인덱스·리비전은 당장 필요 없음(답변), 거절 `Reason` 코드 목록 확인, 시작 재화를 채우는 공개 경로(운영 전 결정), 시작 서브 수 규칙(고정 3 vs 덱 상황에 따라), 개발 전용 진행 준비 경로 알림. 모두 미전달이며 급하지 않다.

### 13.8 골격 6B 진행에 따른 Core 요청 정리 (2026-10-05, 미전달)

최신 요청·담당 경계·인수 조건을 날짜별 독립 문서인 [2026-10-05 Combat Core 요청사항](2026-10-05-combat-core-requests-phase6b.md)으로 옮겼다. 이 절은 기존 링크를 위한 이동 안내이며, 요청 내용은 새 문서에서 관리한다.

### 13.9 전투 타임라인 예측 Buffer 게시 요청 (2026-10-06, 미전달)

View가 `GetTimeline`을 직접 부르는 대신 Core가 예측을 Buffer에 게시하도록 요청한다. 계약 원본은 [구조 개선 설계 VR0](../specs/2026-10-06-combat-view-refactor-design.md#vr0--타임라인-게시-요청), 전달용 요약은 [타임라인 요청 메모](2026-10-06-combat-timeline-core-request-memo.md)다. 미전달·미합의·미구현이며 View는 합의 전까지 현재 호출을 유지한다.

- **2026-10-09 갱신(미전달):** Buffer에 이미 `CommandState.Timeline`이 있고 `TimelineUI`가 읽는다. HUD는 여전히 `GetTimeline()`을 직접 부른다. 두 값의 관계(같은 예측인지, 길이·갱신 시점)를 묻는 질문을 메모 §5에 더했다. 근거는 [SOLID·SSOT 평가](../specs/2026-10-09-combat-view-solid-ssot-evaluation.md) §4.1.

### 13.10 전투 진행 Job·Plan 재구성 알림 (2026-10-10, 호출 위치 이전 완료·미전달)

전투 진행 판단을 `CombatPlan.Decide` 한 곳으로 옮기고 전투 진행의 Core 호출을 Job으로 이전했다([재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md), [Task·실행 기록](../plans/2026-10-10-combat-job-plan-restructure-plan.md)). R3 코드 기준은 `1e7fdda5`이며, 2026-10-10 사용자가 전체 확인 완료를 통보하고 R4 진행을 지시했다.

**Core 요청·Core 코드 변경은 없다.** 처리 API의 의미·서명은 그대로이고, `AbandonBattle`은 `CombatAbandonJob`, `EndBattle`은 `CombatEndBattleJob`, `CompleteChapter`는 `CombatRunEndJob`이 호출한다(K22 A2 변경). `EnterBattle`·명령·미리보기·자동 진행·새 Run도 Job에서 실행한다. 진행 사실은 우리 영역의 `CombatProgressReader`가 Buffer·View에서 읽으며 Core 중계 조회 API를 추가하지 않았다. 처리 직후 확인 실패는 예외 → 기존 F1 오류 경계로 모인다. 이 알림과 [민형님 공유 문서](2026-10-10-combat-job-plan-framework-share-minhyeong.md)는 작성 완료이며 외부 전달·담당자 회신은 아직 확인하지 않았다. 타임라인 요청(§13.9)은 별도 후속 작업으로 남는다.

### 13.11 회복 UI와 후속 비전투 이벤트 Core 계약 요청 (2026-10-12 인수 갱신)

사용자가 [비전투 이벤트 UI 구조 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md)에 따라 Core 요청과 선행 View 작업을 나눈 [Task 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md)을 요청했다. **세부 요청·계약·담당·인수 조건의 원본은 [회복 Core 계약 요청](2026-10-09-recovery-core-handoff-Hojin.md#2부-회복-이벤트-ui-연결을-위한-core-계약-요청)이다.** 이 절은 요청 문서의 연결 위치로 유지한다.

RC0 계약 합의 → RC1 타입 제공 → View 입력 Buffer V4와 Core 서비스 RC2 → RC3 번호 처리 → RC4 인수로 나눴다. V1~V3 카드·입력 수명·표시 자산 준비는 Core를 기다리지 않는다. 기존 C1~C5의 실패 불변·중복 방지를 유지하고, 사유 있는 결과·선택 가능 상태·진행 문맥·취소 경계를 요청한다.

2026-10-12 HEAD `e3305027`에서 RC1 `5cc2c2b8`·RC2 `fa618534`의 실제 계약/서비스를 대조해 [View 계약 인수 기록](2026-10-09-recovery-core-handoff-Hojin.md)을 작성했다. 이후 별도 사용자 승인으로 **V4 입력 Buffer·공용 등록·Q4 검증을 완료하고 Phase D에서 종료**했다. 실제 API·변경 파일·XML/로그·A1/A2 책임은 [V4 Core 인계서](2026-10-09-recovery-core-handoff-Hojin.md), 실행 기록은 View 계획 §21을 따른다. 사용자 커밋·push·Core 실제 병합/인수와 RC3·RC4는 대기 중이다(당시). **2026-10-13 갱신:** V4는 사용자 커밋 `3aad1e83` 뒤 Core 브랜치에 병합됐고(`58c2f12e`), RC3-A(번호 처리·결과 보존)는 천명님 `535554e7`로 완료돼 이 브랜치에 들어왔다(`56c38c3a`, Core 계획 §14 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#14-phase-3-execution-record-2026-10-13`)). RC3-B(Stage Event 진입)는 Stage/Run 인계 3건을 기다리며 멈춰 있고(Core 계획 §15 (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md#15-phase-4-prerequisite-inspection-2026-10-13`)), RC4는 미착수다. 상태 원본은 [회복 Core 계약 요청 §10](2026-10-09-recovery-core-handoff-Hojin.md#10-회신-기록)이다. 공용 파일 순서는 View 입력 등록→Core 결과/처리→조립 검증이다.

Core 계약 우선 경로를 유지하며 R-A 최대 체력 계산 Guard를 구현하지 않는다. 선택지 1·2·3 연결 완료는 View 계획 §18의 이력이고 외형·미정 규칙은 D0에 남는다. 이번 V4는 입력 코드와 허용된 공용 Buffer Group/Factory/키만 수정했으며 CoreSystem 계산·서비스·결과/transaction은 유지했다. 새 Unity 검증은 Q4a 318건·Q4b 1,028건 통과이고, 회복 운영 Play나 RC3/RC4 인수로 확대하지 않는다. 외부 전달·스테이징·커밋·push는 하지 않았다.

### 13.12 로비 Core 계약 요청 (2026-10-11, 미전달)

사용자 요청으로 [로비 구조 설계](../specs/2026-10-11-lobby-design-Hojin.md)를 작성했다. **요청의 원본은 설계 §10**이며, LC0 로비 선택 상태·덱 의미 합의, LC1 첫 Run 전 메인 캐릭터/로드아웃 읽기, LC2 덱 확정, LC3 기존 새 Run 시작 경계와의 연결, LC4 기존 아티팩트의 로비 사용 조건으로 나눈다.

`StartNewRun(loadoutKey)`와 아티팩트의 기존 편집·번호 경로·새 Run 간 보존은 제공된 기능으로 인정한다. 같은 서비스를 다시 요청하지 않는다. 첫 Run 전 선택 상태와 운영 덱 편집/확정의 미제공 경계만 확인하고, 아티팩트는 초기화·씬 이탈 중 요청 수명을 인수한다. 회복 RC0·RC1과는 별도 요청이다.

체스 말처럼 들기·이동·내려놓기 후 시설 실행, 씬/카메라, 기존 글로벌 UI 연결은 View 책임이다. 사용자 추가 결정으로 아티팩트 편집 진입점은 로비 4번에만 두고 StageSelect에서는 제거한다. 기존 보유·배치 상태와 전투 효과는 유지하며 최초 초기화/출발 시 보존 조건을 LC4에서 확인한다. 세부 선행 작업은 [로비 구현 Task 계획](../plans/2026-10-11-lobby-plan-Hojin.md)을 따른다. LV1~LV4는 선행 View 준비, LV5는 LC4 인수 후 운영 이전, LV6~LV8은 각각 실제 캐릭터·덱·출발 계약 인수 후 연결한다. 최초 작성 시점(10-11)에는 계획 단계였다. **현재 LV5와 기본 출발 LV8A를 완료**했으며 기존 LC4·새 Run 계약을 재사용한다. 구현·검증 원본은 [로비 계획 §14.9](../plans/2026-10-11-lobby-plan-Hojin.md#39-lv5-아티팩트-로비-이전)다. LC0~LC3는 별도 대기이며 외부 전달·Core 담당자 합의 완료를 뜻하지 않는다.

**2026-10-13 LC4 점검:** 기존 초기화·번호 편집·새 Run 보존·전투 효과 API는 제공돼 있다. 새 서비스를 요구하는 대신 **로비 적용과 현행 P7 번호 계약의 인수 확인**을 요청한다. 코드/테스트 근거와 전달할 요청 원본은 [로비 설계 §10.1](../specs/2026-10-11-lobby-design-Hojin.md#101-lc4-준비-상태와-core-확인-요청-2026-10-13), 수행 기록은 [로비 계획 §14.8](../plans/2026-10-11-lobby-plan-Hojin.md#38-lc4-기존-core-계약-재사용)이다. 함께 전달할 P7 문서와 View 담당 범위도 원본에 정리했다. 이번 점검에서 Core 코드 수정이나 외부 발송은 하지 않았다.

**같은 날 사용자 확인 후 정정:** StageSelect 전용 Core 제한은 없으며 기존 계약을 재사용해 LV5를 진행하기로 했다. 외부 회신을 필수 선행으로 둔 이전 판단은 철회하고 코드 근거와 사용자 승인을 LC4 사용 조건 확인으로 기록한다([설계 §10.2](../specs/2026-10-11-lobby-design-Hojin.md#102-lc4-기존-계약-재사용-확정-2026-10-13)). 새 Core 변경이 필요한 경우에만 추가 요청한다. LC0~LC3는 별도 대기다.

**2026-10-13 기본 출발 분리:** 사용자가 기본 로드아웃 1로 항상 새 Run을 시작하는 LV8A를 먼저 연결하도록 승인했다. 기존 `IRunStartService.StartNewRun(int)`·`RosterAcquireResult`를 재사용하고 LC0~LC3 회신을 이 기본 출발의 선행으로 두지 않는다. 선택값/자유 덱 계약은 LV8B·LV6/7의 별도 대기다. [계획 §11](../plans/2026-10-11-lobby-plan-Hojin.md#310-lv8-기본-출발-lv8a와-선택-덱-출발-lv8b)이 범위·수정 파일의 원본이다. 계산·검증 Core API 수정은 없으며 `BufferSystems/Core/BufferSystemGroup.cs`에는 새 View 출발 Buffer의 Read/Write 접근자·조회 연결만 추가한다. 기존 거래/저장소/Run 초기화 본문은 유지한다.

### 13.13 StageSelect 요청 처리 번호 기록 요청 (2026-10-10, 미전달)

> **2026-10-13 복원:** 병합 `9d4c76e5`에서 빠졌던 절을 같은 번호로 되살렸다.

아티팩트 편집·프리뷰, 상점 행동, **제출된 모든 맵 진입 시도(전투·상점·미지원 이벤트/회복·없는 노드 포함)**에서 Core가 처리 번호와 성공/정상 거절 결과를 Core 쪽 Buffer에 남기도록 요청한다(H1~H4, SA 4차 확대). 전투는 기존 준비/활성화 경로에 맵 번호를 전달하며 실행 전 취소에는 가짜 Core 기록을 만들지 않는다. 원본은 [처리 번호 요청](2026-10-06-stageselect-core-requests-Hojin.md#4부-stageselect-요청-처리-번호-기록-요청-view--core). 외부 전달·합의는 아직 없지만 사용자 승인으로 H1~H4를 로컬 구현했다. 현재 H1/H2 직접 조회는 완료, H3/H4 직접 조회 후속의 구현·자동 테스트와 최종 빌드·독립 검토까지 완료했다(§5.13·§5.18). 최초 요청 당시의 미구현/기존 경로 설명을 현재 상태로 사용하지 않는다.

### 13.14 Run seed 도입 요청 (2026-10-09, 미전달)

> **2026-10-13 복원:** 원래 §13.10. 병합 `9d4c76e5`에서 빠졌고 그 번호는 지금 전투 Job·Plan 재구성 알림이 쓴다. 상태(2026-10-13 확인): 미전달·미구현. 임시 seed는 `JobRegisterSystem`의 `new Random().Next` 하나를 맵·상점 진입에 넘긴다.

상점 `EnterShop`의 seed를 만들 Run 단위 기준값이 Buffer에 없다. Run 시작 때 seed를 만들어 Run Buffer에 보관·공개하고 기능별 유도 규칙을 Core가 갖도록 요청한다. 요청 원본은 [Run seed 요청](2026-10-06-stageselect-core-requests-Hojin.md#3부-run-seed-도입-요청-view--core)이다. View는 그 전까지 진입마다 무작위 seed를 쓴다(상점 설계 Q1 B안).

### 13.15 회복 결과 반환 요청 (2026-10-09, 미전달)

> **2026-10-13 복원:** 원래 §13.11. 병합 `9d4c76e5`에서 빠졌다. **대체됨:** 이 요청의 C1~C4는 §13.11 회복 계약의 RC2 결과 타입(`RecoveryOperationResult`)으로 대체됐다. 아래 "임시 방어" 방침도 §13.11의 "R-A Guard를 구현하지 않는다"로 바뀌었다.

`Recover`가 골드 부족·회복 효과 없음 같은 정상 실패에도 예외를 던져, Job을 거쳐 런타임 전체가 정지한다. 실패 사유를 가진 결과 반환과 실패 시 상태 불변·중복 방지 보장을 요청한다. 요청 원본은 [회복 결과 요청](2026-10-09-recovery-core-handoff-Hojin.md)이다. View는 기다리지 않고 현재 Core 기준으로 구현하며, 골드 부족·체력 가득·다른 선택지 적용은 버튼을 꺼서 막는 임시 방어를 둔다(반영되면 삭제).

### 13.16 통합 기획서와 상점·회복·맵 규칙 차이 (2026-10-09, 미전달)

> **2026-10-13 복원:** 원래 §13.12. 병합 `9d4c76e5`에서 빠졌다. 상태: 미전달.

기획서 Ver0.3의 상점은 "목록에서 무작위 뽑기"이고 현재 Core는 "진열 캐릭터 구매"다. 회복 선택지 수·가중치, 비전투 5종, 맵 가중치도 다르다. View는 현재 Core 기준으로 상점을 먼저 만든다(A안). 원본은 [기획서 차이 요청](2026-10-06-stageselect-core-requests-Hojin.md#2부-통합-기획서와-상점회복스테이지-규칙-차이-view--core기획-확인-요청)이다.

### 13.17 적 AI 배지 보유 대상 공격 선택 확인 요청 (2026-10-13, 미전달)

적 AI가 보안관 배지를 가진 대상을 공격 후보에서 빼서, 배지를 얻은 유닛이 공격받지 않는 문제의 정책 확인 요청이다. 배지는 전투 스테이지 11개 모두에 있다. 원본은 [적 AI 배지 공격 확인 요청](2026-10-13-combat-core-requests-Hojin.md)이며 처음에는 §5.12에 적었다.

### 13.18 StageSelect Event → 회복 진입 연결 (2026-10-13, 설계·요청 작성/미전달)

사용자 요청으로 [연결 설계](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#2-지금-구조)와 [Task 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)을 작성했다. **담당·요청·수락 증거의 원본은 [Event 회복 진입 요청](2026-10-09-recovery-core-handoff-Hojin.md#5부-event-노드-회복-진입-core-연결-요청)**이다. §13.11의 제공된 회복 서비스·V4·RC3-A는 재사용하며, 미완료 RC3-B/RC4를 다시 구현하는 병렬 계획을 만들지 않는다.

Stage/Run 담당(호진)의 교체 전 입력 취소·같은 키 새 Run 회귀·Core 재확인 정보 인계(S1~S3)를 먼저 요청한다. Core 담당(천명)에게는 기존 노드 endpoint의 Event 분기, 선택/Context/옵션/맵 결과의 원자적 진입, origin/seed 회귀, Factory·RC4 인수(C1~C4)를 요청한다. View는 runtime 요청 수명·Flow·P5 이후 회복 팝업·엄격 모달·Unity 인수를 맡는다. 이번에는 문서만 작성했으며 Core/제품 코드 수정·실행 검증·외부 발송은 하지 않았다.

**2026-10-14 ER1 후속:** 사용자 승인으로 호진 Stage/Lobby 교체 전 취소를 구현하고 Unity 회귀를 실행했다. [ER1 실행·origin 인계](2026-10-09-recovery-core-handoff-Hojin.md#6부-er1-stagerun-origin-선행-작업-인계)에 실제 호출자·queued 같은 키 새 Run·조회 중 번호 무효화·XML·독립 리뷰를 기록했다. Core 파일 수정 없음. 이미 시작한 Shop 조회의 옛 결과 설치는 남아 있어 Core 설치 직전 재검사와 Combat 이탈/Detach 선행 조건 인수가 필요하다. 요청/수락 상태 원본은 위 Event 회복 진입 요청을 유지하며, ER2는 시작하지 않았다. 외부 발송 없음.

### 2026-10-04 머지 후 View 연결 복원
사용자 요청으로 상대방 Core·Combat 조립을 유지하며 StageMapViewSystem을 함께 등록했다. BufferSystemGroup.cs에는 StageView·ArtifactView 네임스페이스 using만 복원했고 계산·발행 로직은 수정하지 않았다. Buffer Factory·서비스 키에도 같은 참조를 복원했다. 진행 기록과 실제 검증 결과는 stage-map-view-plan.md §7을 참조한다.

### 2026-10-04 카메라 병합 충돌 해결
사용자 명시 지시로 b5d14d7 병합 시 MapProgressBuffer의 최신 IDataBaseQueryContext 및 Store 계약을 유지했다. 자동 병합으로 바뀐 StageGeneratorSystem·StageServiceManager 초기화도 현재 Core 계약으로 유지했다. Core 계산 변경 없음. 렌더 API 이동에 따른 전투 View 참조를 새 경로·Handle 명칭에 맞췄다. 검증 상태는 stage-map-view-plan.md §7 참조.


### 13.19 공통 레벨의 꺾인 Wall 형상 지원 요청 (2026-10-13, 미전달)

사용자 그림의 일반 A~J·Boss 9×9 배치에 Wall 12개를 작성했지만 View 쪽에서 Core의 직선 연결 성분 제한을 누락했다. 수정 전 SO는 `벽 연결 성분은 가로 또는 세로 한 줄이어야 합니다. 원본: 201@(1,2), 202@(2,2), 205@(2,3)`로 거절됐다. 후속 C1·C2는 형상 제한을 수정했고 C3에서 운영 에셋/Unity 인수를 통과했다. 위치·SSOT·배치 대안·재현 증거는 [공통 배치 계획](../plans/2026-10-13-combat-common-layout-plan.md)의 마지막 절을 따른다.

그림 유지 시 필요한 요청: WallGeometryBuilder가 ㄴ자 연결 성분을 지원하고 실제 점유 셀의 합집합을 공격·이동 벽 판정에 반영해 달라. 네 성분은 {(1,2),(2,2),(2,3)}, {(6,2),(7,2),(7,3)}, {(2,5),(1,6),(2,6)}, {(7,5),(6,6),(7,6)}이며 Source Id는 y/x 순서로 201~212다. 현 WallRun은 성분 전체의 직사각 경계를 저장하므로 예외 검사만 제거하면 빈칸까지 차단하는 잘못된 형상이 된다. CornerCutTicks의 외곽·안쪽 모서리 의미와 다른 성분의 대각선 접촉 정책도 Core 설계에서 확정해야 한다.

인수 기준: 위 네 성분의 DB 정의/전투 준비 성공, 실제 Wall 셀 이동 차단 유지, ㄴ자의 빈칸을 임의 벽으로 확장하지 않음, 공격 차단·관통·AI·전방/역방향 선분 및 직선 벽 회귀 유지, 원본 ID/셀 증거 보존. View가 판정을 복제하거나 Wall을 Obstacle/숨은 Wall로 치환하지 않는다. Native Unity 테스트와 운영 준비도 확인한다.

대안은 안쪽 네 칸(2,3)/(7,3)/(2,5)/(7,5)을 빼서 가로 직선 성분 4개·Wall 8개로 바꾸는 것이며 현재 실제 Core 형상 검증을 통과했다. 사용자 그림을 바꾸는 결정이므로 선택 전 적용하지 않는다. **당시 요청 작성 단계에는 Core를 수정하지 않았다. 후속 사용자 지시로 C1·C2를 구현했으며 §5.10을 따른다. 외부 발송은 하지 않았다.**

후속 사용자 결정(2026-10-13): 그림 유지 및 Core 수정 설계부터 진행을 선택했고 Core 작업도 본인 담당이라고 밝혔다. 별도 Core 작업자 승인/외부 회신을 기다리는 경로로 취급하지 않는다. 연결 Wall Core 설계 (Git history: `2026-10-13-connected-wall-geometry-design.md`)와 C1~C3 계획 (Git history: `2026-10-13-connected-wall-geometry-plan.md`)을 작성했다. 후속 “Phase C1, C2 차례대로 진행해” 지시로 구현·관리형 회귀를 완료했고 원래 12개 Wall 배치를 보존한다. 변경 파일·계약·검증 결과는 이 문서 §5.10과 Phase 계획 실행 기록에 있다. 후속 C3의 실제 운영 DB/Unity 검증을 완료했다(§5.10 후속 기록). 외부 발송은 하지 않았다.

## 14. Stage 1 boss SO preparation (2026-10-14, not sent)

Source of scope and confirmed values: Boss Unit SO Design (Git history: `2026-10-14-boss-unit-so-design.md`), §3; execution evidence: Boss Unit SO Plan (Git history: `2026-10-14-boss-unit-so-plan.md`), Phase 2. The user approved the configuration and Core identity addition. This addendum is a local handoff, not an externally sent message.

### 14.1 Prepared assets and exported contracts

Paths below are relative to `Assets/ProjectL/DataBaseSO/InGameGroup/`.

| Asset | Key | Status |
|---|---|---|
| `Unit/Item/Unit_Stage1Boss.asset` | `EntityKey.Unit_Stage1Boss = 18` | Existing Unit SO type; unregistered |
| `Targeting/Item/TargetingMethod_Stage1BossRoar.asset` | `5` | Fan / All / None / splash 0; unregistered |
| `Unit/Item/Unit_Gravekeeper.asset` | `EntityKey.Unit_Gravekeeper = 17` | Existing summon reference; unchanged and already in the Unit table |

The boss exports Regular / EnemyOnly / Boss, 1x1 footprint, HP 100, defense 0, attack 5, range 4, priority 30, delay 100, max AP 5, move/attack costs 5, and move range 0. Wall policies remain RequireClear / FromImpact. `UnitData.Behavior` exports `(CanMoveVoluntarily: false, CanBasicAttack: false, IsForcedMovementImmune: true)`.

Its existing Trait list exports:

- `NecromancyTraitData(SummonedUnitKey: Unit_Gravekeeper, MaxLivingSummons: 2, FirstActionDelay: 7L)`.
- `RoarTraitData(HitThreshold: 5)`.
- Existing `KnockbackTraitData(DistanceCells: 3)`; knockback distance has no duplicate boss field.

SO references are converted to keys; exported Unit/Trait data contains no Unity objects. Ordinary units preserve `(true, true, false)` behavior by default. Schema and serialization checks are recorded under Phase 1 in the plan.

### 14.2 Integration work still required

**2026-10-14 scope correction:** The user is the Core programmer and has permitted Core edits. Items 1-2 below are now designed as Phase 3 (Trait/Behavior conversion) and Phase 4 (summon definition/profile assembly) in the existing boss SO design/plan. They were scheduled for this session, not blocked on another Core owner. The documentation update itself did not implement them. Subsequent user-requested Phases 3 and 4 have completed items 1 and 2. Items 3-5 remain separate operational activation dependencies.

1. **Completed in Phase 3:** map `NecromancyTraitData`, `RoarTraitData`, and `UnitData.Behavior` in the Core battle-definition adapter to existing Core contracts (§5.17). The earlier enum-only change in §5.16 was not this conversion.
2. **Completed in Phase 4:** include reachable summoned unit definitions and attack profiles even when absent from the initial roster (§5.18). Actual saved boss/Gravekeeper SO data was verified through the existing source using an isolated query fixture; operational tables remain unregistered.
3. **Phases 5–6 complete:** authored objective and per-placement target flags reach real Core preparation/Buffer, including the approved generic unit-key validation fix (§5.20). Evidence: stage objective plan (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`). No stage-ID, boss-key, or category dispatch. `Combat_Boss.asset` remains unchanged.
4. Provide and register the matching UnitVisual through View ownership. View continues to consume summon/roar results through Buffer; it does not read InGame data directly.
5. View owns live Unit/Targeting/UnitVisual registration, stage placement, and real boss-battle verification in Phase 7. View chooses the final content values using the boss-only lineup at (4, 8), R180 as a proposal; no Core permission is needed unless CoreSystem code must change. Fixed placement already supplies the boss definition root, so shared roster/loadout changes are unnecessary. Update the prepared-asset test's temporary unregistered assertions only in that integration change.

The new boss and targeting assets are intentionally absent from live tables, rosters, loadouts, and stages. **SO preparation is not runtime readiness; no boss spawn, skill playback, or PlayMode success is claimed.** No generic editor tooling, new service/module, or alternative boss data model was added.

### 14.3 Verification

Unity 6000.0.73f1 EditMode: Boss authoring 19/19, InGame data 30/30, DB assets 24/25; total 73 passed, 1 failed, 0 skipped, Unity exit 2. The only failure is the unchanged pre-change baseline in `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage 1101 terrain index 5 expects (7, 3), actual (6, 3). The complete assertion message matches the baseline; no new test failed. The suite is not fully green. Persisted boss/targeting reload and exports are verified, with no compiler errors; the existing unrelated CS0105 warning remains. Detailed RED and final evidence are in the plan execution record. No PlayMode or runtime-readiness claim is made.

**Phase 3 verification (2026-10-14):** Actual saved boss SO now converts into Core Traits/Behavior. Unity EditMode: 144 passed, 1 failed, 145 total, 0 skipped, exit 2; all nine new cases pass and BattleDefinitionAdapterTests is 53/53. The sole failure name and full assertion match the fresh 135/136 baseline: the same Stage 1101 terrain-coordinate mismatch above. No new failure or final C# compiler diagnostic. A fresh independent read-only review found no actionable issue in the Phase 3 diff and confirmed saved XML evidence; it did not rerun Unity or assess Phase 4/activation. No PlayMode verification. Phase 4 is still required to include summon-only definitions and their profiles.

**Phase 4 verification (2026-10-14):** Source assembly includes the boss and its summon-only Gravekeeper definition/profile without adding placements, owned cards, or runtime battle state. Unity EditMode: 159 passed, 1 failed, 160 total, 0 skipped, exit 2; all 15 new cases pass and BattleDefinitionAdapterTests is 68/68. The sole Stage 1101 failure name and full assertion exactly match the fresh 144/145 baseline. No compiler errors; existing CS0105 warning remains. Fresh read-only review found no actionable issue and confirmed saved evidence, without rerunning Unity. The earlier Phase 3 note about pending Phase 4 is historical. Items 3-5 in §14.2 remain pending; this is not operational boss-stage or PlayMode verification.

**Phase 5 verification (2026-10-14):** Stage authoring/data only; 20 new tests all pass. Final selection is 161 passed, 1 failed, 162 total, 0 skipped, Unity exit 2; the sole Stage 1101 terrain assertion exactly matches the fresh 141/142 baseline. No compiler errors; existing CS0105 warning remains. Independent review found no actionable issue. Inspector exposure is verified by code/property paths, not manual UI. Phase 6 forwarding/preparation and Phase 7 activation remain pending; no production assets or CoreSystem/View files changed in this Phase. Full evidence: stage plan execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`).

### 14.4 View 활성화 상태 (2026-10-15, View 작업, 미전달)

- 실행 원본: [보스 View 활성화 설계](../specs/2026-10-15-stage1-boss-view-activation-design.md)·[계획](../plans/2026-10-15-stage1-boss-view-activation-plan.md) §6. **CoreSystem 코드 변경 없음, Core 요청 없음.**
- §14.2 항목 4·5 처리:
  - `UnitDataTable`에 `Unit_Stage1Boss`, 지정 방식 표에 key 5, `UnitVisualDataTable`에 새 `UnitVisual_StageBoss1`(key 18, Melee 공격·기본 반응)을 등록했다.
  - `Combat_Boss.asset`은 목표 DesignatedBossDefeated, 본부·EnemyLoadout 없음, 보스 1개 (4, 8)·**R0**·승리 대상이다. 위 R180 제안 대신 R0으로 정했다. 적 모델은 View가 180도 돌려 플레이어를 보게 하므로 생성 적과 같은 R0이다.
  - TraitVisual Necromancy(80)·Roar(90)을 추가했다. 아이콘은 비어 있다.
  - 시험 `PreparedBossAsset_ExportsConfirmedConfiguration`의 임시 미등록 단언을 등록 단언으로 바꾸고, `OperationalBossStage_HasSingleDesignatedBossInsideEnemyZone`을 추가했다.
- View 표시: 목표 마커(`Objective.TargetEntityId`)·포효 카운터(`HitCount/HitThreshold`)·"보스 격파" 문구, `Roared`(공격 결과로 공격 연출)·`Summoned`(칸 강조·간격)·연속 `Moved`(넉백 경계 수정)를 처리한다. 키·스테이지·Boss Category 분기는 없다.
- 검증: MSBuild 오류 0, 전투 View 관리 실행 회귀는 기준선 대비 새 실패 0이다. 보스 등장은 개발 진입(이어진 노드로 들어가 정의만 보스 스테이지로 바꿈)에서 사용자가 확인했다. **Unity EditMode 결과와 운영 경로(StageSelect 보스 노드) 전투 확인은 대기 중이다.** HUD 설명 칸은 `HUD.prefab` 미연결로 아직 보이지 않는다(현강님 메모, 미전달).

**Phase 6 verification (2026-10-14, current):** 287 passed, 1 failed, 288 total, 0 skipped, Unity exit 2; all 59 new cases pass. Adapter111/111; key/static-stat37/37. The sole Stage1101 terrain assertion exactly matches the original Phase6 baseline. No compiler errors; existing CS8632 in unchanged CombatCommandTestData.cs:96. Independent final review found no actionable issue. Prior pending Phase6 statements in historical records are superseded. Remaining work is View-owned Phase7 content activation and gameplay acceptance. No commit/push or external delivery.

## 15. Undead Heavy and Regeneration (2026-10-15, UR1-UR3 complete)

Source of requirements and open decisions: design (Git history: `2026-10-15-undead-heavy-regeneration-design.md`). Phase ownership, exact files, and verification: implementation plan (Git history: `2026-10-15-undead-heavy-regeneration-plan.md`). **Current direction (revision 2):** the user requested keeping Regeneration settings in the existing unit SO Traits list after reconsidering the benefit of dedicated settings assets. This supersedes the earlier separate trait SO/table and migration proposal. The user subsequently authorized UR1-UR3; contracts/rule/registration, turn-completion recovery and unit authoring/shared conversion are implemented and verified in sections 5.24-5.26. No production Regeneration asset edit or external delivery occurred.

Core status: UR1 supplies the Regeneration value/registry contracts and pure capped-healing rule. UR2 connects the separate recipient/application calculator at accepted unit-turn completion. Existing own-turn effects, enemy/boss shared completion, and normal health delta/event publication remain intact. No own-turn broadcast or second turn counter was added. The written D1/D2 death-interruption and environment/night ordering policies are implemented and tested; D3-D6 remain open for content acceptance.

DB boundary implemented in UR3: RegenerationTraitAuthoring and pure RegenerationTraitData use separate files and the existing UnitDataSO SerializeReference list/export loop. One conversion case in UnitSpecAdapter.ReadUnit(UnitData) retains its signature and both existing consumers (battle definition preparation, including summon dependencies, and roster stat queries). Recovery amount is authored per unit definition. No new SO/table, query capability, service key, factory wiring, or existing-trait migration. Existing trait values and null/duplicate validation remain unchanged. Production Undead Heavy entries still await UR4.

Required unit values, including CriticalChance = 10, are preserved in design section 2. Critical-hit runtime support is currently absent. Existing Heavy still supplies directional self armor in addition to HN1 knockback immunity; the new ally protection design is not implemented here. Content n, owner eligibility, move range, footprint confirmation, prefab, and entry route remain explicit integration inputs. Do not label partial Regeneration implementation as complete Undead Heavy gameplay. A later explicit phase implementation request is the execution authorization; this entry does not request an additional approval for work already authorized by the user.

## 16. Shielder and Intimidation design request (2026-10-15)

**View start point:** [Shielder and Intimidation View handoff](2026-10-15-shielder-intimidation-view-handoff.md). SI5 implementation tasks, verification selection and future execution records have moved there as their single source. The original plan keeps completed Core history, key reservations and SI6 joint integration tasks.

The Shielder and Intimidation design (Git history: `2026-10-15-shielder-intimidation-design.md`) is the source for character values and trait policies. The user confirmed Shielder (쉴더), Heavy, separate configurable attack-percentage/Priority parameters, expiry of both reductions after the victim's next reduced attack, and immediate influence on its pending action order. On 2026-10-15 the user accepted this design as the basis for the Phase and Task plan (Git history: `2026-10-15-shielder-intimidation-plan.md`), then authorized execution. SI1's Core changes are recorded in section 5.27; subsequently requested SI2-SI4 changes are in sections 5.28-5.30. SI4 is complete and stopped for review; SI5-SI6 have not started. No external delivery occurred.

Phase ownership: SI1 supplies the caster's trait contracts/registration and target-owned immutable debuff values; SI2 preserves them through projections, Buffer publication/equality, and cleanup; SI3 connects delivered-hit application, whole-attack expiry including reactions, the affected participant's schedule, and preview/AI/publication consistency. SI4 adds DB authoring and the shared Core adapter conversion; SI5 supplies View status presentation; SI6 integrates Shielder content after its remaining inputs are supplied. Do not insert an inflicted status into the victim's owned TraitStatuses, encode reduction as an unsupported negative bonus, or add a persistent status service. Design section 3 remains the approved gameplay baseline. Key reservations and SI6 tasks remain in the original plan; SI5 file ownership, tests and review stop are maintained in the dedicated View handoff.

## 17. Soul Drop SpawnSoul DB alignment (2026-10-15)

사용자 요청으로 실제 영혼 드랍 SO(1003)를 현재 Core SpawnSoul 계약에 맞췄다. 기존 SO 저작 불일치였던 Task 5 G9의 컴파일 거절은 이번 실제 에셋 수락 검증으로 해소했다. 값·사용법·View 후속 경계는 [영혼 드랍 DB 인계](2026-10-15-soul-drop-handoff-Hojin.md), 실행 수치·기존 DB 실패 두 건과 재현 증거는 SD1 계획·실행 기록 (Git history: `2026-10-15-soul-drop-spawnsoul-db-plan.md`)이 원본이다. 과거 G9 기록은 당시 이력으로 보존한다.

Core/Framework 구현·공개 API·Buffer·서비스·Factory·씬/프리팹 변경은 없다. 이 DB 정합을 위한 새 Core 수정 요청은 없다. 실제 전투 전체 인수와 영혼 필드 표시/수집·소멸 연출, 가시심장 반사 정산은 이번 완료 범위 밖이다. 커밋·push·외부 전달은 하지 않았다.

후속 SDV0에서 [Core 계약 확인 메모](2026-10-15-soul-drop-handoff-Hojin.md#2부-소울-드랍-core-계약-확인-메모--2026-10-15)와 [전투 View 구현 인계](2026-10-15-soul-drop-handoff-Hojin.md)를 작성했다(미전달). 사용자는 SDV1~SDV4 구현 담당 이관만 승인했고 구슬의 제안 표현 수치는 승인하지 않았다. 기존 공개 사실을 사용하므로 새 Core 구현 요청은 현재 없다. 가시심장 반사 또는 추가 Core 사실/규칙 변경이 필요할 때는 기존 경계대로 먼저 중단·문의한다. 진행/검증 원본은 [SDV0 실행 기록](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)이다.

## 18. Thorn Heart reflection connection request (2026-10-15)

**작성 당시 요청 기록:** 후속 Core 설계와 TH0~TH3 구현/검증 상태는 아래 §19가 원본이다. 아래 미지원·미확정·미구현 설명은 이 요청을 작성한 시점의 상태다.

사용자 요청으로 [가시심장 반사 처리 Core 요청 인계](2026-10-13-combat-core-requests-Hojin.md#2부-가시심장-반사-처리-연결--core-요청-인계)를 작성했다(외부 미전달). §17의 소울 드랍 기존 계약 인수와는 별도 요청이다. 현재 가시심장은 설정/조건 규칙까지만 있고 공격 투영 생성에서 미지원 예외로 중단한다. Core 담당자에게 일반/Reaction·Preview/AI의 반사 정산, 사망/후속 효과의 순서, 출처/금액/체력·사망/영혼/완료의 공개 결과와 기존 원자 게시·회귀 검증을 요청한다. 미확정 발동 단위·감소/방어·재귀·사망 순서는 인계서의 확인 항목으로 남겼으며 임의 확정하지 않았다.

이번 승인은 요청 문서 작성과 연결까지다. Core 코드·Buffer·DB·테스트·View는 수정하지 않았고 구현/테스트 실행 승인을 확대하지 않는다. 구현 후 반환할 계약/검증 자료와 View 후속 경계도 위 인계서가 원본이다. 소울 드랍 View의 선행 조건으로 가시심장을 추가하지 않는다. 문서 갱신 뒤 중단하며 커밋·push·스테이징·외부 발송은 하지 않았다.

## 19. Thorn Heart reflection connection design (2026-10-16)

The user confirmed the gameplay policies and requested a source-backed design and Task plan. The Thorn Heart design (Git history: `2026-10-16-thorn-heart-reflection-design.md`) section 2 is the policy source; the Phase and Task plan (Git history: `2026-10-16-thorn-heart-reflection-plan.md`) owns implementation scope, test coverage and execution status. The latest piercing exception supersedes earlier blanket cancellation wording: remaining piercing direct damage continues after reflected attacker death, while offensive follow-ups stop. Preview and AI evaluation omit reflection. See the design for the full policy rather than maintaining a second copy here.

Core request: reuse compiled artifact rules in an attack-local scope, insert the first-hit reflection checkpoint, preserve the existing piercing delivery and final publication boundaries, and replace the unsupported guard only together with actual integration. The proposed event context makes the triggering ally and reflection damage group explicit, including a triggering hit with zero HP loss. Existing health/death events remain the View baseline; dedicated reflection animation is outside this request.

The planning request changed no Core/Buffer/View/DB code or assets and ran no Unity tests. Implementation remains Core-owned unless the user explicitly authorizes this session to perform it. Each Phase ends at a review stop. No commit, push, staging or external delivery occurred.

**TH0 baseline execution (2026-10-16):** the user authorized TH0 only. Native Unity 6000.0.73f1 focused selection: 516 passed, 2 failed, 518 total; full selection: Core 2702/2702 passed, Framework 4262 passed/97 failed/4359 total, combined 6964 passed/97 failed/7061 total, no skips. Both runs exited 2; no C# compiler errors/warnings were observed. All focused results and failure labels/messages exactly match the full run. The plan section 8 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#8-th0-baseline-execution--2026-10-16`) records the 27 failing Framework fixture groups and links every failure's full name/label/message, raw XML/log, command arguments and preservation evidence. This is a failing pre-implementation baseline, not a claim of green tests or implemented reflection.

Artifact 1002 is authored once (InitialCount=1), amount 3, duration 0, with its existing footprint/GUID/table entry. The runtime inventory has no per-type singleton guard; future acquisition prevention is unproven. Current asset tests verify table data and synthetic tests verify the rule compiler; direct compilation of the actual Thorn Heart SO effect still awaits TH3-C's dedicated test. No new tests or product code were added in TH0. The existing working-tree contents and index were preserved; two Unity-generated settings line-ending rewrites were restored to their original representation. At the TH0 review stop, TH1-TH3 were pending.

**TH1 execution (2026-10-16):** explicitly authorized TH1-A/TH1-B are complete; see section 5.31 and plan section 9 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#9-th1-contracts-and-isolated-calculation--2026-10-16`). Optional event context and the isolated compiled-rule scope add 37 passing cases; focused 158/158 and full Core 2739/2739 pass. Full Framework is 4261 passed/98 failed, comprising the exact 97 baseline failures plus an entry-test error that passed its isolated rerun. Independent review found no actionable issue. Production attack integration is still guarded. TH2/TH3 remain pending; stopped for TH1 review without commit, push, staging or external delivery.

**TH2 execution (2026-10-16):** real ordinary-hit reflection is connected and the unsupported guard removed under the user's explicit TH2 request. See section5.32 and plan section10 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#10-th2-attack-connection-and-ordering--2026-10-16`). Nonpiercing cancels later hits after reflection death; piercing continues its complete direct delivery, while both suppress offensive follow-ups and retain cost/final settlement. New31 cases pass; focused113/113 and Core2770/2770 plus existing publication53/53 pass. Independent review is clean. TH3's dedicated preview/AI, atomic publication, operational asset and manual acceptance remains pending. Stop at TH2 review.

**TH3 automated acceptance (2026-10-16):** complete under the user's explicit request; see section5.33 and plan section11 (Git history: `2026-10-16-thorn-heart-reflection-plan.md#11-th3-acceptance--2026-10-16`). Only three test fixtures/metas and documentation were added. All85 added reflection tests pass. Full Core2774/2774 and Framework4275/4372 retain the exact97 TH0 failures, with no new/changed failure. Actual Thorn Heart SO/table effects now have direct compilation-through-attack acceptance; actual inventory grants one and rejects a second Place. Existing View ballista/health-preview compatibility fixtures pass. Independent review has no actionable finding. Manual Unity playback remains unchecked and is the remaining acceptance item. No product changes, staging, commit, push or external delivery.

Upstream acquisition note (not sent): standard InitialCount1 inventory currently supplies one artifact1002; the generic state constructor has no per-type uniqueness rule. Any future reward/shop acquisition must prevent granting a second1002 before inventory insertion. This is a separate acquisition requirement; combat duplicate-rule rejection remains in place and does not stack copies.

## 18. Cowboy Frenzy and fixed maximum action cost design request (2026-10-16)

The user requested a written design and implementation plans after confirming Cowboy's gameplay, Frenzy's percentage/stacking/expiry and kill-earned area attacks, and a universal character maximum action cost. The Cowboy and Frenzy design (Git history: `2026-10-16-cowboy-frenzy-design.md`) section 2 is the policy source. Interaction decisions are settled; section 9 lists final operational content inputs only. Review the separate fixed maximum plan (Git history: `2026-10-16-fixed-character-action-cost-plan.md`) and Cowboy/Frenzy plan (Git history: `2026-10-16-cowboy-frenzy-plan.md`). No implementation is included.

Core request: keep Frenzy independent from the existing random-target KillChainAttack and Intimidation rules, reuse common attack/schedule/publication boundaries, preserve per-hit multiplicity, and model next-turn expiry in both real turn execution and timeline forecast. Treat the universal maximum migration as a separate shared invariant with its own affected construction/restore paths and regression scope. The design records concrete SSOT, SOLID, Framework/FW-contract and modification/understanding/verification-scope checks; do not replace those with a file-count target.

The user's latest movement decision is an existing `MoveRange = 99` setting, not a separate action-cost-only movement mode. Cowboy remains Rider with move cost2 and existing refund rules. No movement-policy flag, special validator or separate movement Task is authorized by these plans. Delay10 follows the inspected Akimbo developer definition; an operational Akimbo SO was not found. Each implementation Phase stops for review.

**AC0 completed (2026-10-16, review stop):** the user authorized inventory and baseline only. Maximum plan section6 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#6-ac0-execution--2026-10-16`) records32 product candidates (including the positional maximum4 in TemporaryCoreDatabase), eight current Unit SOs (unregistered Stage1Boss included), and97 test safety-check paths. No external character-save format was found; persistent main state is in-memory. Fresh approved native Unity6000.0.73f1 EditMode: Core2774/2774 pass; Framework4275 pass/97 fail/4372 total, combined7049 pass/97 fail/7146 total, no skips or inconclusive results, exit2, no observed C# compiler errors/warnings. Exact failures and raw XML/log are linked in the plan; this is a failing pre-implementation baseline. Restricted startup produced no verdict; approved AppData access reached real tests without Unity/cache/package changes. All7570 tracked product/config file bytes and index entries were preserved after restoring only two verified Unity-generated ProjectSettings rewrites. AC1/AC2 are unstarted; no product implementation, staging, commit, push or external delivery.

**AC1 completed (2026-10-16, review stop):** the user's subsequent explicit AC1 instruction authorized the planned Core implementation. See section5.34 and maximum plan section7 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#7-ac1-execution--2026-10-16`). The canonical maximum, admission validation, SO/source/asset/fixture migration and native regression are complete. All53 new cases pass; latest combined full-run/scoped-rerun results retain exactly97 baseline failures with no new/changed failure. AC2 and Cowboy implementation remain unstarted.

**AC2 logic acceptance completed (2026-10-16, review stop):** see section5.35 and maximum plan section8 (Git history: `2026-10-16-fixed-character-action-cost-plan.md#8-ac2-execution--2026-10-16`). Exactly29 new cases pass in actual Unity; no production changes or broad-suite rerun. The user's focused-verification policy is recorded for AC2 through CF2. Manual playback is deferred, and Cowboy tasks remain unstarted.

**CF0 inventory completed (2026-10-16, review stop):** the user's explicit CF0 instruction authorized source/key/content inventory only. Cowboy plan section11 (Git history: `2026-10-16-cowboy-frenzy-plan.md#11-cf0-inventory-and-evidence--2026-10-16`) records the exact CF1/CF2 file inventory, direct-hit HP-flush/indirect-death/reflection boundaries, one arrival projection, schedule/turn-start/forecast/publication owners and existing tests at those seams. Reservations are `TraitKey.Frenzy=120` (CF1), `EntityKey.Unit_Cowboy=19` (CF7), `CombatEventKind.FrenzyWave=30` (CF4); no enum was edited. Actual registration is in TraitRegistry and initial status export in TraitEffectProvider, while generic TraitRegistration requires no mechanical edit. CF7 must replace the specific undefined-entity19 rejection fixture when that value becomes defined.

Registered targeting method1 already matches shape5/single1/unrestricted0/splash0/percent0, GUID `370b1888064844bc8421c2bf81b89916`; reuse it with unit-level RequireClear/FromImpact wall policy. No new targeting asset/key is needed. Existing section9 operational owner/role/footprint/visual/acquisition inputs still gate CF7 only. CF5 must derive post-expiry Priority through a public Application boundary because Systems cannot call internal Domain calculators; the future implementation brief must settle that narrow helper signature.

Per the user-directed verification scope, CF0 launched no Unity/test process. It reused283 recorded passing boundary cases from AC1 plus AC2's prior29 resource/publication cases, explicitly as historical evidence. All7,676 captured existing product/config files, prior working-tree changes, index and HEAD were preserved. Only documentation and local evidence changed; SSOT/SOLID/FW/state ownership and gameplay were not modified. CF0 stops at CF-P0 review; CF1–CF7 remain unstarted. No staging, commit, push or external delivery.

**CF1–CF2 completed (2026-10-16, CF-P1 review stop):** the user explicitly authorized both Tasks. See section5.36 and Cowboy plan section12 (Git history: `2026-10-16-cowboy-frenzy-plan.md#12-cf1cf2-execution--2026-10-16`). Immutable Frenzy contracts/calculation, independent combined debuff preservation, trait registration/codec/status capture and SO/default5/shared conversion are implemented. Native focused new45 plus directly affected12 cases all pass; no full-suite or manual playback rerun. Independent review has no actionable finding. CF3–CF7 and operational Cowboy activation remain unstarted. No staging, commit, push or external delivery.

**CF3–CF6 completed (2026-10-16, CF-P3 review stop):** see section5.37 and Cowboy plan section13 (Git history: `2026-10-16-cowboy-frenzy-plan.md#13-cf3cf6-execution--2026-10-16`). Direct-hit accumulation/kill provenance, free iterative waves, next-own-turn expiry and matching forecast are implemented. Preview/AI, atomic Buffer publication and existing View marker/health playback acceptance have focused native evidence: latest21/21 unique cases pass. The latest user instruction replaced broad/RED verification for this execution. One review finding was fixed and verified; no outstanding actionable finding. CF7 production content/manual gameplay remains pending.

This entry records local design and explicitly authorized implementation/acceptance, not external delivery. The original design-only stage did not modify product code or run tests; later AC0–AC2 and CF0–CF6 execution is recorded above. No staging, commit or push occurred.

## 20. Dynamite Hammer identity request (2026-10-16)

**Status:** Local Core-owned execution record; not sent. The user explicitly authorized DH1 and DH2. DH0–DH2 system scope is complete: identity24, authoring5/5, and combat/publication22/22 plus one wall regression pass. No actionable independent review finding. Stopped at DH-P2; production content is deferred. See §5.39 and plan §§10–11.

The implementation plan (Git history: `2026-10-16-dynamite-hammer-plan.md`) now scopes DH0 to inventory/baseline, DH1 to the missing Core identity and temporary-authoring conversion acceptance, and DH2 to reused combat/publication behavior. Production Unit/Visual registration and availability are deferred. The user's correction assigns Core work to this session, subject to the existing Phase review boundaries.

The Dynamite Hammer design (Git history: `2026-10-16-dynamite-hammer-design.md`) proposes a player-only regular 1×1 Rider using existing Knockback with distance1. It reuses the existing Single / EightDirectionLine / FrontAndSides targeting definition and damage-before-displacement behavior, including diagonal pushes. No new trait, damage, movement, targeting, or Buffer rule is requested.

**DH0 allocation finding:** current enum values end at structure22; Shielder23 is reserved in its plan and View handoff. Reserve `EntityKey.Unit_DynamiteHammer = 24` for DH1; the inspected code/docs/tests contain no competing entity24 allocation or literal undefined-entity24 rejection case. This is a documentation reservation, not an enum edit. Recheck immediately before DH1 and preserve every existing value. No external identity response is needed.

**DH0 complete, DH-P0 review stop:** plan §9 (Git history: `2026-10-16-dynamite-hammer-plan.md#9-dh0-execution--2026-10-16-dh-p0-review-stop`) records fresh native Unity baseline162/162 passing across eleven existing system fixtures, exit0, no compiler errors and three existing-source warnings. All7,716 captured product/config file bytes and index entries are preserved after restoring the single verified Unity-generated FMOD scan-path change. No Hammer enum, asset, test, or runtime implementation was added. DH1/DH2 remain unstarted; production View/content is deferred by the user.

**DH1 complete, DH-P1 review stop:** `Unit_DynamiteHammer = 24` is implemented; the above DH0 reservation is now historical. Plan §10 (Git history: `2026-10-16-dynamite-hammer-plan.md#10-dh1-execution--2026-10-16-dh-p1-review-stop`) records compilable identity RED followed by native authoring GREEN5/5. Only the enum and focused test/metafile were added to product/test sources; production content and existing values remain unchanged. DH2 is next and has not started.

**DH2 complete, DH-P2 review stop:** plan §11 (Git history: `2026-10-16-dynamite-hammer-plan.md#11-dh2-execution--2026-10-16-dh-p2-review-stop`) records final native23/23 passing cases (22 new Hammer combat/publication +1 existing actual wall-occlusion regression), exit0, no new compiler warning/error, and independent Astra review with no actionable issue. Added only `DynamiteHammerCombatTests.cs`/meta. Preview footprint is pre-hit, death clears field position, wall-cell rejection is distinct from actual occlusion, and an unaffordable second attack is distinct from a second eligible paid refund; evidence limits are explicit. No further production Core change was needed; the DH1 enum remains the only changed pre-existing product file. System-only delivery is complete; production Unit/Visual content and availability remain deferred, with no staging/commit/push/external delivery.

**DH3 handoff, not implementation:** The user explicitly stopped View/content work in this Core session and requested the [Dynamite Hammer View handoff](2026-10-16-dynamite-hammer-view-handoff.md). It records the exact Unit/Visual registration brief, confirmed settings, existing published contracts, missing artwork/route choices, and required recipient validation. No production asset, loadout, scene, or View implementation was added; no external message was sent. DH0–DH2 system acceptance remains complete.

**DU1 saved gameplay SO, subsequent explicit user correction:** Created `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_DynamiteHammer.asset`/meta (GUID `b8b7fa1d0a184ded941b09423dc9ec24`) with confirmed values, shared method7 and existing Knockback distance1. This supersedes the earlier exclusion of all production authoring. Plan §13 (Git history: `2026-10-16-dynamite-hammer-plan.md#13-du1-saved-production-unit-so--2026-10-16`) records native saved/temporary authoring6/6 and angle assets9/9 passing, mandatory common DB30/33 with three unchanged-stage/Line-setting failures, and exact evidence. The delivered SO is not yet table-registered because valid View data must be activated with it. The revised [View-only connection handoff](2026-10-16-dynamite-hammer-view-handoff.md) covers only resources/UnitVisual, paired registration using this existing SO, selected availability route and manual View acceptance. No runtime Core logic, loadout, or View resource was changed; no commit/push/external delivery.

Core source inspection found compatible contracts in `UnitSpecAdapter`, `AttackKnockbackCalculator`, `ForcedDisplacementGeometry`, and `TargetingAngleCalculator`; source inspection alone is not runtime verification. The plan's execution record carries fresh baseline evidence. If implementation finds an unsupported boundary, report it before introducing special-case Hammer logic. Presentation resources and production acquisition remain deferred content decisions, not current blockers. No commit, push, or external delivery occurred.

## 21. 토네이도 최초 무작위 생성 정합 요청 (2026-10-17 요청, 2026-10-18 Core 구현)

사용자가 [기획 정합 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md#11-옛-지형-토네이도-10-1710-31-대신함)를 승인하고 **이번 세션의 Core 직접 수정은 하지 말고 Core 작업자 수정안을 준비**하도록 지시했다. G0 수정안 작성·G1 기존 표시 검증 뒤, G1 결과를 반영해 토네이도 최초 무작위 생성 Core 전달안을 최종 정리했다. 상세 API 제안·seed 수명·인수 체크리스트는 그 문서에 있었다. Core 구현 뒤 2026-10-19 사용자 결정으로 그 전달안과 Core 작업자 전달 프롬프트를 삭제했다(내용은 커밋 `4a6be1ba`까지의 이력). 아래 G0~G1 기록은 작성 당시 상태이며, 지금 상태는 이 절 끝의 "Core 구현 반영"을 따른다.

- 현재 원문: `C:/Users/Hyeon2-400/Dropbox/Project L/기획 브레인스토밍/안재령의고뇌/Terrain, Obstacle, Object 기획서.pptx`, 24장, SHA-256 `a89e2d2f9e2d1a0f06fae16de98d4d66deb05c860bae0d20a75d8d80fd65d1c2`. 슬라이드 5는 공통 Terrain, 13은 토네이도다. 이전 20장 원본의 해시를 현재 원문 증거로 재사용하지 않는다.
- **누락:** 슬라이드 13은 최초 생성도 랜덤이라고 한다. 현재 Normal A는 source213/(4,3) 고정이며 `StageEnvironmentAdapter`와 준비 계산이 그 셀을 그대로 게시한다. 반복 발동 뒤 무작위 재배치는 기존 `TornadoCalculator`가 수행하므로 다시 구현할 필요가 없다.
- **요청:** 전투 준비 트랜잭션에서 최초 토네이도 셀을 결정해 기존 source ID·시간값·공개·Terrain 분류와 함께 원자적으로 게시할 계약을 검토한다. 초기 배치 입력의 고정/무작위 구분·기존 호출자 기본값·Core 난수 상태 전달·실패 원자성은 Core 담당이 확정한다. View의 난수·타이머·Buffer 덮어쓰기는 사용하지 않는다.
- **승인된 설계 방향·Core 구현 계약 인수 전:** 최초 후보에서 생존 구조물 전체 점유 칸·다른 활성 Terrain 제외, Character/Object 허용, 이전 위치가 없는 최초 선택에서 authored 셀을 불필요하게 제외하지 않음, 후보 0개는 준비 실패로 보고하며 임의 고정 fallback 없음. PPT 자체가 이 후보/실패 정책을 정하지는 않았다. 여러 원본은 Source ID 순서와 기존 결정적 후보·난수 계약을 따른다.
- **유지:** 3회 완료 개별 유닛 턴·밤 경계·SO 시간 설정·명시적 강제 이동 면역·도착 효과·현재 반복 후보/전투 종료 처리. 유닛 강제 이동 목적지에서 Character/Obstacle 제외는 이미 지원한다. 기존 면역 없는 다중 셀 제한도 임의 확장하지 않는다.
- **인수:** 같은 seed·입력 재현, 후보 제외·하나/없음·중복 방지, 준비 실패 부분 게시 없음, Normal A 실제 준비 Buffer 및 공개 입력 3·6회 발동 경로. 기존 고정 배치 fixture를 단순 완화하지 않고 새 초기화 계약과 분리해 검증한다.
- **View 범위:** 기존 Toneido wrapper·재질3개·Terrain 표시기·결과 소비 유지. 다른 10개 스테이지·Framework·UI·FieldCamera에 새 로직을 추가하지 않는다. 전용 빨려들기/임시 사망 연출은 이번 PPT의 명시 요구가 아니다.

G0에서 Core·DB·Buffer·View 제품 파일과 테스트를 수정하거나 Unity를 실행하지 않았다. 사용자 FMOD 변경도 보존했다. 제품 보존·문서 검증·정지 위치는 위 Task 계획 실행 기록을 따른다. 외부 메시지는 발송하지 않았다.

**후속 G1 완료(2026-10-17):** [Task 계획 G1 기록](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#g1-실행-기록--2026-10-17)에 기존 Terrain 표시 EditMode14/14 통과를 기록했다. 최초 랜덤 Core 계약 인수와는 별개다. 테스트 종료 때 원본/전투용6재질의 Queue300x→200x 및 CRLF→LF 저장을 재현했고, 실행 전 byte로 복원해 보호5,912파일 일치를 확인했다. 근본 원인·실제 Camera/자동 Play·초기 랜덤은 미인수이며 G2를 시작하지 않았다. Core 수정·외부 전달·커밋·push 없이 보고 후 멈춘다.

**G1 반영 전달안 최종 정리(2026-10-17):** 좌표 없는 초기 명세, 정의 조회 뒤 nullable seed 주입 사본, Prepare 시 seed 필수 확인, 0 seed 허용, 명시 준비 progress와 직접 주입 CommandState의 불일치 거절을 제안 API 계약에 명시했다. 준비 뒤 StageMismatch와 취소/대상/Run 변경까지 seed 수명을 검증하도록 했다. G1의14통과와 Queue 잔여는 별도 근거로 기록했으며 신규 랜덤 검사가 실행된 것으로 설명하지 않는다. 이번 최종 정리는 문서 변경뿐이다.

**Core 구현 반영(2026-10-18):** 위 "Core 구현은 아직 없다"는 더 이상 맞지 않는다. Core가 구현 계획 (Git history: `2026-10-18-tornado-initial-random-placement-core-plan.md`) P1·P2를 구현했다(커밋 `f38de8d4`). seed는 스테이지 `timelineSeed`에서 만들고(D1, 사용자 승인) 별도 seed 필드·pending seed는 없다. View 쪽 대조 결과는 [Task 계획 G2의 "사용할 Core 계약"](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#사용할-core-계약대조-완료)(차이 없음), Normal A 활성화는 [Task 계획 G2](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#phase-g2--외부-core-계약-인수와-normal-a-입력-연결)(2026-10-18 실행 뒤 2026-10-19 되돌렸다가 같은 날 재실행 완료, Play 확인은 G3)이다. 지금 Normal A의 토네이도 213은 전투 준비가 정한다(`timelineSeed` 1로 (0,6)). 참고(2026-10-19 G2 재실행): 천명님 작성 `EnvironmentEntryPublicationTests` 3건(무작위 토네이도 DB 경로 시험 포함)과 `StageEnvironmentAdapterTests` 8건이 "G7: 클리어 보상 정의가 없습니다."로 실패한다. `3646ba16`(클리어·처치 보상)이 클리어 보상을 필수로 바꿨는데 이 시험들의 fixture DB에 클리어 보상이 없다. View 쪽은 고치지 않았다. Core 측 회신(실제 서명·실행 증거)은 Core 계획 P3.2가 이 절에 더할 예정이다.

## 22. 경험치·레벨업 Core 계약 알림 (2026-10-18, Core 작업자, 미전달)

설계 경험치·레벨업 (Git history: `2026-10-18-experience-level-up-design.md`), 실행 기록은 계획 §5 (Git history: `2026-10-18-experience-level-up-plan.md`). View 코드는 바꾸지 않았다. View가 따로 할 일은 아래 사건 등록(경고 제거)뿐이고 표시는 선택이다.

| 바뀐 것 | View 영향 |
|---|---|
| 새 사건 2종 `CombatEventKind.ExperienceEarned`(34)·`ExperienceApplied`(35), 형식 `CombatExperienceEvent` | 표시 계약이 없어 순차 연출이 경고를 남기고 건너뛴다(재생 끝에 최종 Buffer로 다시 그림). 경고를 없애려면 "사실만 반영"으로 등록한다. `ExperienceEarned`: 처치된 적(`VictimId`)·받은 아군(`RecipientId`)·양(`Amount`). `ExperienceApplied`: 승리 때 보유 기록 변경(`Owned` Expected/Final 레벨·진행값)·오른 레벨 수·메인 랜덤 성장(`MainGrowth`). 사건 순서는 그 계산 단계의 사망·정리 사건 뒤에 붙는다 |
| `CharacterCombatFacts.Experience`(`CharacterExperienceFacts`) | 적은 `EnemyLevel`(스테이지 적 레벨, 소환체 1), 아군은 `Earned`(이번 전투에서 번 경험치). `CharacterData.ToFacts().Experience`로 읽을 수 있다. 적 레벨 표시·전투 중 획득 표시가 필요하면 여기서 읽는다 |
| `OwnedCharacterRecord.Experience` | 현재 레벨 안의 진행 경험치. 승리 때만 바뀐다. 레벨 표시(`UnitCardUI`)는 기록 참조 변경으로 자동 갱신된다. 경험치 막대를 그리려면 다음 레벨 요구량이 필요한데, 이는 Core 규칙(`ExperienceRules`)이라 View가 계산하지 않는다. 필요하면 보유 능력치 조회에 요구량을 더하도록 Core에 요청한다(P6) |
| `MainCharacterPersistentState.PersistentDefense` | 메인 방어력이 랜덤 성장으로 오를 수 있다. 메인 전투 사실의 `Stats.PhysicalDefense`가 이 값과 같다. 상세 표시(`UnitCardView.ShowDetails`)가 원시 `PhysicalDefense`를 보이는 기존 동작은 그대로다 |
| Damage pipeline defense | Superseded by the user correction in §5.45: defense reduces damage by 1% per point, authored base defense is 5, and positive attack damage has a final minimum of 1 after Heavy/terrain. View consumes published results. |

실제 DB로 처치하는 사건 순서 시험(View 시험 포함)은 새 사건이 끼어 기대값이 바뀔 수 있다. 이번 전체 실행(2026-10-18)에서는 그런 실패가 없었다. 개발 진입 "Fixture" 정의는 경험치 설정이 없어 경험치가 꺼져 있고, Database·보스 스테이지 정의에서만 켜진다.


## 23. Combat rewards delivery (2026-10-18, local handoff)

The user authorized Core combat-rewards R0-R5. Current contracts and View work are in the [reward View handoff](2026-10-18-combat-rewards-view-handoff.md); Buffer registration/fake-query changes are in the [Framework share](2026-10-18-combat-rewards-framework-share.md). Exact test scope and remaining failures are in plan §15 (Git history: `2026-10-18-combat-rewards-plan.md#15-execution-record--r5-2026-10-18`).

Completion.Rewards supplies gross earned gold, completed-node Depth/WidthIndex, the fixed initial deployed cohort and actual XP/level before/after thresholds for the clear screen. Existing CurrencyReadSink remains the current Run-balance source; FameReadSink is cumulative within the runtime. The current initial hand default is main + up to three subs (total 4), with manual main placement. The new receipt supplies clear-screen XP thresholds without exposing Core's internal ExperienceRules to View; §22's general owned-card threshold follow-up remains separate.

Developer Fixture reward configuration was explicitly excluded by the user; use Database mode and carry the Fixture edit as a View follow-up. Heavy live spawn/resources, critical hits and disk persistence remain separate. No View/Plan/Job implementation or external message is included in this local delivery.


## 24. Combat 종료 화면 Run 누적 통계 요청 (2026-10-19, View → Core, 미전달)

사용자 결정: 승리/패배/포기 화면과 EXP 연출은 View에서 구현하고, 아래 두 값만 필요한 Core/Buffer 계약으로 남긴다. 중간 레벨 요구 경험치는 요청하지 않는다(레벨 전후 차이와 최종 비율만 표시).

| 필요한 사실 | UI | 요구 의미 |
|---|---|---|
| 현재 Run에서 수집한 총 명성 | CombatEndCanvas/CompletionPanel_Defeat/TotalReputation/ReputationTxt | 현재 Run 전체의 획득 합계. 현재 런타임 전역 FameReadSink.Amount(이전 Run 포함)와 다름 |
| 현재 Run에서 소모한 총 골드 | CombatEndCanvas/CompletionPanel_Defeat/UsedGold/GoldTxt | 현재 Run 전체의 실제 소비 합계. 전투 비용뿐 아니라 상점/회복 등 모든 골드 소비 경로를 포함. 현재 CurrencyReadSink.Amount나 시작 잔액 차이와 다름 |

Core에서 승인된 실제 정산으로 한 번만 집계하고 Buffer 읽기 계약으로 제공해 주세요. 취소/거절/중복 제출이 소비 합계를 늘려서는 안 됩니다. 환급·정정이 있는 경우의 통계 의미는 Core 계약에 명시해 주세요. 새 Run에서 초기화하되, 패배/포기 완료 화면이 읽고 확인하기 전의 종료 Run 합계는 유지되어야 합니다(영수증 스냅숏 또는 기존 종료 수명에 맞는 읽기 계약). View가 잔액 변화를 감시해 자체 장부를 만들거나 Core/Buffer에 쓰지는 않습니다.

현재 구현은 두 텍스트를 빈칸으로 표시합니다. 0/현재 잔액을 합계처럼 대입하지 않습니다. 종료 좌표는 기존 Current/StageProgress 읽기 계약을 사용하고, 승리 골드·명성/유닛 EXP는 기존 Completion.Rewards로 처리합니다. Core 로직 수정·외부 발송 없음. 설계/실행 기록: [Combat 종료 UI 계획](../plans/2026-10-19-combat-end-ui-plan.md).


## 25. UnitInfo 현재 공격 비용 조회 요청 (2026-10-19, View → Core, 미전달)

사용자는 전투 중 변동된 능력치가 UnitInfo에 반영되도록 수정하도록 지시했다. 공격력·우선도는 기존 공개 CombatEffectiveStatsCalculator를 CharacterData 생성 경계에서 재사용해 EffectiveAttackPower·EffectivePriority로 제공하고, 전투 UnitInfo/유닛 카드가 읽도록 View Phase 1을 적용했다. 기존 EffectivePhysicalDefense와 같은 경계이며 Core 파일은 수정하지 않았다.

남은 값은 특성·효과·이동 상태를 반영한 현재 기본 공격 비용이다. 현재 UnitInfo는 Stats.AttackCost를 표시한다. CombatAttackQueries/CombatAttackCalculator는 CombatEffectEngine과 공격 프로필/특성 바인딩으로 실제 비용을 평가하지만, AttackRangeResult에는 CanAffordAttack만 있고 비용 금액은 없다. AttackPreview.AttackCost는 특정 공격 요청에 대한 값이며 임의 선택 유닛 정보의 상시 조회값이 아니다.

요청: Core에서 실제 공격과 같은 평가 규칙을 재사용하는 읽기 계약/Buffer 공개값을 제공해 주세요. 활성 행위자뿐 아니라 정보창에서 선택한 다른 아군·적의 조회 가능 의미도 정의해 주세요. 공격 기능이 없는 유닛이나 계산 조건이 성립하지 않는 경우는 기본 비용/0을 최종 비용처럼 반환하지 않고 조회 불가를 명시해 주세요. 현재 위치·이동 상태에서 실제 적용되는 할인과 다음 턴/상태 변화에 따른 해제를 포함하고, 현재 AP와 비교한 가능 여부와 비용 금액의 의미를 분리해야 합니다.

검증 요청: 이동 후 공격 할인 전/후·소모 뒤·턴 변경, 재화/영혼 등 현재 계약의 비용 효과, 비활성 선택 유닛/적/공격 불가 유닛의 조회 의미, 실제 실행/범위/상세 미리보기 비용과의 일치. View는 Core Domain 엔진을 직접 호출하거나 할인식을 복제하지 않으며, 정보창 갱신을 위해 공격 조준이 소유한 미리보기 요청 슬롯을 덮어쓰지 않습니다.

이번 Phase에서는 공격 비용과 MAX COST의 기존 의미를 유지했습니다. MAX COST는 최대 행동 자원이며 현재 잔여 ActionCost가 아닙니다. 후속 인수 전까지 공격 비용 최종값 반영을 완료로 기록하지 않습니다. 코드·검증 기록: Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatUnitInfo.md.
## 26. 강화 후보 소진 시 마지막 구매 표시용 읽기 계약 요청 (2026-10-19, View → Core, 미전달)

사용자는 1레벨 강화 UI 설계에서 **실제 구매 후보가 없으면 마지막으로 구매한 강화를 구매완료로 표시**하도록 결정했다. 현재 Run의 마지막 성공한 강화 구매 한 개를 비구매 카드로 보여 주고, 기존 Complete 입력으로 구매 없이 현재 방문을 마친다. 반복 구매·레벨 강화·추가 과금 요청이 아니다. 제품 정책 원본은 [강화 View 연결 설계 §4.4](../specs/2026-10-19-enhancement-view-integration-design-Hojin.md#44-후보가-없을-때-마지막-구매-표시)다.

**현재 부족한 공개 사실:** OwnedCharacterRecord.Enhancement에는 선택만 있고 구매 순서가 없다. CurrentProgressBuffer의 TryGetShopResult는 최근 상점 명령 한 건이라 일반 상점 구매·Complete·거절 결과로 덮어써지며, 새 Run에서도 보존된다. 현재 방문의 Offers도 다음 방문의 마지막 구매 이력을 보장하지 않는다. 이 값들만으로 현재 Run의 마지막 성공한 강화 구매와 당시 표시 내용을 안정적으로 재구성할 수 없다.

| 요청할 읽기 사실 | 필요한 의미 |
|---|---|
| 현재 Run의 마지막 성공 강화 구매 유무 | 실제 거래 반영 순서상 마지막 한 건. 없으면 명시적으로 없음. 보유 목록 순서·UI 수신 순서로 판단하지 않음 |
| 대상과 강화 식별 | TargetOwnedId, CharacterKey, EnhancementKey와 별도 강화 Level. 당시 구매 개체를 다른 같은 종류 개체로 바꾸지 않음 |
| 불변 표시값 | 당시 강화명과 표시용 내용·구매 가격 등 카드에 필요한 고정 상품 사실. View가 InGame DB를 조회하거나 강화 키별 문자열·가격 표를 만들지 않게 함 |

강화 구매가 실제 반영될 때 재화·강화 선택·방문 구매 잠금과 함께 이 사실의 정합성을 유지해 주세요. 구매 성공 뒤 노드 완료가 실패해도 마지막 성공 구매는 유지하며, 구매 준비·검증 실패나 중복·거절은 이력을 바꾸지 않아야 합니다. 일반 상점 구매, 미선택 Complete, 화면 닫기, 씬·챕터 이동도 마지막 강화 구매를 덮어쓰지 않습니다. 새 Run이 정상 설치되면 이전 Run 이력을 사용하지 않고, 새 Run 준비 실패 시에는 기존 Run의 기록을 보존해야 합니다.

공개 Sink·DTO·저장 위치·게시 방식은 기존 Buffer/트랜잭션 소유권에 맞게 Core 담당이 확정합니다. 전체 구매 이력 서비스나 디스크 저장은 요청하지 않습니다. 기존 실제 구매 후보 Offers·현재 방문 HasPurchase/PurchasedMask·완료 판정은 변경하지 않고, 읽기 사실만 현재 Run의 수명으로 제공해 주세요. View는 이력 카드를 현재 구매 후보와 구분하고 제출 가능한 OfferId를 붙이지 않습니다.

인수 사례: 강화 A 구매 → 일반 상점 구매/미선택/거절 → 빈 강화 방문에서 A 표시; 이후 강화 B 구매 → B 표시; 거래 실패 시 이전 이력 유지; 구매 성공·완료 실패 시 성공 이력 유지; 새 Run 정상 설치/준비 실패의 구분; 구매 이력이 없는 빈 방문. View가 최근 결과를 캐시해 별도 구매 장부를 만들거나 Core·Buffer 상태를 직접 쓰는 우회는 사용하지 않습니다.

이번에는 설계와 요청만 기록했습니다. Core·Buffer 제품 코드 수정, Unity 실행, 외부 메시지 발송은 하지 않았습니다.

## 27. StageSelect의 여정 포기용 Run 종료 API 요청 (2026-10-19, View → Core, 미전달)

사용자 정책: Option의 여정 포기를 확인하면 Combat에서는 기존 전투 포기 및 결과창 확인을 거쳐 Run을 종료하고 Lobby로 복귀한다. StageSelect에서는 전투 포기를 호출하지 않고 현재 Run을 종료한 뒤 Lobby로 복귀한다. 사용자는 이번 작업에서 Core를 수정하지 않고 요청 문서를 남기며 API 연결을 기다리도록 명시했다.

현재 공개 계약은 RunStart.StartNewRun, ChapterProgression의 진행 조회·챕터/노드 처리, BattleCompletion.AbandonBattle이다. 전투가 없는 StageSelect에서 현재 Run을 자발적으로 종료하는 공개 API는 확인되지 않았다. StartNewRun 호출은 새 Run 생성이므로 종료 성공의 대체 증거로 사용하지 않는다. View의 Buffer 직접 쓰기, 가짜 전투 생성, 패배/노드 결과 조작으로 우회하지 않는다.

요청: 전투가 없는 진행 중 Run의 자발적 종료를 Core 소유 경계에서 제공해 주세요. 공개 서비스/결과 DTO 명칭은 Core에서 결정하되 수락·거절과 정상 중복 요청의 의미, 종료 Outcome/Completion, 로스터·재화·방문/미결 명령의 정리 및 보존 범위를 명시해 주세요. Run 결과와 Completion 사실은 Core/Buffer 한 곳이 소유하고, View는 공개 완료 사실을 확인한 뒤 LifeCycle 경로로 Lobby 전환만 요청합니다. 활성 전투·종료된 Run·대기 중 방문/진입·중복 요청·처리 실패에 대한 계약과 검증 사례가 필요합니다. 실패/거절이면 성공처럼 Lobby로 이동하지 않습니다.

새 Run 준비 시점은 이 API의 종료와 별도 책임입니다. 기존 로비 출발의 Run 생성 정책을 유지하며, 전투 포기의 기존 새 Run 준비를 이번에 함께 제거할지는 후속 설계에서 명시합니다. 요청 문서를 작성한 것이 API 구현·연결 또는 외부 전달 완료를 뜻하지 않습니다.

## 28. Run deck and Main category design request (2026-10-19, local draft)

The user requested design first and then an implementation plan. The Run deck and Main category design (Git history: `2026-10-19-run-deck-main-category-design.md`) and Phase Task plan (Git history: `2026-10-19-run-deck-main-category-plan.md`) record confirmed rules, proposed policies, source evidence, Core/View ownership, transactions, content dependencies, phase boundaries and acceptance cases. They remain planning drafts; implementation is unstarted. No message has been sent externally.

Confirmed scope: rename Base category to Main while retaining numeric identity 10 and existing category balance; retire the dedicated Main from playable composition. Akimbo is the only currently approved production Main. Future Mains are planned; retain the proposed per-ID persistence and switching foundation now, with at most one owned copy of each eligible definition and only one selected Main. Verify multiple Mains with explicit test-only definitions. Sniper's role/category/release/acquisition are deferred; neither MainCharacter nor Cannon conversion is approved. Demon also changes category name but remains an enemy Regular unit. Cap Cannon/Rider/Heavy ownership at two per category across all definitions. The committed deck selects owned instance IDs, contains exactly one Main, and has at most four cards. New Run grants and selects Akimbo, Cactus Cannon and Shotgun, one each; remove the currently authored starting Sniper grant. The current derived ownership ceiling is seven, not a separate total cap; future eligible Main definitions may increase it.

Core delivery requested for the future implementation phase: ID-addressed Main persistence and queries; centralized acquisition limits; instance-based deck read/evaluate/confirm contracts; atomic initial roster/deck installation; selected-only battle preparation with frozen membership/Main identity; explicit-target settlement/recovery/shop handling; stale-Run/roster/deck rejection. View must consume those contracts instead of writing Buffer state or calculating rules. The old single-owned-Main assertions and all-owned draw path are incompatible with this design and must be audited across their consumers, including existing XP/enhancement/reward paths.

Current production dependencies remain explicit: the inspected operating Unit assets have no Akimbo key-10 definition; approved Unit/Visual data, registration and all owner integrations are required. Audit all existing Sniper references, remove its active registrations/routes coherently and retain dormant assets/GUIDs without deciding its new role or category. A second production Main or Sniper acquisition route is not required for current acceptance. Keep multi-Main fixture evidence separate from Akimbo-only operating acceptance and later content release acceptance. Do not invent content or infer readiness from a fixture. This entry changes documentation only and does not grant permission to implement Core, change assets, stage, commit or push.

### 28.1 Authorized Phase A delivery (2026-10-19, local; not sent)

This updates the preceding planning-only status for A1/A2 only. P0 and then Phase A were explicitly authorized by the user. Phase A delivery (Git history: `2026-10-19-run-deck-main-category-phase-a.md`) and verification evidence (Git history: `2026-10-19-run-deck-main-category-phase-a-results.json`) are the current delivery record; B–E remain unstarted.

Delivered: CategoryKey.Main=10; CategoryConfigData.MainLevelGrowth / mainLevelGrowth with FormerlySerializedAs("baseLevelGrowth"); one unconnected pure Contracts PlayerMainDefinitionPolicy.Validate(key, role, category, eligibility). All category numbers, the 25 matchups and operating data remain unchanged. The legacy serialized growth 7/3 test passes. Existing UnitData/UnitDefinition/CharacterDefinitionAdapter operating validators remain unchanged; their coordinated connection and range-policy migration belong to D1.

Owner notice: TemporaryCoreDatabase.cs, ViewSystem/Dev/DevBattleInitializationSource.cs and ViewSystem/Combat/CombatEventAudio.cs have mechanical Base-to-Main category references only. Audio category/10 and entity preset indices are retained. No View input, scene/UI registration, Plan/Job, flow, Buffer or operating Unit/Visual/loadout data was changed. This is local documentation, not an external message or owner acceptance.

Verification: complete Core 3477 passed; focused Framework 192 passed / 4 failed (196 total), all four exactly P0-identical by full name/message. Full Framework was not rerun. Akimbo content, real Stage authority, per-ID/deck/battle integration and registered editor delivery remain required. Multi-Main/range evidence is fixture/helper-level only; there is no replacement-Main activation or new operating deck feature in PA.

### 28.2 Authorized B1-only foundation (2026-10-19, isolated local worktree; not sent)

B1 delivery (Git history: `2026-10-19-run-deck-main-category-b1.md`) and evidence (Git history: `2026-10-19-run-deck-main-category-b1-results.json`) record the actual pure Contracts/Application foundation in `C:/CodexHome/worktrees/run-deck-b1/Project_L`. No product changes were copied back to the primary checkout. Canonical ID/initial-grant selection, ownership caps and immutable tuple/read/evaluation/battle-membership DTOs are verified with 41 focused tests; full Core 3518 passes. No runtime deck service or read/write sink is delivered yet. CharacterDefinitionFacts gains explicit Category; legacy callers retain None and new rules reject it until the later adapter migration. This is not permission for View to invent categories or compose independent version reads.

B2 stores/Main-state migration and subsequent Core/flow/View/content wiring remain open. No new Main grant, loadout, selected-only battle behavior or operating editor is activated. PB has not passed. Fixture-only multi-Main support is not production content approval, and Framework/manual acceptance was not rerun. This entry is local documentation; no external message or owner acceptance is implied.


### 28.2 Authorized B2 storage delivery (2026-10-19, local; not sent)

User authorized B2 only. B1/B2 live in `C:/CodexHome/worktrees/run-deck-b1/Project_L`, not the primary checkout. B2 delivery (Git history: `2026-10-19-run-deck-main-category-b2.md`) and exact evidence (Git history: `2026-10-19-run-deck-main-category-b2-results.json`) specify actual signatures and pending owners. PB is fixture-ready; C–E remain unstarted.

Main persistence is now keyed: `TryGetMainState(CharacterInstanceId, out MainCharacterPersistentState)`, per-request `InitialMain`, and `MainCharacterPersistentChange(Id, Expected, Final)`. Roster revisions cover changed persistent batches; RunProgress has RunId; the composed Buffer query exposes RunDeckReadSink/RunDeckWriteSink. Core keeps rule calculation and identity generation, Buffer keeps checked preparation/publication. The initial canonical deck is still D1 work; missing selection must stay unavailable.

Owner notice: necessary mechanical changes touched `LobbyDepartureFlow`, `OwnedCharacterStatsReader`, `UnitCardUI`, and `MainCharactorUI`. UI refresh uses roster revision; Main display reads committed selection. No first-owned-Main fallback remains in product API reads. Recovery/Shop records capture explicit MainId; D2 frozen battle membership, C3 semantic integration, C1/C2 authority/editor service, and E presentation/content remain separate acceptance work. No scene, prefab, asset, input, FieldCamera, Framework or unrelated Architecture changes were made.

Verification: B2 29/29; complete Core 3518/3518; affected Framework 93 passed / 3 exact P0-identical failures. Full Framework/manual gameplay not rerun. One independent review's two Important publication findings were fixed with behavioral RED/GREEN. See the delivery report for the one deferred XML-comment minor and precise operating limitations. This local note was not sent externally.

### 28.3 Authorized C/D Core contracts and selected battle integration (2026-10-19, local; not sent)

The user authorized the remaining Phase D after D1 and clarified Core ownership: View/UI/Flow feature work is an owner handoff; tests and minor compatibility edits are allowed. Product changes remain in `C:/CodexHome/worktrees/run-deck-phase-c/Project_L`, uncommitted and unmerged. The primary checkout receives documentation mirrors, not the integrated C/D product source.

Use the [Core/Flow/View/content handoff](2026-10-19-run-deck-core-view-handoff.md) for actual signatures and registration, D1 delivery (Git history: `2026-10-19-run-deck-main-category-d1.md`) for acquisition/initialization, and Phase D delivery (Git history: `2026-10-19-run-deck-main-category-phase-d.md`) plus its machine-readable evidence for final verification and limits. This entry supersedes earlier future-tense statements about these delivered Core contracts; it does not change historical acceptance records.

Delivered: `CoreGroup.Deck.Read/Evaluate/Confirm` with immutable versioned requests/read results and typed unavailable/stale semantics; one shared ownership policy; atomic Run/roster/canonical selection initialization; captured Main IDs for noncombat interactions; and selected-only battle preparation/command assembly. `CombatStateReadSink.Current.Membership` is the frozen `RunDeckSnapshot`; `EntryRosterRevision` and the existing outer `BattleInstanceId` establish its source and lifetime. Exact owned-ID/Main-ID checks reject equal-count bench substitution. Installed membership requires an exact nonzero battle identity at command publication. Main persistence, souls, level and XP settle by captured owned ID; switching and cleanup do not transfer or reapply growth. Core calculates rules, Buffer validates/prepares/installs immutable facts, and View reads contracts/submits commands.

Core's TemporaryCore development fixture now initializes through RunStart with explicit eligible fixture definitions and selection; this is not operating balance/content approval. Normal entry requires captured membership; raw null-membership preparation remains a limited legacy storage-test seam, never a runtime fallback.

The D2/D3 hash audit compares 420 product ViewSystem/UISystem/LifeCycleSystem files to the saved D2 input and finds zero changes. Existing B2/C mechanical View changes are inherited input. UI/display test fixture adjustments are separate and explicitly allowed; no editor/popup/input feature, scene/prefab, real Flow authority, FieldCamera or unrelated Architecture implementation was added.

Remaining owner gates: C1 must provide/expose/bind the real `IStageInteractionRead` authority; default Core availability remains unavailable and the test authority is not operating delivery. E1/E3 must implement/register the deck editor, committed/frozen membership display and input/manual acceptance. E2 must supply approved Akimbo Unit/Visual content and coordinated Unit/Visual/roster/shop/loadout registrations, retaining every connected dependency and preserving dormant Sniper assets while removing inactive routes coherently. `DevBattleInitializationSource`, `CombatEventAudio` and `DeckCardList` remain explicit View cutover sites. No operating SO migration or external delivery is implied.

Verification and remaining full-suite failures are reported precisely in the Phase D delivery/evidence; full Core is 3522/3522. This local note does not imply repository-wide green, C1/PC completion, Phase E completion or operating acceptance. No staging, commit, push, merge, cache deletion or Unity upgrade was performed.

### 28.4 Primary integration and authorized Git delivery (2026-10-19)

The user subsequently authorized helper cleanup, commit and push. A–D Core sources/tests and their handoffs are now consolidated into the primary checkout; earlier worktree-only statements are historical. See the delivery record (Git history: `2026-10-19-run-deck-delivery.md`) for fresh verification, cleanup and retained local evidence. This does not complete C1 or Phase E, activate operating content or accept View behavior.

### 28.5 Run deck Core refactor P0–Phase 4 (2026-10-20, local; not sent)

The user authorized each phase separately and reviewed every gate. Decisions, the phase gates and the test evidence are in the Core refactor decision record (Git history: `2026-10-19-run-deck-core-refactor-plan.md`) (§3 decisions, §10–§15 gates). The work is uncommitted on `feature/Cheon/refactor/core-system` on top of `f6a18f3f`. It supersedes the §28.3 statements below about the membership read path, the deck result shapes and the null-membership storage seam.

**What changed for owners**

| Area | Change |
|---|---|
| Participant list (D-1) | `CombatStateData.Membership` is the only stored copy. `CharacterBuffer` takes it as an argument and keeps no copy. `EntryRosterRevision` is removed (no readers). |
| Battle install (D-3) | Install requires membership plus the current roster revision, deck and Run. Stale values are rejected before any store installs. The command battle ID must match exactly; 0 is not a wildcard. The null-membership storage seam of §28.3 is gone. The completion Main comes from membership. |
| Raw writes (D-1b) | `CharacterWriteSink.Apply` throws while a battle is installed; in battle, use `BattleWriteSink`. |
| Deck results (D-2) | `DeckEvaluationResult.Current` and `DeckEditResult.Current` are the new `DeckRuleState`, and `Evaluate`/`Confirm` compute no stats. `Read()` keeps `DeckEditorSnapshot.Stats` through one shared stats function (`OwnedStatsProjection`). |
| Availability seam (D-4) | The provider contract moved to Core as `IDeckEditAvailabilityRead` (GUID kept). The unbound default is `AuthorityUnbound`. Flow and composition steps are in the C1 request. |
| Boot | `RosterAcquireFailureReason.InvalidInitialDeck` (8) with `RosterAcquireResult.InitialDeckFailure`. |
| Representation (R3) | The Buffer keeps only reference checks (index 0 is an owned Main; the draw order is the exact participant-Sub permutation). The Buffer no longer re-judges order or roles, or per-key Main uniqueness (that is the ownership rule). `MainAlreadyOwned` (3) is never produced; it stays for compilation. |
| Required inputs (R4, R6) | `RosterSourceVersion` (Run, roster revision, deck instance) guards deck, entry, Run start and shop. `ShopOfferPlanner(definitions)`, `ShopPurchaseInput(definitions)` and `NewRunPreparation(initialDeckGrantIndices)` are required. |
| Removed (R7) | `BattleRosterSnapshot`, `CharacterShopRules`, `CharacterPurchaseDecision`, `PlanAcquireSub`, `CharacterRosterRules.IsEligible`. |
| Shop Main level (D-5) | A Main candidate is offered at `CharacterRosterRules.MainAcquisitionLevel` (1). |

**Owner notices**
- **Tests:** other-owner test lines touched under the D-3 exception are fixture lines only: 8 lines in 5 files, listed in the View handoff §6.
- **Product code:** no View, Flow, DB or Framework product code was changed.
- **Staged in handoffs:** `OwnedCharacterStatsReader(deck)` required, `LobbyDepartureText` reasons, the single-argument `PrepareBattle` callers, `InitializeRoster` callers, the retired key-14 sites, `PlayerLoadoutData.Validate`, and the C1 provider and composition.
- **Handoff documents:** [View](2026-10-19-run-deck-core-view-handoff.md), [C1](2026-10-19-run-deck-stage-availability-request.md), [E2](2026-10-19-run-deck-content-e2-handoff.md).

**Verification (Phase 4)**
- **Full Core:** 3521 / 3521.
- **Full Framework:** 5678 tests, 399 failed. 398 match the baseline exactly by name and message, and 0 are resolved.
- **The one new failure:** `TargetingAngleAssetTests.UnchangedUnits_KeepOriginalMethodsAndZeroAngle` (`Unit_Stage1Boss`). It comes from the user's intended data edit during the run (`TargetingMethod_Stage1BossRoar.asset` `choiceAngle` = Front), not from the refactor.

**Still open:** operating Boot and play (L3), C1, E2, and View E1/E3. This entry does not imply merge readiness, and no staging, commit, push or external message is implied.

<a id="tornado-2x2-core-request"></a>

## 29. 토네이도 2×2 효과 범위 확대 요청 (2026-10-20, View → Core, 미전달·미구현)

**2026-10-27 최신 정정 요청 — 판정 칸 진입 즉시 발동(미구현·미전달):** 사용자가 경유 칸과 최종 도착 칸 모두 지나가는 즉시 판정·연출되어야 한다고 확정했다. 현재 `TornadoSourceFact.TriggersArrivalRule = false`이며 `ResolveDue`만 점유자를 처리하므로 이 요구를 충족하지 않는다. 기존 이동의 칸별 진입 경계에서 토네이도 효과를 계산하고, 효과가 성립하면 `ForcedRelocation` 중단 계약으로 남은 경로를 끝내며 기존 `Relocated(TornadoSourceId)`와 도착 정산을 게시해야 한다. View의 근접/Collider 검사나 직접 Buffer 쓰기로 대체하지 않는다. 면역/목적지 없음 정책과 기존 연출은 유지한다. 접촉 후 원본의 이동·다음 due 정책은 미확정이며, Phase 1에 한정됐던 Core 직접 수정 허용도 추가 확인 전이다. 구체 작업·검증 한계는 [최신 구현 계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#tornado-2x2-plan)의 ‘진입 즉시 발동 정정’에 기록한다. 새 테스트 코드를 작성하지 않는다.

**2026-10-27 현재: 사용자 허용으로 Core Phase 1 구현·검증 완료.** 아래 요청 대기/미구현 문장은 과거 이력이다. 실제 변경은 §5.54와 최신 계획 Phase 1 완료 기록을 따른다. 단일 칸 판정·진영 3턴 발동·최초 생성 복원까지 완료했으며 View 외형/연출은 다음 Phase다. 외부 미전달.

**2026-10-27 최신 변경 — 네 칸 판정 요청 철회:** 사용자는 **Core 판정 한 칸 유지·외형만 2×2 확대·판정받은 유닛의 외형 중심 이동**으로 변경했다. 원본은 [설계 §9.6](../specs/2026-11-01-combat-tornado-view-design-Hojin.md#tornado-visual-only), 실행 순서는 [수정된 계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#tornado-2x2-plan)이다. 아래 `OccupiedCells`/`ContainsCell`·네 칸 배치/조회·복수 대상 확장 요청을 구현하지 않는다. 진영 3·6·9턴 종료 후의 발동 재개는 유지 요청이다. 중심 이동은 View 연출로 확정됐으며, 판정 시 자체 회전→중심 접근→카메라 밖 상승 완료 후 기존 무작위 칸으로 이동한다. 중심/상승/회전은 시각 pose이므로 Core의 위치·점유·방향·도착 효과를 바꾸지 않는다. 시각 중심은 판정 칸 중심에서 가로·세로 각각 반 칸씩 대각선으로 이동하도록 사용자 확정했다. 현재 Mapper 간격으로 View에서 계산하고 Core 크기·좌표·점유에는 추가하지 않는다. 외형·연출용 몸·유닛 접근·상승 축이 같은 시각 중심을 사용한다. 이번에는 문서만 수정했으며 외부 전달하지 않았다.

**2026-10-27 후속 확정:** 면역/목적지 없음은 Core 결과대로 유닛 연출을 생략한다. 밤 종료가 겹치면 토네이도 처리가 먼저이며, 토네이도·도착 효과 이후 전투가 계속될 때 밤 정산을 진행한다. View는 유닛 연출과 원본 축소/생성까지 완료한 뒤 밤 전환을 표시한다. Core가 View의 실시간 연출을 기다리도록 결합하지 않는다. 전용 연출 SO·무작위 회전·부드러운 축소/생성·가장자리 외형 넘침은 View 책임이다. 상승은 사용자 지정 RiseHeight까지 진행하며 화면 밖 여부를 자동 검사하지 않는다. 쉬운 순서 설명은 토네이도 대상 이동과 도착 효과 처리 → 살아 있는 전투의 밤 종료이며, View는 해당 연출을 순서대로 보여준다. 최신 계획에서 Core 요청은 Phase 1, View 구현은 Phase 2~4다. 문서 반영만 완료했으며 외부 미전달이다.

**2026-10-27 구현 착수 시 Phase 1 요청 구체화 — 미전달·미구현**

- `TornadoCalculator.ResolveDue`는 projection 검사만 수행한다. 단일 칸 강제 이동·도착 효과·원본 재배치를 복원해야 한다. 면역/목적지 없음에 가짜 유닛 사건을 만들지 않는다.
- `CombatTurnCalculator.EndPlayerTurn`·`EndEnemyPhase`에 발동 호출이 없다. 완료된 진영 3·6·9턴을 기존 ControllerTurnSequence로 판별할 수 있는지 검증하며 CompletedPlayerTurnCount는 유지한다.
- `EndEnemyPhase`의 밤 정산·유닛 회수·NightEnded보다 토네이도·도착 효과를 먼저 처리한다. Terminal이면 이후 밤 처리를 중단한다.
- 기존 `CombatCharacterEvent(Relocated, …, tornadoSourceId)`와 `CombatTornadoEvent(Expected, Final)`를 활용해 사건 샘플을 인계한다. View 높이·중심·회전 사실은 추가하지 않는다.
- 최초 랜덤 생성도 복원한다. `InitialTornadoPlacementTests.DeferredRandomTornado_DoesNotCreateSourcesOrConsumeRandomness` 및 TornadoEnvironmentTests의 보류 기대는 Core 담당이 새 요구에 맞게 변경한다. 사용자 지시로 삭제한 View 에셋 검사 두 함수와 별개다.
- 인수는 계획 P1의 3·6턴 단일 발동, 인접 칸 비대상, 중복 종료, 새 전투 초기화, 면역/후보 없음, 밤 경계(48번째 진영 턴), Terminal 순서를 따른다. 실제 게임 결과/사건을 사용하며 View에서 발동을 합성하지 않는다.

이번 사용자 지시는 계획 실행이며 Core 직접 수정 예외는 아직 명시되지 않았다. 이 세션은 요청만 구체화했고 Core는 수정하지 않았다. 전체 실행 점검은 최신 계획의 실행 기록에 남겼다.

**이하 2026-10-27 변경 전 이력 — 네 칸 확장 및 이전 Phase 번호는 현재 요청이 아님.**

**2026-10-27 구현 계획:** [토네이도 2×2 계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#tornado-2x2-plan) Phase 1~2가 Core 담당 요청이며 Phase 3~5는 공개 계약 인수 후 View 작업이다. Phase 0에서 설계 §9.5의 미확정 정책·공개 API 제안·시험 수정 범위를 확정한다. `OccupiedCells`/`ContainsCell`은 제공된 API가 아니라 제안이며, 진영 턴 완료마다 기존 순번을 활용할 수 있는지 먼저 검토해 달라. 기존 주기 보류 시험은 새 요구와 충돌하므로 담당자 검증에서 대체 대상으로 명시했다. 계획 작성은 외부 전달이나 구현 승인이 아니다.

**2026-10-27 재개 확인:** 사용자가 기존 강제 이동·원본 재배치 동작을 유지하고 실제 범위만 2×2 네 칸으로 확대하도록 확정했다. 요구와 현재 코드 대조의 원본은 [토네이도 설계 §9.4](../specs/2026-11-01-combat-tornado-view-design-Hojin.md#94-재개-요구와-현재-코드-대조--2026-10-27)다. 아래 "현재 Core" 설명은 2026-10-20 이력이다. 지금 `ResolveDue`는 인자 검사만 하고, 준비 단계의 최초 무작위 생성도 보류돼 있다. **턴 단위는 사용자 정정으로 확정됐다:** 플레이어 1턴 → 적 2턴 → 플레이어 3턴 종료 후 발동하고, 이후 각 진영 턴 완료를 세어 6턴 종료 후 재발동한다. 3주기나 플레이어 턴만 3번 세는 방식이 아니다. **Core 담당 요청:** 설계 §9.4의 진영 턴 완료 경계를 기준으로 기존 효과의 재개와 네 칸 범위·복수 대상·사건 공개를 함께 설계해 달라. `CompletedPlayerTurnCount`의 기존 의미를 바꾸거나 View 카운터로 우회하지 않는다. 이번 세션은 문서만 갱신했으며 Core 직접 수정·외부 전달·구현 인수는 하지 않았다.

사용자가 토네이도를 1×1에서 2×2로 늘리도록 문서 수정을 요청했고, 범위 확인에 **"게임 효과 범위도 2×2 네 칸으로 확대"**라고 답했다. 외형만 늘리는 변경이 아니다. §21의 최초 랜덤 구현 완료와 별개인 신규 요구이며 **이번에는 문서만 수정했다. Core 코드·Buffer·에셋·시험 변경이나 외부 전달은 하지 않았다.**

요구·정책 구분의 원본은 [토네이도 기획 정합 설계 §9](../specs/2026-11-01-combat-tornado-view-design-Hojin.md#tornado-2x2-scope), 인수 순서는 [기획 정합 계획 G4](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md#tornado-2x2-plan)다. 이 절에서 별도 게임 정책을 확정하지 않는다.

**현재 Core와 필요한 변경**

- `MovementTileFact.Cell`·`TornadoSourceFact`와 `TornadoCalculator.ResolveDue`는 현재 한 칸·한 대상(`SingleOrDefault`)이다. 원본 하나의 인접 네 칸 범위와 그 안의 복수 대상 처리가 필요하다. 기존 강제 이동 면역을 유지하며 다중 칸 유닛 지원까지 자동 확대하지 않는다.
- 최초 준비(`InitialTornadoPlacementCalculator.Place`)와 반복 재배치는 `TornadoCalculator.PlacementCandidates`를 함께 사용한다. 후보 기준 칸 하나뿐 아니라 **네 칸 전체의 필드 경계·생존 구조물·다른 Terrain 범위**를 검증해야 한다. 현재 최초 후보 없음=준비 실패, 반복 후보 없음=원본 제자리·due 갱신의 구분도 인수한다.
- `Relocated`·유닛 도착 효과·Terminal·`CombatTornadoEvent`의 복수 대상 순서와 발동 묶음을 공개해 달라. 앞선 유닛 처리로 변한 상태·난수를 뒤 대상으로 넘기는 정책을 명시해 달라. View가 그 순서나 대상을 추정하지 않도록 한다.
- 2×2의 기준 칸·네 칸·표시 중심을 View가 읽을 계약, 셀 환경 조회·배치 검증·공개·Hover에서 같은 Source ID 범위를 일관되게 보는 경로를 검토해 달라. View가 InGame DB나 꺼진 연출 Collider로 게임 범위를 계산하지 않는다.

**아직 확정하지 않은 항목:** 기준 칸의 의미와 기존 고정/랜덤 입력 호환, 대상 집합 시점·중복 제거·처리 순서, 이동 목적지의 옛 네 칸 제외 여부, 자기 이전 영역과 일부 겹친 재배치 허용 여부, 복수 대상 사건 묶음이다. 상세는 설계 §9.2를 따른다. Core 계약안과 View의 동시/순차·숨김 시점을 확정한 뒤 구현한다.

**View 준비 상태:** 외형은 네 칸 영역의 중심에 맞춰 확대하고 전투 재질·연출용 루트 Collider를 함께 보정할 계획이다. 수치 후보는 Toneido 설계 §4.3.2에만 둔다. TR-2는 1×1 몸 조회/콜라이더까지 부분 완료이고, 유닛 Y 보정은 여전히 보류, TR-3~TR-5는 미착수다.

## 30. 전투 View 구조 개선이 기대는 Core 동작 (2026-10-22, View 작업, 요청 없음, 미전달)

전투 View 구조 개선([설계 2판](../specs/2026-10-22-combat-view-structure-improvement-design.md), [Task 계획·실행 기록](../plans/2026-10-22-combat-view-structure-improvement-plan.md))은 **Core 코드를 바꾸지 않았다.** 아래는 바뀐 구조가 요청하지 않고 기대는 Core 동작, Core 호출의 변화, 그리고 고친 천명님 줄이다. 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase 1~6 모두 Unity 컴파일·EditMode 시험·재생 문제 없음(2026-10-22 사용자 확인). **아래 동작이 바뀔 예정이면 알려 달라.**

### 30.1 기대는 Core 동작

| 동작 | 근거 | 기대는 곳 |
|---|---|---|
| `EndBattle`은 전투 캐릭터와 전투 저장의 현재 전투·영혼·미리보기만 비운다. `BattleInstanceId`·완료·결과 순번 S·현재 맵 선택은 남는다 | `BufferSystems/Core/BattleTransactions.cs:247-251`(10-23 병합 뒤 줄 번호), `BufferSystems/Combat/CombatStateBuffer.cs:167-172` | 모든 결과가 "정리 → 결과 표시" 순서다. 결과 화면(`CombatEndUI`)은 정리 뒤에 완료·보상 영수증·Run 요약·맵 좌표를 읽는다. 정리 Job의 사후 확인은 "전투 없음·S 그대로"만 본다 |
| 한 전투의 완료 객체는 `EndBattle`·`CompleteChapter` 뒤에도 같은 참조로 남고, 다음 전투를 준비할 때 비워진다 | — (참조 동일성 약속) | 새 판정 `CombatCompletionIdentity.IsSame` 하나(View의 완료 표시·확인 콜백·결과 유지). 값 비교를 쓰지 않는 이유와 이 약속은 그 파일 주석에 있다 |
| 보스 승리의 Run 종료(`RunOutcome`·`LastSummary`)는 `CompleteChapter` 뒤에 확정된다 | 기반 한계(설계 §4.1) | 정리 걸음이 `EndBattle` → (보스 승리면) `CompleteChapter` → 표시 순서다. 그래서 결과 화면은 어떤 결과든 Run 결과가 확정된 뒤 열린다 |
| `CompleteChapter`의 거절은 연결 정의 없음까지 모두 데이터 오류다 | `b6c8513e` 주석 | `CombatChapterCompletionJob`이 거절을 예외(F1)로 |
| 승패 기록(`Finished`)과 완료는 한 거래로 함께 게시된다 | — | View의 자체 제출 정지(`Finished ∨ 완료`)와 Plan 1행(`완료`)을 같은 사실로 본다(주석만, 코드 그대로) |
| 비전투 노드의 전투 진입은 거절이 아니라 예외다(`BattleEntryService.ValidateCombatTarget`) | 10-21 문제점 분석 P23 직접 대조 | 준비 Job → F1. 개발 진입은 `GameBootstrap.ResolveDevEntryNode`가 전투 노드로 보정한다. Bootstrap·CLAUDE.md의 "거절" 설명을 "예외"로 고쳤다 |

### 30.2 Core 호출의 변화 (View 쪽)

- **전투 이탈에서 새 Run을 시작하지 않는다.** `CombatNewRunJob`(`RunStart.StartNewRun`)·`CombatPlan.NewRunLoadoutKey`를 지웠다. 운영 새 Run은 로비 출발(`LobbyRunStartJob`)이 만든다. 그래서 Run이 끝난 뒤 로비에 있는 동안 Core는 끝난 Run 상태이고, 보유 로스터를 읽는 Global UI가 끝난 Run의 로스터를 본다(로비에 보이는 그림은 같음, 사용자가 받아들임).
- **Run 끝 판정 Job을 지웠다.** 목적지는 확인 뒤 Run 진행 읽기 싱크의 `RunOutcome` 하나로 정한다(`CombatPlan.DestinationOf`: 진행 중이면 StageSelect, 아니면 Lobby). 사실 읽기(`CombatProgressReader`)가 Run 진행 읽기 싱크를 하나 더 받는다.
- **`CompleteChapter`는 보스 승리일 때만, 정리 걸음 안에서 한 번** 부른다(전에는 확인 Job·Run 끝 Job이 앞뒤로 나눠 봄).
- **전투 Job은 Core 그룹 대신 서비스 하나씩** 받는다: `ICombatCommandService`·`ICombatPreviewService`·`ICombatAdvanceService`·`IBattleCompletionService`·`IChapterProgressionCoordinator`. 호출하는 메서드·횟수는 같다.
- **판단은 실행 순간이다.** 등록과 실행 사이 한 프레임에 들어온 포기 확인이 이제 등록된 명령보다 먼저 처리된다(사용자가 받아들임, 재구성 설계 J1 개정 줄). 승패가 먼저 기록되면 그 승패로 나가는 규칙(K22 A4)은 그대로다.
- 정리·포기·미리보기 Job의 호출 직후 확인 중 다음 판단이 기대지 않는 것(정리 전 대상 재확인, 미리보기 "그 사이 바뀜", 포기 Job의 결과·순번 보존)은 걷었다(설계 §5.2).

### 30.3 고친 천명님 줄

| 파일 | 원래 줄 | 바뀐 것 |
|---|---|---|
| `C/Plans/CombatPlan.cs` | `RunEndDestination => Lobby`와 설명(`b6c8513e`) | 목적지 함수 `DestinationOf(RunOutcome)` 하나로 |
| `C/Jobs/CombatExitJobs.cs` | `CombatRunEndJob`의 챕터 완료·거절 처리(`b6c8513e`), `EndBattle` 호출(`bc8b8103`) | `CombatChapterCompletionJob`(보스 승리일 때만 등록, 거절은 예외 그대로). `EndBattle`은 서비스로 |
| `C/Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs`·`CombatAbandonJob.cs` | 그룹 경유 Core 호출 줄(`bc8b8103`) | 같은 메서드를 서비스 하나로 |
| `V/Combat/ICombatViewService.cs` | 처음 만든 계약(`e0b02cc7`). 바뀐 선언 줄은 현강님 줄 | 진행 계약을 상속하지 않고 `Progress`로 냄, `IsPlaybackBusy` 삭제(현강님 메모 §1) |

경로: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`, `V/` = `…/RuntimeSystem/InGameSystem/ViewSystem/`.

**고친 천명님 시험**(확인 내용 그대로, 걷은 동작 단언만 지움)
- `CombatProgressJobTests.RunEnd_BossVictoryRejection_Throws` → `ChapterCompletion_Rejection_Throws`(호출만)
- `CombatEntryIntegrationTests`: `FinalChapterVictory_RunCompleted_ReturnsToLobby`·`Abandon_InDeployment_RecordsAbandonmentThenReturnsToLobby`(이름에서 새 Run 걷음), 공용 도우미 `AssertNewRunThenReturn` → `AssertRunEndThenReturn`(호진님과 함께 쓴 줄, 새 Run 상태 단언 일곱 삭제, 목적지 로비·오류 없음·Runner 빔·전투 없음·Run 요약 넷은 그대로)
- `CombatPlanTests`: `[TestCase(JobType.AdvanceCombat)]`(`87348b86`) → `[TestCase((JobType)5)]`, 같은 시험의 가짜 기록 키도 `(JobType)5`. 연결 줄 `JobType.AdvanceCombat`를 지웠기 때문이다(번호 5는 비워 둠). 확인 내용("모르는 종류는 거절")은 그대로

## 31. 전투 View 조정자 C 단계가 기대는 Core 동작 (2026-10-23, View 작업, 요청 없음, 미전달)

조정자 C 단계([설계](../specs/2026-10-23-combat-view-coordinator-c-stage-design.md), [Task 계획·실행 기록](../plans/2026-10-23-combat-view-coordinator-c-stage-plan.md))는 **Core 코드를 바꾸지 않았다.** 아래는 바뀐 구조가 요청 없이 기대는 Core 동작과, 고친 천명님 줄·시험이다.
- 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자).
- Phase C-a~C-d 모두 Unity 확인 문제 없음(2026-10-23 사용자).
- 줄 번호는 HEAD `4c328d07` 기준이다.
- **아래 동작이 바뀔 예정이면 알려 달라.**

### 31.1 기대는 Core 동작

| 동작 | 근거 | 기대는 곳 |
|---|---|---|
| 받아들인 명령의 "끝났음"과 `Finished` 국면은 늘 같고, 끝난 명령은 완료 제안 없이 게시되지 않으며 완료와 같은 `Apply` 거래로 설치된다 | `InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatCommandTransition.cs:88`, `BufferSystems/Core/BattleTransactions.cs:219·228·230` | View의 "전투 끝" 판정 `CombatInputPolicy.IsTerminal`이 `Finished`를 보지 않고 **완료 있음 하나**로 바뀌었다(전에는 `Finished ∨ 완료`). 전투 Plan 1행과 같은 사실이다. `Finished`인데 완료가 없는 상태가 생기면, 명령·미리보기는 국면 검사(PlayerInput만)로 여전히 막히지만 입력 Layer 연결·포기 요청·환경 호버·행동 버튼 표시가 열린다 |
| 포기는 국면을 두고 완료만 남긴다(`CompleteBattle`) | `BattleTransactions.cs:150-172`, `BattleCompletionService.AbandonBattle` | 포기 뒤 끝 판정도 완료로 본다. 결과 없이 생기는 완료라, 그 전에 Plan의 포기 줄이 제출을 정지한다 |
| `EndBattle`은 결과 순번 S·전투 번호·완료를 남긴다 | `BufferSystems/Combat/CombatStateBuffer.cs:167-172`(§30.1과 같음) | View의 "확인하지 않은 결과(P < S)" 판정이 전투 유무를 보지 않게 됐다(골격과 같은 식, 규칙 원본 `CombatProgressFacts.IsUnpresented`). 정리 뒤 완료 표시 앞의 "남은 재생" 검사가 이 값으로 P = S를 확인한다 |
| 정리 → (보스 승리면) 챕터 완료 → 완료 표시가 같은 Runner Tick에 이어 돈다(Core 호출은 각 한 번) | `C/Plans/CombatPlan.cs` `Settle`(§30.2와 같음) | View가 "결과 유지가 아닌데 전투가 사라짐"을 이제 F1로 보고한다. 정상 흐름에서는 닿지 않는다(연쇄 중간에 Runner 한 Tick 예산 32개가 끊기는 경우는 확인하지 못한 추측) |

### 31.2 고친 천명님 줄

| 파일 | 원래 줄 | 바뀐 것 |
|---|---|---|
| `V/Combat/ICombatViewService.cs:23-27` | `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`(`e0b02cc7`) | 계약에서 지움. 바깥 운영 호출이 없었다(로딩 노드·씬 이벤트·시험만 부름). UI가 쓰는 멤버와 `Progress`는 그대로 |
| `V/Combat/CombatViewSystem.cs:379`·`:587` | `public void Attach`·`public void Detach`(`e0b02cc7`) | `internal`(로딩 노드·씬 이벤트·시험이 부름) |
| `V/Combat/CombatViewSystem.cs:1120` | `CaptureLost() => FocusLost()`(`e0b02cc7`) | 삭제(하는 일이 `FocusLost`와 같았다) |
| `V/Combat/CombatViewSystem.cs:86` | 필드 `_sceneRoot`(`e0b02cc7`) | 이름 `_subscribedRoot`(이벤트를 구독한 연결 수명의 루트라는 뜻) |

경로: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `C/` = `…/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`.

### 31.3 고친 천명님 시험(사용자 허락, 확인 내용 그대로)

- `CombatMovementViewTests.FocusOrCaptureLoss_CancelsFrozenDraft`(`:347-349·356`): capture 사례·매개변수를 지우고 `[Test]`·`r.View.FocusLost()`로 바꿨다. 이름은 그대로다.
- `CombatMovementViewTests.cs:394`: `group.Combat.Attach(null)` → `owner.Attach(null)`(같은 시험 위 줄의 캐스트 변수).
- `CoreStageCombatCycleTests.cs:355`: `view.Combat.RequestReturn()` → `((CombatViewSystem)view.Combat).RequestReturn()`(같은 파일에 같은 캐스트가 이미 있음).
- 천명님 파일 안의 우리 줄 `CombatMovementViewTests.cs:46·186·248`: `CombatProgressTestDriver(…, r.View)` → `r.View.Progress`. 조정자가 진행 계약을 직접 구현하지 않게 됐기 때문이다.

### 31.4 시험 장면 변화(고치지 않음)

- `CombatMovementViewTests.BattleEndBeforePointerCallback_CancelsWithoutMappingMissingState`(`:364`)는 표시가 연결된 채 `EndBattle`한 뒤 Tick한다. 이 장면이 이제 F1 경로다.
- 이 시험의 rig는 오류 경계가 아무것도 하지 않고(`_ => {}`), 단언(명령 없음·Layer 해제)도 F1 뒤에 그대로라 통과한다(Unity 확인 문제 없음).
- 시험 이름이 말하는 "취소"는 이제 오류 정리로 일어난다.

## 32. 전투 골격 점검이 기대는 Core 동작 (2026-10-23~24, View 작업, 요청 없음, 미전달)

근거: [골격 점검 설계](../specs/2026-10-23-combat-skeleton-check-design.md), [Task 계획·실행 기록](../plans/2026-10-23-combat-skeleton-check-plan.md). Core 파일은 바꾸지 않았다. 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase K-a~K-e 모두 Unity 확인 문제 없음(사용자).

경로: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`.

### 32.1 바뀐 천명님 줄

- `C/Jobs/CombatPrepareJob.cs` HEAD `:43`(`bc8b8103`, 직행 진입의 Core 호출): 그룹에서 꺼내던 `_service.CoreGroup.BattleEntry.EnterBattle(…)`이 생성자로 받은 `IBattleEntryService`의 `EnterBattle(chapterKey, nodeId)`가 됐다. 부르는 API·인자·결과 게시·실패 처리는 그대로다.
- 준비 Job은 이제 진입 종류에 필요한 Core 서비스 하나만 받는다. 직행은 `IBattleEntryService`, 맵 진입은 `IStageCommandProcessingService`다. 서비스는 Plan이 등록 때 `CoreGroup`에서 꺼내 넘긴다.
- 천명님 시험은 바꾸지 않았다.

### 32.2 기대는 Core 동작(바꾸지 않음)

- **자발적 포기 완료는 패배일 때만 생긴다.** 골격의 포기 판정이 `CombatPlan.IsAbandonment`(패배 ∧ `VoluntaryAbandonment`) 하나로 모였다.
  - 결과 계약 확인(`CombatCompletionJob.RequireCompletion`)은 전에는 사유만 봤다.
  - `StageOutcomeSnapshot`이 자발적 포기를 Defeat에서만 받으므로 실제로 달라지는 경우는 없다.
- **포기 요청의 `AlreadyCompleted`는 운영에서 닿지 않는다.** 판단(`Decide`)은 완료가 없을 때만 포기를 고르고, 그 판단과 같은 Job 안에서 포기 Job을 실행한다(K-1). 그래서 골격은 `AlreadyCompleted`를 받을 수 없는 결과(예외 → F1)로 다룬다. Core의 그 반환 경로(`BattleCompletionService`)는 그대로다.
- 진행 사실 읽기는 더 이상 캐릭터 Buffer를 받지 않는다(쓰지 않던 `ActiveCharacterCount` 삭제). Core 쪽 변화는 없다.

### 32.3 앞 절에서 바뀐 사실

- §31.1 마지막 행 "정리 → (보스 승리면) 챕터 완료 → 완료 표시가 같은 Runner Tick에 이어 돈다"와 그 단서(Runner 한 Tick 예산 32개가 끊기는 경우는 확인하지 못한 추측): 이제 그 셋은 한 Job(`CombatNextJob`) 안에서 차례로 돈다(K-1). Runner 큐·예산에 기대지 않으므로 그 단서는 해당이 없다. Core 호출은 각 한 번 그대로다.

## 33. 전투 View 단계 1라운드가 바꾼 천명님 줄 (2026-10-24, View 작업, 요청 없음, 미전달)

근거: [View 단계 설계](../specs/2026-10-24-combat-view-stage-design.md), [1라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round1-plan.md). Core 파일은 바꾸지 않았다. 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase V-a~V-d 모두 Unity 확인 문제 없음(사용자). 이 절의 줄(V-c)은 그 확인에 들어 있다. 줄 번호는 기준 커밋 `8ed3f8cb` 기준이다.

- `ViewSystem/Combat/Display/Hud/CombatHud.cs`(V-c)
  - 옛 드로우 문구를 걷었다. 지운 것은 `remainingDrawText` 칸, `HideLegacyDrawText`, 그 호출 셋(`OnEnable`·`Render`·`Clear`)이다.
  - 쓰지 않게 된 using 셋을 지웠다. `_shownRunGold` 줄은 위치만 옮겼다.
  - 씬의 그 텍스트 객체는 이미 꺼져 있었다(`Combat.unity` `&1825410295`). 그래서 화면은 그대로다.
- 시험 `CombatDisplayReuseTests.Hud_UsesRunGold_AndLegacyDrawReferenceIsOptionalAndHidden`에서 드로우 문구 줄 셋(`:829·832·833`)을 지웠다(사용자 허락). 골드·참조 검사·Clear 단언은 그대로다. 시험 이름은 바꾸지 않았다.
- 이번 라운드가 새로 기대는 Core 동작은 없다.

## 34. 전투 View 단계 2라운드가 바꾼 천명님 줄 (2026-10-25, View 작업, 요청 없음, 미전달)

근거: [2라운드 설계](../specs/2026-10-24-combat-view-stage-round2-design.md), [2라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round2-plan.md). Core 파일은 바꾸지 않았다. 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase V2-a~V2-g 모두 Unity 확인 문제 없음(사용자). 줄 번호는 기준 커밋 `23436b00` 기준이다.

- **Buffer 번호 있는 비우기(V2-a):** `CombatViewBuffer`의 쓰기 계약에 `bool ClearPreview(int number)`를 더했다. 슬롯의 요청이 그 번호일 때만 비운다. Core가 읽는 읽기 계약은 그대로다.
  - `CombatMovementInteraction.cs:391-395` `ClearOwnedPreview`: "슬롯 번호가 내 번호면 비우기" 메서드(다섯 줄)를 `_input.ClearPreview(_requestNumber)` 식 본문 한 줄로 바꿨다. 판단은 같다.
  - `AttackTargetingInteraction.cs:487-488`: 호버 요청 비우기도 같은 꼴로 바꿨다(`:486`은 우리 줄).
- **공격 Drag 넘겨받기(V2-c):** `CombatPathInput.cs:24` 클래스 선언에 `: ICombatAttackDragTarget`을 더했다. 경로 입력이 넘겨받기 소유자의 받는 쪽 계약을 명시적 구현으로 단다. 경로 골격은 그대로다.
- **조정자(V2-f):** `CombatViewSystem.cs`
  - 지움: `:94` `_currency` 필드, `:104` `_combatQueries` 필드
  - `:244` 재화 검사는 표시 값 읽기 `Display/CombatDisplayValueReader.cs`의 생성자 검사로 꼴이 바뀌었다.
  - `:879·882·884·887`(`ReadDisplayValues` 몸통)은 `CombatDisplayValueReader.Read`로 옮겼다. 읽는 값과 순서는 같다.
- **표시 서비스 묶음(V2-g):** `CombatDisplayServices.cs:36` 화면 전환 서비스가 선택 → 필수가 됐다. 운영 `From`은 늘 넘긴다.
- 고친 천명님 시험(허락):
  - `CombatMovementViewTests.cs` 가짜 `CountingInput`에 `ClearPreview(int)` 한 줄(V2-a, Q-T5)
  - `FrameworkRuntimeCompositionTests.cs:230`(V2-f, Q-T6 b): 리플렉션 경로를 표시 값 읽기의 `_timeline`으로 맞춤. 같은 객체 비교·같은 문구
  - `CombatMovementViewTests.cs:586`(V2-g, Q-T6 c): 조정자 생성자 인자에 `CombatSceneFactory.Instance` 둘
  - `CombatMovementViewTests.BindView`(`:420-428`, V2-f·V2-g, Q-T6 d)
    - `BindServices`에 인자 다섯을 붙였다. V2-f의 빈 타임라인과, V2-g의 텍스트 표시 가짜·재고·튜토리얼 둘이다. 붙인 자리는 그 호출의 우리 줄 끝이고, 천명님 줄 `:423`은 그대로다.
    - 같은 호출 안 우리 줄 `:426-427`의 `new CombatDisplayServices(…)`는 시험 도우미 `CombatViewSystemTests.DisplayServices(…)`로 바꿨다.
    - `view.InitData(CombatViewSystemTests.Database);` 한 줄과 주석 한 줄을 더했다. 연결 입력 묶음의 환경 호버가 View DB 표를 쓰기 때문이다.
- 이번 라운드가 새로 기대는 Core 동작은 없다.

## 35. 전투 View 단계 3라운드가 바꾼 천명님 줄 (2026-10-25, View 작업, 요청 없음, 미전달)

근거: [3라운드 설계](../specs/2026-10-25-combat-view-stage-round3-design.md), [3라운드 Task 계획·실행 기록](../plans/2026-10-25-combat-view-stage-round3-plan.md). Core 파일은 바꾸지 않았다. 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase V3-a~V3-i 모두 컴파일 미확인이다. 줄 번호는 기준 커밋 `35562e96` 기준이다.

- **입력 연결로 옮긴 줄(V3-b):** `CombatViewSystem.cs:100` `_layers` 필드는 새 입력 연결 `Input/CombatInputConnection.cs`(수신 창구·Layer 맞춤·해제)로 옮겼다. 옮긴 메서드의 중괄호 줄 `:856·873·910·915·922·934`도 함께 갔다. 하는 일은 같다.
- **연결 객체(V3-c):** `CombatViewSystem.cs:85` `_battleInstance`는 연결 객체 `CombatViewConnection`의 전투 번호가 됐다. 판정은 같다.
- **소리 프리셋(V3-h):** `CombatEventAudio.cs:63-64·67·74`(Akimbo 특례·분류/10 식)를 지웠다. 값은 이제 View DB(유닛 외형 칸 둘·설정 칸 넷)에 있고, 옛 규칙으로 계산한 값을 넣었다. 유닛 프리셋은 실행 중 분류가 아니라 외형 값이다. Core는 분류 키를 유닛 정의에서 가져오므로 운영 DB에서는 같다.
- **사건 Presenter(V3-i):** `CombatSceneEventPresenter.cs:77`(병합 귀속, 이펙트 선택 인자)은 필수 생성자 인자가 됐다. 표시 Factory의 `effects: effects` 줄도 새 생성 줄로 바뀌었다.
- 고친 천명님 시험(허락)
  - `StageMapCombatEntryBootSmokeTests.cs:137-138`(Q-T7 d): 유닛 프리셋을 실제 DB 프리셋으로 읽음. 확인 내용 그대로
  - `GameAudioTests.cs:52·56`: 분류 시험의 `[TestCase]` 줄. 사용자 결정("분류 → 그 분류 실제 유닛")으로 사례가 유닛 키가 됐다.
- **Ballista 시험(V3-e):** `CombatBallistaAttackTests.cs:63-64`(SetUp)는 글자 그대로다. 코드 모션 길을 걷으며 이 파일의 시험 사례는 지웠지만, 다른 시험이 빌려 쓰는 장면 준비는 남겼다(사용자 결정).
- 이번 라운드가 새로 기대는 Core 동작은 없다.

## 36. Optional development tools proposal and A/B/C implementation (2026-10-23, local; not sent)

The user requested a design and a Task/Phase plan for development APIs, with an explicit constraint that this optional feature must not restructure or weaken the original systems. They confirmed reward-free skipping and an idle-StageSelect first version; active-combat skipping is deferred.

The proposed contracts and ownership are in the [design](../specs/2026-10-23-development-tools-design.md), with allowed files, required reuse routes, prohibitions and stop gates in the [plan](../plans/2026-10-23-development-tools-plan.md). Phase A proposes guarded Core unit/gold grants; B proposes forward-path progress bypass; C proposes the optional View facade and integration verification. Each phase requires separate implementation authorization. Core/Buffer ownership remains unchanged.

Reuse `Characters.Acquire(key)` and the current single Run currency store's expected-balance prepare/install path. Do not use `SetAmount` for runtime grants because it resets `RunSpent`. Navigation prepares supported progress states and then uses ordinary stage selection; it must not fabricate victory, reset the Run, bypass ordinary battle entry, or edit Framework/asmdefs. Boss/noncombat bypass, cross-chapter teleport, panels and hotkeys are outside the proposed first version.

At the initial proposal stage, only documentation was written; that inspection was not runtime validation. Later explicit Phase A authorization and its implementation evidence are recorded below.

**Second review (2026-10-23):** Design/plan §12 records corrections for mandatory normal-navigation reuse, handled-but-unconsumed input admission, shared fault reporting, selection-only storage inputs and DB/graph binding. Core commands and Buffer transaction access use internal capabilities on the existing Groups; the ordinary public Core Group interface and `IBufferQuery` stay unchanged. Lower factory compositions default to omission, with paired inclusion only at the existing full Systems composition. Framework changes, alternate stage-entry routes and runtime enable/disable state remain prohibited. No implementation phase had started at that review point.

**Phase A implementation (2026-10-23, explicitly authorized Core/Buffer exception):** A1/A2 now provide internal gold and unit grant APIs, live admission and guarded optional lower-Core composition. Gold uses the existing currency store and preserves `RunSpent`; unit grants call `Characters.Acquire(key)` once and preserve native failure details and selected deck state. The only existing product files changed are `BufferSystemGroup.cs`, `InGameCoreSystemFactory.cs` and `InGameCoreSystemGroup.cs`, at the plan's approved seams. Existing roster, deck and currency implementations, public ordinary Group interfaces, Framework, ServiceKeys, asmdefs, View/Flow and assets remain unchanged.

Phase A verification: 63/63 new tests passed; the combined required set was 161/165, with all four failures reproduced on the baseline and compared by exact test name and complete failure message. Non-Editor Development/release managed compilation succeeded with zero errors, and release DLL inspection confirmed the development types and integration members were absent. This was not an actual Player build or manual play acceptance. The [plan §13](../plans/2026-10-23-development-tools-plan.md#13-phase-a-execution-record--2026-10-23) preserves the file list, failure identities, evidence paths and architecture/locality audit. Work stopped at that review boundary before the separate Phase B authorization below.

**Phase B implementation (2026-10-23, separately authorized Core/Buffer work):** B1/B2 now provide `SkipNextNode` and `PrepareJumpToNode`. Every hop reuses ordinary `ChapterNavigationRules.Evaluate`; only regular Combat intermediates with successors may be bypassed. The runtime validates chosen DB bindings, then the Buffer transaction prepares every clear and the final selection before installing any change. An already-adjacent jump is a no-op. Target entry remains the existing ordinary route. No rewards, battle completion, Run reset or cross-chapter transition is fabricated.

The only additional ordinary store surface is guarded internal `PrepareDevelopmentSelection`, reusing `StageSelectionData.RequireEntry`, `MatchesExpected` and existing selection installation. Normal methods remain unchanged. The factory injects its existing stage manager; no helper service, new key or root export was added. Two already-listed grant-test constructions received that dependency without changing assertions. Framework, asmdefs, View/Flow and assets remain unchanged.

Phase B verification: 103/103 added cases passed; the expanded combined set is 372/409. All 37 failures are existing `ChapterContinuationServiceTests` failures with the same complete messages as the pre-B baseline. A reviewed exception-handling defect was reproduced and fixed; the final independent review has no remaining P1/P2 findings. Development/release managed compilation has zero errors, all 17 development types are absent in release, and seven normal release type signatures match Phase A. The [plan §14](../plans/2026-10-23-development-tools-plan.md#14-phase-b-execution-record--2026-10-23) records exact evidence and limits. C, full-runtime inclusion, the View facade, actual Player builds and manual acceptance remain unstarted. Work stops at the Phase B review boundary; no commit, push or external message occurred.

**Phase C implementation delivery (2026-10-23, explicitly authorized):** The optional public `ViewGroup.DevelopmentTools` facade now consumes the internal Core commands, existing map readiness and shared fault boundary. View performs no gameplay writes or calculations. Changed navigation requests existing redraw once; ordinary target entry continues through the numbered StageView input, Flow/Jobs and entry observer. Lower paired/leaf factories default to omission; the existing full Systems runtime opts in only for Editor/Development. The only existing production edits are the six approved View/map/composition seams. Eight listed ViewGroup mocks have guarded null properties only. No Phase C changes were made to A/B Core/Buffer logic, Framework, ServiceKeys, asmdefs, ordinary gameplay rules or assets.

C verification: 57/57 new cases passed; A+B+C feature cases total 223/223. Combined required evidence is 318/350 with 32 failures reproduced on pre-C source by exact test name and full message. Both actual Windows Player builds succeeded with zero errors and three warnings. Packaged Release DLLs contain none of the 19 development types, guarded members or feature references, and neither Player includes test assemblies. Independent review found no production P1/P2 issues. The actual Boot smoke reaches Combat but then fails an existing audio-preset assertion that also fails without C. The [plan §15](../plans/2026-10-23-development-tools-plan.md#15-phase-c-delivery-and-outstanding-acceptance--2026-10-23) records filters, source restoration hashes, file scope, build evidence and test-fixture limits.

**Acceptance boundary:** The code is delivered locally, but final Phase C acceptance remains open for a successful actual-scene round-trip and manual checks of granted-unit deck selection, displayed gold, marker rebind and ordinary target interaction. No runtime panel/hotkey, unrelated audio fix, commit, push or external handoff was performed.

**View consumer handoff (2026-10-23, documentation only; not sent):** [Development Tools — View Handoff](2026-10-23-development-tools-view-handoff.md) explains `ViewGroup.DevelopmentTools` lookup, build guards, per-action contexts, all four operations, refusal/fault handling, redraw readiness, ordinary target entry and the pending manual acceptance checklist. It follows the existing UI input/registration rules and introduces no new UI or gameplay behavior.

## 37. `0a6fe9c` 병합 정리 (2026-10-25, 사용자 지시, 커밋 전)

사용자 요청으로 HEAD `c86b6759`에 현강님 통합 커밋 `0a6fe9c43105d52579b8cbd523382fad262a2ee2`를 `--no-commit --no-ff`로 병합했다. 새 Core·공용 AP UI·개발 도구 등 대상 커밋의 변경을 인수하고, 우리 3라운드 구조 개선은 유지했다. 커밋·push는 사용자가 한다.

- 충돌 파일 12개를 정리했다. 표시 Factory에는 우리 소리 프리셋과 현강님 TurnCost를 함께 주입한다. 입력 Factory 계약은 3라운드의 재료 묶음 서명을 유지한다.
- 입력 사실은 연결 객체가 가진 선택 창구에서 읽고, 공격 조준의 전투·진영 턴 번호 검사를 유지한다. 새 Core의 규칙이나 예산을 View에서 계산하지 않는다.
- 오류 경계가 `Common`에서 `ErrorHandling`으로 옮겨져, 우리 새 연결 객체·입력 재료 등 다섯 파일의 using을 맞췄다. Core 파일의 별도 수정은 없다.
- 턴 처리기는 새 `PlayerTurnEnded`·`EnemyPhaseEnded`를 쓰되, 이미 삭제한 코드 Ballista 재장전 의존성은 복원하지 않았다. 자동 병합으로 생긴 `TryBeginReload` 호출도 걷었다. Ballista 클립 연출은 유지한다.
- 소리 프리셋은 3라운드 View DB 원본을 유지한다. 유닛 턴 시작이 사라져 진영 턴 시작에서는 유닛 선택 소리를 내지 않는다.
- 시험 충돌은 기존 결정대로 Ballista 장면 준비만 남겼다. 호진님 통합 시험의 연결 객체 리플렉션을 유지하며, 행위자 선택은 새 Core에 맞게 Main 유닛을 고른다. 우리 `CombatEventSoundsTests`의 보스 사실 생성자를 `ActionUse`로 맞추고 진영 턴 시작의 무음 단언을 맞췄다. 새 시험 사례는 추가하지 않았다.
- 문서 충돌은 양쪽 기록을 보존했다. 개발 도구 기록은 §36으로 번호를 구분했다.
- 정적 점검: 충돌 표시·삭제된 주요 API 참조·주요 전투 enum 참조를 확인했다. 기존 Missing Script DB 에셋은 삭제하지 않았다. SSOT는 Core 사실·View 소리 DB를 각각 유지하고, 생성은 Factory, 연결 수명은 연결 객체가 맡도록 합쳤다. 서비스 신규 등록이나 Framework 계약 변경은 추가하지 않았다.
- Unity 컴파일·EditMode 시험·재생은 미확인이다. 컴파일 확인 뒤 EditMode와 `DataBaseAssetTests`, 새 AP UI·선택·공격/이동·턴 종료·밤 종료를 확인해야 한다.

## 38. 전투 View 4라운드 — 새 Core 소비 경계 (2026-10-26, 미전달)

근거: [4라운드 설계](../specs/2026-10-25-combat-view-stage-round4-design.md), [계획 §6·§7](../plans/2026-10-25-combat-view-stage-round4-plan.md). 구현 기준은 `f616d5d9`이며 a/d/e/f/g/h는 작업 트리에 있다. Vm.1의 실제 소비 계약 정리이며 Core/Buffer 파일은 이번 라운드에서 수정하지 않았다. **현 구현 범위의 추가 Core API 요청은 없다.** 아래 보류 효과를 View에서 구현했다는 뜻은 아니다.

| 원본 계약 | 현재 View 소비 |
|---|---|
| `CombatViewPreviewData.ActionAvailability` → `CombatPreviewResult.ActionAvailability` | 기존 Preview 싱크/슬롯만 사용한다. Tracker가 전투·진영 턴·결과 순번·선택 개체를 키로 관리하고 요청 번호/슬롯·응답의 전투/턴/개체를 확인한다. 다른 유닛의 AP 사용을 포함한 새 결과 게시 뒤 옛 Ready를 버린다 |
| `CombatActionAvailability.CanMove/CanAttack`, 실패 사유 | 현재 Ready의 공격 허용을 버튼/키가 같이 읽는다. 이동 불가 Ready는 실제 시도에서 안내한다. Unknown 이동은 기존 Core 범위·상세 경로 승인 절차를 유지한다. 보관한 입력을 늦은 응답으로 자동 실행하지 않는다 |
| `CharacterData.ActionUse`와 완료 표시 Buffer | 기존 비용 칸에 행동 사용 상태를 붙인다. AP/비용/행동 사용/병종 이동 한도를 View에서 계산하지 않으며 기존 Core 이동/공격 기대 문맥 검사는 유지한다 |
| 진영 턴/`CompletedPlayerTurnsThisNight`·국면/ControllerPhase | 무선택 수동 EndPlayerTurn과 공용 AP를 유지한다. AP=0 자동 종료는 없다. 밤 UI는 완료 아군 턴 수와 현재 진영을 함께 표시하며 적 응답 8/8만으로 밤 종료를 판단하지 않는다 |
| 특성 Snapshot의 `IsAvailable`·전체/부분 불가 사유 | 공개된 상태만 설명에 추가하고 정적 TraitKey로 현재 활성 여부를 단정하지 않는다. DeferredUnitTurnTiming/DeferredPriorityComponent 등 보류 효과의 지원 재개는 Core/기획 후속 판단이다 |
| 플레이어 Move 결과와 표시 확인 순번 | MoveOnly의 수동 종료/Deployment 재진입은 회복 입구일 뿐 실습 통과가 아니다. 수락된 Move의 실제 진입 칸과 결과 표시 완료로만 기존 목표를 판정한다 |

- 소유/순서: Factory가 Tracker 하나를 만들고 SceneInput이 정지/정리한다. 전경이 우선하며 안정 시 가용성→이동 범위→호버다. Pending 선점 뒤 재조회와 현재 Ready 캐시를 구별하고 자기 요청 번호만 정리한다. 새 결과 관측 때 무효화하고 결과 확인 완료 뒤 현재 키로 조회한다. 이 소비는 기존 결과 순번·Snapshot 게시 계약에 의존한다.
- 천명님 변경 줄이 포함된 접점/기존 시험도 사용자 Q-T8 범위로만 맞췄다. `CombatUnitInfoTests`의 동일 필드 선택/비용 상태, `CombatInputPolicyTests`의 무선택 종료, 호진님 공격 Drag 고정물의 실제 Ready 준비가 해당한다. UI 안내 문자열 6개와 폐지 복귀 기대는 계획 §5의 별도 승인 범위다. 기존 공격 전투/턴 번호·이동 기대 문맥·결과 확인 계약을 약화하지 않았다. 자세한 시험 변경 목록은 계획 §5·§7과 각 작업자 공유 메모를 따른다.
- 추가 Core 지원이 필요한 경우: 항상 이동 불가능한 실습 데이터의 처리와 보류 특성 효과 지원은 별도 요구 확정이 필요하다. 이번에는 새 Core 계산/지원 API를 요청하거나 Jobs/Store/기획 데이터를 수정하지 않았다. 기존 §29 등 예전 요청의 해결 여부도 변경하지 않는다.
- **컴파일·기존 EditMode·Unity 기능 인수는 미확인**이다. V4-0의 사용자 정상 확인은 a 변경 전 baseline에만 해당한다. W13(a) 1개·W14(d/e) 3개·W15(f/g) 1개·W16(h) 1개의 정적 검토를 마쳤고 W14의 P2 한 건은 수정·재검토했다. V4-n M01~M14 확인 전에는 기능 인수 완료가 아니다. 커밋·push·외부 발송은 하지 않았다.

## 39. 전투 View 5라운드 — 소비 구조 정리 (2026-10-26, 미전달)

- 선택 원본을 UI에서 Display 소유 CombatSelectionState로 분리하고 정보/강조/입력이 같은 Reader를 읽도록 했다. Display는 완료 표시마다 불변 CharacterData 참조 목록을 한 번 고정해 선택/UI가 공유한다. Buffer의 살아 있는 목록 래퍼와 표시 시점을 구분하기 위한 View 캐시이며, Core 상태나 규칙의 새 저장소가 아니다. 사망/필드 이탈/국면 변경 시 표시 데이터 기준으로 선택을 정리한다.
- 입력 사실 조합은 CombatInputFactsReader로 옮겼다. 매번 현재 연결/기존 ReadSink·진행 관측을 읽고 View Facts 명칭만 ActiveActorId→SelectedActorId로 정정했다. Core ActorId/진영 턴 의미는 유지한다. 빈 범위 관측은 먼저 읽은 선택 ID를 값으로 받아 Facts를 역호출하지 않는다.
- Preview/명령 슬롯·요청 번호/전투·턴·결과 키·P/S/처리 순서/기존 이동 기대 문맥·가용성 응답 의미는 유지했다. 일반 Path도 Facts의 같은 선택을 읽는다. 공용 AP/비용/사용/효과를 View가 계산하지 않는다. typed 확인창/전체 운영 조립과 시험용 부분 조립만 분리했으며 Core/Buffer 파일·공개 API·데이터/씬·Jobs/Store 수정은 없다. using 오류 수정도 View Reader 파일 안이다.
- **추가 Core 요청 없음.** 기존 보류 효과 지원/항상 이동 불가능한 실습 데이터 같은 외부 후속과 §29 등 과거 요청의 해결 상태를 이 작업으로 바꾸지 않는다. 수락된 플레이어 Move의 실제 진입 칸·결과 표시 완료 조건 및 Run 종료 목적지 정책도 유지한다.
- 4라운드 V4-n은 사용자 “컴파일·기존 시험·M01~M14 모두 정상”, 5라운드 a/b/c는 각각 “컴파일·기존 시험·Unity 확인 모두 정상” 보고로 인수했다. W17~19 정적 검토와 d 구조 통합 인수도 마쳤다. 직접 Unity/시험 실행·개별 오류 주입 로그와는 구분하며 [5라운드 계획 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 근거/공백을 기록했다. 문서 준비만 했고 외부 발송·커밋/push는 하지 않았다.

## 2026-10-26 — Phase A retired category and trait contracts

The user explicitly authorized Phase A of the [category/trait rework plan](../plans/2026-10-26-category-and-unit-trait-rework-implementation-plan.md). This is a local owner handoff record; no external message was sent.

- Removed `IHeavyProtectionReadSink`, `HeavyProtectionAreaSnapshot`, `HeavyProtectionQueryService`, the Core group accessor/key and its CombatSystemFactory registration. All in-repository consumers and mocks are migrated; there is no empty replacement service.
- `CategorySnapshot` now exposes category identity only. Removed Rider refund state and Heavy protection metadata/factories. Construct every category with `CreateStateless`; ordinary category identity, matchup, movement/cost/growth and Heavy knockback immunity remain.
- Removed protection offsets from UnitData/UnitSpec/UnitDefinition and protection percentage from CategoryConfigData/SO. Call sites preserve remaining optional values explicitly, including kill rewards. The development TemporaryCoreDatabase is also a consumer and is migrated.
- Removed Frenzy authoring/Data/Spec, registry/codec/status/runtime, victim priority component and wave event. `CharacterDebuffState` now contains Intimidation and defense reduction; callers preserve defense reduction by its named argument. Retired trait ID 120, wave event ID 30, category-rule IDs 1/4 and category-activation IDs 10/21 remain reserved.
- Cowboy temporarily has no base trait at this authorized development checkpoint. Shotgun retains DistanceDamageScale. Hunter's Mark, Pull, their View integration and gameplay decision gates belong to later phases.

Final evidence, case-level test disposition, consumer traces and acceptance limits are recorded in the plan's Phase A execution checkpoint. The user's latest verification scope is the complete Core and CoreIntegration assemblies; Framework/View runtime acceptance is not claimed.

## 2026-10-26 — Phase B Hunter's Mark contracts

The user explicitly authorized Phase B of the [category/trait rework plan](../plans/2026-10-26-category-and-unit-trait-rework-implementation-plan.md) and separately confirmed G-M as design sections 6.2–6.3. This is a local handoff record; no external message was sent. It supersedes the preceding Phase A checkpoint's Shotgun content state only.

- New trait key `HuntersMark = 190`; its authoring/Data/Spec/definition/status carry `BonusDamagePercent`, default 10 in authoring. Shotgun's actual base-trait assignment now uses it; remaining stats, category and targeting are preserved. DistanceDamageScale remains registered. Cowboy remains traitless until its separately authorized next phase.
- `CharacterDebuffState.HuntersMark` is the sole active target slot: immutable source entity, source owner and captured percentage. Source death/removal does not clear it; victim death/battle disposal does. Existing equality, fact copies, independent status writers and cleanup preserve these boundaries. No Buffer, service, Job or mutable View mark store was added.
- `CombatHuntersMarkEvent` uses new event kind 39 and exposes `TargetId`, `Expected`, `Final`; a null final means consumed. No-op changes emit no event. Existing retired serialized numbers remain reserved. Core emits consumption before the qualifying hit's health application and a new mark after actual health application to a surviving eligible target.
- Existing ordinary, queued/multi-hit, KillChain and target follow-up owners use the same feature hooks; `HuntersMarkTraitRule` owns eligibility, non-stacking and percentage math. The existing transaction writer remains the only installer and validates the full mark value with the expected character facts.
- Combat consumers read `HuntersMarkTraitStatusSnapshot.BonusDamagePercent` in the existing trait statuses. Owned consumers receive `OwnedCharacterStatsSnapshot.HuntersMarkDescription`, passed through existing UnitCard and DeckEdit model/formatting paths. These are configuration snapshots, not copies of an active target mark. View does not read InGame data or calculate bonus damage.
- The visual metadata entry is registered under the same trait key; icon art is deliberately unassigned. Existing event routing skips the new unsupported event until D's dedicated in-world mark presentation is implemented, preserving normal result finalization. Indicator/animation/manual scene acceptance remains pending, not included in this delivery.

Verification: complete **Core 3475/3475 + CoreIntegration 71/71 = 3546/3546**, zero failures/skips/inconclusive, approved Unity6000.0.73f1 EditMode process exit 0. Actual asset deserialization, 10/25 authoring to allied damage and card/deck-model text, and atomic publication/preview/retry checks passed. One independent read-only review found no Critical/Important/Minor findings. The user's two-assembly scope excludes Framework execution; no manual View or final art acceptance is claimed. Full evidence, actual caller boundaries, preserved-test mapping and limitations are in the plan's Phase B checkpoint.

## 2026-10-26 — Phase C Pull contracts (local; not sent)

The user authorized Phase C and confirmed the cost/immunity/terrain rules, with **obstacles excluded from Pull**. Inner-range obstacle/structure attacks retain existing behavior; the Pull extension never makes them eligible and never moves their footprint. Phase D is not implemented.

- Trait key200: `PullTraitStatusSnapshot.ExtraRangeCells` is the configured extension. Production Cowboy has base range1 and extension1, Rider identity and unchanged other stats. Its targeting uses existing method6 (CardinalLine/Single/Unrestricted/None). Pull visual metadata is registered; final icon art is unassigned.
- `AttackRangeResult.BasicAttackCells` gives basic attack cells; `RangeCells` gives total selection reach. Use `AttackAims` for legal choices; an inner friendly unit, blocked path, explicit forced-movement immunity or outer obstacle is not a valid attack. Heavy's knockback-only immunity does not by itself block Pull.
- `AttackResult.Pull` and `AttackPreview.Pull` are nullable `AttackPullSnapshot` references. They expose TargetId, StartAnchor, FinalAnchor and immutable actual Path. A shortened path from terrain death is authoritative; do not animate toward an unreached intended endpoint. Original TargetCell/PrimaryCell and TargetOccupiedCells remain the input facts, while AffectedCells/direct delivery use arrival positions.
- `CombatPullEvent` (kind40) supplies Movement and is ordered before the movement's hazard events and later attack/hit events. There is no duplicate generic Moved event for those same steps. No obstacle Pull event exists. Existing unsupported-event routing will need the D-owned playback connection.
- A friendly Pull consumes the attacker's normal attack cost/action once and leaves the victim's voluntary action intact. It produces no direct hit, shield consumption, mark consumption or hostile-hit effects. Terrain hazards still apply. An accepted terrain-terminal Pull can also have zero direct Hits. Target previews remain present; `IsDirectHit=false` distinguishes movement/environment-only entries and SecondaryDamage includes pre-hit environmental damage. Use final HP/Killed facts for falls instead of inferring damage from death.
- Pre-hit HP recovery and position normalization are accounted separately from attack damage. Pull + PiercingFalloff is an unsupported profile combination and fails during configuration; ordinary piercing/obstacle attacks are unchanged.
- Normal rejection reasons added: PullPathBlocked40, PullImmune41, PullNoMovement42. Configuration mismatch/unsupported targeting is an error; do not supply a static range fallback in View.

Fresh required verification: Core3534/3534 and CoreIntegration85/85, total3619/3619, exit0 and no skips. Tests include actual Buffer installation, stale position/mark atomic rejection, preview purity/hidden hazards and retry idempotence. One independent source review found three Important boundary issues; all were reproduced by seven new tests, fixed in the existing owners, and verified by the complete final run. No Critical/Minor findings or second review. Framework tests, D's UI/playback/art and manual scenes are outside this verification. See the implementation plan's Phase C checkpoint for evidence and final review disposition.

## 40. 천명님 `e413008c` 병합 정리 (2026-10-26, 사용자 지시, 커밋 전, 미전달)

사용자 지시로 HEAD `7690a5fd`에 `origin/feature/Cheon/Core/NewCombatSytem`(`e413008c`, 앞의 `c816c46d`·`4bd6b574` 포함)를 `--no-commit --no-ff`로 병합했다. Core 변경은 그대로 인수하고, `4bd6b574`가 10-23 시점 옛 View 위에서 고친 Phase D(끌어오기·사냥꾼의 표식 소비)는 우리 1~5라운드 구조로 옮겼다. 커밋·push는 사용자가 한다.

- 충돌 14개를 정리했다. 옛 경로표 `CombatEventRoutes.cs`는 지운 채로 두고, 끌어오기·표식 사건은 사건 형식 표 `CombatEventTypeTable`에 줄 둘로 넣었다. 끌어오기는 새 처리기 칸 `Pull`, 표식은 사실 칸이며 주체는 각각 `Movement.TargetId`·`TargetId`다.
- 처리기 표 `CombatEventHandlerTable.Create`의 `pull` 인자는 선택 인자에서 필수 인자로 바꿨다(3라운드 "사건 Presenter 인자 모두 필수" 규칙). 우리 `CombatEventHandlerTableTests`는 호출만 맞췄다.
- 강조 층 `PullRange`·`PullDestination`은 우리 층 표 `Rows`에 두 줄로 넣었다. 비트는 `1 << 11`·`1 << 12`다. 천명님 판의 `1 << 10`이 우리 `AllyHoverRange`와 겹쳤기 때문이다. 색·순서는 천명님 판과 같다. GridOutline 채움은 천명님 판처럼 넣지 않았다.
- `CombatAttackTarget.DeliveryCell`은 우리 공격 종류 전략 호출 다섯 곳(근접·직선 코드 모션, 직선 발사, 곡사 코드 모션·발사)과 조준 방향·궤적 미리보기에 적용했다. 궤적은 이동만 하는 끌어오기면 지운다.
- HEAD(우리 쪽)에서 이미 걷은 공격 비용 미리보기 호출(`ShowAttackCostPreview`·`ClearAffectedCells`의 `ClearCostPreview`)은 되살리지 않았다. 카드 특성 설명은 우리 "사용 불가" 표시와 천명님 설명 분기를 함께 유지한다.
- Core·Buffer 파일의 별도 수정은 없다. 이 문서 §5.51 번호가 우리 10-23 절과 천명님 Phase D 절에 겹친다(내용은 둘 다 유지).
- **미해결:** ① 새 Core에서 `PiercingShot`이 null이라 `CombatBallistaStyle.FireShot`이 예외를 던진다(Ballista 공격 정지, 별도 설계).
- **천명님 시험 변경(사용자 지시 "확인 내용 그대로 두고 생성·RouteOf 호출만"):** `CoreIntegration/PullAndMarkPresentationTests.cs`의 단언은 그대로다. 옛 생성자 `new CombatSceneEventPresenter(_units, .1f)`는 지금 생성자를 부르는 `CreatePresenter`로 바꿨다. 값은 옛 기본값과 같다(걸음 0.1초, 소리·카메라 연출·이펙트 없음, 투사체 부모 = 유닛 루트, 필드 창구는 아무것도 안 함). 오디오는 초기화 전 `AudioSystem`이다. 시험 어셈블리가 FMODPlus를 참조하지 않아 가짜를 만들 수 없고, 초기화 전에는 `Play`·`SetPreset`이 소리 없이 false를 돌려준다. 소리 프리셋은 운영 DB에서 한 번 읽는다. `CombatEventRoutes.RouteOf` 두 곳은 `CombatEventTypeTable.RouteOf`로 바꿨다. asmdef는 바꾸지 않았다.
- **우리 시험 기대값(사용자 지시):** `CombatEventRoutesTests` 형식 목록에 끌어오기·표식을 더했다(20 → 22). `FieldOutlineRendererTests` 테두리 손잡이 수는 층 둘이 늘어 :84·:593은 13 → 15, :312는 26 → 30, :575는 14 → 16이다.
- Unity 컴파일은 문제 없음(사용자 확인)이다. EditMode 시험·재생은 미확인이다. 정적 검토(새 에이전트 하나)는 높음 1건(작업 트리 변경 일부가 스테이지에 빠짐, 반영함)·낮음 3건(끌어오기 시작 때 대상 레인 겹침 예외 가능성, 표식 아이콘 null 검사, 들여쓰기)을 냈다. 낮음은 천명님 판과 같은 의미라 고치지 않았다. 시험 조정 검토(새 에이전트 하나)는 높음·중간 0, 낮음 3(SetUp이 운영 DB 생성에 기댐, 시험 창구의 가시·오브젝트 몸 조회는 예외, 초기화 전 오디오는 다른 멤버를 부르면 예외 — 지금 시험 경로는 닿지 않음)이다.

## 41. Ballista 공격을 곡사와 같게 — 충격 볼트 인계 §5 처리 (2026-10-26, 사용자 지시, 미전달)

천명님 [충격 볼트 인계](2026-10-26-ballista-impact-bolt-core-to-view-handoff.md) §5 중 2·4항을 View에서 처리했다. 계획은 [A1 계획](../plans/2026-10-26-combat-ballista-cannon-attack-plan.md)이다. Core·Buffer 파일은 바꾸지 않았고 Core 요청도 없다.

- **2항(같은 타격 칸으로 곡선 비행):** Ballista 외형 설정 `CombatAttack_Ballista.asset`의 종류를 Ballista(4)에서 곡사(3)로 바꿨다. 이제 언데드 대포와 같은 클립 곡사 길로 쏜다. 클립 이벤트 타이밍에 클립 포구에서 곡선 한 발이 나가 `CombatAttackTarget.DeliveryCell`(`Pull?.FinalAnchor ?? PrimaryCell`) 중심에 떨어진다. 궤적 미리보기도 곡선이며 같은 칸을 가리킨다. 출발점은 유닛 위치다(사용자 결정, 곡사 무기가 없는 클립 유닛).
- **4항(착탄 경계에서 피해·강제 이동 해제):** 착탄 순간 이 공격의 대상을 모두 해제한다(관통 대상별 해제가 아님). 충격 볼트 밀치기는 `PiercingShot`이 null이면 이동 후속을 모두 히트 대상으로 보는 기존 넉백 길로 착탄 뒤 재생된다.
- **3항:** 화살 프리팹·소리·클립은 그대로다. 착탄 이펙트는 외형 설정 `impactEffect` 값이다. 옛 Ballista 전용 연출(관통·시위·재장전)은 없다. 화살은 곡선 접선을 따르지 않고 곡사 회전 값으로 돈다(재생을 보고 조정).
- **1항:** 특성 210 외형 `TraitVisual_ImpactBolt`(이름·설명, 아이콘 없음)를 만들어 표에 등록했다(사용자 지시). 설명에는 거리 숫자를 넣지 않았다. 설정 거리를 Core 값으로 보이는 카드 설명 분기(대응 정리 B2)는 아직이다.
- **5항:** 사용자가 재생으로 작동을 확인했다. 아군은 피해만 받고 밀리지 않는데, 이는 Core 규칙(적대 유닛만 밀림)대로다.
- 우리 시험 `CombatBallistaAssetTests`의 외형 종류 기대값을 곡사로 바꿨다(사용자 허락). 천명님 시험은 바꾸지 않았다.

## 42. 전투 스테이지 편집 도구 SO 모드가 읽는 Core (2026-10-27, 같은 날 병합 대응 A2 추가, 2026-10-28 아군 배치 구역 대응 §42.4, Core 파일 변경 없음, 요청 없음, 미전달)

민형님 도구 **ProjecL → 전투 스테이지 편집**에 SO 모드를 더했습니다. 게임의 전투 스테이지 SO를 열고 고치고 만드는 에디터 전용 모드입니다(설계 [so-mode](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md)·[추가](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md), 기획자용 [사용법](../../../Assets/ProjectL/Script/Tool/SO모드%20사용법.md)). Core 파일은 바꾸지 않았습니다. 도구 어셈블리(`ProjectL.Tool.Editor`, 에디터 전용)가 `ProjectL.Core.Contracts`·`ProjectL.Core.Domain`을 참조합니다.

### 42.1 도구가 부르는 Core 공개 API (이름·서명이 바뀌면 도구도 바뀌어야 함)

| API | 쓰는 곳 |
|---|---|
| `Domain` `AppointShapeCalculator.CreateBaseMask`·`CreateDirectionMask`·`CreateSplashOffsets` | 적 공격 범위 미리보기(참고용) `Tool/Editor/CombatStageSoRange.cs` |
| `Contracts.Combat` `ObstacleContentProfile.KeyFor`, `ObstacleContentKind` | 장애물 정의 → 외형 키(한국어 이름·3D 외형) `CombatStageSoAssets.VisualId` |
| `Contracts.Common` `UnitRole`, `UnitOwnerEligibility` | 적 유닛 목록 거르기 `CombatStageSoAssets.CanBeEnemy` |
| `Contracts.Common` `PlayerDeploymentZone.Of`·`Rows`·`Contains`, `CellCoord`, `Owner` / `Domain.Placement` `DeploymentZonePolicy.CanDeploy(Owner.Enemy, …)` | 아군 배치 구역 해석 `CombatStageSoAssets.ZoneOf` → 도구 값 `CombatStageSoZone`. 맵 색·구역 이름·구역 붓·참고 수·경고 ①이 이 값 하나를 씁니다(2026-10-28 추가, §42.4) |
| `Contracts.StageMap` `ChapterStageDistributionRule`·`ChapterStagePool`·`ChapterGraphSpec`(생성자 검사), `ChapterStageKind`·`ChapterGraphConnection` | 표 등록·빼기와 등록된 스테이지 저장 때 맵 생성 규칙 판정 `Tool/Editor/CombatStageSoChapter.cs` (병합 대응 A2, 2026-10-27 추가). 도구가 표 상태로 후보 묶음과 spec을 실제로 만들어 보고, 생성자 예외 문구를 그대로 보입니다 |

### 42.2 도구에 있는 Core 규칙 사본 (규칙이 바뀌면 도구도 맞춰야 함)

Core를 직접 부를 수 없는 규칙은 조건만 옮겼고, 원본 자리를 주석으로 달았습니다.

| Core 원본 | 도구 사본 | 쓰임 |
|---|---|---|
| `UnitEligibilityRules.Require(definition, Owner.Enemy, false)`(일반 역할, 아군 전용 아님) | `CombatStageSoAssets.CanBeEnemy` | 정의 목록을 적 유닛 / 아군 유닛으로 나눔. 아군은 고를 수 없음 |
| `BattleDefinitionAdapter.cs:90-92` 아군 구역 대체 규칙(칸 목록이 있으면 `Of(칸)`, 비면 `Rows(필드 아군 배치 깊이)`) | `CombatStageSoAssets.ZoneOf`의 갈래 한 줄 | 도구에 남은 유일한 구역 사본. 판정 자체는 Core 함수를 부릅니다(§42.1) |
| `PlayerDeploymentZone.FitsBoard`(줄 수 ≤ 판 높이) | 아군 줄 수 슬라이더 범위(1~세로), `CombatStageSoCreation.CreateField` 검사 | 범위 |
| `EnemySpawnCalculator` 출현 후보(판 − 아군 구역 − 구조물·장애물 예약 − 벽·낭떠러지) + `EnvironmentTerrainRules.BlocksVoluntaryEntry`(벽·낭떠러지) | '아군 배치 구역' 참고 수(`DrawSoPlayerZone`), `CombatStageSoAssets.BlocksEntry`(지형 정의 `kind` 이름 `Wall`·`Cliff`) | 참고 수, 경고 ② 출현 수 > 칸, ③ 아군 놓을 칸 없음. 공개 대상·숨김 호환 벽·무작위 시작 지형은 빼지 않는 근사라 "약"으로 보입니다 |
| `BattleDefinitionAdapter.cs:48-51`(고정 적 유닛이 있는데 적 로드아웃 키가 있으면 예외) | 경고 ④ 조건 | 경고 |

옛 사본 `CombatStageSoField.MaxDepthBeside`(두 깊이 합 < 판 높이)와 `EnemyStartRow`(위 깊이 줄 = 적 진영)는 Core `5a1bfbe1f`(적 구역 삭제)에 맞추며 지웠습니다(2026-10-28, AZ-a).
| `StageGeneratorSystem.PrepareGraph`(역할 Regular = 일반, 그 밖 = 보스, 맵마다 보스 하나) + `StageTable.GetStages`(그 맵 키 Root가 없으면 예외) | `CombatStageSoAssets.IsBossRole`, `CombatStageSoChapter.Check` | 맵 생성 규칙 판정(A2) |
| `StageGeneratorSystem.KindOf`(상세 Data 형식 → 종류) | `CombatStageSoAssets.KindOfDetail`(상세 SO 형식 → 종류, 상세 SO 넷과 상세 Data 넷이 하나씩 짝) | 같음 |
| `StageGeneratorSystem.ToConnection`(맵 SO 연결 방식 → Core 연결 방식) | `CombatStageSoChapter.Check`(이름으로 대응) | 같음 |
| `StageMapDataSO.GetData`(출현 칸 넷 → 종류) | `CombatStageSoAssets.DistributionFields` | 같음 |

전투 진입 때만 Core가 판정하는 것(아군 구역 안 고정 적 배치, 고정 적 + 적 로드아웃, 층 사이 겹침, 적 로드아웃 출현 수 > 빈 칸, 아군 정의, 표 미등록 정의)은 도구가 저장을 막지 않습니다. 아는 것만 경고하고, 기획자에게 Play From Boot PlacedStage로 확인하라고 안내합니다.

맵 생성 규칙(A2)은 도구가 막습니다. 이번 변경이 맵을 깨뜨리면 막고, 보스가 둘이 되면 기존 보스를 표에서 빼고 진행할지 묻고, 전부터 깨진 맵이면 경고 뒤 진행합니다([SO 모드 설계 §10](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#10-병합-대응-a1a2-mg1mg9)). 맵 생성 규칙을 생성자 밖(생성기 안의 예외 등)에 더하시면 도구는 그 규칙을 잡지 못합니다. 지금 생성기 안의 예외 셋(최소 배치 자리 어긋남·최소 개수 미달·고정 노드 묶음 없음)은 spec 검사 뒤의 방어용이거나 개발 진입 고정 노드 전용이라 등록과 상관없습니다.

### 42.2a FU-a 저장 전 배치 검사 보완 (2026-10-31, 보고만·미전달)

- 원본: `CellEnvironmentQuery.ValidateInitialPlacement`의 낭떠러지 초기 배치 금지와 `EnvironmentPlacementRules.Evaluate`의 WallOverlap. 낭떠러지 위 고정 적도 허용되지 않습니다.
- 도구 사본: `CombatStageSoAssets.CheckFixedPlacements` → `CombatStageSoPlacementChecks.Check`. 고정 적·적 구조물·참조가 있는 본부의 회전된 점유 칸 전체를 벽/낭떠러지 칸과 비교하고 이름·좌표를 오류로 반환합니다. 호환 벽/지형도 포함하며 공개 대상과 무관합니다.
- 검사·저장·등록·생성이 같은 복제본 검사 경로를 사용합니다. 배치 비우기 생성만 검사 전 호환 배열도 비우고, 일반 저장·배치 복제는 보존합니다. 무작위 출현·미사용 메인 캐릭터 위치·나머지 전투 준비 규칙을 새로 판정하지 않습니다.
- Core·DB SO·공개 API·asmdef 변경이나 신규 Core 요청은 없습니다. 위 두 규칙이 바뀌면 도구 사본도 대조해야 합니다. 컴파일·Unity 확인은 미완료이며 저장 검사 통과를 전투 진입 보장으로 안내하지 않습니다.

### 42.3 보고만 (요청 아님)

1. **메인 캐릭터 자리 필드가 쓰이지 않습니다.**
   - 천명님 `4ae6a9b8`(10-17) 뒤로 메인 캐릭터는 첫 손패 0번 슬롯으로 주어집니다. 그래서 `CombatStageData.mainCharacterAnchor`·`mainCharacterRotation` → `BattleInitialization.MainAnchor`·`MainRotation`을 읽는 코드가 없습니다. `MainCharacterEntrySpec` 형식도 쓰는 곳이 없습니다.
   - SO 툴팁 "Player 배치 구역(맵 아래쪽) 안이어야 합니다. 플레이어 본부는 배치 단계에서 플레이어가 놓습니다."는 낡았습니다.
   - 도구는 이 값을 맵에 그리지 않고 읽은 그대로 보존합니다. 필드·형식·툴팁을 정리할지는 Core·DB 쪽에서 정해 주세요.
2. ~~**배치 진영 계산이 두 벌입니다.**~~ 해소(천명님 `5a1bfbe1f`): 적 출현 후보도 `DeploymentZonePolicy`를 부릅니다.
3. ~~**칸 단위 진영은 보류입니다.**~~ 해소: §43 Core 응답으로 아군 배치 구역 칸 단위가 들어왔고, 도구는 §42.4로 맞췄습니다. 옛 [진영 칸 설계 초안](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#7-보류-배치-진영-칸-단위-dz1dz6)(DZ1~DZ6)은 SO 모드 설계 §11이 대신합니다.

### 42.4 아군 배치 구역 대응 (2026-10-28, AZ-a·AZ-b, Core 파일 변경 없음, 요청 없음)

천명님 `5a1bfbe1f`(적 배치 구역 삭제, 아군 구역 칸 목록)에 도구를 맞췄습니다. 원본은 [SO 모드 설계 §11](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#11-아군-배치-구역-칸-az1az9)(AZ1~AZ9)과 [계획 §7](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#7-아군-배치-구역-칸-계획-az-aaz-c)입니다.

- **도구가 이름으로 읽고 쓰는 칸:** `CombatStageData.playerDeploymentCells`(`CellInput[]`). 저장은 y → x로 정렬해 씁니다(`PlayerDeploymentZone.Of`의 순서와 같음). 필드 이름이 바뀌면 `CombatStageSoAssets`의 상수 하나를 고칩니다. 판 밖·중복의 최종 판정은 그대로 SO `GetData()`(`ValidatePlayerDeploymentCells`)입니다.
- **도구가 하는 일:** 맵을 "아군 구역 / 적 배치 가능" 둘로 칠하고, '아군 구역' 도구로 칸 목록을 칠하거나 지웁니다. 칸 목록이 빈 상태(줄 수 대체)에서 처음 편집하면 보이던 줄 칸을 칸 목록으로 옮긴 뒤 편집하고, 마지막 칸 지우기는 막습니다(빈 목록 = 줄 수로 돌아감). 적 줄 수 슬라이더는 걷었고, 필드 `enemyDeploymentDepth`는 도구가 쓰지 않습니다(새 필드는 틀 값 그대로).
- **경고 넷(저장은 막지 않음):** ① 고정 적이 아군 구역 안(`CanDeploy` 직접 호출) ② 출현 수 > 적이 놓일 칸 ③ 아군 놓을 칸 0 ④ 고정 적 + 적 로드아웃. ②③은 §42.2 근사 사본입니다.
- **PZ-d 때 함께 고칠 도구 줄:** [아군 배치 구역 계획 §8.6](../plans/2026-10-27-combat-player-deployment-zone-plan.md)이 "실제 삭제는 PZ-d 때 SO 모드 도구와 함께"라고 적었습니다. 도구는 `mainCharacterAnchor`·`mainCharacterRotation`을 이름으로 `Require`합니다(`CombatStageSoAssets` 읽기·쓰기 각 한 줄). 그리고 **회전 열거 이름(`names.rotation`)을 `mainCharacterRotation`에서 읽습니다**(`FillNames`). 그 필드를 지우면 도구가 스테이지를 열 때 예외가 나므로, 지우기 전에 알려 주시면 도구 줄을 같이 고치겠습니다(회전 이름은 `enemyHeadquartersRotation`에서 읽도록 바꿀 수 있음).
- **게임 화면:** 칸 구역 스테이지에서 아군 색·놓을 수 있는 칸이 안 나오는 것(§43 "View에 남은 일")은 전투 View 담당으로 남깁니다. 도구는 칸 목록이 있는 스테이지에 그 안내 줄을 띄웁니다.

<a id="deployment-zone-cells-request"></a>

## 43. 배치 구역 칸 단위 지정 요청 (2026-10-27, View → Core, Core가 다른 결정으로 구현 — 아래 Core 응답)

배치 구역을 "아군 = 아래 N줄, 적 = 위 N줄"이 아니라 칸 단위로 자유롭게 정할 수 있게 해 달라는 요청이다. **Core·Buffer 계약만 다룬다.** 칸을 저작하는 자리·편집 도구·View 표시는 넣지 않았다(사용자 결정으로 View 계획 폐기). **이번에는 문서만 썼다. Core·Buffer 코드와 시험은 바꾸지 않았다.** 요청 원본은 [배치 구역 칸 단위 지정 — Core 요청](2026-10-27-combat-deployment-zone-cells-core-request.md)이다.

- 계약: 깊이 두 값 → 진영별 칸 집합. `StageConfig`·`CombatPreparation`·`CombatBattleFacts`·Buffer `CombatStateData`가 구역 값을 실어 나르고, `DeploymentZonePolicy`가 칸으로 판정한다. 적 출현 후보도 같은 구역에서 고른다. 규칙은 겹침 허용·떨어진 조각 허용(사용자 결정)이다.
- 런타임 판정(배치 미리보기·배치 명령·전투 시작 적 재배치)은 모두 `CombatPlacementCalculator.Zones()` 하나를 지난다.
- 첫 단계는 어댑터가 깊이로 구역을 만들어 동작이 같다. 깊이 생성자를 남기면 시험 133곳은 그대로이고, 깊이 속성을 읽는 천명님 시험 10곳만 호출을 맞춘다.
- 확인 요청 Q1~Q5: 검사 규칙, 작은 적 구역의 재배치 거절, 깊이 입구 유지, 안내 문서 갱신, 일정. §42의 진영 칸 설계 초안(보류)과 다른 점도 요청 문서 §6에 적었다.

**Core 응답 (2026-10-27, 천명, 구현 완료, Core·CoreIntegration 시험 통과, 미전달)**

- **결정이 바뀌었다(사용자): 적 배치 구역을 없애고 아군 배치 구역만 칸 단위로 둔다.**
  - 적(고정 배치·본부·가중치 출현·전투 시작 재배치)은 판 안이면서 아군 구역이 아닌 칸이면 된다.
  - 그래서 요청의 "진영별 칸 집합"은 아군 칸 집합 하나가 됐다.
  - 원본은 [아군 배치 구역 설계](../specs/2026-10-27-combat-player-deployment-zone-design.md)(PZ1~PZ7)와 [계획](../plans/2026-10-27-combat-player-deployment-zone-plan.md)(실행 기록 §8)이다.
- **계약:** 새 값 `Contracts/Common/PlayerDeploymentZone`이다. 줄 구역 `Rows(n)`과 칸 구역 `Of(칸)` 두 모양이 있다.
  - `StageConfig`·`CombatPreparation`·`CombatBattleFacts`·Buffer `CombatStateData`가 `PlayerZone`을 실어 나른다.
  - 판정 원본은 `DeploymentZonePolicy(PlayerDeploymentZone)` 한 곳이다. 적 출현 후보도 이 판정을 부른다.
- **입력 자리:** `CombatStageData.playerDeploymentCells`(스테이지 SO)다. 비우면 `FieldData.PlayerDeploymentDepth`(아래부터 N줄)로 대신한다. 대체 규칙은 `BattleDefinitionAdapter` 한 곳에 있다. `FieldData`의 적 줄 수는 남지만 Core는 읽지 않는다.
- **요청 Q1~Q5에 대한 답:**
  - Q1 검사 규칙: 아군 구역 1칸 이상·판 안·중복 없음이다. 적 구역이 없어 겹침 문제는 생기지 않는다.
  - Q2 작은 적 구역: 적 구역이 없어 해당 없다. 재배치 거절은 아군 구역 밖이 모두 막혔을 때만 난다.
  - Q3 깊이 입구: 전환 입구로 남겼다. 옛 줄 수 생성자는 아군 줄 수로 줄 구역을 만들고, 적 줄 수는 받기만 한다. 옛 속성은 계산값이다(아군 줄 수 = 줄 구역의 줄 수 또는 0, 적 줄 수 = 늘 0). View·개발 진입·Framework 시험은 그대로 컴파일된다. View가 `PlayerZone`으로 옮기면 지운다.
  - Q4 안내 문서: `guides/unit-movement-attack-core-handoff.md`의 배치 구역 설명을 고쳤다.
  - Q5 일정: Core 쪽에서 구현했다.
- **View에 남은 일:** 구역 색을 칠할 때 Buffer `CombatStateData.PlayerZone.Contains`를 쓰는 것이다. 지금 View는 옛 줄 수를 읽는다. 그래서 칸 구역 스테이지에서는 아군 색과 놓을 수 있는 칸 표시가 나오지 않는다. 적 구역 색은 이미 사라졌다(적 줄 수 = 0).
- **도구 쪽 반영(2026-10-28, SO 모드):** 전투 스테이지 편집 SO 모드가 칸 목록 붓·참고 수·경고로 맞췄다(§42.4). 게임 화면 표시는 위 "View에 남은 일" 그대로 전투 View 담당이다.

**View 대응 (2026-10-28, 용근, 설계 초안만, 코드 변경 없음, 결정 대기)**

- 설계: [아군 배치 구역 View 설계](../specs/2026-10-28-combat-player-zone-view-design.md)(PV1~PV6, Q-PV1~Q-PV5).
  - View 정적 구역은 Buffer `PlayerZone.Contains`로 칠한다.
  - 적 구역 표시를 걷는다.
  - 개발 진입 Fixture는 `StageConfig` 새 생성자에 `Rows(아군 줄 수)`를 넘기고, 적 줄 수 설정을 걷는다.
  - SO 모드 도구(호진님 영역)는 사용자 결정에 따른다.
- PV-a·PV-b가 끝나면 View·개발 진입은 옛 깊이 속성과 줄 수 생성자를 쓰지 않는다. 그때 PZ-d(전환 입구 걷기)를 걸 수 있다고 이 절에 다시 적는다.
- 요청 문서는 "대신함"으로 표시했다.
- **2026-10-28 진행(코드, 컴파일 미확인):**
  - PV-a: View 정적 구역이 `PlayerZone.Contains`를 쓴다.
  - PV-b: 개발 Fixture가 `StageConfig(…, PlayerDeploymentZone.Rows(n), …)`를 쓴다.
  - 그래서 View·개발 진입에는 옛 깊이 속성과 줄 수 생성자를 쓰는 곳이 없다. PZ-d를 걸 수 있다(남는 사용처는 천명님 Core 시험).
- **천명님께 (2026-10-28, Play From Boot Fixture 모드 삭제, 사용자 지시):**
  - Play From Boot에서 개발 고정 정의(Fixture) 모드를 지웠다. 개발 진입의 전투 정의는 Database 하나다.
  - `ViewSystem/Dev/DevBattleInitializationSource`는 게임 실행 경로에서 더는 만들지 않는다. 지금 쓰는 곳은 천명님 시험 `CoreIntegration/CategoryDefinitionMigrationTests.cs:36` 하나다.
  - 그 시험을 위해 클래스는 남겼다(사용자 결정). 시험은 바꾸지 않았다.
  - 시험이 다른 준비 방식으로 옮겨지면 이 클래스를 지워도 된다. 그때는 View 쪽에 알려 주면 된다.
  - 기본값 상수 셋은 외부 사용처가 없어져 private이 됐다. 시험은 기본 생성자만 쓰므로 영향이 없다.


## 44. 적 공용 행동 코스트 10 (2026-10-27, 사용자 Core 수정 허용)

§5 Core 변경 기록의 후속이다. 사용자가 기존 행동 순서를 유지하는 적 공용 코스트 제한의 Core 수정을 명시적으로 허용했고, 최대 10·공격비 부족 시 이동 시도·검증 생략 실행을 확정했다. [설계](../specs/2026-10-27-enemy-shared-actioncost-design.md)와 [Task 계획](../plans/2026-10-27-enemy-shared-actioncost-plan.md)이 원본이다. 외부 발송하지 않았다.

- Phase E1 코드 반영: Contracts/Common/EnemyTurnRules, Contracts/Combat/EnemyActionBudget(신규 .meta 포함), CombatCommandState, ActionFundingKind, ActionCostSettlementSnapshot, CombatActionAdmission, CombatMovementCalculator, CombatTurnCalculator, MovementProjection, BufferSystems/Combat/CombatStateBuffer. Core 경로는 CoreCaculator 아래다.
- 적 턴 시작마다 10 충전, 일반 이동/공격의 기존 가격으로 공유 예산 검사·정산. 플레이어 예산과 독립. EntityId 순서와 공격 실패 후 이동·모두 불가 시 종료는 기존 AI 그대로다. 무료 반응은 Unbudgeted 유지.
- CombatTurnCalculator·CombatStateBuffer에 있던 다른 미커밋 작업은 되돌리지 않고 해당 예산 연결 부분만 수정했다. 기존 플레이어 설계 문서의 무예산 정책은 이번 설계 범위에서 대체하며 해당 파일은 다른 변경이 있어 수정하지 않았다.
- 사용자 요청으로 테스트·빌드·Unity 확인과 기존 무예산 적 테스트 fixture/기대값 갱신은 생략했다. 코드 반영 상태이며 런타임 검증 완료가 아니다. 커밋·push 없음.

## 45. `origin/feature/La/AudioSystem_R` 병합 정리 — 토네이도 × 기차 기믹·약한 지반 (2026-10-27, 사용자 지시 "충돌 나는 코드들 둘다 반영", 커밋 전, 미전달)

HEAD `1ab12e55`(토네이도 응급 처치)에 `origin/feature/La/AudioSystem_R`(`98d1618c`, 용근님 약한 지반·낭떠러지·이웃 모양·기차 해결·적 코스트 등 포함)를 병합하며 충돌 9개를 양쪽 내용을 모두 남기는 쪽으로 정리했다. 커밋·push는 사용자가 한다.

- **`CombatTurnCalculator`(Core):** 진영 끝 두 곳(플레이어 턴 끝·적 국면 끝)에 우리 쪽 토네이도 발동 정산(`TornadoCalculator.ResolveDue`)과 상대 쪽 기차 기믹 정산(`ResolveEndGimmick`)·밤 종료 분리(`EndNight`)·`CompletedControllerTurnCount`를 함께 넣었다. 순서는 **턴 종료 사건 → 토네이도 발동(같은 단계) → 기차 기믹 → (밤 마지막이면) 밤 종료**다. 적 국면 끝은 토네이도로 승패가 나면 지금처럼 적 국면 종료 사건 없이 끝낸다. 토네이도와 기차가 같은 진영 끝에 둘 다 도는 경우의 순서는 병합 때 정한 것이라 천명님·용근님 확인 대상이다.
- **`MovementProjection.Complete` 주석(Core):** 상대 쪽 주석 "턴 끝·적 국면 끝은 이 함수를 지나지 않습니다"가 위 병합으로 틀려져, 턴 끝도 토네이도 정산 때문에 이 함수를 지나며 토네이도가 내려놓은 진입도 같은 약한 지반 규칙으로 무너진다고 고쳤다. 동작 코드는 바꾸지 않았다.
- View 쪽: `TerrainVisualData`는 이웃 모양 묶음(`edgeShapes`)과 토네이도 연출 설정을 둘 다 받는다. `FieldTerrainPresenter`는 풀 키를 상대 쪽 `TerrainLookKey`로, 회전은 외형 선택 결과(`look.Rotation`, 모양 없는 종류는 옛 스테이지 회전과 같음)로 하고 토네이도 외형 목록은 그대로 둔다. `CombatFieldEventPorts`는 `showTerrainChange`를 `showTornado` 바로 뒤에, 토네이도 창구 넷을 그 뒤에 둔다. `CombatCharacterEventHandler`는 추락 연출 값(필수) 뒤에 토네이도 상승 창구 둘(선택)을 받는다.
- 시험은 호출만 맞췄다: 천명님 `CoreIntegration/PullAndMarkPresentationTests.cs` 창구 생성 한 줄, 우리 `CombatViewSystemTests.FieldPorts`·`TornadoAppearanceSchedulingTests` 창구 생성 한 줄씩(`showTerrainChange` 빈 함수 추가).
- 컴파일·EditMode 시험·재생은 미확인이다(전투 View 작업 규칙상 MSBuild를 돌리지 않음).

## 46. 전투 연출 범용 구조 — 천명님 시험 영향 (2026-10-27, Core 파일 변경 없음, 요청 없음, 미전달)

- View DB 공격 표시 SO가 부품 SO(투사체·코드 모션)를 참조하는 꼴로 바뀌어, 천명님 `CoreIntegration/PullAndMarkPresentationTests.cs`의 `EnableAttack`(`:378` `kind`, `:379` `projectilePrefab`)이 없는 칸을 찾아 사례 넷(`Pull_AttackMotionRunsOnlyForDirectHit`·`Pull_PreviewAndAttackTargetUseArrival` 각 둘)이 실행에서 실패한다. 컴파일은 된다.
- 사용자 결정(Q-T1, 2026-10-27 "고치지 말고 천명님께 넘길 메모만"): 시험은 고치지 않았다. 고칠 곳·제안 코드·값 보관 주의는 따로 정리한 [범용 연출 구조 천명님 공유](2026-10-27-combat-presentation-generic-structure-cheon-share.md)에 있다.
- 바뀐 천명님 줄: `CombatAttackPlayback` 옛 `TryBegin`의 `EndSource(e, version);` 한 줄(입구째 삭제, 끝은 조립 실행이 같은 순서로 냄).
- 기록: [범용 구조 계획 §13.7·§13.8](../plans/2026-10-27-combat-presentation-generic-structure-plan.md).

## 47. 전투 걸음 모양 — 바뀐 천명님 끌어오기 줄 (2026-10-28, Core 파일 변경 없음, 요청 없음, 미전달)

- 무엇: 자발 이동이 칸마다 한 번 뛰도록 걸음 레인이 걸음 시간·높이·접촉 시점을 걸음 모양(`CombatStepStyle`)에 묻게 바뀌었다. 설계 [전투 걸음 모양](../specs/2026-10-28-combat-step-motion-style-design.md)(ST1~ST7), 기록 [계획 §5.2](../plans/2026-10-28-combat-step-motion-style-plan.md).
- 바뀐 천명님 줄(`4bd6b574`, `Presentation/Movement/CombatUnitMotionPlayback.cs`):
  - `BeginPull` 맨 앞에 한 줄: 모양을 모양 표의 밀림 모양(`CombatMovementKind.Forced`)으로 정한다.
  - `TickPull`: 한 칸 시간 `_stepSeconds` → 고른 모양의 `Seconds`, 보간 끝 인자 `_stepClimbHop` → 고른 모양.
- 동작: 옮기는 모양은 같다. 밀림 모양은 걷기이고 값이 옛것(`movementStepSeconds`·`stepClimbHop`)과 같다. 끌어오기 경로·사건 처리기 `CombatPullEventHandler`는 그대로다.
- **추가(2026-10-28 사용자 "끌려 올 때 끌려가는 방향 보게 해줘"):** 같은 두 함수에 회전을 더했다. 동작이 바뀐다.
  - `BeginPull` 끝: 끌려가는 방향(시작 칸 → `FinalAnchor`)으로 돈다. 걸음 경로와 같은 회전 함수(`TurnToward`)·회전 시간(`movementTurnSeconds`, 기본 0.1초)이다.
  - `TickPull` 맨 앞: 다 돈 뒤 남은 시간부터 옮긴다(걸음과 같은 순서).
  - `TickPull` 끝: 도착 뒤 원래 방향(사실 회전)으로 돌아온 다음 완료를 알린다(걸음 경로 끝 방향 복귀와 같은 `BeginFacingRestore`). 완료 통지가 회전 시간만큼(기본 최대 0.1초씩 두 번) 늦어진다.
  - 넉백은 바뀌지 않는다(돌지 않음).
- 시험: 천명님 `PullAndMarkPresentationTests`는 고치지 않았다. 값 묶음 생성자는 끝 선택 인자만 늘어 `:414` 줄이 그대로 컴파일되고, 사건 Presenter 생성자는 바뀌지 않았다.
- 부탁: 없다. 브랜치에서 `BeginPull`·`TickPull`을 고치셨다면 병합 때 "한 칸 시간·높이는 레인이 고른 모양에서 읽는다"만 지키면 된다.

## 48. 전투 이펙트 창구 확장 — 바뀐 천명님 시험 두 줄 (2026-10-28, Core 파일 변경 없음, 요청 없음, 미전달)

- 무엇: 사실 사건(오브젝트 발동·장애물 피해/파괴·추락·약한 지반·아이템·토네이도)에 이펙트·흔들림 큐를 데이터로 붙이고, 화약통·지뢰 폭발을 외형 큐로 옮겼다. 설계 [전투 이펙트 창구 확장](../specs/2026-10-28-combat-effect-cue-expansion-design.md)(EC1~EC10), 기록 [계획 §6](../plans/2026-10-28-combat-effect-cue-expansion-plan.md).
- 읽는 Core 사실: 새로 읽는 것은 없다. View 사건 형식 표의 "순간" 칸이 기존 사건 필드를 읽는다(장애물 반응의 종류·반응·`Origin.PhysicalCell`, 오브젝트 발동 원본, 추락, 정적 지형 원본 사건, 아이템 `Activated`).
  - 장애물 반응의 칸은 Core가 늘 장애물 앵커로 채우는 것에 기댄다(`ObstacleReactionCalculator.cs:61` `new CombatDamageOrigin(before.Anchor, …)`). 비면 이펙트 없이 소리만 낸다(옛 환경 폭발은 비면 예외였다).
- 바뀐 천명님 시험 두 줄(`CoreIntegration/PullAndMarkPresentationTests.cs`, 단언 그대로, 계획 P14):
  1. 사건 Presenter 생성자의 새 필수 인자 `cueSources`: 정적 칸 `s_cueSources` 하나와 생성 인자 한 줄(`s_cueSources ??= CombatDisplayTables.From(PresentationDatabase().ViewQuery).CueSources`, 처음 쓸 때 한 번 만듦).
  2. 필드 창구 생성의 둘째 인자 `_ => { }`(지운 환경 폭발 창구) 삭제.
- §47의 "사건 Presenter 생성자는 바뀌지 않았다"는 그 시점 기준이다. 이번에 인자 하나가 늘었다.
- 부탁: 없다. 브랜치에서 이 시험의 Presenter·필드 창구 생성 줄을 고치셨다면 병합 때 위 두 가지만 맞추면 된다.

<a id="trait-activation-request"></a>
## 49. 특성 발동 사실 요청 (2026-10-29, View → Core, Core 파일 변경 없음, 전달함 — Core 답 대기)

특성이 발동한 순간을 View가 "어느 유닛의 어느 특성이, 무엇을 했나"로 받을 수 있게, 모든 특성 발동을 같은 꼴의 사실로 결과 사건 목록에 내 달라는 요청이다. 원본은 [특성 발동 사실 — Core 요청](2026-10-29-combat-trait-activation-core-request.md)이다. **이번에는 문서만 썼다. Core 코드와 시험은 바꾸지 않았다.**

- 배경: 주변 조건 버프·디버프, 공격·피격 시 발동, 이동 시 지형 생성, 이동 경로 주변 공격 같은 특성이 늘 예정이다. View는 특성마다 전용 사건 대신 발동 사실 하나를 (특성 키, 발동 종류)로 받아 연출하려 한다. 걸려 있는 상태는 지금처럼 유닛 사실의 상태 칸으로 받는다(별도 특성 Buffer 없음).
- 지금: 공통 꼴 `CombatEffectsEvent`·`TraitActivationSnapshot`이 있지만 처치 연쇄·추가 공격(2연발·연쇄 폭발·행운의 탄환)만 낸다. 넉백·충격 볼트·끌어오기·표식·위협·도어 브리칭은 특성 키 없이 결과만 나온다. 피해 근원에는 원인 특성이 없다.
- 요청 계약 R1~R7: 결과가 바뀐 모든 발동이 사실을 냄, 결과 사건 바로 앞에 둠, 대상 여럿·칸 여럿·공격 그룹을 담음, 지속 상태는 상태 칸, 전용 사건은 그대로 두고 발동 사실을 함께, 대상 없는 추가 공격과 이동 중 발동한 공격은 공격 사건 없이 방향·지정 칸.
- 같은 날 더함(사용자가 권장안으로): 다섯째 형태 "다른 유닛의 행동 도중 끼어드는 반응"(예: 범위 안 아군이 맞을 때 그 앞으로 가서 대신 맞고 돌아옴). 지금 걸음 사건에는 이동 원인이 없어 View가 스스로 이동·밀림을 추론하고, 공격 사건은 맞는 순간에 완료돼 그 뒤 사건은 늦게 시작한다.
  - R8: 특성이 만든 걸음은 걸음 사건에 이동 원인(스스로/밀림, 원인 특성)을 담는다.
  - R9: 남의 행동 도중 끼어드는 반응 발동은 그 행동 결과 안에 담는다(공격이면 `AttackResult.TraitActivations`). 사건 목록에 또 내지 않는다.
  - R10: 가로막기 사실은 수호자·지켜진 유닛·실제로 맞은 유닛·공격 그룹을 담는다.
- 확인 요청 Q1~Q13(View 쪽 입장은 모두 A, 사용자 확정): 발동 기준, 사건 형식, 발동 종류 값, 대상 여럿, 결과와 잇는 열쇠, 전용 사건 관계, 추가 공격·이동 중 공격 연출 정보, 진행 순서, 이동 원인 자리, 대신 맞기의 실제 이동 여부, 지키는 동안 넉백·사망 기준 칸, 여러 대상·수호자, 서는 자리.
- 원본 문서 §4에 이 계약으로 View가 할 연출 방향을 참고로 적었다. 발동 시점 여섯 갈래(맞는 순간 곁들임·결과 앞 신호·공격 사건 없는 추가 공격·이동 중 발동·남의 행동 중 반응·특성 자기 이동)마다 필요한 Core 사실(R), 대신 맞기 재생 순서, 지금 특성별 Core 변경·View 연출 표(§7), View 몫 목록(§4.6)이다.
- **전달 뒤 보강(2026-10-29, 사용자 결정, 미전달):** R5에 "걸린 상태 공통 목록"(항목 = 상태 종류 키 + 값 목록 + 남은 턴 + 건 유닛)을 더하고 §9 Q14(지금 칸 셋은 계산용으로 두고 목록은 그 값의 보기, View 입장 A)를 더했다. 버프·디버프가 늘 때 Core 형식·사실 칸·View 줄이 함께 늘지 않게 하려는 것이다. View는 TA-e에서 걸린 상태를 상태 외형 표(View DB, 종류 키별 아이콘·이름·설명 틀·구분) + 상태 줄 표로 그리게 바꿨다(지금은 Core 칸 셋을 읽음).
- **같은 작업에서 바꾼 천명님 줄(2026-10-29, TA-e, 미전달):** `FieldUnitPresenter.RenderUnitHud`의 사냥꾼의 표식 HUD 칸(`4bd6b574`, 특성 외형 표의 표식 아이콘·고정 문구)을 걷고 상태 줄 표로 옮겼다. 키 `HuntersMarkDebuff`와 문구("사냥꾼의 표식: 다른 아군의 일반·추가 공격 피해 N% 증가. 공격 시 소모됩니다.")는 같다. 문구 원본은 이제 상태 외형 에셋 `UnitStatusVisual_HuntersMark`다. 천명님 `PullAndMarkPresentationTests`는 `FieldUnitPresenter.Configure`에 상태 줄 표를 넘기지 않아 표식 칸이 나오지 않으므로 표식 단언 두 시험(`MarkApplyConsume_OneIndicator`, `CancelOrSceneExit_ReleasesMotionAndMarkVisuals(false)`)이 실행에서 실패할 것이어서, 사용자 지시("한 줄 고쳐", 2026-10-29)로 `PullAndMarkPresentationTests.cs:67`의 `Configure`에 `statuses: CombatDisplayTables.From(PresentationDatabase().ViewQuery).UnitStatuses` 한 줄을 넣었다(같은 파일 `:419`와 같은 꼴, 확인 내용 그대로, 컴파일 미확인).
- **천명님께 넘길 정리(2026-10-29, TA-f, 미전달):** [특성 발동 연출 — 천명님 공유](2026-10-29-combat-trait-activation-cheon-share.md). 바꾼 시험 두 줄(§5.60의 `:365`, 위 `:67`), 표로 옮긴 표식 줄, 특성 외형 표의 새 칸 `activations`와 새 외형 에셋 둘(170·180, 문구 확인 부탁), View가 기대는 Core 순서(대상 있는 추가 공격의 발동 사건이 그 피해 사건 앞), 따로 전달할 R5 보강·Q14를 한 문서에 모았다.

## 50. 지형 외형 타일 교체·약한 지반 붕괴 연출 — 바뀐 천명님 시험 한 줄 (2026-10-29, Core 파일 변경 없음, 요청 없음, 미전달)

[타일 교체 설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR8, [계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md) §6.4·§6.5.

- 바뀐 것: 필드 창구 `CombatFieldEventPorts` 생성자에 외형 연출 칸이 하나 늘었습니다(`tickAppearance`·`appearancePlaying`·**`appearanceBlocksStart`**·`cancelAppearance`, 옛 토네이도 이름 셋을 바꾸고 하나 더함).
- 그래서 천명님 `Assets/Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs:427`의 위치 인자 줄에 `() => false` 하나가 더해졌습니다(`…, _ => { }, () => false, () => false, () => { })`). 시험 조립은 외형 연출이 없으므로 "막지 않음"을 넘깁니다. 확인하는 내용은 그대로입니다.
- 이 줄은 작업 트리에서 이미 맞춰져 있었습니다(Unity 확인 Uc2 때 컴파일 통과).
- 부탁: 따로 하실 일은 없습니다. 브랜치에서 이 시험을 고치셨다면 병합 때 인자 넷 꼴을 남겨 주세요.
## 51. 레벨업 특성 성장(증강) View·Flow 인수 (2026-10-29, 호진, Core 파일 변경 없음, 미전달)

천명님 [카테고리 특성 성장 인계](2026-10-27-category-trait-progression-handoff.md) §9의 View·Flow 쪽 항목을 이 작업이 맡았다. 설계는 [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md), 기록은 [증강 계획](../plans/2026-10-28-trait-growth-plan-Hojin.md). Unity 재생 확인 문제 없음(호진, 10-29, 로컬 확인용 콘텐츠로), 커밋 전.

- **계약대로 쓴 것:** View는 Buffer 싱크 넷(제안·결과·입력 읽기·확인 쓰기)만 읽고 쓴다. 제출 번호는 씬과 무관한 요청 소유자가 같은 호출에서 보관하고, 결과는 화면이 보여 준 뒤 "계속"에서 `AcknowledgeResult(receipt.RunId, receipt.Number)`로 확인한다. 자동 확인·입력 취소는 없다. 진행 읽기는 Core가 읽는 원본 열쇠(Run·결과·전투·로스터 개정·제안·처리/확인 번호)가 바뀔 때만 한다. Stage Plan이 노드·상점·회복보다 먼저 처리 Job(실행 순간 예약)·준비 Job을 걸고, 각 Job 뒤 확인 Job이 같은 Runner Tick에 확인한다(Lobby/StageSelect 리팩토링 R-d2가 사실·Plan 꼴로 옮김). 메인 제안도 거르지 않는다(설계 Q-TG11).
- **화면 자리(Q-TG8) 미정:** 지금은 확인용 임시 자리 StageSelect다. 전투에서 돌아온 뒤 StageSelect에서 성장을 반복하고, 남은 동안 노드 진입은 View가 막고 Core도 `PendingTraitGrowth`로 거절한다. 인계 §7 Flow 체크리스트("결과 확인 → 성장 반복 → 전환 한 번")와 순서가 다르다. StageSelect로 확정되면 천명님 합의를 받는다. 전투 씬 안(결과 확인 뒤·떠나기 전)으로 정해지면 이 차이는 없다.
- **콘텐츠 작성자에게 알릴 것:**
  - 후보 풀에 넣는 특성마다 View 특성 외형 행(`TraitVisualDataTable`)이 있어야 한다. 없으면 화면이 예외로 멈춘다(설계 Q-TG4 A). 지금 외형 행이 없는 특성 키: 140·150·170·180.
  - 카탈로그는 Main·Heavy·Rider·Cannon 단계를 꼭 하나씩 요구한다(비어 있어도 됨, `TraitGrowthCatalog`). 획득 단계는 그 병과 아군 유닛마다 원래 특성을 뺀 후보가 셋 이상이어야 DB 생성이 통과한다(`TraitGrowthCatalogValidator`).
- **확인용 콘텐츠는 커밋하지 않는다:** `DataBaseSO/InGameGroup/TraitGrowth/`(특성 정의 셋·병과 단계 넷·표 둘)와 `InGameGroup.asset`의 두 줄은 로컬 전용이다. 운영 값·풀은 기획 승인 뒤 등록한다(인계 §9 "Core 시험이 통과했다고 운영 단계 목록을 켜지 말라").
- **강화 노드(1202):** 옛 설계 Q-TG9는 증강과 무관해 메인 강화 상점 주제로 옮겼다. 강화 상점 SO는 지우지 않고 고쳐서 메인 강화 상점에 쓰고 이름은 `MainEnhancement…`로 바꾼다(호진 10-28 결정, 설계 §14.5). 그때 `EnhancementShopUnavailable` 진입 거절을 함께 푼다.
- **보고만:** 전투 승리 때마다 `CombatExperienceEvent(ExperienceEarned)`가 전투 사건 형식 표에 없어 "표시 계약이 없는 전투 사건을 건너뜁니다" 경고가 남는다(천명님 `39482fb5c`, 10-18부터, 동작 영향 없음). 전투 View 쪽에서 따로 정리한다.
- Core 요청은 없다.

<a id="trait-acquisition-display-order-request"></a>
## 특성 획득 순서 표시 요청 — 2026-10-29 (View → Core, 문서 인계 확정, Core 코드 변경 없음)

> **2026-11-02 대신함 → [§5.82](#trait-display-origin) 구현.** 요청 1·2·4·5는 `TraitSettingsSet.Display`(키 + 출처)로 답했다. 요청 2의 "UnitStatsSnapshot/CharacterData에서 제공"은 이미 전투 안·밖에 실리는 `TraitSettingsSet`에 둔 것으로 대신했다(계산이 자주 다시 만드는 스냅샷에 표시 사실을 넣지 않음). 이 절의 아래 내용은 기록으로 남긴다.

사용자가 Core 요청 문서로 인계하도록 확정했다. P4는 사용자 Play 확인까지 완료했으며 이 항목은 별도 Core 후속이다. 실제 Core 착수·계약 제공은 대기 중이고, 외부 메시지 발송은 하지 않았다.

### 요구와 재현 조건

사용자 요청: “나중에 획득한 특성이 2번에 들어가야 해.”
기존 특성 A는 Ability1, 나중 획득한 특성 B는 Ability2에 표시한다. B의 TraitKey가 A보다 작아도 순서가 뒤집히면 안 된다. UnitInfo의 n개 특성 역시 기본 특성 뒤에 획득 순서를 유지하며, 기존 특성의 강화는 표시 위치를 바꾸지 않는다. 전투 중 카드·UnitInfo와 StageSelect 편성 카드에서 같은 보유 개체는 같은 순서여야 한다.

### 확인한 생산·소비 경로

- OwnedUnitSpecResolver.Resolve는 basis.Traits를 복사하고 Acquire에서 traits.Add(definition.Normal), Enhance에서 traits[index] 교체를 한다. 이 지점에는 기본/획득 순서가 보존되어 있다. UnitSpec도 전달받은 TraitSpec 순서를 복사한다.
- UnitDefinition 생성자는 복사한 TraitDefinition을 TraitKey로 Array.Sort한다. 전투 TraitEffectProvider.CreateStatuses는 이 정렬된 TraitDefinitions를 순회한다. 전투 밖 OwnedCharacterStatsCalculator.ToSnapshot도 같은 목록에서 TraitKeys를 만든다. 따라서 현재 두 표시 입력은 획득 순서를 잃는다.
- OwnedCharacterRecord.Growth.Selections는 선택 순서를 보존하지만 각 TraitGrowthSelection에는 StepId와 TraitKey만 있다. Acquire/Enhance 구분은 Core 카탈로그 단계에 있다. 예를 들어 B 획득 후 A 강화는 이력 [B,A]이므로 이를 표시 순서로 쓰면 A/B가 거꾸로 된다. View가 InGame 카탈로그를 읽거나 강화·획득을 추정해 복원하지 않는다.
- View의 UnitCardTraitContent.Create는 받은 순서대로 항목을 만든다. 소비자는 UnitCardUI.SetPanelContent, CombatUnitInfo.ApplyCharacter, DeckEditUI.ShowCard다. 현재 View 자체의 정렬이 원인은 아니다.

### 요청 계약·권장 구현

1. Core가 보유 개체의 특성 표시 순서(기본 특성 → 신규 획득 순서)를 순수 키 목록으로 공개한다. 예시 이름은 TraitDisplayOrder이며 확정 API가 아니다. 기존 특성의 강화는 위치를 유지한다.
2. 전투 UnitStatsSnapshot/CharacterData가 읽는 사실과 전투 밖 OwnedCharacterStatsSnapshot 또는 동등한 동일 읽기 결과에서 이 순서를 제공한다. TraitStatuses/TraitKeys의 기존 순서가 계산·비교에 필요하면 이를 바꾸지 말고 별도 키 목록을 제공한다.
3. 표시 순서는 규칙/정의 합성 단계에서 확보한다. UnitDefinition의 정렬 이전 입력 순서를 보존해 읽기 결과에 넘기는 방식을 우선 검토한다. 기존 효과 실행의 키 순서를 UI 수정 때문에 제거하지 않는다.
4. 해당 읽기의 특성 목록과 순서 목록은 키 집합·중복·개수가 일치해야 한다. 데이터 불일치는 구성/사실 계약 오류로 보고하며, View에서 임의로 누락·키 정렬·고정 캐시로 보완하지 않는다. 특성이 없는 경우는 빈 목록이다.
5. 기본 특성만 있는 적/메인·소환체 등 획득 이력이 없는 대상의 순서도 생산자가 정의한다. 보유 아군은 동일 OwnedId에 대해 씬 전환·화면 재개방 이후에도 같은 결과가 나와야 한다.

### Core 계약 제공 후 View 작업

- 공용 UnitCardTraitContent 변환에서 위 목록의 키로 같은 읽기의 항목을 배열한다. 아이콘·명칭·현재 상태·설명은 기존 항목을 유지한다.
- UnitCardUI·CombatUnitInfo 및 DeckEditCardModel/모델 조립 → DeckEditUI까지 해당 표시 순서를 전달한다. 기존 슬롯 바인딩·Hover·고정 선택·슬라이드·Exclusive는 그대로 둔다.
- 검토할 분기: A 키가 B보다 큼/작음 모두 A→B, B 획득 후 A 강화, A 강화 후 B 획득, n개 추가 획득, 재진입·목록 축소, 같은 종류의 다른 보유 개체. 계산/발동 순서와 표시 순서가 서로 영향을 주지 않아야 한다.

현재는 원인과 변경 계약만 확정했다. 저장소 AGENTS.md의 Core 작업 경계에 따라 Core 구현·View 추정 정렬은 추가하지 않았다. 획득 순서 표시 수정은 아직 미반영이다.

## 52. 토네이도 대각선 이동 요청 (2026-10-29, 호진, View → Core, 초안, Core 파일 변경 없음, 미전달)

토네이도가 발동 때 필드 전체 무작위 칸으로 순간이동하는 것을, 칸을 따라 대각선으로 움직이며 V자·W자를 그리는 이동으로 바꾸려는 요청이다(사용자 2026-10-29). 원본은 [토네이도 대각선 이동 — Core 요청](2026-10-29-tornado-diagonal-move-core-request-Hojin.md)이다. **이번에는 문서만 썼다. Core 코드와 시험은 바꾸지 않았다.**

- 세부(거리·방향·꺾는 곳)는 정해지지 않았다. 거리·방향은 Core 작업자와 정하기로 했고(사용자), 나머지는 기획에서 정한다. 기획서 13장은 아직 "랜덤한 위치에 다시 스폰"이다.
- 지금: 재배치는 `TornadoCalculator.cs:77-78`의 무작위 후보, 사건 `CombatTornadoEvent`는 옛 칸·새 칸 둘뿐이고 지난 길이 없다.
- 요청 계약 R1~R8: 이동 규칙은 Core 한 곳, 진행 상태(방향 등)는 `TornadoSourceFact`에, 사건에 지난 칸 목록, (답에 따라) 지나는 칸 처리 순서·다음 경로 공개·미리보기와 난수 영향·정의 값 자리·진입/주기 두 길.
- 정할 질문 G1~G9: 이동 단위(한 발동에 경로 전체 / 발동마다 몇 칸), 거리, 방향과 꺾는 곳, 막힌 칸, 지나는 칸의 유닛, 휩쓸린 유닛 목적지, 최초 위치, 진입 발동 때 이동, 예고 표시.
- 바뀔 Core 시험: `TornadoEnvironmentTests`·`InitialTornadoPlacementTests`·`EnvironmentEntryPublicationTests`.


## 53. 대신 맞은 공격의 넉백 요청 (2026-10-30, View → Core, 초안, Core 파일 변경 없음, 미전달 — 사용자가 나중에 전달)

- 배경: 희생(Sacrifice, 300)의 가로채기. 사용자 결정 "대신 맞는 경우에는 밀리지 않음"([유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) Q-AA5·AA8).
- 지금 Core(2026-10-30 코드 확인):
  - 넉백 후보는 공격 장부의 역할 목록이다(`AttackKnockbackCalculator.Resolve`의 `ledger.Roles.Keys`, 충격 볼트는 `plan.Delivery.Hits`의 대상).
  - 장부는 역할을 원래 대상 열쇠로 적는다(`CommandAttackLedger.RecordContact`의 `_roles[original] = role`).
  - 그래서 대신 맞은 수호자는 밀리지 않고(규칙과 맞음), 피해를 받지 않은 **지켜진 유닛이 밀린다**(규칙과 다름). 적 `Unit_Gravekeeper`·`Unit_Stage1Boss`가 넉백을 가져 실제 플레이에서 나온다.
- **요청 R-AA1:** 가로채진 접촉(수신자가 바뀐 접촉)은 넉백 후보를 만들지 않는다. 그 접촉으로는 지켜진 유닛도 수호자도 밀리지 않는다.
- 확인 요청(View 쪽 권장):
  - Q1: 지켜진 유닛이 같은 공격에서 가로채지 않은 다른 접촉도 받았으면(다단 전달·관통) 그 접촉으로는 밀리나? — 권장: 밀린다(보통 규칙).
  - Q2: 수호자가 같은 공격의 직접 대상이기도 하면 그 직접 접촉으로 밀리나? — 권장: 밀린다(보통 규칙). View는 이때 수호자의 옮긴 자리를 접고 밀림 걸음을 그대로 재생한다(설계 AA8 안전장치).
  - Q3: 밀리지 않게 된 대상에는 넉백 발동 사실(`KnockbackApplied`·`KnockbackPrevented`)도 나오지 않나? — 권장: 나오지 않는다(View는 사실대로 큐를 낸다).
- View 영향: 요청이 들어가면 View 변경은 없다(밀림 걸음 사건이 오지 않을 뿐이다). 들어가기 전까지는 지켜진 유닛이 밀리는 모습이 그대로 보인다.

## 54. 특성 시스템 View 대응 — 천명님 공유 (2026-10-30, View 작업, Core 파일 변경 없음, 미전달)

- 넘길 문서: [천명님 공유](2026-10-30-combat-trait-system-view-cheon-share.md) — 바꾼 천명님 줄 둘(§5.62·§5.64), 시험 두 줄(§5.61·§5.63), 병합 때 합친 `CombatMovementCalculator`(§5.59 덧붙임), View가 읽는 새 Core 사실 표, 따로 전달할 것(§53, §49 R5 보강·Q14).
- 요청 없음. 처음 넣었던 요청 R1 `CharacterDebuffState.WithHuntersMark`는 10-30 Core 예고(특성 효과를 공통 버프·디버프로, 표식 → 약점 노출)로 뜻이 없어져 뺐다(10-30 사용자 "R1 빼기"). 표식 줄은 그 계약 인계가 오면 옮긴다.


## 55. Core 버프·디버프 View 연동을 위한 전투 밖 특성 설정 조회 요청 (2026-10-31, 초안·미전달)

사용자 요청: “Core 변화에 맞춰서 기존 것들 연동할 수 있게 설계랑 Task 계획부터”. 설계/요청 작성만 승인됐으며 Core 코드 구현 승인이 아니다.

- 기준: `a0ec23c0e`를 병합한 `0a4d3303c`와 현재 작업 트리. [View 연동 설계 §8](../specs/2026-10-31-status-effect-view-integration-design.md#8-카드특성-설명퇴역-처리), [Task D6](../plans/2026-10-31-status-effect-view-integration-plan.md#6-phase-d--카드특성-설명퇴역-등록-정리).
- 제공 확인: 전투 `UnitStatsSnapshot.TraitStatuses`에는 `RegenerationTraitStatusSnapshot(HealAmount, HealEveryOwnSideTurnEnds, CompletedEligibleTurnEnds)`와 `IntimidationTraitStatusSnapshot.AttackReductionPercent`가 있다. 전투 카드는 이 값을 쓸 수 있다.
- 공백 확인: 전투 밖 `OwnedCharacterStatsSnapshot`은 ThornField/HuntersMark/Pull/ImpactBolt 설명과 TraitAvailability를 제공하지만 Regeneration/Intimidation 정량 설정은 제공하지 않는다. `CharacterBaselineStatsSnapshot.TraitKeys`·가용성만으로 회복량/주기/감소율을 알 수 없다.

**CVI-R1 요청:** 전투 밖 보유/덱 카드의 같은 Owned 조회에서, 강화가 반영된 정의의 재생 회복량·자기 진영 턴 종료 주기와 위압 공격력 감소율을 읽을 수 있게 공개해 달라.

| 필요한 값 | 의미/조건 |
|---|---|
| Regeneration 설정 | HealAmount, HealEveryOwnSideTurnEnds. 전투의 실제 HP 회복량/수신 효과/누적 진행값이 아니다. 전투 밖 CompletedEligibleTurnEnds를 생성할 필요는 없다 |
| Intimidation 설정 | AttackReductionPercent. 제거된 우선도 감소/DeferredPriorityComponent를 복원하지 않는다 |
| 식별/시점 | OwnedId·Stats.TraitKeys·현재 강화 정의와 같은 읽기의 설정. 해당 특성이 없으면 값 없음. 보유 키와 설정의 일치 조건을 생산 경계에서 검증 |

기존 패턴대로 OwnedCharacterStatsSnapshot에 두 선택적 typed Description을 제공하거나, 같은 요구를 만족하는 기존 공개 설정 조회로 제공할 수 있다. 구체 Core 배치는 Core 작업자가 결정한다. 새 서비스·View DB 숫자 복제·전투 상태/타이머 추가를 요청하는 것이 아니다. 값이 있는 정상/강화 정의를 각각 사용하며, 존재하지 않는 강화 정의나 획득 후보를 만드는 요청도 아니다.

인수: 일반/강화 정의가 다른 회복 주기/감소율을 가지면 같은 Owned 조회에 정확히 전달되고, 이전 Snapshot은 불변이며, 특성 미보유에는 설정이 없다. 위압은 공격 감소만, 재생은 설정값만 전달한다. HuntersMark의 이미 제공된 증가율/기간 계약은 유지한다.

응답 전 View는 올바른 일반 정책 설명과 전투 정량 표시까지만 진행한다. 전투 밖 정량/강화 인수 D6만 대기로 두고 HUD·피드백·조립 도구 작업은 계속할 수 있다. View가 InGame DB/Unit SO를 읽거나 전투 중 개체/임의 기본값에서 수치를 채우지 않는다.

상태: **요청 초안 작성, 외부 전달 안 함, Core 파일 변경 없음.** 사용자 또는 Core 작업자의 응답을 받기 전 구현/제공 완료로 처리하지 않는다.

> **2026-11-02 Core 답(천명):** CVI-R1은 특성 설정값 공개(§5.81)로 제공했다. 전투 밖 `OwnedCharacterStatsSnapshot.TraitSettings`에서 `RegenerationTraitSpec.HealAmount`·`HealEveryOwnSideTurnEnds`, `IntimidationTraitSpec.AttackReductionPercent`를 읽는다. 선택적 설명 속성 두 개를 더하는 방식이 아니라 공용 묶음으로 답했다. 커밋 전·미전달.


검증 입력 공유(실행 결과 아님): 기존 `Assets/Tests/EditMode/CoreIntegration/DataBaseAssetTests.cs`의 `ProductionShotGun_HasExplicitDurationOneAndUnchangedMagnitudeAndGuid`는 ShotGun에 HuntersMark30%/1이 있다고 가정한다. 현재 브랜치는 병합 충돌에서 기존 ShotGun=Sacrifice 설정을 유지했다. 이 시험의 입력 가정은 현 에셋과 다르므로 후속 실행 시 새 View 회귀와 구분해야 한다. 시험/운영 특성의 정합 결정은 해당 소유자와 협의하며, 이번 View 설계에서 둘 중 하나를 임의로 바꾸지 않았다. CVI-R1의 DTO 제공과는 별개 사항이다.


### 55.1 View 수신 측 구현 현황 (2026-10-31, 미전달)

Core의 canonical 효과8종 Capture/typed 상세·전체 TraitState 사실·HuntersMark 약점 노출 피드백·카드 Availability·조립 v2 연결을 완료했다. Core 코드는 수정하지 않았다. CVI-R1의 전투 밖 Regeneration/Intimidation 설정 DTO는 여전히 없어 해당 카드 정량/강화 연결(D6)만 대기한다. [실행 기록](../plans/2026-10-31-status-effect-view-integration-plan.md#11-구현검증-기록--2026-10-31)을 따른다. 현재 Play 유지 지시로 실제 에디터 컴파일/기존 DB 시험은 미실행이며 Unity Roslyn 코드 컴파일/정적 대조와 구분한다.

## 56. 배치 단계 손패 왕복 뒤 보호막 질문 (2026-10-31, View → Core, 확인 요청 → 사용자 지시로 View 작업에서 Core 수정 §5.73, 미전달)

- 수정 전 상황: 배치 단계에서 단독 행동 유닛을 우클릭으로 손패에 넣었다가(`ReturnUnit`) 다시 놓으면(`PlaceUnit`) 보호막이 없어지고, 자기 진영 턴 끝 리셋 전까지 돌아오지 않습니다.
- 수정 전 원인(당시 코드 대조): 손패 진입 정리(`CombatReserveReturnCleanup` → `AppliedStatusRules.ClearForReserve`)가 보호막을 예산 0·`Eligible = false`·리셋 횟수 그대로로 남깁니다. 다시 놓아 판정을 통과해도 `TraitPresenceReconciler.RecordShieldActivations`가 "예산 0"이라 `ShieldGranted`를 내지 않습니다. [위치 교대·정찰대·되돌리기 인계](2026-10-31-position-swap-recon-undo-core-view-handoff.md) §6의 "Redeployment does not reconstruct a new budget"대로입니다.
- 당시 질문(아래 처리 기록으로 결정됨): 이 규칙이 **배치 단계에서 자리만 바꾸려고 넣었다 빼는 경우**에도 맞나요? 배치 단계에서는 끌어서 옮기기(`PlaceUnit` 필드 → 필드)는 보호막이 그대로인데 손패 왕복만 없어져, 같은 자리 바꾸기가 방법에 따라 결과가 다릅니다.
  - A(View 권장): 배치 단계(전투 시작 전) 손패 왕복은 보호막 기록을 남기지 않거나 다시 놓을 때 새 예산으로 시작.
  - B: 지금 규칙 그대로(배치 단계도 리셋 전까지 없음). 이 경우 View는 바꿀 것이 없습니다(HUD·이펙트가 사실대로 비어 있음).
- View 쪽 상태: 유지 이펙트는 보호막 상태를 그대로 따르므로(이펙트 따라가기·회수 설계 FE5·FE10) Core가 어느 쪽으로 정해도 View 변경은 없습니다.
- **처리(2026-10-31, 사용자 지시 "코어 고치고"):** A 쪽으로 고쳤다 — 배치 단계 손 복귀는 방어막 예산을 지우지 않고 자격만 끄며, 다시 놓으면 넣기 전 예산으로 돌아온다(새 예산으로 다시 채우지는 않음). 밤 종료 회수는 그대로. 내역은 §5.73. 천명님께는 이 결정이 맞는지 확인 부탁으로 전달.

## 57. 특성 이펙트 작업 — 예정 Core 변경 둘 (2026-10-31, 설계 단계, Core 파일 변경 없음, 미전달)

- **2026-10-31 구현됨(사용자 "권장대로 코어 작업 시작해"):** 도발당함 = §5.74, 마무리 사격 대상 목록 = §5.75. 아래 표는 예고 때 기록이다.

특성 이펙트 정리 중 Core를 고쳐야 하는 것이 둘 나왔다. 둘 다 설계 초안이고 결정 대기다. 구현하면 §5에 기록하고 천명님 공유 문서를 따로 쓴다. 목록 전체는 [특성 이펙트 바뀐 것·바꿀 것 정리](2026-10-31-combat-trait-effect-change-summary.md) §2.

| 변경 | 설계 | 구현 주체 | 결정 대기 |
|---|---|---|---|
| '도발당함' 디버프 상태(`StatusEffectKey.Taunted = 14`, `TauntedStatus`, 수명 `CurrentEligibility`). 판정은 AI와 같은 `TauntEffectRule.Affects`, 켜고 끄기는 보유 조정(`TraitPresenceReconciler`), 걸림·풀림 `StatusApplied`/`StatusRemoved` + 상태 사건 | [도발 디버프 설계](../specs/2026-10-31-combat-taunt-debuff-design.md) TT-a | 이 작업(사용자 명시 지시 "1번으로 우리가 수정하는 방향으로") | Q-TT1~Q-TT5. Q-TT5는 천명님 `StatusEffectContractTests.Catalog_ReservesStableUniqueKeysAndSeparatesClassification`(종류 0~13 고정) 기대값 변경 허락 |
| 공격 범위 응답에 마무리 사격 대상 목록 `AttackRangeResult.InstantDefeatTargetIds`(`CombatAttackQueries.Range`가 `FinishingShotApplication.Qualifies`로 거름, `Success` 끝 선택 인자) | [마무리 사격 대상 표시 설계](../specs/2026-10-31-combat-finishing-shot-target-mark-design.md) FS-a | Q-FS2(권장: 이 작업) | Q-FS1·Q-FS2 |

확인 질문(변경 요청 아님): 땅 다지기·먼지 연막 흔적의 효과가 진영을 가리지 않는 것이 의도인가요?

## 58. 토네이도 전투 View 대응 완료 (2026-11-01, View 작업, Core 파일 변경 없음, 시험 변경 없음, 미전달)

[Core → View 인계](2026-11-01-tornado-rockfall-haboob-core-view-handoff.md) §2·§3·§4·§8·§9.3의 토네이도를 View가 받았습니다. 낙석·하붑은 이어서 합니다. 설계는 [토네이도 View 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md)입니다.

- 표시는 `CombatStateData.ReadTornadoAreas(Owner.Player)`, 재생은 `CombatTornadoCenterEvent`·`CombatTornadoMemberEvent`·`CombatCharacterEvent.TornadoContact`만 읽습니다. 요약 `CombatTornadoGimmickEvent`와 진입 시작 `CombatTornadoEntryEvent`는 받기만 합니다.
- 호버 설명의 이동 주기는 `StageGimmicks`의 `TornadoMemberSpec.RepeatIntervalControllerTurns`를 읽습니다.
- View는 옛 `CombatTornadoEvent`·`TornadoSourceFact`를 더 읽지 않습니다. 사건 형식 표에서 옛 줄을 뺐으니, Core가 옛 사건을 다시 내면 표에 없는 사건으로 경고 뒤 건너뜁니다.
- SO 모드 도구가 `TornadoGeometry.Fits`(공개)를 부르고, 안쪽 중심의 가로 방향 필수 규칙은 사본을 둡니다(`TornadoGimmickPreparationCalculator.Prepare` 입력 검사, 그 함수가 internal이라서). 규칙이 바뀌면 `CombatStageSoAssets.TornadoProblem`도 맞춰야 합니다.
- 요청 없음.

## 59. Cowboy 이동 StaleContext — 병과 기본 특성 합성 정의의 참조 비교 (2026-11-01, 수정 요청·미전달)

> 2026-11-02 병합 대조: 아래는 요청 당시 조사 기록이다. 현재 MovementContextCalculator는 OwnedId가 없는 프로필만 Definitions에 넣고, 보유 프로필은 OwnedProfiles 및 SameProfile 값 비교로 분리한다. 따라서 아래의 보유 Cowboy 합성 정의 참조 비교를 현재 미해결 원인으로 단정하지 않는다. 이번 문서 병합에서는 실제 이동 재현·Core 테스트를 실행하지 않았으며, 인수 조건의 충족 여부는 별도 확인 대상이다.

### 증상·현재 설정

사용자 보고: Cowboy 이동이 거절되며 "상황이 바뀌었습니다. 행동을 다시 지정해주세요."가 표시된다. CombatActionNotice는 이동의 StaleContext를 이 문구로 바꾼다. 발목 꺾임(AnkleBroken)의 이동 제한 안내와는 다르다.

현재 Unit_Cowboy.asset은 canMoveVoluntarily=1, Rider(20), 고유 Pull(extraRangeCells=2), 기본 공격 사거리 1이다. CategoryConfig_Default는 Rider 이동 허용량 4와 기본 Leap(뛰어넘기)을 설정한다. 따라서 기본 합성 특성은 Pull+Leap이며, 이 구성 자체가 자발 이동을 금지하지 않는다. 성장·상태 효과가 있는 실제 개체의 추가 특성은 런타임 확인 대상이다.

### 원인 경로

1. CombatInputAssembler.Assemble는 요청마다 OwnedUnitSpecResolver.BuildProfiles를 호출한다.
2. Cowboy 원본에는 Pull만 있고 Rider 기본 Leap이 없으므로 ResolveBase → Compose의 categoryOnly 분기에서 Leap을 더한 새 UnitSpec을 매번 만든다.
3. MovementContextCalculator.Capture는 행위자 및 관련 수집자의 기본 프로필 Definition을 Definitions에 담는다. 미리보기와 명령 실행에서 각각 만든 합성 정의가 여기에 들어간다.
4. SameValues의 `a.Definitions.SequenceEqual(b.Definitions)`는 값 동등성 구현이 없는 UnitSpec을 참조로 비교한다. 따라서 같은 설정·특성이고 상태 변경이 없어도 false다.
5. CombatCommandService.ProcessNextCommand는 Matches=false이면 RejectStale를 게시한다. View는 성공 미리보기의 MovementContext를 이동 명령에 전달하고 있으며, 최종 거절 안내를 표시한다.

Pull의 대상 강제 이동 규칙이 Cowboy의 자발 이동을 막는 경로가 아니다. 병과 기본 특성 합성으로 만들어진 정의의 동등성 검사 문제다. 다른 유닛도 같은 조건으로 새 기본 정의가 합성되거나, 관련 수집자로 문맥에 포함되면 영향을 받을 수 있다. 특정 EntityKey 예외로 처리하지 않는다.

### 재현 증거·한계

2026-11-01 현재 Library/ScriptAssemblies의 Core Contracts/Domain/Application DLL을 별도 PowerShell 프로세스에 읽어 순수 관리 코드로 확인했다. Cowboy에 대응하는 Pull(2) 기본 UnitSpec과 Rider 기본 Leap 카탈로그를 만들고 동일 입력으로 ResolveBase를 두 번 호출했다.

- 합성 키: Pull, Leap.
- 두 결과의 Traits.SequenceEqual: true(같은 특성 참조).
- 두 결과를 담은 UnitSpec 배열의 SequenceEqual: false.
- 동일 원본을 TraitGrowthCatalog.Disabled로 두 번 해석한 배열의 SequenceEqual: true.

이 결과와 실제 Capture/Matches/거절 경로를 소스로 대조했다. Unity Play 및 전체 이동 명령 통합 재현은 실행하지 않았다. 기존 MovementPublicationTests의 ConfiguredMovementBattle.EncounterSource는 BattleInitialization에 traitGrowth를 전달하지 않아 Disabled를 사용하므로, 운영 Rider 기본 Leap 합성을 포함한 회귀를 대신하지 못한다.

### Core 요청·인수 조건

- 이동 문맥이 요구하는 정의의 동등성을 객체 재생성 여부와 분리해 달라. 정확한 비교 계약과 구현 위치는 Core가 결정한다. 단순 검사 삭제·유닛별 예외·View의 특성 데이터 복제로 우회하지 않는다.
- 같은 운영 기본 특성/성장 설정에서 미리보기 → 명령 조립을 반복해도 무변경 이동이 수락되어야 한다. 실제 이동 의존 값이 바뀌면 기존 StaleContext 거절과 무변경 보장을 유지한다.
- Rider 기본 Leap + Cowboy Pull을 활성화한 조립·미리보기·ProcessNextCommand 통합 회귀를 추가하고, 동일 정의 재생성 허용과 실제 의존 값 변경 거절을 함께 확인해 달라. 성장으로 새 정의를 합성하는 경우도 같은 계약을 점검한다.
- SSOT/SOLID/FW·독립성: 규칙·정의 비교는 Core 소유이며 View 안내나 입력 조립에 특성 판정을 복제하지 않는다. 새 서비스·Builder·Export·조회 경로 변경이 필요한 문제로 보이지 않는다. Pull 공격·카메라·다른 View 기능 수정 없이 이동 문맥과 정의 합성 경계에서 해결할 수 있어야 한다.

상태: 원인 조사·관리 코드 부분 재현·요청 기록 완료. Core 코드·에셋·테스트 변경 없음. 사용자 지시는 확인 요청이므로 Core 수정은 진행하지 않았다. 외부 전달 안 함.

## 60. 덱 공용 상세창 — 전투 밖 EXP 진행·범위 그림 조회 (2026-11-01, 요청·미전달)

> 2026-11-02 병합 대조: 충돌한 기존 §58을 §60으로 옮겼다. 현재 DeckEditorSnapshot의 공개 항목과 보유 능력치 조회에는 이 요청의 EXP 진행·설명용 범위 그림 계약이 확인되지 않아 요청을 보존한다. View 쪽 설계 참조도 §60으로 갱신했다.

사용자 요청: DeckSlot/OwnedUnitSlot이 하나의 새 SlotInfoPannel을 사용하고 EXP·범위 그림·Change까지 연결한다. View 설계·Phase 원본은 Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.md의 공용 유닛 상세창 확장 절이다. Core 구현 지시는 받지 않았으며 이 절은 공개 계약 요청이다.

### 확인한 현재 경계

- DeckEditorSnapshot은 같은 읽기의 OwnedCharacterRecord와 OwnedCharacterStatsSnapshot/CharacterBaselineStatsSnapshot을 가진다. 현재 EXP는 OwnedCharacterRecord.Experience에서 읽을 수 있다.
- 현재 공개된 보유 능력치에는 다음 레벨 요구 EXP/진행 활성 상태가 없다. 요구량 원본은 Core 내부 ExperienceRules.Required와 LevelProgressionSpec이다. View에서 역할별 공식/상수를 복제하거나 InGame DB를 조회하지 않는다.
- 보유 능력치에는 MoveMethod/MoveRange/AttackRange 수치가 있으나 이동/공격 범위 그림용 상대 셀·지정 방식/영향 영역은 없다. CharacterDefinitionFacts도 범위 그림을 공개하지 않는다. 전투의 행위자/필드/preview 수명을 빌려 가짜 전투를 구성하지 않는다.

### 요청·인수 기준

1. 같은 덱 조회 버전에서 보유 ID별 EXP 진행 활성 여부, 현재 레벨 내 진행 EXP, 다음 레벨 요구 EXP를 함께 제공해 달라. 비활성 설정과 아직 게시되지 않은 상태를 정상 0 진행과 구분할 수 있어야 한다. View는 받은 값으로 fillAmount와 잔여 수치만 표시한다. ExperienceRules가 유일한 계산 원본이어야 한다.
2. 전투 밖 설명용 이동/공격 범위의 불변 조회 계약을 제공해 달라. 현재 강화·특성·메인 병과를 반영한 보유 ID 기준이며 상대 원점·기준 회전·자기 점유 영역·이동 도달 영역·공격 지정 영역/영향 영역의 의미를 명시해 달라. 전투 필드의 장애물·대상에 따른 실제 행동 가능 영역과 구분한다. 대표 조준 방향/지점이 필요하면 Core가 정한 기준을 응답에 포함한다. 고정 크기나 유닛별 키 예외로 View가 셀을 추정하지 않는다.
3. 게시 위치는 기존 DeckEditorSnapshot/보유 능력치 조회 확장을 우선 검토하되 정확한 DTO/필드명과 게시/무효화 계약은 Core가 결정한다. 동일 버전의 레벨·성장·병과·범위·EXP가 일치해야 한다. 새 루트 서비스로 하위 조회를 재등록하지 않는다. Pending/전투 중 조회 불가/설정 비활성/유효 결과를 명시한다.
4. 동일 CharacterKey의 다른 OwnedId, 성장/병과 변경, 경험치 비활성, 레벨업 후 잔여 진행, 대표 이동·공격 지정 방식의 규칙과 그림 응답 일치를 검증해 달라. View는 SSOT를 복제하지 않고 응답을 그리는 회귀만 가진다. 기존 이동/공격 명령 및 전투 preview 계약 변경은 이 요청 범위가 아니다.

상태: 공개 API 조사·View Phase 계획 및 Core 요청 기록. Core 파일/테스트 변경 없음. 외부 전달 안 함. View Phase 1/2는 이 응답을 기다리지 않으며, EXP 비율·범위 그림의 실제 연결(Phase 3)은 인수 뒤 진행한다.

## 61. 96ea26a 병합 — 전투 정보 조회와 새 UI 통합 (2026-11-02, 사용자 승인, 미전달)

- 공통 ICombatInspectionFactsReader → CombatInspectionInfo → CombatUnitInfoView 경로와 배치/전투 단계 선택을 유지하고, 새 UI의 레벨·병과 그룹 및 병과 이름 표시를 연결했다.
- 사용자 지시로 구조물 진영·설명에 공격력 표시를 함께 추가했다. 공격력은 동일 StructureCombatFacts.Definition.AttackPower에서 전달하며 Core 규칙·버퍼·서비스 등록은 바꾸지 않았다.
- Unit_Akimbo는 최신 Core 병합의 KillChainAttack 설정을 유지했다. 특성 외형 다섯 개는 기존 발동 연출과 상대 브랜치 아이콘을 결합했다.
- 기존 CombatStructureInspectionTests는 옛 목록 직접 전달/ShowStructure 호출을 현재 실제 CombatShownFacts와 ShowInspection 호출로 옮겼다. 배치 단계에서도 정보 선택이 가능하되 행동 주체로 선택하지 않는 정책을 반영하고, 구조물 공격력은 0이 아닌 값으로 확인하도록 보완했다. 새 테스트 파일은 만들지 않았다.
- 검증: MSBuild의 Systems·CoreIntegration 어셈블리 컴파일 성공. Unity 6000.0.73f1 EditMode에서 CombatStructureInspectionTests 7/7 통과, DataBaseAssetTests 73/76 통과(합계 80/83). DeckUI의 직렬화 객체 236개는 중복 ID·누락된 로컬 참조·부모/자식 연결 오류가 없으며, 다섯 특성 아이콘 GUID가 실제 에셋으로 연결된다. 실제 화면의 Play 확인은 별도다.
- 남은 DB 검증 실패: CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits의 Rider/Heavy 두 경우는 초기 배치가 보드 범위 또는 점유와 충돌한다. RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases는 판매 유닛 1종을 기대하지만 현재 에셋은 5종이다. 관련 Core·InGame DB·DataBaseAssetTests는 병합 전 HEAD(6409e98d0)와 동일함을 확인했다. 이번 UI 병합에서 Core 배치 규칙·운영 상점 데이터·해당 기대값은 수정하지 않았으며, Core/기획 담당의 현행 데이터와 테스트 계약 대조가 필요하다. 결과는 로컬 Logs/merge-96ea26a-tests.xml에 보관했다.

## 62. 단독 행동 보호막이 상대 진영 턴 종료에도 초기화됨 (2026-11-02, 사용자 지시로 구현 완료 §5.76·§62.2, 미전달)

- 사용자 보고/요구: 단독 행동(`SoloAction`)의 `ShieldReset`은 **특성 보유자의 자기 진영 턴 종료**여야 하는데 상대 턴 종료에도 보인다.
- 확인 기준: HEAD `4219e1a1d`와 현재 작업 트리. 코드·에셋 정적 대조로 아래 경로를 확인했다. 이번 조사에서 Unity 재생·EditMode 테스트는 실행하지 않았고 Core·View 코드와 에셋도 수정하지 않았다.
- 운영 설명도 [TraitGrowth_SoloAction.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/TraitGrowth_SoloAction.asset)에 "보호막 한도는 자신의 진영 턴 종료마다 갱신됩니다"로 되어 있다. 설정 주기는 `resetEveryControllerTurnEnds: 1`이다.

### 확인한 원인·영향

1. `CombatTurnCalculator.EndPlayerTurn`과 `EndEnemyPhase`는 모두 `CompletedControllerTurnCount`를 1 올리고, `CleanupControllerTurn`에 종료 진영을 정확히 넘긴다.
2. `TraitTurnLifecycle.EndAndRecord`는 `endedSide`를 받지만 마지막 `TraitPresenceReconciler.EndAndRecord(input, projected, events)` 호출에는 진영을 전달하지 않는다.
3. `TraitPresenceReconciler`는 모든 보호막 출처에 `resetDue = true`와 양 진영 합산 누계 `CompletedControllerTurnCount`를 넘긴다. `SoloActionEffectRule`은 이를 `ShieldStatusRules.Reconcile`로 그대로 전달한다.
4. `ShieldStatusRules.Reconcile`은 초기화 경계가 증가하면 `RemainingBudget`을 설정 한도로 실제 복원하고 `LastResetControllerTurnCount`도 갱신한다. `RecordShieldActivations`는 그 증가로 `ShieldReset`을 낸다. 따라서 이번 현상은 **상대 턴 끝의 실제 보호막 재충전과 발동 사건**까지 포함한다. 잔량이 이미 가득이어도 초기화 경계가 바뀌어 양 0의 `ShieldReset`이 나올 수 있다.
5. View의 `CombatPersistentEffects.NotifyActivation`은 받은 발동 종류가 상태 외형의 `RestartOn`에 있으면 유지 이펙트를 다시 낸다. 보호막 외형은 `ShieldReset`을 등록하고 있어 Core가 낸 상대 턴 리셋도 재생한다.

### 수정 요청·인수 조건

- **SR-R1:** 단독 행동의 보호막 초기화는 출처 유닛의 `Owner == endedSide`인 종료에서만 진행한다. 상대 진영 종료만으로 잔량·마지막 초기화 경계가 바뀌거나 `ShieldReset`이 나오면 안 된다. 다른 유닛의 이동·사망으로 자격이 바뀌는 보유 조정은 계속 필요하므로 상대 턴의 보유 조정 전체를 건너뛰지 않는다.
- **SR-R2:** 주기가 2 이상인 정의도 자기 진영 종료 횟수로 판정한다. 합산 누계의 주기 판정에 진영 조건만 더하면 자기 종료 2회 주기가 성립하지 않는다. 공개된 `LastResetControllerTurnCount`는 합산 누계의 마지막 초기화 기록으로 유지할 수 있으며, 주기 판정과 기록 단위를 구분한다(아래 §62.1 측정안).
- **SR-R3:** 플레이어/적 보유자를 각각 검증한다. 일부 소진·완전 소진·가득 찬 보호막에서 상대 종료는 잔량/초기화 기록 유지 및 `ShieldReset` 없음, 자기 종료는 설정 주기에 따른 복원 및 발동을 확인한다. 같은 종료 중 이동·사망으로 고립 조건이 바뀌는 경우의 자격 조정도 유지한다.
- 공유 경로 주의: 결속(`Cohesion`)도 같은 초기화 경로를 사용한다. 기존 `TraitShieldTests.BothSideEnds_ResetBudgets`는 **결속 fixture**로 양 진영 종료 모두의 재충전을 기대한다. 사용자 이번 요구는 단독 행동이므로 결속 정책까지 임의로 바꾸지 말고, 공통 경로 수정 시 영향과 결속 정책 확인을 구분한다.

### 구조 검토·현재 상태

- SSOT/SRP: 초기화 진영·주기는 Core 규칙, 표시 재생은 View 책임으로 유지한다. View에서 상대 턴 발동만 숨기면 실제 보호막 재충전이 남아 해결되지 않는다.
- SOLID: 기존 특성 규칙과 수명 경계 안에서 필요한 종료 정보만 전달한다. 이번 수정 때문에 새 서비스나 인터페이스를 만들 이유는 없으며 기존 효과 계약의 보장을 유지한다(O/L/I/D).
- JMH/FW·독립성: 서비스 조립·등록·입력·씬 수명 변경은 필요하지 않다. 수정·검증 범위는 단독 행동 초기화와 공유 보호막 경로의 실제 영향으로 한정하고, 이펙트 조립이나 다른 특성의 수명을 함께 재구성하지 않는다.
- **조사 당시: 원인 확인 및 요청 기록만 완료. Core 수정 미구현, Unity 미검증, 외부 전달 안 함.** 이후 사용자 직접 수정 지시와 실행은 §62.1~62.2를 따른다.

### 62.1 사용자 Core 수정 허용 후 범위 측정 (2026-11-02, 구현 전)

사용자 지시: "조건 우리가 추가하자, 수정 범위 측정해". 이 건의 Core 직접 수정은 허용됐으며, 이번 단계에서는 범위를 측정했다. 제품 코드·시험은 아직 변경하지 않았다.

**권장 범위: 기존 C# 5개(Core 3 + 시험 2), 이 인계 문서 1개, 총 6개 파일.** 단독 행동을 대상으로 한다. 결속도 같은 경로를 쓰지만 자기 진영 종료 정책은 이번에 확인되지 않았으므로 기존 결속 정책은 유지한다.

| 파일 | 변경/검증 책임 |
|---|---|
| `Application/Combat/TraitTurnLifecycle.cs` | 이미 받은 `endedSide`를 보호막 보유 조정에 전달. 운영 호출부 1곳 |
| `Application/Combat/TraitPresenceReconciler.cs` | 종료 경계를 내부 호출로 전달. 단독 행동은 출처 `Owner == endedSide`일 때만 초기화 후보가 되며, 상대 턴에도 자격·출처 정리는 계속 수행. 기존 종료 전용 진입점도 진영을 명시하도록 맞춤 |
| `Domain/Traits/Effects/SoloActionEffectRule.cs` | 자기 진영 종료 누계로 설정 주기를 판정. 기존 합산 누계는 초기화 기록으로 유지. 내부 `Evaluate`의 호출자는 보유 조정 1곳 |
| `Assets/Tests/EditMode/TraitShieldTests.cs` | 플레이어/적 출처별 주기 1·2·3, 잔량 0·일부·가득, 상대 종료의 잔량/기록 보존·리셋 사건 없음, 자기 종료의 복원/사건, 상대 턴 자격 변경 확인 |
| `Assets/Tests/EditMode/CoreIntegration/TraitShieldPublicationTests.cs` | 실제 전투 종료 경로를 통해 보호막 상태·발동이 Buffer에 게시되고, 상대 종료에서 단독 행동 리셋이 게시되지 않는지 확인 |
| 이 문서 §5·§62 | 구현 후 사용자 지시·실제 Core 변경·시험 결과/한계 기록. 이 단계에서는 §62만 갱신 |

설계 측정:

- 자기 진영 종료 횟수는 기존 턴 사실에서 구한다. Player는 `CompletedPlayerTurnCount`, Enemy는 `CompletedControllerTurnCount - CompletedPlayerTurnCount`이다. 새 Buffer 누계·캐시는 필요 없다.
- 단독 행동 규칙이 자기 종료 누계와 설정 주기로 "이번에 초기화할지"를 정하고, 공통 보호막 규칙에는 합산 누계를 기록 경계로 전달한다. `LastResetControllerTurnCount`의 공개 의미·형태를 유지하고, 단독 행동 경로에서는 공통 규칙의 합산 주기 계산을 중복 적용하지 않는다. 최초 부여·보유 조정도 기록 단위는 합산 누계로 일관된다.
- `ShieldStatusRules`·`ShieldGrantRules`, `CombatTurnCalculator`, 공개 Contracts/DTO·Buffer·DB SO·View·Framework 조립은 수정할 필요가 없다. 결속의 `BothSideEnds_ResetBudgets` 기대값도 유지한다.
- 현재 운영 단독 행동 주기는 1이다. 진영 조건만 추가하는 최소안은 Core 2개 파일이지만, 설정으로 허용된 2 이상 주기가 자기 종료 횟수와 어긋나는 문제가 남는다. 권장안은 규칙 파일 1개를 더 포함해 그 문제까지 같은 변경 이유 안에서 해결한다.
- SSOT: 기존 턴 사실에서 횟수를 파생하며 별도 상태를 저장하지 않는다. SOLID: 보유 조정은 종료 정보 전달·자격 조정을, 단독 행동 규칙은 자기 주기를 담당한다. 기존 공통 보호막 계약을 유지하고 새 인터페이스/서비스를 추가하지 않는다. JMH/FW: 생성·등록·입력·렌더 수명 변경은 없다. 기능 독립성: 결속·도발·이펙트 기능을 함께 재구성하지 않고 공유 보유 조정의 동작 보존만 회귀한다.

Task/리뷰 지점: ① 위 Core 3개 파일 수정 → ② 두 시험 파일에 회귀 추가 → ③ Unity 컴파일·해당 EditMode 시험과 공유 보호막/이동 되돌리기/손패 복귀 회귀 확인 → ④ §5·§62에 결과 기록 후 Phase 종료 보고. 기존 회귀 후보는 `TraitShieldTests`, `TraitShieldPublicationTests`, `TraitActivationOutcomeTests`, `ShieldStatusPolicyTests`, `MovementPassiveUndoTests`, `ReserveReturnEffectResetTests`이며 실제 시험 수정 범위와 실행 범위를 구분한다. 컴파일 오류나 기존 fixture의 턴 누계 불일치가 발견되면 영향과 추가 파일을 먼저 보고한다.

상태: **범위 측정 완료, 구현 미시작, 테스트 미실행.** 커밋·push·외부 전달은 하지 않았다.

### 62.2 Core 구현과 과거 도구 실행 기록 (2026-11-02, 사용자 Unity 확인 대기)

사용자 "우리가 구현 진행해"로 §62.1의 범위를 실행했다. 실제 제품/시험 변경은 계획한 Core 3개·시험 2개와 이 문서, 총 6개 파일이다. 새 제품 파일·메타는 없다. 구현 계약은 §5.76이 원본이다.

**컴파일:** VS 2022 MSBuild로 `ProjectL.Tests.Core.csproj`와 `ProjectL.Tests.CoreIntegration.csproj` 및 의존 Core/Systems 어셈블리 빌드 종료 코드 0. 변경 파일 새 경고·오류 없음. 기존 `CombatCommandEffects.cs:144` CS8632 1건, 기존 UI CS0649 7건은 유지된다. 로컬 로그는 `Logs/shield-reset-core-build.log`, `Logs/shield-reset-integration-build.log`, 최종 시험 재빌드 로그 `Logs/shield-reset-*-build-final.log`다.

**시험 방식:** 프로젝트의 Unity 제공 NUnit DLL과 Unity Mono 런타임으로 빌드된 실제 시험 어셈블리를 직접 실행했다. 아래는 Unity 에디터 Test Runner 실행 결과가 아니다.

| fixture | 통과/실행 |
|---|---|
| `TraitShieldTests` | 49/49 (신규 38 포함) |
| `TraitShieldPublicationTests` | 8/8 (신규 3 포함) |
| `ShieldStatusPolicyTests` | 9/9 |
| `MovementPassiveUndoTests` | 3/3 |
| `ReserveReturnEffectResetTests` | 5/5 |
| `TraitTurnLifecyclePublicationTests` | 2/2 |
| `TraitActivationOutcomeTests` | 68/69 (아래 기존 도발 실패 1건) |

- 합계 **144/145**, 신규 **41/41** 통과. 자기/상대 진영 종료 시 잔량·합산 초기화 기록·발동 개수, 보호막이 이미 가득한 경우 양 0 리셋, 비기본 주기, 늦은 최초 부여, 상대 종료의 자격 정리, 실제 입력·자동 종료·Buffer 게시를 확인했다. 결속의 기존 양쪽 종료 시험도 통과했다.
- 실패는 기존 `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`의 발동 개수 기대 1/실제 2다. 작업 트리 Core를 되돌리지 않고 HEAD `4219e1a1d`의 변경 전 Core 3개 소스를 임시 빌드 입력으로 대체한 별도 어셈블리에서도 같은 실패를 확인했다. 기존 시험은 수정하지 않았다.
- 같은 수정 전 어셈블리에서 신규 단위 회귀 38건은 모두 실패했고 현재 어셈블리에서는 모두 통과했다. 따라서 새 시험이 이번 결함을 실제 검출함을 확인했다. 수정 전 빌드와 실행기는 `Temp/ShieldResetVerification/`에만 있다.
- 최종 로컬 증거: `Logs/shield-reset-core-nunit-final.xml`(134/135), `Logs/shield-reset-integration-nunit-final.xml`(10/10), `Logs/shield-reset-baseline-nunit.xml`(신규 38 실패 + 기존 도발 1 실패/1 통과). 실행기·로그는 로컬 검증 산출물이다.
- Unity 배치 실행은 다른 에디터가 같은 프로젝트를 열고 있어 시험 시작 전에 종료됐다(`Logs/shield-reset-unity.log`). 열린 에디터 접근도 컴퓨터 사용 도구의 앱 접근 승인 시간 초과로 완료하지 못했다. 에디터 Test Runner·실제 게임의 이펙트 재생은 확인하지 않았으며 열린 에디터를 종료하지 않았다.
- 검증 전 저장한 직렬화 파일 7,569개(에셋·씬·프리팹·머티리얼·메타·프로젝트 설정)를 검증 후 SHA-256으로 대조해 차이 0개를 확인했다. 시작 때 있던 사용자/다른 작업 변경은 보존했다. 변경 6개 파일의 `git diff --check`도 통과했다.

구조 검토: 기존 턴 사실에서 자기 진영 횟수를 파생해 SSOT/P7을 지킨다. 종료 정보 전달·자격 조정과 단독 행동 주기 규칙을 분리하고 기존 효과 계약·좁은 내부 입력을 유지해 SOLID 5원칙을 점검했다. JMH/FW 서비스·조립·수명 계약 변경은 없고, 결속·도발·이펙트의 별도 기능 구현을 수정하지 않았다. 범위 추가나 설계 변경 없이 이번 Phase를 종료하고 보고한다. **외부 전달·커밋·push 없음.**

### 62.3 Core 변경 전달 문서·작업 원칙 정정 (2026-11-02)

- 사용자 요청으로 [단독 행동 ShieldReset Core 변경 전달 문서](2026-11-02-solo-action-shield-reset-core-changes.md)를 별도로 작성했다. 이번 Core 3개 파일·내부 호출 계약·진영별 주기·유지한 경계·사용자 Unity 확인 항목·기존 시험 파일 추가 상태를 정리했다. 외부 전달은 하지 않았다.
- **현재 보고: 컴파일 미확인.** 앞서 Codex가 새 시험 41건을 작성하고 MSBuild·별도 NUnit을 실행한 것은 View 작업의 Test 코드 작성 금지·전투 View MSBuild 금지 원칙을 지키지 못한 작업이다. 실제 실행 사실과 수치는 §62.2에 남기지만 사용자 Unity 인수로 취급하지 않는다. Core 수정 허용을 시험 작성·빌드 제한의 예외 허용으로 해석하지 않는다.
- 사용자가 재확인한 원칙을 `AGENTS.md`에 기록했다. View 작업 전체에서 새 Test 코드를 작성하지 않고, 전투 View에서 MSBuild를 실행하지 않는다. 컴파일·재생 확인은 사용자가 Unity에서 한다. 기존 시험은 컴파일에 필요한 최소 수정만 하고, 실행에서 깨진 우리 시험은 검증 내용을 유지하며 호출만 맞춘다. 다른 사람 시험 수정은 먼저 묻는다.
- 이 파일을 수정하면 파일 제목도 함께 갱신한다는 원칙을 `AGENTS.md`와 `CLAUDE.md`에 기록하고, 이번 문서 제목에 최신 범위·변경 주제·갱신일을 반영했다.
- 이번 정리는 안내·인계 문서만 수정했다. 코드·시험·Git 스테이징을 변경하거나 빌드·시험·커밋·push를 실행하지 않았다.

## 63. 낙석·하붑을 View가 받음 (2026-11-02, Core 요청 없음, 미전달)

[Core → View 인계](2026-11-01-tornado-rockfall-haboob-core-view-handoff.md)의 낙석(§5·§9.4)과 하붑(§6·§9.5)을 View가 받았습니다. Core 파일은 고치지 않았고 요청도 없습니다. 컴파일 미확인, 새 시험 없음, 재생 확인은 사용자가 합니다.

- 낙석: [계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md). 사건 줄·전용 표시·처리기, 호버 `{n}`은 `RockfallGimmickSpec.MaximumHealthLossPercent`에서 읽습니다.
- 하붑: [계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md). 칸마다 연기 이펙트, 이동 사건 순간 `Final.Cells`로 맞춤, 호버 설명.
- **View가 기대는 Core 동작 넷:** ① 칸 조준에서 사거리 검사가 하붑 검사보다 먼저입니다(`AttackSelectionResolver.TryResolve` → `CanDesignate`). 사거리 밖 칸은 일반 문구가 나옵니다. ② 막힌 칸은 범위·조준 목록에서 빠지고, 그 칸을 직접 물으면 `HaboobDesignationBlocked`로 답합니다. View는 목록에 없는 칸을 누를 때 그 칸의 공격 미리보기로 이유를 묻습니다. ③ 다른 자리가 없으면 같은 칸으로 `Relocation`을 냅니다. View는 그때 소리를 내지 않습니다. ④ 경고 사건은 칸이 그대로입니다.
- 기록 정정: 2026-11-02 낙석 RF-d 기록에 이 문서 §63을 적었다고 했으나, 파일에 그 절이 없었습니다(커밋 이력에도 없음). 이 절이 대신합니다.

## 64. 게임 Description 수치 공개 요청 (2026-11-02, 설계안·미구현·미전달)

사용자 요구는 게임에 존재하는 모든 Description의 수치 대응이다. [현황·설계안](../specs/2026-11-02-description-numeric-values-design.md)과 [Task 계획](../plans/2026-11-02-description-numeric-values-plan.md)을 작성했다. 이번 단계에서는 Core/View 코드와 에셋을 변경하지 않았다. 아래는 공개 읽기값 요청이며 Core 직접 수정 허용이나 합의된 API 서명이 아니다.

### 64.1 확인한 간극

- `UnitCardTraitContent`는 일부 특성만 수치를 쓰고 나머지는 View SO 원문이다. 집중 포격은 View 원문 50%/성장 원문 70%, 마무리 사격은 20%/15%, 사기 진작은 20%/10%, 사냥꾼은 공격력 1/3으로 나뉜다. 특정 한 숫자로 원문을 통일해도 강화·등급별 실제 값은 설명할 수 없다.
- 전투 `TraitStatusSnapshot` 중 `ArmorBreakerTraitStatusSnapshot`, `FocusedBombardmentTraitStatusSnapshot` 등은 존재 정보만 있고 설명에 필요한 설정값이 없다. `Hunter`의 성공 보상 횟수나 `Acceleration`의 보유 보너스는 설정 보상량/칸당 증가량의 대용이 아니다.
- 보유 `OwnedCharacterStatsSnapshot`은 가시밭/표식/끌어오기/충격 볼트/정찰대 설정만 개별 공개한다. 충격 볼트가 공용 View 설명까지 전달되지 않는 것은 View 쪽 Task이며 새 Core 값 요청이 아니다.
- 훈련 `CurrentValues`/`NextValues`는 비교 수치에 재사용할 수 있지만 Description 본문은 고정 문자열이다. 성장 `TraitGrowthChoice`는 현재 이름/설명 중심이며 선택 결과의 모든 정량 설정을 주지 않는다.
- 상점 아이템 문장은 InGame 고정 Description이고 가방/장착은 View 풍미 문구다. `ItemInventorySnapshot.Counts`와 장착 키만으로 효과 설정을 알아낼 수 없다. `ItemDefinition`이 Core 처리 입력에 존재하는 것과 View의 공개 읽기 경로로 전달되는 것은 구분해야 한다.
- 유물 `ArtifactData`는 식별·배치·점유 칸 중심이다. 현재 유물 View 문구에 수치가 없더라도 효과 수치 설명을 위해 공개 읽기 경로가 필요하다.
- 고지대/모래폭풍 감소값은 `Domain/Movement/EnvironmentTerrainRules`의 규칙에 있고 `StaticTerrainFact`에는 없다. View SO의 30%, 1, 20%를 숫자 원본으로 유지하면 규칙 변경과 분리된다.

### 64.2 요청하는 정보와 읽기 문맥

| 구분 | 요청 | 기존 값 재사용/경계 |
|---|---|---|
| 특성 설정 | 실제 적용된 정의의 비율·양·범위·지속 단위/횟수·발동 횟수·조건 임계값. 설계 §2.2의 미소비/미공개 목록부터 키별로 확정 | 이미 typed snapshot에 있는 값은 재사용. 활성 상태와 설정값의 의미를 구분 |
| 전투/보유 | 전투의 표시 snapshot과 같은 시점, 보유 `OwnedId`의 강화/훈련이 반영된 조회값 | CharacterKey만으로 추정하지 않음. 적/보유 아군 모두 지원. 퇴역/일부 미지원 안내 유지 |
| 훈련/증강/강화 | 선택 결과의 설정, 훈련의 현재/다음 설정. 구매 후 유지 카드의 시점도 구분 | 기존 `TraitRankValues` 활용. 단순 비교 수치만으로 없는 반경/조건을 추측하지 않음 |
| 아이템 | 상점/미장착 재고/장착 HUD에서 읽을 효과 종류별 설정값 | 로프 우선도 감소, 랜턴 방어 감소, 위스키 발동 임계/회복 비율, 로켓 발동 임계/피해 감소 등 실제 정의 사용 |
| 유물 | 해당 정의/등급/개체의 효과 수치와 적용 조건에 필요한 공개 정보 | 반격·회복·영혼 보너스의 의미와 현재 적용량 구분. 배치 계산 재구현 요청 아님 |
| 환경 | 고지대 피해 감소, 모래폭풍 사거리 감소/하한·피해 감소 등 고정 문장에 남은 규칙 수치. 지뢰 파급 비율/범위 등도 기존 공개 사실로 충분한지 확인 | 이미 공개된 지뢰 피해/깃발 우선도/약한 지반/보급품/토네이도/낙석 값은 추가 요청 아님 |

구체 API/DTO 배치는 Core 작업자가 정한다. 기존 공개 경로가 충분하면 새 API 대신 경로를 안내하면 된다. 부족한 경우 기존 읽기 계약을 통한 불변 typed 값으로 제공하고, 새 표시 전용 규칙 저장소나 View가 해석할 임의 계산식을 만들지 않는다. 특성마다 선택적 생성자 인자를 계속 늘리는 형태보다 필요한 특성 설정을 일관되게 읽는 구성을 검토해 달라는 요청이다. 게임 수치·확률·판정·미지원 기능을 바꾸는 요청이 아니다.

회복 `RecoveryOptionState.ProjectedMaxHealthIncrease`는 이미 제공돼 있으므로 Core 추가 요청이 없다. View의 본문/수치 줄 소비 누락으로 처리한다. 숫자 없는 풍미 문구와 미사용 DescriptionMaster를 옮기는 일도 Core 요청에 포함하지 않는다.

### 64.3 소유권·인수 조건

- Core는 수치와 조건의 원본, View는 문장 틀과 문자열화를 맡는다. View는 InGame/Domain을 직접 읽거나 기본 숫자를 복제하지 않는다.
- 같은 정의의 서로 다른 강화/등급 개체, 전투 전/중, 획득·강화·훈련 후보에서 각 문맥의 실제 값이 나와야 한다. 설정 기간과 활성 효과의 남은 기간을 구분한다.
- SRP/OCP: 특성·아이템·환경의 공개/변환을 기능별로 유지. LSP: 같은 snapshot 계약의 의미·형식 보존. ISP/DIP: 화면이 필요한 읽기 계약만 받음. JMH/FW: 새 루트 서비스나 전역 상태를 요구하지 않고 기존 Factory/Group 경계를 유지한다.
- 계약 추가 시 실제 생산/투영과 Buffer 게시, 보유 조회, 해당 소비 화면만 연결한다. 관련 없는 기능의 규칙·UI 수명을 재구성하지 않는다. Test 코드 작성·전투 View MSBuild 제한은 Core 직접 수정 지시가 추가돼도 자동 해제되지 않는다.
- 상태: **요청 기록만 완료, Core 답변/구현/사용자 Unity 인수 전. 컴파일 미확인. 외부 전달·커밋·push 없음.**

### 64.4 View 선행 반영과 남은 공개값 (2026-11-02)

사용자가 후속 Phase 연속 진행을 지시했다. 이미 공개된 값으로 환경 공통 치환, 특성 5종(가시밭/끌어오기/표식/정찰/충격 볼트), 회복의 종류별 본문·예상량을 연결했다. 덱/보유 카드도 같은 특성 변환을 사용하고, 기존 덱 상세 경로의 가시밭·충격 볼트 전달 누락을 해소했다. 이 5종은 신규 Core 요청이 아니다. 회복도 추가 공개 요청이 없다.

전체 범위는 Combat에 한정되지 않는다. 상점·가방·장착의 동일 아이템, 덱/보유/전투의 동일 개체, 훈련/증강/강화의 결과 설명까지 필요하다. 상세 인수 목록은 [Description 커버리지](../viewDocs/description-numeric-coverage.md)에 있다.

- 위협/재생의 보유 설정 미공개는 기존 §55 요청과 같은 잔여다. 별도 기본값을 만들지 않았다.
- `TrainingChoiceSnapshot.CurrentValues/NextValues`의 Amount/SecondAmount로 등급 비교는 가능하지만, 예를 들어 정찰의 Radius는 읽을 수 없다. 현재/다음 설정의 부가 조건도 필요하다.
- `ArtifactPlacedEffectSnapshot`/`ArtifactLoadoutSnapshot` 타입은 존재한다. 다만 `ArtifactLoadoutReader`가 Core 내부에서 InGame DB를 결합해 만드는 계산 입력이며, 보유/배치 View Buffer에 설정을 게시하는 경로가 아니다. 미배치 목록도 포함하는 공개값이 필요하다.
- 게임 수치/판정/미지원 기능을 바꾸지 않고 기존 정의의 설정을 읽기 계약과 생산자에 연결하는 범위다. Core 직접 수정은 명시적 허용 전이며 View가 InGame DB/Domain을 읽는 우회는 하지 않았다.

View 코드·에셋은 정적 대조만 했고 Unity 확인 전이다. Test 코드·MSBuild·커밋·push·외부 발송 없음. **컴파일 미확인.**

### 64.5 Core 수정 범위 분석 (2026-11-02, 분석만·미전달)

사용자 요청 “Core에서의 수정 범위 분석해줘”에 따라 [파일별 수정 범위·권장 공개 형태](../specs/2026-11-02-description-core-scope-analysis.md)를 작성했다. 코드/에셋/시험 수정은 없고, Core 직접 구현은 아직 승인 전이다.

- 특성: 최종 설정을 공통 불변 형태로 공개. 보유/전투/훈련/증강이 같은 설정 표현을 사용한다. 기존 `TraitSpec` 계열의 재사용과 실제 정의 투영을 우선 검토한다. 기존 상태 snapshot을 가짜 상태로 만들어 보유 설명에 쓰지 않는다.
- 아이템/유물: 기존 `ItemDefinition`/`ItemEffectSpec`, `ArtifactEffectData`가 있어 새 숫자 원본을 만들 필요 없다. 상점/재고/장착 및 미배치/배치의 Buffer 전달이 주요 수정 범위다.
- 환경: `EnvironmentTerrainRules`의 숫자 리터럴을 규칙/공개 투영이 공유하는 원본으로 최소 추출해야 한다. 동작은 유지하지만 규칙 파일에 전혀 닿지 않는 작업이라고 단정하지 않는다.
- 범위 정정: 현행 강화 스테이지는 Training으로 진입하며 옛 Enhancement 상품 생성은 없다. 회복·기존 상태 효과와 옛 강화 상품 활성화는 추가 Core 구현 범위에서 제외한다.
- 별도 결정: 지뢰 원문은 주변 절반 피해지만 실제 `MineMovementTileRule`은 동일 피해를 요청한다. 설명 공개 작업으로 Core 피해를 변경하지 않는다.
- 기존 검사 영향: `UnitStatsSnapshot`에 필드를 추가하면 `With`/`SameValues`와 `ContractShapeGuardTests`의 고정 프로퍼티 목록을 함께 검토해야 한다. 시험 수정 제한은 계속 적용된다.

SSOT·SOLID 5원칙·JMH/FW·기능 독립성과 권장 구현 순서는 분석 §8에 기록했다. **컴파일 미확인.** Test 코드·MSBuild·Unity 실행·커밋·push·외부 발송 없음.

### 64.6 Core 답 — 특성 부분 (2026-11-02, 천명, 구현 완료·커밋 전·미전달)

> 병합 때 번호를 §64.4 → §64.6으로 옮겼다(같은 날 View 쪽이 §64.4·§64.5를 먼저 씀).

- 특성 설정은 구현했다(§5.81). 보유·덱 카드는 `OwnedCharacterStatsSnapshot.TraitSettings`, 전투는 적 포함 `CombatStateData.TraitSettings.For(유닛)`. 값은 resolver가 만든 최종 `TraitSpec`이라 강화·등급별 실제 값이고, 같은 유닛 키라도 보유 개체마다 다르다.
- 설명 전용 DTO를 특성마다 만들지 않고 기존 `TraitSpec` 형식을 그대로 공개했다(§64.2의 "선택적 생성자 인자를 계속 늘리는 형태보다 일관된 구성" 요청, §64.5 분석의 "공통 불변 형태" 권장에 대한 답).
- 남은 것: 훈련 현재/다음·증강 후보 결과 설정, 아이템, 유물, 환경 규칙값 — 아직이다(설계 §8). 지뢰 문구·규칙 불일치는 기획 결정 사항이다.
- View 인계: [특성 설정값 공개 View 인계](2026-11-02-trait-settings-view-handoff.md). 옛 특성별 설명 속성 정리를 View에 요청했다.

## 65. 특성 발동 → 상태 연결 사실 공개 요청 (2026-11-05, 평가 C1-57, 질문, 미전달)

- 배경: 2026-11-04 전투 View 평가 중간 C1-57. View는 특성 발동(`TraitActivationSnapshot`) 뒤 따라오는 상태 사건 가운데 어느 것이 그 발동이 건 상태인지를 사본 표로 짐작한다(`DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs` 38-48, `ViewSystem/Combat/Playback/Support/CombatTraitStateEvidence.cs` 48-80).
  - CleanSweep → 공격력 증가(`AttackIncreaseStatus`)
  - ReconSquad·HuntersMark → 약점 노출(`WeaknessExposure`)
  - 가속 → 보유 공격 보너스(`HeldAttackBonus`)
- 지금은 Core 규칙과 맞는다. Core가 상태 형식이나 부여 방식을 바꾸면 그 특성의 상태 이펙트·몸 위 글자가 빠진다(`CombatTraitFeedbackPlayback.Begin`이 경고를 한 번 남겨 완전히 조용하지는 않음).
- 요청: `TraitActivationSnapshot` 또는 `CombatTraitStateEvent`가 그 발동이 부여·변경한 상태 항목(효과 출처 키 `EffectOriginKey`와 상태 식별)을 함께 공개해 줄 수 있는가.
  - 공개하면: View는 판독기 셋을 출처 기반 판독기 하나로 바꾸고 프로필 표의 증거 열을 지운다(약 60줄, View 쪽).
  - 공개하지 않으면: 지금 사본 표를 받아들인다고 기록만 남긴다. View 쪽 변경 없음.
- 같은 질문을 천명님 특성 공유 문서 §7에도 적었다. 코드 변경 없음, 미전달.

## 66. 공격 시 선행 특성 연결 조사·광범위 요청 철회 (2026-11-10)

### 66.1 정정 이력

처음에는 도발·근원 없는 발동·추가 공격 등 모든 특성의 소속과 역할을 공개하는 관계 DTO를 요청했다. 사용자가 대상을 **공격 시 선행 발동하는 특성**으로 좁히도록 정정했고, 그 범위의 생산 경로와 Buffer 전달을 다시 조사했다. 앞선 요청은 외부에 전달하지 않았으며 현재 범위의 필수 Core 작업에서 철회한다. 이 정정은 §65의 특성 발동–상태 연결 요청과 별개다.

### 66.2 좁힌 범위와 연결 근거

- 대상은 Pull/PullApplied, Vanguard/AttackGranted, CleanSweep/AttackGranted다. 발동 조건·수치를 View가 계산하지 않고 실제 공개 사건만 읽는다.
- CombatAttackCalculator는 사건에 있던 발동 객체를 traitRows에 담고 AttackResultFactory·AttackResult는 그 객체 참조를 보존한다.
- CombatEffectsEvent·CombatCommandTransition·CombatResult는 배열만 복사한다. CombatCommandComposition과 CombatStateBuffer 공개도 동일 사건/발동 참조를 전달한다.
- 최종 결과 안에서 대상 발동의 동일 객체 참조가 해당 공격의 TraitActivations와 사건 목록에 유일하게 나타나고 그 사건이 공격 출처보다 앞이면 소속과 선행 순서를 확정할 수 있다.
- Vanguard/CleanSweep의 ResultOrigin이 없어도 위 연결은 가능하다. 공격자와 발동자가 같은지로 제한하지 않는다.
- Pull의 이동 연출은 AttackResult.Pull과 CombatPullEvent.Movement의 동일 참조로 시작 실행에 연결한다.

### 66.3 제외와 유지보수 조건

- 도발, 이동·턴·피격·사망·처치 발동, Hunter 재선택, 공격 후 발동, 독립 추가 공격 시작, 가로채기는 새 시작 기준의 대상이 아니다.
- 요약에 속하고 앞에 있다는 조건만 사용하면 Hunter 재선택이나 당김 중 처치 보상을 잘못 포함할 수 있다. 현재 지원하는 (특성, 발동 종류) 목록을 View 적용 범위 한 곳에 둔다. 효과 규칙·발동 조건을 복제하는 표는 아니다.
- 발동 값 비교·가까운 다음 공격·AttackGroup 단독으로 참조 누락을 보정하지 않는다. 중복 소속/필요한 발생 누락은 검증 오류다.
- 이 설계는 확인된 현재 인메모리 전달을 사용한다. 깊은 복사·직렬화 복원 등 다른 전달 경로로 바뀌면 참조 기반 대응도 재검토한다. 그때 안정 ID가 필요하면 필요한 범위만 요청한다. 현재는 신규 Core 구현 요청이 없다.

### 66.4 작업 상태

[설계](../specs/2026-11-10-pull-attack-animation-sync-design.md)와 [계획](../plans/2026-11-10-pull-attack-animation-sync-plan.md)을 정정했다. 타이밍은 유닛의 CombatUnitAnimation에서 특성 연출 시작 기준 공격 클립 시작 지연으로 설정하는 방향이다.

사용자는 구현을 보류했다. 이번 작업은 정적 조사·문서 변경이며 Core/View 코드·SO·클립 수정, Test 작성, MSBuild, Unity 재생, 커밋·push·외부 전달 없음. **컴파일 미확인.**

## 67. 적 전용 특성 재구성·자폭·공포 — View 인계 (2026-11-10, Core 구현 완료, 미전달)

설계 [enemy-death-traits-design](../specs/2026-11-10-enemy-death-traits-design.md)·계획 [enemy-death-traits-plan](../plans/2026-11-10-enemy-death-traits-plan.md). Core만 바꿨고 View 코드는 고치지 않았다. 운영 적 유닛에는 세 특성이 아직 없어서(기획이 단계를 지정하기 전) 지금 화면에는 나오지 않는다.

### 67.1 기획이 특성을 붙이기 전에 View가 할 것

| 우선 | 할 것 | 이유 |
|---|---|---|
| 1 | 상태 `StatusEffectKey.Reconstructing`(19, 버프)의 HUD 표시 변환·상태 외형·설명 갈래 | 없으면 재구성 중인 유닛이 HUD에 나오는 순간 `CombatEffectFormatters`가 "표시 변환이 등록되지 않았습니다: Reconstructing"으로 멈춘다(도발당함 14 때와 같은 짝). |
| 2 | 사건 형식 표에 `CombatEventKind.Reconstructed`(49) 한 줄 | 없으면 경고 후 건너뛴다(연출 없음). 체력 0 → 최대 전환은 결과 끝 사실 그리기에만 반영된다. |
| 3 | 특성 외형 표에 `Reconstruction 490`·`SelfDestruct 500`·`Fear 510`(아이콘·이름·설명) | HUD·정보창 특성 칸 |
| 4 | 발동 370·380·390·400 연출 줄, 거절 이유 52 | 아래 표 |

### 67.2 키·상태·사건

| 바뀐 것 | Core 사실 | View 제안 |
|---|---|---|
| 특성 키 | `Reconstruction 490`(단계 1~3), `SelfDestruct 500`(단계 1~3), `Fear 510`. 적 전용이다(플레이어 소유·성장 후보·병과 기본 특성에서 거절). 상태 스냅샷 `ReconstructionTraitStatusSnapshot(Level, Used)`, `SelfDestructTraitStatusSnapshot(Level)`, `FearTraitStatusSnapshot`. | 특성 외형 표 등록. 재구성 `Used`(전투당 1회 사용함)를 칸에 보일지 결정. |
| 상태 키 | `Reconstructing 19`(버프). `ReconstructionStatus(RemainingOwnTurnStarts)`, 읽기 모델 `ReconstructionEffectSnapshot`. 재구성 중에는 이동·공격 불가, 피해·강제 이동은 받는다. 남은 수는 그 유닛 자기 진영 턴 시작마다 1 준다. | 상태 외형·HUD 설명(남은 자기 턴 시작 수). |
| 사건 `Reconstructed 49` | `CombatCharacterEvent` 맞기 전 사실 → 재구성 사실(체력 최대·재구성 중·사용함, Lv2부터 최대 체력 50% 보호막). 그 맞음에는 `HealthChanged`·`Died`가 없다. 공격 피해에서는 피해 출처가 있고, 낙석에서는 없다. 마무리 사격 즉사도 버틴다 — 즉사 발동(`InstantDefeat`)은 그대로 나오고 그 뒤에 370·`Reconstructed`가 온다(사망 사건 없음). 공격 범위 응답의 즉사 대상 목록에는 재구성 보유자도 들어간다. | 쓰러졌다 일어나는 연출(HP가 0까지 떨어졌다 가득 차는 표시). |
| 발동 `ReconstructionStarted 370` | 출처·대상 = 재구성 유닛, 크기 = 잃은 HP(맞기 전 현재 HP). 공격 피해에서는 `Reconstructed` 사건 바로 앞, 낙석에서는 모든 낙석 수신자 사건 뒤. | 재구성 시작 연출 |
| 발동 `ReconstructionEnded 380` | 진영 턴 시작에 부활할 때, 그 유닛의 상태 사건(재구성 해제) 바로 앞. 남은 수만 줄 때는 상태 사건만 나온다. | 부활 연출 |
| 발동 `SelfDestructExploded 390` | 출처 = 죽은 자폭 유닛, 크기 = 폭발 피해(사망 직전 공격력 × 50/75/110%), `TargetIds` = 실제로 바뀐 개체(없어도 사건은 남음), `TargetCells` = 주변 8칸, `TargetCell` = 사망 칸. 뒤에 대상별 체력·구조물 사건(물리 칸 = 사망 칸, 귀속 = 자폭 유닛)이 따른다. | 폭발 이펙트(칸 목록), 피격은 뒤 사건대로 |
| 공포 `StatusApplied 160`(키 `Fear`) | 출처 = 죽은 공포 유닛, 대상 = 실제로 상태가 바뀐 유닛, `TargetCells` = [사망 칸]. 뒤에 무장 해제·속박 상태 사건(출처 = 공포 특성). 바뀐 대상이 없으면 사건이 없다. 같은 키 행동 제한은 남은 횟수가 긴 쪽이 남는다. | 공포 두 상태를 출처 특성 이름으로 묶어 보일지 결정. `Disarm` 설명이 지금 "행동 제한"이다. |
| 발동 `CorpseDisplaced 400` | 넉백 공격으로 죽은 자폭·공포 보유자의 시체 경로. 출처 = 죽은 유닛, 키 = 자폭(있으면)·아니면 공포, 크기 = 칸 수, `TargetCells` = 시작 → 끝. 위치 사건은 없다. 넉백 단계에서 나오고, 폭발·공포는 끝 칸 기준이다. 공개 미리보기에는 없다. | 사망 연출 뒤 시체를 끝 칸까지 미는 연출 |
| 부활 넉백(재구성 Lv3) | 진영 턴 시작의 부활 직후, 주변 8칸의 한 칸 캐릭터(진영 무관)를 EntityId 순으로 바깥 한 칸. 강제 이동 `Moved`(`MovementCause` = 강제 + `Reconstruction`), 발동 `KnockbackApplied`(출처 = 부활 유닛, 키 `Reconstruction`, 크기 1, 대상 = 실제로 밀린 유닛). 공격 결과의 넉백 요약(`AttackResult.Knockbacks`)은 없다. 굳건한 의지로 막히면 `KnockbackPrevented`(출처 = 막은 유닛). 여러 칸 유닛·구조물·장애물은 밀지 않는다. | 기존 넉백 연출 재사용 |
| 거절 이유 | `MovementFailureReason.Reconstructing = 52`, `AttackFailureReason.Reconstructing = 52`. 무장 해제·발목 꺾기보다 먼저 판정한다. | 적 전용이라 플레이어 안내는 거의 없다. 모르는 값에서 예외를 내는 switch가 있으면 갈래 추가. |

### 67.3 사건 순서

- 자폭·공포는 명령 완료 정산에서 나온다. 같은 결과 안에서 원 공격·이동·넉백·낙석 사건이 모두 끝난 뒤, 영혼·보상 사건 앞이다. 연쇄 폭발은 바퀴마다 이어 붙는다. 공격 동시 묶음에 섞지 않는다.
- 낙석: 충돌 사건 → 수신자별 체력(재구성이면 `Reconstructed`)·속박 → 재구성 발동(370) → 자폭·공포. 낙석 사망은 이제 자폭·공포를 발동한다. 기차·낭떠러지 낙사는 발동하지 않는다.
- 진영 턴 시작: 턴 시작 사건 → 특성 수명 → 재구성 감소·부활(380 + 상태 사건) → 부활 넉백 → 지속 피해 → 자폭·공포(밀려 죽은 경우).

### 67.4 미리보기·AI

- 공개 공격 미리보기는 재구성을 반영한다(최대 체력, `Reconstructed`·370). 자폭·공포·시체 밀기는 넣지 않는다.
- AI는 재구성·자폭·공포를 모형화하지 않는다. 재구성 중인 적은 행동 후보가 없다.

### 67.5 운영 활성화 순서

View 등록(67.1) → Unity 확인 → 기획이 적 유닛 DB에 특성·단계 지정(적 전용 자격만 허용, 수치표는 Core `EnemyDeathTraitLevels` 한 곳) → `DataBaseAssetTests`.

## 68. 첫 배치·공격·이동 초기 실행과 대기 Tick 조사 요청 — 2026-11-10

사용자가 첫 동작 끊김 개선과 대기 Tick의 힙 할당·람다 점검을 요청했다. Core 직접 수정 승인은 없으므로 이 항목은 **요청·미전달**이며 구현 완료가 아니다. 원본은 `Logs/ProfilerExport/20261110-212514-765-4380a2dc`다.

- 배치 프레임 1404: CPU 182.041ms, JIT self 합계 141.468ms. Core.Command 135.534ms 내부 Calculate 89.748ms / Publish 44.960ms. 첫 실행 비용을 정상 반복 계산 비용으로 혼동하지 않는다.
- 공격 미리보기 2552: CPU 160.653ms, JIT self 127.285ms, Core.Preview 138.291ms. 공격 실행 2630: CPU 108.314ms, JIT self 48.349ms. 이동 2832: CPU 91.135ms, JIT self 38.934ms.
- 요청: 상태를 바꾸지 않는 명시적 준비가 Core에 필요한지, 운영 빌드/두 번째 실행에서도 남는 계산 비용이 무엇인지 담당자가 검토한다. View에서 가짜 배치·공격·이동 명령을 실행하거나 Core 내부 reflection으로 강제 JIT하지 않는다. 게임 결과/난수/Buffer를 오염시키는 워밍업은 금지한다.
- InGameCoreSystemGroup.Tick의 IReadOnlyList TickModules foreach는 열거자 박싱 후보다. LifeCycleSystemGroup/InGameSystemGroup/InGameViewSystemGroup에도 같은 형태가 있다. 런타임 Update 아래 128B/frame 관측과 정합적이나 할당 호출 스택으로 확정한 것은 아니다. 소유자 확인 뒤 인덱스 순회 등으로 제거하고 재측정 요청한다. Architecture/Core 영역은 이번에 수정하지 않았다.
- View의 계측 마커 일괄 생성은 별도로 P1에서 지연 캐시로 개선한다. 전체 계획과 경계는 `plans/2026-11-10-combat-first-use-performance-plan.md`다. Test/MSBuild/Unity 실행 없음, 컴파일 미확인.
- **추가(2026-11-10): 첫 동작 Core 준비 작업 문서** [2026-11-10-combat-first-use-preparation-core-work.md](2026-11-10-combat-first-use-preparation-core-work.md). Core 단계별 첫 실행 JIT(합계 약 436ms)를 정리했다. 상태를 바꾸지 않는 Core 준비 서비스(`ICombatFirstUsePreparation.PrepareNext`, 가칭)를 전투 골격 Job이 Combat 씬 로딩 준비 단계에서 부르는 안이다. 정할 것은 D1~D7이다. 할지 말지는 사용자가 판단하며(2026-11-10), 그 전에는 요청이 아니고 **미전달**이다. Core 코드 변경 없음.
- **결정(2026-11-10 사용자 "우리측에서 작업하는 걸로 하고 Job에 거는 시점까지 계획으로 잡아"):** 천명님께 요청하지 않고 이 작업에서 Core까지 고친다(사용자 명시 지시 예외). Task 계획은 [plans/2026-11-10-combat-first-use-preparation-plan.md](../plans/2026-11-10-combat-first-use-preparation-plan.md)이다(FU-0~FU-c, 코드 착수 전). 고칠 Core 파일은 여섯이다. 새 파일 셋(`ICombatFirstUsePreparation`·`CombatFirstUsePreparationService`·`CombatFirstUseRehearsal`)과 기존 파일 셋(`CoreProfilerMarkers` 마커, `CoreProfiling` 구간 열거 값, `InGameCoreSystemGroup` 멤버·해석 줄)이다. 측정기는 열거 이름으로 마커를 자동으로 만들어 이름 줄은 따로 필요 없다(계획 FU-0). 계획은 7기준 점검(계획 §7) 뒤 확정했다(2026-11-10 사용자 "문제 없는 지 체크하고 확정"). 연결 줄은 `CombatSystemFactory`·`InGameCoreSystemServiceKey`다. 구현하면 §5 새 항목에 기록하고 천명님 공유를 쓴다.
- **절 번호(2026-11-10):** 원래 §67로 썼으나, 원격의 천명님 §67(적 전용 특성 재구성·자폭·공포 View 인계)과 번호가 겹쳐 원격 커밋을 받으며 §68로 옮겼다. 내용은 그대로다.
- **구현(2026-11-10, 사용자 "쭉 진행해"):** Core 준비 서비스와 전투 골격 연결을 넣었다. Core 변경 기록은 §5.84다. 컴파일 미확인, 미전달.
- **측정(2026-11-11):** 반영분 Unity 컴파일 성공(Editor.log 00:20). 새 기록 `20261111-002203-127-35b7ea71`에서 준비를 뺀 Core 마커 안 JIT가 약 436ms → 56ms다. 첫 배치 182 → 77ms이고, 남은 Core JIT 29.7ms는 게시 경로(D3, 그대로 둠)다. 비교 표는 첫 동작 계획 "FU-c 비교".

## 69. 유닛 범위 조회 답의 원본 약속 — 2026-11-11 (View 기대 기록, 요청 하나 선택, 미전달)

View 호버(`Input/Interaction/CombatUnitRangeHover`)가 성능 계획 P4로 유닛 범위 조회 답(`CombatPreviewResult.UnitRange`, `UnitAttackRangeResult`)을 (전투 번호, `CombatStateBuffer.QueryRevision`, 유닛)마다 기억한다. 같은 판에서 같은 유닛을 다시 호버하면 Core에 묻지 않는다. 요청한 판과 답을 받은 판이 같을 때만 기억한다.

- **View가 기대하는 것:** 유닛 범위 답은 `QueryRevision`이 덮는 사실에서만 나온다. 판이 같으면 같은 유닛의 답도 같다.
- **지금 코드로 확인한 것(2026-11-11 검토):** 답은 `CombatAttackQueries.UnitRange`가 입력 조립(`CombatInputAssembler.Assemble`)의 캐릭터 사실·CommandState·영혼 필드·보유 기록·메인 상태·아티팩트·재고·재화로 계산한다. 전투 중 이것을 바꾸는 길은 `BattleTransactions`의 수락 명령, 캐릭터·영혼 변경, 완료뿐이고 모두 판을 올린다. 거절 명령은 사실을 바꾸지 않는다. 판은 전투 설치 때 1이 되고 늘기만 하며, `EndBattle`이 0으로 돌린다(View는 0인 판의 답을 기억하지 않는다).
- **깨지는 경우:** 나중에 전투 중 재고·아티팩트·재화 등을 판을 올리지 않고 바꾸는 길이 생기면, A → B → A 호버에서 A의 옛 범위가 보인다. Core의 적 판단 도장(`EnemyDecisionSourceStamp`)은 판 밖의 원본을 따로 대조하므로 같은 생각은 아니다.
- **천명님께 부탁(둘 중 하나):** ① 위 약속을 지켜 주고, 판 밖에서 조회 원본을 바꾸는 길을 만들면 알려 준다. ② 또는 행동 가용성처럼 `UnitAttackRangeResult`에 답을 만든 판(`SourceRevision`)을 실어 준다. 그러면 View는 그 값으로 기억한다. 지금은 ①로 충분하다고 본다.
- Core 파일 변경 없음. 기록은 성능 계획 "P4 구현".

## 70. 작렬(CrashImpact) View 대응 — 2026-11-11 (인계 답, 요청 없음, 미전달)

천명님 `810dfc682`와 [작렬 View 인계](2026-11-10-crash-impact-view-handoff.md)를 받아 View 쪽을 맞췄다. 설계 [crash-impact-view-design](../specs/2026-11-11-combat-crash-impact-view-design.md), 계획 [crash-impact-view-plan](../plans/2026-11-11-combat-crash-impact-view-plan.md). Core 파일 변경 없음.

- **인계 §4 답 ① 재생 순서:** View 사건 형식 표가 발동 사건의 근원을 `ResultOrigin`에서 읽어, 부딪힘 발동과 부딪힘 피해가 같은 근원 열쇠로 한 동시 묶음이 되고 직접 타격 묶음과 따로 재생된다. 코드 변경 없이 "타격 → 날아감 → 부딪힘 피해"로 보일 것으로 본다(Unity 재생 확인 대기).
- **② 바로 옆이 막힌 경우:** 이동 사건 없이 `KnockbackApplied`(Magnitude 0)와 부딪힘 발동만 와도 View는 그대로 받는다(날기 시작 줄은 두지 않음).
- **View가 Core 계약으로 기대하는 것:** 발동 사실의 `TargetCells`·`TargetCell`과 부딪힘 피해 근원의 `PhysicalCell`은 맵 밖일 수 있다. 맞은 방향은 칸 좌표 차이로 구해 맵 밖 근원 칸도 받는다(`CombatHitDirection`). View는 특성 발동 이펙트 자리를 정할 때 맵 밖 칸을 건너뛴다(`CombatCuePoints.ForActivation`). 그 밖의 사건(순간 큐 등)은 맵 밖 칸이면 계약 위반으로 보고 예외를 낸다.
- **View가 읽는 값:** 걸음 사건의 `MovementCause.TraitKey == CrashImpact`(날아가는 걸음 모양), 넉백 요약 `Collision`(미리보기 표지 색), `CrashImpactTraitSpec`의 거리·두 비율(설명 토큰 — `TraitDescriptionBindings`에 한 줄, 천명님이 쓴 줄이 있는 파일이라 알림).
- 요청 없음.

## 71. StageSelect 런 포기 — 2026-11-11 (Core·View 구현, 컴파일 미확인, 미전달)

- 사용자 승인 범위는 StageSelect 런 포기에 필요한 Core 변경이다. Core 계약은 §5.86, Task와 확인 목록은 [StageSelect 런 포기 계획](../plans/2026-11-11-stage-select-run-abandon-plan.md)에 있다.
- `OptionUI`가 기존 `ConfirmUI`에서 Stage 전용 문구로 Y 확인을 받고 `LifeCycle.StageRunAbandon`에 요청한다. `StageLifeCycle`은 맵 준비·로딩·Runner·전투 진입·착지 대기를 확인한다. 수락 시 옵션을 닫고 `StagePlan`이 포기를 우선 실행한다.
- Core가 런을 정리하고 Runner가 비면 `StageLifeCycle`이 로비로 전환한다. 새 런은 로비에서 출발할 때 시작한다. 기존 종료 요약·누적 명성·영구 유물을 유지한다.
