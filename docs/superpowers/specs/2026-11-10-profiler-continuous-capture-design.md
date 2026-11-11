# 전체 플레이 연속 Profiler 기록 설계 — 2026-11-10

- 작성: Codex
- 요청: 최근 버퍼 300프레임이 아닌 전체 플레이의 연속 기록과 CSV 내보내기.
- 범위: Tool/Editor의 수집·내보내기. C2에서 사용자 별도 승인으로 FieldCellFillStack/CombatCellFill.shader의 배열 길이 계약도 수정한다. 기존 행동 마커·Core·표시 규칙·씬·빌드 설정은 변경하지 않는다.
- 구현은 하나의 응집된 Phase C1(기록 시작부터 원본 재변환까지)으로 진행하고 종료 시 보고한다. 컴파일·재생·장시간 기록 인수는 사용자 Unity에서 수행한다.

## 결정

1. Unity의 `Profiler.logFile` + `enableBinaryLog`를 사용해 `.raw`로 연속 저장한다. 실시간 CSV 변환·버퍼 크기 늘리기·매 프레임 Hierarchy 순회로 대체하지 않는다.
2. `전체 플레이 자동 기록` 메뉴를 켜면 ExitingEditMode에서 기록을 시작하고 ExitingPlayMode에서 닫는다. EnteredEditMode 뒤 원본을 다시 불러와 기존 다섯 파일로 변환한다. 자동 기록은 이 Editor 세션에만 유지한다. 중간 시작/수동 중지도 제공하며 이 경우 전체 Play 기록이라고 표시하지 않는다.
3. 원본은 `Logs/ProfilerCapture/<고유 ID>/capture.raw`, 상태·시작/종료·누락 알림·내보내기 결과는 `capture.json`이다. CSV는 기존 `Logs/ProfilerExport` 아래 별도 고유 폴더다. 취소·실패 때 raw는 삭제하지 않는다. 마지막 기록과 선택한 raw/data에서 다시 변환할 수 있다.
4. 수명·중복 시작 차단은 Editor 기록 소유자 한 곳이 맡는다. 시작 전 설정을 보관하고 종료/시작 실패 때 복원한다. 정상 중지는 녹화를 끈 상태로 남긴다. 다른 binary log가 이미 켜져 있으면 탈취하지 않는다.
5. SessionState로 Play 진입의 도메인 리로드를 넘긴다. 기록 도중 스크립트 재컴파일/Editor 종료는 원본을 닫고 Interrupted로 남긴다. 변환은 재생 종료 뒤에만 한다. 변환 전 현재 Profiler 버퍼는 별도 파일로 보존한다.
6. 로드된 원본의 first~last 전체를 스캔한다. observed Unity game-frame span과 출력 수, Scanner skipped 및 Unity의 `Skipping profile frame` 알림을 기록한다. 부족하면 전체 기록 성공으로 표시하지 않는다. 이 비교는 초기화 이전/네이티브 내부 누락까지 증명하지는 않는다. 장시간 raw가 300프레임을 넘겨 변환되는지는 Unity 인수 필수다.
7. CPU 영역을 기록 중 켜고 이전 설정을 복원한다. GPU·Deep Profile은 임의로 켜지 않는다. 마커 0건이면 요약·완료 로그에 눈에 띄게 표시한다. GPU 빈 값은 미관측이다.
8. CSV 변환은 기존 Scanner/집계/출력기를 재사용한다. 요약에 버퍼/연속 원본 출처와 범위를 표시한다. raw 로드 시 녹화를 끄며 이미 녹화 중인 세션을 변환으로 덮지 않는다.

## 구조 검토

### C2 변환 결함 수정 — 2026-11-10

직접 검증 결과 11,064 게임 프레임의 raw도 기존 LoadProfile 호출로는 300프레임만 남았다. 기록 방식은 네이티브 raw로 유지하고, **Play가 완전히 끝난 변환 기간에만** `ProfilerCaptureHistory`가 네이티브 히스토리 길이를 원본 인덱스 범위에 맞춰 확장한다. 파일을 불러온 뒤 마지막 인덱스 + 1이 용량보다 크면 그 상한으로 다시 로드한다. Dispose에서 사용자 frameCount로 복원하며 EditorPrefs/플레이 녹화 버퍼 설정은 변경하지 않는다. Unity 내부 API 접근은 이 어댑터에만 격리하고 해당 메서드/설정 getter가 없으면 원본을 보존하고 실패한다. 공개 LoadProfile만으로 전체 범위를 읽을 수 있다는 초기 가정은 폐기한다.

`isPlayingOrWillChangePlaymode`만 검사하면 Play 종료 도중 false가 되어 isPlaying=true인 상태에서 변환이 실행됐다(첫 출력 summary에서 확인). Import와 ExportPending은 두 플래그가 모두 false일 때만 진행한다. 종료 전 원본 로드/히스토리 조작을 차단한다.

C2 네이티브 크래시 후보 수정은 사용자 별도 승인 범위다. `FieldCellFillStack`은 셰이더의 `_HoleShapes[256]`과 같은 256개 배열을 최초 할당하고 미사용 항목을 0으로 유지한다. 기존 모양 계산·업로드 시점·재질 수명은 유지하며 셰이더는 설명 주석만 바꾼다. Unity 컴파일·재생 직접 검증은 이번 조사에 한해 사용자 승인으로 수행한다. 간헐 크래시의 원인 확정과 수정 후 비재현 결과는 구분한다.

| 기준 | 적용 |
|---|---|
| SSOT | raw가 원본, CSV는 파생. 기록 수명은 ContinuousCapture, 스캔/합계는 기존 Scan, 직렬화는 기존 출력기 |
| SRP | 기록 제어와 파일 변환을 분리. 익스포터에 Play/도메인 리로드 상태를 넣지 않음 |
| OCP | 기존 버퍼 내보내기 유지, raw를 로드한 뒤 공통 변환 경로 사용 |
| LSP | 취소는 변환 취소이며 원본 파괴가 아님. 실패를 전체 기록 성공으로 표시하지 않음 |
| ISP/DIP | 게임 서비스에 진단 계약을 추가하지 않음. Editor API와 소규모 구체 협력 객체만 사용 |
| FW/JMH | 서비스 등록·조립·UI 바인딩·렌더 소유권 변경 없음. Editor 콜백을 정적 소유자가 한 번 연결하고 기록 설정을 복원 |
| 기능 독립성 | 수집 정책 변경은 기록 소유자, CSV 열 변경은 출력/Exporter. 게임 입력·연출을 재검증하기 위한 코드 변경 없음 |

디스크/Unity Profiler 메모리 한계는 남는다. 무제한·무손실을 보장하지 않는다. Play 진입 전 설정부터 Play 종료 직전까지의 원본 기록이며, 녹화 활성화 전 프레임을 소급 생성하지 않는다.

## C2 후속 — 긴 기록 변환 크래시 (2026-11-10, 설계 초안, 결정 대기)

### 원인 범위

- 8분 기록(47,466프레임, 원본 7.4GB)을 자동 변환하다 첫 `LoadProfile`(`ProfilerCaptureHistory.Load` 첫 호출) 안에서 에디터가 죽었다. 22:48 Play 종료 → 23:01:56 Windows "가상 메모리 부족" 알림(System 26) → 23:02:14 Unity.exe 0xC0000005(Unity.dll+0x7c11ab). 이번 크래시의 Unity 크래시 보고 폴더는 생기지 않았다.
- **원인은 메모리다.** C2의 전체 범위 변환은 원본의 모든 프레임을 Unity Profiler 메모리에 한 번에 올린다(히스토리 길이를 전체 프레임 수로 늘림). 11,065프레임(원본 1.72GB)을 변환한 뒤 에디터 메모리가 1.30GB → 10.44GB가 됐다. 프레임당 약 0.8MB, 원본 크기의 약 5배다(측정 한 번에서 나온 어림값).
- 47,466프레임이면 약 38GB가 필요하다. 이 PC의 가상 메모리 한도는 43.7GB(물리 15.7GB + 자동 관리 페이지 파일)이고, 변환 직전 에디터가 이미 약 6GB였다. 할당이 실패한 뒤 잘못된 주소를 읽어 죽은 것으로 본다.
- 원본 형식이나 프레임 수 자체의 한계라는 증거는 없다. 다만 4GB가 넘는 원본을 불러온 적은 아직 없다.
- 같이 찾은 것: 변환이 끝나도 불러온 프레임이 Profiler 버퍼에 남는다. 그래서 다음 Play·변환까지 메모리를 계속 잡고(11,065프레임 변환 뒤 다음 Play 중 10.4GB), 다음 변환 전 백업 `buffer-before-load-*.data`가 그 프레임을 다시 저장한다(지금 6개, 약 5.5GB).
- 증거 사본: `Logs/ProfilerDiagnostics/long-capture-load-crash/`(Editor-prev.log 사본, Windows 이벤트, 측정 때 메모리).

### 막는 안

| 안 | 내용 | 좋은 점 | 한계 |
|---|---|---|---|
| A 변환 전 메모리 검사 | 원본 크기로 필요한 메모리를 어림해 Windows의 남은 가상 메모리와 비교한다. 넘으면 자동 변환을 하지 않고 원본을 남긴다. `capture.json`에 `exportStatus = SkippedTooLarge`, 추정치·남은 메모리를 적고, 콘솔에 할 일을 안내한다. 수동 메뉴(다시 내보내기·raw 선택)도 같은 검사를 한다. | 작다(Import 한 곳 + 메모리 읽기 함수 하나). 크래시를 확실히 막는다. | 긴 플레이는 CSV가 없다. |
| B 기록을 구간 파일로 나눠 저장 | 기록 중 일정 프레임(예: 5,000프레임 ≈ 메모리 약 4GB)마다 `Profiler.logFile`을 다음 파일(`capture-000.raw`…)로 바꾼다. 변환은 구간을 하나씩 불러 같은 CSV에 이어 쓴다. A의 검사도 구간마다 한다. | 긴 플레이도 전체 CSV가 나온다. 메모리 상한이 구간 하나다. | Unity 문서는 "기록 중 logFile을 바꾸면 그 파일에 이어 저장한다"고만 한다. 새 파일이 마커 이름 정보를 다시 쓰는지, 경계 프레임이 빠지는지는 Unity에서 먼저 확인해야 한다(B-0). 느린 프레임 5개 계층은 지금 끝에서 다시 여는데 앞 구간은 이미 내려가 있으므로 구간마다 미리 적어야 한다. `capture.json`이 원본 여러 개를 가리키게 바뀐다. |
| C 넘으면 끝부분만 변환 | 히스토리 길이를 한도까지만 늘려 마지막 N프레임만 CSV로 만들고 `Partial`로 표시한다. | 작다. | 앞부분(배치 등)이 빠진다. Unity가 파일을 읽으면서 앞 프레임을 버리는지(메모리가 한도 안에 머무는지) 확인 안 됨. |

안과 별개인 보완 D: 변환이 끝나면 Profiler 버퍼를 비운다(`ProfilerDriver.ClearAllFrames`). 불러온 원본은 디스크에 있고, 그 전 버퍼는 이미 백업했으므로 잃는 것이 없다. 메모리가 바로 풀리고 다음 백업이 작아진다. 대신 변환 직후 Profiler 창에서 그 기록을 바로 볼 수 없다(원본을 다시 불러오면 된다).

권장: 지금 A + D, 긴 플레이 전체 CSV가 필요해지면 B를 다음 묶음으로(B-0 확인부터). 이유: A는 작고 크래시를 확실히 막는다. B는 Unity 동작 확인이 먼저 필요하다.

A를 고르면 정할 값(권장):
- 추정식 = 원본 크기 × 5.5(측정값 약 5.3배에 여유).
- 여유분 4GB를 남긴다. 남은 가상 메모리는 Windows `GlobalMemoryStatusEx`의 남은 commit으로 읽는다. 읽지 못하면 고정 한도 1만 프레임을 쓴다.
- 자동 변환은 넘으면 건너뛴다. 수동 메뉴는 넘으면 확인 창을 띄우고 기본 버튼은 취소다(다른 프로그램을 닫고 다시 시도하는 경우를 위해 "그래도 변환"을 남긴다).
- 필요량이 남은 물리 메모리를 넘으면 가상 메모리 한도 안이어도 페이지 파일을 써서 매우 느려진다. 이 경우는 막지 않고 경고만 한다.

### 결정과 구현 메모 (2026-11-11)

- 결정: 사용자 "A + B + D로". C는 하지 않는다. 진행 기록·Unity 확인 목록은 [계획](../plans/2026-11-10-profiler-continuous-capture-plan.md) "C2 후속" 절의 LC-a~LC-d.
- 책임 자리: 기록 수명·구간 전환은 `ProfilerContinuousCapture`, 메모리 어림은 새 `ProfilerCaptureMemory`, 불러올 파일 목록·구간 반복·버퍼 비우기는 `ProfilerCaptureImport`, 범위를 이어 읽고 한 출력으로 확정하는 일은 `ProfilerFrameExporter.Session`, 히스토리 길이는 `ProfilerCaptureHistory`. 원본은 구간 파일, CSV는 파생이라는 SSOT는 그대로다.
- 구간 번호: Unity가 새 구간 파일의 번호를 앞 파일에 잇는지, 0부터 다시 세는지는 문서에 없다(Ub 대기). 그래서 변환이 두 경우를 모두 받는다 — 이어지면 그대로 쓰고 빈 번호를 세고, 0부터거나 겹치면 앞 구간 끝 다음으로 옮긴다. 실제 동작은 `capture.json` 구간마다 `loadedFirstFrame`·`outputOffset`으로 확인한다.
- 느린 프레임 계층: 앞 구간은 다음 구간을 불러오면 사라져 요약 끝에서 다시 열 수 없다. 범위를 다 읽을 때마다 그때까지 상위(PlayerLoop·CPU 기준 각 5개)의 계층 글을 미리 만든다.
- 메모리 검사는 먼저 가장 큰 구간으로 한 번 한다(모자라면 아무것도 불러오지 않음). 묶음 끝 검토 뒤, 구간을 내려놓아도 메모리가 바로 돌아오지 않을 수 있어 구간마다 불러오기 직전에 다시 보고, 모자라면 읽은 데까지만 `Incomplete`로 출력한다. 크래시보다 일부 출력이 낫다고 봤다.
- 구간 번호 겹침: 번호가 이어지는데 경계 프레임이 두 파일에 다 있으면 한 번만 읽는다(앞 범위의 불러온 번호 안에서 시작하면 겹침, 앞 범위 시작 이하면 다시 시작으로 판정).
- 남는 것: 구간 경계에서 프레임이 빠지는지·새 구간 파일에 마커 이름이 다시 쓰이는지는 Unity 확인 대상이다(마커 이름이 빠지면 둘째 구간부터 이름이 비거나 이상하게 보일 것). 출력 CSV 크기는 길이에 비례한다(8분이면 `frame_markers.csv` 약 10GB). 11-10 이전의 큰 단일 원본은 이 PC에서 변환할 수 없다.

근거: [Unity 6 binary log API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Profiling.Profiler-enableBinaryLog.html), [Unity 6000.0 ProfilerDriver](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/ProfilerEditor/Public/ProfilerAPI.bindings.cs), [ProfilerWindow의 LoadProfile 호출](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/ProfilerEditor/ProfilerWindow/ProfilerWindow.cs).
