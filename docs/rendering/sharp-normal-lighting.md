# Sharp 경계 기준 툰 라이팅

2026-10-15 최종 사용자 요구: 모델의 Mark Sharp 경계에서 라이팅 대비를 강하게 한다. 원래 Quad/N-gon별로 평평하게 만드는 방식은 취소했다.

## 설계와 Task

1. FBX가 가진 분리 노멀을 월드 공간으로 변환해 사용한다. Smooth 영역의 보간은 유지하고, Sharp 경계는 분리된 노멀대로 계산한다. 기본/디졸브 Forward에서 `ddx/ddy`로 삼각형 노멀을 재계산하지 않는다.
2. `_FlatDiffuseNormals`는 기존 Properties/CBUFFER 호환용으로만 남기고 Inspector에서 숨긴다. 새 Forward는 이 값에 영향받지 않는다. 기존 Base 셰이더의 명시적 삼각형 노멀 유틸리티는 삭제하지 않는다.
3. rock_002의 원본 모델을 유지한다. 준비만 했던 폴리곤별 노멀 복사본과 베이크 도구는 제거한다.
4. 현재 테스트 머티리얼의 단계 수를 2, 경계 부드러움을 0, 어두운 밝기를 0.05, 환경광 강도를 0.05로 설정해 대비를 확인한다. 다른 색/Metallic/Smoothness/스펙큘러/림 값은 유지한다.
5. 실제 Unity 렌더와 기본/디졸브 컴파일을 확인하고 Phase 1 리뷰 지점에서 보고한다.

노멀 경계는 입력 메시가 소유하고 명암 계산은 기존 공용 함수가 소유한다. 새로운 런타임 서비스/DB나 CoreSystem 변경은 없다(SSOT/SOLID/FW 경계 유지).

Mark Sharp 태그 자체를 셰이더에서 읽는 것은 아니다. 모델링 툴이 그 경계를 코너별 분리 노멀로 내보내야 하고 Unity의 Normals는 Import를 사용해야 한다. 양쪽 노멀이 빛을 같은 각도로 받거나 같은 툰 구간에 들어가면 같은 밝기가 될 수 있다. 모든 Sharp 경계를 반드시 다른 색으로 만드는 방식은 광원 기반 라이팅과 별도 정책이다.

## Phase 1 결과

- 새 기본/디졸브 Forward는 모델의 월드 노멀을 확산광에 직접 사용한다. 삼각형 재계산 옵션은 비표시 호환 필드로 남긴다.
- `rock_002`는 원래 `rock_test_002zz.fbx`와 `ToonLitPreview`를 유지한다. 모델 복사본은 적용하지 않았고 Assets에서 제거했다. 취소된 복사본은 Git 제외 경로 `Library/ToonVerification/CancelledPlaneLighting`에 보관하며 베이크 도구/설계는 제거했다.
- 에디터의 변경 전 실제 값은 Steps 4.18, Dark Brightness 0.709, Ambient Strength 0.661이었다. 테스트 머티리얼을 Steps 2 / Softness 0 / Dark Brightness 0.05 / Ambient Strength 0.05로 변경했다. Color·Metallic·Smoothness·스펙큘러·림 값은 유지했다. 다른 머티리얼의 명암 값은 자동으로 바꾸지 않는다.
- Unity의 전후 렌더를 확인했다. D3D11 36개 조합, Unity 동기 Pass 컴파일 24개가 모두 통과했고 셰이더 5개 모두 SRP Batcher 호환 코드 0이었다.
- 씬은 작업 전부터 미저장 상태였다. 전후 모두 dirty=true를 유지했고 씬을 저장하지 않았다. 테스트 머티리얼 에셋만 저장했으며 Undo 기록을 남겼다.
- 보고서/비교 이미지는 `Library/ToonVerification/SharpLighting`에 보관한다. 임시 에디터 진단 코드는 제거했다.

## 후속: 노멀 내적 배율

2026-10-15 사용자가 식을 명시했다: `dot(노멀, Directional Light 방향) * 배율`.

- 계산: `saturate(dot(N, L) * max(multiplier, 0))`를 툰 단계 판정 전에 적용한다. 주광원과 추가광은 같은 공용 함수를 사용한다.
- `_DiffuseNormalMultiplier` / Inspector `Normal Light Multiplier`(0~8)를 추가한다. 기본값 1은 기존 계산을 유지한다. 테스트 재질은 2로 비교한다.
- 배율은 확산광의 각도 반응에만 적용한다. 스펙큘러·림·환경광·광원 색/강도·그림자 감쇠는 기존 계산을 유지한다.
- Shared CBUFFER와 Hidden/View Properties 5개에 같은 필드를 선언한다. 별도 DB/서비스나 모델 수정은 없다.
- 내적값 0.2와 0.4는 배율 2에서 0.4와 0.8이므로, 2단계의 기준 0.5 양쪽으로 분리된다. 포화된 값이나 같은 툰 구간의 값은 같은 밝기가 될 수 있다.

Task: 공용 계산/입력/속성 추가 → D3D11 컴파일 → Unity에서 배율 1/2 렌더 비교 및 24개 Pass/SRP 호환 확인 → 임시 코드 제거 후 보고.

결과:

- 배율 프로퍼티/공용 계산을 구현했다. Shader 기본값은 1이며 `ToonLitPreview`에만 2를 저장했다. 에디터에서 사용자가 변경한 다른 값(Steps 6, Ambient 0 등)은 유지했다.
- 비교 시 rock_002의 재질은 사용자가 `URP/Lit`으로 바꾼 `ToonLitDissolvePreview`였다. 렌더 비교 동안만 ToonLit 복사 재질을 적용하고 기존 재질을 복원했다. 오브젝트의 실제 재질을 바꾸거나 씬을 저장하지 않았다.
- 실제 배율 2는 낮은 내적값을 밝게 하지만 일부 상단 면을 포화시켜 같은 밝기로 만들었다. 배율 증가가 모든 면의 대비 증가를 보장하지 않는다는 점을 확인했다. 사용자가 명시한 식을 다른 대비 곡선으로 임의 변경하지 않았다.
- D3D11 36개 조합과 Unity Pass 24개 컴파일 통과. Hidden 3개/View 2개 모두 SRP Batcher 호환 코드 0.
- `Library/ToonVerification/NormalMultiplier`에 배율 1/2 비교 PNG와 보고서를 보관한다. 임시 진단 코드는 제거했다.
