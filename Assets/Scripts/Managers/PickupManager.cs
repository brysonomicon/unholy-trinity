using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

/// <summary>
/// Scene entry point for pickups: builds a <see cref="PickupField"/> from the map and shows each pickup's
/// visual while it is available. Registers with the <see cref="GameManager"/>, whose simulated move phase
/// collects and respawns pickups, then calls <see cref="RefreshVisuals"/>.
/// </summary>
public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private float visualHeightOffset = 0.5f;

    private readonly Dictionary<HexCoord, GameObject> _visuals = new();

    public PickupField Field { get; private set; }

    private void Start()
    {
        Field = new PickupField(mapManager.Map);
        mapManager.LayoutChanged += PositionVisuals;

        SpawnVisuals();
        gameManager.RegisterPickupManager(this);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.UnregisterPickupManager(this);
        }

        if (mapManager != null)
        {
            mapManager.LayoutChanged -= PositionVisuals;
        }
    }

    // shows each visual only while its pickup is available
    public void RefreshVisuals()
    {
        foreach (var (coord, visual) in _visuals)
        {
            visual.SetActive(Field.IsAvailable(coord));
        }
    }

    private void SpawnVisuals()
    {
        foreach (var (coord, spawn) in Field.Spawns)
        {
            var prefab = spawn.Definition.VisualPrefab;
            if (prefab == null)
            {
                continue;
            }

            var visual = Instantiate(prefab, transform);
            visual.name = $"{spawn.Definition.name} {coord}";
            _visuals.Add(coord, visual);
        }

        RefreshVisuals();
        PositionVisuals();
    }

    private void PositionVisuals()
    {
        var layout = mapManager.Layout;
        foreach (var (coord, visual) in _visuals)
        {
            visual.transform.position = layout.HexToWorld(coord) + Vector3.up * visualHeightOffset;
        }
    }
}
