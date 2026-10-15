using Cinemachine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem
{
    /// <summary>수집 당시 오브젝트 이름과 씬 가상 카메라 참조를 보관하는 일반 C# Node.</summary>
    internal sealed class CameraCatalogNode
    {
        internal string ObjectName { get; private set; }
        internal CinemachineVirtualCamera Camera { get; private set; }
        internal ulong Version { get; private set; }

        internal void Bind(string objectName, CinemachineVirtualCamera camera)
        {
            ObjectName = objectName;
            Camera = camera;
            Version++;
        }

        internal void Clear()
        {
            ObjectName = null;
            Camera = null;
            Version++;
        }
    }
}
