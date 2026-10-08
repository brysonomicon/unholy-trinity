using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnholyTrinity.Hex.Editing
{
    /// <summary>
    /// Inspector palette plus Scene-view painting for a MapManager's <see cref="HexMapDefinition"/>.
    /// LMB / drag paints, Shift+LMB erases, R rotates the boost direction.
    /// Portal mode: click the first hex, then the second, to place a linked pair.
    /// </summary>
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : UnityEditor.Editor
    {
        // Static so the palette survives reselecting the object. Statics are reset by domain reloads
        // (entering play mode, recompiling), so the palette is also mirrored to SessionState.
        static bool editing;
        static TileType paintType = TileType.Floor;
        static BoostSettings boost = BoostSettings.Along(1);
        static WallSettings wall = WallSettings.Normal;
        static int pickupGroup;
        static int portalRotation;
        static int fillRadius = 4;
        static bool fillWithWallBorder = true;

        const string PaletteKey = "UnholyTrinity.Hex.MapManagerEditor.Palette";
        static bool paletteLoaded;

        [System.Serializable]
        struct PaletteState
        {
            public bool Editing;
            public TileType PaintType;
            public BoostSettings Boost;
            public WallSettings Wall;
            public int PickupGroup;
            public int PortalRotation;
            public int FillRadius;
            public bool FillWithWallBorder;
        }

        HexCoord? pendingPortal;
        HexCoord? hovered;

        void OnEnable() => LoadPalette();

        void OnDisable() => pendingPortal = null;

        /// <summary>Restores the palette after a domain reload; does nothing if already loaded in this domain.</summary>
        static void LoadPalette()
        {
            if (paletteLoaded)
                return;
            paletteLoaded = true;

            var json = SessionState.GetString(PaletteKey, null);
            if (string.IsNullOrEmpty(json))
                return;
            var state = JsonUtility.FromJson<PaletteState>(json);
            editing = state.Editing;
            paintType = state.PaintType;
            boost = state.Boost;
            wall = state.Wall;
            pickupGroup = state.PickupGroup;
            portalRotation = state.PortalRotation;
            fillRadius = state.FillRadius;
            fillWithWallBorder = state.FillWithWallBorder;
        }

        static void SavePalette()
        {
            var state = new PaletteState
            {
                Editing = editing,
                PaintType = paintType,
                Boost = boost,
                Wall = wall,
                PickupGroup = pickupGroup,
                PortalRotation = portalRotation,
                FillRadius = fillRadius,
                FillWithWallBorder = fillWithWallBorder,
            };
            SessionState.SetString(PaletteKey, JsonUtility.ToJson(state));
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (MapManager)target;
            var definition = manager.Definition;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Painting", EditorStyles.boldLabel);

            if (definition == null)
            {
                EditorGUILayout.HelpBox("Assign a Hex Map definition (Create > Unholy Trinity > Hex Map) to start painting.", MessageType.Info);
                return;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Painting edits the asset but does not affect the running map until it is rebuilt.", MessageType.None);
            }

            EditorGUI.BeginChangeCheck();

            editing =GUILayout.Toggle(editing, editing ? "Stop Editing" : "Edit Map", "Button", GUILayout.Height(24));
            if (editing)
            {
                EditorGUILayout.HelpBox("LMB paint, Shift+LMB erase, R rotate boost direction, Alt+drag orbits as usual.", MessageType.None);
            }

            paintType = (TileType)EditorGUILayout.EnumPopup("Tile", paintType);
            if (paintType != TileType.Portal)
                pendingPortal = null;

            using (new EditorGUI.IndentLevelScope())
            {
                switch (paintType)
                {
                    case TileType.Wall:
                        wall.Kind = (WallKind)EditorGUILayout.EnumPopup("Kind", wall.Kind);
                        if (wall.Kind == WallKind.Spiked)
                        {
                            wall.Damage = EditorGUILayout.IntField("Damage", wall.Damage);
                            wall.DamageMode = (SpikeDamageMode)EditorGUILayout.EnumPopup(new GUIContent("Damage Mode", "Scaled By Speed multiplies Damage by the unit's speed at impact."), wall.DamageMode);
                        }
                        break;
                    case TileType.Boost:
                        boost.Strength = EditorGUILayout.IntField(new GUIContent("Strength", "+ boosts, - is friction"), boost.Strength);
                        boost.Mode = (BoostMode)EditorGUILayout.EnumPopup("Mode", boost.Mode);
                        if (boost.Mode == BoostMode.Fixed)
                            boost.Direction = DirectionPopup("Direction", boost.Direction, manager.Orientation);
                        break;
                    case TileType.PickupPoint:
                        pickupGroup = EditorGUILayout.IntField("Spawn Group", pickupGroup);
                        break;
                    case TileType.Portal:
                        portalRotation = EditorGUILayout.IntSlider(new GUIContent("Rotation (A to B)", "60 degree CCW steps applied when exiting at B; B to A uses the inverse."), portalRotation, 0, 5);
                        EditorGUILayout.LabelField(pendingPortal.HasValue ? $"First portal at {pendingPortal.Value}, click the second hex." : "Click the first portal hex.", EditorStyles.miniLabel);
                        break;
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                fillRadius = EditorGUILayout.IntField("Hexagon Radius", fillRadius);
                fillWithWallBorder = GUILayout.Toggle(fillWithWallBorder, "Wall border", GUILayout.Width(90));
            }
            if (EditorGUI.EndChangeCheck())
                SavePalette();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Hexagon"))
                    FillHexagon(definition, Mathf.Max(0, fillRadius), fillWithWallBorder);
                if (GUILayout.Button("Clear") && EditorUtility.DisplayDialog("Clear map", $"Remove all tiles from '{definition.name}'?", "Clear", "Cancel"))
                    Modify(definition, "Clear Hex Map", d => d.Clear());
            }

            var problems = definition.Validate();
            EditorGUILayout.LabelField($"{definition.Entries.Count} tiles", EditorStyles.miniLabel);
            foreach (var problem in problems)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }

        void OnSceneGUI()
        {
            var manager = (MapManager)target;
            var definition = manager.Definition;
            if (!editing || definition == null)
                return;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            var layout = manager.Layout;
            var previousHover = hovered;
            hovered = MouseToHex(layout, manager.transform.position, e.mousePosition);

            if (hovered.HasValue)
                HexMapGizmos.OutlineHex(layout, hovered.Value, e.shift ? HexTileStyle.WongVermillion : HexTileStyle.WongYellow);
            if (pendingPortal.HasValue)
                HexMapGizmos.OutlineHex(layout, pendingPortal.Value, HexTileStyle.WongReddishPurple, 5f);
            if (paintType == TileType.Boost && boost.Mode == BoostMode.Fixed && hovered.HasValue)
                HexMapGizmos.DrawArrow(layout, layout.HexToWorld(hovered.Value), boost.Direction, HexTileStyle.WongYellow);

            switch (e.type)
            {
                case EventType.MouseMove:
                    if (hovered != previousHover)
                        SceneView.RepaintAll();
                    break;

                case EventType.MouseDown when e.button == 0 && !e.alt && hovered.HasValue:
                    GUIUtility.hotControl = controlId;
                    Undo.IncrementCurrentGroup();
                    ApplyAt(definition, hovered.Value, e.shift, isDrag: false);
                    e.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId && hovered.HasValue:
                    ApplyAt(definition, hovered.Value, e.shift, isDrag: true);
                    e.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;

                case EventType.KeyDown when e.keyCode == KeyCode.R && paintType == TileType.Boost:
                    boost.Direction = boost.Direction.Rotate(e.shift ? -1 : 1);
                    boost.Mode = BoostMode.Fixed;
                    SavePalette();
                    Repaint();
                    e.Use();
                    break;
            }
        }

        void ApplyAt(HexMapDefinition definition, HexCoord coord, bool erase, bool isDrag)
        {
            if (erase)
            {
                if (definition.TryGetTile(coord, out _))
                    Modify(definition, "Erase Hex", d => d.RemoveTile(coord));
                return;
            }

            if (paintType == TileType.Portal)
            {
                if (!isDrag)
                    PlacePortal(definition, coord);
                return;
            }

            var tile = paintType switch
            {
                TileType.Wall => HexTile.Wall(coord, wall.Kind == WallKind.Spiked ? wall : new WallSettings(wall.Kind)),
                TileType.PickupPoint => HexTile.PickupPoint(coord, pickupGroup),
                TileType.Boost => HexTile.BoostTile(coord, boost),
                _ => HexTile.Floor(coord),
            };

            if (definition.TryGetTile(coord, out var existing) && SameTile(existing, tile))
                return;
            Modify(definition, "Paint Hex", d => d.SetTile(tile));
        }

        void PlacePortal(HexMapDefinition definition, HexCoord coord)
        {
            if (!pendingPortal.HasValue || pendingPortal.Value == coord)
            {
                pendingPortal = coord;
                Repaint();
                return;
            }

            var a = pendingPortal.Value;
            pendingPortal = null;
            Modify(definition, "Place Portal Pair", d =>
            {
                d.SetTile(HexTile.PortalTile(a, coord, portalRotation));
                d.SetTile(HexTile.PortalTile(coord, a, (6 - portalRotation) % 6));
            });
            Repaint();
        }

        static void FillHexagon(HexMapDefinition definition, int radius, bool wallBorder)
        {
            Modify(definition, "Fill Hexagon", d =>
            {
                foreach (var coord in HexCoord.Range(HexCoord.Zero, radius))
                    if (!d.TryGetTile(coord, out _))
                        d.SetTile(HexTile.Floor(coord));
                if (wallBorder)
                    foreach (var coord in HexCoord.Ring(HexCoord.Zero, radius + 1))
                        d.SetTile(HexTile.Wall(coord));
            });
        }

        static void Modify(HexMapDefinition definition, string undoName, System.Action<HexMapDefinition> change)
        {
            Undo.RecordObject(definition, undoName);
            change(definition);
            EditorUtility.SetDirty(definition);
        }

        static bool SameTile(HexTile a, HexTile b) =>
            a.Type == b.Type && a.Boost.Equals(b.Boost) && a.Pickup.Equals(b.Pickup) && a.Portal.Equals(b.Portal) && a.WallSettings.Equals(b.WallSettings);

        static HexCoord? MouseToHex(HexLayout layout, Vector3 planeOrigin, Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, planeOrigin);
            if (!plane.Raycast(ray, out float distance))
                return null;
            return layout.WorldToHex(ray.GetPoint(distance));
        }

        static HexDirection DirectionPopup(string label, HexDirection value, HexOrientation orientation)
        {
            var names = Enumerable.Range(0, HexDirectionExtensions.Count)
                .Select(i => $"{(HexDirection)i} ({((HexDirection)i).DisplayName(orientation)})")
                .ToArray();
            return (HexDirection)EditorGUILayout.Popup(label, (int)value, names);
        }
    }
}
