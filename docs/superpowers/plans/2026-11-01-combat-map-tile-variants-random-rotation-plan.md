# 바닥 타일 프리팹 후보·Random 회전 Task 계획

- 날짜: 2026-11-01
- 설계: [variants-random-rotation-design](../specs/2026-11-01-combat-map-tile-variants-random-rotation-design.md)
- 상태: **R1·R2 구현 및 최종 자체 검토 완료.** 실제 전투/맵 툴 시각 인수는 아래 실행 기록에 별도로 남긴다.
- 진행 규칙: 2026-11-01 사용자 "작업 진행하고, Phase 다 마치고 나면 검토 진행해"에 따라 두 Phase를 이어서 마친 뒤 최종 검토했다. 이번 요청에서는 Phase 사이 중지를 생략했다.
- 높이 정책: 사용자 답변으로 Enum별 공유 확정. 구현 기준은 위 설계다.

## Phase 0 — 현재 경로 조사·설계·계획

- [x] TileSet·불변 외형·MapTiles 입력/데이터·StageMapTiles·높이 표의 책임 확인.
- [x] 맵 툴의 SerializedObject 읽기/쓰기, 미리보기·종류 정보·회전 UI/단축키·저장 경로 확인.
- [x] 사용자 높이 정책 확인, 설계/Phase 계획 작성, SSOT/SOLID/JMH/FW/독립성 점검.
- 설계 당시 리뷰 지점: R1 구현 전 사용자 진행 승인을 받았으며, 이후 두 Phase를 모두 구현했다.

## Phase R1 — 여러 프리팹 후보를 런타임·도구에 연결

- [x] R1.1 대상 TileSet 재검색, 기존 kind·prefab GUID/fileID·surfaceHeight 스냅샷. 사용자 변경/다른 작업과 겹치면 해당 변경을 보존할 수 있는 범위 확인.
- [x] R1.2 CombatMapTileSetSO.Entry의 후보 배열·불변 CombatMapTileVisual의 개수/인덱스 조회, 빈 배열/null 후보·중복 종류·높이 검사. 기존 단일 프리팹 계약을 읽는 호출자를 함께 갱신.
- [x] R1.3 Tool.Runtime에 후보 인덱스 선택 도우미+meta. 시드/좌표/종류별 결정 결과, 후보 하나는 0, 여러 후보는 등록 항목 중 선택. 전투 전역 난수/DB 검사와 분리.
- [x] R1.4 StageMapTiles에서 생성 시드 한 번·칸별 후보 선택·생성. 고정 회전/부모/위치/해제 수명은 유지. 높이 표는 종류 공유 높이를 그대로 읽음.
- [x] R1.5 CombatStageSoAssets의 후보 배열 읽기, CombatStageSoTileKind/Scene의 후보 모델·Signature, Preview의 칸별 후보 소비, Window의 미리보기 시드/다시 뽑기·후보 정보 연결. 모든 보기에서 같은 선택을 사용하고 다시 그리기마다 추첨하지 않음.
- [x] R1.6 확인한 TileSet 에셋의 prefab 하나를 후보 한 항목으로 이전. kind/참조/높이·GUID를 보존하고 이전 후 비교. 사용자 TileKind Enum과 ViewStageDataSO/프리팹 배치는 보존.
- [x] R1.7 Tool.Runtime/Systems/Tool.Editor 컴파일·필요한 수치 확인, 기존 후보 하나와 여러 후보의 생성/미리보기 수명 대조. 에셋 변경 시 DataBaseAssetTests 실행 가능성/결과를 기록. Unity 확인과 구분해 보고 후 멈춤.

종료 상태: 여러 후보 등록/선택/미리보기까지 동작. 회전 옵션은 아직 기존 네 각이며, Random은 R2에서 도입한다.

## Phase R2 — Random 회전과 전체 편집 흐름

- [x] R2.1 CombatMapTileRotation : byte +meta, 기존 0~3/Random=4 명시. 바닥 타일 입력/배치 타입·길이/범위 검사·툴팁 변경. Core Rotation4와 다른 회전 배열은 수정하지 않음.
- [x] R2.2 공용 선택 도우미에 Random 각 선택. 정사각은 네 각, 직사각은 두 각. 후보 추첨과 분리된 경로 사용. 런타임/미리보기에서 고정 옵션 또는 실제 각을 생성 시 확정.
- [x] R2.3 작업본 SetRotation/HasRotations/Resize, SO 읽기·쓰기·검사, 초안/저장/재열기의 값 4 보존 대조. 기존 byte 회전 배열의 재해석/직렬화 값 확인.
- [x] R2.4 '타일' 보기·2D/3D 회전 도구에 Random 지정. 단일/복수·전체 선택·Undo/Redo 지원. 각·앞쪽 띠·칸 정보에 Random/미리보기 결과 구분. R/Shift+R·90° 버튼은 Random 칸 보존, 고정 각 지정은 Random 해제.
- [x] R2.5 다시 뽑기로 후보/Random 각의 미리보기만 갱신, SO/작업본/Undo 불변 확인. Repaint·카메라·보기 전환에는 결과 유지.
- [x] R2.6 컴파일/필요한 수치 검증과 Unity 인수 결과 기록. 소수 크기·배경 회전/반전·종류별 공유 높이·지형 교체/복구·기존 고정 회전·씬 재진입/해제 회귀. 맵 툴 사용 안내·관련 공유 문서 갱신, 보고 후 멈춤.

종료 상태: 요청한 네 항목이 연결된다. 미실행 Unity 확인은 완료로 표시하지 않고 남은 인수 항목으로 보고한다.

## 변경 예상 위치

- 새: Tool/Runtime의 선택 계산 도우미(+meta), Systems/DataBaseSystem/Data/View/Table/StageViewProp/Item/CombatMapTileRotation.cs(+meta).
- 데이터/생성: CombatMapTileSetSO.cs, CombatMapTileVisual.cs, CombatMapTilesData.cs, StageMapTiles.cs.
- 도구: CombatStageSoAssets.cs, CombatStageSoTiles.cs, CombatStageSoPreview.cs, CombatLayoutWindow.SoTiles.cs 및 시드/다시 뽑기 연결에 필요한 Window partial의 최소 줄.
- 에셋: 조사/비교가 끝난 TileSet만 단일 참조→후보 한 항목 이전. 새 후보 내용은 임의 작성하지 않음.
- 문서: 이 설계/계획, 맵 툴 사용 안내/사본 계약 공유 메모. Factory·Group·asmdef·Core·FieldCamera·Architecture 변경은 계획에 없음.

## 실행 기록

- 2026-11-01: 설계/계획만 작성. 사용자 수정 중인 CombatMapTileKind.cs의 0 기반 새 항목과 기타 에셋/프리팹 변경을 확인했으며 수정하지 않았다. 이전 소수 크기 작업은 현재 소스에 존재한다.
- 빌드·테스트·Unity 재생은 이번 설계 단계에서 실행하지 않았다. Git 스테이징/커밋/push 없음.

### R1·R2 구현 및 검증 (2026-11-01)

- R1: TileSet `prefabs[]`, 후보를 복제/보관하는 CombatMapTileVisual, 표시 시드·좌표별 공통 후보 선택, StageMapTiles 생성, 에디터 후보 읽기/표시·서명·Preview를 연결했다. TileSet 한 개의 기존 4줄을 후보 한 개씩으로 이전했다. 종류 숫자·GUID/fileID·높이 보존, 기존 사용자 수정 파일 8개 해시 보존 확인.
- R2: View 전용 CombatMapTileRotation : byte(0~3 유지, Random=4), 입력 검사/런타임 각 확정, 작업본 값 4/Resize/저장, Random 지정·R/Shift+R 보존, 일관된 예시 각/후보, 미리보기 시드와 다시 뽑기를 연결했다. Core Rotation4·다른 회전 목록·높이 표·Factory/서비스 수명은 수정하지 않았다.
- R1 최초 Tool.Runtime/Systems/Tool.Editor MSBuild 통과. 로그: Temp/TileVariants/r1-*-build.log. R2 최초 세 어셈블리 빌드도 통과(r2-*-build.log). 새 파일은 임시 include.targets로 Compile만 보충했고 저장소 csproj/asmdef를 수정하지 않았다.
- 검토에서 선택 함수의 반복 System.Random 할당과 작은 타일 칸의 Random 글자 잘림을 보완했다. 선택은 정수 계산/거절 구간으로 무할당 처리하고, 작은 칸은 ? 표식·범례·툴팁과 칸 정보로 구분한다. SO 모드 사용법·사본 계약 공유 문서도 갱신했다(공유 미전달).
- 최종 Runtime 빌드 성공 후, 다른 작업의 최신 도발 Core 소스와 이전 캐시 DLL이 달라 Systems 빌드에서 Taunted 타입 오류가 났다. Core 소스는 수정하지 않고 Contracts→Domain→Application DLL을 다시 만든 뒤 Systems/Tool.Editor를 재빌드하여 **모두 종료 코드 0**을 확인했다. 로그: final-runtime-build.log, final-ProjectL.Core.*-build.log, final-ProjectL.Systems-build.log, final-ProjectL.Tool.Editor-build.log. 기존 UI CS0649/Core CS8632 외 이번 변경 파일 오류/경고 없음.
- 실제 최종 DLL을 Unity 번들 Mono에서 호출: 후보 범위/재현성/단일 후보/잘못된 수 **64,262개**, Random 허용 각/선택 독립성/byte Enum/입력 검사/작업본·Resize/회전 단축키 **43,426개**, 총 **107,688개 수치 확인 통과**. 선택 함수 20,000회 호출에서 관리 힙 할당 0바이트도 확인했다. 스크래치 harness/log는 Temp/TileVariants/r1-check.*, r2-check.*, final-r1-check.log, final-r2-check.log. Native Transform/실제 Undo/파일 재열기를 실행한 검증은 아니다.
- computer-use 스킬로 열린 Unity를 새로고침하고 Console 오류 0을 확인했다. EditMode **DataBaseAssetTests 9/9 통과**(결과 XML 사본: Temp/TileVariants/Unity-DataBaseAssetTests.xml). 현재 이 이름의 9개 시험은 기존 Core 저작/특성 시험이라 새 타일 생성 통합 시험과 구분한다. 테스트를 위해 연 Test Runner 창은 닫았다.
- [최종 검토](../specs/2026-11-01-combat-map-tile-variants-random-rotation-review.md)에서 요구/설계·SSOT·SOLID 5원칙·JMH/FW·기능 독립성, 생성/해제, 에셋 이전과 사용자 변경 보존을 대조했다. 보완 뒤 추가로 확정한 코드 결함 없음.
- 남은 시각 인수: 실제 전투의 다중 후보/Random 생성과 배경 회전·반전, 맵 툴의 실제 파일 저장/재열기·Undo/Redo·다시 뽑기 표시, 지형 교체/복구. 현재 TileSet은 기존 후보 하나씩을 유지하므로 새 후보 등록은 사용자가 Inspector에서 한다. 기존 배치/회전을 일괄 Random으로 바꾸지 않았다. 스테이징·커밋·push 없음.
