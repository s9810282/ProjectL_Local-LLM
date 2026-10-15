# 옛 공격 지정 방식 필드 제거 — View·DB 작업자 공유 메모

**날짜·상태:** 2026-10-06. Core 작업(천명) 완료분 공유. **미전달.** 설계·실행 기록 원본은 [계획 문서](../plans/2026-10-06-legacy-attack-fields-removal-plan.md)다.

## 1 요약

유닛의 공격 모양이 두 곳에 기록돼 있던 것(옛 `appointMethod`·`attackTargetMode`와 새 `targetingMethod`)을 하나로 정리했다. 이제 공격 모양·선택·스플래시는 `targetingMethod`(지정 방식 테이블)만, 사거리는 `attackRange`만 가진다. 실제 공격 계산은 이미 새 지정 방식만 쓰고 있었으므로 **게임 동작은 바뀌지 않는다.** 바뀐 것은 데이터 입력 항목과 일부 생성자 시그니처다.

## 2 DB (`UnitDataSO`·`UnitData`)

| 변경 | 내용 |
|---|---|
| 인스펙터 | `UnitDataSO`의 Attack 항목에서 `appointMethod`·`attackTargetMode`가 사라졌다. `targetingMethod`·`attackRange`·Category·캐논 필드는 그대로 |
| 에셋 | 유닛 에셋 YAML에는 두 키가 남아 있다. Unity는 모르는 키를 무시하고 해당 에셋을 다음에 저장할 때 지운다(옛 `directionalAttackPattern` 잔여와 같은 처리). 에셋을 일부러 다시 저장할 필요는 없다 |
| `UnitData` 생성자 | `AppointMethod appointMethod`, `AttackTargetMode attackTargetMode` 인자 제거. 순서는 `…, moveCost, attackRange, category, …` |
| `UnitData.Validate` | 메인 캐릭터의 "옛 RadialSquare·사거리 1·SingleTarget" 검사 삭제(PlayerOnly·1×1·특성 없음은 유지). 메인의 공격 형태는 `DataBaseAssetTests.InitialUnit_TargetingMethodMatchesApprovedProfile`이 지정 방식 1번·사거리 1로 확인한다. 공격 검사는 Category–충격 결합(`AttackProfileCompatibility.RequireSupportedImpact`)만 한다 |
| enum 삭제 | `Contracts/Common/AppointMethod.cs`, `AttackTargetMode.cs` 삭제 |

**문서 갱신 요청(DB-SO 설계 담당):** [DB-SO 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md)의 공용 타입 표(`AppointMethod` 행)와 Unit 테이블 필드 표에 두 필드가 남아 있다. 설계 문서 소유자가 정리해 주면 된다.

## 3 Core 계약 변경 (View 코드 영향)

| 변경 | View 영향 |
|---|---|
| `UnitSpec` 생성자: `AttackPatternSpec attackPattern, AttackTargetMode attackTargetMode` → `int attackRange`. `AttackPattern`·`AppointMethod`·`AppointRange`·`AttackTargetMode` 속성 제거, `AttackRange` 유지 | `ViewSystem/Dev/DevBattleInitializationSource.Unit(…)`의 생성자 호출만 Core 쪽에서 최소 수정했다(`attackRange` 그대로 전달). 확인 부탁 |
| `AttackPatternSpec` 삭제 | 위 fixture 외 사용처 없음 |
| `UnitStatsSnapshot.AppointMethod` 제거(`AppointRange`·`AttackRange`는 유지) | View 실제 코드는 읽지 않는다. Buffer `CharacterCombatFactsComparer`의 비교 한 줄은 Core 쪽에서 삭제 |
| 가시밭 특성 + 칸 선택(Single)이 아닌 지정 방식 → `CombatAttackProfile` 생성 시 거부 | 데이터 실수를 조기에 드러내는 검사. 선인장 캐논(정사각형·칸 선택·정사각형 주변 스플래시)은 해당 없음 |

View가 공격 가능 칸·조준을 Core의 `AttackRangeResult`(`AttackAims`)로 받는 구조는 그대로다.

## 4 CLAUDE.md

DB 규칙 P4의 공용 타입 목록에서 `AppointMethod`·`AttackTargetMode`를 뺐고, 공격 모양·선택·스플래시는 지정 방식 테이블이, 사거리는 `UnitData.AttackRange`가 가진다고 적었다(2026-10-06).

## 5 확인

- Core·DB 테스트: Unity EditMode Phase 1~3 모두 통과(계획 문서 §7).
- 범위 밖으로 남긴 후보: Cannon 충격 필드(`Square`·`HorizontalRow`·`PierceRayCount`), 옛 개체 조준 `AttackAimKind.Entity`, 사용처 없는 `AttackFailureReason.NotSupportAppointMethod`(계획 문서 §3).
