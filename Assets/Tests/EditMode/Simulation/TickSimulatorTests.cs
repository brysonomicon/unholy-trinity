using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Simulation.Tests
{
    public class TickSimulatorTests
    {
        static readonly HexDirection East = HexDirection.D0;
        static readonly HexDirection NorthWest = HexDirection.D2;

        /// <summary>Adds steps on entering a hex, standing in for boosts and momentum.</summary>
        class ExtraStepsRule : ITileRule
        {
            readonly HexCoord at;
            readonly int steps;

            public ExtraStepsRule(HexCoord at, int steps)
            {
                this.at = at;
                this.steps = steps;
            }

            public void OnTickStarted(SimState state, TickLog log) { }
            public void OnEnter(SimState state, UnitState unit, UnitMove move, TickLog log)
            {
                if (unit.Coord == at)
                    move.StepsRemaining += steps;
            }
            public void OnStop(SimState state, UnitState unit, TickLog log) { }
        }

        /// <summary>Damages on entering a hex, standing in for harm pools.</summary>
        class DamageRule : ITileRule
        {
            readonly HexCoord at;
            readonly int damage;

            public DamageRule(HexCoord at, int damage)
            {
                this.at = at;
                this.damage = damage;
            }

            public void OnTickStarted(SimState state, TickLog log) { }
            public void OnEnter(SimState state, UnitState unit, UnitMove move, TickLog log)
            {
                if (unit.Coord == at)
                    unit.TakeDamage(damage);
            }
            public void OnStop(SimState state, UnitState unit, TickLog log) { }
        }

        HealPickup heal;

        [SetUp]
        public void SetUp()
        {
            heal = ScriptableObject.CreateInstance<HealPickup>();
            heal.name = "Heal";
            heal.Amount = 3;
            heal.Configure(respawnTicks: 1, PickupTrigger.OnStop);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(heal);

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        static UnitState Unit(int id, HexCoord coord, int health = 5) =>
            new UnitState(id, coord, HexVelocity.Zero, health, maxHealth: 10, maxArmour: 0);

        static UnitTickInput Input(int id, HexDirection? direction = null) => new UnitTickInput(id, new MoveInput(direction));

        /// <summary>A straight east-west corridor of floor from q = 0 to q = length - 1.</summary>
        static HexMap Corridor(int length)
        {
            var map = new HexMap();
            for (int q = 0; q < length; q++)
                map.SetTile(HexTile.Floor(At(q)));
            return map;
        }

        static ITileRule[] Rules(params ITileRule[] extra) => extra.Append(new PickupRule()).ToArray();

        [Test]
        public void PlannedDirection_MovesOneHex_AndRecordsVelocity()
        {
            var state = new SimState(Corridor(3), new[] { Unit(0, At(0)) });

            var log = TickSimulator.Simulate(state, new[] { Input(0, East) });

            Assert.AreEqual(At(1), state.GetUnit(0).Coord);
            Assert.AreEqual(new HexVelocity(East, 1), state.GetUnit(0).Velocity);
            Assert.IsInstanceOf<MovedEvent>(log.Events.Single());
        }

        [Test]
        public void Wall_BlocksMove_ButKeepsPlannedMomentum()
        {
            var state = new SimState(Corridor(1), new[] { Unit(0, At(0)) });

            var log = TickSimulator.Simulate(state, new[] { Input(0, East) });

            Assert.AreEqual(At(0), state.GetUnit(0).Coord);
            Assert.AreEqual(new HexVelocity(East, 1), state.GetUnit(0).Velocity);
            Assert.IsInstanceOf<BlockedEvent>(log.Events.Single());
        }

        [Test]
        public void UnitsMovingIntoSameHex_PriorityGetsIt()
        {
            var map = Corridor(3);
            var state = new SimState(map, new[] { Unit(0, At(0)), Unit(1, At(2)) });

            TickSimulator.Simulate(state, new[] { Input(1, East.Opposite()), Input(0, East) });

            Assert.AreEqual(At(1), state.GetUnit(1).Coord);
            Assert.AreEqual(At(0), state.GetUnit(0).Coord);
        }

        [Test]
        public void StandingStill_CollectsOnStopPickup()
        {
            var map = Corridor(2);
            map.SetTile(HexTile.PickupPoint(At(1), heal));
            var state = new SimState(map, new[] { Unit(0, At(1)) }, new PickupField(map));

            var log = TickSimulator.Simulate(state, new[] { Input(0) });

            Assert.AreEqual(8, state.GetUnit(0).Health);
            Assert.IsTrue(log.Events.OfType<PickupCollectedEvent>().Any(e => e.Pickup == heal));
        }

        [Test]
        public void RespawnsBeforeMoving_SoPickupIsCollectibleTheNextTick()
        {
            var map = Corridor(2);
            map.SetTile(HexTile.PickupPoint(At(1), heal));
            var state = new SimState(map, new[] { Unit(0, At(1), health: 1) }, new PickupField(map));

            TickSimulator.Simulate(state, new[] { Input(0) });
            var log = TickSimulator.Simulate(state, new[] { Input(0) });

            Assert.AreEqual(7, state.GetUnit(0).Health);
            CollectionAssert.AreEqual(
                new[] { typeof(PickupRespawnedEvent), typeof(PickupCollectedEvent) },
                log.Events.Select(e => e.GetType()));
        }

        [Test]
        public void ContestedPickup_EarlierStepWins_OverPriority()
        {
            heal.Configure(respawnTicks: 4, PickupTrigger.OnEnter);
            var map = Corridor(5);
            map.SetTile(HexTile.PickupPoint(At(3), heal));
            map.SetTile(HexTile.Floor(At(3, 1)));
            var state = new SimState(map, new[] { Unit(0, At(0)), Unit(1, At(3, 1)) }, new PickupField(map));

            // Unit 0 has priority but reaches (3, 0) on step 3; unit 1 gets there on step 1.
            TickSimulator.Simulate(state, new[] { Input(0, East), Input(1, NorthWest) }, Rules(new ExtraStepsRule(At(1), 2)));

            Assert.AreEqual(8, state.GetUnit(1).Health);
            Assert.AreEqual(5, state.GetUnit(0).Health);
        }

        [Test]
        public void DeathMidMove_StopsUnitWhereItDied()
        {
            var state = new SimState(Corridor(5), new[] { Unit(0, At(0)) });
            var rules = Rules(new ExtraStepsRule(At(1), 3), new DamageRule(At(2), 99));

            var log = TickSimulator.Simulate(state, new[] { Input(0, East) }, rules);

            var unit = state.GetUnit(0);
            Assert.IsTrue(unit.IsDead);
            Assert.AreEqual(At(2), unit.Coord);
            Assert.AreEqual(HexVelocity.Zero, unit.Velocity);
            Assert.IsInstanceOf<DiedEvent>(log.Events.Last());
        }

        [Test]
        public void Preview_IgnoresOtherUnits_AndLeavesRealStateUntouched()
        {
            var map = Corridor(4);
            map.SetTile(HexTile.PickupPoint(At(2), heal));
            var pickups = new PickupField(map);
            var state = new SimState(map, new[] { Unit(0, At(0)), Unit(1, At(1)) }, pickups);
            var plan = new List<MoveInput> { new MoveInput(East), new MoveInput(East) };

            var preview = TickSimulator.PreviewPlan(state, unitId: 0, plan);

            Assert.AreEqual(At(1), preview[0].Unit.Coord, "The other unit is left out, so it doesn't block.");
            Assert.AreEqual(At(2), preview[1].Unit.Coord);
            Assert.AreEqual(8, preview[1].Unit.Health, "Collects the OnStop pickup at the end of tick 2.");

            Assert.AreEqual(At(0), state.GetUnit(0).Coord);
            Assert.AreEqual(5, state.GetUnit(0).Health);
            Assert.IsTrue(pickups.IsAvailable(At(2)));
        }
    }
}
