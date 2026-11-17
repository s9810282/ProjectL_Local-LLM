using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 연출 하나의 모양입니다(특성 발동 연출 설계 TA5). 공격 조립·재생·종류 전략이 공격 사건 대신 이 값을 받습니다.
    /// 공격 사건이 있는 공격은 <see cref="Of"/>로 그 공격 결과의 칸을 그대로 옮기고, 공격 사건이 없는 공격(특성 추가 공격)은 <see cref="ForChildAttack"/>로
    /// 그 발동 사실의 칸을 옮깁니다. 칸·방향을 View가 계산하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 출처 사건(<see cref="Source"/>)은 맞는 순간 대응·사건 완료·사건 소리의 열쇠입니다. 공격 사건이면 그 사건, 특성 추가 공격이면 그 발동 사건입니다.
    /// 사건 완료를 언제 알리는지(<see cref="Completion"/>)도 이 값이 정하고, 공격 조립 둘(코드 모션·클립)이 그대로 씁니다.
    /// 공격 결과가 없는 동작(포효의 정면 공격)은 이 값 없이(null) 부릅니다.
    /// </remarks>
    internal sealed class CombatAttackShape
    {
        /// <param name="source">출처 사건입니다(필수).</param>
        /// <param name="attackerId">공격자입니다(필수).</param>
        /// <param name="direction">공격 방향입니다. 목표 칸이 없을 때 목표점 해석과, 접촉 자리가 수평 길이 없이 정해질 때의 회전에 씁니다.</param>
        /// <param name="deliveryCell">실제 타격 칸입니다(끌어오기면 Core가 준 도착 칸). 방향·전체 지정이면 null입니다.</param>
        /// <param name="affectedCells">영향 칸입니다. 목표 칸이 없을 때 방향 앞 가장 먼 칸을 목표로 씁니다.</param>
        /// <param name="primaryTargetId">주 대상입니다. 직선 투사체의 몸 접촉 진행률에 씁니다. 없으면 비어 있습니다.</param>
        /// <param name="completion">출처 사건 완료를 언제 알리는지입니다(히트 타이밍 설계 H3 정책 표).</param>
        /// <param name="reactions">그 공격 안 반응(가로채기가 있는 발동 사실)입니다(유닛 행동 설계 AA6, 계획 TP-25). 없으면 빈 목록입니다.</param>
        /// <param name="piercingShot">Core 관통 기록입니다(관통·죽음의 손 View 설계 §3.1). 관통이 아니면 null입니다.</param>
        private CombatAttackShape(CombatEvent source, EntityId attackerId, Rotation4 direction, CellCoord? deliveryCell,
            ReadOnlyMemory<CellCoord> affectedCells, EntityId primaryTargetId, CombatHitCompletion completion,
            IReadOnlyList<TraitActivationSnapshot> reactions, PiercingShotDelivery piercingShot,
            IReadOnlyList<CellCoord> skillPath = null, IReadOnlyList<CombatShotRouteContact> skillContacts = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (attackerId.IsEmpty)
                throw new ArgumentException("[CombatAttackShape] 공격자가 있어야 합니다.", nameof(attackerId));
            if (!CombatEnumValues.IsDefined<CombatHitCompletion>((int)completion))
                throw new ArgumentOutOfRangeException(nameof(completion));
            AttackerId = attackerId;
            Direction = direction;
            DeliveryCell = deliveryCell;
            AffectedCells = affectedCells;
            PrimaryTargetId = primaryTargetId;
            Completion = completion;
            Reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
            ContactTargetId = ContactTargetOf(primaryTargetId, reactions);
            PiercingShot = piercingShot;
            SkillPath = skillPath;
            SkillContacts = skillContacts ?? Array.Empty<CombatShotRouteContact>();
        }

        /// <summary>출처 사건입니다.</summary>
        internal CombatEvent Source { get; }

        /// <summary>공격자입니다.</summary>
        internal EntityId AttackerId { get; }

        /// <summary>공격 방향입니다.</summary>
        internal Rotation4 Direction { get; }

        /// <summary>실제 타격 칸입니다. 없으면 null입니다.</summary>
        internal CellCoord? DeliveryCell { get; }

        /// <summary>영향 칸입니다.</summary>
        internal ReadOnlyMemory<CellCoord> AffectedCells { get; }

        /// <summary>주 대상입니다. 없으면 비어 있습니다.</summary>
        internal EntityId PrimaryTargetId { get; }

        /// <summary>출처 사건 완료를 언제 알리는지입니다. 지금은 공격 사건·특성 추가 공격 모두 첫 유효 해제입니다(특성 시스템 View 계획 Tb.4).</summary>
        internal CombatHitCompletion Completion { get; }

        /// <summary>
        /// 그 공격 안 반응 발동 사실입니다(가로채기 = 대신 맞기만, 유닛 행동 설계 AA6). 공격 사건이면 그 공격 결과의 발동 목록, 추가 공격이면 그 추가 공격 결과의
        /// 반응 목록 중 가로채기가 있는 것(<see cref="IsInterception"/>)만 담습니다. 사실을 그대로 들고 다니고 새 값 형식을 만들지 않습니다(계획 TP-25).
        /// </summary>
        internal IReadOnlyList<TraitActivationSnapshot> Reactions { get; }

        /// <summary>
        /// 직선 투사체의 몸 접촉을 잴 대상입니다. 주 대상이 가로채졌으면 실제로 맞은 수호자, 아니면 주 대상입니다(유닛 행동 설계 P5).
        /// 수호자가 옮긴 자리에 서 있으면 그 자리 몸으로 잽니다(몸 조회가 서는 자리를 봄, 설계 AA3).
        /// </summary>
        internal EntityId ContactTargetId { get; }

        /// <summary>
        /// Core 관통 기록입니다(조우 대상·순서·칸, 끝 칸, 멈춘 이유). 관통 탄은 조우마다 그 대상을 해제하고 끝 칸에서 사라집니다
        /// (관통·죽음의 손 View 설계 §3.1, <see cref="CombatProjectileContacts"/>). 관통이 아니거나 추가 공격이면 null입니다.
        /// </summary>
        internal PiercingShotDelivery PiercingShot { get; }

        /// <summary>
        /// 플레이어 스킬 사건 51의 실제 투사체 경로 칸입니다(시전자 칸 제외, 전투 스킬 View 설계 SK3). 경로가 있으면 꺾인 직선 비행이고, 없거나 스킬이 아니면 null입니다.
        /// 재생 길(경로 비행·영역)은 이 값의 유무가 정합니다(설계 P5).
        /// </summary>
        internal IReadOnlyList<CellCoord> SkillPath { get; }

        /// <summary>스킬 사건 51의 대상별 접촉 칸입니다(접촉 순번 순, <c>Hits[].Contacts[].DeliveryIndex</c> → <c>ImpactCells</c>). 경로가 없으면 비어 있습니다.</summary>
        internal IReadOnlyList<CombatShotRouteContact> SkillContacts { get; }

        /// <summary>공격 사건의 모양입니다. 타격 칸 규칙(끌어오기면 도착 칸)은 <see cref="CombatAttackTarget"/>의 <c>DeliveryCell</c> 한 곳입니다.</summary>
        internal static CombatAttackShape Of(CombatAttackEvent attack)
        {
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            AttackResult result = attack.Attack;
            return new CombatAttackShape(attack, result.AttackerId, result.AttackDirection, CombatAttackTarget.DeliveryCell(result),
                result.AffectedCells, result.PrimaryTargetId, CombatHitCompletion.FirstRelease, InterceptionsOf(result.TraitActivations.Span),
                result.PiercingShot);
        }

        /// <summary>
        /// 추가 공격(난사·2연발·연쇄 폭발·행운의 탄환)의 모양입니다(특성 시스템 View 설계 TS4, 계획 Tb.4). 칸·방향은 모두 Core 사실에서 옮깁니다:
        /// 공격자 = 발동 유닛, 방향 = <c>AttackDirection</c>, 타격 칸 = <c>TargetCell</c>(방향 지정이면 없음), 영향 칸 = <c>TargetCells</c>,
        /// 주 대상 = <c>TargetId</c>(대상이 하나일 때, 원래 지정 대상). 출처는 그 발동 사건이고, 뒤따르는 피해는 그 공격 그룹으로 출처에 맺히므로
        /// 사건 완료는 보통 공격처럼 첫 유효 해제입니다(<see cref="CombatHitCompletion.FirstRelease"/>). 아무도 안 맞으면(빈 2연발) 전체 종료 때입니다.
        /// </summary>
        /// <exception cref="ArgumentException">그 발동이 추가 공격 시작이 아닐 때입니다.</exception>
        /// <exception cref="InvalidOperationException">추가 공격 시작에 방향이 없을 때입니다(Core 계약, 계획 TP-6).</exception>
        internal static CombatAttackShape ForChildAttack(CombatEffectsEvent start, TraitActivationSnapshot trait)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (trait == null) throw new ArgumentNullException(nameof(trait));
            if (trait.ChildAttack == null)
                throw new ArgumentException("[CombatAttackShape] 추가 공격 시작 발동이 아닙니다.", nameof(trait));
            if (!trait.AttackDirection.HasValue)
                throw new InvalidOperationException($"[CombatAttackShape] 추가 공격 시작에 방향이 없습니다(Core 계약). 특성: {trait.TraitKey}");
            var cells = new CellCoord[trait.TargetCells.Count];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = trait.TargetCells[i];
            return new CombatAttackShape(start, trait.SourceUnitId, trait.AttackDirection.Value, trait.TargetCell, cells,
                trait.TargetId ?? EntityId.Empty, CombatHitCompletion.FirstRelease, InterceptionsOf(trait.ChildAttack.Reactions),
                piercingShot: null);
        }

        /// <summary>
        /// 플레이어 스킬 사건 51의 모양입니다(전투 스킬 View 설계 SK3). 공격자 = 시전자, 영향 칸 = <c>AffectedCells</c>, 주 대상 없음, 완료 = 첫 유효 해제.
        /// 투사체 경로가 있으면 타격 칸은 첫 경로 칸(시전자가 그쪽을 봄)이고 경로·대상별 접촉 칸을 싣습니다. 경로가 없으면(범위 폭발) 타격 칸 없이 시전자 방향입니다.
        /// </summary>
        /// <param name="casterRotation">시전자의 보인 방향입니다. 경로가 없을 때 공격 방향으로 씁니다.</param>
        internal static CombatAttackShape ForPlayerSkill(CombatPlayerSkillAttackEvent skill, Rotation4 casterRotation)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            PlayerSkillAttackResult attack = skill.Attack;
            var cells = new CellCoord[attack.AffectedCells.Count];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = attack.AffectedCells[i];
            if (attack.ProjectilePath.Count == 0)
                return new CombatAttackShape(skill, attack.ActorId, casterRotation, null, cells, EntityId.Empty, CombatHitCompletion.FirstRelease,
                    Array.Empty<TraitActivationSnapshot>(), piercingShot: null);

            // 접촉 순번 순으로 (실제 수신자, 접촉 칸)을 모읍니다. 접촉 칸은 늘 경로 안입니다(Core 생성자 검사).
            var ordered = new List<(int Delivery, CombatShotRouteContact Contact)>();
            for (int h = 0; h < attack.Hits.Count; h++)
            {
                AttackHitResult hit = attack.Hits[h];
                for (int c = 0; c < hit.Contacts.Count; c++)
                {
                    int delivery = hit.Contacts[c].DeliveryIndex;
                    if (attack.ImpactCells.TryGetValue(delivery, out CellCoord cell))
                        ordered.Add((delivery, new CombatShotRouteContact(hit.TargetId, cell)));
                }
            }
            ordered.Sort((left, right) => left.Delivery.CompareTo(right.Delivery));
            var contacts = new CombatShotRouteContact[ordered.Count];
            for (int i = 0; i < contacts.Length; i++)
                contacts[i] = ordered[i].Contact;
            var path = new CellCoord[attack.ProjectilePath.Count];
            for (int i = 0; i < path.Length; i++)
                path[i] = attack.ProjectilePath[i];
            return new CombatAttackShape(skill, attack.ActorId, casterRotation, path[0], cells, EntityId.Empty, CombatHitCompletion.FirstRelease,
                Array.Empty<TraitActivationSnapshot>(), piercingShot: null, skillPath: path, skillContacts: contacts);
        }

        /// <summary>
        /// 그 발동이 가로채기(대신 맞기)인지의 판정 한 곳입니다(묶음 끝 검토 L1). Core 사실의 가로채기 칸이 있으면 참입니다.
        /// 공격 모양의 반응 목록·포효 결과 경고·특성 처리기(사건 목록 안 발동은 행동하지 않음)가 이것을 부릅니다.
        /// </summary>
        internal static bool IsInterception(TraitActivationSnapshot trait) => trait?.Interception != null;

        /// <summary>발동 목록에 가로채기가 하나라도 있는지입니다.</summary>
        internal static bool HasInterception(ReadOnlySpan<TraitActivationSnapshot> activations)
        {
            for (int i = 0; i < activations.Length; i++)
                if (IsInterception(activations[i])) return true;
            return false;
        }

        // 공격 결과 발동 목록 중 가로채기가 있는 것입니다(Core는 가로채기를 공격 결과 안 반응으로만 둠, 유닛 행동 설계 §1의 2).
        private static IReadOnlyList<TraitActivationSnapshot> InterceptionsOf(ReadOnlySpan<TraitActivationSnapshot> activations)
        {
            List<TraitActivationSnapshot> found = null;
            for (int i = 0; i < activations.Length; i++)
            {
                if (!IsInterception(activations[i])) continue;
                (found ??= new List<TraitActivationSnapshot>()).Add(activations[i]);
            }
            return found ?? (IReadOnlyList<TraitActivationSnapshot>)Array.Empty<TraitActivationSnapshot>();
        }

        // 추가 공격 결과의 반응 목록 중 가로채기가 있는 것입니다(위와 같은 규칙, 목록 꼴).
        private static IReadOnlyList<TraitActivationSnapshot> InterceptionsOf(IReadOnlyList<TraitActivationSnapshot> activations)
        {
            List<TraitActivationSnapshot> found = null;
            for (int i = 0; i < activations.Count; i++)
            {
                if (!IsInterception(activations[i])) continue;
                (found ??= new List<TraitActivationSnapshot>()).Add(activations[i]);
            }
            return found ?? (IReadOnlyList<TraitActivationSnapshot>)Array.Empty<TraitActivationSnapshot>();
        }

        // 주 대상을 지킨 가로채기가 있으면 실제로 맞은 유닛입니다(희생은 수호자 = 실제 수신자, Core 검사).
        private static EntityId ContactTargetOf(EntityId primaryTargetId, IReadOnlyList<TraitActivationSnapshot> reactions)
        {
            if (primaryTargetId.IsEmpty) return primaryTargetId;
            for (int i = 0; i < reactions.Count; i++)
            {
                AttackInterceptionSnapshot interception = reactions[i]?.Interception;
                if (interception != null && interception.ProtectedId == primaryTargetId)
                    return interception.ActualRecipientId;
            }
            return primaryTargetId;
        }
    }
}
