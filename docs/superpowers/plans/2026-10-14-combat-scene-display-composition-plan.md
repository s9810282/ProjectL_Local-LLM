# CombatSceneDisplay 조립·기능 분리 Task 계획

- 작성: View 작업, 2026-10-14.
- 상태: **SD-D0~D5 계획 범위 완료. 구현·현재 Unity 결과 인수·구조 평가·각 Phase 새 에이전트 검사 완료.** Unity는 D5 사용자 전체 통과 확인을 출처로 하며, 과거 수정 전 기준선 미확보는 유지한다. 현재 기록은 §10.7이다.
- 설계 원본: [후속 설계 §3·§5.3·§5.5·§6](../specs/2026-10-14-combat-scene-display-followup-design.md).
- 판단 기준: [설계 인계 §4, 특히 기능 변경의 독립성 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md). SOLID 5원칙·SSOT·JMH/FW와 함께 적용한다.
- 선행 상태 원본: [SD-A/B/C′ 및 C0 평가 계획](2026-10-14-combat-scene-display-followup-plan.md) §7.4~7.8. 기존 구현·검증 이력을 복제하지 않는다.
- 계획 작성 중 HEAD가 `5f6b315e`에서 **`b751fc45a23d6ab7dd97862cdd8f1b2e22245a83`**로 바뀌었다. 후자를 현재 기준으로 확인했고 당시 작업 트리는 깨끗했다. 선행 변경은 현재 코드에 반영돼 있다. 커밋 여부와 Unity 검증 완료 여부는 별개다.
- 이후 별도 커서 스냅 감도 작업의 변경(Display/SceneRoot의 `CursorSnapThreshold`, CursorSnap 계약·테스트 등)이 생긴 것을 확인했다. 이 계획의 구현으로 세지 않으며, D0에서 최신 코드와 동시 편집 여부를 다시 확인하고 보존한다.
- 이 문서가 **조립·기능 분리의 Task·실행 상태 원본**이다. 기존 SD-C1 전체 입력 피드백 이전안과 구분한다.

## 1. 범위와 완료 목표

`CombatSceneDisplay`는 소비자가 사용하는 단일 표시 입구, 해제 뒤 호출 계약, 공통 수명 순서를 유지한다. 구체 생성·Configure·주입은 전용 Factory로, 이동·배치·공격의 표시 구현은 해당 기능으로 모은다.

| 변경 종류 | 전체 작업 완료 때의 목표 |
|---|---|
| 기존 API 안의 이동·배치·공격 표현 변경 | 해당 기능 구현과 관련 검증을 중심으로 수정·검토할 수 있음 |
| 객체 생성 인자·연결·구현 선택 변경 | Factory와 해당 기능에 모임. Display 생성자에서 다시 `new`·Configure하지 않음 |
| 공유 비용·Ghost·Range·Mapper 계약 변경 | 영향받는 기능의 교차 검증을 명시적으로 선택할 수 있음 |
| Install·Clear·Release 변경 | 공통 수명 변경임을 명시하고 관련 통합 회귀로 검증 |

줄 수·필드 수를 임의의 목표치로 줄이지 않는다. Factory 추출만으로 전체 작업을 완료 처리하지 않는다. 최종 단계에서도 공통 수명·Refresh에 실제 필요한 참조는 Display에 남길 수 있다. 기능 구현을 모두 다른 큰 클래스나 무타입 `Parts`/서비스 가방에 모으지 않는다.

**포함하지 않는 변경:** Core·Buffer·FieldCamera·Architecture, 씬·프리팹·DB 값, Audio 호출 위치·정책, 입력 정책, 소비자 인터페이스 해체, 새로운 Framework 서비스/Builder, Refresh 전체 이전, 전용 수명 관리자 신설, 범용 롤백. 생성 실패와 인수 전 해제 누락은 §3의 명시적 범위대로 다룬다. 커밋·push·외부 전달은 사용자가 한다.

## 2. Phase 순서와 공통 리뷰 절차

| Phase | 작업 | 선행 | 종료 산출물 |
|---|---|---|---|
| **SD-D0** | 착수 기준·조립 계약·검증 기준선 확정 | 사용자 착수 지시 | 의존성/소유권 표, 기존 검증 인수, D1 변경 명세 |
| **SD-D1** | 전투 씬 전용 Factory로 조립 추출 | D0 검토·보고 | 실행 로직과 분리된 생성·Configure·연결 경로 |
| **SD-D2** | 이동 초안 구현을 기존 Presenter로 이전 | D1 검토·보고 | 이동 구현 집중, 좁은 이동 표시 검증 |
| **SD-D3** | 배치 표시 구현 분리 | D2 검토·보고 | `CombatPlacementFeedback`, 공유 Ghost 계약 유지 |
| **SD-D4** | 공격 표시 구현 분리 | D3 검토·보고 | `CombatAttackFeedback`, 공용 비용·층 계약 유지 |
| **SD-D5** | 통합 인수·변경 독립성 평가 | D4 검토·보고 | 검증 증거, 기능 수정 지도, Refresh 후속 판단 |

**모든 Phase는 아래 순서로 종료한다. D0의 문서 작업과 D5의 인수 작업도 예외가 아니다.**

1. 해당 Phase 범위만 수행하고 가능한 검증을 마친다. 변경 파일·기준 HEAD/작업 전 상태·실행한 검증·남은 검증·실습 순서를 기록한다.
2. 작업물이 준비된 **뒤에 새 하위 에이전트 한 개를 생성**한다. 이전 Phase 검토자나 해당 Phase 구현에 참여한 에이전트를 재사용하지 않는다. 기존 에이전트에게 새 이름의 역할만 주는 것도 새 검사로 세지 않는다.
3. 검토자는 읽기 전용으로 해당 Phase 전후 차이와 설계·Task를 대조한다. 정확성, SOLID 5원칙, SSOT, FW/JMH, 기능 변경 범위, 실패·수명·회귀 증거를 검사한다. 부모의 결론만 확인하는 검토로 끝내지 않는다.
4. 범위 안의 지적은 처리하고 그 새 검토자에게 수정분을 재검사받는다. 설계·범위 변경이 필요하면 구현을 확대하지 않고 쟁점과 대안을 보고한다.
5. **변경·검증·검토 결과·잔여 사항과 다음 Phase를 보고하고 멈춘다.** 다음 Phase는 사용자의 진행 지시 뒤 시작한다.

검토 결과와 실행 검증은 별도다. Unity를 실행하지 못했으면 `코드·검토 완료 / Unity 대기`로 기록하고 Phase 전체 검증 완료로 쓰지 않는다. 후속 진행 시에도 대기 항목을 유지한다. 테스트 실패가 다음 Phase의 전제나 변경 판별을 막으면 원인·해결 범위를 먼저 정한다.

## 3. 조립·실패 처리의 확정 범위

**D1은 동작을 유지하는 조립 추출이다. 알려진 오류 회복 정책을 동시에 바꾸지 않는다.** 실패 지점을 Factory로 옮겼다고 해결됐다고 쓰지 않으며, 다음 경계를 D0에서 현재 코드와 대조한다.

| 경계 | 이번 계획에서 지킬 것 | 별도 후속 작업 |
|---|---|---|
| Factory 호출 전 | `CombatViewSystem.Attach`의 root/초기화/활성 전투/`_display != null` 검사 유지. `IsAttached`로 대체하지 않음 | 새 자동 재연결 정책 없음 |
| 조립 도중 | 기존 인자·root 검증과 Configure·생성 순서 보존. EventPresenter의 step 검증보다 가시밭 소유 GameObject를 먼저 만들지 않음 | `MovementStepSeconds` 사전 검증 통합, 부분 Configure 복구·조립 원자성 |
| Factory 반환 후 `_display` 저장 전 | 현재 AttachDisplay의 인수 시점 유지. 이때 입력 Clear/Presentation Attach 등이 실패하면 Detach가 생성된 Display를 찾지 못할 수 있음을 미해결로 기록 | 인수 전 예외의 명시적 정리 담당·취소/해제 순서 보완 |
| `_display` 저장 뒤 Install 실패 | 보관된 Display를 후속 Detach가 반환하는 기존 계약 유지 | 인수 전 실패와 같은 롤백으로 통합하지 않음 |

별도 항목은 이 계획의 성공 조건에서 숨기지 않되, 순수 추출의 완료를 오류 회복 개선 완료로 과장하지 않는다. 계획 범위 밖 결정을 새 오류가 강제한다면 공통 리뷰 절차에 따라 먼저 보고한다.

조립 진입점은 `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`의 `internal static Create(root, tables, services)`로 정한다. 새 인터페이스나 서비스 등록은 없다. 반환한 Display의 조립 책임은 끝나지만 실제 수명 인수 시점은 위 표대로 구분한다.

Display는 준비된 협력 객체와 실제 런타임에 필요한 설정·서비스를 **명시적인 타입으로** 받는다. 기존 참조가 많아 D1 주입 생성자가 길어지는 것은 중간 단계의 비용으로 기록한다. 이를 감추는 `Parts`, `Get<T>()`, 후설정 setter를 추가하지 않는다. D2~D4에서 기능의 직접 구현과 필요한 의존성 사용을 함께 모은 뒤 D5에서 남는 참조의 이유를 재평가한다. 작은 생성자를 만들기 위해 공통 수명을 이번 범위 밖으로 옮기지 않는다.

## 4. SD-D0 — 착수·조립 계약·검증 기준선

범위는 조사·기록과 기존 검증이다. 런타임·테스트 코드를 고치지 않는다.

- [x] **D0.1 기준 코드:** 현재 HEAD·dirty/untracked·동시 변경을 다시 확인한다. `b751fc45`의 선행 A/B/C′와 이번 설계·계획이 현재 파일에 반영돼 있는지 읽는다. 커서 스냅 감도처럼 이후 추가된 Display/SceneRoot 계약도 포함한다. 같은 대상 파일의 동시 편집을 피하고 작업자 변경을 덮어쓰지 않는다. 커밋은 요구하거나 대신 수행하지 않는다.
- [x] **D0.2 주입 명세:** Display의 각 참조를 `생성 위치 / 사용 기능 / 공유 인스턴스 / Bind·Clear·Release 소유자` 표로 적는다. D1의 명시적 주입 생성자 서명과 Factory 내 생성 순서를 작성한다. `_fieldBound`의 정의·실행은 Display 한 곳에 남긴다.
- [x] **D0.3 콜백:** `ConsumeThorn`가 Display 자체의 상태를 읽지 않고 같은 ThornPresenter로 위임함을 확인한다. D1에서 사용할 지역 참조 캡처의 단일 할당과 EventPresenter 생성 중 콜백이 실행되지 않는 전제를 대조한다(§5).
- [x] **D0.4 검증 인수:** A/B/C′의 기존 빌드·관리 회귀와 Unity 대기를 각각 기록한다. 새 커밋을 새 Unity 통과 증거로 취급하지 않는다. 해당 기준에서 컴파일 및 D1이 의존하는 기존 조립·수명 회귀의 실행 가능 여부/결과를 확인한다. 현재 Unity 세션을 강제로 종료하거나 같은 프로젝트를 중복 실행하지 않는다.
- [x] **D0.5 영향 파일:** §5~8의 생성 호출부·기능 호출부·테스트 목록을 `rg`로 다시 확인한다. 실제 추가 호출부가 있으면 목록을 보완하며 행동 범위가 달라지면 먼저 보고한다.
- [x] **D0.6 새 에이전트 검사·정지:** §2 절차를 수행한다. 주입 명세가 원래의 공유 참조·검증·생성 순서를 보존하는지, 실패 보완 제외가 명확한지 검사한다.

**완료 조건:** D1에서 어떤 객체를 어디서 만들어 누구에게 넘기며 무엇을 그대로 두는지 명세로 확인할 수 있다. 알려진 실패·Unity 대기를 구분한 기준선과 새 검사 결과가 남는다. D0 종료 보고에 D1 착수 가능 여부를 명시한다.

## 5. SD-D1 — 전투 씬 Factory 조립 추출

| 구분 | 변경 파일 |
|---|---|
| 운영 | 새 `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`와 `.meta`, `CombatSceneDisplay.cs`, `CombatViewSystem.cs` |
| 계약 설명 | `CombatDisplayTables.cs`, `CombatDisplayServices.cs`, `CombatSceneEventPlayback.cs`의 조립 경로 설명 주석 |
| 기존 실제 조립 fixture | `CombatDisplayReuseTests.cs`, `CombatPointerMappingTests.cs`, `CombatSceneEventPresenterTests.cs`, `TerrainOperationalDisplayTests.cs`, `WallAttackInteractionTests.cs`, `CombatMortarAssetTests.cs`의 직접 생성 경로(직접 `new`와 공통 helper를 함께 확인) |
| 문서 | 이 계획 실행 기록·Phase 실습 워크시트, Factory 경로 변경의 Framework 공유 메모(작성만, 외부 발송 안 함) |

위 운영 파일에서 `Factory/…` 이외의 이름은 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` 아래다. 테스트는 `Assets/Tests/EditMode/Framework/` 아래다.

- [x] **D1.1 Factory 생성:** D0 명세대로 root/tables/services 검증, SceneRoot 참조 읽기, `new`·Configure·외형 규칙 등록·콜백 연결을 이동한다. 표·서비스 묶음을 하위 기능에 통째로 전파하지 않는다. 현재 null Audio·선택적 Thorn prefab 의미를 보존한다. 스냅 켜짐·감도 등 실행 중 Inspector에서 읽는 설정을 생성 시 값으로 고정하지 않는다.
- [x] **D1.2 Display 생성자:** 생성·Configure 대신 준비된 객체를 보관한다. `_fieldBound` 정리 목록과 Install/Refresh/Clear/Release의 순서·예외·가드를 그대로 둔다. 새 Field/Units/Ghost/Mapper 복사본을 만들지 않는다. 이전 조립 생성자를 남겨 두 경로가 공존하게 하지 않는다.
- [x] **D1.3 가시밭 콜백 연결:** Factory에 `FieldThornPresenter thorns = null` 지역 참조를 두고, 기존 EventPresenter 생성 위치에서 `cell => thorns?.Consume(cell)`를 전달한다. 기존 순서대로 그 뒤에 가시밭 Presenter를 만들어 지역 참조에 한 번 할당한다. 같은 객체를 EventPlayback·Display에 주입한다. 생성 완료 뒤 참조를 재할당하거나 외부에 노출하지 않는다. 불필요해진 Display의 `ConsumeThorn` 전달 메서드는 제거하되 Clear/Release용 메서드는 유지한다.
- [x] **D1.4 공개 Attach:** 기존 사전 검사 뒤 Factory를 호출해 `AttachDisplay`로 넘긴다. `_display` 인수·입력 바인딩·root 구독 순서를 바꾸지 않는다. Presentation에는 내부 Playback이 아니라 계속 실제 Display를 넘긴다.
- [x] **D1.5 테스트 조립 경로:** 실제 Display를 만드는 기존 fixture를 Factory 경로로 바꾼다. 현재 직접 생성은 위 6개 fixture의 8곳이며 `CombatMortarAssetTests`의 target-typed `new(_root, …)`도 포함한다. FakeDisplay 기반 ViewSystem 단위 테스트를 새 Factory에 결합시키지 않는다. 생성 경로 전환을 이유로 씬·DB fixture의 관측과 정리 보장을 약화하지 않는다.
- [x] **D1.6 검증:** Systems·EditMode 어셈블리 컴파일, 중복 Attach 두 경우(StopInput 전/후), 최초 설치·Build 실패·Clear→재설치·Release 뒤 늦은 사건, 같은 Display의 재생 Attach, 가시밭 소비/소멸·소유 clone 반환을 확인한다. §9의 D1 선택 묶음을 사용한다.
- [x] **D1.7 부족한 조립 회귀:** 잘못된 step 설정이 가시밭 루트 생성 전에 실패하는 행동이나 동일 가시밭 인스턴스 연결을 기존 테스트가 판별하지 못할 때만 행동 회귀를 보완한다. 생성자 호출 횟수나 각 전달 인자를 그대로 복사하는 테스트는 만들지 않는다. 전체 Configure 원자성을 새 보장으로 검사하지 않는다.
- [x] **D1.8 기록:** Factory 변경을 기존 Framework/Architecture 구현 변경과 구별해 공유 메모로 남긴다. 새 C#의 `.meta`, namespace, 한국어 설명·UTF-8/LF를 확인한다.
- [x] **D1.9 새 에이전트 검사·정지:** 생성 순서·같은 인스턴스·수명 인수·실패 의미와 SD-A/B 보존을 우선 검사한다.

**완료 조건:** 구체 생성·Configure 변경은 Factory에서 할 수 있고, Display에 조립 구현이 남지 않는다. 공통 수명·재설치·공개 방어는 그대로다. D1은 조립 분리 완료이며 전체 기능 분리 완료는 아니다.

## 6. SD-D2 — 이동 초안 구현 집중

수정 대상: `CombatMovementPlanningPresenter.cs`, `CombatSceneDisplay.cs`, Factory, `CombatMovementPlanningDisplayTests.cs`, `CombatMovementRangeDisplayTests.cs`, `CombatPointerMappingTests.cs`. 필요하면 좁은 초안 fixture `CombatMovementDraftDisplayTests.cs`와 `.meta`를 추가한다.

- [x] **D2.1 의존성:** 기존 전체 생성자(Field/Units/Ghost/Config)에 필수 `CombatHud`를 더한다. Field-only 생성자는 범위 전용 테스트 경로로 유지한다. 운영 Factory와 전체 생성자를 쓰는 기존 이동 fixture를 갱신한다. HUD를 null로 받는 운영 fallback이나 사후 setter는 추가하지 않는다.
- [x] **D2.2 구현 이동:** `ShowDraft`, `ClearDraft`, `SetPathPlanning`의 필드 층·끝점·이동 HUD·비용 의도를 기존 이동 Presenter로 옮긴다. Display는 공개 해제 방어 후 위임한다. Presenter의 기존 `_released`는 재사용하되 새 플래그나 비용 대상·초안 캐시를 만들지 않는다.
- [x] **D2.3 경계 보존:** `UpdateMovementPointer`의 screen→world 변환은 기존 Display/PointerMapper 경계에 둔다. `UpdatePointer(Vector3?, …)`에 카메라를 추가하지 않는다. `ShowDraft`에 `_editing` 전제를 추가하지 않는다. Field-only 경로에서 기존 Range 동작이 HUD를 요구하지 않도록 한다.
- [x] **D2.4 정리 보존:** `Clear`는 기존 EndEditing+ClearRange를 유지한다. `ClearDraft`나 `SetPathPlanning(false)`를 Install 전·실패·EndEditing에 자동 합치지 않는다. 입력 측의 독립 정리 단계와 공통 비용/체력 구분을 유지한다.
- [x] **D2.5 좁은 표시 검증:** `DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange`의 세부 표시 검사를 Field/Hud/Units 중심의 이동 fixture로 이전한다. 유효·무효·미응답 초안, 후보 칸과 끝점, BeginEditing 전 표시, **ClearDraft 뒤** 하위 Range·HP 보존을 검증한다. 동일 상세 테스트를 씬 fixture에도 중복 유지하지 않는다.
- [x] **D2.6 통합 검증:** 공격 비용→다른 행위자 이동 초안의 슬롯 교체, ClearDraft의 HP 보존, 해제 뒤 늦은 초안, 이동 정리 실패, 재설치 뒤 Ghost/원본 흐림/Range 정리를 기존 회귀로 확인한다. 변경과 무관한 사건 전체를 매번 재실행하지 않는다.
- [x] **D2.7 새 에이전트 검사·정지:** 실제 이동 표현 변경이 이동 구현에 모였는지, 좁은 fixture가 전체 Combat 씬·Catalog 없이 동작을 검증하는지와 정리 시점을 검사한다.

**완료 조건:** 이동 초안의 표시 조건·HUD·격자 변경을 배치·공격 구현과 분리해 읽고 수정할 수 있다. 공용 슬롯과 해제 방어 회귀는 통합 경계로 남는다.

## 7. SD-D3 — 배치 표시 구현 분리

수정 대상: 새 `CombatPlacementFeedback.cs`와 `.meta`, Display, Factory. 검증은 새 `CombatPlacementFeedbackTests.cs`와 `.meta`, 기존 공유 Ghost·실제 씬 관련 fixture를 사용한다.

- [x] **D3.1 객체 경계:** FieldView·PlacementGhost·FieldUnitPresenter·HandPresenter만 명시적으로 받는 `internal sealed CombatPlacementFeedback`을 만든다. 새 인터페이스·서비스·해제 플래그·풀을 만들지 않는다. 소비자에게 직접 노출하지 않는다.
- [x] **D3.2 구현 이동:** Show/HideGhost의 배치 격자 전환, Hide/ShowOriginal의 필드·손패 원본 조정, SetGhostPlaceable을 모은다. View별 원본 대상 기억은 Units/Hand가 계속 가진다.
- [x] **D3.3 좌표 경계:** MoveGhostToCell의 현재 Mapper/보드 안 검사와 cell→world 적용은 배치 객체에 모을 수 있다. 화면 좌표 MoveGhostToPointer는 Display가 기존 PointerMapper로 변환한 월드 위치를 배치 객체에 전달한다. 카메라·UI Raycast를 배치 객체에 넣거나 Mapper를 생성 시 고정하지 않는다.
- [x] **D3.4 공통 수명:** Ghost의 최종 Release·풀 반환자는 기존 소유자다. Display Clear/Release의 순서를 바꾸지 않고, 배치 객체에 별도 최종 Release를 추가하지 않는다. 이동에서 사용 중인 Ghost·원본 흐림 상태를 배치용 상태로 복제하지 않는다.
- [x] **D3.5 기능 검증:** 좁은 Field/Ghost/Units/Hand fixture에서 고스트와 격자 동시 전환, 필드·손패 원본의 숨김/복원, 유효 셀/보드 밖 이동, 비용·체력 등 무관한 표시 보존을 관찰한다. 내부 호출 횟수보다 실제 표시·원본 복구를 검사한다.
- [x] **D3.6 교차 검증:** 기존 이동 Ghost가 배치로 돌아갈 때 외형·풀을 재사용하는 회귀와 중복 Attach 보존을 선택한다. 실제 Display를 Release한 뒤 같은 root에 다시 Attach하여 옛 배치 호출이 새 연결의 Ghost/원본을 바꾸지 않는지 기존 fixture를 확장해 확인한다. 기존 `AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`의 Drag는 이동 경로 입력이므로 배치 회귀의 대체 근거로 삼지 않는다.
- [x] **D3.7 새 에이전트 검사·정지:** 배치 구현의 응집성, 공유 Ghost 소유권, late call 방어, 좌표 어댑터 경계를 검사한다.

**완료 조건:** 배치 고스트·격자·원본 표시 정책은 배치 객체에서 수정할 수 있고, Display에는 공개 방어·필요 좌표 어댑터·공통 수명만 남는다.

## 8. SD-D4 — 공격 표시 구현 분리

수정 대상: 새 `CombatAttackFeedback.cs`와 `.meta`, Display, Factory. 검증은 새 `CombatAttackFeedbackTests.cs`와 `.meta`, 기존 `WallAttackInteractionTests`·비용/HP/포인터 fixture를 사용한다.

- [x] **D4.1 객체 경계:** FieldView·FieldUnitPresenter를 받는 `internal sealed CombatAttackFeedback`을 만든다. 실제 공격 애니메이션 수명인 `CombatAttackPresenter`와 합치지 않는다. 입력 요청·응답 판단은 AttackTargetingInteraction에 유지한다.
- [x] **D4.2 구현 이동:** Show/ClearRangeCells, Show/ClearAffectedCells, ShowAttackCostPreview, Show/ClearAttackHealthPreview, Show/ClearAttackBlockers를 모은다. `_blockedWallCells` 임시 목록도 이동한다. 공격 비용·HP 대상은 계속 Units가 소유하고, 벽 칸 중복 처리는 FieldHighlightState의 기존 의미를 따른다.
- [x] **D4.3 공유 경계:** Display의 일반 Hover와 복귀 버튼은 옮기지 않는다. 이동과 공유하는 Range 층 및 비용 슬롯의 교체·정리 계약을 유지한다. 새 `ClearAll`·Release를 추가해 기존 정리 순서를 바꾸지 않는다. 실제 화면 상태를 보관하는 별도 캐시를 만들지 않는다.
- [x] **D4.4 기능 검증:** Field/Units 중심 fixture에서 접촉별 벽 칸 합침·빈 결과·층 우선순위/복원과 영향 칸 정리 시 비용만 지워지는 것을 확인한다. `WallAttackInteractionTests` 실제 씬 테스트의 세부 표시 검사는 옮길 수 있으나 입력 응답 판별·Clear→Install·teardown 연결 검증은 기존 위치에 남긴다.
- [x] **D4.5 통합 검증:** 공격→이동 비용 교체, ClearAffectedCells의 HP 보존, 오래된 이동 정리가 새 공격 Range를 지우지 않음, Release 뒤 늦은 공격 호출이 새 연결을 바꾸지 않음을 확인한다. GridOutline·CellPrefab 양쪽 벽 표시를 검사한다.
- [x] **D4.6 새 에이전트 검사·정지:** 입력 규칙·애니메이션과 미리보기 표시의 경계, 임시 목록과 SSOT 구별, 공유 비용/Range·late call 계약을 검사한다.

**완료 조건:** 공격 표시 표현을 배치·이동 구현과 분리해 수정할 수 있고, 공용 슬롯·층·Facade 방어는 유지된다.

## 9. 검증 선택과 SD-D5 — 통합 인수

검증은 각 Phase의 영향 범위로 선택한다. 아래 목록은 모든 Phase에서 전체 재실행하라는 뜻이 아니다. 이미 같은 코드에서 통과한 증거는 재사용하고, 코드 변화·새 실패·미검증 항목 때문에 필요한 경우만 넓힌다.

| Phase | 핵심 기존 회귀·검증 |
|---|---|
| D1 | `CombatDisplayReuseTests.AuthoredCombatScene_DuplicateAttach_PreservesGhostInputAndOwnedResources`(두 경우), `CombatSceneEventPresenterTests`의 FirstInstall/BuildFailure/ClearDuringMovement/Reinstall 회귀, `CombatViewSystemTests.InstallFailure_Propagates_WithoutBindingInput`와 같은 Display 재생 Attach, `CombatMortarAssetTests`의 실제 가시밭 소비·소멸·재설치·소유 clone 반환 |
| D2 | 좁은 초안 표시 검증, `CombatMovementPlanningDisplayTests`, `CombatMovementRangeDisplayTests`, `CombatPathInputCleanupTests.ReturnCleanupFailure_StillClearsDraft_AndIgnoresLateRelease`, 포인터 fixture의 공유 비용·ClearDraft·해제 뒤 초안 및 관련 재설치 회귀 |
| D3 | 좁은 배치 표시 검증, `GhostMovesContinuouslyAndKeepsOneRental_HidesUiAndCollider_AndResetsForPlacement`, 실제 Display 중복 Attach·재연결/늦은 배치 호출 |
| D4 | 좁은 공격 표시 검증, `WallAttackInteractionTests` 관련 표시·늦은 응답·teardown, `AttackHealthPreviewDisplayTests`, 포인터 fixture의 비용/HP 독립·늦은 공격 호출, 이동 Range의 새 공격 보존 |
| D5 | 변경 후 아직 검증되지 않은 공유 계약·조립 경로와 아래 실제 재생 시나리오. 기존 성공을 이유 없이 반복하지 않음 |

- 새 `.cs`가 Unity csproj에 없을 때는 기존 `CustomAfterMicrosoftCommonTargets` 방식으로 스크래치 빌드를 확인한다. 생성된 csproj를 수작업 원본으로 만들지 않는다.
- MSBuild/관리 실행은 Unity 네이티브 객체·씬 회귀를 대신하지 않는다. 에디터 컴파일·관련 EditMode 결과와 실행 환경을 남긴다. DB 값은 바꾸지 않으므로 `DataBaseAssetTests` 전체를 새 의무로 추가하지 않는다.

**D5 Tasks**

- [x] **D5.1 누락 확인:** D0 기준선부터 D4까지 코드·검토 완료와 Unity 대기를 대조한다. 새 실패·미해결 검토 지적이 없고 필요한 검증 증거가 갖춰졌을 때만 전체 완료로 쓴다.
- [x] **D5.2 실제 재생:** GridOutline·CellPrefab에서 배치 drag/취소/원본 복원 → 이동 초안·비용·취소/복귀 → 공격 영향·차단 벽 → 사건 종료 후 최종 표시 → 전투 이탈/재진입을 확인한다. RecordInputOnly 개발 Layer와 Live 포기/행동 버튼은 기존 모드 조건을 유지하는지 관련 연결만 확인한다. 새 Core 계산이나 입력 시나리오는 추가하지 않는다.
- [x] **D5.3 변경 지도:** 이동 끝점·배치 원본 숨김·공격 벽 표현·생성 인자 변경 네 시나리오의 `고칠 파일 / 읽을 계약 / 검증 묶음`을 작성한다. 해당 기능을 고치려고 무관한 기능의 구현을 반드시 열어야 하는 의존이 남았는지 점검한다. 이를 입증하려고 가상 기능을 실제 코드에 추가하지 않는다.
- [x] **D5.4 잔여 참조:** Display의 각 참조가 공개 전달·공통 수명·최종 표시 중 어디에 필요한지 확인한다. 생성자 인자 수 감소만을 위해 별도 관리자나 가방을 만들지 않는다. 기능 구현·비용 상태·Mapper를 다시 Display로 모으지 않는다.
- [x] **D5.5 Refresh 평가:** 손패 가시성·활성 행위자 강조를 기존 Presenter의 좁은 입력 또는 응집된 표시 책임으로 더 모을 실익을 문서로 평가한다. Refresh 전체 이전·Audio 변경·새 수명 객체는 이 Phase에서 구현하지 않는다. 필요하면 별도 설계·Task 후보로 남긴다.
- [x] **D5.6 인계:** 완료 범위, 남은 Unity 검증, §3의 의도적으로 보존한 실패 위험, 후속 후보와 각 Phase 검토 결과를 기록한다. A/B/C′ 원본의 검증 상태는 실제 증거가 생겼을 때만 갱신한다.
- [x] **D5.7 새 에이전트 검사·정지:** 최종 구조가 기능 변경 독립성을 실제로 개선했는지, 조립만 이동한 상태나 다른 거대 클래스로 끝나지 않았는지, 누락 검증을 완료로 표시하지 않았는지 검사한다.

**전체 완료 조건:** D1 조립 경계와 D2~D4 기능 구현 경계가 마련되고, 공유 수명·슬롯·층 계약이 필요한 검증으로 지켜지며, 각 Phase의 새 에이전트 검토와 D5 인수가 완료된다. Refresh의 추가 분리 후보나 명시적으로 제외한 실패 회복 개선까지 완료했다고 주장하지 않는다.

## 10. 실행 기록 형식

각 Phase 종료 때 이 절에 한 번씩 기록한다. 기능 워크시트는 `docs/superpowers/viewDocs/2026-10-14-combat-scene-display-sd-dN-worksheet.md`로 연결하고 Task 상태는 이 문서만 갱신한다.

| 기록 | 필요한 내용 |
|---|---|
| 기준 | HEAD, 작업 전 변경, 작업 범위 |
| 결과 | 바뀐 구현·이유, 계획과 차이 여부 |
| 검증 | 실행 환경·선택한 검증·결과·새 실패·미검증 |
| 새 에이전트 검사 | 새 검토자, 검토 시점·범위, 지적과 수정·재검사 결과 |
| 판정 | Phase 코드/문서·검토·Unity 상태를 구분, 다음 범위와 정지 |

### 10.1 계획 작성 (2026-10-14)

- 사용자가 합의한 조립·기능 분리 방향으로 Phase별 Task 계획을 요청했고 매 Phase 종료 후 새 에이전트 검사를 포함하도록 지시했다.
- 설계 원본의 순서를 D0 준비 → D1 Factory → D2 이동 → D3 배치 → D4 공격 → D5 통합 인수로 구체화했다. 알려진 오류 회복 정책은 동작 보존을 위해 별도 후속 범위로 명시했다.
- 이번 작업은 계획 문서 작성이며 D0~D5 실행, 코드 변경, 빌드·테스트는 하지 않았다.
- 조립·콜백·실패 경계와 기능별 Task·회귀 범위를 두 읽기 전용 에이전트가 나눠 대조했다. 가시밭 콜백의 생성 순서, target-typed 생성 호출부, 범위 전용 생성자, 기존 Drag 회귀가 이동 입력이라는 구분을 반영했다.
- 계획 작성 중 시작된 별도 커서 스냅 감도 변경은 읽어서 동시 작업으로 기록했으며 수정하지 않았다. 다음 착수 때 현재 파일을 다시 기준으로 삼도록 D0에 반영했다.
- 계획 작성 뒤 새 에이전트 `composition_plan_final_review`가 독립 검사했다. 원 구조 설계 D4의 Factory 미도입 결정과 후속 방향의 대체 범위를 명시하고, D2.5의 정리 검증을 `ClearDraft 뒤`로 특정했다. 수정분 재검사에서 두 지적 해소·추가 지적 없음으로 확인됐다. 이는 계획 검사이며 D0 실행·완료 검사로 세지 않는다.
### 10.2 SD-D0 착수 기준·조립 계약 (2026-10-15)

- **착수:** 사용자의 "SD-D0 진행해" 지시로 조사·명세·기존 검증만 수행했다. 현재 HEAD는 `6809cefab8a8e775cb6ce958757aa26b585eb123`이고 시작 시 tracked 변경·untracked 파일은 없었다. 선행 A/B/C′와 커서 스냅 감도 변경은 현재 코드에 반영돼 있으며, 커밋을 Unity 통과 근거로 세지 않았다.
- **산출물:** [D0 조립·소유권 명세](../specs/2026-10-15-combat-scene-display-composition-contract.md)에 참조 24개의 취득·사용·공유·정리 소유자, D1 주입 생성자 서명과 Factory 실행 순서, 가시밭 콜백 전제, 인수 전/후 실패 경계, 실제 생성 호출부를 확정했다. `_fieldBound`·공개 해제 방어는 Display에 남고 커서 값은 실시간 조회한다. 선택적 WorldCamera·Audio·Thorn의 기존 의미를 보존한다. D1 구현은 하지 않았다.
- **영향 조사:** 운영 생성 1곳·실제 fixture 6파일 8곳(target-typed new 포함), MovementPlanning 전체 생성 2곳·Field-only 1곳을 확인했다. `sdd0_dependency_audit`가 읽기 전용으로 의존성·콜백·호출부를 별도 대조했다. 이 조사 참여자는 아래 새 Phase 검사자로 세지 않는다.
- **현재 빌드:** VS 2022 MSBuild로 `ProjectL.Tests.Framework.csproj`와 참조 어셈블리를 새 스크래치에 빌드해 성공했다. 오류 0·기존 경고 16·추가 경고 0. C′ 최종 로그와 파일·경고 코드·대상 심볼을 비교해 같은 16건임을 확인했다. `Temp/CombatSceneDisplaySDD0/Baseline.targets`는 중간 출력만 분리했고 csproj·Compile 항목을 변경하지 않았다.
- **현재 관리 회귀:** 새 D0 테스트 DLL에 기존 SDB/SDC 실행기를 사용해 Unity Mono에서 재생 4/4·FieldHighlightState 4/4, 총 **8/8 통과**했다. 실행 목록·로그 위치는 [D0 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d0-worksheet.md)에 있다. 실제 Display·HUD·씬·네이티브 자원을 검증한 Unity Test Runner 결과가 아니다.
- **Unity 대기:** Unity 6000.0.73f1 프로세스·프로젝트 잠금·Editor.log의 현재 프로젝트 경로를 확인했다. 기존 세션을 종료하거나 두 번째 에디터를 실행하지 않았다. A/B/C′의 미검증 상태와 D1이 의존하는 조립·재설치·해제 회귀를 워크시트에 대기로 남겼다. D0 기준 Unity 실행을 확보하지 못한 한계는 후속 Phase에도 유지한다.
- **범위:** 계획 기록·명세·워크시트 3개 문서만 변경했다. 런타임·테스트·씬·DB·다른 작업자 영역은 수정하지 않았고 커밋·push하지 않았다. 조립 도중 부분 Configure와 `_display` 인수 전 예외의 해제 누락 위험은 §3대로 별도 후속이며 이번에 해결했다고 처리하지 않는다.
- **새 Phase 검사:** 산출물 작성·현재 검증 뒤 새 에이전트 `sdd0_phase_review`를 생성해 독립 읽기 전용 검사를 받았다. 24개 참조·생성 순서·같은 인스턴스·선택값·실시간 설정·콜백·수명 인수·호출부를 실제 코드와 대조했고, SOLID 5원칙·SSOT·JMH/FW·기능 변경 독립성 및 검증 증거 구분을 확인했다. 조치할 지적 없음(P0~P3 없음)으로 종료하여 수정·재검사 항목은 없다. 검토자는 파일 수정·빌드·테스트·Unity 실행을 하지 않았다. 최종 문서 링크·UTF-8/LF·공백 검사도 통과했다.
- **판정·정지:** **D0 조사·명세·가능한 기준선 검증·새 검사 완료 / Unity 기준선 대기.** D1의 동작 보존 추출을 막는 명세 불일치나 실행된 검증의 실패는 없어, 별도 사용자 지시 뒤 **D1 착수 가능**으로 판단한다. Unity 미실행은 통과로 취급하지 않으며 후속 Phase에도 대기를 유지한다. 실패가 확인돼 전후 비교를 막으면 원인·해결 범위를 먼저 정한다. 다음 범위는 D1 Factory 추출이며 이번에는 D0에서 멈춘다.

### 10.3 SD-D1 Factory 조립 추출 (2026-10-15)

- **착수:** 사용자의 "SD-D1 진행해" 지시에 따라 D0 명세와 계획 §5 범위로 진행했다. HEAD는 `6809cefab8a8e775cb6ce958757aa26b585eb123`. 시작 시 미커밋 변경은 D0의 계획·명세·워크시트 3개이며 운영·테스트 코드는 D0와 같았다. 그 문서와 선행 A/B/C′·커서 변경을 인수했다.
- **구현:** `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`와 `.meta`를 만들고 기존 root/tables/services 검증·생성·Configure·콜백 연결을 순서대로 옮겼다. Display 생성자는 준비된 24개 참조와 `_fieldBound` 정의만 보관한다. ViewSystem은 사전 검사 뒤 Factory를 호출하며 기존 `_display` 인수·입력 연결·root 구독과 실제 Display 재생 전달 순서를 유지한다. 묶음·Playback 조립 경로 설명도 맞췄다.
- **콜백·수명:** 가시밭은 null 지역 참조를 캡처하고 EventPresenter의 step 검증 뒤 한 번 할당한다. 같은 객체를 Display·EventPlayback이 사용한다. Display ConsumeThorn 전달 메서드만 제거하고 Clear/Release 전달·공개 해제 방어·Install/Refresh/Clear/Release는 보존했다. 동적 스냅 값·선택적 WorldCamera/Audio/Thorn도 보존한다.
- **fixture:** 구현 보조 `sdd1_fixture_migration`이 지정된 실제 fixture 6파일 8곳을 Factory로 전환했다. 새 행동 회귀 `CombatMortarAssetTests.Factory_InvalidMovementStep_RejectsBeforeCreatingThornStorage`를 추가해 step=0 실패가 가시밭 소유 루트 생성보다 앞서는 것을 검사한다. 같은 가시밭 연결은 기존 실제 소비/취소·재설치·clone 반환 회귀가 판별하므로 중복 테스트를 추가하지 않았다. 기존 사용자 씬 보호·teardown은 유지했다.
- **빌드:** VS 2022 MSBuild로 `ProjectL.Tests.Framework.csproj` 및 참조 어셈블리 빌드 성공. 오류 0·기존 경고 16·추가 경고 0(D0 경고 식별자와 대조). ignored `Temp/CombatSceneDisplaySDD1/NewFiles.targets`로 중간 산출물을 분리하고 새 Factory만 Compile에 보충했다. csproj는 수정하지 않았다. 로그는 같은 폴더 `build.log`다.
- **관리 회귀:** 기존 SDB 실행기를 Unity 6000.0.73f1 Mono에서 현재 DLL에 적용해 재생 관리 회귀 **4/4 통과**. 로그는 `playback-managed-regression.log`. D0의 격자 상태 관리 4/4는 변경 없는 기존 증거로 인수하며 이번에 반복하지 않았다. 관리 실행은 실제 Factory·씬·네이티브 자원 회귀를 대신하지 않는다.
- **Unity 대기·D1.6 부분 완료:** 현재 Unity 프로세스·프로젝트 잠금이 유지돼 세션 종료·중복 실행 없이 대기로 남겼다. 어셈블리 컴파일은 통과했지만 계획의 실제 조립·수명 선택 10케이스와 새 회귀 1케이스는 Unity 실행 대기다. D0 수정 전 Unity 기준선도 미확보 상태를 유지한다. 실행 순서는 [D1 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d1-worksheet.md)에 있다.
- **공유·범위:** [Factory 경로 변경 공유 메모](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md)를 작성했으며 미전달이다. 새 서비스/Builder·Framework/Architecture·Core/Buffer·씬/프리팹/DB 변경, 부분 Configure·인수 전 예외의 롤백 보완은 없다. 계획에서 벗어난 결정·D2 구현·커밋·push는 하지 않았다.
- **새 Phase 검사:** 코드·검증·문서 준비 뒤 새 에이전트 `sdd1_phase_review`를 생성해 독립 읽기 전용 검사를 받았다. 실제 전후 코드·새 Factory·fixture·로그를 대조해 생성 순서·24개 참조·가시밭 콜백·선택값·실시간 설정·수명·Attach 인수 경계·SOLID 5원칙/SSOT/FW/기능 변경 독립성을 확인했다. 조치할 지적 없음(P0~P3 없음)으로 종료하여 수정·재검사 항목은 없다. 검토자는 파일 변경·빌드·테스트·Unity 실행을 하지 않았다. 부모도 실행 메서드 본문(ConsumeThorn 제거 제외)·정리 목록 불변, 직접 생성 단일 경로, GUID 중복 없음, 문서 링크·UTF-8/LF·공백과 변경 범위를 확인했다.
- **판정·정지:** **D1 코드·MSBuild·관리 회귀·새 검사 완료 / Unity 확인 대기.** D1.6은 Unity 11케이스를 마칠 때까지 부분 완료로 유지한다. 생성·Configure 변경 지점은 Factory로 분리됐으며 전체 기능 분리 완료로 쓰지 않는다. 이번에는 D1에서 보고하고 멈춘다. D2 이동 초안 구현 집중은 별도 사용자 지시 뒤 진행하며, 새 실패가 발견되면 다음 변경 전에 원인·해결 범위를 정한다.

### 10.4 SD-D2 이동 초안 구현 집중 (2026-10-15)

- **착수:** 사용자의 "SD-D2 진행해, 끝나면 똑같이 새 에이전트로 검사해" 지시에 따라 진행했다. HEAD `6809cefab8a8e775cb6ce958757aa26b585eb123`와 D0·D1의 미커밋 변경 18파일을 포함했다. 시작 시 대상 7파일의 디스크 사본과 선행 변경 해시를 `Temp/CombatSceneDisplaySDD2/`에 남겨 HEAD와 이번 Phase 차이를 구별했다.
- **구현:** ShowDraft/ClearDraft/SetPathPlanning의 초안 층·끝점·HUD·공용 비용 의도를 기존 CombatMovementPlanningPresenter로 옮겼다. 전체 생성자 `(field, units, ghost, config, hud)`의 HUD는 필수이며 Factory와 기존 전체 fixture에 같은 HUD를 전달한다. 범위 전용 Field-only 생성자는 그대로다. Display는 기존 공개 해제 방어 뒤 위임한다.
- **수명·경계:** 기존 `_released`를 재사용하며 `_editing` 전제나 비용 대상·초안 캐시를 추가하지 않았다. Clear/EndEditing/ClearRange/Release와 화면→월드 변환은 그대로다. `_fieldBound`에 초안 정리를 합치지 않았고 비용 원본은 Units, 층 원본은 FieldHighlightState에 둔다. 조사 보조 `sdd2_boundary_audit`가 독립 정리 순서·필드 전용 경로·기존 회귀 및 관리 실행 가능성을 읽기 전용으로 대조했다.
- **테스트 코드·D2.5 부분 완료:** 구현 보조 `sdd2_movement_tests`가 CombatMovementDraftDisplayTests와 `.meta`를 추가했다(3메서드·5케이스). 전체 Combat 씬·Catalog·Core 실행 없이 실제 Field/Hud/Units의 후보/끝점 색·응답별 HUD/비용·편집 전 표시·ClearDraft 뒤 Range/HP/계획 상태를 관찰한다. PointerMapping의 기존 상세 1건과 전용 도우미는 옮겨 중복 제거했고 공용 비용 교체·HP·늦은 호출 통합 회귀는 유지했다. 테스트 어셈블리 컴파일은 통과했지만 새 5케이스의 Unity 실행은 대기다.
- **빌드:** 첫 빌드의 MovementPath using 누락(CS0246)을 이동 Presenter의 Contracts.Combat using 추가로 수정했다. 수정 뒤 Systems·EditMode 및 참조 어셈블리 MSBuild 성공, 오류 0·추가 경고 0. 최종 증분 로그의 기존 경고 15건과 첫 빌드의 Core 경고 1건을 합친 식별자 16개는 D1과 같다. `NewFiles.targets`는 중간 출력과 새 Factory/테스트 Compile 보충만 담당하며 csproj·Core 파일은 수정하지 않았다. 로그는 `build-initial.log`, `build.log`다.
- **관리·D2.6 부분 완료:** `CombatPathInputCleanupTests.ReturnCleanupFailure_StillClearsDraft_AndIgnoresLateRelease`를 Unity Mono에서 D1 DLL과 새 D2 DLL에 각각 직접 호출해 **수정 전·후 1/1 통과**했다. 기존 정리 예외 뒤 초안·입력 해제와 늦은 입력 차단 근거이며 실제 표시 테스트는 아니다. 로그는 `baseline-managed-regression.log`·`final-managed-regression.log`다. 변경과 무관한 사건 전체를 다시 실행하지 않았다.
- **Unity 대기:** 현재 Unity 프로세스·프로젝트 잠금 유지로 세션 종료·중복 실행 없이 대기로 남겼다. 좁은 초안 5케이스와 기존 Planning 18·Range 6·공유 비용/HP/늦은 초안 3·재설치 1, **총 33케이스**가 Unity 실행 대기다. D2.5·D2.6은 전체 검증 완료로 체크하지 않는다. 선행 A/B/C′·D0·D1의 검증 대기도 유지한다. [D2 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d2-worksheet.md)에 실행 순서·구체 선택을 남겼다.
- **범위·효과:** 운영 3파일, 기존 테스트 2파일, 새 테스트와 meta, 계획·워크시트를 수정했다. RangeDisplayTests는 무수정이다. D2 대상 밖의 선행 변경 14파일은 시작 해시와 같았다. 이동 표현의 수정은 이동 Presenter와 좁은 fixture에서 할 수 있고 공유 슬롯·공개 방어는 통합 회귀로 유지한다. D3·D4 구현, Core/Buffer/Framework/FieldCamera/Architecture/씬/DB 변경·커밋·push는 하지 않았다.
- **새 Phase 검사:** 코드·검증·문서 준비 뒤 새 에이전트 `sdd2_phase_review`를 생성해 독립 읽기 전용 검사를 받았다. D1 검사자와 이번 조사·구현 보조를 재사용하지 않았다. D2 전 디스크 사본·현재 코드·fixture·로그를 직접 대조하여 세 메서드의 조건·호출 순서, 독립 정리·해제 방어·Field-only·좌표 변환 경계, 비용/HP/Range 소유권과 SOLID 5원칙·SSOT·JMH/FW·기능 변경 독립성을 확인했다. 조치할 지적 없음(P0~P3 없음)으로 종료해 수정·재검사 항목은 없다. 검토자는 파일 변경·빌드·테스트·Unity 실행을 하지 않았다. 부모의 최종 UTF-8/LF·문서 링크·공백 검사도 통과했다. TMP·Renderer·URP 데칼의 네이티브 실행과 실제 GridOutline 렌더링 성공은 정적 검토로 확정하지 않는다.
- **판정·정지:** **D2 코드·MSBuild·관리 회귀·새 검사 완료 / Unity 확인 대기.** D2.5·D2.6은 Unity 33케이스를 마칠 때까지 부분 완료로 유지한다. 계획에서 벗어난 결정이나 조치할 검사 지적은 없다. 이동 초안 구현은 기존 Presenter로 모였으며 공통 수명·공유 상태 계약은 유지한다. 이번에는 D2에서 보고하고 멈춘다. 다음 D3 배치 표시 구현 분리는 별도 사용자 지시 뒤 진행한다.

### 10.5 SD-D3 배치 표시 구현 분리 (2026-10-15)

- **착수·순서:** 사용자의 "SD-D3, SD-D4 차례대로 진행해줘" 지시로 두 Phase를 순차 수행한다. D3 작업물 준비 → 새 검사·지적 처리 → 보고 뒤 D4로 넘어간다. D5는 미착수다. HEAD는 `6809cefab8a8e775cb6ce958757aa26b585eb123`; 미커밋 D0~D2 변경 23파일을 `Temp/CombatSceneDisplaySDD3/baseline/`과 해시 목록에 보관했다.
- **구현:** 새 `CombatPlacementFeedback`과 meta를 추가했다. Field/Ghost/Units/Hand만 주입하여 Show/HideGhost의 격자 전환, Hide/ShowOriginal의 Units→Hand 순서, SetGhostPlaceable, 현재 Mapper를 사용하는 셀 이동과 월드 위치 적용을 모았다. Display는 기존 해제 방어와 화면→월드 어댑터를 유지하고 Factory가 같은 네 View를 조립한다. 별도 풀·숨김 대상·해제 플래그·Mapper 캐시·최종 Release는 없다. Install/Clear/Release와 `_fieldBound`는 그대로다.
- **구현 보조·테스트:** `sdd3_contract_audit`는 계약을 읽기 전용으로 대조했고 `sdd3_tests`는 좁은 fixture 6케이스(3메서드×2모드)와 실제 같은 root 재연결 후 늦은 배치 7종 호출 무효 1케이스를 작성했다. 새 fixture는 실제 Ghost 색/위치·격자 활성·필드/손패 원본 복원·다른 유닛 비용/HP 보존을 관찰한다. 숨긴 유닛 자신의 OnDisable HP 정리라는 기존 계약은 바꾸지 않는다.
- **빌드:** 첫 빌드에서 테스트의 URP 직접 타입 참조가 어셈블리에 없어 CS0234가 발생했다. 기존 FieldOutlineRendererTests 방식처럼 생성된 `FieldGridDecal` 오브젝트의 활성 상태를 관찰하도록 고쳤다. 어셈블리 참조·운영 코드는 이 오류 때문에 넓히지 않았다. 수정 뒤 Systems·EditMode 및 참조 MSBuild 성공, 오류 0·추가 경고 0. 최종 증분 로그 경고 8건과 최초 로그를 합친 경고 식별자 16개는 D1과 동일하다. `build-initial.log`·`build.log`·`NewFiles.targets`를 스크래치에 보관했다. csproj는 수정하지 않았다.
- **검증 대기·D3.5/.6 부분 완료:** 신규 native 7 + 기존 이동 Ghost 전환 2 + 중복 Attach 2 = **Unity 11케이스 대기**다. 현재 Unity 세션을 유지했고 Test Runner를 실행하지 않았다. 변경 없는 FakeFeedback 입력 관리 테스트는 새 배치 표시를 거치지 않으므로 반복하지 않았다. [D3 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d3-worksheet.md)에 정확한 선택·관측·한계를 기록했다. 선행 Unity 대기는 유지한다.
- **범위:** 새 운영 1+meta, Display·Factory, 새 fixture 1+meta, PointerMappingTests, 계획·워크시트다. 대상 밖 선행 변경 19파일의 시작 해시가 동일하다. Core/Buffer/Framework/Architecture/FieldCamera·씬/프리팹/DB·입력 정책은 변경하지 않았으며 커밋·push하지 않았다. D1의 부분 Configure·인수 전 실패 복구 한계는 유지한다.
- **새 Phase 검사:** 작업물·빌드·문서 준비 뒤 새 `sdd3_phase_review`를 생성하여 독립 읽기 전용 검사를 받았다. 공개 해제 방어·현재 Mapper·같은 Ghost·정리 순서·원본 SSOT·좌표 경계, SOLID 5원칙·FW·기능 변경 독립성과 fixture 전제/정리/HP 기대를 대조했다. 조치할 지적 없음(P0~P3 없음)으로 수정·재검사 항목은 없다. 검토자는 파일 변경·빌드·테스트·Unity 실행을 하지 않았다. 부모의 UTF-8/LF·공백·GUID 중복 검사도 통과했다.
- **판정·후속:** **D3 코드·MSBuild·새 검사 완료 / Unity 11케이스 대기.** D3.5/.6은 Unity 실행 전까지 부분 완료다. D3 결과를 사용자에게 보고했고, 이번 순차 진행 지시에 따라 D4를 시작한다. D3 계획에서 벗어난 결정은 없다.

### 10.6 SD-D4 공격 표시 구현 분리 (2026-10-15)

- **착수:** 같은 사용자 순차 진행 지시에 따라 D3 새 검사 지적 없음·결과 보고 뒤 착수했다. HEAD `6809cefab8a8e775cb6ce958757aa26b585eb123`, D0~D3 미커밋 변경 28파일을 `Temp/CombatSceneDisplaySDD4/baseline/`과 해시 목록에 보관했다. D4 경계·검증 준비는 `sdd4_contract_audit`가 읽기 전용으로 조사했다.
- **구현:** 새 `CombatAttackFeedback`과 meta를 추가해 Field/Units만 주입했다. 공격 Range·Affected·비용·HP·벽의 9개 API와 `_blockedWallCells` 작업 목록을 이전했다. Display는 해제 방어 후 위임하며 일반 Hover·복귀·공통 수명은 유지한다. Factory는 기존 같은 Field/Units를 전달한다. ClearAffected의 층→비용 순서, HP 독립, null 목록의 예외 순서와 기존 중복 처리를 보존한다. 새 상태 캐시·해제 플래그·ClearAll·Release·서비스 등록은 없다.
- **테스트·D4.4 부분 완료:** `sdd4_tests`가 좁은 CombatAttackFeedbackTests 6케이스(3메서드×2모드)를 작성했다. 벽 접촉 합침·교체·빈 결과·실제 색 복원, 영향층 정리와 HP/비용 독립을 관찰한다. 기존 실제 씬의 상세 색 중복을 이전하고 `AuthoredScene_RealBlockerFeedbackClearsOnReinstallAndTeardown`에 실제 contact→Display, Clear→Install, 새 벽 표시와 handle 최종 해제를 남겼다. 기존 Wall 관리 12케이스 본문은 동일하다.
- **통합 회귀 보완:** PointerMapping의 기존 late attack 회귀를 9개 API와 null/default 4호출로 확장했다. 각각의 호출 직후 새 Display의 층 전체·두 행위자 비용·HP가 보존되는지 확인해 Show/Clear 오류가 서로 상쇄되지 않게 했다. D3의 late placement 회귀와 다른 기존 통합 회귀는 유지했다.
- **빌드:** 새 Factory·D2/D3/D4 신규 파일을 `NewFiles.targets`로 미포함일 때만 보충하고 Systems·EditMode 및 참조 어셈블리 MSBuild 성공. 오류 0·기존 경고 16·추가 경고 0이며 D1의 경고 식별자와 일치한다. 로그는 `build.log`다. csproj·어셈블리 참조·Core 파일은 수정하지 않았다.
- **관리 회귀·D4.5 부분 완료:** WallAttackInteractionTests의 실제 씬을 제외한 8메서드·12케이스를 Unity Mono에서 D3 DLL과 D4 DLL에 각각 실행하여 **변경 전·후 12/12 통과**했다. `baseline-managed-regression.log`, `final-managed-regression.log`, 전용 실행기를 스크래치에 남겼다. FakeFeedback/Core/강조 상태 검증이며 새 표시 객체나 Unity Test Runner 실행 근거가 아니다.
- **Unity 대기:** 새 6 + 기존 실제 Wall 씬 1·HP 표시 10·Pointer 비용/HP/late 3·오래된 이동 Range 2 = **22케이스 대기**다. [D4 실습 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d4-worksheet.md)에 구체 선택과 실행 자료를 기록했다. 현재 Unity 세션을 유지했으며 선행 A/B/C′·D0~D3 대기는 그대로다. D4.4/.5는 전체 검증 완료로 체크하지 않는다.
- **범위:** 새 운영 1+meta, Display·Factory, 새 fixture 1+meta, WallAttackInteractionTests·PointerMappingTests, 계획·워크시트다. 범위 밖 선행 변경 23파일의 시작 해시는 동일하다. Core/Buffer/Framework/Architecture/FieldCamera·씬/프리팹/DB·입력/애니메이션 정책과 D1의 알려진 실패 복구 한계는 바꾸지 않았다. 커밋·push하지 않았다.
- **새 Phase 검사:** D4 코드·검증·문서 준비 뒤 새 `sdd4_phase_review`를 생성하여 독립 읽기 전용 검사를 받았다. D3 검사자·D4 보조를 재사용하지 않았다. 작업 전 사본과 현재 코드·fixture·로그를 직접 대조하여 공격 9개 API·작업 목록·동일 Field/Units·공개 해제 방어·ClearAffected 순서·HP/Range 독립성, SOLID 5원칙·SSOT·FW/JMH·기능 변경 독립성을 확인했다. 새 fixture의 보드·HP/비용/색 기대와 정리, 씬 연결·재설치·호출별 late-call 회귀도 점검했다. 조치할 지적 없음(P0~P3 없음)으로 수정·재검사 항목은 없다. 검토자는 파일 변경·빌드·테스트·Unity 실행을 하지 않았다. 부모의 UTF-8/LF·공백·문서 링크·GUID 중복 검사도 통과했다.
- **판정·정지:** **D4 코드·MSBuild·관리 회귀·새 검사 완료 / Unity 22케이스 대기.** D4.4/.5는 Unity 실행 전까지 부분 완료다. D3 Unity 11케이스와 선행 대기도 유지한다. 계획에서 벗어난 결정이나 조치할 지적은 없다. 배치·공격 구현은 각각 해당 기능 객체에서 수정할 수 있고 공유 수명·표시 상태는 기존 소유자에 남는다. 이번에는 D4에서 보고하고 멈춘다. D5 통합 인수·Refresh 경계 평가는 별도 사용자 지시 뒤 진행한다.

### 10.7 SD-D5 통합 인수·변경 독립성 평가 (2026-10-15)

- **착수:** 사용자의 "SD-D5 진행해" 지시로 계획 §9 범위만 진행했다. HEAD `6809cefab8a8e775cb6ce958757aa26b585eb123`와 D0~D4 미커밋 변경 33파일을 인수하고 `Temp/CombatSceneDisplaySDD5/baseline/`·해시 목록에 보관했다. 런타임·테스트·씬·DB를 추가 변경하지 않았다.
- **조사 보조:** `sdd5_structure_audit`는 현재 코드의 변경 지도·26개 참조·Refresh·원칙을 읽기 전용으로 평가했다. `sdd5_validation_audit`는 Phase별 로그·테스트 선택·Unity 로그를 대조했다. 이 둘은 작업물 준비 뒤의 새 Phase 검사자를 대신하지 않는다.
- **검증 증거 인수:** 마지막 D4 MSBuild는 오류 0·기존 경고 16·추가 경고 0, Wall 관리 회귀는 변경 전·후 12/12다. D0~D3의 기존 성공과 새 검토 결과도 대조했다. 코드 변경이 없어 같은 빌드·관리 테스트를 반복하지 않았다. 현재 Editor.log에서 중간 오류 뒤 후속 컴파일·Mono reload 성공과 D4 파일 수정 뒤 생성된 Library DLL을 확인했지만 Test Runner 결과 XML/집계는 찾지 못했다. 이를 실제 게임 동작 성공 근거로 확대하지 않는다.
- **사용자 Unity 확인:** D4 이후 현재 미커밋 코드로 확인한 결과를 물었고 사용자는 "전체 확인함"이라고 답했다. 이어 EditMode 테스트와 GridOutline·CellPrefab 실제 전투 재생의 통과·실패·Ignore 여부를 확인하여 **"모두 통과, 실패·Ignore 없음"**이라는 명시 답변을 받았다. D1~D4 검증 및 두 표시 모드 실제 재생은 이 사용자 확인으로 인수한다. 에이전트 직접 실행·자동 69/69·XML 수집으로 기록하지 않는다. 실행 총수와 개별 단계 재생 로그는 미수집이다.
- **D5.1/.2:** D1 11+D2 33+D3 11+D4 22에서 중복 8을 뺀 고유 선택은 **12 fixture·69케이스**다. 정확한 메서드와 사용자 확인 범위는 [D5 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d5-worksheet.md)에 있다. 모드별 개발 Layer·포기·행동 버튼 연결은 현재 연결 코드와 관련 기존 EditMode 계약도 대조했다. D1.6·D2.5/.6·D3.5/.6·D4.4/.5의 현재 검증을 체크했고 당시 대기 기록은 이력으로 보존했다. 새 미해결 실패는 확인되지 않았다.
- **D5.3/.4:** [인수 평가](../specs/2026-10-15-combat-scene-display-composition-acceptance.md)에 이동 끝점·배치 원본·공격 벽·생성 인자 변경의 수정 파일/읽을 계약/선택 검증을 작성했다. Display의 26개 주입 참조와 `_fieldBound`·`_isReleased`를 전수 확인했다. 모두 공개 전달·공통 수명·최종 표시 또는 실시간 설정에 용도가 있다. 기능 표현의 내부 수정은 관련 기능·View 중심으로 가능하며 공유 슬롯·Ghost·Mapper·수명 변경의 교차 검증은 계속 필요하다.
- **D5.5:** Refresh의 손패 가시성은 기존 HandPresenter의 좁은 입력을 활용하는 후속 후보, 활성 행위자 강조는 actorId/characters→현재 점유칸 변환을 맡는 지속 강조 협력 객체 후보로 평가했다. 후자가 국소화 이득은 더 명확하지만 이번에는 평가만 했다. 상태가 생길 때까지 무조건 분리를 미루는 기준으로 되돌리지 않는다. 전체 Refresh 이전·Audio 변경·새 수명 객체·Parts 가방은 구현하지 않았다.
- **D5.6·선행 인수:** A/B/C′의 현재 Unity 상태는 사용자 전체 확인을 출처로 [선행 계획 §7.10](2026-10-14-combat-scene-display-followup-plan.md)에 갱신했다. 69개 선택만으로 선행 전체를 덮지 않으며 전체 EditMode 확인과 구분한다. A/C′의 수정 전 회귀·D0 당시 Unity 기준선은 소급 확보하지 않아 해당 역사적 증거 항목은 남겼다. 부분 Configure·Factory 반환 후 `_display` 인수 전 실패의 해제 누락은 기존 별도 후속 위험으로 유지한다.
- **원칙·범위:** SOLID 5원칙·SSOT·FW/JMH·기능 변경 독립성을 인수 평가 §5에 대조했다. 조립만 옮긴 상태나 다른 거대 클래스로의 이전으로 끝나지 않았으며 비용/HP/원본/Mapper/자원 소유권을 복제하지 않았다. 문서 5개(현재 계획, 선행 계획, 후속 설계, 새 인수 평가, 새 워크시트)만 변경했다. Core/Buffer/Framework/Architecture/FieldCamera·씬/프리팹/DB 변경, 커밋·push·외부 전달은 없다.
- **새 Phase 검사:** D5 산출물과 증거 정리 뒤 새 `sdd5_phase_review`를 생성하여 독립 읽기 전용 검사를 받았다. 조사 보조·이전 Phase 검사자를 재사용하지 않았다. 현재 코드의 26개 참조·네 변경 시나리오·공통 수명·SOLID 5원칙·SSOT·FW/JMH·기능 변경 독립성을 대조했고, 12 fixture·69개 고유 선택과 중복 계산, 사용자 확인/직접 실행/과거 기준선의 구분, 후속 범위를 확인했다. 조치할 지적 없음으로 수정·재검사 항목은 없다. 검토자는 파일 수정·빌드·테스트·Unity 실행을 하지 않았다. 작업 전 33파일 해시 대조에서 이번 계획 외 32파일이 동일했고 D5 런타임·테스트 추가 변경이 없음을 확인했다. 부모의 문서 UTF-8/LF·링크·공백 검사도 통과했다.
- **판정·정지:** **SD-D0~D5 계획 범위 완료.** 조립과 이동·배치·공격 구현의 변경 지점 분리, 현재 통합 Unity 결과의 사용자 확인 인수, 구조 평가·문서 인계·각 Phase 새 검사를 완료했다. Refresh 두 후보·Audio·실패 시 조립/인수 정리 개선은 후속 범위이며, 과거 수정 전 기준선의 증거 한계는 유지한다. 이번에는 D5 결과를 보고하고 멈추며 후속 구현을 자동으로 시작하지 않는다.
