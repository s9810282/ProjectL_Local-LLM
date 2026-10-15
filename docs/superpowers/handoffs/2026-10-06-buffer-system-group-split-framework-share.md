# BufferSystemGroup 분할 공유 (Framework 담당 민형님께)

작성 2026-10-06 · 브랜치 `feature/Cheon/refactor/core-system` (미커밋) · 상태: **미전달**

근거: `docs/superpowers/plans/2026-10-06-core-buffer-responsibility-cleanup-plan.md` B10 (결정 D3)

## 1 무엇이 바뀌었나

`BufferSystemGroup` 한 클래스가 하던 일이 둘로 나뉘었습니다.

| 이전 | 이후 |
|---|---|
| Group 역할(하위 모듈 InitData 전달) + `IBufferQuery` 서비스 위치 + 저장소 간 거래 11종 본문 | Group 역할 + `IBufferQuery` 노출만 남김 |
| 거래 본문이 모두 `BufferSystemGroup.cs` 안에 있음 | `BufferSystems/Core/`의 internal sealed 클래스로 분리 |

새 클래스(모두 `ProjectL.Systems.RuntimeSystem.BufferSystems.Core`, internal):

- `BufferGuards` — Run 진행 중, 활성 전투, 현재 선택 일치 검사(읽기만 함)
- `BattleCompletionTransaction` — 전투 완료 기록(노드·맵 완료, 영혼 필드, Run 종료 영수증) 준비·설치
- `BattleTransactions : IBattleBufferWriteSink` — 전투 준비·명령·영혼 행동·미리보기·완료·정리
- `ProgressTransactions : IRunProgressBufferWriteSink, IChapterContinuationBufferWriteSink, IChapterProgressBufferWriteSink` — Run 초기화, 챕터 진입·계속, 비전투 노드 완료
- `StageServiceTransactions : IShopStageBufferWriteSink, ICharacterPurchaseBufferWriteSink, IRecoveryStageBufferWriteSink` — 상점 진입·구매, 회복 진입·소비

Group 생성자가 하위 저장소(`*BufferKey.Store`)를 꺼내 각 거래 클래스에 **필요한 것만** 넘깁니다. Group은 저장소 필드를 더 이상 갖지 않습니다.

## 2 바뀌지 않은 것

- `ExportServices`: 여전히 `IBufferQuery`와 `ISystemModule` 두 개만 등록합니다.
- `InitData`: 하위 InitData 모듈 순회 그대로입니다.
- `IBufferQuery` 속성 이름·타입 전부 그대로입니다. 쓰기 Sink 속성이 `this` 대신 거래 객체를 돌려줄 뿐입니다.
- `BufferSystemFactory`(`new BufferSystemGroup(builder.BuildSystemReader())`) 수정 없음.
- 거래 본문 동작 없음(옮기기만 함). 상점·회복 진입의 같은 대상 노드 검사만 private `IsOpenTarget` 하나로 합쳤고, 챕터 진입·계속의 "등록 준비 → 위치 준비 → 함께 설치"를 private `InstallChapter` 하나로 합쳤습니다.

## 3 공개 계약 변경 1건

`PublishRecovery(StageNodeId, StageKey, CharacterRecoveryDecision)`를 `IBattleBufferWriteSink`에서 `IRecoveryStageBufferWriteSink`로 옮겼습니다. 회복은 전투 밖 비전투 스테이지 거래이고, 이미 전투 중이면 거부하는 메서드였기 때문입니다.

- 호출 경로: `buffers.BattleWriteSink.PublishRecovery(...)` → `buffers.RecoveryStageWriteSink.PublishRecovery(...)`
- 프로덕션 호출처는 `CombatProcessingService` 한 곳(Core 영역)이며 함께 고쳤습니다. 테스트 5개 파일도 같이 고쳤습니다.
- View 코드에는 호출처가 없습니다.

## 4 앞으로 거래를 더할 때(갓 클래스 방지)

- `BufferSystemGroup`에 거래 본문을 다시 넣지 않습니다. 클래스 주석에도 적어 두었습니다.
- 새 거래는 맞는 거래 클래스에 넣습니다. 한 클래스가 새 저장소 묶음이나 새 거래 계열을 요구하면 새 클래스로 나누고 Group 생성자에서 조립합니다.
- 공통 진입 조건은 `BufferGuards`에, 여러 거래가 함께 쓰는 준비·설치 단계는 `BattleCompletionTransaction`처럼 별도 클래스로 둡니다.

## 5 확인 부탁

1. Group이 거래 객체를 직접 `new`로 조립하는 방식이 Framework 조립 지침(FW 계약)과 맞는지. 거래 객체는 서비스로 등록하지 않고 `IBufferQuery` 뒤에만 둡니다.
2. `PublishRecovery` 이동에 반대 의견이 있는지.
