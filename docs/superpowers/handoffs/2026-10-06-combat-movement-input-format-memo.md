# 이동 입력 파일 형식 정리 안내 (VR5, 2026-10-06)

- **받는 분:** 천명님(이동 개편 `87348b86` 원작자). **상태:** 메모 준비만 했다. 미전달. 발송은 사용자가 한다.
- **대상:** `ViewSystem/Combat/CombatPathInput.cs`, `ViewSystem/Combat/CombatMovementInteraction.cs`(+ `CombatViewSystem.cs`의 한 줄 여러 문장 몇 곳).

## 무엇을 바꿨나

프로젝트 코딩 규칙(Allman 중괄호, 한 줄에 한 문장, 멤버 사이 빈 줄)에 맞춰 **형식만** 바꿨다. 한 줄에 여러 문장, `{ a; b; }` 블록, 쉼표로 묶은 필드 선언(`private bool _a, _b;`)을 풀었다. `CombatMovementInteraction`에 한국어 클래스 요약 주석을 하나 더했다.

## 동작이 같다는 근거

정리 전후를 최적화 빌드(`-p:Optimize=true`)로 컴파일해 세 클래스(중첩·람다 포함)의 메서드 112개 IL 바이트 해시를 비교했고 **모두 같았다.** 관리 코드 회귀(`MovementDraftTests`·`CombatMovementFeedbackTests` 등 140건)도 통과했다.

## 이전 단계에서 바뀐 계약(참고)

- VR1: `CombatMovementInteraction`은 허용 규칙을 직접 계산하지 않는다(`PresentationAllowsInput` 삭제). `Begin`·`PointerMoved`·`OnPreview`·`Confirm`이 호출자가 그 호출에 평가한 `allowed`를 받는다. 허용 규칙의 원본은 `CombatInputPolicy.CanEditPath`다.
- 상세: [구조 개선 설계](../specs/2026-10-06-combat-view-refactor-design.md) VR1·VR5, View 계획 §7.41·§7.47.
