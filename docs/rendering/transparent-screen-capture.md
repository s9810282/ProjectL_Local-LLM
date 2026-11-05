# 투명 포함 화면 캡처

2026-10-17 사용자 요청: 기존 `Render/RendererFeatures` 아래에 투명 이펙트까지 포함한 화면을 왜곡에서 읽을 수 있는 Renderer Feature를 추가한다.

## 설계와 Phase 1 계획

- `Systems/TransparentScreenCapture/`에 독립 Feature·Shader Graph용 샘플 함수를 둔다.
- 일반 투명 렌더 이후, 후처리 이전에 현재 카메라 컬러를 별도 RenderGraph 텍스처로 복사한다. HDR 형식·해상도·차원은 원본을 유지하고 복사본은 단일 샘플, Bilinear/Clamp로 만든다.
- 출력 계약은 `_ProjectLTransparentScreenTexture`다. 별도 RenderTexture 에셋·보조 카메라는 필요 없다. 텍스처의 수명은 현재 카메라 RenderGraph가 소유하며 다음 프레임의 일반 투명 패스에서 읽는 용도로 쓰지 않는다.
- 선택적으로 `distortionLayerMask`의 투명 오브젝트를 복사 직후 다시 그리는 후순위 렌더를 제공한다. 기본값은 Nothing이며 캡처만 한다. 해당 Layer는 Renderer Data의 기본 Transparent Layer Mask에서 제외하고 카메라 Culling Mask에는 포함해야 한다. Feature가 원본 Renderer Data·씬·Layer 목록을 자동 변경하지 않는다.
- 사용자 추가 지시: 기존 BaseRenderFeature를 상속하지 않는 독립 Feature로 만든다. Unity의 ScriptableRendererFeature만 상속하고 Renderer Data의 Add Renderer Feature로 활성화한다. 기존 RenderFeature Scope·설정 서비스·타입/키 등록에 연결하지 않는다. Game/SceneView 카메라를 지원하고 Preview·Prefab 편집은 제외한다.
- RenderGraph 경로를 대상으로 하며 Compatibility Mode·Overlay UI의 최종 합성·가려진 표면 복구는 이번 범위가 아니다. 카메라마다 캡처하고 같은 카메라의 후순위 패스에서 사용한다.

SSOT/SOLID/FW·독립성: 캡처/후순위 렌더와 Layer 설정은 신규 Feature에, 샘플링 함수는 같은 폴더 HLSL에 둔다. GPU 자원을 다른 시스템이나 Material에 영구 보관하지 않는다. 다른 화면 효과·Framework·DB·Core 로직·FieldCamera·Architecture를 수정하지 않는다. 캡처 구현을 바꿀 때 신규 폴더와 해당 GPU 검증만 읽고 검증한다. 기존 공용 등록 계약에는 변경이 없다.

Task: (1) 독립 Feature·샘플 함수와 meta (2) 기존 공용 연결이 없는지 확인 (3) 연결 가이드 (4) Unity 컴파일 및 실제 GPU에서 불투명+알파 합성·캡처 후 소비·카메라/해상도·MSAA·사용 해제 확인 (5) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

## Renderer 연결

1. 사용하는 URP Renderer Data(예: `Assets/Settings/PC_Renderer.asset`)의 **Add Renderer Feature**에서 **Transparent Screen Capture Feature**를 추가한다. 기존 관리 Feature와 달리 Scope 사용 요청은 필요 없다.
2. 왜곡용 GameObject만 전용 Layer(예: Distortion)에 둔다. 불꽃·연기 등 일반 투명 오브젝트는 기존 Layer에 둔다.
3. Renderer Data의 **Filtering > Transparent Layer Mask**에서 전용 Layer를 제외한다. 카메라의 **Culling Mask**에서는 제외하지 않는다. 기본 패스와 후순위 패스에 중복으로 그리지 않기 위한 설정이다.
4. 새 Feature의 **Distortion Layer Mask**에 같은 Layer를 지정한다. Nothing이면 캡처만 하므로 일반 투명 Shader에서 당장 캡처를 읽을 수 있는 것은 아니다.
5. RenderGraph를 활성화한다(Compatibility Mode 끄기). 이 캡처는 후처리 전 화면이며 Screen Space Overlay Canvas의 최종 UI는 포함하지 않는다.

렌더 순서: 일반 불투명 → 일반 투명/파티클 → 화면 복사 → 전용 Layer의 왜곡 → 후처리. 여러 왜곡 오브젝트는 같은 복사본을 읽으므로 서로의 왜곡 결과를 재귀적으로 캡처하지 않는다.

## Shader Graph 연결

- Unlit Graph의 Surface Type을 **Transparent**, Blend를 **Alpha**, Depth Write를 **Off**로 설정한다.
- **Custom Function** 노드를 만들고 Type을 **File**로 설정한다.
- Source에 같은 Feature 폴더의 `SampleTransparentScreen.hlsl`을 지정한다.
- Name은 **ProjectLTransparentScreen**(접미사 `_float` 제외), 입력 **UV / Vector2**, 출력 **Color / Vector3**로 만든다. Float/Half 정밀도를 지원한다.
- `Screen Position(Default).RG + 왜곡 오프셋`을 UV에 연결하고 Color를 Base Color에 연결한다. 효과의 마스크를 Alpha에 연결한다.
- 노이즈 예시: `(Noise.RG * 2 - 1) * Strength * Mask`. Strength는 화면 UV 단위다. 마스크/노이즈의 오브젝트 UV와 캡처 샘플의 화면 UV를 구분한다.
- Scene Color 노드는 계속 불투명 전용 텍스처를 읽는다. 이 효과에서는 위 Custom Function으로 교체해야 한다. 캡처 텍스처를 별도 Material 프로퍼티에 넣지 않는다.

이 Feature는 투명 합성 결과의 **색**을 제공한다. 개별 투명 오브젝트의 알파나 깊이 레이어를 분리한 텍스처가 아니다. Shader Graph 노드 Preview는 검정으로 표시되며, 전용 Layer로 렌더되는 실제 Game/Scene View에서 확인한다. 화면 가장자리는 Clamp로 샘플링한다.

## Phase 1 완료·검증

- 독립 Feature·Shader Graph 샘플 HLSL·Unity meta와 연결 안내를 추가했다. 기존 BaseRenderFeature·Scope·타입/키·설정 서비스 연결은 없다. 기존 공용 파일의 최종 diff는 0이다.
- Unity 6000.0.73f1 / URP 17.0.4의 격리 프로젝트에서 실제 신규 소스와 HLSL을 컴파일하고 D3D11 GPU 렌더로 확인했다.
- 녹색 불투명 위에 빨강 알파 0.5를 합성한 색 `(0.5, 0.5, 0)`을, 캡처 뒤에 그린 다른 위치의 소비자가 같은 프레임에서 읽었다. 위/아래가 다른 화면에서 Y 방향도 확인했다. 이미지를 확인했다.
- 256×256·MSAA 1과 320×320·MSAA 4에서 통과했다. Feature 비활성화 시 후순위 소비자가 사라지고 재활성화 시 복구되며 원본 색 변경이 다음 캡처에 반영됐다. Unity/RenderGraph 오류는 0이었다. 패키지 자체의 기존 obsolete 경고는 별도다.
- 검증 소스·로그·이미지는 Git 제외 `Library/TransparentScreenCaptureUnityCheck/`에 보관한다. 검증은 직교 Game 카메라 기준이며 원본 프로젝트의 Shader Graph UI 연결·왜곡 아트·카메라 스택/XR·Compatibility Mode는 확인 범위 밖이다.
- 기존 Renderer Data·씬·이펙트 에셋에는 연결하지 않았다. 사용자는 위 연결 순서대로 Feature와 전용 Layer를 지정한다. Phase 리뷰 지점에서 멈춘다.

2026-10-17 컴파일 후속 수정: 원본 프로젝트에서 `Shader.PropertyToID`의 Shader가 상위 `ProjectL.Render.Shader` 네임스페이스로 해석되어 CS0234가 발생했다. `UnityEngine.Shader.PropertyToID`로 타입을 명시했다. 초기 격리 검증에는 기존 네임스페이스가 빠져 있었다. 실제 기존 RenderFeatureCameraFilter 소스도 격리 프로젝트에 포함하여 해당 네임스페이스와 공존하는 Unity 컴파일이 통과함을 확인했다(`unity-namespace-check.log`).
