using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Look of each tile type. Tiles with a prefab assigned use it; everything else falls back to a procedural
    /// hex prism coloured below. MapManager falls back to built-in defaults when no style is assigned.
    /// </summary>
    [CreateAssetMenu(menuName = "Unholy Trinity/Hex Tile Style", fileName = "HexTileStyle")]
    public class HexTileStyle : ScriptableObject
    {
        [Serializable]
        public struct TileVisualEntry
        {
            public TileType Type;
            public GameObject Prefab;
            [Tooltip("Optional: used instead of Prefab for boost tiles with negative strength.")]
            public GameObject FrictionPrefab;
            [Tooltip("Optional: used instead of Prefab for bouncy walls.")]
            public GameObject BouncyPrefab;
            [Tooltip("Optional: used instead of Prefab for spiked walls.")]
            public GameObject SpikedPrefab;
            [Tooltip("Optional: used instead of Prefab for health tiles with negative amount (Prefab is the heal look).")]
            public GameObject HarmPrefab;
            [Tooltip("Optional: used instead of Prefab for lethal health tiles, e.g. lava, or an empty prefab for a void.")]
            public GameObject KillPrefab;
        }

        [Tooltip("Prefabs are authored pointy-top for a hex of size 1 with the base at y = 0. MapManager scales them by " +
                 "hex size and turns them 30 degrees for flat-top. Add an ITileVisual (e.g. TileVisual) to react to tile data.")]
        public List<TileVisualEntry> Prefabs = new List<TileVisualEntry>();

        [Header("Procedural fallback")]
        [Tooltip("Shared material for every tile; colours are applied per tile with _BaseColor. Leave empty to use URP Lit.")]
        public Material TileMaterial;

        [Range(0.5f, 1f), Tooltip("Visual hex size relative to the grid, leaving small gaps between tiles.")]
        public float TileScale = 1f;

        public float FloorHeight = 0.1f;
        public float WallHeight = 1.2f;
        [Tooltip("Height of lethal tiles; lower than FloorHeight so they read as sunken. Keep above 0.")]
        public float PitHeight = 0.01f;

        public Color FloorColor = new Color(0.55f, 0.55f, 0.58f);
        public Color WallColor = new Color(0.22f, 0.22f, 0.26f);
        // Wong colour-blind-safe palette.
        public static readonly Color WongOrange = new Color32(0xE6, 0x9F, 0x00, 0xFF);
        public static readonly Color WongSkyBlue = new Color32(0x56, 0xB4, 0xE9, 0xFF);
        public static readonly Color WongBluishGreen = new Color32(0x00, 0x9E, 0x73, 0xFF);
        public static readonly Color WongYellow = new Color32(0xF0, 0xE4, 0x42, 0xFF);
        public static readonly Color WongBlue = new Color32(0x00, 0x72, 0xB2, 0xFF);
        public static readonly Color WongVermillion = new Color32(0xD5, 0x5E, 0x00, 0xFF);
        public static readonly Color WongReddishPurple = new Color32(0xCC, 0x79, 0xA7, 0xFF);

        public Color BouncyWallColor = WongSkyBlue;
        public Color SpikedWallColor = WongVermillion;
        public Color PickupColor = WongYellow;
        public Color BoostColor = WongBluishGreen;
        public Color FrictionColor = WongOrange;
        public Color PortalColor = WongReddishPurple;
        public Color HealColor = WongBlue;
        public Color HarmColor = WongVermillion;
        public Color KillColor = new Color(0.08f, 0.08f, 0.1f);
        public Color ArrowColor = Color.white;

        public static HexTileStyle CreateDefault()
        {
            var style = CreateInstance<HexTileStyle>();
            style.hideFlags = HideFlags.DontSave;
            return style;
        }

        public Color ColorFor(HexTile tile)
        {
            switch (tile.Type)
            {
                case TileType.Wall:
                    switch (tile.WallSettings.Kind)
                    {
                        case WallKind.Bouncy: return BouncyWallColor;
                        case WallKind.Spiked: return SpikedWallColor;
                        default: return WallColor;
                    }
                case TileType.PickupPoint: return PickupColor;
                case TileType.Boost: return tile.Boost.Strength >= 0 ? BoostColor : FrictionColor;
                case TileType.Portal: return PortalColor;
                case TileType.Health:
                    if (tile.Health.Lethal)
                        return KillColor;
                    return tile.Health.Amount >= 0 ? HealColor : HarmColor;
                default: return FloorColor;
            }
        }

        public float HeightFor(HexTile tile)
        {
            if (tile.Type == TileType.Wall)
                return WallHeight;
            if (tile.Type == TileType.Health && tile.Health.Lethal)
                return PitHeight;
            return FloorHeight;
        }

        /// <summary>The prefab for this tile, or null to use the procedural fallback.</summary>
        public GameObject PrefabFor(HexTile tile)
        {
            foreach (var entry in Prefabs)
            {
                if (entry.Type != tile.Type)
                    continue;
                if (tile.Type == TileType.Boost && tile.Boost.Strength < 0 && entry.FrictionPrefab != null)
                    return entry.FrictionPrefab;
                if (tile.Type == TileType.Wall && tile.WallSettings.Kind == WallKind.Bouncy && entry.BouncyPrefab != null)
                    return entry.BouncyPrefab;
                if (tile.Type == TileType.Wall && tile.WallSettings.Kind == WallKind.Spiked && entry.SpikedPrefab != null)
                    return entry.SpikedPrefab;
                if (tile.Type == TileType.Health && tile.Health.Lethal && entry.KillPrefab != null)
                    return entry.KillPrefab;
                if (tile.Type == TileType.Health && !tile.Health.Lethal && tile.Health.Amount < 0 && entry.HarmPrefab != null)
                    return entry.HarmPrefab;
                return entry.Prefab;
            }
            return null;
        }
    }
}
