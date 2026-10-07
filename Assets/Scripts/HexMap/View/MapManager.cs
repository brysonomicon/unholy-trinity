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

        [SerializeField] HexMapDefinition definition;
        [SerializeField] HexTileStyle style;
        [SerializeField] HexOrientation orientation = HexOrientation.PointyTop;
        [SerializeField, Min(0.01f)] float hexSize = 1f;
        [SerializeField, Tooltip("Debug key that flips pointy/flat orientation in play mode. None disables it.")]
        Key orientationToggleKey = Key.F2;

        Transform visualRoot;
        HexTileStyle runtimeDefaultStyle;
        Material runtimeMaterial;
        MaterialPropertyBlock propertyBlock;
        bool visualsDirty;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public HexMap Map { get; private set; }
        public HexMapDefinition Definition => definition;
        public HexOrientation Orientation => orientation;
        public float HexSize => hexSize;
        public HexLayout Layout => new HexLayout(orientation, hexSize, transform.position);

        /// <summary>Raised after the layout changes (e.g. orientation toggled), so units can re-position.</summary>
        public event Action LayoutChanged;

        void Awake() => Rebuild();

        void Update()
        {
            if (orientationToggleKey != Key.None && Keyboard.current != null && Keyboard.current[orientationToggleKey].wasPressedThisFrame)
                ToggleOrientation();

            if (visualsDirty)
            {
                visualsDirty = false;
                SpawnVisuals();
                LayoutChanged?.Invoke();
            }
        }

        void OnValidate()
        {
            // Inspector edits during play mode (orientation, size, style) refresh the spawned tiles next frame.
            if (Application.isPlaying && Map != null)
                visualsDirty = true;
        }

        void OnDestroy()
        {
            if (runtimeDefaultStyle != null)
                Destroy(runtimeDefaultStyle);
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
        }

        /// <summary>Rebuilds the logical map from the definition and respawns the visuals.</summary>
        public void Rebuild()
        {
            if (definition == null)
            {
                Debug.LogWarning($"{nameof(MapManager)} on '{name}' has no map definition assigned.", this);
                Map = new HexMap();
                return;
            }

            foreach (var problem in definition.Validate())
                Debug.LogWarning($"Hex map '{definition.name}': {problem}", definition);

            Map = definition.Build();
            SpawnVisuals();
        }

        public void SetOrientation(HexOrientation value)
        {
            if (orientation == value)
                return;
            orientation = value;
            if (Application.isPlaying)
                visualsDirty = true;
        }

        [ContextMenu("Toggle Orientation")]
        public void ToggleOrientation() =>
            SetOrientation(orientation == HexOrientation.PointyTop ? HexOrientation.FlatTop : HexOrientation.PointyTop);

        void SpawnVisuals()
        {
            if (visualRoot != null)
                Destroy(visualRoot.gameObject);

            visualRoot = new GameObject(VisualRootName).transform;
            visualRoot.SetParent(transform, false);

            var activeStyle = ResolveStyle();
            var layout = Layout;

            foreach (var tile in Map.Tiles)
            {
                var prefab = activeStyle.PrefabFor(tile);
                var go = prefab != null ? SpawnPrefab(prefab, tile, layout) : SpawnProcedural(activeStyle, tile, layout);
                go.name = $"{tile.Type} {tile.Coord}";
                go.AddComponent<HexTileView>().Init(tile);
            }
        }

        GameObject SpawnPrefab(GameObject prefab, HexTile tile, HexLayout layout)
        {
            // Prefabs are authored pointy-top at hex size 1; flat-top is the same hex turned 30 degrees.
            var rotation = orientation == HexOrientation.FlatTop ? Quaternion.Euler(0f, 30f, 0f) : Quaternion.identity;
            var go = Instantiate(prefab, layout.HexToWorld(tile.Coord), rotation, visualRoot);
            go.transform.localScale = prefab.transform.localScale * hexSize;
            foreach (var visual in go.GetComponentsInChildren<ITileVisual>(true))
                visual.Configure(tile, layout);
            return go;
        }

        GameObject SpawnProcedural(HexTileStyle activeStyle, HexTile tile, HexLayout layout)
        {
            var material = ResolveMaterial(activeStyle);
            propertyBlock ??= new MaterialPropertyBlock();
            float height = activeStyle.HeightFor(tile);
            var go = CreateMeshObject("Tile", visualRoot,
                HexMeshFactory.Prism(orientation, hexSize * activeStyle.TileScale, height), material, activeStyle.ColorFor(tile));
            go.transform.position = layout.HexToWorld(tile.Coord);

            if (tile.Type == TileType.Boost && tile.Boost.Mode == BoostMode.Fixed)
            {
                var arrow = CreateMeshObject("Arrow", go.transform, HexMeshFactory.Arrow(hexSize), material, activeStyle.ArrowColor);
                arrow.transform.localPosition = Vector3.up * (height + 0.01f);
                arrow.transform.rotation = Quaternion.LookRotation(layout.DirectionToWorld(tile.Boost.Direction), Vector3.up);
            }
            return go;
        }

        GameObject CreateMeshObject(string objectName, Transform parent, Mesh mesh, Material material, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            propertyBlock.Clear();
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(propertyBlock);
            return go;
        }

        HexTileStyle ResolveStyle()
        {
            if (style != null)
                return style;
            if (runtimeDefaultStyle == null)
                runtimeDefaultStyle = HexTileStyle.CreateDefault();
            return runtimeDefaultStyle;
        }

        Material ResolveMaterial(HexTileStyle activeStyle)
        {
            if (activeStyle.TileMaterial != null)
                return activeStyle.TileMaterial;
            if (runtimeMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                runtimeMaterial = new Material(shader) { name = "HexTile (Runtime)" };
            }
            return runtimeMaterial;
        }
    }
}
