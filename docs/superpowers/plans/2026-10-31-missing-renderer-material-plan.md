# Renderer 누락 머티리얼 연결 Task 계획

설계: [누락 재질 연결 설계](../specs/2026-10-31-missing-renderer-material-design.md).

## Phase 1 — 연결과 검증

1. [x] 저장된 프리팹·씬·재질·패키지 참조를 조사한다.
2. [x] Git 삭제/이름 변경 이력과 Unity 원본 FBX의 Renderer·슬롯·텍스처를 대조한다.
3. [x] 연결 계획과 근거를 저장하고 변경 전 파일을 백업한다.
4. [x] None/Missing인 표시 슬롯만 기존 재질로 연결한다. 정상 재질과 비표시 파티클의 빈 값은 보존한다.
5. [x] 전체 정적 검사와 변경 에셋 Unity 로드 검사, 참조 외 변경 여부 검사를 수행한다.
6. [x] 임시 Editor 조회 도구를 제거하고 결과·잔여를 기록한다.

이 Phase가 끝나면 구현을 멈추고 결과를 보고한다. 커밋·push는 하지 않는다.

## Phase 1 결과 (2026-10-31)

- 씬 3개(`EffectTest`, `JMHTest`, `LYETest`)·프리팹 6개에서 재질 GUID 참조 1,512개를 기존 `.mat`로 연결했다. 단순 Renderer 참조 150개, PrefabInstance 슬롯 오버라이드 1,362개다.
- 프리팹: `Stage/MapProp/NewMap`, `Stage/MapProp/Stage`, `Stage/MapProp/Stage_Test`, `Stage/MapProp/Test/StageTest`, `Stage/Railway/New/Train Head`, `Environment/Gameplay/Rock Wall`.
- 저장된 프리팹·씬 199개를 재검사했다. 끊어진 참조는 0개다. 직접 빈 슬롯 4개는 비활성 Renderer 또는 `ParticleSystemRenderMode.None`이므로 유지했다. URP 패키지의 정상 기본 재질도 유지했다.
- Unity 6000.0.73f1에서 변경 9개 에셋을 실제 로드해 Renderer 5,426개·재질 슬롯 5,427개를 검사했다. 표시 대상 null 재질 0개, null 셰이더 0개, 검증 예외 0개다. JMHTest의 비표시 Arrow 슬롯 1개는 의도된 예외로 집계됐다.
- 원본 조회 시 3ds Max 셰이더에 없는 mainTexture를 읽어 임시 도구가 콘솔 진단을 남겼다. 실제 텍스처 속성 목록만 읽도록 고쳤고 최종 검증은 예외 없이 완료했다. 임시 `.cs`·`.meta`는 제거했다. 운영 코드 변경은 없다.
- JMHTest는 수정 전 dirty=false를 확인하고 외부 변경 후 저장된 씬을 다시 불러왔다. 사용자가 먼저 수정한 씬 내용은 백업·문자열 비교로 보존했다.
- 변경 전후의 모든 에셋에서 GUID를 제외한 텍스트가 동일함을 검사했다. 새 재질 복제, 기존 재질 설정 수정, Core·FieldCamera·Architecture 수정, 커밋·push는 없다.
- 연결 계획·이유: `output/material-assignment/assignment-plan.json`. 변경 전 파일: 같은 폴더 `backups/`. 검증: `static-validation.json`, `unity-validation.json`. 처음의 Assets 전용 중간 조사는 `audit-initial-assets-only.json`로 별도 보관했다.

재질 연결과 Unity 로드 검증까지 완료했다. 전체 게임 플레이·각 카메라에서의 색감 검수는 이번 연결 검사와 구분한다.
