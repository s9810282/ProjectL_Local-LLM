# 새 Core 규칙 대응 — 로비·스테이지 선택 계획

- 작성자: 호진(이호진)
- 상태: 코드 착수 전. 설계 Q-NC1~Q-NC8 결정 대기. Core 병합(`9748bd3a`, 10-27)으로 NC-0b 착수 조건은 충족했다. 이 계획은 권장안(Q-NC1~Q-NC8 모두 A)을 전제로 쓴 초안이고, 설계 §2.1의 10-27 변화는 아직 반영하지 않았다(NC-0b에서 맞춘다).
- 설계: [새 Core 대응 설계](../specs/2026-10-26-new-core-lobby-stage-response-design-Hojin.md)
- 정리: 2026-10-27 머리말·상태를 다듬고 진행 요약·남은 일을 앞에 두었다. Phase·Task 본문은 그대로다. 옛 판은 git `10914570`.
- 계획 판: Core 끝 `4bd6b574f`, 우리 HEAD `334fb533a`. 줄 번호는 `4bd6b574f` 판이고, NC-0b에서 병합 판으로 다시 센다.

## 1 진행 요약

| 작업 | 내용 | 상태 |
|---|---|---|
| NC-0a | Core 요청 문서(CR1·CR2) | 미착수(결정 대기). 요청 문서 파일 없음 |
| NC-0b | 병합 판 대조(문서만) | 미착수. 병합 조건 충족(`9748bd3a`) |
| NC-1 | 카드 내용 한 곳(설계 D1·D2·D3) | 미착수 |
| NC-2 | 쓸 수 없는 특성 표시(설계 D5) | 미착수. 전투 카드 표시는 10-26 용근님이 먼저 넣음(설계 §2.1) |
| NC-3 | 공유·인계(문서만) | 미착수 |
| NC-4 | Core 목록 인수(CR1 뒤) | 미착수 |

## 2 남은 일

- [ ] 사용자 결정 Q-NC1~Q-NC8(설계 §8). 10-27 상황 열을 함께 본다 — 특히 Q-NC2(우선도 줄은 이미 빠짐), Q-NC5(용근님 문구가 이미 있음), Q-NC6(Core가 이미 거름).
- [ ] 결정 뒤 NC-0a. CR2는 Core가 같은 결과를 냈으므로(`d586bbf1`) 요청 문서에서 뺄지 결정 때 정한다. CR1은 Core의 `TraitAvailability`(`80567226`)를 보고 다시 쓴다.
- [ ] NC-0b 대조에서 설계 §2.1의 변화를 §3·§5·§6과 이 계획 P2·P6·NC-1·NC-2에 반영한다(예: `ShowDetails`는 지금 `int×6` 오버로드, 정보 줄 표기는 `Atk AP`·`Move AP`).
- [ ] 확인 필요: 충격 볼트(`ImpactBoltDescription`) 설정값이 카드 서식에서 빠져 있는지(설계 §2.1).

## 3 진행 규칙

### 3.1 공통

- Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일과 재생을 확인한 뒤 다음 Phase로 간다.
- **새 시험 코드·시험 도우미를 만들지 않고, MSBuild를 돌리지 않는다.** EditMode 시험 실행을 확인 단계로 두지 않는다. C#을 바꾼 보고에는 "컴파일 미확인"을 적는다.
- 기존 시험 조정:
  - 우리 변경으로 컴파일·실행이 깨지는 곳만 최소로 맞춘다.
  - 다른 사람 시험도 묻지 않고, 실제로 깨질 때만 고친다. 고친 것은 NC-3 공유 메모에 줄 번호와 함께 적는다.
- 새 파일은 `.meta`와 함께 만든다. 네임스페이스는 폴더 경로다(`ProjectL.<Script 아래 경로>`).
- 코딩 규칙은 `CLAUDE.md`를 따른다(Allman, `sealed`·`static` 명시, 한국어 XML doc, 예외 규약). UI는 `Assets/AGENTS.md` 규약대로 등록·입력·수명을 바꾸지 않는다.
- 다른 작업자 파일은 설계 §6 표의 줄만 바꾼다. 덱 편집(용근님) 줄은 Q-NC1 A의 허락 범위 안에서만 바꾼다.
- 씬·프리팹·View SO 값은 바꾸지 않는다. 확인할 것이 있으면 Codex에게 넘길 목록으로 보고에 적는다.
- Phase 끝 Codex 검수: 중계기에 검수 요청 명령이 없다. 사용자가 Codex에 직접 요청할 수 있게 보고에 검수 대상과 범위를 적는다. 검수 결과가 오면 지적마다 수용·근거 있는 반려·사용자 판단을 정한다.
- 경로 약어: `U/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/`, `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, `T/` = `Assets/Tests/EditMode/`.

### 3.2 착수 조건

- 설계 결정(Q-NC1~Q-NC8).
- NC-0a(Core 요청 문서)는 결정 직후 바로 한다.
- NC-0b부터는 Core 브랜치 병합 뒤 한다 — 10-27 `9748bd3a`로 충족. 전투 View 충돌은 이 계획이 풀지 않는다.
- 설계·이 계획의 커밋은 사용자가 한다.

### 3.3 계획에서 정한 것 (확인 대상)

설계에 적히지 않은 구현 세부다. 바꾸려면 보고 때 말해 달라.

| ID | 정한 것 | 이유 |
|---|---|---|
| P1 | `UnitCardStats`와 이를 받는 `UnitCardView` 메서드는 `internal`이다 | `UnitCardView`는 `public` MonoBehaviour라 `public` 메서드가 `internal` 형식을 받으면 CS0051이다. 호출자는 같은 어셈블리, 시험은 `InternalsVisibleTo`로 닿는다 |
| P2 | `UnitCardView.ShowDetails(int×7)` 공개 오버로드를 지운다(10-27 지금은 `int×6`) | 호출자는 같은 파일의 `ShowDetails(CharacterData)`와 시험 한 줄뿐이다(정적 검색, Editor·UnityEvent 호출 없음). 남기면 정보 줄 구성이 두 벌이 된다 |
| P3 | `UnitCardUI.Refresh`에서 특성 내용을 사망·배치 표시보다 먼저 채운다 | 조립을 `UnitCardContent` 한 번으로 묶기 때문이다. 셋은 서로 다른 칸을 켜고 끄므로 결과는 같다 |
| P4 | 값이 없는 카드는 `UnitCardContent.Clear(view)`(정보 줄 비움 + 빈 특성 내용)로 그린다 | 지금 `UnitCardUI`는 빈 키 목록으로 빈 내용을, `DeckEditUI`는 직접 빈 내용을 넣는다. 결과가 같은 두 길을 하나로 |
| P5 | `DeckEditCardModel`은 `OwnedStats.OwnedId`가 카드 ID와 다르면 `ArgumentException`을 던진다 | 잘못된 조립을 성공처럼 넘기지 않는다(`Assets/AGENTS.md` 예외 규약). Builder는 ID로 찾으므로 정상 경로에서는 일어나지 않는다 |
| P6 | Q-NC2 A의 정보 줄 글: `ShowDetails`는 `HP : h / m`·`DEF : d`·`ATK : a`·`ATK COST : c`·`MOVE COST : mc`·`MOVE RANGE : r`, `ShowDeckDetails`는 `: 레벨`·`: 최대 체력`·`: 공격`·`: 방어`·`ATK COST : c`·`MOVE COST : mc`·`MOVE RANGE : r` | 줄 순서를 지키고 우선도 줄 자리만 바꾼다. 앞의 네 줄은 설계 때 글 그대로다(10-27 지금 표기는 `Atk AP`·`Move AP`, NC-0b에서 다시 정함) |
| P7 | Q-NC5 A 문구는 `UnitCardTraitContent` 안의 상수 셋이다 | 끌어오기·표식 설명 글도 같은 파일에 있다. 한 곳에서 고친다 |

## 4 Phase와 Task

고치는 파일·줄·주인의 원본은 설계 §6이다. NC-0b에서 병합 판 기준으로 다시 센 결과를 거기에 적는다.

### 4.1 Phase NC-0a Core 요청 문서 (문서만)

| Task | 할 일 | 끝난 상태 |
|---|---|---|
| a.1 | `docs/superpowers/handoffs/2026-10-26-new-core-card-shop-core-request-Hojin.md`를 쓴다. 내용은 설계 §7의 CR1·CR2, 근거 줄(설계 §3 F3~F5), View가 받은 뒤 할 일, 받기 전 View의 임시 경로(설계 D4)다. Q-NC4·Q-NC6이 A가 아니면 해당 요청을 빼거나 B로 바꾼다 | 파일이 있고 미전달로 표시돼 있다. 전달은 사용자가 한다 |
| a.2 | 설계 머리의 상태 줄을 갱신한다 | 결정 결과와 요청 문서 링크가 있다 |

검수 범위(Codex): 요청 문서가 설계 §7과 같은지, Core 계약 이름·줄이 병합 판과 맞는지, View가 Core 규칙을 대신 계산하는 내용이 없는지.

### 4.2 Phase NC-0b 병합 판 대조 (문서만)

| Task | 할 일 | 끝난 상태 |
|---|---|---|
| b.1 | 병합 커밋(`9748bd3a`)과 그 뒤 들어온 Core 커밋을 기록하고 `1ba7aec47`·`c816c46df`·`4bd6b574f`가 들어왔는지 본다 | 기준 판이 정해진다 |
| b.2 | 계약 대조: `OwnedCharacterStatsSnapshot`(설정값 칸, `TraitAvailability`, CR1 목록이 왔는지), `CharacterBaselineStatsSnapshot.MoveRange`, `UnitStatsSnapshot.MoveRange`, `TraitStatusSnapshot.IsAvailable`·`PartialUnavailableReason`, `UnitCardTraitContent`의 두 `Create`와 10-26 사용 불가 표시, `UnitCardView` 정보 줄, `DeckEditCardModel`의 칸, `EnhancementOfferPlanner`(CR2가 왔는지), `DeferredCombatFeatures`의 꺼진 목록 | 설계 §2.1·§3·§5와 다른 점 목록(없으면 "같음") |
| b.3 | 설계 §6 표의 줄 번호·줄 주인을 병합 판 `git blame`으로 다시 센다 | 설계 §6 갱신 |
| b.4 | 바뀌는 멤버의 소비자를 다시 찾는다: `ShowDetails(int`·`ShowDeckDetails`·`new DeckEditCardModel`·`.HuntersMarkDescription`·`.PullDescription`·`SetPanelContent`(시험·Editor 도구 포함) | 소비자 목록. 설계 §6에 없는 곳이 있으면 멈추고 묻는다 |

설계와 다른 결정이 필요해 보이면 구현하지 않고 묻는다. 검수 범위(Codex): b.2~b.4의 대조가 실제 판과 맞는지.

### 4.3 Phase NC-1 카드 내용 한 곳 (설계 D1·D2·D3)

| Task | 파일 | 할 일 |
|---|---|---|
| 1.1 | `U/Global/UnitCardStats.cs`(새, `.meta`) | `internal readonly struct UnitCardStats`: `Health`·`MaxHealth`·`Defense`·`Attack`·`AttackCost`·`MoveCost`·`MoveRange`. `internal` 생성자, `From(CharacterData)`(유효 방어·유효 공격 + `Stats.AttackCost`·`MoveCost`·`MoveRange`), `From(CharacterBaselineStatsSnapshot)`(`PhysicalDefense`·`AttackPower` 등). 인자 null은 `ArgumentNullException` |
| 1.2 | `U/Global/UnitCardView.cs` | `internal void ShowDetails(UnitCardStats)`, `internal void ShowDeckDetails(int? level, UnitCardStats)`. 글은 P6. `ShowDetails(CharacterData)`는 `ShowDetails(UnitCardStats.From(character))`를 부르고 `SetDead`는 그대로. 정수 오버로드 삭제(P2). `ShowStatValues`·체력 연출은 그대로 |
| 1.3 | `U/Global/UnitCardContent.cs`(새, `.meta`) | `internal static class UnitCardContent`: `ShowOwned(view, visuals, level, OwnedCharacterStatsSnapshot)` = `ShowDeckDetails(level, From(owned.Stats))` + 특성 내용(지금 칸 셋을 칸 오버로드로, 설계 D4), `ShowCombat(view, visuals, level, CharacterData)` = 같은 일을 전투 값과 `Stats.TraitStatuses`로, `Clear(view)`(P4). `visuals`가 null이면 지금처럼 빈 특성 내용 |
| 1.4 | `U/Global/UnitCardUI.cs:239-266` | `Refresh` 뒷부분의 정보 줄·특성 조립을 `UnitCardContent` 호출 셋으로 바꾸고 `SetPanelContent`를 지운다(P3). 레벨 계산·`ShowCharacter`·`SetDead`·`SetDeployed`·갱신 판정(`Update`)은 그대로 |
| 1.5 | `V/StageMap/Deck/DeckEditDisplayModel.cs` | `DeckEditCardModel`: `HuntersMarkDescription`·`PullDescription`·부분 짝 검사·쓰지 않게 된 using 삭제. `OwnedCharacterStatsSnapshot OwnedStats` 추가, `Stats => OwnedStats?.Stats`, P5 검사. 생성자 인자 `CharacterBaselineStatsSnapshot stats` → `OwnedCharacterStatsSnapshot ownedStats` |
| 1.6 | `V/StageMap/Deck/DeckEditModelBuilder.cs:38`·`:46-47` | 자리 카드(`:38`)는 null 그대로, 보유 카드(`:46-47`)는 찾은 스냅샷을 넘긴다. `:74-76`의 직군 수 세기는 파생 `Stats`로 그대로 동작 |
| 1.7 | `U/Stage/DeckEditUI.cs:236-271` | `ShowCard`: `ShowCharacter`는 그대로, 값이 없으면 `UnitCardContent.Clear`, 있으면 `UnitCardContent.ShowOwned(view, _traitVisuals, card.Level, card.OwnedStats)` |
| 1.8 | 시험 | `T/Framework/UnitCardViewTests.cs:230`: `new UnitCardStats(…)`를 넘김(현강님 줄). `T/CoreIntegration/HuntersMarkCardContentTests.cs:76-78`: `card.OwnedStats.HuntersMarkDescription`(천명님 줄). NC-0b b.4에서 더 나온 곳도 같은 방식 |
| 1.9 | 정적 확인 | 지운 멤버를 부르는 곳 0, `public` 메서드가 `internal` 형식을 받지 않음, null 경로(덱 자리 카드·`_traitVisuals` null·보유 스냅샷 없음) 동작이 같음, 매 프레임 경로에 새 할당 없음(`UnitCardUI.Update`는 참조 비교 그대로). 동작 차이 목록(정보 줄 마지막 줄, 덱 편집 가시 설명)을 실행 기록에 적는다 |

사용자 확인(컴파일 미확인으로 보고):

1. Unity 컴파일 오류 없음.
2. 스테이지 선택 덱 버튼 → 카드 hover·고정: 정보 줄에 `MOVE RANGE`, 샷건 표식 10%, 카우보이 끌어오기 1칸.
3. 덱 편집 팝업 카드: 2와 같은 정보 줄·특성 글. 선인장 대포의 가시 설명이 설정값 글(View DB 고정 글이 아님)로 나온다.
4. 전투: 손패·정보창 카드의 정보 줄에 `MOVE RANGE`.
5. 로비 덱 팝업·메인 체력 표시가 그대로.

Codex에게 넘길 것(확인만): `UnitCard.prefab`·`DeckEditUI.prefab`·`Combat.unity`·`LobbyDeckPanel.prefab`의 카드 정보 글 옆에 우선도 아이콘이 있는지. 있으면 바꿀지는 사용자가 정한다.

검수 범위(Codex): 새 파일 둘과 바꾼 파일 다섯, 시험 두 줄. 기준은 설계 §4·§9, P1~P6, 다른 작업자 줄이 설계 §6 범위 안인지.

### 4.4 Phase NC-2 쓸 수 없는 특성 표시 (설계 D5)

| Task | 파일 | 할 일 |
|---|---|---|
| 2.1 | `U/Global/UnitCardTraitContent.cs` | `Append`가 받은 상태 스냅샷의 `IsAvailable`이 false면 이름 뒤 표지와 설명 첫 줄, `PartialUnavailableReason`이 비어 있지 않으면 설명 끝 한 줄을 더한다(Q-NC5 A 문구, P7). 사유 코드는 읽지 않는다. 상태 스냅샷이 없는(키만 있는) 특성은 그대로. 10-26 용근님 표시가 이미 있으므로 Q-NC5 결정대로 그것을 바꾸거나 둔다 |
| 2.2 | `U/Global/UnitCardContent.cs` | NC-0b에서 CR1 목록이 왔으면 `ShowOwned`가 `Create(visuals, owned.TraitStatuses.Span)`을 쓴다(NC-4를 여기서 함). 안 왔으면 바꾸지 않는다 |
| 2.3 | 정적 확인 | 서식 시험(`T/Framework/UnitCardTraitContentTests.cs`, `T/CoreIntegration/HuntersMarkCardContentTests.cs`·`PullAndMarkPresentationTests.cs`)이 쓰는 상태가 직접 만든 것(사용 가능)이거나 사용 가능한 Core 값인지 본다. 꺼진 특성의 Core 상태로 글 전체를 단언하는 시험이 있으면 깨질 것이므로 최소로 맞추고 메모에 적는다 |

사용자 확인:

1. 컴파일 오류 없음.
2. 전투: 선인장 대포·저격수 카드 특성 이름 뒤 표지와 설명 첫 줄 안내, 실더 카드 설명 끝 일부 안내. 다른 유닛은 그대로.
3. 스테이지 선택·덱 편집: CR1이 없으면 그대로, 있으면 2와 같은 표시.

검수 범위(Codex): `UnitCardTraitContent` 변경과 시험 대조. 기준은 설계 D5(사유 코드를 해석하지 않음, 쓸 수 없는 특성 목록을 View에 두지 않음).

### 4.5 Phase NC-3 공유·인계 (문서만)

| Task | 대상 | 할 일 |
|---|---|---|
| 3.1 | 현강님 | `docs/superpowers/handoffs/2026-10-26-new-core-card-hyeongang-share-Hojin.md`: `UnitCardView` 서명·정보 줄, `UnitCardUI` 조립 이동, 새 파일 둘, `UnitCardTraitContent` 표시, 고친 시험 한 줄(줄 번호) |
| 3.2 | 용근님 | `docs/superpowers/handoffs/2026-10-26-new-core-card-yonggeun-share-Hojin.md`: 덱 편집 세 파일의 변경과 이유(가시 누락), 전투 카드가 같이 바뀌는 것(정보 줄·사용 불가 표시), 전투 호출(`CombatUnitInfo`·`HandPresenter`의 `ShowDetails(CharacterData)`)은 서명이 그대로라는 것 |
| 3.3 | 천명님 | NC-0a 요청 문서에 "View 쪽 진행" 절 추가: 서식이 상태의 사용 가능 사유를 읽음, 고친 시험 한 곳(줄 번호), CR1·CR2 인수 상태 |
| 3.4 | 사용자 | 설계 F6의 문구 점검 목록을 보고에 표로 넘긴다(Q-NC7 A) |
| 3.5 | 설계 | 머리 상태 줄과 §11에 결과를 적는다 |

모든 메모는 미전달로 둔다. 전달은 사용자가 한다. 검수 범위(Codex): 메모의 줄 번호·내용이 실제 변경과 같은지.

### 4.6 Phase NC-4 Core 목록 인수 (CR1을 받은 뒤, NC-2에서 못 했을 때)

| Task | 할 일 |
|---|---|
| 4.1 | 병합 판에서 `OwnedCharacterStatsSnapshot.TraitStatuses` 계약을 대조한다(순서·키·사용 가능 사유) |
| 4.2 | `UnitCardContent.ShowOwned` 한 줄을 목록 경로로 바꾼다 |
| 4.3 | 칸 오버로드 `UnitCardTraitContent.Create(visuals, keys, thorn, mark, pull)`를 쓰는 운영 코드가 없어졌는지 보고, 지울지 사용자에게 묻는다(천명님 API·시험 셋이 씀) |

사용자 확인: 스테이지 선택·덱 편집 카드의 사용 불가 표시, 특성 글이 전투 카드와 같은지.

## 5 공유 메모

아직 쓴 메모가 없다. NC-0a(천명님 요청)와 NC-3(현강님·용근님 공유)에서 쓰고 모두 미전달로 둔다.
