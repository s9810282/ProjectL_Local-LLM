# 비전투 View 구조 개선 — 민형님 Framework 공유 (2026-10-15)

- 작성자: 호진(이호진)
- 상태: **작성 완료·미전달.** 사용자가 직접 전달한다. 외부 합의·인수 완료 기록이 아니다.
- 원본: [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md) §3~§6·§8·§12와 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md). 둘은 2026-10-27에 옛 비전투 View 설계·계획을 합친 문서이며, § 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`).

**최신 코드 대조(2026-10-16):** HEAD `70de006629feaf880f876c7b8e41fa14297237cf`의 소스·문서를 확인했다. 아래 P3~P5 구조와 이후 별도 회복 등록을 구분하며 과거 자동 검증·Play·Profiler를 이번에 재실행하지 않았다. 전달 상태는 여전히 미전달이다.

P3에서 아티팩트 독립 View 모듈을 로비로 흡수해 당시 View 모듈이 하나 줄었다. 현재 `InGameViewSystemFactory`는 Combat·StageMap·Lobby View와 이후 ER3에서 추가한 `RecoveryRequestLifetime`을 등록한다. `LobbyViewSystem`만 로비 Enter 100·Exit -100에 연결한다. `SceneIndices.Artifact` 4는 예약으로 유지한다. `LobbyViewScope`·`StageMapViewScope`·`PopupViewScope` 자체는 사용 단위 수명 객체이며 서비스나 Framework 모듈로 추가하지 않는다. 회복 요청 수명의 좁은 역할 등록과 씬 표시 수명 분리는 [회복 연결 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)·[회복 UI·Framework 공유](2026-10-15-recovery-er4-ui-framework-share-Hojin.md)를 따른다.

System은 기존 수명 단계·서비스 연결·씬 Enter/Exit 등록을 맡고, 씬 연결 한 번의 하위 객체 생성·Tick·폐기는 Scope가 맡는다. 새 Scope를 외부 호출 전에 게시하고 현재 소유자를 검사해 이전 정리가 새 UI·Layer·카메라를 침범하지 않게 한다. 팝업별 Factory는 고정 의존성을 받아 팝업 한 벌을 만들며, 만든 객체의 수명은 부모 Scope가 소유한다. 로비 카메라는 폐기 마지막에 기존 Handle 경로로 반납한다(FW-011).

DR-009의 동기 Canceled 입력을 다루기 위해 연결 코드에서 **UI 알림 밖에서만 Layer를 변경**한다. Open은 UI 서비스 호출 전, Close는 반환 뒤, 외부 숨김/열림은 다음 Tick, 이미 열린 화면의 채택은 Start에서 처리한다. `IPopupViewHost`는 현재 씬 연결 확인과 호스트 차단만 제공한다. UI 기반 계약을 추가하거나 바꾸지 않았다([팝업 수명 원본(옛 §4.2)](../specs/2026-10-13-view-structure-design-Hojin.md)).

Framework 소스·6단계 수명·Builder 1회 Build·서비스 키·등록 API·JobRunner 계약을 유지했다(FW-001~004). 별도 루프·DI 묶음·이벤트 버스는 없다. 상태와 규칙의 원본은 기존 Buffer/Core이며 DB Query는 표현 데이터 조회만 한다(SSOT). System·Scope·Factory·Controller·Screen의 책임을 나누고 필요한 좁은 인터페이스만 받는다(SOLID, JMH 지침 §2·§7·§15·§27). 이는 View 연결 방식 정리이며 새 Framework 규격을 요구하는 변경은 아니다.

공용 오류 콜백은 기존 `InGameViewFaultCleanup` 한 곳이며 현재 전투 → 맵/상점·회복 표시 → 로비 → 회복 요청 수명 순서다. P3 당시 세 View의 순서는 보존하고 ER3가 선택 `recovery` 콜백을 끝에 추가했다. 로비 아티팩트 **기록 누락**만 도메인 정지·이탈 허용으로 분리했고 실제 Job 예외와 전투 F1은 공용 오류 경계를 따른다. 숨김 사유·UI 전체 dispatch 경계·측정 범위의 한계는 설계와 실행 기록에 유지한다. P6에서는 문서만 검사하며 이전 자동 검증·Play·Profiler를 다시 실행한 것으로 기록하지 않는다.

**P3.6/R6 후속(2026-10-15, 미전달):** P6 코드 대조에서 찾은 정리 helper 중복을 별도 사용자 승인으로 보완했다. `InGameViewFaultCleanup`은 캡처한 전투 → 맵/상점 → 로비 콜백을 호출부에서 null 검사한 뒤 기존 `CleanupErrors.Try`로 실행하고 `ThrowIfAny`로 보고한다. private `TryStop`을 제거했으며 정리 순서·재진입·Disconnect 중 지역 사본·원본 예외/메시지를 유지한다. 공용 helper·Framework 계약을 바꾸지 않았다. 관련 Native 테스트 42/42·전체 새 실패 0·Rebuild 오류/새 경고 0이며 남은 실패와 변경 전후 증거는 [R6 후속 기록](../plans/2026-10-13-view-structure-plan-Hojin.md)을 따른다. P6 문서 검증과 이번 실행을 구분하며 이 메모의 외부 전달 상태는 여전히 미전달이다.

**P2.5/P3.8 검증 후속과 16일 경계:** UI 서비스 반환 뒤 입력 반환과 즉시 새 부모 보호를 강화했지만, 즉시 게시·새 Layer 미취득 조합은 사용자 승인으로 검증 보존에서 제외했다([설계(옛 §9)](../specs/2026-10-13-view-structure-design-Hojin.md)). 좁은 예외와 당시 검증은 유지한다. 16일 맵 이동의 표시 대기·owner lease는 [노드 이동 Framework·Combat 공유](2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md)를 따르며, 이 메모에서 새 Framework 규격이나 Core 요청을 추가하지 않는다. System/Factory의 조립 변경과 팝업별 표시·입력 구현을 분리해 기능 내부 변경이 관련 없는 팝업 구현까지 따라가지 않게 한다. 공용 Scope·입력 소유 계약 변경의 통합 확인은 남는다([기능 독립성 기준 §4.6](2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리)).
