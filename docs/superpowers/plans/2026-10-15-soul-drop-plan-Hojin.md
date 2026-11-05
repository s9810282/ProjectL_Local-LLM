# 소울 드랍 계획

- 작성자: 호진(이호진)
- 상태: SD1(DB 정합)·SDV0(인수) 끝(2026-10-15, 사용자 커밋 `51f5a892`). SDV1~SDV4는 미구현이며 전투 View 작업자에게 이관했다(2026-10-15 사용자). 이 계획의 다음 단계는 인수자가 표현을 정하고 진행 지시를 받은 뒤다.
- 설계: [소울 드랍 설계](../specs/2026-10-15-soul-drop-view-design-Hojin.md)
- 정리: 2026-10-27 이 문서의 옛 1부(SD1 실행 기록)·2부(SDV Task 계획)를 한 틀로 다시 썼다. 끝난 Task 단계·시험 수치·실행 명령·증거 경로(`.superpowers/sdd/…`)는 git `10914570`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| SD1 | `Artifact_SoulDrop.asset` 효과를 SpawnSoul 한 항목으로 | 완료(10-15) | 에이전트 batch EditMode, 관련 아티팩트·영혼 시험 통과. 수동 Play 없음 | `51f5a892` |
| SDV0 | 기획 원문·현재 계약·직접 호출자·동시 편집·검증 기준선 인수, 담당 이관 문서 | 완료(10-15) | 시작 작업본의 기존 실패 기록(제품 변경 없음) | `51f5a892` |
| SDV1 | View 설정·Query·빛 구슬 에셋·DB 등록 | 미구현·이관 | — | — |
| SDV2 | ID별 필드 표시·사건 전환 복제본·풀 | 미구현·이관 | — | — |
| SDV3 | 사건 시간·성장 반영 순서·결과 수명 연결 | 미구현·이관 | — | — |
| SDV4 | 실제 소울 드랍 전투·출력·인계 | 미구현·이관 | — | — |

## 2 남은 일

- [ ] 인수자(전투 View)가 구슬 정식 에셋·색·재질·경로·초수를 정한다([설계 §4.2](../specs/2026-10-15-soul-drop-view-design-Hojin.md#42-표현-기본안미승인)는 미승인 기본안).
- [ ] SDV1~SDV4를 Phase마다 진행 지시를 받아 한다(§3.3).
- [ ] 파일 자리를 지금 전투 View 구조에 다시 맞춘다. 아래 파일 지도는 2026-10-15 기준이고, 그 뒤 사건 처리기 표(10-18)·폴더 정리(10-21)가 있었다.
- [ ] 공유 메모 전달(§4, 모두 미전달).
- [ ] 사람 확인: 구슬 미관·실제 GameView 표시·마우스/키보드/패드(SDV4 뒤).

## 3 작업별 결과

### 3.1 SD1 SpawnSoul DB 정합 (2026-10-15)

- 사용자 승인 범위: 영혼 드랍 SO를 지금 Core 계약에 맞추고 문서·검증·사용법까지. View 표시·가시심장 Core 연결은 범위 밖.
- 두 ModifyStat 항목을 SpawnSoul 한 항목으로 바꿨다(값은 [설계 §3](../specs/2026-10-15-soul-drop-view-design-Hojin.md#3-결정-기록) SD1). Unity Editor API로 `effects`만 저장·재로드했고, 임시 도구는 지웠다. ID·GUID·등록·형상은 그대로다.
- 계획과 다른 점: 시험 보조 함수의 반환 형식을 실제 Table adapter의 `ArtifactDefinitionData`로 바로잡았다. 중립 Owner 사례는 전투 캐릭터가 Player/Enemy만이라 밤/낮 × 적/아군 네 조합으로 고쳤다(Core에 중립 지원을 더하지 않음). 기존 거절 시험을 실제 DB 수락 시험으로 바꾸고 동작 시험 7개를 더했다.
- 확인: 관련 아티팩트·영혼 시험(`ArtifactDataTableTests`·`ArtifactEffectCompilerTests`·`ArtifactSoulSchemaTests`·`ArtifactSoulBufferCalculationTests`·`ArtifactSoulStateTests`) 통과. `DataBaseAssetTests` 실패 2건은 수정 전 SO에서도 같은 기존 실패다(Stage 1101 지형 회전, `LineShader.mat` 없음).
- Unity 저장으로 대상 SO의 CRLF가 LF로 바뀌었다. 사용법은 [소울 드랍 인계 3부](../handoffs/2026-10-15-soul-drop-handoff-Hojin.md#3부-영혼-드랍-spawnsoul-db-정합사용법--2026-10-15), Core 경계 기록은 [Core 전달 문서 §17](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#17-soul-drop-spawnsoul-db-alignment-2026-10-15).

### 3.2 SDV0 인수 (2026-10-15)

- 기준: HEAD `95dbf3f5`, `feature/Hojin/Stage_Shop_Event` 위 미커밋 작업본(ER6·SD1 포함).
- 기획 원문 55개(PPTX 34·DOCX 6·TXT 15)를 읽기 전용으로 다시 추출해 설계 때와 같음을 확인했다. 사용자는 표현을 승인하지 않고 구슬 제작을 전투 View에 넘겼다(SDV1~SDV4 담당 이관).
- 직접 호출자(인수자가 서명을 바꿀 때 함께 고칠 곳): `CombatDisplayTables` 생성 4곳·`CombatDisplayValues` 생성 11곳·`IViewQuery` 시험 대역 3개, 10파일. 제품 `CombatDisplayTables.cs`·`CombatViewSystem.cs`, 시험 `ArtifactPopupConfigTests`·`CombatMortarAssetTests`·`CombatPointerMappingTests`·`CombatSceneEventPresenterTests`·`LobbyInputTests`·`TerrainOperationalDisplayTests`·`UnityStageInputTests`·`WallAttackInteractionTests`. 실제 `ViewDataBase`(`SystemGroup, IViewQuery`)는 따로 확인. 이 목록은 10-15 기준이다.
- 기준선: 관련·전체 EditMode를 제품 변경 없이 실행해 기존 실패를 기록했다. 소울 드랍 근거 시험은 모두 통과했다. 기존 실패 중 SDV3가 이어받을 것은 `CombatSceneEventPresenterTests`(`CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent` 2건, `WarmFactRefresh_AllocatesLessThanLegacyProjection_AndReusesViews`)와 DisplayReuse·Railway 실패다. 새 Core 연결이 필요하다는 실패는 없었다.
- 문서: 인계 1부·2부를 썼다(외부 미전달). 제품 코드·SO·프리팹·씬은 바꾸지 않았다.

### 3.3 SDV1~SDV4 (미구현, 전투 View로 이관)

각 Phase는 진행 지시 뒤 시작하고, 끝나면 시험·리뷰·문서를 갱신하고 멈춘다. 계약은 [설계 §6~§8](../specs/2026-10-15-soul-drop-view-design-Hojin.md#6-표시-데이터와-초기-복구).

| Phase | 할 일 | 계획한 공개 형태 | 꼭 넣을 시험 |
|---|---|---|---|
| SDV1 | View 설정 Data·SO·표·Query, 기존 View DB 등록, 구슬 프리팹·재질, `CombatDisplayTables`에 필수 설정 | `internal sealed SoulFieldVisualData`(`Prefab`, `FieldHeight`·`CollectorHeight`·`CollectSeconds`·`RemoveSeconds`), `ISoulFieldVisualTableQuery : IServiceModule`의 `Config`, `internal SoulFieldVisualTable(IDataProvider<IDatabaseItem>)` | `GetData_ReturnsFreshImmutablePresentationValues`, `MissingOrDuplicateConfig_FailsBeforeUse`, `InvalidPrefabOrNonFiniteTiming_IsRejected`, `ZeroAndOneSecondTiming_AreSupported` |
| SDV2 | ID별 표시·풀·전환 복제본 | `FieldSoulPresenter(SoulFieldVisualData, Transform root)`, `Bind(FieldGridMapper)`, `Sync(SoulFieldSnapshot)`, `float PrepareEvent(CombatSoulEvent)`, `SetProgress(float)`, `CompleteEvent()`, `Clear()`, `Release()`. 반환 초수: 생성·TTL만 0, 수집 `CollectSeconds`, 비수집 `RemoveSeconds` | `SameCellDistinctIds_StaySeparateAndReuse`, `SpawnRemovalAndCollection_KeepExactIdOwnership`, `InvalidEvent_FailsBeforeMutatingExistingViews`, `ClearDuringReturn_DoesNotDoubleReturn`, `TiltedScaledField_UsesMapperWithoutBlockingInput` |
| SDV3 | 사건 시간·결과 순서 연결 | `CombatSoulPlayback(FieldSoulPresenter)`: `IsComplete`, `static InitialField(current, unpresentedResult)`, `PrepareResult`, `Begin(CombatSoulEvent, Action)`, `Tick(float)`, `Cancel()`. `CombatDisplayValues`에 필수 `soulField`(`runGold` 다음)와 끝 선택 `unpresentedResult`, `WithSoulField`. `ReadDisplayValues(bool includeUnpresentedResult = false)`, ShowBattle만 true | `SpawnThenCollect_UsesEventSnapshotsWithEmptyLatestField`, `ForcedCollector_UsesItsEventFinalAnchor`, `MixedRemovalAndCreation_UsesPublishedTurns`, `CollectionKeepsExistingFactsTiming_AndWaitsBeforeAck`, `ZeroDurationAndLargeDelta_CompleteOnce`, `ReattachPendingResult_InstallsFirstExpectedSoulField`, `CancelDuringCompletion_DoesNotNotifyOrAcknowledge`, `NextBattleIdReuse_RejectsOldCompletion`, `ReentrantTickAndPlaybackFailure_KeepGenerationAndCleanup` |
| SDV4 | 실제 Boot → 로비 보드 배치 → 밤 전투 사망 → 같은 칸 도착 수집·성장 → 다음 전투 지속, 별도 미획득 소멸. 출력·사람 확인 구분, 인계 갱신 | 새 Core endpoint 없음 | `SoulDropViewBootSmokeTests`(탐색 상한을 미리 정하고, 운영 HP·영혼·그래프·턴을 덮어쓰지 않음) |

파일 지도(2026-10-15 기준, 인수자가 지금 구조로 다시 맞춘다):

| 파일 | Phase |
|---|---|
| `DataBaseSystem/Data/View/Table/SoulField/Item/SoulFieldVisualData.cs`·`SoulFieldVisualDataSO.cs`, `SoulField/SoulFieldVisualDataTableSO.cs` | SDV1 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/SoulFieldVisualTable.cs`, 기존 `ViewDataBase.cs`·`Factory/DBFactory/Group/ViewDataBaseFactory.cs`·`ServiceKeys/DataBaseGroupServiceKey.cs` | SDV1 |
| `DataBaseSO/ViewGroup/SoulFieldVisual/SoulFieldVisualDataTable.asset`·`Item/SoulFieldVisual_Default.asset`, 기존 `ViewGroup.asset` | SDV1(코드와 한 커밋) |
| `Prefabs/Combat/Soul/SoulOrb.prefab`·`SoulOrb.mat` | SDV1 |
| `FieldSoulPresenter.cs`(전투 View Display) | SDV2 |
| `CombatSoulPlayback.cs`, 기존 `CombatDisplayTables`·`CombatDisplayValues`·`CombatViewSystem`·`CombatSceneDisplayFactory`·`CombatSceneDisplay`·`CombatSceneEventPlayback` | SDV3 |
| 시험 `SoulFieldVisualTests`·`CombatSoulFieldPresentationTests`·`CombatSoulPlaybackTests`·`SoulDropViewBootSmokeTests`, 기존 `ViewDataTableTests`·`DataBaseAssetTests`·`CombatSceneEventPresenterTests`·`CombatPresentationCleanupTests`·`CombatViewSystemTests` | SDV1~SDV4 |

지킬 것: 규칙·HP·성장·TTL·수집자·소비 ID는 Core·Buffer 원본이다(InGame 조회·물리 Trigger 판정 금지). 새 System·ServiceKey·전역 이벤트·Update·Coroutine·Delay·입력 Layer·카메라·Render EffectType·Shader를 더하지 않는다(새 View 표 조회 키만). 결과 ack·F1·마지막 결과 소비 순서를 바꾸지 않는다. Missing Script 에셋 48개·아티팩트/공용 프리팹은 지우거나 이름을 바꾸지 않는다. 기존 실패는 시작과 끝의 이름·메시지를 비교해 새 실패와 구분한다.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [소울 드랍 인계](../handoffs/2026-10-15-soul-drop-handoff-Hojin.md) 1부(전투 View 구현 인계) | 전투 View 작업자(용근님) | 미전달 |
| 같은 문서 2부(Core 계약 확인 메모) | 천명님 | 미전달 |
| 같은 문서 3부(SpawnSoul DB 사용법)·[Core 전달 문서 §17](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#17-soul-drop-spawnsoul-db-alignment-2026-10-15) | 천명님 | 미전달(인계는 "외부 미전달", §17에는 전달 기록 없음) |
