using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Scene entry point for the map: builds the logical <see cref="HexMap"/> from a <see cref="HexMapDefinition"/>
    /// and spawns tile visuals. Orientation is a view-only setting and can be flipped at runtime for playtesting.
    /// The grid lies on the XZ plane at this transform's position (rotation and scale are ignored).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MapManager : MonoBehaviour
    {
        const string VisualRootName = "Tiles";

        [SerializeField] private HexMapDefinition definition;
        [SerializeField] private HexTileStyle style;

        [SerializeField]
        private HexOrientation orientation = HexOrientation.PointyTop;

        [SerializeField, Min(0.01f)] private float hexSize = 1f;

        [SerializeField,
         Tooltip(
             "Debug key that flips pointy/flat orientation in play mode. None disables it.")]
        private Key orientationToggleKey = Key.F2;

        private Transform _visualRoot;
        private HexTileStyle _runtimeDefaultStyle;
        private Material _runtimeMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private bool _visualsDirty;

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public HexMap Map { get; private set; }
        public HexMapDefinition Definition => definition;
        public HexOrientation Orientation => orientation;
        public float HexSize => hexSize;

        public HexLayout Layout =>
            new HexLayout(orientation, hexSize, transform.position);

        /// <summary>Raised after the layout changes (e.g. orientation toggled), so units can re-position.</summary>
        public event Action LayoutChanged;

        private void Awake() => Rebuild();

        private void Update()
        {
            if (orientationToggleKey != Key.None
                && Keyboard.current != null
                && Keyboard.current[orientationToggleKey].wasPressedThisFrame)
            {
                ToggleOrientation();
            }


            if (!_visualsDirty)
            {
                return;
            }

            _visualsDirty = false;
            SpawnVisuals();
            LayoutChanged?.Invoke();
        }

        private void OnValidate()
        {
            // Inspector edits during play mode (orientation, size, style) refresh the spawned tiles next frame.
            if (Application.isPlaying && Map is not null)
                _visualsDirty = true;
        }

        private void OnDestroy()
        {
            if (_runtimeDefaultStyle is not null)
                Destroy(_runtimeDefaultStyle);
            if (_runtimeMaterial is not null)
                Destroy(_runtimeMaterial);
        }

        /// <summary>Rebuilds the logical map from the definition and respawns the visuals.</summary>
        private void Rebuild()
        {
            if (definition is null)
            {
                Debug.LogWarning(
                    $"{nameof(MapManager)} on '{name}' has no map definition assigned.",
                    this);
                Map = new HexMap();
                return;
            }

            foreach (var problem in definition.Validate())
                Debug.LogWarning($"Hex map '{definition.name}': {problem}",
                    definition);

            Map = definition.Build();
            SpawnVisuals();
        }

        private void SetOrientation(HexOrientation value)
        {
            if (orientation == value)
                return;
            orientation = value;
            if (Application.isPlaying)
                _visualsDirty = true;
        }

        [ContextMenu("Toggle Orientation")]
        public void ToggleOrientation() =>
            SetOrientation(orientation == HexOrientation.PointyTop
                ? HexOrientation.FlatTop
                : HexOrientation.PointyTop);

        private void SpawnVisuals()
        {
            if (_visualRoot is not null)
                Destroy(_visualRoot.gameObject);

            _visualRoot = new GameObject(VisualRootName).transform;
            _visualRoot.SetParent(transform, false);

            var activeStyle = ResolveStyle();
            var layout = Layout;

            foreach (var tile in Map.Tiles)
            {
                var prefab = activeStyle.PrefabFor(tile);
                var go = prefab != null
                    ? SpawnPrefab(prefab, tile, layout)
                    : SpawnProcedural(activeStyle, tile, layout);
                go.name = $"{tile.Type} {tile.Coord}";
                go.AddComponent<HexTileView>().Init(tile);
            }
        }

        private GameObject SpawnPrefab(
            GameObject prefab,
            HexTile tile,
            HexLayout layout)
        {
            // Prefabs are authored pointy-top at hex size 1;
            // flat-top is the same hex turned 30 degrees.
            var rotation = orientation == HexOrientation.FlatTop
                ? Quaternion.Euler(0f, 30f, 0f)
                : Quaternion.identity;
            var go = Instantiate(prefab, layout.HexToWorld(tile.Coord),
                rotation, _visualRoot);
            go.transform.localScale = prefab.transform.localScale * hexSize;
            foreach (var visual in
                     go.GetComponentsInChildren<ITileVisual>(true))
                visual.Configure(tile, layout);
            return go;
        }

        private GameObject SpawnProcedural(
            HexTileStyle activeStyle, 
            HexTile tile,
            HexLayout layout)
        {
            var material = ResolveMaterial(activeStyle);
            _propertyBlock ??= new MaterialPropertyBlock();
            float height = activeStyle.HeightFor(tile);
            var go = CreateMeshObject("Tile", _visualRoot,
                HexMeshFactory.Prism(orientation,
                    hexSize * activeStyle.TileScale, height), material,
                activeStyle.ColorFor(tile));
            go.transform.position = layout.HexToWorld(tile.Coord);

            if (tile.Type != TileType.Boost ||
                tile.Boost.Mode != BoostMode.Fixed)
            {
                return go;
            }
            
            var arrow = CreateMeshObject("Arrow", go.transform,
                HexMeshFactory.Arrow(hexSize), material,
                activeStyle.ArrowColor);
            arrow.transform.localPosition = Vector3.up * (height + 0.01f);
            arrow.transform.rotation = Quaternion.LookRotation(
                layout.DirectionToWorld(tile.Boost.Direction), Vector3.up);

            return go;
        }

        private GameObject CreateMeshObject(string objectName, Transform parent,
            Mesh mesh, Material material, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            _propertyBlock.Clear();
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(_propertyBlock);
            return go;
        }

        private HexTileStyle ResolveStyle()
        {
            if (style is not null)
                return style;
            _runtimeDefaultStyle ??= HexTileStyle.CreateDefault();
            return _runtimeDefaultStyle;
        }

        private Material ResolveMaterial(HexTileStyle activeStyle)
        {
            if (activeStyle.TileMaterial is not null)
            {
                return activeStyle.TileMaterial;
            }
            if (_runtimeMaterial is not null)
            {
                return _runtimeMaterial;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            _runtimeMaterial = new Material(shader)
                { name = "HexTile (Runtime)" };

            return _runtimeMaterial;
        }
    }
}