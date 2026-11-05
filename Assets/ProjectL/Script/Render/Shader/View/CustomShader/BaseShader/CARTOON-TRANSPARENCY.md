# Cartoon 표면 타입

2026-11-01 사용자가 확인한 대상: ProjectL/BaseShader/Cartoon.

## 설계와 범위

- Cartoon.shader 머티리얼에서 Opaque / Transparent를 선택한다. 기본은 기존 Opaque다. 기존 머티리얼 에셋은 수정하지 않는다.
- Transparent는 기본 텍스처 알파 × 기본색 알파를 사용한다. 기존 AlphaClip은 두 모드 모두 별도로 유지한다.
- 전용 ShaderGUI 한 곳에서 블렌드·RenderType·렌더 큐·ZWrite와 보조 패스를 설정한다. 투명에서는 그림자·깊이·노멀·반전 헐 외곽선 출력을 끈다. 외곽선 헐이 반투명 물체 내부 전체에 겹쳐 검게 보이는 것을 방지한다. Opaque로 돌아오면 복구한다.
- 공용 Stylized include의 알파 경로는 Cartoon에서만 켜는 define으로 제한한다. HarfRambert와 Composer 생성 셰이더는 변경하지 않는다.
- SSOT/SRP: 머티리얼 표면 타입이 렌더 상태의 원본, ShaderGUI가 상태 적용, 셰이더가 알파 출력을 소유한다. Core/Systems/Factory/FW 등록과 씬은 변경하지 않는다.

## Phase 1

- [x] Cartoon 셰이더 표면 타입·블렌드 상태와 조건부 알파 출력 추가.
- [x] Inspector, 다중 선택, 모드 왕복, 알파 컷아웃 유지 구현.
- [x] Unity 컴파일·렌더 검증 및 결과 기록 후 Phase 종료.

## 검증

Unity 6000.0.73f1에서 구현·검증 완료.

- 실제 Inspector에서 Surface > 표면 타입(Opaque/Transparent) 표시 확인. 다중 선택은 MaterialEditor.targets 각각에 같은 렌더 상태 적용 경로를 사용한다.
- 임시 PreviewScene의 URP SingleCameraRequest로 Alpha 0/0.5/1, 텍스처 0.5 × 기본색 0.5 = 0.25의 실제 픽셀 블렌딩을 확인했다.
- 투명/불투명 양쪽의 알파 컷아웃, 불투명에서 기본색 알파 무시, 모드 왕복의 렌더 큐·ZWrite·보조 패스 복구 검증 통과.
- C# 컴파일 및 셰이더 렌더 오류 0. 결과는 Verification/Cartoon-transparency.txt 및 PNG에 기록한다. 재실행 메뉴: ProjectL > Render > Verify Cartoon Transparency.
- 검증은 새 임시 머티리얼과 미리보기 씬을 사용했다. 사용자 씬·기존 머티리얼을 저장하거나 편집하지 않았다. 플레이어 빌드/실기기 측정은 하지 않았다.
- 처음 잘못 지정했던 Composer Recipe/Generator/Window 변경은 전부 제거했다. Custom/ProjectLBaseLit 및 ToonLit.shader는 변경하지 않았다.
- 커밋/push 없음. Phase 1 종료.

## 사용

Cartoon 머티리얼의 표면 타입을 Transparent로 바꾸고 기본 색의 A(알파)를 낮춘다. 텍스처의 알파도 곱해진다. 기본 Opaque에서는 알파에 따른 반투명이 적용되지 않으며 기존 알파 컷아웃은 따로 쓸 수 있다. 런타임 코드에서 _Surface만 변경하면 블렌드/큐/패스는 자동으로 바뀌지 않는다. 이번 기능은 머티리얼 Inspector에서 설정해 저장하는 사용 범위다.
