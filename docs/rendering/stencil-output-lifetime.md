# 스텐실 전환 출력 수명

2026-11-02 현재: 사용자 요청으로 `ScreenTransitionSystem`의 RenderTexture용 렌더 카메라 참조·제어를 제거했다. 카메라 이름 설정, Handle 대여·반환, 활성화·비활성화, 타깃·마스크·렌더 순서 변경과 복원을 더 이상 수행하지 않는다. 테스트 Runner도 이 카메라를 조회하지 않는다. 스텐실 합성·완료 대기와 Main·버추얼 카메라 처리는 유지한다. RenderTexture 출력 구성과 카메라 수명은 외부에서 관리한다. 현재 사용법은 [화면 전환 사용법](../render-screen-transition.md)을 따른다.

현재 변경 검증: `ProjectL.Systems.csproj` MSBuild 성공(오류 0, 변경 범위 밖 경고 8), 삭제한 카메라 설정·필드 참조 잔여 없음, diff 공백 검사 통과. Unity 배치 실행은 다른 인스턴스의 프로젝트 사용으로 종료되어 EditMode·실제 화면 확인은 미실행이다.

아래는 변경 전 동작과 검증 이력이다. 카메라 수명 정책과 37개 통과 기록은 현재 구현의 검증 결과가 아니다.

2026-10-20: 렌더 카메라는 스텐실 전환을 사용하는 동안만 렌더링한다.

- Boot·JMHTest의 `RenderTexture Camera`는 Camera 컴포넌트만 기본 비활성화한다. 오브젝트와 카메라 추적 구성은 유지한다.
- `ScreenTransitionOutput.Begin`이 기존 카메라 Handle로 출력 설정을 준비하고 활성화한다.
- 마지막 반경의 실제 합성 표시(`PresentationVersion` 증가)를 기다리는 동안에는 카메라와 스텐실 패스를 유지한다.
- 표시 확인 후 메인 카메라의 레이어를 확정하고 렌더 카메라를 즉시 끈다. 완료된 Scop의 Dispose까지 렌더링을 계속하지 않는다.
- 취소·준비 실패·실행 실패·Shutdown에서도 렌더 카메라는 꺼진 상태로 돌아간다. 진입 때 잘못 켜져 있었어도 다시 켜지지 않는다. 렌더 순서·마스크·타깃 텍스처의 복원은 유지한다.
- 스텐실 패스 등록 플래그의 실제 이름은 `IsAddRenderPass`다. 완료 시 기존 `ReleaseRenderFeature`, 취소·실패 시 기존 Scope.Dispose가 RenderFeatureSystem의 사용 수를 반환하고, 마지막 사용자가 끝나면 `UseRenderFreature(false)`가 이 플래그를 false로 만든다. 별도 상태 플래그나 직접 에셋 제어를 추가하지 않는다.

상태 소유자는 기존 ScreenTransitionOutput 하나다. 조립·서비스 등록·카메라 Catalog의 공용 계약을 변경하지 않고 해당 기능과 두 씬의 초기값만 수정한다(SSOT, SRP·OCP·LSP·ISP·DIP 및 FW 생명주기 유지). 기능 변경을 위해 FieldCamera·게임 Core·다른 연출 구현을 수정하지 않는다.

검증: ScreenTransitionOutputTests에서 기본 비활성 씬, 마지막 프레임 대기, 완료 직후 비활성, 재사용, 취소·실패·Shutdown을 확인한다. StencilTransitionBridgeTests에서 실제 RenderFeatureSystem→RenderFeatureScop→StencilBlendingFeature의 IsAddRenderPass 켜짐·해제·재사용을 검증한다.

Unity 6000.0.73f1 / D3D11 EditMode에서 위 두 fixture와 ScreenTransitionScopTests·ScreenTransitionInitializationTests를 실행해 37개 통과, 실패·건너뜀 0개를 확인했다. 원본 결과는 Git 제외 `Library/StencilOutputCheck/results.xml`이다. 실제 전투 재생에서 전환 화면을 육안으로 확인한 결과와는 구분한다.
