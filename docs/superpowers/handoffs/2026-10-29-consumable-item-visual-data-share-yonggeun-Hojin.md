# 소모 아이템 외형 SO를 DB 규칙 P2로 — 용근님 공유

- 작성자: 호진(이호진)
- 상태: 미전달. 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`24c21b3a3`, 10-29).
- 출처: [검사 후속 정리 계획 §6.8](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#68-phase-r-e--묶음-4-db-so-q-r10q-r10bq-r11-tp-r11tp-r13) R-e DM1, TP-R12(사용자 확인).
- 대상: 용근님 파일 `DataBaseSystem/Data/View/Table/ConsumableItem/Item/ConsumableItemVisualDataSO.cs`(전투 가방·유닛 HUD 아이콘 표, `1c229b7a`).

## 바뀐 것

- DB 규칙 P2("SO는 입력 계층이고 `GetData()`는 부를 때마다 새 읽기 전용 Data를 만든다")에 맞췄습니다. 같은 Phase에서 View 표 SO 다섯(회복 외형·아티팩트 외형·아티팩트 보드 설정·노드 외형·맵 표시 설정)도 같은 꼴로 바꿨습니다.
- `ConsumableItemVisualData`는 새 파일 `ConsumableItem/Item/ConsumableItemVisualData.cs`로 나왔고, 생성자에서만 값을 받는 읽기 전용 값입니다. 공개 속성(`ItemKey`·`DisplayName`·`Icon`·`Description`)과 `Validate()`, 이름이 비면 아이템 키를 쓰는 규칙은 그대로입니다.
- SO는 안쪽 입력 칸 `Input`(`[SerializeField] private` 칸 넷 + `ToData()`)을 갖습니다. 바깥 칸 이름 `consumableItemVisualData`와 안쪽 칸 이름이 옛 것과 같아 **에셋 값은 그대로 읽힙니다**(에셋 변경 없음). 꼴은 `EnemyLoadoutDataSO`·`CategoryConfigDataSO`의 안쪽 입력과 같습니다.

## 영향

- 표를 읽는 쪽(가방·HUD)은 바뀐 줄이 없습니다. 표는 DB 생성 때 한 번 `GetData()`를 부릅니다.
- 화면 차이는 없습니다. 바뀐 형식을 쓰는 시험도 없습니다.
- 앞으로 칸을 더할 때는 `Input`에 칸을 더하고 `ToData()`와 Data 생성자에 함께 넘기면 됩니다.

## 병합 때 맞춘 것 (10-29, 컴파일 미확인)

- `origin/feature/JMH/Effect` 병합에서 용근님 `efa5e7d7`(이펙트·사운드 창구)이 옛 Data 클래스에 더한 칸 둘(`itemSoundPreset`·`activationCues`)과 이 P2 분리가 충돌했습니다.
- 두 칸은 `Input`에 같은 이름·툴팁으로 옮기고, `ToData()`가 `activationCues.ToCues()`를 한 번 만들어 Data 생성자에 넘깁니다(`ObjectVisualDataSO`와 같은 꼴). Data 속성 `ItemSoundPreset`·`ActivationCues`와 `Validate()`의 소리 프리셋·큐 칸 규칙 검사는 용근님 판 그대로입니다.
- 다른 점 하나: 옛 판은 `ActivationCues`를 읽을 때마다 새로 만들었고, 지금은 조회마다 새로 만드는 Data가 만들 때 한 번 갖습니다. 읽는 쪽(`CombatCueSources`·`CombatSoundPresets`)은 바뀐 줄이 없습니다.
