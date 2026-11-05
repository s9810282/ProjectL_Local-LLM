using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.ServiceKeys.Runtime;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>GlobalUI의 메인 표시 아래에서 로비 명성 잔액을 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class ReputationUI : UIBase
    {
        [SerializeField] private TMP_Text amountText;

        private IFameBufferReadSink _fame;
        private bool _hasAmount;
        private int _lastAmount;

        public ReputationUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.Reputation;
        public override UIId ParentID => UIId.MainCharactor;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            _fame = resolver.GetService(BufferSystemKey.Buffer).FameReadSink
                ?? throw new InvalidOperationException($"[ReputationUI] '{name}' ({ID})의 Fame Buffer가 없습니다.");
            _hasAmount = false;
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        public override void Open()
        {
            ValidateReferences();
            if (_fame == null)
                throw new InvalidOperationException($"[ReputationUI] '{name}' ({ID})의 Buffer 바인딩 전에 열 수 없습니다.");
            RefreshAmount();
            gameObject.SetActive(true);
        }

        public override void Hide() => gameObject.SetActive(false);

        private void LateUpdate() => RefreshAmount();

        private void RefreshAmount()
        {
            if (_fame == null) return; // 최초 바인딩 전에는 아직 표시할 잔액이 없습니다.
            int amount = _fame.Amount;
            if (_hasAmount && _lastAmount == amount) return;
            amountText.text = amount.ToString();
            _lastAmount = amount;
            _hasAmount = true;
        }

        private void ValidateReferences()
        {
            if (amountText == null || !amountText.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[ReputationUI] '{name}' ({ID})의 amountText는 자식 TMP Text여야 합니다.");
        }
    }
}
