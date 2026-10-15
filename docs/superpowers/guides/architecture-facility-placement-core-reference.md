# 아키텍쳐(시설) 배치 Core 참고 문서

- 대상: 아키텍쳐(시설) 배치 기능을 담당하는 Core 작업자
- 기준 브랜치: `feature/Cheon/CoreSystem`
- 관련 기획서: `스테이지, 맵.pptx`의 시설 파트(8~14 슬라이드)
- 문서 목적: 현재 Core에 존재하는 격자·배치 기능과 데이터 경계를 공유하고, 이후 전투 시스템과 연결할 때 참고할 지점을 정리한다.

이 문서는 구체적인 클래스 구조나 구현 순서를 지정하는 설계서가 아니다. 현재 사용할 수 있는 Core 기능, 그대로 사용하기 어려운 전투 전용 기능, 아직 정해지지 않은 영역을 파악하기 위한 참고 자료다.

## 1. 현재 이해하고 있는 전체 흐름

기획서를 기준으로 한 기본 흐름은 다음과 같다.

```text
스테이지 맵에서 전투할 스테이지 선택
→ 필요하면 시설 버튼으로 아키텍쳐 화면 진입
→ 보유 시설을 Drag & Drop으로 배치·이동·회전·제거
→ 편집 완료 시 배치 상태 저장
→ 전투 스테이지 진입
→ 저장된 시설 배치 결과를 전투 초기화 단계에서 읽음
→ 배치된 시설의 효과를 전투 상태에 반영
```

따라서 아키텍쳐 편집은 실제 전투 `StageSession`이 진행되는 도중의 기능이라기보다, 전투 세션을 만들기 전에 완료하는 준비 단계에 가깝다.

스테이지 선택 화면에서 시설 화면으로 들어간다는 UI 흐름과 시설 배치 데이터의 소유 범위는 구분할 필요가 있다. 시설 배치가 모든 스테이지에서 공통으로 사용되는지, 선택한 스테이지마다 다른지 아직 확정되지 않았다면, 순수 배치 기능이 전투용 `StageConfig`를 반드시 요구하도록 만들 필요는 없다. 추후 스테이지별 보드 또는 제한 규칙이 확정될 때 선택된 `StageKey`나 별도의 시설 보드 설정을 추가로 전달할 수 있다.

## 2. 기획서에서 현재 확인되는 배치 요구사항

시설 파트에서 확인되는 내용은 다음과 같다.

- 기본 배치 공간은 4×4 격자이며 이후 확장될 수 있다.
- 시설은 하나 이상의 정사각형 셀이 연결된 도형이다.
- 시설 Pool에서 Board로 배치하거나, 이미 배치된 시설을 이동하거나, Board 밖으로 빼서 제거한다.
- 배치 공간 밖이거나 다른 시설과 겹치면 배치할 수 없다.
- 신규 배치 실패 시 Pool로 돌아가고, 기존 시설의 이동 실패 시 이전 위치로 돌아간다.
- `R` 입력으로 시계 방향 90도 회전한다.
- Drag 중 가까운 셀에 스냅되고, 예상 점유 셀과 배치 가능 여부를 표시한다.
- 실패 시 실패 사유를 View에 표시할 수 있어야 한다.
- 시설마다 디자이너가 지정하는 기준 타일이 있으며 Drag Pivot으로 사용한다.
- 배치·이동·제거가 완료될 때마다 저장하며, Run 종료 후에도 배치와 강화 상태가 유지된다.
- 전투 중에는 시설 배치를 수정하지 않는다.
- 시설 효과, 획득, 강화, 배치 공간 확장 등의 상세 규칙은 아직 확정되지 않았다.

현재 작업 범위가 순수 시설 배치라면 효과 규칙을 미리 가정하기보다, 유효한 배치 결과를 저장하고 외부에 전달할 수 있는 상태까지 준비해 두는 것으로 충분하다.

## 3. 현재 Core에서 참고하거나 재사용할 수 있는 기능

### `CellCoord`

경로: `Assets/Scripts/ProjectL/Core/Contracts/Common/CellCoord.cs`

정수형 `X`, `Y` 좌표와 덧셈·뺄셈·동등성 비교를 제공한다. 시설 Board의 논리 셀 좌표에도 그대로 사용할 수 있다. Unity의 월드 좌표나 화면 좌표를 `CellCoord`로 변환하는 일은 View 또는 Unity 연결 계층에서 담당하고, Core는 변환된 셀 좌표만 받는 현재 방식을 유지할 수 있다.

### `Rotation4`

경로: `Assets/Scripts/ProjectL/Core/Contracts/Common/Rotation4.cs`

`R0`, `R90`, `R180`, `R270`의 네 방향 회전을 표현한다. 시설의 90도 단위 회전에도 그대로 사용할 수 있다.

### `TileFootprint`

경로: `Assets/Scripts/ProjectL/Core/Domain/Tiles/TileFootprint.cs`

다음 기능이 이미 있다.

- 도형의 Width와 Height 보관
- 로컬 점유 셀 목록 보관
- 도형 내부 범위와 중복 셀 검증
- `Rotation4`에 따른 로컬 셀 회전
- Anchor와 회전을 이용한 실제 점유 셀 계산

시설도 여러 셀로 구성된 도형이므로 이 데이터 표현과 회전 계산을 참고하거나 재사용할 수 있다.

다만 현재 회전은 도형의 바운딩 영역을 기준으로 계산하며, 기획서에 있는 "시설별 디자이너 지정 기준 타일"은 별도로 표현하지 않는다. 시설 Drag에서 기준 타일이 커서를 따라가야 한다면 시설 정의 데이터 또는 시설용 변환 계층에서 Pivot 로컬 좌표를 추가로 다룰 필요가 있다.

### `PlacementValidator`

경로: `Assets/Scripts/ProjectL/Core/Domain/Placement/PlacementValidator.cs`

`TileFootprint`, Anchor, Rotation으로 계산한 모든 셀이 맵 안에 있고 점유 가능한지 검사한다. 시설 배치에서 필요한 경계 검사와 다른 시설과의 중첩 검사도 기본적으로 같은 문제다.

현재 구현은 전투용 `StageMapState`를 직접 받는다. 따라서 검사 방식은 참고하기 좋지만, 시설 Board가 전투 맵의 레일·Entity 점유 상태까지 가질 필요가 없다면 시설용 Board 상태에 맞춰 사용하거나 공통 격자 점유 부분만 분리하는 편이 자연스럽다.

### `StageMapState`의 셀 점유와 원자적 이동

경로: `Assets/Scripts/ProjectL/Core/Domain/Map/StageMapState.cs`

전투 맵에는 다음 기능이 있다.

- 좌표가 맵 안인지 확인
- 셀이 비어 있는지 확인
- 여러 셀 점유와 해제
- `TryRelocateCells`를 통한 다중 셀 이동

특히 `TryRelocateCells`는 기존 점유 셀과 이동 대상 셀이 일부 겹치는 경우를 허용하면서도, 이동이 실패하면 기존 점유 상태를 변경하지 않는다. 시설 이동에서도 유용한 처리 방식이다.

다만 `StageMapState` 자체는 `EntityId`, 레일, 전투 맵 규칙을 함께 가지고 있다. 시설 배치에 그대로 사용할지, 격자 점유 방식만 참고해 시설용 Board 상태를 둘지는 시설 시스템의 독립성과 향후 규칙에 맞춰 판단하면 된다.

### 전투 배치의 Preview와 Commit 흐름

참고 경로:

- `Assets/Scripts/ProjectL/Core/Contracts/Results/CombatPlacementPreview.cs`
- `Assets/Scripts/ProjectL/Core/Application/Deployment/EntityDeploymentService.cs`
- `Assets/Scripts/ProjectL/Core/Application/StageRun/StageSession.cs`

현재 전투 배치는 대략 다음 방식으로 동작한다.

```text
View가 Anchor와 Rotation으로 Preview 요청
→ Core가 예상 점유 셀과 배치 가능 여부 반환
→ View가 Highlight와 Ghost 표시
→ Drop 시 실제 배치 명령 요청
→ Core가 다시 유효성을 검사하고 상태 변경
→ 변경 후 Snapshot으로 View 동기화
```

시설 Drag & Drop도 이 사용 흐름을 참고할 수 있다. Preview는 예상 결과일 뿐 확정 상태를 바꾸지 않고, Drop 시 Core가 다시 검사해 성공한 경우에만 상태를 변경하는 방식이다.

현재 `CombatPlacementPreview`에는 점유 예상 셀과 성공 여부만 있고 구체적인 실패 사유는 없다. 시설 기획에는 실패 사유 표시가 있으므로 시설용 Preview 또는 Result에서는 경계 초과, 다른 시설과 중첩 등의 사유를 구분할 수 있으면 View 연결에 도움이 된다.

## 4. 현재 Core에 존재하지만 시설 상태로 직접 사용하기 어려운 것

### `StructureState`

경로: `Assets/Scripts/ProjectL/Core/Domain/Entities/StructureState.cs`

`StructureState`는 이름은 건물이지만 현재 역할은 전투 맵 안에 존재하는 `CombatEntityState`다. 다음 전투 개념을 포함한다.

- `EntityId`, `EntityKey`, `Owner`
- `Reserve` 또는 `Combat` 위치
- 최대 체력과 현재 체력, 피해와 사망 상태
- 공격력과 공격 가능 여부
- 전투 맵 위 Footprint, Anchor, Rotation

시설 편집 화면의 시설 타일은 전투가 시작되기 전부터 존재하고 Run 종료 후에도 저장될 수 있으므로, 생명주기와 책임이 `StructureState`와 다르다. 순수 시설 배치 상태를 `StructureState`로 표현하면 전투 위치·체력·Owner 같은 불필요한 전제가 함께 들어오게 된다.

반대로 어떤 시설 효과가 전투 맵에 실제 구조물을 생성하는 것으로 확정된다면, 전투 진입 시 시설 데이터를 해석해 `StructureState`를 새로 생성하는 연결은 가능하다. 이 경우에도 저장된 시설 타일 자체와 전투 중 생성된 `StructureState`는 서로 다른 상태로 보는 편이 안전하다.

### `EntityDeploymentService`

경로: `Assets/Scripts/ProjectL/Core/Application/Deployment/EntityDeploymentService.cs`

Footprint 배치·이동 처리에는 참고할 부분이 많지만 다음 전투 규칙에 결합되어 있다.

- `CombatEntityState`
- `ReserveSlotState`
- `StagePhaseState`
- Player/Enemy 배치 구역
- `Reserve`와 `Combat` 사이의 상태 전환

시설 Pool과 시설 Board의 이동은 형태가 비슷하지만 전투 Phase와 Player/Enemy 배치 구역은 필요하지 않다. 따라서 서비스를 그대로 시설 화면에서 호출하기보다 Preview·Commit·실패 시 상태 불변 같은 처리 흐름을 참고하는 용도가 적합하다.

### `StageConfig`

경로: `Assets/Scripts/ProjectL/Core/Contracts/Commands/StageConfig.cs`

현재 `StageConfig`에는 다음과 같은 전투 스테이지 정보가 들어 있다.

- 전투 맵 Width와 Height
- Reserve 크기
- Player/Enemy 배치 깊이
- Timeline Seed
- 초기 적 배치
- 레일 셀
- 스테이지 기믹

시설 배치 공간이나 보유 시설 정보는 포함되어 있지 않다. 현재 기획의 4×4 시설 Board를 전투 맵 크기와 같은 값으로 취급할 근거도 없으므로, 시설 편집 기능을 열기 위해 기존 `StageConfig` 전체를 요구할 필요는 없다.

## 5. 현재 없는 시설 데이터

현재 Core에는 전투용 `StructureState`와 Headquarters용 `EntityKey`가 있지만 다음 시설 전용 데이터는 아직 없다.

- 시설 종류를 식별하는 Key
- 보유한 시설 개체를 식별하는 Id
- 시설 도형과 기준 타일을 정의하는 데이터
- 시설 배치 공간의 크기 또는 확장 상태
- Pool에 있는 시설과 Board에 배치된 시설의 상태
- 저장된 Anchor와 Rotation
- 시설 배치 Snapshot 또는 저장 DTO
- 시설별 전투 효과 정의와 해석 방식
- 시설 강화와 획득 데이터

순수 배치 시스템을 연결하려면 최소한 다음 정보는 외부 데이터에서 전달받을 필요가 있다.

- 시설을 구분할 수 있는 식별 값
- 시설의 로컬 점유 셀 목록
- 시설의 기준 타일(Pivot)
- 현재 보유한 시설 목록
- 시설 Board의 Width와 Height
- 기존에 저장된 배치 위치와 회전 정보

아이콘, 프리팹, 표시 이름 같은 Unity 표현 정보는 Core 배치 판정에 필요하지 않다. Core와 View가 같은 시설을 찾을 수 있도록 식별 값만 공유하면 된다.

## 6. 이후 전투 시스템과 연결할 때 참고할 경계

시설 효과의 상세 규칙이 아직 없으므로, 배치 시스템이 전투 서비스를 직접 호출하거나 전투 Entity를 미리 생성할 필요는 없다. 대신 편집이 끝난 뒤 "현재 유효하게 배치된 시설 목록"을 외부에서 읽을 수 있으면 이후 연결이 가능하다.

전달 결과에는 대략 다음 성격의 정보가 있으면 충분하다. 실제 타입명과 세부 필드는 구현 상황에 맞게 정할 수 있다.

```text
배치된 시설 하나의 결과
- 시설 종류 또는 시설 인스턴스 식별 값
- Anchor 또는 기준 셀
- Rotation
- 필요하다면 현재 강화 단계
```

추천하는 책임 흐름은 다음 정도다.

```text
아키텍쳐 Core
→ 배치 유효성 판정과 확정된 배치 상태 제공

상위 Bootstrap / 게임 진행 계층
→ 배치 상태 저장 및 로드
→ 전투 진입 시 선택된 Stage 정보와 시설 배치 결과 준비

전투 초기화 계층
→ StageConfig와 Unit/Stage 데이터로 StageSession 생성
→ 배치된 시설 종류를 효과 데이터로 해석
→ 초기 능력치 보정, Trait 부여, 기믹 추가, Structure 생성 등
   해당 시설의 실제 효과에 맞는 전투 입력으로 반영
```

여기서 중요한 점은 View가 시설 효과를 직접 계산하지 않는다는 것이다. View는 시설 배치 입력과 화면 표시를 담당하고, 저장된 시설이 어떤 전투 효과가 되는지는 전투 초기화 또는 별도의 Core 해석 단계가 담당하는 편이 기존 Core 경계와 잘 맞는다.

시설 배치 결과를 전투로 넘기는 방식은 이후 효과 기획이 정해졌을 때 선택할 수 있다. 예를 들면 다음과 같은 선택지가 있을 수 있다.

- 전투 진입 전에 시설 결과를 해석해 보정된 Unit/Stage 입력을 만든다.
- 시설 결과를 별도의 전투 Loadout 데이터로 `StageSessionFactory` 주변 조립 단계에 전달한다.
- 시설이 기존 Trait 또는 Stage Gimmick으로 자연스럽게 표현될 때 해당 Spec으로 변환한다.
- 시설이 전투 맵 구조물을 만든다면 초기 배치 정보로 변환한다.

모든 시설을 억지로 하나의 방식으로 변환할 필요는 없다. 시설 배치 시스템은 시설 식별 값과 확정 배치 결과를 안정적으로 제공하고, 실제 효과별 변환은 기획이 정해진 뒤 전투 쪽 연결 계층에서 선택할 수 있다.

## 7. View와 Core 사이에서 참고할 사용 방식

기존 전투 Core는 View가 Domain 상태를 직접 변경하지 않고 `StageSession`을 통해 명령과 Snapshot을 주고받는 방식을 사용한다. 시설 배치도 비슷한 공개 경계를 두면 View 구현이 Core 내부 상태에 의존하지 않는다.

시설 View가 필요로 하는 기능의 성격은 다음과 같다.

- 현재 Board와 Pool 상태 조회
- Drag 중 후보 셀 Preview
- 회전 후 후보 셀 Preview
- Drop 시 배치 또는 이동 요청
- Board 밖 Drop 시 Pool 복귀 요청
- 성공 또는 실패 후 최신 상태 조회
- 실패 사유 조회

메서드 이름이나 Session 구성은 작업자가 선택할 수 있다. 다만 View가 `StageMapState`, 점유 배열, 시설 Domain 객체를 직접 수정하지 않고 Core가 반환한 최신 결과를 기준으로 화면을 갱신하는 방식은 기존 시스템과 일관된다.

## 8. 현재 테스트에서 참고할 수 있는 배치 사례

참고 경로:

- `Assets/Tests/EditMode/TileFootprintTests.cs`
- `Assets/Tests/EditMode/PlacementValidatorTests.cs`
- `Assets/Tests/EditMode/EntityDeploymentServiceTests.cs`

기존 테스트에는 다음 사례가 포함되어 있다.

- 1×1 Footprint의 Anchor 계산
- 가로 두 칸 도형의 90도 회전
- 정상 배치
- 맵 밖 Anchor 거부
- Footprint 일부가 맵 밖으로 나가는 경우 거부
- 이미 점유된 셀과의 중첩 거부
- 기존 셀과 일부 겹치는 자기 자신의 이동
- 다른 Entity와 겹치는 이동 실패
- 실패한 이동에서 기존 위치와 점유 상태 보존
- Preview가 실제 상태를 변경하지 않는지 확인

시설 배치를 추가할 때도 회전 기준 타일, Pool 복귀, 저장 후 복원 같은 시설 고유 사례와 함께 위 불변성을 참고할 수 있다.

## 9. 작업 전에 확인하면 좋은 미정 사항

아래 항목은 순수 배치 구현을 크게 막지는 않지만 데이터 소유 위치에 영향을 줄 수 있다.

- 시설 Board가 계정 또는 전체 Run 공통인지, 선택 스테이지별인지
- 같은 종류의 시설을 여러 개 보유할 수 있는지
- 시설별 회전 허용 여부가 다른지
- 편집 완료 버튼에서 한 번 저장하는지, 각 Drop 성공 시 자동 저장하는지
- Board 확장 상태를 어디에 저장하는지
- 시설 강화 단계가 시설 종류에 붙는지, 개별 시설 인스턴스에 붙는지

이 항목들이 미정이어도 배치 규칙 자체는 구현할 수 있다. 다만 저장 DTO나 시설 식별자를 너무 구체적으로 고정하기 전에 소유 범위만 확인하는 것이 좋다.

## 10. 요약

- `CellCoord`와 `Rotation4`는 시설 배치에서도 바로 참고할 수 있다.
- `TileFootprint`의 다중 셀 도형과 회전 계산을 활용할 수 있지만 디자이너 지정 Pivot은 현재 없다.
- `PlacementValidator`와 `StageMapState.TryRelocateCells`의 경계·중첩·원자적 이동 처리는 시설 배치와 유사하다.
- `StructureState`는 전투 Entity이므로 저장되는 시설 타일 상태로 직접 사용하기에는 책임과 생명주기가 다르다.
- `EntityDeploymentService`는 전투 배치 구역, Reserve, Phase에 결합되어 있어 서비스 자체보다 처리 흐름을 참고하는 편이 적합하다.
- 현재 Core에는 시설 정의, 보유 목록, 시설 Board, 시설 저장 데이터가 없다.
- 시설 편집 결과는 전투 세션 생성 전에 저장하고, 상위 조립 단계가 선택된 Stage 데이터와 함께 전투 초기화 쪽으로 전달하는 흐름이 자연스럽다.
- 시설 효과 계산은 View가 아니라 Core의 전투 초기화 또는 효과 해석 계층에서 담당하는 편이 기존 구조와 맞는다.

