# 아이템 시스템 조립 정리: DB 등록 유지와 Tool.Editor 의존성 복구 공유

날짜: 2026-10-17 · 작성: Core(천명) · 상태: **작업 완료(2026-10-17, 커밋 전), 미전달.** 실제 변경 목록과 검증 결과는 §3.

근거 설계: 아이템 조립 권한·책임 정리 (Git history: `2026-10-17-item-composition-authority-design.md`) §2(A9·A10), §3.3, §7(Q3).

이 문서는 성격이 다른 두 가지를 나눠서 알린다. §1은 **그대로 유지하는 변경**이라 받는 쪽에서 할 일이 없다. §2는 **아이템 커밋이 추가한 의존성만 원상 복구**하는 예고다.

## 1. DB Factory 소유자께 (용근님·민형님) — 유지하는 변경

아이템 커밋 `21845e3e`가 DB 쪽에 새 InGame 테이블을 등록했습니다. 등록 방식은 DB 등록 계약(FW-012)과 같고, 새 테이블을 넣는 경로가 이것뿐이라 **그대로 유지**합니다. 소유 영역을 건드렸기 때문에 알려드립니다.

| 위치 | 추가 내용 |
|---|---|
| `Factory/DBFactory/Group/InGameDataBaseFactory.cs` | `new ConsumableItemTable(dataBaseGroup.GetDataBase<ConsumableItemDataTableSO>())` 생성과 `ExportServices` |
| `ServiceKeys/DataBaseGroupServiceKey.cs` | `InGameTableKey.ConsumableItem` |
| `DataBaseSystem/Queries/InGameDataBase/InGameDataBase.cs` | 좁은 조회 facet `IHasConsumableItemTable`(`ConsumableItems`)과 그 등록. 기존 `IHasArtifactBoardConfig`와 같은 방식이고 `IInGameQuery`는 바꾸지 않았음 |
| `DataBaseSO/InGameGroup/InGameGroup.asset` | `ConsumableItem/ConsumableItemDataTable.asset` 한 항목 |
| 새 타입·에셋 | `Data/InGame/Table/Item/`(`ConsumableItemDataSO`, `ConsumableItemDataTableSO`, `ConsumableItemData`, 효과 Authoring), `Queries/.../ConsumableItemTable.cs`, `DataBaseSO/InGameGroup/ConsumableItem/`(테이블 1 + 아이템 4) |

알아 두실 점:
- 실제 운영 조립(`DataBaseFactory.Create`)에서 이 테이블은 **필수**입니다. `InGameGroup.asset`에서 항목을 빼면 DB 생성이 실패합니다.
- 기존 `ItemMaster` 가져오기 테이블과 옛 에셋은 바꾸지 않았습니다. 런타임 아이템 정의의 원본은 새 수동 테이블 하나입니다.
- DB 쪽 규칙(P1~P7, 순수 Data 분리, `GetData()`가 매번 새 불변 Data 생성)을 따랐습니다. 지원하는 효과인지는 SO가 아니라 Core 규칙 등록부가 판단합니다(2026-10-17 후속 정리 F1).

## 2. Tool.Editor 소유자께 (민형님) — 의존성 원상 복구 예고

아이템 커밋 `21845e3e`가 아이템 Inspector 하나 때문에 Tool.Editor 어셈블리가 게임 Systems에 의존하도록 바꿨습니다. 이 의존성을 **원상 복구**합니다. 되돌리는 대상은 **아이템 커밋이 추가한 부분뿐**이고, 민형님을 포함한 다른 분들의 Tool.Editor 변경은 건드리지 않습니다.

| 위치 | 되돌리는 내용 |
|---|---|
| `Tool/Editor/ProjectL.Tool.Editor.asmdef` | `references`에서 `ProjectL.Systems`, `ProjectL.Core.Contracts` 두 줄 제거. URP 참조·`precompiledReferences` 등 나머지는 그대로 |
| `Systems/AssemblyInfo.cs` | `[assembly: InternalsVisibleTo("ProjectL.Tool.Editor")]` 제거. Systems 내부 타입이 도구 어셈블리에 열려 있던 상태를 되돌림 |
| `Tool/Editor/ConsumableItemDataSOEditor.cs` | `Systems/DataBaseSystem/Editor/ConsumableItemDataSOEditor.cs`로 이동(GUID 유지, `#if UNITY_EDITOR`). 기존 `CombatStageDataEditor`와 같은 위치·방식 |

알아 두실 점:
- 확인한 범위에서는 이 Inspector 말고 Tool.Editor 안에 Systems·Core 타입을 쓰는 파일이 없습니다. 복구 뒤 Tool.Editor의 참조는 아이템 커밋 이전과 같아집니다.
- 진행 중인 도구 작업이 이 두 참조에 기대고 있다면 알려 주세요. 그 경우에는 되돌리기 전에 맞추겠습니다.
- `ProjectL.Tool.Editor.Tests`는 Tool.Editor만 참조하므로 영향이 없을 것으로 봅니다(실행 결과는 §3).

## 3. 실제 변경 목록과 검증 결과

**§1 DB:** 바꾼 것 없음. 위 표의 등록을 그대로 유지한다.

**§2 Tool.Editor:** 예고한 세 가지만 바꿨다.

| 파일 | 실제 변경 |
|---|---|
| `Tool/Editor/ProjectL.Tool.Editor.asmdef` | `ProjectL.Systems`, `ProjectL.Core.Contracts` 두 줄 제거. 아이템 커밋 직전(`21845e3e^`)과 내용이 같다 |
| `Systems/AssemblyInfo.cs` | `InternalsVisibleTo("ProjectL.Tool.Editor")` 제거. 아이템 커밋 직전과 내용이 같다(`ProjectL.Tests.Framework` 공개는 그대로) |
| `ConsumableItemDataSOEditor.cs` | `Tool/Editor/` → `Systems/DataBaseSystem/Editor/`로 `git mv`(GUID `6733710d7cfa4ab0a7b2e7dae4fe838a` 유지). `#if UNITY_EDITOR`, 네임스페이스 `ProjectL.Systems.DataBaseSystem.Editor` |

- Tool.Editor의 다른 파일, `ProjectL.Tool.Editor.Tests`, Tool.Editor의 `AssemblyInfo.cs`는 바꾸지 않았다.
- 검증은 Unity 6000.0.73f1 배치 EditMode 전체로 했다.
  - Tool.Editor 시험 20/20, Core 3,104/3,104, 아이템 198/198 통과. 아이템 198건에는 Inspector 선택기 시험(그래픽 사용, 실제 실행)이 포함된다.
  - `DataBaseAssetTests`는 33/36이다. 실패 3건은 이전부터 있던 스테이지·렌더 에셋 시험이고, 아이템과는 무관하다.
  - Framework 실패 220건은 변경 전 HEAD 기준선과 같은 목록이고, 새 실패는 0건이다.
  - 컴파일 오류는 없고, 새 경고도 없다.
- 같은 작업에서 Core 쪽 재고 쓰기 경로도 정리했다(재고 지급 전용 Buffer 거래 `ItemInventoryTransactions`, `IBufferQuery`에서 재고 저장소 제거). DB·Tool.Editor 소유 영역과는 무관하다.
