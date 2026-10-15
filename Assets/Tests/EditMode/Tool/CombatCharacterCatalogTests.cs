using System;
using NUnit.Framework;

namespace ProjectL.Tool.Editor.Tests
{
    public sealed class CombatCharacterCatalogTests
    {
        private static CombatLayoutDocument CreateDocument()
        {
            var document = CombatLayoutDocument.Create();
            foreach (var entry in new[] { (101, 1), (202, 2), (303, 0) })
            {
                var row = new CombatReferenceRow();
                document.characters.rows.Add(row);
                document.characters.Set(row, "Character_Key", entry.Item1.ToString());
                document.characters.Set(row, "Character_PositionType", entry.Item2.ToString());
            }
            return document;
        }

        [Test]
        public void Placement_UsesCurrentDtFactionForBothAlliedAndEnemyUnits()
        {
            var document = CreateDocument();
            var allied = document.PlaceCharacter(101, 0, 0);
            var enemy = document.PlaceCharacter(202, 1, 0);
            var anotherAllied = document.PlaceCharacter(101, 2, 0);
            Assert.AreEqual("Player", allied.owner);
            Assert.AreEqual("Enemy", enemy.owner);
            Assert.AreEqual("Player", anotherAllied.owner);
            Assert.AreNotEqual(allied.key, anotherAllied.key);
            Assert.DoesNotThrow(() => CombatCharacterCatalog.ValidateUnits(document));
            Assert.DoesNotThrow(document.Validate);
        }

        [Test]
        public void Placement_RejectsUnregisteredOrUnspecifiedFactionWithoutAllocatingAKey()
        {
            var document = CreateDocument();
            Assert.Throws<InvalidOperationException>(() => document.PlaceCharacter(999, 0, 0));
            Assert.Throws<InvalidOperationException>(() => document.PlaceCharacter(303, 0, 0));
            Assert.IsEmpty(document.units);
            Assert.AreEqual(1, document.nextUnitKey);
        }

        [Test]
        public void Placement_RejectsBlockedOccupiedAndOutOfBoundsCells()
        {
            var document = CreateDocument();
            document.PlaceCharacter(101, 0, 0);
            document.Cell(1, 0).type = "Blocked";
            Assert.Throws<InvalidOperationException>(() => document.PlaceCharacter(202, 0, 0));
            Assert.Throws<InvalidOperationException>(() => document.PlaceCharacter(202, 1, 0));
            Assert.Throws<InvalidOperationException>(() => document.PlaceCharacter(202, 40, 0));
            Assert.AreEqual(1, document.units.Count);
            Assert.AreEqual(2, document.nextUnitKey);
        }

        [Test]
        public void DtRefresh_UpdatesPlacedFactionWithoutChangingIdentityOrPosition()
        {
            var document = CreateDocument();
            var unit = document.PlaceCharacter(101, 2, 3);
            int key = unit.key;
            var row = document.characters.Find("Character_Key", 101);
            document.characters.Set(row, "Character_PositionType", "2");
            Assert.Throws<InvalidOperationException>(() => CombatCharacterCatalog.ValidateUnits(document));
            Assert.AreEqual(1, CombatCharacterCatalog.RefreshUnitOwners(document));
            Assert.AreEqual("Enemy", unit.owner);
            Assert.AreEqual(key, unit.key);
            Assert.AreEqual(2, unit.x);
            Assert.AreEqual(3, unit.y);
            Assert.DoesNotThrow(() => CombatCharacterCatalog.ValidateUnits(document));
        }

        [Test]
        public void ExportValidation_RejectsMissingCurrentDtAndUnspecifiedFaction()
        {
            var document = CreateDocument();
            document.PlaceCharacter(101, 0, 0);
            var row = document.characters.Find("Character_Key", 101);
            document.characters.Set(row, "Character_PositionType", "0");
            Assert.Throws<InvalidOperationException>(() => CombatCharacterCatalog.ValidateUnits(document));
            document.characters.rows.Remove(row);
            Assert.Throws<InvalidOperationException>(() => CombatCharacterCatalog.ValidateUnits(document));
        }
    }
}
