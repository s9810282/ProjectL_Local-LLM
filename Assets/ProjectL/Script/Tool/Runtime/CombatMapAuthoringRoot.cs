using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>전투 배경 편집 씬의 선택 상태와 프리팹 인스턴스만 보관합니다. 운영 전투에는 사용하지 않습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatMapAuthoringRoot : MonoBehaviour
    {
        [SerializeField] private ScriptableObject viewStage;
        [SerializeField, HideInInspector] private ScriptableObject loadedViewStage;
        [SerializeField, HideInInspector] private GameObject previewContainer;
        [SerializeField, HideInInspector] private GameObject previewMap;
        [SerializeField, HideInInspector] private Vector3 draftPositionOffset;
        [SerializeField, HideInInspector] private Vector3 draftRotationOffset;
        [SerializeField, HideInInspector] private Vector3 draftScaleOffset = Vector3.one;
        [SerializeField, HideInInspector] private Vector3 draftTileOffset;
        [SerializeField, HideInInspector] private Vector2Int draftAnchorTile;
        [SerializeField, HideInInspector] private int previewUnitKey;
        [SerializeField, HideInInspector] private Vector2Int previewUnitCell;
        [SerializeField, HideInInspector] private GameObject previewUnit;
        [SerializeField, HideInInspector] private float previewSurfaceHeight;

        public ScriptableObject ViewStage => viewStage;
        public ScriptableObject LoadedViewStage => loadedViewStage;
        public GameObject PreviewContainer => previewContainer;
        public GameObject PreviewMap => previewMap;
        public Vector3 DraftPositionOffset => draftPositionOffset;
        public Vector3 DraftRotationOffset => draftRotationOffset;
        public Vector3 DraftScaleOffset => draftScaleOffset;
        public Vector3 DraftTileOffset => draftTileOffset;
        public Vector2Int DraftAnchorTile => draftAnchorTile;
        public int PreviewUnitKey => previewUnitKey;
        public Vector2Int PreviewUnitCell => previewUnitCell;
        public GameObject PreviewUnit => previewUnit;
        public float PreviewSurfaceHeight => previewSurfaceHeight;

        /// <summary>편집용 유닛 외형과 확인할 필드 칸입니다. 운영 데이터에는 쓰지 않습니다.</summary>
        public void SetUnitPreviewSelection(int key, Vector2Int cell)
        {
            previewUnitKey = key;
            previewUnitCell = cell;
        }

        /// <summary>현재 씬에 세운 편집용 유닛과 계산한 타일 바탕 높이를 기록합니다.</summary>
        public void SetUnitPreview(GameObject unit, float surfaceHeight)
        {
            previewUnit = unit;
            previewSurfaceHeight = surfaceHeight;
        }

        /// <summary>에디터의 명시적 불러오기가 끝났을 때 현재 표시의 출처를 기록합니다.</summary>
        public void SetPreview(ScriptableObject stage, GameObject container, GameObject map,
            Vector3 position, Vector3 rotation, Vector3 scale, Vector3 tileOffset, Vector2Int anchorTile)
        {
            loadedViewStage = stage;
            previewContainer = container;
            previewMap = map;
            SetDraftOffsets(position, rotation, scale, tileOffset, anchorTile);
        }

        /// <summary>원본 SO에 쓰기 전인 편집 씬의 배치 작업 값입니다.</summary>
        public void SetDraftOffsets(Vector3 position, Vector3 rotation, Vector3 scale, Vector3 tileOffset,
            Vector2Int anchorTile)
        {
            draftPositionOffset = position;
            draftRotationOffset = rotation;
            draftScaleOffset = scale;
            draftTileOffset = tileOffset;
            draftAnchorTile = anchorTile;
        }
    }
}
