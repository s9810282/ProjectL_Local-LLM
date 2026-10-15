using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 필드에 놓인 유닛·구조물 하나의 표시입니다(옛 FieldUnitView 이식, 계획 D.2).
    /// 위치·방향·표시 여부·체력 숫자만 다루고, 실제 표시값과 다를 때만 씁니다(설계 §6.1).
    /// </summary>
    /// <remarks>
    /// 프리팹 인스턴스에 런타임으로 붙습니다. 애니메이션·피격·사망 연출은 이번 범위가 아닙니다(설계 §1).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FieldUnitView : MonoBehaviour
    {
        private TMP_Text _healthLabel;
        private Quaternion _healthLabelRotation;
        private bool _hasShownHealth;
        private int _shownHealth;
        private int _shownMaxHealth;
        private UnitHUD _unitHud;
        private Canvas _hudCanvas;
        private Camera _hudCamera;
        private Vector3 _hudOffset;
        private float _hudScale;
        internal UnitHUD Hud => _unitHud;

        internal EntityId EntityId { get; private set; }

        internal EntityKey EntityKey { get; private set; }

        internal TMP_Text HealthLabel => _healthLabel;

        internal void AttachUnitHud(UnitHUD prefab, Vector3 offset, float scale, Camera camera)
        {
            if (_unitHud != null || prefab == null) return;
            var canvasObject = new GameObject("UnitHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            canvasObject.transform.SetParent(transform, false);
            _hudCanvas = canvasObject.GetComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.WorldSpace;
            _hudCanvas.overrideSorting = true;
            _hudCanvas.sortingOrder = 10;
            _hudCamera = camera;
            _hudOffset = offset;
            _hudScale = scale;
            _unitHud = Instantiate(prefab, canvasObject.transform, false);
            ((RectTransform)canvasObject.transform).sizeDelta = ((RectTransform)_unitHud.transform).rect.size;
            _unitHud.gameObject.SetActive(false);
        }

        internal void ShowUnitHud(bool show)
        {
            if (_unitHud != null) _unitHud.gameObject.SetActive(show);
            if (_healthLabel != null) _healthLabel.gameObject.SetActive(!show);
        }

        private void LateUpdate()
        {
            if (_unitHud == null || !_unitHud.gameObject.activeSelf) return;
            Camera camera = _hudCamera != null ? _hudCamera : Camera.main;
            _hudCanvas.worldCamera = camera;
            Transform canvas = _hudCanvas.transform;
            canvas.position = transform.position + _hudOffset;
            if (camera != null) canvas.rotation = camera.transform.rotation;
            Vector3 scale = transform.lossyScale;
            canvas.localScale = new Vector3(
                _hudScale / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                _hudScale / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                _hudScale / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
        }

        /// <summary>풀에서 처음 만들 때 한 번 체력 표시를 붙입니다.</summary>
        internal void AttachHealthLabel(TMP_Text template, Vector3 localOffset)
        {
            if (_healthLabel != null || template == null)
                return;

            _healthLabel = Instantiate(template, transform, false);
            _healthLabel.name = "HealthLabel";
            _healthLabel.transform.localPosition = localOffset;
            // 유닛이 돌아도 숫자가 뒤집히지 않도록 템플릿의 월드 방향을 유지한다.
            _healthLabelRotation = template.transform.rotation;
            _healthLabel.transform.rotation = _healthLabelRotation;
            _healthLabel.gameObject.SetActive(true);
        }

        internal void Bind(EntityId entityId, EntityKey entityKey)
        {
            if (EntityId == entityId && EntityKey == entityKey)
                return;

            EntityId = entityId;
            EntityKey = entityKey;
            gameObject.name = $"FieldUnit_{entityKey}_{entityId.Value}";
        }

        /// <summary>현재 값을 적용합니다. 실제로 바뀐 속성이 있으면 true입니다.</summary>
        internal bool Apply(Vector3 worldPosition, Quaternion worldRotation, int currentHealth, int maxHealth, bool visible)
        {
            bool changed = false;
            Transform self = transform;

            // 연출·풀 재사용이 바꾼 값도 되돌리도록 저장값이 아니라 실제 표시값과 비교한다.
            if (self.position != worldPosition)
            {
                self.position = worldPosition;
                changed = true;
            }

            if (self.rotation != worldRotation)
            {
                self.rotation = worldRotation;
                changed = true;
            }

            if (_healthLabel != null && _healthLabel.transform.rotation != _healthLabelRotation)
            {
                _healthLabel.transform.rotation = _healthLabelRotation;
                changed = true;
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
                changed = true;
            }

            if (_healthLabel != null && (!_hasShownHealth || _shownHealth != currentHealth || _shownMaxHealth != maxHealth))
            {
                _healthLabel.SetText("{0}/{1}", currentHealth, maxHealth);
                _hasShownHealth = true;
                _shownHealth = currentHealth;
                _shownMaxHealth = maxHealth;
                changed = true;
            }

            return changed;
        }

        internal void ResetForPool()
        {
            if (_unitHud != null) _unitHud.Clear();
            EntityId = EntityId.Empty;
            _hasShownHealth = false;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
