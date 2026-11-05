using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>옵션 종료 흐름이 최종 정리 뒤 호출하는 플랫폼 종료 입구입니다.</summary>
    internal interface IOptionGameExit
    {
        void Quit();
    }

    internal sealed class OptionGameExit : IOptionGameExit
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
