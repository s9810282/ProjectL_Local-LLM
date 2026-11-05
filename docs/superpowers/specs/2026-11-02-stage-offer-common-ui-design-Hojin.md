# 이벤트 스테이지·증강 공통 레이아웃 설계

- 작성자: 호진(이호진)
- 상태: 4판. SO-a~SO-e 구현 완료(11-02), 재생 확인은 계획 §3. Task 계획: [공통 레이아웃 계획](../plans/2026-11-02-stage-offer-common-ui-plan-Hojin.md).
- 정리: 2026-11-02 10-30 회복·상점 선택 결과 확인 화면 설계(구현 전에 이 설계가 대신함)를 §7로 합쳤다. 옛 판은 git `3ad97e734`.
- 요청(11-02 사용자): `StageOfferUI.prefab`을 이벤트 스테이지 공통으로 쓰고, 증강에서도 레이아웃을 재활용한다. 글은 코드가 아니라 SO에서 넣는다.
- 바뀐 것: 1판은 증강을 뺐고 프리팹을 상점 프리팹에서 만들려 했다. 사용자가 프리팹을 직접 만들고 증강을 넣었다. 2판의 안내 자리·증강 숨김·문구 위치는 §4 결정으로 바뀌었다.
- 기준 코드: HEAD `306cf1287` + 작업 트리.

## 1 사용자 요구

| 번호 | 요구 |
|---|---|
| R1 | `EventNameText`·`EventDescText`는 스테이지마다 글이 다르다(용병: "용병 고용소" / "함께 싸울 용병을 고용합니다") |
| R2 | `GoldText`는 보유 골드만 보여 준다. 골드를 쓰지 않는 이벤트에서는 나오지 않는다 |
| R3 | 무엇을 골랐든 `ExitButton`을 누르기 전에는 떠나지 않는다 |
| R4 | 증강도 같은 레이아웃 부품(`StageOfferLayoutView`)을 쓰되 프리팹은 `TraitUI.prefab`으로 따로 둔다(11-02 사용자 "TraitUI는 증강만, 나머지는 StageOfferUI"). 반드시 골라야 하고 골드를 쓰지 않아 `Under_Line`·`GoldBackground`·`ExitButton`을 끈다. 이름·설명 글은 똑같이 바뀐다 |
| R5 | 안내는 `ErrorPanel`을 켜고 `ErrorText`에 글을 넣는다. 처음에는 꺼져 있고, 몇 초 뒤 천천히 사라진다 |
| R6 | 화면에 나오는 글은 코드에 두지 않고 스테이지마다 SO에서 넣는다 |
| R7 | 레이아웃을 런타임에 만들지 않는다. 씬 로딩 때 준비돼 있어야 한다 |

## 2 확인 사실

**프리팹(`Prefabs/UI/StageOfferUI.prefab`, 작업 트리)**

```text
StageOfferUI (1920×1080, 스크립트 없음)
├─ Panel (Image)
│  ├─ EventNameText
│  │  └─ EventDescText
│  ├─ Left_Line, Right_Line, Under_Line
│  ├─ GoldBackground → GoldIcon → GoldText
│  └─ ExitButton (Button) → Label "떠난다"
├─ Cards
└─ ErrorPanel (Image, 처음 꺼짐, 420×230, 가운데) → ErrorText
```

- 스크립트가 아직 없고, 씬·다른 프리팹이 참조하지 않는다.
- `Cards`가 `Panel` 밖이다. 지금 코드는 "카드 영역과 나가기 버튼은 Panel 안"을 검사하고 Panel만 켜고 끈다.
- `ErrorPanel`·`ErrorText`의 Raycast Target이 켜져 있다. 켜지면 가운데 카드 클릭을 막는다.
- 전면 클릭 막기가 없다. 맵 입력은 입력 Layer가 막지만 뒤쪽 UI 버튼은 눌릴 수 있다.
- 작업 트리에서 `.meta` GUID가 `a88e9c5d…` → `699c5061…`로 바뀌어 있다. 참조가 없어 문제는 없다.

**떠나기(R3)**

| 화면 | 지금 |
|---|---|
| 회복 | 고르면 남아 있고, 나가기로 `Complete`를 낸 뒤 닫힌다. 요구와 맞다 |
| 용병·훈련 | Core `ShopCommandExecutor`가 구매 성공 뒤 바로 완료까지 처리한다. 컨트롤러는 그 결과를 보고 바로 닫는다(`HandleResult`의 `close`). `ReconcileWithoutRequest`도 노드가 클리어되면 닫는다 |
| 증강 | 고르면 결과를 보여 주고 `ContinueButton`을 누르면 확인한다. 숨김·다시 열기 버튼이 있다 |

**스테이지 키**: 용병 1201, 아이템 상점 1203, 강화·훈련 1202(`StageRootDataTable` 미등록), 회복 1301·1302·1303. 컨트롤러는 키를 Buffer에서 읽는다(`_current.CurrentStage.StageKey`, `RecoveryEntryContext.StageKey`). 증강은 스테이지가 아니다.

**실패 사유**: 상점 `ShopFailureReason`, 회복 `RecoveryFailureReason`, 증강 `TraitGrowthFailureReason`이 각각 있고, 지금 글은 `ShopPopupText.Of`·`RecoveryPopupText.Of`·`TraitGrowthPopupText.Of`에 있다. 회복 카드 설명도 `RecoveryPopupText.Of`를 쓴다.

## 3 설계

**SO1 레이아웃 부품을 프리팹 루트에 하나 둔다.** 새 `StageOfferLayoutView`(MonoBehaviour)를 `StageOfferUI.prefab` 루트에 붙인다. 등록 화면 안의 부품이라 `UIBase`가 아니다(`Assets/AGENTS.md`의 "등록된 화면 내부의 세부 위젯"). 하는 일은 그리기와 입력 전달뿐이다.

- 켜고 끄기: 레이아웃 루트 전체(`Panel`·`Cards`가 함께). 프리팹 구조는 그대로 둔다.
- 그리기: 이름·설명·골드·카드, 칸 끄기, 안내.
- 입력: `ExitButton`·카드의 `UIInputBinding` 수신자가 되어 `IStageOfferInput`으로 넘긴다.
- 필수 참조는 `Validate()` 한 곳에서 예외로 검사하고, 구성 검사가 연결 전에 부른다.
- 카드는 `StageEventOfferDescriptionCard`(11-02 사용자)를 `Cards` 아래에 미리 놓고 칸 목록으로 받는다(SO4).

**SO2 표시 값은 모델 하나다.** `StageOfferPopupModel`: `EventName`·`EventDescription`·`GoldText`·`ShowExit`·`CanExit`·카드 목록(`StageOfferCardContent`).

| 칸 | 켜는 조건 |
|---|---|
| `GoldBackground` | `GoldText`가 비어 있지 않음(R2) |
| `ExitButton` | `ShowExit`(R3·R4) |
| `Under_Line` | 골드나 나가기 중 하나라도 있음 |

증강은 `GoldText`를 비우고 `ShowExit`을 끄므로 세 칸이 모두 꺼진다(R4). 나중에 골드를 안 쓰는 이벤트는 `GoldText`만 비우면 골드만 꺼지고 나가기와 줄은 남는다.

**유닛 얼굴(11-02 사용자 "UnitFace에 증강을 고르는 유닛의 얼굴").** 모델 칸 `Portrait`(Sprite)와 레이아웃 선택 칸 `unitFace`(Image)를 더했다. `Reset`이 얼굴을 비우고, 얼굴을 쓰는 화면만 `SetPortrait`로 채운다. 얼굴이 있으면 넣고 켜며, 없으면 끈다. 칸이 비어 있는데 얼굴을 넘기면 대상을 적은 예외다. 지금 채우는 화면은 증강 하나다.

- 그림 원본: 유닛 외형 표 `InfoIcon`(`Sprites/AlphaIcon/UI_head_*`). 덱 편집·유닛 카드·전투 결과와 같은 칸이라 새 연결이 없다.
- 증강 모델 빌더가 제안의 보유 유닛 → 유닛 외형을 한 번 찾아 "누가 몇 레벨" 줄과 얼굴에 함께 쓴다. 다음 제안을 준비하는 동안, 유닛이나 외형 행을 못 찾을 때는 얼굴이 꺼진다.
- 연결: `TraitUI`의 `Panel/UnitFace` 하나. `StageOfferUI`는 비워 둔다.
- 메인 캐릭터 외형은 지금 Akimbo 얼굴을 쓴다(`AlphaIcon`에 메인 전용 얼굴이 없음, 데이터 그대로).

**SO3 입력은 카드 번호와 나가기 둘이다.** `IStageOfferInput { SelectCard(int index); Exit(); }`. 레이아웃은 Core 형식을 모른다. "누른 것은 그린 것이다" 규칙은 컨트롤러가 그릴 때 문맥과 카드별 ID를 기억해서 지킨다. 그리는 자리는 함수 하나로 모은다(상점 셋, 회복 하나, 증강 하나).

**SO4 등록 화면은 스크립트이고, 레이아웃은 씬에 미리 놓는다(Q-SO4, 11-02 사용자 "런타임에 생성하지 마세요").** 씬이 로딩될 때 화면이 준비돼 있어야 한다. 화면 스크립트는 빈 오브젝트에 붙고, 그 자식으로 놓인 레이아웃 프리팹을 칸 `layout`으로 잇는다.

| 등록 화면 | UIId | 부모·정책 | 놓는 곳 |
|---|---|---|---|
| `StageOfferUI`(옛 `ShopUI` GUID): 상점 길 | `Shop`(`screenId`) | `StageScreenRoot`·Exclusive | 씬 빈 오브젝트 + 자식 레이아웃 프리팹 |
| `StageOfferUI`: 회복 길 | `Recovery`(`screenId`) | 같음 | 같음 |
| `TraitGrowthUI` | `TraitGrowth` | 없음·Parallel | 자기 Canvas 프리팹 + 자식 레이아웃 프리팹 |

카드도 런타임에 만들지 않는다. `StageOfferUI.prefab`의 `Cards` 아래에 카드 프리팹을 미리 놓고 칸 목록 `cards`로 잇는다. 쓰지 않는 카드는 숨긴다. 놓인 수보다 많은 카드를 그리려 하면 대상과 수를 적은 예외다. 지금 세 장을 놓고, 훈련 목록처럼 더 필요한 화면은 그 작업 때 프리팹에 더한다.

공통 기반 `StageOfferScreenUI`(추상, `UIBase`)가 열림 상태·알림·잠금·안내를 맡고 `IStageOfferDisplay`를 구현한다. 하위 둘은 ID·부모·정책만 답한다. 컨트롤러 둘이 한 화면을 나누면 입력 연결·잠금·열림 인수가 서로 부딪쳐서 상점 길과 회복 길은 인스턴스를 나눈다. 프리팹은 하나다.

**SO5 글은 새 View 표에서 읽는다(R1·R6, Q-SO5).** 새 표 `StageOfferText`, 항목 SO `StageOfferTextDataSO`. 11-03부터 스테이지 맵 건물 호버의 제목·설명도 이 표의 `eventName`·`eventDescription`을 쓴다(조회 `GetNodeText` = 키 줄 → 종류·역할 공통 `Info` 줄). 팝업 조회 `GetForStage`는 `Stage` 줄만 돌려준다. `Info` 줄은 이름·설명만 필수다.

| 칸 | 뜻 |
|---|---|
| `target` | `Stage` / `TraitGrowth` / `Info`(11-03 추가 — 화면 없이 건물 호버의 이름·설명만, [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md) §5.7 3판) |
| `stageKeys` | 이 글을 쓰는 스테이지 키 목록. 회복 셋은 SO 하나를 함께 쓴다. `TraitGrowth`면 비움. `Info`에서 비우면 아래 종류·역할의 공통 줄 |
| `stageType`·`mapRole` | (11-03 추가) `Info`이고 `stageKeys`가 빌 때만 쓰는 공통 줄의 종류·역할. 종류·역할마다 하나 |
| `eventName` | `EventNameText` 글 |
| `eventDescription` | `EventDescText` 글 |
| `goldFormat` | 보유 골드 꼴(`보유 골드 : {0}G`). **비우면 골드 칸이 꺼진다(R2)** |
| `priceFormat` | 카드 가격 꼴(`{0}G`) |
| `selectButtonText`·`completedButtonText`·`unavailableButtonText` | 카드 버튼 글 |
| `lines` | 카드 설명·머리줄 꼴 목록 `(StageOfferLineKind, format)` |
| `defaultErrorText` | 사유별 글이 없을 때 `ErrorText` 글(필수) |
| `errors` | 사유별 `ErrorText` 글 목록 `(StageOfferErrorKind, text)`(선택) |

**화면에 나오는 글은 코드에 두지 않는다(11-02 사용자).** `ShopPopupText`·`RecoveryPopupText`·`TraitGrowthPopupText`를 지운다. 값이 끼는 글은 SO의 꼴(`{0}`)로 넣고 코드는 값만 넘긴다. 예외 문구와 Debug 로그는 화면 글이 아니라 코드에 남는다.

안내 글이 하나면 "골드 부족"과 "덱 가득"이 같은 글이 되어서 사유별 칸을 둔다. 사유 값은 Core 사유 셋을 묶은 View 열거 `StageOfferErrorKind`이고, Core 사유 → 사유 값 변환은 컨트롤러 쪽 함수 셋이다. 키 겹침·증강 항목 수·빈 필수 글·잘못된 꼴은 표를 만들 때 예외로 막는다. 등록되지 않은 스테이지 키와 SO에 없는 꼴은 쓰는 때 대상을 적은 예외다.

처음 넣을 값(지금 코드 글을 그대로 옮기고, 이름·설명 초안은 사용자가 고친다):

| SO | 키 | 이름 | 설명 |
|---|---|---|---|
| 용병 | 1201 | 용병 고용소 | 함께 싸울 용병을 고용합니다 |
| 상점 | 1203 | 상점(초안) | 필요한 물건을 구입합니다(초안) |
| 훈련소 | 1202 | 훈련소(초안) | 특성을 강화합니다(초안) |
| 회복 | 1301·1302·1303 | 회복(초안) | 메인 캐릭터를 회복합니다(초안) |
| 증강 | — | 증강 선택(초안) | 특성을 하나 선택합니다(초안) |

증강은 제안마다 다른 값(`샷건 Lv.2`, `새 특성`)이 있다. SO의 꼴 `TraitGrowthHeader`(`{0} Lv.{1} · {2}`)와 낱말 `TraitAcquireLabel`·`TraitEnhanceLabel`로 만들어 설명 글 아래 줄에 붙인다.

**SO6 안내는 `ErrorPanel`이고 몇 초 뒤 천천히 사라진다(R5, Q-SO3, 11-02 사용자).** `ShowStatus`를 없애고 `ShowError(string)`로 바꾼다. 켜면 `errorHoldSeconds`(2초) 뒤 `errorFadeSeconds`(0.5초) 동안 사라지고 꺼진다. 값은 레이아웃 칸이다. 새 안내가 오면 처음부터 다시 세고, 화면을 닫으면 바로 끈다. `ErrorPanel`에 `CanvasGroup`을 더해 알파를 줄이고 `blocksRaycasts`를 꺼서 켜진 동안에도 가운데 카드를 누를 수 있다. 시간은 켜진 동안만 레이아웃 `Update`에서 잰다(꺼지면 `enabled = false`). 옛 안내 중 "구매 완료 · '선택하지 않는다'로 완료"는 없어진다. 카드 버튼이 "구매 완료"를 보여 주고 떠난다 버튼이 켜져 있다.

**SO7 고른 뒤에는 나가기까지 남는다(R3).** 상점 컨트롤러에 "결과 확인 중" 상태를 둔다.

1. 구매가 성공하면 결과에 완료가 함께 와도 닫지 않는다.
2. 그때 표시 값을 다시 만들어 그린다. 고른 카드는 "구매 완료", 모든 카드는 잠기고 골드는 새 값이다. 완료 뒤 Buffer가 바뀔 수 있어 이후 다시 그리지 않고 그린 것을 유지한다.
3. `ReconcileWithoutRequest`는 이 상태에서 닫지 않는다.
4. 나가기: 완료가 이미 수락됐으면 명령 없이 닫는다. 완료가 실패했으면 지금처럼 `Complete`를 다시 내고 수락되면 닫는다. 어느 쪽이든 그 뒤 맵을 새로 그린다.

그대로 두는 것: 고르지 않고 나가기는 `Complete` 뒤 닫힘. 골드 부족 등 정상 거절은 팝업에 남음. 상태 불일치(낡은 목록·다른 방문)는 **R3의 예외로 지금처럼 닫는다.** 남겨 두면 누를 수 없는 화면에 갇힌다. 회복은 이미 이 흐름이다.

10-30 결과 확인 화면 설계(§7)와 겹치는 부분은 이 절이 대신한다. 다른 점: 골드는 숨기지 않고(R2) 가운데 별도 나가기 버튼을 두지 않으며(같은 `ExitButton`), 고르지 않은 카드의 X 표시는 이번 범위가 아니다.

**SO8 증강은 고르면 바로 넘어간다(Q-SO1·Q-SO2).**

- 결과가 오면 View가 바로 확인(`AcknowledgeResult`)하고, 다음 제안을 그리거나 남은 성장이 없으면 닫는다. 성공 안내는 없다.
- 실패면 확인한 뒤 `ErrorPanel`에 안내하고 지금 제안을 다시 그린다. 나오면 안 되는 결과는 지금처럼 오류 경계에 보고한다.
- "계속"·숨김·다시 열기 버튼과 코드를 걷는다(`ITraitGrowthReopenInput`, `_hidden`, `RequestReopen`). 노드를 누르면 증강이 남아 있는 동안 진입만 막는다.
- 결과로, 증강이 남아 있고 다른 팝업이 없으면 화면이 열린 채 고를 때까지 닫히지 않는다. 그동안 맵을 움직이거나 덱을 편집할 수 없다. 의도한 것이고 나중에 바꿀 수 있다(11-02 사용자).
- 다음 제안을 준비하는 짧은 동안은 카드 없이 이름·설명만 보인다. "다음 선택 준비 중" 글은 걷는다.
- 설계 10-28 Q-TG7 A·Q-TG12 C를 이 절이 대신한다.

**카드 칸 규칙(11-02 사용자).** 값이 빈 칸은 끈다. 증강은 `TraitNew`만 보이고 `ItemIcon`·`Price`는 꺼진다. 용병·상점·회복은 `ItemIcon`·`Price`만 보인다. **훈련은 `ItemIcon`·`Price`·`TraitNew`가 모두 보인다.** 훈련 카드 자체는 아직 연결 전이라(SO9) 연결할 때 이 규칙으로 값을 채운다.

**SO9 범위 밖.** Core·Buffer, 시안의 병과·능력치·특성 칸(Core 요청이 필요해 다음 작업), 덱 편집 화면. 카드가 넘치는 화면은 훈련소 유닛 단계뿐이고 `StageOfferUI`의 카드 7장·스크롤로 받는다([훈련소 설계](2026-11-02-stage-training-view-design-Hojin.md) TV7). R7은 그대로다.

## 4 결정 (11-02 사용자)

| 번호 | 결정 |
|---|---|
| Q-SO1 | 결과가 오면 바로 확인하고 다음 제안을 그리거나 닫는다 |
| Q-SO2 | 숨김·다시 열기는 없앤다. 필요하면 사용자가 다시 만든다 |
| Q-SO3 | 안내는 `ErrorPanel`·`ErrorText`. 필요할 때 켜고 처음에는 꺼짐. 몇 초 뒤 천천히 사라짐 |
| Q-SO4 | 코드와 프리팹 스크립트 연결은 Claude, 씬에는 스크립트. 절대 런타임에 생성하지 않고 씬 로딩 때 준비돼 있어야 함 |
| Q-SO5 | 글은 임시로 쓰되 코드에 쓰지 않고 스테이지마다 SO에서 넣는다 |
| 삭제 | `ShopUI.prefab`·`RecoveryUI.prefab`·에디터 도구 `StageShopUISetup`·`StageTraitGrowthUISetup`을 지운다 |

11-02 `819293c35`에 재령님이 `ShopUI.prefab`을 크게 고쳤다. 지우기 전 사실을 공유 메모에 적는다.

## 5 원칙 점검

자체 점검이고 측정값이 아니다.

| 기준 | 점검 |
|---|---|
| SSOT | 레이아웃·칸 끄기·카드 배치가 부품 하나로 모인다. 증강과의 두 벌이 없어진다. 글의 원본은 SO이고 코드 사본을 걷는다. 카드별 ID는 그린 컨트롤러 한 곳이다 |
| SOLID | 레이아웃은 그리기와 입력 전달, 등록과 수명은 `UIBase` 화면, 판단은 컨트롤러가 맡는다. 새 이벤트는 SO 하나와 모델 채우기로 끝나고 레이아웃을 고치지 않는다 |
| JMH/FW·UI 규약 | 새 서비스는 View 표 조회 하나(지금 표들과 같은 길)이고 모듈·Tick이 없다. 개폐는 `IUISystemService`, 입력은 `UIInputBinding`·명시적 수신 계약이다. 누락 참조·없는 키는 예외다. View DB는 InGame을 읽지 않는다(P5·P6) |
| 기능 독립성 | 글은 SO만, 칸 모양은 프리팹만, 상점 결과 흐름은 상점 컨트롤러만 본다. 남는 경계는 모델·입력 계약과 카드 원본이다 |

남는 점: 씬에 같은 스크립트가 둘이고, 카드 원본을 고치면 모든 화면을 확인해야 한다. 새 스테이지를 쓰려면 글 SO도 함께 등록해야 한다(없으면 열 때 예외).

## 6 겹치는 작업

- **훈련소 내부 연결(강화 설계 §16 → [훈련소 설계](2026-11-02-stage-training-view-design-Hojin.md)로 구현됨):** `ShopUI`·`IShopPopupInput`·`ShopOfferCardEntry`를 고치려던 계획이다. 이 작업 뒤 새 계약 위에서 하면 TV3이 컨트롤러 안 기억으로 바뀌고 입력 계약 추가가 필요 없다.
- **UI 작업자:** 재령님(`819293c35`)·현강님(`96ea26acc`)이 같은 프리팹·카드 원본을 고쳤다. 지금도 만지는지 착수 전에 확인이 필요하다.

## 7 앞선 안 — 회복·상점 선택 결과 확인 화면 (10-30, 대신함)

10-30 사용자 요구는 "회복·아이템 상점·강화 상점에서 고른 뒤 화면이 닫히지 않고, 결과를 읽은 뒤 나간다"였다("이벤트"의 범위는 회복으로 확인). 설계만 쓰고 구현하지 않았고, 떠나기 동작은 SO7로 구현했다.

- 그때 안: 기존 팝업에 결과 확인 상태를 더한다(별도 결과 팝업은 고르지 않은 카드를 보이려는 요구와 안 맞음). 성공 표시는 요청 번호·방문 문맥과 대조한 결과로만, 회복은 나가기에서 기존 완료 요청, 상점은 구매와 완료가 함께 와도 표시를 유지한다.
- 하지 않은 것: 고르지 않은 카드 위 X 표시, 보유 골드 영역 숨김, 가운데 별도 나가기 버튼. SO7은 같은 `ExitButton`을 쓰고 골드는 R2대로 보인다. X 표시는 다시 원하면 카드 원본에 "선택 불가" 칸을 명시적으로 넘긴다(`IsCompleted`의 반대값으로 추측하지 않는다).
