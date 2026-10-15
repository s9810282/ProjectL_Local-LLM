# Task 5 — 영혼 개수 계산과 Buffer 반영 인계

- 작성일: 2026-10-01
- 기준: `feature/Cheon/refactor/core-system`, 작업 시작 `4c376244` 이후 미커밋 변경
- 범위: [이전 계획 Task 5](../plans/2026-09-29-character-buffer-effect-migration-plan.md). Task 6·7, View Plan·Job 구현은 포함하지 않는다.
- 참고: [Core의 View 답변](2026-09-30-combat-core-response-to-view.md), Task 4A View 답장 `2026-09-30-combat-result-task4a-view-reply.md`.

## 1. 제공한 경로

`SoulEffectCalculator`는 불변 `ArtifactLoadoutSnapshot`을 한 번 컴파일하고, 호출마다 Contracts 값만 받아 변경 제안을 계산한다. Buffer, UnitState, StageSession, 가변 로스터를 참조하거나 소유하지 않는다.

| API / 값 | 의미 |
|---|---|
| `Collect(SoulCollectorInput, SoulFieldSnapshot, movementSucceeded)` | 수집자의 보유 카드 연결·개수·기본 최대 HP와 현재 캐릭터 값을 받아 성장/회복/소비를 계산 |
| `SoulCollectorInput` | 메인은 지속 원본도 전달. 서브의 기본 최대 HP는 정의에서 전달. 강제 도착 여부를 명시 |
| `SoulActionChanges` | CharacterChanges와 필드 변경, 소비한 영혼 ID를 묶음. 성장 개수 합계는 소비 수와 일치하고 변경할 보유 카드에 연결되어야 함 |
| `PlanDeaths(field, deaths, isNight, countdown)` | `SoulDeathFacts`에 기록한 실제 사망 위치에서 밤의 적 사망에 대해 생성. 턴 종료라면 생성 후 감소 |
| `SoulDeathFacts` | 생존→사망 변경과 실제 사망 좌표. 강제 이동 뒤 제외된 유닛도 이동 전 좌표로 되돌아가지 않음 |
| `PlanCountdown(field)` | 남은 유닛 턴 수를 감소시키고 0이 되는 영혼 제거 |
| `EvaluateAttackPower(attacker, count, characters)` | 기본 공격력 + 영혼 + 전투 버프 + 기존 Last Stand 계산. 지속 원본·개수는 변경하지 않음 |
| `ICombatStateBufferReadSink.SoulField` | 전장 영혼 목록과 다음 영혼 ID의 실제 불변 조회. 전투 종료 시 비움 |
| `IBattleBufferWriteSink.Apply(SoulActionChanges)` | 필드 원본/위치/ID, 캐릭터 원본/새 개수/HP/자원을 모두 검증한 뒤 함께 반영 |

영혼 하나는 개수 +1, 공격력 +1, 최대 HP +1이다. 회복량은 SO 설정값으로 계산하고 새 최대 HP에서 제한한다. 메인의 현재 HP는 즉시 지속 원본에 반영하며 기본 최대 HP·기본 공격력을 증가시키지 않는다. 같은 종류의 카드도 수집한 보유 ID만 성장한다. 적의 숫자 EntityId가 플레이어 ID와 겹쳐도 플레이어 영혼 수를 사용하지 않는다.

실패하는 검증은 첫 쓰기 전에 끝낸다. 필드의 최종 설치는 캐릭터 반영 후 단순 대입만 수행한다. 미리보기는 같은 계산을 호출하고 Apply를 호출하지 않는다.

## 2. 유지한 처리 시점과 담당

- 자발적 이동은 비용이 확정되고 pending 비용이 0, checkpoint가 최종 anchor가 된 뒤 수집한다. 수집은 이동 자원을 돌려주거나 다시 정산하지 않는다.
- 강제 이동의 도착 수집은 기존 이동 비용·checkpoint를 보존한다. 생존한 수집자의 최종 점유 셀로 판정한다.
- 기존 사망 투영 순서대로 EntityId 순으로 생성하고, Artifact 출처 순서를 유지한다. 수명은 5 유닛 턴이며 턴 종료 생성분도 기존 규칙대로 그 종료에서 4로 감소한다.
- 기존 효과 엔진·Last Stand 조건을 재사용한다. 공격 방식은 변경하지 않았다.
- 기존 UnitState/StageSoulService 인스턴스 경로는 Task 7 전환·Task 8 제거까지 남는다. 새 경로와 값을 동기화하지 않는다. 새 계산은 기존 인스턴스 Apply를 호출하지 않는다.
- View가 Plan·Job과 호출 연결을 맡는 합의는 유지한다. Flow 테스트의 `FakeState`에 새 조회 멤버 한 줄만 추가했다.

**Task 4A View 답장 §4의 “Task 5에서 처리 멤버 확정 후 Job 연결”은 구현 시점 보정이 필요하다.** 이번 제공물은 영혼 계산/반영 경로다. `TryProcessCommand` / `TryProcessPreview` / `TryAdvance`의 실행 구현, 처리 결과/번호/국면 게시와 Job 연결은 계획 Task 7·8 범위다. 새 영혼 Apply만으로 처리 완료 결과를 만들거나 View가 이를 직접 게임 규칙으로 조립하지 않는다. Task 4A 답장의 결과 순번·자동 진행 합의와 F2 종료 수명은 별도 후속 계약으로 유지한다.

## 3. G9 — 실제 영혼 SO는 아직 연결 불가

대상: `Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/Artifact_SoulDrop.asset`, Artifact ID 1003.

실제 `ArtifactDefinitionDataTableSO` → `ArtifactDefinitionTable` → 아이템 `GetData()` → `ArtifactDefinitionData` → `ArtifactDefinition` 경로는 효과 항목을 복사한다. 두 ModifyStat 항목을 SpawnSoul로 변환하지 않는다. 이 실제 테이블에서 가져온 값을 새 공개 계산기로 컴파일하면 첫 오류 `source.faction`에서 거절됨을 Unity 테스트로 확인했다.

| 필드 | 실제 두 항목 | Canonical Soul Altar |
|---|---|---|
| entryId | `soul_drop_attack`, `soul_drop_health` | 하나의 영혼 생성 효과를 표현하도록 DB 담당자 저작 필요 |
| source.faction | Any (0) | Enemy (2) |
| target.type / faction | AllMatchingUnits (0) / Enemy (2) | Collector (5) / Ally (1) |
| effect.type / statId | ModifyStat (5) / AttackPower 또는 MaxHealth | SpawnSoul (7) / 비어 있음 |
| effect.soulGrowth | 미저작, 기본 0/0/0 | attackBonus 1 / maxHealthBonus 1 / 데이터로 정한 healAmount ≥ 0 |
| value.amount / durationTurns | 1 / 0 | 0 / 5 |

UnitDied(11), IsNight(2), DeathPosition(2)는 현재 스키마와 일치한다. SO 수정은 수행하지 않았다. 두 능력치 항목을 두 영혼으로 해석하거나 컴파일 검증을 완화하지 않는다. DB 담당자가 저작을 정리한 뒤 Task 8의 실제 데이터 바인딩에서 거절 증거 테스트를 수락 테스트로 바꿔야 G9를 닫을 수 있다.

전체 DatabaseCatalog 로드는 별도로 `[CombatStageDetail] Combat stage requires Economy.`에서 실패한다. 이는 Task 0에 기록된 기존 실패다. 영혼 테이블만 정상 어댑터로 직접 로드해 G9 검증을 수행했으며, 전체 DB 초기화가 성공했다고 주장하지 않는다.

## 4. 검증

- 실제 Unity 6000.0.73f1 batch EditMode: 영향 범위 241개 중 240 통과, 기존 Catalog/Economy 실패 1개, skipped 0.
- 새 계산 16개, 실제 Buffer 반영 7개, 스키마 33개 모두 통과. 실제 SO 컴파일 거절 증거 1개 통과.
- 영혼·Last Stand·투영·넉백·가시·효과 소유자·준비·Flow 회귀를 포함한다. 전체 Core/Framework suite, PlayMode, 실게임 연결 검증은 수행하지 않았다.
- 증거: `.superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task5/review-green.xml`, 같은 이름 `.log`. 로그에 테스트 종료 코드 2가 기록됨: 위 기존 실패를 포함했기 때문이며 전체 성공 코드로 보고하지 않는다.
- 독립 리뷰에서 성장/소비 수 일치, 이동 후 사망 위치, 큰 공격력의 중간 overflow를 보완하고 회귀로 확인했다. 수정 후 재리뷰에서 남은 지적 없음. 리뷰어는 코드를 확인했으며 테스트를 별도로 실행하지 않았다.

다음 계획 작업은 Task 6이다. G9 실제 SO와 Task 7 처리 API/결과 게시, Task 8 View 호출 연결은 완료되지 않았다.
