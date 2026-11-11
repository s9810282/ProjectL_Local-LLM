# 프로파일러·첫 동작·대기 Tick 성능 세션 인계 — 2026-11-10

## 다음 세션 시작점

사용자는 첫 유닛 배치·공격·이동의 순간 지연 개선과 아무 입력이 없을 때 Tick의 CPU/힙 할당/람다 점검을 요청했다. 구조체의 클래스 필드 저장, 람다 캡처, 박싱도 구분해서 주의하라고 지시했다. **P1 코드 반영·정적 점검 후 리뷰 지점에서 중단**했다. 다음은 P1 Unity 확인과 P2/P3 진행 범위 검토다. 이 인계 요청 자체는 후속 구현 시작 지시가 아니다.

먼저 읽을 파일:

1. 저장소 `AGENTS.md`, `Assets/AGENTS.md`.
2. [성능 개선 설계·Task·P1 기록](../plans/2026-11-10-combat-first-use-performance-plan.md).
3. [Core 경계 인계 §68](2026-09-24-view-core-data-boundary-handoff.md) — 첫 동작 JIT/Group 할당 조사 요청, **미전달**.
4. 익스포터 문제가 재발하면 [연속 기록 계획 C2](../plans/2026-11-10-profiler-continuous-capture-plan.md), [설계](../specs/2026-11-10-profiler-continuous-capture-design.md), [사용 안내](../viewDocs/combat-action-profiler-guide.md).

## 권한·작업 제한

- 커밋/push 금지. 사용자 staged/unstaged 변경이 많이 섞여 있다. reset/일괄 정리/일괄 stage하지 않는다.
- 설계 → Task → Phase, Phase 끝 보고·중단. 계획과 다른 결정은 먼저 질문한다.
- View Test 코드 작성 금지, 전투 View MSBuild 금지. **이번 P1은 컴파일 미확인·성능 재측정 전**이다.
- 이전 Unity 컴파일·재생 직접 실행 승인은 **크래시 조사에 한정**됐다. 일반 성능 최적화로 확대하지 않는다. P1 작성 후 Unity를 직접 조작하지 않았다.
- Core·FieldCamera·Architecture 소유 영역 직접 수정 금지. Core 요청은 기존 경계 인계에 기록한다. 그 문서를 바꾸면 제목에도 최신 주제/갱신일 반영.
- SSOT/SOLID/FW/JMH와 기능 변경 독립성을 검토한다. 상태/규칙을 View에 복제하거나 가짜 게임 명령으로 워밍업하지 않는다.

## 기준 기록과 확인된 사실

최신 사용자 익스포트(인계 시 확인):

`Logs/ProfilerExport/20261110-212514-765-4380a2dc/`

연결 원본:

`Logs/ProfilerCapture/20261110-212332-e442bb2eed344a6da006aa1a4b09b86c/capture.raw`

- 녹화 21:23:32~21:24:42 KST, 약 70초. 정상 Play 종료, Converted.
- CSV 프레임 0~6233, 6,234행. 번호 공백/중복 없음, 스킵/Unity 누락 경고 0. 네이티브 내부 무손실 증명과는 구분한다.
- 포효 5491~5695: Dispatch/Handle/Prepare/Begin/Tick/Hit/End 확인. CPU 중앙값 8.3122ms, 최대 14.1887ms, 16.67ms 초과 0. 포효 Tick 최대 3.0967ms. 구간 최대 5666은 낙하·사망 표시/JIT도 겹친 프레임이다.
- GPU 열은 비어 있다. Main Thread/Editor CPU 기록이며 빌드 성능으로 단정하지 않는다. 프레임 0의 12.857초는 Play 진입 비용이다.

| 동작 | 프레임 | CPU ms | 근거 |
|---|---:|---:|---|
| 첫 메인 배치 처리 | 1404 | 182.041 | JIT self 합계 141.468, Core.Command 135.534, 배치 Dispatch 8.112 |
| 다음 배치 | 1778 | 50.146 | ShaderGUIUtility.ValidateMaterial 18.017, 배치 Dispatch 10.588 |
| 배치 직후 범위 미리보기 | 1780 | 50.781 | Core.Preview 32.386, JIT self 28.266 |
| 공격 미리보기 | 2552 | 160.653 | Core.Preview 138.291, JIT self 127.285 |
| 공격 실행 | 2630 | 108.314 | JIT self 48.349, 재질 검증 23.467 |
| 이동 미리보기 | 2816 | 74.994 | Core.Preview 57.667, JIT self 48.032 |
| 이동 실행 | 2832 | 91.135 | JIT self 38.934, CombatUnitMotionPlayback 정적 초기화 self 21.845 |

내부 total은 포함 관계가 있어 합산 금지. 후속 배치 1987/2309는 CPU 13.1245/15.8013ms. 최초 실행 JIT와 반복 계산 비용을 구분해야 한다.

## 대기 Tick 조사

- 입력/명령/재생 실행 마커가 없는 1000~1239를 찾고 1000~1199의 200프레임을 대조했다. CPU 중앙값 8.2114ms, GameBootstrap.Update 평균 total 0.167552ms.
- 환경 호버 Poll 평균 0.001472ms/0B, 회오리 표시 Poll 0.000824ms/0B, 카메라 Poll 0.000130ms/0B. 이 Poll들이 큰 시간 병목이라는 증거는 없다.
- 200개 중 183프레임 전체 할당 496B. 나머지는 특성 설명 정적 초기화/UI 활성화/TMP 등의 비용이 있어 순수 무작업으로 단정하지 않는다.
- 프레임 6000과 후반 5800~5999는 IMGUI BeginGUI 368B/frame, GameBootstrap 아래 128B/frame. 후반 구간에는 행동 마커가 없어 전투 입력 대기와 동일하게 취급하지 않는다.
- TickModules는 IReadOnlyList로 노출되며 실제 SystemModuleStore는 Array.AsReadOnly로 만든다. 아래 네 Group의 foreach가 열거자 할당 후보다. 128B와 정합적이지만 개별 할당 스택으로 확정한 것은 아니다.
  - LifeCycleSystem/LifeCycleSystemGroup.cs
  - InGameSystem/InGameSystemGroup.cs
  - InGameSystem/CoreSystem/InGameCoreSystemGroup.cs
  - InGameSystem/ViewSystem/InGameViewSystemGroup.cs
- FrameworkRuntime/Global/Job/Render/Utility/Loading의 Tick 전달은 인덱스 순회다.
- CombatSceneInputDriver의 람다는 생성자에서 1회 캐시. CombatPresentationPlayers의 LINQ는 조립 시점. Cancel/Release의 delegate는 매 Tick 경로가 아니다.
- CombatHudActionAvailability의 캐릭터 비교는 QueryRevision 변경 시점. CombatBodyTextWindow는 재사용 목록 순회. CombatUnitMotionSet은 재사용 스냅샷 목록에 레인을 복사하므로 대기 CPU 후보이나 큰 병목 증거 없음.
- 공용 UnitCardUI.Update 아래 200프레임 중 할당 12회, 총 1,920B. Update는 변경 확인 후 Refresh하므로 무조건 매 프레임 문자열 생성이라고 하지 않는다. 공용 UI 코드는 수정하지 않았다.

구조체의 클래스 필드는 소유 객체 안에 저장되며 읽을 때마다 별도 할당되지 않는다. object/interface 변환의 박싱, 캡처 클로저, 매 Tick 델리게이트 생성을 별도로 확인한다. 모든 foreach/람다를 일괄 제거하지 않는다.

## P1 실제 코드 변경

`Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Support/CombatPlaybackProfile.cs`

- 기존 생성자: 14단계 마커 모두 생성. ByOwner는 Player/Enemy/Unknown/System 프로필 모두 생성.
- 변경: 단계별 `ProfilerMarker?` 필드 + `??=`로 최초 접근 때 생성/캐시. ByOwner도 사용한 Owner 프로필만 생성.
- 첫 이동의 네 종류 × 네 Owner × 14단계 = 224마커 일괄 생성을 제거했다. 사용하는 마커의 최초 비용은 남는다. 전체 JIT 비용이 제거된 것은 아니다.
- 마커 이름/호출 위치/재생 동작/빌드 조건(UNITY_EDITOR 또는 DEVELOPMENT_BUILD) 유지. 새 서비스·새 조립·Core 변경 없음.
- nullable 구조체 필드에 보관하며 캐시 조회에 object/interface 변환·캡처 람다 없음. 반복 조회는 문자열을 만들지 않는다. 기존 메인 스레드 사용 계약이며 멀티스레드 안전성을 새로 보장하지 않는다.
- git 상태는 **AM**: 이전 계측 파일 추가가 staged, 이번 P1은 unstaged 변경. 인덱스와 작업 트리 모두 보존할 것.
- scoped diff --check 통과. **컴파일 미확인, 개선량 측정 전.** 테스트 코드/MSBuild/커밋/push 없음.

## 다음 작업 순서

1. P1을 Unity에서 컴파일하고 같은 시작 조건의 최초/반복 배치·공격·이동을 재측정한다. 사용한 마커 이름과 Roar의 전체 단계가 유지되는지 확인한다. ms 감소를 먼저 가정하지 않는다.
2. P2: 네 Group 소유 영역 확인. Core/Architecture를 수정하려면 명시적인 범위 승인이 필요하다. 할당 스택 또는 최소 계측으로 128B를 확인한 뒤 인덱스 순회 개선과 대기 재측정을 한다. 계측 자체의 할당을 추가하지 않는다.
3. P3: Core 담당과 첫 실행 준비 방법 협의. View에서는 유닛/공격 리소스 준비 소유자와 현재 로딩 수명을 확인한 뒤 첫 재질/애니메이터 준비를 검토한다. 대규모 전역 워밍업·가짜 명령·SRP 비활성화로 우회하지 않는다.
4. Phase마다 계획/결과 갱신하고 사용자 리뷰 지점에서 멈춘다.

## 이전 익스포터·크래시 조사 맥락

- 전체 플레이는 네이티브 raw로 저장하고 Play가 완전히 끝난 뒤 CSV로 변환한다. raw 로드에도 적용되던 300프레임 제한은 ProfilerCaptureHistory가 변환 동안 native history를 확장해 처리한다. Import/ExportPending은 isPlaying과 isPlayingOrWillChangePlaymode 둘 다 검사한다.
- ProfilerFrameCsvOutput의 마지막 Directory.Move 접근 거부가 한 번 발생했다. 최대 5회/100ms 재시도, 완성 CSV 확정 실패 시 .partial 보존으로 보완했다. 잠금 주체는 미확정이다. 원본 재변환 성공과 이후 사용자 자동 출력 성공을 확인했다.
- 네이티브 크래시 최상단은 SharedMaterialData::UpdatePerMaterialCB 읽기 위반. 11월 1일/10일 덤프 동일 지점으로 기록 기능이 처음 도입한 크래시라고 단정하지 않는다.
- FieldCellFillStack의 _HoleShapes 배열이 사용 종류 수였으나 CombatCellFill.shader는 float4[256]. 사용자 승인으로 배열을 256칸으로 맞추고 셰이더 주석 수정. 덤프 버퍼 크기 4288바이트와 정합적이나 원인 객체 신원/인과는 미확정. 수정 후 약 141초 재생에서 비재현, 이후 사용자 녹화도 정상 종료. 완전한 크래시 해결 증명은 아니다.
- 원본·CSV는 크다. 기존 frame_markers.csv 약 1.92GB인 기록도 있다. 분석은 스트리밍으로 필요한 프레임만 읽는다. 원본/로그 삭제 금지, 메모리/디스크 한계는 남는다.
- 자동 기록 체크는 마지막 GUI 확인 시 켜져 있었으나 Editor 재시작 시 다시 확인해야 한다. 현재 Unity UI/Play 상태는 이 인계에서 확인하지 않았다.
