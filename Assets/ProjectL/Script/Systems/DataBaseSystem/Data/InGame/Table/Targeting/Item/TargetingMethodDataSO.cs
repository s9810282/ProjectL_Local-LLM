using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item
{
    /// <summary>공격 지정 방식의 입력 SO입니다. UnitDataSO가 참조하고 순수 데이터에는 키만 전달합니다.</summary>
    [CreateAssetMenu(fileName = "NewTargetingMethodData", menuName = "ProjectL/Database/InGame/TargetingMethodData")]
    internal sealed class TargetingMethodDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int methodKey = 1;
        [SerializeField] private TargetingShape shape = TargetingShape.RadialSquare;
        [SerializeField] private TargetingChoiceMode choiceMode = TargetingChoiceMode.Single;
        [SerializeField] private TargetingSplash splash = TargetingSplash.None;
        [Tooltip("Splash가 None이면 0, 그 외에는 1 이상입니다. 100을 넘는 배율도 허용합니다.")]
        [SerializeField, Min(0)] private int splashPercent;

        internal int MethodKey => methodKey;

        public override IData GetData() => new TargetingMethodData(methodKey, shape, choiceMode, splash, splashPercent);
    }
}
