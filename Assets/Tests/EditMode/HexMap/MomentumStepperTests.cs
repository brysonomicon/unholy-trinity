using System.Linq;
using NUnit.Framework;

namespace UnholyTrinity.Hex.Tests
{
    public class MomentumStepperTests
    {
        static readonly HexDirection East = HexDirection.D0;

        /// <summary>A straight east-west corridor of floor from q = 0 to q = length - 1, on r = 0.</summary>
        static HexMap Corridor(int length)
        {
            var map = new HexMap();
            for (int q = 0; q < length; q++)
                map.SetTile(HexTile.Floor(new HexCoord(q, 0)));
            return map;
        }

        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        [Test]
        public void Stationary_DoesNotMove()
        {
            var result = MomentumStepper.Slide(Corridor(5), At(0), HexVelocity.Zero);
            Assert.AreEqual(At(0), result.End);
            Assert.IsEmpty(result.Path);
            Assert.AreEqual(HexVelocity.Zero, result.FinalVelocity);
        }

        [Test]
        public void PlainSlide_MovesSpeedHexes_AndKeepsMomentum()
        {
            var result = MomentumStepper.Slide(Corridor(10), At(0), new HexVelocity(East, 3));
            Assert.AreEqual(At(3), result.End);
            CollectionAssert.AreEqual(new[] { At(1), At(2), At(3) }, result.Path);
            Assert.AreEqual(new HexVelocity(East, 3), result.FinalVelocity);
            Assert.IsEmpty(result.Events);
        }

        [Test]
        public void Wall_StopsBeforeIt_AndZeroesSpeed()
        {
            var map = Corridor(10);
            map.SetTile(HexTile.Wall(At(3)));
            var result = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 5));

            Assert.AreEqual(At(2), result.End);
            Assert.AreEqual(0, result.FinalVelocity.Speed);
            Assert.AreEqual(East, result.FinalVelocity.Direction);
            var collision = result.Events.Single();
            Assert.AreEqual(MoveEventType.Collided, collision.Type);
            Assert.AreEqual(At(3), collision.Other);
        }

        [Test]
        public void MapEdge_ActsLikeAWall()
        {
            var result = MomentumStepper.Slide(Corridor(3), At(0), new HexVelocity(East, 5));
            Assert.AreEqual(At(2), result.End);
            Assert.AreEqual(0, result.FinalVelocity.Speed);
            Assert.AreEqual(MoveEventType.Collided, result.Events.Single().Type);
        }

        [Test]
        public void AlongBoost_ExtendsSlide_AndCarriesSpeed()
        {
            var map = Corridor(10);
            map.SetTile(HexTile.BoostTile(At(1), BoostSettings.Along(2)));
            var result = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 2));

            Assert.AreEqual(At(4), result.End);
            Assert.AreEqual(new HexVelocity(East, 4), result.FinalVelocity);
            Assert.AreEqual(MoveEventType.Boosted, result.Events.Single().Type);
        }

        [Test]
        public void Friction_SlowsAndCanStopEarly()
        {
            var map = Corridor(10);
            map.SetTile(HexTile.BoostTile(At(1), BoostSettings.Along(-1)));
            var slowed = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 3));
            Assert.AreEqual(At(2), slowed.End);
            Assert.AreEqual(2, slowed.FinalVelocity.Speed);

            map.SetTile(HexTile.BoostTile(At(1), BoostSettings.Along(-5)));
            var stopped = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 3));
            Assert.AreEqual(At(1), stopped.End);
            Assert.AreEqual(0, stopped.FinalVelocity.Speed);
        }

        [Test]
        public void AlongBoost_DoesNotAffectStationaryVelocity()
        {
            Assert.AreEqual(HexVelocity.Zero, BoostSettings.Along(3).Apply(HexVelocity.Zero));
        }

        [Test]
        public void FixedBoost_RedirectsOntoItsDirection()
        {
            var map = Corridor(3);
            var northEast = HexDirection.D1;
            for (int i = 1; i <= 4; i++)
                map.SetTile(HexTile.Floor(At(2 + i, -i)));
            map.SetTile(HexTile.BoostTile(At(2), BoostSettings.Fixed(1, northEast)));

            var result = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 3));

            // 2 steps east to the tile, speed becomes 4, then 2 more steps NE.
            Assert.AreEqual(At(4, -2), result.End);
            Assert.AreEqual(new HexVelocity(northEast, 4), result.FinalVelocity);
        }

        [Test]
        public void FixedBoost_PushesStationaryUnits()
        {
            Assert.AreEqual(new HexVelocity(HexDirection.D3, 2), BoostSettings.Fixed(2, HexDirection.D3).Apply(HexVelocity.Zero));
        }

        [Test]
        public void Portal_TeleportsAndRotates_WithoutRetriggering()
        {
            var map = Corridor(3);
            map.SetTile(HexTile.PortalTile(At(2), At(0, 5), 1));
            map.SetTile(HexTile.PortalTile(At(0, 5), At(2), 5));
            // Exit heads NE (D1) from (0, 5).
            for (int i = 1; i <= 3; i++)
                map.SetTile(HexTile.Floor(new HexCoord(i, 5 - i)));

            var result = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 4));

            CollectionAssert.AreEqual(new[] { At(1), At(2), At(0, 5), At(1, 4), At(2, 3) }, result.Path);
            Assert.AreEqual(At(2, 3), result.End);
            Assert.AreEqual(new HexVelocity(HexDirection.D1, 4), result.FinalVelocity);
            Assert.AreEqual(4, result.StepsTaken);
            var teleport = result.Events.Single();
            Assert.AreEqual(MoveEventType.Teleported, teleport.Type);
            Assert.AreEqual(At(2), teleport.Coord);
            Assert.AreEqual(At(0, 5), teleport.Other);
        }

        [Test]
        public void BoostChain_IsCappedByMaxSteps()
        {
            var map = new HexMap();
            for (int q = 0; q < 100; q++)
                map.SetTile(HexTile.BoostTile(At(q), BoostSettings.Along(1)));

            var result = MomentumStepper.Slide(map, At(0), new HexVelocity(East, 1), maxSteps: 10);

            Assert.AreEqual(10, result.StepsTaken);
            Assert.AreEqual(At(10), result.End);
            Assert.AreEqual(MoveEventType.StepCapReached, result.Events.Last().Type);
        }
    }
}
