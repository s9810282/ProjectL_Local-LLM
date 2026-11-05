# CombatSceneDisplay SD-C0 실습 워크시트

남은 직접 구현의 책임 경계를 읽고 추가 분리의 이득을 판단한다. 현재 방향은 [설계 §5.3](../specs/2026-10-14-combat-scene-display-followup-design.md), 최초 평가 이력은 [후속 계획 §7.7, 새 기준 재평가는 §7.8](../plans/2026-10-14-combat-scene-display-followup-plan.md)이 원본이다. 최초의 현 구조 유지 결론은 조립·기능 구현 분리 권고로 바뀌었다.

## 1. 상태의 주인 확인

1. Display에서 `_isReleased`와 `_blockedWallCells`를 찾는다. 수명 상태와 매 호출마다 다시 채우는 임시 목록을 구분한다.
2. `FieldUnitPresenter`의 비용·체력·숨김 대상, `CombatPathInput`의 초안 캐시, `CombatMovementPlanningPresenter`의 편집·범위 수명을 확인한다. Display가 이 상태를 따로 복제하는지 대조한다.
3. `Refresh`·`RenderActiveActor`·`ShowCompletion`은 전달받은 사실을 화면 표시로 바꾼다. 단순 위임과 표시 변환을 구분하되 Core 규칙 재계산이 있는지 살핀다.

## 2. 이동 표시 이전 비교

1. `ShowDraft`·`ClearDraft`가 함께 바꾸는 대상은 필드 층·호버·이동 HUD·공용 비용 슬롯이다.
2. 이 둘만 기존 이동 Presenter로 옮기면 HUD 의존이 추가된다. `UpdateMovementPointer`까지 옮기면 화면 좌표 변환 의존도 추가된다. 현재 Presenter가 월드 좌표만 받아도 되는 이유를 확인한다.
3. `CombatPathInput`의 `EndMovementPlanning`·`ClearDraft`와 Display `_fieldBound`의 호출 시점을 비교한다. 이동 Presenter의 Clear에 초안 정리를 합치면 어떤 때 공유 표시가 추가로 지워지는지 설명할 수 있어야 한다.

## 3. 조립과 기능 변경 범위 확인

1. 생성자의 `new`, Configure, 콜백 연결과 Install의 Build·Mapper 연결을 구별한다. 앞은 조립 지점으로, 뒤의 공통 순서는 런타임 수명 조정에 남기는 이유를 설명한다.
2. 이동 끝점 색 하나를 바꾸는 경우와 이동에 새 표시 의존성을 추가하는 경우를 나눈다. 각각 수정할 파일·읽어야 할 구현·검증할 행동을 설계 §5.3의 표와 대조한다.
3. 배치 Ghost/원본 숨김, 공격 영향/벽 표시를 해당 기능에 모았을 때 공유 비용 슬롯·Ghost 풀·Range 층의 주인이 바뀌는지 확인한다. 새로운 상태 사본이 필요한 설계는 피한다.
4. `CombatPointerMappingTests.SetUp`의 전체 씬 준비와 `CombatMovementPlanningDisplayTests`·`CombatMovementRangeDisplayTests`의 기능별 준비를 비교한다. 기능 표현 테스트와 씬 조립·공통 수명 회귀가 각각 보장하는 것을 구분한다.

## 4. 현재 방향과 남은 검증

- 기존 상태 소유권 판단은 유지하되 수정·이해·검증 범위를 함께 평가한다. 새 상태가 없어도 이동 초안·배치·공격의 직접 구현을 분리할 실익이 있다.
- 조립만 옮기거나 모든 입력 피드백을 새 큰 클래스 하나에 옮기는 것으로 완료 판정하지 않는다. UI·Mapper·사건의 기존 구현 분리는 유지한다.
- 상세 Task·Phase는 [조립·기능 분리 계획](../plans/2026-10-14-combat-scene-display-composition-plan.md)의 SD-D0~D5를 따른다. 구조 평가·계획 완료와 Unity 회귀 통과를 구분하며 SD-A/B/C′ 검증은 각 워크시트대로 남아 있다.
