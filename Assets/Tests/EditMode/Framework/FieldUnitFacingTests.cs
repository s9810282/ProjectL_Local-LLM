using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>적 유닛 모델이 Core 회전보다 180도 더 돌아 아군 쪽을 보는지 확인합니다.</summary>
    public sealed class FieldUnitFacingTests
    {
        private GameObject _root;
        private FieldGridMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FieldRoot");
            _mapper = new FieldGridMapper(_root.transform, 3, 3, 1f, 1f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void EnemyFacesOpposite_PlayerFollowsCoreRotation()
        {
            Vector3 player = FieldUnitPresenter.ModelRotation(Owner.Player, Rotation4.R0, _mapper) * Vector3.forward;
            Vector3 enemy = FieldUnitPresenter.ModelRotation(Owner.Enemy, Rotation4.R0, _mapper) * Vector3.forward;

            Assert.That(Vector3.Distance(player, Vector3.forward), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(enemy, Vector3.back), Is.LessThan(1e-4f), "적 R0은 아군 쪽(-Z)을 본다.");
            Assert.That(Vector3.Distance(FieldUnitPresenter.ModelRotation(Owner.Enemy, Rotation4.R90, _mapper) * Vector3.forward,
                Vector3.left), Is.LessThan(1e-4f), "다른 회전에도 같은 180도 보정");
        }
    }
}
