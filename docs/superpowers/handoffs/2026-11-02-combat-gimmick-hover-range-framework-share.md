# 전투 기믹·Rider 연막 호버 범위 — 입력 Factory 연결 공유(2026-11-02)

- 받는 분: 민형님(Framework/Factory 작업자)
- 상태: 전달 준비 문서. 외부 발송하지 않음.
- 원본: [설계](../specs/2026-11-02-combat-gimmick-hover-range-design.md), [GH-a 실행 기록](../plans/2026-11-02-combat-gimmick-hover-range-plan.md#42-gh-a-전체-구현2026-11-02).

## 바뀐 연결

`Factory/Runtime/InGame/View/CombatSceneInputFactory.CreateEnvironmentHover`가 기존 `ICombatBattleDisplay` 인스턴스에서 새 좁은 계약 `ICombatGimmickRangeFeedback`을 확인하고 `CombatEnvironmentHover` 생성자 마지막 인자로 전달한다. 구현이 없으면 대상을 명시한 `InvalidOperationException`으로 연결을 멈춘다. 실제 씬 표시는 `CombatSceneDisplay`가 이 계약을 구현한다.

`ICombatBattleDisplay` 자체는 변경하지 않았다. 새 계약은 `ViewSystem/Combat/Input/Contracts/` 아래에 있으며 두 호출(`ShowGimmickRange`, `ClearGimmickRange`)만 갖는다. 생성자는 새 의존성을 필수로 받는다. GH-a에서는 환경 설명 생성자를 바꾸지 않았으며, 후속 GH-c가 기존 특성 조회 인자를 추가한다. 다른 기능의 미리보기 슬롯은 변경하지 않았다.

## 소유와 수명

| 항목 | 경로 |
|---|---|
| 생성/연결 | 기존 씬 Display 생성 뒤 입력 Factory가 같은 인스턴스의 좁은 계약을 주입 |
| 내부 등록 | 새 Framework 서비스/모듈 등록 없음. 기존 `CombatSceneInput`이 환경 호버를 보관 |
| 상위 Export/Builder | Group·루트 Export·Builder 변경 없음 |
| 소비 | 환경 호버 → 좁은 표시 계약 → Display 전달 → `FieldView` 전용 강조 층 |
| 해제 | 기존 환경 호버 Clear/Stop, 결과 재생의 허용 해제, Display/필드 해제 경로. 유닛·공격 조준 층은 건드리지 않음 |

기믹은 토네이도와 하붑이다. 영역 원본은 기존 Buffer의 공개 칸이며 새 Core API나 DB 표가 필요 없다. 기존 칸 집기·UI 가림 결과를 사용해 새 입력 바인딩·Raycast·Update를 더하지 않았다.

## GH-b: Rider 먼지 연막 추가(2026-11-02)

사용자가 같은 Rider가 남긴 활성 연막 전체를 한 그룹으로 확정했다. `CombatDustScreenHoverCells.Append`가 Buffer의 `TraitTrailFact`에서 활성·플레이어 공개·`DustScreen`인 흔적을 읽고 같은 `Origin`의 전체 칸을 기존 목록에 더한다. 외형이 하붑과 임시로 같아도 구분은 특성 사실로 한다.

환경 호버의 수집 호출만 한 줄 추가했다. Factory 인자·서비스/Export·렌더 층은 늘리지 않았고, 기존 표시 계약과 층 이름은 호환을 위해 유지했다. 관련 설명·인스펙터 툴팁에 Rider 연막을 포함했다. 특성 흔적 Presenter·Core·외형 에셋은 변경하지 않았다.

## GH-c: Rider 연막 전용 설명(2026-11-02)

`CombatEnvironmentText` 생성자 마지막에 필수 `ITraitVisualTableQuery` 인자를 추가했고, 같은 `CreateEnvironmentHover`에서 기존 `tables.Traits`를 전달한다. 새 표나 서비스 등록·상위 Export·Builder 변경은 없다. 실제 생성 호출은 이 Factory 한 곳이다.

`TraitVisualData`의 새 `TrailDescription`을 읽고, 이름은 기존 `DisplayName`을 쓴다. 숫자 토큰은 해당 `TraitTrailFact`의 공개 수치로 치환하며 활성·플레이어 공개 흔적만 설명한다. `TraitVisual_DustScreen`에 기본 문구를 추가했다. 기존 특성 `Description`과 임시 외형은 바꾸지 않았다. 설명의 빈 값은 정상 입력이며 범위 표시 수명은 유지한다.

## 확인 상태

코드 정적 점검만 완료했다. 컴파일 미확인. 새 시험·기존 시험 변경·MSBuild·Unity 실행 없음. 사용자가 Unity에서 확인할 항목은 계획 §3에 있다.

## 토네이도 클릭 도착 범위 (GH-d, 2026-11-03)

기존 호버 현재 범위에 더해, 토네이도 빈 영역 칸 클릭은 다음 이동 완료 위치의 범위를 고정 표시한다. `CombatSceneInput`이 기존 행동 처리 후 클릭을 전달하고 `CombatEnvironmentHover` 하나가 현재/고정 범위를 소유한다. 유닛·전경 행동 클릭은 우선 처리하며 새 명령이나 입력 슬롯을 만들지 않는다. 다른 곳 클릭/취소/새 결과/상태/전경 활성/연출/정지에 고정을 해제한다.

새 `CombatTornadoDestinationCells`는 Buffer의 플레이어 공개 Warning 영역에서 Core가 공개한 도착 칸(`TornadoAreaFact.DestinationCells`)을 그대로 모은다(2026-11-04 평가 C3-01. 그 전에는 같은 원본의 이동 거리로 Core 공개 `TornadoGeometry.Travel/Footprint`를 다시 계산했다). 방향이 아직 미정인 Cooldown/배치 단계는 미래 범위를 만들지 않는다. Core 파일/Buffer 계약/난수/규칙 계산 구현은 수정하지 않았으며, 기존 격자 층·색·서비스 등록/Export/생성 경로를 유지한다. 자세한 인수는 [GH-d 계획](../plans/2026-11-03-tornado-click-destination-plan.md)을 따른다. 코드·정적 검토 완료, **컴파일 미확인**.
