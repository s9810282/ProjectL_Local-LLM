# 포효 이펙트 루트 연결·풀 생성 실패 정리 — 2026-11-10

## 범위와 설계

사용자가 `Stage1BossAttack` 루트 Notifier 연결, 생성 전 검증, 실패 시 정리, 예외 메시지 개선을 요청했다. 한 Phase의 오류 수정으로 처리한다. 코드·프리팹 반영 및 정적 확인 완료, **컴파일 미확인**.

- 원인: 등록 Effect 16개 중 `Stage1BossAttack`만 루트 `ParticleStopNotifier`가 없었다. 자식 `AttackEffect`에는 있지만 풀은 루트만 조회한다. 현재 포효 특성의 EffectType.Stage1Boss1에 연결된다.
- 루트에 Notifier를 추가한다. 파티클/프랍 계층·위치·회전·배율과 자식 Notifier는 보존한다. Scope가 이펙트 전체를 이동·회수하므로 자식 검색으로 우회하지 않는다.
- 원본 구성 검증은 기존 `EffectSystem.ValidateEffectData`를 유지해 담당한다. 함수 처음에 null 검사를 하고, 풀 생성 전에 EffectType, 프리팹, 초기 개수, 루트 ParticleSystem/ParticleStopNotifier를 검사한다. 자식 파티클만 있어도 통과하던 검사를 루트 검사로 바꾼다.
- `EffectPool`은 생성된 인스턴스의 루트 ParticleSystem/ParticleStopNotifier 검사와 실패 자원 정리를 담당한다. 원본 구성 검증을 중복하지 않는다. 생성자의 null 인자 방어와 풀 부모 수명 검사는 유지한다. 데이터/구성 오류는 InvalidOperationException, null 인자는 ArgumentNullException으로 구분한다.
- 생성 후 실패하면 해당 인스턴스를 비활성화하고 파괴한다. 여러 개의 초기 인스턴스를 만들다가 실패하면 이전에 성공한 초기 인스턴스도 함께 정리한다. 런타임 Destroy는 프레임 종료 시 처리되며 EditMode는 DestroyImmediate를 사용한다.
- 진단에는 EffectType 이름/숫자, 프리팹 이름, 누락 컴포넌트 또는 실패 이유를 넣고 생성 예외의 원인은 InnerException으로 보존한다.

SSOT/SRP: 원본 데이터 검증은 EffectSystem, 생성 결과 검사·실패 자원 정리는 EffectPool이 소유한다. OCP/ISP/DIP/LSP: 서비스·Scope·공용 CustomPool 계약 및 정상 대여/반환 흐름을 유지한다. FW 서비스 등록·Factory 조립은 변경하지 않는다. 전투 Core·FieldCamera·Architecture는 변경하지 않는다. 검증은 원본 인수/신규 생성 시점에만 하고 매 프레임/기존 인스턴스 대여마다 전체 프리팹을 검색하지 않는다.

## 구현 결과

- [x] Stage1BossAttack 루트의 ParticleStopNotifier 컴포넌트 및 참조 추가.
- [x] EffectSystem.ValidateEffectData의 원본 검증·EffectPool의 인스턴스 생성 후 필수 컴포넌트 검사.
- [x] 실패한 인스턴스와 부분 초기화 인스턴스 정리.
- [x] 종류·프리팹·원인 정보를 포함하는 예외 메시지.
- [x] ValidateEffectData 유지 및 선행 null 검사. EffectPool의 중복 원본 검증 제거.
- [x] 등록 16개 프리팹의 루트 ParticleSystem/Notifier 참조 정적 재검토, diff 공백 검사.
- [ ] Unity 컴파일 및 아래 재생 확인.

새 Test 코드·MSBuild·Unity 실행은 수행하지 않는다. 기존 시험 및 공용 Framework는 수정하지 않는다. 커밋·push·스테이징 없음.

## 사용자 확인 기대값

1. 포효 특성 재생: 기존 particleStopNotifier 예외 없이 이펙트가 생성되고, 종료 후 전체 루트가 풀로 돌아간다. 두 번째 재생도 정상이다.
2. 다른 이펙트 재생: 기존 위치와 회수 흐름이 유지된다.
3. 필요 시 별도 임시 프리팹에서 루트 Notifier를 제거한 잘못된 구성을 확인한다. 생성 전에 종류·프리팹·필수 컴포넌트가 표시된 오류가 나고 새 인스턴스를 남기지 않아야 한다. 확인 후 원본 설정을 복구한다.
4. 생성 중 예외/부분 초기화 정리 분기는 정적으로 검토했으며 실제 예외 주입으로 확인한 것은 아니다.

## 2026-11-10 책임 경계 보정

사용자가 `ValidateEffectData` 유지와 EffectPool의 생성 인스턴스 검사/실패 정리 분리를 요청했다. 이에 따라 원본 검증을 EffectSystem으로 되돌리고 위 설계·구현 결과를 갱신했다. 루트 Notifier 연결, 상세 예외 및 원인 보존, 부분 초기화 정리는 유지했다. 함수 호출 위치와 원본 검증의 중복 제거를 정적으로 확인했다. **컴파일 미확인**, Test 코드·MSBuild·Unity 실행 및 커밋·push·스테이징 없음.
