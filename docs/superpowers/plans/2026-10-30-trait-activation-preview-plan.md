# 특성·Activation 즉시 재생 — Task 계획

> 2026-10-31 조립 확장: 다음 구현 계획은 [공통 사건 블록 Task](2026-10-31-composable-trait-preview-plan.md)다. 기존 단일 발동 도구와 프리셋은 유지하며 후속 A~E는 미착수다. 아래 자동 테스트 관련 기록은 과거 실행 이력이다.

- 날짜: 2026-10-30
- 상태: **A~D 구현, Unity 관련 테스트 30/31 통과, 네 프리셋 각각 20회 반복·중단 확인. 환경 종료의 Setting UI 오류는 사용자 결정으로 유지하며 설정 조합 검증은 미완료.** 최신 근거는 §9.3.
- 설계 원본: [즉시 재생 환경 설계](../specs/2026-10-30-trait-activation-preview-design.md), 특히 §10.
- 사용자 확정: Editor 창 + Play Mode 전용 테스트 씬. 후속 요청으로 A~D를 순차 구현하고 마지막에 검증·정리하도록 승인했다.
- 코드 기준: 현재 커밋 전 TS-f 작업 포함. 기존 수정·미추적 파일을 덮어쓰지 않는다. Phase 착수 때 변경 상태를 다시 확인한다.
- 커밋·push·외부 메시지 발송은 하지 않는다. 이번에는 사용자 A~D 일괄 진행 요청에 따라 중간 보고 후 다음 Phase로 진행했다. 원래 각 Phase의 “멈춘다”는 개별 승인 방식일 때의 리뷰 기준이며, 검증 미실행을 완료로 바꾸는 뜻은 아니다.

## 1. 범위와 작업 경계

첫 버전: 특성/Kind 선택, 좌표·방향·대상 설정, 일반 발동/추가 공격/대신 맞기, 재생·중단·반복, 공유 프리셋. 원본 값 편집은 SO Inspector, 실행은 운영 재생 코드다. 실제 규칙 검증, 재귀 추가 공격 편집, 모든 상태/HUD·흔적의 편집, 타임라인 스크럽·배속, 게임 내 조작 패널은 제외한다.

Core·FieldCamera·Architecture·옛 SO는 수정하지 않는다. Core 계약 DTO는 합성 입력 작성에만 사용한다. 테스트 결과를 운영 Buffer에 쓰거나 Core 처리 API로 주입하지 않는다. Core 변경이 필요해지면 구현을 멈추고 기존 Core 전달 문서에 요청을 적는다.

새 파일은 `.meta`를 함께 만든다. 경로·타입명은 아래를 기준으로 하되 단순 파일 분할은 허용한다. 수명·서비스 등록·공개 범위·기능 범위가 바뀌면 먼저 설계를 보완하고 사용자에게 묻는다.

## 2. 확인한 의존성과 처리 방침

| 의존성 / 코드 근거 | 처리 방침 | Task |
|---|---|---|
| GlobalSystemFactory → JobRegisterSystem.PostBindService → InGame/Buffer/LifeCycle | 기존 FrameworkRuntimeFactory를 세션당 한 번 사용. 객체는 초기화하지만 구간/Run/전투는 시작하지 않음 | B1 |
| CameraCatalogSystem.PostBindService → Global.Loding | 일부 서비스만 떼어 가짜 Global을 만들지 않음 | B1 |
| DataBaseFactory가 Render/InGame/View 전체 생성 | 전체 Catalog 구성 오류는 초기화 실패로 보고. View는 View/Render 값만 조회 | B1 |
| CombatSceneDisplayFactory가 손패·정보창 등 필수 참조 요구 | 사건 재생 조립만 추출. 운영 필수 검사 유지 | A1 |
| CombatSceneEventPlayback의 Railway/Thorn/PhaseTransition 필수 | 실제 Presenter를 연결, 빈 환경 설치. 합성 결과 범위는 세 문맥으로 제한 | A1/B2 |
| 초기 보인 사실 Replace/Refresh는 CharacterData 입력 | 사실 목록 overload로 공통화. 임시 Buffer 생성/변경 불필요 | A2 |
| FieldUnitPresenter는 UnitHUDLayer 필요, Layer는 UISystem 동적 등록 | 기존 CombatUnitHUDFactory/AttachUISystem 사용. 해제 시 등록도 해제 | B2 |
| TraitEventHandler·AttackStart 등 연결 동안 값 캐시 | 재실행마다 최신 View 값으로 표시/재생 조립 새로 생성 | B3/D2 |
| EffectSystem은 Scope를 Shutdown까지 보관 | Scope는 세션에 하나, 실행별 창구는 그 Scope를 빌림. 기존 서비스 경로 유지 | A3/D2 |
| Scope 완료 조회 없음, 카메라 Dispatch 완료 조회 없음 | 재생 완료와 잔상 확인 분리. 반복 전 정해진 확인 시간 뒤 강제 회수 | D2 |
| Tool.Editor asmdef에 Systems 참조 없음 | Assets/ProjectL/Editor/CombatPreview의 기본 Editor 어셈블리 + UNITY_EDITOR 공개 창구 | B1/B4 |
| PlayFromBootToggle.Apply가 reload/delayCall에서 시작 씬 덮어씀 | 임시 시작 씬 소유권을 공통화하고 Boot Apply가 이를 존중. PC별 토글 값 보존 | B4 |
| CameraCatalog는 씬 카메라 수집, UnityCameraCatalog는 유지 객체로 옮길 수 있음 | 맵/카메라를 BindScene 전에 준비, 소유 객체 명시 추적·종료 | B1/B2 |
| ChildAttack·Interception은 접촉/공격 그룹 무결성 요구 | 생성자 계약을 만족하는 문맥별 생성기. Kind별 규칙 계산 없음 | C1/C2 |

위는 소스 대조 결과다. 실행 보장은 Phase별 Unity 검증 뒤에만 기록한다. 전용 부분 Global 런타임은 이번에 만들지 않는다. 기존 런타임 초기화가 다른 기능 DB에 의존하는 한계는 남긴다.

## 3. 예정 파일과 소유권

| 위치 | 변경 내용 |
|---|---|
| `Systems/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs` + 조립 결과 타입(새로) | 사건 재생 객체 생성만 공통화 |
| 같은 폴더 `CombatSceneDisplayFactory.cs` | 기존 생성 구간을 공통 Factory 호출로 교체, 나머지 조립/수명 유지 |
| `ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs`, `Support/CombatShownFacts.cs` | 초기/최종 사실 목록 입력 overload. 기존 CharacterData 경로 호환 |
| `ViewSystem/Combat/Presentation/Effects/CombatEffectWindow.cs` | 빌린 EffectScope를 받는 internal overload. 기존 생성자/Release 의미 유지 |
| `Systems/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs`(새로, UNITY_EDITOR 한정) | 테스트 표시 조립. 기능 코드와 분리 |
| `ViewSystem/Dev/TraitPreview/`(새로, UNITY_EDITOR 한정) | 호스트, 세션, 공개 창구/DTO, 입력 검증, 문맥별 생성기, 프리셋 SO |
| `Assets/ProjectL/Editor/CombatPreview/`(새로) | EditorWindow, 실행·복원 조정. Systems 내부에 직접 접근하지 않음 |
| `Assets/ProjectL/Editor/Boot/PlayFromBootToggle.cs` + 임시 시작 씬 도우미 | 기존 Apply와 테스트 실행의 시작 씬 충돌 해결 |
| 새 `TraitActivationPreview.unity`(Editor 도구용 씬 경로) | 필드·유닛 표시·출력 카메라·호스트 참조. 실제 저장 폴더는 현행 씬 폴더에 맞춤 |
| Editor용 Preview 프리셋 에셋 폴더(새로) | 특성 에셋/Kind/문맥/배치/합성 결과 입력. 원본 연출 값 복사 금지 |
| 관련 기존 EditMode 테스트 및 새 Preview 경계 테스트 | 아래 Phase별 의미 있는 경계 확인 |

`GlobalSystemFactory`·`FrameworkRuntimeFactory`·Global 그룹·EffectSystem/EffectScope·Core 계약/계산은 수정하지 않는 계획이다. 기존 운영 씬/Build Settings/SceneIndices를 바꾸지 않는다. 공유 Factory/Boot 연결은 구현 후 Framework 공유 문서에 기록하고, 에셋 작성자에게는 사용 안내를 작성하되 외부 발송은 하지 않는다.

## 4. Phase A — 공유 재생 조립 준비

**목적:** 테스트 호스트와 무관하게 운영 재생 동작을 보존하면서 공유할 입구를 만든다.

- [x] **A1. 사건 재생 조립 추출.** 설계 §10.2의 입력을 받아 CombatSceneEventPresenter/CombatPhaseTransitionPresenter/CombatSceneEventPlayback을 한 곳에서 생성한다. 반환 묶음은 참조만 보관한다. 기존 Display의 Bind/Clear/Release 순서·MarkReleased 책임은 이전하지 않는다. Factory 실패 시 이미 생성한 소유 자원 정리도 보장한다.
- [x] **A2. 초기·최종 사실 입력 공통화.** ShownFacts.Replace/Presenter.Refresh에 CharacterCombatFacts 목록 입구를 추가한다. 기존 CharacterData 입력은 ToFacts 변환 후 같은 동작으로 연결한다. 목록 스냅샷 수명·사실 자리·죽음 유지 정책을 그대로 보존한다.
- [x] **A3. EffectScope 재사용 입구.** CombatEffectWindow의 기존 서비스 생성자는 유지한다. 빌린 Scope를 받는 overload는 그 Scope를 새로 발급하지 않으며 창구 Release 시 StopAll만 수행한다. 여러 창구 동시 사용을 막는 소유자는 Preview Session이다. 서비스 내부 목록/반환 API를 직접 조작하지 않는다.
- [ ] **A4. 회귀 검증·기록.** Unity 컴파일, 관련 기존 Framework 전투 표시 테스트·CoreIntegration의 PullAndMarkPresentationTests/TraitPresentationContractTests를 확인한다. 기존 일반 공격·이동/취소·추가 공격·희생 재생을 Unity에서 대조한다. 사실 overload의 동일 표시와 Scope 재사용 후 새 창구 재생/회수를 경계 테스트로 확인한다.

**완료 기준:** 운영 Factory가 공유 조립을 사용하고 기존 수명/순서가 유지된다. 새 테스트 씬은 아직 없어도 된다. 공용 변경 목록·검증 결과·Unity 미확인 항목을 구분해 보고하고 **멈춘다**. TS-f 기존 미확인 재생을 이번 추출의 통과 증거로 대신 쓰지 않는다.

## 5. Phase B — Play Mode 호스트와 일반 발동

- [x] **B1. 전용 호스트/서비스 연결.** GameBootstrap 없이 기존 runtime을 생성해 BindScene → InitData → PostBindService → Initialize를 한 번 수행한다. Global Context로 필요한 서비스만 넘긴다. LifeCycle 진입·Job 등록·Run 준비는 하지 않는다. 활성 LifeCycle/전투 없음 확인, 부분 초기화 실패 정리, runtime 이중 생성 차단을 넣는다. UNITY_EDITOR 공개 입구는 도구 입력·상태·명령만 노출한다.
- [x] **B2. 테스트 씬/표시 조립.** 이름이 유일한 출력 카메라와 정상 URP/Cinemachine 설정, 선택한 View 맵 프랍을 BindScene 전에 준비한다. 작은 필드와 유닛 표시, 실제 환경/기차/전환 Presenter, 동적 UnitHUDLayer를 생성한다. 처음에는 고정 크기 필드와 좌표 입력을 쓴다. FieldCamera/Architecture 코드는 건드리지 않는다. 부족한 맵 카메라 참조는 명시 오류/검증 미지원으로 표시한다.
- [x] **B3. 일반 발동 생성기/세션.** View 유닛 키·입력 Stats/Health·소유 역할·점유 칸으로 계약상 유효한 초기 CharacterCombatFacts를 만들고, CombatEffectsEvent 하나의 결과를 생성한다. 초기 사실 설치 → Ordered.Attach → Play → 최종 사실 표시를 연결한다. 재생 상태/세대는 세션이 소유한다. 중단은 완료가 아니며 새 입력 오류는 장면 변경 전에 거절한다.
- [x] **B4. EditorWindow와 시작 씬 복원.** 특성 에셋/Kind·유닛/대상/영향 칸·Magnitude·문맥 입력, 원본 Inspector 열기, 재생·중단을 제공한다. 처음에는 일반 문맥만 활성화하고 나머지는 준비 중으로 표시한다. 임시 시작 씬 소유권을 Boot Apply와 공유한다. 원래 시작 씬·편집 씬 구성을 보존하고 저장 취소 시 진입을 취소한다. SessionState 기반 요청/복원으로 도메인 리로드를 견딘다.
- [ ] **B5. 기본 검증.** 자기 큐·대상 여러 명·영향 칸·사건형 유닛 행동을 확인한다. 입력 오류·빈 큐·줄 없음은 구별한다. Play From Boot 켜짐/꺼짐 각각 진입·복원, 창 닫기·Play 종료·초기화 실패, Domain Reload on/off와 Scene Reload on/off 조합에서 이중 Tick/중복 세션 여부를 확인한다. B3의 중단 뒤 늦은 콜백을 테스트한다.

**완료 기준:** 설정한 일반 Activation을 게임 진행 없이 반복해서 수동 재생할 수 있다. UIId.UnitHUDLayer 등록/해제와 운영 전투 비활성 조건을 확인한다. 검증·미확인 목록을 보고하고 **멈춘다**.

## 6. Phase C — 추가 공격과 대신 맞기

- [x] **C1. 추가 공격 생성기.** WithChildAttack 계약, 공격 그룹/주체, 고유 DeliveryIndex, 실제 수신자의 Contacts, 방향·지정 칸을 검증해 결과를 만든다. 발동 사건과 체력/사망 후속의 출처를 맞춘다. 한 번의 추가 공격과 복수 대상까지다. 큐 줄이 없으면 운영처럼 기본 공격을 표시한다.
- [x] **C2. 반응 생성기.** 정상 공격 결과 안 TraitActivations에 WithInterception을 넣는다. Guardian/Protected/ActualRecipient 및 OriginalTargetId/접촉 순번/공격 출처를 일치시킨다. 희생 생존/사망을 만들고 지켜진 유닛과 수호자 사실을 구별한다. 일반 EffectsEvent로 희생을 위조하지 않는다.
- [x] **C3. 입력/진단 연결.** 문맥별 필수 필드를 창에 노출한다. 지원하지 않는 Kind/문맥 조합은 이유를 표시하고 거절한다. 공격 덮어쓰기·일반 공격 fallback·생략 경고는 운영 정책을 그대로 보여 준다. 문맥을 Kind로 자동 판정하지 않는다.
- [ ] **C4. 계약/시각 검증.** 추가 공격 피격·사망의 출처 연결, 누락/중복 Contacts·반응 불일치 거절을 테스트한다. Unity에서 근접/직선 투사체/곡사 중 현재 가능한 외형을 사용해 접촉·피격 타이밍을 확인한다. 희생 나가기→공격→옮긴 몸 피격→복귀, 사망 시 복귀 없음, 재생 중 중단을 확인한다. Core 넉백 요청의 해결 여부를 이 도구 성공으로 대신 판단하지 않는다.

**완료 기준:** 세 문맥이 실제 운영 재생 경로로 동작한다. 별도 이동/공격 구현 없이 화면을 확인한다. 결과를 보고하고 **멈춘다**.

## 7. Phase D — 프리셋·반복·수명 마무리

- [x] **D1. 공유 프리셋.** Editor 전용 SO에 원본 에셋/Kind/문맥/유닛 키/배치/합성 입력만 저장·불러온다. 원본 SO 수정은 Inspector에 맡긴다. 삭제 에셋/사라진 Kind/잘못된 좌표를 명시 오류로 표시한다. 기본 사례 프리셋을 만들되 원본 TraitVisual 값은 자동 변경하지 않는다.
- [x] **D2. 반복/최신 값.** 실행 전 DataBaseFactory.Create로 새 Query를 검증하고 Preview 표시 조립/캐시에만 전달해 View/설정 값을 새로 읽는다. 같은 runtime에 InitData를 다시 호출하지 않는다. 공유 Scope 한 개를 재사용한다. 완료 뒤 잔상 확인(기본 1초, 조정 가능) → 반복 간격 → 회수/새 초기 배치 순서로 진행한다. SO/의존 에셋 또는 창 입력 변경 시 반복을 멈추고 다음 재생부터 적용한다. Render DB 변경은 환경 재시작으로 안내한다. Catalog 전부 재검증 비용은 첫 버전에 수용하며 측정 없이 부분 DB 재설계로 범위를 넓히지 않는다.
- [x] **D3. 종료·실패 수명.** 예외 시 반복을 즉시 중단한다. Ordered.Detach → 실행/표시 해제 → runtime.Shutdown → 호스트 소유 객체 정리 → 에디터 설정 복원을 수행한다. 한 단계 실패해도 남은 반환을 시도하고 오류를 모아 보고한다. 창 닫기/씬 종료/Play 종료/스크립트 재로드의 중복 정리는 안전해야 한다. 전역 Time.timeScale은 변경하지 않는다.
- [ ] **D4. 최종 검증.** 각 문맥 반복 20회 후 유닛·HUD 등록·이펙트·대여·구독 누적 없음, Scope 신규 발급 1회, SO 변경 다음 재생 반영, 지연 큐·투사체 도중 중단·재시작을 확인한다. 사건 완료/전체 재생 완료/잔상 확인 시간을 구별한다. Editor 전용 코드가 비개발 빌드에 새 의존을 남기지 않는지 컴파일/빌드 구성을 확인한다. DB 에셋을 바꿨으면 DataBaseAssetTests도 실행한다.
- [x] **D5. 인계.** 열기·설정·세 문맥 예시·프리셋·실제 판정과의 차이·환경 재시작 조건을 View 사용 안내에 기록한다. 설계/이 계획의 상태와 근거를 갱신하고 공유 대상별 변경 메모를 작성한다(미전달).

**완료 기준:** 세 문맥을 프리셋에서 불러와 반복 확인할 수 있고 자원/설정 복원이 검증된다. 실제 전투 테스트 환경은 다음 설계로 남긴다. 보고 후 **멈춘다**.

## 8. 원칙·영향 검토

SSOT는 원본 SO/운영 재생 정책/세션 입력의 소유권 분리로 지킨다. 합성 생성기는 공식 대신 참조 무결성만 검사한다. SOLID는 창·문맥 생성·세션 수명·Factory 조립으로 분리하고 기존 취소/해제 계약을 유지한다. 편의용 서비스 루트 재등록이나 DTO의 일괄 공개는 하지 않는다.

| 변경 시나리오 | 수정·이해·검증 범위 |
|---|---|
| 특성 큐/행동 조정 | 원본 SO + 해당 프리셋 재생. 생성기·Core·입력 수정 없음 |
| 창 배치/검색 개선 | Editor 창과 UI 입력 확인. 재생 구현·합성 계약 변경 없음 |
| 새로운 결과 모양 지원 | 문맥 생성기/DTO와 관련 계약 검증. 기존 특성마다 분기 추가 안 함 |
| 공유 조립/사실 목록/Scope overload 변경 | 해당 구현 + 운영 일반 공격·이동/취소·추가 공격·희생 회귀 |
| 서비스 수명/Boot 시작 씬 연결 변경 | 생성→내부 등록→Group Export→소비자 조회 확인, 재생 종료와 일반 Play From Boot 회귀 |

FW-001/002/003/004/010/011/012/016과 JMH Factory/Scope/Handle·수명 원칙을 점검한다. 동적 HUD는 FW-008/009와 Assets/AGENTS.md를 따른다. 초기화에 전체 런타임/전체 Catalog가 필요하다는 공통 의존성은 이번 기능의 독립성 한계로 명시한다. 관련 없는 기능의 테스트를 관성적으로 전부 반복하지 않되, 공유 계약 변경 영향은 위 표대로 확인한다.

## 9. 실행 기록

- **2026-10-30:** 사용자 Play Mode 전용 씬 확정. 소스 의존성 대조 후 설계 §10과 Task A1~D5 작성. 기존 작업 트리의 소스/에셋은 수정하지 않음. Unity 컴파일·EditMode·Play Mode 검증은 미실행. 다음 구현 후보는 **Phase A만**이며, 이번 요청으로 구현을 시작하지 않는다.

### 9.1 A~D 구현 (2026-10-30 후속 요청)

- **승인:** 사용자 “계획대로 Phase A~D 순차적으로 구현하고 마지막에 검증 및 내용 정리해줘”. 단계별 진행 상황을 보고하면서 계속했다.
- **A 코드:** A1~A3 구현. 공유 조립 Factory, 사실 목록 overload, 빌린 Scope overload를 추가했다. 기존 Scope 두 인자 생성자의 null 검증 호출과 모호해지지 않게 새 입구는 `(서비스, 부모, Scope)` 세 인자로 했다. 운영 수명 소유자는 바꾸지 않았다.
- **B 코드:** B1~B4 구현. UNITY_EDITOR 호스트/공개 창구/입력, 전용 씬, 표시 조립, Editor 창, Boot 임시 시작 씬 연결. 현재 씬은 로컬 YAML 에셋으로 작성했고 로컬 fileID 참조를 정적 검증했다. 운영 씬에서 표시 컴포넌트 설정과 필수 체력 템플릿만 가져왔다. 손패·포기·정보창 등은 가져오지 않았다. Unity 씬 생성기는 파일이 없을 때 같은 구성을 만드는 복구 입구다.
- **C 코드:** C1~C3 구현. ChildAttack/Interception 문맥별 생성기와 진단. 접촉 순번·수신자·공격 출처가 대응한다. 치명타는 Core의 기존 사건 생산 형태처럼 Died 하나(살아 있던 Expected → 죽은 Final)를 만들고 별도 HealthChanged를 먼저 넣지 않는다.
- **D 코드:** D1~D3·D5 구현. 입력 프리셋 4개, 반복·잔상 확인·취소·설정 변경 중단, 공개 완료/Scope 발급 진단. 필드·유닛 템플릿에서 실행별 인스턴스를 새로 만들어 풀/외형 캐시까지 교체하고 실행 자식을 반환한다. Scope는 세션당 하나다. 사용 안내와 Framework 공유 메모를 작성했다(미전달).
- **범위 유지:** Core·FieldCamera·Architecture·원본 TraitVisual·운영 씬·Build Settings·asmdef는 수정하지 않았다. 동시 작업 트리의 다른 AI 변경을 원복하지 않았다. CombatAttackStart using 보완을 문의해 사용자 허용을 받았으나, 동시 변경으로 오류가 해소되어 그 파일에 이 작업의 보완은 하지 않았다.

### 9.2 검증 근거와 미확인

| 검증 | 결과 |
|---|---|
| MSBuild ProjectL.Systems (Editor 정의) | 오류 0, 기존 경고만. 로그 `Temp/TraitPreview/final-systems-build.log` |
| MSBuild Assembly-CSharp-Editor | 오류 0. 기존 참조 버전 충돌 경고는 남음. 로그 `Temp/TraitPreview/final-editor-build.log` |
| MSBuild ProjectL.Tests.CoreIntegration | 오류 0. 새 TraitActivationPreviewTests 9건은 **컴파일했으며 실행하지 않음**. 로그 `Temp/TraitPreview/final-tests-build.log` |
| Systems UNITY_EDITOR/DEVELOPMENT_BUILD 제거 컴파일 | 오류 0. 스크래치 targets로 정의만 제거하고 확인. 로그 `Temp/TraitPreview/non-editor-build.log`. 플레이어 빌드 전체 성공 증거는 아님 |
| 전용 씬 로컬 참조·새 meta·기본 프리셋 | 로컬 참조 정적 검사 통과. 25개 문서, 프리셋 4개. Unity에서 열어 시각 확인한 결과는 아님 |
| 옛 ProjectL.Tests.Framework.csproj MSBuild | 현재 파일 시스템에 없는 테스트 경로(CS2001) 때문에 차단. 생성 csproj/다른 테스트 파일은 수정하지 않음 |
| Unity Test Runner·실제 Play·20회 반복·설정 복원 조합 | **미실행**. 다른 AI 동시 작업과 충돌을 피하기 위해 현재 에디터를 조작하지 않음 |

처음 Unity Computer Use 접근은 도구에서 거절됐다. 사용자는 이후 “다른 AI가 다른 작업 진행 중인데 충돌 안나면 접근…안될 경우엔 2번”이라고 지시했다. 실제 같은 작업 트리의 코드 변경이 진행되는 것을 확인해 화면 조작을 생략했다. CLI로 같은 프로젝트의 두 번째 Unity를 띄우거나 현재 씬/Play를 바꾸지 않았다.

**잔여 Task:** A4의 운영 회귀 실행, B5, C4의 실행 검증, D4의 실제 반복/종료/복원 검증. 상세 절차는 [사용 안내 §5](../viewDocs/trait-activation-preview-guide.md#5-남은-unity-확인). 따라서 이 계획 전체를 검증 완료로 표시하지 않는다.

### 9.3 실제 Unity 검증 (사용자 동시 작업 종료 후)

- **승인:** 사용자 “지금 다른 측 작업 끝났으니 검증 진행해줘”. 기존 Unity 한 인스턴스에서 관련 EditMode 테스트와 전용 씬 재생을 실행했다. 운영 전투에는 진입하지 않았다.
- **발견·수정:** 합성 Stats의 필수 Category에 stateless Rider를 지정하고, 가로채기 공격 결과에 Unbudgeted·비용 0의 필수 ActionCostSettlementSnapshot을 넣었다. 기존 생성자 계약을 준수하는 합성 입력 수정이며 Core 수정은 없다. 전용 씬에는 Boot의 기존 AudioManager 구성과 출력 카메라의 FMOD StudioListener를 추가했고 씬 생성기도 맞췄다. AudioManager 누락으로 발생하던 초기화 실패는 재실행에서 해소됐다. StudioListener 추가 후 컴파일·EditMode는 확인했으나 아직 후속 Play는 하지 않았다.
- **검증 도구:** Editor 메뉴 `ProjectL > Combat > Verify Trait Activation Preview`에 관련 TestRunner API 실행, 런타임 반복/취소/설정 조합 검사, 중단·복원 입구를 추가했다. 결과는 무시 디렉터리 `Temp/TraitPreview/`에 기록한다. 설정 조합 실행 전 기존 씬 목록·시작 씬·PC Boot 설정·Enter Play Mode 설정을 보관하고 실패 시에도 반환한다.

| 실제 검사 | 결과·근거 |
|---|---|
| Unity 컴파일 | 재컴파일 성공. 새 Editor 검증 도구도 실제 Unity에서 실행됨 |
| TraitActivationPreviewTests | **9/9 통과**. null Category·필수 비용 정산 누락은 테스트 발견 후 보완 |
| TraitPresentationContractTests | **2/2 통과** |
| PullAndMarkPresentationTests | **19/20 통과**. `CancelOrSceneExit_ReleasesMotionAndMarkVisuals(False)`의 SetUp에서 Unity 네이티브 오류 `The serialized array of [SerializeReference] objects is missing entry for Refid -2`가 발생. 원인은 미확정이며 임의 억제하거나 관련 에셋을 수정하지 않음 |
| 관련 테스트 합계 | **30 통과·1 실패**, 재실행도 같은 결과. `editmode-results.xml`, `editmode-summary.txt`; 이전 실행은 `editmode-before-runtime.xml` |
| Moment_Acceleration | 20회 반복 뒤 Scope 1개·유닛 3개·HUD Layer 1개·ParticleSystem 2개. 중단 후 30프레임 동안 완료 횟수 증가 없음 |
| ChildAttack_DoubleShot | 20회 반복 뒤 Scope 1개·유닛 3개·HUD Layer 1개·ParticleSystem 29개. 중단 검사 통과 |
| Interception_Sacrifice | 위와 같은 20회 반복·자원 수·중단 검사 통과. 수호자가 앞으로 이동하는 실제 화면 확인 |
| Interception_Sacrifice_Death | 위와 같은 20회 반복·자원 수·중단 검사 통과 |
| 자원 측정 범위 | 각 사례 워밍업 2회 후 Stop·4프레임 대기 수치와 20회 후 Stop·4프레임 대기 수치를 비교. 내부 대여/구독 목록을 직접 계측한 증거는 아니므로 모든 자원 누수 부재로 일반화하지 않음 |
| 환경 종료 | **실패**. OptionInputRouter.Shutdown이 미등록 UIId.Setting에 CloseUI를 호출. `runtime-missing-setting.txt` 및 `runtime-verification.txt`에 전체 스택/앞선 통과 기록 |
| 원래 편집 환경 | 실패 처리 후 Combat 편집 씬 복귀, Play 종료, ProjectSettings diff 없음. 전체 시작 씬·설정 조합 자동 대조는 종료 오류로 미도달 |

재생 진입 시에도 같은 Refid -2 오류가 Console에 보였다. 반복 검사 중 추가 오류가 없다는 사실과 에디터 로그가 깨끗하다는 주장은 구분한다. FMOD Listener 누락 경고는 전용 씬 구성으로 보완했다.

**당시 계획 변경 확인 요청:** 공용 옵션 입력기는 Setting UI 등록을 종료 전제로 가진다. 기존 GlobalUICanvas 프리팹을 Preview 호스트가 BindScene 전에 생성·등록하고 runtime.Shutdown 뒤 회수하는 보완을 제안했다. 공용 입력기나 Core는 수정하지 않았으며, 후속 사용자 결정은 §9.4를 따른다.

**남은 검증:** 종료 의존성 보완 후 네 사례 및 Domain/Scene Reload×Play From Boot 8조합 재검증, 원본 SO 수정의 다음 재생 반영·창 닫기/스크립트 재로드, 지정된 지연 큐·투사체 도중 중단, 운영 일반 공격·이동/취소 회귀. A4/B5/C4/D4 전체 완료 표시는 유지 보류한다.

### 9.4 사용자 후속 결정 — 종료 오류 유지·View 테스트 코드 정책

- 사용자는 테스트 환경의 종료 오류를 그대로 두도록 결정했다. GlobalUICanvas 연결 보완과 이를 전제로 한 추가 검증은 진행하지 않는다. 미검증 항목을 통과로 바꾸지 않는다.
- 앞으로 View 측 작업에서는 테스트 코드를 새로 만들지 않는다. 필요한 확인은 코드 검토·컴파일·수동 재생을 기준으로 진행한다.
- 이번에 추가한 `TraitActivationPreviewTests.cs`와 Editor 검증 도구 `TraitActivationPreviewVerifier.cs`는 삭제 예정이다. 사용자가 향후 삭제 의사를 밝혔으며 이번 후속 처리에서는 코드·meta 파일을 삭제하지 않았다. 미리보기 기능 자체와 검증 전용 코드는 구분한다.
- 사용자 확인 대기 상태를 해제하고 이번 검증 작업은 확인된 결과·제한을 남긴 상태로 마무리한다. 커밋·push는 하지 않았다.

### 9.5 사용자 요청에 따른 이번 테스트 코드 삭제·사용 안내 정리

- 사용자 “이번 테스트 코드 삭제해주고, 테스트 환경 내가 어떻게 써야하는 지 정리해줘”에 따라 `Assets/Tests/EditMode/CoreIntegration/TraitActivationPreviewTests.cs`, `Assets/ProjectL/Editor/CombatPreview/TraitActivationPreviewVerifier.cs`와 각각의 `.meta`를 삭제했다. 자동 TestRunner/반복 검증 메뉴도 제거된다. 기존 다른 테스트와 미리보기 기능·전용 씬·프리셋은 유지한다.
- §9.3의 테스트 실행 결과는 삭제 전 이력이다. 해당 검증 도구를 다시 실행할 수 있다는 안내로 사용하지 않는다. Setting 종료 오류 유지 결정도 그대로다.
- [사용 안내](../viewDocs/trait-activation-preview-guide.md)를 실제 창의 버튼·입력과 대조해 처음 프리셋 재생 → 원하는 특성/Kind/문맥 설정 → SO 수정·반복·입력 저장 → 종료 순서로 정리했다. 인덱스와 외형 키, 합성 연출과 실제 규칙의 차이를 명시했다.
- 삭제 대상 4개 파일 부재와 Assets 코드·에셋 내 삭제 타입 참조 없음 확인. Editor MSBuild 종료 코드 0이며 기존 MSB3277 참조 버전 경고는 남는다. Unity 생성 csproj에 남아 있던 삭제 파일 항목은 스크래치 `Temp/TraitPreview/removed-test-sources.targets`로 빌드에서만 제외했고 생성 csproj는 편집하지 않았다. 로그는 `Temp/TraitPreview/test-removal-editor-build.log`다. 이번에는 자동 테스트나 추가 Play 검증을 실행하지 않았다.

### 9.6 Refid -2 원인 확인과 한 에셋 재저장

- 사용자가 “환경 열기 / 재생” 때 오류가 발생한다고 알려 줬다. 일반 Boot 진입 로그에도 22건이 있었고, enhanced null의 참조 목록 항목이 누락된 성장 SO가 22개였다.
- 사용자 “한 개 재저장·비교 허용”에 따라 `TraitGrowth_Acceleration.asset`만 Unity Inspector에서 재저장했다. Unity가 `references.RefIds`에 null 항목 `rid: -2`를 추가했다. 기본 수치·이름/설명·강화 null·meta/GUID 유지 확인.
- 같은 버튼으로 Play 진입 후 로그 증가 구간을 비교해 **22건 → 21건** 감소를 확인했다. 새 테스트/검증 도구 코드는 작성하지 않았고 Core 로직도 수정하지 않았다. 한 에셋의 참조 목록 누락이 오류를 유발했음을 확인한 결과이며, 전체 해결로 기록하지 않는다.
- 나머지 같은 형식의 성장 SO 21개는 아직 수정하지 않았다. 범위·자료·Core 전달 기록은 [DB 경계 인계 §5.66](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#566-가속-성장-so의-null-참조-목록-재저장-2026-10-30-한-에셋-사용자-승인-core-로직-변경-없음-미전달).

### 9.7 남은 21개 보완과 Refid 오류 0건 확인

- 사용자 “나머지 21개도 정리” 승인에 따라 가속에서 Unity가 생성한 null 참조 목록 항목 두 줄을 남은 21개 성장 SO에도 동일하게 보완했다. 게임 값의 재직렬화는 하지 않았다.
- 21개 모두 보완 두 줄을 제거한 원문이 변경 전 백업과 정확히 같고, `.meta` 해시도 같음을 확인했다. 기본/강화 정의·설명·강화 null·GUID 보존.
- Unity Refresh 후 같은 “환경 열기 / 재생”으로 전용 씬 진입을 확인했다. 진입 시점부터 새로 기록된 로그의 Refid -2 오류는 **0건**이다. 이전 22건 → 가속 1개 처리 후 21건 → 전체 22개 처리 후 0건. 재생 종료 후 원래 Combat 편집 씬으로 복귀했다.
- 원본/비교 자료는 `Temp/TraitPreview/null-registry-before/`, `null-registry-fixed-play.log`다. Core 전달 기록은 DB 경계 인계 §5.67. 새 테스트 코드·진단 도구·Core 규칙 수정·스테이징·커밋·push는 하지 않았다.
- 이번 완료는 null 참조 목록 누락에 따른 직렬화 오류 보완이다. 기존 Setting 종료 제한, 미리보기 전체 연출/설정 조합 확인은 기존 기록과 구분한다.

### 9.8 환경 진입 후 빈 화면 수정

- 사용자 “환경 열기 / 재생으로 TraitActivationPreview 진입해도 아무것도 안 보임”을 같은 Unity 인스턴스에서 재현했다. Refid 오류와는 별개인 두 원인이었다.
- **최대화 진입:** Unity `WindowLayout.MaximizePrepare`가 입력 창을 `playing=False, isPlayingOrWillChangePlaymode=True` 상태에서 파괴했다. 창의 OnDestroy가 시작 씬 소유권을 복원하고 대기 요청을 지워 호스트는 준비 상태에 머물렀다. Play 진입 중의 임시 창 파괴는 종료로 처리하지 않고, 첫 요청의 소비를 창 Update에서 기존 정적 시작 씬 소유자 `PreviewPlayStartScene`으로 옮겼다. 요청은 호출 전에 한 번 소비하고 오류도 SessionState에 보관한다.
- **표시 조립:** 최대화를 끄고 재생하면 `FieldUnitPresenter`의 필수 HUD Category Visual Query 누락으로 초기화가 실패했다. Preview Factory에 `ConfigureHUDCategories(tables.Categories)`를 추가해 운영 Factory와 같은 표시 계약을 연결했다.
- **구조 점검:** 요청 상태는 정적 소유자 한 곳, 입력은 창, 의존성 조립은 Factory, 재생 수명은 기존 Session이 맡는다. FW 서비스 등록/수명과 Core·운영 표시기·FieldCamera·Architecture는 수정하지 않았다. 이 수정의 검증에 운영 전투나 특성 규칙 변경을 요구하지 않는다.
- **컴파일:** Unity 스크립트 재컴파일 성공. Editor MSBuild 종료 코드 0, 기존 참조 버전 경고만 남음(`Temp/TraitPreview/blank-preview-final-build.log`). 일시적인 수명 추적 로그는 원인 확인 뒤 제거했다. 새 테스트/검증 도구 코드는 만들지 않았다.
- **수동 재생:** 현재 입력(2연발, ChildAttack, 유닛 3개)에서 Play Focused 첫 재생 완료 1회 → 중단·초기 배치 복원 → 다시 재생 완료 누적 2회를 확인했다. Play Maximized도 첫 버튼 한 번으로 맵·유닛 3개·체력바 표시와 재생 완료 1회를 확인했다. 최대화 해제 전에 이미 화면이 표시됐으며 입력 창을 다시 열어 요청을 전달한 결과가 아니다.
- **로그·복귀:** 각 실행의 진입부터 종료 전까지 새 로그에서 예외·Refid 누락·HUD Category 오류 0건(`blank-fixed-focused-play.log`, `blank-fixed-maximized-play.log`). 환경 종료 버튼으로 원래 Combat 편집 씬에 복귀했고 Play Maximized 설정도 원래 값으로 유지했다. 사용자 결정으로 남긴 Setting 종료 예외는 그대로이며 무오류 종료를 주장하지 않는다.
- 선택돼 있던 `TraitVisual_DoubleShot`의 `activations`는 비어 있어 특성 전용 큐는 없다. 위 완료는 합성 추가 공격 재생이며 원본 연출 에셋을 작성한 것은 아니다. 전체 Reload/Boot 조합 및 운영 전투 회귀의 기존 미확인 상태도 유지한다. 스테이징·커밋·push는 하지 않았다.

### 9.9 출력 카메라 참조 복구 (2026-10-31)

- 사용자 필수 참조 예외를 확인했다. 전용 씬의 호스트 `output`이 `{fileID: 0}`으로 저장돼 있었고, 기존 `TraitPreviewOutput` 오브젝트의 Camera(`fileID: 5003`)는 정상적으로 남아 있었다.
- 씬의 해당 참조 한 줄만 `{fileID: 5003}`으로 복구했다. 다른 씬 변경·표시 설정은 보존했다. Catalog GUID와 Field/Units/Output의 로컬 컴포넌트 참조를 정적으로 대조했고, Unity에서 수정한 씬을 Reload했다.
- 사용자의 Unity 직접 입력이 감지돼 재생 검증을 더 진행하지 않았다. 따라서 이번 결과는 필수 참조 복구·씬 반영 확인이며 실제 희생 재생 통과 기록은 아니다. 코드·새 테스트·Core·시작 설정은 수정하지 않았다.
