# 상점 계획

- 작성자: 호진(이호진)
- 상태: 상점 S0~S5와 상점 안내·노드 표시 완료(2026-10-11). 이벤트 팝업 PPT UI 정리 완료(2026-10-19), 이 정리의 Unity 표시·EditMode 확인은 남았다(§2).
- 설계: [상점 스테이지 UI 설계](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md)
- 정리: 2026-10-27 1부(상점 스테이지 구현), 2부(상점 안내와 스테이지 위치 표시), 3부(이벤트 팝업 PPT UI 정리)를 결과 요약으로 줄였다. 결정 P1~P14와 성능 규칙 R1~R7은 [설계 §3·§7](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md#3-결정-기록)로 옮겼다. 끝난 Task 단계, 시험 통과 수, `Logs/` 증거 경로, Profiler 수치는 git `10914570`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| S0 기준선 | EditMode 전체 기준선, 표시 DB 확인 | 완료 | 사용자 Test Runner(10-09) | — |
| S1 DB 임시값 | 후보 Akimbo 2·ShotGun 3·Sniper 5, 시작 재화 20 | 완료. 뒤에 혼합 상점으로 값이 바뀜 | 사용자 확인 | `7345109e` |
| S2 요청·Job | `ShopRequest`·`ShopScheduler`·`ShopJob` | 완료 뒤 SA P5·P6에서 번호 경로로 교체·삭제 | 사용자 Test Runner | `7345109e` |
| S3 진입 흐름·복구 | `ShopPopupController`·결과 분류·복구·맵 갱신 | 완료 | 사용자 Test Runner(10-10) | `7345109e` |
| S4 화면 계약·모델 | 카드 값·모델 Builder·`UIId.Shop = 14`·화면 작업 안내 | 완료. Host/Screen 계약은 SA P6에서 교체 | Unity 시험(당시) | `7345109e` |
| S4V 화면 | `ShopUI`·카드 프리팹·StageSelect 연결 | 완료(SA P5·P6) | 실제 프리팹 시험(당시) | `7345109e` |
| S5.0 등록 | `Stage_Shop`(1201)·`NodeVisual_Shop_Regular` 표 등록 | 완료(사용자 승인) | 자동 회귀(당시) | `7345109e` |
| S5.1~S5.3 | 씬 연결 확인, 실제 Play 자동 검증, 전체 EditMode 비교 | 완료 | 자동 Play·EditMode(당시). 사람 수동 Play 아님 | `7345109e` |
| S5.3a Profiler | 상점 무입력 측정, Stage Flow 32B/frame 제거 | 완료 | Development Player 측정(당시) | `7345109e` |
| 측정 코드 제거·배경 가림 | 측정 표식 제거, 상점 배경 불투명 | 완료 | 자동 회귀(당시) | `7345109e` |
| S5.4 문서 통합 | 설계·상위 계획 상태 정리 | 완료 | — | — |
| 상점 안내·노드 표시 | 중복 "구매 완료" 제거, "구매했습니다." 3초, 현재 캡슐·완료 봉 | 완료. 3초 숨김은 10-19 혼합 상점에서 빠짐 | 자동 검증(당시) | `3ea220b0` |
| 이벤트 팝업 UI | PPT 표시 항목·하단 배치·카드·골드 상자·정리·골드 부족 안내 | 완료 | 컴파일만. Unity 표시·EditMode 미확인 | `58e36c6a` |

## 2 남은 일

- [ ] 10-19 이벤트 팝업 UI의 Unity 확인: 카드 아래 골드·버튼 위치, 긴 한글 잘림, 숨김 영역, "선택하지 않는다"의 완료·맵 복귀, 구매 뒤 완료 재시도, 골드 부족 안내 표시·배경·닫기 정리. 관련 EditMode(`ShopUITests`·`ShopPopupControllerTests`·`ShopPopupModelBuilderTests`·`RecoveryUITests`·`StageShopUIAssetTests`)도 돌린다.
- [ ] 사람의 수동 Play와 미관 확인(자동 Play는 S5.2에서 했다).
- [ ] Q1 Run seed 소유 결정(설계 §6). 요청 R4는 미전달이다.
- [ ] 시작 Run 재화 5000(`34ee0de4`)이 시험용인지 확인(설계 §5.2).
- [ ] Player 메인 스레드 전체에 남는 96B/frame(상점 밖 `GameBootstrap.Update` 아래, 공용 그룹 순회가 유력 후보)은 따로 측정할 항목이다.
- [ ] S5.2에서 본 전투 바닥 자홍색 표시는 전투 진입 검증과 별개의 미해결 시각 문제로 남겼다(GridDecal 셰이더 GUID 불일치 관찰).
- 진행 전: 상점·회복 "제출 중" 하나와 두 팝업 공통 기반은 [로비·스테이지 검사 후속 정리 계획](2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md)에서 한다.

## 3 작업별 결과

### 3.1 상점 스테이지 구현 S0~S5 (2026-10-09~11)

- S0: 기준선 3504개 중 실패 35(14개 클래스)를 기록했다. 상점 작업은 이 기존 실패를 고치지 않았다.
- S1: 후보는 등록된 `InGameGroup/Unit`의 `UnitDataSO`를 끌어다 놓았다. Core 전달 문서 §5.14에 "기획 값으로 바꿀 임시값"으로 적었다.
- S2: 요청 객체·스케줄러 방식으로 만들었고(P13), SA P5·P6에서 번호 계약으로 바꾸며 지웠다.
- S3: 계획과 다른 점 둘(사용자 확인 2026-10-10). 맵 갱신은 Buffer 비교가 안 돼(클리어를 제자리 변경, `GetStages`가 호출마다 래퍼 할당) 완료·불일치 때 Controller가 한 번 알리는 방식이다. `UIId.Shop = 14`를 S4 대신 S3에서 더했다. 결과 분류표(설계 §4.1)는 사용자가 확인했다.
- S4: 화면에 보이는 부분은 사용자와 다른 AI 몫이라(P11) [ShopUI 작업 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md)를 썼다.
- S5.0: 사용자 승인으로 `StageRootDataTable`(11 → 12)·`StageNodeVisualDataTable`(2 → 3)에 상점 참조를 하나씩 더했다. 기존 순서·GUID는 그대로다. 이벤트는 그때 등록하지 않았다.
- S5.2: 실제 Play 자동 검증(EditMode 안 `UnityTest`가 Boot·StageSelect·Combat을 구동)으로 진입·구매·나가기·클리어 표시·다음 전투 진입을 확인했다. 첫 실행에서 `ShopPopup` Layer 등록 누락을 찾아 `RunTimeSystemFactory.cs`에 한 줄 더했다([Framework 공유 §9](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md)). 시험은 무작위 맵을 찾으므로 `[Explicit]`이다. 골드 부족은 첫 상점에서 자연히 생기지 않아 시험 안에서만 잔액을 0으로 두고 확인한 뒤 되돌렸다.
- S5.3: 전체 EditMode에서 직전 결과 대비 새 실패 0, 기존 실패 34는 그대로였다. 전체 통과가 아니다.
- S5.3a: 상점이 열린 채 입력이 없을 때 `StageNodeEntryFlow`가 매 프레임 맵 준비를 조회해 32B/frame을 할당하던 것을, 미처리 입력을 먼저 보도록 순서만 바꿔 없앴다(회귀 시험 3개 유지). 측정한 Stage Flow/View·상점 구간은 GC 0B였다.
- 뒤이은 사용자 요청: Profiler 표식·측정 전용 시험·임시 Player 소스를 모두 지우고, 상점 배경 alpha를 0.96 → 1로 바꿔 뒤 맵을 가렸다. 회복 화면은 바꾸지 않았다.

### 3.2 상점 안내와 스테이지 위치 표시 (2026-10-11)

- 구매 버튼의 "구매 완료"·비활성은 두고 버튼 아래 중복 표시만 지웠다. "구매했습니다."는 `IDelayService`로 3초 뒤 숨겼다.
- 맵에 현재 노드 캡슐 하나를 두고(선택 전 시작 노드, 뒤에 `CurrentStage.NodeId`), 떠난 클리어 노드는 봉만 보이게 했다. 인터페이스 `Show(..., currentNodeId)`·`SetCurrentNode(int)`·`TryReadCurrentNode(out int)`. 노드 표시의 원본은 [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)다.
- 바뀐 것: 3초 숨김은 천명님 혼합 상점(`a5133b32`, 10-19)에서 Controller의 Delay와 함께 빠졌다. 구매 성공이 바로 완료로 이어지기 때문이다.

### 3.3 이벤트 팝업 PPT UI 정리 (2026-10-19)

이벤트 스테이지 기획서 PPT(6~7·9~10쪽)와 사용자 이미지 교정을 따라 상점·회복 화면을 함께 바꿨다. Core·DB·입력 계약은 바꾸지 않았다. 지금 규격은 [설계 §5.1](../specs/2026-10-06-stage-shop-recovery-ui-design-Hojin.md#51-화면)이다.

| 단계 | 한 것 |
|---|---|
| UI | 별도 나가기 버튼 없이 기존 완료 버튼을 "선택하지 않는다"로, 카드 버튼은 "선택한다". 제목·추가 HP·결과·상태 영역 숨김. 골드 왼쪽·완료 버튼 오른쪽을 카드 아래 같은 높이에 |
| Card | 사용자 교정으로 "기존 카드 크기 유지"를 바꿨다. 새 `StageEventOfferCard.prefab`(이미지 / 이름·가격 / 선택 버튼, 480×840, 간격 144). 공용 `Shop/StageOfferCard.prefab`·옛 `RecoveryPanel.prefab`은 그대로 |
| Fit | 카드 묶음을 0.8배·y 5로 줄여 좌우 여백을 같게 |
| Gold | 골드 뒤에 완료 버튼과 같은 색·크기의 Image 배경(입력 없음) |
| Cleanup | 사용자 요청으로 숨겨 둔 상점 Title, 회복 Title·Health·Receipt, 카드 Details를 지웠다. `RecoveryUI.healthText`·`receiptText`와 검사도 지웠다. `StageOfferCardView.secondaryText`는 선택 참조가 됐다 |
| Notice | Core `InsufficientFunds`만 "골드가 부족합니다."로 Status에 보인다. 다음 제출·닫기·새 표시에서 지운다. 회복은 골드 부족 사유 카드도 제출할 수 있게 했다(판정은 Core) |

- 확인: 프리팹 참조·배치·입력 비차단 정적 검사와 MSBuild 컴파일만 했다. 열린 에디터 때문에 Unity EditMode·실제 렌더링은 하지 못했다(§2).
- 뒤의 변경: 강화 A2(10-20)가 카드를 `StageEventOfferCard`의 Variant `StageEventOfferDescriptionCard.prefab`(480×840, 원본은 480×720로 유지)으로 바꾸고 설명 영역을 다시 켰다. 강화 B(10-20)가 완료 대기·정상 거절 안내를 Status에 더했다. 기록은 [강화 계획](2026-10-19-enhancement-view-integration-plan-Hojin.md).

### 3.4 혼합 상점 대응 (천명님, 2026-10-19)

천명님 `a5133b32`가 유닛·아이템 혼합, 방문당 한 개 구매, 구매 뒤 자동 완료, 완료만 재시도를 넣으며 `ShopPopupController`·`ShopPopupModelBuilder`·`Shop_Default.asset`을 고쳤다. 검증 기록은 [혼합 상점 검증](../handoffs/2026-10-18-mixed-shop-verification.md)이다. 이 계획에서 한 작업은 아니며 결과만 적는다.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [상점 UI — 현강님 공유](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) | 현강님 | 미전달 |
| [StageSelect Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md) §9 | 민형님 | 미전달 |
| [StageSelect Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md) 2부 G1~G5·3부 R4 | 천명님 | 미전달 |
| [SA Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md) | 천명님 | 미전달 |
| [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.14 | 천명님 | 기록(전달은 그 문서 상태를 따름) |
