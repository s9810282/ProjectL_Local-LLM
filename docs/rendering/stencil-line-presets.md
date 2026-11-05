# 스텐실 경계선 프리셋

## 설계와 범위

`StencilBlendingFeature`의 Inspector에서 `LineKey`와 `LineData`를 등록한다. DB나 별도 SO는 만들지 않는다. `LineKey`는 enum이며 `Capter1 = 0`, `Capter2 = 1`, `Capter3 = 2`를 제공한다. 철자는 사용자 지정 그대로 유지한다. enum의 숫자는 Unity 직렬화 값이므로 변경하지 않는다. 같은 키를 중복 등록할 수 없다.

`LineData` struct는 `LineThickness`, `LineColor`, `LineNoiseTexture`, `LineNoiseTiling`, `LineNoiseSpeed`, `LineDistortion`을 갖는다. 원의 중심·반경·전환 폭, 대상 RenderTexture, Compute Shader는 기존 `StencilSettings`에 남긴다.

원본은 직렬화된 `Line Presets` 목록 한 곳이다(SSOT). Feature 생성 및 Inspector 재검증 때 `Dictionary<LineKey, LineData>`를 다시 만들고, 선택 키가 바뀔 때 조회한다. Feature가 선택한 값을 Pass에 전달하며 Pass는 GPU 설정과 노이즈 애니메이션을 처리한다. 딕셔너리와 선택 값은 원본에서 파생된 조회 캐시이며 별도로 편집하지 않는다.

기존 렌더 서비스의 Scope 대여·반환 경로(FW-011)를 유지한다. 프리셋 데이터는 서비스로 등록하지 않으며 새 수명 모듈을 추가하지 않는다. 노이즈 텍스처가 바뀌면 기존 RTHandle 래퍼를 해제하고 새 래퍼를 만들며, 원본 텍스처 자산은 해제하지 않는다.

## Inspector 사용법

1. `Assets/Settings/PC_Renderer.asset`에서 `StencilBlendingFeature`를 펼친다.
2. `Line Presets`의 각 `Key` 드롭다운에서 `Capter1`, `Capter2`, `Capter3`를 선택한다.
3. `Data`의 값을 설정한다. `Line Distortion`은 `0`이면 원형 경계를 유지하며 외곽선에 텍스처 무늬를 표시하고, `1`이면 기존 경계 변형, `1`보다 크면 변형 증폭이다.
4. `Setting > Line Key` 드롭다운에서 적용할 챕터 키를 선택한다.

등록되지 않은 키를 선택하거나 정의되지 않은 enum 값을 프리셋에 등록하면 `InvalidOperationException`으로 설정 오류를 보고한다. 목록을 추가할 때 복제된 키를 다른 키로 바꿔야 한다. 서비스 API에 정의되지 않은 enum 값을 넘기면 `ArgumentOutOfRangeException`을 던진다.

기존 PC Renderer의 다섯 설정은 값과 텍스처 참조를 그대로 유지한 `Capter1` 프리셋으로 이전했다. `Capter2`·`Capter3`도 같은 값으로 초기화했으며 Inspector에서 각각 조절한다. 초기 선택은 `Capter1`이다. 기능 활성화 여부는 바꾸지 않았다.

## 런타임 선택

기존 `IStencilSettingService.SetBorderWidth` / `SetBorderColor` 대신 프리셋을 선택한다. 저장소에 기존 두 API를 호출하는 코드는 없었다.

```csharp
using ProjectL.Render.RendererFeatures.Systems.Stencil;
using ProjectL.Render.Shader.PostProcessing.Core;

// 기존 렌더 Scope를 통해 설정 서비스를 얻는다.
var stencil = scop.UseRenderFeature(RenderFeatureKeys.Stencil);
stencil.SetLineKey(LineKey.Capter1);
```

획득한 기능은 사용이 끝날 때 해당 Scope로 반환한다.

## 구현과 검증 기록 (2026-10-13)

한 Phase로 `LineKey`·`LineData`, Inspector 목록과 딕셔너리, Pass 전달, 기존 에셋 값 이전을 구현한다. 이 Phase 뒤 Unity 확인을 리뷰 지점으로 둔다.

- 구현 완료: 새 `.cs`의 `.meta` 포함, 기존 선택·합성·자원 해제 경로 유지. 사용자 후속 지시에 따라 이름 기반 struct 키를 enum으로 교체하고 세 챕터 프리셋을 등록했다. `LineKey.cs.meta` GUID는 유지했다.
- 정적 확인: 기존 API 호출부 없음, Stencil Feature 사용 에셋은 PC Renderer 하나, 새 GUID 중복 없음.
- 컴파일 확인: Unity가 생성한 `Assembly-CSharp.rsp`의 참조·정의로 전체 Assembly-CSharp 소스와 새 두 파일을 Roslyn으로 컴파일해 오류 0·경고 0. 산출물은 시스템 임시 폴더에만 저장했다. MSBuild는 SDK 경로 접근 및 복원 문제를 해소한 뒤 빌드 진행이 지연되어 중단하고 직접 컴파일로 확인했다. Unity 에디터 컴파일·실행 검증을 대신한 것은 아니다.
- 에셋 이전 확인: 이전 HEAD와 비교해 선 설정 5개 및 기존 합성 설정·활성 상태 6개의 값과 참조 보존 통과. 변경 파일의 `git diff --check`, C# LF·깨진 문자 검사 통과.
- Unity 확인 대기: Inspector에서 세 챕터 키가 드롭다운에 표시되는지, 프리셋을 선택·편집해 각 값이 적용되는지, 노이즈 텍스처 교체와 기능 해제/재사용이 정상인지, EditMode 테스트가 통과하는지 확인한다.

## 프리셋별 바깥 왜곡 강도 (2026-10-13)

두 계산 모드를 추가하는 안은 취소했다. 기존 계산에 `LineData.LineDistortion` 배율 하나를 추가하며 Inspector에서 프리셋마다 관리한다.

`0`에서는 셰이더의 일반 화면 `uv = (pixel + 0.5) / resolution`에 X/Y 타일링을 그대로 곱한 `uv * LineNoiseTiling`으로 `LineNoiseTexture`를 직접 샘플링한다. 원형 재매핑·띠 폭 정규화·각도 반복 반올림·시간 오프셋을 적용하지 않는다. 샘플의 R 채널 무늬를 `LineColor`로 외곽 띠에 표시하며, 띠 바깥 끝의 페이드는 유지한다. `1`에서는 기존 원형 UV와 경계 변형 결과가 유지되고 더 큰 값은 바깥 변형을 증폭한다. 음수는 Inspector와 GPU 전달 경계에서 0으로 제한한다.

일반 2D 타일링의 각 축은 입력값 그대로다. `(1, 1)`이면 원본 UV, `(3, 1)`이면 X 방향 3회 반복이며, 한 축이 0이면 해당 축은 한 행/열만 읽는다. 원형 경로용 기존 `(3, 0)` 설정은 보존했으므로 무왜곡 2D 표시를 사용할 때 Y를 1 등 0이 아닌 값으로 설정한다.

`0~1`의 중간 값은 원래 UV 샘플과 기존 원형 UV 샘플을 보간한다. UV 좌표 자체를 보간하지 않아 `atan2` 이음새의 반복 불일치를 피한다. 양 끝에서는 해당 UV만 한 번 샘플링한다.

무변형 띠는 `step(0, radius - CircleRadius) * (1 - smoothstep(0, 1, (radius - CircleRadius) / lineWidth))`에 텍스처 값을 곱한다. 이 직접 표시 마스크와 기존 변형 마스크를 `saturate(LineDistortion)`으로 보간한다. `0~1`에서는 텍스처 표시와 경계 변형이 함께 전환되며, `1` 이상은 기존 변형 외곽선 계산을 사용한다. 별도 모드 enum은 추가하지 않는다.

`LineDistortion = 0`에서는 외곽 띠 위치에서 일반 2D UV와 타일링에 해당하는 텍스처 부분이 보인다. 띠 안에 텍스처 전체를 다시 펼치지 않는다. 띠 두께가 0이면 최종 마스크에서 표시를 끄며 페이드 계산의 0 나눗셈을 방지한다.

세 챕터 프리셋의 새 필드는 `1`로 초기화하고 기존 다섯 값은 보존한다. 원형 UV 경로의 시간 이동·Pass 재생성 때 위상 보존은 유지한다. 별도 DB·서비스·모드 enum은 추가하지 않는다.

Unity 확인 대기: 각 프리셋에서 `Line Distortion`을 `0`, `0.5`, `1`, `2`로 바꾸어 일반 UV 텍스처 표시·혼합·기존 원형 변형·증폭이 적용되는지 확인한다. `0`에서는 텍스처 좌표가 `uv * LineNoiseTiling`이어야 한다. 사용자 참고 이미지의 갈라진 무늬는 해당 무늬의 텍스처를 연결해야 하며, 두께·색은 프리셋에서 정한다. 기존 에셋의 텍스처·색·두께는 자동 교체하지 않았다.

원형 띠에 텍스처 전체를 펼치는 이전 CPU 미리보기와 검증은 현재 일반 UV 직접 읽기 동작의 증거로 사용하지 않는다. 수정 후 Unity 생성 참조의 C# 컴파일 오류 0·경고 0, D3D11 `CSMain` 컴파일 통과(위에 기록한 FXC 임시 include 처리 사용). 강도 0의 `directUV = uv * LineNoiseTiling` 샘플링과 원형·띠 UV 재매핑 제거, `git diff --check` 통과. Unity 화면 검증은 아직 하지 않았다.

## 원 성장 중 노이즈 흐름 수정 (2026-10-13)

- Inspector에서 반경을 변경하면 URP `ScriptableRendererFeature.OnValidate`가 `Create()`를 호출한다. 기존 Pass 내부의 누적 이동량은 매번 0으로 초기화되므로 연속 반경 편집 중 흐름이 멈춰 보일 수 있다. 누적 이동량과 마지막 갱신 프레임을 Feature 수명의 공유 상태 하나에 보관하고, 새 Pass에 같은 상태를 전달한다. Pass는 시간 누적을 계속 담당하며 같은 프레임 중복 진행 방지도 유지한다(SSOT·기존 책임 경계 유지).
- 기존 노이즈 반경 UV는 `radius * abs(tilingY) - offset`이었다. 경계 반경이 성장하면 성장량이 시간 이동량을 상쇄할 수 있다. `(radius - CircleRadius) * abs(tilingY) - offset`으로 바꿔 원 경계로부터의 거리를 사용한다. 따라서 경계와 함께 움직이는 위치에서 성장/축소와 무관하게 설정 속도로 노이즈가 흐른다. 양수 속도는 움직이는 경계 기준 바깥 방향, 음수는 안쪽 방향이다.
- 기존 PC Renderer의 `tilingY = 0`에서는 UV 상쇄가 발생하지 않으므로 Inspector의 Pass 재생성 대응이 특히 필요하다. 프리셋 값과 원형 마스크·테두리 계산은 그대로 유지한다.
- 검증: Unity 생성 참조를 사용한 전체 Assembly-CSharp 직접 컴파일 오류 0·경고 0. 실제 공유 애니메이션 상태를 Unity Mono로 실행한 시간 진행·동일 프레임 재생성 중복 방지·이동량 유지·정지·음수 속도/래핑·양수 래핑 6건 통과. 경계 기준 UV의 성장/축소 속도 독립성 수치 검사 36건 통과.
- 셰이더 검증: Windows SDK FXC로 D3D11 `CSMain`(cs_5_0) 컴파일 통과. 임시 복사본에서 Unity 전용 `#pragma kernel`을 제외하고 공용 include의 미사용 `PopMarker()` 매크로만 FXC 호환 형태로 바꿨다. 실제 프로젝트의 공용 include는 변경하지 않았다. Unity 셰이더 임포트 검증과는 구분한다.
- Unity 화면 확인 대기: Play 중 Inspector에서 반경을 연속 증가/감소시키고, 반경 타일링 Y가 0인 경우와 0보다 큰 경우 모두 흐름이 계속되는지 확인한다. 성장 속도를 흐름 속도와 같게 맞춘 경우, `LineNoiseSpeed = 0` 및 음수, 프리셋 교체도 확인한다.
