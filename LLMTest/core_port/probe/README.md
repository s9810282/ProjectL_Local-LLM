# 계획 AI 할당·시간 나누기 측정 (측정 전용)

계획 하나의 할당·시간을 "계획 AI가 부른 Core 함수 안"과 "우리 코드"로 나눈다. 결과와 해석은 [병렬 설계](../../docs/2026-11-18-planner-background-parallel-design.md) §16.

- `instrument.py`가 `Assets/ResearchEnemyPlanner/*.cs`를 `probe/planner/`로 복사하고 Core 호출을 `AllocProbe.E(칸, AllocProbe.B(), <호출>)`로 감싼다. 저장소의 계획 AI 파일은 바꾸지 않는다. `planner/`와 `artifacts/`는 만들어지는 것이라 커밋하지 않는다(`.gitignore`).
- 계획 AI 코드에 Core 호출이 새로 생기면 `instrument.py`의 `SLOTS`에 무늬를 더한다. 빠지면 그 몫이 우리 코드로 잡힌다.
- 빌드 출력은 `--artifacts-path`로 따로 둔다(다른 세션의 측정 도구 빌드와 파일을 잠그지 않게).
- `--damage-log`를 붙이면 예상 피해마다 질문과 답을 적는 사본을 만든다(되풀이 세기, `damage` 모드). 시간·할당을 잴 때는 붙이지 않는다.
- `--move-memo`를 함께 붙이면 빠른 이동 점수의 `MoveTo`를 "같은 유닛 객체·같은 칸이면 같은 사본"으로 바꾼 사본을 만든다([기억 설계](../../docs/2026-11-18-planner-damage-memo-design.md) DM3 측정). `damage` 모드가 판마다 적는 지문(평가 수·결정 해시)이 없을 때와 같으면 결정이 바뀌지 않은 것이다.

저장소 루트에서:

```bash
python -I LLMTest/core_port/probe/instrument.py
```

```bash
dotnet build LLMTest/core_port/probe/Harness -c Release --artifacts-path LLMTest/core_port/probe/artifacts
```

```bash
dotnet LLMTest/core_port/probe/artifacts/bin/Harness/release/ProjectL.Tests.Core.dll N5,N8,T8,G16 l0,l1
```

인자: 판 ID 목록, 깊은 V 방식(`l0` 옛 깊은 V, `l1` 한 턴 더 보기). 판마다 데운 뒤 한 번 잰다.

예상 피해 되풀이 세기(병렬 설계 §17, 기억 열쇠 L6~L8은 기억 설계 §11):

```bash
python -I LLMTest/core_port/probe/instrument.py --damage-log
```

```bash
dotnet LLMTest/core_port/probe/artifacts/bin/Harness/release/ProjectL.Tests.Core.dll damage N5,N8,G16 l0,l1
```

빌드 명령은 위와 같다. 다 쟀으면 `--damage-log` 없이 다시 만들어 둔다.
