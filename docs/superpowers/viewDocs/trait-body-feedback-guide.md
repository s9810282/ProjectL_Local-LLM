# 특성 몸체 텍스트 사용법

문구·색·크기·Position Offset·대상·시간은 `Trait Visual Data > Activations > Body Text`에서, 폰트·머티리얼·배경·장식은 프리팹에서 편집한다. 런타임에는 프리팹을 생성하고 같은 프리팹별 풀에서 재사용한다. 텍스트는 발동 사건마다 한 번 시작해 유닛 몸체를 따라가며 사라진다. 보유 아이콘이나 HUD 재갱신만으로 다시 출력하지 않는다.

## 1. 외형 편집

기본 프리팹: `Assets/ProjectL/Prefabs/UI/CombatOnly/Feedback/TraitBodyText.prefab`.

| 편집 위치 | 바꾸는 내용 |
|---|---|
| `Label`의 TextMeshProUGUI | 폰트·정렬·머티리얼. 현재 SCDream7 SDF. 실제 표시 색·크기는 매 출력 시 Activation 값으로 적용 |
| `Content`의 RectTransform | 프리팹 공통 내부 배치. 발동마다 다른 위치는 Body Text의 Position Offset 사용 |
| `Content` 아래 자식 | 배경 Image·장식 추가. 모든 Graphic의 Raycast Target은 끈다 |
| 루트 CanvasGroup | 기본 알파. 실행 시 기본 알파에 페이드 비율을 곱한다. Interactable/Blocks Raycasts는 끈다 |
| 루트 CombatBodyTextView | Label과 루트 Fade Group 참조. 자식 이름을 바꿔도 참조가 유지되면 동작 |

루트는 RectTransform·CanvasGroup·CombatBodyTextView를 유지하고 Anchor Min/Max·Pivot을 모두 `(0.5, 0.5)`로 둔다. 별도 Canvas/GraphicRaycaster는 넣지 않는다. 텍스트 출력용 Overlay Canvas는 Factory가 소유 Scene의 독립 루트로 만든다. 실행 중 Label 문구는 발동 입력으로 바뀌므로 프리팹의 샘플 문구는 확인용이다.

공통 폰트·배경·장식을 바꾸려면 기본 프리팹을 편집한다. 특정 발동의 폰트·장식 구조를 다르게 하려면 Prefab Variant를 만들고 해당 줄의 `Body Text > Prefab Override`에 연결한다. 서로 다른 프리팹은 풀을 공유하지 않는다. Scene 객체 대신 프리팹 에셋을 선택한다.

## 2. 공통 설정과 발동 입력

`CombatPresentationSettings_Default`에 기본 `Body Text Prefab`이 연결돼 있다. `Body Text Stack Gap`은 같은 유닛에 동시 출력되는 문구의 간격이며 현재 28이다. 줄별 Override가 비어 있으면 공통 프리팹을 사용한다. 해당 줄에서 실제 사용하는 프리팹만 필수 검증한다.

`Trait Visual Data > Activations > Body Text`를 펼쳐 Enabled를 켠다.

| 항목 | 사용법 |
|---|---|
| Text Source | TraitDisplayName은 SO의 Display Name. 직접 쓰려면 Override 선택 |
| Text | Override 모드에서 표시되는 여러 줄 입력. 공백 문구는 허용하지 않음 |
| Color | 발동별 글자색·알파. 기본 흰색 |
| Font Size | 발동별 글자 크기. 기본28, 유한한 양수. 실제 인스턴스의 TMP Auto Size는 끔 |
| Position Offset | 몸 중앙 기준 X/Y 보정. +X 오른쪽/+Y 위쪽, 기본(0,0). CanvasScaler 기준1920×1080 좌표 단위 |
| Target | Source는 발동 유닛, Targets는 대상별 출력, All은 발동 유닛과 대상 전체에 출력. 같은 유닛은 한 번만 표시 |
| Count | 기본 None. CompletedMovementCells는 현재 가속 AttackGranted에서 지원 |
| Prefab Override | 비우면 공통 Body Text Prefab. 개별 외형은 프리팹/Variant 선택 |
| Hold / Fade Seconds | 유지 시간과 사라지는 시간. 대표 세 줄은 0.25초 유지·0.6초 페이드 |

3차 변경으로 Color/Font Size/Position Offset을 Activation에 둔다. 예를 들어 Text Source=Override, Text=가속 발동, Color=노랑, Font Size=36, Position Offset=(0,40)이면 해당 발동만 노란36크기 문구를 몸 중심보다 위에 표시한다. Count가 CompletedMovementCells이고2칸 이동했다면 `가속 발동 X 2`가 된다. 오프셋은 CanvasScaler를 따라 스케일되며 월드 거리나 물리 화면 고정px가 아니다. 위치는 매 투영에 절대값으로 넣어 누적하지 않는다. Prefab Override는 그대로 유지한다. Count를 설정한 뒤 Kind를 바꿔 기존 값이 지원되지 않게 되어도 Count를 None으로 다시 선택할 수 있다. Cue Timing도 같은 화면에서 ActivationStart로 복구할 수 있다. Inspector를 그리는 것만으로 값을 초기화하지 않는다.

Cue Timing은 이 줄의 텍스트·Effects·Shake가 시작할 사건이다. Anchor는 기존처럼 Start를 사용한다.

- ActivationStart: 발동 순간. 실제 희생 가로채기에서는 수호자 나가기 시작.
- StateApplied: 대응 상태를 반영하고 HUD를 갱신한 직후. 현재 가속·깔끔한 뒷정리의 AttackGranted 및 사냥꾼의 표식·정찰 부대 StatusApplied 지원. 같은 줄의 행동/공격 덮어쓰기는 비운다.

`Target = All`에서도 StateApplied 시점을 따른다. 대상은 각자의 상태가 반영된 뒤 표시한다. 발동 유닛도 대상 목록에 있으면 자신의 상태 반영 뒤 한 번만 표시하고, 대상 목록에 없으면 실제로 첫 대응 상태가 반영된 뒤 한 번 표시한다. 대응 상태가 없으면 기존 구성 진단을 유지하며, 발동 순간으로 시점을 바꾸지 않는다.

Effects에 설정한 지연은 선택한 시작점 이후에 적용되고 텍스트는 즉시 시작한다. 텍스트는 몸을 따라가지만 기존 파티클은 시작 위치에 놓인다. 유지/페이드 중에도 다음 사건은 진행한다. 공유 HUD Buff/Debuff의 Description Panel/Text와는 별개이며 설명 팝업은 후속 보류다.

## 3. 이미 연결된 특성

| SO | Kind | Cue Timing | Target / Count | 결과 |
|---|---|---|---|---|
| TraitVisual_CleanSweep | AttackGranted | StateApplied | Source / None | 버프 HUD 갱신 직후 깔끔한 뒷정리 |
| TraitVisual_Acceleration | AttackGranted | StateApplied | Source / CompletedMovementCells | 이동 완료 후 가속 X N |
| TraitVisual_Sacrifice | DamageRedirected | ActivationStart | Source / None | 수호자 나가기 시작에 희생 |
| TraitVisual_HuntersMark | StatusApplied | StateApplied | Targets / None | 약점 노출 반영 직후 대상 몸체에 사냥꾼의 표식 |
| TraitVisual_ReconSquad | StatusApplied | StateApplied | Targets / None | 해당 이동 출처 부여 반영 직후 대상 몸체에 정찰 부대 |
| TraitVisual_PositionSwap | MovementChanged | ActivationStart | Source / None | 종료점 교환 사실 뒤 주체 몸체에 위치 교환 |

위 줄은 공통 프리팹을 사용하도록 연결돼 있으며 추가 수동 연결은 필요하지 않다. 가속의 N은 이번 실제 자발 이동 칸 수다. 공격력 증가량 Magnitude와 다르다. 2칸 이동·공격력 증가 10이면 `가속 X 2`다. 미진입 칸·강제 이동·복귀·희생 행동은 세지 않는다. 이동 근거를 확정할 수 없으면 임의의 수치를 만들지 않고 이름만 표시하며 진단한다.

## 4. 조립 도구 확인과 사용자 Play 목록

1. 조립 모드에서 `Scenario_Acceleration`을 불러온다. 같은 Packet의 Movement → Activation → State, 2칸·증가량 10에서 `가속 X 2`와 HUD 시작 시점을 확인한다.
2. `Scenario_CleanSweep`: 선행 아군 공격 뒤 특성 보유자 HUD와 문구가 같이 시작하는지 확인한다.
3. `Scenario_Sacrifice`와 사망 버전: 수호자 나가기 시작·이동 중 추적·복귀/사망 후 회수, 보호 대상 중복 없음.
4. 기본 프리팹과 다른 Variant를 섞어 외형 유지·쌓기·페이드·재사용을 확인한다. Content 위치 보정과 여러 해상도/화면비도 확인한다.
5. Pause·카메라 이동/Shake·화면 밖/복귀·다음 Packet·Stop/Reset/자동 반복·씬 전환 후 잔여 문구와 Canvas가 남지 않는지 확인한다. 첫 투영 전 화면 원점에 문구가 노출되지 않아야 한다.
6. `Target = All`에서 발동 유닛+여러 대상 출력, 자기 자신도 대상일 때 중복 없음, 대상이 없을 때 ActivationStart의 발동 유닛 출력, StateApplied의 대상별 반영 시점과 발동 유닛 1회 출력, 다음 결과·Reset 후 다시 표시되는지를 확인한다. All 추가 작업은 정적 대조만 완료했으며 컴파일·Unity 재생은 미확인이다.

`구성 확인`은 사건 대응과 선택된 Override 구성을 검사한다. 공통 설정까지 포함한 실제 사용 프리팹 검증은 `환경 열기 / 재생`의 사전 검사에서 수행한다. StateApplied에 대응 State가 없거나 순서가 앞서거나 Packet이 다르면 진단한다. 가속 Count에 앞선 이동이 없어도 진단한다. 단독 Moment는 상태 연계 재현에 충분하지 않으므로 조립 모드를 사용한다. 버프/이동 사건을 자동 생성하지 않는다.

시연에 연결된 프리팹·Variant 또는 관련 폰트/머티리얼이 바뀌면 안전한 갱신 시점에 시연을 중단한다. 외형을 편집한 뒤 다시 재생한다. Stop/Reset/환경 종료는 남은 문구를 회수하고, 카메라 밖 문구는 숨기며 추적하던 몸이 사라지면 반환한다.

문구가 보이지 않으면 콘솔의 대상 프리팹/필드 오류, 출력 카메라와 `CombatBodyTextCanvas`의 Display, Label 폰트·Body Text Color 알파·Font Size·Position Offset·Content 배치를 확인한다. 공유 HUD 필수 참조 오류가 있으면 해당 HUD 작업과 분리해서 확인한다.

몸체 프리팹 F~J 작업은 MSBuild 런타임·Editor 컴파일과 코드/에셋 정적 검증까지 완료했다. Unity의 실제 임포트·Play·가독성·Profiler는 사용자 후속이며 위 목록은 성공 기록이 아니다. 조립 도구의 합성 사건 확인과 실제 인게임 Core 발동 확인도 구분한다.

## 5. 구형 에셋 병합 시 이전

현재3차 입력은 color/fontSize/positionOffset이며 문구는 기존 textSource/overrideText를 사용한다. 2차 프리팹 전용 에셋은 선택된 프리팹의 실제 색/크기를 기록해 Activation으로 옮기고 위치는 기존 Content 배치를 유지한 채(0,0)부터 시작한다. 새 기본값은 흰색/28/(0,0)이므로 맞춤 Variant를 쓴 기존 줄은 값을 확인해 이전한다. 1차의 screenOffset은 현재 positionOffset의 좌표 단위를 확인한 뒤 명시 이전한다. 숨은 런타임 우선순위/fallback은 두지 않는다. 폰트는 기본/Override 프리팹의 Label에 두고 공통 Body Text Font 필드는 복원하지 않는다. Kind·Cue Timing·문구·대상·Count·시간·Effects·Shake·UnitActions·GUID는 유지한다.


## 6. canonical 효과 연동 후 확인할 점

가속과 일반 공격력 부여는 HUD에서 AttackIncrease 한 아이콘으로 묶인다. 몸체 텍스트는 각 실제 발동 출처를 따라가므로 `가속 X N`과 `깔끔한 뒷정리`를 그대로 사용한다. N은 완료 이동 칸 수이며 합계 공격력을 나눠 구하지 않는다.

사냥꾼의 표식은 **출처 특성 이름**, 약점 노출은 **대상에게 적용된 효과 이름**이다. 기본 몸체 문구는 사냥꾼의 표식이며 원하면 해당 줄 Text Source를 Override로 바꿔 문구를 편집한다. 약점 노출의 해당 출처 추가/값·수명 갱신이 근거다. 최대 증가율이 그대로인 갱신도 대응하며 Magnitude1을 증가율로 쓰지 않는다. 같은 값/수명으로 상태 사건 자체가 없으면 HUD 갱신만으로 재출력하지 않는다.

조립 도구의 `Scenario_WeaknessExposure`와 `Scenario_WeaknessExposure_Refresh`로 HUD 반영→대상 문구를 확인한다. 재생은 별도 버프 효과가 아니며 진행값 변경이나 만피 회복 기회만으로 새 회복/특성 문구를 만들지 않는다. 실제 HealthRecovered/HP 사실을 사용한다.

HUD Buff/Debuff의 Description Panel/Text는 여전히 후속 연결 대기다. 이 참조가 비어 있어도 몸체 프리팹은 별도 경로로 출력된다. 이번 연동의 실제 에디터 임포트/Play/Profiler는 미검증이며 [계획 §11](../plans/2026-10-31-status-effect-view-integration-plan.md#11-구현검증-기록--2026-10-31)의 대기 항목을 따른다.


## 7. 3차 설정 확인 목록

1. 같은 줄에서 Text Source=Override → Text를 직접 입력하고 색·크기·Position Offset을 바꾼 뒤 다시 재생한다. 기본 이름으로 돌아가려면 TraitDisplayName을 고른다. Override Text 저장값은 지우지 않는다.
2. 동일한 프리팹으로 서로 다른 색/크기/오프셋을 번갈아 출력해 이전 스타일이 남지 않는지 본다. 폰트/배경/장식과 Prefab Override 선택은 유지돼야 한다.
3. Position Offset=(0,40), (40,0), (-40,-20)으로 방향을 확인한다. 움직임·카메라/Shake·반복 재생에서 offset이 누적되지 않고 몸을 따라가야 한다. 화면비/해상도별 크기도 확인한다.
4. 유지·페이드·쌓기·중단/초기화·환경 종료, 가속 X N·StateApplied·희생 이동/사망을 확인한다. 컬러 알파와 프리팹 CanvasGroup 기본 알파에 페이드가 함께 적용된다.

3차는 코드 컴파일·정적 검증까지 수행하고 실제 에디터 임포트·Play·Profiler는 사용자 후속이다. 현재 Play를 종료하거나 Refresh하지 않았다. [Phase K 기록](../plans/2026-10-31-trait-body-feedback-plan.md#10-3차-phase-k--activation별-표시-설정)을 따른다.


정찰 부대는 여러 이동 기여분의 출처 목록 전후 변경으로 대응한다. 억제된 출처가 새로 추가돼 유효 합계가 그대로여도 문구를 한 번 출력한다. Magnitude는 실제 Core가 증가율로 생산하지만 표시 근거에서 증감 수식으로 재계산하지 않는다. 효과 제거·취소·귀환·HUD 갱신은 forward 부여 문구를 만들지 않는다. `Scenario_ReconExposureGroups`로 확인한다.
