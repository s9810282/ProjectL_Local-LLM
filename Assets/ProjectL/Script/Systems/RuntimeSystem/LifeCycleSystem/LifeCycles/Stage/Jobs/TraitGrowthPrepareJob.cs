using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>다음 증강 제안을 Core에 준비시킵니다. 제안이 이미 있거나 선택이 필요 없으면 Core가 아무것도 하지 않습니다.</summary>
    internal sealed class TraitGrowthPrepareJob : IJobService
    {
        private readonly ICharacterRosterService _characters;
        private bool _started;

        internal TraitGrowthPrepareJob(ICharacterRosterService characters) =>
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));

        public int Priority => 3;
        public bool IsCompleted { get; private set; }

        public void Execute()
        {
            if (_started) return;
            _started = true;
            try
            {
                // 돌려받는 진행은 쓰지 않습니다. 바로 뒤의 확인 Job이 원본이 바뀌었는지 봅니다.
                _characters.PrepareNextTraitGrowthOffer();
            }
            finally
            {
                IsCompleted = true;
            }
        }
    }
}
