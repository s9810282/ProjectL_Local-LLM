using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    public static class AudioSystemFactory
    {
        public static IExportSystemServices Create()
        {
            return new AudioSystemServices(new AudioSystem());
        }

        // Audio는 단일 모듈이므로 별도 SystemGroup 없이 서비스와 모듈을 등록한다.
        private sealed class AudioSystemServices : IExportSystemServices
        {
            private readonly AudioSystem _audioSystem;

            public AudioSystemServices(AudioSystem audioSystem)
            {
                _audioSystem = audioSystem;
            }

            public void ExportServices(ISystemWriteSink sink)
            {
                if (sink == null)
                    throw new ArgumentNullException(nameof(sink));

                sink.AddService<IAudioSystemService>(_audioSystem);
                sink.AddModule(_audioSystem);
            }
        }
    }
}
