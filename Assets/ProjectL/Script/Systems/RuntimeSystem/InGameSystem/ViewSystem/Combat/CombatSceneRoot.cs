using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// Combat 씬의 참조 묶음입니다(설계 §4·§11, W2). 동작은 포커스 잃음·꺼짐을 이벤트(<see cref="FocusLost"/>·<see cref="Disabled"/>)로 알리는 것뿐이고,
    /// 씬이 로드되면 CombatViewSystem이 한 번 찾아 연결합니다.
    /// </summary>
    /// <remarks>
    /// Core 호출·입력 해석·런타임 찾기를 하지 않습니다. 참조 누락은 에디터(OnValidate)와 연결 시점에 확인합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CombatSceneRoot : MonoBehaviour
    {
        public event Action FocusLost;
        public event Action Disabled;
        private void OnApplicationFocus(bool focused) { if (!focused) FocusLost?.Invoke(); }
        private void OnDisable() => Disabled?.Invoke();
        [SerializeField, Range(0, .49f)] private float innerCellInset = .15f;
        [Tooltip("배치 Drag·이동 경로 지정·공격 모드 중 OS 커서를 칸 중심으로 옮깁니다(칸이 바뀔 때만). 조작감 비교용으로 끌 수 있습니다.")]
        [SerializeField] private bool snapCursorToGrid = true;
        [Tooltip("스냅 감도입니다. 새 칸의 경계에서 칸 폭의 이 비율만큼 안쪽으로 들어와야 스냅합니다. 0이면 경계를 넘자마자, 0.49면 거의 중심에서 스냅합니다(커서 그리드 스냅 설계 G5).")]
        [SerializeField, Range(0, .49f)] private float cursorSnapThreshold;
        internal float InnerCellInset => innerCellInset;
        internal bool SnapCursorToGrid => snapCursorToGrid;
        internal float CursorSnapThreshold => cursorSnapThreshold;
        // 전투 연출의 전역 시간·크기 값은 View DB 설정 표(CombatPresentationSettings)로 옮겼습니다(연출 조립 설계 §9 I-5).
        [SerializeField] private FieldView field;
        [SerializeField] private FieldUnitPresenter fieldUnits;
        // 옛 손패(HandPresenter) 참조 칸은 걷었습니다. 손패 선택은 유지 덱 카드가 맡습니다(View 단계 설계 W4). 씬에 남은 직렬화 값은 Unity가 무시합니다.
        [SerializeField] private CombatHud hud;
        // 남아 있는 AP 화면을 기존 UI 서비스로 닫기 위한 선택 참조입니다. 전투 진입에는 필요하지 않습니다.
        [SerializeField] private TurnCost turnCost;
        internal TurnCost TurnCost => turnCost;
        [SerializeField] private TurnDisplayUI turnDisplay;
        internal TurnDisplayUI TurnDisplay => turnDisplay;
        [SerializeField] private PlacementGhost ghost;
        [Tooltip("공격·턴 종료·복귀 버튼입니다(행동 버튼 설계). 키 입력과 같은 입구로 들어갑니다.")]
        [SerializeField] private CombatActionPanel actionPanel;
        [SerializeField] private BattleChangerUI battleChanger;
        internal BattleChangerUI BattleChanger => battleChanger;
        [SerializeField] private PackUI pack;
        internal PackUI Pack => pack;
        [SerializeField] private CombatUnitInfo unitInfo;
        internal CombatUnitInfo UnitInfo => unitInfo;
        [SerializeField] private CombatEndUI completion;
        internal CombatEndUI Completion => completion;

        [Tooltip("화면 좌표를 칸으로 바꿀 카메라입니다. 비워 두면 MainCamera 태그의 카메라를 씁니다.")]
        [SerializeField] private Camera worldCamera;

        internal FieldView Field => field;

        internal FieldUnitPresenter FieldUnits => fieldUnits;

        internal CombatHud Hud => hud;

        internal PlacementGhost Ghost => ghost;

        internal CombatActionPanel ActionPanel => actionPanel;

        /// <summary>연결된 카메라입니다. 비어 있으면 null이며, 쓰는 쪽이 MainCamera로 대신합니다.</summary>
        internal Camera WorldCamera => worldCamera;

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
            if (hud == null)
                return nameof(hud);
            if (turnDisplay == null)
                return nameof(turnDisplay);
            turnDisplay.ValidateReferences();
            if (ghost == null)
                return nameof(ghost);
            if (actionPanel == null)
                return nameof(actionPanel);
            if (battleChanger == null)
                return nameof(battleChanger);
            if (pack == null)
                return nameof(pack);
            if (unitInfo == null)
                return nameof(unitInfo);
            if (completion == null)
                return nameof(completion);
            completion.ValidateReferences();

            return field.FindMissingReference()
                ?? fieldUnits.FindMissingReference()
                ?? hud.FindMissingReference()
                ?? ghost.FindMissingReference()
                ?? actionPanel.FindMissingReference();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 씬 로드·도메인 재로드 중에는 다른 컴포넌트의 직렬화 참조가 아직 복원되지 않을 수 있습니다.
            // 중복 예약을 합치고 Inspector 갱신 뒤에 전체 계층을 검사합니다.
            UnityEditor.EditorApplication.delayCall -= ValidateEditorReferences;
            UnityEditor.EditorApplication.delayCall += ValidateEditorReferences;
        }

        private void ValidateEditorReferences()
        {
            if (this == null) return;
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
            {
                OnValidate();
                return;
            }
            string missing = FindMissingReference();
            if (missing != null)
                Debug.LogWarning($"[CombatSceneRoot] 씬 참조가 비어 있습니다: {missing}", this);
        }

        private void OnDestroy()
        {
            UnityEditor.EditorApplication.delayCall -= ValidateEditorReferences;
        }
#endif
    }
}
