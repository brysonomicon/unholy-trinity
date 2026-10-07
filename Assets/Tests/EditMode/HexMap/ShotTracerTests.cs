using NUnit.Framework;

namespace UnholyTrinity.Hex.Tests
{
    public class ShotTracerTests
    {
        static HexCoord At(int q, int r = 0) => new HexCoord(q, r);

        static HexMap Field(int radius)
        {
            var map = new HexMap();
            foreach (var hex in HexCoord.Range(HexCoord.Zero, radius))
                map.SetTile(HexTile.Floor(hex));
            return map;
        }

        [Test]
        public void Shot_StopsAtWall()
        {
            var map = Field(5);
            map.SetTile(HexTile.Wall(At(3)));
            var result = ShotTracer.Trace(map, At(0), HexDirection.D0, 10);

            CollectionAssert.AreEqual(new[] { At(1), At(2) }, result.Path);
            Assert.AreEqual(At(3), result.BlockedAt);
        }

        [Test]
        public void Shot_RunsOutOfRange()
        {
            var result = ShotTracer.Trace(Field(5), At(0), HexDirection.D0, 2);
            CollectionAssert.AreEqual(new[] { At(1), At(2) }, result.Path);
            Assert.IsNull(result.BlockedAt);
        }

        [Test]
        public void Shot_IgnoresBoostTiles()
        {
            var map = Field(5);
            map.SetTile(HexTile.BoostTile(At(1), BoostSettings.Fixed(3, HexDirection.D2)));
            var result = ShotTracer.Trace(map, At(0), HexDirection.D0, 3);
            CollectionAssert.AreEqual(new[] { At(1), At(2), At(3) }, result.Path);
        }

        [Test]
        public void Portals_OnlyTransmitWhenEnabled()
        {
            var map = Field(5);
            map.SetTile(HexTile.PortalTile(At(1), At(-2, 2), 2));
            map.SetTile(HexTile.PortalTile(At(-2, 2), At(1), 4));

            var blocked = ShotTracer.Trace(map, At(0), HexDirection.D0, 3);
            CollectionAssert.AreEqual(new[] { At(1), At(2), At(3) }, blocked.Path);

            var through = ShotTracer.Trace(map, At(0), HexDirection.D0, 3, new ShotTraceOptions { PassThroughPortals = true });
            // Exits at (-2, 2) heading D2 (0, -1).
            CollectionAssert.AreEqual(new[] { At(1), At(-2, 2), At(-2, 1), At(-2, 0) }, through.Path);
            Assert.AreEqual(HexDirection.D2, through.FinalDirection);
        }

        [Test]
        public void LineOfSight_BlockedByWallsBetween()
        {
            var map = Field(5);
            Assert.IsTrue(ShotTracer.HasLineOfSight(map, At(-3), At(3)));
            map.SetTile(HexTile.Wall(At(0)));
            Assert.IsFalse(ShotTracer.HasLineOfSight(map, At(-3), At(3)));
            Assert.IsTrue(ShotTracer.HasLineOfSight(map, At(-3), At(-1)));
        }
    }
}
