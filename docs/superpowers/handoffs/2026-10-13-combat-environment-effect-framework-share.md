# BOOM 등록 E1 — Framework/Render 공유 메모

- 작성: View 작업, 2026-10-13
- 수신 대상: Framework/Render 담당 민형님. **미전달**.
- 승인 범위: [새 계획의 E1](../plans/2026-10-13-combat-environment-explosion-plan.md#phase-e1--기존-경로로-boom-등록재생-확인). 이전에 폐기한 Scope 확장 작업은 포함하지 않는다.
- 상태: 에셋·DB 및 사용자 지시의 E2 전투 연결 완료. 일반 배율의 기존 재생 경로 검증 통과. E1의 부모 배율 재사용 크기 문제와 실제 화면 검증은 미해결로 유지.

## 1. 실제 변경

- 기존 `EffectType`에 `Boom = 2` 추가. Empty=0/TestFire=1 유지.
- `Assets/ProjectL/Script/Render/Effect/Boom/BOOM.prefab`의 기존 루트에 `ParticleStopNotifier` 컴포넌트 1개 연결.
- 원래 파티클 값·계층·재질·GUID 유지. 추가한 컴포넌트와 루트 참조를 제외한 내용이 HEAD와 동일함을 확인했다.
- `Assets/ProjectL/DataBaseSO/RenderGroup/EffectTable/Items/BoomEffect.asset` 신규 등록. 원본 BOOM 참조, 초기 수 0, MaxCount 설정값 16. 기존 EffectDataTable에 항목 하나 추가.
- DB 교차 검사 1개와 실제 BOOM 재생·재사용 테스트를 추가했다.

**EffectScope, ParticleStopNotifier, EffectPool, EffectSystem 구현은 모두 변경하지 않았다.** 후속 사용자 지시로 E2의 화약통 파괴·지뢰 발동 연결을 추가했다. 사건 해석 함수와 공통 Presenter가 기존 Scope의 Play/StopAll만 사용하며, 수명 대기·Tick·공용 API 변경은 없다. 결과와 검증 한계는 [계획 §7.3](../plans/2026-10-13-combat-environment-explosion-plan.md#73-2026-10-13--e2-사건-해석전투-표시-연결)에 기록했다.

## 2. 검증 및 기존 배율 제약

독립 Unity 6000.0.73f1에서 실제 원본 프리팹과 RenderGroup, 현재 빌드 DLL을 사용했다. 기존 서비스로 GetEffectHandle→Play를 실행했다.

- 신규 DB 검사 통과: 그룹 조립·Boom 키·정확한 원본 참조·루트 PS/Notifier·비루프 계층·풀 설정·기존 enum 번호.
- 일반 배율: 자연 반환 2회, 같은 인스턴스 재사용, 루트와 네 자식 방출, 반환 시 잔여 입자 0, 요청 위치·회전, 동시 3개, StopAll 반복 및 재사용 통과.
- BOOM의 TrailModule은 다섯 PS 모두 비활성이다. 별도 trail 유지 시간을 추정하지 않았다.
- 부모 배율 3: 첫 자연 반환은 잔여 입자 0으로 성공하지만 두 번째 Play의 월드 배율 유지 검증이 실패했다. 전후 lossyScale 거리 1.15470076(허용 오차 0.001)이다.

코드상 기존 Play는 `SetParent(target)`로 월드 배율을 유지해 로컬 배율이 1/3이 된다. 반환은 `SetParent(poolRoot, false)`로 그 로컬 배율을 유지한다. 다음 Play에서 작아진 월드 배율을 다시 유지하므로 반복할수록 축소된다. 이 코드는 이번 E1에서 수정하지 않았으며, 새 Notifier 알고리즘이나 자식 종료 관측이 필요한 증거로 해석하지 않는다.

현재는 재현 테스트와 기록만 남긴다. 공용 풀의 변환 복원 등 수정이 필요하다면 별도 범위를 승인받아 진행한다. Unity 화면의 재질·방향·크기에 대한 육안 확인 및 원본 프로젝트의 전체 Test Runner 실행은 남아 있다.

최종 증거·로그·Task 상태 원본은 [계획 §7.2](../plans/2026-10-13-combat-environment-explosion-plan.md#72-2026-10-13--e1-boom-등록-및-기존-경로-검증)다.
