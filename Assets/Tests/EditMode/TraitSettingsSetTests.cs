using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>특성 설정 묶음의 순서·조회·비교 계약(설명 수치 공개 설계 SP1)과 표시 순서·출처(특성 표시 출처 설계 TO4)를 확인한다.</summary>
    public sealed class TraitSettingsSetTests
    {
        private static (TraitSpec, TraitOrigin)[] Unit(params TraitSpec[] specs) =>
            specs.Select(spec => (spec, TraitOrigin.Unit)).ToArray();

        [Test]
        public void Traits_AreSortedByTraitKey()
        {
            var set = new TraitSettingsSet(Unit(new TrackingShotTraitSpec(50, 30), new StationaryFireTraitSpec(20)));

            Assert.That(set.Traits.Select(t => t.TraitKey),
                Is.EqualTo(new[] { TraitKey.StationaryFire, TraitKey.TrackingShot }));
        }

        [Test]
        public void TryGet_FindsByKeyAndByType()
        {
            var fire = new StationaryFireTraitSpec(20);
            var set = new TraitSettingsSet(Unit(fire));

            Assert.That(set.TryGet(TraitKey.StationaryFire, out var byKey), Is.True);
            Assert.That(byKey, Is.SameAs(fire));
            Assert.That(set.TryGet<StationaryFireTraitSpec>(out var byType), Is.True);
            Assert.That(byType.ExtraDamagePercent, Is.EqualTo(20));
            Assert.That(set.TryGet(TraitKey.TrackingShot, out var missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(set.TryGet<TrackingShotTraitSpec>(out var missingType), Is.False);
            Assert.That(missingType, Is.Null);
        }

        [Test]
        public void SameValues_ComparesEverySettingInOrder()
        {
            var left = new TraitSettingsSet(Unit(new StationaryFireTraitSpec(20), new TrackingShotTraitSpec(50, 30)));
            var same = new TraitSettingsSet(Unit(new TrackingShotTraitSpec(50, 30), new StationaryFireTraitSpec(20)));
            var changed = new TraitSettingsSet(Unit(new StationaryFireTraitSpec(25), new TrackingShotTraitSpec(50, 30)));

            Assert.That(left.SameValues(same), Is.True);
            Assert.That(left.SameValues(changed), Is.False);
            Assert.That(left.SameValues(TraitSettingsSet.Empty), Is.False);
            Assert.That(left.SameValues(null), Is.False);
            Assert.That(TraitSettingsSet.Empty.Traits, Is.Empty);
            Assert.That(TraitSettingsSet.Empty.Display, Is.Empty);
        }

        [Test]
        public void Constructor_RejectsNullAndDuplicateKeys()
        {
            Assert.Throws<ArgumentNullException>(() => new TraitSettingsSet(null));
            Assert.Throws<ArgumentException>(() => new TraitSettingsSet(Unit((TraitSpec)null)));
            Assert.Throws<ArgumentException>(() => new TraitSettingsSet(Unit(
                new StationaryFireTraitSpec(20), new StationaryFireTraitSpec(30))));
        }

        [Test]
        public void Display_KeepsGivenOrderAndOrigin_WhileTraitsStaySortedByKey()
        {
            var set = new TraitSettingsSet(new (TraitSpec, TraitOrigin)[]
            {
                (new TrackingShotTraitSpec(50, 30), TraitOrigin.Unit),
                (new StationaryFireTraitSpec(20), TraitOrigin.Growth),
            });

            Assert.That(set.Display.Select(e => (e.Key, e.Origin)), Is.EqualTo(new[]
            {
                (TraitKey.TrackingShot, TraitOrigin.Unit),
                (TraitKey.StationaryFire, TraitOrigin.Growth),
            }));
            Assert.That(set.Traits.Select(t => t.TraitKey),
                Is.EqualTo(new[] { TraitKey.StationaryFire, TraitKey.TrackingShot }));
        }

        [Test]
        public void Constructor_RejectsNoneOriginAndSecondCategoryTrait()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitSettingsSet(new (TraitSpec, TraitOrigin)[]
            {
                (new StationaryFireTraitSpec(20), TraitOrigin.None),
            }));
            Assert.Throws<ArgumentException>(() => new TraitSettingsSet(new (TraitSpec, TraitOrigin)[]
            {
                (new StationaryFireTraitSpec(20), TraitOrigin.Category),
                (new TrackingShotTraitSpec(50, 30), TraitOrigin.Category),
            }));
        }

        [Test]
        public void SameValues_IgnoresDisplayOrderAndOrigin()
        {
            var left = new TraitSettingsSet(new (TraitSpec, TraitOrigin)[]
            {
                (new StationaryFireTraitSpec(20), TraitOrigin.Unit),
                (new TrackingShotTraitSpec(50, 30), TraitOrigin.Growth),
            });
            var reordered = new TraitSettingsSet(new (TraitSpec, TraitOrigin)[]
            {
                (new TrackingShotTraitSpec(50, 30), TraitOrigin.Category),
                (new StationaryFireTraitSpec(20), TraitOrigin.Unit),
            });

            Assert.That(left.SameValues(reordered), Is.True);
        }
    }
}
