# 전투 View 구조 개선 SV 인수 평가 (2026-10-15)

- 작성: View 작업, 2026-10-15. SV8(통합 인수)의 구조 평가다. SV8 검토 에이전트의 지적(수치 단위·시나리오 기준·증거 범위)을 반영한 판이다.
- 기준: HEAD `4cfceaa4` + 미커밋 SV0~SV8 변경(`.cs` 30파일 수정·4파일 추가·2파일 삭제, `.meta` 4개 추가·2개 삭제·1개 수정). 작업 트리에 함께 있는 `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`(Unity가 바꾼 FMOD 경로 한 줄)은 SV와 무관하다.
- 원본: 평가 [structure-evaluation](2026-10-15-combat-view-structure-evaluation.md)(E1~E23, §4.4 시나리오), Task·실행·검토 기록 [SV 계획 §12](../plans/2026-10-15-combat-view-structure-improvement-plan.md). 판단 기준은 [설계 인계 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md).
- 증거 구분: MSBuild·Unity 내장 Mono 관리 실행(Claude 직접), Unity 에디터 컴파일 로그(Claude 확인), Unity Test Runner·재생(사용자 — 실행은 했으나 결과 **미수집**)을 나눠 적는다.

## 1. 결론

조정자 `CombatViewSystem`이 직접 조정하는 것은 Framework·씬 수명, 표시 인수와 정리 순서, 결과 소비 순서, 입력 Layer·수신 연결, facts 조립, 진행 계약이다(§4). 입력 기능은 묶음 하나(`CombatInteraction`)와 입력 Factory를 통해서만 안다. 조립·설치 실패 경로에는 소유자가 생겼다(E7·E15·E23). 표시 쪽(SD-D)과 입력 쪽이 같은 "Factory 조립 + 기능 구현" 구조가 됐다. 연결 활성 판정식·포인터 대상 판정·step 조건·슬롯 소유 판정은 한 곳씩으로 모였고, 행동 버튼 갱신은 고정된 세 지점(Tick 끝 파생, 입력 직후, 정지·제출 정지의 즉시 숨김)으로 정리됐다.

수용으로 남긴 공통 경계(E8 단일 표시 입구, E10 사건 Presenter 내부 부품, E11 활성 행위자 강조, E13·E14)와 범위 밖(Audio, VR6, R6)은 그대로다. **Unity Test Runner 결과를 받기 전이므로 이 인수는 구조·관리 실행·컴파일 기준까지다(§6).**

## 2. 발견 사항 처리 결과

| ID | 처리 | 결과 위치 |
|---|---|---|
| E1 입력 조립이 조정자·합성 객체 안 | 개선 | `CombatSceneInputFactory`(SV5). `CombatInteraction`은 기능 객체를 받기만 한다. 기능 내부 부품은 각 기능 소유 유지 |
| E2 조정자가 경로·배치/공격을 따로 앎 | 개선 | 경로를 `CombatInteraction`에 편입(SV4). 조정자의 `_pathInput` 0 |
| E3 행동 버튼 갱신 시점 6곳 | 개선 | Tick 끝 파생 + 입력 직후 + 즉시 숨김(SV6, Q2=A) |
| E4 입력 기능 수명 비대칭 | 구조 개선 | 수명 메서드 대칭화, `Poll`(SV4). 응답 전달 재설계(R6)는 범위 밖 |
| E5 연결 활성 판정식 6변형 | 개선 | `IsSceneActive`·`CanContinue`·`IsInputReady`·`IsCurrent`(SV2) |
| E6 경로 입력에 복귀 섞임 | 축소 개선 | `CombatReturnRequest` + `Refresh` 두 단계(SV7) |
| E7 인수 전 실패 해제 주인 없음 | 개선 | `AttachCreated`/`TakeDisplay`(SV1, 후속) |
| E8 단일 표시 입구 전달 비용 | 수용 | 변화 없음(§3 S-G) |
| E9 내부 객체의 재생 계약 선언 | 개선 | 선언 제거, 잘못된 연결은 컴파일 오류(SV3) |
| E10 사건 Presenter 내부 생성 | 수용 | 변화 없음 |
| E11 활성 행위자 강조 | 수용 | 변화 없음(SD-D5 §4 후속 후보 유지) |
| E12 계약 파일·public | 개선 | 조각 흡수, internal, 파일 이름 정리(SV3) |
| E13 우선순위 상수 | 수용 | 변화 없음(D11 독립성) |
| E14 Gate 대기 이중 전달 | 수용 | 의도된 현재 값 확인 |
| E15 조용한 반환 경로 구독 누수 | 개선 | 인수했을 때만 구독, Detach는 구독 해제 먼저(SV1) |
| E16 표시 서비스 `new` | 개선 | `CombatDisplayServices.From`(SV5) |
| E17 step 검증 위치·클로저 결합 | 개선 | 씬 루트 검사 + 공용 `ValidateStepSeconds`, 클로저 제거(SV1) |
| E18 Router 후설정 setter | 개선 | 생성 인자(SV4) |
| E19 포인터 대상 사본 | 개선 | facts에는 `IsAttackActive`, 판정은 소비자(SV4) |
| E20 공격 방향 벡터 Core 복제 | 기록 | 변화 없음. Core 요청 후보로 남김 |
| E21 감시가 숨은 이름 | 개선 | `WatchPlanning`·`RefreshReturnEstimate`(SV7) |
| E22 호출자 없는 public | 개선 | `CombatPathInput.SelectCanceled` 삭제(SV3). 테스트 전용 별칭은 유지 |
| E23 설치 실패 뒤 결과 소비 | 개선 | 첫 그리기 성공 전 Tick 정지(SV1, Q5=A) |

평가 §3.4 기록 항목의 현재 상태:

- 개발 Layer 전환은 여전히 경로를 취소하지 않는다(`CancelPresentation`, 경로 제외). RecordInputOnly에서는 전투 Layer 포인터가 언제나 공격 대상이라 경로 지정이 시작되지 않기 때문이다(`CombatInputRouter.ResolvePointerTarget`).
- Tick의 `Poll` 뒤에 `CanContinue`를 다시 보지 않는 동작은 HEAD대로 유지했다. 대신 `Poll` 안에서 응답 처리 중 재진입 정지로 놓인 경로를 다시 부르지 않는다.
- **facts 기본값 의미가 바뀌었다:** 옛 선택 인자 `isPathTarget`(기본 false)이 `isAttackActive`(기본 false)로 바뀌어, 인자를 생략한 facts가 전투 Layer·Live이면 이제 "경로 대상"으로 판정된다. facts를 직접 만드는 테스트는 의도를 명시한다(평가의 "새 인자는 기본값 없이" 권고와 같은 맥락).

## 3. 대표 시나리오 전후 대조

수정 파일은 컴파일러가 강제하거나 동작상 반드시 고쳐야 하는 곳만 센다. "읽을 범위"는 변경을 안전하게 하려고 이해해야 하는 코드다. 이전과 지금은 같은 기준으로 센다.

| 시나리오 | 이전(HEAD) | 지금 | 검증(지금) | 남는 공통 경계 |
|---|---|---|---|---|
| **S-A** 공격 조준 입력에 새 의존성(조정자·Factory 인자에 이미 있는 값) | 공격 구현 + `CombatInteraction` 생성자(전달) + 조정자 Attach + 테스트 직접 생성 4곳 + Wall 1곳 | 공격 구현 + `CombatSceneInputFactory`(서명·전달) + `CreateInteraction` 호출 테스트 5곳 + Wall 1곳. **조정자·`CombatInteraction` 제외** | Interaction·Router·Wall fixture, ViewSystem 통합 | 조정자가 가졌지만 Factory 인자에 없는 값이면 `CreateInput` 한 줄, 새 Global 서비스면 PostBindService·필드도. 테스트 수정 곳 수는 줄지 않았다 |
| **S-B** 경로 입력의 결과 후처리 변경 | 경로 입력 + 조정자 결과 경로 4곳(Tick 폴링·결과 전 취소·확인 뒤 견적·정지) | 경로 입력 + `CombatInteraction`의 해당 수명 메서드 1개(`Poll`·`BeforeResult`·`AfterAcknowledged`·`Stop`) | PathInput·MovementFeedback·Router 정지 테스트, ViewSystem 결과 경로 | 조정자 단계 사이 순서(확인 전/후)를 바꾸면 조정자 Finalize도. 입력 사건 쪽(Router의 FocusLost·SelectCanceled·Cancel·EndTurn·AttackMode·Return·포인터)은 이전엔 Router가 경로를 직접, 지금은 묶음 메서드를 거쳐 부른다 — 결과 후처리와 별개 경로다 |
| **S-C** 행동 버튼에 영향 주는 새 상태 | `CombatActionButtons.Read` + 조정자 갱신 지점 6곳(개발 Layer 콜백 포함) 전수 확인 | 새 상태가 facts나 `Read` 인자에 이미 있으면 `CombatActionButtons.Read`만. Tick 끝·입력 직후가 언제나 다시 계산 | ViewSystem 행동 버튼 테스트(결과 수명·공격 강조·제출 정지·RecordInputOnly) | facts·인자에 없는 새 사실이면 `ReadFacts`·`SyncActionButtons`도. UI 콜백에서 바뀌는 값은 그리기 기준 0~1프레임 늦을 수 있다 |
| **S-D** 복귀 조건 변경 | 경로 입력 안 약 150줄(필드 3개·감시가 숨은 `RefreshReturnPreview`) | `RefreshReturnEstimate`·`RequestReturn`·`HandleReturnPreview`(이름 붙은 세 메서드, 약 50줄) + 필요 시 `CombatReturnRequest`(정체성·판정) | ReturnRequest·RangeFeedback·PlanningInput·MovementFeedback(복귀 제출 1회) | 견적 조건·국면 조건(두 메서드에 겹침)과 단일 미리보기 슬롯 경쟁은 경로 입력에 남는다(설계대로) |
| **S-E** 연결 수명 조건 추가 | 조정자 6곳을 일관되게 수정 | `IsSceneActive` 한 곳(재생·입력 쪽은 형제로 따라감) | ViewSystem·Panel·Receiver | 조건이 재생 쪽이나 입력 쪽 한쪽에만 속하면 그 판정식 한 곳 |
| **S-F** 재생 계약 멤버 추가 | 선언 + Display + 사건 Playback + 사건 Presenter + 가짜(강제 5파일) | 선언 + Display + 가짜(강제 3파일) + 실제 구현 위치 | 재생·연출 fixture(Unity) | 이동 보간 계약(`ICombatMovementPlaybackPresenter`)이면 그 소비자·테스트 가짜도 |
| **S-G** 새 표시 의도 API | 계약 + Display 전달 + 기능 + 가짜 | 같음(E8 수용) | 해당 기능 fixture | 해제 뒤 규칙(D9) 원본 한 곳의 대가 |
| **S-H** 새 필드 표시 추가 | Factory + Display 6지점 | 같음(수용) | 실제 씬 fixture(Unity) | Clear·Release 목록 누락이 실제로 생기면 재검토 |
| **S-I** 결과 전후처리에 참여하는 새 입력 기능(평가 E4) | 새 기능 + 조정자(Tick·결과 전후·정지·취소) + Router(setter·분기) + Interaction 또는 조정자 직접 + facts | 새 기능 + `CombatInteraction`(수명 메서드·포인터 분기) + 입력 Factory. 매 Tick 폴링은 `Poll` 안. Layer로만 정해지는 대상이면 Router 판정식 한 곳 | 새 기능 fixture + Interaction·Router | **조정자가 바뀌는 조건:** ① 모드에 따라 정해지는 새 포인터 대상이면 원 사실이 하나 더 필요해 `CombatInputFacts`·`ReadFacts`·판정식 호출자(Router·경로 입력)가 함께 바뀐다. ② 행동 버튼에 영향을 주면 `SyncActionButtons`. ③ 새 패널 버튼이면 `BindActions`와 입구 대리자. ④ 다른 facts 공급자가 필요하면 `CreateInput` |

## 4. 조정자에 남은 것

- **직접 조정(설계대로 남김):** Framework 수명·씬 Enter/Exit, 표시 인수(`AttachCreated`·`TakeDisplay`·`ContinueAttach`)와 Detach·StopInput 순서, Tick의 결과 소비 순서와 재진입 확인(토큰), 입력 Layer·수신 연결, facts 조립, 진행 계약(제출 정지·완료 표시·포기 요청), 표시 값 읽기.
- **입력 기능을 부르는 지점(호출 지점 단위):** 이전 Interaction 9곳 + 경로 6곳 + Router setter 4곳 = 19곳, 직접 생성 5개(Gate·Interaction·Router·커서 스냅·경로) → 지금 묶음 11곳(멤버 10종: `Poll`·`PreviewAnswered`·`BeforeResult`·`AfterResult`·`AfterAcknowledged`·`CancelInputs`·`CancelPresentation`·`Stop`·`CanEnterAttack`·`IsAttackActive`×2), Factory 2곳(`CreateSceneInteraction`·`CreateCursorSnap`), 직접 생성 2개(Gate·Router). 공격 특화 질의(`CanEnterAttack`·`IsAttackActive`)는 행동 버튼·facts의 원 사실이라 남는다.
- **남는 경로 지식:** 경로용 사실 공급자 `_readPathFacts`(선행 미처리 명령 포함 사실). 다른 사실이 필요한 입력 기능이 생기면 조정자가 바뀐다.
- **줄 수:** `CombatViewSystem` 869 → 972줄. 주석, 인수 단계 분리, 판정식 문서가 늘었다. 줄 수는 목표가 아니었다(계획 §1). 입력 쪽 파일은 `CombatInteraction` 183 → 274, Router 200 → 167, 경로 입력 566 → 584(+ `CombatReturnRequest` 81), 새 Factory 79줄이다.

## 5. 원칙 재점검

| 기준 | 판정 | 근거·한계 |
|---|---|---|
| S | 개선 | 조정자 변경 이유 ④ 입력 조립·⑤ 기능별 호출·⑥ 버튼 시점을 Factory·묶음·Tick 파생으로 옮겼다. 묶음은 고정 순서 위임이며 새 상태는 경로 참조 수명 하나다. 다만 커서 스냅 모드 판단(`IsCursorSnapMode`)이 Router에서 묶음으로 옮겨 와 묶음이 작은 규칙 하나를 갖는다 |
| O | 조건부 개선 | 결과 전후처리에 참여하는 새 입력 기능은 대부분 조정자를 고치지 않는다(S-I). 모드에 따라 정해지는 새 포인터 대상·행동 버튼 영향·새 패널 버튼은 여전히 조정자와 facts를 바꾼다 |
| L | 유지·개선 | 해제 뒤 규칙(D9), 재생 계약(D6) 유지. 인수 전 실패·조용한 반환에서 표시가 반환된다 |
| I | 개선 | 재생 계약을 소비하지 않는 내부 객체의 선언 제거, 소비자 없는 조각 흡수. 소비자별 좁은 계약 유지 |
| D | 개선 | 구체 입력 기능 선택은 Factory, 조정자는 묶음·Factory만. Router 의존성은 생성 인자 |
| SSOT | 개선 | 연결 활성 판정식·포인터 대상 판정·step 조건·슬롯 소유 판정이 한 곳씩. Buffer 사실 복제 없음. 재생 세대 카운터 6종은 범위가 다른 별도 사실(수용) |
| FW/JMH | 유지 | 새 Service·ServiceKey·AddModule·Builder 단계 없음. 씬 검색 한 곳·구독 해제(E15 해소). 공용 Factory 폴더 추가는 Framework 공유 문서 §4에 적었다(미전달) |
| 조립 분리 | 개선 | 표시(SD-D)·입력(SV5) 모두 Factory 조립. 조정자 PostBindService의 표시 서비스 선택도 묶음으로 |
| 기능 변경 독립성 | 개선 | §3에서 S-A·S-B·S-C·S-D·S-E·S-F와 S-I(조건부)의 수정·읽을 범위가 해당 기능과 그 경계로 줄었다. S-G·S-H는 수용한 공통 경계로 남는다 |

## 6. 검증 증거와 한계

- **MSBuild(Claude):** 오류 0, 고유 경고 16(HEAD와 파일·메시지 기준 같음). 최종 빌드(14:51)는 마지막 `.cs` 변경(14:47) 뒤다.
- **관리 실행(Claude, Unity 내장 Mono, 메서드별 별도 프로세스):** 기준선이 있는 combat fixture 37개에서 HEAD 대비 PASS → 다른 상태 **0건**(새 fixture `CombatReturnRequestTests`와 HEAD 산출물에 없던 `CombatEnemyPresentationTests`는 기준선 없음). 새 테스트 메서드 19개 중 관리 실행이 가능한 15개는 모두 통과하고 4개는 Unity 전용이다(씬 루트 2·실제 씬 1·`CombatEnemyPresentationTests`의 step 1). 스크래치 변이 3건(정지 때 경로 Detach 누락·참조 유지, 버튼 갱신을 Tick 앞으로)을 새 테스트가 실패로 잡았다. `CombatViewSystemTests`는 씬 루트 GameObject만 지연(null)한 스크래치 패치 Rig로 돌렸다.
- **Unity 에디터 컴파일(Claude가 Editor.log 확인):**
  - 15:44 에디터 재시작 뒤 컴파일은 모든 `.cs` 변경 뒤였고 성공했다(`error CS` 없음, 어셈블리 재로드 성공). 단 이때 `CombatEnemyPresentationTests.cs`는 .meta 손상으로 **무시됐다**(로그 경고, csproj 0건).
  - .meta 수정(15:49) 뒤 Unity가 다시 컴파일했고(`Requested script compilation` → `successfully reloaded`, `error CS` 없음) csproj(15:56)에 이 파일이 들어갔다. GUID 경고는 다시 나오지 않았다. 이로써 Tests.Framework 어셈블리의 현재 입력 전체가 Unity에서 컴파일됐다.
  - 이것은 컴파일 증거이고 테스트 결과는 아니다.
- **새로 발견한 기존 결함:** `CombatEnemyPresentationTests.cs.meta`의 GUID가 33자리(`…4f5ef`)였고 2026-10-13 커밋 `4afddbf5`에서 처음 만들 때부터 그랬다. 그래서 **2026-10-13부터 이 fixture(28 메서드·50케이스)는 Unity에서 컴파일·실행된 적이 없다.** 32자리(`…4f5e`)로 고쳤다(테스트 스크립트라 GUID 참조 없음, Assets·Packages의 .meta 전체 검사에서 다른 손상·중복 없음).
- **Unity Test Runner(사용자):** 사용자가 실행했다(Editor.log에 테스트 실행 뒤 씬 백업 복원 기록). 결과는 Test Runner 창에만 있고 파일로 저장되지 않아 **아직 받지 못했다**. 남아 있는 `TestResults.xml`은 2026-10-02 실행분이다.
- **재생(사용자):** 미확인. 목록은 계획 §12.11.

## 7. 이력 정정

- 리팩토링 계획 R0.6(2026-10-14 "R0 유니티 확인 완료")과 그 무렵 EditMode 확인 기록에 포함된 `CombatEnemyPresentationTests`는 .meta 손상으로 **실행되지 않은 상태**였다. R1에서 이 fixture의 가짜 클래스를 `override`로 바꾼 변경도 Unity로 확인된 적이 없다. SD-D5 "모두 통과" 기준선에도 이 fixture는 빠져 있었다. 재생으로 확인한 동작(적 행동 간격·이동 회전 등) 기록은 별개이며 무효로 하지 않는다.

## 8. 남은 것

- **Unity Test Runner 결과 인수.** 실패가 나오면 다음 순서로 원인을 나눈다.
  1. 실패가 `CombatEnemyPresentationTests`에만 있으면 2026-10-13 이후 모든 변경이 원인 후보다(이 fixture는 그동안 한 번도 돌지 않았다). SV 변경을 잠시 치우고 .meta 수정만 둔 HEAD에서 이 fixture를 돌려 비교한다(사용자 판단으로 stash 등).
  2. 그 밖의 fixture 실패는 SV 변경 후보로 본다. 관리 실행 기준선과 대조해 네이티브 경로인지 본다.
- **재생 확인**(계획 §12.11 목록).
- **후속 후보(이번 범위 밖):** R6 미리보기 응답 전달 재설계, E20 Core 방향 벡터 공개 요청, E11 활성 행위자 강조 분리, Audio 정리(현강님), VR6.
