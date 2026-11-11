using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;
using UnityEngine;

/// <summary>
/// Scene entry point for pickups: builds a <see cref="PickupField"/> from the map and shows each pickup's
/// visual while it is available. Registers with the <see cref="GameManager"/>, whose move phase drives it
/// through <see cref="CollectAlong"/> and <see cref="AdvanceTicks"/>.
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
        Field.Collected += OnCollected;
        Field.Respawned += OnRespawned;
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


        if (Field != null)
        {
            Field.Collected -= OnCollected;
            Field.Respawned -= OnRespawned;
        }

        if (mapManager != null)
        {
            mapManager.LayoutChanged -= PositionVisuals;
        }
    }

    public void AdvanceTicks(int ticks) => Field.AdvanceTicks(ticks);

    // public void CollectAlong(SlideResult slide, IPickupCollector collector) =>
    //     Field.CollectAlong(slide, collector);

    public void CollectAlong(IEnumerable<HexCoord> path, HexCoord end, IPickupCollector collector) =>
        Field.CollectAlong(path, end, collector);

    private void SpawnVisuals()
    {
        foreach (var (coord, spawn) in Field.Spawns)
        {
            var prefab = spawn.Definition.VisualPrefab;
            if (prefab == null)
            {
                continue;
            }
            Debug.Log($"Spawning visual for {spawn.Definition.name} at {coord}");

            var visual = Instantiate(prefab, transform);
            visual.name = $"{spawn.Definition.name} {coord}";
            visual.SetActive(spawn.IsAvailable);
            _visuals.Add(coord, visual);
        }

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

    private void OnCollected(HexCoord coord, PickupSpawn _) => SetVisualActive(coord, false);

    private void OnRespawned(HexCoord coord, PickupSpawn _) => SetVisualActive(coord, true);

    private void SetVisualActive(HexCoord coord, bool active)
    {
        if (_visuals.TryGetValue(coord, out var visual))
        {
            visual.SetActive(active);
        }
    }
}
