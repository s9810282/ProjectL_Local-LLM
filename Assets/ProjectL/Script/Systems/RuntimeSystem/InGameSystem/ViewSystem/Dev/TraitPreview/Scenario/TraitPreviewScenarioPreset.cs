#if UNITY_EDITOR
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>공통 블록 조합을 공유하는 입력 에셋입니다.</summary>
    [CreateAssetMenu(menuName = "ProjectL/Editor/Trait Preview Scenario")]
    public sealed class TraitPreviewScenarioPreset : ScriptableObject
    {
        public TraitPreviewScenarioInput input = new();
    }
}
#endif
