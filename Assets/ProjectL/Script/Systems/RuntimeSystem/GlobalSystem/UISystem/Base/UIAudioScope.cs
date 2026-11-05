using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>씬 루트 아래에서 동적으로 생성되는 입력에도 동일한 오디오 서비스를 전달한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UIAudioScope : MonoBehaviour
    {
        internal IAudioSystemService Service { get; private set; }

        internal void Bind(IAudioSystemService service)
        {
            Service = service;
            foreach (var binding in GetComponentsInChildren<UIInputBinding>(true))
                binding.ResolveAudio();
        }
    }
}
