# 전투 유닛 클립 칸 다시 짜기 — Task 계획

- 날짜: 2026-11-17
- 설계: [clip-slot-redesign-design](../specs/2026-11-17-combat-unit-clip-slot-redesign-design.md)(CS1~CS11, 결정 Q-A1~Q-A7·Q-B2, P1~P19 — 11-17 사용자 "확정")
- 상태: **계획 작성, 코드 착수 전.** 계획에서 정한 것 TP-1~TP-14는 사용자 확인 대상(§1).
- 경로 줄임: `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`, `DE/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Editor/`, `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `B/` = `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/`

## 0. 진행 규칙

- Phase마다 멈추고 보고한다. 커밋·push는 사용자.
- 전투 View 작업이라 **새 시험 없음·MSBuild 없음**, 보고에 "컴파일 미확인". `Assets/Tests`에 바뀌는 형식을 쓰는 시험은 없다(11-17 grep 0) → 시험 변경 예상 없음.
- 위험은 컴파일보다 **부트 DB 생성 검사**다. 옮기기 도구를 돌리기 전 판은 새 칸이 비어 부트가 멈춘다 → Phase마다 "코드 → 사용자가 도구 실행 → 부트·재생 확인" 순서.
- 묶음 끝 검토는 CS-e에서 새 에이전트 하나로 한 번(메모리 규칙).
- 고칠 파일은 모두 용근 줄. 프리팹(민형님 새 모델 프리팹)의 컴포넌트 값이 바뀌어 공유가 필요하다.

## 1. 계획에서 정한 것(TP, 사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 옮기기 도구는 `DE/CombatClipSlotMigration.cs` 하나(메뉴 `ProjectL > Combat > 클립 칸 옮기기`), Phase마다 단계 메뉴를 하나씩 더한다(1 지점·큐 / 2 클립 묶음 / 3 시작 기준). 단계마다 다시 돌려도 결과가 같다 | internal 형식을 쓰려면 Systems 어셈블리 안(`#if UNITY_EDITOR`, 묶음 SO 에디터와 같은 자리). Q-A5 "한 번"은 단계마다 한 번 |
| TP-2 | **Hit → Contact 합치기(CS10)는 CS-a로 앞당김**(설계 Phase 표는 CS-c). 근접 `Contact`(줄마다)만 CS-b | `CombatCueAnchor`·큐 규칙·큐 입력이 CS3 이름 바꾸기와 같은 파일, 옮기는 에셋도 같은 도구 단계 |
| TP-3 | 옛 칸은 `[SerializeField, HideInInspector, FormerlySerializedAs 없이 원래 이름]`으로 한 Phase 남겨 도구가 읽고, 다음 Phase 첫 Task에 지운다(P11). DB는 처음부터 새 칸만 읽는다 | 옛 칸을 바로 지우면 도구가 읽을 값이 YAML에만 남아 `SerializedObject`로 못 읽음 |
| TP-4 | `contactFilter` 칸은 이펙트 큐 항목·흔들림·소리·플래시 큐 항목 모두에 둔다(Contact 시점일 때만 봄, 기본 `Every`). `Every`는 이펙트만 허용 | 옛 `Hit`을 쓰던 종류(흔들림·소리·플래시)가 `FirstHit`으로 옮겨 와야 함 |
| TP-5 | 큐 자리 `ActorEventPoint`의 회전 칸 `eventPointRotation`은 이펙트 큐에만(소리는 자리를 위치로 안 씀) | 소리 큐의 자리는 지금도 효과 없음 |
| TP-6 | 이벤트 지점 번호 옮기기(P5): 큐 `point: 2`(총구) → 1 + `Actor` + `eventPoint = 2k`, `point: 1`(이펙트) → 1 + `Point` + `eventPoint = 2k+1`. 옮길 큐 에셋 = `CombatAttack_Akimbo`·`CombatAttack_Shotgun`(11-17 grep), 프리팹 = `CombatAnimationEvents`가 붙은 14개 | 지금 쓰는 곳 전부 |
| TP-7 | 동작 칸 값 형식 이름: `CombatClipMotion`·`CombatClipStage`·`CombatClipBlend`·`CombatClipEventEntry`, 입력 `…Input`(직렬화 클래스), 파일은 `DV/`에 하나씩 | 설계 CS1 가칭 그대로 |
| TP-8 | 묶음 SO 필드 이름: `attack`·`hit`(동작 입력), 공격 전용 `motionStart`·`motionStartDelay`. 옛 `attackClip` 등 9칸은 TP-3대로 숨겨 둠 | |
| TP-9 | 근접 여러 줄의 앞 줄 접촉은 연발과 같은 조각 `CombatVolleyReleasePiece`를 근접 조립에서도 쓴다(접촉 순번 = 줄 번호, 한 줄의 대상은 공격 모양의 대상 순서대로, 마지막 대상이 그 줄의 마지막 접촉) | 해제·숫자 나눔·앞 접촉 피격 규칙을 한 벌로 |
| TP-10 | 뒤 단계·페이드아웃 꼬리 진행: `FieldUnitView.Update`에서 `_clipChannel?.TickTail(Time.deltaTime)`, 꼬리가 없으면 바로 돌아감 | P10. 표시 쪽엔 지금 프레임 Tick 없음 |
| TP-11 | 채널 섞개 입력: 0 Idle · 1·2 위층(번갈아 씀). 대여 하나 안에서 단계가 바뀌면 다른 위층에 새 단계를 붙이고 Between 블렌드 | CS7 |
| TP-12 | 선행 구간 알림(CS11): 사건 Presenter가 처리기 시작 직후 한 곳에서 `CombatPreAttackPresentationStarts.EventStarted(사건 위치, 세대)`를 부르고, 처리기가 그 사건을 연출 없이 바로 끝냈으면(시작 함수가 그 프레임에 완료) 부르지 않음(P19) | 입구 셋(발동 텍스트·당김 레인·휩쓸기 걸음) → 하나 |
| TP-13 | `CombatPreAttackTraitMap`은 허용 목록 없이 "그 공격의 발동 중 공격 사건 앞 + 휩쓸기 머리"의 가장 앞 사건 위치만 계산하도록 줄인다(당김 경로 일치 검사 등 소속 검사는 남김) | 종류를 안 봄 |
| TP-14 | 카우보이 PullBack 연결(CS-c)·블렌드 값 넣기(CS-d)는 Claude가 묶음 에셋 YAML에, 시각 조정은 사용자 | 지금까지 묶음 에셋 고친 방식 |

## 2. Phase

### CS-a 이름·이벤트 지점·Hit 합치기 (화면 같음)

| Task | 내용 | 파일 |
|---|---|---|
| Ta.1 | `CombatCueAnchor`: `Cue → Event1`, `Cue2 → Event2`, `Cue3 → Event3`(값 2·6·7), `Hit = 3` 삭제, 툴팁. `CombatClipEventId.Event → Event1`(값 1). 이름표 함수는 그대로(P1) | `DV/CombatCueAnchor.cs`, `DV/CombatClipTimings.cs`, `DV/CombatClipEventIds.cs` + 이름 쓰는 곳 전부(`C/Presentation/Pieces/` 다섯·`CombatCodeMotionAttack`·`TraitActivationVisual` 등, §1 grep 목록) |
| Ta.2 | 새 `CombatContactFilter { Every = 1, FirstHit = 2 }`, 큐 항목 넷에 `contactFilter`(TP-4), 큐 규칙: `Contact`+`Every` = 이펙트·접촉점·지연 0, `Contact`+`FirstHit` = 옛 `Hit` 규칙. 플레이어 스킬 칸 규칙도 `Contact`+`FirstHit` | `DV/CombatContactFilter.cs`(새), `DV/CombatCueEntries.cs`, `DV/CombatEventCues.cs`, `DV/CombatPresentationCueRules.cs` |
| Ta.3 | View 기준 시점 표: `CombatCueAnchors`가 (Contact, FirstHit)을 옛 Hit 신호에, (Contact, Every)는 지금처럼 공격 재생 `PlayImpact`. `PlayImpact`는 `Every`만 | `C/Presentation/Pieces/CombatCueAnchors.cs`·`CombatCueComposer`(있는 곳), `C/Presentation/Attack/CombatAttackPlayback.cs` |
| Ta.4 | 이벤트 지점: `CombatAnimationEvents`에 `eventPoints : Transform[]`, 옛 `muzzle`·`effectPoint`·`additionalAttackPoints` 숨김(TP-3). 값 `CombatEventPoints`(`CombatAttackPoint.cs` → `CombatEventPoints.cs`, `git mv`로 GUID 유지) | `DV/CombatAnimationEvents.cs`, `DV/CombatAttackPoint.cs` |
| Ta.5 | 큐 자리 `CombatCuePoint`: `ActorEffectPoint = 1` → `ActorEventPoint = 1`, `ActorMuzzle = 2` 삭제. 이펙트 큐 `actorPoint` → `eventPoint`(`FormerlySerializedAs("actorPoint")`), 새 `eventPointRotation`(`CombatEventPointRotation { Point = 1, Actor = 2 }`, 새 파일) | `DV/CombatCuePoint.cs`, `DV/CombatCueEntries.cs`, `DV/CombatEventPointRotation.cs`(새) |
| Ta.6 | 대여·채널: `ICombatUnitClip.TryGetMuzzle`·`TryGetEffectPoint` → `TryGetEventPoint(int, out Vector3, out Quaternion)`, `UnitClipChannel`·`FieldUnitView` 구현, 쓰는 곳(`CombatCuePoints`·`CombatEffectPiece`·`CombatFirePiece`·`CombatClipRangedAttack` 무기 조준) | `C/Display/Unit/` 셋, `C/Presentation/Pieces/` 넷 |
| Ta.7 | 검사: `CombatUnitAnimationRules`의 총구·이펙트 지점 검사를 "가리키는 번호가 있고 비지 않고 프리팹 안" 하나로(발사 목록 번호·이 유닛 공격 묶음 큐의 `ActorEventPoint` 번호) | `DV/CombatUnitAnimationRules.cs`, `DV/CombatPresentationCueRules.cs`(`FindUnitError`) |
| Ta.8 | 도구 단계 1: 프리팹 14개 `eventPoints` = 옛 쌍을 2k/2k+1로(TP-6), 큐 가진 모든 SO 스캔 — `point 2` → 1+`Actor`, `point 1` → 1+`Point`, 번호 바꿈, `anchor 3` → 4 + `FirstHit`. 바꾼 에셋 목록을 콘솔에 | `DE/CombatClipSlotMigration.cs`(새) |
| Ta.9 | 문서 주석·툴팁(`Cue`·`Hit`·총구·이펙트 지점 낱말) | 바꾼 파일 |

- 확인 Ua: 사용자가 단계 1 실행 → 부트 DB 생성 통과 → Akimbo 2연발(두 총구·두 이펙트)·Shotgun 총구 섬광·Sniper·Cannon 착탄(Contact)·Hammer 맞는 순간 흔들림(옛 Hit) 같음.

### CS-b 동작 칸 값·이벤트 목록·탑승물 (화면 같음)

| Task | 내용 | 파일 |
|---|---|---|
| Tb.1 | CS-a 옛 칸 지움(`muzzle`·`effectPoint`·`additionalAttackPoints`) | `DV/CombatAnimationEvents.cs` |
| Tb.2 | 새 값·입력: `CombatClipStage`(몸·탑승물·부속), `CombatClipEventEntry`(번호·지점·지연), `CombatClipBlend`(켜짐·초, 이 Phase는 꺼짐만 읽음), `CombatClipMotion`(앞?·본·뒤?·블렌드 셋·`AnimSpeed`·이벤트 목록). 규칙 원본은 각 값의 `FindError` 하나를 SO·값이 같이 부름 | `DV/` 새 파일 넷 |
| Tb.3 | 묶음 SO: `attack`·`hit` 동작 입력(TP-8), 옛 9칸 숨김. `CombatUnitAnimationData` = 공격 동작 + 공격 전용 값 / 피격 동작. `CombatClipAttackData`의 `Shots`·`HitDelay`·`AttackSpeed` → 이벤트 목록·`AnimSpeed`. 이 Phase는 앞·뒤 단계 칸을 값에 두되 비어 있어야 함(검사) | `DV/CombatUnitAnimationSO.cs`·`CombatUnitAnimationData.cs`·`CombatClipAttackData.cs`·`CombatClipData.cs`, `CombatClipShot.cs` 지움 |
| Tb.4 | 단계 규칙 `RequireStage`(CS8): 몸 이름표(`ClipStart`·`ClipEnd` 필수, `ClipEvent`류는 본 단계만), 탑승물·부속 규칙(이벤트 없음·길이·곡선 겹침, 탑승물 포함), 이벤트 줄 규칙(CS2 표) | `DV/CombatUnitAnimationRules.cs` |
| Tb.5 | 큐 시점 규칙: Event1~3 = 그 칸 본 단계에 등록된 번호만(근접·원거리·피격) | `DV/CombatPresentationCueRules.cs`, `DV/CombatClipEventIds.cs` |
| Tb.6 | 채널: 위층 레이어 섞개 = 몸 0·탑승물 1·부속 2..(P6), 대여 `Play(CombatClipStage)` | `C/Display/Unit/UnitClipChannel.cs`·`ICombatUnitClip.cs`·`FieldUnitView.cs` |
| Tb.7 | 조립: 원거리 = 이벤트 줄마다 발사(지금 `Shots` 자리), 근접 = 줄마다 맞는 순간 + 근접 `Contact`(TP-9, 자리 = 대상 몸 중심 + 맞은 방향, P17), 피격 = 이벤트 목록으로 Event 시점. 재생 배율 = 칸 `AnimSpeed`(피격도) | `C/Presentation/Pieces/CombatClipAttack.cs`·`CombatClipRangedAttack.cs`·`CombatClipPiece.cs`·`CombatCuePoints.cs`, `C/Playback/Handlers/CombatCharacterEventHandler.cs`(`ComposeHit`) |
| Tb.8 | 도구 단계 2: 묶음 14개 — 몸·부속 → 본 단계, `Horse`로 시작하는 부속 → 탑승물 후보(창에 목록, 사용자 확인, P15), `shots` → 원거리 줄(비면 [Event1/0]), `hitDelay` → 근접 [Event1/0/지연], 피격 → [Event1], `attackSpeed` → 공격 `AnimSpeed`, 피격 1, 블렌드 꺼짐 | `DE/CombatClipSlotMigration.cs` |
| Tb.9 | 묶음 SO 에디터: 단계·이벤트 목록·블렌드 그리기(시작 모드 UI는 CS-c까지 그대로) | `DE/CombatUnitAnimationSOEditor.cs` |

- 확인 Ub: 단계 2 실행(탑승물: Sabow·Bommber·Cowboy의 말, Cowboy 올가미는 부속) → 부트 통과 → 모든 클립 유닛 공격·피격 같음, 말 셋 같음, 근접 한 줄 = 지금, Akimbo 2연발 같음.

### CS-c 앞/뒤 단계·공통 시작 타이밍 (카우보이 PullBack)

| Task | 내용 | 파일 |
|---|---|---|
| Tc.1 | CS-b 옛 9칸 지움 | `DV/CombatUnitAnimationSO.cs` |
| Tc.2 | 앞·뒤 단계 허용(Q-A6: 앞 = 공격만), 큐 `Start`·Event·`End` = 본 단계 이름표(P7) | `DV/CombatClipMotion.cs`, `DV/CombatPresentationCueRules.cs` |
| Tc.3 | 클립 조각: 대여 하나에서 앞 → 본 단계를 잇고 `ClipStart` 신호 = 본 단계 `ClipStart`, Event·`End` = 본 단계. 본 단계 끝에 조각 완료 + 대여 반환 시 뒤 단계를 채널 꼬리로 넘김 | `C/Presentation/Pieces/CombatClipPiece.cs`·`CombatClipPlayback.cs` |
| Tc.4 | 채널 꼬리: `BeginTail(stage)`·`TickTail(dt)`·끊기(다음 `Play`·취소·풀 반환·래그돌·`Dispose`, P8·P9), `FieldUnitView.Update`(TP-10)·`Pause`/`Resume` | `C/Display/Unit/UnitClipChannel.cs`·`FieldUnitView.cs` |
| Tc.5 | 공통 시작 타이밍(CS11): 값 `motionStart`(`CombatAttackMotionStart { AfterLead = 0, WithLead = 1 }`) + 지연, `CombatAttackAnimationStartMode`·`CombatAttackAnimationStartData`·`CombatPreAttackTraitKinds` 지움, 선행 구간 계산(TP-13), 알림 입구 하나(TP-12), 옛 입구 셋 지움 | `DV/` 새 1·지움 3, `C/Playback/Ordering/CombatPreAttackTraitMap.cs`, `C/Playback/Support/CombatPreAttackPresentationStarts.cs`, `C/Playback/CombatSceneEventPresenter.cs`, 특성·당김·걸음 처리기의 옛 호출 |
| Tc.6 | 도구 단계 3: 모드 0 → `AfterLead`, 1·2 → `WithLead`(지연 그대로), 기준 Kind 칸 버림 | `DE/CombatClipSlotMigration.cs`, `DE/CombatUnitAnimationSOEditor.cs` |
| Tc.7 | 에셋: 카우보이 묶음 공격 앞 단계 = `CowBoy_PullBack` + 탑승물 `Horse_PullBack` + 부속 `Lasso_PullBack`(TP-14) | `B/AnimationClip/Playerable/CombatUnitAnimation_Cowboy.asset` |
| Tc.8 | 선행 구간 사건 대조(P19): 결과 사건 종류마다 처리기가 바로 끝내는지 표를 계획 §5에 적음(휩쓸기 머리 포함) | 문서 |

- 확인 Uc: 단계 3 실행 → 부트 통과 → 카우보이 끌어오기 있는 공격(끌려오는 동안 PullBack → 공격), 없는 공격(PullBack → 공격), SoulEater 휩쓸기 맞춤이 지금과 같은 순간, 뒤 단계 없는 유닛 같음.

### CS-d 블렌드·V8

| Task | 내용 | 파일 |
|---|---|---|
| Td.1 | 블렌드 값 읽기(켜짐이면 초 > 0) | `DV/CombatClipBlend.cs`·`CombatClipMotion.cs` |
| Td.2 | 채널: 섞개 입력 셋(TP-11), In(대여 시작)·Between(단계 바뀜)·Out(대여 반환 뒤 꼬리로, 끝나면 되돌리기 한 번), 끊기 규칙은 꼬리와 같음 | `C/Display/Unit/UnitClipChannel.cs` |
| Td.3 | 조각이 대여 동안 In·Between 가중치를 같은 시계로 넣음(시각·이벤트는 그대로) | `C/Presentation/Pieces/CombatClipPlayback.cs` |
| Td.4 | V8: 채널이 바꾼 Animator 값 다섯(컨트롤러·`applyRootMotion`·`fireEvents`·`cullingMode`·`enabled`)을 기억했다 `Dispose`·실패 정리에서 모두 돌려줌 | `C/Display/Unit/UnitClipChannel.cs` |
| Td.5 | 에셋: 확인용으로 카우보이 공격 칸 In·Between·Out 켬(초는 사용자가 보며 조정) | 카우보이 묶음 |

- 확인 Ud: 켠 칸만 섞임, 끈 칸은 지금과 같음, 페이드아웃 중 다음 공격·피격이 오면 끊김, 사건 완료 시각 같음.

### CS-e 문서·공유·검토

- 이펙트·소리 창구 안내(시점 Event1~3·Contact+고르기·`ActorEventPoint`), 폴더 배치 안내(새 파일), 사운드 작업자 안내 시점 낱말, 클립 설계 머리말에 "대신함" 줄.
- 공유(미전달): 민형님 — 새 모델 프리팹 `eventPoints`(옛 총구·이펙트 쌍의 번호 규칙), 클립 작업 안내(앞·뒤 단계 몸 클립에도 `ClipStart`·`ClipEnd`, `ClipEvent`류는 본 단계만, 말은 탑승물 칸). 호진님 클립 메모 새 절(묶음 칸 모양).
- 묶음 끝 검토(새 에이전트 하나, 7기준), 설계 구현 메모.

## 3. 커밋 묶음(사용자)

- CS-a: 코드 + 도구 단계 1 + 단계 1로 바뀐 프리팹·큐 에셋을 한 커밋(코드만이면 부트가 멈춤). `CombatAttackPoint.cs` → `CombatEventPoints.cs`는 이름 바꾸기로.
- CS-b·CS-c·CS-d: 같은 규칙(코드 + 그 단계 도구 결과 에셋 한 커밋).
- 새 `.cs`의 `.meta`는 함께.

## 4. Unity 확인 목록 요약

| Phase | 사용자 할 일 |
|---|---|
| CS-a | 단계 1 실행 → 부트 → Ua |
| CS-b | 단계 2 실행(탑승물 후보 확인) → 부트 → Ub |
| CS-c | 단계 3 실행 → 부트 → Uc |
| CS-d | 블렌드 초 조정 → Ud |

## 5. 실행 기록

### 5.1 CS-a·CS-b 코드 완료 (2026-11-17, 사용자 "CS-b까지 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua·Ub 대기)

**사용자 할 일(순서):** Unity 컴파일 → 메뉴 `ProjectL > Combat > 클립 칸 옮기기 (1) 이벤트 지점·큐` → `(2) 클립 묶음`(탑승물 후보 창 확인) → 부트 DB 생성 → Ua·Ub. 도구를 돌리기 전 판은 부트가 멈춘다(의도 — 옛 칸에만 값이 있으면 "옮기기 도구를 돌리세요" 문구). 코드 + 도구가 바꾼 프리팹·에셋은 한 커밋.

CS-a(이름·이벤트 지점·Hit 합치기):
- `CombatCueAnchor`: `Cue`·`Cue2`·`Cue3` → `Event1`·`Event2`·`Event3`(값 2·6·7), `Hit = 3` 삭제. 같은 파일에 `CombatContactFilter { Every = 1, FirstHit = 2 }`. `CombatClipEventId.Event` → `Event1`. 이름표 함수 `ClipEvent*`는 그대로(P1).
- 큐 항목: 이펙트·흔들림·소리 시점에 `contactFilter`. 이펙트 큐 `actorPoint` → `eventPoint`(`FormerlySerializedAs`), 새 `eventPointRotation`(`CombatEventPointRotation { None = 0, Point = 1, Actor = 2 }`, 같은 파일). 큐 규칙 `AllowsAnchor`·`AllowsPoint`가 고르기를 받고 `IsEveryContact`·`IsFirstHit`·`RequireFilter`. 플레이어 스킬 칸 = Contact·FirstHit.
- 기준 시점 표 `CombatCueAnchors`: `SetFirstHit`(옛 Hit 칸), `TryStart(anchor, filter, delay)`, `SetClipEvents(motion, played)`(등록한 이벤트만). 큐 붙이기·투사체 소리·코드 모션·클립 조립·피격 조립이 새 입구를 부름. 접촉 이펙트(`PlayImpact`)는 Every만.
- 이벤트 지점: `CombatAttackPoint.cs` → `CombatEventPoints.cs`(`git mv`, GUID 유지, 옛 쌍 형식 `CombatAttackPoint`는 도구용으로 남김), `CombatAnimationEvents.eventPoints`, 옛 `muzzle`·`effectPoint`·`additionalAttackPoints`는 숨김. 큐 자리 `ActorEventPoint = 1`(옛 1·2 합침, 2 비움). 대여 `ICombatUnitClip.TryGetEventPoint` 하나(옛 넷 삭제), 채널·`FieldUnitView`·이펙트 조각 `AtEventPoint`(회전 Point/Actor)·발사 조각. 무기 조준 `CombatWeaponAim.MuzzlePoint`(가장 먼저 쏘는 줄의 지점).
- 도구 `DE/CombatClipSlotMigration.cs` 단계 1: 프리팹 `eventPoints` = [쌍0 총구, 쌍0 이펙트, 쌍1 총구, …](끝의 빈 칸은 줄임), 큐 `point 2` → 1 + `Actor` + `eventPoint 2k`, `point 1`(회전 None) → `Point` + `2k+1`, `anchor 3` → 4 + `FirstHit`, `anchor 4`의 고르기 0 → `Every`.

CS-b(동작 칸·이벤트 목록·탑승물):
- 새 값(`DV/`): `CombatClipEventEntry`(+ 입력), `CombatClipBlend`(+ 입력, 이번엔 보관만), `CombatClipMotion`(+ `CombatClipStageInput`·`CombatClipMotionInput`, 본 단계·이벤트 목록·`AnimSpeed`·블렌드 셋, 규칙 원본 `FindEventsError`, `Registers`). `CombatClipData`에 `Mount`. `CombatClipAttackData` = 동작 + 시작 기준(옛 `Shots`·`HitDelay`·`AttackSpeed` 삭제), `CombatUnitAnimationData.Hit` = `CombatClipMotion`. `CombatClipShot` 값 삭제(입력 `CombatClipShotEntry`는 도구용).
- 묶음 SO: `attack`·`hit` 동작 입력, 시작 모드 셋 그대로(CS-c), 옛 9칸 숨김 + 옮기지 않았으면 멈춤(`RequireMigrated`).
- 규칙 `CombatUnitAnimationRules`: `RequireStage`(몸·탑승물·부속), 이벤트 줄 규칙(원거리 = 지점 있음·지연 0, 근접 = 지점 0·같은 이벤트 한 번, 피격 = 지점 0·지연 0). 큐 유닛 검사 `FindUnitError(attack, attackMotion, points, hit, hitMotion)`: Event1~3은 그 칸에 등록한 번호만(코드 모션 공격은 Event1).
- 채널: 층 몸 0 · 탑승물 1 · 부속 2..
- 조립: 원거리 = 이벤트 줄마다 한 발, 근접 = 줄 하나면 해제 조각, 여럿이면 접촉 집계 + 새 `Pieces/CombatStrikePiece.cs`(줄마다), 근접 `Contact`·Every 이펙트 = 공격 시작 `MeleeContactOf`(그 출처가 맞히는 캐릭터마다 몸 중심 + 공격자→대상 방향). 피격 조립 = 피격 동작 `Main`·`AnimSpeed`·등록 이벤트.
- 도구 단계 2: 몸·부속 → 본 단계, 이름에 `Horse`가 든 첫 부속 → 탑승물(적용 전 목록 창), `shots` → 원거리 줄(지점 `×2`, 지연 0), 빈 `shots` → [Event1/0/옛 `hitDelay`], 피격 → [Event1], `attackSpeed` → 공격 `AnimSpeed`, 피격 1.

**계획과 다른 점(사용자 확인 대상):**
- TP-3·Tb.1: CS-a 옛 지점 칸 지우기를 CS-c 첫 Task로 미룸 — 도구를 아직 돌리지 않아 지우면 옮길 값이 사라짐. 옛 묶음 칸도 CS-c에서 같이 지움.
- TP-4 좁힘: `contactFilter`는 이펙트·흔들림·소리 시점만(플래시는 Start·Event1만, 프리셋 소리는 피격 칸이라 접촉 시점이 없음).
- 새 규칙: 이벤트 목록에 Event1 줄 필수(기준 이벤트 — 특성 덮어쓰기 큐가 Event1을 어느 유닛에서나 쓸 수 있게).
- `CombatEventPointRotation.None = 0`을 "아직 고르지 않음"으로 둠(도구가 옛 큐를 알아보는 표지, 이벤트 지점 자리에서 None이면 검사가 거절).
- `CombatContactFilter`·`CombatEventPointRotation`은 새 파일 대신 기존 파일 안(`.meta` 불필요). 앞·뒤 단계 칸은 아직 안 둠(CS-c에서 더함, 입력은 더하기만).
- 근접 접촉 이펙트는 캐릭터 대상만(건물은 구조물 피격 큐가 따로 있음).

**새 `.cs`(`.meta`는 Unity가 만듦):** `DV/CombatClipEventEntry.cs`·`CombatClipBlend.cs`·`CombatClipMotion.cs`, `C/Presentation/Pieces/CombatStrikePiece.cs`, `DE/CombatClipSlotMigration.cs`. 이름 바꿈 `DV/CombatAttackPoint.cs` → `CombatEventPoints.cs`(+ `.meta`).

**11-17 이어서 고침(컴파일 미확인):** Unity 컴파일이 CS0117 여덟 곳으로 실패했다(Editor.log) — Ta.1의 `CombatClipEventId.Event → Event1` 이름 바꾸기가 쓰는 곳에만 들어가고 정의(`DV/CombatClipTimings.cs`)에는 빠졌다. 정의를 `Event1 = 1`로 고침(값 그대로라 에셋 무변경). 옛 이름(총구·이펙트 지점 입구·`CombatClipShot` 값·`Cue`·`Hit` 시점·`Shots`·`HitDelay`·`AttackSpeed`)을 쓰는 곳은 시험·도구·Systems 모두 0.

**도구 실행·확인 결과(11-17):** 컴파일 성공(Editor.log, 경고 둘은 도구용으로 남긴 옛 칸 `attackSpeed`·`CombatClipShotEntry.eventId`의 CS0414) → 단계 1(프리팹 11개·큐 에셋 셋 `CombatAttack_Akimbo`·`_Hammer`·`_Shotgun`) → 단계 2(묶음 14개, 탑승물 = Sabow `Enemy_Horse_*`·Bommber `Horse_Player_*`·Cowboy `Horse_*`, Cowboy 올가미는 부속) → **Ua·Ub 문제 없음(사용자)**. 말·무기 Idle은 컨트롤러 `ClipIdle_*` 층이 돌려서 그대로 됨(설계 CS5 정정).

**Ua·Ub 확인(사용자):** 도구 두 단계 뒤 부트 통과 → Akimbo 2연발(두 총구·두 이펙트 지점), Shotgun 총구 섬광(행위자 방향), Sniper·Cannon·Ballista·Catus 착탄(Contact·Every), Hammer 맞는 순간 흔들림(옛 Hit → FirstHit), 근접 유닛 맞는 순간 = 지금(옛 선딜레이), 말 셋(Sabow·Bommber·Cowboy, 탑승물 층)·올가미(부속), 피격 클립·플래시 같음.

### 5.2 CS-c 코드·에셋 완료 (2026-11-17, 사용자 "바로 CS-c 진행해", 새 시험 없음, 시험 변경 없음, Uc 문제 없음 — 사용자)

**도구 실행·확인 결과(11-17):** 컴파일 성공(새 `CombatAttackMotionStart.cs.meta`를 Unity가 만듦) → 단계 3이 바꾼 묶음 둘 = `Playerable/CombatUnitAnimation_Cowboy`·`Playerable/CombatUnitAnimation_SoulEater`(`motionStart: 1`(WithLead)·`motionStartDelay: 0`, 옛 `attackAnimationStartMode` 0, 다른 묶음 열넷은 옛 모드 0이라 무변경 — 다시 저장되기 전까지 새 칸 없이 기본값 AfterLead) → 부트 통과 → **Uc 문제 없음(사용자 "확인 했음")**. 아래 "계획과 다른 점"과 `AttackGranted` 기준 변화도 이 확인으로 받아들인 것으로 적는다.

**사용자 할 일(순서):** Unity 컴파일 → 메뉴 `ProjectL > Combat > 클립 칸 옮기기 (3) 시작 기준`(바꾼 묶음 2개 = `Playerable/CombatUnitAnimation_Cowboy`(옛 모드 1)·`Playerable/CombatUnitAnimation_SoulEater`(옛 모드 2)) → 부트 DB 생성 → Uc. 도구를 돌리기 전 판은 부트가 멈춘다(의도 — 옛 시작 모드 칸에 값이 남으면 "(3) 시작 기준을 실행하세요"). 코드 + 도구가 바꾼 묶음 + 아래 에셋은 한 커밋. CS-a·CS-b는 아직 커밋 전이라 CS-a~CS-c를 한 커밋으로 해도 된다(옛 칸을 지워 도구 (1)·(2)는 다시 못 돈다).

사용자 결정(11-17): 카우보이 `CowBoy_PullBack.anim`(민형님 파일)의 자리값 `ClipEvent`(0.75초)는 지우고, 설계대로 앞·뒤 단계 몸 클립에 중간 이벤트가 있으면 부트에서 막는다(A).

- Tc.1 옛 칸 지움: `CombatAnimationEvents`의 `muzzle`·`effectPoint`·`additionalAttackPoints`, 옛 쌍 형식 `CombatAttackPoint`, 묶음 SO 옛 9칸(`attackClip`·`attackSpeed`·`attackCompanionClips`·`hitDelay`·`shots`·`hitClip`·`hitCompanionClips`)과 `RequireMigrated`(클립), `CombatClipShot.cs`(+ `.meta`). 옮기기 도구 단계 1·2 메뉴는 읽을 칸이 없어 걷음.
- Tc.2 앞·뒤 단계: 동작 입력 `pre`·`post`(앞 = 공격 칸만, 피격 칸에 넣으면 부트 거절), 값 `CombatClipMotion.Pre`·`Post`. 앞·뒤 단계 몸은 `CombatClipEvents.ReadStageTimings`(ClipStart·ClipEnd만, `ClipEvent`류가 있으면 거절), 시각 값 `CombatClipTimings`가 중간 이벤트 없는 목록을 받음(`EventTime`은 그때 예외). 탑승물·부속 규칙은 단계마다 같음.
- Tc.3 클립 조각: `CombatClipPiece(unit, motion, aim)` — 앞 단계 → 본 단계를 같은 대여에서 바로 갈아 끼우고(남은 시간은 본 단계가 이어 받음, `CombatClipPlayback.Overflow`), 신호는 본 단계 이름표만(`ClipStart`·Event1~3), 조준은 본 단계를 꽂은 뒤. 본 단계 끝에 뒤 단계를 `ICombatUnitClip.PlayTail`로 채널에 넘김. 공격 큐 `Start` = 앞 단계가 있으면 본 단계 `ClipStart` 신호, 없으면 지금처럼 항목 시작.
- Tc.4 채널 꼬리: `UnitClipChannel.Begin`이 남은 위층(단계 바뀜·꼬리)을 빼고 되돌린 뒤 꽂음, `BeginTail`·`TickTail`·`CutTail`·`HasTail`, `FieldUnitView.Update`가 꼬리 있을 때만 진행. 끊는 곳: 다음 클립, 표시 대여 정리(`ResetClip` = 꺼짐·풀 반환·다른 유닛 바인딩), 래그돌 시작, 연출 취소(`CombatUnitPresentationWindow.CutClipTails`, 사건 Presenter `CancelMovementCore`), `Dispose`. 꼬리를 넘긴 대여의 반환은 꼬리를 끊지 않음.
- Tc.5 공통 시작 타이밍: 새 `DV/CombatAttackMotionStart.cs`(`AfterLead = 0`·`WithLead = 1`), 묶음 SO `motionStart`·`motionStartDelay`, 값 `CombatClipAttackData.MotionStart`·`MotionStartDelay`·`StartsWithLead`(검사 `FindStartError`), 지움 `CombatAttackAnimationStartData`·`CombatAttackAnimationStartMode`·`CombatPreAttackTraitKinds`(+ `.meta`). 선행 구간 지도 `CombatPreAttackTraitMap` → `CombatPreAttackLeadMap`(`git mv`): WithLead 공격마다 "발동 중 공격 앞 + 휩쓸기 머리"의 가장 앞 사건 ~ 공격 직전(사이 행동 시작에서 끊음)을 사건 → 공격으로, 끌어오기·휩쓸기 소속 검사는 그대로. 결합 값 `CombatPreAttackTraitBinding` → `CombatPreAttackLead`(`git mv`, 공격 열쇠 + 사건 위치). 어댑터 `CombatPreAttackPresentationStarts`는 `IsLead`·`EventStarted` 하나, 수신 `CombatPreAttackPlayback.Reserve(lead, delay)`. 알리는 곳은 사건 Presenter의 `PresentEvent`(간격 뒤 표시 포함)·`BeginStep` 두 입구의 같은 규칙: 선행 구간 사건이고 그 시작 안에서 완료되지 않았으면(P19, TP-12) 알림. 옛 입구 셋(특성 피드백 `TraitStarted`·`CaptureTraitStart`·`ActionStarted`, 끌어오기 레인 `started`, 휩쓸기 `StepStarted`)과 몸 글자 조각 `started` 걷음(모두 용근 줄 `78afcdeea`·`36a6d9139`).
- Tc.6 도구 단계 3: 옛 모드 1·2 → `WithLead` + 옛 지연, 옮긴 뒤 옛 칸 0, 모르는 값은 건너뜀(부트가 멈춤). 묶음 SO 에디터: WithLead일 때만 지연 칸.
- Tc.7 에셋: `CombatUnitAnimation_Cowboy` 공격 `pre` = `CowBoy_PullBack`(몸)·`Horse_PullBack`(탑승물)·`Lasso_PullBack`(부속), `CowBoy_PullBack.anim`의 `ClipEvent` 한 줄 지움(참조하는 다른 에셋 없음). PullBack 세 클립의 뼈 경로 겹침은 Attack 세 클립(Ub 통과)과 같은 꼴.

**Tc.8 선행 구간 사건 대조(P19):**

| 사건 | 처리 | 선행 시작으로 셈 |
|---|---|---|
| 특성 발동 `CombatEffectsEvent`(끌어오기 `PullApplied` 등) | 특성 처리기 — 큐·몸 글자·유닛 행동은 곁들임이라 사건은 바로 완료(추가 공격 시작만 출처를 엶) | 안 셈(추가 공격 시작이면 셈) |
| 끌어오기 이동 `CombatPullEvent` | 이동 레인, 도착 때 완료 | 셈 = 옛 기준(레인 시작)과 같은 프레임 |
| 휩쓸기 머리 `CombatSweepEvent` | 형식 표에 없어 표시 안 함 | 안 셈 |
| 휩쓸기 밀림 걸음 `CombatCharacterEvent`(걸음) | `BeginStep` 레인 | 셈 = 옛 기준(첫 걸음)과 같음 |
| 특성 상태 `CombatTraitStateEvent` | 사실 처리기, 바로 완료 | 안 셈 |

→ 지금 쓰는 두 묶음(카우보이 끌어오기·SoulEater 휩쓸기)은 옛 기준과 같은 순간. **바뀐 것:** 옛 "지정 Kind = `AttackGranted`(선봉·청소)" 기준은 발동이 곁들임만 내 바로 끝나므로 선행 시작이 없어 공격 사건에서 시작한다(지금 이 기준을 쓰는 묶음 없음 — 필요해지면 "곁들임 실행을 연 사건도 셈"으로 넓힐지 정할 것).

**계획과 다른 점(사용자 확인 대상):**
- Tc.3: 뒤 단계 꼬리는 "대여 반환 시"가 아니라 **본 단계가 끝나는 순간** 넘김(설계 CS6 "본 단계가 끝나면 뒤 단계는 채널이 혼자"). 원거리처럼 실행이 투사체 도착까지 남아도 뒤 단계가 늦지 않음. 그래서 실행 취소(대여 반환)만으로는 꼬리가 안 끊겨 연출 취소에 `CutClipTails`를 더함(P8).
- Tc.5: 파일 이름 바꿈 둘(`CombatPreAttackTraitMap` → `CombatPreAttackLeadMap`, `CombatPreAttackTraitBinding` → `CombatPreAttackLead`) — 특성 종류를 안 보게 돼 옛 이름이 뜻과 달라서. 새 `.cs`는 계획대로 `CombatAttackMotionStart.cs` 하나(시작 기준 값은 공격 칸 값에 둠).
- 공격 큐 `Start`는 앞 단계가 있을 때만 본 단계 `ClipStart` 신호로 옮김(없으면 지금 그대로 — 지금 클립은 ClipStart가 0초라 같음).
- Tc.2: 시각 값이 중간 이벤트 없는 목록을 받도록 넓힘(앞·뒤 단계용 `ReadStageTimings`).

**새 `.cs`(`.meta`는 Unity가 만듦):** `DV/CombatAttackMotionStart.cs`. 이름 바꿈(`git mv`, `.meta` 함께): `C/Playback/Ordering/CombatPreAttackLeadMap.cs`·`CombatPreAttackLead.cs`. 지움(+ `.meta`): `DV/CombatClipShot.cs`·`CombatAttackAnimationStartData.cs`·`CombatAttackAnimationStartMode.cs`·`CombatPreAttackTraitKinds.cs`.

**Uc 확인(사용자):** 도구 (3) 뒤 부트 통과 → 카우보이 끌어오기 있는 공격(끌려오는 동안 PullBack → 공격), 끌어오기 없는 공격(PullBack → 공격, 공격 시작 큐가 PullBack 뒤), SoulEater 휩쓸기 맞춤이 지금과 같은 순간, 앞·뒤 단계 없는 유닛 공격·피격 같음, 말·올가미가 PullBack에서도 같이 움직이는지.

### 5.3 CS-d 코드·에셋 완료 (2026-11-17, 사용자 "CS-d 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ud 대기)

사용자 확인(11-17, CS-d 착수 전 보고에 대한 "CS-d 진행해"): Out 블렌드는 계획 Td.2의 "대여 반환 뒤"가 아니라 **뒤 단계가 끝날 때, 뒤 단계가 없으면 본 단계가 끝날 때** 섞기 시작한다(CS-c에서 꼬리를 본 단계 끝에 넘기게 바꾼 것과 맞춤).

- Td.1 블렌드 값: 이미 CS-b에서 읽고 검사함(켜짐이면 초 > 0, `CombatClipBlend` 생성자). 주석만 고침(`CombatClipBlend`·`CombatClipMotion.BlendIn`·`BlendBetween`·`BlendOut`).
- Td.2 채널 `UnitClipChannel` 다시 씀: 섞개 입력 셋(0 Idle · 1·2 위층 번갈아, TP-11), 가중치는 시작 값 → 목표(한 입력 1)로 직선(합 늘 1), 섞기가 끝나면 가중치 0인 위층을 빼고 되돌리기(P17), 바닥으로 다 돌아가면 `End`.
  - `Begin(clip, blend)` = In: 남은 위층·꼬리·섞기를 그 자리에서 끊고(P9) 바닥에서 섞음.
  - `CrossFade(clip, blend)` = Between(같은 대여의 단계 바뀜, 본 → 뒤 단계도). 나가는 위층은 마지막 시각에 멈춤. 조준은 풀림(본 단계가 다시 겁니다).
  - `HandOff(tail, speed, between, out)`(옛 `BeginTail` 대신): 뒤 단계가 있으면 Between으로 꽂아 혼자 돌리고 끝에 Out, 없으면 바로 Out. Out이 꺼져 있으면 뒤 단계 끝에서 바로 돌아가고(CS-c와 같음), 뒤 단계도 없으면 남은 섞기만 마치고 지금 자세로 남음(대여 반환이 돌려보냄).
  - `AdvanceBlend(dt)`, `IsBlending`, `HasTail`(넘긴 일 진행 중 → `FieldUnitView.Update`가 `TickTail`), `HoldsTail`(뒤 단계·Out 섞기가 돌고 있음 → 대여 반환이 끊지 않음).
- Td.3 섞기 시간: 대여 동안은 클립 조각 `CombatClipPiece.Tick`이 같은 Tick 시간(실제 초, 재생 배율 없음)으로 `AdvanceBlend`, 넘긴 뒤는 채널 `TickTail`. 재생 단위 `CombatClipPlayback`은 꽂을 때 쓸 블렌드만 받음(생성자 끝 선택 인자) — 앞 단계 = In, 본 단계 = 앞 단계가 있으면 Between·없으면 In. 본 단계 끝에 뒤 단계·Out이 없어도 섞기가 남았으면 넘김.
- Td.4 V8: 채널이 바꾸는 Animator 값 다섯(컨트롤러·`applyRootMotion`·`fireEvents`·`cullingMode`·`enabled`)을 생성 때 기억했다 `Dispose`·생성 실패에서 모두 돌려줌(`RestoreAnimator`).
- 대여 `ICombatUnitClip`: `Play(clip, blend)`(대여 첫 호출 = 채널 `Begin`, 그다음 = `CrossFade`), `IsBlending`, `AdvanceBlend`, `PlayTail(tail, speed, between, fadeOut)`(tail은 null 가능), `RestoreClip`은 `HoldsTail`일 때만 채널을 그대로 둠. 구현 `FieldUnitView.PresentationUnitClip`.
- Td.5 에셋: `Playerable/CombatUnitAnimation_Cowboy` 공격 칸 In 0.1초·Between 0.1초·Out 0.2초 켬(확인용, 초는 사용자가 보며 조정). 피격 칸은 그대로 꺼짐.

**계획과 다른 점(사용자 확인 대상):**
- Td.3 섞기 시간을 넣는 곳은 `CombatClipPlayback`이 아니라 `CombatClipPiece`(섞기는 재생 하나가 아니라 단계 사이의 일이라서). 재생 단위는 꽂을 블렌드만 넘김.
- 본 단계 끝에 In·Between 섞기가 남았으면(본 단계가 블렌드 초보다 짧을 때) 그 섞기도 채널에 넘겨 마저 진행(안 넘기면 원거리처럼 실행이 길 때 섞다 만 채 멈춤). 넘긴 것이 이것뿐이면 대여 반환은 지금처럼 바로 돌아감.
- 컨트롤러(Idle)가 없는 유닛은 섞을 Idle이 없어 In·Out을 바로 바꿈(Between은 섞음). 지금 이벤트 컴포넌트를 붙인 유닛은 모두 컨트롤러가 있음.
- 단계가 블렌드 초보다 짧아 섞기가 겹치면(나가는 위층이 아직 있는데 다음 단계가 옴) 가장 오래된 위층을 빼고 그 가중치를 지금 위층에 얹음 — 그 몫만큼 작은 튐이 있을 수 있음(지금 에셋에서는 닿지 않음).
- Out 섞기가 시작될 때 조준을 풂(조준 곡선은 본 단계 끝에서 이미 0이라 튐 없음).

**바뀐 파일:** `DV/CombatClipBlend.cs`·`CombatClipMotion.cs`(주석), `C/Display/Unit/UnitClipChannel.cs`·`ICombatUnitClip.cs`·`FieldUnitView.cs`, `C/Presentation/Pieces/CombatClipPlayback.cs`·`CombatClipPiece.cs`, 에셋 `Playerable/CombatUnitAnimation_Cowboy.asset`. 새 파일·도구 단계 없음, Factory 무변경, 다른 작업자 줄 없음.

**Ud 확인(사용자):** 카우보이 공격 — Idle → PullBack이 0.1초 섞여 들어오고, PullBack → Attack이 0.1초 섞이며, 공격 끝에 0.2초 섞여 Idle로(말·올가미 포함). 끌어오기·맞는 순간·공격 시작 큐 시점이 지금(Uc)과 같은지. Out 섞는 중에 카우보이가 맞으면 끊고 피격 클립이 바로 시작하는지. 블렌드를 끈 유닛(나머지 전부)·피격 칸은 지금과 같은지. 조준 리그 유닛(곡사·원거리)의 조준이 그대로인지. 표시 끄기·풀 반환 뒤 Animator 값(루트 모션·컬링 등)이 프리팹 값으로 돌아오는지.

**같이 한 것(11-17 사용자 "AttackPresentation 큐에서 인덱스 구분이 어려워 여백 간격"):** 새 에디터 그리기 `DataBaseSystem/Editor/CombatCueEntryDrawer.cs`(`.meta`는 Unity가 만듦, `#if UNITY_EDITOR`) — 큐 항목 다섯 형식(이펙트·흔들림·소리 시점·프리셋 소리·플래시)이 목록 항목이면 위에 여백 10px·옅은 구분선, 칸은 기본 순서·툴팁 그대로. 같은 형식을 쓰는 모든 목록(공격 연출·피격 반응·특성 발동 줄·지형·오브젝트 외형 등)에 같이 적용, 목록이 아닌 한 칸은 기본과 같음. 값·검사 무변경, 컴파일 미확인.

**같이 한 것 2(11-17 사용자 "A 음수 지연" → "1번도 하게 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음):**
- **공격 동작 앞당김(결정 A):** 묶음 SO `motionStartDelay`가 WithLead에서 음수를 받음(`Min(0)` 뗌, 검사는 유한한 수만 — `CombatClipAttackData.FindStartError`, 툴팁·묶음 에디터 안내). 값 `CombatClipAttackData.LeadHoldSeconds`(음수 지연의 크기). 재생: 그 공격의 선행 구간에서 사건 Presenter에 **처음 닿은 사건**(사건 시작 `BeginEvent` 또는 걸음 시작 `BeginStep`)에서 어댑터 `CombatPreAttackPresentationStarts.TakeLeadHold`가 공격 동작을 바로 예약하고(`CombatPreAttackPlayback.Reserve(lead, 0, leadPending: true)` — 아직 처리 전인 그 사건도 의존 검사에 넣음) 사건 Presenter가 곧바로 `Flush`해 동작을 시작한 뒤, 그 사건을 기존 사건 앞 간격(적 행동 간격과 같은 대기 칸)으로 그 초만큼 붙잡음 — 걸음이면 `CombatMovementPresentation.BeginStep`의 걸음 전 간격에 더함. 붙잡힌 사건 뒤 사건도 그만큼 밀림(재생 전체가 길어짐, Core 결과 순서 그대로). 공격마다 한 번. 다른 걸음 레인이 진행 중이면 간격을 시작할 수 없어 앞당김을 쓰지 않고 경고 한 번(동작은 그 사건과 함께). AfterLead는 그대로(선행 연출 끝을 미리 모름). 선행 구간이 없는 공격은 앞당길 사건이 없어 공격과 함께 시작. 지금 에셋은 지연 0이라 화면 변화 없음.
- **Event2가 Event1보다 앞서도 됨:** 시각 규칙 `CombatClipTimings.FindTimeError`가 "시작 ≤ 각 중간 이벤트 ≤ 끝"만 보고 중간 이벤트끼리 순서는 안 봄(옛 문구 "앞 이벤트보다 앞섭니다" → "시작 이벤트보다 앞섭니다"). 목록은 그대로 번호 순(어느 이벤트인지), 새 `EventsInTimeOrder`(시각 순, 같은 시각은 번호 순)를 재생 커서 `CombatClipEventCursor`가 따름. 받는 쪽은 목록 칸으로 고르므로 무변경. 확인한 순서 의존: 근접 여러 줄·원거리 연발의 해제·숫자 나눔은 접촉 집계(`CombatVolleyReleasePiece`)가 "도착 순"이라 그대로 맞음, 조준 리그 첫·마지막 발은 최소·최대 시각이라 맞음, 큐 Event1~3 시점은 번호로 걸려 맞음. 옛 순서를 지키는 클립은 화면 그대로.

**확인 추가(사용자):** 카우보이 `motionStartDelay`를 음수(예: −0.3)로 → PullBack이 먼저 나오고 0.3초 뒤 "끌어오기" 글자·끌려옴 시작, 맞는 순간도 그만큼 늦게. 끌어오기가 없는 공격은 그대로. SoulEater를 음수로 하면 휩쓸기 첫 걸음이 늦춰지는지. 클립 하나에서 ClipEvent2를 ClipEvent 앞에 찍어도 부트가 통과하고 시점이 시각 순으로 나오는지.

**같이 한 것 3(11-17 사용자 "Events의 Point 설명이 근접·피격은 0번만이라는데 아님, 제약 없는 거야", 컴파일 미확인):** 근접 공격·피격 이벤트 줄의 "지점은 0이어야 합니다" 부트 검사를 뗌(`CombatUnitAnimationRules.RequireTimingEvents`, 설계 P3·P4 정정). 근접·피격에서 지점 값은 읽는 곳이 없어 동작 변화는 없음(원거리만 발사 지점으로 씀). 툴팁·문서 문구 넷(`CombatClipEventEntry` Point, 묶음 SO 공격·피격 칸, `CombatClipAttackData`, 규칙 요약) 고침. 같은 이벤트 한 번·피격 지연 0 규칙은 그대로.

**같이 한 것 4(11-17 사용자 "권장안대로 하되 피격 칸 Events는 그대로 두기 — 맞기 전에 예열 동작이 추가될 가능성", 컴파일 미확인, 에셋 변경 없음):** 묶음 14개 중 13개의 공격 Events가 `[Event1, 0, 0]` 한 줄, 피격은 모두 `[Event1]`이라 사실상 빈 칸 채우기였음.
- 빈 목록 = 기본 한 줄: `CombatClipMotion` 생성자가 비거나 null인 목록을 `DefaultEvents`(Event1 · 지점 0 · 지연 0)로 바꿈(공격·피격 같은 값 형식이라 둘 다). 묶음 SO 입력 기본값도 빈 배열. 줄을 적으면 지금처럼 Event1 줄이 있어야 함(설계 CS2 그대로).
- 큐 시점은 클립 표식만: `CombatClipMotion.Registers`(목록에 적었나) → `HasEvent`(본 단계 몸 클립에 그 표식이 있나). 큐 규칙 `CombatPresentationCueRules.FindEventAnchorError`·기준 시점 연결 `CombatCueAnchors.SetClipEvents`가 부름 — 클립 조각은 원래 표식마다 신호를 냈음. 근접 공격에서도 맞는 순간과 별개인 Event2 큐(예: 휘두르기 소리)를 둘 수 있게 됨.
- 피격 칸 Events 칸은 그대로 둠(사용자 결정 — 맞기 전 예열 동작 자리). 비우면 기본 한 줄.
- 기존 에셋의 `[Event1, 0, 0]` 줄은 기본값과 같은 뜻이라 그대로(옮기기 도구 없음).
- 툴팁·주석: 동작 입력 Events, 묶음 SO 공격·피격 칸, 공격 연출 SO 큐 머리, 큐 입력 소리 칸 둘, 값·규칙·처리기·조립 주석.

**확인 추가(사용자):** 부트 통과(지금 에셋 그대로), 묶음 하나의 공격 Events를 비워도 지금과 같은지, Event2 표식이 있는 근접 클립에서 Event2 시점 큐가 목록에 안 적어도 나오는지.

**같이 한 것 5(11-17 사용자 "[Event1, 0, 0] 다 지우고, Header에 적자 — 공격은 공격 애니메이션 임팩트 타이밍, 피격은 피격 타이밍", 컴파일 미확인):**
- 에셋: 묶음 14개의 공격·피격 Events에서 `[Event1, 0, 0]` 줄 28개를 지움(13개 묶음은 두 칸 모두 `events: []`, Akimbo 공격은 `[Event2, 2, 0]` 한 줄만 남음). CRLF 그대로.
- 규칙(같은 지시를 Akimbo에 적용하려고 바꿈): Event1 줄이 없는 목록에는 `[Event1, 0, 0]`을 맨 앞에 붙임(`CombatClipMotion.WithPrimary`, 비면 기본 한 줄). Akimbo는 지금과 같은 `[Event1, 0, 0]`·`[Event2, 2, 0]`. 같이 한 것 4의 "줄을 적으면 Event1 줄 필수"를 대신함 — Event1 줄은 그 지점·지연을 바꿀 때만 적음. `FindEventsError`는 채운 뒤 보므로 그대로.
- 머리말: 묶음 SO `[Header("공격 — Events 기본 [Event1, 0, 0] = 공격 애니메이션 임팩트 타이밍")]`·`[Header("피격 — Events 기본 [Event1, 0, 0] = 피격 타이밍")]`. 툴팁·주석(동작 입력 Events·묶음 SO 두 칸·클래스 요약·공격 칸 값·기본 줄 문서) 고침.
- 확인: 부트 통과, Akimbo 두 발(지점 0 → 2)이 지금과 같은지, 다른 유닛 공격·피격이 지금과 같은지, 인스펙터 머리말 두 줄.

### 5.4 CS-e 문서·공유·묶음 끝 검토 (2026-11-17, 사용자 "Ud, Ub 문제 없음, CS-e, US-c: 진행해", 코드 변경 없음)

- **Ud 문제 없음(사용자).** 같이 한 것 1~5도 문제 없음.
- 문서: 이펙트·소리 창구 안내(시점 Event1~3·Contact+고르기·ActorEventPoint·Event Points 번호 규칙·유닛 소리 줄로 다시 씀, 제목 갱신), 사운드 작업자 안내(시점 낱말), 폴더 배치 안내(갱신 줄·Ordering 줄·§2 소리 규칙 둘), 옛 클립 설계 머리말 "대신함(일부)", 설계 §11 구현 메모.
- 공유(미전달): 민형님 새 메모 `handoffs/2026-11-17-combat-clip-slot-minhyeong-share.md`(프리팹 11개 Event Points 번호 규칙·클립 이름표 규칙·탑승물 칸·Events 머리말), 호진님 클립 메모 §12(묶음 칸 모양·외형 소리 줄 — 유닛 소리와 한 절).
- **묶음 끝 검토(새 에이전트 하나, 7기준, 유닛 소리 US-a·US-b와 한 번에) 약 8.3**(SSOT 8 · SOLID 8 · FW 9 · 독립성 8 · 캡슐화 8.5 · C#/Unity 8 · If/Tick 8.5). 높음 0 · 중간 2 · 낮음 8. 정적 컴파일 깨짐 없음, 옮긴 값 26개 모두 지금 소리 그대로(대조 유닛 소리 계획 §6.3).
  - M1(유닛 소리 쪽 — 유닛 소리 계획 §6.3).
  - **M2 앞당김이 동작을 시작하지 못해도 사건을 붙잡음**(코드로 확인): `CombatPreAttackPresentationStarts.TakeLeadHold`가 예약 직후 `-delay`를 돌려주고 `Flush`가 실제로 시작했는지 보지 않는다. 앞 행동 실행이 남았거나(`HasActionRun`·출처·재생 중) 선행 사건 자신이 공격자 의존이면 사건만 그 초만큼 늦고 동작은 공격 사건까지 시작하지 않는다. 지금 에셋(카우보이 끌어오기)은 앞 행동이 겹칠 때만. 권장: 예약이 "지금 시작했는지"를 돌려주고 시작했을 때만 붙잡음(아니면 0 + 경고 한 번).
  - 낮음: L1 낡은 주석·툴팁(반응 SO 피격 큐 툴팁의 "피격 소리"·"Cue", 큐 항목 요약, 창구 묶음 `ForAttack` 문서·지운 `AddSound` 주석, `CombatClipEventEntry.Point` "근접·피격은 0", 값 규칙의 "프리셋 소리 큐", 옛 이름 Cue·Hit 셋), L2 `CombatClipAttackData.LeadHoldSeconds`를 아무도 안 부르고 `TakeLeadHold`가 `-delay`로 다시 계산(SSOT 사본) — 그 속성을 쓰게 권장, 부르는 곳 없는 `CombatUnitSoundsData.IsEmpty` 등, L3 넘기는 프레임에 조각 `AdvanceBlend` + 같은 프레임 `TickTail`로 섞기·뒤 단계가 dt만큼 앞섬(한 프레임), L4 컨트롤러 없는 유닛은 Out을 켜면 본 단계 끝에 바로 Idle(지금 그런 유닛 없음 — Out을 꺼진 것으로 다루기 권장), L5 작업 트리 카우보이 `motionStartDelay: -0.3`(사용자 확인용 값 — 커밋 전에 둘지 정할 것), L6 유닛 소리 쪽, L7 유닛 소리 쪽, L8 구조(`CombatUnitSoundSet`이 사망 사건을 듦·한 파일 두 형식·시작 지연 칸 그리기가 툴팁을 덮음).
- **반영은 사용자 결정 대기.**

**검토 반영(11-17 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기):**
- M2: 앞당김은 동작이 실제로 시작했을 때만 선행 사건을 붙잡는다. 어댑터 `CombatPreAttackPresentationStarts.BindReceiver(reserve, beginNow)` — 사건 Presenter가 `beginNow = 예약 Flush 뒤 CombatPreAttackPlayback.HasStarted(lead)`(새 질문: 그 예약의 실행이 있고 버리지 않음)를 넘기고, `TakeLeadHold`는 시작 못 하면 경고 한 번 뒤 0(동작은 막힘이 풀릴 때, 늦어도 공격 사건에 시작). Presenter `TakeLeadHold`의 Flush는 어댑터로 옮김.
- L2: 어댑터가 공격 칸 값(`CombatClipAttackData`)을 들고 붙잡을 초는 `LeadHoldSeconds`, 시작 지연은 `MotionStartDelay`를 그대로 읽음(옛 `-delay` 사본 지움). 부르는 곳 없는 `CombatUnitSoundsData.IsEmpty`(이번에 만든 것) 지움. `CombatClipData.EventTime`·`UnitClipChannel.HasBase`·`HasOverlay`·`Graph`는 HEAD부터 있던 것이라 그대로(진단용).
- L3: `UnitClipChannel.HandOff`가 넘긴 프레임 번호를 기억하고 그 프레임의 `TickTail`은 건너뜀(대여가 이미 그 프레임 시간을 넣음).
- L4: 컨트롤러(Idle) 없는 유닛은 `HandOff`가 Out을 꺼진 것으로 봄(뒤 단계가 없으면 지금 자세로 남고 대여 반환이 돌려보냄).
- L1: 낡은 주석·툴팁 열(반응 SO 두 줄, 큐 항목 요약·플래시 입력, 창구 묶음 `ForAttack` 문서·지운 `AddSound` 주석, `CombatClipEventEntry.Point`, 값 규칙 두 곳, 큐 규칙 `RequireDefined`, 공격 값 비행 소리, 투사체 소리 값, 코드 모션 공격, 소리 줄 값 생성자 주석).
- L5: 카우보이 `motionStartDelay`를 −0.3 → 0으로 되돌림(11-17 사용자 "0으로 되돌려", WithLead 그대로). 그대로: L6·L7·L8(기록만). M1은 유닛 소리 계획 §6.3.
- **확인(사용자):** 컴파일, 카우보이 앞당김(−0.3)이 지금처럼 PullBack 먼저 → 0.3초 뒤 끌어오기, 앞 행동 재생이 남은 채 선행 사건이 닿을 때 사건이 늘어지지 않고 경고 한 줄, 블렌드 켠 카우보이 Out·뒤 단계가 지금과 같은지.

**검토 반영 확인 문제 없음(11-17 사용자 "확인 문제 없음").** 클립 칸 다시 짜기(CS-a~CS-e)는 사용자 커밋만 남음.
