# 특성·Activation 재생 도구 — 공용 변경 공유 (미전달)

- 상태: 구현 / MSBuild·Unity 컴파일 / 관련 테스트 30/31 / 네 프리셋 각각 20회 반복·중단 확인. 환경 종료 오류는 사용자 결정으로 유지. 외부 발송하지 않음.
- 사용자 요청: [설계](../specs/2026-10-30-trait-activation-preview-design.md)와 [계획](../plans/2026-10-30-trait-activation-preview-plan.md)의 A~D 순차 구현.
- 사용 안내: [특성 즉시 재생](../viewDocs/trait-activation-preview-guide.md).

## 공용 변경

1. `CombatEventPlaybackFactory`로 사건 Presenter·국면 전환 Presenter·재생 조정 객체 생성만 추출했다. 운영 Display의 필수 참조·입력·HUD 조립과 Bind/Refresh/Clear/Release 소유권은 유지한다.
2. `CombatSceneEventPresenter.Refresh`/`CombatShownFacts.Replace`에 CharacterCombatFacts 목록 overload를 추가했다. 기존 CharacterData 입력은 사실 변환 뒤 같은 경로를 쓴다. 합성 결과를 Buffer에 쓰지 않는다.
3. `CombatEffectWindow`에 서비스·부모·빌린 Scope를 받는 overload를 추가했다. 기존 두 인자 생성자는 유지한다. 서비스 Scope 목록을 직접 수정하거나 최종 수명을 옮기지 않는다.
4. `PlayFromBootToggle.Apply`가 활성 임시 시작 씬을 존중한다. 새 `PreviewPlayStartScene`은 PC별 Boot 켜짐 값을 바꾸지 않고 SessionState로 이전 시작 씬을 보관·복원한다.

## 등록·조회·수명

- 생성: 기존 FrameworkRuntimeFactory → Buffer/Global/InGame/LifeCycle. JobRegister·카메라 Catalog 의존 때문에 부분 Global을 만들지 않는다. 게임 구간 진입/Job 실행/Run 시작은 호출하지 않는다.
- 내부 등록: 기존 하위 Factory와 Group Builder 그대로. 루트 Export도 기존 그룹만 유지한다. 새 루트 서비스 등록은 없다.
- 소비자 조회: SystemGroupsService.Global → Render Context/CoreGroup/ViewGroup, Audio/UI. Factory는 필요한 좁은 서비스를 표시 객체에 넘긴다.
- 유닛 HUD: 기존 UIId.UnitHUDLayer/Parent=None/Parallel, CombatUnitHUDFactory → AttachUISystem → 동적 등록/고정 열기. 실행 해제 때 동적 등록도 해제한다. 도구 조작은 EditorWindow에만 있다.
- Tick: 호스트가 runtime.Tick 후 OrderedCombatPresentation.Tick을 각각 한 번 호출한다. Editor 창은 재생기를 Tick하지 않는다.
- 종료: Ordered.Detach → 실행별 표시/효과/HUD 반환 → runtime.Shutdown → 배경 정리. 표시 템플릿과 실제 실행 인스턴스를 나눠 실행마다 풀·외형 캐시도 새로 만든다. Scope는 세션당 하나다.

## 영향과 제한

Core·FieldCamera·Architecture 구현, 운영 씬 번호/Build Settings, 기존 TraitVisual 에셋은 변경하지 않았다. 테스트 씬은 `Assets/Scenes/Dev/TraitActivationPreview.unity`, 에디터 입력 프리셋은 `Assets/ProjectL/Editor/CombatPreview/Presets/`다. 공개 창구/호스트/입력/프리셋/테스트 Factory는 UNITY_EDITOR 한정이다. asmdef와 InternalsVisibleTo는 변경하지 않았다.

동시 작업 종료 후 사용자 지시로 실제 Unity를 검증했다. 네 프리셋은 각각 20회 반복·중단을 통과했으나 종료 때 OptionInputRouter.Shutdown이 미등록 UIId.Setting을 닫아 실패했다. 기존 GlobalUICanvas 생성·등록·회수 보완은 사용자 결정으로 진행하지 않는다. 앞으로 View 측에서는 테스트 코드를 추가하지 않으며 이번에 추가한 테스트 코드는 후속 요청으로 삭제했다. 공용 입력기를 수정하거나 오류를 억제하지 않았다. 관련 테스트 실패 1건은 기존 Pull/Mark SetUp에서 발생한 Refid -2 직렬화 오류이며 미리보기 9건·특성 계약 2건은 통과했다. 상세 근거·잔여 확인은 계획 §9.3과 사용 안내 §5에 있다.

실제 검증 중 합성 사실의 필수 Category/ActionCostSettlement를 보완했고, 테스트 씬·생성기에 Boot의 AudioManager 구성과 FMOD StudioListener를 추가했다. 운영 오디오 구현이나 원본 에셋 값은 이 보완에서 수정하지 않았다.

후속 사용자 요청으로 이번 테스트 `TraitActivationPreviewTests.cs`와 자동 검증 도구 `TraitActivationPreviewVerifier.cs` 및 각 meta를 삭제했다. 위 테스트 수치는 삭제 전 실행 이력이며, 자동 검증 메뉴는 더 이상 제공하지 않는다. 직접 사용하는 미리보기 창·씬·프리셋은 유지한다. 사용 순서는 최신 사용 안내를 따른다.
