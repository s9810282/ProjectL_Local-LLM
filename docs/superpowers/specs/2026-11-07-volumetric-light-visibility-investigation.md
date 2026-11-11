# JMHTest 볼륨메트릭 빛줄기 가시성 조사 — 2026-11-07

## 1. 판정

**현재 게임 카메라에서 “거리가 멀어서 빛줄기가 안 보이는 것이 당연하다”는 결론은 근거가 없다.** 카메라의 Near→Far 길이 56.35보다 볼륨 계산 범위 60이 길다. 중앙 지면(Y=0)까지의 계산 경로도 약 36.3이다. 앞선 거리 중심 설명은 정정한다.

실제로 기능을 켜서 Frame Debugger로 확인했을 때 계산 패스, 그림자 텍스처, 캐스케이드 행렬과 광원 입력은 존재했다. 화면 변화도 있었다. 다만 참조 이미지처럼 구분되는 줄기보다 전체 공기층·색·대비 변화로 나타났다.

**확인된 구조적 문제는 기존 안개와 새 산란 계산이 별개라는 점이다.** 기존 높이 안개는 주광원의 그림자를 읽지 않으며 새 볼륨 계산 뒤에 색과 알파로 합성된다. 따라서 기존에 눈에 보이는 안개 자체에 이 기능이 명암 줄기를 만드는 구조가 아니다. 장면은 천장 격자가 있는 참조와 달리 중앙이 열린 광장이며, 주광원 고도도 약 52.3°다. 이 배치와 합성 구조는 줄기 대비가 약할 수 있는 구체적인 근거다.

**추가 분리 진단 후의 결론: 중앙 광장의 차광 대비 부족이 우선 원인이며, 약한 산란 무늬가 배경과 합성되는 과정과 후처리가 가시성을 더 낮춘다.** 실제 산란광만 출력하면 왼쪽에 넓은 사선 띠가 생긴다. 경로별 산란 가중 평균 shadow visibility를 출력하면 중앙은 거의 흰색이고 왼쪽 일부에 회색 띠가 나타난다. 표면 그림자도 주변 건물에 대응해 출력된다. 따라서 그림자 계산의 완전한 불작동은 배제한다. 기존 높이 안개 합성 직전/직후에도 띠가 남아 **“안개 두 개가 전부 덮는다”는 가설은 주원인에서 제외**한다. 상세 비교·한계는 §9다.

현재 결과는 “밝은 낮에는 볼륨메트릭 줄기를 만들 수 없다”는 뜻이 아니다. 참조에는 햇빛을 나누는 천장 격자가 있지만 현재 중앙에는 해당 구조가 없고, 현재 광원은 낮게 들어오는 햇빛도 아니다. 밀도만 높이면 중앙의 고른 공기층이 더 강해질 가능성이 크다. 정확한 픽셀별 기여율, 모든 캐스케이드의 정확성, 개별 Bloom/DOF 영향까지 검증한 것은 아니다.

## 2. 조사 범위와 상태 보존

- 대상: 열려 있는 `JMHTest*`의 Scene/Game 화면, 현재 PC Renderer와 Frame Debugger, 저장된 `Assets/Scenes/JMHTest/JMHTest.unity`, 연결된 Renderer/Pipeline/Volume/Material/Shader 및 활성화 코드.
- 저장된 씬의 프리팹 인스턴스 루트 167개와 소스 23종, 직접 직렬화된 MeshRenderer 36개, 카메라 2개, 광원 4개를 목록화했다. 인스턴스 루트는 전부 월드 위치로 해석했다. 비활성·이펙트 인스턴스도 포함하므로 이 수를 실제 그리기/그림자 개수로 해석하지 않는다.
- `Bar_ver1_final`, `Bar_ver2_final`의 바이너리 FBX도 읽어 모델 노드 변환·GeometricTranslation·단위를 대조했다. 모든 프리팹의 삼각형을 Unity 임포트 결과와 대조하거나 GPU 가림률을 재현한 것은 아니다.
- 씬은 이미 미저장 변경 상태였다. 저장 파일의 Main Camera Y는 26.6이지만 실제 관찰/GPU 입력은 **25.89**였다. 아래 거리 계산은 실제 입력을 쓴다. 배치도는 저장된 씬 기준이므로 다른 미저장 배치 변경까지 보증하지 않는다.
- 재생을 시작하거나 씬을 저장하지 않았다. 앞선 ON/OFF 조사와 이번 분리 진단을 구분한다. 이번 추가 조사 시작 시점의 라이브 Feature 제목/내부 패스는 모두 켜져 있었으며 종료 시에도 둘 다 켜짐을 확인했다. Frame Debugger는 비활성화 후 닫았다.
- 이번 추가 조사에서는 Compute 최종 출력을 일시적으로 교체했다. 종료 시 조사 시작 원본으로 바이트 단위 복구했고 SHA-256도 일치했다(`9384589e952d3f78927e978f7269afaa160d595f3330d633d4c52938f695a401`). `PC_Renderer.asset`도 조사 시작 백업과 바이트 단위로 동일했다. 조명·카메라·배치·머티리얼·후처리 값의 상시 변경은 없다. 기존 FMOD·폰트 변경도 보존했다.
- **컴파일 미확인.** Test 코드 작성, MSBuild, 커밋·push 없음. 열린 에디터의 프레임 관찰은 새 빌드나 게임 전체 재생 검증을 뜻하지 않는다.

## 3. 실제 프레임에서 확인한 것

증거 폴더: [volumetric-audit](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit).

| 항목 | 관찰 | 의미와 한계 |
|---|---|---|
| 효과 OFF | 프레임 이벤트 250개, 해당 위치에 VolmetrickLightPass 없음 | 처음 관찰한 꺼진 상태에는 효과가 계산되지 않았음. 사용자의 과거 ON 비교까지 꺼졌다고 단정하지 않음 |
| 효과 ON | 이벤트 254개, 메인 카메라 Event #202 `Compute VolmetrickLight`, 커널 CSMain, 그룹 122×52×1 | 패스가 실제 실행됨. 숫자만으로 산란/그림자 계산의 모든 픽셀이 옳다고 보증하지 않음 |
| 색/깊이/결과 | 973×409, `_ActiveColor`, `_CameraDepth`, `_Result` 바인딩 | 입력과 결과 자원 존재 |
| 그림자 | `_LightDepth` = `_MainLightShadowmapTexture`, 2048×2048, D16 | white fallback이 아닌 실제 그림자 맵을 받음 |
| 그림자 상수 | `_MainLightShadowParams` ≈ (1, 1, 0.0015255, -3.904239), 캐스케이드 구·행렬 값 비영 | 그림자 강도 0 또는 행렬 전체 0이라는 설명은 배제. 샘플별 visibility가 실제로 얼마나 나뉘는지는 미측정 |
| 실제 주광원 | 색 (1, 0.7038034, 0.4910207), 빛을 향한 방향 (0.524936, 0.7911869, -0.3137925) | 흰색이 아닌 따뜻한 산란광을 계산함 |
| 패스 순서 | 불투명/하늘 → 볼륨 Compute/Copy → 투명 2회 → DOF Bokeh 5회 → Bloom 16회 → 최종 처리 | 기존 안개와 후처리가 볼륨 결과 뒤에 적용됨 |
| 투명 2회 | 각각 Cube, `ProjectL/HeightVolumeFog`, Blend One/OneMinusSrcAlpha, Cull Front, ZWrite Off, ZTest Always | 실제 프레임에서도 별도 높이 안개 두 개가 뒤에 그려짐 |

직접 증거:

- [GPU 자원·광원·그림자 입력](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/frame-compute-bindings.png)
- [GPU 산란 설정](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/frame-compute-parameters.png)
- [그림자 행렬](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/frame-shadow-matrices.png)
- [ON 전체 화면](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/unity-volumetric-on.png), [OFF 전체 화면](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/unity-volumetric-off.png)

ON/OFF는 같은 뷰에서 해당 기능만 바꾼 육안 비교다. 기존 안개 노이즈는 시간에 따라 움직이므로 프레임 간 픽셀 차이를 전부 이 기능의 차이로 해석하지 않는다.

## 4. 배치·광원·카메라

[씬 배치도 SVG](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/scene-layout.svg). 원/점은 루트 위치이며 실제 메시 외곽선이나 그림자 영역이 아니다.

### 4.1 카메라와 계산 거리 — 거리 제한은 주원인 근거에서 제외

Main Camera는 직교, 회전 (45°,45°,0°), 크기 15.6, Near 0.35, Far 56.7이다. 실제 위치는 (-18.67,25.89,-18.57), 전방은 약 (0.5,-0.7071,0.5)이다. 저장된 카메라의 근거는 [씬 카메라](C:/Users/User/Project_L/Assets/Scenes/JMHTest/JMHTest.unity:528), 실제 위치는 GPU 캡처다.

- Near→Far = **56.35**, 계산 범위 = **60**. 현재 직교 카메라의 클리핑 범위 내 지점은 이 범위 때문에 잘리지 않는다.
- 화면 중심 광선의 Y=0 교차점 ≈ (-0.363,0,-0.263).
- 카메라→교차점 ≈ 36.61, Near 이후 적분 경로 ≈ **36.26**.
- 별도 RenderTexture Camera도 같은 렌즈를 쓰지만 레이어 0/22/30/31만 보며 건물·안개가 있는 21번 레이어를 제외한다. Post Processing도 꺼져 있다. 따라서 그 타깃 영상과 Main Camera 영상을 같은 비교 대상으로 삼으면 안 된다. 두 카메라는 Base이며 Stack 조합이 아니다.

Compute는 실제로 Near 위치부터 깊이 위치까지 복원해 `min(length(rayDir), LightingRange)`로 자른다. [거리 계산 코드](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/VolumetricLight/VolmetrickLight.compute:149).

### 4.2 건물 배치와 햇빛 — 참조 이미지의 가림 구조와 다름

| 배치도 | 프리팹 루트 | 저장된 월드 위치 X,Y,Z | 씬 행 |
|---|---|---|---|
| B1 | Bar_ver1_final (2) | -25.26, 0.29, 12.31 | 4446 |
| B2 | Bar_ver1_final | -18.43, 0.16, 23.97 | 8373 |
| B3 | Bar_ver2_final (2) | 25.57, 0.15, 16.57 | 11268 |
| B4 | Bar_ver2_final (1) | 4.63, 1.20, 25.51 | 15865 |
| B5 | Bar_ver1_final (1) | -20.90, 0.29, -23.90 | 17511 |
| B6 | Bar_ver1_final (3) | -30.75, 0.29, -21.68 | 20370 |
| B7 | Bar_ver2_final | 26.76, 0.15, -8.19 | 23211 |

에디터 화면과 배치 목록에서 중앙 광장 위를 덮는 격자 천장은 확인되지 않았다. 주변 건물·바위·울타리는 존재하고 바닥 그림자도 화면에 보인다. **“가리는 물체가 전혀 없다”는 뜻은 아니다.**

주광원의 실제 진행 방향은 (-0.52494,-0.79119,0.31379)이고 지평선 기준 고도는 **52.30°**다. 높이 h의 장애물이 수평 지면에 만드는 그림자의 수평 변위는 약 **0.773h**다. 예를 들어 높이 10이면 약 7.73이다. 낮게 깔린 햇빛이 광장 전체를 길게 횡단하는 조건으로 볼 수 없다. 이는 방향에서 나온 기하 계산이며 실제 메시의 그림자 면적을 측정한 값은 아니다. [주광원](C:/Users/User/Project_L/Assets/Scenes/JMHTest/JMHTest.unity:15126).

참조는 상부 격자가 빛을 반복적으로 가르고, 그 아래 공기층의 명암을 보는 구도다. 현재 장면의 열린 중앙에서 같은 무늬가 생긴다고 기대할 근거는 부족하다. 반대로 주변 건물 가장자리의 국소 줄기까지 안 보이는 이유를 이 배치만으로 확정할 수도 없다.

### 4.3 추가 조명과 위상 함수

저장된 광원은 주광원 외에 아래 세 개가 있다.

| 이름 | 실제 종류/세기 | 그림자 | 현재 볼륨 계산 |
|---|---|---|---|
| Point Light (10) | 이름과 달리 Directional / 0.1, 청록 | 없음 | 제외 |
| Point Light (11) | Point / 10, Range 10, 따뜻한 색 | 없음 | 제외 |
| Point Light (21) | Point / 1, Range 34.4, 색온도 3342 사용 | 없음 | 제외 |

표면은 이들 조명과 환경광의 영향을 받을 수 있지만 새 볼륨은 메인 라이트 하나만 산란시킨다. 바닥 밝기와 공기 산란 밝기가 모든 광원에 대해 함께 증가하는 구현이 아니다. [메인 라이트만 전달하는 부분](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/VolumetricLight/VolmetrickLightFeature.cs:88).

현재 Anisotropy=0이라 위상값은 방향과 무관한 1/(4π)이다. 따라서 현재는 시선과 햇빛의 각도 때문에 위상값이 추가로 약해지는 상태가 아니다. 과거 g=0.9였을 때와 구분한다. 현재 시선과 빛을 향하는 방향의 각도는 약 117°지만 g=0에서는 가시성 차이를 만들지 않는다.

## 5. 안개는 세 경로가 겹침

### 5.1 기존 표면 Fog

씬 RenderSettings는 Fog ON, Exponential Squared, Density=0.016, 따뜻한 색 (1,0.82854,0.55189)이다. [설정](C:/Users/User/Project_L/Assets/Scenes/JMHTest/JMHTest.unity:17).

바위 재질이 쓰는 Cartoon 셰이더는 실제로 `multi_compile_fog`와 `MixFog`를 사용한다. 따라서 이 경로는 설정만 존재하는 것이 아니다. 단, 모든 FBX 재질의 실제 셰이더 변형을 GPU에서 개별 대조한 것은 아니다. [표면 Fog 합성](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/Shader/Core/Includes/Stylized/ProjectLStylizedPasses.hlsl:99).

### 5.2 기존 높이 안개 Cube 두 개

| 대상 | 위치 | 스케일 | 월드 Y 범위 | XZ 범위 |
|---|---|---|---|---|
| Cube | (12.1,-2.2,0) | (100,5,100) | -4.7~0.3 | X -37.9~62.1 / Z -50~50 |
| Cube (1) | (0,-0.9,0) | (100,5,100) | -3.4~1.6 | X/Z -50~50 |

기본 Cube 메시 사용을 확인했다. 두 개 모두 `ProjectL_HeightVolumeDayFog.mat`, Density=12, StepCount=64를 사용한다. 이 Density의 공간 분포·색 alpha·높이/노이즈 마스크는 새 볼륨과 다르므로 12/0.006을 단순 비교하면 안 된다. [재질](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/Shader/View/CustomShader/PostProcessing/VolumetricFog/ProjectL_HeightVolumeDayFog.mat:36).

이 셰이더는 높이·노이즈·중앙 마스크에서 밀도를 만들고 `_FogColor`와 `_FogTopColor`를 보간한다. 주광원이나 그림자 맵을 샘플링하지 않는다. 그 안개의 밝고 어두움은 햇빛 가림과 연결되지 않는다. [안개 색 적분](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/Shader/View/CustomShader/PostProcessing/VolumetricFog/VolumetricFog.shader:543).

새 기능은 **BeforeRenderingTransparents**에 실행되고 기존 높이 안개는 Transparent로 뒤에 그려진다. 안개의 합성이 `Cfinal = Cfog + (1-Afog) Cbefore`이므로 앞선 볼륨 ON/OFF 차이는 두 안개를 지나며 **(1-A1)(1-A2)**배가 된다. 이 억제 경로는 코드와 프레임 순서로 확인된다. [새 기능 순서](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/VolumetricLight/VolmetrickLightFeature.cs:211), [안개 합성](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/Shader/View/CustomShader/PostProcessing/VolumetricFog/VolumetricFog.shader:66).

하지만 중앙 마스크 Radius=0/Softness=0.35, 높이와 색 alpha 때문에 **모든 픽셀에서 두 안개가 불투명하게 덮는 것은 아니다.** Frame Debugger에서 투명 패스 전후를 보았을 때 중앙이 완전히 덮이는 결과를 입증하지 못했다. 안개가 영향받는다는 사실과 주원인이라는 주장은 구분한다.

추가 발견: 기존 높이 안개는 `normalize(input.positionWS-cameraWS)`로 모든 광선을 카메라 위치에서 출발시킨다. 현재 Main Camera는 직교라서 올바른 픽셀 광선은 평행하다. 이 부분은 직교 투영과 맞지 않는 계산이며 안개 경로 길이를 왜곡할 수 있다. **별도 기존 셰이더의 문제로 기록하며 이번에 수정하지 않았다.** 이것이 줄기 소실의 주원인인지는 미검증이다. [광선 생성](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/Shader/View/CustomShader/PostProcessing/VolumetricFog/VolumetricFog.shader:399).

### 5.3 새 볼륨 자체의 대비

현재 PC 설정은 Density=0.006, HeightFalloff=0.02, BaseHeight=0, StartDistance=0, Albedo=1, Intensity=12, g=0이다. 실제 GPU 캡처와 대조했다.

중심 광선에서 Y=0까지의 높이 지수 밀도를 연속 적분하면 optical depth≈0.1702, 투과율≈0.8435, 불투명도≈**15.65%**다. 실제 레이마칭은 60개 샘플과 지터를 쓰므로 이 값은 분석용 근사다. 실제 표면 높이/깊이가 Y=0과 다르면 경로도 달라진다.

그림자가 전혀 없는 경로에서는 현재 산란 모델이 아래와 같이 정리된다.

```text
incoming ≈ 실제 주광원 색 × 12/(4π)
         ≈ (0.95493, 0.67208, 0.46889)
after  = T × before + (1-T) × incoming
change = (1-T) × (incoming-before)
```

따라서 **이미 비슷한 따뜻한 밝기의 배경에서는 밀도가 존재해도 차이가 작을 수 있다.** 밝다는 이유만으로 항상 안 보인다는 뜻은 아니다. 경로 안의 그림자 비율이 서로 달라야 공간적으로 구분되는 줄기가 생긴다. 추가 조사에서는 경로별 visibility를 흑백으로 분리해 중앙과 가장자리 분포를 확인했다(§9). HDR 픽셀 수치의 직접 readback은 하지 않았다. [적분 및 최종 합성](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/VolumetricLight/VolmetrickLight.compute:166).

## 6. 후처리와 렌더 설정

- Main Camera의 CinemachineBrain은 실제로 Day Camera를 가리킨다. Day Camera의 Volume 설정은 `Assets/Settings/Volume/DayCamera.asset`을 참조한다. 저장된 연결은 씬 14291행이다.
- 그 프로파일의 활성 Bloom은 Intensity=2.99, Threshold=0.9, Scatter=0.7이다. LiftGammaGain의 Gamma W≈0.1643, Gain W≈0.4501도 있다. DOF는 Bokeh, FocusDistance=1.2, Aperture=9.4, FocalLength=58이다. [Bloom](C:/Users/User/Project_L/Assets/Settings/Volume/DayCamera.asset:358), [명암 보정](C:/Users/User/Project_L/Assets/Settings/Volume/DayCamera.asset:13), [DOF](C:/Users/User/Project_L/Assets/Settings/Volume/DayCamera.asset:788).
- 실제 프레임에 DOF/Bloom 패스가 존재하고 최종 화면은 처리 전 색보다 밝고 흐려진다. 줄기의 경계·명암 대비를 약하게 할 후보지만, 이들만 끈 비교와 최종 Volume Stack 가중치는 측정하지 않아 프로파일 값이 그대로 100% 적용된다고 단정하지 않는다.
- Day 프로파일의 PostExposure=0, Tonemapping=None이다. 따라서 확인 없이 “ACES나 노출 증가 때문에 날아갔다”는 설명을 붙이지 않는다.
- 현재 PC Pipeline은 Depth/Opaque Texture/HDR ON, 주광원 그림자 2048, 4 Cascade, ShadowDistance=100이다. **그림자 지원 설정이 꺼진 문제는 발견하지 못했다.** [PC Pipeline](C:/Users/User/Project_L/Assets/Settings/PC_RPAsset.asset:44).
- GPU의 실제 그림자 fade 상수는 약 거리 50.6부터 fade가 시작되는 값이다. 중심 지면 거리 약 36.6에서는 이 fade가 0이다. 중심 줄기의 부재를 shadow distance fade만으로 설명할 근거도 없다. 좌우 가장자리는 별도로 확인해야 한다.

## 7. 별도 실행 조건 — 활성화 미연결

앞선 ON/OFF 조사에서는 기능 제목과 내부 `Is Add Render Pass`가 꺼진 상태도 관찰했다. 이 상태에는 효과가 없다. 그러나 둘 다 켠 프레임에도 줄기보다 전체 haze 변화가 주로 나타났으므로 **이 체크 하나만으로 사용자 제보 전체가 해결되지는 않는다.** 이번 추가 분리 조사의 시작/종료 라이브 상태는 둘 다 켜짐이다.

운영 초기화는 관리 대상 Feature의 내부 플래그를 false로 만들며, 볼륨 Scope를 획득하는 소비자는 아직 없다. 이는 기존 계획 Phase 2의 미완료 사항이다. [초기화](C:/Users/User/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/Core/RenderFeatureSystem/RenderFeatureSystem.cs:159), [패스 등록 조건](C:/Users/User/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Core/BaseRenderFeature.cs:28).

## 8. 후속 구현 판단

추가 진단으로 **산란 무늬는 생성되지만 중앙의 차광 대비가 부족하고, 최종 화면에서 무늬가 약해지는 것**을 확인했다. 추가 밀도 튜닝보다 아래 설계 판단을 우선한다.

1. 현재 중앙을 유지하면서 참조 같은 줄기를 원하면 공중 차광 패턴을 어디서 만들지 정한다. 실제 차광물/광원 각도 변경과 아트용 마스크·국소 볼륨은 서로 다른 선택이며, 이번 조사에서 임의로 도입하지 않았다.
2. 후처리 묶음의 변화는 확인했지만 Bloom/DOF/색보정 각각의 기여율은 분리하지 않았다. 주원인을 이들 중 하나로 단정하지 않는다. 배치와 산란 대비를 정한 뒤 항목별 튜닝을 한다.
3. 표면·공중 그림자가 실제로 출력되므로 무조건 깊이 복원/그림자 경로를 다시 작성할 근거는 없다. 모든 캐스케이드와 투영 모드의 정확성까지 보증하는 결과는 아니다.
4. 계산은 정상인데 참조와 같은 결과가 목표라면, 주광원 그림자를 읽는 매질과 기존 보이는 높이 안개의 관계를 먼저 설계한다. 밀도·매질을 독립적으로 두 벌 소유하면 연출과 조절이 계속 어긋난다. 안개 통합/책임 분리는 별도 설계·Task·Phase 대상으로 한다.
5. 운영 Scope 연결은 가시성 계산 수정과 구분해 기존 Phase 2에서 진행한다. Core·FieldCamera·Architecture 수정이나 공용 초기화 우회는 필요 근거가 없다.

이번에는 조사만 수행했다. 임시 진단 출력을 복구했으므로 제품 코드에 새 진단 모드가 남아 있지 않다. 안개 통합도 구현하지 않았다. SSOT 관점의 매질 중복과 조립/계산 책임 분리는 위의 후속 설계 쟁점이며, 이번 조사로 FW/SOLID 계약이나 기존 서비스 수명은 변경하지 않았다.

## 9. 추가 분리 진단 결과 — 2026-11-07

### 9.1 방법

동일한 라이브 Main Camera에서 기존 Compute의 최종 출력만 순차 교체했다. 밀도·광원·배치·카메라·안개·후처리 설정은 바꾸지 않았다. 에디터 정지 상태의 자동 임포트로 반영했고 Play·명시적 재컴파일·테스트는 실행하지 않았다. 후처리 전 진단은 Frame Debugger Event #203(Compute 결과의 Copy), 높이 안개 두 개 합성 후는 #205, 전체 후처리까지는 #254 또는 Frame Debugger 비활성 상태에서 관찰했다. 이때 타깃은 961×409였다(이전 조사 973×409와 구분).

진단 출력은 아래 세 가지다.

- 산란광만: `S`. 배경색 `C*T`를 제외해 그림자 무늬가 실제로 생성되는지 확인한다.
- 경로별 가중 visibility: `S.r / (incoming.r * albedo * (1-T))`. 현재 주광원·albedo가 경로에서 일정하므로 산란 기여로 가중한 평균 visibility가 된다. 흰색은 거의 비가림, 회색/검정은 더 많이 가린 경로다. 단순 샘플 개수 비율과는 다르다.
- 표면 visibility: `GetShadowAttenuation(rayEnd)`. 깊이로 복원한 표면 위치에서 같은 그림자 함수가 무엇을 반환하는지 확인한다.

### 9.2 관찰과 원인 우선순위

| 분리 비교 | 실제 관찰 | 판단 |
|---|---|---|
| 산란광만, #203 | 배경이 없어도 왼쪽에 넓은 사선 명암 띠가 보임. 중앙은 대체로 고름 | 그림자에 따른 산란 무늬가 실제로 생성됨. 전체 계산 불작동 가설 제외 |
| 가중 visibility, #203 | 중앙 광장은 거의 흰색. 왼쪽 일부에 넓은 회색 띠와 주변 물체의 경로 차이가 보임 | 중앙에 반복되는 빛/그림자 줄기를 만들 공간 변화가 부족함. 배치 원인이 우선 |
| 표면 visibility, #203 | 주변 건물/소품에 대응하는 검은 영역과 흰 중앙 지면이 보임 | 실제 맵 샘플·좌표 변환·깊이 복원이 관찰 구간에서 의미 있는 결과를 냄. 모든 픽셀의 정답 검증은 아님 |
| 산란광 #203→#205 | 사선 띠가 남음. 화면 우상단의 주황 안개 등 국소 변화가 추가됨 | 기존 높이 안개가 중앙 줄기를 전부 덮는다는 설명은 지지하지 않음 |
| 산란광만→원래 `C*T+S`, 모두 #203 | 분리 화면의 띠가 배경과 합치면 뚜렷하게 읽히지 않음 | 후처리 이전부터 배경 대비 문제가 존재함. 후처리만 탓할 수 없음 |
| #205→최종 #254 | 최종은 더 밝고 흐려지고 색도 변함. 분리 S의 띠는 최종에도 일부 남음 | 후처리는 추가 대비/경계 변화 요인. 단독 원인이나 완전 소실은 아님 |

**종합:** 열린 광장·높은 태양 → 중앙의 가중 visibility가 거의 일정 → 새 산란은 중앙을 고르게 채움 → 주변의 약한 줄기 무늬는 밝고 따뜻한 배경과 합치면서 읽기 어려움 → 후처리가 표시를 더 바꿈. 이 설명이 현재 실제 진단과 가장 일치한다. “거리 때문에 당연히 안 보인다”, “그림자 계산이 꺼졌다”, “높이 안개가 전부 가렸다”는 설명보다 근거가 강하다.

### 9.3 추가 기하 대조와 한계

Bar1/Bar2 FBX의 노드 변환·GeometricTranslation·cm 단위를 적용한 모델 경계를 계산한 뒤 저장된 건물 루트 7개의 변환을 적용했다. Unity FBX 축 변환의 X 반사 두 가능성을 합친 보수적 AABB를 사용했다. 중앙 카메라 광선에서 지면까지 61개 지점 각각에서 실제 GPU의 빛 방향으로 광선을 쏘았을 때 이 7개 상자와의 교차는 0이었다. 중앙 visibility가 거의 흰 GPU 관찰과 방향이 같다.

이는 모든 소품·미저장 배치·정확한 임포트 메시 삼각형까지 계산한 결과가 아니다. AABB 가정이 적용되는 7개 건물의 중앙 경로 근거이며, 실제 그림자 전체를 대체하지 않는다. [수치와 가정 JSON](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/conservative-building-bounds.json).

### 9.4 직접 증거와 복구

아래 이미지는 원본 UI 캡처이며 Game 뷰는 왼쪽 아래다. 별도의 색/명암 확대를 하지 않았다. 픽셀 readback이나 정량 기여율 측정으로 해석하지 않는다.

- [산란광만·안개 전](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/scattering-before-fog.jpg), [높이 안개 후](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/scattering-after-fog.jpg), [최종 후처리 후](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/scattering-after-post.jpg)
- [경로별 가중 visibility](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/weighted-visibility-before-fog.jpg), [표면 그림자](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/surface-shadow-before-fog.jpg)
- [원래 합성·안개 전](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/normal-before-fog.jpg), [높이 안개 후](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/normal-after-fog.jpg), [복구한 최종 화면](C:/Users/User/.codex/visualizations/2026/11/07/01a1b481-69e2-7723-be43-56430b69db99/volumetric-audit/isolated/restored-normal-final.jpg)

Compute 원본 해시와 PC Renderer 바이트 일치를 확인하고, 라이브 활성 체크 둘 다 켜짐을 보존한 채 Frame Debugger를 비활성화/닫았다. 기존 미저장 씬은 저장·폐기하지 않았다. 제품 코드 영구 변경 없음. **컴파일 미확인.**
