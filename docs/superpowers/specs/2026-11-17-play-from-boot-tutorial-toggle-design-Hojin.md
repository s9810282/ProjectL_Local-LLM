# Play From Boot 튜토리얼 사용 설정 — 2026-11-17

## 승인·목표

사용자가 2026-11-17 채팅에서 PC별 `튜토리얼 사용` 토글, 기본 켜짐, 다음 재생부터 적용하는 방식을 승인했다. 이번 변경은 Codex의 C# 직접 작성 예외를 승인받았다.

## 동작

- `ProjectL > Play From Boot 설정...`의 `GameBootstrap 값 덮어쓰기 (이 PC만)` 아래에 `튜토리얼 사용`을 둔다. 시작 구간이나 `Use Dev Progress`에 종속시키지 않는다.
- 저장은 `GameBootstrapEditorOverrides.UseTutorial` 하나가 담당한다. EditorPrefs 키는 `ProjectL.BootOverride.UseTutorial`, 미저장 기본값은 `true`다.
- Boot는 재생 시작에 덮어쓰기가 켜져 있을 때만 값을 읽고, Factory 경로로 불변 `autoStartTutorial` 값을 전달한다. 덮어쓰기 꺼짐과 빌드는 `true`다.
- 꺼져 있으면 `TutorialViewSystem.Attach`가 기존 화면 연결·Context 게시를 마친 뒤 자동 Start 제출을 건너뛴다. 켜져 있으면 기존 대상 맵·첫 전투·완료 이력 판정을 그대로 수행한다.
- 재생 중 설정 변경은 다음 재생부터 적용한다. 진행 중 세션 취소, 강제 튜토리얼 실행, 완료 이력 변경은 포함하지 않는다.

## 접근 방식

승인 방식은 설정 저장 → Boot의 시작 값 확정 → 기존 Factory → 튜토리얼 자동 시작 소유자로 전달하는 것이다. 튜토리얼이 EditorPrefs를 직접 읽는 방식은 에디터 저장 방식과 런타임 동작을 결합하므로 채택하지 않는다. 튜토리얼 모듈 전체를 제외하는 방식은 서비스·입력·정리 수명까지 바꾸므로 채택하지 않는다.

## 구조 점검

- SSOT: 저장값은 EditorPrefs 한 곳, 진행·완료 상태는 기존 Tutorial Buffer에 둔다. 생성 시 전달하는 값은 해당 재생의 고정 설정이다.
- SRP: 창은 입력, 저장부는 영속화, Boot·Factory는 조립, TutorialViewSystem은 자동 시작 여부를 맡는다.
- OCP: 기존 생성 인자에 기본 `true` 값을 추가하며 기존 호출의 기본 동작을 유지한다.
- LSP·ISP: 공개 서비스 계약·등록 단위는 유지하며 새 서비스 인터페이스를 만들지 않는다.
- DIP: 런타임 View에 에디터 저장부 의존성을 추가하지 않고 필요한 bool만 주입한다.
- JMH/FW-001~004: 기존 Factory 생성·Export·Group·Builder·조회 경로를 유지한다. 모듈의 초기화·정리를 생략하지 않는다.
- 기능 독립성: 변경은 개발 설정 UI·저장·조립 전달과 튜토리얼 자동 시작 경계에 한정한다. Core·다른 전투 기능은 수정하지 않는다.

## 검증

프로젝트 지시에 따라 Test 코드를 작성하지 않고 MSBuild·Unity 실행을 하지 않는다. 변경 diff·호출 경로·전처리 경계를 정적으로 확인하고 사용자에게 켜짐/꺼짐/덮어쓰기 꺼짐/다음 재생 적용 확인을 인계한다. 컴파일 미확인.
