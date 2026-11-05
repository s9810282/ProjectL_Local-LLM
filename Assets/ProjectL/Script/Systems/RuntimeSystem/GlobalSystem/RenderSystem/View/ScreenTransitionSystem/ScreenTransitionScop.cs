using System;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>
    /// Pool에서 대여한 화면 전환 요청과 진행 상태입니다.
    /// 완료 후에도 대여를 유지하며 Dispose할 때 객체 자체를 반환합니다.
    /// 반환한 참조는 다시 사용하지 않습니다.
    /// </summary>
    public interface IScreenTransitionScop : IDisposable
    {
        ScreenTransitionData Data { get; }
        float CurrentChangeAmount { get; }
        bool IsCompleted { get; }
        void Play(ScreenTransitionData data);
    }

    internal interface IScreenTransitionScopPoolService
    {
        void Return(ScreenTransitionScop scop);
    }

    internal sealed class ScreenTransitionScop : IScreenTransitionScop, ISystemTick
    {
        private enum PlayState
        {
            Ready,
            Playing,
            Finishing,
            Completed
        }

        private IScreenTransitionScopService _scopService;
        private readonly IScreenTransitionOutput _output;
        private IScreenTransitionScopPoolService _poolService;
        private ScreenTransitionData _data;
        private float _currentChangeAmount;
        private PlayState _state;

        internal ScreenTransitionScop(IScreenTransitionOutput output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public ScreenTransitionData Data
        {
            get
            {
                ThrowIfReturned();
                return _data;
            }
        }

        public float CurrentChangeAmount
        {
            get
            {
                ThrowIfReturned();
                return _currentChangeAmount;
            }
        }

        public bool IsCompleted
        {
            get
            {
                ThrowIfReturned();
                return _state == PlayState.Completed;
            }
        }

        internal void Bind(
            IScreenTransitionScopService scopService,
            IScreenTransitionScopPoolService poolService)
        {
            if (scopService == null)
                throw new ArgumentNullException(nameof(scopService));
            if (poolService == null)
                throw new ArgumentNullException(nameof(poolService));
            if (_poolService != null)
                throw new InvalidOperationException("이미 대여 중인 전환 Scop입니다.");

            _scopService = scopService;
            _poolService = poolService;
        }

        public void Play(ScreenTransitionData data)
        {
            ThrowIfReturned();
            Validate(data);

            // 재요청도 이전 실행을 먼저 중지하여 순회 중 새 요청을 즉시 Tick하지 않습니다.
            _scopService.Deactivate(this);
            _state = PlayState.Ready;
            _currentChangeAmount = 0f;
            _data = default;
            try
            {
                _output.Dispose();
                _output.Begin(data);
                _data = data;
                _state = PlayState.Playing;
                _scopService.Activate(this);
            }
            catch
            {
                _output.Dispose();
                throw;
            }
        }

        void ISystemTick.Tick(float deltaTime)
        {
            ThrowIfReturned();
            if (_state != PlayState.Playing && _state != PlayState.Finishing)
                return;

            try
            {
                if (_state == PlayState.Playing)
                {
                    _currentChangeAmount = Math.Min(
                        _data.TargetChangeAmount,
                        _currentChangeAmount + _data.ChangeSpeed * deltaTime);
                    _output.Apply(_currentChangeAmount);
                    if (_currentChangeAmount >= _data.TargetChangeAmount)
                        _state = PlayState.Finishing;
                }

                if (_state == PlayState.Finishing && _output.TryComplete())
                {
                    _state = PlayState.Completed;
                    _scopService.Deactivate(this);
                }
            }
            catch
            {
                _state = PlayState.Ready;
                _scopService.Deactivate(this);
                _output.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_poolService == null)
                return;

            _scopService.Deactivate(this);
            try
            {
                _output.Dispose();
            }
            finally
            {
                _poolService.Return(this);
            }
        }

        internal void Clear()
        {
            _scopService = null;
            _poolService = null;
            _data = default;
            _currentChangeAmount = 0f;
            _state = PlayState.Ready;
        }

        private static void Validate(ScreenTransitionData data)
        {
            if (data.TargetVirtureCamera != null && string.IsNullOrWhiteSpace(data.TargetVirtureCamera))
                throw new ArgumentException("목표 버추얼 카메라 이름이 필요합니다.", nameof(data));
            if (!IsFinite(data.ChangeSpeed) || data.ChangeSpeed <= 0f)
                throw new ArgumentException("ChangeSpeed는 유한한 양수여야 합니다.", nameof(data));
            if (!IsFinite(data.TargetChangeAmount) || data.TargetChangeAmount <= 0f)
                throw new ArgumentException("TargetChangeAmount는 유한한 양수여야 합니다.", nameof(data));

            var color = data.StencileMaskeColor;
            if (!IsFinite(color.r) || !IsFinite(color.g) || !IsFinite(color.b) || !IsFinite(color.a))
                throw new ArgumentException("스텐실 경계선 색은 유한한 값이어야 합니다.", nameof(data));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void ThrowIfReturned()
        {
            if (_poolService == null)
                throw new ObjectDisposedException(nameof(ScreenTransitionScop));
        }
    }
}
