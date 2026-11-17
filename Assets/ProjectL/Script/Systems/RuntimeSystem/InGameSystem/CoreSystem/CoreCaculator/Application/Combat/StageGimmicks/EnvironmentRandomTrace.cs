using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    /// <summary>
    /// 한 전이 안에서 환경 난수의 알려진 값을 사건 순서대로 따라가는 검증용 추적이다. 기믹 검증기는 난수 흐름을 이 한 곳에서 읽는다.
    /// 값을 선언하는 사건(<see cref="IEnvironmentRandomEvidence"/>, <see cref="CombatEnvironmentRandomEvent"/>)은 알려진 값을 갱신하고,
    /// 값을 남기지 않고 난수를 쓸 수 있는 사건(<see cref="ConsumesUndeclaredRandom"/>) 뒤에는 값을 모르는 것으로 둔다.
    /// 값을 모를 때의 비교는 통과시키며, 선언 사건 자체의 재계산 검사는 각 검증기가 맡는다.
    /// </summary>
    internal sealed class EnvironmentRandomTrace
    {
        private uint _known;
        private bool _unknown;
        private CombatEvent _lastDeclaring;

        internal EnvironmentRandomTrace(uint start)
        {
            _known = start;
        }

        /// <summary>지금까지 관찰한 사건 뒤의 값이 <paramref name="expected"/>와 같거나, 값을 모르면 true다.</summary>
        internal bool Matches(uint expected) => _unknown || expected == _known;

        /// <summary>
        /// <paramref name="declaring"/>이 마지막으로 값을 정한 사건이고 그 뒤에 난수를 쓴 사건이 없으면
        /// 전이의 최종 값이 그 사건의 최종 값과 같아야 한다. 다른 경우는 판단하지 않는다(true).
        /// </summary>
        internal bool EndsWith(CombatEvent declaring, uint finalRandomState) =>
            _unknown || !ReferenceEquals(_lastDeclaring, declaring) || finalRandomState == _known;

        /// <summary>사건을 검증한 뒤 부른다. 사건 순서대로 한 번씩 관찰해야 한다.</summary>
        internal void Observe(CombatEvent occurrence)
        {
            if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));
            switch (occurrence)
            {
                case IEnvironmentRandomEvidence evidence when evidence.FinalRandomState is uint final:
                    Declare(occurrence, final);
                    break;
                case CombatEnvironmentRandomEvent selected:
                    Declare(occurrence, selected.FinalState);
                    break;
                default:
                    if (ConsumesUndeclaredRandom(occurrence)) _unknown = true;
                    break;
            }
        }

        /// <summary>
        /// 환경 난수를 쓸 수 있지만 사용 전후 값을 사건에 남기지 않는 기존 경로다. 이 목록이 그 지식의 유일한 원본이다.
        /// 옛 기믹(토네이도·하붑·낙석)과 진영 시작 밖의 사냥꾼 표식 재선정이 해당한다. 새 난수 사용처는 값을 선언하는 사건을 쓰고,
        /// 그럴 수 없을 때만 여기에 더한다.
        /// </summary>
        private static bool ConsumesUndeclaredRandom(CombatEvent occurrence) => occurrence switch
        {
            CombatTornadoGimmickEvent or CombatTornadoEvent or CombatHaboobEvent or CombatRockfallEvent => true,
            CombatEffectsEvent effects => effects.Traits.Any(t => t.TraitActivationKind == TraitActivationKind.HunterTargetSelected),
            _ => false
        };

        private void Declare(CombatEvent occurrence, uint final)
        {
            _known = final;
            _unknown = false;
            _lastDeclaring = occurrence;
        }
    }
}
