# 이동 프로필 읽기 추적 Task 계획 — MC2B (2026-11-02)

- 구현 계약 원본: [MC2B 설계](../specs/2026-11-02-movement-profile-read-tracking-design.md). 상위 진행 기록: [이동 StaleContext 계획](2026-11-02-movement-context-definition-comparison-plan.md).
- 상태: **MB1·MB2 구현·Unity 컴파일 확인 완료.** 최종 기존 EditMode는 **5,619 통과/8 실패**다. 후속 사용자 진지 구축 정상 확인을 반영했다. rank별 전수와 다른 세부 인수는 미확인으로 남긴다. 사용자 "남은 Phase 끝까지 진행하고 마지막에 검증 및 테스트 작업 진행해" 지시로 §9의 추가 파일을 포함해 마지막 검증까지 진행했다.
- 새 시험 작성·MSBuild 없음. 기존 시험의 검증 내용/입력/실행 흐름은 바꾸지 않는다. 다른 작업자가 작성한 시험의 실행 실패를 수정해야 하면 먼저 묻는다. 기존 시험 실행/Unity 확인은 해당 사용자 지시 범위에서만 수행한다.
- 후속 사용자 요청으로 이번 작업의 코드·meta·관련 문서를 Git 스테이징 대상으로 준비한다. 커밋·push·외부 발송은 하지 않는다. 사용자 에셋 변경은 유지한다.

## MB0 — 의존 경로·설계·Task (문서 완료)

- [x] 직접 이동뿐 아니라 환경/장애물 피해의 수신자·오라·존재 재계산·도발·처치 특성/사냥꾼 읽기까지 대조한다.
- [x] 불변 입력/Contracts/Domain을 바꾸지 않는 Application 내부 수집기와 RequiredProfile 내부 오버로드를 설계한다.
- [x] 수집기 수명, 동일 개체 반복/혼합 선택, 실패 경계, 부정 후보 기록, 공개 투영 범위를 정한다.
- [x] 기존 EffectTargets/Definitions/OwnedProfiles로 동결하고 프로필 의존과 영혼 수집 사실을 분리한다.
- [x] 파일 범위·신규 meta·기존 공개 서명 유지·코드 Phase·정적 확인·사용자 인수를 구체화했다. 설계 §9의 추가 파일은 후속 사용자 지시로 포함했다.
- [x] Core 구조·SSOT·SOLID 5원칙·JMH/FW·변경 독립성·캡슐화·C#/Unity·if/Tick를 정적 검토한다.

**진행 결정:** 최신 사용자 지시는 남은 Phase를 끝까지 구현하고 마지막에 검증·시험하는 것이다. 이번 실행에서는 MB1/MB2의 중간 중단을 생략하고 최종 결과에서 함께 보고한다.

## MB1 — 내부 수집기와 전달 경로 (구현·정적 확인 완료)

이 Phase는 Capture가 수집기를 켜기 전까지다. 사용자 지시로 결속력 방어 간접 읽기를 포함한 **18 C#(신규 1·기존 17) + 신규 meta**를 구현했다. MB2는 MovementContextCalculator 1개다.

- [x] MB1.1 CombatProfileReadSet.cs/meta 추가. EntityId별 선택 기록·반복 읽기 중복 제거·혼합 선택/잘못된 입력의 예외를 구현한다. 새 수집기의 static 캐시/수명 서비스는 만들지 않는다.
- [x] MB1.2 기존 2인자 RequiredProfile의 본문/선택 의미를 유지하고, 같은 결과를 기록하는 내부 3인자 오버로드를 추가한다.
- [x] MB1.3 ProjectRoute에 선택적 수집기 인자를 추가한다. Actor/CanLand 등 투영 생성 전 읽기와 직접 효과 읽기에도 전달한다. 기존 일반 호출은 null 동작을 유지한다.
- [x] MB1.4 ProjectionDependencies/MovementProjection에서 받은 수집기를 보관하고 PositionSwapShields·ReconSquadApplication·TraitTrailCalculator·MovementEnvironmentCalculator·TornadoCalculator·UnitBehaviorRules로 전달한다. 기존 피해 객체를 이어받으면 같은 수집기를 이어받는다.
- [x] MB1.5 CombatDamageApplication 생성자/For의 선택적 마지막 인자로 전달하고, 존재 재계산·CaptureIncoming·처치 정산에 연결한다. TraitPresenceReconciler의 공개 Calculate 서명은 유지하고 내부 경로만 확장한다. 도발 후보는 TauntTargeting까지 전달한다. 결속력 방어는 CombatEffectiveStatsCalculator의 내부 CohesionDefenseBonus까지 전달하며 산식/후보 조건/공개 API는 유지한다.
- [x] MB1.6 TraitKillSettlement/HunterApplication.TargetDied는 피해 객체의 수집기로 읽는다. 공개 투영 뒤의 Complete 전용 Reconcile·공격/보상 전용 파일은 임의로 넓히지 않는다.
- [x] MB1.7 설계의 모든 읽기 경로에서 같은 수집기를 받는지 정적 대조한다. 2인자 선택 원본·기존 후보 조건/정렬/규칙/예외·공개 API·Contracts/Domain·시험 본문이 유지되는지 diff로 확인한다. 누락 경로/추가 파일이 필요하면 구현 전에 설계를 갱신한다.
- [x] MB1.8 새 GUID/meta·LF/UTF-8·Core Unity 참조 부재·diff를 확인하고 인계에 실제 파일/계약/잔여 조건을 기록한다. 최신 사용자 지시로 MB2 및 최종 검증까지 계속 진행했다.

MB1 단독 상태에서는 수집기가 비활성이고, 이어진 MB2에서 Capture에 연결했다. 기존 이동 값 비교는 MC2A를 유지한다. 새 시험은 작성하지 않는다.

## MB2 — Capture 활성화·기존 기대 목록 동결 (구현·정적 확인 완료)

대상은 MovementContextCalculator.cs다. MB1의 수집기 API 변경이나 추가 파일이 필요하면 승인된 범위를 임의로 넓히지 않는다.

- [x] MB2.1 Capture 시작에 새 수집기를 만들어 공개 ProjectRoute에 넘긴다. 실패한 투영은 기존처럼 문맥을 반환하지 않으며 수집기도 보관하지 않는다.
- [x] MB2.2 읽은 EntityId의 원래 입력 사실을 기존 EffectTargets에 합쳐 선택 주체를 보호한다. Mover 중복은 추가하지 않되 기존 효과 대상은 제거하지 않는다. 입력에 없는 읽기 주체를 조용히 제외하지 않는다.
- [x] MB2.3 실제 반환된 프로필만 기본 Definitions/보유 OwnedProfiles에 분류·중복 제거·기존 키 순으로 동결한다. Capture의 별도 기본/보유 선택 조건은 제거하고 RequiredProfile을 다시 구현/호출하지 않는다.
- [x] MB2.4 Collectors/Owned/SoulCounts/Main/Souls·환경 필터/경로/전투/턴·나머지 거절 검사·MC2A 값 비교를 유지한다. 새 DTO/생성자/With 복사·가변 수집기 노출이 없는지 확인한다.
- [x] MB2.5 동일 설정 재생성·키 집합·EntityId/OwnedId 결합, 기본형 대체, 부정 후보·면역/도착 전 종료·피해 전후 존재 판정·오라/처치 읽기의 동결을 정적 검토한다.
- [x] MB2.6 diff·LF/UTF-8·순수 Core 경계를 확인하고 실제 변경/기존 인수 항목/잔여 상태를 인계에 기록한다. 이후 최종 진행 지시로 검증까지 진행했고 MB3.0a의 Unity 컴파일 확인을 완료했다.

## MB3 — 최종 검증·기존 시험 실행 완료, 세부 인수 미확인

사용자 최종 지시로 Unity 컴파일과 기존 시험을 실행한다. 새 시험/판정 수정·MSBuild는 하지 않는다. 아래 세부 시나리오는 기존 시험으로 직접 입증한 범위와 수동 재현이 남은 범위를 구분하며, 실행하지 않은 항목을 통과로 표시하지 않는다.

- [x] MB3.0a Unity 최종 컴파일 확인. 기존 CS8632 경고를 변경 전 로그와 대조했다. MSBuild 없음.
- [x] MB3.0b 기존 EditMode 전체 5,627건을 실행하고, 생성자 호환 회귀를 코드에서 복구한 뒤 전체 재검증했다. 최종 5,619 통과/8 실패·건너뜀 0.
- [x] MB3.0c 현재 소스/기존 계약·diff·GUID·UTF-8/LF·순수 Core 경계와 잔여 실패 근거를 인계 §14에 기록했다.
- [x] MB3.0d 기존 개발 설정으로 Combat 진입·샷건/메인 배치까지 확인했다. 재생을 끝내고 임시 재생 레이아웃 설정을 원복했다. 이동 확정은 미확인이다.
- [x] MB3.U1 후속 사용자 진지 구축 정상 확인을 인수했다. 확인한 rank/구매/오라 조건은 미보고이며, 아래 세부 인수를 일괄 완료 처리하지 않는다.
- [ ] MB3.1 샷건 rank 1/구매 없음 및 rank 2·3, 동일 설정 재생성, 실제 수치/키/선택 변경의 수락·거절을 확인한다.
- [ ] MB3.2 움직이지 않은 진지 구축 출처의 반경/감소율 변경, 범위 밖→안쪽, 피해 0/효과 없음 후보를 구분한다.
- [ ] MB3.3 위치 교환·강제 이동 면역·도착 전 사망/종료, OwnedId 없는 대상도 필요한 읽기가 기록되는지 확인한다.
- [ ] MB3.4 단독 행동/결속력/도발 존재 판정, 사냥꾼·처치 출처가 실제 읽기 집합에 반영되는지 확인한다.
- [ ] MB3.5 같은 기본형을 공유하는 두 개체의 선택 주체, 기본형 대체·보유형 선택, 사용하지 않은 기본/보유 프로필만 바뀐 경우를 구분한다.
- [ ] MB3.6 숨겨진 환경만 바뀐 경우 공개 문맥이 확대되지 않는지, 기존 전투/행위자/초안 경로 변경 거절을 확인한다.
- [x] MB3.7 컴파일/기존 시험 실패가 있으면 원인과 작성 주체/변경 범위를 먼저 확인한다. 기존 시험 판정을 바꿔 통과시키지 않는다. 결과 8의 실행 실패 항목이 여전히 필요하면 별도 계측을 설계한다.

## 실행 기록

- 2026-11-02: 사용자 "다음 진행해"로 MC2B 설계/Task를 구체화했다. 직접 이동 이외의 피해→존재/도발/처치/사냥꾼 경로를 확인했다. 실제 선택 결과를 기록하는 Application 수집기와 기존 DTO 목록의 동결 방식을 채택했다.
- 이번 단계는 문서만 변경했다. MB1/MB2 코드·meta·시험·에셋·Git 스테이징은 수정하지 않았다. MC1의 예전 시험 결과를 MC2A/MC2B 검증으로 확대하지 않는다. **컴파일 미확인.**
- 2026-11-02 MB1 사전 대조: 사용자 "다음 진행해" 지시 후 실제 원본을 대조하다 결속력 방어의 간접 RequiredProfile 읽기를 확인했다. 기존 전달 목록의 누락을 인정하고 CombatEffectiveStatsCalculator.cs 1개 추가안을 설계 §9에 구체화했다. 코드 편집은 시작하지 않았으며 범위 확인 대기다. 방어 규칙을 복제하거나 기록 누락을 조용히 허용하는 우회는 하지 않는다.

- 2026-11-02 최종 진행 지시: 추가 CombatEffectiveStatsCalculator를 포함해 MB1·MB2를 구현했다. 기존 선택기 본문·규칙/후보/산식·Contracts/Domain·공개 DTO·시험 본문은 유지했다. 62개 전달 편집의 역대조와 Capture의 나머지 본문 보존, diff·GUID·LF/UTF-8·Core Unity 참조 부재를 확인했다.
- Unity 컴파일 확인 뒤 EditMode 5,627건을 실행했다. 첫 실행에서 리플렉션 호출에 필요한 기존 MovementProjection 2인자 생성자가 사라진 호환 실패 5건을 발견했다. 기존 생성자를 같은 구현에 위임하도록 복구했고 시험은 수정하지 않았다. 재컴파일 후 전체 재검증은 5,619 통과/8 실패로 끝났다. 상세 최종 결과는 [구현 인계 §14](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#14-mb1mb2-구현과-최종-검증-2026-11-02)에 기록한다.

- 2026-11-02 후속 전달 준비: 사용자 진지 구축 정상 확인과 이번 작업 Git 스테이징 요청을 반영했다. 확인 범위·Core 인수 계약·기존 시험 결과·잔여 인수의 현재 요약은 [Core 구현 전달 문서](../handoffs/2026-11-02-movement-stale-context-core-implementation-handoff.md)를 따른다. 이번 준비의 실행 코드/시험 변경은 없다.
