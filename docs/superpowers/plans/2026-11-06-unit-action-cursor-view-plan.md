# 아군 행동 커서 View 계획 (2026-11-06)

## 설계

- 표시 원본은 Buffer의 `CombatStateData.CommandState.Turn`·`Phase`와 `CharacterData`다. 플레이어 입력 턴에 필드에 선 살아 있는 아군 중 Core의 `UnitActionUseRules`가 이동 또는 공격 시작을 허용하는 유닛에만 커서를 보인다. 이동 뒤 `Moved`에서는 공격 가능성이 남아 표시하고, `Attacked`·`Deferred`에서는 숨긴다. 공격 대상 유무나 현재 이동 경로 유무까지 판정하는 표시는 아니다.
- `CombatSceneDisplay`는 최종 사실을 갱신한 뒤 커서 표시를 요청한다. `FieldUnitPresenter`는 현재 대여 중인 유닛 View를 조회하는 좁은 함수와 프리팹 참조만 제공한다. 새 `CombatUnitActionCursorPresenter`가 표시 조건·인스턴스 재사용·해제를 소유한다. Core·입력·기존 HUD는 수정하지 않는다.
- 커서는 유닛 View의 자식으로 붙어 이동 연출을 따른다. 처음 생성할 때 렌더러 중심을 유닛 머리 위 보정 위치에 맞춘다. 프리팹의 현재 루트 위치가 원점이 아니어도 화면 위치를 맞출 수 있다. 커서 복제본의 Collider를 비활성화해 전투 포인터 판정에 개입하지 않게 한다.
- 풀 반환 때 커서를 즉시 숨긴다. 같은 프리팹 종류의 다른 진영 유닛이 View를 다시 빌려도 이전 아군의 커서가 잠시 보이지 않게 한다. 버려진 View의 커서는 Presenter에서 제거한다.
- 특성 미리보기 씬도 `FieldUnitPresenter`를 사용한다. 미리보기는 행동 커서를 요청하지 않으므로 프리팹이 없어도 기존 조립이 가능해야 한다. 운영 전투 Factory는 조립 초기에 프리팹 참조를 검사하고 누락 시 구성 오류로 거절한다. 커서는 전투 화면의 세부 월드 표시이며 별도 UISystem 화면은 아니다.
- SSOT: 행동 사용 상태와 턴은 Buffer, 시작 가능 규칙은 Core 계약, 위치는 대여된 유닛 View, 외형은 프리팹이다. 조립은 기존 `CombatSceneDisplay`/`FieldUnitPresenter` 경계에서 끝낸다. 독립적인 표시 기능이므로 기존 공격·이동 연출의 구현과 수명을 변경하지 않는다. SOLID: 새 Presenter가 표시 책임을, FieldUnitPresenter가 대여 조회 책임을 유지하며 Core 계약에 의존한다.

## Phase UC1 — 표시·수명 연결

1. `FieldUnitPresenter`에 Unit Cursor 프리팹과 머리 위 보정값을 직렬화하고 현재 유닛 View 조회 입구를 둔다. `Combat.unity`의 참조를 연결한다.
2. `CombatUnitActionCursorPresenter`가 최종 캐릭터/턴 사실을 받아 커서 표시를 갱신한다. 유닛 View별 복제본을 재사용하고, 숨김·씬 종료 때 끈다.
3. `CombatSceneDisplay.RefreshCore`에서 유닛 사실을 그린 뒤 표시를 갱신한다. Clear/Release 수명에 맞춰 정리한다.

Unity 확인: 플레이어 턴 시작 시 행동이 남은 필드 아군 모두 표시, 이동 뒤 유지, 공격 뒤 제거, 적 턴·배치·사망·퇴장 때 제거, 다음 플레이어 턴에 재표시. 이동 연출 중 위치 추적과 프리팹 Collider의 선택 방해 여부도 확인한다. View 시험 코드는 작성하지 않고 전투 View MSBuild는 실행하지 않는다. **컴파일·Unity 재생은 사용자 확인.**

## UC1 구현 기록 (2026-11-06)

- `Combat.unity`의 `FieldUnitPresenter`에 현재 `Unit Cursor.prefab` GUID를 연결했다. 원본 프리팹과 씬의 기존 비활성 샘플은 수정하지 않았다.
- `CombatUnitActionCursorPresenter`는 Buffer 캐릭터·턴 사실과 Core의 `UnitActionUseRules`로 표시 대상을 고른다. 복제본은 유닛 View 자식으로 재사용하며, Renderer 중심을 머리 위 보정값에 맞추고 Collider를 비활성화한다.
- 필드 유닛 풀 반환에서 커서를 즉시 숨기고 View 폐기에서 캐시를 제거한다. `CombatSceneDisplay.RefreshCore`의 최종 사실 갱신 후 호출하며 Clear/Release 수명에 붙였다.
- 특성 미리보기 씬은 기존처럼 프리팹이 없어도 조립된다. 운영 Factory만 프리팹 누락을 조립 초기에 검사한다. `git diff --check` 등 정적 확인만 진행했다. **컴파일 미확인, Unity 재생 미확인.**

## Phase UC2 — 이동 후 커서 색 (2026-11-06)

### 설계·Task

- 행동 표시 원본은 UC1과 같이 Buffer의 `CharacterData.ActionUse`다. `Available`은 프리팹 원색, `Moved`는 이동 후 색, `Attacked`는 커서 숨김을 유지한다. 색으로 실제 공격 대상 유무는 표현하지 않는다.
- 이동 후 색은 `FieldUnitPresenter`의 직렬화 값 하나로 설정한다. 현재 기본은 노란색 계열이다. 원색은 `UnitCursor.mat`에만 두어 중복 저장하지 않는다(SSOT). Core와 외형 머티리얼 에셋은 수정하지 않는다.
- `CombatUnitActionCursorPresenter`가 각 복제본의 Renderer와 원래 `MaterialPropertyBlock`을 한 번 보관한다. 상태가 바뀔 때만 복제본별 블록을 설정·복원한다. 공유 머티리얼을 복제하거나 변경하지 않으므로 다른 커서·유닛에는 영향이 없다.
- UC2 Task: (1) Inspector 이동 후 색 추가, (2) `Moved` 표시 색 전환과 `Available` 원색 복원, (3) 풀 재사용·다음 턴·공격 후 숨김 정적 점검. 기능 변경은 커서 Presenter와 그 조립 값에 한정한다. View 시험 코드와 전투 View MSBuild는 실행하지 않는다.

Unity 확인: 시작 턴 원색 → 이동 완료 후 이동 색 → 공격 후 숨김 → 다음 아군 턴 원색. 다른 아군 커서 색은 독립적으로 유지돼야 한다. `FieldUnits` Inspector에서 이동 후 색 변경이 적용되는지 확인한다.

### UC2 구현 기록

- `FieldUnits`에 `Unit Action Cursor Moved Color` 직렬화 필드를 추가했다. `Combat.unity` 기본값은 `(1, 0.75, 0.15, 1)`이고 기존 커서 높이 보정값 `(0, 2.5, 0)`은 유지했다.
- 커서 복제본의 각 Renderer가 원래 `MaterialPropertyBlock`과 이동 후 블록을 생성 시 한 번 준비한다. `Moved` 전환에서만 색 블록을 적용하고 다음 턴 `Available`에는 원래 블록을 복원한다. `Attacked` 숨김 규칙은 UC1 그대로다.
- 사용자 작업 중인 `UnitCursor.mat`과 프리팹은 수정하지 않았다. 현재 머티리얼의 `_BaseColor`·`_Color` 존재를 정적으로 확인하고 변경 파일의 `git diff --check`를 통과했다. **컴파일 미확인, Unity 재생 미확인.**

## Phase UC3 — 유닛 배율과 독립적인 커서 크기 (2026-11-06)

### 설계·Task

- 커서 외형 크기는 프리팹이 원본이다. 유닛 View의 `localScale`은 모델 외형 값이므로 커서 크기에 곱하지 않는다. 커서를 프리팹 월드 크기로 만든 뒤 유닛 Transform에 월드 변환을 보존하며 붙인다. 부모 회전은 기존처럼 따라가고, 렌더러 중심과 머리 위 오프셋 정렬도 그대로 쓴다.
- 변경은 `CombatUnitActionCursorPresenter.Create`의 생성·부모 연결에만 둔다. 행동 상태·색·풀 회수·Core/Framework 경계는 수정하지 않는다. 새 프레임 갱신이나 할당을 추가하지 않는다. 서로 다른 유닛 외형 배율에서도 같은 커서 프리팹의 화면상 크기가 같아야 한다.
- Unity 확인: 크기가 다른 아군 두 종류에서 커서 크기 비교, 이동 연출 중 위치·방향 추적, 이동 후 색과 공격 후 숨김, 풀 반환 후 재사용. View 시험 코드와 전투 View MSBuild는 실행하지 않는다. **컴파일·Unity 재생은 사용자 확인.**

### UC3 구현 기록

- 커서 프리팹을 먼저 독립 복제하고 유닛 회전을 적용한 뒤 `SetParent(anchor, true)`로 붙였다. 생성 시 프리팹의 월드 크기가 유닛별 기본 배율에 곱해지지 않는다. 렌더러 중심 정렬과 기존 표시 수명은 유지했다.
- `git diff --check`는 통과했다. **컴파일·Unity 재생 미확인.**

## Phase UC4 — 선택한 아군에게만 행동 커서 표시 (2026-11-07)

### 설계·Task

- UC1~UC3의 프리팹·머리 위 위치·유닛 View별 복제본 재사용·이동 후 색·행동 소진 숨김은 유지한다. 플레이어 전투 입력 턴에는 기존처럼 행동 가능한 아군의 복제본을 준비하되, 현재 정보 선택의 `SelectedActorId`와 일치하는 아군 커서만 켠다. 선택 없음·적/구조물 선택·행동 소진·배치·적 턴에는 모두 끈다.
- 선택 ID 원본은 `CombatSelectionState`다. `CombatSceneDisplay`는 마지막으로 표시한 캐릭터·턴·국면과 선택 ID를 `FieldUnitPresenter`에 전달한다. 결과 끝 갱신과 기존 `RefreshInspectionSelection` 선택 변경 콜백에서 같은 갱신을 호출한다. `CombatUnitActionCursorPresenter`는 대상별 표시 판단만 맡는다. Core·입력 선택 규칙·HUD·공격 범위 구현은 수정하지 않는다.
- SSOT·SOLID·성능: 행동 사용은 Buffer 캐릭터, 선택은 선택 상태, 외형은 기존 프리팹이 원본이다. 조립과 기능 구현은 분리하고, 선택 변경에서 기존 복제본을 다시 사용한다. 플레이어 턴 첫 표시의 준비 비용은 기존처럼 처리하며 선택마다 새 인스턴스를 만들지 않는다. 선택 갱신은 현재 캐릭터 목록을 한 번 순회하고 새 컬렉션을 만들지 않는다.
- Unity 확인: 미선택 상태에서 커서 없음 → 행동 가능한 아군 선택 시 그 유닛만 표시 → 다른 아군 선택 시 이전 것은 꺼지고 새 것만 표시 → 적/빈 칸 선택 시 없음. 이동 후 색, 공격 후 숨김, 적 턴·다음 아군 턴, 풀 반환과 다른 유닛 재사용도 확인한다. View 시험 코드와 전투 View MSBuild는 실행하지 않는다. **컴파일·Unity 재생은 사용자 확인.**

### UC4 구현 기록

- 선택 상태의 `SelectedActorId`를 결과 끝 갱신과 기존 정보 선택 변경 콜백에서 커서 Presenter로 전달한다. 행동 가능한 아군 복제본은 기존처럼 준비하고, 선택된 유닛의 것만 켠다. 선택 해제·적 선택 때 기존 복제본을 숨긴다.
- 호출자 검색과 `git diff --check`로 정적 확인했다. 프리팹·Core·입력 선택 정책은 변경하지 않았다. **컴파일·Unity 재생 미확인.**
