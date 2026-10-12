using UnityEditor;
using UnityEngine;

namespace UnholyTrinity.Hex.Editing
{
    /// <summary>Edit-mode preview of a MapManager's definition, drawn with Handles so no GameObjects are spawned.</summary>
    static class HexMapGizmos
    {
        private static readonly Vector3[] Corners = new Vector3[7];

        private static HexTileStyle _defaultStyle;

        private static HexTileStyle DefaultStyle => _defaultStyle != null ? _defaultStyle : _defaultStyle = HexTileStyle.CreateDefault();

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void Draw(MapManager manager, GizmoType gizmoType)
        {
            // In play mode the spawned meshes already show the map.
            if (Application.isPlaying || manager.Definition == null)
                return;

            var layout = manager.Layout;
            var style = DefaultStyle;
            foreach (var entry in manager.Definition.Entries)
            {
                var tile = entry.ToTile();
                DrawTile(layout, tile, style.ColorFor(tile));
            }
            foreach (var entry in manager.Definition.Entries)
                if (entry.Type == TileType.Portal)
                    DrawPortalLink(layout, entry.Coord, entry.Portal.Target, style.PortalColor);
        }

        public static void DrawTile(HexLayout layout, HexTile tile, Color color)
        {
            var center = layout.HexToWorld(tile.Coord);
            FillHex(layout, center, 0.92f, color, tile.Type == TileType.Wall ? 0.9f : 0.55f);

            if (tile.Type == TileType.Boost)
            {
                if (tile.Boost.Mode == BoostMode.Fixed)
                    DrawArrow(layout, center, tile.Boost.Direction, Color.white);
                Handles.Label(center, tile.Boost.Strength.ToString("+0;-0;0"), EditorStyles.whiteBoldLabel);
            }
            else if (tile.Type == TileType.Wall && tile.WallSettings.Kind == WallKind.Bouncy)
            {
                Handles.Label(center, "B", EditorStyles.whiteBoldLabel);
            }
            else if (tile.Type == TileType.Wall && tile.WallSettings.Kind == WallKind.Spiked)
            {
                var scaled = tile.WallSettings.DamageMode == SpikeDamageMode.ScaledBySpeed ? "×v" : "";
                Handles.Label(center, $"S{tile.WallSettings.Damage}{scaled}", EditorStyles.whiteBoldLabel);
            }
            else if (tile.Type == TileType.PickupPoint)
            {
                Handles.Label(center, $"P{tile.Pickup.SpawnGroup}", EditorStyles.boldLabel);
            }
        }

        private static void FillHex(HexLayout layout, Vector3 center, float scale, Color color, float alpha)
        {
            for (int i = 0; i < 6; i++)
                Corners[i] = center + layout.CornerOffset(i) * scale;
            Corners[6] = Corners[0];

            var fill = color;
            fill.a = alpha;
            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(new[] { Corners[0], Corners[1], Corners[2], Corners[3] }, fill, Color.clear);
            Handles.DrawSolidRectangleWithOutline(new[] { Corners[3], Corners[4], Corners[5], Corners[0] }, fill, Color.clear);
            Handles.color = new Color(0f, 0f, 0f, 0.6f);
            Handles.DrawPolyLine(Corners);
        }

        public static void OutlineHex(HexLayout layout, HexCoord coord, Color color, float thickness = 3f)
        {
            var center = layout.HexToWorld(coord);
            for (int i = 0; i < 6; i++)
                Corners[i] = center + layout.CornerOffset(i);
            Corners[6] = Corners[0];
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, Corners);
        }

        public static void DrawArrow(HexLayout layout, Vector3 center, HexDirection direction, Color color)
        {
            var dir = layout.DirectionToWorld(direction);
            var side = Vector3.Cross(Vector3.up, dir) * (layout.Size * 0.25f);
            var tip = center + dir * (layout.Size * 0.6f);
            var tail = center - dir * (layout.Size * 0.4f);
            Handles.color = color;
            Handles.DrawAAPolyLine(4f, tail, tip);
            Handles.DrawAAPolyLine(4f, tip - dir * (layout.Size * 0.3f) + side, tip, tip - dir * (layout.Size * 0.3f) - side);
        }

        private static void DrawPortalLink(HexLayout layout, HexCoord from, HexCoord to, Color color)
        {
            var a = layout.HexToWorld(from) + Vector3.up * 0.05f;
            var b = layout.HexToWorld(to) + Vector3.up * 0.05f;
            Handles.color = color;
            Handles.DrawDottedLine(a, b, 4f);
        }
    }
}
