# 스테이지 내부 입력 — 로컬 세션 인계 문서

> 2026-09-30 로컬 세션(PC: Hyeon2-400)에서 진행한 스테이지 내부 입력 작업을 다른 PC나 새 세션이 이어받기 위한 문서다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [입력 설계 §11](../specs/2026-09-30-stage-input-design.md) → [입력 사용 안내](../viewDocs/stage-input-guide.md)** 순서로 읽는다.
> 대화 기록과 Claude 메모리는 원래 PC에만 있다. 새 세션에 필요한 내용은 이 문서에 옮겼다.
> 이전 작업(CombatViewBuffer)의 인계는 [2026-09-29 인계 문서](2026-09-29-combat-view-buffer-local-session-handoff.md)에 있다.

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당, 로컬 세션)
- 대상 브랜치: `feature/Yonggeun/Stage`
- 상태: **입력 작업 구현 완료(계획 Phase 0~5).**
  - Phase 3·4의 컴파일과 EditMode 테스트 결과는 아직 받지 않았다.
  - 다음 작업은 **전투 View 설계**다.

## 1. 한눈에 보기

| 항목 | 내용 |
|---|---|
| 목표 | Framework `InputSystem`에 없던 Unity 연결 계층(장치 수집·Layer 저장·한 번 Configure)을 만들고, 스테이지 내부(배치·전투) 입력을 "무엇을 했나"로 View에 전달한다 |
| 결과 | `UnityBridge` 연결 계층, 스테이지 입력(Controller 2개, 수신 슬롯, Layer 2개), 입력 설정 View DB 테이블, `ClearLayer` 추가, 테스트 3종 |
| 기준 문서 | [설계](../specs/2026-09-30-stage-input-design.md)(§11 구현 결과) · [계획](../plans/2026-09-30-stage-input-plan.md) · [View 작업자용 사용 안내](../viewDocs/stage-input-guide.md) · [민형님 공유](2026-09-30-stage-input-framework-share.md) |
| 함께 한 일 | ViewGroup에 빠져 있던 `TraitVisual`·`RecoveryOptionVisual`·`RailwayVisual` 테이블과 아이템([DB 스키마 정렬 계획](../plans/2026-09-29-db-schema-core-alignment-plan.md) T12 일부). 천명님 `CombatStateBuffer` 병합(`e502a4da`) |

## 2. 작업 방식

규칙 전체는 `CLAUDE.md`에 있다. 아래는 **CLAUDE.md에 없고 원래 PC의 메모리에만 있던 것**이다. 이전 인계 문서 §2의 내용(스테이지 내부 담당, `CombatLifeCycle`/`StageLifeCycle` 이름 구분, 워크시트 기본)도 그대로 유효하다.

- 사용자는 이번 작업의 Phase를 대부분 **"너가 진행해줘"**로 맡겼다. 새 Phase를 시작할 때는 여전히 워크시트로 할지 직접 할지 묻는다.
- **Phase 보고 끝에 다음 Phase를 자세히 설명한다**(2026-09-30 사용자 요청): 목표, 만들거나 고칠 파일, 핵심 동작과 주의점(공용 파일, 에디터 작업, 결정 필요 여부), 확인 방법.
- 사용자가 "기존 계획을 날리고 처음부터"라고 하면 **참고하지 않을 뿐, 문서는 지우지 않는다**(2026-09-30 확인).
- 사용자가 "파일은 새로 작성"이라고 하면 기존 문서를 고치지 않고 새 파일을 만든다.
- 테스트 방식 결정: 순수 C#과 장치 없는 Unity 연결은 EditMode 테스트로 확인한다. **가상 장치 테스트는 보류(B)**했고, 부트스트랩이 생긴 뒤 재생으로 확인한다.
- 이름 충돌을 피한 결정
  - 연결 계층 폴더는 `UnityBridge`(네임스페이스 `…InputSystem.Unity`가 `Unity.*`를 가림)
  - DB 폴더는 `InputSettings`(`…Table.Input`이 `UnityEngine.Input`을 가림)
  - Unity 쪽 `InputSystem`을 부를 때는 전체 이름이나 별칭을 쓴다

## 3. 커밋 이력 (`feature/Yonggeun/Stage`)

| 커밋 | 내용 |
|---|---|
| `e502a4da` | 천명님 `origin/feature/Cheon/refactor/core-system`(`1f833e1`) 병합. 충돌 5곳 해결 |
| `478e2a5d` | CLAUDE.md 갱신(Buffer 인계) |
| `690fdc2a` | 입력 Phase 0~2: 설계·계획·공유 문서, 액션 에셋, `ClearLayer`, StageInput 전부, `StageInputTests` |
| (미커밋) | 입력 Phase 3~5: asmdef 참조, 입력 설정 DB·에셋, `UnityBridge`, 공용 조립 파일, `UnityStageInputTests`, ViewGroup 테이블 3개, 문서 |

## 4. 알아 둘 것

**입력 구조** (자세한 설명과 예시 코드는 [사용 안내](../viewDocs/stage-input-guide.md))

- View는 Unity 입력을 직접 읽지 않는다. `IStageInputReceiver`를 구현해 수신 슬롯에 `Bind`하고, `ChangeLayer`/`ClearLayer`로 받을 입력을 고른다.
- `InputSystem.Configure`는 런타임에 한 번이다. `UnityInputBridge`가 `InitData(IHasViewQuery)`에서 View DB의 `InputSettings` 에셋을 복제·검증한 뒤 호출한다.
- Controller·Layer 목록은 루트(`RunTimeSystemFactory`)가 만들어 Global → Input 팩토리로 넘긴다. 수신 슬롯은 루트 서비스(`StageInputKey.ReceiverSlot`)다.
- 선택 버튼은 에셋의 `Press(behavior=1)`(ReleaseOnly) 계약이다.
  - 정상 뗌은 `SelectReleased`, 강제 종료는 `SelectCanceled`로 구분한다.
  - `SelectCanceled`에서는 명령을 기록하지 않는다.
- 입력 설정이 없거나 에셋과 코드가 맞지 않으면 초기화에서 예외로 멈춘다(I9).

**Core 쪽 상태** (천명님 `1f833e1` 병합 뒤)

- `ICombatStateBufferReadSink`가 우리 요청 R1~R4의 읽기 자리를 제공한다: `LatestResultSequence`, `HandledCommandNumber`, `AnsweredPreviewNumber`, `Phase`(`CombatFlowPhase`: None/PlayerInput/Automatic/Finished).
- 하지만 값을 올리는 처리 서비스가 아직 없다. 결과 사건 목록(R5)과 처리 서비스(R6)도 없다.
- 그래서 "명령 수락을 보고 Layer 전환", "처리된 명령 지우기", "연출 확인"은 실제로 연결할 수 없다.

**DB 상태**

- `ViewGroup.asset` 테이블은 10개로 팩토리 요구 수와 같다(InputSettings, TraitVisual, RecoveryOptionVisual, RailwayVisual 추가).
- `InGameGroup.asset`은 9개로, 팩토리는 12개를 요구한다(CategoryConfig·StageEconomy·RecoveryOption 없음, DB 스키마 정렬 계획 T11). 그래서 `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`는 **여전히 실패**한다.

**기타**

- 런타임 부트스트랩이 없다. 에디터 재생으로 입력을 확인할 수 없다.
- 프로젝트 설정의 전역 액션이 Unity 기본 템플릿(`InputSystem_Actions`)이라 재생 때 자동으로 켜진다(받는 코드는 없음).
- 씬의 UI 입력은 EventSystem의 `InputSystemUIInputModule`(Unity `DefaultInputActions`)이 따로 처리한다.

## 5. 남은 일 (우선순위 순)

### 5.1 검증 결과 받기

- 에디터 컴파일(Phase 3·4)
- EditMode 테스트
  - `StageInputTests` 18건
  - `UnityStageInputTests` 14건
  - `ViewDataTableTests`(입력 설정 3건 포함)
- 결과를 [설계 §11.4](../specs/2026-09-30-stage-input-design.md)에 반영한다.
- `DataBaseAssetTests`는 T11 때문에 실패하는 것이 맞다. 실패 원인이 InGame 테이블(`CategoryConfigDataTableSO` 등)인지 확인한다.

### 5.2 전투 View 설계 (다음 작업, 새 설계·계획 문서)

- 누가 `IStageInputReceiver`를 구현하나. `InGameViewSystemGroup`(지금 비어 있음)에 어떻게 등록하나. 씬 객체(카메라, 필드)와 어떻게 연결하나.
- 포인터 → `CellCoord` 변환(ViewGroup `StageFieldView`)
- 드래그·호버·하이라이트: 옛 `develop-4week` View 코드 중 무엇을 가져올지. [매핑 문서](../specs/2026-09-28-stage-view-buffer-mapping.md)의 R/C/Q/F/E/V/L 분류를 기준으로 한다.
- 명령 기록(`CombatViewBuffer.Submit`/`SetPreview`)과 Layer 전환 호출(D4: "전투 시작" 수락 뒤 Combat)
- UI 위 포인터 차단(I8)
- 권장: 범위를 입력과 명령 기록까지로 좁히고, 연출은 뒤로 미룬다.
- 확인용 최소 부트스트랩을 넣을지 이 설계에서 정한다.

### 5.3 그 밖

- DB 스키마 정렬 계획 T11(InGame 테이블 3개)과 T12 나머지(UnitVisual 한글 이름·새 유닛 4종 등): 카탈로그 통과의 선행 조건
- 가상 장치 테스트(보류 B): 부트스트랩 뒤 재생으로 누름·뗌, 누른 채 Layer 전환, 종료 시 취소 확인
- 프로젝트 전역 액션을 비우거나 `PlayerInputActions`로 바꿀지 결정
- 민형님께 반영 결과 공유: Phase 0 문서는 "변경 예정"이었다. 폴더명 `UnityBridge`, `StageInputServiceKey.cs` 새 파일, `framework-programmer-guide.md`에 `ClearLayer` 반영 요청 등
- 천명님께 전달: `2026-09-29-combat-view-buffer-core-requests.md`(R1~R6)

## 6. 검증 방법

- 컴파일: Unity 에디터 Console 에러 0
- 테스트: Test Runner → EditMode → `ProjectL.Tests.Framework`
- DB 에셋을 바꿨다면 `DataBaseAssetTests`

## 7. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-30-stage-input-local-session-handoff.md 부터 읽어.
입력 테스트 결과는 ○○였어. 이어서 전투 View 설계 시작하자.
```
