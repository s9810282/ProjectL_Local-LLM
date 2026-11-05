# 전투 스테이지 편집 SO 모드 계획

- 작성자: 호진(이호진)
- 낙석 터널 폭발 옵션(2026-11-02): 사용자 승인 범위의 **§17 RTE-a 코드·문서 반영, 컴파일·Unity 미확인, 커밋 전**. 설계 §24에 따라 스테이지별 View 설정·편집/저장/검사·전투 연결을 추가했다. 고지대 B1은 계속 보류다.
- 기믹 모양 UX(2026-11-02): 사용자 “이 설계대로 계획 문서에 반영하고 구현도 진행” 승인으로 **§16 GSE-a 코드·문서 반영, 컴파일·Unity 미확인, 커밋 전**. 자동 위치·기존 값/기능·고지대 B1 보류를 유지하며 설계 §23을 따른다.
- 하붑 편집(2026-11-02): **§15 HBE-a 코드·문서 반영, 컴파일·Unity 미확인, 커밋 전**. 사용자 설계 승인 범위를 구현했으며 실행 기록은 §15.4다. 고지대 B1 보류와 UXR-c/d 미착수 상태를 유지한다.
- 시안 적용 계획·구현(2026-11-02): **UXR-a·UXR-b와 선행 UX-c 코드 반영, 컴파일·Unity 미확인. UXR-c·UXR-d 미착수.** 설계 §21이 기능 보존 원본이며 UXR 기록은 §14.8, UX-c 기록은 §12.9다. 고지대 B1 보류를 유지한다.
- UX 후속(2026-11-02): **UX-a~UX-c 코드·관련 안내 문서 반영, 컴파일·Unity 미확인**(§12). 고지대 보류 유지.
- 최신 구현 상태(2026-10-31): **FU-a·FU-b 코드 반영, 컴파일·Unity 확인 미완료. FU-c 안내·문서 반영, 고지대 B1은 보류.** 아래 문서 작성 당시의 미착수 기록과 구분한다.
- 상태: SO-a~SO-i·SO-e `48e7b533`, MG `86b447cd`, AZ `ac4a8101`, 안내 줄 삭제 `abbdea4f`, UT `ede23986a`(공유 문서 후속 `f5c8109d4`), TT·TO `f5c8109d4`. 기존 Unity 확인 기록은 §1·§2.1을 따른다. **2026-10-31 후속 보완 FU-a·FU-b 코드는 반영 완료, FU-c 안내·문서도 반영 완료(§11). 컴파일·Unity 미확인. 고지대 B1은 사용자 지시로 보류.** 공유 메모 넷은 미전달이다.
- 설계: [전투 스테이지 편집 SO 모드 설계](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md)
- 정리: 2026-10-27 `2026-10-27-combat-stage-editor-so-mode-plan.md`(SO-a~SO-e, P1~P12)·`2026-10-27-combat-stage-editor-so-mode-additions-plan.md`(SO-f~SO-i·SO-e, P-A1~P-A15)·`specs/2026-10-27-combat-stage-editor-so-mode-merge-inventory.md`(병합 대응 목록)를 이 문서로 합쳤다. 끝난 Task 단계·완료 조건 원문은 git `10914570`. 2026-10-29 다시 줄임: MG·AZ의 Task·체크박스·실행 기록·Unity 확인 목록(Ua·Ub)은 git `a8612321e`.

## 1 진행 요약

모든 코드 작업은 MSBuild 없이·새 시험 없이 했다(Q-SM7 A). "확인"은 사용자 Unity 확인이다.

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| SO-a 열기·보기 | 모드 전환, 스테이지 목록, 작업본 읽기, 2D 격자, 패널(읽기만), 검사 | 완료 | 확인 | `48e7b533` |
| SO-b 편집·저장 | 층별 도구, 패널 편집, 복제본 검사 뒤 쓰기, 초안 | 완료 | 확인 | `48e7b533` |
| 정의 목록 한국어 이름 | View DB 외형 표 `displayName` | 완료 | 확인 | `48e7b533` |
| SO-c 만들기·등록 | 새로 만들기·복제·짝 View 만들기·표 등록/해제 | 완료 | 확인 | `48e7b533` |
| 패널 너비 | 양쪽 패널 경계 끌기 | 완료 | 확인 | `48e7b533` |
| SO-d 공유 SO | 공유 사용처, 전용 복제, 이 스테이지만 쓰는 SO 바로 고치기, 새 필드 | 완료 | 확인 | `48e7b533` |
| SO-f 끌어 옮기기 | 메인 캐릭터 층 제거, 상자 선택, 끌어 옮기기, 단축키 | 완료 | 확인 | `48e7b533` |
| SO-g 상자 편집 | 사각형 칠하기, 복사/붙여넣기, 좌우 대칭, 묶음 옮기기 | 완료 | 확인 | `48e7b533` |
| SO-h 범위 미리보기 | Core 모양 함수, 고른 적·모든 적, 방향 버튼 | 완료 | **확인 필요**(§4.9) | `48e7b533` |
| SO-i 3D 보기 | 3D 미리보기, 외형 표 읽기, 배치 식 사본 | 완료 | 대기 | `48e7b533` |
| 적·아군 구분 | 적 자격 사본으로 정의 목록 나눔 | 완료 | 대기 | `48e7b533` |
| 진영 줄 수 | 스테이지별 아군·적 줄 수 슬라이더(적 쪽은 AZ-a에서 걷음) | 완료 | 대기 | `48e7b533` |
| SO-e 문서·공유 | 기획자용 사용법, 공유 메모 넷, 설계 구현 메모 | 완료 | — | `48e7b533` |
| MG-a 이웃 모양 외형 회전(A1) | 모양 지형 판정, 패널 읽기 줄·지우기, 정의 변경 때 지움, 저장 검사 | 완료 | 확인 | `86b447cd` |
| MG-b 맵 생성 규칙(A2) | 표·맵 SO 읽기, Core 계약 판정, 등록·빼기·저장·검사 흐름, 보스 교체, 맵 생성 줄 | 완료 | 확인 | `86b447cd` |
| MG-c 문서·메모 | 사용법, 공유 메모 넷, 설계 구현 메모 | 완료 | — | `86b447cd` |
| AZ-a 아군 구역 맞추기 | 칸 목록 읽기·쓰기, 구역 해석, 색·범례·이름, 적 슬라이더 걷기, 경고 ①, 새 필드, 맵 밖 | 완료 | 확인(2026-10-28) | `ac4a8101` |
| AZ-b 아군 칸 붓·경고 | 구역 도구, 칠하기/지우기, 칸 목록 비우기, 참고 수·경고 ②~④ | 완료 | 확인(2026-10-28) | `ac4a8101` |
| AZ-c 문서·메모 | 사용법, 공유 메모 넷, 설계 구현 메모 | 완료 | — | `ac4a8101` |
| 안내 줄 삭제(2026-10-29) | 병합 `a8612321e`로 전투 View PV-a가 칸 구역을 그려, 도구 안내 상자·사용법·전투 View 공유 §7(병합 전 §6)을 고침 | 완료 | 컴파일 미확인 | `abbdea4f` |
| UT-a 값·패널 | 배경 방식·바닥 타일 값 읽기·쓰기, View 패널, 초안·창 작업본 형식 번호, 3D 배경 고르기(§8.1) | 완료(2026-10-29) | 확인(Ua, 2026-10-29) | `ede23986a` |
| UT-b 타일 보기·2D 띠 | '타일' 보기·붓·기준 타일, 모드·맞춤 알리기, 2D 칸 띠·고른 칸 줄, 명시적 새로고침(§8.2) | 완료(2026-10-29) | 확인(Ub, 2026-10-29) | `ede23986a` |
| UT-c 3D 타일·높이 | 타일 놓기·맞춤 사본, 타일·지형 방식 높이, 빈 구멍·숨김 복원·교체 외형 좌표계·3D 새로고침(§8.3) | 완료(2026-10-29) | 확인(Uc, 2026-10-29) | `ede23986a` |
| UT-d 문서·공유 | 사용법, 공유 메모 셋, 구현 메모(§8.4) | 완료(2026-10-29) | — | `ede23986a`·`f5c8109d4` |
| TT-a 게임 데이터·놓기 | 용근님 두 파일: 회전 칸·검사·타일 회전 한 줄(§9.1) | 완료(2026-10-29) | 확인(Ua, 2026-10-29) | `f5c8109d4` |
| TT-b '타일' 보기 돌리기 | 작업본 회전, 화면 방향, 고르기·돌리기·표지(§9.2) | 완료(2026-10-29) | 확인(Ub, 2026-10-29) | `f5c8109d4` |
| TT-c 2D·3D 도구·3D 회전 | '타일 회전' 도구, 칸 → 타일 고르기, 3D 회전(§9.3) | 완료(2026-10-29) | 확인(Uc, 2026-10-29) | `f5c8109d4` |
| TT-d 문서·공유 | 사용법, 전투 View 사본 공유, 구현 메모(§9.4) | 완료(2026-10-29) | — | `f5c8109d4` |
| TO-a 3D 판 밖 타일 고르기 | 판 밖 칸 → 타일, 3D 클릭·사각형·테두리·Ctrl+A(§10.1) | 완료(2026-10-29) | 확인(Ua, 2026-10-29) | `f5c8109d4` |
| TO-b 문서 | 사용법·구현 메모(§10.2) | 완료(2026-10-29) | — | `f5c8109d4` |
| FU-a 배치 검사 | 벽·낭떠러지와 고정 적·구조물·본부의 점유 칸 검사(§11.1) | 반영 완료(2026-10-31) | 미확인(Ua) | — |
| FU-b 3D 표시 | 토네이도 위치·높이, 전체 배치/Player 기준(§11.2) | 반영 완료(2026-10-31) | 미확인(Ub) | — |
| FU-c 안내·문서 | 기본 공격 범위 한계, 사용법·사본 공유(§11.3) | 반영 완료(2026-10-31) | 미확인(Uc) | — |
| B1 고지대 이웃 모양 | 기존 기능 유지, 이번 구현 Task 없음 | 사용자 보류(2026-10-31) | — | — |
| 최근 편집 기능 | 본부 삭제·타일 선택 복사/붙여넣기·정의 끌어 놓기(설계 §18~§20) | 기존 코드 반영(2026-11-02), 이번 문서 수정과 구분 | 컴파일·Unity 미확인 | — |
| UXR 시안 적용 | 배치/바닥 타일/설정 재구성·기능 보존·사용법(§14) | UXR-a·b 코드 반영(§14.7~14.8), c·d 미착수 | 컴파일 미확인·Ua/Ub 대기 | 미커밋 |

`48e7b533` 뒤에 용근님이 `c40fd37b`(2026-10-27)에서 도구 두 곳을 고쳤다(레일 필드 이름·이름표, [설계 §9](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#9-다른-작업자-영향)).

## 2 남은 일

### 2.1 Unity 확인 (사용자)

SO-h(사용자가 결과를 말하지 않고 SO-i를 지시함):

- [ ] 사각형 모양 적을 고르면 그 사거리 사각형이 칠해진다.
- [ ] 부채꼴 적(보스 포효 지정 방식 등)이면 "전체"는 네 방향 합친 모양, 방향 버튼은 그 방향만.
- [ ] 스플래시가 있는 적(포격형)은 옅은 칸이 더 보인다.
- [ ] 겹쳐 보기에서 겹친 수 `×n`이 보인다.

SO-i:

- [ ] `Combat_Normal_A`를 3D로 열면 맵 프리팹 위 칸이 게임과 같은 자리에 보이고, 적·본부가 전투 프리팹으로 서 있다.
- [ ] 3D에서 칸을 눌러 놓기·옮기기·지우기가 되고 2D와 결과가 같다.
- [ ] 지형 외형과 고지대 높이가 보인다.
- [ ] 창을 닫았다 열거나 스테이지를 바꿔도 미리보기 오브젝트가 남지 않는다(Hierarchy·씬 오염 없음).
- [ ] 선택 도구 Shift+끌기는 배치가 있는 칸부터 시작해도 상자가 만들어진다.
- [ ] 3D에서 범위·상자·고스트가 칸 위에 보인다.
- [ ] 3D를 연 채 스크립트를 다시 컴파일하거나 창을 닫아도 "not cleaned up" 같은 경고가 없다.

적·아군 구분:

- [ ] 정의 목록이 "적 유닛"과 회색 "아군 유닛"으로 나뉜다(당시 5·9개, 지금 정의 수는 다를 수 있음).
- [ ] 적 유닛을 골라 놓고 바꾸기가 그대로 된다.

아군 줄 수(전용 복제 확인 창·아군 구역 색·경고 ①은 AZ-a에서 확인함):

- [ ] 공유 필드 스테이지에서 아군 줄 수를 바꿔 전용 복제한 뒤 저장하면, 그 스테이지만 새 필드(`<원본 필드>_<스테이지>`)를 쓰고 다른 스테이지는 그대로다.
- [ ] 전용 필드에서 다시 바꾸면 확인 창 없이 바뀌고 Undo가 된다.
- [ ] 칸 목록이 빈 스테이지를 바꾼 줄 수로 전투에 들어가면, 아군 색과 아군 배치 가능 칸이 바뀐 줄 수를 따른다.

2026-10-29 안내 줄 삭제:

- [ ] 컴파일된다. 칸 목록이 있는 스테이지의 '아군 배치 구역' 묶음에 읽기 줄과 '칸 목록 비우기'만 보인다.
- [ ] 칸을 칠한 스테이지로 전투에 들어가면 게임 화면이 칠한 칸에 아군 구역 색을 그린다(전투 View PV-a).

<a id="22-병합-대응-목록-구현-전-사용자가-고름"></a>
### 2.2 병합 대응 목록

SO 모드 커밋 `48e7b533` 뒤 병합된 전투 변경을 HEAD `10914570`까지 대조한 최초 목록이다. **2026-10-31 로컬 HEAD `bd0447662`에서 재확인해 C1을 정정하고 B2·B4·C1·D3~D5를 §11의 후속 설계·계획으로 옮겼다. B1은 보류이며, 문서 갱신을 구현 승인으로 보지 않는다.**

대조한 커밋: 용근님 `6fe71541`(고지대 이웃 모양)·`24002ca0`(낭떠러지)·`2805717b`(약한 지반, 새 스테이지 B·C)·`864a9d89`(스테이지 레벨, D~K 삭제, 새 필드)·`6bcc4c91`·`1eed25ca`(넉백 미리보기, 투사체 완급)·`c40fd37b`(적 공용 행동 비용, 레일 진영 턴), 재령님 `ba9b05fe`(렙디), 천명님 `ccaf47c8`(맵 가중치)·`35872a45`·`1fa0082c`(넉백), 호진 `1ab12e55`(토네이도 연출).

**끝난 것**

| 항목 | 내용 | 결과 |
|---|---|---|
| A1 | 이웃 모양 지형(고지대)에 외형 회전이 있으면 전투 화면이 멈춤 → 도구가 막음 | MG-a(§6.1), 설계 §10 |
| A2 | 표 등록·빼기·맵 키/역할 저장 때 맵 생성 규칙(보스 하나, 출현 종류별 후보) 검사 | MG-b(§6.2), 설계 §10 |
| C2 | 진영 안 배치 가능 칸 수 | AZ-b 참고 수·경고 ②~④가 대신함(§7.2) |
| D1 | 사용법 레일 단위 "진영 종료 횟수"(§3.3·§7.4·§8.2), §8.4 맵 생성 규칙·보스 바꾸기 | 2026-10-27 정리·MG-c |
| D2 | 호진 공유 §4 "Test 스테이지가 Root 표에 등록돼 있음"이 틀림 | 메모 §4에 정정 줄(2026-10-27) |
| E1 | 진영 칸 단위 Core 답 대기 | 천명님 `5a1bfbe1f`로 풀림 → AZ(§7), 설계 §11 |

**남은 것**

- [ ] **B1 고지대 이웃 모양(보류).** 게임은 같은 종류 이웃으로 모양·회전을 고르지만 도구는 Center R0이다. **2026-10-31 사용자 요청으로 이번 Task에서 제외한다.** 기존 고지대 정의·기본 외형·높이·외형 회전 금지는 유지한다. 삭제나 Core 규칙 변경을 뜻하지 않는다. 이웃 모양 보완은 보류 해제 후 별도 설계한다.
- [x] **B2 토네이도 외형 2×2(3D) → FU3/FU-b.** +X·+Z 반 칸 이동뿐 아니라, 앵커 기준 2×2 중 판 안 칸의 원래 타일 바탕 최대 높이를 적용한다. 기존 배율을 다시 곱하지 않는다. 2D·저장 앵커는 유지한다. (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)
- [ ] **B3 기존 Unity 확인 잔여:** 프리팹 없는 낭떠러지의 타일 숨김·약한 지반 교체 외형은 UT-c Uc.6·Uc.9의 기존 사용자 확인 기록을 따른다. SO-i 전체 잔여 확인은 §2.1에 남기며, 이번 문서 갱신으로 추가 통과 처리하지 않는다. 고지대 이웃 모양은 B1 보류다.
- [x] **B4 공개 대상 기준(3D) → FU4·FU5/FU-b.** 전체 배치(기본)/Player 기준을 구분하고 지형 높이·바닥 교체·타일 숨김에 같은 표시 집합을 쓴다. 2D·배치 목록은 전체 유지. 설계 §15의 UI를 FU-b 구현 지시에 따라 반영했다. Unity 사용자 확인은 별도다. (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)
- [x] **C1 벽·낭떠러지 초기 배치 검사 → FU1·FU2/FU-a.** `CellEnvironmentQuery.ValidateInitialPlacement`는 낭떠러지 위 살아 있는 캐릭터·구조물을 거절한다. 기존 ‘낭떠러지 위 고정 적 허용·경고만’ 설명을 폐기한다. 저장 전에 고정 적·적 구조물·본부의 회전된 점유 칸을 검사해 둘 다 오류로 처리한다. 기존 SO 검사의 벽/환경·오브젝트·장애물 검사는 유지한다. (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)
- [ ] **C3(선택, 이번 범위 밖) 약한 지반 진입 한도.** 2D 칸의 한도 숫자 표시. 별도 요청 전에는 Task에 넣지 않는다.
- [x] **D3 전투 View 사본 메모 → FU-c.** 토네이도 위치·원래 바탕 높이, 공개 대상 필터·높이·숨김 기준을 사본 목록에 기록한다. B1은 보류라고 적는다. (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)
- [x] **D4 민형님 공유 메모 → FU-c.** 용근님 `c40fd37b`의 도구 두 곳 변경과 새 도구 내부 검사 책임을 기존 메모에 보완한다(당시 레일 변경 설명 누락, 반영 시 중복 확인). (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)
- [x] **D5 공격 범위 안내 → FU6/FU-c.** SO 기본 사거리·모양이며 특성·버프/디버프 사거리 보정을 포함하지 않는다고 명시한다. 실제 전투 계산을 추가하지 않는다. (반영 완료, Unity 확인은 §11의 Ua/Ub/Uc와 구분.)

**확인했지만 넣지 않을 것**

| 변경 | 까닭 |
|---|---|
| 적 공용 행동 비용 8(`EnemyTurnRules.MaximumActionCost`) | 상수이고 스테이지 SO 값이 아니다 |
| 레일 진영 턴 단위 | 도구 코드는 용근님이 이미 맞췄다(문서만 D1) |
| 토네이도 무작위 시작 | 기존 토글·규칙(토네이도만) 그대로 |
| 새 필드 `Field_13x13_P3E9`·`Field_15x15_P4E9` | 아군 줄 수 도구가 다룬다(적 줄 수는 Core가 읽지 않고, 깊이 합 규칙은 `5a1bfbe1f`로 없어졌다) |
| 맵 출현 가중치 편집 | 맵 SO(StageMap 영역) 값. A2에서 읽기만 |
| 넉백 미리보기·아군 넉백·투사체 완급·Ballista 종류 걷기·추락·토네이도 연출 SO | 전투 중 연출이지 스테이지 데이터가 아니다 |
| 스테이지 D~K 삭제·적 로드아웃 비움 | 데이터 변경일 뿐. 도구는 "(없음)" 참조를 이미 다룬다 |

후속 순서: **FU-a 배치 검사 → FU-b 3D 표시 → FU-c 안내·문서**(§11). 구현을 요청받은 뒤 Phase마다 멈추고 보고한다. B1은 보류, C3은 이번 범위 밖이다.

### 2.3 그 밖

- [ ] 공유 메모 넷 전달(사용자, §5).
- [ ] Core·DB 담당 보고(Core 전달 문서 §42에 적음): `mainCharacterAnchor`·`MainCharacterEntrySpec` 미사용, SO 툴팁 "Player 배치 구역 안이어야 합니다"가 낡음, PZ-d 때 메인 캐릭터 필드를 지우면 도구 열기가 멈춤(§42.4).
- [x] 기존 커밋 확인(2026-10-31 로컬 이력): UT 코드·사용법·설계·계획은 `ede23986a`, 공유 문서 후속은 `f5c8109d4`. 안내 줄 삭제는 `abbdea4f`. 이전의 ‘커밋 전’ 표시는 정정한다. 이번 작업에서 커밋하지 않았다.
- [x] 기존 커밋 확인: TT·TO 코드·문서는 `f5c8109d4`. 게임 파일 두 개의 TT 변경도 이 이력에 포함되며, 새 후속 FU 작업의 완료를 뜻하지 않는다.
- 앞선 커밋: 새 `.cs` 여덟과 `SO모드 사용법.md`는 `.meta`와 함께 `48e7b533`, 새 `CombatStageSoChapter.cs`(+ `.meta`)는 `86b447cd`, AZ 도구 넷·문서는 `ac4a8101`. 시험용 에셋의 ‘미커밋’은 당시 기록이며 현재 모든 시험용 에셋의 상태로 일반화하지 않는다. 이번 문서 작업은 기존 에셋 변경을 건드리지 않는다.
- 작업 브랜치는 따로 정하지 않고 `feature/Hojin/Refactor-StageSelect`에서 했다.

## 3 진행 규칙과 계획에서 정한 것

### 3.1 규칙

- Phase마다 멈추고 보고했다. 순서는 SO-a → SO-d, 이어서 SO-f → SO-i, 마지막에 SO-e(Q-AD6 A). 그 뒤 MG-a → MG-c, AZ-a → AZ-c.
- 검증은 Q-SM7 A: MSBuild 안 돌림, 새 `[Test]` 없음, Unity 확인은 사용자, 보고에 "컴파일 미확인". 검토 에이전트는 돌리지 않았다(P-MG6·P-AZT8).
- DT 모드 코드·동작, 기존 도구 시험(`Tests/EditMode/Tool/`, 민형님), DB SO 코드, 게임 런타임은 바꾸지 않는다.
- Unity 확인용 편집은 운영 스테이지가 아니라 `Test`나 새로 만든 스테이지로 한다.

### 3.2 P1~P12

SO-a~SO-d에 쓴 것이다. 2026-10-27 사용자 "P1~P12 그대로 하고 SO-a 진행해".

| ID | 정한 것 | 구현에서 바뀐 것 |
|---|---|---|
| P1 | 적 유닛·본부·메인 캐릭터 칸 규칙은 도구가 다시 쓰지 않는다. 배치 구역은 색으로만 보이고, 오류는 PlacedStage로 전투에 들어가 확인한다 | 최초 구현 기준. 2026-10-31 후속 설계 FU1·FU2에서 벽/낭떠러지와 고정 배치의 겹침 두 조건만 저장 전 검사로 추가 예정. 나머지 Core 진입 검사는 그대로 |
| P2 | 새로 만들기 = 틀 스테이지(기본: 등록된 첫 Regular 전투)의 참조·View 값을 이어받고 배치를 비운다. 복제는 배치까지 | — |
| P3 | 스테이지 키는 틀 키의 백 단위 안 빈 값을 제안(일반 11xx, 보스 21xx). 모든 Root와 겹치면 거절 | — |
| P4 | 공유 수 1인 SO는 바로 고친다. 정의 SO와 카메라 설정은 읽기만 | 조건을 "이 스테이지를 뺀 사용처 0"으로(SO-d) |
| P5 | 등록할 표는 이름이 아니라 찾아서 정한다. Group은 바꾸지 않는다 | 형식으로만 찾고, 같은 형식 표가 여럿이면 거절(SO-c) |
| P6 | 새 에셋은 원본과 같은 폴더. 스테이지 셋 `Stage_`·`Combat_`·`StageViewProp_<이름>`, 공유 복제 `<원본>_<스테이지 이름>`. 같은 이름이면 만들지 않음 | — |
| P7 | sourceId = 이 스테이지 지형·오브젝트(숨김 호환 포함) 최댓값 + 1 | — |
| P8 | 작업본은 에셋 참조를 GUID 문자열로 갖는다(EditorPrefs JSON 초안 때문) | — |
| P9 | 참조 고르기는 `ObjectField`가 아니라 찾은 에셋 목록 팝업(internal 형식은 `typeof` 불가) | — |
| P10 | 열거 표시 이름은 `SerializedProperty.enumDisplayNames`에서 | 저장하지 않는 복제본에서 읽음(`FillNames`, SO-b) |
| P11 | 저장 검사는 Root·Combat·View 셋을 복제하고 Root 복제본이 Combat 복제본을 가리키게 해 한 번에. 고친 공유 SO도 같이 | — |
| P12 | 짝 View는 같은 `stageKey`의 `ViewStageDataSO`. 둘 이상이면 열기 거절, 없으면 저장 때 틀 값으로 만듦 | View 없는 스테이지는 저장 거절(SO-b), 'View → 틀의 View로 만들기' 버튼으로 명시적으로 만듦(SO-c) |

### 3.3 P-A1~P-A15

SO-f~SO-i에 쓴 것이다. 2026-10-27 사용자 "SO-d Unity 확인 문제 없음. P-A1~P-A15 그대로 하고 SO-f 진행해".

| ID | 정한 것 |
|---|---|
| P-A1 | 메인 캐릭터 자리를 맵·도구·겹침·고른 칸·3D에서 빼고, '스테이지 값'에 "전투에서 쓰이지 않음"으로 읽기만. 값은 그대로 써서 보존 |
| P-A2 | 상자 선택은 창의 `RectInt`. 다른 스테이지를 열면 지움 |
| P-A3 | 복사본은 창에 작업본 조각으로. 다른 스테이지에 붙여도 됨 |
| P-A4 | 붙이기 기준: 고른 칸 = 복사한 상자의 왼쪽 아래 칸 |
| P-A5 | 선택 도구 끌기: 빈 칸 = 상자, 배치 칸 = 옮기기, 상자 안 = 묶음 옮기기, 같은 칸 떼기 = 고르기. (SO-i에서 더함: Shift+끌기는 늘 상자) |
| P-A6 | 단축키 Delete·Esc·R·Ctrl+C·Ctrl+V, 입력 칸 편집 중에는 안 씀. 버튼도 둠 |
| P-A7 | 좌우 대칭 대상은 상자 안(없으면 전체). 본부는 뒤집기 때만 옮기고 대칭 복사에서는 건너뜀 |
| P-A8 | 범위 대상은 고른 칸 맨 위 적 유닛. 사거리 `attackRange`, 모양 `shape`·`splash`. Core가 거절하면 "범위 없음" |
| P-A9 | 범위 색: 사거리 주황, 스플래시만 옅은 주황, 겹칠수록 진하게 + 수 |
| P-A10 | 3D 칸 크기: GridOutline이면 `outlineCellSpacing`, CellPrefab이면 1×1 근사 |
| P-A11 | 3D 인스턴스 안 `Canvas`·`Camera`·`AudioSource`는 끔 |
| P-A12 | 3D 다시 만들기: 맵은 맵·보정이 바뀔 때, 배치는 작업본 JSON이 바뀔 때 |
| P-A13 | 3D 고르기는 바닥 평면(높이 0) |
| P-A14 | 외형 없는 정의는 층 색 상자 |
| P-A15 | 3D 시점 조작은 DT와 같되 코드는 새 클래스에(시점 코드 두 벌을 받아들임) |

## 4 작업별 결과

각 작업의 화면·연산 상세는 [설계](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md) SM·AD 절에 있다. 여기는 한 것·계획과 다른 점·확인만 적는다.

### 4.1 SO-a 열기·보기

- 한 것: asmdef `Framework` 참조, `CombatLayoutWindow.cs` 머리줄 `DT | SO`·`OnGUI` 분기(DT 버튼 넷은 `else` 안으로), 새 `CombatLayoutWindow.SoMode.cs`·`CombatStageSoDocument.cs`·`CombatStageSoAssets.cs`(화면용 캐시 `CombatStageSoCatalog` 포함)·`CombatStageSoValidation.cs`. 쓰기는 없다.
- 계획과 다른 점: 머리줄 버튼은 목록 새로고침·검사 둘이고 스테이지는 왼쪽 목록을 눌러 연다. 열거 이름은 목록을 만들 때 각 스테이지를 한 번씩 읽어 채운다. 공유 SO 값은 그 SO의 `PropertyField`를 비활성으로 그린다. 공개 대상 이름 문제는 SO-b에서 풀었다.
- 확인: 2026-10-27 사용자 "SO-a Unity 확인 문제 없음".

### 4.2 SO-b 편집·저장

- 한 것: 층 `CombatStageSoLayer`, 칸 계산 `CombatStageSoGrid`(R90·R270이면 가로·세로 바꿈), 바로 막기 `PlacementError`, 놓기·옮기기·회전·지우기(지형·오브젝트를 지우면 같은 sourceId 외형 회전도 지움), `Write`, `CheckDocument`(스테이지 키 중복 포함), 화면 편집·저장·초안, `OnUndoRedo` SO 분기 한 줄.
- 계획과 다른 점: ① 기존 창 파일 셋째 변경(`OnUndoRedo`, 없으면 SO Undo 때 DT 초안이 저장됨) ② "같은 층" 범위를 정함(설계 SM6) ③ 공개 대상 이름은 복제본에서 읽음(`FillNames`) ④ 메인 캐릭터·본부는 지우지 않고 옮기기만(본부를 없애려면 참조에서 "(없음)") ⑤ View 없는 스테이지는 저장 거절(만들기는 SO-c) ⑥ 스테이지 키 중복은 도구가 모든 Root와 비교 ⑦ Vector3 입력은 값마다 Undo ⑧ 새 SO 필드는 도구가 쓰지 않는 약점.
- 확인: 2026-10-27 사용자 "SO-b·한국어 이름 Unity 확인 문제 없음".

### 4.3 정의 목록 한국어 이름

- 요청: "정의 목록 한국어로 바꿀 수 있냐?" → "ViewGroup 외형 표 displayName으로". SO-b 위에 더했다.
- 한 것: 유닛·구조물·장애물은 `ViewGroup/UnitVisual/`에서 키로, 지형·오브젝트는 `TerrainVisual`·`ObjectVisual`에서 종류로 찾는다. 장애물 키는 Core `ObstacleContentProfile.KeyFor` → asmdef `ProjectL.Core.Contracts` 참조(설계 때 "참조 한 줄"에서 둘로 늘어남).
- 확인: SO-b와 함께 확인.

### 4.4 SO-c 만들기·등록

- 한 것: 새 `CombatStageSoCreation.cs` — `Create`(파일을 만들기 전에 검사), 키 제안 `ProposeStageKey`, `SetRegistered`, 짝 View `CreateView`. 화면: '새 스테이지' 접이, '표에 등록'·'표에서 빼기', '틀의 View로 만들기'.
- 계획과 다른 점: P5는 형식으로만 찾음, P12는 버튼으로 명시적 생성, 등록은 저장된 에셋 기준, "배치 비우고 만들기"는 숨김 호환 배치도 비움(SM10 예외), 세 파일 복사 도중 실패하면 앞 파일이 남음(검사가 먼저라 드묾).
- 확인: 2026-10-27 사용자 "SO-c·패널 너비 Unity 확인 문제 없음".

### 4.5 패널 너비

- 요청: "양 옆에 있는 창들, 내가 늘리고 싶으면 늘리게 해주세요".
- 한 것: 경계(너비 6) 끌기, `soLeftWidth` 230·`soRightWidth` 310(최소 160·220, 가운데 최소 300). DT 고정 너비는 그대로.
- 확인: SO-c와 함께 확인.

### 4.6 SO-d 공유 SO

- 한 것: 공유 사용처 `ShareUsers`·`OtherUsers`(안 쓰는 `ShareCount` 삭제), `CloneShared`(필드·로스터·경제·적 로드아웃), 바로 고치기(`sharedEdits` JSON, 편집 복제본 `DontSave`, 키 필드 막음), `Write`의 `resolve`로 고친 공유 SO 검사, `CreateField`.
- 계획과 다른 점: 바로 고치기 조건 = "이 스테이지를 뺀 사용처 0"(저장 때 다시 확인), 맵 밖 검사를 저장·검사·등록에 더함, 공유 SO 깊은 검사는 하지 않음(`GetData()`가 던지는 것만).
- 확인: 2026-10-27 사용자 "SO-d Unity 확인 문제 없음".

### 4.7 SO-f 메인 캐릭터 정리·끌어 옮기기·단축키

- 한 것: 메인 캐릭터 층 제거(P-A1, 값 보존), 상자 선택(하늘색 테두리), 끌어 옮기기(초록·빨강 고스트, Esc), 단축키 Delete·Esc·R와 "시계 방향 회전 (R)" 버튼. 작업본 도우미 `AnchorOf`·`RotationOf`·`Rotates`·`PlacementsIn`·`Contains`.
- 계획과 다른 점: 맵 입력을 칸마다 → 맵 영역 하나 + `hotControl`로 바꿈. 놓을 때 `ExitGUI`로 끝나 `EndStroke`를 놓기 처리 안에서 부름.
- 확인: 2026-10-27 사용자 "SO-f Unity 확인 문제 없음".

### 4.8 SO-g 상자 편집

- 한 것: 작업본 묶음 연산 `TryAdd`·`TopErasable`·`EraseTop`·`PaintBox`·`Copy`·`Paste`(`CombatStageSoClip`)·`MirrorOf`·`MirrorFlip`·`MirrorCopy`·`MoveGroup`·`MovedError`·`Clone`, 창 `TrySoEdit`·`TrySoCount`.
- 계획과 다른 점: 복제본에서 먼저 돌려 보고 바뀔 때만 Undo, Ctrl+C·V는 에디터 명령으로, "상자 안" = 기준 칸, 지우기 한 칸 규칙을 `TopErasable` 하나로, 레일 겹침은 `MovedError`로 따로.
- 확인: 2026-10-27 사용자 "SO-g Unity 확인 문제 없음".

### 4.9 SO-h 공격 범위 미리보기

- 한 것: asmdef `ProjectL.Core.Domain`, `CombatStageSoAssets.ReadAttack`, 새 `CombatStageSoRange.cs`(`ShapeOf`·`AddTo`), 맵 위 "공격 범위(참고용)" 줄.
- 계획과 다른 점: 방향 버튼은 Core가 받아 주는 방향의 화살표(대각선 포함), RadialSquare는 버튼 숨김, 유닛 SO를 고치면 새로고침 전까지 예전 모양.
- 확인: **확인 필요.** 사용자가 SO-h 결과를 따로 말하지 않고 SO-i를 지시했다(옛 추가 설계 §8 머리말은 "SO-f~SO-h 확인 완료"로 적었지만 실행 기록과 다르다). 목록은 §2.1.

### 4.10 SO-i 3D 보기

- 먼저 고친 것(사용자 보고 "Shift+끌기로 사각형을 만들려 해도 안 만들어짐"): 선택 도구 Shift+끌기는 늘 상자(`BeginSoDrag`의 `boxOnly`).
- 한 것: 새 `CombatStageSoVisuals.cs`(외형 표 읽기, 배치 식 사본 `CombatStageSoSpace`: `CellCenterToLocal`·`RotationToWorld`·`EnemyFacing`·`CenterOf`·`RaiseSurface`+`FieldSurfaceHeights.Raise`·`WorldToGrid`, 식마다 원본 주석), 새 `CombatStageSoPreview.cs`, 2D/3D 전환·"시점 초기화", 입력 `HandleSoPointer`·테두리 `SoFrames`를 2D·3D가 같이 씀.
- 계획과 다른 점: `MapAssembler`를 쓰지 않음(에디터 전역 `LightmapSettings`), 칸 덧그리기는 화면 좌표, 레일은 덧그리기만, 이름 글자 없음, 정리는 `OnDestroy`·`AssemblyReloadEvents`(DT `OnDisable` 무변경).
- 확인: 대기(§2.1).

### 4.11 정의 목록 적·아군 구분

- 요청: "적 유닛이랑 아군 유닛 구분해 주세요." 당시 "적 유닛" 목록에 아군 정의 9개가 섞여 있었다. 적 배치에 쓰면 Core `UnitEligibilityRules.Require(definition, Owner.Enemy, false)`가 전투 진입 때 멈춘다. 저장된 스테이지에는 그런 배치가 없었다.
- 한 것: `CombatStageSoAssets.CanBeEnemy`(Core 조건 사본, 원본 주석, 필드 이름 `role`·`ownerEligibility`), 목록 `EnemyUnits`·`PlayerUnits`. 아군은 회색 목록과 이유 한 줄. 예전 작업본에 아군 정의가 있으면 경고만(저장은 P1대로 막지 않음).
- 결정: 아군 미리 배치는 만들지 않는다(2026-10-27 사용자).
- 확인: 대기(§2.1).

### 4.12 진영 줄 수

- 경위: 칸 단위 진영 요청 → Core 변경이 필요해 [진영 칸 초안](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#7-보류-배치-진영-칸-단위-dz1dz6)을 썼고, 사용자가 줄 수만 하기로 했다.
- 한 것: `WriteDeploymentDepths`, `CombatStageSoField.MaxDepthBeside`(Core `StageConfig` 규칙 사본)·`EnemyStartRow`, '스테이지 값' 슬라이더, 공유 필드면 전용 복제 확인 창, 진영 밖 고정 배치 경고. `CloneSoShared`는 복제본 GUID를 돌려주게 바꿈.
- 바뀐 것: 적 줄 수 슬라이더·`MaxDepthBeside`·`EnemyStartRow`·적 진영 경고 → 천명님 `5a1bfbe1f`(적 구역 삭제)에 맞춰 AZ-a에서 걷음(`WritePlayerDepth`·`DrawSoPlayerZone`, §7.1).
- 확인: 대기(§2.1 아군 줄 수).

### 4.13 SO-e 기획자용 사용법

- 요청: "기획자가 사용할 수 있을 정도로 문서. 툴 사용법이랑 DB의 SO를 만들어서 값을 넣는 방법, 그리고 검사나 저장하는 규칙".
- 한 것: 새 [`Tool/SO모드 사용법.md`](../../../Assets/ProjectL/Script/Tool/SO모드%20사용법.md)(열 절). 사실은 화면 문구·`PlacementError`·`SoProblems`·`CombatStageSoValidation`·`CombatStageDetail`·`BattlePreparationCalculator`·`EnemySpawnCalculator`·`CreateAssetMenu`와 대조했다.
- 계획과 다른 점: SO 모드 절을 민형님 `사용법.md`·`README.md` 안에 쓰지 않고 따로 두었다. 두 문서에는 링크 단락만.
- 2026-10-27 정리 때 고친 것: 공유 예시(`Field_15x15` 12곳 → 지금 값), 레일 단위(D1), 3D가 게임과 다른 곳(B1·B2), 고지대 외형 회전 주의(A1), 등록 때 맵 규칙 주의(A2), 벽 위 유닛(C1).

### 4.14 SO-e 공유 메모·구현 메모

- 요청: "SO-e 남은 공유 메모 넷과 설계 구현 메모도 써줘".
- 한 것: 공유 메모 넷(§5), 설계 구현 메모(지금은 설계 SM·AD 절에 합침). 메모의 함수·필드 이름은 게임 코드(`FieldGridMapper`·`FieldUnitPresenter`·`FieldTerrainPresenter`·`FieldSurfaceHeights`·`StageViewPropPresenter`)와 대조했다.

## 5 공유 메모

| 메모 | 받는 사람 | 내용(고친 절) | 전달 |
|---|---|---|---|
| [민형님 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-minhyeong-share.md) | 민형님(도구 작성자) | 바뀐 기존 파일, 새 파일, 같이 쓰는 DT 멤버, DT 무변경. MG-c §2·§4, AZ-c 머리·§2·§4 | 미전달 |
| Core 전달 문서 [§42](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) | 천명님 | 도구가 부르는 Core 공개 API·계약, Core 규칙 사본, 보고만. MG-c §42.1·§42.2, AZ-c §42.1~§42.4·§43 끝 한 줄 | 미전달 |
| [전투 View 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) | 전투 View(용근님) | 3D 배치 식 사본, View DB 필드 이름 의존. MG-c §3·§4, AZ-c §2·§6, 10-29 안내 정정(`abbdea4f`) | 미전달 |
| [호진 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-hojin-share.md) | 호진(StageMap·View DB) | 등록이 Run 노드 후보에 주는 영향, 만드는 에셋·이름. MG-c §1·§3·§4, AZ-c §2·§3·§4 | 미전달 |

## 6 병합 대응 A1·A2 계획 (MG-a~MG-c)

- 설계: [설계 §10](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#10-병합-대응-a1a2-mg1mg9)(MG1~MG9).
- 결정: 2026-10-27 사용자 "Q-MG1~Q-MG4 모두 A, MG-P1~MG-P7·P-MG1~P-MG6 그대로 하고 MG-a 진행해".
- 결과: MG-a·MG-b Unity 확인 문제 없음(사용자), MG-c 문서 완료, 커밋 `86b447cd`. 새 시험 없음.

### 6.1 MG-a 이웃 모양 지형의 외형 회전 (A1)

- 한 것: `CombatStageSoAssets.cs`(모양 칸 이름 상수 다섯, `EdgeShapeTerrainKinds`, `UsesEdgeShapes`, 카탈로그 `EdgeShapeKinds`), `CombatLayoutWindow.SoMode.cs`(`DrawSoViewRotation` 읽기 줄·지우기, `DrawSoMarker` 정의 변경 때 지움, 도우미 `SoUsesEdgeShapes`), `CombatStageSoValidation.cs`(`CheckPairs`). 새 파일·asmdef 변경 없음.
- 계획과 다른 점: 없다. 외형 회전을 넣는 다른 길(R 단축키·회전 버튼·새로 만들기·복제)은 새 항목을 만들지 않음을 확인했다.
- 확인: 2026-10-27 사용자 "MG-a Unity 확인(Ua) 문제 없음".
- 작업 중 본 것: 당시 작업 트리 `Stage_Normal_A.asset`이 Boss → Regular(커밋 전, 우리 변경 아님). MG-c 때는 HEAD와 같이 Boss로 돌아와 있었다.

### 6.2 MG-b 표 등록·저장과 맵 생성 규칙 (A2)

- 한 것: 새 `CombatStageSoChapter.cs`(+ `.meta`) — 값 `CombatStageSoChapterStage`·`CombatStageSoDistribution`·`CombatStageSoMapRules`, 결과 `CombatStageSoChapterResult`, 판정 `CombatStageSoChapterVerdict`·`CombatStageSoChapterMapJudgement`·`CombatStageSoChapterJudgement`, 함수 `MapFor`·`Check`·`Judge`·`Summary`·`Changes`·`RoleOf`. `CombatStageSoAssets.cs`(표·맵 SO 읽기, 출현 칸과 종류 짝 사본), `CombatLayoutWindow.SoMode.cs`(등록·빼기·저장·검사 흐름, 맵 생성 줄), `CombatStageSoCreation.cs`(`SetRegistered` 빼기는 View 없이도). asmdef·맵 SO 변경 없음.
- 계획과 다른 점(모양만, 동작 결정은 그대로): 설계 §10.7에 있다(`Judge` 인자 셋, 보스 교체를 먼저 봄, '검사' 버튼 알림, 맵 SO 없음은 저장 뒤 안내). 등록·빼기 뒤 `RefreshSoCatalog`는 실패해도 부른다(보스 교체 뒤 등록이 실패해도 화면이 실제 표 상태를 따르게).
- 확인: 2026-10-27 사용자 "MG-b Unity 확인(Ub) 문제 없음"(11단계, 운영 표를 실제로 고쳤다가 되돌림). 확인 뒤 표는 HEAD와 같다(맵 1 보스 `Stage_Normal_A`, 전투 후보 B·C).

### 6.3 MG-c 문서·메모

- 한 것(2026-10-27, 코드 변경 없음): 사용법 §3.3·§4.4·§5.9·§7.3·§8.2·§8.4(맵 생성 규칙·보스 바꾸기·맵 생성 줄)·§9, 공유 메모 넷(§5), 설계 §2.1·§2.4·§8·§10.6·§10.7, CLAUDE.md.
- 계획에 없던 것: 사용법 §3.3·§4.4·§7.3, 전투 View 공유 §3 필드 이름 줄.
- 작업 트리에서 본 것: 미등록 `Stage_Boss`의 역할이 Regular로 바뀌어 있었다(커밋 전, 우리 변경 아님, 호진 공유 §4).

### 6.4 계획에서 정한 것 (확인 대상, P-MG1~P-MG6)

모두 그대로(2026-10-27 사용자).

| ID | 정한 것 |
|---|---|
| P-MG1 | 이웃 모양 판정 함수는 하나다. 화면용 캐시(카탈로그)와 검사용 새로 읽기가 같은 함수를 부른다 |
| P-MG2 | 맵 판정의 값 형식·판정·바뀐 뒤 상태는 새 파일 하나 `CombatStageSoChapter.cs`에 둔다. 에셋 읽기는 `CombatStageSoAssets`에 둔다(SM3) |
| P-MG3 | 역할은 enum 이름이 "Regular"와 같은지로 본다. 연결 방식은 이름으로 `ChapterGraphConnection`을 찾는다(숫자 순서에 기대지 않음). 이름이 없으면 그 맵은 판정 못 함이다 |
| P-MG4 | 보스 교체는 옛 보스의 Root·View를 `SetRegistered(false)`로 뺀다. 옛 보스의 View는 스테이지 키로 찾고(카탈로그 `ViewsFor`), 없으면 Root만 뺀다 |
| P-MG5 | 저장과 보스 교체가 함께일 때 쓰기 순서는 스테이지 에셋 → 표다. 스테이지 쓰기가 실패하면 표는 그대로다 |
| P-MG6 | 검토 에이전트는 돌리지 않는다(§3.1 규칙) |

## 7 아군 배치 구역 칸 계획 (AZ-a~AZ-c)

- 설계: [설계 §11](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#11-아군-배치-구역-칸-az1az9)(AZ1~AZ9).
- 결정: 2026-10-27 사용자 Q-AZ1~Q-AZ5 모두 A, P-AZ1~P-AZ10 그대로, "P-AZT1~P-AZT9 그대로 하고 AZ-a 진행해".
- 결과: AZ-a·AZ-b Unity 확인 문제 없음(사용자 2026-10-28), AZ-c 문서 완료, 커밋 `ac4a8101`. 새 파일·asmdef 변경 없음, 새 시험 없음. 2026-10-29 안내 줄 삭제는 `abbdea4f`(§7.3).
- 범위 밖: DT 모드·엑셀 도구, 개발 진입 Fixture, DB SO 코드, 게임 화면 표시(Q-AZ5 A, 전투 View 담당).

### 7.1 AZ-a 맞추기

- 한 것(2026-10-27): `CombatStageSoDocument.cs`(칸 목록 필드, `ClearPlacements`가 비움), `CombatStageSoAssets.cs`(필드 이름 상수, 칸 목록 읽기·쓰기 y → x 정렬, 구역 해석 `ZoneOf` → 값 `CombatStageSoZone`, Core 형식 별칭 넷, `WritePlayerDepth`), `CombatLayoutWindow.SoMode.cs`(색·범례·고른 칸 이름·3D, `DrawSoPlayerZone`·`SetSoPlayerDepth`, 경고 ①, 새 필드, 공유 필드 안내 줄, 맵 밖), `CombatStageSoCreation.cs`(`CreateField`·`WriteFieldValues`가 적 깊이를 받지 않음, 아군 줄 수 > 세로 거절).
- 지운 것: `CombatStageSoField.MaxDepthBeside`·`EnemyStartRow`, 색 `EnemyZone`, 창 필드 `soFieldEnemyDepth`, "세로 3줄 미만이면 바꿀 수 없음" 갈래. `CombatStageSoField.EnemyDepth`는 읽기만 남는다(쓰는 곳 없음).
- 계획과 다른 점: 계획의 `PlayerZoneOf` 대신 `ZoneOf` + 도구 값(창 파일에 Core 형식 이름을 넣지 않으려고, 동작 같음, 설계 §11.2 AZ2).
- 확인: 2026-10-28 사용자 "AZ-a Unity 확인(Ua) 문제 없음"(7단계).

### 7.2 AZ-b 아군 칸 붓·경고

- 한 것(2026-10-28): `CombatStageSoDocument.cs`(`PaintZone`), `CombatStageSoAssets.cs`(`BlocksEntry`·`SpawnCountOf`, 카탈로그 캐시), `CombatLayoutWindow.SoMode.cs`(도구 `PlayerZone`·`칠하기 | 지우기`, `PaintSoZone`, 칸 목록 비우기, 참고 수·경고 ②~④). `HandleSoCell`의 칠하는 도구 목록 사본은 `IsPaintTool`로 바꿨다.
- 계획과 다른 점: `PaintZone`이 `fallbackDepth` 대신 `field`를 받아 같은 `ZoneOf`로 해석한다(옮기는 칸 = 화면에 보이는 칸). 편집은 그 자리에서 더하고 빼서 인스펙터 중복은 고치지 않는다(지울 때는 그 칸의 중복도 모두 빠짐). 칸 목록 비우기는 창에서 `Clear()` 한 줄. 구역 도구일 때는 "막힌 칸은 건너뜀" 줄을 숨긴다. 알림 문구("이미 아군 구역인 칸입니다", "필드 아군 줄 수 N줄의 칸을 칸 목록으로 옮긴 뒤 n칸 칠함" 등)를 더했다.
- 참고 수에서 빠지는 것(P-AZ9): 무작위 시작 지형, 숨김 호환 벽, 공개 대상. 아군 구역 안 고정 적은 막힌 칸으로 세지 않는다(경고 ①이 따로 알림).
- 확인: 2026-10-28 사용자 Ub 9단계 문제 없음(마지막 단계는 `Test`를 등록해 PlacedStage로 들어가 칠한 칸에만 아군을 놓음을 확인한 뒤 되돌림).

### 7.3 AZ-c 문서·메모

- 한 것(2026-10-28, 코드 변경 없음): 사용법 §3.2·§3.3·§4.2·§4.3·§5.1·§5.3·§5.4·§5.8("아군 배치 구역"으로 다시 씀)·§6·§7.4·§8.2·§8.5·§9(콘솔 문구는 Core 코드와 대조), 공유 메모 넷(§5), 설계 §11.9, CLAUDE.md. 계획 밖으로 사용법 §3.3의 잘못된 절 번호(§5.7 → §5.9)를 고쳤다.
- 계획 밖에서 찾아 적은 것: 도구는 회전 열거 이름을 `mainCharacterRotation`에서 읽는다(`FillNames`). Core PZ-d가 그 필드를 지우면 스테이지 열기가 멈춘다 → Core 전달 문서 §42.4.
- **2026-10-29 후속(`abbdea4f`, 컴파일 미확인):** 병합 `a8612321e`로 전투 View PV-a가 들어와 게임 화면이 칸 구역을 그린다. 도구 `DrawSoPlayerZone`의 안내 상자 "게임 화면은 아직 칸 구역을 그리지 않습니다…"와 주석 두 줄을 지우고, 사용법 §5.8 "게임 화면" 문단·문제 표 줄, 전투 View 공유 §7(병합 전 §6) 머리를 고쳤다.

### 7.4 계획에서 정한 것 (확인 대상, P-AZT1~P-AZT9)

모두 그대로(2026-10-27 사용자).

| ID | 정한 것 |
|---|---|
| P-AZT1 | 칸 목록은 집합으로 다룬다. 화면은 중복을 합쳐 그린다. 인스펙터로 넣은 중복은 도구가 고치지 않고 저장 검사(`GetData()` → Core `Of`)가 막는다 |
| P-AZT2 | 새로 만들기의 칸 비우기(설계 AZ8)를 AZ-a로 앞당긴다. 상자 편집이 구역을 옮기지 않는 것(P-AZ6)은 따로 할 일이 없고 확인만 한다 |
| P-AZT3 | 경고 ①(고정 적이 아군 구역 안)을 AZ-a로 앞당긴다. 옛 적 진영 경고를 걷는 자리라 비워 두지 않는다. 수 줄과 경고 ②~④는 AZ-b다 |
| P-AZT4 | `SoTool.PlayerZone`은 열거 끝에 더한다. 창의 `soTool`이 숫자로 직렬화돼 사이에 넣으면 저장된 도구 선택이 바뀐다 |
| P-AZT5 | 벽·낭떠러지는 지형 정의 `kind` 열거 **이름**("Wall"·"Cliff")으로 본다. 도구는 Systems 열거를 참조하지 않는다. 원본 자리(`EnvironmentTerrainRules.BlocksVoluntaryEntry`)를 주석으로 단다 |
| P-AZT6 | 줄 수 칸을 칸 목록으로 옮길 때 판 너비는 지금 필드 가로, 순서는 y → x다 |
| P-AZT7 | 효과 없는 구역 편집은 칸 목록으로 옮기지도 않고 Undo도 남기지 않는다 |
| P-AZT8 | 검토 에이전트는 돌리지 않는다(§3.1 규칙, P-MG6과 같음) |
| P-AZT9 | `WriteDeploymentDepths`는 아군 줄 수만 쓰게 바꾼다(→ `WritePlayerDepth`). 적 줄 수 값은 그대로 둔다(P-AZ8) |

<a id="8-병합-최신화-계획-ut-aut-d"></a>
## 8 병합 최신화 계획 (UT-a~UT-d)

- 설계: [설계 §12](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#12-병합-최신화-배경-방식바닥-타일-ut1ut10)(UT1~UT10).
- 결정: 2026-10-29 사용자 "모두 권장안대로 하는데, 3D/2D에서도 타일이 바뀐 것을 알고 싶어요" → Q-UT1~Q-UT5 A, P-UT1~P-UT10 그대로, UT10(2D·3D 타일 표시)·P-UT11·P-UT12 추가.
- 문서 보완(2026-10-29): 사용자 "이 5가지 보완 내용을 설계·계획 문서에 직접 반영해줘. 코드는 수정하지 말고."에 따라 설계 P-UT13~P-UT17을 아래 Task·Unity 확인에 연결했다(§8.6). 이번 문서 반영을 코드 착수나 Phase 인수로 취급하지 않는다.
- 기준 판: HEAD `954527d9`(용근님 바닥 타일 세팅). 당시 사용법 §7.4·§8.5 배경 방식 정정, 설계 §12, 이 절은 미커밋이었다. 이후 반영 이력은 아래 상태를 따른다.
- 규칙: §3.1 그대로(MSBuild 없음, 새 시험 없음, 검토 에이전트 없음, Phase마다 멈추고 보고, 보고에 "컴파일 미확인").
- 범위 밖: 게임 코드·DB SO 코드·타일 종류 열거·타일 세트 내용(Q-UT5 A), B1·B2·C1(Q-UT4 A, §2.2에 남김), DT 모드.
- 상태: 다섯 보완의 문서 반영 완료. P-UTT1~P-UTT10 그대로(2026-10-29 사용자 "놓친 거 없으면 그대로, UT-a 진행"). UT-a 코드 완료·Ua 확인(2026-10-29 사용자), UT-b·UT-c 코드 완료·Ub·Uc 확인(사용자), UT-d 문서 완료(§8.4). 코드·사용법·설계·계획 `ede23986a`, 공유 문서 후속 `f5c8109d4`.

새 파일 둘(`.meta`는 Unity가 만듦, P-UT8): `Tool/Editor/CombatStageSoTiles.cs`(작업본 타일 값·화면 방향 대응·맞춤·칸 → 타일 대응·타일 높이, 순수 계산), `Tool/Editor/CombatLayoutWindow.SoTiles.cs`(배경 패널·'바닥 타일' 묶음·'타일' 보기·붓, 같은 `partial class`). 바뀌는 기존 파일: `CombatStageSoDocument.cs`·`CombatStageSoAssets.cs`·`CombatStageSoVisuals.cs`·`CombatStageSoPreview.cs`·`CombatLayoutWindow.SoMode.cs`. asmdef 변경 없음.

### 8.1 UT-a 값·패널 (UT1·UT2·UT3·UT8)

화면 변화는 오른쪽 View 패널뿐이고, 3D는 배경 고르기만 방식대로 고친다(P-UTT4).

- [x] Ta.1 `CombatStageSoTiles.cs`(새): 작업본 값 `CombatStageSoTileLayout`(`[Serializable]`, `tileSetGuid`·`columns`·`rows`·`tileSize`·`offset`·`anchorTile`·`kinds`(int 배열, 에셋과 같은 MapTileRoot 순서)·`IsEmpty`). 연산 `KindAt`·`SetKind`(배열 칸), `Resize(columns, rows, fillKind)`(P-UT2), `CreateDefault(…)`(P-UT3: 세트·필드 가로세로·칸 간격 ÷ 배경 배율·위치 0·기준 (0,0)·첫 종류).
- [x] Ta.2 `CombatStageSoDocument.cs`: `CombatStageSoView`에 `mode`(int)·`tiles`(`CombatStageSoTileLayout`). 작업본에 초안 형식 번호 `format`과 상수 `CurrentFormat`(UT8).
- [x] Ta.3 `CombatStageSoAssets.cs`: 필드 이름 상수(`mode`·`mapTiles`·`tileSet`·`columns`·`rows`·`tileSize`·`offset`·`anchorTile`·`tiles`, 세트 `tiles`·`kind`·`prefab`·`surfaceHeight`), `ReadView`·`WriteView`에 방식·타일(타일이 비면 에셋 칸도 모두 비움 — 게임 "모두 비면 타일 없음"과 같은 값), 방식·종류 열거 이름(`enumNames`, `CombatStageSoNames`에 칸 둘), 타일 세트 목록 `TileSetGuids()`(P-UTT5)·세트 읽기 `ReadTileSet(guid)` → 종류마다 (값, 이름, 프리팹, 높이).
- [x] Ta.4 `CombatLayoutWindow.SoTiles.cs`(새) `DrawSoBackground(view)`: 방식 팝업(프리팹 / 맵 SO / 프리팹 + 바닥 타일, `SoChange`), 방식이 쓰는 칸만(맵 SO 또는 맵 프리팹), 보정 줄 방식별 안내(UT2). `DrawSoTileLayout(view)`: 타일 방식이면 세트(목록 + Inspector 열기)·가로·세로·크기·위치·기준 타일, 비면 '타일 만들기', 세트 종류 읽기 줄, 세트에 없는 종류 칸 수 경고.
- [x] Ta.5 `CombatLayoutWindow.SoMode.cs` `DrawSoView`: 맵 SO·맵 프리팹 두 줄과 문구 "맵 SO가 있으면…"을 `DrawSoBackground` 호출로 바꾼다. `SaveSoDraft`·`RestoreSoDraft`가 형식 번호를 쓰고 비교한다(다르면 복원하지 않고 알림, P-UT6).
- [x] Ta.6 `CombatStageSoPreview.RebuildMap`: 방식대로 맵 프리팹/맵 SO 프리팹을 고르고 맵 키에 방식을 넣는다(P-UTT4).

**Ua (Unity 확인, P-UTT8: Test를 '복제'한 미등록 스테이지로)**

1. 컴파일된다.
2. `Combat_Normal_A`는 배경 "프리팹"(맵 프리팹 줄만), `Combat_Boss`는 "맵 SO"(맵 SO 줄만), `Test`는 "프리팹 + 바닥 타일"이고 묶음 값이 인스펙터와 같다(17 × 17, 크기 (2, 2), 위치 (−1, −2, 0.5), 기준 (4, 4), 세트 `CombatMapTileSet_Temp`, 종류 넷).
3. Test 복제본에서 가로 17 → 18로 바꿔 저장하면 인스펙터 `tiles` 길이가 18 × 17이고 기존 칸 종류가 같은 자리에 남으며, 새로 생긴 가로 18번째 칸(줄마다 하나)은 Test1이다. Undo로 17로 돌아간다.
4. 방식을 "프리팹"으로 바꿔 저장해도 타일 값이 인스펙터에 남고, 다시 "프리팹 + 바닥 타일"로 바꾸면 그대로 쓰인다.
5. `Combat_Normal_A` 복제본에서 "프리팹 + 바닥 타일" → '타일 만들기' → 저장이 통과한다.
6. 타일 세트를 비우고 저장하면 저장 검사가 막는다(문구에 "타일 세트(tileSet)가 필요합니다").
7. 3D에서 `Combat_Boss`는 맵 SO 배경, 나머지는 맵 프리팹 배경이다(방식대로).
8. (계획 밖으로 더한 것) 업데이트 전에 창에 스테이지를 열어 둔 채 컴파일되면 "도구가 바뀌어 …을(를) 에셋에서 다시 읽었습니다" 알림이 뜨고, 그 스테이지의 배경 방식·타일 값이 인스펙터와 같다. 열어 둔 스테이지가 없었다면 이 단계는 건너뛴다.

**UT-a 실행 기록 (2026-10-29, 사용자 "놓친 거 없으면 그대로, UT-a 진행", 컴파일 미확인, 새 시험 없음, Ua 대기)**

- 새 파일 둘(`.meta`는 Unity가 만듦): `CombatStageSoTiles.cs`(작업본 타일 값 `CombatStageSoTileLayout`, 세트 종류 줄 `CombatStageSoTileKind`, 배경 방식 규칙 사본 `CombatStageSoBackground` — 방식 이름·라벨·회전 단계 `QuarterTurns`), `CombatLayoutWindow.SoTiles.cs`(`DrawSoBackground`·`DrawSoTileLayout`·`DrawSoTileKinds`·`ResizeSoTiles`·`CreateSoTiles`, 세트 종류 캐시 `soTileKinds`, 입력 칸 `SoVector2`·`SoVector2Int`·`SoNote`).
- 바뀐 파일 넷: `CombatStageSoDocument.cs`(형식 번호 `format`·`CurrentFormat = 1`, View `mode`·`tiles`), `CombatStageSoAssets.cs`(필드 이름 상수, `ReadView`·`WriteView`에 방식·타일, `ReadTiles`·`WriteTiles`·`ReadTileSet`·`FillViewNames`, 이름 `viewMode`, 카탈로그 `TileSets`), `CombatLayoutWindow.SoMode.cs`(`DrawSoView`가 배경 패널·바닥 타일 묶음을 부름, 초안 형식 비교, `ReopenSoOldFormat`, 새로고침이 세트 캐시를 비움, 쓰지 않게 된 `using ProjectL.Tool.Runtime` 삭제), `CombatStageSoPreview.cs`(`Sync`에 방식 이름, `RebuildMap`이 방식대로 고름·맵 키에 방식).
- 계획과 다른 점·더한 것:
  - **창에 남은 작업본도 형식을 본다(계획 밖, 사용자에게 보고).** 초안만 보면 업데이트 전에 창에 열어 둔 작업본이 컴파일 뒤 창 직렬화로 그대로 돌아와(타일 빈 값), 그대로 저장하면 Test의 타일이 지워진다. `DrawSoMode`가 형식이 다르면 저장 안 한 편집을 버리고 같은 스테이지를 에셋에서 다시 읽는다(`ReopenSoOldFormat`). Undo로 옛 형식 상태가 돌아와도 같은 길로 다시 읽는다. Ua 8로 확인.
  - 타일 종류는 작업본에 열거 **값**(intValue)으로 둔다. 다른 열거는 순번(enumValueIndex)인데, 종류 열거에 0이 없어 순번과 값이 달라서다(파일 주석).
  - 배경 방식은 순번으로 두고, 뜻은 열거 이름(`Prefab`·`MapDefinition`·`PrefabWithTiles`)으로 판단한다. 이름을 모르면 맵 SO·맵 프리팹 칸을 모두 보이고, 3D는 옛 규칙(맵 SO가 있으면 맵 SO)으로 고른다. 이름은 카탈로그를 만들 때 첫 View에서 읽는다(`FillViewNames`).
  - 세트 목록 함수 `TileSetGuids()` 대신 카탈로그 `TileSets`(다른 참조 목록과 같은 `CombatStageSoChoices`, P-UTT5 그대로 형식 이름 검색·이름순).
  - 가로·세로 입력은 1~128칸만 받는다(창이 멈추지 않게 하는 도구 상한, 게임에는 없음).
  - 방식이 쓰지 않는 칸에 값이 남아 있으면 "값이 남아 있지만 이 방식은 쓰지 않습니다" 줄을 보인다(P-UT4).
- 알아 둘 것: 저장 때 View의 `mapTiles` 칸을 늘 쓰므로, 이 칸이 아직 YAML에 없던 View(예: 보스)를 저장하면 같은 기본값의 `mapTiles` 블록이 에셋에 새로 생긴다(값 변화 없음).

### 8.2 UT-b 타일 보기·붓·맞춤 알리기·2D 띠 (UT4·UT7·UT10 2D)

- [x] Tb.1 `CombatStageSoTiles.cs` 계산(원본 자리 주석, P-UT7):
  - `CombatStageSoAssets.ReadFieldView`·`CombatStageSoFieldView`가 실제 GridOutline 여부를 전달하고, `CombatStageSoTileFit.Of(layout, isGridOutline, rotationOffset, backdropScale, columns, rows, cellWidth, cellLength)`가 전제 판단을 결과에 포함한다(P-UT13, 원본 `FieldCellSpacing.FromGridOutline`). CellPrefab의 1×1 근삿값으로 타일 맞춤 성공을 판단하지 않는다.
  - 화면 방향: 회전 단계 k(P-UTT10), 기울기(회전 X·Z), 화면 가로·세로 타일 수(k 홀수면 바뀜), 화면 칸 ↔ 배열 칸(`ScreenToArray`·`ArrayToScreen`), 타일 한 장의 칸 수(축별, 정수배 아니면 문제).
  - GridOutline 전제와 기존 맞춤 예외 넷을 문제로, 필드 넘침·한 타일에 칸 여럿을 경고로(UT7). 기준 타일은 화면 크기로 검사(게임 `AnchorShift`).
  - 칸 → 타일 `TryTileOfCell(cell, out arrayColumn, out arrayRow)`(P-UT11), 문제가 있으면 false.
- [x] Tb.2 `CombatLayoutWindow.SoTiles.cs` 보기: `soMapView`(0 2D·1 3D·2 타일, P-UTT1) 툴바. '타일' 보기는 배치 도구 줄 대신 타일 붓 줄(종류 고르기, `칠하기 \| 기준 타일`, '모두 이 종류로', P-UTT2), 타일 맵을 화면 방향으로 그리기(종류 색 P-UT5, 필드 자리 사각형·넘치면 빨강, 기준 타일 테두리, 칸 글자 = 종류 이름). 타일 방식이 아니면 안내 한 줄(P-UTT3).
- [x] Tb.3 붓 입력: 누름·끌기 칸마다, Shift+끌기 사각형, 기준 타일 모드는 누른 타일로 옮김. 끌기 한 번 = Undo 한 번(`EndStroke` 공유, 타일 칸 전용 마지막 칸), 효과 없으면 Undo 없음.
- [x] Tb.4 지형 외형 방식 읽기: `CombatStageSoAssets` 외형 표 읽기에 `visualMode`(열거 이름 `OnTile`·`ReplaceTile`, P-UTT6) → `CombatStageSoVisual.ReplacesTile`.
- [x] Tb.5 2D 띠(UT10): `DrawSoMap2D`가 칸 아래쪽 띠에 타일 종류 색(타일 밖 = 어두운 띠), 바닥 교체 지형 칸은 띠에 "교체" 표시. 범례에 판 안 종류. `DrawSoCell` 고른 칸 줄에 "바닥 타일 Test3(높이 −0.1)". 모드·맞춤 문제가 있으면 필드 대응 표시(타일 보기의 필드 사각형·2D 띠·고른 칸 타일 정보)를 생략한다. 프리팹 없는 ReplaceTile도 2D 지형 표식·선택 표시는 유지한다(P-UT14).
- [x] Tb.6 모드·맞춤 알리기: Tb.1의 같은 판단 결과로 '바닥 타일' 묶음·맵 아래·저장 뒤 알림에 문제·경고를 보인다(경고만, Q-UT2 A). GridOutline 전제 실패만으로 저장을 막지 않는다.
- [x] Tb.7 명시적 새로고침(P-UT17): `RefreshSoCatalog`가 외형·타일 세트·필드 표시 값을 다시 읽고 2D 대응·높이 관련 캐시를 무효화한다. 같은 GUID의 Inspector 변경도 반영한다. 작업본·Undo·선택은 유지한다. 자동 변경 감지는 추가하지 않고, Inspector 수정 뒤 '목록 새로고침'을 누르라는 안내를 '바닥 타일' 묶음에 넣는다. 3D 갱신 연결은 Tc.5다.

**Ub (Unity 확인, Test 복제본)**

1. 컴파일된다. 보기 툴바가 2D·3D·타일 셋이다.
2. '타일' 보기에 17 × 17이 모두 Test3 색으로 보이고, 필드(9 × 9) 사각형이 화면 기준 (4, 4)부터 놓인다.
3. 화면 좌하단 타일 하나를 Test1로 칠하고 저장하면 인스펙터 `tiles`의 273번째 값(인덱스 272)만 바뀐다(회전 보정 Y −90: 화면 (0, 0) = 배열 가로 0·세로 16).
4. 필드 안 타일을 칠하면 2D 보기에서 그 칸의 아래 띠 색이 바뀌고, 고른 칸 줄에 새 종류와 높이가 나온다.
5. 기준 타일 모드로 (5, 4)를 누르면 필드 사각형이 한 칸 옮겨지고 2D 띠 대응도 한 칸 밀린다. (16, 16)이면 사각형이 빨강이고 "필드가 타일 맵 밖으로 넘침" 경고가 나온다.
6. 타일 크기를 (1.5, 1.5)로 바꾸면 "정수배가 아님" 문제가 나오고 2D 띠가 사라진다. 회전 보정 X를 10으로 바꾸면 기울기 문제가 나온다.
7. 끌어 칠하기 한 번이 Undo 한 번에 되돌아간다.
8. 약한 지반을 놓은 칸의 띠가 검은 줄무늬다("교체" 표시, 범례 "줄무늬 = 바닥 교체").
9. 저장한 복제본을 표에 등록해 Play From Boot PlacedStage로 들어가 3번에서 칠한 타일이 화면 좌하단에 있는지 본다(끝나면 등록을 뺀다).
10. CellPrefab + PrefabWithTiles 조건에서 GridOutline이 필요하다는 안내가 나오고 필드 대응 표시를 생략한다. 데이터가 유효하면 이 조건만으로 저장을 막지 않는다. GridOutline 조건을 만족하고 새로고침하면 대응 표시가 복구된다.
11. 확인용 타일 세트 복제본을 연결한 뒤 Inspector에서 높이를 바꾸고 '목록 새로고침'을 누르면 고른 칸 높이가 갱신된다. 저장하지 않은 칠하기·선택이 유지되고 기존 Undo가 동작한다.
12. (계획 밖으로 더한 것) '타일' 보기에서 Delete·R·Ctrl+C/V를 눌러도 2D의 배치·선택이 바뀌지 않는다. Shift+끌기 중 Esc를 누르면 사각형만 취소된다.

**UT-b 실행 기록 (2026-10-29, 사용자 "Ua 확인했어, Ub 실행해" → UT-b 구현, 컴파일 미확인, 새 시험 없음, Ub 대기)**

- 새 파일 없음. 바뀐 파일: `CombatStageSoTiles.cs`(맞춤 계산 `CombatStageSoTileFit` — 문제·경고, 화면 크기, `ScreenToArray`, `TryTileOfCell`·`TryScreenTileOfCell`, `FieldTiles`, `ColumnsAlongFieldX`), `CombatStageSoVisuals.cs`(`CombatStageSoVisual.ReplacesTile`), `CombatStageSoAssets.cs`(외형 표 읽기에 `visualMode`), `CombatLayoutWindow.SoTiles.cs`('타일' 보기·붓·입력, 2D 띠·범례, 고른 칸 줄, 맞춤 알림, 저장 뒤 한 줄, 새로고침 안내), `CombatLayoutWindow.SoMode.cs`(보기 셋 `soMapView`, 타일 보기 갈래, 2D 띠·고른 칸·알림 연결, 타일 보기 단축키 막기, 저장 알림).
- 계획과 다른 점·더한 것:
  - **방향 계산:** 계획 P-UTT10의 "회전 보정 Y를 90°로 반올림" 대신, 회전 보정 전체를 적용한 타일 가로·세로 축이 어느 필드 축·방향에 가까운지로 정한다(게임 `AnchorShift`의 `columnsAlongX`와 같은 비교). X·Z가 0이면 결과가 같고, 배율 보정이 음수(뒤집힘)여도 게임과 같은 방향이 나온다. UT-a의 `CombatStageSoBackground.QuarterTurns`는 지우고 '타일 만들기'도 같은 함수(`ColumnsAlongFieldX`)를 쓴다.
  - **축 평행 검사:** 게임 `RequireAlong`은 기울기가 없으면 늘 성립해 기울기 검사 하나로 알린다.
  - **"교체" 표시:** 칸(40px) 아래 6px 띠에 글자가 들어가지 않아 검은 줄무늬로 그리고 범례에 "줄무늬 = 바닥 교체"를 둔다(Ub 8 문구 고침).
  - **바닥 교체 칸:** 공개 대상(knownTo)은 보지 않고 칸이 있는 지형만 본다. 게임은 Player에게 보이는 지형만 타일을 숨기므로, 숨김 지형이면 도구만 줄무늬를 그린다.
  - **알림 자리:** 맞춤 문제·경고는 2D·3D·'타일' 보기의 맵 아래, '바닥 타일' 묶음, 저장 뒤 알림 한 줄에 같은 결과로 보인다. 띠 범례는 2D에서만.
  - **'타일' 보기 단축키:** 배치 단축키(복사·붙여넣기·Delete·R)를 받지 않는다. 보이지 않는 2D 선택이 바뀌지 않게 하려는 것이다. Esc는 끄는 중인 사각형만 취소한다(Ub 12).
  - 빠르게 끈 사이 칸은 메우지 않는다(배치 칠하기와 같음). 타일 칸은 28px 고정, 넘치면 스크롤.
  - 외형 표는 3D와 같은 캐시(`soVisuals`)를 쓰고, '목록 새로고침'이 세트 종류 캐시와 함께 비운다(P-UT17의 2D 쪽). 3D 다시 만들기 연결은 UT-c(Tc.5)다.

### 8.3 UT-c 3D 배경·높이 (UT5·UT6·UT10 3D)

- [x] Tc.1 `CombatStageSoPreview.RebuildMap`: 타일 방식이고 Tb.1의 모드·맞춤 조건이 성립하면 게임 순서대로 MapTileRoot(배경 자식, `offset`)·타일 인스턴스(배열 칸마다)·배경 회전(격자 가운데 기준, `YawCorrection` 사본)·수평 이동(`AnchorShift` 사본)을 하고, 필드 칸 → 타일 인스턴스 표를 남긴다. 맵 키에 방식·타일 값·필드 표시 모드·필드 크기·칸 간격(P-UTT7). 모드·맞춤 문제가 있으면 타일 없이 배경만 놓고 같은 문제를 맵 아래에 띄운다(UT10). 명시적 새로고침도 Tc.5를 통해 재생성한다.
- [x] Tc.2 `CombatStageSoSpace`: 바탕 표(칸 → 타일 높이, `TryTileOfCell` + 세트 높이) → 지형 방식(OnTile 올림 = max(지금, 바탕 + 높이), ReplaceTile 교체 = 프리팹 없어도·한 칸 여럿이면 낮은 값) → `SurfaceAt`·`BaseAt`·`OverlayAt`(지형 높이만). `StandingPoint`는 첫 점유 칸 높이부터 최댓값. `Signature`에 타일·지형 값.
- [x] Tc.3 `RebuildPlacements`: 타일 위 지형 외형 = 칸 바탕, 오브젝트 = 칸 표면. 정상적인 ReplaceTile의 프리팹 없음은 빈 구멍으로 처리해 색 상자를 그리지 않고, 외형 데이터 자체의 누락·오류와 구분한다(P-UT14). 높이 교체(Tc.2)·타일 숨김(Tc.4)은 프리팹 생성 여부와 분리한다. 프리팹이 있는 교체 외형은 P-UT16의 부모·위치·회전·배율 계약을 한 배치 계산에서 따른다. 원본 `FieldTerrainPresenter.PlaceOnTile`·`PlaceInField`·`LocalPosition`을 주석으로 남기며, 타일 없는 칸에는 교체 높이를 위치에 다시 더하지 않는다. `Corners`(덧그리기)는 `OverlayAt`.
- [x] Tc.4 타일 수명(P-UT15): `CombatStageSoPreview`가 자신이 숨긴 타일과 현재 숨길 타일 집합을 비교해 복원한다(원본 `FieldTileReplacement.Apply`·`Restore`). 원래 꺼진 타일은 켜지 않고, 공유 타일은 마지막 교체 지형이 제거될 때만 복원한다. 놓기·삭제·이동·정의 변경·Undo·Redo가 같은 경로로 갱신한다. 배경 재생성·스테이지 교체·창 종료 때 교체 외형·숨김 기록·옛 타일 참조를 정리하고, 새 타일 표에 현재 작업본을 다시 적용한다.
- [x] Tc.5 3D 새로고침(P-UT17): Tb.7의 명시적 새로고침에 맵 키·배치 키·파생 높이 무효화를 연결해 다음 미리보기 갱신에서 배경·타일·배치를 함께 다시 만든다. 참조 GUID·작업본 JSON이 같아도 내부 에셋 변경을 반영한다. 기존 `ResetView`를 호출해 시점을 초기화하지 않으며, 작업본·Undo·선택도 유지한다. 재생성 정리는 Tc.4의 같은 수명 경로를 쓴다.

**Uc (Unity 확인)**

1. 컴파일된다.
2. Test 복제본 3D에서 타일이 배경 구멍을 채운다. 같은 스테이지 PlacedStage 화면과 타일 자리·방향이 같다.
3. '타일' 보기에서 칠한 뒤 3D로 바꾸면 바뀐 타일이 바로 보인다.
4. 높이 −0.1 타일 위 유닛이 타일 윗면에 선다(게임과 같은 높이).
5. 고지대 칸 위 오브젝트가 블록 윗면에 선다(타일 없는 `Combat_Normal_A`에서도).
6. 약한 지반 칸은 그 타일이 숨고 약한 지반 외형이 타일 자리에 있다.
7. 아군 구역·범위 덧그리기는 타일 높이를 따르지 않고 지형 높이만 따른다.
8. `Combat_Normal_A`·`Combat_Boss` 3D는 전과 같다(타일 없음).
9. 낭떠러지(ReplaceTile·프리팹 없음)를 놓으면 바닥 타일만 사라지고 색 상자는 생기지 않는다. 2D 표식·선택은 유지되고, 프리팹 유무와 관계없이 교체 높이가 적용된다.
10. 약한 지반·낭떠러지의 삭제·이동·정의 변경·Undo·Redo 뒤 현재 배치와 같은 타일만 숨는다. 이전 자리에 불필요한 구멍이 남지 않는다.
11. 한 타일을 여러 필드 칸이 공유할 때 교체 지형 하나를 지워도 다른 교체 지형이 남으면 숨김을 유지하고, 마지막 교체 지형을 제거하면 복원된다. 원래 비활성인 확인용 타일은 복원 과정에서 켜지지 않는다.
12. Test의 배경 회전 Y −90°·배율 0.5에서 교체 외형의 위치·크기·방향이 게임과 일치한다. 외형 회전과 0이 아닌 오프셋을 가진 확인용 외형에서도 필드 방향·타일 루트 위치 기준을 따른다. 타일 밖 칸은 높이 0 + 외형 오프셋이고 교체 높이를 중복해서 더하지 않는다.
13. 확인용 타일 세트 복제본의 프리팹·높이를 Inspector에서 바꾼 뒤 '목록 새로고침'을 누르면 같은 GUID 상태에서도 2D 정보·타일 외형·유닛 위치가 함께 갱신된다. 저장하지 않은 칠하기·Undo·선택·3D 시점은 유지된다.
14. 교체 지형이 있는 채로 타일 크기·배경을 바꾸거나 스테이지를 교체해도 이전 타일 참조 오류·잔여 교체 외형이 없다. 창 종료·재열기에도 미리보기 객체가 남지 않는다.
15. CellPrefab + PrefabWithTiles 조건에서는 3D 타일 없이 배경과 모드 문제를 보인다. GridOutline 조건을 만족하고 새로고침하면 현재 작업본의 타일·교체 외형·높이가 다시 표시된다.

**UT-c 실행 기록 (2026-10-29, 사용자 "Ub 확인됨, UT-c 하는데 문서 수정은 나중에" → 코드 먼저, Uc 확인 문제 없음 — 사용자 2026-10-29, 새 시험 없음)**

- 새 파일 없음. 바뀐 파일: `CombatStageSoTiles.cs`(3D·높이용 타일 묶음 `CombatStageSoTileScene` — 맞춤이 성립할 때만, 종류 프리팹·칸 바탕 높이·맵 열쇠; 맞춤 사본 `YawCorrection`·`AnchorShift`), `CombatStageSoVisuals.cs`(`CombatStageSoSpace`가 바탕·지형 두 층 — `BaseAt`·`OverlayAt`·`SurfaceAt`, 바닥 교체는 프리팹 없어도 바탕을 바꾸고 한 칸에 여럿이면 낮은 값, `StandingPoint` 첫 칸부터, 덧그리기 `Corners`는 지형 높이만), `CombatStageSoPreview.cs`(`BuildTiles` — MapTileRoot·타일·회전·이동·칸 → 타일 표, `PlaceReplacement`, `ApplyHiddenTiles`, `Invalidate`, 맵 열쇠에 칸 공간 서명), `CombatLayoutWindow.SoMode.cs`(3D 칸 공간에 타일 묶음, 새로고침이 미리보기 무효화), `CombatLayoutWindow.SoTiles.cs`(`SoTileScene`).
- 계획과 다른 점·알아 둘 것:
  - 타일 묶음은 2D 띠와 같은 맞춤 결과(`soTileBand`)에서 만든다. 칸 → 타일 대응·칸 바탕 높이·3D 타일 숨김이 같은 `TryTileOfCell` 하나를 쓴다(P-UT12).
  - 맵을 다시 만들면 타일과 그 아래 교체 외형이 함께 없어지므로 숨김 기록을 비우고 배치를 반드시 다시 놓는다.
  - 높이·바닥 교체는 공개 대상(knownTo)을 보지 않는다. 게임은 Player에게 보이는 지형만 센다.
  - 토네이도는 다른 타일 위 지형처럼 칸 바탕에 놓는다(게임의 반 칸 위치·덮는 칸 최대 바탕은 B2로 남김).
  - MapTileRoot는 숨김(HideAndDontSave)으로 만들어 작업 씬을 더럽히지 않고 미리보기 장면의 배경 아래로 옮긴다.

### 8.4 UT-d 문서·공유 (UT9)

- [x] Td.1 사용법: §3 화면(보기 셋), 새 절 "배경과 바닥 타일"(방식·묶음·'타일' 보기·붓·기준 타일·2D 띠·모드·맞춤 문제), §7.4 View 값, §8.2 저장이 막는 타일 오류, §8.5 "이 도구는 아직 방식을 보이지 않으니" 줄 고침, §9 진입 문제(모드 조건 포함). 다섯 보완 중 사용자에게 필요한 GridOutline 전제·낭떠러지 빈 구멍·삭제/Undo 복원·교체 외형 보정 기준·Inspector 수정 후 '목록 새로고침'을 설명한다.
- [x] Td.2 공유 메모(모두 미전달로 표시): 전투 View 사본 공유 §1·§3·§5·§6(도구가 맞춤, 새 사본 줄, 이름 읽기 줄), 호진 공유 §4(Test 다시 등록·타일 방식), 민형님 공유 §2(새 파일 둘). 사본 목록에 `FieldCellSpacing.FromGridOutline`·`FieldTileReplacement.Apply`/`Restore`·`FieldTerrainPresenter.PlaceOnTile`/`PlaceInField`/`LocalPosition`의 도구 대응을 추가한다.
- [x] Td.3 설계 §12 구현 메모, 이 절 실행 기록, CLAUDE.md 현재 상태.

**UT-d 실행 기록 (2026-10-29, 사용자 "Uc 확인됨, 문서 정리", 코드 변경 없음)**

- 사용법(`Tool/SO모드 사용법.md`): §3.2 보기 셋·타일 보기 붓 줄, §3.3 View 줄, §5.5 타일 보기 단축키, §5.7 3D 타일·높이·덧그리기, 새 §5.10 "배경과 바닥 타일"(방식 표·바닥 타일 묶음·타일 보기·2D 띠·고른 칸 줄·맞춤 문제 알림·새로고침·공개 대상 차이, 확인은 Test 복제본으로), §8.2 "View 배경" 줄, §8.5 맞춤 줄·"이 도구는 아직 방식을 보이지 않으니" 고침, §9 진입 문구 넷·"Stage data not found"(미등록 스테이지로 진입)·타일이 안 보일 때.
- 공유 메모(모두 미전달): 전투 View 사본 공유 §1 사본 표(새 줄 일곱, 바뀐 줄 넷)·§3 이름 읽기(`visualMode`, View `mode`·`mapTiles`, 타일 세트)·§5·§6 머리에 "2026-10-29 해소(도구 쪽)", 호진 공유 §4 Test 다시 등록·강화 빠짐, 민형님 공유 머리 검증 줄·§2 새 파일 둘·보기 직렬화 필드 `soThreeD` → `soMapView`. Core 전달 문서는 Core 호출이 바뀌지 않아 고치지 않았다.
- 설계 §12 상태 줄·새 §12.8 구현 메모, 설계 머리 상태, 이 계획 §1 표·머리·§8 상태, CLAUDE.md.

<a id="85-계획에서-정한-것-확인-대상-p-utt1p-utt10"></a>
### 8.5 계획에서 정한 것 (확인 대상, P-UTT1~P-UTT10)

| ID | 정한 것 |
|---|---|
| P-UTT1 | 보기 선택은 직렬화된 bool `soThreeD` 대신 int `soMapView`(0 2D·1 3D·2 타일)로 바꾼다. 창 상태일 뿐이라 바꾼 뒤 처음 열 때 2D로 시작해도 된다 |
| P-UTT2 | '타일' 보기에서는 배치 도구 줄을 숨기고 타일 붓 줄을 보인다. 배치 편집은 2D·3D에서만 한다 |
| P-UTT3 | 타일 방식이 아닌 스테이지의 '타일' 보기는 안내 한 줄만 보인다. 방식은 View 패널에서 바꾼다 |
| P-UTT4 | 3D 배경을 방식대로 고르는 것(한 줄)은 UT-a에서 먼저 고친다. 타일 놓기는 UT-c다 |
| P-UTT5 | 타일 세트 목록은 형식 이름 문자열 검색(`t:CombatMapTileSetSO`)으로 찾고 이름순이다. 도구는 internal 형식을 참조하지 않는다 |
| P-UTT6 | 지형 외형 방식은 `visualMode` 열거 **이름**(`OnTile`·`ReplaceTile`)으로 읽는다(P-AZT5와 같은 방식). UT-b에서 읽기 시작해 2D "교체" 표시에 쓰고, UT-c에서 높이에 쓴다 |
| P-UTT7 | 3D 배경은 맵 키(방식·맵·보정·타일 값·필드 표시 모드·필드 크기·칸 간격)가 바뀌거나 '목록 새로고침'을 눌렀을 때 다시 만든다. 명시적 새로고침은 같은 GUID의 외부 에셋 변경도 반영하고 3D 시점을 유지한다(P-UT17) |
| P-UTT8 | Unity 확인은 `Test`를 '복제'한 미등록 스테이지로 한다. `Test` 원본·운영 표는 바꾸지 않는다. 확인 뒤 복제본 에셋은 사용자가 정리한다 |
| P-UTT9 | 검토 에이전트는 돌리지 않는다(§3.1). Phase마다 멈추고 Unity 확인을 받는다 |
| P-UTT10 | 화면 방향의 회전 단계는 회전 보정 Y를 90°로 나눠 반올림한 값(0~3)이다. 게임은 타일 가로축 각에서 반올림하는데, 회전 X·Z가 0이면 같다. X·Z가 있으면 기울기 문제로 알리고 방향은 Y만으로 정한다 |

<a id="86-다섯-보완-문서-반영-대응"></a>
### 8.6 다섯 보완 문서 반영 대응

2026-10-29 사용자 요청으로 문서에 반영했다. 아래는 요구사항 대응이며 구현·Unity 확인 완료 기록이 아니다. 동작 계약 원본은 설계 §12.4의 P-UT13~P-UT17이고, 실행 순서·확인은 이 계획이 맡는다.

| 보완 | 설계 | Task | Unity 확인 |
|---|---|---|---|
| GridOutline 전제 | UT7·UT10, P-UT13 | Tb.1·Tb.5·Tb.6, Tc.1 | Ub.10, Uc.15 |
| 프리팹 없는 빈 구멍 | UT6, P-UT14 | Tc.2·Tc.3·Tc.4, B3 문구 정정 | Uc.6·Uc.9 |
| 타일 숨김·복원 | UT6, P-UT15 | Tc.4 | Uc.10·Uc.11·Uc.14 |
| 교체 외형 좌표계 | UT6, P-UT16 | Tc.3 | Uc.12 |
| Inspector 수정 뒤 명시적 새로고침 | UT3·UT5, P-UT9·P-UT17 | Tb.7·Tc.5, P-UTT7 | Ub.11, Uc.13·Uc.15 |

- 문서·사본 공유는 Td.1~Td.3에서 반영하고, 모든 새 확인 항목은 사용자 Unity 확인 전까지 미확인으로 남긴다. 확인용 데이터는 복제본을 쓰며 원본 타일 세트·운영 설정을 바꾸지 않는다. 조건을 구성하지 못한 항목은 통과로 적지 않는다.
- 원칙 점검: 설계 §12.4의 책임 경계를 따른다. 새로고침은 기존 창에서 읽기·무효화를 연결하고, 표시 계산과 숨김 상태를 창 패널에 중복 구현하지 않는다. 런타임 서비스·FW 조립 계약·DT 모드 변경은 없다.
- 기존의 MSBuild·새 시험 코드·검토 에이전트 없음, Phase마다 보고 후 중단 규칙을 유지한다. B1·B2·C1은 이번 구현 범위에 더하지 않는다.

<a id="9-바닥-타일-칸별-회전-계획-tt-att-d"></a>
## 9 바닥 타일 칸별 회전 계획 (TT-a~TT-d)

- 설계: [설계 §13](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#13-바닥-타일-칸별-회전-tt1tt9)(TT1~TT9).
- 결정: 2026-10-29 사용자 — Q-TT1 A("이 작업에서 고침"), Q-TT2 A("봐서 불편하면 말할게요"), Q-TT3·Q-TT4 A, Q-TT5 바뀜(2D·3D에서도 회전, TT9).
- 기준 판: HEAD `ede23986a`(UT 코드·사용법·설계·계획 포함). UT의 ‘커밋 전’ 및 선행 커밋 권장 표시는 2026-10-31 이력 대조로 정정했다. TT·TO 후속은 `f5c8109d4`.
- 규칙: §3.1 그대로(MSBuild 없음, 새 시험 없음, 검토 에이전트 없음, Phase마다 멈추고 보고, 보고에 "컴파일 미확인"). 확인은 `Test`를 복제한 미등록 스테이지로 한다(P-UTT8과 같음).
- 범위 밖: 타일 세트·종류 열거, 지형 표시·높이 표·타일 맞춤·바닥 교체(게임), 종류 칠하기 방식(Q-TT4 A), DT 모드, B1·B2·C1.
- 상태: **P-TTT1~P-TTT12 그대로(2026-10-29 사용자 "그대로, TT-a 진행"). TT-a 코드 완료·Ua 확인(사용자), TT-b 코드 완료·Ub 확인(사용자), TT-c 코드 완료·Uc 확인(사용자), TT-d 문서 완료(§9.4). 커밋 `f5c8109d4`, 공유 미전달.**

바뀌는 파일: 게임 둘(`CombatMapTilesData.cs`·`StageMapTiles.cs`, 용근님), 도구 다섯(`CombatStageSoTiles.cs`·`CombatLayoutWindow.SoTiles.cs`·`CombatStageSoAssets.cs`·`CombatStageSoPreview.cs`·`CombatLayoutWindow.SoMode.cs`)과 작업본 형식 번호 한 줄(`CombatStageSoDocument.cs`). 새 파일 없음, asmdef 변경 없음(도구는 회전을 int로 다룸). 시험 코드는 두 게임 형식을 쓰지 않는다(2026-10-29 확인).

### 9.1 TT-a 게임 데이터·놓기 (TT1~TT3)

지금 에셋은 회전 칸이 비어 있어 화면 변화가 없다.

- [x] Ta.1 `CombatMapTilesData.cs` 입력 `CombatMapTilesInput`: `tiles` 다음에 `[SerializeField] private Rotation4[] rotations = Array.Empty<Rotation4>();`와 툴팁(순서는 `tiles`와 같음, 비우면 모두 R0, 위에서 본 시계 방향 90° 단위, 회전 중심은 타일 피벗이라 피벗이 타일 가운데인 프리팹만 칸에 맞음, 가로 ≠ 세로 크기면 90°·270° 금지). `Rotation4`의 using을 더한다.
- [x] Ta.2 `CreateData`: "모두 비면 null" 판정에 `rotations` 길이 0도 넣는다. 검사 셋을 종류 검사 뒤에 둔다 — 길이가 0도 가로 × 세로도 아니면 예외, 정의되지 않은 값(`System.Enum.IsDefined`)이면 칸 좌표를 담아 예외, 가로 ≠ 세로(P-TTT2)인데 R90·R270이 하나라도 있으면 칸 좌표를 담아 예외. 불변 값에는 길이 가로 × 세로 배열로 넘긴다(빈 입력이면 R0으로 채움).
- [x] Ta.3 불변 값 `CombatMapTilesData`: 생성자 끝 인자 `Rotation4[] rotations`(null 거절, 길이 검사), `RotationAt(column, row)`(`KindAt`과 같은 칸 검사·순서). 요약 주석에 회전 규칙 한 줄.
- [x] Ta.4 `StageMapTiles.Build`: 타일을 만든 뒤 `localRotation = Euler(0, RotationAt × 90, 0) × 프리팹 localRotation`(P-TTT3). 요약 주석에 회전 한 줄.

**Ua (Unity 확인)**

1. 컴파일된다.
2. `Test`(회전 칸 비어 있음)로 전투에 들어가면 타일이 지금과 같다.
3. Test 복제본 View를 인스펙터에서 열면 Map Tiles 아래에 `Rotations`(빈 목록)가 보인다.
4. 인스펙터에서 `Rotations`를 길이 17 × 17로 만들고 칸 몇 개를 R90·R180으로 두면, 그 스테이지로 진입할 때 그 칸 타일만 돌아가 있다.
5. `Rotations` 길이를 3처럼 틀리게 두면 부트 DB 생성(또는 도구 저장 검사)이 회전 수 문구로 멈춘다. 확인 뒤 비운다.

**TT-a 실행 기록 (2026-10-29, 사용자 "그대로, TT-a 진행" — P-TTT1~P-TTT12 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)**

- 바뀐 파일 둘(용근님 코드, 공유는 TT-d): `CombatMapTilesData.cs` — 입력 칸 `rotations`(`Rotation4[]`, `tiles` 다음, 툴팁), "모두 비면 null"에 회전 길이, 검사 함수 `CreateRotations`(길이 0 → 모두 R0, 길이 다르면 예외, 정의 안 된 값 예외, 가로 ≠ 세로(`Mathf.Approximately`)인데 R90·R270이면 칸 좌표 예외), 불변 값 생성자 끝 인자 `rotations`(null·길이 검사)·`RotationAt`·요약 한 줄. `StageMapTiles.cs` — 타일마다 `Euler(0, r × 90, 0) × 프리팹 localRotation` 한 줄과 주석.
- 생성자를 부르는 곳은 `CreateData` 하나라 다른 호출을 고칠 곳이 없다. 시험 코드는 두 형식을 쓰지 않는다.
- 계획과 다른 점 없음. 회전 검사는 종류 검사 뒤(세트 종류 확인 다음)에 돈다.
- 지금 에셋(Test 포함)은 `rotations` 칸이 YAML에 없어 빈 목록으로 읽히고, 모두 R0이라 화면 변화가 없다.

### 9.2 TT-b 도구 작업본·'타일' 보기 돌리기 (TT4~TT7)

- [x] Tb.1 `CombatStageSoTiles.cs` 작업본 `CombatStageSoTileLayout`: `public int[] rotations = Array.Empty<int>();`, `RotationAt(column, row)`(빈 배열이면 0), `SetRotation(column, row, value)`(빈 배열이면 가로 × 세로 0 배열을 먼저 만듦), `HasRotations`(0이 아닌 칸이 있음). `Resize`가 종류와 같은 규칙으로 회전도 옮긴다(새 칸 0, 빈 배열이면 빈 배열 그대로). `IsEmpty`에 회전 길이 0 조건.
- [x] Tb.2 `CombatStageSoDocument.cs`: `CurrentFormat` 1 → 2(UT8 길: 옛 작업본·초안은 에셋에서 다시 읽음).
- [x] Tb.3 `CombatStageSoAssets.cs`: 필드 이름 상수 `Rotations = "rotations"`, `ReadTiles`가 `intValue`로 읽음, `WriteTiles`는 `HasRotations`면 가로 × 세로 길이로, 아니면 빈 배열로 씀(P-TTT1).
- [x] Tb.4 `CombatStageSoTileFit` 화면 방향(TT7, 원본 자리 주석). 이미 가진 축(`columnsAlongX`·`columnPositive`·`rowPositive`)으로:
  - `ScreenTurnSign`: 화면에서 본 시계 방향이 저장 값에서도 시계 방향이면 +1, 배경이 뒤집혀 반대면 −1(화면 축 행렬의 부호).
  - `ScreenFrontOf(rotation)`: 저장 회전 r인 타일의 R0 앞쪽(타일 루트 +Z)이 화면에서 가리키는 쪽(위·오른쪽·아래·왼쪽).
  - `ScreenAngleOf(rotation)`: 화면에서 본 시계 방향 각(0·90·180·270). 표시 글자와 각 지정에 쓴다.
- [x] Tb.5 `CombatLayoutWindow.SoTiles.cs` 모드: `soTileAnchorMode`(bool) → `[SerializeField] int soTileMode`(0 칠하기·1 회전·2 기준 타일, P-TTT5). 붓 줄 툴바 셋. 회전 모드면 종류 고르기·'모두 이 종류로' 대신 회전 줄(Tb.7).
- [x] Tb.6 고른 타일(TT5·P-TTT4): 창 필드 `HashSet<Vector2Int> soTileSelection`(배열 좌표, 직렬화 안 함). 가로·세로·세트·스테이지가 바뀌면 비운다(Undo 뒤 범위 밖 칸은 버림). 입력은 설계 TT5 표다.
  - 클릭 = 하나만, 끌기 = 지나간 타일로 새로.
  - Ctrl(또는 ⌘, P-TTT6)+클릭 = 더하기/빼기, Ctrl+끌기 = 더함.
  - Shift+끌기 = 사각형으로 새로, Ctrl+Shift+끌기 = 사각형 더함.
  - `SelectAll` 명령(Ctrl+A) = 타일 맵 전부, Esc = 끄는 중이면 그 끌기만 취소·아니면 비움.
  - 고르기는 작업본을 바꾸지 않아 Undo가 없다.
- [x] Tb.7 돌리기(TT6): 회전 줄 = "고른 타일 n개" · '↺ 90°' · '↻ 90°' · '0°'·'90°'·'180°'·'270°'(화면 각 지정, P-TTT7). 단축키 R = ↻, Shift+R = ↺(회전 모드에서만). 한 번 = `SoChange` 한 번, 바뀌는 칸이 없으면 Undo 없이 알림. 화면 시계 방향 d는 저장 값에 `ScreenTurnSign × d`로 더한다(4로 나눈 나머지). 각 지정은 화면 각 → 저장 값으로 바꿔 넣는다.
- [x] Tb.8 표시(TT7·P-TTT8): 타일 칸마다 `ScreenFrontOf` 쪽 변에 굵은 띠, 회전이 0이 아니면 종류 이름 뒤에 화면 각 글자("Test1 90°"). 고른 타일은 하늘색 테두리. 범례에 "앞쪽(0°일 때 위)"·"고른 타일".
- [x] Tb.9 안내: 타일 보기 아래 회전 모드 입력 한 줄. 가로 ≠ 세로 크기인데 90°·270°가 있으면 경고 한 줄(저장 검사가 막음, Q-TT3 A). 그 경우 돌리기 버튼은 0°·180°만 받는다.

**Ub (Unity 확인, Test 복제본)**

1. 컴파일된다. 업데이트 전에 창에 스테이지를 열어 뒀다면 "에셋에서 다시 읽음" 알림이 뜬다(형식 2).
2. '타일' 보기 모드 줄이 [칠하기 | 회전 | 기준 타일]이고, 칠하기·기준 타일은 지금과 같다.
3. 회전 모드: 클릭 하나, 끌기, Ctrl+클릭(더하기·빼기), Ctrl+끌기, Shift+끌기, Ctrl+Shift+끌기, Ctrl+A, Esc가 설계 TT5 표대로 고른다. 필드 자리 밖 타일도 고를 수 있다.
4. R·Shift+R·버튼으로 돌리면 고른 타일마다 앞쪽 띠가 시계·반시계로 돌고 각 글자가 바뀐다. 한 번 = Undo 한 번이다.
5. 저장하면 인스펙터 `Rotations`가 17 × 17이 되고, 화면 90°로 돌린 칸은 저장 값 R90이다(Test 배경은 뒤집힘이 없어 화면 각 = 저장 값. 뒤집힌 배경만 반대). 그 스테이지로 들어가면 같은 모양이다(3D는 TT-c). 모두 0°로 돌려 저장하면 `Rotations`가 빈 목록이 된다.
6. 가로를 18로 바꾸면 회전한 칸이 같은 자리에 남고 고른 타일이 비워진다.
7. 타일 크기를 (2, 3)으로 바꾸면 90° 버튼이 막히고, 이미 90°인 칸이 있으면 경고가 뜨고 저장 검사가 막는다. 확인 뒤 되돌린다.
8. TT-a Ua 4처럼 인스펙터에서 넣어 둔 회전은 툴로 열면 그대로 보인다(검은 띠·각 글자).

**TT-b 실행 기록 (2026-10-29, 사용자 "Ua 확인, TT-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)**

- 바뀐 파일 넷(새 파일 없음):
  - `CombatStageSoTiles.cs`: 작업본 `rotations`·`HasValidRotations`·`HasRotations`·`RotationAt`·`SetRotation`, `Resize`가 회전도 옮김, `IsEmpty`에 회전. 맞춤 계산에 화면 방향 `ScreenTurnSign`·`ScreenFrontOf`·`ScreenAngleOf`·`StoredOf`·`Turned`.
  - `CombatStageSoDocument.cs`: `CurrentFormat` 2.
  - `CombatStageSoAssets.cs`: 필드 이름 `TileRotations`, `ReadTiles`·`WriteTiles`(모두 0°면 빈 목록).
  - `CombatLayoutWindow.SoTiles.cs`: 모드 셋 `soTileMode`(옛 `soTileAnchorMode` 삭제), 고른 타일 `soTileSelection`·`SyncSoTileSelection`, 회전 줄 `DrawSoTileRotateBar`, 돌리기 `TurnSoTiles`·`SetSoTilesAngle`·`ApplySoTileRotations`, 고르기 `PickSoTile`·`PickSoTiles`, 키 `HandleSoTileSelectionKeys`(TT-c의 2D·3D 도구도 이것을 부름), 앞쪽 띠 `DrawSoTileFront`, 입력·안내·범례.
- 계획과 다른 점(사용자 확인 대상):
  - **화면 각은 "프리팹 원래 모습에서 화면에서 본 시계 방향으로 돈 양"이다**(`ScreenAngleOf = (부호 × 저장 값) × 90`). 계획 P-TTT8의 범례 "앞쪽(0°일 때 위)"처럼 화면 절대 방향으로 두면, 배경을 돌린 스테이지(Test −90°)는 돌리지 않은 타일이 모두 "270°"처럼 보여 헷갈린다. 그래서 0° = 돌리지 않음, 각 글자는 돌린 타일에만, 범례는 "검은 띠 = 프리팹 앞(+Z) 쪽"으로 했다. 뒤집힘이 없으면 화면 각 = 저장 값이다(Ub 5 고침).
  - 회전 목록 길이가 틀린 에셋(인스펙터에서 잘못 넣음)은 툴이 0°로 보이고 경고를 띄운다. 타일 하나를 돌리면 0° 목록으로 다시 채워진다(옛 잘못된 값은 버림).
  - 맞춤 문제가 있어도 회전 모드 고르기·돌리기는 된다(P-TTT7, 축은 회전 보정·배율 부호만 씀).

### 9.3 TT-c 2D·3D 도구·3D 회전 (TT8·TT9)

- [x] Tc.1 3D 회전(TT8): `CombatStageSoPreview.BuildTiles`가 타일을 만든 뒤 Ta.4와 같은 한 줄(원본 자리 주석). 맵 다시 만들기 열쇠(`CombatStageSoTileScene.Signature`)는 작업본 JSON이라 회전도 이미 들어간다.
- [x] Tc.2 도구(TT9): `SoTool` 끝에 `TileRotate`, `SoToolLabels` 끝에 "타일 회전"(P-TTT9). 칠하기 도구가 아니다(`IsPaintTool` false). 이 도구를 고르면 도구 줄 아래에 Tb.7 회전 줄을 같은 함수로 보인다.
- [x] Tc.3 칸 → 타일 고르기: `HandleSoPointer`의 누름·끌기·뗌에서 도구가 `TileRotate`면 칸을 `soTileBand.Fit.TryTileOfCell`로 배열 좌표로 바꿔 Tb.6과 같은 고르기 함수를 부른다(사각형은 칸 사각형이 덮는 타일 모두). 타일 방식이 아니거나 맞춤이 안 되면 `SetSoNotice` 안내만, 타일 없는 칸은 건너뜀. 2D·3D가 같은 `HandleSoPointer`라 한 곳이다.
- [x] Tc.4 단축키: `TileRotate` 도구면 R·Shift+R은 타일 돌리기, Esc는 고르기 비우기, Ctrl+A는 필드가 덮는 타일 전부(P-TTT10). 배치 단축키(Delete·복사·붙여넣기)는 받지 않는다.
- [x] Tc.5 표시: 2D·3D 공용 테두리 `SoFrames`에 고른 타일이 덮는 필드 칸 사각형을 하늘색으로 더한다(타일 화면 좌표 → 칸 사각형, 필드 밖은 잘라냄). 고른 칸 줄(`DrawSoCellTile`)에 "회전 90°"(화면 각)를 덧붙인다.
- [x] Tc.6 다른 도구로 바꿔도 고른 타일은 남는다('타일' 보기와 같은 목록). 2D 띠에는 회전을 그리지 않는다(TT8).

**Uc (Unity 확인, Test 복제본)**

1. 3D에서 '타일' 보기로 돌린 타일이 실제로 돌아가 있다. 저장한 뒤 그 스테이지로 전투에 들어가면 같은 모양이다.
2. 2D·3D 도구 줄에 '타일 회전'이 있고, 칸 클릭·끌기·Ctrl·Shift로 고르면 그 칸의 타일들이 하늘색 칸 테두리로 보인다(한 타일이 칸 여럿이면 그 칸 모두).
3. R·Shift+R·버튼으로 돌리면 3D 타일이 바로 돌고, '타일' 보기로 가면 같은 타일이 골라져 있고 각도가 맞다.
4. 타일 방식이 아닌 스테이지(예: `Combat_Normal_A` 복제본)에서 '타일 회전' 도구로 누르면 안내만 나온다.
5. '선택' 등 다른 도구로 바꾸면 배치 편집·단축키가 지금과 같다.
6. 2D에서 칸을 누르면 오른쪽 고른 칸 줄의 '바닥 타일'에 "회전 90°" 같은 각이 붙는다(0°도 적힘).

**TT-c 실행 기록 (2026-10-29, 사용자 "Ub 확인, TT-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)**

- 바뀐 파일 넷(새 파일 없음):
  - `CombatStageSoPreview.cs`: `BuildTiles`에 게임과 같은 타일 회전 한 줄(TT8).
  - `CombatStageSoTiles.cs`: 맞춤 계산에 `ArrayToScreen`(ScreenToArray의 반대)·`TryFieldCellsOfTile`(타일이 덮는 필드 칸, 필드 밖은 잘라냄).
  - `CombatLayoutWindow.SoMode.cs`: 도구 열거·이름 끝에 `TileRotate`("타일 회전"), 도구 설명 줄, `HandleSoPointer`의 누름·끌기·뗌 갈래 한 줄씩, 키 갈래(배치 단축키 대신 타일 키), `SoFrames`가 타일 테두리를 더함, 맵 그리기에서 회전 줄 호출.
  - `CombatLayoutWindow.SoTiles.cs`: 새 영역 "2D·3D '타일 회전' 도구" — 회전 줄 `DrawSoTileToolBar`, 준비 판정 `SoTileToolReady`, 칸 입력 `PressSoTileCell`·`DragSoTileCell`·`ReleaseSoTileCell`, 키 `HandleSoTileToolKeys`(TT-b의 `HandleSoTileSelectionKeys`를 부름), 필드 위 타일 `FieldSoTiles`, 테두리 `AddSoTileFrames`. 고른 칸 줄에 화면 각.
- 계획과 다른 점·더한 것:
  - 회전 줄은 도구 줄 바로 아래가 아니라 공격 범위 줄 다음(타일 대응을 만든 뒤)에 보인다. 타일 대응이 맵을 그릴 때 만들어져서다.
  - 고른 타일 테두리는 '타일 회전' 도구일 때만 그린다. 다른 도구로 바꿔도 목록은 남고 다시 고르면 보인다(Tc.6).
  - 타일이 없는 칸(필드가 타일 맵을 넘친 칸)을 누르면 안내를 띄우고, Ctrl 없이 눌렀으면 고르기를 비운다.
  - 이 도구로 칸을 누르면 그 칸이 오른쪽 '고른 칸'이 된다(각을 바로 볼 수 있게).
  - 맞춤 문제가 있으면 2D·3D 도구는 안내만 하고, '타일' 보기에서는 계속 고르고 돌릴 수 있다고 알린다.

### 9.4 TT-d 문서·공유

- [x] Td.1 사용법(`Tool/SO모드 사용법.md`) §5.10에 "타일 돌리기"('타일' 보기 회전 모드·2D·3D 타일 회전 도구·입력 표·화면 각과 저장 값·가로 ≠ 세로 금지·피벗 조건), §8.2 저장 검사 문구, §9 막히는 경우.
- [x] Td.2 전투 View 사본 공유(`handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md`): 새 절 "게임 파일 변경(용근님)" — 두 파일의 바뀐 줄·뜻·검사·지금 에셋 영향 없음, 사본 표에 회전 줄(`BuildTiles` 회전, 화면 방향 계산), 필드 이름 `rotations`.
- [x] Td.3 설계 §13 구현 메모, 계획 §1 표·§9 실행 기록, CLAUDE.md 상태 줄.

### 9.5 계획에서 정한 것 (확인 대상, P-TTT1~P-TTT12)

| ID | 정한 것 |
|---|---|
| P-TTT1 | 게임 칸 이름은 `rotations`, 형식 `Rotation4[]`, `tiles` 바로 다음. 도구는 모두 0이면 빈 배열로 저장한다(에셋 변경 최소, 지금 에셋과 같은 모양) |
| P-TTT2 | 가로 ≠ 세로는 `tileSize.x`와 `tileSize.y`를 `Mathf.Approximately`로 비교한다. 배경 배율 X·Z가 달라 보이는 크기가 다른 경우는 게임이 막지 않고 도구도 따로 알리지 않는다(정수배 검사가 칸 맞춤을 이미 봄) |
| P-TTT3 | 회전은 프리팹 자기 회전 뒤에 타일 루트 위 방향으로 곱한다(`Euler(0, r×90, 0) × 프리팹 localRotation`). 회전 중심은 피벗이고, 피벗 조건은 툴팁·사용법으로만 안내한다 |
| P-TTT4 | 고른 타일은 창 필드(배열 좌표 집합)이고 직렬화하지 않는다. 다시 컴파일하면 비워진다. 고르기에는 Undo가 없다 |
| P-TTT5 | '타일' 보기의 bool `soTileAnchorMode`를 int `soTileMode`로 바꾼다. 창 상태라 바꾼 뒤 처음 열 때 칠하기로 시작해도 된다 |
| P-TTT6 | Ctrl은 `e.control || e.command`(맥 ⌘). Ctrl+A는 에디터가 보내는 `SelectAll` 명령으로 받는다(Ctrl+C·V를 명령으로 받는 지금 방식과 같음) |
| P-TTT7 | 화면에서 고르는 각(0·90·180·270)과 ↻·↺는 모두 **화면에서 본** 시계 방향이다. 저장 값은 맞춤 축으로 바꾼다. 맞춤 축은 회전 보정·배율 부호만으로 정해져 GridOutline·정수배 문제가 있어도 계산된다 |
| P-TTT8 | '타일' 보기 표지는 앞쪽 변 띠 + 0이 아닐 때 각 글자. 고른 타일 테두리는 하늘색(상자 선택 색과 구분되게 조금 다른 색) |
| P-TTT9 | 맵 도구 열거 끝에 `TileRotate`("타일 회전")를 더한다. 옛 도구 순번은 그대로라 창에 남은 도구 선택이 바뀌지 않는다 |
| P-TTT10 | 2D·3D '타일 회전' 도구의 Ctrl+A는 필드가 덮는 타일 전부, '타일' 보기의 Ctrl+A는 타일 맵 전부다 |
| P-TTT11 | 3D에서 고른 타일은 칸 테두리로만 보인다(타일 오브젝트에 색을 입히지 않음, 미리보기 장면을 늘리지 않음) |
| P-TTT12 | 용근님께 넘길 변경 정리는 새 문서 대신 전투 View 사본 공유에 절로 넣는다(같은 사람·같은 기능) |

## 10 3D 판 밖 타일 고르기 계획 (TO-a·TO-b)

- 설계: [설계 §14](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#14-3d에서-판-밖-타일-고르기-to1to6)(TO1~TO6). 결정: 2026-10-29 사용자 "권장안대로 진행" → Q-TO1~Q-TO3 A.
- 규칙: §3.1 그대로(MSBuild 없음, 새 시험 없음, Phase마다 멈추고 보고, "컴파일 미확인"). 도구 파일만, 새 파일 없음.
- 상태: **TO-a·TO-b 완료(Ua 확인 문제 없음 — 2026-10-29 사용자), 커밋 `f5c8109d4`.**

### 10.1 TO-a 코드

- [x] Oa.1 `CombatStageSoTiles.cs` 맞춤 계산: `ScreenTileOfCell(cell)`(내림 나눗셈, 범위 검사 없음), `TryScreenTileOfCell`은 그것 + 타일 맵 안 검사(음수 칸 허용, TO1), `CellsOfScreenTile(screen)`(자르지 않은 칸 사각형), `TryFieldCellsOfTile`은 그것을 판 안으로 자름.
- [x] Oa.2 `CombatStageSoVisuals.cs`: `CombatStageSoSpace.RawCellAt` 공개(판 밖도 칸 번호).
- [x] Oa.3 `CombatLayoutWindow.SoMode.cs`: 3D는 '타일 회전' 도구일 때 칸 함수 둘을 `RawCellAt`으로(TO2). `SoFrames(grid, clipTiles)` — 2D는 자르고 3D는 자르지 않음(TO4). 도구 설명 줄 문구(TO6 일부).
- [x] Oa.4 `CombatLayoutWindow.SoTiles.cs`: 판 밖 누름은 '고른 칸'을 바꾸지 않음, 타일 없는 자리 안내 문구, Shift+끌기는 타일 맵 안으로 붙인 타일 사각형(2D·3D 공통, TO3)·사각형 테두리도 그 타일 사각형, 3D Ctrl+A = 타일 맵 전부(Q-TO1 A), 테두리 자르기 인자.

**Ua (Unity 확인, Test 복제본, 3D)**

1. 컴파일된다.
2. 3D에서 '타일 회전' 도구로 판 밖 타일을 누르면 하늘색 테두리가 그 타일에 보이고, R로 돌면 3D 타일이 돈다. 오른쪽 '고른 칸'은 바뀌지 않는다.
3. 판 안에서 판 밖까지 끌기·Shift+끌기·Ctrl+끌기가 판 밖 타일도 고른다. 사각형은 타일 맵 끝에서 멈춘다.
4. 3D Ctrl+A는 타일 맵 전부, 2D Ctrl+A는 판 위 타일만 고른다. 2D에서는 판 밖 테두리가 그려지지 않는다.
5. 다른 도구(선택·칠하기)는 3D 판 밖 클릭을 지금처럼 받지 않는다.
6. 타일 맵 밖(배경만 있는 곳)을 누르면 안내만 나온다.

**TO-a 실행 기록 (2026-10-29, 사용자 "권장안대로 진행", 컴파일 미확인, 새 시험 없음, Ua 대기)**

- 바뀐 파일 넷(새 파일 없음): `CombatStageSoTiles.cs`(`TryScreenTileOfCell` 내림 나눗셈·음수 허용, `ScreenTileOfCell`·`CellsOfScreenTile`·`TryClipToField`, `TryFieldCellsOfTile`은 자르기를 씀), `CombatStageSoVisuals.cs`(`RawCellAt` 공개), `CombatLayoutWindow.SoMode.cs`(3D 칸 함수 둘이 '타일 회전' 도구일 때 `RawCellAt`, `SoFrames(grid, clipTiles)` — 2D true·3D false, 도구 설명 줄), `CombatLayoutWindow.SoTiles.cs`(판 밖 누름은 '고른 칸' 그대로, 안내 문구, 타일 사각형 `SoTileCellBox`, 테두리 `AddSoTileFrames(frames, clip)`·`AddSoTileFrame`, Ctrl+A 3D = 타일 맵 전부).
- 계획과 다른 점 없음. Shift+끌기 사각형은 2D도 타일 사각형으로 셈이 바뀌었지만 고르는 타일은 같다(판 안 칸 사각형이 덮는 타일 = 두 끝 타일 사이 타일). 2D 사각형 테두리는 칸 사각형 대신 덮는 타일 경계(판 안으로 자름)로 그린다.


### 10.2 TO-b 문서

- [x] Ob.1 사용법 §5.10 "타일 돌리기"의 필드 밖 줄·Ctrl+A 줄, §9 '타일 회전' 줄.
- [x] Ob.2 설계 §14 구현 메모, 이 절 실행 기록, CLAUDE.md 상태 줄.

<a id="11-후속-보완-계획-fu-afu-c"></a>
## 11 후속 보완 계획 (FU-a~FU-c)

- 설계 원본: [설계 §15](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#15-후속-보완-배치-검사토네이도공개-대상-fu1fu6). 여기에는 Task·확인 항목·진행 상태만 둔다.
- **2026-10-31 현재 상태: FU-a·FU-b 코드 및 FU-c 안내·사용법·공유 문서 반영 완료. 컴파일·Unity 미확인, 공유 미전달.** 사용자 후속 구현 지시에 따라 반영했으며 고지대 B1은 계속 보류한다.
- 설계 당시 이력(2026-10-31, 구현 전 재검토): 숨은 배치의 묶음 편집은 Fb.3·Fb.5/Ub.9~Ub.11, 생성 전 호환 배치 정리는 Fa.1·Fa.3·Fa.5/Ua.7~Ua.8에 연결했다. 문서만 갱신했으며 모든 구현·Unity 확인은 미착수/미확인이다.
- 기준 판: 로컬 HEAD `bd0447662`. 최신 원격 조회·병합이나 Unity 실행을 한 것은 아니다.
- 범위: 고정 배치 검사, 토네이도 위치·높이, 3D 공개 대상 보기, 안내·기록 정정. **고지대 B1은 보류**이며 기존 고지대 기능·Core 규칙을 삭제하지 않는다. 스테이지 완전 삭제, 약한 지반 숫자(C3), 특성·전투 시뮬레이션·전투 되돌리기는 범위 밖이다.
- 진행 규칙: 구현 요청 뒤 FU-a → FU-b → FU-c. Phase마다 멈추고 보고한다. §3.1의 MSBuild·새 시험 코드·검토 에이전트 없음, 사용자 Unity 확인을 유지한다. 설계 당시 문서 작업에는 코드·에셋·사용법·공유 문서 변경이 없었다. 구현에서도 Core·런타임·DB SO·asmdef·DT 모드 수정은 이 계획에 포함하지 않는다.

### 11.1 FU-a 배치 검사 (FU1·FU2)

**목표:** 저장할 배치가 벽·낭떠러지 때문에 전투 진입에서 거절되는 경우를 검사·저장·등록 전에 같은 오류로 알린다. 전체 전투 준비 성공을 보장하는 검사는 아니다.

예정 파일은 `Assets/ProjectL/Script/Tool/Editor/` 아래다.

| Task | 예정 변경 | 완료 조건 |
|---|---|---|
| [x] Fa.1 검사 입력 읽기 | `CombatStageSoAssets.cs`에서 작업본과 생성 목적이 반영된 임시 복제본의 정의·회전된 점유 칸·현재 환경과 호환 배치를 읽어 정규화 | 설계 §15.2.1에 따라 일반 저장/배치 복제는 호환 배열 보존, 배치 비우기 생성만 검사 전에 비움. 원본 SO를 수정하지 않고 `knownTo`·3D 보기와 무관하다 |
| [x] Fa.2 한정된 검사 함수 | 새 내부 `CombatStageSoPlacementChecks.cs`(+ `.meta`)에 고정 적·적 구조물·참조 있는 본부의 점유 칸과 벽/낭떠러지 칸 교차 검사 | 종류·배치 이름·문제 좌표를 반환한다. 앵커 밖 점유 칸도 잡는다. Core 원본 이름·검토 기준을 주석에 남긴다 |
| [x] Fa.3 기존 검사에 조립 | `CombatStageSoValidation.cs`의 `CheckDocument`에 `clearLegacyPlacements`(기본 false)를 전달받아 작업본 반영 후 검사 전에 처리하고, 새 검사를 연결 | 검사·저장·등록 및 생성 경로가 같은 최종 데이터 기준으로 검사한다. `GetData()`와 새 검사 모두 비우기 이후 실행한다. 기존 SO 검사는 유지하고 오류 시 에셋·표를 쓰지 않는다 |
| [x] Fa.4 경계 기록 | 구현 메모에 적용 대상·제외 대상·변경 파일을 기록 | 무작위 배치·미사용 메인 캐릭터 위치는 추측하지 않으며 나머지 전투 진입 규칙은 Core에 남는다 |
| [x] Fa.5 생성 경로 연결 | `CombatStageSoCreation.cs`의 `Create`에서만 검사 인자에 `!copyPlacements`를 전달. 임시/최종 비우기는 기존 `CombatStageSoAssets.ClearLegacyPlacements` 재사용 | 새 파일 생성 전에 최종 저장 상태로 검사한다. 배치 복제는 호환 배치까지 보존. 일반 저장의 빈 개수/ID 목록을 삭제 신호로 쓰지 않는다 |

**Ua — 사용자 Unity 확인(전부 미확인, 복제한 미등록 스테이지):**

- [ ] Ua.1 벽과 낭떠러지 각각에 고정 적·적 구조물·본부를 겹치면 검사에 이름·좌표가 나오고 저장·등록도 거절한다. 정상 칸으로 옮기면 해당 오류가 사라진다.
- [ ] Ua.2 여러 칸을 차지하는 배치의 앵커는 정상이고 회전 후 가장자리 점유 칸만 겹쳐도 잡는다. 회전을 바꿔 겹침을 없애면 해소된다.
- [ ] Ua.3 Player에 비공개인 벽/낭떠러지도 잡으며 3D 두 보기·2D 중 어디에서 검사하든 결과가 같다.
- [ ] Ua.4 숨김 호환 벽/지형 데이터가 있는 복제본에서도 잡는다. 검사 전후 호환 필드·원본 에셋은 유지된다. 조건을 준비하지 못하면 미확인으로 남긴다.
- [ ] Ua.5 오류 상태의 저장·등록 전후 에셋과 표가 그대로이며, 기존 SO 검사에서 잡던 벽/환경·오브젝트·장애물 오류도 유지된다.
- [ ] Ua.6 무작위 적·무작위 시작 토네이도는 임의 위치로 검사하지 않는다. 미사용 메인 캐릭터 위치 때문에 새 오류를 내지 않는다. 검사 통과 안내가 전체 전투 진입 보장을 약속하지 않는다.
- [ ] Ua.7 호환 벽/낭떠러지가 본부와 겹치는 복제용 틀에서 ‘배치 비우기’로 새로 만들면, 제거될 지형 때문에 거절되지 않는다(나머지 정의는 정상). 새 에셋의 호환 배열은 비고 본부 자리·참조는 이어받으며 틀 원본은 그대로다.
- [ ] Ua.8 같은 틀을 ‘배치까지 복제’하면 겹침 오류로 중단하고 새 Root·Combat·View 파일을 만들지 않는다. 겹침 없는 틀의 배치 복제는 호환 배열까지 보존한다. 일반 저장도 이를 지우지 않으며, 배치 비우기에 남는 다른 SO 오류는 여전히 생성 전에 거절한다.

### 11.2 FU-b 3D 표시 (FU3~FU5)

**목표:** 토네이도의 위치·높이를 맞추고 공개 대상에 따른 미리보기 차이를 명시한다. 두 작업은 표시 파일을 공유하지만 토네이도 계산과 공개 판정 함수를 분리한다. 고지대 이웃 모양은 구현하지 않는다.

| Task | 예정 변경 | 완료 조건 |
|---|---|---|
| [x] Fb.1 토네이도 계산 | `CombatStageSoVisuals.cs`에 원래 타일 바탕 조회·2×2 최대 높이 계산을 두고 `CombatStageSoPreview.cs`에서 위치에 적용 | 반 칸 이동, 판 경계·음수 높이·타일 밖 0 처리, 오프셋과 기존 배율을 설계 FU3대로 적용한다. 교체 지형 높이가 섞이지 않는다 |
| [x] Fb.2 공개 대상 판정 | `CombatStageSoVisuals.cs`의 공통 표시 판정과 높이 계산 입력을 정리 | `Names.owner`의 Player 이름으로 판정한다. 전체/Player 모드에서 외형·높이·바닥 교체·타일 숨김이 같은 지형 집합을 쓴다. 오브젝트도 공개 여부를 따른다 |
| [x] Fb.3 보기·편집 경계 | `CombatLayoutWindow.SoMode.cs`에 보기 설정·제외 수·숨은 선택 안내와 공통 편집 대상 판정을 연결. 클릭·단축키·지우개·상자·전체 뒤집기/대칭·고스트·대상 수에 적용 | 설계 §15.2.2대로 Player 기준 화면 편집은 표시 대상만 처리하고 패널의 명시적 개별 편집은 유지. 대상 없는 작업은 무변경. 숨은 배치가 든 붙여넣기는 전체 배치/2D 전환 안내 후 중단한다 |
| [x] Fb.4 캐시·복원 | `CombatStageSoPreview.cs`의 배치 캐시에 보기 기준을 포함하고, 모드·knownTo·Undo/Redo·새로고침 때 공간/표시 계산을 함께 갱신 | 자신이 숨긴 타일만 복원하고 새 표시 집합으로 다시 숨긴다. 겹친 교체 지형이 남아 있으면 해당 타일은 계속 숨긴다. 종료·전환·재컴파일 시 기존 해제를 지킨다 |
| [x] Fb.5 묶음 연산 대상 분리 | `CombatStageSoDocument.cs`의 삭제 대상 조회·사각형 지우기·복사·묶음 이동·뒤집기·대칭 복사에 명시적 대상 판정을 받는 경로 추가. `SoMode`가 공개 판정을 전달하며 기본 호출은 기존 전체 대상 | 실제 작업본/사전 확인 복제본 각각에서 대상을 구한다. 원본 객체 참조를 복제본 편집에 쓰지 않는다. 숨은 배치도 충돌 검사에는 포함하고, 2D·전체 배치·타일 작업과 기존 본부 제한은 유지한다 |

**Ub — 사용자 Unity 확인(전부 미확인):**

- [ ] Ub.1 X/Z 칸 간격이 다른 필드에서 토네이도가 각 축의 반 칸만 이동한다. 2D 표식·저장 앵커·기존 배율·외형 회전·오프셋은 유지된다.
- [ ] Ub.2 토네이도가 덮는 2×2 원래 타일 높이를 다르게 해 최대값을 따른다. 전부 음수이면 가장 높은 음수를 쓰며 0으로 올리지 않는다. 판 밖 칸은 제외하고 판 안 타일 밖 칸은 0으로 계산한다.
- [ ] Ub.3 같은 영역에 낭떠러지·약한 지반 교체가 있어도 토네이도 높이는 원래 타일 바탕 기준이다. 무작위 시작은 고정 위치로 임의 표시하지 않는다.
- [ ] Ub.4 공개 목록이 Player/Enemy/양쪽/빈 목록인 고정 지형·오브젝트를 비교한다. 전체 배치는 모두, Player 기준은 Player 포함만 표시한다. 잘못된 참조·열거 이름은 조용히 공개 처리하지 않는다.
- [ ] Ub.5 비공개 ReplaceTile 지형을 Player 기준에서 제외하면 높이·타일 숨김도 함께 해제된다. 같은 타일을 가리는 다른 표시 대상이 남으면 계속 숨긴다. 원래 비활성인 타일을 켜지 않는다(P-UT15).
- [ ] Ub.6 숨은 배치가 3D 클릭 후보에 끼지 않고 같은 층에 새 배치로 덮어써지지 않는다. 이전 선택은 숨김 안내와 함께 유지되고 2D·목록에서 편집 가능하다. 보기 전환만으로 저장 필요 표시나 Undo 항목이 생기지 않는다.
- [ ] Ub.7 공개 대상 편집·Undo/Redo·같은 GUID 에셋의 명시적 새로고침 후 외형·높이·타일 숨김·제외 수가 함께 갱신된다. 창 닫기·스테이지 전환·재컴파일 뒤 미리보기 객체나 숨김 상태가 남지 않는다.
- [ ] Ub.8 기존 타일 칠하기·회전·판 밖 선택과 기본 지형 표시가 유지된다. B1 고지대 이웃 모양은 여전히 보류이며 Player 기준을 ‘전투 화면 완전 재현’으로 안내하지 않는다.
- [ ] Ub.9 같은 칸/상자에 Player 공개·비공개 지형/오브젝트를 섞고 클릭·지우개(단일/끌기/사각형)·Delete·R·상자 삭제·묶음 이동·복사·뒤집기·대칭 복사를 확인한다. Player 기준은 표시 대상만 처리하고 숨은 배치의 좌표·값·개수는 그대로다. 상자 없는 전체 뒤집기/대칭도 표시 대상만이며 문구·고스트·대상 수가 일치한다.
- [ ] Ub.10 숨은 배치만 있는 선택에서 화면 편집은 안내만 하고 Dirty·초안·Undo가 생기지 않는다. 공개 여부 편집·보기 전환·Undo/Redo 뒤에는 현재 대상이 적용된다. 숨은 배치와의 기존 충돌은 계속 막으며, 패널에서 명시적으로 고른 숨은 배치는 안내를 보고 개별 편집할 수 있다.
- [ ] Ub.11 전체 배치/2D에서 복사한 비공개 배치를 Player 기준으로 붙이면 전체 배치/2D 전환 안내 후 아무것도 추가하지 않는다. 전환 후에는 공개 목록을 보존해 기존 방식으로 붙인다. 2D·전체 배치의 기존 묶음 연산과 타일 보기·타일 회전은 그대로이며, 사전 확인 복제본에서 실패한 묶음 연산은 실제 작업본을 바꾸지 않는다.

### 11.3 FU-c 안내·문서 (FU6)

| Task | 예정 변경 | 완료 조건 |
|---|---|---|
| [x] Fc.1 공격 범위 안내 | `CombatLayoutWindow.SoMode.cs`의 범위 안내 문구 | ‘특성·버프/디버프에 따른 사거리 보정은 반영하지 않은 기본 범위’를 명시한다. 사거리 계산 코드는 확장하지 않는다 |
| [x] Fc.2 사용법 | `Assets/ProjectL/Script/Tool/SO모드 사용법.md`의 범위·3D·검사·생성·문제 안내 | 저장 오류/남은 Core 검사, 배치 비우기와 배치 복제의 검사 차이, 토네이도 식, 공개 대상 두 보기·화면/패널 편집 범위·붙여넣기 제한·고지대 보류를 설명한다 |
| [x] Fc.3 공유 메모 | 기존 전투 View·민형님 공유 메모와 Core 전달 문서 §42 | 사본 원본·검사 두 조건·새 파일 책임·c40fd37b의 기존 도구 변경을 기록한다. Core 변경을 요청하거나 구현한 것으로 쓰지 않는다. 전달은 사용자, 미전달 상태 유지 |
| [x] Fc.4 실행 기록 | 이 계획과 설계의 상태·파일·사용자 확인·이후 커밋 기록 | 구현 완료·Unity 확인·커밋·공유 전달을 구분한다. 불가능했던 확인은 이유와 함께 남긴다. 기존 UT·TT·TO 확인을 FU 확인으로 재사용하지 않는다 |

**Uc — 문서·화면 대조(미확인):**

- [ ] Uc.1 범위 안내와 사용법이 같은 한계를 설명하고 실제 특성 적용을 약속하지 않는다.
- [ ] Uc.2 검사·저장·등록 오류의 이름·좌표와 사용법 예시가 맞는다. 기존 ‘낭떠러지는 경고만’ 문구가 현재 안내에 남지 않는다.
- [ ] Uc.3 두 보기의 기본값·공개 판정·숨김/높이·선택·묶음 편집·붙여넣기 제한 및 배치 비우기/복제 검사 차이를 실제 동작과 대조한다. 고지대 보류 및 토네이도 무작위 시작의 한계를 유지한다.
- [ ] Uc.4 공유 메모의 파일·원본·미전달 상태와 실행 기록이 맞으며 완료하지 않은 Task를 완료로 올리지 않는다.

### 11.4 원칙 점검과 이번 문서 기록

- SSOT: 행동 계약은 설계 §15, 작업·검증 상태는 이 절. Core/View 원본에서 파생한 검사·표시 사본의 위치와 대조 기준을 남긴다. 새 SO 저장 필드나 규칙 원본을 만들지 않는다.
- SOLID·독립성: 입력 읽기(`Assets`), 교차 검사(`PlacementChecks`), 검사 조립(`Validation`), 공간/공개 계산(`Visuals`), 객체 수명(`Preview`), 생성 목적 전달(`Creation`), 작업본 묶음 연산(`Document`), UI 연결(`SoMode`)을 나눈다. 공개 판정을 공유하되 표시/편집 대상 필터와 전체 데이터 기반 충돌 판정을 분리한다. 각 기능 변경은 해당 Task와 관련 확인으로 제한한다. 공통 높이·숨김 변경일 때만 관련 3D 회귀를 함께 확인한다.
- JMH/FW: 전역 서비스·Core 조립·새 어셈블리 참조는 추가하지 않는다. 기존 Preview 소유자의 생성·복원·해제 책임과 실패 시 원본을 쓰지 않는 저장 경계를 유지한다.
- 2026-10-31 문서 반영: B1 보류, C1 오류 기준 정정, B2 높이 보완, B4 공개 기준, D5 공격 범위 안내를 설계 FU1~FU6과 Task에 연결했다. UT·TT·TO의 이미 존재하는 커밋 기록을 정정했다. **코드·에셋·사용법·공유 문서는 수정하지 않았고 Unity·MSBuild·테스트를 실행하지 않았다.**
- 2026-10-31 재검토 후속: 사용자 요청으로 설계 §15.2.1~§15.2.2와 위 Task·수동 확인 항목을 보완했다. **고지대 B1은 계속 보류, 코드·에셋 변경 없음.** 새 생성/편집 경계는 아직 구현하지 않았다.

### 11.5 FU-a 구현 기록 (2026-10-31)

- 코드 반영: `CombatStageSoAssets.CheckFixedPlacements`가 적용된 복제본의 필드·배치·호환 벽/지형을 읽고, 새 `CombatStageSoPlacementChecks.cs`(+ `.meta`)가 고정 적·구조물·본부의 회전 점유 칸과 벽/낭떠러지의 교차를 검사한다. 공개 대상은 검사 결과에 영향을 주지 않는다.
- 연결: `CheckDocument`에서 기존 검사와 함께 호출하므로 검사·저장·등록·생성에 적용한다. `clearLegacyPlacements`는 기본 false이며, `Creation.Create`의 배치 비우기만 true다. 임시 복제본에서 호환 배열을 지운 뒤 검사하고 원본은 유지한다.
- 검증: 호출 경로·입력 형식 대조 및 `git diff --check` 확인. **컴파일 미확인, Unity Ua.1~Ua.8 미확인.** MSBuild·테스트 실행·새 테스트 작성·커밋·push 없음.
- Phase 경계: FU-a 코드 반영 후 보고하고 멈춘다. 사용자 Unity 확인과 Phase 인수 전이며, FU-b·FU-c 및 고지대는 진행하지 않았다.

### FU-b 구현 기록 (2026-10-31)

- 토네이도: 판 안 2×2 원래 타일 바탕의 최대 높이와 반 칸 이동을 Preview에 적용. 기존 배율·오프셋·회전·저장 앵커 유지.
- 공개 기준: 3D ‘Player 기준’ 토글(기본 꺼짐 = 전체 배치). 공통 IsShown 판정을 높이·외형·타일 숨김·화면 편집에 적용하고 배치 캐시 키에 기준을 포함. 작업본에 보기 설정을 저장하지 않음.
- 편집: 클릭·지우개·삭제·회전·상자 이동·복사·뒤집기·대칭·고스트는 표시 대상만 사용. 충돌은 전체 배치로 확인. 숨은 배치가 있는 붙여넣기는 전체 배치/2D 전환 안내. 패널의 명시적 개별 편집은 숨김 안내와 함께 유지.
- 변경: Tool/Editor의 Assets·Visuals·Preview·Document·SoMode. 새 파일·Core·런타임·에셋 변경 없음. FU-c는 미착수, 고지대 B1은 보류.
- 검증: 호출부·필터 전달·캐시 키 정적 대조. **컴파일 미확인, Unity Ub.1~Ub.11 미확인.** MSBuild·테스트·커밋·push 없음. FU-b Phase 보고 지점이며 사용자 인수 전.

### FU-c 반영 기록 (2026-10-31)

- 범위 안내: SoMode 화면에 특성·버프/디버프 사거리 보정 미반영을 명시. 범위 계산은 변경하지 않음.
- 사용법: 기본 범위의 한계, 토네이도 반 칸·2×2 원래 바탕 높이, 전체/Player 기준과 화면·패널 편집 범위, 숨은 배치 붙여넣기 제한, 벽/낭떠러지 저장 오류와 나머지 Core 진입 검사, 배치 비우기/복제 차이를 갱신.
- 공유: 전투 View §9, 민형님 §5, Core 전달 §42.2a에 원본·사본·책임·검사 한계를 기록. 모두 미전달. 고지대 B1은 계속 보류.
- 정적 대조: 문구·사용법·호출 경로 및 문서 차이 검사. 컴파일·Unity Ua/Ub/Uc의 실제 화면 확인은 미완료이며 기존 검증으로 대체하지 않음. 새 테스트·MSBuild·커밋·push 없음. 이번 코드는 안내 문자열만 변경했고 에셋·Core·런타임은 수정하지 않음.

### 11.6 FU-a~FU-c 검토 후 보완 (2026-10-31)

- [x] Player 기준 오류 복구: `Visuals.TryIsShown`이 판정 오류를 반환하며 오류 배치는 표시·화면 편집에서 제외한다. `SoMode`는 sourceId·좌표·원인을 표시하고 고른 칸/무작위 지형 패널은 유지한다. 잘못된 공개 목록은 명시적 비우기 버튼으로 복구하며, 사용자가 누를 때만 기존 Undo·초안 경로로 변경한다. 정상 숨김과 판정 오류를 구분하고 공개 판정 원본은 `IsShown` 하나로 유지한다.
- [x] 문서 현재 상태: FU 상태 표·후속 목록·현재 설명을 반영 완료로 맞췄다. 구현 전 기록과 Phase별 당시 미착수 기록은 이력으로 보존한다.
- [ ] Unity 확인: Player 기준에서 정의 누락/잘못된 공개 대상이 있어도 원인과 패널이 보이고, 오류 배치는 화면 연산에서 제외되는지 확인. 패널 수정·목록 복구 후 오류 해제, Undo/Redo, 정상 숨김 배치, 2D 전환을 확인한다.
- 검증: 변경 호출부와 문서 상태 정적 대조 및 `git diff --check`. **컴파일·Unity 미확인**. 새 시험·MSBuild·커밋·push 없음. Core·런타임·에셋 변경 없음. 고지대 B1 보류 유지.

<a id="12-편집-ux-보완-계획-uxauxc"></a>
## 12 편집 UX 보완 계획 (UX-a~UX-c)

- 설계 원본: [설계 §16](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#16-편집-ux-보완-오류-위치작업-상태숨김-안내-ux1ux3). 목표는 오류 탐색·현재 작업 상태·숨김 복구 동선 개선이다. Unity IMGUI와 기존 SO 모드 구조를 유지한다.
- **2026-11-02 UX-a~UX-c 코드와 해당 사용법·공유 메모 반영, 컴파일·Unity 미확인.** UX-c는 UXR-b 뒤 사용자 요청으로 진행했다(§12.9). FU 및 UX-a/b·UXR-a/b의 기존 검증 대기를 대체하지 않는다. 전체 화면 사용법 갱신·창 내부 도움말은 UXR-c/d에 남긴다.
- 구현 요청 뒤 UX-a → UX-b → UX-c 순서로 진행하고 각 Phase 끝에 보고·정지한다. §3.1의 MSBuild·새 시험·검토 에이전트 없음, 사용자 Unity 확인·직접 커밋 정책을 유지한다. Core·런타임·DB SO·DT 모드 변경 없음. **고지대 B1 계속 보류.**

### 12.1 UX-a 검사 결과·위치 이동

예정 파일은 `Assets/ProjectL/Script/Tool/Editor/` 아래다.

- [x] Xa.1 새 `CombatStageSoIssue.cs`(+ `.meta`): 메시지·선택적 문제 칸·검사 작업본 대상(층/인덱스 또는 본부) 값 및 Root/작업본 개정에 묶인 결과 상태를 정의. 상세 결과가 원본이며 메시지 목록은 투영한다.
- [x] Xa.2 `CombatStageSoPlacementChecks.cs`·`CombatStageSoAssets.cs`·`CombatStageSoValidation.cs`: 검사 입력의 대상 정보를 상세 결과에 보존. 일반 SO 오류는 위치 없는 결과로 유지하고 문자열 호환 호출도 같은 상세 검사에서 메시지를 얻는다. `CombatStageSoCreation.cs`의 기존 검사 소비 경로가 동일하게 차단하는지 대조한다(규칙·배치 비우기 정책 변경 없음).
- [x] Xa.3 새 partial `CombatLayoutWindow.SoUx.cs`(+ `.meta`)와 `CombatLayoutWindow.SoMode.cs` 연결: 기존 `SoProblems`의 직접 좌표 검사도 상세 결과로 수집. 검사·저장·등록이 같은 결과 모델을 소비하고, 생성 실패는 기존 생성 안내 유지. 문자열에서 좌표를 추출하지 않는다.
- [x] Xa.4 결과 행의 `위치 보기 (2D)`와 선택·대상 강조·패널 스크롤 연결. 문제 칸/앵커 구분, 판 밖 대상 직접 편집과 X·Y 일괄 적용, 맵 스크롤의 일회성 위치 이동, 위치 없는 오류·무작위 대상 처리, 작업본 변경 시 결과 무효화와 클릭 직전 Root/개정 확인. 기존 SoChange·Undo/Redo·열기·복원·새로고침 수명에 연결한다. 결과/이동 때문에 Dirty·초안을 만들지 않는다.

**Ua — 사용자 Unity 확인(미확인):**

- [ ] Ua.1 회전된 여러 칸 배치의 가장자리 겹침 행을 누르면 문제 칸과 해당 배치 패널이 일치한다. 같은 칸의 여러 배치를 구분하고 이전 상자/끌기가 다음 편집에 남지 않는다.
- [ ] Ua.2 위치 없는 GetData/맵 오류는 원문만 표시한다. 판 밖 문제는 임의 칸으로 보정하지 않으며 유효 앵커만 별도 이동한다. 양 축 모두 판 밖인 대상도 배치 수정 패널에서 좌표를 함께 적용해 복구한다. 큰 맵·작은 창에서 대상 칸이 보이고 이동 후 수동 스크롤은 유지된다. Player 기준에서 숨겨진 고정 배치도 명시적 2D 이동으로 찾을 수 있다.
- [ ] Ua.3 검사 후 삭제·이동·목록 재정렬·Undo/Redo·다른 스테이지 열기·초안 복원·새로고침·재컴파일 뒤 옛 결과가 다른 배치를 가리키지 않는다.
- [ ] Ua.4 검사·저장·등록의 오류 수/차단 결과 및 생성의 배치 비우기/복제가 FU-a와 같다. 검사 거절 시 에셋·표가 바뀌지 않는다(파일 쓰기 도중 실패의 롤백은 UX 범위 밖). 보기 이동만으로 저장 표시·Undo·초안이 생기지 않는다.

### 12.2 UX-b 작업 상태 표시

- [x] Xb.1 `CombatLayoutWindow.SoUx.cs`: 설계 §16.3의 상태 줄을 기존 값/결과 수명에서 계산. `SoMode.DrawSoHeaderStatus`에 연결하고 기존 일회성 성공·실패 알림은 유지한다. 독립적인 Dirty/검사 성공 bool을 추가하지 않는다.
- [x] Xb.2 미검사·변경 후 재검사 필요·현재 검사 결과 전환을 작업본 변경/열기/Undo·저장 흐름과 대조. 데이터 변경 없는 2D/3D/Player 전환과 위치 이동은 검사 결과를 유지한다. 기존 저장 확인/버리기 대화상자·초안 정책을 바꾸지 않는다. 설계 §16.6의 맵 규칙 실패/조회 실패/조건부 경고/사용자 취소/쓰기 실패/표 변경 실패 전환표를 적용한다.

**Ub — 사용자 Unity 확인(미확인):**

- [ ] Ub.1 열린 스테이지 없음 → 열기 → 편집 → 검사 → 저장 흐름에서 이름·키·저장·검사 상태가 일치한다. 좁은 창에서도 상태와 기존 버튼이 보이고 색 없이 뜻을 이해할 수 있다.
- [ ] Ub.2 검사 통과 후 수정·Undo/Redo는 재검사 필요, 검사 통과 후 쓰기 실패는 미저장/실패 안내를 유지한다. 초안 복원·목록 새로고침·재컴파일 때 옛 통과 표시가 남지 않는다. 공통 검사 통과 뒤 맵 규칙 위반/조회 실패가 통과로 표시되지 않고 확인 취소·보스 교체 조건·저장 후 표 실패도 구분된다.
- [ ] Ub.3 Player 토글을 기억한 상태로 2D/타일 보기에 가도 현재 Player 필터 적용으로 잘못 표시하지 않는다. 보기 이동만으로 Dirty/초안/Undo가 생기지 않는다.

### 12.3 UX-c 숨김 안내·사용법 기록

- [x] Xc.1 `CombatLayoutWindow.SoUx.cs`·`CombatLayoutWindow.SoMode.cs`: 정상 비공개/판정 오류/위치 미정 안내와 요약 수를 분리. 현재 보기와 무관한 `CombatStageSoVisuals.TryIsShown(..., true, ...)` 진단과 기존 FU 편집기를 재사용. 고정 오류는 UX-a 대상 계약에 연결하고, `SoWorkspace.DrawSoDetails`에서 진단을 조립한다.
- [x] Xc.2 `전체 배치로 보기`/`2D에서 수정`을 공통 보기 전환 경로로 연결. 선택 보존·미확정 끌기 취소·Preview 캐시/높이/타일 숨김 갱신을 기존 경로로 처리. 붙여넣기 중단 안내에도 전환을 제공하되 붙여넣기를 자동 실행하지 않는다.
- [x] Xc.3 사용법과 기존 전투 View·민형님 공유 메모에 위치 지원 한계·저장/검사 구분·숨김 안내·도구 파일 책임을 기록. 공유 미전달, 코드 반영/Unity 확인/커밋 상태를 구분했다(§12.9).

**Uc — 사용자 Unity 확인(미확인):**

- [ ] Uc.1 Player/Enemy/양쪽/빈 공개 목록·정의 누락·잘못된 공개 값·무작위 시작을 비교해 분류와 요약 수가 맞고 오류에도 패널이 유지된다. 무작위 시작은 가짜 좌표 이동을 제공하지 않는다.
- [ ] Uc.2 전체 보기로 전환하면 선택 유지, 높이/외형/자신이 숨긴 타일 복원이 함께 갱신된다. 공개 목록·충돌 검사·초안·Undo는 바뀌지 않는다. 전체/2D 전환만으로 오류 수·원인이 사라지지 않는다. 잘못된 정의가 있는 전체 3D에서도 패널이 유지된다. 정상 비공개와 오류가 해결되었다는 안내를 혼동하지 않는다.
- [ ] Uc.3 비공개가 든 복사본은 Player 기준에서 중단되고, 전환 버튼은 보기만 바꾼다. 사용자가 다시 실행한 붙여넣기만 기존 규칙대로 반영된다. 타일 보기·판 밖 타일 회전은 유지된다.
- [ ] Uc.4 사용법/공유 문서가 실제 UI와 일치하고 고지대 보류·컴파일/Unity 미확인·미전달 상태를 사실대로 기록한다.

### 12.4 설계 당시 문서 검토 기록 (2026-10-31, 구현 전)

UX1 → Xa/Ua, UX2 → Xb/Ub, UX3 → Xc/Uc로 연결했다. 오래된 결과 대상, 판 밖/위치 없는 오류, 저장 실패, 숨김과 판정 오류, 무작위 시작을 확인 항목에 포함했다. SSOT·SOLID·JMH/FW·기능 독립성은 설계 §16.5를 기준으로 대조했다. 이번 변경은 설계·계획 두 문서뿐이며 코드·에셋·사용법·공유 문서 변경, Unity/MSBuild/시험 실행, 커밋·push는 없다.

### 12.5 UX-a 구현 기록 (2026-10-31)

- 새 파일: `CombatStageSoIssue.cs`·`CombatLayoutWindow.SoUx.cs`와 각 `.meta`. 변경: `CombatStageSoPlacementChecks.cs`·`CombatStageSoAssets.cs`·`CombatStageSoValidation.cs`·`CombatLayoutWindow.SoMode.cs`. 생성의 `CheckDocument` 문자열 반환은 상세 결과의 메시지 투영으로 유지하므로 Creation 자체 수정 없음.
- 상세 검사: 일반 예외는 위치 없는 항목, 고정 배치 겹침·맵 밖 배치는 문제 칸과 층/인덱스 대상, 판 밖 아군 구역 칸은 좌표만 제공한다. 임시 복제본의 배열 순서가 작업본과 같으며 객체 참조를 가져오지 않는다. 규칙·오류 수·생성 전 비우기/보존 조건은 그대로다.
- 이동: `위치 보기 (2D)`·`배치 보기 (2D)`·`배치 수정 (맵 밖)`. 미확정 배치/타일 끌기 취소, 상자 해제, 문제 칸과 원인 배치 분리, 원인 패널 상단 강조·중복 편집 제거, 맵과 패널의 일회성 스크롤. 판 밖 배치는 좌표를 함께 입력하고 기존 배치 검사·Undo로 적용한다. 본부 삭제 금지 등 기존 패널 제약 유지.
- 수명: Root GUID·작업본 개정에 결과를 연결. SoChange·Undo/Redo·열기/복원·새로고침·모드 전환·저장 작업본 정리에서 폐기하며 재컴파일 시 직렬화하지 않는다. 일반 칸 선택은 오류 대상 강조만 해제한다. 이동 자체는 Dirty/초안/Undo를 만들지 않는다.
- 정적 검토: 새 반환 타입/문자열 호환 호출부, 임시 복제본 배열 대응, 결과 수명, UI 대상/복구 경로 및 `git diff --check` 대조. **컴파일·Unity Ua.1~Ua.4 미확인**. MSBuild·시험 실행·새 시험 작성·커밋·push 없음. 실제 스크롤/IMGUI·Undo 동작 확인은 사용자 Unity 확인으로 남긴다.
- 이번 Phase 끝 보고 지점. UX-b(맵 규칙까지의 검사 상태 전환)·UX-c(보기 독립 오류 진단/숨김 안내)와 사용법·공유 갱신은 미착수다. Core·런타임·DB SO·기존 고지대 기능은 변경하지 않았으며 고지대 B1 보류를 유지한다.

### 12.6 UX-b 구현 기록 (2026-10-31)

- 작업 상태: `SoUx.DrawSoWorkStatus`가 기존 `soModified`, 작업본 이름/키, 현재 2D/3D/Player/타일 보기와 검사 상태를 텍스트·HelpBox로 표시한다. 이름과 상태는 줄바꿈 가능한 스타일을 사용하고 기존 툴바 밖에 둔다. 새로운 Dirty 저장값은 없다.
- 검사 수명: `CombatStageSoCheckResult`에 NeedsRecheck/Checking/Complete 단계와 조건부 경고를 추가했다. null은 미검사, 수정·Undo/초안 복원은 재검사 필요, 새 스테이지 열기는 미검사다. 새로고침은 이전 결과를 폐기하며 이전 검사 성공 알림도 제거한다. 실제 2D/3D/Player/타일 보기 전환 및 UX-a 위치 이동은 상태를 바꾸지 않는다.
- 결과 확정: 공통 오류는 바로 오류 결과, 오류 없는 공통 검사는 맵 규칙까지 확인한 후 통과/조건부 경고로 확정한다. 새 변경의 맵 위반·조회 실패·등록 입력 읽기 실패는 위치 없는 오류다. 보스 교체/이미 깨진 맵은 경고로 보존하고 기존 확인 절차·허용 조건은 유지한다. 취소는 실행하지 않았다는 작업 알림이며 오류를 추가하지 않는다.
- 저장·표 변경: 파일 쓰기 실패 시 기존 미저장 플래그를 해제하지 않는다. 공유 작업본 정리가 없으면 쓰기 전 검사 결과를 유지하고, 정리/새로고침 시에는 재검사 필요로 전환한다. 에셋 저장 후 표 변경 실패는 기존 구분 안내를 유지한다. 해제는 공통 저장 검사를 새로 강제하지 않으며 과거 결과를 무효화하고 해제 작업 안내만 표시한다. 부분 쓰기의 롤백은 추가하지 않았다.
- 변경 파일: `CombatLayoutWindow.SoUx.cs`, `CombatStageSoIssue.cs`, `CombatLayoutWindow.SoMode.cs`, 설계·계획. 새 파일·Core·런타임·DB SO·고지대 변경 없음. UX-c는 미착수다.
- 검증: 검사/저장/등록/해제의 분기와 결과 수명, UI 문구의 정적 대조 및 `git diff --check`. **컴파일·Unity Ub.1~Ub.3 미확인**. MSBuild·시험 실행·새 시험·커밋·push 없음. 좁은 창의 실제 줄바꿈과 확인 창/Undo 동작은 사용자 Unity 확인으로 남긴다. UX-b Phase 보고 지점이며 고지대 B1 보류 유지.

### 12.7 복사·붙여넣기 단축키 포커스 수정 (2026-10-31)

- 사용자 보고: 선택 영역 복사/붙여넣기 버튼은 되지만 Ctrl+C/V는 동작하지 않는다. 검사를 실행해도 같음.
- 원인 경로: 버튼과 단축키는 같은 CopySo/PasteSo를 호출하지만 HandleSoKeys는 keyboardControl이 0일 때만 받는다. 기존 HandleSoPointer는 맵 선택 때 hotControl만 잡고 이전 숫자·텍스트 입력 칸 포커스를 해제하지 않았다. 검사 결과는 이 단축키 조건에 사용하지 않는다.
- 수정: 2D/3D 공통 HandleSoPointer의 유효 칸 왼쪽 MouseDown에서 GUI.FocusControl(null)을 호출한다. 입력 칸 편집 중 단축키 보호와 복사 대상·충돌·공개 대상 정책은 유지한다. 새 상태·규칙·서비스 없이 기존 포커스 해제 방식을 사용하며 Core·에셋·고지대·UX-c 변경은 없다.
- 정적 확인: 맵 클릭 → 포커스 해제 → 영역/목적지 선택 → 기존 Copy/Paste 명령 경로와 버튼 경로를 대조했다. **컴파일·Unity 동작 미확인**. MSBuild·시험 작성/실행·커밋·push 없음.
- [ ] 사용자 확인: 숫자/텍스트 입력 뒤 선택 도구로 Shift+드래그 → Ctrl+C → 빈 목적지 칸 클릭 → Ctrl+V. 2D/3D에서 버튼과 같은 결과이며, 입력 칸 안에서는 일반 텍스트 복사/붙여넣기가 유지되는지 확인한다.

### 12.8 새 배치 공개 대상 기본값 (2026-10-31)

- 사용자 지정: 새 지형·오브젝트의 공개 대상 기본값은 Player + Enemy. None·Neutral은 선택하지 않는다. 기존 배치와 복사본의 공개 목록은 보존한다(설계 §15.2.2).
- 구현: Document의 공통 Add에서만 기본 목록을 생성하고, SoMode는 현재 열거 이름 목록을 전달한다. 클릭/드래그와 PaintBox → TryAdd가 같은 Add를 사용한다. 열거 이름으로 순번을 구하고 각 배치마다 새 목록을 만든다. 직렬화 필드 초기값·읽기·복사 경로는 변경하지 않는다.
- 범위: 도구 Document·SoMode, 설계·계획·사용법. SSOT는 공통 신규 생성 경로이며 Core·런타임·기존 에셋·고지대·UX-c 확장은 없다.
- 검증: 변경된 호출부와 신규 생성·읽기·복제 경로를 정적으로 대조했고 `git diff --check`를 확인했다. **코드 반영, 컴파일·Unity 미확인**. MSBuild·시험 작성/실행·커밋·push 없음.
- [ ] 사용자 확인: 지형·오브젝트를 클릭/드래그/Shift+사각형으로 놓으면 Player·Enemy만 켜진다. 하나의 공개 대상을 바꿔도 다른 새 배치는 그대로이며, 비공개 배치의 복사/대칭/스테이지 복제와 저장·다시 읽기는 기존 설정을 보존한다.

### 12.9 UX-c 구현 기록 (2026-11-02)

- 사용자 요청 범위: 숨김 안내와 복구 동선. **Xc.1~Xc.3 코드·문서 반영, 컴파일 미확인, Unity Uc.1~Uc.4 대기, 커밋 전.** UXR-c/d는 진행하지 않았다. 이전 Ua/Ub와 UXR-a/b 확인을 완료로 간주하지 않는다.
- 진단: 속성 상단 `공개·숨김 안내`에서 정상 비공개/판정 오류를 중복 없이 집계한다. 무작위 시작 지형 수는 위치 미정이라는 별도 축이다. Visuals의 `TryIsShown(..., true, ...)`를 사용하므로 전체/2D/타일/설정에서도 오류 수·원인이 남고, 수정·Undo·새로고침 뒤 다시 계산한다. 저장 검사 결과에 합산하지 않는다.
- 복구: 고정 오류 행은 층/인덱스와 Root/개정을 확인하고 UX-a의 2D 선택·강조·일회 스크롤·판 밖 좌표 함께 적용을 사용한다. 검사 결과 객체와 별개로 대상 이동의 Root/개정만 공유한다. 무작위 시작은 좌표 버튼 없이 기존 무작위 지형 패널을 연다. 필드가 유효하지 않으면 필드 설정을 안내한다. FU의 정의·공개 목록 명시 수정/비우기를 유지한다.
- 보기/붙여넣기: 전체 배치와 2D 전환은 기존 선택·상자·클립보드를 보존하고 공통 입력 취소 경로를 사용한다. 위치 찾기는 기존 UX-a처럼 대상 선택으로 바꾸고 상자를 해제한다. Player 토글도 같은 취소 경로로 통일했다. Preview의 기존 기준 키와 RaiseSurfaces가 높이·외형·타일 숨김을 갱신한다. 붙여넣기 거절 안내에 전환 버튼과 재실행 안내를 붙였으며 대기 명령/자동 붙여넣기는 없다.

- 변경/책임: `SoUx`는 진단 안내·상태·대상 이동, `SoMode`는 기존 맵/붙여넣기의 호출 연결, `SoWorkspace`는 속성 영역 조립이다. 새 파일·서비스·asmdef 없음. Visuals/Document/Preview/SoTiles 및 Core·런타임·DB SO·운영 에셋은 이번에 변경하지 않았다. 사용법과 View/민형님 공유 메모는 해당 UX 범위만 갱신했고 **미전달**이다.
- 원칙 점검: 공개 규칙은 Visuals, 저장 판정은 기존 검사, Dirty/초안/Undo는 기존 창·명령이 원본(SSOT). UI 조립·진단·이동·편집 책임을 나누고(S), 선택적 위치 없는 결과도 수용하며(O), 기존 검사/편집 계약을 유지하고(L), 위치값/대상만 전달하고(I), 창이 규칙 제공자에 의존한다(D). JMH/FW 서비스·Factory와 Preview 해제 수명은 유지한다. 고지대·토네이도 식·타일 생성 구현을 다시 수정하지 않아 기능 변경의 독립성을 유지했다.
- 정적 확인: 이번 시작 시점과의 정적 대조에서 검사/저장/등록·복사·뒤집기·대칭·개별 편집·공개 목록 등 기존 메서드 24개와 타일 파일이 유지됨을 확인했다. 표시 진단의 Root/개정 수명, 전체 보기의 오류 패널 경계, 미확정 입력 취소와 Preview 갱신 키, 자동 붙여넣기 없음 및 차이/텍스트 형식을 확인했다. `git diff --check`와 변경 8개 파일의 UTF-8/LF 검사가 통과했다. **컴파일·Unity는 미확인**이며 실제 IMGUI 스크롤/전환/Undo/단축키는 Uc 사용자 확인 항목이다. 새 시험·MSBuild·검토 에이전트·커밋·push 없음. **고지대 B1 계속 보류, UX-c Phase 끝 보고 후 정지.**

## 13 낙석 값 패널·터널 경고 계획 (RF-a~RF-c)

- 설계 원본: [설계 §17](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#17-낙석-값-패널터널-자리-경고-rf1rf6). 결정 Q-RF1~Q-RF3 모두 A(2026-11-02 사용자).
- 상태(2026-11-02): **RF-a~RF-c 완료(Ub 문제 없음 — 사용자, 커밋 전, 공유 메모 미전달).** 새 시험·MSBuild 없음, 컴파일·재생은 사용자 Unity 확인. Phase 끝마다 보고하고 멈춘다.
- 바꾸는 파일은 모두 `Assets/ProjectL/Script/Tool/Editor/`이고 새 `.cs`는 없다. SO 코드·에셋·런타임은 고치지 않는다. 낙석 값 칸을 고쳐 저장하면 그 스테이지 에셋이 바뀐다.

### 13.1 계획에서 정한 것 (확인 대상)

| 번호 | 내용 | 이유 |
|---|---|---|
| P-RF1 | Q-RF2 A의 "낙석 값:" 머리말은 작업본 검사에서 만든다. 켜져 있으면 작업본 값으로 `RockfallGimmickSpec`을 먼저 만들어 보고, 실패하면 머리말을 붙여 문제에 넣은 뒤 **복제본의 낙석만 꺼서** 나머지 검사를 잇는다 | 게임 파일을 안 고치고 같은 오류가 두 번 나오는 것을 막는다. 낙석 오류가 다른 오류를 가리지도 않는다. 규칙 원본은 Core 그대로다. asmdef는 `ProjectL.Core.Contracts`를 이미 참조한다 |
| P-RF2 | 터널 경고는 검사 결과(`soCheck.Warnings`)에만 낸다. 패널에서 매 프레임 프리팹을 훑지 않는다 | Tick·검사는 필요할 때만. 상단 작업 상태에도 경고 수가 함께 나온다 |
| P-RF3 | 터널 둘 이상은 `Check`의 문제로 넣어 낙석이 꺼져 있어도 저장을 막는다. 프리팹이 비었으면 터널 검사를 건너뛴다(비어 있는 칸은 View 자체 검사가 막음) | 설계 RF5 |
| P-RF4 | 오프셋 줄은 추가 때 작업본에 없는 가장 앞 칸을 넣고, 마지막 한 줄은 지우기를 막지 않는다. 비면 저장 검사가 Core 문구로 막는다 | 규칙 사본을 만들지 않는다(RF4) |

### 13.2 RF-a 값 입출력·패널

1. `CombatStageSoDocument.cs`: 값 묶음 `CombatStageSoRockfall`(`enabled`, 턴 둘, 돌 수 둘, 손실 %, 묶임 턴, `List<Vector2Int> footprintOffsets`), 칸 `rockfall`, `CurrentFormat = 4`와 주석 한 줄(옛 초안은 지금 규칙대로 에셋에서 다시 읽음).
2. `CombatStageSoAssets.cs`: 필드 이름 상수, `Read`·`Write`에 값과 `ReadCell`·`WriteArray`·`WriteCell`로 오프셋.
3. `CombatLayoutWindow.SoMode.cs`: `DrawSoStageValues`의 레일 칸 줄 아래에 `DrawSoRockfall()`. "낙석 사용" 토글, `DisabledScope` 안에 값 칸(`SoInt`·`SoChange`), 오프셋 줄(x·y·지우기)과 추가 버튼. 맵 SO 방식이면 안내 한 줄(Q-RF3 A).

Unity 확인 Ua:

- [ ] `Test`를 열면 낙석 사용이 켜져 있고 값이 2·2·3·5·20·1, 오프셋 (0,0)이다.
- [ ] `Stage_Test2`는 꺼져 있고 값이 회색이며, 켜면 입력할 수 있다.
- [ ] 값을 바꾸고 저장·다시 열면 남는다. 껐다 켜도 값이 그대로다.
- [ ] Undo·Redo가 값·오프셋 모두에 듣는다.
- [ ] 저장 뒤 `.asset` 차이가 고친 값뿐이다(`haboob` 등 다른 칸이 바뀌지 않는다).
- [ ] 업데이트 전 남은 초안이 있어도 낙석 값이 기본값으로 덮이지 않는다.
- [ ] 복제하면 낙석 값이 따라온다.

구현 기록(2026-11-02, 사용자 "그대로, RF-a 진행", 컴파일 미확인, 새 시험 없음): 바꾼 파일은 `CombatStageSoDocument.cs`·`CombatStageSoAssets.cs`·`CombatLayoutWindow.SoMode.cs` 셋이고 계획과 다른 점은 없다. 값 묶음 `CombatStageSoRockfall`, 형식 4, `DrawSoRockfall`·`DrawSoRockfallFootprint`. 잘못된 값의 "낙석 값:" 머리말은 아직 없고 RF-b에서 넣는다(그때까지는 Core 문구 그대로 저장이 막힌다).

### 13.3 RF-b 검사

1. `CombatStageSoValidation.cs`: `CheckDocumentIssues`에 P-RF1 낙석 값 검사와 복제본 끄기.
2. 같은 파일: `RockfallTunnelObjectName` 사본 한 줄(원본 `StageViewPropPresenter` 주석), 프랍 프리팹의 터널 수를 세는 함수(`GetComponentsInChildren<Transform>(true)`), `Check`에서 둘 이상이면 문제(P-RF3).
3. `CombatLayoutWindow.SoUx.cs`·`SoMode.cs`: 검사·저장 흐름에서 `SetSoCheck` 뒤 터널 0개 + 낙석 켜짐 + 프리팹 방식이면 `soCheck.Warnings`에 한 줄.

Unity 확인 Ub:

- [ ] 돌 수 최소 > 최대, 손실 101, 오프셋 음수·중복·(1,1)만, 오프셋 없음이 각각 "낙석 값:" 오류로 저장을 막고 오류가 한 건씩 나온다.
- [ ] 낙석을 끄면 같은 값이어도 낙석 오류가 없다.
- [ ] 낙석 오류가 있어도 다른 검사 오류가 함께 보인다.
- [ ] 프리팹에서 터널을 잠시 빼면 켜진 스테이지는 경고만 나오고 저장되며, 꺼진 스테이지는 아무것도 안 나온다.
- [ ] 터널을 둘로 만들면 낙석을 꺼도 저장이 막힌다.
- [ ] 확인 뒤 프리팹을 되돌린다.

구현 기록(2026-11-02, 사용자 "RF-b 해주세요", 컴파일 미확인, 새 시험 없음): 바꾼 파일은 `CombatStageSoValidation.cs`·`CombatStageSoAssets.cs`·`CombatLayoutWindow.SoUx.cs`·`CombatLayoutWindow.SoMode.cs`. 계획과 다른 점 둘: 터널 수 세기와 이름 사본은 필드 이름 상수가 있는 `CombatStageSoAssets`에 두었고(`RockfallTunnelCount`), 둘 이상 검사는 `Check`가 아니라 작업본을 받는 `CheckDocumentIssues`에 넣었다. 저장 전 프리팹을 바꾼 작업본도 그 프리팹 기준으로 세기 위해서다. 경고는 검사·저장 흐름에서 내고, 표 등록 흐름은 넣지 않았다.

### 13.4 RF-c 문서

- `Tool/SO모드 사용법.md`: 낙석 값 뜻, 터널 오브젝트 규칙, 경고와 저장 막힘의 차이.
- [전투 View 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) 표에 오브젝트 이름 사본 한 줄(미전달).
- 설계 §17·이 절에 구현 기록.
- 사용법의 옛 스테이지 예시(`Combat_Normal_A`·보스)는 최신화 목록의 문서 항목이라 이 Phase에 넣지 않는다.

구현 기록(2026-11-02, 사용자 "Ub 확인, RF-c", 코드 변경 없음): 사용법 새 §5.11·§8.2 한 줄, 전투 View 사본 공유 §1 표 한 줄(미전달), 설계 §17.6. Ua 결과는 사용자가 따로 말하지 않았고 Ub 확인으로 함께 본 것으로 적지 않는다. 커밋 목록: `Tool/Editor/`의 `CombatStageSoDocument.cs`·`CombatStageSoAssets.cs`·`CombatStageSoValidation.cs`·`CombatLayoutWindow.SoMode.cs`·`CombatLayoutWindow.SoUx.cs`, `Tool/SO모드 사용법.md`, 이 계획·설계·공유 메모. 새 `.cs` 없음.

## 14 확장 UX 시안 적용 계획 (UXR)

- **목표:** 사용자가 선택한 확장 시안의 작업 영역·속성 분류·사용법을 현재 Unity 편집 창에 적용하면서 기존 기능을 보존한다.
- **설계 원본:** [설계 §21](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#21-확장-ux-시안-적용--기존-기능-보존-uxr). 기능 목록은 **§21.2 UXR-F01~F27 한 곳**이 원본이다. 시안은 화면 참고이며 실제 값·저장·검사·3D 동작의 원본이 아니다.
- **구조·기술:** Unity 6000.0.73f1의 기존 `CombatLayoutWindow`·IMGUI를 유지한다. 화면 조립·도움말 partial을 분리하고 현행 Document/Assets/Creation/Validation/Chapter/Preview/SoTiles/SoUx 경로를 호출한다. 새 웹 런타임·UI Toolkit 전환·서비스는 도입하지 않는다.
- **상태(2026-11-02): UXR-a·b 및 별도 요청된 선행 UX-c 코드 반영.** UXR Ua.1~Ua.3·Ub.1~Ub.4와 UX Uc.1~Uc.4 확인 대기. UXR-c·UXR-d는 미착수다. UX-c 실행 기록은 §12.9이며 고지대 B1 보류, 컴파일·Unity 미확인.

### 14.1 공통 제약과 선행 관계

1. 최초 문서 요청 이후 승인된 UXR-a·b와 별도 UX-c(§12.9)를 반영했다. Core·FieldCamera·Architecture·DT 동작·DB SO 스키마·작업본 형식·원본 운영 스테이지는 바꾸지 않는다. 사용법·공유 문서의 전체 갱신은 UXR-d다. 시험 코드 작성·MSBuild·커밋·push 없음. 사용자 Unity 확인과 Phase 보고·정지 규칙을 유지한다.
2. 구현 순서: **UXR-a → UXR-b → 기존 UX-c(§12.3) → UXR-c → UXR-d**. UX-c를 아직 구현하지 않았으면 UXR-c의 숨김 안내 연결을 완료 처리하지 않는다. UX-c의 코드/확인 원본은 §12.3이며 같은 동작을 UXR에 중복 구현하지 않는다. UXR-a·b는 UX-c 없이도 단계 검토할 수 있다.
3. 최신 기능을 작업 시작 때 다시 대조한다. 본부 삭제(설계 §18), TC(§19), 정의 끌어 놓기(§20), 토네이도 가로 방향·회전 금지, 타일 랜덤 미리보기, 새 공개 대상 기본값은 이미 있는 기능이다. 다른 작업자의 진행 중 수정은 덮어쓰지 않는다. 원본 동작이 바뀌었으면 대응표·계획부터 맞춘다.
4. 시안 차이의 처리 원본은 설계 §21.4다. 특히 경고→저장 오류 승격, sourceId 직접 입력, 필드 24칸 상한, 본부 삭제 금지, 선택 묶음 중심 반전, 여러 초안 보관, 시안 전용 단축키를 도입하지 않는다.
5. 확인은 사용자 소유의 비운영 복제 스테이지로 한다. `Test` 원본은 다른 작업자의 시험 에셋이므로 직접 덮어쓰지 않는다. 각 Phase는 해당 기능과 공통 입력/수명에 실제 영향이 있는 범위를 확인한다. 모든 기능의 최종 누락 확인은 UXR-d에서 한 번 모은다.

**검토 초점(수동 확인 배정):**

| 조건 | 기대 결과 | 확인 담당 |
|---|---|---|
| 숫자 입력 뒤 Ctrl+C/V, 타일/설정/도움말로 전환, 끌기 중 전환 | 입력 포커스를 존중하며 명령 한 번, 숨은 배치 변경 없음, 고스트/핫 컨트롤 해제 | UXR-a Ua.2, UXR-b Ub.1~Ub.2 |
| 오류 위치 보기 후 수정/Undo/다른 스테이지, 판 밖·무작위 대상 | 현재 Root·개정의 대상만 이동, 판 밖 직접 편집 유지, 임의 좌표 없음 | UXR-b Ub.4 |
| 회전/음수 배율 배경, 직사각 타일, 판 밖 타일, 다른 세트 붙여넣기 | 기존 TileFit·선택·각 변환·건너뜀 유지 | UXR-b Ub.2 |
| 검사 통과 뒤 쓰기 실패, 보스 교체 취소/표 변경 실패, 등록 해제 | 기존 미저장·조건부 경고·부분 실패 구분, 빼기에 공통 저장 검사 강제 안 함 | UXR-c Uc.2 |
| 공유 참조 편집·목록 새로고침·초안 복원·Preview 오류 | 공유 원본 보호, 기존 편집/시점 수명 유지, 복구 패널 접근 가능 | UXR-b Ub.3, UXR-c Uc.3 |

### 14.2 UXR-a — 작업 영역과 공통 조작 (코드 반영·컴파일 미확인)

**파일:** 새 `Assets/ProjectL/Script/Tool/Editor/CombatLayoutWindow.SoWorkspace.cs`(+ `.meta`), 수정 `CombatLayoutWindow.SoMode.cs`·`CombatLayoutWindow.SoTiles.cs` 표시/포커스 연결·`CombatLayoutWindow.SoUx.cs` 현재 영역 표시/기존 위치 이동 연결. SoUx 연결은 설정에서 기존 위치 버튼이 숨은 지도만 바꾸는 회귀를 막기 위해 이번 단계에 포함한다. 전체 속성 재분류는 UXR-b에 남긴다. 아래 파일명의 공통 접두 경로는 `Assets/ProjectL/Script/Tool/Editor/`다.

**소비:** 기존 `DrawSoMode`, `DrawSoMap`, `DrawSoDetails`, `DrawSoTileView`, `HandleSoKeys`, `CancelSoDrag`·`EndStroke`, `soMapView`·타일 선택·두 클립보드. **제공:** 설계 §21.6의 `SoWorkspace`, `GetSoWorkspace()`, `SetSoWorkspace(SoWorkspace)`, `DrawSoWorkspaceTabs()`, `DrawSoWorkspaceBody()`, `SoInspectorSection`, `OpenSoInspector(SoInspectorSection)`.

- [x] Ra.1 기존 창 안에 공통 머리줄과 배치/바닥 타일/설정 조립을 만든다. UXR-F01~F03의 목록 정보·틀/키 제안·짝 View 복구 접근을 보존한다. 새로 만들기·저장·등록 명령은 기존 진입점을 사용한다.
- [x] Ra.2 설계 §21.3·§21.6의 단일 상태 계산과 전환 규칙을 적용한다. 표시 상태만 추가하고 SO/초안 형식은 바꾸지 않는다. 기존 도구/보기/타일 모드 순번을 유지한다. 설정/목록/생성에서 지도 키를 차단하고 배치·타일 키 분배를 한 번만 수행한다. 사용법은 기존 파일을 열며 창 내부 도움말 입력 차단은 해당 UI를 만드는 Rc.1에서 연결한다.
- [x] Ra.3 기존 1100×650 최소 크기·패널 경계/스크롤에서 버튼과 상태를 읽을 수 있게 줄을 나눈다. 3D 시점/선택/클립보드를 보존하고, 오류로 지도 대신 안내가 보여도 속성·새로고침을 열 수 있게 한다. 코드 반영과 실제 크기/테마 확인(Ua.3)은 구분한다.

**Ua — 사용자 Unity 확인:**

- [ ] Ua.1 기존 스테이지 열기 → 세 영역 이동 → 2D/3D/타일 회전 복귀에서 이름·키·역할·등록·선택 정보가 남는다. 보기 변경만으로 미저장/검사 개정/Undo/초안이 바뀌지 않는다. DT로 돌아가도 기존 화면과 동작이 유지된다.
- [ ] Ua.2 이동/칠하기/상자/팔레트 끌기/타일 사각형 도중 영역을 바꿔도 고스트나 입력 소유권이 남지 않는다. 적용된 칠하기의 기존 Undo 단위는 유지한다. 설정/도움말에서 Delete/R/Ctrl+C/V로 숨은 지도가 바뀌지 않고, 입력 칸에서는 텍스트 복사가 동작한다.
- [ ] Ua.3 최소 크기·큰 창·밝은/어두운 테마에서 검사·저장·사용법과 전환 버튼에 접근할 수 있다. 3D를 여러 번 오가거나 창을 닫아도 기존 Preview 정리와 시점 정책이 유지된다.

Phase 끝에 코드 반영/컴파일 미확인/사용자 확인/남은 항목을 보고하고 멈춘다.

### 14.3 UXR-b — 전체 편집 기능 재배치 (코드 반영·컴파일 미확인)

**파일:** `CombatLayoutWindow.SoWorkspace.cs`, `CombatLayoutWindow.SoMode.cs`, `CombatLayoutWindow.SoTiles.cs`, `CombatLayoutWindow.SoUx.cs`. 데이터·연산 파일은 읽어 대조하며 UI 재배치 때문에 변경하지 않는다.

**소비:** UXR-a 영역/속성 열기 계약, 기존 `DrawSoCell`·`DrawSoReferences`·`DrawSoView`·`DrawSoPlayerZone`·`DrawSoRockfall`, 선택/상자/팔레트/타일 입력, UX-a 대상 이동. **제공:** 설계 §21.2의 화면별 전체 기능 접근과 기존 명령 호출.

- [x] Rb.1 UXR-F04~F16을 배치 팔레트·선택 속성·설정 분류로 옮긴다. 묶인 `DrawSoStageValues`는 기본 값/아군 구역/기믹/등록 표시로만 나누고, 기존 편집·확인 함수를 유지한다. 본부 삭제, 아군 읽기 목록, 토네이도 가로 방향/무작위 목록, 회전 제한, 자동 sourceId, 신규 공개 대상 기본값을 누락하지 않는다.
- [x] Rb.2 UXR-F17~F23을 바닥 타일·배경/기믹 속성에 연결한다. 공유 SO 전체 PropertyField·키 잠금과 전용 복제/새 필드를 예시 필드로 축소하지 않는다. 배경 비활성 값·타일 세트/후보/높이·타일 회전/복사·판 밖 선택·랜덤 다시 뽑기를 기존 함수로 연결한다.
- [x] Rb.3 UXR-F24의 위치 이동에 `SetSoWorkspace(Layout)`·2D·`OpenSoInspector(Selection)`을 연결한다. 검사 Root/개정·판 밖 좌표 일괄 적용·스크롤·무작위/위치 없는 오류 처리는 UX-a 계약을 유지한다. 지도 렌더링 실패 시에도 선택/참조 복구 패널을 남긴다.
- [x] Rb.4 기능 대응표 UXR-F01~F26을 실제 진입점과 대조한다. 각 행의 기존 하위 조작이 새 위치에서 도달 가능한지 §14.8에 기록했다. 정적 접근 경로 대조이며 Ub의 Unity 확인 완료를 뜻하지 않는다.

**Ub — 사용자 Unity 확인:**

- [ ] Ub.1 팔레트 클릭/끌어 놓기·맵 밖 취소·연속/Shift 칠하기·단일/상자 이동·복사/다른 스테이지 붙여넣기·맵 중심 반전/대칭·Undo를 확인한다. 본부 삭제 후 정의 재지정은 자리/회전을 복원하며, 보스 격파와 본부 파괴의 저장 조건이 기존과 같다. 공개 대상 None/Neutral/Player/Enemy·숨김·복사 보존도 확인한다.
- [ ] Ub.2 타일 세 보기와 판 밖 선택, Ctrl/Shift 선택 조합, R/Shift+R/Random, 직사각 제한, 다른 배경 회전·음수 배율·다른 세트 붙여넣기를 확인한다. 붙일 선택 기준·누락 종류/범위 밖/각 보정 수가 기존과 같고, 배치와 타일 클립보드가 섞이지 않는다. 다시 뽑기만 난수를 바꾸며 저장/Undo 상태는 바꾸지 않는다.
- [ ] Ub.3 공유/전용 SO 전체 필드·새 필드의 대기 칸/아군 줄 수, 아군 구역 마지막 칸 보호·줄 수 복귀·경고, 배경 세 방식/짝 View 생성, 레일/낙석 전체 입력·오프셋·터널 검사가 유지된다. 고지대·토네이도 외형 회전 제한도 유지한다.
- [ ] Ub.4 설정/타일/Player 기준 3D에서 오류 위치를 누르면 정확한 2D 대상·속성이 열린다. 판 밖 대상도 수정할 수 있고, 수정/삭제/Undo/다른 스테이지 뒤 옛 결과는 실행되지 않는다. 참조/표시 오류에서도 편집 패널이 남는다.

### 14.4 UXR-c — 사용법·상태·복구 동선 (미착수)

**선행:** UXR-b와 기존 UX-c(§12.3)의 숨김 안내·복구 계약. 두 Phase 모두 코드 반영 상태이며 Unity 확인은 대기다(§14.8·§12.9). 후속 요청 시 기존 진단/복구 함수를 사용하고 재구현하지 않는다.

**파일:** 새 `CombatLayoutWindow.SoHelp.cs`(+ `.meta`), `CombatLayoutWindow.SoWorkspace.cs`, `CombatLayoutWindow.SoUx.cs`, `CombatLayoutWindow.SoMode.cs`·`CombatLayoutWindow.SoTiles.cs`의 기존 도구 안내 연결.

**소비:** 현재 작업 영역/도구·명령 상태·거절 이유, `soModified`·`soCheck`, `OpenSoInspector`, UX-c 진단·복구. **제공:** 설계 §21.6의 `SoHelpTopic`, `ShowSoHelp(SoHelpTopic)`, `DrawSoHelp()`와 UXR-F27.

- [ ] Rc.1 설계 §21.5의 여섯 도움말 주제와 현재 도구 한 줄 사용법을 연결한다. 주제에서 기능 위치와 기존 사용법 파일을 열 수 있게 한다. 단축키를 예시 HTML에서 복사하지 않고 기존 처리 분기와 대조한다. Ctrl+C/V는 검사 상태와 무관함, 배치/타일 목적지 기준과 포커스 조건을 명시한다.
- [ ] Rc.2 검사·저장·등록·빼기의 기존 정책은 그대로 두고 공통 상태·비활성 이유·다음 행동을 연결한다. 시안의 예시 경고 등급/보스 규칙/가짜 공유 수/터널 수 입력을 가져오지 않는다. 초안과 SO 저장·에셋 생성 시점·Git 공유를 구분해 안내한다.
- [ ] Rc.3 정상 숨김/판정 오류/무작위 위치 미정 안내를 UX-c의 원본 진단에서 표시한다. `전체 배치로 보기`/`2D에서 수정`은 보기만 바꾸고 대기 중 붙여넣기를 자동 재실행하지 않는다. 표시 진단 오류를 저장 검사에 임의 추가하지 않는다.

**Uc — 사용자 Unity 확인:**

- [ ] Uc.1 처음 쓰는 작업자가 도움말에서 새 스테이지→배치→필요시 바닥→검사→저장→등록 위치를 찾을 수 있다. 입력 칸 편집 후 지도 클릭·상자 선택·Ctrl+C/V가 버튼과 같으며, 타일 안내에 현재 가능한 복사가 불가능하다고 나오지 않는다. 안내 이동만으로 데이터/Undo가 바뀌지 않는다.
- [ ] Uc.2 미검사/재검사/오류/조건부 경고/확인 취소/저장 실패/표 실패를 구분한다. 아군 구역 참고 경고·타일 맞춤 경고를 새 저장 오류로 바꾸지 않는다. 보스 교체·기존 맵 문제·맵 SO 없음·해제의 기존 판정 흐름이 유지된다. 쓰기 실패의 전체 롤백을 안내하지 않는다.
- [ ] Uc.3 저장 전 새로고침/다시 읽기/다른 스테이지 열기와 취소, 초안 복원·구형 초안 거절에서 기존 정책이 유지된다. 정상 숨김과 잘못된 정의/공개 값이 구분되고, 보기 전환만으로 오류가 해결됐다고 표시하지 않는다. 새 임시본 이력이나 자동 에셋 저장 기능이 생기지 않는다.

### 14.5 UXR-d — 사용법 정합성과 누락 최종 확인 (미착수)

**파일:** `Assets/ProjectL/Script/Tool/SO모드 사용법.md`, 이 설계·계획, `docs/superpowers/handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md`, `docs/superpowers/handoffs/2026-10-27-combat-stage-editor-so-mode-minhyeong-share.md`. 새 Core 요청이나 Core 전달 문서 수정은 예정하지 않는다.

- [ ] Rd.1 실제 구현 화면 기준으로 사용법의 시작·화면 구성·따라 하기·단축키·저장/등록·자주 막히는 경우를 갱신한다. 빠른 사용법과 상세 설명이 같은 조작을 가리키게 한다. 현재 문서의 옛 “타일 보기 Ctrl+C/V 불가”는 배치 단축키 차단과 TC 타일 복사 허용으로 나누고, 옛 본부 삭제 금지·도구 개수 등도 현재 기능과 대조한다.
- [ ] Rd.2 사용법에 마지막 PC 초안/실제 에셋 저장/등록/사용자 Git 공유, 생성 시 에셋 쓰기, 경고와 차단 오류·부분 실패·미리보기 한계를 설명한다. §12.3 Xc.3의 숨김 안내 문서 내용을 인수해 새 화면 이름에 맞추되 진단 규칙을 중복 정의하지 않는다.
- [ ] Rd.3 UXR-F01~F27 각 행을 `새 화면 진입점 확인 / 기존 동작과 대조 / 사용자 Unity 확인 또는 미확인 사유`로 기록한다. 시안 클릭·코드 정적 대조와 Unity 확인을 혼동하지 않는다. 변경 없는 기능은 억지로 재작성하지 않는다.
- [ ] Rd.4 공유 메모에는 화면 위치·입력/미리보기 소유권 유지·새 partial 책임·고지대 보류를 적고 **미전달**로 남긴다. Phase 코드 완료/컴파일 미확인/사용자 확인/커밋 상태를 따로 갱신한다. 실제 게시·메시지 발송·커밋·push는 하지 않는다.

**완료 기준:** 대응표의 모든 기능에 실제 접근 경로가 있고, 기존 동작 차이와 미확인 항목이 숨겨져 있지 않으며, 필요한 사용자 Unity 확인을 기록한 뒤 Phase 인수를 받는다. 설계에만 있는 항목을 코드 완료로 표시하지 않는다.

### 14.6 최초 문서 반영·자체 검토 이력 (2026-11-02, 구현 전)

- 반영: 설계 §21의 UXR-F01~F27 대응표, 시안과 기존 계약 차이, 화면/입력 수명, 도움말·저장 안내, SSOT·SOLID 5원칙·JMH/FW·변경 독립성; 이 계획의 파일 책임·순서·Phase별 수동 확인 항목.
- 검토: 현재 SoMode/SoTiles/SoUx·Document의 연결점, 사용법, 최근 설계 §17~§20과 대조했다. 특히 본부 삭제·맵 중심 대칭·sourceId 자동 관리·토네이도 방향·타일 선택 붙여넣기·경고 등급·단일 초안·등록 해제 정책의 시안 차이를 보존 기준으로 확정했다.
- 당시 기존 규칙에 따라 별도 검토 에이전트/새 시험/MSBuild/Unity 실행 없이 문서 링크·대응표/Task 연결·차이만 자체 검토했다. **문서 반영 시점에는 설계·계획 두 문서만 변경했으며 UXR-a~UXR-d·UX-c는 미착수**였다. 이후 UXR-a 구현 상태는 §14.7을 따른다. RF-b의 기존 사용자 확인 등 이전 기록을 이번 확인으로 확대하지 않는다.

### 14.7 UXR-a 구현·자체 대조 이력 (2026-11-02)

사용자 요청: “UXR-a부터 진행해줘. 기존 기능은 빠지지 않게 유지하고, 고지대 개선은 계속 보류해줘.” **Ra.1~Ra.3 코드 반영, 컴파일 미확인, Ua.1~Ua.3 사용자 Unity 확인 대기. UXR-a에서 보고 후 정지하며 UXR-b·UX-c·UXR-c·UXR-d는 진행하지 않는다.**

| 파일 | 반영 내용 |
|---|---|
| `CombatLayoutWindow.SoWorkspace.cs` + `.meta` | 세 작업 영역, 메타정보를 보존한 스테이지 선택/전체 목록/생성, 기존 패널 조립, 설정 바로가기, 보기·입력 전환, 기존 사용법 파일 열기 |
| `CombatLayoutWindow.SoMode.cs` | 공통 머리줄·작업 영역 호출, 지도 키 단일 분배/숨은 지도 차단, 성공한 열기의 목록 닫기, 기존 속성 패널의 설정 표시, 좁은 맵의 도구 열 수·안내 줄바꿈 |
| `CombatLayoutWindow.SoTiles.cs` | 타일 지도 유효 클릭 시 입력 칸 포커스 해제. 선택/클립보드/각 변환/난수 코드는 유지 |
| `CombatLayoutWindow.SoUx.cs` | 설정/목록/생성의 실제 화면 상태 표시, 기존 오류 위치 이동에서 배치/2D/선택 속성 열기 |
| 이 설계·계획 | 현재 단계·코드 반영/Unity 미확인·다음 단계의 경계를 맞춤 |

**보존 대조:**

- UXR-F01~F03: 상단 선택과 `전체 목록`에 이름·키·맵·역할·등록 정보를 유지한다. `새로 만들기 / 복제`는 기존 `DrawSoCreate`를 사용해 틀·키 제안·배치 비우기/복제를 모두 제공한다. 짝 View 복구는 기존 오른쪽 View와 설정의 배경·타일·카메라에서 접근한다. 열기 취소/실패 때 목록을 닫거나 작업본을 버리지 않는다.
- UXR-F04~F26: 기존 배치 팔레트·맵 도구·오른쪽 속성과 타일 본문 함수를 재사용한다. 저장·검사·등록/빼기·복사/붙여넣기·본부 삭제·공개 대상·토네이도·낙석·공유 SO의 연산은 바꾸지 않는다. 세부 분류/타일 종류 위치는 UXR-b까지 기존 패널에서 접근한다. 전체 대응표의 최종 누락 확인은 UXR-d에 남긴다.
- 전환: 현재 영역은 설정 표시 여부+`soMapView`에서 계산한다. 마지막 2D/3D 보기와 기존 선택/두 클립보드/미리보기 시점을 유지하고 새 저장 상태를 만들지 않는다. `EndStroke`로 이미 적용된 칠하기만 마무리하며 미확정 끌기·사각형·이 도구의 DragAndDrop 데이터와 입력 소유권은 해제한다. 목록/생성도 지도 대신 본문을 쓰므로 지도 단축키를 차단한다.
- 복구: 설정에서도 현재 필드를 읽고 기존 검사 결과를 표시한다. 오류 위치를 누르면 배치·2D·선택 속성을 열며 기존 대상/개정/판 밖 수정 로직을 유지한다. 이 최소 연결을 UXR-a에 포함했으며 Rb.3의 전체 속성 재배치·확인 완료를 뜻하지 않는다.
- 도움말: 상단 `사용법`은 `SO모드 사용법.md`를 연다. 창 내부 여섯 주제·빠른 사용법 재배치·문서 전체 최신화는 각각 UXR-c/d에 남긴다. 고지대 B1·Core·DT·DB SO/초안 형식·미리보기 해제 소유권은 유지한다.

**정적 확인 결과:** 변경 diff·호출 경로·보기/도구 숫자·문서 상태를 대조했다. 지도 키 분배 진입점은 `DrawSoMode`의 한 번 호출이며, 저장/검사/복사·타일 연산 변경은 없다. 새 `.meta` GUID는 Assets 안에서 하나이고 기존 사용법 파일 경로가 존재한다. 대상 파일의 `git diff --check`는 통과했다. 새 시험 코드/시험 실행/MSBuild/Unity 실행/별도 검토 에이전트/커밋·push는 하지 않았다. 정적 대조는 실행 성공의 증거가 아니다.

**사용자 확인:** 비운영 복제 스테이지에서 §14.2 Ua.1~Ua.3을 수행한다. 특히 세 작업 영역을 오간 뒤 선택/복사본/3D 시점 유지, 설정·목록·생성에서 Delete/R/Ctrl+C/V 무편집, 숫자 입력 후 지도 클릭→복사/붙여넣기, 설정 검사 결과→2D 위치 이동, 최소 크기에서 검사/저장/사용법과 기존 속성 접근을 확인한다. **컴파일 미확인·Unity 미확인, 고지대 계속 보류, 미커밋·미전달.**

### 14.8 UXR-b 구현·기능 경로 대조 (2026-11-02)

사용자 요청: “다음 단계인 UXR-b도 진행해줘. 기존 기능은 빠지지 않게 유지하고, 고지대 개선은 계속 보류해줘.” **Rb.1~Rb.4 코드 반영·정적 대조, 컴파일 미확인, Ub.1~Ub.4 사용자 Unity 확인 대기.** UXR-a의 Ua도 별도 결과를 받지 않았으므로 대기 상태를 유지한다.

**변경 파일:** `Assets/ProjectL/Script/Tool/Editor/CombatLayoutWindow.SoWorkspace.cs`·`CombatLayoutWindow.SoMode.cs`·`CombatLayoutWindow.SoTiles.cs`·`CombatLayoutWindow.SoUx.cs`, 설계 §21.10과 이 계획. UXR-b에서 새 `.cs`/`.meta`·시험 코드는 만들지 않았다. 기존 UXR-a 미커밋 변경을 이어서 편집했으며 다른 작업자의 파일은 변경하지 않았다.

**화면 조립:** Workspace는 여섯 설정 분류·세 배치 속성 분류·공통 검사 결과·표시 오류 복구를 조립한다. SoMode는 기존 Stage/Reference 패널을 표시 단위로 분리하고, 겹친 칸의 속성 대상과 무작위 지형 목록을 제공한다. SoTiles는 종류 팔레트와 기존 타일 구성·편집 명령을 연결하고 긴 조작 줄을 나눈다. SoUx는 기존 결과를 쓰며 검사 실행 시 펼침·위치 이동 시 속성 대상 표시만 연결한다.

아래는 설계 §21.2 대응표의 **실제 호출 경로 확인**이다. 세부 규칙을 다시 정의하지 않는다. 전 행의 실행 확인은 **Unity 미확인**이며 공통 수동 확인은 §14.3 Ub.1~Ub.4, 최종 누락 확인은 UXR-d가 원본이다.

| 기능 | 실제 화면 진입점 / 기존 동작 대조 |
|---|---|
| UXR-F01 | 공통 머리줄·전체 목록 → `DrawSoStagePicker`/`DrawSoStageList`/열기·새로고침·다시 읽기 유지 |
| UXR-F02 | 공통 새로 만들기/복제 → 기존 `DrawSoCreate`·생성 명령 유지 |
| UXR-F03 | 설정 → 배경·타일·카메라 → `DrawSoView`의 짝 View 생성, 열기 중복 검사 유지 |
| UXR-F04 | 배치 왼쪽 → 기존 `DrawSoPalette`/`DrawSoPlayerUnits`; 클릭/끌어 놓기의 속성 표시 연결만 추가 |
| UXR-F05 | 배치 도구 줄·맵 → 도구 숫자/배치 연산 유지, 도구/맵 클릭 때 해당 속성 표시 |
| UXR-F06 | 배치 → 선택 → 속성 대상 → 기존 `DrawSoPlaced`/Unit/Piece/Marker/Placement 편집기. X·Y 동시 적용·정의·회전·아이템 유지 |
| UXR-F07 | 배치 편집 명령 두 줄 → Copy/Paste/Flip/MirrorCopy 유지. 속성 대상 선택이 맵 끌기·Delete/R의 대상을 바꾸지는 않음 |
| UXR-F08 | 배치 선택의 본부 편집/삭제, 설정 → 공유 SO·본부·구조물의 정의 재지정; 기존 Remove/검사 유지 |
| UXR-F09 | 선택 또는 무작위 지형 → 기존 `DrawSoOwners`/외형 회전. sourceId·신규 기본값·복사 값은 Document 원본 유지 |
| UXR-F10 | 선택 및 무작위 지형 → 기존 `DrawSoMarker`; 가로 방향·무작위 시작·회전 제한 유지. 크기/높이 등의 정의·미리보기 계약은 기존 경로 |
| UXR-F11 | 배치 → 아군 구역, 설정 → 필드·아군 구역 → 기존 `DrawSoPlayerZone`/필드 전용 복제/구역 편집 명령 유지 |
| UXR-F12 | 배치 2D/3D → 기존 Preview/Visuals/높이/시점·자원 해제 유지. 스크롤 종료를 scope로 바꾸고 표시 오류에 복구 경로 추가 |
| UXR-F13 | 3D 기준과 선택 속성 → 기존 SoPlayerFilter/SoShown/TryIsShown 유지. UXR-b 당시 남겼던 숨김 안내는 후속 UX-c에서 반영(§12.9), Unity 확인 대기 |
| UXR-F14 | 배치 → 공격 범위 펼침 → 기존 모드/방향/범위 계산 유지. 접어도 이미 고른 범위 표시는 유지 |
| UXR-F15 | 설정 → 기본 정보 → 기존 키/역할/시드/승리 조건/레벨/벽 모서리/통계·호환 값 안내 유지 |
| UXR-F16 | 설정 → 필드·아군 구역 및 공유 SO·본부·구조물 → 전체 SharedEditor/키 잠금/전용 복제/새 필드/목록 유지 |
| UXR-F17 | 설정 → 배경·타일·카메라, 타일 왼쪽 바로가기 → 기존 `DrawSoView`/Background/보정/카메라 유지 |
| UXR-F18 | 타일 오른쪽 또는 설정의 배경 분류 → 같은 `DrawSoTileLayout`/Kinds/FitMessages, 후보·높이·사용 수·Inspector 열기 유지 |
| UXR-F19 | 타일 왼쪽 종류 → 가운데 도구·지도 → 기존 칠하기/전체 칠하기/사각형/기준 타일 입력·좌표 변환 유지 |
| UXR-F20 | 타일 선택 또는 배치 타일 회전(2D/3D) → 같은 선택·회전 함수/직사각 제한·판 밖 선택 유지 |
| UXR-F21 | 타일 선택의 복사/붙여넣기 → 기존 CopySoTiles/PasteSoTiles·선택 원점·별도 클립보드 유지 |
| UXR-F22 | 타일/3D 랜덤 다시 뽑기 → 기존 버튼·시드·Preview 무효화 유지 |
| UXR-F23 | 배치 레일 도구 / 설정 → 기믹 · 레일·낙석 → 기존 레일 값·Rockfall/Footprint 전체 패널과 검사 유지 |
| UXR-F24 | 공통 검사/저장·각 영역 속성/목록 상단의 검사 결과 → 기존 결과/개정·FocusSoIssue/판 밖 복구, 저장 실패/조건부 경고 정책 유지 |
| UXR-F25 | 설정 → 등록·맵 생성 규칙 → 기존 `SetSoRegistered`/ChapterLine·두 표/보스 교체·빼기 정책 유지 |
| UXR-F26 | 공통 작업 관리·각 영역 → 기존 Undo/초안/포커스/선택 수명 유지. 지도/타일 scroll scope·작업별 속성 분류는 표시 상태 |

**범위 판단:** 겹친 칸의 새 선택은 속성 표시만 정한다. 기존 맵 끌기·삭제/회전 키의 대상 규칙을 바꾸지 않고 화면에도 구분해서 안내한다. 검사 대상은 우선 표시하며 기존 대상/Root/개정·판 밖 좌표 적용 경로를 유지한다. 표시 오류는 해당 영역에 표시하고 복구 설정을 열게 하며 저장 검사에 추가하지 않는다. `ExitGUIException`을 삼키지 않는다.

**구조 점검:** SSOT는 Document·기존 명령·SoUx 결과·SoTiles 선택/클립보드/시드 그대로다. S/O는 Workspace 조립과 편집 함수의 분리, L은 기존 명령과 거절·Undo 정책 재사용, I는 영역/분류 단위 연결, D는 기존 편집 진입점 호출로 점검했다. JMH/FW의 서비스·조립·게임 입력·자원 소유권을 변경하지 않는다. 이후 설정 분류 변경은 Workspace, 타일 표현 변경은 SoTiles에 머무르며 저장·Core를 함께 고칠 필요가 없다. 공통 포커스/스크롤/검사 위치 연결은 배치와 타일에 영향을 주므로 Ub에서 함께 확인한다.

**정적 대조:** HEAD와 기존 핵심 함수 40개의 본문을 대조해 동일함을 확인했다(배치 복사/이동 보조, 각 배치 속성, 공개 대상·토네이도·낙석·아군 구역, 공유 SO 전체 편집, 타일 복사/선택/각 변환/난수, 검사 개정·대상 복구). `CheckSoStage`/`SaveSoStage`/`SetSoRegistered` 세 함수도 결과 위치 안내 문구를 제외한 본문이 동일함을 확인했다. 지도 키는 `DrawSoMode`에서 한 번 분배하고, 공통 검사 내용도 기존 `DrawSoCheckResult` 한 곳에서 표시한다. 대상 파일 `git diff --check`와 UTF-8/LF·잘못된 문자·공백 확인을 통과했다. 데이터·Assets·Creation·Validation·Chapter·Preview 등 연산 파일은 이번에 변경하지 않았다. 이 결과는 컴파일·Unity 조작 확인을 대신하지 않는다.

**남은 확인·단계:** Ub.1~Ub.4와 최소 크기/테마에서 긴 속성·회전 줄·공통 검사 스크롤을 사용자 Unity에서 확인한다. UXR-F01~F26의 기존 패널을 임시 고급 설정에 남기거나 제거한 항목은 없다. UXR-F27의 창 내부 도움말·빠른 사용법 재배치는 UXR-c, 기존 사용법/공유 문서 전체 최신화는 UXR-d, 숨김 안내는 선행 UX-c로 남긴다. **UXR-b에서 보고 후 정지. 컴파일 미확인·Unity 미확인, 고지대 B1 보류, 시험/MSBuild/별도 검토 에이전트/커밋·push 없음.**

## 15 하붑 기믹 편집 추가 계획 (HBE)

- **상태:** 2026-11-02 사용자 설계 승인 뒤 HBE-a 코드·문서 반영, 컴파일·Unity 미확인, 커밋 전(§15.4). 기존 RF/UXR 범위와 별도로 추가했으며 고지대 B1 보류와 UXR-c/d 미착수 상태를 유지한다.
- **설계 원본:** [설계 §22](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#22-하붑-기믹-편집-추가안-hbe). 목표는 기믹 패널의 네 입력과 읽기/편집/검사/저장을 함께 제공하는 것이다.
- **실행 방식:** 설계 확인 뒤 `superpowers:executing-plans`로 HBE-a 한 Phase를 수행하고 보고·정지한다. §3.1에 따라 새 시험·MSBuild·검토 에이전트·커밋·push 없음. 컴파일/재생 확인은 사용자 Unity이며 보고에 **컴파일 미확인**을 쓴다.
- **제약:** Unity 6000.0.73f1·기존 IMGUI 유지. Core/DB SO/런타임/FieldCamera/Architecture/DT/기존 에셋 변경 없음. 새 C#은 .meta 포함. LF·UTF-8·Allman 및 기존 명명 규칙 유지.

### 15.1 HBE-a 하붑 설정과 저장 연결

파일은 별도 표기 외에 `Assets/ProjectL/Script/Tool/Editor/` 아래다.

- [x] Ha.1 `CombatStageSoDocument.cs`: `CombatStageSoHaboob`(enabled, relocationIntervalTurns, warningLeadTurns, footprintOffsets)을 추가하고 document.haboob으로 소유한다. 기본값은 공개 `HaboobGimmickSpec()`에서 얻고 모양을 복사한다. CurrentFormat은 4→5. 기존 초안 거절/에셋 다시 읽기와 Json 복제 경로를 대조하고, ClearPlacements는 수정하지 않는다.
- [x] Ha.2 `CombatStageSoAssets.cs`: 네 필드 경로를 상수로 모으고 `ReadHaboob(SerializedObject combat, CombatStageSoDocument document)`·`WriteHaboob(SerializedObject combat, CombatStageSoDocument document)`를 각각 ReadCombat/WriteCombat에서 호출한다. enabled와 하위 값·목록 순서를 모두 보존하고 임의 기본값 대체/정규화는 하지 않는다. 검사용 복제본만 받는 `DisableHaboob(Object combatCopy)`를 제공한다.
- [x] Ha.3 `CombatStageSoValidation.cs`: `TryCheckHaboob(CombatStageSoHaboob haboob, List<CombatStageSoIssue> problems)`에서 사용 중인 값만 공개 Spec으로 검증한다. 실패는 위치 없는 “하붑 값: 원문” 한 건, 이후 검사본에서만 비활성화한다. CheckDocumentIssues에서 기존 낙석 검사와 독립 호출한다. CheckDocument 문자열 소비·저장·등록·생성의 차단 계약을 유지한다.
- [x] Ha.4 새 `CombatLayoutWindow.SoHaboob.cs`와 .meta: `DrawSoHaboob(CombatStageSoDocument document)`·`DrawSoHaboobFootprint(List<Vector2Int> offsets)` 구현. 설계 §22.1의 네 항목과 위치 미리보기 한계 안내를 표시한다. 입력은 기존 SoToggle/SoInt/SoChange·DelayedIntField, 목록 추가/삭제는 기존 Undo/ExitGUI 흐름 사용. 꺼짐 상태에서 값은 비활성·보존한다.
- [x] Ha.5 `CombatLayoutWindow.SoMode.cs`의 DrawSoGimmicks에 하붑 호출 한 줄을 더한다. `CombatLayoutWindow.SoWorkspace.cs`의 분류 이름은 “기믹”으로 바꾸고 기존 레일·낙석을 유지한다. Creation의 배치 비우기/복제, Check/Save/Register, Undo·초안·Dirty·검사 무효화와 함께 대조한다. 선택 영역/타일 클립보드·공개 대상·3D Preview는 수정하지 않는다.
- [x] Ha.6 사용법 `Assets/ProjectL/Script/Tool/SO모드 사용법.md`와 기존 View/민형님 공유 메모에 위치·네 입력·단위·검사/진입 한계·초안 형식 갱신 주의를 기록한다. 공유 미전달 유지. 설계/계획의 코드 반영·정적 확인·Unity 대기·커밋 상태를 각각 갱신한다. 소스/호출부·diff·UTF-8/LF를 정적으로 확인하고 Phase 끝에 보고한다.

### 15.2 사용자 Unity 확인 (구현 후, 현재 전부 미확인)

운영 에셋 대신 미등록 Test 복제본으로 확인한다. 실제 파일 생성·저장은 사용자가 실행한다.

- [ ] Ua.1 기존 하붑 켜짐/꺼짐 스테이지를 열면 네 값과 순서가 Inspector 원본과 같다. 끄기→저장→다시 열기→켜기에도 값이 보존되고, 수정/Undo/Redo의 저장·재검사 상태가 기존대로 바뀐다.
- [ ] Ua.2 기본 모양((0,0) 없는 12칸), 이동 주기=경고 시간은 통과한다. 이동 0, 경고 0/주기 초과, 빈 목록·중복·음수·정규화되지 않은 목록은 사용 중일 때 “하붑 값” 오류로 저장/등록/생성이 막힌다. 낙석도 잘못되면 두 오류가 각각 보이고 위치 버튼을 만들지 않는다. 끄면 하위 값은 검사 제외·보존이다.
- [ ] Ua.3 배치 비우고 새로 만들기와 배치까지 복제가 틀의 하붑 설정을 보존한다. 선택 영역/타일 Ctrl+C/V는 기존 동작 그대로이며 하붑 설정을 옮기지 않는다. 레일·낙석·고지대 기존 편집과 공개 대상 기본값/숨김 복구가 유지된다.
- [ ] Ua.4 코드 갱신 전 미저장 내용을 먼저 저장한다. 구형 초안/열린 작업본은 기존 경고와 거절/다시 읽기를 따르고 원본 하붑 값이 기본값으로 덮이지 않는다. 새 형식 초안 복원은 하붑 편집을 복원한다. 창 크기를 줄여도 “기믹” 분류와 네 입력·목록 버튼에 접근할 수 있다.
- [ ] Ua.5 검사 통과한 복제본을 Play From Boot PlacedStage로 열어 기존 하붑이 설정 주기로 동작하는지 확인한다. 필드보다 큰 모양은 도구의 값 검사와 별개로 Core 진입 실패가 가능하다는 안내를 확인한다. 편집 툴이 고정 위치·다음 이동 위치·새 경고 이펙트를 제공한다고 오해하지 않는다.

### 15.3 설계·계획 자체 검토 (2026-11-02)

설계·계획 작성 당시 네 SO 필드 → Ha.1/2/4, 검사/복제본 격리 → Ha.3, 화면 연결·기존 기능 보존 → Ha.5, 사용법/형식 갱신/검증 상태 → Ha.6으로 연결했다. 꺼진 잘못된 값·구형 초안·동시 기믹 오류·배치 비우기·필드보다 큰 모양을 확인 사례로 남겼고 SSOT·SOLID·JMH/FW·변경 독립성은 설계 §22.3에서 점검했다. 당시 문서만 수정했으며 이후 승인·코드 반영은 아래 §15.4와 구분한다.

### 15.4 HBE-a 구현 기록 (2026-11-02)

- 사용자 승인: “이 설계대로 HBE-a 구현 진행해줘. 하붑 설정과 저장·검사·Undo·복제까지 연결”. **Ha.1~Ha.6 코드·문서 반영, 컴파일 미확인, Ua.1~Ua.5 Unity 확인 대기, 커밋 전.** 앞선 단계 확인을 이번 검증으로 확대하지 않는다.
- 값/입출력: `CombatStageSoDocument`에 직렬화 하붑 값과 공개 Spec 기반 기본값 복사, CurrentFormat 5. Assets의 `ReadHaboob`/`WriteHaboob`을 기존 ReadCombat/WriteCombat에 연결했다. 사용 여부와 비활성 값·상대 칸 목록 순서를 그대로 보존한다. 구형 초안/열린 작업본은 기존 거절/에셋 다시 읽기를 유지하며 사용자에게 업데이트 전 미저장 편집 저장을 안내했다.
- 검사: `TryCheckHaboob`은 사용 중일 때만 공개 Spec을 호출한다. 실패 시 “하붑 값:” 위치 없는 오류를 남기고 `DisableHaboob(combatCopy)`로 검사본만 꺼서 나머지 검사를 이어간다. 낙석 검사·표 등록/맵 판정은 변경하지 않았다. 필드에 모양 전체가 들어가는지와 실제 위치 선정은 전투 진입 확인으로 남긴다.
- 편집/복제: 새 `CombatLayoutWindow.SoHaboob.cs`(+ .meta)의 네 입력과 목록 추가/삭제는 SoChange를 사용한다. 기존 Undo·초안·Dirty·검사 무효화가 함께 적용된다. SoMode의 기존 레일/낙석 뒤에 호출 한 줄, Workspace 분류 이름 “기믹”만 연결했다. ClearPlacements/Creation은 그대로라 배치 비우기·스테이지 복제에 하붑 값이 유지되며 영역/타일 클립보드에는 포함하지 않는다.
- 문서: 사용법 §5.12·검사 표와 메뉴 위치, 민형님 §7·View §12 공유 메모, 설계 §22.4·기능 보존 표를 갱신했다. 공유 메모는 미전달이다. 초기 설계 문서 작성 이력은 유지하고 사용자 승인/코드 반영/Unity 대기를 구분했다.
- 구조/자체 검토: UI는 새 partial, 입출력은 Assets의 하붑 함수, 검사는 Validation의 공개 계약 호출, 조립은 SoMode/Workspace다. Spec 기본값·규칙을 복제하지 않았고 기존 서비스/Factory/Preview 수명은 그대로다. SSOT·SOLID·JMH/FW·기능 독립성은 설계 §22.3과 일치하며 설계 변경은 없다. 사용자 제한에 따라 새 시험·MSBuild·검토 에이전트 없이 정적으로 검토했다.
- 정적 근거: 시작 시점 소스와의 전체 텍스트 대조에서 Document/Assets/Validation/SoMode/Workspace는 계획된 하붑 추가·형식 번호·메뉴 이름 변경을 제외한 기존 본문이 동일했다. Creation/SoUx/SoTiles/Visuals/Preview 다섯 파일은 변경 없음. SO 네 필드 이름·Core 공개 생성자·읽기/쓰기/검사/조립의 각 호출 위치·검사본 비활성화·.meta GUID 유일성을 대조했다. git diff --check와 변경 12개 파일의 UTF-8/LF·공백 확인을 통과했다. 실제 Unity 직렬화·Undo·저장/재열기·IMGUI 조작은 이 정적 확인으로 대체하지 않는다.
- **남은 확인:** §15.2 Ua.1~Ua.5. **컴파일 미확인, Unity 미확인, 고지대 B1 계속 보류. HBE-a 보고 후 정지.** Core·런타임·DB SO·운영 에셋 변경과 시험·MSBuild·커밋·push는 하지 않았다. UXR-c/d도 진행하지 않았다.

## 16 낙석·하붑 모양 편집 UX (GSE)

- 승인: 2026-11-02 사용자 “이 설계대로 계획 문서에 반영하고 구현도 진행”. 설계 원본은 [§23 GSE](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#23-낙석하붑-모양-편집-ux-제안-gse)다. 이 계획 작성과 GSE-a 구현을 같은 요청 범위로 수행하고 Phase 끝에 보고한다.
- 목표/구조: 기존 낙석·하붑 상대 칸 목록을 공통 IMGUI 격자로 편집한다. 격자는 임시 획/보기만 소유하고 확정 목록은 창의 SoChange를 거친다. Assets/Validation/Creation·Core 자동 위치는 그대로 쓴다.
- 실행: writing-plans → executing-plans, 현재 Unity checkout의 feature/Hojin/LastStageSelect에서 직접 진행한다. §3.1과 사용자 지시가 스킬의 시험·검토 에이전트·커밋 절차보다 우선한다. 새 시험·시험 도우미·MSBuild·Unity 실행·에이전트·커밋·push 없음. 컴파일/재생은 사용자 확인이며 **컴파일 미확인**으로 보고한다. 아래 상태/실행 기록을 이 Phase의 작업 기록으로 쓴다.
- 공통 제약: Unity 6000.0.73f1, 기존 IMGUI 유지, 새 .cs에 .meta, UTF-8/LF·Allman·4칸. Core·DB SO 코드/스키마·운영 에셋·DT 동작·타일/공개 판정·고지대 개선·UXR-c/d는 변경하지 않는다. CurrentFormat 5 유지.

### 16.1 GSE-a Task

파일은 별도 표기 외에 `Assets/ProjectL/Script/Tool/Editor/` 아래다.

- [x] Gs.1 새 `CombatStageSoGimmickShapeEditor.cs`(+ .meta): `bool Draw(IReadOnlyList<Vector2Int> source, bool editable, out List<Vector2Int> changed)`로 격자와 칠하기/지우기·이동/확대/전체 보기를 제공한다. `bool IsInteracting`·`void Cancel()`·`void ResetView()`를 조립에 공개한다. 드래그 중 임시 목록만 수정, 중복 없는 칠하기/지우기, 빠른 이동은 선분의 칸을 이어 채움, 격자 밖 MouseUp에서도 한 번 확정. 바뀐 획만 각 축 최솟값을 빼 정규화하고 보기 원점을 같은 양만큼 옮긴다. 빈 모양은 허용·안내, 범위 차가 int를 넘으면 작업본을 바꾸지 않고 이유를 표시한다. 전체 보기/확대는 음수·큰 좌표를 자르지 않으며 매우 축소된 보기는 다시 확대 후 칠하도록 안내한다. 원본 순서/중복/잘못된 값을 보기만 해서는 고치지 않는다.
- [x] Gs.2 새 `CombatLayoutWindow.SoGimmickShapes.cs`(+ .meta): 낙석/하붑 편집기와 세부/고급 펼침 상태를 소유한다. `DrawSoGimmickShape(bool haboob)`·`ApplySoGimmickShape(bool haboob, List<Vector2Int> cells, string label)`로 해당 Document 목록에만 SoChange를 적용하고 획마다 Undo 그룹을 구분한다. 기본 모양 버튼은 해당 공개 Spec에서 새 목록을 복사한다. 작업본 교체/Undo에서 편집기 보기를 초기화하며 새 영구 직렬화 필드는 만들지 않는다.
- [x] Gs.3 `CombatLayoutWindow.SoMode.cs`·`CombatLayoutWindow.SoHaboob.cs`: 기존 기믹 설정에 모양 격자를 연결하고 수치는 세부 설정, 기존 X/Y 목록은 고급 좌표로 유지한다. 기존 레일/낙석 경고/하붑 한계 안내는 남긴다. `DrawSoGimmickLinks()` 바로가기는 배치 Sidebar에서 해당 제목으로 이동하며 기믹을 켜거나 새 배치 데이터를 만들지 않는다. `DrawSoGimmickHeading(string label, bool haboob)`가 이동할 제목을 기록한다.
- [x] Gs.4 수명 연결: `CombatLayoutWindow.cs`의 OnDisable에 새 격자 취소만 더하고, 새 partial의 OnLostFocus도 취소한다. `HandleSoGimmickShapeCancellation()`을 공통 머리줄 전에 호출해 Esc/명령/Ignore/진행 중 휠 입력을 취소한다. `SoWorkspace.CancelSoWorkspaceInput`·`OpenSoInspector`, SoChange·검사/저장/목록 새로고침/스테이지 열기/등록/생성·OnSoUndoRedo에서 미완료 획을 취소한다. 포인터 캡처는 소유한 id일 때만 반환한다. 기존 맵/타일 Undo·취소·단축키 동작은 수정하지 않는다.
- [x] Gs.5 사용법·View/민형님 공유 메모·설계/계획 상태 갱신: 모양의 의미, 칠하기/지우기·한 획 Undo·기본 모양·고급 좌표·이동/확대·자동 위치/필드 수용 한계, Unity 미확인을 기록한다. 공유 미전달 유지. 소스/호출 경계·diff·UTF-8/LF/.meta 유일성 정적 확인 후 GSE-a를 보고한다.

### 16.2 중점 검토 및 사용자 Unity 확인 (모두 미확인)

- [ ] Gu.1 빠른 드래그/되짚기/밖에서 놓기/빈 칸 지우기/빈 모양: 칸 누락·반전·중복·의도 없는 Dirty 없음. 한 획 Undo/Redo와 검사 상태·초안이 함께 바뀜(Gs.1/2).
- [ ] Gu.2 Esc·포커스 상실·검사/저장·도구/분류/스테이지 전환·Undo·재컴파일/닫기: 미완료 획과 hotControl 해제, 늦은 MouseUp으로 다른 작업본이 바뀌지 않음. 기존 맵/타일 입력은 유지(Gs.4).
- [ ] Gu.3 원점 행/열 지우기, 음수/큰 좌표, 중복 고급 좌표: 단순 열기/보기 이동은 값·순서 보존, 칠한 모양만 상대 좌표 자동 맞춤, 화면 위치와 다음 획 대응 안정. 정수 범위를 벗어나는 변환은 원본 보존(Gs.1/2).
- [ ] Gu.4 낙석/하붑 바로가기·좁은 창·꺼진 기믹: 해당 제목에 도착하고 기존 사용 여부/수치 유지. 기본 모양은 공개 Spec의 낙석 1칸/하붑 12칸이며 주기·피해는 그대로. 세부/고급 입력과 기존 오류 표시 모두 접근 가능(Gs.2/3).
- [ ] Gu.5 저장/재열기·켜기/끄기·새 형식 초안·배치 비우기/복제와 기존 레일/토네이도/영역·타일 Ctrl+C/V/공개 대상 회귀. 전투 진입에서 자동 위치와 주기를 확인하며 격자 편집을 고정 위치 지정으로 오해하지 않음(Gs.3/5).

### 16.3 실행 기록

- 시작: 설계 §23 승인과 Task 의존성을 대조했다. Gs.1의 Draw 결과를 Gs.2가 SoChange로 반영하고, Gs.3은 두 패널의 UI만 연결, Gs.4는 같은 편집기 Cancel을 수명에 조립한다. 현재 작업본 스키마·저장/검사 계약 변경은 없다.
- 작업 시작 시 다른 변경은 NewStageTest.prefab·폰트 2개·CLAUDE.md·낙석 View 계획에 있었다. 해당 변경은 수정하지 않는다. 설계 §23 초안은 앞선 같은 작업의 변경이다.
- **Gs.1~Gs.5 코드·문서 반영. 컴파일 미확인, Gu.1~Gu.5 Unity 확인 대기, 커밋 전.** 다른 Phase나 고지대 개선을 진행하지 않았다.
- 격자: 새 편집기에 칠하기/지우기·선분 칸 연결·임시 획·MouseUp 확정·변경 없음 판정, 각 축 원점 맞춤과 화면 원점 보정, int 범위 확인을 구현했다. 표시용 원점/배율은 작업본과 분리했다. 전체 보기·휠 확대/축소·가운데/Alt 드래그·칸 크기로 큰/음수 모양을 읽을 수 있으며, 축소 중 칠하기는 확대 안내로 막는다. Layout 이벤트의 임시 Rect로 획이 취소되지 않도록 Rect 변경 판정은 Layout을 제외한다.
- 조립: 두 패널에서 같은 격자를 사용하고 공개 Spec 기본 모양만 복사한다. 확정은 SoChange 한 번과 분리된 Undo 그룹으로 연결했다. 기존 X/Y 함수 본문은 그대로 고급 좌표에 남기고 수치 입력은 세부 설정으로 묶었다. Sidebar 바로가기는 정의 목록의 접힘과 무관하게 접근하며 해당 기믹 제목으로 스크롤한다.
- 수명: 공통 입력 취소·명령·Undo·포커스 상실·OnDisable에 새 편집기 취소를 연결했다. 취소는 해당 편집기가 소유한 hotControl만 해제한다. 스테이지 교체/Undo 시 보기를 다시 맞추며 SO/DT 전환과 기존 맵/타일 취소 구현은 유지했다. 미완료 획은 영구 작업본/초안에 넣지 않는다.
- 정적 근거: git diff --check 통과. 기존 DrawSoRockfallFootprint/DrawSoHaboobFootprint·CopySo/PasteSo·DrawSoGimmicks 다섯 함수가 HEAD와 동일함을 텍스트 비교했다. Document/Assets/Validation/Creation/Preview/Visuals/SoTiles/SoUx는 diff가 없다. 새 .meta GUID 두 개의 유일성과 변경 13개 파일의 UTF-8/LF·BOM/대체문자 없음·줄 끝 공백 없음을 확인했다. 두 패널의 모든 기존 수치 대입과 토글/경고·각 취소 호출을 자체 검토했으며 독립 리뷰나 Unity 실행 증거는 아니다.
- 문서: 사용법 §5.13, 민형님 §8·View §13 공유 메모와 설계 상태를 갱신했다. 실제 자동 위치·필드 수용/전투 진입·연출은 기존 Core/View가 소유한다. 공유 미전달이다.
- 설계 판단: 기존 EditorWindow 내부 기능으로 런타임 서비스/Factory 등록을 늘리지 않았다. 표시/입력은 새 편집기, 값 변경/기믹 연결은 partial, 저장·검사는 기존 경로로 SSOT·SOLID·JMH/FW·변경 독립성을 유지한다. 새 시험·MSBuild·에이전트·커밋·push 없음. **GSE-a 끝에서 보고·정지, 고지대 B1 계속 보류.**

## 17 낙석 터널 폭발 선택 (RTE)

- 승인/원본: 2026-11-02 사용자 “전투 스테이지 편집 툴에도 적용해주세요”. [설계 §24](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#24-낙석-터널-폭발-켜기끄기-설계안-rte)에 따른 RTE-a 한 Phase를 구현하고 보고한다.
- 목표/구조: 스테이지별 View SO에 폭발 생략 값을 저장하고, 기존 Factory → 낙석 Presenter → TryGetTunnelPose 경계로 터널 WarningCues만 제어한다. Tool은 SoChange와 기존 View 입출력을 사용한다. Unity 6000.0.73f1, 기존 IMGUI/SerializedObject를 유지한다.
- 제약/실행: writing-plans → executing-plans로 현재 checkout에서 직접 구현한다. 새 시험·시험 도우미·MSBuild·Unity 실행·검토 에이전트·커밋·push 없음. Core/FieldCamera/Architecture·운영 에셋·공용 RockfallVisual 값은 수정하지 않는다. 고지대 B1·UXR-c/d는 계속 보류다. 아래 §17.3을 실행 기록으로 사용한다.

### 17.1 RTE-a 구현 Task

아래 경로는 `Assets/ProjectL/Script/` 기준이다. 새 파일은 만들지 않는다.

- [x] Rt.1 `Systems/DataBaseSystem/Data/View/Table/StageViewProp/Item/StageViewPropDataSO.cs`: `StageViewPropData`에 직렬화 bool `disableRockfallTunnelExplosion`(기본 false)과 파생 `RockfallTunnelExplosionEnabled`를 추가한다. 기존 에셋은 켜짐이며 규칙/공용 연출 값은 그대로다.
- [x] Rt.2 `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/StageViewPropPresenter.cs`: 같은 SO 조회에서 옵션을 캐시하고 Release에서 true로 초기화, 읽기 속성으로 노출한다. `FieldRockfallPresenter.cs` 생성자 마지막에 필수 `bool tunnelExplosionEnabled`를 받고 `TryGetTunnelPose(out Pose pose)`가 꺼짐이면 누락 경고 없이 false를 반환한다. `ICombatRockfallField.cs`의 정상 false 계약 주석을 맞춘다. `Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`·`CombatTraitPreviewFactory.cs` 두 호출에서 옵션을 명시적으로 넘긴다(배경이 없는 기존 경로는 true). 앵커 검색/중복 검사, Transform·능선·EventHandler는 그대로다.
- [x] Rt.3 `Tool/Editor/CombatStageSoDocument.cs`: `CombatStageSoView.disableRockfallTunnelExplosion`, CurrentFormat 5→6. `CombatStageSoAssets.cs`: 필드 경로 상수와 ReadView/WriteView bool 매핑. `CombatLayoutWindow.SoMode.cs`: 낙석 세부 설정에 긍정형 “터널 폭발 사용”, SoChange로 반전 저장, 낙석 꺼짐/짝 View 없음은 입력 비활성·값 보존, 누락/효과 범위 안내. `CombatLayoutWindow.SoUx.cs`: 꺼짐이면 터널 누락 안내만 제외. 기존 초안/Undo/Dirty·검사 상태/복제/배치 비우기 경로를 대조하며 영역/타일 복사는 수정하지 않는다.
- [x] Rt.4 `Tool/SO모드 사용법.md`, 본 설계/계획, 기존 민형님·View 공유 메모에 위치·효과 범위·초안 갱신·사용자 확인을 반영한다. 낙석 View 설계 머리말에는 옵션 계약 원본(편집 설계 §24)을 연결한다. 공유 미전달 유지. 변경 범위/호출부·diff·UTF-8/LF를 정적으로 검토하고 보고한다.

### 17.2 중점 검토 및 사용자 Unity 확인 (모두 미확인)

- [ ] Ru.1 기존/새 View 에셋은 기본 켜짐. 스테이지별 끄기 → 검사/저장 → 다시 열기, Undo/Redo, 새 형식 초안 복원, 배치 포함/비우고 복제에서 값 보존(Rt.1/3).
- [ ] Ru.2 코드 갱신 전 미저장 편집 저장. 구형 초안/열린 작업본은 기존 거절/다시 읽기를 따르고, 짝 View가 없는 스테이지는 토글 비활성·안내. 낙석 자체를 껐다 켜도 옵션 보존(Rt.3).
- [ ] Ru.3 터널 사용 끔+터널 없음은 누락 안내 없음. 켜짐+터널 없음은 저장 가능한 안내. 양쪽 모두 능선 누락 안내와 중복 앵커 오류 유지. MapDefinition 검사 한계 유지(Rt.2/3).
- [ ] Ru.4 실제 전투에서 꺼짐은 터널 FX/폭발 흔들림만 생략. 경고 칸/느낌표/경고음·능선에서 낙하·착지 FX/흔들림/소리·자동 위치/피해/묶임 유지. 터널 아래에 능선 끝점이 있어도 유지(Rt.2).
- [ ] Ru.5 꺼진 스테이지에서 켜진 스테이지로 전환하면 설정이 남지 않음. 특성 미리보기 생성/해제와 기존 하붑·기차·모양/영역/타일 편집이 유지됨(Rt.2/3).

### 17.3 실행 기록

- 시작: 설계 §24 승인. Rt.1의 속성을 Rt.2가 읽고, Rt.3의 저장 경로는 동일 직렬화 필드와 연결한다. 생성자는 실제 사용 두 곳뿐이며 시험 코드 호출은 없다. 기존 SoChange/Creation/형식 검사 재사용으로 새 저장/Undo 정책은 만들지 않는다. 사용자가 작업본을 먼저 저장하도록 구현 전 안내했다.
- 기존 변경: GSE-a 코드/문서와 Combat_Test2·StageViewProp_Test2·NewStageTest.prefab·폰트·CLAUDE.md·낙석 View 계획의 변경을 보존한다. 기존 계획의 §16 기록은 당시 확인 결과로 남긴다.

- **Rt.1~Rt.4 코드·문서 반영, 컴파일 미확인, Ru.1~Ru.5 Unity 확인 대기, 커밋 전.** 설계 변경 없이 RTE-a 범위만 진행했다.
- 값/조립: View SO에 생략 bool(false 기본)과 긍정형 읽기 속성을 추가했다. 배경 생성 때 캐시하고 Release 때 true로 초기화한다. 실제 표시/특성 미리보기 두 Factory가 낙석 Presenter에 필수 bool을 넘긴다. 꺼짐이면 TryGetTunnelPose가 누락 경고 전에 false를 반환한다. EventHandler의 경고음·낙하·착지 경로와 Transform/앵커 검색은 유지했다.
- Tool: 작업본 형식 6, View bool Read/Write, 낙석 세부 설정의 긍정형 토글과 도움말, 터널 누락 안내의 조건만 추가했다. SoChange의 Undo·Dirty·초안·재검사 경로와 Creation의 배치 비우기/복제 경로를 재사용한다. 초안 구형 거절/열린 작업본 다시 읽기는 기존 정책이다.
- 정적 확인: 대상 16개 파일의 UTF-8/LF·BOM/대체문자 없음과 git diff --check 통과. 직렬화 이름/읽기/쓰기, 생성자 사용 두 곳, 옵션 해제 수명, 사용 여부 반전, 같은 이름 중복 검사 유지, 기존 형식 검사와 SoChange 호출을 대조했다. EventHandler/Creation/Validation은 git diff가 없고, SoMode의 앞선 GSE 변경과 이번 토글/도움말 추가를 구분해 자체 검토했다. 실제 Unity 직렬화·IMGUI·Undo·저장·재생 성공은 이 확인으로 대체하지 않는다.
- 구조: SSOT는 스테이지별 View SO, Tool 입력/변환과 Factory 조립·Presenter 표시 책임을 분리했다. 정상 false 계약을 문서화하고 기존 ICombatRockfallField만 사용한다(SOLID S/O/L/I/D). 서비스 생성/등록/Export 경로 추가 없음(JMH/FW), 공통 경계는 직렬화와 생성자 두 곳이며 낙하/착지/하붑/타일 기능의 구현을 수정하지 않았다.
- 문서: 사용법 §5.11·민형님 §9·View §14에 옵션과 형식 갱신을 기록했고 낙석 View 설계 머리말에 계약 원본 링크를 추가했다. 공유 미전달이다. 작업 중 나타난 다른 작업자의 문서 정리와 에셋 변경은 손대지 않았다.
- **RTE-a 보고 후 정지. 고지대 B1·UXR-c/d 보류 유지.** 새 시험·시험 도우미·MSBuild·Unity 실행·검토 에이전트·커밋·push 없음.

## 18 하붑 시작 위치 지정 (HAT)

- 승인/원본: 2026-11-03 사용자 "도구에 넣어". [설계 §25](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#25-하붑-시작-위치-지정-hat)의 HAT-a 한 Phase를 구현하고 보고한다. HAT1~HAT6은 권장으로 진행했으며 사용자 확인 대상이다.
- 제약: 새 시험·MSBuild·검토 에이전트·커밋·push 없음. Core·DB SO·런타임·운영 에셋 무변경. 새 파일 없음.

### 18.1 HAT-a 구현 Task

아래 경로는 `Assets/ProjectL/Script/Tool/` 기준이다.

- [x] Ht.1 `Editor/CombatStageSoDocument.cs`: `CombatStageSoHaboob.fixedInitialAnchor`·`initialAnchor`, 칸 함수 `StartCells()`(공개 스펙 + 시작 위치, 꺼짐·무작위·잘못된 값은 null), 형식 6 → 7.
- [x] Ht.2 `Editor/CombatStageSoAssets.cs`: 칸 이름 상수 둘, `ReadHaboob`·`WriteHaboob` 매핑.
- [x] Ht.3 `Editor/CombatStageSoValidation.cs`: `TryCheckHaboob`가 지정 시작 위치를 스펙에 넘김(음수). `Editor/CombatLayoutWindow.SoMode.cs` `SoProblems`: 판 밖이면 문제(칸 = 시작 위치).
- [x] Ht.4 `Editor/CombatLayoutWindow.SoHaboob.cs`: 토글·X·Y·뜻 안내·덮는 칸 범위 줄, 안내 문구 고침. `Editor/CombatLayoutWindow.SoMode.cs`: 구름 색, 2D 칸 덧칠, 3D 채우기, 범례 한 줄, 판 안 칸 함수.
- [x] Ht.5 `SO모드 사용법.md` §5.12·§8.2, 이 계획·설계 기록.

### 18.2 사용자 Unity 확인 (모두 미확인)

- [ ] Hu.1 업데이트 전 미저장 편집 저장. 기존 스테이지를 열면 시작 위치 지정 꺼짐·X·Y 0, 저장해도 다른 하붑 값·배치 그대로.
- [ ] Hu.2 하붑 켜기 → 시작 위치 지정 → X·Y 입력: 2D·3D 맵에 구름 칸이 그려지고 범례·덮는 칸 줄이 맞다. 끄면 그림이 사라지고 X·Y 값은 남는다. Undo/Redo·초안·다시 열기에 값이 따라간다.
- [ ] Hu.3 판 밖 시작 위치(예: 9×9 판에서 X 6)는 검사에서 저장이 막히고 오류를 누르면 그 칸으로 간다. 음수는 "하붑 값:" 오류. 하붑이나 지정을 끄면 두 오류 모두 사라진다.
- [ ] Hu.4 저장 뒤 Play From Boot PlacedStage에서 구름이 맵에 그린 칸에 나오고, 이동은 무작위다.
- [ ] Hu.5 배치 비우고 새로 만들기·배치까지 복제가 시작 위치를 보존한다. 영역 Ctrl+C/V·낙석·토네이도·타일 편집에 변화 없음.

### 18.3 실행 기록

- **Ht.1~Ht.5 코드·문서 반영(2026-11-03), 컴파일 미확인, Hu.1~Hu.5 Unity 확인 대기, 커밋 전.** 새 파일 없음. 설계와 다른 결정 없음.
- 칸 계산은 `CombatStageSoHaboob.StartCells()` 하나를 패널 '덮는 칸' 줄·2D 칸 덧칠·3D 채우기·범례·판 밖 검사가 함께 부른다. 모양은 Core `HaboobGimmickSpec`(주기·경고는 기본값으로 넘겨, 주기 값이 잘못돼도 그림은 남음)이 검사한 목록이고, 잘못된 모양·음수 시작 위치면 null → 그리지 않고 "하붑 값:" 오류가 알린다.
- 판 밖 검사는 `SoProblems`에서 토네이도 고정 중심 검사 다음에 둔다. 오류 칸은 시작 위치(판 밖이어도 '맵 밖 배치' 오류처럼 그 좌표를 넘김).
- 하붑 읽기/쓰기는 칸 이름 상수 둘(`fixedInitialAnchor`·`initialAnchor`)만 더했다. 기존 에셋은 YAML에 두 칸이 없어도 직렬화 기본값(꺼짐·(0,0))으로 읽히고, 도구에서 저장하면 두 칸이 에셋에 처음 써진다.
- 정적 확인: CellInput 직렬화 이름 `x`·`y`, `CellCoord(int, int)`, 스펙 생성자 인자 이름, 줄바꿈 CRLF 유지를 대조했다. 실제 IMGUI·Undo·저장·전투 결과는 Hu.1~Hu.5로 확인한다.
- **HAT-a 보고 후 정지.** 새 시험·MSBuild·검토 에이전트·커밋·push 없음.

## 19 배치 도구로 하붑 시작 위치·낙석 켜기 (GP)

- 승인/원본: 2026-11-03 사용자 결정([설계 §26.6](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#266-결정2026-11-03-사용자)). GP-a 한 Phase를 구현하고 보고한다.
- 제약: 새 시험·MSBuild·검토 에이전트·커밋·push 없음. Core·DB SO·런타임 무변경. 새 파일 없음, 작업본 형식 7 유지.

### 19.1 GP-a 구현 Task

아래 경로는 `Assets/ProjectL/Script/Tool/` 기준이다.

- [x] Gp.1 `Editor/CombatStageSoDocument.cs`: `CombatStageSoHaboob.ShapeOffsets()`(Core 공개 계약이 검사한 모양, 잘못되면 null).
- [x] Gp.2 `Editor/CombatLayoutWindow.SoMode.cs`: 도구 `HaboobStart`·`Rockfall`(열거·이름 끝), 누르기 갈래, 안내 줄, 도구 줄 그리기 호출, 끌어 놓기 받기·그림자 갈래, Delete가 구름 칸이면 시작 위치 비우기, 고른 칸 정보 한 줄.
- [x] Gp.3 `Editor/CombatLayoutWindow.SoHaboob.cs`: 누른 칸 → 시작 위치 식(한 곳), 놓기·비우기, 도구 줄(상태·비우기·설정 바로가기), 끌어 놓기 그림자.
- [x] Gp.4 `Editor/CombatLayoutWindow.SoGimmickShapes.cs`: 왼쪽 패널 '기믹 배치' 항목 둘(누르기 = 도구, 끌기 = 맵에 설치)과 "기믹 설정" 제목, 낙석 켜기·끄기·도구 줄.
- [x] Gp.5 `SO모드 사용법.md` §5.12·§5.11, 이 계획·설계 기록.

### 19.2 사용자 Unity 확인 (모두 미확인)

- [ ] Gu.1 도구 줄 끝에 '하붑 시작'·'낙석'이 있고 기존 도구 버튼 순서·동작은 그대로.
- [ ] Gu.2 하붑 시작 도구로 칸을 누르면 그 칸이 구름 가운데 근처가 되도록 구름이 놓이고(하붑·시작 위치 지정도 켜짐), Undo 한 번으로 되돌아간다. 다른 칸을 누르면 옮겨진다. 구름이 판 밖으로 나가는 칸은 놓지 않고 안내한다. 2D·3D 모두.
- [ ] Gu.3 왼쪽 '하붑 시작 위치'를 맵으로 끌면 놓일 구름 칸 그림자(초록/빨강)가 보이고, 놓으면 그 자리에 설치된다. '낙석 켜기'를 아무 칸에 끌어 놓으면 낙석이 켜진다.
- [ ] Gu.4 선택 도구로 구름 칸(배치 없는 칸)을 누르고 Delete → 시작 위치가 비워져 구름이 사라지고 하붑은 켜진 채. 배치가 있는 칸은 배치부터 지워진다. 도구 줄 "시작 위치 비우기(무작위로)"도 같다.
- [ ] Gu.5 낙석 도구로 아무 칸을 누르면 켜지고, 이미 켜져 있으면 안내만. "낙석 끄기"로 꺼진다. 저장·다시 열기·초안에 값이 따라간다.

### 19.3 실행 기록

- **Gp.1~Gp.5 코드·문서 반영(2026-11-03), 컴파일 미확인, Gu.1~Gu.5 Unity 확인 대기, 커밋 전.** 새 파일 없음, 작업본 형식 7 유지.
- 누른 칸 → 시작 위치 식은 `SoHaboobAnchorAt` 하나(누르기·끌어 놓기 그림자가 함께 씀), 모양은 `CombatStageSoHaboob.ShapeOffsets()`(Core 공개 계약). 판 밖 판단은 놓기·그림자·저장 검사가 같은 "모든 칸이 판 안"이다.
- 끌어 놓기는 정의 목록 끌기(§20)의 `HandleSoPaletteDrag` → `DropSoPalette` → `HandleSoCell` 한 길을 그대로 쓰고, 기믹 항목은 붓 없이 도구만 바꾼다. 끄는 동안 그림자는 기존 `SoFrames`의 끌어 놓기 갈래에 기믹 갈래 하나를 더했다(마우스를 올린 칸의 그림자는 Q-GP5 A로 없음).
- Delete: 상자 선택이 없고 고른 칸에 지울 배치가 없을 때만 구름 칸이면 시작 위치를 비운다. 상자 선택 Delete와 지우기 도구는 그대로(하붑을 건드리지 않음).
- 설계와 다른 점: GP1의 "도구를 고르면 오른쪽에 설정이 열림"은 설정 작업 영역으로 바뀌어 배치 화면을 떠나므로, 도구 줄의 상태·버튼·바로가기로 대신했다(설계 §26.6 구현 보충).
- 정적 확인: `git diff --check` 통과, 줄바꿈 CRLF 유지, 도구 열거는 끝에만 더함, 도구에 따라 갈리는 switch 넷(끌어 놓기·안내·층·붓)의 기본 갈래를 대조. 실제 IMGUI·끌어 놓기·Undo는 Gu.1~Gu.5로 확인한다.
- **GP-a 보고 후 정지.** 새 시험·MSBuild·검토 에이전트·커밋·push 없음.
