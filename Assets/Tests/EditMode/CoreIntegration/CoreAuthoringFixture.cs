using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    // 테스트가 만든 입력만 소유합니다. 변환·검증·계산은 실제 SO와 Core 코드가 수행합니다.
    internal sealed class CoreAuthoringFixture : IDisposable
    {
        private readonly List<Object> _objects = new();

        internal T Create<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }

        internal T Clone<T>(T source) where T : ScriptableObject
        {
            var value = Object.Instantiate(source);
            _objects.Add(value);
            return value;
        }

        internal UnitDataSO CreateUnit(EntityKey key, CategoryKey category, params UnitTraitAuthoring[] traits)
            => CreateUnit(key, category, CreateTargetingMethod(), traits);

        internal TargetingMethodDataSO CreateTargetingMethod(int key = 1)
        {
            var method = Create<TargetingMethodDataSO>();
            Set(method, "methodKey", key);
            Set(method, "shape", TargetingShape.RadialSquare);
            Set(method, "choiceMode", TargetingChoiceMode.Single);
            Set(method, "splash", TargetingSplash.None);
            Set(method, "splashPercent", 0);
            Set(method, "choiceAngle", TargetingChoiceAngle.Unrestricted);
            return method;
        }

        internal UnitDataSO CreateUnit(EntityKey key, CategoryKey category, TargetingMethodDataSO method,
            params UnitTraitAuthoring[] traits)
        {
            var unit = Create<UnitDataSO>();
            Set(unit, "key", key);
            Set(unit, "role", UnitRole.Regular);
            Set(unit, "ownerEligibility", UnitOwnerEligibility.PlayerOnly);
            Set(unit, "killRewardConfigured", true);
            Set(unit, "killRewardEligible", false);
            Set(unit, "killGold", 0);
            Set(unit, "killExperience", 0);
            Set(unit, "canMoveVoluntarily", true);
            Set(unit, "canBasicAttack", true);
            Set(unit, "isForcedMovementImmune", false);
            Set(unit, "footprintWidth", 1);
            Set(unit, "footprintHeight", 1);
            Set(unit, "maxHealth", 37);
            Set(unit, "physicalDefense", 4);
            Set(unit, "targetingMethod", method);
            Set(unit, "attackRange", 2);
            Set(unit, "category", category);
            Set(unit, "attackPower", 12);
            Set(unit, "wallDelivery", AttackWallDelivery.RequireClear);
            Set(unit, "wallPropagation", AttackWallPropagation.FromImpact);
            Set(unit, "priority", 7);
            Set(unit, "traits", new List<UnitTraitAuthoring>(traits));
            return unit;
        }

        internal static void Set(object target, string field, object value)
        {
            var property = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(target.GetType().FullName, field);
            property.SetValue(target, value);
        }

        public void Dispose()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }
    }
}
