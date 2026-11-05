using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>
    /// 배치 Drag·이동 경로 지정·공격 모드 중 OS 커서를 칸 중심으로 옮기는 규칙의 유일한 소유자입니다(커서 그리드 스냅 설계 §2·C1).
    /// 커서가 가리키는 칸이 직전에 스냅을 판단한 칸과 다를 때만 옮기고, 같은 칸 안에서는 자유롭게 둡니다(G1).
    /// </summary>
    /// <remarks>
    /// 필드 밖·UI 위에서는 옮기지 않고 기억한 칸을 비웁니다(U2). 옮겨 갈 칸 중심이 UI에 가렸거나 화면 밖이거나 다른 칸으로 잡혀도
    /// 옮기지 않습니다(평가 C10-07). UI 가림 판정은 기존
    /// <see cref="ICombatPointerMapping.TryMapPointer"/>를 그대로 씁니다. 모드 진입만으로는 옮기지 않으며(G1),
    /// 비활성 상태의 포인터 이동이 오면 기억한 칸을 비워 다음 모드의 첫 칸에서 다시 스냅합니다.
    /// 옮긴 결과로 들어오는 다음 포인터 이벤트는 같은 칸이므로 다시 옮기지 않습니다.
    /// </remarks>
    internal sealed class CombatCursorSnap
    {
        // 감도 상한(G5). 0.5 이상이면 어떤 점도 칸 안쪽 영역에 들지 않아 스냅이 멈춘다.
        private const float MaxThreshold = .49f;

        private readonly ICombatPointerMapping _mapping;
        private readonly ICombatCursorDevice _device;
        private CellCoord? _snapped;

        internal CombatCursorSnap(ICombatPointerMapping mapping, ICombatCursorDevice device)
        {
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>마지막으로 스냅을 판단한 칸입니다(커서를 옮겼거나, 칸 중심이 가려 옮기지 않기로 한 칸, 평가 C10-07). 없으면 null입니다.</summary>
        internal CellCoord? SnappedCell => _snapped;

        /// <summary>
        /// 포인터가 움직인 뒤 부릅니다. <paramref name="active"/>는 스냅 모드(배치 Drag·경로 지정·공격 모드) 여부입니다.
        /// 커서를 옮겼으면 true입니다.
        /// </summary>
        internal bool PointerMoved(bool active, float screenX, float screenY)
        {
            if (!active || !_device.IsCursorSnapEnabled)
            {
                _snapped = null;
                return false;
            }

            // 필드 밖이거나 UI 위면 스냅하지 않는다(U2).
            if (!_mapping.TryMapPointer(new Vector2(screenX, screenY), out Vector2 grid, out bool overCancelArea)
                || overCancelArea)
            {
                _snapped = null;
                return false;
            }

            CellCoord cell = MovementPointerSampler.CellAt(grid);
            if (_snapped == cell)
                return false;
            // 감도(G5): 새 칸의 경계에서 기준 비율만큼 안쪽으로 들어오기 전에는 옮기지 않는다. 기억한 칸도 그대로 둬
            // 경계에 걸친 동안 커서가 떨리지 않게 한다. 기준이 0이면 경계를 넘자마자 옮긴다.
            float threshold = SanitizeThreshold(_device.CursorSnapThreshold);
            if (threshold > 0f && !MovementPointerSampler.IsInside(grid, threshold))
                return false;
            if (!_device.TryCellCenterToScreen(cell, out Vector2 center))
                return false;
            // 칸 중심이 UI에 가렸거나 다른 칸으로 잡히면(옆 고지대 블록에 가림 등) 옮기지 않는다. 옮기면 커서가 그 칸을 누를 수 없는
            // 자리로 끌려간다(평가 C10-07). 이 칸은 기억해 같은 칸 안에서는 다시 묻지 않고, 칸의 보이는 부분에서 커서를 자유롭게 둔다.
            if (!_mapping.TryMapPointer(center, out Vector2 centerGrid, out bool centerOverUi)
                || centerOverUi
                || MovementPointerSampler.CellAt(centerGrid) != cell)
            {
                _snapped = cell;
                return false;
            }
            if (!_device.TryWarpCursor(center))
                return false;

            _snapped = cell;
            return true;
        }

        // 씬 값은 Range로 막히지만 계약상 아무 값이나 올 수 있다. 0 미만·NaN은 0, 0.5 이상은 칸 안에 들 수 없으므로 0.49로 자른다.
        private static float SanitizeThreshold(float threshold)
        {
            if (float.IsNaN(threshold) || threshold <= 0f)
                return 0f;
            return threshold < MaxThreshold ? threshold : MaxThreshold;
        }

        /// <summary>기억한 칸을 비웁니다. 다음 스냅 모드의 첫 칸에서 다시 옮깁니다.</summary>
        internal void Reset()
        {
            _snapped = null;
        }
    }
}
