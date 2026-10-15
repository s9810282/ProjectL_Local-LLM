# DB-SO 작업 develop 반영 전 — Core 작업자 확인 요청

> `feature/Yonggeun/DB_SO`를 develop에 넣기 전에 Core 작업자에게 알리고 확인받을 내용을 정리했다.
> 상세 내용은 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md)(이하 "전달 문서")에 있고, 이 문서는 그 요약이자 대화용 메모다.

- 작성일: 2026-09-26
- 작성: View 작업 (용근)
- 받는 사람: Core 작업자
- 대상 브랜치: `feature/Yonggeun/DB_SO` (`f860909`, develop `8a5e703`을 이미 merge함)

## 1. 한 줄 요약

DB-SO 재구성 작업을 develop에 넣으려고 한다. **Core 파일 5개와 Core가 쓰는 Stage 데이터 API에 손댄 부분이 있다.** 현재 Core 브랜치와 충돌은 없는 것으로 확인했지만, 방향이 맞는지 먼저 확인받고 싶다.

## 2. develop에 들어가면 Core 쪽에서 바뀌는 것

### 2.1 Core 파일을 직접 수정한 것

| 파일 | 변경 | 커밋 |
|---|---|---|
| `CoreSystem/StageSystem/StageGeneratorSystem.cs` | using 1줄 + **노드 기록 때 `StageType`, `MapRole`을 함께 기록** (Buffer B1) | `723c6ab`, `39b634f` |
| `CoreSystem/StageSystem/StageServiceCollection.cs` | using 1줄 | `723c6ab` |
| `CoreSystem/StageSystem/StageServiceManager.cs` | using 1줄 | `723c6ab` |
| `BufferSystems/Stage/StageProgressData.cs` | `StageType`, `MapRole` 프로퍼티와 생성자 기본값 인자 추가 (Buffer B1) | `39b634f` |
| `BufferSystems/Stage/StageProgressBuffer.cs` | `SetClear`에서 새 필드 2개도 복사 (Buffer B1) | `39b634f` |

- using 3줄은 `StageType`/`StageMapRole`이 `DataBaseSystem.Data.Common`으로 옮겨져서 컴파일에 필요해 넣은 것이다. 로직 변화는 없다.
- `CoreCaculator`(Contracts/Domain/Application)는 건드리지 않았다.

### 2.2 Buffer B1 — 사용자 지시로 View 쪽에서 구현한 Core 로직

스테이지 맵 View가 노드 외형(일반/보스)을 고르려면 노드 종류가 필요한데, View는 InGameGroup을 읽지 않으므로 Core가 Buffer에 같이 써 주는 방식으로 했다.

```csharp
public StageProgressData(int nodeId, int stageKey, int depth, int widthIndex,
    IEnumerable<int> nextNodeIds, bool isClear = false,
    StageType stageType = default, StageMapRole mapRole = default)
```

- 기존 5·6인자 호출은 그대로 컴파일된다(기본값 인자).
- 생성 알고리즘과 난수 사용 순서는 바꾸지 않았다.
- **시작 노드(StageKey 0)는 두 필드가 기본값**(`Event`/`Regular`)이다. 의미 없는 값이라 `StageKey == 0`을 먼저 확인하는 규칙으로 했다.

자세한 내용은 전달 문서 §5.3·§6에 있다.

### 2.3 Core가 쓰는 Stage 데이터 API 변경

| # | 변경 | Core 영향 |
|---|---|---|
| M1 | `StageType`, `StageMapRole` 네임스페이스 → `ProjectL.Systems.DataBaseSystem.Data.Common` | 새 코드에서 쓸 때 using만 바꾸면 된다 |
| M3 | `StageRootData.StageData` 타입: `StageBaseData`(SO) → `IStageData`(순수 C#) | `.StageData.StageType`, `.Validate()`는 그대로 동작한다 |
| M4 | 상세 SO의 `GetData()`가 순수 Data(`CombatStageDetail`/`ShopStageDetail`/`EventStageDetail`)를 반환한다 | **상세 SO를 직접 쓰던 코드가 있으면 영향이 있다** |
| – | `CombatStageDetail` 검증: **Field·Roster가 없으면 거부** | 빈 전투 스테이지 데이터는 더 이상 통과하지 않는다 |
| – | `IInGameQuery`에 테이블 6개 추가(Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster) | 추가만 했다 |
| – | `DataBaseFactory.Create`가 Catalog에 InGameGroup·ViewGroup 에셋을 모두 요구 | 에셋은 이 브랜치에서 등록해 두었다 |

InGame Data 규칙(순수 C#, SO는 입력 계층, SO 참조는 키로 변환)은 전달 문서 §3에 있다.

## 3. 충돌 점검 결과 (2026-09-26)

`feature/Cheon/refactor/core-system`(`ac3e3e35`)과 이 브랜치를 시험 merge해 봤다.

- Git 충돌: **없음**.
- 그 브랜치가 바꾼 `.cs` 543개에서 위 API(`StageRootData`, `IStageData`, `StageProgressData`, `StageType`, `StageMapRole`, `IInGameQuery`, `DataBaseFactory` 등)를 쓰는 곳: **없음**.
- 이 점검 이후 Core 쪽에 새로 커밋된 작업이나 로컬에만 있는 작업은 확인하지 못했다. 그런 작업에서 위 API를 쓰고 있다면 알려 주면 좋겠다.

## 4. 확인 부탁 (전달 문서 §12)

### develop에 넣기 전에 답을 받고 싶은 것

| # | 질문 |
|---|---|
| 2 | 새 Core는 Framework/Systems 위의 시스템으로 동작하고, View는 Buffer로만 규칙 값을 받는다. 이 전제가 맞는가? |
| 5 | M3/M4(스테이지 상세가 SO에서 순수 Data로 바뀜)와 Combat 검증 강화가 진행 중인 Core 작업에 영향이 있는가? |
| 6 | Buffer B1 구현(§2.2)이 Core 방향과 맞는가? 시작 노드 기본값 처리 방식(`StageKey == 0` 먼저 확인)도 괜찮은가? |

### 나중에 답해도 되는 것

| # | 질문 |
|---|---|
| 7 | 옛 `CoreCaculator`/Contracts를 정리할 계획이 있는가? 정리한다면 공용 타입(`EntityKey`, `CellCoord` 등)을 `Data/Common/`으로 옮기겠다 |
| 10 | Missing Script 상태인 옛 DataBaseSO 에셋 48개는 언제 삭제해도 되는가? (지금은 보존 중) |
| 11 | 전투 View용 Buffer(B2: 맵 크기, 엔티티 목록, 배치 깊이, 배치 미리보기)는 언제 같이 정의할까? |

## 5. 답변 방법

- 전달 문서 §12 응답 표에 적거나, 이 문서를 보며 직접 이야기해도 된다.
- 수정이 필요하면 View 쪽에서 고치겠다. Core 쪽에서 직접 고치는 편이 낫다면 그렇게 해도 된다.

## 참고

- [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md): 전체 상세, 응답 양식
- [DB-SO 재구성 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md): 규칙 P1~P7, 테이블 스펙, 결정 D1~D21
- [로컬 세션 인계 문서](2026-09-25-db-so-local-session-handoff.md): 작업 이력, 남은 일
