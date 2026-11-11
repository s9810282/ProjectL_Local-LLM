using System;
using System.Collections.Generic;
using ProjectL.Render.Shader.PostProcessing.VolmetrickLight;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.RendererFeatures.Systems.VolumetricLight
{
    internal sealed class VolmetrickLightHistoryCache : IDisposable
    {
        internal readonly struct Inputs
        {
            public readonly Matrix4x4 ViewProjection;
            public readonly Matrix4x4 LightTransform;
            public readonly int LightId;
            public readonly float ShadowStrength;
            public readonly int CascadeCount;
            public readonly int LightingSteps;
            public readonly float LightingRange;
            public readonly float LightingIntensity;
            public readonly float LightingIntensityMax;
            public readonly float ShadowIntensity;
            public readonly float ShadowIntensityMax;

            public Inputs(Matrix4x4 viewProjection, Matrix4x4 lightTransform, int lightId,
                float shadowStrength, int cascadeCount, VolmetrickLightSettings settings)
            {
                ViewProjection = viewProjection;
                LightTransform = lightTransform;
                LightId = lightId;
                ShadowStrength = shadowStrength;
                CascadeCount = cascadeCount;
                LightingSteps = Mathf.Max(1, settings.LightingSteps);
                LightingRange = settings.LightingRange;
                LightingIntensity = settings.LightingIntensity;
                LightingIntensityMax = settings.LightingIntensityMax;
                ShadowIntensity = settings.ShadowIntensity;
                ShadowIntensityMax = settings.ShadowIntensityMax;
            }

            public bool Matches(in Inputs other)
            {
                return ViewProjection.Equals(other.ViewProjection) &&
                    LightTransform.Equals(other.LightTransform) &&
                    LightId == other.LightId && ShadowStrength == other.ShadowStrength &&
                    CascadeCount == other.CascadeCount && LightingSteps == other.LightingSteps &&
                    LightingRange == other.LightingRange && LightingIntensity == other.LightingIntensity &&
                    LightingIntensityMax == other.LightingIntensityMax &&
                    ShadowIntensity == other.ShadowIntensity && ShadowIntensityMax == other.ShadowIntensityMax;
            }
        }

        internal sealed class State : IDisposable
        {
            private RTHandle _texture;
            private bool _valid;
            private int _lastCompletedFrame = -1;
            private Inputs _inputs;

            public Camera Camera { get; }
            public RTHandle Texture => _texture;
            public int Parity { get; private set; }
            public int LastUsedFrame { get; private set; }

            public State(Camera camera)
            {
                Camera = camera;
            }

            public bool Prepare(in RenderTextureDescriptor cameraDescriptor, in Inputs inputs,
                int frameIndex, bool allowReuse)
            {
                LastUsedFrame = frameIndex;
                if (_texture != null && !_texture.rt.IsCreated())
                {
                    Dispose();
                }

                RenderTextureDescriptor descriptor = cameraDescriptor;
                descriptor.graphicsFormat = GraphicsFormat.R32G32_SFloat;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.bindMS = false;
                descriptor.enableRandomWrite = true;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.memoryless = RenderTextureMemoryless.None;
                descriptor.useDynamicScale = false;
                descriptor.useDynamicScaleExplicit = false;

                bool reallocated = RenderingUtils.ReAllocateHandleIfNeeded(ref _texture, descriptor,
                    FilterMode.Point, TextureWrapMode.Clamp, name: "VolmetrickLightHistory");
                if (reallocated)
                {
                    _valid = false;
                }

                return !_valid || !allowReuse ||
                    _lastCompletedFrame != frameIndex - 1 || !_inputs.Matches(inputs);
            }

            public void Complete(in Inputs inputs, int frameIndex, int parity, bool fullRefresh)
            {
                _inputs = inputs;
                _lastCompletedFrame = frameIndex;
                Parity = fullRefresh ? 0 : parity ^ 1;
                _valid = true;
            }

            public void Dispose()
            {
                _texture?.Release();
                _texture = null;
                _valid = false;
            }
        }

        private readonly Dictionary<int, State> _states = new Dictionary<int, State>();
        private readonly List<int> _expiredCameras = new List<int>();

        public State Acquire(Camera camera, int frameIndex)
        {
            // 다른 카메라의 렌더가 계속될 때 소멸·사용 중단 카메라의 GPU 자원을 회수한다.
            _expiredCameras.Clear();
            foreach (KeyValuePair<int, State> entry in _states)
            {
                if (entry.Value.Camera == null || frameIndex - entry.Value.LastUsedFrame > 2)
                {
                    entry.Value.Dispose();
                    _expiredCameras.Add(entry.Key);
                }
            }

            foreach (int cameraId in _expiredCameras)
            {
                _states.Remove(cameraId);
            }

            int id = camera.GetInstanceID();
            if (!_states.TryGetValue(id, out State state))
            {
                state = new State(camera);
                _states.Add(id, state);
            }

            return state;
        }

        public void Dispose()
        {
            foreach (State state in _states.Values)
            {
                state.Dispose();
            }

            _states.Clear();
            _expiredCameras.Clear();
        }
    }
}
