# 내부 채움 투명도 그라데이션 계획

- 날짜: 2026-11-01
- 설계: [opacity-gradient-design](../specs/2026-11-01-cell-fill-opacity-gradient-design.md)
- 범위가 작은 표현 변경으로 FG1 한 Phase에서 구현·검증·자체 검토 후 보고한다.

## Phase FG1

- [x] 기존 Outline과 채움의 책임, 셰이더의 거리/겹침 마스크, 색·전체 알파 원본 조사.
- [x] FG1.1 채움 셰이더에 전이 폭/중앙 비율 추가. 기존 윤곽 거리·알파 마스크 재사용, RGB/Outline 유지.
- [x] FG1.2 운영 CellFill.mat 기본값과 FieldCellFillStack의 필수 속성/범위 검사 연결.
- [x] FG1.3 관리 빌드·셰이더/거리 수치 대조·가능한 Unity 확인, SSOT/SOLID/FW/독립성 자체 검토와 실행 기록.

## 실행 기록

- 시작: 셰이더/재질은 작업 트리에서 깨끗한 상태였다. 사용자가 수정 중인 StageFieldView_Default.asset은 보존하며 변경 전 해시를 Temp/CellFillOpacity에 기록했다. 기존 셰이더/재질 원본도 같은 곳에 보관했다.
- FG1 구현: 채움의 기존 RGB를 반환하고 알파만 윤곽 거리로 감쇠한다. 전이 폭은 축소한 한 칸 짧은 변의 25%, 중앙은 기존 알파의 15%다. 기존 Outline·구멍 마스크·높이별 이웃·재질 사본 수명은 유지했다. 필수 속성과 유한값/범위 검사는 FieldCellFillStack.Build에서 한다.
- 빌드: ProjectL.Systems.csproj Debug 관리 빌드 종료 코드 0. 첫 시도는 Temp/bin의 참조 DLL 부재(CS0006)로 실패했다. Library/ScriptAssemblies의 현재 Unity 산출물을 Temp/bin/Debug에 복사한 뒤 통과했고, 최종 증분 빌드도 종료 코드 0이다. 로그는 Temp/CellFillOpacity/systems-build.log와 systems-build-final.log다. 첫 정상 빌드에는 이번 수정 파일 밖의 CS0649 경고가 있었다.
- 수치 대조: 기존 ClusterDistance를 CPU에서 계산해 4×3 사각형의 독립적인 외곽 거리와 비교했다. 정사각/가로세로 길이가 다른 칸 3종, 동일 높이/중간 열에서 높이가 갈리는 경우 총 63,906개 표본에서 감쇠 결과 최대 오차는 4.45e-16 미만이었다. 폭 0/0.1/0.25/0.45와 중앙 비율 0/0.15/1의 범위·단조 변화도 확인했다. GPU 결과나 임의 오목/둥근 윤곽 전체를 검증한 증거는 아니다.
- Unity 확인: 에디터 새로고침에서 셰이더 컴파일 표시가 끝났고 Console 로그/경고/오류가 모두 0인 상태를 확인했다. Play 재생은 하지 않았다. 실제 전투 화면의 진하기, 오목/둥근 모서리·EachCell·겹침 구멍의 시각 회귀와 GPU 비용은 미확인이다.
- 자체 검토: 색/전체 알파는 Palette, 모양은 기존 ClusterDistance, 전이 설정은 재질이 소유한다(SSOT). 표시 기능을 셰이더에 두고 입력·조립·서비스·해제 계약을 바꾸지 않았으며 SOLID 5원칙/JMH·FW/기능 독립성 기준에서 추가 분리나 공용 서비스가 필요하지 않았다. 검토 중 구성 전 종류 0의 재질에서 높이·이웃 지도를 읽지 않도록 가드를 보완했다. 사용자가 수정 중인 StageFieldView_Default.asset은 변경 전 해시와 일치했다.

## 조절 방법과 남은 확인

- 운영 재질: `Assets/ProjectL/Prefabs/Combat/Materials/CellFill.mat`.
- `Inner Fade Width`: 가장자리에서 안쪽으로 옅어지는 폭. 현재 0.25, 0이면 기존 단색이다.
- `Center Opacity Ratio`: 중앙 진하기/기존 채움 진하기. 현재 0.15, 1이면 감쇠가 없다.
- 기존 재질 사본 생성 시점에 따라 설정 변경은 다음 전투 진입에서 확인한다. FG1 코드 구현·자체 검토는 완료했고, 실제 게임 화면의 시각 인수는 남아 있다.
