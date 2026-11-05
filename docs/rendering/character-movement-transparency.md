# 캐릭터 이동 미리보기 반투명 — 2026-10-20

## 문제와 설계

사용자 요청: 현재 캐릭터 셰이더에 이동 미리보기 코드와 호환되는 투명 기능을 추가한다.
현재 `Custom/EntityBaseShader`는 알파를 출력하지만 Forward가 블렌딩 없이 `ZWrite On`으로 고정돼 있고,
`UnitModelAppearance`는 Standard/URP만 허용하므로 이동 미리보기에서 예외를 던진다.

- 셰이더 계약: `ProjectLMovementTransparency=True` 태그로 지원을 선언한다. 기본색은 `_Color` 또는 `_BaseColor`,
  렌더 상태는 `_SrcBlend`, `_DstBlend`, `_SrcBlendAlpha`, `_DstBlendAlpha`, `_ZWrite`로 제어한다.
- Shader Composer가 렌더 상태 프로퍼티와 Forward의 동적 Blend/ZWrite를 생성한다. 기본값은 One/Zero,
  ZWrite 1로 기존 불투명/디졸브 외형을 유지한다. 기본색 참조가 위 이름일 때만 지원 태그를 생성한다.
- 현재 EntityBaseShader도 동일한 생성 계약으로 갱신한다. 기존 조명·림·디졸브 계산과 원본 머티리얼 값은 유지한다.
- UnitModelAppearance는 지원 태그와 필수 프로퍼티를 검사한 뒤 기존 전용 복사 머티리얼에만 알파 블렌딩,
  ZWrite 0, Transparent 큐를 적용한다. 투명 복사본의 그림자·깊이·외곽선 노멀 패스를 비활성화한다.
- 기존 ColorSystem 손잡이·PropertyBlock 합성·복원·Dispose 수명을 유지한다. 지원하지 않는 셰이더는 계속 예외로 보고한다.

SSOT/SOLID/FW/독립성: 셰이더의 지원 선언은 태그, 상태 생성은 Composer, 외형 대여·복원은 UnitModelAppearance가 소유한다.
코드의 셰이더 이름 허용 목록을 늘리지 않고 명시적인 지원 계약으로 확장한다. 새 서비스·Factory 등록은 없다.
FW-010~012 및 ColorSystem 경로를 유지하고 게임 Core·FieldCamera·Architecture·씬·DB 에셋은 수정하지 않는다.
반투명 기능 변경은 생성기/캐릭터 셰이더/외형 구현과 해당 검사에 머문다. 조명·공격 조립을 변경하지 않는다.

## Phase 1 Task

1. Composer 생성 계약 및 EntityBaseShader 반영.
2. UnitModelAppearance 지원 검사와 복사본 렌더 상태 연결.
3. 실제 캐릭터 셰이더로 반투명·피격 플래시 동시 적용 및 복원, 미지원 거부 회귀 검증.
4. Unity 컴파일·EditMode 검사·GPU 알파 합성을 확인하고 결과를 기록한 뒤 리뷰 지점에서 멈춘다.

## 검증 기록

- Phase 1 완료. Unity 6000.0.73f1에서 컴파일 오류/Shader 오류가 없음을 확인했다.
- 실제 Unity Test Runner EditMode 9건 통과, 실패/건너뜀 0. 증거는 Git 제외
  `Library/MovementTransparencyCheck/results.xml`, `complete.txt`에 있다.
- Direct3D11 GPU로 실제 캐릭터 Forward를 그려, 이동 복사 머티리얼과 ColorSystem의 PropertyBlock이
  `배경 × (1 - 알파) + 캐릭터 색 × 알파`로 합성되는지 픽셀을 읽어 확인했다.
- 피격 플래시/이동 투명도의 중첩·개별 종료, 원본 재질과 블록 복원, 풀 반환 관련 기존 회귀,
  미지원 셰이더 거부, EntityBaseShader의 Composer 재생성 일치가 통과했다.
- Unity가 지원 태그의 `True`를 `true`로 반환하는 것을 실제 검사에서 확인해 대소문자 무관 비교로 보완했다.
- 임시 Test Runner 연결 코드는 Assets에서 제거했다. 씬/DB/원본 머티리얼은 변경하지 않았다.
- 실제 전투 입력으로 이동 미리보기의 미관을 평가한 것은 아니다. Phase 1 리뷰 지점에서 멈춘다.

## BaseShader 대상 정정 — 2026-10-20

사용자가 첨부한 실제 예외의 셰이더는 `Custom/ProjectLBaseLit`이며 대상은 `BaseShader.shader`다.
앞선 EntityBaseShader만의 수정과 검증으로 이 셰이더의 호환까지 확인했다고 해석하면 안 된다.
BaseShader에도 기존 생성 계약의 다섯 렌더 상태 프로퍼티, 지원 태그, Forward Blend/ZWrite를 적용한다.
게임 코드의 추가 분기나 예외 무시는 넣지 않는다. 기존 `_BaseColor`로 이동 투명도·플래시를 합성한다.
검증은 두 셰이더를 각각 지정해 실행하며, BaseShader의 GPU 합성과 원본 복원, 재생성 일치를 별도로 확인한다.

- BaseShader 후속 검증 완료. `ColoredCharacters.mat`의 Shader GUID도 BaseShader의
  `838930d2b68b3e34680ce0d5e4582d7c`와 일치함을 확인했다.
- Unity Test Runner EditMode 12건 통과, 실패/건너뜀 0, Direct3D11.
  `Custom/ProjectLBaseLit`를 명시한 GPU 알파 합성·피격 플래시 중첩·복원과 BaseShader 재생성 일치가 포함된다.
- 증거: Git 제외 `Library/MovementTransparencyCheck/BaseShader/results.xml`, `complete.txt`, `shader.txt`.
  실제 Unity에서 BaseShader의 지원 태그 및 다섯 렌더 상태 프로퍼티가 조회됨을 기록했다.
- BaseShader의 기존 라이팅/림과 `_BaseColor`/`_BaseMap`은 유지했다. 기본 불투명 렌더 상태도 유지하며,
  이동 복사 머티리얼에만 알파 블렌딩과 ZWrite 0이 적용된다. 씬/DB/원본 머티리얼은 수정하지 않았다.
- 임시 Test Runner 연결 코드를 Assets에서 제거했다. 실제 전투 입력의 미관 평가는 별도다.
