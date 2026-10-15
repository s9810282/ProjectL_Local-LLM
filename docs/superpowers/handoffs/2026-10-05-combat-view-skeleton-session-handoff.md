# 전투 골격·View 공동 인계 — Phase 7 / View F

- 작성일: 2026-10-05. 코드 기준: `feature/Yonggeun/Stage`, `bfe63a27`.
- 사용자 요청으로 두 문서 Phase를 같은 세션에서 정리했다. 이번 변경은 문서·UML뿐이다.
- 문서 작업 완료는 전투 전체·운영 DB·포기 기능 완료를 뜻하지 않는다. 커밋·push·외부 전달은 사용자가 한다.

후속 사용자 지시로 View 작업은 다른 세션에서 진행한다. [View 전용 시작 안내](2026-10-05-combat-view-next-session-handoff.md)에 읽을 순서·직전 구조 평가·수정 경계·복사할 시작 문구를 정리했다. 이 공동 인계의 완료 범위·검증 출처는 유지한다.

## 1. 다음 작업자가 읽을 순서와 원본

| 읽을 것 | 용도 |
|---|---|
| [골격 계획 §0.1·§0.2](../plans/2026-09-30-combat-flow-skeleton-plan.md#01-병합-후-실행-순서와-계획-간-소유권) | 남은 순서, 담당과 차단 조건의 원본 |
| [현재 통합 UML](../specs/2026-10-05-combat-current-uml.md) / [HTML](../specs/2026-10-05-combat-current-uml.html) | 구현된 클래스·진입·이동·결과·정상 이탈의 관계 |
| [골격 설계](../specs/2026-09-30-combat-flow-skeleton-design.md) | §4.1 Flow 분기, §6.3 진입, §6.4 정상 이탈 정책 |
| [View 설계](../specs/2026-09-30-combat-view-design.md) | §4 책임, §7 입력, §9 연출, §10.1 단일 F1, §23 구현 정렬 |
| [Core 요청](2026-10-05-combat-core-requests-phase6b.md) | 공개 포기 API·T1/G7·두 재화 분리의 요청 원본 |
| [공동 Framework 공유](2026-10-05-combat-view-skeleton-framework-share.md) | 실제 공용 변경과 다른 호출자에게 필요한 대응 |

이전 UML은 당시 코드 스냅샷이다. “세 Job 미연결”, “View 입력 미연결”, “Immediate만 사용”, “F1 미구현”이라는 과거 기록을 현재 상태로 되돌리지 않는다. Core 담당 문서의 과거 F1/F2 미구현 표도 최신 계획과 대조한다.

## 2. 인계 범위와 증거

완료 범위의 원본은 골격 계획 §6.14~15와 View 계획 §7.23~24다.

| 범위 | 인계하는 것 | 증거의 한계 |
|---|---|---|
| 골격 0~5·6A / View 0~E2·A2 | 네 Job 연결, 대상 캡처·수락·활성화, 표시·배치/공격·경로 이동·Return·응답 사용 | 기존 완료 결정 유지. 운영 DB source 성공과 구분 |
| F1 및 정상 F2 기반 `5078ee0e` | Bootstrap·입력 오류 정지, 정리·늦은 콜백 방어, Completion UI·EndBattle·전환 guard | 사용자 “커밋한 것들은 검증 됐음” 확인을 인수. 새 개별 로그/수치를 받았다고 하지 않음 |
| 일반 승리 자동 복귀 `bfe63a27` | 실제 LifeCycle 호출자에서 기존 정상 이탈 순서로 StageSelect 복귀 | 이전 작업의 MSBuild 3대상 오류 0, 관리 코드 92/92. Unity 새 실행 결과는 미수신. 커밋됐다는 사실만으로 추가 검증 완료를 추정하지 않음 |
| 이번 Phase 7 / View F | 코드 대조, 현재 UML, 설계 정렬, 공유·요청·인계 문서 | 문서 링크·구조·HTML 점검. Unity 재실행 없음 |

`RecordInputOnly`는 실제 준비 후 배치/공격 입력을 기록하는 개발 진단이다. 처리 Job 실행과 Core 견적 context가 필요한 Move/Return을 대신하지 않는다. 개발 Live는 `DevBattleInitializationSource`를 쓴다. 실제 View DB/씬을 읽는 테스트도 준비 정의가 fixture이면 운영 전투 증거가 아니다.

## 3. 유지할 연결 경계

- **진입:** 호출자가 chapter/node를 요청에 담고 기존 Register → Plan → Runner → EnterBattle을 사용한다. 수락 결과·Job 완료 후 Manager가 Combat을 활성화한다. View는 `ICombatEntryRead.IsActive`만 읽고 중복 준비하지 않는다.
- **정상 처리:** Core만 규칙·상태·결과를 게시한다. View는 입력 Buffer에 의도를 쓰고, 모든 사건 표시·후처리가 성공한 뒤 입력을 정리하고 결과 번호를 확인한다. 확인 뒤에도 Flow가 최신 결과를 읽을 수 있다(F3).
- **표시 수명:** 새 제출 정지와 오류 StopInput/Detach를 구분한다. 정상 종료 때 마지막 Playback을 취소하지 않는다. 이동 연출은 기존 View Tick에서 진행하며 별도 Runner를 만들지 않는다.
- **정상 이탈:** 소유자·목적지 정책은 골격 §6.4를 사용한다. 일반 결과 확인과 Completion 버튼 확인은 별개다. EndBattle 뒤 결과·Completion 보존과 캐릭터/전투 정리를 확인한 후 전환한다. Combat의 Manager.Change/Exit 직접 우회는 허용하지 않는다.
- **F1:** 오류 뒤 같은 Runtime을 재Tick하지 않는다. 공유 오류 객체를 복제하지 않고 Runtime 종료는 정상 이탈 대기와 분리한다.
- **데이터:** View는 InGame DB에서 규칙을 읽거나 Core Buffer·재화를 직접 쓰지 않는다. HUD의 현재 공통 Currency 읽기는 분리 완료 전의 제한이다.

## 4. 다음 시작점

| 작업 | 현재 조건 | 조건 충족 뒤 우리 작업 |
|---|---|---|
| 포기 | Core 소유 payload 조립 공개 API 미인수 | 실제 서명/중복/실패 의미 대조 → 기존 F2에 포기 의도·Completion 대기 연결. 더미 ArtifactLoadout으로 우회하지 않음 |
| 패배·챕터 완료(최종 승리 포함) | 목적지 사용자 보류. Lobby/Artifact 씬은 예약 상태 | 목적지·화면 확정 뒤 동일 이탈 경계의 호출자 연결. 임의 복귀·새 Run 시작 없음 |
| 운영 전투 준비 | A2 데이터/query는 제공, 기본 source는 아직 T1/G7 예외 | Core 조립·반환 인수 후 fixture 없는 로스터·챕터·대상 경로 연결 |
| 재화 | Stage/Run 분리 규칙은 확정, 현재 공통 잔액 | Core의 별도 읽기·초기화·보존 API 인수 후 전투 HUD를 Stage 골드로 연결 |
| StageSelect 다음 전투 선택 | 기존 CombatEntry API는 있음, 실제 화면 호출자는 별도 | 담당/범위 확정 뒤 노드 선택을 API에 연결. 일반 승리의 화면 복귀만으로 다음 노드 선택이 완성된 것은 아님 |
| 선택적 표현 확장 | 이동 외 VFX·Drag 외형·Reason별 HUD·규모 측정은 후속 | View 계획 §8 적용. 지뢰/Object/목적지 보정은 실제 Core/저작 사실 인수 후 표시 |

세 Core 요청은 서로 독립적으로 인수할 수 있다. 골격 6B 전체 종료가 필요한 작업과 문서 Phase 완료를 섞지 않는다. 과거 완료 검증을 새 미완료 목록에 다시 넣지 않는다.

## 5. 받은 답변과 남은 요청의 차이

- 입력 유지, 한 처리의 결과 하나, 결과 순번 원본, 자동 진행 거절/예외 의미는 기존 답변을 반영했다. 준비 인덱스·새 리비전은 현재 View에 필요하지 않다.
- F1 독립 진행, 정상 F2 우선 연결 지시는 이행했다. 포기는 패배라는 규칙이 확정돼도 View가 정산 입력을 조립할 권한이 생기는 것은 아니다.
- 초기값 SO/DB, Stage/Run 재화 분리, 같은 세션 재연결 보존 규칙은 미정이 아니다. 실제 source/API·설치/폐기 수명과 공통 잔액 불일치에 대한 Core 구현 인수가 남았다.
- Reason 코드 목록의 안정성·시작 서브 수 변경은 기존 Core 메모의 후속 질문이며 현재 기본 전투/6B의 차단 항목으로 확대하지 않는다.
- 공유 문서 준비와 상대 담당자 수신·합의는 별개다. 이번 세션에서 외부 메시지는 보내지 않았다.
