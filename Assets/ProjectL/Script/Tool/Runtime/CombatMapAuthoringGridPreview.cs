using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Rendering.Universal;
#endif

namespace ProjectL.Tool.Runtime
{
#if UNITY_EDITOR
    /// <summary>편집 씬의 필드 격자를 Game 뷰에 표시합니다. 데칼과 재질은 씬·원본 에셋에 저장하지 않습니다.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CombatMapAuthoringGridPreview : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Material sourceMaterial;
        [SerializeField, HideInInspector] private int columns;
        [SerializeField, HideInInspector] private int rows;
        [SerializeField, HideInInspector] private float cellWidth;
        [SerializeField, HideInInspector] private float cellLength;
        [SerializeField, HideInInspector] private float lineWidth;
        [SerializeField, HideInInspector] private float depth;
        [SerializeField, HideInInspector] private uint renderingLayers;

        private GameObject _decalObject;
        private Material _material;
        private string _problem;

        public string Problem => _problem;

        public void Configure(Material source, int fieldColumns, int fieldRows, float width, float length,
            float line, float projectorDepth, uint layers)
        {
            sourceMaterial = source;
            columns = fieldColumns;
            rows = fieldRows;
            cellWidth = width;
            cellLength = length;
            lineWidth = line;
            depth = projectorDepth;
            renderingLayers = layers;
            Refresh();
        }

        public void Refresh()
        {
            Release();
            _problem = null;
            if (!isActiveAndEnabled || sourceMaterial == null || columns < 1 || rows < 1) return;
            try
            {
                if (Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.01f
                    || transform.lossyScale != Vector3.one)
                    throw new InvalidOperationException("편집용 필드 그리드 루트는 회전 0·배율 1이어야 합니다.");
                CombatGridDecalLayout layout = CombatGridDecalLayout.Create(transform, columns, rows,
                    cellWidth, cellLength, lineWidth, depth);
                _material = new Material(sourceMaterial)
                {
                    name = $"{sourceMaterial.name} (AuthoringGrid)",
                    hideFlags = HideFlags.HideAndDontSave
                };
                layout.ApplyMaterial(_material);
                _decalObject = new GameObject("FieldGridPreview") { hideFlags = HideFlags.HideAndDontSave };
                _decalObject.transform.SetParent(transform, false);
                var projector = _decalObject.AddComponent<DecalProjector>();
                projector.material = _material;
                projector.renderingLayerMask = renderingLayers;
                _decalObject.transform.localPosition = layout.LocalPosition;
                _decalObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                projector.size = layout.Size;
                projector.pivot = layout.Pivot;
            }
            catch (Exception exception)
            {
                _problem = exception.Message;
                Release();
            }
        }

        private void OnEnable() => Refresh();
        private void OnDisable() => Release();
        private void OnDestroy() => Release();

        private void Release()
        {
            if (_decalObject != null) DestroyImmediate(_decalObject);
            if (_material != null) DestroyImmediate(_material);
            _decalObject = null;
            _material = null;
        }
    }
#endif
}
