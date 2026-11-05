# 증강 선택 화면 연결 줄 — 민형님 공유 (2026-10-29, 호진, 미전달)

- 작성자: 호진(이호진)
- 설계: [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md)(옛 강화 설계 §14, 10-29 분리), 계획 [증강 계획](../plans/2026-10-28-trait-growth-plan-Hojin.md)(TG-b·TG-d, 옛 강화 계획 §5).
- 상태: Unity 재생 확인 문제 없음(호진, 10-29), 커밋됨(`24c21b3a3`). 기능·계약은 바꾸지 않고 증강 쪽을 잇는 줄만 더했습니다.

## 1 더한 연결 줄

| 파일 | 줄 | 왜 |
|---|---|---|
| `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | using 한 줄, `var traitGrowth = new TraitGrowthRequestLifetime(faults); traitGrowth.ExportServices(builder);`, 그룹 생성 끝 `, traitGrowth: traitGrowth` | 증강 선택 요청 한 건의 번호를 씬보다 오래 들고 실행 예약을 맡는 View 모듈(회복 `RecoveryRequestLifetime`과 같은 꼴) |
| `RuntimeSystem/InGameSystem/ViewSystem/InGameViewSystemGroup.cs` | 생성자 끝 선택 인자 `traitGrowth`, 노출 `TraitGrowthRequests`·`TraitGrowthExecution` | 화면은 제출 창구, Stage Plan·Job은 예약·종료 창구를 씁니다 |
| `ServiceKeys/Runtime/LifeCycle/LifeCycleSystemServiceKey.cs` | `StageTraitGrowthFacts` | "증강 선택이 남아 있는지" 읽기 사실(`StageLifeCycle`이 냄) |
| `RuntimeSystem/LifeCycleSystem/LifeCycleSystemGroup.cs` | 노출 `StageTraitGrowthFacts` | 전투 진입 시도 `StageEntryAttempts`와 같은 길로 View가 읽습니다 |
| `Factory/Runtime/InGame/View/StageMapSceneFactory.cs` | `new TraitGrowthPopupViewFactory(...)` 한 줄, 팝업 묶음 생성에 인자 하나 | 스테이지 맵 팝업 넷째(상점·회복·덱 편집과 같은 꼴) |

## 2 다른 세션이 바꾼 줄 (참고)

- 처음에는 `JobRootType.TraitGrowth = 7`과 `JobRegisterSystem`의 증강 Plan 등록 줄도 더했습니다. 그 뒤 Lobby/StageSelect 리팩토링(R-d2, 10-29)이 Stage 실행을 사실·Plan 꼴로 옮기면서 증강도 Stage `Next` 하나로 합치고 그 두 줄을 걷었습니다. 그 변경은 그쪽 민형님 공유(검사 후속 정리 계획 §6.11, `2026-10-28-stage-map-scene-factory-share-minhyeong-Hojin.md`)를 따릅니다.

## 3 커밋할 때

- `InGameViewSystemFactory.cs`에는 로비·스테이지 맵 리팩토링 줄과 증강 줄이 함께 있습니다. 어느 쪽 커밋에 넣을지 나눠야 합니다.
