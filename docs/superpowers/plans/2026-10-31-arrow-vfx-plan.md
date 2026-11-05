# 충격 화살 VFX Task 계획

설계 원본: `../specs/2026-10-31-arrow-vfx-design.md`.

## Phase 1 — 독립 효과 에셋 제작과 미리보기

- [x] Arrow 폴더/원본 프리팹/모델/머티리얼 검사, 수정 전 백업(`.utmp/arrow-vfx/original/Arrow/`).
- [x] 설계·수명·조립 경계 기록.
- [x] 편집 가능한 Vertex Color 기반 Shader Graph와 청록·아이보리·먼지 머티리얼 제작.
- [x] 기존 Arrow GUID를 유지하여 비행 파티클 재구성. 모델 재사용, 가는 나선과 꼬리 추가.
- [x] 단발 ArrowImpact 제작: 섬광/원호/바람/먼지/돌.
- [x] 열린 씬과 독립된 비행→착탄 Editor 미리보기 창 제공.
- [x] Unity 임포트/컴파일, 네이티브 렌더와 수명/반복 경계 검증.
- [x] 결과·실제 확인 방법·남는 제한 기록 후 멈추고 사용자 리뷰.

## Phase 1R — 충격 화살로 시각 수정 (사용자 후속 요청)

- [x] “바람 마법으로 읽힌다”는 피드백을 반영해 설계 표현 갱신.
- [x] 기존 비행/착탄 에셋을 백업하고 GUID·호출 계약 유지.
- [x] 넓고 각진 금속 촉, 짧은 아이보리 압축선·직선 속도선으로 비행 변경.
- [x] 강한 착탄 섬광, 빠른 충격 링, 짧은 균열, 밀려나는 먼지·돌로 착탄 변경.
- [x] 미리보기·설명 갱신, 실제 렌더/반복/종료 검증 후 리뷰.

## Phase 2 — 사용자 리뷰 뒤

- 실제 게임의 발사/도착/취소 호출과 자원 대여·반환 경계를 확인하고 연결한다.
- Phase 1 시각 피드백을 반영한다. 필요 없고 외부 참조도 없는 기존 에셋만 정리한다.
- 이번 제작 요청만으로 Core 계산/전투 흐름을 수정하지 않는다.

## 실행 기록

- 최초 작업 트리: Arrow 외에는 Addressable 설정, FMOD 두 파일, JMHTest 씬, 새 FallingRocks 에셋이 이미 변경돼 있었다. Arrow 내부 `Shader Graphs_MainLayer.mat`도 사용자 수정 상태였다. 모두 보존 대상이다.
- 원본 검사: 비행 프리팹은 Body + AirLine + Layer1~7. Body는 외부 rock FBX 머티리얼을 참조해 흰색으로 보이고, 비행 입자가 노란 불꽃/검은 연기/흰 띠로 구성돼 있었다.
- Phase 1 결과: Arrow 프리팹을 Body/WindRibbon/WindFlecks/AirLine으로 재구성했다. 기존 FBX의 ArrowObject를 복사한 색상 메시와 간단한 깃을 사용한다. ArrowImpact는 ContactFlash/BrokenWindArcs/WindStrokes/DustPuffs/EarthChips 단발 구성이다. 비행 루트 GUID는 `48189758eeffe9b44a2224c0882ae82a`로 유지했다.
- 셰이더는 각각 11개 네이티브 노드, 7개 연결, Tint/Opacity 속성 2개다. Mesh 색을 Color32로 저장하여 Shuriken이 Float32 색을 잘못 읽던 문제를 해결했다. 나선 끝의 삼각함수 오차는 0 이상으로 보정해 NaN/AABB 오류를 제거했다. 깃의 중복 양면 삼각형도 제거했다.
- 꼬리는 Local 좌표에서 뒤로 움직이는 짧은 입자를 Ribbon 모드로 연결한다. 양 끝 Width over Trail은 0이다. 몸체/나선은 4초 수명·4초 반복, 나선 Z 각속도 π rad/s로 교체 경계를 맞췄다.
- Unity 6000.0.73f1 실제 컴파일 성공. 두 Shader Graph supported=true, ShaderUtil 오류 0. 비행 0.3/1/3.995/4.005/60초에 Body/WindRibbon 각 1개 유지, 총 입자는 7~8개였다. StopEmittingAndClear 후 0개, 재시뮬레이션 생성 확인.
- 착탄 총 입자 수는 0.04초 12 / 0.13초 18 / 0.24초 12 / 0.4초 8 / 1초 0. 초기화 후 재생성 확인. Editor의 Simulate는 일시정지 상태를 남기므로 IsAlive를 런타임 종료 증거로 사용하지 않았다. 실제 런타임 회수·취소 계약은 Phase 2 대상이다.
- 네이티브 URP SingleCameraRequest로 단계별 캡처와 66프레임 비행→착탄 시퀀스를 렌더했다. 순차 시뮬레이션 마지막에도 착탄 입자 0개였다. 미리보기 이미지/GIF는 `output/arrow-vfx-2026-10-31/`에 있다. AI 컨셉 이미지가 아닌 실제 Unity 렌더다.
- 메뉴 **ProjectL → VFX → Arrow Preview**에서 세 모드/재시작/일시정지/반복을 실제 실행 확인했다. 임시 검사·빌드 코드는 `.utmp/arrow-vfx/workshop-source/`에 보관하고 Assets에서 제거한다. 상시 남는 코드는 Arrow 폴더 안의 Editor 미리보기 창 하나뿐이다.
- 기존 Shader/Material/FBX는 삭제하지 않았다. 원본 백업 대비 변경은 Arrow.prefab뿐이며 사용자 수정 `Shader Graphs_MainLayer.mat`의 해시는 동일하다. JMHTest 씬 저장, Core/FieldCamera/Architecture 수정, 커밋/push는 하지 않았다.
- 리뷰 지점: Phase 1 완료. 실제 발사·충돌·피격·풀·취소 연결은 하지 않았다. 현재 게임 카메라 거리에서의 최종 굵기/범위 튜닝은 사용자 시각 리뷰 뒤 반영한다.

### Phase 1R 기록 — 충격 화살 재디자인

- 사용자가 청록 나선 디자인을 충격 화살과 거리가 멀다고 지적했다. 청록색/나선/공전 요소를 제거하고 목재·강철·아이보리 압축 공기·황토 먼지 중심으로 교체했다. 원본 FBX는 보존하되 복사 메시의 촉을 각진 넓은 금속 형태로 재구성하고 목 부분에 금속 띠를 넣었다.
- 비행 자식은 Body/AirCompression/SpeedLines/PressureFlecks/AirLine이다. 촉 주변 압축선은 교차하는 가는 활 모양이며 회전하지 않는다. 처음 시도한 세 개의 넓은 압축 원호는 프로펠러처럼 보여 가는 선으로 수정했다.
- 착탄은 ContactFlash(0.1초)/ShockRing(0.23초)/GroundCracks(0.43초)/ImpactLines/DustPuffs/EarthChips이다. 충격 링은 빠르게 퍼지고 회전하지 않는다. 먼지는 8개, 돌은 6개 이하다.
- 두 셰이더 오류 0. 비행 0.3/1/3.995/4.005/60초에 본체/압축선 유지, 총 입자 7~8개. 착탄은 0.04초 23개, 0.13초 22개, 0.24초 16개, 0.4초 14개, 1초 0개. Stop Clear/재생성 통과. 66프레임 순차 시뮬레이션 종료에도 착탄 입자는 0개였다.
- 기존 자체 제작 v1의 외부 참조 없는 WindRibbon/WindTrail 머티리얼과 TaperedSpiral/BrokenWindArcs/WindStroke 메시만 정리했다. 초기 사용자 Shader/FBX/Material은 보존했다. v1 백업은 `.utmp/arrow-vfx/v1-backup/Arrow/`, 수정 도구 원본과 검증은 `.utmp/arrow-vfx-shock/`에 보관한다.
- Editor 미리보기는 프로젝트 에셋 변경 시 임시 PreviewScene을 해제하고 새 프리팹을 불러오도록 바꿨다. 메뉴/호출 계약과 비행·착탄 프리팹 GUID는 유지했다.
- 실제 Unity 렌더 디자인 시트와 GIF: `output/shock-arrow-v2-2026-10-31/`. 이전 출력은 별도 보존한다. JMHTest 씬 파일 해시는 수정 전후 동일하다. 커밋/push 및 전투 연결은 하지 않았다.
- 맵 배경을 함께 검증하려 했지만 복사한 렌더러/임시 카메라의 렌더에는 현재 맵의 전용 표시 경로가 재현되지 않았다. 이 캡처는 폐기하고 실제 맵에서 검증됐다고 보고하지 않는다. 제공한 이미지/GIF는 따뜻한 주황 배경의 독립 URP 미리보기다. 원래 게임 카메라에서의 최종 평가는 사용자 리뷰/후속 조립 단계에 남는다.
