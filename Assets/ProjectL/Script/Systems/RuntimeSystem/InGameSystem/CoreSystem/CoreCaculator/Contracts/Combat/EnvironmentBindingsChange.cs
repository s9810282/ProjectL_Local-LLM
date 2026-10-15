using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>원본 추가와 같은 명령에서 검증·게시할 환경 분류 변경이다.</summary>
    public sealed class EnvironmentBindingsChange
    {
        public EnvironmentSemanticBindings Expected { get; }
        public EnvironmentSemanticBindings Final { get; }
        public EnvironmentBindingsChange(EnvironmentSemanticBindings expected, EnvironmentSemanticBindings final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
        }
    }
}
