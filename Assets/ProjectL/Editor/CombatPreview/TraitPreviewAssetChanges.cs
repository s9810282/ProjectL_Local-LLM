using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using UnityEditor;

namespace ProjectL.Tool.Editor
{
    /// <summary>Inspector Undo 밖의 외부 SO·연결된 텍스트 프리팹 재임포트도 다음 안전한 Editor 갱신에서 재생을 취소합니다.</summary>
    internal sealed class TraitPreviewAssetChanges : AssetPostprocessor
    {
        private static bool _queued;
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (_queued || !EditorApplication.isPlaying || TraitActivationPreviewBridge.Host == null)
                return;
            if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(Related))
                return;
            _queued = true;
            var host = TraitActivationPreviewBridge.Host;
            // Import 콜백 안에서 표시를 다시 만들거나 DB를 조회하지 않습니다.
            EditorApplication.delayCall += () =>
            {
                _queued = false;
                if (host != null && TraitActivationPreviewBridge.Host == host && EditorApplication.isPlaying)
                    host.Stop();
            };
        }

        private static bool Related(string path) => path.EndsWith(".asset") && (path.StartsWith("Assets/ProjectL/DataBaseSO/") || path.StartsWith("Assets/ProjectL/Editor/CombatPreview/Presets/")) || TraitActivationPreviewBridge.Host != null && TraitActivationPreviewBridge.Host.IsBodyTextDependency(path);
    }
}
