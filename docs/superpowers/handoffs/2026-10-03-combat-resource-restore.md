# 전투 외형 리소스 복원

원본: `ae07af37` · 적용 브랜치: `feature/Yonggeun/Stage` · 2026-10-03

사용자 요청으로 옛 전투의 유닛 외형·아이콘을 현재 폴더 구조로 가져왔다. 원본/대상 경로·GUID·파일 해시는 [복원 명세](../plans/2026-10-03-combat-resource-restore-manifest.json), 실행·검증 상태는 [전투 View 계획 §7.13](../plans/2026-10-01-combat-view-plan.md#713-전투-외형-리소스-복원-2026-10-03)을 기준으로 한다.

## 복원 범위

| 대상 | 추가 에셋 수 | 현재 위치 |
|---|---:|---|
| 아이콘·카드·HUD 이미지 | 42 | `Assets/ProjectL/Sprites/` |
| 적·메인·선인장 캐논·본부 외형 프리팹 | 6 | `Assets/ProjectL/Prefabs/Stage/Units/` |
| AkimboTest·Enemy·EnemyHorse FBX, 재질 | 4 | `Assets/ProjectL/Resources/Test/Model/FBX/` |
| 발리스타·선인장 캐논 무기: FBX 2, 프리팹 2, 충돌 Mesh 에셋 2, 재질 1, 텍스처 3 | 10 | `Assets/ProjectL/PACK/` |
| **합계** | **62** | `.meta`와 폴더 메타데이터는 별도 |

기존 Akimbo·ShotGun·Sniper의 모델/프리팹 등 11개는 현재 경로의 동일 GUID 에셋을 재사용한다. PNG·FBX 50개는 Git LFS 포인터 대신 로컬 캐시의 실제 원본 바이트를 가져오며 원본 SHA-256·크기를 대조한다. 추가 에셋의 GUID를 유지해 재질·텍스처·프리팹 연결을 보존한다.

## 현재 외형 DB 연결

`ViewGroup/UnitVisual/UnitVisualDataTable.asset`에 현재 `EntityKey`의 유닛 8종과 본부 1종을 등록한다. Core 정의·로드아웃·전투 배치에는 손대지 않는다. 새 외형 항목의 등록만으로 새 유닛이 전투에 등장하지는 않는다.

| 키 | 항목 | 연결 기준 |
|---|---|---|
| 10 | Akimbo | 옛 카탈로그의 StageUnit_Akimbo 및 전용 아이콘 |
| 11·12 | ShotGun·Sniper | 기존 프리팹 유지, 옛 카탈로그의 전용 아이콘 |
| 13 | Demon | TestUnit_Base 임시 외형에서 옛 Enemy 프리팹·Miner 아이콘으로 복원 |
| 14 | MainCharacter | 옛 카탈로그도 TestUnit_Base를 사용하므로 외형 유지, Main 아이콘 연결 |
| 15 | Ballista | 새 외형 항목, EnemyUnit_Ballista·전용 아이콘 |
| 16 | CactusCannon | 새 외형 항목, StageUnit_CatusCannon·Character_CactusIcon |
| 17 | Gravekeeper | 새 외형 항목, EnemyUnit_UnderTaker·전용 아이콘 |
| 20 | Headquarters | 기본 Cube 임시 외형에서 옛 Base 프리팹으로 복원, 아이콘 없음 |

원본 카탈로그의 CactusCannon 아이콘은 Sniper로 연결돼 있었다. 이번에는 복원한 `AlphaIcon/Character_CactusIcon.png`를 정보·타임라인 두 칸에 사용한다. 전용 TimelineIcon 파일은 원본에 없다.

`StageUnit_MainCharacter.prefab`도 리소스로 복원했지만 자동으로 DB에 연결하지 않는다. 이는 Akimbo 기반의 별도 프리팹이며, 원본 카탈로그의 메인 캐릭터 연결은 TestUnit_Base다.

## 현재 구조에 맞춘 프리팹 정리

- 복원한 전투 프리팹 5개에서 옛 `HandUnitView`, `HandUnitSelectable`, `BallistaWeapon`, `MortarWeapon`, `UnitProceduralMotion` 컴포넌트 총 14개를 제거했다. GameObject의 컴포넌트 목록과 PrefabInstance의 추가 컴포넌트 참조도 함께 정리한다. 모델·Transform·Renderer·콜라이더와 해당 fileID는 유지한다.
- 기존 `Resources/Test/Model/Prefab/TestUnit_Base.prefab`에도 사라진 `HandUnitView`, `HandUnitSelectable`, `AnimationNotify`가 남아 있어 3개를 제거했다. 현재 Presenter가 현재 View 컴포넌트를 연결하는 경로를 사용한다.
- 무기 충돌 에셋 2개의 원본에는 없는 외부 편집 도구 스크립트가 붙어 있었다. 해당 컨테이너만 제거하고 Mesh 하위 에셋·GUID·fileID를 유지하며 첫 Mesh를 주 에셋으로 지정했다.
- URP Lit 셰이더·기본 재질·재질 버전 정보는 설치된 URP 패키지를 참조한다. Render 시스템이나 셰이더 코드는 변경하지 않는다.

공격/피격 모션, 발사체 동작, 옛 UI 프리팹·컨트롤러의 복원은 포함하지 않는다. 현재 상태 표시에서 사용할 외형 데이터와 재사용 가능한 이미지가 대상이다. 손패 카드·타임라인 UI가 해당 이미지를 표시하는 기능은 그 UI 구현에서 연결한다.

## 후속 확인 위치

Unity에서 `DataBaseAssetTests`와 계획 §7.13의 외형 확인을 실행한다. 새 유닛 3종은 외형만 등록했으므로 Prefab Mode에서도 확인할 수 있다. `Combat.unity`의 기존 씬 편집 및 FMOD 캐시 변경은 이번 작업 전 내용 그대로 보존했다.
