# 전투 스테이지 진입·전투 준비 — View/Flow 전달용 요약

작성일: 2026-10-03

대상: 전투 진입 화면, Flow, Plan/Job, LifeCycle 연결 담당자.

**Core/Buffer의 전투 진입·준비 계약 구현과 검증은 완료됐고, 실제 View/Flow 연결은 후속 작업으로 남아 있다.** 이 문서는 전달용 한글 요약이다. **정확한 API 계약, 내부 처리, 변경 파일 목록, 테스트·리뷰 근거는 [영어 원본](2026-10-03-stage-entry-battle-preparation-handoff.md)을 참고한다.** 구현 시 세부 사항은 영어 원본을 기준으로 확인한다.

디스코드에는 이 요약본과 `2026-10-03-stage-entry-battle-preparation-handoff.md`를 함께 첨부한다. 첨부 파일 미리보기에서 상대 링크가 열리지 않으면 같은 이름의 영어 원본을 직접 열면 된다.

## 1. 완료된 범위와 남은 범위

| 구분 | 원문에 기록된 상태 | 의미 |
| --- | --- | --- |
| C: Core/Buffer | 완료 | P1~P3 구현·테스트·독립 리뷰 완료. 테스트용 정의로 진입 계약 검증. |
| V: View/Flow 연결 | 미완료 | 진입 대상 전달, Job 결과 처리, LifeCycle 활성화를 실제 실행 경로에 연결해야 함. |
| D: 실제 DB 준비 | 미완료 | `Method_Key`, 쿼리·유닛 연결, `Economy` 등 기본 소스가 읽는 데이터 준비 필요. |
| A: 실제 게임에서의 활성화 | 미완료 | C/V/D와 실제 씬·초기화·수명 검증 필요. |

커밋·푸시나 테스트용 소스로 진입한 결과만으로 실제 게임 연결 완료를 의미하지 않는다. 회복/상점 진입, 챕터 이동, Run 초기화, 보상, 지형 정책, 에셋 제작은 이번 범위에 포함되지 않는다.

## 2. View/Job에서 사용할 호출

기존 `IInGameCoreSystemGroupService.Combat` 포트에 아래 계약이 있다.

```csharp
ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
void PrepareBattle();
```

첫 전투 또는 다음 전투 스테이지 진입에는 **`CoreGroup.Combat.EnterBattle(chapterKey, nodeId)`**를 사용한다. 호출자는 챕터와 노드 의도를 전달하고, 실제 StageKey는 Core가 현재 진행 상태에서 결정한다.

- 해당 챕터에는 이미 진입해 있어야 한다. 챕터/그래프 등록, 로스터·메인·소울 상태, Framework 초기화·바인딩도 선행 조건이다.
- 이 호출이 로스터, 지갑, 아티팩트 재고를 초기화하거나 챕터를 변경하지는 않는다.
- 종료된 전투도 정리 전까지 보존 중이면 새 진입을 막는다. 기존 정리 절차가 먼저 완료돼야 한다.

| 호출 결과 | 의미와 후속 처리 |
| --- | --- |
| `Accepted` | 반환 전에 대상 Current 선택과 준비된 Character/Combat 상태가 게시됐다. 이후 실행 계층이 전투 활성화를 진행한다. Job 완료·씬 로딩·입력 활성화까지 완료됐다는 뜻은 아니다. |
| 정상적인 진입 거절 | 거절된 `ChapterProgressionResult`를 처리한다. 처음 진행 검사에서 거절되면 소스 조회·진입 쓰기를 하지 않는다. Job은 정상 반환으로 한 번 완료하되 전투를 활성화하지 않는다. |
| 예외 | 잘못된 인자, 의존성 누락, 보존 전투, 잘못된 데이터, 소스·계산·게시 불변식 오류 등에 해당한다. 정상 거절과 구분해서 오류 경로로 처리한다. |

Core는 준비 후 게시 직전에 진행 상태를 다시 확인한다. 재검사에서 거절되면 그 결과를 반환하고, 출발 선택이나 수락된 대상이 바뀌었으면 `StageMismatch`를 반환한다.

**수락 후 View/Job에서 같은 Combat 대상을 다시 확정하거나 선택하면 안 된다.** Core가 이미 선택을 게시했다. 매개변수 없는 `PrepareBattle()`은 이미 확정된 선택을 사용하는 구형 호출의 호환용이다. 첫/다음 스테이지 진입 경로로 쓰지 않으며, 호출자 이전을 함께 마친 뒤 제거 여부를 결정한다.

## 3. View/Flow 담당 후속 작업: V1~V5

현재 `Register(JobRootType, JobType)`와 `CombatPlan.Register(service, jobType)`에는 챕터/노드 인자를 전달하는 계약이 없다. 따라서 **기존 Prepare 호출 한 줄을 교체하는 것만으로 연결이 끝나지 않는다.** 다음 항목은 실행/View/Flow 담당자가 구현하고 검증할 범위다.

- [ ] **V1 — 진입 대상 전달:** 기존 Register → Plan → Job 경로에서 챕터/노드 의도를 실행 시점까지 보관·전달한다. 요청 보관을 위해 Current의 확정 선택을 미리 바꾸거나, 실행 시점의 다른 클릭을 읽지 않는다. Core 큐나 별도 실행기로 우회하지 않는다. 필요한 공용 Framework 계약 확장은 해당 담당자 검토에 포함한다.
- [ ] **V2 — 한 번 실행하고 결과 구분:** 진입 Job에서 `EnterBattle`을 한 번 호출하고 수락·거절·예외를 구분한다. 거절을 포함한 정상 반환은 Job을 한 번 완료한다. 반환 결과를 Flow에 전달하는 방법을 정하고, 수락한 경우에만 전투를 활성화한다. `HasBattle`이나 이전 전투 명령의 결과·순번을 이번 진입 성공의 근거로 쓰지 않는다.
- [ ] **V3 — LifeCycle 중복 준비 제거:** 수락 후 준비된 전투를 활성화할 때 `OnEnter → BeginPreparation → 기존 PrepareBattle()`이 다시 실행되지 않도록 시작 경로를 함께 이전한다. 씬 리소스를 먼저 로드하더라도 수락 전까지 입력·활성 처리를 막는다. 중복 Combat 선택 확정 호출을 제거하고 비전투 호출 경로는 구분한다.
- [ ] **V4 — 취소·교체·실패 처리:** 활성화 전 요청 취소·교체와 실패 정책을 정한다. 오래된 요청을 자동 재시도하지 않고, 수락 뒤 씬 활성화가 실패해도 진입을 조용히 재실행하거나 무제한 초기화로 복구하지 않는다. 이전 작업 중단·배출과 입력 정리는 기존 실행/View 권한으로 처리한다. Framework F1과 View/Flow F2 검증도 필요하다.
- [ ] **V5 — 실제 실행 경로 검증:** `Register → Plan → Runner → Job → Core → Buffer → Flow`에서 서로 다른 첫/다음 대상, 정상 거절, 예외, 중복 등록 방지, LifeCycle 시작 정확히 한 번을 검증한다. 이후 D/A에서 실제 DB·씬 수락까지 확인한다. 가짜 Job 테스트나 서비스 직접 호출만으로 V를 완료 처리하지 않는다.

주요 확인 파일은 `CombatPrepareJob.cs`, `CombatPlan.cs`, `CombatFlow.cs`, `CombatLifeCycle.cs`, `JobRegisterSystem.cs`다. 전체 경로는 영어 원본의 **Gate V** 절에 있다. 기존 `CombatCommandJob`, `CombatPreviewJob`, `CombatAdvanceJob` 연결도 실행 담당자의 별도 인계 범위이며, 이번 P1~P3에 포함되지 않는다.

## 4. 상태 소유권과 보존 규칙

Core는 진입 허용 여부·대상 데이터 확인·계산을 담당하고, 기존 Buffer 그룹은 Current/Character/Combat을 모두 준비한 뒤 설치한다. 각 Buffer가 상태 소유권을 유지하며, Buffer가 DB 조회·이동 정책·Job 예약을 맡지는 않는다.

준비 실패 시 기존 상태·결과·프리뷰를 보존한다. 다음 준비가 성공하면 완료 응답과 프리뷰 응답을 지우되 결과와 번호는 유지한다. View의 요청·명령·표현 상태를 Core가 임의로 정리하지 않으므로 입력 수명 처리는 실행/View 담당 범위다.

이 보장은 기존 직렬 실행 환경에서의 동기적인 준비 후 설치에 대한 것이다. 동시/재진입 쓰기나 프로세스 종료까지 복구하는 트랜잭션은 아니며, 소스가 별도로 일으킨 외부 변경까지 되돌리지는 않는다.

## 5. 검증 기록과 상세 문서

아래 수치는 영어 원본에 기록된 기존 검증 결과다. 이 요약 작성 과정에서 Unity 테스트를 다시 실행한 것은 아니다.

- P3 집중 검증: **222/222 통과**.
- P3 전체 회귀: **1371/1376 통과**, 기존과 동일한 실패 5개 유지. 전체 통과 상태는 아니다.
- 실패 5개는 DB 카탈로그 구성 시의 `Combat stage requires Economy` 오류다. 원문에는 기존 기준선과 실패 이름·메시지·스택이 동일하다고 기록돼 있다.
- P1~P3 패키지의 독립 리뷰에서 조치할 결함은 보고되지 않았다. P3는 테스트·문서 변경이며, P1/P2 운영 코드 수정은 없었다.
- 실제 Factory/Framework/Buffer를 사용한 검증이지만 그래프·지원 프로필에는 테스트용 DB/소스가 사용됐다. 기본 소스의 `Method_Key` 거절 경계도 검증했으며, 실제 저작 DB·씬 진입 성공을 입증한 것은 아니다.

**자세한 내용은 [Stage Entry and Battle Preparation: Core Handoff — 영어 원본](2026-10-03-stage-entry-battle-preparation-handoff.md)을 참고한다.** 공개·내부 계약은 `Consumer contract`와 `Internal contracts and ownership`, 테스트 근거·재현 명령은 `Verification evidence`, 실제 연결 작업은 `Gate V` 절에 정리돼 있다. 설계·구현 계획·P1/P2 보고서 링크도 원본에 포함돼 있다.
