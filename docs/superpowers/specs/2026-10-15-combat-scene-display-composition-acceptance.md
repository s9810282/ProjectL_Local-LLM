# CombatSceneDisplay 조립·기능 분리 인수 평가

- 작성: View 작업, 2026-10-15. SD-D5의 구조 평가·후속 판단이다.
- Task·실행·검사 상태 원본: [조립 계획 §9·§10.7](../plans/2026-10-14-combat-scene-display-composition-plan.md).
- 설계 원본: [후속 설계 §5.3·§5.5·§6](2026-10-14-combat-scene-display-followup-design.md). 원칙은 [인계 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md).
- 기준: HEAD `6809cefab8a8e775cb6ce958757aa26b585eb123`와 D0~D4 미커밋 변경 33파일을 포함한 디스크 코드. D5는 런타임·테스트·씬·DB를 변경하지 않는다.
- 검증 선택과 증거 구분: [D5 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d5-worksheet.md). 사용자 확인을 도구의 직접 실행이나 수정 전 기준선으로 바꾸어 기록하지 않는다.

## 1. 평가 결론과 범위

**조립과 이동·배치·공격 표시 구현의 변경 지점이 분리됐다.** 소비자는 단일 Display와 기존 좁은 계약을 계속 사용한다. 기존 API 안의 기능 표현 변경은 해당 기능과 관련 View·fixture를 중심으로 할 수 있으며, 다른 두 기능의 구현을 관성적으로 읽거나 바꿀 필요가 줄었다.

| 책임 | 현재 위치 | Display에 남는 역할 |
|---|---|---|
| 생성·Configure·구현 선택·콜백 연결 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory` | 준비된 객체 보관 |
| 이동 초안·끝점·비용·이동 HUD·격자 계획 표시 | 기존 `CombatMovementPlanningPresenter` | 공개 방어, 화면→월드 어댑터, 공통 정리 호출 |
| 배치 고스트·격자·원본 숨김/복원 | `CombatPlacementFeedback` | 공개 방어, 화면→월드 어댑터 |
| 공격 범위·영향·비용/HP 의도·벽 칸 변환 | `CombatAttackFeedback` | 공개 방어 |
| 개발 패널·포기/행동·집기·사건 재생 | 기존 Binding/Panel/PointerMapper/EventPlayback | 공개 전달과 수명 조정 |
| 설치·최종 표시 순서·Clear·Release | `CombatSceneDisplay` | 현재 공통 경계 유지 |

모든 기능을 다른 하나의 큰 클래스에 옮기거나 서비스 가방으로 감추지 않았다. 그렇다고 Display의 직접 구현이 모두 사라진 것은 아니다. Refresh의 손패 가시성 조건·활성 행위자 조회, Audio 호출과 공통 수명 순서는 남아 있다. 현재 주입 참조는 D1의 24개에서 배치·공격 객체를 더한 **26개**다. 성공 기준은 참조·줄 수 감소가 아니라 기능별 수정·이해·검증 범위의 개선이다.

## 2. 기능 변경 지도

아래는 기존 공개 API를 유지하는 변경의 기본 경로다. 공유 계약을 바꾸면 오른쪽의 교차 검증을 추가한다. 새 의존성이 기존 공급 계약에 없다면 Tables/Services/SceneRoot 등도 바뀔 수 있으므로 항상 두 파일만 바꾼다고 보장하지 않는다.

| 변경 시나리오 | 주로 고칠 파일 | 읽을 계약 | 기본 검증과 확장 조건 |
|---|---|---|---|
| 이동 끝점·유효/무효·미응답 표현 | `CombatMovementPlanningPresenter.ShowDraft/ClearDraft`; 색 자체는 Field 팔레트 | candidate 우선/없으면 Endpoint, 편집 시작 전 표시 허용, MovementDraft·Hover 독립, Units 공용 비용, ClearDraft의 Range/HP/계획 상태 보존 | `CombatMovementDraftDisplayTests` 5케이스. 비용 조건을 바꾸면 Pointer 비용 대상 교체·HP 보존, 수명을 바꾸면 늦은 호출·정리 실패·재설치 |
| 배치 원본 숨김·복원 | `CombatPlacementFeedback.HideOriginal/ShowOriginal`; 실제 숨김 방식은 Units/Hand | 같은 ID를 두 View에 전달, 숨김 대상은 각 View 소유, 자기 OnDisable의 HP 정리는 기존 계약, Ghost는 이동과 공유 | 좁은 `ChangingOriginalBetweenFieldAndHand_RestoresBoth_AndPreservesSharedFeedback` 두 모드. 공개 방어는 재연결 late placement, Ghost 자원을 바꾸면 이동→배치 재사용 |
| 공격 벽 표현 | `CombatAttackFeedback.ShowAttackBlockers/ClearAttackBlockers`; 색·우선순위는 Field 계층 | null 거절, 결과마다 목록 교체/빈 결과 정리, 중복·보드 밖은 Field 처리, List는 작업 버퍼 | 좁은 BlockerContacts/BlockerAndHoverPriority 두 모드. 응답 선택·취소 변경은 Wall 입력 회귀, 공개 방어는 재연결 late attack |
| 기능 생성 인자·의존성 변경 | Factory + 해당 기능 생성자 + 해당 fixture | 같은 Field/Units/Ghost, 기존 Configure 순서, 실시간 root 설정, Event step 검증보다 늦은 Thorn 자원 생성 | 해당 기능 + 실제 Factory 설치. 공유 참조·생성 순서를 바꾸면 중복 Attach·잘못된 step·설치 실패·재설치·반환 |

이전에는 표시 조건과 조립·수명 코드가 Display 한 파일에 모여 있었고 일부 세부 표시 검증도 전체 씬·Catalog를 준비했다. 현재는 이동/배치/공격 fixture를 따로 준비하며 씬 회귀는 조립·공유 수명·공개 방어를 담당한다. **공용 비용 슬롯, 층 우선순위, Ghost, Mapper, 취소·Release 변경의 교차 확인은 필요한 비용으로 남는다.**

## 3. Display 잔여 참조 전수 점검

[현재 Display](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs)의 26개 참조에는 모두 실행상 용도가 있다. 공통 수명은 Install의 Build·Bind도 포함하며, 서비스 참조를 가진다고 그 서비스 자체를 Shutdown한다는 뜻은 아니다.

| 참조 | 남는 이유 |
|---|---|
| `_field` | Build/Release, 현재 Mapper 연결, 정적·행위자 강조, 일반 Hover |
| `_movementPlanning` | 이동 API 전달, `_fieldBound` Clear와 최종 Release |
| `_placement` | 배치 API 전달 전용 |
| `_attack` | 공격 표시 9개 API 전달 전용 |
| `_units` | 최종 HUD 타임라인 입력, 공용 비용/HP 정리, 유닛 반환 |
| `_hand` | 최종 손패 Render와 반환 |
| `_hud` | 재화·완료 표시, 설치 때 완료창 숨김, Clear/Release 정리 |
| `_ghost` | **공통 수명 전용**: Clear의 Hide, 최종 Release. 이동·배치가 한 자원을 공유 |
| `_abandon` | 포기 표시·바인딩 전달, 최종 바인딩 해제·확인창 정리 |
| `_actions` | 행동·복귀 표시·바인딩 전달, Clear/Release의 숨김·해제 |
| `_cursorSnapSettings` | 실행 중 Inspector의 스냅 켜짐·감도를 읽는 빌린 씬 참조 |
| `_pointer` | 집기·화면/월드 어댑터, Build 뒤 현재 Mapper Bind·Unbind·Release |
| `_devLayerPanel` | 개발 패널 Show/Hide 전달과 최종 Release |
| `_fieldConfig` | Install의 Field.Build 설정 |
| `_colors` | Field.Build에 전달하는 빌린 색 서비스 |
| `_areaOutlines` | Field.Build에 전달하는 빌린 아웃라인 서비스 |
| `_pointLines` | Field.Build에 전달하는 빌린 선 서비스 |
| `_events` | 최종 사실 Refresh, Mapper Bind·Clear·Release |
| `_audio` | BGM 프리셋/재생·승리음 직접 호출. Audio 분리는 별도 범위 |
| `_props` | Install의 스테이지 프랍 표시, Clear/Release |
| `_objects` | 현재 Mapper를 사용하는 최종 Render, 설치 실패·Clear·Release |
| `_terrain` | 현재 Mapper를 사용하는 최종 Render, 설치 실패·Clear·Release |
| `_railway` | Install·Clear·Release의 레일 수명 |
| `_environmentEffects` | Mapper Bind·Clear·Release의 환경 연출 수명 |
| `_thorns` | 최종 사실 Sync·미연결 검증·Clear·Release; 선택적 프리팹 의미 유지 |
| `_eventPlayback` | 사건 전달, 공통 접촉 취소, Refresh 전 완료 전제 |

주입 대상이 아닌 `_fieldBound`는 Install 직전·실패·Clear의 정리 순서 원본이며, `_isReleased`는 Display 공개 입구의 씬 연결 수명 원본이다. 벽 칸 임시 목록은 공격 객체로 이전됐다. 기존 MovementPlanning의 해제 플래그는 그 객체의 기존 수명이며 새로 복제한 상태가 아니다.

현재 이 참조들을 없애기 위해 수명 관리자·`Parts`·서비스 조회·후설정 setter를 추가할 이유는 없다. 향후 수명 정책 자체가 별도 변경 책임이 되면 그 범위를 새로 설계한다. 참조 수만 줄이기 위해 기능 객체에 공통 반환을 중복 맡기지 않는다.

## 4. Refresh 후속 판단

Refresh는 **완료 전제 확인 → 현재 사실을 각 표시로 반영**하는 공통 호출 순서를 계속 소유한다. 전체 메서드를 또 다른 큰 Presenter로 이전하지 않는다. 아래 두 변환은 남은 독립 변경 이유로 인정하며 이번에는 구현하지 않는다.

| 후보 | 분리 실익 | 검토할 경계와 비용 | 판단 |
|---|---|---|---|
| 손패 가시성 | 국면→가시성 정책을 손패 기능에서 수정 가능 | 기존 HandPresenter 우선. 전체 CombatState 대신 StagePhase·종료 여부 같은 좁은 값. 현재 `Render(characters, bool visible)`의 단순한 정책 입력을 잃고 상태 타입 의존이 늘어남 | 후속 후보로 유지. 현 API도 이미 좁아 우선순위는 낮다. 조건 한 줄마다 새 정책 클래스를 만들지 않음 |
| 활성 행위자 강조 | Display의 actor 조회·점유칸 변환을 기능 안에서 수정·검증 가능 | actorId·characters만 받아 Field에 현재 점유칸을 적용하는 지속 강조 협력 객체 후보. 스냅샷 저장·규칙 계산 없이 호출마다 변환 | 손패보다 국소화 이득이 명확한 후속 후보. 상태가 생겨야만 분리할 수 있다는 조건은 두지 않되 별도 설계·Task 뒤 진행 |

FieldView에 전체 CombatState/Character 목록을 넘기거나 FieldUnitPresenter에 FieldView 의존을 추가하면 저수준 경계가 넓어진다. Audio 호출 위치·프리셋 정책과 새 수명 객체도 이번 평가를 근거로 함께 옮기지 않는다.

## 5. 원칙 점검

| 기준 | 현재 판단과 한계 |
|---|---|
| SSOT | Buffer 사실은 복제하지 않음. 비용·HP는 Units, 원본 숨김은 Units/Hand, 층·Mapper는 Field, 고스트 자원은 Ghost. 벽 List는 호출별 작업 버퍼 |
| SRP | Factory의 조립, Display의 공개 입구·공통 수명, 이동·배치·공격 표시 구현을 분리. Refresh 변환·Audio 잔여는 §4·별도 범위로 인정 |
| OCP | 기존 API 안의 표현 변경은 해당 기능 중심. 새 공개 API·공급 계약까지 항상 무수정으로 확장되는 것은 아님 |
| LSP | 해제 뒤 예외/무시/false, null 바인딩 해제 허용, 취소와 완료 구별 유지. 소비자는 내부 객체로 가드를 우회하지 않음 |
| ISP | 소비자의 기존 좁은 인터페이스 유지. 배치 4개·공격 2개 실제 의존성만 주입 |
| DIP | 전역 서비스 인터페이스 유지, 구체 협력 객체 선택은 Factory. 기능에 Tables/Services/SceneRoot 전체를 전파하지 않음 |
| FW/JMH | Builder·서비스 등록·입력/UI 경로를 늘리지 않음. 기존 Scope/Handle·풀 반환 소유자를 유지. 구체 기능 분리를 형식적 인터페이스·범용 등록 체계로 확대하지 않음 |
| 기능 변경 독립성 | §2 네 시나리오가 실제 기능/Factory와 좁은 검증에 모임. 조립만 이동하거나 다른 거대 클래스에 모은 상태와 구별됨 |

## 6. 인수 이후 남는 범위

| 항목 | 이번 인수의 의미 |
|---|---|
| 부분 Configure 실패 | 기존 순서 그대로이며 전체 조립 원자성·롤백 개선은 미구현 |
| Factory 반환 후 `_display` 인수 전 실패 | 입력 Clear/Presentation Attach 등의 예외에서 생성된 Display 해제 담당이 비는 기존 위험 유지. `_display` 저장 뒤 Install 실패의 Detach 반환과 구별 |
| Refresh 두 후보·Audio·수명 관리자 | 후속 평가/설계 대상. 이번 구현 완료에 포함하지 않음 |
| 수정 전 Unity 기준선 | D0 및 선행 A/C′의 당시 미확보 이력 유지. 현재 사용자 통과 확인으로 과거 실패 재현·수정 전 통과를 소급하지 않음 |
| 실행 증거 | 현재 결과는 사용자 전체 확인과 기존 빌드/관리·Editor 로그를 구분하여 인수. XML·정확한 전체 실행 건수·프레임별 플레이 로그는 수집하지 않음 |

후속 구현을 바로 시작하지 않는다. 이번 분리의 인수 상태와 새 Phase 검사는 계획 §10.7을 따른다.
