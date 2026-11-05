# 전투 View GC·프레임 끊김 개선 설계 (GH1~GH6)

- 날짜: 2026-11-03
- 상태: **결정 완료(2026-11-03 사용자: Q-GH1·Q-GH2·Q-GH3 A, Q-GH4 B, Q-GH5 B, Q-GH6 B — §6).** Q-GH4 B로 GH6(처음 나올 때 생성)을 더했다. Task 계획 [gc-hitch-plan](../plans/2026-11-03-combat-view-gc-hitch-plan.md). **GC-a~GC-f 구현 완료(Ua~Ue 문제 없음 — 사용자, 구현 메모 §8). 묶음 끝 검토 권장 반영(L1·L3·L4·L8·L9, 계획 §3.8, Uf 대기).**
- 요청: 사용자 "적 턴 넘어갈 때 + 적 진행 중 GC Allocated In Frame이 튐" → "코어 계산 측 문제인 거 확인했는데, View는 View대로 따로 고쳐 보자".
- 배경: 한 프레임 697ms·GC.Alloc 약 248만 번·98.6MB(사용자 Profiler). 0단계 Profiler 구간 표시(CLAUDE.md 2026-11-03 항목)로 사용자가 Core 계산이 원인임을 확인했다. 이 설계는 Core와 따로 **View 쪽의 할당과 반복 작업**을 줄인다. Core 쪽은 범위 밖이다(§5).
- 범위: 전투 View(`ViewSystem/Combat/`). 골격·Core·Framework·GlobalSystem·UISystem 파일은 고치지 않는다.
- 근거: 2026-11-03 코드 훑기(입력·표시·재생·공용 네 묶음)와 직접 대조. **비용 크기(ms·KB)는 재지 않았다.** 효과는 0단계 구간 표시로 Phase마다 확인한다.
- 규칙: View 작업이라 새 시험을 쓰지 않고, 전투 View라 MSBuild도 돌리지 않는다. 컴파일·재생은 사용자가 Unity에서 확인한다.

## 1. 확인 사실

### 1.1 강조 다시 그리기 (훑기 3번)

- `FieldView`의 변경 입구 넷(`SetHoverCell`·`ClearHoverCell`·`SetLayerCells`·`ClearLayer`, `FieldView.cs:194-228`)은 바뀐 칸이 있으면 바로 표시기 `Repaint`를 부른다.
- GridOutline 표시기는 바뀐 칸과 상관없이 `Refresh()` 전체를 돈다(`FieldOutlineRenderer.cs:207-213`).
  - `Refresh`는 매번 채우기 판 쌓기(`FieldCellFillStack.Apply`, 칸 × 종류)와 슬롯 약 20개를 돈다.
  - 칸이나 색이 바뀐 슬롯만 메시를 다시 만든다(`FieldDrawnCells.SameAs`).
- 한 프레임에 여러 번 일어난다.
  - 공격 미리보기 응답 하나에 층 변경이 4~6번이다(`AttackTargetingInteraction.cs:320-347` → `CombatAttackFeedback.cs:134-136`).
  - 유닛 호버 범위는 지우기 한 번과 층 둘이다(`CombatAttackFeedback.cs:89-96`).
  - 결과를 받기 직전 정리(`BeforeResult`)도 지우기가 여러 번이다.
  - 중간 상태 때문에 같은 판·테두리를 한 프레임에 두 번 만들 수 있다.
- 메시를 다시 만들 때 생기는 할당은 다음과 같다.
  - 우리 쪽: 슬롯마다 `cells.ToArray()`(`FieldOutlineRenderer.cs:627`), 채우기 판 `new Vector3[]`(`FieldCellFill.cs:98`).
  - GlobalSystem 쪽: `CreateGridOutline`의 HashSet·Dictionary·List(`MeshHightLevelBuilder.cs:262` 이하), GridCube 목록.
- GlobalSystem은 받은 배열을 그 자리에서 메시로 만들고 보관하지 않는다(`MeshSystem.cs:87-92`). 그래서 우리가 배열을 다시 써도 안전하다.
- 색만 바뀌어도 `AreaOutlineHandle.Update`는 메시부터 다시 만든다(`AreaOutlineHandle.cs:53`). 그러나 층 색은 층마다 고정이라 색만 바뀌는 슬롯은 호버 한 칸 정도다. 이번 범위에서 뺀다.
- 그린 결과를 같은 프레임에 바로 읽는 곳은 없다.
  - 표시기 입구는 `CanReuse`·`MeasureSpacing`·`Build`·`Repaint`·`Refresh`·`Release`뿐이다.
  - 채우기 깊이는 `Refresh` 안에서만 묻는다.
  - 지금 `Assets/Tests`에 `FieldView`·표시기·`RenderFacts`·`CancelInputs`를 쓰는 시험은 없다.

### 1.2 사건마다 모든 유닛 다시 그리기 (훑기 2번)

- `CombatShownFacts.Render()`(`:155`) → `FieldUnitPresenter.RenderFacts`가 모든 유닛·구조물을 다시 그린다. 부르는 곳은 다음과 같다.
  - 사실 사건마다: `CombatSceneEventPresenter.cs:340`
  - 피격·사망·추락·토네이도: `CombatCharacterEventHandler.cs:124`·`:156`·`:188`·`:206`
  - 결과 시작: `CombatSceneEventPresenter.cs:181`
  - 복귀: `CombatMovementPresentation.cs:86`
  - 걸음 한 칸의 시작과 끝: `CombatUnitMotionPlayback.cs:277`·`:375` → `ShowCharacter`
  - 끌어오기: `CombatUnitMotionPlayback.cs:147`·`:183`
- 그릴 때마다 유닛마다 HUD를 처음부터 다시 쓴다(`RenderUnitHud`, `FieldUnitPresenter.cs:367-389`).
  - 참조 검사(`ValidateReferences`) 3번
  - TMP 글자 여러 개
  - 상태 문자열(`"StatusEffect:" + 키`, `CombatUnitStatuses.cs:30`)과 아이템 설명 합치기(`FieldUnitPresenter.cs:390`)
  - 상태 아이콘 강제 정렬(`UnitHUD.SetStatuses`)
- HUD 내용을 정하는 것은 두 가지뿐이다. 그 유닛의 사실(불변 `CharacterCombatFacts`) 하나와, 전투 동안 바뀌지 않는 표(유닛 외형·직군·상태 외형·아이템 외형)다.
- 결과 하나 안에서는 바뀐 유닛만 새 사실 객체로 바뀐다(`CombatShownFacts.ApplyCharacter`).
- 그래서 적 여럿이 걸으면 "걸은 칸 수 × 2 × 유닛 수"만큼 HUD를 다시 쓴다. 대부분은 바뀌지 않은 유닛이다.

### 1.3 매 프레임 할당 (훑기 4번)

- 경로 입력은 미리보기 응답이 한 번이라도 온 뒤로 매 프레임 `OnPreview`를 받는다(`CombatPathInput.cs:723-730`).
  - 경로 입력이 허용되지 않으면 매 프레임 `CancelInputs()`가 돈다. 허용되지 않는 구간은 배치 국면, 공격 모드, 적 턴, 재생 중이다.
  - 그때마다 람다 넷, 대리자 하나, `params` 배열을 새로 만든다(`:481-511`).
  - 다른 코드가 판 번호가 정확히 1 오르는 것에 기댄다(`:238`). 그래서 동작은 그대로 두고 할당만 없애야 한다.
- 경로 Drag 중에는 매 프레임 `SequenceEqual`을 부른다(`CombatMovementInteraction.cs:402`, `IReadOnlyList` 열거자 할당).

### 1.4 사건·조각마다 작은 할당

- `System.Enum.IsDefined(typeof(…), 값)`은 부를 때마다 박싱하고 리플렉션을 쓴다. 조각 시작·신호·조립 항목·Gate가 부르는 여덟 곳이다.
  - `CombatPieceStart.cs:91`, `CombatPieceContext.cs:76`
  - `CombatComposition.cs:88`·`:209`·`:244`
  - `CombatStandMovePiece.cs:52`, `CombatHitGate.cs:70`, `CombatAttackShape.cs:38`
  - `CombatAttackStyles.cs:42`와 `CombatTraitStateEvidence.cs:29`는 만들 때 한 번이라 제외한다.
- `CombatShownFacts.ApplyCharacter`·`ApplyStructure`의 `RemoveAll` 람다(`:51`·`:73`). 사건마다 돈다.
- 투사체를 반환할 때마다 `new List<Exception>()`을 만든다(`CombatProjectilePool.cs:359`, `:175`·`:208`도 같은 꼴).
- 몸 위 글자 창의 `Exists` 람다(`CombatBodyTextWindow.cs:56`).

### 1.5 매 프레임 CPU (할당은 아님)

- HUD 층이 매 프레임 모든 HUD의 `localPosition`을 비교 없이 쓴다(`UnitHUDLayer.cs:258`). 그래서 HUD 캔버스를 매 프레임 다시 묶는다.
- 같은 갱신이 매 프레임 `RefreshSelectedInfo()`를 불러, 선택된 HUD의 `ValidateReferences()`를 돈다(`UnitHUDLayer.cs:59`·`:265`).
- 환경 설명 창이 떠 있으면 포인터가 그대로여도 매 프레임 위치를 쓴다(`CombatEnvironmentHover.cs:61-65` → GlobalSystem `TextNoticeUI`).

### 1.6 처음 나올 때 생성 (훑기 1번, Q-GH4 B로 이번 범위 — GH6)

풀이 빈 채로 시작해서, 처음 나오는 프레임에 `Instantiate`가 일어난다. GC가 아니라 생성 끊김이다.

- 유닛·HUD: `FieldUnitPresenter.cs:568-630`
- 투사체: `CombatProjectilePool.cs:66`
- 몸 위 글자: `CombatBodyTextPool.cs:55`
- 배치 고스트: `PlacementGhost.cs:145-214`
- 강조 판: `FieldCellFillStack.cs:452-511`

GH6을 위해 더 확인한 것(2026-11-03):

- 전투 설치(`CombatSceneDisplay.Install`, 씬 전환 로딩 안)에서 Buffer 캐릭터 목록에 필드·손패·대기·적이 모두 있다(`RenderFacts`가 `Area`로 거름). 적은 전투 준비 때 만들어진다(`BattleDefinitionAdapter.cs:106`). 키를 모르는 것은 전투 중 소환뿐이다.
- 유닛 표시는 키별 풀이고, HUD는 표시에 붙어 함께 풀로 돌아간다(`FieldUnitView.ResetForPool`이 HUD를 비우고 숨기고 표시를 끈다).
- Framework `CustomPool`은 처음 크기만큼 만들 때 대여·반환 수명(`OnBeforeReturn`)을 거치지 않는다(`CustomPool.cs:53-71`). 유닛 표시는 켜진 채 만들어지므로, 미리 만들 때는 반환 수명을 거쳐야 한다.
- 투사체 프리팹은 유닛 외형 키의 공격 값에 있다(`ICombatUnitTargets.AttackSettingsOf(key).ProjectilePrefab`, 근접은 없음). 풀은 프리팹별이고 처음 쓸 때 만든다(`CombatProjectileFlights.PoolOf`). 인스턴스는 꺼진 보관 루트 아래 꺼진 채 만들어진다.
- 고스트는 프리팹별 풀이고 한 번에 하나만 뜬다. 만들 때 숨긴다(`PlacementGhost.Create`).
- 몸 위 글자 프리팹 목록은 이미 함수가 있다(`CombatCueSources.BodyTextPrefabs`, 개발 미리보기가 씀). 특성 발동 글자에만 쓴다.
- 강조 판은 (종류, 깊이)마다 처음 쓸 때 재질 사본과 오브젝트를 만든다. 칸을 넣지 않은 판은 아무것도 그리지 않는다(`FieldCellFill.Update` 전에는 메시가 없음).
- 에디터의 Mono JIT(사용자 Profiler의 `Mono.JIT` 78ms)는 코드를 처음 실행할 때 생긴다. 미리 만들기로는 줄지 않는다.

## 2. 설계

### GH1. 강조는 프레임당 한 번 그린다

- `FieldView`의 변경 입구는 상태만 바꾸고 바뀐 칸을 모은다. 그리기는 `FieldView.LateUpdate`에서 한 번 한다.
- 모으는 방법은 다음과 같다.
  - 상태가 돌려주는 바뀐 칸 목록은 다시 쓰는 목록이라 그대로 들고 있지 않는다.
  - 칸 수 크기의 표시 배열과 목록에 옮겨 담는다. 둘 다 구성 때 한 번 만들어 할당이 없다.
  - 칸과 무관한 변경(`SetPlacementActive`·`SetPathPlanning`·`RepaintSurface`)은 "전체 다시"로 표시한다.
- 두 표시기 모두 같은 규칙을 쓰고, 표시기 계약 `IFieldHighlightRenderer`는 바꾸지 않는다.
  - CellPrefab 표시기는 모은 칸만 `Repaint`한다.
  - GridOutline 표시기는 `Refresh`를 한 번 한다.
- 지금처럼 바로 그리는 경우는 다음과 같다.
  - 구성(`Build`)은 바로 그린다.
  - `FieldView`가 꺼져 있어 `LateUpdate`가 돌지 않으면 바로 그린다(지금 동작).
  - `Release`는 기다리던 그리기를 버린다.
- 보이는 차이는 없다. `LateUpdate`는 렌더보다 앞이라 같은 프레임에 그려지고, 최종 상태 하나를 그리므로 순서에 따라 결과가 달라지지 않는다.
- 효과: 응답 하나·정리 하나에 여러 번 돌던 `Refresh`가 프레임당 1번이 된다. 중간 상태 때문에 메시를 다시 만드는 일도 없어진다.

### GH2. 그릴 때 우리 쪽 할당을 없앤다

- 테두리 슬롯마다 칸 배열을 들고, 칸 수가 같으면 그 배열을 다시 쓴다. GlobalSystem은 배열을 보관하지 않는다(§1.1).
- 채우기 판 위치 배열도 같은 방식으로 다시 쓴다.
- GlobalSystem 빌더 안의 할당은 남는다. 민형님께 요청 메모로 넘긴다(§5).

### GH3. 사실이 같은 유닛은 HUD를 다시 쓰지 않는다

- `FieldUnitView`가 자기 HUD에 마지막으로 쓴 사실 객체를 기억한다. `RenderUnitHud`는 같은 객체면 건너뛴다. 위치·체력을 맞추는 `view.Apply`는 지금처럼 늘 한다.
- 기억을 비우는 때는 다음과 같다.
  - HUD가 숨겨질 때(HUD가 숨을 때 체력 표시 상태를 지움 — 계획 c.0 대조로 더함, 계획 §3.3)
  - HUD를 새로 붙일 때
  - 다른 개체로 묶일 때
  - 풀로 돌아갈 때
  - 표시 `Configure` 때(표가 바뀔 수 있음)
- 결과 끝의 최종 표시(`Replace`)는 사실 객체를 새로 만들어 모든 HUD를 한 번 다시 쓴다. 지금과 같고 결과당 한 번이다.
- 걸음 한 칸마다의 그리기는 그대로 둔다. HUD는 움직인 유닛만 다시 쓴다(그 유닛은 칸마다 사실이 바뀜). 걸음 그리기 자체를 그 유닛만으로 줄이는 것은 하지 않는다(Q-GH2 B).
- 계획 단계에서 다시 대조할 것: HUD의 같은 칸을 다른 길이 쓰지 않는지.
  - 피해 숫자·비용·예상 체력은 다른 칸이다(`CombatUnitHudFeedback`).
  - 구조물은 `SetStructure`로 따로 쓴다.

### GH4. 매 프레임·사건마다 생기는 작은 할당을 없앤다 (동작 같음)

- `CancelInputs`
  - 람다와 `params` 대신, 생성 때 한 번 만든 대리자 다섯과 판 번호 검사를 쓴다.
  - 실패는 `CleanupErrors.Try`·`ThrowIfAny`로 모은다. 순서·예외 묶음·문구는 지금과 같다.
  - 판 번호는 지금처럼 1 올린다. 실패 목록은 실패할 때만 만들어지므로 평소 할당은 0이다.
- 열거 값 검사: 박싱 없는 검사 도우미 하나(정의된 값 표를 처음 한 번 만듦)를 여덟 곳이 부른다.
- 그 밖의 작은 할당
  - `RemoveAll` 람다 → 인덱스 반복
  - 투사체 반환의 `errors`는 실패할 때만 만든다(같은 파일 `:144`·`:522`와 같은 꼴)
  - 글자 창 `Exists` → 반복
- 경로 Drag의 `SequenceEqual` → 인덱스 비교 함수.

### GH5. 매 프레임 CPU를 줄인다 (선택)

- HUD 층
  - 위치가 같으면 쓰지 않는다.
  - 선택 정보 갱신은 선택이나 보임이 바뀔 때만 한다. 매 프레임 `ValidateReferences`가 없어진다.
- 환경 설명 창은 화면 위치가 바뀔 때만 옮긴다.

### GH6. 처음 나올 때 생성을 전투 설치 때로 옮긴다 (Q-GH4 B)

- 원칙: 전투 중 처음 생기는 것 가운데, **무엇을 몇 개 만들지 전투 시작 때 Buffer와 View 표로 알 수 있는 것만** 미리 만든다. View는 InGame DB를 읽지 않는다(P6).
- 때: `CombatSceneDisplay.Install` 안, 첫 그리기(`RefreshCore`) 뒤에 한 번. 씬 전환 로딩 안이라 끊김이 보이지 않는다. `Refresh`(결과 끝)에서는 하지 않는다.
- 대상과 수
  1. 유닛 표시 + HUD: 살아 있는 캐릭터의 키마다 그 키 캐릭터 수만큼(필드·손패·대기·적 모두). 이미 만든 수는 빼고 모자란 만큼만 만든다. 만든 것은 보통 반환 길(`ResetForPool`)로 풀에 넣는다.
  2. 배치·이동 고스트: 플레이어 캐릭터 키의 프리팹마다 하나.
  3. 투사체: 살아 있는 캐릭터 키의 공격 값 투사체 프리팹마다 하나(한 공격에 한 발, 맞으면 바로 풀로 돌아옴).
  4. 몸 위 글자: 글자 프리팹마다 정해진 수(계획에서 정함).
  5. 강조 판: 채우기 종류마다 깊이 0 판. 깊이 1 이상은 겹칠 때만 생겨 그대로 둔다.
- 넣지 않는 것(그대로 처음 나올 때 만듦)
  - 전투 중 소환되는 유닛: 키를 미리 모른다.
  - 가시밭·특성 흔적·약한 지반 교체 외형: 생길지와 수를 미리 모른다.
  - 특성 추가 공격의 공격 표시 덮어쓰기 투사체: 특성 외형 표를 걷어야 해 이번에는 뺀다.
  - 이펙트(GlobalSystem `EffectScope` 풀): 다른 영역, Q-GH5 B로 나중.
  - 에디터 Mono JIT: 미리 만들기로 줄지 않는다.
- 효과: 배치 국면의 첫 배치·첫 Drag, 적 턴의 첫 원거리 공격, 첫 특성 글자, 첫 호버·범위 표시 때의 생성 끊김이 전투 설치 때로 옮겨진다. 전투 설치는 그만큼 길어진다.

## 3. 바꾸지 않는 것

- 강조 규칙·색·층 표, 재생 순서·시점, HUD 내용, 입력 규칙.
- 표시기 계약 `IFieldHighlightRenderer`와 강조 변경을 부르는 쪽의 코드.
- Core·Framework·GlobalSystem·UISystem 파일, Factory·서비스 키.

## 4. Phase(안)

| Phase | 내용 | 확인(사용자, 0단계 구간 표시로) |
|---|---|---|
| GC-a | GH4 작은 할당 | 적 턴 중 `Combat.View.Input.Tick`의 GC Alloc이 0 |
| GC-b | GH1·GH2 강조 | 공격 모드 호버 중 `Combat.View.Field.Refresh`의 Calls가 프레임당 1 이하 |
| GC-c | GH3 HUD | 적 진행 중 `Combat.View.Units.Hud`의 시간·GC Alloc이 크게 줄어듦(Calls는 그대로, 계획 §3.3) |
| GC-d | GH5 매 프레임 CPU (Q-GH3) | 카메라가 멈춘 동안 HUD 캔버스 다시 묶기가 사라짐 |
| GC-e | GH6 처음 나올 때 생성 (Q-GH4 B) | 배치 국면 첫 배치·적 턴 첫 원거리 공격 프레임에 `Instantiate`가 없음 |
| GC-f | 0단계 구간 표시 지우기(Q-GH6 B)·문서·묶음 끝 검토 하나 | — |

Phase마다 컴파일 미확인, 새 시험 없음. Phase가 끝나면 멈추고 보고한다.

## 5. 범위 밖과 요청

- **Core 적 AI(천명님, 이번 끊김의 주원인)**
  - 적 행동마다 모든 적을 다시 검사한다(`EnemyAiTurnService.cs:31-51`).
  - 추격 탐색이 칸마다 가상 세계(캐릭터 배열·시야 환경 둘·벽 기하)를 새로 만든다(`EnemyMovementPathfinder.CanAttackFrom`, `:82-110`).
  - 요청 문서는 Q-GH5 B로 나중(사용자가 다른 영역을 맡아 처리).
- **민형님 GlobalSystem**
  - 테두리·판 메시 빌더의 임시 목록 재사용
  - 색만 바꾸는 입구(선택)
  - 이펙트 풀의 미리 만들기 수와 최대 수
- **UISystem 화면 담당**
  - UnitHUD: 같은 글자면 다시 쓰지 않기, `ValidateReferences` 줄이기
  - 피해 숫자: `fontSize` 대신 크기로 애니메이션
  - 전투 로그: 행 풀, 결과 게시 때 몰아 만들지 않기
  - 덱 카드 `foreach` 열거자, 시스템 그룹 Tick의 `foreach`
  - 설명 창 `TextNoticeUI.LateUpdate`·선택 정보 호버 창 `UnitHUDHoverPanel.RefreshPosition`: 위치가 같으면 쓰지 않기(GC-d 뒤 남은 매 프레임 쓰기, 계획 §3.4)
- 위 다른 영역 요청 메모는 모두 Q-GH5 B로 이번에 쓰지 않는다.

## 6. 정할 것 (결정 완료, 2026-11-03 사용자)

결정: "Q-GH4: 이번에 같이 진행 / Q-GH5: 다른 영역은 알아서 할 테니 나중에 / Q-GH6: 지움 / 나머지는 A로 진행" → Q-GH1 A·Q-GH2 A·Q-GH3 A·**Q-GH4 B**(GH6)·**Q-GH5 B**·**Q-GH6 B**(GC-f에서 지움).

| 번호 | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-GH1 | 강조를 언제 그리나 | A `FieldView.LateUpdate` / B 조정자 Tick 끝에서 명시 호출 | **A**. 입력 콜백·UI 이벤트·Tick이 프레임 안 서로 다른 때에 강조를 바꾼다. B는 Tick 뒤에 오는 UI 이벤트를 한 프레임 늦게 그린다. |
| Q-GH2 | HUD를 어디까지 줄이나 | A 사실이 같으면 HUD 쓰기를 건너뜀 / B A + 걸음 그리기를 그 유닛만 / C 하지 않음 | **A**. 한 곳만 고치면 되고 효과가 대부분 나온다. B는 풀 반환 규칙(`Begin`·`End`) 밖의 그리기 길이 하나 더 생긴다. |
| Q-GH3 | GH5(매 프레임 CPU)를 이번에 넣나 | A 넣음 / B 뺌 | **A**. 작고 GC와 같은 프레임 시간 문제다. HUD 층에 현강님 줄이 있으면 변경 정리로 넘긴다. |
| Q-GH4 | 처음 나올 때 생성 끊김(풀 미리 만들기) | A 다음 묶음 / B 이번 | **A**. GC가 아니라 원인이 다르고, 스테이지마다 무엇을 미리 만들지 정해야 한다. |
| Q-GH5 | 다른 영역 요청 메모 | A 이번에 셋 씀(Core 전달 문서 요청·민형님 공유·UISystem 담당 메모) / B 나중 | **A**. Core 쪽이 주원인이라 Core 요청이 가장 효과가 크다. |
| Q-GH6 | 0단계 구간 표시 | A 남김 / B 이 묶음 끝에 지움 | **A**. 비용이 거의 없고 다음 측정에도 쓴다. |

## 7. 원칙 점검

- **SSOT**
  - "강조를 언제 그리나"는 `FieldView` 한 곳에서 정한다.
  - "HUD를 다시 쓸지"는 `FieldUnitView` 한 곳에서 사실 객체가 같은지로 정한다.
  - 열거 값 검사는 도우미 하나가 맡는다.
  - "무엇을 미리 만들지"는 설치 때 Buffer 목록 하나에서 정하고, 만드는 일은 각 풀의 주인이 한다(GH6).
- **SRP**: `FieldView`는 상태 반영과 그리는 때를, 표시기는 그리기를 맡는다. GH1이 둘을 더 분명히 나눈다.
- **기능 변경의 독립성**: 표시기 계약과 강조를 부르는 쪽은 그대로다. HUD 쪽 UISystem도 그대로다. 각 GH는 따로 되돌릴 수 있다.
- **FW 계약**: GlobalSystem·서비스·Factory를 바꾸지 않는다.
- **위험**
  - GH1: 강조를 바꾸고 같은 프레임에 그린 결과를 바로 읽는 코드가 새로 생기면 틀린다. `FieldView` 문서 주석에 "그리기는 프레임 끝"을 적는다.
  - GH3: 다른 길이 HUD의 같은 칸에 쓰면 낡은 값이 남는다. 계획 때 HUD를 쓰는 길을 다시 대조한다.
  - GH6: 미리 만든 유닛 표시가 켜진 채 풀에 남으면 원점에 보인다. 보통 반환 길을 거쳐 끈다.
- **예상 효과**: 적 턴 전환 프레임의 큰 GC는 Core 몫이라 이 설계로는 줄지 않는다. 줄어드는 것은 세 가지다.
  - 적 진행 중 사건마다의 HUD 할당(GH3)
  - 호버와 결과 정리 때의 강조 재생성(GH1·GH2)
  - 쉬지 않고 쌓이는 작은 할당(GH4)
  - 처음 나올 때 생성 끊김(GH6)

  크기는 Phase마다 구간 표시로 확인한다.

## 8. 구현 메모 (2026-11-03, GC-a~GC-f)

실행 기록의 원본은 [계획](../plans/2026-11-03-combat-view-gc-hitch-plan.md) §3이다. 여기에는 설계와 다르게 된 점과 남은 것만 적는다.

- **GH1**(GC-b): 그리기 실패 예외가 `LateUpdate`에서 나므로 오류 경계(F1)가 아니라 Unity 콘솔로 간다. 칸 변경과 칸 무관 변경이 같은 프레임에 겹치면 GridOutline 표시기는 `Refresh`를 두 번 돈다(메시는 한 번). 검토 반영(계획 §3.8): `FieldView`는 실행 순서를 맨 뒤(`DefaultExecutionOrder` 10000)로 둬 다른 `LateUpdate`의 강조 변경도 같은 프레임에 그리고, 재생 중이 아니면(편집 모드) 층 API가 바로 그린다.
- **GH3**(GC-c): 기억을 비우는 때에 "HUD가 숨겨질 때"를 더했다(`UnitHUD.OnUIHidden`이 체력 상태를 지워서, 계획 c.0 대조). HUD 층 자체가 꺼질 때도 비운다(검토 반영 L1, 계획 §3.8: 기준점 계약 `IUnitHUDAnchor.OnHudLayerHidden`). 비우는 때의 목록 원본은 `FieldUnitView._hudFacts` 주석 한 곳이다.
- **GH5**(GC-d): 선택 HUD **찾기 반복**은 매 갱신 그대로다(HUD 개체가 UISystem에서 층 모르게 바뀌어서). 빠진 것은 매 프레임 참조 검사다. 남는 매 프레임 쓰기 `TextNoticeUI.LateUpdate`·`UnitHUDHoverPanel.RefreshPosition`은 §5 UISystem 목록에 더했다.
- **GH6**(GC-e): 셀 캐릭터 규칙(살아 있고 제외 아님)은 `CombatSceneDisplay.Prewarm` 한 곳, 수(TP-6)는 쓰는 꼴을 아는 각 주인이 갖는다. 키별 만든 수는 풀처럼 비우지 않는다(계획 적음 "Release에서 비움"이 틀림). 같은 씬에서 표시를 다시 만들면 풀에 남은 표시의 HUD는 처음 빌릴 때 만든다.
- **0단계 구간 표시**(GC-f, Q-GH6 B): 여섯 파일에서 넣은 줄만 되돌렸다. 다시 재려면 계획 §3.6의 이름을 다시 넣는다.
- **다른 사람 줄**: 실제로 고친 다른 작업자 줄은 현강님 HUD 줄(`1013a4df`)뿐이다 → [현강님 메모](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) §21(미전달). blame이 호진님·천명님으로 나오는 줄은 저장소 전체를 다시 올린 커밋(`83fb2767`·`4782921e`) 표시라 그분들 줄이 아니다.
- **남은 것**: Unity 확인 Uf(계획 §3.8), 사용자 커밋(새 `Presentation/Pieces/CombatEnumValues.cs` + `.meta`), §5 다른 영역 요청(Q-GH5 B, 사용자가 맡음). 검토 그대로 둔 것은 L2·L5·L6·L7(계획 §3.7).
