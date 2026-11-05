# GUI Shader Composer

2026-10-15 사용자 요청: Node 연결 대신 GUI로 라이트 모델을 먼저 선택하고 효과를 추가한다. 효과마다 사용할 마스크를 선택한 뒤 Shader 파일로 내보낸다.

## 설계

- Unity EditorWindow: `ProjectL > Shader Composer`.
- 조립 순서: 기본 표면(Color/Texture) → 라이트 모델 → 순서가 있는 효과 목록. 각 효과가 자체 마스크 종류와 파라미터를 가진다.
- 첫 모델: Lit(PBR), Toon, Unlit. 기본값/지원 파라미터/호출 함수는 모듈 Catalog가 정의한다.
- 첫 효과: 색상 곱하기, 발광, 림 발광, 디졸브. 마스크: 없음, 텍스처, 노이즈, 높이, 밴드. 반전 가능.
- 효과 추가/삭제/순서 변경과 기본값은 GUI에서 한다. Shader 코드를 직접 편집하거나 Node를 연결할 필요가 없다.
- 현재 프로젝트의 공용 HLSL을 사용하는 `.shader`를 생성한다. URP와 ProjectL Composer HLSL에 의존한다. 독립적인 타 프로젝트용 패키지 내보내기는 후속 범위다.
- 기존 UsePass 라이브러리는 입력 CBUFFER가 고정되어 있어 임의 효과 파라미터 조립에 그대로 사용할 수 없다. 새 결과는 동일한 생성 입력을 모든 Pass에 공유하고 공용 Pass HLSL/계산 함수를 include한다.
- Forward는 모델 라이팅 후 효과를 순서대로 적용한다. 디졸브는 같은 마스크로 Forward/ShadowCaster/Depth/Normals/Meta의 clip을 일치시킨다. Meta는 기본색과 발광을 분리하고 카메라 의존 림은 굽지 않는다.
- 파라미터 정의에서 Properties·CBUFFER·GUI를 생성해 선언 불일치를 막는다. 효과 ID는 순서를 바꿔도 유지하여 파라미터 이름을 안정적으로 만든다.
- 결과 파일에 레시피를 주석으로 넣어 다시 GUI로 열 수 있게 한다. 입력 이름/ID/선택 모듈을 검증한 뒤 생성한다. 다른 파일과 기존 씬/머티리얼을 자동으로 교체하지 않는다.
- 임시 미리보기 Shader/Material/MaterialEditor는 Window가 소유하고 갱신/닫기 때 해제한다.

SSOT: Recipe가 선택과 기본값을, Catalog가 모듈 계약을, HLSL이 계산을 소유한다. GUI·코드 생성·파일 내보내기를 분리한다(SOLID). Editor 전용 도구로 Framework 서비스/DB/Scope 계약이나 CoreSystem을 변경하지 않는다. FieldCamera/Architecture 영역도 변경하지 않는다.

## Task / Phase

### Phase 2Q — 주변 깊이 보정 Half Lambert 팔레트 (2026-10-16)

사용자 확정: 주변 픽셀과의 깊이 차이로 홈·돌출부를 찾아 Half Lambert 값을 보정한 뒤 Step 구간에 색을 매핑한다.

설계: 기존 Half Lambert는 유지하고 `Half Lambert Depth Palette` 모델을 추가한다. 현재 표면과 8방향 주변 깊이를 역 VP로 월드 위치로 복원한다(직교/원근 공통). 현재 삼각형 접평면에서 이웃 위치가 벗어난 정도를 접선 방향 거리로 정규화하여 경사면의 기울기는 제거한다. 주변보다 솟은 표면은 양수, 들어간 표면은 음수로 보정한다. 화면 밖·배경·최대 비교 거리 밖의 샘플과 현재 표면에 맞지 않는 깊이는 제외한다. 보정값은 한 모델 평가당 한 번 계산하고 각 광원의 Half Lambert 값에 더한 뒤 0~1 제한→최근접 Step→5색 팔레트 순서로 적용한다. 그림자·광원 색·감쇠·환경광·모델 혼합은 기존 계약을 따른다.

기본값: 삼각형 면 노멀(아래 추가 지시 반영), 명암 곡선 2, 환경광 0.1, Step 5(2~20), 반경 4픽셀, 깊이 강도 0.5, 최대 비교 거리 0.25월드 유닛. 팔레트는 짙은 적갈색→벽돌색→주황→밝은 주황→살구색의 5개 입력이다. 깊이 강도 0은 깊이 보정만 비활성화하며 팔레트·Step은 유지한다.

깊이 계약: 불투명 Forward보다 먼저 현재 카메라의 깊이 텍스처가 필요하다. 현재 PC 렌더러의 불투명 이전 SSAO는 깊이 prepass를 요구한다. Preview도 사용하는 렌더러 설정에 따라 깊이 시점이 달라지며 URP RenderGraph에서 Preview라는 이유만으로 prepass가 보장되지는 않는다. Depth Texture 옵션만 켜고 불투명 뒤에 복사하는 렌더러에서는 이 모델이 요구하는 시점에 깊이가 없을 수 있다. 이 경우나 유효 샘플이 없는 경우 보정은 0으로 복귀한다. 현재 렌더러/씬/프리뷰 설정을 자동 수정하지 않는다. 가려진 면·화면 밖 정보나 같은 깊이의 다른 오브젝트 식별은 화면 깊이만으로 제공되지 않는다.

SSOT/SOLID/FW·독립성: 신규 HLSL이 깊이 잔차·팔레트·Half Lambert 결합을 소유한다. Catalog가 파라미터·기본값과 깊이 요구 플래그를 소유하고 Generator는 활성 모델의 요구를 읽어 전용 include 플래그를 출력한다. 다른 모델은 깊이 샘플러를 요구하지 않는다. 기존 모델 계산과 Framework·DB·CoreSystem·FieldCamera·Architecture는 수정하지 않는다.

Task: (1) 모델 요구 메타데이터와 조건부 생성 연결 (2) 전용 HLSL·meta 및 Catalog 등록 (3) 내장 설명서·깊이 시점 안내 (4) 단독/혼합 Pass·직교/원근 복원·평면/오목/볼록·샘플 제외·Step 색 수치 검증 (5) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

완료·검증: 모델·기본값·조건부 깊이 include·내장 설명서를 연결했다. 단독/Toon 혼합 D3D11 컴파일 42건이 통과했다. 실제 HLSL 수치 검증에서 평면 잔차 0, 볼록/오목 잔차 +0.5/-0.5, 거리 초과 샘플 제외, 경사면 제거, 보정 강도 0, Step 선택과 팔레트 기준색, 직교/원근 위치 복원을 확인했다. 격리 Unity 6000.0.73f1 프로젝트에서 실제 Catalog/Generator의 파라미터 연결·레시피 직렬화 왕복·깊이 요구 조건을 검사하고 생성 Shader의 6개 Pass를 컴파일했다.

실제 GPU 검증: 격리 프로젝트의 D3D11 렌더에서 ForcePrepass를 사용하고 비대칭 요철 메시의 깊이 보정 전후를 비교했다. 직교 28,130픽셀·원근 31,011픽셀의 색이 바뀌었고 최대 RGB 차이는 두 경우 모두 0.6011353이었다. 이 스트레스 검증은 Step 20·반경 8·거리 제한 0.5·강도 0/1이며 제품 기본값과 구분한다. 이미지도 확인했다. RT 경로의 VP/역 VP Y 반전 차이를 보정했으며 라이트 레이어가 꺼진 경우 레이어 마스크로 조명을 잘못 제외하지 않도록 처리했다. 검증 산출물은 Git 제외 경로 `Library/ToonVerification/DepthHalfLambert/`와 `Library/DepthPaletteUnityCheck/`에 있다. 원본 프로젝트의 Tool UI 조작·실제 바위 씬 확인은 남아 있다. 화면 깊이 기반이므로 카메라·해상도에 따라 결과가 달라진다. Phase 리뷰 지점에서 멈춘다.

추가 지시(2026-10-16): 깊이 보정 Half Lambert 라이팅도 폴리곤(삼각형 면) 노멀을 사용한다. 모델 내부 문맥 사본의 노멀을 기존 위치 미분 함수로 교체하고 주광원·추가광과 GI 혼합에 적용한다. 라이트맵이 없는 환경광은 면 노멀로 다시 평가한다. 깊이 보정·Step·팔레트·라이트맵 데이터와 혼합하는 다른 모델은 유지한다. Task는 모델 내부 노멀 교체 → 내장 설명서 갱신 → 단독/혼합 Pass 컴파일 및 격리 Unity 확인 → 결과 기록이다. 기존 공용 면 노멀 함수를 재사용하고 Framework·Core·씬 조립을 변경하지 않아 SSOT·SOLID·FW 및 기능 독립성을 유지한다.
추가 지시 반영·검증: 모델 내부 노멀과 비라이트맵 SH 환경광을 면 기준으로 교체하고 내장 설명서를 갱신했다. 단독/Toon 혼합 D3D11 컴파일 42건과 격리 Unity의 생성 Shader 6개 Pass 컴파일이 통과했다. 이번 변경 후 원본 씬의 바위 외형은 확인하지 않았다. Phase 리뷰 지점에서 멈춘다.

추가 지시 — 노멀 스무스(2026-10-16): 기존 Half Lambert → 깊이 보정 → Step → 팔레트 계산을 유지하면서 면 명암을 부드럽게 연결하도록 `NormalSmoothness`(노멀 스무스, 0~1, 기본 0)를 추가한다. 0은 기존 삼각형 면 노멀, 1은 메시의 보간된 노멀이며 중간값은 공용 면 노멀 함수의 혼합을 사용한다. 깊이 보정의 접평면은 스무스와 무관한 기하 면 노멀로 유지하고, 혼합 노멀은 직접광·비라이트맵 SH 환경광에만 적용한다. 메시가 하드 노멀만 제공하면 1에서도 경계가 남고 Step 색 경계·실루엣은 별도다. 이전 생성 Shader의 호출은 0으로 위임하는 오버로드로 호환한다. Catalog가 범위·기본값·GUI/생성 바인딩을 소유하고 HLSL이 연산을 소유한다. 다른 모델·Framework·Core·씬 조립은 수정하지 않는다(SSOT/SOLID/FW·독립성). Task: Catalog/HLSL 연결 → 설명서 갱신 → 이전/새 호출·단독/혼합 Pass 및 Unity 레시피/기본값 검증 → 결과 기록 후 리뷰 지점에서 멈춤.

노멀 스무스 완료·검증: Catalog의 0~1 슬라이더와 HLSL 혼합을 연결했다. 기본 0·이전 레시피 누락값의 0 복구·0.5 직렬화 왕복·생성 Material 프로퍼티를 격리 Unity에서 확인했고 새 호출의 6개 Pass가 컴파일됐다. 이전 호출을 사용하는 단독/Toon 혼합 D3D11 컴파일 42건도 통과했다. 실제 D3D11 렌더에서 스무스 0→0.5 / 0.5→1의 변경 픽셀은 직교 8,510 / 8,432개, 원근 9,400 / 9,337개로 중간값과 양 끝의 반응을 확인했다. 깊이 강도 0.5·Step 20의 요철 검증 메시이며 원본 바위 씬과 구분한다. 검증 코드는 Git 제외 Library/DepthPaletteUnityCheck/에 있다. 기존 생성 Shader는 기존 호출로 계속 동작하며 새 슬라이더는 Composer에서 다시 생성해야 노출된다. Phase 리뷰 지점에서 멈춘다.

### Phase 2P — PBR 결과 밝기 Step 모델 (2026-10-16)

사용자 요청·확정: 삼각형 면 노멀로 PBR을 계산한 뒤 결과 밝기를 단계화하며 RGB 비율을 유지한다. 모델 혼합·효과·안개·톤매핑보다 앞에서 해당 모델 결과에 적용한다.

설계: `PBR Step (결과 밝기 단계)` 모델을 별도로 추가한다. 금속성 0·매끄러움 0.5·단계 수 6(2~20)·기준 밝기 범위 1(0.1~8)을 기본값으로 한다. 선형 PBR RGB에서 휘도 Y를 구하고 간격 `기준 범위 / (단계 수 - 1)`의 가장 가까운 밝기로 맞춘다. 정중앙은 큰 값으로 정한다. RGB에는 `선택 밝기 / Y`를 곱한다. 검정은 0으로 유지한다. HDR은 0~1로 제한하지 않고 기준 범위 밖에서도 같은 간격으로 이어간다. 예: 6단계·기준 1에서 0.63→0.6, 2.63→2.6.

SSOT/SOLID/FW·독립성: 새 `PbrStepLighting.hlsl`이 면 노멀 준비와 휘도 양자화를 소유하며 PBR 계산은 기존 `ProjectLComposerLit`을 재사용한다. Catalog가 입력·기본값·생성 호출을 소유하고 공용 include에는 연결만 추가한다. 면 노멀 변경은 함수 내부 문맥 사본에 적용한다. 비라이트맵 SH와 정점광 입력은 면 노멀로 다시 평가하고 라이트맵 값은 유지한다. 기존 모델·공용 Pass·효과 순서·씬·Material·Framework·DB·CoreSystem·FieldCamera·Architecture는 수정하지 않는다.

Task: (1) 전용 HLSL·meta와 Catalog 등록 (2) 내장 설명서 추가 (3) 새 모델 단독/혼합 Pass 컴파일 및 실제 HLSL의 밝기·RGB 비율·검정·HDR 검증 (4) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

완료·검증: 새 모델·입력·내장 설명서를 연결했다. 단독·Toon 혼합 D3D11 컴파일 42건(Forward+/정점광/추가광/그림자/라이트맵·깊이/노멀/Meta)이 통과했다. 실제 HLSL의 상수 입력 컴파일 결과에서 기본 범위의 0.63→0.6, HDR 2.63→2.6, 검정→0, 기준 범위 2의 0.63→0.8, 20단계의 0.63→12/19를 확인했다. 입력 RGB (0.8,0.4,0.2)는 (0.679983,0.339992,0.169996)로 변환되어 RGB 비율 4:2:1과 목표 휘도 0.4를 유지했다. 검증 소스·바이트코드·어셈블리는 Git 제외 경로 `Library/ToonVerification/PbrStep/`에 보관한다. Unity 에디터 컴파일·UI·실제 렌더 확인은 남아 있다. Phase 리뷰 지점에서 멈춘다.

### Phase 2O — Step Lit 인접 조명색 모델 (2026-10-16)

사용자 요청·확정: Step으로 만든 계산값들 가운데 일반 라이팅의 내적값과 인접한 값을 찾아 그 조명색을 칠한다. 별도 팔레트나 최종 RGB의 채널별 양자화가 아니다.

설계: `Step Lit (인접 조명색)`을 새 모델로 추가한다. 0~1에 균등 배치한 N개 표본(기본 6, 범위 2~20)에서 `saturate(N·L)`에 가장 가까운 값을 고른다. 표본 간 정중앙은 큰 값으로 정한다. 예: 6단계의 0.63 → 0.6. 선택한 내적값으로 Lambert 확산광을 계산하고 광원의 색·강도·거리 감쇠·그림자를 적용한다. 각 광원은 독립 평가하며 합산 후 환경광을 더한다. 환경광 기본값은 기존 모델과 같은 0.15다.

SSOT/SOLID/FW·독립성: Catalog는 모델 입력과 기본값을 소유하고, 새 `NearestStepLighting.hlsl`은 표본 선택과 확산광 구현을 소유한다. 공용 조립기 include에는 연결만 추가한다. 신규 모델 변경은 해당 HLSL·Catalog 항목·관련 검증에 머물며 기존 모델의 계산을 수정하지 않는다. 기존 모델 혼합·Pass 수명·효과 순서는 공통 계약을 재사용한다. Framework 서비스·DB·CoreSystem·FieldCamera·Architecture·씬·기존 Material은 변경하지 않는다.

Task: (1) 모델 구현·meta와 Catalog 등록 (2) 내장 설명서 안내 (3) 실제 HLSL의 표본 선택 및 새 모델 Shader Pass 컴파일 검증 (4) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

완료·검증: 새 모델과 내장 설명서를 연결했다. 단독·Toon 혼합 Shader의 D3D11 컴파일 42건이 통과했다. 실제 HLSL을 상수 입력으로 컴파일한 결과에서 6표본의 0.63→0.6, 0.05→0, 정중앙 0.1→0.2, 양 끝 범위 제한, 20표본의 0.63→12/19를 확인했다. 기본색·광원 RGB·거리 감쇠·그림자 감쇠의 결합도 기대값과 일치했다. 검증 소스·바이트코드·어셈블리는 Git 제외 경로 `Library/ToonVerification/NearestStep/`에 보관한다. Unity 에디터 컴파일·UI·실제 렌더 확인은 남아 있다. Phase 리뷰 지점에서 멈춘다.

추가 지시(2026-10-16): Step Lit은 정점 보간 노멀 대신 삼각형 면 노멀을 사용한다. 기존 `ProjectLFlatDiffuseNormal`을 고정값 1로 호출하여 위치 미분 기반 면 노멀을 구하고, Step Lit 함수 안의 문맥 사본에만 적용한다. 주광원·추가광 내적과 실시간/베이크 GI 혼합에 해당 노멀을 사용한다. 라이트맵이 없는 SH 환경광도 면 노멀로 다시 평가하며 기존 라이트맵 값은 유지한다. 공용 Pass·다른 모델은 수정하지 않는다. Task는 모델 내부 노멀 교체·설명서 갱신·단독/혼합 Pass 컴파일 검증이다.

추가 지시 반영·검증: 면 노멀과 SH 환경광 처리를 반영하고 단독·Toon 혼합 D3D11 컴파일 42건을 다시 통과했다. 실제 Unity 화면에서의 면별 명암 확인은 남아 있다. 위치에 따른 광원 방향·감쇠·그림자는 기존처럼 픽셀별로 평가한다. Phase 리뷰 지점에서 멈춘다.

### Phase 2N — Toon 명암 단계 상한 20 (2026-10-16)

사용자 요청: Tool의 Toon 라이팅 명암 단계를 20까지 설정한다.

설계·Task: Catalog의 Toon `Steps` 범위를 2~20으로 늘리고 기본값 4는 유지한다. 공용 Toon HLSL의 clamp와 단계 경계 반복 상한은 하나의 GPU 상수로 맞춘다. 기존 단계의 계산식과 부드러움은 유지한다. 이어서 Toon을 포함한 Shader Pass를 컴파일하고 결과를 기록한 뒤 멈춘다.

SSOT/SOLID/FW·독립성: GUI·입력 정규화·새 Shader의 Range 선언은 Catalog의 같은 파라미터 정의를 사용한다. GPU의 방어 상한은 HLSL 한 상수에서 clamp와 반복문이 공유한다. 조립 UI와 조명 계산의 기존 책임을 유지한다. 기존 내보낸 Shader의 Range 선언은 재내보내기 때 갱신한다. 씬·Material·다른 라이트 모델의 UI 범위·Framework 서비스·CoreSystem·FieldCamera·Architecture는 변경하지 않는다.

완료·검증: Toon UI/입력/내보내기에 사용되는 Catalog 범위와 공용 HLSL 계산 상한을 20으로 반영했다. 최대 20단계 설정을 가진 Toon 조립 Shader의 D3D11 컴파일 21건(Forward+/추가광/그림자/라이트맵·깊이/노멀/Meta 포함)이 통과했다. Unity 에디터 UI·실제 렌더는 미확인이다. 검증 소스·산출물은 Git 제외 경로 `Library/ToonVerification/Toon20Steps/`에 보관한다. Phase 리뷰 지점에서 멈춘다.

### Phase 2M — 새 파일 저장의 Shader 내부 이름 충돌 (2026-10-15)

사용자 보고: 다른 파일명으로 새로 저장해도 같은 이름이 있다는 오류가 난다. 파일명과 Shader 내부 경로 이름이 별개인데 기본 `ProjectL/Generated/NewShader`를 재사용하면 기존 `TestLit.shader`와 충돌한다. 기존 검사는 충돌 파일 경로를 보여 주지 않고 `Shader.Find`의 임시 Shader도 충돌로 취급한다.

설계: 새 `.shader` 파일을 저장할 때만 내부 이름 충돌을 자동 해소한다. 기존 내부 이름의 상위 경로와 선택한 파일명을 합친 후보를 쓰고 후보도 중복되면 숫자를 붙인다. 원래 이름이 고유하면 그대로 쓴다. 기존 파일 재저장은 이름/경로를 유지하며 다른 에셋과의 충돌은 실제 경로를 포함해 거절한다. 임시 미리보기 Shader는 충돌 대상에서 제외하고 실제 Shader 에셋을 확인한다. 경로를 정규화하여 같은 파일의 대소문자/상대 경로 차이를 중복으로 오인하지 않는다. 성공 뒤에만 창의 내부 이름을 결과와 맞추고 상태 줄에 이름 변경을 안내한다. 내장 설명서에 파일명/내부 이름의 차이와 새 파일 자동 조정을 적는다.

SSOT/SOLID/FW·독립성: 내보내기 모듈이 이름 충돌/출력 이름을 결정하고, Generator는 조정된 Recipe 사본으로 소스와 내장 Recipe를 함께 만든다. 원본 Recipe는 저장 성공 뒤 창에서만 갱신한다. HLSL·조명·효과·게임 씬·기존 Material·Framework/DB/CoreSystem은 변경하지 않는다.

Task: (1) 내보내기 이름/경로 검사와 창 상태·설명서 (2) 기본 이름 충돌 새 파일·후보 중복·기존 파일 재저장·임시 Shader·실제 충돌 거절·Recipe 왕복/GUID/Material 유지 검증 (3) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

Phase 2M 완료: 새 파일의 내부 이름 충돌은 상위 Shader 경로 + 저장 파일명으로 해소하고 필요하면 숫자를 붙인다. 임시 Shader를 무시하며 실제 에셋들을 검사한다. 같은 파일의 경로를 정규화하고 기존 파일 충돌 오류에는 실제 경로를 표시한다. 저장 성공 뒤 창의 이름과 생성 코드·내장 Recipe를 일치시키고 변경 이름을 상태 줄에 표시한다. 내장 설명서에 새 동작을 추가했다.

검증: 열린 Unity 6000.0.73f1에서 고유 이름 유지, 동일 내부 이름의 다른 파일 저장과 새 Material 연결, 후보 이름 중복의 숫자 처리, 내장 Recipe 재생성 일치, 호출 Recipe 비변경, 기존 파일 이름/GUID/Importer meta/Material 값 유지, 상대 경로 정규화, 임시 Shader 오인 방지, 기존 파일의 실제 충돌 경로 안내·쓰기 전 거절을 확인했다. 새 출력 두 개의 12개 Shader Pass 컴파일 오류 0을 확인했다. 사용자 `TestLit.shader`와 두 테스트 씬의 파일 내용은 검증 전후 바이트가 같았다. 테스트가 만든 Assets 임시 폴더는 해제했으며 검증 코드와 로그는 `Library/ToonVerification/ExportNaming/`에 보관하고 Assets의 임시 검증 소스/.meta는 제거했다. Phase 리뷰 지점에서 멈춘다.

### Phase 2L — 라인 마스크 (2026-10-15)

사용자 확정: 반복 줄무늬와 직선 한 줄을 모두 지원한다.

설계: 효과 마스크에 `라인 (UV)`을 추가하고 모드로 반복/한 줄을 고른다. 모델의 원본 UV0에서 (0.5, 0.5)를 기준으로 선의 방향(0도 수평, 90도 수직), 수직 방향 위치 이동, 전체 선 폭, 반복 중심 간격, 바깥 경계 페이드를 조절한다. 폭·이동·간격·페이드는 UV 단위이며 반복 간격은 한 줄 모드에서 사용하지 않는다. 폭 0은 빈 마스크, 반복 폭이 간격 이상이면 전체 마스크다. 화면 미분으로 약 1픽셀 경계를 완화하며 반복 경계에서 미분하지 않도록 접기 전 좌표를 사용한다. 반전·공유 프로퍼티·레시피 재열기·모든 효과와 디졸브 보조 Pass/Meta는 기존 공간 마스크 경로를 재사용한다.

SSOT/SOLID/FW·독립성: 입력과 기본값은 Catalog, UV 라인 계산은 MaskUtility 한 함수, UI/생성/저장은 기존 모듈 경로가 소유한다. 전용 GUI 분기나 새 서비스/Recipe 필드를 만들지 않는다. Framework/게임 CoreSystem/DB/FieldCamera/Architecture와 실제 씬·머티리얼을 변경하지 않는다.

Task: (1) Catalog·HLSL·내장 설명서 (2) 두 모드/방향/이동/폭·간격 경계/반전 GPU 확인과 효과·모델별 Pass 컴파일, 공유 입력/레시피 왕복 (3) 결과 기록·임시 검증 코드 정리 후 Phase 리뷰 지점에서 멈춤.

Phase 2L 완료: `라인 (UV)` 마스크의 Repeat(반복 줄무늬) / Single(직선 한 줄), 방향·위치·폭·반복 간격·부드러움·반전을 추가했다. 기존 Catalog 기반 GUI·프로퍼티 참조·생성기·저장을 그대로 사용하며 새 Recipe 필드나 GUI 분기는 없다. 내장 설명서에 단위·사용법·UV 이음새와 늘어남을 설명했다.

검증: Unity 6000.0.73f1 / D3D11에서 모델 5종 × 효과 4종 × 모드 2종의 40개 조합, 공유 폭 입력·GPU 확인용 2개 = 42개 Shader의 252개 Pass 컴파일 오류 0과 SRP Batcher 호환 코드 0을 확인했다. Recipe 재생성 일치·공유 폭 선언 1회와 자동 선언 생략을 확인했다. URP GPU 렌더에서 한 줄/반복, 45·90도 회전, 위치 이동, 반전, 부드러움, 폭 0, 반복 폭=간격, 잘못된 간격 0의 유한한 결과와 한 줄 모드의 간격 무시를 확인했다. 실제 창에서 반복 프리뷰와 Repeat/Single 목록도 확인했다. 반복 디졸브·한 줄 발광의 D3D11 정점/픽셀·인스턴싱·추가광·Forward+·라이트맵·그림자·노멀·Meta 변형 42개도 컴파일했다. 초기 한글/공백 Enum 이름의 ShaderLab 파싱 오류는 안정적인 Repeat/Single 이름으로 해결했다. 첫 GPU 검사에서는 기본 발광 색의 R=0을 밝기로 읽은 검증 코드만 수정해 흰색으로 재검증했다. 검증 소스·이미지·로그는 Git 제외 경로 `Library/ToonVerification/LineMask/`에 보관한다. 다른 그래픽 API·실제 GI 재베이크는 이번 검증에 포함하지 않았다. Phase 리뷰 지점에서 멈춘다.

### Phase 2K — 옛 BaseShader의 GUI 조립 형식 전환 (2026-10-15)

사용자 확정 대상: `View/NotUse/BaseShader/Base/BaseShader.shader` (`Custom/ProjectLBaseLit`) 한 개다. ToonLit/ToonLitDissolve와 옛 BaseShaderDissolve는 이번 범위에 없다.

설계: 기존 평면/보간 노멀 명암·스펙큘러·직접광 림·SH 환경광을 `Base Lit (기존 베이스)` 라이트 모델로 제공한다. 계산은 기존 공용 Toon/Specular/Rim 함수에 연결한다. 기존 `_Color`·`_MainTex`·명암/반사광/림 프로퍼티를 사용자 프로퍼티와 모델 입력 참조로 조립한다. `_MainTex_ST` Vector4 입력으로 기존 머티리얼의 텍스처 타일링/이동도 유지한다. Shader 이름/경로/.meta GUID와 기존 기본값은 유지하고 내장 Recipe가 있는 생성기로 셰이더 전체를 생성한다. Recipe와 생성 코드가 같은 모델을 가리켜 다시 열기/편집/내보내기가 가능해야 한다.

호환: 생성기에 선택 가능한 Unity 표준 Pass 이름 모드와 안개 적용 옵션을 추가한다. 기본값은 기존 조립기와 같고 이 베이스의 레시피에서만 기존 PascalCase Pass 이름/UniversalForward·DepthNormals 태그와 안개 미적용을 선택한다. 기존 런타임 Material.SetShaderPassEnabled 호출도 유지한다. Meta는 조립기의 URP 베이크 경로를 사용한다(옛 화면 좌표 Meta 구현은 이전하지 않는다). 기존 Material 에셋을 수정하거나 다른 셰이더로 바꾸지 않는다.

SSOT/SOLID/FW: 생성 결과 안의 Recipe가 변환 후 기본값·프로퍼티 연결의 원본이다. 일회성 변환 코드는 검증용으로만 쓰고 저장소에 중복 프리셋을 두지 않는다. Catalog는 모델 입력, ComposerLighting은 라이팅, Generator는 렌더 출력 계약을 소유한다. 게임 CoreSystem/Framework/DB/FieldCamera/Architecture는 수정하지 않는다. 이 경로의 Shader/Core는 렌더 HLSL 라이브러리다.

Task: (1) 베이스 모델·공통 출력 옵션 (2) 기존 프로퍼티/기본값 레시피로 단일 Shader 생성·GUID 유지 (3) Unity 모든 Pass 컴파일·SRP Batcher·레시피 재생성 일치·기존 Material 값/참조 유지·기존/변환 GPU 이미지와 UV/평면 명암/추가광 비교·실제 도구에서 열기 (4) 검증 코드 정리·결과 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2K 완료: 지정된 `Custom/ProjectLBaseLit` 파일 하나를 `Base Lit (기존 베이스)` 모델과 사용자 프로퍼티 연결로 생성했다. 평면/보간 명암·반사광·직접광 림·SH 환경광을 유지한다. 일반 Toon과 Base Lit은 공통 직접광 루프를 사용하며 기존 Toon의 함수 호출 계약도 유지한다. Shader 이름·GUID·기존 프로퍼티 14개와 PascalCase Pass 이름 6개, 텍스처 ST를 보존했다. 추가 출력에서 안개/Unity 표준 Pass 이름을 선택할 수 있다. 내장 설명서에 열기와 프로퍼티 창에서 기본값 편집을 설명했다. 처음 붙인 Base Toon 명칭은 사용자 정정으로 Base Lit로 바꿨고, 작업 중 열린 창/레시피의 이전 모델 키도 정규화한다.

검증: Unity 6000.0.73f1에서 Base Lit/일반 Toon/Base Lit+Half Lambert+빛 마스크 발광의 전체 Pass 컴파일 및 SRP Batcher 코드 0을 확인했다. Base Lit의 기본 평면 명암, 보간 노멀, 단계/부드러움/금속성, 비기본 텍스처 타일링·이동, 색광/추가 Point+림의 5가지 GPU 렌더 비교가 원본과 모든 픽셀에서 동일했다. 일반 Toon의 이전/공통화 뒤 GPU 이미지도 동일했다. 최종 Base Lit을 포함해 기존 Half Lambert/Toon 혼합/전체 스택의 D3D11 정점·픽셀·Forward+·정점광·SSAO/쿠키/Light Layers·라이트맵/ShadowMask·점광원 그림자·옥타헤드럴 노멀·Meta 시각화 변형 84개를 컴파일했다. Recipe 읽기→재생성은 최종 파일과 바이트가 같았고 `.meta` 내용과 참조 Material 파일 8개가 변경되지 않았다. 실제 창의 `조립한 Shader 열기` 파일 대화상자로 해당 파일을 열어 모델/프로퍼티 연결과 프리뷰를 확인했다.

제한: 화면 렌더의 비교 증거는 D3D11 프리뷰 조건이다. 실제 게임 씬의 GI 재베이크와 다른 그래픽 API는 이번 검증에 포함하지 않았다. Meta는 공용 URP 베이크 경로로 갱신됐으므로 기존 베이크 결과와의 재대조는 별도 확인 대상이다. 변환 전 원본·검증 소스·이미지·로그/컴파일 산출물은 Git 제외 경로 `Library/ToonVerification/BaseAssembly/`에 보관했고 임시 `.cs`/`.meta`는 Assets에서 제거했다. Phase 리뷰 지점에서 멈춘다.

### Phase 2J — URP 후처리 프리뷰 (2026-10-15)

사용자 요청: 프리뷰에도 URP 후처리 효과를 표시한다.

설계: 오른쪽 프리뷰 설정에 URP 후처리 토글(기본 켜짐)과 Volume Profile 에셋 필드를 추가한다. 비어 있으면 URP 기본 Volume 설정을 쓰고 선택하면 해당 프로필을 가중치 1로 적용한다. Preview 카메라 종류를 유지하면서 URP SingleCameraRequest로 AdditionalCameraData의 후처리 설정을 전달한다. 별도 VolumeStack에 VolumeManager.Update의 빈 LayerMask로 URP 기본값을 적용하고 선택 프로필의 공개 VolumeComponent.Override를 호출한다. 전용 프리뷰 씬의 빈 Volume을 렌더 동안만 등록해 Volume이 없는 씬에서도 Unity의 초기화 생략 경로로 이전 값이 남지 않게 한다. 렌더 뒤 기존 전역 stack을 finally로 복구한다. 프로필 에셋은 읽기만 하고 수정하지 않는다. 현재 씬의 Volume 자동 혼합/커스텀 Renderer Feature/카메라 누적 효과의 완전 재현은 이번 범위에 없다. 라이트 오버레이는 후처리된 이미지 위에 그린다.

SSOT/SOLID/FW: 효과 설정의 원본은 VolumeProfile, 선택/토글은 프리뷰 설정, 수명과 렌더는 별도 Editor 보조 클래스가 소유한다. URP/Core Runtime 어셈블리 참조만 Editor asmdef에 추가한다. Recipe/Shader 생성·게임 씬/프로필 에셋/프로젝트 URP 설정·Framework/DB/CoreSystem·FieldCamera·Architecture는 수정하지 않는다. 추가 갱신 루프 없이 기존 Repaint만 사용한다.

Task: (1) URP 카메라/VolumeStack 렌더와 해제 (2) 설정 UI/설명서/.meta (3) Unity 컴파일·Bloom/색보정 실제 GPU 결과·토글/프로필 교체·씬 Volume 격리·전역 stack/Recipe 유지·자원 해제 검증 (4) 결과 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2J 완료: 오른쪽 `URP 후처리` / `Volume Profile`을 기본 구와 FBX의 공통 프리뷰에 연결했다. Preview 카메라에 URP AdditionalCameraData를 추가하고 SingleCameraRequest를 사용하며, 프로필 선택과 토글은 Shader 재생성을 유발하지 않는다. 기존 라이트 오버레이는 후처리가 끝난 이미지 위에 유지된다. 내장 설명서에 프로필 입력, Override/활성 상태, 비용과 씬 비교의 한계를 설명했다.

검증: Unity 6000.0.73f1 / URP 17.0.4 Editor 컴파일 오류 0. GPU 이미지에서 빨간 색 보정, 후처리 끄기, 중립 프로필로 교체, 밝은 표면 밖으로 퍼지는 Bloom을 확인했다. 실제 씬에 우선순위 10000의 빨간 Global Volume을 추가해도 프리뷰 이미지가 완전히 같았고, 모든 렌더 뒤 전역 VolumeStack 참조가 복구됐다. 원본 프로필 JSON·실제 씬 루트/dirty 유지, 숨은 Volume의 렌더 사이 비활성화와 Dispose 시 Volume/Stack 해제를 확인했다. 실제 창에 기존 `Post process Profile` 에셋을 넣어 결과/오버레이 표시와 UI 토글을 확인했으며 Recipe/Shader 인스턴스가 유지됐다. 검증용 UI 설정은 원복했고 임시 검증 코드는 Assets에서 제거해 `Library/ToonVerification/PostProcessing/`에 이미지·로그와 함께 보관했다.

검증 중 보완: 리로드 직후 VolumeManager 초기화 전 호출은 최초 기본 렌더로 URP를 준비한다. Preview 씬 필터만으로는 씬 Volume 격리가 보장되지 않아 빈 LayerMask로 기본값을 초기화하고 공개 VolumeComponent.Override로 선택 프로필만 적용했다. 빈 Volume은 Unity의 Volume 개수 0일 때 초기화 생략 최적화로 이전 프로필 값이 남는 것을 방지한다. 최초 실패와 수정 후 통과 로그를 함께 남겼다. 실행 게임의 코드/설정에는 비용을 추가하지 않으며, Editor 프리뷰 갱신에는 선택한 후처리의 GPU 비용이 든다. Phase 리뷰 지점에서 멈춘다.

### Phase 2I — 프리뷰 라이트 오버레이 (2026-10-15)

사용자 요청: 프리뷰 라이트도 오버레이로 보여 준다.

설계: 프리뷰 이미지 위에 라이트 번호/종류와 위치 표시를 그린다. Directional은 모델 주위의 방향 화살표(절대 위치 의미 없음), Point는 위치와 범위 구, Spot은 위치·방향과 범위/안쪽·바깥쪽 각도 원뿔을 표시한다. 오른쪽 패널에 라이트 오버레이/영향 범위 표시 토글을 추가한다. 비활성 라이트는 회색으로 구분하며 화면 밖 라이트는 테두리 표식/라벨로 안내한다. 선은 프리뷰 영역에 잘리고 카메라 뒤쪽 구간은 near plane에서 잘라 투영한다. 카메라 회전/확대축소와 실제 Light 설정을 따른다. 오버레이는 표시 전용이며 드래그로 라이트를 조작하는 핸들은 이번 범위에 없다.

SSOT/SOLID/FW: 좌표/종류/범위/각도는 이미 프리뷰에 적용된 Light 컴포넌트에서 읽어 재계산 규칙을 중복하지 않는다. 투영/표시는 별도 Editor 클래스가 맡고 고정 선 버퍼를 재사용한다. 표시 토글은 프리뷰 설정만 소유하며 Recipe/셰이더/실제 씬·FBX·Framework/DB/CoreSystem·FieldCamera·Architecture는 변경하지 않는다. 추가 씬 객체나 주기적 갱신을 만들지 않는다.

Task: (1) 투영/클리핑·라이트별 오버레이와 .meta (2) 표시 토글·프리뷰 연결·설명서 (3) Unity 컴파일·세 종류 표시/카메라 추종/영역 클리핑·토글과 조명 분리 확인 (4) 결과 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2I 완료: `ShaderComposerPreviewLightOverlay`가 실제 프리뷰 Light에서 읽은 번호/종류·위치·방향·Point 범위 구·Spot 안쪽/바깥쪽 원뿔을 이미지 위에 표시한다. 꺼진 라이트는 회색과 `(꺼짐)`, 화면 밖 라이트는 테두리 표식과 안내로 구분한다. 오른쪽 `라이트 오버레이` / `영향 범위 표시` 토글과 내장 설명서를 연결했다. 종횡비에 따른 카메라 배치는 프리뷰가 직접 계산하므로 PreviewRenderUtility의 임시 화각 변경을 꺼서 이미지와 오버레이 투영을 일치시켰다.

검증: Unity 6000.0.73f1 Editor 컴파일 오류 0. 실제 창에서 Directional/Point/Spot 동시 표시, 카메라 드래그·휠 확대축소 추종, 프리뷰 경계 클리핑, 화면 밖 표식, 범위만 숨기기/전체 숨기기, 비활성 Point의 회색 표식과 조명 꺼짐을 확인했다. 별도 검증으로 가로/세로 렌더에서 표시 토글 전후 원본 조명 이미지의 전체 픽셀이 동일했고, 카메라 투영·Light 인스턴스·실제 씬 루트/dirty 상태가 유지됐다. UI 조작 전후 Recipe와 임시 Shader 인스턴스가 같아 재생성되지 않음을 확인했다. 검증용 조명/카메라는 원복했으며 임시 검증 코드는 Assets에서 제거하고 `Library/ToonVerification/Overlay/`에 결과와 함께 보관했다. 추가 런타임 비용은 없고 Editor Repaint에서만 선/라벨을 그린다. Phase 리뷰 지점에서 멈춘다.

검증 기록 보정: 최초 Shader 인스턴스 비교는 도메인 리로드 직후 재생성이 끝나기 전에 기준을 저장해 실패했다. 리로드 완료 후 기준을 다시 잡고 실제 UI 토글 전후 동일 인스턴스임을 확인했으며 도구 코드 수정은 필요 없었다. 최초 실패와 재검증 결과는 검증 로그에 함께 남겼다.

### Phase 2H — 프리뷰 오른쪽 설정과 사용자 라이트 (2026-10-15)

사용자 요청: 프리뷰 오른쪽에 설정 창을 두고 프리뷰 씬에 라이트를 추가한다.

설계: 조립/프리뷰/프리뷰 설정의 3열로 배치하고 최소 창 크기를 늘린다. 기본 구와 FBX 모두 같은 PreviewRenderUtility를 사용해 설정을 동일하게 적용한다. 오른쪽 스크롤 패널에서 배경색/환경광, 최대 8개의 실시간 Directional/Point/Spot 라이트의 추가/삭제/활성화/색/강도, 종류에 맞는 위치/회전/범위/스폿 각도를 편집한다. 위치는 모델 중심 기준, 위치와 범위는 모델 반경의 배수다. 기본 설정은 기존 두 Directional 조명이다. 기본 조명 복원은 프리뷰 설정만 초기화한다. 실제 동시 반영 수는 현재 URP 설정을 따른다. 그림자/쿠키/씬 조명 복사 기능은 이 Phase에 추가하지 않는다.

SSOT/SOLID/FW: 직렬화된 프리뷰 설정은 EditorWindow가 한 개 소유하고 Recipe/내보낸 Shader에는 포함하지 않는다. 설정 데이터/GUI는 별도 파일로 분리하고 Light 컴포넌트는 설정에서 파생되는 표시 자원이다. Light 객체는 전용 프리뷰 씬에만 추가하며 수량 변경 때만 만들고 제거한다. 창 닫기/리로드 때 PreviewRenderUtility가 해제한다. 조명 값 수정은 Shader 재생성/컴파일을 유발하지 않는다. 실제 씬/원본 FBX·머티리얼·Framework/DB/CoreSystem·FieldCamera·Architecture는 변경하지 않는다.

Task: (1) 프리뷰 설정/라이트 데이터·전용 패널 GUI와 .meta (2) 기본 구/FBX 프리뷰 통합·전용 Light 생성/동기화/해제 (3) 3열 배치와 변경 경계·설명서 연결 (4) Unity 컴파일·세 광원 종류의 실제 렌더·추가/삭제/비활성/복원·Recipe/실제 씬 유지 검증 (5) 결과 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2H 완료: 오른쪽 스크롤 설정 패널과 프리뷰 전용 라이트 목록을 연결했다. `ShaderComposerPreviewSettings`가 직렬화 값, 별도 GUI가 편집, `ShaderComposerObjectPreview`가 Light 객체와 메시/카메라 수명을 담당한다. 기본 구도 같은 프리뷰를 사용하며 FBX 변경/해제에 라이트 설정은 유지된다. 프리뷰 설정 수정은 Recipe 변경 검사에서 제외하고 MaterialEditor 기반의 별도 기본 도형 미리보기는 제거했다. 기존 10Hz 주기 갱신은 유지한다.

Unity 기본 프리뷰 조명이 렌더 시작 때 재활성화되는 것을 검증에서 발견해 기본 두 Light의 강도/색을 0/검정으로 고정했다. 사용자 조명 목록만 실제 빛을 낸다. 근거: [Unity 6000.0 PreviewRenderUtility 소스](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Editor/Mono/Inspector/PreviewRenderUtility.cs)의 InitPreview/Render/FinishFrame.

검증: Unity 6000.0.73f1 Editor 컴파일과 최소 너비의 3열 패널/라이트 추가 버튼을 확인했다. 실제 URP GPU 렌더에서 무광·비활성은 검정, Directional/Point/Spot의 빨강 조명은 각각 중심 픽셀 R=1/0.447/0.447이었다. Point 범위 밖과 반대 방향 Spot의 무광, 빨강 Directional+파랑 Point의 동시 기여, 환경광 흰색 반영을 확인했다. 라이트 수 감소 때 실제 객체 파괴, 값 변경 때 객체 재사용, 기본 조명 복원, 기본 구/FBX 공통 렌더, 설정 직렬화 왕복, Recipe/Shader 유지, 실제 씬의 root 수/dirty 상태 유지, Dispose 뒤 라이트 파괴를 확인했다. 셰이더 생성/계산은 바꾸지 않았으며 앞 Shader 변형 검증은 유지한다. 그림자/쿠키와 기기별 성능 측정은 이번 범위에 없다. 상세 보고/이미지/검증 소스는 Git 제외 경로 `Library/ToonVerification/PreviewLights/`에 보관하고 임시 검증 스크립트는 Assets에서 제거했다. Phase 리뷰 지점에서 멈춘다.

### Phase 2G — 툴 내장 사용 설명서 (2026-10-15)

사용자 요청: 사용 설명서를 툴 자체에 포함한다.

설계: 조립 창/프로퍼티 창의 `사용 설명서` 버튼과 `ProjectL > Shader Composer Help` 메뉴로 독립 설명서 EditorWindow를 연다. 목차는 시작하기, 라이트 모델, 효과·마스크, FBX 미리보기, 사용자 프로퍼티, 저장·재편집, 성능, 문제 해결이다. 현재 UI의 동작과 제한을 설명하고 빠른 예제를 포함한다. 모델/효과/마스크 이름과 개수 제한은 Catalog에서 읽어 표시한다. 설명서 내용은 도움말 창이 소유하며 설계 문서에는 별도 사용 설명서 사본을 만들지 않는다. Recipe/미리보기 설정은 변경하지 않고 주기적 갱신/렌더링도 추가하지 않는다.

SSOT/SOLID/FW: 도움말은 독립 Editor 표시 책임만 가지며 현재 선택 상태를 복제하지 않는다. Catalog가 가진 목록/제한은 재정의하지 않는다. Framework 서비스/수명 계약·DB·CoreSystem·FieldCamera·Architecture 및 셰이더 생성/계산은 변경하지 않는다.

Task: (1) 목차와 본문을 가진 도움말 창 및 .meta 추가 (2) 두 도구 창과 메뉴 진입 연결 (3) Unity 컴파일·실제 창/본문 표시·Recipe 유지 확인 (4) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

Phase 2G 완료: `ShaderComposerHelpWindow`에 8개 목차와 읽기 전용 본문을 포함했다. 조립 창/프로퍼티 창의 `사용 설명서` 버튼과 메뉴로 같은 창을 연다. 시작 예제, 가중치 혼합, 효과/마스크와 디졸브 제한, FBX 입력/조작, 사용자 프로퍼티 참조, 내보내기/재편집과 Material 값의 차이, 현재 성능상 한계, 문제 해결을 설명한다. 모델/효과/마스크 목록과 모델/효과 수 제한은 Catalog를 읽는다.

검증: Unity 6000.0.73f1에서 Editor 어셈블리 컴파일과 조립 창 버튼으로 설명서 열기, 목차 전환, 시작하기/라이트 모델/성능 본문 표시와 줄바꿈을 확인했다. 설명서 조작 전후 기존 모델 종류/가중치와 미리보기가 유지되는 것을 화면에서 확인했다. 도움말은 Recipe에 쓰거나 이벤트/주기적 갱신을 등록하지 않는다. Shader 생성/계산은 바꾸지 않아 앞 Phase의 Shader 검증은 유지하며 이번에는 문서 창과 Editor 컴파일을 확인했다. Phase 리뷰 지점에서 멈춘다.

### Phase 2F — FBX 에셋 직접 입력 (2026-10-15)

사용자 요청: 미리보기에 GameObject 대신 FBX를 넣어서 읽는다.

설계: 입력을 `FBX 모델` 에셋 필드로 바꾸고 FBX 파일 선택/Project에서 선택한 FBX 사용을 제공한다. Unity가 임포트한 프로젝트 안의 `.fbx` 파일을 대상으로 한다. FBX 안의 하위 메시를 끌어 넣어도 해당 파일의 모델 루트를 읽는다. 씬 오브젝트·일반 프리팹·다른 파일은 거부하고 기존 유효 입력을 유지한다. 외부 FBX는 먼저 Project에 임포트하도록 안내한다. FBX의 MeshRenderer/SkinnedMeshRenderer·하위 Transform·LOD0를 기존 미리보기 수집기로 읽고 모든 서브메시에 조립 Material을 적용한다. 원본 임포터/모델/머티리얼은 변경하지 않는다. 비워 두면 기본 도형, 보기 초기화는 FBX 메시 재수집과 카메라 초기화다. 옛 창의 `previewObject`는 직렬화 이름을 이전하고 FBX일 때만 유지한다.

SSOT/SOLID/FW: 유효 FBX 판별과 모델 루트 해석은 미리보기 클래스의 한 함수가 담당한다. Window는 입력/UI, 미리보기 클래스는 메시/카메라/대여 자원 수명만 담당한다. FBX는 Window의 미리보기 설정이며 Recipe·생성 Shader·Framework/DB/CoreSystem·FieldCamera·Architecture 변경은 없다.

Task: (1) FBX 판별/루트 해석과 미리보기 입력 연결 (2) 에셋 필드·파일 선택·선택 FBX 사용·오류 안내/직렬화 이전 (3) Unity 컴파일·실제 FBX의 메시 수집/렌더와 잘못된 입력 거부 확인 (4) 결과 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2F 완료: `FBX 모델` 에셋 필드와 `FBX 파일 선택`/`선택 FBX 사용`을 연결했다. FBX 판별/루트 해석은 `ShaderComposerObjectPreview.TryGetFbxModel` 하나를 사용한다. 하위 Mesh를 넣어도 파일 전체 루트를 읽고 이전 GameObject 입력의 FBX 참조는 유지한다. 일반 프리팹/씬 오브젝트 입력은 받지 않는다.

검증: Unity Editor 컴파일 완료. 실제 `rock3.fbx`, `CactusBomb.fbx`, Skinned Mesh를 가진 `Akimbo.fbx`에서 메시 수집·유효 Bounds·재수집 때 이전 Bake Mesh 해제·원본 FBX/임포터 dependency hash 유지·비울 때 미리보기 메시 해제를 확인했다. FBX 메시 하위 에셋 → 모델 루트 해석과 null/Material/씬 GameObject/일반 프리팹 거부, 잘못된 입력 시 기존 FBX 유지와 안내도 확인했다. 조립 창에서 rock3 FBX를 실제 URP로 렌더링한 것을 확인했다. Skinned FBX는 메시 수집/Bake와 자원 해제를 검증했으며 이번 실제 화면 확인 대상은 rock3다. 셰이더 계산/생성을 바꾸지 않았으므로 앞 Phase의 Shader 변형 검증은 유지한다. 임시 검증 스크립트는 Assets에서 제거했고 상세 보고/검증 소스는 Git 제외 경로 `Library/ToonVerification/FbxPreview/`에 보관했다. Phase 리뷰 지점에서 멈춘다.

사용법: Project의 `.fbx` 파일을 `FBX 모델`에 끌어 넣거나 `FBX 파일 선택`으로 Assets 안의 FBX를 선택한다. Project에서 파일을 선택한 상태라면 `선택 FBX 사용`을 눌러도 된다. 외부 파일은 먼저 Project에 임포트한다. FBX를 씬에 배치할 필요가 없다. 빈 입력은 기본 도형이며 FBX 변경 후 `보기 초기화`로 메시를 다시 읽는다.

### Phase 2E — 하프램버트와 라이트 모델 혼합 (2026-10-15)

사용자는 하프램버트 추가와 한 Shader에서 여러 라이트 모델의 혼합(Toon + 하프램버트 등)을 요청했다.

설계: 라이트 모델을 최대 4개의 카드 목록으로 편집한다. 각 카드는 안정적인 ID, 활성화, 모델 종류, 독립 파라미터와 가중치를 가진다. 같은 모델도 중복 추가할 수 있다. 활성 모델의 결과를 비음수 가중치 합으로 나눈 가중 평균으로 혼합한다. 1:1은 반씩 혼합하며 모두 가중치 0이면 검정이다(후속 효과는 그대로 적용). 0 가중치는 모델 계산을 건너뛴다. 가중치도 사용자 Float/Range 프로퍼티를 참조할 수 있다. 활성 모델은 하나 이상 필요하고 마지막 카드는 삭제하지 않는다. 효과는 혼합된 결과에 기존 순서대로 한 번 적용한다.

하프램버트: 광원별 `pow(saturate(N·L * 0.5 + 0.5), Power)` × 광원 색/강도 × 거리·스폿/그림자 감쇠를 합산하고 환경광 강도를 별도로 조절한다. Power 기본값 2, 환경광 기본값 0.15. 금속성/스페큘러는 기존 Lit/Toon과 혼합해 사용한다. 메인광/추가 조명/Forward+·light layers/cookie·SSAO 경로를 따른다. 빛 마스크는 기존 실제 N·L 기반을 유지하며 모델 선택에 따라 달라지지 않는다.

SSOT/SOLID/FW: Recipe의 모델 목록만 현재 선택을 소유한다. v1의 단일 모델/파라미터는 v2 모델 ID 0으로 한 번 이전 후 이전 필드를 비운다(열린 EditorWindow의 직렬화 상태 포함). ID 0은 기존 `_Model_` 프로퍼티 이름을 보존하고 추가 모델은 ID별 접두사를 사용한다. Catalog는 파라미터, HLSL은 계산, Generator는 혼합/선언, Window는 목록 편집을 담당한다. 사용자 프로퍼티 검증/삭제/타입 변경도 모든 모델·가중치를 순회한다. 서비스·DB·CoreSystem·FieldCamera·Architecture 변경은 없다.

Task: (1) v2 모델 목록·v1 이전·하프램버트 Catalog/HLSL (2) 모델별 선언/가중 평균 생성과 사용자 프로퍼티 참조·검증 (3) 카드 추가/삭제/활성화/설정 GUI와 미리보기 연결 (4) 기존 레시피·혼합/공유 입력 왕복·각 Pass/조명 변형 컴파일 및 실제 GPU 혼합 확인 (5) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

Phase 2E 완료: 하프램버트 모델과 최대 4개 모델의 가중 평균 혼합을 구현했다. 모델 카드마다 활성화/삭제/종류/가중치/고유 파라미터를 편집한다. 가중치·모델 파라미터 모두 사용자 프로퍼티 연결을 지원한다. v1 레시피의 값·참조를 v2의 ID 0으로 이전하고 기존 모델 프로퍼티 접두사를 유지한다. 기존 효과/마스크/Pass 계약은 유지한다.

검증: Unity 6000.0.73f1에서 단일 모델 4종, 서로 다른 모델의 2개 혼합 6종, 4개 모델+빛 마스크 발광+공간 디졸브 스택, 같은 모델 중복, 공유 프로퍼티, 연결 해제, 기존 예제와 사용자 TestLit(v1, 읽기만), 비활성 모델 조합 = 17개 Shader/102개 Pass 오류 0·SRP Batcher 호환 코드 0. v1 값/참조 이전·이전 반복의 안정성·v2 레시피 왕복·공유 선언 한 번·삭제/타입 변경 시 참조 해제·잘못된 ID/모델 수/전체 비활성 거부를 확인했다. 기본 하프램버트/Toon+하프램버트/4모델 스택의 D3D11 변형 63개도 컴파일했다(추가 조명/그림자, light layers/cookie/SSAO, Forward+, 라이트맵/shadowmask, 보조 Pass 포함).

실제 URP GPU 미리보기에서 Toon/하프램버트/1:1/1:3 결과가 각각 `0 / 0.2941 / 0.1490 / 0.2196`으로 가중 평균과 픽셀 양자화 오차 안에서 일치했다. 전체 가중치 0의 검정과 하프램버트 정면/측면/뒷면 `1 / 0.2510 / 0`도 확인했다. GUI의 모델 추가와 하프램버트 선택 항목을 확인했다. 실제 씬의 여러 광원/그림자 가림·베이크·다른 플랫폼 시각 회귀는 이번 검증에 포함하지 않았다. 상세 보고/검증 소스/이미지는 Git 제외 경로 `Library/ToonVerification/ModelBlend/`에 보관하고 임시 검증 스크립트는 Assets에서 제거했다. Phase 리뷰 지점에서 멈춘다.

사용법: `1. 라이트 모델`에서 `+ 라이트 모델 추가`를 누르고 각 카드의 모델을 고른다. `Half Lambert (하프램버트)`의 `명암 곡선`은 기본 2이며 값이 클수록 명암이 집중된다. `환경광 강도`는 간접광 비중이다. Toon과 하프램버트 카드를 하나씩 두고 두 가중치를 1로 하면 절반씩 혼합한다. 가중치 0.25와 0.75는 1:3 혼합이다. 체크를 끈 모델은 생성에서 제외되고 새로 추가한 모델은 독립 ID를 받아 다른 카드의 값을 덮어쓰지 않는다. 효과는 혼합 결과에 한 번 적용된다. 모델 수에 따라 셰이더 계산 비용이 늘어나며 기존 내보낸 파일은 조립기로 열어 다시 내보내야 새 구성을 사용한다.

### Phase 2D — 빛 기반 효과 마스크 (2026-10-15)

사용자 요청: 빛이 비추는 부분에만 효과가 보이는 마스크를 추가한다.

설계: 효과 마스크 목록에 `빛`을 추가한다. 실시간 메인광과 추가 조명의 선형 휘도 × N·L × 거리/스폿 감쇠 × 그림자 감쇠를 합산한다. URP light layers/cookie 및 Forward+ 경로를 따른다. 환경광·반사광·베이크 GI는 포함하지 않는다. 기준 밝기 이하에서는 0, 기준+부드러움 이상에서는 1이다. 부드러움 0은 경계 선택이며 무광/뒷면은 기준 0이어도 0으로 유지한다. 기존 반전과 사용자 프로퍼티 참조를 지원하고 Lit/Toon/Unlit에서 동일하게 동작한다.

색상·발광·림의 표시 영역에 적용한다. 빛으로 형상을 clip하면 ShadowCaster/Depth/Forward 간 입력이 다르고 그림자와 자기 참조가 생기므로 활성 디졸브와의 조합은 명시적으로 검증 오류를 표시한다. 실시간 조명에 의존하는 효과는 Meta 베이크에 포함하지 않는다. 기존 공간 마스크의 clip/Meta 계약은 유지한다.

SSOT/SOLID/FW: 모듈/입력 정의는 Catalog, 조명 계산은 ComposerLighting, 스칼라 마스크 계산은 MaskUtility가 소유한다. Forward 문맥에서 밝기를 한 번 계산해 여러 효과가 공유한다. GUI는 기존 Catalog/프로퍼티 편집을 사용한다. 새 런타임 서비스·DB·CoreSystem·FieldCamera·Architecture 변경은 없다.

Task: (1) Catalog·조명/마스크 HLSL·Forward 문맥 연결 (2) 디졸브 조합 검증·GUI 안내·Meta 제외 (3) 모델별 생성/왕복/공유 입력과 Shader Pass/조명 변형 컴파일 검증 (4) 결과 기록 후 Phase 리뷰 지점에서 멈춤.

Phase 2D 완료: `빛 (실시간 조명)` 마스크와 기준 밝기/경계 부드러움/반전을 추가했다. 조명 휘도는 Forward에서 한 번 계산해 공유하고 기존 사용자 프로퍼티 연결과 레시피 재열기를 사용한다. 활성 디졸브 조합은 검증 오류로 차단하며 빛 마스크 효과는 Meta에서 제외한다.

검증: Unity 6000.0.73f1의 열린 Editor에서 모델 3종 × 효과 3종, 모델별 공유 프로퍼티/공간 디졸브 스택 3종, 기존 예제 1종 = 13개 Shader/78개 Pass 컴파일 오류 0·SRP Batcher 호환 코드 0. 공유 선언 1회·입력 치환·참조 왕복·Meta 제외·공간 디졸브 유지·잘못된 조합 거부·비활성 마스크의 조명 계산 생략을 확인했다. URP 실제 GPU 미리보기에서 검정 Unlit 표면+초록 발광을 사용해 정면 조명/조명 꺼짐/뒷면의 중심 픽셀 G가 각각 1/0/0이었다. D3D11 FXC로 모델별 스택의 63개 vertex/fragment 변형(추가 조명, 추가 그림자, cookie/light layers, Forward+, shadowmask/정적·동적 라이트맵, 각 보조 Pass)을 컴파일했다. GUI 목록의 새 마스크도 확인했다. 실제 씬의 Point/Spot·그림자 가림·베이크 시각 회귀와 다른 플랫폼 검증은 포함하지 않았다. 임시 자동 검증 스크립트는 Assets에서 제거했으며 기록/소스/이미지는 Git 제외 경로 `Library/ToonVerification/LightMask/`에 남겼다. Phase 리뷰 지점에서 멈춘다.

사용법: 효과 카드의 `마스크 → 빛 (실시간 조명)`을 선택한다. `기준 밝기`를 높이면 강한 빛을 받는 부분만 남고 `경계 부드러움`으로 전환 폭을 조절한다. `반전`을 켜면 어두운 영역에 효과를 표시한다. 색상·발광·림에 사용하며 환경광과 베이크 GI는 마스크 밝기에 포함하지 않는다. 추가 조명과 그림자는 URP 설정에서 활성화된 범위만 반영된다. 기존 내보낸 Shader에 새 마스크를 적용하려면 조립기로 열어 마스크를 선택하고 다시 내보낸다.

### Phase 1 — 사용 가능한 첫 조립기

1. Recipe/파라미터/모듈 Catalog를 정의한다.
2. 공용 라이트 모델·마스크·Pass HLSL과 기본 입력 계약을 만든다.
3. 효과별 마스크/프로퍼티를 가진 Shader 생성기를 만든다.
4. EditorWindow의 모델/효과/마스크 GUI, 순서 변경, 미리보기, 레시피 읽기, 내보내기를 연결한다.
5. 실제 Editor 컴파일과 GUI, 대표 모델/마스크/효과 조합의 Shader 컴파일·SRP 호환·클리핑 Pass 일치를 확인한다.
6. 완료를 보고하고 리뷰 지점에서 멈춘다.

### Phase 2 — 후속 확장

사용자 리뷰 뒤 새로운 라이트 모델/효과/마스크, 프로젝트 밖 패키지 내보내기와 필요한 고급 렌더 상태를 추가한다. 첫 버전은 불투명/디졸브 알파클립이다.

### Phase 2A — 사용자 프로퍼티와 입력 참조

2026-10-15 후속 요청으로 승인된 범위: GUI에서 사용자 프로퍼티를 만들고 기본 표면/라이트 모델/효과/마스크의 입력이 어느 프로퍼티를 참조할지 선택한다.

설계: 사용자 프로퍼티는 안정적인 ID, Shader 이름, 표시 이름, 타입, 기본값과 Range/HDR 설정을 가진다. Float/Range/Color/Vector2/Vector4/Texture2D를 지원한다. 입력에는 프로퍼티 ID만 저장하며 빈 ID는 기존 직접 값을 사용한다. 서로 호환되는 타입만 연결한다. 프로퍼티 선언·CBUFFER·Sampler는 한 번만 생성하고 참조된 입력의 자동 프로퍼티는 생략한다. 이름 변경은 ID를 유지한다. 삭제/호환되지 않는 타입 변경은 연결을 해제하고 당시 기본값을 직접 값으로 복사한다. 미리보기/내보내기/레시피 재편집이 같은 연결을 사용한다. 기존 version 1 레시피에는 필드를 추가하는 방식으로 호환한다.

SSOT/SOLID/FW 점검: 기본값과 선언은 Recipe의 사용자 프로퍼티 하나가 소유하고 입력은 ID만 참조한다. 프로퍼티 타입/참조 검증은 별도 Editor 모듈이 담당하고 기존 Generator/Export가 결과를 소비한다. DB·Framework 서비스·CoreSystem·FieldCamera·Architecture는 변경하지 않는다.

Task:

1. 사용자 프로퍼티/참조 데이터와 타입 호환·이름/중복/끊어진 연결 검증을 추가한다.
2. Shader 생성과 기본 표면/라이팅/마스크/효과/Meta/clip의 변수 치환을 공통 참조 해석으로 연결한다.
3. 프로퍼티 추가/수정/삭제 GUI와 입력별 참조 선택, 미리보기 갱신을 연결한다.
4. 기존 레시피 호환, 공유 선언/텍스처 Sampler, 이름 변경/삭제/타입 변경, Material 기본값, 실제 Shader Pass 컴파일을 검증한다.
5. 완료를 보고하고 Phase 리뷰 지점에서 멈춘다.

## Phase 1 결과 — 2026-10-15

구현 완료. 사용자 리뷰 지점에서 멈춘다.

- EditorWindow, Catalog, Recipe, Generator, Export로 책임을 분리했다. 새 서비스나 DB를 만들지 않는다.
- 라이트 모델 3종, 효과 4종, 마스크 5종을 조립한다. 효과는 최대 8개이며 활성화/삭제/순서 변경을 지원한다.
- 기본값과 텍스처 GUID를 결과 Shader 첫 주석에 저장한다. `조립한 Shader 열기`로 이 도구가 생성한 파일을 다시 편집한다. 머티리얼에서 바꾼 값이나 직접 수정한 HLSL을 역변환하는 기능은 아니다.
- 임시 Shader/Material 미리보기는 창이 소유한다. 파라미터 변경은 Material 값만 바꾸고, 모델/효과/마스크/Pass 구성 변경 시 Shader를 다시 컴파일한다.
- 공용 HLSL은 `Render/Shader/Core/Includes/Composer/`에 있다. 결과 Shader가 프로젝트의 이 경로와 URP를 include하므로 이 파일들을 함께 유지한다.
- 예제는 `Render/Shader/View/ComposerExample.shader`와 `.mat`이다. Toon + 높이 색상 마스크 + 노이즈 발광 + 밴드 림 + 텍스처 디졸브 조합이다. 빈 텍스처 마스크는 흰색이므로 실제 디졸브 패턴을 보려면 텍스처를 지정한다.

검증:

- Unity 6000.0.73f1에서 Editor 어셈블리 컴파일, 창 표시와 Material 미리보기, 파라미터 반영, 코드 보기 배치 확인.
- 기본 모델 3개 + 효과/마스크 조합 20개 + 모델별 효과 스택 3개 = 26개 Shader의 156개 Pass를 Unity ShaderUtil로 컴파일했다. 오류 없음, SRP Batcher 호환 코드 0.
- D3D11 FXC로 대표 Lit/Toon 스택의 42개 vertex/fragment 변형을 컴파일했다. 인스턴싱, 추가 조명, SSAO/cookie/light layers, Forward+, 정적/동적 라이트맵, shadowmask, punctual shadow, oct normals/rendering layers, Meta editor visualization 포함.
- 레시피 왕복의 ID/순서 유지, 잘못된 Shader 이름과 중복 ID 거부, 실제 Shader/Material 에셋 내보내기를 확인했다.
- 공유 clip 함수를 모든 Pass에서 호출한다. 실제 GI 베이크와 모든 플랫폼의 시각적 회귀 확인은 이번 검증에 포함하지 않았다.
- 임시 자동 검증 스크립트는 제거했다. 상세 출력은 Git 제외 경로 `Library/ToonVerification/Composer/report.txt`에 있다.

## 사용법

1. `ProjectL > Shader Composer`를 연다.
2. Shader 이름과 라이트 모델을 고르고 기본색/텍스처를 설정한다.
3. `+ 효과 추가`로 효과를 넣고 카드마다 마스크와 값을 고른다. 위에서 아래로 적용된다.
4. 높이/밴드 마스크는 오브젝트 로컬 좌표를 사용한다. 텍스처/노이즈는 모델 UV를 사용하며 텍스처 채널과 반전을 선택할 수 있다. 밴드 두께는 중심의 불투명 구간 전체 폭이고 위/아래 페이드는 각각 선형이다.
5. 미리보기를 확인한 뒤 `Shader 파일로 내보내기`로 Assets 아래에 저장한다. `머티리얼도 함께 만들기`를 켜면 현재 값을 가진 새 머티리얼도 만든다.
6. 수정할 때 `조립한 Shader 열기`로 저장한 Shader를 연다. 기존 머티리얼의 값은 자동으로 덮어쓰지 않으며 새 머티리얼에는 고유한 파일명을 사용한다.

첫 버전은 불투명 표면과 디졸브 clip을 지원한다. 투명 Blend/양면 렌더링/버텍스 변형은 후속 범위다. 모델·효과·마스크 종류를 바꾸면 해당 종류의 고유 파라미터는 기본값으로 초기화된다.

## 사용자 프로퍼티 사용법

### Phase 2C — 사용자 오브젝트 미리보기

후속 Phase 2F에서 입력을 FBX 파일로 한정했다. 아래는 당시 구현 기록이며 현재 사용법은 Phase 2F를 따른다.

2026-10-15 승인된 요청: 미리보기 오브젝트를 직접 넣는다. 조립 창에 GameObject 입력(프리팹/FBX 모델/씬 오브젝트)과 현재 선택 사용, 보기 초기화를 추가한다. 빈 입력은 기존 기본 도형을 사용한다. 하위 MeshRenderer와 SkinnedMeshRenderer의 메시/변환을 스냅샷으로 읽고, Skinned Mesh는 현재 포즈를 별도 Mesh에 Bake한다. 조립 Material을 모든 서브메시에 적용한다. 원본 오브젝트를 복제하거나 동작 스크립트를 실행하지 않는다. 오브젝트의 중심/크기로 카메라를 맞추고 드래그 회전/휠 확대축소를 지원한다. 메시 없는 오브젝트는 안내를 표시한다. 현재 포즈/변환의 재수집은 보기 초기화로 한다.

SSOT/SOLID/FW: Recipe와 Shader 계산은 그대로 유지하고 미리보기 오브젝트는 Window의 Editor 설정이다. 독립 PreviewRenderUtility/스냅샷 Mesh의 수명은 전용 IDisposable이 책임지고 창 종료/대상 해제 시 정리한다. 원본 Mesh와 Scene/Prefab 데이터, Framework/DB/CoreSystem/FieldCamera/Architecture를 변경하지 않는다.

Task: (1) 메시 수집·Bounds·카메라·입력·해제를 담당하는 미리보기 클래스 추가 (2) GameObject 필드와 기존 조립 Material 연결 (3) Unity 컴파일 및 실제 오브젝트 렌더링 확인 (4) 완료 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2C 완료: `ShaderComposerObjectPreview`가 전용 PreviewRenderUtility, 메시 스냅샷, Bounds 기반 카메라와 조작/해제를 담당한다. Window의 `미리보기 오브젝트`에 GameObject를 넣거나 `선택 오브젝트 사용`으로 지정한다. 빈 입력은 기존 MaterialEditor 도형 미리보기다. 조립 Material을 모든 서브메시에 적용하고 LODGroup은 LOD0만 모은다. Unity Editor 컴파일, 기본 도형 표시, 씬 Sphere 지정과 실제 URP 렌더링, 드래그 회전, 휠 확대, 보기 초기화를 확인했다. Inspector에서 원본 Sphere의 Transform와 ToonLitPreview 머티리얼이 유지되는 것도 확인했다. 복합 프리팹과 Skinned Mesh 포즈는 구현했으며 이번 수동 렌더 확인은 씬의 정적 MeshRenderer 대상으로 했다. Shader 계산/내보내기를 변경하지 않았으므로 기존 Shader 조합 컴파일 결과는 유지한다. 사용자 리뷰 지점에서 멈춘다.

오브젝트 미리보기 사용법: Hierarchy의 씬 오브젝트나 Project의 프리팹/모델을 `미리보기 오브젝트` 필드로 끌어 넣는다. 미리보기 안에서 드래그하면 회전하고 휠로 확대/축소한다. `보기 초기화`는 카메라와 현재 포즈/변환의 스냅샷을 갱신한다. MeshRenderer/SkinnedMeshRenderer가 없는 오브젝트는 안내를 표시한다. ParticleSystem/VFX 시뮬레이션은 실행하지 않는다.

### Phase 2B — 프로퍼티 편집 창 분리

2026-10-15 사용자 요청 범위: 프로퍼티 추가/이름/타입/기본값/삭제를 독립 `Shader Properties` EditorWindow로 옮긴다. 조립 창의 `프로퍼티 창` 버튼 또는 `ProjectL > Shader Composer Properties` 메뉴로 연다. 조립 창이 Recipe를 계속 소유하고 프로퍼티 창은 해당 조립 창 참조만 저장한다(SSOT). 새 조립/Shader 열기/Undo 뒤에도 매번 소유자의 현재 Recipe를 읽는다. 편집 Undo는 Recipe 소유자인 조립 창에 기록하고 변경 시 참조 목록·생성 코드·미리보기를 갱신한다. 상태 변경 구독은 창 수명에 맞춰 해제한다. 조립 창이 닫히면 편집을 막고 조립 창 열기를 제공한다. 생성/내보내기/공용 HLSL·Framework·Core 계약은 변경하지 않는다.

Task: (1) 프로퍼티 GUI를 별도 창으로 이동하고 값 컨트롤을 재사용 (2) 같은 Recipe·변경 알림·Undo·재연결 수명 연결 (3) Unity 컴파일과 두 창의 실제 표시 확인 (4) 완료 보고 후 Phase 리뷰 지점에서 멈춤.

Phase 2B 완료: `ShaderComposerPropertiesWindow`로 편집 UI를 이동했다. Recipe는 조립 창 하나에만 있으며 프로퍼티 창은 직렬화된 소유 창 참조를 통해 현재 데이터를 읽는다. 조립 창 변경 이벤트 구독/해제와 소유 창의 Undo 기록, 편집 후 미리보기/참조 목록 갱신을 연결했다. Unity Editor 컴파일과 도구 버튼으로 독립 창 열기를 확인했고 기존 `_Property1`/`_Property2`와 모델의 참조 선택이 유지됐다. 두 창을 따로 배치할 수 있다. 생성 Shader 계산 변경이 없어 기존 48개 Pass 검증은 유지하며 이번에는 창 표시와 Editor 컴파일을 검증했다. 사용자 리뷰 지점에서 멈춘다.

Phase 2A 구현/검증 완료(2026-10-15), 사용자 리뷰 지점이다. `ShaderComposerProperties`가 호환·참조·선언 검증을 담당한다. Unity에서 기존 레시피의 모델 3종, 공유 프로퍼티의 모델 3종, 삭제 후/타입 변경 후 조합 2종을 합친 8개 Shader의 48개 Pass를 컴파일했다. 모두 오류 없이 SRP Batcher 호환 코드 0이다. 프로퍼티 6타입, 공유 텍스처의 단일 선언/Sampler, clip/Meta 참조, ID/연결 레시피 왕복, 이름 변경, 삭제/타입 변경 시 기본값 복사, 11개 잘못된 입력 거부를 확인했다. 실제 Shader 에셋 내보내기/재열기와 새 Material의 Color/Texture 기본값도 통과했다. GUI에서 추가한 프로퍼티 편집 영역과 입력 참조 목록을 확인했다. 테스트용 코드·내보내기 에셋은 제거했고 상세 결과는 Git 제외 경로 `Library/ToonVerification/Properties/report.txt`에 남겼다.

1. 조립 창의 `프로퍼티 창` 버튼 또는 `ProjectL > Shader Composer Properties` 메뉴로 별도 창을 열고 `+ 프로퍼티 추가`를 누른다.
2. Shader 이름(예: `_EffectColor`), 표시 이름, 타입과 기본값을 정한다. 타입은 Float/Range/Color/Vector2/Vector4/Texture2D다. Range는 최솟값/최댓값, Color는 HDR을 설정할 수 있다.
3. 기본 표면/모델/효과/마스크에 나타나는 `○○ 입력` 드롭다운에서 만든 프로퍼티를 선택한다. 호환되는 프로퍼티가 없으면 기존 직접 값 UI를 사용한다.
4. 같은 `_EffectColor`를 발광 색과 림 색에 연결하면 두 효과가 하나의 값을 공유한다. 텍스처도 기본 표면과 여러 마스크가 공유할 수 있다.
5. `직접 값`을 선택하면 입력 자체의 값을 다시 사용한다. 사용자 프로퍼티의 기본값을 바꾸면 연결된 입력들의 미리보기가 함께 바뀐다.
6. 내보낸 Shader/머티리얼에는 사용자 프로퍼티가 한 번만 나타난다. Shader를 다시 열면 프로퍼티 정의와 연결도 복원된다. Shader 프로퍼티 이름을 스크립트의 `Material.SetFloat/SetColor/SetVector/SetTexture`에서도 사용할 수 있다.

숫자 입력은 Float/Range, 색은 Color, 텍스처는 Texture2D를 참조한다. 속도 같은 Vector2 입력은 Vector2/Vector4를, 타일링+이동은 Vector4(XY=타일링, ZW=이동)를 참조한다. 숫자 프로퍼티는 연결한 모듈의 계산 규칙을 그대로 사용하므로 Axis/Channel은 정수 값, 반전은 0/1로 설정한다. Range 프로퍼티는 참조 입력들의 범위를 자동으로 합치지 않는다.

프로퍼티의 이름 변경은 연결을 유지한다. 삭제하거나 호환되지 않는 타입으로 바꾸면 해당 입력에 현재 기본값을 복사하고 `직접 값`으로 전환한다. 직접 값의 슬라이더는 각 입력의 범위로 제한된다. 입력을 다른 모델/효과/마스크로 바꾸면 새 입력 계약에 맞게 정규화되며 같은 키에 남은 참조라도 타입이 맞지 않으면 오류를 표시한다.
