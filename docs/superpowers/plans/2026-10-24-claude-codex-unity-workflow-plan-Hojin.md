# Claude–Codex 자동화 연결 계획

- 작성자: 호진(이호진)
- 상태: Phase A 실행 중(2026-10-24 기록, 그 뒤 Phase A 진행 기록 없음). 2026-11-05에 Codex 실행 옵션을 바꾸고 직접 실행에서 관찰한 것을 §2 "알아 둘 것" 맨 앞 두 줄에 적었다. Codex 추가 호출 2회(확인 기록 재실행·Phase 끝 검수) 사용자 승인 대기. B는 A 보고 뒤 사용자 지시로.
- 설계: [Claude–Codex 자동화 연결 설계](../specs/2026-10-24-claude-codex-unity-workflow-design-Hojin.md). 결정(역할·Q-W1~Q-W8·D1~D9·P1~P13)과 파일 배치는 설계 §2.3·§3이 원본이다.
- 범위: 설계 §11의 **A 규칙·환경 확인**과 **B 검수 연결**. C 수정·재검수, D Unity 연결, E 실제 작업 인수는 B 인수 뒤 다음 계획이다.
- 정리: 2026-10-27 결정 표·환경 사실·파일 목록을 설계로 옮기고, 끝난 Task는 결과 요약으로 줄였다. 옛 판은 git `10914570`.

## 0 진행 규칙

- Phase 하나가 끝나면 Codex 검수(Q-W6)를 받고 보고한 뒤 멈춘다. “진행해”는 그 Phase의 Task만이다.
- 커밋·push는 사용자가 한다. 공용 `CLAUDE.md`·`AGENTS.md`는 바꾸지 않는다(Q-W2).
- A·B의 산출물에는 **C#·Unity 에셋이 없다.** 새 시험 코드·시험 도우미(가짜 Codex 포함)·MSBuild·EditMode 실행 요청은 하지 않는다(Q-W3·Q-W5). 확인은 도구를 실제로 실행한 시나리오 기록이고, 재현하지 못한 것은 “미확인”이다.
- 다른 작업의 미커밋 변경은 읽을 수 있지만 쓰지 않는다. stash·reset·clean 하지 않는다.
- A·B의 Codex 실행은 전부 `read-only`다. 위험 옵션은 쓰지 않는다(설계 §7.3).
- 실제 Codex 호출은 사용자 계정 사용량을 쓴다. Phase 표의 호출 횟수보다 많이 필요하면 멈추고 묻는다.
- Hook은 응답마다 도므로 할 일이 없을 때 프로세스를 새로 띄우지 않는다(P10).

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| 계획 작성·결정 | 1판 → 사용자 답(Q-W1~Q-W8, P1~P13 그대로) → 2판 | 완료(2026-10-24) | — | `83fb2767`(문서) |
| A1 | 설계에 D1~D9 반영 | 완료 | — | `83fb2767`(문서) |
| A2 | 개인 규칙·`CLAUDE.local.md`·`.git/info/exclude`·`~/.codex/AGENTS.md` 절 | 완료 | `git status --ignored`에서 둘 다 무시, 추적 파일 변경 0 | 저장소 밖(커밋 대상 아님) |
| A3~A6 | 색인·중계기(`sources`·`ack`·`status`·`hook session-start`)·`lib/` 넷·스키마·프롬프트·`config.json`·SessionStart Hook | 완료 | 스크립트 직접 실행으로 확인 | 저장소 밖 |
| A7 | 환경 확인 E1~E9 | 일부 | E4·E7·E8 정상, E5 일부, E6 막힘, E1~E3·E9 미확인 | — |
| A8 | 두 도구의 확인 기록 | Claude 완료, Codex **거절** | Codex가 원본을 못 읽어 `read=false` → READY로 안 넘어감 | — |
| A9 | 거절 확인(틀린 해시·없는 절) | 완료 | `ack` 거절·`sources` 실패·`NEEDS_USER` 확인 | — |
| A10 | Phase 끝 Codex 검수 | 대기 | 요청문만 준비 | — |
| A11 | 실행 기록·보고·멈춤 | 진행 중 | — | — |
| Phase B | 검수 연결(B1~B9, S1~S12) | 미착수 | — | — |

## 2 남은 일

**Phase A 마무리**

- [ ] **사용자 승인:** Codex 추가 호출 2회(계획의 3회에 더해 총 4회). 확인 기록 재실행에서도 읽기가 막히면 다시 멈추고 보고한다.
- [ ] E6 다시 확인: 사용자 설정의 `[windows] sandbox = "elevated"`를 옮겨 오도록 고친 뒤 Codex 읽기 전용 샌드박스가 저장소(공백 경로 포함)를 읽는지.
- [ ] A8 Codex 확인 기록 재실행 → 두 기록의 `policyDigest`·출처 목록·역할 경계 대조.
- [ ] A10 Phase 끝 검수(요청문 `state/…/requests/phase-a-review-20261024/prompt.md`). 지적마다 수용·반려·사용자 판단을 보고에 넣는다.
- [ ] E1~E3(사용자): 새 Desktop 세션을 열어 Hook이 실제로 불리는지·실행 셸·`CLAUDE_PROJECT_DIR`, Hook 출력이 시작·재개·압축 뒤 문맥에 들어오는지, `CLAUDE.local.md`와 외부 개인 규칙 승인 창이 뜨고 가져오기가 되는지. 압축은 다음 압축 때 관찰하고 못 보면 미확인.
- [ ] E9(사용자): Codex 앱에서 이 프로젝트를 열고 “개인 규칙 요약”을 요청해 개인 규칙을 읽는지.
- [ ] A 종료 확인 ①~⑤(아래)를 채워 보고하고 멈춘다.

**Phase A 종료 확인:** ① 두 확인 기록의 `policyDigest`·출처 목록·역할 경계가 같다 ② 절 누락·해시 불일치에서 거절한다(A9 완료) ③ E1~E3 결과가 기록됐다(Hook이 안 돌면 B의 Hook 부분은 `NEEDS_USER`, P4 반환 경로는 영향 없음) ④ `git status`의 추적 파일 목록이 착수 전과 같다 ⑤ Phase 끝 검수 결과가 보고에 있다.

**Phase B(미착수, A 보고 뒤 사용자 지시로)**

| Task | 담당 | 내용 |
|---|---|---|
| B1 | Claude | `submit`(종류 `REVIEW_DOCUMENT`·도구 스크립트 `REVIEW_CODE`, 대상·근거 문서·완료 조건, 착수 때 `git status` 요약과 대상 해시, Claude 확인 기록 필수, 중복 = 종류+대상+입력 해시+정책 해시가 같은 미종결 요청 → 기존 ID), `status`, `cancel`(프로세스 트리 종료 → `CANCELLED`, 결과 보존) |
| B2 | Claude | `run`: 잠금, 실행 직전 정책 해시 재계산(다르면 `POLICY_CHANGED`·실행 안 함), Codex 실행(P3·P9), 종료 코드, 결과 검사(P11), 실행 뒤 대상 해시 재계산(다르면 `STALE_INPUT`, 결과는 보존하되 현재 결과로 넘기지 않음), 결과 저장 뒤 상태 갱신 |
| B3 | Claude | 프롬프트 `review-document.md`·`review-tool-code.md`, `review-result.schema.json` |
| B4 | Claude | `show`, `accept`(인수 → `READY`). Stop Hook: 결과 도착·미인수면 요청 ID·시도별 한 번만 이어 가기(P10). SessionStart에 대기·실행 중·미인수 요청 복원 |
| B5 | Claude | `fixtures/review-fixture.md`(기대 지적은 실행 기록에만 적고 fixture에 넣지 않음) |
| B6 | Claude·사용자 | 시나리오 S1~S12 |
| B7 | Claude | 사용 안내 `docs/claude-codex-workflow-guide-Hojin.md`(개인 폴더) |
| B8 | Claude·Codex | Phase 끝 검수, 이번엔 연결로 |
| B9 | Claude | 실행 기록·보고·멈춤 |

| S | 상황 | 기대 | Codex 호출 |
|---|---|---|---|
| S1 | fixture 정상 검수 | 백그라운드 종료 알림 → 같은 세션에서 `show` → `accept` → `READY` | 1 |
| S2 | 같은 입력 다시 `submit` | 기존 요청 ID, 실행 없음 | 0 |
| S3 | S1 실행 중 같은 요청 `run` | 잠금으로 거절 | 0 |
| S4 | 실행 중 대상 사본 수정 | `STALE_INPUT`, 현재 결과로 넘기지 않음 | 1 |
| S5 | `submit` 뒤 `run` 전에 색인 대상 절 바뀜(색인 사본) | `POLICY_CHANGED`, 실행 없음 | 0 |
| S6 | 시간 한도 초과(짧은 한도) | `TIMEOUT`, 프로세스 트리 종료, 성공 아님 | 1 |
| S7 | 형식 오류·잘린 결과 | **미확인**(Q-W5). 오류가 나면 사용자가 Codex에 직접 확인 요청 가능 | 0 |
| S8 | 실행 오류(없는 실행 파일 경로) | `EXEC_FAILED` | 0 |
| S9 | 실행 중 `cancel` | `CANCELLED`, 남은 결과 보존 | 1 |
| S10 | 결과 도착 뒤 세션을 닫고 다시 열기 | SessionStart가 미인수 결과를 알리고 Claude 확인 기록을 다시 요구 | 0 |
| S11 | 미인수인 채 응답 끝 | Stop이 한 번만 이어 감, 같은 요청으로 다시 막지 않음 | 0 |
| S12 | Claude 확인 기록 없이 `submit` | 거절 | 0 |

- **Codex 호출:** 시나리오 4회 + Phase 끝 검수 1회 = 5회. 줄이려면 S6·S9를 미확인으로 돌릴 수 있다(각 1회).
- **B 종료 확인:** ① S1~S12 결과가 실행 기록에 있고 실패·시간초과·입력/정책 변경이 `OK`로 보고된 적 없다(S7은 미확인) ② 결과가 같은 Desktop 세션에 도착한다 ③ 저장소 추적 파일·전역 설정 변경 0 ④ Phase 끝 검수가 연결로 돌았다.

**알아 둘 것**

- [ ] 2026-11-05 바뀜: 중계기 Codex 실행에서 `--ephemeral`을 빼고(`lib/Codex.ps1`), `config.json` `configOverrides`에 `model_reasoning_summary=auto`를 더했다(설계 P3). 지금은 Codex 확인 기록 실행만 이 부품을 쓴다. Phase B의 검수 실행도 같은 옵션을 이어받는다. 같은 옵션으로 직접 돌린 검수에서 확인한 것: 세션 파일이 `~\.codex\sessions\2026\11\05\rollout-…-<thread_id>.jsonl`로 저장됐다(약 560 KB). 이벤트 로그에 `reasoning` 항목(짧은 요약 제목) 8개가 남았다. 데스크톱 앱 목록에 나타나는지는 미확인이다. 세션 파일이 쌓여도 `maxResultBytes`는 `result.txt`만 보므로 저장량은 제한하지 않는다(Codex 검수 낮음 지적에서 확인, Phase B 설계 때 정리할 것).
- [ ] 2026-11-05 관찰(E6 관련): 로비 LB-a 검수를 중계기와 같은 옵션(`windows.sandbox=elevated` 포함)으로 직접 돌렸더니, 읽기 전용 샌드박스가 저장소 밖의 개인 규칙 파일(`ProjectL-Hojin\rules\hojin-rules.md`)을 읽지 못하고 거절했다. 규칙 내용을 프롬프트에 붙여 다시 돌리자 검수가 됐다. A8 Codex 확인 기록이 거절된 원인도 이것일 수 있다(재현 확인 전).

- [ ] `CLAUDE.local.md`·Hook은 만든 checkout(`C:\Users\as010\Project_L`)에만 있다. Desktop이 별도 worktree로 세션을 열거나 다른 PC에서 열면 개인 규칙이 자동으로 들어가지 않는다. 2026-10-27에 다른 PC의 checkout(`C:\Users\Hyeon2-400\Project_L`)에는 `CLAUDE.local.md`·`.claude/`가 없음을 확인했다.
- [ ] 이 설계·계획은 공용 `docs/superpowers/`에 있어 커밋하면 다른 작업자도 볼 수 있다(자동으로 읽히지는 않음). 비공개로 두려면 개인 폴더로 옮길 수 있다(사용자 결정 대기).
- [ ] Codex 지침 크기: 저장소 `AGENTS.md`(25,908 B)와 개인 Codex 지침(1,037 B)의 합은 상한 32 KiB 안이다. Codex를 `Assets` 아래 폴더에서 열면 `Assets/AGENTS.md`까지 더해져 넘을 수 있다(미확인).
- [ ] 범위 밖(다음 계획): 게임 C# `REVIEW_CODE`, 지적별 수용·반려와 3회 한도의 자동 흐름(C), 쓰기 승인 입력·단독 편집권·PreToolUse(C·D), Codex View 적용·Unity Editor 제한 명령(D), 실제 Phase 인수(E).

## 3 작업별 결과

### 3.1 계획 작성·결정 (2026-10-24)

- 1판 → 사용자 답 → 2판. 1판에서 공용 `CLAUDE.md` 현재 상태에 넣었던 한 줄은 Q-W2에 따라 뺐다(공용 파일 변경 0). 설계 문서 이름에 `-Hojin`을 붙였다. 결정 내용은 설계 §3.

### 3.2 Phase A (2026-10-24, 진행 중)

- **Q-W8 확인:** `~/.codex/AGENTS.md`는 git 밖이고 권한은 as010·SYSTEM·Administrators·`CodexSandboxUsers`(읽기)뿐이며 이 PC의 사람 계정은 as010 하나다. 공식 문서상 이 사용자의 Codex만 읽는 전역 개인 지침이다 → 원본을 개인 폴더 `backup/`에 복사하고 Project_L 조건 절을 덧붙였다.
- **A2:** 개인 규칙 `rules/hojin-rules.md`(§1~§7), `CLAUDE.local.md`(`@Assets/AGENTS.md`, `@~/ProjectL-Hojin/rules/hojin-rules.md`), `.git/info/exclude` 두 줄.
- **A3~A6:** `sources.json`(프로필 셋), `relay.ps1`, `lib/` 넷, `ack.schema.json`, `prompts/ack.md`, `config.json`, `.claude/settings.local.json`(SessionStart, 매처 `startup|resume|clear|compact`). `onboarding` 정책 해시 `b554beba9b33…`, `review-document` 프로필도 절 추출 정상.
- **A8:** Claude 확인 기록 저장(정책 `b554beba9b33…`). Codex 확인 기록 1차는 원본을 하나도 열지 못해 `read=false` → 중계기가 READY로 넘기지 않았다(E6).
- **A9:** 틀린 해시 → `ack claude` 거절(종료 2, “해시가 다릅니다: assets-agents-md”). 없는 절 제목 색인 사본 → `sources` 실패(종료 1), `status`는 `NEEDS_USER`(“규칙 원본을 읽지 못했습니다”), 원래 색인으로 `ONBOARDING` 복귀. 사본은 지웠다. A8의 Codex 거절도 “못 읽으면 READY 아님”의 실제 사례다.

**환경 확인 결과**

| E | 확인 | 결과 |
|---|---|---|
| E1 | Hook 실행 셸, `CLAUDE_PROJECT_DIR` 전달 | 스크립트 직접 실행으로 문맥 출력·진단 파일·한국어(ASCII 이스케이프) 확인. **Desktop이 실제로 Hook을 부르는지와 실행 셸은 미확인**(새 세션 필요) |
| E2·E3 | Hook 출력·`CLAUDE.local.md` 가져오기가 Desktop 문맥에 들어오는가 | 미확인(사용자가 새 세션을 열어야 함) |
| E4 | Codex 찾기·버전·로그인 | `codex-cli 0.164.0-alpha.11.1`(`bin\8925276705115dea\`), ChatGPT 로그인 |
| E5 | `--ignore-user-config`에 플러그인·MCP가 없는가 | 1차: 계정 연결 앱(github·gmail·google·linear 등)·웹 검색·이미지 생성·하위 에이전트가 남음 → 기능 스위치로 끔. 2차: 앱·웹·이미지 없음. 셸·파일 편집(읽기 전용 제한)·이미지 보기·MCP 리소스 읽기·goal·clock·**하위 에이전트 도구는 남음**(끄는 스위치를 못 찾아 프롬프트로 금지) |
| E6 | 읽기 전용 샌드박스에서 저장소 읽기, `~/.codex/AGENTS.md` 읽힘 | **두 실행 모두 셸 읽기 명령이 전부 “blocked by policy”**(`cmd /c type` 포함). 추정 원인: `[windows] sandbox = "elevated"`가 `--ignore-user-config`로 빠짐 → 옮겨 오도록 고쳤으나 Codex 실행으로는 아직 확인 안 함 |
| E7 | 한국어 프롬프트·결과 왕복(PS 5.1 표준입력) | 정상 |
| E8 | `--output-schema` 엄격 스키마 | 정상(확인 기록 JSON이 스키마대로 옴) |
| E9 | Codex 앱 대화의 개인 규칙 인식 | `--ignore-user-config` 실행에도 개인 Codex 지침의 Project_L 절과 저장소 `AGENTS.md`가 들어옴(환경 확인 답). 앱 대화 확인은 사용자 몫, 미확인 |

**계획과 다른 점**

1. `.ps1`은 BOM 있는 UTF-8(P1 보충, 설계 §3.3). PS 5.1이 BOM 없는 스크립트를 ANSI로 읽어 한국어 문자열에서 구문 오류가 났다.
2. Codex 격리 인자 추가(P3 보충, 설계 §3.3): 기능 스위치 10개, `web_search=disabled`, `approval_policy=never`, `windows.sandbox` 옮겨 오기. 기능 스위치 10개는 `codex features list`로 모두 꺼짐을 확인했다.
3. `config.example.json` 생략. 개인 폴더라 공유 견본이 필요 없고 같은 값 두 벌을 피한다.
4. 첫 Hook 시험에서 원자적 쓰기의 `[IO.File]::Replace`에 `$null`이 빈 문자열로 넘어가는 문제를 찾아 `[NullString]::Value`로 고쳤다.

**Codex 호출:** 2회 사용(환경 확인 45.7초, 확인 기록 121초). 계획의 3회 안에서는 확인 기록 재실행과 Phase 끝 검수를 둘 다 할 수 없어 2회 추가(총 4회) 승인 대기다.

### 3.3 Phase B

미착수. 할 일은 §2.

## 4 공유 메모

없다. 이 연결은 사용자 개인 도구이며 공용 지침·다른 작업자 파일을 바꾸지 않는다(Q-W2). 전역 설정(`~/.claude/settings.json`·`~/.codex/config.toml`)도 바꾸지 않았다.
