# 에디터 자동 재생 (연구 저장소 전용)

열려 있는 Unity 에디터에서 사람 손 없이 "재생 → 메인 배치 → 전투 진입 → 턴 종료 → 첫 적 턴"을 돌려, 계획 AI 콘솔 줄을 파일로 받는다. 에디터가 프로젝트를 열고 있으면 배치 모드로 Unity를 따로 띄울 수 없어서 만든 것이다. 결과와 해석은 [병렬 설계](../../docs/2026-11-18-planner-background-parallel-design.md) §24.

- 에디터 쪽: `Assets/ResearchEnemyPlanner/Editor/ResearchAutoPlay.cs`(+ 어셈블리 정의). 이 폴더의 `trigger.txt`가 생기면 시작하고, 끝나면 `done.txt`에 상태·계획 줄·오류(처음 다섯 줄)를 쓴다. 트리거가 없으면 아무 일도 하지 않는다.
- 바깥쪽: `run.ps1`(트리거를 만들고 에디터 창을 앞으로 부르며 `done.txt`를 기다림), `focus.ps1`(에디터를 떠났다 돌아와 바뀐 스크립트를 다시 컴파일하게 함), `win.ps1`(창 찾기·앞으로 부르기).
- 스테이지는 `ProjectL > Play From Boot 설정`에서 고른 것을 그대로 쓴다.

저장소 루트에서:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File LLMTest/core_port/autoplay/focus.ps1 -Seconds 60
```

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File LLMTest/core_port/autoplay/run.ps1 -Label run
```

알아 둘 것:

- **도는 동안(한 번에 약 1분) 그 PC의 마우스·키보드를 쓰면 섞인다.** 에디터 창을 5초마다 앞으로 부르고, 게임 화면에 가짜 키·클릭을 넣는다.
- 메인 배치는 덱 UI와 같은 입구로 메인 카드를 집고 필드 칸을 아래 줄 가운데부터 하나씩 눌러 본다. 놓였는지는 필드 위 유닛 수가 늘었는지로 본다(메인은 놓인 뒤에도 덱 카드로 다시 집을 수 있다).
- 코드를 바꾼 뒤에는 `focus.ps1`을 먼저 돌려 컴파일이 끝나게 한다. 재생 중에 스크립트가 바뀌면 에디터가 재생 도중 다시 컴파일해 예외가 쏟아진다.
- `done.txt`의 `lastSubmitToDecision`은 마지막으로 턴 종료를 누른 때부터 첫 결정이 준비될 때까지다. 누름이 0.5초 간격이라 그만큼의 오차가 있다.
- 게임 내부 이름(`DeckCardList`·`FieldView`·`CombatActionPanel`·`TextNoticeUI`의 private 멤버)에 기대므로, 그쪽이 바뀌면 고쳐야 한다.

## 빌드(플레이어)에서 돌리기

에디터는 관리 힙이 커서(약 850MB가 살아 있음) GC 멈춤이 실제 게임보다 훨씬 길게 나온다. 프레임 튐은 빌드에서 잰다(병렬 설계 §26).

1. 개발 빌드 만들기 — 열려 있는 에디터에서: 트리거 내용을 `build`로 준다. 출력은 저장소 밖 `%TEMP%/ProjectL_ResearchBuild/ProjectL_Research.exe`(약 1.3GB).

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File LLMTest/core_port/autoplay/run.ps1 -Label build -Gc build -TimeoutSeconds 1200
```

2. 빌드 실행(자동 재생 포함) — 결과는 `%TEMP%/ProjectL_ResearchRun/<이름>/done.txt`.

```bash
bash LLMTest/core_port/autoplay/run_player.sh auto1 auto
```

- 빌드 쪽 자동 재생은 `Assets/ResearchEnemyPlanner/Runtime/ResearchRuntimeAutoPlay.cs`다. 실행 인자 `-researchAutoPlay`가 있을 때만 켜진다. `-researchGc auto|manual:<ms>|disabled`로 계획 동안의 GC 방식을 고른다(에디터는 GC 방식 변경을 받지 않는다).
- 빌드에는 에디터의 Play From Boot 설정이 없으므로, 빌드할 때 `Editor/ResearchBuild.cs`가 빌드되는 Boot 씬에만 그 설정을 적용한다(씬 파일은 그대로). 그래서 **빌드는 빌드한 PC의 Play From Boot 설정(시작 Combat·스테이지)을 그대로 담는다.**
- 빌드에서는 튜토리얼이 늘 자동 시작해 조작을 막는다. 개발 빌드를 `-researchNoTutorial`로 띄우면 건너뛴다(`GameBootstrap.cs`의 연결 줄, `hooks.patch`).
- 첫 빌드 때 에디터가 "Addressables Build Report" 확인 창을 띄우고 멈춘다(설정을 켤지 묻는 창). 한 번 답하면 다시 묻지 않는다.
- 게임 창이 뜨고 가짜 키·클릭이 들어가므로, 도는 동안(한 번에 약 1분) 마우스·키보드를 쓰지 않는다.
- 긴 프레임 원인 보기(병렬 설계 §27): `bash LLMTest/core_port/autoplay/run_player.sh rounds1 auto 6` — 셋째 인자가 볼 적 턴 수다(기본 1). `done.txt`의 `spike` 줄이 8.3ms 넘은 프레임(메인 스레드에서 시간이 든 구간 위쪽), `watch` 줄이 구간별 첫 값과 그 뒤 값이다. 6번이면 약 2분 걸린다.
- 넷째 인자부터는 빌드에 그대로 넘긴다. 미리 정산 실험(병렬 설계 §28): `bash LLMTest/core_port/autoplay/run_player.sh pre_on1 auto 6 -researchPresettle`. 인자가 없으면 꺼져 있다.
- 미리 컴파일 실험(병렬 설계 §29): `bash LLMTest/core_port/autoplay/run_player.sh pw_combat auto 3 -researchPrewarm Combat`. 범위 `"*"`는 게임 어셈블리 전부, `"**"`는 엔진까지다. 결과의 `prewarm` 줄이 컴파일한 메서드 수와 든 시간이다.
