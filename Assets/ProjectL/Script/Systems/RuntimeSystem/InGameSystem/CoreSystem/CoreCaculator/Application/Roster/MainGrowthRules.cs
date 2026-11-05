using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 메인 랜덤 성장의 유일한 위치다(경험치·레벨업 설계 §3.4, 사용자 결정 X4·X5).
    /// 도달한 레벨마다 최대 체력·공격력·방어력 중 하나를 균등 확률로 고르고, 결과는 지속 상태에 더한다.
    /// 선택은 (전투 seed, 메인 보유 ID, 도달 레벨)의 순수 함수라 새 난수 상태가 없고, 같은 입력이면 같은 선택이다.
    /// </summary>
    internal static class MainGrowthRules
    {
        /// <summary><paramref name="reachedLevel"/>에 도달했을 때 고르는 성장이다.</summary>
        internal static MainGrowthKind Choose(int seed, CharacterInstanceId mainId, int reachedLevel)
        {
            if (mainId.IsEmpty)
                throw new ArgumentException("메인 보유 ID가 필요합니다.", nameof(mainId));
            if (reachedLevel < 2)
                throw new ArgumentOutOfRangeException(nameof(reachedLevel));

            unchecked
            {
                // 세 입력을 섞은 뒤 SplitMix64 마무리로 고르게 퍼뜨린다. 세 선택지의 확률은 균등하다.
                ulong z = (uint)seed * 0x9E3779B97F4A7C15UL ^ (uint)mainId.Value * 0xC2B2AE3D27D4EB4FUL ^
                          (uint)reachedLevel * 0x165667B19E3779F9UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (MainGrowthKind)(int)(z % 3UL + 1UL);
            }
        }

        /// <summary>성장 하나를 지속 상태에 더한다. 최대 체력이 올라도 현재 체력은 그대로다(X4).</summary>
        internal static MainCharacterPersistentState Apply(MainCharacterPersistentState state, MainGrowthKind kind,
            LevelProgressionSpec spec)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!spec.IsEnabled)
                throw new InvalidOperationException("꺼진 경험치 설정에는 메인 성장량이 없습니다.");

            return kind switch
            {
                MainGrowthKind.MaxHealth => state.With(
                    persistentMaxHealth: checked(state.PersistentMaxHealth + spec.MainMaxHealthGrowth)),
                MainGrowthKind.Attack => state.With(
                    persistentAttack: checked(state.PersistentAttack + spec.MainAttackGrowth)),
                MainGrowthKind.Defense => state.With(
                    persistentDefense: checked(state.PersistentDefense + spec.MainDefenseGrowth)),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
    }
}
