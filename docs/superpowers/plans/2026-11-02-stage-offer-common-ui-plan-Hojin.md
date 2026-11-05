# 이벤트 스테이지·증강 공통 레이아웃 계획

- 작성자: 호진(이호진)
- 설계: [공통 레이아웃 설계](../specs/2026-11-02-stage-offer-common-ui-design-Hojin.md)
- 상태: SO-a~SO-e 완료(11-02). 컴파일·재생 확인 상태는 §3 Phase마다, 커밋 전. 공유 메모는 미전달.
- 정리: 2026-11-02 10-30 회복·상점 UI 표현 다듬기 1차 계획을 §3.1로 합치고, 끝난 Phase의 Task 목록을 §2 요약으로 줄였다. 옛 판은 git `3ad97e734`.
- 규칙: Phase마다 멈추고 보고한다. 커밋은 사용자가 한다. 새 시험 코드는 없다. 컴파일·재생은 사용자가 Unity에서 확인하고 보고에는 "컴파일 미확인"을 적는다. 검토는 묶음 끝에 한 번 한다.

## 1 계획에서 정한 것 (확인 대상)

11-02 사용자 지시로 옛 TP-1(런타임 생성)·TP-2(다음 입력에 꺼짐)·TP-5·TP-6(코드에 남는 글)·TP-8(프리팹 유지)은 없어졌다.

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-3 | 글 SO는 스테이지 키 목록을 갖는다. 회복 1301·1302·1303은 SO 하나를 함께 쓴다 | "스테이지마다"를 지키면서 같은 글을 세 벌 두지 않는다. 나누고 싶으면 SO를 나누면 된다 |
| TP-4 | 글 SO의 안내는 기본 글 + 사유별 글 목록이다. 값은 지금 코드 글을 옮긴다 | 안내 글 하나로는 골드 부족과 덱 가득이 구분되지 않는다 |
| TP-9 | 카드는 `Cards` 아래에 세 장을 미리 놓는다. 넘치면 예외다 | 런타임 생성을 하지 않으면 칸 수가 정해진다. 세 장이 지금 화면들의 최대이고, 훈련소 목록은 그 연결 때 늘린다 |
| TP-10 | 안내는 2초 유지 뒤 0.5초 동안 사라진다. 값은 레이아웃 칸이다 | "몇 초 뒤 천천히"의 시작 값이다. 프리팹에서 바로 고칠 수 있다 |
| TP-11 | 회복 선택지 이름이 View 표에 없으면 예외다(옛 "회복 선택지 {id}" 대체 글을 걷음) | 코드에 글을 두지 않는다. 데이터 누락은 조용히 넘기지 않는다 |

## 2 Phase 요약

| Phase | 내용 | 사용자 작업 | 확인 |
|---|---|---|---|
| SO-a 글 표 | View 표 `StageOfferText`(SO 다섯), 조회 `StageOfferTextTable`, 연결 줄 넷, 옛 UI 생성 도구 둘 삭제 | 에셋 문구 고치기 | 컴파일·진입 확인됨 |
| SO-b 레이아웃·상점·회복 | `StageOfferLayoutView`·`StageOfferScreenUI`·계약 둘·모델, `ShopUI.cs` → `StageOfferUI.cs`(GUID 유지), 옛 계약·모델·글·프리팹 삭제 | StageSelect 씬에 화면 둘(`screenId` Shop/Recovery)과 레이아웃 프리팹 | 확인됨 |
| SO-c 떠나기 | 구매 뒤 떠난다까지 남음(`ShopPopupController` 하나) | — | 확인됨 |
| SO-d 증강 | `TraitGrowthUI`를 공통 화면 하위로, 고르면 바로 다음 제안, 숨김·다시 열기 삭제 | `TraitGrowthUI.prefab` 자식을 레이아웃 프리팹으로 | 증강 나옴(재생 보고 고친 뒤) |
| SO-e 문서·검토 | 공유 메모, 관련 설계 고침, 묶음 끝 검토 | — | 기록은 [훈련소 계획](2026-11-02-stage-training-view-plan-Hojin.md) §3 TU-d |

## 3 실행 기록

### 3.1 앞선 작업 — 회복·상점 UI 표현 다듬기 1차 (10-30)

10-30 사용자 승인. 아트·카드 480×720·자동 가운데 배치·Canvas 구조는 그대로 두고 프리팹 셋의 표현 값만 고쳤다. C#·UI 등록·입력은 무변경, 덱 편집과 결과 확인 기능(설계 §7)은 범위 밖.

- 공통 설명 카드: 이름·가격 폭 180 → 300, 버튼 글자 자동 크기 24~32, 호버·선택·눌림·비활성 색 차이.
- `ShopUI`·`RecoveryUI`: 안내 영역 560×44 → 960×64(y −350), 안내 글자 20~26·골드·종료 글자 24~32 자동 크기. 회복 카드 색 배율 5 → 1.
- 정적 확인(ID·override 중복)만 했다. 실제 화면·해상도·키보드 초점은 미확인이다. 두 프리팹은 SO-b에서 지웠다.

### SO-a 코드·에셋 완료 (11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 새 코드 일곱(+ `.meta`, 폴더 `.meta` 둘): 표 SO, 항목 SO, Data, 열거 셋(`StageOfferTextTarget`·`StageOfferErrorKind`·`StageOfferLineKind`), 조회 `StageOfferTextTable`.
- 새 에셋(+ `.meta`, 폴더 `.meta` 둘): `DataBaseSO/ViewGroup/StageOfferText/` 표 하나 + `Item/` 다섯(Mercenary 1201, Shop 1203, Training 1202, Recovery 1301·1302·1303, TraitGrowth).
- 연결 줄: `ViewTableKey`, `ViewDataBase`(셋), `ViewDataBaseFactory`(둘), `ViewGroup.asset`(한 줄). 민형님 공유는 SO-e.
- 지움: `Editor/Stage/StageShopUISetup.cs`·`StageTraitGrowthUISetup.cs`(+ `.meta`). 코드·씬 참조 없음.
- 아직 아무도 표를 읽지 않아 화면 변화는 없다. 표 생성 검사는 부트의 DB 생성에서 돈다.
- 계획과 다른 점: 카드 글 칸(가격·버튼·꼴)을 함께 넣었다(11-02 지시로 코드에 글을 두지 않음). 옛 글 두 줄은 떠난다 버튼에 맞춰 바꿨다: `PurchaseUsed` "이미 구매했습니다. 떠난다를 눌러 주세요.", `CompletionRetry` "완료하지 못했습니다. 떠난다를 다시 눌러 주세요."
- 커밋: 코드와 `DataBaseSO/ViewGroup/StageOfferText/`·`ViewGroup.asset`은 한 커밋(에셋이 없으면 DB 생성 실패).
- Unity 컴파일·게임 진입 확인됨(11-02 사용자).

### SO-b 코드·프리팹 완료 (11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 씬 작업·Unity 확인 대기)

- 새 코드(+ `.meta`, Unity가 만든 GUID): `IStageOfferInput`, `IStageOfferDisplay`, `StageOfferPopupModel`, `StageOfferLayoutView`, `StageOfferScreenUI`.
- 옮김(GUID 유지): `ShopUI.cs` → `StageOfferUI.cs`(`screenId` 칸), `ShopPopupViewScreen.cs` → `StageMap/StageOfferPopupViewScreen.cs`(두 어댑터를 하나로).
- 지움: `StageOfferPopupUI`, `RecoveryUI`, 옛 계약 넷, `ShopPopupModel`·`RecoveryPopupModel`, 카드 항목 둘, `RecoveryPopupViewScreen`, `ShopPopupText`·`RecoveryPopupText`, 프리팹 `ShopUI`·`RecoveryUI`(+ 빈 폴더 둘).
- 컨트롤러 둘: 입력은 `IStageOfferInput`, 카드 번호 → ID는 모델 빌더가 기억(`OfferIds`·`OptionIds`), 안내는 `ShowError` + 글 표. 사유 변환은 `ShopResultPolicy.ErrorOf`·`RecoveryPopupModelBuilder.ErrorOf`.
- 연결 줄: `StageMapViewServices`(`OfferTexts`), `StageMapViewSystem`, `StageMapSceneFactory`, Factory 둘. 구성 검사: `StageScreenRootUI`(칸 형식·`screenId`), `StageSelectPanelUI`.
- 프리팹: 위 "프리팹" 줄대로. 카드 간격은 60, 카드 크기는 카드 프리팹 값(480×720)이고 `Cards` 배율 0.8은 그대로라 세 장이 `Cards` 칸(1000×550)을 넘는다. 배치는 사용자가 맞춘다.
- 계획과 다른 점: 클릭 막기 Image를 Claude가 프리팹에 넣음(사용자 작업에서 뺌). 상점 모델 빌더는 현재 방문이 없으면 빈 모델 대신 예외(부르는 쪽이 문맥을 확인한 뒤에만 부름).
- 없어진 화면 동작: "구매 완료 · '선택하지 않는다'로 완료" 안내, 나가기 버튼 글 바꾸기("떠난다" 고정), 회복 선택지 대체 이름(TP-11). 구매 성공 뒤 닫히는 것은 SO-c에서 바꾼다.
- 커밋: 코드, `StageOfferUI.prefab`, 옛 프리팹 삭제, `StageSelect.unity` 사용자 작업은 한 커밋(없으면 StageSelect 진입이 멈춤).
- Unity 확인·씬 작업 확인됨(11-02 사용자 "확인, SO-c 진행").

### SO-c 코드 완료 (11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- 바꾼 파일 하나: `Shop/ShopPopupController.cs`. 다른 파일·에셋·씬 무변경.
- 상태 `_awaitingExit` 하나: 구매와 완료가 **모두 수락**됐을 때만 참이다. 완료가 실패한 구매는 이 상태가 아니고, 지금 흐름 그대로 떠난다가 `Complete`를 다시 낸다(설계 SO7의 "결과 확인 중"을 "떠난다만 남음"으로 좁혀 둘을 따로 세지 않음).
- `HandleResult`: 구매 성공은 닫지 않고 다시 그린다(고른 카드 "구매 완료"·모든 카드 잠김·새 골드). 닫힘은 `Complete` 수락과 상태 불일치뿐이다.
- `CanSubmitNow`: 이 상태에서는 노드가 완료돼도 허용(떠난다만 누를 수 있고, 카드는 내용이 이미 꺼져 있어 레이아웃이 막는다).
- 나가기: 이 상태면 명령 없이 `CloseAndRefresh(true)`, 아니면 `Complete` 제출.
- `ReconcileWithoutRequest`: 이 상태이고 화면이 열려 있으면 닫지 않는다. 화면이 밖에서 닫혔으면 지금처럼 맵을 새로 그린다.
- `Show`: 이 상태로 다시 열리면 그린 것을 유지하고 허용만 다시 내린다(계획에 없던 줄. 없으면 완료된 노드라 잠겨 떠날 수 없다).
- Core·Buffer 확인: 완료(`PublishStageClear`)는 노드 완료만 기록하고 선택·상점 상태를 지우지 않아 완료 뒤 `Build`가 된다.
- 회복은 이미 이 흐름이라 무변경.
- 커밋: SO-b 묶음 뒤 또는 함께. 이 파일은 SO-b에서도 바뀌었다.
- Unity 확인 문제 없음으로 받음(11-02 사용자 "확인, SO-d 진행").

### SO-d 코드 완료 (11-02, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 사용자 프리팹 작업·Unity 확인 대기)

- 화면: `TraitGrowthUI`를 `StageOfferScreenUI` 하위로 다시 씀(GUID 유지, ID·Parallel·초점 설정만 남음). 이제 등록·개폐·입력·그리기는 공통 부품이 맡는다.
- 컨트롤러: 입력은 `IStageOfferInput`, 화면은 `IStageOfferDisplay`. 결과가 오면 `Refresh`가 바로 확인하고 다음 제안을 그린다. 실패면 `ShowError` 뒤 지금 제안을 다시 그리고, 성공 안내는 없다. 결과 화면·"계속"·숨김·다시 열기·고른 제안 보관을 걷었다.
- 모델: `TraitGrowthPopupModelBuilder`가 `StageOfferPopupModel`을 채운다. 글은 SO `StageOfferText_TraitGrowth`에서 읽고, 설명 글 아래 줄에 `TraitGrowthHeader`를 붙인다. 사유 변환 함수는 `ErrorOf` 하나다. 골드 글은 비우고 `ShowExit`은 거짓이라 `Under_Line`·`GoldBackground`·`ExitButton`이 꺼진다.
- 호스트: `StageMapViewScope.BlocksEntryForGrowth`는 진입만 막는다(다시 열기 호출 삭제). 읽기 실패는 전처럼 오류 경계에 보고하고 막는다.
- 연결 줄: `TraitGrowthPopupViewFactory` 끝 인자 `texts`, `StageMapSceneFactory`의 `services.OfferTexts`(민형님 공유는 SO-e).
- 지움: `ITraitGrowthPopupDisplay`, `ITraitGrowthPopupInput`, `ITraitGrowthReopenInput`, `TraitGrowthPopupModel`, `TraitGrowthPopupText`, `TraitGrowthPopupViewScreen`(+ `.meta`).
- 계획과 다른 점 셋:
  - `TraitGrowthPopupViewScreen`도 지웠다. 옛 계약을 받던 어댑터라 SO-b의 `StageOfferPopupViewScreen`으로 대신한다.
  - 첫 카드 초점을 남겼다(`StageOfferScreenUI.FocusFirstCardOnRender`, `StageOfferLayoutView.FocusFirstCard`). 나가기가 없는 화면이라 옛 동작(Core 인계 §7)을 잃지 않게 했고, 증강만 켠다. 상점·회복 동작은 그대로다.
  - 결과 확인이 거절(`AcknowledgeResult` 거짓)되면 예외로 오류 경계에 보고한다. 옛 화면은 "계속"으로 다시 시도할 수 있었지만, 지금은 그대로 두면 고를 수 없는 화면에 갇힌다.
- 빈 입구: `IStageOfferInput.Exit`는 증강에서 빈 구현이다. 레이아웃이 나가기가 꺼진 화면에서 부르지 않는다.
- 카드 수: 프리팹 카드는 세 장이고 넘으면 예외다(TP-9). 시험 코드 기준 제안은 세 장까지로 보이나 운영 값은 재생으로 확인한다.
- **사용자 프리팹 작업(코드와 같은 커밋):** `TraitGrowthUI.prefab`에서 루트(`TraitGrowthUI`)만 남기고 자식을 지운 뒤, 자식으로 `StageOfferUI.prefab`을 놓아 `layout` 칸에 잇는다. 그 전에는 StageSelect 진입이 `layout` 참조 예외로 멈춘다.
- 사용자 확인: 이름·설명·대상 유닛 줄, 세 칸 꺼짐, 고르면 바로 다음 제안 또는 닫힘, 노드를 눌러도 진입 막힘, 실패 안내, 설정 창과 겹침.

### SO-d 재생 보고: 증강 화면이 안 보임 (11-02, 코드 수정, 컴파일 미확인, 재생 미확인)

- 보고: 스테이지 클리어로 유닛이 레벨업했는데 증강 화면이 나오지 않음(사용자). 콘솔 예외 없음(Editor.log).
- 원인(코드·씬 대조): `StageSelect.unity`가 `TraitGrowthUI` 인스턴스 루트에 `m_IsActive: 0`을 저장해 뒀다. `StageOfferScreenUI.Open`은 레이아웃만 켜고 루트를 켜지 않아, 논리로는 열려도(`IsOpen` 참, 입력 Layer도 팝업) 화면에 나오지 않고 예외도 없다. 옛 `TraitGrowthUI`도 루트를 켜지 않았다.
- 고침: `StageOfferScreenUI.Open`이 `UIBase.Open`처럼 루트를 켜고 레이아웃 준비를 확인한다. 닫을 때 루트는 그대로 둔다. 씬은 고치지 않았다.
- 재생으로 증강이 나옴(11-02 사용자).

### SO-d 추가: 증강 프리팹을 `TraitUI`로 (11-02 사용자 "TraitUI는 증강만, 나머지는 StageOfferUI", 컴파일 미확인, Unity 확인 대기)

- `TraitGrowthUI.prefab`의 자식을 `StageOfferUI`에서 `TraitUI`로 바꿨다. 두 프리팹의 안쪽 fileID가 같아 원본 GUID와 이름만 바꿨고 `layout` 연결은 그대로다. 씬은 무변경이다.
- `TraitUI.prefab`의 레이아웃 카드 칸 둘이 비어 있어(진입 때 참조 예외가 날 자리) `OfferCard1`·`OfferCard2`로 이었다.
- 코드 동작 무변경(주석 둘만). 화면의 이름·설명은 프리팹 글이 아니라 SO `StageOfferText_TraitGrowth`에서 온다.

### 카드 `TraitNew`·가격 숨김 (11-02 사용자, 컴파일 미확인, 새 시험 없음, Unity 확인 대기)

- 요청: `ItemInfo`의 `TraitNew`에 신규·강화 특성 구분을 보이고, 증강에서는 돈이 필요 없으니 `ItemIcon`·`Price`를 끈다.
- `StageOfferCardContent`에 `KindText`(끝 선택 인자, 상점·회복 호출은 그대로). 값이 빈 칸은 끄는 규칙으로, 가격이 비면 `Price`·`ItemIcon`이, 종류 글이 비면 `TraitNew`가 꺼진다. 증강 여부를 카드가 묻지 않는다(R2 골드 칸과 같은 꼴).
- `StageOfferCardView` 필수 칸 `kindText`·`priceIcon`. `StageEventOfferCard.prefab`에 이어서 변형·인스턴스가 물려받는다.
- 증강 모델은 가격을 비우고 `KindText`에 SO 낱말을 넣는다. SO `TraitAcquireLabel`을 프리팹 글에 맞춰 "신규 특성"으로 바꿨다(대상 유닛 줄에도 같은 글이 나온다).
- 커밋: 코드와 카드 프리팹은 한 커밋(프리팹이 없으면 필수 칸 예외로 StageSelect 진입이 멈춘다).

### 카드 너비 480 (11-02 사용자, Unity 확인 대기)

- `StageOfferUI`의 `OfferCard0` 크기 덮어쓰기를 지우고 `StageEventOfferDescriptionCard` 너비를 489 → 480으로 맞췄다. `TraitUI` 카드의 560은 그대로다.

### SO-e 문서 (11-02, 훈련소 TU-d와 합침)

- 기록은 [훈련소 계획](2026-11-02-stage-training-view-plan-Hojin.md) §3 TU-d.
