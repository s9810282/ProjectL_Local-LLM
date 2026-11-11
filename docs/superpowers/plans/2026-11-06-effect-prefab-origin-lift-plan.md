# 전투 효과 프리팹별 시작 높이 보정 계획 (2026-11-06)

## 설계

- 요청 대상은 `Sacrifice`, `KnockBackShild`, `Cure`, `Increased movement`, `Reduced movement`, `BallistarHit` 여섯 프리팹이다. 각각 효과 전체의 시작 위치를 월드 위 방향으로 `0.05` 올린다. 기존 프리팹 내부 자식 Transform 값은 바꾸지 않는다. `KnockBackShild`의 기존 SO Y `+0.05`는 0으로 옮겨 중복 보정을 막고, 다른 전투 큐·상시 SO는 바꾸지 않는다.
- 여섯 프리팹 모두 **루트에 ParticleSystem과 ParticleStopNotifier**가 있다. `EffectPool.CreateElement`는 두 컴포넌트를 루트에서 찾고, `EffectScope.Play`는 그 루트를 부모 아래의 요청 위치로 옮긴다. 빈 오브젝트를 새 루트로 넣으면 이 계약과 자연 종료·풀 수명이 깨질 수 있다. 루트 Transform에 Y를 입력해도 재생 시 덮어쓴다.
- 루트에 선택적인 `EffectPrefabOriginOffset` 컴포넌트를 붙여 `worldOffset=(0,0.05,0)`을 프리팹별로 직렬화한다. 풀은 생성할 때 이를 한 번 읽어 ElementNode에 저장한다. Scope는 재생 요청의 지역 위치에 해당 월드 변위를 부모 지역 벡터로 변환해 더한다. 따라서 루트 ParticleSystem과 모든 자식이 함께 이동하고, 부모 배율·회전과 무관하게 월드 Y `+0.05`다. 컴포넌트가 없는 프리팹은 0으로 기존 동작이다.
- 위치 기준의 SSOT는 프리팹 컴포넌트다. 프리팹은 보정 데이터, 풀은 한 번 조회·캐시, Scope는 놓기 책임만 맡는다(SRP). 다른 효과에는 새 컴포넌트가 필요 없고 데이터만으로 추가 대상에 적용 가능하다(OCP). 상속 계층을 바꾸지 않아 LSP에 영향이 없고, Scope는 이미 갖고 있는 ElementNode의 한 값만 읽는다(ISP·DIP). `EffectScopeRequest`, 서비스 등록·수명, Core·Buffer·FieldCamera·Architecture 계약은 유지한다. 다른 효과의 SO와 Prefab을 읽거나 수정하지 않아 변경이 독립적이다.
- `BallistarHit`은 접촉 효과라 G2의 바닥 기준 전환 전후로 최종 위치가 바뀔 수 있다. 이 Phase의 `+0.05`는 그 기준점 위에 항상 적용된다. `Increased movement` 프리팹의 기존 미커밋 자식 Transform 변경은 보존한다.

## Phase EFP1 — 여섯 효과 프리팹 높이

1. 선택적 프리팹 보정 컴포넌트와 Unity `.meta`를 추가한다. 값은 월드 변위이며 기본값 0이다.
2. 풀 생성 때 보정을 캐시하고 Scope가 위치에 한 번 적용한다. 기존 루트 ParticleSystem/Notifier, 자식 계층, SO는 유지한다.
3. 여섯 프리팹 루트에 각각 `worldOffset.y=0.05`를 입력한다. 굳건한 의지 SO의 기존 Y `+0.05`는 0으로 옮긴다. 다른 프리팹 값은 기본 0이다.
4. YAML 참조·대상 수·변경 차이와 `git diff --check`를 확인하고 멈춘다. View 시험 코드와 전투 View MSBuild는 실행하지 않는다.

Unity 확인: 각 효과의 루트 및 보이는 파티클이 기존보다 월드 Y `0.05` 높음(굳건한 의지의 최종 높이는 SO 보정을 옮긴 것이므로 이전과 같음), 부모 배율·회전/고지대, 루프 상시 효과의 중단, `Cure`·`Increased movement`·`Reduced movement`·`BallistarHit` 자연 종료와 풀 재사용. DB 에셋 변경이 있으므로 `DataBaseAssetTests`도 사용자가 확인한다. **컴파일·Unity 재생은 사용자 확인.**

## 상태

- EFP1: 코드·여섯 프리팹·굳건한 의지 SO 반영, 정적 검사 완료. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**

### EFP1 구현 기록

- `EffectPrefabOriginOffset`와 `.meta`를 추가했다. 여섯 프리팹의 기존 루트에 컴포넌트와 `worldOffset=(0,0.05,0)`을 연결했다. 프리팹 루트의 ParticleSystem·ParticleStopNotifier 및 자식 계층은 그대로다.
- `ElementNode`가 풀 인스턴스 생성 때 보정을 한 번 읽어 보관한다. `EffectScope.Play`는 부모의 역변환으로 월드 보정을 지역 위치로 바꿔 요청 위치에 더한다. 컴포넌트가 없는 기존 프리팹은 0 경로다.
- 굳건한 의지 SO의 `positionOffset.y=0.05`를 `0`으로 옮겨 KnockBackShild 프리팹 보정과 중복되지 않게 했다. `Increased movement` 프리팹에서 기존에 수정된 자식 Transform 값은 유지했다.
- 여섯 프리팹 각각의 루트에서 ParticleSystem·ParticleStopNotifier·새 컴포넌트가 정확히 하나인지, 여섯 모두 Render EffectTable 항목에 참조되는지 정적 확인했다. `git diff --check`를 확인했다. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**
