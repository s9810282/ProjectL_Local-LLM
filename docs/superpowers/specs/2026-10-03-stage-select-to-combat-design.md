# StageSelect 선택에서 Combat 진입까지의 연결 설계

작성일: 2026-10-03. 사용자 검토용 초안이며 아직 구현 승인을 받은 설계가 아니다.

## 목표와 현재 사실

Boot에서 StageSelect로 진입한 뒤 Core가 준비한 맵을 표시하고, 사용자가 전투 노드를 선택·확정하면 Core의 진입 수락 후 Combat을 활성화한다. 기존 3번 b안의 맵 대기 범위를 확장하여 4~6번을 연결한다.

현재 StageMapViewSystem과 SceneRoot, 표시 부품은 존재한다. StageLifeCycle의 Flow 연결은 TODO이며 StagePlan.Start와 StageJob의 EnterChapter 경로는 존재한다. StageViewBuffer는 번호가 있는 노드 선택 요청을 보관하지만 소비 경로는 없다.

Core의 EnterBattle(chapterKey, nodeId)는 준비와 선택 게시를 함께 담당한다. DatabaseBattleInitializationSource는 현재 Method_Key·유닛 연결·조회 스키마가 제공되지 않았다는 T1/G7 예외를 명시적으로 발생시킨다. 이 설계가 그 스키마를 추측하거나 기본 소스의 거절을 제거하지 않는다. 운영 DB 실제 성공은 별도의 Core 저작 계약·데이터 준비가 필요하다.

## 제안하는 실행 경로

1. StageLifeCycle에 StageMapFlow를 연결한다. 로스터의 준비 여부와 기존 시작 로스터 공개 API를 확인한 뒤 최초 진입 준비를 실행하며, 기존 StagePlan.Start → StageJob → EnterChapter로 맵을 준비한다. 화면에 들어올 때마다 ResetRunProgress를 호출하지 않는다.
2. 기존 StageSelection 입력 Controller·ReceiverSlot을 사용한다. 화면의 Camera는 씬 참조로 받으며 FieldCamera 구현은 수정하지 않는다. 노드 클릭으로 후보를 선택하고 기존 Confirm 입력으로 확정한다. UI가 소비한 포인터 입력은 노드 선택으로 중복 전달하지 않는다. 후보 강조는 Core 진입 수락을 의미하지 않는다.
3. 확정 요청은 현재 맵 키와 노드 ID를 함께 보존한다. 현재 StageViewCommandData는 노드 ID만 보관하므로 챕터 보존 계약을 구체화해야 한다. 실행 때 다른 맵이나 클릭을 읽는 방식은 사용하지 않는다. 기존 API 호출·테스트와 다른 소비자를 확인한 뒤 호환 변경 범위를 구현 계획에 명시한다.
4. 기존 Register → Plan → Runner → Job 경로에서 불변 요청 하나를 전달한다. Framework의 범용 Register 계약을 변경하는 것보다 기존 요청 Buffer·Stage 전용 실행 상태를 사용하는 방향을 우선한다. 요청을 가져오는 시점과 실행 중 중복 등록 차단의 소유자는 StageMapFlow로 고정한다.
5. 진입 Job이 CoreGroup.Combat.EnterBattle을 한 번 호출한다. 이번 호출의 반환 결과를 Flow로 전달한다. 정상 거절도 완료하며 StageSelect에 남고 이유를 표시한다. 예외는 RuntimeFaultBoundary로 전달하고 자동 재실행하지 않는다. 성공을 HasBattle 또는 이전 CombatResult로 추측하지 않는다.
6. 수락 후 실행 중인 Job 완료를 확인하고 입력을 중단한 뒤 LifeCycleManager.Change(Combat)를 한 번 요청한다. CombatLifeCycle·CombatFlow의 시작 경로는 이미 준비된 전투를 활성화하도록 이전하여 기존 PrepareBattle의 중복 호출을 막는다. StageSelect 생성물 정리는 기존 Detach를 사용한다.

## 범위와 주의사항

- 이번 대상은 전투 노드 진입이다. 상점·이벤트 노드는 Combat으로 강제 전환하지 않는다. 별도 경로가 연결되기 전에는 미지원 안내를 제공한다.
- Core 규칙·계산·DB 전투 준비 소스는 수정하지 않는다. View의 Flow·Plan·Job과 요청 전달 계약을 Core 공개 API에 맞춘다.
- 완료 결과 게시와 씬 로딩 실패는 별개다. 수락 뒤 전환이 실패해도 EnterBattle을 다시 실행하지 않는다.
- 공용 JobRunner의 실행 도중 무분별하게 Clear나 LifeCycle 전환을 호출하지 않는다. Job 완료 뒤 Flow에서 전환하도록 실제 Runner 순서를 검증한다.
- 아티팩트 화면, 전투 종료 후 StageSelect 복귀, 새 Run·저장 복구 정책은 이번 진입 연결과 별도로 다룬다.

## 검증과 완료 기준

- 실제 Register·Plan·Runner·Job·Core·Buffer 조립에서 요청 대상 보존, 중복 확정 차단, 정상 거절·예외, 수락 시 전환 1회, PrepareBattle 중복 없음, StageSelect Detach를 검증한다.
- EditMode 전체와 StageMap·ChapterGraph·DB·Framework 관련 테스트를 실행한다. 이전 기록을 현재 통과 결과로 대신하지 않는다.
- Boot → StageSelect 맵 표시·선택·확정·Combat 진입은 실제 DB로 별도 재생 검증한다. fixture 성공은 실행 경로 검증으로만 보고한다.
- 기본 DB 소스가 T1/G7에서 거절하는 동안은 운영 진입 완료로 기록하지 않는다. Core 스키마·데이터가 준비되면 마지막 실제 성공 검증을 수행한다.

## 근거

- `docs/superpowers/plans/2026-10-03-stage-map-view-plan.md` §2·§5
- `docs/superpowers/handoffs/2026-10-03-stage-entry-battle-preparation-handoff-ko.md` V1~V5
- `docs/superpowers/viewDocs/runtime-scene-binding-guide.md`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/DatabaseBattleInitializationSource.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/StageView/StageViewCommand.cs`
