using System.Linq;
using NUnit.Framework;

namespace UnholyTrinity.Hex.Tests
{
    public class HexCoordTests
    {
        [Test]
        public void Distance_CountsSteps()
        {
            Assert.AreEqual(0, HexCoord.Distance(HexCoord.Zero, HexCoord.Zero));
            Assert.AreEqual(3, HexCoord.Distance(HexCoord.Zero, new HexCoord(3, -3)));
            Assert.AreEqual(4, HexCoord.Distance(new HexCoord(-2, 0), new HexCoord(2, 0)));
            Assert.AreEqual(2, HexCoord.Distance(new HexCoord(1, 1), new HexCoord(-1, 2)));
        }

        [Test]
        public void Neighbors_AreAllAdjacentAndDistinct()
        {
            var neighbors = Enumerable.Range(0, 6).Select(i => HexCoord.Zero.Neighbor((HexDirection)i)).ToList();
            Assert.That(neighbors, Is.Unique);
            Assert.That(neighbors.All(n => n.DistanceTo(HexCoord.Zero) == 1));
        }

        [Test]
        public void Direction_RotateAndOpposite()
        {
            Assert.AreEqual(HexDirection.D1, HexDirection.D0.Rotate(1));
            Assert.AreEqual(HexDirection.D5, HexDirection.D0.Rotate(-1));
            Assert.AreEqual(HexDirection.D2, HexDirection.D4.Rotate(4));
            Assert.AreEqual(HexDirection.D3, HexDirection.D0.Opposite());
            Assert.AreEqual(HexCoord.Zero, HexDirection.D2.ToOffset() + HexDirection.D2.Opposite().ToOffset());
        }

        [Test]
        public void RotateAround_MatchesDirectionRotation()
        {
            for (int d = 0; d < 6; d++)
            {
                var dir = (HexDirection)d;
                for (int steps = -7; steps <= 7; steps++)
                    Assert.AreEqual(dir.Rotate(steps).ToOffset(), dir.ToOffset().RotateAround(HexCoord.Zero, steps), $"{dir} by {steps}");
            }
        }

        [Test]
        public void Ring_And_Range_HaveExpectedCounts()
        {
            Assert.AreEqual(1, HexCoord.Ring(HexCoord.Zero, 0).Count());
            Assert.AreEqual(18, HexCoord.Ring(new HexCoord(2, -1), 3).Count());
            Assert.That(HexCoord.Ring(new HexCoord(2, -1), 3).All(h => h.DistanceTo(new HexCoord(2, -1)) == 3));
            Assert.AreEqual(37, HexCoord.Range(HexCoord.Zero, 3).Count()); // 3r(r+1)+1
            Assert.That(HexCoord.Range(HexCoord.Zero, 3), Is.Unique);
        }

        [Test]
        public void LineTo_IsContiguousAndIncludesEnds()
        {
            var a = new HexCoord(-2, 1);
            var b = new HexCoord(3, -2);
            var line = a.LineTo(b);
            Assert.AreEqual(HexCoord.Distance(a, b) + 1, line.Count);
            Assert.AreEqual(a, line.First());
            Assert.AreEqual(b, line.Last());
            for (int i = 1; i < line.Count; i++)
                Assert.AreEqual(1, line[i - 1].DistanceTo(line[i]));
        }
    }
}
