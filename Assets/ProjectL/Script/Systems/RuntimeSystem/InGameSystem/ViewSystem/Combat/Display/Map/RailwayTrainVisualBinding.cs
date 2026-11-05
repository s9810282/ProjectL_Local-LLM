using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>열차 Head 프리팹의 외형 루트·선택적 Animator·앞면·뒤 연결점을 명시합니다. 재생 상태는 Presenter가 소유합니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    internal sealed class RailwayTrainVisualBinding : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;
        [Tooltip("애니메이션 계층 밖에 둔 열차 앞부분의 접촉 기준점입니다.")]
        [SerializeField] private Transform frontContact;
        [Tooltip("Body 칸의 Front Joint를 맞출 뒤 연결점입니다. Model Root 밖에 둡니다. Body Count가 0이면 비워도 됩니다.")]
        [SerializeField] private Transform rearJoint;

        internal Transform ModelRoot => modelRoot;
        internal Animator Animator => animator;
        internal Transform FrontContact => frontContact;
        /// <summary>Body Count가 0이면 null일 수 있습니다.</summary>
        internal Transform RearJoint => rearJoint;

        internal void Validate(RailwayVisualData config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (modelRoot == null || modelRoot == transform || !modelRoot.IsChildOf(transform))
                throw new InvalidOperationException("[RailwayTrainVisualBinding] 프리팹 내부의 별도 Model Root가 필요합니다.");
            if (!IsOutsideModel(frontContact))
                throw new InvalidOperationException("[RailwayTrainVisualBinding] Model Root 밖의 자식 Front Contact가 필요합니다.");
            if (config.TrainBodyCount > 0 && !IsOutsideModel(rearJoint))
                throw new InvalidOperationException("[RailwayTrainVisualBinding] Body를 붙이려면 Model Root 밖의 자식 Rear Joint가 필요합니다.");
            if (animator != null && animator.transform != modelRoot && !animator.transform.IsChildOf(modelRoot))
                throw new InvalidOperationException("[RailwayTrainVisualBinding] Animator는 Model Root 안에 있어야 합니다.");
            if ((config.Clip != null || config.Controller != null) && animator == null)
                throw new InvalidOperationException("[RailwayTrainVisualBinding] Clip/Controller 설정에는 Animator가 필요합니다.");
        }

        // 애니메이션이 움직이지 않는 자리(루트의 자식이면서 Model Root 밖)인지 봅니다.
        private bool IsOutsideModel(Transform point) =>
            point != null && point != transform && point.IsChildOf(transform) &&
            point != modelRoot && !point.IsChildOf(modelRoot);
    }
}
