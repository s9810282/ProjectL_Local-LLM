# 기본·겹침 테두리 방식 및 독립 공격 영역 구현 계획 (2026-11-02)

- 설계: [기본·겹침 강조와 독립 공격 영역](../specs/2026-11-02-field-outline-style-and-independent-impact-design.md).
- 사용자 확인: 기본 강조/겹침 강조 두 방식만 분리하고, 캐논은 조준 지점의 피해·폭발 영역만 닫는다.
- 순서: 설계 → 이 Task 계획 → Phase별 구현·보고. 각 Phase가 끝나면 멈춘다. 커밋·push는 하지 않는다.
- 검증 제한: Test 코드 작성·시험 실행·MSBuild 없이 정적 검토한다. Unity 컴파일·재생·DB 에셋 시험은 사용자 확인이며 보고에 **컴파일 미확인**을 명시한다.

## Phase OS1 — 기본/겹침 Merged·EachCell 분리

- [x] OS1.1 StageFieldViewData에 `overlapCellOutlineStyle` 하나와 조회·검증·설명을 추가하고 운영 에셋에 기존과 같은 Merged를 명시한다.
- [x] OS1.2 FieldCellShape가 깊이별 방식을 제공하게 하고, 테두리·채움·호버 덩어리 수집·중첩 경고가 실제 깊이로 조회하게 한다. 기존 크기 식과 높이별 깊이 전달은 유지한다.
- [x] OS1.3 필드 전용 채움 셰이더의 판/구멍 연결 방식을 분리하고 재질에 현재/다음 깊이 값을 각각 전달한다. 미지정 재질의 기존 동작도 유지한다.
- [x] OS1.4 참조·diff·설계 원칙을 정적 검토하고 Unity 확인 항목을 인계한 뒤 멈춘다.

대상: StageFieldViewDataSO.cs, StageFieldView_Default.asset, FieldCellShape.cs, FieldOutlineRenderer.cs, FieldCellFillStack.cs, Prefabs/Combat/Shaders/CombatCellFill.shader 및 관련 설계·계획 문서. 새로운 C# 파일·서비스·Factory는 만들지 않는다.

## Phase OS1b — 모든 채움 강조의 깊이 통일 (사용자 후속 요청)

- 사용자: 호버 공격 범위뿐 아니라 나머지 강조도 일부가 겹치면 전체를 겹침으로 표현한다. 설계 §1.1을 추가했고 OS2 전에 이 보완을 구현한다.
- [x] OS1b.1 현재 표시하는 종류의 겹침 관계와 기존 명시적 받침 관계에서 종류별 깊이를 정한다. 같은 종류의 모든 칸을 같은 깊이에 둔다.
- [x] OS1b.2 호버·테두리가 통일된 깊이를 따르게 하고, 빈 깊이를 건너 실제 바로 위 종류를 구멍 지도로 전달한다. 셰이더에는 그 종류의 실제 모양을 전달한다.
- [x] OS1b.3 지도/재질의 생성·해제, 평소 채움/행동 필터, 겹침 해제·연쇄 관계·깊이 이동의 캐시를 정적 검토하고 보고한 뒤 멈춘다.

대상: FieldCellFillStack.cs, 필드 전용 CombatCellFill.shader, 관련 설계·계획 설명. 종류 순서·깊이 결과 소비·Core·입력·Factory 계약은 바꾸지 않는다. Test 코드·MSBuild·Unity 실행은 하지 않는다.

## Phase OS2 — 캐논 피해·폭발 영역 닫기 (OS1b 리뷰 뒤)

- [x] OS2.1 기존 공격 미리보기의 실제 병과 사실에서 캐논 영향 표시의 닫힘을 한 곳에서 정해 전달한다.
- [x] OS2.2 영향 상태와 같은 수명으로 닫힘 정보를 보관·해제하고 FieldAttackOutlineOpening의 영향 영역/합쳐진 호버 판단에 연결한다.
- [x] OS2.3 Outline·채움에 같은 닫힘이 전달되는지, 일반 공격·전체 선택 범위·해제 경로를 정적 검토하고 보고한다.

예상 대상: 공격 입력/표시 계약과 CombatSceneDisplay·CombatAttackFeedback, FieldView·FieldHighlightState/읽기 계약·FieldAttackOutlineOpening. CoreSystem·Buffer 규칙·일반 렌더 메시 생성기는 변경하지 않는다. 구체적인 전달 인자는 구현 전에 기존 호출자와 최소 변경으로 확정한다.

전달 확정: 기존 ShowAffectedCells 계약에 `closeOutline = false` 선택 인자를 추가한다. 성공한 AttackPreview의 실제 병과로 호출자가 한 번 판단하고, Field의 영향 표시 입구가 칸과 닫힘을 상태에 함께 반영한다. 일반 층 갱신·ClearLayer·Reset은 닫힘을 해제하며, 칸이 같아도 닫힘 변경은 다시 그린다. 기존 기본 호출은 열린 정책이다.

## 실행 기록

- 시작 시 사용자 변경이 FMOD 캐시·일부 전투/유닛 SO·외형 SO·폰트·이펙트 프리팹·Combat 씬에 있다. 유지한다.
- 현재 단계: **OS1·OS1b·OS2 코드·정적 검토 완료, Unity 확인 대기.**

### OS1 구현·정적 검토

- 기존 `Cell Outline Style`은 깊이 0에, 새 `Overlap Cell Outline Style`은 깊이 1 이상에 적용한다. 운영 에셋에는 새 값만 Merged(0)로 추가했다. 기존 크기·반지름·재질 설정은 보존했다.
- FieldCellShape의 `EachCellAt(depth)`가 방식 선택의 원본이다. 표시기, 채움 재질, 합쳐진 호버의 덩어리 수집, 깊이 2 이상 중첩 경고가 이를 사용한다. 높이별 추가 핸들의 `InsetDepth` 전달도 유지돼 고지대에서 기본 방식으로 돌아가지 않는다.
- 채움 셰이더에 `_HoleJoinCells`를 추가했다. 재질 사본은 현재 판/다음 판의 연결 방식을 각각 전달한다. 미지정 값 -1은 기존 `_JoinCells`를 따르므로 기존 직접 재질 사용은 같은 방식의 판/구멍을 유지한다. 렌더 재질 속성 검사에도 추가했다.
- 정적 검토: 이전 `_shape.EachCell`·`Style` 소비자가 남지 않은 것을 확인했다. 변경 파일 범위의 `git diff --check` 통과. 수정한 C#·셰이더는 UTF-8·LF다. 새 C#·meta·서비스·Factory·입력·CoreSystem·FieldCamera·Architecture 변경은 없다.
- 원칙 검토: 설정·깊이·모양의 원본 분리(SSOT), 기존 모양/표시/재질 책임과 좁은 조회 유지(SOLID 5원칙), 등록·구체 DTO·핸들 반환 수명 유지(JMH/FW), 캐논 정책과 방식 설정의 수정 범위 분리(기능 독립성)를 대조했다.
- **컴파일 미확인.** MSBuild·시험·Unity 실행은 하지 않았다. Test 코드는 작성하거나 수정하지 않았다.

### OS1 사용자 Unity 확인

1. 다음 Combat 진입에서 두 설정이 보이고, 기본 Merged + 겹침 EachCell 및 반대 조합에서 테두리·채움의 방식이 각각 맞는지 확인한다. 같은 방식 두 조합도 확인한다.
2. 겹친 호버가 같은 색 판에 합쳐질 때 덩어리/한 칸 테두리가 해당 깊이를 따르는지, 아래 채움이 위 판 모양만큼 비워지는지 확인한다. 고지대와 평지에서도 같다.
3. 새 에셋 필드가 추가됐으므로 `DataBaseAssetTests`를 확인한다.

### OS1b 구현·정적 검토

- 모든 채우는 강조에 대해 현재 표시 칸의 겹침 관계를 모으고, 기존 순서대로 아래 종류의 최대 깊이 + 1을 계산한다. 같은 종류 전체가 같은 깊이다. 명시적 호버 공격/이동 받침 관계도 유지한다. 같은 색 호버는 아래 판 깊이를 따르고 별도 호버는 해당 칸 맨 위 판 위에 놓인다.
- 실제 표시 종류를 모으는 필터는 깊이 계산/쌓기가 공유한다. 배치 구역, 평소 채움(낙석 경고), 행동 중 채움의 기존 조건을 유지했다. 매번 깊이·관계를 다시 파생하며 해제된 종류의 칸/지도도 비운다. 테두리는 기존 깊이 결과 조회를 그대로 써 통일된 크기/방식을 받는다.
- 빈 깊이 대응: 실제 바로 위 종류와 그 판 자체의 이웃 비트를 RFloat 구멍 지도에 묶어 넣는다. 셰이더는 같은 깊이 결과에서 만든 종류별 모양 배열로 비운다. 옆 칸의 받침/더 가까운 다른 판 유무로 위 판 모양이 끊기지 않는다. 기존 직접 재질은 새 선택 값 기본 0으로 이전 구멍 해석을 유지한다.
- 수명/캐시: 구멍 지도는 기존 표시기 수명에서 생성·반환한다. 모양 배열·관계 배열·작업 목록을 재사용한다. 종류 깊이가 바뀌면 기존 재질 사본에도 새 모양을 전달하고, 갱신 실패 시 해당 재질의 캐시를 성공으로 남기지 않는다. 기존 중첩 경고도 빈 깊이를 건너 실제 아래 종류를 비교한다.
- 정적 확인: 변경 파일의 `git diff --check` 통과. 옛 EmptyDepth/칸별 받침 삽입 참조를 제거했고, 깊이 이동 후 빈 이전 판·색 합침 호버·높이 분리·구멍 비트 순서/형식·셰이더 기본 경로를 코드로 대조했다. 새 C#·meta·DB 입력 값·Core·입력·Factory 변경은 없다.
- 원칙 검토: 기존 상태에서 깊이를 파생하는 책임은 FillStack 한 곳(SSOT/SRP)이며, Outline은 기존 조회 계약을 유지한다(O/L/I/D). 셰이더는 전달된 모양과 이웃만 그린다. 새 등록/서비스 없이 기존 핸들·지도·재질 반환 수명을 유지한다(JMH/FW). 캐논 닫힘·전투 계산·조립과 독립된 파일 범위다.
- **컴파일 미확인.** Test 코드 작성·수정, 시험·MSBuild·Unity 실행을 하지 않았다.

### OS1b 사용자 Unity 확인

1. 일반 공격 범위·피해 영역·위험 범위·넉백 도착 등에서 일부 칸만 아래 강조와 겹치게 하고, 같은 종류의 나머지 칸도 같은 크기와 Overlap Cell Outline Style인지 확인한다.
2. A/B는 한쪽에서, B/C는 다른 쪽에서 겹치는 연쇄 경우와 세 겹 중 중간 깊이가 빈 칸에서 위아래 순서·구멍·채움 색이 맞는지 확인한다. 아래 판 밖으로 이어지는 위 Merged 판도 구멍 이음이 일치해야 한다.
3. 강조 해제/다른 조준으로 전환해 전체 깊이가 다시 계산되는지, 같은 색/다른 색 호버·평소 낙석 경고·고지대가 유지되는지 확인한다. 기본/겹침 Merged/EachCell 혼합 두 조합도 확인한다.

### OS2 범위와 확인 기준

- 목표: 캐논의 조준 지점 피해·폭발 영역만 닫힌 테두리/채움 가장자리로 그린다.
- 파일: 공격 영향 표시 계약/호출자, CombatSceneDisplay·CombatAttackFeedback, Field 상태·읽기 계약, FieldAttackOutlineOpening을 최소 변경한다.
- 주의: 같은 색으로 합쳐진 호버도 닫힘을 따르고, 영향 해제 때 닫힘 상태를 비운다. 전체 선택 범위와 일반 공격의 기존 열림은 유지한다. Core나 일반 메시 생성 변경은 계획에 없다.
- 확인: 캐논·캐논 전직 메인 캐릭터의 피해 영역 네 변, 일반 공격 전환/취소·해제, 전체 선택 범위 유지.

### OS2 구현·정적 검토

- AttackTargetingInteraction의 기존 성공 응답 표시 지점에서 `attack.ProjectedCategory.Key == CategoryKey.Cannon`을 한 번 판단한다. Core의 AttackResultFactory가 실제 공격자 `Stats.Category`를 응답에 넣고, 전직 메인의 Category도 실제 전투 병과임을 읽기 확인했다. Core 파일은 수정하지 않았다.
- ShowAffectedCells의 `closeOutline = false` 선택 인자를 통해 SceneDisplay·공격 피드백이 값만 전달한다. FieldView.SetAffectedCells가 영향 칸/닫힘을 함께 적용하고, FieldHighlightState.IsAffectedOutlineClosed가 그 수명을 소유한다. 일반 영향 교체·빈 영역·ClearLayer·Reset/Clear 때 해제한다.
- FieldAttackOutlineOpening은 닫힌 Affected에만 기본 DTO(열린 변·원점 제거 영역 없음)를 반환한다. 기존 Outline/채움이 같은 판단을 사용하고, Affected에 같은 색으로 합쳐진 호버도 같은 닫힘을 받는다. 전체 공격 선택 범위·이동/유닛 호버 범위·일반 공격의 조건은 그대로다.
- 동일한 칸에서 닫힘만 바뀌어도 변경 칸 목록을 만들고 다시 그린다. Outline의 열린 변 캐시 비교, 채움의 칸 캐시보다 앞선 SetOpening 갱신 경로를 대조했다. 조준 불가·포인터 이탈·모드 정리의 ClearAffectedCells와 FieldView.Release의 상태 Clear도 확인했다.
- 정적 확인: 변경 범위 `git diff --check` 통과, 계약/구현/호출 서명 일치 및 상태 읽기 계약 구현자를 확인했다. Test 코드 수정은 필요하지 않았고 작성·실행하지 않았다. 수정 C#은 UTF-8·LF다. OS2에서 SO·씬·셰이더·일반 렌더 메시·Core·Factory·서비스 등록·FieldCamera·Architecture는 수정하지 않았다.
- 원칙 검토: 병과는 기존 성공 응답, 표시 수명은 Field 상태, 경계 판단은 기존 Opening 부품 하나가 원본이다(SSOT). 입력은 표시 의도, SceneDisplay는 전달, Field는 상태, 기존 렌더는 그리기를 맡는다(SOLID S/O/L/I/D). 기존 서비스·등록·구체 DTO·핸들 반환 계약을 유지하고(JMH/FW), 깊이/방식/구멍 셰이더와 별도 파일에서 캐논 정책을 변경했다(기능 독립성).
- **컴파일 미확인.** MSBuild·시험·Unity 실행은 하지 않았다. 이 작업의 코드 Phase는 모두 끝났으며 아래 Unity 인수는 사용자 확인이다.

### OS2 사용자 Unity 확인

1. 일반 캐논과 캐논 전직 메인 캐릭터에서 조준 지점 피해·폭발 영역의 네 변과 채움 가장자리가 닫히는지 확인한다. 같은 색으로 합쳐진 호버, Merged/EachCell·겹침·지형 높이 분리에서도 같다.
2. 캐논의 전체 선택 가능 공격 범위는 기존 표시인지, 일반 공격의 원점 쪽 열린 경계는 유지되는지 확인한다.
3. 조준 취소·불가 응답·모드 종료 뒤 일반 공격으로 전환하거나 필드에 재진입해 닫힘 상태가 남지 않는지 확인한다. 같은 영향 칸에서 일반/캐논이 바뀌는 경우도 다시 그려져야 한다.
