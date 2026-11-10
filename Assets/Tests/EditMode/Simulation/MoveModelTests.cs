using NUnit.Framework;
using UnholyTrinity.Hex;
using UnholyTrinity.Simulation;

namespace UnholyTrinity.Simulation.Tests
{
    public class MoveModelTests
    {
        const HexDirection East = HexDirection.D0;
        const HexDirection NorthEast = HexDirection.D1;
        const HexDirection West = HexDirection.D3;

        // base 2, aimed max 4
        static readonly SimSettings Speeds = new SimSettings(EnvironmentalDamage.None, baseSpeed: 2, aimedMaxSpeed: 4);
        static readonly SimSettings SpeedsDiagonalsCount =
            new SimSettings(EnvironmentalDamage.None, baseSpeed: 2, aimedMaxSpeed: 4, betweenAimsAccelerate: true);

        static readonly Facing AimEast = new Facing(East);
        static readonly Facing AimEastNorthEast = new Facing(East, isBetweenAxes: true);
        static readonly Facing AimNorthEast = new Facing(NorthEast);

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        /// <summary>An east-west corridor of floor from q = -length to q = length.</summary>
        static HexMap Corridor(int length)
        {
            var map = new HexMap();
            for (int q = -length; q <= length; q++)
                map.SetTile(HexTile.Floor(At(q)));
            return map;
        }

        static SimState State(SimSettings settings, HexVelocity velocity = default, int corridor = 30) =>
            new SimState(Corridor(corridor), new[] { new UnitState(0, At(0), velocity, 10, maxHealth: 10, maxArmour: 0) },
                settings: settings);

        static UnitState Tick(SimState state, HexDirection? direction, Facing? facing = null)
        {
            TickSimulator.Simulate(state, new[] { new UnitTickInput(0, new MoveInput(direction, facing)) });
            return state.GetUnit(0);
        }

        [Test]
        public void DefaultSettings_MoveOneHexPerTick()
        {
            var state = State(default);

            Assert.AreEqual(At(1), Tick(state, East, AimEast).Coord);
            Assert.AreEqual(At(2), Tick(state, East, AimEast).Coord);
        }

        [Test]
        public void UnaimedMove_GoesAtBaseSpeed_AndDoesNotBuild()
        {
            var state = State(Speeds);

            Tick(state, East, AimNorthEast);
            var unit = Tick(state, East, AimNorthEast);

            Assert.AreEqual(At(4), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 2), unit.Velocity);
        }

        [Test]
        public void AimedMove_StartsAtBase_ThenAddsOnePerTick_UpToAimedMax()
        {
            var state = State(Speeds);

            Assert.AreEqual(2, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(3, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(4, Tick(state, East, AimEast).Velocity.Speed);
            var unit = Tick(state, East, AimEast);

            Assert.AreEqual(4, unit.Velocity.Speed, "Capped at the aimed max.");
            Assert.AreEqual(At(2 + 3 + 4 + 4), unit.Coord);
        }

        [Test]
        public void LookingAway_DropsBackToBaseSpeed()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            Assert.AreEqual(new HexVelocity(East, 2), Tick(state, East, AimNorthEast).Velocity);
        }

        [Test]
        public void Hold_KeepsSlidingAtCurrentVelocity()
        {
            var state = State(Speeds, new HexVelocity(East, 3));

            var unit = Tick(state, null, AimNorthEast);

            Assert.AreEqual(At(3), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 3), unit.Velocity);
        }

        [Test]
        public void Opposite_BrakesToAStop_WithoutMoving()
        {
            var state = State(Speeds, new HexVelocity(East, 3));

            var unit = Tick(state, West, new Facing(West));

            Assert.AreEqual(At(0), unit.Coord);
            Assert.IsFalse(unit.Velocity.IsMoving);
        }

        [Test]
        public void Wall_StopsMomentum_SoTheNextHoldStaysPut()
        {
            var state = State(Speeds, new HexVelocity(East, 4), corridor: 2);

            Tick(state, null);
            var unit = Tick(state, null);

            Assert.AreEqual(At(2), unit.Coord);
            Assert.AreEqual(HexVelocity.Zero, unit.Velocity);
        }

        [Test]
        public void DiagonalAim_OnlyAccelerates_WhenTheToggleIsOn()
        {
            var off = State(Speeds, new HexVelocity(East, 2));
            var on = State(SpeedsDiagonalsCount, new HexVelocity(East, 2));

            Assert.AreEqual(2, Tick(off, East, AimEastNorthEast).Velocity.Speed);
            Assert.AreEqual(3, Tick(on, East, AimEastNorthEast).Velocity.Speed);
        }

        [Test]
        public void DiagonalAim_WithToggleOn_LinesUpWithBothNeighbours_ButNothingElse()
        {
            Assert.IsTrue(AimEastNorthEast.LinesUpWith(East, betweenCountsForBoth: true));
            Assert.IsTrue(AimEastNorthEast.LinesUpWith(NorthEast, betweenCountsForBoth: true));
            Assert.IsFalse(AimEastNorthEast.LinesUpWith(HexDirection.D2, betweenCountsForBoth: true));
            Assert.IsFalse(AimEastNorthEast.LinesUpWith(HexDirection.D5, betweenCountsForBoth: true));
        }

        [Test]
        public void NoFacing_NeverAccelerates()
        {
            var state = State(Speeds, new HexVelocity(East, 2));

            Assert.AreEqual(2, Tick(state, East).Velocity.Speed);
        }
    }
}
