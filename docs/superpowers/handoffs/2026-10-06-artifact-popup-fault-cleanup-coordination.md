# 아티팩트·전투 공용 오류 정리 협의안

작성일: 2026-10-06. 제안 문서이며 전달·합의 완료 기록이 아니다.
진행 원본: [아티팩트 팝업 계획 §9](../plans/2026-10-05-artifact-popup-plan.md).

## 현재 확인한 계약

- RuntimeFaultBoundary.ConnectStopInput은 런타임당 콜백 하나만 허용한다.
- InGameViewSystemFactory는 현재 combatView.StopInput을 연결한다.
- CombatViewSystem.Shutdown은 자기 StopInput 콜백을 Disconnect한다.
- GameBootstrap은 fault 이후 Runtime.Tick을 중단한다. 아티팩트 Tick에서 fault를 발견해 정리하는 것으로는 충분하지 않다.
- ArtifactViewSystem.StopInput은 자기 드래그·프리뷰·입력 Slot·대기 요청을 정리하고 현재 Layer가 Artifact일 때만 해제한다. 공용 오류 경계에는 아직 연결하지 않았다.

## 협의할 결정

권장안은 조립 계층이 단일 정리 콜백을 소유하고, 전투·아티팩트의 정리 메서드를 각각 호출하는 것이다.

1. 단일 콜백의 연결·해제 소유자는 누구인가? CombatViewSystem의 기존 자기 콜백 해제와 중복되지 않아야 한다.
2. 오류·정상 Shutdown에서 호출 순서는 어떻게 할 것인가? 정리 하나가 실패해도 나머지는 실행하고 오류를 함께 보고해야 한다.
3. StopInput 뒤 각 View Shutdown/Detach가 재호출돼도 구독·풀·Layer 정리가 안전해야 한다.
4. 다른 화면이 획득한 Layer를 이전 View가 덮어쓰지 않는 기존 계약을 유지한다.

조립 코드를 바꾸기 전에 전투 작업자와 위 소유권·종료 순서 및 테스트 범위를 합의한다. RuntimeFaultBoundary를 View마다 새로 만들거나 두 번째 ConnectStopInput으로 콜백을 덮어쓰지 않는다.

## 수정 범위 및 검증 후보

협의 후 변경 후보: InGameViewSystemFactory/그룹 조립의 정리 소유자, CombatViewSystem의 콜백 해제 책임. Core 계산·전투 상태·씬 전환 정책은 변경 대상이 아니다.

검증 후보: Artifact 드래그 중 오류와 Combat 연출 중 오류, 한쪽 정리 예외에도 다른 쪽 실행, Shutdown 반복, Layer 소유권, 입력 Slot 해제, Ghost/프리뷰 풀 반환, 원래 fault 보존.

## 합의 전 제한

아티팩트 입력·편집 API는 fault 상태에서 새 요청을 거부한다. 그러나 아무 입력 없이 공용 fault가 발생하면 임시 표현의 즉시 제거까지 보장하지 않는다. 이것을 완료된 자동 오류 정리로 보고하지 않는다. 전투 종료 후 편집 잠금 해제 및 Core 효과 에셋 계약 변경도 별도 담당자 협의 사항이다.
