using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>EXP 표시 영역입니다. Core 진행 계약 인수 전에는 대기 표시만 제공합니다.</summary>
    [Serializable]
    internal sealed class DeckSlotInfoExperience
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text remaining;

        internal void Validate(Transform owner)
        {
            DeckSlotInfoReferences.RequireChild(fill, owner, "experience.fill");
            DeckSlotInfoReferences.RequireChild(remaining, owner, "experience.remaining");
            if (fill.type != Image.Type.Filled || fill.fillMethod != Image.FillMethod.Horizontal || fill.fillOrigin != 0)
                throw DeckSlotInfoReferences.Error(owner, "experience.fill은 왼쪽부터 채우는 Horizontal Filled Image여야 합니다.");
        }

        internal void RenderPending()
        {
            fill.fillAmount = 0f;
            remaining.text = "-";
        }
    }
}
