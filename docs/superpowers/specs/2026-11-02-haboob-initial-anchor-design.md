# 하부브 시작 위치 지정 설계·계획 (2026-11-02)

> 상태: **Core·DB 코드 완료(HA-a, 2026-11-02, 시험 통과·새 실패 0, 커밋 전 — §5).** 사용자(Core 작업자) 요청 "시작 위치 정할 수 있게 수정", 기준점은 사용자 결정 **A(모양의 상대 좌표 (0,0))**.
> 원본 설계 [stage-gimmick-system-design](2026-10-31-stage-gimmick-system-design.md) U12·O4의 "무작위 최초 배치"를 이 문서가 바꾼다(지정이 없으면 그대로 무작위).

## 1. 지금 코드 (`06835a862`)

- 모양은 `HaboobGimmickSpec.FootprintOffsets`이고, X·Y 최솟값이 0이어야 한다(`StageGimmickMask.CopyNormalized`). 그래서 상대 좌표 (0,0)은 늘 모양을 감싸는 상자의 모서리다(모양 칸이 아닐 수 있음 — 기본 12칸 모양은 (0,0)이 빈칸).
- `HaboobGimmickRule.SelectCells`가 이 (0,0)이 놓일 보드 칸(기준점)을 무작위로 고르고, 칸 = 기준점 + 상대 좌표로 만든다. 전투 준비(`Prepare`)와 재배치가 같은 함수를 쓴다.
- 재배치는 지금 기준점(`current`의 X 최솟값·Y 최솟값)만 빼고 다시 무작위.

## 2. 결정

| 번호 | 내용 | 근거 |
|---|---|---|
| HA1 | 시작 위치 = 상대 좌표 (0,0)이 놓일 보드 칸. Core가 이미 쓰는 기준점 그대로라 새 개념·반올림이 없다. | 사용자 A |
| HA2 | 지정한 시작 위치는 **전투 준비 때 한 번만** 쓴다. 재배치는 지금 규칙(무작위, 지금 자리 제외) 그대로. | 권장안(§0 대화), 재배치 규칙을 바꾸면 따로 정함 |
| HA3 | 시작 위치를 지정하면 난수를 쓰지 않는다. 그 스테이지는 하부브 뒤 기믹(토네이도)의 최초 난수 결과가 지정 전과 달라질 수 있다. 지정 안 한 스테이지는 그대로. | 난수는 실제로 고른 것만 소비 |
| HA4 | 값 꼴: 스펙 `CellCoord? InitialAnchor`(끝 선택 인자, null = 무작위, 토네이도 `InitialCenter`와 같은 꼴). 음수면 스펙 생성 때 `ArgumentOutOfRangeException`. | 판 크기를 모르는 검사는 스펙에서 |
| HA5 | 모양 전체가 판 안에 들어가지 않는 시작 위치는 **전투 준비 때** `InvalidOperationException`(F1). 들어가는 범위 = 무작위 후보 범위와 같은 식 하나. | 원본 설계 §5.2 "판 크기 검사는 전투 준비" |
| HA6 | DB 입력: `HaboobGimmickAuthoring`에 `fixedInitialAnchor`(bool, 기본 꺼짐) + `initialAnchor`(`CellInput`). Unity가 nullable을 직렬화하지 못해 둘로 둔다. Data `HaboobGimmickData.InitialAnchor`, 어댑터가 스펙으로 넘김. | 기존 에셋은 꺼짐 = 지금 동작 |
| HA7 | 전투 스테이지 편집 도구(SO 모드, 호진님 영역)는 고치지 않는다. 도구는 이 두 칸을 모르고 저장 때 그대로 남긴다. 값은 `CombatStageData` 인스펙터(기본 그리기)에서 넣는다. 도구에서 고르고 덮이는 칸을 미리 그리는 것은 호진님께 알림. | 영역 |

## 3. 변경 영향

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| `HaboobGimmickSpec` 인자 하나 | 스펙을 만드는 곳(어댑터·도구 검사·시험) — 끝 선택 인자라 호출 그대로 | `HaboobGimmickTests`, `StageGimmickProgressionTests` | 낙석·토네이도 계산 |
| `HaboobGimmickRule` 최초 배치 | 재배치(같은 칸 계산 함수를 씀) | `HaboobGimmickTests`, `HaboobPublicationTests` | 하부브 지정 금지 규칙(칸 목록만 읽음) |
| DB 입력 두 칸·Data·어댑터 | 기존 에셋(꺼짐), SO 모드 도구 읽기·쓰기(필드 이름으로 기존 칸만) | `StageGimmickAuthoringTests` | View(칸 목록만 읽음, 변경 없음) |

## 4. Task (HA-a 하나)

1. 스펙 `InitialAnchor`·검사.
2. 규칙: 판 안 범위 계산 하나(`AnchorLimit`)·칸 만들기 하나(`CellsAt`)로 나누고, `Prepare`가 지정 위치면 검사 뒤 그 칸, 아니면 무작위.
3. DB 입력 두 칸·Data·어댑터.
4. 시험(Core·CoreIntegration만): 지정 위치에 놓임·난수 그대로, 판 밖이면 준비 실패, 음수 거부, 지정 뒤 재배치는 시작 자리를 피함, SO 입력이 준비까지 닿음.
5. 원본 설계 U12 한 줄.

## 5. 실행 기록

- **HA-a(2026-11-02):** §4 그대로. 새 파일 없음.
- **시험(2026-11-02, Unity 배치 모드, Core·CoreIntegration 두 묶음):** 변경 뒤 Core 4,660 중 실패 1, CoreIntegration 847 중 실패 24. 새 시험 4개와 하부브·기믹 시험 54개 모두 통과. 기준(HEAD `3216c1c99`, 이 변경 파일 7개만 되돌림) Core 4,657 중 실패 1, CoreIntegration 846 중 실패 24. 실패 25개는 두 판이 이름까지 같다 → 새로 생긴 실패 0(기존 실패는 도발 발동 수·DB 에셋·덱 UI·휴식 시설·카메라, 이 변경과 무관).
