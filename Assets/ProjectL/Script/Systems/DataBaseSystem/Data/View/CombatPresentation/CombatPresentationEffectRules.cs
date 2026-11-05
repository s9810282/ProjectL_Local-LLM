using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 연출 이펙트 종류 규칙의 원본입니다(투사체 접촉 이펙트 설계 IE2, 계획 P1). 값 칸 검사(이펙트 큐 항목)와
    /// 내는 쪽 검사(이펙트 창구)가 이 규칙을 부릅니다. 종류 자체의 원본은 Render DB <see cref="EffectType"/>이고,
    /// 그 표에 실제로 등록됐는지는 첫 재생 때 이펙트 시스템이 봅니다.
    /// </summary>
    internal static class CombatPresentationEffectRules
    {
        /// <summary><see cref="IsKnown"/> 규칙 문구입니다.</summary>
        internal const string KnownRule = "이펙트는 정의된 EffectType이어야 합니다(Empty = 이펙트 없음).";

        /// <summary><see cref="IsPlayable"/> 규칙 문구입니다.</summary>
        internal const string PlayableRule = "낼 이펙트는 Empty가 아닌 정의된 EffectType이어야 합니다.";

        /// <summary>값 칸에 둘 수 있는 종류인지입니다. <see cref="EffectType.Empty"/>(이펙트 없음)도 맞는 값입니다.</summary>
        internal static bool IsKnown(EffectType effect) => System.Enum.IsDefined(typeof(EffectType), effect);

        /// <summary>실제로 낼 수 있는 종류인지입니다. 정의됐고 <see cref="EffectType.Empty"/>가 아니어야 합니다.</summary>
        internal static bool IsPlayable(EffectType effect) => effect != EffectType.Empty && IsKnown(effect);
    }
}
