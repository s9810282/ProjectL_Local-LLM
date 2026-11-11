# 전투 첫 동작·대기 성능 세션 정리 — 2026-11-11

2026-11-10~11 세션에서 한 일, 바뀐 파일, 남은 일을 한 곳에 모은다. 사용자가 "일단 미뤄두고 현재까지 작업한거 정리"라고 해서 P6 2단계는 보류로 두고 여기서 멈췄다.

세부 기록의 원본은 다음 두 계획이다. 이 문서는 요약만 담는다.

- [성능 계획](../plans/2026-11-10-combat-first-use-performance-plan.md): 기준 기록 분석, P1·P4·P6, 측정
- [첫 동작 Core 준비 계획](../plans/2026-11-10-combat-first-use-preparation-plan.md): FU-0~FU-b, 검토, FU-c 비교

이 세션은 Test 코드를 쓰지 않았고 MSBuild를 돌리지 않았다. 컴파일과 재생은 사용자가 Unity에서 확인했다. Claude는 Unity를 직접 조작하지 않았고 커밋·push하지 않았다.

## 1. 한 일

| 작업 | 내용 | 상태 |
|---|---|---|
| 기준 기록 2차 분석 | 첫 동작마다 Core JIT가 모두 약 436ms. GC 출처는 Core 미리보기 46%, 강조 테두리 메시 24% | 완료(코드 변경 없음) |
| P1 계측 마커 지연 생성 | `CombatPlaybackProfile`이 쓰는 마커만 처음 쓸 때 만든다(이전에는 첫 이동 때 224개를 한꺼번에 만들었다) | 컴파일 성공, 측정에서 정적 초기화 비용이 사라진 것 확인 |
| FU-a·FU-b 첫 동작 Core 준비 | Combat 씬 로딩 화면이 덮은 동안 Core 리허설을 Job으로 돌려, 첫 배치·공격·이동·적 턴의 Core JIT를 미리 끝낸다. Core는 사용자 지시로 이 작업에서 고쳤다 | 컴파일 성공, 측정 완료, 재생 확인 대기 |
| P4 호버 범위 답 기억 | 유닛 범위 답을 (전투 번호, 조회 판, 유닛)마다 기억한다. 같은 판에서 같은 유닛을 다시 호버하면 Core에 묻지 않는다 | 컴파일 성공, 측정 완료, 화면 확인 대기 |
| P6 1단계 테두리 계측 | `FieldOutlineRenderer`에 Editor·Development 전용 마커를 넣어 강조 그리기 할당을 나눠 볼 수 있게 했다 | 컴파일 성공, 측정 완료, **2단계 보류** |

### 측정 결과

| 첫 동작 | 이전 CPU ms | 이번 CPU ms |
|---|---:|---:|
| 배치 | 182.0 | 77.3 |
| 공격 미리보기 | 160.7 | 20.6 |
| 공격 실행 | 108.3 | 40.8 |
| 이동 미리보기 | 75.0 | 22.1 |
| 이동 실행 | 91.1 | 27.8 |
| 적 턴 시작 | 48.5 | 기록에 없음 |

- 준비를 뺀 Core JIT 합계는 약 436ms에서 56ms로 줄었다. 대신 로딩 화면이 덮은 동안 준비에 671ms가 든다.
- 첫 배치에 남은 Core JIT 29.7ms는 결과를 Buffer에 쓰는 경로다. 처음부터 그대로 두기로 정한 몫이다(계획 FU-P3).
- P4: 기록 하나에서 유닛 범위 요청은 6번, 기억한 답으로 그린 것은 24번이었다. 할당 약 5.3MB를 덜 썼다고 본다(추정).
- P6: 강조 그리기 할당 56.45MB 가운데 83%는 GlobalSystem 테두리 메시 만들기, 16%는 칸 채우기 메시 만들기다. 우리 View 코드가 직접 할당한 몫은 0.2%다. 한 프레임에 같은 테두리를 두 번 그리는 헛 호출은 없었다.
- 기록: 기준 `20261110-212514`, FU-c `20261111-002203`, P4 `20261111-004603`, P6 `20261111-005950`(모두 `Logs/ProfilerExport/` 아래).

### 검토

| 묶음 | 결과 | 반영 |
|---|---|---|
| FU-a·FU-b | 중간 1·낮음 8, 7기준 약 8.1 → 반영 뒤 예상 약 8.4 | 중간 1(리허설 공격이 빈 칸을 조준)과 낮음 넷 고침, 낮음 다섯 그대로 |
| P4 | 높음·중간 0, 낮음 5, 7기준 약 8.9 | 낮음 셋 고침, L1은 Core 약속으로 적음, L2 그대로 |
| P6 1단계 | 계측만이라 검토 에이전트는 돌리지 않고 7기준만 직접 점검 | — |

## 2. 바뀐 파일 (커밋은 사용자)

### 묶음 A — 첫 동작 Core 준비(한 커밋)

서비스 키·Factory·그룹·Plan·Job이 서로를 참조하므로 함께 넣어야 컴파일된다. 새 `.cs`는 Unity가 만든 `.meta`와 함께 넣는다.

- 새 파일
  - `RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/ICombatFirstUsePreparation.cs`
  - `RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatFirstUsePreparationService.cs`
  - `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatFirstUseRehearsal.cs`
  - `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatFirstUseLoadingPreparation.cs`
  - `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatFirstUsePrepareJob.cs`
- 바꾼 파일
  - Core: `CoreSystem/CombatSystem/CoreProfilerMarkers.cs`, `CoreCaculator/Application/Diagnostics/CoreProfiling.cs`, `CoreSystem/InGameCoreSystemGroup.cs`
  - 연결 줄: `Factory/Runtime/InGame/Core/CombatSystemFactory.cs`, `ServiceKeys/Runtime/InGame/Core/InGameCoreSystemServiceKey.cs`, `GlobalSystem/JobSystem/JobRegister/JobRootType.cs`
  - 골격: `LifeCycles/Combat/Plans/CombatPlan.cs`, `LifeCycles/Combat/CombatEntryService.cs`, `LifeCycles/Combat/CombatEntryRequest.cs`

### 묶음 B — View 성능(각각 따로 커밋해도 된다)

- P1: `ViewSystem/Combat/Playback/Support/CombatPlaybackProfile.cs`
- P4: `ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs`
- P6 1단계: `ViewSystem/Combat/Display/Field/FieldOutlineRenderer.cs`

### 묶음 C — 문서

- 새 문서: 계획 둘(`plans/2026-11-10-combat-first-use-performance-plan.md`, `plans/2026-11-10-combat-first-use-preparation-plan.md`), Core 작업 문서(`handoffs/2026-11-10-combat-first-use-preparation-core-work.md`), 프로파일러 세션 인계(`handoffs/2026-11-10-profiler-performance-session-handoff.md`), 이 문서
- 바꾼 문서: Core 전달 문서(§5.84·§68·§69, 제목 갱신), 민형님 Factory 공유 §54, `CLAUDE.md`

### 이 작업이 아닌 변경 (커밋에서 나눌 것)

작업 트리에 함께 있지만 이 세션이 고치지 않은 것이다.

- `ViewSystem/Combat/Display/Field/FieldCellFillStack.cs`와 `Prefabs/Combat/Shaders/CombatCellFill.shader`(구멍 모양 배열 길이 변경)
- 스테이지·View 에셋(`Combat_LD_P35`, `Test 1`, `StageViewProp_LD_P35`, `CombatUnitAnimation_FixSo`), `Assets/Scenes/Dev/CombatMapAuthoring.unity`
- 에디터가 바꾸는 파일: FMOD 캐시·이벤트 카탈로그, Addressables 설정, 글꼴 아틀라스

## 3. 남은 일 (권장 순서)

1. P4 화면 확인: 같은 판에서 A → B → A 호버(두 번째 A에서 `Combat.Action.Interaction.UnitHover.Remembered`만 나오는지), 이동·공격 뒤와 적 턴 뒤에 범위를 새로 그리는지.
2. FU 재생 확인: Combat 직행 진입과 맵 진입 로딩, 두 번째 전투, 로딩 중 종료.
3. 적 턴까지 넣은 짧은 기록. 적 턴 시작을 기준(48.5ms)과 비교한다.
4. 커밋: 위 묶음대로.
5. 공유 전달(모두 미전달)
   - 천명님: Core 전달 문서 §5.84(Core 변경 기록), §68(첫 동작 요청·구현·측정), §69(유닛 범위 답은 조회 판이 덮는 사실에서만 나온다는 약속)
   - 민형님: Factory 공유 §54(연결 줄)

## 4. 보류·결정 대기

- **P6 2단계(보류, 2026-11-11 사용자):** 테두리 메시 GC를 어떻게 줄일지 고르지 않았다.
  - 선택지: A GlobalSystem 수정·요청(민형님 영역), B View 자체 메시, C 겹친 칸 방식을 덩어리로(에셋 값 하나, 화면이 바뀜), D 그대로 둠
  - 권장: A를 요청 문서로 넘기고 View는 그대로 둔다(D)
  - 1단계 마커는 2단계 비교에 쓰려고 남겨 두었다.
- **P2 Group 열거자 할당:** 효과가 GC의 1.8%라 뒤로 미뤘다(Q-FP4). 고칠 네 파일이 우리 영역 밖이다.
- **P4 검토 L2:** "Core 조회 결과가 아직 최신인가"를 판정하는 규칙이 두 곳에 따로 있다(호버 기억, `CombatActionAvailabilityTracker`). 다음 정리 후보로만 적었다.
- **FU 검토에서 그대로 둔 낮음:** L2(로딩 완료가 Runner 순서에 묶임), L3(일찍 끝난 준비를 다시 하지 않음), L4(자동 국면 조건 사본), L8(단계마다 다시 조립), L9(IL2CPP에서는 효과 없음). 이유는 첫 동작 계획 §8에 적었다.
- **View 첫 실행 JIT:** View 마커 안 JIT가 모두 112.6ms(프레임마다 6~12ms) 남아 있다(P3 View 쪽). 실제 사건 없이 재생기를 돌릴 수 없어 효과가 제한된다. 아직 손대지 않았다.

## 5. 평가 아티팩트

[전투 View 평가](https://claude.ai/artifact/5WNzcEZ1sPSaFw5ennuoaY) '적용 기록'에 두 차수를 더했다. 둘 다 평가 문제 목록 밖이라 점수 변화는 없다(12기준 85.6, 요청 7기준 81.4).

- 11차: P1, 첫 동작 Core 준비(FU-a·FU-b)
- 12차: P4, 11차 측정 결과(FU-c)

P6 1단계는 계측만이라 차수를 더하지 않았다.

## 6. 함께 돈 세션

"큰 프로파일러 원본 자동 변환 크래시 막기" 세션의 기록 도구 변경은 사용자 커밋 `2f065c9f1`("프로파일러 익스포터 업데이트")에 들어갔다.
