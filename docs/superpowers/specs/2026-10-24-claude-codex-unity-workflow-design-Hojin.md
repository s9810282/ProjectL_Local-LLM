# Claude–Codex 자동화 연결 설계

- 작성자: 호진(이호진) — 본문 초안은 호진 요청으로 Codex가 씀
- 상태: 사용자 승인(2026-10-24) — **시험 운영**. 사용자가 해 보다가 중간에 설계·역할·흐름을 바꿀 수 있다(Q-W1). Phase A 실행 중이다(계획).
- 범위: Windows Claude 데스크톱 **Code 탭 / Local 세션**, 같은 로컬 프로젝트의 Codex 실행, Unity 6000.0.73f1 사이의 역할·규칙 확인·작업 인계·검수·Unity 적용 연결. 개발 도구이며 게임 런타임의 일부가 아니다. 실제 구현은 Phase별 계획과 사용자 승인으로 한다.
- 계획: [Claude–Codex 자동화 연결 계획](../plans/2026-10-24-claude-codex-unity-workflow-plan-Hojin.md)
- 작성: Codex(사용자가 이 연결 설계서 작성을 직접 요청). 이후 일반 기능 문서의 작성·유지는 Claude 담당이다.
- 정리: 2026-10-27 승인 때 각 절에 덧붙인 보완 D1~D9와 계획의 결정(Q-W·P)을 §3 결정 기록으로 모으고, 승인된 것과 아직 제안인 것을 나눴다. 옛 판은 git `10914570`.

## 1 목적과 범위

Claude는 C#과 문서를 쓰고, Codex는 문서·코드를 검수하며 Unity에서 보이는 화면·씬·프리팹·참조·설정을 구성한다. 사용자는 두 도구 사이에서 내용을 매번 복사하지 않고 승인한 Phase 안의 작업을 이어 가며, 설계 변경과 Phase 종료에서는 직접 판단한다.

작업 순서를 하나로 고정하지 않는다. 문서 검수, 코드 검수, Unity 화면 적용, 코드 수정 요청, 문서 반영 요청을 작업에 맞춰 잇는다.

### 1.1 확인한 환경 (2026-10-24)

| 항목 | 내용 |
|---|---|
| 작업 환경 | 사용자가 Claude 데스크톱 Code 탭에서 작업한다. Desktop 앱 3.6556.2, `CLAUDE_CODE_ENTRYPOINT=claude-desktop`, Agent SDK 0.3.308. `claude` CLI는 PATH에 없다 |
| 저장소 | 연결 대상은 `C:\Users\as010\Project_L`(브랜치 `feature/Hojin/StageSelect`). 다른 복사본·Dropbox 경로·worktree를 같은 대상으로 보지 않는다 |
| Codex | `codex`는 PATH에 없다. Codex 앱이 실제로 돌리는 판은 `%LOCALAPPDATA%\OpenAI\Codex\bin\<임의 이름>\codex.exe`(0.164.0-alpha.11.1, 앱 업데이트 때 폴더 이름이 바뀜)이고, 0.142.2는 앱 설치 폴더에 남은 옛 판이다(D1). 두 판 모두 `exec`에 `--ignore-user-config`·`--ephemeral`·`--output-schema`·`-o`·`--json`·`-s read-only`·`-C`가 있다. 프로젝트 `trust_level = "trusted"`. ChatGPT 로그인(2026-10-24 Phase A E4) |
| 셸 | PowerShell 5.1.26100, Git Bash. node·python 없음(`python`은 Store 바로가기). 실행 정책 LocalMachine·Process `Bypass` |
| 기존 규칙 | 루트 `AGENTS.md`, `CLAUDE.md`, `Assets/AGENTS.md`. 두 루트 문서는 내용과 최신 기록이 다르다(§4.3) |
| Unity 제어 | `Packages/manifest.json`에 `com.unity.pipeline`이 없고 PATH에 `unity`가 없다. 다른 수단까지 없다고 단정하지 않는다. 자동 Editor 연결·캡처는 미검증이다 |
| 기존 변경 | 다른 작업의 미커밋 변경(로비 코드·씬·프리팹·시험·문서 등)이 있다. 자동화의 입력·산출물로 자동 편입하지 않는다 |

Desktop Local Code 세션이 CLI와 지침·Hook 설정을 공유하는 것은 [공식 Desktop 문서](https://code.claude.com/docs/en/desktop#shared-configuration)로 확인했다. 설치된 Desktop 내장 판의 실제 Hook 지원은 Phase A E1~E3에서 확인한다.

## 2 지금 구조

### 2.1 선택한 방식

**로컬 중계기 + 짧은 Hook + Codex CLI + 제한된 Editor 연결**이다(§5 비교, Q-W1로 시험 운영 승인). Claude Desktop의 현재 Local 세션을 대화 입구로 두고, 중계기가 요청·상태·결과를 파일로 관리하며, Codex는 CLI로 부르고, Unity 에셋 변경은 허용 명령만 노출한 Editor 연결로 한다(D 단계).

### 2.2 구성 요소와 책임

| 구성 요소 | 책임 | 하지 않는 것 |
|---|---|---|
| 출처 색인·초기 확인 | 규칙 원본·역할·승인 범위를 실행에 제공 | 게임 규칙·과거 상태를 새로 정의 |
| 작업 중계기 | 요청 검증·상태 전이·단독 편집권·중복 방지·결과 전달 | 문서·코드 내용의 옳고 그름 판정 |
| Claude 실행 | 문서·모든 C# 작성, 검수 의견의 수용/반려 판단 | 사용자 승인 생성, Codex가 작업 중인 에셋 동시 저장 |
| Codex 검수 실행 | 원본·변경본·설계·규칙 대조, 근거 있는 지적 | 게임 파일 수정, 안 한 시험을 통과로 판정 |
| Codex View 실행 | 승인된 UI·씬·프리팹·표현 설정·참조 구성 | C# 작성·평가, 규칙 값 변경, 보호 영역 우회 |
| Unity Editor 연결 | 제한된 구조화 명령을 Editor API로 수행·저장·조회 | 임의 C#·셸 실행, 임의 경로 쓰기 |
| 실행 기록 | 검수·적용·검증 근거와 미해결 항목 보관 | 요청 상태나 승인 원본의 사본으로 동작 |

이 도구는 JMH 런타임 System·Service에 등록하지 않는다. 게임의 Factory·Group·Bootstrap 조립을 자동화 편의로 바꾸지 않는다.

### 2.3 파일 배치 (D7)

중계기·색인·프롬프트·상태는 **저장소 밖 개인 폴더 `C:\Users\as010\ProjectL-Hojin\`**에 둔다(Q-W7 A). 저장소 안에는 git이 추적하지 않는 두 파일만 두고 `.git/info/exclude`(이 PC의 git만 보는 무시 목록)에 등록한다. 루트 `.gitignore`·`~/.claude/settings.json`·`~/.codex/config.toml`은 바꾸지 않는다(P8).

| 위치 | 파일 | 내용 |
|---|---|---|
| 개인 폴더 | `rules/hojin-rules.md` | **개인 규칙 원본(두 도구 공통).** §1 시험 운영 §2 역할 §3 검증 제한 §4 예외(사용자가 Codex에 직접 실행·캡처 확인 요청) §5 Phase 끝 Codex 검수 §6 공용 지침 차이 읽는 법 §7 개인 문서는 공용 파일에 쓰지 않음 |
| 개인 폴더 | `agent-workflow/relay.ps1`, `lib/Text.ps1`·`Policy.ps1`·`State.ps1`·`Codex.ps1` | 중계기 입구 하나와 입출력·정책 해시·상태·Codex 실행 |
| 개인 폴더 | `agent-workflow/policy/sources.json` | 규칙 출처 색인(§4.1 표 + 개인 규칙). id·경로·절·읽는 때(`always`·`assets`·`structure`·`review`·`scene`·`combat`·`db`)·대상 도구 |
| 개인 폴더 | `agent-workflow/schemas/`, `prompts/`, `config.json`, `fixtures/` | 확인 기록·검수 결과 스키마, Codex 프롬프트 틀, Codex 찾기·한도, 검수 연결용 문서 |
| 개인 폴더 | `state/<작업 루트 이름>/` | `state.json`(상태 원본), `acks/`, `requests/<id>/`(요청·프롬프트·결과·이벤트·stderr) |
| 개인 폴더 | `docs/claude-codex-workflow-guide-Hojin.md` | 사용 안내(B에서 작성) |
| 저장소(추적 안 함) | `CLAUDE.local.md` | `@Assets/AGENTS.md`와 개인 규칙 가져오기 두 줄 |
| 저장소(추적 안 함) | `.claude/settings.local.json` | Hook: SessionStart(A), Stop(B). 명령은 개인 폴더의 `relay.ps1 hook …` |
| 사용자 홈 | `~/.codex/AGENTS.md` | 기존 일반 규칙 4줄 유지 + “작업 폴더가 `C:\Users\as010\Project_L`이면 개인 규칙을 먼저 읽고 따른다” 한 절(Q-W8) |

로컬 상태는 Unity `Library` 삭제와 함께 사라지는 캐시로 두지 않는다. 로그·요청에 인증 토큰·비밀키를 저장하거나 두 도구의 인증 파일을 서로 복사하지 않는다. 모델은 사용자 설정을 따르며 요금·계정 접근을 추측하지 않는다.

바뀐 것: 승인 전 배치안(저장소 안 `tools/agent-workflow/`·`.agent-workflow/`, 공용 진입점에 색인 링크를 다는 최소 변경) → 저장소 밖 개인 폴더와 추적 안 하는 로컬 파일 둘(D7, 2026-10-24 사용자 Q-W2·Q-W7). 공용 `CLAUDE.md`·`AGENTS.md`는 다른 작업자의 AI도 읽기 때문이다.

## 3 결정 기록

### 3.1 사용자 결정 (2026-10-24)

| ID | 결정 |
|---|---|
| 역할 | “UI·표시용은 Codex, C#은 Claude”. 모든 C#·내부 구현·문서 = Claude, 검수·Unity 화면 구성 = Codex, 설계·범위·Phase 인수·커밋·push = 사용자. 중계기·Hook 스크립트도 Claude가 쓰고 Codex가 검수한다 |
| Q-W1 | 설계 선택안으로 해 본다(시험 운영). 중간에 바꿀 수 있고 Codex에게도 알려야 한다 → 개인 규칙 §1(Codex가 매 실행 읽음). 바뀌면 Claude가 개인 규칙을 먼저 고치고 설계·계획을 맞춘다 |
| Q-W2 | 공용 `CLAUDE.md`·`AGENTS.md` 대신 사용자만의 문서를 이 컴퓨터의 폴더에. 다른 작업자의 AI가 읽으면 문제다 → 공용 두 파일 무변경 |
| Q-W3 | 검증 제한을 “두 지침 파일”에 → 공용 파일이 아니라 두 도구가 함께 읽는 개인 규칙 하나(§3)로(해석 확인됨). 공용 `CLAUDE.md`의 전투 View 제한(2026-10-20)은 그대로 둔다 |
| Q-W4 | A: 중계기가 실행마다 앱 폴더에서 `codex.exe`를 찾아 쓰고 판 번호를 기록한다(앱과 같은 판, 업데이트 자동 추종). 옵션이 바뀌면 `EXEC_FAILED`로 드러나고 다른 판으로 조용히 넘어가지 않는다. B(0.142.2 고정)는 채택하지 않음 |
| Q-W5 | 실패 시나리오는 가짜 Codex 없이 미확인으로 남긴다. 예외: 사용자가 Codex에게 직접 “돌려서 캡처도 해보고 확인해줘”라고 하면 Codex가 한다(개인 규칙 §4). 그 결과는 실행 증거이며 통과 판정·사용자 인수와 구분한다 |
| Q-W6 | Phase 끝마다 Claude가 Codex 검수를 요청한다(개인 규칙 §5). 대상은 문서와 이 도구의 스크립트(B의 검수 종류를 `REVIEW_DOCUMENT` + 도구 스크립트 전용 `REVIEW_CODE`로 넓힘). 게임 C# 검수 기준은 C에서 정한다 |
| Q-W7 | A: 개인 폴더 `C:\Users\as010\ProjectL-Hojin\`(저장소 밖, `git clean`에도 남음). B(저장소 안 `.hojin\`)는 채택하지 않음 |
| Q-W8 | “개인 Codex 지침을 내 Codex만 읽는지 확인하고 맞으면 거기에” → 확인 결과 맞음(git 밖, 이 PC의 사람 계정 as010 하나, 공식 문서상 전역 개인 지침) → `~/.codex/AGENTS.md`에 Project_L 조건 절을 덧붙임(원본은 개인 폴더 `backup/`에 복사) |
| P1~P13 | 계획에서 정한 구현 선택을 그대로 승인(§3.3) |

### 3.2 설계 보완 D1~D9 (승인 때 반영)

| D | 위치 | 확인한 것 | 반영 |
|---|---|---|---|
| D1 | §1.1 Codex | `codex`가 PATH에 없다. 앱이 쓰는 판은 0.164-alpha이고 폴더 이름이 업데이트 때 바뀐다. 0.142.2는 옛 판 | Q-W4 |
| D2 | §7.3 검수 격리 | 사용자 Codex 설정에 플러그인(computer-use·chrome·browser 등)·MCP·`notify`가 있다 | 검수 실행은 `--ignore-user-config`로 격리하고, 버려지는 모델·추론 강도·service_tier는 실행마다 사용자 설정에서 읽어 넘김(P3) |
| D3 | §4.2 자동 인식 | 공용 `CLAUDE.md`가 있으면 Claude는 기본 설정에서 `AGENTS.md`(루트·하위)를 읽지 않는다(공식 문서). Codex도 `--cd` 루트에서는 `Assets/AGENTS.md`를 자동으로 읽지 않는다 | `CLAUDE.local.md`가 `Assets/AGENTS.md`를 가져오고(P13), 색인의 `assets` 조건 항목을 두 도구에 명시적으로 넘김 |
| D4 | §4.3 검증 제한 | 사용자 지시(2026-10-23·24)는 전투 View만이 아니라 사용자의 모든 작업이며 EditMode 실행 요청도 하지 않는다. 지금은 Claude 메모에만 있다 | 개인 규칙 §3 |
| D5 | §7.1 반환 경로 | Claude가 실행한 백그라운드 명령의 종료 알림이 같은 Desktop 세션을 다시 부른다 | 결과 반환 1차 경로(P4). Hook은 시작·재개 복원과 미인수 알림만 |
| D6 | §11 가짜 프로세스 | 사용자 결정 Q-W5 | 가짜 없이 실제 실행, 재현 못 한 실패는 미확인 |
| D7 | §2.3 배치, §4.3 진입점 | 공용 두 파일은 다른 작업자도 읽는다(Q-W2) | 공용 진입점 무변경. 개인 폴더·`CLAUDE.local.md`·`~/.codex/AGENTS.md`(Q-W8)·중계기 프롬프트로 개인 규칙 전달 |
| D8 | §5.2·§11 | Phase 끝마다 Claude가 Codex 검수를 요청(Q-W6) | 개인 규칙 §5, B 검수 종류 |
| D9 | 상태 줄 | 시험 운영(Q-W1) | 상태 줄 |

### 3.3 구현 선택 P1~P13 (2026-10-24 사용자 “그대로”)

| P | 결정 |
|---|---|
| P1 | 중계기 언어 = Windows PowerShell 5.1(`powershell.exe -NoProfile -File`, 정책 변경 없음). 모든 파일 입출력은 UTF-8(BOM 없음) 함수 하나. `Test-Json`이 없어 구조 검사는 필수 칸만 직접. 보충(Phase A): `.ps1`은 BOM 있는 UTF-8(PS 5.1이 BOM 없는 스크립트를 ANSI로 읽음), 데이터 파일은 BOM 없음 |
| P2 | 규칙 해시는 절 단위. 색인은 경로·절 제목·읽는 때·대상 도구만 갖고 해시를 저장하지 않는다. 요청마다 절을 꺼내 LF로 맞춘 뒤 SHA-256, 모든 절 해시를 묶어 `policyDigest`. 공용 두 파일의 “현재 상태” 절은 필수 절에서 뺀다. 개인 규칙은 파일 전체가 필수. 절 제목을 못 찾거나 추출이 비면 실패 |
| P3 | Codex 격리. 검수는 `exec --ignore-user-config --sandbox read-only --cd <루트> --output-schema <스키마> -o <결과> --json -`(프롬프트는 UTF-8 파일 → 표준입력). 모델·추론 강도·service_tier는 사용자 설정 최상위 값을 읽어 `-m`·`-c`로 넘긴다. 인증은 `CODEX_HOME` 그대로, 인증 파일을 읽거나 복사하지 않음. 보충(Phase A): `--disable` 기능 10개(apps·multi_agent·image_generation·browser_use·browser_use_external·computer_use·in_app_browser·plugins·remote_plugin·hooks), `-c web_search=disabled`, `-c approval_policy=never`(샌드박스는 read-only 그대로), 사용자 설정에서 옮겨 오는 값에 `windows.sandbox` 추가(`carryUserSettings`) — 모두 `config.json` 한 곳. 2026-11-05 바뀜(사용자 "1번과 2번도 같이 적용"): 처음 판의 `--ephemeral`을 빼 세션을 기본 `CODEX_HOME\sessions`에 저장하고(Codex가 어떤 평가를 했는지 나중에 다시 보기 위함), `configOverrides`에 `model_reasoning_summary=auto`를 더해 추론 요약을 이벤트 로그에 남긴다. 요약은 모델이 주는 요약이고 내부 사고 전체가 아니다. `--ignore-user-config`라 사용자 설정 파일에서 켜도 적용되지 않는다. `codex exec` 세션이 데스크톱 앱 목록에 저절로 나타나는지는 미확인 |
| P4 | 결과 반환 1차 경로 = Bash 백그라운드 실행 종료 알림. Claude가 `relay run <id>`를 백그라운드로 실행하면 끝날 때 이 Desktop 세션이 다시 불린다(별도 프로세스·`--resume`·채팅 자동 전송 없음). 결과 원본은 요청 폴더 파일, stdout은 요약 |
| P5 | 상태는 A·B에서 `ONBOARDING`·`READY`·`CODEX_REVIEWING`·`CLAUDE_REVISING`·`NEEDS_USER`·`CANCELLED`만 쓴다. 상태 원본은 작업 루트별 `state.json` 하나. 실행 결과는 다른 칸 `outcome`(`OK`·`STALE_INPUT`·`POLICY_CHANGED`·`TIMEOUT`·`INVALID_RESULT`·`EXEC_FAILED`·`CANCELLED`), FW 판정은 또 다른 칸 |
| P6 | B의 검수는 읽기 전용이라 승인 기록 없이 사용자 채팅 지시 또는 개인 규칙 §5로 제출한다. `approvalRef`는 `read-only-review` 고정이며 쓰기 허용에는 쓰지 않는다. 쓰기 승인 입력은 C에서 정한다 |
| P7 | Hook 범위: A = SessionStart(startup·resume·compact), B = Stop 추가. PreToolUse(편집권·허용 대상 검사)는 C·D로 미룬다. B에서 검수 중 문서가 바뀌면 입력 해시 비교가 `STALE_INPUT`으로 잡는다 |
| P8 | 저장소 추적 파일 변경 0. 로컬 파일 둘만 `.git/info/exclude`에 등록 |
| P9 | 한도(`config.json` 한 곳): 검수 시간 20분, 결과 파일 256 KB, 지적 50개, 같은 요청 실행 1회(자동 재시도 없음, 다시 하려면 새 요청) |
| P10 | Stop Hook 비용: 먼저 셸에서 `state.json`에 알릴 것이 있는지만 보고 있을 때만 PowerShell을 띄운다. Hook 셸이 bash가 아니면 그 셸에 맞게 바꾼다 |
| P11 | 결과 검사: `--output-schema`(엄격: 모든 칸 필수·추가 칸 금지)가 1차, 중계기가 JSON 파싱·필수 칸·요청 ID·입력/정책 해시 되돌림·크기를 2차로 본다 |
| P12 | 확인 기록: Claude는 새 세션·재개·압축 뒤 SessionStart가 “확인 기록 필요”를 알리면 `relay sources`로 받은 원본을 읽고 `relay ack claude`로 기록(세션 ID·`policyDigest`·출처별 해시와 핵심 규칙 2~5줄·역할·보호 영역·금지 동작·검증 제한·미해결 충돌). 없거나 낡으면 `submit` 거절. Codex는 모든 검수 결과에 같은 모양의 확인 기록 칸을 넣고, A에서 확인 기록만 받는 실행을 한 번 한다 |
| P13 | 개인 규칙 전달: Claude = `CLAUDE.local.md`가 `@Assets/AGENTS.md`와 개인 규칙을 가져옴(작업 폴더 밖 파일은 프로젝트마다 처음 한 번 승인 창). 승인 창이 안 뜨거나 거절되면 SessionStart Hook이 개인 규칙 원문을 문맥에 넣는다. Codex = 중계기 실행은 프롬프트에 원문, 앱 대화는 Q-W8 |

### 3.4 아직 제안이거나 다음 계획에서 정할 것

| 항목 | 지금 | 정할 곳 |
|---|---|---|
| 수정·재검수 한도 3회 | 설계 제안 | C 계획 |
| 쓰기 승인 입력 방식, 단독 편집권, PreToolUse | 미정(P6·P7) | C·D 계획 |
| 게임 C# `REVIEW_CODE` 기준(FW·Unity 계약) | 미정(Q-W6) | C 계획 |
| Editor 제한 명령의 구체 API·폴더·asmdef·저장·Undo·dirty | 기존 도구 조사 뒤 | D 계획 |
| 작업별 취소·시간 한도·결과 상한 | A·B 검수만 P9로 정함 | 각 Phase 계획 |
| 요청·결과 JSON Schema의 필수 값·열거형 | B는 검수 결과·확인 기록만 | 각 Phase 계획 |
| SDK/App Server 중심 별도 클라이언트, Unity CLI/Pipeline 설치 | 채택하지 않음. 바꾸려면 설계 변경으로 보고 | — |

## 4 역할과 규칙 원본

### 4.1 역할과 소유권

| 대상 | 작성·수정 | 상대 도구 |
|---|---|---|
| 모든 `.cs`와 그 `.meta` | **Claude**(UI·표시·MonoBehaviour·Editor 도구·자동화용 Editor C# 포함) | Codex는 검수·수정 요청만. 임시 C#·문자열 C# 평가로 우회하지 않음 |
| 내부 기능 구현 | Claude(작업자가 고칠 수 있는 영역·승인 범위 안) | Codex가 계약·동작·영향 검수 |
| 일반 설계·계획·가이드·인계 문서 | Claude | Codex가 검수하고 Unity 결과를 구조화 기록으로 넘김. 사용자가 Codex에 문서 작성을 직접 요청하면 그 요청은 예외 |
| 씬·프리팹·UI 배치·시각 설정·직렬화 참조 | **Codex**(작업별 허용 에셋·필드 안) | Claude가 필요한 C# 계약을 먼저 주고 결과를 문서에 반영 |
| View SO·머티리얼 | Codex의 화면 작업에 필요한 표현 값·참조 필드만 작업별 허용 | 규칙·경제·진행 값은 화면 설정이 아니다. 별도 판단 요청 |
| 중계 코드·Hook 코드·Editor 연결 도구 | Claude 작성, Codex 검수 | 게임 UI 담당이라는 이유로 Codex가 도구 C#까지 쓰지 않음 |
| 설계·범위·담당 경계 변경, Phase 인수 | **사용자** | AI 결과·검수 통과를 사용자 승인으로 바꾸지 않음 |
| 커밋·push | **사용자** | 자동화와 두 도구 모두 하지 않음 |

이 역할 분담은 사용자 개인 지시라 공용 `CLAUDE.md`·`AGENTS.md`에 적지 않는다. 지금 적용 원본은 개인 규칙 §2이고, 이 표는 설계 근거다(D7).

**Core·FieldCamera·Architecture의 기존 담당자 소유권은 그대로다.** “Claude가 내부 기능을 맡는다”는 보호 영역을 넘긴다는 뜻이 아니다. Core 요구는 [데이터 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 요청으로 남기고, 예외는 사용자가 명시한 작업에만 적용한다.

### 4.2 처음에 읽을 원본과 확인 절차

공통 규칙 본문을 여러 프롬프트에 복사하지 않는다. 원본을 가리키는 **출처 색인**을 두고 실행마다 경로·절·해시를 기록한다(P2). 추출한 요약은 원본·해시에 묶인 파생 자료다.

| 읽는 때 | 원본 | 목적 |
|---|---|---|
| 모든 새 작업·재개 | 개인 규칙 전체(저장소 밖) | 시험 운영·역할·검증 제한·예외·Phase 끝 검수·공용 지침 차이 읽는 법 |
| 모든 새 작업·재개 | [AGENTS.md](../../../AGENTS.md), [CLAUDE.md](../../../CLAUDE.md)의 작업 방식·기준 문서·DB·코딩·주의·검증 절 | 사용자 지시, 역할, 금지 범위, 공통 규격 |
| 대상 경로 결정 | 해당 경로의 상위·하위 지침. `Assets` 작업은 [Assets/AGENTS.md](../../../Assets/AGENTS.md) 필수 | 경로별 UI 입력·조립·등록 규약 |
| 첫 접속·구조 변경 | [Script README](../../../Assets/ProjectL/Script/README.md), [Framework 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md) | 폴더·어셈블리·Factory·Group·서비스 조회 경로 |
| 설계·코드 검수·Unity 연결 | [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) §3–6·§12–13, [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md) | FW MUST/SHOULD/MAY, 확장 지점, 판정 기준 |
| 설계·리뷰 | [프로젝트 적용 기준](../handoffs/2026-09-30-combat-view-design-session-handoff.md) **§4만** | SSOT·SOLID 5원칙·기능 변경 독립성. 같은 문서의 과거 상태를 지금 사실로 가져오지 않음 |
| 씬·UI 적용 | [씬 연결 가이드](../viewDocs/runtime-scene-binding-guide.md)와 대상의 SceneRoot·Factory·UI 계약 | 생성·등록·Attach/Detach·개폐·해제 |
| Combat C# 요청·검수 | [Combat 폴더 배치](../viewDocs/combat-view-folder-guide.md) | 위치·책임·namespace·`.meta` |
| 작업별 | 승인된 설계·계획·현재 Phase·관련 인계와 실제 코드 | 허용 변경과 완료 조건 |
| DB·SO | [DB-SO 설계](2026-09-24-db-so-ingame-view-group-design.md)와 해당 테이블·에셋 | P1–P7, 입력 계층·표현 값·규칙 값 구분 |

원본을 못 읽거나 절 추출이 잘리면 읽은 것으로 치지 않는다. GwonCheonmyeong 작성 옛 guides·plans·specs는 자동 판단 기준에서 뺀다. 과거 문서의 씬 번호·경로·완료 기록은 지금 코드로 다시 확인한다.

**시작·재개 절차**

1. 실제 저장소 루트·checkout·Unity가 여는 프로젝트 경로를 확인한다. 다른 worktree는 별도 작업 루트다.
2. 공통 규칙과 대상 경로 지침을 읽고 현재 승인·설계·계획·Phase를 확인한다. 자동 인식에만 기대지 않는다.
3. 시작 전 미커밋 파일과 허용 대상의 해시를 기록한다. 기존 변경의 작성자·편집 인수 여부를 모르면 그 파일에 쓰지 않는다.
4. Claude와 Codex가 각각 **확인 기록**(읽은 출처·해시, 역할, 허용 파일, 보호 영역, 금지 동작, 적용 FW 절, 검증 제한, 미해결 충돌)을 낸다(P12).
5. 중계기는 필수 출처 누락·버전 불일치·권한 초과를 본다. “숙지했다”는 자연어만으로 쓰기 준비를 열지 않는다. 기록은 이해의 증거일 뿐 준수 보증이 아니다.
6. 새 세션·재개·컨텍스트 압축·규칙 변경·담당자 교체 때 다시 확인한다. 같은 출처는 해시를 다시 쓰되, 그 내용을 받지 않은 새 실행에는 원문을 다시 준다.

Claude의 지침 가져오기는 [공식 문서](https://code.claude.com/docs/en/memory#share-one-file-with-other-coding-tools)를 따른다. 공용 `CLAUDE.md`가 있으면 Claude는 기본 설정에서 `AGENTS.md`(루트·하위)를 읽지 않으므로 `Assets/AGENTS.md`는 `CLAUDE.local.md`가 가져온다. Codex도 `--cd` 루트에서는 `Assets/AGENTS.md`를 자동으로 읽지 않아 색인의 `assets` 조건 항목으로 넘긴다(D3). `CLAUDE.local.md`는 공식적으로 “개인 프로젝트 지침”이며 `CLAUDE.md` 뒤에 붙어 읽힌다. `@` 가져오기는 파일 기준 상대·절대 경로, 4단계까지다. git이 무시하는 파일이라 만든 checkout에만 있다.

### 4.3 공용 지침 차이와 처리

공용 파일은 고치지 않고 개인 규칙 §6에 “사용자 세션에서는 이렇게 읽는다”로 적었다.

| R | 차이 | 처리 |
|---|---|---|
| R1 | 두 루트 문서 머리가 “Codex는/Claude는 View 작업자를 돕는다”. 새 역할 분담은 어느 파일에도 없다 | 개인 규칙 §2 |
| R2 | 삭제 금지: `CLAUDE.md`는 Missing Script·참조 없는 에셋 삭제 금지·감지 시 보고 + 48개, `AGENTS.md`는 48개만 | 넓은 쪽(`CLAUDE.md`)을 두 도구에 |
| R3 | 원칙 점검: `AGENTS.md`가 SOLID 5원칙·조립과 기능 구현 분리·§4.6 강조로 더 구체적 | 구체적인 쪽(`AGENTS.md`)을 두 도구에 |
| R4 | 기준 문서 표의 전투 폴더 배치 안내 줄은 `CLAUDE.md`에만 | 두 도구에(색인 `combat`) |
| R5 | 검증: `CLAUDE.md` 작업 방식에 전투 View 한정 제한(2026-10-20: MSBuild·새 `[Test]` 금지, 보고에 “컴파일 미확인”), 두 파일 “검증” 절은 옛 일반 안내(EditMode 실행), 넓힌 지시는 메모에만 | 개인 규칙 §3이 옛 일반 안내보다 우선(D4). 사용자의 모든 작업에서 EditMode 실행 요청도 하지 않는다. DB 에셋을 바꾸면 필요한 검증을 요청하되 요청·미실행·통과를 구분한다 |
| R6 | `Assets/AGENTS.md`(UI 입력·서비스 조립·UIBase 등록·예외 규약)는 Claude 자동 로드 밖 | P13 |
| R7 | “현재 상태”: `AGENTS.md`는 2026-10-20, `CLAUDE.md`는 2026-10-21까지(2026-10-24 기준) | 필수 절에서 뺌(P2) |
| R8 | Claude 메모의 다른 규칙(성능 우선 등)은 Codex가 모른다 | 이번 범위 밖. 검수 기준에 필요해지면 그때 묻는다 |

그 밖의 충돌은 출처·적용 범위·사용자 결정 근거를 기록하고, 풀리지 않으면 `NEEDS_USER`로 멈춘다. 더 긴 문서·더 새 파일 시각·더 강한 AI 표현으로 우선순위를 정하지 않는다. 기존 루트 문서를 덮어쓰거나 이력을 지우지 않는다.

## 5 방식 비교와 순서

### 5.1 방식 비교

| 방식 | 장점 | 한계·판단 |
|---|---|---|
| Stop Hook 안에서 매번 Codex를 끝까지 실행 | 검수만 하는 작은 연결은 단순하다 | 긴 Unity 작업·재개·소유권·사용자 대기를 표현하기 어렵다. Hook 실패가 완료 판정을 대신한다. 채택하지 않음 |
| **로컬 중계기 + 짧은 Hook + Codex CLI + 제한된 Editor 연결** | Desktop 대화를 유지하며 작업 종류별 인계·상태·복구·권한을 명시할 수 있다 | 중계기와 Editor 연결 도구의 최초 구현·검증이 필요하다. **선택(Q-W1 시험 운영 승인)** |
| SDK/App Server 중심 별도 클라이언트 | 자체 대화 UI·세션·이벤트를 폭넓게 관리 | 지금 필요한 범위를 넘는다. 첫 연결에서 도입하지 않음 |

`codex exec`의 구조화 결과와 샌드박스 옵션은 [OpenAI 공식 문서](https://learn.chatgpt.com/docs/non-interactive-mode)에 근거한다. 제거된 `codex mcp-server`는 쓰지 않으며 App Server도 MCP의 대체 호출 형식이 아니다([공식 이전 안내](https://learn.chatgpt.com/docs/mcp-server)).

바뀐 것: 승인 전 판은 “사용자가 확정한 것은 역할·규칙 준수·설계 작성이며 기술 선택은 아직 승인 전”이었다 → 2026-10-24 Q-W1로 선택안을 시험 운영으로 승인했다.

### 5.2 쓸 수 있는 순서

- **문서:** Claude 초안 → Codex 문서 검수 → Claude 보완 → 사용자 리뷰.
- **코드와 화면:** 승인된 한 Phase 안에서 Claude C# → Codex 검수 → Claude 수정 → Codex View 적용 → Claude 문서 반영 → 사용자 Phase 리뷰.
- **화면부터:** 승인된 표시 변경 → Codex View 적용. 필요한 C# 계약이 없으면 쓰기를 멈추고 Claude 수정 요청 → 코드 검수 → Codex 재적용.
- **검수만:** 결과를 반환하고 끝낸다. 화면 적용 권한을 자동으로 주지 않는다.

화면 작업에서 새 기능·정책이 필요해지면 단순 C# 수정 요청으로 설계 변경을 우회하지 않는다. 기존 Phase가 포함하면 그 범위로 넘기고, 아니면 `NEEDS_USER`.

Phase가 끝날 때마다 Claude가 그 산출물의 Codex 검수를 요청하고 결과를 보고에 넣는다(Q-W6·D8).

### 5.3 변경 시나리오로 보는 독립성

| 변경 | 변경·이해 범위 | 필요한 검증 |
|---|---|---|
| 검수 기준 추가 | 원본 규칙/색인과 검수 프롬프트. Editor 명령은 안 건드림 | 새 출처 전달·누락·불일치 |
| 프리팹 참조 연결 명령 추가 | Editor 연결의 그 명령·허용 필드·결과 규격 | 대상·GUID·참조·반복 적용 |
| Codex 실행 수단·판 교체 | Codex 호출 어댑터·`config.json`·인증·결과 처리 | 요청/결과·실패·취소·중복. 게임 UI 전체 회귀를 관성으로 요구하지 않음 |
| 반환 경로 교체 | `hook`·`show`만 | 결과 도착·미인수 알림 |
| 개인 지시 변경 | 개인 규칙만 | 두 도구가 새 해시로 다시 확인 |
| 승인·편집권 계약 변경 | 중계기·모든 실행 입구 | 양쪽 쓰기·재개·Unity 저장을 포함한 통합 확인 |

공통 경계는 요청 식별자·정책 버전(`policyDigest`)·편집권·승인·결과 규격이다. 같은 상태를 여러 구성 요소가 따로 소유하지 않는다.

## 6 요청·결과·승인 계약

첫 구현은 파일 기반 구조화 요청과 결과다. 아래는 의미 계약이며 스키마의 필수 값·열거형은 각 Phase 계획이 정한다.

### 6.1 작업 요청

| 필드 | 뜻 |
|---|---|
| `workflowId`, `requestId`, `revision` | 작업 묶음·이번 요청·상태 버전. 같은 요청을 다른 작업으로 중복 실행하지 않는다 |
| `kind` | `REVIEW_DOCUMENT`, `REVIEW_CODE`, `APPLY_VIEW`, `REQUEST_CSHARP_CHANGE`, `REQUEST_DOCUMENT_UPDATE` |
| `projectRoot`, `checkoutIdentity` | 정규화한 절대 루트와 checkout 식별. 다른 복사본으로 자동 전환하지 않는다 |
| `designRef`, `planRef`, `phaseId` | 승인된 문서 경로·내용 해시와 현재 Phase. 문서 해시가 바뀌면 기존 승인을 다시 쓰지 않는다 |
| `approvalRef` | 원래 사용자 지시·승인 기록. AI가 채운 승인 문장은 인정하지 않는다(B의 읽기 전용 검수는 `read-only-review`, P6) |
| `policyDigest`, `ackRefs` | 적용 출처 버전과 두 도구의 확인 기록 |
| `allowedTargets`, `allowedOperations` | 정확한 파일·필드·작업 허용 목록. 디렉터리 전체를 기본값으로 삼지 않는다 |
| `baselineRef`, `inputDigest` | 편집을 인수한 시점의 상태와 검수할 정확한 입력 버전. HEAD와의 전체 diff로 남의 변경을 귀속시키지 않는다 |
| `completionCriteria`, `validationPolicy` | 판정 조건과 허용된 확인·사용자 확인 필요 사항 |
| `returnTo`, `attempt` | 같은 Desktop 세션으로 돌려줄 위치와 반복 횟수 |

### 6.2 결과와 검수 의견

모든 결과는 요청 ID·입력 해시·정책 해시·변경 파일·수행 작업·실제 확인 근거·미확인 사항·다음 담당을 가진다. 결과가 현재 입력과 다르면 `STALE_INPUT`이며 적용하지 않는다.

검수 항목은 중요도, 파일·위치, 관찰 사실, 적용 규칙과 출처, 영향, 수정 방향, 필요한 검증을 적는다. 근거가 없으면 질문·미확인으로 남기고 결함을 지어내지 않는다. 내부 추론 전문 대신 검증 가능한 근거를 넘긴다.

FW 평가는 프로그래머 가이드 §13의 `PASS / FAIL / WARNING / NEEDS DESIGN REVIEW`를 따른다. 이 판정, 자동화 요청의 실행 성공(`outcome`), 사용자 Phase 인수는 서로 다른 값이다. 안 한 검증이 있는데 전체 PASS를 선언하지 않는다.

Claude는 각 지적에 `수용 / 근거를 둔 반려 / 사용자 판단 필요`를 남긴다. Codex가 반려 근거를 확인하고 승인 범위 안에서만 수정·재검수를 잇는다. 한도는 **3회(설계 제안, C에서 확정)**이며 같은 지적이 풀리지 않거나 넘으면 사용자 판단으로 돌린다.

### 6.3 승인과 Phase 경계

승인 기록은 사용자가 실제로 허용한 설계·계획의 해시, Phase, 허용 대상, 예외와 지시 근거를 가리킨다. 처음에는 사용자가 확인하는 보수적 방식으로 구현한다. 다른 AI가 전한 “사용자가 승인했다”로 승인 상태를 만들지 않는다.

“진행해”는 이미 승인한 범위에만 적용한다. `현재 Phase 결과 보고`와 `다음 Phase 승인`은 다른 사건이다. 설계 승인으로 곧바로 게임 파일 쓰기나 패키지 설치를 시작하지 않는다.

### 6.4 상태

중계기의 **작업 루트별 현재 요청 상태 레코드 하나**(`state.json`)가 실행 상태의 원본이다. 로그·검수 JSON·Claude 답변은 상태를 덮는 원본이 아니다.

| 상태 | 뜻·다음 동작 |
|---|---|
| `ONBOARDING` | 출처·역할·현재 승인을 확인한다. 쓰기 금지 |
| `READY` | 확인과 이번 요청 범위가 준비됐다. 그 작업만 실행 가능 |
| `CLAUDE_WORKING` | Claude가 승인된 문서/C#을 쓴다. Codex 에셋 쓰기는 대기 |
| `CODEX_REVIEWING` | 입력 버전을 고정해 읽기 전용 검수. 양쪽 쓰기 대기 |
| `CLAUDE_REVISING` | 검수 의견을 판정·반영(B에서는 결과 도착·Claude 인수 대기). 바뀌면 재검수 |
| `CODEX_APPLYING_VIEW` | Codex가 승인된 에셋을 단독 편집. Claude 쓰기 대기 |
| `CLAUDE_SYNCING_DOCUMENTS` | Codex의 적용 근거로 문서 갱신 |
| `NEEDS_USER` | 승인·설계·영역·미해결 판단·오류 복구로 사용자 입력 필요. 자동 쓰기·다음 Phase 금지 |
| `PHASE_REPORTED` | 결과·미확인을 보고했다. 사용자 인수 전 새 Phase를 만들지 않음. Unity 인수 완료가 아니다 |
| `CANCELLED` | 취소 확정. 기존 수정은 보존하고 수행 범위를 보고 |

A·B는 `ONBOARDING`·`READY`·`CODEX_REVIEWING`·`CLAUDE_REVISING`·`NEEDS_USER`·`CANCELLED`만 쓴다(P5). 컴파일 미확인 등 실제 검증 상태는 별도 결과 칸이다.

## 7 Hook·반환 경로와 쓰기 보호

### 7.1 Hook의 책임

Desktop의 현재 Local 세션이 대화 입구다. Hook은 짧게 상태를 보고 필요한 규칙·결과·다음 호출을 현재 대화에 넘긴다. 긴 Codex 실행이나 Unity 편집 완료를 Hook 안에서 기다리지 않는다.

| 지점 | 동작 | Phase |
|---|---|---|
| 시작·재개·압축 뒤(SessionStart) | 프로젝트 일치·정책 버전·대기 요청 복원, Claude 확인 기록 필요 여부, 개인 규칙 대체 주입(P13) | A |
| 응답 종료(Stop) | 결과 도착·미인수면 요청 ID·시도별 **한 번만** 이어 가기 이유 반환 | B |
| 도구 실행 전(PreToolUse) | 편집권·허용 대상·승인 상태 대조, 범위 밖 요청 거절 | C·D |
| 명시적 workflow 호출 | 중계기에 제출·상태 조회·결과 인수(`relay` 하위 명령) | A·B |

**결과 반환의 1차 경로는 Claude가 실행한 백그라운드 명령의 종료 알림이다**(D5·P4). Stop의 `decision/reason`으로 같은 응답을 이어 갈 수 있지만 **Stop 실패·시간초과가 대화 종료를 반드시 막지는 않는다**([공식 Stop 계약](https://code.claude.com/docs/en/hooks#stop-decision-control), [실패 차단의 예외](https://code.claude.com/docs/en/hooks#block-the-action-when-a-hook-fails)). 그래서 Stop을 승인·완료 상태의 원본이나 유일한 강제 수단으로 쓰지 않는다. `stop_hook_active`만 보고 검수를 건너뛰지 않고 요청 ID·입력 버전·시도 횟수로 중복을 판별한다. 같은 메시지를 계속 돌려주는 무한 차단은 만들지 않는다. Hook의 실제 지원·재진입·시간 제한은 설치된 Desktop 판으로 확인한다([공식 Hook 문서](https://code.claude.com/docs/en/hooks)). 비동기 Hook만 붙여서 idle 세션이 자동으로 이어진다고 보장하지 않는다.

### 7.2 긴 작업·대기·복구

- 한 작업 루트에 단일 실행자를 둔다. 활성 Claude 세션은 짧은 상태 조회로 완료 결과를 받는다. 긴 프로세스의 stdout만이 결과 보관소가 되지 않게 한다.
- 요청은 저장 뒤 실행한다. 결과를 원자적으로 저장한 뒤 상태 버전을 올린다.
- 중복 제출은 기존 요청 상태를 돌려준다. 실패·시간초과를 검수 통과로 바꾸지 않는다.
- 실행자가 죽으면 잠금만 지우고 같은 에셋 작업을 재실행하지 않는다. 마지막 명령과 실제 에셋 상태를 대조해 재개를 판단한다. 잠금은 pid + 프로세스 시작 시각 + runId로 검사하며 시간만 지났다고 살아 있는 실행자의 편집권을 뺏지 않는다.
- Desktop/Unity가 닫히거나 사용자가 중단하면 쓰기를 멈추고 결과를 보존한다. 재개 때 같은 프로젝트·정책·파일 버전인지 다시 확인한다.
- 사용자가 대화를 끝낸 뒤 새 Desktop 턴을 자동으로 깨우지 않는다. 대기 결과는 다음 조회·재개에서 인수한다.
- 같은 Desktop 세션을 별도 `claude --resume`으로 동시에 열거나, 다른 Codex 앱 채팅에 자동 메시지를 보내지 않는다. 이 채팅의 기억이 CLI에 공유된다고 가정하지 않는다.

### 7.3 쓰기 범위와 다른 작업 보호

프롬프트·Hook만으로 완전한 파일 접근 통제를 보장하지 않는다. Hook·연결 프로세스의 권한과 모델 샌드박스는 다를 수 있다([Claude 공식 sandbox 범위](https://code.claude.com/docs/en/sandboxing#what-runs-outside-the-sandbox)).

1. 검수 Codex는 읽기 전용 실행이다. 부작용 있는 Editor 도구·Hook·MCP를 노출하지 않는다. 사용자 Codex 설정의 플러그인·MCP·`notify`가 들어오지 않게 `--ignore-user-config`와 기능 스위치로 격리한다(D2·P3). `--dangerously-bypass-approvals-and-sandbox`·`--dangerously-bypass-hook-trust`·`workspace-write`·`danger-full-access`는 쓰지 않는다.
2. View Codex의 일반 파일·셸 접근도 읽기 위주로 두고, 에셋 변경은 §8의 **허용 명령만 노출한 Editor 연결**로 한다. 넓은 workspace 쓰기와 지침만으로 C# 금지를 지켰다고 주장하지 않는다.
3. Editor 연결은 요청의 프로젝트·작업 ID·허용 에셋·필드·편집권을 다시 검사한다. 경로 정규화·프로젝트 밖 경로·링크 우회를 검사한다. 임의 코드·셸·파일 쓰기 명령은 노출하지 않는다.
4. Claude의 C#·문서 작성도 현재 Phase의 경로와 기존 소유권에 제한한다. 문서·설정·정책 파일을 고쳐 자기 승인 범위를 넓히지 않는다.
5. 저장 뒤 실제 diff·GUID·허용 필드와 예상 부수 에셋을 비교한다(예방 통제의 대체가 아님). 예상 밖 변경은 자동화를 멈추고 보고하며 남의 작업을 자동으로 되돌리지 않는다.

한 작업 루트의 쓰기 담당자는 한 시점에 하나다(Claude·Codex·Editor 저장 포함). 넘길 때 저장·해시·상태 버전을 확인한다. Unity의 미저장 씬·Prefab Stage가 있거나 허용 대상에 남의 편집이 있으면 덮어쓰지 않는다. 미커밋 변경은 자동 stash·reset·clean·삭제하지 않는다. `.cs.meta`는 C# 담당이, Codex가 만든 에셋·폴더의 `.meta`는 그 에셋 작업이 다룬다.

## 8 Unity Editor 적용 경로 (D 단계)

### 8.1 첫 연결의 선택안

게임 런타임과 분리된 **Editor 전용 제한 명령 연결**이다. 기존 프로젝트의 생성·연결 도구를 먼저 조사해 다시 쓰고, 없는 기능은 Claude가 Editor C#으로 구현하고 Codex가 검수한다. 요청·응답은 프로젝트 로컬 파일로 넘기고 Unity 객체 변경은 Editor 메인 스레드에서 한다. 모델이 만든 임의 C#을 실행하는 통로는 만들지 않는다.

씬·프리팹 작업은 [EditorSceneManager](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.html), [PrefabUtility](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.html) 등 정식 Editor API를 쓴다. 구체 API와 저장·Undo·dirty 처리는 D 계획에서 공식 문서를 확인해 정한다. YAML 문자열 치환을 기본 저장 경로로 삼지 않는다.

Unity CLI/Pipeline 설치는 선행 조건이 아니다. 설치·패키지 추가·임의 eval 허용으로 자동 전환하지 않으며, 새 연결 방식은 설계 변경으로 보고한다.

### 8.2 노출할 명령의 범위

| 종류 | 허용 범위 |
|---|---|
| 조회 | 프로젝트·씬·계층·컴포넌트·참조·에셋 GUID·미저장 상태·허용된 콘솔 내용 |
| 배치 | 승인된 기존 프리팹의 인스턴스 생성·부모·Transform/RectTransform·형제 순서·표시 값 |
| 에셋 | 승인된 프리팹 생성·수정과 저장. 경로·컴포넌트·필드·입력 프리팹 제한 |
| 연결 | 승인된 직렬화 필드와 View SO·머티리얼의 표현 값·참조. 임의 reflection으로 범위를 넓히지 않음 |
| 확인 | 정해진 씬/게임 뷰 캡처·참조 누락·배치 결과(실제 지원은 연결 시험에서 확인) |

새 스크립트나 공개 필드가 필요하면 `REQUEST_CSHARP_CHANGE`로 Claude에 넘긴다. 다른 필드·UnityEvent·공개 상태로 임시 우회하지 않고, 임시 C#·동적 코드·외부 생성기로 C# 작성자를 숨기지 않는다.

### 8.3 기존 UI·FW 규약

- 독립 개폐 화면·팝업·패널·HUD는 UIBase/UISystem 등록이 기본이다. UIId·부모·Policy·생성/등록/바인딩·개폐·재사용·씬 전환·해제를 함께 검수한다.
- 버튼 동작은 `UIInputBinding`/`IUIInputReceiver` 계약을 따른다. Inspector UnityEvent나 `onClick.AddListener`로 대신하지 않는다.
- Factory 생성/등록 → Group 보관·Export → 상위 Builder → 소비자 조회를 기록한다. 하위 UI·Render·Input을 루트에 다시 노출하지 않는다.
- SceneRoot는 씬 참조와 검증, 시스템은 기존 수명·서비스 계약을 따른다. Bootstrap에 기능을 몰아넣지 않는다.
- 필수 참조 누락은 성공이 아니다. 대상과 원인을 기록하고 코드 수정이 필요하면 Claude에 넘긴다.
- Missing Script·미참조 에셋을 정리 목적으로 지우지 않는다(기존 48개 포함). `.meta` GUID·프리팹 연결·참조를 인계 결과에 남긴다.

### 8.4 적용과 검증의 구분

Editor 연결이 응답했다는 것은 화면이 정상이라는 증거가 아니다. 에셋 저장, 참조 확인, 실제 캡처, 사용자 화면 인수를 나눠 기록한다. 자동 캡처가 안 되면 사용자 확인 필요로 남긴다. 컴파일·재생 확인은 사용자 담당이며, 연결 도구가 사용자의 Editor 컴파일을 대신하거나 Test Runner/Play를 자동 시작하지 않는다. Editor 도구 C#의 첫 가져오기·컴파일도 사용자 확인이 필요하다.

## 9 검수 기준과 반환

검수는 이번 변경과 관련 계약으로 한정한다. 공통 수명·공개 계약이 바뀌면 근거와 함께 넓힌다.

| 기준 | 기록할 근거 |
|---|---|
| SSOT | 기획값·Buffer 사실·Core 규칙·입력·표현 파생값의 원본과 복제 여부 |
| SOLID 5원칙 | 책임·확장 방식·계약 보장·필요한 인터페이스·구체 의존 |
| JMH/FW | MUST·권장·확장 규정을 구분하고 생성부터 소비까지 경로 제시 |
| 기능 독립성 | 기능 내부 변경/의존성 교체/공통 계약 변경별 수정 파일·이해할 코드·검증 동작, 남는 공통 경계 |
| 프로젝트 경계 | Core·다른 담당자·DB P1–P7·입력 경로·에셋/GUID 보존·코딩 규격 |
| 실행 증거 | 읽은 원본·실제 diff·실행 결과·미확인. 공식 문서와 관찰을 구분 |

Codex의 Unity 결과는 작업 요청별 구조화 기록이 원본이다. Claude는 관련 설계·계획의 실행 기록 자리에 결과와 링크를 반영하고 같은 상태를 별도 문서에 계속 복제하지 않는다. 사용자 보고에는 변경 이유·범위·수행 확인·미확인·다음 판단을 넣는다.

## 10 실패·중단·해제

- 규칙 누락·정책 충돌·승인 누락·보호 영역·C# 필요·입력 변경·편집 충돌은 실행하지 않고 담당자나 사용자에게 돌려준다.
- 시간초과·실행 오류·형식 오류·잘린 결과는 성공이 아니다. 진단을 남기고 같은 쓰기 작업을 자동 재시도하지 않는다.
- 중단 때 새 명령 발행을 먼저 막고, 수행 중인 Editor 명령이 저장 중인지 확인한 뒤 결과를 기록한다. 부분 변경은 보존·보고하고 자동 원복하지 않는다.
- 연결 해제는 이 프로젝트의 Hook·실행자·Editor 연결만 끈다. 다른 Hook·MCP·사용자 전역 설정이나 작업 에셋을 지우지 않는다.
- 상태 파일 손실·해시 불일치 때 새 승인을 만들어 복구하지 않는다. `NEEDS_USER`에서 실제 파일과 사용자 지시를 다시 확인한다.

## 11 Phase

| Phase | 목표와 종료 확인 | 지금 |
|---|---|---|
| A. 규칙·환경 확인 | Desktop 내장 판·Codex 인증·정책 색인·지침 차이·단독 편집 기준 확인. 두 도구가 같은 원본·역할을 반환하고, 누락·충돌 때 쓰기 금지 | 실행 중(계획) |
| B. 검수 연결 | 바뀌지 않는 문서 fixture로 Claude → Codex → 같은 Claude 세션 결과 전달. 누락·오류·시간초과·중복·정책/입력 변경·재개가 완료로 오인되지 않음 | 계획 있음, 미착수 |
| C. 수정·재검수 | 승인된 작은 문서 또는 허용된 C# 변경으로 수용/반려·3회 한도·Phase 종료 정지 확인. C# 예제·fixture도 Claude가 씀 | 다음 계획 |
| D. Unity 연결 | 기존 도구 조사 뒤 Claude가 제한 Editor 명령 구현, 사용자가 컴파일 확인. 승인된 소규모 UI/프리팹으로 Codex 적용·저장·참조·가능한 캡처·C# 변경 거절·복구 확인 | 다음 계획 |
| E. 실제 작업 인수 | 승인된 한 Phase에서 코드·검수·화면·문서 인계 확인, 보고·정지. 다음 Phase·커밋·push가 자동 실행되지 않음 | 다음 계획 |

연결 확인은 작은 문서 fixture 등 개발 도구 수준에서 먼저 한다. 전투 View에 새 `[Test]`를 만들거나 MSBuild를 돌리는 검증으로 바꾸지 않는다. 가짜 프로세스는 쓰지 않고 실제 실행으로 재현하지 못한 실패는 미확인으로 남긴다(D6). 예외는 사용자가 Codex에 직접 “돌려서 캡처하고 확인”을 요청한 경우다.

바뀐 것: 승인 전 판의 “초기 연결 확인은 가짜 프로세스 결과로 먼저” → 가짜 없이 미확인(Q-W5·D6).

## 12 인수 기준과 미확인

**설계 인수 기준**

- 양쪽 첫 실행이 기존 규격·규칙·승인 범위를 먼저 읽는 절차와 출처를 갖는다.
- Claude = 모든 C#·문서, Codex = 검수·Unity 표현 에셋이라는 역할과 반환 경로가 분명하다.
- 임의 순서의 요청을 지원하되 설계 → 계획 → Phase와 사용자 리뷰 지점을 건너뛰지 않는다.
- 읽기 전용 검수와 제한된 에셋 쓰기를 구분하고 Hook의 강제력 한계를 숨기지 않는다.
- 입력 버전·단독 편집·다른 작업의 미커밋 변경·실패·취소·재개·중복을 처리한다.
- SSOT·SOLID 5원칙·JMH/FW·기능 독립성의 검수 근거와 검증 제한을 보존한다.

**미확인(2026-10-24 Phase A 기록 기준):** Desktop이 실제로 Hook을 부르는지와 실행 셸, Hook 출력·`CLAUDE.local.md` 가져오기가 Desktop 세션 문맥에 들어오는지, Codex 읽기 전용 샌드박스의 저장소 읽기(첫 실행에서 막힘), Codex 앱 대화의 개인 규칙 인식, Editor 연결·캡처·저장·충돌 방지, 사용자 승인 입력의 실제 사용성. 이 문서는 그 기능의 구현 완료나 성공 증거가 아니다. 진행 결과는 계획이 원본이다.

**설계 작성 때 검토(2026-10-24):** 작성자와 분리된 구현 관점 검토에서 추가로 답할 중요한 질문은 없었다. 프로젝트 규칙 검토에서 역할·보호 영역·초기 규칙·전투 View 검증 제한·Phase 경계·SSOT/SOLID/FW·기능 독립성의 실질적 모순·누락을 찾지 못했다. 이는 설계 검토 기록이며 실제 연결의 통과 증거가 아니다.

**원칙 자체 점검(계획 작성 때, 2026-10-24):** 예상 약 8.6/10. SSOT — 개인 지시는 개인 규칙 하나, 공용 규칙은 원본 파일에만(해시는 실행마다 계산), 상태는 `state.json` 하나, 모델 설정은 사용자 Codex 설정, 한도는 `config.json`. 역할 분담이 이 설계 §4.1(근거)과 개인 규칙 §2(적용)에 있어 §4.1에 “지금 원본은 개인 규칙”을 적어 묶었다. 남는 위험: Hook·외부 가져오기 승인 창은 Desktop 내장 판에 기대고, 확인 기록은 이해를 보증하지 않으며, worktree 세션에는 `CLAUDE.local.md`·Hook이 없다.
