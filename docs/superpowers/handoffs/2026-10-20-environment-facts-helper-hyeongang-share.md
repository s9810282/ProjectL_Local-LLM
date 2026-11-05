# 환경 사실 공용 도우미·호버 위험 범위 색 — 현강님 공유 (2026-10-20, 미전달)

작성: 용근 작업(Claude). 근거: [호버 공격 범위 설계 §8](../specs/2026-10-19-combat-unit-hover-attack-range-design.md), [위험 범위 계획](../plans/2026-10-20-combat-hazard-hover-range-plan.md) P4·T5·T8.

## 1. 무엇이 바뀌었나

전투에서 유닛이 없는 칸에 커서를 올리면, 그 칸에 플레이어가 아는 지뢰나 살아 있는 화약통이 있을 때 피해 칸이 보이게 했습니다. 이 일을 하면서 현강님 코드 `CombatEnvironmentText`(환경 설명 Hover)의 판단 두 개를 공용 도우미로 옮겼습니다. **문구·동작은 같습니다.**

| 파일 | 바뀐 것 |
|---|---|
| `ViewSystem/Combat/CombatEnvironmentText.cs` | private `IsKnownToPlayer`를 지우고 `CombatEnvironmentFacts.IsKnownToPlayer`를 부릅니다. 장애물 분류·프로필을 찾던 반복 두 개를 `CombatEnvironmentFacts.TryGetObstacle` 한 줄로 바꿨습니다(분류 밖·프로필 없음이면 건너뜀은 그대로). |
| `ViewSystem/Combat/Display/ObjectViewSet.cs`·`TerrainViewSet.cs` | 같은 private `IsKnownToPlayer` 사본을 지우고 공용 도우미를 부릅니다. |
| 새 `ViewSystem/Combat/Display/CombatEnvironmentFacts.cs` | "플레이어에게 알려진 원본인가"·"구조물의 장애물 프로필" 판단 한 곳. 호버가 포인터마다 부를 수 있어 열거자를 만들지 않는 반복으로 썼습니다. |

같은 규칙이 세 파일에 따로 있던 것을 하나로 모은 것입니다(SSOT). 새 위험 범위 찾기(`CombatHazardLookup`)도 같은 도우미를 씁니다.

## 2. 씬 값 하나

`FieldView` 인스펙터 "입력·미리보기 층 색" 묶음에 **Hazard Range Color**(기본 보라 `0.75, 0.45, 1`)가 생겼습니다. `Combat.unity`를 다음에 저장하면 이 칸이 씬에 들어갑니다. 색을 바꾸고 싶으면 여기서 조정하면 됩니다.

## 3. 확인 부탁

- 환경 설명 Hover 문구가 지금과 같은지(지형·오브젝트·장애물). 회귀 시험은 `CombatEnvironmentHoverTests`·`ObjectViewSetTests`·`TerrainViewSetTests`(무수정)입니다.
- 앞으로 환경 설명에 "알려진 원본" 조건을 바꿀 일이 생기면 `CombatEnvironmentFacts` 한 곳만 고치면 오브젝트·지형 표시와 위험 범위가 함께 따라갑니다.

## 4. 2026-10-20 후속 — 고지대 등 바닥 지형 호버 설명 (미전달)

- 고지대·약한 지반·낭떠러지 외형을 운영 `TerrainVisualDataTable`에 등록했습니다([고지대 View 계획](../plans/2026-10-20-combat-high-ground-view-plan.md) HG-0). 등록 전에는 공개된 이 세 지형 칸에 호버하면 `CombatEnvironmentText.Build`의 `GetTerrainVisual`이 `KeyNotFoundException`을 냈습니다(배치가 없어 드러나지 않았음). `CombatEnvironmentText`는 고치지 않았습니다.
- 이제 그 칸들에서 외형 SO의 이름·설명이 나옵니다. 고지대 설명의 "저지대의 대상을 공격할 때 벽을 무시합니다" 줄은 Core에 아직 없는 규칙이라 실제 동작(벽에 막힘)과 다릅니다. 문구는 그대로 둡니다(사용자 결정).
- `TerrainViewSetTests`의 바닥 지형 시험 하나를 새 규칙(프리팹 유무로 그림)에 맞춰 고쳤습니다.
