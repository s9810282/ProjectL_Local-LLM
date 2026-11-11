using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// 전투 결과 화면과 그때의 배경음입니다(씬 표시 나누기 설계 DS4). 승리면 승리 효과음을 내고, 확인이 받아들여지면 완료 뒤 배경음으로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// 배경음 정책(<see cref="CombatBgmPolicy"/>)은 표시(국면 배경음)와 같은 객체를 받습니다. 국면 → 프리셋 번호 규칙의 원본은 정책 하나입니다(평가 C2-03).
    /// 해제 확인(예외)은 표시가 하고 여기는 인자만 검사합니다(계획 TP-7).
    /// </remarks>
    internal sealed class CombatResultScreenDisplay
    {
        private readonly CombatEndUI _completion;
        private readonly CombatBgmPolicy _bgm;

        internal CombatResultScreenDisplay(CombatEndUI completion, CombatBgmPolicy bgm)
        {
            _completion = completion != null ? completion : throw new ArgumentNullException(nameof(completion));
            _bgm = bgm ?? throw new ArgumentNullException(nameof(bgm));
        }

        /// <summary>
        /// 결과 화면을 엽니다. 확인을 누르면 <paramref name="confirm"/>을 부르고, 참이면 완료 뒤 배경음으로 바꿉니다(View 2라운드 W6-5).
        /// 확인을 받아도 되는지(같은 연결·같은 완료·한 번만)는 <paramref name="confirm"/>이 판단합니다.
        /// </summary>
        internal void Show(BattleCompletionChanges completion, Func<bool> confirm)
        {
            if (confirm == null) throw new ArgumentNullException(nameof(confirm));
            _bgm.PlayResult(completion.Outcome.Outcome);
            _completion.Present(completion, () =>
            {
                if (!confirm())
                    return;
                _bgm.ApplyAfterCompletion();
            });
        }

        // 정리 목록에서도 불리므로 다른 정리처럼 파괴된 객체를 거릅니다.
        internal void Hide()
        {
            if (_completion != null) _completion.Dismiss();
        }
    }
}
