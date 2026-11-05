# 공격 범위 Outline 열기 계획

- 날짜: 2026-11-01
- 설계: [attack-outline-opening-design](../specs/2026-11-01-attack-outline-opening-design.md).
- 사용자에게 캐릭터 쪽 변 전체를 숨기는지 확인받았으며 AO1 한 Phase를 구현·검증한 뒤 자체 검토하고 보고한다.

## Phase AO1

- [x] 기존 닫힌 고리·둥근 모서리·EachCell·선택한 공격자 표시·깊이/높이별 분리 조사.
- [x] AO1.1 일반 렌더 DTO에 선택적인 열린 변을 추가하고 기존 메시의 해당 삼각형만 생략한다.
- [x] AO1.2 Field의 공격자 방향 판단 부품과 공격 관련 층 전달, 그리기 캐시를 연결한다.
- [x] AO1.3 관련 자동 검증·빌드·가능한 Unity 확인 후 SSOT/SOLID/FW/독립성 자체 검토와 결과 기록.

## 실행 기록

- 시작: 앞선 채움 그라데이션 수정과 사용자 작업이 작업 트리에 남아 있다. 기존 변경을 유지한다. 이번 작업은 채움 재질/셰이더·씬·SO를 변경하지 않는다.
- 구현: GridOutlineOpening(방향·격자 경계선, 기본값 없음)을 AreaOutlineData와 GridOutlineCreateData로 전달한다. 기존 닫힌 고리·둥근 모서리·UV를 계산한 뒤 해당 변/모서리의 삼각형만 생략한다. EachCell의 직선 묶음 경로도 같은 경계선만 생략하므로 내부 공유 선은 유지된다.
- 표시 연결: FieldAttackOutlineOpening이 기존 ActiveActor/대상 층 칸을 한 번 순회해 열린 변을 정한다. FieldOutlineRenderer는 깊이·높이로 나누기 전에 그 값을 받아 모든 부분에 전달하며, 마지막으로 그린 열린 변이 달라도 갱신한다. 실패한 Update가 그리기 캐시에 남지 않도록 Remember를 성공 뒤로 옮겼다.
- 검토 보완: 공격 영향 채움과 같은 색으로 합쳐진 호버가 같은 덩어리 외곽을 다시 그릴 수 있었다. FieldCellFillStack에서 기존 지도/깊이로 합쳐진 층을 파생해 읽고, 그 호버는 같은 층의 열린 변을 따른다. 별도 호버 표시는 기존 고리다. 생성자에서 enum 검사를 범위 비교로 하여 표시 갱신 때 검증용 박싱을 만들지 않는다.
- 관리 빌드: VS 2022 MSBuild의 ProjectL.Systems.csproj Debug, BuildProjectReferences=false 종료 코드 0. 로그는 Temp/AttackOutlineOpening/systems-build.log 및 systems-build-final.log. 기존 UI 직렬화 필드의 CS0649 경고가 있으며 변경 파일의 오류는 없었다. 최종 보완은 Unity 재컴파일 뒤 시험으로 확인했다.
- Unity EditMode: ProjectL.CombatField.Editor.Tests 어셈블리 14/14 통과, 실패/건너뜀 0. 첫 구현과 검토 보완 뒤 각각 실행했다. 최종 XML은 Temp/AttackOutlineOpening/editmode-results.xml이다. 네 방향·직사각형·비정방 칸·축소/둥근 모서리·EachCell/내부 변·구멍·높이 부분·기본 DTO 전달·공격자만 이동하는 경우/소스 없음/주변 범위를 검증했다. 실제 삼각형이 변의 점을 덮는지와 중앙을 새 선으로 가로지르지 않는지 검사한다. 새 시험 어셈블리에 한정한 InternalsVisibleTo를 추가했고 모든 새 에셋의 Unity 생성 meta를 유지했다.
- 변경 범위: 사용자 StageFieldView_Default.asset은 앞서 보관한 해시와 일치한다. CoreSystem·FieldCamera·Architecture·공용 셰이더·씬·DB 값은 수정하지 않았다. Test Runner가 검증 중 Combat 씬을 다시 로드했으며 시험 창은 닫았다.
- 자체 검토: SSOT(기존 강조 상태/지도만 사용), SOLID 5원칙(방향 판단/메시 생략/전달 책임 분리와 기본 계약 보존), JMH/FW(구체 렌더 DTO·기존 핸들 수명, 새 서비스/등록 없음), 기능 독립성(판단 부품/생략 부품과 공용 전달 지점만 변경)을 대조했다. 공용 메시 기본값과 EachCell 회귀를 관련 시험에 포함했다. 추가 코드 결함은 발견하지 못했다.

## 완료와 남은 확인

- AO1 구현·빌드·관련 자동 시험·자체 검토 완료.
- 실제 전투 Play 화면에서의 열린 변/호버 합침 시각 인수는 아직 하지 않았다. 자동 시험 14건은 메시·방향 판단 검증이며 실제 전투 화면의 검증을 대신하지 않는다.

## Phase AO2 — Outline 선과 채움 가장자리 동시 적용

- 사용자 정정: "둘 다 캐릭터 쪽을 없애줘". AO1에서 채움 그라데이션을 유지해 진한 띠가 남는 부분을 보완한다.
- [x] AO2.1 기존 열린 변 판단을 채움 재질에도 전달한다. 소스만 움직여도 재질만 갱신한다.
- [x] AO2.2 닫힌 모양/구멍 마스크와 열린 변을 제외한 투명도 거리 계산을 분리한다.
- [x] AO2.3 실제 채움 셰이더 GPU 시험과 AO1 회귀·빌드, 자체 검토 후 결과를 기록한다.

### AO2 실행·검토 결과

- 기존 Outline의 열린 변은 유지하고, 채움도 FieldAttackOutlineOpening의 같은 값을 재질 사본에 받는다. Plate는 마지막 재질 값을 기억하며 열린 변이 바뀌면 그 값만 갱신한다. 색/칸이 같은 경우 메시를 다시 만들지 않는 기존 경로보다 먼저 갱신한다.
- 셰이더는 원래 윤곽 거리로 모양을 마스킹한다. 전이 거리만 열린 경계선의 외부 이웃/연결 사분면을 가상으로 이어서 구한다. 실제 지도/높이/구멍·RGB·전체 알파·채움 모양은 그대로이고 열린 변의 진한 띠만 사라진다. 기본 _OpenEdge = 0이면 이전 그라데이션이다.
- 개발 중 HLSL 예약어 line을 변수명으로 쓴 컴파일 오류를 gridLine으로 수정했다. 이후 운영 셰이더 동기 컴파일/실제 GPU DrawMesh·RenderTexture 픽셀 검사가 통과했다.
- 관리 빌드: ProjectL.Systems Debug MSBuild 종료 코드 0, Temp/AttackOutlineOpening/ao2-systems-build.log. 이전과 같은 수정 밖 UI 필드 CS0649 경고가 있다.
- Unity EditMode: 관련 어셈블리 24/24 통과, 실패/건너뜀 0. AO1 메시·방향 회귀 14건 + 실제 채움 셰이더 GPU 시험 10건이다. GPU 시험은 네 방향 × Merged/EachCell, 다른 세 변 유지, 재질 초기화, 여러 칸의 열린 변 이음매, 위 판 구멍 마스크를 대조했다. 결과는 Temp/AttackOutlineOpening/ao2-editmode-results.xml이다.
- 자체 검토: 열린 변 판단 SSOT는 기존 부품 하나다. 채움의 전달/캐시는 FieldCellFillStack, 모양/투명도는 셰이더가 담당해 SOLID·기능 독립성을 유지한다. 새 서비스/등록·Factory·Core·수명 변경 없이 기존 렌더 DTO/재질 사본 경계를 쓴다(JMH/FW). 사용자 StageFieldView_Default.asset 해시는 기존 보관 값과 일치하며 원본 머티리얼 설정도 추가 변경하지 않았다.
- AO2 구현·빌드·GPU 포함 관련 시험·자체 검토 완료. 실제 전투 Play 화면 인수는 이 시험과 구분하며 아직 미확인이다.

## Phase AO3 — 사용자 화면의 중앙 경계·작은 사각형 수정

- [x] AO3.1 범위 원점 칸과 중앙 제거 영역을 전달해 선택/호버·이동/공격의 인접 경계를 함께 제거한다.
- [x] AO3.2 채움의 실제 노출 경계 거리로 전환한다. 사용자 재질 폭 0.4·중앙 0을 보존한다.
- [x] AO3.3 중앙 캐릭터/다른 호버 원점·넓은 폭/내부 칸·축소/둥근 모서리 GPU 및 기존 회귀·빌드·자체 검토를 기록한다.

### AO3 실행·검토 결과

- 원인 1: 원점이 범위 경계 상자 안에 있으면 닫힌 고리를 유지하던 AO1 정책과 유닛 호버 범위 미적용이 사용자 화면의 중앙 경계를 남겼다. 열린 변 DTO에 원점 칸의 ClearArea를 추가해 중앙 경계·자기 표지와 원점 호버를 함께 비운다. 이동/공격 범위의 외곽·다른 구멍은 유지한다. 다른 유닛을 호버할 때 현재 선택 캐릭터의 표지는 지우지 않는다.
- 원점 전달: 유닛 호버 응답이 원점 EntityId를 표시 계약에 넘기고 CombatSceneDisplay가 완료 표시 목록에서 점유 칸을 조회한다. 유닛 호버에 원점이 없으면 구성 원인/ID를 명시한 예외이며 환경 위험만 빈 원점이 정상이다. FieldHighlightState는 호버 범위의 원점 칸을 읽기 전용으로 보관하고 해제/Reset/Clear 때 비운다. 규칙이나 Buffer 쓰기는 추가하지 않았다.
- 원인 2: 합집합 구성 상자들의 min SDF를 그라데이션 거리로 사용하면 폭이 넓을 때 내부 조각 경계가 드러난다. 실제 모양 마스크는 기존 식을 유지하고, 전이만 실제 노출 변·볼록 원호·오목 모서리까지의 거리로 바꿨다. 이전 0.25 폭 시험/CPU 사각형 대조는 사용자 폭 0.4의 내부 사각형 부재를 보장하지 못했다. 폭 한도 0.45까지 같은 국소 SDF를 그대로 쓸 수 있다는 가정은 철회했다.
- 자동 검증: 관련 EditMode 31/31 통과, 실패·건너뜀 0. 기존 회귀 + 중앙 원점 Merged/EachCell 메시·다른 호버 원점/해제 + GPU 폭 0.25/0.4/0.45·축소/둥근 모서리 내부 표본·중앙 네 방향 띠 제거를 포함한다. 결과는 Temp/AttackOutlineOpening/ao3-editmode-results.xml. ProjectL.Systems Debug MSBuild 종료 코드 0(ao3-systems-build.log), 최종 Unity 재컴파일 후 시험도 통과했다.
- 실제 재생: 기존 에디터 개발 설정(Combat/PlacedStage Test)을 바꾸지 않고 재생했다. 필드 유닛의 호버 범위를 Game 뷰 확대 상태에서 확인해, 중앙 유닛 칸을 두르는 작은 테두리·내부 작은 사각형이 사라지고 외곽 범위 테두리가 유지되는 것을 육안 확인했다. 재생은 종료했고 원래 씬/분할 뷰로 돌아왔다. 모든 전투 조준 모양/높이 조합을 재생 확인한 것은 아니다.
- 사용자 설정 보존: CellFill.mat의 전이 폭 0.4, 중앙 비율 0을 그대로 사용했다. AO3에서 원본 재질/SO/씬 값을 쓰지 않았다. Unity의 기존 재질 직렬화 공백은 이번 소스 diff 검사에서 제외했다.
- 구조 자체 검토: 캐릭터 사실 원본은 완료 표시 스냅숏, 호버 원점의 표시 수명은 Field 상태, 열린 경계 정책은 기존 판단 부품 한 곳이다(SSOT). 원점 조회/표시 전달·판단·메시/셰이더 책임을 유지한다(SOLID). 공용 DTO/표시 계약 확장에 맞춰 기본값·다른 원점·해제 회귀를 검사했고 새 서비스/등록·Factory·CoreSystem·FieldCamera·Architecture 변경은 없다(JMH/FW·독립성). 추가 결함은 발견하지 못했다.
- AO3 구현·관련 GPU/메시 시험·실제 호버 화면 확인·자체 검토 완료.
