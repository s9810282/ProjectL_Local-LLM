using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>화면 투영 호스트와 프리팹별 풀을 조립합니다. 발동별 스타일은 표시 요청이 전달합니다.</summary>
    internal static class CombatBodyTextFactory
    {
        internal static void ValidatePrefabs(CombatCueSources cues, GameObject defaultPrefab)
        {
            if (cues == null)
                throw new ArgumentNullException(nameof(cues));
            foreach (GameObject prefab in cues.BodyTextPrefabs(defaultPrefab))
                CombatBodyTextView.RequirePrefab(prefab);
        }

        internal static CombatBodyTextResources CreateResources(Scene ownerScene, Camera output)
        {
            if (!ownerScene.IsValid() || !ownerScene.isLoaded)
                throw new ArgumentException("몸체 텍스트를 소유할 Scene이 유효해야 합니다.", nameof(ownerScene));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            var host = new GameObject("CombatBodyTextCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                SceneManager.MoveGameObjectToScene(host, ownerScene);
                host.layer = 5;
                Canvas canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                canvas.targetDisplay = output.targetDisplay;
                CanvasScaler scaler = host.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                return new CombatBodyTextResources(host.GetComponent<RectTransform>());
            }
            catch
            {
                CombatBodyTextObjects.Destroy(host);
                throw;
            }
        }
    }
}
