# 손패 호버 상승·공격 예상 체력 프리뷰 설계

- 작성: View 작업, 2026-10-13
- 상태: 사용자 지시로 Phase H·P 코드·자동 검증 완료, 실제 Combat 재생 확인 대기. H 기본값 16·160 연결, P 대상별 예상 잔여 체력 연결.
- Task·실행 기록 원본: [구현 계획](../plans/2026-10-13-hand-hover-attack-preview-plan.md)
- 기준: 현재 작업 트리와 [View 설계 원칙](../handoffs/2026-09-30-combat-view-design-session-handoff.md) §4, [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), `Assets/AGENTS.md`.

## 1. 요청·조사 근거·범위

요청은 손패 카드에 포인터가 올라가면 약간 상승하고, 공격 모드의 조준 호버에서 공격 후 대상 체력을 미리 보여 주는 것이다. 두 기능을 현재 View 구조에 연결하며 Core 계산과 기존 집기·놓기·공격 제출 경로를 유지한다.

조사 기준 HEAD는 `e0c41e45`, 과거 참조는 `origin/develop-4week`의 `ae07af37`이다. 현재 작업 트리에는 적 행동 간격 연출 등 진행 중인 변경이 있으므로 HEAD만으로 현재 구현을 판단하지 않는다. 구현 직전에 관련 파일의 작업 상태를 다시 확인한다.

| 기능 | 과거 구현 | 현재 구현·빠진 연결 |
|---|---|---|
| 손패 상승 | `Assets/Scripts/ProjectL/Unity/View/UI/CardHandItem.cs`의 PointerEnter/Exit·UpdateHover, `CardHandPresenter.cs`의 호버 조건·Tick | `Display/HandPresenter.cs`는 화면 좌표 집기·카드 배치·풀 재사용을 제공한다. 상승 연출은 없다 |
| 예상 체력 | `Assets/Scripts/ProjectL/Unity/View/Combat/CombatAttackTargeting.cs` → `FieldUnitPresenter.PreviewHudHealth` → `UnitHUD.PreviewHealth` | `AttackTargetingInteraction.ApplyAttackAnswer`는 영향 칸·AP 비용·벽 차단만 표시한다. `UnitHUD.SetHealthPreview(int?)`는 있으나 대상별 연결이 없다 |

현재 파일의 경로 기준은 `Assets/ProjectL/Script/Systems/RuntimeSystem/`이다. `Combat/`은 그 아래 `InGameSystem/ViewSystem/Combat/`, `UnitHUD`는 `GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs`에 있다.

[기존 손패 카드화 설계](2026-10-06-hand-card-design.md) §6에서 제외했던 호버 상승을 이번 후속 범위에 포함한다. 호버 정보 카드, 핀 선택, Synergy/Function 패널은 추가하지 않는다. `UnitCard.prefab` 하나를 외형 원본으로 유지한다.

공격 프리뷰의 최초 표현은 **대상별 예상 잔여 체력 숫자와 기존 체력바**다. 별도 `-피해량` 텍스트, 감소 구간을 남기는 반투명 바, 피격·발사 연출은 이번 범위 밖이다. 기존 HUD가 표시하는 `현재 / 최대`에서 현재 부분과 바 길이만 예상 값으로 바뀐다. 예: 실제 `20 / 20` → 호버 동안 `13 / 20` → 호버 해제 시 `20 / 20`.

## 2. H 결정 — 손패 카드 호버 상승

### H1. 고정 판정 영역과 움직이는 외형

손패 인스턴스에만 런타임 래퍼 `HandCardHoverView`를 만든다. 공용 카드 프리팹을 복사하거나 덱 카드의 호버 동작을 바꾸지 않는다.

```text
HandPresenter.cardRoot
  └ HandCardHoverView (고정 RectTransform + 투명 Image, 호버 판정)
      └ UnitCardView (기존 UnitCard.prefab 인스턴스, 상승하는 외형)
```

- 래퍼는 `IPointerEnterHandler`·`IPointerExitHandler`로 자신의 표시용 호버 여부만 기록한다. 클릭·드래그·Submit 명령은 처리하지 않는다. 이벤트 구독·클로저·Button.onClick 바인딩을 추가하지 않는다.
- 래퍼의 판정 크기는 기존 카드 프리팹 크기 × `cardScale`이다. `HandPresenter.ApplyLayout`은 래퍼만 배치하고 외형의 상승 오프셋을 덮어쓰지 않는다.
- 투명 Image만 손패 인스턴스의 Raycast 대상이 된다. 자식 카드의 Graphic Raycast는 생성 시 한 번 끈다. 공용 프리팹 에셋과 덱 Variant의 값을 변경하지 않는다.
- `HandPresenter.TryPickUnit`은 같은 고정 래퍼 Rect를 사용한다. 외부 `ICombatPointerPicker`와 `CombatPointerMapper`의 공개 동작은 유지한다. 포인터 콜백에 별도 Raycast를 넣지 않는다.
- 겹친 카드의 우선순위는 기존 `ReserveSlotIndex` 배치 순서와 역순 집기 규칙이다. 호버 때문에 sibling 순서를 바꾸지 않는다. EventSystem의 판정 순서와 집기 순서를 일치시킨다.
- 상승한 외형만 보이는 영역은 새로운 집기 영역으로 확장하지 않는다. 판정 영역이 따라 움직여 Enter/Exit가 반복되는 현상을 피한다.

### H2. 연출 설정·진행

설정 원본은 기존 `HandPresenter` Inspector 한 곳이다. 추가 설정은 아래 두 값으로 한다.

| 설정 | 제안 기본값 | 단위·동작 |
|---|---:|---|
| `hoverLift` | 16 | Card Root 로컬 UI 단위의 상승 거리 |
| `hoverMoveSpeed` | 160 | 같은 단위/초. 16 상승에 약 0.1초 |

과거 값은 카드 공간에서 상승 45·속도 450이었다. 현재 기본 배율 0.35를 적용한 약 15.75·157.5를 기준으로 위 값을 제안한다. 이제 값은 부모 UI 공간 단위이며 `cardScale`을 다시 곱하지 않는다. Canvas 해상도 배율은 Canvas가 적용한다.

상승 거리가 0이면 상승하지 않는다. 이동 속도가 0이면 목표 위치에 즉시 적용한다. 음수·NaN·무한대는 연결/설정 검증에서 거절한다. `HandPresenter`의 Unity Update에서 활성 카드의 표시만 보간한다. InputSystem 조회, 새 서비스 Tick, Coroutine·Task는 추가하지 않는다. 시간 간격은 UI 표시이므로 `Time.unscaledDeltaTime`을 사용한다.

포인터 Exit는 부드럽게 원위치로 복귀한다. 드래그 시작·비활성화·풀 반환 등 수명 정리는 즉시 원위치로 돌린다. 설정과 보간은 순수 표현 값이며 Core 상태가 아니다.

### H3. 수명·기존 동작 유지

- 기존 배치 단계 표시 조건을 그대로 사용한다. RecordInputOnly에서 Core 국면이 배치인 경우 카드가 보이는 기존 조건도 유지한다.
- `SetHiddenEntity`로 드래그가 시작하면 전체 손패의 호버 상태를 비우고, 드래그 동안 상승을 억제한다. 드래그 대상이 필드 유닛이어도 같은 처리를 적용한다.
- `ClearHiddenEntity` 후 이전 호버를 자동 복원하지 않는다. 새 PointerEnter부터 시작한다.
- 래퍼 비활성화, 포커스 상실, 풀 반환·새 ID 바인딩, 손패 숨김·Release에서 호버와 오프셋을 비운다. 숨겨진 래퍼의 투명 Image가 UI 입력을 가로막지 않도록 래퍼 전체를 비활성화한다.
- 풀 단위는 래퍼와 카드 한 쌍이다. `CustomPool<HandCardHoverView>`·`EntityViewSet<HandCardHoverView>`로 수명을 함께 관리하고, 내용 표시만 내부 `UnitCardView`에 위임한다. 손패 루트 자체는 Drop 영역으로 계속 사용한다.
- 카드 내용과 이름·능력치 문구는 `UnitCardView`, 슬롯 순서는 Core의 `ReserveSlotIndex`가 원본이다. 정보 카드의 핀 선택 정책은 변경하지 않는다.

## 3. P 결정 — 공격 예상 체력 표시

### P1. Core 응답을 그대로 사용

요청 경로는 기존 공격 범위 요청 → `AttackAims`에 따른 칸·방향·전체 조준 → 공격 상세 미리보기 요청 → 번호가 일치한 `CombatPreviewResult.Attack` 응답이다. View가 유닛 DB에서 공격 방식·피해·방어를 읽거나 Core 서비스를 직접 호출하지 않는다.

`AttackPreview.Targets`의 각 `AttackTargetPreview`는 다음 사실을 이미 제공한다.

| 값 | 사용 |
|---|---|
| `TargetId` | 기존 필드 View와 연결 |
| `TargetHealthAfter` | 예상 숫자·바 길이의 원본 |
| `ExpectedDamage`, `HealthNormalizationLoss`, `TargetHealthBefore` | 계약 확인·회귀 검증. 최초 UI에서 추가로 계산하거나 합산하지 않음 |

`현재 체력 - ExpectedDamage`를 View에서 계산하지 않는다. Core가 제공하는 `TargetHealthAfter`에는 체력 정규화 등 공격 결과가 반영돼 있다. 실제 Buffer 체력과 표시 중인 실제 체력 사실은 변경하지 않는다.

### P2. 표시 계약·책임

`ICombatInteractionFeedback`에 아래 두 표시 의도를 추가한다. 명칭은 구현 기준으로 사용한다.

- `ShowAttackHealthPreview(ReadOnlyMemory<AttackTargetPreview> targets)` — 이전 대상 프리뷰를 교체하고 이번 대상에만 적용한다.
- `ClearAttackHealthPreview()` — 예상 체력만 비운다. 반복 호출해도 안전하다.

`AttackTargetingInteraction`이 응답 수명과 표시 허용을 판단하고, `CombatSceneDisplay`는 필드 Presenter에 위임한다. `FieldUnitPresenter`가 `TargetId`로 이미 존재하는 View만 찾는다. 없는·반환된 대상 때문에 새 View를 빌리거나 예외를 만들지 않는다.

예상 체력은 `FieldUnitView`의 좁은 표시 메서드로 적용한다. 활성 HUD는 기존 `UnitHUD.SetHealthPreview`를 사용한다. HUD가 없는/숨겨진 대상은 기존 3D 체력 숫자에 같은 예상 잔여 체력을 표시한다. fallback은 `Apply`로 받은 실제 현재/최대 표시값을 원본으로 복원하며 예상 값으로 실제 값 캐시를 덮어쓰지 않는다. 현재 캐릭터·구조물 HUD 표시 조건을 확장하지 않는다.

단일 대상만 가정하지 않고 `Targets` 전체를 순회한다. 광역·방향·관통 공격과 구조물도 Core 응답에 포함된 대상만 표시한다. 피해 0은 Core가 준 예상 값 그대로, 예상 체력 0은 0으로 표시하고 View를 제거하거나 사망 연출을 시작하지 않는다. 이후 턴의 지속 피해는 이번 응답에 포함되지 않았다면 추가 예측하지 않는다.

### P3. 표시 허용·늦은 응답·실제 연출 우선

피해 프리뷰는 다음을 모두 만족할 때 표시한다.

1. Live 실행 모드이며 공격 모드·현재 호버가 유효하다.
2. 기존 상세 요청 번호가 현재 Buffer 요청·응답 번호와 같고 공격자가 현재 조준 행위자와 같다.
3. 기존 `CombatInputPolicy.CanPreview` 조건을 만족하고 `AttackPreview.CanAttack`이 true다.
4. `facts.IsPresenting`이 false다. 즉 결과 재생 중이거나 `S > P`인 상태에는 예상 체력이 실제 피격 표시를 덮지 않는다.

기존 공격 범위·영향 칸 표시와 요청 허용 정책은 바꾸지 않는다. 4번은 **예상 체력 표시**의 추가 조건이며 입력 전체를 새 정책으로 차단하는 것이 아니다. 재생 중 받은 예상 체력 응답을 나중에 재사용하지 않는다. 후처리의 기존 `AfterResult` 범위 재요청과 호버 재요청으로 새 응답을 받는다. RecordInputOnly에는 가짜 피해값·DB 계산을 넣지 않는다.

새 결과를 처음 재생하기 직전에도 예상 체력을 비운다. `CombatViewSystem.ConsumeResult`의 기존 `_playback.Begin` 앞에서 Interaction의 좁은 프리뷰 무효화 호출을 한 번 수행한다. 그 호출은 Attack 소유의 상세 요청과 예상 체력만 정리하며 공격 모드·범위·명령·결과 확인 번호를 종료/변경하지 않는다. 현재 이미 진행 중인 적 행동 간격 연출의 완료·취소 경로를 바꾸지 않는다.

### P4. 정리 시점·AP와의 공존

| 시점 | 예상 체력 처리 |
|---|---|
| 새 호버 칸으로 이동, UI 위·필드 밖, 새 상세 요청 | 이전 대상 즉시 복원, 새 응답까지 표시 없음 |
| 공격 불가·AP 부족·유효 조준 없음·실패 응답·표시 조건 불충족 | 복원, 이전 응답 재적용 금지 |
| Attack 명령 제출 번호가 0보다 큼 | 즉시 복원하고 기존 상세 요청 번호 무효화. 제출한 명령은 지우지 않음 |
| 제출이 거절돼 번호가 0 | 제출 성공으로 취급하지 않음. 현재 표시 허용 조건이 사라졌다면 복원 |
| 결과 재생 시작·후처리·행위자 교체 | 복원, 이후 현재 모드가 유효하면 기존 경로로 새 응답 요청 |
| 취소·이동 모드 전환·턴 종료·포커스 상실·종료 입력 정리 | 복원 |
| 새 실제 체력 적용·새 ID 바인딩·풀 반환·Clear/Release | 예상 값과 대상 추적을 비움 |

미리보기 요청은 Attack이 소유한 번호일 때만 비운다. 이미 다른 입력 소유자가 기록한 요청을 지우지 않는다. 이전 응답은 요청 번호 무효화 뒤 다시 표시할 수 없다.

대상 체력과 공격자 AP는 별도로 정리한다. 현재 `FieldUnitPresenter.ClearHudPreview(actorId)`는 `UnitHUD.ClearPreview()`로 두 표시를 함께 비우므로 비용 전용 정리는 `SetActionCostPreview(0)`, 체력 전용 정리는 `SetHealthPreview(null)`을 사용하도록 분리한다. 이동 AP 프리뷰의 동작을 유지한다. 공격자가 피격 대상에도 포함되는 응답에서도 AP 설정/정리가 예상 체력을 지우지 않아야 한다.

프리뷰 적용 대상 추적은 `FieldUnitPresenter` 한 곳에 두고 기존 대상만 복원한다. HUD·fallback에는 표시용 예상 값만 두며 전체 Attack 응답이나 Core 가변 상태를 별도 저장하지 않는다.

## 4. 변경 범위·다른 작업과의 연결

| 대상 | 변경 |
|---|---|
| `Combat/Display/HandCardHoverView.cs` + `.meta` 신규 | 고정 판정·자식 외형·호버 플래그·보간/초기화 |
| `Combat/Display/HandPresenter.cs` | 래퍼 풀·배치·동일 Rect 집기, 두 설정, UI 표시 Update·드래그 정리 |
| `Assets/Scenes/Combat.unity` | H Phase에서 HandPresenter의 두 제안 설정만 연결 |
| `Combat/AttackTargetingInteraction.cs` | 유효 응답 대상 전달·정리·제출 성공 시 상세 요청 무효화 |
| `Combat/ICombatInteractionFeedback.cs` | 예상 체력 표시/해제 두 메서드 |
| `Combat/CombatInteraction.cs`·`CombatViewSystem.cs` | 결과 재생 직전의 좁은 무효화 연결 |
| `Combat/CombatSceneDisplay.cs` | 예상 체력 표시 위임·Clear/Release 정리, 기존 AP 정리 보존 |
| `Combat/Display/FieldUnitPresenter.cs`·`FieldUnitView.cs` | ID별 적용·복원·fallback·풀 수명, HP/AP 전용 정리 |
| `Assets/Tests/EditMode/Framework/` | 기존 Interaction·HUD 테스트 보완, 손패 판정/수명과 대상별 표시 검증 |

기존 `UnitHUD`의 예상 체력 API를 재사용한다. 현재 기능을 만족하기 위해 공용 HUD 프리팹/카드 프리팹·DB를 변경할 필요는 없다. 테스트 가짜 Feedback은 새 계약에 맞춰 갱신한다.

Core·Buffer 스키마·입력 ActionMap·Flow/Job/Plan·FieldCamera·Architecture·공용 Render 내부는 변경하지 않는다. 과거 Session/Relay·직접 Core 조회·카드 Drag 콜백은 가져오지 않는다. 새 C#은 블록 네임스페이스·Allman·UTF-8/LF와 `.meta` 규칙을 따른다.

현재 [적 연출](2026-10-13-enemy-presentation-design.md), [이동 피드백](2026-10-13-movement-planning-feedback-design.md), [임시 공격 연출](2026-10-13-temporary-attack-presentation-design.md) 문서가 별도로 있다. 공유 파일은 해당 작업의 Phase 보고 뒤 최신 변경 위에 필요한 줄만 연결하며 동시에 편집하지 않는다. 본 계획은 해당 작업의 구현 완료를 주장하거나 재생 책임을 가져오지 않는다.

## 5. SSOT·SOLID·JMH/FW 점검

- **SSOT:** 피해·공격 후 체력은 Core 응답, 실제 체력은 기존 Buffer/표시 사실, 외형은 UnitCard.prefab, 슬롯은 ReserveSlotIndex, 호버 플래그는 래퍼, 호버 설정은 HandPresenter, 프리뷰 대상 추적은 FieldUnitPresenter가 각각 소유한다. View가 피해나 턴 지속 피해를 다시 계산하지 않는다.
- **S:** 래퍼는 카드 표시만, HandPresenter는 손패 배치/수명만, Attack은 요청/응답 수명만, FieldUnitPresenter/View/HUD는 개체 표시만 담당한다. Bootstrap·LifeCycle에 UI 표시 상태를 넣지 않는다.
- **O/L:** 기존 표시 계약에 두 의도만 추가하고 모든 가짜 구현도 맞춘다. Show는 교체, Clear는 멱등적이며 해제 후 늦은 호출은 기존 Display 계약을 따른다. 예상 사망은 실제 사망으로 취급하지 않는다.
- **I/D:** Attack은 Feedback 계약으로 표시하며 MonoBehaviour/씬 검색을 하지 않는다. 대상 전달에 기존 불변 Core 결과 타입을 사용한다. 손패 호버는 로컬 표현이라 새 범용 서비스·이벤트 버스·입력 인터페이스를 만들지 않는다.
- **JMH:** 역할 분리, DB와 런타임 사실 분리, Build 후 구조 불변, 풀 재사용·명시적 정리, 과도한 추상화 금지를 적용한다.
- **FW-004/005~007:** 기존 상태 경계와 입력 Configuration을 유지한다. 호버 콜백은 Core 명령을 만들지 않는다. 공격 제출은 기존 Gate/Buffer 경로다.
- **FW-008/009:** 손패 래퍼에 UIBase를 붙이거나 UISystem에 등록하지 않는다. 화면 이력·덱 핀 동작을 변경하지 않는다.
- **FW-011/014/016:** 기존 풀 콜백에서 카드와 래퍼를 함께 정리하며 예상 체력 때문에 View를 대여하지 않는다. 기존 Framework 실행/결과 확인 순서를 유지한다.
- **Assets UI 규칙:** PointerEnter/Exit 사용, Exit/disable/rebind에서 호버 정리, 콜백 Raycast·할당 없음, 기존 클릭/드래그 경로 및 정보 카드 핀 정책 유지.

## 6. 검증·완료 기준

H는 고정 판정 영역·겹침 순서·카드 배율·재배치·드래그/풀 재사용에서 상승이 유지/복원되는지 Unity EventSystem과 실제 화면으로 확인한다. P는 요청 번호·대상 교체·실패·제출/연출·수명 경계를 우선 검증하고, 단일/광역/방향/관통·구조물과 0 체력 표시를 확인한다.

컴파일·관리 테스트 통과와 Unity EditMode/화면 확인은 구분한다. Phase별 검증 결과와 미확인 항목은 [Task 계획](../plans/2026-10-13-hand-hover-attack-preview-plan.md)에만 기록한다. 각 Phase 끝에서 보고하고 멈춘다.
