using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 열차 칸(Head + Body N개)을 잇는 규칙 한 곳입니다(칸 분리 설계 TC3). 설치 때 한 번만 부릅니다.
    /// 칸들은 같은 부모 아래에 놓여 이동 루트와 함께 움직이므로 매 프레임 할 일이 없습니다.
    /// </summary>
    internal static class RailwayTrainCars
    {
        internal const string BodyNamePrefix = "TrainBody_";

        /// <summary>
        /// <paramref name="correction"/> 아래 로컬 0에 Head를 놓고, Body를 하나씩 앞 칸 뒤에 붙인 뒤 Head 바인딩을 돌려줍니다.
        /// 뒤 칸의 Front Joint를 앞 칸의 Rear Joint 위치에 맞춥니다. Head의 활성화·Animator 처리는 부르는 쪽이 합니다.
        /// </summary>
        internal static RailwayTrainVisualBinding Build(Transform correction, RailwayVisualData config)
        {
            if (correction == null) throw new ArgumentNullException(nameof(correction));
            if (config == null) throw new ArgumentNullException(nameof(config));
            GameObject head = Object.Instantiate(config.TrainHeadPrefab, correction, false);
            RailwayTrainVisualBinding binding = head.GetComponent<RailwayTrainVisualBinding>();
            if (binding == null)
                throw new InvalidOperationException("[RailwayTrainCars] Train Head Prefab 루트에 RailwayTrainVisualBinding이 필요합니다.");
            binding.Validate(config);

            Transform rear = binding.RearJoint;
            for (int i = 1; i <= config.TrainBodyCount; i++)
            {
                GameObject body = Object.Instantiate(config.TrainBodyPrefab, correction, false);
                body.name = BodyNamePrefix + i;
                RailwayTrainBodyBinding joints = body.GetComponent<RailwayTrainBodyBinding>();
                if (joints == null)
                    throw new InvalidOperationException("[RailwayTrainCars] Train Body Prefab 루트에 RailwayTrainBodyBinding이 필요합니다.");
                joints.Validate();
                body.transform.position += rear.position - joints.FrontJoint.position;
                body.SetActive(true);
                rear = joints.RearJoint;
            }
            return binding;
        }
    }
}
