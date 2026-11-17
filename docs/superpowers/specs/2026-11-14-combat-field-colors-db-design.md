# 전투 필드 색을 View DB로 옮기고 에디터에서 바로 반영하기 설계 (2026-11-14)

> 사용자 요청: "대부분의 데이터는 SO를 DB로 받아오는데 왜 색은 FieldView에 있어? FieldView에 둔다고 런타임에 색 수정이 바로 반영되는 것도 아니던데" → "옮기고 나서 런타임에 색 값을 수정하면 반영되게 하고 싶은데" → "그 설계 문서 써줘".
> 상태: **결정 완료(2026-11-14 사용자 "권장안대로 하고 Task 계획 써줘"): Q-FC1~Q-FC4 모두 A, P-FC1~P-FC3 그대로.** Task 계획 [combat-field-colors-db-plan](../plans/2026-11-14-combat-field-colors-db-plan.md)(FC-a~FC-d, TP-1~TP-10 그대로). **FC-a~FC-d 완료(2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 구현 기록 §8), 검토 L2·L3·L7 반영(같은 날 사용자 "권장대로", 컴파일 미확인).** 남은 것: Unity 확인 U-0·1~10(§7), 검토 L1·L4 결정, 호진님 메모 전달.
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DB/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/`. 줄 번호는 이 문서를 쓸 때의 작업 트리 기준이다.

## 0. 요지

- `FieldView`의 씬 색 30개와 채우기 불투명도 하나를 View DB `StageFieldViewDataSO` 안의 색 묶음 하나(`FieldColorsData`)로 옮긴다. 기본 에셋에는 지금 `Combat.unity` 값을 그대로 옮겨 적어 화면은 바뀌지 않는다.
- 색을 쓰는 곳(칸 강조 팔레트·궤적 선·넉백/끌어오기 화살표)은 색을 생성 때 복사하지 않고 그 묶음을 들고 있다가 그릴 때 읽는다.
- 에디터에서 SO 값을 고치면(`OnValidate`) 에디터 전용 신호가 나가고, 전투 중인 `FieldView`가 받아서 필드를 한 번 다시 그린다. 빌드에는 이 신호가 없다.
- 실시간 반영은 색과 채우기 불투명도만이다. 두께·길이·재질 같은 모양 값은 지금처럼 다음 전투 진입 때 반영된다.

## 1. 확인한 사실 (코드 대조)

### 1.1 지금 색이 있는 곳과 쓰이는 길

- `V/Display/Field/FieldView.cs:43-127`에 `[SerializeField] Color` 30개와 `fillAlpha` 하나가 있다.
  - 칸 강조 층 색 22개와 `fillAlpha`(정적 구역·호버·입력/미리보기 층·상태 층·기믹·포효 예고·넉백 도착·낙석 경고)
  - 궤적 선 색 2개(`trajectoryColor`·`trajectoryBlockedColor`)
  - 화살표 색 5개(`knockbackColor`·`knockbackBlockedColor`·`knockbackImmuneColor`·`knockbackCollisionColor`·`pullArrowColor`)
- 이 색은 `FieldView.Build`(전투 설치 때) 한 번만 읽힌다.
  - 칸 강조: `CreatePalette()`(`FieldView.cs:632-637`)가 값을 `FieldHighlightPalette` 생성자에 넘겨 고정한다. 표시기 셋(`FieldOutlineRenderer`·`FieldCellFillStack`·`FieldCellColorRenderer`)이 이 팔레트를 들고 있다.
  - 궤적: `BuildTrajectory`(`:578`)가 색 둘을 `FieldTrajectoryLines` → `FieldTrajectoryLine` 생성자에 넘겨 필드에 저장한다.
  - 화살표: `BuildKnockbackMarkers`(`:592-593`)가 색 다섯을 `FieldKnockbackMarkers` 생성자에 넘긴다.
- 그래서 플레이 중에 인스펙터 색을 바꿔도 다음 전투 진입까지 반영되지 않는다. 씬에 둔 이점은 "씬마다 다른 색"뿐인데 전투 씬은 `Combat.unity` 하나다.
- 이 색들이 씬에 있게 된 것은 2026-10-02 View Phase D에서 옛 `FieldView`를 이식할 때 같이 들어왔고, 그 뒤 새 색을 "다른 필드 강조 색과 같은 자리"에 더해 온 결과다(궤적 설계 F11, 넉백 KB6, 끌어오기 PL3 §9.7). 모양 값은 그동안 `StageFieldViewDataSO`로 갔다.

### 1.2 DB 쪽

- `StageFieldViewDataSO.GetData()`는 사본이 아니라 SO 안의 직렬화 객체를 그대로 돌려준다(`DB/Item/StageFieldViewDataSO.cs:387`). DB 표 `StageFieldViewTable.Config`도 그 객체를 그대로 든다. 설정형 표라 아이템은 하나뿐이다(`StageFieldView_Default.asset`).
- 인스펙터 수정이 그 안쪽 객체에 그대로 덮어써지는지, Unity가 새 객체로 바꿔 끼우는지는 확인하지 않았다(U-0). 이 설계는 둘 중 어느 쪽이어도 동작하게 신호가 새 객체를 함께 넘긴다(FC3).
- `DatabaseItem`(Framework)에는 `OnValidate`가 없어 SO 파일이 직접 둘 수 있다.

### 1.3 다시 그리기 쪽

- 칸 테두리(`FieldOutlineRenderer`)·채우기(`FieldCellFillStack`)·화살촉(`FieldArrowhead`)은 "그린 칸·색이 지난번과 같으면 건너뜀"을 색까지 비교한다(`FieldDrawnCells.SameAs(cells, color)`, `FieldArrowhead._shownColor`). 팔레트 색이 바뀐 뒤 다시 그리면 바뀐 것만 다시 칠한다.
- 궤적 선(`FieldTrajectoryLine`)은 막힘 여부(`_shownBlocked`)만 기억해, 색이 바뀌어도 다시 넣지 않는다 → 고칠 곳.
- CellPrefab 모드 표시기(`FieldCellColorRenderer`)의 `Refresh()`는 빈 함수이고 바뀐 칸만 칠한다 → 색이 바뀌면 모든 칸을 바뀐 칸으로 넣어야 한다.
- `FieldView`의 그리기 대기(GH1: 바뀐 칸 모음 + 전체 다시, `LateUpdate`에서 한 번)를 그대로 쓴다.

### 1.4 시험·다른 사용처

- `Assets/Tests`(517개 파일)에서 `FieldView`·`FieldHighlightPalette`·`StageFieldViewData`·`FieldKnockbackMarkers`·`FieldTrajectoryLine`을 참조하는 시험은 없다. 팔레트를 만드는 곳은 `FieldView` 하나다.
- `FieldView` 컴포넌트는 `Combat.unity` 하나에만 있다.

### 1.5 씬 값과 코드 기본값

기본 에셋은 **씬 값**을 옮겨 적는다. 코드 기본값과 다른 칸:

| 칸 | `Combat.unity` 값 (옮겨 적을 값) | 지금 코드 기본값 |
|---|---|---|
| `hazardRangeColor` | (1, 0.15686275, 0, 1) | (0.75, 0.45, 1, 1) |
| `enemyAttackRangeColor` | (1, 0.15662651, 0, 1) | (1, 0.45, 0.35, 1) |
| `attackRangeColor` | (1, 0.15686275, 0, 1) | (0.45, 0.85, 1, 1) |
| `moveRangeColor` | (0.10849059, 1, 0.20388347, 1) | (0.45, 0.85, 1, 1) |
| `roarForecastColor` | (0.6, 0.34901962, 0.90196085, 1) | (1, 0.35, 0.1, 1) |
| `fillAlpha` | 0.183 | 0.25 |
| `knockbackDestinationColor` | (1, 0.8509804, 0.2, 1) | (0.05, 1, 0.75, 1) |

나머지 23개는 씬 값 = 코드 기본값이다. `pullArrowColor`는 씬에 아직 없어(2026-11-14 추가) 코드 기본값 (0.05, 1, 0.75, 1)을 쓴다.

## 2. 설계

### FC1. 색 묶음 하나를 SO 안에 (Q-FC1 A)

- 새 직렬화 클래스 `FieldColorsData`(`DB/Item/FieldColorsData.cs`)가 색 30개와 `fillAlpha`를 갖는다. 인스펙터 머리말은 지금 `FieldView`의 묶음(정적 구역·호버 칸·입력/미리보기 층·상태 층·기믹·포효·GridOutline 채우기·궤적·넉백/끌어오기·낙석)을 그대로 따르고, 필드 이름·툴팁도 그대로 옮긴다.
- `StageFieldViewData`에 `[SerializeField] FieldColorsData colors` 한 칸과 읽기 `Colors`를 더한다(머리말 "Colors").
- 검사 `FieldColorsData.Validate()`: 색 성분이 유한한 0~1 값인지, `fillAlpha`가 0~1인지. `StageFieldViewData.Validate()`가 부른다(부트 DB 생성에서 실패). 지금 팔레트 생성자의 `fillAlpha` 검사는 여기로 옮긴다.
- 순수 Data 규칙(P1)은 InGame용이고, View DB는 Unity 타입(`Color`)을 쓸 수 있다(P5).

### FC2. 색을 쓰는 곳은 묶음을 들고 그릴 때 읽는다

- `FieldHighlightPalette`는 색 값 대신 `FieldColorsData` 참조 하나를 들고, 속성은 그 묶음을 읽는다(`PlayerZone => _colors.PlayerZoneColor` 식). 묶음을 바꿔 끼우는 입구 `Use(FieldColorsData)` 하나를 둔다. 표시기 셋은 같은 팔레트 객체를 계속 들고 있으면 되므로 고치지 않는다.
- 궤적 선과 화살표도 같은 팔레트를 받는다. 생성자의 색 인자를 걷고, 그릴 때 팔레트에서 읽는다.
  - `FieldTrajectoryLine`: 지난번 넣은 색을 기억해 색이 다를 때만 ColorSystem에 넣는다(막힘 여부 대신 색 비교).
  - `FieldKnockbackMarkers.ColorOf`: 팔레트의 화살표 색을 읽는다. 화살촉은 이미 색을 비교한다.
- 칸 강조 색이 아닌 궤적·화살표 색도 팔레트에 두는 것은 "필드 색을 읽는 곳 하나"를 위해서다. 팔레트 이름은 그대로 둔다(바꾸면 다른 작업과 겹치는 파일이 늘어남).
- 매 프레임 읽는 것이 아니라 그릴 때 읽는다. 필드 읽기라 할당이 없다.

### FC3. 에디터에서 고치면 바로 다시 그린다 (Q-FC3 A)

- `StageFieldViewDataSO`에 에디터 전용 `OnValidate`를 둔다. 값이 바뀌면 `StageFieldViewData`의 에디터 전용 정적 신호 `EditorChanged(StageFieldViewData current)`를 낸다. 신호와 `OnValidate`는 모두 `#if UNITY_EDITOR` 안이라 빌드에는 없다.
- 신호를 `StageFieldViewData`(View가 이미 쓰는 Data 형식)에 두어 View가 SO 형식을 알지 않게 한다.
- `FieldView`는 `Build`가 끝날 때 신호를 구독하고 `Release`·`OnDestroy`에서 푼다(에디터 전용). 받으면:
  1. `_palette.Use(current.Colors)` — Unity가 안쪽 객체를 새로 바꿔 끼웠어도 새 객체를 가리키게 한다(U-0이 어느 쪽이든 동작).
  2. 모든 칸을 바뀐 칸으로 넣고 전체 다시 그리기를 표시한다(CellPrefab 모드 때문에 칸 전체, GridOutline은 색 비교로 바뀐 것만 다시 칠함). 그리기는 GH1대로 프레임 끝 한 번이다.
  3. 지금 보이는 궤적 선과 화살표에 색을 다시 넣는다(`ReapplyColors()`: 궤적 선은 마지막 막힘 여부로, 화살표는 마지막 종류 목록으로).
- 실시간 반영 범위는 색과 `fillAlpha`만이다. 모양 값(두께·길이·재질·간격)은 메시를 다시 만들어야 해 지금처럼 다음 전투 진입 때 반영된다(Q-FC3).
- DB 표가 든 옛 참조(`StageFieldViewTable.Config`)는 그대로 둔다. 색은 FieldView가 신호로 새 참조를 받으므로 상관없고, 다른 값은 다음 전투 진입(= 다음 재생)에 다시 읽는 지금 규칙 그대로다.

### FC4. 플레이 중 수정은 에셋에 남는다 (Q-FC2 A)

- 씬 컴포넌트 값은 플레이를 끝내면 되돌아가지만, SO 값은 플레이 중에 고쳐도 에셋에 그대로 남는다(Unity 동작).
- 색을 튜닝하는 쓰임에는 이쪽이 편하다(맞춘 색이 그대로 저장됨). 대신 실험으로 바꾼 값도 남으므로, 커밋 전에 `StageFieldView_Default.asset` 변경을 확인해야 한다. 사용법 안내에 한 줄로 적는다.

### FC5. 옮기기

- `FieldView`에서 색 30개·`fillAlpha`와 그 툴팁·머리말을 지우고, `CreatePalette()`는 `new FieldHighlightPalette(config.Colors)`가 된다. 궤적·화살표 생성 줄은 색 인자 대신 팔레트를 넘긴다.
- 기본 에셋 `StageFieldView_Default.asset`에 §1.5 값으로 색 묶음을 적는다. **SO 코드와 에셋은 한 커밋**(없으면 묶음이 코드 기본값으로 채워져 색이 바뀜).
- `Combat.unity`에 남는 옛 색 줄은 손으로 지우지 않는다. 필드가 없어지면 Unity가 무시하고, 다음 씬 저장 때 지운다(Q-FC4 A).

## 3. 고치는 파일

| 파일 | 바꾸는 것 |
|---|---|
| `DB/Item/FieldColorsData.cs` (새, + `.meta`) | 색 30개·`fillAlpha`, 읽기, `Validate` |
| `DB/Item/StageFieldViewDataSO.cs` | `colors` 칸·`Colors` 읽기·`Validate` 호출, 에디터 전용 `EditorChanged` 신호와 SO `OnValidate` |
| `Assets/ProjectL/DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset` | 색 묶음(§1.5 씬 값) |
| `V/Display/Field/FieldHighlightPalette.cs` | 묶음 참조·`Use`·궤적/화살표 색 읽기, 생성자 검사 이동 |
| `V/Display/Field/FieldView.cs` | 색 칸 지움, 팔레트 생성, 에디터 신호 구독·다시 그리기 |
| `V/Display/Field/FieldTrajectoryLines.cs`·`FieldTrajectoryLine.cs` | 색 인자 → 팔레트, 색 비교, `ReapplyColors` |
| `V/Display/Field/FieldKnockbackMarkers.cs` | 색 인자 → 팔레트, 마지막 종류 기억, `ReapplyColors` |

표시기 셋(`FieldOutlineRenderer`·`FieldCellFillStack`·`FieldCellColorRenderer`)·Factory·Core·Framework·GlobalSystem은 고치지 않는다. 새 시험 없음, 고칠 시험 없음(§1.4).

## 4. 원칙 점검

- **SSOT**: 필드 색의 원본이 씬에서 View DB 한 곳으로. 모양과 색이 한 SO에 모인다. 색을 읽는 곳은 팔레트 하나.
- **SOLID**: 표시기·부품은 "색을 어디서 가져오나"를 모르고 팔레트에 묻는다. 실시간 반영은 `FieldView` 한 곳이 신호를 받아 팔레트·부품에 알린다.
- **FW 계약**: DB 조회 계약 무변경(설정형 표 하나). Framework·GlobalSystem 무변경.
- **변경 독립성**: 에디터 실시간 반영은 `#if UNITY_EDITOR` 묶음이라 빼도 나머지가 그대로 돈다.
- **성능**: 빌드에는 신호가 없다. 그릴 때 필드 읽기뿐이라 할당이 늘지 않는다.

## 5. 결정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-FC1 | SO 안에서 색을 어떻게 둘까 | **A 색 묶음 하나(`FieldColorsData`)**. 바꿔 끼울 참조·검사가 하나 | B 관련 모양 묶음 옆에 흩어 둠(궤적 색은 궤적 묶음 옆 등). 인스펙터는 보기 좋지만 팔레트가 `StageFieldViewData` 전체를 들어야 함 |
| Q-FC2 | 플레이 중 SO 수정이 에셋에 남는 것 | **A 받아들임**(튜닝 값이 바로 저장, 커밋 전 확인) | B 플레이 끝에 되돌리는 장치(에디터 도구 필요, 범위 큼) |
| Q-FC3 | 실시간 반영 범위 | **A 색과 `fillAlpha`만** | B 모양 값까지(메시 다시 만들기, 범위 큼) |
| Q-FC4 | `Combat.unity`의 옛 색 줄 | **A 그대로 두고 다음 저장 때 Unity가 지움** | B 씬 YAML에서 손으로 지움 |

## 6. 설계에서 정한 것 (사용자 확인 대상)

- **P-FC1.** 궤적·화살표 색도 팔레트(`FieldHighlightPalette`)에서 읽는다. 이름은 그대로 둔다.
- **P-FC2.** 신호는 `StageFieldViewData`에 둔다(View가 SO 형식을 참조하지 않게).
- **P-FC3.** 색이 바뀌면 칸 전체를 다시 칠 대상으로 넣는다(CellPrefab 모드). GridOutline은 색 비교로 실제로 다른 칸만 다시 칠한다.

## 7. Phase와 Unity 확인

- **FC-a 값**: `FieldColorsData`·SO 칸·검사·신호, 기본 에셋 색(§1.5). 아직 아무도 읽지 않아 화면 변화 없음. SO 코드와 에셋은 한 커밋.
- **FC-b 옮기기**: 팔레트·`FieldView`·궤적·화살표가 DB 색을 씀, 씬 색 칸 지움. 화면 변화 없어야 함.
- **FC-c 실시간 반영**: `FieldView` 신호 구독·다시 그리기·`ReapplyColors`.
- **FC-d 문서·검토**: 이 문서 구현 기록, "색은 FieldView 씬 값"이라고 적은 설계들(공격 궤적 TJ8·넉백 KB6·궤적 대상별 선 §9.7·낙석 RP-16·포효 예고 RF4)에 대신함 한 줄, 호진님 필드 설정 표 메모 새 절(미전달), 새 에이전트 검토.
- 모두 컴파일 미확인으로 보고한다(CLAUDE.md). Unity 확인은 모든 Phase 뒤 한 번.

Unity 확인(사용자):

- **U-0** (FC-a 뒤, 사실 확인): 플레이 중 `StageFieldView_Default`에서 색 하나를 바꾼 뒤 그 값이 DB가 든 객체에도 들어갔는지. FC3은 어느 결과여도 동작하지만, 결과를 문서에 남긴다.
1. 전투 진입 화면이 옮기기 전과 같은지: 이동 범위(초록)·공격 범위(빨강)·채우기 진하기·포효 예고(보라)·넉백 도착 칸(노랑)·궤적·화살표.
2. `FieldView` 인스펙터에 색 칸이 더 없고, `StageFieldView_Default`에 "Colors" 묶음이 보이는지.
3. 플레이 중 공격 모드에서 공격 범위 색을 바꾸면 그 프레임에 바로 바뀌는지.
4. 궤적이 보이는 동안 궤적 색을 바꾸면 바로 바뀌는지, 넉백·끌어오기 화살표도 같은지.
5. `fillAlpha`를 바꾸면 채우기 진하기가 바로 바뀌는지.
6. 두께 같은 모양 값을 바꾸면 그 전투에서는 그대로이고 다음 전투 진입부터 바뀌는지.
7. 플레이를 끝낸 뒤 바꾼 색이 에셋에 남아 있는지(FC4) — 확인 뒤 되돌리기.
8. 전투를 나갔다 다시 들어와도 색 변경이 계속 반영되는지(신호 구독이 새 필드에 다시 걸림, 옛 구독은 풀림). 플레이 중 고친 색이 다시 들어온 전투에서도 그대로인지도 본다(묶음 끝 검토 L1 — U-0이 "다른 객체"면 다음 구성 때 옛 색으로 돌아감).
9. 전투 중에 다른 필드 설정 에셋(쓰지 않는 것)을 만들거나 고쳐도 전투 필드 색이 바뀌지 않는지(묶음 끝 검토 L2). 쓰는 에셋(`StageFieldView_Default`)을 고치면 지금처럼 바로 바뀌는지도 다시 본다 — U-0이 "다른 객체"여도 같은 SO라 받아야 한다.
10. GridOutline 스테이지에서 색을 고쳐도 Profiler의 `Combat.View.Field.Outline.Refresh`가 한 번 고칠 때 한 번인지(묶음 끝 검토 L3, 이전에는 두 번).

## 8. 구현 기록 (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **FC-a~FC-c:** 계획 §7.1. 설계·계획과 다른 점은 없다.
- **툴팁 다시 쓰기**(사용자 "색깔별로 Tooltip, 어떤 용도인지랑 어디어디에 이 색이 들어가는지"): `FieldColorsData`의 31칸을 모두 "무엇 / 쓰이는 곳 / 그리기" 세 줄로 썼다. 쓰이는 곳은 각 층을 칠하는 코드에서 찾았다. 옛 툴팁과 다른 것:
  - `activeActorColor`: Core 현재 행위자가 아니라 클릭해 고른 아군의 칸이다.
  - 빠져 있던 쓰임새: `movementDraftColor`(경로 띠·화살촉, 끌어오기 도착 칸), `moveRangeColor`(끌어오기 확장 범위), `attackHoverInvalidColor`(막힌 벽 칸), `gimmickHoverRangeColor`(클릭한 토네이도 도착 영역), 넉백 도착 칸·화살표 색(포효 예고의 넉백 미리보기에도).
- **Q-FC1 다시 보기**(사용자 "색을 몰아넣는 게 맞나, 각자 파트 옆이 맞나"): 답은 반반이었다. 칸 강조 색 23개와 `fillAlpha`는 같은 대상(칸 강조 층)을 칠하고 층 표가 한 벌로 읽어 묶음이 맞다. 궤적 색 2개와 화살표 색 5개는 그 파트의 재질·크기와 함께 바뀌고, 같은 SO가 고스트 색을 이미 파트 옆에 두므로 파트 옆이 낫다. **사용자 "일단 변경은 하지 않는다"로 지금 구조(A)를 유지한다.** 커밋 뒤에 옮기면 묶음 안의 필드를 바깥 필드로 `FormerlySerializedAs`로 이을 수 없어 에셋 값을 다시 적어야 한다.
- **FC-d:**
  - 주석 둘을 "씬 값" → View DB로 고쳤다: `FieldHighlightLayers.cs`의 `FieldHighlightColor.RockfallWarning` 문서(호진님 줄 `678dbafb7`), `ICombatKnockbackCanvas.cs`의 `FieldKnockbackMarkKind.Pull` 문서(우리 줄).
  - 대신함 한 줄: [공격 궤적 설계](2026-10-19-combat-attack-trajectory-design.md) TJ8(F11 포함), [넉백 미리보기 설계](2026-10-27-combat-knockback-preview-view-design.md) KB6, [궤적 대상별 선 설계](2026-11-14-combat-attack-trajectory-per-target-design.md) PL3·§9.7, [포효 예고 설계](2026-11-10-combat-roar-forecast-design.md) RF4.
  - 호진님 낙석 계획(RP-16)은 고치지 않았다(계획 TP-9). 대신 [호진님 필드 설정 표 메모](../handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md) 새 절 "필드 색이 View DB로"에 적었다(미전달).
  - SO 모드 도구는 필드 설정 표에서 표시 방식·칸 간격·유닛 크기만 이름으로 읽어 색과 무관하다(`Tool/Editor/CombatStageSoAssets.cs` `ReadFieldView`).
- **묶음 끝 검토**(새 에이전트 하나, 읽기 전용): 높음·중간 0, 낮음 8, 약 8.7(SSOT 9.0·SOLID 8.5·FW 9.0·독립성 8.5·캡슐화 9.0·C#/Unity 8.5·If/Tick 8.5). 확인된 것: 바뀐 생성자 호출처·시험·도구 참조 없음, 같은 어셈블리라 `internal` 보임, HEAD `Combat.unity`의 29색·`fillAlpha`가 에셋과 코드 기본값 양쪽과 같음, 에디터 전용 이름이 모두 `#if UNITY_EDITOR` 안, 두 표시기·궤적·화살표 다시 칠하기가 맞음, 구독 두 번·구성 실패·해제 뒤 신호 모두 막힘.
  - **반영:** L5 낡은 툴팁 둘(`StageFieldViewDataSO.cs` 채우기 재질의 "FieldView fillAlpha", 이동 경로 두께의 "Combat 씬 FieldView의 이동 초안 색" → Colors 묶음, 둘 다 우리 줄), L8 `roarForecastColor` 툴팁(포효 카운터 "1 이하", 전투 국면만 — `CombatRoarForecastTracker.CanKeep`). U-8에 재진입 뒤 색이 남는지 더함(L1).
  - **결정 대기(코드 동작):** L1 U-0이 "인스펙터 수정 때 SO 안 객체가 바뀜"이면 DB 표가 옛 객체를 들어 다음 구성 때 옛 색·모양으로 돌아감(추정) — 그때는 표가 매번 SO에서 읽게. L4 에디터 일시정지 중에는 칸 색만 풀 때까지 늦음(궤적·화살표는 바로).
  - **반영(2026-11-14 사용자 "L2, L3, L7 권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음):**
    - L2 다른 필드 설정 SO의 신호를 받던 것: `FieldView`가 구성한 설정을 기억하고(`_editedConfig`), 그 설정과 **같은 SO**에서 온 신호만 받는다. 같은 객체인지만 보면 U-0이 "다른 객체"일 때 쓰는 에셋을 고쳐도 반영이 멈추므로, SO가 자기를 설정에 적어 두고(`GetData`·`OnValidate`가 `BindEditorOwner`·`NotifyEditorChanged(this)`, 저장 안 하는 칸 `_editorOwner`) `StageFieldViewData.IsSameEditorSource`가 같은 객체이거나 같은 SO면 참이다. 받으면 새 객체를 기억한다. 모두 에디터 전용이다.
    - L3 GridOutline 두 번 그리기: `QueueRepaintAll`이 "전체 다시"를 따로 표시하지 않는다(바뀐 칸이 있으면 GridOutline `Repaint`가 `Refresh`를 한 번 부르고, CellPrefab `Refresh`는 할 일이 없어서). 편집 모드는 지금처럼 바로 그린다.
    - L7 핫 리로드·편집 모드 구독: `_listeningToConfigEdits`·`_editedConfig`에 `[NonSerialized]`, `OnConfigEdited` 첫 줄에서 파괴된 필드면 구독을 풀고 끝낸다(편집 모드에서 `Release` 없이 파괴되면 `OnDestroy`가 불리지 않음).
    - 바뀐 줄은 모두 이 작업이 쓴 줄이다(`FieldView.cs`·`StageFieldViewDataSO.cs`, 공유 없음). Unity 확인 9·10을 더했다.
  - **보고만:** L6 `Assets/Scenes/Dev/TraitActivationPreview.unity`의 `FieldView`는 자기 색을 갖고 있었다(`attackRangeColor` (1, 0.484, 0.241) 등, 6칸 없음). 이제 DB 색을 써서 그 미리보기 씬의 색이 Combat 씬 색으로 바뀐다. 남은 씬 YAML 줄은 다음 저장 때 Unity가 지운다.
