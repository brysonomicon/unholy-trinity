using NUnit.Framework;
using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Hex.Tests
{
    public class PickupFieldTests
    {
        class TestUnit : IHealable
        {
            public int Health;
            public int MaxHealth = 10;

            public bool TryHeal(int amount)
            {
                if (Health >= MaxHealth)
                    return false;
                Health = Mathf.Min(Health + amount, MaxHealth);
                return true;
            }
        }

        class NotHealable : IPickupCollector
        {
        }

        HealPickup heal;

        [SetUp]
        public void SetUp()
        {
            heal = ScriptableObject.CreateInstance<HealPickup>();
            heal.Amount = 3;
            heal.Configure(respawnTicks: 4, PickupTrigger.OnStop);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(heal);

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        /// <summary>A straight east-west corridor of floor from q = 0 to q = length - 1, with a pickup at <paramref name="pickupAt"/>.</summary>
        static HexMap CorridorWithPickup(int length, HexCoord pickupAt, PickupDefinition pickup)
        {
            var map = new HexMap();
            for (int q = 0; q < length; q++)
                map.SetTile(HexTile.Floor(At(q)));
            map.SetTile(HexTile.PickupPoint(pickupAt, pickup));
            return map;
        }

        [Test]
        public void Collecting_HealsCollector_AndMakesPointUnavailable()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));
            var unit = new TestUnit { Health = 5 };

            Assert.IsTrue(field.TryCollect(At(1), PickupTrigger.OnStop, unit));
            Assert.AreEqual(8, unit.Health);
            Assert.IsFalse(field.IsAvailable(At(1)));
            Assert.IsFalse(field.TryCollect(At(1), PickupTrigger.OnStop, unit));
        }

        [Test]
        public void Respawns_AfterExactlyRespawnTicks()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));
            field.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit());

            CollectionAssert.IsEmpty(field.AdvanceTicks(3));
            Assert.IsFalse(field.IsAvailable(At(1)));

            CollectionAssert.AreEqual(new[] { At(1) }, field.AdvanceTicks(1));
            Assert.IsTrue(field.IsAvailable(At(1)));

            CollectionAssert.IsEmpty(field.AdvanceTicks(5), "An available pickup should not respawn again.");
        }

        [Test]
        public void Clone_IsIndependentOfOriginal()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));
            var clone = field.Clone();

            Assert.IsTrue(clone.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit()));
            Assert.IsFalse(clone.IsAvailable(At(1)));
            Assert.IsTrue(field.IsAvailable(At(1)));
        }

        [Test]
        public void CollectorAtFullHealth_LeavesPickup()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));

            Assert.IsFalse(field.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit { Health = 10 }));
            Assert.IsTrue(field.IsAvailable(At(1)));
        }

        [Test]
        public void CollectorWithoutCapability_LeavesPickup()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));

            Assert.IsFalse(field.TryCollect(At(1), PickupTrigger.OnStop, new NotHealable()));
            Assert.IsTrue(field.IsAvailable(At(1)));
        }

        [Test]
        public void OnStopPickup_IgnoresOnEnter()
        {
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));
            var unit = new TestUnit();

            Assert.IsFalse(field.TryCollect(At(1), PickupTrigger.OnEnter, unit));
            Assert.IsTrue(field.IsAvailable(At(1)));

            Assert.IsTrue(field.TryCollect(At(1), PickupTrigger.OnStop, unit));
            Assert.IsFalse(field.IsAvailable(At(1)));
        }

        [Test]
        public void OnEnterPickup_IgnoresOnStop()
        {
            heal.Configure(respawnTicks: 4, PickupTrigger.OnEnter);
            var field = new PickupField(CorridorWithPickup(3, At(1), heal));
            var unit = new TestUnit();

            Assert.IsFalse(field.TryCollect(At(1), PickupTrigger.OnStop, unit));
            Assert.IsTrue(field.IsAvailable(At(1)));

            Assert.IsTrue(field.TryCollect(At(1), PickupTrigger.OnEnter, unit));
            Assert.IsFalse(field.IsAvailable(At(1)));
            Assert.AreEqual(3, unit.Health);
        }

        [Test]
        public void UnassignedPickupPoint_IsReported_AndSkipped()
        {
            var map = CorridorWithPickup(3, At(1), null);

            CollectionAssert.Contains(map.Validate(), $"Pickup point {At(1)} has no pickup assigned.");
            Assert.IsEmpty(new PickupField(map).Spawns);
        }
    }
}
