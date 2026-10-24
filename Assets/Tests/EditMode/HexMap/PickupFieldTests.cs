using NUnit.Framework;
using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Hex.Tests
{
    public class PickupFieldTests
    {
        static readonly HexDirection East = HexDirection.D0;

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
            int respawns = 0;
            field.Respawned += (_, _) => respawns++;
            field.TryCollect(At(1), PickupTrigger.OnStop, new TestUnit());

            field.AdvanceTicks(3);
            Assert.IsFalse(field.IsAvailable(At(1)));
            Assert.AreEqual(0, respawns);

            field.AdvanceTicks(1);
            Assert.IsTrue(field.IsAvailable(At(1)));
            Assert.AreEqual(1, respawns);

            field.AdvanceTicks(5);
            Assert.AreEqual(1, respawns, "An available pickup should not respawn again.");
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
        public void OnStopPickup_IgnoredWhenSlidOver_CollectedAtSlideEnd()
        {
            var map = CorridorWithPickup(6, At(2), heal);
            var field = new PickupField(map);
            var unit = new TestUnit();

            field.CollectAlong(MomentumStepper.Slide(map, At(0), new HexVelocity(East, 4)), unit);
            Assert.IsTrue(field.IsAvailable(At(2)));

            field.CollectAlong(MomentumStepper.Slide(map, At(0), new HexVelocity(East, 2)), unit);
            Assert.IsFalse(field.IsAvailable(At(2)));
        }

        [Test]
        public void OnEnterPickup_CollectedMidSlide()
        {
            heal.Configure(respawnTicks: 4, PickupTrigger.OnEnter);
            var map = CorridorWithPickup(6, At(2), heal);
            var field = new PickupField(map);
            var unit = new TestUnit();

            field.CollectAlong(MomentumStepper.Slide(map, At(0), new HexVelocity(East, 4)), unit);

            Assert.IsFalse(field.IsAvailable(At(2)));
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
