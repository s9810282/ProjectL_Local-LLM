# 토네이도·하붑·Rider 먼지 연막 호버 범위 설계

- 갱신일: 2026-11-02
- 상태: GH-a~GH-c 코드 구현 완료. Unity 확인 대기·컴파일 미확인.
- 사용자 요청: 토네이도 및 먼지 그룹에 마우스를 올리면 범위 표시.
- 사용자 정정: 먼지구름은 `HaboobVisual_Default`를 사용하는 **하붑 기믹**이다.
- 후속 요청: 하붑에 더해 Rider 특성 `DustScreen`이 남기는 연막도 표시한다. 사용자 "같은 Rider가 남긴 활성 연막 전체"로 그룹 기준 확정(§8, GH-b).
- 설명 후속 요청: Rider 연막 전용 `Trail Description`을 추가하고 환경 설명 호버에 연결한다(§9, GH-c).
- Task 계획: [기믹 호버 범위 계획](../plans/2026-11-02-combat-gimmick-hover-range-plan.md).
- 클릭 후속(2026-11-03): [토네이도 도착 범위 설계·계획](../plans/2026-11-03-tornado-click-destination-plan.md). 기존 호버는 유지하고 클릭 시 공개된 Warning의 다음 도착 범위를 고정 표시한다. 이 문서의 현재 범위 전용 제한은 호버에만 적용하며, 후속 클릭은 별도 계획을 따른다. GH-d 코드 완료·컴파일 미확인.

## 1. 확인한 현재 구조

| 대상 | 현재 범위 원본 | 현재 표시 |
|---|---|---|
| 토네이도 | `CombatStateData.ReadTornadoAreas(Owner.Player)` → 각 `TornadoAreaFact.Cells` | 몸·방향 화살표, 영역 칸의 설명 호버 |
| 먼지구름(하붑) | `CombatStateData.Gimmicks.Entries` → `HaboobGimmickState.Cells` | 칸마다 연기, 영역 칸의 `HaboobVisual_Default` 이름·설명 호버 |
| Rider 먼지 연막 | `CombatStateData.Environment.Tiles` → `TraitTrailFact`의 `Cell`·`Origin` | 특성 흔적 외형·`TrailDescription` 호버. 활성·플레이어 공개 조건을 만족하는 `DustScreen`만 그룹 수집/설명(§8·§9) |

`HaboobVisual_Default`는 표현 설정이다. 영역은 SO나 연기 인스턴스에서 얻지 않고 Buffer의 `Cells`에서 읽는다. 떨어져 있는 칸도 같은 하붑 상태의 `Cells`에 들어 있으면 같은 그룹이다.

`CombatEnvironmentHover`는 UI 가림·칸 집기·현재 칸/결과 순번 캐시·설명 수명을 이미 소유한다. 반면 `CombatUnitRangeHover`는 유닛 우선으로 공격/이동 범위를 표시하며, 유닛이 없는 칸의 지뢰·화약통 범위도 표시한다. 이 기능에 기믹을 합치면 유닛이 구름 안에 있을 때 기믹 범위가 빠지고 같은 표시 창구를 서로 지우게 된다.

## 2. 화면 동작

| 상황 | 표시 |
|---|---|
| 토네이도 영역의 어느 칸에 호버 | 그 토네이도 원본의 현재 `Cells` 전체 강조 |
| 하붑 영역의 어느 칸에 호버 | 현재 하붑의 `Cells` 전체 강조 |
| Rider 연막 칸에 호버 | 같은 특성 출처(`Origin`)의 활성·공개 연막 전체 강조. 떨어진 칸도 포함 |
| 여러 토네이도 또는 토네이도·하붑이 호버 칸에서 겹침 | 호버 칸을 포함하는 그룹들의 전체 칸을 합쳐 표시. 중복 칸은 한 번만 표시 |
| 해당 칸에 유닛이 있음 | 기믹 범위를 유지하며 기존 유닛 호버 범위도 자기 규칙대로 표시 |
| 공격 모드 | 환경 설명 호버와 같은 허용 조건에서 기믹 범위도 표시. 조준 강조가 위에 보임 |
| UI 가림·필드 밖·포커스 손실·입력 정리·종료 | 기믹 범위 해제 |
| 결과 재생·후처리 | 숨김. 표시가 완료된 뒤 현재 Buffer 영역으로 다시 판단 |
| 기믹이 없는 칸 | 기믹 범위 해제 |

표시는 기존 격자 테두리·채우기 방식을 따른다. 전용 색은 `FieldView`의 직렬화 값 한 곳에서 조절하며, 기본은 청록색이다. 기믹 강조는 유닛 범위·이동 경로·공격 조준보다 아래에 둔다. 설명이 비어 있어도 영역은 표시한다.

토네이도의 배치 단계 영역도 기존 몸/설명과 동일한 공개 목록으로 표시한다. `IsActive`만으로 영역을 제외하지 않는다. 숨겨진 토네이도는 공개 조회 목록에 없으므로 표시하지 않는다.

다음 위치·이동 경로를 예측하거나 하붑 모양의 바깥 사각형을 채우지 않는다. 현재 공개된 칸 목록만 사용한다.

## 3. 책임과 연결

### GH1. 기믹 칸 수집

새 `Input/Assist/CombatGimmickHoverCells.cs`가 공개 토네이도 영역 목록·기믹 상태 목록·호버 칸을 받아, 그 칸을 포함하는 그룹들의 칸을 재사용 목록에 모은다. 중복 제거도 이 부품이 맡는다. 공격 가능 여부나 연결성·범위 모양을 계산하지 않는다.

토네이도 목록을 얻는 공개 조회는 호출할 때 파생 영역을 만든다. 현재 칸·결과 순번이 같은 캐시 경로에서는 다시 조회하지 않는다. 더 긴 캐시가 필요하면 현재 Buffer 객체를 기준으로 한 표현 캐시로 제한하고, 기믹의 권위 있는 상태는 복제하지 않는다.

### GH2. 호버 수명

`CombatEnvironmentHover`가 기존 칸 집기 결과를 수집 부품과 설명에 함께 전달한다. 별도 포인터 콜백·Raycast·Update는 만들지 않는다. 같은 Buffer 객체·같은 칸·같은 결과 순번이면 재사용한다. 캐시를 내려놓을 때 Buffer 참조도 비운다.

범위와 설명의 표시 여부는 따로 관리한다. 빈 설명을 숨기는 일이 범위를 없애면 안 된다. UI 가림·허용 해제·외부 `UnHover`·`Clear`는 범위도 정리한다. 결과가 바뀌면 마우스가 그대로여도 새 영역으로 판단한다.

### GH3. 좁은 표시 계약

새 `Input/Contracts/ICombatGimmickRangeFeedback`에 `ShowGimmickRange(cells)`·`ClearGimmickRange()`만 둔다. `ICombatBattleDisplay`의 기존 필수 계약에 추가하지 않고 실제 표시 구현이 별도로 제공한다. 씬 입력 Factory가 구성 오류를 명시적으로 검사한 뒤 이 좁은 계약을 연결한다.

`CombatSceneDisplay`는 전달만 하고, `FieldView`의 전용 층 `GimmickHoverRange`를 설정/해제한다. 생성과 연결은 `CombatSceneInputFactory`, 칸 수집과 호버 동작은 기능 부품이 맡는다. 별도 Framework 서비스나 UI 화면을 만들지 않는다.

### GH4. 강조 표시

- `FieldHighlightLayer`·`FieldHighlightColor`에 기믹 호버 한 항목을 추가한다.
- 순서·채움·평소 표시 여부는 `FieldHighlightLayers`의 표 한 곳에서 정한다. 기믹 호버는 전경 행동으로 취급하지 않고 평소 표시가 가능한 층으로 둔다.
- 색 원본은 `FieldView.gimmickHoverRangeColor`이며 `FieldHighlightPalette`가 전달한다. 기존 팔레트 생성 호출의 컴파일 호환성을 유지하는 방식을 계획에 둔다.
- 기존 `FieldOutlineRenderer`의 층 목록·높이·겹침·자원 수명 경로를 사용한다. 고정 슬롯 수 등 실제 연결부를 확인하고 필요한 곳만 맞춘다.
- 범위를 지울 때 자기 층만 지운다. 유닛 범위·위험 범위·조준·낙석 경고를 지우지 않는다.

## 4. 기존 설계와의 관계

[토네이도 설계](2026-11-01-combat-tornado-view-design-Hojin.md) TN2·Q-TN1의 기존 동작은 상시 칸 강조 없음이다. 이번 요청은 **호버 중에만 현재 영역을 강조**하는 확장이다. 기존 몸·화살표·이동·던짐 연출은 유지한다.

[하붑 설계](2026-11-01-combat-haboob-view-design-Hojin.md)의 칸별 연기·설명 위에 호버 영역을 추가한다. 기믹 규칙·연기 수명·이동 시점은 변경하지 않는다. 두 기존 문서의 호버 절에 이 설계의 연결을 기록했다.

## 5. 원칙 점검

| 기준 | 적용과 남는 공통 경계 |
|---|---|
| SSOT | 토네이도 공개 `Cells`, 하붑 상태 `Cells`가 영역 원본. View는 포함 여부·합집합만 처리하며 SO·연기 외형에서 영역을 추정하지 않음 |
| S | 수집·호버 수명·강조 표시·Factory 조립을 분리. 설명 문자열 작성에 범위 처리를 넣지 않음 |
| O | 기믹 전용 수집 부품과 층을 추가. 유닛·지뢰·화약통 내부에 기믹 분기를 넣지 않음 |
| L | 새 표시 계약은 반복 해제에 안전하고 자기 층만 해제. 기존 유닛 범위 계약을 바꾸지 않음 |
| I | 칸 표시/해제 두 호출만 노출. 수집 부품에는 필요한 사실 목록만 전달 |
| D | 호버는 좁은 표시 계약에 의존, 실제 연결은 Factory. 씬 검색이나 Core 직접 호출 없음 |
| JMH·FW | FW-004 소유 경계 준수. FW-005~007 기존 입력 경로 유지. FW-010~012 기존 렌더 자원 대여/반환 경로 사용. 새 서비스·Builder·UI 등록 없음 |

독립성 비교: 단순히 `CombatUnitRangeHover`의 위험 찾기에 합치면 기믹 영역 변경마다 유닛 우선순위·미리보기 슬롯·위험 표시를 함께 읽어야 한다. 구현에서는 그룹 판별 변경은 수집 부품, 호버 수명 변경은 환경 호버, 색 변경은 `FieldView` 값에 머문다. 공통 강조 표·팔레트·해제 수명은 공유 경계이므로 이 변경에 영향을 받는 겹침과 정리는 함께 확인한다.

## 6. 검증과 제외

View Test 코드를 새로 작성하지 않고 MSBuild도 실행하지 않는다. 기존 시험은 새 계약 때문에 컴파일이 깨지는 호출이 있을 때만 최소 조정한다. 다른 사람이 작성한 시험의 실행 의미를 바꿔야 하면 먼저 사용자에게 묻는다.

Unity 확인 항목과 Phase 완료 기준은 계획 문서에 둔다. 현재는 **컴파일 미확인**이다. Core·FieldCamera·Architecture·DB 에셋·기믹 외형 에셋 변경은 이 안에 포함하지 않는다.

## 7. GH-a 구현 메모(2026-11-02)

- 수집은 `CombatGimmickHoverCells.Collect`, 호버 수명은 `CombatEnvironmentHover`, 표시 계약은 `ICombatGimmickRangeFeedback`이다. 기존 설명 문자열 부품·유닛 호버 부품은 고치지 않았다.
- 층은 `GimmickHoverRange = 1 << 17`, 색 항목은 20이다. 강조 표에서 목표 표시 위·행위자/범위/조준 아래이며, 채움·평소 표시가 모두 참이다. 이 층 자체로 전경 행동 중이 되지 않는다.
- `FieldView.gimmickHoverRangeColor` 기본값은 `(0.15, 0.85, 0.75, 1)`이다. 운영 `CreatePalette`가 명시적으로 전달한다. 팔레트 생성자의 마지막 선택 인자는 기존 호출의 소스 호환성을 위한 것으로, 생략하면 투명색이다.
- `FieldOutlineRenderer`의 슬롯은 `DrawOrder`, `FieldCellFillStack`의 채움 종류는 같은 표의 `FillsCell`에서 파생된다. 두 부품은 수정하지 않았고 새 층도 기존 높이 분리·겹침·자원 반환 길을 따른다. CellPrefab 모드 역시 같은 강조 표와 팔레트로 색을 고른다.
- 외부 숨김 버전이 바뀌면 전용 범위도 지운다. 설명이 비면 설명만 숨기고, 범위가 비면 범위만 숨긴다. 결과 순번 또는 Buffer 객체가 바뀌면 마우스가 멈춰 있어도 다시 판단한다.
- 계획 밖 결정 없음. 새 테스트·기존 테스트 수정·MSBuild·Unity 실행 없음. 코드 정적 점검과 실제 화면 확인을 구분한다.

## 8. Rider 먼지 연막 확장(GH-b, 2026-11-02)

### 확인한 사실

- Rider 먼지 연막은 `state.Environment.Tiles`의 `TraitTrailFact`, `Key == TraitKey.DustScreen`이다. 하붑 상태와 다르며 임시 외형이 같아도 사실 형식으로 구분한다.
- 각 흔적은 `Id`·`Cell`·`Origin`·`Active`·`KnownTo`를 공개한다. `Origin`은 생성 유닛(`OwnerInstanceId`)·특성·규칙의 출처를 식별한다. 이동 한 번을 구분하는 그룹 번호는 없다.
- 기존 `FieldTrailPresenter`는 활성이고 플레이어에게 알려진 흔적을 표시한다. 호버 역시 같은 공개 조건을 사용한다. 만료된 흔적은 Buffer에 남아 있어도 제외한다.
- `HaboobVisual_Default`나 프리팹 참조를 검사하지 않는다. 연막 외형이 나중에 교체되어도 범위 동작은 유지되어야 한다.

### 그룹 기준 — 사용자 확정

사용자는 **같은 Rider가 남긴 활성 연막 전체**를 선택했다. 그룹은 호버한 연막과 같은 특성 출처(`Origin`)로 구분한다. 떨어진 칸도 같은 출처면 포함하고, 다른 Rider의 연막은 합치지 않는다. 한 칸에 여러 출처가 겹치면 그 출처들의 그룹을 합친다. 숨겨진/만료된 흔적은 포함하지 않는다.

### 책임·표시

- 새 `Input/Assist/CombatDustScreenHoverCells.cs`가 Rider 연막의 포함 여부·그룹 칸 수집만 맡는다. 토네이도·하붑 전용 `CombatGimmickHoverCells`에 Rider 분기를 넣지 않는다.
- `CombatEnvironmentHover`가 기존 기믹 칸을 모은 뒤 Rider 연막 칸을 같은 재사용 목록에 덧붙인다. 두 수집 부품의 조합만 이 호버에 두고, 규칙은 각 부품 안에 둔다.
- 기존 `ICombatGimmickRangeFeedback`와 `GimmickHoverRange` 층을 재사용한다. 이름은 기존 연결 호환성을 유지하며, 표시 계약 설명과 색상 툴팁에 Rider 연막도 포함한다. 새 층·색·렌더 자원·Factory 인자는 필요 없다.
- 유닛·조준과의 겹침, UI 가림·결과 재생·포인터 이탈·씬 해제, 빈 설명에서도 범위 표시가 되는 수명은 GH-a와 같다. GH-b에서는 설명 UI를 추가하지 않았으며 후속 GH-c가 연막 설명을 연결한다.

### 원칙 점검

범위 원본은 Buffer의 흔적 칸과 출처이며 표현 캐시 외의 상태를 만들지 않는다(SSOT). 수집은 별도 부품, 조합은 환경 호버, 표시는 기존 좁은 계약이 맡는다(S·I·D). 기존 기믹/유닛 내부를 바꾸지 않고 연막 부품을 추가하며(O), 같은 표시/해제 계약을 지킨다(L). 새 서비스·입력 바인딩·조회·렌더 자원 경로가 없어 FW 등록/수명 계약은 기존 경로를 따른다. 연막 그룹 변경은 연막 수집 부품에 머물고, 공통 범위 해제·중복 제거·기믹과 겹침은 통합 확인 경계로 남는다.

### 구현 결과

`CombatDustScreenHoverCells.Append`는 호버 칸의 활성·공개 DustScreen 흔적을 찾고, 같은 `Origin`의 활성·공개 흔적 칸을 기존 목록에 중복 없이 더한다. `IsKnownToPlayer`는 기존 `CombatEnvironmentFacts`를 재사용한다. 작업 목록은 환경 호버가 보관하며 수집 함수가 별도 목록/열거자를 만들지 않는다.

`CombatEnvironmentHover`에서 기존 기믹 수집 다음에 한 번 호출한다. 수명·캐시·렌더 연결은 변경하지 않았다. 표시 계약·층·팔레트 설명과 인스펙터 툴팁만 Rider까지 넓혔다. 코드 정적 점검 완료, 컴파일 미확인. 새 시험·기존 시험 변경·MSBuild·Unity 실행 없음.

## 9. Rider 연막 설명(GH-c, 2026-11-02)

사용자가 연막 전용 Description 필드와 호버 연결 추가를 지시했다. 기존 특성 아이콘 설명과 연막 칸 설명은 각각 작성한다.

- `TraitVisualData`에 직렬화 `trailDescription`과 읽기 전용 `TrailDescription`을 더한다. 이름은 기존 특성 `DisplayName`을 사용한다. 설명이 빈 값이면 설명 패널을 열지 않으며 기존 특성 Description을 대신 표시하지 않는다.
- `TraitVisual_DustScreen.asset`의 새 필드에 기본 문구를 작성한다. `{사거리감소량}`·`{피해감소율}`은 해당 `TraitTrailFact`의 `AttackRangeReduction`·`AttackDamageReductionPercent`로 치환한다. View가 효과 우선순위나 중첩 결과를 계산하지 않는다.
- `CombatEnvironmentText`가 활성·플레이어 공개·현재 칸의 `DustScreen` 흔적을 설명한다. 기존 환경 원본 순회·문자열 치환·표시 수명을 사용하고, 동일한 완성 설명은 같은 칸에서 한 번만 붙인다. 서로 다른 수치로 문구가 달라지면 각각 표시하며 유효 효과를 임의로 고르지 않는다.
- `CombatSceneInputFactory`가 기존 `tables.Traits`를 환경 설명 생성자에 필수로 주입한다. 새 서비스·표·UI 화면·입력 바인딩을 만들지 않는다. 표가 없으면 구성 오류이며 빈 설명은 정상 입력이다.
- 범위 수집·기존 특성 Description·Rider 임시 외형·Core 규칙은 변경하지 않는다. 이 Phase의 에셋 변경은 새 `trailDescription` 값뿐이다.

SSOT는 문구=특성 View SO, 수치=Buffer 사실, 칸/활성/공개=Buffer다. S/O/I/D는 기존 설명 작성 부품·좁은 특성 조회·Factory 조립을 사용하고, L은 빈 설명/반복 해제 등 기존 호버 계약을 유지한다. JMH·FW 서비스 등록·입력·UI·자원 수명은 변경하지 않는다. 설명 수정은 SO 필드와 설명 작성 부품에 머물며 범위 수집이나 렌더러를 읽거나 바꾸지 않는다. 공통 수명·겹침은 기존 경계로 함께 확인한다.

### GH-c 구현 결과

입력 위치는 `TraitVisual_DustScreen` 인스펙터의 `Trait Visual Data > Trail Description`이다. 기본 문구는 "연막 위 유닛의 공격 사거리가 {사거리감소량} 감소하고, 받는 공격 피해가 {피해감소율}% 감소합니다."로 넣었다. 두 자리에는 해당 흔적의 Buffer 수치가 들어간다.

`CombatEnvironmentText`가 기존 환경 순회에서 연막을 설명하고, 재사용 `HashSet<string>`으로 같은 완성 문구를 한 번만 붙인다. 매 Build에 이 표시 캐시를 비운다. 빈 설명은 연막 설명만 생략하며, 같은 칸의 하붑 등 다른 설명은 유지한다. 생성자의 새 특성 조회 인자는 Factory의 기존 `tables.Traits`다. 관련 생성 호출은 이 Factory 한 곳으로 확인했고 기존 시험을 고치지 않았다.

정적 점검 완료, 컴파일 미확인. DB 에셋 새 필드 값을 추가했으므로 사용자 Unity 확인에 `DataBaseAssetTests`를 포함한다. 새 시험·MSBuild·Test Runner·Unity 실행 없음. 에셋의 기존 프리팹/상시 이펙트 변경은 이 작업에 포함하지 않고 새 설명 필드만 스테이징한다.
