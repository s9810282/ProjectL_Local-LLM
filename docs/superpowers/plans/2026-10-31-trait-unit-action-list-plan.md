# 특성 UnitAction 목록 — Task 계획

설계: [UnitAction 목록과 희생 물러나기](../specs/2026-10-31-trait-unit-action-list-design.md). 사용자 요청 범위는 View 동작과 기존 SO 입력 전환이다. 하나의 기능 단위인 Phase A 완료 뒤 리뷰 결과를 보고한다. Core·FieldCamera·Architecture 변경, 새 View 테스트, 커밋·push는 하지 않는다.

## Phase A — 목록 입력과 두 유닛의 희생 동작

- [x] A1 단일 입력·런타임 값·발동 줄 캐시를 목록으로 전환. 기존 SO 3개의 행동 값을 보존하며 마이그레이션.
- [x] A2 일반 특성 발동과 미리보기 진단의 행동 목록 소비 연결. 추가 공격의 행동 제외 정책 유지.
- [x] A3 가로채기 Targets 지원, AwayFromAttacker 자리 계산, 행동 시작 전 기준점 포착.
- [x] A4 Stand 대여를 실제 행동 유닛에 연결하고 모든 나가기 완료 뒤 공격. 수호자/행동 유닛의 후속 정산 뒤 복귀.
- [x] A5 희생 SO의 기존 수호자 설정 유지 + 보호 대상 물러나기 항목 추가.
- [x] A6 컴파일·직렬화/참조 확인·가능한 수동 재생. 사용 안내·제약 표 갱신, 검증/미검증 기록과 함께 보고.

## 착수 상태

- 기존 SO에서 사용자가 편집한 큐·거리·시간 값을 보존한다. 확인 시점 희생 수호자의 OutSeconds는 0.5초다.
- 실행 방식 선호를 질문했다. 답이 없으면 권장안인 동시 실행을 기준으로 진행한다.

## Phase A 실행 기록 — 2026-10-31

- `TraitActivationVisual` 입력과 `CombatActivationRows` 캐시를 읽기 전용 행동 목록으로 전환했다. None은 제외하고 null·같은 활성 Actor 중복은 검증 오류다.
- 일반 발동 처리기는 목록을 순회한다. 가로채기는 Source/Targets를 수호자/보호 대상으로 매핑하고, 기준 위치를 이동 전에 포착한다. 모든 나가기 조각 완료 뒤 공격하며 수호자 및 행동 유닛의 후속 정산 뒤 각자 복귀한다.
- 희생에 보호 대상의 `Targets / AwayFromAttacker / Distance 0.3 / Walk / Face Attacker` 항목을 추가했다. 기존 수호자 거리 0.6·Hop·Out 0.5초·Return 0.25초와 큐를 보존했다. 보호 대상도 Out 0.5초·Return 0.25초다.
- CleanSweep·DoubleShot·Sacrifice 3개 SO를 명시적으로 목록 형식으로 전환했다. 역변환 대조로 기존 필드 전체가 보존됨을 확인했고 각 meta/GUID도 원본과 일치한다. 현재 작업 트리의 다른 사용자 에셋 변경은 건드리지 않았다.
- MSBuild `Assembly-CSharp-Editor.csproj` 빌드 종료 코드 0. 기존 MSB3277 참조 경고 유지. Unity Assets Refresh 후 Tundra build success와 최신 `ProjectL.Systems` DLL 반영을 확인했다. 처음에는 Unity가 이전 DLL로 재생했으므로 그 재생은 이번 기능 검증에서 제외했다.
- 최신 미리보기 진단 `유닛 행동: 2개` 확인. 생존 설정에서 기본 시간으로 2회 완료, 동작 관찰용으로 Out/Return을 임시 5초로 늘린 1회에서 두 유닛의 동시 이동·공격·원위치 복귀 및 누적 완료 3회를 확인했다. 임시 시간은 최종 0.5/0.25초로 복원했다. 생존 재생 중 새 예외·Refid 오류·행동 건너뜀 경고 없음(`Temp/TraitUnitActionList/survival-play.log`).
- 최종 시간으로 사망 설정 재진입 후 완료 1회 확인. 수호자가 사망해 사라지고 보호 대상은 원위치에 남았다. 종료 전 로그에 새 예외·Refid 오류·행동 건너뜀 경고 없음(`Temp/TraitUnitActionList/final-death-play.log`). 원래 Kill=true·Repeat=false 입력을 복원하고 Play를 종료했다.
- 수동 검증에 Windows Computer Use 스킬을 사용했다. 새 View 테스트·검증 메뉴는 만들지 않았다. Core·FieldCamera·Architecture 수정 없음, `git diff --check` 통과.
- 사용법과 제약은 [미리보기 안내](../viewDocs/trait-activation-preview-guide.md)와 [특성/효과/연출 제약 표 §8.2](../viewDocs/trait-effects-and-activation-reference.md#82-unit-actions-목록)에 반영했다. 최초 단일 행동 설계는 목록 확장 문서로 이어지도록 표시했다.

검증 범위의 한계: 이번에는 일반 Moment의 다중 행동·추가 공격 행동 제외·같은 공격의 다중 가로채기·목록 순서 교체·중간 취소·보호 대상 자신의 동시 피격까지 수동 재현하지 않았다. 기존 종료 시 Setting UI 미등록 예외는 사용자 결정대로 유지한다.
