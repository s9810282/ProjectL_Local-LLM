using Cinemachine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
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

        [Tooltip("챕터 배경을 생성할 부모입니다. MapRoot와 형제로 두고 배경 프리팹의 기준 좌표를 유지합니다.")]
        [SerializeField] private Transform backgroundRoot;

        [Tooltip("Canvas 구성과 등록 화면을 연결하는 UI 진입점입니다.")]
        [SerializeField] private StageMapCanvasView ui;

        [Tooltip("맵 드래그가 제어할 직교 가상 카메라입니다. 비활성화되는 MapRoot 밖에 둡니다.")]
        [SerializeField] private CinemachineVirtualCamera mapCamera;

        internal Transform MapRoot => mapRoot;

        internal Transform BackgroundRoot => backgroundRoot;

        internal StageMapCanvasView UI => ui;

        internal CinemachineVirtualCamera MapCamera => mapCamera;

#if UNITY_EDITOR
        // 작업값은 씬에 저장하지 않습니다. 확정값의 원본은 가상 카메라 Transform입니다.
        internal string CameraFramingKey => "ProjectL.StageCamera." + gameObject.scene.path + "." + mapCamera.name;
#endif

    }
}
