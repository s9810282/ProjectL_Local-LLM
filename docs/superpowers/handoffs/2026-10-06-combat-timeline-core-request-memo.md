# 전투 타임라인 예측 Buffer 게시 요청 — 전달 메모 (VR0, 2026-10-06, 2026-10-09 갱신)

- **받는 분:** Core 담당 천명님. **참조:** 현강님(`10f88c49` HUD·타임라인 작성).
- **상태:** 메모 준비만 했다. **미전달·미합의·미구현.** 발송은 사용자가 한다. 2026-10-09에 현재 코드(`2b25f64a`) 기준으로 줄 번호를 고치고 §5 확인 질문을 더했다.
- **요청 계약의 원본:** [구조 개선 설계 VR0](../specs/2026-10-06-combat-view-refactor-design.md#vr0--타임라인-게시-요청). 아래는 전달용 요약이며 계약 항목을 다시 정의하지 않는다.

## 1. 요청 한 줄

전투 행동 순서 예측(타임라인)을 Core가 Buffer에 게시하고, View는 그 값을 읽기만 하도록 바꾸고 싶다. 게시 시점·무효화·버전 일치·빈 값·종료/재진입·길이·계산 빈도와 회신해 줄 증거는 설계 VR0 표에 있다.

## 2. 왜 바꾸려는가

- 프로젝트의 데이터 흐름은 Core → Buffer → View이고, View는 규칙에서 나온 값을 Buffer로만 받는다(CLAUDE.md P6).
- 지금은 View가 Core 서비스를 직접 부른다. 결과를 그릴 때마다 Core 예측 계산이 View 쪽 호출로 실행된다.

| 위치 | 현재 코드 |
|---|---|
| `ViewSystem/Combat/CombatViewSystem.cs:164` | `PostBindService`에서 `resolver.GetService(SystemGroupsService.InGame).CoreGroup.Combat`을 보관(`_combatQueries`) |
| `ViewSystem/Combat/CombatViewSystem.cs:636` | `ReadDisplayValues()`가 `_combatQueries?.GetTimeline(Math.Max(MinTimelineEntries, 캐릭터 수 × TimelineEntriesPerCharacter))` 호출(상수 10·4, VR5). 설치와 결과 반영마다 실행 |
| `CoreSystem/CombatSystem/CombatProcessingService.cs:307-310` | `GetTimeline(int maxEntries)` → `CombatTurnCalculator.Forecast(...)` |
| `ViewSystem/Combat/Display/FieldUnitPresenter.cs:149` | `SetHudTimeline`이 받은 목록으로 UnitHUD 순서 번호 표시(소비 지점, 바꾸지 않음) |

## 3. 합의되면 View가 할 일과 하지 않을 일

- **할 일(VR6):** `ReadDisplayValues`가 게시된 Buffer 값을 읽는다. `_combatQueries`, `CoreGroup.Combat` 조회, Core 네임스페이스 using을 지운다. HUD 표시 의미와 개수는 합의 없이 바꾸지 않는다.
- **하지 않을 일:** 합의 전에 View가 임시 Buffer 필드나 가짜 예측을 만들지 않는다. Core 계산을 View로 옮기지 않는다. 그동안은 현재 직접 호출을 유지한다.

## 4. 현강님께

- HUD 쪽 코드(`SetHudTimeline`, UnitHUD)는 그대로 둔다. 바뀌는 것은 값을 가져오는 곳(`ReadDisplayValues`) 하나다.
- 지금 쓰는 길이(`Math.Max(10, 캐릭터 수 × 4)`, VR5에서 `MinTimelineEntries`·`TimelineEntriesPerCharacter` 상수로 이름만 붙임)가 HUD에 필요한 최소 길이인지 알려 주면 Core 요청의 "길이/절단" 항목에 반영한다.

## 5. 2026-10-09 추가: 이미 Buffer에 있는 타임라인과의 관계

메모를 쓴 뒤 상황이 바뀌었다. 전투 상태 Buffer에 타임라인 목록이 이미 있고, 화면 위 타임라인 UI는 그것을 읽는다. 지금 턴 순서를 보여 주는 곳이 둘이고 출처가 다르다.

| 소비자 | 읽는 값 | 갱신 시점 | 위치 |
|---|---|---|---|
| 유닛 HUD의 턴 순서 숫자 | `GetTimeline(max(10, 캐릭터 수 × 4))` 예측 | 전투 설치·결과 연출이 끝난 뒤 | `CombatViewSystem.cs:636` → `FieldUnitPresenter.cs:149` |
| 화면 위 `TimelineUI` | `CombatStateData.CommandState.Timeline` | `MonoBehaviour.Update`마다(최신 Buffer) | `GlobalSystem/UISystem/Screens/Stage/TimelineUI.cs:116` |

Core 코드에서 `CommandState.Timeline`은 명령 처리 때 이전 상태의 목록을 이어받고 죽은 유닛 항목을 걸러 낸다(`CombatCommandCalculator.cs:392`). `GetTimeline`은 호출할 때마다 `CombatTurnCalculator.Forecast`로 새로 계산한다. 두 값이 같은 의미인지 View 코드만으로는 알 수 없다.

**확인하고 싶은 것(답에 따라 VR6 방향이 정해진다)**

1. `CommandState.Timeline`은 VR0에서 요청한 "게시된 예측"과 같은 값인가? 다르다면 무엇을 뜻하는가(현재 라운드의 순서, 남은 행동 등)?
2. 같은 값이라면: 길이(몇 개까지 들어 있는가), 언제 다시 채워지는가(명령·자동 진행·밤 시작·턴 종료), 배치 단계·전투 종료 때 비어 있는가.
3. 다른 값이라면: HUD 숫자와 `TimelineUI`가 어느 쪽을 써야 하는가. 둘 다 같은 쪽을 쓰게 하고 싶다.
4. View가 Buffer 값을 쓰게 되면 `GetTimeline()`을 View 이외에 쓰는 곳이 있는가(없으면 공개 서비스에서 빼도 되는가).

**View 쪽 사정:** 일반 결과는 연출이 끝난 뒤에 화면에 반영한다. `TimelineUI`는 연출과 상관없이 최신 Buffer를 읽으므로, 같은 값을 써도 연출 중에는 HUD와 한 순간 다르게 보일 수 있다. 어느 시점의 값을 보여 줄지는 현강님(UI)과 따로 맞춘다.
