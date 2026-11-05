using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 그리기 손잡이 하나(칸 테두리·칸 채우기)에 마지막으로 그린 칸 목록과 색입니다. 같은 값이면 다시 그리지 않으려고 씁니다.
    /// 빈 목록은 색과 상관없이 같은 모습으로 봅니다. 그린 뒤에만 <see cref="Remember"/>를 불러, 실패한 그리기는 다음에 다시 시도됩니다.
    /// </summary>
    internal sealed class FieldDrawnCells
    {
        private readonly List<Vector2Int> _cells = new();
        private Color _color;
        private bool _hasDrawn;

        internal bool SameAs(IReadOnlyList<Vector2Int> cells, Color color)
        {
            if (!_hasDrawn || _cells.Count != cells.Count)
                return false;
            if (cells.Count > 0 && _color != color)
                return false;
            for (int i = 0; i < cells.Count; i++)
            {
                if (_cells[i] != cells[i])
                    return false;
            }

            return true;
        }

        internal void Remember(IReadOnlyList<Vector2Int> cells, Color color)
        {
            _cells.Clear();
            for (int i = 0; i < cells.Count; i++)
                _cells.Add(cells[i]);
            _color = color;
            _hasDrawn = true;
        }

        /// <summary>기억을 지워 다음 그리기가 늘 다시 쓰게 합니다(높이 표가 바뀔 때·손잡이를 돌려줄 때).</summary>
        internal void Forget()
        {
            _cells.Clear();
            _hasDrawn = false;
        }
    }
}
