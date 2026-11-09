using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation.Tests
{
    public class HealthTileRuleTests
    {
        static readonly HexDirection East = HexDirection.D0;
        static readonly ArmourProtection HalfArmour = new ArmourProtection(50);
        static readonly SimSettings ArmourApplies = new SimSettings(EnvironmentalDamage.None);
        static readonly SimSettings ArmourIgnored = new SimSettings(EnvironmentalDamage.HarmTile);

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        static UnitState Unit(HexCoord coord, int health = 50, int armour = 0) =>
            new UnitState(0, coord, HexVelocity.Zero, health, maxHealth: 100, maxArmour: 100, armour, HalfArmour);

        static UnitTickInput[] Move(HexDirection? direction = null) => new[] { new UnitTickInput(0, new MoveInput(direction)) };

        /// <summary>A straight east-west corridor of floor from q = 0 to q = 2, with <paramref name="pool"/> at q = 1.</summary>
        static SimState StateWithPool(HealthTileSettings pool, UnitState unit, SimSettings settings = default)
        {
            var map = new HexMap();
            for (int q = 0; q < 3; q++)
                map.SetTile(HexTile.Floor(At(q)));
            map.SetTile(HexTile.HealthTile(At(1), pool));
            return new SimState(map, new[] { unit }, settings: settings);
        }

        static IEnumerable<System.Type> Types(TickLog log) => log.Events.Select(e => e.GetType());

        [Test]
        public void OnEnterHarm_FiresWhenEntered_NotWhileStandingInIt()
        {
            var state = StateWithPool(HealthTileSettings.Harm(10), Unit(At(0)));

            var entered = TickSimulator.Simulate(state, Move(East));
            var stood = TickSimulator.Simulate(state, Move());

            Assert.AreEqual(40, state.GetUnit(0).Health);
            Assert.AreEqual(10, entered.Events.OfType<DamagedEvent>().Single().HealthLost);
            CollectionAssert.DoesNotContain(Types(stood), typeof(DamagedEvent));
        }

        [Test]
        public void OnStopHeal_FiresEveryTickYouStandInIt()
        {
            var state = StateWithPool(HealthTileSettings.Heal(5, HealthTrigger.OnStop), Unit(At(1)));

            TickSimulator.Simulate(state, Move());
            TickSimulator.Simulate(state, Move());

            Assert.AreEqual(60, state.GetUnit(0).Health);
        }

        [Test]
        public void Heal_AtFullHealth_LogsNothing()
        {
            var state = StateWithPool(HealthTileSettings.Heal(5, HealthTrigger.OnStop), Unit(At(1), health: 100));

            var log = TickSimulator.Simulate(state, Move());

            CollectionAssert.DoesNotContain(Types(log), typeof(HealedEvent));
        }

        [Test]
        public void Harm_IsAbsorbedByArmour_UnlessHarmTilesIgnoreArmour()
        {
            var absorbed = StateWithPool(HealthTileSettings.Harm(10), Unit(At(0), armour: 20), ArmourApplies);
            var ignored = StateWithPool(HealthTileSettings.Harm(10), Unit(At(0), armour: 20), ArmourIgnored);

            TickSimulator.Simulate(absorbed, Move(East));
            TickSimulator.Simulate(ignored, Move(East));

            Assert.AreEqual((45, 15), (absorbed.GetUnit(0).Health, absorbed.GetUnit(0).Armour));
            Assert.AreEqual((40, 20), (ignored.GetUnit(0).Health, ignored.GetUnit(0).Armour));
        }

        [Test]
        public void LethalTile_KillsThroughArmour_AndEndsTheMoveThere()
        {
            var state = StateWithPool(HealthTileSettings.Kill(), Unit(At(0), armour: 100));

            var log = TickSimulator.Simulate(state, Move(East));

            var unit = state.GetUnit(0);
            Assert.IsTrue(unit.IsDead);
            Assert.AreEqual(At(1), unit.Coord);
            Assert.AreEqual(HexVelocity.Zero, unit.Velocity);
            Assert.IsInstanceOf<DiedEvent>(log.Events.Last());
        }

        [Test]
        public void Preview_ShowsPoolDamage_UnderTheSameSettings_WithoutTouchingRealState()
        {
            var state = StateWithPool(HealthTileSettings.Harm(10), Unit(At(0), armour: 20), ArmourIgnored);

            var preview = TickSimulator.PreviewPlan(state, unitId: 0, new[] { new MoveInput(East) });

            Assert.AreEqual((40, 20), (preview[0].Unit.Health, preview[0].Unit.Armour));
            Assert.AreEqual(50, state.GetUnit(0).Health);
        }

        [TestCase(10, 100, true, 5)]
        [TestCase(3, 100, true, 2)]
        [TestCase(3, 100, false, 1)]
        [TestCase(1, 100, true, 1)]
        [TestCase(1, 100, false, 0)]
        [TestCase(10, 4, true, 4)]
        [TestCase(10, 0, true, 0)]
        public void ArmourAbsorbs_HalfOfEachHit_CappedByArmourLeft(int damage, int armour, bool roundUp, int expected)
        {
            Assert.AreEqual(expected, new ArmourProtection(50, roundUp).Absorbed(damage, armour));
        }
    }
}
