# 전투 첫 동작·대기 Tick 성능 개선 — 2026-11-10

작성: Codex. 2차 분석·입장 준비/반복 미리보기 검토: Claude(2026-11-10, 아래 두 절). 기준 기록: `Logs/ProfilerExport/20261110-212514-765-4380a2dc`.

## 설계

목표는 첫 배치·공격·이동의 불필요한 초기화와 매 프레임 할당을 줄이는 것이다. 단순히 비용을 로딩으로 옮긴 뒤 제거됐다고 보고하지 않는다. Editor JIT와 실제 반복 계산·리소스 생성 비용을 구분한다.

- P1: View 계측의 일괄 생성 제거. `CombatPlaybackProfile`은 사용한 Owner/단계의 마커만 최초 접근 때 생성하고 캐시한다. 이름·호출 위치·범위는 유지한다. 매 Tick에 문자열을 만들지 않는다. Editor/Development 정의 밖에는 기존처럼 포함하지 않는다. 첫 이동의 정적 초기화는 네 Owner × 네 이동 종류 × 14단계 = 224마커를 미리 만들고 있었다.
- P2: 대기 할당 원인 분리. 5800~5999의 200프레임과 6000에서 GameBootstrap 아래 128B/frame, IMGUI BeginGUI 아래 368B/frame을 관측했다. 네 Group의 `IReadOnlyList<ISystemTick>` foreach가 후보다. Core/Architecture 소유자는 직접 바꾸지 않는다. View 개별 Poll의 할당으로 단정하지 않는다.
- P3: 첫 배치/공격/이동 준비 개선. JIT 합계는 배치 1404에서 141.468ms, 공격 미리보기 2552에서 127.285ms, 공격 실행 2630에서 48.349ms다. Core 초기 실행은 Core 전달 요청으로 분리한다. 첫 재질 검증/읽기 비용은 1778에서 18.017ms, 2630에서 23.467ms이며 렌더 담당 협의 없이 에셋·SRP 설정을 바꾸지 않는다. 로딩 준비 확장은 실제 사용하는 유닛/공격의 준비 소유자 안에서 결정해야 한다.

## Task·Phase

1. **P1 이번 Phase:** 기준 기록 분석, 대기 Tick 정적 점검, 계측 객체/마커 지연 생성 구현. 정적 검토 후 중단·보고한다.
2. **P2 후속:** 소유 영역 확인 뒤 Group 열거 할당 제거 또는 세부 계측으로 128B 호출자 확정. 대기 연속 200프레임 재측정.
3. **P3 후속:** Core 첫 실행/리소스 준비 담당 인수와 View 준비 수명 확정 뒤 초기화 개선. 최초·반복 배치/공격/이동을 같은 조건에서 비교한다.

P2·P3의 범위와 순서는 아래 "결정과 후속 검토"를 따른다(2026-11-10 사용자 답). 지금 정해진 것과 남은 것은 다음과 같다.

4. **P4(코드 완료 2026-11-11, 컴파일 미확인, 재생·측정 전):** 호버 범위 답 기억하기(Q-FP2 a, View). 기록은 아래 "P4 구현".
5. **P5(코드 완료·측정 2026-11-11 — FU-a·FU-b, Unity 컴파일 성공, 재생 확인 대기):** 새 기록 `20261111-002203-127-35b7ea71`에서 첫 배치 182 → 77ms, 첫 공격 미리보기 161 → 21ms, 첫 이동 실행 91 → 28ms, 준비 뺀 Core JIT 합계 약 436 → 56ms(준비 비용 671ms는 로딩 가림 중). 비교는 첫 동작 계획 "FU-c 비교". 첫 동작 Core 준비를 전투 Plan·Job으로, Combat 씬 로딩 준비 단계에서 건다(Q-FP1). Q-FP1a = 우리가 Core까지 만든다(2026-11-10 사용자). Task 계획은 [첫 동작 Core 준비 계획](2026-11-10-combat-first-use-preparation-plan.md)이다.
6. **P6(1단계 계측·측정 완료 2026-11-11, 2단계 보류 — 2026-11-11 사용자 "일단 미뤄두고"):** 강조 테두리 메시 GC(Q-FP3). 기록은 아래 "P6 1단계 계측". 2단계(View 자체 메시 또는 GlobalSystem 수정·요청)는 측정 뒤 정한다.
7. P2는 뒤로 미룬다(Q-FP4).

크래시 조사에 한정된 이전 Unity 직접 검증 승인을 이번 일반 View 최적화에 확대하지 않는다. Test 코드·MSBuild 없음. 컴파일·재생은 사용자 확인, **컴파일 미확인**. 코드 작성으로 ms 개선을 측정했다고 하지 않는다.

## 대기 코드 검토

- FrameworkRuntime, GlobalSystemGroup, JobSystemGroup, RenderSystemGroup, UtilitySystemGroup, LodingSystemGroup: Tick 전달은 인덱스 순회. Tick마다 람다 생성이 보이지 않는다.
- LifeCycleSystemGroup, InGameSystemGroup, InGameCoreSystemGroup, InGameViewSystemGroup: 인터페이스 반환 TickModules에 foreach. 열거자 할당 후보 4곳이며 128B/frame 관측과 정합적이나 CSV에 개별 호출 스택이 없어 확정하지 않는다.
- CombatSceneInputDriver: 후속 사실 공급 람다는 생성자에서 한 번 만들어 보관한다. TickInputs의 람다 신규 생성 없음.
- CombatPresentationPlayers: Tick·완료 검사는 배열 인덱스 순회. LINQ Where/ToArray는 생성 시 조립에만 쓰인다. Cancel/Release의 delegate는 매 프레임 경로가 아니다.
- CombatHudActionAvailability: 캐릭터 순회·딕셔너리 비교는 QueryRevision이 바뀔 때 수행한다. 대기 중 매 프레임 전체 재생성 경로가 아니다.
- CombatUnitMotionSet: Dictionary Values를 재사용 List에 복사하고 비활성 레인을 건너뛴다. 대기 CPU 작업 후보지만 이 기록에서 큰 병목이라는 증거는 없다. 용량 확장 외 매번 새 배열 생성 없음.
- CombatBodyTextWindow: 재사용 목록 역순 순회, 완료 때만 반환. 빈 목록 Poll 자체를 할당으로 간주하지 않는다.
- DelaySystem: 풀 생성 람다는 초기화 시점이며 Tick은 인덱스 순회/대기 큐 검사. ScreenTransitionSystem도 재사용 목록 인덱스 순회다.
- 대기 중 렌더 그림자 SRPBatcher.Flush 평균 self 0.263ms, RecordRenderGraph 0.252ms 등 렌더 경로 비용이 눈에 띈다. GPU 기록은 없어 GPU 병목을 판단하지 않는다. 모든 Tick을 끄거나 폴링 주기를 낮추는 변경은 하지 않는다.

### 대기 구간 해석 보완

5800~5999는 행동 마커가 아예 없는 후반 구간으로 전투 입력 대기와 동일하다고 단정하지 않는다. 별도로 Input/Command/Playback 실행 마커가 없는 1000~1239를 찾아 1000~1199의 200프레임을 확인했다. CPU 중앙값 8.2114ms, GameBootstrap.Update 평균 total 0.167552ms, 환경 호버 Poll 0.001472ms/0B, 회오리 표시 Poll 0.000824ms/0B, 카메라 Poll 0.000130ms/0B다. 200개 중 183프레임이 전체 496B였다. 나머지에는 TraitDescriptionBindings 정적 초기화, UI 활성화/TMP 갱신 등이 있어 전체 구간을 항상 496B 또는 순수 무작업이라고 하지 않는다.

UnitCardUI.Update 아래 GC.Alloc는 이 구간 12회 합계 1,920B다. 코드상 Update는 변경 확인 후에만 Refresh하며, 매 Update의 무조건 문자열 재생성/람다라고 단정할 근거는 없다. 공용 UI 담당 코드이므로 이번 수정에 포함하지 않는다.

## P1 구현·검토 결과

- CombatPlaybackProfile의 14개 마커를 nullable 구조체 필드에 캐시하고 최초 사용하는 단계만 생성하도록 변경했다. ByOwner도 요청한 Owner만 생성한다. 마커 이름 14종과 Owner 선택 분기, 기존 호출 위치를 유지했다. 호출자는 수정하지 않았다.
- 구조체 필드는 소유 객체 내부에 저장되며 별도 객체로 박싱하지 않는다. nullable 값의 ??= 접근에 캡처 람다/object/interface 변환을 넣지 않았다. 첫 사용 할당은 남고 반복 접근은 필드 값을 반환한다. 이를 전체 플레이 무할당으로 확대해서 주장하지 않는다.
- Core 초기 실행과 Group 열거 후보는 Core 경계 인계 §68에 요청으로 기록했다(미전달). Core/Architecture/FieldCamera 구현 변경 없음.
- scoped git diff --check 통과. 기존 staged 변경 보존, Test 코드·MSBuild·Unity 실행·커밋·push 없음. **컴파일 미확인, 개선량 실측 전**. P1 리뷰 지점에서 중단하며 P2/P3는 완료하지 않았다.

## 구조 검토

SSOT: 마커 이름/캐시는 CombatPlaybackProfile 한 곳. 게임 상태 사본 없음. SRP: 계측 생성 정책만 바꾸고 재생기 동작·Factory 조립을 유지. OCP: 기존 호출자/마커 이름 그대로. LSP: 같은 단계에 같은 마커 반환. ISP/DIP: 서비스/API 추가 없음. FW/JMH: 서비스 등록·수명·Core Buffer 변경 없음. 기능 독립성: 계측 비용 수정은 지원 클래스 하나로 제한하며 게임 이벤트 처리나 입력 규칙을 바꾸지 않는다. 캐시는 Unity 메인 스레드의 기존 재생 호출 전용이며 병렬 호출 계약을 새로 만들지 않는다.

## 기준 기록 2차 분석 (2026-11-10, Claude)

같은 익스포트를 `frame_markers.csv` 전체(546만 줄)로 다시 읽었다. 코드·에셋 변경 없음. Editor·Main Thread 기록이라 빌드 성능으로 단정하지 않는다. GPU 열은 비어 있다. 아래 수는 모두 이 기록 하나의 값이며 재측정 전 추정이 섞인 곳은 그렇게 적는다.

### 첫 동작과 반복 동작

| 동작 | 첫 번째 (CPU ms) | 두 번째 이후 | 첫 번째의 주 원인 |
|---|---:|---|---|
| 배치 | 1404: 182.0 | 1778: 50.1 → 1987: 13.1 → 2309: 15.8 | JIT 141ms, 그중 116ms가 Core.Combat.Command 안 |
| 공격 미리보기 | 2552: 160.7 | 9.6~13.7 (3985만 47.6) | JIT 127ms, 그중 120ms가 Core.Combat.Preview 안 |
| 공격 실행 | 2630: 108.3 | 2956: 17.4, 3297: 19.1 (5376만 57.9) | JIT 48ms(Core 16.7, View 22.8), 에디터 재질 로딩 약 20ms |
| 이동 미리보기 | 2816: 75.0 | 약 8 | JIT 45ms, Core.Combat.Preview 안 |
| 이동 실행 | 2832: 91.1 | 5642: 8.9 | Core JIT 32ms + `CombatUnitMotionPlayback..cctor` self 21.9ms |
| 적 턴 시작 | 4223: 48.5 | — | Core.Command 안 JIT 27ms |

- 두 번째 이후의 큰 프레임(3985 공격 미리보기, 5376 공격 실행)은 다른 유닛·넉백 경로를 **처음** 지나는 프레임이다. Core 안 JIT 25~30ms가 다시 난다. 반복 계산 자체는 대부분 20ms 아래다.
- JIT 합계: Core 마커 안 약 436ms(9개 프레임), View 마커 안 약 50ms(정적 초기화 둘 제외, 마커 밖 JIT 제외).
- **스크립팅 백엔드:** `ProjectSettings.asset`의 `scriptingBackend`에 Standalone 항목이 없어 PC 빌드는 기본값 Mono다. 그래서 JIT는 빌드에서도 남는다고 본다(에디터보다 작을 수 있음, 미측정). IL2CPP면 JIT 자체가 없다. 빌드 설정은 우리 영역이 아니다.
- **에디터 전용 비용:** 1778·2630·2694·2717·5479의 `ShaderGUIUtility.ValidateMaterial` → `Loading.ReadObject` → `File.Open`(재질이 처음 보일 때 에디터가 Library에서 읽음, 프레임당 최대 약 23ms)은 빌드에 없다. 고치지 않는다. 첫 셰이더 적용(`StdRender.ApplyShader` 1.5~2.6ms)은 빌드에도 있을 수 있다(미확인).
- **P1 대상 확인:** 정적 초기화 self는 `CombatUnitMotionPlayback` 21.85ms(2832), `CombatUnitReactionPresenter` 12.49ms(2694)다. 두 클래스의 정적 필드는 `CombatPlaybackProfile.ByOwner`뿐이라 P1 뒤 거의 사라질 것으로 본다. 다만 마커 하나 만드는 비용이 장면마다 다르다. 143(진입)의 `CombatEventProfiler..cctor`는 마커 약 2천 개에 self 3.34ms였고, 2832는 224개에 21.85ms였다. 그래서 감소량은 재측정으로 정한다. 이 계측은 Editor·Development 빌드에만 있다.

### GC 출처 (프레임 1240 이후, 출처가 잡힌 45.4MB)

| 출처 | 양 | 비율 | 영역 |
|---|---:|---:|---|
| `Core.Combat.Preview` | 21.12MB | 46.5% | Core |
| `FieldView.LateUpdate`(강조 테두리 다시 그리기) | 10.88MB | 23.9% | View → GlobalSystem MeshSystem |
| `Core.Combat.Command` | 5.68MB | 12.5% | Core |
| 렌더(에디터 IMGUI 포함) | 1.84MB | 4.0% | 엔진·에디터 |
| `Core.Calc.EnemyPreparation` | 0.95MB | 2.1% | Core |
| `GameBootstrap.Update` 직접(네 Group 열거자) | 0.83MB | 1.8% | 민형님·Core 등 |
| `Combat.Action.Interaction.MovePath`(Poll) | 0.11MB | 0.2% | View |

- 할당이 100KB를 넘는 98프레임이 37.8MB(78%)를 만든다. 5KB 미만 4,662프레임은 합쳐 2.76MB다. GC.Collect는 2870(6.12ms)·6117(7.26ms) 두 번이고, GC 사용 메모리 약 886→797MB, 898→801MB였다. GC를 부르는 것은 동작 순간의 할당이다.
- **`FieldView.LateUpdate`:** 다시 그리며 할당한 프레임이 195개다. 범위를 다시 그릴 때는 한 번에 300~540KB, self 약 1~5ms다(예: 2311 538KB, 3514 545KB/1193회). 그린 칸·색이 같으면 다시 만들지 않는 검사(`FieldOutlineRenderer.Apply`의 `slot.Drawn.SameAs`)는 이미 있다. 하위 마커가 없어 할당 줄은 확정하지 못했다. 가장 유력한 원인은 GlobalSystem `MeshHightLevelBuilder.CreateGridOutline`이다. 부를 때마다 HashSet·List 다섯·간선 List·Dictionary<_, List>·정렬 List·bool[]을 새로 만든다(코드 확인).
- 반복해도 무거운 Core 미리보기: AttackRange 3570·3853에서 Core.Combat.Preview 약 10~11.5ms(`Core.Calc.AttackSelection` self 약 7ms), 호출당 약 1.39MB.

### 대기 Tick (P2 근거 보강)

- `GameBootstrap.Update` 바로 아래 할당이 6,233프레임 중 5,900프레임에서 **정확히 4회 × 32B**다. 네 Group의 `foreach`(인터페이스 열거자 박싱)와 모양이 같다. 그래도 GC 전체의 1.8%라 GC 빈도에 주는 영향은 작다.
- `MovePath.Poll`(`CombatPathInput.Refresh`)이 3,491프레임에서 32B를 1회씩 쓴다. 경로를 짜는 중에는 없다. `CombatMovementRangeTracker.Clear`는 비어 있으면 바로 돌아가므로 원인이 아니다. 줄은 미확정이며, Profiler GC.Alloc 호출 스택으로 확인한다.
- 368B/frame은 `GUI.Repaint` → `GUIUtility.BeginGUI` → `EditorResources.Load`(에디터)다. 같은 경로에 `CinemachineBrain.OnGUI`가 있다. 빌드에서는 달라지므로 대상이 아니다.
- 상시 Poll 마커(카메라·환경 호버·회오리·경로·호버 등)는 나온 프레임당 평균 0.1~12μs다.
- 평소 프레임 PlayerLoop 약 5.3ms 중 렌더(`RenderPlayModeViewCameras`)가 약 4.0ms다. 그림자 0.55ms, 컬링 0.5ms, 외곽선 패스 약 0.33ms 등이다. Draw Calls 중앙값 5,385다. 영역 밖이라 기록만 한다.

## 입장 때 미리 처리·반복 미리보기 검토 (2026-11-10 사용자 질문)

사용자 질문: 큰 효과는 Core 쪽이지만, 첫 동작은 View가 Combat 입장 때 미리 처리해 두고, 미리보기는 View가 Preview를 반복해서 부르는 방식이 문제를 일으키지 않나.

### 미리보기 호출 실태

| 종류 | 호출 수 | 합계 ms | 중앙값 ms | 최대 ms | 호출당 할당 |
|---|---:|---:|---:|---:|---:|
| UnitPlacement | 34 | 10.7 | 0.28 | 0.46 | 26KB |
| MainCharacterPlacement | 5 | 16.7 | 0.42 | 15.33 | 28KB |
| ActionAvailability | 20 | 29.1 | 0.92 | 5.91 | 163KB |
| MoveRange | 13 | 20.6 | 1.50 | 6.33 | 221KB |
| UnitAttackRange | 25 | 68.0 | 1.44 | 32.39 | 233KB |
| Attack | 10 | 190.4 | 2.73 | 138.29 | 356KB |
| AttackRange | 7 | 65.9 | 4.10 | 32.94 | 575KB |
| Move | 1 | 57.7 | 57.67 | 57.67 | 1,053KB |

- 매 프레임 묻지 않는다. 슬롯이 하나라 한 프레임에 최대 1회이고, 사실이 바뀔 때만 묻는다(약 55초에 115회). 반복 호출의 CPU는 중앙값 0.3~4ms라 끊김 원인이 아니다.
- 문제는 **호출당 할당**이다. 미리보기가 전체 GC의 46.5%를 만든다. 호출 하나가 26KB~1MB를 쓰므로, 같은 일을 묻는 횟수가 늘면 GC.Collect가 그만큼 빨리 온다.
- 결과 직후의 연쇄: 턴 시작·결과 뒤에 HUD 가용성(`CombatHudActionAvailability`)이 사실이 바뀐 아군 **유닛마다 하나씩** 묻고(2324~2327 4회, 2729~2733 4회, 4730~4734 4회, 사이에 이동 모드 범위 1회), 이어 이동 모드 범위·호버 범위가 묻는다. 유닛 수만큼 Core가 입력 조립(`AssembleInput` 약 0.25ms)과 계산을 다시 하고, 할당도 유닛 수만큼 생긴다.
- 호버 범위(`CombatUnitRangeHover`)는 **마지막에 답받은 유닛 하나**(`_answeredUnit`)만 기억한다. A→B→A로 호버하면 같은 판에서도 A를 다시 묻는다. 이 기록으로는 유닛을 구분할 수 없어 중복이 몇 번인지는 미확정이다.

### View가 입장 때 미리 처리하는 방식의 문제

1. **명령 경로는 미리보기로 데워지지 않는다.** 배치 명령 JIT 116ms, 이동 32ms, 적 턴 27ms는 Core.Combat.Command 안이다. 데우려면 실제 명령을 내야 하는데, 이는 게임 상태를 바꾸는 가짜 명령이다(§68·인계 금지 사항).
2. **입장(배치 국면) 때는 공격·이동 미리보기를 부를 수 없다.** 필드에 아군이 없어 대상·행위자가 없다. 가장 큰 공격 미리보기 JIT(120ms)는 입장 때 데울 수 없다. 전투 시작(EnterCombat) 직후라면 부를 수 있지만, 그러면 같은 끊김이 그 순간으로 옮겨 갈 뿐이다.
3. **미리보기는 Buffer에 답을 게시하고 슬롯을 차지한다**(`CombatPreviewService.ProcessLatestPreview` → `_battle.PublishPreview`). 사용자가 하지 않은 조회가 HUD 가용성·이동 모드 범위·호버 범위의 배경 요청 규칙(R-a·R-b, `CombatPreviewSlotRule`)과 슬롯을 다툰다.
4. **무거운 경로가 어디인지는 Core 지식이다.** View가 "무엇을 미리 부를지" 목록을 들면 SSOT·기능 독립성을 깬다. Core가 바뀌면 그 목록은 조용히 낡는다. 처음 지나는 새 경로(3985, 5376)는 어차피 덮지 못한다.
5. View 자기 코드의 첫 실행(약 50ms, P1 뒤)은 View가 로딩 중 준비할 수 있는 몫이다. 하지만 실제 사건 없이 재생기를 돌릴 수는 없어 효과가 제한된다. P1 재측정 뒤 남는 양을 보고 정한다.

결론: View가 입장 때 Core를 대신 데우는 방식은 권하지 않는다. View가 할 수 있는 일은 미리보기 **횟수**를 줄이는 것이고, 호출당 비용과 첫 실행 준비는 Core 몫이다.

### 결정과 후속 검토 (2026-11-10 사용자 답)

사용자 답: "Q-FP1: Core에서 첫 준비하는 과정을 전투쪽 Plan-Job에 넣는 건 어때 / Q-FP2: 권장안대로 / Q-FP3: 좀 더 자세히 설명, View에서 처리가 된다면 View에서 처리하는 게 베스트, GlobalSystem은 안 건드는 방향이 가능하다면 그렇게 / Q-FP4: 뒤로 미뤄".

#### Q-FP1 — Core 첫 준비를 전투 Plan·Job에 넣기 (검토, 결정 대기)

맞는 점:

- Core를 부르는 자리는 LifeCycle Job이다(FW). 전투 골격(`LifeCycles/Combat/`)은 우리 영역이다.
- 새 전투 `JobType` 값과 `JobRegisterSystem`의 Combat 등록 줄은 영역 규칙의 "연결 작업"이다. 바꾸면 민형님 공유 문서에 적는다.
- Core가 이번 전투의 실제 유닛·특성으로 준비하면, 처음 지나는 새 경로(3985·5376)까지 덮을 수 있다. View가 목록을 들 때와 달리 무엇을 데울지는 Core 한 곳이 정한다.

주의할 점(시점):

- 지금 진입 연쇄 [준비 `CombatPrepareJob` → 전환 `CombatEntryTransitionJob`] 사이에 넣으면, 앞 씬(스테이지 맵·로비)이 보이는 동안 끊긴다. 맵 진입이면 노드를 누른 직후, 말이 움직이기 전이다.
- 그래서 Job은 **Combat 씬 로딩의 "씬 준비" 단계**에 건다. 이 단계는 화면이 가린 채 돌고, `LodingSystem`에서 AfterLoad → 준비 → Reveal 순서다. 가림이 풀리면 예외가 난다. View가 이미 같은 단계에 선택 유닛 준비(`CombatViewSystem.cs:247` → `CombatUnitPreparation`)를 등록해 두었다. 이 단계에서는 Combat LifeCycle이 이미 활성이다.

설계 초안(FP1, 결정 뒤 상세 설계):

1. 전투 골격에 로딩 준비 하나(가칭 `CombatFirstUsePreparation`)를 Combat 씬 준비로 등록한다. 등록 자리(CombatLifeCycle 또는 진입 서비스)와 View 준비와의 순서는 설계 때 정한다.
2. 준비가 시작되면 `Register(Combat, JobType.FirstUsePrepare)`(새 전투 값)를 건다. `CombatPlan.Register`는 이 값으로 Job 하나를 만든다(진입·착지 전환과 같은 꼴). Job이 끝나면 준비가 완료되고 화면이 드러난다.
3. Job은 Core 준비 입구 하나만 부른다. Core 쪽 약속은 넷이다.
   - 상태·Buffer·난수·사건을 바꾸지 않는다.
   - 무엇을 데울지는 Core가 이번 전투 정의로 정한다.
   - 프로세스에서 한 번만 한다. JIT는 프로세스 단위라, 두 번째 전투부터는 건너뛴다.
   - 한 번이 길면 로딩 화면이 멈춰 보이므로, 배치·공격·이동·적 턴 같은 단계로 나눠 여러 Tick에 걸쳐 돌 수 있게 한다.
4. 기대 효과: 첫 동작들의 Core JIT 합계 약 436ms 중 준비가 지나는 경로만큼이 첫 전투 로딩 시간으로 옮겨 간다(가린 화면 뒤). View 몫 약 50ms는 P3 View 쪽에서 따로 본다. 측정은 같은 시작 조건에서 첫 동작·반복 동작·로딩 길이를 함께 잰다.

막힌 것: Core 준비 입구(서비스 하나와 그 안의 준비 계산)는 Core 코드다.

- **Core 작업 문서(2026-11-10 작성, 사용자 판단 대기):** [첫 동작 Core 준비 작업 문서](../handoffs/2026-11-10-combat-first-use-preparation-core-work.md). Core 단계별 JIT, 지켜야 할 약속, 입구 모양 안, 준비 시점에 필드가 비어 있는 문제(D2), 게시 경로 38ms(D3), 정할 것 D1~D7이 들어 있다.
- **Q-FP1a(결정 2026-11-10: ㉠ 우리가 Core까지 만든다, 사용자 "우리측에서 작업하는 걸로 하고 Job에 거는 시점까지 계획으로 잡아"):** Task 계획은 [첫 동작 Core 준비 계획](2026-11-10-combat-first-use-preparation-plan.md)이다. 아래는 결정 전 질문 원문이다. 만들 사람을 정해야 한다. ㉠ 사용자 지시로 이 작업에서 Core를 고친다(Core 전달 문서 §5에 기록). ㉡ §68에 요청을 덧붙여 천명님께 넘기고, 우리는 골격 쪽(준비·Job·Plan·연결 줄)만 먼저 만든다. 우리 쪽만 먼저 만들면 부를 입구가 없으므로, 입구가 생길 때까지 등록하지 않는다.
- B(PC 빌드 IL2CPP 검토)는 이 안과 별도로 유효하다.

#### Q-FP2 — 호버 범위 답 기억하기 (결정: 권장안 a)

- Task P4(View, `Input/Interaction/CombatUnitRangeHover`): (전투 번호, `QueryRevision`, 유닛)마다 받은 답을 기억한다. 같은 판에서 같은 유닛을 다시 호버하면 묻지 않고 기억한 답으로 그린다. 판이나 전투가 바뀌면 비운다.
- 지금 기억하는 것은 마지막 답 하나(`_answeredUnit`)뿐이다. 배경 요청 규칙 R-b(`CombatPreviewSlotRule.ShouldRequestBackground`의 "이미 답받음")와 같은 판단 자리에 둔다. 답 객체는 Core 것을 읽기만 한다.
- 기억하는 수의 상한은 필드 유닛 수 정도로 둔다. 상세는 Task 착수 때 적는다.
- 확인: 같은 판의 재호버에서 `Preview.UnitAttackRange.Request`가 다시 나오지 않는지, 결과 뒤에는 다시 묻는지.

#### Q-FP3 — 강조 테두리 메시 GC 자세히 (Q-FP3a 결정: 계측부터, 2026-11-11 사용자)

원인:

- 테두리 핸들 하나를 그릴 때마다 GlobalSystem `MeshHightLevelBuilder.CreateGridOutline`이 새 컬렉션을 만든다. HashSet(칸), List 다섯(정점·인덱스·UV·법선·색), 간선 List, Dictionary(꼭짓점 → List<int>, 꼭짓점마다 List 하나), 정렬 List, bool[]이다.
- 다섯 List는 **칸 수 기준으로 용량을 미리 잡는다**(정점류 칸 × 16, 인덱스 칸 × 24). 칸 하나에 약 0.86KB다(Vector3 192B + int 96B + Vector2 128B + 법선 192B + Color 256B). 덩어리 테두리는 경계 변에만 정점을 쓰는데도 칸 전체만큼 잡는다.
- 범위가 100~200칸이면 핸들 하나에 약 90~200KB, 이동+공격 두 층이면 관측된 300~540KB와 맞는다. 계산에 의한 추정이며, 아직 마커로 확정하지 않았다.
- 메시 적용(`MeshNode.Apply`)은 `SetVertices(List)` 등이라 할당이 없다. 쓰레기는 모두 "만들기"에서 나온다.
- View 쪽은 이미 같은 칸·색이면 다시 그리지 않는다(`FieldOutlineRenderer.Apply`의 `slot.Drawn.SameAs`). 숨길 때는 빈 칸으로 `Update`한다(`HideArea`).

View만으로 처리하는 길:

| 길 | 효과 | 대가 |
|---|---|---|
| ① View가 테두리 메시를 직접 만든다(자기 MeshFilter·Mesh·재질, 버퍼 재사용) | 이 출처의 쓰레기 거의 0, GlobalSystem 무변경 | `CreateGridOutline`과 도우미 약 450줄(경계 고리·둥근 모서리·안쪽 거리·칸마다 방식·열린 변·점선 UV)을 View에 다시 만든다. 같은 모양 규칙이 두 곳에 생겨 SSOT·"있는 기능 사용"에서 감점이다. 재질·색 연결(ColorSystem 키)을 다시 짜고, 모양이 지금과 같은지 다시 확인해야 한다. 게임에서 GlobalSystem AreaOutline을 쓰는 곳이 없어진다(지우지 않고 보고). 작업량 중~대. |
| ② 만든 모양을 숨겨 두었다 다시 보이기(캐시) | 같은 모양이 다시 나올 때만 줄어듦 | 공개 계약 `IAreaOutlineHandle`에는 `Update`·`Dispose`뿐이고 숨김이 없다. 지금의 숨김도 빈 칸 `Update`라 메시를 지운다. 내부 형식으로 캐스트하면 FW 계약 위반이라 **공개 API로는 안 된다.** |
| ③ 호출 수 줄이기 | 계측 전에는 모름 | 같은 칸·색 건너뛰기는 이미 있다. 슬롯·깊이 판·높이 조각마다 `Update`가 몇 번 나는지 세어 보면 헛 호출이 있는지 알 수 있다. |

비교용(GlobalSystem을 고치는 길): `CreateGridOutline` 안에서 칸 수 기준 선할당을 줄이고 임시 컬렉션을 재사용한다. 모양과 동작은 같고 한 파일이며, 효과가 가장 크다. 민형님 영역이라 사용자 허락이나 요청이 필요하다.

권장 순서:

1. 먼저 View 안에서만 Editor·Development 전용 마커를 붙인다(`FieldOutlineRenderer`의 핸들 `Update` 둘레, 슬롯 종류별). 동작은 바뀌지 않는다. 이것으로 원인과 ③의 여지를 확정한다.
2. 그 결과로 ①(View 자체 메시)과 GlobalSystem 수정·요청 가운데 고른다.

효과 대비 비용으로는 GlobalSystem 쪽이 낫다. GlobalSystem을 끝까지 건드리지 않는다면 ①이 View 안에서 가능한 유일한 길이다.

- **Q-FP3a(결정 대기):** 계측(1단계)부터 할지, 바로 ①을 설계할지.

#### Q-FP4 — P2 Group 열거자 (결정: 뒤로 미룸)

P2는 Q-FP2·Q-FP3 뒤로 미룬다. 네 파일은 우리 영역 밖이고, 효과는 GC의 1.8%다.

## P4 구현 (2026-11-11, 사용자 "새 기준 기록 남겼으니까 확인하고 P4 진행해")

컴파일 미확인(Unity가 아직 가져오지 않음). 새 시험 없음, 시험 변경 없음, MSBuild 없음. 재생·측정 전.

**바꾼 것:** `ViewSystem/Combat/Input/Interaction/CombatUnitRangeHover.cs` 한 파일.

- 받은 유닛 범위 답을 (전투 번호, `QueryRevision`, 유닛)마다 기억한다. 저장 자리는 private 중첩 클래스 `RememberedAnswers`(사전 하나 재사용)와 값 `RememberedAnswer`(답을 받을 때 만든 배열 둘)다.
- 같은 판에서 같은 유닛을 다시 집으면(`Pick`), Core에 묻지 않고 기억한 답으로 그린다. 그리고 답을 받은 것으로 둔다(R-b 그대로). 이 판단은 원하는 유닛이 바뀔 때만 한다(`_answeredUnit != _wanted`). 매 Tick 할 일은 늘지 않았다.
- 요청 때의 판(`_requestBattle`·`_requestRevision`)과 답을 받을 때의 판이 같을 때만 기억한다. 다르면 그리기는 전과 같고 기억하지 않는다. 지금 그리지 않는 답(원하는 유닛이 바뀜, 살펴보기 불가, 이동 모드 행위자)도 같은 판이면 기억한다.
- 다른 판의 답은 쓰지 않는다. 새 판의 답을 기억할 때 지난 판의 답을 모두 버린다. `Clear()`(입력 정리·정지·강제 선택 종료)도 버린다. `Suspend()`(전경 진입·결과 재생 전·표현 정리)는 남긴다. 한 판의 수는 그 판에 집은 유닛 수를 넘지 않는다.
- 요청 식별 넷(번호·유닛·판 둘)은 `ForgetRequest` 한 곳에서 함께 잊는다.
- 계측: 기억한 답으로 그릴 때 `Combat.Action.Interaction.UnitHover.Remembered`(Editor·Development 전용). 적중 수를 Profiler에서 센다.

**보이는 차이:** 없음. 같은 판의 재호버에서 범위가 응답 대기 없이 바로 그려진다(한 프레임 이상 빨라짐). 결과 뒤(판이 바뀜)에는 전처럼 다시 묻는다.

**묶음 끝 검토(검토 에이전트 하나, 코드만 읽음):** 높음·중간 0, 낮음 5. 7기준 평균 약 8.9(SSOT 8, SOLID 8.5, FW 10, 독립성 9, 캡슐화 9, C#/Unity 9, If/Tick 9).

- 확인된 것: 판은 전투 설치 때 1, 늘기만 하고 `EndBattle`이 0으로 돌린다. 그래서 (전투, 판) 쌍은 다시 나오지 않는다. 답의 원본(캐릭터 사실·CommandState·영혼·보유·메인·아티팩트·재고·재화)을 전투 중 바꾸는 길은 모두 판을 올린다. 요청 값은 유닛 ID 하나라 View 상태(선택·이동 초안·공격 모드·튜토리얼)는 답에 들어가지 않는다. 표시(`CombatAttackFeedback.ShowHoverRange` → `FieldHighlightState`)는 배열을 복사만 하고 쥐지 않는다. 낡은 답 표시·요청 멈춤·중복 요청은 찾지 못했다.
- 반영: L3(요청 식별을 함께 잊음 → `ForgetRequest`), L4(필드 주석), L5(그리지 않는 답도 기억, 적중 마커).
- Core 전달 문서 §69(미전달): L1 "판이 같으면 답도 같다"는 지금 View 주석에만 있는 가정이다. 약속으로 적고, 대안으로 `UnitAttackRangeResult.SourceRevision`을 부탁했다.
- 그대로 둔 것: L2 최신 여부 규칙이 둘이다(이 기억은 (전투, 판), `CombatActionAvailabilityTracker.Matches`는 (전투, 진영 턴, 판, 결과 순번, 행위자)). "Core 조회 원본" 값 하나로 묶는 것은 다음 정리 후보다. 둘이 묻는 Core 조회가 달라 이번 범위에서는 합치지 않는다.

**확인(사용자):**

- Unity 컴파일
- 같은 판에서 A → B → A 호버: 두 번째 A에서 `Combat.Action.Preview.UnitAttackRange.Request`가 다시 나오지 않고 `Combat.Action.Interaction.UnitHover.Remembered`가 나오는지
- 이동·공격 결과 뒤 같은 유닛 호버: 다시 묻는지(새 판)
- 적 턴 뒤·두 번째 전투에서 범위가 옛 것이 아닌지

## P6 1단계 계측 (2026-11-11, 사용자 "계측부터" = Q-FP3a 권장 순서 1)

컴파일 미확인. 새 시험 없음, 시험 변경 없음, MSBuild 없음. 동작 변화 없음. GlobalSystem 무변경.

**바꾼 것:** `ViewSystem/Combat/Display/Field/FieldOutlineRenderer.cs` 한 파일. 모두 `UNITY_EDITOR || DEVELOPMENT_BUILD` 안이라 일반 배포 빌드에는 없다.

| 마커 | 자리 | 보는 것 |
|---|---|---|
| `Combat.View.Field.Outline.Refresh` | `Refresh` 전체 | 강조 다시 그리기 한 번의 전체 비용·할당 |
| `Combat.View.Field.Fill.Apply` | 채우기 부품 `Apply` | 칸 채우기 판(`FieldCellFillStack`) 몫 |
| `Combat.View.Field.Path.Apply` | 이동 경로를 실제로 다시 그릴 때 | 경로 띠·화살촉 몫 |
| `Combat.View.Field.Outline.Draw.<핸들 이름>` | 핸들 `Update`(칸 있음) | GlobalSystem `CreateGridOutline` 몫. 핸들 이름은 `FieldOutline_<층>`·`_Inset<깊이>`·`_<높이 순번>`이다 |
| `Combat.View.Field.Outline.Clear.<핸들 이름>` | 핸들 `Update`(빈 칸 = 숨김) | 숨길 때의 몫 |

- 우리 칸 배열(`CellArrayOf`)은 핸들 마커 밖에서 채운다. 그래서 `Draw`·`Clear` 안의 `GC.Alloc`은 GlobalSystem 메시 만들기 몫만이다. 우리 배열 할당은 `Outline.Refresh` 바로 아래에 보인다.
- 핸들 마커는 핸들마다 처음 쓸 때 만든다(P1과 같은 지연 생성, `Slot.UpdateMarker`). 이름은 핸들 이름 하나에서 나온다.
- 같은 칸·색이면 `Update`를 부르지 않는 기존 검사(`slot.Drawn.SameAs`)는 그대로다. 그래서 핸들 마커의 호출 수가 곧 실제 메시 다시 만들기 횟수다.

**7기준 자체 점검:** SSOT(마커 이름은 핸들 이름에서 나옴, 새 사실 없음), SOLID(계측만, 책임 그대로), FW(GlobalSystem·서비스·Buffer 무변경), 독립성(한 파일), 캡슐화(핸들 마커는 private 중첩 `Slot` 안), C#/Unity(정적 마커 셋 + 지연 생성 nullable 구조체, `#if`로 배포 빌드 제외), If/Tick(`_fills?.Apply`를 `if (_fills != null)` 블록으로 바꾼 것뿐, 새 검사 없음). 검토 에이전트는 돌리지 않았다(계측만이라 2단계 묶음 끝에 함께 검토).

**측정(사용자):**

- 이동 범위·공격 범위·호버를 여러 번 켜고 끄는 짧은(약 1분) 기록.
- 보는 것:
  - `FieldView.LateUpdate` 할당 중 `Outline.Draw.*`·`Fill.Apply`·`Path.Apply`·`Outline.Refresh` 직접 몫의 비율
  - 핸들별 호출 수·호출당 할당
  - 한 프레임에 같은 핸들이 두 번 이상 그려지는지(③ 헛 호출)
  - `Clear.*`가 할당하는지
- 그 결과로 2단계를 고른다: ① View 자체 메시, ② GlobalSystem `CreateGridOutline` 선할당·재사용 수정(민형님 영역, 허락이나 요청 필요), ③ 헛 호출 줄이기.

## 측정 (2026-11-11, 기록 `20261111-004603-874-69af22c1`, 6,377프레임, 약 61초)

- **빌드:** 플레이 전 마지막 빌드에는 P4(00:37 수정)가 들어 있었다. P6 계측(`FieldOutlineRenderer`, 00:43 수정)은 들어 있지 않았다. P6 계측은 플레이 뒤 00:49에야 컴파일됐다(`ProjectL.Systems.dll` 00:49:27, `error CS` 없음). 그래서 이 기록에 `Combat.View.Field.*` 마커가 없다. Unity는 에디터 창에 포커스가 올 때 다시 컴파일하므로, 스크립트를 고친 직후 바로 Play를 누르면 옛 빌드로 돌 수 있다.
- **P4 확인:**
  - `Combat.Action.Preview.UnitAttackRange.Request`는 6번, `Combat.Action.Interaction.UnitHover.Remembered`는 24번이다. 같은 판의 재호버 80%가 Core에 묻지 않았다.
  - Core 유닛 범위 조회는 한 번에 약 1.26ms·221KB다(`Core.Combat.Preview.UnitAttackRange`). 그래서 이 플레이에서 약 5.3MB 할당과 약 30ms CPU를 덜 썼다고 본다(호출당 값 × 적중 수, 추정).
  - 화면이 맞는지(결과 뒤 다시 묻기, 옛 범위 없음)는 사용자 재생 확인이 따로 필요하다.
- **P6 기준값(계측 전):**
  - `FieldView.LateUpdate` 할당은 모두 11.52MB다. 할당이 있는 프레임은 136개이고, 10KB 이상 95개, 100KB 이상 33개(9.58MB, 83%)다.
  - 가장 큰 프레임은 2108(482KB·3.74ms), 2308(459KB), 4968(454KB)이다. 모두 하위 마커가 없는 `GC.Alloc` 직접 몫이다.
  - 이 기록 전체 할당(첫 프레임 112.6MB 제외)의 약 18%다.
  - 2단계 비교는 이 값과 한다.
- **다음:** 계측이 들어간 빌드로 짧은 기록을 다시 받는다. Play 전에 Unity 창을 한 번 눌러 컴파일이 끝났는지 확인한다(하단 진행 표시 또는 Console).

## P6 1단계 측정 결과 (2026-11-11, 기록 `20261111-005950-111-34ac0f41`, 6,284프레임, 약 62초)

계측이 들어간 빌드로 돌았다(`Combat.View.Field.*` 마커 있음). 호버를 많이 한 플레이다. 수는 이 기록 하나의 값이다.

- **크기:** `FieldView.LateUpdate` 할당은 56.45MB다. 이 기록 전체 할당(첫 두 프레임 제외 98.8MB)의 약 57%다. 모두 우리 마커 안이고, 마커 밖 직접 할당은 0이다.
- **나눔:**

| 출처 | 할당 | 비율 | 호출 | 호출당 |
|---|---:|---:|---:|---:|
| 테두리 그리기 `Outline.Draw.*`(GlobalSystem `CreateGridOutline`) | 46.95MB | 83% | 367 | 평균 131KB |
| 칸 채우기 `Fill.Apply`(→ `FieldCellFill.Update` → GlobalSystem `GridCube`) | 9.29MB | 16% | 490 | 평균 19KB |
| 테두리 숨기기 `Outline.Clear.*` | 0.16MB | 0.3% | 820 | 0.2KB |
| 우리 칸 배열 등(`Outline.Refresh` 직접) | 0.10MB | 0.2% | — | — |
| 이동 경로 `Path.Apply` | 0 | 0 | 13 | 0 |

- **가장 비싼 핸들:** 겹친 칸 판 테두리(`_Inset`)다. `AllyHoverRange_Inset2` 16.3MB(76번, 호출당 220KB), `AllyHoverMoveRange_Inset1` 15.9MB(76번, 215KB), `MoveRange_Inset1` 7.9MB(29번, 278KB). 덩어리 테두리(`MoveRange` 50KB, `Hover` 11KB)보다 4~25배 크다.
  - 원인(코드 확인): 기본 에셋의 겹친 칸 방식이 칸마다(`overlapCellOutlineStyle: 1` = EachCell, 모서리 0.35)다. 그래서 `CreateGridOutline`이 칸마다 고리를 만들고, `RoundCorners`가 칸마다 점 목록 셋·배열 셋을 새로 만든다. 덩어리 방식(`cellOutlineStyle: 0`)은 경계 고리 하나다.
- **아군 호버 한 번:** 그 프레임에 837KB다(상위 10프레임이 모두 837.3KB). P4로 Core 조회(221KB)는 다시 호버할 때 없어졌지만, 그리기는 호버할 때마다 그대로 다시 한다.
- **③ 헛 호출:** 한 프레임에 같은 핸들을 두 번 그린 적은 0번이다. `Refresh`는 316프레임 중 175프레임에서 두 번 돌지만(칸·칸 무관 변경이 같은 프레임, GC-b 때 알던 것), 두 번째는 같은 칸·색 검사로 메시를 다시 만들지 않는다. 그래서 줄일 헛 호출은 거의 없다.
- **CPU·GC 수집:** `Outline.Refresh`는 492번에 모두 286ms(호출당 0.58ms)다. 가장 큰 프레임은 15.97ms다. 플레이 중 `GC.Collect`는 3318(6.9ms)·6153(0.9ms)·6156(7.6ms)이다. 할당은 크지만, 이 기록에서 수집 한 번의 멈춤은 약 7ms다.

**2단계 선택지(측정 반영):**

| 안 | 할 일 | 효과 | 대가 |
|---|---|---|---|
| A. GlobalSystem 수정·요청 | `CreateGridOutline`·`RoundCorners`·`GridCube` 만들기의 임시 컬렉션 재사용, 칸 수 기준 선할당 줄이기 | 이 출처 할당 대부분(최대 약 56MB/분) | 민형님 영역이라 사용자 허락 또는 요청 문서가 필요하다. 한두 파일, 모양 같음. |
| B. View 자체 메시(①) | 테두리와 채우기 메시를 View가 직접 만들고 버퍼 재사용 | A와 같음 | 두 빌더 약 450줄+를 View에 다시 만든다(SSOT 감점, 모양 다시 확인). 작업량 대. |
| C. 겹친 칸 방식을 덩어리로 | 기본 에셋 `overlapCellOutlineStyle` 1 → 0 | 겹친 칸 핸들 호출당 약 215~280KB → 약 50KB 수준으로 추정(호버 한 번 837KB → 약 250KB) | 화면이 바뀐다(5판에서 사용자가 고른 "칸마다 네 변" 모습). 코드 변경 없음. |
| D. 그대로 둠 | 없음 | 없음 | 수집 한 번 약 7ms, 1분에 두 번 정도라 끊김으로 크게 보이지 않는다. |

권장: A를 요청 문서로 민형님께 넘기고(View 코드 변경 없음), View 쪽은 지금 그대로 둔다(D). 효과는 A가 가장 크고, B는 같은 효과에 비용이 크다. C는 모습이 바뀌므로 기획 판단이다. 1단계 마커는 2단계 비교에 쓰므로 남긴다.

**보류(2026-11-11 사용자 "일단 미뤄두고 현재까지 작업한거 정리"):** 2단계는 고르지 않고 미룬다. 요청 문서도 아직 쓰지 않았다. 다시 시작할 때 이 표에서 고르고, 1단계 기록(`20261111-005950`)을 기준으로 비교한다.
