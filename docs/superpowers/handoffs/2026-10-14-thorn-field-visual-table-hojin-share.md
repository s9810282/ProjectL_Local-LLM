# View DB 표 추가 공유: 가시밭 칸 외형 `ThornFieldVisual` (용근 → 호진님)

- 작성일: 2026-10-14. 작성: 용근(View). 브랜치 `feature/Yonggeun/Stage`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련: [구조 리팩토링 설계](../specs/2026-10-14-combat-view-structure-refactor-design.md) D12, [리팩토링 계획](../plans/2026-10-14-combat-view-structure-refactor-plan.md) R4.3a·§6.10

## 1. 요약

전투의 가시밭 칸 3D 외형을 담는 View DB 표 `ThornFieldVisual`(설정 하나, 레일 `RailwayVisual`과 같은 형태)을 추가했습니다. 이 때문에 호진님 파일 3개에 **한 줄씩** 들어갔고, `ViewGroup.asset`에 표가 하나 등록됐습니다. 동작이 바뀌는 곳은 없습니다.

## 2. 호진님 파일에서 바뀐 것

`IViewQuery`에 `ThornFieldVisualTableQuery` 속성이 생겨, 이 인터페이스를 직접 구현한 테스트 가짜에 같은 모양의 한 줄을 더했습니다.

| 파일 | 더한 줄 |
|---|---|
| `Tests/EditMode/Framework/ArtifactPopupConfigTests.cs` | `public IThornFieldVisualTableQuery ThornFieldVisualTableQuery => null;` |
| `Tests/EditMode/Framework/LobbyInputTests.cs` | 같음 |
| `Tests/EditMode/Framework/LobbyViewSystemTests.cs` | `… ThornFieldVisualTableQuery => throw new NotSupportedException();` (기존 레일 줄과 같은 방식) |

같은 변경이 `UnityStageInputTests.cs`(용근)에도 들어갔습니다.

## 3. 병합 때 주의

- **`Assets/ProjectL/DataBaseSO/ViewGroup/ViewGroup.asset`:** `databaseTables` 목록 끝에 `- {fileID: 11400000, guid: 2ff468128b63412ea8759bc7205501f4, type: 2}` 한 줄이 늘었습니다. 호진님도 표를 추가하셨다면 같은 위치에서 충돌할 수 있습니다. **두 줄 모두 남기면 됩니다**(순서는 상관없음).
- **표 에셋이 없으면 DB 생성이 실패합니다**(`Database of type ThornFieldVisualDataTableSO not found`). 코드만 받고 `DataBaseSO/ViewGroup/ThornFieldVisual/` 폴더를 빠뜨리지 않게, 한 커밋으로 묶어 올립니다.
- 새 `IViewQuery` 가짜를 만드실 때는 `ThornFieldVisualTableQuery`도 구현해야 합니다.

## 4. 새 파일

- 코드:
  - `DataBaseSystem/Data/View/Table/ThornField/`: `ThornFieldVisualDataTableSO.cs`, `Item/ThornFieldVisualDataSO.cs`
  - `DataBaseSystem/Queries/ViewDataBase/ViewTable/ThornFieldVisualTable.cs`
- 에셋: `DataBaseSO/ViewGroup/ThornFieldVisual/ThornFieldVisualDataTable.asset`, `Item/ThornFieldVisual_Default.asset`(프리팹 `Prefabs/Combat/ThornCactus.prefab`)
- 등록: `IViewQuery`·`ViewDataBase`·`ViewDataBaseFactory`·`ViewTableKey.ThornFieldVisual`
