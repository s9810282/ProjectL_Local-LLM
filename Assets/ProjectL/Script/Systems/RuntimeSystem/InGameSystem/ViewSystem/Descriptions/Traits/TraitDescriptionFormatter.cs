using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Descriptions.Traits
{
    /// <summary>특성 문장을 같은 표시 시점의 설정으로 완성합니다. 가용성 안내와 화면 수명은 호출자가 맡습니다.</summary>
    internal static class TraitDescriptionFormatter
    {
        internal static TraitSpec ReadSettings(TraitSettingsSet settings, TraitKey key) =>
            settings != null && settings.TryGet(key, out var source) ? source : null;

        internal static string Format(TraitVisualData visual, TraitSpec source)
        {
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            string context = $"TraitVisual.{visual.Key}.Description";
            if (source != null && source.TraitKey != visual.Key)
                throw new InvalidOperationException($"[{context}] 표시 특성과 설정의 키가 다릅니다: {source.TraitKey}.");

            if (!TraitDescriptionBindings.TryGet(visual.Key, out var binding))
            {
                // 수치가 없거나 별도 표시 설계가 필요한 종류입니다. 임의 토큰은 등록 오류로 알립니다.
                return new DescriptionTemplate<TraitSpec>(context, Normalize(visual.Description)).Format(source);
            }

            return binding.Prepare(context, Normalize(visual.Description)).Format(source);
        }

        private static string Normalize(string text) => (text ?? string.Empty).Replace("\\n", "\n");
    }
}
