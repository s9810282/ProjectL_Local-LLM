# 이동 범위와 경로 편집 고스트 Task 계획

- 작성: View 작업, 2026-10-13
- 설계 원본: [이동 표시 설계](../specs/2026-10-13-movement-planning-feedback-design.md)
- 상태: Phase 1 범위 연결 코드·자동 검증 완료(§4.3), 원본 GameView 확인 대기. Phase 2 고스트/반투명은 미착수. Phase 1 리뷰 지점에서 멈춘다.
- Task·진행 상태·실행 증거는 이 문서에만 기록한다. View 전체 계획에는 진입 링크만 둔다.
- 각 Phase 완료 뒤 변경·검증·잔여 사항을 보고하고 멈춘다. 커밋·push는 사용자가 한다.

## 0. Phase 0 — 검토 방향의 설계·계획화

- [x] Q→PathInput→Interaction→Feedback/SceneDisplay의 현재 책임을 확인.
- [x] MoveRange 요청·Core 응답·Range 강조 층이 이미 있음을 확인.
- [x] 최신 미리보기 슬롯 하나의 범위/경로/복귀 충돌을 확인하고 요청 순서를 설계.
- [x] 같은 칸 내부 커서 추적과 기존 ShowDraft 호출 생략을 분리하도록 계획.
- [x] 기존 Ghost와 원본 숨김이 배치 의도임을 확인하고 이동 표시 수명 분리.
- [x] 반투명 자원 준비·복원·풀 재사용 및 셰이더 착수 점검을 계획.
- [x] SSOT·SOLID·JMH/FW·Core 경계·동시 편집 주의사항을 설계에 기록.
- [x] 사용자 병행 독립 준비 지시(§0A 범위만).
- [x] 다른 세션 완료·대기 및 깨끗한 작업 트리를 확인하고 사용자 Phase 1 착수 지시를 받음.

문서 작성 단계에서는 코드·씬·DB 에셋·재질을 변경하지 않는다. 기존 FMOD 캐시, 로드아웃, Combat 씬, Core 전달 문서, 적 연출 작업은 보존한다.

## 0A. 병행 독립 준비 — 완료, Phase 1에서 공통 연결 인수

사용자 지시: “지금 병행 가능한 독립 로직이랑 전용 테스트 준비까지만 진행해줘. 공통 파일 연결은 다른 세션 작업 끝난 뒤에 하자.” 이 지시는 Phase 1 전체 착수나 Phase 2 구현 지시가 아니다.

- [x] 신규 `CombatMovementRangeRequest.cs`와 `.meta`: 범위 요청의 번호/지정 세대/행위자/전투 ID/시작 위치/결과 순번 대조, 슬롯 소유권 판정, 응답 1회 소비, 멱등 Cancel.
- [x] 같은 파일의 불변 `CombatMovementRangeFacts`: 요청 당시 공개 사실의 값만 보관. Core/Buffer의 실제 상태를 수정하거나 이동 규칙을 계산하지 않음.
- [x] 신규 `CombatMovementRangeRequestTests.cs`와 `.meta`: 정상·실패·빈 범위, 사실 변경, 슬롯 교체, 다른 응답/초기화되지 않은 결과, 반복 소비, 취소/새 지정/번호 재사용, 잘못된 Begin의 기존 수명 보존, 초기 결과 순번 0을 검증.
- [x] 기존에 완료된 C1의 Systems/Contracts DLL 사본과 NUnit을 독립 Temp에 복사해 새 소스 두 개만 컴파일/실행. 별도 출력 폴더를 사용하고 기존 프로젝트의 csproj/asmdef/Library/공통 출력은 수정하지 않음.
- [x] 독립 .NET 8 빌드 오류 0·경고 0, 전용 테스트 **30 통과·0 실패**. Unity Test Runner/실제 게임 동작 검증과 구분.
- [x] 후속 Phase 1에서 `CombatPathInput`의 범위 게시/요청 소유와 Request 객체 연결 완료(§4.3).
- [x] 후속 Phase 1에서 Interaction 견적 유예/재개와 빠른 Q→범위→정확한 경로/동결 제출 자동 검증 완료(§4.3).
- [x] 후속 Phase 1에서 Feedback/PlanningPresenter/SceneDisplay/FakeDisplay의 범위 강조 연결 완료(§4.3). 고스트/반투명은 Phase 2 대기.

**연결 시 계약:** 실제 `SetPreview`가 발급한 범위 요청으로 Begin하고 돌려받은 지정 세대를 보관한다. 최신 공개 사실과 슬롯을 다시 읽어 TryConsume하며, 거절 반환만으로 자동 유예 해제나 Draft 취소를 하지 않는다. 사실/슬롯 소유권 상실은 기존 PathInput 수명과 Policy로 정리한다. 성공 소비는 로컬 Pending만 끝내므로 Buffer 슬롯을 비우는 책임은 연결하는 호출자에 있다. 현재 슬롯 소유 여부를 재확인한 뒤 정리하고, 표시/콜백을 거쳐 재진입할 수 있으므로 새 지정 수명을 재확인한 후 최신 경로 견적을 재개한다. 실패/빈 범위도 정식 응답이면 대기를 끝내지만 Move 실행을 승인하지 않는다.

**독립 준비 당시의 보류:** 기존 공통 파일과 전체 빌드·Unity 검증은 다른 세션 종료 뒤로 미뤘다. 후속 사용자 Phase 1 지시와 다른 세션 완료 확인으로 범위 연결에 필요한 공통 파일만 인수했다(§4.3). UnitPresenter/UnitView와 고스트/반투명은 계속 Phase 2 범위다.

## 1. Phase 1 — 이동 가능 범위 연결

**목표:** Q 진입부터 Core 목적지 범위를 표시하고, 경로 견적과 단일 미리보기 슬롯을 안전하게 공유한다. 범위 응답 대기 중에도 기존 초안 편집을 유지한다.

**대상 파일:** `CombatPathInput.cs`, `CombatMovementInteraction.cs`, `ICombatMovementFeedback.cs`, 신규 `CombatMovementPlanningPresenter.cs` 및 `.meta`, `CombatSceneDisplay.cs`, 기존 테스트 Display/테스트 파일. FieldView/강조 상태는 기존 API를 사용한다. Core·Buffer·씬·View DB·고스트·유닛 재질은 이 Phase에서 변경하지 않는다.

- [x] **P1.1 착수 확인:** 최신 코드/작업 트리와 적 연출 진행 파일을 다시 확인한다. SceneDisplay 동시 편집을 피하고 필요한 조립/위임 줄만 수정한다. 기존 `MoveRange` 처리/응답과 `CanEditPath`를 인수하며 Core 직접 호출은 추가하지 않는다.
- [x] **P1.2 범위 요청 수명:** Q 수락 시 이전 소유 요청을 정리하고 새 지정 수명·범위 번호·행위자·전투/시작 위치/결과 상태를 기록한다. 현재 슬롯과 일치하는 응답만 수락하고 중복·늦은 응답을 무시한다.
- [x] **P1.3 견적 유예:** 범위 대기 중 Interaction은 기존 Draft로 편집/되돌리기/동결을 계속하고 경로 견적 게시만 미룬다. 최신 초안 하나를 범위 소비 후 게시한다. 경로 0걸음이면 게시하지 않고, 확정 중이면 정확한 동결 경로의 새 견적/context를 기다린다. 유예를 입력 허용 bool로 처리하지 않는다.
- [x] **P1.4 범위 표시:** Feedback의 좁은 범위 의도와 PlanningPresenter를 추가하고 응답 `TargetAnchor`를 기존 Range 층에 표시한다. 실패/목적지 0개는 빈 범위. 기존 Range/MovementDraft/Hover 우선순위와 두 필드 표시 모드를 재사용한다.
- [x] **P1.5 정리/복귀 공존:** 취소·다시 Q·E·Return·턴 종료·행위자/결과 상태 변경·FocusLost·Detach에서 범위 캐시/유예/자기 요청을 정리한다. Return 요청과 서로 덮어쓰지 않고, 비활성 이동 정리가 새 공격 범위를 지우지 않게 한다. 한 표시 정리 실패가 다른 정리를 막지 않게 한다.
- [x] **P1.6 테스트 갱신:** Q 직후 `HasPreview == false`를 기대하던 테스트는 범위 요청을 먼저 응답시키도록 Rig를 수정한다. 명령 0·시작 칸/상하좌우·경계 떨림·정확한 경로·클릭 확정의 원래 검증은 유지한다. 테스트를 단순히 느슨하게 바꾸지 않는다.
- [x] **P1.7 검증:** 아래 필수 자동 사례와 관련 EditMode 회귀를 실행한다. MSBuild/관리 직접 실행/Unity Test Runner 증거를 구분한다. 새 소스가 csproj에 없으면 기존 새 Compile 추가 절차를 사용하고 생성 csproj를 임의 수정하지 않는다.
- [ ] **P1.8 Unity 확인:** CellPrefab/GridOutline 각각에서 Q→범위→경로 강조 겹침, 빠른 Q→이웃 시작/확정, 우클릭/E/턴 종료 후 해제, 범위 없는 유닛을 확인한다. 범위 응답이 지연될 때도 초안이 사라지지 않아야 한다.

**필수 자동 사례**

| 사례 | 기대 증거 |
|---|---|
| Q 1회·반복 Tick | MoveRange 한 번, Command 0, 반복 범위 게시 0 |
| Q 직후 빠른 경로 입력 | Range 슬롯 유지, Draft 최신 경로 편집 유지, 범위 소비 뒤 최신 Move 요청 1개 |
| 범위 대기 중 클릭 | 경로 동결, Command 0; 일치하는 범위→Move 응답 뒤 동일 경로 1회 제출 |
| 범위 성공/실패/빈 결과 | Core TargetAnchor만 표시, 대체 규칙 계산 없음, 실패 뒤 견적 유예 해제 |
| 취소·재Q·다른 행위자·전투·결과 상태 | 옛 응답 미표시/미제출, 새 수명 요청 보존 |
| 같은 범위 응답 반복 | 범위 적용/견적 재개 중복 0, 정확한 견적 번호/context 유지 |
| E/Return/EndTurn/FocusLost/Detach | 범위와 자기 요청 정리, 새 공격/복귀 요청 또는 결과 확인 우회 없음 |
| 범위 표시/해제 콜백 실패 | 입력 수명 정리·Draft/Return 등 다른 정리 계속, 해제 후 재표시 없음 |

기존 `CombatMovementFeedbackTests`, `CombatMovementViewTests`, `CombatPathInputCleanupTests`, `MovementDraftTests`, 입력 Router 회귀 및 필드 강조/표시 테스트를 우선 사용한다. 요청 순서/응답 수명 사례가 많으면 동일 테스트 어셈블리 안에 `CombatMovementRangeFeedbackTests`와 `.meta`를 추가한다. 새 asmdef는 만들지 않는다.

**완료/리뷰 보고:** 변경 파일, 범위와 경로 요청 순서, 실패/늦은 응답 처리, 자동/Unity 결과, 미확인 항목. Phase 1 끝에서 멈춘다. Unity 기능 확인이 없으면 코드 완료와 사용자 확인 대기를 구분한다.

## 2. Phase 2 — 커서 고스트와 원본 모델 반투명

Phase 1 리뷰 뒤 사용자 진행 지시로 착수한다. 적 회전 작업이 같은 UnitPresenter/View에 진행 중이면 먼저 최신 완료 상태를 인수한다.

**목표:** 실제 경로 편집 시작부터 원본 모델을 약간 흐리게 하고, 커서를 따라가는 고스트와 현재 견적색을 표시한다. 모든 종료/풀 재사용에서 원래 상태를 복원한다.

**대상 파일:** `CombatPathInput.cs`, `ICombatMovementFeedback.cs`, `CombatMovementPlanningPresenter.cs`, `CombatSceneDisplay.cs`, `Display/PlacementGhost.cs`, `Display/FieldUnitPresenter.cs`, `Display/FieldUnitView.cs`, `StageFieldViewDataSO.cs`, `StageFieldView_Default.asset`, 필요한 View 소유 투명 머티리얼/자원 헬퍼 및 `.meta`, 기존/신규 EditMode 테스트. SceneRoot/Factory 등록/입력 액션/Core/Buffer 변경은 필요 없다.

- [x] **P2.1 재질 착수 점검:** 현재 운영 플레이어 프리팹의 모델 Renderer·모든 재질 슬롯·투명 모드·색 프로퍼티를 추적한다. GhostMaterial 공란과 배치 재질 영향을 확인한다. 지원 기존 셰이더의 투명 모드/변형만 사용하며, 공용 셰이더/Render 변경이 필요하면 근거를 보고하고 해당 변경은 구현 전에 확인한다.
- [x] **P2.2 표현 설정:** StageFieldViewData에 `movementOriginalOpacity=0.65`, `neutralGhostTint=(0.8, 0.8, 0.8, 0.5)`와 유한값/범위 검증을 추가하고 기본 에셋에 기록한다. View 소유 투명 고스트 재질을 기존 GhostMaterial에 연결한다. 기존 유효/무효색·스케일·높이·Range 색은 재사용한다.
- [x] **P2.3 Feedback/표시 수명:** 실제 Interaction 시작 시 행위자 ID·외형 키·방향을 전달하고 PlanningPresenter가 원본 흐림과 고스트를 시작한다. Q의 시작 칸 대기에서는 아직 원본을 흐리지 않는다. 범위 표시 수명과 실제 고스트 편집 수명을 구분한다.
- [x] **P2.4 Ghost 재사용:** 기존 풀/프리팹/GUID를 유지하며 이동의 연속 위치·방향·중립/유효/무효색·일시 숨김을 지원한다. 배치용 ShowGhost/HideOriginal/SetPlacementActive는 호출하지 않는다. HUD·Collider·그림자 계약을 유지하고 재대여 시 위치/방향/색/표시를 초기화한다.
- [x] **P2.5 연속 포인터 표시:** 경로/후보/응답이 같아 ShowDraft를 생략해도 커서 월드 위치는 갱신한다. 필드 밖/UI 위/매핑 실패는 숨기고 기존 재진입 규칙에 맞게 다시 표시한다. 확정 후 견적 대기에는 위치를 동결하고 현재 revision과 맞는 응답만 색에 반영한다.
- [ ] **P2.6 원본 흐림/복원:** 모델 Renderer를 인스턴스 준비 때 분리해 HUD·체력 숫자를 제외한다. FieldUnitPresenter가 대상 ID를 관리하고 FieldUnitView가 원래 재질/색과 투명 변형/ColorHandle을 소유한다. 공유 원본 에셋은 바꾸지 않는다. RenderFacts/회전 override와 공존하고 기존 View를 유지한다.
- [x] **P2.7 자원 수명:** 취소·제출·E·Return·턴 종료·입력 차단·FocusLost·Detach·사망/필드 이탈·대상 교체·새 ID·풀 반환·최종 파괴/Release에서 명시적 복원/고스트 반환을 검증한다. Dispose만으로 복원됐다고 간주하지 않는다. 반환한 참조 폐기, 중복 Handle 발급 금지, 실패 시 다른 정리 계속, 늦은 콜백 무시를 적용한다.
- [x] **P2.8 자동 검증:** 동일 칸 커서 이동, 미응답/거절/옛 revision 색, 확정 위치 동결, Renderer 실제 색/재질 복원, RenderFacts 중 흐림 유지, 풀 재사용과 반복 Attach/Detach를 검증한다. 기존 입력/배치/회전/표시 회귀를 실행한다. DB 에셋 변경이 있으므로 `DataBaseAssetTests`를 실행한다.
- [ ] **P2.9 Unity 재생/표현 확인:** 아래 화면 확인 목록을 실행하고 알파·중립색을 화면에서 조정한다. 값을 조정하면 원본 SO만 갱신하고 관련 DB 검증을 다시 확인한다. 불투명한 상태를 반투명 완료로 보고하지 않는다.
- [x] **P2.10 기록/안내:** 실제 검증 증거·미확인 부분·재질 설정 위치를 실행 기록에 남기고 기존 입력 사용 안내에 Q 범위/고스트 동작을 짧게 추가한다. 필요 공유 문서는 작성만 하며 외부 발송하지 않는다.

**필수 자동/Unity 증거**

| 대상 | 기대 증거 |
|---|---|
| 같은 칸에서 연속 커서 이동 | 경로/견적 갱신 없음에도 Ghost 월드 위치 변경, 추가 Pool 대여 없음 |
| 견적 미응답·거절·빠른 경로 수정 | 중립→현재 승인/거절색, 옛 응답색 잔류 없음 |
| UI/밖/재진입·확정 대기 | 고스트 숨김/재표시 규칙 유지, 동결 위치 변경 없음, 틈을 가로지른 경로 생성 없음 |
| 원본 표시 | 원래 위치/회전/점유/Collider 유지, 모델만 반투명, HUD/체력 숫자 원래 표시 |
| RenderFacts·적 회전 공존 | 재표시가 흐림을 취소하지 않고 흐림 복원이 회전 override를 지우지 않음 |
| 취소·제출·종료·해제 실패 | Ghost 1회 반환, 원본 재질/색 명시 복원, 남은 정리 계속, 재생/결과 확인 유지 |
| 같은 프리팹 새 개체·재전투 | 알파/색/방향/확정 위치/범위 누수 없음, ColorHandle 중복 발급 없음 |
| 배치 회귀 | 손패 집기·필드 유닛 재배치·원본 숨김/복원·배치색과 GridOutline 배치 활성 계약 유지 |
| 셀/격자 두 모드 | 범위 아래 경로/호버 강조, 고스트가 집기를 막지 않음, 필드 회전/스케일에도 커서와 정렬 |
| 재질 | 모든 모델 슬롯과 원래 텍스처/색 보존, 투명 모드 실제 화면 확인, 공용 원본 에셋 변경 없음 |

기존 `CombatMovementFeedbackTests`, `CombatPathInputCleanupTests`, `CombatDisplayReuseTests`, `FieldUnitFacingTests`, `ColorHandleLifetimeTests`와 배치 입력 테스트를 재사용한다. 실물 Renderer/Ghost 복원은 동일 테스트 어셈블리의 신규 `CombatMovementPlanningDisplayTests` 및 `.meta`로 추가한다. 관리 직접 실행은 실제 Unity 렌더링 증거를 대체하지 않는다.

**완료/리뷰 보고:** 두 기능의 실제 동작, 반투명/고스트 설정 위치, 요청·색·재질·풀 수명 검증, 배치/회전 회귀, Unity 확인 상태. Phase 2 끝에서 멈춘다. 승인 범위를 벗어나는 셰이더/공용 변경이 남았으면 미완료 항목과 필요한 결정을 명시한다.

## 3. 검증과 문서의 원본

- 설계 동작/경계는 설계 문서, Task와 완료/대기는 이 계획, 실행 수치/로그는 실제 실행 기록이 원본이다.
- 관련 MSBuild 오류 0·변경 파일 새 경고 0을 확인하고 가능한 Unity 에디터 컴파일과 EditMode Test Runner를 실행한다. 실제 접근 수단이 없으면 수행한 검증과 사용자 확인 대기를 구분한다.
- 기존 결과 제출/확인·Live/RecordInputOnly 경계는 수정 대상이 아니다. 기능을 위해 기존 테스트의 보장을 지우지 않는다.
- DB 에셋/재질/GUID 변경은 Phase 2 범위만 비교한다. 옛 Missing Script 에셋은 삭제하지 않는다.
- 매 프레임 Range/Move 재요청, 포인터마다 Instantiate/GetComponents/ToArray, 원본 전체 숨김으로 반투명 대체가 없는지 코드 리뷰한다. 성능 목표나 측정 없는 최적화 작업은 추가하지 않는다.

## 4. 실행 기록

### 4.1 Phase 0 (2026-10-13)

사용자가 두 기능의 구조 검토 방향을 그대로 구현 계획으로 정리해 달라고 요청했다. 현재 코드와 진행 중인 적 연출 변경을 다시 읽고 설계·Task 계획을 작성했다. Phase 1은 범위/요청 순서, Phase 2는 고스트/반투명/에셋/Unity 확인으로 나눴다. 범위 응답 중 초안 편집 유지, 동일 칸 커서 추적, 공유 Range 층 소유권, 정확한 동결 견적, 재질 복원/풀 수명을 완료 조건으로 정했다.

이 단계에서는 문서만 작성했으며 운영 코드·씬·에셋 수정, 테스트 실행, 커밋·push는 하지 않았다. 다음은 Phase 1이다.

### 4.2 병행 독립 준비 완료 (2026-10-13)

사용자가 공통 연결을 다른 세션 종료 뒤로 미루고 독립 로직·전용 테스트 준비만 허용했다. §0A의 Request/Facts와 전용 테스트·meta를 추가했다. Buffer 읽기/쓰기·규칙/범위 계산·Draft/견적/표시 상태는 추가하지 않았으며 기존 런타임 클래스/인터페이스/씬/DB/공통 FakeDisplay를 변경하지 않았다. 상세 전체 계획 §7.72도 이번에는 갱신하지 않아 다른 세션의 공통 문서 편집을 피했다.

**검증:** `Temp/MovementRangeIndependent/Independent.csproj`는 새 Request/테스트 소스와 전용 실행기만 컴파일한다. 참조는 완료된 `Temp/TemporaryAttackC1/Probe/Assets/Plugins`의 Systems/Contracts 및 설치된 NUnit의 **사본**이다. 프로젝트 참조/패키지 다운로드는 없고 기존 빌드/Unity 출력과 격리했다. `.NET 8` 빌드 오류 0·경고 0(`build.log`), NUnit fixture의 Test/TestCase 30 통과·0 실패(`results.txt`). 이는 관리 코드의 독립 검증이며 최신 운영 어셈블리 전체 빌드·Unity Test Runner·실제 입력/표시 증거가 아니다. GUID 중복 검색에서 새 두 GUID는 각 meta 한 곳에만 존재한다.

**남은 작업:** 다른 세션 종료 확인 뒤 P1.1부터 공통 연결을 진행한다. P1.2의 호출자 조립/소유 요청 정리, P1.3 견적 유예·확정, P1.4~P1.8 표시/회귀/Unity 검증과 Phase 2는 아직 미완료다. 이번 승인 범위를 완료해 여기서 멈춘다. 커밋·push·외부/다른 세션 메시지 발송은 하지 않았다.


### 4.3 Phase 1 범위 연결 완료 (2026-10-13, Unity 화면 확인 대기)

사용자의 “Phase1 작업 시작 해” 지시를 받아 착수했다. 다른 세션 “CatusCannon 임시 공격 연출 찾기”는 R2를 마치고 대기 중이었으며 작업 트리는 깨끗했다. 최신 공격·피격·사망 코드를 인수하고 SceneDisplay의 조립/위임/정리 줄만 수정했다. Core·Buffer·Flow/Job/Plan·FieldCamera·Architecture·씬·View DB·머티리얼·공용 셰이더는 변경하지 않았다.

**구현:** Q 수락 시 `CombatPathInput`이 실제 Buffer 발급 `MoveRange` 요청과 §0A Request를 연결한다. 전투 ID/행위자/시작 위치/결과 순번/현재 슬롯/응답 번호를 대조해 한 번 소비한다. 범위를 기다리는 동안 `CombatMovementInteraction`은 Draft 편집·되돌리기·클릭 동결을 유지하고 견적 게시만 유예한다. 일치하는 범위 응답 뒤 최신 Draft 하나의 Move 견적을 게시한다. 0걸음이면 게시하지 않으며 범위 응답 자체로 Move를 승인하거나 제출하지 않는다. 실패/빈 범위도 정식 응답이면 유예를 끝내고 기존 정확한 경로/context 검증을 따른다.

`ICombatMovementFeedback`의 좁은 Show/Clear 범위 의도와 신규 `CombatMovementPlanningPresenter`를 추가했다. Core `Destinations[].TargetAnchor`만 기존 `FieldView.Range` 층에 전달하며 점유 칸 전체로 확장하거나 View에서 범위를 계산하지 않는다. 취소·재Q·E·Return·턴 종료·결과/행위자 변경·FocusLost·Detach에서 소유 요청/범위/유예를 정리한다. 공유 Range 층은 이동이 표시한 동안만 정리하고 반복 취소가 새 공격 범위를 지우지 않는다. 표시 콜백 뒤 지정 수명을 다시 확인하고, 표시/해제 실패에서도 다른 정리를 계속한다. SceneDisplay Install/Clear/Release도 표시 소유 수명을 정리한다.

**검증:** 신규 `CombatMovementRangeFeedbackTests` **21/21**, `CombatMovementRangeDisplayTests` **6/6** 및 기존 §0A 요청 수명 **30/30** 통과. 관련 입력/Router/정책/Draft/이동 표시/필드 강조·GridOutline/전투 진행·실제 Combat 씬 테스트를 포함한 독립 Unity EditMode 회귀 **438 통과·0 실패·0 건너뜀**. 기존 손패 좌표에 관한 `HandCards_OrderBySlot_`·`HandCards_PickByScreenPoint_` 두 계열은 이 범위 회귀 대상에서 제외했다. 새 테스트 어셈블리/생성 csproj 변경 없이 기존 테스트 Rig/FakeDisplay와 실제 씬 테스트를 범위→정확한 Move 견적의 두 응답 단계로 갱신했다. 실제 씬 테스트는 범위 표시/제출 뒤 해제, 이동 연출과 체크포인트 복귀까지 검증한다.

- VS 2022 MSBuild: `ProjectL.Tests.Framework.csproj`와 참조 런타임 빌드 오류 0, 변경 파일 새 경고 0. 기존 Core nullable·UI 미할당 필드·테스트 대역 경고는 이번 변경과 무관하며 수정하지 않았다.
- 추가 Compile만 지정한 `Temp/MovementRangePhase1/NewFiles.targets`, 별도 `bin`·`obj` 사용. Unity는 같은 폴더의 `Probe`에 기존 에셋 검증 프로젝트 Assets/Packages/ProjectSettings와 최신 런타임 DLL/테스트 소스를 복사했다. 복사 에셋의 MonoScript만 DLL에 remap하고 원본 프로젝트 Library/에셋은 변경하지 않았다.
- 최초 Unity 실행은 샌드박스의 Package Manager 로컬 IPC 연결 실패로 Test Runner에 도달하지 못했다. 격리 프로젝트만 권한 있는 배치 실행으로 재시도했다. 첫 검증 434/438에서 새 CellPrefab 테스트의 ColorSystem 대역과 실제 씬 테스트의 응답 순서를 수정했고, 최종 동일 회귀는 438/438 통과했다.
- 최종 증거: `Temp/MovementRangePhase1/msbuild-tests-final.log`, `unity-edit-final2.log`, `tests-final.xml`, `tests-final-summary.txt`. 최초 테스트 결과는 `tests-initial.xml`에 보관했다. 원본 GameView 수동 재생/픽셀 외형 검증을 수행한 것으로 보고하지 않는다.

**구조 점검:** 입력 허용 판단은 기존 `CanEditPath`, 규칙 값은 Core 응답, 번호/실행 상태는 Buffer가 원본이다(SSOT). Request는 정체성·1회 소비, Interaction은 Draft/견적, PlanningPresenter는 범위 표시만 맡는다(SOLID). 새 내부 표시 협력자는 서비스/Factory/전역 이벤트를 추가하지 않으며 SceneDisplay의 기존 수명으로 정리한다(JMH/FW). 기존 강조 상태·표시기·풀·색 서비스 계약을 재사용했다. 새 C#과 meta를 함께 추가했고 GUID 중복 및 diff 공백 오류를 점검했다.

**남은 확인:** P1.8 원본 GameView에서 두 표시 모드의 범위색·경로/호버 겹침·빠른 Q/확정·우클릭/E/턴 종료 후 해제를 확인한다. 자동 테스트의 실제 FieldView/씬 검증과 화면 외형 확인은 구분한다. Phase 1 코드·자동 검증을 완료해 리뷰 지점에서 멈춘다. Phase 2 고스트/반투명·자원·에셋은 시작하지 않았으며 커밋·push·외부 발송은 하지 않았다.


### 4.4 Phase 2 연결·자동 검증 완료, Standard 재질 대응 보류 (2026-10-13)

사용자의 “Phase2 진행해” 지시로 작업했다. Phase 1이 커밋된 깨끗한 작업 트리에서 최신 공격·피격·사망·회전 수명을 인수했다. Core·Buffer·Flow/Job/Plan·FieldCamera·Architecture·씬·공용 Render/셰이더는 수정하지 않았다. 최종 URP 픽셀 검사 뒤 Standard 대응 필요성을 보고했으며 사용자는 **“여기서 멈추고 기존 재질을 별도로 정리”**를 선택했다. Standard를 URP/Lit으로 변환하는 변경은 적용하지 않고 이 리뷰 지점에서 멈춘다.

**구현:** 실제 Draft가 시작될 때만 `CombatPathInput`이 행위자 ID/외형/방향을 Feedback으로 전달한다. Q의 시작 칸 대기에서는 원본을 흐리지 않는다. `CombatMovementPlanningPresenter`가 범위 수명과 편집 수명을 나눠 기존 `FieldUnitPresenter`/`PlacementGhost`를 조정한다. 이동은 배치용 원본 숨김이나 `SetPlacementActive`를 호출하지 않는다. 포인터 갱신은 경로 표시 캐시와 독립적이며 같은 칸 안에서도 연속 월드 위치를 반영한다. UI/필드 밖과 기존 끝점 재진입 규칙은 유지한다. 견적 대기는 중립색, 현재 revision의 승인/거절만 유효/무효색에 반영한다. 확정 뒤에는 포인터 입력·UI 이동·새 카메라 좌표 변환도 이미 표시한 월드 자세를 바꾸지 않고 색만 갱신한다.

원본 모델은 새 `MovementModelOpacity`가 준비 단계에 추린 Renderer의 원래 모든 슬롯/shared material/전체·슬롯 PropertyBlock을 보존한다. 기존 Standard/URP 셰이더의 투명 모드 복사 재질과 Renderer당 하나의 ColorHandle을 첫 사용에 준비해 재사용한다. 원본 텍스처·색·슬롯과 위치/회전/Collider를 유지한다. 비활성 풀에서도 Canvas/TMP 부모를 포함해 HUD·체력 숫자를 제외한다. RenderFacts·회전 override와 공존하고, 사망/필드 이탈/새 ID/풀 반환/입력 종료에서는 재질·블록을 명시적으로 복원한다. Release/최종 파괴는 소유 변형과 손잡이를 반환하며 하나의 실패 뒤에도 다른 정리를 계속한다. 고스트는 기존 프리팹 풀을 유지하고 일시 숨김은 대여를 유지한다. 재대여 때 위치·방향·배치색을 초기화하고 최종 Release에서는 손잡이·클론을 정리한다.

**설정:** `StageFieldView_Default.asset`에 `movementOriginalOpacity=0.65`, `neutralGhostTint=(0.8,0.8,0.8,0.5)`를 기록하고 유한값/알파 범위를 검증한다. View 소유 `Assets/ProjectL/Resources/View/MovementGhost.mat`은 기존 URP/Unlit 셰이더의 Transparent/Alpha Blend/ZWrite Off/queue 3000을 사용한다. 기존 유효·무효색·크기·높이를 재사용한다. 기존 배치 고스트도 이 재질을 사용하므로 기존 배치 회귀를 함께 실행했다. 모델 원본 shared material과 공용 셰이더 에셋을 수정하지 않았다.

**자동 검증:** 신규 `CombatMovementPlanningInputTests` **14/14**, `CombatMovementPlanningDisplayTests` **18/18** 통과. 같은 칸 추적/견적 미응답·거절·옛 revision/확정 동결/취소·Detach·차단·복귀/표시 콜백 실패·재진입, 실물 두 셰이더 슬롯·PropertyBlock·HUD/Collider·풀 재사용·재전투·회전·사망 유지·부분 시작 실패·손잡이 해제 실패를 검증했다. 필드의 기존 월드 크기 (1,1,1) 계약은 바꾸지 않았고 Y 회전과 크기가 다른 고스트 루트에서 월드 추적을 검사했다. 기존 범위/이동/Draft/입력/배치/표시·색/공격·피격·사망·회전 회귀를 포함한 격리 Unity EditMode 최종 결과는 **599 통과·1 실패·0 건너뜀**이다.

- `DataBaseAssetTests` **22 통과·1 실패**. 실패는 `EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`의 Stage 1101 지형 좌표 index 5: 예상 (7,3), 실제 (6,3). `Temp/TemporaryAttackR1/final-r1-tests.xml`·`Temp/TemporaryAttackR2/tests-final.xml`에 동일 실패가 있어 이번 변경으로 생긴 실패와 구분한다. 해당 Core/지형 정의는 수정하지 않았다.
- 기존 손패 좌표 `HandCards_OrderBySlot_`·`HandCards_PickByScreenPoint_` 계열 두 가지는 Phase 1과 같은 회귀 필터에서 제외했다. PlayMode 전용 사망/피격 lifecycle fixture는 EditMode의 0건이며 실행 증거로 세지 않는다.
- VS 2022 MSBuild `ProjectL.Tests.Framework.csproj`와 참조 런타임: 오류 0, 변경 파일 새 경고 0. 기존 Core nullable·UI/테스트 대역 미할당 경고는 유지했다. 임시 추가 Compile targets와 별도 bin/obj를 사용하고 생성 csproj/asmdef를 수정하지 않았다.
- 독립 `Temp/MovementRangePhase1/Probe`에 최신 DLL·테스트·변경 에셋의 사본을 반영했다. 원본 프로젝트 Library/씬/그래픽 설정은 수정하지 않았다. 최초 새 표시 테스트의 필드 크기 설정·좌표 오차를 수정했고, 재대여 검사에서 드러난 비활성 HUD 탐색 누락도 고쳤다.
- 최종 자동 증거: `Temp/MovementPlanningPhase2/msbuild-tests-final.log`, `unity-edit-final.log`, `movement-planning-tests-final.xml`, `movement-planning-tests-final-summary.txt`. 이전 실행 실패/결과도 같은 폴더에 보존했다. GUID 5개는 각각 meta 한 곳에서만 발견됐으며 diff 공백 오류 0이다.

**실제 셰이더 픽셀 검사와 남은 제한:** 운영 MainCharacter/Akimbo/ShotGun/Sniper 프리팹에 Standard와 URP/Lit 슬롯이 섞여 있다(`movement-phase2-material-inspection.txt`). 격리 그래픽 실행에서 원본 URP 전역 설정의 사본과 같은 URP 패키지의 기본 Renderer를 사용했다. 첫 시도의 누락 Texture/Core/Config include와 전역 설정 MonoScript 연결은 사본에서만 보완했다. 최종 검사에는 셰이더 컴파일 오류가 없으며, 청색 배경의 흰 Quad 중앙 픽셀은 URP/Lit·Unlit에서 원본 (1,1,1) → 흐림 (0.651,0.651,1) → 복원 (1,1,1), 중립 Ghost는 (0.400,0.400,0.902)로 실제 알파 혼합을 확인했다. 이 검사는 셰이더/재질의 offscreen 검사이며 운영 PC Renderer 전체 기능이나 원본 GameView 수동 확인을 대체하지 않는다. 증거는 `unity-pixels-final.log`, `movement-phase2-pixels.txt`, `movement-phase2-*.png`와 실행기 `MovementPhase2Pixels.cs`다.

반면 **Standard는 원본/흐림/복원 모두 (1,0,1)의 URP 오류색**으로 표시됐고 반투명이 되지 않았다. 셰이더 이름/알파/블렌드 프로퍼티만으로 해당 슬롯을 화면 완료로 간주하지 않는다. 기본 URP Renderer 검사와 운영 프리팹의 Standard 참조가 함께 확인됐으므로 해당 슬롯의 현재 파이프라인 호환성 대응이 남았다. 기존 셰이더의 투명 모드만 쓰는 D6 범위를 넘어서는 View 전용 URP/Lit 변환을 제안했지만 사용자는 이번에는 별도 재질 정리를 선택했다. **P2.6은 지원 URP 슬롯 적용·수명 코드와 자동 검증까지 완료, 전체 운영 슬롯 반투명은 미완료. P2.9의 원본 GameView 두 모드/배치/색 조정도 확인 대기**다. 전체 Phase 2의 표현 완료로 보고하지 않는다.

**구조 점검:** Draft/확정/견적 revision은 기존 Interaction, 입력 허용은 기존 Policy, 가능 여부·범위·숫자는 Core/Buffer, 표현 수치는 View DB가 원본이다(SSOT). PathInput은 의도, PlanningPresenter는 표시 수명, Ghost/UnitView는 Unity 적용과 소유 자원을 맡는다(SOLID). 새로운 서비스/Factory/입력 구독/Coroutine을 만들지 않고 기존 색·풀·SceneDisplay 수명을 사용한다(JMH/FW). 포인터마다 Instantiate/GetComponents/ToArray를 호출하지 않으며 ColorHandle을 중복 발급하지 않는다. 재질 대응 선택을 기다린 뒤 변환을 적용하지 않았고 커밋·push·외부 발송도 하지 않았다.
