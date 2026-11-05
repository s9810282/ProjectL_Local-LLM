using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 필드 외형 연출(토네이도 생성·축소, 바닥 교체 무너짐)을 재생기의 진행·완료·취소 계약에 연결합니다(타일 교체 설계 TR8).
    /// "다음 사건을 막는가"와 "끝났는가"를 따로 묻습니다. 무너짐은 떨리는 동안만 막고, 가라앉기까지 끝나야 완료입니다.
    /// </summary>
    internal sealed class CombatFieldAppearancePlayer : ICombatPresentationPlayer
    {
        private readonly Action<float> _tick;
        private readonly Func<bool> _isPlaying;
        private readonly Func<bool> _blocksStart;
        private readonly Action _cancel;

        /// <param name="tick">외형 연출을 진행합니다.</param>
        /// <param name="isPlaying">외형 연출이 하나라도 진행 중인지입니다. 완료·결과 끝 사실 반영이 기다립니다.</param>
        /// <param name="blocksStart">다음 사건 시작을 막는 외형 연출이 있는지입니다.</param>
        /// <param name="cancel">외형 연출을 멈춥니다.</param>
        internal CombatFieldAppearancePlayer(Action<float> tick, Func<bool> isPlaying, Func<bool> blocksStart, Action cancel)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
            _isPlaying = isPlaying ?? throw new ArgumentNullException(nameof(isPlaying));
            _blocksStart = blocksStart ?? throw new ArgumentNullException(nameof(blocksStart));
            _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        }

        public void BeginResult(CombatResult result) { }
        public void BeginFrame(in CombatPlaybackFrame frame) { }
        // 최초 Render의 생성은 결과 없이도 시작됩니다. 결과를 받되 사건 시작은 막는 연출(토네이도 생성, 무너짐 떨림)이 끝날 때까지 기다립니다.
        public CombatStartBlock FindBlock(in CombatStartQuery query) =>
            query.Profile != CombatStartProfile.Result && _blocksStart() ? CombatStartBlock.FieldAppearance : CombatStartBlock.None;
        public bool Tick(in CombatPlaybackFrame frame)
        {
            if (frame.MapperBound) _tick(frame.DeltaTime);
            return true;
        }
        public bool IsComplete => !_isPlaying();
        public bool BlocksFinalFacts => _isPlaying();
        public void Cancel() => _cancel();
        public void Release() => _cancel();
    }
}
