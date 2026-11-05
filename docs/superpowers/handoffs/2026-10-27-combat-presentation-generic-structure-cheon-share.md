# 전투 연출 범용 구조 — 천명님 공유 (2026-10-27, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 천명님.
> 상태: View 쪽 코드·에셋 완료, **컴파일·Unity 미확인**, 커밋 전.
> 관련 문서: [설계](../specs/2026-10-27-combat-presentation-generic-structure-design.md) · [계획 §13.3~§13.8](../plans/2026-10-27-combat-presentation-generic-structure-plan.md)

## 요약

- Core 파일은 바꾸지 않았습니다. Core에 요청할 것도 없습니다.
- View DB의 공격 표시 SO(`CombatAttackPresentationSO`) 구조가 바뀌었습니다. 그래서 천명님 시험 `CoreIntegration/PullAndMarkPresentationTests.cs`의 **사례 넷이 실행에서 실패합니다.** 컴파일은 됩니다.
- 이 시험은 사용자 결정으로 **고치지 않았습니다.** 천명님께서 고쳐 주세요. 고칠 곳과 제안 코드는 §3에 있습니다.

## 1. 바뀐 것 (시험에 닿는 부분만)

공격 표시 SO가 칸을 직접 갖지 않고 **부품 SO를 참조**합니다.

| 옛 칸 (`CombatAttackPresentationSO`) | 지금 자리 |
|---|---|
| `kind` | 없음. 투사체 부품의 **타입**에서 정해집니다. 부품 없음 = 근접, `CombatStraightProjectileSO` = 직선, `CombatArcProjectileSO` = 곡사 |
| `projectilePrefab`, `projectileEasing` | 투사체 부품의 기반 클래스 `CombatProjectileSO`(추상) |
| 직선 속도·높이 | `CombatStraightProjectileSO` |
| 곡사 비행 시간·높이·회전 | `CombatArcProjectileSO` |
| 근접 모션 다섯 칸 | `CombatMeleeMotionSO` |
| 쏘기 반동·시간·발사 시점 | `CombatShootMotionSO` |
| 곡사 무기 준비·복귀·버킷 | `CombatMortarMotionSO`(`CombatShootMotionSO`를 상속) |
| `impactEffect` | 공격 칸 큐 `cues`(접촉 시점 이펙트) |

새 칸은 셋입니다: `projectile`(투사체 부품), `codeMotion`(코드 모션 부품), `cues`(큐, 초기값이 있어 비워 둬도 됩니다).

함께 바뀐 동작이 둘 있습니다.

- **코드 모션이 없으면 공격 모션이 없습니다.** 코드 모션 공격도 조립 실행으로 바뀌었습니다(설계 G5). 시작할 모션이 없으면 처리기가 조립을 만들지 않고, 옛날처럼 일반 사건 소리로 넘어갑니다(`CombatAttackPlayback.HasCodeMotion`). 옛 SO는 종류마다 모션 값을 늘 갖고 있었으므로, 같은 동작을 보려면 모션 부품을 붙여야 합니다.
- **유닛 표시 집합이 외형 키별로 연출 값을 보관합니다**(`FieldUnitPresenter._presentationValues`, 키 = `EntityKey`). 유닛을 처음 빌릴 때 읽고, `Configure` 때 비웁니다. 그래서 어떤 외형 키의 유닛을 그린 **뒤에** `attackPresentation`을 바꾸면 옛 값이 남습니다.

## 2. 실패하는 사례

| 시험 | 사례 | 실패 자리 | 원인 |
|---|---|---|---|
| `Pull_AttackMotionRunsOnlyForDirectHit` | `false`·`true` | `EnableAttack` `:378` | `Set(_attackSettings, "kind", …)` — 칸이 없어 `GetField`가 null → `NullReferenceException` |
| `Pull_PreviewAndAttackTargetUseArrival` | `false`·`true` | 같음 | 같음 |

`:378`을 지나도 `:379` `projectilePrefab`이 같은 이유로 멈춥니다. 다른 시험은 공격 설정을 쓰지 않아 영향이 없습니다.

## 3. 고치는 방법 (제안)

`EnableAttack`만 바꾸면 됩니다. 단언은 그대로 둡니다.

```csharp
private ScriptableObject _projectilePart;
private ScriptableObject _motionPart;

private void EnableAttack(CombatAttackPresentationKind kind)
{
    _attackSettings = ScriptableObject.CreateInstance<CombatAttackPresentationSO>();
    // 종류는 투사체 부품 타입에서 나옵니다(부품 없음 = 근접).
    _projectilePart = kind switch
    {
        CombatAttackPresentationKind.Linear => ScriptableObject.CreateInstance<CombatStraightProjectileSO>(),
        CombatAttackPresentationKind.Mortar => ScriptableObject.CreateInstance<CombatArcProjectileSO>(),
        _ => null,
    };
    // 옛 SO처럼 종류마다 코드 모션을 둡니다. 근접은 이것이 없으면 공격 모션이 돌지 않습니다.
    _motionPart = kind switch
    {
        CombatAttackPresentationKind.Melee => ScriptableObject.CreateInstance<CombatMeleeMotionSO>(),
        CombatAttackPresentationKind.Linear => ScriptableObject.CreateInstance<CombatShootMotionSO>(),
        CombatAttackPresentationKind.Mortar => ScriptableObject.CreateInstance<CombatMortarMotionSO>(),
        _ => null,
    };
    if (_projectilePart != null)
    {
        // projectilePrefab은 기반 클래스 CombatProjectileSO의 private 칸입니다.
        // 이 파일의 Set은 target.GetType().GetField를 써서 기반 클래스 칸을 찾지 못하므로, 선언 타입으로 찾습니다.
        typeof(CombatProjectileSO).GetField("projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_projectilePart, _prefab);
        Set(_attackSettings, "projectile", _projectilePart);
    }
    Set(_attackSettings, "codeMotion", _motionPart);
    if (kind == CombatAttackPresentationKind.Mortar)
    {
        var weapon = _prefab.AddComponent<CatusCannonWeaponView>();
        var bucket = new GameObject("Bucket").transform;
        bucket.SetParent(_prefab.transform);
        Set(weapon, "bucket", bucket);
    }
    Set(Get(_visuals, "_data"), "attackPresentation", _attackSettings);
    _presenter.BeginEvent(new CombatCharacterEvent(CombatEventKind.Placed, _battle.Actor, _battle.Actor), () => { });
}
```

- `TearDown`에서 `_projectilePart`·`_motionPart`도 `DestroyImmediate`해 주세요(`_attackSettings`처럼).
- **값 보관(§1):** 지금 순서에서는 SetUp이 대상(`Unit_Demon`)만 놓고, 행위자(`Unit_Cowboy`)는 `EnableAttack` 안에서 처음 그려집니다. 그래서 보관이 새 값을 읽으므로 따로 비울 필요는 없어 보입니다. 행위자를 `EnableAttack`보다 먼저 그리는 순서를 더하면 보관을 비워야 합니다. 방법은 `((System.Collections.IDictionary)Get(_units, "_presentationValues")).Clear()` 한 줄(이 파일에는 `using System.Collections`가 없음)이거나 `_units.Configure(…)`를 다시 부르는 것입니다. 우리 Framework 시험 도우미 `CombatPresentationTestAssets.Reload`가 같은 일을 하지만, 이 시험 어셈블리(CoreIntegration)는 Framework 시험 어셈블리를 참조하지 않아 쓸 수 없습니다.
- `:365` `_attackSettings.CreateData()`와 `FireShot` 호출은 그대로 둬도 됩니다(서명이 같습니다). 직선·곡사 부품이 붙으면 `CreateData`가 통과하고, 투사체 풀은 `_prefab`으로 찾힙니다(`:368`).
- 확인한 뒤에도 실패하면 아래 순서로 봐 주세요.
  - 근접 `false` 사례에서 `CurrentPresentationPose`가 null이면, 코드 모션 부품이 빠졌거나 값 보관이 옛 값을 읽은 것입니다.
  - 직선·곡사 사례에서 `CreateData`가 예외를 내면, 메시지의 칸 이름(`Invalid …`)이 빠진 부품 값을 가리킵니다.

## 4. 이번 작업에서 바꾼 천명님 줄

- `CombatAttackPlayback`의 옛 `TryBegin` 안 `EndSource(e, version);` 한 줄(HEAD `:197`, `d4a6d6fd`)이 입구째 지워졌습니다.
  - 코드 모션 공격의 끝은 이제 조립 실행이 냅니다: 모션 끝 → 항목 끝 → 실행 `Finish` → `EndSource`.
  - 부르는 순서와 세대 확인은 옛 것과 같습니다(계획 Gc.6의 6번).
- 시험 쪽에서 바꾼 줄은 모두 우리 줄입니다(묶음 끝 검토가 blame으로 확인).

## 5. 답이 필요한 것

없습니다. 고치신 뒤 Unity에서 이 시험 파일을 돌려 결과만 알려 주세요.
