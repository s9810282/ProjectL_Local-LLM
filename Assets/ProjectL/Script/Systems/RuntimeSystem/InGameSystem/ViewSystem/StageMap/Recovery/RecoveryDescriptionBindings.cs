using System;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery
{
    /// <summary>회복 종류별 설명 토큰입니다. 설정량과 Core가 계산한 실제 예상량을 구분합니다.</summary>
    internal static class RecoveryDescriptionBindings
    {
        private static readonly DescriptionToken<RecoveryOptionState> ProjectedHeal = new("예상회복량", value => value.ProjectedHealAmount);
        private static readonly DescriptionToken<RecoveryOptionState>[] Flat =
        {
            new("회복량", value => value.Option.Heal.Value), ProjectedHeal
        };
        private static readonly DescriptionToken<RecoveryOptionState>[] Percent =
        {
            new("회복률", value => value.Option.Heal.Value), ProjectedHeal
        };
        private static readonly DescriptionToken<RecoveryOptionState>[] Full = { ProjectedHeal };
        private static readonly DescriptionToken<RecoveryOptionState>[] MaxHealth =
        {
            new("최대체력증가량", value => value.Option.MaxHealthIncrease),
            new("예상최대체력증가량", value => value.ProjectedMaxHealthIncrease)
        };

        internal static string Format(string description, RecoveryOptionState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            string context = $"RecoveryOptionVisual.{source.Option.Id.Value}.Description";
            DescriptionToken<RecoveryOptionState>[] tokens = source.Option.EffectKind switch
            {
                RecoveryEffectKind.MaxHealthGrowth => MaxHealth,
                RecoveryEffectKind.Heal => source.Option.Heal.Mode switch
                {
                    RecoveryHealMode.Flat => Flat,
                    RecoveryHealMode.PercentOfMax => Percent,
                    RecoveryHealMode.Full => Full,
                    _ => throw new InvalidOperationException($"[{context}] 회복 방식이 맞지 않습니다: {source.Option.Heal.Mode}.")
                },
                _ => throw new InvalidOperationException($"[{context}] 효과 종류가 맞지 않습니다: {source.Option.EffectKind}.")
            };
            return new DescriptionTemplate<RecoveryOptionState>(context, (description ?? string.Empty).Replace("\\n", "\n"), tokens)
                .Format(source);
        }
    }
}
