using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// Combat 씬의 참조 묶음입니다(설계 §4·§11, W2). 동작은 없고, 씬이 로드되면 CombatViewSystem이 한 번 찾아 연결합니다.
    /// </summary>
    /// <remarks>
    /// Core 호출·입력 해석·런타임 찾기를 하지 않습니다. 참조 누락은 에디터(OnValidate)와 연결 시점에 확인합니다.
    /// 개발 Layer 패널(W20)은 Canvas의 버튼 두 개와 "입력 기록 모드" 표시를 묶은 오브젝트이며 기본 비활성으로 둡니다.
    /// 켜고 끄기와 버튼 연결은 CombatViewSystem이 RecordInputOnly일 때만 합니다(설계 §8.1).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CombatSceneRoot : MonoBehaviour
    {
        public event Action FocusLost;
        public event Action Disabled;
        private void OnApplicationFocus(bool focused) { if (!focused) FocusLost?.Invoke(); }
        private void OnDisable() => Disabled?.Invoke();
        [SerializeField, Range(0, .49f)] private float innerCellInset = .15f;
        [SerializeField, Min(.01f)] private float movementStepSeconds = .12f;
        internal float InnerCellInset => innerCellInset;
        internal float MovementStepSeconds => movementStepSeconds;
        [SerializeField] private FieldView field;
        [SerializeField] private FieldUnitPresenter fieldUnits;
        [SerializeField] private HandPresenter hand;
        [SerializeField] private CombatHud hud;
        [SerializeField] private PlacementGhost ghost;
        [Tooltip("포기 버튼과 확인창입니다(포기 UI 설계, Live 전용).")]
        [SerializeField] private CombatAbandonPanel abandonPanel;

        [Tooltip("화면 좌표를 칸·손패로 바꿀 카메라입니다. 비워 두면 MainCamera 태그의 카메라를 씁니다.")]
        [SerializeField] private Camera worldCamera;

        [Header("개발 Layer 패널 (RecordInputOnly 전용, 기본 비활성)")]
        [SerializeField] private GameObject devLayerPanel;
        [SerializeField] private Button devDeploymentButton;
        [SerializeField] private Button devCombatButton;

        internal FieldView Field => field;

        internal FieldUnitPresenter FieldUnits => fieldUnits;

        internal HandPresenter Hand => hand;

        internal CombatHud Hud => hud;

        internal PlacementGhost Ghost => ghost;

        internal CombatAbandonPanel AbandonPanel => abandonPanel;

        /// <summary>연결된 카메라입니다. 비어 있으면 null이며, 쓰는 쪽이 MainCamera로 대신합니다.</summary>
        internal Camera WorldCamera => worldCamera;

        internal GameObject DevLayerPanel => devLayerPanel;

        internal Button DevDeploymentButton => devDeploymentButton;

        internal Button DevCombatButton => devCombatButton;

        /// <summary>빠진 참조가 있으면 이름을 담아 예외를 던집니다.</summary>
        internal void ValidateReferences()
        {
            string missing = FindMissingReference();
            if (missing != null)
                throw new InvalidOperationException($"[CombatSceneRoot] 씬 참조가 비어 있습니다: {missing}");
        }

        private string FindMissingReference()
        {
            if (field == null)
                return nameof(field);
            if (fieldUnits == null)
                return nameof(fieldUnits);
            if (hand == null)
                return nameof(hand);
            if (hud == null)
                return nameof(hud);
            if (ghost == null)
                return nameof(ghost);
            if (abandonPanel == null)
                return nameof(abandonPanel);
            if (devLayerPanel == null)
                return nameof(devLayerPanel);
            if (devDeploymentButton == null)
                return nameof(devDeploymentButton);
            if (devCombatButton == null)
                return nameof(devCombatButton);

            return field.FindMissingReference()
                ?? fieldUnits.FindMissingReference()
                ?? hand.FindMissingReference()
                ?? hud.FindMissingReference()
                ?? ghost.FindMissingReference()
                ?? abandonPanel.FindMissingReference();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            string missing = FindMissingReference();
            if (missing != null)
                Debug.LogWarning($"[CombatSceneRoot] 씬 참조가 비어 있습니다: {missing}", this);
        }
#endif
    }
}
