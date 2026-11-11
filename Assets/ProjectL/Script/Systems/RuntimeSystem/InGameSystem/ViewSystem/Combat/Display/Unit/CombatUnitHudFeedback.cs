using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 HUD의 일시 피드백입니다(연출 조립 설계 §9 I-2, 결정 Q-R15 A). 사건 재생의 피해 숫자와 입력 미리보기의 예상 체력을 보이고,
    /// 어느 대상에 보였는지를 기억해 지웁니다. 표시는 <see cref="ICombatUnitTargets"/>로 찾고 HUD 계약(<see cref="ICombatUnitHudTarget"/>)으로만 씁니다.
    /// </summary>
    /// <remarks>
    /// <see cref="FieldUnitPresenter.HudFeedback"/>이 한 번 만듭니다. 사실에서 파생해 매번 다시 쓰는 HUD 상태(체력·상태 칸·공격력 등)는
    /// 사실 그리기(<see cref="FieldUnitPresenter.RenderFacts"/>)가 맡습니다. 사실 그리기는 시작할 때 예상 체력을 지웁니다.
    /// </remarks>
    internal sealed class CombatUnitHudFeedback
    {
        private readonly ICombatUnitTargets _targets;
        private readonly HashSet<EntityId> _healthPreviewTargets = new();

        internal CombatUnitHudFeedback(ICombatUnitTargets targets) =>
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));

        /// <summary>사건 재생 시 확정된 HP 감소량을 지금 켜진 유닛 HUD에 전달합니다. 0 이하는 보이지 않습니다.</summary>
        internal void ShowDamage(EntityId id, int damage)
        {
            if (damage <= 0)
                return;
            if (_targets.TryGetHud(id, out var target) && target.IsActive)
                target.ShowDamage(damage);
        }

        /// <summary>Core가 제공한 예상 잔여 체력을 기존 대상에만 표시합니다. 이전 대상은 먼저 복원합니다.</summary>
        internal void ShowAttackHealthPreview(ReadOnlyMemory<AttackTargetPreview> targets)
        {
            ClearAttackHealthPreview();
            foreach (AttackTargetPreview preview in targets.Span)
            {
                if (!_targets.TryGetHud(preview.TargetId, out var target))
                    continue;
                target.SetHealthPreview(ShownHealthOf(preview));
                _healthPreviewTargets.Add(preview.TargetId);
            }
        }

        // 미리보기에 보일 예상 체력의 규칙 한 곳입니다(재구성 연출 설계 RE3·계획 TP-12). 재구성될 대상은 Core 예상 체력이 최대라 그대로 보이면
        // "피해 없음"처럼 보이므로 치명 예상인 0을 보입니다. 재구성 여부는 Core 칸이 정하고 View는 체력 수로 짐작하지 않습니다.
        private static int ShownHealthOf(AttackTargetPreview preview) => preview.TargetReconstructs ? 0 : preview.TargetHealthAfter;

        /// <summary>공격 대상의 예상 체력을 복원합니다.</summary>
        internal void ClearAttackHealthPreview()
        {
            foreach (EntityId id in _healthPreviewTargets)
            {
                if (_targets.TryGetHud(id, out var target))
                    target.SetHealthPreview(null);
            }
            _healthPreviewTargets.Clear();
        }

        /// <summary>풀로 돌아간 표시를 예상 체력 대상에서 뺍니다. 표시 복원은 풀 정리가 담당합니다.</summary>
        internal void OnReturned(EntityId id) => _healthPreviewTargets.Remove(id);
    }
}
