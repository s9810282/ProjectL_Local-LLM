using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>보유 카드의 다음 레벨 EXP 진행값을 두 상세창에서 같은 방식으로 표시합니다.</summary>
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

        internal void Render(ExperienceProgressSnapshot progress)
        {
            if (progress == null || !progress.IsEnabled)
            {
                RenderPending();
                return;
            }

            fill.fillAmount = (float)progress.Current / progress.Required;
            remaining.text = $"{progress.Current} / {progress.Required}";
        }

        internal void RenderPending()
        {
            fill.fillAmount = 0f;
            remaining.text = "-";
        }
    }
}
