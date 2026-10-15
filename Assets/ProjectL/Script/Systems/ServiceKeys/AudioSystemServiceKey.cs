using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;

namespace ProjectL.Systems.ServiceKeys
{
    public static class AudioSystemServiceKey
    {
        public static readonly SystemGroupKey<IAudioSystemService> Audio = new(nameof(Audio));
    }
}
