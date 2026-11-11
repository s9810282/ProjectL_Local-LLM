using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>기존 카메라 소유자의 자동 조작만 빌립니다. 별도 Catalog 핸들을 만들지 않습니다.</summary>
    internal interface IStageMapCameraTourControl
    {
        bool IsReady { get; }
        long BeginTour(Vector3 playerPosition, Vector3 bossPosition);
        void SetTourProgress(long generation, float progress);
        void EndTour(long generation, bool restoreStart);
    }
}
