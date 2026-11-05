# 임시 공격 연출 원본 에셋·설정 조사

- 작성: View 작업, 2026-10-13
- 범위: [임시 공격 연출 계획](../plans/2026-10-13-temporary-attack-presentation-plan.md)의 C1.2와 C1.3 원본값 조사
- 설계: [임시 공격 연출 4종 이식 설계](../specs/2026-10-13-temporary-attack-presentation-design.md)
- 과거 원본: 로컬 `develop-4week`, 고정 커밋 **`ae07af37e9e897e4462c102b67d6b05f7a0bb8db`**
- 상태: 읽기 전용 조사 완료. 아래 옛 에셋·스크립트는 아직 복원하지 않았다. 실제 모델·프리팹 연결은 계획 C2 이후 해당 Phase에서 진행한다.

이 문서의 옛 경로는 고정 커밋에서 조회하는 경로다. 현재 파일 위치와 구분한다. 필요한 발사체·가시밭 외형과 설정값만 이식하며 기존 유닛 프리팹 전체, 원본 PACK, 옛 Missing Script SO 48개를 덮어쓰거나 삭제하지 않는다. 이번 조사는 다른 작업자 파일과 기존 설계·계획을 수정하지 않았다.

## 1 GUID 대조 결과

2026-10-13 조사 시 현재 `Assets/**/*.meta` 전체에서 중복 GUID가 발견되지 않았다. 아래 발사체·가시밭 프리팹 5개, 공격 SO 9개, 옛 공격 스크립트 GUID는 현재 에셋에 없다. 실제 복원 전에는 당시 작업 트리에서 다시 대조한다.

기존 유닛 프리팹과 Ballista PACK 모델·머터리얼·텍스처는 옛 GUID를 유지한다. 이미 존재하는 GUID의 에셋을 새 경로로 복제하지 않는다. 에셋을 옮길 때는 `.meta`를 함께 다루고 GUID를 유지한다. 새 구조에서 과거 타입을 옮기는 경우 필요한 `MovedFrom`을 적용한다.

### 1.1 발사체·잔류 프리팹

| 옛 경로 | GUID | 현재·의존성 |
|---|---|---|
| `Assets/Prefabs/Bullet.prefab` | `6f0973fcb5364871b71c1a17dd9b4dd9` | 현재 없음. 기본 Unity mesh, `Bullet.mat`, 옛 `Bullet.cs` |
| `Assets/Prefabs/CactusBomb.prefab` | `46ae341f6d8a0424bb84e60a3cb79ed3` | 현재 없음. `CactusBomb.fbx`, 옛 `Bullet.cs` |
| `Assets/Prefabs/ThornCactus.prefab` | `09fc9d774997b0349b84c5298e5ceca1` | 현재 없음. 옛 `서브_프랍_꼬마선인장.fbx` nested prefab, 스크립트 없음 |
| `Assets/Prefabs/BallistaBolt.prefab` | `e93bf59f3871b924380214b0a086eeb1` | 현재 없음. 기존 Ballista PACK 모델·PolygonFantasyKingdom 머터리얼, 옛 `Bullet.cs` |
| `Assets/Prefabs/MortarShell.prefab` | `829a71a18cc45c64297c2736b113a661` | 현재 없음. 기본 Unity mesh, `MortarShell.mat`, 옛 `Bullet.cs`. 원본 MortarPresentation은 이 에셋 대신 CactusBomb을 사용 |

원본 프리팹에서 확인한 표시 값:

- Bullet 루트 스케일 `(0.12, 0.12, 0.4)`, 기본 mesh fileID `10207`.
- CactusBomb 루트 스케일 `(0.75, 0.75, 0.75)`. FBX 안의 mesh와 embedded material을 사용한다.
- ThornCactus는 루트 아래 FBX 인스턴스의 스케일을 `(40, 40, 40)`으로 덮어쓴다. 자식 회전·위치 override도 있으므로 원본의 모델 정렬을 함께 보존한다.
- BallistaBolt 루트 스케일 `(1, 1, 1)`, `BoltMesh` 스케일 `(0.28775615, 0.28775615, 0.28775615)`. 기존 PACK 모델 mesh fileID `4300010`을 사용한다.
- MortarShell의 `ShellModel` 스케일 `(0.18, 0.18, 0.18)`, 기본 mesh fileID `10207`. 고정 커밋의 SO·씬·프리팹에서 MortarShell prefab GUID 참조는 발견되지 않았다. 조사 목록에만 남기며 필수 복원 대상으로 삼지 않는다.

원본 네 발사체 프리팹의 `Bullet` 스크립트 GUID는 `63e4548bebc34c05867565c8e4911bda`다. 현재 이 스크립트는 없다. 옛 프리팹을 그대로 복원하는 Phase에서는 해당 컴포넌트의 이식·연결을 함께 처리하고 Missing Script 부재를 확인해야 한다.

### 1.2 없는 외형 의존성

| 옛 경로 | GUID | 내용 |
|---|---|---|
| `Assets/Materials/Bullet.mat` | `57f974494f6a435889f224efa2302097` | URP Unlit, BaseColor `(1, 0.72, 0.16, 1)` |
| `Assets/DataBase/SO/CactusCannonSO/MortarShell.mat` | `4f922c9364dd0324aac87aadace87f5a` | URP Lit, BaseColor `(0.14, 0.16, 0.12, 1)`. 미사용 MortarShell prefab의 의존성 |
| `Assets/Resources/FBX/CactusBomb/CactusBomb.fbx` | `d722d3bd35fa63d49a12f0b39a698c1a` | embedded material. 로컬 FBX bytes에서 외부 이미지 filename이 발견되지 않음 |
| `Assets/Scripts/Render/Shader/Background prop/서브_프랍_꼬마선인장.fbx` | `23073acca54663b4290905e5d9b21ace` | ThornCactus의 FBX. 상대 이미지 경로 `cactusT.png` 사용 |
| `Assets/Scripts/Render/Shader/Background prop/cactusT.png` | `533d649f22b45484cade79599d878356` | Thorn FBX 옆에 필요한 원본 텍스처 |

FBX의 이미지 의존성은 prefab YAML의 GUID 참조에만 나타나지 않는다. Thorn 원본 FBX bytes에는 `RelativeFilename: cactusT.png`가 있으므로 FBX만 복원하지 않고 상대 이미지도 함께 복원·검증한다. 원본 FBX importer는 `externalObjects: {}`, `materialImportMode: 2`, `materialLocation: 1`이다.

같은 옛 폴더의 아래 에셋은 존재를 조사했지만 Thorn prefab/FBX의 필수 의존성으로 확정하지 않았다. 이 조사만으로 복원 범위에 넣지 않는다.

| 옛 경로 | GUID | 연결 |
|---|---|---|
| `Assets/Scripts/Render/Shader/Background prop/cactusT2.png` | `66804854469b1a242b3af68994d05c94` | 아래 cactusT2 material이 사용 |
| `Assets/Scripts/Render/Shader/Background prop/Materials/cactusT2.mat` | `9ad7c8d83e1b0ae4f9ca4d2158faf18a` | URP Lit, cactusT2 texture 사용 |

현재 `Assets/ProjectL/Script/Render/Shader/Background scene test/서브_프랍_꼬마선인장.fbx`는 이름이 같아도 원본과 다르다. 현재 GUID는 `0ad8a2263bccd4249bcbf03f3b640c07`, 실제 bytes SHA256은 `236d7f1bef60a553cdafd3fd4eeea9af088f88526b5304dc4ca32e2efb18eaea`다. 원본 Thorn FBX의 SHA256과 일치하지 않는다. 기존 모델의 GUID를 바꾸거나 옛 Thorn prefab을 이 모델로 임의 remap하지 않는다.

### 1.3 기존 PACK 재사용

| 현재 경로 | 옛 경로 | 유지된 GUID |
|---|---|---|
| `Assets/ProjectL/Prefabs/PACK/Ballista/Models/SM_Wep_Ballista_Mobile_01.fbx` | `Assets/PACK/Ballista/Models/SM_Wep_Ballista_Mobile_01.fbx` | `3e74f5682b119c448b20bfb54a522578` |
| `Assets/ProjectL/Prefabs/PACK/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_01_A.mat` | `Assets/PACK/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_01_A.mat` | `71e41e43b459a244abaf5acca76b89ee` |
| `Assets/ProjectL/Prefabs/PACK/PolygonFantasyKingdom/Textures/PolygonFantasyKingdom_Texture_01_A.png` | `Assets/PACK/PolygonFantasyKingdom/Textures/PolygonFantasyKingdom_Texture_01_A.png` | `270fd5c64f7118c4495be93b7ce2530c` |
| `Assets/ProjectL/Prefabs/PACK/PolygonFantasyKingdom/Textures/Misc/PolygonFantasyKingdom_Texture_Emmisive.png` | `Assets/PACK/PolygonFantasyKingdom/Textures/Misc/PolygonFantasyKingdom_Texture_Emmisive.png` | `ddc131df32f11f54a98daf8633aee1a3` |
| `Assets/ProjectL/Prefabs/PACK/PolygonFantasyKingdom/Textures/Misc/PolygonFantasyKingdom_Texture_Normal_01.png` | `Assets/PACK/PolygonFantasyKingdom/Textures/Misc/PolygonFantasyKingdom_Texture_Normal_01.png` | `ee4ca7d229aa8f04cab686907c5f3396` |

이 에셋들은 복제·수정하지 않고 기존 GUID를 통해 재사용한다. 재귀 조사 중 Assets 밖 참조로 확인한 다음 GUID는 현재 URP PackageCache에 있다.

| GUID | 패키지 경로 |
|---|---|
| `650dd9526735d5b46b79224bc6e94025` | URP `Shaders/Unlit.shader` |
| `933532a4fcc9baf4fa0491de14d08ed7` | URP `Shaders/Lit.shader` |
| `d0353a89b1f911e48b9e16bdc9f2e058` | URP `Editor/AssetVersion.cs` |

따라서 해당 shader·AssetVersion 참조는 없는 에셋으로 판단하지 않는다. 실제 Unity import·material 색상 검증은 각 복원 Phase에서 별도로 수행한다.

## 2 공격 SO 9개와 값

다음 **9개**가 고정 커밋의 공격 표현 SO다. 모두 현재 같은 GUID의 에셋이 없다. 경로의 공통 접두사는 `Assets/DataBase/SO/`다. 새 공유 설정의 복원 기본값 출처이며, 새 구조에 옛 SO 전체를 자동 복사한다는 뜻은 아니다.

| 옛 상대 경로 | GUID | 공격 표현 값 |
|---|---|---|
| `BulletSO/BulletPresentation.asset` | `c3d7e368e452483d88f6a2e5b4aa0315` | Bullet, Speed `20`, Height `0.6` |
| `UnitMotionSO/UnitMotionSettings.asset` | `268132468dc8f2f4daf0863c2f97d703` | RecoilDistance `0.1`, RecoilAngle `7`, ShootDuration `0.42`, FireTime `0.3` |
| `CactusCannonSO/MortarPresentation.asset` | `4203ff7ddc5d13444970d78b7dcfd0ea` | CactusBomb, FlightDuration `0.8`, ArcHeightInCells `2`, SpinDegreesPerSecond `(60, 30, 15)` |
| `CactusCannonSO/ThornFieldPresentation.asset` | `5038199b9fa842c4c859a93ac0d51672` | ThornCactus |
| `CactusCannonSO/MortarWeaponSettings.asset` | `5a8f60ca0b9254b4ca3f28b3b18a5425` | BucketPitch `-20`, PrepareDuration `0.15`, ReturnDuration `0.25` |
| `CactusCannonSO/CactusCannonMotion.asset` | `4db1ed001a7dc8b4fbc923480e3f892d` | RecoilDistance `0.04`, RecoilAngle `2`, ShootDuration `0.42`, FireTime `0.3` |
| `BallistaSO/BallistaBulletPresentation.asset` | `75cba7c23978318439499eb55adf43e7` | BallistaBolt, Speed `12`, Height `0.6` |
| `BallistaSO/BallistaWeaponSettings.asset` | `7b8d8ada4594d3240bf34e35d8708020` | StringTravel `0.3`, StringDuration `0.22`, StringOscillations `2`, ReloadDuration `0.18` |
| `BallistaSO/BallistaMotionSettings.asset` | `80b30124368ddf5448f8a2e719457f1e` | RecoilDistance `0.04`, RecoilAngle `2`, ShootDuration `0.5`, FireTime `0.3` |

FireTime은 동작 내 발사 시점 비율이다. 옛 `ShootPrepared`는 준비 시간을 더하고 `(prepare + duration * fireFraction) / (prepare + duration)`으로 전체 동작의 발사 비율을 정한다. 공통·Cactus·Ballista Motion SO에는 피격 표현 값도 들어 있으나 이번 설계의 피격 흔들림 범위 밖이므로 이 조사에 따라 기능을 켜지 않는다.

옛 SO 타입들은 `Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/Unit/`에 있었다.

| 타입 | script GUID |
|---|---|
| `BulletPresentationSO` | `7a3171c83c504e599917e98b4628a891` |
| `UnitMotionSettingsSO` | `cdc3c6837284fd84e95d29a8f154c91f` |
| `MortarPresentationSO` | `d4b5ba7f6bd942a4881ca6cc2ac1c8e5` |
| `ThornFieldPresentationSO` | `8bba39e6d54baf24bbdbdb2ac3a69363` |
| `MortarWeaponSettingsSO` | `0ba344ebc9e53854e81c551c3843f239` |
| `BallistaWeaponSettingsSO` | `15d7301404283b64eab1c292c7f7325a` |

옛 Stage 씬의 `BulletPresenter`는 공통 BulletPresentation, UnitMotionSettings, ThornFieldPresentation을 연결했다. Ballista·CatusCannon의 전용 projectile·weapon·motion 설정은 각 유닛 prefab에서 연결했다. 현재 새 공유 설정은 이 책임 배치와 값을 출처로 삼되 옛 씬 조립을 복원하지 않는다.

## 3 유닛별 과거 표시와 override

원본 `Assets/Scripts/ProjectL/Unity/View/Animation/CombatAnimationPresenter.cs`의 120행은 `AttackRange >= 2`로 원거리/근접을 선택했다. 아래 사거리 값은 **과거 표시를 확인하는 조사 근거**이며, 현재 View가 InGame DB를 읽거나 사거리로 표현 종류를 판단하도록 하자는 계약이 아니다. 현재 종류 연결은 설계대로 View 프로필에 명시한다.

| 유닛 | 옛 Unit SO의 AttackRange | 과거 표시 |
|---|---:|---|
| Akimbo | 1 | BodyBash |
| MainCharacter | 1 | BodyBash |
| Demon | 1 | BodyBash |
| Gravekeeper | 1 | BodyBash |
| ShotGun | 2 | 일반 Bullet |
| Sniper | 3 | 일반 Bullet |
| CactusCannon | 3 | Mortar |
| Ballista | 99 | BallistaBolt |

값의 옛 경로는 `Assets/DataBase/SO/Stage/UnitTable/Player/Unit/{Akimbo,MainCharacter,ShotGun,Sniper,CactusCannon}.asset`과 `Assets/DataBase/SO/Stage/UnitTable/Enemy/Unit/{Demon,Gravekeeper,Ballista}.asset`이다.

**Akimbo와 MainCharacter의 과거 동작은 총알이 아니라 BodyBash다.** 해당 두 유닛을 총알 표시로 바꾸는 것은 과거 복원과 다른 결정이다. 이 문서는 변경을 결정하지 않으며 C3 연결 시 과거 사실과 새 요구를 구분한다.

원본 `UnitProceduralMotion`의 근접 값은 UnitMotionSettings SO가 아닌 serialized 필드 기본값이다.

| 필드·단계 | 값 |
|---|---:|
| `_attackDuration` | `0.48`초 |
| `_attackWindupAngle` | `22`도 |
| `_attackImpactAngle` | `55`도 |
| `_attackPullback` | `0.08` |
| `_attackLunge` | `0.25` |
| 준비 구간 | 정규화 진행 `0~0.3` |
| 타격 전진 구간 | `0.3~0.55` |
| 옛 Impact 통지 | 진행 `0.55` |
| 복귀 구간 | `0.55~1` |

원본 `UnitProceduralMotion.cs`와 `FieldUnitPresenter.cs`가 근거다. `FieldUnitPresenter.cs` 408행은 motion이 없는 유닛에 런타임으로 컴포넌트를 추가했다. Ballista와 CatusCannon만 prefab에 motion과 전용 SO override가 들어 있었다. 나머지는 위 근접 기본값을 사용했다.

| 옛 prefab | 전용 motion·weapon 연결 |
|---|---|
| `Assets/Prefabs/Stage/Units/StageUnit_CatusCannon.prefab` | CactusCannonMotion, MortarPresentation, MortarWeaponSettings. Bucket Transform fileID `8766096888193243087` |
| `Assets/Prefabs/Stage/Units/EnemyUnit_Ballista.prefab` | BallistaMotionSettings, BallistaBulletPresentation, BallistaWeaponSettings. Bolt fileID `7828128587098523157`, String fileID `7835483593138187437` |

위 fileID는 옛 prefab에서만 확인한 값이다. 현재 모델의 피벗 연결은 해당 Phase에서 현 prefab 계층과 대조한다. 과거 값만으로 현 prefab 전체를 덮어쓰지 않는다.

### 3.1 기존 유닛 prefab GUID 보존

옛 `Assets/Prefabs/Stage/Units/`의 아래 파일은 현재 `Assets/ProjectL/Prefabs/Stage/Units/`로 옮겨졌고 GUID는 유지됐다.

| 파일 | GUID |
|---|---|
| `Base.prefab` | `0c39bddbbcf195b4e8f4a1227d31a966` |
| `Enemy.prefab` | `c52e7ebe3799d2243bbb0a6fe0bcae71` |
| `EnemyUnit_Ballista.prefab` | `642518c57301d30469215547e3999e73` |
| `EnemyUnit_UnderTaker.prefab` | `035794848a9e7de4bb183cfb2dfc22a0` |
| `StageUnit_Akimbo.prefab` | `df3d7d5280e0477fb54cf819ce8b8854` |
| `StageUnit_CatusCannon.prefab` | `740814e4d9acd3f42a3515d16094e717` |
| `StageUnit_MainCharacter.prefab` | `997984e858b340aa83745d5c217bce25` |
| `StageUnit_ShotGun.prefab` | `8d8c2cd006ae494ab1ca6a71f6123052` |
| `StageUnit_Sniper.prefab` | `2bd556bb583a4d6db644d8cef352411f` |

옛 `Assets/DataBase/SO/Stage/UnitTable/Visual/UnitVisualCatalog.asset`의 MainCharacter 항목은 `Assets/Resources/Test/Model/Prefab/TestUnit_Base.prefab` GUID `3b5fb1cb70e1ffe45bea7cdf23a7c0ff`를 연결했다. 현재 `Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/Item/UnitVisual_MainCharacter.asset`은 Akimbo prefab GUID `df3d7d5280e0477fb54cf819ce8b8854`를 연결한다. 과거 Catalog 연결을 복구하지 않고 현재 모델·표시 연결을 보존한다.

## 4 로컬 LFS 실제 bytes

다음 LFS 실제 bytes가 로컬에 있다. 원본 고정 커밋의 pointer에 적힌 SHA256·크기와 local object의 실제 SHA256·크기를 대조해 일치함을 확인했다. 네트워크 호출은 하지 않았다.

| 에셋 | SHA256 | bytes |
|---|---|---:|
| CactusBomb FBX | `d6d309cd420a241bf8b68315294306b9c8ccd29f394d777e3227235d262ff390` | 102364 |
| Thorn FBX | `5ffa8e75bddaf8a2a3f4d8385bb23d23871ba7cc90b100226935fb1aa4629bc4` | 26972 |
| Thorn cactusT.png | `ce7ed9533e32362ad431565dc860b60f5e920f8a22703c5620727a36226f077d` | 2760 |

현재 로컬 object 위치는 프로젝트 기준 다음과 같다.

```text
.git/lfs/objects/d6/d3/d6d309cd420a241bf8b68315294306b9c8ccd29f394d777e3227235d262ff390
.git/lfs/objects/5f/fa/5ffa8e75bddaf8a2a3f4d8385bb23d23871ba7cc90b100226935fb1aa4629bc4
.git/lfs/objects/ce/7e/ce7ed9533e32362ad431565dc860b60f5e920f8a22703c5620727a36226f077d
```

`git show`로 위 binary 에셋을 조회하면 실제 FBX/PNG 대신 LFS pointer 텍스트가 나온다. 해당 Phase에서 복원할 때 검증한 local object bytes와 원본 `.meta`를 함께 사용한다. `.git`의 object를 변경하거나 네트워크에서 내려받는 작업은 필요하지 않다.

## 5 검증 범위와 다음 Phase

- 이번 확인: 고정 커밋의 `git show`·`git ls-tree`·`git grep`, 현재 `rg` GUID 대조, FBX local bytes의 이미지 filename 조사, LFS SHA256·크기 대조.
- 아직 하지 않은 것: 모델·프리팹·SO·스크립트 복원, Unity import·에셋 로딩·material 외형 확인, 컴파일·EditMode·실제 공격 재생.
- C1에서는 이 조사와 공통 설정·좌표·Tick·수명 기반만 만든다. 운영 공격 사건을 켜거나 실제 유닛 연결을 앞당기지 않는다.
- C2는 CatusCannon의 CactusBomb·무기 피벗과 ThornCactus 외형을 필요한 의존성만 복원·연결한다. 생성·제거 사실은 설계대로 실제 Buffer 목록을 따른다.
- C3는 유닛별 과거 BodyBash/Bullet 사실을 기준으로 표현 프로필을 확인한다. 과거와 다른 선택이 필요하면 구현 전에 사용자에게 묻는다.
- C4는 기존 Ballista PACK 외형을 재사용하고 BallistaBolt와 전용 바인딩만 복원한다. 관통 결과 사실의 인수는 별도 Task다.

SSOT 원본은 사건·Buffer의 전투 사실, 새 공유 View 설정의 표현 값, 현재 Mapper의 좌표, AttackPresenter의 단일 동작 수명이다. 이 inventory는 에셋·값의 역사적 출처이며 런타임 규칙이나 새 조립 경계를 추가하지 않는다.

## 6 피격·사망 모션 후속 조사 (2026-10-13)

사용자의 추가 요청으로 같은 고정 커밋 `ae07af37e9e897e4462c102b67d6b05f7a0bb8db`를 읽기 전용 조사했다. 추가 설계는 [설계 §9](../specs/2026-10-13-temporary-attack-presentation-design.md#9-피격사망-모션-추가-설계-2026-10-13), Task는 [계획 §7](../plans/2026-10-13-temporary-attack-presentation-plan.md#7-피격사망-모션-추가-계획-2026-10-13)이 원본이다. 이번 조사에서 런타임 코드·프리팹·SO는 변경하지 않았다.

| 과거 경로 | 확인한 사실 |
|---|---|
| `Assets/Scripts/ProjectL/Unity/View/Animation/UnitProceduralMotion.cs` | `Hit`는 sin 왕복 오프셋·기울기, `FallBackward`는 현재 방향 뒤로 -90도 SmoothStep. 절차적 Transform 모션이며 이 경로는 AnimationClip을 사용하지 않음 |
| `Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/Unit/UnitMotionSettingsSO.cs`와 `Assets/DataBase/SO/UnitMotionSO/UnitMotionSettings.asset` | HitDuration 0.18초, HitCellFraction 0.05, HitAngle 4도. 사망 시간은 이 SO에 없고 모션 컴포넌트에 있음 |
| `Assets/Prefabs/Stage/Units/EnemyUnit_Ballista.prefab`, `StageUnit_CatusCannon.prefab` | 모션 컴포넌트의 deathDuration 0.55초·deathRestDuration 0.2초. 모션 코드 기본값과 일치 |
| `Assets/Scripts/ProjectL/Unity/View/Animation/UnitHitPresenter.cs` | 살아 있는 피해 대상만 피격. 공격자 모델이 있으면 공격자→대상 방향, 없으면 대상 뒤쪽. 옛 FieldRoot의 셀 폭 사용 |
| `Assets/Scripts/ProjectL/Unity/View/Animation/CombatAnimationPresenter.cs` | 착탄 반응 뒤 체력 반영. 치명 피해는 피격 생략, 사망 소리·FallBackward와 Gate 대기. 옛 Relay/Gate를 현재 구조에 복원하지 않음 |
| `Assets/Scripts/ProjectL/Unity/View/Field/FieldUnitPresenter.cs` | 죽은 대여 모델을 유지하고 모션·Gate 이후 반환. 사망 유지 집합과 ReleaseDeadViews 사용 |
| `Assets/Tests/EditMode/StageComposition/AttackImpactTimingTests.cs` | 타격 이후 반응 순서·한 번 호출·취소 후 지연 사망 미호출을 검증. 현재 Ballista 접촉/Ordered 계약으로 검증 의도를 옮김 |

현재 코드 `CombatCommandEvents.Health`는 죽었으면 Died, 그 외 HealthChanged를 게시한다. `CombatCharacterEvent.Expected/Final`과 `DamageOrigins.PhysicalCell`은 이미 제공되므로 추가 Core 계약 없이 체력 감소·치명 피해·표시 방향을 연결할 수 있다. 여러 물리 출처의 방향 결정은 추가 설계의 표현 규칙이다.

현재 `CombatSceneEventPresenter.PresentEvent`는 Final 사실 적용 뒤 RenderEvents를 호출하며, `FieldUnitPresenter.RenderFacts`는 죽은 대상을 건너뛰고 EntityViewSet.End에서 반환한다. 따라서 옛 모션 코드만 붙이면 사망 모션을 재생할 모델이 남지 않는다. 새 설계에서는 사실 적용 전 같은 대여의 유지권을 확보하고 모션 완료 후 기존 풀 반환 경로로 해제한다.

옛 `UnitProceduralMotion.Update`·Cancel의 완료 콜백·공격/이동 큐·Vector3.up 고정 계산은 그대로 옮기지 않는다. 기존 외부 Tick, 취소와 성공 분리, Mapper 법선, 대여 버전·HUD 보호 계약으로 맞춘다. 원본 SO의 피격 값과 컴포넌트의 사망 값을 새 공유 View 반응 프로필의 기본값으로 모은다. 현재 유닛 프리팹·Akimbo 공격 프로필·원본 PACK·옛 Missing Script 에셋을 보존한다.
