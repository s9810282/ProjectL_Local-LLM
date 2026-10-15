# View 입력 Buffer 제작 요청 — Core 연결 계약

- 작성일: 2026-09-29
- 전달 대상: View 작업자 및 Framework 작업자
- 목적: View가 기록하고 Core 처리 계층이 읽는 입력 Buffer의 역할, 필요한 입력값, 처리 결과 연결 방식을 공유합니다.
- 상태: 대화에서 확정한 요구와 제안을 정리한 전달 문서입니다. 아래 타입명·API 예시는 구현 확정안이 아니며, 이 문서 작성으로 코드가 변경되지는 않았습니다.

## 1. 요청 요약

View 입력은 다음 두 Buffer로 나눕니다. 기존 진행 상태 Buffer와 혼동하지 않도록 이름에 `Input`을 붙이는 것을 권합니다.

| 제안 이름 | 역할 | 대표 입력 |
|---|---|---|
| `StageInputBuffer` | 챕터·스테이지 선택과 비전투 스테이지 조작 | 노드 선택, 상점 구매, 회복 선택, 결과 확인 |
| `CombatInputBuffer` | 전투 조작과 전투 미리보기 | 배치, 이동, 공격, 회수, 턴 종료, 범위 조회 |

**View가 입력을 기록하고, Core 측에는 읽기 권한만 제공합니다. Core는 View 입력 Buffer의 값을 수정하거나 입력을 제거하지 않습니다. View와 Core 사이의 명령·결과 전달은 Buffer를 통해서만 수행합니다.**

Core가 필요한 것은 플레이어의 선택입니다. 현재 체력·행동력 등 전투 상태나 피해 계산 결과를 View가 조립하여 전달하지 않습니다.

## 2. 대화에서 합의한 방향

1. 입력을 스테이지용과 전투용 두 Buffer로 구분합니다. 기능마다 새 입력 Buffer를 만들지는 않습니다.
2. 스테이지 입력도 무엇을 눌렀는지 구분할 수 있어야 합니다. 선택지가 하나라면 명령 종류만, 여러 개라면 선택한 항목의 ID를 함께 기록합니다.
3. 전투 입력은 개체 ID·논리 칸 좌표·방향 등 사용자 선택을 전달합니다.
4. 전투 미리보기는 CombatInputBuffer에 포함할 수 있지만, 실행 확정 입력과 구분해야 합니다.
5. Core는 View 입력 Buffer를 읽기만 합니다. 입력 정리는 View/Framework가 담당합니다.
6. 입력 번호와 결과를 대응시키는 방식은 사용자가 긍정적으로 검토한 제안입니다. 구체적인 보관 방식·API·처리 중 상태·결과 저장 위치는 공동 합의가 필요합니다.
7. 기존 서비스 등록, ReadSink/WriteSink, PostBindService, Plan/Job 경로를 재사용합니다. 공용 Framework 변경을 전제하지 않습니다.

## 3. 전체 처리 흐름과 권한

```text
사용자 조작
  → View가 의미 있는 명령으로 변환
  → StageInputBuffer 또는 CombatInputBuffer에 기록
  → Systems의 Core 처리 계층이 입력과 현재 게임 Buffer를 조회
  → Plan/Job 경로에서 Core 규칙 실행
  → Core 측 상태 Buffer 갱신 및 입력 처리 결과 기록
  → View가 상태·결과 Buffer를 조회하여 표시
  → View/Framework가 처리 확인된 입력을 제거
```

| 대상 | View 측 | Core 처리 계층 |
|---|---|---|
| View 입력 Buffer | 입력 기록, 합의한 정책에 따른 정리 | 읽기만 가능 |
| 게임 상태 Buffer | 읽어서 표시 | 공개 API로 조회·갱신 |
| 입력 처리 결과 저장 영역 | 읽어서 요청 결과 확인 | 처리 결과 기록 |

입력 Buffer를 제거하는 `Dequeue`, `Clear`, `MarkProcessed` 등의 API를 Core의 읽기 인터페이스에 넣지 않습니다.

Core.Application/Core.Domain은 Systems Buffer를 직접 참조하지 않습니다. Systems 처리 계층이 기존 PostBindService로 Buffer 서비스를 받고, 필요한 값을 Core.Contracts 입력 또는 필요한 경우 포트로 연결합니다. View가 Core Snapshot이나 DB 데이터를 조립하여 전달하지 않습니다.

## 4. StageInputBuffer에 필요한 입력

아래 이름은 의미를 설명하기 위한 예시입니다. 최종 enum·타입명은 기존 명명 규칙에 맞춰 정하면 됩니다.

| 명령 예시 | 명령별 입력값 | Core가 조회·판단할 내용 |
|---|---|---|
| `SelectChapter` | 챕터 키 | 챕터 진입 가능 여부, 기존 진행 기록 |
| `SelectStageNode` | 노드 ID | 현재 챕터, 연결 관계, 클리어 여부, 스테이지 키 |
| `PurchaseShopOffer` | 상품 항목 ID | 상품 내용·가격·판매 상태, 재화, 구매 가능 여부 |
| `SelectRecoveryOption` | 회복 선택지 ID | 선택지 효과, 회복 대상과 회복량 |
| `FinishShop` | 추가 값 없음 | 상점 이용 종료 가능 여부와 후속 진행 |
| `ConfirmStageResult` | 추가 값 없음 | 현재 결과와 후속 진행 가능 여부 |

- 현재 챕터는 CurrentProgressBuffer에서 조회합니다. 노드 선택마다 View가 그래프나 스테이지 정보를 함께 보낼 필요는 없습니다.
- 회복 행동이 하나뿐인 UI라면 `RequestRecovery` 같은 명령 하나로 충분합니다. 여러 선택지가 있을 때만 선택지 ID를 전달합니다.
- 상품은 캐릭터 종류의 키 대신 현재 상점의 상품 항목 ID로 선택합니다. 같은 캐릭터라도 상품 조건이 다를 수 있기 때문입니다.
- `ConfirmStageResult`는 결과 확인 의사입니다. View가 승리·패배·클리어 여부를 결정하여 전달하는 명령이 아닙니다.
- 실제 화면에 없는 행동을 이 목록만을 근거로 새로 만들 필요는 없습니다.

## 5. CombatInputBuffer에 필요한 실행 입력

| 명령 예시 | 명령별 입력값 | 설명 |
|---|---|---|
| `PlaceCharacter` | 캐릭터 개체 ID, 목표 칸, 배치 방향 | 선택한 보유 개체를 배치 |
| `MoveCharacter` | 캐릭터 개체 ID, 목표 칸 | 가능 여부·비용·경로는 Core가 계산 |
| `Attack` | 공격자 개체 ID, 조준 정보 | 아래 공격 조준 형식 참조 |
| `ReturnCharacter` | 캐릭터 개체 ID | 빈 패 슬롯 자동 선택이면 슬롯 번호 불필요 |
| `StartCombat` | 추가 값 없음 | 배치 완료 후 시작 버튼이 있는 흐름에 사용 |
| `EndUnitTurn` | 추가 값 없음 | 현재 행동 유닛은 Core 측 상태에서 조회 |

### 공격 조준 정보

기존 Core의 AttackAim은 유닛 대상과 칸 대상을 구분하고, 필요한 경우 방향을 받습니다.

| 조준 종류 | 입력 |
|---|---|
| 유닛 대상 | 대상 개체 ID |
| 칸 대상 | 목표 칸 |
| 방향 선택이 필요한 공격 | 위 대상 정보에 선택한 공격 방향 추가 |

대상 종류에 맞는 값만 전달합니다. 대상 ID와 목표 칸을 모두 필수로 요구하지 않습니다. 광역 공격의 전체 피격 대상, 실제 피해량, 사망 여부는 Core가 계산합니다.

### 식별자와 좌표

- 캐릭터 종류의 DB 키와 실제 보유 개체 ID를 구분합니다. 같은 서브 캐릭터를 여러 장 가질 수 있습니다.
- 목표 칸은 기존 CellCoord에 대응하는 논리 좌표입니다. 화면 픽셀 좌표·월드 좌표·GameObject·Transform을 명령 데이터로 전달하지 않습니다.
- 배치 회전과 공격 방향은 서로 다른 의미이므로 구분합니다. 기존 Rotation4·AttackDirection 등 Contracts 타입의 재사용 가능성을 확인합니다.

## 6. 전투 미리보기 입력

View가 규칙을 직접 계산하지 않으므로 범위와 예상 결과도 Core 측에서 준비해야 합니다.

| 미리보기 종류 | 입력 |
|---|---|
| 이동 가능 범위 | 캐릭터 개체 ID |
| 이동 예상 결과 | 캐릭터 개체 ID, 목표 칸 |
| 공격 가능 범위 | 공격자 개체 ID |
| 공격 예상 결과 | 공격자 개체 ID, 조준 정보 |
| 배치 가능 여부 | 캐릭터 개체 ID, 목표 칸, 배치 방향 |
| 미리보기 해제 | 해제 명령 또는 미리보기 대상 없음 |

미리보기는 실행 명령과 구분합니다. 호버·드래그 중에는 체력·행동력·위치 등 실제 게임 상태를 변경하지 않습니다. Core가 계산한 미리보기 결과는 View가 읽을 수 있는 Core 측 Buffer에 기록합니다. 그 저장 위치는 아직 확정하지 않았습니다.

실행 명령은 처리 전 유실되면 안 됩니다. 반면 호버 미리보기는 최신 대상만 유지하는 방식을 제안합니다. 두 종류가 같은 CombatInputBuffer에 있어도 보관 정책은 구분할 수 있습니다. 새 미리보기로 교체되거나 해제된 뒤 이전 결과가 다시 표시되지 않도록 요청과 결과의 대응 기준도 맞춥니다.

## 7. 기존 기능을 이전 범위에 포함할 때 추가할 입력

다음은 기존 Core에 관련 진입점이 있는 기능입니다. 이번 UI·이전 범위에 포함할 때만 입력을 추가합니다.

| 기능 | 입력 |
|---|---|
| 수동 카드 뽑기 | 뽑기 요청 |
| 지정 패 슬롯으로 회수 | 캐릭터 개체 ID, 목표 슬롯 번호 |
| 본부 배치 | 목표 칸, 배치 방향 |
| 아티팩트 배치 | 아티팩트 식별자, 목표 칸, 회전 |
| 배치된 아티팩트 이동 | 배치 개체 ID, 목표 칸, 회전 |
| 아티팩트 회수 | 배치 개체 ID |

아티팩트 편집이 별도 화면인지 전투 조작인지에 따라 두 입력 Buffer 중 어느 쪽에 둘지 정하면 됩니다. 이 문서에서는 배치를 확정하지 않습니다.

## 8. 입력 번호와 처리 결과 연결 — 협의할 제안

### 입력 데이터

```text
입력 번호
명령 종류
명령별 값
```

### Core 측 처리 결과

```text
대응하는 입력 번호
성공 또는 거절
거절 사유 — 거절된 경우
```

위 항목은 개념적인 최소 구성입니다. 입력·결과 타입이나 새 결과 Buffer를 반드시 이 형태로 만들라는 의미는 아닙니다.

입력 번호는 반복 조회와 새 사용자 입력을 구분하고 결과를 대응시키기 위한 값입니다. Run ID·Run 버전·락·일반 트랜잭션을 도입하기 위한 계약이 아닙니다. 입력의 timestamp를 고유 번호로 간주하지 않습니다.

### 처리 순서 예시

```text
1. View: 공격 입력 42를 CombatInputBuffer에 기록
2. Systems 처리 계층: 입력 42를 한 번만 작업으로 등록
3. Core: 현재 Buffer를 기준으로 공격 가능 여부와 결과 계산
4. Systems 처리 계층: 승인된 상태 변경을 반영하고 입력 42의 결과 기록
5. View: 결과와 게임 상태를 읽어 표시
6. View/Framework: 확인한 입력 42를 제거
```

거절도 처리 완료 결과입니다. 거절된 입력을 계속 남겨 자동으로 재실행하지 않습니다. 입력 제거와 게임 상태 초기화는 별개입니다.

### 반드시 맞출 세부 책임

| 항목 | 필요한 합의 |
|---|---|
| 입력 번호 발급 | View/Framework 중 발급 주체, 번호의 유효 범위와 재사용 시점 |
| 두 Buffer의 번호 구분 | 전역 번호를 쓰거나 `(입력 Buffer 종류, 입력 번호)`로 결과를 식별 |
| 처리 전 입력 보관 | 다음 클릭이 아직 처리되지 않은 실행 명령을 덮어쓰지 않도록 보관 |
| 중복 등록 방지 | 처리 중인 입력을 다음 Tick에 다시 Job으로 등록하지 않는 책임과 상태 위치 |
| 결과 보관 | View가 읽기 전에 다른 결과가 덮어쓰지 않도록 유지·정리 규칙 설정 |
| 입력 정리 | View/Framework가 해당 결과를 확인하고 해당 입력만 제거 |
| 화면 전환 | 미처리 입력·결과를 어떤 시점에 유지하거나 정리할지 결정 |

Core가 View 입력을 수정하지 않는 조건은 위 합의에도 유지됩니다. 처리 중 식별이 필요하면 Systems 실행 관리 또는 Core 측 처리 상태에 둡니다. 완료 결과만 기록하고 처리 중 중복 등록을 방치하는 방식은 피합니다.

입력 목록 방식인지 한 건씩 수락하는 방식인지는 아직 확정하지 않았습니다. 한 건 방식이라면 처리 전 다음 입력을 덮어쓰지 않는 수락 정책이 필요합니다. 목록 방식이라면 처리 순서와 결과 보관 정책을 맞춰야 합니다. 기존 Runner가 우선순위 기반이라는 점도 고려하되, Runner 개조부터 전제하지 않습니다.

## 9. 현재 Framework 확인 결과

2026-09-29 로컬 소스 확인 기준이며, 다른 브랜치나 작업자의 미반영 구현까지 확인한 결과는 아닙니다.

| 기존 구성 | 현재 역할 | 이번 계약과의 차이 |
|---|---|---|
| InputSnapshot | 액션 이름·입력 단계·값·시간 전달 | 입력 번호와 처리 확인 없음 |
| InputSystem / InputRouteTable | 등록된 핸들러로 입력 전달 | 처리 확인까지 보관하는 입력 Buffer 아님 |
| JobRegisterSystem | 루트·Job 종류로 Plan의 Job 등록 | 사용자 입력 번호·명령값·결과 대응 없음 |
| JobRunnerSystem | Job 실행, IsCompleted 확인 | 입력별 성공·거절 결과 보관 없음 |
| JobQueue의 Sequence | 동일 우선순위 Job의 등록 순서 유지 | 외부 요청 식별용 공개 번호 아님 |
| 기존 BufferSystemGroup | 게임 상태 Buffer의 Read/Write 서비스 제공 | StageInputBuffer·CombatInputBuffer·입력 결과 계약 없음 |

따라서 입력별 처리 확인 연결은 추가가 필요합니다. 기존 서비스 등록·ReadSink/WriteSink·PostBindService·Plan/Job 실행 기반은 재사용할 수 있습니다. 공용 Framework나 Runner 변경을 선행 조건으로 삼지 않습니다.

## 10. View가 전달하지 않을 정보

- 현재 체력·능력치·행동력·현재 턴 등 이미 Core 측 Buffer에 있는 상태
- 이동·공격 가능 여부, 소모 비용, 피해량, 회복량
- 사망 여부, 승패, 스테이지 클리어 결과
- 광역 공격의 전체 피격 대상과 적 AI의 행동
- 무작위 드로우 결과와 최초 가져올 카드 목록
- DB 행·Core Snapshot·Core 런타임 객체
- Job 실행 순서와 관련 규칙의 후속 실행 지시

View 내부에서 기존 입력 핸들러를 이용해 클릭을 ID·논리 칸으로 해석할 수는 있습니다. 다만 그 결과를 Core에 직접 호출하는 대신 입력 Buffer에 기록합니다. 애니메이션 완료 통지는 현재 필수 계약으로 추가하지 않습니다. 실제로 게임 진행이 해당 완료를 기다려야 할 때 필요한 경로만 별도로 합의합니다.

## 11. 관련 게임 규칙 — 입력 구현 시 참고

- 게임 진행은 챕터 진입 → 미클리어 스테이지 순차 진행 → 보스 클리어이며, 스테이지 클리어와 맵 완료 기록을 유지합니다.
- 클리어 스테이지 재진입은 Core가 StageProgressBuffer 기록을 보고 차단합니다.
- 메인 캐릭터는 1장이고, 서브 캐릭터는 여러 장 보유할 수 있습니다. 보유 서브 전체가 전투용 덱입니다.
- 전투 진입 시 메인 1장과 랜덤 서브 최대 3장을 가져옵니다. 서브가 부족하면 있는 만큼 가져오며 같은 보유 개체를 중복 선택하지 않습니다.
- 메인의 지속 수치는 사망 전까지 스테이지·챕터 사이에서 유지합니다. 전투 한정 버프는 별도로 종료 처리합니다.
- 서브의 전투 상태는 전투 종료 시 초기화합니다. 사망한 서브도 다음 전투에서 사용 가능하며 보유 기록은 유지합니다.
- 아티팩트에 의한 서브 공격력·최대 체력 강화가 있으며, 전투 상태 초기화로 적용 근거가 사라지면 안 됩니다.
- 현재 Action 코스트는 해당 유닛 턴 종료 시 최대치로 회복합니다.
- 상점 획득·회복·드로우·턴 종료의 실제 결과는 Core가 계산합니다. View는 해당 행동을 선택했다는 입력만 전달합니다.

## 12. 작업자 간 확인 요청

다음 사항을 정하면 두 입력 Buffer와 Core 연결 작업을 병행할 수 있습니다.

1. StageInputBuffer / CombatInputBuffer의 공개 읽기·쓰기 인터페이스와 기존 서비스 등록 위치
2. 위 입력 목록 중 이번 UI에서 실제 사용하는 항목 및 기존 ID·좌표·방향 타입
3. 입력 번호 발급 범위와 두 Buffer 사이의 식별 방식
4. 미처리 입력 보관, 처리 중 중복 등록 방지, 결과 보관·입력 제거의 담당 계층
5. Core 측 결과·미리보기 저장 위치 — 기존 Buffer 확장 또는 별도 Buffer 필요 여부

이 문서는 Run 재시작, Run 버전, 락, 일반 트랜잭션, 새로운 공용 실행 프레임워크를 요청하지 않습니다.

## 참고 소스와 문서

- [코드 구조 README](../../../Assets/ProjectL/Script/README.md)
- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)
- [Framework 구조 설명](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)
- [Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md)
- [InputSnapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/InputSystem/Data/InputSnapshot.cs)
- [InputSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs)
- [JobRegisterSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs)
- [JobRunnerSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/JobRunnerSystem.cs)
- [StageCombatFacade](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/StageRun/StageCombatFacade.cs)
- [StageDeploymentService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/StageRun/StageDeploymentService.cs)
- [AttackAim](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/AttackAim.cs)

문서의 참고 링크는 저장소 기준 상대 경로입니다. 파일만 따로 전달하면 링크는 열리지 않을 수 있으나, 본문만으로 요청 내용과 협의 항목을 확인할 수 있습니다.
