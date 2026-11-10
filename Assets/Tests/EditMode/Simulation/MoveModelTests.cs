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

        // base 2, aimed max 5, gain 1
        static readonly SimSettings Speeds = new SimSettings(EnvironmentalDamage.None, baseSpeed: 2, aimedMaxSpeed: 5);
        static readonly SimSettings SpeedsDiagonalsCount =
            new SimSettings(EnvironmentalDamage.None, baseSpeed: 2, aimedMaxSpeed: 5, betweenAimsAccelerate: true);
        static readonly SimSettings SpeedsGainTwo =
            new SimSettings(EnvironmentalDamage.None, baseSpeed: 2, aimedMaxSpeed: 5, aimedGain: 2);

        static readonly Facing AimEast = new Facing(East);
        static readonly Facing AimEastNorthEast = new Facing(East, isBetweenAxes: true);
        static readonly Facing AimNorthEast = new Facing(NorthEast);
        static readonly Facing AimWest = new Facing(West);

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        /// <summary>A parallelogram of floor, q and r from -size to size, so turns have room.</summary>
        static HexMap Open(int size)
        {
            var map = new HexMap();
            for (int q = -size; q <= size; q++)
                for (int r = -size; r <= size; r++)
                    map.SetTile(HexTile.Floor(At(q, r)));
            return map;
        }

        /// <summary>An east-west corridor of floor from q = -length to q = length.</summary>
        static HexMap Corridor(int length)
        {
            var map = new HexMap();
            for (int q = -length; q <= length; q++)
                map.SetTile(HexTile.Floor(At(q)));
            return map;
        }

        static SimState State(SimSettings settings, HexVelocity velocity = default, HexMap map = null) =>
            new SimState(map ?? Open(30),
                new[] { new UnitState(0, At(0), velocity, 10, maxHealth: 10, maxArmour: 0) },
                settings: settings);

        static UnitState Tick(SimState state, HexDirection? direction, Facing? facing = null) =>
            Tick(state, new MoveInput(direction, facing));

        static UnitState Tick(SimState state, MoveInput input)
        {
            TickSimulator.Simulate(state, new[] { new UnitTickInput(0, input) });
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
        public void FromStop_StartsAtBase_EvenWhenAimed()
        {
            var state = State(Speeds);

            var unit = Tick(state, East, AimEast);

            Assert.AreEqual(At(2), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 2), unit.Velocity);
        }

        [Test]
        public void Aimed_SameDirection_AddsGainPerTick_UpToAimedMax()
        {
            var state = State(Speeds);

            Assert.AreEqual(2, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(3, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(4, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(5, Tick(state, East, AimEast).Velocity.Speed);
            var unit = Tick(state, East, AimEast);

            Assert.AreEqual(5, unit.Velocity.Speed, "Capped at the aimed max.");
            Assert.AreEqual(At(2 + 3 + 4 + 5 + 5), unit.Coord);
        }

        [Test]
        public void Aimed_UsesConfiguredGain()
        {
            var state = State(SpeedsGainTwo, new HexVelocity(East, 2));

            Assert.AreEqual(4, Tick(state, East, AimEast).Velocity.Speed);
            Assert.AreEqual(5, Tick(state, East, AimEast).Velocity.Speed, "Gain still caps at the aimed max.");
        }

        [Test]
        public void Unaimed_SameDirection_KeepsSpeed()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            var unit = Tick(state, East, AimNorthEast);

            Assert.AreEqual(At(4), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 4), unit.Velocity);
        }

        [Test]
        public void Aimed_Turn_CarriesSpeedPlusGain_IntoNewDirection()
        {
            var state = State(Speeds, new HexVelocity(East, 3));

            var unit = Tick(state, NorthEast, AimNorthEast);

            Assert.AreEqual(new HexVelocity(NorthEast, 4), unit.Velocity);
            Assert.AreEqual(At(0) + NorthEast.ToOffset() * 4, unit.Coord);
        }

        [Test]
        public void Unaimed_Turn_DropsToBase()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            Assert.AreEqual(new HexVelocity(NorthEast, 2), Tick(state, NorthEast, AimEast).Velocity);
        }

        [Test]
        public void Reverse_MovesAtBase_EvenWhenAimed()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            var unit = Tick(state, West, AimWest);

            Assert.AreEqual(At(-2), unit.Coord);
            Assert.AreEqual(new HexVelocity(West, 2), unit.Velocity);
        }

        [Test]
        public void Hold_StopsWithoutMoving()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            var unit = Tick(state, null, AimEast);

            Assert.AreEqual(At(0), unit.Coord);
            Assert.AreEqual(HexVelocity.Zero, unit.Velocity);
        }

        [Test]
        public void Unplanned_DoesNotMove_AndKeepsMomentum()
        {
            var state = State(Speeds, new HexVelocity(East, 4));

            var unit = Tick(state, MoveInput.None);

            Assert.AreEqual(At(0), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 4), unit.Velocity);
        }

        [Test]
        public void Wall_StopsTheMove_ButKeepsPlannedMomentum()
        {
            var state = State(Speeds, new HexVelocity(East, 4), Corridor(2));

            var unit = Tick(state, East, AimEast);

            Assert.AreEqual(At(2), unit.Coord);
            Assert.AreEqual(new HexVelocity(East, 5), unit.Velocity);
        }

        [Test]
        public void DiagonalAim_NeverAccelerates_ByDefault()
        {
            var state = State(Speeds, new HexVelocity(East, 3));

            Assert.AreEqual(3, Tick(state, East, AimEastNorthEast).Velocity.Speed, "Same direction keeps speed.");
        }

        [Test]
        public void DiagonalAim_Accelerates_WhenTheToggleIsOn()
        {
            var state = State(SpeedsDiagonalsCount, new HexVelocity(East, 3));

            Assert.AreEqual(4, Tick(state, East, AimEastNorthEast).Velocity.Speed);
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
            var state = State(Speeds, new HexVelocity(East, 3));

            Assert.AreEqual(3, Tick(state, East).Velocity.Speed);
        }
    }
}
