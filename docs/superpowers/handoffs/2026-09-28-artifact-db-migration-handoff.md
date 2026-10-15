# Artifact DB 부분 이전 및 Buffer 인계

- 상태: **부분 이전 완료, 표현 데이터 저작 및 Unity EditMode 실행 대기**. 전체 `Artifact DB 이전 1차 완료`로 표시하지 않는다.
- 대상: `feature/Hojin/DB_SO_2`의 사용자 작성 SO/Query/Factory 코드에 에셋과 검증을 추가.
- 원본: `feature/develop-4week-Hojin`, 고정 커밋 `ae07af37e9e897e4462c102b67d6b05f7a0bb8db`.
- 결정: 사용자 승인에 따라 유효한 데이터부터 등록하고 문제 항목은 원본 보존 및 보류 목록에 기록한다. 효과 값·아이콘·모델을 임의로 대체하지 않는다.

## 이전 결과

### 2026-10-02 현재 결정: 컴파일 호환성을 위한 Snapshot 계약 복원

- 사용자 결정: 현재 Snapshot 버전을 유지한다. 이후 문제가 생겼을 때 원인 후보로 추적할 수 있도록 이 복원 이력을 보존한다. 아래 9월 30일 목록 방식 복원은 과거 기록이며 현재 계약이 아니다.
- 원인: 머지 `581ca450`에서 목록 방식 `ArtifactBuffer.Artifacts`와 `CombatProcessingService`의 `ArtifactReadSink.Current.PlacedItems` 사용이 함께 남아 CS1061이 발생했다. View 입력용 `ArtifactViewBuffer`나 입력 Layer가 원인은 아니다.
- 복원 기준: 해당 머지의 두 번째 부모인 용근 브랜치 버전. `ArtifactBuffer`의 `Current` / `SetSnapshot`, `ArtifactBufferSnapshot`, 개별 상태 `ArtifactData`를 복원했다. 이 Snapshot 계약의 최초 추가는 호진 커밋 `8f38e2c1`이며, Core 작업자가 새로 설계했다고 단정하지 않는다.
- 목적은 현재 Core 호출부와의 컴파일 호환성이다. 이전 구조보다 우수하다는 판단이나 운영 동작 검증 완료를 의미하지 않는다. Core 소유 상태 Buffer와 View 소유 입력 Buffer는 별도로 유지한다.
- 검증: Unity 프로젝트 응답 파일과 컴파일러로 Systems 및 기존 Framework 테스트 어셈블리 컴파일 통과. 기존 테스트의 API 사용과 ResetSpy의 View Buffer 접근자 누락도 맞췄다. 새 테스트 파일은 추가하지 않았다. Unity 재생 및 테스트 실행은 미확인이다.
- 후속 문제 조사 시 확인: Core 상태가 Snapshot으로 게시되는 시점, 보유/배치 목록의 최신성, Clear·Run 초기화 시 보존 범위, 복사 비용, View 입력의 승인/거절 결과 연결. 이는 확인 항목이며 현재 결함으로 확정한 내용은 아니다.
- View 작업 중 오류를 없애기 위해 이 Core 상태 계약을 다시 임의로 변경하지 않는다. 계약 변경이 필요하면 Core 담당자와 소비 코드를 함께 확인한다.

### 2026-09-30 아티팩트 View 입력 Buffer 추가

- 사용자 승인에 따라 `ArtifactViewBuffer`를 별도로 추가했다. 기존 Core 상태용 `ArtifactBuffer`는 아래 복원 상태를 유지한다.
- `BufferSystemKey.Buffer`의 `ArtifactViewReadSink` / `ArtifactViewWriteSink`로 접근한다. Core 처리 계층은 읽기 계약만 사용하고 View는 기록·조회·정리를 담당한다.
- 현재 Core API에 맞춰 `Place(artifactId, CellCoord, Rotation4)`는 종류 ID, `Move(instanceId, CellCoord, Rotation4)`와 `Return(instanceId)`는 Core가 발급한 배치 개체 ID를 사용한다. View에서 개체 ID를 발급하지 않는다.
- `Submit`은 요청 번호를 발급하고 목록에 보관만 한다. `Commands`와 `TryGetNextCommand`는 읽기 전용이며, View가 결과를 확인한 뒤 `RemoveCommand(number)`로 해당 요청만 지운다.
- 요청 번호는 이 Buffer 인스턴스 안에서 증가하며 삭제 후에도 재사용하지 않는다. 다른 Buffer와 결과를 공유할 때는 Buffer 종류도 구분해야 한다. `handledThrough`는 순서대로 처리 완료된 마지막 번호를 의미한다.
- Core 소비·중복 작업 등록 방지·승인/거절 결과 보관·화면 전환 시 미처리 요청 정책은 후속 연결 사항이다. 입력 Controller 및 실제 View와도 아직 연결하지 않았다.
- 기존 두 입력 Buffer 제안에서 아티팩트 입력을 분리한 변경이므로 Core 작업자에게 이 읽기 계약을 공유해야 한다. 문서 작성은 외부 전달 완료를 의미하지 않는다.

### 2026-09-30 Buffer 변경 선택 복원 — 현재 계약

- 사용자 요청에 따라 Snapshot 도입 전 커밋 `6d734ac4`의 `ArtifactBuffer.cs`와 `ArtifactData.cs`를 복원했다.
- 읽기는 `IReadOnlyList<ArtifactData> Artifacts`, 쓰기는 `Add(ArtifactData)`와 `Clear()`를 사용한다. `Current`와 `SetSnapshot`은 더 이상 제공하지 않는다.
- `ArtifactData`는 당시의 필드 없는 연결용 더미 클래스다. 실제 데이터 계약은 Core 담당 영역에서 정해야 한다.
- Core의 `ArtifactState`, 아티팩트 DB·SO 및 스테이지 선택 작업은 유지했다. 기존 진행 테스트의 아티팩트 보존 검증은 목록 계약에 맞췄다.
- 사용자 요청으로 이번 대화에서 추가한 Editor 전용 `ArtifactBufferTests`, `StageViewBufferTests`, `StageSelectionInputTests`와 해당 `.meta`를 삭제했다. `UnityStageInputTests`에 추가했던 맵 선택 검사도 제거했으며, 다른 기존 테스트는 유지했다.
- 아래 9월 29일 API와 테스트 설명은 과거 기록이며 현재 계약으로 사용하지 않는다.

### 2026-09-29 Buffer API 수정

- 사용자 요청으로 `ArtifactBufferSnapshot`과 해당 `.meta`를 제거했다. Core의 `ArtifactState`와 `ArtifactItemState`는 수정하지 않았다.
- `IArtifactBufferReadSink.Current`는 `ArtifactState`를 반환하고, `IArtifactBufferWriteSink.SetSnapshot(ArtifactState)`는 전달받은 상태 참조를 그대로 보관한다. 추가 배열 복사는 하지 않는다.
- 초기 상태와 `Clear()` 후의 `Current`는 `null`이다. 소비자는 `Current != null`로 초기화 여부를 확인한다. null 입력과 양수가 아닌 보드 크기는 기존처럼 거부한다.
- 아래의 Empty 반환 및 배열 분리 테스트 설명은 9월 28일 구현 기록이다. 현재 테스트는 Core 상태 직접 보관, 전체 교체·초기화, 잘못된 입력 거부를 검증한다.
- 원본 배열이 반드시 변경된다는 요구사항은 없다. 기존 배열 복사는 방어적 구현이었으며 Core 변경을 요구하는 근거가 아니다. Core → Buffer → View 실행 연결은 여전히 후속 작업이다.

| ID | 원본 | 규칙 등록 | 표현 등록 / 보류 사유 |
|---|---|---|---|
| 1001 | Artifact_HealingChalice | 등록: Single 1칸, 초기 수량 1, 회복량 10 | 표현 SO 생성(이름·ID 입력), 아이콘 미지정으로 테이블 등록 보류 |
| 1002 | Artifact_ThornArmor | 등록: Domino 2칸, 초기 수량 1, 피해량 3 | 표현 SO 생성(이름·ID 입력), 아이콘 미지정으로 테이블 등록 보류 |
| 1003 | Artifact_SoulDrop | 등록: TShape 4칸, 초기 수량 1, 기존 ModifyStat 효과 2개 | 표현 SO 생성(이름·ID 입력), 아이콘 미지정으로 테이블 등록 보류 |
| 1004 | Artifact_LastResistance | 보류: Empty 형태, ModifyStat의 statId 누락 | 보류: 이름·아이콘·모델 미지정 |
| 1011 | TestArtifact_Rider | 보류: Empty 형태 | 보류: 이름·아이콘 없음, 모델 GUID 현재 Assets에서 찾지 못함 |

1003은 최신 SpawnSoul 기능으로 변환한 것이 아니다. 원본의 AttackPower/MaxHealth ModifyStat 설정을 그대로 보존했다. 실제 의도와 효과 실행 연결은 Core 담당자가 확인해야 한다. 기존 DB 생성자 검증 통과는 모든 게임 규칙의 의미적 정확성을 보장하지 않는다.

원본 형태 enum은 Empty=0, Single=1, Domino=2, TShape=5다. 기존 ArtifactShapeAdapter의 좌표를 사용했다. Empty를 임의의 1칸으로 바꾸지 않았다.

## 생성 및 수정

- `Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/`: 규칙 SO 3개 생성.
- `InGameGroup/Artifact/ArtifactDefinitionDataTable.asset`: 규칙 SO 3개 등록.
- `ViewGroup/Artifact/ArtifactViewDataTable.asset`: 빈 표현 테이블 생성. 누락 아이콘을 허용하도록 Validate를 약화하지 않았다.
- `ViewGroup/Artifact/Item/`: `ArtifactView_HealingChalice.asset`, `ArtifactView_ThornArmor.asset`, `ArtifactView_SoulDrop.asset` 생성. 원본의 이름과 ID를 입력했다. 아이콘과 프리팹은 현재 프로젝트에서 찾지 못해 비워 두었으며, 아이콘 지정 전에는 테이블에 등록하지 않는다.
- 기존 InGameGroup/ViewGroup 에셋에 각각 새 테이블 등록. 기존 테이블 참조는 유지.
- `ArtifactBuffer.cs`: Add/Artifacts 대신 SetSnapshot/Current로 전체 상태 교체, Clear 시 Empty 반환. 기존 Factory·Buffer 서비스 키는 유지.
- `ArtifactData.cs`: 더미 상태라는 오래된 주석 정리.
- `InGameDataTableTests`: Factory가 요구하는 새 아티팩트 테이블을 테스트 그룹에도 추가.
- `ArtifactBufferTests`: 원본 배열 분리, 전체 교체·초기화, null/잘못된 보드 거부 3개 테스트.
- `ArtifactDataTableTests`: 중복·누락 ID, 불완전 표현 거부, 실제 Catalog의 부분 이전 결과 3개 테스트.

별도 YAML·TXT 사본, 검증 스크립트, JSON 목록은 정리하고 이전 기록은 이 문서 하나로 관리한다. 원본 5개와 표시 문구·드래그 기준점은 위 고정 Git 커밋에서 확인할 수 있다.

## 원본 및 에셋 추적 정보

- 원본 폴더: `Assets/DataBase/SO/NewArtifact/ArtifactTable/items/`
- 새 규칙 SO 폴더: `Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/`
- 아래 파일명은 원본 폴더 기준이다. 등록한 3개는 새 규칙 SO 폴더에서도 같은 파일명을 사용한다.
- 원본 초기 수량은 5개 모두 1이다. 규칙 등록 ID는 1001·1002·1003, 규칙 보류 ID는 1004·1011, 표현 보류 ID는 5개 전체다.

| ID | 파일명 | 원본 형태 값 | 원본 프리팹 GUID | 원본 프리팹 fileID |
|---|---|---|---|---|
| 1001 | `Artifact_HealingChalice.asset` | 1 (Single) | `4b81f072ec322f9459d9e6d6ba2c13bf` | `3807140229930616291` |
| 1002 | `Artifact_ThornArmor.asset` | 2 (Domino) | `c3f2dd3831fd1104f9337b7757bbc160` | `7458559281675288433` |
| 1003 | `Artifact_SoulDrop.asset` | 5 (TShape) | `a87cbe24399d4a34c809e730c5821f93` | `1117408295722858690` |
| 1004 | `Artifact_LastResistance.asset` | 0 (Empty) | 없음 | `0` |
| 1011 | `TestArtifact_Rider.asset` | 0 (Empty) | `c8edceb87dee09b4888da558396ddaab` | `4231442584887248009` |

GUID가 있는 원본 프리팹 참조의 type은 모두 3이다. 위 참조는 복구용 기록이며 현재 브랜치에서 해당 모델을 복원했다는 뜻은 아니다.

## 검증 결과와 한계

- PowerShell 원본 대조: ID·초기 수량·effects 블록 동일, 형태 셀 수, 개별 에셋·스크립트·그룹 참조 검사 통과.
  - 이전 작업 당시 실행한 결과다. 사용했던 `verify.ps1`과 YAML 사본은 정리했으므로 현재 재실행 명령은 제공하지 않는다.
- Buffer 테스트 3개: 별도 .NET 10 실행기로 실제 Buffer/상태 소스와 NUnit 테스트를 실행해 통과. 서비스 등록 인터페이스만 대체한 격리 검증이며 Unity 런타임 테스트가 아니다.
- Systems 소스 별도 컴파일: Unity가 생성한 csproj의 소스 목록과 설치된 Unity/Library DLL 참조로 오류 0개. 기존 오디오 using 중복 경고 4개.
- Framework 테스트 소스 전체 별도 컴파일: 오류 0개, 기존 테스트 미할당 필드 경고 1개.
- 일반 dotnet 프로젝트 빌드는 생성된 프로젝트 참조 출력 DLL 부재로 실패하여 위 별도 컴파일로 보완했다.
- Unity EditMode 배치 실행은 같은 프로젝트를 사용 중인 에디터 때문에 시작하지 못했다. 에디터를 종료하거나 패키지를 설치하지 않았다. **Unity 에셋 로딩·전체 DB 생성·EditMode 통과는 아직 확인되지 않았다.**

## 다음 확인

1. 열린 Unity에서 컴파일 완료 후 Test Runner의 EditMode `ProjectL.Tests.Framework` 실행. 새 6개 테스트와 기존 Catalog 테스트 포함.
2. 생성된 표현 SO 3개에 아이콘과 필요한 모델을 지정하고 기존 표시 문구·드래그 기준점을 표현 데이터에 어떤 형태로 옮길지 결정한다. 필수 값 검증을 통과한 View SO를 표현 테이블에 등록한다. 1004·1011은 이름과 규칙을 확정한 뒤 표현 SO도 생성한다.
3. 1004·1011의 실제 형태, 1004의 statId를 확정한 후 규칙 SO 등록.
4. 보류가 해소되면 이 문서의 이전 결과 및 `Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`의 예상 목록을 함께 갱신하고 전체 규칙↔표현 ID 대응 테스트로 전환한다.
5. 별도 후속: Core GetState → Buffer SetSnapshot 호출과 실제 View 표시. 이번 변경으로 자동 연결되지 않는다.

## 공유 및 커밋 범위

- 권장 제목: `[호진] Artifact DB 부분 이전 및 Buffer Snapshot 구조 정리`.
- Core 담당자에게 Buffer API 변경을 공유한다. 현재 저장소에서는 이전 Add/Artifacts API의 소비자가 발견되지 않았다. 다른 미병합 브랜치는 별도 확인 대상이다.
- FMOD·StageSelect·프로젝트 설정 등 작업 시작 전/에디터에서 발생한 별도 변경은 이 DB 작업과 구분해서 검토한다.
- 커밋·푸시는 실행하지 않았다.
