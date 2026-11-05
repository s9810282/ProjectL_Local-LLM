using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 설명 구간 동안 튜토리얼 Layer를 빌리고 UI 선택을 비웁니다(튜토리얼 설계 §15.4.3). 옵션창과 같은 <c>OverrideLayer</c> 방식입니다.
    /// </summary>
    /// <remarks>
    /// 빌린 동안 들어온 Layer 변경 요청은 InputSystem이 기억했다가 돌려줄 때 적용합니다. 그래서 빌리는 동안 배치 → 전투로 국면이 바뀌어도
    /// 돌려준 뒤에는 그때의 Layer로 돌아갑니다. 같은 구간 안에서는 빌린 채 둡니다.
    /// </remarks>
    internal sealed class TutorialViewInputLease : ITutorialViewInputLease
    {
        private readonly IInputLayerService _layers;
        private readonly IInputLayerOverrideService _overrides;
        private readonly ITutorialViewSelection _selection;
        private IDisposable _scope;

        internal TutorialViewInputLease(IInputLayerService layers, IInputLayerOverrideService overrides,
            ITutorialViewSelection selection)
        {
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        /// <summary>현재 Layer가 설정 Layer인지입니다. InputSystem에 독점 여부를 묻는 공개 API가 없어 이렇게 판정합니다(설계 §10 한계).</summary>
        public bool IsMonopolized => _layers.CurrentLayer == GlobalInputNames.Settings;

        public bool IsBorrowed => _scope != null;

        /// <summary>
        /// 빌리기 직전에 설정 Layer인지 다시 확인하고(구현 계획 §7), 그렇다면 같은 프레임 경합으로 보고 false입니다(설계 §8.3).
        /// 설정창이 열려 있을 때 기다리는 것은 이 함수가 아니라 시작 조건(<c>TutorialPlan.CanStartExplanation</c>)이 정합니다.
        /// 독점 여부를 알 수 없는 다른 실패(설정이 아닌 독점 등)는 예외를 그대로 던집니다(설계 §8.3·§10 한계).
        /// </summary>
        public bool TryBorrow()
        {
            if (IsBorrowed)
                throw new InvalidOperationException("[TutorialViewInputLease] 이미 빌린 입력을 다시 빌릴 수 없습니다.");
            if (IsMonopolized)
                return false;

            IDisposable scope = _overrides.OverrideLayer(TutorialInputLayers.Tutorial);
            try
            {
                _selection.SaveAndClear();
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            _scope = scope;
            return true;
        }

        /// <summary>UI 선택을 되돌리고 Layer를 돌려줍니다. 하나가 실패해도 나머지를 하고 실패를 모아 던집니다.</summary>
        public void Return()
        {
            IDisposable scope = _scope;
            if (scope == null)
                return;

            _scope = null;
            List<Exception> errors = null;
            CleanupErrors.Try(_selection.Restore, ref errors);
            CleanupErrors.Try(scope.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[TutorialViewInputLease] 튜토리얼 입력을 돌려주지 못했습니다.");
        }
    }
}
