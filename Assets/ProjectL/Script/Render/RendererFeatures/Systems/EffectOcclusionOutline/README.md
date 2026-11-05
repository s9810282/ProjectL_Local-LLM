# 이펙트 가림 윤곽선

## 설계와 Phase 1 계획

요청: 지정한 이펙트에 가려진 지정 캐릭터의 바깥 윤곽을 표시한다. Opaque는 알파 컷아웃만, Transparent는 실제 합성 알파를 가림으로 취급한다.

- 독립 URP RenderGraph Renderer Feature. 캐릭터 Layer Mask와 이펙트 Layer Mask는 겹치지 않는다. 둘 다 Nothing이면 비활성 구성이다.
- 이펙트를 제외한 불투명 장면 깊이, 캐릭터 원본 깊이, 확장 메시 깊이를 따로 기록한다. 벽 뒤의 캐릭터를 드러내지 않는다.
- Cartoon의 전용 LightMode 패스가 기존 텍스처·기본색·컷아웃 값을 그대로 사용해 가림 알파를 기록한다. 전용 패스를 지원하지 않는 이펙트 셰이더는 이번 범위에 포함하지 않는다.
- 확장 메시와 원본 깊이의 차이에 가림 알파를 곱해 선을 그린다. 두께는 화면 픽셀 단위 정점 확장으로 조절한다. 두께에 비례하는 주변 텍셀 반복 검사는 없다.
- 일반 투명/화면 왜곡 렌더 뒤, 후처리 앞에 합성한다. Game 기본 카메라와 SceneView를 지원한다. 카메라 스택 Overlay와 Preview는 제외한다.
- 선택 Renderer Data에 기능을 추가하는 에디터 메뉴와 실제 GPU 검증을 제공한다. 기존 씬·머티리얼·Renderer Data는 자동 변경하지 않는다.

Task: (1) Feature와 확장 셰이더 (2) Cartoon 전용 마스크 패스 (3) 설치 메뉴 (4) 임시 씬에서 앞/뒤, 투명/불투명/컷아웃, 벽, 두께, 직교/원근을 검증하고 결과 기록. Phase 1 종료 후 보고한다.

## 경계 검토

설정/렌더 자원 수명은 Feature 한 곳이 소유한다(SSOT). 표면 알파 계산은 Cartoon의 기존 입력을 재사용한다. 렌더 구현과 에디터 조립/검증은 별도 파일·어셈블리다. 선 표현 변경은 이 기능 폴더, 표면 지원 추가는 해당 셰이더의 전용 패스, 설치 변경은 Editor에 머문다. Core, 입력, FieldCamera, 게임 서비스는 변경하지 않는다.

SOLID: Feature는 렌더 수명과 패스 조립, 셰이더는 GPU 표시, Editor는 에셋 조립/검증을 담당한다. 다른 이펙트 셰이더도 동일 LightMode 계약으로 확장 가능하다. 서비스 인터페이스를 불필요하게 추가하지 않는다. FW-001/002의 서비스 등록 대상이 아닌 URP 소유 기능이며, 기존 TransparentScreenCaptureFeature와 같은 생성/해제 경계를 사용한다. 공통 경계는 Cartoon의 기존 알파/컷아웃 입력과 URP Renderer Data뿐이다.

## 사용

1. Project에서 사용할 Universal Renderer Data를 선택하고 `ProjectL > Render > Add Effect Occlusion Outline To Selected Renderer` 실행.
2. 추가된 Feature의 `Character Layers`와 `Effect Layers` 지정. 캐릭터와 이펙트는 서로 다른 GameObject 레이어여야 한다. 카메라/Renderer의 원래 레이어 마스크에도 계속 포함시킨다.
3. Character는 불투명이며 `DepthOnly` 패스를 제공해야 한다. Effect는 `ProjectL/BaseShader/Cartoon`을 사용한다. 이펙트 레이어에 일반 벽을 섞지 않는다.
4. `Width Pixels`, `Outline Color`, `Minimum Opacity`, `Opacity Softness`, `Depth Bias` 조절. 알파가 낮은 연기에는 Minimum Opacity를 낮춘다. 프로젝트 도구로 평균 노멀을 UV3에 베이크한 대상들에만 `Use Baked Normals`를 켠다. 기본은 일반 노멀이다.

RenderGraph를 켠 URP Forward 렌더러가 대상이다. 정점 노멀 기반이라 날카롭게 분리된 저폴리 메시에서 선이 갈라질 수 있다. 겹친 캐릭터는 하나의 바깥 실루엣으로 처리한다. 알파 컷아웃 캐릭터의 잘린 내부 구멍 확장은 지원하지 않는다. 차폐할 불투명 벽도 `DepthOnly` 패스가 필요하다. 이펙트 이외 투명 물체를 벽처럼 차폐하는 기능은 포함하지 않는다. 전용 깊이 3장과 가림 1장을 추가하므로 실제 대상 해상도/규모에서 성능 측정이 필요하다.

## 레이어를 바꾼 뒤 원본 이펙트가 보이지 않을 때

Feature의 Character/Effect Layers는 가림 판정용이다. 원본의 기본 렌더 마스크를 자동으로 바꾸지 않는다. `Transparent Layer Mask`에서 이펙트 레이어가 빠지면 연기는 사라지고 선만 보일 수 있다.

Renderer Data를 선택하고 `ProjectL > Render > Include Outline Layers In Selected Renderer`(Ctrl+Alt+L)를 실행하면 Character/Effect를 Opaque 마스크에, Effect를 Transparent 마스크에 추가하고 해당 Renderer를 저장한다. 기존의 다른 마스크 비트와 편집 중인 씬은 보존한다. 별도 Feature가 재렌더하는 전용 레이어는 그 기능의 설정 계약을 먼저 따른다.

2026-11-01 후속 수정: 현재 `PC_Renderer`의 투명 마스크에서 빠진 `Blending` 레이어를 포함하여 Smoke 원본과 윤곽선이 함께 보이는 것을 현재 씬에서 확인했다.

## Phase 1 결과 — 2026-11-01

구현 완료. 현재 씬과 운영 Renderer Data에는 연결하지 않았다. Cartoon에는 일반 렌더가 자동 호출하지 않는 `ProjectLEffectOcclusion` 마스크 패스만 추가했다.

Unity 6000.0.73f1 / DX11의 실제 URP 렌더 검증 메뉴(`ProjectL > Render > Verify Effect Occlusion Outline`, Ctrl+Alt+O) 통과:

- 직교/원근 카메라에서 이펙트 앞이면 표시, 뒤이면 추가 픽셀 0.
- Transparent 알파 0, Opaque 컷아웃 구멍, 미선택 이펙트 레이어는 추가 픽셀 0.
- Opaque는 기본색 알파가 0이어도 정상 가림.
- 이펙트와 캐릭터 사이 및 이펙트 앞의 불투명 벽은 추가 픽셀 0.
- 구형 대상의 네 사분면에 모두 선 표시. 2px/7px 설정에 따른 윤곽 면적 증가 확인.
- 절반이 투명한 텍스처: 투명한 왼쪽 선 0px, 가려진 오른쪽 선 585px. Opaque 컷아웃도 같은 위치만 표시.
- 기존 Cartoon 투명도 검증(Ctrl+Alt+U)도 재실행 통과. 셰이더 컴파일 오류 없음.

출력은 `Verification/verification.txt`와 같은 폴더의 on/off PNG에 있다. 이 검증은 Unity 에디터 메뉴에서 실행한 GPU 비교이며 전체 NUnit EditMode Test Runner 실행을 뜻하지 않는다. 임시 씬/파이프라인은 검증 종료와 예외 시 해제/복구한다. 검증 도중 Renderer Data 에셋과 기존 씬/머티리얼 값을 저장하지 않는다. 운영 캐릭터의 실제 애니메이션·평균 노멀과 다수 파티클/목표 기기 성능은 씬 연결 후 확인할 범위다.
