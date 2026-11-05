using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 열차 Body 프리팹의 앞·뒤 연결점입니다. 앞 칸의 뒤 연결점에 이 칸의 앞 연결점을 맞춰 붙입니다(칸 분리 설계 TC2).
    /// 간격은 모델의 사실이라 프리팹이 원본입니다. 애니메이션은 두지 않습니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    internal sealed class RailwayTrainBodyBinding : MonoBehaviour
    {
        [Tooltip("앞 칸의 Rear Joint에 맞추는 이 칸의 앞 연결점입니다.")]
        [SerializeField] private Transform frontJoint;
        [Tooltip("다음 Body 칸을 맞출 이 칸의 뒤 연결점입니다.")]
        [SerializeField] private Transform rearJoint;

        internal Transform FrontJoint => frontJoint;
        internal Transform RearJoint => rearJoint;

        internal void Validate()
        {
            if (!IsChildPoint(frontJoint) || !IsChildPoint(rearJoint))
                throw new InvalidOperationException("[RailwayTrainBodyBinding] 루트의 자식 Front Joint와 Rear Joint가 필요합니다.");
            if (frontJoint == rearJoint)
                throw new InvalidOperationException("[RailwayTrainBodyBinding] Front Joint와 Rear Joint는 서로 다른 Transform이어야 합니다.");
        }

        private bool IsChildPoint(Transform point) => point != null && point != transform && point.IsChildOf(transform);
    }
}
