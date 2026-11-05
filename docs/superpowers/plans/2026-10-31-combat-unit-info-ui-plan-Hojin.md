# 전투 유닛 정보 UI(HUD 버프 패널·UnitInfo) 계획

- 작성자: 호진(이호진)
- 설계: [전투 유닛 정보 UI 설계](../specs/2026-10-31-combat-unit-info-ui-design-Hojin.md)
- 상태: 네 작업 모두 코드·프리팹 반영(2026-10-31). 실제 전투 재생은 모두 미확인, 커밋은 사용자(`8339e308f` 등).
- 정리: 2026-11-02 10-31 계획·기록 넷을 결과 요약으로 합쳤다. Task 체크 목록 원문은 git `3ad97e734`.
- 규칙: 사용자 승인 범위만, 사용자가 바꾼 프리팹 값은 보존, 새 시험 없음, Core·FieldCamera·Architecture 무변경, 커밋·push 없음.

## 1 진행 요약

| 작업 | 설계 | 상태 | 확인 |
|---|---|---|---|
| HUD 버프 호버 패널 | §2 | 완료 | 컴파일 성공(Unity Refresh, `ProjectL.Systems.dll` ExitCode 0)·HUD 프리팹 가져오기 성공. 재생 미확인 |
| UnitInfo 기본 정보 | §3 | 완료 | 컴파일 성공·DeckUI 가져오기 성공·Prefab Stage 캡처. 재생 미확인 |
| 특성 설명창 기준점 | §4 | 완료 | 정적 확인만. 컴파일·재생 미확인 |
| UnitInfo 버프·디버프 설명 | §5 | 완료 | 정적 확인만. 컴파일·재생 미확인 |

## 2 남은 일

- [ ] 재생 확인 — HUD: 처음 숨김, 버프 사이 전환, 이탈·상태 제거·HUD 숨김에 닫힘, 뒤쪽 지형·유닛 호버 막힘, 화면 가장자리 가독성.
- [ ] 재생 확인 — UnitInfo: 유닛 전환, HP·수치 갱신, 행동 가능 칸(소모·불가·적 선택), 버프 추가·제거와 여러 줄, 해상도별 배치.
- [ ] 재생 확인 — 특성 설명창: Combat 진입 때 옛 Grid 안 패널 검사 오류가 없어졌는지, 두 아이콘과 늘어난 슬롯의 기준점 위치, 해상도 변경·UnitInfo 이동 중 따라감, 이탈·숨김에 닫힘.
- [ ] 재생 확인 — 상태 설명: 버프·디버프 사이 전환, 효과 끝, 유닛 전환, 여러 줄 슬롯.
- [ ] 미확정: 사용자 보고 "HP·공격력·방어력 글자가 설명창 위에 보인다". 저장된 프리팹에서는 공용 패널이 UnitInfo 마지막 자식이고 따로 Canvas가 없다. 실제 화면을 보기 전에는 원인·해결로 적지 않는다(본문 Overflow와는 다른 현상).

## 3 작업별 결과

### 3.1 HUD 버프 호버 패널

- `BuffPanel` 배경 보존, 280×160, 제목 18·본문 16(최소 12 자동 축소), 기존 글꼴. 처음 꺼짐, Raycast 끔.
- 버프 패널을 따로 직렬화 칸으로 이어 아이템 설명 칸을 지켰다. 클릭 고정은 아이템 슬롯에만 남는다.
- 제목·본문은 `CombatUnitStatuses` → `UnitHUDStatus`로 넘긴다. HUD 프리팹 ID 중복 없음·로컬 참조 유효.

### 3.2 UnitInfo 기본 정보

- 새 영역 넷(`HealthBar`·`ActionAvailability`·`CombatStats`·`StatusIconArea`), `AbilityArea` 이동·아이콘 60×60. `UnitIcon`·`Level` 블록 8개가 편집 전과 같음을 확인했다.
- `CombatUnitInfo`·Factory에 `CombatUnitStatuses` 주입, `CombatInputSurfaces`·`SyncActionButtons`에 가용성 답 전달.
- DeckUI ID 중복 없음·참조 유효, 코드 `diff --check` 통과. 프리팹 diff의 YAML 빈 값 뒤 공백은 사용자 선행 변경이라 손대지 않았다.

### 3.3 특성 설명창 기준점

- `UnitAbilitySlot.tooltipAnchor`·전투용 검사·복제 때 참조 매핑, `CombatUnitInfoView` 위치·피벗 맞춤.
- DeckUI 두 `AbilIcon` 아래 `TooltipAnchor`(기존 패널 RectTransform 값 복사), 두 패널을 UnitInfo 아래로. 뒤이어 사용자 요청으로 `AbilityPannel2`와 자식을 지우고 `AbilityPannel1` → `AbilityPannel`(지운 것에 대한 외부 참조는 부모 자식 목록뿐, `Combat.unity` 참조 없음).

### 3.4 UnitInfo 버프·디버프 설명

- `CombatUnitInfoView`에 패널·제목·설명 참조와 아이콘 기준 위치 오프셋, `StatusIconTemplate` Button 연결, 입력 수신자 분리.
- 패널은 Grid 밖 UnitInfo 자식, 기본 꺼짐. 프리팹 로컬 fileID 중복·끊긴 참조 없음, 특성 패널 보존.
