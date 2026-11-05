# 훈련소 View 연결 — Core 공유 (천명님께)

- 작성자: 호진(이호진)
- 작성: 2026-11-02, 호진(이호진) — 본문 초안은 Claude
- 상태: 코드 완료(사용자 지시, 컴파일·재생 사용자 확인 대기, 커밋 전, 미전달). 기록은 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §5.78~§5.80.
- 설계: [훈련소 View 연결 설계](../specs/2026-11-02-stage-training-view-design-Hojin.md)
- 파일 이름에 three-offers가 남은 것은 초기 안의 흔적입니다. 세 장 뽑기는 넣지 않았습니다.

## 1 요지

인계 문서대로 연결했습니다. **유닛을 먼저 고르고, 그 유닛의 `TrainingChoices`를 전부 보여 줍니다.** 넘치면 스크롤입니다. 규칙·명령·결과 계약은 그대로이고, Core·Buffer에는 표시용 값만 더했습니다.

## 2 더한 것

| 파일 | 변경 | 이유 |
|---|---|---|
| `TraitGrowthDefinition` | `ValuesAt(rank)`, `ResolveRank`가 이것을 부름(동작 같음) | 등급 값 읽기를 한 곳으로 |
| `TrainingChoiceSnapshot` | `CurrentValues`·`NextValues`(끝 선택 인자) | 카드의 `보호막 10 → 19 (+9)`. 인계 문서 `:72`가 요청하라고 한 값입니다 |
| `TrainingChoiceSnapshot` | `Name`·`Description`(끝 선택 인자) | 증강과 같은 Core 글을 쓰려고. View 외형 글은 숫자가 달랐습니다(아래 §5) |
| `TrainingRules.Choices` | 위 값을 채움 | |
| `CurrentShopData` | `TrainingPurchase`(끝 선택 인자). `WithTrainingPurchase`가 지금 목록에서 찾아 넣고 없으면 첫 쓰기 전에 예외 | 구매 뒤 목록에서 산 쌍은 등급이 오르거나 빠져, 산 단계를 다시 그릴 수 없습니다 |

증강 쪽: `IStageTraitGrowthFacts.IsGrowthStopped`를 더해, 증강 도메인이 멈추면 화면이 오류 경계로 보고합니다. 나갈 길 없는 화면에 조용히 갇히지 않게 하려고입니다.

## 3 시험

천명님 시험은 고치지 않았습니다. 코드 대조로는 영향이 없다고 보지만 **실행 미확인**입니다. 사용자가 Unity에서 확인합니다. View 작업 규칙상 새 시험은 쓰지 않았으니, 필요하면 더해 주세요.

**별개로 이미 실패하는 시험 셋이 있습니다.** `RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases`와 `ProductionRestVisit_EntersSelectsAndCompletes` 두 사례입니다. 시험이 회복 선택지 `2, 4`와 용병 후보 ShotGun 하나를 기대하는데, 지금 에셋이 회복 `1, 2, 4`(호진 `f672bb337`)와 용병 넷입니다. 이번 변경 때문이 아니라고 보고, 호진 결정으로 시험과 에셋을 고치지 않았습니다.

## 4 데이터 (임시 값)

- `Enhancement_Default`: 가격 300·500.
- `StageRootDataTable`: `Stage_Enhancement`(1202) 등록.
- `StageMap_Chapter1`: `depthCount` 6, `enhancementDistribution` 가중치 7·최소 1. `StageMapData.Validate`의 최소 개수 합 규칙 때문에 깊이를 늘렸습니다.

## 5 알려 드릴 것

- **설명 글의 숫자:** View `TraitVisual_*` 글이 실제 값과 다릅니다(FinishingShot 20% / 15, MoraleBoost 20% / 10, Hunter 1 / 3, FocusedBombardment 50% / 70). 훈련 카드는 이제 맞는 Core 글을 읽습니다. 다만 Core 글도 1등급 숫자라 2등급 카드에서는 수치 줄과 다릅니다.
- **Unit_ShotGun의 특성 네 개**는 전투 작업 시험 값으로 보입니다. 되돌리면 시작 유닛 중 훈련 대상이 없어 첫 층 훈련소는 모두 잠깁니다(정상 방문).

## 6 확인 부탁

1. 모든 유닛의 훈련 특성이 3개 이하라는 전제가 맞나요? 넘어도 스크롤로 보여 멈추지는 않습니다.
2. `TrainingChoices`가 구매 때 다시 계산되고 `TrainingPurchase`를 더한 꼴이 괜찮나요?
3. 등급 수치를 영수증이 아니라 선택지에 둔 것이 괜찮나요?
