# 선인장 캐논 가시 폭탄 — Core → View 인계 (2026-11-17)

이 문서는 가시 폭탄 작업에서 View 작업자에게 넘기는 것만 모은 전용 인계다. 통합 인계 문서(2026-09-24)의 §5.98~§5.101·§75는 Core 쪽 기록이고, View가 할 일은 이 문서에서 갱신한다.

- 규칙 원본: [가시 폭탄 설계](../specs/2026-11-17-cactus-cannon-thorn-bomb-design.md)(R01~R16)
- 작업·검증 기록: [가시 폭탄 구현 계획](../plans/2026-11-17-cactus-cannon-thorn-bomb-implementation-plan.md)(Phase 0~4, 후속 1·2)
- 현재 상태
  - Core와 1차 View 연결은 이 작업에서 들어갔다.
  - Core·CoreIntegration 시험은 통과했다.
  - **씬 재생 미확인.**
  - 아래 §3 두 건과 폭발 소리는 View 몫으로 넘긴다.
  - 외부 미전달.

## 1. Core가 주는 것

- **상태:** `CombatCommandState.ThornBombs`(`ThornBombStateSet`)가 활성 폭탄과 다음 BombId의 원본이다. 전투가 끝나면 비고, View는 최종 동기화로만 읽는다.
- **사건(재생 순서대로 온다):**

| 사건 | 번호 | 뜻 |
|---|---|---|
| `CombatThornBombAttachedEvent` | 63 | 새 폭탄 부착. 연쇄가 다 끝난 뒤 온다 |
| `CombatThornBombDetonatedEvent` | 64 | 폭발. 폭심지(`Center`)·기폭 이유·공격 그룹이 있다. 뒤따르는 피해·넉백 사건이 이 그룹에 묶인다 |
| `CombatThornBombDiscardedEvent` | 65 | 대상이 낙사해 터지지 않고 사라짐(설계 R15). 같은 전이에 그 대상의 낙사 사건이 있다 |

- **조준 미리보기(`AttackPreview`):**
  - `ThornBombExplosions`: 폭발마다 하나, 연쇄 순서대로 온다. 각 항목에 다음이 있다.
    - `Center`, `Hits`(대상·피해), `Knockbacks`
    - `BlastCells`: 판 안의 폭발 칸
    - `KnockbackOccupiedCellsOf(대상)`: 밀기 직전 점유 칸
  - `ProjectedThornBombs`: 명령 뒤 예상 활성 폭탄
  - `ThornBombFinalHealth`: 폭발에 맞은 유닛의 정산 뒤 체력과 재구성 여부
- **연출 데이터 열쇠:** `TraitActivationKind.ThornBombDetonated = 420`. View 조회용이라 Core는 이 발동을 내지 않는다. 가시 폭탄 TraitVisual 줄의 발동 줄을 이 값으로 찾는다.

### 1.1 View가 읽는 곳

모두 기존 Buffer 경로이고, 타입은 모두 공개 계약(`Contracts.Combat`·`Contracts.Results`)이다. Core 내부(`Application/Combat/ThornBombs/`)는 View가 쓰지 않는다.

| 필요한 것 | 읽는 곳 | 지금 쓰는 View 코드 |
|---|---|---|
| 활성 폭탄(최종 동기화) | `CombatStateData.CommandState.ThornBombs` → `Bombs`(`ThornBombSnapshot`: BombId·대상·출처·출처 진영·부착 로컬 칸·저장 공격력·피해율·만료 순번) | `CombatSceneDisplay` → `CombatSceneEventPresenter.Refresh` |
| 부착·폭발·소멸 순간 | 결과 재생의 `CombatResult` 사건 목록(사건 형식 표의 줄로 처리기에 감) | `CombatThornBombEventHandler` |
| 조준 미리보기 | 미리보기 응답 `CombatInputFacts.PreviewAnswer`(`CombatPreviewResult`)의 공격 미리보기 `AttackPreview` | `AttackTargetingInteraction`·`CombatKnockbackPreview`·`CombatUnitHudFeedback` |
| 지금 턴 순번 | `CombatStateData.CommandState.Turn.ControllerTurnSequence` | (아직 쓰는 곳 없음) |

### 1.2 지금 계약에 없는 것

아래가 필요해지면 View에서 계산하지 말고 Core에 요청한다. View가 계산하면 Core 규칙을 베끼게 된다(SSOT).

- **폭발 기준 피해 숫자(예: 폭탄 호버 설명의 "7 피해"):** 식은 저장 공격력 × 피해율 ÷ 100(정수 절삭)이고, Core 내부 `ThornBombRules.CalculateBaseDamage`에 있다. 실제 피해는 보호막·피해 감소에 따라 더 줄 수 있다. 조준 중에는 `ThornBombExplosions[].Hits`와 `ThornBombFinalHealth`를 쓰면 된다. 그 밖의 상시 표시가 필요하면 `ThornBombSnapshot`에 기준 피해 읽기를 더해 달라고 요청한다.
- **"N턴 뒤 폭발" 남은 턴:** 만료 순번과 지금 턴 순번은 둘 다 공개다. 하지만 "부착 진영의 다음 자기 턴 시작에 터진다"는 판정은 Core(`ThornBombResolution.ScheduleDue`)에 있다. 표시가 필요하면 남은 턴 읽기를 요청한다.
- **부착 칸 위치:** `AttachmentLocalCell`은 여러 칸 유닛에서 공격 중심이 맞은 로컬 칸이다. 지금 부착물은 몸 중심 위에 붙고 이 값은 쓰지 않는다. 칸 위치에 붙이려면 대상 모양(프로필의 점유 로컬 칸)으로 바꿔야 하는데, 그 변환도 Core 쪽에 있다.

## 2. 이 작업에서 이미 들어간 View

View 작업자가 손댈 때 알아야 할 파일이다. 모두 이 작업 안에서 사용자 지시로 고쳤다.

| 파일 | 내용 |
|---|---|
| `ViewSystem/Combat/Display/Unit/ThornBombAttachmentPresenter.cs` | BombId별 부착물. 대상 몸을 따라가고 Cancel·Clear·Release에서 풀에 반환 |
| `ViewSystem/Combat/Playback/Handlers/CombatThornBombEventHandler.cs` | 부착·폭발·소멸 사건. 폭발 이펙트는 TraitVisual 발동 줄(420) 큐를 폭심지에 냄 |
| `ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs` | 사건 셋의 줄. 폭발은 공격 출처를 열고, 소멸은 낙사 유닛이 주체 |
| `ViewSystem/Combat/Presentation/Movement/CombatStepContacts.cs` | 부착·폭발 사건에서 걸음 묶음을 끊음(아래 D5) |
| `ViewSystem/Combat/Display/Feedback/CombatKnockbackPreview.cs`, `Input/Interaction/AttackTargetingInteraction.cs` | 연쇄 폭발 칸을 영향 칸으로, 여러 칸 유닛의 넉백 도착 칸을 점유 칸 전체로 그림 |
| `ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs` | `Refresh(…, thornBombs)` 폭탄 인자 필수. 비우려면 `ThornBombStateSet.Empty`를 명시 |
| `Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs` | 부착물 presenter 조립. TraitVisual 가시 폭탄 줄과 부착물 프리팹을 필수로 읽음 |
| `DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs`, `Queries/ViewDataBase/ViewTable/TraitVisualTable.cs` | 부착물을 붙이는 특성(지금은 가시 폭탄)의 줄·프리팹이 없으면 부트 DB 생성에서 멈춤. 규칙 원본은 `TraitVisualData.RequiresAttachment` |
| `DataBaseSO/ViewGroup/TraitVisual/Item/Cannon/TraitVisual_ThornBomb.asset` | 부착물 `CactusBomb.prefab`, 폭발 발동 줄(Boom·Start·SelectedCell·자연 끝) |

## 3. View 작업자에게 넘기는 것

| ID | 무엇 | 왜 | 제안 |
|---|---|---|---|
| D5 | `CombatStepContacts`가 걸음 묶음을 끊는 조건에 폭탄 사건 형식 둘(부착·폭발)을 이름으로 적었다 | 비슷한 사건이 생길 때마다 범용 클래스를 고쳐야 한다. 지금 동작에는 문제가 없다 | 사건 형식 표(`CombatEventTypeTable`)에 열 하나를 두고 그 값으로 판정한다 |
| D6 | 부착물 풀의 초기 수가 0이다 | 전투에서 첫 부착 때 재생 도중 `Instantiate`가 일어나 작은 끊김이 날 수 있다. 설치 때 미리 만드는 규칙(GH6)과 다르다 | 가시 폭탄 보유 유닛이 있는 전투면 설치 때 하나를 미리 만든다 |
| 소리 | 폭발 소리가 없다 | 특성 발동 줄에는 소리 칸이 없다. 사용자 결정으로 소리 창구(슬롯)는 나중에 연다 | 슬롯이 열리면 폭발 연출에 연결한다. 연결 방식(발동 줄에 소리 칸을 더할지, 사건 소리 표에 줄을 둘지)은 그때 정한다 |

## 4. View 쪽 Unity 확인

번호는 구현 계획과 같다. 자동 시험이 없는 항목이다.

| ID | 조건 | 기대 결과 |
|---|---|---|
| V10 | 같은 입력으로 미리보기·실행, 미리보기 반복·취소 | 연쇄 중심·순서·피해·넉백·최종 폭탄이 같다. 미리보기가 원본을 바꾸지 않는다 |
| V11 | B가 밀린 뒤 폭발 / 사망 오브젝트 반환 / 씬 해제 | 이동 뒤 위치에서 폭발한다. 반환 대상이 없어도 사건 중심을 쓴다. 누수·이중 반환이 없다 |
| V14 | 취소·전투 종료·재진입·최종 동기화 | 부착물·폭발 표시가 남지 않는다 |
| V19 | 폭탄 유닛이 넉백으로 낭떠러지에 떨어짐 | 폭발이 없다. 추락 연출 뒤 부착물이 사라진다. 다른 유닛의 동시 넉백이 끊기지 않는다 |
| V20 | 폭탄 유닛이 기차에 치임 | 기차 연출 뒤 그 레일 칸에서 폭발·넉백·연쇄가 일어나고, 전투가 멈추지 않는다 |
| V21 | 자폭·공포 유닛이 기차에 치임 | 기차 연출 뒤 그 레일 칸에서 자폭 깜빡임·폭발·공포가 나온다 |
| V22 | 가시 폭탄 TraitVisual 줄을 빼거나 프리팹을 비우고 부트 | DB 생성에서 멈춘다 |
| V23 | 연쇄가 생기는 조준 호버(여러 칸 유닛 포함) | 연쇄 폭발마다 3×3(판 안) 영향 칸. 여러 칸 유닛의 도착 칸이 유닛 모양 전체 |
| V24 | 폭발 재생 | 폭심지에 Boom(옛 화면과 같다). TraitVisual 발동 줄의 이펙트를 바꾸면 따라 바뀐다 |

## 5. 참고: Core 보류

D8은 View 몫이 아니다. 폭탄 게시 검증이 "최종 턴으로 바로 가는 턴 시작"만 순번으로 세는 점이다. 한 전이에 턴 시작이 둘 들어오는 경로가 지금은 없어 Core에서 보류한다.
