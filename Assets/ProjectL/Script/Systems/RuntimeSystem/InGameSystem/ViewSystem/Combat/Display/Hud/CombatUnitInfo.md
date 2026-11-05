## 2026-11-03 후속: 호버에 특성 이름도, 정보창 칸은 아이콘만 (호진 요청, 현강님 미전달)

- 아래 절의 Unity 확인 문제 없음(사용자). 이어서 사용자 요청: UnitInfo 특성 칸에서 이름도 숨기고, 호버 패널에 제목(특성 이름)과 설명을 같이 띄운다. 아래 절의 "제목은 비운다", "칸에는 이름·아이콘"은 이 후속이 대신한다.
- `AbilityText`에서 이름 글자 참조(`nameText`)·검사·쓰기를 지웠다. 칸은 아이콘만 그리고 이름·설명 문자열은 기억만 한다. 호버는 `ShowHover(이름, 설명)`이며, 표시 중인 특성은 설명이 비어도 이름을 띄운다(빈 칸·숨긴 칸만 띄우지 않음). 정보 갱신 때는 이름·설명이 둘 다 같으면(Ordinal 비교) 패널을 다시 쓰지 않는다.
- GC: 앞 절과 같다. 이름도 정보를 그릴 때 만든 문자열을 그대로 넘기고(`?? string.Empty`는 참조 선택), 비교는 Ordinal이다.
- 프리팹(사용자): 특성 칸의 Name 셋(자리표시 글 "방어구 부수기")을 끄거나 지운다. 코드가 더는 채우지 않아 그대로 두면 자리표시 글이 보인다. 이름 글자가 꺼지면 호버는 아이콘 칸에만 반응한다. Ability2의 TraitDesc는 아직 켜져 있으나 Ability2가 꺼져 있어 보이지 않는다. 지운 `abilitySlots[].nameText` 직렬화 값은 남아 있어도 무시된다.
- 현강님 공유 메모 덧붙임(미전달): `CombatUnitInfoView`의 특성 칸 이름 글자 참조도 지웠다. Hover 패널의 기존 제목 칸을 그대로 쓰므로 TextNoticeUI 쪽 변경은 없다.
- **Unity 확인 문제 없음(2026-11-03 사용자).** 새 시험은 없고 MSBuild는 돌리지 않았다. 확인 항목: ① 특성 칸 호버 때 제목(특성 이름)과 설명이 함께 뜸 ② UnitInfo 특성 칸에 이름 글자가 안 보임(프리팹 정리 뒤) ③ 사용 불가 표시가 붙은 특성 이름 ④ 앞 절 ③~⑥ 다시 한 번.

## 2026-11-03 특성 설명은 공용 Hover 패널로 — 단일 Phase (호진 요청, 현강님 미전달)

- 사용자 요청: 전투 UnitInfo의 특성 칸에 마우스를 올리면 DontDestroyOnLoad의 `HoverTarget/HoverPannel`(공용 Hover, UIId.Hover)에 그 특성 설명만 띄운다. 제목 칸은 비운다(특성 이름은 정보창에 이미 보임). UnitInfo 특성 칸에는 이름·아이콘만 남기고 설명(TraitDesc)은 쓰지 않는다. 아래 11-02 "Ability Name·Description·Icon 직접 연결"의 설명 부분을 대신한다.
- 호버 대상: 각 Ability0~2 칸(아이콘을 감싼 90×90)에 이미 있는 Button. 상태 아이콘과 같은 `UIInputBinding`·`IUIInputReceiver` 방식이며 새 직렬화 참조는 없다. 등록 때 `icon.GetComponentInParent<Button>(true)`로 찾고, UnitInfo 내부·칸마다 다른 Button이 아니면 예외로 거절한다. Name 글자도 Raycast Target이 켜진 칸의 자식이라 이름 위에서도 열린다(아이콘에만 반응시키려면 프리팹에서 Name의 Raycast Target을 끈다).
- 서비스: `CombatUnitInfo.BindUIService`가 `resolver.GetService(SystemGroupsService.Global).UITextPresentation`을 받아 view에 넘긴다. OptionUI·SoundSettingUI와 같은 Global Context 경로이며 Factory·`CombatDisplayServices`·서비스 등록·루트 노출은 바꾸지 않았다. 연결 전 호버는 정상 대기로 보고 띄우지 않는다.
- 위치: 특성 칸 오른쪽 위 모서리의 화면 좌표를 `MoveHover`로 넘긴다(같은 창 상태 설명창과 같은 기준). 화면 밖 보정은 Hover 패널(TextNoticeUI)이 한다. 입력 장치를 직접 읽지 않는다. UnitInfo가 화면 왼쪽 아래라 패널이 위로 밀려 특성 이름을 가릴 수 있다.
- 닫기: 칸에서 나감, 정보창 숨김·슬라이드 퇴장·대상 변경·구조물 선택·Clear·표시 해제 때 닫는다. 밖에서 이미 닫혔으면(숨김 번호가 바뀜) 지금 열린 패널은 다른 사용처의 것일 수 있어 닫지 않는다(스테이지 맵 노드 호버 `StageNodeHoverInfo`와 같은 규칙). 설명이 빈 특성은 띄우지 않는다.
- 필드 설명 호버(`CombatEnvironmentHover`)와 같은 패널을 쓴다. 그쪽이 띄운 채 포인터가 바로 특성 칸으로 들어오면 같은 프레임의 처리 순서에 따라 그쪽이 방금 연 패널을 닫을 수 있다. 그래서 특성 칸 위에 있는 동안 `LateUpdate`에서 숨김 번호가 바뀌었으면 한 번 다시 연다(렌더 전이라 깜빡임 없음). 그쪽은 다시 닫지 않으므로 반복되지 않는다.
- 정보 갱신 때 호버 중인 칸의 글이 같으면(Ordinal 비교) 패널을 다시 쓰지 않는다. 글이 바뀌었거나 칸이 비면 다시 띄우거나 닫는다.
- 패널은 300×200 고정이라 제목 줄 자리(위쪽 약 50)가 빈 채로 남는다. 설명을 패널 전체로 늘리려면 TextNoticeUI·GlobalUICanvas의 Hover 배치를 고쳐야 해서 이번에는 하지 않았다.

### GC

- 입력 연결(`UIInputBinding` 셋)·수신 객체·칸 배열은 준비 때 한 번만 만든다. Button·Canvas 찾기도 등록·준비 때만 한다.
- 호버 진입·이탈·위치 계산은 할당이 없다: 설명 문자열은 정보를 그릴 때 이미 만든 것을 그대로 넘기고 제목은 `string.Empty`, 좌표는 `TransformPoint`·`RectTransformUtility.WorldToScreenPoint`(값 형식), 글 비교는 `string.Equals(…, Ordinal)`. 람다·LINQ·박싱·문자열 조합 없음.
- 매 프레임 일은 띄운 동안 `LateUpdate`의 숨김 번호 읽기 하나(UITextPresentation의 UIId 사전 조회)뿐이다.
- 남는 할당은 이 코드 밖이다: TextNoticeUI가 숨길 때 글을 비우므로 다시 띄울 때 TMP가 글을 다시 배치하고, `hasHover`라 상태 아이콘처럼 UIHover 효과음을 낸다.

### 프리팹(사용자)

- 사용자가 `DeckUI.prefab`의 TraitDesc 셋을 끄거나 지운다. 코드가 더는 채우지 않아 그대로 두면 자리표시 글이 보인다.
- 코드에서 지운 `abilitySlots[].descriptionText` 직렬화 값은 프리팹에 남아 있어도 무시되며 다음 저장 때 Unity가 정리한다.

### 규약 검토

- SSOT: 설명 원본은 기존 `UnitCardTraitContent.Description`(View DB 특성 외형) 한 곳이다. 칸은 표시 중인 문자열 참조만 들고 패널 열림 상태는 TextNoticeUI·UISystem이 갖는다. 숨김 번호는 패널이 원본이고 여기서는 관측만 한다.
- SOLID: 표시·호버 판단은 UnitInfo 위젯, 패널 위치·개폐 표현은 TextNoticeUI, 서비스 전달은 기존 Global Context가 맡는다. 새 서비스·UIId·인터페이스·이벤트 구독 없음.
- FW·UI 규약: Hover 패널은 기존 GlobalUI 등록 그대로 `IUITextPresentation` → `IUISystemService.OpenUI/CloseUI`로만 연다(FW-008·009). 하위 서비스 루트 재등록이나 루트 직접 조회를 더하지 않았다. 버튼은 `UIInputBinding`을 쓰고 onClick 리스너·UnityEvent를 쓰지 않는다. 필수 구성(칸 Button·상위 Canvas) 누락은 UIId·필드가 든 예외로 등록 전에 거절한다. UISystem의 닫기 순회 안에서 불리는 Hover 닫기는 순회 중인 등록부(`_uis`·`_parents`)를 바꾸지 않는다(코드 확인).
- 기능 독립성: 바뀐 곳은 UnitInfo의 두 파일뿐이다. 문구는 특성 표시 변환, 패널 모양은 TextNoticeUI, 연결은 BindUIService 한 줄에 머문다. 공유 경계는 공용 Hover 패널(필드 설명 호버와 공유)과 정보창 숨김 수명이다.

### 현강님 공유 메모 (미전달)

- 바꾼 현강님 코드: `CombatUnitInfo.cs`(`BindUIService` 재정의 추가), `CombatUnitInfoView.cs`(특성 칸 설명 텍스트 참조 삭제, 특성 칸 Button 호버 → 공용 Hover 패널, `ResetHover`·`LateUpdate`·`Show`에 호버 정리·갱신). TextNoticeUI·UITextPresentation·UISystem은 바꾸지 않았다.
- 제목 없는 Hover를 위해 `ShowHover(string.Empty, 설명)`을 쓴다. 제목 줄 자리를 없애려면 Hover 패널 쪽 배치 변경이 필요하다(요청 아님, 참고).

### 검증

- **컴파일 미확인.** Test 코드 작성·MSBuild·Unity 실행은 하지 않았다.
- Unity 확인: ① 아군 선택 → 특성 칸에 호버하면 제목 없이 설명만 뜨고 나가면 닫힘 ② UnitInfo 칸에는 이름·아이콘만(TraitDesc는 프리팹 정리 뒤) ③ 지형·오브젝트 설명이 떠 있는 칸에서 특성 칸으로 곧장 옮겨도 특성 설명이 뜸, 특성 칸에서 필드로 나가면 필드 설명이 다시 뜸 ④ 호버 중 다른 유닛·구조물 선택, 슬라이드 퇴장, 국면 전환, 씬 이동에서 잔상 없음 ⑤ 특성 3개 유닛(Ability2 비활성)과 설명 없는 특성 ⑥ 패널이 화면 아래·왼쪽 끝에서 잘리지 않는지, 이름을 가리는 정도.

## 2026-11-02 특성 3개 수신·Ability2 비활성 유지

- UnitInfo 고정 슬롯을 3개로 늘리고 사용자가 추가한 DeckUI/UnitInfo의 Ability2에 세 번째 특성의 Name·TraitDesc·AbilIcon 참조를 연결했다. 세 번째 특성도 기존 표시값 경로로 받아 갱신하며, Ability2의 프리팹 비활성 상태는 그대로 유지한다. 특성이 없거나 구조물 선택·Clear 시에는 세 번째 슬롯도 함께 비운다.
- 표시 내용의 원본은 기존 Buffer/View DB, 슬롯 활성 상태의 원본은 프리팹이다(SSOT). 전용 View의 슬롯 수와 프리팹 참조만 변경하며 공용 위젯·입력·서비스 생성/등록/Export/조회·UIId/부모/Policy·열기/닫기·재사용/해제 수명은 유지한다(SOLID·FW·기능 독립성).
- **컴파일 미확인.** Test 코드 작성·MSBuild·Unity 실행은 하지 않았다. Unity에서 특성 3개 유닛 선택 시 오류 없이 세 번째 이름·설명·아이콘이 들어오는지, Ability2는 계속 비활성인지, 특성 수가 적은 유닛/구조물로 전환 시 이전 내용이 비워지는지 확인한다.

## 2026-11-02 Ability Name·Description·Icon 직접 연결

- 사용자 후속 지시로 각 고정 Ability 슬롯의 Name·TraitDesc(Description)·AbilIcon에 기존 특성 표시값의 이름·설명·아이콘을 넣는다. 특성이 없는 슬롯과 구조물 선택·Clear에서는 두 텍스트와 Sprite를 비우고 Image 표시를 끈다. 아이콘이 없는 특성도 Image만 끄며 텍스트는 표시한다. 슬롯 오브젝트 활성 상태·RectTransform은 변경하지 않는다.
- UnitInfo에서 공용 Ability 버튼·호버 설명창 연결을 제거하고, DeckUI 프리팹의 두 슬롯에 있는 기존 TMP·Image 참조만 연결했다. 공용 UnitAbilitySlot·UnitAbilityTooltip과 다른 화면의 동작은 수정하지 않는다. 상태 아이콘의 호버 입력·정보창 UI 등록·수명은 유지한다.
- **컴파일 미확인.** Unity에서 선택 변경 시 특성 이름·설명·아이콘 갱신과 없는 슬롯의 표시 초기화를 확인한다. 아래 Ability 호버 유지 기록은 이 후속 지시로 대체한다.

## 2026-11-02 Ability 프리팹 배치 유지

- 사용자 지시로 UnitInfo의 특성 설명창 위치·pivot 강제 적용을 제거했다. 특성 슬롯과 설명창의 위치·크기·간격은 프리팹 값을 사용한다. UnitInfo는 특성 내용과 표시·호버 입력만 갱신하며, 설명창용 TooltipAnchor 연결을 요구하지 않는다.
- 상태 아이콘 수에 따른 UnitInfo 전체 높이·Y 위치 보정도 제거했다. 상태 아이콘 영역 내부의 줄 배치만 유지하여 Ability와 부모 패널의 배치에 영향을 주지 않는다. 기존 정보창 열기·닫기 연출과 HP·보호막 표시는 유지한다.
- 프리팹이 배치의 원본이며 기존 Buffer/View DB가 표시 내용의 원본이다(SSOT). 변경은 전용 View의 표현에 한정하고 공용 특성 위젯·입력 계약·UIId·부모·Policy·서비스 조립을 바꾸지 않는다(SOLID·FW·기능 독립성).
- **컴파일 미확인.** 빌드·시험은 실행하지 않았다. Unity에서 특성 호버 표시·해제와 상태 아이콘 갱신 전후 프리팹 배치 유지를 확인한다.

## 2026-11-02 보호막 누적 Fill — UnitInfo Phase

- 선행 확인: 사용자가 HUD Phase의 보호막 표시가 잘 동작한다고 보고한 뒤 UnitInfo에 같은 방식 적용을 요청했다. HUD 재생 확인을 UnitInfo의 검증 완료로 간주하지 않는다.
- 설계·Task: (1) 기존 정보값에 같은 Buffer 사실의 보호막 잔량 전달 (2) 전용 View의 Shield 참조·비율·Clear/Hide 수명 추가 (3) DeckUI의 Shield Image 조립 (4) 참조·변경 범위 정적 검사와 Unity 확인 인계. 이 단일 Phase에서 멈춘다.
- 확정 표현: H=현재 HP, M=최대 HP, S=유효 보호막, D=Max(M, H+S). HP Fill은 H/D, Shield Fill은 (H+S)/D로 왼쪽부터 채우고 Shield → SubFill → Fill 순서로 겹친다. D=0이면 두 비율은 0, S=0이면 Shield Image를 비활성화한다. 앞선 아래의 보호막 구간 배치 제안은 이 누적 Fill 설계로 대체한다.
- 데이터: CombatInspectionInfo가 기존 CombatInspectionFacts.Character의 TraitEffects.ActiveShieldTotal을 읽어 Shield로 전달하며 구조물은 0이다. HP와 같은 불변 사실의 표시값이며 출처별 합산·유효성 판정·Core 조회/쓰기를 추가하지 않는다. ShowCombat의 마지막 선택 인자는 shield=0으로 두어 기존 호출도 유효하다.
- 연출·수명: HUD와 같이 합계 분모는 double로 계산한다. 같은 분모의 피해에는 기존 HealthFillAnimation의 Fill → SubFill 감소를 유지하고 최초 표시·최대 HP/분모 변경·회복·대상 변경·숨김 후 재표시는 즉시 맞춘다. Shield Fill은 확정 비율을 즉시 반영한다. Clear는 세 바와 보호막 표시 캐시를 초기화하고 Hide는 HP 연출을 완료한다. 새 보호막 숫자나 예상 소모 표시는 추가하지 않는다.
- 조립·검증: DeckUI/UnitInfo/HealthBar에 기존 HP바와 같은 크기의 회색 ShieldFill을 추가하고 필수 shieldFill 참조를 연결했다. 필수 참조·Sprite·가로 Filled·왼쪽 시작·부모/형제 순서 오류는 UIId.CombatUnitInfo와 필드가 포함된 예외로 등록 전 거절한다. 기존 HP 텍스트·위치·특성/상태 입력과 씬 연결을 유지한다.
- SSOT·SOLID·독립성: Buffer가 수치 원본, CombatInspectionInfo가 같은 사실의 표시 전달, 전용 View가 비율·연출 수명, 프리팹이 참조 조립을 맡는다(S/D). 공용 HealthFillAnimation을 수정하지 않고 조합한다(O). 기존 UIWidgetBase·ShowCombat 호출 계약을 유지하고(L), 좁은 표시값만 전달한다(I). 새로운 규칙·서비스·인터페이스·입력·이벤트를 만들지 않는다. 변경은 정보값·전용 View·DeckUI 조립에 한정하며 기존 HUD·선택·Core·다른 기능의 구현을 수정하지 않는다. 남는 공통 경계는 기존 HP 연출과 UI Hide 수명이다.
- FW·서비스 계층: GlobalSystemFactory 생성·Global 내부 IUISystemService 등록 → GlobalSystemGroup.UIServiceCommand → CombatDisplayServices.From → CombatSceneDisplayFactory → CombatUnitInfo.Configure 경로, UIId.CombatUnitInfo·부모 Deck·Exclusive 정책·BindScene 등록·OpenChildUI/CloseUI·씬 해제 수명을 유지한다. 추가 Image는 등록 화면 내부 위젯이므로 별도 등록·루트 Export·루트 직접 조회가 없다. FW 필수 계약과 프로젝트 계층 규약에 새 변경은 없다.
- 정적 확인: 직렬화 참조, 중복 fileID·객체 유실, 세 Image의 크기·부모·그림 순서·Sprite 설정, 표시값/호출부 연결과 공백 검사를 확인했다. **컴파일 미확인**. Test 작성·수정·실행, MSBuild·컴파일·재생은 수행하지 않았다.
- Unity 확인: HUD와 같은 유닛을 선택해 H=40/M=100/S=30일 때 HP 40%·Shield 70%, S=80일 때 HP 약 33.3%·Shield 100%인지 비교한다. 보호막 감소/소멸·회복·최대 HP 초과 경계, 피해 연출 중 대상 변경, 닫기/재열기, 보호막 없는 유닛·구조물 전환에서 잔상이 남지 않는지 확인한다. 커밋·push하지 않는다.

---

## 2026-11-02 HP 전환 연출 복원 — 단일 Phase

- 범위·설계: UnitInfo의 HP 표현만 HUD와 같은 `HealthFillAnimation`으로 복원한다. 같은 대상의 피해는 Fill → SubFill 순서로 감소하고 첫 표시·대상 변경(Clear)·최대 HP 변경·회복은 즉시 맞춘다. 현재/최대 HP 숫자는 실제 공개값을 즉시 표시한다. 공격 예상 표시·HUD 자체 동작·보호막 바 구현은 이번 범위에 없다.
- Task: (1) HUD 연출·UnitInfo 수명·Buffer 확인 (2) 전용 View에서 공용 연출 소비 (3) DeckUI 프리팹에 SubFill·Sprite·참조 조립 (4) 소스·직렬화 정적 검사 후 Phase 보고.
- 구현: HP 1%당 Fill 0.003초, SubFill 0.01초로 HUD 기본값을 따른다. FixedUpdate에서 진행하고 같은 목표의 반복 갱신은 재시작하지 않는다. Clear는 두 바를 0으로, Hide는 마지막 목표로 완료하고 재표시 때 즉시 맞춘다. 기존 Fill의 빈 Sprite도 HUD의 게이지 Sprite로 연결했다. SubFill은 같은 위치·크기의 노란색 Image를 Fill 뒤에 그린다. 필수 참조·Sprite·가로 Filled·형제 순서 오류는 등록 전 검증에서 예외로 보고한다.
- SSOT·SOLID: HP 사실은 기존 Buffer, 비율 감소 방식은 공용 HealthFillAnimation이 원본이다. View는 이전 최대 HP와 연출용 비율만 보관하고 규칙을 재계산하지 않는다. 새 추상화·인터페이스·서비스는 없다. 기존 UIWidgetBase 수명과 좁은 ShowCombat 호출 계약을 유지한다(LSP/ISP). 조립은 프리팹, 표시·틱 수명은 CombatUnitInfoView, 감소 알고리즘은 기존 공용 객체가 담당한다(SRP/DIP). 공용 알고리즘 수정 없이 기존 객체를 조합한다(OCP).
- FW·서비스 계층: GlobalSystemFactory가 생성·Global 내부에 등록한 UISystem → GlobalSystemGroup.UIServiceCommand → CombatDisplayServices.From → CombatSceneDisplayFactory → CombatUnitInfo.Configure 경로를 유지한다. UIId.CombatUnitInfo·부모 Deck·Exclusive 정책, BindScene 등록·OpenChildUI/CloseUI·씬 해제 수명을 변경하지 않는다. 추가된 바는 등록 화면 내부 위젯이므로 별도 등록·루트 Export·루트 직접 조회가 없다. FW 필수 계약과 프로젝트의 서비스 계층 규약에 새 변경은 없다.
- 독립성: 복원 전후 모두 수치·선택·입력·재생 완료 후 갱신 계약은 같다. HP 표현 수정은 전용 View와 DeckUI의 참조 조립에 한정하며 Core·공용 HUD·Factory·선택 로직·다른 정보 위젯의 코드를 수정하지 않았다. 남는 공통 경계는 기존 HealthFillAnimation과 UIWidgetBase의 Hide 수명이다.
- 보호막 조사: `ICharacterBufferReadSink.TryGet`의 `CharacterData.StatusEffectSnapshots`에 `ShieldEffectSnapshot.RemainingEligibleBudget`이 공개돼 있다. Core의 `StatusEffectReadModel.Capture`가 유효 잔량(`TraitEffects.ActiveShieldTotal`)을 투영하므로 View에서 출처별 합산·유효성 판정을 다시 할 필요가 없다. 현재 UnitInfo의 CombatInspectionInfo에는 별도 보호막 수치가 없어, 구현 시 기존 정보 변환 경계에서 전달하고 프리팹에 보호막 구간을 조립해야 한다. Core 변경은 필요 없다.
- 보호막 표시 제안(미구현): H=현재 HP, M=최대 HP, S=공개된 유효 보호막 잔량일 때 D=Max(M, H+S), 체력 폭 H/D, 보호막 폭 S/D로 두고 보호막 구간은 체력 끝에서 시작한다. H+S가 M을 넘을 때 사용자가 제시한 H/(H+S), S/(H+S)가 된다. D=0이면 두 폭은 0이다. 이하 구간에서는 최대 HP 기준 빈 공간을 유지한다.
- 검증: git diff 공백 검사와 프리팹 fileID·참조·형제 순서·Sprite·Image 설정을 정적으로 확인했다. Test 코드 작성·MSBuild·컴파일·재생은 수행하지 않았다. **컴파일 미확인**. Unity에서 최초 표시, 같은 대상 피해/연속 피해/회복, 최대 HP 변경, 피해 연출 중 대상 전환·닫기/재열기, 구조물 HP 갱신을 확인한다. 커밋·push하지 않았다.

---

> 2026-11-01: 적 본부도 정보 선택 대상에 추가했다. 표시 항목·선택/해제 수명·프리팹 연결·규약 검토·검증 결과의 원본은 [선택 HUD 안내의 적 본부 정보 선택 절](../Unit/UnitHUD-info-panels.md#적-본부-정보-선택-2026-11-01)이다. HUD의 행동 슬롯은 같은 안내의 별도 11-01 기록대로 제거됐다.

> R4 current status (2026-10-29): Combat action prices have been removed. CombatUnitInfo hides the retained legacy move/attack cost widget roots and no longer requires those widget references to configure. Unit cards retain HP, ATK, DEF and level; category icons, unit HUD action-use indicators and existing selection/transition lifetimes remain. Core action availability, aim/range facts and input/pending/playback gates remain authoritative. References below to Stats.MoveCost, Stats.AttackCost, numeric AP rendering or the cost-only availability display callback describe the former implementation. Source migration is delivered; native and manual/PlayMode acceptance must be reported separately.

---

The original implementation notes below are historical where they conflict with the R4 status above.

## 2026-11-01 UnitInfo 텍스트 분리 — 단일 Phase 완료

- 요청·Task: 기존 MaxHP 텍스트는 `/ {MaxHP}`, `HealthBar/CurrHP`는 현재 HP 값만 표시한다. Level은 값만 표시하고 Category/Txt와 각 AbilIcon/Name을 연결한다. 프리팹 참조 연결 → 표시·Clear·복제 처리 → 컴파일·직렬화 참조 확인으로 진행했다.
- 병종은 Buffer의 Category.Key를 기존 CANNON 예시처럼 대문자로 표시한다. 특성 이름은 툴팁과 동일한 UnitCardTraitContent.Name을 사용한다. 같은 선택 개체의 갱신·대상 변경·Clear 수명에서 새 텍스트도 함께 갱신·초기화한다.
- UnitAbilitySlot은 이름 참조·표시·복제를 소유하고 CombatUnitInfoView에서만 필수로 검증한다. 이름이 없는 기존 공용 카드의 슬롯 구성은 유지한다. DeckUI에 있던 세 번째 슬롯도 등록해 미사용 슬롯의 예시 문구가 남지 않게 했다.
- SSOT·SOLID: HP·레벨·병종은 기존 Buffer, 특성 이름은 기존 View 표시 데이터가 원본이다. 새 상태·서비스·상속 계층·입력 연결을 추가하지 않는다. 프리팹은 참조 조립, View와 공용 슬롯은 표현을 맡는다. 공통 변경 경계는 UnitAbilitySlot의 선택적 이름 참조이며 기존 카드 호출은 그대로 유효하다. Core·다른 기능의 규칙/입력/선택 코드는 수정하지 않았다.
- FW·UI 수명: 기존 GlobalSystemFactory의 UISystem 생성·Global 내부 등록 → GlobalSystemGroup.UIServiceCommand → CombatDisplayServices.From → CombatSceneDisplayFactory → CombatUnitInfo.Configure 경로를 유지한다. UIId.CombatUnitInfo, 부모 Deck, Exclusive 정책과 BindScene 등록·OpenChildUI/CloseUI·씬 해제 수명은 그대로다. 새 텍스트는 등록 화면 내부 위젯이며 별도 UI 등록·루트 Export·루트 조회가 없다. 필수 참조·자식 계층은 표시 상태 변경 전에 예외로 검증한다.
- 검증: 생성된 csproj의 소스·참조와 현재 Library 의존 DLL을 사용한 Roslyn C# 컴파일 오류 0. SerializeField의 CS0649 경고가 출력됐다. 프리팹 fileID 중복 없음, HP/병종/레벨 TMP 참조와 특성 슬롯 3개의 버튼·이름·복제 기준점 계층을 정적으로 확인했다. Unity 에디터 컴파일·EditMode 실행·Play 화면 확인은 미실행이다. 커밋·push하지 않았다.

# Combat UnitInfo 변경 기록 — 2026-10-18

> **현재 기준 — 2026-10-26 V5-d:** 아래 날짜별 설계/검증은 당시 이력이다. 현재 선택 원본은 Display 소유 `Selection/CombatSelectionState`이며 UnitInfo는 `ICombatSelectionReader`를 빌린다. 같은 필드 유닛 재클릭은 선택 유지, 빈 칸은 해제, 적은 정보만 표시한다. 같은 국면의 턴/일반 결과는 유효 선택을 유지하고 죽음/필드 이탈/국면 변경·Display Clear/Release/새 Install이 정리한다. UI의 Hide/Clear는 선택을 쓰지 않는다. 비용/행동 사용은 Core 가용성 응답을 표시한다. 4라운드 V4-n 및 5라운드 a/b/c 각각 사용자 컴파일·기존 시험·Unity 정상 보고로 인수했으며 상세 근거/확인 공백은 [5라운드 계획 §10](../../../../../../../../../../docs/superpowers/plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 둔다.

## 확정 설계

- 대상은 `Assets/Scenes/Combat.unity`의 `UnitInfo`다.
- 기본 표시 대상은 현재 턴 유닛이다. 필드 유닛 클릭으로 전환하며 같은 유닛 재클릭·빈 곳 클릭 시 현재 턴 유닛으로 돌아간다. 턴 변경 시 클릭 선택을 해제한다. 선택 대상이 사망·필드 이탈하면 현재 턴 유닛으로 돌아간다.
- 전투 페이즈 진입은 `CloseDeck`과 동일한 `UISystem.CloseUI(UnitCard)` 경로로 덱을 닫고 UnitInfo를 켠다. 배치 페이즈는 `OpenChildUI(Deck, UnitCard)`로 덱을 열고 UnitInfo를 끈다. 같은 페이즈의 갱신은 덱을 다시 열거나 닫지 않는다.
- Name·Portrait·HP·ATK·DEF·Ability는 기존 `UnitCardView`를 재사용한다. HP는 `CurrentHP / MaxHP`와 기존 Fill → SubFill 연출이다. 다른 개체 선택 시 HP를 즉시 맞추고 Ability 패널 고정을 해제한다.
- 공격력은 `Stats.EffectiveAttackPower`, 방어력은 `CharacterData.EffectivePhysicalDefense`, 비용은 `Stats.MoveCost`·`Stats.AttackCost`를 표시한다. 순간 조건부 공격 판정은 View에서 다시 계산하지 않는다.
- 병종 원본은 Buffer의 `Stats.Category`다. `Category/Image`는 View DB의 `CategoryVisualTable`에서 같은 키의 `WhiteIcon`을 조회한다. Sprite 미입력은 숨김 표시이며, 키 항목 누락은 구성 오류다.
- Ability는 UnitCard와 동일한 TraitVisual 조회·이름/설명 결합을 사용한다. 현재 씬은 버튼 아래 `AbilityPannel`에 이름·설명이 있고 별도 능력 아이콘 참조는 없다. UnitCard처럼 Hover 열기는 끄고 클릭 고정·재클릭 닫기·패널 클릭/Submit 닫기를 연결했다.

## Task와 구현 Phase

1. 기존 카드 표시·입력·덱 수명과 씬 구조 확인.
2. UnitInfo 선택·페이즈 전환 컴포넌트 작성, 완료된 전투 표시 값 전달, 기존 입력 경로에서 선택 전달.
3. 씬 참조·패널 닫기 버튼·수평 HP Fill 연결.
4. 운영 씬을 PreviewScene으로 열어 검증하는 회귀 테스트 3개 작성, 컴파일 확인.

위 항목을 하나의 구현 Phase로 완료했다. Unity 실행 검증은 아래와 같이 별도로 남긴다.

## 경계·설계 점검

- SSOT: 규칙·HP·비용·턴·병종 원본은 Buffer, 이름·초상화·능력 설명은 View DB다. 컴포넌트의 대상 ID와 이전 표시 값은 선택 상태·표시 캐시다.
- SOLID: UnitInfo 기능은 `CombatUnitInfo`, 카드 연출·패널 입력은 `UnitCardView`, 생성·연결은 Factory에 둔다. 입력 경로는 좁은 `ICombatUnitInspection`만 호출한다. 기존 카드 API·다른 표시 구현의 계약은 유지한다.
- FW 계약: 내부 협력 객체를 별도 서비스로 등록하지 않는다. 기존 씬 연결·완료된 재생의 Refresh·Clear/Release 수명을 따른다. 입력은 기존 InputSystem 경로와 `UIInputBinding`을 사용한다.
- 독립성: 텍스트·비용·병종 표시 수정은 UnitInfo와 해당 테스트에서 처리한다. 표시 추가 조립 때문에 `CombatSceneDisplayFactory`, `CombatDisplayServices`, `CombatSceneRoot`를 변경했다. `CombatViewSystem`은 선택 입력 전달만 추가했다. Core·FieldCamera·Architecture는 수정하지 않았다.
- 공용 조립 변경 메모: `CombatDisplayServices`에 기존 Global `UIServiceCommand` 참조를 추가해 UnitInfo에만 주입한다. 별도 서비스 키·수명·UI 등록은 추가하지 않았다.

## 검증

- VS MSBuild로 `ProjectL.Systems`와 `ProjectL.Tests.Framework` Compile 성공. 기존 경고만 출력됐으며 새 파일 경고는 없다. 산출물은 저장소 밖 시각화 작업 폴더에 생성했다.
- 추가 테스트 `CombatUnitInfoTests` 3개: 선택 유지/토글/빈 곳/같은 행위자의 다음 턴, 페이즈별 덱 전환 횟수와 실제 유효값/비용, EventSystem 클릭·Submit과 대상 변경 시 패널/HP 초기화.
- **Unity EditMode 테스트 실행·Play 확인은 미완료다.** 컴파일 통과를 실행 통과로 간주하지 않는다. Editor가 새 스크립트를 반영한 뒤 위 테스트와 실제 배치 → 전투 → 배치 전환을 확인한다.
- 기존 씬 편집(AbilityPannel 이름·비용 부모 변경·활성 상태 등)은 보존했다. 커밋·push하지 않았다.

## 규약 리뷰 후속 — 2026-10-18, 단일 Phase

### 설계·Task

- 사용자 처리: 아이콘과 버튼 Image 분리(리뷰 1번). 이번 Phase는 씬·프리팹을 편집하지 않는다.
- Task 1(리뷰 2번): 클릭 선택 유무와 실제 표시 대상 ID를 구분한다. 선택이 없어서 행위자로 돌아가더라도 최종 표시 대상이 같으면 카드를 유지한다. 실제 대상 변경·표시 대상 소멸·전체 Clear 때만 초기화한다.
- Task 2(리뷰 3번): 카드 공통 `UnitCardTraitContent`가 특성의 View DB 조회, 아이콘 선택, 이름·설명 결합과 줄바꿈 변환을 맡는다. 전투 상태 특성과 보유 카드 특성 키는 각각의 Create 입구로 받고, 결합 규칙은 Append 한 곳에 둔다. 기존 순서·누락 항목 처리·문구는 유지한다.
- Task 3: 기본 행위자 표시와 직접 선택 상태에서 같은 개체의 HP 갱신·능력 패널 고정·같은 행위자의 다음 턴을 확인하는 회귀 테스트를 추가한다. 공통 변환에는 두 입력 형식의 동일 결과·누락 항목·구분자·줄바꿈·빈 입력 검증을 추가한다.

### 경계·독립성 점검

- SSOT: Buffer와 View DB를 원본으로 유지하고, 특성 조합 정책만 공통 변환에 모았다. `_selected`는 클릭 선택, `_shown`은 실제 표시 대상이므로 서로 다른 사실이다.
- SOLID: 선택은 CombatUnitInfo, 카드 연출·입력은 UnitCardView, 특성 변환은 UnitCardTraitContent가 소유한다. 두 호출부는 기존의 좁은 Trait Query만 전달한다. 상속·서비스·새 인터페이스는 추가하지 않았다(LSP에 영향 없음).
- FW: 서비스 등록·입력 Route·UI 이력·조립과 수명 계약은 변경하지 않았다. 내부 변환을 Framework 서비스로 등록하지 않는다.
- 변경 시나리오: 이전에는 특성 구분자·줄바꿈 정책 변경에 UnitCardUI와 CombatUnitInfo를 각각 수정해야 했다. 이제 UnitCardTraitContent와 공통 변환 테스트에서 처리한다. 공통 출력 정책을 바꾸면 덱·전투 양쪽의 표시 확인은 필요하지만, 각 화면의 선택·입력·씬 조립은 수정하지 않는다. 선택 초기화 수정은 CombatUnitInfo와 해당 회귀 테스트에 한정한다.

### 결과·검증

- Task 1~3 코드 완료. `ProjectL.Systems`와 `ProjectL.Tests.Framework`를 VS MSBuild Compile로 확인했다(오류 0, 변경 파일 경고 없음). 신규 파일은 임시 targets로만 포함했고 산출물은 OS 임시 폴더에 뒀다.
- **Unity EditMode 실행과 Play 확인은 아직 미완료다.** 현재 실행 중인 Editor에서 `CombatUnitInfoTests`와 `UnitCardViewTests`를 실행하고, 같은 유닛의 피격 후 HP 연출·능력 패널 유지 및 다른 유닛 선택 시 초기화를 확인한다.
- 이 Phase 완료 지점에서 멈춘다. 커밋·push하지 않는다.

### 재점검 후 정리

- 사용자 지시로 공통 특성 변환 테스트와 전용 fixture를 삭제했다. 별도 테스트 클래스로 분리하지 않는다. 위 Task 3의 공통 변환 테스트 추가는 이전 이력이며 현재는 선택·HP·패널 회귀 테스트만 유지한다.
- 사용자 수정 씬에서 패널 내부 아이콘의 자식이 비어 있고, 능력 이름·설명이 패널의 직접 자식으로 분리된 것을 확인했다. 아이콘 비활성화가 이름·설명을 함께 숨기던 참조 문제는 해소됐다(씬 정적 확인).

## 2026-10-19 전투 변동 능력치 표시 — Phase 1

### 설계·Task

1. 기존 공개 Core 계산을 Buffer의 불변 표시 데이터에서 재사용한다.
2. UnitInfo와 전투 CharacterData를 사용하는 공용 카드가 같은 표시값을 소비한다.
3. 효과 반영·해제·불변 스냅숏·같은 선택 유닛 갱신을 검증하고 이 Phase에서 보고한다.

공격력·우선도는 공개 CombatEffectiveStatsCalculator의 AttackPower/Priority로 제공할 수 있다. 할인된 공격 비용은 현재 공개 금액 계약이 없어 Phase 2의 Core 인수 조건이다. 그 요청은 docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md §25에 기록했다(미전달). Core 파일은 변경하지 않는다.

### 구현

- CharacterData에 get-only EffectiveAttackPower/EffectivePriority를 추가했다. 기존 EffectivePhysicalDefense처럼 생성 당시의 같은 불변 CharacterCombatFacts에서 Core 공개 함수를 호출한다. 기존 Stats의 의미·값은 변경하지 않고, 별도의 가변 능력치 저장소나 계산식을 View에 만들지 않는다. 공격력은 피해 전용 배율을 포함하지 않는 Core 표시 공격력이다.
- CombatUnitInfo는 공용 UnitCardView.ShowDetails(CharacterData)로 전달한다. 공용 카드와 UnitCardUI의 전투 캐릭터 분기도 같은 Effective 표시값을 사용한다. 비전투 보유 카드 조회는 변경하지 않는다.
- 결과 재생 완료 → CombatSceneDisplay.Refresh → CombatUnitInfo.Render의 기존 갱신 시점, 같은 선택·패널 고정·HP 연출 수명은 유지한다. Core 미확인 상태를 UI Update로 먼저 끌어와 연출보다 앞서 표시하지 않는다.
- 공격 비용은 현재 Stats.AttackCost, MAX COST는 MaxActionCost라는 기존 의미를 유지한다. 전자를 실제 할인 후 비용으로 반영하는 작업은 미완료다. 정보창을 위해 공격 입력이 소유한 Preview 요청을 덮어쓰지 않는다.

### 규약·독립성 재검사

- SSOT: 능력치 규칙은 Core 함수 한 곳이다. Buffer의 get-only 표시값은 동일 불변 사실의 투영이며 독립적인 런타임 상태/동기화 소유자가 아니다. HeldEnvironment/Debuffs/Items 변경 비교가 이미 CharacterCombatFactsComparer에 있어 원본 변경 시 새 CharacterData가 발행된다.
- SOLID: 생성·투영은 기존 CharacterData 경계, 표시 선택은 CombatUnitInfo, 공용 숫자 표시는 UnitCardView가 맡는다. 공격력 계산 규칙 변경은 Core 계산기에 머물며 화면별 계산 분기를 복제하지 않는다. 새 서비스·인터페이스·Factory·UI 등록·루트 조회·입력 연결은 없다(FW-001~009 관련 변경 없음).
- Core 소유 영역 수정 없이 공개 Application 계약만 Buffer 경계에서 사용했다. View는 Buffer 공개값만 읽는다. 기존 미등록 UI 전체 재구성은 이번 범위에 포함하지 않는다.

### 검증·남은 일

- 게임 코드와 CharacterDisplayStatsTests/CombatUnitInfoTests/UnitCardViewTests 및 보조 코드 컴파일 성공. 기존 경고만 확인했다.
- 관리 코드 실행 2건 통과: 실제 Buffer 발행에서 효과 반영·제거·이전 스냅숏 불변 확인, 중첩 우선도 패널티의 Core 하한 확인.
- 같은 선택 개체의 효과 획득/해제에 따른 공격력·우선도 화면 갱신 테스트를 추가했다. Unity 네이티브 객체/씬을 사용하는 EditMode 실행·Play 확인은 미실행이다.
- Phase 1 코드 완료. Phase 2는 Core의 현재 공격 비용 읽기 계약 제공 뒤에 연결한다. 커밋·push하지 않는다.

## Category 아이콘 연결 — 2026-10-19

- 입력 원본: ViewGroup/CategoryVisual의 키별 SO와 CategoryVisualDataTable. 기존 Category/Image 참조를 재사용하고 SO의 WhiteIcon만 읽는다. 입력 중 Sprite null은 허용하며 다른 종류의 아이콘으로 대신 표시하지 않는다.
- 조립·등록 공유 메모(미전달): ViewDataBaseFactory가 CategoryVisualTable을 생성하고 View DB 내부 Builder에 ICategoryVisualTableQuery를 한 번 등록한다. ViewDataBase가 ViewTableKey.CategoryVisual로 보관하고 IViewQuery.CategoryVisualTableQuery로 제공한다. 상위에는 기존 IViewQuery만 Export한다. CombatDisplayTables.From → CombatSceneDisplayFactory → CombatUnitInfo.Configure로 필요한 조회만 주입한다. 루트에 개별 테이블을 재등록하거나 UnitInfo에서 resolver/씬 검색을 하지 않는다(FW-001/003/004 준수).
- SSOT·독립성: 병종 규칙/키는 Buffer, 아이콘은 CategoryVisual 표가 원본이다. 개체 선택·턴·재생 완료 후 갱신·Clear 수명은 유지한다. 신규 화면/입력/이벤트/UIId는 추가하지 않는다. 일반 아이콘 교체는 SO만 수정하며 Core·다른 UI 표시 구현은 수정하지 않는다.
- 검증: 실제 Category 키 전달, Sprite 반영, null Sprite에서 이미지 비활성화를 기존 UnitInfo 회귀에 추가했다. 컴파일 결과는 세션 보고를 따른다. Unity EditMode의 CombatUnitInfoTests·DataBaseAssetTests 실제 실행과 Play 확인은 미실행이다.

## 2026-10-20 Category·Cost 호버 설명 — 단일 Phase

- 요청: Category/Image 호버 동안 CategoryPanel, Cost/Img 호버 동안 CostNotify를 표시한다. 이탈 시 닫고 둘 다 Exclusive로 등록한다. 클릭·Submit은 고정 표시를 만들지 않는다.
- Task: 기존 씬 버튼·UISystem 수명 확인 → CombatInfoHoverUI와 UIId 29/30 추가 → 씬 패널·버튼 연결 → 컴파일·씬 정적 검증. 이 Phase의 구현은 완료했다.
- 등록 단위: CategoryPanel=CombatCategoryInfo(29), CostNotify=CombatCostInfo(30). 부모는 None, UIBase 기본 Exclusive 정책이다. UISystem.BindScene이 비활성 패널도 수집·등록하고 서비스 바인딩 중 UIInputBinding을 버튼에 한 번 붙인다. 외부 열기·닫기는 IUISystemService.OpenUI/CloseUI, 실제 활성 변경은 각 UI의 Open/Hide가 맡는다.
- 수명: 두 패널은 초기 비활성 상태이며 같은 씬 인스턴스를 재사용한다. 기존 UIInputBinding이 포인터 이탈·버튼 비활성·재바인딩·interactable 상실 시 호버를 한 번 해제한다. UnitInfo 비활성화 시 자식 버튼도 비활성화되며 패널을 닫는다. 씬 전환은 기존 UISystem 등록 초기화와 씬 객체 해제를 따른다. CanvasGroup.blocksRaycasts=false로 설명 패널이 호버 입력을 가로채지 않는다.
- SSOT·SOLID·독립성: 열림 상태와 배타 처리는 UISystem 한 곳이 소유한다. 새 컴포넌트는 두 설명 패널의 입력·표시만 담당하고, 기존 CombatUnitInfo의 규칙 값·카드 표시·선택 로직은 수정하지 않는다. 씬은 의존성 조립, 컴포넌트는 호버 동작을 담당한다. 설명 문구·레이아웃 변경은 해당 씬 패널에서, 호버 동작 변경은 CombatInfoHoverUI와 해당 동작 확인에서 처리한다. 남는 공통 경계는 UIInputBinding·UISystem의 Exclusive 정책이다.
- FW·서비스 계층: 기존 Global Factory가 만든 UISystem과 씬 UI 등록 경로만 사용한다. 추가 Builder 등록·루트 Export·루트 resolver 직접 조회는 없다. UISystem이 BindUISystemService로 전달한 기존 서비스를 소비한다. 필수 UIId·버튼·CanvasGroup 구성은 등록 전에 예외로 검증한다. Core·FieldCamera·Architecture는 수정하지 않았다.
- 검증: 새 파일을 임시 targets로 포함해 ProjectL.Systems MSBuild Compile 성공(오류 0, 새 파일 경고 없음). 씬 fileID 중복 없음, 두 패널 초기 비활성·UIId·Button 참조·빈 UnityEvent를 확인했다. Unity EditMode 실행·Play 호버 확인은 미완료다. 실행 확인 시 EventSystem pointerEnter/pointerExit로 열기·닫기와 Exclusive 전환, 호버 중 UnitInfo/버튼 비활성화를 확인한다. 커밋·push하지 않았다.
