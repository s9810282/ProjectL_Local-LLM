# 아티팩트·전투·맵/상점 공용 오류 정리 변경 기록

- 작성자: 호진(이호진)

> **2026-10-16 코드 재대조:** 공용 입력 정리는 전투 → 맵/상점·회복 표시 → 로비 → `RecoveryRequestLifetime`이며 아티팩트는 LobbyViewScope 안에서 정리된다. 마지막 회복 요청 수명은 [ER3 기록](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#38-er3-runtime-요청-수명과-실행-경로-2026-10-15)의 후속 연결이다. `ArtifactViewSystem`·`ArtifactPopupSession`과 helper의 아티팩트 인자는 제거됐다. 구조·수명·기록 누락 정책은 [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)(옛 비전투 View 구조 개선 설계 §3~§6), P0~P5 구현·검증은 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md)을 따른다. 아래 P3.9·P3.10과 10-11 전달 사항은 당시 기록이며 외부 미전달·미합의 상태를 유지한다. 최종 공용 변경 공유는 [전투 View 공유 메모](2026-10-15-noncombat-view-scope-combat-share-yonggeun-Hojin.md)에 정리했다.

작성일: 2026-10-06, 갱신: 2026-10-15(P6 문서 정리). 사용자 승인으로 아래 변경을 구현했으며 **전투 작업자 미전달·미합의**다. P3.9의 합의 전 구현 보류는 당시 사용자 요청으로 해제됐다. SA P5·P6의 Core/전투 맵 번호 연결과 사용자 병합 후 전환 수명 보완도 구현했다. 이 문서의 승인이나 검증을 외부 인수 완료로 취급하지 않는다.

진행 원본: 초기 기록은 [아티팩트 팝업 계획 §9](../plans/2026-10-05-artifact-popup-plan-Hojin.md), SA P3.9·P3.10은 [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)(옛 SA 계획), 현재 P0~P5는 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md)(옛 비전투 View 구조 개선 계획)이다.

현재 범위는 [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)(옛 비전투 View 구조 개선 설계 §5.1·§6)를 따른다. [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md)(옛 SA 설계 §2.4.2)와 병합 `9d4c76e5`의 [스테이지 맵 계획 §4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 병합 검토 §7)는 이전 구조·검증 이력이다. 이번 문서 정리에서 Unity를 다시 실행하지 않았다.

## 현재 확인한 계약

- RuntimeFaultBoundary.ConnectStopInput은 런타임당 콜백 하나만 허용한다.
- P3.9 이전에는 Factory가 combatView.StopInput을 연결하고 CombatViewSystem.Shutdown이 그 콜백을 해제했다. 아티팩트/상점의 독립 오류 콜백은 없었다.
- Factory는 같은 경계를 CombatViewSystem·StageMapViewSystem·LobbyViewSystem에 주입하고 InGameViewFaultCleanup.StopInput 하나를 연결한다. 그룹이 helper를 소유하고 종료 때 연결을 해제한다.
- `LifeCycleSystemFactory`도 같은 오류 경계를 StageLifeCycle·CombatLifeCycle·독립 CombatEntryService·LobbyLifeCycle·ArtifactBoardPreparation에 전달한다. 삭제된 CombatFlow나 각 화면의 별도 오류 경계·정리 콜백을 다시 만들지 않는다.
- GameBootstrap은 fault 이후 Runtime.Tick을 중단한다. 아티팩트 Tick에서 fault를 발견해 정리하는 것으로는 충분하지 않다.
- LobbyViewSystem.StopInput은 정지 상태로 전환하고 LobbyViewScope를 폐기한 뒤, Scope 유무와 관계없이 아티팩트 입력·프리뷰 Buffer를 비운다. 아티팩트 Controller는 PopupViewScope·슬롯·드래그·표시·카메라 구독을 직접 정리한다. 각 View 내부 자원은 공용 helper가 직접 조작하지 않는다.

## 사용자 승인으로 반영한 결정

RuntimeFaultBoundary는 단일 콜백 계약을 유지한다. Framework/UI 문서가 이 helper를 필수로 정한 것은 아니다. 여러 View를 정리하는 콜백의 연결과 해제를 같은 수명 소유자에 두기 위해 조립 계층을 선택했다.

1. **소유자:** InGameViewSystemGroup. Factory 조립 때 helper가 연결하며 그룹 Shutdown의 finally에서 같은 콜백을 해제한다. helper는 일반 C# 객체로 새 Framework 모듈/서비스/Tick 등록이 아니다. Combat.Shutdown은 자기 Detach만 수행한다.
2. **즉시 정리 순서:** 현재 전투 → 맵/상점·회복 표시 → 로비(아티팩트 포함) → `RecoveryRequestLifetime`. 한 정리가 실패해도 나머지를 실행하고 AggregateException에 원인들을 보존한다. RuntimeFaultBoundary.FirstError는 최초 원인 그대로다. R6 후속에서 공용 `CleanupErrors` 위임으로 정리했으며 private `TryStop`은 제거됐다([R6 실행 기록](../plans/2026-10-13-view-structure-plan-Hojin.md)(옛 비전투 View 구조 개선 계획 R6 후속 기록)). 정상 Shutdown도 동일 입력 정리 후 기존 자식 모듈 역순을 모두 시도한다.
3. **종료 수명:** `GameBootstrap.ShutdownOnce`와 그룹 `Shutdown`은 각각 반복/재진입을 막는다. 정상 Boot 종료에서는 Bootstrap의 `StopInputForShutdown`과 그룹 `Shutdown`에서 helper를 각각 호출해 네 정리 콜백을 **두 번 실행한다**. 이후 `RecoveryRequestLifetime.Shutdown`이 자기 `StopInput`을 한 번 더 호출하므로 이 정상 경로의 회복 요청 정지는 총 세 번이다. 각 소유자는 반복 정리를 허용해야 한다. 이는 그룹을 독립 종료할 때도 입력 정리를 보장하는 현재 호출 구조이며, 입력 정리 자체가 런타임당 1회라는 뜻이 아니다. 정리 시작 때 콜백들을 보관하므로 도중 종료가 연결을 해제해도 남은 함수를 호출한다. 자식 Shutdown이 실패해도 최종 연결은 해제한다.
4. **입력/상태:** 오류 때 새 입력과 같은 런타임 재연결을 거부한다. 등록 전 대기 입력/프리뷰를 비우되 발급 번호·Core 결과·Global Runner는 초기화하지 않는다. Idle/실패 Job 배출이나 다음 Tick을 기다리지 않는다. 맵·상점·아티팩트 정리는 자기 Layer인지 확인한다. **기존 전투 `ReleaseInput`은 연결된 Layer 서비스의 현재 Layer가 기본값이 아니면 종류와 관계없이 지운다. 따라서 실제 공용 조립에서 다른 화면의 Layer 보존을 보장하지 않는다.** 오류 뒤에는 런타임 Tick이 정지하며, 이 검토에서 전투 정리 정책은 변경하지 않았다.

각 View의 일반 카메라/UI 구독 해제 책임은 이전하지 않았다. 공용 경계 콜백 하나의 수명만 그룹에 둔다. DisconnectStopInput은 다른 콜백이면 무시하므로 옛 Combat 호출이 공용 콜백을 실제로 해제하는 것은 아니다. 옛 해제 코드를 제거한 이유는 연결 대상과 소유권을 일치시키기 위해서다.

정상 반환 뒤 아티팩트 처리 번호 누락은 LobbyLifeCycle의 오류 로그로 해당 도메인만 정지한다. LobbyFlow는 정지한 도메인의 확인·등록을 건너뛰어 출발·이탈을 계속 허용한다. 출발 보드는 Core 현재 상태를 따르며 View가 따로 맞추거나 재제출하지 않는다. Core/Job 예외는 기존 공용 RuntimeFaultBoundary로 전체 정지한다.

## P3.9 실제 수정 범위 (당시 기록, P3.10 후속은 아래)

- 공용: InGameViewSystemFactory, InGameViewSystemGroup, 신규 InGameViewFaultCleanup.
- 전투 코드: `CombatViewSystem.Shutdown`의 경계 콜백 해제만 제거. 전투 계산·진입·연출 동작은 변경하지 않았다.
- 전투 테스트: `Assets/Tests/EditMode/Framework/CombatViewSystemTests.cs`도 수정했다. 테스트 Rig이 공용 `InGameViewFaultCleanup`으로 콜백을 연결하도록 바꾸고, `Shutdown_DetachesAndLeavesSharedCallbackToOwner`에서 전투 종료가 콜백을 해제하지 않으며 소유자 해제 뒤 재연결할 수 있음을 검증한다. 전투 작업자에게 이 파일도 변경 범위로 전달한다.
- 맵/상점: StageMapViewSystem·StageMapInteraction·ShopPopupController에 오류 정지/이전 수신자 거부·예외 중 나머지 정리·입력 Clear 추가.
- 아티팩트: ArtifactViewSystem.StopInput에서 기존 Session 정리와 입력 Buffer Clear를 독립 시도. ArtifactPopupSession·ArtifactPopupUI·Host/Screen 계약은 이번 단계에서 변경하지 않았다.
- Framework 기반·RuntimeFaultBoundary·UIBase·UISystem·UIInputBinding은 변경하지 않았다. P3.9 당시에는 **P3.10 미착수**로 새 OpenStateChanged 운영 구독·계약 제거가 없었다. 당시 후속 연결은 아래 P3.10 기록을 따른다.

## 검증 및 후속 전달

- 실제 Factory의 드래그/맵 입력·기존 상점 가짜 UI 대기 경로에서 Tick 없이 정리, 슬롯 반환·Ghost 제거·취소된 대기 Job·이전 수신자 요청 거부를 확인했다. 실제 Runtime 조립에서 Stage/Shop/Artifact 입력 목록·최신 프리뷰 Clear와 번호 유지도 확인했다.
- 복수 StopInput 실패와 자식 Shutdown 실패, 반복 종료, 정리 중 재진입 종료, 최종 콜백 해제, 최초 오류 보존을 검사했다. `RepeatedShutdown_PreservesForeignLayerAndFirstError`의 Layer 보존 검사는 **전투 View에 Layer 서비스를 연결하지 않은 아티팩트 Session 조건**이다. 실제 세 View의 공용 Layer 연결까지 검증한 것으로 확대 해석하지 않는다. 관련/전체 Unity·순차 MSBuild 증거는 SA 계획 P3.9 기록을 원본으로 본다.
- P3.9 당시 실제 S4V/U1 ShopUI·이벤트/회복 스테이지 View는 만들지 않았다. 이후 ShopUI 구현·연결 상태는 SA 계획 P5·P6 기록을 따른다. 향후 View가 추가되면 자기 StopInput 구현과 공용 조립 연결, 종료/fault 회귀를 함께 추가해야 한다.
- 전투 작업자에게 확인받을 내용: 변경 소유권/순서가 진행 중인 작업과 충돌하는지, 이후 Combat에서 공용 경계 콜백을 다시 등록/해제하지 않는지. **이 자료는 미전달이며 합의 완료로 표시하지 않는다.**

전투 종료 후 편집 잠금 해제와 Core 효과 에셋 계약 변경, 맵 요청 번호 전달은 별도 인수 사항이다. 공용 콜백 변경 승인이나 W1 오류 회귀 통과로 이 인수들을 완료 처리하지 않는다.

## P3.10 후속: 아티팩트 자체 UI 구독 (2026-10-11)

사용자 승인으로 아티팩트 Host/Screen을 제거하고 Session이 UI 소유 OpenStateChanged를 한 번 연결/해제하도록 바꿨다. 공용 RuntimeFaultBoundary 콜백 소유자·전투 정리·Factory/그룹/helper는 P3.9 그대로다. Session의 StopInput은 두 UI 버튼을 즉시 막고 자신의 수명 구독을 먼저 해제한 뒤 슬롯·드래그·프리뷰·대기 요청·자기 Layer를 정리한다. 카메라 구독/3D 자원은 자신의 Detach/Shutdown에서 해제·반환한다. 개별 자원을 공용 그룹으로 옮기지 않았다.

Session 알림 실패는 기존 경계에 보고하고 UI 전환을 재귀 호출하지 않는다. 부분 정리 실패에도 남은 정리를 시도하며 UI Hide finally는 Panel/문구를 정리한다. 실제 UI/UISystem/EventSystem과 Boot 회귀·실패 기준선 비교는 SA 계획 P3.10 기록을 따른다. P3.10 당시 P5/P6과 Core/전투 번호 구현은 진행하지 않았다. 이후 진행 상태는 SA 계획의 P5·P6 기록을 따르며 외부 작업자에게는 미전달·미합의다.

## 2026-10-11 검토 지적을 반영한 전달 사항

- 전투 작업자의 변경 목록에는 `CombatViewSystem.cs`와 `CombatViewSystemTests.cs`를 함께 포함한다.
- 정상 종료의 호출 순서는 `GameBootstrap.ShutdownOnce` → `RuntimeFaultBoundary.StopInputForShutdown` → helper의 세 `StopInput` → `FrameworkRuntime.Shutdown` → `InGameViewSystemGroup.Shutdown` → helper의 세 `StopInput` → 자식 모듈 역순 종료 → helper 연결 해제다. 앞 단계에서 예외가 나 런타임 순회가 중단되는 경우 모든 단계의 도달을 보장하지 않는다. Fault 뒤 정상 종료까지 실행하면 최초 fault의 입력 정리가 이보다 먼저 추가될 수 있다.
- Layer 보존은 개별 맵·상점·아티팩트 정리의 소유권 검사다. 전투가 같은 Layer 서비스에 연결된 실제 공용 경로의 보존 보장은 아니다. 전투 Layer 정책 변경은 별도로 협의할 항목으로 남긴다.
- 세 View의 정리 반복 허용과 위 호출 순서가 진행 중인 전투 작업과 충돌하는지 확인받는다. 이 기록은 정정·전달용이며 합의 완료가 아니다.
