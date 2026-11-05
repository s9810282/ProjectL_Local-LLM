# 전투 View GC 끊김 줄이기 — 로컬 세션 인계 문서

> 클라우드 세션에서 한 전투 View GC 끊김 작업(GC-a~GC-f, 검토 반영, 원격 병합)을 로컬 세션으로 넘기는 문서다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [계획](../plans/2026-11-03-combat-view-gc-hitch-plan.md) §3 → [설계](../specs/2026-11-03-combat-view-gc-hitch-design.md)** 순서로 읽는다.
> 실행 기록·결정의 원본은 계획 §3이다. 이 문서는 시작점과 남은 일만 적는다.

- 작성일: 2026-11-03
- 작성: View 작업(전투 View 담당, 클라우드 세션)
- 대상 브랜치: `feature/Yonggeun/Refector-Stage`
- 상태: **코드·문서 완료, 원격 병합 뒤 push 완료. 컴파일 미확인**(병합 뒤 Unity를 한 번도 열지 않음). 새 시험 없음, 시험 변경 없음.

## 1. 로컬에서 시작하기

```bash
git fetch origin feature/Yonggeun/Refector-Stage
```

```bash
git checkout feature/Yonggeun/Refector-Stage
```

```bash
git pull origin feature/Yonggeun/Refector-Stage
```

받은 뒤 Unity를 열어 컴파일부터 확인한다(§4의 1번).

## 2. 커밋

| 커밋 | 내용 |
|---|---|
| `bbbcb162` | GC-a~GC-f 코드·문서 + 묶음 끝 검토 반영(L1·L3·L4·L8·L9). 새 파일 `Presentation/Pieces/CombatEnumValues.cs`(+ `.meta`) |
| `5e668b2a` | `origin/feature/Yonggeun/Refector-Stage`(262커밋 앞섬) 병합. 충돌 12개(코드 9·문서 3)를 모두 양쪽을 살려 정리 — 계획 §3.9 |
| (이 문서 커밋) | 이 인계 문서·`CLAUDE.md` 한 줄 |

클라우드 세션은 사용자 지시("너 클라우드라서 푸쉬해줘야댐")로 커밋·push했다. **로컬 세션은 `CLAUDE.md` 규칙대로 커밋·push를 사용자가 직접 한다.**

## 3. 한 일 요약

목표: 적 턴 넘어갈 때·적 진행 중 GC 튐을 View 쪽에서 줄인다. 원인의 큰 쪽은 Core 적 AI 계산(천명님 영역)이라 범위 밖이다.

| 단계 | 내용 | 기록 |
|---|---|---|
| GC-a | 작은 할당 0: 입력 정리 대리자 미리 만듦, 열거 값 검사 `CombatEnumValues`(여덟 곳), `RemoveAll`·`Exists`·`SequenceEqual` 반복으로 | 계획 §3.1 |
| GC-b | 강조 그리기를 `FieldView.LateUpdate`에서 프레임당 한 번, 테두리·채우기 배열 재사용 | §3.2 |
| GC-c | 사실 객체가 같은 유닛은 HUD를 다시 쓰지 않음(`FieldUnitView`가 기억) | §3.3 |
| GC-d | HUD 위치·선택 HUD 검사·환경 설명 창을 바뀔 때만 | §3.4 |
| GC-e | 처음 나올 때 생성을 전투 설치 때로(`CombatSceneDisplay.Prewarm`: 유닛+HUD·고스트·투사체·몸 위 글자, 강조 판 깊이 0) | §3.5 |
| GC-f | 0단계 Profiler 표시 지움, 문서·현강님 메모, 묶음 끝 검토 약 9.6 | §3.6·§3.7 |
| 검토 반영 | L1 HUD 층 숨김 알림 `IUnitHUDAnchor.OnHudLayerHidden`, L3 재생 중이 아니면 바로 그림, L4 `FieldView` 실행 순서 10000, L8 HUD 기억 비우는 때 목록 한 곳, L9 닿지 않는 갈래 삭제 | §3.8 |
| 원격 병합 | 원격 강조 입구 둘을 대기열로, `UnitHUDLayer.Project` 안에 바뀔 때만 쓰기, 만든 수는 성공 뒤 세고 `ReleasePools`가 비움 | §3.9 |

다시 재고 싶으면 계획 §3.6의 0단계 표시 이름을 다시 넣는다. 원격에는 용근님 사전 준비 작업의 계측 표시(`Combat.Units.*`)가 따로 있다.

## 4. 남은 일 (순서대로)

1. **Unity 컴파일·확인** — 병합 뒤 처음이다.
   - Uf 1~4(계획 §3.8): 강조가 바로 나오고 사라지는지(UI 위로 옮길 때 호버 칸이 한 프레임 늦게 지워지지 않는지), HUD 층이 숨었다 나온 뒤 피격 체력바 애니메이션, 선택 정보 창·끌어 옮긴 뒤 HUD.
   - 병합 확인(계획 §3.9): Combat 진입 로딩 → 등장 연출 순서, 같은 씬 재진입 뒤 유닛·HUD.
   - 오류가 나면 병합으로 섞인 파일부터 본다: `FieldUnitPresenter`(대여·생성)·`UnitHUDLayer`(`Project`·준비 중 갈래)·`FieldView`(새 입구 둘)·`CombatSceneDisplay.Install`(미리 만들기 → 등장 연출 준비 순서).
2. **결정 필요 — 미리 만들기 겹침.** 원격 `7404e70e`(선택 유닛 사전 준비, 로딩 중 한 Tick에 하나씩 모델·HUD 준비)와 GH6 유닛 미리 만들기(설치 때 셀 캐릭터 전부)가 플레이어 유닛 일부를 같이 맡는다. 지금은 둘 다 둔다(GH6가 먼저 만들고 사전 준비는 그것을 빌려 HUD를 캔버스에서 데움). 원격 계측 `PreparedCapacityMiss`·`CreateOutsidePreparedCapacity`는 설치 때 GH6 생성을 "준비 밖"으로 센다. 한 곳으로 줄일지(예: 선택 유닛은 사전 준비만, GH6는 적·손패 밖만) 사용자가 정한다. 원격 쪽 설계·인계는 원격의 `docs/superpowers/specs/2026-11-02-combat-selected-unit-prewarm-design.md`·`handoffs/2026-11-03-combat-unit-prewarm-review-handoff.md`.
3. **공유 전달(미전달)** — 현강님 메모 [§21](2026-10-22-combat-view-structure-hyeongang-share.md)(HUD 그리기 건너뛰기·`UnitHUDLayer` 줄·남는 UISystem 매 프레임 쓰기).
4. **나중(Q-GH5 B, 사용자가 맡음)** — 설계 §5의 다른 영역 요청: UISystem `TextNoticeUI.LateUpdate`·`UnitHUDHoverPanel.RefreshPosition` 매 프레임 쓰기, Core 적 AI 계산(Core 전달 문서에 아직 안 적음).
5. **커밋** — 로컬에서 바꾼 것만. 위 커밋들은 이미 원격에 있다.

## 5. 받아들인 차이 (기록만)

- 강조 그리기 실패는 F1이 아니라 Unity 콘솔로 간다(검토 L2).
- 칸 변경·칸 무관 변경이 같은 프레임이면 `Refresh` 두 번(메시는 한 번).
- 유닛 표시 하나를 버리는 길(`DiscardView`)은 키를 몰라 만든 수를 빼지 않는다 → 미리 만들기가 하나 덜 만들 뿐(성능만).
- 그대로 둔 검토 항목 L2·L5·L6·L7(계획 §3.7).

## 6. 작업 규칙 (이번 작업에서 지킨 것)

전체는 `CLAUDE.md`. 이번 작업에서 따른 것만 적는다.

- View 작업이라 새 시험을 쓰지 않았고, 전투 View라 MSBuild도 돌리지 않았다. 보고에는 "컴파일 미확인".
- Phase마다 멈추고 사용자 Unity 확인(Ua~Ue)을 받은 뒤 다음으로 갔다. Uf와 병합 확인은 아직이다.
- 다른 작업자 줄을 고치면 그 사람 메모에 적는다(이번엔 현강님 HUD 줄만, blame이 호진님·천명님으로 나오는 줄은 저장소 전체 업로드 커밋이라 그분들 줄이 아님 — 계획 §3.6).
