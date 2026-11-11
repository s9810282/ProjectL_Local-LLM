using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 이동 경로 하나를 띠·직각 꺾임·끝 화살촉으로 그립니다(이동 경로 화살표 설계 MA1). 하위 Render 부품을 조합합니다:
    /// MeshSystem(띠 <see cref="MultiLineCreateData"/>, 화살촉 꼭짓점 셋 <see cref="CircleCreateData"/>)과 ColorSystem(색 덮어쓰기).
    /// 선례는 <see cref="FieldTrajectoryLine"/>입니다.
    /// </summary>
    /// <remarks>
    /// 띠 이음은 <see cref="FieldMovementPathShape"/>, 화살촉은 <see cref="FieldArrowhead"/>가 맡습니다. 이 부품은 받은 구간 점만 그립니다(경로·격자·상태를 모름).
    /// 띠는 높이 구간마다 하나이며, 구간 번호마다 처음 필요할 때 만들어 <see cref="Release"/>까지 다시 씁니다(계획 TP-4).
    /// 화살촉은 하나이고, 메시는 <see cref="Build"/>에서 한 번 넣은 뒤 오브젝트 자세만 바꿉니다(설계 P3).
    /// 띠·화살촉은 같은 머티리얼·같은 색·같은 높이라 겹친 곳이 한 덩어리로 보입니다. 그림자는 띠(MeshRenderer 기본값 = 드리우기·받기 켬)와 같게 화살촉도 켭니다(<c>shadows: true</c>). 정렬 순서는 설정하지 않습니다(설계 P5).
    /// 불투명 머티리얼이라 깊이를 쓰고, 표시기가 모든 칸 채우기·테두리보다 높이 놓아 그 반투명 표시가 위에 덧칠되지 않습니다(겹친 강조 2단계 계획 §7.6).
    /// 다시 채우는 때는 <see cref="Show"/>뿐이고 매 프레임 하는 일은 없습니다. 좌표는 부모(필드 셀 루트) 로컬입니다.
    /// </remarks>
    internal sealed class FieldMovementPathLine
    {
        private const string BandName = "FieldMovementPath_";
        private const string ArrowName = "FieldMovementPathArrow";

        private readonly Transform _parent;
        private readonly Material _material;
        private readonly float _thickness;
        private readonly float _arrowLength;
        private readonly float _arrowWidth;
        private readonly float _tipOffset;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly List<Part> _bands = new();
        private readonly List<Vector3> _shape = new();
        private readonly Dictionary<int, Vector3[]> _points = new();

        private FieldArrowhead _arrow;

        /// <param name="config">경로 값(머티리얼·띠 두께·화살촉 길이·너비·꼭지 자리)의 원본입니다. 머티리얼이 있어야 합니다.</param>
        internal FieldMovementPathLine(Transform parent, StageFieldViewData config, IMeshSystemService meshes, IColorSystemService colors)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _material = config.MovementPathMaterial != null
                ? config.MovementPathMaterial
                : throw new ArgumentException("[FieldMovementPathLine] 이동 경로 머티리얼이 필요합니다.", nameof(config));
            _thickness = config.MovementPathThickness;
            _arrowLength = config.MovementPathArrowLength;
            _arrowWidth = config.MovementPathArrowWidth;
            _tipOffset = config.MovementPathArrowTipOffset;
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>
        /// 머티리얼의 색 덮어쓰기 속성을 확인하고, 화살촉 오브젝트(꺼 둔 상태)를 만들어 메시를 한 번 넣습니다.
        /// 띠는 처음 그릴 때 만듭니다. 실패하면 만든 것을 거두고 원인을 전파합니다.
        /// </summary>
        internal void Build()
        {
            if (_arrow != null)
                throw new InvalidOperationException("[FieldMovementPathLine] 이미 구성돼 있습니다.");
            // 조건은 DrawPointLineSystem.InitData와 같다(계획 TP-6).
            if (!_material.HasProperty(MaterialBaseTypeKey.MainColor.Name))
                throw new InvalidOperationException("[FieldMovementPathLine] 이동 경로 머티리얼에 _BaseColor가 없습니다.");

            try
            {
                _arrow = new FieldArrowhead(_parent, ArrowName, _material, _arrowLength, _arrowWidth, _meshes, _colors, shadows: true);
            }
            catch (Exception error)
            {
                try
                {
                    Release();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException("[FieldMovementPathLine] 구성에 실패했고 정리도 실패했습니다.", error, releaseError);
                }

                throw;
            }
        }

        /// <summary>
        /// 경로를 그립니다. <paramref name="runs"/>의 앞쪽 <paramref name="runCount"/>개가 높이 구간이고(구간마다 필드 로컬 점, 높이 포함),
        /// 마지막 구간의 마지막 점이 도착 칸입니다. 점이 둘 이상인 마지막 구간의 띠 끝을 화살촉 안으로 줄이고, 화살촉은 도착 칸에서
        /// <paramref name="lastStep"/>(경로 마지막 걸음, 필드 로컬) 방향을 봅니다. 점이 둘 미만인 구간은 띠를 끕니다.
        /// </summary>
        internal void Show(IReadOnlyList<IReadOnlyList<Vector3>> runs, int runCount, Vector3 lastStep, Color color)
        {
            if (runs == null)
                throw new ArgumentNullException(nameof(runs));
            if (runCount < 1 || runCount > runs.Count)
                throw new ArgumentOutOfRangeException(nameof(runCount));
            if (_arrow == null)
                throw new InvalidOperationException("[FieldMovementPathLine] 구성하기 전에 그릴 수 없습니다.");

            // 높이가 바뀐 칸에서 끝나면 마지막 구간은 도착 칸 하나뿐이라 띠가 없고, 그 앞 구간이 도착 칸 가운데까지 이어져 있습니다.
            // 그때는 그 앞 구간의 띠를 줄여야 화살촉을 덮지 않습니다(중간 구간은 늘 점이 둘 이상, 묶음 끝 검토 M1).
            int trimRun = runs[runCount - 1].Count >= 2 ? runCount - 1 : runCount - 2;
            float trimAlong = FieldMovementPathShape.TrimAlong(_tipOffset, _arrowLength);
            for (int k = 0; k < runCount; k++)
            {
                FieldMovementPathShape.Fill(runs[k], k == trimRun, trimAlong, _thickness * 0.5f, _shape);
                Part band = BandAt(k);
                if (_shape.Count < 2)
                {
                    band.Renderer.enabled = false;
                    continue;
                }

                Vector3[] points = PointsFor(_shape.Count);
                _shape.CopyTo(points);
                band.MeshHandle.Update(new MultiLineCreateData(band.Name, points, _thickness, 1f, Color.white));
                Paint(band, color);
                band.Renderer.enabled = true;
            }

            for (int k = runCount; k < _bands.Count; k++)
                _bands[k].Renderer.enabled = false;

            IReadOnlyList<Vector3> last = runs[runCount - 1];
            if (last.Count == 0)
                throw new ArgumentException("[FieldMovementPathLine] 마지막 구간에 도착 칸이 없습니다.", nameof(runs));
            _arrow.Show(last[last.Count - 1], lastStep, color, _tipOffset);
        }

        /// <summary>이 부품이 <paramref name="config"/>의 경로 머티리얼로 그리는지입니다. 렌더러를 다시 쓸지 판단할 때 묻습니다.</summary>
        internal bool Uses(StageFieldViewData config) => config != null && config.MovementPathMaterial == _material;

        /// <summary>띠와 화살촉을 모두 끕니다. 구성 전이어도 안전합니다.</summary>
        internal void Clear()
        {
            for (int k = 0; k < _bands.Count; k++)
                _bands[k].Renderer.enabled = false;
            _arrow?.Clear();
        }

        /// <summary>
        /// 화살촉·띠마다 Color 핸들 → Mesh 핸들 → 오브젝트 순서로 각각 거두고, 실패는 모아 한 번 던집니다. 여러 번 불려도 안전합니다.
        /// </summary>
        internal void Release()
        {
            List<Exception> errors = null;
            FieldArrowhead arrow = _arrow;
            _arrow = null;
            try
            {
                arrow?.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new List<Exception>()).Add(error);
            }
            for (int k = 0; k < _bands.Count; k++)
                _bands[k].Dispose(ref errors);
            _bands.Clear();

            if (errors != null)
                throw new AggregateException("[FieldMovementPathLine] 해제 중 실패했습니다.", errors);
        }

        // 구간 번호의 띠입니다. 없으면 만듭니다(번호는 0부터 차례로 늘어남).
        private Part BandAt(int index)
        {
            while (_bands.Count <= index)
                _bands.Add(CreatePart(BandName + _bands.Count));
            return _bands[index];
        }

        // 점 수에 맞는 배열입니다. 점 수마다 처음 쓸 때 한 번만 만듭니다(MultiLine은 배열 길이를 점 수로 쓰고, 메시는 Update 안에서 바로 만들어짐).
        private Vector3[] PointsFor(int count)
        {
            if (!_points.TryGetValue(count, out Vector3[] points))
            {
                points = new Vector3[count];
                _points.Add(count, points);
            }
            return points;
        }

        // 색이 바뀐 오브젝트에만 색을 넣습니다.
        private static void Paint(Part part, Color color)
        {
            if (part.ShownColor == color)
                return;
            part.ColorHandle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, color);
            part.ShownColor = color;
        }

        // 꺼 둔 오브젝트 하나를 만들고 Mesh·Color 핸들을 등록합니다. 실패하면 만든 것을 거두고 원인을 전파합니다.
        private Part CreatePart(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_parent, false);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.enabled = false;
            var part = new Part(name, gameObject, renderer);
            try
            {
                part.MeshHandle = _meshes.Register(filter);
                part.ColorHandle = _colors.CreateColorHandle(renderer)
                    ?? throw new InvalidOperationException("[FieldMovementPathLine] 렌더러를 ColorSystem에 등록하지 못했습니다.");
                return part;
            }
            catch (Exception error)
            {
                List<Exception> errors = null;
                part.Dispose(ref errors);
                if (errors == null)
                    throw;

                errors.Insert(0, error);
                throw new AggregateException("[FieldMovementPathLine] 오브젝트를 만들다 실패했고 정리도 실패했습니다.", errors);
            }
        }

        /// <summary>띠 하나 또는 화살촉 하나의 오브젝트·핸들과 마지막으로 넣은 색입니다.</summary>
        private sealed class Part
        {
            internal Part(string name, GameObject gameObject, MeshRenderer renderer)
            {
                Name = name;
                GameObject = gameObject;
                Renderer = renderer;
            }

            internal string Name { get; }
            internal GameObject GameObject { get; private set; }
            internal MeshRenderer Renderer { get; private set; }
            internal IMeshRenderHandle MeshHandle { get; set; }
            internal IColorHandle ColorHandle { get; set; }
            internal Color? ShownColor { get; set; }

            // Color 핸들 → Mesh 핸들 → 오브젝트 순서로 각각 거둡니다. 참조를 먼저 비워 두 번 거두지 않습니다.
            internal void Dispose(ref List<Exception> errors)
            {
                IColorHandle color = ColorHandle;
                IMeshRenderHandle mesh = MeshHandle;
                GameObject gameObject = GameObject;
                ColorHandle = null;
                MeshHandle = null;
                GameObject = null;
                Renderer = null;
                ShownColor = null;

                try
                {
                    color?.Dispose();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
                try
                {
                    mesh?.Dispose();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
                if (gameObject != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(gameObject);
                    else
                        UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
        }
    }
}
