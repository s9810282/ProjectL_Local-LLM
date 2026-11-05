# 전투 진입 요청에 "준비 완료" 질문 추가 — 용근님 공유

- 작성자: 호진(이호진)
- 날짜: 2026-10-27
- 작성: 호진(Claude 작업), 미전달
- 출처: 10-26 Lobby·StageSelect 원칙 검사 3번 "진입 요청 단계 직접 해석 → 이름 있는 질문"(View 쪽만). 사용자 "3번을 진행해줘."
- 상태: 코드 반영, 외부 C# 빌드 오류 0, 사용자 확인·커밋됨(`45b20161`, 10-27)

## 1. 바꾼 것 (`LifeCycles/Combat/CombatEntryRequest.cs`)

- 이름 있는 질문 하나를 더했습니다.

  ```csharp
  public bool IsPrepared => Status == CombatEntryStatus.Prepared && IsJobCompleted;
  ```

  "Core가 진입을 수락해 준비를 마쳤고 진입 Job도 끝났다"는 뜻입니다. `Prepared`는 `PublishResult`에서 수락된 결과로만 들어가므로 `Result`는 늘 수락입니다. 같은 꼴의 `IsActivating`(`Activating && IsJobCompleted`)과 나란히 둡니다.
- `TryCompleteMapPresentation`의 `Status == CombatEntryStatus.Prepared && IsJobCompleted`를 `IsPrepared`로 바꿨습니다(이 줄은 호진 줄). 뒤의 `Result != null && Result.Accepted` 검사는 그대로입니다.
- 동작은 같습니다. 단계 전이 표·`Require`·Job은 바꾸지 않았습니다.

## 2. 왜

클래스 머리말의 규칙("소비자는 단계 값을 해석하는 대신 이름 있는 질문을 읽습니다")에 맞추려는 것입니다. "준비 완료"에 맞는 질문이 없어서, 같은 식이 요청 안(`TryCompleteMapPresentation`)과 View 두 곳에 따로 적혀 있었습니다(SSOT).

## 3. 이 질문을 쓰게 바꾼 View (호진 줄)

| 위치 | 전 | 후 |
|---|---|---|
| `ViewSystem/StageMap/StageMapEntryObserver.cs:41` | `Status == Prepared && IsJobCompleted` | `IsPrepared` |
| 같은 파일 `:48` | `Status == Cancelled` | `IsCancelled` |
| 같은 파일 `:79` | `Status != Active && Status != Rejected` | `!IsActive && !IsRejected` |
| `ViewSystem/StageMap/StageMapNodeMovement.cs:59` | `Status != Prepared \|\| !IsJobCompleted \|\| !Result.Accepted` | `!IsPrepared` |

이제 View(`ViewSystem/`)에서 `CombatEntryStatus` 값을 직접 비교하는 곳은 0곳입니다.

## 4. 남아 있는 직접 비교 (바꾸지 않음, 참고)

- 용근님 영역: `LifeCycles/Combat/Jobs/CombatEntryTransitionJob.cs:38-41`의 `switch`. 요청을 움직이는 실행 쪽이라 그대로 둡니다.
- Stage 영역(재구성 보류 범위): `LifeCycles/Stage/StageLifeCycle.cs:169-192`, `StageNodeEntryFlow.cs:56-77`. 재구성을 다시 시작할 때 이 질문들로 바꿀 수 있습니다.
