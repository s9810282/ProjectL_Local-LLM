
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// Combat 씬 Canvas의 Run 골드를 표시합니다.
    /// </summary>
    /// <remarks>
    /// 값은 CombatViewSystem이 Buffer에서 읽어 넘깁니다.
    /// 같은 값이면 텍스트를 다시 쓰지 않습니다. 씬의 Parallel UI로 등록하고 개폐는 UISystem이 관리합니다.
    /// 완료 확인은 CombatEndUI가 담당합니다. 이 HUD는 골드 표시와 등록된 UI 수명만 소유합니다.
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class CombatHud : UIBase
    {
        public CombatHud() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.CombatHud;

        // UI 없는 격리 표시 조립은 직접 Open/Hide한다. 운영 경로는 씬 바인딩이 필수다.
        internal void Present()
        {
            if (UISystemService != null) UISystemService.OpenUI(ID, true);
            else Open();
        }
        internal void Dismiss()
        {
            if (UISystemService != null) UISystemService.CloseUI(ID);
            else Hide();
        }

        [Tooltip("이번 Run에서 유지되는 골드입니다.")]
        [SerializeField, FormerlySerializedAs("currencyText"), FormerlySerializedAs("stageGoldText")] private TMP_Text runGoldText;

        private bool _hasShown;
        private int _shownRunGold;

        protected override void OnUIOpened() => InvalidateShownValues();
        protected override void OnUIHidden() => InvalidateShownValues();

        private void InvalidateShownValues()
        {
            _hasShown = false;
        }

        internal void Render(int runGold)
        {
            if (_hasShown && _shownRunGold == runGold)
                return;

            runGoldText.SetText("{0}", runGold);
            _hasShown = true;
            _shownRunGold = runGold;
        }

        internal void Clear()
        {
            InvalidateShownValues();
            runGoldText.SetText(string.Empty);
            Dismiss();
        }

        internal string FindMissingReference()
        {
            if (runGoldText == null)
                return $"{nameof(CombatHud)}.{nameof(runGoldText)}";
            return null;
        }
    }
}
