using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 클립의 시작 기준으로 지원하는 선행 발동 목록입니다. 발동 여부나 효과량은 계산하지 않습니다.
    /// 설정 선택지와 결과 대응기가 이 목록을 함께 사용합니다.
    /// </summary>
    internal static class CombatPreAttackTraitKinds
    {
        private static readonly (TraitKey Trait, TraitActivationKind Kind)[] _entries =
        {
            (TraitKey.Pull, TraitActivationKind.PullApplied),
            (TraitKey.Vanguard, TraitActivationKind.AttackGranted),
            (TraitKey.CleanSweep, TraitActivationKind.AttackGranted),
        };

        internal static IReadOnlyList<TraitActivationKind> Choices { get; } = CreateChoices();

        internal static bool Supports(TraitKey trait, TraitActivationKind kind)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Trait == trait && _entries[i].Kind == kind)
                    return true;
            }
            return false;
        }

        internal static bool SupportsKind(TraitActivationKind kind)
        {
            for (int i = 0; i < Choices.Count; i++)
            {
                if (Choices[i] == kind)
                    return true;
            }
            return false;
        }

        private static IReadOnlyList<TraitActivationKind> CreateChoices()
        {
            var kinds = new List<TraitActivationKind>();
            for (int i = 0; i < _entries.Length; i++)
            {
                if (!kinds.Contains(_entries[i].Kind))
                    kinds.Add(_entries[i].Kind);
            }
            return Array.AsReadOnly(kinds.ToArray());
        }
    }
}
