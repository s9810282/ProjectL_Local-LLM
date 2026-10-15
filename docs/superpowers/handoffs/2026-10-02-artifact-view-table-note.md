# Artifact 외형 테이블 등록 정정 메모

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 Combat 담당)
- 대상: 호진님(Artifact DB 이전 담당)
- 기준: `feature/Yonggeun/Stage` HEAD `25d281a7` 이후 작업 트리
- 상태: **수정 반영(데이터만, 코드 변경 없음). 전달 미실시.**

## 1. 무엇이 문제였나

- `Assets/ProjectL/DataBaseSO/ViewGroup/Artifact/ArtifactViewDataTable.asset`에 외형 아이템 3개(1001 회복 성배, 1002 가시 갑옷, 1003 영혼 방울)가 등록돼 있었고, 세 개 모두 `icon`이 비어 있었다.
- `ArtifactViewTable` 생성자가 아이템마다 `ArtifactViewData.Validate()`를 부르고, 아이콘이 없으면 `[ArtifactViewData] 아이콘이 필요합니다. ID: 1001` 예외를 낸다. 그래서 **DB 생성(`DataBaseFactory.Create`) 자체가 실패**해 부트스트랩이 시작하지 못했다.
- 원래 의도는 등록 보류였다.
  - [Artifact DB 이전 인계](2026-09-28-artifact-db-migration-handoff.md): 1001~1003 모두 "표현 SO 생성(이름·ID 입력), 아이콘 미지정으로 테이블 등록 보류".
  - `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`: 외형 테이블이 비어 있어야 한다고 기대(주석 "원본 아이콘 부재: 보류 목록을 해소할 때 이 기대값도 함께 갱신").
  - 그런데 `8f38e2c1`(2026-09-28)에서 테이블에 세 아이템이 등록된 채 커밋됐다.
- 지금까지 드러나지 않은 이유: DB는 Render → InGame → View 순서로 만들어진다. 그동안은 InGame의 `CombatStageDetail` Economy 누락 예외가 먼저 나서 View 단계에 도달하지 않았다. View Phase A에서 Economy를 채우자 이 문제가 드러났다.

## 2. 반영한 것

- `ArtifactViewDataTable.asset`의 `databaseItems`를 비웠다(`[]`). 인계 문서·테스트가 말하는 "등록 보류" 상태로 되돌린 것이다.
- 외형 SO 에셋 3개(`ArtifactView_HealingChalice`·`ThornArmor`·`SoulDrop`)는 **삭제하지 않았다.** 이름·ID 입력은 그대로 남아 있다.
- 코드·테스트·InGame 쪽 Artifact 정의(1001~1003)는 바꾸지 않았다.

## 3. 아이콘이 준비되면

1. 각 `ArtifactView_*.asset`에 아이콘(Sprite)을 지정한다.
2. `ArtifactViewDataTable.asset`에 세 아이템을 다시 등록한다.
3. `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`의 "비어 있음" 기대를 실제 등록 목록으로 바꾼다.
4. `DataBaseAssetTests`·`ArtifactDataTableTests`를 실행한다.

## 4. 전달 기록

| 대상 | 전달 | 답변 |
|---|---|---|
| 호진님 | 미전달 | — |

전달은 사용자가 한다.
