using System;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Descriptions.Traits
{
    /// <summary>한 특성의 토큰을 공개 설정에 연결합니다. 개체 값과 화면 수명은 보관하지 않습니다.</summary>
    internal sealed class TraitDescriptionBinding
    {
        private readonly DescriptionToken<TraitSpec>[] _tokens;

        internal TraitDescriptionBinding(params DescriptionToken<TraitSpec>[] tokens)
        {
            _tokens = (DescriptionToken<TraitSpec>[])(tokens ?? throw new ArgumentNullException(nameof(tokens))).Clone();
        }

        internal DescriptionTemplate<TraitSpec> Prepare(string context, string text) =>
            new(context, text, _tokens);
    }
}
