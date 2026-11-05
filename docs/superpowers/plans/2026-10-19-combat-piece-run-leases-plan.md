# 조각 실행 대여 묶음 분리 계획 (카메라 평가 C6·C7)

- 작성일: 2026-10-19. 코드 변경 없음(계획만).
- 사용자 요청 흐름: "카메라 C-6 계획 확인 좀" → 초안 제시 → "1,2번 권장안대로 하고 3번은 AC 보다는 먼저 작업할건데 그 전에 현재 초안 평가 기준에 맞춰서 검토해봐" → 검토(P1~P8) → "권장안대로 하고 계획 문서 작성해줘".
- 출처 발견: [카메라 원칙 평가](../specs/2026-10-19-combat-camera-principles-evaluation.md) C6(중간, `CombatPieceRun` 642줄에 대여까지 다섯 책임)·C7(낮음, 대여가 유닛 채널과 장면 채널 두 모양).
- 관련 규칙 원본: [조각·조립 설계](../specs/2026-10-18-combat-presentation-pieces-design.md) BP1(조각 계약)·BP4(실행), [카메라 연출 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md) K3(장면 채널).
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- 성격: **동작 동일 리팩터링**이다. 화면·완료 시점·예외 문구·정리 순서가 바뀌지 않는다.

## 0. 결정과 착수 조건

### 0.1 결정(2026-10-19, 사용자)

| # | 내용 | 결정 |
|---|---|---|
| 1 | C7(대여 모양 통일)도 함께 | 함께(권장안) |
| 2 | 조각 맥락이 채널을 어디서 받나 | 맥락이 `run.Leases`에서 바로 받음. 실행의 전달 함수 넷 제거(권장안) |
| 3 | 순서 | 클립 AC-3보다 먼저 |
| P1 | 조립 쪽 대여 요청도 목록 하나로 | 합침(권장안) — 장면 채널 `CameraZoom`을 채널 열거에 더하고 조립도 목록 하나 |

### 0.2 착수 조건

1. **카메라 K-4~K-6 커밋 뒤.** 충족 — 사용자 커밋 `c343203d`("기차 카메라 효과 추가").
2. **같은 파일을 고치는 다른 작업이 없을 것.** 고치는 파일: `C/CombatPieceRun.cs`·`C/CombatPieceContext.cs`·`C/CombatComposition.cs`·`T/CombatCameraPieceTests.cs`(사례 추가) + 새 파일 둘. **`T/CombatPieceRunTests.cs`는 고치지 않는다** — 새 시험은 그 안의 시험 가짜(`FakeTargets` 등)를 쓰기만 한다. 클립 AC-2(T11·T12)가 표시 계약에 멤버를 더하며 그 가짜를 고치므로 파일이 겹치지 않게 한다.
   - 병행 확인(2026-10-19): 작업 트리의 진행 중 변경(유닛 호버 공격 범위 — Core 미리보기·Buffer·시험, 공격 궤적·이동 범위 추적 설계 문서, 클립 문서)은 위 파일을 고치지 않는다. 호버 공격 범위 계획·공격 궤적 설계·이동 범위 추적 설계에도 위 파일이 없다.
   - 같은 작업 트리라 MSBuild·관리 실행에 다른 작업의 커밋 전 변경이 섞인다. 그쪽이 컴파일되지 않는 중간이면 검증이 막히므로, 필요하면 "HEAD + 이 계획 파일만" 빌드(다른 파일은 HEAD 판으로 바꿔 넣는 targets)로 검증하고, 전체 비교는 시험 이름으로 이 계획 시험과 남의 시험을 갈라 보고한다.
   - **폴더 정리(M)와는 동시에 하지 않는다**(M은 모든 Combat 파일을 옮김).
3. **클립 계획과의 순서.** 클립 AC-1·AC-2는 실행 파일을 고치지 않아 나란히 가도 된다. AC-3(T16 채널 분기)은 이 계획이 커밋된 뒤 착수한다. 클립 계획 §0.3 9번·T16은 이미 "C6 분리가 먼저 들어왔으면 그 묶음에 한 행"으로 적혀 있어 고치지 않는다(다른 세션 문서).
4. **폴더 정리(M)와의 순서.** M은 Q-M 결정 전이다. 이 계획이 먼저 들어가면 M 대응표 §3 `Presentation/Pieces/` 줄에 새 파일 `CombatPieceRunLeases`를 더해야 한다(Q-M2 B의 "새 파일을 §3에 더해 함께 옮김"). 그 사실을 실행 기록에 남기고, M 문서는 M 착수 때 고친다.

## 1. 지금 코드 (HEAD `339efa14`, K-4~K-6은 이 파일들을 고치지 않음)

| 덩어리 | 위치(`C/CombatPieceRun.cs`) | 내용 |
|---|---|---|
| 대여 기록 | 52–59, 72, 82 | `Lease` 구조체(크기·자세·플래시 칸 셋 + 유닛·채널), 목록 `_leases`, 카메라 줌 `_cameraZoom` 따로 |
| 빌리기 | 314–389 | `TryBorrow`: 유닛 채널 `switch`(셋) + 모르는 채널 예외, 하나라도 실패하면 되돌림. `TryBorrowCameraZoom`: 유닛 대여 뒤 줌, 실패 시 유닛 대여까지 되돌림. 예외·false 처리 모양이 두 함수에 반복 |
| 접근자 | 284–312 | `ScaleOf`·`PoseOf`·`FlashOf`(같은 선형 검색 셋)·`CameraZoom()`, 못 찾으면 채널별 문구의 `InvalidOperationException` |
| 되돌리기 | 617–640 | 유닛 `switch`(셋, 빌린 순서) → 목록 비움 → 줌(맨 끝) |
| 호출 | 145, 577, 613 | `TryStart`의 `TryBorrow`, `Finish`·`Stop`의 `RestoreLeases` |

- 조립 `C/CombatComposition.cs`: 유닛 채널은 `Lease(unit, channel)` → `Leases` 목록, 카메라 줌은 `LeaseCameraZoom()` → `LeasesCameraZoom` bool. 채널 열거 `CombatPieceChannel`은 `Scale = 1`·`Pose = 2`·`Flash = 3`.
- 소비자: 조각 맥락 `C/CombatPieceContext.cs` 31–40행(`_run.ScaleOf` 등) 한 곳. 조립 요청을 읽는 곳은 실행 하나(막기 규칙·재생기는 읽지 않음).
- 시험 결합: 리플렉션으로 대여 상태를 읽는 시험은 없다. `CombatPieceRunTests` 364–371행은 실행 생성자 인자 null 검사(창구·카메라 포함), `CombatCameraPieceTests`는 `LeaseCameraZoom()`·`LeasesCameraZoom`을 쓴다.
- 클립 계획 T16의 기대: 이 분리 뒤 `Clip = 4` 채널은 "묶음에 한 행(빌리기 `TryBeginClip`·되돌리기 `RestoreClip`·꺼내기)".

## 2. 설계

### 2.1 새 클래스 `C/CombatPieceRunLeases.cs`

```text
internal sealed class CombatPieceRunLeases
    생성자(CombatUnitPresentationWindow window, CombatCameraWindow camera)
    bool TryBorrow(CombatComposition composition, Action cancelled)   // 전부 빌리거나 아무것도 안 빌림
    ICombatUnitScale  Scale(EntityId unit)
    ICombatUnitPose   Pose(EntityId unit)
    ICombatUnitFlash  Flash(EntityId unit)
    ICombatCameraZoom CameraZoom()
    void Restore(ref List<Exception> errors)                          // 빌린 순서대로, 목록 비움
```

- **한 모양 기록(C7, P2):** 빌린 것 하나 = (유닛, 채널, 대여 객체, 되돌리기 동작). 카메라 줌도 같은 목록의 항목이다(유닛 = 비어 있음). 되돌리기는 목록을 차례로 부르기만 하고 `switch`가 없다.
- **채널 하나 = 한 행(P2):** 빌리기의 `switch` 한 칸이 창구의 `TryBegin…`을 부르고, 성공하면 그 자리에서 대여 객체와 되돌리기(`Restore…`)를 함께 기록한다. 접근자는 같은 찾기 함수 하나를 쓰는 한 줄이다. 그래서 클립의 `Clip` 채널은 열거 값 + 한 칸 + 접근자 한 줄이다.
- **과한 추상화 금지(FW §27):** 채널 표·채널 인터페이스·제네릭 접근자는 만들지 않는다. 구체 클래스 하나와 `switch` 하나다.
- **할당(FW §19, P7):** 되돌리기 동작은 대여마다 대리자 하나다. 실행(사건)마다 생기고 프레임마다는 생기지 않는다.

### 2.2 실행 `C/CombatPieceRun.cs`

- 생성자 서명은 그대로다(창구·카메라 null 검사 시험 유지). 안에서 `Leases = new CombatPieceRunLeases(window, camera)`를 만든다.
- `internal CombatPieceRunLeases Leases { get; }`를 연다(결정 2).
- `TryStart`는 `Leases.TryBorrow(_composition, OnLeaseCancelled)`, `Finish`·`Stop`은 `Leases.Restore(ref errors)`를 부른다.
- 지우는 것: `Lease` 구조체, `_leases`, `_cameraZoom`, `ScaleOf`·`PoseOf`·`FlashOf`·`CameraZoom()`, `TryBorrow`·`TryBorrowCameraZoom`·`RestoreLeases`.
- 시간 규칙·신호·미도착·완료는 그대로 둔다(셋이 맞물린 한 덩어리라 더 나누지 않음, P8). 예상 약 510줄.

### 2.3 조각 맥락 `C/CombatPieceContext.cs`

- `Scale`·`Pose`·`Flash`·`CameraZoom`이 `_run.Leases.…`를 부른다. 맥락 생성자는 그대로다(P4).

### 2.4 조립 `C/CombatComposition.cs` (P1)

- `CombatPieceChannel`에 장면 채널 `CameraZoom = 101`을 더한다. **장면 채널은 100번대**로 둔다. 유닛 채널 번호를 비워 두어 클립 계획 T16의 `Clip = 4`가 그대로 맞는다.
- 장면 채널 판정은 조립 파일의 함수 하나(`IsSceneChannel(channel)`, `CameraZoom`이면 true)에 둔다.
- `Lease(unit, channel)`: 장면 채널이면 `ArgumentException`("장면 채널은 전용 함수로 빌린다")을 던진다. 그 밖의 검사는 그대로다.
- `LeaseCameraZoom()`: 목록에 (비어 있는 유닛, `CameraZoom`)을 한 번 더한다. 두 번째 호출 예외 문구는 그대로다.
- `LeasesCameraZoom`: 목록에서 파생한다(bool 필드 제거). 공개 모양과 기존 시험 단언은 그대로다.
- `Leases` 목록에 장면 채널 요청이 함께 들어간다. 이 목록을 읽는 곳은 대여 묶음 하나다.

### 2.5 동작 동일 규칙 (P3, 새 시험이 하나씩 확인)

| # | 규칙 | 지금 근거 |
|---|---|---|
| R1 | 빌리는 순서: 유닛 채널을 조립에 적은 순서대로, **장면 채널은 그 뒤**. 조립에 적은 순서가 줌 먼저여도 같다(빌리기에서 두 번 돈다) | `TryBorrow` 끝에서 `TryBorrowCameraZoom` |
| R2 | 되돌리는 순서: 빌린 순서 그대로(유닛들 → 줌). 역순으로 바꾸지 않는다 | `RestoreLeases` |
| R3 | 빌리다 예외: 이미 빌린 것을 되돌린다. 되돌리기 오류가 없으면 원래 예외를 그대로, 있으면 원래 예외를 맨 앞에 둔 `AggregateException` | 두 `catch` |
| R4 | 못 빌림(false): 이미 빌린 것을 되돌리고, 되돌리기 오류만 `CombatSceneEventPresenter.CleanupFailureMessage`로 던진다. 오류가 없으면 false | `ThrowIfAny` |
| R5 | 모르는 유닛 채널: `[CombatPieceRun] 대여 방법을 모르는 채널입니다: …` 예외(되돌림 뒤) | `default` |
| R6 | 접근자: 빌리지 않은 채널·되돌린 뒤는 지금 문구 그대로의 `InvalidOperationException`(크기·자세·플래시·카메라 줌 각각) | 접근자 넷 |
| R7 | 취소 콜백: 유닛 창구·카메라 창구에 실행의 `OnLeaseCancelled`를 그대로 넘긴다 | `TryBegin…(…, OnLeaseCancelled, …)` |
| R8 | `Restore`는 여러 번 불려도 두 번째부터 아무것도 하지 않는다(목록 비움) | `_leases.Clear()`·`_cameraZoom = null` |

- 예외 문구의 머리(`[CombatPieceRun]`)는 그대로 둔다. 로그·시험 문구가 같아야 동작 동일이다.

## 3. Phase

Phase마다 멈추고 보고한다(Unity 결과를 Phase마다 받을지, L-2 뒤 한 번에 받을지는 착수 때 사용자가 정함).

### L-0 기준선

- L-0.1 착수 조건 §0.2 확인(K-4~K-6 커밋, 같은 파일 작업 없음).
- L-0.2 관리 실행 기준선: 그 시점 HEAD 전체 실행. 실패 목록 고정.
- L-0.3 MSBuild 경고 기준선.

### L-1 대여 묶음 분리 (C6)

| Task | 내용 | 파일 |
|---|---|---|
| L-1.1 | `CombatPieceRunLeases` 생성. 이 Phase는 **지금 모양 그대로 옮긴다**(구조체·`switch` 두 곳·줌 따로). 모양 통일은 L-2 | `C/CombatPieceRunLeases.cs`(+ `.meta`, 새 GUID) |
| L-1.2 | 실행이 대여 묶음을 갖고 부름(§2.2), 옮긴 멤버 삭제 | `C/CombatPieceRun.cs` |
| L-1.3 | 맥락이 `_run.Leases`를 씀(§2.3) | `C/CombatPieceContext.cs` |
| L-1.4 | 새 시험 `CombatPieceRunLeasesTests`(관리 실행, 기존 시험 틀의 `FakeTargets`·`FakeCameraEffects` 재사용): R1~R8 | `T/CombatPieceRunLeasesTests.cs`(+ `.meta`) |
| L-1.5 | MSBuild(새 파일은 targets로 더함), 관리 실행 전체: 기존 시험 상태 변화 0, 검토 에이전트 1개 | — |

### L-2 대여 한 모양 (C7)

| Task | 내용 | 파일 |
|---|---|---|
| L-2.1 | 채널 열거에 `CameraZoom = 101`, 장면 채널 판정 함수, `Lease`의 장면 채널 거절, `LeaseCameraZoom`이 목록에 더함, `LeasesCameraZoom` 파생(§2.4) | `C/CombatComposition.cs` |
| L-2.2 | 대여 묶음의 기록을 한 모양으로, 빌리기 한 `switch`(유닛 채널 셋 + 장면 채널 하나, 빌린 자리에서 되돌리기 기록), 장면 채널은 두 번째로 돈다(R1), 접근자는 찾기 함수 하나(§2.1) | `C/CombatPieceRunLeases.cs` |
| L-2.3 | 시험: 조립의 장면 채널 거절·중복 거절·`LeasesCameraZoom` 파생, R1(줌을 먼저 적은 조립), L-1 시험 그대로 통과 | `T/CombatPieceRunLeasesTests.cs`, `T/CombatCameraPieceTests.cs`(사례 추가만) |
| L-2.4 | MSBuild, 관리 실행 전체(L-0 대비 상태 변화 0), 검토 에이전트 1개 | — |

### L-3 문서·마무리

| Task | 내용 |
|---|---|
| L-3.1 | [카메라 원칙 평가](../specs/2026-10-19-combat-camera-principles-evaluation.md)에 C6·C7 해소 기록(점수 갱신: SOLID 7 → 9 예상, 종합 약 8.3) |
| L-3.2 | [조각·조립 설계](../specs/2026-10-18-combat-presentation-pieces-design.md) BP4의 대여 문단이 새 클래스를 가리키게(규칙은 다시 적지 않음, P5). 카메라 설계 K3의 "장면 채널" 문단에 채널 열거 값 한 줄 |
| L-3.3 | 실행 기록(이 문서 §6), CLAUDE.md 현재 상태 줄, 메모리 |
| L-3.4 | 폴더 정리 계획 §3에 새 파일을 더해야 한다는 사실을 실행 기록에 남김(§0.2-4). 클립 계획은 고치지 않음(§0.2-3) |

## 4. 검증

- **MSBuild:** 오류 0, 기준선 대비 새 경고 0. 새 `.cs` 둘은 Unity가 csproj를 다시 만들기 전까지 targets로 더한다(새 파일만 Compile에 추가).
- **관리 실행:** L-0 대비 기존 시험 상태 변화 0. 새 시험은 모두 관리 실행에서 돈다(Unity 없는 창구·가짜 표시).
- **Unity Test Runner:** `CombatPieceRunTests`, `CombatPieceRunLeasesTests`, `CombatPieceRunPlayerTests`, `CombatPieceSignalTests`, `CombatAbilityCompositionTests`, `CombatHitCompositionTests`, `CombatDeathCompositionTests`, `CombatFlashPieceTests`, `CombatScalePieceTests`, `CombatRisePieceTests`, `CombatCameraPieceTests`, `CombatCameraWindowTests`, `CombatTrainCameraTests`, `CombatUnitPresentationWindowTests`, `BossRoarTests`, 그리고 평소 EditMode 전체.
- **재생:** 보스 포효(커짐·줌인·흔들림·작아짐·줌 복귀), 소환(돌며 등장), 피격 플래시, 사망, 기차(K-6 줌은 실행 밖이라 영향 없음 확인). 화면·완료 시점이 이전과 같아야 한다.

## 5. 위험과 대응

| 위험 | 대응 |
|---|---|
| 빌리기·되돌리기 순서가 바뀌어 겹친 대여(같은 유닛 크기·플래시)의 최종 외형이 달라짐 | R1·R2를 시험으로 고정. L-1은 모양을 바꾸지 않고 옮기기만 해 차이를 L-2 하나로 좁힘 |
| 예외 묶음 모양이 바뀌어 실패 정리 시험(Unity 전용 포함)이 달라짐 | R3·R4 시험. 문구 머리 유지 |
| 장면 채널을 열거에 넣어 유닛 채널 자리에 잘못 쓰임 | `Lease`가 거절(L-2.1), 대여 묶음 `switch`의 모르는 채널 예외 유지 |
| 클립 AC-3과 순서가 어긋남 | §0.2-3. 클립 세션이 AC-3 착수 전에 이 계획의 커밋을 확인 |

## 6. 실행 기록

### 6.1 L-0 기준선 (2026-10-19, 사용자 "L-0부터 진행해, Unity는 L-2 뒤 한 번에")

- 착수 조건: 카메라 K-4~K-6 커밋 `c343203d` 확인. HEAD `927962fb`.
- 같은 작업 트리에서 다른 세션(유닛 호버 공격 범위 등)이 Core 미리보기·Buffer·입력 파일을 고치는 중이었다. 그래서 기준선과 이후 검증은 **"HEAD + 이 계획 파일만" 빌드**로 했다. 다른 세션의 커밋 전 `.cs`는 HEAD 판으로 바꿔 넣고, 그쪽 새 파일은 뺀다(스크래치 도구 `headtargets.sh`, 복사본 이름은 Windows 경로 260자 한도 때문에 번호 + 파일 이름).
  - 그쪽이 작업 중에 파일을 더 바꿔 격리 목록이 두 번 낡았다(목록을 만든 뒤 빌드하는 사이). 빌드 직전에 목록을 다시 만들어 해결했다.
- 기준선(순수 HEAD): MSBuild 오류 0·경고 38(앞 기준선과 같은 파일), 관리 실행 5,719개 = PASS 3,260·NATIVE 2,316·FAIL 94·UNSUPPORTED 49.

### 6.2 L-1 대여 묶음 분리 (C6)

- `C/CombatPieceRunLeases.cs`(새) — 지금 모양 그대로 옮김(구조체·`switch` 두 곳·줌 따로). 실행은 생성자 서명 그대로 묶음을 만들고 `Leases`로 연다. `TryStart`·`Finish`·`Stop`이 묶음을 부른다. 맥락은 `_run.Leases`.
- 실행 642 → 499줄. 쓰지 않게 된 `Contracts.Ids` using 제거.
- 새 시험 `T/CombatPieceRunLeasesTests.cs` 8개.
- **검토 에이전트:** 높음 0, "동작은 HEAD와 글자까지 같다". 중간 1(되돌리기 실패 갈래 미시험, 아래 6.3에서 사용자 결정 A로 해소), 낮음 3 반영(순서 시험을 열거 값 순서와 다르게, 되돌린 뒤 꺼내기 예외, 원래 예외 문구 단언, 취소 콜백 채널 넷).

### 6.3 L-2 대여 한 모양 (C7) + 조립 요청 목록 하나 (P1)

- `C/CombatComposition.cs`: 채널 열거 `CameraZoom = 101`(장면 채널은 100번대), `IsSceneChannel`(규칙 "100 이상" 한 곳, 검토 낮음 1 반영), `Lease`가 장면 채널 거절, `LeaseCameraZoom`이 목록에 더함, `LeasesCameraZoom`은 목록에서 파생.
- `C/CombatPieceRunLeases.cs`: 기록 한 모양(유닛·채널·대여·되돌리기), `TryBorrowOne`의 채널마다 한 칸(빌린 자리에서 되돌리기 기록), 두 바퀴(유닛 → 장면), 꺼내기는 찾기 하나. 166줄.
- 시험: 줌을 먼저 적은 조립(R1), 조립의 장면 채널 요청·거절·"100번대 ⇔ 장면 채널"(`T/CombatCameraPieceTests.cs` 사례 추가).
- **계획과 다른 점(사용자 결정 A, 2026-10-19):** 되돌리기 실패 갈래(R3 묶음 예외·R4 정리 오류) 시험을 위해 `T/CombatPieceRunTests.cs`의 시험 가짜에 플래그를 더했다(`FakeTarget.ThrowOnScaleRestore`·`FakeScale.ThrowOnRestore`, 네 줄). 계획 §0.2는 이 파일을 고치지 않는다고 했다. 클립 AC-2가 같은 파일의 다른 가짜를 고치므로 그 세션은 이 두 멤버를 남겨야 한다. 새 시험 2개.
- **검토 에이전트:** 높음·중간 0, "동작 동일이 맞다". `Leases`를 읽는 곳은 묶음뿐이고, 채널 `switch`도 하나, 채널 값 열거·직렬화도 없다. 낮음 4 중 2 반영(장면 채널 판정을 "100번대" 규칙으로, 묶음 주석에 장면 채널 추가 방법). 2건은 두었다(장면 채널의 빈 유닛 약속이 두 파일에 있음, 되돌리기 중 재진입 대비 복사 — 재진입 길이 막혀 있어 차이 없음).
- **검증(최종 빌드 l3, 격리):** MSBuild 오류 0·새 경고 0. 관련 시험 148개 PASS. 관리 실행 전체: L-0 기준선(5,719개) 대비 **기존 시험 상태 변화 0**, 새 시험 12개 모두 PASS(묶음 11·조립 1) — 5,731개 = PASS 3,272·NATIVE 2,316·FAIL 94·UNSUPPORTED 49.
- **Unity:** 사용자 확인 완료(2026-10-19, "유니티 확인 완료"). 그 뒤 바뀐 것은 시험 가짜 플래그·새 시험 둘, `IsSceneChannel`을 같은 결과의 "100 이상" 규칙으로, 주석·시험 사례 하나뿐이다. 다음 Unity 실행 때 `CombatPieceRunLeasesTests`·`CombatCameraPieceTests`·`CombatPieceRunTests`를 함께 보면 된다.

### 6.4 L-3 문서

- 조각·조립 설계 BP4 자원 행에 대여 묶음 한 문장(규칙은 다시 적지 않음), 카메라 설계 K3에 장면 채널 열거 한 줄.
- 카메라 원칙 평가에 3차(C6·C7 해소) 기록.
- 폴더 정리 계획 §3 `Presentation/Pieces/` 줄에 `CombatPieceRunLeases`를 더해야 한다(M 착수 때, §0.2-4). 클립 계획은 고치지 않았다(T16이 이미 이 경우를 적음). 단 클립 AC-2는 위 시험 가짜 플래그를 남겨야 한다.
- 커밋 묶음: `C/CombatPieceRunLeases.cs`·`T/CombatPieceRunLeasesTests.cs`(+ `.meta` 둘), `C/CombatPieceRun.cs`·`C/CombatPieceContext.cs`·`C/CombatComposition.cs`, `T/CombatCameraPieceTests.cs`·`T/CombatPieceRunTests.cs`, 문서. 같은 작업 트리의 다른 세션 변경(Core 미리보기·입력·`CombatInteraction`·`CombatViewSystem` 등)은 이 묶음이 아니다.
