# 런 덱 View 호출처 이관 — 현강님 공유

작성: 2026-10-20, View 작업자(용근) 세션. **미전달.**

천명님 Core → View 인계([core-view-handoff](2026-10-19-run-deck-core-view-handoff.md) §4·§6)의 View 쪽을 사용자 지시로 이 세션이 고쳤습니다. 현강님 파일이라 확인 부탁드립니다.
- 설계: [run-deck-view-integration-design](../specs/2026-10-20-run-deck-view-integration-design.md) RV3·RV4·RV6
- 계획: [run-deck-view-integration-plan](../plans/2026-10-20-run-deck-view-integration-plan.md) RD-2
- 검증은 건너뛰었습니다(사용자 지시). 컴파일은 아직 확인하지 않았습니다.

## 바뀐 것

**`DeckCardList`**
- 보유 목록에서 `Role == Main`으로 찾던 코드를 지웠습니다.
- 슬롯은 `CurrentDeckIds()` 하나로 채웁니다.
  - 전투 중: `CombatStateReadSink.Current.Membership.OrderedIds`
  - 전투 밖: `RunDeckReadSink.TryGetCurrent` → `OrderedIds`
  - 덱이 없으면 모두 비웁니다.
- 메인은 Core가 0번에 두므로 0번 슬롯이 그대로 메인 칸입니다. 덱이 3장이면 넷째 칸은 비어 있습니다.
- `ShowCard(slot, OwnedCharacterRecord)`를 `ShowCard(slot, CharacterInstanceId)`로 바꿨습니다(전에도 ID만 썼음).
- 필드 `_characters` 대신 `_deck`·`_combatState`를 둡니다.

**`OwnedCharacterStatsReader`**
- `deck`을 필수로 바꿨습니다(R6). 필수 인자는 선택 인자 뒤에 올 수 없어서 순서가 `(characters, combat, stats, deck, faults = null)`로 바뀌었습니다.
- null이면 `ArgumentNullException`을 던집니다.
- 호출처 셋을 맞췄습니다: `MainCharactorUI`·`UnitCardUI`(위치 인자로 `buffer.RunDeckReadSink, _faults`), 시험 도우미 `OwnedStatsUITestServices.cs:146`(컴파일용 한 줄, 단정 그대로 — 사용자 결정 Q-RV7).

**`CombatDeploymentConfirmation`** (아래 "확인 부탁"의 문제를 사용자 결정 Q-RD3 A로 같이 고쳤습니다)
- 생성자 둘째 인자로 참가 목록 `Func<RunDeckSnapshot> membership`을 받습니다. 운영 조립은 `CombatViewSystem`에서 `() => _state.Current?.Membership`입니다.
- `CheckMainPlacement`: `membership.MainId`의 배치를 봅니다.
- `HasUnplacedUnits`: `Owned` 전체 대신 `membership.OrderedIds`를 돕니다. 덱 밖 보유 유닛 때문에 확인창이 뜨는 일이 없어집니다.
- 시험 `CombatDeploymentConfirmationTests.cs:33` 생성자 호출 한 줄에 참가 목록 `[3, 1, 2]`를 넣었습니다(옛 보유 목록과 같은 ID라 사례 결과는 같습니다).

## 시험 영향 (시험 코드는 이번 범위 밖이라 고치지 않음)

- **`DeckButtonUITests.MainFirstAndThreeSubCards_ReuseInstances_AndBlockOnlyPlacement`:** 로스터만 넣고 덱은 설치하지 않습니다. 그래서 이제 카드가 하나도 안 보여 실패할 것입니다.
  - `CharacterBufferTestData.InstallFixtureDeck(buffers)`처럼 덱을 설치해야 합니다.
  - 이 시험은 퇴역한 key 14도 씁니다.
- `CombatDeploymentConfirmationTests`: 가짜 `Characters`에 메인이 `OwnershipRole.Main`으로 들어 있으면 결과는 같습니다.

## (고침, Q-RD3 A) 같은 파일의 다른 곳 — 처음 찾았을 때의 기록

`CombatDeploymentConfirmation.HasUnplacedUnits`도 **보유 목록 전체(`Owned`)**를 돕니다.
- 이제 전투에는 덱에 든 유닛만 들어갑니다(Run 덱 설계 D10). 그래서 덱 밖 보유 유닛이 있으면 그 유닛은 전투 캐릭터가 없어 "미배치"로 셉니다. 그러면 "미배치 유닛이 있습니다" 확인창이 늘 뜹니다.
- 지금 커밋된 시작 로드아웃은 보유 3장이 모두 덱이라 드러나지 않습니다. 작업 트리 로드아웃처럼 덱 밖 유닛(Cowboy)이 생기면 드러납니다.
- 고치려면 `Owned` 대신 참가 목록(`CombatStateReadSink.Current.Membership.OrderedIds`)을 돌아야 합니다. 이때 생성자에 전투 상태 읽기가 하나 늘어나므로, 우리 조립(`CombatViewSystem`) 한 줄과 현강님 시험 생성 한 줄이 같이 바뀝니다. → 용근 세션 질문 Q-RD3

## 추가 (RD-4) — 새 UI `DeckEditUI`와 `UIId.DeckEdit`

- **`UIId.DeckEdit = 32`**를 추가했습니다(70aa3e5 병합에서 29~31이 먼저 쓰여 32, 비어 있던 12는 쓰지 않음).
- **새 UI** `GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs`(+ 계약 `IDeckEditDisplay`·`IDeckEditInput`)
  - StageSelect 덱 편집 팝업입니다.
  - 루트는 항상 켜 두고, 열기 버튼은 Panel 밖에 있습니다.
  - 덱 칸과 보유 칸은 기존 `UnitCardView`에 값만 넣습니다(`ShowCharacter`·`ShowDeckDetails`·`ShowAbilityContent`).
  - 특성 문구는 `UnitCardTraitContent.Create`를 재사용합니다.
  - 이름·아이콘·특성은 `GetUIData`에서 View DB로 찾습니다(`UnitCardUI`와 같은 방식).
  - 능력치는 Core 덱 읽기 한 번의 값만 씁니다. 그래서 Buffer를 따로 읽는 `UnitCardUI`는 재사용하지 않았습니다.
- 정책은 기본값(Exclusive)이라 상점·회복 화면과 같습니다.

## 추가 (RD-4 보충) — 덱 편집 프리팹

- 도구 `Editor/Stage/StageDeckEditUISetup.cs`가 `Prefabs/UI/Deck/DeckEditUI.prefab`을 만듭니다.
- 카드 칸은 `Prefabs/UI/UnitCard.prefab`을 중첩해 씁니다(덱 0.6배·보유 0.45배, `UnitCardUI` 없음).
- 글꼴은 함렡 Extra Bold SDF입니다.
- 모양(배치·색·크기)은 임시입니다. 다듬어 주시면 됩니다. 필수 참조는 `DeckEditUI`의 Tooltip을 보면 됩니다.
