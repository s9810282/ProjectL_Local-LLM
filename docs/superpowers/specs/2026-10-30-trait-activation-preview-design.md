# 특성·Activation 즉시 재생 환경 설계

> 2026-10-31 후속: 전후 사건·상태를 공통 블록으로 조립하는 [사건 블록 조립 설계](2026-10-31-composable-trait-preview-design.md)와 [Task 계획](../plans/2026-10-31-composable-trait-preview-plan.md)을 작성했다. 후속은 구현 전이며 아래 단일 발동 도구의 실행 이력과 구분한다. 새 View 테스트/자동 검증 도구는 추가하지 않는 최신 사용자 지침을 따른다.

- 날짜: 2026-10-30
- 상태: **A~D 구현, 실제 네 프리셋 각각 20회 반복·중단 확인. 종료 시 Setting UI 오류는 사용자 결정으로 유지하며 전체 검증은 미완료.** 상세 조립안은 §10, 최신 근거는 [Task 계획 §9.3](../plans/2026-10-30-trait-activation-preview-plan.md#93-실제-unity-검증-사용자-동시-작업-종료-후)을 따른다.
- 요청: 특성과 Activation을 골라 바로 재생하는 환경을 먼저 만들고, 실제 전투 조건 검증 환경은 뒤에 다룬다.
- 조사 기준: 현재 작업 트리. 유닛 행동(TS-f) 등 커밋 전 변경이 있으므로 특정 커밋의 완료 상태로 간주하지 않는다.
- 이 문서는 테스트 도구의 설계다. 특성 규칙과 운영 연출 정책은 기존 원본을 따른다.

## 1. 목표와 확인 범위

**특성 외형 에셋의 Activation 한 줄을 선택하고, 필요한 유닛·대상·칸을 배치해 실제 전투와 같은 연출 경로로 반복 재생한다.**

확인 대상은 큐 위치·지연, 유닛 행동, 추가 공격 표시, 대신 맞기, 피격·사망과의 연결이다. 실제 발동 조건·피해 공식·턴 규칙은 이 도구에서 계산하지 않는다. 입력한 피해·상태는 화면을 확인하기 위한 합성 사실이며, 실제 Core 판정 통과를 뜻하지 않는다. 창에 항상 `연출 확인 / 합성 결과`를 표시한다.

성공 기준은 “게임을 진행해 조건을 만들지 않고 한 설정을 반복 확인할 수 있음”, “운영과 같은 처리기·순서·자원 대여를 사용함”, “반복·중단 뒤 이전 실행의 자원이 남지 않음”이다.

## 2. 확인한 현재 구조

| 사실 | 코드 원본 | 설계에 주는 제약 |
|---|---|---|
| Activation 조회 키는 특성 키와 Kind | `TraitVisualData.TryGetActivation` | 선택 목록을 별도 특성별 코드로 복제하지 않는다 |
| 한 줄에 큐·추가 공격 표시 덮어쓰기·유닛 행동이 있음 | `TraitActivationVisual` | 줄의 설정을 직접 재생 코드로 다시 구현하지 않는다 |
| 일반 발동·추가 공격은 발동 사건 처리기를 지남 | `CombatTraitEventHandler` | 일반 발동도 실제 사건을 만들어 넘긴다 |
| 추가 공격 여부는 Kind만이 아니라 ChildAttack 사실로 판단 | `CombatEventTypeTable`, `TraitActivationSnapshot.WithChildAttack` | FollowUpAttack 선택만으로 공격이 생기지 않는다 |
| 희생 가로채기는 발동 사건이 아니라 공격 결과 안 반응 | `TraitActivationSnapshot.WithInterception`, `CombatAttackStart` | 희생을 일반 발동 사건으로 만들어 재생하면 안 된다 |
| 발동 줄은 처리기 연결 동안 캐시됨 | `CombatTraitEventHandler._rows` | SO 수정 후 재실행은 캐시까지 새로 만들어야 한다 |
| 사건 완료 통지는 전체 연출 완료와 다름 | `ICombatEventPlayback`, `OrderedCombatPresentation` | 큐·유닛 행동·공격 후속까지 끝나야 반복한다 |
| 전체 표시 Factory는 HUD·가방·정보창 등도 요구 | `CombatSceneDisplayFactory.Create` | 도구 편의를 위해 운영 필수 참조 검사를 약화하지 않는다 |

특성별 시기·발동 종류는 [현존 특성 표](../viewDocs/combat-trait-activation-timing.md), 유닛 행동·가로채기는 [유닛 행동 설계](2026-10-30-combat-trait-unit-action-design.md)가 원본이다.

## 3. 작업 화면 제안

**EditorWindow에서 설정하고 전용 테스트 씬의 Game View에서 Play Mode로 본다.** 메뉴 이름 제안: `ProjectL > Combat > Trait Activation Preview`.

| 영역 | 제공할 것 |
|---|---|
| 특성 선택 | View 특성 에셋 검색·선택, 해당 에셋의 Activation Kind 목록, 원본 Inspector 열기 |
| 재생 문맥 | 일반 발동 / 추가 공격 / 공격 반응(대신 맞기) |
| 유닛 | 발동 유닛, 대상 목록, 반응 문맥의 공격자·지켜진 유닛. 외형은 View DB 키로 선택 |
| 배치 | 작은 고정 격자에서 칸·방향 입력, 대상 추가·삭제. 처음에는 좌표 입력으로 충분 |
| 결과 재료 | 영향 칸, Magnitude, 공격 방향·지정 칸, 추가 공격·반응에 필요한 피격 대상과 표시 피해·생존/사망 |
| 조작 | 재생 / 중단·초기화 / 반복, 반복 간격, 설정 저장·불러오기 |
| 진단 | 선택한 키·Kind·문맥, 줄 유무, 큐·행동 유무, 공격 덮어쓰기/일반 공격 사용, 경고·오류, 재생 상태 |

원본 Activation 값은 기존 SO Inspector에서 고친다. 창에 큐·행동 편집기를 한 벌 더 만들지 않는다. 다음 재생은 수정된 값을 새로 읽으며 실행 중인 재생에는 반영하지 않는다. 설정 프리셋에는 연출 값을 복사하지 않고 에셋 참조·Kind·배치·합성 사실 입력만 저장한다.

기본 목록은 실제 에셋에 있는 줄이다. 줄이 없는 Kind의 확인은 `줄 없음 확인` 선택을 따로 둔다. 모든 Kind가 해당 특성에서 실제 발동한다고 표시하지 않는다. 줄이 없어도 운영과 같이 추가 공격 자체는 재생될 수 있다.

## 4. 세 재생 문맥

### P1. 일반 발동

`CombatEffectsEvent`에 선택한 특성 발동 사실을 넣는다. 발동 유닛·대상 목록·영향 칸·Magnitude를 입력한다. 큐와 사건형 유닛 행동을 확인한다. 체력·상태·흔적이 필요한 경우는 별도의 명시적 후속 사실을 넣으며, Kind 이름으로 효과를 추측해 생성하지 않는다.

최초 버전은 큐·유닛 행동과 공격에 수반되는 피격·사망까지다. 모든 상태/HUD·흔적의 편집기는 후속 범위다. 예를 들어 TrailCreated 줄의 큐 확인은 가능하지만 실제 흔적 생성은 별도 환경 사실을 넣기 전에는 검증 대상이 아니다.

### P2. 추가 공격

발동 사실에 계약을 만족하는 `ChildAttack`을 넣고, 같은 공격 출처를 가진 피격·체력·사망 사건을 이어 붙인다. 공격 표시 덮어쓰기와 유닛 일반 공격으로의 복귀를 운영 규칙 그대로 확인한다. 첫 버전은 추가 공격 한 번과 대상 여러 명까지이며 재귀적인 추가 공격 연쇄 편집은 제외한다.

### P3. 공격 반응 — 대신 맞기

공격자·지켜진 유닛·수호자를 배치한다. 수호자가 발동 유닛이고 지켜진 유닛이 발동 대상이다. 가로채기를 정상 공격 결과 안에 넣고 실제 수신자의 피격·체력·사망 후속을 이어 붙인다. 나가기 → 공격 → 대신 맞기 → 생존 시 복귀를 확인한다.

계약상 가로채기를 담을 수 없는 Kind는 이 문맥으로 재생할 수 없다. `TraitActivationSnapshot` 등의 생성자 검사를 그대로 통과해야 하며 리플렉션으로 검사를 우회하지 않는다. 희생의 넉백 규칙은 기존 Core 요청 범위이며 이 도구에서 판정하지 않는다.

문맥은 **합성 사실을 담는 위치**이고 Activation 줄의 새 필드가 아니다. Kind나 행동 설정만으로 문맥을 자동 결정하지 않는다. 입력이 맞지 않으면 누락·충돌 필드를 구체적으로 표시한다.

## 5. 구조와 경계 제안

```text
Editor 창 ── 설정/재생 요청 ──► Preview Session
                                  │
                        입력 검증·합성 결과 생성
                                  │
                    OrderedCombatPresentation (공유)
                                  │
                    ICombatEventPlayback (공유)
                                  │
                운영 처리기·조각·유닛 표시·이펙트 서비스
```

- **Editor 창**: 선택·입력·프리셋·세션 명령만 담당한다. 사건 순서나 연출 구현을 모른다.
- **합성 결과 생성**: 입력을 초기 표시 사실과 불변 `CombatResult`로 바꾼다. 일반 발동·추가 공격·반응의 생성은 문맥별로 나눈다. 특성별 switch와 피해 공식은 두지 않는다.
- **Preview Session**: 생성·재생·전체 완료·반복·취소·실패 상태와 한 실행의 수명을 소유한다. 운영 Buffer 확인·Flow·Run 변경은 호출하지 않는다.
- **Preview Factory**: 세션 의존성 조립을 담당한다. 기능 구현은 하지 않는다.
- **운영 재생 부품**: 특성 처리기, 공격 시작, 순서 재생, 피격·사망, 몸 위치 조회, 자원 대여·반환을 그대로 사용한다. 도구 전용 이펙트/유닛 이동 재생기를 만들지 않는다.

운영 `CombatSceneDisplayFactory` 전체를 호출하면 손패·포기창 등 이번 확인과 관계없는 화면이 필수가 된다. 따라서 **공유할 사건 재생 조립을 작은 Factory로 추출하고 운영/도구가 함께 사용**하는 안을 권장한다. 조립 추출과 도구 구현은 별도 Phase로 나눈다. 운영 필수 검사를 선택 사항으로 바꾸거나 가짜 null 창구로 대체하지 않는다.

의존성 대조 결과와 추출 경계는 §10에서 구체화했다. 그 범위를 넘어 운영 수명 계약 변경이 필요하면 구현하지 말고 설계를 보완해 사용자에게 묻는다.

### 실행 환경과 서비스

- 첫 버전은 Editor 전용 Play Mode 도구다. 빌드 진입점·운영 씬 번호·Run 목적지를 추가하지 않는다. 전용 씬은 운영 Combat 씬을 복제한 별도 운영 분기로 키우지 않는다.
- 기존 런타임 Factory/Global Context의 Render·Effect·Camera·Audio 서비스를 재사용한다(§10.1). 서비스 연결을 위해 Core·Buffer·LifeCycle 객체도 생성·초기화하지만 게임 구간·Run·전투에는 진입하지 않는다. 합성 사실은 운영 Buffer에 넣지 않는다.
- 등록 위치는 기존 Global 내부, 상위 Export는 Global Context, 소비자는 Context에서 받은 좁은 서비스다. 하위 서비스를 루트에 재등록하지 않는다. 세션 내부 협력 객체를 각각 System/Service로 등록하지 않는다.
- Framework 모듈은 AddModule과 정해진 초기화·Tick·Shutdown을 따른다. 세션과 Global 서비스의 Tick은 호스트 한 곳이 진행하고 Editor 창이 다시 Tick하지 않는다.
- EditorWindow는 게임 UI가 아니다. Game View 안에는 도구용 버튼 패널을 만들지 않는다. 게임 패널이 필요해지면 별도 설계에서 UIBase·UIId·Policy·UISystem 등록을 정한다.
- Editor 코드는 `Assets/ProjectL/Editor/CombatPreview/`에 두고, Systems 안의 `UNITY_EDITOR` 한정 공개 창구·입력 DTO만 사용한다. 기존 `Tool/Editor` asmdef는 Systems 참조가 없어 사용하지 않는다. 내부 타입의 일괄 public 변경·리플렉션·asmdef/InternalsVisibleTo 변경은 하지 않는다.

## 6. 수명·반복·수정 반영

1. `테스트 환경 열기`로 씬/Play Mode를 준비한다. 편집 중 씬이 있으면 Unity의 저장 절차를 따른다. Play From Boot 설정과 충돌할 때는 원래 시작 씬 설정을 보관하고 테스트 실행 동안만 대체한 뒤 복원한다. 정확한 현행 연결 위치는 계획 때 확인한다.
2. 재생 전에 모든 입력·참조를 검사한다. 검사 실패 시 장면을 부분 변경하지 않는다.
3. 이전 실행을 취소·해제하고 최신 View 설정으로 표시/처리기 연결을 새로 만든다. 초기 사실을 설치한 뒤 합성 결과 하나를 재생한다. 첫 버전은 속도보다 새 연결의 확실한 초기화를 우선한다.
4. 사건 소비와 유닛 행동·투사체·피격·사망·지연 큐 등 소유 실행이 모두 끝나면 `재생 완료 / 잔상 확인`으로 옮긴다. 파티클 자연 종료를 뜻하지 않는다(현재 Scope에 완료 조회가 없음). 설정한 잔상 확인 시간(기본 제안 1초)과 반복 간격 뒤 남은 이펙트를 회수하고 다음 실행을 시작한다. 수동 재생은 완료 뒤 화면을 유지해 중단/다음 재생 때 회수한다.
5. 중단은 완료로 처리하지 않는다. 취소 → 대여·이펙트 회수 → 표시 해제 → 초기 배치를 새로 표시한다. 이전 세대의 완료 콜백이 반복을 다시 열 수 없다.
6. SO 변경은 다음 재생부터 적용한다. 반복 중 설정이 바뀌면 반복을 멈추고 다시 시작하게 한다. 연결 캐시만 일부 비우는 전용 경로는 만들지 않는다.
7. 창 닫기·씬 종료·Play 종료·스크립트 재로드에서 구독과 요청을 끊고 세션을 해제한다. Domain Reload 설정이 달라도 정리가 중복 호출에 안전해야 한다. 이전 Scene Setup과 시작 씬 설정을 복원한다.

실패는 대상 에셋/Kind/문맥을 포함해 보고하고 반복을 멈춘다. DB 구성 오류를 빈 화면의 성공으로 처리하지 않는다. 반면 빈 큐·줄 없음처럼 운영이 허용하는 입력은 그대로 표시한다.

## 7. 프리셋과 첫 확인 사례

프리셋은 팀에서 공유 가능한 Editor 전용 설정 에셋을 권장한다. 문맥·에셋 참조·유닛 키·배치·합성 결과 입력을 보관한다. 실행 중 체력·Transform·서비스 핸들·파생 공격 결과는 저장하지 않는다. 원본 연출 SO를 자동 수정하지 않는다.

| 사례 | 확인할 것 |
|---|---|
| 자기 자신 큐 / 대상 여러 명 / 영향 칸 여러 개 | 몸·칸 기준 위치와 중복 재생 여부 |
| 가속 등 큐만 있는 줄 | 공격을 새로 만들지 않고 큐만 재생 |
| 사건형 유닛 행동 | 나가기·머물기·복귀, 큐와의 동시 진행 |
| 난사·2연발 등 추가 공격 | ChildAttack 입구·투사체·접촉·피격 순서, 덮어쓰기와 기본 공격 |
| 희생: 생존 / 사망 | 먼저 나가기·옮긴 자리 피격·생존 시 복귀·사망 시 복귀 없음 |
| 줄 없음 / 큐 없음 / 잘못된 문맥 | 운영과 같은 빈 연출, 원인 진단, 계약 위반 차단 |
| 재생 중 중단·창 닫기·다시 재생 | 대여·지연 큐·투사체·카메라 복원과 늦은 콜백 무효화 |
| SO 수정 뒤 재생 / 반복 20회 | 새 값 반영, 자원·유닛·구독 누적 없음 |

## 8. 원칙 검토와 검증 범위

| 기준 | 적용과 남는 확인 |
|---|---|
| SSOT | 연출 값은 View SO, 재생 정책은 운영 코드, 프리셋은 테스트 입력만. 합성 사실은 세션 전용이며 운영 Buffer에 쓰지 않음 |
| SRP | 창=입력, 문맥별 생성기=합성, Session=수명, Factory=조립, 기존 처리기=연출 |
| OCP | 특성 추가는 외형 줄 선택으로 대응. 새 결과 모양이 생길 때 문맥 생성기를 확장 |
| LSP | 기존 재생 계약의 취소/완료/해제 보장을 그대로 지킴. 도구에서 사건 완료를 전체 완료로 바꾸지 않음 |
| ISP | 창은 세션 명령·상태만, 재생 부품은 필요한 서비스만 받음 |
| DIP | 운영 내부를 창에서 찾아 조작하지 않음. 공유 재생 계약에 연결하고 구체 조립은 Factory에 모음 |
| JMH·FW | FW-001~004·010~012·016 중심. 등록/Export/조회 그래프·대여 반환·Build 1회·초기화 순서 확인. 입력/게임 UI를 추가하면 FW-005~009도 적용 |
| 독립성 | 큐/행동 값 수정은 SO+해당 프리셋 확인, 배치 UI 수정은 창만, 합성 반응 추가는 생성기+관련 확인. 공통 조립·취소 변경은 운영 회귀까지 필요 |

추출 전에는 도구에서 운영 전체 표시/UI를 이해해야 한다. 추출 뒤에는 공유 재생 조립과 필드/유닛 설치 계약을 이해하면 된다. 남는 공통 경계는 Mapper, 세대 취소, 표시 대여, Global 서비스 수명이다. 이 경계 변경 시 추가 공격·희생·일반 공격·이동/취소 회귀가 필요하다. Core·FieldCamera·Architecture 파일은 변경 대상으로 잡지 않는다.

자동 검증은 문맥별 합성 결과의 계약/공격 출처 연결, 취소 뒤 완료·반복 차단처럼 의미 있는 경계에 둔다. 시각·클립·이펙트·카메라 확인은 Unity Play Mode에서 한다. 조립 추출은 관련 기존 EditMode 테스트로 검증한다. DB 에셋 변경 시 DataBaseAssetTests도 실행한다. 이번 문서는 조사·설계만 했으며 테스트 실행 증거는 아니다.

## 9. 사용자 검토 항목과 다음 단계

| 번호 | 제안 | 이유 |
|---|---|---|
| Q-P1 | Editor 창 + 전용 Play Mode 테스트 씬 | 클립·투사체·서비스·카메라를 실제 실행 환경에서 확인 |
| Q-P2 | 첫 범위에 일반 발동·추가 공격·대신 맞기 모두 포함 | 현재 Activation의 서로 다른 입구를 전부 다룸 |
| Q-P3 | 원본 값은 Inspector에서 편집, 프리셋은 입력만 공유 저장 | 연출 값 사본과 별도 편집 규칙을 만들지 않음 |

Q-P1은 사용자 확정이며, 이후 “계획대로 Phase A~D” 구현 요청으로 Q-P2·Q-P3을 포함한 계획 범위도 승인됐다. Phase 경계는 (A) 공유 조립 추출·운영 회귀, (B) 테스트 호스트·일반 발동, (C) 추가 공격·대신 맞기, (D) 프리셋·반복·수명 검증이다. 이번에는 일괄 진행 요청에 따라 중간 보고 뒤 계속했다. 검증 미실행 항목은 별도로 남긴다.

## 10. 의존성 대조 결과와 구체 조립안 (2026-10-30)

### 10.1 서비스 런타임

Global만 만든 부분 런타임은 채택하지 않는다. `GlobalSystemFactory`는 JobRegister도 만들고, 그 `PostBindService`가 InGame·Buffer·LifeCycle을 필수 조회한다. CameraCatalog도 Global의 Loding을 조회한다. 이를 피하려고 가짜 Context나 서비스 미등록 상태를 만들면 기존 계약을 어긴다.

**기존 `FrameworkRuntimeFactory.Create`를 세션당 한 번 사용하고 게임 진행을 시작하는 Bootstrap 작업은 수행하지 않는 안**으로 구체화한다. Core 객체의 존재와 실제 전투 시작을 구분한다. `LifeCycleManager`는 Change 전에는 활성 구간이 없고, CombatView Initialize는 로딩 연결을 등록할 뿐 표시를 붙이지 않는다. 테스트 호스트는 GameBootstrap을 함께 놓지 않는다.

초기화 순서: `DataBaseFactory.Create` → View Query로 씬 출력 카메라·테스트 맵 준비 → 런타임 Create → BindScene → InitData → PostBindService → Initialize → Global Context 조회 → 테스트 표시 설치. **LifeCycle.Change, CombatEntry, RunStart, ArtifactBoardPreparation.Request, JobRegister.Register, 운영 UI 표시 요청은 호출하지 않는다.** 초기화용 Buffer 기본값과 테스트용 합성 사실은 별개이며 후자를 Buffer에 쓰지 않는다.

이 방식은 전체 Catalog를 요구한다. DataBaseFactory가 InGame 그룹도 검증하지만 View의 규칙 조회·Core 계산을 허용하는 뜻은 아니다. 다른 기능의 DB 오류로 호스트 초기화가 막힐 수 있다는 의존성은 남는다. 이를 줄이는 부분 런타임 재설계는 이번 범위에 넣지 않는다. 실행 전/중 `HasActiveLifeCycle == false`, `HasBattle == false`를 읽기 전용 안전조건으로 확인하고 어기면 중단한다.

생성/등록 그래프는 운영과 동일하다: 하위 Factory → 각 Group 내부 Builder → 루트 Buffer/Global/InGame/LifeCycle Export → FrameworkRuntime. 소비자는 `SystemGroupsService.Global` → Render Context/CoreGroup/ViewGroup 및 Audio/UI만 꺼내 받는다. 루트 서비스 추가·재등록은 없다. 호스트는 runtime.Tick을 먼저, 세션의 OrderedCombatPresentation.Tick을 한 번 호출한다. 사건 재생기를 별도로 다시 Tick하지 않는다. 종료는 세션 Detach·표시 해제 → runtime.Shutdown → 호스트 소유 씬 객체 정리다.

### 10.2 공유 조립의 입력·출력

새 `CombatEventPlaybackFactory`(가칭)는 기존 DisplayFactory의 **사건 Presenter + 국면 전환 Presenter + CombatSceneEventPlayback 생성**만 추출한다.

- 입력: FieldView, Configure된 FieldUnitPresenter, CombatEventPresentationValues, Audio, SoundPresets, CueSources, CombatCameraWindow, CombatEffectWindow, CombatFieldEventPorts, FieldRailwayPresenter, FieldThornPresenter, 화면 전환 서비스/설정.
- 출력: 같은 사건 Presenter와 재생 조정 객체를 보관한 작은 조립 결과. 기존 Display가 쓰는 Bind/Refresh/Clear/Release 입구는 보존한다. 새 운영 System/Service가 아니다.
- 필드 포트는 Object/Terrain/Trail/Thorn의 실제 Presenter로 연결한다. Railway·PhaseTransition도 필수 그대로 만들고, 테스트 합성 결과에는 기차·국면 사건을 넣지 않는다. null/no-op 대체 재생기로 바꾸지 않는다.
- 운영 DisplayFactory의 나머지 HUD·손패·입력·정보창 조립과 `CombatSceneRoot.ValidateReferences`는 그대로다. 공유 Factory가 기존 객체를 인수해 종료 순서를 바꾸지 않는다.

PreviewFactory는 작은 필드·유닛·환경 표시와 위 공유 조립을 만든다. 초기값은 `CharacterData` 대신 `CharacterCombatFacts` 목록과 빈 구조물/환경 목록으로 준비한다. FieldUnitPresenter.RenderFacts를 사용하고, 사건 Presenter/ShownFacts에는 **사실 목록으로 초기 표시를 동기화하는 overload**를 더한다. 기존 CharacterData 입구는 ToFacts 변환 후 같은 사실 목록 입구로 연결한다. 규칙 계산이나 운영 Character Buffer 쓰기는 추가하지 않는다.

필드 Build → 빈 기차 Install → 사건 Presenter Bind → 초기 사실 표시/보인 사실 동기화 → Ordered.Attach → Play 순서를 지킨다. 최종 합성 사실은 결과 완료 후에만 표시한다. 환경 표시·Mapper·필드·유닛 표시의 해제 순서는 현재 Display의 관련 목록을 대조해 같은 의존 순서를 유지한다.

### 10.3 유닛 HUD·맵·카메라

유닛 Configure에는 상태 외형 표·특성 표·UnitHUDLayer가 필요하다. `CombatUnitHUDFactory`로 생성하고 실제 UISystem에 `UIId.UnitHUDLayer`, Parent=None, Parallel 정책으로 동적 등록·고정 열기한다(AttachUISystem 경로). 세션 재생마다 이전 Layer를 해제/등록 해제한 뒤 새 Layer를 연결한다. UnitHUDLayer의 시험용 null UI 우회는 사용하지 않는다. 도구 조작 UI는 EditorWindow만이며 손패·포기창·정보창을 생성하지 않는다.

씬에는 이름이 유일한 출력 카메라와 Cinemachine 출력 구성을 둔다. `StageViewPropPresenter`에 선택한 View 맵 프랍을 받아 **runtime.BindScene 이전**에 생성하고 MapCameras를 `CombatCameraWindow.AttachMap`에 넘긴다. 카메라가 없는 맵이면 줌·흔들림을 검증했다고 표시하지 않는다. 첫 검증 fixture는 필요한 이름의 맵 카메라가 실제 있는 프랍을 선택해 검사한다. FieldCamera·Architecture 구현은 수정하지 않는다. 카메라가 DontDestroyOnLoad로 옮겨질 수 있으므로 부모 Transform 소멸에만 의존하지 않고 호스트가 소유 객체를 추적한다.

### 10.4 반복 중 Scope와 잔상

`CombatEffectWindow`는 첫 Play에서 Scope를 얻고 Release에서 StopAll한다. `EffectSystem.GetEffectHandle`의 Scope 목록은 Shutdown까지 보관된다. 재생마다 새 창구가 새 Scope를 얻는 방식은 누적되므로 보완이 필요하다.

Preview 세션은 Scope 한 개를 보관하고 실행마다 새 `CombatEffectWindow`에 빌려 준다. 이를 위해 View 창구에 **기존 서비스 생성자는 유지하는 internal Scope 입력 overload**를 추가한다. 창구는 자기 Release에서 StopAll하고 닫히며, Scope 목록의 최종 종료는 기존 EffectSystem.Shutdown이 맡는다. 한 세션에 실행은 하나여서 같은 Scope를 동시에 여러 창구가 사용하지 않는다. 새 Scope를 받는 시점은 이전 실행 정리 뒤 최초 필요 시 한 번이다. Global EffectSystem/EffectScope 자체는 수정하지 않는다.

Scope에는 재생 중 개수/완료 조회가 없고 카메라 Dispatch도 일반 완료 통지가 없다. 첫 버전은 §6의 잔상 확인 시간을 사용한다. 파티클 생명주기를 추측하거나 Transform 하위 ParticleSystem을 훑어 운영 완료 판정을 바꾸지 않는다. 반복 20회에서 EffectScope 발급 횟수 1회와 남은 인스턴스/대여 반환을 검증한다.

### 10.5 에디터 실행과 에셋 입력

현행 Play From Boot 위치는 `Assets/ProjectL/Editor/Boot/PlayFromBootToggle.cs`다. InitializeOnLoad의 delayCall과 sceneListChanged에서 시작 씬을 다시 지정하므로 한 번 대입하는 것만으로는 부족하다. 같은 Editor 어셈블리에 **임시 시작 씬 소유자**를 추가해 Boot Apply도 활성 임시 값을 존중하도록 한다. 원래 SceneAsset/PC별 Boot 설정을 SessionState 등에 보관하고 종료·실패·창 닫기 때 복원한다. PC별 Boot 켜짐 값 자체는 바꾸지 않는다. Unity의 scene setup 복원과 함께 Domain Reload/Scene Reload 설정 양쪽을 확인한다.

Editor 창은 UNITY_EDITOR 한정 공개 DTO/창구로 세션에 연결한다. 도구 이름은 TraitActivationPreview 접두를 쓰며 가시성 때문에 Core/표시 DTO를 모두 public으로 바꾸지 않는다. 프리셋은 Editor 전용 SO이고 Build Settings에 테스트 씬을 넣지 않는다. View 외형 목록·Kind 목록은 현행 에셋/Query에서 읽으며 특성 37개 목록을 하드코딩하지 않는다. 재생 전 DataBaseFactory.Create로 검사한 새 Query를 Preview 표시 조립에만 사용해 캐시된 줄·공격 표시를 교체한다. runtime.InitData를 같은 런타임에 다시 호출하지 않는다. 이는 첫 버전의 확실한 최신 값/검증 적용을 위한 비용이며, Render 서비스는 최초 DB로 초기화되므로 Render DB 자체 수정은 환경을 종료 후 다시 열어 적용한다.

첫 재생 요청의 보관·소비와 진입 오류는 `PreviewPlayStartScene`이 SessionState와 정적 Editor 업데이트로 소유한다. 입력 창의 표시·구독 수명에 의존하지 않는다. Play Maximized 진입 시 Unity의 `WindowLayout.MaximizePrepare`가 입력 창을 임시 파괴하므로, Play 진입 중 OnDestroy는 사용자 닫기로 처리하지 않는다. HUD Category Visual 등 표시 의존성은 Preview Factory에서 기존 Query를 연결한다. 운영 표시기·Framework 수명·Core 규칙은 바꾸지 않는다.

### 10.6 합성 사실 계약

추가 공격은 공격자/공격 그룹/방향/지정 칸, 실제 수신자 Hits와 Contacts, DeliveryIndex가 일치해야 한다. 대신 맞기는 Interception의 Guardian/Protected/ActualRecipient와 접촉의 OriginalTargetId·출처를 일치시킨다. `AttackOutcomeSnapshot`/`TraitActivationSnapshot` 생성자 검증을 그대로 사용한다. 후속 체력/사망의 공격 출처도 같은 그룹을 사용한다. 이는 특성 규칙 판정이 아니라 테스트 데이터의 참조 무결성이다. 아군 소유 역할·OwnedId, 필드 위치·점유 칸, Stats/Health도 CharacterCombatFacts 계약을 지켜 입력에서 만든다. 테스트 전용 ID는 세션 안에서만 발급한다.

### 10.7 확인 수준과 범위

위 조립안은 후속 요청에서 구현했다. Scope 입력 overload는 기존 null 인자 호출과의 모호성을 피하려고 `(서비스, 부모, Scope)` 세 인자를 쓴다. 필드·유닛 표시는 템플릿에서 실행별 새 인스턴스를 만들어 외형 풀도 교체한다. 새 공개 입구·사실 목록 overload·Scope 입력 overload·Boot 임시 시작 씬 연결은 계획 범위 안에서 추가했다. 실제 Unity에서 네 기본 프리셋 각각 20회 반복·중단 검사를 통과했으나 종료 시 Setting UI 의존성이 추가로 확인됐다. 사용자 결정으로 기존 GlobalUICanvas 연결 보완은 진행하지 않으며 설정 조합 검증은 미완료다. 최신 근거는 계획 §9.3이다. [공용 변경 메모](../handoffs/2026-10-30-trait-activation-preview-framework-share.md)를 작성했고 외부 발송하지 않았다.
