# CharacterBuffer와 Effect 엔진의 Framework 이전 설계

작성일: 2026-09-29  
용도: Claude Opus가 후속 구현 계획을 작성하기 위한 설계 및 코드 조사 인계  
상태: 사용자 요청에 따라 작성한 설계안. 게임 규칙의 확정 사항과 기술 제안을 구분하며, 구현 완료나 기술안 전체 승인을 뜻하지 않는다.  
조사 기준 HEAD: `655607206d2aa58a3c4a6a67340b736592e0caac`

## 2026-10-01 attack targeting addendum

Read the [attack targeting design](2026-10-01-attack-targeting-methods-design.md) and the updated [implementation plan](../plans/2026-09-29-character-buffer-effect-migration-plan.md) for the attack slice after Task 5. The new design reconciles the supplied 22-slide PPTX and [MD source snapshot](../handoffs/2026-10-01-targeting-methods-source-handoff.md), including corrected shape numbers and confirmed splash damage semantics. It supersedes old attack-targeting behavior only where those rules explicitly change it; existing code is not higher authority than the user's newly supplied design.

CharacterBuffer/Soul ownership, pure Core calculations, Systems service binding, and View-owned CombatFlow/Plan/Jobs remain unchanged. Task 6 retains its existing persistent-value/turn/cleanup scope. Task 7A–7E implements the revised attack contracts, geometry, damage, Buffer publication, preview and AI together; Task 8 verifies actual data and the existing execution route. Do not migrate the old attack selection once and rewrite it again afterward.

This addendum is documentation only. It does not modify completed task evidence, authorize unrelated Framework/View changes, or settle the targeting design's scoped T1–T7 gameplay/data/interface questions. New implementation documentation is written in English; historical text below is retained.

## 1. 목적과 읽는 순서

기존 전투 Core의 규칙과 Effect 엔진을 재사용하면서, 런타임 상태의 원본을 Systems의 Buffer로 이전한다. 캐릭터 데이터에 효과마다 필드를 추가하거나, Core와 Buffer에 같은 가변 상태를 동시에 유지하는 구조를 피한다.

후속 작업자는 2절의 확정 규칙, 4절의 현재 코드 차이, 5~8절의 제안 구조, 12절의 미확정 규칙을 먼저 읽는다. 이후 13절의 이전 단위를 실제 구현 계획으로 분해한다. 이 문서는 구현 계획을 대신하지 않으며, 그대로 전투 전체를 한 번에 고치라는 요청이 아니다.

정보 우선순위는 이번 대화에서 사용자가 확정한 내용 → 현재 소스의 실제 동작 → 과거 설계·임시 기획 자료 순이다. 기존 코드의 동작은 회귀 기준이지만 변경된 요구보다 우선하지 않는다.

## 2. 사용자 확정 사항

### 2.1 상태 소유와 경계

- Buffer가 런타임 진행 데이터의 원본이다. Core는 현재 상태와 명령으로 규칙을 판단한다.
- View는 사용자 의도를 입력 Buffer에 기록하고, Core 측 상태·결과 Buffer를 읽어서 표시한다. DB 조회와 Core Snapshot 조립을 View에 맡기지 않는다.
- Core.Application / Core.Domain은 Systems Buffer를 직접 참조하지 않는다. Systems의 처리 계층이 PostBindService로 연결하며, 필요한 경우 Core.Contracts 입력·포트를 사용한다.
- Buffer가 초기화 대상과 기록 보존 범위를 책임진다. Job은 공개 API를 호출하고 개별 Buffer의 Clear를 나열하지 않는다.
- 새 Buffer·공용 Framework 변경·Run ID/버전·락·일반 트랜잭션·재시작 이력을 선행 조건으로 추가하지 않는다.
- 맵 진행은 CurrentProgressBuffer / StageProgressBuffer / MapProgressBuffer를 원본으로 사용한다. StageMapRunState 등의 별도 진행 상태는 제거 대상이나 이번 캐릭터 작업과 무관한 일괄 삭제는 하지 않는다.

### 2.2 캐릭터와 덱

| 항목 | 확정 규칙 |
|---|---|
| 메인 | 1장 |
| 서브 | 여러 장 보유 가능. 같은 종류의 카드도 개별 보유 개체로 구분해야 함 |
| 덱 | 보유 서브 전체가 전투용 덱. 별도 편성 기능은 현재 없음 |
| 최초 가져오기 | 메인 1장 + 무작위 서브 최대 3장. 부족하면 있는 만큼, 같은 보유 개체 중복 선택 없음 |
| 서브 사망 | 보유 기록은 남으며 다음 전투에서 사용 가능 |
| 서브 전투 상태 | 전투 종료 시 초기화 |
| 메인 지속 상태 | 현재/최대 체력, 공격력, 최대 Action 코스트, 이동 수치 등은 사망 전까지 스테이지·챕터 사이에서 유지 |
| 턴 종료 | 해당 유닛의 현재 Action 코스트를 최대치로 회복 |
| 임시 버프 | 전투 한정 효과가 존재할 수 있으며 종료 시 제거 |
| 상점 | 캐릭터 획득 가능. 서브 강화 상품은 현재 요구 아님 |
| 회복 스테이지 | 메인 체력 회복 가능 |

마지막 영혼 성장 합의가 이전의 “서브 전투 후 초기화”에 대한 예외다. 서브의 지속 영혼 성장까지 초기화하면 안 된다. 최초 가져오기와 실제 필드 배치/전투 시작은 별개이며, 이 문서만으로 자동 배치를 추가하지 않는다.

### 2.3 아티팩트

- 회복 성배: 메인 대상, 회복량 3. 임시 텍스트의 모든 아군/10은 사용하지 않는다.
- 영혼 성장: 메인과 서브 모두 전투 종료 후에도 공격력·최대 체력 성장이 유지된다.
- 영혼 하나의 증가량은 현재 고정값(공격력 +1, 최대 체력 +1)을 전제로 한다. 개체별 획득 개수를 보관하고 효과 수치를 재계산하는 방식을 사용자가 제안했고 본 설계의 기준으로 채택한다.
- 영혼 성장 반영은 전투 진입 및 전투 중 획득 직후에 필요하다. 영혼 개수의 보존과 전투용 적용 수치의 재생성을 구분한다.
- 영혼 획득 이동 비용은 환급 대상에서 제외한다는 기획 의도를 보존한다.
- 기존 Effect 엔진을 재사용한다. 새 범용 엔진을 먼저 만들지 않는다.
- 메인 사망 후 전체 기록 처리/새 Run 시작 API는 이번에 새로 확정하지 않았다. 영혼 기록을 임의로 지우는 호출을 추가하지 않는다.

### 2.4 View 입력

StageInputBuffer / CombatInputBuffer의 두 영역을 사용한다. 이름과 API는 View/Framework 작업자와 조율 중이다. Core는 입력을 읽기만 하고 입력 제거는 View/Framework가 한다. 입력 번호와 성공·거절 결과 대응 방식은 제안 단계이며 정확한 연결 계약은 아직 받지 않았다.

상세 목록은 [View 입력 Buffer 전달 문서](../handoffs/2026-09-29-view-input-buffer-core-contract-ko.md)를 따른다. 이 문서의 구현 계획을 위해 View Buffer를 임의로 구현하거나 계약이 확정된 것처럼 취급하지 않는다.

## 3. 용어와 이번 설계의 범위

- Framework Plan: StagePlan 등의 정적 작업 구성 코드. Job 목록과 우선순위, 서비스 주입을 구성한다.
- Job: Runner가 Execute를 호출하는 작업 단위. 입력/상태를 읽고 Core 처리를 호출한다.
- Effect 변경 Plan: CombatEffectPlan 등의 계산 결과. 적용할 상태 변경을 표현한다.
- 지속: 전투·챕터 경계를 넘어 보유하는 런타임 상태. 디스크 저장이나 영구 계정 진행을 자동으로 뜻하지 않는다.
- 파생 수치: 기준 수치와 효과 상태에서 계산한 표시·판정용 값. 별도로 증가시키는 두 번째 원본이 아니다.

이번 설계는 CharacterBuffer, 지속 성장, 전투 효과 상태와 기존 Core 연결을 다룬다. UI/Prefab/씬 변경, 모든 Artifact 규칙 구현, Run 재시작 시스템, 일반 이벤트 버스, 범용 저장 시스템, 전체 Plan 명칭 변경은 범위 밖이다.

## 4. 현재 코드에서 확인한 사실과 설계 영향

이 절의 파일은 16절 코드 지도의 경로를 기준으로 한다.

| 현재 사실 | 이전 시 영향 |
|---|---|
| CharacterData와 ArtifactData는 비어 있는 더미 타입 | 초안의 필드나 TryGet/Apply가 이미 구현되어 있다고 가정하지 말 것 |
| CharacterBuffer는 Characters/Add/Clear와 Read/WriteSink를 제공 | 기존 등록 구조를 유지하며 좁게 확장 가능 |
| RunCharacterDeckState는 EntityKey별 장수를 저장 | 같은 종류의 카드별 영혼 성장 구분 불가능 |
| StageDrawState는 EntityKey 배열을 섞고 커서로 소비 | 보유 개체 ID를 보존하도록 드로우 입력과 결과 변경 필요 |
| StageSetupService는 종류 키로 새 UnitState를 생성 | 다음 전투에서 같은 보유 카드의 성장을 찾을 연결 필요 |
| StageSessionFactory는 전투마다 EntityIdGenerator를 생성하고 ID는 1부터 시작 | 현재 EntityId를 그대로 지속 성장 키로 쓰면 전투 간 다른 개체에 성장 연결 위험 |
| UnitState는 체력 외에 Action·이동 정산·Trait/Category·SoulGrowth를 보관 | 화면 필드만 옮기는 것으로 상태 이전 완료 불가 |
| StageSession 및 조립 서비스들이 맵·점유·패·턴·타임라인 등 가변 상태를 공유 | 캐릭터만 Buffer에 복사하고 기존 객체가 계속 변경되면 원본 중복 |
| TraitEffectOwner와 CategoryEffectOwner는 UnitState resolver에 의존 | 엔진은 재사용 가능하나 owner의 상태 접근/적용 연결은 이전 필요 |
| Artifact는 별도 Lifecycle/Projection/SoulService/AttackPowerEvaluator 경로도 사용 | 모두 이미 공통 owner에 통합되어 있다고 가정하지 말 것 |

특히 개체 식별과 종류별 장수 덱은 이번 설계에서 발견한 선행 변경점이다. 영혼 개수 필드 하나를 추가하는 것만으로 지속 성장을 구현할 수 없다.

## 5. 권장 저장 구조 — 최소 변경과 책임 분리

다음은 기술 제안이며 최종 타입/API는 구현 계획에서 구체화한다.

### 5.1 CharacterBuffer: 보유 개체와 캐릭터 상태

기존 CharacterBuffer를 유지하고 내부 데이터를 역할별로 구분한다. 거대한 CharacterData 생성자에 모든 효과 필드를 밀어 넣지 않는다.

| 내부 데이터 묶음 | 원본으로 저장할 내용 | 수명 |
|---|---|---|
| 보유 개체 | 안정적인 보유 ID, 캐릭터 종류 키, 메인/서브, 보유에 필요한 성장 데이터 | 보유 기간 |
| 지속 효과 기록 | 보유 ID → 영혼 개수 | 전투/챕터 이동 후 유지 |
| 캐릭터 현재 상태 | 현재 체력, 현재 Action, 위치 구분·칸·방향 등 | 필드별 유지/초기화 정책 적용 |
| 적용 수치 조회값 | 적용 공격력·최대 체력·최대 Action 등 | Core 계산 결과로 갱신, 외부 직접 수정 금지 |

영혼 개수는 CharacterBuffer 내부의 별도 좁은 레코드/매핑으로 두는 안을 우선한다. 개체에 귀속된 지속 성장 한 종류만으로 SoulBuffer 또는 범용 PermanentEffectBuffer를 추가하지 않는다. 캐릭터 데이터 안에 SoulAttackBonus/SoulMaxHealthBonus를 원본으로 중복 보관하지 않는다.

보유하지 않은 적 유닛은 전투 기간에만 존재한다. 같은 조회 형태를 제공할 수 있어도 적까지 보유 덱/지속 성장 목록에 포함하지 않는다. 비전투 보유 상태와 패 상태도 구분한다. 기존 초안의 Hand/Field/EnemyWaiting/Excluded만으로는 비전투 보유 상태가 명확하지 않다.

현재 체력은 메인에 대해 하나의 원본만 둔다. 전투 종료 시 별도 Run 체력으로 복사한 후 두 곳을 계속 수정하는 구조를 목표로 삼지 않는다. 기존 Run checkpoint는 이전 대상 경로가 전환된 뒤 정리한다.

### 5.2 전투 범위 저장 영역

턴·낮밤·승패·드로우 순서/커서·점유·효과 수명은 캐릭터 보유와 다른 책임이다. 기존 Buffer에는 이들을 담는 완성된 저장소가 없다. 다음 영역을 전투 상태 Buffer의 후보로 명시하되 물리적 클래스 수는 최소화한다.

- 전투 진행: 현재 행동자, phase, 타임라인, 결과와 이미 수행한 종료 처리
- 드로우/패: 보유 ID 순서와 소비 위치, 패 슬롯 관계
- 효과 실행 상태: Trait/Category의 남은 사용 횟수·중첩·방향 등 기존 규칙에 필요한 상태
- 전장 효과: 바닥 영혼의 ID·Origin·칸·남은 턴, 기존 가시 지대 등 실제 이전 범위의 상태
- 이동 정산: 체크포인트·미정산 비용·확정 거리 등 기존 규칙에 필요한 상태

권장안은 위 데이터를 하나의 전투 수명 저장 영역에서 타입별로 분리하는 것이다. 이를 CombatStateBuffer로 신설할지 기존 합의된 전투 Buffer에 넣을지는 구현 계획에서 Framework 담당 경계를 확인한다. View의 CombatInputBuffer와는 명확히 다른 데이터다.

새 저장 영역의 근거는 현재 Core에 실제로 존재하는 전투 상태와 View가 표시해야 하는 바닥 영혼이다. 미래 기능을 예상한 범용 key/object 저장소는 만들지 않는다. 캐릭터 위치와 점유는 별도 독립 변경 원본으로 만들지 말고 위치를 기준으로 함께 갱신하는 인덱스 또는 단일 점유 소유 계약을 정한다.

### 5.3 ArtifactBuffer

ArtifactBuffer는 보유/배치/선택된 아티팩트 정보를 담당한다. 개별 캐릭터의 영혼 개수는 아티팩트 아이템 자체의 상태가 아니다. Artifact 해제 시 이미 획득한 성장을 삭제하는 정책은 현재 합의되지 않았으므로 자동으로 연결하지 않는다.

효과 설정은 기존 ArtifactEffectData와 DB/정의 경로를 재사용한다. 설정에 남은 턴·획득 개수를 섞지 않는다. 기존 compiler/loadout/activation lease의 수명은 실제 호출자 이전과 함께 보존한다. 엔진만 재사용한다는 이유로 ArtifactService의 장기 가변 소유까지 그대로 남겨도 된다는 뜻은 아니다. Artifact 보유·배치의 완전한 원본 이전은 독립 후속 범위로 표시할 수 있다.

## 6. 보유 개체 ID와 전투 ID

### 6.1 기술 결정

**채택: Core.Contracts에 `CharacterInstanceId`를 추가하고, 기존 `EntityId`는 전투 범위 식별자로 유지한다.** 2026-09-29 사용자의 기술 선택 요청에 따라 생성·드로우·구조물·전투 상태 호출 관계를 비교한 뒤 결정했다. 후속 구현 계획에서 두 대안 중 하나를 다시 고르는 미확정 항목이 아니다. 아직 런타임 코드에 구현한 것은 아니다.

| 식별자 | 의미 | 발급·유지 범위 |
|---|---|---|
| 캐릭터 종류 키 | 어떤 정의/DB 행인가 | 같은 종류 카드들이 공유 |
| CharacterInstanceId | 보유한 메인 또는 서브 카드 한 장 | 보유 생성 시 발급, 전투·챕터 이동과 서브 사망에도 유지 |
| EntityId | 현재 전투에서 동작하는 유닛·구조물 등 | 기존 Stage별 발급 방식 유지 |

### 6.2 비교와 선택 근거

| 방식 | 이점 | 필요한 변경/부담 | 결정 |
|---|---|---|---|
| 보유 ID와 전투 ID 분리 | 기존 전투 ID 의미·발급을 유지하며 카드별 성장 식별 가능 | 좁은 ID 타입과 현재 전투 연결 필요 | 채택 |
| 기존 EntityId를 보유 기간까지 유지 | ID 타입 하나로 표현 | 전투별 allocator 교체/외부 ID 주입, 적·구조물 ID와 충돌 방지, 관련 factory와 조회 경로 점검 필요 | 미채택 |

현재 StageSessionFactory는 Stage마다 EntityIdGenerator를 만들며, UnitFactory와 StructureFactory가 ID를 발급한다. ReserveSlotState, StageMapState, CombatTimelineState 등은 EntityId를 사용한다. 특히 타임라인 초기화에는 EntityId 값 정렬도 있다. 발급 정책 변경은 영혼 저장 외의 전투 동작까지 영향 분석을 요구한다.

두 안 모두 종류별 장수 덱을 보유 개체 목록으로 바꿔야 한다. 따라서 별도 ID를 피한다고 덱 이전이 없어지지는 않는다. 현 요구에 필요한 지속 식별만 추가하고 전투 식별의 의미를 유지하는 쪽을 선택했다.

```text
보유 카드 A: CharacterInstanceId=101, 종류=Rider, 영혼=2
보유 카드 B: CharacterInstanceId=102, 종류=Rider, 영혼=0

이번 전투: EntityId 7 → 보유 101
다음 전투: EntityId 3 → 보유 101
성장 기록은 항상 보유 101에 귀속
```

- 종류별 장수는 보유 레코드에서 계산하는 조회값이다. 기존 counts와 보유 목록을 모두 쓰는 원본으로 유지하지 않는다.
- 드로우는 보유 ID를 섞고 소비하며, 종류 키는 생성/정의 조회 때 보유 데이터에서 읽는다.
- 상점 구매는 새 보유 개체를 만들고 영혼 개수를 0으로 시작한다. 이미 보유한 같은 종류의 성장량을 복사하지 않는다.
- 전투 ID를 받는 공격·이동 입력은 현재 전투 연결로 해석한다. 패에 생성된 캐릭터도 기존처럼 EntityId를 부여하므로 패→필드 배치/회수 입력은 EntityId로 처리한다. 보유 목록 화면에서 특정 카드를 지목하는 기능이 생기면 CharacterInstanceId를 사용한다. View 작업자에게 이 ID 의미를 전달하되 입력 Buffer의 최종 필드/API 합의와 구분한다.
- 이 ID는 카드 식별을 위한 것으로 Run ID/버전 요구를 새로 만드는 것이 아니다.

### 6.3 저장·발급·연결 계약

- `CharacterInstanceId`는 Contracts/Ids의 별도 readonly 값 타입으로 정의한다. 현재 EntityId처럼 양의 int와 Empty 표현을 사용하되 서로 암시적 변환을 제공하지 않는다. GUID·Run ID·버전 체계는 추가하지 않는다.
- CharacterBuffer의 보유 저장 영역이 보유 ID의 발급 카운터와 보유 레코드를 함께 소유한다. 메인 생성·초기 서브 등록·상점 획득 때 각 카드에 한 번 발급한다. 거절된 획득에서는 보유 레코드·성장 기록이 생기지 않는다. 다장 획득은 필요한 ID 범위와 overflow를 쓰기 전에 검증한다.
- 전투 초기화/정리는 보유 ID 카운터와 영혼 개수를 초기화하지 않는다. 삭제된 ID도 같은 보유 저장 수명 안에서 재사용하지 않는다. 전체 보유 저장소 폐기·저장 복원 정책은 별도 수명 계약이며 이번에 디스크 저장 기능을 만들지 않는다.
- 메인/서브 영혼 원본 키는 CharacterInstanceId다. EntityKey 또는 EntityId로 영혼을 누적하지 않는다.
- 전투 연결의 단일 원본은 전투 상태 저장 영역의 `EntityId → CharacterInstanceId` 매핑으로 둔다. 역방향 조회가 필요하면 그 매핑에서 계산하거나 파생 인덱스를 사용한다. 성장·HP를 이 매핑에 복제하지 않는다.
- 적/구조물은 보유 연결을 갖지 않는다. 연결 없는 개체를 종류 키로 추측하여 아군 보유 카드에 연결하지 않는다.
- 메인 전투 준비와 서브 드로우는 CharacterInstanceId로 보유 레코드를 찾고 정의/성장 값을 읽는다. 기존 factory로 새 전투 EntityId를 만들고, 해당 전투에서 아직 소비되지 않은 보유 카드인지 검증한 뒤 연결을 등록한다.
- 하나의 전투 EntityId는 최대 한 보유 개체에, 하나의 보유 개체는 같은 전투에서 최대 한 전투 EntityId에 연결한다. 같은 카드를 두 번 드로우하여 복제하지 않는다. 패와 필드 이동은 연결을 유지한다.
- 서브 사망만으로 보유 데이터나 영혼을 지우지 않는다. 해당 전투의 사망/결과 정산이 연결을 사용할 수 있도록 연결은 전투 정리까지 유지한다. 추후 전투 중 부활은 같은 개체의 재활성화를 기본으로 하며 새 복제 개체 생성 정책을 암묵적으로 도입하지 않는다.
- 전투 종료 정산이 모두 끝난 뒤 연결을 비운다. 다음 전투에서는 새로운 EntityId와 같은 CharacterInstanceId를 연결한다. 같은 숫자의 EntityId가 재사용되어도 이전 전투 매핑이 남아 있지 않아야 한다.
- 현재 전투 매핑 확인만으로 오래된 View 입력을 식별할 수는 없다. 화면 전환 시 입력 정리는 View/Framework 입력 수명 계약에서 처리한다. 이 매핑을 오래된 요청 차단 장치라고 설명하지 않는다.

이 결정으로 추가되는 것은 보유 ID 값 타입·CharacterBuffer의 발급/개체 기록·현재 전투 연결이다. 전용 ID 서비스, 별도 DeckBuffer, 별도 SoulBuffer를 추가할 근거는 없다.

### 6.4 후속 계획에 포함할 수정 지점

1. Contracts/Ids/CharacterInstanceId.cs 추가와 보유 레코드/조회 API 정의.
2. CharacterBuffer의 메인·서브 개체 등록/영혼 기록/ID 발급 책임 구현.
3. RunCharacterDeckState 및 RunCharacterDeckService의 종류별 장수 원본을 보유 목록 조회로 전환. 기존 counts는 필요하면 파생 조회만 유지.
4. StageDrawSpec/StageDrawState의 추첨 단위를 보유 ID로 변경하고 StageDrawService/StageSetupService에 해당 보유 ID를 전달.
5. 메인 준비·서브 생성 결과의 EntityId와 보유 ID를 현재 전투 저장 영역에 연결. UnitFactory/StructureFactory의 기존 전투 ID 발급 의미는 유지.
6. StageSoulService의 획득 결과를 보유 ID에 귀속하고, 다음 전투 수치 계산도 그 보유 ID의 개수를 사용.
7. 상점 구매 성공 시 새 보유 개체 생성, 종료 시 연결 정리, View에 패/필드 EntityId와 필요한 보유 조회값 제공.

이 목록은 구현 순서의 의존성 근거다. 기존 StageDrawSpec 호출자와 테스트를 실제로 조사하여 정확한 변경 파일과 전환 범위를 구현 계획에 추가한다.

## 7. 영혼 개수 기반 성장과 수치 계산

현재 정책에서는 한 번 획득한 영혼의 효과가 동일하므로 개수만 원본으로 저장한다.

```text
영혼 공격력 보정 = 획득 개수 × 1
영혼 최대 체력 보정 = 획득 개수 × 1
적용 수치 = 기준/지속 성장 수치 + 영혼 보정 + 기타 적용 효과
```

위 식은 영혼의 단순 가산 부분을 설명한다. 다른 효과의 곱연산·상한·우선순위를 임의로 가산으로 바꾸지 않고 기존 규칙을 유지한다.

### 전투 진입

1. 보유 개체와 지속 성장/영혼 개수를 조회한다.
2. 정의·장착 효과에서 해당 전투의 적용 수치를 계산한다.
3. 메인 현재 체력은 보존한다. 최대 체력 재계산을 이유로 완전 회복하지 않는다.
4. 서브 전투 상태를 초기 상태로 준비한다. 영혼 개수는 유지한다. 서브를 증가된 최대 체력으로 완전 회복해 시작하는 것은 초기화 정책의 권장 해석이며 구현 전에 명시 확인한다.
5. 보유 서브 ID를 섞어 최대 3개를 가져온다. 메인 포함 여부를 서브 추첨에 섞지 않는다.

### 전투 중 영혼 획득

1. 실제 도착·생존 여부·획득 대상 영혼을 기존 규칙으로 판단한다.
2. 해당 보유 개체의 최종 영혼 개수와 적용 수치를 계산한다.
3. 현재 체력 변화, 바닥 영혼 제거, 이동 비용 확정을 같은 명령의 결과로 준비한다.
4. 모든 필요한 검증 후 공개 API로 반영하고 완료 결과를 노출한다.

현재 체력 +1 여부는 최대 체력 +1과 별도 정책이다. 현 코드 SoulGrowthMath는 maxHealthBonus와 healAmount를 구분하므로 이를 합쳐서 추정하지 않는다. 확정 전까지 기존 회복 계산을 보존하며 12절의 확인 항목으로 둔다.

### 전투 종료

- 성배의 메인 회복 3을 결과 처리에 한 번 반영한다.
- 메인 현재 체력과 지속 수치, 메인/서브 영혼 개수와 보유 목록을 유지한다.
- 서브 전투 상태, 전투 한정 버프, 바닥 영혼과 해당 전투 연결을 정리한다.
- 이전 전투의 최종 적용 공격력에 영혼 보정을 다시 더하지 않는다. 항상 기준과 획득 개수에서 계산한다.

수치가 나중에 영혼마다 달라지면 개수만으로 과거 성장량을 복원할 수 없다. 그 요구가 실제로 생길 때 누적 성장량이나 종류별 개수로 확장한다. 지금부터 획득 이력 전체를 저장하지 않는다. 동일 개수로 표시와 실제 공격 계산이 같은 값을 사용하도록 한다.

## 8. 기존 Effect 엔진 재사용과 어셈블리 경계

### 재사용할 구조

| 구성 | 유지할 역할 | 변경 지점 |
|---|---|---|
| CombatEffectEngine / ICombatEffectEngine | 규칙 평가·기여 합산·변경 Plan 계산 | 새로 필요한 계산만 좁게 추가 |
| EffectRuleBinding / EffectOriginKey | Rule·출처·기대 상태 연결 | 상태 공급원을 Buffer 기반 입력으로 전환 |
| CombatEffectPlan / 평가 결과 타입 | 계산 결과 표현 | live 소유 객체 참조 필요 여부 점검 |
| CombatEffectCoordinator | owner별 binding 병합·검증·적용 조율 | UnitState에 직접 쓰는 owner 구현 교체 |
| TraitEffectProvider / CategoryEffectOwner | 기존 특성·카테고리 변환과 규칙 | 상태 읽기/쓰기 부분 분리 |
| ArtifactEffectCompiler | 정의 검증·규칙 구성 | 고정 성장 모델과 지원 schema 확인 |
| ArtifactActionProjection / AttackPowerEvaluator | 한 명령의 예상 상태와 공격 보정 합성 | SoulGrowth의 원본을 개수 기반으로 변경 |
| StageSoulService / SoulFieldState | 획득·생성·만료 계산의 근거 | 장기 저장 책임은 Buffer로 이전 |

현 common owner 목록은 Trait/Category이며 Artifact의 별도 경로가 있다. 모든 Artifact를 한 번에 owner로 통합하는 작업은 필수 조건이 아니다. 기존 엔진이 지원하는 부분을 재사용하고 Artifact 특유의 lifecycle을 보존한다.

### 공개 경계

```text
Systems Job / 처리 서비스
  → Buffer ReadSink
  → Core.Contracts 입력
  → Core.Application 명령 처리
  → 기존 Domain Effect 엔진과 규칙
  → Core.Contracts 결과
  → Systems가 Buffer WriteSink로 반영
```

CombatEffectEngine·EffectRuleBinding·여러 Plan은 internal이며 Domain 타입이다. Systems가 이 타입들을 직접 사용하려고 Domain 참조나 public 노출을 대량 추가하지 않는다. 기존 ICombatEffectOwner도 내부 인터페이스이므로 Systems에서 바로 구현하는 것으로 계획하지 않는다.

권장 연결은 Application 안의 adapter가 Contracts 입력/결과를 기존 내부 규칙에 맞게 변환하는 방식이다. 반복 상태 조회가 실제로 필요하면 Contracts에 좁은 상태 포트를 두고 Systems adapter로 구현한다. 포트도 Domain의 UnitState·IEffectStateSnapshot 등을 공개 경계로 유출하지 않아야 한다.

Core 계산에 필요한 한 명령의 복사본/예상 상태는 허용한다. 장기간 살아 있는 UnitState와 Buffer에 가변 상태를 복제해 상호 동기화하는 것을 최종 구조로 삼지 않는다. Rule registry·정의·불변 compiled loadout은 상태 원본과 구분하며 수명 동안 재사용 가능하다.

### Plan 명칭

StagePlan 등 Framework Plan 이름과 CombatEffectPlan 이름은 유지한다. 문서와 계획에서 각각 “작업 구성 Plan”, “Effect 변경 Plan”으로 구분한다. EffectChangeSet으로의 전체 rename은 이번 이전의 선행 작업이 아니다.

## 9. 갱신 API와 초기화 정책

이 절은 의미 계약이며 최종 메서드 서명이 아니다.

- 조회: 보유 ID별 캐릭터/영혼 개수, 전투 ID별 현재 캐릭터, 현재 전투 효과를 조회한다.
- 획득: 보유 개체를 추가하고 안정적인 ID를 할당한다. 같은 종류의 개체를 합쳐 성장 정보를 잃지 않는다.
- 명령 결과 반영: 여러 캐릭터·효과의 계산 결과를 검증하고 반영한다. ReadSink에서 얻은 객체를 외부가 직접 수정하지 못하게 한다.
- 전투 준비/정리: 보존할 보유·메인 체력·영혼 개수와 지울 전투 상태의 범위는 Buffer 측 API가 소유한다.
- View 조회: 현재 체력/수치/위치/영혼 표시 및 필요한 효과 결과를 Buffer로 제공한다. 오래된 불변 객체를 들고 있으면 갱신되지 않으므로 조회 시점을 View와 조율한다.

현재 CharacterBuffer의 Add/Clear를 무조건 삭제하거나 호환 wrapper를 추가하지 않는다. 실제 호출자를 조사하고 전투 정리에 전체 Clear가 사용되지 않도록 좁은 API로 대체한다. BufferSystemGroup.ResetRunProgress는 과거 재시작 경로이며 이 설계를 이유로 호출 범위를 넓히지 않는다.

여러 Buffer가 바뀌는 공격은 관련 결과를 준비하고 검증한 뒤 반영한다. 기존 코드가 보장하던 거절 시 무변경과 한 명령 내 결과 일관성을 유지한다. 이것을 위해 새로운 범용 트랜잭션/락/Run 버전 체계를 도입하지 않는다. 기존 revision/expectation은 단순히 “버전 금지”로 해석해 일괄 삭제하지 말고 이전 경로에서 어떤 보장을 대신하는지 확인한다.

## 10. 대표 실행 흐름

### 공격·사망·승리

입력의 공격자/조준값 → 현재 Buffer와 적용 효과 상태 조회 → Core 공격 계산 및 Effect 변경 Plan → 피해·사망·반격·영혼 생성·승리·성배 회복까지 해당 명령 결과 준비 → 검증 → 상태 반영 → 처리 결과 노출.

동일 공격의 공격자 사실을 사용하며 전역 LastAttacker를 새로 두지 않는다. 반사 피해가 재반사를 발생시키는지 등은 기존 가갑 방어 조건을 보존한다. View는 이 후속 규칙들을 개별 순서로 호출하지 않는다.

### 이동·영혼 획득

입력의 유닛/목표 칸 → 이동 계산 → 실제 최종 도착과 영혼 획득 판정 → 성장 개수·현재 체력·바닥 영혼 제거·이동 정산 결과 준비 → 반영. Preview는 획득 예상 결과를 보여줄 수 있지만 영혼 개수나 바닥 상태를 변경하지 않는다.

### 상점·회복

상점 구매는 보유 목록에 새 카드 추가 → 이후 전투 추첨 대상에 포함한다. 회복 스테이지는 같은 메인의 현재 체력 원본을 갱신한다. 전투별 임시 객체만 변경하고 보유 원본과 연결하지 않는 경로를 남기지 않는다.

## 11. 임시 아티팩트 기획의 적용 범위

| 효과 | 이번 설계에서의 처리 |
|---|---|
| 성배 | 사용자 최신 확정: 메인 회복 3. 기존 compiler/rule 재사용 |
| 가갑 | 기존 반격 규칙과 상태 입력 재사용. 임시 텍스트의 수치 3으로 자동 변경하지 않음 |
| 영혼 | 지속 획득 개수로 변경. 바닥 수명과 획득 후 성장 수명은 별개 |
| 최후의 저항 | 기존 배치 생존 수 기반 계산 재사용. 성장 수치에 누적 저장 금지 |
| Rider 3×3 오라 | 향후 조건부 최대 Action 계산 사례로 설계 검토. 기존 compiler는 category target/exclusion을 제한하므로 현재 지원 완료로 간주하지 않음 |

Rider를 이유로 모든 문자열 statId를 해석하는 범용 엔진을 추가하지 않는다. 실제 구현 때 필요한 capability와 입력 context만 확장한다.

## 12. 미확정 규칙과 영향 범위

오푸스는 아래 사항을 임의 확정하거나 이미 승인된 요구처럼 계획에 넣지 않는다. 관련 기능을 구현할 단계에서만 확인하며 전체 설계를 중단할 필요는 없다.

| 항목 | 현재 근거/권장 처리 | 막히는 구현 범위 |
|---|---|---|
| 영혼 획득 시 현재 HP도 +1인가 | 기존 코드는 최대 HP 증가와 회복량을 별도로 계산. 기존 정책 보존 후 확인 | 영혼 획득 체력 결과 |
| 5턴의 경계 | 현재 SoulFieldState는 unit-turn 카운트 형태. 기존 테스트는 생성 후 countdown 순서를 검증 | 수명 정책 변경 시 |
| 서브 다음 전투 시작 HP | 증가된 최대 HP까지 회복하는 해석 권장, 사용자 명시 확인 필요 | 전투 초기화 최종 기대값 |
| 강제 이동 획득 | 현재 테스트는 생존한 강제 이동의 최종 도착 획득을 지원 | 새 기획이 이를 바꿀 경우 |
| 여러 영혼/아티팩트 중첩 | 고정 단위 영혼을 여러 개 획득한 것으로 개수 증가 가능. 같은 출처 중복 생성 규칙은 기존 테스트 확인 | 중첩 설정 변경 |
| 아티팩트 해제 후 영혼 성장 | 이미 획득한 성장 유지가 권장. 제거 정책은 확정되지 않음 | 전투 외 장착 변경 |
| Rider 최대 Action 증가/감소 | 현재 Action도 즉시 증가하는지, 감소 시 clamp, 중첩·범위 경계 미정 | Rider 구현 |
| 최대 HP 임시 효과 해제 | 현재 HP clamp/체력 비율 유지 등의 정책 미정 | 임시 최대 HP 효과 |
| 메인 사망 후 전체 기록 | 이 문서에서 새 Run 흐름을 추가하지 않음 | 사망 이후 재시작 |
| View 입력/결과 계약 | 전달 문서에 제안만 존재 | 실제 UI 명령 연결 |

한 유닛의 턴 종료에는 Action 회복뿐 아니라 기존 이동 정산·효과 상태 전이가 있을 수 있다. 사용자가 “추가로 생각한 턴 종료 효과가 없다”고 한 것을 기존 필요한 내부 정리를 삭제하라는 의미로 해석하지 않는다.

## 13. 후속 구현 계획의 권장 작업 단위

각 단계는 파일 목록, 전후 원본, 호출자 교체, 대표 테스트, 완료 조건, 다음 단계 의존성을 명시한다. 테스트가 통과하는 중간 상태인지 의도적인 미완성 단계인지 구분한다.

1. **식별/상태 계약:** 6절에서 채택한 CharacterInstanceId + 전투 EntityId 연결을 구현할 최소 데이터, 영혼 개수 저장, 공개 Contracts와 Systems 경계를 구체화. ID 대안 선택은 완료되었으며 이 단계에서 재결정하지 않는다. 전투 전체 필드를 투기적으로 추가하지 않는다.
2. **보유·상점·드로우:** 종류별 장수 원본을 보유 개체로 전환. 같은 종류 카드의 개별 성장과 전투별 연결을 보존. 최초 드로우는 메인+최대3 정책 검증.
3. **수치·초기화:** 개수 기반 영혼 수치 계산, 메인 체력 보존, 서브 전투 초기화와 지속 성장 보존. 계산값 중복 누적 차단.
4. **Effect 상태 연결:** Trait/Category/Artifact의 읽기와 apply 경로를 Buffer 기반으로 전환. engine/rule을 가능한 한 유지하고 Application adapter를 추가.
5. **대표 전투 명령:** 공격과 이동/영혼 획득 각각에 대해 영향을 받는 상태 전체를 이전. HP만 새 Buffer로 쓰고 사망/점유/효과는 구 Core 원본으로 두는 상태를 완료로 보고하지 않음.
6. **종료·다음 전투:** 성배 회복 한 번, 서브 사망 후 재사용, 같은 카드 영혼 지속, 바닥 효과 제거, 다음 전투 재계산 검증.
7. **View 계약 통합 및 구 경로 정리:** 받은 입력 계약으로 연결하고 실제 소비자가 사라진 구 상태/호스트만 제거. 구 Run/StageMap은 의존성을 별도로 확인.

단계 4~5의 범위가 크면 명령별로 나누되 한 명령이 읽고 쓰는 상태의 폐쇄된 집합을 먼저 확보한다. 임시 전환 중 구 경로와 새 경로를 서로 다른 테스트/진입점으로 유지할 수 있으나, 같은 플레이 세션에 두 가변 원본을 동시에 활성화하지 않는다. 이 전환 상태와 제거 조건을 계획에 적는다.

## 14. 검증 기준

불필요하게 같은 사례를 복제하지 않고 대표 경계로 검증한다.

| 대표 사례 | 확인할 결과 |
|---|---|
| 같은 종류 카드 A/B 중 A만 영혼 획득 | 다음 전투에서도 A만 성장, 전투 EntityId 재배정에 영향 없음 |
| 서브 사망 후 전투 종료/재진입 | 보유·영혼 유지, 전투 사망/위치/임시 효과 초기화 |
| 영혼 N개로 반복 전투 진입 | 매번 +N 적용, +2N/+3N 누적 없음 |
| 메인 부상 상태로 다음 스테이지 | 현재 HP 보존, 최대 HP 재계산만으로 완전 회복하지 않음 |
| 성배 승리 처리 재호출 | 회복 3이 한 번만 적용, 최대 HP 초과 없음 |
| 최초 서브 0/2/4개 | 각각 0/2/3개 추첨, 같은 보유 ID 중복 없음 |
| 영혼 미리보기/거절된 이동 | 영혼 개수·바닥 영혼·이동 비용 원본 변경 없음 |
| 실제 영혼 획득 이동 | 성장과 바닥 제거, 환급 불가 이동 정산이 함께 반영 |
| 최후의 저항 조건 변화 | 현재 수치 재계산, 영구 성장으로 누적되지 않음 |
| 여러 owner 중 하나 검증 실패 | 다른 owner만 먼저 적용되지 않음 |
| 상점 동일 종류 추가 획득 | 새 보유 ID, 기존 카드 성장 복제 없음 |
| 전투 A/B에서 같은 숫자의 EntityId 재사용 | 이전 연결이 제거되고 현재 보유 ID에만 성장 반영 |
| 같은 보유 카드의 중복 연결/드로우 | 두 번째 생성/연결 거절, 기존 보유/전투 상태 무변경 |
| 적/구조물 EntityId와 보유 ID의 숫자가 우연히 같음 | 타입/연결 기준으로 구분, 보유 성장에 영향 없음 |
| 전투 정리와 신규 카드 구매 | 기존 보유 ID·영혼 유지, 새 ID가 기존 카드와 충돌하지 않음 |

기존 참조 테스트: ArtifactSoulCollectionTests, ArtifactSoulLifecycleTests, ArtifactSoulStateTests, ArtifactActionProjectionTests, ArtifactEffectsIntegrationTests, ArtifactHealingChaliceTests, ArtifactLastStandCombatTests, ArtifactOutcomeAtomicityTests, ArtifactLayoutLeaseTests, CombatEffectOwnerSeamTests, CombatEffectEngineTests, RunCharacterDeckTests.

예전 “영혼 성장은 Stage 한정” 기대는 새 요구와 충돌할 수 있으므로 변경 이유를 명시한다. 기존 공격/이동/방어/관통/환급/강제이동 결과를 무관하게 바꾸지 않는다.

프로젝트에 ProjectL.Tests.Core.csproj와 Framework/StageComposition 테스트 프로젝트가 있다. 구현 후 해당 csproj의 현재 생성 상태와 테스트 포함을 확인하고 관련 빌드를 수행한다. dotnet build 성공은 Unity Test Runner 통과를 뜻하지 않는다. 실행된 fixture·통과 수·미실행 Unity/View/PlayMode 범위를 분리해 보고한다. 이 설계 작성 중 빌드나 테스트는 실행하지 않았다.

## 15. 오푸스에게 요청할 산출물

이 설계와 현재 HEAD 차이를 재확인한 뒤 구현 계획을 작성한다. 계획만 작성하는 요청이면 코드 구현·커밋·View 문서 수정까지 진행하지 않는다.

- 단계별 목표, 대상 파일/타입, read/write 호출자와 제거할 구 원본
- 최종 Buffer 소유표와 Contracts 타입/API 초안
- 확정된 CharacterInstanceId/EntityId 분리 결정에 따른 발급·연결·획득/드로우 변경 상세
- 영혼 개수 저장/재계산/획득/초기화 흐름
- 기존 Effect 엔진 재사용 부분과 상태 owner adapter 변경 상세
- 내부 타입 접근성과 asmdef 경계를 유지하는 방법
- 대표 회귀 테스트 및 테스트 실행 방법
- 미확정 게임 규칙을 물어야 하는 정확한 단계와 그전까지 가능한 작업
- 단계 종료 시 실제 플레이 연결 수준 및 View/Framework 작업자 인계 사항

새 Buffer가 필요하다고 판단하면 “기존 어떤 데이터/API로 표현이 불가능한가”를 적는다. 새 범용 엔진·global registry·Run 버전·일반 transaction 기반을 추가하는 계획은 이 설계의 기본안이 아니다. 기존 revision/lease 검증의 제거도 별도 근거 없이 수행하지 않는다.

## 16. 코드 지도와 참고 자료

아래 상대 링크는 저장소에서 문서를 열 때 유효하다. 파일만 전달할 경우 경로를 검색해서 확인한다. 문서의 핵심 판단은 본문에 포함되어 있다.

### Systems / Framework

- [CharacterData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterData.cs), [CharacterBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterBuffer.cs)
- [ArtifactData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactData.cs), [ArtifactBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactBuffer.cs)
- [BufferSystemGroup](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs), [BufferSystemFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs)
- [ChapterProgressionCoordinator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs): 기존 PostBindService → Contracts 계산 → Buffer 기록 사례
- [StagePlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs), [StageJob](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs)
- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [구조 문서](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [개발 지침](../framework/Framework%20Development%20Guidelines.md)

### Core — 아래 경로의 기준

`Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`

| 경로 | 확인할 책임 |
|---|---|
| Contracts/Ids/EntityId.cs | 현재 전투 식별 타입 |
| Application/Factories/EntityIdGenerator.cs | ID 발급, 1부터 시작 |
| Domain/Run/RunCharacterDeckState.cs | 종류별 장수 원본 |
| Domain/Draw/StageDrawState.cs | 종류 키 기반 shuffle/커서 |
| Application/Run/RunCharacterDeckService.cs | 덱 변경·StageDrawSpec 구성 |
| Application/Run/RunProgressionService.cs | main checkpoint와 기존 Run/Stage 연결 |
| Application/Run/RunShopService.cs | 구매 결과와 덱 변경 |
| Application/Run/RunRecoveryService.cs | 회복 선택과 메인 체력 |
| Application/StageRun/StageSetupService.cs | 드로우한 종류로 Unit 생성 |
| Application/StageRun/StageDrawService.cs | 자동/수동 드로우와 패·재화 조건 |
| Application/StageRun/StageSessionFactory.cs | 전체 조립, ID/state/effect owner 생성 |
| Application/StageRun/StageSession.cs | Stage 상태 참조와 dispose guard |
| Application/StageRun/StageCombatFacade.cs | 공격/이동/턴 종료/승패 연결 |
| Domain/Entities/UnitState.cs | Action, 이동 체크포인트, Trait/Category, SoulGrowth |
| Domain/CombatEffects/CombatEffectEngine.cs | 기존 효과 평가/Plan 계산 |
| Domain/CombatEffects/ICombatEffectEngine.cs | 지원 capability 확인 |
| Domain/CombatEffects/EffectRuleBinding.cs | 출처·Rule·상태 입력 |
| Domain/CombatEffects/Transitions/CombatEffectPlan.cs | 효과 변경 결과 |
| Application/Combat/Effects/ICombatEffectOwner.cs | 기존 내부 owner 경계 |
| Application/Combat/Effects/CombatEffectCoordinator.cs | owner 병합·전체 검증·적용 |
| Application/Combat/Effects/CombatEffectOrchestrator.cs | Unit 정의/상태에서 계산 context 구성 |
| Application/Combat/Traits/TraitEffectOwner.cs | UnitState resolver와 Trait apply |
| Application/Combat/Categories/CategoryEffectOwner.cs | Category 상태 연결 |
| Contracts/Common/ArtifactEffectData.cs | 실행 상태를 포함하지 않는 효과 정의 |
| Application/Artifact/ArtifactEffectCompiler.cs | 지원 schema 제한, 성배/LastStand 규칙 구성 |
| Application/Artifact/StageArtifactLifecycleService.cs | activation과 Stage 결과 처리 |
| Application/Artifact/StageArtifactLoadoutSession.cs | loadout 수명 연결 |
| Application/Artifact/ArtifactActionProjection.cs | 명령 내 예상 상태 합성 |
| Application/Artifact/ArtifactAttackPowerEvaluator.cs | 영혼/LastStand 공격 보정 |
| Application/Artifact/StageSoulService.cs | 영혼 획득·생성·수명 연결 |
| Domain/Artifact/SoulGrowthState.cs | 현재 bonus 누적과 HP 계산, count 기반 변경 대상 |
| Domain/Artifact/SoulFieldState.cs | 바닥 영혼과 countdown/collection 계획 |

테스트 경로: `Assets/Tests/EditMode/`의 14절 fixture 파일. 모든 .meta와 asmdef 이름/GUID를 보존하고 전체 파일 이동을 전제하지 않는다.

### 과거 문서와 임시 입력

- 기존 Effect/Artifact 통합 설계 (Git 이력: `2026-09-16-combat-effects-artifact-integration-design.md`)
- 기존 Artifact Stage 통합 설계 (Git 이력: `2026-09-17-artifact-effects-stage-integration-design.md`)
- 기존 Category Effect API 설계 (Git 이력: `2026-09-20-category-effect-api-refactor-design.md`)
- 기존 덱/드로우 설계 (Git 이력: `2026-09-18-run-character-deck-stage-draw-design.md`)
- 기존 상점 설계 (Git 이력: `2026-09-19-sub-character-shop-stage-design.md`)
- 사용자 제공 `CharacterBuffer.txt`: 불변 CharacterData/TryGet/Apply 검토 초안. 현재 구현으로 간주하지 않음.
- 사용자 제공 `아티팩트 효과 (임시).txt`: 성배/가갑/영혼/LastStand/Rider 예시. 성배와 영혼 지속성은 최신 대화로 수정됨.

과거 문서의 Run ID/버전, Stage 한정 영혼 성장, View Snapshot 공급 등을 최신 요구로 되살리지 않는다. 구 구현에서 유효한 계산과 안전 조건은 현재 호출 관계를 확인해 재사용한다.
