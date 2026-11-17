using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 공격 궤적 선 여러 개를 그립니다(궤적 대상별 선 설계 PT5). 선 하나는 <see cref="FieldTrajectoryLine"/> 하나입니다(공격 궤적 설계 TJ1).
    /// </summary>
    /// <remarks>
    /// 구성 때 선 하나만 만들고, 그보다 많은 경로가 오면 모자란 선을 그때 만들어 해제할 때까지 다시 씁니다(설계 P4).
    /// 경로 i번째를 선 i번째에 그리고 남는 선은 숨깁니다. 공격·대상을 모르고 받은 경로만 그립니다.
    /// </remarks>
    internal sealed class FieldTrajectoryLines
    {
        private readonly Transform _parent;
        private readonly StageFieldViewData _config;
        private readonly FieldHighlightPalette _palette;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly List<FieldTrajectoryLine> _lines = new();

        /// <param name="config">궤적 값의 원본입니다. 재질이 있어야 합니다(선마다 <see cref="FieldTrajectoryLine"/>이 검사).</param>
        /// <param name="palette">궤적 색을 그릴 때 읽는 필드 색입니다(필드 색 DB 설계 FC2).</param>
        internal FieldTrajectoryLines(Transform parent, StageFieldViewData config, FieldHighlightPalette palette,
            IMeshSystemService meshes, IColorSystemService colors)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>첫 선을 만듭니다. 실패하면 만든 선은 스스로 거두고 원인을 전파합니다.</summary>
        internal void Build()
        {
            if (_lines.Count > 0)
                throw new InvalidOperationException("[FieldTrajectoryLines] 이미 구성돼 있습니다.");
            AddLine();
        }

        /// <summary>경로마다 선 하나로 그리고 남는 선은 숨깁니다. 막힘이면 모든 선이 막힘 색·표지입니다.</summary>
        internal void Show(IReadOnlyList<FieldTrajectoryPath> paths, bool blocked)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            if (_lines.Count == 0)
                throw new InvalidOperationException("[FieldTrajectoryLines] 구성하기 전에 그릴 수 없습니다.");

            try
            {
                while (_lines.Count < paths.Count)
                    AddLine();
            }
            catch
            {
                // 선을 더 만들지 못하면 지난 경로가 남지 않게 모두 숨기고 원인을 전파합니다.
                Clear();
                throw;
            }
            for (int i = 0; i < paths.Count; i++)
                _lines[i].Show(paths[i].Points, paths[i].Count, blocked);
            for (int i = paths.Count; i < _lines.Count; i++)
                _lines[i].Clear();
        }

        /// <summary>보이는 선에 지금 필드 색을 다시 넣습니다(필드 색 DB 설계 FC3, 에디터 값 바뀜 때).</summary>
        internal void ReapplyColors()
        {
            for (int i = 0; i < _lines.Count; i++)
                _lines[i].ReapplyColor();
        }

        /// <summary>모든 선을 숨깁니다. 구성 전이어도 안전합니다.</summary>
        internal void Clear()
        {
            for (int i = 0; i < _lines.Count; i++)
                _lines[i].Clear();
        }

        /// <summary>선마다 각각 거두고 실패는 모아 한 번 던집니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            List<Exception> errors = null;
            for (int i = 0; i < _lines.Count; i++)
            {
                try
                {
                    _lines[i].Release();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }
            _lines.Clear();

            if (errors != null)
                throw new AggregateException("[FieldTrajectoryLines] 해제 중 실패했습니다.", errors);
        }

        private void AddLine()
        {
            var line = new FieldTrajectoryLine(_parent, _config, _palette, _meshes, _colors);
            line.Build();
            _lines.Add(line);
        }
    }
}
