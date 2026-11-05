using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor
{
    [Serializable]
    public sealed class FbxPlacementBakeSettings
    {
        public float resolution = 10;
        public int atlasSize = 1024;
        public int directSamples = 16;
        public int indirectSamples = 64;
        public int environmentSamples = 32;
        public LightingSettings.Lightmapper lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        public MixedLightingMode mixedMode = MixedLightingMode.Shadowmask;
        public LightmapsMode directionalMode = LightmapsMode.CombinedDirectional;
        public Color ambientColor = new Color(.2f, .2f, .2f);

        public LightingSettings CreateLightingSettings()
        {
            if (!float.IsFinite(resolution) || resolution < .1f || resolution > 100 ||
                directSamples < 1 || indirectSamples < 1 || environmentSamples < 1 ||
                atlasSize < 128 || atlasSize > 4096 || !Mathf.IsPowerOfTwo(atlasSize))
                throw new ArgumentException("베이크 해상도·샘플 수·아틀라스 크기가 올바르지 않습니다.");
            return new LightingSettings
            {
                name = "맵 베이크 설정", bakedGI = true, realtimeGI = false,
                lightmapper = lightmapper, lightmapResolution = resolution, lightmapMaxSize = atlasSize,
                directSampleCount = directSamples, indirectSampleCount = indirectSamples,
                environmentSampleCount = environmentSamples, mixedBakeMode = mixedMode,
                directionalityMode = directionalMode, maxBounces = 2
            };
        }
    }
}
