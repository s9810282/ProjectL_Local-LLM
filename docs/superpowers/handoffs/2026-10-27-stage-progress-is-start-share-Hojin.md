# `StageProgressData.IsStart` 추가 — 민형·용근님 공유

- 작성자: 호진(이호진)
- 날짜: 2026-10-27
- 작성: 호진(Claude 작업), 미전달
- 출처: 10-26 Lobby·StageSelect 원칙 검사 6번 C-1, 사용자 결정 Q-C1 A. [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(옛 작은 정리 설계 §2), [계획](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) Phase C-1.
- 상태: 코드 반영, 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`42be34e6`, 10-27)

## 바뀐 것 (`BufferSystems/Stage/StageProgressData.cs`, 민형님 43줄·용근님 9줄 파일)

```csharp
/// <summary>맵의 시작 노드인지입니다. 시작 노드는 실제 스테이지가 없어 StageKey가 0입니다. 소비자는 StageKey를 0과 비교하지 않고 이것을 읽습니다.</summary>
public bool IsStart => StageKey == 0;
```

- 저장하지 않는 계산 읽기입니다. 필드·생성자·직렬화·Core가 Buffer에 쓰는 경로는 그대로입니다.
- `StageType`·`MapRole` 요약 주석의 "시작 노드(StageKey 0)"를 "시작 노드(`IsStart`)"로 맞췄습니다.

## 이 읽기를 쓰게 바꾼 곳 (모두 호진 줄)

`ViewSystem/StageMap/`의 `StageMapViewDisplay.cs:83·103`, `StageMapViewReader.cs:68`, `StageMapViewScope.cs:457`, `StageNodeView.cs:67·69·130`. `StageKey == 0` → `IsStart`, `StageKey != 0` → `!IsStart`. 동작은 같습니다.

## 그대로 둔 같은 판정 (참고)

- Core: `StageGeneratorSystem`·`ChapterProgressionCoordinator`·`StageCommandProcessingService`·`DevelopmentNavigationActions`의 `StageKey == 0` / `!= 0`.
- Stage(재구성 보류 범위): `LifeCycles/Stage/StageLifeCycle.cs:228`.
- 원하시면 같은 읽기로 바꿀 수 있습니다. `StageNodeVisualTable.cs:40`의 `StageKey == 0`은 "공통 외형"이라는 다른 뜻이라 대상이 아닙니다.
