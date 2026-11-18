# 람다 걷기 측정 사본 (측정 전용)

"계획 AI가 지나는 Core 함수의 람다(LINQ)를 반복문으로 바꾸면 얼마나 줄어드나"를 재기 위한 Core 사본이다(2026-11-18).
**Core 원본(`Assets/.../CoreCaculator`)은 바꾸지 않는다.** `make.py`가 Contracts·Domain·Application을 `src/`로 복사한 뒤
`EDITS`의 문장을 바꾼다. 바꾼 문장은 결과가 같아야 한다(같은 순서·같은 예외). `src/`·`bin/`·`obj/`는 만들어지는 것이라 커밋하지 않는다.

- 바꾸는 것은 셋이다. `EDITS`(문장 바꿈), `PATTERNS`(정규식, 특성 판정 13곳), `apply_batch5`·`apply_batch6`(공격 선택의 단계 함수를 단계 알림·결과 콜백 없이 도는 판으로 자동 변환해 따로 둠. 단계 판은 그대로).
- 측정 도구 프로그램은 `../harness/Harness/Program.cs`를 그대로 쓴다. 원본 Core로 빌드한 측정 도구와 이 사본으로 빌드한 것을 같은 모드로 돌려 비교한다.
- 고칠 문장이 원본에 정확히 한 번 있어야 한다. develop 갱신으로 원본이 바뀌면 `make.py`가 멈춘다.
- 어느 자리를 고를지는 `allocbysite`(할당 표본의 형식을 람다 묶음·반복기를 둘러싼 Core 함수로 바꿔 모음)로 본다.

저장소 루트에서:

```bash
python -I LLMTest/core_port/nolambda/make.py
```

```bash
dotnet build LLMTest/core_port/nolambda/Harness -c Release
```

```bash
dotnet LLMTest/core_port/nolambda/Harness/bin/Release/net8.0/ProjectL.Tests.Core.dll bench N5,N8,T8,G16 3
```

비교할 원본 쪽은 `dotnet build LLMTest/core_port/harness/Harness -c Release` 뒤 같은 dll 경로의 `harness`판. 결정이 같은지는 `dump`(README `../README.md` §4)로, 예상 피해 기억이 맞는지는 `memocheck`로 본다.

결과와 해석: [병렬 설계](../../docs/2026-11-18-planner-background-parallel-design.md) §18.
