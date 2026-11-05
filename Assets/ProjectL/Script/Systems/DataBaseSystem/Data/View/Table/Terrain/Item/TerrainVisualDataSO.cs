using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>
    /// 이웃 모양별 프리팹 묶음입니다(지형 이웃 모양 설계 TC1·TC5). Center는 외형의 기본 프리팹이라 여기에 없습니다.
    /// 칸은 비어 있어도 됩니다. 빈 칸을 무엇으로 그릴지는 표시 쪽 외형 선택이 정합니다.
    /// </summary>
    internal sealed class TerrainEdgeShapePrefabs
    {
        /// <summary>모양을 쓰지 않는 종류의 값입니다.</summary>
        internal static readonly TerrainEdgeShapePrefabs None = new(null, null, null, null, null);

        private readonly GameObject _oneSide;
        private readonly GameObject _twoSide;
        private readonly GameObject _twoSideOpposite;
        private readonly GameObject _threeSide;
        private readonly GameObject _fourSide;

        internal TerrainEdgeShapePrefabs(GameObject oneSide, GameObject twoSide, GameObject twoSideOpposite, GameObject threeSide,
            GameObject fourSide)
        {
            _oneSide = oneSide;
            _twoSide = twoSide;
            _twoSideOpposite = twoSideOpposite;
            _threeSide = threeSide;
            _fourSide = fourSide;
            HasAny = oneSide != null || twoSide != null || twoSideOpposite != null || threeSide != null || fourSide != null;
        }

        /// <summary>모양 칸이 하나라도 있는지입니다.</summary>
        public bool HasAny { get; }

        /// <summary>모양 칸의 프리팹입니다. 비어 있으면 null입니다. Center는 이 묶음에 없어 예외입니다.</summary>
        public GameObject Of(TerrainEdgeShape shape) => shape switch
        {
            TerrainEdgeShape.OneSide => _oneSide,
            TerrainEdgeShape.TwoSide => _twoSide,
            TerrainEdgeShape.TwoSideOpposite => _twoSideOpposite,
            TerrainEdgeShape.ThreeSide => _threeSide,
            TerrainEdgeShape.FourSide => _fourSide,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Center와 정의되지 않은 모양은 모양 묶음에 없습니다.")
        };
    }

    /// <summary>지형 종류의 표현 데이터입니다. 규칙 설정과 배치 좌표는 포함하지 않습니다.</summary>
    internal sealed class TerrainVisualData : IData
    {
        public TerrainContentKind Kind { get; }
        public TerrainVisualMode VisualMode { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public GameObject Prefab { get; }
        public Vector3 LocalOffset { get; }
        public Vector3 LocalScale { get; }
        public TornadoPresentationSettingsData TornadoPresentation { get; }

        /// <summary>
        /// 칸 표면 높이(필드 로컬 단위)입니다. 블록·유닛·걸음이 이 값 하나를 읽습니다(고지대 View 설계 Q-HG1). 뜻은 방식마다 다릅니다(타일 교체 설계 TR1).
        /// <list type="bullet">
        /// <item><see cref="TerrainVisualMode.OnTile"/>: 바탕(바닥 타일 윗면) 위로 올리는 높이, 0 이상. 0이면 바탕 그대로입니다.</item>
        /// <item><see cref="TerrainVisualMode.ReplaceTile"/>: 그 칸의 새 바탕 높이(바뀐 타일의 윗면, 필드 바닥면 기준), 음수 가능.</item>
        /// </list>
        /// </summary>
        public float SurfaceHeight { get; }

        // 이웃 모양별 프리팹입니다. 모양을 쓰지 않는 종류는 None입니다. 바깥은 UsesEdgeShapes·ShapePrefab으로만 읽습니다.
        private TerrainEdgeShapePrefabs EdgeShapes { get; }

        /// <summary>붙은 모양에 따라 프리팹을 바꾸는 종류인지입니다(모양 칸이 하나라도 있음, 지형 이웃 모양 설계 TC1).</summary>
        public bool UsesEdgeShapes => EdgeShapes.HasAny;

        internal TerrainVisualData(TerrainContentKind kind, TerrainVisualMode visualMode, string displayName, string description, Sprite icon,
            GameObject prefab, Vector3 localOffset, Vector3 localScale, float surfaceHeight = 0f, TerrainEdgeShapePrefabs edgeShapes = null,
            TornadoPresentationSettingsData tornadoPresentation = null)
        {
            if (kind == TerrainContentKind.None || !System.Enum.IsDefined(typeof(TerrainContentKind), kind))
            {
                throw new InvalidOperationException("지형 표현에는 지원되는 종류 키가 필요합니다.");
            }

            if (!System.Enum.IsDefined(typeof(TerrainVisualMode), visualMode))
            {
                throw new InvalidOperationException("지형 표현에는 지원되는 표시 방식이 필요합니다.");
            }

            if (!float.IsFinite(localOffset.x) || !float.IsFinite(localOffset.y) || !float.IsFinite(localOffset.z)
                || !float.IsFinite(localScale.x) || !float.IsFinite(localScale.y) || !float.IsFinite(localScale.z)
                || localScale.x <= 0f || localScale.y <= 0f || localScale.z <= 0f)
            {
                throw new InvalidOperationException("지형 표현 보정은 유한한 값이고 크기는 양수여야 합니다.");
            }

            if (!float.IsFinite(surfaceHeight))
            {
                throw new InvalidOperationException($"{kind}: 지형 표면 높이는 유한해야 합니다.");
            }

            // 방식별 규칙은 이 생성자 한 곳입니다(묶음 끝 검토 L2). 타일 위는 그릴 프리팹이 있어야 하고, 높이는 바탕 위로 올리는 값입니다.
            // 바닥 교체는 프리팹이 없으면 타일만 비웁니다(타일 교체 설계 TR1).
            if (visualMode == TerrainVisualMode.OnTile && prefab == null)
            {
                throw new InvalidOperationException($"{kind}: 타일 위(OnTile) 지형은 프리팹이 필요합니다.");
            }

            if (visualMode == TerrainVisualMode.OnTile && surfaceHeight < 0f)
            {
                throw new InvalidOperationException($"{kind}: 타일 위(OnTile) 지형의 표면 높이는 0 이상이어야 합니다. 바닥보다 낮은 칸은 바닥 교체(ReplaceTile)로 둡니다.");
            }

            // 토네이도 외형은 덮는 칸들의 바탕 위에 놓이는 연출이라 타일을 바꾸지 않습니다(타일 교체 설계 P5).
            if (kind == TerrainContentKind.Tornado && visualMode != TerrainVisualMode.OnTile)
            {
                throw new InvalidOperationException("토네이도 외형은 타일 위(OnTile) 방식이어야 합니다.");
            }

            edgeShapes ??= TerrainEdgeShapePrefabs.None;
            // 빈 모양 칸은 Center(기본 프리팹)로 그리므로 모양을 쓰면 기본 프리팹이 있어야 합니다.
            if (edgeShapes.HasAny && prefab == null)
            {
                throw new InvalidOperationException($"{kind}: 이웃 모양을 쓰면 Center 프리팹(Prefab)이 필요합니다.");
            }

            Kind = kind;
            VisualMode = visualMode;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
            Description = description ?? string.Empty;
            Icon = icon;
            Prefab = prefab;
            LocalOffset = localOffset;
            LocalScale = localScale;
            SurfaceHeight = surfaceHeight;
            EdgeShapes = edgeShapes;
            TornadoPresentation = tornadoPresentation;
        }

        /// <summary>
        /// 모양의 프리팹입니다. Center는 <see cref="Prefab"/>이고, 비어 있는 모양 칸은 null입니다.
        /// 빈 칸을 무엇으로 그릴지는 표시 쪽 외형 선택 한 곳이 정합니다.
        /// </summary>
        public GameObject ShapePrefab(TerrainEdgeShape shape) =>
            shape == TerrainEdgeShape.Center ? Prefab : EdgeShapes.Of(shape);
    }

    /// <summary>ViewGroup에서 설정할 지형 외형 SO입니다. 비어 있는 리소스는 후속 표시 연결 전에 지정합니다.</summary>
    [CreateAssetMenu(fileName = "NewTerrainVisual", menuName = "ProjectL/Database/View/TerrainVisual")]
    internal sealed class TerrainVisualDataSO : DatabaseItem
    {
        [SerializeField] private TerrainContentKind kind = TerrainContentKind.HighGround;
        [Tooltip("OnTile: 바닥 타일 위에 프리팹을 놓습니다(효과·벽·고지대, 프리팹 필수). ReplaceTile: 그 칸의 바닥 타일을 이 외형으로 바꿉니다(약한 지반·낭떠러지, 프리팹이 없으면 타일만 비움).")]
        [SerializeField] private TerrainVisualMode visualMode = TerrainVisualMode.OnTile;
        [SerializeField] private string displayName;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [Tooltip("이 종류의 프리팹입니다. 이웃 모양을 쓰면 Center(드러난 변 없음)이자 빈 모양 칸의 대체입니다.")]
        [SerializeField] private GameObject prefab;
        [Tooltip("표현 위치 보정입니다. 스테이지 배치 좌표가 아닙니다. 타일 위(OnTile)와 타일이 없는 칸은 칸 자리 기준·필드 축, 바닥 교체(ReplaceTile)의 타일 칸은 끈 타일 자리 기준·타일 루트 축입니다(크기 보정도 같음).")]
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [Tooltip("필드 로컬 단위입니다. OnTile: 바탕(바닥 타일 윗면) 위로 올리는 블록 높이, 0 이상(0이면 바탕 그대로). ReplaceTile: 바뀐 타일의 윗면 높이, 필드 바닥면 기준이고 음수 가능(낭떠러지는 구멍 바닥).")]
        [SerializeField] private float surfaceHeight;
        [Tooltip("토네이도에만 연결하는 전용 연출 설정입니다.")]
        [SerializeField] private TornadoPresentationSettingsSO tornadoPresentation;

        [Header("이웃 모양 (비우면 Center = Prefab, R0)")]
        [Tooltip("드러난 변 하나입니다. R0 기준 드러난 쪽은 동(+X)입니다.")]
        [SerializeField] private GameObject oneSidePrefab;
        [Tooltip("이웃한 드러난 변 둘(바깥 모서리)입니다. R0 기준 드러난 쪽은 북(+Z)·동입니다.")]
        [SerializeField] private GameObject twoSidePrefab;
        [Tooltip("마주 보는 드러난 변 둘(폭 1 줄)입니다. R0 기준 드러난 쪽은 동·서입니다.")]
        [SerializeField] private GameObject twoSideOppositePrefab;
        [Tooltip("드러난 변 셋(끝)입니다. R0 기준 드러난 쪽은 북·동·서이고 남에 이웃이 있습니다.")]
        [SerializeField] private GameObject threeSidePrefab;
        [Tooltip("드러난 변 넷(외딴 칸)입니다.")]
        [SerializeField] private GameObject fourSidePrefab;

        public override IData GetData()
        {
            if (kind == TerrainContentKind.Tornado && tornadoPresentation == null)
                throw new InvalidOperationException("토네이도 외형에는 전용 연출 설정이 필요합니다.");
            if (kind != TerrainContentKind.Tornado && tornadoPresentation != null)
                throw new InvalidOperationException("토네이도 연출 설정은 다른 지형에 연결할 수 없습니다.");
            return new TerrainVisualData(kind, visualMode, displayName, description, icon, prefab, localOffset, localScale, surfaceHeight,
                new TerrainEdgeShapePrefabs(oneSidePrefab, twoSidePrefab, twoSideOppositePrefab, threeSidePrefab, fourSidePrefab),
                tornadoPresentation != null ? tornadoPresentation.GetData() : null);
        }
    }
}
