# RenderSystemViewRefactoring 병합 확인

- 날짜: 2026-10-12
- 현재 브랜치: `feature/Yonggeun/Stage`, 병합 전 HEAD `b1cbc627`
- 대상: `origin/feature/JMH/RenderSystemViewRefactoring`, `b7c79107`
- 사용자 승인: 병합과 충돌 해결, 현재 포기 처리와 새 이동 경로 선 표시 모두 유지. 커밋·push는 사용자가 수행한다.
- 최신 원격을 fetch한 뒤 `--no-commit --no-ff`로 적용했다. 이 기록 시점에는 병합 커밋을 만들지 않았다.

## 충돌 해결

- `CombatViewSystem`: 현재 브랜치의 포기 요청 상태 → 전투 Plan 처리 구조를 유지하고 `IDrawPointLineSystemService` 전달을 추가했다. 대상 브랜치의 과거 `ICombatAbandonRequest` 직접 주입은 복원하지 않았다.
- `CombatDisplayReuseTests`, `CombatMovementViewTests`, `CombatViewSystemTests`: 현재 포기 계약을 유지하고 새 선 서비스 fake를 전달한다.
- `WallAttackInteractionTests`: 벽 강조 색·해제를 검사하는 기존 `outlines` 인스턴스를 그대로 전달하고 선 서비스 fake를 추가한다.
- FMOD Catalog·Editor Cache: 현재 브랜치 값을 병합 기준으로 유지했다. 병합 직전 생긴 로컬 변경은 `Temp/RenderSystemViewMerge/`에 복사한 뒤 그대로 복원했으며 스테이징하지 않았다. 두 파일 모두 백업과 복원본 SHA-256 일치를 확인했다.
- 대상 브랜치의 Outline → SelectOutline 이름 변경, PACK 에셋 이동, LineColor 등록, GridOutline 이동 경로 표시를 반영했다.

## 계약 대조

- SSOT: 포기 확인 상태는 `CombatAbandonController`, 처리 순서는 기존 전투 Plan이 소유한다. 이동 경로는 입력 순서를 유지하며 View에서 규칙을 다시 계산하지 않는다.
- SOLID: 전역 서비스는 `PostBindService`에서 받고 표시 계층에는 좁은 선 서비스 계약으로 전달한다. 기존 벽 강조 테스트의 관측 객체를 보존한다.
- FW-001/002: DrawPointLine 서비스와 수명 모듈 등록 유지. FW-010/011: 구체 메시 DTO와 공개 핸들 반환 경로 유지. FW-012: `_LineColor` MaterialTable 등록 유지.
- 게임 Core·LifeCycle 코드에는 병합 변경이 없다. Core 요청이나 별도 경계 변경을 추가하지 않았다.

## 검증

- `ProjectL.Tests.Framework.csproj`와 참조 시스템 어셈블리 MSBuild 성공, 오류 0. 변경하지 않은 Core/UI/테스트 파일에서 경고 14건이 출력됐다.
- Unity 생성 csproj는 편집하지 않았다. 신규·이동 파일은 `Temp/RenderSystemViewMerge/CompileOverrides.targets`로 컴파일 목록에 반영했다.
- 관리 코드 회귀 33건 통과: FieldHighlightState 7건, CombatPlanDecide 17건, CombatPanelController 9건. 실제 NUnit assertion을 실행했으며 **Unity Test Runner 실행 결과는 아니다.** 기록은 `Temp/RenderSystemViewMerge/managed-regression.log`.
- 선·SelectOutline·라인 리소스·신규 테스트 관련 `.meta` 10개의 GUID 고유성 확인.
- 병합 대상 YAML·meta에는 Unity 직렬화의 빈 값 뒤 공백이 있다. C#의 불필요한 공백은 정리했다.

## Unity에서 남은 확인

- EditMode: `CombatViewSystemTests`, `CombatDisplayReuseTests`, `CombatMovementViewTests`, `WallAttackInteractionTests`, `DrawPointLineSystemTests`, `FieldOutlineRendererTests`, `FrameworkRuntimeCompositionTests`, `DataBaseAssetTests`.
- GridOutline 이동 경로의 순서·꺾임·지우기와 포기 확인 → 남은 처리/표시 → 새 Run → StageSelect 동작.
- 현재 프로젝트가 Unity에서 열려 있어 별도 batchmode 테스트를 실행하지 않았다. 에디터 종료나 잠금 파일 삭제는 하지 않았다.
