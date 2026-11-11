# 전투 배경 공통 배율 CS0 파일 기준 — 2026-11-10

이 기록은 디스크에 저장된 작업 트리의 입력 기준이다. Unity 실행/미저장 메모리 상태와 메시 행렬 비교를 수행했다는 뜻은 아니다. 에셋 값 이전은 수행하지 않았다.

**후속 상태(2026-11-10):** 사용자가 Unity 확인에서 문제없음을 보고해 구형 `tileSize` 직렬화 필드와 일회성 이전 검사 도구를 정리했다. 아래 값/해시는 **정리 전 기준 자료**이며 현재 에셋 해시와 일치해야 하는 검사가 아니다. 아래 도구 사용 안내도 당시 이력이다. 현재 상태는 [공통 배율 계획의 최신 정리 기록](../plans/2026-11-10-combat-map-common-scale-plan.md)을 따른다.

## 입력과 보존 근거

- ViewStage 21개(등록 20, 미등록 1) 모두 PrefabWithTiles, TileSize=(2,2), ScaleOffset=(0.75,0.75,0.75).
- Test 1의 Map3GrayBoxP 루트 배율은 사용자 수정으로 XYZ=1이다. 나머지 20개의 StageTest 원본 루트도 1이다. 원본/자식/타일 외형은 이번 작업에서 수정하지 않았다.
- 등록 StageFieldView_Default: GridOutline, OutlineCellSpacing=(1.5,1.5), FieldUnitScale=1, GhostScale=1.
- 21개 모두 CombatMapTileSet_Temp를 공유한다. 종류 0·1·2·3·5·6·7의 SurfaceHeight=0, 종류 4는 -0.1이다. 종류 7은 외형 4개 중 선택하며 나머지는 각 1개다. 이 값과 선택 정책은 바꾸지 않았다.
- 기존 배경 배율 1×0.75와 새 배율 1.5÷2가 같다. TileSize, 위치, 회전, 기준 타일, SurfaceHeight, 외형/회전 선택을 보존하면 새 배치식은 기존 축 보정+기준 타일 정렬의 같은 강체 변환이다. 부동소수점 차이와 Unity 메시 경계의 실제 비교는 아래 검사 도구에서 확인해야 한다.
- 프리팹/타일 세트가 여러 스테이지에서 공유돼도 해당 에셋에 쓰지 않으므로 공유 원본 변경은 0건이다. 구형 필드 삭제는 Unity 재열기/저장 확인 뒤로 남긴다.

## 저장된 Authoring 씬 기준

- LoadedViewStage=Test 1. 작업 Position=(0,-0.4,0), Rotation=(0,180,0), Scale=(0.75,0.75,0.75), TileOffset≈(1,-1.5,-2), Anchor=(1,1).
- SO의 TileOffset=(1,-1.5,-1.96)과 씬 작업 값이 다르다. 두 상태를 합치거나 저장하지 않았다.
- StageBackdropPreview의 위치≈(0.75,0,-1.5000002), 회전≈0°, 배율1. 배경 인스턴스 로컬 Position=(0,-0.4,0), Rotation=180° Y, Scale=0.75.
- 형제 MapTileRoot 로컬 위치≈(-0.7499997,-1.525,1.5000004), Rotation=180° Y, Scale=0.75. 컨테이너 적용 후 타일 중심≈(0,-1.525,0).
- SO로 재생성하면 배경 Z≈-1.47, 작업 값으로 재적용하면 Z≈-1.5다. 이는 기존부터 있는 0.03 차이이며 새 배율 전환으로 통합하지 않는다.
- 씬 카메라/프랍 override/유닛 프리뷰를 수정하지 않았다. 열린 씬의 미저장 변경 여부는 이번 파일 조사만으로 확정할 수 없다.

## ViewStage 파일 기준

| 에셋 | StageKey | 등록 | 타일 수 | TileOffset | SHA-256 |
|---|---:|---|---|---|---|
| StageViewProp_Chapter2_Boss.asset | 2299 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | BEF32DAFBA9F596513EF888F61B0808B127C50B229ED8A40A10CB9E5F89C5A48 |
| StageViewProp_Chapter2_Regular.asset | 2201 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 24571E858C47DFD64A007594FCCE3D579233BA462DEBB044FF75803040B4C5F0 |
| StageViewProp_LD_P20.asset | 1120 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 952B7EF3DE719BA2976106A257CC2999534A83F768FF1F1CADFF01819FE7959C |
| StageViewProp_LD_P21.asset | 1121 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | DFE93E0BCF5E75925E35712B7877CE6DE7119E77AF37274D79C67FA9A43AECBA |
| StageViewProp_LD_P22.asset | 1122 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 6C95A5B53F32092FF303ADEA033CA4BEFB0445559ED01A2DF1DF2B68DFFAB18C |
| StageViewProp_LD_P23.asset | 1123 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 36375F846FFBFCC72D33C00855FFF05DC1B4DBEAB956EF139DF0B9A25640E138 |
| StageViewProp_LD_P24.asset | 1124 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 5FEF608F0B7FF4E1F832E3A3E7C8BBAEBD68CFD6FE67111F5337F3CE240069C6 |
| StageViewProp_LD_P25.asset | 1125 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 16AF47D32F7B5CDD98D28FE008A75EDF25F6A0DEA4BB63AC6C4180158412E275 |
| StageViewProp_LD_P26.asset | 1126 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 0CF42DF44C6D5B6620250D9479144963E39AFA2F84B334D7EC8CD6072F5078EC |
| StageViewProp_LD_P27.asset | 1127 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | B1C69AB283568ED8D811899EB476A694D2B01FA9A47D3D49B34C78C6F46BFF68 |
| StageViewProp_LD_P28.asset | 1128 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | E18DE76D8FF9B75CD340565DF856D83172DC52C1E9A758643B58A42FCBD50B49 |
| StageViewProp_LD_P29.asset | 1129 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | A3DB83C011CD33C6A985396013D9466DC1EFF81A24891BA9C908F917DB1E5A53 |
| StageViewProp_LD_P30.asset | 1130 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 9C9C9604502620CC82099E510F51CE40BEA4B3F8916B630D01FF64CBBC9BA7D3 |
| StageViewProp_LD_P31.asset | 1131 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 79A5685BE844717BD0A3A996D5E0A0AB24FDA1F1A650EBB3325E1E5E8DF9D9E8 |
| StageViewProp_LD_P32.asset | 1132 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | C4E62371CE4BCB1BA4E010A188029BD002B7C0F3D65FA121B9F1FF27F0D2839C |
| StageViewProp_LD_P33.asset | 1133 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 795BAEAF4B42DEB3CC1A2249988C21ED202D75EF4796D062B660D60135EEDDBA |
| StageViewProp_LD_P34.asset | 1134 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | AA66311F79F6AA3186B5C42A1C074C256E24C1B51F4782D6E4F3C2E3240F4390 |
| StageViewProp_LD_P35.asset | 2135 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 84127F6253ED89937ADE6CAC34ECBEA483368F721414FD647C58D5EE1877275D |
| StageViewProp_Test2.asset | 10001 | 미등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 746C3F5A46DDD14E42ADAAF6D65BB1A4FC8499AD6FCE848A78B25BCA230CB7CB |
| Test 1.asset | 10002 | 등록 | 17 × 17 | {x: 1, y: -1.5, z: -1.96} | 9F59784CA97E53082ABF24B5BB46F2CCF2CCA6FD69EA6272265D78A6C4504A97 |
| Test.asset | 10000 | 등록 | 17 × 17 | {x: -1, y: -2, z: 0.5} | 9F97F0420B1C6349685417C7FA265D7B292F5B6A29D7E200A79F42EC83B78BA1 |

## 관련 입력 파일 해시

- `Assets/ProjectL/DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset`: `AC7E1C63D33719A100D5520B57A5828CDE4F26F9F1FBCEB8B4322753AB7E15ED`
- `Assets/ProjectL/Prefabs/Stage/MapProp/Test/Stages/StageTest.prefab`: `CF3D147AD577F0B45636F76FB3971E343C3BBC4DB70B7573C4A9A47EE315318A`
- `Assets/Scenes/JMHTest/GrayBoxingProp/Map3GrayBoxP.prefab`: `22C1882F336074E702DB990195A6B3A7F8EF259179DF600529DB007C010B90D2`
- `Assets/Scenes/Dev/CombatMapAuthoring.unity`: `1A98DEB0C592F72135796FAED012631D3E45DD9DC06FFE9996B89714141B10A7`

## Unity에서 추가로 남길 증거

Project에서 대상 ViewStage들을 선택 → `ProjectL > 전투 배경 공통 배율 이전 확인` → `현재 선택한 ViewStage 검사` → 보고서 복사. 등록 여부와 무관하게 선택 항목을 검사하되, 보드 계산에는 해당 StageKey의 운영 전투 Root가 필요하다. Root가 없는 항목은 수치 검증 미완료로 보고한다.

보고서는 의존 에셋 해시, 구형/새 배율·배경/타일 행렬, 메시 경계·타일 모서리 오차, 칸 대응, SurfaceHeight, 열린 Authoring 씬의 작업 값/실제 표시 차이를 포함한다. 저장 버튼·루트 초기화·자동 fallback은 제공하지 않는다. 호환 입력은 값 이전이 필요 없고, 불일치 입력은 별도 결정이 필요하다.

현재 이 도구의 Unity 실행·컴파일은 미확인이다. 이전/이후 캡처, 다른 회전/기준 타일/Undo/재열기, 그리드 간격 변경 확인은 계획 CS4의 사용자 확인 항목으로 남긴다.
