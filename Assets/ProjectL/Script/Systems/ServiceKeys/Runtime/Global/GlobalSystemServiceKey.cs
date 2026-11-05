using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
    public static class GlobalSystemServiceKey
    {
        public static readonly SystemGroupKey<IUISystemService> UI =
            new(nameof(UI));

        public static readonly SystemGroupKey<IGlobalUIPresentation> GlobalUIPresentation =
            new(nameof(GlobalUIPresentation));

        public static readonly SystemGroupKey<IUITextPresentation> UITextPresentation =
            new(nameof(UITextPresentation));

        public static readonly SystemGroupKey<IAudioSystemService> Audio = new(nameof(Audio));
    }
}
