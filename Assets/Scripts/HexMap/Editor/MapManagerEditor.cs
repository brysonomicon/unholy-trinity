using System;
using System.Linq;
using UnholyTrinity.Pickups;
using UnityEditor;
using UnityEngine;

namespace UnholyTrinity.Hex.Editing
{
    /// <summary>
    /// Inspector palette plus Scene-view painting for a MapManager's <see cref="HexMapDefinition"/>.
    /// LMB / drag paints, Shift+LMB erases, R rotates the boost direction.
    /// Portal modes: Two Way places a linked pair (click A, then B). One Way picks a target first, then every
    /// click places an entrance into it; click the target again to release it. Exit places a receive-only portal.
    /// </summary>
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : Editor
    {
        // Static so the palette survives reselecting the object. Statics are reset by domain reloads
        // (entering play mode, recompiling), so the palette is also mirrored to SessionState.
        private static bool _editing;
        private static TileType _paintType = TileType.Floor;
        private static BoostSettings _boost = BoostSettings.Along(1);
        private static WallSettings _wall = WallSettings.Normal;
        private static HealthTileSettings _healthHarm = HealthTileSettings.Harm(1);
        private static PickupDefinition _pickup;
        private static int _portalRotation;
        private static PortalPaintMode _portalMode = PortalPaintMode.TwoWay;
        private static int _fillRadius = 4;
        private static bool _fillWithWallBorder = true;

        private const string PaletteKey =
            "UnholyTrinity.Hex.MapManagerEditor.Palette";

        private static bool _paletteLoaded;

        [Serializable]
        private struct PaletteState
        {
            public bool Editing;
            public TileType PaintType;
            public BoostSettings Boost;
            public WallSettings Wall;
            public HealthTileSettings Health;
            public PickupDefinition Pickup;
            public int PortalRotation;
            public PortalPaintMode PortalMode;
            public int FillRadius;
            public bool FillWithWallBorder;
        }

        private enum PortalPaintMode
        {
            TwoWay,
            OneWay,
            Exit,
        }

        // Two Way: the first hex of the pair. One Way: the target new entrances link to.
        private static HexCoord? _pendingPortal;
        private HexCoord? _hovered;

        private void OnEnable() => LoadPalette();

        private void OnDisable() => _pendingPortal = null;

        /// <summary>Restores the palette after a domain reload; does nothing if already loaded in this domain.</summary>
        private static void LoadPalette()
        {
            if (_paletteLoaded)
                return;
            _paletteLoaded = true;

            var json = SessionState.GetString(PaletteKey, null);
            if (string.IsNullOrEmpty(json))
                return;
            var state = JsonUtility.FromJson<PaletteState>(json);
            _editing = state.Editing;
            _paintType = state.PaintType;
            _boost = state.Boost;
            _wall = state.Wall;
            _healthHarm = state.Health;
            _pickup = state.Pickup;
            _portalRotation = state.PortalRotation;
            _portalMode = state.PortalMode;
            _fillRadius = state.FillRadius;
            _fillWithWallBorder = state.FillWithWallBorder;
        }

        private static void SavePalette()
        {
            var state = new PaletteState
            {
                Editing = _editing,
                PaintType = _paintType,
                Boost = _boost,
                Wall = _wall,
                Health = _healthHarm,
                Pickup = _pickup,
                PortalRotation = _portalRotation,
                PortalMode = _portalMode,
                FillRadius = _fillRadius,
                FillWithWallBorder = _fillWithWallBorder,
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

            if (definition is null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Hex Map definition (Create > Unholy Trinity > Hex Map) to start painting.",
                    MessageType.Info);
                return;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Painting edits the asset but does not affect the running map until it is rebuilt.",
                    MessageType.None);
            }

            EditorGUI.BeginChangeCheck();

            _editing = GUILayout.Toggle(_editing,
                _editing ? "Stop Editing" : "Edit Map", "Button",
                GUILayout.Height(24));
            if (_editing)
            {
                EditorGUILayout.HelpBox(
                    "LMB paint, Shift+LMB erase, R rotate boost direction, Alt+drag orbits as usual.",
                    MessageType.None);
            }

            _paintType = (TileType)EditorGUILayout.EnumPopup("Tile", _paintType);
            if (_paintType != TileType.Portal)
                _pendingPortal = null;

            using (new EditorGUI.IndentLevelScope())
            {
                switch (_paintType)
                {
                    case TileType.Wall:
                        _wall.Kind =
                            (WallKind)EditorGUILayout.EnumPopup("Kind",
                                _wall.Kind);
                        if (_wall.Kind == WallKind.Spiked)
                        {
                            _wall.Damage =
                                EditorGUILayout.IntField("Damage", _wall.Damage);
                            _wall.DamageMode =
                                (SpikeDamageMode)EditorGUILayout.EnumPopup(
                                    new GUIContent("Damage Mode",
                                        "Scaled By Speed multiplies Damage by the unit's speed at impact."),
                                    _wall.DamageMode);
                        }

                        break;
                    case TileType.Boost:
                        _boost.Strength = EditorGUILayout.IntField(
                            new GUIContent("Strength",
                                "+ boosts, - is friction"), _boost.Strength);
                        _boost.Mode =
                            (BoostMode)EditorGUILayout.EnumPopup("Mode",
                                _boost.Mode);
                        if (_boost.Mode == BoostMode.Fixed)
                        {
                            _boost.Direction = DirectionPopup("Direction",
                                _boost.Direction, manager.Orientation);
                        }
                        break;
                    case TileType.PickupPoint:
                        _pickup = (PickupDefinition)EditorGUILayout.ObjectField(
                            new GUIContent("Pickup", "Sets the effect, trigger and respawn time for painted points."),
                            _pickup, typeof(PickupDefinition), false);
                        break;
                    case TileType.Health:
                        _healthHarm.Lethal = EditorGUILayout.Toggle(new GUIContent("Lethal", "Kills outright and ends the slide, e.g. lava or a void."), _healthHarm.Lethal);
                        if (!_healthHarm.Lethal)
                            _healthHarm.Amount = EditorGUILayout.IntField(new GUIContent("Amount", "+ heals, - harms"), _healthHarm.Amount);
                        _healthHarm.Trigger = (HealthTrigger)EditorGUILayout.EnumPopup(new GUIContent("Trigger", "On Enter fires for every hex slid into; On Stop only where the slide ends."), _healthHarm.Trigger);
                        break;
                    case TileType.Portal:
                        var mode = (PortalPaintMode)EditorGUILayout.EnumPopup(
                            new GUIContent("Mode",
                                "Two Way: a linked pair. One Way: entrances into one target. Exit: receive-only portal."),
                            _portalMode);
                        if (mode != _portalMode)
                        {
                            _portalMode = mode;
                            _pendingPortal = null;
                        }

                        if (_portalMode != PortalPaintMode.Exit)
                        {
                            _portalRotation = EditorGUILayout.IntSlider(
                                _portalMode == PortalPaintMode.TwoWay
                                    ? new GUIContent("Rotation (A to B)",
                                        "60 degree CCW steps applied when exiting at B; B to A uses the inverse.")
                                    : new GUIContent("Rotation",
                                        "60 degree CCW steps applied when exiting at the target."),
                                _portalRotation, 0, 5);
                        }
                        EditorGUILayout.LabelField(PortalHint(), EditorStyles.miniLabel);
                        break;
                    // [CB] This causes exceptions to be thrown. Sketch.
                    // case TileType.Floor:
                    //     
                    //     throw new NotImplementedException();
                    // default:
                    //     throw new ArgumentOutOfRangeException();
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _fillRadius =
                    EditorGUILayout.IntField("Hexagon Radius", _fillRadius);
                _fillWithWallBorder = GUILayout.Toggle(_fillWithWallBorder,
                    "Wall border", GUILayout.Width(90));
            }

            if (EditorGUI.EndChangeCheck())
                SavePalette();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Hexagon"))
                {
                    FillHexagon(definition, Mathf.Max(0, _fillRadius),
                        _fillWithWallBorder);
                }
                if (GUILayout.Button("Clear") &&
                    EditorUtility.DisplayDialog("Clear map",
                        $"Remove all tiles from '{definition.name}'?", "Clear",
                        "Cancel"))
                {
                    Modify(definition, "Clear Hex Map", d => d.Clear());
                }
            }

            var problems = definition.Validate();
            EditorGUILayout.LabelField($"{definition.Entries.Count} tiles",
                EditorStyles.miniLabel);
            foreach (var problem in problems)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }

        void OnSceneGUI()
        {
            var manager = (MapManager)target;
            var definition = manager.Definition;
            if (!_editing || definition == null)
                return;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            var layout = manager.Layout;
            var previousHover = _hovered;
            _hovered = MouseToHex(layout, manager.transform.position,
                e.mousePosition);

            if (_hovered.HasValue)
            {
                HexMapGizmos.OutlineHex(layout, _hovered.Value,
                    e.shift
                        ? HexTileStyle.WongVermilion
                        : HexTileStyle.WongYellow);
            }
            if (_pendingPortal.HasValue)
            {
                HexMapGizmos.OutlineHex(layout, _pendingPortal.Value,
                    HexTileStyle.WongReddishPurple, 5f);
            }
            if (_paintType == TileType.Boost && _boost.Mode == BoostMode.Fixed &&
                _hovered.HasValue)
            {
                HexMapGizmos.DrawArrow(layout, layout.HexToWorld(_hovered.Value),
                    _boost.Direction, HexTileStyle.WongYellow);
            }

            switch (e.type)
            {
                case EventType.MouseMove:
                    if (_hovered != previousHover)
                        SceneView.RepaintAll();
                    break;

                case EventType.MouseDown
                    when e.button == 0 && !e.alt && _hovered.HasValue:
                    GUIUtility.hotControl = controlId;
                    Undo.IncrementCurrentGroup();
                    ApplyAt(definition, _hovered.Value, e.shift, isDrag: false);
                    e.Use();
                    break;

                case EventType.MouseDrag
                    when GUIUtility.hotControl == controlId && _hovered.HasValue:
                    ApplyAt(definition, _hovered.Value, e.shift, isDrag: true);
                    e.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;

                case EventType.KeyDown when e.keyCode == KeyCode.R &&
                                            _paintType == TileType.Boost:
                    _boost.Direction = _boost.Direction.Rotate(e.shift ? -1 : 1);
                    _boost.Mode = BoostMode.Fixed;
                    SavePalette();
                    Repaint();
                    e.Use();
                    break;
            }
        }

        private void ApplyAt(HexMapDefinition definition, HexCoord coord, bool erase,
            bool isDrag)
        {
            if (erase)
            {
                if (definition.TryGetTile(coord, out var erased))
                {
                    Modify(definition, "Erase Hex", d =>
                    {
                        d.RemoveTile(coord);
                        if (erased.Type == TileType.Portal)
                            UnlinkEntrancesTo(d, coord);
                    });
                    if (_pendingPortal == coord)
                        _pendingPortal = null;
                }                
                return;
            }

            if (_paintType == TileType.Portal)
            {
                if (!isDrag)
                {
                    PlacePortal(definition, coord);
                }
                return;
            }

            var tile = _paintType switch
            {
                TileType.Wall => HexTile.Wall(coord, _wall.Kind == WallKind.Spiked ? _wall : new WallSettings(_wall.Kind)),
                TileType.PickupPoint => HexTile.PickupPoint(coord, _pickup),
                TileType.Boost => HexTile.BoostTile(coord, _boost),
                TileType.Health => HexTile.HealthTile(coord, _healthHarm.Lethal ? HealthTileSettings.Kill(_healthHarm.Trigger) : _healthHarm),
                _ => HexTile.Floor(coord),
            };

            if (definition.TryGetTile(coord, out var existing) &&
                SameTile(existing, tile))
            {
                return;
            }

            Modify(definition, "Paint Hex", d => d.SetTile(tile));
        }

        public void PlacePortal(HexMapDefinition definition, HexCoord coord)
        {
            switch (_portalMode)
            {
                case PortalPaintMode.Exit:
                    _pendingPortal = null;
                    // a two-way partner that pointed here keeps working as a one-way entrance
                    Modify(definition, "Place Portal Exit", d => d.SetTile(HexTile.PortalExit(coord)));
                    break;

                case PortalPaintMode.OneWay:
                    if (!_pendingPortal.HasValue)
                    {
                        _pendingPortal = coord;
                        break;
                    }

                    var portalTarget = _pendingPortal.Value;
                    if (portalTarget == coord)
                    {
                        // clicking the target again releases it
                        _pendingPortal = null;
                        break;
                    }

                    Modify(definition, "Place Portal Entrance", d =>
                    {
                        if (!d.TryGetTile(portalTarget, out var targetTile) ||
                            targetTile.Type != TileType.Portal)
                        {
                            d.SetTile(HexTile.PortalExit(portalTarget));
                        }

                        d.SetTile(HexTile.PortalTile(coord, portalTarget, _portalRotation));
                    });
                    break;

                default:
                    if (!_pendingPortal.HasValue || _pendingPortal.Value == coord)
                    {
                        _pendingPortal = coord;
                        break;
                    }

                    var a = _pendingPortal.Value;
                    _pendingPortal = null;
                    Modify(definition, "Place Portal Pair", d =>
                    {
                        d.SetTile(HexTile.PortalTile(a, coord, _portalRotation));
                        d.SetTile(
                            HexTile.PortalTile(coord, a, (6 - _portalRotation) % 6));
                    });
                    break;
            }

            Repaint();
        }

        private static string PortalHint()
        {
            switch (_portalMode)
            {
                case PortalPaintMode.Exit:
                    return "Click to place a receive-only exit.";
                case PortalPaintMode.OneWay:
                    return _pendingPortal.HasValue
                        ? $"Target {_pendingPortal.Value}: click to add entrances, click the target to release."
                        : "Click the target (an exit or existing portal).";
                default:
                    return _pendingPortal.HasValue
                        ? $"First portal at {_pendingPortal.Value}, click the second hex."
                        : "Click the first portal hex.";
            }
        }

        // entrances into a removed portal become floor so the map never holds a dangling link
        private static void UnlinkEntrancesTo(HexMapDefinition definition, HexCoord target)
        {
            var dangling = definition.Entries
                .Where(e => e.Type == TileType.Portal && !e.Portal.ExitOnly && e.Portal.Target == target)
                .Select(e => e.Coord)
                .ToList();
            foreach (var coord in dangling)
                definition.SetTile(HexTile.Floor(coord));
        }

        private static void FillHexagon(
            HexMapDefinition definition,
            int radius,
            bool wallBorder)
        {
            Modify(definition, "Fill Hexagon", d =>
            {
                foreach (var coord in HexCoord.Range(HexCoord.Zero, radius))
                {
                    if (!d.TryGetTile(coord, out _))
                        d.SetTile(HexTile.Floor(coord));
                }
                if (wallBorder)
                {
                    foreach (var coord in HexCoord.Ring(HexCoord.Zero,
                                 radius + 1))
                    {
                        d.SetTile(HexTile.Wall(coord));
                    }
                }
                    
            });
        }

        private static void Modify(HexMapDefinition definition, string undoName,
            Action<HexMapDefinition> change)
        {
            Undo.RecordObject(definition, undoName);
            change(definition);
            EditorUtility.SetDirty(definition);
        }

        private static bool SameTile(HexTile a, HexTile b) =>
            a.Type == b.Type && a.Boost.Equals(b.Boost) &&
            a.Pickup.Equals(b.Pickup) && a.Portal.Equals(b.Portal) &&
            a.WallSettings.Equals(b.WallSettings) && a.Health.Equals(b.Health);

        private static HexCoord? MouseToHex(HexLayout layout, Vector3 planeOrigin,
            Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, planeOrigin);
            if (!plane.Raycast(ray, out float distance))
            {
                return null;
            }
            return layout.WorldToHex(ray.GetPoint(distance));
        }

        private static HexDirection DirectionPopup(string label, HexDirection value,
            HexOrientation orientation)
        {
            var names = Enumerable.Range(0, HexDirectionExtensions.Count)
                .Select(i =>
                    $"{(HexDirection)i} ({((HexDirection)i).DisplayName(orientation)})")
                .ToArray();
            return (HexDirection)EditorGUILayout.Popup(label, (int)value,
                names);
        }
    }
}
