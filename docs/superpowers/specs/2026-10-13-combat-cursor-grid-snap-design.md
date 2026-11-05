# 전투 커서 그리드 스냅 설계 (2026-10-13)

- **목표:** 전투 화면에서 배치 Drag·이동 경로 지정·공격 모드 중에는 **OS 마우스 커서를 칸 중심으로 옮긴다**. 플레이어가 지금 어느 칸을 가리키는지 커서 위치만으로 분명히 알게 한다.
- **상태:** 설계 확정(2026-10-13). 진행·검증 기록은 View 계획 §7.74.
- **사용자 결정(2026-10-13):** U1 스냅 대상은 OS 커서 자체(표시만 맞추거나 커서를 숨기는 방식이 아님). U2 필드 밖·UI 위에서는 스냅하지 않는다(지금처럼 자유).

## 1. 지금 동작

| 모드 | 현재 |
|---|---|
| 배치 Drag | 고스트는 필드 안에서 칸 중심(`MoveGhostToCell`), 밖에서는 포인터(`MoveGhostToPointer`). 칸이 바뀔 때만 미리보기 요청 |
| 이동 경로 지정 | 이동 고스트가 포인터의 필드 평면 위치를 그대로 따라감(`UpdateMovementPointer`). 경로 초안은 직전·현재 포인터 사이에 지나간 칸을 기록(`MovementPointerSampler.AppendCrossedCells`), 확정은 칸 안쪽 영역(`IsInside`, `innerCellInset`) |
| 공격 모드 | 고스트 없음. 호버 칸 강조·미리보기 요청은 칸이 바뀔 때만 |
| 커서 제어 | 코드 어디에서도 `WarpCursorPosition`·`Cursor.*`를 쓰지 않음 |

## 2. 핵심 규칙: 칸이 바뀔 때만 옮긴다

포인터가 움직일 때마다 칸 중심으로 되돌리면 커서가 그 칸을 벗어나지 못한다. 그래서 **커서가 가리키는 칸이 직전에 스냅한 칸과 다를 때만** 그 칸 중심으로 옮긴다.

```text
포인터 이동 (스냅 모드 중)
  ├─ 필드 밖 / UI 위 → 스냅 안 함, 기억한 칸 비움 (U2)
  ├─ 같은 칸        → 아무것도 안 함 (칸 안에서는 자유롭게 움직임)
  └─ 다른 칸        → 그 칸 중심 화면 좌표로 커서 이동, 그 칸을 기억
                       (이동이 만드는 다음 포인터 이벤트는 같은 칸 → 다시 옮기지 않음)
스냅 모드 진입 → 커서를 옮기지 않음(G1: 진입 순간 커서가 필드 밖·UI 위일 수 있다). 모드 중 첫 포인터 이동에서 그 칸으로 스냅
스냅 모드가 아닐 때의 포인터 이동 → 기억한 칸 비움(다음 모드의 첫 칸에서 다시 스냅)
```

- **경로 지정과의 호환:** 경계를 넘는 순간의 포인터 이벤트로 지나간 칸이 먼저 기록되고, 그 뒤 칸 중심으로 이동한 이벤트는 같은 칸 안이라 추가 기록이 없다. 확정 판정(`IsInside`)은 칸 중심이 항상 통과한다. 경로 규칙·Core 요청은 바뀌지 않는다.
- **배치·공격과의 호환:** 둘 다 칸 단위로만 판정하므로 동작은 같고, 커서가 칸 중심에 있게 될 뿐이다.

## 3. 구조

```text
CombatInputRouter.Route
  └ 포인터·모드 요청을 기존 소유자에게 분배한 뒤
      CombatCursorSnap.Update(active, ...)        ← 스냅할지·어디로의 유일한 판단 (순수 C#)
         active = 배치 Drag 중 || 공격 모드 || 경로 지정 중
         ICombatPointerPicker.TryPickCell(screen)  (기존)
         ICombatCursorDevice                        (신규, 표시 쪽 구현)
            TryCellCenterToScreen(cell, out screen)  카메라로 칸 중심 → 화면 좌표
            Warp(screen)                 Mouse.current.WarpCursorPosition, 창이 포커스일 때만
```

- **C1 판단 한 곳:** 스냅 여부·대상 칸·"칸이 바뀌었는가" 기억은 `CombatCursorSnap` 하나만 가진다. Router는 요청을 분배한 뒤 호출만 한다. 배치·공격·경로 소유자는 바뀌지 않는다.
- **C2 활성 조건의 원본:** 배치 Drag는 `CombatInteraction.Drag`, 공격 모드는 `CombatInteraction.IsAttackActive`, 경로 지정은 `CombatPathInput`의 지정 중 여부(새 읽기 전용 `IsPlanning`)를 그대로 읽는다. 같은 조건을 다시 만들지 않는다.
- **C3 장치 경계:** Unity 입력 장치 호출은 표시 쪽 구현(`CombatSceneDisplay`/`CombatPointerMapper`) 한 곳에만 둔다. `Mouse.current`가 없거나 창이 포커스가 아니면 아무것도 하지 않는다(다른 창의 커서를 뺏지 않음).
- **C4 수명:** 스냅 객체는 Attach마다 새로 만들고(Router와 같은 수명) Detach와 함께 버린다. 스냅 모드가 아닌 포인터 이동이 오면 기억한 칸을 비운다. 확인창(포기)이 열린 동안은 입력이 막혀 포인터 이동이 오지 않는다. 해제된 표시에 대한 호출은 무시한다(VR4 규칙과 같음).

## 4. 결정 필요

| ID | 질문 | 추천 |
|---|---|---|
| G1 | 스냅 시점 | **확정: 칸이 바뀔 때만.** 모드 진입 즉시 스냅은 하지 않는다(진입 순간 커서가 필드 밖·UI 위일 수 있음, 사용자 결정). 매 이벤트마다 옮기면 커서가 칸을 못 벗어난다 |
| G2 | 스냅 판단을 둘 곳 | **확정: 새 `CombatCursorSnap`(Router가 분배 뒤 호출).** 각 모드 소유자에 넣으면 같은 규칙이 세 곳에 생긴다 |
| G3 | 끌 수 있게 할지 | **확정: `CombatSceneRoot`에 `snapCursorToGrid`(기본 켬)** 직렬화 값. 조작감 확인 중 바로 끄고 비교할 수 있다 |
| G4 | 기록 모드(RecordInputOnly)에서도 스냅 | **확정: 같이 스냅.** 입력 모양은 두 모드가 같다 |
| G5 | 스냅 감도(2026-10-14 추가) | **확정: 별도 값 `CombatSceneRoot.cursorSnapThreshold`(`Range(0, .49)`, 기본 0 = 기존 동작).** 칸이 바뀌어도 새 칸 경계에서 칸 폭의 이 비율만큼 안쪽(`MovementPointerSampler.IsInside(grid, threshold)`, 두 축 모두)에 들어오기 전에는 옮기지 않고 기억한 칸도 유지한다(경계 떨림 없음). 경로 입력의 `innerCellInset`과는 분리한다(그 값은 경로가 칸을 받아들이는 판정이라 손맛 조정이 경로 판정을 바꾸지 않게). 값은 `ICombatCursorDevice.CursorSnapThreshold`로 호출마다 읽어 재생 중 Inspector로 비교할 수 있다. 계약 밖 값은 `CombatCursorSnap`이 NaN·음수 → 0, 0.49 초과 → 0.49로 자른다 |

## 5. 바뀌는 파일(추천안 기준)

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/CombatCursorSnap.cs`(신규) | §2 규칙, 기억한 칸, `PointerMoved(active, x, y)`, `Reset()` |
| `ViewSystem/Combat/ICombatCursorDevice.cs`(신규) | `IsCursorSnapEnabled`·`TryCellCenterToScreen`·`TryWarpCursor`(G1로 현재 위치 읽기는 필요 없음) |
| `CombatPointerMapper.cs` | 칸 중심 → 화면 좌표(`TryCellCenterToScreen`), `Mouse.current.WarpCursorPosition`(포커스일 때만), 연결 수명 규칙 그대로 |
| `CombatSceneDisplay.cs`·`ICombatBattleDisplay.cs` | `ICombatCursorDevice` 위임, `snapCursorToGrid` 꺼짐이면 Warp 안 함 |
| `CombatSceneRoot.cs` | `snapCursorToGrid` (G3) |
| `CombatInputRouter.cs` | `SetCursorSnap`, 포인터 이동을 소유자에게 분배한 뒤 `CombatCursorSnap.PointerMoved(IsCursorSnapMode, x, y)` 호출 |
| `CombatPathInput.cs` | 읽기 전용 `IsPlanning` |
| `CombatViewSystem.cs` | Attach 때 `CombatCursorSnap` 생성·Router에 전달 |
| 테스트 | `CombatCursorSnap` 단위(같은 칸 무시·칸 변경 1회·필드 밖 비움·비활성 무시·모드 진입 즉시·이동 뒤 재이벤트 무시), Router 연결(가짜 장치), PointerMapper 칸 중심 변환(씬) |
| `viewDocs/stage-input-guide.md` | 경로 지정 절에 "커서가 칸 중심으로 이동" 한 줄 |

## 6. SOLID·SSOT·FW 점검

- **S:** 스냅 규칙은 `CombatCursorSnap`, 좌표 변환·장치 호출은 표시 쪽, 모드 판단은 기존 소유자.
- **O/D:** 새 모드가 생겨도 활성 조건 한 줄만 더한다. 스냅은 장치 인터페이스로만 커서를 만진다.
- **SSOT:** 활성 조건은 기존 소유자 상태를 읽기만 한다. 칸 판정은 기존 `TryPickCell` 하나.
- **FW:** 새 서비스·모듈 없음. Framework InputSystem(UnityBridge)의 계약은 바꾸지 않는다(커서 이동은 장치 출력이며 입력 액션은 그대로).

## 7. 위험·확인 방법

- **조작감:** 칸이 큰 화면에서는 경계를 넘을 때 커서가 칸 반 크기만큼 튄다. G3 토글로 비교한다.
- **에디터 Game 뷰:** `WarpCursorPosition`의 좌표 기준이 Game 뷰와 맞는지 재생으로 확인한다(빌드와 다를 수 있음).
- **확인:** EditMode 위 테스트. 재생: 배치 Drag(손패 카드 → 필드 첫 칸에서 중심으로, 칸 이동마다 중심, 손패 영역으로 나가면 자유), 이동(Q → 시작 칸 진입 시 중심, 경로가 이전과 같게 그려짐, 확정), 공격(E 즉시 현재 칸 중심, 칸 이동마다 중심, Direction 조준), 필드 밖·UI 위 자유, 창 포커스를 잃었을 때 커서를 뺏지 않음, G3 끄면 이전과 같음.
