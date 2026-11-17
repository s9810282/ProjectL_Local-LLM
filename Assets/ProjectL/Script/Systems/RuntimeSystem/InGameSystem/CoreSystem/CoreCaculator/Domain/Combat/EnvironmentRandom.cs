using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>환경 콘텐츠가 함께 쓰는 결정적 난수다. 상태는 호출자가 명령 진행값으로 보관한다.</summary>
    internal static class EnvironmentRandom
    {
        // 같은 전투 seed에서 시작하는 아이템 난수열과 겹치지 않도록 환경 난수열에만 쓰는 값이다.
        private const uint StreamSalt = 0x7F4A7C15u;

        /// <summary>
        /// 전투 seed에서 환경 난수 시작값을 만든다. 0을 포함한 모든 seed가 유효하며 같은 seed는 같은 시작값이다.
        /// </summary>
        internal static uint InitialState(int battleSeed)
        {
            uint z = unchecked((uint)battleSeed ^ StreamSalt);
            z = unchecked((z ^ (z >> 16)) * 0x7FEB352Du);
            z = unchecked((z ^ (z >> 15)) * 0x846CA68Bu);
            return z ^ (z >> 16);
        }

        /// <summary>후보가 하나뿐이면 난수를 쓰지 않고 0을 반환한다.</summary>
        internal static int Choose(ref uint state, int count) => count == 1 ? 0 : NextIndex(ref state, count);

        internal static int NextIndex(ref uint state, int count) => EnvironmentRandomSequence.NextIndex(ref state, count);
    }
}
