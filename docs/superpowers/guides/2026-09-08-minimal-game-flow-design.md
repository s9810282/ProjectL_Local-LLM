# 최소 GameFlow와 아키텍처 연결 경계 설계

- 작성일: 2026-09-08
- 상태: 검토용 초안. 아래의 확정 대화 내용과 설계 제안·기획 가정을 구분한다.
- 대상: 현재 `Assets/Scripts/ProjectL/Core`의 Application / Contracts 및 필요한 내부 연결
- 이번 산출물: 설계문서. 런타임 구현, View 변경, 브랜치 이동은 수행하지 않는다.
- 검토 근거: 현재 Core 소스, 이번 대화, 사용자가 제공한 아키텍처 발동 조건, 「스테이지, 맵.pptx」 8~19쪽

## 1. 목적과 합의된 방향

현재 전투 View가 사용하는 `StageSession`, `StageSession.Combat`, 기존 Result / Snapshot을 유지하면서 스테이지 사이의 진행을 Application에서 조율한다.

구현 순서는 **작지만 완결된 GameFlow → 아키텍처 구현·연결 → 필요한 게임 흐름 확장**이다. 첫 단계만으로 스테이지 선택, 생성, 플레이, 결과 확인, 다음 선택 또는 Run 종료를 연결할 수 있어야 한다.

GameFlow는 프레임마다 실행하는 Update 루프가 아니다. 진행 요청을 검증하고 Session의 생성·전환·종료 순서를 관리하는 객체다. 전투 중 이동·공격·턴 처리의 새 필수 경유지가 되지 않는다.

합의된 제약:

- 기존 전투 API 사용 방식과 Core / View 경계를 유지한다.
- GameFlow는 개별 기능을 대체하지 않고 연결한다.
- Deployment / Combat과 밤 시간은 기존 Stage가 관리한다.
- 아키텍처 배치 구성과 전투용 런타임 상태의 수명을 분리한다.
- 플레이어 재화는 같은 Run의 스테이지/노드 사이에서 유지한다. 재화를 사용하지 않는 플레이 흐름도 정상 지원한다(사용자 추가 요구).
- 아키텍처 전용 Resolver를 향후 도입하며, 기존 Trait 인터페이스의 전면 범용화는 선행 조건으로 삼지 않는다.

## 2. 현재 Core에서 확인한 경계

| 현재 요소 | 현재 책임 | 이번 설계와의 관계 |
|---|---|---|
| `StageMapSession` | 맵 조회와 연결된 노드 선택 | GameFlow가 전용 인스턴스를 소유하고 진행 명령을 조율 |
| `StageMapRunState.TrySelectNode` | 선택 성공 시 `CurrentNodeId` 즉시 변경 | Session 생성 실패와 맵 진행을 분리하지 않도록 내부 계획/확정 경계 필요 |
| `StageSessionFactory.Create` | 설정·유닛/건물 카탈로그로 Stage 조립 | 기존 구현 재사용, 기존 공개 오버로드 유지 |
| `StageSession` | 생성·배치·전투 진입·Snapshot | 기존 View 진입점 유지 |
| `StageCombatFacade` | 이동·공격·턴·기믹·승패 조율 | GameFlow가 동일 명령을 다시 감싸지 않음 |
| `StageSnapshot.Outcome` | 전투 종료 여부와 원인 | GameFlow 결과 단계의 판단 근거 |
| `StagePhase` | Deployment / Combat | GameFlow 상태에 복제하지 않음 |

현재 `StageMapSession`은 활성 전투나 결과 확인 여부를 모른다. `StageSessionFactory` 역시 선택된 노드와 Run 진행을 모른다. 이 사이의 책임을 이번 GameFlow가 담당한다.

현재 `StageSnapshot`에는 Run 내 스테이지 진입 식별자가 없다. `EntityId`나 `StageKey`는 서로 다른 Session의 늦은 요청을 구분하는 용도로 사용하지 않는다.

## 3. 접근 방식 비교와 선택

| 방식 | 이점 | 비용/한계 | 판단 |
|---|---|---|---|
| 외부 호출자가 기존 Session을 계속 연결 | 당장 변경이 가장 적음 | 아키텍처 구성 전달과 중복 결과 처리를 외부에서 계속 관리 | 단독 Stage 개발에는 유지 가능 |
| GameFlow가 모든 전투 요청까지 중계 | 하나의 호출 입구 | 기존 View 변경이 크고 Stage와 책임 중복 | 채택하지 않음 |
| GameFlow가 Session 생명주기만 조율 | 기존 전투 API 유지, 진행 규칙 집중 | 생성·선택·결과 연결부의 수정 필요 | 채택 |

`Core.Application.GameFlow`에 추가한다. 새 어셈블리, Unity 의존성, 전역 Singleton, 모든 Domain State를 담는 글로벌 객체는 만들지 않는다.

## 4. 최소 범위와 기획 가정

### 포함

- Run 하나의 맵과 현재 Stage 소유
- 연결된 노드 선택과 Stage 생성의 원자적 진입
- 현재 Stage와 진입 식별자의 제공
- Stage 결과 조회 및 1회 소비
- 일반 스테이지 승리 후 다음 선택
- 최종 노드 승리 후 Run 승리
- 패배 후 Run 종료: **사용자 확인 전 설계 가정**
- 동일 명령 재호출, 잘못된 순서, 오래된 진입 ID에 대한 거절
- Run 단위 플레이어 재화 보관·조회·증감 기반 및 노드 사이 잔액 유지
- 재화 초기값 생략 및 재화 사용 콘텐츠 미연결 상태에서도 정상 진행
- 기존 전투 API를 사용하는 View의 이관 계약과 검증 항목

### 제외

- 타이틀, 씬 로딩, 화면 애니메이션, 입력 잠금의 Unity 구현
- 실제 아키텍처 편집판, 효과 Resolver, 지속 효과, 저장소 구현
- 이벤트 스테이지 전용 Session, 보상 선택/지급, 상점
- Run 저장/불러오기, 중도 포기, 같은 노드 재도전, 진행 중 Run 교체
- 스테이지 간 플레이어 유닛·체력 계승 규칙
- Run 종료 후 재화 영구 저장·계승과 구체적인 재화 명칭·획득량·가격·입장료
- 모든 전투 요청을 GameFlow로 변경하는 작업

최소 범위에서는 일반/보스 노드를 모두 기존 전투 `StageSession`으로 구성한다. 이벤트 스테이지는 향후 별도 콘텐츠 흐름으로 추가한다.

플레이어 초기 유닛 생성과 본부 배치는 현재 Stage API 사용 방식을 유지한다. 이전 스테이지 병력·체력의 자동 계승을 구현했다고 간주하지 않는다. 실제 연속 플레이에 계승이 필수라면 별도의 Run 데이터 전달 설계가 구현 범위에 추가되어야 한다.

## 5. 상태 모델과 소유권

### 공개 상태: `GameFlowPhase` (신규 제안)

| 상태 | 의미 | 허용되는 주요 요청 |
|---|---|---|
| `StageSelection` | 활성 Stage 없음, 다음 노드 선택 가능 | 맵 조회, `TryEnterStage` |
| `StageActive` | 현재 Stage의 Outcome이 InProgress | 기존 Stage / Combat 명령 |
| `StageResult` | 현재 Stage가 종료됨, 결과 확인 대기 | 최종 상태 조회, `TryCompleteStage` |
| `RunEnded` | 마지막 결과 처리로 Run 종료 | Run 결과 조회 |

`StageActive` 안에 Deployment와 Combat을 모두 포함한다. 낮밤 전환 시 GameFlowPhase는 변경하지 않는다.

GameFlow 내부에는 선택 대기 / Stage 보유 / Run 종료라는 진행 정보만 저장한다. Stage를 보유한 경우 공개 `StageActive` / `StageResult`는 현재 Stage의 `Outcome`에서 계산한다. 별도의 승패 복사본을 계속 동기화하지 않는다.

`GetSnapshot()`은 조회만 한다. 결과 소비, 보상 지급, Stage 전환을 실행하지 않는다. 공격 직후 GameFlow 명령이 없어도 다음 조회에서 StageResult가 보여야 한다. 이를 위해 공격 API를 감싸거나 Update에서 상태를 동기화할 필요가 없다.

### 소유권

재화 계승은 확정된 포함 범위다. 현재 요구는 같은 Run 내 유지로 해석한다. 새 Run 자동 이월이나 영구 재화 여부는 별도 기획이다. 이벤트/상점에서 재화를 사용할 수 있다는 확장 방향만 반영하고, 해당 노드 입장 자체에 반드시 비용이 있다고 가정하지 않는다.


- `GameFlowSession`: 자신이 생성한 전용 StageMapSession, 현재 StageSession, 진입 ID, 완료된 마지막 결과, Run 종료 결과.
- `PlayerWalletState`(신규 Domain 내부 타입): Run 동안 유지되는 플레이어 재화 잔액. GameFlowSession이 소유하고 Application의 재화 처리 기능을 통해 변경한다. Stage 생성·종료마다 재생성하거나 잔액을 복사·합산하지 않는다.
- 기존 StageSession과 내부 서비스: 전투 상태 전체.
- 향후 영구 프로필/저장 계층: 아키텍처 보유·배치·강화 데이터. Run이 끝났다고 삭제하지 않는다.
- 향후 Stage 내부 아키텍처 실행부: 해당 Stage의 발동 횟수·쿨다운·지속 효과.

GameFlow가 소유한 가변 StageMapSession은 View에 직접 노출하지 않는다. 기존 공개 StageMapSession API는 단독 맵 개발과 테스트용으로 유지한다. 실제 Run에서는 맵 선택의 소유자를 GameFlow 하나로 정한다.

## 6. Application / Contracts 제안

이 절의 타입과 메서드는 신규 설계이며 현재 구현된 API가 아니다.

### Application

| 요소 | 제안 계약 |
|---|---|
| `GameFlowSessionFactory.Create` | 맵 생성 설정, 일반/보스 StageKey, Stage 생성용 카탈로그와 선택적인 초기 재화(생략 시 0)를 받아 새 GameFlowSession 생성 |
| `GameFlowSession.GetSnapshot()` | 불변 GameFlowSnapshot 반환 |
| `GameFlowSession.CanEnterStage(nodeId)` | 진행 단계·연결·카탈로그 존재를 검사. 실제 생성 성공을 보장하는 함수는 아님 |
| `GameFlowSession.TryEnterStage(nodeId)` | Stage 생성과 맵 진행을 조율하고 GameFlowCommandResult 반환 |
| `GameFlowSession.TryGetCurrentStage(out entryId, out session)` | StageActive / StageResult 동안만 현재 진입 ID와 기존 StageSession 반환 |
| `GameFlowSession.TryCompleteStage(entryId)` | 실제 종료된 현재 Stage의 결과를 한 번 소비하고 다음 선택 또는 Run 종료 |

새 `StageEntryId`는 진입 인스턴스의 불변 ID로, Run이 달라도 충돌하지 않는 값으로 생성한다(예: Guid 기반 값 타입). StageKey/NodeId와 구별한다. UI 결과 콜백은 자신이 표시하던 진입 ID를 함께 전달한다.

`TryGetCurrentStage`는 Application의 명시적 연결 창구다. Session 객체를 Contracts Result / Snapshot에 포함하지 않는다. View는 진입 성공 후 이 API에서 Session을 받고 기존 전투 Presenter에 전달한다.

### Stage 생성용 데이터

`StageStartSpec`(Contracts, 신규)은 StageConfig와 UnitSpec / StructureSpec 목록을 보관하는 불변 입력이다. 배열 등 변경 가능한 입력 컬렉션은 방어 복사한다.

`StageStartCatalog`(Application, 신규)는 StageKey와 StageStartSpec의 대응을 보관한다. 중복 키, 누락된 맵 사용 키, 잘못된 정적 구성은 가능한 한 GameFlow 생성 시 검증한다. UnitCatalog / StructureCatalog가 담당하는 기존 검증 로직을 재사용한다.

첫 구현은 메모리에 준비된 입력만 사용한다. Addressables나 씬 로딩을 Factory 안에서 실행하지 않는다. 향후 동적 구성/저장은 별도의 Application 입력 구성 경계로 확장한다.

기존 `StageSessionFactory.Create`의 시그니처를 변경하지 않는다. 아키텍처가 추가될 때 별도 입력/오버로드로 확장하고, 기존 호출은 아키텍처가 없는 구성으로 동작하도록 유지한다. 이번 단계에 빈 Architecture 관련 타입이나 사용하지 않는 이벤트 버스를 선행 구현하지 않는다.

### Contracts

- `GameFlowSnapshot`: Phase, 기존 형식의 Map Snapshot, 현재 진입 ID/StageKey/NodeId(없으면 nullable), 현재 StageSnapshot(보유 중일 때), 마지막 완료 Stage 결과, Run 결과, 불변 PlayerWalletSnapshot.
- `PlayerWalletSnapshot`: 현재 잔액 Balance. 재화가 없는 정상 상태도 null 대신 Balance=0으로 표현한다. 기존 StageSnapshot에 잔액을 중복 추가하지 않는다.
- `CompletedStageSnapshot`: 진입 ID, NodeId, StageKey, 최종 StageOutcomeSnapshot, 최종 StageSnapshot. 완료 당시 값을 보존한다.
- `RunOutcome`: InProgress / Victory / Defeat. StageOutcome과 이름이 비슷해도 별도 의미다.
- `GameFlowCommandResult`: Succeeded, FailureReason, CurrentFlow. 성공/실패 모두 최신 불변 Snapshot 반환.
- 예상 FailureReason: WrongFlowPhase, NodeNotFound, NotConnected, FinalNodeReached, StageConfigurationMissing, StageCreationFailed, NoCurrentStage, StageEntryMismatch, StageNotEnded.

현재 `StageMapNodeSnapshot.IsSelectable`은 그래프상 다음 후보라는 의미로 유지한다. GameFlowSnapshot의 맵 표현에서는 StageSelection 외 단계에 이 값을 false로 투영해 View의 기존 노드 활성화 표현을 재사용할 수 있게 한다. 실제 요청의 허용 여부는 항상 GameFlow가 다시 검증한다. 원본 Domain 그래프를 UI 활성화 때문에 수정하지 않는다.

### 재화의 최소 계약

- 첫 구현은 명칭이 정해지지 않은 정수 잔액 하나로 제안한다. 여러 재화가 확정되면 재화 식별자별 잔액으로 확장하며, 이름·환율·소수 단위는 지금 정하지 않는다.
- Balance와 amount는 음수가 아닌 long으로 표현한다. 초기값 생략은 0으로 처리한다. 명시적으로 전달한 음수 초기값은 잘못된 입력으로 거절한다.
- 내부 재화 처리 기능은 TryCredit(amount) / TrySpend(amount)에 해당하는 명시적인 증감 연산을 제공한다. 잔액 부족, 음수 입력, 증가 시 정수 오버플로를 구별하고 실패 시 잔액을 유지한다. amount=0은 성공하는 무변경 처리다.
- 일반 View에 임의 잔액 설정 API를 노출하지 않는다. GameFlowSnapshot으로 조회하고, 향후 구매·보상·이벤트 Application이 규칙 검증 후 내부 재화 연산을 사용한다. 재화 증감 능력과 콘텐츠별 지급/구매 규칙은 구분한다.
- 기본 스테이지 선택·전투 진입·결과 완료에는 재화 조건이나 자동 증감을 넣지 않는다. 잔액 0, 재화 UI 없음, 상점/이벤트/보상 서비스 없음이어도 정상적으로 Run을 진행할 수 있다. 사용하지 않는 상점 서비스 등을 Factory 필수 의존성으로 요구하지 않는다.
- 같은 Run의 다음 Stage 생성, 낮밤 전환, 결과 화면 표시, 다음 노드 선택에서 잔액을 초기화하지 않는다. RunEnded에서도 최종 잔액을 조회할 수 있다. 새 Run은 명시적인 초기값 또는 기본값 0으로 시작한다.
- 향후 비용이 있는 행동은 비용 검증·대상 처리·잔액 변경을 함께 성공시키거나 모두 유지하도록 설계한다. 차감 후 노드 진입 실패, 결제 후 상품 지급 실패로 잔액만 줄어드는 상태를 허용하지 않는다.
- 구매/보상의 중복 요청 방지는 해당 콘텐츠 거래에서 처리한다. 이번 기본 잔액 연산만으로 중복 지급 방지가 완성됐다고 간주하지 않는다.

재화를 사용하지 않는 것은 정상 진행이다. 향후 정의된 비용을 감당하지 못하는 경우에만 해당 유료 행동을 명시적으로 거절한다. 첫 구현은 잔액 관리 기반을 포함하며, 유료 행동이나 자동 보상 지급은 아직 추가하지 않는다.

## 7. 스테이지 진입 거래

현재 `TrySelectNode`를 먼저 호출한 뒤 Factory를 호출하면 생성 실패 시 노드만 진행한 상태가 된다. 다음 절차로 해결한다.

1. StageSelection인지 검증한다.
2. 현재 위치와 연결을 검증하고, 목표 NodeId / StageKey / 현재 출발 NodeId를 담은 내부 선택 계획을 만든다. 아직 위치를 바꾸지 않는다.
3. 카탈로그에서 입력을 구하고, 기존 StageSessionFactory로 외부에 노출되지 않은 임시 StageSession을 완전히 생성한다.
4. 선택 계획이 여전히 유효한지 검증한다.
5. 같은 단일 스레드 호출에서 맵 위치와 현재 Stage 참조·진입 ID를 확정한다. 이 구간에 외부 콜백, I/O, await를 넣지 않는다.
6. 성공 결과를 반환한 뒤 View가 Session을 받는다.

StageMapSession / StageMapRunState에 내부 Plan / Validate / Commit 경계를 추가한다. 기존 공개 TrySelectNode는 같은 경계를 사용하도록 내부만 정리하여 기존 반환 계약을 유지한다.

실패하면 맵 위치, 현재 Stage, 진행 상태, 마지막 결과, 재화 잔액이 바뀌지 않는다. 성공 시에도 기본 진입은 재화를 차감하지 않는다. 임시 Stage는 외부에서 관찰할 수 없으며 이번 메모리 기반 구현에서는 자원 해제가 필요한 Unity 객체를 갖지 않는다.

정의된 잘못된 입력/구성 실패만 Result로 변환한다. 예상치 못한 프로그래밍 오류를 포괄적인 catch로 성공처럼 숨기지 않는다. 치명적 런타임 오류까지 복구 가능한 거래라고 주장하지 않는다.

선택 노드로 이동한 사실은 클리어 사실과 다르다. 최종 노드에 진입했다는 `IsAtFinalNode`만으로 Run 승리를 선언하지 않는다.

## 8. 결과 처리와 Run 종료

View는 기존 전투 Result를 표시하고 `CurrentStage.Outcome.IsStageEnded`로 결과 연출을 시작할 수 있다. GameFlow는 완료 요청 시 자신이 보유한 Stage를 다시 조회해 최종 상태를 검증한다. View가 전달한 승패 boolean이나 오래된 StageSnapshot은 판단 근거로 받지 않는다.

`TryCompleteStage(entryId)`:

1. 현재 Stage 존재, 진입 ID 일치, Stage 종료 여부를 확인한다.
2. 불변 완료 결과를 만든다.
3. 일반 노드 승리면 StageSelection, 최종 노드 승리면 RunEnded/Victory, 패배면 RunEnded/Defeat로 전환한다.
4. 마지막 완료 결과를 저장하고 현재 Stage 참조를 해제한다. 이후 맵 진행 요청을 허용할지는 3번 결과에 따른다.

처리 중 외부 이벤트/보상 지급/저장을 실행하지 않는다. 반복 완료는 실패하고 결과를 다시 반영하지 않는다. 다음 Stage 진입 후 도착한 이전 ID의 콜백은 StageEntryMismatch로 거절한다. 실패 이유 우선순위는 현재 Stage 없음 → ID 불일치 → 종료 전 순서다.

새 Run 시작은 새 GameFlowSession 생성으로 표현한다. 이번 범위에는 진행 중 객체를 Reset하거나 활성 전투를 강제로 폐기하는 API를 넣지 않는다.

종료된 이전 Stage 참조를 View가 보관하더라도 새 Run/Stage 상태로 연결되지 않는다. 기존 Core의 StageEnded 검사는 유지하며, 기존 명령의 종료 후 변경 차단을 회귀 검증한다. 범위 밖인 중도 포기까지 안전하다고 주장하지 않는다. 중도 포기 추가 시 내부 Session 무효화 규칙이 필요하다.

## 9. View 이관 계약

### 유지

- 기존 StageSession / Combat 명령과 Query / Preview 호출
- 성공/실패 Result의 CurrentStage 반영 방식
- 배치·이동·공격·타임라인의 표시와 연출 책임
- 단독 StageSessionFactory / StageMapSessionFactory를 사용하는 개발 장면과 테스트
- View가 Application과 Contracts만 참조하는 경계

### 실제 Run에서 변경

- 시작 연결부: GameFlow 생성 후 진입 성공 시 Session을 얻어 기존 Presenter에 전달.
- 맵 선택 연결부: 직접 StageMapSession.TrySelectNode 호출 대신 GameFlow.TryEnterStage 사용.
- 결과 연결부: 표시하던 StageEntryId로 TryCompleteStage 호출.
- Stage 교체 시 이전 연출/콜백 취소 또는 진입 ID 확인. 새 Session에는 기존 UI 캐시·EntityId 선택 상태를 재사용하지 않음.

직접 생성 방식과 GameFlow 방식을 같은 Run에서 혼용하지 않는다. 기존 Factory를 남기는 것은 단독 개발 호환성을 위한 것이며, 실제 Run에서 두 개의 진행 소유자를 운영하자는 의미가 아니다.

실제 View 변경 파일 수는 이번 Core 기준 검토로 확정할 수 없다. 전투 View가 Session 주입 방식이면 생성·선택·결과 연결부에 변경을 집중할 수 있다. 여러 스크립트가 직접 Session을 생성한다면 이관 범위가 커진다.

GameFlow는 공통 Core API의 잘못된 직접 호출을 자동으로 차단하는 보안 경계가 아니다. 각 기능의 Phase·Outcome·대상 검증은 유지한다. 예를 들어 CreatePlayerReserveUnit의 전투 중 허용 여부는 별도 기획 규칙으로 정해야 하며 이번 GameFlow 추가만으로 해결되지 않는다.

## 10. 향후 아키텍처 연결

### 생명주기

1. StageSelection에서 영구 배치 구성을 조회·편집한다. 실제 편집 명령의 허용 조건도 Core에서 보장한다.
2. 스테이지 진입 요청에서 이번 Stage에 사용할 구성을 불변 입력으로 확정한다.
3. Stage 생성 과정에서 아키텍처 전투 런타임을 생성한다.
4. 해당 Stage의 시작 처리를 완료한 뒤 Session을 공개한다.
5. 전투 중 발동은 Stage 내부에서 처리하고 기존 Result / Snapshot으로 표시 정보를 전달한다.
6. Stage 종료 시 전투용 상태를 마무리하며 영구 배치 구성은 유지한다.

위 단계는 향후 연결 위치를 정의한다. 첫 GameFlow 구현에서 실제 아키텍처 효과나 저장을 실행하지 않는다.

스테이지 진입 시점에는 현재 구조상 View가 생성·배치할 플레이어 유닛이 아직 없을 수 있다. 따라서 ‘진입 시 배치된 아군을 회복’ 같은 즉시 효과는 대상이 없을 수 있다. 아키텍처 구현 전에 초기 플레이어 준비를 Application으로 옮길지, Stage 진입 효과의 대상/적용 시점을 별도로 정의할지 확정해야 한다. 이를 해결하려고 매번 TryEnterCombat에서 Stage 진입 효과를 반복 실행하지 않는다.

### 제공된 발동 조건의 담당 경계

다음은 연결 방향이다. 의미가 불명확한 항목은 지금 GameFlow의 구현 요구사항으로 고정하지 않는다.

| 사용자 발동 조건 | 담당 | 연결 지점과 확인할 의미 |
|---|---|---|
| 낮에서 밤으로 전환될 때 | Stage | TryEnterCombat의 성공한 Deployment → Combat 전환. 실패/조회는 발동 없음 |
| 밤에서 낮으로 전환될 때 | Stage | EndUnitTurn에서 밤 종료와 Deployment 전환이 확정된 경계. 유닛 회수 전/후 대상 판정은 효과 설계에서 확정 |
| 적 유닛을 처치했을 때 | Stage | 확정된 사망 사실에 공격/효과 원인을 함께 전달. 적 사망과 아군에 의한 처치를 구별 |
| 아군 유닛이 처치되었을 때 | Stage | 아군 사망 확정 시. 공격 외 기믹/지속 피해도 고려 |
| 아군 유닛이 공격을 시도했을 때 | Stage | Preview/대상 선택 제외. 유효한 공격 명령 접수 시점인지 실패 명령도 포함하는지 확인 필요 |
| 아군 유닛이 공격을 받을 때 | Stage | 공격 대상으로 확정된 시점인지 양수 피해 확정 시점인지 확인 필요. 피해 경감이면 적용 전 계산 경계 필요 |
| 적 유닛이 공격을 시도할 때 | Stage | 아군 공격 시도와 같은 명령 의미를 적용하고 Source 진영만 구분 |
| 아군 유닛이 이동을 결정했을 때 | Stage | 성공 TryMove마다인지, 공격 성공/턴 종료에서 Pending 이동을 확정할 때인지 확인 필요 |
| 아군 유닛의 턴 종료 시 | Stage | 유효한 EndUnitTurn의 확정 경계. 같은 호출의 밤 종료 효과와 실행 순서 정의 필요 |
| 스테이지 진입 시 | GameFlow가 시작을 조율, 효과는 Stage | 성공한 진입 인스턴스당 한 번. 화면 재표시/밤 재진입과 구별. 초기 유닛 준비 문제는 위 문단 참조 |

GameFlow는 공격·피격·사망·이동 사건을 전역 방송하지 않는다. Stage 내 Application이 사건 정보를 구성하고 전용 ArchitectureResolver가 조건/대상을 판단한다. Trait와 공유할 효과 계산·적용 기능은 실제 아키텍처 구현에서 추출한다.

현재 이동은 위치와 점유를 바꾸되 공격 성공/턴 종료 전에는 일부 비용 환급이 가능하다. ‘이동 결정’을 무조건 TryMove 성공에 연결하면 위치 조정 반복으로 아키텍처가 여러 번 발동할 수 있다. 이 기획 질문은 반드시 따로 해결한다.

향후 사건에는 필요한 범위에서 Source/Target ID와 진영, 당시 위치, 피해 전후 값, 원인, 순서 식별자를 포함한다. 사망 위치는 제거 전 캡처한다. 단순 사후 알림으로 공격 비용·피해 경감 같은 사전 계산을 대체하지 않는다. 연쇄 효과·동시 사망·승패 판정 순서 및 Preview 일관성은 아키텍처 설계의 범위다.

## 11. 변경 파일 범위 제안

- 신규 Application/GameFlow: GameFlowSession, GameFlowSessionFactory, StageStartCatalog와 진행/진입 조율용 내부 타입, 재화 증감 처리 기능.
- 신규 Domain/Resources: PlayerWalletState와 잔액 검증 규칙. 특정 명칭이나 콘텐츠 규칙에 의존하지 않는다.
- 신규 Contracts 재화 관련: PlayerWalletSnapshot. 내부 증감 실패는 향후 콘텐츠별 공개 Result에 대응시킨다.
- 신규 Contracts: GameFlowPhase, RunOutcome, StageEntryId, StageStartSpec, GameFlowSnapshot, CompletedStageSnapshot, GameFlowCommandResult / FailureReason.
- 기존 Application/StageMap 및 Domain/StageMap: 내부 선택 계획과 확정 경계. 기존 공개 선택 계약 유지.
- 기존 StageSession / Combat / Factory: 최소 GameFlow에서는 공개 API 변경을 요구하지 않음. 필요한 종료 후 방어의 누락이 검증에서 발견되면 별도로 명시하고 수정.
- 테스트: GameFlow EditMode 및 Application / Contracts만 참조하는 View 경계 테스트.
- 인계 문서: 실제 Run 진입·Session 전달·완료 처리 예제 추가. 기존 전투 호출 설명은 유지.

모든 진입점과 내부 타입의 이름은 구현 전 리뷰에서 확정한다. 큰 리팩터링, 새 이벤트 프레임워크, 사용하지 않는 범용 State 머신은 범위에 포함하지 않는다.

## 12. 검증 기준

1. 새 Flow는 StageSelection, 시작 노드, 활성 Stage 없음으로 시작한다.
2. 연결 노드 진입 성공 시 맵 위치·Stage·진입 ID가 함께 확정된다.
3. 잘못된 노드, 카탈로그 누락, Factory의 구성 실패 시 맵과 Flow가 원상태다.
4. StageActive / StageResult에서 다음 노드 진입은 거절된다.
5. GameFlow가 제공한 Session으로 기존 배치·공격·턴·Snapshot 흐름이 실행된다.
6. 전투 종료 후 GameFlow 명령이나 Update 없이 조회만으로 StageResult를 확인한다. 반복 조회는 상태를 변경하지 않는다.
7. 진행 중 Stage 완료 요청은 실패한다. 승패를 View가 임의 지정할 수 없다.
8. 일반 승리 완료는 다음 선택, 최종 노드 승리 완료는 Run 승리, 패배 완료는 Run 패배가 된다(패배 정책 확정 후 반영).
9. 최종 노드에 들어가기만 해서는 Run 승리가 되지 않는다.
10. 완료 반복 및 이전 진입 ID의 늦은 콜백은 다음 Stage에 영향을 주지 않는다.
11. 이전 Snapshot/완료 결과는 다음 전투와 조회 후에도 값이 바뀌지 않는다.
12. 맵 IsSelectable 투영과 CanEnterStage의 단계 제한이 일치한다. 실제 명령이 최종 검증한다.
13. 기존 StageMap 선택 테스트 및 StageSession / CombatViewBoundary 테스트가 유지된다.
14. 종료된 이전 Session의 공개 변경 명령이 새 상태를 만들거나 새 Flow에 영향을 주지 않는다.
15. Contracts가 Application/Domain/Unity 타입에 의존하지 않으며 Application의 noEngineReferences를 유지한다.
16. 초기 재화 생략 시 잔액 0으로 생성되며, 상점·이벤트·보상 서비스나 재화 UI 없이 전체 기본 Run 흐름이 진행된다.
17. 명시한 초기 잔액과 내부 증감 결과는 Stage 완료·다음 노드 진입·낮밤 전환 후에도 유지된다.
18. 재화 증가·소비는 정확한 잔액을 반환하고, 잔액 부족·음수·오버플로 실패는 잔액을 바꾸지 않는다. 0 증감도 정상 처리된다.
19. Stage 진입 실패·결과 완료 반복·Snapshot 조회는 재화를 변경하지 않는다. 과거 Wallet Snapshot은 이후 증감에도 불변이다.
20. RunEnded의 최종 잔액을 조회할 수 있고, 새 Run 초기값 생략 시 이전 잔액이 자동 이월되지 않는다.

새 회귀 테스트는 위 행동 계약을 검증한다. 새 코드의 필드/메서드 존재만 확인하는 테스트로 대체하지 않는다. 이번 문서 작성에서는 런타임 테스트를 실행하지 않는다.

## 13. 구현 순서와 리뷰 항목

1. 패배 정책과 병력·체력 계승 범위를 확인한다. 재화의 Run 내 계승은 확정 범위로 반영한다.
2. GameFlow Contracts / 상태 전환 / 카탈로그 입력을 확정한다.
3. 맵 선택의 내부 계획·확정 경계를 추가하고 실패 원자성을 검증한다.
4. 최소 GameFlow, 결과 1회 소비, Run 재화 상태·조회·내부 증감 기능을 구현한다. 재화 미사용 흐름도 검증한다.
5. 기존 전투 API를 그대로 쓰는 통합 테스트와 View 연결 예제를 제공한다.
6. 실제 Run의 생성·선택·결과 연결부를 이관한다. View 구현은 별도 작업으로 협의한다.
7. 아키텍처 기획의 사건 의미를 확정하고 배치 구성 전달과 대표 효과부터 구현한다.
8. 보상·이벤트·저장·타이틀 등 실제 필요한 흐름을 단계적으로 추가한다.

사용자 확인 사항:

- **이번 Flow 전환에 영향:** 패배 시 Run 종료를 기본으로 둘지, 같은 노드 재도전을 지원할지.
- **확정된 재화 범위:** 같은 Run 내 재화 계승과 미사용 시 정상 진행을 포함한다. 재화 명칭·구체적인 소비 콘텐츠·Run 간 영구 계승은 아직 확정하지 않는다.
- **남은 연속 플레이 데이터 범위:** 병력/체력 계승을 이번 작업에 포함할지. 초안은 기존 Stage 초기화 방식을 유지하고 이 둘의 자동 계승은 제외한다.
- **아키텍처 구현 전 확정:** 공격 시도/피격/이동 결정의 의미, Stage 진입 효과와 플레이어 초기 준비의 순서, 밤 종료 시 대상 조회 시점.

이 초안은 미확정 아키텍처 의미를 GameFlow 상태나 API에 선반영하지 않는다. 설계 검토 후 승인된 범위로 구현 계획을 별도 작성한다.
