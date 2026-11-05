# 이동 문맥의 실제 프로필 읽기 추적 설계 — MC2B (2026-11-02)

- 상태: **사용자 최종 진행 지시로 §9의 추가 파일을 포함한 MB1·MB2 구현과 Unity 컴파일 확인 완료.** 최종 기존 EditMode 5,619 통과/8 실패이며 후속 사용자 진지 구축 정상 확인을 반영했다. Core 전달 요약과 rank별 전수 등 잔여 인수는 아래 전달 문서를 따른다.
- 상위 결정/비교 계약: [정의 비교 설계](2026-11-02-movement-context-definition-comparison-design.md). 감사 근거: [구현 인계 §10](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#10-mc21-프로필-의존-감사와-중단-2026-11-02).
- 구현 순서/상태 원본: [MC2B 구현 계획](../plans/2026-11-02-movement-profile-read-tracking-plan.md).
- 범위: 실제 공개 이동 투영이 읽은 **프로필 설정과 선택 주체**의 기대값 보강이다. 이동·피해·특성 실행 규칙이나 모든 런타임 사실의 의존 추적을 다시 만드는 작업은 아니다.

## 1. 결정과 책임

한 번의 MovementContextCalculator.Capture가 Application 내부의 `CombatProfileReadSet`을 만들고, ProjectRoute의 입장 검사부터 공개 투영 종료까지 공유한다. 기존 RequiredProfile 선택의 **반환 결과**만 기록한다. 계산 중 선택한 프로필을 문맥 수집에서 다시 고르지 않는다.

| 소유자 | 책임 |
|---|---|
| CombatCommandCalculator.RequiredProfile 기존 2인자 메서드 | 보유 프로필 우선·성장 개체 누락 오류·허용된 기본형 대체의 유일한 원본 |
| 새 내부 3인자 RequiredProfile 오버로드 | 기존 2인자 선택을 호출하고 성공한 결과를 전달받은 수집기에 기록 |
| CombatProfileReadSet (신규, internal sealed) | EntityId별 선택 결과를 한 Capture 동안 보관. 특성 종류·효과·공개 여부를 판정하지 않음 |
| ProjectionDependencies / MovementProjection | 동일 수집기를 이동/환경/피해 하위 계산에 연결. 후보 판정은 기존 계산 소유자에 남김 |
| CombatDamageApplication | 이동에서 받은 수집기를 기존 특성 존재·수신 피해·처치 정산 경로에 전달 |
| MovementContextCalculator | 기록을 기존 불변 문맥 목록으로 고정. MC2A의 값 비교를 그대로 사용 |

Contracts/Domain에는 관찰자·수집기·가변 읽기 상태를 추가하지 않는다. CombatCommandInput의 불변 값과 WithActor/WithIntent/WithWorld 복사 계약, MovementPreviewContext 생성자/공개 필드 형식을 유지한다. 새 Framework 서비스·등록·Factory·이벤트·Tick은 없다.

## 2. 읽기 경로와 기록 범위

단순한 이동 함수의 호출 목록으로 끝내지 않고 피해 하위 경로를 함께 연결한다.

| 경로 | 기록할 프로필 선택 | 전달 지점 |
|---|---|---|
| 이동 입장 | 자발적 이동 가능 여부의 행위자 | Capture → ProjectRoute → Actor → UnitBehaviorRules |
| 착지/위치 교환 | 행위자·동승자의 Behavior/발자국 및 행위자 효과 | CanLand, ExchangeEndpoint → PositionSwapShields |
| Leap·정찰·흔적·이동 성공 효과 | 기존 각 계산이 선택한 행위자 | CombatMovementCalculator, ReconSquadApplication, TraitTrailCalculator |
| 환경 강제 이동/토네이도 | 면역 여부·발자국을 읽은 대상 | MovementEnvironmentCalculator, TornadoCalculator → UnitBehaviorRules |
| 환경/장애물 피해 | 수신자와 진지 구축 오라 후보 | CombatDamageApplication → CombatCommandEffects.CaptureIncoming |
| 피해 전후 특성 존재 재계산 | 단독 행동·결속력 출처, 수신자의 결속력 방어 설정 및 도발 후보 | TraitPresenceReconciler → CombatEffectiveStatsCalculator / EnemyAi/TauntTargeting |
| 공개 투영 중 처치 특성 정산 | 사냥꾼, 피의 부름/사기 진작 등의 기존 선택 출처 | CombatDamageApplication → TraitKillSettlement / HunterApplication.TargetDied |

기록은 RequiredProfile이 반환된 직후, 해당 설정으로 효과 유무/범위/횟수를 판단하기 **전**에 한다. 오라가 범위 밖이거나 면역·주기·한도 때문에 결과를 내지 않아도 실제로 프로필을 읽었다면 남긴다. 새 특성별 분기나 후보 목록을 이동 수집기에 만들지 않는다.

TraitPresenceReconciler의 현재 후보는 단순한 Field 유닛 목록과 같지 않다. 추적을 위해 임의의 Area/진영 필터를 추가하지 않는다. 후보가 바뀌어 프로필 읽기 집합이 달라지면 고정한 대상/정의 목록에도 그 차이가 나타난다.

ProjectRoute가 반환된 뒤의 Complete/AfterTransition 전용 처리와 보상 계산은 이번 **공개 투영 추적 수명 밖**이다. 다른 명령/조회에서 RequiredProfile을 사용하는 모든 파일을 일괄 변경하지 않는다. 실제 명령의 게시 검증·정산 계약은 유지한다.

## 3. 내부 API·수명

### B1. 수집기

- `CombatProfileReadSet`은 EntityId → CombatAttackProfile의 내부 사전을 소유한다. 기록은 `Record(EntityId, CombatAttackProfile)`, 소비는 내부 읽기 전용 항목 조회로 제한한다.
- null 프로필/잘못된 식별자는 인자 오류로 거절한다. 정상 호출에서는 RequiredProfile이 이미 유효한 선택 결과를 반환한다.
- 동일 EntityId의 반복 읽기는 한 항목이다. 한 수명 안에서 다른 프로필 참조가 선택되면 혼합 입력/잘못된 수명 연결로 보고 대상 ID를 포함한 InvalidOperationException을 낸다. Profiles는 불변이며 With 계열 복사도 같은 프로필 원본을 유지하므로 정상 공개 투영의 재조회는 같은 참조다.
- 기록을 정상 StaleContext로 숨기거나 첫 값/마지막 값으로 조용히 덮어쓰지 않는다. 프로필 선택 실패는 기존 RequiredProfile 예외를 그대로 유지한다.
- Capture마다 새로 만든다. 비교/동결 후 보관하지 않으며 GC 수명이다. static 사전·ThreadStatic/AsyncLocal·구독·Dispose/Release·캐시·요청 번호는 필요 없다.

### B2. 선택 원본과 전달

기존 2인자 RequiredProfile의 선택 코드는 보존한다. 새 3인자 오버로드는 2인자를 호출한 뒤 `profileReads?.Record(character.EntityId, profile)`하고 같은 객체를 반환한다. 선택 조건을 복제하지 않는다.

ProjectRoute에는 선택적 내부 인자 `CombatProfileReadSet profileReads = null`을 추가한다. Capture만 이를 생성해 넘긴다. 기존 일반 이동 실행/조회 호출은 null이며 계산 결과/예외/사건 순서는 기존과 같다. MovementProjection의 기존 2인자 생성자도 유지하고 새 3인자 구현으로 위임하므로 기존 리플렉션 호출과 호환된다. 기록이 없는 경우의 분기는 명시적인 선택 기능이며 오류를 무시하는 fallback이 아니다.

- 수집기는 Actor/CanLand보다 **먼저** 전달되어 입장·착지에서 읽은 프로필도 포함한다.
- MovementProjection은 동일 수집기를 ProjectionDependencies에 보관하고 이동 하위 계산이 사용하게 한다.
- CombatDamageApplication의 생성자/For에는 선택적 내부 인자를 마지막에 추가하고 읽기 전용 내부 접근만 제공한다. 이동 투영의 피해 객체와 특성 재계산/정산이 같은 수집기를 사용한다.
- 기존 피해 객체를 이어받는 MovementProjection 생성자는 그 피해 객체의 수집기를 이어받는다. 중첩 경계에서 새 수집기를 만들지 않는다.
- 기존 공개 TraitPresenceReconciler.Calculate의 서명은 유지한다. 수집기는 내부/비공개 계산 경로에서만 추가 인자로 전달한다. 공개 메서드에 internal 타입을 노출하지 않는다.
- 구현: TraitPresenceReconciler의 결속력 방어 호출은 CombatEffectiveStatsCalculator.CohesionDefenseBonus까지 수집기를 전달한다. 내부 메서드의 선택적 마지막 인자와 RequiredProfile 호출만 바꾸며, 공개 능력치 API·후보/방어 산식은 유지한다(§9, 후속 사용자 지시로 반영).
- CombatCommandEffects의 이미 선택된 프로필을 받는 Capture는 그대로 둔다. CaptureIncoming의 선택 지점만 수집기 인자를 전달한다.
- TraitKillSettlement/HunterApplication은 이미 받는 피해 객체의 수집기를 사용한다. Complete 전용 Hunter.Reconcile 등은 기본 null 동작을 유지한다.

새 프로필 소비 경로를 추가할 때 수집기를 전달해야 한다는 계약은 정적 호출 경로 검토 대상이다. 컴파일러가 미래의 모든 전달 누락을 자동 검출한다고 주장하지 않는다. 새 특성 설정 비교는 기존 MC1 추상 계약으로 계속 강제한다.

## 4. 기존 MovementPreviewContext에 고정하는 방법

Capture는 공개 ProjectRoute에 새 수집기를 넘기고 성공한 투영의 기록을 아래 기존 목록으로 고정한다.

1. 읽은 EntityId를 기존 EffectTargets 집합에 합친다. 행위자는 이미 Mover/SameMover로 보호되므로 추가로 합칠 때 제외하되 기존 효과 대상에 들어 있으면 제거하지 않는다. 다른 읽기 주체는 입력의 원래 CharacterCombatFacts를 담아 EntityId/CharacterKey/OwnedId 등 선택 주체의 동일성을 기존 SameTarget으로 비교한다.
2. 입력에 없는 읽기 EntityId는 조용히 건너뛰지 않고 InvalidOperationException으로 보고한다. 이번 경로는 기존 입력 개체의 공개 투영이다. 입력 외 새 개체를 만들어 읽는 기능은 별도 계약이 필요하다.
3. 기록된 선택 결과 중 기본형은 Definition을 EntityKey로 중복 제거/정렬해 Definitions에, 보유형은 OwnedId로 중복 제거/정렬해 OwnedProfiles에 담는다. 그룹 안에 서로 다른 선택이 섞인 구성은 정상 불일치로 숨기지 않는다.
4. Capture에서 키/OwnedId로 RequiredProfile 선택을 다시 구현하거나 다시 호출하지 않는다. 선택되지 않은 기본형/보유형을 함께 넣지 않는다.
5. 영혼 수집의 기존 Collectors/Owned 기록·SoulCounts·Main·Souls 수집은 그대로 둔다. 프로필 설정 추적과 보유 진행 사실 수집을 같은 참여 집합으로 합치지 않는다.

EntityId와 프로필의 결합은 기존 대상 사실과 정의/보유 프로필 목록이 함께 보호한다. 예를 들어 같은 값의 기본 정의를 공유한 두 유닛도 대상 사실의 EntityId/OwnedId가 다르면 같은 선택 주체로 취급하지 않는다. MC2A의 SameDefinition/SameProfile 및 TraitSpec.SameValues는 변경하지 않는다.

기본형의 Method는 기존과 같이 전투 준비에 고정된 원본이라는 경계를 유지한다. 기본형은 기존 Definitions 구조를 사용하며, 보유형 Method는 기존 SameProfile 참조 비교를 따른다. 이 변경을 모든 객체의 완전한 의미 동등성이나 필드별 최소 의존 추적으로 확대 해석하지 않는다.

## 5. 공개/숨김 경계와 호환

- 수집기는 공개 Capture의 `ProjectRoute(..., preview: true)`에서만 활성화한다. 숨겨진 지형을 포함한 실제 실행 경로를 추가로 돌려 기록을 합치지 않는다.
- VisibleTo(observer), MovementPreviewContext의 환경 재필터, 지형 난수/경로 검증을 그대로 유지한다. 숨겨진 환경 원본·환경 전체·입력 전체 프로필 목록을 문맥에 붙이지 않는다.
- 기록 대상의 판정은 현재 Core가 실제 읽는 경로다. 프로필에는 환경처럼 KnownTo 계약이 없으므로 새 수집기가 Area/진영을 가시성 필터로 임의 해석하지 않는다. 기존 Core 규칙이 정한 공개 투영과 정의/캐릭터 사실 형식을 사용한다.
- 가변 수집기 자체는 Contracts/Buffer/View에 넘기지 않는다. 기존 문맥 생성자가 복사한 불변 목록만 반환한다. public DTO/생성자/With 복사/Buffer/명령 형식 변경은 없다.
- R3를 구성하는 프로필 의존은 보강하지만, 기존 SameTarget이 다루지 않는 모든 런타임 상태의 비교까지 새로 보장하지 않는다. 별도 상태 비교의 기존 경계는 유지한다.

## 6. 변경 파일 — 구현 시 허용 범위

CoreCaculator 아래 경로다. **실제 구현은 런타임 C# 19개(기존 18·신규 1)와 신규 meta 1개.** 사용자 최종 진행 지시로 CombatEffectiveStatsCalculator를 포함했다. MC2B는 시험/Contracts/Domain/서비스/Factory/View/에셋을 변경하지 않는다.

| 파일 | 변경 이유 |
|---|---|
| Application/Combat/CombatProfileReadSet.cs + meta (신규) | 한 Capture의 읽기 결과 수집 |
| Application/Combat/CombatCommandCalculator.cs | 기존 선택에 기록을 붙이는 내부 오버로드 |
| Application/Combat/ProjectionDependencies.cs | 이동 투영에 전달한 수집기 보관 |
| Application/Combat/MovementProjection.cs | 동일 수집기를 이동/피해 하위 계산에 연결 |
| Application/Combat/CombatMovementCalculator.cs | ProjectRoute·Actor·CanLand·직접 효과 읽기에 전달 |
| Application/Combat/UnitBehaviorRules.cs | 행동/면역의 기존 선택에 전달 |
| Application/Combat/PositionSwapShields.cs | 교환 행위자 프로필 읽기에 전달 |
| Application/Combat/ReconSquadApplication.cs | 정찰 프로필 읽기에 전달 |
| Application/Combat/TraitTrailCalculator.cs | 흔적 프로필 읽기에 전달 |
| Application/Combat/MovementEnvironmentCalculator.cs | 강제 이동 대상의 읽기에 전달 |
| Application/Combat/TornadoCalculator.cs | 면역 대상의 읽기에 전달 |
| Application/Combat/CombatDamageApplication.cs | 피해/존재 판정/처치에 같은 수집기 전달 |
| Application/Combat/CombatCommandEffects.cs | 수신자/진지 구축 후보의 선택에 전달 |
| Application/Combat/TraitPresenceReconciler.cs | 단독 행동/결속력/도발 판정 내부 경로에 전달 |
| Application/Combat/CombatEffectiveStatsCalculator.cs | 결속력 방어 설정의 간접 읽기까지 전달 |
| Application/Combat/EnemyAi/TauntTargeting.cs | 기존 도발 후보의 실제 프로필 읽기에 전달 |
| Application/Combat/TraitKillSettlement.cs | 공개 투영 중 처치 출처의 읽기에 전달 |
| Application/Combat/HunterApplication.cs | TargetDied가 읽는 사냥꾼 설정에 전달 |
| Application/Combat/MovementContextCalculator.cs | 수집기 생성/동결·기존 기대 목록에 반영 |

추가 프로필 소비가 이 경로에서 발견되거나 입력 외 개체/공개 경계 변경이 필요하면 구현 전에 설계/범위를 갱신해 확인한다. 공격·보상 등 현재 공개 ProjectRoute 밖의 읽기 파일을 예방적으로 변경하지 않는다.

## 7. 구조 검토

| 기준 | 적용/남는 경계 |
|---|---|
| Core 구조 — 1순위 | 선택·추적·조립은 Application, 불변 입출력은 Contracts, 규칙은 Domain에 유지. 입력 객체에 가변 관찰자를 넣지 않음 |
| SSOT | 선택은 기존 RequiredProfile, 후보는 기존 효과/존재 판정, 값 비교는 MC1/MC2A. 이동에 특성 후보/필드 지식을 복제하지 않음 |
| S/O | 수집기는 읽기를 기록하고 효과를 해석하지 않음. 새 특성은 기존 선택/효과 경계를 사용하면 이동 분기를 수정하지 않음. 새 프로필 소비 함수는 전달 경로 검토가 필요함 |
| L | 기록 유무에 따라 기존 선택 객체·오류·규칙 결과가 바뀌지 않음. 실제 의존 집합/설정 변경은 문맥 차이로 판단 |
| I/D | 공유 계산에는 프로필 수집기만 전달. 전체 ProjectionDependencies/새 서비스 컨테이너를 넘기지 않음. 인터페이스/등록을 위한 등록은 만들지 않음 |
| JMH/FW | 새 서비스·Builder·수명 모듈·이벤트·렌더 자원 없음. FW 등록/UI/렌더 계약은 직접 적용 대상이 아니며 기존 경계를 우회하지 않음 |
| 기능 변경 독립성 | 특성 수치 변경은 자기 Spec, 선택 규칙 변경은 기존 선택기, 추적/동결 정책은 수집기/Capture에 머묾. 공유 전달 인자의 추가는 이번 공통 경계 변경으로 해당 경로의 통합 확인이 필요함 |
| 캡슐화·C#/Unity·if/Tick | 내부 sealed 수집기·불변 결과·한국어 주석·Allman·기존 GUID 유지. 새 Tick/폴링/전역 상태 없음. null 수집기는 명시적 비추적 호출 경로이며 구성 오류를 정상 성공으로 숨기지 않음 |

대안은 채택하지 않았다. Capture가 오라/특성 후보를 다시 찾으면 규칙 지식이 중복되고, Profiles 열거를 감싸면 SingleOrDefault가 훑은 전체 목록을 실제 선택으로 오인한다. 불변 입력에 관찰자를 넣거나 전역/스레드 상태로 수집하면 Core 입력/수명 경계를 바꾸게 된다. 이번 안은 명시적 Application 인자 전달의 수정 비용을 선택한다.

## 8. 검증·리뷰 조건

MB1에서는 수집기 미활성 상태의 기존 호출 계약/선택 의미 보존과 위 호출 경로의 인자 전달을 정적 대조한다. MB2에서는 실제 읽기 결과의 동결·키/주체 결합·Collector 독립성·숨김 경계를 대조한다. 기존 시험 본문/판정은 수정하지 않으며 새 시험·MSBuild는 없다. 최종 사용자 지시로 Unity 컴파일과 기존 EditMode 시험을 실행한다.

사용자 Unity 인수는 이동하지 않은 진지 구축 출처의 반경/감소율 변경, 범위 밖→안쪽 후보, 도발/단독 행동/결속력 존재 판정, 사냥꾼/처치, 면역·도착 전 종료, 기본형 대체·보유 선택, 동일 설정 재생성, 미사용 프로필/숨겨진 지형만 바뀐 경우를 구분한다. 구체 Task는 구현 계획이 관리한다. **최종 사용자 지시로 MB1·MB2를 이어서 구현하고 마지막에 검증한다. 실제 컴파일/시험 결과와 실행하지 않은 세부 인수를 구분한다.**

## 9. MB1 사전 대조 보완 — 결속력 방어의 간접 읽기 (후속 지시로 구현)

기존 설계의 전달 파일 목록에서 CombatEffectiveStatsCalculator를 빠뜨렸다. TraitPresenceReconciler.Calculate 57행은 수신자마다 CohesionDefenseBonus를 호출하고, 이 내부 메서드 77행은 RequiredProfile로 결속력 설정을 다시 읽는다.

초기 결속력 출처 목록에서 이미 기록했으리라고 가정할 수 없다. 출처 목록은 죽은 개체를 제외하지만 수신자 순회에는 그 개체가 포함되며, CohesionDefenseBonus는 특성 상태가 있으면 프로필을 읽는다. 따라서 이 읽기를 앞선 출처 기록으로 대신 처리하거나, 이동 쪽에 결속력/사망 조건을 복제하면 설계의 실제 읽기/부정 후보 계약을 지키지 못한다.

개정안의 구체 변경은 다음 세 곳이다.

1. CombatEffectiveStatsCalculator의 **내부** CohesionDefenseBonus 마지막에 선택적 CombatProfileReadSet 인자를 추가한다.
2. 같은 메서드의 RequiredProfile 호출을 기존 선택 결과를 기록하는 3인자 오버로드로 연결한다.
3. 원래 계획에 있던 TraitPresenceReconciler.Calculate가 같은 수집기를 이 내부 메서드에 전달한다.

방어 산식·특성/사망 후보 조건·기존 공개 능력치 메서드·다른 호출의 기본 null 동작은 유지한다. MB1은 기존 17 C#에서 18 C#으로 늘고, MB2의 MovementContextCalculator 범위는 그대로다. 사전 대조 당시에는 확인을 기다렸으며, 이후 사용자 "남은 Phase 끝까지 진행하고 마지막에 검증 및 테스트 작업 진행해" 지시로 이 추가 파일을 포함해 구현했다. 현재는 MB1·MB2 코드 및 Unity 컴파일 확인 완료다.

## 10. 구현 결과 (2026-11-02)

설계의 지역 수집기·기존 선택 결과 기록·명시적 전달·기존 목록 동결을 구현했다. Capture 외의 일반 호출은 추적 없이 기존 동작을 유지한다. 기존 MovementProjection 2인자 생성자는 리플렉션 호환을 위해 유지한다. 실제 호출 경로·검증/시험 근거·남은 수동 인수는 [구현 인계 §14](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#14-mb1mb2-구현과-최종-검증-2026-11-02)와 구현 계획이 관리한다.

- 2026-11-02 후속 전달 준비: 사용자 진지 구축 정상 확인과 이번 작업 Git 스테이징 요청을 반영했다. 확인 범위·Core 인수 계약·기존 시험 결과·잔여 인수의 현재 요약은 [Core 구현 전달 문서](../handoffs/2026-11-02-movement-stale-context-core-implementation-handoff.md)를 따른다. 이번 준비의 실행 코드/시험 변경은 없다.
