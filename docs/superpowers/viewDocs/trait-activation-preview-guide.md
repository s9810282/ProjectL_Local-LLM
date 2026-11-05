# 특성·Activation 미리보기 사용 안내

특성별 효과·현재 인게임 사용 가능 상태·Kind 전체·Anchor/Point 제약은 [특성 효과·Activation 저작 참조](trait-effects-and-activation-reference.md)에서 확인한다.

Unity 메뉴 **ProjectL > Combat > Trait Activation Preview**에서 사용한다. 다른 씬을 편집 중이어도 창의 버튼으로 전용 Play Mode 씬을 열 수 있다. 씬을 직접 열거나 Build Settings에 추가할 필요는 없다.

이 환경은 선택한 발동·피격 결과를 만들어 **연출을 확인**한다. 실제 특성 보유 여부, 발동 조건, 피해 공식은 계산하지 않는다.

## 0. 조립 재생으로 전후 상황 확인하기

2026-10-31부터 **간단 재생 / 조립 재생**을 제공한다. [설계](../specs/2026-10-31-composable-trait-preview-design.md)·[구현/검증 기록](../plans/2026-10-31-composable-trait-preview-plan.md)을 함께 참고한다.

1. `ProjectL > Combat > Trait Activation Preview`를 열고 **조립 재생**을 선택한다.
2. **프리셋 빠른 불러오기**에서 아래 예제를 고른다. 직접 만든 에셋은 하단 **조립 프리셋**에 넣고 불러와도 된다.
3. 초기 유닛과 순서 목록을 확인하고 **구성 확인**을 누른다. 오류는 묶음·블록 이름과 이유로 표시한다.
4. **환경 열기 / 재생**으로 처음부터 시연한다. 로딩 뒤 초기 배치를 먼저 보려면 순서 맨 앞에 시간/수동 대기를 둔다.
5. **묶음마다 멈춤**을 켜거나 재생 중 **묶음 경계에서 멈춤**을 누르면 현재 묶음이 끝난 뒤 기다린다. **다음 진행**은 다음 묶음을 실행하고, **자동 계속**은 경계 멈춤을 해제한다. 수동 대기는 다음 진행이 필요하다.
6. **중단·초기화**는 진행·대기·반복을 취소하고 초기 배치/상태로 돌아간다. 다시 재생할 때 입력·원본 SO를 새로 읽는다.

| 조립 프리셋 | 흐름 |
|---|---|
| Scenario_Acceleration | 초기 관찰 → 같은 묶음의 이동·발동·가속 +10 → 관찰 → 공격·가속 소모 → 관찰 |
| Scenario_CleanSweep | 다른 아군 A 공격 → 보유자 B에게 공격력 +5 → 관찰 → B 공격 → 명시적 부여 제거 |
| Scenario_ManualStep | 깔끔한 뒷정리 조합을 Editor에서 편집·저장한 예제. 유닛 순서가 달라도 참조 유지, 마지막에 다음 진행 대기 |
| Scenario_Sacrifice / Scenario_Sacrifice_Death | 초기 관찰 → 공격+희생 반응 → 관찰. 생존/사망 비교 |
| Scenario_FollowUpAttack | 기존 자료 보존. Retired DoubleShot 외형이 활성 미등록이면 구성 검사에서 진단 |

예제는 공통 블록을 저장한 입력 에셋이다. 특성마다 전용 시나리오 코드를 추가할 필요가 없다. **이동/공격 블록이 실제 특성을 자동 판정하지는 않는다.** 원하는 상태와 발동을 직접 이어 붙인다. 피해도 자동 계산하지 않고 실제 수신자의 최종 HP를 입력한다(0=사망).

### 조립할 때의 단위

| 단위 | 사용 방법 |
|---|---|
| 초기 유닛 | 이름·외형·진영·배치·HP·공격력, Held 가속·결속 방어·재생 설정/진행·초기 효과 출처 목록 |
| 재생 묶음 | 블록을 순서대로 포함. 한 유닛 이동 경로 하나, 일반 공격 하나, 추가 공격 하나 또는 상태/발동만 배치 |
| Movement | 이동 유닛·시작 칸을 제외한 인접 경유 칸·최종 방향. 다른 생존 유닛 칸을 통과하지 않음 |
| Attack | 공격자·지정 칸·원래 대상·실제 수신자 최종 HP. 지정 칸은 첫 원래 대상의 칸 |
| State | Acceleration=Held 설정/0으로 소모, Effect=정규 효과 Set/Remove, Cohesion=명시 방어 보너스, Regeneration=명시 설정/진행 |
| Health | 지정한 최종 HP 사실. 회복 공식/특성 발동을 자동 생성하지 않음 |
| Return | Hand 또는 EnemyWaiting 귀환 사실. 상태 제거는 별도 Effect Remove로 작성 |
| Activation | TraitVisual SO·Kind·발동자·대상·칸·수치. 큐/Unit Actions만 재생하며 버프를 자동 생성하지 않음 |
| ChildAttack | 발동 입력+추가 공격의 피해. FollowUpAttack/KillChainAttack만 가능. 발동자=공격자 |
| Interception | 같은 묶음의 공격에 반응. DamageRedirected, 발동자=수호자, 대상 한 명=보호 대상. 공격의 최종 HP는 수호자 기준 |
| 시간/수동 대기 | 묶음 사이 관찰 또는 다음 진행 대기. 공격과 피격/희생 후속 사이에는 끼우지 않음 |

상태·일반 발동의 **Recorded**는 기록 순서이며, **AttackHit**는 같은 묶음 공격의 타격 후속이다. 타격 시점은 기존 운영 Gate가 정한다. 공격 없이 AttackHit를 고르면 구성 오류다. 일반 공격과 추가 공격을 각각 다른 묶음에 두면 첫 공격 완료 뒤 다음 공격을 볼 수 있다.

버프가 켜지고 꺼지는 것은 **State**, 그 순간 이펙트/행동은 **Activation**으로 각각 작성한다. 버프 아이콘은 UnitStatusVisual의 기존 HUD 경로다. 가속을0으로 바꾸면 Held가 사라진다. 일반 공격력 부여가 남았다면 AttackIncrease 아이콘은 유지된다. 보유 특성은 변하지 않는다. 원본에 발동 줄이 비어 있으면 상태 변화는 보여도 전용 큐는 없을 수 있다.

2026-10-31 확인 기준으로 공유 `HUD.prefab`의 `Description Panel/Text` 참조가 비어 있어 **아이콘 설명 팝업은 표시되지 않는다**. 상태 아이콘·전후 수치 진단·소모는 사용할 수 있다. 전용 환경의 UI 포인터 입력은 연결했으며, 공유 HUD 프리팹의 설명 패널 연결은 사용자 결정으로 후속 보류했다.

Effect의 **Source ID**는 같은 효과 출처를 수정/제거할 때 재사용한다. 대상·효과 키·Origin은 유지한다. 다른 출처는 새 ID로 더한다. 키에 따라 Amount/Percent/Remaining Turns/Eligible/Last Reset 입력만 표시한다. 남은 횟수는 명시한 사실이며 대기/공격이 자동 차감하지 않는다. DefenseIncrease는 Effect 출처 대신 Cohesion으로 입력한다.

블록/묶음은 추가·복제·정렬·제거할 수 있고 Undo를 지원한다. 유닛 참조는 안정 ID이므로 목록 순서 변경으로 다른 유닛이 선택되지 않는다. 유닛 삭제 뒤 남은 참조는 오류로 표시한다. 블록 종류를 바꾸면 해당 입력을 초기화한다. 조건에 맞는 상태/발동 블록은 **앞 묶음에 붙이기 / 새 묶음으로 분리**할 수 있다.

하단 진단은 현재 묶음의 입력 전→완료 예정 HP·기본/유효 공격력·가속/부여 값을 표시한다. 전체 완료 횟수는 모든 묶음과 대기를 마쳤을 때만 오른다. **전체 완료 뒤 관찰 시간**은 시작 지연이 아니며, 반복 간격과 함께 전체 재시작 전 기다리는 시간이다.

**조립 프리셋으로 저장**은 현재 입력을 새 에셋으로 저장한다. 원본 TraitVisual/공격 SO 값은 복제하지 않는다. 기존 네 간단 프리셋은 유지되며 **간단 입력을 조립으로 가져오기**로 명시적으로 변환할 수 있다.

## 1. 간단 재생으로 처음 한 번 재생하기

1. Unity가 Play 중이면 종료하고 **ProjectL > Combat > Trait Activation Preview**를 연다.
2. Project 창에서 `Assets/ProjectL/Editor/CombatPreview/Presets/`를 연다.
3. 창 상단 **프리셋 빠른 불러오기**에서 `Interception_Sacrifice`를 고른다. 하단 **프리셋** 칸에 에셋을 넣고 **프리셋 불러오기**를 눌러도 된다.
4. 하단 **환경 열기 / 재생**을 누른다. 편집 중인 씬의 저장 확인이 나오면 처리한다.
5. 전용 씬이 Play Mode로 열리고 **Game** 창에 희생이 재생된다. 미리보기 창의 상태와 재생 완료 횟수도 확인할 수 있다.
6. 같은 설정으로 다시 보려면 **환경 열기 / 재생**, 배치를 되돌리려면 **중단·초기화**, 끝내려면 **환경 종료**를 누른다.

전용 씬은 `Assets/Scenes/Dev/TraitActivationPreview.unity`다. 다른 게임 씬이 이미 Play 중이면 먼저 종료해야 한다. 시작 씬은 도구가 임시로 지정하므로 평소 Play From Boot 설정을 직접 바꾸지 않는다. 모든 Reload/Boot 설정 조합을 검증한 것은 아니다.

## 2. 기본 프리셋 고르기

| 프리셋 | 볼 수 있는 상황 | context |
|---|---|---|
| Moment_Acceleration | 기존 단독 합성 입력. 현재 가속 StateApplied/Count는 Movement·State가 없어 조립 모드 안내 진단 | Moment |
| ChildAttack_DoubleShot | 기존 보존 자료. 활성 미등록 외형이면 재생 사전 진단 | ChildAttack |
| Interception_Sacrifice | 수호자가 나가서 대신 맞고 생존 | Interception |
| Interception_Sacrifice_Death | 수호자가 대신 맞고 사망 | Interception |

프리셋은 원본 특성 에셋 참조와 재생 입력을 저장한다. 이펙트·큐·유닛 행동 등 연출 값은 원본 SO에서 읽는다. 원본 발동 줄에 표시가 없으면 해당 연출이 나오지 않는 것이 정상이다.

## 3. 원하는 특성·발동으로 바꾸기

1. 비슷한 상황의 프리셋을 불러온다.
2. **특성 검색**에 에셋 이름 일부를 입력하고 **특성** 목록에서 원본 TraitVisual 에셋을 선택한다. 검색은 에셋 경로·파일명을 기준으로 한다.
3. **Activation Kind**에서 확인할 발동 종류를 고른다. 기본 목록은 원본에 작성된 Activation 줄만 보여 준다. 미작성 종류까지 선택하려면 **줄 없음도 확인**을 켠다.
4. `context`를 맞춘다. 일반 발동은 Moment, 추가 공격은 ChildAttack, 대신 맞기는 Interception이다.
5. 필요한 유닛·대상·피해 입력을 수정하고 **환경 열기 / 재생**을 누른다. 설정을 바꾸면 진행 중인 재생·반복은 중단된다.

| 입력 | 뜻·사용 방법 |
|---|---|
| units | 등장 유닛 목록. key는 View 외형 키이며 enemy·cell·direction·health 등을 설정한다 |
| sourceIndex | 발동 유닛의 목록 인덱스. 추가 공격에서는 공격자, 대신 맞기에서는 수호자 |
| targetIndices | 대상들의 목록 인덱스. 대신 맞기에서는 지켜질 유닛 한 명 |
| attackerIndex | 대신 맞기에서 원래 공격하는 별도 유닛. 추가 공격의 공격자는 sourceIndex이므로 이 값으로 바꾸지 않는다 |
| cells / magnitude | 발동 사실에 넣을 영향 칸·수치 |
| direction / targetCell | 공격 문맥의 방향·지정 칸 입력. 대신 맞기의 실제 대상 칸은 지켜질 유닛 위치에서 정한다 |
| damage / kill | 합성 피해량 / 강제 사망 표시. 실제 피해 계산 결과가 아니다 |
| stageViewKey | 맵 외형 키. 기본 10000. 변경하려면 환경을 종료한 뒤 다시 연다 |

목록 인덱스는 **0부터 시작**한다. 기본 프리셋에서 `units[0]`은 발동 유닛, `units[1]`은 대상, `units[2]`는 대신 맞기 문맥의 공격자다. 유닛 외형 키를 sourceIndex 등에 넣는 방식이 아니다.

필드는 **8×8, 좌표 0~7**이다. 초기 유닛은 살아 있어야 하며 같은 칸에 겹칠 수 없다. 대신 맞기는 수호자·지켜질 유닛·공격자가 서로 달라야 한다. ChildAttack은 FollowUpAttack 또는 KillChainAttack, Interception은 DamageRedirected Kind가 필요하다. 잘못된 조합은 창의 오류와 Console에서 확인한다.

## 4. 연출 수정·반복·입력 저장

- **원본 SO Inspector 열기**를 눌러 선택한 TraitVisual 에셋을 Inspector에 표시한다. 해당 Activation 줄의 큐·공격 표시·유닛 행동을 수정한 뒤 다시 재생한다. 이 작업은 실제 원본 에셋 값을 바꾼다.
- `repeat`를 켜고 재생하면 전체 재생 완료 뒤 반복한다. `observationSeconds`는 완료 뒤 잔상을 볼 시간(기본 1초), `repeatInterval`은 그 뒤 반복 간격(기본 0.5초)이다.
- 창 입력이나 SO 에셋을 수정하면 반복이 중단된다. 수정 뒤 **환경 열기 / 재생**을 다시 누른다.
- **현재 설정을 새 프리셋으로 저장**으로 자주 쓰는 배치·발동·피해 입력을 저장한다. 원본 연출 값의 복사본은 저장하지 않는다.
- Render DB나 맵 외형을 바꿨다면 **환경 종료** 후 다시 연다. 일반 View SO 수정은 다음 재생부터 읽는다.

## 5. 버튼과 현재 제한

희생에서 두 유닛을 함께 움직이려면 원본 SO의 `DamageRedirected → Unit Actions`에 아래 두 항목을 넣는다. 현재 희생 SO에도 연결돼 있다.

| 항목 | Actor | Place | Distance | Look | Out Seconds | Return Seconds | Face |
|---|---|---|---|---|---|---|---|
| 수호자 | Source | GuardFront | 0.6 | Hop | 0.5 | 0.25 | Attacker |
| 보호 대상 | Targets | AwayFromAttacker | 0.3 | Walk | 0.5 | 0.25 | Attacker |

두 행동은 동시에 시작하고 모두 나간 뒤 공격한다. 보호 대상은 수호자가 맞는 동안 물러난 자리에 머물고, 피격·사망 후속 처리가 끝나면 원래 자리로 돌아온다. `Hold Seconds`는 Interception에서 쓰지 않는다. 목록에 같은 활성 Actor 역할을 두 번 넣을 수 없다. 미리보기 진단에 `유닛 행동: 2개`가 표시되는지 확인한다. `AwayFromAttacker`는 공격자 정보가 있는 Interception용이며 일반 Moment에서는 경고 뒤 Stay로 대체된다.

| 버튼 | 동작 |
|---|---|
| 환경 열기 / 재생 | Play 전에는 전용 씬을 열어 재생, 전용 씬에서 Play 중이면 현재 설정으로 다시 재생 |
| 중단·초기화 | 진행 중인 연출·반복을 취소하고 초기 배치 복원. Play Mode는 유지 |
| 환경 종료 | 세션·서비스를 정리하고 도구가 시작한 Play를 종료 |

종료 때 **Setting UI 미등록 예외**가 발생하는 제한은 사용자 결정으로 유지한다. 확인된 실행에서는 오류 뒤 Play가 종료되고 기존 Combat 편집 씬으로 돌아왔으나, 전체 Reload/Boot 설정 조합의 복원까지 확인한 것은 아니다. Play가 남아 있으면 Unity 상단 Play 버튼으로 종료한다.

Play 진입 시 `SerializeReference ... Refid -2` 오류는 성장 SO의 null 참조 목록 항목 누락이 원인이었다. 사용자 승인으로 가속을 Unity에서 재저장하고 같은 null 항목을 남은 21개에도 보완했다. 기본/강화 값·강화 null·GUID를 유지했으며, 동일한 재생 진입의 오류는 **22건 → 21건 → 0건**으로 해소됐다. 상세 기록은 계획 §9.6~9.7이다.

이외 잘못된 Kind·좌표·에셋 참조 오류는 입력을 확인한다. 카메라 없는 맵에서는 줌·흔들림을 확인할 수 없으며 창에 안내가 나온다. 기본 공격 fallback 등 운영 경고는 Console에서 확인한다.

환경 진입 후 빈 화면이던 문제도 보완했다. Play Maximized의 임시 창 파괴로 첫 요청이 취소되던 처리와 HUD Category Visual 표 연결 누락을 수정했다. Play Focused/Maximized 모두 첫 클릭으로 맵·유닛·체력바 표시와 추가 공격 완료를 수동 확인했다. `Activation 줄 없음`이 뜨는 특성은 원본 SO에 전용 연출이 아직 없는 상태이며, ChildAttack에서는 추가 공격 자체만 재생될 수 있다. 상세 기록은 계획 §9.8이다.

이 환경은 추가 공격의 재귀 연쇄, 모든 상태/HUD·흔적 편집, 실제 전투 규칙 검증을 제공하지 않는다.

## 6. 확인 이력과 코드 정책

네 기본 프리셋은 각각 20회 반복·중단 검사를 통과했다. 측정한 Scope 발급 횟수·유닛·HUD Layer·ParticleSystem 수는 유지됐다. 내부 모든 대여·구독의 누수 부재나 전체 설정 조합 검증을 뜻하지 않는다.

사용자 요청으로 이번에 만든 `TraitActivationPreviewTests.cs`, `TraitActivationPreviewVerifier.cs`와 각각의 meta를 삭제했다. 자동 검증 메뉴는 없어졌으며 미리보기 창·전용 씬·프리셋은 유지한다. 앞으로 View 측에서는 테스트 코드를 새로 만들지 않고 코드 검토·컴파일·수동 재생으로 확인한다.

- 설계: [즉시 재생 환경 설계](../specs/2026-10-30-trait-activation-preview-design.md)
- 구현·검증·삭제 기록: [Task 계획 §9](../plans/2026-10-30-trait-activation-preview-plan.md#9-실행-기록)


몸체 텍스트 설정·HUD 동시 시작·가속 X N은 [특성 몸체 텍스트 사용법](trait-body-feedback-guide.md)을 따른다. StateApplied는 한 묶음의 Activation → State가 필요하며 가속은 앞선 Movement도 같은 묶음에 둔다.


몸체 텍스트의 문구·색·크기·Position Offset은 해당 Activation의 Body Text에서 편집한다. 폰트·배경·장식/Content 내부 배치는 `TraitBodyText.prefab`에서 편집한다. 공통 `Body Text Prefab`을 기본으로 쓰며 줄별 `Prefab Override`에 Variant를 연결할 수 있다. 문구·색·크기·Position Offset·대상·유지/페이드·Cue Timing은 Activation에 있다. 시연에 연결된 프리팹/의존 에셋을 수정하면 안전한 갱신 시점에 시연이 중단되므로 편집 후 다시 재생한다. 상세는 [몸체 텍스트 사용법](trait-body-feedback-guide.md)을 따른다.


## 10. Core 효과 연동 v2 — 새 조립 프리셋

| 프리셋 | 확인 순서 |
|---|---|
| Scenario_WeaknessExposure | 초기 A20%/2 → B30%/1 부여/몸체 문구 → A20%/1·B 제거 → 모두 제거 |
| Scenario_WeaknessExposure_Refresh | A20%/2+B30%/1 → A10%/1 갱신(최대30 유지) → B 제거(유효10) |
| Scenario_EffectSources | 효과8종·가속 Held+일반 부여·결속·활성/비활성 보호막 공존 → 출처 유닛만 Hand로 귀환해 수신 효과 유지 |
| Scenario_RegenerationCadence | HP95/100·설정10·주기2 → 명시 진행1/0·실제 회복5·HP100 → 만피 진행만 변경 |
| Scenario_EffectReturns | 효과 제거를 명시한 뒤 플레이어 Hand / 적 EnemyWaiting 귀환 |

새 예제를 불러와 **구성 확인 → 환경 열기 / 재생** 순서로 사용한다. 첫 관찰/묶음별 멈춤으로 HUD 전후 상태를 비교한다. 효과 자체는 State, 발동 문구는 Activation으로 작성하고 StateApplied는 같은 Packet에서 **Activation → 해당 State** 순서여야 한다. HuntersMark의 StatusApplied Magnitude는1이며 증가율은 Effect의 Percent에 넣는다.

같은 Source ID의 약한 값/짧은 기간 갱신도 Set이다. 출처가 여러 개여도 효과 아이콘은 하나다. 도구가 턴/만료/최대 출처/피해/회복/귀환 정리를 판정하지 않으므로, 다음 시점의 값과 제거/최종 HP를 각각 입력한다. Core Capture와 유효 공격력 투영은 공개 읽기 API를 사용하며 운영 Buffer에는 쓰지 않는다.

기존 v1은 사본에서 AttackGrant→Effect로 바꾼 뒤 v3으로 이전하고, v2는 timed 수명/Movement 없음으로 명시 이전한다. 원본 프리셋·유닛/블록/부여 ID·GUID를 보존하며 새 저장은 v3이다. v2에서 옛 AttackGrant를 직접 쓰는 숨은 호환 경로는 없다. 기존 enum0/1은 이전 식별용으로 보존했고 관리 참조 직렬화는 추가하지 않았다.

특성 선택은 연결된 환경의 Catalog(연결 전에는 기본 Catalog)의 활성 TraitVisual 표를 따른다. 표에서 제외한 MoveAttackDiscount/DoubleShot 등은 SO/프리셋이 남아도 선택/구성에서 거절한다. 새 줄 없음과 미등록은 다르다. Core 가용성을 View가 복원하지 않는다.

현재 검증은 Unity 제공 Roslyn으로 런타임·Editor·비Editor 정의 컴파일 및 정적 대조다. 사용자 지시로 현재 Play를 유지했으므로 **실제 에디터 임포트/컴파일·DataBaseAssetTests는 대기**이며 위 프리셋의 실제 Play 성공 기록은 아니다. [계획 §11](../plans/2026-10-31-status-effect-view-integration-plan.md#11-구현검증-기록--2026-10-31)과 [몸체 텍스트 사용법](trait-body-feedback-guide.md)을 함께 확인한다.

## 11. 에디터 인수 순서와 체크리스트

아래는 사용자가 직접 진행할 순서다. 현재 실행 중인 Play는 Codex가 종료하거나 변경하지 않았다. 합성 프리셋은 입력된 사실의 표시를 확인하며 실제 Core 발동 조건·턴 규칙은 별도로 인게임에서 확인한다.

### 11.1 재생 전

1. 현재 Play를 마칠 때 직접 종료하고 Unity 임포트·컴파일 완료를 기다린다. Console에서 이번 변경의 컴파일 오류, Missing Script, 누락된 참조/직렬화 오류가 없는지 확인한다. 새 C#·SO·프리팹이 정상 인식되는지도 본다.
2. `UnitStatusVisualDataTable`의 활성 행이 AttackIncrease·AttackDecrease·DefenseIncrease·DefenseDecrease·Shield·Disarm·Bind·WeaknessExposure의8개인지 확인한다. Acceleration/ArmorBreak SO는 보존 자료이며 활성 표에서는 빠져 있다.
3. `CombatPresentationSettings_Default`의 Body Text Prefab에 `TraitBodyText.prefab`이 연결돼 있는지, 프리팹의 CombatBodyTextView에서 Label/Fade Group 참조가 유효한지 확인한다. 추가 수동 연결이 필요한 설계는 아니다. 누락된 경우 오류와 대상 필드를 기록한다.
4. Test Runner의 EditMode에서 기존 `DataBaseAssetTests`를 실행해 결과를 기록한다. `ProductionShotGun_HasExplicitDurationOneAndUnchangedMagnitudeAndGuid`는 HuntersMark30%/1을 가정하지만 현재 ShotGun은 병합 시 유지한 Sacrifice다. 해당 입력 불일치 실패와 새 View 에셋 오류를 구분하며 시험/운영 특성을 임의 변경하지 않는다.

### 11.2 모든 새 프리셋의 공통 조작

1. `ProjectL > Combat > Trait Activation Preview` → **조립 재생**을 선택한다.
2. 아래 순서의 프리셋을 **프리셋 빠른 불러오기**에서 선택한다. 목록에서 찾기 어렵다면 `Assets/ProjectL/Editor/CombatPreview/Presets/`의 에셋을 **조립 프리셋**에 넣고 불러온다.
3. 첫 확인은 자동 반복을 끄고 **묶음마다 멈춤**을 켠다. **구성 확인**에서 오류가 없는지 확인한다.
4. **환경 열기 / 재생**을 누른다. 첫 관찰에서 배치/HUD를 보고 **다음 진행**으로 묶음별 결과를 확인한다. 묶음 사이 시간 대기는 상태를 자동 만료시키지 않는다.
5. 한 프리셋이 끝나면 **중단·초기화**로 초기 상태 복원을 확인한다. 다음 프리셋을 불러오고 다시 구성 확인/재생한다. 원본을 보존하려면 **조립 프리셋으로 저장**은 누르지 않는다.

### 11.3 새 프리셋별 진행과 기대 결과

| 권장 순서 / 프리셋 | 묶음별 확인 | 통과 기준 |
|---|---|---|
| 1. Scenario_WeaknessExposure | 초기 A20%/2 → **적중 후 약점 노출**에서 HP100→80·B30%/1 추가 → **대상 진영 종료 후 명시 값**에서 A20%/1·B 제거 → **A 만료**에서 A 제거 | 처음부터 약점 노출 아이콘 하나. B 부여 뒤에도 하나이며 HUD 반영 직후 대상 몸에 `사냥꾼의 표식`이 한 번 출력·페이드. B만 제거하면 아이콘 유지, 마지막 A 제거 후 사라짐. HP80은 입력한 값이며30% 피해 공식의 계산 결과가 아님 |
| 2. Scenario_WeaknessExposure_Refresh | 초기 A20%/2+B30%/1 → **적중 후 약점 노출**에서 A10%/1로 갱신 → **B 제거 후 A10%** | 최대값30이 변하지 않는 A 갱신도 대상 문구를 한 번 출력. 아이콘이 증식하지 않으며 B 제거 뒤 A가 남아 아이콘 유지. B 제거 자체로 표식 부여 문구가 새로 나오지 않음 |
| 3. Scenario_EffectSources | 초기 수신자 상태 → **방어막 적용 조건의 명시 갱신** → **출처 이탈·수신 효과 유지** | 효과8종이 키당 하나씩 공존. 일반 부여5+Held10은 AttackIncrease 하나, 결속 방어6은 별도 DefenseIncrease. 첫 보호막 비활성 뒤 유효 예산0이므로 Shield 아이콘은 사라진다. 비활성 출처 이력은 상태 사실에 유지된다. A만 손패로 돌아가도 수신자 효과는 유지. 초기 Held10만으로 새 가속 발동 문구가 나오지 않음 |
| 4. Scenario_RegenerationCadence | 초기 HP95/100·회복량10·주기2 → **첫 자기 진영 종료** → **두 번째 종료·명시 회복** → **만피 다음 주기 시작** → **만피 회복 기회 소비·HP 사건 없음** | 첫 진행1에서는 HP95 유지. 두 번째는 실제 회복량5·HP100. 뒤의 진행1→0에서는 HP100 유지, 가짜 추가 회복 없음. 재생 전용 버프 아이콘을 만들지 않음. 현재 Regeneration SO는 전용 Activation 줄이 없어 `재생` 몸체 문구/특수 이펙트 출력을 기대하지 않음 |
| 5. Scenario_EffectReturns | 아군/적의 초기 약점 노출 → **Hand 반환 사실** → **EnemyWaiting 반환 사실** | 아군 효과 제거 뒤 아군 몸/HUD가 필드에서 퇴장하고 적은 유지. 이후 적 효과 제거·대기 귀환으로 적도 퇴장. 이 예제는 제거와 귀환을 각각 입력했으며 귀환만으로 자동 제거됐다는 증거는 아님 |

**현재 화면에서 확인할 수 없는 항목:** 공유 HUD의 Description Panel/Text가 비어 있으므로 출처별 증가율·잔여 횟수·보호막 활성 상세의 팝업 가시성은 후속이다. 표의20/30/10, 예산/수명 값은 입력과 기대 상태를 설명한다. 아이콘만 보고 상세 수치가 맞다고 판정하지 않는다. 위 내용의 실제 설명 표시·동일 키 고정 팝업 갱신은 패널 연결 뒤 확인한다. 약점 노출의 전용 깨진 방패 Sprite도 아트 인수 대기이며 기본 아이콘을 전용 아트 완료로 기록하지 않는다.

### 11.4 기존 동작 회귀와 인게임 확인

- `Scenario_Acceleration`: 2칸 이동 후 HUD 갱신과 함께 `가속 X 2`. 증가량10을 이동 횟수로 사용하지 않는다.
- `Scenario_CleanSweep`: 다른 아군 공격 뒤 보유자의 HUD/문구가 시작한다. HUD 재갱신만으로 중복 출력하지 않는다.
- `Scenario_Sacrifice`/`Scenario_Sacrifice_Death`: 수호자 나가기 시작에 문구, 보호 대상 동시 물러남, 피격 후 복귀 또는 사망 회수. 이동·피격·다음 공격이 멈추지 않아야 한다.
- 중단·초기화/반복 재생/환경 종료 뒤 남은 몸체 문구·중복 HUD·이전 상태가 없는지 확인한다. 재시작은 초기 상태에서 시작한다. 프리팹 Label/Content를 편집했다면 다음 재생에 외형이 반영되는지도 본다.
- 실제 인게임에서는 최초 적중 뒤 약점 노출 적용·첫 적중 소급 없음, 대상 진영 턴 수명, 출처 이탈/대상 귀환, 재생의 자기 진영 주기·최대 HP 제한, 희생/끌어오기/연쇄 공격의 수신자·HP 순서를 확인한다. 조립 프리셋이 이를 자동 계산하지는 않는다.
- 카드에서는 표식 증가율/기간, 전투 중 위압 감소율·재생 회복량/주기를 확인한다. 전투 밖 위압/재생 정량·강화 표시는 CVI-R1 Core 응답 전까지 대기다.

문제 보고에는 프리셋 이름, 마지막 실행 묶음, 기대/실제 화면, Console 오류 전문을 남긴다. 현재 사용자 확인 결과는 아직 기록되지 않았다.


## 12. 위치 교환·정찰 부대 연동 v3

현재 저장 버전은3이다. 기존 v1/v2 프리셋15개는 원본을 바꾸지 않고 사본에서 이전한다. 초기 유닛에 명시 Action Use/누적 이동 거리, 위치 교환·정찰 보유/설정을 입력할 수 있다. Effect의 WeaknessExposure에서 `노출 수명`을 고르면 timed에만 잔여 횟수가 보인다. `이동 기여분 식별 사용`을 켜면 battle/turn/명령 번호를 입력하며 actor는 출처 유닛/Owner ID를 그대로 쓴다. 같은 Source ID의 Movement 식별도 제거 전에는 바꿀 수 없다. 입력 번호는 운영 명령 토큰이 아니다.

새 `FieldPlacement`는 한/두 참여자의 최종 칸·회전·행동·누적 거리를 입력하는 명시 사실 블록이다. Moved 교환은 PositionSwap 원인의 주체 Voluntary/승객 Forced 순서다. Returned는 복원이며 해당 블록 하나만 있는 단독 묶음에서 공개 Participants/행동 정산/Result 검증을 통과해 재생한다. 효과 제거는 별도 State 묶음에서 작성한다. 도구가 역적용 영수증·실제 사용 잠금·비용/거리 자격·도착 효과를 계산하지 않는다.

공통 조작은 **조립 재생 → 프리셋 불러오기 → 구성 확인 → 환경 열기 / 재생 → 다음 진행**이다. 새5개 예제는 묶음마다 멈춤을 기본으로 켰다. 한 예제 후 중단·초기화하고 다음 예제를 불러온다.

| 권장 순서 / 새 프리셋 | 진행과 기대 결과 |
|---|---|
| 1. Scenario_PositionSwapRestore | A(0,0)→(1,0) 경유 후 A(2,0)/B(0,0) 교환 → 관찰 → A(0,0)/B(2,0) 복원. 승객 별도 걷기 없음. 명시한 A행동/거리2만 복원 |
| 2. Scenario_ReconExposureGroups | 초기 Recon10+20+HuntersMark30=50 → A의 두 번째 이동15 추가(유효50 유지·대상 문구) → B20 제거(유효45). 효과 아이콘 하나·출처 목록 유지 |
| 3. Scenario_ReconMoveRemoval | 같은 A의 이동1=10/이동2=20·독립 공격 감소 → HP80→70 → 이동1만 제거 → 이동2 제거. 독립 감소/HP70 유지. 이 예제는 명시 제거이며 실제 Core Undo 잠금 검증이 아님 |
| 4. Scenario_ShieldHistory | 유효 예산30과 비활성 출처 → 활성 출처를0/비활성으로 명시(칩 제거) → 정상 reset 사실20을 명시(칩 복귀). 상태 이력을 삭제/재할당하지 않음 |
| 5. Scenario_ReserveCleanup | 대상 Returned→명시 공격 부여/노출 제거→Held0·재생 진행0. 비활성 보호막 이력 유지. 실제 장비 재고 환불·최대HP·밤/Main 규칙은 Core 인게임에서 별도 확인 |

출처별 상세/정확한 합계의 화면 확인은 HUD Description Panel/Text 연결 후다. 현재는 아이콘 수·표시 시점·몸체 문구·HP·배치와 구성 진단을 확인한다. 입력값만 보고 Core 피해 계산/사용 확정이 검증됐다고 판단하지 않는다. 실제 Core 인수 목록은 [계획 §8](../plans/2026-10-31-position-swap-recon-undo-view-integration-plan.md#8-사용자-play-인수-목록)을 따른다.

위치 교환/정찰은 운영 획득 풀·유닛 장착을 추가하지 않았다. 실제 인게임 발동 검증에는 사용자가 별도로 준비한 보유 구성이 필요하다. 이번 코드 컴파일/정적 검증은 실제 에디터 임포트·Play 성공을 대신하지 않는다.
