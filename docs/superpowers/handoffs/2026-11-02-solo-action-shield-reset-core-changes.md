# 단독 행동 ShieldReset — Core 변경 전달 (2026-11-02, 미전달)

> View 작업에서 사용자 지시로 수정한 Core 변경을 Core 작업자에게 전달하기 위한 문서입니다.
> 상태: **코드 반영, 컴파일 미확인** — 사용자 Unity 컴파일·재생 확인 대기.
> 구현 기록 원본: [View–Core 인계 §5.76·§62](2026-09-24-view-core-data-boundary-handoff.md). 외부 발송은 하지 않았습니다.

## 1. 수정한 동작

단독 행동(`SoloAction`)은 특성 보유자의 **자기 진영 턴 종료에만** 보호막을 초기화합니다. 주기 2·3 같은 설정도 자기 진영 종료 횟수로 판정합니다.

| 특성 보유자 | 아군 턴 종료 | 적 턴 종료 |
|---|---|---|
| 아군 | 설정 주기에 도달하면 초기화 | 상대 종료만으로 잔량·초기화 기록 변경이나 `ShieldReset` 없음 |
| 적 | 상대 종료만으로 잔량·초기화 기록 변경이나 `ShieldReset` 없음 | 설정 주기에 도달하면 초기화 |

운영 단독 행동 설명은 이미 "보호막 한도는 자신의 진영 턴 종료마다 갱신됩니다"였지만, 기존 Core는 양 진영 종료마다 보호막을 채웠습니다. 잔량이 가득해도 초기화 기록이 바뀌어 양 0의 `ShieldReset`이 발생했고, View가 그 사실을 받아 유지 이펙트를 다시 재생했습니다.

사용자 승인: "조건 우리가 추가하자, 수정 범위 측정해" → "우리가 구현 진행해". 범위는 단독 행동입니다. 결속(`Cohesion`)의 기존 양 진영 종료 초기화 정책은 유지했습니다.

## 2. 변경한 Core 파일 3개

아래 경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` 기준입니다.

| 파일 | 변경 내용 |
|---|---|
| `Application/Combat/TraitTurnLifecycle.cs` | 이미 받은 `endedSide`를 `TraitPresenceReconciler.EndAndRecord`에 전달합니다. |
| `Application/Combat/TraitPresenceReconciler.cs` | 일반 보유 조정은 종료 진영 없음, 턴 종료 조정은 명시한 진영을 전달합니다. 단독 행동은 출처 진영과 종료 진영이 같을 때만 초기화 후보가 됩니다. 상대 종료에서도 자격·출처 정리와 도발 조정은 수행합니다. |
| `Domain/Traits/Effects/SoloActionEffectRule.cs` | 자기 진영 종료 누계와 `ResetEveryControllerTurnEnds`로 초기화 시점을 판정합니다. 공통 보호막 규칙에는 합산 누계를 기록 경계로 전달하며, 합산 주기를 중복 적용하지 않습니다. |

자기 진영 종료 누계는 기존 턴 사실에서 구합니다.

- Player: `CompletedPlayerTurnCount`
- Enemy: `CompletedControllerTurnCount - CompletedPlayerTurnCount`

새 Buffer 상태나 별도 누계를 만들지 않았습니다. `LastResetControllerTurnCount`는 최초 부여와 이후 초기화 모두 **기존 합산 종료 누계 단위**를 유지합니다.

## 3. 병합할 때 확인할 내부 호출 계약

- `TraitPresenceReconciler.EndAndRecord(input, projected, endedSide, events)`와 `EndControllerTurn(input, projected, endedSide)`는 종료 진영을 명시합니다. 종료 진영은 Player 또는 Enemy여야 합니다.
- `SoloActionEffectRule.Evaluate`에는 합산 누계 다음으로 자기 진영 종료 누계가 추가됐습니다. 보유 조정이 두 누계와 진영 일치 여부를 전달합니다.
- `CombatTurnCalculator`가 전달하던 실제 종료 진영을 그대로 사용합니다. 다음 진영을 나타내는 `ControllerPhase`에서 종료 진영을 추정하지 않습니다.
- 일반 보유 조정에서는 보호막을 다시 채우지 않습니다. 이동·사망으로 고립 조건이 바뀌면 기존 예산을 유지하며 자격만 조정합니다.
- 공통 `ShieldStatusRules`·`ShieldGrantRules`, 공개 Contracts/DTO, Buffer, DB SO, View 이펙트 설정과 서비스 조립은 수정하지 않았습니다.

## 4. 사용자 Unity 확인 항목

**컴파일 미확인.** 컴파일·재생 확인은 사용자가 Unity에서 수행합니다.

1. 아군 단독 행동 보호막을 일부/전부 소진한 뒤 적 턴을 종료해도 재충전·리셋 연출이 발생하지 않는지 확인합니다. 아군 턴 종료에는 설정 주기대로 갱신돼야 합니다.
2. 적 단독 행동도 반대로 확인합니다. 아군 턴 종료에는 유지되고 적 턴 종료에만 갱신돼야 합니다.
3. 주기 2·3을 확인할 때 양 진영 합계가 아닌 해당 보유자의 진영 종료 횟수로 셉니다. 전투 중 늦게 부여된 보호막도 같은 진영 누계 기준을 따릅니다.
4. 보호막이 가득한 자기 진영의 갱신 시점에도 양 0의 `ShieldReset`은 발생할 수 있습니다. 상대 진영 종료에서는 이 리셋도 없어야 합니다.
5. 상대 턴 중 주변 유닛 이동·사망에 따른 자격 해제/복구, 이동 되돌리기·손패 왕복, 결속의 기존 갱신 동작을 확인합니다.

## 5. 앞선 시험 변경과 검증 기록 정정

이번 작업에서는 앞서 `TraitShieldTests.cs`에 38건, `CoreIntegration/TraitShieldPublicationTests.cs`에 3건을 추가했고 MSBuild·별도 NUnit 실행도 수행했습니다. **이는 View 작업의 Test 코드 작성 금지·전투 View MSBuild 금지 원칙을 지키지 못한 작업입니다.** Core 수정 승인을 이 원칙의 예외 승인으로 취급하지 않습니다.

실제로 실행한 과거 기록은 [인계 §62.2](2026-09-24-view-core-data-boundary-handoff.md#622-core-구현과-과거-도구-실행-기록-2026-11-02-사용자-unity-확인-대기)에 보존합니다. 별도 실행에서 신규 41건은 통과했고 전체는 144/145였습니다. 남은 도발 발동 개수 시험 1건은 수정 전 Core 소스를 별도 빌드한 어셈블리에서도 같은 실패가 발생했습니다. 이 기록은 사용자 Unity 컴파일·Test Runner·재생 인수 결과가 아닙니다.

현재 두 시험 파일의 추가 내용은 남아 있습니다. 이번 문서 정리에서는 시험 코드나 Git 스테이징 상태를 변경하지 않았고, 빌드·시험을 추가 실행하지 않았습니다.

## 6. 구조 검토

기존 턴 사실에서 자기 진영 횟수를 파생해 SSOT/P7을 유지했습니다. 보유 조정은 종료 정보 전달과 자격 조정, 단독 행동 규칙은 주기 판정을 담당합니다(SRP). 기존 공통 효과·공개 계약을 유지하고 필요한 내부 입력만 추가해 OCP/LSP/ISP/DIP를 점검했습니다. JMH/FW 서비스 등록·Builder·입력·렌더 수명 변경은 없습니다. 수정 범위는 단독 행동과 공유 보유 조정의 종료 정보 전달에 한정됩니다.
