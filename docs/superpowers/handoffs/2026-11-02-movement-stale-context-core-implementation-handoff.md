# 이동 StaleContext 수정 — Core 구현 전달·진지 구축 사용자 확인 (2026-11-02)

- 대상: Core 담당자. 사용자 지시로 View 작업 측이 직접 반영한 Core 변경의 인수 문서다.
- 상태: **MC1·MC2A·MC2B 구현 완료, Unity 컴파일 확인, 사용자 진지 구축 문제 없음 확인.**
- 사용자 확인 원문: “진지 구축 문제 없는거 확인했고”(2026-11-02). 확인한 rank·추가 구매·오라 배치 조건은 별도로 전달되지 않았다. rank 1·2·3 전수 확인으로 확대하지 않는다.
- 기존 EditMode: **5,627건 중 5,619 통과·8 실패**, 건너뜀/불확정 0. 실패 내역과 잔여 검증은 §5~6에 있다.
- 이번 전달 준비에서는 문서만 갱신했다. 실행 코드는 위 시험 버전 그대로이며 새 시험·MSBuild·추가 Unity 실행은 하지 않았다.
- Git 스테이징은 사용자 요청으로 이번 수정의 코드·meta·관련 문서만 대상으로 한다. 커밋·push·외부 발송은 하지 않는다.

## 1. 문제와 최종 동작

샷건 이동 미리보기 뒤 경로를 확정하면 Core가 `StaleContext`로 거절하는 증상이 보고됐다.

기존 합성은 훈련 가능한 특성의 rank 1도 `ResolveRank`를 거쳐 같은 설정의 새 TraitSpec/UnitSpec을 만들었다. 이동 문맥은 기본 Definitions를 객체 참조로, 보유 프로필의 Traits를 원소 참조로 비교했으므로 실제 값이 그대로여도 거절될 수 있었다.

최종 코드는 **동일한 정의/특성 설정의 재생성을 같은 값으로 비교**한다. 실제 읽은 프로필의 설정·선택 주체가 달라지거나 전투/행위자/초안 경로 등 기존 기대 사실이 달라지면 문맥 불일치 거절을 유지한다. View에서 문맥 검사를 생략하는 우회는 없다.

원래 결과 8 로그의 구체 실패 비교 항목은 직접 계측하지 않았다. 코드에서 확인한 결함과 사용자 진지 구축 정상 확인을 구분해 기록한다. 상세 조사 이력은 [조사·실행 기록](2026-11-02-shotgun-movement-stale-context-core-request.md)에 있다.

## 2. 반영한 변경과 책임

| 단계 | 구현 | 소유/계약 |
|---|---|---|
| MC1 | TraitSpec의 공통 SameValues와 파생 37개 타입의 SameValuesCore | 각 특성이 자기 전체 설정의 동등성을 소유한다. 이동에 특성별 switch/필드 목록을 만들지 않는다. |
| MC2A | MovementDefinitionComparison과 MovementContextCalculator 연결 | UnitSpec·CombatAttackProfile 조합 비교를 한 곳에 둔다. 기본/보유 정의 모두 같은 비교를 사용한다. |
| MC2B | CombatProfileReadSet·RequiredProfile 기록 오버로드·명시적 전달·Capture 동결 | 기존 선택기가 반환한 프로필만 기록하고 기존 문맥 목록에 고정한다. 선택/효과 후보 규칙을 복제하지 않는다. |

### TraitSpec 공개 소스 계약

[공통 진입점](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/Traits/TraitSpec.cs):

```csharp
public bool SameValues(TraitSpec other);
protected abstract bool SameValuesCore(TraitSpec other);
```

공통 진입점이 동일 참조/null/정확한 런타임 형식/TraitKey를 검사한다. 파생 구현은 같은 형식·키인 인자에 대해 **모든 설정 값**을 비교한다. 거리별 수치 목록은 기존 목록 값 비교를 사용하고 무설정 타입은 공통 검사 뒤 true를 반환한다. Object.Equals/GetHashCode나 기존 컬렉션의 전역 동등성은 바꾸지 않았다.

**새 TraitSpec 파생 타입은 SameValuesCore 구현이 필요하다.** 새 설정 필드를 추가할 때도 해당 비교를 함께 수정해야 한다. 새 타입의 구현 누락은 컴파일로 드러나지만 새 필드의 비교 누락까지 자동 검출하지는 않는다.

### 이동 정의 비교

[MovementDefinitionComparison](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementDefinitionComparison.cs)은 UnitSpec 독립 설정 18개를 비교하고 특성은 TraitKey 기준 집합으로 비교한다. 특성 배열의 배치 순서만 달라도 같은 값이다. 발자국 칸은 기존 순서/좌표 비교를 유지한다.

Method·Behavior·Occlusion·KillReward는 전투 준비에 고정된 원본의 참조 동일성 계약을 유지한다. 모든 객체를 완전한 의미 동등성으로 전환한 변경은 아니다.

### 실제 프로필 읽기 추적

[CombatProfileReadSet](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatProfileReadSet.cs)은 한 Capture 동안 EntityId별 선택 결과를 기록하는 internal sealed 자료다.

- 기존 2인자 RequiredProfile의 선택 본문은 유지했다. 내부 3인자 오버로드가 같은 반환 객체를 기록한다. 보유 우선·성장 개체 누락·기본형 대체 판단의 원본은 기존 선택기다.
- Capture의 공개 ProjectRoute에서만 수집기를 만든다. 일반 실행/조회는 수집기 null 경로로 기존 동작을 유지한다. 같은 개체의 반복 읽기는 한 항목이고 혼합 선택은 예외다.
- Actor/CanLand → 이동/위치 교환·면역·정찰/흔적·토네이도 → 환경 피해 수신자/진지 구축 오라 → 피해 전후 존재/도발·결속력 방어 → 처치 특성/사냥꾼에 같은 수집기를 전달한다.
- 효과 없음·범위 밖·면역 등의 후보도 기존 계산이 프로필을 실제 읽었다면 기록한다. 수집기에 특성별 후보/효과 조건을 추가하지 않는다.
- Capture는 읽기 주체를 기존 EffectTargets에, 선택된 기본형/보유형을 Definitions/OwnedProfiles에 키 순으로 고정한다. Collectors/Owned/SoulCounts/Main/Souls는 기존 영혼 수집 책임으로 유지한다.
- 가변 수집기를 Contracts/Domain/입력 DTO/With 복사/Buffer/View에 노출하지 않는다. 새 서비스·전역/스레드 상태·이벤트·Tick은 없다. 공개/숨김 환경 필터와 기존 게시 검증도 유지한다.
- MovementProjection의 기존 `(CombatCommandInput, bool)` 생성자를 유지하고 새 3인자 구현으로 위임한다. 기존 리플렉션 호출의 호환을 확인했다.

## 3. 파일 범위와 인수할 경계

이번 수정의 코드 묶음은 **C# 61개(운영 58·기존 시험 3), 신규 meta 2개**다.

| 범위 | 파일 수 | 변경 |
|---|---:|---|
| Contracts/Commands/Traits | 38 C# | TraitSpec 1개 + 구체 Spec 37개 |
| Application/Combat | 20 C# | 기존 18개 + MovementDefinitionComparison/CombatProfileReadSet 신규 2개 |
| Assets/Tests/EditMode | 3 C# | BattlePreparationRuleTests·LegacyEnhancementRetirementTests·StatusEffectRetirementTests의 가짜 파생 타입에 필수 override만 추가. assertion/입력/실행 본문 유지 |
| 신규 코드 meta | 2 | 위 신규 2파일과 함께 인수. 기존 GUID/경로는 유지 |

MC2B의 전달 파일 목록과 각 책임은 [추적 설계 §6](../specs/2026-11-02-movement-profile-read-tracking-design.md#6-변경-파일--구현-시-허용-범위)이 원본이다. RuntimeSourceHashes 20개가 기존 최종 시험 후 바뀌지 않았음을 전달 준비 때 확인했다.

Core 인수에서 유지할 사항:

1. 새 프로필 소비 함수를 공개 이동 투영에 연결하면 같은 수집기를 하위 RequiredProfile 읽기까지 전달한다. 컴파일러가 미래의 전달 누락을 모두 검출하지는 않는다.
2. 기본/보유 프로필 선택은 RequiredProfile에, 효과 후보/산식은 기존 각 계산기에 둔다. Capture에서 다시 선택하거나 특성 후보를 재구성하지 않는다.
3. 추적 수명은 공개 ProjectRoute가 반환될 때까지다. 이후 Complete/AfterTransition 전용 처리·보상·숨겨진 실제 실행의 의존을 무조건 합치지 않는다.
4. 입력에 없는 읽기 주체·한 수명 안의 혼합 선택은 구성 오류다. 이를 정상 StaleContext나 첫 값/마지막 값 덮어쓰기로 숨기지 않는다.

설정 합성/훈련 산식·Domain 실행 규칙·DTO 생성자/With·Buffer·View·Factory/서비스·씬/에셋은 이번 수정에서 변경하지 않았다. 작업 트리에 있는 사용자 에셋/씬 변경과 다른 작업 문서는 이 묶음에 포함하지 않는다.

## 4. 같은 오류 조건에 해당했던 훈련 특성

아래는 수정 전 등급 합성 코드와 현재 훈련 설정으로 확인한 **17개 영향 범위**다. 전부 개별 실게임 재현을 완료한 목록은 아니다.

| 계열 | 특성 |
|---|---|
| 공통 5 | 선봉장, 깔끔한 뒷정리, 사기 증진, 단독 행동, 피의 부름 |
| 중장 4 | 위치 교대, 결속, 도발, 진지 구축 |
| 기동 2 | 땅 다지기, 정찰대 |
| 캐논 6 | 고정 사격, 방어구 부수기, 마무리 사격, 추적 사격, 사냥꾼, 일점 포격 |

rank 1·추가 구매 없음에서도 새 특성 객체를 만들었으며, 공격 특성도 이동 문맥의 프로필 비교에 포함돼 영향을 받을 수 있었다. 병과 기본 특성인 뛰어넘기·굳건한 의지는 UnitSpec 재합성 경로의 추가 확인 대상이다.

## 5. 검증 결과와 실패 8건

Unity 6000.0.73f1에서 최종 Core/시험 어셈블리 컴파일을 확인했다. 기존 nullable 경고 CS8632는 남아 있다. 새 시험 작성·MSBuild는 하지 않았다.

최종 EditMode 실행: **2026-11-02 06:02:34~06:03:55 KST**.

| 시험 묶음 | 전체 | 통과 | 실패 |
|---|---:|---:|---:|
| Core | 4,637 | 4,636 | 1 |
| CoreIntegration | 847 | 841 | 6 |
| 나머지 Editor 시험 | 143 | 142 | 1 |
| **합계** | **5,627** | **5,619** | **8** |

첫 전체 시험에서 발생한 MovementProjection 생성자 리플렉션 호환 실패 5건은 기존 2인자 생성자를 복구해 해소했다. 최종 TornadoGimmickPublicationTests는 5/5 통과했고 이동·위치 교환·환경/숨김·게시 관련 기존 시험군도 통과했다.

| 남은 실패 | 건수 | 근거와 상태 |
|---|---:|---|
| FbxPlacementTests.PrefabCatalog_ListsRegularAndVariantAndAddsAnIndependentVolumeProfile | 1 | EditorWindow.Close NullReferenceException. 11월 1일 결과와 같은 시험/메시지 |
| TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True) | 1 | 기대 활성화 1/실제 2. 11월 1일 및 MC1 결과와 같음 |
| CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected의 CinemachineCameraOffset 사례 | 1 | Cinemachine 형식 로드 실패. 11월 1일 결과와 같음 |
| DataBaseAssetTests.CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools | 1 | 샷건 SteadfastWill 기대 1/실제 0. 시험 당시 사용자 에셋은 PositionSwap 설정 |
| DataBaseAssetTests.GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait | 1 | KillChainAttack 기대/실제 빈 목록. 시험 당시 사용자 에셋과 기대가 다름 |
| DataBaseAssetTests.RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases | 1 | 상점 후보 샷건 1개 기대/실제 5개 |
| DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider/Heavy) | 2 | 이동 Capture 전 BattlePreparationCalculator.Place에서 초기 보드/점유 충돌. 어느 배치의 충돌인지는 미계측 |

앞의 3건은 저장된 이전 XML과 동일하다. DB/준비 5건은 같은 이전 실패 이력이 없으므로 전부 “기존 실패”로 단정하지 않는다. 다른 담당자의 시험 판정·카메라·도발·초기 배치 규칙이나 사용자 에셋을 바꿔 통과시키지 않았다.

원본 XML/로그는 `Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/`의 **로컬 산출물이며 Git ignore 대상**이다. 이번 스테이징에는 강제로 포함하지 않는다. 이 문서와 [상세 검증 기록 §14](2026-11-02-shotgun-movement-stale-context-core-request.md#14-mb1mb2-구현과-최종-검증-2026-11-02)에 전달 가능한 결과/실패 근거를 남겼다.

## 6. 사용자 확인과 잔여 인수

**사용자가 진지 구축에 문제가 없음을 확인했다.** 확인된 결과는 반영하되 rank별 전수·실제 수치 변경 거절·오라 출처 변경까지 확인한 것으로 확대하지 않는다.

남은 세부 검증:

- rank 1·추가 구매 없음 및 rank 2·3, 다른 훈련 특성에서 동일 설정 재생성 수락/실제 수치·키·선택 변경 거절.
- 움직이지 않은 진지 구축 오라 출처와 범위 밖/효과 없는 후보, 사망·면역·도착 전 종료.
- 단독 행동/결속/도발의 존재 판정, 처치 특성/사냥꾼 설정 읽기.
- 같은 기본형을 공유한 개체의 EntityId/OwnedId 결합, 기본형 대체/보유형 선택, 미사용 프로필만 바뀐 경우.
- 숨겨진 환경만 바뀐 경우와 기존 전투/행위자/초안 경로 변경 거절.
- StaleContext가 다시 재현되면 실제 실패 비교 항목의 별도 계측. 사용자 정상 확인만으로 원래 결과 8의 실패 항목을 역으로 확정하지 않는다.

추가 구현 변경을 Core에 요청하는 상태는 아니다. 현재 구현을 인수하고 위 소스 계약/잔여 검증을 이어갈 수 있도록 전달한다.

## 7. 설계·계획·조사 원본

- [정의/특성 비교 설계](../specs/2026-11-02-movement-context-definition-comparison-design.md) · [상위 Task 계획](../plans/2026-11-02-movement-context-definition-comparison-plan.md)
- [실제 프로필 읽기 추적 설계](../specs/2026-11-02-movement-profile-read-tracking-design.md) · [추적 Task 계획](../plans/2026-11-02-movement-profile-read-tracking-plan.md)
- [Core 구조 우선 검토](../specs/2026-11-02-movement-context-definition-comparison-review.md)
- [조사·단계별 구현/시험 이력](2026-11-02-shotgun-movement-stale-context-core-request.md)

이전 통합 인계 파일은 이번 문서 작성/기록에 사용하지 않았다. 이 문서가 Core 담당자에게 전달할 현재 요약이며 세부 계약·실행 이력은 위 원본에서 관리한다.
