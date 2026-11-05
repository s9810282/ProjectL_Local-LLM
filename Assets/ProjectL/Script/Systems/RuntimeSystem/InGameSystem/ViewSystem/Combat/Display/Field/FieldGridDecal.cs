using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 필드 셀 격자 선을 URP 데칼(GridDecal 셰이더)로 그립니다. 선 간격은 칸 중심 간격, 선 두께는 View DB의 gridLineWidth입니다.
    /// GridOutline 모드 표시기(<see cref="FieldOutlineRenderer"/>)만 씁니다.
    /// </summary>
    /// <remarks>
    /// 셰이더는 월드 XZ로 격자를 계산하므로 필드 루트가 회전하지 않았을 때만 그립니다.
    /// 공유 머티리얼을 바꾸지 않도록 인스턴스를 만들어 값을 넣고, <see cref="Dispose"/>에서 파괴합니다.
    /// </remarks>
    internal sealed class FieldGridDecal
    {
        private static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        private static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
        private static readonly int GridEdgeSizeId = Shader.PropertyToID("_GridEdgeSize");
        private static readonly int GridOriginId = Shader.PropertyToID("_GridOrigin");
        private static readonly int MaskCenterId = Shader.PropertyToID("_MaskCenter");

        private readonly Transform _fieldRoot;
        private GameObject _gameObject;
        private DecalProjector _projector;
        private Material _source;
        private Material _instance;
        private bool _warnedRotation;

        internal FieldGridDecal(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot != null ? fieldRoot : throw new ArgumentNullException(nameof(fieldRoot));
        }

        /// <summary>
        /// 격자 크기에 맞춰 데칼을 배치하고 셰이더 값을 채웁니다. 머티리얼이 없거나 필드가 회전돼 있으면 숨깁니다.
        /// </summary>
        /// <param name="lineWidth">격자 선 두께(월드 단위). 칸 간격보다 작아야 합니다.</param>
        /// <param name="renderingLayers">데칼을 받을 표면의 Rendering Layer 마스크입니다(View DB). 데칼 레이어가 꺼진 렌더러에서는 무시됩니다.</param>
        /// <param name="surfaceTop">
        /// 가장 높은 격자 높이(필드 로컬, 고지대 블록 윗면)입니다. 투영 상자를 그만큼 위로 늘려 블록 윗면에도 격자를 찍습니다(고지대 View 계획 P15).
        /// 0이면 지금처럼 필드 평면 위아래로 depth/2씩입니다. 바닥 타일 높이로는 늘리지 않습니다(타일 높이 설계 TH4 개정 2).
        /// </param>
        internal void Show(FieldGridMapper mapper, FieldCellSpacing spacing, float lineWidth, Material source, float depth,
            uint renderingLayers, float surfaceTop = 0f)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (source == null)
            {
                Hide();
                return;
            }

            if (Quaternion.Angle(_fieldRoot.rotation, Quaternion.identity) > 0.01f)
            {
                if (!_warnedRotation)
                {
                    Debug.LogWarning("[FieldGridDecal] 격자 데칼은 회전하지 않은 필드에서만 그립니다.", _fieldRoot);
                    _warnedRotation = true;
                }

                Hide();
                return;
            }

            EnsureProjector(source);

            // 선은 셀 경계를 중심으로 칸 간격의 2·(1 - 가장자리 값)만큼 그려진다. 가장자리 값은 두 축이 같으므로
            // 넓은 축에서도 선이 lineWidth를 넘지 않도록 넓은 칸 간격 기준 비율을 쓴다.
            float lineRatio = lineWidth / Mathf.Max(spacing.Width, spacing.Length);
            float edge = Mathf.Clamp01(1f - (lineRatio * 0.5f));

            // 격자선이 칸 경계에 오도록 필드 모서리(월드 XZ)를 격자 원점으로, 필드 중심을 마스크 중심으로 넣는다.
            Vector3 corner = mapper.LocalToWorld(new Vector3(-mapper.TotalWidth * 0.5f, 0f, -mapper.TotalLength * 0.5f));
            Vector3 center = _fieldRoot.position;
            _instance.SetVector(CellSizeId, new Vector4(spacing.Width, spacing.Length, 0f, 0f));
            _instance.SetFloat(GridSizeId, 1f);
            _instance.SetFloat(GridEdgeSizeId, edge);
            _instance.SetVector(GridOriginId, new Vector4(corner.x, corner.z, 0f, 0f));
            _instance.SetVector(MaskCenterId, new Vector4(center.x, center.z, 0f, 0f));

            // 바닥처럼 같은 Rendering Layer를 켠 표면에만 찍습니다. 캐릭터·벽은 이 레이어를 켜지 않습니다.
            // URP DecalProjector의 renderingLayerMask setter는 데칼 캐시 갱신(OnValidate)을 부르지 않으므로,
            // 갱신을 부르는 size·pivot보다 먼저 넣어야 캐시에 반영됩니다. 뒤에 넣으면 캐시는 기본값(Default)으로 남아
            // 모든 표면에 찍히다가, 다른 계기(Game 뷰 크기 변경 등)로 캐시가 다시 만들어질 때에야 걸러집니다.
            _projector.renderingLayerMask = renderingLayers;

            // 투영 방향(로컬 +Z)을 아래로 두고, 상자가 필드 평면 아래로 depth/2, 위로 depth/2 + 표면 높이만큼 걸치게 한다.
            // 표면 높이 0이면 위아래 depth/2씩으로 지금과 같다.
            float top = Mathf.Max(0f, surfaceTop);
            float height = depth + top;
            Transform decal = _gameObject.transform;
            decal.localPosition = new Vector3(0f, (depth * 0.5f) + top, 0f);
            decal.localRotation = Quaternion.Euler(90f, 0f, 0f);
            decal.localScale = Vector3.one;
            _projector.size = new Vector3(mapper.TotalWidth, mapper.TotalLength, height);
            _projector.pivot = new Vector3(0f, 0f, height * 0.5f);

            if (!_gameObject.activeSelf)
                _gameObject.SetActive(true);
        }

        internal void Hide()
        {
            if (_gameObject != null && _gameObject.activeSelf)
                _gameObject.SetActive(false);
        }

        /// <summary>만든 데칼 오브젝트와 머티리얼 인스턴스를 파괴합니다. 여러 번 불려도 안전합니다.</summary>
        internal void Dispose()
        {
            DestroySafe(_gameObject);
            DestroySafe(_instance);
            _gameObject = null;
            _projector = null;
            _instance = null;
            _source = null;
        }

        private void EnsureProjector(Material source)
        {
            if (_gameObject == null)
            {
                _gameObject = new GameObject("FieldGridDecal");
                _gameObject.transform.SetParent(_fieldRoot, false);
                _projector = _gameObject.AddComponent<DecalProjector>();
            }

            if (_source == source)
                return;

            DestroySafe(_instance);
            _source = source;
            _instance = new Material(source) { name = $"{source.name} (FieldGrid)" };
            _projector.material = _instance;
        }

        private static void DestroySafe(Object target)
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
