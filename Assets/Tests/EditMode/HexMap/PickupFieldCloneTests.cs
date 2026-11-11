using NUnit.Framework;
using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Hex.Tests
{
    // planning forecasts pickups on a clone of the live field, so the clone must never leak back
    public class PickupFieldCloneTests
    {
        class TestUnit : IHealable
        {
            public bool TryHeal(int amount) => true;
        }

        HealPickup heal;

        [SetUp]
        public void SetUp()
        {
            heal = ScriptableObject.CreateInstance<HealPickup>();
            heal.Amount = 3;
            heal.Configure(respawnTicks: 2, PickupTrigger.OnStop);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(heal);

        static HexCoord At(int q) => new HexCoord(q, 0);

        static PickupField FieldWithPickupAt(HexCoord coord, PickupDefinition pickup)
        {
            var map = new HexMap();
            map.SetTile(HexTile.Floor(At(0)));
            map.SetTile(HexTile.PickupPoint(coord, pickup));
            return new PickupField(map);
        }

        [Test]
        public void CollectingOnClone_LeavesOriginalAvailable()
        {
            var field = FieldWithPickupAt(At(1), heal);
            var clone = field.Clone();

            Assert.IsTrue(clone.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit()));

            Assert.IsFalse(clone.IsAvailable(At(1)));
            Assert.IsTrue(field.IsAvailable(At(1)));
        }

        [Test]
        public void Clone_CopiesRespawnTimer_AndCountsDownIndependently()
        {
            var field = FieldWithPickupAt(At(1), heal);
            field.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit());
            var clone = field.Clone();

            clone.AdvanceTicks(2);

            Assert.IsTrue(clone.IsAvailable(At(1)), "The clone keeps the timer it was copied with.");
            Assert.IsFalse(field.IsAvailable(At(1)), "Advancing the clone doesn't advance the original.");
        }

        [Test]
        public void Clone_DoesNotRaiseOriginalsEvents()
        {
            var field = FieldWithPickupAt(At(1), heal);
            int raised = 0;
            field.Collected += (_, _) => raised++;

            field.Clone().TryCollect(At(1), PickupTrigger.OnStop, new TestUnit());

            Assert.AreEqual(0, raised);
        }
    }
}
