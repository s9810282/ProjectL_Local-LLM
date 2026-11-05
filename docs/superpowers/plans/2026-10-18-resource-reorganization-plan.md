# 2026-10-18 리소스·프리팹 폴더 정리

현재 상태: **전체 파일 정리 완료, Unity 재임포트·에셋 테스트·씬 재생 확인 대기.** 사용자가 후속 지시로 단계마다 멈추지 말고 마무리하도록 요청했으므로 아래 단계는 작업 이력 구분으로만 유지한다.

## 범위와 설계

사용자 승인 계획: 원본 리소스는 용도별로 `Assets/ProjectL/Resources` 아래에 모으고 게임용 프리팹은 `Assets/ProjectL/Prefabs` 아래에 구분한다. 모델 교체와 프리팹 내부 구조 변경은 하지 않는다.

사용자 추가 확정: **Effect 관련 파일만 유지한다. 그 밖의 FBX도 이동 대상이다.** Effect 디렉터리뿐 아니라 GUID로 확인되는 의존 파일도 기존 위치에 남긴다. 공유 원본을 복사해서 두 벌로 만들지 않는다.

목적지:

```text
Assets/ProjectL/
├─ Resources/
│  ├─ Environment/
│  │  ├─ BackgroundTest/       기존 Assets/Scripts 배경 검증 모델
│  │  ├─ BackgroundSceneTest/  기존 Shader/View/NotUse 배경 모델
│  │  ├─ Props/               소품별 Models·Materials·Textures
│  │  ├─ Gameplay/Materials/  배치용 환경 머티리얼
│  │  └─ Railway/             철도 모델·머티리얼·애니메이션
│  ├─ Characters/
│  │  ├─ Prototype/           기존 테스트 캐릭터 원본
│  │  ├─ Colored/             컬러 캐릭터 원본
│  │  └─ Enemy/SourcePacks/    외부 원본 패키지의 상대 구조
│  ├─ Artifacts/              유물 모델·텍스처·공유 머티리얼
│  └─ Shared/ShaderTests/     기존 셰이더 검증용 아트의 상대 구조
└─ Prefabs/
   ├─ Environment/Gameplay/   배치용 환경 프리팹
   ├─ Characters/             기존 Stage/Units 프리팹
   ├─ Artifacts/              기존 Assets/Prefabs/Artifacts
   ├─ Stage/                  맵 전체·필드·철도 게임용 조립 유지
   ├─ UI/                     유지
   └─ Test/                   테스트 전용 프리팹
```

모델·머티리얼·텍스처·애니메이션은 같은 용도 묶음 안에서 구분한다. 외부 패키지의 원본 프리팹은 원본 패키지 구조 안에 보관하고, 게임용 래퍼 프리팹과 구분한다. 패키지 일부가 Effect와 공유되면 해당 공유 파일은 원래 위치에 남기고 기존 GUID 참조를 사용한다. `.fbm`은 해당 FBX 옆 상대 위치를 유지한다.

현재 Resources 안의 Font·View 설정, UI 리소스, 플러그인·FMOD·TMP, 코드·셰이더 구현, 씬, DB-SO 위치는 이번 이동 대상이 아니다. 이름만으로 미사용이라고 판단하거나 에셋을 삭제하지 않는다. `FieldCamera`·`Architecture`·Core 로직을 수정하지 않는다.

## 구조 점검

- SSOT: 에셋을 복제하지 않고 기존 파일과 `.meta`를 이동한다. 머티리얼·모델 GUID는 유지한다. 공유 Effect 의존성은 한 원본을 계속 참조한다.
- 책임과 변경 독립성: 폴더 이동과 경로 문자열 수정만 수행한다. Factory·서비스 등록·전투 규칙·카메라 동작을 변경하지 않는다. 배경 이동과 캐릭터 이동을 별도 단계로 검토할 수 있다.
- SOLID: 기존 컴포넌트·서비스 인터페이스·의존 방향은 유지한다. 폴더 정리만을 위한 런타임 추상화나 조회 시스템을 추가하지 않는다.
- FW 계약: 기존 서비스 생성·등록·Export·소비자 조회 경로를 유지한다. 프리팹 및 SO의 GUID 연결이 이동 후에도 같은 대상을 가리키는지 검증한다.

## 단계별 Task

### 1단계 — 인벤토리와 이동 목록 (완료)

- 조사 대상 실제 에셋 392개를 분류했다. 파일 `.meta`는 별도이며 에셋 수에 포함하지 않았다.
- 이동 후보 296개: 배경 리소스 185, 배경 프리팹 9, 캐릭터 리소스 21, 캐릭터 프리팹 15, 유물 리소스 29, 유물 프리팹 4, 공유 검증 아트 33.
- 후보에는 FBX 96개와 프리팹 30개가 포함된다. 프리팹 수에는 외부 패키지의 원본 프리팹도 포함한다.
- 조사 범위 중 Effect 관련 26개와 기존 분류 유지 66개를 구분했다. Effect 디렉터리 전체와 그 의존성 보호 목록은 manifest에 별도로 기록했다.
- Resources 안 테스트 캐릭터 프리팹 4개는 상대 로드 경로 확인 대상으로 표시했다. 3단계에서 코드·에셋·Addressables 사용처를 확인한 뒤 이동 여부를 확정한다.
- 각 파일의 원본 GUID, 파일·메타 SHA-256, 텍스트에서 확인한 GUID 참조 파일과 C# 경로 문자열 사용처를 기록했다.
- 후보 목적지 중복 0, 이미 존재하는 목적지 0, 조사 대상 `.meta` GUID 누락 0을 확인했다.
- 이번 단계에서는 실제 에셋·프리팹·경로 문자열을 변경하지 않았다.

개별 목록 원본: [이동 manifest](2026-10-18-resource-reorganization-manifest.json). `candidate`는 다음 단계 이동 후보이며 이동 완료를 뜻하지 않는다. manifest는 이동 전 스냅샷이다.

### 2단계 — 배경·소품·철도 리소스와 배경 프리팹 (이동·정적 검증 완료, Unity 확인 대기)

1. 1단계 스냅샷의 GUID·해시 및 작업 트리 변경을 재확인한다. 다른 작업자가 바꾼 파일은 덮어쓰지 않는다.
2. `Resources/Environment` 목적지 후보 185개와 `Prefabs/Environment` 후보 9개를 파일·메타 쌍으로 이동한다. 새 폴더의 메타도 함께 관리한다.
3. 철도 게임용 조립 프리팹은 `Prefabs/Stage/Railway`에 유지하고 Source 모델·애니메이션·머티리얼만 이동한다. 중첩 프리팹 구조와 파일 이름은 유지한다.
4. 영향받는 테스트·도구의 기존 경로 문자열 및 폴더 기준 검색을 새 경로에 맞춘다. manifest의 리터럴 사용처는 정확한 전체 경로 검색 결과이므로, 폴더 경로 조합·Resources 상대 경로·FBX 임포트 검색도 별도로 검사한다.
5. 이동 후 파일·메타 해시, GUID, 이동 대상의 이전 경로 잔존, 새 Missing 참조를 검증한다. 관련 에셋 테스트 및 맵·철도·아침/밤 전환을 확인한다.
6. 단계 결과를 보고하고 리뷰 지점에서 멈춘다.

### 3단계 — 캐릭터·유물·공유 검증 아트 (이동·정적 검증 완료)

1. Effect 공유 파일은 제외하고 캐릭터·유물·공유 아트의 확정 후보를 이동한다.
2. 테스트 모델 프리팹 4개의 Resources 로드 의존성을 확인한다. 경로를 사용하는 소비자가 있으면 같은 단계에서 호환성을 확보한 뒤 이동한다. 계획과 다른 결정이 필요하면 구현 전에 확인한다.
3. 패키지 원본은 상대 폴더 구조를 유지한다. 캐릭터 게임용 프리팹은 스크립트·콜라이더·Animator·외형 연결을 그대로 유지한다.
4. 관련 C# 경로 문자열과 DB·씬·중첩 프리팹 참조를 검증한다. 경로 조합을 쓰는 소비자도 검사한다.
5. 관련 에셋 테스트·캐릭터 표시·유물 표시를 확인하고 결과를 보고한 뒤 멈춘다.

### 4단계 — 통합 검증과 문서 갱신 (정적 검사·문서 완료, Unity 확인 대기)

1. 모든 이동의 GUID·내용·메타 해시 및 Effect 보호 파일의 위치·내용을 대조한다.
2. Unity 재임포트 후 새 Missing Script·Mesh·Material·Texture·Prefab 참조가 없는지 확인한다. 기존 Missing Script SO는 보존하고 새 문제와 구분한다.
3. 변경 영향에 맞는 EditMode 에셋 테스트와 필요한 컴파일 검증을 수행한다. DB 에셋 값이 실제로 바뀌면 DataBaseAssetTests를 수행한다.
4. 맵 생성·캐릭터 표시·철도·유물·스텐실 아침/밤 전환을 실제 씬에서 확인한다.
5. `Assets/ProjectL/Script/README.md`의 경로 안내와 이 계획의 실행 기록을 갱신한다. 커밋·push는 사용자가 한다.

## 검증의 한계와 실행 기록

1단계는 파일 및 텍스트 참조 검사다. Unity의 임포트 의존성 검사와 실제 씬 재생을 수행한 결과가 아니다. 외부 경로 문자열, 동적으로 조합하는 로드 키, 모델 임포터가 이름으로 찾는 머티리얼은 실제 이동 단계에서 추가 확인한다.

기존 전투 아침 전환 변경, 사용자가 수정한 전환 속도·목표 진행량, FMOD 생성 파일은 이번 단계에서 변경하지 않았다.

### 2단계 실행 결과

- 배경·소품·철도 리소스 185개와 배경 프리팹 9개, 총 194개를 승인된 목적지로 이동했다. 실제 FBX는 67개이며 머티리얼 45개·애니메이션/컨트롤러 각 1개·이미지 71개·프리팹 9개다.
- 모든 이동 파일과 `.meta`가 이동 전 SHA-256과 일치한다. 이동한 GUID 194개는 각각 새 경로에 한 번만 존재한다. 이전 파일·메타 경로는 모두 없어졌다.
- 새 폴더 34개에 고유 GUID의 `.meta`를 만들었다. 원래 폴더와 폴더 메타는 삭제하지 않았다.
- Effect 보호 목록의 실제 파일 114개와 그 `.meta`의 위치·해시가 그대로다. 보호 목록 총 132항목에는 폴더도 포함된다.
- 1단계 조사 목록 중 이동하지 않은 198개도 파일·메타 해시가 일치한다. 다음 단계의 캐릭터·유물·공유 아트는 아직 이동하지 않았다.
- 철도 게임용·원본 조립 프리팹은 `Prefabs/Stage/Railway`에 유지하고, 원본 FBX·애니메이션·머티리얼만 `Resources/Environment/Railway`로 이동했다. 소품 FBX와 `.fbm` 상대 위치도 유지했다.
- `TerrainVisualDatabaseTests.ActualCatalog_ProvidesThreePlaceholderCubesAndTornadoParticles`의 배치용 지형 프리팹·머티리얼 경로 두 곳을 새 위치에 맞췄다. 유지한 Tornado 프리팹과 Effect 머티리얼 경로는 그대로다.
- 코드·설정의 이동 전 전체 경로 잔존은 0건이다. 기존 폴더 경로 검색에서 남은 것은 유지한 철도 조립 프리팹 또는 Effect 경로다. 기록된 GUID 참조 파일의 이동 후 경로 252개가 존재한다.
- VS MSBuild `ProjectL.Tests.Framework.csproj /t:Build` 성공(오류 0). 기존 중복 using·미할당 테스트 필드 경고가 있으며 Unity EditMode 테스트 실행과 구분한다. 로그: `Logs/environment-resource-move-phase2-build.log`.
- Unity 에디터가 `Temp/UnityLockfile`을 점유 중임을 확인했다. 두 번째 에디터 실행·강제 종료·잠금 파일 삭제는 하지 않았다. 이번 단계의 Unity 임포트 확인, 관련 네이티브 에셋 테스트와 씬 재생은 아직 수행하지 않았다.
- Unity 확인 대상: `TerrainVisualDatabaseTests.ActualCatalog_ProvidesThreePlaceholderCubesAndTornadoParticles`, `DataBaseAssetTests.RailwayVisual_RestoredMeshesMaterialsAndAnimationHaveValidReferences`, `DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`, 관련 철도·화면 전환 테스트 및 실제 맵·철도·아침/밤 전환.

기계 판독 실행 기록: [2단계 결과](2026-10-18-resource-reorganization-phase2-result.json). 정적 검증은 Unity가 FBX를 재임포트한 결과나 실제 화면 확인을 대체하지 않는다. 다음은 3단계 캐릭터·유물·공유 검증 아트 이동이다.

### 최종 파일 정리 결과

- 사용자 후속 지시에 따라 중간 리뷰를 요청하지 않고 남은 이동·정리·문서 갱신을 이어서 수행했다.
- 기존 194개에 캐릭터·유물·공유 아트 102개와 테스트 모델 프리팹 4개를 더해 **총 300개 에셋과 `.meta`**를 이동했다. FBX 96개, 프리팹 34개가 포함된다.
- 테스트 모델 프리팹 4개는 코드의 Resources 로드·이전 상대 경로 및 Addressables 사용처를 조사했다. 확인된 게임용 프리팹 사용처는 GUID 참조이며, 이동 뒤 같은 GUID를 유지한다.
- `ArtifactBoardMapperTests`, `CombatMortarAssetTests`, `StageMapViewDisplayTests`의 전체 에셋 경로를 갱신했다. 앞서 수정한 `TerrainVisualDatabaseTests`를 포함해 테스트 코드 4개에서 경로만 바꿨다.
- 모든 이동 에셋·메타의 원본 해시 일치 및 GUID 300개가 새 경로에 각각 한 번만 존재함을 확인했다. 새 폴더는 총 81개이며 고유 GUID 메타가 있다. 이동하지 않은 조사 에셋 92개도 파일·메타 해시가 일치한다. Effect 보호 실제 파일 114개와 메타도 그대로다.
- 이전 부모 중 실제로 비어 있고 폴더 GUID를 다른 파일에서 참조하지 않는 폴더 49개만 비재귀 삭제했다. 남은 에셋이나 기존 Missing Script SO는 삭제하지 않았다. 폴더 메타 백업은 `Temp/resource-empty-folder-cleanup-receipt.json`에 기록했다.
- 코드·설정에서 이전 전체 경로 잔존 0건, 기록된 GUID 참조 파일의 현재 경로 406개 존재를 확인했다.
- VS MSBuild `ProjectL.Tests.Framework.csproj /t:Build` 최종 빌드 성공(오류 0, 기존 경고 유지). 로그: `Logs/resource-reorganization-final-build.log`.
- `Assets/ProjectL/Script/README.md`를 현재 리소스·프리팹 구조로 갱신했다. 커밋·push는 하지 않았다.
- Unity 에디터는 기존 프로세스가 잠금 파일을 점유 중이며, 네이티브 에셋 테스트·씬 재생은 실행하지 않았다. 파일 정리는 완료됐으며 Unity 검증은 별도 미확인 항목이다.

전체 실행 기록: [최종 결과](2026-10-18-resource-reorganization-final-result.json). 이전 단계의 개수·경로·미수행 기록은 당시 상태를 뜻한다.
