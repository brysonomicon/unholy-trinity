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

        public Color FloorColor = new Color(0.55f, 0.55f, 0.58f);
        public Color WallColor = new Color(0.22f, 0.22f, 0.26f);
        public Color BouncyWallColor = new Color(0.2f, 0.55f, 0.85f);
        public Color SpikedWallColor = new Color(0.75f, 0.15f, 0.15f);
        public Color PickupColor = new Color(0.95f, 0.8f, 0.2f);
        public Color BoostColor = new Color(0.25f, 0.85f, 0.45f);
        public Color FrictionColor = new Color(0.65f, 0.4f, 0.2f);
        public Color PortalColor = new Color(0.55f, 0.3f, 0.95f);
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
                default: return FloorColor;
            }
        }

        public float HeightFor(HexTile tile) => tile.Type == TileType.Wall ? WallHeight : FloorHeight;

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
                return entry.Prefab;
            }
            return null;
        }
    }
}
