using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 씬 표시와 결과 화면 표면이 내는 배경음 정책입니다(평가 C2-03 1단계, 씬 표시 나누기 설계 DS4). 국면 배경음, 승리 보상 효과음, 완료 확인 뒤 배경음을 언제 어떻게 바꾸는지 한 곳에 둡니다.
    /// 프리셋 번호는 소리 프리셋(<see cref="CombatSoundPresets.Bgm"/>)이 답하고, 이 객체는 AudioSystem에 내는 차례만 정합니다.
    /// </summary>
    /// <remarks>
    /// 표시 Factory가 연결마다 하나 만들어 씬 표시(<c>CombatSceneDisplay</c>, 설치 첫 그리기·결과 끝 그리기의 국면)와 결과 화면(<c>CombatResultScreenDisplay</c>, 승리·완료 뒤)에 같은 것을 넘깁니다(씬 표시 나누기 설계 DS4).
    /// 결과 재생 중 국면이 바뀌는 사건의 배경음은 사건 소리(<see cref="CombatEventAudio"/>)가 냅니다.
    /// </remarks>
    internal sealed class CombatBgmPolicy
    {
        private readonly IAudioSystemService _audio;
        private readonly CombatSoundPresets _sounds;

        /// <param name="audio">오디오 서비스입니다(필수).</param>
        /// <param name="sounds">국면·완료 뒤 배경음 프리셋 번호를 답합니다(필수).</param>
        internal CombatBgmPolicy(IAudioSystemService audio, CombatSoundPresets sounds)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        /// <summary>국면(낮·밤)의 배경음으로 바꾸고, 배경음이 멈춰 있으면 틉니다. 표시를 그릴 때마다 부릅니다.</summary>
        internal void ApplyPhase(bool isNight) => Apply(isNight ? CombatBgmPhase.Night : CombatBgmPhase.Day);

        /// <summary>완료 표시를 열 때 결과 효과음을 냅니다.</summary>
        internal void PlayResult(StageOutcome outcome)
        {
            if (outcome == StageOutcome.Victory)
            {
                _audio.Play(AmbgmEvent.UIReward);
                // 신규 승리·패배 효과음은 사용 가능해지면 연결을 활성화합니다.
                // _audio.Play(UiEvent.StageWin);
            }
            // else if (outcome == StageOutcome.Defeat)
            // {
            //     _audio.Play(UiEvent.StageDefeat);
            // }
        }

        /// <summary>완료 확인이 받아들여진 뒤 맵으로 돌아가는 배경음으로 바꾸고, 배경음이 멈춰 있으면 틉니다.</summary>
        internal void ApplyAfterCompletion() => Apply(CombatBgmPhase.AfterCompletion);

        // 프리셋을 먼저 바꾸고, 멈춰 있을 때만 틉니다. 이미 돌고 있으면 프리셋 바꾸기로 이어집니다.
        private void Apply(CombatBgmPhase phase)
        {
            _audio.SetPreset(AmbgmEvent.BGM, _sounds.Bgm(phase));
            if (!_audio.IsPlaying(AmbgmEvent.BGM)) _audio.Play(AmbgmEvent.BGM);
        }
    }
}
