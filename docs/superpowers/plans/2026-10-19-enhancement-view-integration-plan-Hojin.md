# 강화 View 통합 계획

- 작성자: 호진(이호진)
- 상태: Phase A·B·C·D·E0와 기존 상점 외형 3종 등록 완료(2026-10-20). 실제 Editor Play로 3종 각각 구매 → 다음 전투 반영 확인(10-20). H(마지막 구매 읽기)와 E1~E4 전체 인수는 보류다. 레벨업 증강 선택 화면(옛 §5)은 2026-10-29 [증강 계획](2026-10-28-trait-growth-plan-Hojin.md)으로 옮겼다.
- 설계: [1레벨 강화 UI와 View 연결 설계](../specs/2026-10-19-enhancement-view-integration-design-Hojin.md)
- 정리: 2026-10-27 Phase별 Task 단계·파일 지도·검증 명령과 실행 기록(옛 §5~§19)을 결과 요약으로 줄였다. 끝난 Task 단계, 시험 통과 수, `.superpowers/sdd/` 증거 경로, 보호 해시 수는 git `10914570`. 2026-10-29 증강 계획(옛 §5)을 떼어 냈다(옛 판 git `a8612321e`).

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| A 실제 후보 표시·공통 설명 카드 | 상품 종류별 내용 Builder, 강화 후보 표시, 설명 카드 Variant를 상점·회복에 연결 | 완료 | 당시 EditMode, 실제 Canvas 렌더·EventSystem hit. 리뷰 수정 요구 없음 | `3b875440` |
| B 공통 안내·강화 실패 분류 | 완료 대기·정상 거절 안내를 Status로, 강화 사유 넷 분류 | 완료 | 당시 EditMode. 리뷰 수정 요구 없음 | `3b875440` |
| C StageKey 지도 외형 조회 | 지정 행 우선·공통 대체 Query | 완료 | 당시 EditMode. 리뷰 수정 요구 없음 | `89cbc44e` |
| D 레시피 준비 | 레시피 SO 5개, 표 4행(연쇄 폭발 보류) | 완료. 뒤에 표가 바뀜(§3.5) | 당시 에셋 교차검증. 리뷰 수정 요구 없음 | `89cbc44e` |
| E0 강화 스테이지 준비 | 상세·루트 SO 2개(미등록) | 완료 | 당시 에셋 교차검증 | `89cbc44e` |
| E §17 1202 등록 | 루트를 `StageRootDataTable`에 추가, 공통 상점 외형 사용 | 완료. 10-29 등록을 다시 뺌(설계 §14) | 당시 EditMode·실제 DB 지도 생성·구매 | `89cbc44e` |
| E 실제 Play | 독 가시, 2연발, 도어 브리칭을 각각 새 Run에서 구매 → 다음 전투 | 완료 | 실제 Editor Play(자동 화면 조작), 승인된 메모리 재화 1000G | 문서만 |
| 강화 노드 전용 View 행 | `NodeVisual_Shop_Enhancement`(StageKey 1202) | 행만 등록, 아트는 일반 상점과 같음 | 문서 기록 없음 | `34ee0de4` |
| H 마지막 구매 읽기 | 이력 카드·이력 없음 안내 | 보류(Core 대기) | — | — |
| E1~E4 운영 인수 | 전용 외형·운영 등록·전 과정 Play | 보류 | — | — |
| TG 증강 선택 화면 | 별도 주제로 옮김 → [증강 계획](2026-10-28-trait-growth-plan-Hojin.md) | — | — | `24c21b3a3` |

## 2 남은 일

- 먼저 알 것: 천명님 `d48542034`(10-28)로 Core의 서브 강화 판매가 걷혔고 강화 노드(1202) 진입은 `EnhancementShopUnavailable`로 거절된다. 10-29 1202를 맵에서 뺐다(설계 §14). 아래 H·E1~E4는 서브 강화 기준이라, 메인 강화 상점([설계 §15](../specs/2026-10-19-enhancement-view-integration-design-Hojin.md#15-메인-강화-상점-me1me8-2026-10-29-초안), 초안)을 정할 때 함께 다시 본다.
- [ ] **H1 인수:** [Core 전달 문서 §26](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#26-강화-후보-소진-시-마지막-구매-표시용-읽기-계약-요청-2026-10-19-view--core-미전달)의 실제 DTO·ReadSink·조립·현재 Run 수명을 확인한다. 다른 구매·Complete·거절·실패가 이력을 덮지 않아야 한다. 인수 전에는 파일·인터페이스·더미 서비스를 만들지 않는다.
- [ ] **H2 표시·입력 시험:** 후보가 있으면 후보만, 0개 + 이력 있음이면 구매완료 비활성 1장, 0개 + 이력 없음이면 안내·완료만. 이력 카드는 OfferId·구매 제출이 없고 수신자에서도 거절한다.
- [ ] **H3 구현·검증:** 인수한 읽기 사실로 구매 후보와 비구매 이력을 나눈다. `Offers`·`HasPurchase`·`PurchasedMask`는 그대로 두고 새 Run·완료 실패·일반 구매 뒤 이력 보존을 확인한다.
- [ ] **E1 운영 등록:** 전용 외형·아이콘을 받아 1202 행에 잇는다. 남은 레시피 대상(약자 멸시 = Shielder, 럭키 불릿·체인지 라이더 = Akimbo, 연쇄 폭발 = DynamiteHammer)의 Unit 등록·획득·외형 인수 뒤 연다.
- [ ] **E2 에셋 검증:** `DataBaseAssetTests`로 Catalog·그룹·레시피·대상·루트·맵·외형 교차 참조·중복·누락.
- [ ] **E3 실제 플레이:** 지도 강화 구분 → 진입 → 1/2/3/0개 후보 → 구매·미선택 → 완료 실패 뒤 Complete만 재시도 → 다음 전투 → 새 Run 초기화. 자연 재화 확보와 강화 효과의 실제 발동(가시·추가 공격·방어 무시)도 본다.
- [ ] **E4 종료:** 일반 상점·회복·강화 교차 방문, 긴 한글·가격·실제 입력·팝업 뒤 지도 차단·숨김/복귀·호스트 교체, AudioSystem R 동작.
- [ ] 확인 필요: 연쇄 폭발이 `34ee0de4`(10-20)에서 표에 들어갔다(D는 "등록 보류"). 대상 DynamiteHammer는 여전히 Unit 표 미등록이다. 1202 후보에는 없어 지금 진입에는 영향이 없다.
- [ ] 확인 필요: `PlayerLoadout_1.startingRunCurrency`가 지금 5000이다(`34ee0de4`). §3.7의 Play는 운영 20G 기준이었다.
- [ ] 사용자 처리: `ShopUI.prefab` 자식 Panel 초기값(시험은 비활성 기대, 실제 활성). 사용자가 직접 끄기로 했다.

관련 시험: `EnhancementShopPopupModelTests`·`EventDescriptionCardAssetTests`·`ShopPopupModelBuilderTests`·`ShopConsumableVisualTests`·`ShopStageServiceTests`(파일 `ShopPopupControllerTests.cs`)·`ViewPopupCompositionTests`·`ShopUITests`·`RecoveryUITests`·`StageShopUIAssetTests`·`RecoveryViewAssetTests`·`RecoverySceneConnectionTests`·`StageNodeVisualTableTests`·`EnhancementStageRegistrationTests`·`DataBaseAssetTests`.

## 3 작업별 결과

### 3.1 복원 기준 (2026-10-19)

AudioSystem R 병합 뒤 `efa7c03b`에는 옛 브랜치의 Phase A 코드·프리팹·시험·계획이 없었다. 설계·계획을 지금 소스와 대조해 복원하고 Phase A를 다시 만들었다. 이전 브랜치의 통과 수치는 쓰지 않았다. 대조로 바로잡은 사실: 원본 카드에 이미 비활성 Details·`secondaryText`가 있다(480×720), `ShowStatus`는 이미 문구가 있으면 텍스트·배경을 켠다, `IsUnexpected` 사유는 이미 예외로 보고된다, 마지막 구매 요청의 실제 위치는 Core 전달 문서 §26이다(§25는 UnitInfo 요청).

### 3.2 Phase A (2026-10-19~20)

- A1: `IShopOfferContentBuilder`와 Unit·Item·Enhancement 구현을 새로 두고 `ShopPopupModelBuilder`·`ShopPopupViewFactory`·`ShopPopupText`를 고쳤다. Controller·결과 정책·UI·Buffer API는 그대로다. 누락·중복 종류 거절은 RED에서 빠진 것을 찾아 넣었다.
- A2: Unity `PrefabUtility`로 `StageEventOfferDescriptionCard.prefab`을 원본의 진짜 Variant로 만들고 ShopUI·RecoveryUI(비활성 중첩 템플릿 포함)를 같은 카드에 이었다. 버튼에 투명 클릭 Graphic을 더했다. 원본 카드 셋과 Audio/Title 소스는 바뀌지 않았다.
- 실제 1920×1080 Canvas에서 강화명·가격·대상 이름·51자 한글 설명이 넘치지 않았고, EventSystem hit 뒤 포인터·Submit이 수신자 슬롯으로 가며 잠금 뒤 제출이 막혔다.
- 시험 조립만 새 생성자에 맞췄다.

### 3.3 Phase B (2026-10-20)

- 바꾼 파일은 `ShopResultPolicy`·`ShopPopupController`·`ShopPopupText`·`RecoveryPopupController`·`RecoveryPopupText` 다섯이다.
- 상점: 정상 거절을 `ShopPopupText.Of`로 보이고, 구매 성공 뒤 완료 미수락·완료 재거절을 Status에 잇는다. 구매된 방문으로 돌아오면 `HasPurchase`로 안내·잠금을 되살린다. 방금 보인 정상 거절 안내를 다음 Tick이 덮지 않게 처리한 스냅샷을 기억한다.
- 회복: 지금 영수증의 `AppliedOptionId`와 미완료로 완료 대기를 보이고, Select 정상 거절(`InsufficientFunds`·`NoEffect`·`AlreadyResolved`)을 보인다. 공개 `RecoveryProcessedResult`가 Complete에 Accepted와 Progression을 요구하므로 Complete 정상 재거절은 만들지 않았다.
- Status 560×44·26pt에서 긴 상점 문구가 말줄임돼 구매 완료·이미 구매 안내를 짧게 줄였다.

### 3.4 Phase C (2026-10-20)

- `StageNodeVisualDataSO`에 StageKey(기본 0)와 음수 검증, `StageNodeVisualTable`에 지정 우선·공통 대체·중복/불일치 거절, `StageMapViewSystem`은 Buffer 노드의 StageKey를 넘기는 한 줄.
- 계획 착수 때 찾은 Query 대역 다섯(`StageMapViewSystemTests` 둘, `CombatEntryIntegrationTests.MapMovement`·`RecoveryStageMapViewTests`·`ViewPopupCompositionTests` 하나씩)에 overload만 더했다. `DataBaseAssetTests.EveryStage_HasNodeVisual`은 StageKey를 넘기게 했다. 새 `StageNodeVisualTableTests`.
- Factory·서비스 키·NodeView·표시 캐시·Core는 바꾸지 않았다. 운영 강화 외형은 등록하지 않았다.

### 3.5 Phase D (2026-10-20)

| 레시피 | 대상 | 입력 | 당시 | 지금 |
|---|---|---|---|---|
| 2연발 | ShotGun 11 | DoubleShot radius 1 추가 | 표 등록 | 표에 있음 |
| 도어 브리칭 | ShotGun 11 | DoorBreaching radius 1 추가 | 표 등록 | 표에 있음 |
| 독 가시 | CactusCannon 16 | ThornField 대체: 피해 7·지속 5·방어 감소 30%·Started | 표 등록 | 표에 있음 |
| 정밀 사격 | Ballista 15 | PiercingFalloff 20%, PrecisionShot 추가 | 표 등록(Ballista는 당시 EnemyOnly) | 천명님 `d586bbf1`(10-27)에서 SO·표 행 삭제 |
| 연쇄 폭발 | DynamiteHammer 24 | ChainExplosion attackPercent 50 추가 | SO만, 표 등록 보류(Unit 표·외형 없음) | `34ee0de4`(10-20)에서 표 추가. 대상은 여전히 미등록 |

- 모든 행은 레벨 1·가격 700·가중치 1. 설명은 PPT 15~16쪽과 Core 후속 확정(독 가시 30%·Started)을 따랐다. Unity Editor API로 만들었고 런타임 C#은 바꾸지 않았다.
- 실제 강화 진입 Adapter(`EnhancementShopDefinitionAdapter.Build`)가 후보마다 Unit 표 참조를 요구해서, 검증 가능한 4종만 등록한 것이 계획 D2의 판단이었다.
- 럭키 불릿·체인지 라이더(Akimbo)와 약자 멸시(Shielder)는 대상 운영 인수 뒤로 남겼다.

### 3.6 Phase E0와 1202 등록 (2026-10-20)

- E0(사용자 승인): `Stage/Enhancement/Enhancement_Default.asset`(레시피 셋)과 `Stage/Root/Stage_Enhancement.asset`(1202·mapKey 1·Regular)을 만들고 표에는 넣지 않았다. 첫 실행은 병행 작업 파일 `TutorialViewTestRig.cs`의 using 누락으로 컴파일이 멈췄고, 그 파일이 고쳐진 뒤 다시 돌렸다(남의 소스는 고치지 않음).
- §17 등록(사용자 승인): `StageRootDataTable` 끝에 루트 하나만 더했다(당시 13 → 14). 전용 View 행 없이 공통 Shop 외형을 썼다. 새 `EnhancementStageRegistrationTests`로 실제 DB 지도 생성, 레시피별 구매, 샷건만 보유 시 미선택 완료, 대상 없는 빈 방문 완료를 확인했다.
- 당시 map1 일반 후보는 12 → 13개라 노드당 강화 배정 비중은 1/13이었다. 2026-10-27에는 `StageRootDataTable`이 다른 작업자 변경으로 6행(Normal A·B·C, Shop, Event, Enhancement)이었고, 10-29 강화 행을 뺐다(설계 §14). 이미 생성된 지도는 바뀌지 않고, 출현은 보장되지 않는다.

### 3.7 실제 Play (2026-10-20)

- 실제 Editor에서 Boot → 로비 → 새 Run의 챕터 1 지도로 갔다. 자연 생성된 1202 노드를 클릭하면 일반 상점 외형에서 강화 팝업이 열리고 세 카드의 얼굴·설명·700G가 보였다.
- 운영 20G로는 골드 부족 안내·잔액 유지·오류 없음, 미선택 완료 → 다음 전투 진입(강화 없음)을 확인했다.
- 사용자가 "이번 Play에만 1000G 준비"를 승인해 Play마다 메모리 재화를 한 번만 1000G로 두었다(운영 DB 무변경). 독 가시·2연발·도어 브리칭을 각각 별도 새 Run에서 샀다. 셋 모두 700G 한 번 차감(→ 300G), 고른 개체만 해당 강화 레벨 1, 노드 클리어·지도 복귀, 연결된 다음 Combat에서 같은 개체에 강화 특성 스냅샷(독 가시는 ThornField 피해 7·5턴·방어 감소 30%·Started로 교체)이 실렸다.
- 한계: 재화를 직접 준비한 직후 열린 팝업의 잔액 문구는 20G로 남았다(준비 도구가 화면 갱신을 부르지 않음). 강화 효과의 실제 발동, 자연 재화 확보, 전투 승리 누적은 하지 않았다. 임시 관찰기·설정은 정리하고 PC 시작 설정을 되돌렸다.

### 3.8 당시 남은 기존 실패

이 작업이 고치지 않은, 변경 전후 이름·메시지가 같았던 실패다(2026-10-20 기준, 지금도 같다고 가정하지 않는다).

| 실패 | 내용 |
|---|---|
| `StageShopUIAssetTests.ShopPrefabs_ValidateRequiredReferencesAndPrescribedCanvasLayout` | Panel.activeSelf 기대 false·실제 true. 사용자가 직접 끄기로 함 |
| `RecoverySceneConnectionTests.StageSelect_RecoveryOverlayKeepsExistingMapAndOutputCameraConfiguration` | 미사용 TestCamera의 MainCamera 태그 검사. 사용자 확인으로 운영 결함 아님 |
| `CombatEntryIntegrationTests` 15건 | WinBattle/LoseBattle 시험 도우미가 메인 배치 없이 진입 |
| `DataBaseAssetTests` 8건 | 노드 아이콘 null 기대 4, Stage1101 벽 좌표, Stage2101 본진 Y, 옛 LineShader 경로, RailwayVisual mesh null |
| `StageMapViewDisplayTests.BeginMarkerMove_PreservesCurrentAndHidesOnlyPendingAppearance` | Highlight 활성 기대와 노드 프리팹 초기값 차이 |

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [Core 전달 문서 §26](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#26-강화-후보-소진-시-마지막-구매-표시용-읽기-계약-요청-2026-10-19-view--core-미전달) 마지막 구매 읽기 요청 | 천명님 | 미전달 |

## 5 레벨업 특성 성장(증강) 선택 화면 계획 (TG-0~TG-e, 2026-10-28)

2026-10-29 별도 주제로 옮겼다 → [증강 계획](2026-10-28-trait-growth-plan-Hojin.md), [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md). 증강은 메인 강화 상점과 다른 기능이다. 옛 §5.1~§5.5 본문은 git `a8612321e`.
