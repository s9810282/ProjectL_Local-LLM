using System;
using System.Collections.Generic;
using ProjectL.Tool.Runtime;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 스테이지 배경 프랍(View DB StageViewProp)을 필드 기준으로 하나 세웁니다.
    /// Combat 씬 로드 때 스테이지 키로 한 번 고르고, 씬 연결을 놓을 때 없앱니다. 게임 상태와 무관한 장식입니다.
    /// </summary>
    /// <remarks>
    /// 세우는 방식은 프랍의 배경 방식(<see cref="StageViewPropMode"/>)입니다. 맵 SO는 MapDefinition 방식에서만 읽히고(<c>StageViewPropData.Map</c>),
    /// PrefabWithTiles 방식이면 바닥 타일 루트를 배경 아래 자식으로 데이터의 Offset 자리에 세우고(<see cref="StageMapTiles"/>),
    /// 배경을 돌리고 옮겨(타일은 자식이라 따라옴) 게임 (0,0) 칸을 데이터의 기준 타일 칸에 둡니다(<see cref="CombatMapTileAlignment"/>, 타일 맞춤 설계 TA3 2026-10-28 개정 5). 필드는 움직이지 않습니다.
    /// 같은 스테이지로 다시 세우면 기존 인스턴스를 그대로 둡니다. 키에 맞는 프랍이 없으면 아무것도 두지 않습니다.
    /// 필드 칸 집기는 평면 계산이라 프랍의 콜라이더가 입력을 가리지 않습니다.
    /// 주인은 전투 표시가 아니라 조정자의 씬 수명이고, 카메라 다시 연결(Combat Enter <c>SceneNodeOrder.BindCameras</c>)보다 먼저 세웁니다(배경 설계 BD1·BD3).
    /// 프랍 안의 버추얼 카메라(Day·Night)는 배경을 다 맞춘 뒤 읽어 <see cref="MapCameras"/>로 내기만 합니다.
    /// 카메라 창구에 넘기는 일은 Factory가 표시를 만들 때 합니다(카메라 연출 조각 설계 K1, 2026-10-23 개정).
    /// </remarks>
    internal sealed class StageViewPropPresenter
    {
        // 맵 프리팹 안에서 낙석 터널 폭발 자리를 나타내는 오브젝트 이름입니다. 이름 규칙의 원본은 여기 한 곳입니다.
        internal const string RockfallTunnelObjectName = "RockfallTunnelBoom";
        // 맵 프리팹 안에서 낙석 돌이 출발하는 능선의 두 끝점을 나타내는 오브젝트 이름입니다(낙석 View 설계 RG2).
        internal const string RockfallRidgeStartObjectName = "RockfallRidgeStart";
        internal const string RockfallRidgeEndObjectName = "RockfallRidgeEnd";

        private readonly IStageViewPropTableQuery _props;
        private readonly Transform _parent;
        // 바닥 타일을 칸 경계에 맞출 때 칸 간격을 읽는 필드 표시 설정입니다(필드는 씬 로드 뒤에 만들어져 Mapper를 쓸 수 없음).
        private readonly StageFieldViewData _field;
        private readonly List<CombatMapCamera> _mapCameras = new();
        private CombatCameraSettingsData _cameraSettings;
        private CombatMapTileSurface _tileSurface;
        private IReadOnlyList<Transform> _placedTiles = Array.Empty<Transform>();
        internal IReadOnlyList<Transform> PlacedTiles => _placedTiles;
        private IReadOnlyList<CombatBoardRevealTarget> _backdropRevealTargets = Array.Empty<CombatBoardRevealTarget>();
        internal IReadOnlyList<CombatBoardRevealTarget> BackdropRevealTargets => _backdropRevealTargets;
        private Transform _rockfallTunnel;
        private Transform _rockfallRidgeStart;
        private Transform _rockfallRidgeEnd;
        private bool _rockfallTunnelExplosionEnabled = true;
        private GameObject _instance;
        private int _stageKey;

        internal StageViewPropPresenter(IStageViewPropTableQuery props, Transform parent, StageFieldViewData field)
        {
            _props = props ?? throw new ArgumentNullException(nameof(props));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            _parent = parent;
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        /// <summary>세운 프랍 안의 버추얼 카메라 사실입니다. 프랍이 없으면 비어 있습니다.</summary>
        internal IReadOnlyList<CombatMapCamera> MapCameras => _mapCameras;

        /// <summary>
        /// 세운 프랍의 카메라 조작 값입니다(카메라 조작 설계 CC5). 프랍이 없으면 null입니다.
        /// 운영 DB는 필수로 검사합니다(<c>ViewStageData</c>). DB를 거치지 않은 조회(시험)에서만 프랍이 있어도 null일 수 있습니다.
        /// </summary>
        internal CombatCameraSettingsData CameraSettings => _cameraSettings;

        /// <summary>
        /// 세운 바닥 타일의 필드 칸별 윗면 높이 표입니다(타일 높이 설계 TH2). 타일 방식이 아니거나 프랍이 없으면 null입니다.
        /// 타일 맞춤을 끝낸 직후 한 번 만들고, 배경을 치우면 비웁니다.
        /// </summary>
        internal CombatMapTileSurface TileSurface => _tileSurface;

        /// <summary>
        /// 세운 프랍 안의 낙석 터널 폭발 자리입니다(낙석 View 계획 RP-1). 맵 프리팹의 <c>RockfallTunnelBoom</c> 오브젝트이고, 배경을 옮기면 함께 움직입니다.
        /// 프랍에 없으면 null이고, 그 스테이지는 폭발 없이 경고 칸만 켭니다.
        /// </summary>
        internal Transform RockfallTunnel => _rockfallTunnel;

        /// <summary>이 스테이지의 터널 폭발 사용 여부입니다. 배경이 없으면 기존 기본값인 켜짐입니다(편집 설계 RTE).</summary>
        internal bool RockfallTunnelExplosionEnabled => _rockfallTunnelExplosionEnabled;

        /// <summary>세운 프랍 안의 낙석 능선 한쪽 끝입니다(낙석 View 설계 RG2). 배경을 옮기면 함께 움직입니다. 프랍에 없으면 null입니다.</summary>
        internal Transform RockfallRidgeStart => _rockfallRidgeStart;

        /// <summary>세운 프랍 안의 낙석 능선 다른 쪽 끝입니다(낙석 View 설계 RG2). 배경을 옮기면 함께 움직입니다. 프랍에 없으면 null입니다.</summary>
        internal Transform RockfallRidgeEnd => _rockfallRidgeEnd;

        /// <summary>스테이지 키의 프랍을 필드 아래에 둡니다. 키가 0이거나 등록된 프랍이 없으면 기존 것만 치웁니다.</summary>
        /// <param name="boardSize">이 전투의 보드 크기입니다. 바닥 타일 방식이면 필요하고, 없으면(전투 없음) 그 방식은 예외입니다(계획 P5).</param>
        internal void Show(int stageKey, Vector2Int? boardSize)
        {
            if (_instance != null && _stageKey == stageKey)
                return;

            Release();
            if (stageKey < 1 || !_props.TryGetStageViewProp(stageKey, out StageViewPropData data))
                return;

            _instance = data.Map != null ? MapAssembler.Instantiate(data.Map, _parent) : Object.Instantiate(data.GameObject, _parent, false);
            Transform instance = _instance.transform;
            instance.localPosition = data.PositionOffset;
            instance.localRotation = Quaternion.Euler(data.RotationOffset);
            instance.localScale = Vector3.Scale(data.GameObject.transform.localScale, data.ScaleOffset);
            // 타일을 세우거나 맞추거나 카메라를 읽지 못하면(같은 이름 둘 등) 세운 프랍도 치워, 다음 같은 키 설치가 반쯤 세운 채 지나가지 않게 합니다.
            // 카메라 기준 자세는 배경을 다 맞춘 뒤의 월드 값입니다(카메라 조작 설계 CC6). 조작 값은 같은 조회에서 한 번 읽습니다(CC5).
            try
            {
                CombatMapTilesData tiles = data.MapTiles;
                if (tiles != null)
                    ShowTiles(tiles, boardSize, stageKey);
                _backdropRevealTargets = CombatBackdropRevealTargets.Read(instance, _parent, _placedTiles);
                CombatMapCameras.Read(_instance.transform, _mapCameras);
                _cameraSettings = data.CameraSettings;
                _rockfallTunnelExplosionEnabled = data.RockfallTunnelExplosionEnabled;
                _rockfallTunnel = FindUniqueByName(_instance.transform, RockfallTunnelObjectName, stageKey);
                _rockfallRidgeStart = FindUniqueByName(_instance.transform, RockfallRidgeStartObjectName, stageKey);
                _rockfallRidgeEnd = FindUniqueByName(_instance.transform, RockfallRidgeEndObjectName, stageKey);
            }
            catch
            {
                Release();
                throw;
            }
            _stageKey = stageKey;
        }

        /// <summary>
        /// 세운 프랍(아래의 바닥 타일 포함)을 없애고 맵 카메라 사실을 비웁니다. 창구는 그 전에 표시 해제가 비웁니다(배경 설계 BD4).
        /// 여러 번 불려도 안전합니다.
        /// </summary>
        internal void Release()
        {
            GameObject instance = _instance;
            _instance = null;
            _stageKey = 0;
            _mapCameras.Clear();
            _cameraSettings = null;
            _tileSurface = null;
            _placedTiles = Array.Empty<Transform>();
            _backdropRevealTargets = Array.Empty<CombatBoardRevealTarget>();
            _rockfallTunnel = null;
            _rockfallRidgeStart = null;
            _rockfallRidgeEnd = null;
            _rockfallTunnelExplosionEnabled = true;
            DestroyObject(instance);
        }

        // 프랍 아래(비활성 포함)에서 그 이름의 오브젝트를 찾습니다. 없으면 null이고, 둘이면 어느 자리인지 정할 수 없어 예외입니다.
        private static Transform FindUniqueByName(Transform root, string name, int stageKey)
        {
            Transform found = null;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != name)
                    continue;
                if (found != null)
                    throw new InvalidOperationException($"[StageViewPropPresenter] 맵 프리팹에 {name} 오브젝트가 둘 이상입니다. StageKey: {stageKey}");
                found = candidate;
            }
            return found;
        }

        // 바닥 타일 루트를 배경 아래 Offset 자리에 세우고, 배경(안의 카메라·타일 포함)을 돌리고 옮겨 게임 (0,0) 칸을 기준 타일 칸에 둔 뒤
        // 필드 칸별 타일 높이 표를 만듭니다(타일 맞춤 설계 TA3 2026-10-28 개정 5, 타일 높이 설계 TH2). 배경 높이는 데이터 값 그대로입니다(계획 P8).
        private void ShowTiles(CombatMapTilesData tiles, Vector2Int? boardSize, int stageKey)
        {
            if (boardSize == null)
            {
                throw new InvalidOperationException(
                    $"[StageViewPropPresenter] 바닥 타일을 필드에 맞추려면 전투(보드 크기)가 있어야 합니다. StageKey: {stageKey}");
            }
            FieldCellSpacing spacing = FieldCellSpacing.FromGridOutline(_field);
            Transform backdrop = _instance.transform;
            Transform tileRoot = StageMapTiles.Build(backdrop, tiles, out Transform[,] placed).transform;
            Vector3 columnLocal = new(tiles.TileSize.x, 0f, 0f);
            Vector3 rowLocal = new(0f, 0f, tiles.TileSize.y);

            // 1) 타일 축이 필드 축과 평행하도록 남은 각만큼 격자 가운데를 기준으로 배경을 돌립니다(설계 Q-TA3 A, 타일은 자식이라 따라옴).
            CombatMapTileAlignment.RequireLevel(_parent.InverseTransformDirection(tileRoot.up));
            float yaw = CombatMapTileAlignment.YawCorrection(ToField(tileRoot, columnLocal));
            backdrop.RotateAround(tileRoot.TransformPoint(tiles.GridCenter), _parent.up, yaw);

            // 2) 게임 (0,0) 칸이 기준 타일 칸에 오도록 배경을 수평 이동합니다. 위치 보정의 가로 값은 여기서 덮입니다(계획 P8).
            Vector3 center = _parent.InverseTransformPoint(tileRoot.TransformPoint(tiles.GridCenter));
            backdrop.localPosition += CombatMapTileAlignment.AnchorShift(center, ToField(tileRoot, columnLocal), ToField(tileRoot, rowLocal),
                tiles.Columns, tiles.Rows, tiles.AnchorTile, boardSize.Value, spacing, out bool fieldExceedsTiles);

            // 3) 맞춘 자리로 필드 칸마다 타일 윗면 높이와 타일을 적습니다(타일 높이 설계 TH2, 타일 교체 설계 TR2). 배경은 이 뒤로 움직이지 않습니다.
            _tileSurface = CombatMapTileSurface.Build(_parent, tileRoot, tiles, placed, boardSize.Value, spacing, out bool sharesTiles);
            var placedList = new List<Transform>(placed.Length);
            foreach (Transform tile in placed) placedList.Add(tile);
            _placedTiles = placedList.AsReadOnly();

            if (fieldExceedsTiles)
            {
                Debug.LogWarning(
                    $"[StageViewPropPresenter] 필드({boardSize.Value.x} × {boardSize.Value.y})가 기준 타일 {tiles.AnchorTile}에서 바닥 타일 맵({tiles.Columns} × {tiles.Rows}) 밖으로 넘쳐, 넘친 칸은 배경 위에 놓입니다. StageKey: {stageKey}",
                    tileRoot);
            }

            // 타일이 칸보다 커서 한 타일에 칸이 여럿 놓이면, 한 칸의 바닥 교체가 그 타일 전체를 비웁니다(타일 교체 설계 Q-TR3 A — 경고만).
            if (sharesTiles)
            {
                Debug.LogWarning(
                    $"[StageViewPropPresenter] 바닥 타일 한 장에 필드 칸이 둘 이상 놓입니다(타일 크기 {tiles.TileSize}). 바닥 교체(약한 지반·낭떠러지) 칸은 그 타일 전체를 비웁니다. StageKey: {stageKey}",
                    tileRoot);
            }
        }

        // 타일 루트 기준 벡터를 필드 칸 루트 기준으로 바꿉니다(위치·회전·배율 보정 반영).
        private Vector3 ToField(Transform tileRoot, Vector3 tileLocal) => _parent.InverseTransformVector(tileRoot.TransformVector(tileLocal));

        // EditMode 테스트에서는 Destroy를 쓸 수 없다.
        private static void DestroyObject(GameObject target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
