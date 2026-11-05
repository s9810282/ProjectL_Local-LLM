# 소울 드랍 인계

- 작성자: 호진(이호진)

전투 View 작업자에게 넘기는 소울 드랍 구현 인계와, 그 근거인 Core 계약 확인·SpawnSoul DB 정합 기록을 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-소울-드랍-전투-view-구현-인계--2026-10-15) | 소울 드랍 전투 View 구현 인계 — 2026-10-15 |
| [2부](#2부-소울-드랍-core-계약-확인-메모--2026-10-15) | 소울 드랍 Core 계약 확인 메모 — 2026-10-15 |
| [3부](#3부-영혼-드랍-spawnsoul-db-정합사용법--2026-10-15) | 영혼 드랍 SpawnSoul DB 정합·사용법 — 2026-10-15 |

---

## 1부. 소울 드랍 전투 View 구현 인계 — 2026-10-15

- 대상: **전투 View 작업자.** 기존 SDV0 인계서를 전달용으로 보완했다. 사용자 승인 범위는 SDV1~SDV4 담당 이관과 문서 작성이다. 후속 구현은 Phase별 진행 지시 후 시작한다.
- 상태: **SD1 DB 정합·SDV0 인수 확인 완료 / SDV1~SDV4 미구현 / 외부 미전달.** 구슬의 정확한 표현·에셋은 아직 확정하지 않았다. 이번 갱신에서 제품·Core·DB·씬·프리팹·테스트는 수정하지 않았다.
- 기준: HEAD `95dbf3f587547a45d075c963417aea2b26ea9cf7` 위 ER6·SD1·SDV0 미커밋 작업본. 인수 시 현재 HEAD/dirty와 공용 전투 파일을 다시 확인한다. 기존 변경을 초기화·정리하지 않는다.
- 원본: 규칙/저작은 [SD1 DB 인계](#3부-영혼-드랍-spawnsoul-db-정합사용법--2026-10-15), 표시 설계는 [SDV 설계](../specs/2026-10-15-soul-drop-view-design-Hojin.md), 정확한 파일·서명·Task·테스트는 [소울 드랍 계획 §3.3](../plans/2026-10-15-soul-drop-plan-Hojin.md#33-sdv1sdv4-미구현-전투-view로-이관)(옛 SDV 계획)이다. 아래는 인수용 요약과 현재 소스의 색인이며 원본과 다른 규칙을 만들지 않는다.

### 1. 지금 완료된 것과 맡길 작업

| 구분 | 현재 상태 |
|---|---|
| 실제 소울 드랍 저작 | SD1에서 ID1003 SO의 효과를 단일 SpawnSoul로 맞췄다. 기존 ID·T자4칸·GUID·등록·아티팩트 모형은 보존했다 |
| Core 사실·사건 | 필드 영혼 ID/칸/잔여 유닛 턴과 생성·수집·제거의 전후 사실을 게시한다. 기존 계약으로 View를 구현할 수 있으며 새 Core 구현을 선행 조건으로 두지 않는다 |
| 기존 View | 영혼 사건의 Character.Final 등 사실 처리는 있다. 필드 구슬·수집/소멸 연출 연결은 아직 없다 |
| 인수 준비 | SDV0에서 기획 원문·수명·소스/호출자·검증 기준선을 확인했고 SDV1~SDV4 담당 이관이 승인됐다 |
| 전투 View가 구현할 것 | 전용 View 설정/Query·구슬 에셋, ID별 필드 표시·풀, 수집/비수집 제거 재생, 기존 결과 수명 연결과 검증 |

실제 동작 전제는 **보유가 아닌 로비 보드 배치**다. 새 Play에서 Boot→Lobby로 들어가 소울 드랍을 배치한 뒤 전투에 진입한다. 밤 적 사망·유효한 최종 도착 수집·수명·성장·회복·이동 비용은 SD1에 정리된 Core 규칙을 따른다. 옛 기획의 HP+1을 현재 HP 회복으로 바꾸거나 5 유닛 턴을 전체 라운드로 세지 않는다. 처음 게시된 잔여 턴이4인 사실도 임의 보정하지 않는다.

### 2. 인수 후 먼저 확인할 것

1. [Core 계약 확인 메모](#2부-소울-드랍-core-계약-확인-메모--2026-10-15)와 SD1 인계를 읽고 현재 작업본의 실제 SO/계약을 확인한다. 이 확인 메모의 답변 대기를 새 선행 조건으로 만들지 않는다.
2. 설계 §4의 기획 출처·미확정 표현(옛 §2), §6~§7의 최초 표시·재생·수명을 읽고 계획의 [파일·책임 지도(소울 드랍 계획 §3.3)](../plans/2026-10-15-soul-drop-plan-Hojin.md#33-sdv1sdv4-미구현-전투-view로-이관)와 SDV1부터 인수한다.
3. 전투 공용 파일의 동시 편집과 현재 서명을 확인한다. SDV0의 직접 생성자/Query 대역 목록은 [소울 드랍 계획 §3.2](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)(옛 SDV0 실행 기록)에 있다. 서명이 바뀌었다면 현재 계약을 먼저 대조한다.
4. 구슬의 실제 에셋·재질/색·높이·경로·초수·같은 칸 다중 영혼의 표현을 사용자에게 확인한 뒤 SDV1 에셋을 만든다. 기존 설계와 다른 범위/수명 결정이 필요하면 먼저 묻는다.

**빛 구슬 모양은 사용자 확인이다. 흰 Unlit 임시 구체·높이0.25/0.5·수집/제거0.2초·선형 경로·같은 칸 중첩은 미승인 제안이다.** 기존 아티팩트 해골/촛불 모형은 필드 구슬과 별개다. 예시 수치를 확정값으로 저작하거나 이미 에셋이 있다고 보고하지 않는다.

### 3. 이미 제공된 읽기 계약

| 실제 소스 | 사용할 사실과 주의점 |
|---|---|
| [CombatStateBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs) | `ICombatStateBufferReadSink.SoulField`·BattleInstanceId. 현재 필드는 최초/최종 표시용이며 재생 중 앞 사건을 최신 최종 Buffer로 대체하지 않는다 |
| [SoulFieldFacts·Snapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/SoulFieldFacts.cs) | Id/Origin/Cell/RemainingUnitTurns/HealAmount, 불변 목록과 Expected/Final. 동일 셀의 여러 ID를 합치거나 View 타이머로 TTL을 줄이지 않는다 |
| [CombatSoulEvent](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs) | Field.Expected/Final·ConsumedIds·CollectorId·Characters. 수집자는 명령 ActorId와 다를 수 있으므로 해당 사건의 수집자 Final.Anchor를 사용한다 |

표시는 Final에만 있는 ID를 생성, ConsumedIds를 수집, Expected에만 남고 소비되지 않은 ID를 비수집 제거로 분류한다. TTL만 갱신된 ID의 복제본은 유지한다. 비수집 제거에 별도 만료 사유 필드가 있다고 가정하지 않는다. 전투 종료/Detach의 자원 정리를 수집·만료 사건으로 꾸미지 않는다.

한 결과의 생성→수집과 최신 필드가 이미 빈 경우, 같은 셀 다중ID, 강제 도착의 다른 수집자, 다음 전투에서 ID1이 재사용되는 경우를 모두 처리한다. 실제 게시 사실이 잘못됐다면 최근 위치/필드로 보정하지 말고 기존 F1·Core 요청 경계로 보고한다.

### 4. 현재 소스와 앞으로 추가할 부품

| 현재 존재하는 접점 | 맡길 후속 변경 |
|---|---|
| [CombatViewSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs)·[CombatDisplayValues](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayValues.cs) | System은 값/결과 순서만 전달. 현재 Values에 SoulField/UnpresentedResult/WithSoulField가 없고 ReadDisplayValues도 인자 없는 버전이다. **추가 전달 API는 SDV3 예정** |
| [CombatDisplayTables](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayTables.cs) | 현재9개 필수 인자. SDV1에서 새 영혼 표현 설정을 명시적으로 전달하고 모든 직접 생성자 호출자를 갱신 |
| [ViewDataBase.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs)·[ViewDataBaseFactory](../../../Assets/ProjectL/Script/Systems/Factory/DBFactory/Group/ViewDataBaseFactory.cs) | IViewQuery는 ViewDataBase.cs 안에 있으며 현재16개 조회다. SDV1에서 기존 View DB/Builder에 설정 표를 연결 |
| [CombatSceneDisplayFactory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs) | 기존 Factory가 같은 영혼 표시/재생 부품을 한 벌 조립·주입. 영혼 부품의 생성자는 참조/설정 검증만 하고 소유 GameObject/풀은 Display 인수 뒤 Bind에서 생성 |
| [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatSceneDisplay.cs)·[FieldGridMapper](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Field/FieldGridMapper.cs) | 기존 Bind/Install/Refresh/Clear/Release와 Mapper 좌표/높이를 사용. Display가 공통 수명·정리를 소유 |
| [CombatSceneEventPlayback](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatSceneEventPlayback.cs)·[CombatSceneEventPresenter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs)·[OrderedCombatPresentation](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/OrderedCombatPresentation.cs) | 기존 사건/완료 대기·Tick·취소를 확장. 기존 영혼 사건 사실 처리 후 전용 재생을 연결 |

**아래는 미구현 예정 부품이다.** 현재 구현된 API로 취급하지 않는다. 정확한 경로·서명·생성자 인자 위치·테스트명은 계획을 따른다.

| 예정 부품 | 단일 책임 |
|---|---|
| SoulFieldVisualData/SO/Table/Query·View 설정 에셋·SoulOrb 프리팹/재질 | 표현 설정·저작·조회. 게임 규칙 값은 저장하지 않음 |
| FieldSoulPresenter | ID별 idle/전환 복제본·풀·루트·좌표와 재사용/반환 |
| CombatSoulPlayback | 한 사건의 경과 시간·현재 콜백·완료/취소 무효화·최초 영혼 스냅샷 선택 |
| 신규 테스트4종 | SoulFieldVisualTests·CombatSoulFieldPresentationTests·CombatSoulPlaybackTests·SoulDropViewBootSmokeTests |

새 System·씬 Scope·입력 Controller·전용 범용 Factory·Camera·전역 Update/Coroutine/Delay 예약을 추가하지 않는다. 기존 System/Factory/Display와 기능별 부품으로 나눈다. meta/GUID·원본 에셋 보존과 Builder 한 번 Build 계약도 지킨다.

### 5. 반드시 유지할 표시·수명 계약

- **최초 설치:** 미확인 결과에 Soul 사건이 있으면 첫 Soul 사건의 Field.Expected를 선택한다. 미확인 결과가 없거나 그 결과에 Soul 사건이 없으면 현재 SoulField를 사용한다. Install 내부 마지막 Refresh에도 예정된 WithSoulField(initial)을 전달해 최신 Final로 덮이지 않게 한다. 최종 Refresh는 현재 SoulField를 쓴다. 기존 결과 순번 누락/F1·ack 정책은 바꾸지 않는다.
- **사실 처리와 연출:** 기존 `_events.BeginEvent(soul, callback)`을 먼저 거쳐 Audio·Character.Final·RenderEvents 시점을 유지하고, callback에서 같은 세대인지 확인한 뒤 구슬 연출을 시작한다. 최초 Character/고정 카드의 최신 사실 표시도 유지한다. 모든 능력치 표시를 구슬 도착 뒤로 늦추는 새 계약을 만들지 않는다.
- **재생 시간:** 생성/TTL 갱신만 있으면 즉시 완료, 수집/제거는 설정 범위 안에서 기존 Ordered 재생 Tick으로 완료한다. 결과 밖의 백그라운드 연출을 남기지 않는다. Events→Railway→Soul의 외부 호출마다 취소/교체 세대를 다시 확인한다.
- **정상 완료:** 복제본 반환/Final 정렬 뒤 완료하고, 기존 최종 Refresh→입력 후처리/Layer→정확한 명령 제거→AcknowledgePresented→재생 수명 해제 순서를 지킨다. 별도 결과 번호/ack 소유자를 만들지 않는다.
- **취소·해제:** 콜백/세대부터 무효화하고 소유 자원을 정리한다. Cancel은 완료/ack를 알리지 않는다. 기존 상대 정리 순서를 유지하고 Field/Mapper 해제 전에 영혼을 비운다. CleanupErrors 원칙으로 일부 실패에도 나머지를 정리하며 원인과 정리 예외를 보존한다.
- **재진입·교체:** Clear/Release 반복, 반환/SetActive 콜백 중 취소, 다음 전투의 같은 ID에서 이전 Tick/콜백이 새 연결을 변경하거나 중복 반환하지 않아야 한다.
- **자원·입력:** 원본 프리팹/Material·전달 부모·카메라·피커를 보존하고 자기 복제본/루트만 반환·파괴한다. Collider/광선 입력/실제 Light를 추가하지 않는다. Core 수집·성장·비용을 View에서 계산하거나 Buffer에 쓰지 않는다.

적용 원본은 [Framework 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)·[FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md#6-framework-contract), [UI 입력 규칙](../../../Assets/AGENTS.md), [CameraCatalog](../../render-camera-catalog.md)·[CameraEffect](../../render-camera-effects.md), [hojinView](../viewDocs/view-development-guide-Hojin.md), 설계의 SSOT/SOLID·수명 항목이다. 옛 G9 거절이나 옛 unknown 정지 정책을 복원하지 않는다.

### 6. Phase별 인수 순서와 검증

| Phase | 전달할 결과·종료 지점 |
|---|---|
| SDV1 | 표현/자산 확인 뒤 설정·Query·구슬·View DB 등록, 실제 에셋/설정 검증·컴파일·문서 후 중단. 필드 운영 연결은 아직 아님 |
| SDV2 | ID별 필드 표시/풀, 같은 셀 다중ID·TTL identity·반환 재진입·기울기/입력 비간섭 테스트 후 중단 |
| SDV3 | 시간 재생·최초/최종 값·기존 결과 수명 연결. 생성→수집·강제 수집자·재연결·Cancel/fault/정리 실패·정확한 ack 통합 검증 후 중단 |
| SDV4 | 실제 SO/보드 배치에서 밤 생성·도착 수집/성장·미수집 소멸·다음 전투 지속, 실제 출력·회귀/전체 비교·최종 인계 후 중단 |

각 Phase는 계획의 RED→구현→관련 검증·독립 리뷰·문서 순서를 따른다. 실제 Boot/입력, Core fixture, 합성 입력, 렌더 이미지와 사람이 직접 조작한 증거를 구분한다. 아직 없는 테스트가 선택0/skip인 것을 통과로 세지 않는다. 사람이 직접 확인하기 전에는 마우스/키보드·패드·미관 항목을 완료하지 않는다.

**기존 SDV0 실행 기록이며 이번 문서 갱신에서 재실행한 수치가 아니다.** [소울 드랍 계획 §3.2](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)(옛 검증 기준선 원본, 상세는 git `10914570`)와 정확한 실패 이름·label·메시지를 인수하고, 후속 구현 시작 시 현재 작업본 기준선을 새로 확인한다.

| SDV0 이력 | 전체 | 통과 | 실패 | 제외 |
|---|---:|---:|---:|---|
| 관련 집합 | 3,398 | 3,325 | 73 | 0 |
| 필터 없는 전체 EditMode | 6,997 | 6,909 | 87 | Explicit1 |

Explicit 상점은 관련 실행에서1/1 통과했고 전체에서는 제외됐다. 이 이력은 실제 소울 구슬 표시의 완료 증거가 아니다. 기존 DisplayReuse·Railway와 SceneEventPresenter의 visibility 중 Cancel2건/사실 Refresh1건 등 실패를 숨기지 않는다. **기존 수명 회귀가 모두 통과한 상태로 인수한 것으로 보지 않는다.** 새 실패/예상하지 않은 제외와 기존 실패 multiset을 구분하고 범위 밖 수정을 시작하지 않는다.

### 7. 가시심장 분리와 중단 조건

[가시심장 반사 Core 요청 인계](2026-10-13-combat-core-requests-Hojin.md#2부-가시심장-반사-처리-연결--core-요청-인계)는 별도로 작성됐다. **소울 드랍 View의 선행 조건이 아니다.** 현재 가시심장이 포함되면 RequireSupportedAttackSettlement에서 공격 투영을 거절하므로 소울 드랍 단독 로드아웃으로 검증하고 guard를 우회하지 않는다. (→ 바뀜: 2026-10-16 천명님 `ebf26246`으로 가시심장 반사가 연결되고 이 guard는 없어졌다.)

두 아티팩트의 동시 장착이 필수이거나 가시심장 때문에 필요한 회귀가 막히면 그 지점에서 재현/영향을 보고한다. 추가 Core 사실·규칙/성장/수명 변경·표현 불가능한 수집자/좌표·공용 파일 동시 수정·기존 설계/계약과 다른 결정도 먼저 확인한다. Core 제품 코드는 직접 고치지 말고 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md)에 요청한다.

### 8. 작업 후 남길 인계

각 Phase의 구현 파일/설정·사용법, 실행한 테스트와 기존 실패 비교, 자원/설정·원본/index 복원, 미실행 사람 항목·남은 조건을 계획과 이 인계서에 갱신한다. 설계/계약 변경이 승인됐다면 원본도 함께 맞춘다. Phase 종료마다 멈추고 보고하며 커밋·push·스테이징은 사용자가 한다.

이번 요청은 전달 문서 갱신까지다. 후속 제품 구현·Unity/빌드/테스트 실행·외부 발송은 하지 않았다.

문서 검증: 현재 소스/계획과의 독립 대조에서 필수 오류0건이며 최초 표시 fallback을 명확히 했다. 이 인계서 한 파일을 갱신했고 로컬 링크29건/anchor·UTF-8/LF/BOM·공백/diff 검사와 index 보존을 확인했다. 작업 중 별도로 추가된 회복 UI 배경 가이드는 수정하지 않고 보존했다.

---

## 2부. 소울 드랍 Core 계약 확인 메모 — 2026-10-15

- 상태: **기존 계약·DB 반영 결과의 인수 확인 자료. 새 Core 구현 요청은 현재 없음. 외부 미전달.**
- 사용자는 SDV1~SDV4 구현을 전투 View 작업자에게 이관하도록 승인했다. 필드 영혼 표현은 아직 구현하지 않았다. 진행 상태·검증 기준선은 [소울 드랍 계획 §3.2 SDV0 인수](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)(옛 SDV0 실행 기록)이 원본이다.
- 기준: HEAD `95dbf3f5`와 현재 ER6·SD1 미커밋 작업본. 이 메모 때문에 Core 제품 파일을 수정하지 않는다.

### 1. Core 담당자에게 먼저 공유할 내용

소울 드랍의 규칙 계산과 공개 사실은 이미 제공되어 있다. 전투 View가 아래 사실을 읽어 빛 구슬을 연결할 예정임을 확인하는 메모다. **Core의 새 구현 완료를 기다리는 선행 Task는 만들지 않는다.** 실제 API가 달라지거나 필요한 사실이 빠진 재현이 생기면 [Core 경계 인계](2026-09-24-view-core-data-boundary-handoff.md)에 별도 요청을 남기고 해당 연결을 멈춘다.

| 현재 제공된 계약 | 전투 View의 사용 |
|---|---|
| [SoulFieldFacts·Snapshot·Change](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/SoulFieldFacts.cs)의 ID·셀·출처·남은 유닛 턴·Expected/Final | ID별 구슬과 사건 시점의 생성·비수집 제거를 표시한다. TTL을 자체 계산하지 않는다 |
| [CombatSoulEvent](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs)의 Field·CollectorId·Characters·ConsumedIds | 소비된 ID를 그 사건의 수집자 최종 위치로 표현한다. 명령 ActorId·최신 Buffer로 사건 사실을 대체하지 않는다 |
| [CombatStateBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs)의 읽기 전용 SoulField·BattleInstanceId | 최초/최종 표시와 전투 교체 경계를 인수한다. View에서 Buffer에 쓰지 않는다 |
| 실제 [Artifact_SoulDrop.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/Artifact_SoulDrop.asset)의 SpawnSoul 저작 | [SD1 DB 인계](#3부-영혼-드랍-spawnsoul-db-정합사용법--2026-10-15)의 값·사용법을 따른다. 실제 규칙과 허용 저작의 원본을 이 메모에 중복 정의하지 않는다 |

기획의 사망 위치 드랍·도착 수집·수명·성장·이동 비용 환급 금지와 현 Core의 대조는 [설계 §4.1·§2.1](../specs/2026-10-15-soul-drop-view-design-Hojin.md#4-기획-원문과-표현-기본안)(옛 §2·§4)에 있다. 옛 txt의 HP+1을 현재 HP 회복으로 바꾸거나 5 유닛 턴을 전체 라운드로 바꾸지 않는다. 턴 종료 생성 직후 최초 남은 값이4인 것도 게시된 사실대로 표현한다.

### 2. 기존 구현·테스트 근거

아래는 현재 소스에 있는 검증 접점이다. 이 목록 자체가 이번 실행 통과를 뜻하지 않으며 실제 결과는 SDV0 XML 기록을 따른다.

| 검증 접점 | 확인할 경계 |
|---|---|
| [ArtifactDataTableTests](../../../Assets/Tests/EditMode/Framework/ArtifactDataTableTests.cs) | 실제 SO→Table→정의1003의 수락·밤 적 사망 생성·성장/무회복·수명 |
| [ArtifactSoulBufferCalculationTests](../../../Assets/Tests/EditMode/ArtifactSoulBufferCalculationTests.cs) | 사망 좌표·생성 뒤 countdown·소멸 한 번·ID/원본 보존 |
| [MovementEnvironmentTests](../../../Assets/Tests/EditMode/MovementEnvironmentTests.cs), [CombatCommandEffectTests](../../../Assets/Tests/EditMode/CombatCommandEffectTests.cs) | 실제 최종 도착/강제 이동 수집·경유 칸 미수집·성장·이동 비용 유지·사건 생산 |
| [CharacterSoulPublicationTests](../../../Assets/Tests/EditMode/Framework/CharacterSoulPublicationTests.cs), [CombatCommandPublicationTests](../../../Assets/Tests/EditMode/Framework/CombatCommandPublicationTests.cs) | 성장/체력/필드/결과 원자 게시와 잘못된 사실의 전체 거절 |
| [CombatSceneEventPresenterTests](../../../Assets/Tests/EditMode/Framework/CombatSceneEventPresenterTests.cs) | 기존 Soul Character.Final 처리와 최종 Refresh 시점 유지 |

### 3. 별도 Core 작업과 중단 조건

**가시심장 반사 정산 연결은 별도 Core 작업이다.** (→ 바뀜: 2026-10-16 천명님 `ebf26246`으로 완료, guard 삭제) [SoulEffectCalculator.RequireSupportedAttackSettlement](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Artifact/SoulEffectCalculator.cs)는 장착된 ThornHeartRule을 미지원으로 거절하고 [CommandAttackProjection](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CommandAttackProjection.cs)이 이를 호출한다. View 작업에서 이 guard를 우회하거나 제거하지 않는다. 소울 드랍 단독 확인 로드아웃에 가시심장을 함께 넣지 않는다. 가시밭/ThornField와도 구분한다.

가시심장이 필요한 실제 요구, 추가 Core 사실/API, 성장·수명·환급·반사 순서 변경이 필요하면 재현과 요청을 정리하고 사용자에게 먼저 묻는다. 현재 소울 드랍 필드 표시에는 이 추가 작업이 필요하지 않았다.

이후 사용자 요청으로 별도의 [가시심장 반사 처리 Core 요청 인계](2026-10-13-combat-core-requests-Hojin.md#2부-가시심장-반사-처리-연결--core-요청-인계)를 작성하고 [Core 경계 §18](2026-09-24-view-core-data-boundary-handoff.md#18-thorn-heart-reflection-connection-request-2026-10-15)에 연결했다. 현재 미연결 지점·정산 정책 확인·공개 결과·검증 기준을 요청하는 문서이며 Core 구현/외부 발송은 하지 않았다. 이 소울 드랍 메모의 “새 Core 구현 불필요” 범위는 소울 드랍 표시 자체에 한정된다.

다음 전달 자료는 [전투 View 구현 인계](#1부-소울-드랍-전투-view-구현-인계--2026-10-15)다. Core 확인 메모를 먼저 준비하는 순서는 담당 간 이해를 맞추기 위한 것이며, 이미 공개된 계약에 새 승인 대기 조건을 추가하지 않는다.

---

## 3부. 영혼 드랍 SpawnSoul DB 정합·사용법 — 2026-10-15

- 상태: **SD1 DB 정합 완료, 종료 보고 지점.** 작성된 문서는 외부에 전달하지 않았다.
- 작업 원본: [SD1 계획·실행 기록](../plans/2026-10-15-soul-drop-plan-Hojin.md).
- Core 계약 원본: [DB 실행 데이터 요구사항 §11](2026-10-08-db-runtime-data-requirements.md#11-artifacts-and-board-initialization--g19g20), `CoreCaculator/Application/Artifact/ArtifactEffectCompiler.cs`의 `ValidateSoulAltar`·`ValidateSoulGrowth`.

### 1. 적용 범위

대상은 `Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/Artifact_SoulDrop.asset`(ID 1003)의 **효과 저작**이다. 두 ModifyStat 항목을 한 SpawnSoul 항목으로 이전한다. SO의 ID·초기 수량 1·기존 T자 4칸·GUID·등록·표현 에셋은 보존한다. Core 규칙·공개 API·Buffer·Framework 조립/서비스·씬·프리팹은 변경하지 않는다.

| Inspector 필드 | 저작 값 |
|---|---|
| Effects / Size | 1 |
| Entry Id | `soul_drop` |
| Trigger | UnitDied |
| Source | Enemy / Category None |
| Condition | IsNight / 미사용 Comparison LessThan |
| Target | Collector / Ally / Category None / 범주 제외·HP 비율 필터 꺼짐 / 비율 0 |
| Location | DeathPosition |
| Effect | SpawnSoul / Stat Id 비움 / Knockback Distance 0 |
| Soul Growth | Attack Bonus 1 / Max Health Bonus 1 / Heal Amount 0 |
| Value | Flat / Basis None / Amount 0 |
| Duration Turns | 5 |

성장 +1/+1과 수명 5는 현재 Core가 요구하는 값이다. `value.amount`에 성장량을 넣지 않는다. 회복은 별도 데이터 필드이며 이번에는 기존의 미저작 기본값 0을 명시했다. 최대 HP가 증가해도 현재 HP를 추가 회복시키지 않는다. 실제 수명 계산·수집 가능 여부·성장·회복 상한은 Core가 결정한다.

### 2. 사용 순서

1. Project 창에서 위 SO를 선택하고 Effects가 한 항목인지 확인한다. 이 값을 바꿀 때는 위 Core 계약의 허용 조합을 유지한다.
2. Play를 새로 시작해 Boot → Lobby로 진입한다. 초기 DB/계산기 스냅샷을 이미 만든 실행 중 세션을 에셋 수정만으로 갱신한 것으로 보지 않는다.
3. 로비 아티팩트 편집 화면에서 **영혼 드랍**을 보드에 배치한 뒤 전투에 진입한다. 풀에 보유만 하고 보드에 배치하지 않은 효과는 장착 효과가 아니다.
4. 밤에 적 유닛이 실제로 죽으면 그 사망 위치에 배치 한 개당 영혼 하나를 계산한다. 낮 또는 아군 사망은 생성하지 않는다. 현재 Core 전투 캐릭터의 Owner는 Player/Enemy이며 중립 전투 캐릭터 지원을 이번에 추가하지 않는다. 두 능력치 필드를 두 개의 영혼으로 해석하지 않는다.
5. 생존한 아군이 Core의 유효한 도착/이동 정산으로 해당 칸에 도착하면 수집한다. 한 영혼당 수집자의 영혼 개수·공격력·최대 HP가 각각 1 증가하며 추가 회복량은 0이다.
6. 수집하지 않은 영혼은 5 유닛 턴 수명으로 감소·소멸한다. 턴 종료 정산에서 생성되는 경우에는 생성 뒤 그 정산의 감소가 적용되어 처음 게시되는 남은 수명이 4일 수 있다. 프레임 수·전체 라운드 수로 세지 않는다.

가시심장 반사 정산 연결은 별도 Core 작업이므로 이번 확인용 로드아웃에 함께 넣지 않는다. (→ 바뀜: 2026-10-16 `ebf26246`으로 Core 연결 완료) 가시밭/ThornField 기능과 가시심장 Artifact를 같은 기능으로 취급하지 않는다.

### 3. 확인 지점과 남는 View 작업

- 영혼 필드 상태: 기존 `ICombatStateBufferReadSink.SoulField.Entries`의 ID·칸·남은 유닛 턴·출처·회복량을 읽는다.
- 수집자 상태: 기존 Character Buffer의 보유 영혼 수·최종 능력치/체력과 전투 결과를 읽는다. View가 수집 판정·능력치 보정을 다시 계산하거나 Core/Buffer에 직접 값을 쓰지 않는다.
- 이번 완료 기준은 실제 SO → 기존 Table adapter → Core 컴파일·공개 계산 검증이다. 실제 전투 Plan/Job/Buffer 전체 인수와 사람이 보는 Game View 기능 검증을 이 단위 검증으로 대체하지 않는다.
- **필드 영혼 모델·칸 표시·수집/소멸 연출은 이번 DB 정합 범위에 포함하지 않는다.** DB를 맞췄다는 이유로 화면에 영혼 오브젝트가 새로 나타나는 것은 아니다. 기존 읽기 계약을 사용한 View 후속 구현이 필요하다.

### 4. 검증·인계

실제 실행 수치·기존 실패 재현·증거 파일은 [소울 드랍 계획 §3.1 SD1](../plans/2026-10-15-soul-drop-plan-Hojin.md#31-sd1-spawnsoul-db-정합-2026-10-15)(옛 SD1 실행 기록, 수치·증거 경로는 2026-10-27 정리 때 잘려 git `10914570`에 있다)이 원본이다. 관련 아티팩트·영혼 검증은 통과했고, DB 에셋 전체 검사의 기존 지형/머티리얼 실패 두 건은 남아 있다. 수동 Play 검증은 하지 않았다.

과거 [Task 5 G9 거절 증거](2026-10-01-character-soul-task5-handoff.md#3-g9--실제-영혼-so는-아직-연결-불가)는 당시 이력으로 남기고, 이번 저작 수락과 전투 전체 인수 상태를 구분한다. [Core 경계 §17](2026-09-24-view-core-data-boundary-handoff.md#17-soul-drop-spawnsoul-db-alignment-2026-10-15)에 이 결과를 연결했다. 기존 Framework 등록·Build·모듈 수명·UI·카메라 계약 변경은 없으며 새 서비스·이벤트 버스·런타임 캐시를 추가하지 않는다. 이 DB 변경 때문에 필요한 새 Core API/규칙 수정은 없다. 가시심장 반사 정산이나 View 표시를 이 작업에서 구현한 것으로 보지 않는다.

### 5. View 후속 설계 — 구현 전

같은 날 사용자 요청으로 [필드 표시·수집·소멸 설계](../specs/2026-10-15-soul-drop-view-design-Hojin.md)와 [소울 드랍 계획 §3.3](../plans/2026-10-15-soul-drop-plan-Hojin.md#33-sdv1sdv4-미구현-전투-view로-이관)(옛 SDV0~SDV4 Task 계획)을 작성했다. Project L의 PPT·Word·txt 원문을 검사하고 기획의 드랍/획득/소멸과 현재 Core 사실을 대조했다. 빛 구슬은 사용자 확인이며 미기재 경로/시간·자산 범위는 SDV0의 인수 항목이다. 이번은 문서 작성만으로 필드 View·전투 Play 인수가 끝난 것은 아니다. 가시심장 Core 연결은 필요해지면 해당 지점에서 멈추고 보고한다.

이후 SDV0에서 사용자는 SDV1~SDV4 구현 담당을 전투 View 작업자에게 이관하도록 승인했다. [Core 계약 확인 메모](#2부-소울-드랍-core-계약-확인-메모--2026-10-15)를 먼저 준비하고 [전투 View 구현 인계](#1부-소울-드랍-전투-view-구현-인계--2026-10-15)에 연결했다(미전달). Core의 새 구현은 현재 선행 조건이 아니며, 제안된 임시 구슬 표현은 미승인으로 남긴다. 실제 SDV0 검사·검증 기준선 상태는 [소울 드랍 계획 §3.2](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)(옛 SDV0 실행 기록)을 따른다.
