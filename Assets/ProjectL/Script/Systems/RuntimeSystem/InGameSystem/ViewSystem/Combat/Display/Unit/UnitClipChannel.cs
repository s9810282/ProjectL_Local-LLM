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
    /// 바닥(입력 0)에 프리팹 컨트롤러의 Idle을 깔고 위층에 사건 클립을 얹습니다.
    /// 위층은 둘(입력 1·2)을 번갈아 쓰고(클립 칸 다시 짜기 계획 TP-11), 위층 하나는 레이어 섞개입니다(층 0 = 몸, 탑승물·부속이 그 뒤, 설계 P6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 그래프는 Unity 게임 시간으로 평가합니다(결정 Q-AC10 A). 사건 클립은 속도 0이라 시각은 대여(<see cref="ICombatUnitClip"/>)가 정하고,
    /// 그 자세는 같은 프레임의 Unity 평가에서 보입니다.
    /// </para>
    /// <para>
    /// 섞기(설계 CS7)는 섞개 입력 셋의 가중치를 시작 값에서 목표(한 입력 1, 나머지 0)로 직선으로 옮깁니다. In = 바닥 → 위층(<see cref="Begin"/>),
    /// Between = 위층 → 다른 위층(<see cref="CrossFade"/>), Out = 위층 → 바닥(뒤 단계 끝·본 단계 끝, <see cref="HandOff"/>). 섞는 동안 나가는 위층은
    /// 마지막으로 받은 시각에 멈춰 있습니다. 섞기가 끝나면 가중치 0인 위층을 빼고, 바닥으로 다 돌아갔으면 아래 되돌리기를 한 번 합니다.
    /// 섞기 시간은 대여 동안은 대여(<see cref="AdvanceBlend"/>), 넘긴 뒤에는 <see cref="TickTail"/>이 넣습니다.
    /// 컨트롤러가 없으면 섞을 Idle이 없어 In·Out은 바로 바꿉니다.
    /// </para>
    /// <para>
    /// 사건 클립이 바닥으로 돌아가면 Animator 아래 Transform 로컬 값을 준비 때 값으로 되돌립니다(계획 P3·P17). 컨트롤러가 있으면 같은 프레임의 Unity 평가에서
    /// Idle이 자기 뼈를 다시 덮고, Idle이 움직이지 않는 뼈(무기 뼈 등)는 준비 때 자세로 남습니다(위층을 빼면 그 뼈는 그래프 바인딩에서 빠져 스스로 돌아오지 않음).
    /// 컨트롤러가 없으면 사건 클립이 없는 동안 Animator를 꺼 둡니다.
    /// Animator GameObject 자신의 Transform은 기억하지 않습니다(루트 곡선 금지, 자세 대여가 쓰는 자리일 수 있음).
    /// </para>
    /// <para>대여 수명은 <see cref="FieldUnitView"/>가 지키고, 이 클래스는 그래프만 다룹니다. 넘겨받으며 바꾼 Animator 값 다섯은 <see cref="Dispose"/>가 돌려줍니다(검토 V8).</para>
    /// <para>
    /// 꼬리(클립 칸 다시 짜기 설계 CS6·CS7)는 대여가 <see cref="HandOff"/>로 넘긴 뒤 이 채널이 혼자 돌리는 것입니다: 뒤 단계, 그 뒤 Out 섞기, 넘길 때 남은 섞기.
    /// 시각은 <see cref="TickTail"/>이 게임 시간으로 넣고(설계 P10), 끝나면 바닥으로 돌아갑니다. 다음 대여의 <see cref="Begin"/>·<see cref="CutTail"/>·<see cref="End"/>·<see cref="Dispose"/>가
    /// 그 자리에서 끊습니다(섞어서 넘어가지 않음, 설계 P8·P9).
    /// </para>
    /// </remarks>
    internal sealed class UnitClipChannel : IDisposable
    {
        private const int BaseInput = 0;
        private const int FirstOverlay = 1;
        private const int SecondOverlay = 2;
        private const int InputCount = 3;
        private const int NoFade = -1;

        private readonly Animator _animator;
        // 연결 때 복사한 지점 정의입니다. 번호 해석과 입력 배열은 이 값에 감춥니다.
        private readonly CombatEventPoints _eventPoints;
        private readonly CombatWeaponPitchRig _pitchRig;
        // 넘겨받으며 바꾸는 Animator 값 다섯의 원래 값입니다(검토 V8). Dispose·생성 실패에서 모두 돌려줍니다.
        private readonly RuntimeAnimatorController _controller;
        private readonly bool _animatorWasEnabled;
        private readonly bool _applyRootMotion;
        private readonly bool _fireEvents;
        private readonly AnimatorCullingMode _cullingMode;
        // Animator 아래 자식들의 준비 때 로컬 값입니다(계획 P3·P17). 사건 클립이 바닥으로 돌아갈 때 되돌립니다.
        private readonly Transform[] _restTransforms;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3[] _restScales;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        // 입력 1·2의 위층입니다(입력 0 칸은 쓰지 않음). 비어 있으면 유효하지 않습니다.
        private readonly AnimationLayerMixerPlayable[] _overlays = new AnimationLayerMixerPlayable[InputCount];
        // 지금 시각을 받는 위층 입력입니다. 위층이 없으면 BaseInput입니다.
        private int _current = BaseInput;
        // 섞개 입력의 지금 가중치와 섞기 시작 때 가중치입니다. 합은 늘 1입니다.
        private readonly float[] _weights = new float[InputCount];
        private readonly float[] _fadeFrom = new float[InputCount];
        private int _fadeTarget = NoFade;
        private float _fadeElapsed;
        private float _fadeSeconds;
        // 대여가 시각을 넘겼는지입니다(HandOff 뒤). 참이면 이 채널이 혼자 진행합니다.
        private bool _handed;
        // 넘긴 프레임 번호입니다. 그 프레임에는 TickTail이 시간을 넣지 않습니다(대여가 이미 넣음).
        private int _handedFrame = -1;
        // 뒤 단계 꼬리입니다(설계 CS6). 꼬리가 없으면 _tailing이 거짓입니다.
        private bool _tailing;
        private float _tailTime;
        private float _tailEnd;
        private float _tailSpeed;
        // 뒤 단계가 끝나면(또는 뒤 단계 없이 넘기면 바로) 쓸 Out 블렌드입니다.
        private CombatClipBlend _fadeOut;
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

            _eventPoints = events.CaptureEventPoints();
            _pitchRig = events.GetComponent<CombatWeaponPitchRig>();
            _controller = _animator.runtimeAnimatorController;
            _animatorWasEnabled = _animator.enabled;
            _applyRootMotion = _animator.applyRootMotion;
            _fireEvents = _animator.fireEvents;
            _cullingMode = _animator.cullingMode;
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
                _mixer = AnimationMixerPlayable.Create(_graph, InputCount);
                Playable source = _mixer;
                if (_pitchRig != null)
                    source = _pitchRig.Bind(_graph, source);
                var output = AnimationPlayableOutput.Create(_graph, "Unit", _animator);
                output.SetSourcePlayable(source);
                SetOnly(BaseInput);
                if (_controller != null)
                {
                    var idle = AnimatorControllerPlayable.Create(_graph, _controller);
                    _graph.Connect(idle, 0, _mixer, BaseInput);
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
                _pitchRig?.Clear();
                RestoreAnimator();
                throw;
            }
        }

        /// <summary>바닥(Idle 컨트롤러)이 있는지입니다(진단용 읽기, 지금 부르는 곳은 없음).</summary>
        internal bool HasBase => _controller != null;

        /// <summary>위층 사건 클립이 꽂혀 있는지입니다(진단용 읽기, 지금 부르는 곳은 없음).</summary>
        internal bool HasOverlay => _current != BaseInput;

        /// <summary>그래프입니다(진단용 읽기, 지금 부르는 곳은 없음). EditMode에서는 Unity 시간이 흐르지 않아 직접 평가해야 합니다.</summary>
        internal PlayableGraph Graph => _graph;

        /// <summary>섞는 중인지입니다(설계 CS7). 대여가 본 단계 끝에 남은 섞기를 넘길지 묻습니다.</summary>
        internal bool IsBlending => _fadeTarget != NoFade;

        /// <summary>넘겨받은 일(뒤 단계·Out 섞기·남은 섞기)을 혼자 진행하는 중인지입니다. 표시가 프레임마다 <see cref="TickTail"/>을 부를지 묻습니다.</summary>
        internal bool HasTail => _handed && (_tailing || _fadeTarget != NoFade);

        /// <summary>
        /// 대여가 돌아가도 이어야 할 꼬리(뒤 단계 또는 Out 섞기)가 있는지입니다. 거짓이면 대여 반환이 지금처럼 바로 바닥으로 돌아갑니다.
        /// 넘길 때 남은 In·Between 섞기만 있으면 거짓입니다(그 섞기는 대여 반환에서 끊김).
        /// </summary>
        internal bool HoldsTail => _handed && (_tailing || _fadeTarget == BaseInput);

        /// <summary>
        /// 대여 하나의 첫 단계를 꽂습니다(In, 설계 CS7). 남은 위층·꼬리·섞기가 있으면 그 자리에서 빼고 되돌린 뒤(설계 P9) 바닥에서 새 클립으로 섞습니다.
        /// 블렌드가 꺼져 있거나 컨트롤러가 없으면 바로 바꿉니다(지금과 같음). 시각은 0입니다.
        /// 값의 이벤트 시각은 쓰지 않습니다(시각은 <see cref="SetTime"/>으로 받음). 꽂는 도중 실패하면 만든 위층을 지우고 예외를 그대로 냅니다(계획 P16).
        /// </summary>
        internal void Begin(CombatClipData clip, CombatClipBlend blend)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (_disposed) throw new ObjectDisposedException(nameof(UnitClipChannel));
            End();
            Connect(FirstOverlay, clip);
            _current = FirstOverlay;
            StartFade(FirstOverlay, _controller != null ? blend : CombatClipBlend.Off);
            if (_controller == null) _animator.enabled = true;
            _graph.Play();
        }

        /// <summary>
        /// 같은 대여 안에서 다음 단계를 꽂습니다(Between, 설계 CS7). 지금 위층은 마지막 시각에 멈춘 채 다른 위층 입력에 새 클립을 꽂아 섞습니다.
        /// 다른 위층 입력에 앞선 섞기의 나가는 위층이 아직 있으면 그 가중치를 지금 위층에 얹고 뺍니다(아주 짧은 단계에서만).
        /// 블렌드가 꺼져 있으면 지금처럼 앞 위층을 빼고 되돌린 뒤 바로 바꿉니다. 조준은 풀립니다(새 단계가 다시 겁니다). 시각은 0입니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">꽂힌 위층이 없을 때입니다(먼저 <see cref="Begin"/>).</exception>
        internal void CrossFade(CombatClipData clip, CombatClipBlend blend)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (_disposed) throw new ObjectDisposedException(nameof(UnitClipChannel));
            if (_current == BaseInput)
                throw new InvalidOperationException("[UnitClipChannel] 다음 단계를 꽂을 앞 단계가 없습니다.");
            _pitchRig?.ResetAim();
            int next = _current == FirstOverlay ? SecondOverlay : FirstOverlay;
            if (_overlays[next].IsValid())
            {
                _weights[_current] += _weights[next];
                _weights[next] = 0f;
                RemoveOverlay(next);
                RestorePose();
            }
            Connect(next, clip);
            _current = next;
            StartFade(next, blend);
        }

        /// <summary>지금 위층 모든 층(주 클립·탑승물·부속 클립)의 시각(초)을 같은 값으로 바꿉니다. 나가는 위층은 그대로입니다. 평가하지 않습니다.</summary>
        internal void SetTime(float time)
        {
            if (_current == BaseInput)
                throw new InvalidOperationException("[UnitClipChannel] 꽂힌 사건 클립이 없습니다.");
            if (!float.IsFinite(time) || time < 0f)
                throw new ArgumentOutOfRangeException(nameof(time), time, "클립 시각은 유한한 0 이상이어야 합니다.");
            AnimationLayerMixerPlayable overlay = _overlays[_current];
            int layers = overlay.GetInputCount();
            for (int i = 0; i < layers; i++)
                overlay.GetInput(i).SetTime(time);
            _pitchRig?.SetTime(time);
        }

        /// <summary>섞기를 실제 초만큼 진행합니다(재생 배율은 걸지 않음, 설계 P12). 섞는 중이 아니면 아무 일도 하지 않습니다. 음수·수가 아닌 시간은 0으로 봅니다.</summary>
        internal void AdvanceBlend(float deltaTime)
        {
            if (_fadeTarget == NoFade) return;
            if (deltaTime > 0f && float.IsFinite(deltaTime)) _fadeElapsed += deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeSeconds);
            for (int i = 0; i < InputCount; i++)
                _weights[i] = Mathf.Lerp(_fadeFrom[i], i == _fadeTarget ? 1f : 0f, t);
            ApplyWeights();
            if (t >= 1f)
                FinishFade();
        }

        /// <summary>
        /// 대여가 본 단계 끝에 시각을 넘깁니다(클립 칸 다시 짜기 설계 CS6·CS7). 뒤 단계가 있으면 Between으로 섞어 꽂고 시각 0부터 게임 시간 × <paramref name="speed"/>로 혼자 돌리며,
        /// 끝 이벤트 시각에 닿으면 Out 블렌드로 바닥에 돌아갑니다. 뒤 단계가 없으면 바로 Out으로 섞기 시작합니다. Out이 꺼져 있으면 뒤 단계 끝에서 바로 돌아가고,
        /// 뒤 단계도 없으면 바닥으로 가지 않고 지금 자세로 남습니다(대여 반환이 돌려보냄, 지금과 같음). 컨트롤러(Idle)가 없는 유닛은 Out을 꺼진 것으로 봅니다.
        /// 넘길 때 남은 섞기는 이어서 진행합니다(넘긴 프레임은 대여가 이미 시간을 넣어 다음 프레임부터).
        /// </summary>
        /// <param name="tail">뒤 단계입니다. 없으면 null입니다.</param>
        /// <exception cref="InvalidOperationException">꽂힌 위층이 없을 때입니다.</exception>
        internal void HandOff(CombatClipData tail, float speed, CombatClipBlend between, CombatClipBlend fadeOut)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitClipChannel));
            if (_current == BaseInput)
                throw new InvalidOperationException("[UnitClipChannel] 넘길 사건 클립이 없습니다.");
            _handed = true;
            // 넘긴 프레임에는 대여(클립 조각)가 이미 그 프레임의 시간을 넣었습니다. 같은 프레임의 TickTail은 건너뜁니다(묶음 끝 검토 L3).
            _handedFrame = Time.frameCount;
            // 컨트롤러(Idle)가 없으면 섞을 바닥이 없어 Out은 꺼진 것으로 봅니다 — 뒤 단계가 없으면 지금 자세로 남고 대여 반환이 돌려보냅니다(묶음 끝 검토 L4).
            _fadeOut = _controller != null ? fadeOut : CombatClipBlend.Off;
            if (tail == null)
            {
                if (_fadeOut.Enabled) StartFadeOut();
                return;
            }
            CombatPresentationValueRules.RequirePositive(speed, nameof(speed));
            CrossFade(tail, between);
            _tailing = true;
            _tailTime = 0f;
            _tailEnd = tail.EndTime;
            _tailSpeed = speed;
            SetTime(0f);
            if (_tailEnd <= 0f)
                FinishTail();
        }

        /// <summary>
        /// 넘겨받은 일(뒤 단계·섞기)을 진행합니다. 넘겨받지 않았거나 넘겨받은 그 프레임이면 아무 일도 하지 않습니다(그 프레임 시간은 대여가 넣음 — 같은 프레임에 두 번 넣지 않게).
        /// 음수·수가 아닌 시간은 0으로 봅니다.
        /// </summary>
        internal void TickTail(float deltaTime)
        {
            if (!_handed || Time.frameCount == _handedFrame) return;
            AdvanceBlend(deltaTime);
            if (!_tailing) return;
            if (deltaTime > 0f && float.IsFinite(deltaTime)) _tailTime += deltaTime * _tailSpeed;
            if (_tailTime >= _tailEnd)
            {
                SetTime(_tailEnd);
                FinishTail();
                return;
            }
            SetTime(_tailTime);
        }

        /// <summary>넘겨받은 일이 있으면 끊고 바닥으로 돌아갑니다(취소·풀 반환·래그돌, 설계 P8). 넘겨받지 않았으면 아무 일도 하지 않습니다.</summary>
        internal void CutTail()
        {
            if (_handed) End();
        }

        /// <summary>
        /// 위층을 모두 빼서 아래 클립째 파괴하고 바닥으로 바로 돌아갑니다. Animator 아래 Transform은 준비 때 값으로 되돌립니다(계획 P17). 꼬리·섞기·조준도 끝납니다.
        /// 컨트롤러가 없으면 그래프를 멈추고 Animator를 끕니다. 위층이 없으면 상태만 비웁니다.
        /// </summary>
        internal void End()
        {
            _pitchRig?.ResetAim();
            _handed = false;
            _tailing = false;
            _fadeTarget = NoFade;
            if (_current == BaseInput && !_overlays[FirstOverlay].IsValid() && !_overlays[SecondOverlay].IsValid()) return;
            try
            {
                try { RemoveOverlay(FirstOverlay); }
                finally { RemoveOverlay(SecondOverlay); }
            }
            finally
            {
                _current = BaseInput;
                RestorePose();
                SetOnly(BaseInput);
                if (_controller == null)
                {
                    if (_graph.IsValid()) _graph.Stop();
                    if (_animator != null) _animator.enabled = false;
                }
            }
        }

        /// <summary>프리팹이 조준 리그를 제공할 때 조준점을 설정합니다.</summary>
        internal void AimWeapon(CombatWeaponAim aim)
        {
            if (_pitchRig == null)
                return;
            if (!_eventPoints.TryGet(aim.MuzzlePoint, out Transform muzzle))
                throw new InvalidOperationException($"[UnitClipChannel] 상하 조준에 발사 지점 {aim.MuzzlePoint}이 필요합니다: {_animator.name}");
            _pitchRig.Aim(aim, muzzle);
        }

        /// <summary>번호별 이벤트 지점의 지금 월드 자세입니다(클립 칸 다시 짜기 설계 CS4). 목록 밖이거나 칸이 비었으면 false입니다.</summary>
        internal bool TryGetEventPoint(int point, out Vector3 position, out Quaternion rotation)
        {
            if (!_eventPoints.TryGet(point, out Transform transform))
            {
                position = default;
                rotation = Quaternion.identity;
                return false;
            }
            transform.GetPositionAndRotation(out position, out rotation);
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
            if (_graph.IsValid() && (_controller != null || _current != BaseInput)) _graph.Play();
        }

        /// <summary>위층을 끝내고 그래프를 파괴한 뒤, 넘겨받으며 바꾼 Animator 값 다섯을 돌려줍니다(검토 V8). 여러 번 불러도 안전합니다.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { End(); }
            finally
            {
                try
                {
                    if (_graph.IsValid())
                    {
                        _graph.Stop();
                        _graph.Destroy();
                    }
                }
                finally
                {
                    _pitchRig?.Clear();
                    _graph = default;
                    _mixer = default;
                    _overlays[FirstOverlay] = default;
                    _overlays[SecondOverlay] = default;
                    RestoreAnimator();
                }
            }
        }

        // 섞기를 목표 입력으로 시작합니다. 꺼져 있으면 바로 목표만 1로 두고 나머지 위층을 뺍니다(지금과 같음).
        private void StartFade(int target, CombatClipBlend blend)
        {
            if (!blend.Enabled)
            {
                for (int i = 0; i < InputCount; i++)
                    _weights[i] = i == target ? 1f : 0f;
                ApplyWeights();
                FinishFadeAt(target);
                return;
            }
            Array.Copy(_weights, _fadeFrom, InputCount);
            _fadeTarget = target;
            _fadeElapsed = 0f;
            _fadeSeconds = blend.Seconds;
            ApplyWeights();
        }

        // Out 섞기(위층 → 바닥)를 시작합니다. 컨트롤러가 없으면 섞을 Idle이 없어 바로 돌아갑니다.
        private void StartFadeOut()
        {
            if (_controller == null || !_fadeOut.Enabled)
            {
                End();
                return;
            }
            _pitchRig?.ResetAim();
            StartFade(BaseInput, _fadeOut);
        }

        // 뒤 단계가 끝 시각에 닿았습니다. Out이 켜져 있으면 끝 프레임에 멈춘 채 섞고, 꺼져 있으면 바로 돌아갑니다.
        private void FinishTail()
        {
            _tailing = false;
            StartFadeOut();
        }

        private void FinishFade()
        {
            int target = _fadeTarget;
            _fadeTarget = NoFade;
            FinishFadeAt(target);
        }

        // 섞기가 끝났습니다. 바닥이 목표면 바닥으로 돌아가고(되돌리기 한 번, 설계 CS7), 위층이 목표면 나간 위층을 빼고 되돌립니다.
        private void FinishFadeAt(int target)
        {
            if (target == BaseInput)
            {
                End();
                return;
            }
            int other = target == FirstOverlay ? SecondOverlay : FirstOverlay;
            if (!_overlays[other].IsValid()) return;
            _weights[other] = 0f;
            ApplyWeights();
            RemoveOverlay(other);
            RestorePose();
        }

        // 입력 하나에 새 위층(몸 · 탑승물 · 부속)을 만들어 꽂습니다. 가중치는 0으로 둡니다(섞기가 정함). 실패하면 만든 위층을 지웁니다(계획 P16).
        private void Connect(int input, CombatClipData clip)
        {
            IReadOnlyList<AnimationClip> companions = clip.Companions;
            // 층 순서는 몸 0 · 탑승물 1 · 부속 2..입니다(클립 칸 다시 짜기 설계 P6). 탑승물이 없으면 부속이 1부터입니다.
            int first = clip.Mount != null ? 2 : 1;
            AnimationLayerMixerPlayable overlay = default;
            try
            {
                overlay = AnimationLayerMixerPlayable.Create(_graph, companions.Count + first);
                ConnectLayer(overlay, 0, clip.Clip);
                if (clip.Mount != null)
                    ConnectLayer(overlay, 1, clip.Mount);
                for (int i = 0; i < companions.Count; i++)
                    ConnectLayer(overlay, i + first, companions[i]);
                _graph.Connect(overlay, 0, _mixer, input);
            }
            catch
            {
                if (overlay.IsValid()) _graph.DestroySubgraph(overlay);
                throw;
            }
            _overlays[input] = overlay;
            _weights[input] = 0f;
            ApplyWeights();
        }

        // 입력 하나의 위층을 빼서 아래 클립째 파괴합니다. 되돌리기·가중치는 부르는 쪽이 정합니다.
        private void RemoveOverlay(int input)
        {
            AnimationLayerMixerPlayable overlay = _overlays[input];
            _overlays[input] = default;
            if (!overlay.IsValid()) return;
            _graph.Disconnect(_mixer, input);
            _graph.DestroySubgraph(overlay);
        }

        private void SetOnly(int input)
        {
            for (int i = 0; i < InputCount; i++)
                _weights[i] = i == input ? 1f : 0f;
            ApplyWeights();
        }

        private void ApplyWeights()
        {
            if (!_mixer.IsValid()) return;
            for (int i = 0; i < InputCount; i++)
                _mixer.SetInputWeight(i, _weights[i]);
        }

        /// <summary>층 하나에 클립을 속도 0·시각 0·IK 끔으로 꽂고 가중치 1(덮어쓰기)로 둡니다.</summary>
        private void ConnectLayer(AnimationLayerMixerPlayable overlay, int layer, AnimationClip source)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, source);
            playable.SetSpeed(0);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetTime(0);
            _graph.Connect(playable, 0, overlay, layer);
            overlay.SetInputWeight(layer, 1f);
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

        private void RestoreAnimator()
        {
            if (_animator == null) return;
            _animator.runtimeAnimatorController = _controller;
            _animator.applyRootMotion = _applyRootMotion;
            _animator.fireEvents = _fireEvents;
            _animator.cullingMode = _cullingMode;
            _animator.enabled = _animatorWasEnabled;
        }
    }
}
