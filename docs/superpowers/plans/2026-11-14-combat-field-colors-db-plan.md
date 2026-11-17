# 전투 필드 색 View DB 이동·에디터 실시간 반영 Task 계획 (2026-11-14)

> 설계: [combat-field-colors-db-design](../specs/2026-11-14-combat-field-colors-db-design.md)(FC1~FC5).
> 결정(2026-11-14 사용자 "권장안대로 하고 Task 계획 써줘"): **Q-FC1~Q-FC4 모두 A**, 설계에서 정한 것 P-FC1~P-FC3 그대로.
> 상태: **TP-1~TP-10 그대로(사용자 "그대로, FC-c 까지 진행해"). FC-a~FC-d 완료(2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 기록 §7).** 남은 것: Unity 확인(§6), 호진님 메모 전달.
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DB/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageField/`. 기준 판 HEAD `ca7b33e53`.

## 0. 진행 규칙

- 전투 View 작업이라 MSBuild를 돌리지 않고 새 시험을 쓰지 않는다(CLAUDE.md). 보고마다 "컴파일 미확인"을 적는다.
- Phase가 끝나면 멈추고 보고한다. Unity 확인은 사용자 결정대로 모든 Phase 뒤 한 번(설계 §7)이다.
- 검토는 묶음 끝(FC-d)에 새 에이전트 하나로 한다.
- 고칠 기존 시험은 없다(설계 §1.4: `FieldView`·`FieldHighlightPalette`·`StageFieldViewData`·`FieldKnockbackMarkers`·`FieldTrajectoryLine`을 참조하는 시험 없음). 착수 때 grep으로 다시 확인한다.

## 1. 계획에서 정한 것 (사용자 확인 대상)

- **TP-1.** `FieldColorsData`의 코드 기본값은 지금 코드 기본값이 아니라 **`Combat.unity` 씬 값**(설계 §1.5)으로 둔다. 새로 만든 SO나 칸이 빠진 에셋도 지금 화면과 같게 보이게 하려는 것이다. `pullArrowColor`만 씬 값이 없어 지금 코드 기본값.
- **TP-2.** 읽기 이름은 필드 이름의 Pascal 형(`PlayerZoneColor`·`FillAlpha` …). 팔레트 쪽 이름(`PlayerZone` …)은 그대로 두어 표시기 셋을 고치지 않는다.
- **TP-3.** 팔레트는 `FieldView`가 처음 `Build`할 때 한 번 만들고, 매 `Build`마다 `Use(config.Colors)`로 다시 가리킨다. 표시기는 전투가 바뀌어도 재사용되므로(`EnsureRenderer`) 팔레트 객체를 바꾸지 않고 안의 참조만 바꾼다.
- **TP-4.** 궤적 선은 "마지막에 넣은 막힘 여부" 대신 "마지막에 넣은 색"을 기억해, 색이 다를 때만 ColorSystem에 넣는다. 막힘 여부는 다시 넣기(`ReapplyColor`)용으로 따로 기억한다.
- **TP-5.** 실시간 반영 길은 검사를 다시 돌리지 않는다. 색은 인스펙터 색 선택기가 0~1로 막고 `fillAlpha`는 `[Range(0, 1)]`이 막는다. 부트 DB 생성의 검사(`Validate`)는 그대로 돈다.
- **TP-6.** 새 `FieldColorsData.cs`의 `.meta`는 Unity 기본 형식으로 새 GUID를 넣어 함께 만든다(앞 묶음들과 같은 방식).
- **TP-7.** 에디터 신호는 SO 로드·도메인 리로드 때도 난다(`OnValidate`). 받는 쪽은 필드가 구성돼 있지 않으면 아무것도 하지 않는다.
- **TP-8.** 화살표 다시 칠하기를 위해 `FieldKnockbackMarkers`가 마지막으로 그린 종류 목록을 기억한다(목록 재사용, 할당 없음).
- **TP-9.** 공유: `rockfallWarningColor` 세 줄은 호진님 줄(`678dbafb77`)이라 옮긴 사실을 호진님 필드 설정 표 메모([attack-trajectory-field-view-hojin-share](../handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md))에 새 절로 적는다(미전달). 호진님 낙석 계획 문서(`-Hojin` 문서)는 고치지 않는다. 민형님 줄은 `EnsureRenderer` 서명 한 줄뿐이고 고치지 않는다.
- **TP-10.** 커밋 묶음 권장: FC-a(SO 코드 + 에셋 + `.meta`, 반드시 한 커밋) → FC-b·FC-c(View) → FC-d(문서). 한 커밋으로 묶어도 된다.

## 2. Phase FC-a 값 (화면 변화 없음)

### Ta.1 새 `DB/Item/FieldColorsData.cs`

- `[Serializable] internal sealed class FieldColorsData`. 네임스페이스는 `ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item`(같은 폴더의 `StageFieldViewDataSO.cs`와 같음). 선례는 `CombatPresentationSettings`의 중첩 설정 클래스(`CombatBoardRevealSettings` 등).
- 필드 31개: `FieldView.cs:43-127`의 색 30개와 `fillAlpha`를 같은 이름·같은 `[Header]`·`[Tooltip]`으로 옮긴다. 툴팁 안의 "씬"·"FieldView" 문구는 바뀐 자리에 맞게 고친다. 기본값은 TP-1.
- `moveRangeColor`의 `[FormerlySerializedAs("rangeColor")]`는 옮기지 않는다(씬 이름 이전 흔적이고, 새 SO 칸에는 옛 이름 데이터가 없음).
- 읽기 31개(TP-2).
- `internal void Validate()`: 색마다 성분 넷이 유한하고 알파가 [0, 1], `fillAlpha`가 유한한 [0, 1]. 문구는 칸 이름을 넣은 한국어 `InvalidOperationException`(파일 규칙). 검사 도우미 하나로 반복을 줄인다.

### Ta.2 `DB/Item/StageFieldViewDataSO.cs`

- `StageFieldViewData`에 머리말 "Colors (필드 색, 필드 색 DB 설계 FC1)" + `[SerializeField] private FieldColorsData colors = new();`. 자리는 "Movement Planning" 묶음 뒤(인스펙터 맨 끝).
- 읽기 `public FieldColorsData Colors => colors;`(다른 읽기와 같은 접근 수준).
- `Validate()` 끝에서 `colors`가 null이면 예외, 아니면 `colors.Validate()`.
- 에디터 전용(`#if UNITY_EDITOR`):
  - `StageFieldViewData`에 `internal static event Action<StageFieldViewData> EditorChanged;`와 `internal void NotifyEditorChanged() => EditorChanged?.Invoke(this);`
  - `StageFieldViewDataSO`에 `private void OnValidate() => stageFieldViewData?.NotifyEditorChanged();`
- 클래스 요약에 "색은 `Colors` 한 묶음, 에디터에서 고치면 신호(FC3)" 한 줄.

### Ta.3 `StageFieldView_Default.asset`

- `stageFieldViewData:` 아래 끝에 `colors:` 블록을 적는다. 값은 설계 §1.5(씬 값 31개, `pullArrowColor`는 코드 기본값). 들여쓰기는 같은 블록의 다른 칸과 맞춘다. 줄 끝은 지금 파일 형식(LF) 그대로.

### Ta.4 `.meta`

- `FieldColorsData.cs.meta` (TP-6).

FC-a 끝: 화면 변화 없음(아무도 읽지 않음). 보고: 바꾼 파일, 컴파일 미확인.

## 3. Phase FC-b 옮기기 (화면 변화 없어야 함)

### Tb.1 `V/Display/Field/FieldHighlightPalette.cs`

- 생성자 `FieldHighlightPalette(FieldColorsData colors)`(null이면 예외). 옛 24인자 생성자와 `fillAlpha` 검사는 지운다(검사는 Ta.1로 옮김).
- `internal void Use(FieldColorsData colors)` — null이면 예외.
- 속성은 식 본문으로 묶음을 읽는다(`internal Color PlayerZone => _colors.PlayerZoneColor;`). 이름·문서 주석 그대로.
- 새 속성 7개: `Trajectory`·`TrajectoryBlocked`·`Knockback`·`KnockbackBlocked`·`KnockbackImmune`·`KnockbackCollision`·`PullArrow`(P-FC1).
- `FillAlpha => _colors.FillAlpha`. `ToFill`·`TryGet`·`TryGetZone`은 그대로.
- 클래스 요약: 원본 = View DB `StageFieldViewData.Colors`, 그릴 때 읽음, `Use`로 바꿔 끼움(FC2·FC3).

### Tb.2 `V/Display/Field/FieldTrajectoryLine.cs`

- 생성자 색 인자 둘 → `FieldHighlightPalette palette`(null이면 예외). `_color`·`_blockedColor` 필드 삭제.
- `_shownBlocked`(bool?) → `_shownColor`(Color?) + `_lastBlocked`(bool?) (TP-4). `Show`에서 색 = `blocked ? _palette.TrajectoryBlocked : _palette.Trajectory`, 다를 때만 `SetOverrideData`. 막힘 표지 갈래는 `blocked` 그대로.
- 지금 `_shownBlocked = null`을 두는 자리(구성·해제)는 둘 다 비운다.
- 생성자 문서의 "필드 씬 값" 문구 고침.

### Tb.3 `V/Display/Field/FieldTrajectoryLines.cs`

- 생성자 색 인자 둘 → 팔레트 하나, `AddLine`이 팔레트를 넘김.

### Tb.4 `V/Display/Field/FieldKnockbackMarkers.cs`

- 생성자 색 인자 다섯 → 팔레트 하나. 색 필드 다섯 삭제, `ColorOf`가 팔레트를 읽는다.

### Tb.5 `V/Display/Field/FieldView.cs`

- 색 30개·`fillAlpha`의 `[Header]`·`[Tooltip]`·`[SerializeField]` 줄을 지운다. `cellsRoot`·`DrawOrder` 상수는 남긴다. `using UnityEngine.Serialization;`은 다른 사용처가 없으면 지운다.
- 필드 `private FieldHighlightPalette _palette;`.
- `Build`: `ValidateCellsRoot()` 뒤, `Release()` 앞에서 `if (_palette == null) _palette = new FieldHighlightPalette(config.Colors); else _palette.Use(config.Colors);` (TP-3). 같은 크기라 일찍 돌아가는 갈래(`:176`)보다 뒤라 그때는 바꾸지 않는다 — 같은 크기 재구성은 같은 전투 안이라 참조가 같다.
- `EnsureRenderer`의 `CreatePalette()` 두 곳 → `_palette`. `CreatePalette` 삭제.
- `BuildTrajectory`·`BuildKnockbackMarkers`가 색 대신 `_palette`를 넘긴다.
- 클래스 문서 "강조 색은 이 컴포넌트의 씬 설정에서 읽습니다" → "View DB `StageFieldViewData.Colors`에서".

FC-b 끝: 화면 변화 없어야 함(값 = 씬 값). `Combat.unity`의 옛 색 줄은 남아도 무시된다(Q-FC4 A).

## 4. Phase FC-c 에디터 실시간 반영

### Tc.1 다시 넣기 입구

- `FieldTrajectoryLine.ReapplyColor()`: 보이는 중이고 `_lastBlocked`가 있으면 그 막힘 여부의 지금 팔레트 색을 넣는다(다를 때만). `FieldTrajectoryLines.ReapplyColors()`가 선마다 부른다.
- `FieldKnockbackMarkers`: `List<FieldKnockbackMarkKind> _shownKinds`(TP-8). `Show`에서 비우고 종류를 담고, `Clear`·`Release`에서 비운다. `ReapplyColors()`는 그 목록으로 화살촉마다 `FieldArrowhead.SetColor(ColorOf(kind))`(이미 색 비교함).

### Tc.2 `FieldView` 구독 (`#if UNITY_EDITOR` 묶음)

- `private bool _listening;`과 `Listen(bool on)`: 상태가 다를 때만 `StageFieldViewData.EditorChanged`에 더하거나 뺀다.
- `Build` 성공 끝(`_mapper = mapper;` 뒤)에서 `Listen(true)`, `Release` 맨 앞과 `OnDestroy`에서 `Listen(false)`.
- 받는 함수 `OnFieldConfigEdited(StageFieldViewData current)`:
  1. `_mapper == null || _palette == null || current == null`이면 돌아간다(TP-7).
  2. `_palette.Use(current.Colors)`.
  3. 모든 칸을 바뀐 칸으로 넣고 전체 다시를 표시한다 — 새 private `QueueRepaintAll()`(칸 수만큼 `_pendingMarks`·`_pendingCells`에 넣고 `QueueRefresh()`, 목록은 이미 칸 수만큼 있어 할당 없음).
  4. `_trajectory?.ReapplyColors(); _knockbackMarkers?.ReapplyColors();`
- 비에디터 빌드에는 이 묶음이 없다. 호출 자리(`Listen`)도 같은 `#if` 안에 둔다.

FC-c 끝: 보고(컴파일 미확인).

## 5. Phase FC-d 문서·공유·검토

- 설계 문서: 머리 상태, §8 구현 기록(계획과 다른 점 포함).
- "색은 `FieldView` 씬 값"이라고 적은 우리 설계에 대신함 한 줄: 공격 궤적 설계(TJ8·F11), 넉백 미리보기 View 설계(KB6), 궤적 대상별 선 설계 §9.7·PL3, 포효 예고 설계(RF4 색 칸).
- 호진님 필드 설정 표 메모 새 절(TP-9, 미전달): `StageFieldViewData`에 "Colors" 묶음, 낙석 경고 색 자리 이동, 플레이 중 SO 수정은 에셋에 남음(FC4).
- 사용법 한 줄: 위 메모와 설계 FC4로 갈음(따로 안내 문서 없음).
- CLAUDE.md 현재 상태 줄 갱신.
- 묶음 끝 검토: 새 에이전트 하나(읽기 전용). 컴파일 대조(바뀐 생성자 호출처 grep), 씬 값 옮겨 적기 대조(설계 §1.5와 에셋), 에디터 구독 수명(전투 재진입·씬 종료), 비에디터 빌드에서 `#if` 밖 참조가 없는지.

## 6. Unity 확인 (모든 Phase 뒤 한 번)

설계 §7의 U-0·1~8 그대로.

## 7. 실행 기록

### 7.1 FC-a·FC-b·FC-c (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 사용자 "그대로, FC-c 까지 진행해" → TP-1~TP-10 그대로, 세 Phase를 이어서 함. MSBuild 안 돌림.
- 착수 확인: `Assets/Tests`에 바꾼 형식(`FieldView`·`FieldHighlightPalette`·`StageFieldViewData`·`FieldKnockbackMarkers`·`FieldTrajectoryLine(s)`)을 참조하는 시험 없음. 바꾼 생성자의 호출처는 `FieldView`·`FieldTrajectoryLines` 안뿐(grep).
- FC-a:
  - 새 `DB/Item/FieldColorsData.cs`(+ `.meta`, 새 GUID): 색 30개·`fillAlpha`, 기본값 = 씬 값(TP-1), 읽기, `Validate`. 머리말 순서는 `FieldView` 묶음을 따르되 낙석 경고를 상태·기믹·포효 층 뒤(채우기 앞)로 모음.
  - `StageFieldViewDataSO.cs`: "Colors" 칸·`Colors` 읽기·`Validate` 끝에 묶음 검사, 에디터 전용 `StageFieldViewData.EditorChanged`·`NotifyEditorChanged`, SO `OnValidate`.
  - `StageFieldView_Default.asset`: `colors:` 블록 31줄. 스크립트로 `Combat.unity`의 `FieldView` 값 30개와 한 칸씩 대조해 모두 같음(`pullArrowColor`만 에셋에만, 코드 기본값).
- FC-b:
  - `FieldHighlightPalette`: 생성자 `(FieldColorsData)`, `Use`, 속성은 묶음을 읽는 식 본문, 궤적·화살표 색 7개 추가, `fillAlpha` 검사는 묶음으로.
  - `FieldTrajectoryLine`·`FieldTrajectoryLines`·`FieldKnockbackMarkers`: 색 인자 → 팔레트.
  - `FieldView`: 색 30개·`fillAlpha` 칸과 `using UnityEngine.Serialization` 삭제, `_palette`(처음 구성 때 만들고 매 구성마다 `Use`), `CreatePalette` 삭제, 클래스 문서 고침.
- FC-c:
  - `FieldTrajectoryLine`: `_shownBlocked` → `_shownColor`·`_lastBlocked`, 색 넣기 `ApplyColor` 하나(다를 때만), `ReapplyColor`. `FieldTrajectoryLines.ReapplyColors`.
  - `FieldKnockbackMarkers`: `_shownKinds`(`Show`에서 채우고 `Clear`·`Release`에서 비움), `ReapplyColors`(화살촉 `SetColor`가 같은 색이면 건너뜀).
  - `FieldView`(에디터 전용 `#region`): `ListenToConfigEdits`(구성 성공 끝에서 켜고 `Release` 맨 앞·`OnDestroy`에서 끔), `OnConfigEdited`(구성 전이면 무시 → `Use` → `QueueRepaintAll` → 궤적·화살표 다시 넣기), `QueueRepaintAll`(모든 칸을 바뀐 칸으로 + 전체 다시).
- 계획과 다른 점: 없음. 같은 크기라 `Build`가 일찍 돌아가는 갈래는 팔레트를 다시 가리키지 않는다(같은 세션에서 DB 표는 같은 묶음을 들고 있어 차이 없음, 계획 Tb.5에 적은 대로).

### 7.2 툴팁·FC-d (2026-11-14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 툴팁(사용자 요청): `FieldColorsData` 31칸 툴팁을 "무엇 / 쓰이는 곳 / 그리기" 세 줄로 다시 씀. 값·칸 위치는 그대로. 옛 툴팁과 다른 것은 설계 §8.
- Q-FC1 다시 보기: 사용자 "일단 변경은 하지 않는다" → 지금 구조 유지(설계 §8).
- FC-d(사용자 "FC-d 진행해"): 주석 둘(`FieldHighlightLayers.cs` 낙석 색 — 호진님 줄, `ICombatKnockbackCanvas.cs` 끌어오기 — 우리 줄), 대신함 한 줄 넷(궤적 TJ8·넉백 KB6·대상별 선 PL3·§9.7·포효 RF4), 호진님 메모 새 절(미전달), 설계 §8, CLAUDE.md. 계획과 다른 점: 없음(설계 §7에 적힌 낙석 RP-16은 TP-9대로 고치지 않고 메모로).
- 묶음 끝 검토: 높음·중간 0, 낮음 8, 약 8.7. 반영 L5(SO 툴팁 둘)·L8(포효 예고 툴팁)·U-8 한 줄, 결정 대기 L1·L2·L3·L4·L7, 보고만 L6(Dev 씬 색 바뀜). 내용은 설계 §8.
- 검토 L2·L3·L7 반영(사용자 "L2, L3, L7 권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음): L2 같은 SO에서 온 신호만 받음(`IsSameEditorSource`, SO가 설정에 자기를 적음), L3 `QueueRepaintAll`이 전체 다시를 따로 표시하지 않음, L7 `[NonSerialized]` 둘·파괴된 필드면 구독 풂. 계획과 다른 점: L2를 같은 객체 비교가 아니라 같은 SO 비교로(U-0이 "다른 객체"여도 반영이 멈추지 않게). 설계 §8, Unity 확인 9·10은 설계 §7.
