# 클릭 최단 경로 이동 — Core → View 전용 인계 (2026-11-17)

이 문서는 **2026-11-17 클릭 최단 경로 이동 Core 플랜 전용** 인계 문서다. 사용자 요청에 따라 9월 24일 통합 인계의 §5.94~§5.97·§74를 옮겼다. 이후 이 기능의 제공 상태와 View 인수 사항은 이 문서에서 갱신한다.

- 요구사항·정책 원본: [Core 설계](../specs/2026-11-17-combat-click-shortest-move-core-design.md)
- Core Task·검증 근거 원본: [Core Phase별 계획](../plans/2026-11-17-combat-click-shortest-move-core-plan.md)
- 현재 상태: **Core 코드 인계 가능 / 컴파일 미확인 / 신규 계약 실행 검증 미완료 / View Phase 1~4 코드 반영·사용자 Unity 우선 확인 3건 완료 / 외부 미전달**

View 구현·리뷰와 남은 Unity 확인은 [View Phase별 계획](../plans/2026-11-17-combat-click-shortest-move-view-plan.md)에 기록했다. 아래 Core Phase 기록은 인계 당시 상태다.

## 1. 범위와 제공 상태

사용자 요청은 기존 드래그와 클릭 이동을 동시에 지원하는 것이다. 클릭 흐름은 **목적지 첫 클릭 → 최단 경로·도착 위치 미리보기 → 같은 목적지 두 번째 클릭 → 이동**이다. 사용자 요구와 승인된 세부 정책, 구현 경계의 원본은 [클릭 최단 경로 이동 Core 설계](../specs/2026-11-17-combat-click-shortest-move-core-design.md)다. 과거 10-27 초안의 미결정 사항을 이번 작업의 승인으로 해석하지 않는다.

- **Core 제공 요청:** 기존 이동 범위 BFS에서 부모를 기록해 목적지별 `MovementPath`를 제공한다. `MovementRangeResult`에서 경로를 조회하고 출처 `QueryRevision`을 대조한다. 세부 API·최단성·동률·도약/자리 교환 판정은 설계 §4를 따른다.
- **실행 경계:** 기존 Move 미리보기·`MovementPreviewContext`·Move 명령을 재사용한다. 클릭 횟수와 확정 대기는 View가 소유하며, 첫 클릭의 견적 응답만으로 이동을 제출하지 않는다. Core 제공만 끝나도 클릭 입력이 완성되는 것은 아니다.
- **후속 인수:** View의 두 번 클릭 상태·선택/드래그/환경 클릭 우선순위는 설계 §5, 담당 범위·리뷰 지점은 §6, SSOT·SOLID 5원칙·JMH/FW·기능 독립성 검토는 §7, 확인 항목은 §8에 있다.
- **Core 진행 상태.** [Phase별 계획](../plans/2026-11-17-combat-click-shortest-move-core-plan.md)의 공통 탐색, 경로 응답, 출처 부착과 게시 검증을 구현했다(이 문서 §4.1~§4.3). Phase 4 직접·독립 정적 리뷰에서 구현 결함은 발견하지 못했다(이 문서 §4.4, 계획 §9.4). **Core 코드 인계 가능**이며 실행 검증 완료를 뜻하지 않는다. 사용자 자료 `feature-change-locality-guidelines.md`의 수정·이해·검증 범위 기준도 실제 diff와 대조했다.
- **사용자 확인 항목.** 계획 §7 K1~K7의 실제 관찰과 §7.2의 Core 5개·CoreIntegration 3개 기존 시험 클래스 실행 결과가 남아 있다. 이번 Phase에서 Unity/컴파일/시험은 실행하지 않았고 사용자 확인 결과도 전달받지 않았다. 계산 도중 요청·리비전·턴 불일치는 정적 검토만 했으며 실행 재현으로 보고하지 않는다. **컴파일 미확인. 신규 계약 실행 검증 미완료.**

## 2. 제공 API와 기존 실행 연결

- `MovementRangeResult.Success(EntityId, IReadOnlyList<MovementPath>)`가 경로를 소유하고 `Destinations`를 파생한다. 기존 목적지의 `StepDistance == route.StepCount`, `TargetAnchor == route.Endpoint`이며 목적지 순서는 Y/X다.
- `TryGetRoute(CellCoord, out MovementPath)`는 원점부터 목적지까지의 불변 경로를 제공한다. 실패·빈 성공·default·범위 밖은 false/out null이다. 경로는 상하좌우 최소 걸음이며 동률은 Right/Up/Left/Down 최초 발견이다. Core는 자리 교환 가능한 종점도 제공하므로 View의 빈 칸 클릭 정책과 구분한다.
- `SourceRevision`은 순수 계산 결과에서 0이고, 게시 어댑터의 `WithSourceRevision(long)`으로 최초 캡처한 양수 revision이 붙는다. 실패·빈 성공도 정식 게시 응답이다. View는 이 값을 직접 찍지 않는다.
- 실행 순서는 `MoveRange → TryGetRoute → CombatViewPreviewData.Move(actor, path) → MovePreview/MovementPreviewContext → CombatViewCommandData.Move(actor, path, context)`다. 범위 경로는 실행 승인이나 실제 도착 보장이 아니며, 기존 견적과 문맥 검증·진입 효과·중단·Undo를 따른다. 클릭 전용 Core 명령/API는 없다.


## 3. 후속 View 구현에서 지킬 조건

아래는 제공 코드의 소비 조건이며 **View 구현 완료 기록이 아니다.** 세부 정책의 원본은 설계 §5다.

1. 범위 수락 시 `SourceRevision > 0 && SourceRevision == current.QueryRevision`과 요청 번호·세대·전투·행위자·원점을 함께 대조한다. 견적도 현재 후보의 전체 경로·문맥·요청 소유와 일치해야 한다. View는 기존 Buffer로 사실을 받고 InGame DB나 Core 런타임 상태를 직접 조회하지 않는다.
2. 첫 클릭은 경로 견적만 요청한다. 현재 `CombatMovementInteraction.Confirm`은 `_frozen`을 세우고 유효 `OnPreview`에서 `SubmitFrozen`을 호출하므로 **첫 클릭을 이 Confirm에 연결하면 안 된다.** 유효 미리보기를 표시한 뒤 시작한 동일 목적지 두 번째 클릭에서만 기존 제출 입구로 Move 1개를 보낸다.
3. 견적 대기 중 동일 목적지 클릭은 확정 예약을 만들지 않는다. 응답이 누름과 뗌 사이에 와도 그 입력을 확정으로 승격하지 않는다. 다른 합법 빈 목적지는 후보·견적을 교체한다. 다른 유닛은 선택하며 자리 교환 UX는 기존 드래그로 유지한다.
4. 현재 선택의 범위가 준비되지 않았으면 가용성/슬롯 대기를 포함해 필드 안 빈 칸 누름을 소비하고 선택을 유지한다. 목적지를 예약하지 않고 환경/토네이도 클릭에도 중복 전달하지 않는다. 응답 후 새 클릭이 필요하다. 확정된 실패/빈 범위는 대기가 아니다.
5. revision 변경 시 후보·견적·확정 자격 전체를 취소하고 범위만 다시 조회한다. 이전 목적지 자동 복원·재견적·제출은 없다. 변경을 감지한 누름도 새 후보/확정으로 재사용하지 않는다. 턴·모드·전투 변경과 결과 재생의 취소 계약도 유지한다.
6. 단일 Preview 슬롯의 소유권을 유지하고 취소 시 자신의 요청 번호만 비운다. 선택 처리 전에 누름의 소유자를 정하며 원점 드래그와 두 번 클릭을 동시에 지원한다. 제출 후 반복 입력은 기존 명령 대기 게이트로 막는다. 클릭/선택/취소 인수는 View 후속 계획·구현 후 Unity에서 별도로 확인한다.

## 4. Core Phase별 인계 이력

### 4.1 클릭 최단 경로 이동 Phase 1 공통 탐색 분리 (2026-11-17)

- 사용자가 Core 계획의 첫 Phase 진행을 요청했다. Core.Application에 `MovementRangeSearch.cs`와 `.meta`를 추가하고 `CombatMovementCalculator.Range`와 `UnitMoveCells`를 새 탐색에 연결했다.
- 기존 `CanTraverse`·`CanLand`로 거리와 첫 발견 부모를 기록한다. 통과 가능 칸만 큐에서 확장한다. 기존 범위 목적지 생산, 호버의 별도 조회 자격과 지형 시점, 두 출력의 Y/X 정렬은 기존 코드와 대조했다. 경로 복원 API의 운영 연결은 Phase 2다.
- 호출처·공백·GUID와 순수 C# 경계를 정적으로 확인했다. **컴파일 미확인.** §1~§3의 확인 항목과 View 제공 상태를 참고한다.

### 4.2 클릭 최단 경로 이동 Phase 2 경로 응답 연결 (2026-11-17)

- 사용자 요청에 따라 `MovementRangeResult`의 성공 생성 계약과 유일한 생산자인 `CombatMovementCalculator.Range`를 함께 이전했다. 최단 경로를 목록으로 보관하고 종점·걸음 수·점유 칸 정보를 그 경로에서 파생한다. 기존 공통 `MovementDestination`과 호버의 이동 칸 조회는 기존 계약을 사용한다.
- `TryGetRoute(CellCoord, out MovementPath)`, `SourceRevision`, `WithSourceRevision(long)`을 제공한다. 출처 0은 게시 출처 미부착이며 실제 양수 출처의 부착은 Phase 3에서 연결한다.
- 생산 호출, 경로 목록 소유, 빈 성공·실패·default 처리를 정적으로 대조했다. **컴파일 미확인.** §1~§3에 사용자 Unity 확인 항목을 적었다.

### 4.3 클릭 최단 경로 이동 Phase 3 출처 검증과 게시 (2026-11-17)

- 사용자 요청에 따라 `CombatPreviewService`에서 입력 조립 전에 읽은 `QueryRevision`을 `MoveRange` 성공·빈 성공·실패 응답에 부착했다. 게시 직전에 요청 번호와 전투 인스턴스에 더해 리비전과 진영 턴이 맞는지 확인하고, 불일치 응답은 게시 전에 반환한다.
- 정상 응답은 기존 Battle 쓰기 Sink를 통해 게시되고 `CombatPreviewJob`은 같은 실행에서 응답 번호로 완료를 판단한다. 불일치 분기는 응답 번호를 갱신하지 않는다. **컴파일 미확인.** §1~§3에 Unity 확인 항목을 적었다.

### 4.4 클릭 최단 경로 이동 Phase 4 정적 검토와 View 인계 (2026-11-17)

- 사용자 요청으로 Phase 1~3 작업 트리의 검색·경로 응답·출처 게시 변경을 직접 검토하고 별도 읽기 전용 리뷰를 수행했다. 구현 결함은 발견하지 못했다. SSOT·SOLID 5원칙·JMH/FW·첨부 기능 변경 독립성 지침도 실제 diff와 대조했다.
- 이번 Phase는 계획과 이 인계 문서만 갱신했다. Core/View 코드·시험 변경은 없다. 기존 Move 견적·문맥 검증·실행·Undo 경계를 재사용하는 점과 첫 클릭에서 `Confirm`을 호출하면 안 되는 이유를 §1~§3에 정리했다.
- **Core 코드 인계 가능. 컴파일 미확인. 신규 계약 실행 검증 미완료.** K1~K7의 정적 근거·미실행 항목과 기존 회귀 확인 대상은 [계획 §9.4](../plans/2026-11-17-combat-click-shortest-move-core-plan.md#94-phase-4-정적-검토와-view-인계--2026-11-17)에 있다. 사용자 Unity 실행 증거를 기다리며 View 구현으로 넘어가지 않는다.

## 5. 남은 검증과 다음 작업

Core의 K1~K7 신규 계약 관찰과 기존 8개 시험 클래스 확인은 [계획 §7](../plans/2026-11-17-combat-click-shortest-move-core-plan.md#7-검증-계획과-증거)을 따른다. 정적 검토 근거와 실행 미확인 구분은 같은 계획 §9.4에 있다. 이번 문서 분리는 검증 상태를 변경하지 않는다.

후속 View 작업자는 설계 §5와 이 문서 §2~§3을 기준으로 별도 Task·Phase 계획을 작성한다. 드래그와 두 번 클릭의 공존, 입력/슬롯 소유권, 후보 취소와 Unity 인수 항목을 포함한다. View 코드·시험·컴파일에 관한 저장소 작업 제한을 그대로 따른다. 문서 분리 요청은 View 구현 시작 승인이 아니다.
