# Obstacle hit count migration

2026-11-16

The combat SO reads an explicit positive `requiredHits`. New definitions created from the Unity asset menu start with one hit. A serialized definition that omits `requiredHits` fails conversion. This keeps the new-asset default out of the old HP data path.

| Repository asset | GUID | Old maxHealth | Proposed requiredHits | Reference and approval |
| --- | --- | ---: | --- | --- |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Obstacle/Item/Obstacle_PowderBarrel.asset` | `586fba37fd424a45b8a52bebca8ba754` | 15 | Pending content-owner value | Authoring template. Its GUID occurs only in its own metadata in the repository asset search. |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Obstacle/Item/Obstacle_Supplies.asset` | `a607404e52cd415c9c336cd0c3a3b513` | 10 | Pending content-owner value | Authoring template. Its GUID occurs only in its own metadata in the repository asset search. |

`Assets/ProjectL/DataBaseSO/InGameGroup/ObstacleTable/Core/Table/ObstacleMasterDataTableSO.asset` (`760617ab4e4ff3c4eb6f991dcdb11e6f`) has an empty `databaseItems` array and four old HP rows. These rows use the separate `ObstacleMasterData` schema. They are outside the native obstacle definition migration. The repository search found no serialized native pillar definition.

When a template is selected for an authored stage, the content owner supplies explicit hit counts, including the supported pillar length where applicable. Apply the approved fields to the existing asset identity and run `DataBaseAssetTests` and an authored battle-entry check. Both templates currently retain their original bytes and GUIDs.
