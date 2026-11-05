# 전투 View 단계 2라운드 설계 (2026-10-24)

- **상태:** 설계 초안(코드 변경 없음). 정할 것은 §6(Q-V9~Q-V18, Q-T4~Q-T5), 설계에서 정한 것은 §7(P8~P17)이다. 결정 뒤 Task 계획을 쓴다.
  - Q-V9(시작 전 1라운드 커밋)는 사용자 커밋 `23436b00`(2026-10-24)으로 충족했다. 2라운드 A/B 측정의 기준 판은 이 커밋이다.
  - **결정 완료(2026-10-24 사용자 "모두 A"):** Q-V10~Q-V18·Q-T4·Q-T5 모두 A, P8~P17 그대로. Task 계획은 [round2-plan](../plans/2026-10-24-combat-view-stage-round2-plan.md)이다. 계획이 코드와 대조해 이 설계와 다르게 정한 것 아홉(TP-2·3·4·6·7·8·9·10·15)은 그 계획에 있다.
  - **구현(2026-10-24~25):** V2-a~V2-g 코드 완료, Unity 확인 문제 없음(사용자). V2-g 뒤 결정 Q-V2g-1·2 A의 코드 한 곳은 컴파일 미확인이다. V2-h에서 공유 문서 넷·폴더 배치 안내를 썼다. 설계·계획과 다르게 한 것은 각 절 끝의 "구현 메모"에 있다(TP 아홉 포함). 측정(V2-i)은 §11이다: N(`23436b00`) 8.1 → M 8.7(예상 8.92).
- **입력:**
  - [View 단계 설계](2026-10-24-combat-view-stage-design.md) §6(2·3라운드 방향)과 §10(1라운드 측정 7.4 → 8.3, 남은 문제·새로 찾은 문제).
  - 1라운드 뒤 지금 코드(아래 "기준")를 직접 읽고 git blame으로 다른 사람 줄을 셌다.
- **기준:** 작업 트리 = HEAD `8ed3f8cb` + 커밋 전 1라운드 변경. 줄 번호는 지금 작업 트리 기준이다.
- **약어:** `V/`·`G/`·`F/`·`T/`는 1라운드 설계와 같다. `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/`.
- **지킬 것(1라운드와 같음):**
  - MSBuild를 돌리지 않고, 새 `[Test]`를 만들지 않는다(보고에 "컴파일 미확인").
  - 커밋하지 않는다.
  - 범위는 전투 View(`V/`), `CombatViewBuffer`, `CombatEndUI`, 전투 View Factory, 그리고 우리 시스템을 조립하는 연결 줄이다.
  - Framework·GlobalSystem·Core·골격의 기능·계약은 바꾸지 않는다.
  - 참조 없는 에셋·Missing Script는 지우지 않고 보고한다.

## 0. 목표와 측정

**목표:** 1라운드 측정에서 남은 SOLID 중간 셋(V-M4 조정자, V-M5 입력 묶음 허브, V-M7 사건 형식 분류기)을 푼다. 같은 묶음에 걸린 V-M8(미리보기 비우기)·V-M12의 남은 부분(시험 입구 null 허용)과 낮음 다섯도 함께 푼다.

**측정:** 1라운드와 같은 방법이다.
- 범위·평가자 셋·감점 규칙·블라인드 A/B는 1라운드 §10과 같다.
- A/B의 기준 판은 **1라운드 커밋**이다. 그래서 2라운드를 시작하기 전에 사용자가 1라운드를 커밋해야 한다(§6 Q-V9).
- 예상 점수는 "알려진 문제 기준"이다. 지난 세 번 모두 측정이 예상보다 0.2~1.8 낮았다(이번 1라운드는 0.2).

## 1. 대상 문제 (1라운드 측정 §10 기준)

| # | 기준 | 문제(지금 상태) | 묶음 |
|---|---|---|---|
| **V-M4** | SOLID | 조정자 `CombatViewSystem` 1231줄. 연결마다 입력 객체 여덟(`_gate`·`_interaction`·`_router`·덱 둘·아이템·환경 호버·거절 안내)을 직접 만들고 정리한다. BattleChanger·Pack·행동 버튼을 맞추고, 완료 배경음·튜토리얼 막힘 보고·타임라인 길이·재고 읽기도 맡는다 | W6 |
| **V-M5** | SOLID | 입력 묶음 `CombatInteraction` 468줄이 허브다. 수명 메서드 열 개(`BeforeResult`·`AfterResult`·`CancelInputs`·`Stop`…)가 기능 다섯을 이름으로 하나씩 부른다. 기능 하나를 더하면 그 열 곳과 Router 분기를 함께 고친다 | W5 |
| **V-M7** | SOLID | 사건 형식 지식이 정적 분류기 넷과 경로 표에 흩어져 있다. `CombatActionBoundary`(9곳이 부름), `CombatEventSubjects`(주체·근원 switch), `CombatHitFollowUps.TryGetSource`·`IsMovement`, `CombatEnemyPresentationPacing`의 형식 분기, 민형님 `CombatPhaseTransitionPresenter.TryBegin`의 형식 검사 | W7 |
| **V-M8** | 있는 기능 | "내 요청일 때만 비우기"를 소유자 여섯이 각자 쓴다(`PlacementDragInteraction.cs:339-343`, `AttackTargetingInteraction.cs:484-501`, `CombatUnitRangeHover.cs:252-258`, `CombatMovementInteraction.cs:391-395`, `CombatMovementRangeTracker.cs:94·130-135`, `CombatReturnEstimateTracker.cs:85-87`). Buffer에는 조건 없는 `ClearPreview()`만 있다(`B/CombatView/CombatViewBuffer.cs:41·114`) | W5 |
| **V-M12 남은 것** | 불필요·우회 | 시험 입구의 null 허용: `BindServices` 선택 인자 넷(`V/CombatViewSystem.cs:231-234`), PostBind에서만 채우는 `_audio`·`_combatQueries`(`?.` 셋), `CombatDisplayServices` 선택 인자 넷(`V/CombatDisplayServices.cs:35-38`), 입력 Factory의 알림 생략(`F/CombatSceneInputFactory.cs:64`) | W6 |
| V-L7 | SOLID | `CombatPathInput` 720줄이 공격 Drag 넘겨받기까지 맡는다 | W5(일부) |
| V-L10 | FW | 조정자가 정적 Factory를 직접 부른다(`V/CombatViewSystem.cs:415·476·645·649·658`) | W6 |
| V-L14 | 독립성 | 아군 호버와 공격 모드가 같은 층 `AttackRange`를 쓰고, 배타가 호출 순서(`Suspend`를 `Enter` 앞에)에 기댄다(`V/Display/Feedback/CombatAttackFeedback.cs:45-78`, `CombatInteraction.cs:330-332`) | W5 |
| V-L15 | 독립성 | 미리보기 응답 전달 규칙이 둘이다. 경로는 매 Tick 마지막 응답을 받고(`Poll`), 나머지는 새 응답일 때 현재 슬롯 답만 받는다(`PreviewAnswered`)(`V/CombatViewSystem.cs:359-370`). 둘인 까닭은 §3 W5-3 | W5(설명만, Q-V13) |
| 새로 찾은 낮음 | 독립성 | 아이템 드래그 여부가 입력 사실에 없어 판정식의 기본 인자(`itemDragActive = false`)에 기댄다. 경로·환경 호버·Router 취소는 그 인자를 넘기지 않는다(`V/Input/CombatInputRouter.cs:83`) | W5 |

**이번에 하지 않는 것:** V-M6·V-L20(공격 종류·Ballista, 3라운드 W8), V-M9·V-M10·V-L11·V-L12(값은 DB로, 3라운드 W9). 남는 낮음 V-L1·V-L2·V-L4·V-L5·V-L9·V-L18과 새로 찾은 낮음 여섯도 하지 않는다(§10). `CombatSceneEventPresenter`의 선택 인자 약 20개(V-L8)는 대부분 연출 값이라 W9가 설정 묶음으로 옮길 때 함께 본다(P17).

## 2. 묶음과 순서

| 순서 | 묶음 | 항목 | 푸는 문제 |
|---|---|---|---|
| 1 | **W5 입력 소유자 공통 계약** | W5-1 공통 수명 계약과 목록 하나 · W5-2 포인터 대상은 사실 하나로 · W5-3 응답 전달의 두 단계를 계약에 · W5-4 Buffer의 번호 있는 비우기 · W5-5 공격 Drag 넘겨받기를 소유자 하나로 · W5-6 아군 호버 층 분리 | V-M5, V-M8, V-L7 일부, V-L14, V-L15(설명만), 새로 찾은 낮음 |
| 2 | **W7 사건 형식 표 하나** | W7-1 형식 표 `CombatEventTypeTable` · W7-2 분류기 넷을 표로 · W7-3 화면 전환 사건도 표가 답함(민형님 줄) | V-M7 |
| 3 | **W6 조정자 나누기** | W6-1 연결 입력 묶음 · W6-2 입력 표면 맞춤 · W6-3 표시 값 읽기 · W6-4 튜토리얼 막힘 보고 · W6-5 완료 배경음은 표시로 · W6-6 생성 주입 · W6-7 운영 입구와 시험 입구 | V-M4, V-L10, V-M12 남은 것 |

순서의 까닭:
- W5가 먼저다. W6-1이 옮기는 입력 객체 생성·정리가 W5의 목록 꼴을 그대로 쓰기 때문이다.
- W7은 입력과 파일이 겹치지 않는다. W5와 W6 사이에 두어 W6 전에 조정자 밖 변경을 끝낸다.
- W6이 마지막이다. 조정자에서 빼는 것이 가장 많아서, 앞 묶음이 바꾼 호출을 한 번에 옮긴다.

## 3. W5 입력 소유자 공통 계약

### W5-1 공통 수명 계약과 목록 하나 (V-M5)

**지금:** 묶음의 수명 메서드마다 기능을 이름으로 부른다. 같은 "정리"가 메서드마다 다른 순서·다른 기능 조합으로 적혀 있다.

```csharp
// V/Input/Interaction/CombatInteraction.cs (지금)
internal void BeforeResult()
{
    _attackDrag?.Invalidate();
    _path?.CancelInputs();
    _attack.BeforeResult();
    _rangeHover?.Suspend();
}
internal void CancelPresentation()
{
    _attackDrag?.Invalidate();
    _item?.Cancel();
    _placement.CancelPresentation();
    _attack.Exit();
    _rangeHover?.Suspend();
}
// AfterResult·CancelInputs·SelectCanceled·Stop·Poll·PreviewAnswered도 같은 꼴
```

**바꾼 뒤:** 입력 기능 하나가 공통 수명에 어떻게 참여하는지를 그 기능 파일 안의 작은 어댑터가 답한다. 묶음은 Factory가 준 목록을 차례로 돈다.

```csharp
// 새 파일 V/Input/Interaction/CombatInputOwner.cs
/// <summary>
/// 전투 입력 기능 하나가 공통 수명에 참여하는 방법입니다(View 2라운드 W5-1). 입력 기능 묶음이 목록 순서대로 부릅니다.
/// 기본 구현은 "그 단계에서 할 일 없음"입니다(사건 처리기 CombatEventHandler와 같은 꼴).
/// </summary>
internal abstract class CombatInputOwner
{
    /// <summary>이 소유자가 받는 포인터 대상입니다. 포인터를 받지 않는 소유자(호버·넘겨받기)는 None입니다.</summary>
    internal virtual CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.None;
    /// <summary>포인터를 쓰는 중(Drag·모드·경로 지정)인지입니다. 쓰는 소유자는 많아야 하나입니다.</summary>
    internal virtual bool IsActive => false;
    /// <summary>배경 소유자(사용자 조작 없이 화면을 맞추는 호버)인지입니다.</summary>
    internal virtual bool IsBackground => false;

    internal virtual void PointerMoved(in CombatPointerInput input) { }
    internal virtual void SelectPressed(in CombatPointerInput input) { }
    internal virtual void SelectReleased(in CombatPointerInput input) { }
    /// <summary>선택이 강제로 끝났습니다(Layer 전환·Unbind). 명령은 기록하지 않습니다.</summary>
    internal virtual void SelectCanceled() { }

    /// <summary>새 응답일 때 한 번, 현재 슬롯 답(tick.Facts.PreviewAnswer)을 받습니다. 자기 요청이면 쓰고 true입니다(W5-3).</summary>
    internal virtual bool PreviewAnswered(in CombatInputTick tick) => false;
    /// <summary>매 Tick 감시입니다. 마지막 응답(tick.LatestPreview)을 보며 수명 확인·다시 시도·배경 요청을 합니다. 응답 단계 앞에 부릅니다(W5-3).</summary>
    internal virtual void Poll(in CombatInputTick tick) { }

    internal virtual void BeforeResult() { }
    internal virtual void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) { }
    internal virtual void AfterAcknowledged() { }

    /// <summary>전경 소유자 하나가 활성이 되기 전에 부릅니다. 배경 소유자는 내려놓습니다(W5-6과 함께 배타 규칙 한 곳).</summary>
    internal virtual void YieldToForeground() { }
    /// <summary>경로 제외 정리: Drag·고스트·모드·자기 미리보기. 정리할 것이 없으면 아무것도 하지 않습니다.</summary>
    internal virtual void CancelPresentation() { }
    /// <summary>경로 포함 정리입니다. 기본은 <see cref="CancelPresentation"/>입니다.</summary>
    internal virtual void CancelInputs() => CancelPresentation();
    /// <summary>오류 정지·씬 연결 해제입니다. 기본은 <see cref="CancelInputs"/>입니다.</summary>
    internal virtual void Stop() => CancelInputs();
}
```

```diff
 // V/Input/Interaction/PlacementDragInteraction.cs (예: 기능 메서드는 그대로, 끝에 어댑터 하나)
+        /// <summary>공통 수명에 참여하는 창구입니다(W5-1). 기능 메서드는 그대로 두고 단계마다 어느 메서드인지만 답합니다.</summary>
+        internal CombatInputOwner Owner { get; }
 ...
+        private sealed class InputOwner : CombatInputOwner
+        {
+            private readonly PlacementDragInteraction _p;
+            internal InputOwner(PlacementDragInteraction p) => _p = p;
+            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Placement;
+            internal override bool IsActive => _p.IsDragging;
+            internal override void PointerMoved(in CombatPointerInput i) => _p.PointerMoved(i.Facts, i.X, i.Y);
+            internal override void SelectPressed(in CombatPointerInput i) => _p.SelectPressed(i.Facts, i.Characters, i.X, i.Y);
+            internal override void SelectReleased(in CombatPointerInput i) => _p.SelectReleased(i.Facts, i.X, i.Y);
+            internal override void SelectCanceled() => _p.Cancel();
+            internal override bool PreviewAnswered(in CombatInputTick t) => _p.PreviewAnswered(t.Facts.PreviewAnswer);
+            internal override void AfterResult(in CombatInputFacts f, IReadOnlyList<CharacterData> c) => _p.AfterResult(f, c);
+            internal override void CancelPresentation() => _p.CancelPresentation();
+        }
```

```diff
 // V/Input/Interaction/CombatInteraction.cs (바꾼 뒤, 수명 메서드는 목록을 돈다)
-        internal void BeforeResult()
-        {
-            _attackDrag?.Invalidate();
-            _path?.CancelInputs();
-            _attack.BeforeResult();
-            _rangeHover?.Suspend();
-        }
+        internal void BeforeResult()
+        {
+            for (int i = 0; i < _owners.Count; i++)
+                _owners[i].BeforeResult();
+        }
```

**목록 순서(원본은 입력 Factory 한 곳, P9):** 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버. 지금 각 메서드의 순서를 이 하나로 맞출 수 있는지 대조했다.

| 단계 | 지금 순서 | 목록 순서로 | 같은가 |
|---|---|---|---|
| `BeforeResult` | 넘겨받기 → 경로 → 공격 → 호버 | 같음(아이템·배치는 할 일 없음) | 같음 |
| `CancelPresentation` | 넘겨받기 → 아이템 → 배치 → 공격 → 호버 내려놓기 | 같음(경로는 경로 제외라 할 일 없음) | 같음 |
| `CancelInputs` | 넘겨받기 → 경로 → (위 정리) → 호버 지움 | 같음(호버의 `CancelInputs` = 지움) | 같음 |
| `Stop` | 넘겨받기 → 경로 Detach → (위 정리) → 호버 지움 | 같음 | 같음. 지금처럼 하나가 실패해도 나머지를 시도하고 모아 던진다 |
| `SelectCanceled` | 넘겨받기 → 경로 → 아이템 → 배치 → 호버 지움 | 같음(공격은 할 일 없음) | 같음 |
| `AfterResult` | 배치 → 아이템 → 공격 | 아이템 → 배치 → 공격 | **순서가 바뀜.** 두 Drag는 같은 배치 Layer에서 함께 켜지지 않아(묶음이 서로 끔) 결과는 같다 |
| `PreviewAnswered` | 공격 → 호버 → 배치(먼저 쓴 쪽에서 멈춤) | 배치 → 공격 → 호버(경로는 감시 단계에서 받음) | **순서가 바뀜.** 요청 번호는 하나뿐이라 쓰는 소유자는 많아야 하나다. 결과는 같다 |
| `Poll` | 넘겨받기 → 경로 → 호버 | 같음 | 같음 |
| `AfterAcknowledged` | 경로 | 같음 | 같음 |

**묶음에 남는 것:** 사용자 의도 입구는 기능 이름으로 남긴다. 덱 카드 집기, 아이템 집기·해제, 공격 모드 진입·끄기, 복귀, 키 취소다. 이 입구들은 기능 사이의 전환(다른 쪽 표현 정리, 호버 내려놓기)을 정하는 자리라서, 수명처럼 "모두에게 같은 일"이 아니다. 대신 전환의 공통 부분은 한 메서드로 모은다.

```csharp
// 전경 소유자 하나가 활성이 되기 전의 규칙 하나(W5-1·W5-6). 지금 SelectDeckUnit·SelectItem·AttackModeRequested가 각자 적던 정리다.
private void BeginForeground(CombatInputOwner next)
{
    for (int i = 0; i < _owners.Count; i++)
    {
        CombatInputOwner owner = _owners[i];
        if (owner.IsBackground) owner.YieldToForeground();
        else if (!ReferenceEquals(owner, next)) owner.CancelPresentation();
    }
}
```

- 지금 `SelectDeckUnit`은 아이템 취소·호버 내려놓기를 하고, `SelectItem`은 배치 표현 정리·호버 내려놓기를 하며, `AttackModeRequested`는 호버 내려놓기·배치 표현 정리를 한다. 셋 다 "다음 소유자 밖의 표현 정리 + 배경 내려놓기"다.
- `BeginForeground`는 지금 세 입구가 부르지 않던 소유자도 부른다(공격 진입 때 아이템, 덱 집기 때 공격·넘겨받기 등). 그런 소유자는 그때 활성이 아니라서, 하는 일이 없거나 세대 번호만 올린다(코드 대조).
  - 아이템 `Cancel`은 끄는 중이 아니면 바로 돌아온다.
  - 배치 `CancelPresentation`은 끄는 중이 아니면 배치 호버만 지운다. 배치를 부르는 두 입구(아이템 집기·공격 진입)는 지금도 같은 호출을 한다.
  - 공격 `Exit`는 모드가 아니면 세대 번호만 올린다(`AttackTargetingInteraction.cs:216-222`). 모드가 아니면 그 번호를 기다리는 콜백이 없다.
  - 넘겨받기 `Invalidate`는 몸짓 세대만 올린다(`CombatAttackDragGesture.cs:79-84`). 배치 Layer에서는 후보가 없다.
  - 그래서 지금과 다른 점은 "덱 카드·아이템을 집을 때 쉬고 있는 공격·넘겨받기의 세대 번호가 오른다" 하나다. 보이는 차이는 없다.
- 경로의 `CancelPresentation`은 할 일이 없다(경로 제외). 공격 진입 전의 경로 취소는 지금처럼 호출자(`LeaveMoveMode`)가 한다.

**바깥 서명은 그대로 둔다(P8).** 묶음의 공개 멤버 이름·인자(`PointerMoved(target, …)`, `Poll(preview)`, `PreviewAnswered(facts, …)`, `IsAttackActive`, `IsPathPlanning` 등)와 기능 클래스의 메서드는 바꾸지 않는다. 다른 사람 시험이 이것들을 직접 부르기 때문이다.
- 호진님 `HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`(묶음 수명·`Poll(preview)`)
- 천명님 `WallAttackInteractionTests`(공격 기능 메서드)
- `CombatInteractionTests`의 천명님·현강님 줄

**새 형식 둘(입력 값 묶음):**
- `CombatPointerInput`: 포인터 하나의 값(사실·캐릭터·좌표·대상·"다른 소유자 쉼").
- `CombatInputTick`: 한 Tick의 값(사실·캐릭터·마지막 응답·"다른 소유자 쉼").
- 둘 다 `readonly struct`이고 `in`으로 넘긴다. 매 입력마다 할당하지 않는다.
- **구현 메모(V2-b, 2026-10-24):** 위 스케치와 다르게 구현한 것이 셋이다. 기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-b" 실행 기록에 있다.
  - `CombatInputTick`은 만들지 않았다. 사실 없이 부르는 1인자 `Poll(preview)`(P8)가 남아서, 한 Tick 값에 가짜 사실을 넣어야 하기 때문이다. 대신 단계를 셋으로 나눴다.
    - 전경 감시 `Poll(CombatPreviewResult latest)`
    - 배경 감시 `ObserveTick(in facts, chars, bool canShow)`
    - 응답 `PreviewAnswered(in facts, chars)`
  - `CombatPointerInput`에는 대상·"다른 소유자 쉼"이 없다(사실·캐릭터·좌표만). 대상으로 소유자를 고르는 일은 묶음이 한다. 배경 허용 값은 전경 처리 뒤 묶음이 계산해 `ObservePointer(input, canShow)`로 넘긴다(계획 F4).
  - 기능 쪽 창구 속성은 `Owner`가 아니라 `InputOwner`이고, 어댑터 클래스는 `OwnerAdapter`다. `Owner`라고 하면 같은 클래스 안의 열거형 이름(`Owner.Player`·`Owner.Enemy`)을 가려 컴파일 오류가 난다(세 곳, 그중 `PlacementDragInteraction`의 것은 다른 사람 줄).
  - 소유자 목록은 P9의 "입력 Factory 한 곳"이 아니라 묶음 `CombatInteraction`의 생성자가 만든다(계획 TP-2). 묶음은 이름 입구(덱·아이템·공격·복귀·취소) 때문에 기능을 이름으로 받아야 한다. 순서까지 Factory가 넘기면 같은 기능을 두 번 받게 된다. 순서 표는 생성자 주석에 둔다.

### W5-2 포인터 대상은 사실 하나로 (새로 찾은 낮음)

```diff
 // V/Input/CombatInputFacts.cs — 생성자 끝 선택 인자(1라운드 P1과 같은 규칙)
-            bool isModalOpen = false)
+            bool isModalOpen = false,
+            bool isItemDragActive = false)

 // V/Input/CombatInputRouter.cs — 판정식은 사실 하나만 받는다
-        internal static PointerTarget ResolvePointerTarget(InputLayer layer, bool attackActive, bool itemDragActive = false)
+        internal static PointerTarget ResolvePointerTarget(in CombatInputFacts facts)
```

- 부르는 곳 다섯(Router 둘, 묶음 `Poll`, 경로 `AllowsInput`, 조정자 `SyncEnvironmentHover` — 현강님 줄 `:1181`)이 모두 같은 사실을 넘긴다. 지금은 아이템 인자를 넘기는 곳이 둘뿐이다.
- 판정은 지금과 같다(배치 Layer는 아이템을 끄는 중이면 Item, 아니면 Placement / 전투 Layer는 공격 모드면 Attack, 아니면 Path).
- 경로·환경 호버는 배치 Layer에서 Item과 Placement를 가리지 않으므로 결과가 같다(경로는 Path만, 환경 호버는 None이 아닌지만 본다).
- 묶음은 대상별 소유자를 표 하나로 찾는다. 소유자가 선언한 `Target`으로 생성 때 만들고, 같은 대상이 둘이면 생성 때 예외다(P10).
- 판정식을 직접 부르는 우리 시험 둘(`CombatInputRouterTests:62`, `CombatItemDragInteractionTests:190-194`)은 사실을 만들어 넘기게 호출만 맞춘다.
- **구현 메모(V2-a, 2026-10-24):** 판정식은 사실 하나만 받게 바꾸지 않았다(계획 TP-3). 셋째 인자의 기본값만 지웠다. 사실을 쓰는 곳(경로·환경 호버)은 `facts.IsItemDragActive`를, Router는 묶음의 실시간 값을 넘긴다. 시험 여럿이 Router에 공격 모드 값이 없는 고정 사실을 주어서, 판정을 사실만 보게 하면 공격 제출이 경로로 가기 때문이다. 판정식 두 인자 호출 시험 둘은 셋째 인자 `false`만 더했다.

### W5-3 응답 전달의 두 단계를 계약에 (V-L15)

**지금:** 한 Tick에서 조정자가 두 번 부른다. 경로는 `Poll`로 매번 마지막 응답(번호 무관)을 받고, 나머지는 새 응답일 때 현재 슬롯 답만 받는다. 이 차이는 조정자의 Tick과 묶음 주석에만 적혀 있다.

```csharp
// V/CombatViewSystem.cs:359-370 (지금)
_interaction.Poll(_state.TryGetLatestPreview(out var latestPreview) ? latestPreview : null, ReadFacts(), _characters.Characters);
if (signal.PreviewAnswered)
{
    _interaction.PreviewAnswered(ReadFacts(followUp: true), _characters.Characters);
    ...
}
```

**왜 둘인가(코드 대조):**
- 경로의 소비자는 응답을 받은 Tick에 쓰지 못하면 다음 Tick에 다시 시도하는 것에 기댄다.
  - 복귀 견적(`CombatReturnEstimateTracker.HandleAnswer`)은 `AllowsReturnInput()`이 참일 때만 답을 쓰고 "답 받음"을 표시한다.
  - 쓰지 못한 동안에도 자기 요청은 슬롯에 남는다. 그래서 배경 요청 규칙 R-b가 "기다리는 중"으로 보고 다시 요청하지 않는다.
  - 응답을 한 번만 넘기면, 그 Tick에 조건이 잠깐 거짓이었을 때 견적이 끝내 보이지 않는다.
- 공격 조준 호버와 배치는 같은 응답을 받을 때마다 다시 적용한다. 영향 칸·체력 예상·궤적(`ApplyAttackAnswer`)과 호버 상태·고스트 색이다. 공격 범위(`_rangeAnswered`)와 유닛 호버(`ClearOwnedRequest`)만 한 번 쓰게 막혀 있다. 그래서 이들에게 매 Tick 넘기면 같은 그리기를 반복한다.
- 그래서 지금의 두 시점은 각 소비자의 성질에서 나온 것이다. 하나로 합치려면 소유자마다 "이 번호를 이미 썼나" 기억을 더하고, 경로 소비자 셋의 다시 시도를 그 기억으로 바꿔야 한다. 이동 입력에서 가장 섬세한 곳이라 권장안은 이 길을 가지 않는다(합치는 길은 Q-V13 B).

**바꾼 뒤(Q-V13 A):** 두 시점을 계약의 두 단계로 이름 붙이고, 조정자는 입구 하나만 부른다.

```diff
-            _interaction.Poll(_state.TryGetLatestPreview(out var latestPreview) ? latestPreview : null,
-                ReadFacts(), _characters.Characters);
-            if (signal.PreviewAnswered)
-            {
-                _interaction.PreviewAnswered(ReadFacts(followUp: true), _characters.Characters);
-                if (!CanContinue())
-                    return;
-            }
+            // 감시 → 응답 순서 하나(W5-3). 감시는 매 Tick 마지막 응답과 함께, 응답은 새 응답일 때만 현재 슬롯 답으로 한 번.
+            _interaction.Tick(ReadFacts(), _characters.Characters,
+                _state.TryGetLatestPreview(out var latestPreview) ? latestPreview : null,
+                signal.PreviewAnswered ? ReadFacts(followUp: true) : (CombatInputFacts?)null);
+            if (!CanContinue())
+                return;
```

- 계약의 `Poll(tick)`은 "매 Tick 감시. 마지막 응답(`tick.LatestPreview`)을 보며 자기 요청의 수명을 확인하고 다시 시도한다"이다. 경로·넘겨받기·호버가 쓴다.
- 계약의 `PreviewAnswered(tick)`은 "새 응답일 때 한 번. 현재 슬롯 답(`tick.Facts.PreviewAnswer`)이 자기 번호면 쓴다"이다. 배치·공격·호버가 쓴다.
- 어느 소유자가 어느 단계를 쓰는지가 그 소유자의 어댑터에 적힌다. 조정자는 두 단계를 모른다.
- 응답 단계는 지금처럼 현재 슬롯 답이 있을 때만 돈다(배치의 `PreviewAnswered`는 답이 null이면 안 된다).
- 동작은 같다. 순서(감시 → 응답)와 넘기는 값(감시는 `ReadFacts()`, 응답은 `ReadFacts(followUp: true)`)을 그대로 둔다.
- 묶음의 `Poll(preview)` 한 인자 판(경로만 감시, 시험용)은 호진님 시험이 쓰므로 남긴다(P8).
- V-L15는 낮음으로 남을 수 있다. 규칙이 둘인 것은 그대로이고, 그 까닭이 계약에 적힐 뿐이기 때문이다(§8).

### W5-4 Buffer의 번호 있는 비우기 (V-M8)

```diff
 // B/CombatView/CombatViewBuffer.cs
         void ClearPreview();
+
+        /// <summary>
+        /// 미리보기 요청이 <paramref name="number"/>일 때만 비웁니다. 비웠으면 true입니다. 0 이하·다른 번호·빈 슬롯이면 아무것도 하지 않습니다.
+        /// 요청 소유자는 자기 번호로 이것만 부릅니다(남의 요청을 지우지 않게, View 2라운드 W5-4).
+        /// </summary>
+        bool ClearPreview(int number);
```

```diff
 // V/Input/Interaction/PlacementDragInteraction.cs (여섯 곳 같은 꼴)
         private void ClearOwnedPreview()
         {
-            if (_previewRequestNumber != 0 && _requests.HasPreview && _requests.Preview.Number == _previewRequestNumber)
-                _input.ClearPreview();
+            _input.ClearPreview(_previewRequestNumber);
             _previewRequestNumber = 0;
         }
```

- 조건 없는 `ClearPreview()`는 남긴다. Buffer `Clear`가 쓰고, 다른 작업자 시험 여럿이 직접 부른다(유물 시험·전투 진입 통합 시험 등).
- 이동 범위 요청의 `OwnsPreview`는 종류·행위자까지 본다. 요청 번호는 Buffer가 하나씩만 발급하므로(`PeekNextNumber`), 번호가 같으면 종류·행위자도 같다. 그래서 `ClearPreview(number)`로 바꿔도 같다. `OwnsPreview`는 범위 응답 소비 검사에 그대로 쓴다.
- 이 계약을 구현하는 시험 가짜 둘에 한 줄씩 더해야 컴파일된다. 우리 `CombatInteractionTests.RejectingSubmitSink`(`:1112`)와 천명님 `CombatMovementViewTests.CountingInput`(`:647`)이다(Q-T5).

### W5-5 공격 Drag 넘겨받기를 소유자 하나로 (V-L7 일부)

**지금:** 공격 모드에서 누른 채 끌면 이동 Drag로 넘기는 일(호진님 코드)이 세 곳에 나뉘어 있다.
- 묶음의 `TryHandleAttackDragPointer`(`:152-206`) 안에 있다.
- 묶음 메서드 아홉 곳에 `_attackDrag?.Invalidate()`가 한 줄씩 있다(수명 단계 여섯, 의도 입구 셋).
- 경로 입력의 출발점 메서드 셋(`CombatPathInput.cs:117-150`)이 맡는다.

**바꾼 뒤:**
- 새 소유자 `V/Input/Interaction/CombatAttackDragHandover.cs`가 몸짓(`CombatAttackDragGesture`, 호진님 파일 그대로)을 갖는다. 묶음의 `TryHandleAttackDragPointer` 본문을 옮긴다.
- 목록 맨 앞의 소유자라서, 수명 단계(결과 전·표현 정리·입력 정리·정지·선택 강제 종료·감시)마다 `Invalidate`를 자기 어댑터에서 한다. 그 여섯 줄은 묶음에서 없어진다.
- 공격 모드 진입은 `BeginForeground`가 넘겨받기의 `CancelPresentation`(= `Invalidate`)을 부른다. 의도 입구 셋(복귀·공격 끄기·이동 모드 끝)에는 `_handover.Invalidate()` 한 줄씩이 남는다.
- 묶음의 `TryHandleAttackDragPointer`는 이름을 남기고 넘겨받기 소유자에 위임한다(Router 줄 `:229` 그대로).
- 경로 쪽 출발점 메서드 셋은 경로의 지정 상태(`_pathActor`·`_planningVersion`·`PlanningActor`)를 읽어야 해서 경로에 남긴다. 넘겨받기 소유자는 경로를 좁은 계약 `ICombatAttackDragTarget`(출발점 셋 + `PointerMoved`·`SelectReleased`·`CancelInputs`)으로만 본다.
- 동작은 같다(같은 문장을 옮김).
- V-L7(경로 720줄)은 크게 줄지 않는다. 경로에 남는 것은 지정 상태 기계, 범위·복귀의 시점, 넘겨받기의 받는 쪽이다. 낮음으로 남을 수 있다(§8).
- **구현 메모(V2-c, 2026-10-24):** 위와 다르게 한 것이 둘이다. 기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-c" 실행 기록에 있다.
  - 의도 입구는 넷이다(복귀·공격 끄기·이동 모드 끝·공격 진입, 계획 검증 F10). 공격 진입은 지금처럼 진입 판단(`CanEnter`) 앞에서 무효화한다. 들어갈 수 있으면 `BeginForeground`가 한 번 더 무효화한다.
  - 받는 쪽 계약에는 `IsDetached`도 있다(계획 F6). 경로가 없거나 끊겼으면 넘기지 않는다. 옛 "경로 참조 있음·같은 참조" 검사는 이것 하나로 바뀌었다.

### W5-6 아군 호버 층 분리 (V-L14)

```diff
 // V/Display/Field/FieldHighlightLayers.cs
-        HazardRange = 1 << 9
+        HazardRange = 1 << 9,
+        /// <summary>
+        /// 호버한 아군 유닛의 공격 범위 칸입니다(View 2라운드 W5-6). 공격 모드의 <see cref="AttackRange"/>와 색은 같고 층만 나눠,
+        /// 호버를 지워도 공격 모드 범위가 지워지지 않습니다. 둘이 함께 보이는 일은 없습니다(전경 규칙 BeginForeground).
+        /// </summary>
+        AllyHoverRange = 1 << 10

 // DrawOrder: AttackRange 다음 자리, ColorOf: AttackRange 색, FillsCell: 채움
 // V/Display/Feedback/CombatAttackFeedback.cs LayerOf: Ally → AllyHoverRange
```

- 화면은 같다. 색이 공격 범위 색과 같고, 두 층이 함께 켜지지 않기 때문이다.
- 배타는 W5-1의 `BeginForeground` 한 곳이 정한다. 층이 나뉘어서, 순서가 틀려도 공격 범위가 지워지지 않는다.
- 아웃라인 손잡이가 층마다 하나라서 하나 늘어난다. 이를 세는 우리 시험 `FieldOutlineRendererTests`(`:84`·`:312`·`:575`·`:593`)의 숫자가 바뀐다. 바뀐 동작이라 그 단언의 숫자만 맞춘다(Q-T4).
- 층을 하나 더하는 데 다섯 곳(열거·순서·색·채움·호버 색조)이 필요한 것은 새로 찾은 낮음 그대로다. 이번에는 늘리기만 한다.
- **구현 메모(V2-a, 2026-10-24):** 행동 중 마스크(`FieldHighlightState.ActingLayers`)에도 새 층을 넣었다(계획 TP-4). 빼면 GridOutline에서 아군 호버 범위가 사라진다. 그래서 층을 더하는 곳은 여섯이다. 바뀐 숫자를 보는 우리 단언은 지우지 않고 기대값만 맞췄다: 손잡이 수 넷, `FillNames` 한 줄, `CombatAttackFeedbackTests`의 아군 호버 둘.

## 4. W7 사건 형식 표 하나 (V-M7)

### W7-1 형식 표 `CombatEventTypeTable`

**지금:** "이 사건 형식은 무엇인가"를 묻는 곳이 다섯이다.

| 묻는 것 | 지금 자리 | 부르는 곳 |
|---|---|---|
| 사건 틀 형식 목록·경로(Step·Return·Railway·Event·Skip) | `Playback/Ordering/CombatEventRoutes` | 순차 재생, 재생 조정, 처리기 표 |
| 새 행동 시작인가 | `Playback/Ordering/CombatActionBoundary.IsAction` | 9곳(Gate·후속 반응·걸음 묶음·주체 막기·강조 끄기·동시 묶음 등) |
| 주체·피해 근원 | `Playback/Ordering/CombatEventSubjects` switch 둘 | 동시 묶음·후속 반응·걸음·주체 막기 |
| 후속 반응의 출처·이동 사건인가 | `Presentation/Attack/CombatHitFollowUps.TryGetSource`·`IsMovement` | 후속 반응·동시 묶음·Gate·걸음 접촉 |
| 적 연출 간격 종류·머묾 | `Playback/Support/CombatEnemyPresentationPacing` 형식 분기 | 사건 Presenter |
| 화면 전환 사건 | 민형님 `Playback/CombatPhaseTransitionPresenter.TryBegin:33-35` | 재생 조정 |

**바꾼 뒤:** 사건 형식마다 한 줄인 표 하나(`V/Playback/Ordering/CombatEventTypeTable.cs`, 경로 표를 넓힌 것)가 모두 답한다.

```csharp
// 새 파일(옛 CombatEventRoutes를 넓힘). 한 줄(Row) = 사건 형식 하나의 사실 지식.
// 이름은 Core의 CombatEventKind(사건 종류 열거)와 헷갈리지 않게 "형식 표"로 한다(P14).
internal static class CombatEventTypeTable
{
    private static readonly Dictionary<Type, Row> Rows = Build(
        Row<CombatAttackEvent>(CombatEventHandlerSlot.Attack, isActionStart: true,
            actor: e => e.Attack.AttackerId,
            attackSource: e => (e.Attack.AttackerId, e.Attack),
            pacing: e => e.Attack.Succeeded ? CombatEventPacing.Attack : CombatEventPacing.None),
        Row<CombatRoarEvent>(CombatEventHandlerSlot.Roar, isActionStart: true,
            actor: e => e.BossId,
            attackSource: e => e.Attack.HasValue ? (e.BossId, e.Attack.Value) : default,
            pacing: _ => CombatEventPacing.AttackLike, holdsWhenShownOnly: true),
        Row<CombatSummonEvent>(CombatEventHandlerSlot.Summon, isActionStart: true,
            actor: e => e.Record.SummonerId, pacing: _ => CombatEventPacing.Action, holdsWhenShownOnly: true),
        Row<CombatTurnEvent>(CombatEventHandlerSlot.Turn, isActionStart: true,
            actor: e => e.ActorId, pacing: _ => CombatEventPacing.Turn,
            transition: e => e.Kind == CombatEventKind.CombatEntered ? CombatPhaseTransitionKind.Night
                : e.Kind == CombatEventKind.NightEnded ? CombatPhaseTransitionKind.Morning : CombatPhaseTransitionKind.None),
        Row<CombatCharacterEvent>(CombatEventHandlerSlot.Character,
            route: CharacterRoute, subject: e => e.Final?.EntityId ?? e.Expected?.EntityId ?? EntityId.Empty,
            origins: e => e.DamageOrigins, isMovement: IsMovementKind),
        Row<CombatStructureEvent>(CombatEventHandlerSlot.Facts,
            subject: e => e.Final?.EntityId ?? e.Expected?.EntityId ?? EntityId.Empty, origins: e => e.DamageOrigins),
        Row<CombatOutcomeEvent>(CombatEventHandlerSlot.Facts,
            transition: e => e.Outcome.Outcome == StageOutcome.Victory ? CombatPhaseTransitionKind.Morning : CombatPhaseTransitionKind.None),
        // … 나머지 사실 형식(영혼·효과·유물·기믹(Railway 경로)·원본 변경·가시 소비·장애물 반응·보유 효과·오브젝트 발동/생성·회오리·낙하·아이템)
        );

    internal static CombatEventRoute RouteOf(CombatEvent e) => …;          // 옛 CombatEventRoutes.RouteOf
    internal static bool IsActionStart(CombatEvent e) => …;               // 옛 CombatActionBoundary.IsAction
    internal static bool TryGetSubject(CombatEvent e, out EntityId s) => …;
    internal static bool TryGetOrigins(CombatEvent e, out IReadOnlyList<CombatDamageOrigin> o) => …;
    internal static bool TryGetAttackSource(CombatEvent e, out EntityId attacker, out AttackResult attack) => …;
    internal static bool IsMovement(CombatEvent e) => …;
    internal static EntityId ActorOf(CombatEvent e) => …;                 // 처리기 ActorOf를 대신함
    internal static CombatEventPacing PacingOf(CombatEvent e) => …;
    internal static bool HoldsWhenShownOnly(CombatEvent e) => …;
    internal static CombatPhaseTransitionKind TransitionOf(CombatEvent e) => …;
    internal static CombatEventHandlerSlot HandlerSlotOf(Type eventType) => …;
}
```

- 열쇠는 사건 형식이다. 모든 사건 형식은 `sealed`라 형식 같음 비교가 `is` 검사와 같다. 지금 처리기 표가 등록 때 확인하는 규칙을 표가 만들 때 확인한다.
- 값에 따라 갈리는 것(캐릭터 사건의 Moved·Returned 경로, 기믹의 Railway 경로, 턴 사건 종류별 전환)은 그 줄의 함수가 답한다. 형식 밖으로 나가지 않는다.
- 표에 없는 형식은 지금처럼 Skip(경고 후 건너뜀)이고, 나머지 질문에는 "없음"으로 답한다.
- 처리기 표 `CombatEventHandlerTable.Create`는 줄마다 `HandlerSlot`으로 처리기를 고른다. 처리기의 `ActorOf`는 표로 옮긴다. 처리기에는 연출 단계(간격 전 보임·강조 칸·사실 전후 시작·사실 반영)만 남는다.
- 새 사건 형식 하나 = 표 한 줄(+ 연출이 있으면 처리기 하나). 지금은 5~6곳이다.

### W7-2 분류기 넷을 표로

- `CombatActionBoundary`는 지운다. 9곳은 `CombatEventTypeTable.IsActionStart`를 부른다. 우리 시험 `CombatActionBoundaryTests`는 확인 내용을 그대로 두고 호출만 표로 맞춘다(Q-T4).
- `CombatEventSubjects`에는 피해 근원 열쇠(`OriginKey`·`TryGetOriginKey`, 근원 여럿을 하나로 합치는 규칙)만 남긴다. 주체·근원 switch는 표로 옮긴다.
- `CombatHitFollowUps`의 `TryGetSource`·`IsMovement`는 표로 옮긴다. 후속 반응을 모으는 상태(`Begin`·`TryGet`·`HitTargetsOf`)는 그대로 둔다. 시험 두 줄은 호출만 맞춘다.
- `CombatEnemyPresentationPacing`은 형식 검사 대신 `PacingOf`(없음·공격·공격 같은 행동·행동·턴)로 가른다. "성공한 공격만"은 공격 줄의 함수가 답한다. 턴 안의 갈래(턴 끝·턴 시작·전투 진입·밤 끝)는 사건의 `Kind`(Core 값)로 지금처럼 가른다. 간격 상태 기계는 그대로다. `BeforeEvent`·`HoldAfter` 서명이 같아서 우리 시험 `CombatEnemyPresentationPacingTests`는 바뀌지 않는다.
- `CombatEventRoutes`는 표에 합친다. 이름은 지운다(P14). 우리 시험 `CombatEventRoutesTests`·`CombatEventHandlerTableTests`는 호출만 맞춘다.
- 동작 차이는 없다. 같은 조건을 옮기기만 한다.
- **구현 메모(V2-d, 2026-10-24):** 둘을 P14와 다르게 했다.
  - `CombatEventRoutes.cs`는 지우지 않고 `git mv`로 `CombatEventTypeTable.cs`로 이름을 바꿨다(계획 TP-8). 기록이 이어지고 GUID가 같다. 경로 열거 `CombatEventRoute`는 그 파일에 남는다. `CombatActionBoundary.cs`는 `.meta`와 함께 지웠다.
  - 처리기의 `ActorOf`를 걷으면서, `CombatEventHandlerTableTests`에서 None 처리기의 행위자를 보던 단언 하나를 지웠다(계획 TP-9). 같은 시험의 나머지는 호출만 맞췄다.

### W7-3 화면 전환 사건도 표가 답함 (민형님 줄)

```diff
 // V/Playback/CombatPhaseTransitionPresenter.cs:33-37 (민형님 코드)
-            bool entersCombat = item is CombatTurnEvent { Kind: CombatEventKind.CombatEntered };
-            bool returnsToMorning = item is CombatTurnEvent { Kind: CombatEventKind.NightEnded } ||
-                item is CombatOutcomeEvent { Outcome: { Outcome: StageOutcome.Victory } };
-            if (!entersCombat && !returnsToMorning)
-                return false;
+            CombatPhaseTransitionKind transition = CombatEventTypeTable.TransitionOf(item);
+            if (transition == CombatPhaseTransitionKind.None)
+                return false;
+            bool entersCombat = transition == CombatPhaseTransitionKind.Night;
```

- 어떤 사건이 전환을 거는지는 표가 답하고, 전환 재생·완료 합류·Scope 대여는 민형님 코드 그대로다.
- 민형님 시험 `CombatPhaseTransitionPresenterTests`(317줄)는 `TryBegin` 서명·동작이 같아서 바뀌지 않는다.
- 민형님 줄이라 고친 뒤 공유 문서에 적는다. 민형님 확인을 받을지는 Q-V17이다.
- **구현 메모(V2-d, 2026-10-24):** 위 diff 말고도 민형님 줄 `:4`의 `using …Contracts.Common`(`StageOutcome`을 더 쓰지 않음)을 `using …Playback.Ordering`으로 바꿨다. W7-1 스케치와 다르게 한 것도 셋 있다(기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-d" 실행 기록).
  - 줄 만드는 함수 `Row<T>`와 줄 형식 `TypeRow`는 이름을 나눴다(같은 이름이면 CS0102).
  - 포효의 공격 출처는 `: default`가 아니라 명시한 null이다(`default`면 빈 튜플이 출처가 됨).
  - `HandlerSlotOf`만 표에 없는 형식에 예외다.

## 5. W6 조정자 나누기 (V-M4·V-L10·V-M12 남은 것)

**조정자에 남길 것(바꾼 뒤):** Framework 수명, 씬 수명(배경·루트 찾기), 연결 수명(표시 인수·Detach·StopInput·씬 루트 구독), Tick의 결과 소비 순서, 입력 Layer 맞춤, 입력 사실 조립(`ReadFacts`), 진행 사실 호스트.
- 옮기는 것은 약 200줄이고 필드는 약 45개에서 약 30개가 된다. 남는 크기는 약 1050줄이다(그중 문서 주석 약 300줄).
- 목적은 줄 수보다 책임 목록을 줄이는 것이다. V-M4가 든 책임(BattleChanger·Pack 직접 조작, 완료 배경음, 튜토리얼 세션 해석, 재고 읽기, 타임라인 길이)이 모두 나간다.
- 평가자가 크기만으로 중간을 남길 수 있다(§8).

### W6-1 연결 입력 묶음 `CombatSceneInput` (`V/Input/`)

**지금:** 조정자가 연결마다 입력 객체 여덟을 만들고(`CreateInput` `:640-659`), 정리하고(`StopInteraction` `:752-769`), 입력을 나눈다(`RouteInput` `:1154-1173`). 대부분 현강님 줄이다(덱 입력·환경 호버·확인창·`RouteInput`).

**바꾼 뒤:** 연결 한 번의 입력 객체를 하나의 묶음이 갖는다. 묶음은 입력 Factory가 만든다.

```csharp
// 새 파일 V/Input/CombatSceneInput.cs
/// <summary>씬 연결 한 번의 입력 객체(대기 관문·입력 기능 묶음·Router·덱/아이템 입구·환경 호버·거절 안내)와 그 정리 순서입니다(View 2라운드 W6-1).</summary>
internal sealed class CombatSceneInput
{
    internal CombatCommandGate Gate { get; }
    internal CombatInteraction Interaction { get; }
    internal ICombatDeckPlacementInput DeckPlacement { get; }
    internal ICombatDeckHoverInput DeckHover { get; }
    internal ICombatItemInput ItemInput { get; }

    /// <summary>입구 검사를 통과한 입력 하나를 나눕니다(지금 조정자 RouteInput의 본문, 배치 확인 갈래 포함).</summary>
    internal void Route(CombatInputRouter.Request request, float screenX, float screenY) { … }
    internal void OnResult(CombatResult result) { … }          // 거절 안내
    internal void CancelPresentation() { … }                  // 환경 호버 + 묶음 CancelInputs
    /// <summary>덱·아이템·환경 호버·묶음을 정리합니다. 하나가 실패해도 나머지를 시도합니다(지금 StopInteraction 순서 그대로).</summary>
    internal void Stop() { … }
}
```

- 조정자의 필드 여덟(`_gate`·`_interaction`·`_router`·`_deckPlacement`·`_deckHover`·`_itemInput`·`_environmentHover`·`_actionNotices`)이 `_input` 하나가 된다.
- `ICombatViewService.DeckHover`·`DeckPlacement`·`ItemInput`(현강님 UI가 읽음)은 서명 그대로 `_input`의 값을 돌려준다.
- 배치 확인창(`CombatDeploymentConfirmation`)은 서비스 수명이라 조정자에 남는다. 입력 수신 창구가 `IsModalOpen`을 묻기 때문이다. 확인창이 부르는 진입 제출·취소·안내 콜백은 묶음의 메서드를 부른다.
- 동작은 같다. 같은 문장을 같은 순서로 옮긴다.
- **구현 메모(V2-e, 2026-10-24):** 계획 TP-6·TP-7대로 덱 호버와 배치 확인 갈래는 조정자에 남았다. 그 밖에 다른 것은 셋이다(기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-e" 실행 기록).
  - `Stop`은 `Stop(ref List<Exception> errors)`이다(실패를 조정자의 목록에 모음).
  - `RouteCommand`·`SyncEnvironmentHover`·`HasEnvironmentHover`가 더해졌다.
  - HUD 아이템 해제 연결은 조정자가 걸고 푼다.

### W6-2 입력 표면 맞춤 `CombatInputSurfaces` (`V/Input/ActionButtons/`)

- 지금 조정자의 `SyncActionButtons`(`:1190-1204`)·`HideActions`(`:744-749`)·`SyncEnvironmentHover`(`:1176-1183`)와 BattleChanger 입력 연결(`:527`·`:693`)을 옮긴다.
- 맡는 것: 배치 확인창 맞춤, BattleChanger·Pack 보임, 행동 버튼, 환경 호버 허용.
- 판단은 1라운드 W2의 `CombatInputPolicy.IsInputBlocked` 하나를 그대로 쓴다.
- 조정자는 `_surfaces.Sync()`·`_surfaces.Hide()`만 부른다. 부르는 자리(Tick 끝·입력 직후·입력 닫힘·정지)는 지금과 같다.
- GlobalSystem의 `BattleChangerUI`·`PackUI` 서명은 바꾸지 않는다(1라운드 P2).

### W6-3 표시 값 읽기 `CombatDisplayValueReader` (`V/Display/`)

- 지금 조정자의 `ReadDisplayValues`(`:878-887`)·`CurrentStageKey`·타임라인 길이 상수(`:73-75`)·재고 읽기(`ReadItemStock` `:1092`)를 옮긴다.
- 그래서 조정자는 `_currency`·`_items`·`_combatQueries`를 갖지 않는다.
- 타임라인을 Core에 직접 묻는 것(VR6 대기)은 그대로다. 묻는 자리만 이 읽기 하나로 모인다.

### W6-4 튜토리얼 막힘 보고 `CombatBlockedInputReport` (`V/Input/Assist/`)

- 지금 조정자의 `ReportBlockedInput`(`:1027-1038`, 호진님 줄)을 옮긴다.
- 튜토리얼 상태 읽기·요청 쓰기 Sink 둘은 이 클래스만 갖는다. Router는 지금처럼 대리자 하나를 받는다.

### W6-5 완료 배경음은 표시로

```diff
 // V/Display/ICombatBattleDisplay.cs
-        void ShowCompletion(BattleCompletionChanges completion, Action confirmed);
+        /// <summary>결과 화면을 엽니다. 확인을 누르면 <paramref name="confirm"/>을 부르고, 참이면 완료 뒤 배경음으로 바꿉니다.</summary>
+        void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm);

 // V/CombatViewSystem.cs ShowCompletion 호스트(바꾼 뒤): 배경음 두 줄(현강님 :955-956)이 표시로 간다
-                _audio?.SetPreset(AmbgmEvent.BGM, CombatEventSounds.BgmPreset(CombatBgmPhase.AfterCompletion));
-                if (_audio != null && !_audio.IsPlaying(AmbgmEvent.BGM)) _audio.Play(AmbgmEvent.BGM);
```

- 표시는 이미 국면 배경음(`CombatSceneDisplay.cs:203-204`)과 승리 효과음(`:274`)을 낸다. 완료 뒤 배경음도 같은 자리로 모인다.
- 그래서 조정자는 `_audio`를 갖지 않는다.
- 확인 판단(같은 연결·같은 완료·진행 사실의 Confirmed)은 조정자의 `confirm`이 그대로 맡는다.
- 이 계약을 구현하는 시험 가짜는 우리 `CombatViewSystemTests.FakeDisplay`(`:1600`) 하나다.
- **구현 메모(V2-f, 2026-10-24):** 계획 TP-10대로 타임라인 조회는 `BindServices`의 필수 인자다. 튜토리얼 짝 검사는 보고 객체 생성자로 옮겼다. 표시 `ShowCompletion`은 `confirm` null을 인자 예외로 거른다(기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-f").

### W6-6 생성 주입 (V-L10)

```csharp
// 새 파일 V/Display/ICombatSceneFactory.cs (표시 생성 계약)
internal interface ICombatSceneFactory
{
    StageViewPropPresenter CreateBackdrop(CombatSceneRoot root, CombatDisplayTables tables);
    CombatSceneDisplay CreateDisplay(CombatSceneRoot root, CombatDisplayTables tables, CombatDisplayServices services, StageViewPropPresenter backdrop);
}

// 새 파일 V/Input/ICombatSceneInputFactory.cs (입력 생성 계약)
internal interface ICombatSceneInputFactory
{
    CombatSceneInput CreateSceneInput(/* 표시·Buffer·사실 공급자·서비스 — 지금 CreateInput이 넘기는 것 */);
}
```

```diff
 // F/InGameViewSystemFactory.cs:60 (연결 줄, 호진님 줄)
-            var combatView = new CombatViewSystem(faults, new OrderedCombatPresentation());
+            var combatView = new CombatViewSystem(faults, new OrderedCombatPresentation(),
+                CombatSceneFactory.Instance, CombatSceneFactory.Instance);
```

- 구현은 두 계약을 함께 구현하는 `sealed` 클래스 하나다(`F/CombatSceneFactory.cs`, 상태 없음, `Instance` 하나). 지금 정적 Factory 둘(`CombatSceneDisplayFactory`·`CombatSceneInputFactory`)에 위임한다.
- 정적 Factory 둘은 그대로 둔다. 시험이 정적 메서드(`CombatSceneInputFactory.CreateInteraction` 등)를 직접 부르기 때문이다.
- 조정자는 생성자로 받은 계약만 부른다. 정적 Factory 호출 다섯 곳이 없어진다.
- 생성자 인자는 필수다. 선택 인자로 두면 V-M12와 같은 꼴이 되기 때문이다.
- `CombatViewSystem`을 만드는 시험 일곱 곳(`CombatViewSystemTests:183·184·206·878·1336`, `CombatDisplayReuseTests:578`, `CombatMovementViewTests:585`)은 모두 우리 줄이다. 운영 구현 `Instance`를 넘기게 호출만 맞춘다.
- 이 줄은 민형님 영역의 연결 줄이라 민형님 공유 문서에 적는다(CLAUDE.md 영역 규칙). 호진님이 쓴 줄이라 호진님 메모에도 적는다.

### W6-7 운영 입구와 시험 입구 (V-M12 남은 것)

**지금:** 운영 입구 `PostBindService`가 시험 입구 `BindServices`를 부른다. 그 입구의 선택 인자 넷이 null이면 기능이 조용히 꺼진다. `_audio`·`_combatQueries`는 PostBind에서만 채워서, 시험 조립에서는 늘 null이다.

**바꾼 뒤(Q-V16 A):**
- `BindServices`의 선택 인자 넷(텍스트 표시·재고·튜토리얼 둘)을 필수로 바꾼다.
- `_audio`·`_combatQueries`는 W6-3·W6-5로 조정자에서 없어진다. 남는 받는 자리는 표시 값 읽기(타임라인)와 표시 서비스 묶음(오디오)이다.
- `CombatDisplayServices`의 선택 인자 넷(오디오·화면 전환·UI·카메라 효과)을 필수로 바꾼다.
- 입력 Factory의 알림 생략(`text == null ? null : …`)을 지운다. 텍스트 표시가 필수가 되기 때문이다.
- 그러면 운영 코드의 `?.` 검사(조정자 `_items?.`·`_tutorialState?.`·`_textPresentation != null`, 표시 Factory `services.ScreenTransitions != null` 등)가 없어진다.
- 시험은 가짜를 넘긴다. 가짜는 시험 어셈블리에 이미 있는 것(오디오·화면 전환·UI·텍스트·튜토리얼)을 쓰거나, 우리 시험 도우미에 모은다.

**시험 영향:**
- `BindServices`를 부르는 곳 셋: 우리 `CombatDisplayReuseTests:580`, 천명님 `CombatMovementViewTests:423`, 천명님 `CombatViewSystemTests:1338`.
- `new CombatDisplayServices(`를 부르는 곳 열둘: 우리 열, 민형님 `CombatSceneEventPresenterTests:173`, 현강님 `CombatSceneEventPresenterTests:1449`.
- 다른 사람 줄 넷은 인자만 더한다. 고치기 전에 허락이 필요하다(Q-T5).

**구현 메모(V2-g, 2026-10-25):** 계획대로 했다. 다른 점은 셋이다(기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-g").
- 시험 조립도 View DB를 받아서(계획 TP-15 a: 실제 DB를 한 번 읽어 둠), "DB 전 연결 거절" 시험 하나만 DB 없는 조립을 따로 만든다.
- 표시 생성자도 오디오를 필수로 검사한다.
- 조정자의 Factory `using`을 지웠다.

위 W6-6의 "`CombatMovementViewTests:585` … 모두 우리 줄"은 틀렸다. 천명님 `:586`이고, 계획 Q-T6 c로 허락받았다.

V2-g 검토가 null 허용 둘을 더 찾았다. 2026-10-25 사용자 결정(권장안 A)은 이렇다(기록은 [계획](../plans/2026-10-24-combat-view-stage-round2-plan.md) "Phase V2-g" 끝).
- **Q-V2g-1 A:** `CombatSceneEventPlayback` 생성자의 선택 인자 셋(화면 전환·카메라 창구·카메라 연출 값)을 필수로 하고, 닿지 않게 된 `?.`를 걷었다. 민형님 줄 일곱이다(Factory 공유 §24). 컴파일 미확인이다.
- **Q-V2g-2 A:** 조정자 `AttachCreated`의 시험 입구 null 허용(`root`·`inspection`)은 남긴다. 운영 연결이 아니라 가짜 표시로 붙이는 시험 입구라서다. 계획 TP-17에 더했다.

**남는 것:** `CombatSceneEventPresenter` 생성자의 선택 인자(오디오·카메라·이펙트·몸 조회 등 약 20개, V-L8)는 이번에 두지 않는다. 대부분 연출 값이라 3라운드 W9가 설정 묶음으로 옮길 때 함께 필수로 한다(P17). 그래서 평가자가 이것을 V-M12의 남은 꼴로 볼 수 있다(§8).

## 6. 정할 것

권장은 A다.

| # | 묻는 것 | A(권장) | B | C |
|---|---|---|---|---|
| **Q-V9** | 시작 전 1라운드 커밋 | 사용자가 1라운드를 커밋한 뒤 2라운드를 시작한다(A/B 기준 판) | 커밋 없이 이어서 함(측정 기준은 스크래치 사본) | — |
| **Q-V10** | 2라운드 범위 | W5 → W7 → W6, 한 라운드 | W5·W7만 하고 W6은 따로 | — |
| **Q-V11** | 공통 계약의 꼴 | 추상 기본 클래스 + 기능 파일 안의 어댑터. 기능 메서드·묶음 공개 멤버 그대로라 다른 사람 시험 무변경 | 기능이 기본 클래스를 바로 상속하고 메서드 이름을 바꿈(천명님·호진님·현강님 시험 다수 수정) | — |
| **Q-V12** | 포인터 대상 판정 | 판정식 하나는 그대로 두고 사실 하나만 받음. 소유자는 자기 대상을 선언(W5-2) | 소유자마다 "이 포인터를 받나" + 우선순위 목록(규칙이 소유자 넷에 나뉨) | — |
| **Q-V13** | 응답 전달(V-L15) | 두 시점을 계약의 두 단계(감시·응답)로 이름 붙이고 조정자는 입구 하나(동작 같음, W5-3) | 하나로 합침. 소유자마다 "이미 쓴 번호" 기억을 더하고 경로 소비자 셋의 다시 시도를 바꿈(이동 입력 동작 위험) | — |
| **Q-V14** | 아군 호버 층(V-L14) | 층을 나눔(색 같음, 아웃라인 손잡이 +1) | 나누지 않고 전경 규칙 한 곳만 | — |
| **Q-V15** | 공격 Drag 넘겨받기(호진님 코드) | 소유자 하나로 옮김(W5-5) | 묶음에 그대로 | — |
| **Q-V16** | 시험 입구 null 허용 | 운영 입구를 모두 필수로, 시험이 가짜를 넘김(W6-7) | 운영은 private 필수 입구, 시험 입구는 운영 어셈블리의 널 객체로 채움(시험 무변경, 시험용 형식이 운영에 생김) | 그대로 |
| **Q-V17** | 화면 전환 사건 고르기(민형님 줄 다섯, `:33-37`) | 표가 답하게 바꾸고 공유 문서에 적음(W7-3) | 민형님 코드 그대로(형식 지식 한 곳이 남음) | — |
| **Q-V18** | 사건 형식 표의 꼴(W7) | 형식마다 한 줄인 정적 표 하나(경로 표를 넓힘), 처리기는 연출만 | §6 원안대로 처리기 멤버로 옮김(사실 처리기 하나가 형식 열넷을 맡아 그 안에 switch가 남음) | — |
| **Q-T4** | 우리 시험 | 걷은 기능·바뀐 동작은 지우고 나머지는 호출만 맞춤. 바뀐 동작은 아웃라인 손잡이 수 네 단언뿐 | — | — |
| **Q-T5** | 다른 사람 시험 고침 허락 | 천명님 셋(`CombatMovementViewTests:423`·`:647` 가짜 한 줄, `CombatViewSystemTests:1338`), 민형님 하나(`CombatSceneEventPresenterTests:173`), 현강님 하나(`:1449`). 모두 인자·멤버 한 줄씩 | 허락 안 함 → Q-V16 B, W5-4는 정적 도우미로 | — |

## 7. 설계에서 정한 것 (확인 대상)

- **P8** 입력 묶음과 기능 클래스의 바깥 서명은 그대로 둔다. 바뀌는 것은 안쪽(목록을 도는 것)뿐이다. 묶음의 한 인자 `Poll(preview)`는 호진님 시험 때문에 남긴다(V-L18 그대로).
- **P9** 소유자 목록의 순서는 입력 Factory 한 곳이 정한다: 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버. 순서 표(§3 W5-1)를 그 자리 주석에 둔다.
- **P10** 같은 포인터 대상을 선언한 소유자가 둘이면 묶음을 만들 때 예외다.
- **P11** `CombatPointerInput`·`CombatInputTick`은 `readonly struct`이고 `in`으로 넘긴다. 입력마다 할당하지 않는다. (V2-b 구현에서 `CombatInputTick`은 만들지 않았다. §3 W5-1 구현 메모.)
- **P12** 새 층 이름은 `AllyHoverRange`, 그리는 순서는 `AttackRange` 바로 다음이다.
- **P13** Buffer의 조건 없는 `ClearPreview()`는 남긴다(Buffer `Clear`와 다른 작업자 시험이 씀).
- **P14** 사건 형식 표의 이름은 `CombatEventTypeTable`이고, `Playback/Ordering/`에 둔다. Core의 사건 종류 열거 `CombatEventKind`와 헷갈리지 않게 "Kinds"를 쓰지 않는다. 옛 `CombatEventRoutes`·`CombatActionBoundary` 파일은 `.meta`와 함께 지운다(우리 파일, 쓰는 곳은 모두 옮김). 경로 열거 `CombatEventRoute`는 그대로 둔다.
- **P15** 새 파일 위치는 폴더 배치 안내를 따른다. 루트에는 두지 않는다.
  - `Input/Interaction/`: `CombatInputOwner`·`CombatPointerInput`·`CombatAttackDragHandover`·`ICombatAttackDragTarget`(`CombatInputTick`은 V2-b에서 만들지 않음)
  - `Input/`: `CombatSceneInput`·`ICombatSceneInputFactory`
  - `Input/ActionButtons/`: `CombatInputSurfaces`
  - `Input/Assist/`: `CombatBlockedInputReport`
  - `Display/`: `CombatDisplayValueReader`·`ICombatSceneFactory`
  - `Playback/Ordering/`: `CombatEventTypeTable`
  - `F/`: `CombatSceneFactory`
- **P16** 생성 계약 둘(표시·입력)의 운영 구현은 상태 없는 클래스 하나의 `Instance`다. 조정자 생성자는 두 계약을 따로 받는다. 시험도 그 `Instance`를 넘긴다(가짜 Factory를 새로 만들지 않음).
- **P17** `CombatSceneEventPresenter`의 선택 인자는 3라운드 W9에서 설정 묶음과 함께 필수로 한다.

## 8. 원칙 점검과 예상

**SSOT.**
- 수명 단계의 순서가 목록 하나가 된다(W5-1).
- 전경 전환 규칙이 한 메서드가 된다(W5-1).
- 응답 전달의 두 시점이 계약 한 곳에 적힌다(W5-3).
- 사건 형식 지식이 표 하나가 된다(W7).

**SOLID.**
- 입력 기능 하나를 더할 때 고칠 곳이 열 곳에서 기능 파일 하나와 Factory 목록 한 줄이 된다. 의도 입구가 있는 기능이면 묶음 메서드가 더해진다.
- 사건 형식 하나를 더할 때 고칠 곳이 5~6곳에서 표 한 줄이 된다.
- 조정자의 책임이 수명·결과 순서·Layer·사실·진행 호스트로 줄어든다.

**FW 계약.** 조정자가 정적 Factory를 부르지 않는다(W6-6).

**있는 기능.** 미리보기 비우기를 Buffer 멤버 하나로 한다(W5-4).

**독립성.** 층을 나눠 호출 순서 의존을 없앤다(W5-6). 아이템 드래그도 사실로 넘긴다(W5-2). 응답 전달의 두 시점은 계약에 적히지만 둘인 채 남는다(W5-3).

**불필요·우회.** 운영 입구의 null 허용을 없앤다(W6-7). 넘겨받기의 흩어진 `Invalidate` 아홉 줄 중 여섯이 없어진다(W5-5).

**새로 생길 수 있는 문제(예상).**
- 기능 다섯마다 어댑터가 생긴다. 메서드를 한 줄씩 넘기는 코드다. 평가자가 "같은 일 두 입구"로 볼 수 있다(낮음 하나로 셀 수 있음).
- 묶음에 기능 이름을 쓰는 의도 입구가 남는다(덱·아이템·공격·복귀·취소). V-M5가 낮음으로 남을 수 있다.
- 사건 형식 표가 크다(약 20줄, 줄마다 함수 여럿). 표 하나라 SSOT에는 좋지만 "큰 정적 표"로 볼 수 있다.
- `CombatSceneInput`이 연결 입력의 새 허브로 보일 수 있다. 조정자의 입력 절반을 그대로 옮긴 것이기 때문이다.
- 표시 계약 `ShowCompletion`의 대리자 형식이 바뀐다(가짜 하나).

**예상 점수(알려진 문제 기준, §0의 주의대로 측정은 더 낮을 수 있음):**

| 기준 | 1라운드 뒤(측정) | 2라운드 뒤(권장안) | 근거 |
|---|---|---|---|
| SSOT | 9.25 | 9.25 | 이번 묶음의 SSOT 이득은 SOLID·독립성 문제를 푸는 방법이다(같은 문제는 한 기준에서만 셈) |
| SOLID | 6.25 | 8.5 | 중간 셋(V-M4·V-M5·V-M7) 해결, V-M6 남음, 낮음(V-L7·어댑터·층 다섯 곳 등) −0.75. V-M4가 크기 때문에 중간으로 남으면 7.75 |
| FW 계약 | 9.5 | 9.75 | V-L10 해결, V-L9 남음 |
| 있는 기능 사용 | 8.75 | 9.5 | V-M8 해결, 낮음 셋 남음(V-L11·`Boom` 고정·V-L12) |
| 독립성 | 8.25 | 8.5 | 낮음 둘(V-L14·아이템 기본 인자) 해결, V-M10 남음, 낮음 셋 남음(V-L15·`"InvalidMainCharacter"`·마스크 둘) |
| 불필요·우회 | 7.75 | 8.25 | V-M12 남은 것이 낮음으로(Presenter 선택 인자), Ballista 중간 남음, 낮음(V-L18·`lastHit`·손패 파일) |
| **합** | **8.3** | **약 8.95** | 1라운드 설계 §7의 2라운드 예상은 9.2였다 |

- Q-V16 B면 운영 어셈블리에 시험용 널 객체가 생긴다. 불필요·우회에 낮음 하나가 더해질 수 있다(약 8.9).
- Q-V18 B면 사실 처리기 안에 형식 switch가 남는다. V-M7이 낮음으로 남을 수 있다(SOLID 약 8.25, 합 약 8.9).
- Q-V13 B(하나로 합침)면 V-L15가 풀려 독립성 약 8.75, 합 약 9.0이다. 대신 이동 입력의 동작 위험이 생긴다.
- 조정자가 크기(약 1050줄) 때문에 V-M4 중간으로 남으면 SOLID 7.75, 합 약 8.8이다.
- 3라운드(W8·W9)까지의 예상은 1라운드 설계 §7과 같게 약 9.5다.

## 9. 다른 사람 줄과 공유 (라운드 끝 문서 Phase, 모두 미전달로 작성)

| 사람 | 바꾸는 줄(blame) | 묶음 |
|---|---|---|
| **현강님** | 조정자의 덱 입력·환경 호버·확인창·`RouteInput`·`HideActions`·`SyncEnvironmentHover`·타임라인 한 줄·완료 배경음 두 줄(약 60줄, 옮김), `PlacementDragInteraction`의 덱 선택·소리(그대로, 어댑터만 더함), 묶음의 덱 입구 셋(그대로), 입력 Factory 알림 한 줄, 시험 `CombatSceneEventPresenterTests:1449`(허락) | W5·W6 |
| **호진님** | 묶음의 공격 Drag 넘겨받기(`:152-206`은 옮김, `Invalidate` 아홉 줄 중 여섯은 어댑터로·셋은 넘겨받기 호출로), Router의 튜토리얼 막힘·`TryHandleAttackDragPointer` 줄(그대로), 경로 출발점 메서드 셋(그대로, 좁은 계약 구현), 조정자의 튜토리얼 보고·`BindServices` 튜토리얼·재고 인자(옮김·필수로), 연결 줄 `InGameViewSystemFactory.cs:60`, `CombatMovementRangeTracker`의 빈 범위 근거(그대로) | W5·W6 |
| **천명님** | 경로 입력 골격(그대로), 조정자의 사실 조립·Layer 맞춤·결과 소비 줄(그대로, 둘레만), 공격 기능의 천명님 줄(그대로), 시험 셋(허락) | W5·W6 |
| **민형님** | `CombatPhaseTransitionPresenter.cs:33-37`(W7-3), 연결 줄 `InGameViewSystemFactory.cs:60`(민형님 공유 문서에 적음), 시험 `CombatSceneEventPresenterTests:173`(허락) | W6·W7 |

- 공유는 1라운드처럼 라운드 끝 문서 Phase에서 쓴다.
  - 현강님 메모 §9
  - 호진님 메모 §11
  - 천명님 Core 전달 문서 §34(요청 없음)
  - 민형님 Factory 공유 문서 §24
- 폴더 배치 안내 갱신: 새 파일 위치, `CombatEventRoutes` → `CombatEventTypeTable`.
- **작성 완료(V2-h, 2026-10-25, 모두 미전달):** 현강님 메모 §9, 호진님 메모 §11, 천명님 Core 전달 문서 §34, 민형님 Factory 공유 §24, 폴더 배치 안내. 위 표는 설계 때의 예상이다. 실제 바꾼 줄(blame)은 그 공유 문서와 계획 Vh2.1~Vh2.4가 원본이다.

## 10. 이번 라운드 밖

- **3라운드 W8:** 공격 종류 전략 하나(V-M6), Ballista 두 경로(V-L20, 지금 중간). `EnemyUnit_Ballista.prefab`은 참조 없는 에셋이라 보고만 했다.
- **3라운드 W9:** 화면 전환 설정(V-M9), 유닛 소리 프리셋(V-M10), 이펙트 Scope(V-L11), 다시 하는 검사(V-L12), Presenter 선택 인자(V-L8·P17).
- **남기는 낮음:**
  - V-L1 생존 판정 사본, V-L2 결과 화면 재계산, V-L4 정리 목록, V-L5 UI 입구 보호, V-L9 `GetUIData`, V-L18 시험 전용 멤버
  - 새로 찾은 낮음: 지뢰·화약통 목록 두 곳, `Boom` 고정, `"InvalidMainCharacter"` 문자열, 마스크 둘, 층 다섯 곳, 표시의 전역 상태
  - `lastHit`(호진님 결과 화면 필드, 켜는 곳 없음): 보고만 했다.
  - 1라운드에서 새로 생긴 낮음 둘: 엔딩 부모 조건 사본, 남긴 손패 파일
- **손패 파일:** 씬 `Hand`의 `HandPresenter` 컴포넌트를 뗄지는 사용자 결정 대기다(1라운드 Q-V5). 이번 라운드와 상관없다.

## 11. 2라운드 측정 (2026-10-25, V2-i)

**방법:** 1라운드 측정([View 단계 설계 §10](2026-10-24-combat-view-stage-design.md))과 같다.
- **범위:** `ViewSystem/Combat/` 전체, `CombatView` Buffer, `CombatEndUI`, 전투 View Factory, `Combat.unity`다. Factory는 1라운드 셋에 2라운드의 새 `CombatSceneFactory.cs`(한 판에만 있음)를 더했다.
- **두 판:** 스크래치패드 `view-ab2/`의 이름 없는 폴더 둘(M·N)로 꺼냈다. 배정은 무작위였고, 결과는 M = 2라운드 뒤 작업 트리, N = 기준 커밋 `23436b00`이다.
- **평가자:** 하위 에이전트 셋이 두 판을 함께 매겼다(SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회).
  - 지시문은 1라운드 것을 그대로 쓰고 판 이름·파일 수·범위 한 줄만 바꿨다.
  - 어느 쪽이 새 판인지 알리지 않았다. 우리 문서·메모리·시험·git 기록은 읽지 못하게 했다.
- **감점:** 높음 −2, 중간 −0.75, 낮음 −0.25이고, 낮음은 기준마다 −1.0까지만 깎는다.
- **합치기:** 같은 문제는 한 기준에서만 셌다. 기준선(1라운드 설계 §1)에 있던 문제는 그 기준에 두었다. 1라운드 측정에서 새로 찾은 문제는 그때 둔 기준에 두었다.
- **직접 대조:** 중간 이상 주장과 우리 변경으로 생겼다는 낮음은 코드·에셋으로 대조했다. 모두 맞았다. 다만 "호출처 없는 메서드" 둘은 시험만 부르는 멤버라 시험 전용 멤버 항목에 합쳤다.

**점수(합친 뒤):**

| 기준 | N(`23436b00`) | M(2라운드 뒤) | 계획 §0 예상(M) |
|---|---|---|---|
| SSOT | 9.0 | **9.0** | 9.25 |
| SOLID | 5.5 | **7.5** | 8.25 |
| FW 계약 | 9.5 | **9.75** | 9.75 |
| 있는 기능 사용 | 8.75 | **9.5** | 9.5 |
| 독립성 | 8.25 | **8.25** | 8.5 |
| 불필요·우회 | 7.5 | **8.25** | 8.25 |
| **합** | **8.1** | **8.7** | 8.92 |

- N은 1라운드 측정의 Q와 같은 코드다. 그때 8.3, 이번 8.1로 0.2 낮게 나왔다. 평가자가 강조 층 문제를 낮음에서 중간으로 올리고 낮음을 더 찾았기 때문이다. 같은 잣대에서 0.2 안쪽이다.
- M은 예상보다 0.2 낮다. 차이는 셋이다.
  - SOLID −0.75: 강조 층 다섯 곳이 중간으로 올랐다.
  - SSOT −0.25, 독립성 −0.25: 낮음이 더 나와 상한에 걸렸다.
- 평가자 원점수(합치기 전, 같은 문제를 여러 기준이 셈)는 N 7.2 / M 8.2다.

**기준선 문제의 상태(M):**
- **해결:**
  - V-M4(조정자): 중간 → 낮음으로 남음(1159줄)
  - V-M5(입력 묶음 허브), V-M7(사건 형식 분류기), V-M8(미리보기 비우기 일곱 곳), V-M12(운영 의존 null 허용)
  - V-L10(정적 Factory 직접 호출), V-L14(아군 호버 층)
  - 1라운드 새 낮음 "아이템 드래그 기본 인자"
- **남음(3라운드 대상):**
  - V-M6(공격 종류 분기, SOLID 중간)
  - V-M10(소리 프리셋 switch, 독립성 중간)
  - Ballista 두 경로(V-L20, 불필요 중간). 관통 접촉 일정의 두 벌도 같은 뿌리로 여기서만 셌다.
  - V-M9(화면 전환 설정, 있는 기능 낮음)
  - 낮음: V-L2·V-L7·V-L9·V-L18, 위험 종류 목록 세 곳, `Boom` 고정, `"InvalidMainCharacter"`, `lastHit`, 표시의 전역 상태, 엔딩 부모 조건 사본, 남긴 손패 파일
- **TP-17로 남긴 것:** 하위 부품의 시험용 null 허용(`CombatSceneEventPresenter` 선택 인자, `AttachCreated`의 `root`·`inspection`). 평가자도 M에서는 낮음 하나로 매겼다.

**등급이 바뀐 것:** 강조 층 하나의 속성이 다섯 곳에 있다. 열거·그리는 순서·색·채움·행동 중 층 마스크에 평소 층 마스크와 호버 색조까지다. 1라운드에서는 SOLID 낮음이었는데 평가자 둘이 중간으로 매겼다. 마스크 하나를 빠뜨리면 GridOutline에서 그 층이 조용히 안 보인다. 2라운드가 `AllyHoverRange`를 더할 때 여섯 곳을 고친 것(V2-a 구현 메모)이 그 예다.

**새로 찾음(기존 코드, 두 판 모두, 낮음):**
- 주체 막기 규칙 사본: `CombatStepPlayer`와 `CombatSubjectBlocking`(SSOT)
- 레일웨이 판별을 형식 표와 `CombatEventSounds`가 따로 함. 표 밖에 남은 형식 지식이다(SSOT)
- 가시밭 프리팹이 없어도 DB 검사가 거르지 않아, 가시밭이 있는 전투에서야 실패함(독립성)
- 한 슬롯의 배경 요청 순서가 목록 순서에 기댐(독립성, V-L15 계열)
- `CombatUnitPresentationWindow` 멤버 약 30개(SOLID)
- 씬에서 비어 있는 선택 참조 둘: `CombatActionPanel.attackActiveIndicator`, `CombatSceneRoot.worldCamera`. 대체 경로만 쓰인다(불필요)
- 보고만: DB `fieldDisplayMode`가 GridOutline이라 CellPrefab 렌더러가 돌지 않음(데이터 선택이라 감점 안 함)

**새로 생김(우리 변경, M만, 낮음):**
- SSOT
  - 묶인 씬 루트가 조정자 `_subscribedRoot`와 입력 표면 `_root` 두 필드에 있다.
  - 경로 정지를 묶음의 `_path = null`과 경로의 `IsDetached` 두 사실로 안다(계획 TP-5에서 받아들인 것).
- 독립성: 14인자 입력 생성 서명이 세 벌이다(계약·어댑터·정적 Factory). 새 의존 하나에 네 곳을 고친다(컴파일러가 잡음).
- SOLID
  - `CombatInputOwner`가 빈 가상 메서드 15개의 두꺼운 기반이다(설계 §8에서 예상한 어댑터 낮음).
  - 생성 계약이 구체 `CombatSceneDisplay`를 돌려준다.
  - 정적 Factory와 인스턴스 어댑터, 생성 입구가 둘이다(계획 TP-14·P16).
- 불필요
  - 배치 생성자의 쓰지 않는 `requests` 인자. V2-a에서 남기고 V2-b·V2-g에서 보기로 했는데 걷지 않았다.
  - 번호 없는 `ClearPreview()`의 운영 소비자 없음(P13로 남김).

**3라운드 방향에 주는 것:**
- W8(공격 종류 전략)은 V-M6과 Ballista 두 경로(관통 접촉 일정 두 벌 포함)를 함께 푼다.
- W9(값은 DB로)는 V-M10·V-M9·`Boom` 고정을 푼다.
- 강조 층 다섯 곳(이제 중간)은 1·2라운드 묶음에 없었다. 층 하나의 속성을 한 곳(층 표)에 두는 일을 3라운드 후보로 더할지 정해야 한다.
- 우리 변경이 남긴 낮음 중 작은 것(쓰지 않는 `requests` 인자, 씬 루트 두 필드)은 3라운드 첫 Phase에서 함께 걷을 수 있다.
