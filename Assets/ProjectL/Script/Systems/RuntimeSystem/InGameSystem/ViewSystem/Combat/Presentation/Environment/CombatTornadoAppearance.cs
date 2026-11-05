using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment
{
    /// <summary>한 외형의 생성·축소·재배치 전환입니다. 정상 배율과 곡선 계수를 분리하고 원본 사실을 소유하지 않습니다.</summary>
    internal sealed class CombatTornadoAppearance
    {
        private sealed class Padding
        {
            internal Renderer Renderer;
            internal int Slot;
            internal float Value;
            internal MaterialPropertyBlock Original;
            internal MaterialPropertyBlock Working;
        }

        private static readonly int VertexPadding = Shader.PropertyToID("_VertexPadding");
        private readonly GameObject _view;
        private readonly Vector3 _scale;
        private readonly Padding[] _padding;
        private readonly ParticleSystem[] _particles;
        private float _progress;
        private int _direction;
        private bool _initialized;
        private bool _pendingShow;
        private Vector3 _nextPosition;
        private TornadoPresentationSettingsData Settings { get; }
        internal bool IsPlaying => _direction != 0;

        internal CombatTornadoAppearance(GameObject view, Vector3 scale, TornadoPresentationSettingsData settings)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _scale = scale;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var padding = new System.Collections.Generic.List<Padding>();
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    if (material == null || !material.HasProperty(VertexPadding)) continue;
                    var original = new MaterialPropertyBlock();
                    var working = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, slot);
                    renderer.GetPropertyBlock(working, slot);
                    padding.Add(new Padding { Renderer = renderer, Slot = slot, Value = material.GetFloat(VertexPadding),
                        Original = original, Working = working });
                }
            }
            _padding = padding.ToArray();
            _particles = view.GetComponentsInChildren<ParticleSystem>(true);
        }

        internal void Refresh(Vector3 position)
        {
            if (!_initialized) ShowAt(position);
            // 사건으로 진행 중인 전환·숨김은 최종 사실 갱신이 다시 시작하지 않습니다.
            else if (!IsPlaying && _view.activeSelf && _view.transform.position != position) ShowAt(position);
        }

        private void ShowAt(Vector3 position)
        {
            if (!_initialized || !_view.activeSelf)
            {
                _initialized = true;
                _pendingShow = false;
                _progress = 0f;
                _view.transform.position = position;
                ApplyScale();
                _view.SetActive(true);
                PlayParticles();
                _direction = 1;
                return;
            }
            if (_direction >= 0 && _view.transform.position == position) return;
            _nextPosition = position;
            _pendingShow = true;
            _direction = -1;
        }

        internal void Tick(float deltaTime)
        {
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (_view == null) throw new InvalidOperationException("토네이도 전환 중 외형이 사라졌습니다.");
            float remaining = deltaTime / Settings.AppearanceSeconds;
            while (_direction != 0)
            {
                float distance = _direction > 0 ? 1f - _progress : _progress;
                float step = Mathf.Min(distance, remaining);
                _progress = Mathf.Clamp01(_progress + _direction * step);
                remaining -= step;
                ApplyScale();
                if (step < distance) break;
                bool hidden = _direction < 0;
                _direction = 0;
                if (hidden)
                {
                    _view.SetActive(false);
                    if (_pendingShow) ShowAt(_nextPosition);
                }
                if (remaining <= 0f) break;
            }
        }

        // 몸 층(Layer1~4)은 켜질 때 저절로 재생하지 않아 직접 시작합니다. 한 번 내보낸 입자는 Pause Until Replaced로 남아 몸을 이룹니다.
        private void PlayParticles()
        {
            foreach (ParticleSystem particles in _particles)
            {
                if (particles == null) continue;
                particles.Clear(false);
                particles.Play(false);
            }
        }

        private void ApplyScale()
        {
            float coefficient = Settings.Appearance(_progress);
            _view.transform.localScale = _scale * coefficient;
            foreach (Padding padding in _padding)
            {
                padding.Working.SetFloat(VertexPadding, padding.Value * coefficient);
                padding.Renderer.SetPropertyBlock(padding.Working, padding.Slot);
            }
        }

        /// <summary>오류·반환·씬 해제에서는 기다리지 않습니다. 다음 대여에 이전 전환/재질 덮어쓰기를 남기지 않습니다.</summary>
        internal void Reset()
        {
            _direction = 0;
            _progress = 0f;
            _pendingShow = false;
            _initialized = false;
            if (_view == null) return;
            _view.SetActive(false);
            _view.transform.localScale = _scale;
            foreach (Padding padding in _padding)
                if (padding.Renderer != null)
                    padding.Renderer.SetPropertyBlock(padding.Original.isEmpty ? null : padding.Original, padding.Slot);
        }
    }
}
