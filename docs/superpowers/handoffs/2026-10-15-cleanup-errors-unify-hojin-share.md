# CleanupErrors 통합 공유 (호진님께, 2026-10-15)

- 작성: View 작업(용근), `ba0c9d7`(맵·상점 공용 팝업 이전)을 `feature/Yonggeun/Stage`에 병합하면서. **미전달.**

## 무엇이 있었나

병합 뒤 같은 이름의 정리 부품이 두 곳에 있었다.

| 위치 | 출처 | 멤버 |
|---|---|---|
| `RuntimeSystem/Common/CleanupErrors.cs` (`ProjectL.Systems.RuntimeSystem.Common`) | 전투 View 리팩토링 R1(2026-10-14) | `Try`, `ThrowIfAny`, `RunAll` |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/CleanupErrors.cs` (`…ViewSystem.Common`) | 공용 팝업 이전 | `Try`, `ThrowIfAny` |

공용 팝업·로비·상점 파일(`ArtifactPopupController`, `ArtifactPopupViewFactory`, `LobbyDeckPopupController`, `LobbyViewScope`, `ShopPopupController`, `StageMapViewScope`)이 두 네임스페이스를 모두 using해서 `CleanupErrors`가 모호해졌다(CS0104 108건).

## 어떻게 했나 (사용자 결정)

- `RuntimeSystem.Common.CleanupErrors` 하나만 남기고 `ViewSystem/Common/CleanupErrors.cs`(+ .meta)를 지웠다. 팝업 쪽 코드는 고치지 않았다. 위 파일들은 이미 `RuntimeSystem.Common`을 using하고 있어 그대로 이쪽으로 연결된다.
- 동작 차이 하나를 맞췄다. `ThrowIfAny`에 빈 목록을 넘기면 던지지 않는다(호진님 쪽 규칙과 테스트 `ThrowIfAny_NoErrorsDoesNotThrow`).
- `Try`의 null 정리는 이쪽 구현에서 `ArgumentNullException`으로 모인다(호진님 쪽은 NullReferenceException으로 모였다). 둘 다 실패로 모으고 다음 정리를 계속하는 점은 같다.
- `CleanupErrorsTests.cs`의 using만 `ProjectL.Systems.RuntimeSystem.Common`으로 바꿨다.

## 앞으로

정리 실패를 모으는 부품은 `ProjectL.Systems.RuntimeSystem.Common.CleanupErrors`를 쓴다. 여러 정리를 순서대로 돌리고 한 번 던지는 경우는 `RunAll(message, ...)`도 있다.
