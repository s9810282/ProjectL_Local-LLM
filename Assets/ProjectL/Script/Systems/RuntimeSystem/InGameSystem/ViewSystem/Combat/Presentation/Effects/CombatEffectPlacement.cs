using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 이펙트 창구(<see cref="CombatEffectWindow"/>)가 이펙트 하나를 어떻게 놓고 언제 거두나입니다(이펙트 끝 규칙 설계 EL5). 값은 셋입니다:
    /// 따라갈 유닛(비면 낸 자리에 머묾), 시간 끝(0보다 크면 낸 뒤 그 초에 거둠), 부르는 쪽이 거둠(손잡이로).
    /// 셋 다 아니면 자연 끝입니다(프리팹이 스스로 멈출 때).
    /// </summary>
    /// <remarks>
    /// 큐 값 → 놓기 값 변환은 <see cref="ForCue"/> 한 곳입니다(계획 TP-E26). 큐의 끝 열거(<see cref="CombatEffectEnd"/>)와 이 값의 끝은 층이 다릅니다:
    /// 큐는 "데이터가 고른 끝", 이 값은 "창구가 할 일"입니다. 오래 보이는 이펙트(상태 동안·보유 동안)는 부르는 쪽이 거두므로 <see cref="Held"/>로 냅니다.
    /// </remarks>
    internal readonly struct CombatEffectPlacement
    {
        private CombatEffectPlacement(EntityId follow, float seconds, bool held, bool actionBound = false)
        {
            Follow = follow;
            Seconds = seconds;
            Held = held;
            ActionBound = actionBound;
        }

        /// <summary>낸 뒤 몸 위치를 따라갈 유닛입니다(위치만, 이펙트 따라가기 설계 FE1). 비면 낸 자리에 머뭅니다.</summary>
        internal EntityId Follow { get; }

        /// <summary>0보다 크면 낸 뒤 이 초가 지나면 거둡니다. 0이면 시간 끝이 아닙니다.</summary>
        internal float Seconds { get; }

        /// <summary>부르는 쪽이 손잡이(<see cref="CombatEffectHandle"/>)로 거두는지입니다. 끝나지 않는 것이 맞아 경고하지 않습니다(설계 EL6).</summary>
        internal bool Held { get; }

        /// <summary>
        /// 큐의 행동 끝(<see cref="CombatEffectEnd.UnitAction"/>)에서 온 부르는 쪽 거둠인지입니다. 발동의 행동 묶음(<see cref="CombatEffectActionGroup"/>)이 거두고,
        /// 늦어도 결과 끝에 창구가 거둡니다(계획 TP-E4).
        /// </summary>
        internal bool ActionBound { get; }

        /// <summary>프리팹이 스스로 멈출 때 끝나는지입니다(시간 끝도, 부르는 쪽이 거둠도 아님).</summary>
        internal bool IsNatural => !Held && Seconds <= 0f;

        /// <summary>낸 자리에 머물고 자연히 끝납니다.</summary>
        internal static CombatEffectPlacement Stay => default;

        /// <summary><paramref name="unit"/>의 몸 위치를 따라가고 자연히 끝납니다. 오래 보이는 이펙트는 <see cref="AsHeld"/>를 붙여 부르는 쪽이 거둡니다.</summary>
        internal static CombatEffectPlacement Following(EntityId unit)
        {
            if (unit.IsEmpty) throw new ArgumentException("따라갈 유닛이 비어 있습니다.", nameof(unit));
            return new CombatEffectPlacement(unit, 0f, held: false);
        }

        /// <summary>
        /// 큐 하나의 놓기 값입니다. 따라갈지는 큐(<see cref="CombatEffectCue.FollowsUnit"/>)가 정하고, 몸 유닛을 모르면 머뭅니다.
        /// 끝은 큐의 끝입니다(자연·시간·행동 끝 = 부르는 쪽이 거둠, 계획 TP-E26).
        /// </summary>
        /// <param name="unit">그 자리가 몸 자리일 때 몸의 유닛입니다. 지점·칸·접촉 자리는 비웁니다.</param>
        internal static CombatEffectPlacement ForCue(CombatEffectCue cue, EntityId unit)
        {
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            EntityId follow = cue.FollowsUnit ? unit : EntityId.Empty;
            return cue.End switch
            {
                CombatEffectEnd.Natural => new CombatEffectPlacement(follow, 0f, held: false),
                CombatEffectEnd.Seconds => new CombatEffectPlacement(follow, cue.EndSeconds, held: false),
                CombatEffectEnd.UnitAction => new CombatEffectPlacement(follow, 0f, held: true, actionBound: true),
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue.End, "정의되지 않은 이펙트 끝입니다."),
            };
        }

        /// <summary>같은 따라가기로, 부르는 쪽이 손잡이로 거두는 놓기 값입니다. 시간 끝은 버립니다.</summary>
        internal CombatEffectPlacement AsHeld() => new(Follow, 0f, held: true);
    }
}
