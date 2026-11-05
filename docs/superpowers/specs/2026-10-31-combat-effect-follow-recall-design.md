# 전투 몸 자리 이펙트 따라가기·회수 설계 (2026-10-31)

상태: **FE-a(따라가기)·FE-b(켜고 꺼지는 특성의 회수, 2026-10-31 사용자 "FE-b 진행" = P7~P10 그대로) 코드 완료 — 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기(§2.5·§3.5).**

**대신함(2026-11-01, [이펙트 구조 2판](2026-10-31-combat-effect-lifetime-design.md) EL-b):** FE4(발동 줄 `keepWhileStatus`·`recallKinds`)·FE6(받침의 쥠 규칙·발동 짝 회수)·FE10(결과 끝 다시 내기)은 걷었습니다. 상태 동안 이펙트는 상태 외형 `whileEffect`·`restartOn`, 보유 동안은 특성 외형 `alwaysEffects`가 맡고, 유지 표시 부품이 보인 사실을 그릴 때 맞춥니다(EL2·EL3·EL4). 따라가기(FE1~FE3)·숨김/사라짐(FE11)·링 버퍼 회수(FE12)는 그대로입니다.

이 설계는 [클립 이펙트 설계](2026-10-19-combat-clip-effect-design.md)의 결정 Q-E1 A("놓고 따라가지 않음")를 몸 자리에 한해 바꾼다. 다른 자리(클립 이펙트 지점·발사 지점·사건 칸·투사체 접촉)는 그대로 놓기만 한다.

## 0. 요청과 결정

- 사용자 보고(2026-10-31): "지금 유닛 위치에 생긴 이펙트가 유닛의 이동을 안 따라가네".
- 결정(2026-10-31 사용자):
  - **Q-FE1 큐마다 칸**: 이펙트 큐 항목에 "남기기" 칸. 꺼짐(기본) = 몸 자리(ActorBody·SubjectBody) 이펙트가 유닛을 따라감.
  - **Q-FE2 위치만**: 위치만 따라가고 회전은 낼 때 그대로.
  - **Q-FE3 바로 멈춤**: 따라가던 유닛 표시가 사라지면(사망 뒤 제거·풀 반환) 이펙트도 바로 멈춤.
  - Q-FE1에 덧붙임: "단독 행동처럼 켜고 꺼지는 특성은 그에 따른 이펙트 회수도 필요함" → §3(FE-b).
  - **Q-FE4 회수 신호 = 둘 다**(상태 자동 + 발동 짝). 사용자 질문 "상태 자동이면 다 되는 것 아닌가, 턴이 지나 버프가 사라지는 것까지 생각하면" → 답은 §3.1.
  - **Q-FE5 회수 모양 = 자연스럽게**(방출만 멈추고 남은 입자는 수명대로).
  - **Q-FE6 중복 = 하나만 유지**(같은 줄 이펙트가 그 유닛에 켜져 있으면 새로 내지 않음).

## 1. 확인한 사실 (코드 대조)

- 이펙트 창구 `CombatEffectWindow`는 Framework `EffectScope` 하나로 이펙트를 필드 루트(`FieldView.CellsRoot`)에 놓았다. 몸 자리 위치는 조립을 만들 때 보인 사실(`CombatShownFacts.StandingBodyOf`)로 정해, 그 뒤 유닛이 움직여도 이펙트는 남았다.
- GlobalSystem `EffectScope`(민형님)는 `Play`가 손잡이를 돌려주지 않고, 회수는 `StopAll`과 `Stop(EffectType)`(그 종류의 첫 하나)뿐이다. 이펙트는 요청의 `TargetParent` 아래에 놓이고, 파티클이 멈추면 `ParticleStopNotifier`의 알림으로 Scope가 풀에 돌린다(부모를 풀 루트로 되돌림). `StopImmediately`는 알림을 지우고 멈춰 Scope 목록에 남는다.
- 유닛 아래에 바로 붙이면 유닛 표시가 꺼지거나 파괴될 때 이펙트가 같이 꺼지거나 파괴된다(클립 이펙트 설계 E-R1, 지금도 맞음).
- 몸 위치를 매 프레임 따라가는 읽기가 이미 있다: `FieldUnitPresenter.TrackBody(id)`(몸 글자가 씀, 그 표시의 대여 수명을 붙잡아 표시가 바뀌거나 꺼지면 null).
- 단독 행동은 켜질 때 `ShieldGranted(210)`, 꺼질 때 `ShieldRemoved(230)`, 턴 끝 갱신 때 `ShieldReset(240)` 발동 사실을 낸다(`TraitPresenceReconciler`). 발동 종류에는 짝이 더 있다(`StatusApplied 160`·`StatusConsumed 170`·`StatusRemoved 180`·`StatusExpired 190`, `TrailCreated 260`·`TrailRemoved 280` 등).
- Core는 이제 걸린 상태를 공통 목록으로 준다: `CharacterCombatFacts.StatusEffects.Entries`(종류 `Key`, 출처 `Origin` = 특성 키·주인 유닛), 화면 읽기는 `StatusEffectReadModel.Capture`(HUD 상태 줄이 씀). 보호막은 출처가 남아도 `Eligible`이 꺼질 수 있고, HUD는 남은 보호막 0이면 숨긴다(`CombatUnitStatuses.AddTo`).
- 공유 반응 에셋 `CombatUnitReaction_Default`의 피격·사망 이펙트(효과 4)는 SubjectBody다.

## 2. FE-a 따라가기 (코드 완료)

### FE1 큐 칸과 판단 원본

- 입력 `CombatEffectCueEntry.stayInPlace`(bool, 기본 꺼짐), 값 `CombatEffectCue.StaysInPlace`.
- "따라가나" 판단의 원본은 `CombatEffectCue.FollowsUnit` 하나: 남기기 꺼짐 + 자리가 ActorBody·SubjectBody.
- 큐 자리 해석 `CombatCuePoints`가 몸 자리를 그 몸의 유닛과 함께 받는다(새 `CombatCuePoints.Body`). `ForSubject(subject, …)`·`ForActivation(…, IReadOnlyList<Body>, …)`로 서명이 바뀌었고, 호출처는 피격·사망·토네이도(`CombatCharacterEventHandler`), 순간 큐(`CombatMomentCuePlayback`), 상태 뒤 특성 피드백(`CombatTraitFeedbackPlayback`)이다. 발동 사실 하나의 자리(`ForActivation(trait, …)`)는 대상 ID를 그대로 붙인다.
- 따라가는 큐는 조각 `CombatEffectPiece.Following(…, unit, at)`이 되고, 아니면 지금처럼 `At`이다.

### FE2 따라가기 받침

- 새 `Presentation/Effects/CombatEffectFollower.cs`(+ `.meta`, MonoBehaviour): 이펙트 하나를 자식으로 품고 `LateUpdate`에서 몸 위치로 옮긴다. 위치만 옮기고 회전은 부모 기준 그대로다(Q-FE2).
- 이펙트 창구 `PlayFollowing(type, unit, 위치, 회전)`: 필드 루트 아래 빈 받침을 빌려(없으면 만듦) 지금 몸 위치에 두고, 이펙트를 받침 아래에 낸다. 낸 자리와 몸 위치의 차이(큐 위치 보정 포함)는 그대로 유지된다.
- 받침은 이펙트가 풀로 돌아가 비면(자식 없음) 스스로 꺼지고 다시 빌릴 수 있다. 창구 `Release`는 `StopAll`로 이펙트를 먼저 풀로 돌린 뒤 받침을 지운다.
- 창구 생성 인자에 몸 위치 읽기(`Func<EntityId, Func<Vector3?>>`)가 더해졌다. Factory 연결 줄 둘: `CombatSceneDisplayFactory`(`units.TrackBody`)·`CombatTraitPreviewFactory`(같음).

### FE3 표시가 사라지면 바로 멈춤

- 받침이 읽은 몸 위치가 null이면(표시가 풀로 돌아가거나 꺼짐) 자식 파티클을 `Stop(방출 멈춤 + 지우기)`한다. 이펙트 시스템의 멈춤 알림으로 Scope가 풀에 돌린다.
- 2026-10-31 FE11로 고침: "사라짐"(같은 표시가 그 유닛에 더 붙어 있지 않음 — 풀 반환·다른 유닛에 붙음·파괴)일 때만 바로 멈춘다. "숨음"(같은 표시가 꺼졌을 뿐 — 배치 끌기의 원래 유닛 숨김)은 FE11.

### FE11 숨으면 같이 숨기 (2026-10-31, 사용자 "그렇게 고쳐")

- 까닭: 배치 끌기에서 원래 유닛이 숨으면 따라가던 이펙트가 FE3대로 멈췄고, 이를 메우려던 "원래 유닛을 다시 보일 때 다시 내기"(옛 FE10 일부)는 끌기를 놓는 순간(명령 제출 직후·결과 전) 옮기기 전 사실(보호막 켜짐)로 이펙트를 새로 재생했다. 그래서 단독 행동이 깨지는 자리로 옮겨도 보호막 이펙트가 처음부터 나왔다가 230으로 사라졌다(2026-10-31 사용자 보고 "단독 행동이 발동하지 않는 위치로 옮겨도 보호막이 발동").
- 몸 읽기를 `FieldUnitPresenter.TrackBodyPresence(id)`(같은 표시가 그 유닛에 붙어 있나 + 몸 위치, 숨었으면 위치 null)로 나눈다. 몸 글자가 쓰는 `TrackBody`는 이 읽기 위에서 옛 동작 그대로다(보이지 않으면 null).
- 받침: 붙어 있지 않음 → 바로 멈춤(FE3). 붙어 있고 위치 없음(숨음) → 이펙트는 그대로 두고 그리기(`Renderer.enabled`)만 끈다. 다시 보이면 켠다. 숨긴 채 이펙트가 풀로 돌아가면(끝남·`StopAll`·창구 해제) 받침이 그리기를 되돌린다(숨긴 그리기가 풀의 이펙트에 남지 않게).
- 그래서 끌기를 놓으면: 혼자가 아닌 자리 → 있던 이펙트가 결과의 230·상태 사건으로 회수돼 사라짐(새로 재생 없음), 혼자인 자리 → 그대로 이어짐, 끌기 취소 → 그 자리에서 다시 보임. 끌기 복구 때 다시 내기는 걷었다.

### 2.4 정한 것 (확인 대상)

- P1 낼 자리는 지금과 같다(보인 사실의 몸 자리 + 보정). 따라가기는 낸 뒤의 움직임만 더한다.
- P2 낼 때 그 유닛 표시가 없으면 그 자리에 놓기만 한다(지금 동작).
- P3 바로 멈춤은 Unity 파티클 API(`ParticleSystem.Stop`)를 받침이 직접 부른다. GlobalSystem 공개 메서드에 "알림을 남긴 채 즉시 멈춤"이 없어서다(GlobalSystem 무변경). Unity가 지운 뒤 멈춤 알림을 보내지 않으면 그 이펙트는 다음 `StopAll`까지 Scope에 남는다 — Unity 확인 ④.
- P4 공유 반응 에셋의 피격·사망 이펙트 두 줄은 `stayInPlace: 1`로 바꿔 지금 모습을 지켰다. 따라가게 두면 사망 이펙트가 죽은 유닛 표시가 사라질 때 끊기고, 피격 불꽃이 넉백을 따라 미끄러진다. 따라가게 하려면 칸을 끄면 된다.
- P5 받침은 결과 재생 Tick이 아니라 `LateUpdate`로 옮긴다. 결과 재생이 끝난 뒤에도 남은 이펙트가 따라가야 해서다(몸 글자가 `Canvas.willRenderCanvases`를 쓰는 까닭과 같음).
- P6 특성 외형 에셋(작업 트리의 사용자 편집분 포함)의 몸 자리 이펙트는 기본값 그대로 따라간다.

### 2.5 Unity 확인

1. 특성 발동 이펙트(몸 자리)가 걷는 유닛을 따라간다. 유닛이 돌아도 이펙트는 돌지 않는다.
2. 큐의 Stay In Place를 켜면 지금처럼 그 자리에 남는다.
3. 피격·사망 이펙트(공유 반응 에셋)는 지금과 같다(그 자리에 남음).
4. 따라가던 유닛이 죽어 표시가 사라지거나 손패로 돌아가면 이펙트가 바로 사라지고, 그 뒤 같은 이펙트가 다시 정상으로 나온다(풀 반환 확인).
5. 클립 이펙트 지점·발사 지점·칸·투사체 접촉 이펙트는 지금과 같다.
6. 특성 연출 툴(`CombatTraitPreviewFactory` 조립)에서도 같다.

## 3. FE-b 켜고 꺼지는 특성의 이펙트 회수 (코드 완료)

### 3.1 상태 자동이면 다 되나 (사용자 질문의 답)

거의 된다. Core가 걸린 상태를 공통 목록(`StatusEffects`)에 출처(특성 키·주인 유닛)와 함께 두므로, "그 유닛에 이 특성 출처의 상태가 남아 있나"만 보면 사라진 까닭(턴 지남 만료·소비·정리·되돌리기·사망·보호막 제거)과 상관없이 한 규칙으로 회수된다. 발동 짝으로만 하면 까닭마다 짝(190 만료·170 소비·180 제거·230 보호막 제거 …)을 다 적어야 한다.

상태 자동으로 안 되는 것이 셋 있어 발동 짝을 보조로 둔다(Q-FE4 둘 다):

- 공통 상태 목록 밖의 특성 효과: 협동 방어 보너스(`TraitEffects.CohesionDefenseBonus`, 출처 없음), 가속 보유분(`AccelerationTraitRule.Held`, 능력치에서 계산), 사냥꾼 표적 같은 특성 런타임 상태.
- 상태가 아닌 발동: 흔적 생성·제거(260·280), 이동 변경 등.
- 출처가 남아도 꺼진 상태: 보호막의 `Eligible` 꺼짐·남은 양 0 → 이것은 "켜짐" 판정을 HUD와 같은 규칙으로 하면 상태 자동에 들어간다(FE5).

### 3.2 설계

- **FE4 발동 줄 칸**(`TraitActivationVisual`): `keepWhileStatus`(bool, 기본 꺼짐 = 지금처럼 한 번 내고 끝) — 켜면 그 줄의 몸 자리 이펙트는 "그 유닛에 이 특성 출처의 켜진 상태가 있는 동안" 유지된다. `recallKinds`(발동 종류 목록, 비어도 됨) — 같은 특성의 이 종류 발동이 그 유닛에 오면 회수한다. 둘 중 먼저 오는 쪽으로 회수한다. 이 칸을 쓰는 이펙트는 고리(Loop) 파티클이어야 오래 남는다(아니면 스스로 끝남).
- **FE5 켜진 상태 판정 한 곳**: 유닛 사실의 공통 상태 중 출처가 (그 특성 키, 발동 유닛)이고, 보호막이면 `Eligible`이고 남은 양이 있는 것. 구현 때 HUD는 고치지 않았다: HUD 숨김(`CombatUnitStatuses.AddTo`)은 Core가 합친 값(`ActiveShieldTotal` = 0)을 보고, 이 판정은 출처 하나씩 같은 뜻(켜진 출처가 없음 = 합계 0)으로 본다. 출처별 규칙의 원본은 `CombatTraitStatusPresence.IsOn` 하나다.
- **FE6 손잡이**: 유지 이펙트는 남기기 여부와 상관없이 받침에 낸다(따라가지 않는 것은 위치 읽기 없는 받침). 받침이 열쇠(특성 키, 발동 유닛, 줄의 발동 종류, 붙은 유닛)를 가진다. 같은 열쇠가 켜져 있으면 새로 내지 않는다(Q-FE6).
- **FE7 회수 모양**: `ParticleStopNotifier.StopEmitting()`(GlobalSystem 공개 메서드, 방출만 멈추고 남은 입자는 수명대로, 다 사라지면 알림으로 풀 반환, Q-FE5). 붙은 유닛 표시가 사라지면 FE3대로 바로 멈춤.
- **FE12 링 버퍼 이펙트는 바로 지움**(2026-10-31, 사용자 보고 "이동으로 단독 행동 배리어 버프가 꺼졌는데 배리어가 안 사라짐"): 배리어 프리팹 `Shild.prefab`은 Looping 꺼짐·입자 1개 터뜨림·Ring Buffer = Pause Until Replaced다. 링 버퍼 입자는 수명이 끝나도 죽지 않고 대체될 때까지 멈춰 남아(그래서 반복 없이도 계속 보임), 방출만 멈추는 FE7로는 사라지지 않고 이펙트 시스템의 멈춤 알림도 오지 않는다. 그래서 이펙트 안 파티클 중 링 버퍼를 쓰는 것이 있으면 회수 때 방출 멈춤 + 지우기로 바로 지운다(받침 `Recall`·`UsesRingBuffer`). 링 버퍼가 아니면 지금처럼 자연스럽게.
- **FE8 판정 시점**: 받침이 `LateUpdate`에서 보인 사실(`CombatShownFacts.FindCharacter`, 결과 재생 중에는 사건 시점의 사실, 끝난 뒤에는 최종 사실)을 읽는다. 상태 사건이 보이는 순간 회수돼 HUD 아이콘이 꺼지는 때와 맞는다. 발동 짝은 특성 피드백이 그 발동을 재생할 때 열쇠로 찾아 회수한다.
- **FE9 처음 켜지기 전**: 발동 시작(ActivationStart) 줄은 상태 사건보다 먼저 이펙트를 낸다. 그래서 상태를 한 번 본 뒤부터 "없어지면 회수"를 센다. 그 결과가 끝날 때까지 한 번도 못 보면 회수한다.
- **FE10 빠진 유지 이펙트 다시 내기**(2026-10-31 보완, 사용자 "보완 진행"): 유지 이펙트를 발동이 올 때만 내면 발동이 다시 오지 않는 경우에 이펙트가 돌아오지 않는다 — 다 깎였다가 턴 끝 재충전(`ShieldReset` 240만 옴), 되돌리기로 상태가 돌아옴. 그래서 결과 끝 최종 사실에서 "상태가 켜져 있는데 유지 이펙트가 없는 보이는 유닛"에 그 특성의 유지 줄 몸 자리 이펙트를 다시 낸다(배치 끌기는 FE11이 맡음 — 처음에는 끌기 복구 `ShowOriginal`에서도 다시 냈으나 옛 자리 사실로 재생돼 걷음). 이미 켜져 있으면 FE6가 건너뛴다. 흔들림·몸 글자는 다시 내지 않는다.

### 3.3 정한 것 (2026-10-31 사용자 "FE-b 진행" = 그대로)

- P7 FE4 칸 자리는 발동 줄(줄의 이펙트 모두에 적용)이다. 큐마다가 아니다.
- P8 회수 대상은 몸 자리 이펙트뿐이다(유닛이 있어야 열쇠가 됨). 칸 자리 이펙트는 유지 칸을 켜도 한 번 내고 끝난다(구현 때 경고 대신 칸 툴팁에 적음).
- P9 발동 짝 회수는 그 발동의 대상(없으면 발동 유닛)에 붙은 같은 특성 이펙트만 회수한다.
- P10 공유: 호진님 특성 외형 표 메모(새 칸 둘), 민형님은 없음(GlobalSystem 공개 메서드만 씀).

### 3.4 구현 (2026-10-31, 컴파일 미확인)

- DB: 발동 줄 `TraitActivationVisual`에 `keepWhileStatus`·`recallKinds`(읽기 `KeepWhileStatus`·`RecallKinds`), 검사(회수 종류는 정의된 값·자기 종류 아님·겹침 없음). 줄 그리기 `TraitActivationVisualDrawer`(`CombatBodyTextDrawer.cs`)의 칸 목록에 둘을 더함.
- 줄 값 `CombatActivationRow.KeepWhileStatus`·`RecallKinds`·`HoldsEffects`(`CombatActivationRows.Of`가 넘김).
- 쥘 규칙 새 `Presentation/Effects/CombatEffectHold.cs`(+ `.meta`): 특성·줄의 발동 종류·발동 유닛·회수 종류·상태 규칙. `RecalledBy`(발동 짝)·`SameRow`(같은 이펙트 다시 내지 않는 열쇠).
- 상태 판정 한 곳 새 `Playback/Support/CombatTraitStatusPresence.cs`(+ `.meta`): 공통 상태 목록에서 출처가 (특성, 주인)이고 켜진 것(보호막은 `Eligible`·남은 양 > 0).
- 받침 `CombatEffectFollower`: 쥔 이펙트의 상태 규칙(본 뒤부터 없어지면 회수)·발동 짝 회수(`RecallBy`)·결과 끝 맞추기(`Settle`), 회수 = `ParticleStopNotifier.StopEmitting()`.
- 창구 `CombatEffectWindow.PlayHeld`(같은 줄·발동 유닛·유닛·큐면 다시 내지 않음)·`RecallBy`·`SettleHeld`. 조각 `CombatEffectPiece.Held`, 큐 자리 `CreatePiece(…, hold)`, 큐 붙이기 `CombatCueSinks.Hold`, 곁들임 실행 `CombatMomentCuePlayback.PlayAccent(…, hold)`.
- 연결: 특성 피드백 `CombatTraitFeedbackPlayback`이 줄에서 쥘 규칙을 만들고(`HoldOf`, 발동 시작·상태 뒤 두 길), 발동마다 먼저 발동 짝 회수를 부름(줄 없는 발동도). 생성 인자에 이펙트 창구(사건 Presenter 생성자 한 줄). 결과 끝 최종 사실(`CombatSceneEventPresenter.Refresh`) 뒤 `SettleHeld`.
- Factory 무변경. 사건 처리기의 피드백 없는 대체 길(시험 조립용)과 대신 맞기 앞 항목은 쥐지 않는다.
- FE10(보완): 특성 피드백 `SettleHeld`(① 창구 `SettleHeld`로 꺼진 것 회수 ② 보인 사실의 필드·생존·보이는 유닛마다 켜진 특성 출처 상태 → 그 특성 유지 줄 → `Restore`가 그 유닛 몸 자리로 곁들임 실행, 흔들림 없음). 유지 줄 목록 `TraitVisualData.KeptActivationKinds`·`CombatCueSources.KeptActivationKinds`·`CombatActivationRows.KeptKindsOf`(특성마다 보관), 줄 찾기 `CombatActivationRows.Of(특성, 종류)`(발동 길과 같은 보관이라 큐 객체가 같음 → FE6 판단이 맞음), 출처 읽기 `CombatTraitStatusPresence.TryGetTraitOrigin`, 창구 `IsShown`. 부르는 곳: `CombatSceneEventPresenter.Refresh`(최종 사실 뒤) 하나.
- FE11: `FieldUnitPresenter.TrackBodyPresence`(+ `TrackBody`가 그 위로), 창구 몸 읽기 형식 `Func<EntityId, Func<(bool Bound, Vector3? Body)>>`(Factory 연결 줄 둘이 `units.TrackBodyPresence`를 넘김 — 민형님 §43 고침), 받침 `HideRenderers`·`ShowRenderers`·`OnDestroy`, 창구 `StopAll`이 그리기를 먼저 되돌림. 끌기 복구 때 다시 내기(`CombatSceneDisplay.ShowOriginal` → `SettleHeldEffects`)는 걷음.
- 남는 것: 다시 내는 이펙트는 큐 지연을 따른다(결과 재생 밖이면 지연 있는 항목은 다음 재생 Tick까지 기다림 — 유지 줄 이펙트는 지연 0 권장).

### 3.5 Unity 확인

1. 단독 행동 줄(210)에 Keep While Status를 켜고(이펙트는 반복 파티클이거나 링 버퍼 입자 — `Shild.prefab`은 링 버퍼라 그대로 계속 보임, Looping은 켜지 말 것) → 보호막이 붙으면 이펙트가 남아 유닛을 따라다니고, 특성 보유자나 주변 유닛이 움직여 보호막이 꺼지면(230) 사라진다(링 버퍼는 바로, 그 밖은 자연스럽게, FE12).
2. 같은 유닛에 보호막이 다시 붙으면 이펙트가 하나만 다시 나온다(겹치지 않음). 보호막이 남은 채 턴 끝 갱신(240)이 오면 새로 나오지 않고, 다 깎였다가 턴 끝에 다시 차면 그 결과 끝에 다시 나온다(FE10).
3. Recall Kinds에 230만 적고 Keep While Status를 끄면 230이 올 때만 사라진다.
4. 턴이 지나 만료되는 버프 줄에 Keep While Status를 켜면 만료 순간(HUD 아이콘이 꺼질 때) 사라진다.
5. 유지 중인 유닛이 죽거나 손패로 돌아가면 이펙트가 바로 사라진다(FE3).
6. 칸 자리 이펙트는 유지 칸을 켜도 한 번 나고 끝난다.
7. 배치 단계에서 단독 행동 유닛을 끌면 끄는 동안 이펙트가 안 보인다. 혼자인 자리에 놓으면 이펙트가 이어지고, 혼자가 아닌 자리에 놓으면 새로 재생되지 않고 서서히 사라지며(HUD 보호막 아이콘도 꺼짐), 끌기를 취소하면 제자리에서 다시 보인다(FE11).
8. 손패에 넣었다 다시 놓으면(배치 단계) 넣기 전 예산의 보호막이 다시 생기고 HUD 아이콘·유지 이펙트도 다시 나온다 — 2026-10-31 사용자 지시로 Core 수정(Core 전달 문서 §5.73, §56 처리). 조건이 깨지는 자리에 놓으면 둘 다 없다.

## 4. 공유·기록

- 민형님 Factory 공유: 연결 줄 둘(`CombatEffectWindow` 생성 인자 `units.TrackBody`) — 미전달.
- 호진님: 이펙트 큐 칸 `stayInPlace`, 발동 줄 칸 `keepWhileStatus`·`recallKinds` — 메모 `handoffs/2026-10-30-combat-trait-system-view-hojin-share.md` §5(미전달).
- 클립 이펙트 설계 Q-E1 A 줄에 "몸 자리는 이 설계가 바꿈" 표시.
