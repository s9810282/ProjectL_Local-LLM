# 셰이더 유틸리티와 Pass 조립

2026-10-14 사용자 요청으로 만든 `Shader/Core` 폴더를 채운다. 이 Core는 렌더 리소스이며 게임의 CoreSystem을 변경하지 않는다. `Lightng`, `Maske` 등 사용자가 만든 폴더 이름은 유지한다.

## 설계

- `Includes/Lightng`: 현재 BaseColor 툰 명암, 면 노멀, 기존 스펙큘러·림 계산. 계산 함수는 머티리얼 전역값 대신 인자로 값을 받는다.
- `Includes/Maske`: 선형 밴드, 노이즈, 디졸브의 유지/테두리 마스크 계산. 실제 `clip`은 Pass에서 호출한다.
- `Includes/Shadow`: 그림자를 받는 좌표와 그림자 맵에 그리는 바이어스 좌표 계산. URP의 그림자 처리를 재구현하지 않고 제공 함수에 연결한다.
- 공통 입력: 모든 새 Pass가 동일한 `UnityPerMaterial` 레이아웃과 정점 입력을 사용한다. SRP Batcher의 Pass별 상수 버퍼 불일치를 방지한다. Hidden Pass 라이브러리에도 같은 머티리얼 속성을 선언한다. UsePass가 속성 선언까지 상속하는 것은 아니므로 View에도 선언이 필요하다.
- `Passes/LightingPass`: Forward 정점/픽셀 함수와 기본·디졸브 Forward Pass.
- `Passes/ShadowPass`: 기본·디졸브 ShadowCaster Pass.
- `Passes/URPPass`: 기본·디졸브 DepthOnly, DepthNormals, NormalOnly, Meta Pass.
- `View`: Properties와 UsePass로 조립하는 새 기본/디졸브 예제. 기존 shader 이름/GUID와 씬 머티리얼 참조를 교체하지 않는다.

SSOT: 계산은 Includes, 정점/픽셀 처리 흐름은 Pass HLSL, GPU 상태·키워드는 Pass shader가 소유한다. 함수 호출자와 렌더 상태를 분리한다. 새로운 Framework 서비스·DB·Scope 수명 계약은 만들지 않아 FW-001~016의 경계를 변경하지 않는다.

## Task 계획과 Phase

### Phase 1 — 라이브러리와 조립 예제

1. 위 역할별 HLSL과 `.meta`를 생성한다.
2. 재사용 가능한 Hidden Pass shader 세 개를 생성한다.
3. View의 기본·디졸브 예제에서 UsePass로 조립한다.
4. 현재 기본 셰이더의 이동으로 끊어진 툰 include만 공용 유틸리티로 연결한다.
5. D3D11 각 Pass/주요 변형 컴파일, 동일 CBUFFER·Pass 이름·GUID 확인, 가능하면 Unity import/조립 렌더 검증.
6. 결과를 보고하고 리뷰 지점에서 멈춘다.

### Phase 2 — 기존 사용처 전환 (이번 범위 밖)

Phase 1 검토 뒤 사용자가 지시하면 기존 셰이더를 조립 구조로 바꾸거나 씬 머티리얼을 새 View 셰이더로 전환한다. `View/NotUse`의 에셋은 삭제하지 않는다.

## 검증 기록

Phase 1 완료. 기존 씬 머티리얼 전환은 Phase 2의 리뷰 지점으로 남긴다.

- D3D11 정점/픽셀과 Forward+·추가광·SSAO·쿠키·인스턴싱·점광원 그림자·옥타헤드럴 노멀/Rendering Layers의 36개 컴파일 조합 통과.
- View 셰이더 두 개의 UsePass 12개가 정확한 Hidden shader/Pass 이름을 참조함을 확인.
- Unity에서 Hidden 라이브러리의 Pass 12개와 View 조립 결과의 Pass 12개를 동기 컴파일: 총 24개 모두 성공.
- Hidden 라이브러리 3개와 View 셰이더 2개 모두 Unity의 SRP Batcher 호환 코드 0(OK).
- Unity Inspector에서 `ToonLitPreview`의 두 단계 명암과 `ToonLitDissolvePreview`의 구멍·발광 테두리 렌더 확인. Console 오류/경고 0.
- 임시 Unity 진단 코드는 확인 후 제거했다. 보고서와 외부 컴파일 산출물은 Git에서 제외된 `Library/ToonVerification`에 보관한다.
- 다른 그래픽 API 및 실제 씬의 새 머티리얼 그림자/Depth 회귀는 Phase 2에서 확인한다.

### 2026-10-14 rock_002 단계 명암 오류 수정

사용자가 보고한 다리의 점무늬를 실제 `ShaderTest` 씬의 `rock_002`로 재현했다. 대상은 `rock_test_002zz.fbx`의 메시이며 `ToonLitPreview`/`ProjectL/View/ToonLit`을 사용한다. 디스크 값과 에디터 값이 달랐으므로 에디터에 로드된 값을 기준으로 비교했다(`Flat Diffuse Normals = 1`).

- 그림자 끄기, 스펙큘러/림 끄기, 환경광 끄기 모두 점무늬가 남았다. 초기 자기 그림자 오류 추정은 틀렸다.
- 면 노멀을 메시 노멀로 대체하면 점무늬가 사라졌다. float 정밀도만 사용한 미분 면 노멀에서도 재현됐다.
- float RenderTexture에서 원래 0.5 부근인 내적값을 읽어 `0.499952435~0.5000252`의 흔들림을 확인했다. `Step(0.5, NdotL)`이 같은 면을 두 밝기로 나누는 원인이었다.
- `Softness = 0`일 때만 내적값을 `count * 1024` 격자에 반올림해 비교한다. 각 단계 경계가 격자점에 놓여 작은 미분 오차가 경계를 왕복하지 않게 한다. 최대 경계 이동은 내적값 기준 약 0.000244이며 중간 밝기는 추가하지 않는다. `Softness > 0`의 Smoothstep 계산은 그대로 유지한다.
- 노멀/내적/단계 판정은 float로 유지하고, 머티리얼 CBUFFER와 Properties 선언은 바꾸지 않았다. 기존 Base 셰이더도 같은 공용 함수를 참조하므로 이 보정을 사용한다.
- 수정 후 실제 ToonLit의 확산광 단독 및 그림자·스펙큘러·림 포함 렌더에서 다리 점무늬 제거 확인. D3D11 36개 조합 재컴파일 통과.
- 비교 동안 원본 머티리얼과 광원 상태를 복원했고 씬 dirty 상태는 전후 모두 false였다. 임시 진단 코드/셰이더는 제거했다. 비교 PNG와 원시값 보고서는 Git 제외 경로 `Library/ToonVerification/LightingAudit`에 보관한다.

## 사용

2026-10-15 후속 사용자 요구로 새 Forward는 삼각형 미분 노멀 대신 모델의 분리 노멀을 사용한다. `_FlatDiffuseNormals`는 비표시 호환 필드이며 새 Forward에 영향이 없다. Mark Sharp 경계/명암 대비 변경과 검증은 [sharp-normal-lighting](sharp-normal-lighting.md)을 따른다. 아래 2026-10-14 기록은 당시 구현과 오류 수정 이력이다.

`View/ToonLit.shader`와 `View/ToonLitDissolve.shader`가 Properties와 UsePass만으로 조립하는 예제다. `ToonLitPreview.mat`와 `ToonLitDissolvePreview.mat`를 선택해 렌더 결과와 Inspector 속성을 확인할 수 있다.

| 위치 | 내용 |
|---|---|
| Includes/Lightng/ToonLighting.hlsl | 단계 명암과 기존 Base 셰이더용 삼각형 노멀 유틸리티 |
| Includes/Lightng/LitLighting.hlsl | 기존 스펙큘러·림과 주광원/추가광의 공용 직접광 계산 |
| Includes/Maske/MaskUtility.hlsl | 중앙은 1, 양 외곽은 각각 선형 페이드하는 밴드 마스크와 노이즈 마스크 |
| Includes/Maske/DissolveUtility.hlsl | 디졸브 유지 마스크와 테두리 마스크 |
| Includes/Shadow/ShadowUtility.hlsl | 그림자 수신 좌표와 광원별 ShadowCaster 바이어스 좌표 |
| Includes/ProjectLInput.hlsl | 텍스처·UnityPerMaterial·정점 입력의 공용 계약 |
| Passes/SurfaceInput.hlsl | 텍스처 읽기와 디졸브 clip의 공용 호출 |
| Passes/LightingPass | 기본/디졸브 Forward 정의와 실행 함수 |
| Passes/ShadowPass | 기본/디졸브 ShadowCaster 정의와 실행 함수 |
| Passes/URPPass | 기본/디졸브 DepthOnly·DepthNormals·NormalOnly·Meta 정의와 실행 함수 |

UsePass 예시:

```shaderlab
UsePass "Hidden/ProjectL/Core/LightingPass/FORWARD"
UsePass "Hidden/ProjectL/Core/ShadowPass/SHADOWCASTER"
UsePass "Hidden/ProjectL/Core/URPPass/DEPTHONLY"
```

디졸브용은 Pass 이름 앞에 `DISSOLVE_`를 붙인다. CPU에서 디졸브를 조절할 때는 `_DissolveAmount`를 0~1로 설정한다. 0은 모두 유지, 1은 모두 제거한다. Preview 머티리얼의 노이즈는 이동된 기존 `View/NotUse/Test/New Folder/Untitled2.png`를 GUID로 참조한다.

현재 이동된 기존 두 Base 셰이더는 조립 구조로 변환하지 않았다. 존재하지 않는 `ToonDiffuse.hlsl` include만 새 `Core/Includes/Lightng/ToonLighting.hlsl`로 연결해 기존 참조를 복구했다. 기존 파일/폴더 GUID와 shader 이름은 유지한다.
