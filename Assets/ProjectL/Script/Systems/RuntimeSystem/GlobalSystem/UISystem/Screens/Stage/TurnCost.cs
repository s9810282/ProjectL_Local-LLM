using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 외부에서 전달한 Cost 수량과 사용 예상을 표시한다. 비용 계산과 전투 데이터 조회는 하지 않는다.
    /// initialInstances 순서로 채우고 역순으로 소모한다. 슬롯 구성 변경 뒤에는 ResetCost를 호출한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurnCost : UIGroup
    {
        public TurnCost() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.TurnCost;

        // 소유권 원본은 UIGroup이다. 이 목록은 표시 순서와 할당 없는 연출 순회를 위한 캐시다.
        private readonly List<TurnCostSlot> _slots = new();
        private bool _hasValue;
        private int _current;
        private int _maximum;
        private int _previewSpend;

        public int Current => _current;
        public int Maximum => _maximum;
        public int PreviewSpend => _previewSpend;

        /// <summary>새 턴·대상 재바인딩. 기존 예상과 연출을 지우고 수량을 즉시 반영한다.</summary>
        public void ResetCost(int current, int maximum)
        {
            ValidateValue(current, maximum);
            CacheSlots();
            ValidateCapacity(maximum);
            _hasValue = true;
            _current = current;
            _maximum = maximum;
            _previewSpend = 0;
            ApplyValues(snap: true);
        }

        /// <summary>확정 수량. 감소한 Fill만 소모 연출을 재생한다. 같은 값은 연출과 예상을 유지한다.</summary>
        public void SetCost(int current, int maximum)
        {
            ValidateValue(current, maximum);
            if (!_hasValue)
            {
                ResetCost(current, maximum);
                return;
            }
            ValidateSlots();
            ValidateCapacity(maximum);
            if (_current == current && _maximum == maximum) return;
            bool snap = _maximum != maximum || !isActiveAndEnabled;
            _current = current;
            _maximum = maximum;
            _previewSpend = 0;
            ApplyValues(snap);
        }

        /// <summary>현재 수량 중 사용 예정인 개수. 확정 수량은 변경하지 않는다.</summary>
        public void SetPreview(int spend)
        {
            if (!_hasValue)
                throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': SetCost/ResetCost 뒤 사용 예상을 설정해야 합니다.");
            if (spend < 0 || spend > _current) throw new ArgumentOutOfRangeException(nameof(spend));
            ValidateSlots();
            // 숨긴 HUD에 늦게 도착한 예상 표시는 재표시 때 이어가지 않는다.
            if (!isActiveAndEnabled) { ClearPreview(); return; }
            _previewSpend = spend;
            ApplyPreview();
        }

        /// <summary>취소·호버 종료. 바인딩 전 또는 반복 취소도 정상 경로다.</summary>
        public void ClearPreview()
        {
            if (!_hasValue) return;
            ValidateSlots();
            _previewSpend = 0;
            ApplyPreview();
        }

        public override void Open()
        {
            if (_hasValue) ValidateSlots();
            else CacheSlots();
            // 데이터가 오기 전에는 빈 HUD로 열 수 있다. Open은 충전 초기화가 아니다.
            base.Open();
        }

        protected override void OnUIHidden()
        {
            _previewSpend = 0;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null) _slots[i].CompletePresentation();
        }

        private void LateUpdate() => Advance(Time.unscaledDeltaTime);

        internal void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!_hasValue || !isActiveAndEnabled) return;
            ValidateMembership();
            for (int i = 0; i < _slots.Count; i++) _slots[i].Advance(deltaTime);
        }

        private void ApplyValues(bool snap)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                TurnCostSlot slot = _slots[i];
                slot.SetValue(i < _maximum, i < _current, snap);
                if (isActiveAndEnabled) slot.Open();
                else slot.Hide();
            }
        }

        private void ApplyPreview()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetPreview(isActiveAndEnabled && i >= _current - _previewSpend && i < _current);
        }

        private void CacheSlots()
        {
            var slots = new List<TurnCostSlot>();
            foreach (UIBase instance in Instances)
            {
                if (!(instance is TurnCostSlot slot) || slot.transform == transform || !slot.transform.IsChildOf(transform))
                    throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': initialInstances에는 하위 TurnCostSlot만 지정해야 합니다. 대상: {instance}");
                slot.ValidateReferences();
                slots.Add(slot);
            }
            if (slots.Count == 0)
                throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': initialInstances에 Cost 슬롯이 필요합니다.");
            // UIGroup Stack의 열거는 마지막 멤버부터이므로 Inspector 배열 순서로 복원한다.
            slots.Reverse();
            _slots.Clear();
            _slots.AddRange(slots);
        }

        private void ValidateMembership()
        {
            if (_slots.Count != InstanceCount)
                throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': 슬롯 구성 변경 뒤 ResetCost가 필요합니다.");
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] == null || _slots[i].OwnerGroup != this)
                    throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': 슬롯 {i}의 소유권이 변경되었습니다. ResetCost가 필요합니다.");
        }

        private void ValidateSlots()
        {
            ValidateMembership();
            for (int i = 0; i < _slots.Count; i++) _slots[i].ValidateReferences();
        }

        private void ValidateCapacity(int maximum)
        {
            if (maximum > _slots.Count)
                throw new InvalidOperationException($"[TurnCost] UIId.{ID} '{name}': 최대 Cost {maximum}개를 표시할 슬롯이 부족합니다({_slots.Count}개).");
        }

        private static void ValidateValue(int current, int maximum)
        {
            if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (current < 0 || current > maximum) throw new ArgumentOutOfRangeException(nameof(current));
        }
    }
}
