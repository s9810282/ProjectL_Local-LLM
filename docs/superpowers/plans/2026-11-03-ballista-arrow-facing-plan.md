# 발리스타 곡사 화살 방향 보정 — 설계·Task 계획 (2026-11-03)

상태: Phase A 코드 구현·정적 검토 완료. **컴파일 미확인**, Unity 재생 인수 대기. 사용자 요청은 화살촉 끝 Pivot을 가진 발리스타 화살의 부자연스러운 곡사 움직임 개선이다.

## 설계

- 현재 위치는 `CombatShotPath.ArcPoint`의 포물선을 따르지만 회전은 출발→도착의 정면에 시간당 XYZ 회전을 더한다. Ballista 에셋은 0.8초, 높이 5칸, 회전 (60,30,15)도/초다. 곡선 접선과 화살 방향이 일치하지 않으며 촉 끝 Pivot 주위로 몸통이 돈다.
- 화살촉 Pivot과 기존 프리팹 배치/사용자 변경은 유지한다. 현재 경로의 점은 계속 촉 끝 위치다. 곡사 SO에 `CombatArcRotationMode`를 추가한다: `Spin=0`은 기존 회전, `FollowPath=1`은 진행 방향 정렬. Ballista만 FollowPath로 설정한다. FollowPath는 XYZ Spin을 적용하지 않는다.
- 위치/시간/완급/피격/착탄/소리/미리보기는 유지한다. 포물선 미분 `to-from + up × 4h(1-2p)`를 `CombatShotPath.ArcTangent` 한 곳에 둔다. 완급 적용 후 위치와 같은 진행률 p를 사용하며, 발사 첫 자세도 p=0의 접선을 쓴다. 궤적을 추정하는 프레임 간 위치 차이 대신 해석적 접선을 사용해 첫 프레임/완급 정지 구간에서도 방향을 얻는다.
- 프리팹의 기존 축 보정 회전은 정렬 뒤 곱한다. 방향이 0인 퇴화 경로는 이전 회전을 보존하며, up과 방향이 평행하면 기존 풀의 보조 축 선택을 유지한다. 새 보간 지연/중심 Pivot/물리 시뮬레이션은 추가하지 않는다.
- 설정 전달: 곡사 SO → 불변 AttackPresentationData → 기존 클립/코드 모션 발사 경로 → 투사체 풀. 값 생성자와 풀 직접 호출은 기본값 Spin을 유지해 기존 호출의 의미를 보존한다. 단일 풀의 대여/반환/취소/재생 세대 계약은 변경하지 않는다.

## Phase A — 방향 정렬 구현

- [x] A1 enum/.meta, SO 필드/툴팁, 불변 값/검증/생성 전달을 추가한다.
- [x] A2 경로 접선과 풀의 최초/진행 회전을 연결하고 클립/코드 모션 양쪽에 설정을 전달한다.
- [x] A3 Ballista 투사체 에셋에 FollowPath를 적용한다. 프리팹과 기존 높이/시간은 보존한다.
- [x] A4 호출 지점·기본 Spin 유지·0방향/수직 경로·취소/재대여·GUID·diff를 정적으로 검토하고 결과를 기록한다.

## 구조 검토 기준

- SSOT: 곡선과 접선은 ShotPath, 모드 설정은 투사체 SO, 시간/대여는 기존 풀. 모델별 키 분기 없음.
- SOLID: S는 입력/값/기하/실행 책임 유지, O는 데이터로 선택하는 회전 방식 확장, L은 기존 Spin 기본값과 완료/취소 보장 유지, I는 기존 곡사 발사 계약에 회전 모드 하나만 전달, D는 기존 전달/조립 경로를 사용한다.
- FW/JMH: 기존 SceneDisplayFactory의 AttackPlayback/Flights/Pool 생성 경로를 사용하며 새 서비스/Export/조회/MonoBehaviour/UI 등록 없음. 전역 서비스·Core·Buffer에 새 의존성 없음.
- 독립성: Ballista 외형 방향은 SO 값과 공용 기하/풀에서 처리하고 피격/이펙트/진입/열차/무기/클립 코드 내부는 변경하지 않는다. 이번 공통 경계는 새 값의 발사 전달과 Spin 호환성이다. 이후 수치 조정은 SO 한 곳, 경로 변경은 ShotPath 한 곳에 남는다.

## 사용자 Unity 인수

컴파일·재생 미확인으로 인계한다. View Test 코드 작성·MSBuild·Unity 실행 없음. 가까운/먼 목표·대각선·높이가 다른 목표에서 발사 첫 자세부터 상승/정점/하강 방향과 촉 끝 착탄을 확인한다. 다른 대포의 Spin, 반복 발사/취소/재진입, 피격/착탄 소리 시점을 확인한다. DB 에셋 변경으로 `DataBaseAssetTests`도 사용자 확인 항목이다.

## Phase A 결과

`CombatArcRotationMode`와 .meta를 추가했다. 모드는 곡사 부품 SO에서 불변 값으로 복사되며, `MortarStyle → AttackPlayback → Flights → Pool`의 클립 발사와 `MortarAnimation → Pool`의 코드 모션에 모두 전달된다. 기존 값 생성자/Flights/Pool 직접 호출은 Spin 기본값을 유지한다. 내부 ICombatAttackStyleContext 구현은 AttackPlayback 한 곳이며 함께 서명을 갱신했다. 현재 시험 소스에서 서명을 맞춰야 하는 가짜 구현이나 호출은 발견되지 않았다.

`ArcTangent`는 기존 포물선의 미분이며 정점에서도 출발→목표의 수평 성분이 유지된다. 발사 첫 자세도 같은 식을 사용한다. 방향이 없는 경우 기존 자세를 유지하고, 비유한 파생 방향은 거절한다. 초기 회전 계산은 풀 대여 전에 끝낸다. Spin 분기는 기존 출발 회전 × 시간별 Euler 식과 보조 up축 선택을 유지했다. 시간·진행률·완료/접촉 콜백·회수 코드는 변경하지 않았다.

Ballista 에셋에 `rotationMode: 1`만 추가했다. 0.8초/높이 5칸/기존 Spin 수치는 보존되며 FollowPath에서는 Spin 값을 쓰지 않는다. 다른 곡사 에셋은 새 필드 기본값 Spin을 사용한다. Arrow 프리팹의 기존 사용자 수정(Body/Wind 위치·PlayOnAwake)을 보존했다. 프리팹의 파티클은 `moveWithTransform: 0`이다. 이는 SimulationSpace의 Local 값이며, 직렬화 필드 매핑과 enum은 [Unity InitialModuleUI](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ParticleSystemEditor/ParticleSystemModules/InitialModuleUI.cs)·[Unity ParticleSystemEnums](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ParticleSystem/Managed/ParticleSystemEnums.cs)에서 대조했다. 따라서 파티클 공간 변경은 추가하지 않았다. 실제 모델 방향과 촉 끝 일치는 Unity 재생 인수로 남긴다.

SSOT/SOLID/FW·JMH/독립성은 위 설계 기준을 유지한다. 새 등록/Export/전역 조회/서비스 상태 없음. 구성은 기존 SO 값 생성과 발사 전달 경로, 경로 기하는 ShotPath, 자세 적용과 대여는 Pool에 남는다. 코드/설정/호출 지점·GUID 고유성·diff 공백 정적 검토 완료. Core·FieldCamera·Architecture·프리팹·Test 코드는 수정하지 않았으며 MSBuild·Unity 컴파일/재생/시험은 실행하지 않았다. **컴파일 미확인**.
