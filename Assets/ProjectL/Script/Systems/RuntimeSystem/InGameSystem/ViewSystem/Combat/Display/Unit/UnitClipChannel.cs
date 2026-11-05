using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 하나의 애니메이션 그래프입니다(클립 설계 AC9·AC10, E5). 이벤트 컴포넌트가 붙은 Animator를 넘겨받아,
    /// 바닥(입력 0)에 프리팹 컨트롤러의 Idle을 깔고 위층(입력 1)에 사건 클립 하나를 바로 갈아 끼웁니다.
    /// 위층은 레이어 섞개 하나이고, 층 0이 주 클립(몸), 층 1..N이 같은 시각으로 도는 부속 클립(무기 등)입니다(설계 6차 AC17).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 그래프는 Unity 게임 시간으로 평가합니다(결정 Q-AC10 A). 사건 클립은 속도 0이라 시각은 대여(<see cref="ICombatUnitClip"/>)가 정하고,
    /// 그 자세는 같은 프레임의 Unity 평가에서 보입니다.
    /// </para>
    /// <para>
    /// 사건 클립이 끝나면 Animator 아래 Transform 로컬 값을 준비 때 값으로 되돌립니다(계획 P3·P17). 컨트롤러가 있으면 같은 프레임의 Unity 평가에서
    /// Idle이 자기 뼈를 다시 덮고, Idle이 움직이지 않는 뼈(무기 뼈 등)는 준비 때 자세로 남습니다(위층을 빼면 그 뼈는 그래프 바인딩에서 빠져 스스로 돌아오지 않음).
    /// 컨트롤러가 없으면 사건 클립이 없는 동안 Animator를 꺼 둡니다.
    /// Animator GameObject 자신의 Transform은 기억하지 않습니다(루트 곡선 금지, 자세 대여가 쓰는 자리일 수 있음).
    /// </para>
    /// <para>대여 수명은 <see cref="FieldUnitView"/>가 지키고, 이 클래스는 그래프만 다룹니다. 넘겨받은 컨트롤러·켜짐 상태는 <see cref="Dispose"/>가 돌려줍니다.</para>
    /// </remarks>
    internal sealed class UnitClipChannel : IDisposable
    {
        private const int BaseInput = 0;
        private const int OverlayInput = 1;

        private readonly Animator _animator;
        // 연결 때 복사한 지점 정의입니다. 번호 해석과 입력 배열은 이 값에 감춥니다.
        private readonly CombatAttackPoints _attackPoints;
        private readonly RuntimeAnimatorController _controller;
        private readonly bool _animatorWasEnabled;
        // Animator 아래 자식들의 준비 때 로컬 값입니다(계획 P3·P17). 사건 클립이 끝날 때 되돌립니다.
        private readonly Transform[] _restTransforms;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3[] _restScales;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        // 위층입니다. 층 0 = 주 클립, 층 1..N = 부속 클립(설계 6차 AC17). 사건 클립이 없으면 유효하지 않습니다.
        private AnimationLayerMixerPlayable _overlay;
        private bool _disposed;

        /// <summary>
        /// Animator의 컨트롤러를 읽고 컨트롤러 칸을 비운 뒤(우리 그래프 하나만 Animator를 움직이게) 그래프를 만듭니다.
        /// 컨트롤러가 있으면 바로 Idle을 돌립니다.
        /// </summary>
        internal UnitClipChannel(CombatAnimationEvents events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            _animator = events.Animator;
            if (_animator == null)
                throw new InvalidOperationException($"[UnitClipChannel] 전투 클립 이벤트 컴포넌트와 같은 GameObject에 Animator가 없습니다: {events.name}");

            _attackPoints = events.CaptureAttackPoints();
            _controller = _animator.runtimeAnimatorController;
            _animatorWasEnabled = _animator.enabled;
            Transform self = _animator.transform;
            Transform[] all = _animator.GetComponentsInChildren<Transform>(true);
            _restTransforms = new Transform[all.Length - 1];
            int count = 0;
            foreach (Transform child in all)
                if (child != self) _restTransforms[count++] = child;
            _restPositions = new Vector3[count];
            _restRotations = new Quaternion[count];
            _restScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                _restPositions[i] = _restTransforms[i].localPosition;
                _restRotations[i] = _restTransforms[i].localRotation;
                _restScales[i] = _restTransforms[i].localScale;
            }

            try
            {
                _animator.runtimeAnimatorController = null;
                _animator.applyRootMotion = false;
                _animator.fireEvents = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _graph = PlayableGraph.Create($"UnitClip_{events.name}");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(_graph, "Unit", _animator);
                _mixer = AnimationMixerPlayable.Create(_graph, 2);
                output.SetSourcePlayable(_mixer);
                if (_controller != null)
                {
                    var idle = AnimatorControllerPlayable.Create(_graph, _controller);
                    _graph.Connect(idle, 0, _mixer, BaseInput);
                    _mixer.SetInputWeight(BaseInput, 1f);
                    _animator.enabled = true;
                    _graph.Play();
                }
                else
                {
                    _animator.enabled = false;
                }
            }
            catch
            {
                if (_graph.IsValid()) _graph.Destroy();
                _animator.runtimeAnimatorController = _controller;
                _animator.enabled = _animatorWasEnabled;
                throw;
            }
        }

        /// <summary>바닥(Idle 컨트롤러)이 있는지입니다(시험용 읽기).</summary>
        internal bool HasBase => _controller != null;

        /// <summary>위층 사건 클립이 꽂혀 있는지입니다(시험용 읽기).</summary>
        internal bool HasOverlay => _overlay.IsValid();

        /// <summary>그래프입니다(시험용 읽기). EditMode에서는 Unity 시간이 흐르지 않아 시험이 직접 평가합니다.</summary>
        internal PlayableGraph Graph => _graph;

        /// <summary>
        /// 사건 클립(주 클립 + 부속 클립)을 위층에 꽂고 시각을 0으로 둡니다. 바닥 가중치는 0이 됩니다(바로 갈아 끼움, 설계 AC9).
        /// 이미 위층이 있으면 예외입니다. 값의 이벤트 시각은 쓰지 않습니다(시각은 <see cref="SetTime"/>으로 받음).
        /// 꽂는 도중 실패하면 만든 위층을 지우고 예외를 그대로 냅니다(계획 P16).
        /// </summary>
        internal void Begin(CombatClipData clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (_disposed) throw new ObjectDisposedException(nameof(UnitClipChannel));
            if (_overlay.IsValid())
                throw new InvalidOperationException("[UnitClipChannel] 이전 사건 클립이 아직 남아 있습니다.");
            IReadOnlyList<AnimationClip> companions = clip.Companions;
            try
            {
                _overlay = AnimationLayerMixerPlayable.Create(_graph, companions.Count + 1);
                ConnectLayer(0, clip.Clip);
                for (int i = 0; i < companions.Count; i++)
                    ConnectLayer(i + 1, companions[i]);
                _graph.Connect(_overlay, 0, _mixer, OverlayInput);
            }
            catch
            {
                if (_overlay.IsValid()) _graph.DestroySubgraph(_overlay);
                _overlay = default;
                throw;
            }

            _mixer.SetInputWeight(BaseInput, 0f);
            _mixer.SetInputWeight(OverlayInput, 1f);
            if (_controller == null) _animator.enabled = true;
            _graph.Play();
        }

        /// <summary>위층 모든 층(주 클립·부속 클립)의 시각(초)을 같은 값으로 바꿉니다. 평가하지 않습니다.</summary>
        internal void SetTime(float time)
        {
            if (!_overlay.IsValid())
                throw new InvalidOperationException("[UnitClipChannel] 꽂힌 사건 클립이 없습니다.");
            if (!float.IsFinite(time) || time < 0f)
                throw new ArgumentOutOfRangeException(nameof(time), time, "클립 시각은 유한한 0 이상이어야 합니다.");
            int layers = _overlay.GetInputCount();
            for (int i = 0; i < layers; i++)
                _overlay.GetInput(i).SetTime(time);
        }

        /// <summary>
        /// 위층을 빼서 아래 클립째 파괴하고 바닥으로 돌아갑니다. Animator 아래 Transform은 준비 때 값으로 되돌립니다(계획 P17).
        /// 컨트롤러가 없으면 그래프를 멈추고 Animator를 끕니다. 위층이 없으면 아무 일도 하지 않습니다.
        /// </summary>
        internal void End()
        {
            if (!_overlay.IsValid()) return;
            try
            {
                _graph.Disconnect(_mixer, OverlayInput);
                _graph.DestroySubgraph(_overlay);
            }
            finally
            {
                _overlay = default;
                _mixer.SetInputWeight(OverlayInput, 0f);
                if (_controller != null)
                {
                    _mixer.SetInputWeight(BaseInput, 1f);
                }
                else
                {
                    if (_graph.IsValid()) _graph.Stop();
                    if (_animator != null) _animator.enabled = false;
                }

                RestorePose();
            }
        }

        /// <summary>발사 지점의 지금 월드 위치입니다. 포구가 없으면 false입니다.</summary>
        internal bool TryGetMuzzle(out Vector3 position) => TryGetMuzzle(0, out position);

        /// <summary>번호별 총구의 현재 월드 위치입니다. 정의/총구가 없으면 false입니다.</summary>
        internal bool TryGetMuzzle(int point, out Vector3 position)
        {
            if (!_attackPoints.TryGetMuzzle(point, out Transform muzzle))
            {
                position = default;
                return false;
            }
            position = muzzle.position;
            return true;
        }

        /// <summary>이펙트 지점의 지금 월드 위치·회전입니다. 지점이 없으면 false입니다.</summary>
        internal bool TryGetEffectPoint(out Vector3 position, out Quaternion rotation) => TryGetEffectPoint(0, out position, out rotation);

        /// <summary>번호별 이펙트 지점의 현재 월드 자세입니다. 정의/지점이 없으면 false입니다.</summary>
        internal bool TryGetEffectPoint(int point, out Vector3 position, out Quaternion rotation)
        {
            if (!_attackPoints.TryGetEffectPoint(point, out Transform effectPoint))
            {
                position = default;
                rotation = Quaternion.identity;
                return false;
            }
            effectPoint.GetPositionAndRotation(out position, out rotation);
            return true;
        }

        /// <summary>표시가 꺼질 때 그래프를 멈춥니다.</summary>
        internal void Pause()
        {
            if (_graph.IsValid() && _graph.IsPlaying()) _graph.Stop();
        }

        /// <summary>표시가 켜질 때 그래프를 다시 돌립니다. 돌릴 것(바닥 또는 위층)이 있을 때만 돌립니다.</summary>
        internal void Resume()
        {
            if (_graph.IsValid() && (_controller != null || _overlay.IsValid())) _graph.Play();
        }

        /// <summary>위층을 끝내고 그래프를 파괴한 뒤, 넘겨받은 컨트롤러와 Animator 켜짐 상태를 돌려줍니다. 여러 번 불러도 안전합니다.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { End(); }
            finally
            {
                try { if (_graph.IsValid()) _graph.Destroy(); }
                finally
                {
                    _graph = default;
                    _mixer = default;
                    _overlay = default;
                    if (_animator != null)
                    {
                        _animator.runtimeAnimatorController = _controller;
                        _animator.enabled = _animatorWasEnabled;
                    }
                }
            }
        }

        /// <summary>층 하나에 클립을 속도 0·시각 0·IK 끔으로 꽂고 가중치 1(덮어쓰기)로 둡니다.</summary>
        private void ConnectLayer(int layer, AnimationClip source)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, source);
            playable.SetSpeed(0);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetTime(0);
            _graph.Connect(playable, 0, _overlay, layer);
            _overlay.SetInputWeight(layer, 1f);
        }

        private void RestorePose()
        {
            for (int i = 0; i < _restTransforms.Length; i++)
            {
                Transform child = _restTransforms[i];
                if (child == null) continue;
                child.localPosition = _restPositions[i];
                child.localRotation = _restRotations[i];
                child.localScale = _restScales[i];
            }
        }
    }
}
