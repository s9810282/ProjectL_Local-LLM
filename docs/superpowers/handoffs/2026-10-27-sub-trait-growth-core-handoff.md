# 서브 특성 성장 — Core → View·기획 인계

> **과거 인계 — 재전달·구현 기준으로 사용하지 않음(2026-10-29 정정).** 아래의 “메인에는 적용하지 않는다”, “메인은 제외”와 옛 API·고정 획득/강화 칸은 당시 계약 이력이다. [현재 Core 인계](2026-10-27-category-trait-progression-handoff.md)가 이 문서를 대체한다. 현재는 **메인·서브 모두** 설정된 레벨에 특성을 선택해 획득·강화할 수 있고 UI도 공통 경로를 사용한다. 최신 콘텐츠 상태는 [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md)를 따른다.

Date: 2026-10-27
Status: G-1~G-3 Core 코드와 시험 코드 작성, G-4 문서 정리. **Unity 컴파일·Core/CoreIntegration 시험·PlayMode 미실행.** 인계 문구는 준비했으며 외부로 전달하지 않았다.
Source: `feature/Cheon/Core/NewCombatSytem`, 현재 HEAD `928362f7c`(Refector-Stage fast-forward 병합), `5a1bfbe1`에서 작성한 특성 성장 미커밋 변경 보존. 계획 §8.7 참고.
Plan: [Task 계획](../plans/2026-10-27-sub-trait-growth-plan.md), Design: [설계](../specs/2026-10-27-sub-trait-growth-design.md).

## 1. 인수 상태와 담당

| 항목 | 현재 상태 | 다음 담당 |
|---|---|---|
| 획득 1칸·강화 1칸, 최대 특성 2개·강화 1개 | Core 계약·규칙·합성·Buffer 연결 코드 반영 | 사용자 Unity 확인 |
| 레벨 선택 조회·명령 | 기존 `ICharacterRosterService`에 공개, UI 연결 전 | View |
| 상점 | 종류별 빈 칸 판정, 레벨 조건 우회, 판매 허용 키 별도 유지 | 사용자·View의 실행 확인 |
| SO 입력·DB 조기 검사(G1.18·G1.19) | **미구현**, §4 | View/DB 담당 |
| 획득·강화 레벨 값과 추가 콘텐츠 | 레벨 입력 필드 연결 전. 현재 사용 가능한 강화 후보 없음 | 기획 |
| Unity 시험 | 미실행. TP-9 Framework 시험 삭제는 Refector-Stage 병합으로 반영 | 사용자 |

Core 선택은 무료·확정이며 메인에는 적용하지 않는다. 상점과 레벨 선택이 같은 칸을 쓰므로 상점에서 먼저 산 칸은 레벨이 되어도 다시 고를 수 없다. 획득 칸 → 강화 칸 순서로 합성하여 획득한 특성도 강화할 수 있다.

## 2. View 전달 문구(채팅 붙여넣기용)

```text
서브 유닛 특성 성장 Core 인계입니다. Core 코드·시험 코드는 작성됐고 Unity 컴파일/시험/재생 확인은 아직입니다.

기존 Core 그룹의 Characters(ICharacterRosterService)에 API 두 개를 추가했습니다.
- GetTraitGrowthChoices(ownedId): TraitGrowthChoices.OwnedId와 Items 반환.
  각 행은 Key(강화 정의 키), Name, Description, Kind(Acquire/Enhance), TraitKey입니다.
  TraitKey는 획득이면 더할 특성, 강화면 교체할 특성입니다. 아이콘은 View 외형 표로 연결합니다.
- ChooseTraitGrowth(ownedId, key): TraitGrowthResult.Succeeded, FailureReason, Final 반환.
  성공일 때만 Final에 저장된 보유 기록이 있습니다. 비용은 없고 선택은 확정입니다.

결과 연출·전투 정리가 끝난 뒤 StageSelect에서 재조회해 Items가 있는 유닛에 선택 알림을 표시하고,
후보 이름/설명/아이콘을 보여 준 뒤 선택한 Key로 명령을 호출해 주세요.
성공하면 후보와 유닛 정보를 재조회합니다. 유닛 정보는 기존 CharacterStats.ReadOwned()의
Items[].Stats.TraitKeys에 성장 결과가 반영되므로 View에서 특성을 다시 합성할 필요가 없습니다.
보유 개체 구분에는 CharacterKey가 아닌 CharacterInstanceId(ownedId)를 사용합니다.

조회는 전투 중에도 되지만 선택은 전투 기록이 남아 있으면 InBattle로 거절됩니다.
조회 결과는 예약이 아닙니다. 명령을 보낼 때 현재 상태로 다시 판정합니다.
Items가 비었다고 최대 성장이라고 단정하면 안 됩니다(잠김/후보 없음/메인/미보유도 빈 결과).
레벨 열림 판정과 후보 필터는 Core가 맡고, View는 조회 결과를 표시합니다.
저장은 위 명령 경계로 요청하며 View에서 Buffer의 Growth 칸을 직접 쓰지 않습니다.

실패 이유:
- NotSub: 메인은 대상 아님. NotOwned: 보유 대상 없음.
- NotUnlocked: 레벨/활성화 조건 미충족 또는 그 칸을 이미 선택함.
- NotCandidate: 현재 가진 특성에 적용 불가하거나 보류/모양/종점 조건에 맞지 않음.
- UnknownDefinition: 그 유닛의 정의 목록에 선택 키가 없음.
- InBattle: 전투 정리 후 재시도. RunEnded: 종료된 Run에서는 선택 종료.
- SourceChanged: 조회 중 Run 또는 로스터가 바뀜. 화면과 후보를 재조회.
실패하면 성장 기록을 저장하지 않습니다. DB 초기화·카테고리 표·서비스 바인딩 누락은 설정 예외입니다.

상점도 같은 획득/강화 칸을 씁니다. 같은 유닛이 다음 강화 상점에서 다른 종류의 칸을 살 수 있습니다.
한 상점 방문에서 한 번만 구매하는 정책은 유지됩니다.
ShopFailureReason.AlreadyEnhanced의 이름/번호는 같고, 뜻은 '그 상품 종류의 칸이 이미 참'입니다.
이미 성장한 유닛 전체를 일괄 구매 불가로 표시하지 않도록 관련 표시·문구를 확인해 주세요.
현재 ShopResultPolicy는 이 사유를 Mismatch로 분류합니다. 문구·처리는 View 인수 때 확인할 항목입니다.

남은 DB/SO 작업: CategoryConfigDataSO.LevelProgressionInput에 subTraitAcquireLevel/subTraitEnhanceLevel을
추가하고 LevelProgressionSpec의 같은 이름 인자로 전달해야 합니다(0=없음, 2 이상=열림 레벨).
EnhancementData.Validate에는 TraitGrowthRules.KindOf(AddedTraits.Count, ReplacedTraits.Count)가
None이면 거절하는 검사를 연결해야 합니다. 규칙을 DB에 복사하지 않습니다.
현재 SO는 두 레벨을 전달하지 않아 모두 0입니다. 이 연결과 기획 값 설정 전에는 레벨 선택이 열리지 않습니다.

현재 콘텐츠: ShotGun의 2연발/도어 브리칭, DynamiteHammer의 연쇄 폭발은 획득 후보입니다.
강화 후보는 0개입니다. CactusCannon 독 가시는 ThornField 보류 때문에 후보에서 제외됩니다.
획득한 특성의 강화형은 구조상 지원하지만 실제 후보 콘텐츠를 추가해야 화면에서 확인할 수 있습니다.
```

## 3. 기획 전달 문구(채팅 붙여넣기용)

```text
서브 특성 성장 구조를 Core에 반영했습니다. 레벨에 도달하면 플레이어가 획득 1회·강화 1회를
무료로 확정 선택하며, 최대치는 특성 2개·그중 강화 1개입니다. 메인은 제외입니다.
강화 레벨을 획득 레벨보다 낮게 두면 기본 특성을 먼저 강화할 수 있고, 선택은 미뤄도 됩니다.
상점으로 먼저 산 종류는 레벨 선택에서도 사용한 것으로 취급합니다.

레벨은 CategoryConfig_Default의 Level Progression에 전역 값 둘을 연결할 예정입니다.
- subTraitAcquireLevel: 획득 칸이 열리는 레벨.
- subTraitEnhanceLevel: 강화 칸이 열리는 레벨.
0은 해당 선택 없음, 나머지는 2 이상입니다. 현재 SO 필드는 View/DB 인계 작업이 남았습니다.
연결 후 두 레벨의 기획 값을 정해 주세요. 지금 운영 설정에서 경험치는 켜져 있지만 두 특성 레벨은 기본 0입니다.

후보는 기존 EnhancementDataSO로 만듭니다.
- 획득: AddedTraits 정확히 1개, ReplacedTraits 0개. 현재 없는 특성만 더합니다.
- 강화: AddedTraits 0개, ReplacedTraits 정확히 1개. 가진 특성과 같은 TraitKey를 강화 수치로 교체합니다.
획득한 특성의 강화형은 같은 유닛의 해당 획득 정의도 강화 표에 등록되어 있어야 합니다.
후보는 레벨 선택에서 그 유닛의 전체 정의를 읽습니다. 상점에서 팔려면 해당 강화 스테이지의
CandidateKeys에도 넣어야 합니다. 레벨 선택은 Cost/Weight를 사용하지 않고 상점만 사용합니다.

현재 유효한 획득 후보는 ShotGun 2연발/도어 브리칭, DynamiteHammer 연쇄 폭발의 3개입니다.
현재 사용 가능한 강화 후보는 0개입니다. CactusCannon 독 가시는 ThornField 보류로 제외됩니다.
후보가 없는 유닛은 레벨에 도달해도 선택할 것이 없습니다. 필요한 유닛별 획득/강화형 콘텐츠를 정해 주세요.
UI·SO 연결과 Unity 검증이 남아 있으므로 지금 바로 플레이 가능한 완료 상태를 뜻하지 않습니다.
```

## 4. View/DB 인수 작업의 정확한 위치

G1.18 대상: [CategoryConfigDataSO.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigDataSO.cs)의 `LevelProgressionInput`.

- `[SerializeField, Min(0)]`로 `subTraitAcquireLevel`·`subTraitEnhanceLevel` 추가, 툴팁에 0 또는 2 이상을 명시한다.
- `ToData()`에서 `new LevelProgressionSpec(..., subTraitAcquireLevel: subTraitAcquireLevel, subTraitEnhanceLevel: subTraitEnhanceLevel)`로 전달한다. 기존 5개 인자의 뜻은 그대로다.
- 기존 경험치 입력 다섯 개가 모두 0이면 `default`인 판정에 새 두 값은 포함하지 않는다(계획 G1.18). 경험치가 꺼졌을 때는 특성 선택도 열리지 않는다. 활성 설정에서 0 또는 2 이상 검사는 Core 생성자가 담당한다.
- 운영 에셋: [CategoryConfig_Default.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Category/Item/CategoryConfig_Default.asset). 값은 기획 결정 뒤 입력한다. 이 세션에서는 파일·값을 바꾸지 않았다.

G1.19 대상: [EnhancementData.cs](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Enhancement/Item/EnhancementData.cs)의 `Validate()`.

- `TraitGrowthRules.KindOf(AddedTraits.Count, ReplacedTraits.Count) == TraitGrowthKind.None`이면 데이터 오류로 거절한다. Application.Roster의 공통 함수를 호출하며 별도 모양 판정을 만들지 않는다.
- 이 검사는 정의 모양의 조기 검사다. 보유 개체별 특성 수·중복·교체 대상은 Core `Candidates`가 판정한다.
- 획득 특성 강화형의 저작 경로 검사는 상점 어댑터의 `ValidateDefinition`에 이미 연결했다. 선행 획득 정의를 검사에 읽었다고 상점 판매 키가 늘어나지 않는다.

## 5. 검증과 운영 활성화 순서

1. TP-9 Framework 시험 삭제는 후속 Refector-Stage 병합으로 반영했다. 현재 해당 경로의 tracked 파일·디스크 `.cs`는 0개이며 `DevelopmentGrantServiceTests.cs`도 없다. CoreIntegration 시험은 유지했다. 삭제 인수는 Unity 전체 컴파일 성공과 구분한다.
2. 사용자가 Unity에서 컴파일하고 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 실행한다. 새 `TraitGrowthChoiceServiceTests` 10개도 이 범위에 포함된다. 이 문서에는 통과 결과가 없다.
3. View/DB가 §4의 SO 연결·정의 검사를 완료한 뒤 DB 에셋 검증과 Inspector 직렬화·변환을 확인한다. `DataBaseAssetTests`는 TP-9 삭제 이후 인수된 시험 위치가 제공될 때 사용한다. 이를 위해 삭제 대상 Framework 시험을 수정·복원하지 않는다.
4. 기획 값 연결 후 ShotGun을 획득 레벨까지 올려, 전투 정리 뒤 후보 조회 → 하나 선택 → 재선택 거절 → 유닛 정보/다음 전투 반영을 확인한다. 상점 구매가 같은 칸을 소비하는지도 확인한다.
5. 실제 강화 콘텐츠가 생기면 기본 특성 강화와 획득 특성 강화, 같은 유닛의 다음 상점에서 다른 종류 구매를 재생 확인한다. 현재 강화 콘텐츠 0개 상태에서는 이 경로를 운영 에셋으로 확인할 수 없다.

Core 시험 코드 작성, 정적 검사·소스 리뷰, Unity 시험 통과, View 연결, 실제 재생 확인은 각각 다른 상태다. G-4는 문서 인계까지이며 후자의 확인을 대신하지 않는다.

## 6. 코드 진입점

- [CharacterRosterService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs): 조회·선택·저장 전 원본/Run/전투 검사.
- [TraitGrowthChoices.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Roster/TraitGrowthChoices.cs), [TraitGrowthResult.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Roster/TraitGrowthResult.cs): 응답 계약.
- [TraitGrowthRules.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/TraitGrowthRules.cs): 공통 열림·후보·순수 선택·정의 검사.
- [TraitGrowthChoiceServiceTests.cs](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthChoiceServiceTests.cs): 실제 Core/Buffer 조립과 재진입 상태 변경 사례(미실행).
