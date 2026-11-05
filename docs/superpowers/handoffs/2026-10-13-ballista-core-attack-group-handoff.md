# Ballista C4 공격 그룹 계약 인계

- 작성: View 작업, 2026-10-13
- 승인: 사용자가 이번 작업의 최소 Core 계약 구현과 인계 기록을 명시적으로 허용했다.
- 실행·검증 상태 원본: [임시 공격 계획 §6.4](../plans/2026-10-13-temporary-attack-presentation-plan.md#64-c4-ballista)
- 외부 전달: 미전달. 문서 작성은 Core 담당자에게 메시지를 보낸 것이 아니다.

## 변경 이유

`PiercingShotDelivery`는 실제 이동 셀·피격 대상과 순서·종점·종료 사유를 이미 제공한다. 운영 Ballista는 `PiercingFalloffTraitAuthoring(30, int.MaxValue)`와 단일 지정으로 한 줄을 발사한다. 과거 3방향 고정 발사는 현재 규칙이 아니다.

후속 피해·사망에는 `CombatDamageOrigin.AttackGroup`이 있지만 `AttackResult`에는 대응 그룹이 없었다. 같은 공격자의 연쇄 피해·장애물 폭발 등을 공격자 ID만으로 화살에 묶거나, View에 Core 내부의 직접 공격 번호 `1`을 복제하는 방식은 피한다.

## 최소 Core 변경

| 파일 | 변경 |
|---|---|
| `Contracts/Results/AttackResult.cs` | 읽기 전용 `long AttackGroup` 추가. 성공 생성 경계의 마지막 선택 인수 `attackGroup = 0`으로 전달한다. 음수 거부, 실패·취소 결과의 양수 그룹 거부. 0은 대응 정보가 없는 기존 생성 경로다. |
| `Application/Combat/CombatCommandCalculator.cs` | 성공한 직접 공격 결과에 기존 `CommandAttackGroups.Direct`를 그대로 게시한다. 같은 명령의 직접 피해 출처와 같은 SSOT 값을 사용한다. |
| `Assets/Tests/EditMode/PiercingShotCommandTests.cs` | 운영 Ballista 계산 결과의 그룹이 양수이며 실제 직접 피해 출처 그룹과 같음을 확인한다. |

피해량·감쇠·공격 범위·벽·피격 순서·그룹 발급 정책은 변경하지 않는다. `PiercingShotDelivery.Origin`은 특성 효과 출처이고, 새 `AttackGroup`은 피해 발생 단위다. 두 식별자는 대체 관계가 아니다. 이 변경의 위치는 공용 계약과 기존 Core 계산 게시 경계이며 별도 View용 계산기를 Core에 만들지 않는다.

그룹은 명령 내부 값이며 전역 고유 ID가 아니다. View는 한 결과의 최신 `CombatAttackEvent` 구간 안에서 **그룹·가해자·실제 접촉 대상**이 모두 일치하는 후속 사건만 대응한다. 결과 취소·다음 결과 시작에서 대응표를 버린다. 0을 직접 공격으로 추측하지 않는다.

## View 소비와 표시 시점

피해 계산은 Core 결과를 그대로 사용한다. View는 실제 관통 종점까지 화살을 보간하고 접촉에 도달한 대상만 표시 대기를 해제한다. 연결된 사건의 체력·사망 표시는 화살 도착 이후이며 피해 숫자 팝업을 새로 추가하지 않는다.

공격 사건은 발사 시점을 통지하고, 같은 공격 수명은 화살·반동이 끝날 때까지 유지한다. Ordered는 선택적인 `ICombatPresentationBarrier`로 다음 이동·공격·턴 전환과 결과 확인을 기다린다. 사건을 재정렬하거나 미리 적용하지 않는다. 원본 목록이 먼 대상 사건부터라면 가까운 대상도 그 사건 순서 뒤에 표시된다. 여러 출처가 합쳐진 단일 사건에 직접 공격 출처가 있으면 해당 접촉을 기다린다.

잔여 장전 상태는 `BallistaWeaponView`가 소유하고 턴 종료 재장전·풀 반환·비활성화·Clear에서 정리한다. Coroutine·별도 Update·새 서비스·Buffer 직접 쓰기는 추가하지 않는다. 사거리나 InGame DB로 공격 표시 종류를 고르지 않는다. Akimbo의 프로필 변경은 사용자가 View SO에서 직접 수행했다.

검증 원본과 GameView 확인 여부는 계획 §6.4를 따른다. Core 담당자는 최소 계약 변경과 명령 내 그룹 범위를 확인하면 된다.
