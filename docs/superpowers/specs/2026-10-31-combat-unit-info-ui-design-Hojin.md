# 전투 유닛 정보 UI(HUD 버프 패널·UnitInfo) 설계

- 작성자: 호진(이호진)
- 상태: 네 기능 모두 구현(2026-10-31, 사용자 승인). 컴파일은 일부 확인, 실제 전투 재생은 미확인(계획 §1).
- 범위: 전투 씬 유닛 HUD의 버프·디버프 호버 설명, `DeckUI/UnitInfo`(유닛 정보 창)의 기본 정보 배치·특성 설명창 위치·버프·디버프 호버 설명. Core 계산·Buffer 쓰기, 덱·손패 입력, 장착 아이템의 설명·우클릭 해제는 바꾸지 않는다.
- 계획: [전투 유닛 정보 UI 계획](../plans/2026-10-31-combat-unit-info-ui-plan-Hojin.md)
- 정리: 2026-11-02 HUD 버프 호버 패널 설계·계획, UnitInfo 기본 정보 배치 계획, 전투 특성 설명창 위치 기준점 기록, UnitInfo 버프·디버프 호버 설명 계획(모두 10-31)을 이 문서와 계획으로 합쳤다. 옛 판은 git `3ad97e734`.

## 1 공통 원칙

- 새 UIId·Canvas·서비스 등록은 없다. HUD 패널은 `UnitHUDLayer` 아래, 설명창들은 `UIId.CombatUnitInfo`(부모 `Deck`, Exclusive) 안의 세부 위젯이다(`Assets/AGENTS.md`의 "등록된 화면 내부의 세부 위젯").
- 입력은 `UIInputBinding`·`IUIInputReceiver` 호버다. Inspector UnityEvent·`Button.onClick`을 쓰지 않는다. 별도 Update 포인터 감지를 만들면 호버 상태 주인이 둘이 되므로 만들지 않는다.
- 설명 패널·글자는 Raycast Target을 끄고(`CanvasGroup.blocksRaycasts = false`) 아이콘 입력을 가리지 않는다. 아이콘은 Raycast Target을 켜 두어 기존 `CombatPointerMapper`의 GraphicRaycaster 판정으로 뒤쪽 지형·유닛 호버를 막는다.
- 제목·본문 원본은 View DB `UnitStatusVisual`·특성 외형 한 곳이고, `CombatUnitStatuses` → `UnitHUDStatus` 전달값을 그대로 쓴다. 제목을 합친 글에서 잘라 내지 않는다. 수치·행동 가능은 Buffer 응답이고 View에서 다시 계산하지 않는다.
- 호버가 끝나는 곳(이탈·상태 제거·화면 숨김·재사용·유닛 전환·Clear)에서 패널을 닫는다.

## 2 HUD 버프 호버 패널

- 사용자가 만든 `HUD.prefab`의 `BuffIconRoot/BuffPanel`(배경)을 쓴다. 제목·본문 TMP를 더하고 처음에는 꺼 둔다.
- 버프·디버프 아이콘에 호버하는 동안만 그 제목·설명을 보인다. 클릭으로 고정하지 않는다. 장착 아이템 설명은 따로 두어 클릭 고정·우클릭 해제가 그대로다.
- 표시 데이터: `UnitStatusVisualData.DisplayName`이 제목, `DescribeBody`가 본문이다.

## 3 UnitInfo 기본 정보

- `UnitIcon`·`Level`은 사용자 배치를 그대로 둔다(View DB `InfoIcon`, 기존 레벨 조회).
- 더한 영역: 가운데 위 `HealthBar`(`HealthFill`·`HealthText`), 오른쪽 `ActionAvailability`(`MoveAvailability`·`AttackAvailability`), 가운데 `CombatStats`(`AttackPowerText`·`DefenseText`), 얼굴 아래 `StatusIconArea`(`StatusIconTemplate`, 실제 상태 수만 만들고 재사용). `AbilityArea`는 오른쪽 아래로 옮겼다.
- HP·공격력·방어력은 `CharacterData` 공개값이다.
- 이동·공격 칸은 칸마다 밝음/어두움 둘이다(사용자 승인). 원본은 `CombatActionAvailabilityTracker.Read` 응답이고 `CombatInputSurfaces`가 넘긴다. 응답의 `ActorId`가 지금 보이는 유닛과 다르거나, 응답 대기·입력 막힘·튜토리얼 제한이거나, 적처럼 조회하지 않는 유닛이면 어둡다. 이 칸은 버튼이 아니다.
- 폭이 모자라면 특성·상태 아이콘은 다음 줄로 넘기고 배경을 늘린다. 사용자가 지운 `nameText`는 선택 칸으로 둔다.
- 상세 정보의 새 입력은 보류했다. `CombatUnitInfo`가 선택·데이터 조합, 위젯이 표시·배치, Factory가 `CombatUnitStatuses` 주입을 맡는다.

## 4 특성 설명창 위치 — 아이콘별 기준점

- 사용자가 아이콘 자식으로 맞춘 설명창 위치를 유지하고 싶어 했다. 고른 안: UnitInfo 안 **공용 설명창 하나**(`AbilityPannel`, 350×300)를 아이콘마다 둔 `TooltipAnchor` 위치·피벗에 놓는다. 슬롯마다 설명창은 복제·수명이 겹치고, 고정 위치는 아이콘별 배치를 못 따라가서 버렸다.
- 원본: 위치는 슬롯의 `TooltipAnchor`, 내용은 `UnitCardTraitContent`, 호버 상태는 `CombatUnitInfoView`.
- `UnitAbilitySlot.tooltipAnchor`는 공용 슬롯에서는 선택 칸이고 전투 정보창만 필수로 검사한다. 슬롯을 복제하면 기준점 참조도 복제본으로 잇는다. 호버 시작과 `LateUpdate`에 패널을 맞춰 Grid 재배치·화면 이동을 따라간다.
- 위치는 `AbilIcon/TooltipAnchor`의 Anchored Position·Pivot에서, 크기·모양은 `AbilityPannel`에서 고친다. 화면 가장자리 자동 보정은 범위 밖이다.

## 5 UnitInfo 버프·디버프 호버 설명

- `StatusIconArea`의 아이콘에 호버하면 제목·설명을 보인다. 특성 설명창보다 작게(사용자 요청) `StatusTooltipPanel` 280×180, 제목 22·본문 18 최대 자동 축소, 특성 패널의 배경·글꼴을 쓴다.
- `StatusIconTemplate`에 Button + `UIInputBinding`(호버만, 클릭 고정 없음). 풀링된 아이콘에도 연결한다.
- 특성 입력과 상태 입력은 수신자를 나눠 슬롯 번호가 섞이지 않고, 보이는 설명은 한 종류만이다.
- 목록을 새로 그릴 때 `UnitHUDStatus`를 보관한다. 효과 키가 바뀌거나 사라지면 닫고, 같은 효과의 수치만 바뀌면 본문을 고친다.
