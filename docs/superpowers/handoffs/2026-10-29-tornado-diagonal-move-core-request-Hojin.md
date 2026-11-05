# 토네이도 대각선 이동 — Core 요청 (초안)

- 작성자: 호진(이호진)
- 작성: 2026-10-29, 호진
- 받는 사람: 천명님(Core)
- 상태: **초안, 미전달.** 이동 규칙의 세부(거리·방향·꺾는 곳)는 기획 확인과 Core 협의로 정한다(사용자 2026-10-29). §3의 질문에 답이 오면 §4를 확정한다.
- 범위: Core 규칙·사실·사건 계약만 다룬다. View 연출(경로를 따라 외형 옮기기, 생성·소멸 교체)은 [토네이도 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md#11-옛-지형-토네이도-10-1710-31-대신함)에서 따로 정한다.
- 기준: HEAD `354bd8d7`의 Core 코드를 직접 대조했다. 경로는 `CoreCaculator/` 아래다.
- 기록 위치: [Core 전달 문서 §52](2026-09-24-view-core-data-boundary-handoff.md), 토네이도 설계 §8

## 1. 요청 요지

지금 토네이도는 발동하면 **필드 전체에서 무작위로 고른 칸으로 순간이동**합니다. 이것을 **칸을 따라 대각선으로 움직이며 V자·W자를 그리는 이동**으로 바꾸려고 합니다(사용자 2026-10-29).

- 거리와 방향은 Core 작업자와 정하기로 했습니다(사용자). 그 밖의 세부는 기획에서 정할 일이며, 아직 정해지지 않았습니다.
- 그래서 이 문서는 **바뀌는 Core 지점**, **세부가 어떻게 정해지든 View가 받아야 할 사실**, **정할 질문**을 정리합니다.
- 기획서 확인: [Terrain, Obstacle, Object 기획서.pptx](<C:/Users/Hyeon2-400/Dropbox/Project L/기획 브레인스토밍/안재령의고뇌/Terrain, Obstacle, Object 기획서.pptx>)(24장, SHA-256 앞자리 `a89e2d2f9e2d1a0f`, 10-29 다시 확인)의 13장은 "3턴이 지날 시 해당 타일의 유닛을 필드 내 랜덤한 위치로 강제 이동, 이후 랜덤한 위치에 다시 토네이도 Terrain 스폰"입니다. **V·W 이동은 기획서에 아직 없습니다.** 기획서가 고쳐지면 그 판을 원본으로 합니다.

## 2. 지금 Core 동작

| 항목 | 지금 | 위치 |
|---|---|---|
| 발동 시점 | 유닛이 판정 칸에 들어간 순간(경유·종점 모두), 그리고 진영 턴 끝 주기(첫 발동·반복 간격 3·3) | `Application/Combat/TornadoCalculator.cs:35` `ResolveEntry`, `:17` `ResolveDue` |
| 휩쓸린 유닛 | 판정 칸의 유닛 하나를 필드 전체 무작위 칸으로(생존 구조물·생존 유닛·출발 칸·벽 제외), 도착 효과 정산 | `TornadoCalculator.cs:53-74`, 후보 `:89` `UnitCandidates` |
| 원본 이동 | 필드 전체 무작위 칸(생존 구조물·다른 활성 Terrain·지금 칸 제외, 유닛·Object 허용). 후보가 없으면 제자리 | `TornadoCalculator.cs:77-78`, 후보 `:99` `SourceCandidates` → `:107` `PlacementCandidates`(최초 배치와 공유) |
| 다음 발동 | `AfterActivation(cell)` = 새 칸 + (다음 발동 + 반복 간격) | `Contracts/Combat/TornadoSourceFact.cs:27` |
| 변경 검사 | 새 사실이 `expected.AfterActivation(final.Cell)`과 같은지 확인 | `Application/Combat/CommandEnvironmentState.cs:61` `ChangeTornado` |
| 난수 | 유닛 목적지·원본 새 칸 모두 환경 난수 `ChooseRandom` | `CommandEnvironmentState.cs:52` |
| 사건 | `CombatCharacterEvent(Relocated, …, tornadoSourceId)` → 도착 효과 → `CombatTornadoEvent(Expected, Final)`. **칸은 옛 칸·새 칸 둘뿐이고 지나간 길은 없습니다** | `TornadoCalculator.cs:65·80`, `Contracts/Combat/CombatEvent.cs:91` |
| 이동 미리보기 | 토네이도가 발동하면 보드 전체 Terrain 후보와 환경 난수를 의존값으로 비교 | `Application/Combat/MovementContextCalculator.cs:38` |
| 정의 값 | 토네이도 머리는 첫 발동·반복 간격 둘 | `DataBaseSystem/Data/InGame/Table/Terrain/Item/TerrainDefinitionDataSO.cs:23-28` |

## 3. 정할 것

G1~G9는 기획 질문이고, 답이 오면 Core와 맞춥니다. G2·G3은 사용자가 "Core 작업자와 정함"으로 정했습니다.

| ID | 질문 | 선택지 예 | 답이 바꾸는 것 |
|---|---|---|---|
| G1 | 한 번 발동에 얼마나 움직이나 | A: 한 번 발동에 V·W 경로 전체를 지나간다 / B: 발동마다 몇 칸씩 가고, 여러 번 발동한 자취가 V·W가 된다 | 사건 하나에 담기는 칸 수, 진행 상태(R2)의 크기 |
| G2 | 거리 | 한 번에 몇 칸, 정의 값으로 둘지 | 정의 값 칸(R7) |
| G3 | 방향과 꺾는 곳 | 시작 방향(무작위·정해짐), 꺾는 곳(필드 가장자리에서 튕김·정해진 칸 수마다·막힌 칸에서) | 진행 상태(R2), 난수 사용(R6) |
| G4 | 막힌 칸 | 생존 구조물·벽·다른 활성 Terrain·필드 밖을 만나면 멈춤·꺾음·건너뜀 | 경로 규칙(R1) |
| G5 | 지나가는 칸의 유닛 | 휩쓸린다 / 휩쓸리지 않는다(기획서는 판정 칸의 유닛만) | 칸별 처리(R4) |
| G6 | 휩쓸린 유닛의 목적지 | 지금처럼 필드 무작위 / 경로 끝 칸 또는 그 주변 | 유닛 목적지 규칙, View 낙하 연출 |
| G7 | 최초 위치 | 지금처럼 무작위(스테이지 seed) / 가장자리 등 정해진 곳. 시작 방향도 함께 | 최초 배치(`InitialTornadoPlacementCalculator`) |
| G8 | 진입 발동 때도 움직이나 | 지금은 진입·주기 발동이 같은 재배치를 쓴다(이 관계도 사용자 확정 기록이 없다) | 발동 두 길의 규칙 |
| G9 | 다음 이동을 미리 보여 주나 | 예고 표시 / 없음 | 다음 경로 공개(R5) |

## 4. 요청 계약

세부가 어떻게 정해지든 필요한 것은 R1~R3, 답에 따라 필요한 것은 R4~R8입니다. 이름은 예시이며 확정 API가 아닙니다.

- **R1 이동 규칙은 Core 한 곳.** 무작위 재배치 대신 경로 규칙을 Core가 계산합니다. View는 경로를 계산하지 않습니다(규칙 사본이 생기므로). `PlacementCandidates`를 최초 배치가 계속 쓰는지, 경로 규칙이 다른 조건을 쓰는지 함께 정해 주세요.
- **R2 진행 상태는 사실에.** 방향·꺾인 횟수처럼 다음 이동에 필요한 값은 `TornadoSourceFact`에 두어 Buffer로 게시해 주세요. `AfterActivation`·`SameValues`·`WithKnownTo`와 `ChangeTornado`(`CommandEnvironmentState.cs:61`) 검사도 그 값을 포함해야 합니다. 같은 사실이면 같은 경로가 나와야 합니다(재현).
- **R3 사건에 지난 칸 목록.** `CombatTornadoEvent`에 시작 칸부터 끝 칸까지 지난 순서대로 칸 목록(예시 이름 `Path`)을 넣어 주세요. 움직이지 않았으면 시작 칸 하나입니다. View는 이 목록을 따라 외형을 옮깁니다.
- **R4 (G5에서 휩쓸린다면) 칸별 처리 순서.** 경로 순서대로 대상을 처리하고, 각 `Relocated`가 경로의 몇 번째 칸에서 일어났는지 View가 알 수 있어야 합니다(사건 순서 또는 칸 위치). 한 발동에 대상이 여럿이 되므로 처리 순서·앞 대상 결과의 반영·도착 효과로 전투가 끝날 때(Terminal) 남은 경로를 정해 주세요.
- **R5 (G9에서 예고한다면) 다음 경로 공개.** 다음 발동 때 지날 칸을 사실로 게시해 주세요. 난수를 쓴다면 미리 뽑아 보관해야 예고와 실제가 같습니다.
- **R6 미리보기·난수 영향.** 경로가 결정적이면 이동 미리보기(`MovementContextCalculator:38`)의 의존 범위가 바뀝니다. 환경 난수를 쓰는 횟수가 바뀌면 같은 스테이지 seed의 다른 환경 결과(휩쓸린 유닛 목적지 등)도 바뀝니다. 바뀌는 범위를 알려 주세요.
- **R7 정의 값 자리.** 거리 등 패턴 값을 `TerrainDefinitionDataSO`의 토네이도 머리에 둘지 정해 주세요(InGame Data는 순수 C#, DB 규칙 P1). 스테이지마다 다르면 스테이지 배치 입력(`CombatStageEnvironmentInput`)도 후보입니다. 저작 칸이 바뀌면 SO 모드 편집 도구도 맞춥니다(호진).
- **R8 (G8) 진입·주기 두 길이 같은 규칙인지.** 다르면 사건에서 어느 발동인지 구분할 값이 필요합니다.

## 5. View가 할 일(참고, 이 요청 밖)

- R3의 칸 목록을 따라 외형을 옮깁니다. 지금의 "옛 칸에서 줄어듦 → 새 칸에서 커짐"(`CombatTornadoAppearance`)을 바꾸는 일이라, 기다리는 셰이더 교체(토네이도 설계 §2.5)와 함께 설계합니다.
- 휩쓸린 유닛 연출은 G6에 따라 낙하 목적지만 바뀝니다(지금 상승·낙하 조각 `CombatTornadoLiftPiece`).
- G9이면 호버 설명(`ViewSystem/Combat/Input/Assist/CombatEnvironmentText.cs:68`)이나 칸 표시에 다음 이동을 보입니다.

## 6. 영향 받는 시험

Core 시험 `TornadoEnvironmentTests`·`InitialTornadoPlacementTests`와 `Framework/EnvironmentEntryPublicationTests`의 기대값이 바뀝니다(천명님 시험). View 쪽은 새 시험을 만들지 않습니다.
