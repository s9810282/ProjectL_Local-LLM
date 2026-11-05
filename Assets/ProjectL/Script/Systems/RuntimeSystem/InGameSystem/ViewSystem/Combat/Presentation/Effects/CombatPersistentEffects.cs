using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 오래 보이는 이펙트(상태 유지·특성 상시)를 지금 보인 사실에 맞추는 한 곳입니다(이펙트 끝 규칙 설계 EL4, 계획 TP-E23).
    /// 보인 사실을 그릴 때마다(<c>CombatShownFacts.Render</c>) 출처들이 채운 "지금 보여야 할 것"과 켜진 이펙트를 비교해, 없는 것은 내고 남은 것은 거둡니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 보인 사실을 그리는 모든 길(사건 순간·걸음 도착·캐릭터 사건·결과 끝 최종 사실)이 같은 그리기 하나를 지나므로, 유지 이펙트는 HUD 아이콘과 같은 순간에 켜지고 꺼집니다.
    /// 그래서 사건 종류마다 따로 연결하지 않습니다. 매 프레임 검사하지 않고 그릴 때만 맞춥니다.
    /// </para>
    /// <para>
    /// 이펙트는 창구의 "부르는 쪽이 거둠" 놓기 값으로 몸을 따라가게 냅니다. 숨김·사라짐·거두는 모양은 받침 그대로입니다(끌기 동안 숨김, 몸이 사라지면 바로 멈춤,
    /// 링 버퍼면 바로 지우고 아니면 방출만 멈춤). 결과 취소의 전부 지우기(<c>StopAll</c>) 등으로 이펙트가 밖에서 끝났으면 다음 맞추기에서 다시 냅니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatPersistentEffects
    {
        private readonly CombatEffectWindow _effects;
        private readonly IReadOnlyList<ICombatPersistentEffectSource> _sources;
        private readonly Dictionary<CombatPersistentEffectKey, Live> _live = new();
        // 맞출 때마다 다시 쓰는 목록입니다(할당 없음).
        private readonly List<CombatPersistentEffectWant> _wants = new();
        private readonly HashSet<CombatPersistentEffectKey> _wanted = new();
        private readonly List<CombatPersistentEffectKey> _gone = new();

        /// <param name="sources">출처 목록입니다. 순서는 같은 열쇠가 겹칠 때 앞의 것이 이기는 차례입니다.</param>
        internal CombatPersistentEffects(CombatEffectWindow effects, IReadOnlyList<ICombatPersistentEffectSource> sources)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            for (int i = 0; i < _sources.Count; i++)
                if (_sources[i] == null)
                    throw new ArgumentException($"유지 이펙트 출처 {i}가 비었습니다.", nameof(sources));
        }

        /// <summary>
        /// 지금 보인 사실로 유지 이펙트를 맞춥니다. 보여야 하는데 없거나 밖에서 끝난 것은 내고, 더는 보이지 않을 것은 거둡니다.
        /// </summary>
        /// <param name="shown">지금 보인 캐릭터 사실입니다.</param>
        /// <param name="bodyOf">유닛의 지금 몸 자리(서는 자리 반영)입니다. 찾지 못하면 null이고 그 이펙트는 내지 않습니다.</param>
        internal void Sync(IReadOnlyList<CharacterCombatFacts> shown, Func<EntityId, Pose?> bodyOf)
        {
            if (shown == null) throw new ArgumentNullException(nameof(shown));
            if (bodyOf == null) throw new ArgumentNullException(nameof(bodyOf));
            _wants.Clear();
            for (int i = 0; i < _sources.Count; i++)
                _sources[i].Collect(shown, _wants);
            _wanted.Clear();
            for (int i = 0; i < _wants.Count; i++)
            {
                CombatPersistentEffectWant want = _wants[i];
                if (!_wanted.Add(want.Key))
                    continue;
                CombatHeldEffect effect = want.Effect;
                Vector3 additionalWorldOffset = _effects.AdditionalWorldOffsetOf(want.Key.Body, effect.RiderYOffset);
                if (_live.TryGetValue(want.Key, out Live live))
                {
                    if (live.Handle.IsLive && live.Effect.SameAs(effect) && live.AdditionalWorldOffset == additionalWorldOffset)
                        continue;
                    live.Handle.Recall();
                    _live.Remove(want.Key);
                }
                Pose? body = bodyOf(want.Key.Body);
                if (!body.HasValue)
                    continue;
                Pose at = CombatCuePoints.Apply(body.Value, effect.PositionOffset, effect.RotationOffset, additionalWorldOffset);
                CombatEffectHandle handle = _effects.Play(effect.Effect, at, CombatEffectPlacement.Following(want.Key.Body).AsHeld());
                _live.Add(want.Key, new Live(handle, effect, additionalWorldOffset, want.RestartOn));
            }
            _gone.Clear();
            foreach (KeyValuePair<CombatPersistentEffectKey, Live> pair in _live)
                if (!_wanted.Contains(pair.Key))
                    _gone.Add(pair.Key);
            for (int i = 0; i < _gone.Count; i++)
            {
                _live[_gone[i]].Handle.Recall();
                _live.Remove(_gone[i]);
            }
        }

        /// <summary>
        /// 특성 발동 하나가 시작했습니다(계획 TP-E13). 그 발동의 대상(없으면 발동 유닛)에 붙은 유지 이펙트 중 이 발동 종류로 다시 내는 것을 거둡니다.
        /// 바로 뒤 사실 그리기의 맞추기가 새로 냅니다(예: 보호막 리셋마다 다시 재생, 결정 Q-EL3 A).
        /// </summary>
        internal void NotifyActivation(TraitActivationSnapshot trait)
        {
            if (trait == null) throw new ArgumentNullException(nameof(trait));
            _gone.Clear();
            foreach (KeyValuePair<CombatPersistentEffectKey, Live> pair in _live)
                if (Touches(trait, pair.Key.Body) && Contains(pair.Value.RestartOn, trait.TraitActivationKind))
                    _gone.Add(pair.Key);
            for (int i = 0; i < _gone.Count; i++)
            {
                _live[_gone[i]].Handle.Recall();
                _live.Remove(_gone[i]);
            }
        }

        /// <summary>켜진 유지 이펙트를 모두 거두고 잊습니다. 보인 사실을 비울 때(연결 해제) 부릅니다. 반복 호출해도 안전합니다.</summary>
        internal void Clear()
        {
            foreach (Live live in _live.Values)
                live.Handle.Recall();
            _live.Clear();
        }

        // 발동이 닿는 유닛입니다. 대상이 있으면 대상들, 없으면 발동 유닛입니다(옛 발동 짝 회수와 같은 규칙).
        private static bool Touches(TraitActivationSnapshot trait, EntityId unit)
        {
            if (trait.TargetIds.Count == 0)
                return trait.SourceUnitId == unit;
            for (int i = 0; i < trait.TargetIds.Count; i++)
                if (trait.TargetIds[i] == unit)
                    return true;
            return false;
        }

        private static bool Contains(IReadOnlyList<TraitActivationKind> kinds, TraitActivationKind kind)
        {
            for (int i = 0; i < kinds.Count; i++)
                if (kinds[i] == kind)
                    return true;
            return false;
        }

        // 켜진 유지 이펙트 하나입니다. 같은 값(종류·보정)이면 다시 내지 않습니다. 값 객체가 새로 만들어져도(DB 검사 다시 실행 등) 화면이 깜박이지 않습니다(묶음 끝 검토 L1).
        private readonly struct Live
        {
            internal Live(CombatEffectHandle handle, CombatHeldEffect effect, Vector3 additionalWorldOffset, IReadOnlyList<TraitActivationKind> restartOn)
            {
                Handle = handle;
                Effect = effect;
                AdditionalWorldOffset = additionalWorldOffset;
                RestartOn = restartOn;
            }

            internal CombatEffectHandle Handle { get; }
            internal CombatHeldEffect Effect { get; }
            // 마지막으로 낸 월드 위치 보정입니다. 같은 설정이어도 대상 병과가 바뀌면 다시 놓습니다.
            internal Vector3 AdditionalWorldOffset { get; }
            internal IReadOnlyList<TraitActivationKind> RestartOn { get; }
        }
    }
}
