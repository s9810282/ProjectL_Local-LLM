# 전체 플레이 연속 기록 Task — 2026-11-10

- 작성: Codex
- 설계: [연속 기록 설계](../specs/2026-11-10-profiler-continuous-capture-design.md)
- 상태: C2 수정·직접 검증 완료. Editor 컴파일, OFF/ON 재생, 11,065프레임 및 수정 후 8,605프레임 전체 변환 확인. 배열 계약 수정 후 약 141초 크래시 비재현. 간헐 크래시 인과 확정·장시간 안정성·포효 재현은 미확인. 2026-11-10 8분(47,466프레임) 기록의 자동 변환이 메모리 부족으로 크래시 → 2026-11-11 A + B + D(메모리 검사·구간 파일로 나눠 저장·변환 뒤 버퍼 비우기) LC-a~LC-d 코드·문서 완료, LC-a Unity 확인 문제 없음, LC-b·LC-c는 컴파일 미확인·Unity 점검 대기(아래 "C2 후속" 점검 목록).

## 2026-11-10 크래시 대응

### C2 후속 — 긴 기록 변환 크래시 (2026-11-10, Claude, 코드 변경 없음, 결정 대기)

- 사실: `20261110-224015-c32cd37340e143299720bda1aa4f67ea`(8분, 47,466프레임, 원본 7.4GB)를 Play 종료 뒤 자동 변환하던 중 `ProfilerCaptureHistory.Load`의 첫 `LoadProfile` 안에서 크래시. 23:01:56 Windows "가상 메모리 부족" 알림 → 23:02:14 Unity.exe 0xC0000005. `capture.json`은 `Stopped`, `exportStatus` 빈 값으로 남았다. 원본은 그대로 있다.
- 원인: 메모리 부족. 전체 범위 변환은 모든 프레임을 한 번에 올리고, 11,065프레임 변환 때 에디터가 1.30GB → 10.44GB(프레임당 약 0.8MB, 원본의 약 5배)였다. 47,466프레임은 약 38GB가 필요하고, 가상 메모리 한도는 43.7GB, 변환 전 에디터는 약 6GB였다. 원본 형식·프레임 수 자체의 한계라는 증거는 없다(4GB 넘는 원본 로드는 미확인).
- 같이 찾음: 변환 뒤에도 불러온 프레임이 Profiler 버퍼에 남아 메모리를 잡고, 다음 변환 전 백업이 그 프레임을 다시 저장한다(`buffer-before-load-*.data` 6개, 약 5.5GB, 지우지 않음).
- 증거 사본: `Logs/ProfilerDiagnostics/long-capture-load-crash/`. 원본·로그·백업은 지우지 않았다.
- 설계: [연속 기록 설계](../specs/2026-11-10-profiler-continuous-capture-design.md) "C2 후속" 절. 안 A(변환 전 메모리 검사, 넘으면 원본만 남기고 안내)·B(기록을 구간 파일로 나눠 저장, B-0 Unity 확인 먼저)·C(넘으면 끝부분만 변환)와 보완 D(변환 뒤 버퍼 비우기). 권장 A + D.
- 주의(대응 전까지): 7.4GB 원본을 `저장된 raw 또는 data 전체 내보내기...`로 고르면 같은 크래시가 난다. 기록은 한 번에 약 1만 프레임(1분 30초~2분) 이하로 한다. → LC-a~LC-c로 대신함(2026-11-11): 큰 원본은 확인 창에서 막히고, 새 기록은 구간으로 나뉜다.
- **결정(2026-11-11 사용자 "A + B + D로"):** A 변환 전 메모리 검사, B 기록을 구간 파일로 나눠 저장, D 변환 뒤 버퍼 비우기. C는 하지 않는다. A의 값은 설계 "A를 고르면 정할 값(권장)"대로.

#### Task 계획 LC-a~LC-d

Phase마다 멈추고 보고한다. Test 코드·MSBuild 없음, 컴파일·재생은 사용자 Unity 확인. 고치는 파일은 `Tool/Editor`의 기록 도구와 이 문서들뿐이다.

- **LC-a 메모리 검사 + 버퍼 비우기 (A + D)**
  - [x] a1 새 `ProfilerCaptureMemory`: 필요량 어림(원본 크기 × 5.5)·남은 가상 메모리(Windows `GlobalMemoryStatusEx`)·여유 4GB·판정. 읽지 못하면 고정 한도 1만 프레임.
  - [x] a2 `ProfilerCaptureImport.Export`: 버퍼 백업 전에 검사. 자동 변환은 넘으면 건너뜀(`SkippedTooLarge`, 콘솔 안내). 수동 메뉴는 확인 창.
  - [x] a3 `ProfilerCaptureRecord`에 필요량·남은 메모리 칸. `ProfilerContinuousCapture`가 자동/수동을 넘김.
  - [x] a4 원본을 불러왔으면 변환이 끝날 때(성공·취소·실패 모두) `ProfilerDriver.ClearAllFrames`.
  - Unity 확인 Ua: ① 컴파일 ② 1분 안쪽 기록의 자동 변환이 지금처럼 끝나고, 콘솔에 버퍼를 비웠다는 줄이 나오며 Profiler 창이 빈다 ③ `저장된 raw 또는 data 전체 내보내기...`로 7.4GB 원본을 고르면 확인 창에 필요·남은 메모리가 나오고, "변환하지 않음"·Esc면 불러오지 않고 콘솔에 "불러오지 않았습니다" 경고가 나온다(**"그래도 변환"은 누르지 않는다**). 이 메뉴는 원본 옆 `capture.json`을 읽기만 하므로 그 파일은 바뀌지 않는다 ④ `마지막 연속 기록 다시 내보내기`도 짧은 기록은 확인 창 없이 변환된다.
  - **LC-a 실행 기록(2026-11-11, Claude, 컴파일 미확인, Test 코드·MSBuild 없음, Ua 대기):**
    - 새 `Tool/Editor/ProfilerCaptureMemory.cs`(+ `.meta`, Unity 기본 형식): 어림 상수(× 5.5, 여유 4GB, 고정 한도 1만 프레임, 프레임당 160KB)와 판정 `Check(파일 크기, 관측 프레임)`. Windows 에디터(`UNITY_EDITOR_WIN`)에서만 `GlobalMemoryStatusEx`의 남은 commit(`ullAvailPageFile`)·남은 물리 메모리를 읽는다.
    - `ProfilerCaptureImport.Export(path, record, interactive)`: `interactive` 인자를 필수로 더함. 메뉴 둘(raw 선택·다시 내보내기)은 true, 자동 변환은 false. 기록 읽기(`ReadRecord`)를 try 앞으로 옮겨 검사에 씀(동작 같음). 넘치면 묻거나(수동) 건너뛰고(자동) 경고 한 줄에 어림값·원본 경로·할 일을 적는다. 물리 메모리를 넘으면 느려질 수 있다는 경고만 낸다.
    - 확인 창은 `DisplayDialogComplex("변환하지 않음", "취소", "그래도 변환")` — Enter·Esc 모두 변환하지 않는다.
    - `ProfilerCaptureRecord`: `loadNeedBytes`·`loadAvailableBytes`(-1 = 모름). 옛 `capture.json`은 기본값으로 읽힌다.
    - D: 원본을 불러오기 시작했으면 `finally`에서 `ProfilerDriver.ClearAllFrames`(실패해도 원래 예외를 가리지 않게 경고만). 불러오기 전에 멈추면 사용자의 버퍼는 그대로다.
    - 계획과 다른 점: Ua ③의 "`capture.json`이 `SkippedTooLarge`가 된다"는 틀렸다. raw 선택 메뉴는 C1 규칙대로 원본 옆 메타데이터를 읽기만 한다. 자동 변환과 `마지막 연속 기록 다시 내보내기`만 그 파일에 남긴다. 물리 메모리 경고 기준은 여유분 없이 "필요량 > 남은 물리 메모리"로 했다(여유분을 빼면 거의 모든 변환에서 경고가 나와서).
    - 정적 점검: 새·바뀐 파일 BOM 없음·LF·U+FFFD 없음·괄호 짝, scoped `git diff --check` 통과. 바꾼 `ProfilerContinuousCapture` 줄은 `Export` 호출 둘뿐이다.
    - 지금 이 PC 값으로 어림하면 7.4GB 원본은 필요 약 38GB > 남은 가상 메모리 약 17GB라 건너뛰고, 1분짜리(약 1GB)는 필요 약 5.3GB로 변환한다.
- **LC-b 기록을 구간 파일로 (B 기록 쪽)**
  - [x] b1 Play 중 5,000 게임 프레임마다 `Profiler.logFile`만 다음 파일(`capture-000.raw`, `capture-001.raw`…)로 바꾼다. 기록 소유 확인은 지금 구간 경로로.
  - [x] b2 `capture.json`에 구간 목록(경로·시작 게임 프레임). 중단·리로드·종료 때 구간 목록 보존.
  - [x] b3 구간으로 나뉜 기록은 LC-c 전까지 자동 변환하지 않고 안내만 한다.
  - Unity 확인 Ub(B-0): 2분 이상 기록 → 구간 파일이 여럿 생김 → Profiler 창에서 둘째 구간 파일을 불러 마커 이름(`Combat.Action.*` 등)이 보이는지, 첫 구간 끝 번호와 둘째 구간 첫 번호가 이어지는지(0부터 다시인지) 확인. 결과로 LC-c의 번호 규칙을 확정한다.
  - **LC-b 실행 기록(2026-11-11, Claude, Ua 문제 없음 — 사용자, 컴파일 미확인, Test 코드·MSBuild 없음, Ub 대기):**
    - `ProfilerCaptureRecord`: 구간 칸 `segmentFrames`·`segments`(새 직렬화 형식 `ProfilerCaptureSegment` = 경로·바꾼 순간의 게임 프레임, 같은 파일), 읽기 `ActivePath`(지금·마지막 구간)·`IsSegmented`(구간 둘 이상). 옛 `capture.json`은 빈 목록으로 읽혀 `rawPath` 하나를 구간 하나로 본다(P7).
    - `ProfilerContinuousCapture`: 새 기록의 첫 파일 이름이 `capture.raw` → `capture-000.raw`. Editor update에서 지금 구간이 5,000 게임 프레임에 닿으면 `Profiler.logFile`만 다음 경로로 바꾸고 구간을 더한 뒤 SessionState·`capture.json`에 바로 남긴다(콘솔에 구간 시작 한 줄). 경로 바꾸기가 실패하면 `Interrupted`로 멈춘다. 기록 소유 확인(1초 검사·`Stop`·`CloseInterruptedLog`)은 모두 `ActivePath`로 비교한다. 구간 목록이 빈 기록(이 코드 전에 시작해 리로드를 넘어온 것)은 나누지 않는다.
    - `ProfilerCaptureImport`: 구간이 둘 이상인 기록은 메모리 검사 전에 `SkippedSegmented`로 멈추고 "Profiler 창 Load로 하나씩 열 수 있다"고 안내한다(자동·수동 모두). raw 선택에서 구간 파일 어느 것을 골라도 원본 옆 `capture.json`을 그 기록으로 읽는다(c5 앞당김 — LC-c가 같은 길로 전체를 변환한다).
    - 5,000 게임 프레임 미만인 짧은 기록은 구간이 하나라 지금처럼 자동 변환된다.
    - 계획과 다른 점: 없음. c5의 "구간 파일을 고르면 그 기록으로 읽기"만 LC-b로 앞당겼다(LC-b의 안내 갈래가 쓰기 때문).
    - 정적 점검: 바뀐 세 파일 BOM 없음·LF·괄호 짝, scoped `git diff --check` 통과.
    - Ub 순서: ① 컴파일 ② 자동 기록을 켜고 2분 이상 Play → 콘솔에 "연속 기록 구간 1 시작"이 나오고 폴더에 `capture-000.raw`·`capture-001.raw`…가 생기며 `capture.json`의 `segments`에 모두 적힘 ③ Play 종료 뒤 콘솔에 "구간 파일 N개로 나뉜 기록이라 아직 CSV로 바꾸지 않았습니다" 경고, 크래시 없음 ④ Profiler 창 Load로 `capture-000.raw`와 `capture-001.raw`를 차례로 열어, 각각 마지막 프레임 번호(창 위 "Frame: N")와 마커 이름(`Combat.*`, `PlayerLoop` 아래 이름)이 보이는지 적기. 창은 최근 몇백 프레임만 남겨 첫 번호는 볼 수 없으므로 마지막 번호로 판단한다 — 001의 마지막 번호가 000의 마지막 번호 + 약 5,000이면 번호가 이어지고, 약 5,000 이하면 구간마다 0부터 다시 센다 ⑤ 1분 안쪽 기록은 지금처럼 자동 변환됨.
- **LC-c 구간 변환 (B 변환 쪽)**
  - [x] c1 구간을 차례로 불러 같은 CSV에 이어 쓴다. 구간마다 A 검사, 구간 사이에 버퍼 비움. 진행 창에 "구간 k/n".
  - [x] c2 프레임 번호 규칙(Ub 결과로 확정)·구간 사이 빈 번호를 `capture.json`에 기록.
  - [x] c3 느린 프레임 5개 계층을 구간마다 미리 적어 둔다.
  - [x] c4 히스토리 길이 판정을 "불러온 개수가 용량과 같으면 두 배로 다시 불러옴"으로 바꾼다.
  - [x] c5 수동 raw 선택에서 구간 파일을 고르면 그 기록의 구간 전체를 변환한다(LC-b에서 함).
  - Unity 확인 Uc: 2분 이상 기록의 자동 변환 결과가 한 출력 폴더에 모든 구간 프레임을 담는다, 배치·턴 종료 마커가 앞·뒤 구간에 모두 있다, 옛 `capture.raw` 하나짜리 원본도 다시 변환된다.
  - **LC-c 실행 기록(2026-11-11, Claude, 사용자 "일단 마지막까지 진행하고 추후 점검할게" = Ub를 기다리지 않고 LC-d까지 진행, 컴파일 미확인, Test 코드·MSBuild 없음, Ub·Uc 대기):**
    - c2 번호 규칙은 Ub 결과 없이 두 경우를 모두 처리하게 정했다: 불러온 첫 번호가 앞 구간의 마지막 출력 번호보다 크면 그대로 쓰고 사이 빈 번호를 센다(`segmentGapFrames`). 같거나 작으면(구간마다 0부터 다시 세거나 겹치면) 앞 구간 끝 다음 번호로 옮긴다(`renumberedSegments`, 구간마다 `outputOffset`). 출력 번호는 늘 커지기만 해서 `actions.csv`·요약의 번호 사전이 겹치지 않는다. Ub에서 어느 쪽이었는지는 `capture.json`의 구간마다 `loadedFirstFrame`·`outputOffset`으로 나중에 확인한다.
    - `ProfilerFrameScan.Read(frame, outputFrame)`: 여는 번호와 출력 번호를 나눴다(합계·CSV·행은 출력 번호).
    - `ProfilerFrameExporter`: 새 `Session`(출력 폴더·스캔·계층 글 보관)이 `ReadLoaded`(지금 버퍼 전체 읽기, 진행 창 "구간 k/n")·`NoteMissing`·`Commit`을 맡는다. 메뉴 "기록 내보내기"도 범위 하나짜리 세션으로 같은 길을 쓴다(화면 결과 같음). 요약에 구간 안내 줄을 더하고, 빈 번호나 읽지 못한 구간이 있으면 `Incomplete`.
    - c3 느린 프레임 계층: 범위를 다 읽을 때마다 지금까지 PlayerLoop 기준·CPU 기준 상위 5개 중 이 범위에 있는 프레임의 계층 글을 만들어 둔다. 요약은 끝에서 고른 상위 5개의 글을 꺼내 쓴다(없으면 지금처럼 "버퍼에서 사라졌습니다"). 최종 상위 프레임은 자기 범위를 다 읽은 순간에도 상위였으므로 빠지지 않는다.
    - c4 `ProfilerCaptureHistory.Load(path, expectedFrames)`: 상한 = max(사용자 길이, 기대 수 × 2 + 64). 불러온 개수가 상한에 닿으면 max(개수 × 2, 마지막 번호 + 2)로 늘려 다시 불러오고, 3번째에도 닿으면 경고 뒤 잘린 채 진행한다(관측 수보다 적어 `Incomplete`). 기대 수는 구간을 바꾼 게임 프레임 간격, 옛 기록은 지금처럼 마지막 관측 번호 + 1, 모르면 파일 크기 ÷ 160KB.
    - `ProfilerCaptureImport`: 불러올 파일 목록(구간 기록은 구간마다, 그 밖은 고른 파일 하나)을 먼저 만들고, 가장 큰 파일로 A 검사를 한 번 한 뒤 차례로 불러 읽는다. 두 번째 구간부터는 불러오기 전에 `ClearAllFrames`로 앞 구간을 내려놓는다. 빈·없는 파일은 `Missing`, 불러오기 실패는(구간이 둘 이상일 때만) `Failed`로 남기고 나머지를 계속 변환한다. 구간이 하나면 실패는 지금처럼 예외다. LC-b의 "구간 기록은 변환하지 않음" 갈래는 걷었다.
    - `ProfilerCaptureRecord`: 전체 `segmentGapFrames`·`renumberedSegments`·`missingSegments`, 구간마다 `loadedFirstFrame`·`loadedLastFrame`·`outputOffset`·`loadStatus`.
    - 계획과 다른 점: c1의 "구간마다 A 검사"는 "가장 큰 구간으로 한 번"으로 했다(구간 하나씩만 올리므로 가장 큰 것이 정하고, 중간에 건너뛰어 일부만 나오는 출력을 피함).
    - 정적 점검: 바뀐 파일 BOM 없음·LF·괄호 짝, 옛 호출(`Read(int)`·`Load(path, record)`) 남은 곳 없음.
- **LC-d 문서·검토**
  - [x] d1 가이드 주의 단락을 새 동작으로 바꿈(가이드 "긴 기록과 메모리" 절), 설계 "결정과 구현 메모", 이 절 실행 기록.
  - [x] d2 묶음 끝 검토 한 번(새 에이전트 하나).
  - **LC-d 실행 기록(2026-11-11, Claude, 컴파일 미확인):**
    - 검토 전 직접 찾아 고침: 구간 전환 검사가 매 Editor 프레임 경로 비교(문자열 할당)를 해서 기록 중 GC 측정에 섞일 수 있었다 → 할당 없는 프레임 수 비교를 먼저 하고 경로 비교는 바꿀 때만.
    - 묶음 끝 검토(새 에이전트 하나, 읽기 전용): 높음 0, 컴파일 오류 못 찾음. 출력 번호 중복 없음, 느린 프레임 계층이 최종 상위 5개에 늘 잡힘, 소유 검사·리로드 복원 일관됨을 확인했다.
    - 반영한 것:
      - 중간 1: 구간을 내려놓아도 메모리가 바로 돌아오지 않을 수 있다 → 두 번째 구간부터(구간이 둘 이상이면 모두) 불러오기 직전에 메모리를 다시 보고, 모자라면 그 구간부터 `SkippedMemory`로 남기고 읽은 데까지만 출력한다(`Incomplete`). '그래도 변환'을 골랐으면 다시 보지 않는다. 히스토리 다시 불러오기 전에도 버퍼를 비운다.
      - 중간 2: 번호가 이어지는데 경계 프레임이 두 파일에 다 있으면 구간 전체가 1 밀리고 그 프레임을 두 번 셌다 → 앞 범위의 불러온 번호와 비교해 세 경우로 나눔: 이어짐(빈 번호 셈) / 겹침(겹친 프레임은 한 번만 읽고 `segmentOverlapFrames`에 셈) / 다시 시작(앞 끝 다음으로 옮김).
      - 낮음 3: 다시 내보낼 때와 `SkippedTooLarge`로 건너뛸 때 앞 변환의 결과 칸(구간별 칸 포함)을 비운다(`Session.ResetResult`).
      - 낮음 4: 구간 `loadStatus`를 실제로 읽은 뒤 `Loaded`로 적는다(프레임 없음 `Empty`, 취소 `Cancelled`).
      - 낮음 5: 메모리 고정 한도 판정에 쓰는 프레임 수는 관측 수와 파일 크기 어림 중 큰 쪽으로(중간 시작·크래시로 짧게 남은 관측 대비). 히스토리 상한도 같은 값 이상.
      - 낮음 6: 구간 경로 바꾸기가 새 경로로 바뀐 뒤 실패하면 그 파일을 구간으로 더한 뒤 멈춰, binary log가 켜진 채 남지 않게 한다.
      - 낮음 7: raw 선택 때 원본 옆 `capture.json`의 경로를 파일 이름으로 맞추고 지금 폴더로 바꿔 읽는다(기록 폴더를 옮겨도 그 기록 전체를 변환).
    - 그대로 둔 것: 낮음 8(자동 변환에서 모든 구간이 비면 확인 창이 뜸 — 이전부터 있던 동작). 검토가 짚은 남는 메모리 위험 둘: 옛 큰 단일 원본에서 '그래도 변환'을 고르는 경우(설계상 허용), 구간 사이 메모리가 돌아오지 않는 경우(위 중간 1로 줄였지만 판정은 어림).
    - 문서: 가이드 "긴 기록과 메모리" 절, 설계 "결정과 구현 메모", 이 절. 공유 노트 폴더에 요약 한 장(부모 세션용).
    - 남은 것: 사용자 Unity 확인 Ub·Uc(아래 목록), 커밋(사용자). 바뀐 파일은 `Tool/Editor`의 `ProfilerCaptureMemory.cs`(새, `.meta` 포함)·`ProfilerCaptureImport.cs`·`ProfilerCaptureRecord.cs`·`ProfilerContinuousCapture.cs`·`ProfilerFrameExporter.cs`·`ProfilerFrameScan.cs`·`ProfilerCaptureHistory.cs`와 문서 셋.
  - **점검 목록(사용자 "추후 점검", Ub·Uc 합침):**
    1. 컴파일.
    2. 1분 안쪽 기록: 지금처럼 자동 변환되고, 콘솔에 "Profiler 버퍼를 비웠습니다"가 나온다.
    3. 2분 넘는 기록: 콘솔에 "연속 기록 구간 1 시작"…, 폴더에 `capture-000.raw`·`capture-001.raw`…, Play 종료 뒤 진행 창에 "구간 k/n", 출력 폴더 하나에 모든 프레임.
    4. 그 출력의 `capture.json`: `exportStatus`가 `Converted`인지, 구간마다 `loadStatus = Loaded`·`loadedFirstFrame`·`outputOffset`, 전체 `segmentGapFrames`·`segmentOverlapFrames`·`renumberedSegments`. 둘째 구간 `loadedFirstFrame`이 0이면 Unity가 파일마다 번호를 다시 세는 것이고, 첫 구간 끝 근처면 이어지는 것이다.
    5. `actions.csv`에 배치(앞 구간)와 턴 종료(뒤 구간) 마커가 모두 있고, 둘째 구간 이후 마커 이름이 비거나 깨지지 않았는지.
    6. Profiler 창 Load로 `capture-001.raw`만 열어도 마커 이름이 보이는지(새 파일에 이름 정보가 다시 쓰이는지).
    7. `저장된 raw 또는 data 전체 내보내기...`로 7.4GB 원본 → 확인 창, "변환하지 않음"·Esc면 불러오지 않음(**"그래도 변환"은 누르지 않는다**).
    8. 옛 `capture.raw` 하나짜리 원본(예: `20261110-212332-…`, 1.1GB)을 같은 메뉴로 다시 변환해도 지금처럼 나온다.

#### 계획에서 정한 것 (사용자 확인 대상)

- P1 남은 가상 메모리를 읽지 못하면(Windows 밖 등) 관측 게임 프레임 수(없으면 파일 크기 ÷ 160KB)로 1만 프레임 고정 한도를 쓴다.
- P2 수동 메뉴 확인 창 버튼은 "변환하지 않음"(Enter)·"취소"(Esc)·"그래도 변환" 셋이다. 기본 버튼이 변환하지 않게 하려고 셋으로 둔다.
- P3 검사는 버퍼 백업보다 앞에 둔다. 건너뛰면 백업 파일도 만들지 않는다.
- P4 D는 원본을 불러온 경우에만 비운다. 불러오기 전에 멈추면 사용자의 Profiler 버퍼는 그대로다.
- P5 구간 크기는 5,000 게임 프레임(원본 약 0.8GB, 메모리 약 4GB) 상수다.
- P6 구간을 바꿀 때 기록을 끄지 않고 경로만 바꾼다(Unity 문서: binary log가 켜진 채 새 경로를 주면 그 파일에 저장한다).
- P7 `rawPath`는 첫 구간을 가리킨다(폴더 위치·옛 코드 호환). 옛 `capture.raw` 하나짜리 기록은 구간 하나로 읽는다.

### C2 직접 검증 승인 및 실행 계획

사용자가 이번 조사에 한해 Unity 컴파일·재생 직접 수행, 기록 OFF/ON 대조와 렌더링 원인 분리를 명시적으로 허용했다. Test 코드는 작성하지 않는다. 영구 렌더링 설정 변경은 결과를 제시하기 전 수행하지 않는다.

1. 원본 크래시 덤프/로그 두 건을 보존하고 최상단 네이티브 프레임·예외 주소를 대조한다.
2. 같은 프로젝트/진입 설정에서 연속 기록 OFF 진입을 확인한 뒤 ON으로 제한된 캡처를 수행한다.
3. 드러난 기록/변환 결함은 기존 Editor 소유자 안에서 수정하고 300프레임 초과 원본→CSV 및 초반/후반 행동 기록을 확인한다.
4. 렌더링 크래시가 재현되면 임시 진단으로 원인 머티리얼/셰이더를 분리한다. 인과 확인 없이 에셋·렌더 설정을 영구 변경하지 않는다. 근본 원인과 검증 한계를 구분해 보고한다.

진행 증거: 11월 10일과 11월 1일 덤프 모두 `SharedMaterialData::UpdatePerMaterialCB`의 같은 명령에서 0xC0000005 읽기 위반이다. 최신 fault 명령은 `mov eax,dword ptr [rdi+rbx]`, 읽기 주소는 0x1F6A5000000이다. 11월 10일 20:37 크래시는 raw 종료/변환 이전이며, 직전 메모리 로그는 1.55 GB다. 기존 연속 기록이 크래시를 처음 도입했다거나 OOM이라고 단정할 증거는 없다. 네이티브 머티리얼 버퍼 복사 오류의 원인 에셋은 아직 미확정이다. 증거는 `Logs/ProfilerDiagnostics/material-cbuffer-crash/`에 보존했다.

#### C2 실제 검증 결과

- 후속 승인: 사용자가 두 파일 수정·직접 검증을 승인했다. `FieldCellFillStack`의 배열을 `byte.MaxValue + 1`개로 생성하고 셰이더 주석을 전체 256칸 전달 계약으로 수정했다. 표시 규칙·SRP 설정·셰이더 연산·게임 조립에는 변경이 없다. 배열은 기존처럼 생성 때 한 번 할당하며 갱신 시 재사용한다. 최초 그리기 전에 `ApplyPlate`가 배열을 전달한다. SSOT는 기존 모양 계산 소유자를 유지하고 C#/셰이더 경계의 고정 용량 계약만 일치시켰다.
- Unity에서 `ProjectL.Systems.dll`이 21:10:52에 갱신됐다. 당시 이미 진행 중이던 Play에서 스크립트 리로드가 발생해 `20261110-211020-2138583b368c40e3acc1fb3e94f96fce`는 `Interrupted`로 종료됐다. 전투 씬 루트 해제 예외는 이 리로드 중 발생한 것으로, 수정본의 새 Play 검증과 구분한다.
- 수정본 새 재생 `20261110-211201-262ff6951c8c4a938656fbbe22b29832`: 21:12:01~21:14:22(약 141초), 배치·전투 진입·적 턴 두 차례·정상 Play 종료. 크래시 비재현, 누락 경고 0. 자동 변환에서 0~8604의 8,605프레임, 스킵 0, 행동 경로 164개까지 읽었지만 마지막 `Directory.Move`가 접근 거부 IOException으로 실패했다. 이는 네이티브 크래시와 별개인 출력 확정 실패다. 파일 잠금 주체는 미확정이다.
- 출력 확정은 IOException/UnauthorizedAccessException에 최대 5회, 100ms 간격으로 재시도한다. 모든 파일을 쓴 뒤 확정에 실패하면 `.partial-*` 완성 CSV를 보존하고 실패를 보고한다. 취소/쓰기 도중 실패의 임시 파일 정리 정책은 유지한다. 원본은 항상 보존한다.
- 최종 검증: `ProjectL.Tool.Editor.dll` 21:17:41 컴파일 반영, 같은 원본 재변환 `20261110-211854-441-740d3808` 정상 완료(21:20:35). 0~8604 총 8,605프레임, 스킵/누락 경고 0, `Converted`, 재생 중 아니오, 출력 6개 파일 존재. 배치 제출 2972, 턴 종료 5173/7264, 적 TraitState 5174~7646을 확인했다. 포효 전용 마커는 관측되지 않았다. 이번 성공에서 잠금 재시도 분기가 실제 실행됐는지는 계측하지 않았다.
- 검증 종료 시 Unity는 Edit 모드이며 자동 기록 메뉴는 켜진 상태다. Test 코드·MSBuild·커밋·push 없음. 범위 내 diff 공백 검사를 통과했다. 원본 전체 로드와 경로별 CSV 때문에 변환 메모리·파일 크기는 기록 길이에 비례한다(이번 frame_markers.csv 약 1.92 GB).

- Unity GUI에서 기록 OFF 진입·종료와 ON 진입·배치·전투 시작·턴 종료·Play 종료를 수행했다. 두 실행에서 크래시는 재현되지 않았다. 이것만으로 간헐 크래시 해결을 주장하지 않는다.
- ON 원본 `20261110-204710-e081d82b982249158b0103f97a52af7d`는 약 91초, 관측 게임 프레임 1~11064, 누락 경고 0이다. 처음 변환은 10765~11064의 마지막 300프레임만 남겼으며, Play 종료 도중 `isPlaying`이 true인 상태에서 변환했다. 두 결함을 실제 출력으로 확인했다.
- `ProfilerCaptureHistory`가 변환 동안에만 native history를 확장하고 종료 시 사용자 설정의 길이로 복원한다. 플레이 중 버퍼 설정과 EditorPrefs는 변경하지 않는다. 내부 API가 없으면 원본을 보존하고 실패한다. Import와 자동 변환 모두 `isPlaying`과 `isPlayingOrWillChangePlaymode`를 함께 검사한다.
- Unity Assets Refresh로 `ProjectL.Tool.Editor.dll` 컴파일을 확인했다. 수정 후 같은 원본을 다시 변환한 `20261110-205412-101-59ea4d5b`는 프레임 0~11064, 출력 11,065개, 스킵 0, 행동 경로 156개, `Converted`, 재생 중 아니오다. 배치 제출은 프레임 5273에 보존됐다. 이 재생에서 포효 마커는 관측되지 않아 포효 재현 확인으로 세지 않는다.
- 크래시 후보: `FieldCellFillStack`은 `_HoleShapes`를 실제 종류 수만큼 전달하지만 `CombatCellFill.shader`의 `UnityPerMaterial`은 `float4[256]`을 선언한다. 해당 버퍼의 정렬 포함 크기 4,288바이트는 덤프의 r9=0x10C0과 일치한다. 미니덤프에 원본 머티리얼 힙이 없어 객체 신원/인과 확정은 불가능하다. 두 파일을 고정 256개 전달 계약으로 맞추는 최소 변경을 사용자에게 제안했으며 승인 전에는 수정하지 않는다.
- 기존 임시 비활성화 설명과 컴파일 미확인 표기는 아래 C1 시점의 이력이다. C2에서 수행하지 않은 Domain Reload 옵션별 대조·실패 주입·장시간 안정성은 여전히 미검증이다.

`20261110-203652-c3a3d10450e741e8afa8279d5ffbba6d`는 Recording 상태에서 종료되었고 raw는 약 252 MB다. Editor.log의 Managed Stacktrace는 GameView 렌더링의 ScriptableRenderContext.Internal_Cull_Injected를 가리킨다. CSV 변환 완료 증거는 없다. 연속 기록과 크래시의 정확한 인과는 이 로그만으로 확정하지 않는다. 앞선 경로 정규화 수정으로 안정성이 확인됐다는 의미가 아니다.

`Available = false`로 정적 초기화의 Editor 콜백 등록/Recover 실행, 자동/수동 시작, raw/data Import를 차단했다. 자동 시작·대기 변환 SessionState를 초기화하며 기존 raw·출력 파일은 삭제하지 않는다. 사용자에게 재현 녹화를 요구하지 않았고 Unity 실행/컴파일은 수행하지 않았다. 원인 해결이나 전체 플레이 기록 완료가 아닌 추가 피해 방지 조치다.

## Phase C1 — 연속 기록·원본 재변환

- [x] C1.1 고유 raw/manifest와 Editor 기록 수명. 자동 Play·중간 시작·중지·리로드·종료·실패 설정 복원.
- [x] C1.2 raw/data 로드와 기존 버퍼 보존, 전체 범위 CSV 변환. 기록 출처·관측 수·누락·행동 마커 0건 보고.
- [x] C1.3 사용자 메뉴와 안내. 재변환/취소 시 원본 보존.
- [x] C1.4 정적 검토, .meta, diff/변경 경계 확인. Phase 종료 보고.

## C1 구현·정적 검토 기록

- `ProfilerContinuousCapture`가 Editor 콜백·SessionState·설정 복원을 소유한다. 대기 중인 변환을 새 기록으로 덮지 않는다. 기록 경로 소유권이 바뀌면 다른 기록 설정을 덮어쓰지 않는다.
- `ProfilerCaptureImport`가 raw/data 로드 전 기존 버퍼를 보존하고 `ProfilerFrameExporter.ExportCurrent`를 호출한다. `ProfilerCaptureRecord`가 원본과 출력의 진단 메타데이터다. 외부 파일 옆 manifest는 읽기만 한다.
- 기존 CSV 출력기는 추가 capture.json을 임시 출력 폴더에 함께 쓴 후 확정한다. 취소·실패 시 원본을 지우지 않고, 재변환은 새로운 출력 폴더를 만든다.
- 수집 중에는 Hierarchy/CSV 처리를 하지 않는다. 기존 동작 마커, Core, FieldCamera, Architecture, 에셋, Test 파일은 이번 C1에서 변경하지 않았다. 앞선 마커 작업과 사용자 미커밋 변경은 보존했다.
- 새 파일 3개의 `.meta`, 변경 C# 5개의 괄호·문자열 제외 구분자 짝/UTF-8/LF/문자 손상 여부, 메뉴 수명·복구·취소 경로, scoped `git diff --check`를 정적으로 확인했다. 컴파일·실제 API 실행·장시간 raw 로드 확인의 대체 증거가 아니다.
- SSOT/SOLID/FW/JMH·기능 독립성은 설계의 구조 검토를 따랐다. 기존 스캐너/집계/직렬화를 재사용하고 게임 서비스 조립과 기능 구현에 기록 의존성을 추가하지 않았다.

## 사용자 Unity 인수 대기

### Play 진입 중단 후속 수정 — 2026-11-10

- 증거: `20261110-202057-e337eda18b6f46adb5b5c167cbdf8d37/capture.json`은 게임 프레임 1에서 경로 소유권 변경으로 Interrupted. Editor.log에는 이후에도 binary 기록이 켜져 재변환·새 시작이 거절된 기록이 있다. 당시 actual logFile 값은 기록하지 않아 원인 확정은 불가하다.
- 확인된 결함: OwnsLog가 원시 문자열을 비교해 같은 파일의 Windows/Unity 구분자 표기 차이를 다른 소유자로 판정할 수 있었다. 절대 경로·구분자 정규화를 공통 비교에 적용했다. 시작 시점과 raw 기록 방식은 유지한다.
- 이전 실패로 켜진 채 남은 기록은 새 시작/원본 변환 전, Last의 Interrupted 원본과 현재 네이티브 경로가 일치하고 Play 밖일 때만 닫는다. 원본 재개·덮어쓰기나 다른 소유자의 기록 종료는 하지 않는다. 이전 버전에서 이미 버린 설정 스냅샷은 복구할 수 없어 남은 기록의 종료만 처리한다.
- 실제 경로 변경/기록 중지 시 actual/expected 경로와 binary/enabled 상태를 종료 사유에 남겨 재발 시 구분한다. Import의 manifest 경로 확인도 같은 비교를 사용한다.
- 정적 경로·수명 검토 및 scoped diff 점검 완료. 게임 서비스·조립·마커 변경 없이 Editor 기록 소유자와 Import 안에서 수정해 기존 SSOT/SOLID/FW/JMH 경계를 유지했다. **컴파일 미확인**, Domain Reload 켬/끔·300프레임 초과 자동 출력은 사용자 Unity 확인 대기다.

2026-11-10 컴파일 오류 후속: 사용자 Unity에서 `ProfilerCaptureImport.cs`의 CS0023 보고. `ProfilerDriver.SaveProfile`은 void인데 bool로 취급한 오류를 수정했다. 저장 호출 뒤 백업 파일 존재·크기를 검사하도록 변경했다. 재컴파일은 사용자 확인 대기이며 **컴파일 미확인**이다.

- [ ] 컴파일. Domain Reload 켬/끔에서 자동 기록 시작·종료, 중간 시작·수동 중지.
- [ ] 300프레임보다 충분히 긴 플레이를 녹화하고 초반 배치·후반 포효가 같은 출력에 존재하는지 확인.
- [ ] raw 로드 범위·CSV 수와 관측 game-frame span 대조. GPU/GC/행동 마커 확인.
- [ ] Play 진입 취소·스크립트 리로드·쓰기/로드 실패·변환 취소·재변환에서 raw 보존과 설정 복원.

Test 코드·MSBuild·Unity 배치 실행·커밋·push 없음.
