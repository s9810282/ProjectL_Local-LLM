# 볼륨메트릭 라이트 — 구현 계획·실행 기록 (2026-11-07)

설계 원본: [투과율·산란광 분리 설계](../specs/2026-11-07-volumetric-light-scattering-design.md).

## Phase 1 — 계산·설정 교체

- [x] P1.1 Settings에 밀도·산란 비율·기여도·방향성·시작 거리·높이 설정과 검증을 제공한다. 사용하지 않는 기존 모델의 설정/명령은 제거한다.
- [x] P1.2 Feature가 현재 카메라의 메인 라이트·GPU 투영·그림자 유효성을 명시 전달한다. 공용 Base/서비스 수명은 바꾸지 않는다.
- [x] P1.3 Compute의 음수 감산을 없애고 투과율과 비음수 단일 산란을 적분한다. 기존 cascade·직교 광선·원본 alpha를 유지한다.
- [x] P1.4 PC Renderer의 해당 Settings만 새 초기값으로 갱신한다. 활성화 값과 GUID는 유지한다.
- [x] P1.5 정적 계약·에셋·diff를 점검하고 Unity 인수 항목을 기록한다. **Phase 종료 후 멈추고 사용자에게 보고한다.**

완료 기준: 코드·에셋 교체와 정적 점검. 컴파일/재생 성공을 의미하지 않는다. Test 코드 작성·MSBuild 실행 없음.

## Phase 2 — 운영 활성화·Unity 인수 (아직 시작하지 않음)

- 목표: 실제 게임에서 효과를 켜고 끄는 소유자와 구간을 정하고 기존 `IRenderFeatureScop`으로 연결한다. `RenderFeatureSystem`의 전역 초기화 정책을 우회하지 않는다.
- 예상 파일: 활성 구간을 소유하는 View 모듈과 그 Factory, 필요 시 Render 서비스 계약의 좁은 연결 지점. 구체 파일/영향은 Phase 1 리뷰 뒤 설계에 기록한다. FieldCamera·Architecture 및 Core는 직접 수정하지 않는다.
- 주의: Render 어셈블리는 Systems를 참조하므로 Systems Factory에 Render 구체 타입 역참조를 추가할 수 없다. 서비스 타입의 소유 계층과 Scope Dispose 순서를 먼저 확정한다. 활성화 정책/공용 계약이 달라지면 구현 전에 사용자에게 확인한다.
- 사용자 Unity 확인: Phase 1 설계 §5의 계산 비교, 운영 진입/이탈, 씬 전환, 재진입 및 Play 종료 시 Scope 해제, 기존 높이 안개 중첩과 최종 밀도 튜닝.

## 실행 기록

- 2026-11-07: 사용자 구현 요청 인수. 설계 → Task 계획 작성. Phase 1 구현 시작. 기존 호출자 검색 결과 새 설정 계약으로 맞출 외부 호출자 없음. 작업 전 Git 변경 없음.
- 2026-11-07: **Phase 1 코드 완료, 리뷰 지점에서 정지. 컴파일 미확인.** Test 코드 작성·수정, MSBuild, Unity 실행, 커밋·push는 수행하지 않았다.
- 변경 코드: `VolmetrickLightSettings.cs`, `VolmetrickLightFeature.cs`, `VolmetrickLight.compute`. 기존 `.meta`와 GUID 유지. 새 C# 파일 없음.
- 에셋 정적 대조: `PC_Renderer.asset`의 9개 YAML 구간 중 `2433005988638492109`(볼륨메트릭) 한 구간만 변경했다. 다른 Renderer Feature 설정과 등록 목록은 동일하다. 해당 기능의 `m_Active=1`, `IsAddRenderPass=0`, Script/Compute GUID도 유지했다.
- CPU에서 바인딩하는 인자 20개가 HLSL 선언과 대응한다. 옛 강도/그림자 setter 호출자 잔여 없음. 설치된 URP에서 사용 API의 접근성과 인자, Core RP의 높이·투과율·위상 함수 정의를 대조했다. 이는 컴파일 검증이 아니다.
- `git diff --check` 공백 오류 없음. 변경 6개 파일 모두 LF·UTF-8(BOM 없음), U+FFFD 없음. 로컬 Git의 `core.autocrlf`에 따른 향후 CRLF 변환 안내는 있으나 현재 파일은 LF다.
- 잔여 한계: 전체 해상도 레이마칭 비용 미측정, 메인 Directional Light만 지원, 환경광/추가 광원/투명 오브젝트 fog 미지원. 기존 높이 안개와 겹치면 감쇠가 중첩될 수 있으므로 Unity 화면에서 비교해야 한다.

## 사용자 Unity 인수 순서

1. Unity 스크립트·Compute 임포트 오류를 확인한다. 이번 보고는 **컴파일 미확인**이다.
2. 재생 초기화가 끝난 뒤 PC Renderer의 VolmetrickLightFeature에서 `Is Add Render Pass`를 잠시 켜 계산을 비교한다. 운영 활성화 연결은 아직 없다.
3. Density=0에서 기능을 끈 화면과 같고, 기본 Density=0.002에서 빛줄기가 약하게 나타나는지 확인한다. 전체 화면이 짙게 변하면 먼저 밀도를 낮추고 `Scattering Intensity`로 광원 기여도만 조절한다.
4. 광원 색·세기를 바꾸어 산란광이 따라가는지 확인한다. 그림자가 빛줄기를 가리되 별도 RGB 감산으로 색을 찌그러뜨리지 않는지 본다. 광원이 없거나 산란 강도=0이어도 매질 감쇠는 남는 것이 현재 모델이다.
5. Height Falloff=0에서 균일 매질을 비교하고 Start Distance, Base Height, Anisotropy를 각각 바꾼다. 1/4 cascade, 직교/원근, GameView/SceneView도 설계 §5대로 확인한다.
6. 현재 프레임의 표시 비교를 마치면 임시 활성화를 끈다. 씬 진입/이탈의 정상 활성화·반환은 Phase 2에서 연결한다.

## 렌더 작업자 공유 메모 (아직 외부 전달하지 않음)

- 볼륨메트릭의 서비스 타입·키와 기존 Scope 계약은 유지했다. 등록/Factory/Group은 수정하지 않았다.
- Settings API는 설계 §3.2의 새 매질 설정으로 교체했다. 옛 `SetLightingIntensity*`, `SetShadow*`, `SetShadowsColor` 호출이 다른 브랜치에 있으면 새 물리량과 아트 조절값을 선택해 이전해야 한다. 이름만 기계적으로 치환하면 단위가 달라 같은 화면을 보장하지 않는다.
- Feature의 직렬화 필드 이름은 camelCase로 바꾸고 `FormerlySerializedAs`를 추가했다. PC 에셋도 새 이름으로 저장했다. 기존 GUID와 클래스/네임스페이스는 유지했다.
- 운영 활성화는 미연결이다. Scope 획득/반환 소유자, 활성 구간, 어셈블리 의존 방향은 Phase 2 설계에서 확정한다.

## Phase 1 후속 — RenderGraph 기록 오류 수정 (2026-11-07)

- 사용자 전달: `ArgumentException: Trying to use an invalid resource (pass DrawOpaqueObjects)`.
- 원인 조사: 기존 Unity `Editor.log`에서 이 오류 전에 `cameraColorTargetHandle` 접근 오류 → `UniversalCameraData.IsHandleYFlipped`의 NullReferenceException → 볼륨메트릭 `RecordRenderGraph`의 투영 계산(수정 전 148행)을 확인했다. 설치된 URP의 `GetGPUProjectionMatrix()`가 `IsCameraProjectionMatrixFlipped()`를 거쳐 호환 모드 타깃을 조회하는 것이 최초 오류다.
- 연쇄 해석: 설치된 URP의 `RecordAndExecuteRenderGraph`는 기록 중 예외가 나면 `EndRecordingAndExecute`에 도달하지 않는다. 전역 자원 목록 정리는 실행 종료 경로에 있어, 뒤 카메라/프레임의 `UseAllGlobalTextures`가 이전 핸들을 읽는 `DrawOpaqueObjects` 오류로 이어질 수 있다. 첨부 스택은 후속 오류이며, 최초 오류는 로컬 Editor 로그로 확인했다.
- 수정: 기존 Feature에서 `GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true)`를 사용한다. 중간 텍스처만 처리한다는 기존 조건을 활용해 호환 모드 타깃 조회를 없앴다. 투영 계산도 Compute builder 생성 전에 완료한다. 산란 설계·설정·Scope 수명 변경은 없다.
- 정적 확인: 해당 호환 모드 호출 제거 및 패키지 소스 경로 대조, diff 공백 점검. **수정 후 컴파일·재생 미확인.** Test 코드 및 MSBuild 실행 없음. 이미 기록이 중단된 RenderGraph 상태가 남는다면 Unity 에디터를 재시작해 확인한다.
- 이번 후속에서 코드 한 파일과 설계·계획 문서만 수정했다. 사용자 실행 후 생긴 FMOD 캐시·폰트 에셋 변경은 수정하거나 되돌리지 않았다. Phase 2는 시작하지 않았다.

## Phase 1 후속 — 빛줄기 가시성·셰이더 경고 (2026-11-07)

- 사용자 피드백: 빛줄기가 잘 안 보임. D3D11 Compute 92행 signed 정수 나눗셈/나머지 성능 경고 2개.
- 조사 당시 PC 에셋: Density=0.002, ScatteringIntensity=1, Anisotropy=0.9, Albedo=1, IsAddRenderPass=1. 방향성 0.9는 정면 산란을 좁게 집중하므로 탑다운 등 다른 시선에서는 약해질 수 있다. Boot에 저장된 카메라는 직교·45도, Directional Light는 강도 1로 확인했으나 현재 런타임 시선/광원 값은 관찰하지 않았다. 특정 단일 원인으로 확정하지 않는다.
- 코드 수정: 음수 cascade를 반환한 뒤 `uint` 인덱스의 `& 1u`, `>> 1u`로 atlas 좌표를 계산한다. 0~3의 기존 배치는 동일하며 signed 나눗셈/나머지 연산을 제거한다.
- 프리셋 조정: Settings 기본값과 PC 에셋의 ScatteringIntensity=8, Anisotropy=0.2. 밀도를 유지해 배경 투과율을 더 줄이지 않으면서 산란 기여도를 높인다. PC의 사용자 변경 Albedo=1, IsAddRenderPass=1을 보존했다. 이 활성화 값은 운영 Scope 연결 완료를 뜻하지 않는다.
- 확인: 정적 diff·연산 교체·설정 일치 점검. **컴파일 미확인, 수정 후 경고 소멸·실제 가시성은 Unity 미확인.** Test 코드·MSBuild·Unity 실행 없음. 사용자는 같은 시점에서 효과 on/off 및 Anisotropy=0/0.2, ScatteringIntensity=8을 비교한다. 햇빛을 가리는 물체의 실시간 그림자가 없는 구간에서는 빛줄기 모양이 약할 수 있다.
- 이번 후속은 기존 설계의 설정 튜닝과 동일 결과의 정수 연산 교체다. 공용 수명·활성화 연결을 변경하는 Phase 2는 시작하지 않았다.

## Phase 1 후속 — 밝은 주간 참조 이미지 반영 (2026-11-07)

- 사용자 목표 명확화: 밝은 장면의 off/on 참조 이미지처럼 공기층과 넓은 빛줄기가 드러나는 결과를 원한다. 원래 색 보존을 우선해 효과를 매우 약하게 만드는 목표로 해석했던 부분을 정정했다. 설계 §1.1에 이미지 관찰과 인수 기준을 기록했다.
- 설정 계획: 기존 매질·산란 모델의 네 값만 주간 비교용으로 조정한다. 별도 광원·합성 모드·볼륨이나 운영 Scope는 추가하지 않는다.
- 반영 완료: Settings 기본값 및 PC 에셋 Density 0.002→0.006, HeightFalloff 0.05→0.02, ScatteringIntensity 8→12, Anisotropy 0.2→0. 등방 산란으로 시선 방향에 따른 약화를 제외하고 그림자 분포의 빛줄기를 먼저 비교한다. 사용자 변경 Albedo=1 및 IsAddRenderPass=1은 보존했다.
- 인수 기준: 밝은 표면 앞에서도 공기층이 드러나고 가림에 따른 밝고 어두운 줄기가 나뉘는가, 전체 화면 밝기만 오르지 않는가, 유닛과 표면 구분이 유지되는가. 실제 게임의 같은 구도에서 on/off를 비교해야 하며 참조 일치를 아직 완료 처리하지 않는다.
- 검증: diff·프리셋 값·LF/UTF-8 정적 점검. **컴파일 미확인, 수정 후 실제 화면 미확인.** Test 코드·MSBuild·Unity 실행 없음. 기존 Phase 2의 운영 활성화 연결은 시작하지 않았다.

## Phase 1 조사 — on/off 차이 없음 제보 (2026-11-07)

- 사용자 제보: 효과가 있어도 없어도 동일하게 보임. 추가 프리셋 덮어쓰기 대신 실행 경로를 재조사했다.
- 코드에서 확인: `BaseRenderFeature.AddRenderPasses`는 내부 `IsAddRenderPass=false`이면 등록하지 않는다. `RenderFeatureSystem` 초기화도 이 값을 false로 설정하며 볼륨메트릭 Scope 소비자는 아직 없다. 다만 사용자가 재생 후 수동으로 켰는지는 코드만으로 알 수 없다.
- 기존 Unity 화면을 읽기 전용으로 관찰했다. 관찰 시점에는 JMHTest의 Scene 뷰가 열려 있고 Feature 제목 체크는 켜져 있지만 내부 `Is Add Render Pass`는 꺼져 있었다. Density=0.0006, StartDistance=0이었다. 앞서 전달한 밀도 0.006보다 10배 낮다. 비교 과정에서 잠시 꺼둔 순간일 가능성이 있으므로 과거 on 상태에도 실행되지 않았다고 단정하지 않는다.
- 비교 조건 확인 요청: 내부 패스 켜짐 및 Density=0.006 상태에서도 차이가 없는지. 0.0006의 60거리 균일 매질은 높이 감소 전에도 불투명도가 약 3.5% 이하이므로 밝은 화면의 차이가 작을 수 있다.
- 코드·프리셋 변경 없음. UI 클릭·값 변경·재생 시작·컴파일 실행 없이 현재 화면만 읽었다. **컴파일 미확인**, 조건을 맞춘 on/off 비교와 원인 확정은 남아 있다.

## Phase 1 조사 — 씬 배치·실제 프레임 근거 (2026-11-07)

- 사용자 요청: 안 보이는 이유를 씬 배치와 복합 요소까지 확인해 근거로 제시한다. 상세 증거와 한계는 [가시성 조사](../specs/2026-11-07-volumetric-light-visibility-investigation.md)에 기록했다.
- JMHTest의 저장된 프리팹 루트 167개/소스 23종, 광원 4개, 카메라 2개, 직접 직렬화된 MeshRenderer 36개와 연결된 안개·표면 셰이더·Volume·PC Pipeline을 대조했다. 미저장 씬의 카메라 Y=25.89는 UI와 실제 GPU 입력으로 구분했다.
- 거리 설명 정정: 직교 카메라 Near→Far=56.35 < LightingRange=60. 중심 Y=0까지 계산 경로도 약 36.3이다. 현재 게임 뷰의 부재를 거리 제한으로 설명할 근거는 없다.
- 에디터 정지 상태에서 해당 Feature의 제목/내부 체크만 잠시 켜 프레임을 비교했다. Compute와 실제 2048 그림자 맵·캐스케이드 행렬·주광원 입력을 확인했다. 변화는 존재하지만 전체 haze가 주로 보였고 참조 같은 줄기는 분명하지 않았다.
- 기존 높이 안개 2회가 새 볼륨 뒤에 합성되고, 그 안개 자체는 주광원 그림자를 읽지 않는다. 열린 중앙 광장과 주광원 고도 약 52.3°, 실제 DOF/Bloom도 확인했다. 각 요인의 픽셀별 기여율이나 단일 주원인은 미확정이다. 안개/후처리 개별 제외 및 visibility 분리 출력은 수행하지 않았다.
- 임시 체크는 관찰 시작 당시 상태(둘 다 꺼짐)로 복구했고 Frame Debugger를 비활성화/닫았다. 진단 전후 PC Renderer 파일 문자열은 동일하다. 씬 저장·재생 시작·소스/프리셋 변경 없음. **컴파일 미확인**, Test 코드·MSBuild·커밋·push 없음. Phase 2는 시작하지 않았다.

## 가시성 추가 조사 — 산란·그림자 분리 진단 계획 (2026-11-07)

- 사용자 요청: 산란광과 그림자 계산, 기존 안개/후처리 영향까지 추가 조사한 뒤 결론을 도출한다.
- 단일 조사 Phase: (1) 현 파일/활성 상태 보관, (2) 기존 Compute의 최종 출력만 일시적으로 산란광·경로 가림률·표면 그림자 진단 화면으로 교체, (3) 에디터 정지 상태의 Frame Debugger에서 투명/후처리 전후를 비교, (4) 임시 소스·설정 복구, (5) 증거와 결론 갱신 후 보고한다. 필요한 경우 원래 계산과 강제 비가림 결과도 대조한다.
- 목적은 가시성 원인 분리다. 테스트 코드·운영 기능·새 서비스/설정 계약은 추가하지 않는다. 기존 매질·조명·배치 값을 상시 변경하거나 Phase 2 Scope를 구현하지 않는다. Core·FieldCamera·Architecture는 수정하지 않는다. 진단 수식은 해당 기능 안에서만 일시 사용하고 원래 코드로 복구한다(기능 독립성·SSOT·FW 수명/조립 계약 유지).
- Unity Play·명시적 재컴파일·MSBuild·Test Runner는 실행하지 않는다. 열린 에디터가 파일 변경을 반영한 화면을 관찰하며, 프로젝트 컴파일/재생 확인 상태는 **컴파일 미확인**으로 유지한다.

### 추가 조사 완료 — 리뷰 지점

- 실제로 사용한 임시 출력은 산란광 S, 산란 가중 평균 shadow visibility, 표면 shadow visibility 세 가지다. 배경/투명 안개/후처리를 Frame Debugger 단계로 분리했다. 강제 비가림·추가 차광물은 필요하지 않아 적용하지 않았다.
- 산란 S에 왼쪽 사선 띠가 존재하고 표면 그림자도 물체에 대응한다. 중앙의 평균 visibility는 거의 흰색이다. 중앙이 열린 배치와 약 52.3° 주광원 고도가 중앙 차광 대비 부족의 우선 근거다.
- 높이 안개 두 draw 전후에도 산란 띠가 남았다. 안개가 전부 덮는다는 가설을 주원인에서 제외한다. 원래 배경 합성 시 후처리 전부터 띠가 약하며, 최종 후처리는 추가로 밝기·색·경계를 바꾼다. 개별 Bloom/DOF 기여율이나 모든 픽셀의 정확성은 단정하지 않는다.
- 건물 7개의 보수적 FBX 경계와 중심 광선의 61개 지점에서도 빛 방향 교차가 0이었다. 소품 전체나 Unity 임포트 삼각형 검증이 아닌 보조 근거다.
- 최종 결론·캡처·한계는 [가시성 조사 §9](../specs/2026-11-07-volumetric-light-visibility-investigation.md#9-추가-분리-진단-결과--2026-11-07)에 기록했다. 앞선 조사에서 미수행이라고 적었던 분리 진단은 이 후속에서 수행됐다.
- 임시 Compute를 조사 시작 원본으로 바이트 단위 복구했고 SHA-256 일치를 확인했다. PC Renderer도 백업과 바이트 일치한다. 이번 추가 조사 시작/종료 라이브 체크 둘 다 켜짐을 보존했다. Frame Debugger는 비활성화/닫음. 미저장 씬 저장·폐기, 제품 코드 영구 변경, Test 코드·MSBuild·Play·커밋·push 없음. **컴파일 미확인.**
- 조사 Phase 완료에서 멈춘다. 차광 연출/매질 통합은 후속 설계 대상이며 Phase 2 활성화 연결은 시작하지 않았다.
