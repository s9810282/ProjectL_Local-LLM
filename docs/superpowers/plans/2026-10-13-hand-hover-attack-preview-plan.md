# 손패 호버 상승·공격 예상 체력 프리뷰 구현 계획

- 작성: View 작업, 2026-10-13
- 설계 원본: [손패 호버·공격 프리뷰 설계](../specs/2026-10-13-hand-hover-attack-preview-design.md)
- 상태: Phase H·P 코드·자동 검증 완료, 실제 Combat 재생 확인 대기. 상승 거리 16·속도 160, 대상별 예상 잔여 체력 연결.
- 각 Phase 보고에서 멈춘다. 사용자 “Phase P 진행해” 지시(2026-10-13)로 P를 구현했으며 실제 Combat 화면 확인은 남아 있다.
- 커밋·push는 사용자가 한다. 이 문서 작성은 구현·Unity 검증 완료가 아니다.

## 0. Phase 0 — 조사·설계·Task

- [x] 현재 HandPresenter의 카드 배치·집기·풀 수명을 확인.
- [x] `origin/develop-4week`의 CardHandItem/CardHandPresenter·공격 대상 HUD 프리뷰를 확인.
- [x] 현재 `AttackPreview.Targets.TargetHealthAfter`와 `UnitHUD.SetHealthPreview` 사이의 누락 연결을 확인.
- [x] 고정 판정 영역·표시용 래퍼, 기존 입력 경로 유지와 설정 제안값을 설계.
- [x] 예상 체력의 요청 번호·제출·결과 재생·풀 수명과 AP 공존을 설계.
- [x] SSOT·SOLID·JMH/FW·UI 규칙과 다른 연출 작업의 공유 파일을 점검.
- [x] 설계·H 범위 사용자 구현 지시(2026-10-13, “설계대로 Phase H 손패 카드 호버 상승부터 구현”).

조사 시 진행 중인 적 연출 코드·Combat 씬·PlayerLoadout·FMOD 및 공용 View 계획 변경은 기존 작업이다. 덮어쓰지 않는다. 이번 Phase 0은 아래 설계/계획 두 파일만 추가하며 운영 코드·씬·에셋·공용 계획은 수정하지 않는다.

## 1. Phase H — 손패 카드 호버 상승

범위는 설계 H1~H3다. 공격 프리뷰는 P Phase에서 다룬다.

### H0. 착수 전 확인

- [x] 현재 작업 트리·파일 소유/진행 상태를 다시 확인하고 HandPresenter 동시 편집이 없는 시점에 착수한다.
- [x] 현재 카드 크기·배율·Canvas 설정과 제안값 상승 16·속도 160을 확인한다. 설계와 다른 구조/범위가 필요하면 구현 전에 사용자에게 확인한다.

### H1. 고정 판정 래퍼와 카드 생성

- [x] `Display/HandCardHoverView.cs`와 `.meta`를 추가한다. PointerEnter/Exit는 표시용 플래그만 변경한다.
- [x] 고정 Rect·투명 Raycast Image 안에 기존 UnitCardView 인스턴스를 둔다. 자식 Graphic Raycast는 손패 인스턴스에서만 한 번 끈다.
- [x] 호버 플래그·외형 오프셋·내용 참조의 명시적 초기화/복원 메서드를 만든다. 새 ID 바인딩·disable·풀 반환에 같은 정리 경로를 사용한다.
- [x] HandPresenter의 풀·EntityViewSet 단위를 래퍼로 전환한다. 내용과 이름/능력치 표시는 UnitCardView에 위임한다.

### H2. 배치·집기·상승 연결

- [x] ApplyLayout과 TryPickUnit을 같은 래퍼 Rect 기준으로 연결한다. ReserveSlotIndex·역순 집기·기존 외부 Picker 계약을 유지한다.
- [x] 카드 크기/배율에 맞는 래퍼 크기를 생성 시 적용하고 재배치가 상승 오프셋을 덮지 않게 한다. 호버 sibling 재정렬은 추가하지 않는다.
- [x] HandPresenter에 `hoverLift`·`hoverMoveSpeed` 설정과 유효값 검증을 추가한다. Update에서 unscaledDeltaTime으로 표시만 보간한다.
- [x] Exit는 부드러운 복귀, 수명 정리는 즉시 복귀한다. 0 거리·0 속도 동작을 설계대로 처리한다.
- [x] 기존 SetHiddenEntity/ClearHiddenEntity에 전체 호버 초기화·드래그 중 억제를 연결한다. 숨김·Release·포커스 상실에서 래퍼 판정도 꺼지거나 상태가 초기화되는지 확인한다.
- [x] Combat 씬의 HandPresenter에 두 값만 연결한다. 다른 작업의 씬 변경과 기존 cardScale/cardGap/bottomMargin을 보존한다.

### H3. 검증·리뷰

- [x] Unity EventSystem의 PointerEnter/Exit dispatch로 고정 판정·상승·복귀를 검증한다. Button.onClick으로 대체하지 않는다.
- [x] 겹친 카드의 실제 GraphicRaycaster 대상과 TryPickUnit 집기 대상이 일치하는지 검증한다. 위로 이동한 자식 외형이 새 Raycast 영역을 만들지 않는지도 확인한다.
- [x] 상승 도중 Render/카드 수 변경, 드래그 원본 숨김, 필드 유닛 드래그, disable·풀 반환·새 ID 재사용 후 오프셋 누수가 없는지 검증한다.
- [x] 관련 Systems·Framework 테스트 컴파일과 기존 손패/배치 입력 회귀를 실행한다. 새 파일이 생성 csproj에 없으면 기존 CompileOverrides 방식으로 확인하고 csproj를 직접 수정하지 않는다.
- [ ] Unity 재생: 카드 가운데/가장자리·겹침, 빠른 Enter/Exit, 배율·해상도, 손패↔필드 드래그/취소, 배치→전투→배치, 씬 재진입을 확인한다.
- [ ] 덱 카드·정보 카드 핀 선택·Synergy/Function 패널의 기존 동작을 확인한다.
- [x] 변경 파일·설정 위치·테스트 근거·Unity 미확인 부분을 실행 기록에 남기고 보고한다.

**H 완료 기준:** 카드 외형만 상승하고 판정·집기·Drop·풀 수명이 유지된다. Unity 화면 확인 전에는 코드 검증 완료와 화면 확인 대기를 분리해 기록한다. H 보고 후 멈추며 P를 자동 착수하지 않는다.

## 2. Phase P — 공격 예상 체력 프리뷰

H 리뷰와 사용자 진행 지시 뒤 착수한다. 범위는 설계 P1~P4와 기존 HUD/fallback 표시 연결이다.

### P0. 착수 전 공유 파일 확인

- [x] 적 연출·이동 피드백·임시 공격 연출의 해당 Phase 보고와 현재 diff를 확인한다. CombatSceneDisplay·FieldUnitPresenter/View·CombatViewSystem 등을 동시에 편집하지 않는다.
- [x] 현재 Preview 흐름과 결과 시작/후처리 경계가 설계 조사 기준과 같은지 재확인한다. 차이가 있으면 구현 전에 설계·범위 변경을 확인한다.

### P1. 대상별 표시·HP/AP 정리 분리

- [x] ICombatInteractionFeedback에 ShowAttackHealthPreview/ClearAttackHealthPreview를 추가하고 기존 모든 가짜 구현을 갱신한다.
- [x] FieldUnitPresenter에서 응답 Targets의 기존 View만 찾아 적용하고, 이전 프리뷰 대상 ID를 한 곳에서 추적해 교체/정리한다.
- [x] FieldUnitView에서 HUD의 SetHealthPreview와 기존 3D 체력 숫자 fallback을 같은 예상 체력으로 연결한다. 실제 표시 캐시는 유지하고 정리 때 복원한다.
- [x] 기존 비용 정리를 AP 전용으로 바꿔 체력 프리뷰와 공존하게 한다. 체력/비용을 모두 지우는 UnitHUD.ClearPreview는 전체 수명 정리에 계속 사용할 수 있다.
- [x] 새 실제 체력 적용·새 ID Bind·풀 반환·Release가 예상 값을 비우고, 누락/사라진 ID를 새로 대여하지 않는지 확인한다.

### P2. 호버 응답·제출·결과 재생 연결

- [x] ApplyAttackAnswer에서 현재 Live·요청 번호·공격자·호버·기존 CanPreview·CanAttack 조건과 IsPresenting을 확인해 예상 체력을 표시한다.
- [x] 새 호버 요청 전에 이전 체력을 복원한다. 필드 밖·UI·AP 부족·차단·실패·응답 표시 불가에서도 정리한다.
- [x] Attack 제출이 양수 번호를 반환하면 즉시 체력 프리뷰를 비우고 소유한 상세 요청을 무효화한다. 실제 제출 명령과 범위/모드 유지 정책은 보존한다.
- [x] CombatInteraction의 좁은 무효화 메서드와 ConsumeResult의 재생 시작 전 호출을 연결한다. 한 결과의 첫 Begin 전에 한 번만 정리한다.
- [x] 결과 후처리는 기존 AfterResult→범위→상세 요청 경로로 재개한다. 재생 중 받은 예상 응답을 캐시해 재생 뒤 적용하지 않는다.
- [x] 취소·이동 모드 전환·턴 종료·포커스 상실·행위자 변경·Clear/Release의 기존 정리 경로에 체력 정리를 연결한다.
- [x] 영향 칸·차단 벽·공격 AP·이동 AP와 기존 음향/적 연출/결과 확인 순서를 보존한다.

### P3. 계약·수명 회귀 검증

단순 메서드 호출 확인을 늘리기보다 아래 실패 가능성이 있는 경계에 테스트를 집중한다. 기존 CombatInteractionTests·WallAttackInteractionTests·UnitHUDTests를 우선 확장하고 대상 연결/풀 통합 검증만 필요한 만큼 추가한다.

| 사례 | 검증할 결과 |
|---|---|
| A 호버→B 호버, A의 늦은 응답 | A 복원, 현재 B 요청만 적용 |
| 성공→실패/AP 부족/벽 차단/필드 밖 | 이전 대상 프리뷰·표시 대상 추적 제거 |
| 단일·방향·광역·관통·구조물 | Targets에 있는 ID 모두 적용, 없는 ID 추가 없음 |
| 정규화 손실·피해 0·예상 체력 0 | TargetHealthAfter 그대로 표시, 실제 체력·사망 상태 불변 |
| Attack 제출 성공→이전 응답 도착 | 성공 시 복원, 이전 번호 재적용 없음, 명령은 유지 |
| 새 결과 시작·연출 중 응답·후처리 | 프리뷰가 실제 피격 체력을 덮지 않음, 새 요청으로만 복귀 |
| 공격자도 대상인 응답 | AP 설정/정리가 예상 체력을 지우지 않음, HP 정리가 AP를 지우지 않음 |
| HUD 없는 대상·HUD 숨긴 구조물 | 기존 숫자 fallback에서 적용/복원, HUD 활성 조건 불변 |
| 사라진 ID·새 ID/풀 재사용·씬 이탈 | 누락 ID 무시, 새로운 대여 없음, 프리뷰 누수 없음 |
| RecordInputOnly | 입력 기록 유지, 피해 프리뷰 생성/표시 없음 |

- [x] 관련 Systems·테스트 어셈블리를 컴파일하고 영향 받은 EditMode 회귀를 실행한다.
- [x] 관리 테스트만 실행했다면 Unity Test Runner와 구분해 기록한다. 순수 테스트 결과를 실제 HUD/포인터 확인의 대체 증거로 쓰지 않는다.

### P4. Unity 화면 확인·리뷰

- [ ] 단일 타격에서 예상 숫자/체력바, 포인터 이동·해제·취소 때 실제 체력 복원을 확인한다.
- [ ] ShotGun 등 방향/광역과 관통 공격에서 다중 대상·벽 차단·구조물 표시를 확인한다.
- [ ] 사망 예상 0에서 실제 View가 유지되고, 확정 후 기존 피격/사망 사건 순서로 바뀌는지 확인한다.
- [ ] 연출 중 빠른 호버/모드 전환, 턴 종료·포기·포커스 상실·씬 재진입에서 잔상과 요청 누수가 없는지 확인한다.
- [ ] 기존 공격자 AP 반투명 표시·이동 AP·손패 상승·기존 배치/공격/이동 조작을 함께 확인한다.
- [x] 코드 검증·Unity 확인 상태·남은 항목을 기록하고 보고한다.

**P 완료 기준:** 최신 유효 Core 응답의 대상별 예상 잔여 체력이 표시되고 모든 정리 경계에서 실제 표시로 복원된다. 결과 연출·HP/AP·풀·기록 모드 회귀가 없다. P 보고 후 멈춘다.

## 3. 구현 중 변경 규칙

- 설계 밖의 정보 카드·피해량 텍스트·반투명 감소 구간·피격 연출·입력 정책 변경은 추가하지 않는다.
- Core/Buffer의 새 계약이 필요해졌다면 구현하지 않고 사용자에게 설명하고 기존 Core 전달 문서에 요청으로 남긴다.
- 기존 작업 내용을 보존하고 각 Phase에 필요한 줄만 편집한다. 새 .cs에는 .meta를 함께 만든다.
- 본 계획의 Task/실행 상태가 원본이다. 공유 View 계획에 링크를 추가할 때는 상세 Task/완료 상태를 중복 기록하지 않는다.
- 파일 삭제, 커밋·push, 외부 전달은 이번 구현 범위에 포함하지 않는다.

## 4. 실행 기록

### 4.1 Phase 0 (2026-10-13)

사용자의 두 기능 확인 요청을 조사한 뒤 “이대로 설계 및 구현계획” 요청에 따라 설계와 Phase H/P Task를 작성했다. 과거 구현의 표시 원리는 재사용하되 현재 Core 응답·입력 계약·카드 외형 원본을 유지하도록 정리했다. 기본 수치는 과거 값과 현재 카드 배율에서 제안했으며 구현된 값이나 사용자 개별 확정값으로 기록하지 않았다.

이번 작업은 문서 두 파일 추가만 수행했다. 운영 코드·씬·에셋 수정, 테스트 실행, 커밋·push는 하지 않았다. 다음 작업은 사용자의 구현 지시 뒤 Phase H다.

### 4.2 Phase H (2026-10-13, 코드·자동 검증 완료, 실제 재생 확인 대기)

사용자가 설계대로 Phase H 구현을 지시했다. HandCardHoverView와 .meta를 추가하고 HandPresenter의 풀·배치·집기를 고정 래퍼 기준으로 연결했다. 카드 내용은 기존 UnitCardView가 표시한다. PointerEnter/Exit와 unscaledDeltaTime 보간으로 외형만 올리고, 드래그·포커스 상실·비활성화·숨김·새 ID·풀 반환에서 호버를 초기화했다. 부모 UI 단위 16 상승, 속도 160을 Combat 씬의 HandPresenter에 연결했다. 손패 입력 Router·Picker 계약·공용 카드 프리팹·Core/Buffer는 수정하지 않았다.

이번 코드/씬 변경은 HandPresenter, 신규 HandCardHoverView(.meta 포함), 신규 HandCardHoverTests(.meta 포함), Combat 씬의 두 호버 필드다. 씬의 이동 시간 0.2와 적 간격/회전 설정 등 다른 작업의 변경을 보존했다. 설계 상태와 이 계획의 실행 상태를 갱신했으며 공유 View 계획은 동시 작업을 피하려고 편집하지 않았다.

검증 근거:

- VS MSBuild: Systems·ProjectL.Tests.Framework를 포함한 관련 참조 프로젝트 컴파일 오류 0. 기존 CS0649 경고 13건, 호버 변경 파일의 신규 경고 없음. `Temp/HandHoverH/NewFiles.targets`로 새 파일만 Compile에 보완했다. 원본 로그: `Temp/HandHoverH/msbuild-final.log`.
- Unity 6000.0.73f1 **정식 Test Runner EditMode 69/69 통과**, 실패·건너뜀 0. HandCardHoverTests 11, 기존 CombatInteractionTests 39, CombatInputRouterTests 19다. 원본 결과: `Temp/HandHoverH/unity-editmode-results.xml`, 요약: `Temp/HandHoverH/unity-editmode-summary.txt`, Editor 로그: `Temp/HandHoverH/test-runner-regression-3.log`.
- 검증은 독립 `Temp/HandHoverH/Probe` 프로젝트에 작업 DLL·테스트 소스·로컬 Test Framework를 연결해 실행했다. 원본 프로젝트의 열려 있는 씬을 전환하거나 저장하지 않았다. DLL Reflection 실행으로 정식 Test Runner를 대체한 결과가 아니다.
- 호버 테스트는 실제 UI Camera/GraphicRaycaster, EventSystem PointerEnter/Exit dispatch를 사용했다. EditMode에서는 입력 모듈을 돌리지 않고 GraphicRaycaster에 같은 포인터를 직접 전달했다. 일반 MonoBehaviour의 disable/focus 콜백은 테스트에서 명시적으로 호출했다. 실제 Play의 포인터 자동 진입·이탈/수명 콜백은 아래 재생 확인 대상이다.
- 카드 400×550·배율 0.35에서 판정 140×192.5, 배율 0.7에서도 상승 거리 16 유지, 겹침/역순 집기, 재배치 중 상승 유지, Exit 복귀, 손패/필드 드래그, 숨김·포커스·풀 재사용, 0 설정·잘못된 설정을 검증했다.
- 변경 C#의 UTF-8/LF·U+FFFD 없음, 새 메타 GUID 고유성 및 변경 코드 whitespace를 확인했다.

첫 전체 컴파일은 동시에 진행 중이던 적 회전 변경의 생성자/메서드 연결이 완성되기 전에 실행돼 해당 파일에서 실패했다. 그 파일을 수정하지 않고 변경이 연결된 뒤 다시 실행해 통과했다. 테스트용 UI 판정과 EditMode 수명 호출을 보완한 뒤 위 최종 69건을 통과했다.

남은 확인은 실제 Combat 씬에서 빠른 호버·겹침·드래그/취소·해상도·배치/전투 전환·씬 재진입과 덱 정보 카드의 기존 동작이다. 자동 검증을 실제 화면 확인 완료로 기록하지 않는다. Phase H 보고 지점에서 멈추며 Phase P는 착수하지 않았다. 커밋·push는 하지 않았다.

### 4.3 Phase P (2026-10-13, 코드·자동 검증 완료, 실제 재생 확인 대기)

사용자 “Phase P 진행해” 지시에 따라 설계 P1~P4를 구현했다. 착수 HEAD는 `4afddbf5`이며 작업 트리는 깨끗했고 적 연출 M2 완료·M3 사용자 확인 대기 기록을 확인했다. 기존 회전 override와 이동/행동 간격 연출 위에 필요한 표시·정리만 연결했다.

구현:

- Feedback의 ShowAttackHealthPreview/ClearAttackHealthPreview를 추가했다. SceneDisplay는 FieldUnitPresenter에 위임하며 Clear/Release에서 직접 정리한다. 해제된 Display의 늦은 표시 호출은 무시한다.
- Presenter는 Targets 전체에서 기존 ID만 찾아 TargetHealthAfter를 적용하고 이전 대상 집합을 교체/복원한다. 누락 ID로 View를 빌리지 않는다. View는 활성 HUD의 기존 숫자·바와 HUD 없음/숨김의 3D 숫자 fallback을 연결했다. 실제 HP 캐시를 별도로 유지하며 Apply·새 Bind·풀 반환·disable에서 예상 값을 비운다.
- 비용 정리는 SetActionCostPreview(0), 체력 정리는 SetHealthPreview(null)로 분리했다. 공격자도 피해 대상인 경우 HP/AP가 공존하며 HP 0 예상 때문에 실제 View를 제거하지 않는다. 실제 RenderFacts·풀 반환·Release에서 대상 추적을 비운다.
- Attack은 현재 요청/응답 번호·행위자·호버·Live·CanPreview·CanAttack·!IsPresenting 조건을 확인한다. 새 호버·실패·표시 불가·양수 제출·결과 재생 시작·모드/씬 수명 정리에서 복원한다. 양수 제출은 상세 번호만 무효화하고 명령·범위·모드는 유지한다. 제출 0이면 성공으로 취급하지 않는다.
- ConsumeResult의 첫 Begin 직전에 Interaction.BeforeResult를 호출한다. 연출 중 받은 상세 응답은 기존 영향 칸/AP를 반영하되 HP는 표시/보관하지 않고 상세 번호를 무효화한다. 후처리의 기존 범위→상세 재요청으로만 다시 표시한다.
- 리뷰에서 일반 연출 중 같은 칸 PointerMoved가 기존 상세 요청을 삭제하는 문제를 찾아 HP만 복원하도록 좁혔다. 제출 0의 표시 불가 조건도 HP만 복원한다. 기존 요청 허용·영향 칸/AP 정책을 유지하며 이 경계를 회귀 테스트에 포함했다.

검증 근거:

- VS MSBuild 관련 Systems·ProjectL.Tests.Framework와 참조 프로젝트 컴파일 성공, 오류 0·변경 파일 신규 경고 0. 첫 전체 빌드의 기존 경고는 CS0649 13건과 Core의 CS8632 1건이다. 원본 로그 `Temp/AttackHealthP/msbuild-1.log`, 최종 테스트 보완 후 빌드 `Temp/AttackHealthP/msbuild-final.log`, 새 테스트 Compile 보완 `Temp/AttackHealthP/NewFiles.targets`. 생성 csproj를 직접 수정하지 않았다.
- Unity 6000.0.73f1 **정식 Test Runner EditMode 289/289 통과**, 실패·건너뜀 0. 신규 대상 표시 9, Interaction 58(신규 19 포함), ViewSystem 66(신규 2 포함), 기존 입력 Router 19, HUD 15, 벽 공격 12, 손패 호버 11, 필드 방향 1, 적 간격/이동 연출 56, 이동 View 22, 표현 정리 6, 결과 재생 14를 실행했다. 원본 XML `Temp/AttackHealthP/unity-editmode-results.xml`, 요약 `Temp/AttackHealthP/unity-editmode-summary.txt`, 로그 `Temp/AttackHealthP/unity-editmode-final.log`.
- H와 같은 독립 `Temp/HandHoverH/Probe`에 최신 구현 DLL·테스트 소스 177개·로컬 Test Framework를 연결했다. 실행기는 `Temp/AttackHealthP/AttackHealthNativeChecks.cs`다. 정식 Test Runner 결과이며 DLL reflection 실행으로 대체하지 않았다. 원본 프로젝트 씬을 열거나 저장하지 않았다. 작성된 Combat 씬을 여는 기존 벽 표시 테스트 1개는 독립 환경의 실행 필터에서 제외했고 해당 씬 확인을 완료로 기록하지 않는다.
- 신규 실패 경계: A→B 교체와 늦은 A, 실패/없는 Attack/잘못된 공격자, 자동/후처리/연출/행위자 변경, 필드 밖·유효 조준 없음·AP 부족, 양수/0 제출, 타 소유자 요청 보존, 새 결과 첫 Begin·지연 Tick·확정 HP Refresh·새 범위/상세 응답, 기록 모드, 취소/이동 모드/턴 종료/Stop 정리를 확인했다.
- 실제 GameObject/UnitHUD/Slider/TMP/Presenter 풀 테스트로 다중 대상·구조물·정규화 손실·피해 0·예상 체력 0·누락 ID 무대여·숨겨진 HUD fallback·HP/AP 독립 정리·동일 실제 HP 갱신·새 ID/풀 재사용·M2 방향 유지를 확인했다. 예상 값으로 실제 Core/Buffer 사실을 바꾸지 않았다.
- 첫 Unity 실행은 288/289 통과였다. 기존 UnitHUDTests의 disable 사례가 일반 MonoBehaviour OnDisable의 EditMode 자동 호출을 가정해 실패했다. 실제 운영 OnDisable 코드는 유지하고 테스트에서 정리 콜백을 명시적으로 호출하도록 보완한 뒤 위 최종 289건을 통과했다. 첫 결과와 로그는 `Temp/AttackHealthP/unity-editmode-1-attack-health-results.xml`, `unity-editmode-1.log`에 보존했다. 실제 Play의 자동 수명 콜백은 재생 확인 대상으로 남긴다.
- 변경 C# UTF-8/LF·U+FFFD 없음, 신규 테스트 .meta GUID 고유성, git diff --check 및 View/Core 소유 경계·SSOT·SOLID·JMH/FW 계약을 점검했다.

운영 변경은 AttackTargetingInteraction, ICombatInteractionFeedback, CombatInteraction, CombatViewSystem, CombatSceneDisplay, FieldUnitPresenter/View다. 신규 AttackHealthPreviewDisplayTests와 .meta, 기존 CombatInteractionTests·CombatViewSystemTests·WallAttackInteractionTests·UnitHUDTests를 보완했다. 설계/계획 두 문서를 갱신했고 공용 View 계획·Core/Buffer·입력 ActionMap·Flow/Job/Plan·FieldCamera·Architecture·씬/프리팹/DB·Render 내부는 수정하지 않았다.

남은 확인은 P4의 실제 Combat 화면 재생이다. 단일/ShotGun 방향·광역/관통·벽/구조물의 예상 숫자/바와 복원, 확정 후 피격/사망 사건, 연출 중 빠른 호버·포커스 상실·턴 종료·포기·씬 재진입, AP/이동/손패의 함께 쓰기를 사용자가 확인해야 한다. 자동 테스트 통과를 원본 Combat 화면 확인으로 기록하지 않는다. Phase P 보고 지점에서 멈추며 커밋·push는 하지 않았다.
