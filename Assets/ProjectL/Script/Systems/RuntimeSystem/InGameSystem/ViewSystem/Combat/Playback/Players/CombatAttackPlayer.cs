using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 공격 재생기입니다(연출 조립 설계 B12). <see cref="CombatAttackPlayback"/>의 수명 규칙을 실행 목록 계약으로 답하고,
    /// 출처 겹침 허용의 프레임 시작 값을 한 번 기록합니다(보완 3). 공격 시작·출처 발사·출처 수명·정면 공격 모션 같은 시작 API는 처리기·조각이 부품에서 직접 씁니다.
    /// </summary>
    internal sealed class CombatAttackPlayer : ICombatPresentationPlayer, ICombatSourceOverlap
    {
        private readonly CombatAttackPlayback _attacks;
        private bool _allowedAtFrameStart;

        internal CombatAttackPlayer(CombatAttackPlayback attacks) =>
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));

        public bool AllowsOverlap => _attacks.AllowsOverlap;

        public bool AllowedAtFrameStart => _allowedAtFrameStart;

        public bool IsComplete => _attacks.IsComplete;

        public bool BlocksFinalFacts => _attacks.IsPlaying || _attacks.IsUnavailable;

        public void BeginResult(CombatResult result) => _attacks.BeginResult(result);

        public CombatStartBlock FindBlock(in CombatStartQuery query)
        {
            // 공격 중 유닛·무기 표시가 사라졌으면 다음 사건을 시작하지 않고, 다음 Tick이 그 원인으로 실패를 알리게 합니다.
            if (_attacks.IsUnavailable) return CombatStartBlock.Unavailable;
            // 공격 Gate: 사건 완료 뒤 남은 공격 연출과 겹쳐도 되는 사건인지 답합니다(히트 타이밍 설계 H3). 시작할 사건이 있을 때만 봅니다.
            if (query.Item != null && !_attacks.CanBegin(query.Item))
                return CombatStartBlock.AttackGate;
            // 히트 전 공격 중이면 막습니다. 겹침 허용 프로필은 사건 완료 뒤 남은 공격 연출과 겹칩니다.
            return _attacks.BlocksNextEvent(query.AllowsOverlap) ? CombatStartBlock.AttackBusy : CombatStartBlock.None;
        }

        // 겹침 허용은 프레임 시작 기준입니다. 이 프레임에 포효가 끝나도 겹쳐 진행 중이던 반응·걸음은 시간을 받습니다(B14 O5).
        public void BeginFrame(in CombatPlaybackFrame frame) => _allowedAtFrameStart = _attacks.AllowsOverlap;

        // 공격을 먼저 진행한다. 세대 변경·사건 완료 전(겹침 불가)의 공격 진행 중이면 이 프레임을 끝낸다(B14 O1·O2).
        public bool Tick(in CombatPlaybackFrame frame) => _attacks.Tick(frame.DeltaTime, frame.MapperBound);

        public void Cancel() => _attacks.Cancel();

        public void Release() => _attacks.Release();
    }
}
