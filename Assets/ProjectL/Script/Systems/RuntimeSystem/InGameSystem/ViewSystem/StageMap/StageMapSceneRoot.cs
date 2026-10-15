using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬의 참조 묶음입니다. 동작은 없고 <see cref="StageMapViewSystem"/>이 씬 진입 때 한 번 찾아 씁니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageMapSceneRoot : MonoBehaviour
    {
        [Tooltip("노드와 연결선을 생성할 부모입니다. 맵 전체의 위치·회전·크기는 이 Transform으로 조정합니다.")]
        [SerializeField] private Transform mapRoot;

        internal Transform MapRoot => mapRoot;
    }
}
