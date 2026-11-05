using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>종료 화면 내부의 초상화·획득 경험치·게이지 한 칸입니다. 수명은 종료 화면이 소유합니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class CombatRewardSlot : UIWidgetBase
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Image experience;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private GameObject levelUp;
        private CombatRewardExperienceAnimation _animation;

        internal void ValidateReferences()
        {
            Require(portrait, nameof(portrait));
            Require(experience, nameof(experience));
            Require(experienceText, nameof(experienceText));
            Require(levelUp, nameof(levelUp));
            if (experience.type != Image.Type.Filled || experience.fillMethod != Image.FillMethod.Horizontal)
                throw new InvalidOperationException($"[CombatRewardSlot] UIId.CombatEnd/{name}.experience는 Horizontal Filled Image여야 합니다.");
        }

        private void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
                throw new InvalidOperationException($"[CombatRewardSlot] UIId.CombatEnd/{name}.{field} 참조가 없습니다.");
            Transform target = reference is GameObject go ? go.transform : ((Component)reference).transform;
            if (target != transform && !target.IsChildOf(transform))
                throw new InvalidOperationException($"[CombatRewardSlot] UIId.CombatEnd/{name}.{field}는 슬롯 내부에 있어야 합니다.");
        }

        internal void Show(CharacterRewardReceipt row, Sprite icon, float seconds)
        {
            ValidateReferences();
            var animation = new CombatRewardExperienceAnimation(row, seconds);
            // View 외형의 InfoIcon은 선택 자원입니다. 없으면 빈 초상화로 표시합니다(기존 외형 계약).
            portrait.sprite = icon;
            portrait.enabled = icon != null;
            experienceText.SetText("+{0}", row.TotalExperience);
            _animation = animation;
            Apply();
            Open();
        }

        internal void Tick(float deltaSeconds)
        {
            if (_animation == null || _animation.IsComplete) return;
            _animation.Tick(deltaSeconds);
            Apply();
        }

        private void Apply()
        {
            experience.enabled = _animation.Enabled;
            experience.fillAmount = _animation.Fill;
            levelUp.SetActive(_animation.LeveledUp);
        }

        internal void Clear() => Hide();

        public override void Hide()
        {
            _animation = null;
            portrait.sprite = null;
            portrait.enabled = false;
            experience.fillAmount = 0;
            experienceText.SetText(string.Empty);
            levelUp.SetActive(false);
            base.Hide();
        }
    }
}
