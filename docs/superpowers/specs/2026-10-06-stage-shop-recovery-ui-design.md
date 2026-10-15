# StageSelect 안의 상점·회복 스테이지 UI 설계

- 작성일: 2026-10-06
- 작성: View 작업(스테이지 선택 담당, 호진)
- 상태: **사용자 검토용 초안. 구현 전.**
- 브랜치: `feature/Hojin/Stage_Shop_Event`
- 기준 코드: `f68408b1`(Core `feature/Cheon/refactor/core-system` `5f2d0381` 병합 후). Core 변경 내용은 천명님 문서 「Core 계약 변경·해결 현황 공유」(2026-10-06)
- 관련: [스테이지 선택 계획](../plans/2026-10-03-stage-map-view-plan.md) 6번, [아티팩트 팝업 UI 이전 설계](2026-10-06-artifact-popup-ui-migration-design.md)(같은 UI 구조), Core 문서(천명님): [상점 연결 설계](2026-10-03-shop-stage-core-connection-design.md)·[인계](../handoffs/2026-10-03-shop-stage-core-connection-handoff.md), [회복 연결 설계](2026-10-03-recovery-stage-core-connection-design.md)

## 0. 결정 (사용자, 2026-10-06)

| 번호 | 결정 |
|---|---|
| S1 | 상점·이벤트 스테이지는 **새 씬 없이 StageSelect 씬 안의 UI**로 처리한다 |
| S2 | 이벤트 스테이지는 지금 Core의 **회복 스테이지**(`StageType.Event` = 회복)로 진행한다 |
| S3 | 상점 판매 후보는 **임시값**으로 먼저 채운다 |
| S4 | 구매·회복을 하지 않아도 **"나가기" 버튼**으로 스테이지를 완료(노드 클리어)하고 맵으로 돌아간다 |
| S5 | 상점·회복 모두 **InGame DB(규칙 값)와 View DB(화면 표시 값)** 를 갖춘다 |
| S6 | 화면 규격은 **예전 `develop-4week`의 Shop·Recovery 씬(호진 `d2aa5642`)** 에 맞춘다 |
| S7 | UI는 **현강님 UI 구조·문서**를 따른다(`UIBase`, `UIInputBinding`, UISystem으로 열고 닫기) |

## 1. Core가 제공하는 것 (확인 결과, 수정하지 않음)

| 단계 | 상점 `ICharacterShopService` | 회복 `IRecoveryStageService` |
|---|---|---|
| 진입 | `EnterShop(chapterKey, nodeId, seed)` → `ChapterProgressionResult`. 노드 선택과 제안 3개를 한 번에 Buffer에 기록 | `EnterRecovery(chapterKey, nodeId)` → `ChapterProgressionResult`. 옵션 목록 기록 |
| 행동 | `Purchase(chapterKey, nodeId, offerId)` → `CharacterPurchaseResult`(실패는 `ShopFailureReason`으로 반환, 예외 없음) | `Recover(chapterKey, nodeId, optionId)` → `bool`. **골드 부족·회복량 0·다른 옵션 이미 적용 시 예외** |
| 완료 | `CompleteShop(chapterKey, nodeId)`. 구매 0개도 허용 | `CompleteRecovery(chapterKey, nodeId)`. 회복 안 해도 허용 |
| 화면 값 | `CurrentProgress.Shop`(제안 3개: `OfferId`·`CharacterKey`·`Cost`, 구매 여부), `Currency.Amount` | `CurrentProgress.Recovery`(옵션 `Id`·`Cost`·`Heal`, 적용 옵션·지불·회복량), `Currency.Amount` |

- **재화(병합 후):** `CurrencyBuffer`는 이제 **상점·회복 전용 Run 재화**다. 전투 드로우용 스테이지 골드는 전투 상태(`CombatStateData.StageGold`)로 분리돼 서로 영향이 없다. 상점·회복 화면의 "보유 골드"는 `Currency.Amount`를 읽는다.
- **시작 Run 재화가 임시 0이다**(`PlayerLoadout_1.startingRunCurrency: 0`). 상점 가격·회복 비용이 2~10이라, 이 값이 바뀌기 전에는 운영 경로에서 구매·회복이 결제되지 않는다(§7 Q6).
- **새 Run:** StageSelect 첫 진입에서 로스터가 없으면 `StageLifeCycle`이 `NewRun`(`StartNewRun` → `EnterChapter`)을 등록한다. 상점·회복 흐름은 그 뒤 맵이 준비된 상태에서 시작하므로 영향이 없다.
- **전투 진입(병합 후):** 기본 DB 전투 준비의 T1/G7 예외가 해결됐다. 같은 `StageMapEntryFlow`에서 전투 노드도 실제 진입이 가능하므로, 노드 분기 검증 때 전투 경로도 함께 확인한다.
- 진입 뒤 완료 전에는 같은 노드든 다른 노드든 다시 진입할 수 없다(`CurrentStageUncleared`). 완료하면 그 노드는 클리어되고 다음 노드로 진행할 수 있다.
- 전투가 남아 있으면(`EndBattle` 전) 진입이 예외다. 일반 승리 복귀(F2)가 `EndBattle` 뒤 StageSelect로 오므로 정상 경로에서는 해당하지 않는다.
- 상점 제안 3개는 `seed`로 후보 중에서 뽑는다(중복 허용, 후보 1개 이상). 제안이 Buffer에 고정되므로 seed는 진입 때 한 번만 쓰인다.

## 2. 실행 흐름

```text
[맵 클릭]  StageViewBuffer 요청 → StageMapEntryFlow
             ├─ Combat → 기존 전투 진입(변경 없음)
             ├─ Shop   → 상점 진입 요청 → Job: EnterShop
             └─ Event  → 회복 진입 요청 → Job: EnterRecovery
[진입 결과] 수락 → UISystem.OpenUI(Shop | Recovery) → 입력 Layer를 팝업용으로 전환
           거절 → 이유 표시(현재는 로그), 요청 기록 정리
[행동]     구매/회복 버튼 → 요청 → Job: Purchase / Recover → 결과 문구, Buffer 기준 다시 그림
[나가기]   나가기 버튼 → 요청 → Job: CompleteShop / CompleteRecovery
             수락 → UISystem.CloseUI → 입력 Layer를 StageSelection으로 → 맵 다시 그림(클리어 표시)
             거절·실패 → 문구 표시, 팝업 유지(나가기만 다시 시도)
```

- Core 호출은 모두 View 쪽 **Plan·Job**이 한다(K2). 요청 객체(대상·상태·결과)와 스케줄러는 아티팩트 패턴(`ArtifactEditRequest`·`ArtifactEditScheduler`·`ArtifactPlan`·`ArtifactEditJob`)을 따른다.
- 한 번에 요청 하나만 실행한다(진행 중이면 버튼 입력 무시). 런타임 오류 상태면 요청을 만들지 않는다(아티팩트 D11과 같음).

## 3. 구성

| 구성 | 위치 | 맡는 일 |
|---|---|---|
| `StageMapEntryFlow` (기존, 수정) | `ViewSystem/StageMap/` | 노드 종류별 분기. 상점·회복 요청을 만들고 결과에 따라 UI 열기 |
| `StageNodeRequest`·`StageNodeScheduler` (새) | `LifeCycleSystem/LifeCycles/Stage/` | 상점·회복 요청(진입·구매·회복·완료) 하나씩 실행, 상태·결과 보관 |
| `StagePlan` (기존, 확장) | 같은 폴더 `Plans/` | 요청 → Job 생성(`CreateShopJob` 등)을 정적 메서드로 추가. 기존 `Register` 서명과 `JobType` 분기(`None`·`NewRun`→`NewRunJob`+`StageJob`·`Start`→`StageJob`, 병합 후 구조)는 건드리지 않는다. 이 파일은 Core가 사용자 결정 D2로 고쳤으므로 추가만 하고 공유 문서에 적는다 |
| `ShopJob`·`RecoveryJob` (새) | 같은 폴더 `Jobs/` | Core 서비스를 한 번 호출하고 결과를 요청에 기록. 예외는 기록 후 전파 |
| `StageNodeViewSystem` (새) 또는 `StageMapViewSystem` 확장 | `ViewSystem/StageMap/` | 팝업 열기·닫기 후 입력 Layer, 팝업 UI와 연결(host), 맵 다시 그리기 요청 |
| `ShopUI` (새, `UIBase`) | `GlobalSystem/UISystem/Screens/Stage/` | 상점 화면. 버튼은 `UIInputBinding` |
| `RecoveryUI` (새, `UIBase`) | 같은 폴더 | 회복 화면 |

- UI ↔ View 연결은 아티팩트와 같은 "좁은 인터페이스 + 씬 연결 때 한 번 Connect" 방식(이벤트 구독 없음).
- 새 `UIId` 2개(`Shop = 14`, `Recovery = 15`)를 현강님 파일 `UIIDs.cs`에 추가하고 공유 문서에 기록한다(아티팩트 U2와 같은 방식).

## 4. 화면 규격 (예전 씬 기준, Canvas 1920×1080)

**상점 `ShopUI`** — 예전 `Shop.unity`
- 전체 화면 패널 1920×1080, 배경 (0.08, 0.09, 0.12, 0.96). 뒤의 맵·아티팩트 버튼 클릭을 막는다.
- 제목 "캐릭터 상점" 48pt (0, 330) 700×80 / 보유 골드 23pt 하단 기준 (0, 180) 260×48 / 결과 문구 (0, −265) 900×60
- 제안 카드 3장: 360×430, x = −420·0·420, y = 20. 안에 이름 32pt, 가격 "NG" 28pt, 보유 수 "보유 N" 25pt, 구매 버튼 240×70(−135)
- 나가기 버튼 "나가기": 260×70, 오른쪽 아래 모서리 기준 (−200, 100)
- 구매한 카드는 "구매 완료"로 바꾸고 버튼을 끈다. 골드가 부족한 카드는 버튼을 끄지 않고 눌렀을 때 Core 결과(`InsufficientFunds`) 문구를 보여 준다(규칙 판단은 Core).

**회복 `RecoveryUI`** — 예전 `Recovery.unity`
- 패널 1160×760 가운데 + **전체 화면 투명 차단 이미지**(패널이 화면 전체가 아니므로 뒤 클릭을 막기 위해)
- 보유 골드: 패널 하단 (0, 28), 배경 300×64
- 회복 카드: 300×520, 간격 350. 안에 그림 250×220, 제목 28pt, 상세(비용·회복량) 260×80, 선택 버튼 "선택한다" 250×64
- **카드 수는 옵션 수만큼**(현재 회복 스테이지 옵션 2개). 예전 3장 고정 틀은 템플릿 하나를 복제하는 방식으로 바꾸고, 가운데 정렬한다.
- 나가기 버튼: 260×70, 예전 위치 (200, −100)은 패널 바깥이라 화면 오른쪽 아래 (−200, 100)로 맞춘다(상점과 같은 위치).
- 하나를 적용하면 나머지 선택 버튼을 끈다(Core는 옵션 하나만 허용).

## 5. DB (S5)

| 구분 | 상점 | 회복 |
|---|---|---|
| InGame(규칙) | `Stage/Shop/Shop_Default.asset`의 `candidates` — **현재 비어 있어 진입이 예외.** 임시값: Akimbo 2, ShotGun 3, Sniper 5(예전 `Shop_1` 가격). 판매 가능 유닛은 `PlayerOnly·Regular`(Akimbo·ShotGun·Sniper뿐) | 있음. `Event_Recovery` 옵션 1·2, 등록 테이블 `Recovery/RecoveryOptionDataTable`(1: 체력 10·2G, 2: 30%·5G, 3: 전체·10G) |
| View(표시) | 있음. `UnitVisual`의 `displayName`·`infoIcon`(카드 이름·그림) | 있음. `RecoveryOptionVisual`의 `displayName`. 카드 그림 Sprite는 **없음** → 필드 추가 여부 결정 필요(§7 Q4) |
| 화면 고정값 | 제목·버튼 문구는 씬 TMP에 직접(현강님 화면들과 같음) | 같음 |

- 시작 Run 재화 `PlayerLoadout_1.startingRunCurrency`(InGame, 현재 0)도 상점·회복 동작에 필요한 값이다. 기획 값 또는 임시값 결정이 필요하다(§7 Q6). Excel DT 이전 대상이라는 Core 문서 내용을 따른다.
- 상세 문구(비용·회복량)는 Buffer의 옵션 값으로 만든다(계산 가능한 값은 DB에 따로 두지 않음, P7).
- "보유 N"은 `CharacterBuffer.Owned`에서 같은 캐릭터 수를 세어 표시한다(표시용 파생값).
- 판매 후보를 바꾸는 것은 InGame DB 변경이므로 `DataBaseAssetTests`를 함께 돌린다.

## 6. 맵·입력

- **맵 다시 그리기:** 지금 `StageMapViewSystem`은 씬 진입 때 한 번만 그린다. 완료 후 클리어 표시가 바뀌도록, Buffer의 노드 클리어 상태가 바뀌면 다시 그리게 한다(제 코드 수정).
- **입력 Layer:** 팝업이 열리면 맵 클릭(`StageSelection`)을 끈다. `Common`만 있는 팝업용 Layer를 새로 두거나 기존 Layer 정의 방식을 따른다. 닫으면 `StageSelection`으로 복귀. 씬 이탈 때는 아티팩트처럼 우리 Layer일 때만 정리한다.
- **아티팩트 버튼:** 상점·회복 팝업이 열려 있는 동안 아티팩트 열기 버튼은 화면 차단으로 눌리지 않는다. 반대로 아티팩트가 열린 동안에는 맵 입력이 꺼져 상점·회복에 진입하지 않는다.

## 7. 정해야 할 것

| 번호 | 질문 | 제안 |
|---|---|---|
| Q1 | 상점 `seed`를 어디서 가져올지(Buffer에 런 seed가 없음) | 지금은 `chapterKey`·`nodeId`·`stageKey`를 섞은 고정 해시. 같은 노드면 매 런 같은 제안이 나오는 한계가 있어, 런 seed가 생기면 그것을 섞는다 |
| Q2 | `Recover`가 골드 부족·회복량 0일 때 **예외**를 던져 런타임이 멈추는 문제. 병합 후에도 그대로이며(`CharacterRecoveryCalculator`), 천명님 공유 문서에도 언급이 없다. 시작 재화가 0이라 지금 데이터로는 회복 선택 즉시 이 경우에 걸린다 | Core에 "예외 대신 실패 결과 반환"을 요청(Core 전달 문서). 그 전까지 View는 골드 부족이면 선택 버튼을 끄는 최소 방어만 한다(체력 가득 판단은 하지 않음 → 그 경우는 Core 수정 전까지 남는 위험) |
| Q3 | 맵 다시 그리기를 `StageMapViewSystem` 확장으로 할지 | 확장(제 코드). 클리어 상태 변경 감지 시 한 번 다시 그림 |
| Q4 | 회복 카드 그림을 View DB에 둘지 | `RecoveryOptionVisual`에 선택적 `icon` 필드 추가(없으면 빈 그림). 상점은 `UnitVisual.infoIcon` 사용 |
| Q5 | 나가기 실패(Core 거절) 시 | 문구 표시 후 팝업 유지, 나가기만 다시 시도(Core 인계 지침) |
| Q6 | 시작 Run 재화(`startingRunCurrency`, 현재 0) 값 | 기획 값이 오기 전까지 동작 확인용 임시값(예: 20 — 상점 1~2개 구매 또는 회복 1회 가능)을 넣을지 결정. 이 필드는 Core 문서상 임시 SO 필드이며 Excel DT 이전 대상 |

## 8. 범위 밖

- 진짜 이벤트(선택지·랜덤 보상): Core에 없음. 필요하면 Core 요청.
- 아이템 판매: 상점 데이터 구조가 캐릭터 전용.
- 런 seed 도입, 보스·챕터 완료 처리, 저장·불러오기.

## 9. 검증 계획(요약)

- EditMode: 노드 종류 분기, 요청 하나씩 실행·중복 무시, 수락 시 UI 열기·Layer 전환, 거절 처리, 구매·회복 결과 반영, 나가기 → 완료 → 닫기·Layer 복귀, 완료 실패 시 유지, 씬 이탈 정리, 버튼 On Click 0개(`UIInputBinding`).
- Boot smoke: Boot → StageSelect → 상점/회복 노드 진입(실제 DB·Core) → 구매/회복 → 나가기 → 노드 클리어·맵 갱신. 시작 재화가 0이면 구매·회복 결제 경로를 검증할 수 없으므로 Q6 결정 뒤 실행한다.
- DB: `DataBaseAssetTests`.
- 병합 후 기준선: 구현 전에 EditMode 전체를 한 번 돌려 병합 후 실패 목록을 기록한다(천명님 문서 기준 2909개 중 28 실패, 이 브랜치에서는 재확인 필요).

## 10. 변경 이력

- 2026-10-06 초안.
- 2026-10-06 Core `5f2d0381` 병합(`f68408b1`) 반영: Run 재화 분리와 시작 재화 0(§1·§5, Q6 추가), 새 Run 진입 분기(§1), T1/G7 해소로 전투 진입 가능(§1), `StagePlan` 새 구조(§3), `Recover` 예외가 그대로임을 확인(Q2).
