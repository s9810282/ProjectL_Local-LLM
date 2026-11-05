using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 지금의 <see cref="EventSystem"/> 선택을 비우고 되돌립니다. 되돌릴 때는 선택이 아직 비어 있을 때만,
    /// 기억한 대상이 살아 있고 켜져 있으면 다시 선택합니다(옵션창 <c>OptionUI</c>와 같은 조건).
    /// </summary>
    internal sealed class TutorialViewEventSystemSelection : ITutorialViewSelection
    {
        private GameObject _saved;

        public void SaveAndClear()
        {
            EventSystem events = EventSystem.current;
            _saved = events != null ? events.currentSelectedGameObject : null;
            if (events != null && _saved != null)
                events.SetSelectedGameObject(null);
        }

        public void Restore()
        {
            GameObject saved = _saved;
            _saved = null;
            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject != null)
                return;
            if (saved != null && saved.activeInHierarchy)
                events.SetSelectedGameObject(saved);
        }
    }
}
