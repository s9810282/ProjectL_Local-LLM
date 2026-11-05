using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>현재 Bus 볼륨을 백분율로 표시하고 슬라이더 입력을 오디오 서비스에 전달합니다.</summary>
    public sealed class SoundSettingUI : UIBase, IUISliderReceiver
    {
        [Serializable]
        private sealed class BusSlider
        {
            [SerializeField] private UIValueSlider slider;
            [SerializeField] private string busPath;
            public UIValueSlider Slider => slider;
            public string BusPath => busPath;
        }

        [SerializeField] private BusSlider[] buses = Array.Empty<BusSlider>();
        private IAudioSystemService _audio;
        private bool _slidersBound;
        private float[] _initialVolumes;
        private bool[] _initialAvailable;
        private bool _isEditing;

        internal bool HasChanges
        {
            get
            {
                if (!_isEditing) return false;
                for (int i = 0; i < buses.Length; i++)
                {
                    if (_initialAvailable[i] && _audio.TryGetBusVolume(buses[i].BusPath, out float volume)
                        && !Mathf.Approximately(_initialVolumes[i], volume)) return true;
                }
                return false;
            }
        }

        internal void BeginEdit()
        {
            if (_audio == null) throw new InvalidOperationException("[SoundSettingUI] 오디오 서비스를 연결해야 합니다.");
            BindSliders();
            if (_initialVolumes == null || _initialVolumes.Length != buses.Length)
            {
                _initialVolumes = new float[buses.Length];
                _initialAvailable = new bool[buses.Length];
            }
            for (int i = 0; i < buses.Length; i++)
                _initialAvailable[i] = _audio.TryGetBusVolume(buses[i].BusPath, out _initialVolumes[i]);
            _isEditing = true;
        }

        internal void SaveEdit() => _isEditing = false;

        internal void CancelEdit()
        {
            if (!_isEditing) return;
            _isEditing = false;
            for (int i = 0; i < buses.Length; i++)
            {
                if (!_initialAvailable[i]) continue;
                string path = buses[i].BusPath;
                if (_audio.TryGetBusVolume(path, out float volume) && Mathf.Approximately(volume, _initialVolumes[i])) continue;
                if (!_audio.TrySetBusVolume(path, _initialVolumes[i]))
                    Debug.LogWarning($"[SoundSettingUI] Bus 볼륨을 복구하지 못했습니다: {path}", this);
            }
            RefreshVolumes();
        }

        public override UIId ID => UIId.SoundSetting;
        public override UIId ParentID => UIId.Setting;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            Policy = UIOpenPolicy.Parallel;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _audio = resolver.GetService(SystemGroupsService.Global).AudioServiceCommand;
            BindSliders();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        private void BindSliders()
        {
            if (_slidersBound) return;
            if (buses.Length != 4) throw new InvalidOperationException("[SoundSettingUI] Bus Slider 4개를 연결해야 합니다.");
            for (int i = 0; i < buses.Length; i++)
            {
                BusSlider binding = buses[i];
                if (binding == null || binding.Slider == null || string.IsNullOrWhiteSpace(binding.BusPath))
                    throw new InvalidOperationException("[SoundSettingUI] Slider와 Bus 경로를 연결해야 합니다.");
                binding.Slider.minValue = 0f;
                binding.Slider.maxValue = 100f;
                binding.Slider.wholeNumbers = false;
                binding.Slider.Bind(this, i);
            }
            _slidersBound = true;
        }

        public override void Open()
        {
            if (_audio == null) throw new InvalidOperationException("[SoundSettingUI] 오디오 서비스를 연결해야 합니다.");
            BindSliders();
            RefreshVolumes();
            gameObject.SetActive(true);
        }

        private void RefreshVolumes()
        {
            for (int i = 0; i < buses.Length; i++)
            {
                BusSlider binding = buses[i];
                bool available = _audio.TryGetBusVolume(binding.BusPath, out float volume);
                binding.Slider.interactable = available;
                if (available) binding.Slider.SetValueWithoutNotify(Mathf.Clamp01(volume) * 100f);
                else Debug.LogWarning($"[SoundSettingUI] Bus 볼륨을 읽을 수 없습니다: {binding.BusPath}", this);
            }
        }

        void IUISliderReceiver.ValueChanged(int slot, float value)
        {
            if (!isActiveAndEnabled || _audio == null || slot < 0 || slot >= buses.Length) return;
            BusSlider binding = buses[slot];
            if (_audio.TrySetBusVolume(binding.BusPath, Mathf.Clamp01(value / 100f))) return;
            // 실패한 입력을 적용된 볼륨처럼 표시하지 않는다.
            if (_audio.TryGetBusVolume(binding.BusPath, out float volume))
                binding.Slider.SetValueWithoutNotify(Mathf.Clamp01(volume) * 100f);
            else binding.Slider.interactable = false;
            Debug.LogWarning($"[SoundSettingUI] Bus 볼륨을 변경하지 못했습니다: {binding.BusPath}", this);
        }

        public override void Hide() => gameObject.SetActive(false);
    }
}
