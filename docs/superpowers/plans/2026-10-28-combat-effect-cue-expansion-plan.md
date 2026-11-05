# 전투 이펙트 창구 확장 — Task 계획

> 날짜: 2026-10-28
> 상태: 계획 작성(코드 착수 전). 7기준 검토 권장 반영(2026-10-28 사용자 "권장대로 반영", 설계 §7, F2 = A). 계획에서 정한 것 P1~P19는 **그대로 확정(2026-10-28 사용자 "모두 그대로")**(§1, 반영으로 바뀐 것 P3·P6·P8·P11, 새로 P15~P19). 착수 조건 §0.2 중 맵 타일은 풀렸고, 소리 2차·걸음 모양 커밋 전에 사용자 지시로 착수했다. **EC-a 코드 완료(2026-10-28, Ua 문제 없음 — 사용자, 기록 §6.1). EC-b 연결 코드 완료(2026-10-28, Ub 문제 없음 — 사용자, 기록 §6.2). EC-c 폭발 옮기기 코드·에셋 완료(2026-10-28, Uc 문제 없음 — 사용자, 기록 §6.3). EC-d 문서·공유·묶음 끝 검토 완료(2026-10-28, 검토 약 9.7 — 중간 1·낮음 5, 기록 §6.4). 검토 권장 반영(2026-10-28 사용자 "권장대로 반영", M1·L1·L3·L4·L5, L2 그대로, P19 바뀜, 기록 §6.5). Ud 1~4 문제 없음(2026-10-29 사용자). 사용자 커밋 `efa5e7d7`(소리 창구 2차와 한 커밋, §4.1).**
> 설계: [effect-cue-expansion-design](../specs/2026-10-28-combat-effect-cue-expansion-design.md)(EC1~EC10, 결정 §4 — Q-EC1~Q-EC7 모두 A, 2026-10-28 사용자 "모두 권장안대로", 검토 §7)
> 관련: [소리 창구 2차 계획](2026-10-28-combat-environment-item-sound-plan.md)(같은 사건·같은 값 자리), [범용 연출 구조 계획](2026-10-27-combat-presentation-generic-structure-plan.md)(큐 형식·규칙·붙이기의 원본)

## 0. 진행 규칙·착수 조건

### 0.1 진행 규칙

- Phase가 끝나면 멈추고 보고한다. 다음 Phase는 사용자 지시로 시작한다.
- 새 시험은 쓰지 않는다. MSBuild는 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다(View 작업 지시).
- 기존 시험은 컴파일이 깨지는 곳만 최소로 맞춘다. 이번에 맞출 곳은 천명님 시험 두 줄이다(§1 P14).
- 검토 에이전트는 EC-d에서 한 번 돌린다(묶음 끝에 한 번).
- 커밋은 사용자가 한다.

### 0.2 착수 조건

| 조건 | 까닭 |
|---|---|
| 소리 창구 2차 코드 커밋 | 같은 SO 다섯(오브젝트·유닛·아이템 외형, 설정 표, 토네이도 설정)과 `CombatDisplayTables`·`CombatCharacterEventHandler`·`CombatEventSounds`를 고친다. 특히 소리 표의 사실 갈래는 이번에 순간 값으로 옮긴다(P16) — 2차 결과를 먼저 고정해 두어야 "동작 같음"을 대조할 수 있다 |
| 맵 바닥 타일 TA-b 커밋(다른 세션) | `CombatSceneDisplayFactory`를 둘 다 고친다(EC-c) |
| 걸음 모양 설계(다른 세션, 결정 대기)와 파일 확인 | 그쪽이 착수하면 `CombatSceneEventPresenter`·`CombatEventPresentationValues` 주변이 겹칠 수 있다. 먼저 착수한 쪽이 커밋한 뒤 다른 쪽 |

- 작업 트리의 `CombatAttack_Ballista.asset` 변경은 이번 것이 아니다. 커밋 묶음에서 뺀다.

## 1. 계획에서 정한 것(P1~P19, 사용자 확인 대상)

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| P1 | 보정 칸 이름은 `positionOffset`(Vector3)·`rotationOffset`(Vector3, 오일러 각). 값 검사 규칙은 두지 않는다 | 어떤 값이든 뜻이 있다(0 = 지금과 같음). 툴팁에 자리 축과 "위치는 월드 단위, 필드 배율을 따르지 않음"을 적는다(검토 F9) |
| P2 | 보정 계산의 원본은 `CombatCuePoints.Apply(Pose 자리, CombatEffectCue 큐)` 정적 함수 하나 | 자리 해석이 이미 `CombatCuePoints` 한 곳이라 계산도 같은 곳에 둔다 |
| P3 | 보정을 적용하는 곳은 자리마다 하나다(검토 F7). 지점 자리 둘(`AtClipPoint`·`AtClipMuzzle`)은 `EffectType` 대신 큐(`CombatEffectCue`)를 받아 시작 순간 지점에 `Apply`한다. 정해진 자리의 `At`은 `CreatePiece`가 이미 보정한 자리를 받아 그대로 쓴다 | 지점 자리는 조각이 시작하는 순간에만 읽을 수 있다. 정해진 자리까지 조각이 다시 보정하면 두 번 적용된다. 조각을 만드는 곳은 `CombatCuePoints` 하나뿐이다 |
| P4 | 새 열거 값: 자리 `EventCell = 6`, 칸 `ObjectActivated = 5`·`ObstacleHit = 6`·`ObstacleDestroyed = 7`·`Fall = 8`·`WeakGroundCollapse = 9`·`ItemActivated = 10`·`Tornado = 11` | 이미 있는 값 뒤에 이어 붙인다. 에셋에 저장된 값은 그대로다 |
| P5 | 새 입력 형식 이름은 `CombatVisualCueInput`(이펙트·흔들림 목록 둘) | 소리·플래시를 받지 않는 칸이 같은 형식을 쓴다(설계 EC4) |
| P6 | 큐 값은 내는 곳 `CombatMomentCuePlayback`이 열쇠(순간 종류 + 열쇠)마다 처음 읽을 때 보관하고 그 연결 동안 다시 쓴다(검토 F8). 내는 곳은 사건 Presenter와 함께 연결마다 만들어지므로(`CombatViewSystem.cs:456` → 표시 Factory) 따로 비우지 않는다. SO를 에디터에서 고치면 다음 전투 진입부터 반영된다 | 사건마다 읽으면 `ToCues()`가 매번 배열을 만든다. 유닛 표시 값 보관(`CombatUnitPresentationValues`)과 같은 방식이고, 비우는 때가 연결 수명으로 정해진다 |
| P7 | 곁들임 범위는 `CombatPieceScope.Accent = 3`, 조립 입구 `CombatComposition.ForAccent()`. 곁들임 조립은 채널 대여를 거절하고, 사건 완료 콜백 없이 시작한다(`TryBegin(조립, null, 세대)`) | 설계 EC6. 재생기의 막기 규칙은 행동 실행·유닛 실행만 세므로 고칠 곳이 거의 없다(검토 확인) |
| P8 | 새 파일 넷(모두 가칭): 순간 값 `Playback/Ordering/CombatEventMoment.cs`(형식 표 옆), 값 답 `Playback/Support/CombatCueSources.cs`, 내는 곳 `Playback/Support/CombatMomentCuePlayback.cs`, 지형 변화 분류 `Playback/Support/CombatTerrainChanges.cs`(구현은 `Playback/Ordering/` — §6.2). 1판의 사건 → 큐 표 `CombatMomentCues`는 만들지 않는다 | 폴더 배치 안내 규칙(`:117`, 새 분류기 파일 금지) — 사건 형식별 지식은 형식 표 칸으로(P15, 검토 F2 A) |
| P9 | 값 답 `CombatCueSources`는 `CombatDisplayTables`가 만들고, 사건 Presenter 생성자가 새 필수 인자로 받는다(`sounds` 다음). 선택 인자로 두지 않는다 | V3-i가 Presenter 의존을 모두 필수로 했다. null 갈래를 다시 만들지 않는다. 대신 Factory 한 줄·천명님 시험 한 줄을 맞춘다 |
| P10 | 주체 몸 자리는 시점별로 받는다(`CombatCuePoints.ForSubject(start, end)`). End 시점 큐는 끝 자리를 쓴다. 피격·사망은 두 자리가 같다 | 토네이도는 휩쓸리기 전 자리와 내려앉은 자리가 다르다(설계 EC8). 피격·사망은 지금과 같다 |
| P11 | 정적 지형 변화 분류를 `Playback/Support/CombatTerrainChanges.cs`(가칭, 구현은 `Playback/Ordering/` — §6.2) 하나로 모은다: 종류 바뀜·같은 종류(진입 기록)·약한 지반 붕괴. 부르는 곳은 사실 처리기·동시 묶음·형식 표의 순간 칸이다(소리 표는 순간 값을 읽으므로 직접 부르지 않음). 커밋을 따로 나눈다(§4, 검토 F5) | 사실 처리기·동시 묶음·소리 표에 이미 세 벌이다(소리 2차 검토 L4, 설계 EC7). 동시 묶음 순서 코드까지 고치므로 먼저 따로 확인한다 |
| P12 | 화약통·지뢰 폭발 큐 값은 [Boom, Start, 지연 0, EventCell, 위치 보정 0, 회전 보정 (−90, 0, 0)], 흔들림 없음 | 지금 환경 폭발과 같은 시점·회전이다. 높이만 칸 표면으로 바뀐다(Q-EC6 A) |
| P13 | 설정 표 "환경 연출" 묶음(`environmentExplosionEffect`)은 코드와 함께 기본 에셋 줄도 지운다. 새 묶음 머리말은 "전투 연출 큐 (이펙트 창구 확장 설계 EC5)" | 쓰지 않는 직렬화 줄을 남기지 않는다 |
| P14 | 천명님 `PullAndMarkPresentationTests` 두 줄을 컴파일 맞춤으로 고친다: 필드 창구 생성의 둘째 인자 `_ => { }` 삭제(`:422`), Presenter 생성(`:413-419`)에 새 인자(`CombatDisplayTables.From(…).CueSources`, 소리 프리셋 줄과 같은 꼴). 단언은 그대로 | 규칙상 컴파일 깨짐은 최소로 맞춘다. Core 전달 문서 다음 절에 기록한다 |
| P15 | 순간 값 `CombatEventMoment`(readonly struct: 순간 종류 열거 `CombatMomentKind` + 열쇠 셋 중 하나 + 사건 칸 또는 주체 id). 형식 표 `Row<T>` 끝에 선택 인자 `moment`(`Func<T, CombatEventMoment?>`, 기본 null)를 더하고 질문 `TryGetMoment(e, out moment)` 하나. 채우는 줄은 여섯(오브젝트 발동·생성, 장애물 반응, 추락, 정적 지형 원본 사건, 아이템). 순간 종류: `ObjectActivated`·`ObjectCreated`·`ObstacleHit`·`ObstacleDestroyed`·`Fall`·`WeakGroundCollapse`·`ItemActivated` | 설계 EC7(검토 F2 A). 선택 인자라 다른 열넷 줄은 그대로다. 조건(아이템 `Activated`·장애물 반응·약한 지반 붕괴)은 이 칸 한 곳에만 둔다 |
| P16 | 소리 표 `CombatEventSounds.For`의 사실 갈래 다섯과 생성 갈래를 `TryGetMoment` 한 갈래 + 순간 종류 → (슬롯, 프리셋) 대응 하나로 바꾼다. 장애물의 "프리셋 없이" 규칙·슬롯 대기(null)는 그대로 | 소리 결과는 지금과 같다. 순간 종류 → 값 대응은 소리 표와 큐 값 답이 각자 갖는다(값이 다르므로 사본이 아님, 설계 §7.4) |
| P17 | 빈 조립 막기: `CombatComposition`에 질문 `IsEmpty` 하나, 내는 곳이 큐를 붙인 뒤 비면 시작하지 않는다(검토 F1). 토네이도 조립은 뼈대 항목(들어 올림)이 있어 해당 없음 | 수신기 없음·흔들림 세기 0·몸 못 찾음이면 항목이 0이 되어 `MarkUsed`가 예외(`:240`). 데이터 한 줄로 전투가 멈추지 않게 한다 |
| P18 | 장애물 큐 칸 검사: `UnitVisualData.Validate`가 키가 `ObstacleContentProfile.IsNativeObstacleKey`가 아니고 `obstacleHitCues`·`obstacleDestroyedCues`에 항목이 있으면 거절한다(검토 F6). 파일이 이미 Core 계약 `EntityKey`를 쓰므로 using 하나만 더한다 | 캐릭터 외형에 채운 값이 조용히 무시되지 않게 한다. Core 규칙을 사본으로 두지 않는다 |
| P19 | 토네이도 끝 자리: `CombatShownFacts`에 사실 값을 받아 몸 가운데를 답하는 함수 하나(`TryGetBodyCenterOf(CharacterCombatFacts, out Vector3)`, 가칭, `FieldUnitPresenter.BodyCenterOf`를 부름)를 더한다(검토 F11). 지연 툴팁에 "유닛 실행이라 그 유닛과 주체 없는 사건을 늦춤"을 적는다(검토 F3). **바뀜(§6.5, 묶음 끝 검토 M1):** 토네이도 칸 큐는 지연 0만 받는다(늦게 끝나는 큐가 착지 칸을 되돌림), 툴팁도 "지연 0만" | 내려앉는 자리는 아직 보이지 않은 도착 사실이다. 지금 함수는 보인 사실만 찾는다 |

## 2. Phase

| Phase | 내용 | 화면 |
|---|---|---|
| EC-a 값 | 보정 칸, 새 자리·칸·규칙, 새 입력 형식, 값 자리 칸·검사 | 같음 |
| EC-b 연결 | 지형 변화 분류 하나(커밋 따로), 형식 표 순간 칸·소리 표 옮김, 보정 적용, 사건 칸·시점별 주체 자리, 곁들임 실행·빈 조립 막기, 값 답·내는 곳, 토네이도 큐 | 같음(새 값이 모두 비어 있음) |
| EC-c 폭발 옮기기 | 화약통·지뢰 폭발 큐 에셋, 환경 폭발 창구·규칙·설정 칸 삭제, 천명님 시험 줄 | 고지대 밖은 같음 |
| EC-d 문서·공유·검토 | 공유 메모, 설계 구현 메모, 폴더 배치 안내, 사운드 작업자 안내 문서의 이펙트 쪽 안내, 묶음 끝 검토 | — |

## 3. Task

### EC-a 값

| Task | 파일 | 내용 |
|---|---|---|
| Ea.1 | `DataBaseSystem/Data/View/CombatPresentation/CombatCueEntries.cs` | `CombatEffectCueEntry`에 `positionOffset`·`rotationOffset` 칸과 툴팁(자리 축: x 오른쪽·y 위·z 그 자리의 앞, 자리마다 앞이 무엇인지, 위치는 월드 단위 — P1). 내부 생성자 끝 선택 인자 둘. `CombatEffectCue`에 `PositionOffset`(Vector3)·`RotationOffset`(Quaternion, 생성자에서 한 번 계산) |
| Ea.2 | `CombatCuePoint.cs` | `EventCell = 6`과 문서(사건 칸 표면 가운데, 필드 회전). `ActorMuzzle` 주석을 코드에 맞게 "회전은 행위자 자세 방향"으로 고침(설계 §1.4) |
| Ea.3 | `CombatPresentationCueRules.cs` | `CombatCueEvent`에 칸 일곱(P4). `AllowsAnchor`: 다섯 사실 칸·아이템 = Start, 토네이도 = Start·End. `AllowsPoint`: 오브젝트·장애물 둘·추락·약한 지반 = EventCell, 아이템·토네이도 = SubjectBody. `FindError`의 소리 시점 조건을 "공격 둘·사망만"으로. 소리 목록·플래시는 지금 규칙(피격만)이 새 칸을 거른다. 머리 주석의 시점 표에 새 칸 |
| Ea.4 | `CombatEventCues.cs` | 새 입력 `CombatVisualCueInput`(`effects`·`shakes`) → `ToCues()` = 소리 시점·플래시 없음 |
| Ea.5 | `Object/Item/ObjectVisualDataSO.cs` | SO 칸 `activationCues`(`CombatVisualCueInput`, 툴팁 "발동(지뢰)·습득(깃발·배지) 순간, 사건 칸 자리"), Data 생성자 새 필수 인자, `ActivationCues` 속성, 생성자에서 `Require(cues, ObjectActivated)` |
| Ea.6 | `Unit/Item/UnitVisualDataSO.cs` | 칸 `obstacleHitCues`·`obstacleDestroyedCues`(툴팁 "장애물 외형(화약통·보급품)에서만 씀, 다른 외형에 넣으면 DB 생성이 멈춤"), 속성 둘(읽을 때 `ToCues()` — 보관은 내는 곳, P6), `Validate`가 둘을 규칙에 넣어 보고, 장애물 키가 아니면 항목이 있을 때 거절(P18) |
| Ea.7 | `ConsumableItem/Item/ConsumableItemVisualDataSO.cs` | 칸 `activationCues`(툴팁 "쓰인 순간, 중복 효과도 같음, 소지자 몸 자리"), 속성, `Validate` |
| Ea.8 | `CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` | 새 머리말 묶음(P13)에 `fallCues`·`weakGroundCollapseCues`, 속성 둘, `Validate`. 환경 폭발 칸은 EC-c에서 지운다 |
| Ea.9 | `Terrain/Item/TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`(호진님) | SO 칸 `cues`(툴팁 "Start = 휩쓸리는 순간, End = 내려앉는 순간, 그 시점 유닛 몸 자리. 지연을 주면 그 유닛과 주체 없는 사건이 그만큼 늦어짐" — P19), Data 생성자 새 필수 인자·`Cues` 속성·`Require(cues, Tornado)` |

- 사실 칸 여섯(오브젝트·장애물 둘·추락·약한 지반·아이템)의 지연 툴팁에는 "다음 사건은 막지 않지만, 기차 사건과 결과 끝은 그만큼 기다림"을 적는다(설계 EC3·EC6, 검토 F4).
- 확인(Ua, 사용자):
  1. 컴파일, Boot에서 DB 생성이 오류 없이 끝남.
  2. 인스펙터: 공격 묶음·반응 묶음의 이펙트 항목에 보정 칸 둘. 오브젝트·유닛·아이템 외형, 설정 표, 토네이도 설정에 새 큐 목록.
  3. (선택) 오브젝트 외형의 새 큐에 시점 End를 넣으면 Boot DB 생성이 이유 문구와 함께 멈춤. 확인 뒤 되돌림.
  4. (선택) 캐릭터 유닛 외형(예: Akimbo)의 `obstacleHitCues`에 항목 하나를 넣으면 Boot DB 생성이 멈춤(P18). 확인 뒤 되돌림.
  5. 전투 화면이 지금과 같음.

### EC-b 연결

| Task | 파일 | 내용 |
|---|---|---|
EC-b는 두 덩어리다. Eb.1은 지형 변화 분류만 옮기는 동작 같은 정리이고, 커밋을 따로 나눈다(P11, §4). Eb.2부터가 이펙트 연결이다.

| Task | 파일 | 내용 |
|---|---|---|
| Eb.1 | 새 `Playback/Support/CombatTerrainChanges.cs`(+ `.meta`, 구현은 `Playback/Ordering/` — §6.2) | 정적 지형 변화 분류 하나(P11): `TryGetKindChange(e, out before, out after)`, `IsSameKindEntry(e)`, `IsWeakGroundCollapse(e, out cell)`. 사실 처리기 `:74`, 동시 묶음 `CombatConcurrentGroups:94`, 소리 표 `IsWeakGroundCollapse`가 이것을 부름(동작 같음). 소리 표의 호출은 Eb.3에서 순간 칸으로 옮겨 간다 |
| Eb.2 | 새 `Playback/Ordering/CombatEventMoment.cs`(+ `.meta`), `Playback/Ordering/CombatEventTypeTable.cs` | 순간 값·종류 열거(P15). `Row<T>` 끝 선택 인자 `moment`, `TypeRow` 칸, 질문 `TryGetMoment`. 채우는 줄 여섯(오브젝트 발동·생성, 장애물 반응 = 반응 종류로 피해/파괴, 추락, 정적 지형 원본 = `CombatTerrainChanges.IsWeakGroundCollapse`, 아이템 = `Activated`만) |
| Eb.3 | `Playback/Support/CombatEventSounds.cs` | 사실 갈래 다섯·생성 갈래 → `TryGetMoment` 한 갈래 + 순간 종류 → (슬롯, 프리셋) 대응(P16). 장애물 도우미의 "프리셋 없이" 규칙·슬롯 대기 값은 그대로 |
| Eb.4 | `Presentation/Pieces/CombatCuePoints.cs` | `Apply`(P2), 사건 칸 자리 입구 `ForCell(Pose)`, 시점별 주체 자리 `ForSubject(start, end)`(P10, 옛 한 인자 판은 둘이 같은 자리), `CreatePiece`가 EventCell·시점별 주체 자리를 해석하고 정해진 자리에 보정을 적용(P3) |
| Eb.5 | `Presentation/Pieces/CombatEffectPiece.cs` | 지점 둘(`AtClipPoint`·`AtClipMuzzle`)이 큐를 받아 시작 순간 지점에 `Apply`. `At`은 받은 자리를 그대로 씀(P3) |
| Eb.6 | `Presentation/Attack/CombatAttackPlayback.cs` | `PlayImpact`가 접촉 자리에 `Apply` |
| Eb.7 | `Presentation/Pieces/CombatComposition.cs` | `CombatPieceScope.Accent`·`ForAccent()`, 곁들임이면 `Lease`·`LeaseCameraZoom` 예외(P7), 질문 `IsEmpty`(P17). 범위 주석에 "기차 사건·결과 끝은 기다림"(설계 EC6) |
| Eb.8 | `Playback/Players/CombatPieceRunPlayer.cs` | 곁들임 실행은 행동 하나 규칙·주체 막기에 들지 않음을 확인(코드는 범위로 이미 나뉨, 검토 확인). 주석에 셋째 범위. 바꿀 줄이 있으면 그 줄만 |
| Eb.9 | 새 `Playback/Support/CombatCueSources.cs`(+ `.meta`) | 값 답: 순간 종류·열쇠 → 큐 묶음(`Object(종류)`·`Obstacle(종류, 반응)`·`Item(키)`·`Fall()`·`WeakGroundCollapse()`). 외형이 없으면 빈 큐. `CombatDisplayTables`에 `CueSources` 한 줄(소리 프리셋 줄 옆). 소리 프리셋 답과 합치지 않음(설계 §7.4) |
| Eb.10 | `Playback/Support/CombatShownFacts.cs` | 사실 값의 몸 가운데 `TryGetBodyCenterOf(CharacterCombatFacts, out Vector3)`(P19) |
| Eb.11 | 새 `Playback/Support/CombatMomentCuePlayback.cs`(+ `.meta`) | `TryGetMoment` → 큐 값(열쇠별 보관, P6) → 비어 있지 않으면 곁들임 조립(시점 표 Start만) + 큐 붙이기(소리 없음). 칸 자리 = `CellSurfaceToWorld`·`RotationToWorld(R0)`, 주체 자리 = 보인 사실 몸 가운데·서 있는 방향. 몸을 못 찾으면 그 자리 이펙트는 넣지 않음. **붙인 뒤 `IsEmpty`면 시작하지 않음(P17).** 조각 실행 재생기에 `TryBegin(조립, null, 세대)` |
| Eb.12 | `Playback/CombatSceneEventPresenter.cs` | 생성자 새 필수 인자 `cueSources`(P9), 내는 곳을 만들고 `PresentEvent`의 `_sound.Play(e)` 바로 뒤에서 한 줄로 부름. 환경 폭발 호출은 EC-c까지 그대로 |
| Eb.13 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` | Presenter 생성 줄에 `tables.CueSources` |
| Eb.14 | `Playback/Handlers/CombatCharacterEventHandler.cs`(`TryBeginTornado`, 호진님 줄) | 시점 표(Start = 조립 시작, End = 들어 올림 조각 끝)·시점별 주체 자리(끝 = `TryGetBodyCenterOf(Final)`)·큐 붙이기 한 줄. 휩쓸림 소리 조각·착지 소리는 그대로 |
| Eb.15 | `PullAndMarkPresentationTests.cs`(천명님) | Presenter 생성(`:413-419`)에 새 인자(P14 앞 절반) |

- 확인(Ub, 사용자):
  1. 컴파일, 전투 진입.
  2. 지금 이펙트가 그대로 나는지: 착탄 Boom(발리스타·선인장 대포·언데드 마인카트), 피격·사망 Hit, 화약통·지뢰 폭발(아직 옛 길).
  3. 소리가 그대로인지(Eb.3): 지금 소리가 나는 장애물 피해·파괴는 같은 소리. 나머지 사실 소리는 슬롯 대기라 원래 안 난다.
  4. 보정: 기본 반응 묶음 피격 Hit에 위치 보정 y 0.5를 넣으면 맞은 유닛 몸에서 그만큼 위에 남. 확인 뒤 되돌림.
  5. 새 칸(폭발과 겹치지 않게 Hit 이펙트로 시험한 뒤 되돌림): 깃발 오브젝트 외형 `activationCues`에 Hit → 깃발 습득 순간 그 칸에, 설정 표 `fallCues`에 Hit → 넉백으로 낭떠러지에 떨어질 때 그 칸에, 아이템 외형(위스키 등) `activationCues`에 Hit → 쓰인 순간 소지자 몸에, 토네이도 설정 `cues`에 Start·End 하나씩 → 휩쓸리는 자리·내려앉는 자리.
  6. 빈 조립(P17): 깃발 외형 `activationCues`에 흔들림 하나만(세기 0) 넣고 깃발을 습득해도 예외가 없음. 확인 뒤 되돌림.
  7. 새 큐에 지연 1초를 넣어도 다음 사건이 기다리지 않는지, 결과가 끝날 때 남은 이펙트가 나고 나서 끝나는지. (있으면) 같은 결과의 기차 사건은 그 이펙트 뒤에 시작함(설계 EC6).
  8. 약한 지반(`Test` 스테이지 칸 (6,3)): 무너질 때 칸 외형이 지금처럼 바뀜(분류 하나로 옮긴 뒤 동작 같음).

### EC-c 폭발 옮기기

| Task | 파일 | 내용 |
|---|---|---|
| Ec.1 | `DataBaseSO/ViewGroup/UnitVisual/Item/Object/UnitVisual_PowderBarrel.asset`, `DataBaseSO/ViewGroup/ObjectVisual/Item/ObjectVisual_Mine.asset` | 폭발 큐 항목(P12) |
| Ec.2 | `CombatSceneEventPresenter.cs`, `CombatFieldEventPorts.cs` | 환경 폭발 호출·창구 칸 `presentEnvironmentEffect` 삭제 |
| Ec.3 | `CombatSceneDisplayFactory.cs`, `Display/CombatSceneDisplay.cs`, `Playback/CombatSceneEventPlayback.cs` | 환경 폭발 Presenter 생성·전달·`Bind`·`Clear`·`Cancel`·`Release` 줄 삭제(이펙트 회수는 이펙트 창구가 이미 맡음) |
| Ec.4 | `Display/Map/CombatEnvironmentEffectPresenter.cs`·`CombatExplosionRules.cs`(+ `.meta`) | 삭제 |
| Ec.5 | `CombatPresentationSettingsDataSO.cs`, `CombatPresentationSettings_Default.asset` | "환경 연출" 칸·속성·검사와 에셋 줄 삭제(P13) |
| Ec.6 | `PullAndMarkPresentationTests.cs:422`(천명님) | 필드 창구 둘째 인자 삭제(P14 뒤 절반) |

- 확인(Uc, 사용자):
  1. 컴파일, Boot DB 생성.
  2. 화약통을 부수면 폭발이 전과 같은 자리·방향으로 한 번만 남. 지뢰를 밟아도 같음.
  3. 폭발 도중 결과 취소(포기 등)·전투 이탈에서 이펙트가 남지 않음.
  4. (있으면) 고지대 칸 위 화약통·지뢰는 폭발이 블록 위에 남.

### EC-d 문서·공유·검토

| Task | 내용 |
|---|---|
| Ed.1 | 호진님 설정 표 메모 §9(설정 표 큐 칸 둘·환경 폭발 칸 삭제·토네이도 설정 `cues`·`TryBeginTornado` 한 줄·지연이 유닛 실행을 늦춤) |
| Ed.2 | Core 전달 문서 다음 절(천명님 시험 두 줄, Core 파일 변경 없음, 요청 없음) |
| Ed.3 | 민형님 Factory 공유 §36(표시 Factory의 Presenter 인자 한 줄·환경 폭발 생성 줄 삭제) |
| Ed.3b | 현강님 사건 소리 메모 §9(소리 표 사실 갈래를 형식 표 순간 칸으로 옮김, 결과 같음) |
| Ed.4 | 폴더 배치 안내: 새 파일 넷 배치, 규칙 한 줄("사실 사건의 순간은 형식 표 `moment` 칸이 답하고, 소리 표와 큐가 같이 읽는다"), 옛 이름 정리(F10 — 안내 `:84`·`:117`, `CombatPresentationEffectRules.cs:7` 주석, `CombatFieldEventPorts` 요약, `CombatSceneEventPresenter.cs:298` 주석) |
| Ed.5 | 설계 구현 메모(§7 새 절), 계획 실행 기록(§6) |
| Ed.6 | 사운드 작업자 안내와 같은 꼴의 이펙트 안내가 필요하면 사용자에게 물음(이번 계획에는 쓰지 않음) |
| Ed.7 | 묶음 끝 검토 에이전트 하나(7기준, 설계 §6 예상 8.6·§7 검토 예상 약 9.6과 대조) |
| Ed.8 | CLAUDE.md 현재 상태·메모리 |

## 4. 커밋 묶음

- **Eb.1(지형 변화 분류)은 따로 먼저 커밋한다(P11, 검토 F5).** 동시 묶음 순서 코드를 고치는 동작 같은 정리라, 이펙트 묶음과 섞이면 순서 문제가 생겼을 때 원인을 가르기 어렵다. 새 `CombatTerrainChanges.cs`(+ `.meta`)와 부르는 곳 셋.
- **EC-a 값 파일 여섯은 다른 작업의 커밋 전 변경과 섞여 있다(2026-10-28 착수 때).** 오브젝트·유닛·아이템 외형 SO, 설정 표 SO, 토네이도 설정 SO·Data에는 소리 2차 hunk가, 설정 표 SO에는 걸음 모양 ST-a hunk도 있다. 소리 2차·걸음 모양을 먼저 커밋하려면 그 파일에서 EC-a hunk(큐 칸·속성·검사·using)를 빼고 넣는다.
- **그 밖의 EC-a~EC-c는 한 커밋을 권장한다.** EC-a만 커밋해도 DB 생성은 되지만, EC-b의 새 `.cs` 셋(+ `.meta`)과 바꾼 코드는 함께 들어가야 컴파일된다.
- EC-c의 지운 파일 둘(+ `.meta`)과 에셋 셋(화약통·지뢰 외형, 설정 기본 에셋)은 코드와 한 커밋이다. 코드만 들어가면 폭발이 사라지고, 에셋만 들어가면 폭발이 두 번 난다.
- 천명님 시험 두 줄은 같은 커밋이다(없으면 시험 어셈블리 컴파일 실패).

### 4.1 2026-10-28 EC-d·검토 반영 뒤 다시 정리(HEAD `9a1d5437`, 모두 커밋 전·스테이지 안 함)

- **바뀐 사실:** 걸음 모양(ST)은 HEAD에 들어가 이제 섞이지 않는다. **소리 창구 2차(ES)는 아직 커밋 전이다.** 맵 타일 높이(TH)도 커밋 전이다.
- **소리 2차 hunk가 섞인 파일 아홉:** `ObjectVisualDataSO.cs`·`UnitVisualDataSO.cs`·`ConsumableItemVisualDataSO.cs`·`CombatPresentationSettingsDataSO.cs`·`TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`·`CombatEventSounds.cs`·`CombatDisplayTables.cs`·`CombatCharacterEventHandler.cs`. 특히 `CombatEventSounds.cs`는 EC-b가 소리 2차의 사실 갈래를 순간 갈래로 옮긴 것이라, 소리 2차만 따로 커밋하려면 중간 판을 손으로 다시 만들어야 한다. **권장: 소리 2차와 EC를 한 커밋으로.**
- **맵 타일 높이 hunk가 섞인 파일 둘:** `CombatSceneDisplay.cs`·`CombatSceneDisplayFactory.cs`. 줄이 서로 달라 hunk로 나눌 수 있다. 지운 `CombatEnvironmentEffectPresenter.cs`에 있던 TH-d 변경은 파일과 함께 사라지므로 TH 커밋에는 그 파일이 없다(어느 쪽을 먼저 커밋해도 됨).
- **Eb.1 따로 먼저(선택):** `Playback/Ordering/CombatTerrainChanges.cs`(+ `.meta`)·`CombatFactEventHandler.cs`·`CombatConcurrentGroups.cs`. 세 파일에 다른 작업 hunk는 없다(`CombatFactEventHandler.cs`에는 EC-d 주석 한 줄이 같이 있으나 동작과 무관).
- **소리 2차 + EC 한 커밋에 넣을 것(이 묶음 몫):**
  - 새 파일 셋(+ `.meta`): `Playback/Ordering/CombatEventMoment.cs`, `Playback/Support/CombatCueSources.cs`, `Playback/Support/CombatMomentCuePlayback.cs`(Eb.1을 따로 안 하면 `CombatTerrainChanges.cs`도).
  - 지운 파일 둘(+ `.meta`): `Display/Map/CombatEnvironmentEffectPresenter.cs`, `Display/Map/CombatExplosionRules.cs`.
  - DB View 코드 열둘: `CombatPresentation/` 여섯(`CombatCueEntries`·`CombatCuePoint`·`CombatCueAnchor`·`CombatPresentationCueRules`·`CombatEventCues`·`CombatPresentationEffectRules`), 외형·설정·토네이도 여섯.
  - 전투 View 코드: `CombatEventTypeTable`·`CombatEventSounds`·`CombatShownFacts`·`CombatCharacterEventHandler`·`CombatEventHandler`·`CombatCuePoints`·`CombatEffectPiece`·`CombatComposition`·`CombatPieceRunPlayer`·`CombatAttackPlayback`·`CombatDisplayTables`·`CombatSceneEventPresenter`·`CombatFieldEventPorts`·`CombatSceneEventPlayback`, 그리고 `CombatSceneDisplay`·`CombatSceneDisplayFactory`의 EC hunk.
  - 에셋 셋: `UnitVisual_PowderBarrel.asset`·`ObjectVisual_Mine.asset`·`CombatPresentationSettings_Default.asset`(EC hunk만 있음).
  - 시험: `Assets/Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs`(천명님 파일, 컴파일 맞춤).
  - 문서: 설계·이 계획·폴더 배치 안내(다른 세션 hunk도 있음)·공유 메모 넷(각 문서에 다른 작업 절도 있음)·CLAUDE.md.
- **커밋됨(2026-10-29):** 사용자가 `efa5e7d7`로 위 목록 전부와 소리 창구 2차·Eb.1 셋을 한 커밋에 넣었다(권장대로). 같은 커밋에 사용자 변경(스테이지 에셋 둘·필드 외형 설정·Boot 카메라 값·글꼴·FMOD 캐시)과 GlobalSystem `EffectScope.Stop(EffectType)`도 들어갔다. 작업 트리는 깨끗하다.

## 5. 위험

| 위험 | 대응 |
|---|---|
| 곁들임 실행이 결과 끝·기차 사건을 늦춤 | 지연 칸 툴팁에 적는다. 지금 에셋 값은 지연 0이다 |
| 큐가 하나도 안 붙어 빈 조립이 예외(검토 F1) | 내는 곳이 `IsEmpty`를 보고 건너뛴다(P17). Ub 6에서 확인한다 |
| 소리 표를 순간 칸으로 옮기다 소리가 바뀜 | 순간 종류 → 슬롯 대응은 옛 갈래를 줄마다 옮긴다. Ub 3에서 확인한다 |
| 토네이도 큐 지연이 다른 사건을 늦춤(검토 F3) | 피격·사망 큐와 같은 성질로 받아들이고 툴팁에 적는다(P19) |
| 토네이도 끝 자리를 잘못 잡아 착지 이펙트가 휩쓸리기 전 자리에 남 | Ub 4에서 확인한다 |
| 다른 세션(맵 타일·걸음 모양)과 같은 파일 | §0.2 순서대로 커밋한 뒤 착수한다 |
| 화약통 폭발이 두 번 남(에셋과 옛 길이 함께) | EC-b 확인에서는 폭발 큐를 넣지 않는다. EC-c 에셋·코드를 한 커밋으로 묶는다 |

## 6. 실행 기록

### 6.1 EC-a 값 코드 완료(2026-10-28, 컴파일 미확인, 새 시험 없음, 에셋 변경 없음, Ua 대기)

- 착수: 사용자 "파일 겹치는거 없잖아 EC-a 진행해"(§0.2 조건 중 소리 2차·걸음 모양 커밋 전에 시작). 실제로는 EC-a 값 파일 여섯이 소리 2차의 커밋 전 hunk와 같은 파일이고, 설정 SO에는 걸음 모양 ST-a hunk도 있다. 동작은 겹치지 않고, 커밋 때 hunk를 나눈다(§4).
- 한 것:
  - Ea.1 `CombatCueEntries.cs`: 이펙트 큐 입력에 `positionOffset`·`rotationOffset` 칸(툴팁: 축·월드 단위), 내부 생성자 끝 선택 인자 둘. 값 `CombatEffectCue`에 `PositionOffset`(Vector3)·`RotationOffset`(Quaternion, 생성자에서 한 번 `Euler`). 검사 없음(P1).
  - Ea.2 `CombatCuePoint.cs`: `EventCell = 6`, `ActorMuzzle` 문서를 "지점 위치 + 행위자 자세 방향"으로, `SubjectBody` 문서에 칸별 방향. `CombatCueAnchor.cs` Start·End 문서에 사실 사건·토네이도 뜻 한 줄씩.
  - Ea.3 `CombatPresentationCueRules.cs`: `CombatCueEvent` 칸 일곱(5~11, 문서 포함), `AllowsAnchor`(사실 여섯 = Start, 토네이도 = Start·End), `AllowsPoint`(오브젝트·장애물 둘·추락·약한 지반 = EventCell, 아이템·토네이도 = SubjectBody), 소리 시점 조건 = 공격 둘·사망만, 머리 문서 시점 표.
  - Ea.4 `CombatEventCues.cs`: 새 입력 `CombatVisualCueInput`(이펙트·흔들림), 묶음 문서.
  - Ea.5 `ObjectVisualDataSO.cs`: 칸 `activationCues`, Data 생성자 새 필수 인자·`ActivationCues`·검사.
  - Ea.6 `UnitVisualDataSO.cs`: 칸 `obstacleHitCues`·`obstacleDestroyedCues`, 속성 둘(읽을 때 만듦), `Validate`에 규칙 둘 + 장애물 키가 아니면 항목이 있을 때 거절(P18, Core `ObstacleContentProfile.IsNativeObstacleKey`, using `Contracts.Combat` 하나 — 이름 겹침 없음 확인).
  - Ea.7 `ConsumableItemVisualDataSO.cs`: 칸 `activationCues`, 속성, `Validate`.
  - Ea.8 `CombatPresentationSettingsDataSO.cs`: 새 머리말 "전투 연출 큐 (이펙트 창구 확장 설계 EC5)"에 `fallCues`·`weakGroundCollapseCues`, 속성 둘, `Validate` 맨 뒤(파일 규칙대로). 환경 폭발 칸은 EC-c에서 지운다.
  - Ea.9 `TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`(호진님): 머리말 "연출 큐" + 칸 `cues`(툴팁에 유닛 실행 지연 — P19), Data 생성자 새 필수 인자·`Cues`·검사.
- 계획에 없던 것 둘:
  - `CombatPresentationCueRules.Require`에 끝 선택 인자 `context`(문구 앞에 어느 값인지 붙임). 기존 호출(반응 묶음)은 그대로다.
  - `CombatEventCues.IsEmpty`(큐가 하나도 없는지). P18 검사가 쓰고, EC-b 내는 곳(Eb.11)도 쓸 수 있다. P17의 `CombatComposition.IsEmpty`(붙은 항목)와는 다른 사실이다 — 큐가 있어도 수신기가 없으면 항목은 0이다.
- 확인한 것: 새 필수 인자 둘(`ObjectVisualData`·`TornadoPresentationSettingsData`)을 부르는 곳은 각 SO의 `GetData` 하나뿐이다. 남은 시험(`Assets/Tests` 282개)은 바꾼 형식을 쓰지 않는다. 에셋은 새 칸이 없어도 기본값(빈 목록·보정 0)으로 읽힌다.
- 남는 것: `CombatCuePoints.CreatePiece`는 아직 `EventCell`을 모른다(그 값이면 "정의되지 않은 자리" 예외). 지금은 닿지 않는다 — 공격·피격·사망 칸 규칙이 `EventCell`을 거르고, 새 칸 큐는 EC-b 전까지 아무도 읽지 않는다. EC-b Eb.4에서 넣는다.
- 화면 변화 없음. Ua(§3 EC-a 확인 1~5)는 사용자가 Unity에서 한다. **Ua 문제 없음(2026-10-28 사용자).**

### 6.2 EC-b 연결 코드 완료(2026-10-28, Unity 컴파일 성공 — Editor.log, 시험·재생 미확인, 새 시험 없음, 에셋 변경 없음, Ub 대기)

- 착수: 사용자 "Ua 문제 없음, EC-b".
- 한 것:
  - Eb.1 새 `Playback/Ordering/CombatTerrainChanges.cs`(+ `.meta`, Unity가 만듦): 갈래 판정 셋 `TryGetKindChange`·`IsSameKindEntry`·`IsWeakGroundCollapse`. 사실 처리기·동시 묶음이 부르고 옛 사본 둘을 지웠다(동작 같음).
  - Eb.2 새 `Playback/Ordering/CombatEventMoment.cs`(+ `.meta`): 순간 종류 `CombatMomentKind`(일곱)·값 `CombatEventMoment`(종류·열쇠·칸 또는 주체, 형식별 읽기 속성). 형식 표 `Row<T>` 끝 선택 인자 `moment`·`TypeRow` 칸·질문 `TryGetMoment`, 채운 줄 여섯(정적 지형 원본 = 약한 지반 붕괴만, 장애물 반응, 오브젝트 발동·생성, 추락, 아이템 `Activated`만).
  - Eb.3 소리 표: 사실 갈래 여섯(장애물·오브젝트 둘·추락·약한 지반·아이템)을 `default` 한 갈래 + 순간 종류 → 소리 `Moment` 하나로. 장애물 "프리셋 없이" 규칙·슬롯 대기 값 그대로. 쓰지 않게 된 `Display.Map` using 삭제.
  - Eb.4 `CombatCuePoints`: 보정 적용 `Apply`(원본 하나), `ForSubject(start, end)`(한 인자 판은 둘이 같음)·`ForCell`, `CreatePiece`가 정해진 자리(몸·칸)에 보정을 적용하고 주체 몸은 끝(End) 시점이면 끝 자리.
  - Eb.5 `CombatEffectPiece`: 지점 둘(`AtClipPoint`·`AtClipMuzzle`)이 큐를 받아 시작 순간 지점에 `Apply`. `At`은 받은 자리 그대로.
  - Eb.6 공격 재생 `PlayImpact`: 접촉 자리에 `Apply`.
  - Eb.7 `CombatComposition`: 범위 `Accent = 3`·`ForAccent()`, 곁들임이면 `Lease`·`LeaseCameraZoom` 예외(`RequireLeasable` 하나), `IsEmpty`.
  - Eb.8 조각 실행 재생기: 문서만(막기 판정은 행동·유닛만 세고 재생 끝 판정만 모든 실행을 셈 — 코드 확인, 바꿀 줄 없음).
  - Eb.9 새 `Playback/Support/CombatCueSources.cs`(+ `.meta`): `For(moment)` 하나(오브젝트·장애물·아이템 외형, 설정 표 추락·약한 지반, 생성 = 빈 묶음). `CombatDisplayTables.CueSources`.
  - Eb.10 `CombatShownFacts.TryGetBodyCenterOf(facts)`. 옛 `TryGetBodyCenter(id)`는 이것을 부른다(찾는 규칙 하나).
  - Eb.11 새 `Playback/Support/CombatMomentCuePlayback.cs`(+ `.meta`): 순간 → 큐 값(열쇠별 보관) → 빈 큐면 끝 → 곁들임 조립 + 큐 붙이기(소리 없음) → **붙은 항목이 없으면 시작 안 함** → `TryBegin(조립, null, 세대)`. 칸 자리 = `CellSurfaceToWorld`·`RotationToWorld(R0)`(필드 밖이면 예외), 주체 자리 = 보인 사실 몸 가운데·서 있는 방향.
  - Eb.12 사건 Presenter: 생성자 새 필수 인자 `cueSources`(`sounds` 다음), 내는 곳을 조각 실행 재생기 옆에서 만들고 `PresentEvent`의 `_sound.Play(e)` 바로 뒤 한 줄.
  - Eb.13 표시 Factory: Presenter 생성 줄에 `tables.CueSources`(민형님 공유는 EC-d).
  - Eb.14 `TryBeginTornado`(호진님 줄): 들어 올림 항목 번호를 받아 `End` 시점에 잇고 큐 붙이기 한 줄, 서 있는 몸 자리 도우미 `StandingBodyOf`(시작 = 보인 사실, 끝 = 도착 사실).
  - Eb.15 천명님 `PullAndMarkPresentationTests`: 정적 칸 `s_cueSources` 하나 + Presenter 생성 인자 한 줄(P14 앞 절반, 단언 그대로).
- 계획과 다른 점 둘:
  - `CombatTerrainChanges`를 계획 P8의 `Playback/Support/`가 아니라 `Playback/Ordering/`에 뒀다. 폴더 배치 안내가 "사건 형식마다의 지식"을 Ordering에 두고, 형식 표·동시 묶음(Ordering)이 Support를 부르지 않게(지금 Ordering → Support 의존은 0) 하려는 것이다. 이름·내용은 계획대로다.
  - Eb.1에서 소리 표의 약한 지반 판정을 이 함수로 바꾸지 않았다. 소리 표는 Eb.3에서 순간 칸을 읽게 되어 직접 부를 일이 없다(P11 "소리 표는 순간 값을 읽으므로 직접 부르지 않음"과 같은 결과). 그래서 Eb.1 커밋은 새 파일 + 사실 처리기 + 동시 묶음 셋만으로 닫힌다.
- 확인한 것:
  - Unity 에디터가 새 `.cs` 넷의 `.meta`를 만들었고, 마지막 컴파일(시험 고친 뒤)이 오류 없이 끝났다(Editor.log). 중간에 시험 어셈블리 CS7036(Presenter 새 인자)이 났다가 Eb.15로 풀렸다. 시험 실행·재생은 하지 않았다.
  - 남은 시험에 바꾼 서명(`AtClipPoint`·`CombatCuePoints`·`CombatEventSounds.For`·Presenter 생성자·범위 열거)을 쓰는 곳은 위 천명님 시험 하나뿐이다.
- 알아 둘 것:
  - 장애물 반응의 칸은 피해 근원의 물리 칸이라 비어 있을 수 있다. 비면 소리만 나고 사건 칸 이펙트는 넣지 않는다. 옛 환경 폭발은 화약통 파괴에 칸이 없으면 예외였다 — EC-c에서 폭발을 큐로 옮기면 이 경우가 "예외"에서 "이펙트 없음"으로 바뀐다(기록만, EC-c 보고에 다시 적음).
  - 소리 결과는 옛 갈래와 줄마다 같다(장애물 반응 `None`은 Core가 만들지 않아 갈래 차이 없음).
- 커밋 묶음(§4):
  - Eb.1 따로: `Playback/Ordering/CombatTerrainChanges.cs`(+ `.meta`), `CombatFactEventHandler.cs`, `CombatConcurrentGroups.cs` — 세 파일 모두 다른 작업의 커밋 전 hunk가 없다.
  - 그 밖: 새 `.cs` 셋(+ `.meta`)과 바꾼 파일. `CombatEventSounds.cs`·`CombatDisplayTables.cs`·`CombatCharacterEventHandler.cs`에는 소리 2차 hunk, `CombatSceneEventPresenter.cs`에는 걸음 모양 ST-b(스테이지됨) hunk, `CombatSceneDisplayFactory.cs`에는 다른 세션(맵 타일 높이) hunk가 섞여 있다.
- 화면 변화 없음(새 큐 칸이 모두 비어 있음). Ub(§3 EC-b 확인 1~8)는 사용자가 Unity에서 한다. **Ub 문제 없음(2026-10-28 사용자).**

### 6.3 EC-c 폭발 옮기기 코드·에셋 완료(2026-10-28, Unity 컴파일 성공 — Editor.log, 시험·재생 미확인, 새 시험 없음, Uc 대기)

- 착수: 사용자 "Ub 문제 없음, EC-c".
- 한 것:
  - Ec.1 에셋: 화약통 `UnitVisual_PowderBarrel.asset`의 `obstacleDestroyedCues`, 지뢰 `ObjectVisual_Mine.asset`의 `activationCues`에 폭발 큐 하나씩(P12 — Boom·Start·지연 0·EventCell·위치 보정 0·회전 보정 (−90,0,0)). 화약통 `obstacleHitCues`는 빈 묶음으로 적었다(피해만 받고 부서지지 않을 때는 이펙트 없음, 옛 동작과 같음).
  - Ec.2 사건 Presenter: 환경 폭발 필드·호출 한 줄 삭제, `PresentEvent` 문서를 "소리 → 순간 큐 → 사실 반영"으로, 순간 큐 줄 주석에 화약통·지뢰 폭발(EC9). 필드 창구: 칸 `presentEnvironmentEffect`(둘째 위치 인자)·속성 삭제, 요약에 "폭발은 외형의 순간 큐로" 한 줄.
  - Ec.3 재생 조정 `CombatSceneEventPlayback`: 생성자 인자·필드 삭제, 결과 취소는 `RunAll(CancelPendingEvents, 사건 표시 취소, 기차 취소, 가시 정리)`. 씬 표시 `CombatSceneDisplay`: 필드·생성자 인자·`Bind`·필드 연결 정리(`Clear`)·해제(`Release`) 줄 삭제. 표시 Factory: 환경 폭발 Presenter 생성 두 줄, 필드 창구 이름 인자, 재생 조정·씬 표시 생성 인자 삭제(민형님 공유는 EC-d). 이펙트 회수는 이미 이펙트 창구가 맡는다(결과 취소 = 사건 Presenter 취소 → `StopAll`, 해제 = `Release`).
  - Ec.4 `Display/Map/CombatEnvironmentEffectPresenter.cs`·`CombatExplosionRules.cs`(+ `.meta`) 삭제(스테이지하지 않음).
  - Ec.5 설정 표: "환경 연출" 머리말·`environmentExplosionEffect` 칸·`EnvironmentExplosionEffect` 속성·검사 두 줄 삭제, 쓰지 않게 된 `Render.Table.Effect.Enum` using 삭제. 기본 에셋 `environmentExplosionEffect: 2` 줄 삭제(P13).
  - Ec.6 천명님 `PullAndMarkPresentationTests.cs:424`: 필드 창구 생성의 둘째 인자 `_ => { }` 하나 삭제(P14 뒤 절반, 단언 그대로).
- 계획과 다른 점 없음.
- 확인한 것:
  - Unity 마지막 컴파일이 오류 없이 끝났다(Editor.log `Tundra build success`). 중간 오류(표시 Factory 이름 인자·재생 조정 인자 수, 시험의 `() => false`를 `Action` 자리에 넣은 CS0201)는 고치는 도중의 것이고 마지막 빌드에는 없다. 시험 실행·재생은 하지 않았다.
  - View 코드에 옛 이름(`CombatEnvironmentEffectPresenter`·`CombatExplosionRules`·`EnvironmentExplosionEffect`·`PresentEnvironmentEffect`)이 남은 곳은 없다(Core `HeldEnvironmentEffects`는 무관). 문서의 옛 이름은 EC-d Ed.4에서 정리한다.
- 알아 둘 것(보고):
  - **화약통 파괴 사건에 칸이 없을 때:** 옛 길은 예외(F1)였고, 이제는 이펙트 없이 소리만 난다(EC-b §6.2 기록과 같음). 다만 Core는 장애물 반응 사건의 칸을 늘 장애물 앵커로 채운다(`ObstacleReactionCalculator.cs:61` `new CombatDamageOrigin(before.Anchor, …)`). 그래서 지금 코드에서는 이 갈래에 닿지 않는다.
  - **높이:** 지운 `CombatEnvironmentEffectPresenter.cs`에는 맵 타일 높이 TH-d의 커밋 전 변경(폭발 자리 → 칸 표면)이 있었다. 파일과 함께 사라지지만, 큐의 `EventCell` 자리가 `CellSurfaceToWorld`(타일 바탕 + 지형)라 높이 규칙은 같다(타일 높이 계획 §8 TH-d 기록이 미리 적어 둔 대로).
  - **자리·방향은 옛 길과 같다(코드 대조):** 옛 길은 `CellsRoot` 아래 로컬 (칸 표면, 회전 (−90,0,0))이었다. 큐는 월드 (칸 표면, `CellsRoot.rotation` × R0) 위에 보정 (−90,0,0)을 곱하고(`CombatCuePoints.Apply`), 이펙트 창구가 같은 `CellsRoot`의 로컬로 되돌린다(`CombatEffectWindow.Play`). 맵퍼의 필드 루트가 `CellsRoot`라 로컬 회전은 다시 (−90,0,0)이다.
- 커밋 묶음(§4): 지운 파일 둘(+ `.meta`)·에셋 셋·바꾼 코드는 한 커밋. `CombatSceneDisplay.cs`·`CombatSceneDisplayFactory.cs`에는 맵 타일 높이 hunk가, `CombatPresentationSettingsDataSO.cs`에는 소리 2차·걸음 모양 ST-a·EC-a hunk가 섞여 있다. 에셋 셋(화약통·지뢰 외형, 설정 기본 에셋)에는 EC-c hunk만 있다.
- Uc(§3 EC-c 확인 1~4)는 사용자가 Unity에서 한다. **Uc 문제 없음(2026-10-28 사용자).**

### 6.4 EC-d 문서·공유·묶음 끝 검토(2026-10-28, 주석 넷 외 코드 변경 없음)

- 착수: 사용자 "Uc 문제 없음, EC-d".
- 한 것:
  - Ed.1 호진님 설정 표 메모 **§10**(§9는 걸음 모양이 먼저 씀): 설정 표 큐 칸 둘·환경 폭발 칸 삭제, 토네이도 설정 `cues`, `TryBeginTornado` 한 자리, 외형 SO 큐 칸 참고.
  - Ed.2 Core 전달 문서 **§48**: 천명님 시험 두 곳(Presenter 새 인자 = 정적 칸 + 인자, 필드 창구 인자 삭제), 장애물 반응 칸을 Core가 늘 채우는 것에 기댐.
  - Ed.3 민형님 Factory 공유 §36: Presenter 인자 한 줄, 환경 폭발 줄 삭제, 민형님 커밋 `e18c8313`에서 마지막으로 바뀐 필드 연결 정리 줄 하나.
  - Ed.3b 현강님 사건 소리 메모 §9: 소리 표 사실 갈래 → 형식 표 순간 칸, 결과 같음.
  - Ed.4 폴더 배치 안내: 갱신 줄, 트리 둘, Map 줄 "환경 효과" 삭제, Ordering·Support 줄에 새 파일 넷, 사건 형식 지식 줄(순간 칸 규칙, 옛 "폭발 칸" 삭제), 연출 덧붙임 줄(보정 적용·순간 큐·빈 조립). 주석 넷: `CombatPresentationEffectRules` 요약, 처리기 문서 셋("소리·환경 효과" → "소리·순간 큐", `CombatEventHandler` 둘·`CombatFactEventHandler` 하나). `CombatFieldEventPorts` 요약·사건 Presenter 주석은 EC-c에서 고침.
  - Ed.5 설계 구현 메모 §8, 이 기록.
  - Ed.6 이펙트 안내(사운드 작업자 안내 꼴)는 쓰지 않았다 — 필요한지 사용자에게 묻는다.
  - 모든 공유 메모는 미전달이다.
- **Ed.7 묶음 끝 검토(새 에이전트 하나, 7기준, 감점식): 약 9.7**(SSOT 9.25·SOLID 9.75·FW 10·독립성 9.25·캡슐화 9.75·C#/Unity 10·If/Tick 10). 설계 §7 예상 약 9.6과 같은 방식이고 거의 같다. 설계 §6 예상 8.6은 "알려진 문제 기준"이라 비교하지 않는다. 높음 0·중간 1·낮음 5.
  - **M1(중간, 독립성, 결함 — 지금 데이터에서는 나지 않음, 코드로 다시 확인함):** 토네이도 큐에 지연을 주면 착지한 유닛이 휩쓸리기 전 칸으로 되돌아가 보인다. 들어 올림이 끝날 때 지연 있는 큐가 남아 있으면 실행은 완료만 먼저 알리고 자세 대여를 쥔 채로 둔다(`CombatPieceRun.CompleteIfDone`). 완료 콜백의 그리기는 대여 중이라 위치를 쓰지 않고(`FieldUnitView.cs:147`), 실행이 끝나면 `RestorePose`가 대여 시작 때 위치로 되돌린다. 다음 사실 그리기까지 그 칸에 남는다. 지연 0이면 큐 조각이 그 자리에서 끝나 문제가 없다. 지금 에셋은 큐가 비어 있다. 권장: 큐 규칙에서 토네이도 칸 이펙트·흔들림 지연을 0만 허용(검토 F3도 함께 사라짐).
  - L1(SSOT): 서 있는 유닛 몸 자리 규칙 두 벌(`CombatCharacterEventHandler.StandingBodyOf`·`CombatMomentCuePlayback.SubjectPoseOf`). 권장: `CombatShownFacts` 함수 하나.
  - L2(SSOT): 큐 입력 칸 `?? throw` → `ToCues()` 꼴 다섯 곳.
  - L3(OCP): 순간 종류 하나를 더하면 고칠 곳이 여섯 곳이 넘고(열거 둘·규칙 둘·SO 칸·큐 값 답·소리 표·형식 표 줄), 두 switch는 런타임 예외뿐이다. 폴더 안내의 "값 하나를 더한다"는 작업량보다 적게 쓴다.
  - L4(SSOT): 아이템 큐 방향이 설계 EC2·`CombatCuePoint.cs:20`은 "소지자 모델 방향", 코드는 "서 있는 방향(보인 사실)".
  - L5(캡슐화): 큐 값 보관이 외형마다 다름(오브젝트·토네이도 = 생성 때 불변, 유닛·아이템·설정 = 읽을 때마다 새로). `CombatCueSources.cs:18` 문서가 일부만 맞다.
  - 문서와 코드가 다른 곳: 설계 §3 표·이 계획 P8·P11·Eb.1의 `CombatTerrainChanges` 자리(Support → 실제 Ordering), 설계 §8 "칸 없는 장애물 반응은 소리만"(흔들림 큐는 남), 설계 §6 SOLID 근거(재생기 코드 갈래는 늘지 않음), 설계 §1.6·P14 "시험 두 줄"(실제는 정적 칸 하나를 더해 세 줄).
  - 맞다고 확인된 것: 곁들임 범위의 막기·대여·완료·빈 조립 막기(옛 F1 풀림), 보정 적용 한 곳(부르는 곳 셋), 순간 일곱 종류의 형식 표·소리·큐 대응과 옛 소리 결과, 폭발 옮기기의 시점·부모·로컬 회전·회수·에셋 값, 옛 이름 참조 없음, P18·P6·세대, 새 파일 자리.
- 반영은 사용자 결정 대기다. → **권장대로 반영(2026-10-28 사용자, §6.5).**

### 6.5 묶음 끝 검토 권장 반영(2026-10-28 사용자 "권장대로 반영", 새 시험 없음, 에셋 변경 없음)

- 반영한 것(설계 §8.1):
  - **M1:** `CombatPresentationCueRules`에 규칙 문구 `TornadoNoDelayRule`과 `FindError`의 두 줄(토네이도 칸의 이펙트·흔들림 지연이 0이 아니면 거절), 머리 문서 한 줄. 토네이도 설정 SO `cues` 툴팁을 "지연은 0만"으로, `TryBeginTornado` 주석 한 줄(호진님 줄 옆). 이 규칙은 토네이도 Data 생성(부트 DB)에서 걸린다. 지금 에셋은 토네이도 큐가 비어 있어 걸리지 않는다.
  - **L1:** `CombatShownFacts.StandingBodyOf(facts)` 하나. `CombatCharacterEventHandler`의 private `StandingBodyOf`와 `CombatMomentCuePlayback`의 private `SubjectPoseOf`를 지우고 이것을 부른다(동작 같음). 쓰지 않게 된 `Contracts.Ids` using 하나 삭제.
  - **L4:** `CombatCuePoint.SubjectBody` 문서, 설계 EC2 표.
  - **L5:** `CombatCueSources` 머리 문서.
  - **L3:** 폴더 배치 안내의 사건 형식 지식 줄에 고칠 곳 여섯.
  - 문서 불일치: 설계 §3 표·§6 SOLID 근거·§8 장애물 문장·EC3·EC8, 이 계획 P8·P11·Eb.1(자리 Ordering)·P19(지연 0만).
- 반영하지 않은 것: L2(입력 칸 null 검사 다섯 곳, 한 줄씩이고 문구가 달라 도우미 이득 없음).
- 계획과 다른 점: P19가 바뀜(지연 툴팁 → 지연 0만). 사용자 "권장대로 반영"으로 정했다.
- 확인: 바꾼 파일 뒤의 Unity 빌드가 오류 없이 끝났다(Editor.log, `ProjectL.Systems.dll`이 마지막 수정보다 뒤). 시험 실행은 하지 않았다. 재생 확인(Ud): ① Boot DB 생성. ② 토네이도 설정 `cues`에 지연 0인 End 이펙트를 넣으면 착지 순간 이펙트가 나고 유닛이 착지 칸에 그대로 남는다. ③ 같은 항목의 지연을 0.5로 바꾸면 부트가 "토네이도 칸의 지연은 0이어야 합니다"로 멈춘다(확인 뒤 되돌림). ④ 아이템 발동·토네이도 큐가 지금과 같은 자리에 난다(L1, 동작 같음). **Ud 1~4 문제 없음(2026-10-29 사용자 "확인했고").**

## 7. 7기준 검토 반영(2026-10-28, 코드 착수 전)

- 사용자 "검토"(7기준) → 새 에이전트 하나가 설계·계획·코드를 대조했다. 지금 9.5, 계획 그대로 9.1, 권장 반영 뒤 약 9.6. 지적·반영 표는 설계 §7.
- 사용자 "권장대로 반영"으로 이 계획에서 바꾼 것:
  - P3(보정 적용 자리 하나), P6(연결마다 보관), P8(새 파일 넷, 큐 표 대신 순간 값), P11(부르는 곳·커밋 분리).
  - 새로 P15(형식 표 순간 칸), P16(소리 표 옮김), P17(빈 조립 막기), P18(장애물 키 검사), P19(토네이도 끝 자리·지연 툴팁).
  - Task: Ea.1·Ea.6·Ea.9 툴팁·검사, EC-b를 Eb.1~Eb.15로 다시 짬(분류 먼저, 순간 칸·소리 표, `IsEmpty`, `CombatShownFacts`), Ed.3b·Ed.4 정리 목록, Ua 4·Ub 3·6·7.
  - 커밋: Eb.1 따로 먼저(§4). 위험 셋 추가(§5).
- 반영하지 않은 것: 값 답 둘 합치기(설계 §7.4).
- 결정(2026-10-28 사용자 "모두 그대로"): P1~P19 모두 쓴 그대로.
- 착수 조건 확인(2026-10-28, git):
  - 맵 타일 TA-b: 풀림. `CombatSceneDisplayFactory`는 `407714d7`에 들어갔고 지금 변경이 없다. 맵 타일의 남은 변경은 이 계획 파일과 겹치지 않는다.
  - 소리 2차: 커밋 전. 외형 SO 셋·토네이도 설정·`CombatDisplayTables`·`CombatEventSounds`·`CombatCharacterEventHandler`·`CombatSoundPresets`가 바뀐 채다.
  - 걸음 모양 ST-b: 커밋 전. 사건 Presenter 생성자 줄·`CombatEventPresentationValues`가 겹친다. 설정 SO에는 두 작업의 변경이 섞여 있다.
  - 이 둘이 커밋된 뒤 EC-a를 시작한다.
