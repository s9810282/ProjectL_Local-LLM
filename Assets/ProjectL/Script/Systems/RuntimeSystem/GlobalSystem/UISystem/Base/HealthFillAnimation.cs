using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// HP 표시 비율만 소유한다. 호출자가 고정 틱의 경과 시간을 전달하면 Fill 다음 SubFill을 줄인다.
    /// 실제 HP, Image 참조, 틱 등록과 UI 수명은 호출자가 소유한다.
    /// </summary>
    internal sealed class HealthFillAnimation
    {
        // float로 전달된 비율·시간의 반올림 때문에 완료가 한 틱 늦어지는 것을 막는다.
        private const double CompletionTolerance = 0.0000001d;

        private bool _initialized;
        private double _target;
        private double _fillAmount;
        private double _subFillAmount;
        private double _fillSecondsPerPercent;
        private double _subFillSecondsPerPercent;

        public float FillAmount => (float)_fillAmount;
        public float SubFillAmount => (float)_subFillAmount;
        public bool IsPlaying => _fillAmount > _target || _subFillAmount > _target;

        /// <summary>초기 표시·회복·예상 표시·재바인딩에는 두 바를 즉시 맞춘다.</summary>
        public void Snap(float amount)
        {
            ValidateAmount(amount);
            _initialized = true;
            _target = amount;
            _fillAmount = amount;
            _subFillAmount = amount;
        }

        /// <summary>
        /// 감소 목표와 각 바의 HP 1%당 시간을 받는다. 같은 목표는 진행을 다시 시작하지 않는다.
        /// 연속 감소는 현재 표시에서 이어가고, 회복은 두 바에 즉시 반영한다.
        /// </summary>
        public void SetTarget(float amount, float fillSecondsPerPercent, float subFillSecondsPerPercent)
        {
            ValidateAmount(amount);
            ValidateTime(fillSecondsPerPercent, nameof(fillSecondsPerPercent));
            ValidateTime(subFillSecondsPerPercent, nameof(subFillSecondsPerPercent));
            _fillSecondsPerPercent = fillSecondsPerPercent;
            _subFillSecondsPerPercent = subFillSecondsPerPercent;

            if (!_initialized || amount > _target)
            {
                Snap(amount);
                return;
            }

            _target = amount;
            // 0초 설정은 다음 틱을 기다리지 않는다. SubFill은 Fill 완료 이후에만 반영한다.
            if (_fillSecondsPerPercent == 0d) _fillAmount = _target;
            if (_fillAmount == _target && _subFillSecondsPerPercent == 0d) _subFillAmount = _target;
        }

        /// <summary>
        /// 고정 틱 한 번을 진행한다. Fill 완료 후 남은 시간은 SubFill에 넘겨 틱 간격이 바뀌어도 속도를 유지한다.
        /// 반환값이 true이면 표시값을 Image에 반영한다. 완료 뒤에는 추가 진행하지 않는다.
        /// </summary>
        public bool FixedTick(float deltaTime)
        {
            ValidateTime(deltaTime, nameof(deltaTime));
            if (!IsPlaying || deltaTime == 0f) return false;

            double previousFill = _fillAmount;
            double previousSubFill = _subFillAmount;
            double remainingTime = deltaTime;
            Advance(ref _fillAmount, _fillSecondsPerPercent, ref remainingTime);
            if (_fillAmount == _target)
                Advance(ref _subFillAmount, _subFillSecondsPerPercent, ref remainingTime);
            return previousFill != _fillAmount || previousSubFill != _subFillAmount;
        }

        /// <summary>UI 해제 시 진행을 끝내고 마지막 목표에 두 바를 맞춘다.</summary>
        public void Complete()
        {
            if (_initialized) Snap((float)_target);
        }

        private void Advance(ref double amount, double secondsPerPercent, ref double remainingTime)
        {
            if (amount <= _target) return;
            if (secondsPerPercent == 0d)
            {
                amount = _target;
                return;
            }

            double difference = amount - _target;
            double secondsPerBar = secondsPerPercent * 100d;
            double step = remainingTime / secondsPerBar;
            if (difference <= step + CompletionTolerance)
            {
                remainingTime = Math.Max(0d, remainingTime - difference * secondsPerBar);
                amount = _target;
            }
            else
            {
                amount -= step;
                remainingTime = 0d;
            }
        }

        private static void ValidateAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f || amount > 1f)
                throw new ArgumentOutOfRangeException(nameof(amount), "HP 표시 비율은 0~1이어야 합니다.");
        }

        private static void ValidateTime(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "시간은 유한한 0 이상의 값이어야 합니다.");
        }
    }
}
