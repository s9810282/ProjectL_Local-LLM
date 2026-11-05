using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 소리를 내는 시간 없는 조각입니다(조각·조립 설계 BP8). 무엇을 낼지는 소리 표(<see cref="CombatEventSounds"/>)가 답하고,
    /// 이 조각은 조립이 정한 순간에 그것을 냅니다. 사망 조립에서는 사망 반응 다음, 사실 반영 전입니다.
    /// 무엇을 낼지는 조각이 시작하는 순간에 고릅니다. 사건 소리는 그 순간의 보인 사실로 고르고(<see cref="CombatEventAudio.CueOf"/>, 사망 소리는 반영 전이어야 함),
    /// 투사체·피격 소리는 붙일 때 이미 고른 단서를 냅니다(투사체·피격 소리 설계 PS2). 고르는 방식이 둘이어도 조각은 한 모양입니다(묶음 끝 검토 낮음 3).
    /// </summary>
    internal sealed class CombatEventSoundPiece : ICombatPresentationPiece
    {
        private readonly CombatEventAudio _sound;
        private readonly Func<CombatSoundCue> _cue;

        /// <param name="cue">낼 소리 단서를 고르는 함수입니다. 조각이 시작하는 순간에 한 번 부릅니다. 소리 없음을 돌려주면 아무것도 내지 않습니다.</param>
        internal CombatEventSoundPiece(CombatEventAudio sound, Func<CombatSoundCue> cue)
        {
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _cue = cue ?? throw new ArgumentNullException(nameof(cue));
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _sound.Play(_cue());
            IsEnded = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
