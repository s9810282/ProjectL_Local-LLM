# 선택 ClipEvent2·3 복수 발사 설계 리뷰 (2026-11-02)

- 대상: [설계 2차](2026-11-02-combat-clip-multishot-design.md), [Task 계획 2차](../plans/2026-11-02-combat-clip-multishot-plan.md).
- 요청 기준: SSOT, SOLID 5원칙, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성, 캡슐화, C#/Unity 구조, 불필요한 if/Tick 검사.
- 판정: **방향은 유지할 수 있으나 구현 전에 R1~R4를 보완해야 한다. R5는 단순화 권고다.** P1 1건, P2 3건, P3 1건.
- 이번 검토는 설계와 실제 기존 코드의 계약 대조다. 새 기능의 실행 버그나 성능 저하를 재현했다는 뜻이 아니다. 설계/계획의 정책·구현은 고치지 않았고, 리뷰 문서만 추가했다. **컴파일 미확인.**

> **후속 상태(2026-11-02):** 사용자 "보완점 보완하고 작업 시작해" 승인으로 R1~R5 권장 보완을 설계/계획 3차에 반영했다. MS-a의 불변 시각/선택 번호 빈칸 허용 코드는 완료, 컴파일 미확인이다. 아래는 최초 지적 원문이며 모든 코드 보완이 완료됐다는 뜻은 아니다. R1 접촉 집계와 R2 소리 정규화 등은 후속 Phase다. 현재 상태 원본은 [계획의 MS-a 실행 기록](../plans/2026-11-02-combat-clip-multishot-plan.md#ms-a-코드-완료정적-대조-2026-11-02)을 따른다.

> **MS-b 후속:** 사용자 "다음 작업 시작."으로 R1의 집계 준비/일회 해제/Hit/End/취소 및 R3의 발사 목록·지점 정의 캡슐화 코드를 반영했다. 컴파일/재생 미확인이다. R2와 R4의 실제 소비 제한은 MS-c에 남는다. 최신 상세는 [MS-b 실행 기록](../plans/2026-11-02-combat-clip-multishot-plan.md#ms-b-코드-완료정적-대조-2026-11-02)이며 아래의 최초 지적을 실행 검증 완료로 바꾸지 않는다.

> **MS-c 후속:** 사용자 "다음 진행해"로 R2의 단일 SoundTimings 소비/검사와 R4의 원거리 클립 한정 Cue2/3 연결·미지원 입력 거절을 반영했다. R3의 추가 소리 목록은 복사·읽기 전용으로 제공하며 actorPoint는 기존 지점 정의를 고른다. 탄별 투사체 소리는 발사/Contact 시점을 공통 도우미에 넘기고 집계 Hit과 구분한다. 정적 상세는 [MS-c 실행 기록](../plans/2026-11-02-combat-clip-multishot-plan.md#ms-c-코드-완료정적-대조-2026-11-02)이다. R1~R5 코드 보완을 반영했으며 MS-d의 에셋 적용·사용자 Unity 인수는 남아 있다. **컴파일 미확인.**

## 1. 발견 사항 — 영향 순서

### R1 [P1] 접촉 집계 조각의 종료·재진입 계약을 명시해야 한다

대상: [설계 MS4](2026-11-02-combat-clip-multishot-design.md#6-ms4--개별-접촉과-한-번의-피해-표시), 계획 MS-b.4~6.

설계는 집계 조각이 모든 탄 접촉 뒤 `release → Hit`을 실행한다고 정했지만, **집계 조각 자신이 언제/어떻게 끝나는지**와 완료 통지 중 재진입 순서를 정의하지 않았다. 기존 실행은 단순히 Gate를 열었다고 모든 조각을 끝내지 않는다. 집계 조각이 남으면 클립/탄이 모두 끝나도 `_remainingItems`가 0이 되지 않아 채널 복원·출처 전체 종료가 오지 않을 수 있다.

근거:

- [조각 계약](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/ICombatPresentationPiece.cs:16): 외부 진행 조각은 `EndCallback`으로 끝을 알린다.
- [실행 종료 판정](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatPieceRun.cs:438): 모든 항목이 끝나야 `Finish`한다.
- [신호/끝의 가용성 검사](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatPieceRun.cs:228): 끝난 항목은 새 신호를 낼 수 없다. 따라서 집계의 끝을 `Hit`보다 먼저 보내면 후속 큐가 사라질 수 있다.
- [시작 중 신호 처리](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatPieceRun.cs:281): `Begin` 안의 동기 신호도 실제로 처리한다. 첫 이벤트가 0초인 경우도 기존 계약이다.

보완안:

1. 집계는 자기 시계가 없는 외부 진행 조각으로 정의한다. `Begin`에서 보호된 `Hit`/`End` 콜백과 접촉 입력 수명을 먼저 준비하고, 그 뒤 클립/발사가 시작되도록 조립 순서를 확정한다.
2. 마지막 최초 접촉에서 완료 표시를 먼저 확정한 뒤 **`release → Hit → End`** 순서로 통지한다. `release` 중 취소/세대 변경/재진입이 일어나도 이중 통지하지 않는다.
3. 접촉 입력은 발사별 식별자와 취소된 실행 여부를 확인한다. 다른 조각이 집계의 가변 배열이나 `CombatPieceContext`를 직접 다루지 않고 주입받은 접촉 콜백만 호출하게 한다.
4. `Cancel`은 접촉 입력을 닫고 완료/피해 해제를 알리지 않는다. 집계는 투사체 손잡이를 소유하지 않는다. 전체 실행은 집계가 끝나도 아직 날고 있는 각 발사 조각을 계속 기다린다.
5. `Tick`에서 접촉 목록을 스캔해 끝을 찾지 않는다. 접촉 콜백에서 한 번 처리한다.

이는 새 Gate 정책을 만드는 변경이 아니라 기존 조각 계약을 충족하는 데 필요한 보완이다. SOLID의 L과 캡슐화, JMH 수명 분리·FW-104에 해당한다. FW-104는 권장 계약이며 자체 MUST 위반으로 분류하지 않는다.

### R2 [P2] 사건 소리의 번호별 시점 검사와 런타임 원본을 하나로 정해야 한다

대상: [설계 MS5의 additionalSounds](2026-11-02-combat-clip-multishot-design.md:119), 계획 MS-c.1~3.

기존 직렬화 `sound`를 유지하면서 추가 입력을 받는 것 자체는 SSOT 위반이 아니다. 다만 설계는 값 변환 뒤에도 `Sound + AdditionalSounds` 두 소비 경로를 유지하는지, 한 목록으로 정규화하는지 확정하지 않았다. 이 상태에서는 첫 소리와 추가 소리에 서로 다른 검사·생략·시점 조회 규칙이 붙기 쉽다.

특히 기존 [UsesAnchor](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:245)는 이펙트·흔들림·피격 프리셋 소리·플래시를 확인하지만 **사건 소리 `Sound`는 확인하지 않는다.** 기존 일반 `Cue` 공격 소리는 클립 없는 코드 모션에서도 쓸 수 있어 이 점만으로 현재 버그라고 단정할 수 없다. 새 `Cue2/3`는 해당 클립 이벤트가 필수이므로, 이 도우미를 그대로 재사용하면 **소리에만 Cue2를 고른 입력**의 누락 이벤트 검사를 빠뜨릴 수 있다. 런타임 [시점 표](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatCueAnchors.cs:83)는 미설정 시점에 예외를 던진다.

보완안:

- SO의 기존 `sound`와 선택 추가 입력은 유지하되, 값 변환에서 사건 소리 시점 목록 하나로 합친다. 기존 읽기가 필요하면 첫 원소를 읽는 호환 접근으로 둔다.
- [CombatEventCues](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs:15)의 빈 큐 판정·허용 사건 검사·번호별 시점 존재 검사·[큐 조립](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatCueComposer.cs:68)이 모두 그 목록을 읽게 한다.
- 이벤트 사용 여부를 검사하는 한 경로가 이펙트·흔들림·플래시·피격 소리·사건 소리를 빠짐없이 포함하도록 Task에 적는다. 코드 모션/덮어쓰기 제약에도 같은 판단을 재사용한다.
- 이 판정은 값 생성/유닛 외형 검사에서 한다. 재생 Tick에서 '이번에는 Cue2가 있는가'를 재검사하거나 없는 큐를 무시하지 않는다.

추가 소리의 시점과 기존 소리의 시점은 서로 다른 입력일 수 있다. 지적은 두 소리를 하나로 줄이라는 뜻이 아니라 **같은 종류의 사실을 읽고 검사하는 런타임 계약을 하나로 두라는 뜻**이다.

### R3 [P2] 불변 데이터와 Unity 입력의 캡슐화 경계를 구체화해야 한다

대상: [설계의 불변 시각 목록](2026-11-02-combat-clip-multishot-design.md:52), 발사 목록·추가 지점·actorPoint 입력.

'불변 목록'이라는 설명만 있고 실제 노출·복사·원소 변경 계약이 없다. 새 목록을 `IReadOnlyList<T>`로 선언하는 것만으로 원소나 원본 컬렉션까지 불변이 되지는 않는다. Microsoft 문서도 이 인터페이스가 원소 내용의 불변성을 보장하지 않는다고 설명한다. [Microsoft IReadOnlyList](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1)

기존 [CombatEventCues.Copy](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs:62)는 입력을 복사한다. 이 점은 재사용할 수 있지만, 이번 설계에서 더 강한 불변을 보장하려면 새 값의 반환 형태까지 정해야 한다. 기존 프로젝트 전체의 컬렉션을 일괄 교체할 필요는 없다.

보완안:

- **Unity 입력:** `[Serializable]` 입력 타입의 private `[SerializeField]` 필드/목록으로 두고 실행 중 변경용 setter를 노출하지 않는다.
- **DB/View 값:** 이벤트/발사 항목은 생성 뒤 바뀌지 않는 값으로 만들고, 목록은 생성 시 복사한다. backing 배열/List를 소비자에게 그대로 돌려주지 않는다. 읽기 전용 래퍼 또는 `Count + index 읽기` 같은 기존 호환 가능한 방식으로 한 번 준비한다. 새 컬렉션 라이브러리나 일반화된 저장소 서비스는 필요 없다.
- **표시 연결:** 지점 목록 자체를 외부에서 재정렬/교체하지 못하게 한다. Transform의 애니메이션 이동은 정상이며 '참조 대상 Transform도 불변'이라는 보장을 하지는 않는다.
- `ICombatUnitClip`은 지점의 현재 값만 읽고 SO/입력 배열·가변 집계 상태를 반환하지 않는다.
- 직렬화 입력에 runtime 불변 모델의 `readonly` 필드/프로퍼티를 그대로 사용하지 않는다. Unity 6000.0은 일반 필드 직렬화에서 readonly 필드를 제외하므로 입력/값 타입을 분리해야 한다. [Unity 직렬화 규칙](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html)

### R4 [P2] Akimbo 발사 확장과 근접·피격 큐 확장을 분리해 범위를 결정해야 한다

대상: [설계 MS5](2026-11-02-combat-clip-multishot-design.md:117), 계획 MS-c.1~2의 '실제 번호별 클립 시점을 조립하는 소비자'.

범용 이름표를 공통으로 읽는 것과, 모든 소비자가 새 번호로 효과를 낼 수 있게 하는 것은 다른 기능 변경이다. 현재 계획은 근접·피격의 `Cue2/3` 소비까지 한 Phase에 넣었다. Akimbo의 두 발 발사에는 피격 소비 확장이 필요하지 않다. 또한 피격의 실제 조립은 Reaction Presenter가 아니라 [CombatCharacterEventHandler.ComposeHit](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatCharacterEventHandler.cs:320)에 있다. 현재는 첫 `Cue`만 연결한다.

이 문제는 공통 파일을 절대 바꾸지 말라는 뜻이 아니다. 최초 범용 이벤트/신호 확장의 통합 회귀는 필요하다. 다만 '일반화했으니 모든 기능도 동시에 확장'하면 독립 변경 범위를 불필요하게 키운다.

권장안:

- **이번 범위:** 공통 이벤트/신호 형식 + 공격에서 필요한 큐 연결 + 여러 발/지점/집계 + Akimbo 적용. 기존 근접/피격의 첫 이벤트 동작은 회귀만 확인한다.
- **별도 범위:** 근접/피격의 두 번째·세 번째 큐를 실제로 고르는 기능. 이를 지금 포함하려면 사용자 선택 후 정확한 조립 파일, 지원 큐 종류(특히 현재 Start/Cue만 허용하는 피격 플래시), DB 조건, 회귀를 별도 Task로 확정한다.
- 범위에서 미룬 조합은 DB에서 명확히 거절한다. 일반 이벤트를 읽을 수 있다는 이유로 선택만 허용하고 재생 연결을 빠뜨리면 안 된다.
- 식별자→재생 신호→큐 시점 대응은 공통 연결 도우미 한 곳에서 관리한다. 소비자마다 숫자 일치 캐스트/동일 switch를 복제하지 않는다. DB 데이터 계층이 런타임 Pieces enum을 참조하는 방향은 유지해서 금지한다.

이는 JMH §27~28과 프로젝트 §4.6에 따른 범위/기능 독립성 보완이며, 실제 공통 코드 분리를 하지 말자는 주장은 아니다. 권장 범위 변경은 아직 설계/계획에 적용하지 않았다.

### R5 [P3] 선택 이벤트 번호의 연속성 강제는 필요성을 다시 판단한다

대상: [설계 입력 규칙](2026-11-02-combat-clip-multishot-design.md:44), 계획 MS-a.2.

현재 안은 3번이 있으면 2번도 요구한다. 번호 순서에 의미를 부여한 작성 정책으로는 가능한 선택이므로 그 if 자체가 잘못은 아니다. 하지만 새 커서는 '실제 존재하는 시점'을 처리하도록 설계했으며, 3번 소비에 2번의 존재가 필요한 런타임 근거는 아직 없다. 추가된 범용 이름표를 쓰기 위해 소비하지 않는 2번까지 찍게 만들 수 있다.

권고: 번호 중복·필수 첫 이벤트·시간 범위·실제 소비 이벤트 존재만 검사하고 선택 번호의 빈칸은 허용하는 쪽을 검토한다. 연속성이 편집 정책상 필요하다면 이유를 기록하고 유지한다. 이벤트 수가 최대 셋인 현재 범위에서 새 설정 플래그나 정책 인터페이스는 필요 없다.

## 2. 요청 기준별 판정

| 기준 | 판정 | 근거/잔여 |
|---|---|---|
| SSOT | 보완 필요 | 피해/시각/지점 원본 분리는 적절하다. 사건 소리 목록·시점 사용 검사 원본은 R2, 대응표는 R4에서 구체화한다. |
| SOLID 5원칙 | 방향 적절, S/L 보완 | 아래 표. |
| JMH 지침·FW 계약 | 설계상 적절, 수명 보완 | 새 서비스/Builder 우회 없음. R1의 조각 수명과 R4 범위를 먼저 확정한다. |
| 기능 변경 독립성 | 보완 필요 | 원거리 조립 분리는 좋다. 근접/피격의 새 기능까지 포함하는 범위는 R4. |
| 캡슐화 | 보완 필요 | 리스트/입력 참조와 집계 콜백의 노출 계약 R1/R3. |
| C#/Unity 권장 구조 | 방향 적절, 구체화 필요 | 직렬화 입력과 불변 실행 값 분리, 순수 C# 조립/커서, 기존 MonoBehaviour 이름표 유지. R3. |
| if/Tick 검사 | 남발 설계는 없음 | 새 시계·Update·씬 반복 검색이 없다. R5는 불필요할 수 있는 입력 제약. 집계는 콜백 처리로 명시할 것. |

### SOLID 5원칙 각각

| 원칙 | 판정 |
|---|---|
| S | 조립·발사·접촉 집계 분리는 적절하다. 큐의 첫 소리/추가 소리를 별도 정책으로 소비하지 않게 R2 보완. |
| O | 2·3번 시점과 새 발사는 데이터로 선택할 수 있다. 4번 이후가 이번 범위 밖이라는 제한은 명시돼 있다. 모든 enum을 무제한 확장 구조로 바꾸는 것은 요구하지 않는다. |
| L | 기존 세 마커·단발·첫 이벤트 접근을 보존한다. 집계의 끝과 `Hit` 순서가 기존 조각 계약을 지켜야 한다(R1). |
| I | Core/Buffer에 발사 정보를 늘리지 않고 클립 지점 읽기만 확장한다. 입력 배열/가변 상태를 계약에 추가하지 않는 조건(R3). |
| D | 대여·발사 함수·기존 창구에 의존하고 씬 검색/Core 호출을 추가하지 않는다. 내부 기능을 독립 Framework 서비스로 과잉 등록하지 않는다. |

### JMH·FW와 조립 경로

확인한 현재 생성 경로는 [CombatEventPlaybackFactory.Create](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs:23) → [CombatSceneEventPresenter의 재생기/공격 시작 조립](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs:131) → 기존 조립 실행이다. 계획의 새 부품은 이 내부에서 행동별로 만들어 실행이 소유한다.

- FW-001~003: 새 Service/Module/Builder/상위 Export/루트 조회를 제안하지 않는다. 이 변경 때문에 하위 서비스 루트 재등록을 추가할 이유가 없다. **구현 전이므로 새 등록 경로의 실행 검증을 통과했다고 표현하지 않는다.**
- FW-004·011·014: 기존 대여/효과 Scope/투사체 손잡이의 반환 경로를 유지하는 방향이다. 집계는 자원을 직접 소유하지 않는다.
- FW-012: 새 효과 타입/테이블을 만들지 않는다. 실제 효과를 골라 연결할 때 등록/프리팹 조건을 확인한다.
- FW-013·016: 실제 Unity 이벤트 직접 실행이나 별도 EventBus, Runtime 재초기화가 없다.
- FW-104: 동기 접촉·완료 재진입·취소 순서는 호출 경계에 명시해야 한다(R1). SHOULD와 MUST를 구분한다.
- JMH §23·25·26·27·28: SO와 실행 상태 분리, 검색 금지, Presenter 비대화, 불필요한 추상화 방지, 실제 기능 범위 확정으로 대조했다.

## 3. if/Tick의 허용 위치와 피할 패턴

| 위치 | 필요한 처리 | 피할 처리 |
|---|---|---|
| DB 입력/값 생성 | 선택 목록 없음→기존 단발 해석, 잘못된 번호/중복/없는 시점 검사 | 재생 중 같은 입력 규칙 재검사, 없는 값을 임의 기본값으로 대체 |
| 클립 커서 | 현재 시간이 다음 시각을 넘었는지, 취소/끝 여부 | 모든 번호/클립 이벤트 배열을 매 프레임 재파싱·정렬·할당 |
| 탄 접촉 콜백 | 중복 접촉 방지, 해당 실행 가용성, 남은 접촉 수 갱신 | 매 Tick 전체 탄/Transform을 훑어 접촉·완료를 추측 |
| 취소/세대 | 기존 경계의 늦은 콜백 차단과 일회 완료 | 모든 계층에 같은 취소/해제 집합 복제 |
| 효과 시작 | 선택 지점의 현재 월드 값 한 번 읽기 | 대기 중 Find/GetComponent 반복 |

`if (cancelled)`, 중복 콜백 방지, 다음 이벤트 시각 비교는 실제 계약에 필요한 분기다. 개수만 줄이면 수명이 깨질 수 있다. 반면 데이터 불변 검사를 Tick으로 옮길 이유는 없다. 기존 인터페이스 때문에 존재하는 빈 `Tick` 구현과 새로운 완료 polling은 구분한다.

Unity 공식 문서는 다수 MonoBehaviour의 Update 콜백/조기 반환 비용과 실제 프로파일링의 필요를 설명한다. 이 프로젝트는 이미 통합 실행기를 사용하므로, 이번 기능에 별도 Update 관리자·MonoBehaviour·새 주기 검사기를 만들 이유가 없다. 이 근거를 특정 if의 성능 수치로 확대해석하지 않는다. [Unity 6000.0 업데이트 지침](https://docs.unity3d.com/6000.0/Documentation/Manual/events-per-frame-optimization.html)

## 4. SSOT로 잘못 분류하지 않을 사항

- 클립 이벤트 번호와 발사 지점 번호는 서로 다른 선택이다. 서로 값이 다르다고 SSOT 위반이 아니다.
- `shots`의 발사 총구와 효과 큐의 `actorPoint`도 '무엇을 발사하나'와 '어디에 효과를 내나'라는 별도 표현 의도일 수 있다. 현재 두 선택이 반드시 같아야 한다는 규칙은 없다. 자동 동기화/일치 검사/새 링크 기능을 추가하지 않는다.
- SO의 기존 직렬화 `sound`와 추가 입력을 호환 목적으로 나누어 두는 것은 허용할 수 있다. 런타임 원본/검사 경로까지 둘로 나눌 필요는 없다(R2).
- 접촉 집계의 '이 탄을 셌는가'와 Gate의 '이 대상의 후속을 열었는가'는 다른 사실이다. 집계 상태를 모두 제거하라는 뜻이 아니다.

## 5. 결론과 다음 리뷰 지점

유지할 방향은 **선택 ClipEvent2·3 + 기존 단발 무수정 + 설정으로 이벤트/지점 연결 + 모든 탄 접촉 뒤 기존 결과 한 번 표시**다.

다음 설계 개정에서는 R1 집계 수명, R2 단일 소리 시점 소비/검사, R3 새 값의 캡슐화, R4 실제 소비 확장의 범위를 먼저 확정하고 Task에 반영한다. R5는 범용 이벤트의 작성 정책 선택으로 남긴다. 이번 리뷰만으로 수정안을 승인/구현한 것으로 해석하지 않는다.

코드·에셋·시험 변경, Unity 실행, MSBuild, 커밋·push 없음. **컴파일 미확인.**
