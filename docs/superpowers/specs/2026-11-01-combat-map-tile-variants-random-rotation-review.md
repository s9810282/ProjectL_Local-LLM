# 바닥 타일 후보·Random 회전 최종 검토

- 날짜: 2026-11-01
- 요청: R1·R2를 모두 구현한 뒤 검토한다. Phase 사이 중지는 이번 사용자 지시로 생략했다.
- 기준: [설계](2026-11-01-combat-map-tile-variants-random-rotation-design.md), [Task 계획/실행 기록](../plans/2026-11-01-combat-map-tile-variants-random-rotation-plan.md), View 경계 인계 §4.6.
- 범위: 이번 타일 변경의 자체 코드 검토·관리 코드 검증·에셋 이전 비교·Unity EditMode 실행. 아래 실제 전투/도구 시각 인수는 구분한다.

## 1. 요구·설계 대조

| 요구 | 확인한 최종 경로 |
|---|---|
| Enum별 여러 후보 | TileSet Entry의 prefabs[] → CombatMapTileVisual의 복제한 비공개 후보 배열. 종류 한 줄/공유 SurfaceHeight, 빈 후보·null 항목/중복 종류 검사 |
| 하나는 고정, 여러 개는 무작위 | StageMapTiles.Build가 생성 시드 하나와 칸 좌표/종류로 PrefabIndex를 구함. 후보 수 1은 0, 다중 후보는 범위 내 선택 |
| Rotations.Random | 바닥 타일 전용 byte Enum의 값 4. 기존 0~3/빈 배열 R0, 직사각의 고정 90·270 금지는 유지. 생성할 때만 실제 각으로 해석 |
| 같은 선택 규칙 | Tool.Runtime.MapTileRandomSelection을 Runtime/Editor가 함께 사용. 시드·좌표·종류·용도로 구분하며 후보/각 추첨은 서로 순서를 밀지 않음 |
| 맵 툴 | 세트 후보 배열 읽기·후보 개수/목록, 단일/복수 Random 지정, 앞쪽 띠/툴팁/칸 정보/3D 예시 각, 다시 뽑기와 시드 포함 미리보기 키 |
| 저장·Resize·회전 | int 배열의 4 유지, HasRotations는 4를 비어 있지 않은 값으로 판단, 기존 WriteArray(intValue) 사용. Resize는 겹친 칸 유지, Turned는 Random을 그대로 반환 |
| 높이·바닥 교체 | 후보별 높이를 만들지 않음. 종류의 공유 높이와 기존 칸 중심→타일 Transform 표/바닥 교체·복구 책임 유지 |

## 2. 검토에서 보완한 부분

1. **표시 중 선택 함수 할당:** 초기 구현은 고정 시드의 System.Random을 호출마다 만들었다. 동일 결과는 보장했지만 타일 보기의 반복 표시에서 불필요한 객체가 생겨, 정수 혼합·남는 구간 거절로 계산하는 무상태/무할당 함수로 바꿨다. 후보 수로 나눌 때의 편향을 거절 구간으로 처리한다. 새 전역/전투 난수 상태는 없다.
2. **작은 타일 칸의 Random 문구:** 28px 칸에 Random(각도) 전체를 넣으면 잘릴 수 있어 `?`+범례로 구분하고 툴팁/칸 정보에 전체 문구·예시 각을 표시했다. 고정 R0과 구분되며 검은 띠는 실제 예시 방향을 따른다.
3. **관련 안내의 옛 정수배 제한:** SO 모드 사용법의 타일 크기/맞춤 알림에 이전 제한이 남아 있어, 앞 단계의 소수 크기 지원과 현재 후보/Random 계약에 맞췄다.

## 3. SSOT·SOLID·JMH/FW·독립성

- 후보/높이 사실은 TileSet 한 곳, 회전 옵션은 ViewStageDataSO 한 곳, 선택 계산은 공용 도우미 한 곳이다. 런타임은 만든 Transform을 재사용하고 도구는 창의 시드를 사용한다. GetData/검사에서 추첨하지 않고 선택 결과를 SO에 역저장하지 않는다.
- SRP: 입력 검사/불변 외형 데이터/선택 계산/프리팹 생성/편집 표시를 분리했다. OCP: 후보 추가에 종류별 switch나 생성 분기 추가가 필요 없다. LSP: 새 상속 계약은 없으며 단일 후보+고정 각을 유지한다. ISP: 도우미에 좌표·시드·개수만 전달한다. DIP: Runtime/Editor는 이미 참조하는 하위 Tool.Runtime만 사용한다.
- 생성 주체는 기존 StageMapTiles/CombatStageSoPreview다. Factory 생성·Group 등록·Export·루트 Builder·서비스 조회 경로 변경이 없다. 기존 배경/표시의 씬 연결·해제 순서를 사용하므로 새 FW 서비스/Module/이벤트 수명 계약을 도입하지 않았다.
- 기능 변경 시나리오: 후보 추가는 TileSet 편집, Random 각 선택 정책은 도우미/입력 검사/해당 도구 표시 범위다. FieldGridMapper/높이 표/Core Rotation4/유닛 회전/카메라/전투 조립을 수정하지 않았다. 좌표계·해제 계약 자체를 바꾸는 경우에는 여전히 타일/지형 통합 확인이 필요하다.

## 4. 검증 증거와 한계

- 관리 코드 빌드·수치 검증의 최종 결과/로그는 계획 실행 기록을 따른다. 실제 컴파일된 공용 함수와 에디터 작업본·입력 검사를 호출해 후보 범위/재현성, 허용 각, byte Enum, 값 4·Resize·고정 각/Random 단축키를 확인한다. 이것은 실제 Unity Undo·파일 저장/재열기나 Transform 실행 증거는 아니다.
- 기존 사용자 수정 파일 8개(타일 종류 Enum 포함)의 SHA-256 보존을 확인했다. TileSet 기존 4줄의 종류 숫자·프리팹 GUID/fileID·공유 높이가 후보 한 항목으로 그대로 이전됐다. 사용자 ViewStageDataSO 배치/회전·프리팹과 새 NewTest 폴더에는 후보를 임의 배정하지 않았다.
- computer-use 스킬로 열린 Unity에서 새로고침·Console 확인 후 **EditMode DataBaseAssetTests 9/9 통과**를 확인했다. 결과 사본은 Temp/TileVariants/Unity-DataBaseAssetTests.xml. 현재 이 이름의 9개 시험은 기존 Core 저작/특성 시험이므로, 이를 새 타일 생성 통합 시험으로 표현하지 않는다. 테스트를 위해 새로 연 창은 닫았다.
- 아직 실제 전투의 다중 후보/Random 타일 생성, 배경 회전/반전이 있는 맵에서의 새 외형 표시, 맵 툴의 파일 저장/재열기·실제 Undo/Redo·미리보기 다시 뽑기의 시각 인수는 미실행이다. 기존 자료를 검사한 범위와 관리 코드 검증을 이 실행 증거로 확대하지 않는다.

## 5. 결론

요구한 R1·R2의 코드 연결과 구조 검토를 마쳤다. 위 보완 뒤 이번 변경에서 추가로 확정한 코드 결함은 없다. 실제 전투/맵 툴의 시각 인수는 §4의 남은 확인 항목이다. 스테이징·커밋·push는 하지 않는다.
