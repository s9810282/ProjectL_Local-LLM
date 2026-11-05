using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 전투 연출이 이펙트를 내는 창구입니다(이펙트 설계 EF1). Framework 이펙트(<see cref="IEffectSystemServices"/>)의 Scope 하나를 가지고,
    /// 받은 월드 자리에 이펙트를 냅니다. 입구는 <see cref="Play"/> 하나이고(이펙트 끝 규칙 설계 EL5), 놓기 값(<see cref="CombatEffectPlacement"/>)이
    /// 따라갈 유닛·끝(자연·시간·부르는 쪽이 거둠)을 정합니다. 모든 이펙트는 필드 루트 아래 받침(<see cref="CombatEffectFollower"/>)에 놓입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope는 처음 낼 때 한 번 받습니다. 이펙트 시스템은 Scope를 Shutdown까지 쥐고 있어(사용 평가 G2) 낼 때마다 받으면 쌓이기 때문입니다.
    /// Framework Scope에는 요청 하나만 멈추는 API가 없어, 하나씩 거두기는 받침이 맡습니다(시간 끝·손잡이 <see cref="CombatEffectHandle"/>).
    /// 전부 지우기는 <see cref="StopAll"/>이고, 결과 재생 취소·해제 때 Presenter가 부릅니다(결정 Q-E4 A).
    /// </para>
    /// <para>
    /// 이펙트를 유닛 아래에 붙이지 않는 이유: 유닛 표시가 꺼지거나 파괴되면 파티클 멈춤 알림이 오지 않거나 풀의 이펙트가 함께 파괴됩니다(설계 E-R1).
    /// 그래서 따라가는 이펙트도 필드 루트 아래의 받침(<see cref="CombatEffectFollower"/>)에 놓고, 받침이 몸 위치를 읽어 옮깁니다.
    /// 받침은 이 창구가 만들고 빌려 주며, 이펙트가 끝나 비면 다시 씁니다.
    /// </para>
    /// <para>
    /// 종류마다 처음 낸 인스턴스로 끝나지 않는 파티클(Looping·Ring Buffer)이 있는지 한 번 판단해 기억합니다(계획 TP-E25). 링 버퍼는 회수 모양을 정하고,
    /// 끝나지 않는 파티클이 있는데 자연 끝으로 내면 종류마다 한 번 경고합니다(설계 EL6, 결정 Q-EL8 A). View DB는 Render 이펙트 표의 프리팹을 읽지 않아 DB 생성 때는 막지 않습니다.
    /// </para>
    /// <para>
    /// 오래 보이는 이펙트(상태 유지·특성 상시)는 유지 표시 부품(<see cref="CombatPersistentEffects"/>)이 "부르는 쪽이 거둠" 놓기 값으로 내고 손잡이로 거둡니다(이펙트 끝 규칙 설계 EL4).
    /// </para>
    /// </remarks>
    internal sealed class CombatEffectWindow
    {
        private readonly IEffectSystemServices _effects;
        private readonly Transform _parent;
        private readonly Func<EntityId, Func<(bool Bound, Vector3? Body)>> _track;
        private Func<EntityId, CategoryKey?> _categoryOf;
        private readonly List<CombatEffectFollower> _followers = new();
        // 행동 끝 큐로 낸 이펙트입니다. 행동 묶음이 먼저 거두고, 남은 것은 결과 끝에 거둡니다(이펙트 끝 규칙 계획 TP-E4).
        private readonly List<CombatEffectHandle> _actionBound = new();
        // 종류마다 한 번 판단한 파티클 성질과, 경고를 남긴 종류입니다(계획 TP-E25·TP-E17). 판단에 쓰는 목록은 다시 씁니다.
        private readonly Dictionary<EffectType, EffectTraits> _traits = new();
        private readonly HashSet<EffectType> _warned = new();
        private readonly List<ParticleSystem> _particles = new();
        private EffectScope _scope;
        private bool _released;

        /// <param name="parent">이펙트를 놓을 부모입니다. 전투 필드 루트(<c>FieldView.CellsRoot</c>)를 넘깁니다.</param>
        /// <param name="track">
        /// 유닛 하나의 몸 위치 읽기를 줍니다(<c>FieldUnitPresenter.TrackBodyPresence</c>). 표시가 없으면 null이고, 읽기는 "그 표시가 아직 그 유닛에 붙어 있나"와
        /// 몸 위치(숨었으면 null)를 돌려줍니다(이펙트 따라가기·회수 설계 FE11).
        /// </param>
        internal CombatEffectWindow(IEffectSystemServices effects, Transform parent, Func<EntityId, Func<(bool Bound, Vector3? Body)>> track)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        /// <summary>세션의 Scope를 빌립니다. 창구는 효과만 회수하며 Scope의 최종 수명은 서비스가 소유합니다.</summary>
        internal CombatEffectWindow(IEffectSystemServices effects, Transform parent, Func<EntityId, Func<(bool Bound, Vector3? Body)>> track, EffectScope scope)
            : this(effects, parent, track)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        /// <summary>현재 보인 사실의 병과 조회를 연결마다 한 번 받습니다. 유지 이펙트와 사건 큐가 같은 조회를 씁니다.</summary>
        internal void BindBodyCategories(Func<EntityId, CategoryKey?> categoryOf)
        {
            if (_released) throw new ObjectDisposedException(nameof(CombatEffectWindow));
            if (categoryOf == null) throw new ArgumentNullException(nameof(categoryOf));
            if (_categoryOf != null)
                throw new InvalidOperationException("[CombatEffectWindow] 몸 병과 조회는 한 번만 연결합니다.");
            _categoryOf = categoryOf;
        }

        /// <summary>이펙트를 놓는 유닛의 Rider 월드 높이 보정입니다. 유닛이 없는 자리와 보정 0은 조회가 필요 없습니다.</summary>
        internal Vector3 AdditionalWorldOffsetOf(EntityId body, float riderYOffset)
        {
            if (body.IsEmpty || riderYOffset == 0f)
                return Vector3.zero;
            if (_categoryOf == null)
                throw new InvalidOperationException("[CombatEffectWindow] Rider 높이 보정에 필요한 몸 병과 조회가 연결되지 않았습니다.");
            // 구조물처럼 캐릭터 병과가 없는 대상은 기본 위치를 씁니다.
            return _categoryOf(body) == CategoryKey.Rider ? Vector3.up * riderYOffset : Vector3.zero;
        }

        /// <summary>
        /// <paramref name="type"/> 이펙트를 월드 자리 <paramref name="at"/>에 냅니다(입구 하나, 설계 EL5). 놓기 값이 따라갈 유닛이면 그 몸 위치를 따라가고
        /// (위치만, 낸 자리와 몸 위치의 차이는 유지), 그 유닛 표시가 지금 없으면 그 자리에 머뭅니다. 따라가던 표시가 사라지면 바로 멈추고(결정 Q-FE3),
        /// 숨었을 뿐이면 같이 숨었다가 다시 보이면 같이 보입니다(FE11). 끝은 놓기 값이 정합니다(자연·시간·부르는 쪽이 손잡이로 거둠).
        /// </summary>
        /// <returns>이 이펙트의 손잡이입니다. 거두지 않을 호출은 버려도 됩니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">비었거나 정의되지 않은 종류일 때입니다(낼 것이 없는 호출은 부르는 쪽 실수).</exception>
        /// <exception cref="ObjectDisposedException"><see cref="Release"/> 뒤일 때입니다.</exception>
        internal CombatEffectHandle Play(EffectType type, Pose at, CombatEffectPlacement placement)
        {
            Transform parent = Prepare(type);
            Func<(bool Bound, Vector3? Body)> presence = placement.Follow.IsEmpty ? null : _track(placement.Follow);
            (bool Bound, Vector3? Body) now = presence?.Invoke() ?? default;
            CombatEffectFollower follower = RentFollower(parent);
            follower.Begin(now.Bound ? presence : null, now.Body ?? at.position, placement.Seconds);
            PlayOn(follower, type, at, warnUnending: placement.IsNatural);
            CombatEffectHandle handle = follower.Handle;
            if (placement.ActionBound)
                _actionBound.Add(handle);
            return handle;
        }

        /// <summary>
        /// 행동 끝 큐로 낸 이펙트 중 남은 것을 모두 거둡니다(계획 TP-E4). 결과 끝 최종 사실을 그릴 때 사건 Presenter가 부릅니다.
        /// 행동 묶음이 이미 거둔 것·밖에서 끝난 것은 손잡이가 무시합니다. 대신 맞기처럼 행동 묶음이 없는 길의 행동 끝 큐는 여기서 끝납니다.
        /// </summary>
        internal void RecallActionBound()
        {
            for (int i = 0; i < _actionBound.Count; i++)
                _actionBound[i].Recall();
            _actionBound.Clear();
        }

        /// <summary>
        /// 낸 이펙트 중 남은 것을 모두 풀로 돌립니다. 아직 낸 적이 없으면 아무 일도 하지 않습니다. 빈 받침은 다음 프레임에 스스로 꺼집니다.
        /// 숨긴 이펙트는 그리기를 먼저 되돌립니다(숨긴 채 풀로 돌아가면 다음에 쓸 때 보이지 않음, FE11).
        /// </summary>
        internal void StopAll()
        {
            _actionBound.Clear();
            foreach (CombatEffectFollower follower in _followers)
                if (follower != null)
                    follower.ShowRenderers();
            _scope?.StopAll();
        }

        /// <summary>남은 이펙트를 돌리고 받침을 지운 뒤 창구를 닫습니다. 그 뒤 <see cref="Play"/>는 예외입니다. 여러 번 불러도 안전합니다.</summary>
        internal void Release()
        {
            if (_released) return;
            _released = true;
            // 이펙트를 먼저 풀로 돌려(부모가 풀 루트로 바뀜) 받침을 지울 때 풀의 이펙트가 함께 파괴되지 않게 합니다.
            StopAll();
            foreach (CombatEffectFollower follower in _followers)
                if (follower != null)
                    UnityEngine.Object.Destroy(follower.gameObject);
            _followers.Clear();
            _categoryOf = null;
        }

        // 낼 수 있는 상태인지 보고 Scope를 준비합니다. 놓을 부모(필드 루트)를 돌려줍니다.
        private Transform Prepare(EffectType type)
        {
            if (_released) throw new ObjectDisposedException(nameof(CombatEffectWindow));
            if (!CombatPresentationEffectRules.IsPlayable(type))
                throw new ArgumentOutOfRangeException(nameof(type), type, CombatPresentationEffectRules.PlayableRule);
            if (_parent == null)
                throw new InvalidOperationException("[CombatEffectWindow] 이펙트를 놓을 부모가 파괴됐습니다.");
            _scope ??= _effects.GetEffectHandle()
                ?? throw new InvalidOperationException("[CombatEffectWindow] EffectScope가 없습니다.");
            return _parent;
        }

        // 월드 값을 받침 지역 값으로 바꿔 받침 아래에 내고, 종류의 파티클 성질을 받침에 넘깁니다. 이펙트 시스템은 낼 때 바로 받침 아래로 옮깁니다(EffectScope.Play).
        private void PlayOn(CombatEffectFollower follower, EffectType type, Pose at, bool warnUnending)
        {
            Transform anchor = follower.transform;
            Vector3 localPosition = anchor.InverseTransformPoint(at.position);
            Quaternion localRotation = Quaternion.Inverse(anchor.rotation) * at.rotation;
            _scope.Play(new EffectScopeRequest(type, anchor, localPosition, localRotation));
            EffectTraits traits = TraitsOf(type, anchor);
            follower.Adopt(traits.RingBuffer);
            if (warnUnending && traits.Unending && _warned.Add(type))
                Debug.LogWarning($"[CombatEffectWindow] 끝이 자연(Natural)인 이펙트에 스스로 끝나지 않는 파티클(Looping·Ring Buffer)이 있어 남을 수 있습니다: {type}. "
                    + "큐의 End를 Seconds로 고르거나 프리팹을 고칩니다(이펙트 끝 규칙 설계 EL6).");
        }

        // 종류마다 처음 낸 인스턴스로 한 번 판단합니다. 같은 종류는 같은 프리팹입니다(계획 TP-E25).
        private EffectTraits TraitsOf(EffectType type, Transform anchor)
        {
            if (_traits.TryGetValue(type, out EffectTraits known))
                return known;
            bool unending = false;
            bool ringBuffer = false;
            anchor.GetComponentsInChildren(true, _particles);
            for (int i = 0; i < _particles.Count; i++)
            {
                ParticleSystem.MainModule main = _particles[i].main;
                bool ring = main.ringBufferMode != ParticleSystemRingBufferMode.Disabled;
                ringBuffer |= ring;
                unending |= ring || main.loop;
            }
            _particles.Clear();
            var traits = new EffectTraits(unending, ringBuffer);
            _traits.Add(type, traits);
            return traits;
        }

        // 한 종류 이펙트의 파티클 성질입니다. 끝나지 않음 = Looping이나 Ring Buffer 파티클이 하나라도 있음, 링 버퍼 = Ring Buffer 파티클이 있음.
        private readonly struct EffectTraits
        {
            internal EffectTraits(bool unending, bool ringBuffer)
            {
                Unending = unending;
                RingBuffer = ringBuffer;
            }

            internal bool Unending { get; }
            internal bool RingBuffer { get; }
        }

        // 빈 받침을 다시 쓰고, 없으면 필드 루트 아래에 새로 만듭니다. 파괴된 받침(필드 루트가 먼저 사라짐)은 목록에서 뺍니다.
        private CombatEffectFollower RentFollower(Transform parent)
        {
            for (int i = _followers.Count - 1; i >= 0; i--)
            {
                CombatEffectFollower follower = _followers[i];
                if (follower == null)
                {
                    _followers.RemoveAt(i);
                    continue;
                }
                if (follower.IsFree)
                    return follower;
            }
            var anchor = new GameObject("CombatEffectFollow");
            anchor.transform.SetParent(parent, false);
            CombatEffectFollower created = anchor.AddComponent<CombatEffectFollower>();
            _followers.Add(created);
            return created;
        }
    }
}
