using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 연발 앞 탄의 접촉 피격이 그 유닛의 다음 피격·사망에 끊기는 규칙입니다(2026-11-17 사용자 "A 끊고 다시", 피격 구조 정리 설계 HC3).
    /// 다음 피격·사망 사건은 이 실행을 기다리지 않고(통과), 시작하며 이 실행을 끊은 뒤 처음부터 다시 냅니다(끊기).
    /// 끊는 사건인지의 규칙은 <see cref="CombatCharacterHits.RestartsHit"/> 하나입니다.
    /// </summary>
    /// <remarks>
    /// 끊긴 실행은 완료를 알리지 않으므로 사건 완료를 기다리는 시작에는 쓰지 않습니다(<see cref="Cuttable"/>, 재생기가 거절).
    /// 자세는 빌려도 됩니다(코드 피격 움직임을 내는 접촉 피격). 필드가 없는 객체라 <see cref="Instance"/> 하나를 함께 씁니다(설계 P2).
    /// </remarks>
    internal sealed class CombatNextHitPass : ICombatRunSubjectPass
    {
        internal static readonly CombatNextHitPass Instance = new();

        private CombatNextHitPass()
        {
        }

        public bool Passes(CombatEvent item) => IsCutBy(item);

        public bool IsCutBy(CombatEvent item) => item is CombatCharacterEvent character && CombatCharacterHits.RestartsHit(character);

        public bool Cuttable => true;

        public bool ForbidsPose => false;
    }
}
