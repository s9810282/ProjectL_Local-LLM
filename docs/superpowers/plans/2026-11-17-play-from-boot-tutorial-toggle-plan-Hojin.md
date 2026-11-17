# Play From Boot 튜토리얼 사용 설정 Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans. 사용자 승인에 따라 현재 checkout에서 직접 구현한다. 프로젝트의 Test 작성·MSBuild·커밋·push 금지를 스킬 기본 절차보다 우선한다.

**Goal:** PC별 개발 재생 설정에서 튜토리얼 자동 시작을 켜고 끈다.

**Architecture:** EditorPrefs 설정을 Boot에서 한 번 읽고 기존 Factory 조립 인자로 전달한다. TutorialViewSystem은 생성 시 받은 값을 보관하고 Attach의 자동 Start만 제어한다.

**Tech Stack:** Unity 6000.0.73f1, C#, EditorGUILayout, EditorPrefs.

**Spec:** [승인 설계](../specs/2026-11-17-play-from-boot-tutorial-toggle-design-Hojin.md)

## Global Constraints

- 기본값 true, 에디터 덮어쓰기에서만 적용, 다음 재생부터 반영.
- Core·FieldCamera·Architecture와 기존 다른 변경은 수정하지 않는다.
- Test 작성·MSBuild·커밋·push를 하지 않는다. 컴파일·재생은 사용자 Unity 확인이다.

## Review Focus

1. 저장값이 없을 때 기존 자동 시작을 유지한다.
2. false를 저장했어도 덮어쓰기 꺼짐과 비에디터 빌드는 기존 동작을 유지한다.
3. 로비·챕터·전투 시작에서 토글을 설정할 수 있고 첫 전투 대상 판정은 유지된다.
4. 재생 중 토글을 바꿔도 현재 세션에 영향을 주지 않는다.
5. false에서도 초기화·화면 연결·해제와 입력 정리 계약을 유지한다.

## Task 1: 설정·조립·자동 시작 연결

**Files:**
- Modify: `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs`
- Modify: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrapEditorOverrides.cs`
- Modify: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs`
- Modify: `Assets/ProjectL/Script/Systems/Factory/Runtime/RunTimeSystemFactory.cs`
- Modify: `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/InGameSystemFactory.cs`
- Modify: `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs`
- Modify: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Tutorial/TutorialViewSystem.cs`

**Interfaces:** `GameBootstrapEditorOverrides.UseTutorial` (bool, 기본 true). Factory의 생성 경로와 TutorialViewSystem 생성자 마지막에 선택 인자 `bool autoStartTutorial = true`를 추가한다. Core에는 이 값을 전달하지 않는다.

- [x] 저장 프로퍼티, 설정 창 토글과 다음 재생 적용 안내를 연결한다.
- [x] Boot가 재생 시작에 값을 확정하고 Factory → View 생성자로 전달한다.
- [x] TutorialViewSystem의 Context 게시 이후 자동 시작만 조건부로 수행한다.
- [x] 관련 호출과 전처리 경계를 읽고, `git diff --check -- <수정 파일>` 결과가 출력 없이 종료 코드 0인지 확인한다.
- [x] 별도 읽기 전용 코드 리뷰를 받고 결과와 사용자 Unity 확인 항목을 기록한다.

## 실행 기록

- 2026-11-17: 사용자 예외 승인과 위 동작 범위를 확인했다. 현재 브랜치는 `feature/Hojin/11_03_StageSelect`이고 이번 대상 파일에는 선행 수정이 없다. 현재 Unity 프로젝트에 직접 반영하며 기존 미커밋 변경은 보존한다.
- 검증 제한: 스킬의 TDD·빌드·커밋 절차 대신 프로젝트가 허용한 정적 검사와 사용자 재생 확인을 적용한다. 구현 후에도 컴파일 미확인으로 보고한다.
- 2026-11-17 코드 반영: 위 7개 C# 파일, 기존 생성 호출은 선택 인자의 기본 true를 사용한다. 직접·이름 지정 호출 경로를 확인했고, `UNITY_EDITOR`에서만 EditorPrefs를 읽으며 `DEVELOPMENT_BUILD` 단독과 일반 빌드는 Boot에서 true를 전달한다.
- 자동 Start 생략 때 활성 세션·미처리 요청이 없으므로 기존 TutorialPlan은 할 일이 없는 상태를 반환한다. Attach·Detach·Shutdown 등록과 화면·입력 정리 경로는 그대로다.
- 정적 검사: 대상 7파일 `git diff --check` 출력 없음·종료 코드 0. Test 코드 작성·MSBuild·Unity 실행 없음. 컴파일 미확인.
- 별도 읽기 전용 코드 리뷰: 지적 사항 없음. Review Focus 5항목과 SSOT·SOLID·JMH/FW 등록·조회 경로, 에디터/개발/일반 빌드의 전처리 경계를 확인했다. 실제 UI·재생·빌드 성공은 판단하지 않았으며 사용자 확인 대상으로 남긴다.
- 최종 형식: 수정 C#은 UTF-8·LF로 맞췄다. 로컬 Git의 줄바꿈 변환 예고를 제외한 최종 공백 검사는 `git -c core.autocrlf=false diff --check -- <수정 7파일>`로 확인한다(저장소 설정은 변경하지 않음).
- Task 1 구현·정적 검수 완료. 승인 범위 변경 없음. Unity 컴파일·재생 확인 대기.

## 공용 조립 변경 공유 — 민형님 전달용, 미전달

- `FrameworkRuntimeFactory.Create`, `InGameSystemFactory.CreateComposition` 및 내부 `Compose`, `InGameViewSystemFactory.CreateComposition` 마지막에 `bool autoStartTutorial = true` 선택 인자를 추가했다.
- 설정은 Runtime → InGame → View → TutorialViewSystem 생성자로만 전달한다. Core 조립 인자, Group 보관·Export, 루트 Builder 등록과 소비자 서비스 조회는 변경하지 않았다.
- 소스에서 기존 인자를 생략한 호출은 true를 사용한다. 리플렉션으로 생성 메서드를 호출하는 외부 코드가 있으면 실제 시그니처에 맞춰 전체 인자를 전달해야 한다. 현재 Assets C# 검색에서는 해당 리플렉션 호출을 발견하지 않았다.

## 사용자 Unity 확인 항목

1. 메뉴에서 덮어쓰기를 켜고 `튜토리얼 사용`을 끈 뒤 대상 첫 전투에 진입: 설명·실습 제한 없이 전투를 진행할 수 있는지 확인한다.
2. 재생을 종료하고 토글을 켠 뒤 같은 조건으로 진입: 기존 튜토리얼이 시작되는지 확인한다.
3. 토글을 끈 저장값을 둔 채 덮어쓰기를 끄고, 기존 씬 설정으로 대상 첫 전투에 진입: 기존 튜토리얼이 시작되는지 확인한다.
4. 재생 중 토글 변경은 현재 세션에 영향을 주지 않고 다음 재생부터 적용되는지 확인한다.
