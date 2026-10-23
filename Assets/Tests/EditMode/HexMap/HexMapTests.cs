using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnholyTrinity.Hex.Tests
{
    public class HexMapTests
    {
        [Test]
        public void OffMapAndWalls_AreNotWalkable_AndBlockShots()
        {
            var map = new HexMap();
            map.SetTile(HexTile.Floor(HexCoord.Zero));
            map.SetTile(HexTile.Wall(new HexCoord(1, 0)));

            Assert.IsTrue(map.IsWalkable(HexCoord.Zero));
            Assert.IsFalse(map.IsWalkable(new HexCoord(1, 0)));
            Assert.IsFalse(map.IsWalkable(new HexCoord(5, 5)));
            Assert.IsTrue(map.BlocksLineOfFire(new HexCoord(1, 0)));
            Assert.IsTrue(map.BlocksLineOfFire(new HexCoord(5, 5)));
            Assert.IsFalse(map.BlocksLineOfFire(HexCoord.Zero));
        }

        [Test]
        public void WallVariants_BlockLikeWalls_AndReportTheirSettings()
        {
            var map = new HexMap();
            map.SetTile(HexTile.Floor(HexCoord.Zero));
            map.SetTile(HexTile.BouncyWall(new HexCoord(1, 0)));
            map.SetTile(HexTile.SpikedWall(new HexCoord(2, 0), 3, SpikeDamageMode.ScaledBySpeed));

            Assert.IsFalse(map.IsWalkable(new HexCoord(1, 0)));
            Assert.IsTrue(map.BlocksLineOfFire(new HexCoord(2, 0)));

            Assert.IsTrue(map.TryGetWall(new HexCoord(1, 0), out var bouncy));
            Assert.AreEqual(WallKind.Bouncy, bouncy.Kind);
            Assert.IsTrue(map.TryGetWall(new HexCoord(2, 0), out var spiked));
            Assert.AreEqual(WallSettings.Spiked(3, SpikeDamageMode.ScaledBySpeed), spiked);

            Assert.IsFalse(map.TryGetWall(HexCoord.Zero, out _));
            Assert.IsFalse(map.TryGetWall(new HexCoord(5, 5), out var offMap));
            Assert.AreEqual(WallKind.Normal, offMap.Kind);
        }

        [Test]
        public void SpikeDamage_FixedOrScaledBySpeed()
        {
            Assert.AreEqual(3, WallSettings.Spiked(3).DamageFor(4));
            Assert.AreEqual(12, WallSettings.Spiked(3, SpikeDamageMode.ScaledBySpeed).DamageFor(4));
            Assert.AreEqual(0, WallSettings.Bouncy.DamageFor(4));
        }

        [Test]
        public void Validate_AcceptsReciprocalPortals()
        {
            var map = new HexMap();
            map.SetTile(HexTile.PortalTile(new HexCoord(0, 0), new HexCoord(3, 0), 1));
            map.SetTile(HexTile.PortalTile(new HexCoord(3, 0), new HexCoord(0, 0), 5));
            Assert.IsEmpty(map.Validate());
        }

        [Test]
        public void Validate_FlagsBrokenPortals()
        {
            var map = new HexMap();
            map.SetTile(HexTile.PortalTile(new HexCoord(0, 0), new HexCoord(9, 9)));           // off map
            map.SetTile(HexTile.PortalTile(new HexCoord(1, 0), new HexCoord(2, 0)));           // target not a portal
            map.SetTile(HexTile.Floor(new HexCoord(2, 0)));
            map.SetTile(HexTile.PortalTile(new HexCoord(3, 0), new HexCoord(3, 0)));           // self
            map.SetTile(HexTile.PortalTile(new HexCoord(4, 0), new HexCoord(5, 0)));           // not reciprocal
            map.SetTile(HexTile.PortalTile(new HexCoord(5, 0), new HexCoord(3, 0)));

            var problems = map.Validate();
            Assert.That(problems.Any(p => p.Contains("(0, 0)") && p.Contains("off the map")));
            Assert.That(problems.Any(p => p.Contains("(1, 0)") && p.Contains("not a portal")));
            Assert.That(problems.Any(p => p.Contains("(3, 0)") && p.Contains("itself")));
            Assert.That(problems.Any(p => p.Contains("(4, 0)") && p.Contains("but that portal")));
            Assert.IsFalse(map.TryGetPortalExit(new HexCoord(0, 0), out _, out _));
        }

        [Test]
        public void Definition_RoundTripsThroughHexMap()
        {
            var definition = ScriptableObject.CreateInstance<HexMapDefinition>();
            try
            {
                var map = new HexMap();
                map.SetTile(HexTile.Floor(new HexCoord(0, 0)));
                map.SetTile(HexTile.Wall(new HexCoord(1, 0)));
                map.SetTile(HexTile.PickupPoint(new HexCoord(2, 0), 3));
                map.SetTile(HexTile.BouncyWall(new HexCoord(3, 0)));
                map.SetTile(HexTile.SpikedWall(new HexCoord(4, 0), 2, SpikeDamageMode.ScaledBySpeed));
                map.SetTile(HexTile.BoostTile(new HexCoord(0, 1), BoostSettings.Fixed(-2, HexDirection.D4)));
                map.SetTile(HexTile.PortalTile(new HexCoord(1, 1), new HexCoord(2, 1), 2));
                map.SetTile(HexTile.PortalTile(new HexCoord(2, 1), new HexCoord(1, 1), 4));
                map.SetTile(HexTile.HealTile(new HexCoord(3, 1), 2));
                map.SetTile(HexTile.HarmTile(new HexCoord(4, 1), 3, HealthTrigger.OnStop));
                map.SetTile(HexTile.KillTile(new HexCoord(0, 2)));

                definition.Write(map);
                var rebuilt = definition.Build();

                Assert.AreEqual(map.Count, rebuilt.Count);
                foreach (var tile in map.Tiles)
                {
                    Assert.IsTrue(rebuilt.TryGetTile(tile.Coord, out var other));
                    Assert.AreEqual(tile.Type, other.Type);
                    Assert.AreEqual(tile.Boost, other.Boost);
                    Assert.AreEqual(tile.Portal, other.Portal);
                    Assert.AreEqual(tile.Pickup, other.Pickup);
                    Assert.AreEqual(tile.WallSettings, other.WallSettings);
                    Assert.AreEqual(tile.Health, other.Health);
                }
                Assert.IsEmpty(definition.Validate());
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
