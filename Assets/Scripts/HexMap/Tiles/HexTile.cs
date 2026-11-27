using System;
using System.Collections.Generic;
using UnholyTrinity.Hex.Interfaces;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// tile data. Only the payload matching <see cref="Type"/> is meaningful;
    /// use the static factories to build tiles.
    /// </summary>
    public class HexTile
    {
        public static event Action<HexTile> TrackCollision;
        public static event Action<HexTile> ClearCollision;

        public readonly HexCoord Coord;
        public readonly TileType Type;
        public readonly BoostSettings Boost;
        public readonly PortalLink Portal;
        public readonly PickupPointData Pickup;
        public readonly WallSettings WallSettings;
        public readonly HealthTileSettings Health;
        public readonly List<IMovable> StandingOn = new();
        public bool HasCollision => StandingOn.Count > 1;

        public bool IsWalkable => Type != TileType.Wall;
        public bool BlocksLineOfFire => Type == TileType.Wall;

        public HexTile(HexCoord coord, TileType type,
            BoostSettings boost = default, PortalLink portal = default,
            PickupPointData pickup = default, WallSettings wall = default,
            HealthTileSettings health = default)
        {
            Coord = coord;
            Type = type;
            Boost = boost;
            Portal = portal;
            Pickup = pickup;
            WallSettings = wall;
            Health = health;
        }

        public virtual void Enter(ref IMovable mover)
        {
            mover.Tile = this;
            mover.Coord = Coord;
            StandingOn.Add(mover);
            if (HasCollision)
            {
                TrackCollision?.Invoke(this);
            }
        }

        public virtual void Exit(ref IMovable mover)
        {
            bool hadCollision = HasCollision;
            StandingOn.Remove(mover);
            if (hadCollision && !HasCollision)
            {
                ClearCollision?.Invoke(this);
            }
        }

        #region Contructors
        public static HexTile Floor(HexCoord coord) =>
            new HexTile(coord, TileType.Floor);

        public static HexTile Wall(HexCoord coord) =>
            new HexTile(coord, TileType.Wall);

        public static HexTile Wall(HexCoord coord, WallSettings wall) =>
            new HexTile(coord, TileType.Wall, wall: wall);

        public static HexTile BouncyWall(HexCoord coord) =>
            Wall(coord, WallSettings.Bouncy);

        public static HexTile SpikedWall(HexCoord coord, int damage,
            SpikeDamageMode mode = SpikeDamageMode.Fixed) =>
            Wall(coord, WallSettings.Spiked(damage, mode));

        public static HexTile PickupPoint(HexCoord coord,
            PickupDefinition pickup = null) => new HexTile(coord,
            TileType.PickupPoint, pickup: new PickupPointData(pickup));

        public static HexTile BoostTile(HexCoord coord, BoostSettings boost) =>
            new HexTile(coord, TileType.Boost, boost: boost);

        public static HexTile PortalTile(HexCoord coord, HexCoord target,
            int rotationSteps = 0) => new HexTile(coord, TileType.Portal,
            portal: new PortalLink(target, rotationSteps));

        public static HexTile PortalExit(HexCoord coord) => new HexTile(coord,
            TileType.Portal, portal: PortalLink.Exit);

        public static HexTile HealthTile(HexCoord coord,
            HealthTileSettings health) =>
            new HexTile(coord, TileType.Health, health: health);

        public static HexTile HealTile(HexCoord coord, int amount,
            HealthTrigger trigger = HealthTrigger.OnEnter) => HealthTile(coord,
            HealthTileSettings.Heal(amount, trigger));

        public static HexTile HarmTile(HexCoord coord, int damage,
            HealthTrigger trigger = HealthTrigger.OnEnter) => HealthTile(coord,
            HealthTileSettings.Harm(damage, trigger));

        public static HexTile KillTile(HexCoord coord,
            HealthTrigger trigger = HealthTrigger.OnEnter) =>
            HealthTile(coord, HealthTileSettings.Kill(trigger));
        #endregion

        public override string ToString() => $"{Type} {Coord}";
    }
}
