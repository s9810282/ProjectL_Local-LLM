using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// 등록 화면 내부의 반복 카드·슬롯이다. 독립 화면 ID·이력을 만들지 않고 소유 UI의 수명을 따른다.
    /// 데이터에 따른 개별 표시도 Open/Hide를 사용한다. 동적 위젯은 생성한 소유 UI가 BindOwner한다.
    /// </summary>
    public abstract class UIWidgetBase : UIBase
    {
        protected UIWidgetBase() => Policy = UIOpenPolicy.Parallel;
        public sealed override UIId ID => UIId.None;
        internal UIBase OwnerUI { get; private set; }

        internal void BindOwner(UIBase owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (owner == this || owner is UIWidgetBase ||
                (owner.transform != transform && !transform.IsChildOf(owner.transform)))
                throw new InvalidOperationException($"[{GetType().Name}] '{name}'의 소유 UI는 같은 객체 또는 상위 화면이어야 합니다.");
            if (OwnerUI != null && OwnerUI != owner)
                throw new InvalidOperationException($"[{GetType().Name}] '{name}'은 이미 '{OwnerUI.name}'에 소유되어 있습니다.");
            OwnerUI = owner;
        }

        internal void PrepareOwner()
        {
            if (OwnerUI != null) return;
            for (Transform current = transform; current != null; current = current.parent)
            {
                UIBase selected = null;
                foreach (UIBase candidate in current.GetComponents<UIBase>())
                {
                    if (candidate is UIWidgetBase) continue;
                    if (selected != null)
                        throw new InvalidOperationException($"[{GetType().Name}] '{name}'의 소유 UI가 모호합니다: '{current.name}'.");
                    selected = candidate;
                }
                if (selected == null) continue;
                BindOwner(selected);
                return;
            }
            throw new InvalidOperationException($"[{GetType().Name}] '{name}'을 소유하는 등록 화면이 없습니다.");
        }

        public sealed override void ExportUIService(IUIRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            throw new InvalidOperationException($"[{GetType().Name}] '{name}'은 소유 화면 내부 위젯이며 독립 UIId로 등록할 수 없습니다.");
        }
    }
}
