using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 피격·사망 반응 재생기입니다(연출 조립 설계 B12). 반응 항목은 부품(<see cref="CombatUnitReactionSet"/>)이, 반응 불가 상태는 이 재생기가 원본입니다.
    /// 반응을 시작하는 처리기는 부품을 직접 쓰고, 표시가 사라졌을 때는 이 재생기의 <see cref="CancelUnavailable"/>을 부릅니다.
    /// </summary>
    internal sealed class CombatReactionPlayer : ICombatPresentationPlayer, ICombatSubjectActivity
    {
        private readonly CombatUnitReactionSet _reaction;
        private bool _unavailable;
        private int _versionAtFrameStart;

        internal CombatReactionPlayer(CombatUnitReactionSet reaction) =>
            _reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));

        public bool IsComplete => !_reaction.IsPlaying && !_unavailable;

        public bool BlocksFinalFacts => _reaction.IsPlaying || _unavailable;

        /// <summary>반응 중 유닛 표시가 사라졌을 때 부릅니다. 모든 반응을 버리고, 다음 프레임 맨 앞에서 예외로 알립니다(B14 O13).</summary>
        internal void CancelUnavailable()
        {
            _unavailable = true;
            _reaction.Cancel();
        }

        public void BeginResult(CombatResult result)
        {
        }

        public CombatStartBlock FindBlock(in CombatStartQuery query)
        {
            if (_unavailable) return CombatStartBlock.Unavailable;
            // 주체 기준 막기 규칙은 조각 유닛 실행·걸음 레인과 함께 쓰는 한 곳입니다(조각·조립 설계 BP5, 동시 처리 설계 C5·C7, 3라운드 W12-3).
            return CombatSubjectBlocking.Blocks(query, this) ? CombatStartBlock.Reaction : CombatStartBlock.None;
        }

        // 공격 완료 콜백·이 프레임에 시작한 새 반응에는 이번 프레임 시간을 주지 않도록 프레임 시작 Version을 기록합니다(B14 O12).
        public void BeginFrame(in CombatPlaybackFrame frame)
        {
            if (_unavailable)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 반응 중 유닛 표시가 비활성화됐습니다.");
            _versionAtFrameStart = _reaction.Version;
        }

        public bool Tick(in CombatPlaybackFrame frame)
        {
            if (_reaction.IsPlaying)
                _reaction.Tick(frame.DeltaTime, _versionAtFrameStart);
            return true;
        }

        public void Cancel()
        {
            _unavailable = false;
            _reaction.Cancel();
        }

        public void Release() => _reaction.Release();

        bool ICombatSubjectActivity.IsPlaying => _reaction.IsPlaying;

        bool ICombatSubjectActivity.IsPlayingFor(EntityId subject) => _reaction.IsPlayingFor(subject);

        bool ICombatSubjectActivity.CanOverlapDeaths => _reaction.CanOverlapDeaths;
    }
}
