# 맵 노드 이동 공용·전투 진입 공유 — 2026-10-16

- 작성자: 호진(이호진)
- 상태: **작성만 함. 외부 전달·전투 작업자 합의/인수는 별도다.**
- 대상: Framework·전투 View/진입 수명 담당자.
- 사용자 승인 범위: NM1 로비 이동 공용화, NM2 Core 수락 뒤 맵 이동·착지 후 진입, NM3 운영·비용 검증. 패배 후 로비 복귀를 허용한 다른 작업자의 발언만으로 이 진입 계약 변경에 동의했다고 해석하지 않는다.
- 원본: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md), [스테이지 맵 계획·검증 상태](../plans/2026-10-10-stage-map-plan-Hojin.md). 둘은 2026-10-27에 옛 노드 이동 설계·계획을 합친 문서이며, 맨 아래 "계획 기록"은 합친 계획의 노드 이동 결과를 가리킨다(옛 판은 git `10914570`).

## 변경한 공용 계약

로비의 이동/설정 두 일반 C# 클래스를 Common의 `ChessPieceMotion`·`ChessPieceMotionSettings`로 옮기고 meta GUID를 유지했다. 로비는 기존 world 모드와 수치를, 맵은 local 모드와 StageMap 설정을 쓴다. Framework 내부·Factory 등록·새 서비스 키는 추가하지 않았다.

기존 `ICombatEntryService`는 `IMapCombatPresentationOwnerSource` 역할도 제공한다. 맵 Scope는 `BindMapPresentationOwner()`로 lease 하나를 소유하고 종료 때 Dispose한다. 요청은 그때의 owner를 ticket으로 캡처한다. ticket의 Pending/Ready/Abandoned는 표시 완료 상태이며 Core의 수락·현재 노드·전투 결과를 대체하지 않는다.

기존 전투 준비 수락과 Job 완료 뒤, 맵 입력 번호가 있는 요청만 기존 진입 전환 Job에서 착지 Ready를 기다린다. 현재 lease와 현재 준비 요청이 일치할 때 `TryComplete(request)`가 한 번 게시된다. 정상 `BeginActivation` 뒤에는 씬 이탈의 lease 폐기가 통과한 gate를 다시 취소하지 않는다. owner 상실은 Abandoned로 진단한다. Bootstrap 번호 0의 직접 진입은 이 대기에서 제외한다.

## 수명과 작업 경계

맵 Scope가 이동과 표시 owner를 함께 정리하며, Delay 완료 콜백은 직접 씬/팝업을 열지 않는다. 현재 View Tick이 착지를 소비하고 기존 경로를 계속한다. 새 System·팝업 수명·Core 규칙·CombatPlan 등록 목록은 만들지 않았다. 전투 패배·포기·승리 목적지 정책과 FieldCamera·Architecture는 변경하지 않았다.

J5의 "Prepared + Job 완료 뒤 전환"에는 맵 요청에 한해 표시 gate가 추가됐다. 해당 부분은 [J5 후속 참조](../specs/2026-10-10-combat-job-plan-restructure-design.md#j5-진입--combatentryservice-분리-d3-a-확정)로 연결했다. 최초 NM2 439건 통과 뒤 테스트 SO 메모리 오염을 발견해 복제 SO로 격리했고 후속 440건이 통과했다. NM3 실제 Boot는 1/1, 현재 placeholder의 분리 Player 숨김·복구 1,000쌍/pass는 관리 할당 0 B였다. 사용자는 노드 크기·연결선·클릭 이동·착지 후 진입의 Play 정상을 확인했다. 일반 승리 뒤 같은 맵의 현재 완료 노드 즉시 복원과 다음 상점 클릭·착지·팝업 완료는 별도 운영 1/1로 추가 확인했고 관련 회귀 447/447을 통과했다. 상세 사람 체크·반복 이동 전체 비용과 전투 캡처 후 Player Quit 시간 초과는 별도다. 조건·미실시·종료 한계는 계획 기록을 따른다. Core에 새 구현을 요청할 항목은 현재 없다.
