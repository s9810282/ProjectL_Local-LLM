using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 실제 연출 실행 시작을 같은 공격의 선택 Kind에 연결하는 어댑터입니다.
    /// 예약·시계·첫 시작 선택은 수신자의 책임이며 이 객체는 결과/세대가 지난 통지를 버립니다.
    /// </summary>
    internal sealed class CombatPreAttackPresentationStarts
    {
        private readonly Func<EntityId, CombatClipAttackData> _clipOf;
        private readonly Func<int> _generation;
        private readonly Dictionary<int, CombatAttackAnimationStartData> _settings = new();
        private readonly Dictionary<CombatPullEvent, int> _pullIndices = new();
        private CombatResult _result;
        private CombatPreAttackTraitMap _map;
        private Action<CombatPreAttackTraitBinding, CombatAttackAnimationStartData> _started;

        internal CombatPreAttackPresentationStarts(Func<EntityId, CombatClipAttackData> clipOf, Func<int> generation)
        {
            _clipOf = clipOf ?? throw new ArgumentNullException(nameof(clipOf));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>같은 재생 수명의 예약 수신자를 연결합니다.</summary>
        internal void BindReceiver(Action<CombatPreAttackTraitBinding, CombatAttackAnimationStartData> started) =>
            _started = started ?? throw new ArgumentNullException(nameof(started));

        internal void Begin(CombatResult result)
        {
            Clear();
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            for (int i = 0; i < result.Events.Count; i++)
            {
                // 포효는 전용 코드 모션이며 일반 공격 클립 설정으로 바꾸지 않습니다.
                if (result.Events[i] is not CombatAttackEvent attackEvent)
                    continue;
                var attack = attackEvent.Attack;
                CombatClipAttackData clip = _clipOf(attack.AttackerId);
                if (clip != null && clip.AnimationStart.UsesSelectedTrait)
                    _settings.Add(i, clip.AnimationStart);
            }
            // 기본 모드만 있는 결과는 새 소속 검사와 색인 할당을 요구하지 않습니다.
            if (_settings.Count == 0)
                return;
            _map = CombatPreAttackTraitMap.Build(result, _generation());
            _result = result;
            for (int i = 0; i < result.Events.Count; i++)
                if (result.Events[i] is CombatPullEvent pull)
                    _pullIndices.Add(pull, i);
        }

        /// <summary>텍스트 조각처럼 실제 실행 안에서 통지할 때만 콜백을 만듭니다. Pull은 이동 레인이 기준입니다.</summary>
        internal Action CaptureTraitStart(TraitActivationSnapshot trait, int generation)
        {
            if (!TrySelectTrait(trait, generation, out var binding, out var settings))
                return null;
            return () => Publish(binding, settings);
        }

        internal void TraitStarted(TraitActivationSnapshot trait, int generation)
        {
            if (TrySelectTrait(trait, generation, out var binding, out var settings))
                Publish(binding, settings);
        }

        internal Action CapturePullStart(CombatPullEvent source, int generation)
        {
            if (_map != null && generation == _generation() && _pullIndices.TryGetValue(source, out int index) &&
                _map.TryGetPullBinding(index, out var binding) && TrySelect(binding, out var settings))
                return () => Publish(binding, settings);
            return null;
        }

        internal void Clear()
        {
            _result = null;
            _map = null;
            _settings.Clear();
            _pullIndices.Clear();
        }

        private bool TrySelectTrait(TraitActivationSnapshot trait, int generation,
            out CombatPreAttackTraitBinding binding, out CombatAttackAnimationStartData settings)
        {
            binding = default;
            settings = default;
            return _map != null && generation == _generation() && _map.TryGetBinding(trait, out binding) &&
                !binding.PullEventIndex.HasValue && TrySelect(binding, out settings);
        }

        private bool TrySelect(CombatPreAttackTraitBinding binding, out CombatAttackAnimationStartData settings) =>
            _settings.TryGetValue(binding.Attack.EventIndex, out settings) && settings.Kind == binding.Trait.TraitActivationKind;

        private void Publish(CombatPreAttackTraitBinding binding, CombatAttackAnimationStartData settings)
        {
            if (ReferenceEquals(_result, binding.Attack.Result) && _generation() == binding.Attack.Generation)
                _started?.Invoke(binding, settings);
        }
    }
}
