using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 설명 중 보일 대상 밖을 덮는 그래픽입니다(튜토리얼 설계 §15.4). 자기 영역에서 구멍을 뺀 칸만 그립니다.
    /// 색(투명도 5%, 알파 0.95)은 정의 데이터가 정하고 표시 코드가 넣습니다. 클릭은 받지 않습니다(막기는 따로 둔 투명 막이 맡음).
    /// </summary>
    /// <remarks>
    /// 구멍은 화면 좌표로 받아 두고 메시를 만들 때 로컬 좌표로 바꿉니다. 그래서 Canvas 배율이 바뀌어 다시 그릴 때도 같은 화면 자리를 비웁니다.
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class TutorialViewOverlay : MaskableGraphic
    {
        private readonly List<Rect> _screenHoles = new();
        private readonly List<Rect> _localHoles = new();
        private readonly List<Rect> _cells = new();
        private float _padding;

        /// <summary>덮개는 클릭을 받지 않습니다. 프리팹 값과 상관없이 늘 false입니다.</summary>
        public override bool raycastTarget
        {
            get => false;
            set { }
        }

        /// <summary>받아 둔 구멍 수입니다(시험·확인용).</summary>
        internal int HoleCount => _screenHoles.Count;

        /// <summary>구멍을 화면 좌표(픽셀)로 넣습니다. 비우면 영역 전체를 덮습니다. 구멍마다 둘레에 여백(Canvas 단위)을 더 비웁니다.</summary>
        internal void SetScreenHoles(IReadOnlyList<Rect> screenHoles, float padding)
        {
            if (screenHoles == null)
                throw new ArgumentNullException(nameof(screenHoles));

            _screenHoles.Clear();
            for (int i = 0; i < screenHoles.Count; i++)
                _screenHoles.Add(screenHoles[i]);
            _padding = Mathf.Max(0f, padding);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _localHoles.Clear();
            for (int i = 0; i < _screenHoles.Count; i++)
                _localHoles.Add(TutorialFocusGeometry.ToLocal(rectTransform, _screenHoles[i], _padding));

            TutorialViewOverlayCells.Build(rectTransform.rect, _localHoles, _cells);
            Color32 tint = color;
            for (int i = 0; i < _cells.Count; i++)
            {
                Rect cell = _cells[i];
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(cell.xMin, cell.yMin), tint, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMin, cell.yMax), tint, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMax, cell.yMax), tint, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMax, cell.yMin), tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
