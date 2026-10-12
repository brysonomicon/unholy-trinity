using NUnit.Framework;
using UnityEngine;

namespace UnholyTrinity.Hex.Tests
{
    public class HexLayoutTests
    {
        [TestCase(HexOrientation.PointyTop)]
        [TestCase(HexOrientation.FlatTop)]
        public void HexToWorld_RoundTrips(HexOrientation orientation)
        {
            var layout = new HexLayout(orientation, 1.3f, new Vector3(5f, 2f, -3f));
            foreach (var hex in HexCoord.Range(HexCoord.Zero, 6))
                Assert.AreEqual(hex, layout.WorldToHex(layout.HexToWorld(hex)));
        }

        [TestCase(HexOrientation.PointyTop)]
        [TestCase(HexOrientation.FlatTop)]
        public void WorldToHex_SnapsPointsInsideTheHex(HexOrientation orientation)
        {
            var layout = new HexLayout(orientation, 1f, Vector3.zero);
            var hex = new HexCoord(2, -1);
            var center = layout.HexToWorld(hex);
            for (int i = 0; i < 6; i++)
                Assert.AreEqual(hex, layout.WorldToHex(center + layout.CornerOffset(i) * 0.8f));
        }

        [TestCase(HexOrientation.PointyTop)]
        [TestCase(HexOrientation.FlatTop)]
        public void NeighborCenters_AreSqrt3SizeApart_AndDirectionsTurnCounterClockwise(HexOrientation orientation)
        {
            var layout = new HexLayout(orientation, 1f, Vector3.zero);
            for (int d = 0; d < 6; d++)
            {
                var dir = (HexDirection)d;
                Assert.AreEqual(Mathf.Sqrt(3f), Vector3.Distance(layout.HexToWorld(HexCoord.Zero), layout.HexToWorld(dir.ToOffset())), 1e-4f);

                // Viewed from above, each next direction is 60 degrees counter-clockwise.
                float angle = Vector3.SignedAngle(layout.DirectionToWorld(dir), layout.DirectionToWorld(dir.Rotate(1)), Vector3.down);
                Assert.AreEqual(60f, angle, 1e-3f, $"{orientation} {dir}");
            }
        }

        [Test]
        public void D1_IsNorthEast_InBothOrientations()
        {
            foreach (var orientation in new[] { HexOrientation.PointyTop, HexOrientation.FlatTop })
            {
                var v = new HexLayout(orientation, 1f, Vector3.zero).DirectionToWorld(HexDirection.D1);
                Assert.That(v.x > 0f && v.z > 0f, $"{orientation}: {v}");
                Assert.AreEqual("NE", HexDirection.D1.DisplayName(orientation));
            }
        }
    }
}
