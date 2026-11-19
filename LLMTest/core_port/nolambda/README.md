# Core 람다 걷기 (연구 저장소 전용)

계획 AI가 지나는 Core 함수의 람다(LINQ·단계 함수 콜백)를 같은 결과의 반복문·단계 없는 판으로 바꾸는 스크립트다. 측정과 해석은 [병렬 설계](../../docs/2026-11-18-planner-background-parallel-design.md) §18(측정)·§22(적용).

- **2026-11-19부터 이 저장소의 Core(`Assets/.../CoreCaculator`)에 적용돼 있다**(사용자 "Project_LLM이니까 해도 된다, 본 프로젝트에 넣을지는 상의"). 팀 저장소(Project_L)·develop에는 없다.
- `make.py`는 고치기 전 Core를 `src/`로 가져와 문장을 바꾼다. 바꾸는 것은 셋이다: `EDITS`(문장 바꿈 35곳), `PATTERNS`(정규식, 특성 판정 13곳), `apply_batch5`·`apply_batch6`(공격 선택의 단계 함수를 단계 알림·결과 콜백 없이 도는 판으로 자동 변환해 따로 둠. 단계 판은 그대로).
- 바꾼 문장은 결과가 같아야 한다(같은 순서·같은 예외 조건). 고칠 문장이 원본에 정확히 한 번 있어야 하고, 아니면 멈춘다(조용히 틀리지 않게).
- `src/`·`bin/`·`obj/`는 만들어지는 것이라 커밋하지 않는다.

## 쓰는 법 (저장소 루트에서)

develop 갱신 뒤(Core가 develop 판으로 돌아온 상태)에 다시 넣기 — `../README.md` §3의 4번:

```bash
python -I LLMTest/core_port/nolambda/make.py --apply
```

Assets의 Core에 이미 적용된 상태에서는 고치기 전 판을 git에서 읽는다(적용 전 마지막 커밋은 `d20074be`):

```bash
python -I LLMTest/core_port/nolambda/make.py --base d20074be
```

- `--apply`를 붙이면 `src/`에서 달라진 `.cs`를 Assets의 Core에 넣고, 새 파일 `Contracts/Common/TraitStatusScan.cs`의 `.meta`(고정 GUID)를 만든다.
- 붙이지 않으면 `src/`만 만든다. 이 폴더의 프로젝트 파일(`Contracts`·`Domain`·`Application`·`Harness`)이 `src/`로 측정 도구를 빌드한다(`dotnet build LLMTest/core_port/nolambda/Harness -c Release`). 원본과 견줄 때 쓴다: 고치기 전 판은 `git worktree`나 `git stash`로 Core를 `d20074be` 판으로 둔 측정 도구(`../harness`)다.

## 넣은 뒤 확인

`../README.md` §4의 측정 도구로 `dump`(결정 기록이 고치기 전과 같은지), `memocheck`(어긋남 0), `tests`(Core 시험)를 돌린다. 2026-11-19 적용 때의 결과는 병렬 설계 §22.

## 알아 둘 것

- 공격 선택·대상 모으기·칸 고르기·가림 판정은 **단계 판(`…Steps`)과 단계 없는 판(`…Direct`) 두 벌**이 있다. 단계 없는 판은 이 스크립트가 단계 판에서 기계적으로 만든다. Core에서 단계 판을 손으로 고쳤으면 이 스크립트를 다시 돌려 단계 없는 판을 맞춘다(손으로 한쪽만 고치지 않는다).
- 정렬을 바꾼 곳 셋(범위 칸, 공격 대상 ID, 먼지 막이 출처)은 "같은 키 = 같은 값"이라 결과가 같다. 먼지 막이는 환경 사실이 타일 ID 중복을 막는 것(`MovementEnvironmentFacts` 생성자)에 기댄다.
