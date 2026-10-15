# 균열 기반 배경 전환

`Assets/Settings/PC_Renderer.asset`의 **StencilBlendingFeature → Setting**에서 조절한다.
기존 `TargetRenderTexture` 연결을 그대로 사용하며, 별도 SO나 컨트롤러가 필요하지 않다.

- **Shape = Crack**: 균열 내부에 대상 화면을 표시한다. 외부는 원래 화면이며, 경계 바깥에만 옅은 테두리를 표시한다.
- **Crack Opening Ratio**: 균열이 벌어지는 비율. `0`이면 효과가 완전히 사라진다. 기본 `0.08`, 넓게 보려면 `0.15~0.35`를 사용한다. 텍스처 투명도가 아니라 폭을 조절한다. `1`은 Crack Size X만큼의 중심부 폭이며, 화면 전체 전환을 뜻하지 않는다.
- **Crack Size**: 월드 단위의 가로 폭(X), 세로 길이(Z). 두 값을 바꿔 가로세로 비율을 조절한다.
- **Crack Center / Rotation**: 월드 XZ 중심 위치와 Y축 회전 각도.
- **Crack Jaggedness**: 중심 균열과 가지의 꺾임 정도.
- **Crack Branch Ratio**: 옆으로 뻗는 가지 길이. `0`이면 주 균열만 표시한다.
- **Crack Outline Ratio**: 중심부 벌어짐 폭에 대한 외곽선 두께 비율. 기본 `0.15`.
- **Crack Outline Opacity**: 외곽선 강도. 기본 `0.2`, `0`이면 외곽선만 숨긴다.
- **Line Color**: 외곽선 색. 알파도 외곽선 강도에 곱해진다.
- **Line Noise…**: 기존 노이즈 텍스처, 타일링, 흐름 설정. 균열 형상은 텍스처 없이도 생성된다.

기존 좌우 전환은 **Shape = World Split**으로 선택할 수 있다. `World Line Ratio`와 `Line Thickness`는 이 모드에서만 사용한다.

실행 중 조절할 때는 기존 렌더 기능의 `SetProperty` 경로를 사용한다. 추가된 키는 `Shape`, `CrackCenter`, `CrackSize`, `CrackRotation`, `CrackOpeningRatio`, `CrackJaggedness`, `CrackBranchRatio`, `CrackOutlineRatio`, `CrackOutlineOpacity`이며 float/Vector2/MaskShape 값을 받는다.

현재 효과는 기존 깊이 텍스처로 복원한 월드 XZ 위치에 적용하는 화면 합성이다. 실제 지형을 파거나 이동시키지 않는다. 별도의 캐릭터 제외 마스크는 없으며, 바닥 위 오브젝트도 같은 XZ 영역이면 전환 대상이다. 두 카메라의 위치·방향·투영·화면 비율을 일치시키고 대상 카메라가 먼저 TargetRenderTexture를 렌더링해야 두 배경이 맞물린다.

## 계산 비용

동일 Renderer Feature 안에서 균열 필드 생성과 화면 합성을 관리한다. 매 프레임 화면 픽셀마다 30개 선분을 순회하지 않는다.

- 균열 모양을 512×512 RGHalf 텍스처에 캐시한다(약 1 MiB). 최초 실행 또는 크기·꺾임·가지 길이·외곽 폭 설정 변경 시에만 생성한다.
- 실행 중 벌어짐 비율·위치·회전·색·외곽선 강도·노이즈 흐름 변경은 캐시를 다시 만들지 않는다. Inspector 재직렬화나 렌더 기능 재생성 시에는 캐시도 재생성될 수 있다.
- 화면 합성은 캐시를 한 번 샘플링해 내부/외부를 판정한다. 대상 텍스처·노이즈 샘플링과 깊이 기반 좌표 복원은 여전히 필요하다.
- 벌어짐 비율이 0이면 합성 패스 자체를 건너뛴다. 결과를 URP 카메라 컬러로 넘겨 기존 전체 화면 복사 패스도 제거했다.
- 캐시 해상도에 따른 근사이므로 아주 얇은 균열이나 극단적인 확대에서는 세부 모양이 달라질 수 있다. 대상 배경을 렌더링하는 별도 카메라 비용은 이 최적화와 별개다.
